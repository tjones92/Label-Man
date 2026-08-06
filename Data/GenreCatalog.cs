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
		// KEYFRAME PASS, calibrated against TWO benchmarks read together: the historical
		// market-share targets (AdjustedHistoricalGenreShareTargets, normalized to 100%) and the
		// hand-counted year-end Hot 100 genre histogram. Where the two disagree the market table
		// wins on baselines, because a baseline is a demand quantity; a genre that charts wrongly
		// at a correct market share is a chart-side defect and is NOT repaired here.
		//
		// TRANSFER IS QUADRATIC, NOT LINEAR. Measured across 260 genre-years that moved between
		// d7-segcurve-decade-522-1001 and d7-genretune1-decade-522-1001, realized market share goes
		// as roughly baseline^2 (median exponent 1.98, IQR 1.27-2.51). Sizing a keyframe by the
		// naive share ratio therefore overshoots hard in both directions, and did: Soul was raised
		// 1.37x and returned 2.0x the share (27.9% against a 17.5% target), while British Blues was
		// cut 2.9x and collapsed 15x (12.8% -> 0.8% against a 4.1% target). Size every change by
		// sqrt(target/current), not by target/current.
		//
		// Transfer is also field-dependent: it is measured against whatever else is competing for a
		// normalized 100%, so cutting a genre that held 12 points of the market raises everyone
		// else's efficiency. Read the two together -- the exponent sets the size of a move, the
		// field sets which way the rest of the catalog drifts underneath it.
		Add("traditional-pop", Genre.TraditionalPop, GenreFamily.Pop, 1950, 1971, .15f, .45f, .42f,.48f,.49f,.44f,.44f,.41f,.39f);
		// Death 1965 -> 1971. IsAvailableForNewSupply returns false once year > DeathYear, so new
		// supply was exactly zero from 1966 and the genre realised 0.0% against a 1.17% target at
		// 1969 -- the implied baseline was 9.69 against a 1.0 cap, i.e. unreachable by keyframe.
		// The late-decade survivor is manufactured teen-aimed pop (the Monkees), which is a real
		// category and is what the market target's 1.70/1.17 at 1968/69 describes.
		Add("teen-pop", Genre.TeenPop, GenreFamily.Pop, 1957, 1971, .90f, .90f, .70f,.74f,.63f,.43f,.31f,.26f,.21f);
		Add("baroque-pop", Genre.BaroquePop, GenreFamily.Pop, 1966, 1970, .60f, .50f, .02f,.02f,.06f,.38f,.34f,.21f,.13f);
		// Late keyframe gently corrected (handoff 11.5, finding 2): the year-end count shows Sunshine
		// Pop holding late rather than collapsing, but keyframe and radio multiplier COMPOUND in the
		// airplay channel, so only the erroneous 1969 collapse is softened (.22 -> .28), the 1968
		// value is left at its authored .35, and the radio up-lever is dropped to 1.40. Together these
		// target ~28-32 year-end slots without stealing calibrated slots from Soul.
		Add("sunshine-pop", Genre.SunshinePop, GenreFamily.Pop, 1965, 1971, .65f, .55f, .02f,.03f,.10f,.49f,.46f,.35f,.28f);
		Add("bubblegum", Genre.Bubblegum, GenreFamily.Pop, 1967, 1971, .95f, .90f, .01f,.02f,.03f,.05f,.16f,.46f,.71f);
		Add("easy-listening", Genre.EasyListening, GenreFamily.Pop, 1950, null, .15f, .35f, .68f,.59f,.45f,.46f,.53f,.50f,.49f);
		Add("british-pop", Genre.BritishPop, GenreFamily.Pop, 1964, 1968, .90f, .80f, .01f,.02f,.95f,.50f,.43f,.35f,.30f);
		// Early Rock and Roll ran 19.2% against a 13.5% target. The authored late-decade decline is
		// deliberately preserved: the year-end benchmark's late RnR counts are misclassification of
		// genre-ambiguous records and are not evidence of a surviving commercial category.
		Add("rock-and-roll", Genre.RockAndRoll, GenreFamily.Rock, 1955, null, .85f, .85f, .48f,.46f,.42f,.22f,.15f,.10f,.07f);
		// Emergence 1961 -> 1960: "Walk Don't Run" charted in 1960, so the genre exists at the
		// game's start date rather than opening a year in.
		Add("surf-rock", Genre.SurfRock, GenreFamily.Rock, 1960, 1966, .90f, .70f, .12f,.48f,.78f,.26f,.20f,.16f,.06f);
		Add("garage-rock", Genre.GarageRock, GenreFamily.Rock, 1963, 1968, .90f, .85f, .08f,.16f,.39f,.56f,.38f,.30f,.24f);
		// Capped at 1.00 through 1966-67 and STILL short of a 6.43% target at 1967 (3.9% realised).
		// This one cannot be closed from the baseline alone and is the clearest case in the catalog
		// of a genre whose supply is limited by something other than authored demand.
		Add("psychedelic-rock", Genre.PsychedelicRock, GenreFamily.Rock, 1966, 1971, .85f, .40f, .02f,.02f,.10f,1.00f,1.00f,.93f,.68f);
		Add("acid-rock", Genre.AcidRock, GenreFamily.Rock, 1966, 1971, .85f, .28f, .02f,.02f,.05f,.10f,.15f,.18f,.12f);
		Add("hard-rock", Genre.HardRock, GenreFamily.Rock, 1967, null, .85f, .40f, .01f,.02f,.05f,.15f,.30f,.50f,.65f);
		Add("proto-metal", Genre.ProtoMetal, GenreFamily.Rock, 1968, null, .85f, .40f, .01f,.01f,.02f,.05f,.10f,.20f,.35f);
		Add("progressive-rock", Genre.ProgressiveRock, GenreFamily.Rock, 1968, null, .80f, .25f, .01f,.01f,.02f,.05f,.10f,.25f,.40f);
		Add("blues-rock", Genre.BluesRock, GenreFamily.Rock, 1966, null, .80f, .45f, .02f,.05f,.10f,.18f,.24f,.32f,.36f);
		Add("proto-punk", Genre.ProtoPunk, GenreFamily.Rock, 1967, null, .85f, .40f, .01f,.01f,.02f,.05f,.15f,.25f,.30f);
		Add("british-beat", Genre.BritishBeat, GenreFamily.Rock, 1963, 1967, .90f, .75f, .01f,.02f,1.00f,.53f,.37f,.30f,.19f);
		// THE LARGEST SINGLE MISS IN THE MODEL. The old curve ramped to 1.00 -- the highest value in
		// the catalog -- and realised 12.8% of the 1969 market and 27 of the 100 year-end slots,
		// against a 4.09% market target and a hand-counted 0. British blues was a real but modest
		// commercial category (Mayall, early Fleetwood Mac); the heavy end of that sound belongs to
		// BluesRock and HardRock, which are separately authored and under-supplied. The curve still
		// RISES across the decade, because the market target does -- it is scaled down, not killed.
		Add("british-blues", Genre.BritishBlues, GenreFamily.Rock, 1964, null, .85f, .40f, .01f,.02f,.16f,.44f,.44f,.54f,.69f);
		// R&B and soul are ONE handover, not two independent curves, so they are authored together.
		// R&B is the older trade name: in 1960 it covers everything Black radio played, and across the
		// decade the music it named was largely renamed soul rather than replaced.
		//
		// The previous pass OVERCORRECTED this. R&B was tapered hard to .20 and realised 1.9% at 1969
		// against a 7.74% market target, so the handover ran the trade name to near-extinction when
		// history keeps it as a substantial category all decade. The taper is now gentle.
		//
		// The 20-25% family ceiling quoted here previously is SUPERSEDED. It was an estimate; the
		// historical target table is explicit and internally normalized, and it puts Soul + R&B +
		// Gospel + Funk at roughly 32% by 1969. The family constraint is real but it sits higher
		// than the old note assumed.
		Add("rnb", Genre.RnB, GenreFamily.RhythmAndSoul, 1949, null, .70f, .80f, .36f,.48f,.49f,.50f,.48f,.46f,.42f);
		// Soul was cut too far by the previous pass and is now the single largest under-supply in the
		// catalog. It realised 1.9 / 3.5 / 4.7 / 6.7 / 9.1 / 11.4 / 14.0 percent of the market at
		// 1960/62/64/66/67/68/69 against targets of 7.9 / 10.2 / 12.2 / 16.0 / 18.1 / 18.5 / 17.5,
		// and took 14 of 100 year-end slots at 1969 against a hand-counted 28. BOTH benchmarks agree
		// and both say the same thing: soul is roughly half of what it should be in every single
		// year, and the early decade is the worst of it -- Motown, Stax and Atlantic were a major
		// commercial force from 1960, not from 1966.
		//
		// The earlier "~13% at 1967" authoring aim is superseded by the target table's 18.08%; that
		// figure was set when soul was measured at 23.7% and being cut downward, and it undershoots.
		// SECOND PASS: the first correction overshot to 27.9% at 1969 against a 17.5% target, because
		// it was sized linearly against a quadratic transfer. Re-sized by sqrt(target/current); note
		// the curve is now much FLATTER late than intuition suggests, because soul's efficiency rises
		// steeply once British Blues and Gospel stop taking a quarter of the late-decade market.
		Add("soul", Genre.Soul, GenreFamily.RhythmAndSoul, 1960, null, .75f, .80f, .41f,.63f,.73f,.75f,.76f,.72f,.70f);
		Add("funk", Genre.Funk, GenreFamily.RhythmAndSoul, 1967, null, .80f, .70f, .02f,.05f,.10f,.25f,.40f,.55f,.70f);
		Add("doo-wop", Genre.DooWop, GenreFamily.RhythmAndSoul, 1954, 1965, .80f, .85f, .84f,.37f,.11f,.04f,.04f,.02f,.01f);
		// Gospel's EARLY years were already correct (1.1% realised against a 1.1% target at 1960);
		// only the late ramp was wrong, and it was very wrong -- .75 at 1969 realised 11.3% of the
		// market and 20 of 100 year-end slots against a 2.63% target and a hand-counted 1. So this is
		// not a flat cut: the early keyframes are held and the late rise is removed, keeping a small
		// 1969 crossover tick for the genuine pop breakthrough ("Oh Happy Day").
		Add("gospel", Genre.Gospel, GenreFamily.Gospel, 1950, null, .50f, .70f, .34f,.32f,.33f,.26f,.25f,.24f,.31f);
		// TENSION, stated rather than split the difference silently: country's MARKET share is BELOW
		// target (6.6% against 11.69% at 1969) while its CHART presence is far above (13 of 100
		// year-end slots against a hand-counted 3). The baseline is raised to serve the market
		// benchmark, which is the one a baseline controls. That will make the chart over-presence
		// worse until the chart-side divergence is addressed -- country, jazz, classical and comedy
		// all chart well above what their units justify, and that is one shared defect, not four.
		Add("country", Genre.Country, GenreFamily.Country, 1950, null, .40f, .65f, .52f,.58f,.55f,.60f,.58f,.64f,.68f);
		Add("country-rock", Genre.CountryRock, GenreFamily.Country, 1968, null, .70f, .40f, .01f,.02f,.05f,.10f,.20f,.40f,.55f);
		Add("folk", Genre.Folk, GenreFamily.Folk, 1958, 1966, .60f, .50f, .42f,.44f,.37f,.27f,.27f,.25f,.28f);
		Add("folk-rock", Genre.FolkRock, GenreFamily.Folk, 1965, null, .80f, .55f, .02f,.02f,.12f,1.00f,.80f,.71f,.68f);
		Add("contemporary-folk", Genre.ContemporaryFolk, GenreFamily.Folk, 1961, 1969, .60f, .50f, .10f,.40f,.55f,.45f,.40f,.40f,.40f);
		Add("singer-songwriter", Genre.SingerSongwriter, GenreFamily.Folk, 1967, null, .65f, .35f, .02f,.05f,.10f,.20f,.30f,.40f,.50f);
		// NOTE, and it cuts against the surface reading: jazz is UNDER its market target late (1.4%
		// realised against 4.09% at 1969), not over. What is too high is its CHART presence -- 5 of
		// 100 year-end slots at 1960 against a hand-counted 0. Lowering the baseline would worsen the
		// market benchmark to chase the chart one, so the baseline is raised toward the market target
		// and the singles-chart over-presence is left to the chart-side divergence work.
		Add("jazz", Genre.Jazz, GenreFamily.Jazz, 1945, null, .35f, .30f, .49f,.40f,.34f,.36f,.39f,.38f,.38f);
		Add("bossa-nova", Genre.BossaNova, GenreFamily.Jazz, 1962, 1967, .40f, .45f, .05f,.50f,.55f,.40f,.30f,.25f,.20f);
		Add("blues", Genre.Blues, GenreFamily.Blues, 1945, null, .45f, .50f, .30f,.30f,.30f,.35f,.40f,.40f,.40f);
		// Classical's MARKET share is roughly right; what is wrong is that it reaches a SINGLES chart
		// at all (1-7 year-end slots against a hand-counted 0 in every year). SingleOrientation .15
		// -> .04 is the demand-side half of that: it is an album category almost by definition. If
		// slots persist after this, the remainder is chart-side and belongs with the Jazz/Country
		// divergence work rather than with another baseline cut.
		Add("classical", Genre.Classical, GenreFamily.Classical, 1945, null, .20f, .04f, .40f,.25f,.23f,.25f,.29f,.35f,.36f);
		Add("boogaloo", Genre.Boogaloo, GenreFamily.Latin, 1966, 1969, .70f, .70f, .02f,.05f,.10f,.35f,.40f,.35f,.25f);
		Add("tex-mex", Genre.TexMex, GenreFamily.Latin, 1959, null, .65f, .75f, .15f,.20f,.25f,.30f,.30f,.30f,.30f);
		Add("latin-pop", Genre.LatinPop, GenreFamily.Latin, 1958, null, .55f, .60f, .20f,.25f,.30f,.35f,.35f,.35f,.35f);
		Add("ska", Genre.Ska, GenreFamily.Caribbean, 1964, 1967, .60f, .80f, .01f,.02f,.05f,.10f,.12f,.10f,.08f);
		Add("rocksteady", Genre.Rocksteady, GenreFamily.Caribbean, 1966, 1968, .60f, .80f, .01f,.01f,.02f,.08f,.12f,.12f,.10f);
		Add("reggae", Genre.Reggae, GenreFamily.Caribbean, 1968, null, .65f, .80f, .01f,.01f,.02f,.03f,.05f,.10f,.20f);
		// 1960 was on target (0.8%) and every later year ran 3-4x over against a flat ~0.6-0.8%
		// historical line. The authored mid-decade bulge is not a real commercial pattern: the
		// comedy LP boom was an ALBUM phenomenon and should not inflate the singles market.
		Add("comedy", Genre.Comedy, GenreFamily.NonMusic, 1955, null, .50f, .15f, .53f,.37f,.28f,.30f,.27f,.28f,.23f);
		Add("childrens", Genre.Childrens, GenreFamily.NonMusic, 1950, null, .50f, .30f, .35f,.35f,.35f,.35f,.35f,.35f,.35f);
		AllProfiles = new ReadOnlyCollection<GenreProfile>(new List<GenreProfile>(Profiles.Values));
	}

	// RADIO ACCEPTANCE -- the airplay-side companion to the baseline, and the missing
	// chart-efficiency dimension (handoff section 11.5 step 1). One authored scalar (the
	// baseline) formerly drove BOTH channels: sales as ~baseline^2 (the transfer law) and
	// airplay as ~baseline^5 (AIRPLAY_CONVEXITY inside UpdateRadioHeat's * genreAcceptance).
	// That coupling made it physically impossible for a genre to be small-selling yet
	// heavily programmed, which is exactly what a singles-driven AM-Top-40 genre was.
	//
	// This multiplies the national acceptance that feeds radio heat ONLY
	// (ChartSimulator.UpdateRadioHeat via ChartManager, the single caller of
	// GetNationalDemandAcceptance) and never touches the sales acceptance
	// (GetRegionalDemandAcceptance) or the divided-out radio access. Default 1.0 = neutral.
	//
	// Amplified by AIRPLAY_CONVEXITY = 5, so a ratio r here lands as roughly r^5 on airplay
	// points (less, because genreAcceptance scales only the quality/push/momentum block of
	// radioHeat, not the additive artistHeat and position bonuses). Its effect is
	// concentrated LATE by construction: the airplay era weight ramps 0.60->1.00 across
	// 1960-68, so airplay is ~14% of chart points in 1960 and ~58% in 1969. That is why this
	// is the right lever for late-decade errors and cannot reach early-decade ones -- the
	// split is by WHEN the error happens, not which genre it is.
	//
	// - SunshinePop UP: its whole deficit is 1966-69, the airplay-rich end; a small-selling
	//   AM-Top-40 genre that charted heavily. Because the keyframe and this multiplier COMPOUND
	//   in the airplay channel (both land inside the 5th power), the slot fix is split between a
	//   gentle 1969-keyframe correction (.22 -> .28) and a modest r here (1.40x), NOT a large r.
	// - Country / PsychedelicRock DOWN: their whole surplus is 1965-69 (Country is UNDER
	//   early, so the late-biased lever leaves the early years alone). A near-full pop-radio
	//   strip is intended for Country -- it charted on its own country radio, not the Hot 100
	//   panel. PsychedelicRock needs only a moderate cut.
	// Jazz and Folk are NOT here: their surplus is early-decade, where a full airplay strip
	// removes only 14-37% of points. They are format/denominator work (step 2), not airplay.
	private static readonly IReadOnlyDictionary<Genre, float> RadioAcceptanceOverrides =
		new Dictionary<Genre, float> {
			[Genre.SunshinePop] = 1.40f,
			[Genre.GarageRock] = 1.55f,
			[Genre.Country] = 0.45f,
			[Genre.PsychedelicRock] = 0.90f,
		};

	/// <summary>Per-genre radio-acceptance multiplier applied to the national acceptance that feeds
	/// radio heat only. Expects a canonical genre; default 1.0 leaves a genre's airplay untouched.</summary>
	public static float GetRadioAcceptance(Genre canonical) =>
		RadioAcceptanceOverrides.TryGetValue(canonical, out float r) ? r : 1f;

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
