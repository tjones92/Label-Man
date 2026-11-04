using System;

/// <summary>--radio-push-spread-check: the probe is an identity when off and an order-preserving compression when on.</summary>
public static class RadioPushSpreadChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Radio push spread: " + why); }
		float was = RadioPushSpread.Factor;
		try {
			float major = 0.79f, mid = 0.53f, indie = 0.36f;
			RadioPushSpread.SetFactorForChecks(1f);
			Check(!RadioPushSpread.Enabled, "factor 1 must read as off");
			foreach (float push in new[] { 0f, indie, mid, major, 1f })
				Check(RadioPushSpread.Apply(push) == push, "off must return the push bit-for-bit");

			RadioPushSpread.SetFactorForChecks(0.5f);
			float m = RadioPushSpread.Apply(major), d = RadioPushSpread.Apply(mid), i = RadioPushSpread.Apply(indie);
			Check(RadioPushSpread.Enabled, "factor .5 must read as on");
			Check(m > d && d > i, "compression must keep the tier order");
			Check(Math.Abs((m - i) - 0.5f * (major - indie)) < 1e-5f, "factor .5 must halve the Major-Independent gap");
			Check(Math.Abs(RadioPushSpread.Apply(RadioPushSpread.Pivot) - RadioPushSpread.Pivot) < 1e-6f, "the pivot is fixed");
			Check(m < major && i > indie, "Majors lose push and Independents gain it");

			RadioPushSpread.SetFactorForChecks(0f);
			Check(Math.Abs(RadioPushSpread.Apply(major) - RadioPushSpread.Pivot) < 1e-6f, "factor 0 flattens to the pivot");
		} finally {
			RadioPushSpread.SetFactorForChecks(was);
		}
	}
}
