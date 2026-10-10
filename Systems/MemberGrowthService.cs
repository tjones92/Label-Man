using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase 5 of SimTools/BandMemberSimulationDirective.md (§4.10, §4.11): growth, decline and personality drift.
/// <para>
/// <b>Closed form.</b> Work adds to <see cref="Musician.effectiveHours"/> (additive, so chunking a year can't change
/// the sum); each axis is then RECOMPUTED from state, never accumulated:
/// <c>axis = formation + (ceiling - formation) x (1 - exp(-effectiveHours / H)) - decline(age, road, substance)</c>.
/// There is no gig simulation for AI acts, so a year's hours come from the act's state (§4.10's work proxy): an
/// unsigned working act plays the rooms of its scene most nights (the Hamburg case), a signed act tours on its chart
/// evidence and cuts sessions per release, a held act is idle. Plasticity falls with age, so work moves the young.
/// </para>
/// <para>
/// <b>Shadow first</b> (<c>--shadow-musician-growth</c>): hours accrue and the grown values are computed and logged,
/// but nothing live is written. <b>Live</b> (<c>--enable-musician-growth</c>) writes the axes, re-anchors
/// <see cref="Musician.technicalSkill"/> on the primary axis, drifts personality within +/-.2 of the generated values,
/// and re-derives the act's stats and disposition.
/// </para>
/// </summary>
public static class MemberGrowthService {
	/// <summary>Effective hours for ~63% of a person's headroom. Hamburg was ~3,000 stage hours over two years.</summary>
	public const float HoursScale = 3000f;
	public const float ClubRoomHours = 900f, OtherRoomHours = 450f, RoadHoursAtFullLoad = 900f, SessionHoursPerRelease = 40f;
	public const float RehearsalHours = 150f;
	public const float KindResidency = 1.0f, KindRoad = 0.65f, KindSession = 0.45f, KindRehearsal = 0.35f;

	// Decline (§4.10): voices fade after the mid-thirties, faster for road-worn ones; hands after fifty; creativity
	// peaks around thirty.
	public const float VocalDeclinePerYear = 0.006f, VocalDeclineStartAge = 34f;
	public const float InstrumentalDeclinePerYear = 0.004f, InstrumentalDeclineStartAge = 50f;
	public const float CreativityDeclinePerYear = 0.004f, CreativityDeclineStartAge = 32f, CreativityDeclineCap = 0.06f;

	// Personality drift (§4.11): bounded, annual, from state.
	public const float DriftBound = 0.20f, DriftRate = 0.25f;
	public const float EgoFromFame = 0.60f, LoyaltyPerTenureYear = 0.01f, TemperamentPerRoadYear = 0.02f, ReliabilityPerSubstance = 0.30f;
	/// <summary>A trait move past this re-derives the act's disposition.</summary>
	public const float DispositionStep = 0.05f;

	public sealed class GrowthRow {
		public float hours, weightedHours, technicalNow, technicalGrown, vocalPowerGrown, instrumentalGrown, creativityGrown;
		internal float rawTechnicalShift, rawCreativityShift; // grown minus formation, before the population centring
	}

	// ---- population centring (sized on bms5-obs-1001, §14) -------------------------------------------------
	// Growth makes the young better than the field; it must not make the field better than it was. Unchecked, the
	// shadow's mean grown-minus-live skill rose +.0055 then +.011 a year (act base quality +.003, +.006). So each
	// pass takes the population's mean shift from formation and every live value is written relative to it: who
	// grew is unchanged, the average is held. Live writes wait for the end of the pass so the centre is this
	// year's, not last year's.
	private static float technicalCentre, creativityCentre;
	private static double passTechnicalShift, passCreativityShift;
	private static int passCount;
	private static readonly List<(SimulatedArtist Act, Musician Person, GrowthRow Row, int Year)> pendingWrites = new();

	public static float TechnicalCentre => technicalCentre;
	public static float CreativityCentre => creativityCentre;

	public static void RestoreCentres(float technical, float creativity) { technicalCentre = technical; creativityCentre = creativity; }

	internal static void ResetForProbe() { technicalCentre = creativityCentre = 0f; passTechnicalShift = passCreativityShift = 0; passCount = 0; pendingWrites.Clear(); }

	/// <summary>End of the band-life pass: fix this year's centres, then apply every live write held during the pass.</summary>
	public static void EndPass() {
		if (passCount > 0) {
			technicalCentre = (float)(passTechnicalShift / passCount);
			creativityCentre = (float)(passCreativityShift / passCount);
		}
		passTechnicalShift = passCreativityShift = 0; passCount = 0;
		var touched = new HashSet<SimulatedArtist>();
		foreach (var (act, person, row, year) in pendingWrites) {
			row.technicalGrown = Mathf.Clamp(person.formationTechnical + row.rawTechnicalShift - technicalCentre, 0f, 1f);
			row.creativityGrown = Mathf.Clamp(person.formationCreativity + row.rawCreativityShift - creativityCentre, 0f, 1f);
			bool moved = Write(person, row);
			moved |= Drift(person, act, year);
			if (moved) touched.Add(act);
		}
		pendingWrites.Clear();
		foreach (SimulatedArtist act in touched) {
			act.RecalculateStats();
			if (act.evolution != null) ArtistEvolutionService.DeriveDisposition(act, act.evolution);
		}
	}

	/// <summary>One act's year, after the band-life life events (only people still present work). Returns per-person
	/// rows for telemetry when a growth mode is on; null otherwise.</summary>
	public static Dictionary<Musician, GrowthRow> OnActYear(SimulatedArtist a, List<Musician> present, float roadLoad,
		int releasesNow, int year) {
		if (!BandLife.GrowthShadow || a == null || present == null || present.Count == 0) return null;
		bool live = BandLife.GrowthEnabled;
		var rows = new Dictionary<Musician, GrowthRow>();
		(float hours, float weighted) = Hours(a, roadLoad, releasesNow);
		foreach (Musician m in present) {
			if (m.axesVersion == 0) MemberAxesService.EnsureAxes(m, a, year);
			if (m.axesVersion == 0) continue;
			BaselineCreativity(m, year);
			bool works = m.lifeState == MemberLifeState.Active || (m.lifeState == MemberLifeState.StudioOnly);
			// An act with no allowance (unsigned, not seeking a deal) still plays the rooms: credit what it played.
			float memberHours = hours, memberWeighted = weighted;
			if (hours <= 0f && LocalScenes.RoomGrowthCredit) {
				memberHours = LocalSceneRoomService.Realized(a.artistId, m.personId, year).StageHours;
				memberWeighted = memberHours * KindResidency;
			}
            if (m.lifeState == MemberLifeState.Active) LocalSceneRoomService.AttributeBudget(a.artistId, m.personId, year, memberHours);
			float personHours = m.lifeState == MemberLifeState.StudioOnly ? SessionHoursPerRelease * releasesNow * KindSession : memberWeighted;
			if (works) m.effectiveHours += personHours * m.developmentRate * Plasticity(m.GetAge(year));
			GrowthRow row = Grown(m, year);
			row.hours = works ? memberHours : 0f;
			row.weightedHours = works ? personHours : 0f;
			rows[m] = row;
			passTechnicalShift += row.rawTechnicalShift;
			passCreativityShift += row.rawCreativityShift;
			passCount++;
			if (live) pendingWrites.Add((a, m, row, year));
		}
		return rows;
	}

	/// <summary>The year's paid live hours (road for a signed act, room work for one seeking a deal): Hours less the
	/// sessions and rehearsal, which pay nothing at the door.</summary>
	public static float LiveHours(SimulatedArtist a, float roadLoad) =>
		!string.IsNullOrEmpty(a.labelId) ? RoadHoursAtFullLoad * roadLoad
		: a.prospectMarketStatus == ProspectMarketStatus.Seeking ? (BandLifeService.IsClubFamilyGenre(a.primaryGenre) ? ClubRoomHours : OtherRoomHours)
		: 0f;

	/// <summary>The year's raw and kind-weighted hours from the act's state.</summary>
	public static (float Hours, float Weighted) Hours(SimulatedArtist a, float roadLoad, int releasesNow) {
		float sessions = SessionHoursPerRelease * Mathf.Min(releasesNow, 6);
		if (!string.IsNullOrEmpty(a.labelId)) {
			float road = RoadHoursAtFullLoad * roadLoad;
			return (road + sessions + RehearsalHours, road * KindRoad + sessions * KindSession + RehearsalHours * KindRehearsal);
		}
		if (a.prospectMarketStatus == ProspectMarketStatus.Seeking) {
			float room = BandLifeService.IsClubFamilyGenre(a.primaryGenre) ? ClubRoomHours : OtherRoomHours;
			return (room + RehearsalHours, room * KindResidency + RehearsalHours * KindRehearsal);
		}
		return (0f, 0f);
	}

	/// <summary>How readily work becomes skill at an age: full until 20, falling through the twenties and early thirties.</summary>
	public static float Plasticity(int age) => age <= 20 ? 1f : age <= 26 ? 1f - 0.25f * (age - 20) / 6f :
		age <= 35 ? 0.75f - 0.45f * (age - 26) / 9f : age <= 50 ? 0.30f - 0.20f * (age - 35) / 15f : 0.05f;

	/// <summary>The growth origin: today's values are the formation values, and decline counts only the years after
	/// this one (a 45-year-old singer's voice already sounds 45; switching growth on must not age it again).</summary>
	private static void BaselineCreativity(Musician m, int year) {
		if (m.growthOriginYear == 0) m.growthOriginYear = year;
		if (m.formationCreativity > 0f || m.creativity <= 0f) return;
		m.formationCreativity = m.creativity;
	}

	/// <summary>Years of decline past a start age, counted from the growth origin rather than from birth.</summary>
	private static float DeclineYears(Musician m, int year, float startAge) {
		int origin = m.growthOriginYear > 0 ? m.growthOriginYear : year;
		float now = Mathf.Max(0f, m.GetAge(year) - startAge);
		float then = Mathf.Max(0f, m.GetAge(origin) - startAge);
		return now - then;
	}

	/// <summary>The grown values for a person now, computed from state. Writes nothing.</summary>
	public static GrowthRow Grown(Musician m, int year) {
		float g = 1f - Mathf.Exp(-m.effectiveHours / HoursScale);
		float inst = m.formationInstrumental + Mathf.Max(0f, m.ceilingInstrumental - m.formationInstrumental) * g
			- InstrumentalDeclinePerYear * DeclineYears(m, year, InstrumentalDeclineStartAge);
		float vocalDecline = VocalDeclinePerYear * DeclineYears(m, year, VocalDeclineStartAge) * (1f + m.roadYears / 8f);
		float vp = m.formationVocalPower + Mathf.Max(0f, m.ceilingVocal - m.formationVocalPower) * g - vocalDecline;
		float vc = m.formationVocalControl + Mathf.Max(0f, m.ceilingVocal - m.formationVocalControl) * g - 0.5f * vocalDecline;
		inst = Mathf.Clamp(inst, 0f, 1f); vp = Mathf.Clamp(vp, 0f, 1f); vc = Mathf.Clamp(vc, 0f, 1f);
		bool singer = MemberAxesService.IsSinger(m);
		float primaryNow = singer ? 0.5f * (vp + vc) : inst;
		float primaryFormed = singer ? 0.5f * (m.formationVocalPower + m.formationVocalControl) : m.formationInstrumental;
		float techShift = primaryNow - primaryFormed;
		float creativityShift = -Mathf.Min(CreativityDeclineCap, CreativityDeclinePerYear * DeclineYears(m, year, CreativityDeclineStartAge));
		float creativityBase = m.formationCreativity > 0f ? m.formationCreativity : m.creativity;
		// The shadow reports against last pass's centre; a live write is re-centred at the end of the pass.
		return new GrowthRow { technicalNow = m.technicalSkill, vocalPowerGrown = vp, instrumentalGrown = inst,
			technicalGrown = Mathf.Clamp(m.formationTechnical + techShift - technicalCentre, 0f, 1f),
			creativityGrown = Mathf.Clamp(creativityBase + creativityShift - creativityCentre, 0f, 1f),
			rawTechnicalShift = techShift, rawCreativityShift = creativityShift };
	}

	private static bool Write(Musician m, GrowthRow row) {
		bool moved = Math.Abs(m.technicalSkill - row.technicalGrown) > 0.0005f || Math.Abs(m.creativity - row.creativityGrown) > 0.0005f;
		m.instrumentalSkill = row.instrumentalGrown;
		m.vocalPower = row.vocalPowerGrown;
		m.technicalSkill = row.technicalGrown;
		m.creativity = row.creativityGrown;
		return moved;
	}

	/// <summary>Personality drift (§4.11): each trait moves a quarter of the way toward a target set by the person's
	/// state, and never further than .2 from what they were generated with. Returns whether any trait crossed a step.</summary>
	public static bool Drift(Musician m, SimulatedArtist a, int year) {
		if (!m.traitsBaselined) {
			m.traitsBaselined = true;
			m.generatedEgo = m.ego; m.generatedLoyalty = m.loyalty; m.generatedTemperament = m.temperament; m.generatedReliability = m.reliability;
		}
		int tenure = Mathf.Max(0, year - m.joinedYear);
		bool stepped = false;
		m.ego = Move(m.ego, m.generatedEgo, m.generatedEgo + EgoFromFame * m.personalRecognition, ref stepped);
		m.loyalty = Move(m.loyalty, m.generatedLoyalty, m.generatedLoyalty + LoyaltyPerTenureYear * tenure, ref stepped);
		m.temperament = Move(m.temperament, m.generatedTemperament, m.generatedTemperament - TemperamentPerRoadYear * m.roadYears, ref stepped);
		m.reliability = Move(m.reliability, m.generatedReliability, m.generatedReliability - ReliabilityPerSubstance * m.substanceLoad, ref stepped);
		return stepped;
	}

	private static float Move(float now, float generated, float target, ref bool stepped) {
		float bounded = Mathf.Clamp(target, generated - DriftBound, generated + DriftBound);
		float next = Mathf.Clamp(now + (bounded - now) * DriftRate, 0f, 1f);
		if (Mathf.Floor(next / DispositionStep) != Mathf.Floor(now / DispositionStep)) stepped = true;
		return next;
	}
}
