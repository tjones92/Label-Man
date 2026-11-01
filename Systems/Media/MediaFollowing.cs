using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>An act's place in the nation's attention (CelebrityFanbaseDesign.md §7.2). Regional favorites stay on the episode model.</summary>
public enum SalienceState { None, NationalSensation, CulturalPhenomenon }

/// <summary>One region's following of an act (§2.1), counted in people.</summary>
public sealed class FollowingRegion {
	/// <summary>Young followers by the year they joined, when they were about 16 (§2.3). Each cohort ages out by about 22.</summary>
	public Dictionary<int, float> Young = new();
	public float Adult;
	public float YoungTotal() { float t = 0f; foreach (float v in Young.Values) t += v; return t; }
}

/// <summary>
/// One act's durable fanbase record (§2), adult regard (§4.1) and national salience (§7). Media-owned. Phase 0 is
/// observe-only: nothing outside media reads it.
/// </summary>
public sealed class MediaFollowingState {
	public string ArtistId, ArtistName;
	public Dictionary<string, FollowingRegion> Regions = new(StringComparer.Ordinal);
	/// <summary>Adult heat by region, [0,1]: the adult audience's converter, as teen attachment is the young one's (§4.1).</summary>
	public Dictionary<string, float> Regard = new(StringComparer.Ordinal);
	/// <summary>Smoothed share of national attention by family: sales, television, press (§7.1).</summary>
	public float[] Family = new float[MediaFollowing.Families];
	/// <summary>Smoothed share of young and of adult attention (sales and television together), for the both-audiences test.</summary>
	public float[] Audience = new float[2];
	/// <summary>
	/// Smoothed share of young attention in the sales and television families. The sensation test reads these and
	/// <see cref="Audience"/>[0] (their weighted sum), not the all-audience families.
	/// </summary>
	public float[] YoungFamily = new float[MediaFollowing.YoungFamilies];
	public SalienceState State;
	public int StateSinceDay = -1;
	/// <summary>Consecutive weeks the phenomenon test has passed (to enter) or failed (to leave).</summary>
	public int PhenomenonWeeks;
	public SalienceState Peak;
	public float PeakSalience, PeakFollowing;
	public int PeakFollowingDay = -1;
	/// <summary>Day index of the act's latest release seen in the market, and of its last week in the national top 40.</summary>
	public int LastReleaseDay = -1, LastTop40Day = -1;
	/// <summary>Quality of the act's recent releases by format (newest last, at most <see cref="MediaFollowing.QualityMemory"/>).</summary>
	public List<float> RecentSingles = new(), RecentAlbums = new();
	/// <summary>Releases already counted for disappointment and would-be advance orders.</summary>
	public HashSet<string> ReleasesSeen = new(StringComparer.Ordinal);
	public int LastActiveDay;

	public float Salience() {
		float s = 0f;
		for (int f = 0; f < MediaFollowing.Families; f++) s += MediaFollowing.FamilyWeight[f] * Family[f];
		return s;
	}
	public float YoungTotal() { float t = 0f; foreach (FollowingRegion r in Regions.Values) t += r.YoungTotal(); return t; }
	public float AdultTotal() { float t = 0f; foreach (FollowingRegion r in Regions.Values) t += r.Adult; return t; }
}

/// <summary>
/// The arithmetic of followings, regard and salience (CelebrityFanbaseDesign.md §2, §4.1, §7), pure so the fixtures can
/// drive it. Phase 0 (<c>--observe-following</c>): runs inside the media world (observe or causal) and writes only media
/// state and telemetry, so every economy CSV must match the control. Inflows are today's buyers (sales, all formats) and
/// television viewers only; press, fan magazines and live rooms join in later phases. All constants are the design's
/// initial values, to be calibrated against its §9 targets. Draws no RNG.
/// </summary>
public static class MediaFollowing {
	public const string ObserveSwitch = "--observe-following";
	private static bool configured, observe;
	public static bool Active => observe && MediaAtmosphere.Observing;

	/// <summary>Per-act rows (monthly, at releases, weekly salience leaders), opened by the audit runner. Print-only.</summary>
	public static System.IO.TextWriter Log;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>())
			if (argument == ObserveSwitch) observe = true;
		configured = true;
	}

	internal static void SetForChecks(bool on) { observe = on; configured = true; }

	// ---- joiners (§2.2) ---------------------------------------------------------------------------------------------

	/// <summary>
	/// At full heat, three buyers in ten become committed fans. Calibration 1 (Phase 0 notes): the design's 0.10 left fan
	/// clubs and advance orders 4-10x short of §9, since a club is only 3% of a following.
	/// </summary>
	public const float CBuy = 0.30f;
	/// <summary>
	/// Registered viewers who become followers. A weekly series converts the same audience a little every week. Calibration
	/// 1: the design's 0.02 let standing-lane television outjoin record buying two to one.
	/// </summary>
	public const float CView = 0.005f;
	/// <summary>Registration (§3.1): the share of a fresh audience that remembers the act afterwards.</summary>
	public const float R0 = 0.25f;

	/// <summary>Heat is the converter: a cold hit leaves few followers, the same units sold in a frenzy leave many.</summary>
	public static float HeatFactor(float heat) => 0.2f + 0.8f * Math.Clamp(heat, 0f, 1f);

	/// <summary>following += joiners × (1 − following / fitPopulation). Never past the people who fit the act.</summary>
	public static float Join(float following, float joiners, double fitPopulation) {
		if (joiners <= 0f || fitPopulation <= 0) return following;
		double room = Math.Max(0.0, 1.0 - following / fitPopulation);
		return (float)Math.Min(fitPopulation, following + joiners * room);
	}

	/// <summary>Share of a record's buyers who are young (teen and young adult): singles skew young, LPs adult (initial values).</summary>
	public static float YoungBuyerShare(Genre genre, ReleaseFormat format) {
		float teen = MediaOutletCatalog.TeenFit(genre);
		return format == ReleaseFormat.Single ? 0.25f + 0.7f * teen : 0.10f + 0.5f * teen;
	}

	/// <summary>The share of an audience who could follow an act of this genre at all (saturation's denominator).</summary>
	public static float AudienceFit(Genre genre, bool young) {
		float teen = MediaOutletCatalog.TeenFit(genre);
		return young ? 0.15f + 0.6f * teen : 0.25f + 0.5f * (1f - teen);
	}

	/// <summary>Registration performance factor (§3.1): a breakdown is still remembered, just less.</summary>
	public static float RegistrationPerformance(MediaPerformanceResult result) => result?.Outcome switch {
		PerformanceOutcome.Breakdown => 0.6f,
		PerformanceOutcome.Weak => 0.8f,
		PerformanceOutcome.Engaging => 1.15f,
		PerformanceOutcome.Electrifying => 1.6f,
		_ => 1f,
	};

	/// <summary>Novelty (§3.1): 1 for the act's first national exposure, falling toward 0.4 with national dates already seen.</summary>
	public static float Novelty(int nationalDatesBefore) => 0.4f + 0.6f * (float)Math.Pow(0.8, Math.Max(0, nationalDatesBefore));

	/// <summary>
	/// Attention (§3.1, §7.3): below 1 when other acts hold the country's attention. Each other act above the sensation
	/// bar takes its excess over that bar, measured in phenomenon bars, from everyone else's registration.
	/// </summary>
	public static float Attention(float othersExcess) => 1f / (1f + Math.Max(0f, othersExcess) / PhenomenonShare);

	// ---- losses (§2.3) ----------------------------------------------------------------------------------------------

	/// <summary>Calibration 1: young 0.15 → 0.22 and adult 0.10 → 0.08, toward half-lives of 2-3 and 5-7 years after the last hit.</summary>
	public const float YoungDriftYear = 0.22f, AdultDriftYear = 0.08f;
	/// <summary>Extra drift once twelve months pass with no release. Adult audiences wait longer (calibration 1: half the young rate).</summary>
	public const float SilenceYoungYear = 0.15f, SilenceAdultYear = 0.075f;
	public const int SilenceDays = 365;
	/// <summary>Young members join at about 13-19 and leave the young pool at about 22: three to nine years after joining.</summary>
	public const float AgeOutStartYears = 3f, AgeOutEndYears = 9f;
	/// <summary>Of the members who age out, half stay on as adult followers.</summary>
	public const float AgeOutToAdult = 0.5f;
	/// <summary>Switching: each other act's salience in a sensation or phenomenon takes that share of young followings a year.</summary>
	public const float SwitchingPerSalienceYear = 1f;
	public const int QualityMemory = 3;
	/// <summary>Releases of a format the followers must have heard before they judge one against them.</summary>
	public const int DisappointmentMemory = 2;
	/// <summary>Quality this far under the act's recent average is a disappointment. Calibration 1: 0.05 → 0.10, since at
	/// 0.05 ordinary variation between an act's records tripped it on a quarter of releases.</summary>
	public const float DisappointmentGap = 0.10f;
	public const float DisappointmentMin = 0.05f, DisappointmentMax = 0.20f;

	public static float Weekly(float annualRate) => 1f - (float)Math.Pow(1.0 - Math.Clamp(annualRate, 0f, 0.999f), 1.0 / 52.0);

	/// <summary>
	/// The share of a young cohort that ages out this week, given the years since it joined (joined mid-year). Join ages are
	/// spread evenly, so survival falls linearly from year 3 to year 9; the weekly hazard is dt / (9 − t).
	/// </summary>
	public static float AgeOutThisWeek(double yearsSinceJoin) {
		const double dt = 1.0 / 52.0;
		if (yearsSinceJoin + dt <= AgeOutStartYears) return 0f;
		double left = AgeOutEndYears - Math.Max(AgeOutStartYears, yearsSinceJoin);
		return left <= dt ? 1f : (float)Math.Clamp(dt / left, 0.0, 1.0);
	}

	/// <summary>
	/// Disappointment (§2.3): a release clearly below the act's recent average of the same format loses 5-20% of the
	/// following at once. None before the act has two records of the format to compare against.
	/// </summary>
	public static float Disappointment(IReadOnlyList<float> recent, float quality) {
		if (recent == null || recent.Count < DisappointmentMemory) return 0f;
		float gap = recent.Average() - quality;
		if (gap <= DisappointmentGap) return 0f;
		return Math.Clamp(DisappointmentMin + (gap - DisappointmentGap), DisappointmentMin, DisappointmentMax);
	}

	// ---- what a following would do at launch (§2.4; Phase 0 telemetry only) -----------------------------------------

	/// <summary>Launch intent per follower over the first three weeks: (young, adult).</summary>
	public static (float Young, float Adult) AdvancePerFollower(ReleaseFormat format) =>
		format == ReleaseFormat.Single ? (0.35f, 0.10f) : (0.12f, 0.30f);

	/// <summary>A manager's fan-club figure is about 3% of the following (§2.4 player view).</summary>
	public const float FanClubShare = 0.03f;

	// ---- regard (§4.1) ----------------------------------------------------------------------------------------------

	/// <summary>Adult audiences warm and cool more slowly than teenagers: a 13-week half-life (initial value).</summary>
	public static readonly float RegardRetention = (float)Math.Pow(0.5, 1.0 / 13.0);
	public const float RegardMaxWeeklyGain = 0.08f;

	public static float RegardDeposit(float regard, float gain) {
		gain = Math.Clamp(gain, 0f, RegardMaxWeeklyGain);
		return Math.Clamp(regard + gain * (1f - regard), 0f, 1f);
	}

	// ---- salience (§7) ----------------------------------------------------------------------------------------------

	/// <summary>Attention families measured in Phase 0: sales, television, press. Live attendance joins with its inflow.</summary>
	public const int Families = 3;
	public const int SalesFamily = 0, TvFamily = 1, PressFamily = 2;
	/// <summary>Families measured by audience: sales and television. Press readers are not split young/adult in Phase 0.</summary>
	public const int YoungFamilies = 2;
	public static readonly float[] FamilyWeight = { 0.5f, 0.3f, 0.2f };
	/// <summary>A leaky integrator with about a six-week half-life.</summary>
	public const float SalienceKeep = 0.89f;
	/// <summary>
	/// A family counts toward breadth where the act holds 3× an average charting act's share. Calibration 1: measured each
	/// week against the acts on the national chart (<see cref="SetBars"/>), not a fixed 1-in-100 slot share. At a fixed
	/// 3% no act reached breadth 2 in 1960-61, because attention spreads over a thousand acts selling.
	/// </summary>
	public const float BreadthLead = 3f;
	public const float DefaultBar = 3f / 100f, MinBar = 0.003f;
	/// <summary>This week's breadth bars: by family, then young and adult audience. Recomputed weekly, never saved.</summary>
	public static readonly float[] FamilyBar = { DefaultBar, DefaultBar, DefaultBar };
	public static readonly float[] AudienceBar = { DefaultBar, DefaultBar };
	public static readonly float[] YoungFamilyBar = { DefaultBar, DefaultBar };

	/// <summary>Sets each bar to 3× the mean smoothed share of the acts on the national chart this week.</summary>
	public static void SetBars(IReadOnlyCollection<MediaFollowingState> charting) {
		if (charting == null || charting.Count == 0) return;
		for (int f = 0; f < Families; f++) FamilyBar[f] = Math.Max(MinBar, BreadthLead * charting.Average(s => s.Family[f]));
		for (int a = 0; a < 2; a++) AudienceBar[a] = Math.Max(MinBar, BreadthLead * charting.Average(s => s.Audience[a]));
		for (int f = 0; f < YoungFamilies; f++) YoungFamilyBar[f] = Math.Max(MinBar, BreadthLead * charting.Average(s => s.YoungFamily[f]));
	}

	internal static void ResetBars() {
		for (int f = 0; f < Families; f++) FamilyBar[f] = DefaultBar;
		for (int a = 0; a < 2; a++) AudienceBar[a] = DefaultBar;
		for (int f = 0; f < YoungFamilies; f++) YoungFamilyBar[f] = DefaultBar;
	}
	/// <summary>Calibration 1: θs 1.5% → 2%. At 1.5% the bar sat at the top of a flat field and 17-32 acts entered in two years.</summary>
	public const float SensationShare = 0.02f, SensationExit = 0.6f;
	/// <summary>
	/// An LP sale counts a third of a single in the sales family of attention. A hit single is heard on every radio and
	/// jukebox; and the model's LP market runs about 3x the historical one (Billboard 1966: about 50M LPs against 173M),
	/// so LP units at par made adult catalog acts the nation's most-watched (calibration 1).
	/// </summary>
	public const float AlbumAttentionWeight = 1f / 3f;
	public const float PhenomenonShare = 0.08f, PhenomenonLead = 3f;
	public const int PhenomenonHoldWeeks = 8;
	/// <summary>A phenomenon needs every measured family plus both audiences.</summary>
	public const int PhenomenonBreadth = Families + 1;

	public static float Smooth(float smoothed, float share) => smoothed * SalienceKeep + share * (1f - SalienceKeep);

	public static int Breadth(MediaFollowingState s) {
		int b = 0;
		for (int f = 0; f < Families; f++) if (s.Family[f] > FamilyBar[f]) b++;
		if (s.Audience[0] > AudienceBar[0] && s.Audience[1] > AudienceBar[1]) b++;
		return b;
	}

	/// <summary>Share of the nation's young attention (sales and television), the sensation's salience.</summary>
	public static float YoungSalience(MediaFollowingState s) => s.Audience[0];

	/// <summary>Breadth among the young: young sales, young television, and press (not split by audience in Phase 0).</summary>
	public static int YoungBreadth(MediaFollowingState s) {
		int b = 0;
		for (int f = 0; f < YoungFamilies; f++) if (s.YoungFamily[f] > YoungFamilyBar[f]) b++;
		if (s.Family[PressFamily] > FamilyBar[PressFamily]) b++;
		return b;
	}

	/// <summary>
	/// The sensation test, all on young attention: young salience ≥ θs, young breadth 2, and a larger share of the young's
	/// attention than of the adults'. Measured on total attention, teen idols topped out near 1.1% (adults are 58% of it)
	/// while adult orchestras entered; and a young bar alone let big adult acts through on volume (screens fb0c3, fb0c4).
	/// </summary>
	public static bool SensationTest(MediaFollowingState s) =>
		YoungSalience(s) >= YoungSensationShare && YoungBreadth(s) >= 2 && s.Audience[0] >= s.Audience[1];

	/// <summary>
	/// θs on young attention. Young attention sits on fewer acts than total attention: at 2% (fb0c5) 23-27 acts entered in
	/// 1960-61 alone, bunched at 2.0-2.6% young share, against 10-25 a decade.
	/// </summary>
	public const float YoungSensationShare = 0.03f;

	/// <summary>
	/// One week's move between the attention states (§7.2). A sensation is the teen state (author's decision, 2026-11-01):
	/// it enters on <see cref="SensationTest"/> and leaves when young salience falls below 0.6 θs. In calibration 1 the
	/// entrants were adult TradPop, EasyListening and Country acts, and the 1960s sensation is a youth craze. Adult acts
	/// reach the top only as a phenomenon, measured on total attention and needing both audiences.
	/// A phenomenon needs θp, a lead of D over the next act and full breadth for eight weeks, and leaves once any test has
	/// failed for eight weeks. Hysteresis replaces the old fixed cooldown. Returns true when the state changed.
	/// </summary>
	public static bool Advance(MediaFollowingState s, float nextBest, int day) {
		float salience = s.Salience();
		int breadth = Breadth(s);
		bool phenomenonTest = salience >= PhenomenonShare && salience >= PhenomenonLead * nextBest && breadth >= PhenomenonBreadth;
		SalienceState old = s.State, next = old;
		if (old == SalienceState.CulturalPhenomenon) {
			s.PhenomenonWeeks = phenomenonTest ? 0 : s.PhenomenonWeeks + 1;
			if (s.PhenomenonWeeks >= PhenomenonHoldWeeks) { next = SalienceState.NationalSensation; s.PhenomenonWeeks = 0; }
		} else {
			s.PhenomenonWeeks = phenomenonTest ? s.PhenomenonWeeks + 1 : 0;
			if (s.PhenomenonWeeks >= PhenomenonHoldWeeks) { next = SalienceState.CulturalPhenomenon; s.PhenomenonWeeks = 0; }
		}
		if (next == SalienceState.None && SensationTest(s)) next = SalienceState.NationalSensation;
		if (next == SalienceState.NationalSensation && old != SalienceState.CulturalPhenomenon && YoungSalience(s) < SensationExit * YoungSensationShare) next = SalienceState.None;
		if (next == old) return false;
		s.State = next;
		s.StateSinceDay = day;
		if (next > s.Peak) s.Peak = next;
		return true;
	}
}
