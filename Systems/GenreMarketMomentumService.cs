using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Enabled-only Phase-3 local genre-market shocks. No RNG is consumed here.</summary>
public readonly record struct GenreMarketKey(Genre Genre, string RegionId, AudienceSegment Segment);

public sealed class GenreMarketState {
	public float PreShock, Decay, PositiveImpulse, AdjacentImpulse, DonorPressure, Shock, Fatigue;
	public int RecentHits;
}

public readonly record struct GenreMarketEvent(string Kind, Genre Source, Genre Target, string RegionId, AudienceSegment Segment, float Amount, string Detail);

public static class GenreMarketMomentumService {
	private static readonly Dictionary<GenreMarketKey, GenreMarketState> States = new();
	private static readonly Dictionary<Genre, float> EmergenceAdvanceWeeks = new();
	private static readonly Dictionary<string, float> ZeitgeistDeltas = new(StringComparer.Ordinal);
	private static readonly List<GenreMarketEvent> Events = new();
	private static readonly Dictionary<string, float> ExplicitEdges = BuildEdges();
	private const float ShockHalfLifeWeeks = 24f;
	private const float FatigueDecay = .94f;
	private const float ExpansionShare = .20f;
	private const float MaxEmergenceAdvanceWeeks = 156f;
	private const float MaxZeitgeistDelta = .15f;

	public static IEnumerable<KeyValuePair<GenreMarketKey, GenreMarketState>> SnapshotStates() => States.OrderBy(pair => pair.Key.RegionId, StringComparer.Ordinal).ThenBy(pair => pair.Key.Segment).ThenBy(pair => pair.Key.Genre);
	public static IReadOnlyList<GenreMarketEvent> DrainEvents() { var copy = Events.ToArray(); Events.Clear(); return copy; }
	public static float GetShock(Genre genre, MarketRegion region, AudienceSegment segment) =>
		States.TryGetValue(new GenreMarketKey(genre, region.regionId, segment), out GenreMarketState state) ? state.Shock : 0f;
	public static float GetEmergenceAdvanceWeeks(Genre genre) => EmergenceAdvanceWeeks.TryGetValue(genre, out float weeks) ? weeks : 0f;
	public static float GetZeitgeistDelta(string field) => ZeitgeistDeltas.TryGetValue(field, out float delta) ? delta : 0f;

	public static void AdvanceWeek(IEnumerable<RecordRuntimeData> records, IEnumerable<MarketRegion> regions, float year) {
		if (!GenreMarketV2.Enabled) return;
		Events.Clear();
		foreach (GenreMarketState state in States.Values) {
			state.PreShock = state.Shock;
			float decay = Mathf.Exp(-Mathf.Log(2f) / ShockHalfLifeWeeks);
			state.Shock *= decay;
			state.Decay = state.Shock - state.PreShock;
			state.PositiveImpulse = state.AdjacentImpulse = state.DonorPressure = 0f;
			state.Fatigue *= FatigueDecay;
			state.RecentHits = Mathf.Max(0, state.RecentHits - (state.Fatigue < .02f ? 1 : 0));
		}
		foreach (string field in ZeitgeistDeltas.Keys.ToArray()) ZeitgeistDeltas[field] *= .995f;
		foreach (Genre genre in EmergenceAdvanceWeeks.Keys.ToArray()) EmergenceAdvanceWeeks[genre] *= .997f;

		MarketRegion[] orderedRegions = regions.OrderBy(region => region.regionId, StringComparer.Ordinal).ToArray();
		foreach (RecordRuntimeData record in records.Where(record => record.unitsThisWeek > 0).OrderBy(record => record.baseRecord.recordId, StringComparer.Ordinal)) {
			foreach (MarketRegion region in orderedRegions) {
				if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData local)) continue;
				float evidence = GetEvidence(record, local, region);
				if (evidence < .45f) continue;
				foreach (AudienceSegment segment in SegmentCapacityModel.All) {
					float segmentWeight = GenreCatalog.Get(GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, (int)year)).SegmentWeights.GetValueOrDefault(segment.ToString());
					if (segmentWeight <= .01f) continue;
					ApplyEvidence(record, region, segment, evidence * segmentWeight, year);
				}
			}
		}
	}

	private static float GetEvidence(RecordRuntimeData record, RegionalRecordData local, MarketRegion region) {
		float chart = record.currentPosition > 0 ? Mathf.Clamp((101f - record.currentPosition) / 100f, 0f, 1f) : 0f;
		float breakout = local.breakoutScore;
		float sustained = Mathf.Clamp(local.sustainedGrowthWeeks / 4f, 0f, 1f);
		float volume = Mathf.Clamp(local.unitsSoldThisWeek / Mathf.Max(500f, region.population * 700f), 0f, 1f);
		float credibility = record.GetQuality() * (chart > 0f || breakout >= .24f ? .15f : 0f);
		return Mathf.Clamp(chart * .34f + breakout * .30f + sustained * .18f + volume * .18f + credibility, 0f, 1f);
	}

	private static void ApplyEvidence(RecordRuntimeData record, MarketRegion region, AudienceSegment segment, float evidence, float year) {
		Genre source = GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, (int)year);
		Genre secondary = GenreCatalog.MapLegacy(record.baseRecord.secondaryGenre, (int)year);
		float raw = .18f * evidence;
		ApplyImpulse(source, source, region, segment, raw * .80f, "primary");
		if (secondary != source) ApplyImpulse(source, secondary, region, segment, raw * .20f, "secondary");
		DistributeAdjacency(source, region, segment, raw * .20f);
		ChargeDonors(source, region, segment, raw * (1f - ExpansionShare));
		NudgeEmergence(source, region, record.currentPosition, evidence, year);
		NudgeZeitgeist(source, evidence, record.currentPosition);
	}

	private static void ApplyImpulse(Genre source, Genre target, MarketRegion region, AudienceSegment segment, float requested, string kind) {
		GenreMarketState state = GetOrCreate(target, region, segment);
		float saturation = 1f / (1f + Mathf.Max(0f, state.Shock) * 2f + state.Fatigue);
		float amount = requested * saturation;
		state.Shock = Mathf.Clamp(state.Shock + amount, -1.25f, 1.25f);
		if (kind == "adjacent") state.AdjacentImpulse += amount; else state.PositiveImpulse += amount;
		state.Fatigue = Mathf.Clamp(state.Fatigue + amount * .75f, 0f, 1.5f);
		state.RecentHits++;
		Events.Add(new GenreMarketEvent(kind, source, target, region.regionId, segment, amount, "saturating-positive"));
	}

	private static void DistributeAdjacency(Genre source, MarketRegion region, AudienceSegment segment, float budget) {
		var recipients = GenreCatalog.All.Select(profile => profile.Genre)
			.Where(candidate => candidate != source)
			.Select(candidate => (Genre: candidate, Weight: GetAdjacency(source, candidate)))
			.Where(pair => pair.Weight > .12f).OrderBy(pair => pair.Genre).ToArray();
		float total = recipients.Sum(pair => pair.Weight);
		if (total <= 0f) return;
		foreach (var recipient in recipients) ApplyImpulse(source, recipient.Genre, region, segment, budget * recipient.Weight / total, "adjacent");
	}

	private static void ChargeDonors(Genre source, MarketRegion region, AudienceSegment segment, float amount) {
		var donors = GenreCatalog.All.Select(profile => profile.Genre)
			.Where(candidate => candidate != source && candidate is not (Genre.Classical or Genre.Comedy or Genre.Childrens or Genre.Gospel))
			.Select(candidate => (Genre: candidate, Weight: GetDonorWeight(source, candidate, segment)))
			.Where(pair => pair.Weight > .001f).OrderBy(pair => pair.Genre).ToArray();
		float total = donors.Sum(pair => pair.Weight);
		if (total <= 0f) return;
		foreach (var donor in donors) {
			float debit = amount * donor.Weight / total;
			GenreMarketState state = GetOrCreate(donor.Genre, region, segment);
			state.Shock = Mathf.Clamp(state.Shock - debit, -1.25f, 1.25f);
			state.DonorPressure -= debit;
			Events.Add(new GenreMarketEvent("donor", source, donor.Genre, region.regionId, segment, -debit, "segment-competition"));
		}
	}

	private static float GetDonorWeight(Genre source, Genre candidate, AudienceSegment segment) {
		float overlap = GenreCatalog.Get(source).SegmentWeights.GetValueOrDefault(segment.ToString()) * GenreCatalog.Get(candidate).SegmentWeights.GetValueOrDefault(segment.ToString());
		float baseline = GenreCatalog.Get(candidate).GetBaseline(1965f);
		return overlap * baseline * (1f - GetAdjacency(source, candidate));
	}

	public static float GetAdjacency(Genre left, Genre right) {
		if (left == right) return 1f;
		float family = GenreCatalog.Get(left).Family == GenreCatalog.Get(right).Family ? .12f : 0f;
		return Mathf.Max(family, ExplicitEdges.GetValueOrDefault(Edge(left, right)));
	}

	private static void NudgeEmergence(Genre genre, MarketRegion region, int chartPosition, float evidence, float year) {
		if (GenreCatalog.Get(genre).GetLifecycle(year) != GenreLifecycleState.PreEmergent || (chartPosition > 0 && chartPosition > 40) || evidence < .60f) return;
		float advance = Mathf.Min(3f, evidence * 3f);
		EmergenceAdvanceWeeks[genre] = Mathf.Min(MaxEmergenceAdvanceWeeks, GetEmergenceAdvanceWeeks(genre) + advance);
		Events.Add(new GenreMarketEvent("emergence", genre, genre, region.regionId, AudienceSegment.CollegeFolk, advance, "qualified-early-scene"));
	}

	private static void NudgeZeitgeist(Genre genre, float evidence, int chartPosition) {
		if (chartPosition > 0 && chartPosition > 40) return;
		string field = genre switch {
			Genre.RnB or Genre.Soul or Genre.Funk => "racialIntegration",
			Genre.BritishBeat or Genre.BritishBlues or Genre.BritishPop => "britishInfluence",
			Genre.PsychedelicRock or Genre.AcidRock or Genre.ProgressiveRock or Genre.ProtoPunk => "experimentalism",
			Genre.Folk or Genre.FolkRock or Genre.SingerSongwriter => "politicalAwareness",
			_ => "youthInfluence"
		};
		float nudge = Mathf.Min(.01f, evidence * .006f);
		ZeitgeistDeltas[field] = Mathf.Clamp(GetZeitgeistDelta(field) + nudge, -MaxZeitgeistDelta, MaxZeitgeistDelta);
		Events.Add(new GenreMarketEvent("zeitgeist", genre, genre, "national", AudienceSegment.MainstreamAM, nudge, field));
	}

	public static float GetZeitgeistFactor(Genre genre, AudienceSegment segment, float year) {
		Zeitgeist z = Zeitgeist.GetForYear(Mathf.Clamp(Mathf.FloorToInt(year), 1960, 1969));
		float centered(string field, float baseValue) => baseValue + GetZeitgeistDelta(field) - .5f;
		float influence = 0f;
		GenreProfile p = GenreCatalog.Get(genre);
		if (segment is AudienceSegment.Youth or AudienceSegment.MainstreamAM) influence += (p.AudienceLean - .5f) * centered("youthInfluence", z.youthInfluence) * .45f;
		if (segment is AudienceSegment.CollegeFolk or AudienceSegment.UndergroundFM && genre is Genre.PsychedelicRock or Genre.AcidRock or Genre.ProgressiveRock or Genre.ProtoPunk or Genre.FolkRock) influence += centered("counterCultureStrength", z.counterCultureStrength) * .28f;
		if (segment is AudienceSegment.MainstreamAM or AudienceSegment.AdultMOR && genre is Genre.RnB or Genre.Soul or Genre.Funk) influence += centered("racialIntegration", z.racialIntegration) * .25f;
		if (genre is Genre.BritishBeat or Genre.BritishBlues or Genre.BritishPop) influence += centered("britishInfluence", z.britishInfluence) * .25f;
		if (segment is AudienceSegment.CollegeFolk or AudienceSegment.UndergroundFM && genre is Genre.PsychedelicRock or Genre.AcidRock or Genre.ProgressiveRock or Genre.ProtoPunk) influence += centered("experimentalism", z.experimentalism) * .22f;
		if (segment == AudienceSegment.CollegeFolk && genre is Genre.Folk or Genre.FolkRock or Genre.SingerSongwriter or Genre.Comedy) influence += centered("politicalAwareness", z.politicalAwareness) * .18f;
		return Mathf.Clamp(1f + influence, .75f, 1.25f);
	}

	private static GenreMarketState GetOrCreate(Genre genre, MarketRegion region, AudienceSegment segment) {
		var key = new GenreMarketKey(genre, region.regionId, segment);
		if (!States.TryGetValue(key, out GenreMarketState state)) States[key] = state = new GenreMarketState();
		return state;
	}

	public static void ResetForProbe() { States.Clear(); EmergenceAdvanceWeeks.Clear(); ZeitgeistDeltas.Clear(); Events.Clear(); }
	public static void InjectProbeImpulse(Genre source, MarketRegion region, AudienceSegment segment, float amount, float year) {
		ApplyImpulse(source, source, region, segment, amount * .8f, "primary"); DistributeAdjacency(source, region, segment, amount * .2f); ChargeDonors(source, region, segment, amount * .8f); NudgeEmergence(source, region, 1, .9f, year); NudgeZeitgeist(source, .9f, 1);
	}
	public static void DecayForProbe(int weeks) { for (int i = 0; i < weeks; i++) foreach (GenreMarketState state in States.Values) state.Shock *= Mathf.Exp(-Mathf.Log(2f) / ShockHalfLifeWeeks); }

	private static string Edge(Genre a, Genre b) => string.CompareOrdinal(a.ToString(), b.ToString()) < 0 ? a + "|" + b : b + "|" + a;
	private static Dictionary<string, float> BuildEdges() {
		var edges = new Dictionary<string, float>(); void Add(Genre a, Genre b, float weight) => edges[Edge(a, b)] = weight;
		Add(Genre.RnB, Genre.Soul,.75f); Add(Genre.Soul, Genre.DooWop,.60f); Add(Genre.Soul, Genre.Gospel,.70f); Add(Genre.Soul, Genre.Funk,.75f);
		Add(Genre.Blues, Genre.BluesRock,.72f); Add(Genre.BluesRock, Genre.BritishBlues,.65f); Add(Genre.Folk, Genre.FolkRock,.75f); Add(Genre.FolkRock, Genre.SingerSongwriter,.65f);
		Add(Genre.Country, Genre.CountryRock,.72f); Add(Genre.Country, Genre.Folk,.45f); Add(Genre.RockAndRoll, Genre.SurfRock,.65f); Add(Genre.SurfRock, Genre.GarageRock,.62f); Add(Genre.GarageRock, Genre.BritishBeat,.58f); Add(Genre.GarageRock, Genre.ProtoPunk,.68f);
		Add(Genre.PsychedelicRock, Genre.AcidRock,.80f); Add(Genre.AcidRock, Genre.ProgressiveRock,.70f); Add(Genre.SunshinePop, Genre.BaroquePop,.62f); Add(Genre.BaroquePop, Genre.FolkRock,.48f);
		Add(Genre.Ska, Genre.Rocksteady,.78f); Add(Genre.Rocksteady, Genre.Reggae,.78f); Add(Genre.Boogaloo, Genre.Soul,.60f); Add(Genre.Boogaloo, Genre.Funk,.55f); Add(Genre.Boogaloo, Genre.LatinPop,.55f);
		return edges;
	}
}
