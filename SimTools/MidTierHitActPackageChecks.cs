using System;
using Godot;

/// <summary>--midtier-hit-act-package-check: every hook is the identity when off, and for Majors, Independents, the player and
/// cold acts; on, a MidTier's hot act reads due 8, the craft reference, and a zero (never a positive) fee clears.</summary>
public static class MidTierHitActPackageChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("MidTier hit-act package: " + why); }
		bool c0 = MidTierHitActPackage.Cadence, k0 = MidTierHitActPackage.Craft, m0 = MidTierHitActPackage.Material;
		int fd0 = HitActCadence.FieldDueWeeks;
		bool lane0 = HitActCadence.LaneEnabled, ret0 = HitActCadence.RetentionEnabled;
		try {
			HitActCadence.SetForChecks(true, false);
			HitActCadence.SetFieldDueForChecks(0);
			var mid = new AILabel { labelId = "mid", tier = LabelTier.MidTier };
			var indie = new AILabel { labelId = "ind", tier = LabelTier.Independent };
			var major = new AILabel { labelId = "maj", tier = LabelTier.Major };
			var player = new AILabel { labelId = "pl", tier = LabelTier.MidTier, isPlayerOwned = true };
			var dead = new AILabel { labelId = "dead", tier = LabelTier.MidTier, status = LabelStatus.Defunct };
			const int week = 100;
			var hot = new SimulatedArtist { artistId = "hot", hitCadenceLastTop40Week = week - 10 };
			var cold = new SimulatedArtist { artistId = "cold", hitCadenceLastTop40Week = week - HitActCadence.HotWindowWeeks - 1 };
			float r = FootprintCraftSoftening.CraftReference;

			MidTierHitActPackage.SetForChecks(false, false, false);
			Check(!MidTierHitActPackage.Enabled, "nothing named reads as off");
			Check(HitActCadence.DueWeeksFor(mid) == HitActCadence.DueWeeks, "off, a MidTier is due at 13");
			foreach (float q in new[] { 0f, .41f, .7f, r, .95f })
				Check(MidTierHitActPackage.CraftRead(q, ReleaseFormat.Single, mid, hot, week) == q, "off, craft is the input bit-for-bit");
			Check(!MidTierHitActPackage.ClearsZeroFee(mid, hot, 0f, week), "off, nothing clears");

			MidTierHitActPackage.SetForChecks(true, true, true);
			Check(HitActCadence.DueWeeksFor(mid) == MidTierHitActPackage.DueWeeks, "a MidTier is due at 8");
			Check(HitActCadence.DueWeeksFor(indie) == HitActCadence.DueWeeks && HitActCadence.DueWeeksFor(major) == HitActCadence.DueWeeks &&
				HitActCadence.DueWeeksFor(player) == HitActCadence.DueWeeks && HitActCadence.DueWeeksFor(null) == HitActCadence.DueWeeks,
				"Independents, Majors, the player and no label stay at 13");
			HitActCadence.SetFieldDueForChecks(6);
			Check(HitActCadence.DueWeeksFor(mid) == 6, "a shorter field interval is kept");
			HitActCadence.SetFieldDueForChecks(10);
			Check(HitActCadence.DueWeeksFor(mid) == 8 && HitActCadence.DueWeeksFor(indie) == 10, "the package takes the shorter interval at MidTiers only");
			HitActCadence.SetFieldDueForChecks(0);

			Check(MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, mid, hot, week) == r, "a MidTier hot act reads the reference");
			Check(MidTierHitActPackage.CraftRead(.9f, ReleaseFormat.Single, mid, hot, week) == .9f, "craft above the reference is never cut");
			Check(MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Album, mid, hot, week) == .6f, "albums are untouched");
			Check(MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, mid, cold, week) == .6f, "a cold act is untouched");
			Check(MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, indie, hot, week) == .6f &&
				MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, major, hot, week) == .6f &&
				MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, player, hot, week) == .6f, "other tiers and the player are untouched");
			Check(MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, mid, hot, 0) == .6f, "no live week, no lift");
			Check(FootprintCraftSoftening.Craft(.6f, ReleaseFormat.Single, mid, hot, week) == r, "the live craft read carries the lift");
			Check(FootprintCraftSoftening.Craft(.6f, ReleaseFormat.Single) == .6f, "the label-only read is untouched");

			Check(MidTierHitActPackage.ClearsZeroFee(mid, hot, 0f, week), "a zero fee clears for a MidTier hot act");
			Check(!MidTierHitActPackage.ClearsZeroFee(mid, hot, 1f, week) && !MidTierHitActPackage.ClearsZeroFee(mid, hot, float.NaN, week),
				"a positive or undefined fee never clears");
			Check(!MidTierHitActPackage.ClearsZeroFee(mid, cold, 0f, week) && !MidTierHitActPackage.ClearsZeroFee(mid, null, 0f, week), "cold or no act never clears");
			Check(!MidTierHitActPackage.ClearsZeroFee(indie, hot, 0f, week) && !MidTierHitActPackage.ClearsZeroFee(major, hot, 0f, week) &&
				!MidTierHitActPackage.ClearsZeroFee(player, hot, 0f, week) && !MidTierHitActPackage.ClearsZeroFee(dead, hot, 0f, week),
				"other tiers, the player and a closed label never clear");

			MidTierHitActPackage.SetForChecks(false, true, false);
			Check(HitActCadence.DueWeeksFor(mid) == HitActCadence.DueWeeks && !MidTierHitActPackage.ClearsZeroFee(mid, hot, 0f, week) &&
				MidTierHitActPackage.CraftRead(.6f, ReleaseFormat.Single, mid, hot, week) == r, "parts switch independently");
			GD.Print("MIDTIER_HIT_ACT_PACKAGE_CHECK passed");
		} finally {
			MidTierHitActPackage.SetForChecks(c0, k0, m0);
			HitActCadence.SetForChecks(lane0, ret0);
			HitActCadence.SetFieldDueForChecks(fd0);
		}
	}
}
