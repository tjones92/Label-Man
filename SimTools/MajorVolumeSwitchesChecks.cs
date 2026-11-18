using System;
using Godot;

/// <summary>--major-volume-check: both switches are identity when off; on, only AI Major-tier labels are exempt from the
/// capital-risk test and the material hold, and a Major's launch target and roster cap rise to the opening roster.</summary>
public static class MajorVolumeSwitchesChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Major volume: " + why); }
		bool gate = MajorVolumeSwitches.GateExemption; int roster = MajorVolumeSwitches.OpeningRoster;
		int pool0 = MajorVolumeSwitches.SlatePoolSize; float craft0 = MajorVolumeSwitches.NewcomerCraftCap;
		try {
			AILabel Lab(LabelTier tier, int cap, bool player = false) => new AILabel { tier = tier, maxRosterSize = cap, isPlayerOwned = player };

			MajorVolumeSwitches.SetForChecks(false, 0);
			var major = Lab(LabelTier.Major, 40);
			Check(!MajorVolumeSwitches.Exempt(major), "off: no Major is exempt");
			Check(MajorVolumeSwitches.OpeningTarget(major, 29) == 29 && major.maxRosterSize == 40, "off: the drawn target and cap are unchanged");

			MajorVolumeSwitches.SetForChecks(true, 0);
			Check(MajorVolumeSwitches.Exempt(Lab(LabelTier.Major, 40)), "on: an AI Major is exempt");
			Check(!MajorVolumeSwitches.Exempt(Lab(LabelTier.MidTier, 40)) && !MajorVolumeSwitches.Exempt(Lab(LabelTier.Independent, 10)) &&
				!MajorVolumeSwitches.Exempt(Lab(LabelTier.Major, 40, true)) && !MajorVolumeSwitches.Exempt(null),
				"other tiers, the player and no label keep the gates");

			MajorVolumeSwitches.SetForChecks(false, 58);
			var small = Lab(LabelTier.Major, 40);
			Check(MajorVolumeSwitches.OpeningTarget(small, 29) == 58 && small.maxRosterSize == 58, "a Major below N opens at N with its cap raised");
			var big = Lab(LabelTier.Major, 90);
			Check(MajorVolumeSwitches.OpeningTarget(big, 70) == 70 && big.maxRosterSize == 90, "a Major already above N keeps its draw and cap");
			var mid = Lab(LabelTier.MidTier, 20);
			Check(MajorVolumeSwitches.OpeningTarget(mid, 12) == 12 && mid.maxRosterSize == 20, "non-Majors are untouched");
			Check(!MajorVolumeSwitches.Exempt(Lab(LabelTier.Major, 40)), "the roster switch alone exempts nothing");
			Check(!MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.Major, 40)), "the roster switch alone caps no slate pool");

			MajorVolumeSwitches.SetForChecks(false, 0, 3);
			Check(MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.Major, 40)), "pool: an AI Major is capped");
			Check(!MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.MidTier, 40)) && !MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.Major, 40, true)) &&
				!MajorVolumeSwitches.SlatePoolCapped(null) && !MajorVolumeSwitches.Exempt(Lab(LabelTier.Major, 40)), "pool: other tiers and the player are uncapped; no gate exemption");
			var acts = new[] { "a", "b", "c", "d", "e", "f", "g" };
			var rolls = new System.Collections.Generic.Dictionary<string, double> { ["a"] = .9, ["b"] = .1, ["c"] = .5, ["d"] = .3, ["e"] = .05, ["f"] = .3, ["g"] = .99 };
			var pool = MajorVolumeSwitches.SlatePool(acts, a => a == "g", a => rolls[a], a => a);
			Check(string.Join("", pool) == "bdeg", "pool: hot acts kept, N lowest rolls kept with ties by id, original order");
			Check(string.Join("", MajorVolumeSwitches.SlatePool(new[] { "x", "y", "z", "h" }, a => a == "h", a => 0, a => a)) == "xyzh", "pool: no cut when cold acts ≤ N");
			MajorVolumeSwitches.SetForChecks(false, 0, 0);
			Check(!MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.Major, 40)), "pool off: no Major is capped");

			SimulatedArtist Act(CareerState state) => new SimulatedArtist { careerState = state };
			Check(MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), Act(CareerState.NewSigning)) == .9f,
				"craft off: a Major newcomer reads its label craft");
			MajorVolumeSwitches.SetForChecks(false, 0, 0, .65f);
			Check(MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), Act(CareerState.NewSigning)) == .65f &&
				MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), Act(CareerState.Unsigned)) == .65f,
				"craft: a Major's NewSigning or Unsigned single reads C");
			Check(MajorVolumeSwitches.NewcomerCraft(.6f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), Act(CareerState.NewSigning)) == .6f,
				"craft: a cap above the label's craft lifts nothing");
			Check(MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), Act(CareerState.Rising)) == .9f &&
				MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Album, Lab(LabelTier.Major, 40), Act(CareerState.NewSigning)) == .9f &&
				MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.MidTier, 40), Act(CareerState.NewSigning)) == .9f &&
				MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40, true), Act(CareerState.NewSigning)) == .9f &&
				MajorVolumeSwitches.NewcomerCraft(.9f, ReleaseFormat.Single, Lab(LabelTier.Major, 40), null) == .9f,
				"craft: established acts, albums, other tiers, the player and no act are untouched");
			Check(!MajorVolumeSwitches.SlatePoolCapped(Lab(LabelTier.Major, 40)) && !MajorVolumeSwitches.Exempt(Lab(LabelTier.Major, 40)),
				"craft alone caps no pool and exempts nothing");
			MajorVolumeSwitches.SetForChecks(false, 0, 0, 0f);
			GD.Print("MAJOR_VOLUME_CHECK passed");
		} finally { MajorVolumeSwitches.SetForChecks(gate, roster, pool0, craft0); }
	}
}
