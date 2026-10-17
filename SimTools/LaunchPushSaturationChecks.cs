using System;
using Godot;

/// <summary>--launch-push-saturation-check: identity when off, identity below the knee, an order-preserving saturation above it.</summary>
public static class LaunchPushSaturationChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Launch push saturation: " + why); }
		float was = LaunchPushSaturation.Factor;
		try {
			float knee = LaunchPushSaturation.Knee;
			float big5 = 0.79f, topMid = 0.70f, mid = 0.52f, indie = 0.36f;
			LaunchPushSaturation.SetFactorForChecks(1f);
			Check(!LaunchPushSaturation.Enabled, "factor 1 must read as off");
			foreach (float impact in new[] { 0f, indie, mid, knee, topMid, big5, 1f })
				Check(LaunchPushSaturation.Apply(impact) == impact, "off must return the impact bit-for-bit");

			LaunchPushSaturation.SetFactorForChecks(0.5f);
			Check(LaunchPushSaturation.Enabled, "factor .5 must read as on");
			foreach (float impact in new[] { 0f, indie, mid, knee })
				Check(LaunchPushSaturation.Apply(impact) == impact, "at or below the knee the impact must be bit-for-bit unchanged");
			float b = LaunchPushSaturation.Apply(big5), t = LaunchPushSaturation.Apply(topMid);
			Check(b > t && t > knee, "saturation must keep the order above the knee");
			Check(Math.Abs((b - knee) - 0.5f * (big5 - knee)) < 1e-5f, "factor .5 must halve the excess over the knee");
			Check(b < big5, "a Big-5 launch must lose impact");

			LaunchPushSaturation.SetFactorForChecks(0f);
			Check(Math.Abs(LaunchPushSaturation.Apply(big5) - knee) < 1e-6f, "factor 0 caps at the knee");
			Check(LaunchPushSaturation.Apply(indie) == indie, "factor 0 leaves an Independent unchanged");
			GD.Print("LAUNCH_PUSH_SATURATION_CHECK passed");
		} finally {
			LaunchPushSaturation.SetFactorForChecks(was);
		}
	}
}
