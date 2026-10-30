using System;

static class AudienceChecks {
	public static void Run() {
		void Check(bool condition, string text) { if (!condition) throw new Exception(text); }
		var cold = new BoundedAudienceState(100, 0);
		Check(cold.Prepare(0, 0, 1, 100) == 0, "No awareness/contact must not manufacture buyers");
		cold.Settle(0);
		var stockout = new BoundedAudienceState(100, .5);
		stockout.Prepare(0, 0, 1, 1); stockout.Settle(0);
		Check(stockout.Owners == 0 && stockout.Interested == 45, "Unfilled intent must not deplete owners");
		var full = new BoundedAudienceState(100, 1);
		full.Prepare(0, 0, 1, 1000); full.Settle(100);
		Check(full.Prepare(100, 100, 1, 1000) == 0, "Owners cannot buy title again after saturation");
		full.Settle(0);
		var nonbuyer = new BoundedAudienceState(100, 0);
		nonbuyer.Prepare(1, 0, 1, 0); nonbuyer.Settle(0);
		Check(nonbuyer.Enthusiasts > 0 && nonbuyer.Owners == 0, "Nonbuyers may become enthusiasts");
		double oldEnthusiasm = nonbuyer.Enthusiasts;
		nonbuyer.Prepare(0, 0, 1, 0); nonbuyer.Settle(0);
		Check(nonbuyer.Enthusiasts == oldEnthusiasm*.75, "Enthusiasm decays without listening");
		var rng = new Random(42);
		for (int trial = 0; trial < 100; trial++) {
			var s = new BoundedAudienceState(100 + rng.NextDouble()*10000, rng.NextDouble());
			for (int week = 0; week < 200; week++) {
				double intent = s.Prepare(rng.NextDouble()*10, rng.NextDouble(), rng.NextDouble(), rng.NextDouble()*5);
				s.Settle(Math.Floor(intent*rng.NextDouble()));
				Check(s.Interested >= 0 && s.Owners >= 0 && s.Interested+s.Owners <= s.Capacity+1e-6
					&& s.Enthusiasts >= 0 && s.Enthusiasts <= s.Capacity+1e-6, "Population must remain conserved");
			}
		}
		bool rejected = false;
		try { cold.Prepare(0, 0, 1, 0); cold.Settle(1); } catch (InvalidOperationException) { rejected = true; }
		Check(rejected, "Settlement above finite intent must fail");
		Console.WriteLine("Audience checks passed: no-source identity, stockout, saturation, nonbuyer advocacy, decay, 20,000 conservation transitions, oversell rejection.");
	}
}
