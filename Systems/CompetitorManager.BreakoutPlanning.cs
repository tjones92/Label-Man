using System;
using System.Linq;

public partial class CompetitorManager {
	// A Top 40 entry is observed demand, not an extra work entitlement. Keep the dated slot's identity and
	// original intention/expiry window; only its artist assignment and live work date may change.
	internal int PlanBreakoutFollowUps(AILabel label) {
		if (!LabelReleaseProgramme.BreakoutPlanningEnabled || !LabelReleaseProgramme.UsesWorkBudget(label) || ProgrammeState == null) return 0;
		int changed = 0;
		foreach (var artist in label.roster.Where(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) &&
			a.labelId == label.labelId && a.members?.Count > 0 &&
			HitActCadence.WeeksSinceTop40(a, pipelineWeek) <= HitActCadence.HotWindowWeeks)
			.OrderBy(a => a.hitCadenceLastTop40Week).ThenBy(a => a.artistId, StringComparer.Ordinal)) {
			var clock = ProgrammeClockFor(artist);
			// An already released follow-up satisfies this observed entry. Seeded pre-run hits are not new triggers.
			if (artist.hitCadenceLastTop40Week < 0 || clock.SingleWeek > artist.hitCadenceLastTop40Week) continue;
			int target = Math.Max(pipelineWeek + LabelReleaseProgramme.PreparationWeeks, clock.SingleWeek + LabelReleaseProgramme.SingleFloor);
			if (target > pipelineWeek + LabelReleaseProgramme.Horizon) continue;
			var singles = ProgrammeState.Items.Where(i => i.LabelId == label.labelId && i.Format == ReleaseFormat.Single &&
				LabelReleaseProgramme.IsOpen(i) && pipelineWeek - i.IntendedWeek <= LabelReleaseProgramme.Horizon).ToList();
			var own = singles.Where(i => i.ArtistId == artist.artistId && i.ContractSequence == artist.contractSequence)
				.OrderBy(i => i.DueWeek).ThenBy(i => i.Id, StringComparer.Ordinal).FirstOrDefault();
			// Retried cash/material failures must not be perpetually re-dated, re-reserved or duplicated.
			if (own?.Evidence?.Contains(";breakoutPlanning=1", StringComparison.Ordinal) == true) continue;
			var item = own ?? singles.Where(i => i.IntendedWeek >= target && i.PreparedRecord == null && i.PaidProduction == 0 &&
				i.RecordId == null && i.MaterialSearch == null && i.Source != "hit-follow-up" &&
				i.Evidence?.Contains(";breakoutPlanning=1", StringComparison.Ordinal) != true)
				.OrderBy(i => i.IntendedWeek).ThenBy(i => i.Score).ThenBy(i => i.Id, StringComparer.Ordinal).FirstOrDefault();
			if (item == null) continue; // No existing work to allocate; retry against the rolling horizon next week.
			string previousArtist = item.ArtistId;
			bool reassigned = own == null;
			if (reassigned) {
				item.ArtistId = artist.artistId;
				item.ContractSequence = artist.contractSequence;
			}
			item.Source = "hit-follow-up";
			item.Score = Math.Max(10, item.Score);
			item.Evidence += $";breakoutPlanning=1;triggerTop40Week={artist.hitCadenceLastTop40Week};previousArtist={previousArtist}";
			// A prepared or already due intention can proceed sooner than the normal new-session lead.
			int due = Math.Max(clock.SingleWeek + LabelReleaseProgramme.SingleFloor, Math.Min(item.DueWeek, target));
			item.ReadyWeek = Math.Min(item.ReadyWeek, due - 1);
			RescheduleProgrammeItem(item, due, item.State, reassigned ? "breakout-budget-slot-reassigned" : "breakout-budget-follow-up-reviewed");
			changed++;
		}
		return changed;
	}
}
