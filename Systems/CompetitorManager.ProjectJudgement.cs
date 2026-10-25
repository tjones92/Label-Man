using System;
using System.Collections.Generic;

/// <summary>The evidence available at a single's latest preparation review. No future demand or fit draw.</summary>
public sealed class ProgrammeProjectReview {
	public int Week;
	public string CandidateId, Action;
	public float Perceived, RoutineRead, Uncertainty;
	public bool UseSearchedMaterial;
}

public partial class CompetitorManager {
	public event Action<ReleaseSlateItem, int> OnProgrammeProjectReview;
	internal bool HasProjectJudgementState => ProgrammeState?.Items.Exists(i => i.ProjectReview != null) == true;

	// The same standard for incumbents and prospects: is there heard material worth this act's session?
	// This compares the label's imperfect reads, not hidden realized quality or a universal hit threshold.
	internal static float RoutineProjectRead(AILabel label, SimulatedArtist artist, int year, out float band) {
		var routine = AiRepertoireService.MeanOwnOriginal(label, artist, year, out _);
		routine.Id = "routine-session|" + artist.artistId;
		routine.Readiness = ShadowReadiness.Recordable;
		routine.EvidenceVersion = 1;
		return AiRepertoireService.Read(routine, label, artist, out band);
	}

	// A shortfall the label cannot tell from its own read error is not evidence of weaker material. Each read errs
	// uniformly within its half-width; this is one standard deviation of the difference of two independent such errors.
	internal static float ComparisonUncertainty(float halfWidth, float otherHalfWidth) =>
		MathF.Sqrt((halfWidth * halfWidth + otherHalfWidth * otherHalfWidth) / 3f);

	internal static bool WorthRecording(float perceived, float routineRead, float uncertainty = 0f) =>
		float.IsFinite(perceived) && float.IsFinite(routineRead) && float.IsFinite(uncertainty) && perceived >= 0 &&
		perceived >= routineRead - Math.Max(0f, uncertainty);

	// The intention's fixed window is closing: a session now keeps the ordinary preparation lead before the window's last
	// issue week, so the label stops holding for stronger material and records the act's own session instead. Once no
	// later week can issue, a session would only shelve a paid master, so the project is left to expire.
	internal static bool LastProjectReview(ReleaseSlateItem item, int week) =>
		week >= item.IntendedWeek + LabelReleaseProgramme.Horizon - LabelReleaseProgramme.PreparationWeeks &&
		week < item.IntendedWeek + LabelReleaseProgramme.Horizon;

	internal bool ReviewProgrammeProject(AILabel label, SimulatedArtist artist, GameDate date, ReleaseSlateItem item) {
		if (!LabelReleaseProgramme.ProjectJudgementEnabled || item.Format != ReleaseFormat.Single) return true;
		var shortlist = AiRepertoireService.Judge(label, artist, date.year, pipelineWeek);
		float routine = RoutineProjectRead(label, artist, date.year, out float routineBand);
		float perceived = shortlist.BestPerceived;
		// Two shortlist reads of one act share the label's bias for that act, so only their independent errors compare.
		float independent = 1f - ReleaseJudgement.Parameters.BiasShare;
		float uncertainty = ComparisonUncertainty(shortlist.ReadBand * independent, routineBand * independent);
		bool searched = false;
		if (item.MaterialSearch?.State == ProgrammeMaterialState.Ready) {
			var song = CompositionCatalogService.GetSong(item.MaterialSearch.SongId);
			float searchedBand = 0;
			float heard = song == null ? -1 : ProgrammeMaterialRead(label, artist, song, item.MaterialSearch.Professional, date.year, true, out searchedBand);
			// A searched song's read has no shared act bias, so the whole of both bands is in play.
			if (heard > perceived) { perceived = heard; searched = true; uncertainty = ComparisonUncertainty(searchedBand, routineBand); }
		}
		bool last = LastProjectReview(item, pipelineWeek);
		bool pending = !last && item.MaterialSearch?.State == ProgrammeMaterialState.Pending;
		bool closed = pipelineWeek >= item.IntendedWeek + LabelReleaseProgramme.Horizon;
		bool record = !pending && !closed && WorthRecording(perceived, routine, uncertainty);
		bool own = !pending && !record && last;
		item.ProjectReview = new ProgrammeProjectReview { Week = pipelineWeek,
			CandidateId = own ? "routine-session|" + artist.artistId : searched ? item.MaterialSearch.SongId : shortlist.BestId,
			Perceived = perceived, RoutineRead = routine, Uncertainty = uncertainty, UseSearchedMaterial = searched && record,
			Action = pending ? "await-material-delivery" : record ? "record-reviewed-material" : own ? OwnSessionAction : "hold-for-stronger-material" };
		OnProgrammeProjectReview?.Invoke(item, pipelineWeek);
		return record || own;
	}

	// The session holds shortlist songs to exactly the standard its review applied, read uncertainty included.
	internal static float SessionMinimumRead(ProgrammeProjectReview review) =>
		review == null ? -1f : review.RoutineRead - Math.Max(0f, review.Uncertainty);

	// Planner ranking: the mean best read among the slot's acts with heard material (0 when none has any).
	internal static float SlotMaterialMean(IEnumerable<float> bestReads) {
		float sum = 0f; int n = 0;
		foreach (float r in bestReads) if (float.IsFinite(r) && r >= 0) { sum += r; n++; }
		return n > 0 ? sum / n : 0f;
	}

	// One scale for every act: material well above the slot's mean earns up to +1, and no heard material is -1.
	internal static float MaterialAttention(float bestRead, float slotMean) =>
		!float.IsFinite(bestRead) || bestRead < 0 ? -1f : Math.Clamp(4f * (bestRead - slotMean), -1f, 1f);

	internal const string OwnSessionAction = "record-own-session";
	internal static bool IsOwnSession(ReleaseSlateItem item) => item?.ProjectReview?.Action == OwnSessionAction;
}
