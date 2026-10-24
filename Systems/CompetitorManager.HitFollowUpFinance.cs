using System;
using System.Linq;
using Godot;

/// <summary>
/// --hit-follow-up-finance (requires the slate and capacity). Two parts, both limited to non-Major AI labels and to acts
/// with a Top 40 within <see cref="HitActCadence.HotWindowWeeks"/>:
/// <list type="bullet">
/// <item>Throughput: each such act adds <see cref="LabelReleaseProgramme.HitFollowUpSinglesPerYear"/> single slots a year
/// to the label's dated planning, preparation and issue work. Planned extra slots are open only to hot acts. They use their
/// own keyed roll domain, so the base slots and their draws are unchanged.</item>
/// <item>Funding: when such an act's slot is short of unreserved cash, the label factors wholesale receivables, latest due
/// first, at the player's factoring terms until the shortfall is covered. The discount is a write-off. Receivables are
/// billed by house and week, not by record, so the act's hit gates the sale but cannot be traced to its own invoices.</item>
/// </list>
/// Adds no saved state; factoring changes only the existing receivable ledger, cash and write-offs.
/// </summary>
public partial class CompetitorManager {
	private const uint HitFollowUpRollDomain = 3;
	public static int HitFollowUpFactorings { get; private set; }
	public static float HitFollowUpFactoredFace { get; private set; }
	public static float HitFollowUpFactoredCash { get; private set; }
	internal int ProgrammeWeekForChecks => pipelineWeek;
	internal static void ResetHitFollowUpCountersForChecks() { HitFollowUpFactorings = 0; HitFollowUpFactoredFace = HitFollowUpFactoredCash = 0; }

	internal bool HitFollowUpFinanceApplies(AILabel label) =>
		LabelReleaseProgramme.HitFollowUpFinanceEnabled && label != null && !label.isPlayerOwned && label.tier != LabelTier.Major;

	internal bool IsHotProgrammeAct(SimulatedArtist artist, int week) =>
		artist != null && HitActCadence.WeeksSinceTop40(artist, week) <= HitActCadence.HotWindowWeeks;

	internal float HitFollowUpWeeklyRate(AILabel label, int week) {
		if (!HitFollowUpFinanceApplies(label)) return 0f;
		int hot = label.roster.Count(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members?.Count is > 0 && IsHotProgrammeAct(a, week));
		return hot * LabelReleaseProgramme.HitFollowUpSinglesPerYear / 52f * LabelReleaseProgramme.StatusFactor(label.status);
	}

	internal int HitFollowUpSlots(AILabel label, int week) {
		float rate = HitFollowUpWeeklyRate(label, week);
		return rate <= 0 ? 0 : LabelReleaseProgramme.Slots(rate, ProgrammeRoll(label, null, "hit-follow-up-finance", week, HitFollowUpRollDomain));
	}

	/// <summary>Factor enough receivables to cover <paramref name="shortfall"/>; returns the cash raised.</summary>
	internal float FactorForHitFollowUp(AILabel label, SimulatedArtist artist, float shortfall) {
		// A cent over the need, so float rounding cannot leave a sale that still fails the reservation.
		shortfall += .01f;
		if (!HitFollowUpFinanceApplies(label) || !IsHotProgrammeAct(artist, pipelineWeek) || shortfall <= .01f ||
			label.wholesaleReceivables.Count == 0) return 0f;
		float available = label.wholesaleReceivables.Sum(r => r.Amount * LabelReleaseProgramme.FactorRate(HouseReliability(r.DistributorId)));
		// Selling only part of what is needed buys nothing: the slot would still be short.
		if (available + .01f < shortfall) return 0f;
		float raised = 0f, face = 0f;
		var order = Enumerable.Range(0, label.wholesaleReceivables.Count)
			.OrderByDescending(i => label.wholesaleReceivables[i].DueWeek).ThenBy(i => label.wholesaleReceivables[i].DistributorId, StringComparer.Ordinal).ToList();
		var emptied = new System.Collections.Generic.List<int>();
		foreach (int index in order) {
			if (raised >= shortfall) break;
			var receivable = label.wholesaleReceivables[index];
			float rate = LabelReleaseProgramme.FactorRate(HouseReliability(receivable.DistributorId));
			float sold = Math.Min(receivable.Amount, (shortfall - raised) / rate);
			receivable.Amount -= sold;
			label.wholesaleReceivables[index] = receivable;
			if (receivable.Amount <= .01f) { sold += receivable.Amount; emptied.Add(index); }
			face += sold; raised += sold * rate;
		}
		foreach (int index in emptied.OrderByDescending(i => i)) label.wholesaleReceivables.RemoveAt(index);
		label.outstandingWholesaleReceivables = Mathf.Max(0f, label.outstandingWholesaleReceivables - face);
		label.lifetimeWholesaleWriteOffs += face - raised;
		// Booked as a collection would be: record receipts, and this month's revenue.
		LabelCompanyAccounts.ChangeCash(label, raised, LabelCashKind.RecordReceipts);
		label.monthlyRevenue += raised;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) financials.lastMonthRevenue += raised;
		HitFollowUpFactorings++; HitFollowUpFactoredFace += face; HitFollowUpFactoredCash += raised;
		GD.Print(FormattableString.Invariant($"HIT_FOLLOW_UP_FINANCE week={pipelineWeek} label={label.labelId} tier={label.tier} artist={artist.artistId} face={face:F2} cash={raised:F2} shortfall={shortfall:F2}"));
		return raised;
	}

	private float? HouseReliability(string distributorId) =>
		independentDistributors.FirstOrDefault(d => d.distributorId == distributorId)?.reliability;
}
