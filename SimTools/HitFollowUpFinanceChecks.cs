using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>--hit-follow-up-finance-check: off is inert; scope (non-Major, hot acts); dedicated slot rate; factoring
/// terms, order and accounting identity; the reservation path; insufficient receivables sell nothing.</summary>
public static class HitFollowUpFinanceChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Hit follow-up finance: " + message); checks++; }
		var manager = CompetitorManager.Instance;
		bool allocation = LabelReleaseProgramme.AllocationEnabled, capacity = LabelReleaseProgramme.CapacityEnabled;
		bool finance = LabelReleaseProgramme.HitFollowUpFinanceEnabled;
		float perYear = LabelReleaseProgramme.HitFollowUpSinglesPerYear;
		var previous = manager.ProgrammeState;
		var label = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive && l.tier != LabelTier.Major &&
			l.roster.Any(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members?.Count > 0));
		var major = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive && l.tier == LabelTier.Major);
		var artist = label.roster.First(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members?.Count > 0);
		var saved = (label.cashReserves, Receivables: label.wholesaleReceivables.ToList(), label.outstandingWholesaleReceivables,
			label.lifetimeWholesaleWriteOffs, label.monthlyRevenue, label.roster.ToDictionary(a => a, a => a.hitCadenceLastTop40Week));
		int week = manager.ProgrammeWeekForChecks;
		try {
			LabelReleaseProgramme.SetForChecks(true, true);
			manager.SetProgrammeForChecks(new ReleaseProgrammeState());
			foreach (var a in label.roster) a.hitCadenceLastTop40Week = week - 200;
			HitActCadence.OnTop40(artist, week - 5);
			void Ledger(params (int Due, float Amount)[] rows) {
				label.wholesaleReceivables = rows.Select(r => new WholesaleReceivable(r.Due, "check-house-none", r.Amount)).ToList();
				label.outstandingWholesaleReceivables = rows.Sum(r => r.Amount);
			}

			LabelReleaseProgramme.SetHitFollowUpFinanceForChecks(false);
			Ledger((week + 4, 100), (week + 8, 200));
			Check(!manager.HitFollowUpFinanceApplies(label) && manager.HitFollowUpSlots(label, week) == 0, "off plans no extra slots");
			Check(manager.FactorForHitFollowUp(label, artist, 50) == 0 && label.outstandingWholesaleReceivables == 300, "off sells no receivables");

			LabelReleaseProgramme.SetHitFollowUpFinanceForChecks(true, 2);
			Check(!manager.HitFollowUpFinanceApplies(major) && manager.HitFollowUpWeeklyRate(major, week) == 0, "Majors are excluded");
			float expected = 2f / 52f * LabelReleaseProgramme.StatusFactor(label.status);
			Check(Math.Abs(manager.HitFollowUpWeeklyRate(label, week) - expected) < 1e-6, "one hot act adds two singles a year");
			Check(manager.HitFollowUpSlots(label, week) == manager.HitFollowUpSlots(label, week), "slot rolls are keyed and replay");
			artist.hitCadenceLastTop40Week = week - HitActCadence.HotWindowWeeks - 1;
			Check(manager.HitFollowUpWeeklyRate(label, week) == 0, "an act past the hot window adds nothing");
			Check(manager.FactorForHitFollowUp(label, artist, 50) == 0, "a cold act cannot factor");
			HitActCadence.OnTop40(artist, week - 5);
			Check(Math.Abs(LabelReleaseProgramme.FactorRate(null) - .775f) < 1e-6 && LabelReleaseProgramme.FactorRate(1) == .85f &&
				LabelReleaseProgramme.FactorRate(0) == .70f, "the player's factoring terms");

			CompetitorManager.ResetHitFollowUpCountersForChecks();
			float cash0 = label.cashReserves, writeOff0 = label.lifetimeWholesaleWriteOffs;
			float raised = manager.FactorForHitFollowUp(label, artist, 50);
			float face = 300 - label.outstandingWholesaleReceivables;
			Check(raised >= 50 && raised < 50.1f && Math.Abs(label.cashReserves - cash0 - raised) < .01f, "raises the shortfall in cash");
			Check(label.wholesaleReceivables.Single(r => r.DueWeek == week + 4).Amount == 100, "latest-due invoice is sold first");
			Check(Math.Abs(face * .775f - raised) < .01f && Math.Abs(label.lifetimeWholesaleWriteOffs - writeOff0 - (face - raised)) < .01f,
				"the discount is booked as a write-off");
			Check(Math.Abs(label.wholesaleReceivables.Sum(r => r.Amount) - label.outstandingWholesaleReceivables) < .01f, "ledger and total agree");
			Check(CompetitorManager.HitFollowUpFactorings == 1 && Math.Abs(CompetitorManager.HitFollowUpFactoredFace - face) < .01f, "counters");

			Ledger((week + 4, 10));
			float cash1 = label.cashReserves;
			Check(manager.FactorForHitFollowUp(label, artist, 50) == 0 && label.cashReserves == cash1 && label.outstandingWholesaleReceivables == 10,
				"receivables that cannot cover the shortfall are not sold");
			Ledger((week + 4, 40), (week + 9, 40));
			raised = manager.FactorForHitFollowUp(label, artist, 61);
			Check(raised >= 61 && label.wholesaleReceivables.Count == 1 && label.wholesaleReceivables[0].DueWeek == week + 4,
				"an emptied invoice leaves the ledger");

			// The reservation path: short of unreserved cash, the hot act's slot is funded by a sale; a cold act's is not.
			var item = new ReleaseSlateItem { Id = "hff-check", LabelId = label.labelId, ArtistId = artist.artistId, Format = ReleaseFormat.Single,
				State = ReleaseSlateState.Proposed, IntendedWeek = week + 2, DueWeek = week + 2, ReadyWeek = week + 1 };
			label.cashReserves = EconomicLabelLifecycle.CommitmentReserve(label, false);
			Ledger((week + 6, 50000));
			Check(manager.ReserveSlate(label, artist, item, TimeManager.Instance.CurrentDate, out _) && item.ReservedMarketing > 0 &&
				label.outstandingWholesaleReceivables < 50000, "a cash-short hot follow-up is funded by factoring");
			artist.hitCadenceLastTop40Week = week - 200;
			label.cashReserves = EconomicLabelLifecycle.CommitmentReserve(label, false);
			Ledger((week + 6, 50000));
			var cold = new ReleaseSlateItem { Id = "hff-cold", LabelId = label.labelId, ArtistId = artist.artistId, Format = ReleaseFormat.Single,
				State = ReleaseSlateState.Proposed, IntendedWeek = week + 2, DueWeek = week + 2, ReadyWeek = week + 1 };
			Check(!manager.ReserveSlate(label, artist, cold, TimeManager.Instance.CurrentDate, out string shortfall) &&
				shortfall == "insufficient-unreserved-cash" && label.outstandingWholesaleReceivables == 50000, "a cold act's slot stays short");
			GD.Print($"HIT_FOLLOW_UP_FINANCE_CHECK passed checks={checks}");
		} finally {
			label.cashReserves = saved.cashReserves; label.wholesaleReceivables = saved.Receivables;
			label.outstandingWholesaleReceivables = saved.outstandingWholesaleReceivables;
			label.lifetimeWholesaleWriteOffs = saved.lifetimeWholesaleWriteOffs; label.monthlyRevenue = saved.monthlyRevenue;
			foreach (var pair in saved.Item6) pair.Key.hitCadenceLastTop40Week = pair.Value;
			manager.SetProgrammeForChecks(previous); LabelReleaseProgramme.SetForChecks(allocation, capacity);
			LabelReleaseProgramme.SetHitFollowUpFinanceForChecks(finance, perYear);
			CompetitorManager.ResetHitFollowUpCountersForChecks();
		}
	}
}
