using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public sealed record PolarMaterialCandidateObservation(SimulatedArtist Artist, Record Record, SongComposition Song,
	Genre ProjectGenre, int Year, string Phase, string Source, string ReferenceId);

/// <summary>Harness-only bounded telemetry and lagged market observations. Never consumed by selection.</summary>
public sealed class PolarSongShadowTelemetry : IDisposable {
	private readonly PolarSongTable table;
	private readonly StreamWriter writer, summary;
	private readonly Dictionary<Genre, MarketTasteSnapshot> tastes = new();
	private int currentWeek = -1, written, omitted;
	public PolarSongShadowTelemetry(string directory, string run) {
		table = PolarSongTable.Current;
		writer = new StreamWriter(Path.Combine(directory, run + "-polar-fit.csv"));
		summary = new StreamWriter(Path.Combine(directory, run + "-polar-fit-budget.csv"));
		writer.WriteLine("week,year,recordId,artistId,songId,phase,source,referenceId,referenceKind,inferredTaxonomy,mappingVersion,capability,identity,moment,deficit,worstAxis,stretch,referenceIdentityDistance,hasMarketEvidence,tasteAsOfWeek,proposedArchetype,realizedCapability,realizedIdentity,realizedMoment,realizedStretch,inferredCapability,inferredSession");
		summary.WriteLine("week,written,omitted,rowLimit");
		PolarSongMetadataService.ShadowProfilesEnabled = true;
		PolarSongMetadataService.WarmCommittedProfiles(table);
		SongMaterialSelectionService.OnPolarCandidate += Observe;
	}
	private void Observe(PolarMaterialCandidateObservation e) {
		if (e.Song == null || e.Artist == null) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (currentWeek != week) { FlushBudget(); currentWeek = week; written = omitted = 0; }
		if (written >= table.N("maxShadowRowsPerWeek")) { omitted++; return; }
		var reference = PolarSongMetadataService.Get(e.ReferenceId);
		// A missing or future legacy ID falls back explicitly to demo inference; never pick a latest version.
		if (reference != null && (reference.songId != e.Song.songId || reference.recordingYear > e.Year)) reference = null;
		var songProfile = SongProfileDeriver.Derive(e.Song, reference?.taxonomy ?? e.Song.demoTaxonomy, table);
		var label = ChartManager.Instance?.GetLabelById(string.IsNullOrEmpty(e.Record.labelId) ? e.Artist.labelId : e.Record.labelId);
		var act = PolarActProfileDeriver.Derive(e.Artist, label, null, table);
		var taste = tastes.GetValueOrDefault(e.ProjectGenre);
		var fit = PolarMaterialFit.Compute(songProfile, act, taste, week, table);
		// Full resolver evaluation is reserved for source finalists, keeping pool sampling cheap.
		string archetype = "";
		PolarArrangementProposal proposal = null;
		if (e.Phase != "pool") {
			proposal = PolarCoverResolver.Propose(e.Song, reference, act, e.ProjectGenre, e.Year, week, e.Record.recordId, taste, table);
			archetype = proposal.taxonomy.archetype.ToString();
		}
		writer.WriteLine(string.Join(",", new[] { week.ToString(), e.Year.ToString(), Csv(e.Record.recordId), Csv(e.Artist.artistId), Csv(e.Song.songId),
			Csv(e.Phase), Csv(e.Source), Csv(reference?.masterId ?? e.ReferenceId), reference == null ? "demo" : "master", songProfile.inferredTaxonomy.ToString(),
			Csv(table.Version), F(fit.Capability), F(fit.Identity), F(fit.Moment), F(fit.Deficit), fit.WorstAxis.ToString(), F(fit.Stretch),
			F(fit.ReferenceIdentityDistance), fit.HasMarketEvidence.ToString(), taste?.asOfWeek.ToString() ?? "", archetype,
			proposal == null ? "" : F(proposal.fit.Capability), proposal == null ? "" : F(proposal.fit.Identity),
			proposal == null ? "" : F(proposal.fit.Moment), proposal == null ? "" : F(proposal.fit.Stretch),
			act.inferredCapability.ToString(), act.inferredSession.ToString() }));
		written++;
	}
	public void ObserveCompletedWeek(IEnumerable<RecordRuntimeData> chart, int week) {
		// Chart outcomes from a completed week can affect NEXT week's Moment only. No new RNG/random walk.
		var observations = chart.Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single)
			.Where(r => r.baseRecord.releaseDate.year > 0).GroupBy(r => r.baseRecord.primaryGenre);
		foreach (var group in observations) {
			var profiles = group.Select(r => {
				var song = CompositionCatalogService.GetSong(r.baseRecord.songId);
				return song == null ? null : SongProfileDeriver.Derive(song, PolarSongMetadataService.Get(r.baseRecord.PlugMasterId)?.taxonomy, table);
			}).Where(p => p != null).ToArray();
			if (profiles.Length == 0) continue;
			var prior = tastes.GetValueOrDefault(group.Key);
			var taste = new MarketTasteSnapshot { asOfWeek = week, observations = profiles.Length };
			for (int i = 0; i < SongProfile.IdentityCount; i++) {
				float average = profiles.Average(p => p.axes[SongProfile.DemandCount + i]);
				taste.identity[i] = prior == null ? average : SongProfileDeriver.Lerp(prior.identity[i], average, table.N("tasteDrift"));
			}
			tastes[group.Key] = taste;
		}
	}
	private void FlushBudget() { if (currentWeek >= 0) summary.WriteLine($"{currentWeek},{written},{omitted},{table.N("maxShadowRowsPerWeek")}"); }
	public void Dispose() { SongMaterialSelectionService.OnPolarCandidate -= Observe; PolarSongMetadataService.ShadowProfilesEnabled = false; FlushBudget(); writer.Dispose(); summary.Dispose(); }
	private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
	private static string Csv(string v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
}
