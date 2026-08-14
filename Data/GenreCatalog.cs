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

	/// <summary>The highest baseline this genre has reached at or before `year` -- its peak so far. A
	/// decline signal built as GetBaseline(year)/GetBaselinePeakThrough(year) stays 1.0 while a genre is
	/// still rising toward its peak (so emergent genres are never penalised) and falls only once the
	/// genre is past its own peak.</summary>
	public float GetBaselinePeakThrough(float year) {
		float clamped = Mathf.Clamp(year, 1960f, 1969f);
		int[] years = { 1960, 1962, 1964, 1966, 1967, 1968, 1969 };
		float peak = GetBaseline(clamped);
		for (int i = 0; i < years.Length; i++) {
			if (years[i] > clamped) break;
			if (BaselineKeyframes[i] > peak) peak = BaselineKeyframes[i];
		}
		return peak;
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
		// Mid trim + late lift (2026-08): over mid on BOTH charts (single 24/14 vs 15/8 at '62/'66; album
	// 41% vs ~30 at '62). Album affinity trimmed 0.50->0.44. V3: the late keyframes over-lifted -- album
	// ran 15-16% at 1969 (both seeds) vs a 7 target, so 1968 .46->.40 and 1969 .50->.38 pull the late
	// decline back down toward target.
	Add("traditional-pop", Genre.TraditionalPop, GenreFamily.Pop, 1950, 1971, .15f, .45f, .42f,.40f,.44f,.36f,.42f,.40f,.38f);
		// Death 1965 -> 1971. IsAvailableForNewSupply returns false once year > DeathYear, so new
		// supply was exactly zero from 1966 and the genre realised 0.0% against a 1.17% target at
		// 1969 -- the implied baseline was 9.69 against a 1.0 cap, i.e. unreachable by keyframe.
		// The late-decade survivor is manufactured teen-aimed pop (the Monkees), which is a real
		// category and is what the market target's 1.70/1.17 at 1968/69 describes.
		// Early-mid over-supply trim (over at 17/27 slots '60/'62 vs 13/13). Two seeds confirmed a real early
	// over; V3 cuts the early keyframes again (still 17/22 vs 13/13 at .50/.48): 1960 .50->.44, 1962
	// .48->.42, 1964 .50->.46. Late keyframes hold the authored decline.
	Add("teen-pop", Genre.TeenPop, GenreFamily.Pop, 1957, 1971, .90f, .90f, .44f,.42f,.46f,.43f,.31f,.26f,.21f);
		Add("baroque-pop", Genre.BaroquePop, GenreFamily.Pop, 1966, 1970, .60f, .50f, .02f,.02f,.06f,.38f,.34f,.21f,.13f);
		// CURVE RESHAPED, not levelled (radio branch, 2026-08): .49/.46/.35/.28 -> .35/.42/.45/.50.
		// The old curve peaked 1966 and decayed; the hand-counted year-end slots do the OPPOSITE
		// (4/10/4/12), peaking 1969 -- the model's authored lifecycle and the chart benchmark pointed
		// in opposite directions. Three things follow from rising into 1969 instead of levelling the
		// endpoint alone:
		//   1. 1969 market share ~1.1% -> ~3.5% (transfer is quadratic: (.50/.28)^2), worth ~6 year-end
		//      slots at the model's p95 realised chart efficiency of 1.84 slots per 1% unit share.
		//   2. It REMOVES the PANEL_LIFECYCLE_PULL damping for free. Vitality reads
		//      GetBaseline(y)/GetBaselinePeakThrough(y), so on the old declining curve Sunshine Pop was
		//      multiplied by .963/.829/.743 at 1967/68/69 -- damped hardest exactly where the benchmark
		//      wants 10/4/12 slots. A monotone riser has peak == current, so vitality is 1.000 always.
		//   3. 1966-67 stay near their market targets (1.47/1.87) instead of being inflated to 2.4/3.2,
		//      which is how radio-guard-1001 "charted" the genre and why that result did not hold.
		// DELIBERATELY SHORT of the benchmark. The 1969 hand count of 12 slots against a 0.73% market
		// target demands 16.4x chart efficiency -- the highest of any genre-year in the benchmark pair,
		// and 1.7x beyond anything the model has ever produced. The market row's own note says why:
		// "Retrospective label ... Real but never a trade category" -- the 1969 records it counts are
		// booked as pop/EL in the market table, so the two benchmarks are counting different records.
		// ~13 of the 30 benchmark slots is the honest ceiling; the rest is a classification artifact.
		Add("sunshine-pop", Genre.SunshinePop, GenreFamily.Pop, 1965, 1971, .65f, .55f, .02f,.03f,.10f,.35f,.42f,.45f,.50f);
		// Late trim (2026-08): over 1969 (11 vs 4). First cut to .38/.45 overcorrected 1969 to under (1 vs 4);
	// 1969 restored .45 -> .55.
	Add("bubblegum", Genre.Bubblegum, GenreFamily.Pop, 1967, 1971, .95f, .90f, .01f,.02f,.03f,.05f,.16f,.38f,.55f);
		// ALBUM INVERSION FIX (2026-08): EL album ran OVER early (20% vs 8) and UNDER late (9% vs 14-24) --
	// the Alpert/Tijuana-Brass MOR album wave rose across the decade, the model fell. Flat affinity
	// cannot invert, so the early TRIM is done off the album-only affinity (0.65->0.34) and the LATE
	// lift off the baseline. V3: the affinity cut (which also touches MID album) plus a mid-baseline dip
	// left EL album under mid (2.7% at 1964 vs 11); but EL/TJB actually PEAKED 1966 ("4 TJB albums in the
	// top 10 simultaneously, 1966" -- bench note). So the mid/late keyframes are raised into a mid-late
	// peak: .45/.46/.53/.60/.66 -> .52/.58/.62/.68/.74, lifting mid-late album AND EL's under mid-late
	// singles. See MarketRegion EasyListening affinity note.
	Add("easy-listening", Genre.EasyListening, GenreFamily.Pop, 1950, null, .15f, .35f, .60f,.54f,.52f,.58f,.62f,.68f,.74f);
		// Invasion-peak trim (2026-08): British Beat/Pop over at '64-65 (13-18 vs 5-8). 1964 .95 -> .65.
	Add("british-pop", Genre.BritishPop, GenreFamily.Pop, 1964, 1968, .90f, .80f, .01f,.02f,.65f,.50f,.43f,.35f,.30f);
		// Early Rock and Roll ran 20.5% against a 13.5% target at 1960 -- the single largest year-over in
		// the catalog, and pure baseline (the radio panel does not touch RnR in 1960, since it is pre-peak
		// so the lifecycle vitality is neutral). The 1960 keyframe is cut .48 -> .40 to bring 1960 market
		// share down to ~target at the SOURCE, rather than damping RnR's airplay at runtime (a vacuum-guard
		// approach that shed RnR's airplay into TradPop/Soul and halved its 1960 #1 weeks). The freed 1960
		// share redistributes benignly to the many under-target 1960 genres (RnB gains most), unlike the
		// airplay guard which flooded already-over genres. RnR 1960 share responds ~LINEARLY to this
		// keyframe (measured exponent ~1.15 over .48->.40, not the catalog-typical ~2), so the cut is sized
		// off a two-point fit: .48->.35 lands 1960 near its 13.5 target.
		//   1962 .46 -> .42: with 1960 at .35, leaving 1962 at .46 made 1962 a relative SUPPLY SURGE
		// (1960-61 thinned, 1962 jumps back up), which dominated the year-end recap -- RnR charted 27 slots
		// at 1962 vs a 12 benchmark (at the flat V3.1 .48/.46 keyframe it was 21; the jump added the rest).
		// RnR's 1962 market also ran +1.0 OVER target, so lowering this keyframe pulls the 1962 chart bulge
		// AND the 1962 market both toward target. RadioAcceptance is deliberately NOT used to trim RnR's
		// chart: a short-run test (RA 1.10->0.80) confirmed it bleeds RnR's market -- incl. the 1960 win --
		// because RnR airplay feeds its units and RA is flat across years. The small residual over-charge
		// (17 vs 14 at 1960) is a hit-making singles genre charting hard, near the noise floor; accepted.
		// The authored late-decade decline is deliberately preserved: the year-end benchmark's late RnR
		// counts are misclassification of genre-ambiguous records, not a surviving commercial category.
		Add("rock-and-roll", Genre.RockAndRoll, GenreFamily.Rock, 1955, null, .85f, .85f, .35f,.42f,.42f,.22f,.15f,.10f,.07f);
		// Emergence 1961 -> 1960: "Walk Don't Run" charted in 1960, so the genre exists at the
		// game's start date rather than opening a year in.
		Add("surf-rock", Genre.SurfRock, GenreFamily.Rock, 1960, 1966, .90f, .70f, .12f,.48f,.78f,.26f,.20f,.16f,.06f);
		// Mid trim (2026-08): over mid-60s (9-11 vs 1-3); paired with the RadioAcceptance 1.55->1.05 cut.
	Add("garage-rock", Genre.GarageRock, GenreFamily.Rock, 1963, 1968, .90f, .85f, .08f,.14f,.30f,.34f,.30f,.26f,.22f);
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
		// Invasion-peak trim (2026-08): 1964 1.00 -> .68 (see british-pop note).
	Add("british-beat", Genre.BritishBeat, GenreFamily.Rock, 1963, 1967, .90f, .75f, .01f,.02f,.68f,.53f,.37f,.30f,.19f);
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
		// V3 trim: R&B ran slightly over (10-15 slots vs 8-12) and the IntegrationEraGapClose 0.45->0.70
	// lift (for soul album) raises R&B units too, so the baseline is trimmed. First pass over-cut 1969
	// (3 vs 8) and left a 1962 spike (21 vs 11), so 1962 .45->.42 and the late keyframes eased back up.
	// LATE TAPER (radio branch, 2026-08). The baseline was FLAT at ~.42 to 1969, but "R&B" as a Hot 100
	// chart category did not survive the decade -- it was progressively re-labelled Soul, which is why the
	// slot benchmark reads 12/18/28/21/10/2/3/0/0/0 and why there is no market-share target row for R&B at
	// all. A flat baseline keeps producing R&B singles forever: on mix5-decade 1965-69 ran 9/8/5/6/7 slots
	// against a 2/3/0/0/0 benchmark, a 30-slot surplus, on 6.78% of 1969 SINGLES units. The first four
	// keyframes are held EXACTLY -- 1960-64 scores 15/20/25/17/10 against 12/18/28/21/10 and must not move
	// -- and only 1966/1967.5/1969 are cut. Share goes as baseline^2, so .43->.36, .42->.26, .42->.18 is
	// ~0.70x/0.38x/0.18x of realised share in those years. The freed units land on Soul (172 vs a 179
	// benchmark) and Funk (6 vs 12), both under, which is where the historical re-labelling sent them.
	// PARTIAL BACK-OUT (mix6 result): 1967.5/1969 held -- 1967-69 landed 4/1/0 against a 0/0/0
	// benchmark, exactly the intent -- but 1966 is restored .36 -> .41. Stated correction: the original
	// note assumed the .36 cut was what worsened 1966, and that does not survive the run. RnB 1966 rose
	// 8 -> 11 slots WHILE its baseline fell, because cutting supply lowers a genre's realised top-20
	// share, which makes the chart guard damp it LESS and lets its remaining records chart higher. Same
	// self-healing shape as the absorption-trim result. So 1966 is restored to keep the guard engaged
	// there, not because the cut "did damage" in the direction first claimed.
	Add("rnb", Genre.RnB, GenreFamily.RhythmAndSoul, 1949, null, .70f, .80f, .34f,.44f,.42f,.44f,.41f,.26f,.18f);
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
		// SOUL RE-SPLIT (2026-08, D7 genre-arc pass 2). Two opposite-direction misses on ONE genre: singles
	// are UNDER early (2 vs 8 at 1960) and WERE WAY OVER late (32 vs 18), while the album is UNDER late
	// (7 vs 22). The airplay-down RadioAcceptance 0.72 fixed the late-singles over (now ~14, near the 17
	// target); the flat SingleOrientation .66 leans soul slightly toward album vs the original .80; the
	// 1960 baseline .41 -> .52 nudges the early-singles under. NOTE the late album (7% vs 22) is NOT
	// fixed here and is NOT a routing problem -- a pass-3 orientation ramp was tested on two seeds and
	// left it at ~7% (see LateFormatOrientationOverrides): soul album is bounded by the RhythmAndSoul
	// segment album buyer pool, which is the open lever for it.
	Add("soul", Genre.Soul, GenreFamily.RhythmAndSoul, 1960, null, .75f, .66f, .52f,.63f,.73f,.75f,.76f,.72f,.70f);
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
		// Peak trim (2026-08): over from '66 (15/14/14/8). The first sqrt-trim to .60/.52/.45/.48
	// OVERCORRECTED to under (2/2/1/4 vs 5/6/5/4), so restored partway: .78/.62/.52/.55.
	Add("folk-rock", Genre.FolkRock, GenreFamily.Folk, 1965, null, .80f, .55f, .02f,.02f,.12f,.78f,.62f,.52f,.55f);
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
		// Album 1960 over-seed trim (2026-08): classical album ran 23% at 1960 vs ~0-4. The 1960 baseline
	// .40 -> .26 pulls the early album down (singles are already ~0 via orientation .04); album affinity
	// also cut 0.82 -> 0.45. Later keyframes hold (late classical album is already small).
	Add("classical", Genre.Classical, GenreFamily.Classical, 1945, null, .20f, .04f, .26f,.25f,.23f,.25f,.29f,.35f,.36f);
		Add("boogaloo", Genre.Boogaloo, GenreFamily.Latin, 1966, 1969, .70f, .70f, .02f,.05f,.10f,.35f,.40f,.35f,.25f);
		Add("tex-mex", Genre.TexMex, GenreFamily.Latin, 1959, null, .65f, .75f, .15f,.20f,.25f,.30f,.30f,.30f,.30f);
		Add("latin-pop", Genre.LatinPop, GenreFamily.Latin, 1958, null, .55f, .60f, .20f,.25f,.30f,.35f,.35f,.35f,.35f);
		Add("ska", Genre.Ska, GenreFamily.Caribbean, 1964, 1967, .60f, .80f, .01f,.02f,.05f,.10f,.12f,.10f,.08f);
		Add("rocksteady", Genre.Rocksteady, GenreFamily.Caribbean, 1966, 1968, .60f, .80f, .01f,.01f,.02f,.08f,.12f,.12f,.10f);
		Add("reggae", Genre.Reggae, GenreFamily.Caribbean, 1968, null, .65f, .80f, .01f,.01f,.02f,.03f,.05f,.10f,.20f);
		// 1960 was on target (0.8%) and every later year ran 3-4x over against a flat ~0.6-0.8%
		// historical line. The authored mid-decade bulge is not a real commercial pattern: the
		// comedy LP boom was an ALBUM phenomenon and should not inflate the singles market.
		// SingleOrientation .15 -> .22: comedy singles died entirely after 1961 (0 vs a steady ~0.7%
		// hand count) -- the novelty single (Chipmunks, "Hello Muddah") was a small but persistent AM
		// category. A modest single lean restores a thread of comedy singles without inflating the
		// album LP boom (which the raised album affinity drives). Baseline shape unchanged.
		Add("comedy", Genre.Comedy, GenreFamily.NonMusic, 1955, null, .50f, .22f, .53f,.37f,.28f,.30f,.27f,.28f,.23f);
		Add("childrens", Genre.Childrens, GenreFamily.NonMusic, 1950, null, .50f, .30f, .35f,.35f,.35f,.35f,.35f,.35f,.35f);
		// THREE LATE-DECADE ADDITIONS (2026-08, D7 soundtrack/genre-arc pass). Author-requested genres
		// present in the album handcount but absent from the earlier singles list, so shapes are by
		// discretion (baseline sized ~sqrt against comparable rising late-60s genres like FolkRock/
		// HardRock/Bubblegum, which sit .55-.71 at 1969). Both formats: baseline sizes overall units,
		// SingleOrientation splits single vs album. Keyframes at 1960/62/64/66/67/68/69.
		// Psychedelic Pop -- Pet Sounds, Donovan; pop-leaning psych, emerges 1966, peaks 66-67, fades.
		Add("psychedelic-pop", Genre.PsychedelicPop, GenreFamily.Pop, 1966, 1971, .70f, .55f, .02f,.02f,.02f,.28f,.34f,.24f,.15f);
		// Pop Rock -- Neil Diamond, Three Dog Night, Abbey Road-era Beatles; broad mainstream, rises late,
		// strong on BOTH charts (big singles acts with major albums).
		Add("pop-rock", Genre.PopRock, GenreFamily.Pop, 1967, null, .70f, .60f, .01f,.01f,.02f,.05f,.25f,.42f,.55f);
		// Roots Rock -- CCR, The Band, roots-era Dylan; emerges 1968, big by 1969, singles-and-albums.
		Add("roots-rock", Genre.RootsRock, GenreFamily.Rock, 1968, null, .65f, .55f, .01f,.01f,.02f,.02f,.06f,.32f,.50f);
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
			// Garage 1.55 -> 1.05: the up-lever was amplifying a genre that is already OVER at its
			// mid-60s peak (9-11 year-end slots vs a 1-3 hand count). Paired with a mid-baseline trim.
			[Genre.GarageRock] = 1.05f,
			[Genre.Country] = 0.45f,
			// PsychedelicRock 0.90 -> 1.20 (reporter-panel compensation): the AM-Top40 reporter panel
			// correctly under-plays psych rock (it lived on album/FM), which pulled it 8%->3% chart
			// share vs V3.1. Raise its national airplay propensity to restore the V3.1 singles bound.
			[Genre.PsychedelicRock] = 1.20f,
			// Soul 0.72 (NEW): the airplay lever is the right one for Soul's headline miss because the
			// miss is LATE (32 year-end slots vs 18) where airplay is ~58% of chart points, and Soul is
			// UNDER early where airplay barely counts -- so a late-biased strip trims the over without
			// touching the early under. Works WITH the SingleOrientation .80->.66 re-split (single->album).
			// 0.72 -> 0.55 (V3): raising IntegrationEraGapClose (soul album fix) added ~+10 SALES to soul
			// late singles (24 vs 18), so more airplay must be stripped to rebalance. Album-safe -- RA
			// never touches album units, so this pulls late singles back without disturbing the soul-album
			// win (15-19% at 1966-68).
			[Genre.Soul] = 0.55f,
			// Comedy 1.70 (NEW): comedy singles were absent (0 vs a persistent ~0.7% AM-novelty count) and
			// SingleOrientation could not surface them. Comedy has no Zeitgeist acceptance entry so it sits
			// at UnestablishedAcceptance 0.30; the airplay channel is the lever that had never been applied
			// to comedy. A 1960-62 probe at 2.20 surfaced 1/3/4 year-end slots (over the ~0.7 target and a
			// late-blowup risk since airplay is 58% of points by 1969). At 1.70 a decade run gave 3/3 slots
			// in 1960-61 then 0 from 1962 -- comedy UNITS collapse late (baseline .53->.23) so airplay
			// alone cannot chart it late. Nudged to 1.90 to sustain the thread a little longer; a true flat
			// line would need a late baseline floor (kept off to preserve the good comedy-album shape).
			[Genre.Comedy] = 1.90f,
			// Jazz airplay lever REVERTED (was 1.30): a decade run showed it BACKFIRED -- jazz units
			// collapse late (baseline .38) so airplay could not chart it late (still 0-3 vs 4), while the
			// r^5 amplification + field shift inflated the EARLY years it was never meant to touch (12 vs 6
			// slots at 1960). This confirms the documented rule: jazz's chart surplus is EARLY-decade and
			// is format/denominator work, NOT airplay. Jazz stays at the neutral 1.0.
			// Blues 1.15 (NEW): blues singles were absent (0 vs a steady ~2-3). A modest airplay channel
			// (no Zeitgeist entry, so 0.30 base) surfaces a thread; eased 1.30->1.15 after a mid-decade
			// spike (6 vs 2 at 1967).
			[Genre.Blues] = 1.15f,
			// EL 0.62 (NEW): fixing EL's late album under (via the raised baseline) SPILLED onto its
			// singles -- late EL singles ran 9-10 vs a 3-4 target because baseline drives both charts. The
			// airplay-down lever is the clean separator: it strips the late (airplay-rich) EL singles the
			// baseline over-produced WITHOUT touching the album (albums have no airplay channel). 0.62 then
			// slightly over-stripped (late EL singles 1-2 vs 3-4), so eased to 0.72.
			[Genre.EasyListening] = 0.72f,
			// ---- Reporter-panel compensation pass (radio branch, w=0.13). The AM-Top40 reporter panel
			// adds genre-appropriate airplay differentiation that the old coarse tail lacked, so these
			// values offset the panel's chart-side drift to hold each genre near its V3.1 singles bound.
			// Conservative first pass (RadioAcceptance is ^5-amplified in airplay); residuals are a
			// tracked follow-up. Over-charters trimmed, under-charters (rock/psych routed off Top40) lifted.
			[Genre.Bubblegum] = 0.88f,        // was over +4.8pp
			[Genre.BritishPop] = 0.88f,       // was over +3.4pp
			[Genre.TeenPop] = 0.90f,          // was over +3.1pp
			[Genre.PopRock] = 1.20f,          // was under -2.1pp
			[Genre.RockAndRoll] = 1.10f,      // RnR over-charts early (17/19/27/21 slots 1960-63 vs a 14/12/12/11
			                                  // benchmark) while its market sits on target -- a market-vs-chart
			                                  // conflict. A short-run test of RA=0.80 confirmed RA is the WRONG
			                                  // lever here: it bled RnR's market (incl. the 1960 win) for only a
			                                  // partial chart trim, because RnR airplay feeds its units too and
			                                  // RA is flat across years. Left at the market-validated 1.10; the
			                                  // early chart-over is accepted as a documented divergence (RnR is
			                                  // exempt from the chart guard, which is gated to 1965+ anyway).
			[Genre.HardRock] = 1.15f,         // was under -1.5pp
			[Genre.Funk] = 1.12f,             // was under -1.4pp
			[Genre.BritishBlues] = 1.15f,     // was under -1.35pp
			[Genre.RootsRock] = 1.12f,        // was under -1.1pp
		};

	/// <summary>Per-genre radio-acceptance multiplier applied to the national acceptance that feeds
	/// radio heat only. Expects a canonical genre; default 1.0 leaves a genre's airplay untouched.</summary>
	public static float GetRadioAcceptance(Genre canonical) =>
		RadioAcceptanceOverrides.TryGetValue(canonical, out float r) ? r : 1f;

	// ERA-RAMPED FORMAT ORIENTATION -- the new lever for a genre that must move single->album ACROSS
	// the decade, which a flat SingleOrientation cannot express. The flat value drives the whole
	// catalog; this optional override supplies a genre's 1969 routing orientation, and
	// GetFormatOrientation lerps flat(1960) -> override(1969). It feeds the format-routing tilt ONLY
	// (GenreAcceptanceService.GetFormatMultiplier); the flat SingleOrientation property still drives
	// AlbumModel compilation-chance and telemetry, so this does not disturb those seams.
	//
	// SOUL RAMP TESTED AND REMOVED (2026-08, two-seed decade run). The hypothesis: ramp soul .78->.50
	// to keep it single-heavy early and album-heavy late, lifting the late album (9.6% vs 22). RESULT
	// on seeds 1001 AND 2002: soul album late stayed at ~7% (UNCHANGED -- the ramp is inert on it),
	// while soul's late SINGLES fell to 8 (vs a 17 target). Diagnosis: the binding constraint on soul
	// album is NOT the single/album routing but the soul-audience ALBUM BUYER POOL -- soul's baseline
	// .70 (high) yields only ~7% album while TradPop's .39 yields ~15%, i.e. RhythmAndSoul segments
	// have low album propensity in the demand model, so rerouting just makes soul albums that don't
	// chart and strips soul's late singles. The real soul-album lever is the SEGMENT album affinity
	// for soul's demographics (a MarketRegion segment-demand change), not orientation. Override cleared;
	// the GetFormatOrientation infrastructure is kept for a genre whose album side CAN absorb rerouting.
	private static readonly IReadOnlyDictionary<Genre, float> LateFormatOrientationOverrides =
		new Dictionary<Genre, float>();

	/// <summary>Format-routing orientation for the single/album tilt, era-aware. Returns the flat
	/// SingleOrientation for genres without an override; for those with one, lerps flat (1960) to the
	/// override (1969). Canonical genre expected.</summary>
	public static float GetFormatOrientation(Genre canonical, float year) {
		float flat = Get(canonical).SingleOrientation;
		if (!LateFormatOrientationOverrides.TryGetValue(canonical, out float late)) return flat;
		float t = Mathf.Clamp((year - 1960f) / 9f, 0f, 1f);
		return Mathf.Lerp(flat, late, t);
	}

	// ---- Hand-counted year-end Hot 100 CHART-SLOT benchmark (SimTools/D7GenreChartDivergenceHandoff.md
	// section 3), slots out of ~100 per year, 1960-1969. This is the CHART-side target (a ranked
	// 100-slot recap), a DIFFERENT object from the market-units-share target: a genre is meant to
	// over-chart relative to its unit share when it was radio-driven (Soul holds ~28 slots on ~17.5%
	// of units; Bubblegum holds 7-9 on ~4% -- 'Sugar Sugar' was 1969's best-selling single). Used by
	// the radio panel's chart-referenced vacuum guard as the expected chart presence, so the guard trims
	// only genres OVER-charting their historical slot share and reroutes the freed airplay to genres
	// under theirs. Genres absent here have no slot target -> the guard leaves them alone.
	//
	// EXPLICIT ZERO ROWS (radio branch, 2026-08). "Omitted as guard-inert" was wrong, and the stronger
	// top-20 guard proved it: a genre with no row is not un-guarded, it is guard-IMMUNE, so it becomes
	// the destination for every slot the guard frees elsewhere. Between mix4-decade and mix5-decade the
	// un-benchmarked bloc went 27 -> 47 decade slots, almost all of it LatinPop 14 -> 38 against a zero
	// benchmark, while LatinPop's market unit share barely moved (1968 2.85% -> 3.69%, 1969 3.01% ->
	// 3.04%) -- pure chart-access absorption. This is RockAndRoll's old ChartGuardExempt failure
	// relocated: with the biggest un-damped genre immune, the vacuum has exactly one place to go.
	// A zero row is a REAL target ("this genre held no year-end Hot 100 slots"), which for these is
	// simply true of the 1960s recaps, and it is a different statement from an absent row.
	private static readonly Genre[] ZeroSlotBenchmarkGenres = {
		Genre.LatinPop, Genre.PopRock, Genre.Boogaloo, Genre.TexMex, Genre.RootsRock,
		Genre.ContemporaryFolk, Genre.SingerSongwriter, Genre.Blues, Genre.BluesRock,
		Genre.BossaNova, Genre.BaroquePop, Genre.ProgressiveRock, Genre.ProtoMetal,
		Genre.AcidRock, Genre.ProtoPunk, Genre.PsychedelicPop, Genre.Childrens,
		Genre.Ska, Genre.Rocksteady, Genre.Reggae, Genre.Classical,
	};

	// Values below are the section-3 table verbatim.
	private static readonly Dictionary<Genre, int[]> ChartSlotBenchmark = new() {
		//                          1960 61  62  63  64  65  66  67  68  69
		[Genre.Soul]            = new[]{ 6,  9,  7, 15, 14, 22, 22, 28, 28, 28 },
		[Genre.TraditionalPop]  = new[]{22, 16, 15, 20, 14, 11,  8,  9, 12,  7 },
		[Genre.TeenPop]         = new[]{28, 18, 20,  8,  5,  7,  6,  9,  2,  2 },
		[Genre.RnB]             = new[]{12, 18, 28, 21, 10,  2,  3,  0,  0,  0 },
		[Genre.RockAndRoll]     = new[]{14, 12, 12, 11,  8,  3,  6,  6,  5,  2 },
		[Genre.Country]         = new[]{ 9,  7,  7,  8,  2,  1,  2,  2,  1,  3 },
		[Genre.DooWop]          = new[]{ 7, 10,  6,  3,  0,  0,  0,  1,  0,  0 },
		[Genre.EasyListening]   = new[]{ 5,  8,  8,  5,  3,  4,  5,  0,  6,  8 },
		[Genre.SurfRock]        = new[]{ 3,  2,  1,  6,  9,  2,  3,  0,  0,  1 },
		[Genre.Comedy]          = new[]{ 3,  3,  2,  3,  1,  0,  0,  2,  0,  1 },
		[Genre.Folk]            = new[]{ 1,  1,  3,  7,  3,  2,  1,  2,  2,  0 },
		[Genre.BritishBeat]     = new[]{ 0,  0,  0,  0, 24, 15,  8,  3,  3,  0 },
		[Genre.BritishPop]      = new[]{ 0,  0,  0,  0,  2, 12,  6,  7,  3,  4 },
		[Genre.FolkRock]        = new[]{ 0,  0,  0,  0,  0, 12, 14,  6,  5,  3 },
		[Genre.GarageRock]      = new[]{ 0,  0,  0,  0,  3,  5, 12,  4,  1,  1 },
		[Genre.BritishBlues]    = new[]{ 0,  0,  0,  0,  0,  5,  3,  2,  0,  0 },
		[Genre.SunshinePop]     = new[]{ 0,  0,  0,  0,  0,  0,  4, 10,  4, 12 },
		[Genre.PsychedelicRock] = new[]{ 0,  0,  0,  0,  0,  0,  1,  9, 10,  6 },
		[Genre.Bubblegum]       = new[]{ 0,  0,  0,  0,  0,  0,  0,  0,  7,  9 },
		[Genre.Funk]            = new[]{ 0,  0,  0,  0,  0,  1,  0,  1,  4,  6 },
		[Genre.HardRock]        = new[]{ 0,  0,  0,  0,  0,  0,  0,  1,  5,  3 },
		[Genre.Jazz]            = new[]{ 0,  1,  1,  0,  1,  1,  0,  0,  1,  2 },
		[Genre.CountryRock]     = new[]{ 0,  0,  0,  0,  0,  0,  0,  0,  0,  6 },
	};

	private static readonly HashSet<Genre> ZeroSlotBenchmarkSet = new(ZeroSlotBenchmarkGenres);

	/// <summary>Historical year-end CHART-SLOT target for a genre in a year, as a fraction of the ~100
	/// slot chart (0..1). Returns false when the genre has no benchmark row, so the caller does not treat
	/// "no benchmark" as "target is zero" (an unbenchmarked genre is left un-guarded). A benchmark of 0
	/// slots is a real target (the genre should not chart that year) and returns true with share 0.</summary>
	public static bool TryGetChartSlotShare(Genre genre, int year, out float share) {
		share = 0f;
		if (ZeroSlotBenchmarkSet.Contains(genre)) return true;   // real zero target, not "no target"
		if (!ChartSlotBenchmark.TryGetValue(genre, out int[] slots)) return false;
		int clamped = Math.Clamp(year, 1960, 1969);
		share = slots[clamped - 1960] / 100f;
		return true;
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
		if (Profiles.Count != 45) throw new InvalidOperationException($"Expected 45 canonical genre profiles, found {Profiles.Count}.");
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
