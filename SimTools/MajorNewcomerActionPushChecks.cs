using System;
using Godot;

/// <summary>--major-newcomer-action-push-check: identity when off and out of scope; a waiting Major newcomer single reads
/// Ref + F x (push - Ref); any Hot 100 week or regional breakout restores the full push.</summary>
public static class MajorNewcomerActionPushChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Major newcomer action push: " + why); }
		float was = MajorNewcomerActionPush.Factor;
		try {
			var major = new AILabel { tier = LabelTier.Major };
			var mid = new AILabel { tier = LabelTier.MidTier };
			var player = new AILabel { tier = LabelTier.Major, isPlayerOwned = true };
			RecordRuntimeData Rec(CareerState career, ReleaseFormat format = ReleaseFormat.Single) =>
				new RecordRuntimeData { baseRecord = new Record { format = format }, launchCareerState = career };
			var fresh = Rec(CareerState.NewSigning);
			const float r = MajorNewcomerActionPush.ImpactRef;

			MajorNewcomerActionPush.SetForChecks(1f);
			foreach (float p in new[] { 0f, .2f, r, .55f, .9f }) Check(MajorNewcomerActionPush.Apply(p, fresh, major) == p, "off returns the push bit-for-bit");

			MajorNewcomerActionPush.SetForChecks(0f);
			Check(MajorNewcomerActionPush.Apply(.55f, fresh, major) == r, "F = 0: a waiting newcomer reads the reference");
			Check(MajorNewcomerActionPush.Apply(.30f, fresh, major) == .30f, "a push at or below the reference is never raised or cut");
			MajorNewcomerActionPush.SetForChecks(.5f);
			Check(Math.Abs(MajorNewcomerActionPush.Apply(.56f, fresh, major) - (r + .5f * (.56f - r))) < 1e-6f, "F = 0.5 moves halfway");
			MajorNewcomerActionPush.SetForChecks(0f);
			Check(MajorNewcomerActionPush.Apply(.55f, Rec(CareerState.Established), major) == .55f &&
				MajorNewcomerActionPush.Apply(.55f, Rec(CareerState.Rising), major) == .55f, "acts past NewSigning keep the full push");
			Check(MajorNewcomerActionPush.Apply(.55f, Rec(CareerState.NewSigning, ReleaseFormat.Album), major) == .55f, "albums are untouched");
			Check(MajorNewcomerActionPush.Apply(.55f, fresh, mid) == .55f && MajorNewcomerActionPush.Apply(.55f, fresh, player) == .55f &&
				MajorNewcomerActionPush.Apply(.55f, fresh, null) == .55f, "other tiers, the player and no label are untouched");
			var charted = Rec(CareerState.NewSigning); charted.weeksOnChart = 1;
			var charting = Rec(CareerState.NewSigning); charting.currentPosition = 88;
			var breakout = Rec(CareerState.NewSigning); breakout.regionalBreakoutCount = 1;
			Check(MajorNewcomerActionPush.Apply(.55f, charted, major) == .55f && MajorNewcomerActionPush.Apply(.55f, charting, major) == .55f &&
				MajorNewcomerActionPush.Apply(.55f, breakout, major) == .55f, "action restores the full push");
			GD.Print("MAJOR_NEWCOMER_ACTION_PUSH_CHECK passed");
		} finally { MajorNewcomerActionPush.SetForChecks(was); }
	}
}
