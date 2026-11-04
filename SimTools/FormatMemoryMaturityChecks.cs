using System;
using Godot;

/// <summary>
/// Audit A1 gate: with --format-memory-maturity-curve, a provisional format-memory estimate of a front-loaded
/// release lands near its final outcome; with the switch off the frozen linear maturity is unchanged.
/// The synthetic album follows AlbumSimulator's normal-album shape (flat demand to week 8, then 0.93 a week)
/// under a 0.75 awareness ramp, retires at week 75 (the reference run's median final age) and carries the
/// reference run's median album sunk cost, 23% of lifetime label net.
///   --path . SimTools/FormatMemoryMaturityChecks.tscn
/// Prints FORMAT_MEMORY_MATURITY_CHECKS_PASS / _FAIL.
/// </summary>
public partial class FormatMemoryMaturityChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	private static float[] Cumulative(Func<int, float> weekly, int weeks) {
		var cumulative = new float[weeks];
		float sum = 0f;
		for (int w = 0; w < weeks; w++) cumulative[w] = sum += weekly(w);
		return cumulative;
	}

	// Estimate at `age` and the final outcome, both as the memory computes them.
	private static (float Estimate, float Final) Project(float[] cumulative, float sunkShare, bool album, int age) {
		float lifetime = cumulative[^1], sunk = sunkShare * lifetime;
		float maturity = CompetitorManager.GetResponsiveMemoryMaturity(album, age);
		(float estimate, _) = CompetitorManager.GetResponsiveMemoryEconomicsForProbe(cumulative[age], sunk,
			terminalExpectedNet: 0f, maturity, finalized: false);
		(float final, _) = CompetitorManager.GetResponsiveMemoryEconomicsForProbe(lifetime, sunk,
			terminalExpectedNet: 0f, 1f, finalized: true);
		return (estimate, final);
	}

	public override void _Ready() {
		bool saved = FormatMemoryMaturityCurve.CaptureSwitch();
		try {
			float[] album = Cumulative(w => (1f - Mathf.Pow(.75f, w + 1)) * (w < 8 ? 1f : Mathf.Pow(.93f, w - 8)), 76);
			float[] flatAlbum = Cumulative(w => w < 8 ? 1f : Mathf.Pow(.93f, w - 8), 76);
			// A single: two-week build, then 0.90 a week, retired at week 33 (78% earned by age 13; the reference run measured 75%).
			float[] single = Cumulative(w => (w < 2 ? .6f + .2f * w : 1f) * Mathf.Pow(.90f, Mathf.Max(0, w - 2)), 34);

			FormatMemoryMaturityCurve.ConfigureForProbe(false);
			Check(CompetitorManager.GetResponsiveMemoryMaturity(true, 13) == Mathf.Clamp(14f / 52f, .05f, 1f) &&
				CompetitorManager.GetResponsiveMemoryMaturity(false, 13) == Mathf.Clamp(14f / 20f, .05f, 1f) &&
				CompetitorManager.GetResponsiveMemoryMaturity(true, 52) == 1f && CompetitorManager.GetResponsiveMemoryMaturity(false, 0) == .05f,
				"switch off: maturity is the frozen linear (age + 1) / 52 or / 20");
			(float offEstimate, float offFinal) = Project(album, .23f, album: true, 13);
			Check(offEstimate > 1.8f * offFinal,
				$"switch off: the linear age-13 album estimate overshoots ({offEstimate:F2} vs final {offFinal:F2}), as audit A1 measured");

			FormatMemoryMaturityCurve.ConfigureForProbe(true);
			foreach (int age in new[] { 13, 26, 52 }) {
				(float estimate, float final) = Project(album, .23f, album: true, age);
				Check(Math.Abs(estimate / final - 1f) <= .15f,
					$"curve on: front-loaded album age {age} estimate {estimate:F2} within 15% of final {final:F2} ({estimate / final - 1f:+0.0%;-0.0%})");
			}
			(float flatEstimate, float flatFinal) = Project(flatAlbum, .23f, album: true, 13);
			Check(Math.Abs(flatEstimate / flatFinal - 1f) <= .15f,
				$"curve on: album with no awareness ramp, age-13 estimate {flatEstimate:F2} within 15% of final {flatFinal:F2}");
			(float singleEstimate, float singleFinal) = Project(single, .38f, album: false, 13);
			Check(Math.Abs(singleEstimate / singleFinal - 1f) <= .15f,
				$"curve on: single age-13 estimate {singleEstimate:F2} within 15% of final {singleFinal:F2}");
			float previous = 0f;
			bool monotone = true;
			for (int age = 0; age <= 90; age++) {
				float albumFraction = FormatMemoryMaturityCurve.Fraction(true, age), singleFraction = FormatMemoryMaturityCurve.Fraction(false, age);
				monotone &= albumFraction >= previous && singleFraction >= .05f && singleFraction <= 1f;
				previous = albumFraction;
			}
			Check(monotone && FormatMemoryMaturityCurve.Fraction(true, 13) == .534f && FormatMemoryMaturityCurve.Fraction(true, 200) == 1f &&
				FormatMemoryMaturityCurve.Fraction(false, 26) == .973f,
				"curve is monotone, clamped to [.05, 1], and passes through the fitted knots");

			GD.Print($"FORMAT_MEMORY_MATURITY_CHECKS_PASS checks={checks}");
			FormatMemoryMaturityCurve.ConfigureForProbe(saved);
			GetTree().Quit(0);
		} catch (Exception e) {
			FormatMemoryMaturityCurve.ConfigureForProbe(saved);
			GD.PushError($"FORMAT_MEMORY_MATURITY_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}
}
