using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Phase 3: the weekly playlist decision (design doc a 3.2-3.4). Once per reporter station per week
/// it scores every eligible record's candidacy, fills its High/Mid/Light slots, and drops the rest --
/// with tier inertia (stickiness) and the re-add hysteresis that protects the returns-to-#1 metric.
///
/// Runs in SimulateWeek AFTER sales (candidacy reads this week's settled salesSupport) and BEFORE the
/// radioPlay aggregation. All randomness uses the network's own RNG, never the global GD stream, so
/// with REPORTER_PANEL_WEIGHT at 0 this phase still produces byte-identical simulation output; it only
/// starts moving radioPlay once the weight is dialled in (Phase 2b).
/// </summary>
public sealed partial class StationNetwork {

	// ---- candidacy weights (doc a 3.2). Provisional; Phase 2b tunes against the V3.1 economy. ----
	private const float REL_WEIGHT = 0.5f;
	private const float LOYALTY_WEIGHT = 0.3f;
	private const int BURN_ONSET = 8;          // weeks in rotation before burn starts biting
	private const float BURN_DECAY = 0.96f;    // per-week candidacy decay past onset
	private const float HEAT_FOLLOW = 0.5f;    // how hard PDs chase national radioHeat
	private const float TRADE_WEIGHT = 0.3f;   // the trade "pick" bandwagon
	private const float INTEGRATION_CROSSOVER = 0.6f;  // RnB/Soul onto Top40 as integration rises

	// ---- tier inertia (doc a 3.3). Bonus resists demotion; scaled by autonomy (Boss churns fast). ----
	private static float TierInertiaBase(SpinTier tier) => tier switch {
		SpinTier.High => 0.18f,
		SpinTier.Mid => 0.10f,
		SpinTier.Light => 0.04f,
		_ => 0f
	};

	// ---- anti-oscillation (doc a 3.4, NON-NEGOTIABLE) ----
	private const int MIN_READD_WEEKS = 4;         // a dropped record is locked out this long
	private const float READD_HYSTERESIS = 1.2f;   // and must beat the light cutoff by this margin
	private const float READD_MIN_CANDIDACY = 0.05f;

	// ---- circulation filter: a record is a fresh candidate only if it is actually in play. ----
	private static bool InCirculation(RecordRuntimeData r, float support) =>
		r.radioHeat > 0.02f || r.currentPosition > 0 || support > 0.05f;

	// Format -> the audience segments its playlist admits (basis of formatMatch).
	private static readonly Dictionary<StationFormat, AudienceSegment[]> FormatSegments = new() {
		// RegionalLatin is NOT admitted by Top40 in general -- only latin-leaning stations carry it
		// (handled in FormatMatch). Admitting it here on every Top40 reporter over-charted LatinPop.
		[StationFormat.Top40] = new[] { AudienceSegment.MainstreamAM, AudienceSegment.Youth },
		[StationFormat.RnB] = new[] { AudienceSegment.UrbanRnB },
		[StationFormat.Country] = new[] { AudienceSegment.CountryWestern },
		[StationFormat.MOR] = new[] { AudienceSegment.AdultMOR, AudienceSegment.FamilyChildrens },
		[StationFormat.Jazz] = new[] { AudienceSegment.JazzHiFiClassical },
		[StationFormat.Gospel] = new[] { AudienceSegment.GospelChurch },
		[StationFormat.UndergroundFM] = new[] { AudienceSegment.UndergroundFM, AudienceSegment.CollegeFolk },
		// The personality-era generalist: broad admittance across the mainstream segments.
		[StationFormat.FullService] = new[] {
			AudienceSegment.MainstreamAM, AudienceSegment.Youth, AudienceSegment.AdultMOR,
			AudienceSegment.CollegeFolk, AudienceSegment.UrbanRnB, AudienceSegment.CountryWestern },
	};

	// Per-record factors that don't vary by station, cached once per week to keep the 63-station
	// sweep cheap.
	private struct RecordFactors {
		public RecordRuntimeData rec;
		public string id, labelId, artistId;
		public Genre canonical;
		public GenreFamily family;
		public GenreProfile profile;   // market-side routing (SegmentWeights); may be null
		public float support;          // GetSalesSupportRatio (base; exponent applied per station)
		public float quality;          // 0..1
		public float heatPull;         // 1 + radioHeat*HEAT_FOLLOW + tradePick*TRADE_WEIGHT
		public bool inCirculation;     // false = fading catalog; a candidate only if already on a playlist
	}

	private readonly List<RecordFactors> factorScratch = new();

	/// <summary>The reporter playlist meeting. Populates each station's rt.playlist for the week.</summary>
	public void UpdatePlaylists(IReadOnlyList<RecordRuntimeData> records, MarketRegion[] regions, int week, int year) {
		if (records == null || regions == null) return;

		// 1. Cache per-record station-invariant factors once.
		factorScratch.Clear();
		foreach (RecordRuntimeData r in records) {
			if (r?.baseRecord == null || r.baseRecord.format == ReleaseFormat.Album) continue;
			float support = ChartSimulator.GetSalesSupportRatio(r);
			bool circulating = InCirculation(r, support);
			// A record already on a playlist stays a candidate even once it leaves circulation (so it
			// can burn out gracefully rather than vanish); everything else must be in play.
			Genre canon = GenreCatalog.MapLegacy(r.baseRecord.primaryGenre, year);
			GenreCatalog.TryGet(canon, out GenreProfile profile);
			bool tradePick = r.currentPosition > 0 && r.currentPosition <= 40;
			factorScratch.Add(new RecordFactors {
				rec = r, id = r.baseRecord.recordId, labelId = r.baseRecord.labelId, artistId = r.baseRecord.artistId,
				canonical = canon, family = profile?.Family ?? GenreFamily.Pop, profile = profile,
				support = support, quality = r.GetQuality(),
				heatPull = 1f + r.radioHeat * HEAT_FOLLOW + (tradePick ? TRADE_WEIGHT : 0f),
				inCirculation = circulating,
			});
		}

		// 2. Per region, per reporter: score and fill.
		foreach (MarketRegion region in regions) {
			if (!stationsByRegion.TryGetValue(region.regionId, out List<RadioStation> roster)) continue;
			float integration = region.currentIntegration;
			foreach (RadioStation station in roster)
				DecideStationPlaylist(station, integration, week);
		}
	}

	private readonly List<(string id, float adjusted, SpinTier prior)> primaryScratch = new();
	private readonly List<(string id, float adjusted)> readdScratch = new();

	private void DecideStationPlaylist(RadioStation station, float integration, int week) {
		StationRuntime rt = station.rt;
		if (rt == null) return;
		Deejay dj = GetDeejay(station.leadDjId);
		float autonomy = station.djAutonomy;

		primaryScratch.Clear();
		readdScratch.Clear();

		foreach (RecordFactors f in factorScratch) {
			float quality = f.quality;
			SpinTier prior = rt.TierOf(f.id);
			bool incumbent = prior != SpinTier.None;

			float formatMatch = FormatMatch(f.profile, station.format, f.family, integration, station.latinLeaning);
			if (formatMatch <= 0f) continue;                        // format never admits this genre
			if (!incumbent && !f.inCirculation) continue;           // not on the air and not circulating

			// Re-add hysteresis: a record this station dropped cannot re-enter for MIN_READD_WEEKS.
			bool everDropped = rt.droppedOnWeek.TryGetValue(f.id, out int droppedWeek);
			bool locked = !incumbent && everDropped && week - droppedWeek < MIN_READD_WEEKS;
			if (locked) continue;

			float candidacy = Candidacy(f, quality, formatMatch, station, dj, autonomy, rt);
			if (candidacy <= 0f) continue;

			if (!incumbent && everDropped) {
				// A cleared re-add: Light tier only, and only if it clears the hysteresis margin below.
				readdScratch.Add((f.id, candidacy));
			} else {
				float inertia = TierInertiaBase(prior) * (0.3f + autonomy * 0.7f);
				primaryScratch.Add((f.id, candidacy * (1f + inertia), prior));
			}
		}

		// Rank incumbents + fresh, fill High -> Mid -> Light.
		primaryScratch.Sort((a, b) => b.adjusted.CompareTo(a.adjusted));
		var next = new Dictionary<string, SpinTier>(StringComparer.Ordinal);
		int high = station.highSlots, mid = station.midSlots, light = station.lightSlots;
		float lightCutoff = float.MaxValue;   // the weakest retained score at the Light boundary
		int filled = 0;
		foreach (var c in primaryScratch) {
			SpinTier tier;
			if (filled < high) tier = SpinTier.High;
			else if (filled < high + mid) tier = SpinTier.Mid;
			else if (filled < high + mid + light) { tier = SpinTier.Light; lightCutoff = c.adjusted; }
			else break;
			next[c.id] = tier;
			filled++;
		}

		// Re-adds compete only for LEFTOVER Light capacity, and must beat the light cutoff by the
		// hysteresis margin -- a dropped record cannot bump an incumbent, only fill genuine slack.
		int lightUsed = 0;
		foreach (var kv in next) if (kv.Value == SpinTier.Light) lightUsed++;
		int lightFree = Mathf.Max(0, light - lightUsed);
		if (lightFree > 0 && readdScratch.Count > 0) {
			readdScratch.Sort((a, b) => b.adjusted.CompareTo(a.adjusted));
			float floor = lightCutoff == float.MaxValue ? READD_MIN_CANDIDACY : lightCutoff * READD_HYSTERESIS;
			foreach (var c in readdScratch) {
				if (lightFree <= 0) break;
				if (c.adjusted < floor || c.adjusted < READD_MIN_CANDIDACY) continue;
				next[c.id] = SpinTier.Light;
				lightFree--;
			}
		}

		CommitPlaylist(rt, next, week);
	}

	/// <summary>candidacy = formatMatch x qualityTaste x salesSupport x relationship x payola x freshness x heatPull.</summary>
	private float Candidacy(RecordFactors f, float quality, float formatMatch, RadioStation station, Deejay dj, float autonomy, StationRuntime rt) {
		// qualityTaste: the DJ's ear, weighted by autonomy. Boss station (autonomy~0) -> ~1 (neutral).
		float affinity = dj?.GenreAffinity(f.canonical) ?? 1f;
		float tasteRead = (0.6f + quality * 0.8f) * affinity;
		float noiseMag = (1f - (dj?.taste ?? 0.5f)) * 0.4f;
		float judgment = Mathf.Max(0.05f, tasteRead + (rng.Randf() - 0.5f) * 2f * noiseMag);
		float qualityTaste = Mathf.Lerp(1f, judgment, autonomy);

		// salesSupport: Boss stations follow sales sharply; personality stations tolerate a slow seller.
		float salesSupport = Mathf.Pow(Mathf.Max(0f, f.support), 1f + (1f - autonomy) * 2f);

		// relationship: cultivation edge (Phase 4 cultivation writes these; 0 until then).
		float relationship = 1f + rt.Rapport(f.labelId) * REL_WEIGHT + rt.Loyalty(f.artistId) * LOYALTY_WEIGHT;

		// payola: Phase 4 ledger; neutral for now.
		float payola = 1f;

		// freshness: per-station burn, replacing the aggregate STATION_DROP_BURN.
		int weeks = rt.weeksInPlaylist.TryGetValue(f.id, out int w) ? w : 0;
		float freshness = weeks > BURN_ONSET ? Mathf.Pow(BURN_DECAY, weeks - BURN_ONSET) : 1f;

		return formatMatch * qualityTaste * salesSupport * relationship * payola * freshness * f.heatPull;
	}

	/// <summary>0 excludes the record entirely; else the genre's routed reach into the format's segments,
	/// plus an integration-scaled RnB/Soul crossover onto Top40.</summary>
	private static float FormatMatch(GenreProfile profile, StationFormat format, GenreFamily family, float integration, bool latinLeaning) {
		if (profile == null || !FormatSegments.TryGetValue(format, out AudienceSegment[] segments)) return 0f;
		float m = 0f;
		foreach (AudienceSegment seg in segments)
			m += profile.SegmentWeights.TryGetValue(seg.ToString(), out float v) ? v : 0f;
		if (format == StationFormat.Top40 && family == GenreFamily.RhythmAndSoul)
			m += (profile.SegmentWeights.TryGetValue("UrbanRnB", out float rb) ? rb : 0f) * integration * INTEGRATION_CROSSOVER;
		// Latin-leaning Top40 stations (Southwest flavor) additionally admit the RegionalLatin routing.
		if (format == StationFormat.Top40 && latinLeaning)
			m += profile.SegmentWeights.TryGetValue("RegionalLatin", out float lat) ? lat : 0f;
		return Mathf.Clamp(m, 0f, 1f);
	}

	private static void CommitPlaylist(StationRuntime rt, Dictionary<string, SpinTier> next, int week) {
		// Drops: records that were on the playlist and no longer are -> record the drop week (hysteresis).
		foreach (var kv in rt.playlist) {
			if (!next.ContainsKey(kv.Key)) {
				rt.droppedOnWeek[kv.Key] = week;
				rt.weeksInPlaylist.Remove(kv.Key);
			}
		}
		// Retained/added: advance the burn clock; clear any stale drop record on (re-)add.
		foreach (var kv in next) {
			if (rt.playlist.ContainsKey(kv.Key)) rt.weeksInPlaylist[kv.Key] = rt.weeksInPlaylist.GetValueOrDefault(kv.Key) + 1;
			else { rt.weeksInPlaylist[kv.Key] = 1; rt.droppedOnWeek.Remove(kv.Key); }
		}
		rt.playlist.Clear();
		foreach (var kv in next) rt.playlist[kv.Key] = kv.Value;
	}
}
