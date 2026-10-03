using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Opt-in behavior boundary. Scores are internal decisions; never player reads.</summary>
public static class PolarSongBehavior {
	public static bool UsePolarFitSelection; // Default false, including old saves.
	// Headless causal probes only. Never persisted as gameplay settings.
	internal static bool AuditLegacyRealization, AuditLegacyPromotion;
	internal static bool AuditNoHookCeiling, AuditNoCapabilityPenalty, AuditNoCriticAdjustment, AuditNoMomentConversion;
	internal static bool AuditOriginalRealization;
	internal static float ExecutionFactor(float capability) {
		if (AuditNoCapabilityPenalty) return 1;
		var table = PolarSongTable.Current;
		// Minor fit uncertainty is not a universal execution tax. Severe mismatches
		// retain the original maximum loss; the original probe keeps its old curve.
		float shortfall = AuditOriginalRealization ? 1 - capability :
			Mathf.Clamp((table.N("executionFullCapability") - capability) / table.N("executionFullCapability"), 0, 1);
		return 1 - shortfall * table.N("executionPenalty");
	}
	internal static float RealizeHook(float performanceHook, float compositionHook) {
		var table = PolarSongTable.Current;
		// Composition craft is material for the performance, never a demand-axis bonus.
		// A hard min lost the upside of strong songs while taxing every weaker one.
		if (AuditOriginalRealization) return AuditNoHookCeiling ? performanceHook : Mathf.Min(performanceHook, compositionHook);
		float hook = Mathf.Lerp(performanceHook, compositionHook, table.N("recordingHookBlend"));
		return AuditNoHookCeiling ? hook : Mathf.Lerp(hook, Mathf.Min(hook, compositionHook), table.N("hookCeilingStrength"));
	}
	internal static float PromotionRealizationFactor(AlbumTrack track) {
		if (AuditOriginalRealization) return 1;
		var table = PolarSongTable.Current;
		float hookWeight = table.N("recordingQualityHookWeight"), productionWeight = table.N("recordingQualityProductionWeight");
		float quality = Mathf.Clamp(track.hookStrength * hookWeight + track.productionQuality * productionWeight +
			track.danceability * (1 - hookWeight - productionWeight), 0, 1);
		return Mathf.Lerp(1, quality, table.N("promotionRealizationWeight"));
	}
	private static Dictionary<Genre, MarketTasteSnapshot> tastes = new();
	public static Dictionary<Genre, MarketTasteSnapshot> CaptureTaste() => new(tastes);
	public static void RestoreTaste(Dictionary<Genre, MarketTasteSnapshot> saved) => tastes = new(saved ?? new());
	public static void ResetTaste() => tastes.Clear();
	public static SongMasterMetadata Reference(SongComposition song, int year, string explicitId = null) {
		var master = PolarSongMetadataService.Get(explicitId);
		if (explicitId != null) return master != null && master.songId == song.songId && master.recordingYear <= year ? master : null;
		// Only known completed releases, never an unshipped master or a future outcome.
		foreach (var memory in (song.recordings ?? new()).Where(r => r.year <= year)
			.OrderByDescending(r => r.year).ThenBy(r => r.recordId, StringComparer.Ordinal)) {
			master = PolarSongMetadataService.Get(memory.recordId);
			if (master != null && master.songId == song.songId && master.recordingYear <= year) return master;
		}
		return null; // Explicit demo inference for historical catalogue without a known master.
	}
	public static MaterialFit Fit(SongComposition song, SimulatedArtist artist, Genre genre, int year,
		string referenceId = null, PolarSessionContext session = null, AILabel label = null) {
		var table = PolarSongTable.Current;
		var reference = Reference(song, year, referenceId);
		return PolarMaterialFit.Compute(SongProfileDeriver.Derive(song, reference?.taxonomy ?? song.demoTaxonomy, table),
			PolarActProfileDeriver.Derive(artist, label ?? ChartManager.Instance?.GetLabelById(artist.labelId), session, table),
				tastes.GetValueOrDefault(genre), (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + 1, table);
	}
	internal static float SelectionScore(MaterialFit fit) {
		var table = PolarSongTable.Current;
		return fit.Capability * table.N("selectionCapability") + fit.Identity * table.N("selectionIdentity") + fit.Moment * table.N("selectionMoment");
	}
	// Autonomous commission/live-set decision only; player catalogues do not call this ordering.
	internal static IEnumerable<SongComposition> SuitableSongs(IEnumerable<SongComposition> pool, SimulatedArtist artist, int year) =>
		pool.Where(s => s != null && s.originYear <= year).GroupBy(s => s.songId).Select(g => g.First())
			.Select(s => new { Song = s, Score = SelectionScore(Fit(s, artist, artist.primaryGenre, year)) })
			.OrderByDescending(x => x.Score).ThenByDescending(x => x.Song.primaryGenre == artist.primaryGenre)
			.ThenBy(x => x.Song.songId, StringComparer.Ordinal).Select(x => x.Song);
	public static SelectedSongMaterial Prepare(SelectedSongMaterial material, SimulatedArtist artist, Record record,
		Genre genre, int year, AILabel label = null, PolarSessionContext session = null) {
		if (!UsePolarFitSelection || material?.Song == null) return material;
		var reference = material.IsCover ? Reference(material.Song, year, material.ReferenceSubjectId ?? material.OriginalRecordId) : null;
		var table = PolarSongTable.Current;
		var act = PolarActProfileDeriver.Derive(artist, label ?? ChartManager.Instance?.GetLabelById(artist.labelId), session, table);
		var proposal = PolarCoverResolver.Propose(material.Song, reference, act, genre, year,
			(ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + 1, record.recordId, tastes.GetValueOrDefault(genre), table);
		material.PolarProposal = proposal;
		material.ReferenceSubjectId = reference?.masterId ?? "demo:" + material.Song.songId;
		material.OriginalRecordId = reference?.masterId;
		material.OriginalArtistId = reference?.artistId;
		if (material.IsCover && !AuditLegacyRealization) material.ArrangementOriginality = proposal.fit.Stretch;
		return material;
	}
	public static void ObserveCompletedWeek(IEnumerable<RecordRuntimeData> chart, int week) {
		if (!UsePolarFitSelection) return;
		var table = PolarSongTable.Current;
		foreach (var group in chart.Where(r => r?.baseRecord?.format == ReleaseFormat.Single).GroupBy(r => r.baseRecord.primaryGenre)) {
			if (tastes.TryGetValue(group.Key, out var prior) && prior.asOfWeek >= week) continue;
			var profiles = group.Select(r => {
				var song = CompositionCatalogService.GetSong(r.baseRecord.songId);
				return song == null ? null : SongProfileDeriver.Derive(song, PolarSongMetadataService.Get(r.baseRecord.PlugMasterId)?.taxonomy ?? song.demoTaxonomy, table);
			}).Where(p => p != null).ToArray();
			if (profiles.Length == 0) continue;
			var taste = new MarketTasteSnapshot { asOfWeek = week, observations = profiles.Length };
			for (int i = 0; i < SongProfile.IdentityCount; i++) {
				float mean = profiles.Average(p => p.axes[SongProfile.DemandCount + i]);
				taste.identity[i] = prior == null ? mean : SongProfileDeriver.Lerp(prior.identity[i], mean, table.N("tasteDrift"));
			}
			tastes[group.Key] = taste;
		}
	}
	public static PolarFitSnapshot CommittedFit(Record record) => UsePolarFitSelection && record?.format == ReleaseFormat.Single
		? PolarSongMetadataService.Get(record.PlugMasterId)?.realizedFit : null;
	public static float CriticCraft(float craft, Record record) {
		if (AuditLegacyRealization || AuditNoCriticAdjustment) return craft;
		var fit = CommittedFit(record);
		return fit == null ? craft : Mathf.Clamp(craft * (1 - (1 - fit.identity) * PolarSongTable.Current.N("criticIdentityPenalty")), 0, 1);
	}
	public static float MomentMultiplier(Record record) {
		if (AuditLegacyRealization || AuditNoMomentConversion) return 1;
		var fit = CommittedFit(record);
		return fit == null || !fit.hasMarketEvidence ? 1 : 1 + (fit.moment - PolarSongTable.Current.N("neutralMoment")) * PolarSongTable.Current.N("momentConversionStrength");
	}
}
