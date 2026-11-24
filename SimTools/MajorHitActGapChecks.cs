using System;
using Godot;

/// <summary>--major-hit-act-gap-check: --slate-major-hit-act-gap=W spaces only an AI Major's hot act (Top 40 in the last
/// 26 weeks); off, every act reads SingleGapWeeks exactly as before.</summary>
public static class MajorHitActGapChecks {
	public const string Switch = "--major-hit-act-gap-check";

	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Major hit-act gap: " + why); }
		AILabel Lab(LabelTier tier, bool player = false) => new AILabel { labelId = "lab_" + Guid.NewGuid().ToString("N"), tier = tier, isPlayerOwned = player };
		var hot = new SimulatedArtist { artistId = "gap-hot", hitCadenceLastTop40Week = 100 };
		var cold = new SimulatedArtist { artistId = "gap-cold" };
		int floor = LabelReleaseProgramme.SingleFloor;
		try {
			LabelReleaseProgramme.SetMajorHitActGapForChecks(0);
			Check(LabelReleaseProgramme.SingleGapWeeks(hot, 110, Lab(LabelTier.Major)) == LabelReleaseProgramme.SingleGapWeeks(hot, 110),
				"off: a Major's hot act reads the ordinary gap");
			LabelReleaseProgramme.SetMajorHitActGapForChecks(12);
			Check(LabelReleaseProgramme.SingleGapWeeks(hot, 110, Lab(LabelTier.Major)) == Math.Max(floor, 12), "on: a Major's hot act waits W");
			Check(LabelReleaseProgramme.SingleGapWeeks(hot, 100 + LabelReleaseProgramme.ColdActRecentWeeks + 1, Lab(LabelTier.Major)) ==
				LabelReleaseProgramme.SingleGapWeeks(hot, 100 + LabelReleaseProgramme.ColdActRecentWeeks + 1), "on: the act cools after 26 weeks");
			Check(LabelReleaseProgramme.SingleGapWeeks(cold, 110, Lab(LabelTier.Major)) == LabelReleaseProgramme.SingleGapWeeks(cold, 110), "on: cold Major acts unchanged");
			foreach (LabelTier tier in new[] { LabelTier.MidTier, LabelTier.Independent, LabelTier.Small })
				Check(LabelReleaseProgramme.SingleGapWeeks(hot, 110, Lab(tier)) == LabelReleaseProgramme.SingleGapWeeks(hot, 110), "on: non-Major labels unchanged");
			Check(LabelReleaseProgramme.SingleGapWeeks(hot, 110, Lab(LabelTier.Major, player: true)) == LabelReleaseProgramme.SingleGapWeeks(hot, 110), "on: the player unchanged");
			Check(LabelReleaseProgramme.SingleGapWeeks(hot, 110) == LabelReleaseProgramme.SingleGapWeeks(hot, 110, null), "on: no label, no change");
			GD.Print("MAJOR_HIT_ACT_GAP_CHECK passed");
		} finally { LabelReleaseProgramme.SetMajorHitActGapForChecks(0); }
	}
}
