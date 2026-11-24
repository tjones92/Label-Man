using System;
using Godot;

/// <summary>--label-blind-launch-check: off, the scope never opens and every read is the label's own; on, a live AI
/// single's launch reads the reference footprint (every region covered at the reference reach, none strong or home) and
/// the reference campaign impact, reputation and payola, while an album, a player record and opening catalog are untouched.
/// Neither path draws from the global stream beyond the launch stock's own noise draw.</summary>
public static class LabelBlindLaunchChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Label-blind launch: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
		bool promoWas = LabelBlindLaunch.Promo, footWas = LabelBlindLaunch.Foot; float doseWas = LabelBlindLaunch.Dose;
		LabelBlindLaunch.FootTarget scopeWas = LabelBlindLaunch.Scope;
		ulong stateWas = GameRandom.State;
		try {
			var indie = new AILabel { labelId = "indie", tier = LabelTier.Independent, distributionStrength = 0.3f, nationalReach = 0.25f,
				reputation = 0.2f, payolaWillingness = 0.6f, marketingPower = 0.4f, budgetLevel = 0.3f, homeRegion = "deepsouth",
				distributionRegions = new[] { "eastcoast" }, strongRegions = new[] { "greatlakes" } };
			var major = new AILabel { labelId = "major", tier = LabelTier.Major, distributionStrength = 0.95f, nationalReach = 0.9f,
				reputation = 0.8f, payolaWillingness = 0.2f, marketingPower = 0.9f, budgetLevel = 0.9f, homeRegion = "eastcoast",
				distributionRegions = new[] { "eastcoast", "westcoast" }, strongRegions = new[] { "eastcoast" } };
			var live = new GameDate(1961, 3, 1);
			RecordRuntimeData Make(string id, ReleaseFormat format, bool player = false, GameDate? date = null) =>
				new(new Record { recordId = id, artistId = "act", format = format, isPlayerOwned = player, releaseDate = date ?? live });
			RecordRuntimeData single = Make("blind-single", ReleaseFormat.Single), album = Make("blind-album", ReleaseFormat.Album),
				player = Make("blind-player", ReleaseFormat.Single, true), opening = Make("blind-opening", ReleaseFormat.Single, false, GameDate.StartDate);
			int Stock(AILabel l, string region) { GameRandom.Seed(77); return ChartSimulator.CalculateInitialRegionalStock(l, region, 1f, 1f, "blind-single"); }
			string[] regions = { "eastcoast", "greatlakes", "deepsouth", "westcoast" };

			LabelBlindLaunch.SetForChecks(false, false);
			float[] factorOff = Array.ConvertAll(regions, r => ChartSimulator.GetRegionalLaunchFactor(indie, r));
			int[] stockOff = Array.ConvertAll(regions, r => Stock(indie, r));
			using (LabelBlindLaunch.Launch(single, indie)) {
				Check(!LabelBlindLaunch.InFoot, "off must never open the footprint scope");
				for (int i = 0; i < regions.Length; i++) {
					Check(ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]) == factorOff[i], "off must keep the launch factor");
					Check(Stock(indie, regions[i]) == stockOff[i], "off must keep the launch stock");
				}
			}
			Check(LabelBlindLaunch.Impact(0.7f, single, indie) == 0.7f && LabelBlindLaunch.Reputation(single, indie) == indie.reputation &&
				LabelBlindLaunch.Payola(single, indie) == indie.payolaWillingness, "off must keep the label's promotion");
			Check(factorOff[1] > 1f && factorOff[3] < factorOff[0], "fixtures must have strong, covered and uncovered regions");

			LabelBlindLaunch.SetForChecks(false, true);
			float reference = 0.55f + LabelBlindLaunch.ReferenceReach * 0.45f;
			using (LabelBlindLaunch.Launch(single, indie)) {
				Check(LabelBlindLaunch.InFoot, "foot must open the scope for a live AI single");
				int first = Stock(indie, regions[0]);
				foreach (string r in regions) {
					Check(Near(ChartSimulator.GetRegionalLaunchFactor(indie, r), reference), "foot must launch every region at the reference factor");
					Check(Near(ChartSimulator.GetRegionalLaunchFactor(major, r), reference), "foot must launch a Major at the same factor");
					Check(Stock(indie, r) == first && Stock(major, r) == first, "foot must stock every region and label alike");
				}
			}
			Check(!LabelBlindLaunch.InFoot, "the scope must close on dispose");
			foreach (RecordRuntimeData other in new[] { album, player, opening })
				using (LabelBlindLaunch.Launch(other, indie)) Check(!LabelBlindLaunch.InFoot, "foot must skip albums, player records and opening catalog");
			Check(LabelBlindLaunch.Impact(0.7f, single, indie) == 0.7f, "foot alone must keep promotion");

			// The parts of foot read alone: factor moves the launch factor only, stock the launch stock only.
			LabelBlindLaunch.SetFootPartsForChecks(true, false, false, false, false);
			using (LabelBlindLaunch.Launch(single, indie)) {
				Check(LabelBlindLaunch.InFactor && !LabelBlindLaunch.InStock && !LabelBlindLaunch.InCity && !LabelBlindLaunch.InHold &&
					!LabelBlindLaunch.InExpose, "factor must open its own read only");
				for (int i = 0; i < regions.Length; i++) {
					Check(Near(ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]), reference), "factor must set the reference launch factor");
					Check(Stock(indie, regions[i]) == stockOff[i], "factor alone must keep the launch stock");
				}
			}
			LabelBlindLaunch.SetFootPartsForChecks(false, true, false, false, false);
			using (LabelBlindLaunch.Launch(single, indie)) {
				int first = Stock(indie, regions[0]);
				for (int i = 0; i < regions.Length; i++) {
					Check(ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]) == factorOff[i], "stock alone must keep the launch factor");
					Check(Stock(indie, regions[i]) == first, "stock must stock every region alike");
				}
			}
			LabelBlindLaunch.SetFootPartsForChecks(false, false, true, true, true);
			using (LabelBlindLaunch.Launch(single, indie)) {
				Check(LabelBlindLaunch.InCity && LabelBlindLaunch.InHold && LabelBlindLaunch.InExpose && !LabelBlindLaunch.InFactor &&
					!LabelBlindLaunch.InStock, "city, hold and expose must open only their own reads");
				for (int i = 0; i < regions.Length; i++)
					Check(ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]) == factorOff[i] && Stock(indie, regions[i]) == stockOff[i],
						"city, hold and expose must keep the launch factor and stock");
			}

			// --label-blind-launch-scope: the foot parts reach only a Major's single (major) or a Major newcomer's (major-newcomer).
			var newcomer = new SimulatedArtist { artistId = "new-act", careerState = CareerState.NewSigning };
			var unsignedAct = new SimulatedArtist { artistId = "unsigned-act", careerState = CareerState.Unsigned };
			var veteran = new SimulatedArtist { artistId = "old-act", careerState = CareerState.Established };
			Check(LabelBlindLaunch.Scope == LabelBlindLaunch.FootTarget.All, "the scope must default to every single");
			LabelBlindLaunch.SetForChecks(false, true);
			LabelBlindLaunch.SetScopeForChecks(LabelBlindLaunch.FootTarget.MajorNewcomer);
			float majorOwn = ChartSimulator.GetRegionalLaunchFactor(major, "westcoast");
			Check(majorOwn > reference + 0.1f, "a Major's own covered factor must sit above the reference");
			using (LabelBlindLaunch.Launch(single, major, newcomer)) {
				Check(LabelBlindLaunch.InFoot, "major-newcomer must open a Major newcomer's launch");
				foreach (string r in regions) Check(Near(ChartSimulator.GetRegionalLaunchFactor(major, r), reference), "a scoped Major newcomer must launch at the reference factor");
			}
			using (LabelBlindLaunch.Launch(single, major, unsignedAct)) Check(LabelBlindLaunch.InFoot, "major-newcomer must open an Unsigned act at a Major");
			using (LabelBlindLaunch.Launch(single, major, veteran)) {
				Check(!LabelBlindLaunch.InFoot, "major-newcomer must not open a Major's established act");
				Check(ChartSimulator.GetRegionalLaunchFactor(major, "westcoast") == majorOwn, "an established Major act must keep its own factor");
			}
			using (LabelBlindLaunch.Launch(single, major)) Check(!LabelBlindLaunch.InFoot, "major-newcomer must not open a launch without an artist");
			using (LabelBlindLaunch.Launch(single, indie, newcomer)) {
				Check(!LabelBlindLaunch.InFoot, "major-newcomer must not open a non-Major newcomer");
				for (int i = 0; i < regions.Length; i++) Check(ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]) == factorOff[i] &&
					Stock(indie, regions[i]) == stockOff[i], "a non-Major under the scope must keep its own factor and stock");
			}
			foreach (RecordRuntimeData other in new[] { album, player, opening })
				using (LabelBlindLaunch.Launch(other, major, newcomer)) Check(!LabelBlindLaunch.InFoot, "the scope must still skip albums, player records and opening catalog");
			LabelBlindLaunch.SetScopeForChecks(LabelBlindLaunch.FootTarget.Major);
			using (LabelBlindLaunch.Launch(single, major, veteran)) Check(LabelBlindLaunch.InFoot, "major must open a Major's established act");
			using (LabelBlindLaunch.Launch(single, major)) Check(LabelBlindLaunch.InFoot, "major must open a Major launch without an artist");
			using (LabelBlindLaunch.Launch(single, indie, newcomer)) Check(!LabelBlindLaunch.InFoot, "major must not open a non-Major");
			LabelBlindLaunch.SetScopeForChecks(LabelBlindLaunch.FootTarget.All);
			using (LabelBlindLaunch.Launch(single, indie, newcomer)) Check(LabelBlindLaunch.InFoot, "an unscoped foot must open every live single");

			// --label-blind-launch-dose: opens a keyed share of a non-Major's regions to the reference footprint, lift only.
			LabelBlindLaunch.SetFootPartsForChecks(false, false, false, false, false);
			using (LabelBlindLaunch.Launch(single, indie))
				foreach (string r in regions) Check(!LabelBlindLaunch.Opened(r), "no dose must open nothing");
			LabelBlindLaunch.SetDoseForChecks(1f);
			using (LabelBlindLaunch.Launch(single, indie)) {
				Check(!LabelBlindLaunch.InFoot, "the dose must not open the ablation's foot scope");
				for (int i = 0; i < regions.Length; i++) {
					Check(LabelBlindLaunch.Opened(regions[i]), "dose 1 must open every region");
					float f = ChartSimulator.GetRegionalLaunchFactor(indie, regions[i]);
					Check(f >= reference - 1e-5f && f >= factorOff[i], "an opened region must read at least the reference and never less than its own factor");
					Check(Stock(indie, regions[i]) == stockOff[i], "the dose must leave launch stock alone");
				}
				Check(ChartSimulator.GetRegionalLaunchFactor(indie, "greatlakes") == factorOff[1], "a strong region must keep its own factor");
			}
			foreach (string r in regions) Check(!LabelBlindLaunch.Opened(r), "the dose scope must close on dispose");
			using (LabelBlindLaunch.Launch(single, major))
				foreach (string r in regions) Check(!LabelBlindLaunch.Opened(r), "a Major must never be opened");
			foreach (RecordRuntimeData other in new[] { album, player, opening })
				using (LabelBlindLaunch.Launch(other, indie)) Check(!LabelBlindLaunch.Opened(regions[0]), "the dose must skip albums, player records and opening catalog");
			LabelBlindLaunch.SetDoseForChecks(0.5f);
			int opened = 0;
			for (int k = 0; k < 200; k++) {
				var s = Make("dose-" + k, ReleaseFormat.Single);
				using (LabelBlindLaunch.Launch(s, indie)) foreach (string r in regions) if (LabelBlindLaunch.Opened(r)) opened++;
			}
			Check(opened > 320 && opened < 480, $"dose 0.5 must open about half the regions (opened {opened} of 800)");

			// --label-blind-launch-dose-hot-acts: only a single whose act had a Top 40 in the last 26 weeks is opened.
			bool knownWas = KnownActAirplay.Enabled;
			KnownActAirplay.SetForChecks(true);
			try {
				LabelBlindLaunch.SetDoseForChecks(1f); LabelBlindLaunch.SetDoseHotActsForChecks(true);
				var hitAct = new SimulatedArtist { artistId = "hot-act", hitCadenceLastTop40Week = 90 };
				var coldAct = new SimulatedArtist { artistId = "cold-act" };
				using (LabelBlindLaunch.Launch(single, indie, hitAct, 100)) Check(LabelBlindLaunch.Opened(regions[0]), "a hit act's single within the window must be opened");
				using (LabelBlindLaunch.Launch(single, indie, hitAct, 90 + LabelBlindLaunch.HotWindowWeeks)) Check(LabelBlindLaunch.Opened(regions[0]), "the window's last week must still open");
				using (LabelBlindLaunch.Launch(single, indie, hitAct, 91 + LabelBlindLaunch.HotWindowWeeks)) Check(!LabelBlindLaunch.Opened(regions[0]), "past the window must not open");
				using (LabelBlindLaunch.Launch(single, indie, coldAct, 100)) Check(!LabelBlindLaunch.Opened(regions[0]), "a cold act must not be opened");
				using (LabelBlindLaunch.Launch(single, indie)) Check(!LabelBlindLaunch.Opened(regions[0]), "no artist must not be opened");
				using (LabelBlindLaunch.Launch(single, major, hitAct, 100)) Check(!LabelBlindLaunch.Opened(regions[0]), "a Major hit act must never be opened");
				LabelBlindLaunch.SetDoseHotActsForChecks(false);
				using (LabelBlindLaunch.Launch(single, indie, coldAct, 100)) Check(LabelBlindLaunch.Opened(regions[0]), "without the restriction a cold act is opened as before");
			} finally { LabelBlindLaunch.SetDoseHotActsForChecks(false); KnownActAirplay.SetForChecks(knownWas); }

			// --label-blind-launch-dose-exposure: the chance an opened region reads at least; the reference chance when not named.
			Check(Near(LabelBlindLaunch.DoseExposureChance, LabelBlindLaunch.ReferenceExposureChance), "without the switch the dose reads the reference exposure chance");
			LabelBlindLaunch.SetDoseExposureForChecks(0.95f);
			Check(Near(LabelBlindLaunch.DoseExposureChance, 0.95f), "the named exposure chance must replace the reference");
			using (LabelBlindLaunch.Launch(single, major))
				Check(!LabelBlindLaunch.Opened(regions[0]), "the exposure chance must not open a Major");
			LabelBlindLaunch.SetDoseExposureForChecks(null);
			Check(Near(LabelBlindLaunch.DoseExposureChance, LabelBlindLaunch.ReferenceExposureChance), "clearing the switch must restore the reference chance");
			LabelBlindLaunch.SetDoseForChecks(0f);

			LabelBlindLaunch.SetForChecks(true, false);
			Check(LabelBlindLaunch.Impact(0.7f, single, indie) == LabelBlindLaunch.ReferenceImpact &&
				LabelBlindLaunch.Impact(0.2f, single, major) == LabelBlindLaunch.ReferenceImpact, "promo must give every live single the reference impact");
			Check(LabelBlindLaunch.Reputation(single, major) == LabelBlindLaunch.ReferenceReputation &&
				LabelBlindLaunch.Payola(single, major) == LabelBlindLaunch.ReferencePayola, "promo must read the reference reputation and payola");
			Check(LabelBlindLaunch.Impact(0.7f, album, indie) == 0.7f && LabelBlindLaunch.Impact(0.7f, player, indie) == 0.7f &&
				LabelBlindLaunch.Impact(0.7f, opening, indie) == 0.7f && LabelBlindLaunch.Impact(0.7f, null, indie) == 0.7f,
				"promo must skip albums, player records, opening catalog and a missing record");
			using (LabelBlindLaunch.Launch(single, indie)) Check(!LabelBlindLaunch.InFoot, "promo alone must not open the footprint scope");
			GD.Print("LABEL_BLIND_LAUNCH_CHECK passed");
		} finally {
			LabelBlindLaunch.SetForChecks(promoWas, footWas); LabelBlindLaunch.SetDoseForChecks(doseWas); LabelBlindLaunch.SetScopeForChecks(scopeWas);
			GameRandom.State = stateWas;
		}
	}
}
