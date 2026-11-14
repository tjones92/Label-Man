using System;
using Godot;

/// <summary>--uncovered-sales-terms-check: off, nothing is split; on, only units in uncovered markets are billed, by unit
/// share of the week's revenue, at the configured honesty; the player is never split; malformed switches are refused.</summary>
public static class UncoveredSalesTermsChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Uncovered sales terms: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-3f;
		int weeksWas = UncoveredSalesTerms.Weeks; float honestyWas = UncoveredSalesTerms.Honesty;
		try {
			var indie = new AILabel { labelId = "indie", tier = LabelTier.Independent };
			var player = new AILabel { labelId = "player", tier = LabelTier.Small, isPlayerOwned = true };
			var units = new[] { ("r1", "eastcoast", 600), ("r1", "westcoast", 300), ("r2", "westcoast", 100), ("r2", "south", 0) };
			bool Covered(string region, string record) => region == "eastcoast";

			UncoveredSalesTerms.SetForChecks(0);
			Check(UncoveredSalesTerms.Split(indie, 1000f, units, Covered).Count == 0, "off must split nothing");

			UncoveredSalesTerms.SetForChecks(10, .8f);
			var split = UncoveredSalesTerms.Split(indie, 1000f, units, Covered);
			Check(split.Count == 1 && split.ContainsKey("westcoast"), "only the uncovered market with units must be billed");
			Check(Near(split["westcoast"].Billed, 400f), "billed must be the uncovered unit share of revenue (400 of 1000 units)");
			Check(Near(split["westcoast"].Collectable, 320f), "collectable must be billed x honesty");
			Check(UncoveredSalesTerms.Split(player, 1000f, units, Covered).Count == 0, "the player must not be split");
			Check(UncoveredSalesTerms.Split(indie, 0f, units, Covered).Count == 0, "no revenue must split nothing");

			bool Refused(params string[] args) {
				typeof(UncoveredSalesTerms).GetField("configured", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
					.SetValue(null, false);
				try { UncoveredSalesTerms.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Refused("--uncovered-sales-terms=0"), "0 weeks must be refused");
			Check(Refused("--uncovered-sales-honesty=0.9"), "honesty without the switch must be refused");
			Check(Refused("--uncovered-sales-terms=8", "--uncovered-sales-honesty=1.2"), "honesty above 1 must be refused");
			Check(!Refused("--uncovered-sales-terms=8", "--uncovered-sales-honesty=0.9") &&
				UncoveredSalesTerms.Weeks == 8 && Near(UncoveredSalesTerms.Honesty, .9f), "a valid pair must configure");
			GD.Print("UNCOVERED_SALES_TERMS_CHECK passed");
		} finally {
			UncoveredSalesTerms.SetForChecks(weeksWas, honestyWas);
		}
	}
}
