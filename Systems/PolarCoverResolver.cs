using System;
using System.Linq;

/// <summary>Pure proposal, never a commit. Reference, demo, first-master state and rights are untouched.</summary>
public static class PolarCoverResolver {
	public static PolarArrangementProposal Propose(SongComposition song, SongMasterMetadata reference, ActProfile act,
		Genre projectGenre, int year, int week, string plannedMasterId, MarketTasteSnapshot taste, PolarSongTable table) {
		if (song == null || act == null || string.IsNullOrWhiteSpace(plannedMasterId)) throw new ArgumentException("A song, act and stable planned master ID are required.");
		if (reference != null && (reference.songId != song.songId || reference.recordingYear > year)) throw new ArgumentException("Invalid or future reference master.");
		var source = reference?.taxonomy ?? song.demoTaxonomy ?? new SongTaxonomy { primaryGenre = song.primaryGenre, secondaryGenre = song.secondaryGenre };
		var heard = SongProfileDeriver.Derive(song, source, table);
		var pulled = PolarMaterialFit.Pull(heard, act);
		float demandReach = act.interpretiveReach * heard.plasticity * table.N("candidateDemandReach");
		for (int i = 0; i < SongProfile.DemandCount; i++) pulled.axes[i] += Math.Clamp(act.axes[i] - heard.axes[i], -demandReach, demandReach);
		pulled.dominantDemand = pulled.axes.Take(SongProfile.DemandCount).Max();
		var projected = PolarMaterialFit.Evaluate(heard, pulled, act, taste, week, table);
		string family = GenreCatalog.TryGet(projectGenre, out var genre) ? genre.Family.ToString() : GenreFamily.Pop.ToString();
		bool instrumentalReading = LiveRepertoire.AuditPhase>=1 && act.hasKnownPerformers && !act.hasVocalist && source.vocalPresence == SongVocalPresence.Present;
		bool explicitInstrumental = instrumentalReading || source.vocalPresence == SongVocalPresence.Instrumental || (source.lyricModes??Array.Empty<SongLyricMode>()).Contains(SongLyricMode.Instrumental);
		var candidates = table.Archetypes.Where(r => (explicitInstrumental || r.Axes[0]!=0 || r.Axes[1]!=0 || r.Axes[4]!=0) &&
			(LiveRepertoire.AuditPhase>=1 || (int)Enum.Parse<SongArchetype>(r.Name)<=(int)SongArchetype.LivePartyRecord) && r.FromYear <= year && (r.Families.Contains(family) || r.Name == heard.archetype.ToString()))
			.OrderBy(r => r.Name, StringComparer.Ordinal).ToArray();
		SongTaxonomy best = null; SongProfile bestProfile = null;
		float bestDistance = float.PositiveInfinity; ulong bestTie = ulong.MaxValue;
		foreach (var row in candidates) {
			if (heard.plasticity * act.interpretiveReach <= 0) continue;
			var taxonomy = source.Copy(); taxonomy.archetype = Enum.Parse<SongArchetype>(row.Name);
			if(instrumentalReading)taxonomy.vocalPresence=SongVocalPresence.Instrumental;
			taxonomy.primaryGenre = projectGenre;
			taxonomy.secondaryGenre = source.primaryGenre == projectGenre ? source.secondaryGenre : source.primaryGenre;
			taxonomy.pace = row.Pace; taxonomy.mood = row.Mood; taxonomy.mappingId = table.Version;
			if (act.hasVocalist && act.vocalApproach != SongVocalApproach.Unknown) taxonomy.vocalApproaches = new[] { act.vocalApproach };
			if (projected.Stretch >= table.N("lyricFlipStretch") && act.interpretiveReach >= table.N("lyricFlipReach") &&
				(taxonomy.lyricModes ?? Array.Empty<SongLyricMode>()).Contains(SongLyricMode.RomanticAddress))
				taxonomy.lyricModes = family == GenreFamily.RhythmAndSoul.ToString() ?
					new[] { SongLyricMode.DemandBoast, SongLyricMode.CallAndResponse } : new[] { SongLyricMode.DemandBoast };
			var realized = SongProfileDeriver.Derive(song, taxonomy, table); realized.plasticity = heard.plasticity;
			if (PolarMaterialFit.IdentityDistance(realized.axes, heard.axes, table) > heard.plasticity * act.interpretiveReach) continue;
			float demandDistance = 0;
			for (int i = 0; i < SongProfile.DemandCount; i++) { float d = realized.axes[i] - pulled.axes[i]; demandDistance += d * d; }
			float identityDistance = PolarMaterialFit.IdentityDistance(realized.axes, pulled.axes, table);
			float distance = demandDistance / SongProfile.DemandCount + identityDistance * identityDistance * table.N("resolverIdentityWeight");
			// A nearer shape is not plausible when the performers cannot realize its demands.
			var execution = PolarMaterialFit.Evaluate(heard, realized, act, null, week, table);
			distance += (1 - execution.Capability) * table.N("resolverExecutionWeight");
			ulong tie = StableHash(plannedMasterId + "|" + row.Name);
			if (distance < bestDistance - table.N("nearTie") || (Math.Abs(distance - bestDistance) <= table.N("nearTie") && tie < bestTie)) {
				bestDistance = distance; bestTie = tie; best = taxonomy; bestProfile = realized;
			}
		}
		// Empty/era-ineligible pools return an explicit straight-reading proposal, never invent a standard.
		bool fallback = best == null;
		best ??= source.Copy(); bestProfile ??= heard.Copy();
		return new PolarArrangementProposal { plannedMasterId = plannedMasterId, songId = song.songId,
			parentRecordingId = reference?.masterId, mappingVersion = table.Version, taxonomy = best,
			referenceProfile = heard, proposedProfile = pulled, realizedProfile = bestProfile,
			fit = PolarMaterialFit.Evaluate(heard, bestProfile, act, taste, week, table), changedArchetype = bestProfile.archetype != heard.archetype, usedFallback = fallback };
	}
	private static ulong StableHash(string key) {
		// FNV-1a algorithm constants, not gameplay tuning. No RNG stream or mutable counter.
		ulong hash = 14695981039346656037UL;
		foreach (char c in key) { hash ^= c; hash = unchecked(hash * 1099511628211UL); }
		return hash;
	}
}
