using System;
using Godot;

/// <summary>--regional-radio-rise-check: off is the authored lerp bit for bit; on, rising weeks approach the authored
/// settle level faster and never overshoot it, falling weeks are untouched, and a fixed target settles where it did.</summary>
public static class RegionalRadioRiseChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Regional radio rise: " + message); checks++; }
		const float hold = 0.92f, lerp = 0.15f;
		float Authored(float r, float t) => Mathf.Lerp(r * hold, t, lerp);
		try {
			RegionalRadioRise.SetForChecks(-1f);
			Check(RegionalRadioRise.Next(0.1f, 0.5f, hold, lerp) == Authored(0.1f, 0.5f)
				&& RegionalRadioRise.Next(0.4f, 0.1f, hold, lerp) == Authored(0.4f, 0.1f), "off is the authored lerp");
			float ratio = RegionalRadioRise.SettleRatio(hold, lerp);
			Check(Math.Abs(ratio - 0.15f / 0.218f) < 1e-5f, "settle ratio is lerp / (1 - hold(1 - lerp))");

			RegionalRadioRise.SetForChecks(0.5f);
			Check(RegionalRadioRise.Next(0.4f, 0.1f, hold, lerp) == Authored(0.4f, 0.1f), "a falling week keeps the authored hold");
			Check(RegionalRadioRise.Next(0.3f, 0.3f, hold, lerp) == Authored(0.3f, 0.3f), "at or above the settle level it is authored");
			float r = 0.05f, a = 0.05f;
			for (int w = 0; w < 3; w++) { r = RegionalRadioRise.Next(r, 0.5f, hold, lerp); a = Authored(a, 0.5f); }
			Check(r > a && r <= ratio * 0.5f + 1e-6f, "rising: faster than authored and never past the settle level");
			float fr = 0f, fa = 0f;
			for (int w = 0; w < 200; w++) { fr = RegionalRadioRise.Next(fr, 0.4f, hold, lerp); fa = Authored(fa, 0.4f); }
			Check(Math.Abs(fr - fa) < 1e-4f && Math.Abs(fa - ratio * 0.4f) < 1e-4f, "a fixed target settles at the authored level");
			RegionalRadioRise.SetForChecks(0.25f);
			Check(RegionalRadioRise.Next(0.0f, 0.5f, hold, lerp) >= Authored(0.0f, 0.5f), "the slowest allowed rate is no slower than authored");
			GD.Print($"REGIONAL_RADIO_RISE_CHECK passed checks={checks}");
		} finally {
			RegionalRadioRise.SetForChecks(-1f);
		}
	}
}
