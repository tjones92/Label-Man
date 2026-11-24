using System;
using Godot;

/// <summary>--major-newcomer-package-check: identity when off and outside scope; F interpolates impact above the
/// Independent reference and the stock scale toward the floor.</summary>
public static class MajorNewcomerPackageChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Major newcomer package: " + why); }
		float was = MajorNewcomerPackage.Factor;
		try {
			var major = new AILabel { tier = LabelTier.Major };
			var mid = new AILabel { tier = LabelTier.MidTier };
			var player = new AILabel { tier = LabelTier.Major, isPlayerOwned = true };
			var newcomer = new SimulatedArtist { careerState = CareerState.NewSigning };
			var rising = new SimulatedArtist { careerState = CareerState.Rising };
			var star = new SimulatedArtist { careerState = CareerState.Star };

			MajorNewcomerPackage.SetFactorForChecks(1f);
			Check(!MajorNewcomerPackage.Enabled, "factor 1 must read as off");
			Check(!MajorNewcomerPackage.Applies(major, newcomer, false), "off must never apply");
			foreach (float i in new[] { 0f, 0.36f, 0.55f, 0.9f }) Check(MajorNewcomerPackage.Impact(i, true) == i, "off must return impact bit-for-bit");
			Check(MajorNewcomerPackage.StockScale(true) == 1f, "off must leave the stock scale at exactly 1");

			MajorNewcomerPackage.SetFactorForChecks(0.5f);
			Check(MajorNewcomerPackage.Applies(major, newcomer, false), "a Major newcomer single must be in scope");
			Check(!MajorNewcomerPackage.Applies(major, newcomer, true), "albums are out of scope");
			Check(!MajorNewcomerPackage.Applies(mid, newcomer, false), "a MidTier newcomer is out of scope");
			Check(!MajorNewcomerPackage.Applies(player, newcomer, false), "the player is out of scope");
			Check(!MajorNewcomerPackage.Applies(major, rising, false) && !MajorNewcomerPackage.Applies(major, star, false),
				"Rising and senior acts are out of scope");
			Check(MajorNewcomerPackage.Impact(0.55f, false) == 0.55f, "out of scope must return impact bit-for-bit");
			Check(MajorNewcomerPackage.StockScale(false) == 1f, "out of scope must leave the stock scale at 1");
			Check(MajorNewcomerPackage.Impact(0.30f, true) == 0.30f, "at or below the reference impact is unchanged");
			Check(Math.Abs(MajorNewcomerPackage.Impact(0.56f, true) - 0.46f) < 1e-5f, "factor .5 halves the excess over the reference");
			Check(Math.Abs(MajorNewcomerPackage.StockScale(true) - 0.80f) < 1e-6f, "factor .5 gives a stock scale of .80");

			MajorNewcomerPackage.SetFactorForChecks(0f);
			Check(Math.Abs(MajorNewcomerPackage.Impact(0.55f, true) - MajorNewcomerPackage.ImpactRef) < 1e-6f, "factor 0 sets the reference impact");
			Check(Math.Abs(MajorNewcomerPackage.StockScale(true) - MajorNewcomerPackage.StockFloor) < 1e-6f, "factor 0 sets the stock floor");
			GD.Print("MAJOR_NEWCOMER_PACKAGE_CHECK passed");
		} finally {
			MajorNewcomerPackage.SetFactorForChecks(was);
		}
	}
}
