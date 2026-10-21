using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum ShadowReadiness { Offered, Recordable }
public enum ShadowDisposition { Active, Archived, ShadowIssued }

/// <summary>
/// One song an AI act has access to, in the Phase 1 shadow. A catalogue song is referenced by <see cref="SongId"/> and never
/// re-registered; a shadow original has no catalogue entry yet, only fixed traits and a fixed title seed, so a later phase
/// can author it once without rerolling (directive risk 6).
/// </summary>
public sealed class ShadowCandidate {
	public string Id, ArtistId, SongId, Provenance;
	public SongMaterialSource Source;
	public ulong TitleSeed;
	public int ArrivalWeek, ReadyWeek, DispositionWeek = -1;
	public ShadowReadiness Readiness;
	public ShadowDisposition Disposition;
	public string DispositionReason;
	/// <summary>0 = an unheard offer; 1 = a demo or rehearsal heard. A read changes only with new evidence, never by waiting.</summary>
	public int EvidenceVersion;
	/// <summary>Composition traits, fixed when the song reached the act.</summary>
	public float Craft, Hook, Lyric, Originality;
	/// <summary>Polar fit at the last evidence event. Own material is not evaluated and keeps full capability.</summary>
	public bool FitEvaluated;
	public float Capability = 1f, Identity, Moment;
}

public sealed class ShadowActRepertoire {
	public string ArtistId, LabelId;
	public int FirstSeenWeek, LastSignedWeek, Sequence;
	public List<ShadowCandidate> Candidates = new();
}

/// <summary>Saved Phase 1 state. Absent from saves written without the shadow, so those saves are unchanged.</summary>
public sealed class AiRepertoireSaveData {
	public int ConfigVersion { get; set; }
	public int LastStepWeek { get; set; } = -1;
	public Dictionary<string, ShadowActRepertoire> Acts { get; set; } = new();
	/// <summary>Lifetime tallies (arrivals by source, archives by reason, shadow issues). Diagnostics only.</summary>
	public Dictionary<string, long> Counters { get; set; } = new();
}

/// <summary>What the label would have judged best at one legacy single release. The Audit* fields are true expectations
/// for calibration reads only; nothing that ranks candidates reads them.</summary>
public sealed class ShadowJudgement {
	public int Week, Year;
	public string LabelId, ArtistId, RecordId;
	public LabelTier Tier;
	public ReleaseStrategy Strategy;
	public float LegacyHook, LegacyProduction;
	public int Active, Recordable, Offered;
	public string BestId, BestSongId, BestProvenance;
	public SongMaterialSource BestSource;
	public int BestAgeWeeks = -1;
	public float BestPerceived = -1f, ReadBand;
	public float AuditBestExpectedQuality = -1f, AuditBestExpectedHook = -1f, AuditBestExpectedProduction = -1f, AuditBestSpread = -1f;
	public float AuditOracleExpectedQuality = -1f, AuditMeanRecordableExpected = -1f;
	/// <summary>Phase 2A: how this single's material was chosen ("observe" when selection is off; "pool", "fallback-empty",
	/// "fallback-refused", "placement" or "observe-promo" when on) and which shortlist song was consumed, if any.</summary>
	public ReleaseSelectMode Mode;
	public string Outcome = "observe", SelectedId;
	public SongMaterialSource SelectedSource;
}

public sealed class ShadowMaterialEvent {
	public int Week;
	public string Kind, ArtistId, LabelId, CandidateId, SongId, Provenance, Reason;
	public SongMaterialSource Source;
	public int EvidenceVersion;
}

/// <summary>The completed week's shadow totals, for the audit runner. Not saved.</summary>
public sealed class ShadowRepertoireHealth {
	public int Week, Year, ActsTracked, ActsSigned, Active, Recordable, Offered, ActsWithRecordable;
	public int Archived, Issued, Initialized, Untracked;
	public readonly Dictionary<SongMaterialSource, int> Arrivals = new();
	public readonly Dictionary<string, int> ArchivedByReason = new(StringComparer.Ordinal);
}

/// <summary>
/// Release Judgement Phase 1 (observe-only): a bounded, saved song shortlist per AI act. Material arrives through dated,
/// keyed events (writing, professional offers, covers, traditional), becomes recordable after a demo or rehearsal delay,
/// and is read by the act's current label through a persistent bias plus song-specific error that scales with scouting.
/// At every legacy single release the best perceived recordable song is logged and marked as counterfactually issued, so
/// depletion is visible. Nothing here is read by the economy; no catalogue song, title, crew, cash or global RNG is touched.
/// Called as an explicit step of <c>ChartManager.OnWeekEnded</c> and at the two AI single release sites.
/// </summary>
public static class AiRepertoireService {
	private static readonly SongMaterialSource[] ArrivalSources = {
		SongMaterialSource.ArtistWritten, SongMaterialSource.ExternalProfessional, SongMaterialSource.CoverStandard,
		SongMaterialSource.CoverRecentHit, SongMaterialSource.TraditionalPublicDomain
	};

	private static AiRepertoireSaveData state = new();
	// The week being accumulated (releases during the week, then the week-end step) and the last completed one.
	private static ShadowRepertoireHealth current = new(), health;

	// Phase 2A: the shortlist song a record was cut from, between generation and release (same tick; never saved).
	// A record that fails its affordability check never releases, so its song stays on the shortlist.
	private static readonly Dictionary<string, (string CandidateId, string Outcome)> pendingPicks = new(StringComparer.Ordinal);

	public static event Action<ShadowJudgement> OnJudgement;
	public static event Action<ShadowMaterialEvent> OnMaterialEvent;
	public static ShadowRepertoireHealth LastHealth => health;
	public static IReadOnlyDictionary<string, ShadowActRepertoire> Acts => state.Acts;
	public static IReadOnlyDictionary<string, long> Counters => state.Counters;
	public static bool HasState => state.LastStepWeek >= 0 || state.Acts.Count > 0;

	// ---- the weekly step -------------------------------------------------------------------------------------

	/// <summary>Runs once per completed chart week, after settlement and media, from information that week left behind.</summary>
	public static void AfterCompletedWeek(GameDate date) {
		if (!ReleaseJudgement.Shadow || ChartManager.Instance == null) return;
		Step(date, ChartManager.Instance.GetCurrentChartWeek());
	}

	/// <summary>The weekly step at an explicit chart week (probes drive it without simulating the economy).</summary>
	internal static void Step(GameDate date, int week) {
		if (!ReleaseJudgement.Shadow || ChartManager.Instance == null || week <= state.LastStepWeek) return;
		pendingPicks.Clear();
		state.ConfigVersion = ReleaseJudgement.ConfigVersion;
		var seen = new HashSet<string>(StringComparer.Ordinal);
		foreach (AILabel label in ChartManager.Instance.GetAllLabels().Where(Trackable).OrderBy(l => l.labelId, StringComparer.Ordinal)) {
			foreach (SimulatedArtist artist in label.roster.Where(a => a != null && a.members != null && a.members.Count > 0)
					.OrderBy(a => a.artistId, StringComparer.Ordinal)) {
				if (!seen.Add(artist.artistId)) continue;
				ShadowActRepertoire act = Ensure(label, artist, week, date.year);
				act.LabelId = label.labelId;
				act.LastSignedWeek = week;
				AdvanceReadiness(act, label, artist, week, date.year);
				Arrive(act, label, artist, week, date.year);
				EnforceCap(act, label, artist, week);
			}
		}
		Prune(week, seen);
		current.Week = week; current.Year = date.year;
		Tally(current, seen);
		health = current;
		current = new ShadowRepertoireHealth();
		state.LastStepWeek = week;
	}

	private static bool Trackable(AILabel label) => label != null && !label.isPlayerOwned && label.roster != null &&
		label.status != LabelStatus.Defunct && label.status != LabelStatus.Bankrupt;

	private static ShadowActRepertoire Ensure(AILabel label, SimulatedArtist artist, int week, int year) {
		if (state.Acts.TryGetValue(artist.artistId, out var act)) return act;
		act = new ShadowActRepertoire { ArtistId = artist.artistId, LabelId = label.labelId, FirstSeenWeek = week, LastSignedWeek = week };
		state.Acts[artist.artistId] = act;
		current.Initialized++;
		Count("initialized");
		// Seed from what an act plausibly already plays: the same arrival rules, forced, already rehearsed.
		for (int i = 0; i < ReleaseJudgement.Parameters.InitialStock; i++) {
			SongMaterialSource source = PickSource(artist, label, year, $"init|{artist.artistId}|{i}");
			ShadowCandidate candidate = Create(act, label, artist, source, week, year, "initialization", $"init|{i}");
			if (candidate == null) continue;
			candidate.ReadyWeek = week;
			MakeRecordable(candidate, label, artist, week, year);
		}
		return act;
	}

	private static void AdvanceReadiness(ShadowActRepertoire act, AILabel label, SimulatedArtist artist, int week, int year) {
		foreach (ShadowCandidate c in act.Candidates) {
			if (c.Disposition != ShadowDisposition.Active) continue;
			if (c.Readiness == ShadowReadiness.Offered && c.ReadyWeek <= week) MakeRecordable(c, label, artist, week, year);
			if (c.Disposition == ShadowDisposition.Active && c.Source == SongMaterialSource.CoverRecentHit &&
				week - c.ArrivalWeek >= ReleaseJudgement.Parameters.RecentHitWindowWeeks) Archive(c, act, week, "window-closed");
		}
	}

	private static void MakeRecordable(ShadowCandidate c, AILabel label, SimulatedArtist artist, int week, int year) {
		c.Readiness = ShadowReadiness.Recordable;
		c.EvidenceVersion = 1;
		SongComposition song = CompositionCatalogService.GetSong(c.SongId);
		// v3: an unauthored original is heard as a provisional demo (never registered), so its capability is read like any
		// other song's rather than assumed perfect. Realization later applies the authored composition's own fit.
		SongComposition heard = song ?? (c.Source == SongMaterialSource.ArtistWritten ? ProvisionalDemo(c, artist, year) : null);
		if (heard != null && PolarSongBehavior.UsePolarFitSelection) {
			MaterialFit fit = PolarSongBehavior.Fit(heard, artist, ProjectGenre(artist, year), year, label: label);
			c.FitEvaluated = true;
			c.Capability = fit.Capability; c.Identity = fit.Identity; c.Moment = fit.Moment;
			// The act's existing refusal rule still binds on catalogue material; an AI act always has a songbook.
			if (song != null && PolarSongBehavior.Refuses(fit, artist, emptySongbook: false)) {
				Archive(c, state.Acts[c.ArtistId], week, "refused");
				return;
			}
		}
		Emit("ready", c, label.labelId, week, null);
	}

	/// <summary>A local composition for an original the act has written but nobody has recorded: the same traits it will be
	/// authored with, the act's genre, no registry entry and no RNG (mirrors the player's provisional demo).</summary>
	private static SongComposition ProvisionalDemo(ShadowCandidate c, SimulatedArtist artist, int year) {
		Genre genre = ProjectGenre(artist, year);
		var demo = new SongComposition {
			songId = "rj-demo:" + c.Id, primaryGenre = genre, secondaryGenre = artist.secondaryGenre, originYear = year,
			originKind = SongOriginKind.ArtistOriginal, compositionQuality = c.Craft, melodicStrength = c.Craft,
			lyricQuality = c.Lyric, commercialHook = c.Hook, originality = c.Originality,
			adaptability = Mathf.Clamp(c.Originality * 0.6f + 0.2f, 0f, 1f),
			demoTaxonomy = new SongTaxonomy { primaryGenre = genre, secondaryGenre = artist.secondaryGenre }
		};
		CompositionShapeVariation.Ensure(demo);
		return demo;
	}

	private static void Arrive(ShadowActRepertoire act, AILabel label, SimulatedArtist artist, int week, int year) {
		foreach (SongMaterialSource source in ArrivalSources) {
			float rate = ArrivalRate(source, label, artist, year);
			if (rate <= 0f || ReleaseJudgement.Unit($"arrive|{artist.artistId}|{week}|{source}") >= rate) continue;
			Create(act, label, artist, source, week, year, "arrival", $"w{week}|{source}");
		}
	}

	private static void EnforceCap(ShadowActRepertoire act, AILabel label, SimulatedArtist artist, int week) {
		while (act.Candidates.Count(c => c.Disposition == ShadowDisposition.Active) > ReleaseJudgement.Parameters.ShortlistCap) {
			ShadowCandidate weakest = act.Candidates.Where(c => c.Disposition == ShadowDisposition.Active)
				.OrderBy(c => Read(c, label, artist, out _)).ThenBy(c => c.Id, StringComparer.Ordinal).First();
			Archive(weakest, act, week, "passed-over");
		}
	}

	private static void Prune(int week, HashSet<string> signed) {
		var drop = new List<string>();
		foreach (var pair in state.Acts.OrderBy(p => p.Key, StringComparer.Ordinal)) {
			pair.Value.Candidates.RemoveAll(c => c.Disposition != ShadowDisposition.Active &&
				week - c.DispositionWeek > ReleaseJudgement.Parameters.ArchiveRetentionWeeks);
			if (!signed.Contains(pair.Key) && week - pair.Value.LastSignedWeek > ReleaseJudgement.Parameters.UntrackAfterWeeks) drop.Add(pair.Key);
		}
		foreach (string id in drop) state.Acts.Remove(id);
		current.Untracked += drop.Count;
	}

	private static void Tally(ShadowRepertoireHealth health, HashSet<string> signed) {
		health.ActsTracked = state.Acts.Count;
		health.ActsSigned = signed.Count;
		foreach (var act in state.Acts.Values) {
			bool any = false;
			foreach (var c in act.Candidates) {
				if (c.Disposition != ShadowDisposition.Active) continue;
				health.Active++;
				if (c.Readiness == ShadowReadiness.Recordable) { health.Recordable++; any = true; } else health.Offered++;
			}
			if (any) health.ActsWithRecordable++;
		}
	}

	// ---- arrivals ----------------------------------------------------------------------------------------------

	/// <summary>
	/// How readily publishers' pluggers bring a label professional songs: its own A&amp;R strength, not its tier. v3 used a
	/// tier table (Major 0.90 .. Small 0.25), which gave Majors ~1.9x the professional arrivals of a Small label, deeper
	/// shortlists and most of 2A's head drift (2026-10-19 five-seed screen). Scouting is drawn per label from overlapping
	/// tier envelopes, so Majors still lead on average. 0.63 holds the act-weighted mean arrival multiplier at v3's (~0.93).
	/// </summary>
	private static float LabelAccess(AILabel label) => 0.63f * Mathf.Clamp(label.scoutingAbility, 0f, 1f);

	private static Genre ProjectGenre(SimulatedArtist artist, int year) => GenreCatalog.MapLegacy(artist.primaryGenre, year);

	/// <summary>SourceMix shapes which opportunities ARRIVE (directive §2), not a lottery at release.</summary>
	internal static float ArrivalRate(SongMaterialSource source, AILabel label, SimulatedArtist artist, int year) {
		Genre genre = ProjectGenre(artist, year);
		SourceShares shares = SongMaterialSelectionService.GetSourceMixShares(genre, year, artist.primaryGenre);
		float b = ReleaseJudgement.Parameters.BaseArrivalRate;
		return source switch {
			SongMaterialSource.ArtistWritten => b * shares.Aw * (0.4f + 1.2f * SongMaterialSelectionService.ArtistWriterCraft(artist)),
			SongMaterialSource.ExternalProfessional => SongMaterialSelectionService.GetProfessionalMaterialAvailability(genre, year) <= 0.01f ? 0f
				: b * shares.Pro * (0.5f + LabelAccess(label)),
			SongMaterialSource.CoverStandard => b * shares.Std,
			SongMaterialSource.CoverRecentHit => b * shares.Hit,
			SongMaterialSource.TraditionalPublicDomain => b * shares.Trad,
			_ => 0f
		};
	}

	private static SongMaterialSource PickSource(SimulatedArtist artist, AILabel label, int year, string key) {
		float[] rates = ArrivalSources.Select(s => ArrivalRate(s, label, artist, year)).ToArray();
		float total = rates.Sum();
		if (total <= 0f) return SongMaterialSource.ArtistWritten;
		float roll = ReleaseJudgement.Unit(key + "|source") * total;
		for (int i = 0; i < rates.Length; i++) { roll -= rates[i]; if (roll < 0f) return ArrivalSources[i]; }
		return ArrivalSources[^1];
	}

	private static IReadOnlyList<SongComposition> PoolFor(SongMaterialSource source, Genre genre) {
		GenreFamily family = GenreCatalog.Get(genre).Family;
		IReadOnlyList<SongComposition> Either(IReadOnlyList<SongComposition> byGenre, IReadOnlyList<SongComposition> byFamily) =>
			byGenre.Count > 0 ? byGenre : byFamily;
		return source switch {
			SongMaterialSource.ExternalProfessional => CompositionCatalogService.GetProfessionalForGenre(genre),
			SongMaterialSource.CoverStandard => Either(CompositionCatalogService.GetStandardsForGenre(genre), CompositionCatalogService.GetStandardsForFamily(family)),
			SongMaterialSource.CoverRecentHit => Either(CompositionCatalogService.GetCoverableHitsForGenre(genre), CompositionCatalogService.GetCoverableHitsForFamily(family)),
			SongMaterialSource.TraditionalPublicDomain => Either(CompositionCatalogService.GetTraditionalForGenre(genre), CompositionCatalogService.GetTraditionalForFamily(family)),
			_ => Array.Empty<SongComposition>()
		};
	}

	private static ShadowCandidate Create(ShadowActRepertoire act, AILabel label, SimulatedArtist artist, SongMaterialSource source,
		int week, int year, string provenance, string key) {
		var c = new ShadowCandidate {
			ArtistId = artist.artistId, Source = source, Provenance = provenance, ArrivalWeek = week,
			Readiness = ShadowReadiness.Offered, Disposition = ShadowDisposition.Active
		};
		if (source == SongMaterialSource.ArtistWritten) {
			Genre genre = ProjectGenre(artist, year);
			float craft = SongMaterialSelectionService.ArtistWriterCraft(artist);
			float era = SongMaterialSelectionService.GetArtistWrittenEraWeight(genre, year);
			float noise = (ReleaseJudgement.Unit($"craft|{artist.artistId}|{key}") - 0.5f) * 0.10f;
			c.Craft = Mathf.Clamp(craft * 0.80f + era * 0.10f + noise, 0f, 1f);
			c.Hook = ArtistWrittenHookScale.Hook(c.Craft, craft, noise, MeanPerformanceHook(artist, label), 0f);
			c.Lyric = Mathf.Clamp(craft * 0.70f + era * 0.10f, 0f, 1f);
			c.Originality = Mathf.Clamp(artist.members.Count > 0 ? artist.members[0].creativity * 0.6f + 0.2f : 0.4f, 0f, 1f);
			c.TitleSeed = RepertoireTaxonomy.Hash(BandLife.WorldSeed.ToString(System.Globalization.CultureInfo.InvariantCulture) +
				$"|release-judgement|title|{artist.artistId}|{key}");
		} else {
			SongComposition song = PickSong(act, source, artist, year, key);
			if (song == null) { Count("empty-pool:" + source); return null; }
			c.SongId = song.songId;
			c.Craft = song.compositionQuality; c.Hook = song.commercialHook; c.Lyric = song.lyricQuality; c.Originality = song.originality;
		}
		act.Sequence++;
		c.Id = $"rj:{artist.artistId}:{act.Sequence}";
		int delay = ReadinessDelay(source, artist.artistId, key);
		c.ReadyWeek = week + delay;
		act.Candidates.Add(c);
		Count("arrival:" + source);
		current.Arrivals[source] = current.Arrivals.GetValueOrDefault(source) + 1;
		Emit("arrival", c, label.labelId, week, provenance);
		return c;
	}

	private static SongComposition PickSong(ShadowActRepertoire act, SongMaterialSource source, SimulatedArtist artist, int year, string key) {
		IReadOnlyList<SongComposition> pool = PoolFor(source, ProjectGenre(artist, year));
		if (pool.Count == 0) return null;
		for (int attempt = 0; attempt < 4; attempt++) {
			int index = Math.Min(pool.Count - 1, (int)(ReleaseJudgement.Unit($"pick|{artist.artistId}|{key}|{attempt}") * pool.Count));
			SongComposition song = pool[index];
			if (song == null || song.originYear > year) continue;
			if (source == SongMaterialSource.CoverRecentHit && song.originYear < year - 3) continue;
			if (act.Candidates.Any(c => c.Disposition == ShadowDisposition.Active && c.SongId == song.songId)) continue;
			return song;
		}
		return null;
	}

	/// <summary>Weeks from an offer to a recordable demo or rehearsal.</summary>
	private static int ReadinessDelay(SongMaterialSource source, string artistId, string key) {
		(int min, int max) = source switch {
			SongMaterialSource.ArtistWritten => (1, 3),
			SongMaterialSource.ExternalProfessional => (1, 2),
			SongMaterialSource.CoverRecentHit => (1, 3),
			_ => (1, 4)
		};
		return min + (int)(ReleaseJudgement.Unit($"ready|{artistId}|{key}") * (max - min + 1));
	}

	// ---- expectation and perception ----------------------------------------------------------------------------

	private static float StudioMod(AILabel label) {
		if (label.strongRegions == null || label.strongRegions.Length == 0) return 1f;
		var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
		return region != null ? ChartSimulator.GetStudioQualityModifier(region) : 1f;
	}

	/// <summary>The mean of GenerateRecordFromArtist's performance hook, without its draws.</summary>
	private static float MeanPerformanceHook(SimulatedArtist artist, AILabel label) {
		float studioMod = StudioMod(label);
		float baseQuality = SingleStudioModifier.BaseQuality(artist.CalculateBaseQuality(), studioMod);
		return Mathf.Clamp(artist.songwritingAbility * 0.55f + baseQuality * 0.35f + 0.03f, 0f, 1f);
	}

	/// <summary>
	/// The expected shipped hook and production of recording this candidate with this act and label, by the same
	/// realization the single path applies, with the session draws at their means. True expectation: audit and read base.
	/// </summary>
	internal static float ExpectedRecording(ShadowCandidate c, SimulatedArtist artist, AILabel label,
		out float hook, out float production, out float spread) {
		float studioMod = StudioMod(label);
		float performanceHook = MeanPerformanceHook(artist, label);
		production = Mathf.Clamp(label.productionQuality * 0.4f + artist.studioPerformance * 0.3f + studioMod * 0.2f + 0.025f, 0f, 1f);
		const float hookSd = 0.0866f, productionSd = 0.0433f;   // U(-0.12, 0.18) and U(-0.05, 0.10)
		if (PolarSongBehavior.UsePolarFitSelection && !PolarSongBehavior.AuditLegacyRealization) {
			float execution = PolarSongBehavior.ExecutionFactor(c.Capability);
			hook = Mathf.Clamp(PolarSongBehavior.RealizeHook(performanceHook, c.Hook) * execution, 0f, 1f);
			production = Mathf.Clamp(production * execution, 0f, 1f);
			float blend = PolarSongTable.Current.N("recordingHookBlend");
			spread = 0.5f * Mathf.Sqrt(Sq(hookSd * (1f - blend) * execution) + Sq(productionSd * execution));
		} else {
			float authority = c.Source switch {
				SongMaterialSource.ArtistWritten => 0.50f, SongMaterialSource.ExternalProfessional => 0.68f,
				SongMaterialSource.CoverRecentHit => 0.72f, SongMaterialSource.CoverStandard => 0.62f, _ => 0.50f
			};
			float polish = c.Source == SongMaterialSource.ExternalProfessional ? Mathf.Clamp(c.Craft * 0.7f + LabelAccess(label) * 0.3f, 0f, 1f) : 0f;
			hook = Mathf.Clamp(Mathf.Lerp(performanceHook, c.Hook, authority) + polish * 0.04f, 0f, 1f);
			production = Mathf.Clamp(production + polish * 0.035f, 0f, 1f);
			spread = 0.5f * Mathf.Sqrt(Sq(hookSd * (1f - authority)) + Sq(productionSd));
		}
		return (hook + production) / 2f;
	}

	private static float Sq(float x) => x * x;

	internal static float ReadBand(AILabel label, ShadowCandidate c) {
		float band = Mathf.Lerp(ReleaseJudgement.Parameters.ReadBandMax, ReleaseJudgement.Parameters.ReadBandMin, Mathf.Clamp(label.scoutingAbility, 0f, 1f));
		return c.Readiness == ShadowReadiness.Offered ? band * ReleaseJudgement.Parameters.UnheardBandScale : band;
	}

	/// <summary>
	/// The label's read of a candidate's recorded quality: truth plus a persistent label/act bias and an error fixed per
	/// (label, candidate, evidence version). Rereading, reordering or waiting returns the same value.
	/// </summary>
	internal static float Read(ShadowCandidate c, AILabel label, SimulatedArtist artist, out float band) {
		band = ReadBand(label, c);
		float expected = ExpectedRecording(c, artist, label, out _, out _, out _);
		float share = ReleaseJudgement.Parameters.BiasShare;
		float bias = (ReleaseJudgement.Unit($"bias|{label.labelId}|{artist.artistId}") * 2f - 1f) * band * share;
		float error = (ReleaseJudgement.Unit($"read|{label.labelId}|{c.Id}|e{c.EvidenceVersion}") * 2f - 1f) * band * (1f - share);
		return Mathf.Clamp(expected + bias + error, 0f, 1f);
	}

	// ---- the release observation -------------------------------------------------------------------------------

	/// <summary>
	/// A legacy AI single just shipped. Judge the act's shortlist as its label would have, log it, and mark the chosen
	/// song as counterfactually issued so the shortlist depletes as a real one would. Writes shadow state only.
	/// </summary>
	public static void ObserveSingleRelease(AILabel label, SimulatedArtist artist, Record record, ReleaseStrategy strategy, GameDate date) {
		if (!ReleaseJudgement.Shadow || label == null || artist == null || record == null || label.isPlayerOwned) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		ShadowJudgement judgement = Judge(label, artist, date.year, week);
		judgement.RecordId = record.recordId;
		judgement.Strategy = strategy;
		judgement.LegacyHook = record.hookStrength;
		judgement.LegacyProduction = record.productionQuality;
		judgement.Mode = ReleaseJudgement.Select;
		// Selection on: an orphan consumes exactly the song it was cut from (none on a fallback). A promo single still
		// consumes the best perceived song counterfactually, as calibrated: the LP project spends the act's ready material.
		string consume;
		if (pendingPicks.Remove(record.recordId, out var pick)) { consume = pick.CandidateId; judgement.Outcome = pick.Outcome; }
		else { consume = judgement.BestId; judgement.Outcome = ReleaseJudgement.Selecting ? "observe-promo" : "observe"; }
		judgement.SelectedId = consume;
		if (consume != null) {
			ShadowActRepertoire act = state.Acts[artist.artistId];
			ShadowCandidate best = act.Candidates.First(c => c.Id == consume);
			judgement.SelectedSource = best.Source;
			best.Disposition = ShadowDisposition.ShadowIssued;
			best.DispositionWeek = week;
			best.DispositionReason = strategy.ToString();
			Count("issued");
			current.Issued++;
			Emit("issued", best, label.labelId, week, strategy.ToString());
		} else Count("issued-empty");
		OnJudgement?.Invoke(judgement);
	}

	// ---- Phase 2A: selection ------------------------------------------------------------------------------------

	/// <summary>The ranking score for one recordable candidate. Perceived: the label's read of the recorded quality.
	/// Fit: the exact polar selection score alone, with no song-quality read (the directive's fit-only comparison).</summary>
	internal static float SelectionScore(ShadowCandidate c, AILabel label, SimulatedArtist artist, ReleaseSelectMode mode) =>
		mode == ReleaseSelectMode.Fit
			? PolarSongBehavior.SelectionScore(new MaterialFit(c.Capability, c.Identity, c.Moment, 0f, default, 0f, 0f, false))
			: Read(c, label, artist, out _);

	/// <summary>
	/// Release Judgement 2A. For a live AI orphan single, take the act's best recordable shortlist song under the active
	/// ranking and build its material through the fixed-composition routes, realizing that composition once. An original
	/// is authored on its first recording and keeps that composition for any later take. Returns null (legacy fallback)
	/// when nothing is recordable or the session refuses the top songs. Draws nothing from the global stream.
	/// </summary>
	internal static SelectedSongMaterial SelectMaterial(AILabel label, SimulatedArtist artist, Record record, Genre genre, int year) {
		if (!ReleaseJudgement.Selecting || label == null || label.isPlayerOwned || artist == null || record == null) return null;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		ShadowActRepertoire act = Ensure(label, artist, week, year);
		ReleaseSelectMode mode = ReleaseJudgement.Select;
		var ranked = act.Candidates
			.Where(c => c.Disposition == ShadowDisposition.Active && c.Readiness == ShadowReadiness.Recordable)
			.Select(c => (Candidate: c, Score: SelectionScore(c, label, artist, mode)))
			.OrderByDescending(x => x.Score).ThenBy(x => x.Candidate.Id, StringComparer.Ordinal)
			.Select(x => x.Candidate).Take(3).ToList();
		string outcome = "fallback-empty";
		foreach (ShadowCandidate c in ranked) {
			SelectedSongMaterial material = BuildMaterial(c, label, artist, record, genre, year);
			if (material == null) { outcome = "fallback-missing"; continue; }
			if (PolarSongBehavior.UsePolarFitSelection && material.PolarProposal != null &&
				PolarSongBehavior.Refuses(material.PolarProposal.fit, artist, emptySongbook: false)) {
				Archive(c, act, week, "refused-at-session");
				outcome = "fallback-refused";
				continue;
			}
			RepertoireInfluenceService.OnSelected(material, artist, record, year);
			pendingPicks[record.recordId] = (c.Id, "pool");
			Count("selected:" + mode);
			return material;
		}
		pendingPicks[record.recordId] = (null, outcome);
		Count(outcome);
		return null;
	}

	internal static (string CandidateId, string Outcome)? PendingForChecks(string recordId) =>
		pendingPicks.TryGetValue(recordId, out var pick) ? pick : null;

	/// <summary>A player placement replaced the label's pick at the session: the shortlist song was not recorded.</summary>
	internal static void ReleasePick(Record record) {
		if (record != null && pendingPicks.ContainsKey(record.recordId)) pendingPicks[record.recordId] = (null, "placement");
	}

	private static SelectedSongMaterial BuildMaterial(ShadowCandidate c, AILabel label, SimulatedArtist artist, Record record, Genre genre, int year) {
		SongComposition song = CompositionCatalogService.GetSong(c.SongId);
		if (c.Source == SongMaterialSource.ArtistWritten) {
			if (song == null) {
				// First recording: author the original once, with the traits it reached the act with.
				song = CompositionCatalogService.CreateArtistOriginal(record, artist, label, genre, year, c.Craft, c.Hook, c.Lyric, c.Originality);
				c.SongId = song.songId;
				Count("authored");
			}
			return SongMaterialSelectionService.BuildArtistOriginalForSong(label, artist, record, song, genre, year);
		}
		if (song == null) return null;
		return c.Source == SongMaterialSource.ExternalProfessional
			? SongMaterialSelectionService.BuildProfessionalForSong(label.tier, artist, record, song, genre, year)
			: SongMaterialSelectionService.BuildCoverForSong(artist, record, song, genre, year);
	}

	/// <summary>Pure read of an act's shortlist through a label's ears. Changes nothing but first-sight initialization.</summary>
	internal static ShadowJudgement Judge(AILabel label, SimulatedArtist artist, int year, int week) {
		ShadowActRepertoire act = Ensure(label, artist, week, year);
		var j = new ShadowJudgement { Week = week, Year = year, LabelId = label.labelId, ArtistId = artist.artistId, Tier = label.tier };
		ShadowCandidate best = null;
		float bestRead = float.MinValue, bestBand = 0f, oracle = -1f, sum = 0f;
		foreach (ShadowCandidate c in act.Candidates) {
			if (c.Disposition != ShadowDisposition.Active) continue;
			j.Active++;
			if (c.Readiness != ShadowReadiness.Recordable) { j.Offered++; continue; }
			j.Recordable++;
			float read = Read(c, label, artist, out float band);
			float expected = ExpectedRecording(c, artist, label, out _, out _, out _);
			sum += expected;
			oracle = Math.Max(oracle, expected);
			if (read > bestRead || read == bestRead && StringComparer.Ordinal.Compare(c.Id, best.Id) < 0) {
				best = c; bestRead = read; bestBand = band;
			}
		}
		if (best == null) return j;
		j.BestId = best.Id; j.BestSongId = best.SongId; j.BestSource = best.Source; j.BestProvenance = best.Provenance;
		j.BestAgeWeeks = week - best.ArrivalWeek;
		j.BestPerceived = bestRead; j.ReadBand = bestBand;
		j.AuditBestExpectedQuality = ExpectedRecording(best, artist, label, out float hook, out float production, out float spread);
		j.AuditBestExpectedHook = hook; j.AuditBestExpectedProduction = production; j.AuditBestSpread = spread;
		j.AuditOracleExpectedQuality = oracle;
		j.AuditMeanRecordableExpected = sum / j.Recordable;
		return j;
	}

	// ---- bookkeeping -------------------------------------------------------------------------------------------

	private static void Archive(ShadowCandidate c, ShadowActRepertoire act, int week, string reason) {
		c.Disposition = ShadowDisposition.Archived;
		c.DispositionWeek = week;
		c.DispositionReason = reason;
		Count("archived:" + reason);
		current.Archived++;
		current.ArchivedByReason[reason] = current.ArchivedByReason.GetValueOrDefault(reason) + 1;
		Emit("archived", c, act.LabelId, week, reason);
	}

	private static void Count(string key) => state.Counters[key] = state.Counters.GetValueOrDefault(key) + 1;

	private static void Emit(string kind, ShadowCandidate c, string labelId, int week, string reason) {
		if (OnMaterialEvent == null) return;
		OnMaterialEvent(new ShadowMaterialEvent {
			Week = week, Kind = kind, ArtistId = c.ArtistId, LabelId = labelId, CandidateId = c.Id, SongId = c.SongId,
			Provenance = c.Provenance, Reason = reason, Source = c.Source, EvidenceVersion = c.EvidenceVersion
		});
	}

	// ---- save ----------------------------------------------------------------------------------------------------

	/// <summary>Writes the block only when the shadow has state, so a save made without it is byte-unchanged.</summary>
	public static void CaptureWorld(WorldSaveData world) { if (world != null) world.AiRepertoire = HasState ? state : null; }

	public static void RehydrateWorld(WorldSaveData world) {
		health = null; current = new ShadowRepertoireHealth(); pendingPicks.Clear();
		AiRepertoireSaveData saved = world?.AiRepertoire;
		if (saved != null && saved.ConfigVersion != ReleaseJudgement.ConfigVersion) {
			// Observe-only state from different rules is not migrated: start the shadow over rather than mix rule sets.
			GD.Print($"RELEASE_JUDGEMENT_SHADOW_RESET saved=v{saved.ConfigVersion} current=v{ReleaseJudgement.ConfigVersion}");
			saved = null;
		}
		state = saved ?? new AiRepertoireSaveData();
	}

	/// <summary>A fresh world in the same process must not inherit a previous world's shortlists.</summary>
	public static void Reset() { state = new AiRepertoireSaveData(); health = null; current = new ShadowRepertoireHealth(); pendingPicks.Clear(); }
}
