using System;

/// <summary>--airplay-rotation-ceiling-check: the probe is an identity when off and an order-preserving soft cap when on.</summary>
public static class AirplayRotationCeilingChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Airplay rotation ceiling: " + why); }
		float was = AirplayRotationCeiling.Ceiling;
		try {
			float[] levels = { 0f, 0.05f, 0.15f, 0.21f, 0.29f, 0.40f, 0.70f, 1f };
			AirplayRotationCeiling.SetCeilingForChecks(0f);
			Check(!AirplayRotationCeiling.Enabled, "ceiling 0 must read as off");
			foreach (float x in levels)
				Check(AirplayRotationCeiling.Apply(x) == x, "off must return the rotation bit-for-bit");

			const float c = 0.27f;
			AirplayRotationCeiling.SetCeilingForChecks(c);
			Check(AirplayRotationCeiling.Enabled, "a positive ceiling must read as on");
			float previous = -1f;
			foreach (float x in levels) {
				float y = AirplayRotationCeiling.Apply(x);
				Check(y <= x && y < c, "the cap never raises rotation and stays under the ceiling");
				Check(y > previous || x == 0f, "the cap keeps rotation order");
				previous = y;
			}
			Check(AirplayRotationCeiling.Apply(0f) == 0f, "zero rotation stays zero");
			Check(AirplayRotationCeiling.Apply(0.5f * c) / (0.5f * c) > 0.999f, "half the ceiling is effectively untouched");
			Check(Math.Abs(AirplayRotationCeiling.Apply(c) / c - Math.Pow(2, -1.0 / AirplayRotationCeiling.Sharpness)) < 1e-4,
				"at the ceiling rotation is C / 2^(1/m)");
			Check(AirplayRotationCeiling.Apply(4f * c) / c > 0.999f, "far above the ceiling rotation is C");
		} finally {
			AirplayRotationCeiling.SetCeilingForChecks(was);
		}
	}
}
