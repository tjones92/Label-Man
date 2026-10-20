using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// Release Judgement directive, Phase 0 and Phase 1 readouts. Observation only: reads public state and the shadow's events,
// writes new files only, and never changes a pre-existing CSV's schema.
//   Phase 0 (always): <run>-single-launch-quality.csv, one row per AI single launch (realized quality and the act's gap
//     since its previous single), and <run>-act-release-years.csv, one row per act-year with signed weeks, releases by
//     path, distinct Hot 100 records and year-start lifetime counters (for lagged-status cohorts and zero-release years).
//   Phase 1 (--release-judgement-shadow): <run>-release-judgements-shadow.csv, <run>-repertoire-health.csv and, with
//     --release-judgement-events, <run>-material-events.csv.
public partial class ChartAuditRunner {
	private sealed class ActYear {
		public int Year, SignedWeeks, OrphanSingles, PromoSingles, StandaloneLps, ExtractedSingles;
		public string LabelId = "", Tier = "", CareerAtStart = "";
		public int ChartedAtStart = -1, Top40AtStart = -1, Top10AtStart = -1;
		public readonly HashSet<string> Hot100Records = new(StringComparer.Ordinal);
	}

	private StreamWriter launchQualityWriter, actReleaseYearWriter;
	private StreamWriter shadowJudgementWriter, repertoireHealthWriter, materialEventWriter;
	private readonly Dictionary<string, int> lastSingleAuditWeek = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ActYear> actYears = new(StringComparer.Ordinal);
	private int actYearYear = -1, actYearWeeksObserved;

	private void OpenReleaseJudgementOutputs(string directory) {
		launchQualityWriter = CreateWriter(Path.Combine(directory, $"{runName}-single-launch-quality.csv"));
		launchQualityWriter.WriteLine("week,year,singleId,labelId,tier,artistId,strategy,genre,careerState,diagQuality,hook,production,runtimeQuality,weeksSinceLastSingle");
		actReleaseYearWriter = CreateWriter(Path.Combine(directory, $"{runName}-act-release-years.csv"));
		actReleaseYearWriter.WriteLine("year,weeksObserved,artistId,labelId,tier,signedWeeks,singles,orphanSingles,promoSingles,standaloneLps,lpsScheduled,hot100Records,careerStateAtStart,chartedAtStart,top40AtStart,top10AtStart");
		if (!ReleaseJudgement.Shadow) return;
		shadowJudgementWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-judgements-shadow.csv"));
		shadowJudgementWriter.WriteLine("auditWeek,week,year,labelId,tier,artistId,recordId,strategy,legacyQuality,legacyHook,legacyProduction,active,recordable,offered,bestId,bestSongId,bestSource,bestProvenance,bestAgeWeeks,bestPerceived,readBand,auditBestExpectedQuality,auditBestExpectedHook,auditBestExpectedProduction,auditBestSpread,auditOracleExpectedQuality,auditMeanRecordableExpected,selectMode,outcome,selectedId,selectedSource");
		repertoireHealthWriter = CreateWriter(Path.Combine(directory, $"{runName}-repertoire-health.csv"));
		repertoireHealthWriter.WriteLine("auditWeek,week,year,actsTracked,actsSigned,actsWithRecordable,active,recordable,offered,initialized,untracked,issued,archived,archivedPassedOver,archivedRefused,archivedWindowClosed,arrivalsArtistWritten,arrivalsProfessional,arrivalsStandard,arrivalsRecentHit,arrivalsTraditional");
		AiRepertoireService.OnJudgement += WriteShadowJudgement;
		if (ReleaseJudgement.Events) {
			materialEventWriter = CreateWriter(Path.Combine(directory, $"{runName}-material-events.csv"));
			materialEventWriter.WriteLine("week,kind,labelId,artistId,candidateId,songId,source,provenance,evidenceVersion,reason");
			AiRepertoireService.OnMaterialEvent += WriteMaterialEvent;
		}
	}

	/// <summary>Phase 0: one row per AI single launch, from the same release event the strategy CSV reads.</summary>
	private void CaptureLaunchQuality(ReleaseStrategyTelemetry strategy) {
		if (launchQualityWriter == null || strategy == null) return;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		ActYear act = ActYearFor(strategy.artistId, year);
		if (strategy.strategy == ReleaseStrategy.AlbumStandalone) { act.StandaloneLps++; return; }
		string id = strategy.strategy == ReleaseStrategy.AlbumWithPromo ? strategy.promoSingleId : strategy.recordId;
		if (strategy.strategy == ReleaseStrategy.AlbumWithPromo) act.PromoSingles++;
		else if (strategy.strategy == ReleaseStrategy.AlbumExtraction) act.ExtractedSingles++;
		else act.OrphanSingles++;
		RecordRuntimeData runtime = string.IsNullOrEmpty(id) ? null : ChartManager.Instance?.GetRecordRuntimeData(id);
		if (runtime?.baseRecord == null) return;
		Record record = runtime.baseRecord;
		CaptureGenreHookLaunch(strategy, record, year);
		int gap = lastSingleAuditWeek.TryGetValue(strategy.artistId, out int last) ? currentAuditWeek - last : -1;
		lastSingleAuditWeek[strategy.artistId] = currentAuditWeek;
		launchQualityWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture), Csv(id),
			Csv(strategy.labelId), Csv(strategy.tier.ToString()), Csv(strategy.artistId), Csv(strategy.strategy.ToString()),
			Csv(record.primaryGenre.ToString()), Csv(strategy.careerState.ToString()),
			F((record.hookStrength + record.productionQuality) / 2f), F(record.hookStrength), F(record.productionQuality),
			F(runtime.GetQuality()), gap.ToString(CultureInfo.InvariantCulture)
		}));
	}

	private ActYear ActYearFor(string artistId, int year) {
		if (year != actYearYear) FlushActYears(year);
		if (!actYears.TryGetValue(artistId, out ActYear act)) {
			act = new ActYear { Year = year };
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(artistId);
			if (artist != null) {
				act.CareerAtStart = artist.careerState.ToString();
				act.ChartedAtStart = artist.charted; act.Top40AtStart = artist.top40Hits; act.Top10AtStart = artist.top10Hits;
			}
			actYears[artistId] = act;
		}
		return act;
	}

	/// <summary>Weekly: signed weeks per AI-rostered act, distinct Hot 100 records per act, and the shadow's health.</summary>
	private void CaptureReleaseJudgementWeek(int week) {
		int year = TimeManager.Instance.CurrentDate.year;
		if (year != actYearYear) FlushActYears(year);
		actYearWeeksObserved++;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (AILabel label in ChartManager.Instance.GetAllLabels()) {
			if (label?.roster == null || label.isPlayerOwned) continue;
			foreach (SimulatedArtist artist in label.roster) {
				if (artist == null || !seen.Add(artist.artistId)) continue;
				ActYear act = ActYearFor(artist.artistId, year);
				act.SignedWeeks++;
				act.LabelId = label.labelId;
				act.Tier = label.tier.ToString();
			}
		}
		foreach (RecordRuntimeData entry in ChartManager.Instance.GetCurrentChart()) {
			Record record = entry?.baseRecord;
			if (record == null || string.IsNullOrEmpty(record.artistId) || record.isPlayerOwned) continue;
			ActYearFor(record.artistId, year).Hot100Records.Add(record.recordId);
		}
		ShadowRepertoireHealth health = AiRepertoireService.LastHealth;
		if (repertoireHealthWriter == null || health == null) return;
		int Arrivals(SongMaterialSource source) => health.Arrivals.GetValueOrDefault(source);
		int ArchivedFor(string reason) => health.ArchivedByReason.GetValueOrDefault(reason);
		repertoireHealthWriter.WriteLine(string.Join(",", new[] {
			week, health.Week, health.Year, health.ActsTracked, health.ActsSigned, health.ActsWithRecordable, health.Active, health.Recordable,
			health.Offered, health.Initialized, health.Untracked, health.Issued, health.Archived,
			ArchivedFor("passed-over"), ArchivedFor("refused"), ArchivedFor("window-closed"),
			Arrivals(SongMaterialSource.ArtistWritten), Arrivals(SongMaterialSource.ExternalProfessional), Arrivals(SongMaterialSource.CoverStandard),
			Arrivals(SongMaterialSource.CoverRecentHit), Arrivals(SongMaterialSource.TraditionalPublicDomain)
		}.Select(v => v.ToString(CultureInfo.InvariantCulture))));
	}

	private void FlushActYears(int nextYear) {
		if (actReleaseYearWriter != null)
			foreach (var pair in actYears.OrderBy(p => p.Key, StringComparer.Ordinal)) {
				ActYear a = pair.Value;
				actReleaseYearWriter.WriteLine(string.Join(",", new[] {
					a.Year.ToString(CultureInfo.InvariantCulture), actYearWeeksObserved.ToString(CultureInfo.InvariantCulture), Csv(pair.Key),
					Csv(a.LabelId), Csv(a.Tier), a.SignedWeeks.ToString(CultureInfo.InvariantCulture),
					(a.OrphanSingles + a.PromoSingles + a.ExtractedSingles).ToString(CultureInfo.InvariantCulture), a.OrphanSingles.ToString(CultureInfo.InvariantCulture),
					a.PromoSingles.ToString(CultureInfo.InvariantCulture), a.StandaloneLps.ToString(CultureInfo.InvariantCulture),
					(a.PromoSingles + a.StandaloneLps).ToString(CultureInfo.InvariantCulture), a.Hot100Records.Count.ToString(CultureInfo.InvariantCulture),
					Csv(a.CareerAtStart), a.ChartedAtStart.ToString(CultureInfo.InvariantCulture), a.Top40AtStart.ToString(CultureInfo.InvariantCulture),
					a.Top10AtStart.ToString(CultureInfo.InvariantCulture)
				}));
			}
		actYears.Clear();
		actYearYear = nextYear;
		actYearWeeksObserved = 0;
	}

	private void WriteShadowJudgement(ShadowJudgement j) {
		if (shadowJudgementWriter == null) return;
		string Opt(float v) => v < 0f ? "" : F(v);
		shadowJudgementWriter.WriteLine(string.Join(",", new[] {
			currentAuditWeek.ToString(CultureInfo.InvariantCulture), j.Week.ToString(CultureInfo.InvariantCulture), j.Year.ToString(CultureInfo.InvariantCulture),
			Csv(j.LabelId), Csv(j.Tier.ToString()), Csv(j.ArtistId), Csv(j.RecordId), Csv(j.Strategy.ToString()),
			F((j.LegacyHook + j.LegacyProduction) / 2f), F(j.LegacyHook), F(j.LegacyProduction),
			j.Active.ToString(CultureInfo.InvariantCulture), j.Recordable.ToString(CultureInfo.InvariantCulture), j.Offered.ToString(CultureInfo.InvariantCulture),
			Csv(j.BestId), Csv(j.BestSongId), Csv(j.BestId == null ? "" : j.BestSource.ToString()), Csv(j.BestProvenance),
			j.BestAgeWeeks.ToString(CultureInfo.InvariantCulture), Opt(j.BestPerceived), j.BestId == null ? "" : F(j.ReadBand),
			Opt(j.AuditBestExpectedQuality), Opt(j.AuditBestExpectedHook), Opt(j.AuditBestExpectedProduction), Opt(j.AuditBestSpread),
			Opt(j.AuditOracleExpectedQuality), Opt(j.AuditMeanRecordableExpected),
			Csv(j.Mode.ToString()), Csv(j.Outcome), Csv(j.SelectedId), Csv(j.SelectedId == null ? "" : j.SelectedSource.ToString())
		}));
	}

	private void WriteMaterialEvent(ShadowMaterialEvent e) {
		materialEventWriter?.WriteLine(string.Join(",", new[] {
			e.Week.ToString(CultureInfo.InvariantCulture), Csv(e.Kind), Csv(e.LabelId), Csv(e.ArtistId), Csv(e.CandidateId), Csv(e.SongId),
			Csv(e.Source.ToString()), Csv(e.Provenance), e.EvidenceVersion.ToString(CultureInfo.InvariantCulture), Csv(e.Reason)
		}));
	}

	private void CloseReleaseJudgementOutputs() {
		if (actYearYear >= 0 && actYears.Count > 0) FlushActYears(actYearYear);
		AiRepertoireService.OnJudgement -= WriteShadowJudgement;
		AiRepertoireService.OnMaterialEvent -= WriteMaterialEvent;
		launchQualityWriter?.Dispose(); actReleaseYearWriter?.Dispose();
		shadowJudgementWriter?.Dispose(); repertoireHealthWriter?.Dispose(); materialEventWriter?.Dispose();
		launchQualityWriter = actReleaseYearWriter = shadowJudgementWriter = repertoireHealthWriter = materialEventWriter = null;
	}
}
