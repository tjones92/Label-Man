using System;
using System.Collections.Generic;
using Godot;

// Tests the timing that weekly snapshots could not establish: a successful decision is observed while the
// label is still Independent with its four-quarter streak, then promotion resets the streak. Failed decisions
// are observed too. Uses the actual promotion path without advancing a simulation world.
public static class MidTierPromotionAuditChecks {
	public static void Run() {
		LabelLifecycleManager manager = LabelLifecycleManager.Instance;
		int decisions = 0, promotions = 0;
		AILabel label = new() {
			labelId = "mid_tier_audit_check", labelName = "Promotion audit check", tier = LabelTier.Independent,
			status = LabelStatus.Stable, monthsActive = 24, sustainedCapabilityQuarters = 3,
			ownedReach = .65f, cashReserves = 100_000f, lastMonthlyProfit = 100f,
			roster = new List<SimulatedArtist>()
		};
		for (int i = 0; i < 6; i++) label.roster.Add(new SimulatedArtist { artistId = $"mid_tier_audit_artist_{i}" });
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("MidTier promotion audit: " + why); }
		void Observe(AILabel observed, int charting, bool eligible) {
			if (observed != label) return;
			decisions++;
			Check(observed.tier == LabelTier.Independent && charting == 5, "decision captures the Independent tier and charting input");
			Check(observed.sustainedCapabilityQuarters == (eligible ? 4 : 3), "capability streak is captured before a promotion can reset it");
		}
		void Promoted(AILabel observed, LabelTier from, LabelTier to) {
			if (observed != label) return;
			promotions++;
			Check(decisions == 2 && from == LabelTier.Independent && to == LabelTier.MidTier,
				"successful gate evidence precedes the transition event");
			Check(observed.sustainedCapabilityQuarters == 0, "transition event follows the streak reset");
		}
		manager.OnMidTierPromotionEvaluated += Observe;
		manager.OnLabelPromoted += Promoted;
		try {
			Check(!manager.TryPromoteIndependentLabel(label, 5) && decisions == 1 && promotions == 0,
				"a blocked review is observed without promoting");
			label.sustainedCapabilityQuarters = 4;
			Check(manager.TryPromoteIndependentLabel(label, 5) && decisions == 2 && promotions == 1,
				"an eligible review produces exactly one promotion");
			GD.Print("MID_TIER_PROMOTION_AUDIT_CHECK passed");
		} finally {
			manager.OnMidTierPromotionEvaluated -= Observe;
			manager.OnLabelPromoted -= Promoted;
			label.Dispose();
		}
	}
}
