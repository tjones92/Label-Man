using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;

/// <summary>Immutable, data-owned canonical genre metadata for the enabled Directive 5 path.</summary>
public sealed class GenreProfile {
	public string Id { get; }
	public Genre Genre { get; }
	public GenreFamily Family { get; }
	public float EmergenceYear { get; }
	public float? DeathYear { get; }
	public float AudienceLean { get; }
	public float SingleOrientation { get; }
	public float[] BaselineKeyframes { get; }
	public IReadOnlyDictionary<string, float> SegmentWeights { get; }

	internal GenreProfile(string id, Genre genre, GenreFamily family, float emergence, float? death, float audience, float orientation, float[] baseline) {
		Id = id; Genre = genre; Family = family; EmergenceYear = emergence; DeathYear = death;
		AudienceLean = audience; SingleOrientation = orientation; BaselineKeyframes = baseline;
		SegmentWeights = new ReadOnlyDictionary<string, float>(GenreSegmentRouting.Create(genre, family, audience));
	}

	public float GetBaseline(float year) {
		float clamped = Mathf.Clamp(year, 1960f, 1969f);
		int[] years = { 1960, 1962, 1964, 1966, 1967, 1968, 1969 };
		for (int i = 0; i < years.Length - 1; i++) if (clamped <= years[i + 1]) return Mathf.Lerp(BaselineKeyframes[i], BaselineKeyframes[i + 1], (clamped - years[i]) / (years[i + 1] - years[i]));
		return BaselineKeyframes[^1];
	}

	public GenreLifecycleState GetLifecycle(float year) {
		if (year < EmergenceYear) return GenreLifecycleState.PreEmergent;
		if (DeathYear.HasValue && year > DeathYear.Value) return GenreLifecycleState.Legacy;
		if (year < EmergenceYear + 1f) return GenreLifecycleState.Emerging;
		if (DeathYear.HasValue && year > DeathYear.Value - 1f) return GenreLifecycleState.Declining;
		return GenreLifecycleState.Established;
	}
}

public static class GenreCatalog {
	private static readonly Dictionary<Genre, GenreProfile> Profiles = new();
	private static readonly Dictionary<string, GenreProfile> ProfilesById = new(StringComparer.Ordinal);
	private static readonly ReadOnlyCollection<GenreProfile> AllProfiles;
	static GenreCatalog() {
		// id, enum, family, emergence, death, audience lean, single orientation, 1960/62/64/66/67/68/69 baseline.
		Add("traditional-pop", Genre.TraditionalPop, GenreFamily.Pop, 1950, 1971, .15f, .45f, .62f,.49f,.31f,.16f,.11f,.11f,.10f);
		Add("teen-pop", Genre.TeenPop, GenreFamily.Pop, 1957, 1965, .90f, .90f, .70f,.75f,.50f,.35f,.30f,.28f,.25f);
		Add("baroque-pop", Genre.BaroquePop, GenreFamily.Pop, 1966, 1970, .60f, .50f, .02f,.02f,.06f,.18f,.12f,.09f,.06f);
		Add("sunshine-pop", Genre.SunshinePop, GenreFamily.Pop, 1965, 1971, .65f, .55f, .02f,.03f,.08f,.22f,.22f,.16f,.08f);
		Add("bubblegum", Genre.Bubblegum, GenreFamily.Pop, 1967, 1971, .95f, .90f, .01f,.02f,.03f,.05f,.08f,.22f,.38f);
		Add("easy-listening", Genre.EasyListening, GenreFamily.Pop, 1950, null, .15f, .35f, .65f,.60f,.52f,.42f,.36f,.30f,.24f);
		Add("british-pop", Genre.BritishPop, GenreFamily.Pop, 1964, 1968, .90f, .80f, .01f,.02f,.80f,.75f,.55f,.40f,.30f);
		Add("rock-and-roll", Genre.RockAndRoll, GenreFamily.Rock, 1955, null, .85f, .85f, .68f,.65f,.50f,.24f,.16f,.10f,.07f);
		Add("surf-rock", Genre.SurfRock, GenreFamily.Rock, 1961, 1966, .90f, .80f, .05f,.60f,.65f,.40f,.30f,.25f,.20f);
		Add("garage-rock", Genre.GarageRock, GenreFamily.Rock, 1963, 1968, .90f, .85f, .10f,.20f,.50f,.65f,.55f,.40f,.30f);
		Add("psychedelic-rock", Genre.PsychedelicRock, GenreFamily.Rock, 1966, 1971, .85f, .85f, .02f,.02f,.10f,.55f,.95f,.90f,.70f);
		Add("acid-rock", Genre.AcidRock, GenreFamily.Rock, 1966, 1971, .85f, .40f, .02f,.02f,.05f,.10f,.15f,.18f,.12f);
		Add("hard-rock", Genre.HardRock, GenreFamily.Rock, 1967, null, .85f, .40f, .01f,.02f,.05f,.15f,.30f,.50f,.65f);
		Add("proto-metal", Genre.ProtoMetal, GenreFamily.Rock, 1968, null, .85f, .40f, .01f,.01f,.02f,.05f,.10f,.20f,.35f);
		Add("progressive-rock", Genre.ProgressiveRock, GenreFamily.Rock, 1968, null, .80f, .25f, .01f,.01f,.02f,.05f,.10f,.25f,.40f);
		Add("blues-rock", Genre.BluesRock, GenreFamily.Rock, 1966, null, .80f, .45f, .02f,.05f,.10f,.18f,.24f,.32f,.36f);
		Add("proto-punk", Genre.ProtoPunk, GenreFamily.Rock, 1967, null, .85f, .40f, .01f,.01f,.02f,.05f,.15f,.25f,.30f);
		Add("british-beat", Genre.BritishBeat, GenreFamily.Rock, 1963, 1967, .90f, .75f, .01f,.02f,.95f,.70f,.50f,.40f,.35f);
		Add("british-blues", Genre.BritishBlues, GenreFamily.Rock, 1964, null, .85f, .85f, .01f,.02f,.15f,.65f,.80f,.95f,1.00f);
		Add("rnb", Genre.RnB, GenreFamily.RhythmAndSoul, 1949, null, .70f, .80f, .40f,.50f,.55f,.50f,.48f,.45f,.42f);
		Add("soul", Genre.Soul, GenreFamily.RhythmAndSoul, 1960, null, .75f, .95f, .20f,.55f,.75f,.85f,.90f,.90f,.90f);
		Add("funk", Genre.Funk, GenreFamily.RhythmAndSoul, 1967, null, .80f, .70f, .02f,.05f,.10f,.25f,.40f,.55f,.70f);
		Add("doo-wop", Genre.DooWop, GenreFamily.RhythmAndSoul, 1954, 1965, .80f, .85f, .75f,.50f,.20f,.10f,.05f,.03f,.02f);
		Add("gospel", Genre.Gospel, GenreFamily.Gospel, 1950, null, .50f, .70f, .35f,.35f,.38f,.48f,.55f,.65f,.75f);
		Add("country", Genre.Country, GenreFamily.Country, 1950, null, .40f, .65f, .48f,.50f,.50f,.52f,.52f,.54f,.56f);
		Add("country-rock", Genre.CountryRock, GenreFamily.Country, 1968, null, .70f, .40f, .01f,.02f,.05f,.10f,.20f,.40f,.55f);
		Add("folk", Genre.Folk, GenreFamily.Folk, 1958, 1966, .60f, .50f, .40f,.50f,.60f,.45f,.35f,.30f,.30f);
		Add("folk-rock", Genre.FolkRock, GenreFamily.Folk, 1965, null, .80f, .55f, .02f,.02f,.10f,.75f,.70f,.60f,.55f);
		Add("contemporary-folk", Genre.ContemporaryFolk, GenreFamily.Folk, 1961, 1969, .60f, .50f, .10f,.40f,.55f,.45f,.40f,.40f,.40f);
		Add("singer-songwriter", Genre.SingerSongwriter, GenreFamily.Folk, 1967, null, .65f, .35f, .02f,.05f,.10f,.20f,.30f,.40f,.50f);
		Add("jazz", Genre.Jazz, GenreFamily.Jazz, 1945, null, .35f, .30f, .45f,.38f,.28f,.25f,.25f,.24f,.24f);
		Add("bossa-nova", Genre.BossaNova, GenreFamily.Jazz, 1962, 1967, .40f, .45f, .05f,.50f,.55f,.40f,.30f,.25f,.20f);
		Add("blues", Genre.Blues, GenreFamily.Blues, 1945, null, .45f, .50f, .30f,.30f,.30f,.35f,.40f,.40f,.40f);
		Add("classical", Genre.Classical, GenreFamily.Classical, 1945, null, .20f, .15f, .40f,.40f,.40f,.40f,.40f,.40f,.40f);
		Add("boogaloo", Genre.Boogaloo, GenreFamily.Latin, 1966, 1969, .70f, .70f, .02f,.05f,.10f,.35f,.40f,.35f,.25f);
		Add("tex-mex", Genre.TexMex, GenreFamily.Latin, 1959, null, .65f, .75f, .15f,.20f,.25f,.30f,.30f,.30f,.30f);
		Add("latin-pop", Genre.LatinPop, GenreFamily.Latin, 1958, null, .55f, .60f, .20f,.25f,.30f,.35f,.35f,.35f,.35f);
		Add("ska", Genre.Ska, GenreFamily.Caribbean, 1964, 1967, .60f, .80f, .01f,.02f,.05f,.10f,.12f,.10f,.08f);
		Add("rocksteady", Genre.Rocksteady, GenreFamily.Caribbean, 1966, 1968, .60f, .80f, .01f,.01f,.02f,.08f,.12f,.12f,.10f);
		Add("reggae", Genre.Reggae, GenreFamily.Caribbean, 1968, null, .65f, .80f, .01f,.01f,.02f,.03f,.05f,.10f,.20f);
		Add("comedy", Genre.Comedy, GenreFamily.NonMusic, 1955, null, .50f, .15f, .40f,.55f,.40f,.35f,.35f,.40f,.40f);
		Add("childrens", Genre.Childrens, GenreFamily.NonMusic, 1950, null, .50f, .30f, .35f,.35f,.35f,.35f,.35f,.35f,.35f);
		AllProfiles = new ReadOnlyCollection<GenreProfile>(new List<GenreProfile>(Profiles.Values));
	}

	public static IReadOnlyList<GenreProfile> All => AllProfiles;
	public static bool TryGet(Genre genre, out GenreProfile profile) => Profiles.TryGetValue(genre, out profile);
	public static GenreProfile Get(Genre genre) => Profiles.TryGetValue(genre, out GenreProfile profile) ? profile : throw new KeyNotFoundException($"No canonical profile for {genre}.");
	public static GenreProfile Get(string id) => ProfilesById.TryGetValue(id, out GenreProfile profile) ? profile : throw new KeyNotFoundException($"Unknown genre id '{id}'.");
	public static Genre MapLegacy(Genre genre, int? releaseYear = null) => genre switch {
		Genre.Psychedelic => Genre.PsychedelicRock, Genre.BritishInvasion => Genre.BritishBeat,
		Genre.Motown => Genre.Soul, Genre.GirlGroup => Genre.TeenPop, Genre.Skiffle => Genre.Folk,
		Genre.SkaRocksteady => !releaseYear.HasValue || releaseYear <= 1965 ? Genre.Ska : releaseYear <= 1967 ? Genre.Rocksteady : Genre.Reggae,
		_ => genre
	};
	public static void Validate() {
		if (Profiles.Count != 42) throw new InvalidOperationException($"Expected 42 canonical genre profiles, found {Profiles.Count}.");
		foreach (GenreProfile p in Profiles.Values) {
			if (string.IsNullOrWhiteSpace(p.Id) || p.BaselineKeyframes.Length != 7 || !float.IsFinite(p.AudienceLean) || p.AudienceLean < 0f || p.AudienceLean > 1f || !float.IsFinite(p.SingleOrientation) || p.SingleOrientation < 0f || p.SingleOrientation > 1f) throw new InvalidOperationException($"Invalid profile '{p.Id}'.");
			foreach (float value in p.BaselineKeyframes) if (!float.IsFinite(value) || value < 0f || value > 1f) throw new InvalidOperationException($"Invalid baseline value for '{p.Id}'.");
			float sum = 0f; foreach (float value in p.SegmentWeights.Values) sum += value;
			if (Math.Abs(sum - 1f) > 0.000001f) throw new InvalidOperationException($"Segment weights for '{p.Id}' are not normalized.");
		}
	}
	private static void Add(string id, Genre genre, GenreFamily family, float emergence, float? death, float audience, float orientation, params float[] baseline) {
		GenreProfile profile = new(id, genre, family, emergence, death, audience, orientation, baseline);
		if (!Profiles.TryAdd(genre, profile) || !ProfilesById.TryAdd(id, profile)) throw new InvalidOperationException($"Duplicate canonical genre '{id}'.");
	}
}
