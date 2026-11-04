using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What the act and label did about a standards request (§7.3). Each has its own access and credibility consequence.</summary>
public enum StandardsResponse {
	Pending,
	/// <summary>Agree to the lyric or presentation change.</summary>
	ComplyEdit,
	/// <summary>Offer another song: satisfies standards while the original stands untouched elsewhere.</summary>
	AlternateSong,
	/// <summary>Ask the producer to defend the original to the network.</summary>
	ProducerDefends,
	/// <summary>Refuse before confirming: lose the date honestly.</summary>
	Withdraw,
	/// <summary>Promise compliance, then sing the original on air. Only a live microphone can do this.</summary>
	PromiseThenDefy,
	/// <summary>Let the producer cut the segment.</summary>
	AcceptRemoval,
}

/// <summary>Where a standards dispute ended up.</summary>
public enum StandardsResolution { Open, Cleared, Edited, SongChanged, Withdrawn, Defied, Cut }

/// <summary>
/// One standards query (§7.2): the exact issue, the interpreting authority, the requested change, the deadline and the
/// consequence. Raised by an outlet's own policy against the song's authored content tags; never inferred from genre,
/// hair, name or a member's private life.
/// </summary>
public sealed class StandardsQuery {
	public string Id, ProgramId, AppearanceId, ArtistId, ArtistName, LabelId, RecordId, Title;
	public GameDate Raised = GameDate.StartDate, Deadline = GameDate.StartDate, AirDate = GameDate.StartDate;
	public string Tag;
	public ContentSeverity Severity;
	public string Evidence;            // "title word 'Whiskey'"
	public string Authority;           // "CBS program practices", "the station manager"
	public string RequestedChange;     // what they want
	/// <summary>A query an anxious desk raised on a Mild reading, not a clear objection.</summary>
	public bool Anxious;
	/// <summary>The producer is on the act's side; the network still decides (§3.3 Smothers).</summary>
	public bool ProducerSympathetic;
	public bool PlayerQuery;
	public StandardsResponse Response;
	public StandardsResolution Resolution;
	/// <summary>Whether the dispute became public. Unreported rehearsal negotiations are not public events (§7.3).</summary>
	public bool Public;
	public string Outcome;
}

/// <summary>
/// Outlet-specific standards policy (§7.2) and the consequences of each response (§7.3). The same material can be fine
/// on a local dance show and refused on a family variety hour; strictness follows the program, the network, the year
/// and the region. Pure functions plus keyed draws: nothing here reads a global stream or writes an economic value.
/// </summary>
public static class MediaStandards {
	/// <summary>Who reviews a program's material.</summary>
	public static string Authority(TvProgram program) => program.Network switch {
		TvNetwork.CBS => "CBS program practices",
		TvNetwork.NBC => "NBC continuity acceptance",
		TvNetwork.ABC => "ABC standards and practices",
		_ => "the station manager",
	};

	/// <summary>
	/// How strongly a program objects to a tag at a severity, from 0 (never asks) to 1 (always asks). Initial design
	/// values, not measured policy. Family variety is strictest; network teen shows guard sex and drugs; a local dance
	/// show cares about its sponsors and its town.
	/// </summary>
	public static float Strictness(TvProgram program, string tag, ContentSeverity severity, GameDate date, string regionId) {
		bool marked = severity == ContentSeverity.Marked;
		bool late = date.year >= 1967;
		float s = program.Kind switch {
			TvProgramKind.PrimetimeVariety => tag switch {
				SongContentTags.ExplicitLanguage or SongContentTags.SexualSuggestion or SongContentTags.DrugReference => marked ? 1f : 0.55f,
				SongContentTags.ReligiousProvocation => marked ? 0.9f : 0.3f,
				SongContentTags.PoliticalProtest or SongContentTags.AntiWar => marked ? (late ? 0.85f : 0.6f) : 0.15f,
				SongContentTags.ViolenceCrime => marked ? 0.6f : 0.1f,
				SongContentTags.Alcohol => marked ? 0.4f : 0.02f,
				_ => 0f,
			},
			TvProgramKind.NationalTeen => tag switch {
				SongContentTags.ExplicitLanguage or SongContentTags.SexualSuggestion or SongContentTags.DrugReference => marked ? 0.95f : 0.35f,
				SongContentTags.ReligiousProvocation => marked ? 0.6f : 0.1f,
				SongContentTags.PoliticalProtest or SongContentTags.AntiWar => marked ? 0.45f : 0.05f,
				SongContentTags.Alcohol => marked ? 0.35f : 0.05f,
				_ => marked ? 0.2f : 0f,
			},
			_ => tag switch {
				SongContentTags.ExplicitLanguage or SongContentTags.SexualSuggestion or SongContentTags.DrugReference => marked ? 0.8f : 0.1f,
				SongContentTags.ReligiousProvocation => marked ? 0.5f : 0.05f,
				SongContentTags.Alcohol => marked ? 0.2f : 0f,
				_ => marked ? 0.15f : 0f,
			},
		};
		// The Smothers Brothers' network tightened on war and politics as the show went on.
		if (program.Id == "smothers_brothers" && (tag == SongContentTags.PoliticalProtest || tag == SongContentTags.AntiWar))
			s = Math.Min(1f, s + (date.year >= 1968 ? 0.35f : 0.15f));
		// A conservative region's station is warier of religion and sex on its own local program.
		if (program.Network == TvNetwork.Local && regionId == "deepsouth" && (tag == SongContentTags.ReligiousProvocation || tag == SongContentTags.SexualSuggestion))
			s = Math.Min(1f, s + 0.2f);
		return s;
	}

	/// <summary>The song's findings: the composition's authored tags, or the record title read the same way when the
	/// record has no composition behind it. No tags means nothing in the title, never proof of clean content.</summary>
	public static List<SongContentTags.Finding> FindingsFor(RecordRuntimeData record) {
		Record rec = record?.baseRecord;
		if (rec == null) return new();
		SongComposition song = CompositionCatalogService.GetSong(rec.songId);
		return song != null ? SongContentTags.Read(song) : SongContentTags.Classify(rec.title, null, SongContentContext.Unknown);
	}

	/// <summary>
	/// Raise a query for a booking, or null when the desk lets it through. One query per booking: the strongest finding
	/// the desk would ask about. A keyed draw decides the uncertain cases, so an ambiguous word doesn't always trigger a ban.
	/// </summary>
	public static StandardsQuery Review(TvProgram program, GameDate airDate, string appearanceId, RecordRuntimeData record, string regionId, GameDate raised) {
		var findings = FindingsFor(record);
		if (findings.Count == 0) return null;
		// The composition is the lyric's source of truth; when the disc goes out under another name, say which song was read.
		SongComposition song = CompositionCatalogService.GetSong(record.baseRecord.songId);
		string songNote = song != null && !string.Equals(song.title, record.baseRecord.title, StringComparison.OrdinalIgnoreCase) ? $" (the song \"{song.title}\")" : "";
		StandardsQuery best = null;
		float bestStrictness = 0f;
		foreach (var f in findings) {
			float s = Strictness(program, f.Tag, f.Severity, airDate, regionId);
			if (s <= 0f) continue;
			if (MediaAtmosphere.Unit("standards|" + appearanceId + "|" + f.Tag) >= s) continue;
			if (s <= bestStrictness) continue;
			bestStrictness = s;
			Record rec = record.baseRecord;
			best = new StandardsQuery {
				Id = "std|" + appearanceId, ProgramId = program.Id, AppearanceId = appearanceId, ArtistId = rec.artistId, ArtistName = rec.artistName,
				LabelId = rec.labelId, RecordId = rec.recordId, Title = rec.title, Raised = raised, AirDate = airDate,
				Deadline = airDate.AddDays(-Math.Max(1, Math.Min(5, BroadcastExposure.DayIndex(airDate) - BroadcastExposure.DayIndex(raised) - 1))),
				Tag = f.Tag, Severity = f.Severity, Evidence = f.Evidence + songNote, Authority = Authority(program),
				RequestedChange = RequestedChange(f.Tag, program.Method),
				Anxious = f.Severity == ContentSeverity.Mild,
				ProducerSympathetic = program.Id == "smothers_brothers" || program.Network == TvNetwork.Local && MediaAtmosphere.Unit("standards-producer|" + appearanceId) < 0.5f,
			};
		}
		return best;
	}

	private static string RequestedChange(string tag, PerformanceMethod method) {
		if (method == PerformanceMethod.Mime) return "a different record, or a clean version of this one: the dancers mime to the disc, so the words can't change on the day";
		return tag switch {
			SongContentTags.DrugReference => "sing the line without the word they read as a drug reference",
			SongContentTags.SexualSuggestion => "change the suggestive line for the broadcast",
			SongContentTags.ExplicitLanguage => "drop the swear word for the broadcast",
			SongContentTags.PoliticalProtest or SongContentTags.AntiWar => "drop the verse they consider editorializing",
			SongContentTags.ReligiousProvocation => "change the line they consider irreverent",
			_ => "change the line for the broadcast",
		};
	}

	/// <summary>Responses the method and the act's rights actually permit (§7.3, fixture 7). A fixed mime track cannot
	/// sing a different lyric, and cannot be defied on air either.</summary>
	public static IEnumerable<StandardsResponse> Options(StandardsQuery query, PerformanceMethod method, bool hasAlternate) {
		if (method != PerformanceMethod.Mime) yield return StandardsResponse.ComplyEdit;
		if (hasAlternate) yield return StandardsResponse.AlternateSong;
		if (query.ProducerSympathetic) yield return StandardsResponse.ProducerDefends;
		yield return StandardsResponse.Withdraw;
		if (method != PerformanceMethod.Mime) yield return StandardsResponse.PromiseThenDefy;
	}

	/// <summary>
	/// The act's own answer when it has the say (§7.3: artists have agency). A label can recommend; an act with creative
	/// control decides. Read from the existing disposition, never a fresh personality roll.
	/// </summary>
	public static bool ActConsents(SimulatedArtist artist, StandardsResponse proposed, string key) {
		if (artist == null) return true;
		ArtistEvolutionProfile d = artist.evolution;
		float ambition = d?.artisticAmbition ?? 0.5f, pragmatism = d?.commercialPragmatism ?? 0.5f;
		float ego = artist.members?.Where(m => m.isActive).Select(m => m.ego).DefaultIfEmpty(0.5f).Max() ?? 0.5f;
		float u = MediaAtmosphere.Unit("standards-consent|" + key + "|" + proposed);
		return proposed switch {
			// A conspicuous surrender is what an ambitious act resists; a pragmatic one shrugs.
			StandardsResponse.ComplyEdit => u < Math.Clamp(0.55f + 0.5f * (pragmatism - ambition) - 0.25f * (ego - 0.5f) + (artist.artistCreativeControl ? -0.15f : 0.15f), 0.08f, 0.97f),
			StandardsResponse.AlternateSong => u < 0.9f,
			// Defiance is the act's to commit. A label can't order it on someone else's microphone.
			StandardsResponse.PromiseThenDefy => u < Math.Clamp(0.15f + 0.5f * (ambition - pragmatism) + 0.3f * (ego - 0.5f) + (d?.volatility ?? 0.3f) * 0.3f, 0.02f, 0.8f),
			_ => true,
		};
	}

	/// <summary>
	/// What a rival act and its label do with a query (§13 policy, simplified until the Phase 3 planner): a pragmatic act
	/// takes the edit, an ambitious one offers another song or asks the producer, and a rare volatile act with a live
	/// microphone and the creative say promises and then sings the original. Keyed: the same query always gets the same answer.
	/// </summary>
	public static StandardsResponse AiChoice(StandardsQuery query, SimulatedArtist artist, PerformanceMethod method, bool hasAlternate,
		MediaPolicy policy = MediaPolicy.OpportunisticPublicity) {
		var options = Options(query, method, hasAlternate).ToList();
		ArtistEvolutionProfile d = artist?.evolution;
		float ambition = d?.artisticAmbition ?? 0.5f, pragmatism = d?.commercialPragmatism ?? 0.5f, volatility = d?.volatility ?? 0.3f;
		float u = MediaAtmosphere.Unit("standards-ai|" + query.Id);
		bool control = artist?.artistCreativeControl == true || artist?.manager == ManagerArchetype.Visionary;
		// The house's habit comes first (§13), the act's own consent always binds (§7.3): a commercial or cautious house takes
		// the workable edit, an artistic one offers another song or asks the producer to fight, a specialist house would
		// rather send a different record than change one. Opportunists fall through to the act's own disposition.
		switch (policy) {
			case MediaPolicy.CommercialPragmatism or MediaPolicy.CautiousCrisis or MediaPolicy.TeenPopCultivation:
				if (options.Contains(StandardsResponse.ComplyEdit) && ActConsents(artist, StandardsResponse.ComplyEdit, query.Id)) return StandardsResponse.ComplyEdit;
				if (options.Contains(StandardsResponse.AlternateSong)) return StandardsResponse.AlternateSong;
				return policy == MediaPolicy.CautiousCrisis ? StandardsResponse.Withdraw : options.Contains(StandardsResponse.ProducerDefends) ? StandardsResponse.ProducerDefends : StandardsResponse.Withdraw;
			case MediaPolicy.ArtisticProtection:
				if (control && volatility > 0.6f && ambition > pragmatism && u < 0.12f) break; // a volatile act with control makes its own call
				if (options.Contains(StandardsResponse.AlternateSong) && u < 0.5f) return StandardsResponse.AlternateSong;
				if (options.Contains(StandardsResponse.ProducerDefends)) return StandardsResponse.ProducerDefends;
				break;
			case MediaPolicy.SpecialistCommunity:
				if (options.Contains(StandardsResponse.AlternateSong)) return StandardsResponse.AlternateSong;
				break;
		}
		if (options.Contains(StandardsResponse.PromiseThenDefy) && control && volatility > 0.6f && ambition > pragmatism && u < 0.12f)
			return StandardsResponse.PromiseThenDefy;
		if (options.Contains(StandardsResponse.ProducerDefends) && ambition > 0.55f && u < 0.5f) return StandardsResponse.ProducerDefends;
		if (options.Contains(StandardsResponse.AlternateSong) && (control || ambition > pragmatism) && u < 0.75f) return StandardsResponse.AlternateSong;
		if (options.Contains(StandardsResponse.ComplyEdit) && ActConsents(artist, StandardsResponse.ComplyEdit, query.Id)) return StandardsResponse.ComplyEdit;
		if (options.Contains(StandardsResponse.AlternateSong)) return StandardsResponse.AlternateSong;
		return StandardsResponse.Withdraw;
	}

	/// <summary>Whether the network clears the original after the producer argues for it. Producer support and network
	/// clearance can diverge (§7.3); a strict network rarely gives way.</summary>
	public static bool NetworkClears(StandardsQuery query, TvProgram program) {
		float give = program.Network == TvNetwork.Local ? 0.6f : query.Anxious ? 0.45f : 0.18f;
		return MediaAtmosphere.Unit("standards-clear|" + query.Id) < give;
	}

	/// <summary>Whether a taped segment the act defied is caught before broadcast. A live transmission can't be caught.</summary>
	public static bool CaughtOnTape(StandardsQuery query) => MediaAtmosphere.Unit("standards-tape|" + query.Id) < 0.85f;

	/// <summary>Whether a quietly settled dispute leaks into print anyway. Rare for a compliant edit, likelier for a
	/// headline act or a pulled segment; never for an unreported rehearsal note on a small show.</summary>
	public static bool Leaks(StandardsQuery query, TvProgram program, int chartPosition, StandardsResolution resolution) {
		if (program.Network == TvNetwork.Local) return false;
		float p = resolution switch {
			StandardsResolution.Defied => 1f,
			StandardsResolution.Cut => 0.45f,
			StandardsResolution.Withdrawn => 0.15f,
			StandardsResolution.Edited => chartPosition > 0 && chartPosition <= 10 ? 0.25f : 0.06f,
			_ => 0f,
		};
		return MediaAtmosphere.Unit("standards-leak|" + query.Id) < p;
	}
}
