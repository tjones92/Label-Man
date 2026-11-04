using System;
using System.Collections.Generic;
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
		pulled.dominantDemand = SongProfileDeriver.MaxDemand(pulled.axes);
		var projected = PolarMaterialFit.Evaluate(heard, pulled, act, taste, week, table);
		string family = GenreCatalog.TryGet(projectGenre, out var genre) ? genre.Family.ToString() : GenreFamily.Pop.ToString();
		bool instrumentalReading = LiveRepertoire.AuditPhase>=1 && act.hasKnownPerformers && !act.hasVocalist && source.vocalPresence == SongVocalPresence.Present;
		bool explicitInstrumental = instrumentalReading || source.vocalPresence == SongVocalPresence.Instrumental || (source.lyricModes??Array.Empty<SongLyricMode>()).Contains(SongLyricMode.Instrumental);
		var candidates = Candidates(table, explicitInstrumental, LiveRepertoire.AuditPhase>=1, family, year, heard.archetype);
		SongTaxonomy best = null; SongProfile bestProfile = null;
		float bestDistance = float.PositiveInfinity; ulong bestTie = ulong.MaxValue;
		float lyricFlipStretch = table.N("lyricFlipStretch"), lyricFlipReach = table.N("lyricFlipReach"), nearTie = table.N("nearTie");
		float identityWeight = table.N("resolverIdentityWeight"), executionWeight = table.N("resolverExecutionWeight");
		ulong tiePrefix = AppendHash(14695981039346656037UL, plannedMasterId + "|");
		// Rows differ only in archetype/pace/mood. Everything else on the reading is the same for each row, so one
		// template carries it and Derive is prepared once; a row's own taxonomy is built only when it leads.
		SongTaxonomy template = null;
		SongProfileDeriver.Prepared prepared = null;
		foreach (var row in candidates) {
			if (heard.plasticity * act.interpretiveReach <= 0) continue;
			SongArchetype archetype = ParseArchetype(row.Name);
			if (template == null) {
				// It only reassigns fields, so it can share the source's arrays; the winner is deep-copied below.
				template = source.ShallowCopy();
				if(instrumentalReading)template.vocalPresence=SongVocalPresence.Instrumental;
				template.primaryGenre = projectGenre;
				template.secondaryGenre = source.primaryGenre == projectGenre ? source.secondaryGenre : source.primaryGenre;
				template.mappingId = table.Version;
				if (act.hasVocalist && act.vocalApproach != SongVocalApproach.Unknown) template.vocalApproaches = new[] { act.vocalApproach };
				if (projected.Stretch >= lyricFlipStretch && act.interpretiveReach >= lyricFlipReach &&
					(template.lyricModes ?? Array.Empty<SongLyricMode>()).Contains(SongLyricMode.RomanticAddress))
					template.lyricModes = family == GenreFamily.RhythmAndSoul.ToString() ?
						new[] { SongLyricMode.DemandBoast, SongLyricMode.CallAndResponse } : new[] { SongLyricMode.DemandBoast };
				prepared = SongProfileDeriver.Prepare(song, template, table);
			}
			var realized = SongProfileDeriver.Derive(prepared, archetype, projectGenre); realized.plasticity = heard.plasticity;
			if (PolarMaterialFit.IdentityDistance(realized.axes, heard.axes, table) > heard.plasticity * act.interpretiveReach) continue;
			float demandDistance = 0;
			for (int i = 0; i < SongProfile.DemandCount; i++) { float d = realized.axes[i] - pulled.axes[i]; demandDistance += d * d; }
			float identityDistance = PolarMaterialFit.IdentityDistance(realized.axes, pulled.axes, table);
			float distance = demandDistance / SongProfile.DemandCount + identityDistance * identityDistance * identityWeight;
			// A nearer shape is not plausible when the performers cannot realize its demands.
			distance += (1 - PolarMaterialFit.Capability(realized, act, table)) * executionWeight;
			ulong tie = AppendHash(tiePrefix, row.Name);
			if (distance < bestDistance - nearTie || (Math.Abs(distance - bestDistance) <= nearTie && tie < bestTie)) {
				bestDistance = distance; bestTie = tie; bestProfile = realized;
				best = template.ShallowCopy(); best.archetype = archetype; best.pace = row.Pace; best.mood = row.Mood;
			}
		}
		// Empty/era-ineligible pools return an explicit straight-reading proposal, never invent a standard.
		bool fallback = best == null;
		best = (best ?? source).Copy(); bestProfile ??= heard.Copy();
		return new PolarArrangementProposal { plannedMasterId = plannedMasterId, songId = song.songId,
			parentRecordingId = reference?.masterId, mappingVersion = table.Version, taxonomy = best,
			referenceProfile = heard, proposedProfile = pulled, realizedProfile = bestProfile,
			fit = PolarMaterialFit.Evaluate(heard, bestProfile, act, taste, week, table), changedArchetype = bestProfile.archetype != heard.archetype, usedFallback = fallback };
	}
	// The archetype rows a reading may move to. Pure in its arguments and the (immutable) table, and asked once per
	// candidate song per material choice, so each distinct question is answered once.
	private static PolarSongTable candidateTable;
	private static readonly Dictionary<(bool, bool, string, int, SongArchetype), PolarArchetypeRow[]> candidateCache = new();
	private static PolarArchetypeRow[] Candidates(PolarSongTable table, bool explicitInstrumental, bool phase1, string family, int year, SongArchetype heard) {
		if (!ReferenceEquals(candidateTable, table)) { candidateCache.Clear(); candidateTable = table; }
		var key = (explicitInstrumental, phase1, family, year, heard);
		if (candidateCache.TryGetValue(key, out var rows)) return rows;
		string heardName = heard.ToString();
		rows = table.Archetypes.Where(r => (explicitInstrumental || r.Axes[0]!=0 || r.Axes[1]!=0 || r.Axes[4]!=0) &&
			(phase1 || (int)ParseArchetype(r.Name)<=(int)SongArchetype.LivePartyRecord) && r.FromYear <= year && (r.Families.Contains(family) || r.Name == heardName))
			.OrderBy(r => r.Name, StringComparer.Ordinal).ToArray();
		candidateCache.Add(key, rows);
		return rows;
	}
	private static readonly Dictionary<string, SongArchetype> parsedArchetypes = new(StringComparer.Ordinal);
	private static SongArchetype ParseArchetype(string name) {
		if (name != null && parsedArchetypes.TryGetValue(name, out var archetype)) return archetype;
		archetype = Enum.Parse<SongArchetype>(name);
		parsedArchetypes[name] = archetype;
		return archetype;
	}
	// FNV-1a over a prefix's state (offset basis 14695981039346656037), not gameplay tuning. No RNG stream or mutable counter.
	private static ulong AppendHash(ulong hash, string text) {
		foreach (char c in text) { hash ^= c; hash = unchecked(hash * 1099511628211UL); }
		return hash;
	}
}
