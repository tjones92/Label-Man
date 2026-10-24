using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager {
	public event Action<ReleaseSlateItem, AILabel, int> OnLpAssembly;
	internal bool HasLpAssemblyState => ProgrammeState != null && (ProgrammeState.MasterControl != null || ProgrammeState.Shelf != null ||
		ProgrammeState.OpeningMasters != null || ProgrammeState.Items.Any(i => i.LpAssembly != null) ||
		ProgrammeState.Clocks.Values.Any(c => c.PackageWeek.HasValue));

	/// <summary>
	/// Once per world: an opening act's pre-1960 singles (OpeningCareerService history, drawn on its current label) become
	/// masters that label controls. The history carries title, year and peak but no recording, so a side's quality is
	/// provisional: the act's ordinary LP-track level plus a lift for its chart peak. Release months are keyed. An act
	/// that already issued LPs before 1960 may already have had a package; its last one is placed uniformly in the
	/// spacing window before the start, so eligibility spreads out instead of arriving in the first week.
	/// </summary>
	internal void SeedOpeningCatalogue() {
		if (!LabelReleaseProgramme.LpAssemblyEnabled || ProgrammeState == null || ProgrammeState.OpeningMasters != null) return;
		var seeded = ProgrammeState.OpeningMasters = new(StringComparer.Ordinal);
		var ledger = ProgrammeState.MasterControl ??= new(StringComparer.Ordinal);
		foreach (AILabel label in GetAllLabels().Where(l => !l.isPlayerOwned).OrderBy(l => l.labelId, StringComparer.Ordinal))
		foreach (SimulatedArtist artist in label.roster.OrderBy(a => a.artistId, StringComparer.Ordinal)) {
			var history = artist.publicHistory ?? new List<PublicReleaseEntry>();
			var tracks = new List<AlbumTrack>();
			float level = Mathf.Clamp(artist.CalculateBaseQuality() * .88f + label.productionQuality * .12f, .12f, .95f);
			for (int i = 0; i < history.Count; i++) {
				var e = history[i];
				if (e.format != ReleaseFormat.Single || e.year >= LpAssembly.OpeningYear || e.labelName != label.labelName) continue;
				string id = $"opening:{artist.artistId}:{i}";
				int month = 1 + Math.Min(11, (int)(LabelReleaseProgramme.Roll("opening-catalogue", artist.artistId, id, 0, 41) * 12));
				float lift = e.peakPosition is >= 1 and <= 100 ? .10f * (101 - e.peakPosition) / 100f : 0;
				tracks.Add(new AlbumTrack { sourceRecordId = id, title = e.title, genre = artist.primaryGenre, isReleasedSingle = true,
					quality = Mathf.Clamp(level + lift, .12f, .95f), releaseDate = new GameDate(e.year, month, 1), peakPosition = e.peakPosition });
				ledger[id] = new LpMasterControl { LabelId = label.labelId };
			}
			if (tracks.Count == 0) continue;
			seeded[artist.artistId] = tracks;
			bool hadLp = history.Any(e => e.format == ReleaseFormat.Album && e.year < LpAssembly.OpeningYear);
			if (hadLp && tracks.Count(x => x.peakPosition is >= 1 and <= LpAssembly.RelevantHitPeak) >= LpAssembly.GreatestHitsHits)
				ProgrammeClockFor(artist).PackageWeek = pipelineWeek -
					(int)(LabelReleaseProgramme.Roll("opening-catalogue", artist.artistId, "last-package", 0, 42) * LpAssembly.PackageGapWeeks);
		}
	}

	private IEnumerable<(string Id, AlbumTrack Track)> OpeningMastersFor(SimulatedArtist artist) =>
		ProgrammeState?.OpeningMasters != null && ProgrammeState.OpeningMasters.TryGetValue(artist.artistId, out var tracks)
			? tracks.Select(x => (x.sourceRecordId, x)) : Enumerable.Empty<(string, AlbumTrack)>();

	// Every issue or transfer passes TrackRelease, so the ledger follows sales and leases; a master leaves the label's
	// control when it moves, and an act's singles from a former label never count as this label's catalogue.
	internal void NoteMasterControl(string labelId, string recordId) {
		if (!LabelReleaseProgramme.LpAssemblyEnabled) return;
		ProgrammeState ??= new();
		var ledger = ProgrammeState.MasterControl ??= new(StringComparer.Ordinal);
		if (ledger.TryGetValue(recordId, out var control)) { control.LabelId = labelId; return; }
		Record record = ChartManager.Instance?.GetRecordRuntimeData(recordId)?.baseRecord;
		if (record == null || record.format != ReleaseFormat.Single || record.isPlayerOwned) return;
		ledger[recordId] = new LpMasterControl { LabelId = labelId,
			FromAlbum = !string.IsNullOrEmpty(record.sourceAlbumRecordId) || record.projectRole is ProjectRecordRole.PromoSingle or ProjectRecordRole.AlbumExtraction };
	}

	// A paid single master whose window expired stays on the label's shelf while the act remains signed. Bounded per act;
	// shelves of departed acts are cleared, since only the act's own LP can use them.
	internal void ShelveExpiredMaster(ReleaseSlateItem item) {
		if (!LabelReleaseProgramme.LpAssemblyEnabled || item.Format != ReleaseFormat.Single || item.PreparedRecord == null) return;
		AILabel label = GetLabel(item.LabelId);
		if (label?.roster.Any(a => a.artistId == item.ArtistId) != true) return;
		var shelf = ProgrammeState.Shelf ??= new();
		shelf.RemoveAll(s => s.LabelId == label.labelId && label.roster.All(a => a.artistId != s.ArtistId));
		shelf.Add(new ShelvedMaster { Record = item.PreparedRecord, LabelId = label.labelId, ArtistId = item.ArtistId, ShelvedWeek = pipelineWeek });
		var own = shelf.Where(s => s.LabelId == label.labelId && s.ArtistId == item.ArtistId)
			.OrderBy(s => s.ShelvedWeek).ThenBy(s => s.Record.recordId, StringComparer.Ordinal).ToList();
		if (own.Count > LpAssembly.ShelfPerAct) shelf.Remove(own[0]);
	}

	private static float MasterQuality(Record r) => r.hookStrength * .5f + r.productionQuality * .3f + r.danceability * .2f;

	private static AlbumTrack ShelvedTrack(Record r) => new() {
		masterId = r.masterId, title = r.title, genre = r.primaryGenre, quality = MasterQuality(r),
		hookStrength = r.hookStrength, productionQuality = r.productionQuality, danceability = r.danceability, isReleasedSingle = false,
		songId = r.songId, songSource = r.songSource, isCover = r.isCover, originalRecordId = r.originalRecordId,
		originalArtistId = r.originalArtistId, publisherId = r.publisherId, songwriterNames = r.songwriterNames,
		compositionQuality = r.compositionQuality, compositionHook = r.compositionHook, lyricQuality = r.lyricQuality,
		songFamiliarityAtRelease = r.songFamiliarityAtRelease, standardDurability = r.standardDurability, arrangementOriginality = r.arrangementOriginality
	};

	private sealed class ControlledSingle {
		public string Id;
		public AlbumTrack Track;
		public bool Hit, RelevantHit, Recent;
		public float Freshness;
	}

	/// <summary>
	/// The three propositions for one LP intention, each priced on its actual track list with the existing album margin
	/// model: reused masters pay no second recording fee, and only the included singles enter the hit inventory. Pure:
	/// no draws, no state. The best feasible proposition is chosen; a greatest-hits package needs the hit gate and an
	/// honest track count, and a hit-led LP needs a recent hit not already on a studio LP.
	/// </summary>
	internal int CurrentProgrammeWeekForChecks => pipelineWeek;

	internal LpAssemblyPlan BuildLpAssemblyForChecks(AILabel label, SimulatedArtist artist, ReleaseSlateItem item, int year) =>
		BuildLpAssembly(label, artist, artist, year, BuildDecisionContext(label, artist, year, 6), item);

	private LpAssemblyPlan BuildLpAssembly(AILabel label, SimulatedArtist artist, SimulatedArtist pricingArtist, int year,
		DecisionContext decision, ReleaseSlateItem item) {
		GameDate today = TimeManager.Instance?.CurrentDate ?? new GameDate(year, 1, 1);
		var ledger = ProgrammeState?.MasterControl;
		var singles = new List<ControlledSingle>();
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var catalogue = OpeningMastersFor(artist).Concat((artist.releasedSingleIds ?? new List<string>())
			.Select(id => (Id: id, Track: ChartManager.Instance?.TryGetTrackSnapshot(id, out AlbumTrack live) == true ? live : null)));
		foreach (var (id, track) in catalogue) {
			if (track == null || !seen.Add(id) || ledger == null || !ledger.TryGetValue(id, out var control) || control.LabelId != label.labelId) continue;
			int age = track.releaseDate.year > 0 && today >= track.releaseDate ? today.WeeksDifference(track.releaseDate) : int.MaxValue;
			bool hit = track.peakPosition is >= 1 and <= LpAssembly.RelevantHitPeak;
			singles.Add(new ControlledSingle { Id = id, Track = track, Hit = hit,
				RelevantHit = hit && ChartManager.Instance.GetCompUseCount(id) == 0,
				Recent = age <= LpAssembly.RecentWeeks && control.StudioLpUses == 0 && !control.FromAlbum,
				Freshness = GetSourceHitFreshness(id, track) });
		}
		var shelved = (ProgrammeState?.Shelf ?? new List<ShelvedMaster>()).Where(s => s.LabelId == label.labelId && s.ArtistId == artist.artistId)
			.OrderByDescending(s => MasterQuality(s.Record)).ThenBy(s => s.Record.recordId, StringComparer.Ordinal).ToList();
		static int Peak(ControlledSingle s) => s.Track.peakPosition is >= 1 and <= 100 ? s.Track.peakPosition : 101;
		int trackCount = 9 + Math.Min(3, (int)(ProgrammeRoll(label, artist, item.Id + "|lp-length", item.ProposedWeek, 40) * 4));
		// A new cut's expected quality, from GenerateAlbum's standard-LP formula at its mean draw.
		float spread = AlbumModel.GetTrackSpreadMultiplier(GenreCatalog.Get(artist.primaryGenre).Family, year);
		float newCut = Mathf.Clamp(artist.CalculateBaseQuality() * .88f + label.productionQuality * .12f - .02f * spread, .12f, .95f);
		float allNew = AlbumModel.CalculatePooledAppeal(Enumerable.Repeat(newCut, trackCount), LpAssembly.PricingCohesion, year);

		float Price(List<ControlledSingle> reused, List<ShelvedMaster> shelfMasters, int total, out int newSessions, out float multiplier, out float appealScale) {
			newSessions = Math.Max(0, total - reused.Count - shelfMasters.Count);
			// The same freshness-weighted track appeal the album is sold on: stale hits do not sell a package.
			float appeal = AlbumModel.CalculatePooledAppeal(reused.Select(s => s.Track.quality * s.Freshness)
				.Concat(shelfMasters.Select(s => MasterQuality(s.Record))).Concat(Enumerable.Repeat(newCut, newSessions)), LpAssembly.PricingCohesion, year);
			appealScale = allNew > 0 ? appeal / allNew : 1;
			multiplier = (LpAssembly.NewSessionMultiplier * newSessions + compilationProductionMultiplier * (total - newSessions)) / Math.Max(1, total);
			var inventory = new HitInventory();
			foreach (var s in reused) {
				inventory.idsExamined++; inventory.resolvedSingles++;
				if (Peak(s) > 100) continue;
				inventory.chartedSingles++;
				inventory.hitScore += s.Freshness * (101f - s.Track.peakPosition) / 100f;
			}
			return CalculateAlbumPriorNet(label, pricingArtist, year, decision, 1f, inventory, out _, multiplier, appealScale);
		}
		LpAssemblyPlan Plan(LpProposalKind kind, List<ControlledSingle> reused, List<ShelvedMaster> shelf, int total) {
			float net = Price(reused, shelf, total, out int newSessions, out float multiplier, out float appealScale);
			return new LpAssemblyPlan { Kind = kind, ReleasedMasterIds = reused.Select(s => s.Id).ToList(),
				ShelvedMasterIds = shelf.Select(s => s.Record.recordId).ToList(), TrackCount = total, NewSessions = newSessions,
				ProductionMultiplier = multiplier, ProjectedNet = net, AppealScale = appealScale };
		}

		int studioReuseCap = trackCount / 2; // a studio LP is mostly new work, whatever stock exists
		var ordinaryShelf = shelved.Take(Math.Min(LpAssembly.MaxShelvedOnStudioLp, studioReuseCap)).ToList();
		var ordinary = Plan(LpProposalKind.Ordinary, new(), ordinaryShelf, trackCount);

		var recentHits = singles.Where(s => s.Recent && s.Hit).OrderBy(Peak).ThenBy(s => s.Id, StringComparer.Ordinal).Take(LpAssembly.MaxRecentHits).ToList();
		LpAssemblyPlan hitLed = null;
		if (recentHits.Count > 0) {
			var reused = recentHits.Concat(singles.Where(s => s.Recent && !s.Hit).OrderByDescending(s => s.Track.quality)
				.ThenBy(s => s.Id, StringComparer.Ordinal).Take(LpAssembly.MaxOtherRecentSingles)).Take(studioReuseCap).ToList();
			hitLed = Plan(LpProposalKind.HitLed, reused, shelved.Take(Math.Max(0, Math.Min(LpAssembly.MaxShelvedOnStudioLp, studioReuseCap - reused.Count))).ToList(), trackCount);
		}

		int relevantHits = singles.Count(s => s.RelevantHit);
		LpAssemblyPlan greatestHits = null;
		int? lastPackage = ProgrammeState != null && ProgrammeState.Clocks.TryGetValue(artist.artistId, out var clock) ? clock.PackageWeek : null;
		bool spaced = !lastPackage.HasValue || pipelineWeek - lastPackage.Value >= LpAssembly.PackageGapWeeks;
		if (relevantHits >= LpAssembly.GreatestHitsHits && spaced) {
			// Hits lead, then the rest of the controlled catalogue, then previously unissued masters; no new sessions.
			var reused = singles.OrderByDescending(s => s.RelevantHit).ThenBy(Peak).ThenByDescending(s => s.Track.quality)
				.ThenBy(s => s.Id, StringComparer.Ordinal).Take(trackCount).ToList();
			var shelf = shelved.Take(trackCount - reused.Count).ToList();
			int total = reused.Count + shelf.Count;
			if (total >= LpAssembly.MinGreatestHitsTracks) greatestHits = Plan(LpProposalKind.GreatestHits, reused, shelf, total);
		}

		var chosen = new[] { ordinary, hitLed, greatestHits }.Where(p => p != null)
			.OrderByDescending(p => p.ProjectedNet).ThenByDescending(p => (int)p.Kind).First();
		chosen.RelevantHits = relevantHits; chosen.ControlledSingles = singles.Count;
		chosen.OpeningMasters = singles.Count(s => s.Id.StartsWith("opening:", StringComparison.Ordinal));
		chosen.PackageSpacingBlocked = relevantHits >= LpAssembly.GreatestHitsHits && !spaced;
		chosen.OrdinaryNet = ordinary.ProjectedNet;
		chosen.HitLedFeasible = hitLed != null; chosen.HitLedNet = hitLed?.ProjectedNet ?? 0;
		chosen.GreatestHitsFeasible = greatestHits != null; chosen.GreatestHitsNet = greatestHits?.ProjectedNet ?? 0;
		chosen.GreatestHitsAppealScale = greatestHits?.AppealScale ?? 0;
		chosen.Reason = chosen.Kind switch {
			LpProposalKind.GreatestHits => "earned-catalogue-package",
			LpProposalKind.HitLed => "recent-hit-leads-studio-lp",
			_ => chosen.PackageSpacingBlocked ? "ordinary-package-spacing"
				: relevantHits >= LpAssembly.GreatestHitsHits && greatestHits == null ? "ordinary-catalogue-too-short-for-package"
				: hitLed != null || greatestHits != null ? "ordinary-outprices-reuse" : recentHits.Count == 0 ? "ordinary-no-recent-hit" : "ordinary"
		};
		return chosen;
	}

	// The masters an assembled LP reuses, resolved at session time. Shelved masters are removed from the shelf by the caller
	// once the LP master exists, so they are used once.
	private (List<AlbumTrack> Released, List<AlbumTrack> Shelved) ResolveLpAssemblyTracks(LpAssemblyPlan plan) {
		var released = new List<AlbumTrack>();
		foreach (string id in plan.ReleasedMasterIds) {
			if (ChartManager.Instance.TryResolveTrackSnapshot(id, out AlbumTrack track, out _)) released.Add(track);
			else if (ProgrammeState?.OpeningMasters?.Values.SelectMany(x => x).FirstOrDefault(x => x.sourceRecordId == id) is { } opening) released.Add(opening);
		}
		var shelf = ProgrammeState?.Shelf ?? new List<ShelvedMaster>();
		var shelved = plan.ShelvedMasterIds.Select(id => shelf.FirstOrDefault(s => s.Record.recordId == id)).Where(s => s != null)
			.Select(s => ShelvedTrack(s.Record)).ToList();
		return (released, shelved);
	}

	private void CompleteLpAssembly(ReleaseSlateItem item, AILabel label) {
		var plan = item.LpAssembly;
		ProgrammeState.Shelf?.RemoveAll(s => plan.ShelvedMasterIds.Contains(s.Record.recordId));
		if (ProgrammeState.Shelf?.Count == 0) ProgrammeState.Shelf = null;
		if (plan.Kind == LpProposalKind.GreatestHits && label.roster.FirstOrDefault(a => a.artistId == item.ArtistId) is { } act)
			ProgrammeClockFor(act).PackageWeek = pipelineWeek;
		OnLpAssembly?.Invoke(item, label, pipelineWeek);
	}

	// A studio LP that carries an issued single uses it up for later studio LPs; a hits package instead counts comp uses.
	private void NoteStudioLpUse(Record album) {
		if (!LabelReleaseProgramme.LpAssemblyEnabled || album?.album == null || album.album.albumFormat == AlbumFormat.Compilation) return;
		var ledger = ProgrammeState?.MasterControl;
		if (ledger == null) return;
		foreach (AlbumTrack track in album.album.trackRefs ?? Array.Empty<AlbumTrack>())
			if (track?.sourceRecordId != null && ledger.TryGetValue(track.sourceRecordId, out var control)) control.StudioLpUses++;
	}
}
