using System;
using System.Linq;
using Godot;

public sealed class AlbumPracticeTelemetry {
	public int Year, Week, SourceAlbumAge, SourceAlbumPeak, PriorChartedSingles;
	public string LabelId, ArtistId, AlbumId, SingleId, Genre, Format, Route, Reason, MasterId, SongId;
	public float CommissionProbability, Roll, SingleContribution, CampaignCost, PreparationCost, LpLift, Cannibalization, DelayCost, OpportunityCost, IncrementalNet, Hook, Production;
}

public partial class CompetitorManager {
	public event Action<AlbumPracticeTelemetry> OnAlbumPracticeDecision;
	// The competitor's release tick precedes ChartManager's tick in week one.
	// Prewarm never advances this clock; new week-one commitments are live decisions.
	internal bool AlbumPracticeLiveTick => pipelineWeek > 0;
	private bool lastPracticeStandalone;
	private Genre lastPracticeAlbumGenre;
	private string lastPracticeAlbumId;
	private SimulatedArtist lastPracticeAlbumArtist;

	private ReleasePlan ResolveRecordedAlbumCampaign(AILabel label, SimulatedArtist artist, GameDate date,
		DecisionContext decision, ReleasePlan plan, Record album) {
		if (!AlbumReleasePractice.IsLive) return plan;
		float probability = AlbumReleasePractice.AdvanceCommissionProbability(album.primaryGenre, date.year, AlbumReleasePractice.CampaignScale);
		float roll = AlbumReleasePractice.Roll("advance", label.labelId, artist.artistId, album.recordId, pipelineWeek);
		AlbumTrack track = SelectAlbumSingleTrack(album, artist, date.year);
		float preparation = label.GetProductionCost() * .15f * decision.recordingCostMultiplier;
		float campaign = label.GetMarketingBudget(artist);
		float contribution = ForecastRecordedSingleContribution(label, artist, date, decision, track);
		var traits = track == null ? (hook: 0f, production: 0f, dance: 0f) : GetTrackTraitsForPromo(track);
		int trackQuartile = GetQualityQuartile((traits.hook + traits.production) * .5f);
		float peakScore = expectedPeakScoreByBucket[trackQuartile * 4 + decision.careerBand];
		float headroom = 1f - ProjectLaunchAwareness(label, artist, campaign);
		// Forecast the actual capped launch-awareness mechanism on the LP's scale.
		// No free $10k or additional unobserved recruitment credit.
		float lpContribution = Mathf.Max(0f, plan.priorAlbumNet + plan.albumProductionCost);
		float lift = promoAwarenessBonusMax * peakScore * headroom * lpContribution;
		// The realized AlbumSimulator diverts a bounded FRACTION OF LP DEMAND.
		// It does not divert single-units * LP-margin. Forecast the same exposure.
		float cannibalization = lpContribution * cannibalizationStrength * plan.substitutionPropensity * plan.expectedOverlapFraction * peakScore;
		float delay = .10f * (albumDropGapWeeksMin + albumDropGapWeeksMax) * .5f / 52f * Mathf.Max(0f, plan.projectedAlbumStandaloneNet);
		float opportunity = AlbumReleasePractice.SeparateSinglesCalendar
			? AlbumReleasePractice.SinglesProgrammeProbability(album.primaryGenre, date.year) * Mathf.Max(0f, plan.projectedOrphanSingleNet - campaign) : 0f;
		float net = AlbumReleasePractice.IncrementalContribution(contribution, preparation, campaign, lift, cannibalization, delay, opportunity);
		string reason = !AlbumReleasePractice.CanAdvance(album.primaryGenre, album.album.albumFormat, date.year) ? "format-or-market" :
			track == null ? "no-unissued-master" : roll >= probability ? "no-advance-commission" : net <= 0f ? "incremental-economics" : "advance-approved";
		bool approved = reason == "advance-approved";
		plan.strategy = approved ? ReleaseStrategy.AlbumWithPromo : ReleaseStrategy.AlbumStandalone;
		plan.expectedPromoSingleNet = contribution - preparation;
		plan.expectedPromoLift = lift;
		plan.cannibalizationLoss = plan.cannibalizationCharged = cannibalization;
		plan.promoAdvantage = net;
		plan.projectedAlbumWithPromoNet = plan.projectedAlbumStandaloneNet + net;
		OnAlbumPracticeDecision?.Invoke(new AlbumPracticeTelemetry {
			Year = date.year, Week = pipelineWeek, LabelId = label.labelId, ArtistId = artist.artistId,
			AlbumId = album.recordId, Genre = album.primaryGenre.ToString(), Format = album.album.albumFormat.ToString(),
			Route = approved ? "AdvanceAlbumSingle" : "AlbumOnly", Reason = reason,
			CommissionProbability = probability, Roll = roll, SingleContribution = contribution,
			CampaignCost = campaign, PreparationCost = preparation, LpLift = lift,
			Cannibalization = cannibalization, DelayCost = delay, OpportunityCost = opportunity, IncrementalNet = net,
			MasterId = track?.masterId, SongId = track?.songId, Hook = traits.hook, Production = traits.production,
			PriorChartedSingles = plan.chartedSingles
		});
		return plan;
	}

	private float ForecastRecordedSingleContribution(AILabel label, SimulatedArtist artist, GameDate date,
		DecisionContext decision, AlbumTrack track, bool catalogue = false) {
		if (track == null) return 0f;
		var traits = GetTrackTraitsForPromo(track);
		decision.qualityEstimate = (traits.hook + traits.production) * .5f;
		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.genreSinglesMarketFactor = CalculateSingleGenreMarketFactor(track.genre, date.year);
		float contribution = Mathf.Max(0f, SinglePriorNetAtBookedPrice(decision) + decision.singleProductionCost);
		if (catalogue) {
			var memory = GetResponsiveMemory(label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumExtraction), ChartManager.Instance.GetCurrentChartWeek());
			contribution += memory.Confidence * memory.Residual * Mathf.Max(1f, contribution);
		}
		return Mathf.Max(0f, contribution);
	}

	private Record TryExtractSuccessfulAlbumSingle(AILabel label, SimulatedArtist artist, GameDate date,
		DecisionContext decision, ref ReleasePlan plan) {
		if (!AlbumReleasePractice.IsLive || AlbumReleasePractice.Roll("catalogue-opportunity", label.labelId, artist.artistId, "", pipelineWeek) >= .35f) return null;
		foreach (var runtime in ChartManager.Instance.GetRecordsForLabel(label.labelId)
			.Where(r => r.baseRecord.format == ReleaseFormat.Album && r.baseRecord.artistId == artist.artistId)
			.OrderByDescending(r => r.totalUnitsSold).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal)) {
			Record album = runtime.baseRecord;
			// leadSingleIds includes advance singles; an album may support one later cut as well.
			bool extracted = album.album?.postAlbumSingleIds?.Length > 0;
			if (album.album == null || album.album.externalMedia != null || !AlbumReleasePractice.CanExtract(
				album.primaryGenre, date.year, runtime.weeksSinceRelease, runtime.peakPosition, extracted)) continue;
			AlbumTrack track = SelectAlbumSingleTrack(album, artist, date.year);
			if (track == null) continue;
			float contribution = ForecastRecordedSingleContribution(label, artist, date, decision, track, catalogue: true);
			float preparation = label.GetProductionCost() * .15f * decision.recordingCostMultiplier;
			float campaign = label.GetMarketingBudget(artist);
			var traits = GetTrackTraitsForPromo(track);
			float peakScore = expectedPeakScoreByBucket[GetQualityQuartile((traits.hook + traits.production) * .5f) * 4 + decision.careerBand];
			float remainingLpContribution = Mathf.Max(0f, runtime.lifetimeLabelNet) / Mathf.Max(1, runtime.weeksSinceRelease) *
				Mathf.Min(12, 52 - runtime.weeksSinceRelease) * .5f;
			float diversion = remainingLpContribution * cannibalizationStrength * CalculateSubstitutionPropensity(album.primaryGenre, date.year) * peakScore;
			float incrementalNet = AlbumReleasePractice.IncrementalContribution(contribution, preparation, campaign, 0f, diversion, 0f);
			if (incrementalNet <= 0f) continue;
			// Check the non-negotiable cash floor BEFORE moving any public LP master.
			// The shared launch path can clamp campaign spend, but cannot fund preparation.
			float reserve = (EconomicLabelLifecycle.Enabled && !label.isPlayerOwned
				? EconomicLabelLifecycle.CommitmentReserve(label, signing: false) : label.GetMonthlyOverhead()) +
				LabelPressingEconomics.InitialRunReserve(label, ReleaseFormat.Single, GetPressingCostPerUnit(ReleaseFormat.Single));
			if (label.cashReserves < reserve + preparation) continue;
			Record single = CreatePromoSingleFromAlbum(album, artist, label, date.year);
			if (single == null) continue;
			single.sourceAlbumRecordId = album.recordId;
			album.album.postAlbumSingleIds = (album.album.postAlbumSingleIds ?? Array.Empty<string>()).Append(single.recordId).ToArray();
			plan.strategy = ReleaseStrategy.AlbumExtraction;
			plan.priorSingleNet = contribution - preparation;
			plan.projectedSingleNet = plan.projectedOrphanSingleNet = plan.priorSingleNet;
			plan.singleProductionCost = preparation;
			OnAlbumPracticeDecision?.Invoke(new AlbumPracticeTelemetry {
				Year = date.year, Week = pipelineWeek, LabelId = label.labelId, ArtistId = artist.artistId,
				AlbumId = album.recordId, SingleId = single.recordId, Genre = single.primaryGenre.ToString(),
				Format = album.album.albumFormat.ToString(), Route = "PostAlbumSingle", Reason = "observed-album-success",
				SourceAlbumAge = runtime.weeksSinceRelease, SourceAlbumPeak = runtime.peakPosition,
				SingleContribution = contribution, CampaignCost = campaign, PreparationCost = preparation,
				IncrementalNet = incrementalNet, Cannibalization = diversion,
				MasterId = track.masterId, SongId = track.songId, Hook = single.hookStrength, Production = single.productionQuality
			});
			return single;
		}
		return null;
	}

	private void TryIndependentSinglesProgramme(AILabel label, GameDate date) {
		if (!AlbumReleasePractice.IsLive || !AlbumReleasePractice.SeparateSinglesCalendar || !lastPracticeStandalone) return;
		Genre genre = lastPracticeAlbumGenre;
		string albumId = lastPracticeAlbumId;
		SimulatedArtist artist = lastPracticeAlbumArtist;
		float probability = AlbumReleasePractice.SinglesProgrammeProbability(genre, date.year);
		float roll = AlbumReleasePractice.Roll("independent-single", label.labelId, "", albumId, pipelineWeek);
		if (roll >= probability) return;
		// The act was due at the fork. Its LP commitment does not erase that
		// independently due singles slot. Normal single A&R can make a new master
		// or choose an eligible older LP cut; it cannot extract from this new LP.
		bool released = TryReleaseRecord(label, date, singlesOnly: true, scheduledSingleArtist: artist);
		OnAlbumPracticeDecision?.Invoke(new AlbumPracticeTelemetry {
			Year = date.year, Week = pipelineWeek, LabelId = label.labelId, AlbumId = albumId, Genre = genre.ToString(),
			Route = "IndependentSinglesOpportunity", Reason = released ? "funded-and-released" : "no-funded-eligible-single",
			CommissionProbability = probability, Roll = roll
		});
	}

	private AlbumTrack SelectAlbumSingleTrack(Record albumRecord, SimulatedArtist artist, int year) {
		AlbumTrack[] tracks = albumRecord.album?.nonSingleTracks;
		if (tracks == null || tracks.Length == 0) return null;
		int index = FindAlbumSingleTrackIndex(albumRecord, artist, year);
		return tracks[index];
	}

	private int FindAlbumSingleTrackIndex(Record albumRecord, SimulatedArtist artist, int year) {
		Album album = albumRecord.album;
		bool structured = PolarSongBehavior.UsePolarFitSelection || (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		int bestIndex = 0;
		for (int i = 1; i < album.nonSingleTracks.Length; i++) {
			if (PolarSongBehavior.UsePolarFitSelection && !PolarSongBehavior.AuditLegacyPromotion) {
				if (SongMaterialSelectionService.PromoSuitability(album.nonSingleTracks[i], artist, albumRecord.primaryGenre, year) >
					SongMaterialSelectionService.PromoSuitability(album.nonSingleTracks[bestIndex], artist, albumRecord.primaryGenre, year)) bestIndex = i;
			} else if (structured) {
				int selectionYear = albumRecord.releaseDate.year > 0 ? albumRecord.releaseDate.year : (TimeManager.Instance?.CurrentDate.year ?? 1960);
				float candidate = GetLeadSingleSuitability(album.nonSingleTracks[i], albumRecord.primaryGenre, selectionYear);
				float current = GetLeadSingleSuitability(album.nonSingleTracks[bestIndex], albumRecord.primaryGenre, selectionYear);
				if (candidate > current || (Mathf.IsEqualApprox(candidate, current) && string.CompareOrdinal(album.nonSingleTracks[i].title, album.nonSingleTracks[bestIndex].title) < 0)) bestIndex = i;
			} else if (album.nonSingleTracks[i].quality > album.nonSingleTracks[bestIndex].quality) bestIndex = i;
		}
		return bestIndex;
	}
}
