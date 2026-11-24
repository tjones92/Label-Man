using System;
using Godot;

/// <summary>--earned-head-capacity-check: identity when off and for Majors / the player; earned rate is R x records / 12,
/// never below the envelope, never above the Major envelope.</summary>
public static class EarnedHeadCapacityChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Earned head capacity: " + why); }
		float was = EarnedHeadCapacity.SinglesPerRecord;
		try {
			var mid = new AILabel { tier = LabelTier.MidTier };
			var indie = new AILabel { tier = LabelTier.Independent };
			var major = new AILabel { tier = LabelTier.Major };
			var player = new AILabel { tier = LabelTier.MidTier, isPlayerOwned = true };
			EarnedHeadCapacity.SetForChecks(0f);
			Check(!EarnedHeadCapacity.Enabled, "0 must read as off");
			foreach (float e in new[] { 0f, 0.37f, 2.1f, 10f })
				Check(EarnedHeadCapacity.Singles(e, true, 30, 10f) == e, "off must return the envelope bit-for-bit");
			Check(EarnedHeadCapacity.Singles(mid, 2.1f, 10f) == 2.1f, "off must leave a live label's envelope unchanged");

			EarnedHeadCapacity.SetForChecks(4f);
			Check(EarnedHeadCapacity.InScope(mid) && EarnedHeadCapacity.InScope(indie), "MidTier and Independent are in scope");
			Check(!EarnedHeadCapacity.InScope(major) && !EarnedHeadCapacity.InScope(player), "Majors and the player are out of scope");
			Check(EarnedHeadCapacity.Singles(major, 10f, 10f) == 10f, "a Major's envelope is untouched");
			Check(EarnedHeadCapacity.Singles(2.1f, false, 30, 10f) == 2.1f, "out of scope returns the envelope");
			Check(EarnedHeadCapacity.Singles(2.1f, true, 3, 10f) == 2.1f, "three records (rate 1.0) stay on a 2.1 envelope");
			Check(Math.Abs(EarnedHeadCapacity.Singles(2.1f, true, 15, 10f) - 5f) < 1e-5f, "15 records earn 5 a month");
			Check(EarnedHeadCapacity.Singles(2.1f, true, 60, 10f) == 10f, "the earned rate is capped at the Major envelope");
			Check(EarnedHeadCapacity.Singles(2.1f, true, 0, 10f) == 2.1f, "no records never cuts the envelope");
			GD.Print("EARNED_HEAD_CAPACITY_CHECK passed");
		} finally {
			EarnedHeadCapacity.SetForChecks(was);
		}
	}
}
