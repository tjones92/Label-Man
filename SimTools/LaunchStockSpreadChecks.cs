using System;
using Godot;

/// <summary>--launch-stock-spread-check: identity when off, below the knee and for the player; an order-preserving
/// compression above it.</summary>
public static class LaunchStockSpreadChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Launch stock spread: " + why); }
		float was = LaunchStockSpread.Factor, pushWas = LaunchPushSaturation.Factor;
		bool scopeWas = LaunchCapScope.MajorOnly;
		try {
			float knee = LaunchStockSpread.Knee;
			float major = 0.88f, topMid = 0.75f, mid = 0.52f, indie = 0.40f;
			LaunchStockSpread.SetFactorForChecks(1f);
			Check(!LaunchStockSpread.Enabled, "factor 1 must read as off");
			foreach (float d in new[] { 0f, indie, mid, knee, topMid, major, 1f })
				Check(LaunchStockSpread.Apply(d, false) == d, "off must return the depth bit-for-bit");

			LaunchStockSpread.SetFactorForChecks(0.5f);
			Check(LaunchStockSpread.Enabled, "factor .5 must read as on");
			foreach (float d in new[] { 0f, indie, mid, knee })
				Check(LaunchStockSpread.Apply(d, false) == d, "at or below the knee the depth must be bit-for-bit unchanged");
			float m = LaunchStockSpread.Apply(major, false), t = LaunchStockSpread.Apply(topMid, false);
			Check(m > t && t > knee, "the compression must keep the order above the knee");
			Check(Math.Abs((m - knee) - 0.5f * (major - knee)) < 1e-5f, "factor .5 must halve the excess over the knee");
			Check(LaunchStockSpread.Apply(major, true) == major, "the player's depth must be untouched");

			LaunchStockSpread.SetFactorForChecks(0f);
			Check(Math.Abs(LaunchStockSpread.Apply(major, false) - knee) < 1e-6f, "factor 0 caps at the knee");
			Check(LaunchStockSpread.Apply(indie, false) == indie, "factor 0 leaves an Independent unchanged");
			// --launch-caps-major-only: both caps reach Major-tier labels only; off, every label as before.
			var majorLabel = new AILabel { tier = LabelTier.Major };
			var indieLabel = new AILabel { tier = LabelTier.Independent };
			LaunchPushSaturation.SetFactorForChecks(0f);
			LaunchCapScope.SetMajorOnlyForChecks(false);
			Check(LaunchCapScope.Applies(indieLabel) && LaunchCapScope.Applies(majorLabel), "off must scope the caps to every label");
			Check(LaunchPushSaturation.Apply(0.70f, indieLabel) == knee, "off must saturate a non-Major above the knee as before");
			LaunchCapScope.SetMajorOnlyForChecks(true);
			Check(!LaunchCapScope.Applies(indieLabel) && LaunchCapScope.Applies(majorLabel), "major-only must scope the caps to Majors");
			Check(LaunchPushSaturation.Apply(0.70f, indieLabel) == 0.70f, "major-only must leave a non-Major's impact untouched");
			Check(Math.Abs(LaunchPushSaturation.Apply(0.90f, majorLabel) - knee) < 1e-6f, "major-only must still saturate a Major");
			GD.Print("LAUNCH_STOCK_SPREAD_CHECK passed");
		} finally {
			LaunchStockSpread.SetFactorForChecks(was);
			LaunchPushSaturation.SetFactorForChecks(pushWas);
			LaunchCapScope.SetMajorOnlyForChecks(scopeWas);
		}
	}
}
