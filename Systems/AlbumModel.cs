using System.Collections.Generic;
using System.Linq;
using Godot;

public static class AlbumModel {
	public static float EraWeightStartYear = 1960f;
	public static float EraWeightEndYear = 1968f;
	public static float CohesionRiseStartYear = 1964f;
	public static float CohesionRiseEndYear = 1968f;
	public static float CompilationDeclineStartYear = 1963f;
	public static float CompilationDeclineEndYear = 1969f;
	public static float MaximumCompilationChance = 0.97f;
	// Deliberately vanishing. Tuned against the measured concept count, not by intuition:
	// the target is a handful across 1965-66, not a wave.
	public static float EarlyStatementYear = 1965f;
	/// <summary>
	/// The pioneer bar, RE-SIZED (2026-08) against the distribution it is actually applied to.
	/// <para>
	/// It was 0.55, and it fired zero times in seven years. `statementExcellence` is now written
	/// to album telemetry, and measured over 22,595 artist albums it is **92.6% exact zeros** --
	/// it is a product of two terms each clamped to zero below 0.70, and most acts or most rooms
	/// fail one of them. Its p99 is .217 and its observed maximum is .789. A bar of 0.55 sits at
	/// the 99.96th percentile of that, so 0.55 was not a high bar, it was a closed door: with
	/// the 0.94 roll gate on top it projects to 0.5 pioneer albums across 1965-66 and 0.1
	/// records actually clearing the 0.62 statement condition.
	/// </para>
	/// <para>
	/// 0.10 sits at roughly the 97.6th percentile and still describes what it was authored to
	/// describe -- both the act and the room clearly above the competence threshold -- because
	/// the zeros are 92.6% of the field. Projected on the measured 1965-66 population it yields
	/// ~20 pioneer albums across the two years, **~5 records clearing the 0.62 statement
	/// condition** and **~1 formally minted as AlbumFormat.Concept**. That is Rubber Soul,
	/// Revolver, Pet Sounds and a Dylan record, and it leaves the "wave by 1968" shape alone --
	/// the concept count is 0 / 405 / 554 / 577 across 1966-69 and this adds about one to 1966.
	/// </para>
	/// <para>
	/// The bar only has teeth in 1965-66 by construction: from 1967 the era ramp puts the
	/// ordinary baseline above the pioneer floor of 0.80, so `Max(baseline, floor)` stops
	/// lifting anything. That is the window this path exists for.
	/// </para>
	/// </summary>
	public static float EarlyStatementExcellence = 0.10f;
	public static float EarlyStatementRollThreshold = 0.94f;
	public static float EarlyStatementCohesionFloor = 0.80f;

	// Exogenous curve. A future acclaim/legitimacy loop may add a bounded nudge.
	public static float GetAlbumEraWeight(int year) => Mathf.SmoothStep(0f, 1f,
		Mathf.Clamp((year - EraWeightStartYear) / (EraWeightEndYear - EraWeightStartYear), 0f, 1f));

	/// <summary>
	/// LP-RATIO RECALIBRATION (2026-08). Was a step that returned 0 until the album-adoption curve
	/// reached its midpoint (~1964), which zeroed the dedicated album retail channel early and forced
	/// pre-1964 albums to clear inside the single channel under a 2x overlap penalty -- displacing
	/// ~79% of serviceable album demand in 1960 and pinning LP unit share near zero. The LP retail
	/// channel existed in 1960; it was simply smaller. So retail fulfillment is now mature throughout
	/// the period (=1) and the channel's SIZE is carried entirely by its era-scaled capacity share
	/// (the AlbumChannelShareEra convex quadratic in ChartManager), which is the calibrated LP:45 lever.
	/// </summary>
	public static float GetRetailFulfillmentMaturity(int year) => 1f;

	/// <summary>
	/// How much of the record is the record: the mean track measured against the best one.
	/// 1.0 when every side is as strong as the strongest — a body of work — and low when one
	/// hit is carrying ten pieces of filler.
	/// <para>
	/// This is deliberately NOT <see cref="Album.thematicCohesion"/>, and the distinction is
	/// the whole point. Cohesion is the concept-album axis and is gated by the era ceiling,
	/// which pins it to the 0.08 clamp floor for every artist album before 1966. Rubber Soul
	/// and Pet Sounds were not concept albums; what made them landmarks is that they were
	/// albums rather than a smattering of singles with other songs around them. That property
	/// is a fact about the tracks and is available in any year, which is why the landmark rule
	/// is stated against this and not against cohesion.
	/// </para>
	/// </summary>
	public static float GetAlbumIntegrity(IEnumerable<float> trackQualities) {
		float[] qualities = trackQualities?.Select(quality => Mathf.Clamp(quality, 0f, 1f)).ToArray()
			?? System.Array.Empty<float>();
		if (qualities.Length == 0) return 0f;
		float peakTrack = qualities.Max();
		return peakTrack <= 0f ? 0f : Mathf.Clamp(qualities.Average() / peakTrack, 0f, 1f);
	}

	public static float CalculatePooledAppeal(IEnumerable<float> trackQualities, float thematicCohesion, int year) {
		float[] qualities = trackQualities?.Select(quality => Mathf.Clamp(quality, 0f, 1f)).ToArray()
			?? System.Array.Empty<float>();
		if (qualities.Length == 0) return 0f;
		float peakTrack = qualities.Max();
		float meanTrack = qualities.Average();
		float peakWeighted = 0.70f * peakTrack + 0.30f * meanTrack;
		float wholeWeighted = 0.45f * meanTrack + 0.35f * Mathf.Clamp(thematicCohesion, 0f, 1f) + 0.20f * peakTrack;
		return Mathf.Lerp(peakWeighted, wholeWeighted, GetAlbumEraWeight(year));
	}

	/// <summary>
	/// The exogenous cohesion ramp: how far the form itself had travelled in a given year,
	/// before anything about the act or the room is considered.
	/// <para>
	/// SEPARATED OUT AND FIXED (2026-08). This was
	/// <c>Mathf.SmoothStep(0.12f, 0.96f, t)</c>, written as though those were an output range.
	/// Godot's SmoothStep treats them as EDGES, so the term evaluated to
	/// <c>0, 0, .065, .429, .844, 1, 1</c> across 1963-69 -- zero until 1965 and pinned to the
	/// 0.08 clamp floor for every artist album before 1966, roughly two years later than every
	/// surrounding comment assumes. The intended reading is a smoothstep across the window
	/// mapped INTO [0.12, 0.96], which gives <c>.12, .12, .251, .540, .829, .96, .96</c>.
	/// </para>
	/// <para>
	/// The repair is close to monotonic, which is why it was safe to make. It raises 1964-66
	/// and trims 1967-69 by only 1.8% / 4% / 4% on the era term -- not the ~10% the directive
	/// estimated -- and less than that after the 1.0 clamp, which nearly every 1968-69 album is
	/// already sitting on. It does not open a concept-album wave in 1966 either: statementViable
	/// needs a 0.72 ceiling and 0.540 x the largest reachable talent/production multiplier
	/// (1.1775) is 0.636, so 1965-66 statements still come only through the pioneer path below,
	/// which is the pairing this fix was always supposed to ship with.
	/// </para>
	/// </summary>
	public static float GetCohesionEraTerm(int year) => Mathf.Lerp(0.12f, 0.96f,
		Mathf.SmoothStep(0f, 1f,
			Mathf.Clamp((year - CohesionRiseStartYear) / (CohesionRiseEndYear - CohesionRiseStartYear), 0f, 1f)));

	/// <summary>
	/// Near-top talent in a near-top room, as a single [0,1] score. Exposed because the pioneer
	/// bar is stated against it and a bar is meaningless until it is checked against the
	/// distribution it will be applied to -- which is why it is also written to album telemetry.
	/// <para>
	/// Note the reachable ceiling is well under 1: the highest measured label
	/// <c>productionQuality</c> is 0.91, so the production factor tops out at 0.70 and no album
	/// can score above 0.70 however good the act is.
	/// </para>
	/// </summary>
	public static float GetStatementExcellence(float artistTalent, float labelProduction) =>
		Mathf.Clamp((artistTalent - 0.70f) / 0.30f, 0f, 1f) *
		Mathf.Clamp((labelProduction - 0.70f) / 0.30f, 0f, 1f);

	public static float GetMaximumAchievableCohesion(int year, float artistTalent, float labelProduction, float luckyRoll) {
		// The exogenous curve is the floor and the shape. Album legitimacy -- records that
		// actually happened and were actually heard -- can pull it forward in time by a
		// bounded amount, never rewrite it, and is exactly 1.0x when that phase is off.
		float era = GetCohesionEraTerm(year) * AlbumLegitimacyService.CurrentCeilingMultiplier;
		float excellence = GetStatementExcellence(artistTalent, labelProduction);
		float baseline = Mathf.Clamp(era * (0.45f + 0.50f * artistTalent + 0.25f * labelProduction), 0.08f, 1f);
		// Rubber Soul (1965) and Pet Sounds (1966) preceded the era ramp. That ramp alone
		// cannot reach the 0.72 statement bar until 1967, and the pre-1965 fluke term it
		// replaces could not either -- its ceiling topped out around 0.55 -- so no concept
		// album was reachable anywhere in the decade before 1967, which the artifacts
		// confirm: zero of them across 1960-66. This opens a deliberately vanishing path
		// from 1965 for near-top talent in a near-top room on the best few percent of rolls.
		bool pioneer = year >= EarlyStatementYear && excellence > EarlyStatementExcellence &&
			luckyRoll > EarlyStatementRollThreshold;
		return pioneer ? Mathf.Clamp(Mathf.Max(baseline, EarlyStatementCohesionFloor), 0.08f, 1f) : baseline;
	}

	/// <summary>
	/// How far each family was actually swept up in the LP revolution. Rock and jazz almost
	/// entirely; soul later and only partly, since it stayed a singles business well past
	/// 1969; bubblegum essentially not at all -- it was manufactured studio product aimed at
	/// children and remained so. Applying one uniform era decline to every genre is what put
	/// psychedelic rock and classical on the same compilation rate.
	/// </summary>
	public static float GetAlbumRevolutionSusceptibility(GenreFamily family) => family switch {
		GenreFamily.Rock or GenreFamily.Folk or GenreFamily.Jazz => 0.80f,
		GenreFamily.RhythmAndSoul or GenreFamily.Blues => 0.55f,
		GenreFamily.Country or GenreFamily.Gospel => 0.45f,
		GenreFamily.Pop => 0.12f,
		_ => 0.30f
	};

	/// <summary>
	/// How evenly the material is spread across an album: 0 is a hit with filler around it,
	/// 1 is a record where every side was a considered performance.
	/// <para>
	/// Jazz cut LPs as bodies of work from the start — Giant Steps is 1960 — so it has no
	/// revolution to undergo; it was already there, and is a large part of why the form was
	/// available to be taken seriously later. Pop and rock began as a hit with filler around
	/// it and became albums across the decade. That asymmetry is the point: the mid-sixties
	/// album shift is a ROCK phenomenon, and a genre-flat rule cannot express it.
	/// </para>
	/// <para>
	/// Deliberately expressed as VARIANCE rather than level. Raising jazz track quality
	/// outright would worsen a known calibration miss (the model already over-weights jazz on
	/// the early album chart); narrowing the spread instead raises the body-of-work reading
	/// while slightly LOWERING peak-driven chart appeal, which pushes both numbers the way
	/// they need to go.
	/// </para>
	/// </summary>
	public static float GetTrackConsistency(GenreFamily family, int year) {
		float innate = family switch {
			GenreFamily.Jazz or GenreFamily.Classical => .80f,
			GenreFamily.Blues => .55f,
			GenreFamily.Folk => .50f,
			GenreFamily.Gospel => .45f,
			_ => .20f
		};
		// What the family learned from the revolution, scaled by how far it was carried by it.
		// Pop's susceptibility is .12, so bubblegum stays manufactured product all decade.
		float learned = GetAlbumRevolutionSusceptibility(family) * GetAlbumEraWeight(year);
		return Mathf.Clamp(innate + (1f - innate) * learned, 0f, 1f);
	}

	/// <summary>
	/// Multiplier on album-track quality spread. A tighter spread is more of a record, because
	/// the album cuts sit closer to the best thing on it.
	/// </summary>
	public static float GetTrackSpreadMultiplier(GenreFamily family, int year) =>
		Mathf.Lerp(1f, .25f, GetTrackConsistency(family, year));

	/// <summary>
	/// Probability that an album is assembled from already-released singles plus filler.
	/// Propensity is the catalog's authored SingleOrientation -- a singles-led genre builds
	/// its LPs out of singles -- decayed by the era term above. The model was right about
	/// 1960, where pop and rock LPs genuinely were a hit plus filler; it simply never let
	/// go, holding youth genres at 82-96% through 1969 while the album became the artistic
	/// unit around them, and holding classical there too for want of being on a hardcoded
	/// six-genre adult list.
	/// </summary>
	public static float GetCompilationChance(float singleOrientation, GenreFamily family, int year) {
		float decline = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(
			(year - CompilationDeclineStartYear) / (CompilationDeclineEndYear - CompilationDeclineStartYear), 0f, 1f));
		float retained = 1f - decline * GetAlbumRevolutionSusceptibility(family);
		return Mathf.Clamp(Mathf.Clamp(singleOrientation, 0f, 1f) * retained, 0f, MaximumCompilationChance);
	}

	public static float GetCompilationChance(Genre genre, int year) =>
		GenreCatalog.TryGet(GenreCatalog.MapLegacy(genre, year), out GenreProfile profile)
			? GetCompilationChance(profile.SingleOrientation, profile.Family, year)
			: GetCompilationChance(0.60f, GenreFamily.Pop, year);
}
