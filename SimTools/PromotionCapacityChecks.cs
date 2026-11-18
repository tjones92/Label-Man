using System;
using System.Collections.Generic;
using Godot;

/// <summary>--promotion-capacity-check (run from --major-volume-check): plug slots are off and route nothing unless named;
/// on, hit acts take slots first and may buy one more, other singles need a free unreserved slot and a read at or above the
/// label's median, and every routine issue reads the reference promotion.</summary>
public static class PromotionCapacityChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Promotion capacity: " + why); }
		AILabel Lab(LabelTier tier, float cash = 1e6f, bool player = false) =>
			new AILabel { labelId = "lab_" + Guid.NewGuid().ToString("N"), tier = tier, isPlayerOwned = player, cashReserves = cash, marketingPower = 1f, roster = new List<SimulatedArtist>() };
		SimulatedArtist Act(CareerState state, int hits = 0) => new SimulatedArtist { artistId = "a" + Guid.NewGuid().ToString("N"), careerState = state, consecutiveHits = hits };
		int month = 1960 * 12 + 1;
		try {
			PromotionCapacity.SetForChecks(-1, -1);
			Check(!PromotionCapacity.Enabled && PromotionCapacity.SlotsFor(Lab(LabelTier.Major)) == -1, "off: no label has slots");
			Check(PromotionCapacity.Decide(Lab(LabelTier.Major), Act(CareerState.Star), "r0", .7f, month, null) == PromotionCapacity.Outcome.NotRouted,
				"off: nothing is routed");
			Check(!PromotionCapacity.IsRoutine("r0") && !LabelBlindLaunch.InFactor && !LabelBlindLaunch.InExpose, "off: no routine record or scope");

			PromotionCapacity.SetForChecks(2, -1);
			Check(PromotionCapacity.SlotsFor(Lab(LabelTier.Major)) == 2 && PromotionCapacity.SlotsFor(Lab(LabelTier.MidTier)) == -1 &&
				PromotionCapacity.SlotsFor(Lab(LabelTier.Independent)) == -1 && PromotionCapacity.SlotsFor(Lab(LabelTier.Major, player: true)) == -1,
				"major switch: only AI Majors have slots");
			Check(PromotionCapacity.HitAct(Act(CareerState.Star)) && PromotionCapacity.HitAct(Act(CareerState.Established, 1)) &&
				!PromotionCapacity.HitAct(Act(CareerState.Established)) && !PromotionCapacity.HitAct(null), "hit act: star or last run a Top 40");

			// No hit acts on the roster: no reserve; no bar before six reads. Two plugs, then routine.
			var major = Lab(LabelTier.Major);
			Check(PromotionCapacity.Decide(major, Act(CareerState.NewSigning), "n1", .5f, month, null) == PromotionCapacity.Outcome.Plugged, "first non-hit single plugs");
			Check(PromotionCapacity.Decide(major, Act(CareerState.NewSigning), "n2", .5f, month, null) == PromotionCapacity.Outcome.Plugged, "second non-hit single plugs");
			Check(PromotionCapacity.Decide(major, Act(CareerState.NewSigning), "n3", .9f, month, null) == PromotionCapacity.Outcome.Routine &&
				PromotionCapacity.IsRoutine("n3") && !PromotionCapacity.IsRoutine("n1"), "slots spent: routine, and recorded as routine");
			float charged = 0f;
			Check(PromotionCapacity.Decide(major, Act(CareerState.Star), "h1", .5f, month, p => charged += p) == PromotionCapacity.Outcome.BoughtPlug &&
				Math.Abs(charged - major.GetStandardMarketingBudget()) < .01f, "slots spent: a hit act buys a plug at the standard marketing budget");
			var poor = Lab(LabelTier.Major, cash: 100f);
			PromotionCapacity.Decide(poor, Act(CareerState.Star), "p1", .5f, month, null);
			PromotionCapacity.Decide(poor, Act(CareerState.Star), "p2", .5f, month, null);
			Check(PromotionCapacity.Decide(poor, Act(CareerState.Star), "p3", .5f, month, p => charged += 1e9f) == PromotionCapacity.Outcome.Routine &&
				charged < 1e8f, "a label that cannot cover four plugs does not buy one");
			Check(PromotionCapacity.Decide(major, Act(CareerState.NewSigning), "n4", .9f, month + 1, null) == PromotionCapacity.Outcome.Plugged,
				"a new month restores the slots");

			// Reserve: four hit acts on the roster hold back two of two slots, so a non-hit single goes routine while a hit act plugs.
			PromotionCapacity.SetForChecks(2, -1);
			var stars = Lab(LabelTier.Major);
			for (int i = 0; i < 4; i++) stars.roster.Add(Act(CareerState.Star));
			Check(PromotionCapacity.Decide(stars, Act(CareerState.NewSigning), "s1", .9f, month, null) == PromotionCapacity.Outcome.Routine,
				"reserved slots go to hit acts");
			Check(PromotionCapacity.Decide(stars, Act(CareerState.Star), "s2", .4f, month, null) == PromotionCapacity.Outcome.Plugged,
				"a hit act takes a reserved slot whatever the read");

			// Bar: after six reads a non-hit single needs a read at or above the median.
			PromotionCapacity.SetForChecks(50, -1);
			var bar = Lab(LabelTier.Major);
			for (int i = 0; i < 6; i++) PromotionCapacity.Decide(bar, Act(CareerState.NewSigning), "b" + i, .70f, month, null);
			Check(PromotionCapacity.Decide(bar, Act(CareerState.NewSigning), "low", .50f, month, null) == PromotionCapacity.Outcome.Routine,
				"a read well below the median goes routine");
			Check(PromotionCapacity.Decide(bar, Act(CareerState.NewSigning), "high", .85f, month, null) == PromotionCapacity.Outcome.Plugged,
				"a read well above the median plugs");
			Check(Math.Abs(PromotionCapacity.Read("x", .7f) - .7f) <= PromotionCapacity.ReadError + 1e-6f &&
				PromotionCapacity.Read("x", .7f) == PromotionCapacity.Read("x", .7f), "the read error is keyed and bounded");

			PromotionCapacity.SetForChecks(-1, 3);
			Check(PromotionCapacity.SlotsFor(Lab(LabelTier.MidTier)) == 3 && PromotionCapacity.SlotsFor(Lab(LabelTier.Major)) == -1,
				"midtier switch: only AI MidTiers have slots");

			// A routine scope turns on exactly the factor, stock and shared-exposure reads.
			PromotionCapacity.SetForChecks(1, -1);
			using (new PromotionCapacity.RoutineScope(true))
				Check(LabelBlindLaunch.InFactor && LabelBlindLaunch.InStock && LabelBlindLaunch.InExpose && LabelBlindLaunch.InSharedExpose &&
					!LabelBlindLaunch.InCity && !LabelBlindLaunch.InHold, "routine scope: factor, stock, shared exposure; not city or hold");
			Check(!LabelBlindLaunch.InFactor && !LabelBlindLaunch.InExpose, "routine scope closes");
			GD.Print("PROMOTION_CAPACITY_CHECK passed");
		} finally { PromotionCapacity.SetForChecks(-1, -1); }
	}
}
