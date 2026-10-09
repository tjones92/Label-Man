using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>Who the lineup-churn implementation is allowed to act on. There is ONE implementation; this
/// is its scope switch (SimTools/BandMemberSimulationDirective.md §6).</summary>
public enum LineupChurnScope {
	/// <summary>No lineup ever changes. Observation (if on) still measures what would have happened.</summary>
	Off,
	/// <summary>Phase 3 development checkpoint: only the player's own acts lose and gain members.</summary>
	Roster,
	/// <summary>Phase 4: every act in the world. Changes AI outcomes; ships only behind a two-seed decade A/B.</summary>
	World,
}

/// <summary>
/// Feature boundary for the band-member simulation (SimTools/BandMemberSimulationDirective.md). Copies the
/// <see cref="ArtistEvolution"/> / <see cref="ArtistRecognition"/> pattern: every phase has its own switch,
/// observation is separable from writing, and a run with everything off does no new work and draws nothing.
/// <para>
/// Phases and their switches:
/// <list type="bullet">
/// <item>Phase 1 <c>--enable-cowriting</c>: writing teams on artist originals. Credits only -- song traits
/// are untouched, so the economy is byte-identical.</item>
/// <item>Phase 1b <c>--enable-member-axes</c>: split skill axes, ceilings, sight-reading. Keyed and anchored
/// on the stored technicalSkill; nothing existing reads them.</item>
/// <item>Phase 2 <c>--observe-band-life</c>: strain, morale, life events and would-be departures across the
/// world, written only to new state and to SimLogs.</item>
/// <item>Phase 3/4 <c>--enable-lineup-churn=roster|world</c>: the one churn implementation and its scope.</item>
/// <item>Phase 4c <c>--enable-team-writing</c>, Phase 5 <c>--enable-musician-growth</c>, Phase 5b
/// <c>--polar-member-axes</c>, Phase 7 <c>--enable-member-identity</c>: world changes, each behind its own
/// decade A/B.</item>
/// </list>
/// </para>
/// </summary>
public static class BandLife {
	private static bool configured;
	private static bool cowriting;
	private static bool memberAxes;
	private static bool observe;
	private static LineupChurnScope churnScope = LineupChurnScope.Off;
	private static bool teamWriting;
	private static bool musicianGrowth;
	private static bool growthShadow;
	private static bool polarMemberAxes;
	private static bool memberIdentity;
	private static bool wealthObserve;
	private static bool wealthReaders;
	private static bool roadFatigue;
	private static ulong? interactiveSeed;

	/// <summary>Phase 1: artist originals are credited to writing teams rather than to the first writer in list order.</summary>
	public static bool CowritingEnabled => cowriting;
	/// <summary>Phase 1b: every member carries split skill axes, ceilings and a sight-reading score.</summary>
	public static bool MemberAxesEnabled => memberAxes;
	/// <summary>The annual band-life pass runs over the whole world, writing only new state and telemetry.</summary>
	public static bool ObservingWorld => observe || churnScope == LineupChurnScope.World;
	/// <summary>The annual pass runs at all (for the world, or at least for the player's roster).</summary>
	public static bool AnnualPassActive => ObservingWorld || churnScope != LineupChurnScope.Off;
	public static LineupChurnScope ChurnScope => churnScope;
	/// <summary>Lineups on the player's roster actually change.</summary>
	public static bool RosterChurn => churnScope != LineupChurnScope.Off;
	/// <summary>Lineups everywhere actually change (Phase 4).</summary>
	public static bool WorldChurn => churnScope == LineupChurnScope.World;
	/// <summary>Phase 4c: team craft, professional writing teams and AI cut-ins move song traits.</summary>
	public static bool TeamWritingEnabled => teamWriting;
	/// <summary>Phase 5: grown skill writes the live fields. Implies the shadow computation.</summary>
	public static bool GrowthEnabled => musicianGrowth;
	/// <summary>Phase 5 shadow pass: grown skill is computed beside the live fields and reported, never written.</summary>
	public static bool GrowthShadow => growthShadow || musicianGrowth;
	/// <summary>Phase 5b: PolarActProfileDeriver reads the split member axes.</summary>
	public static bool PolarMemberAxes => polarMemberAxes && memberAxes;
	/// <summary>Phase 7: identity lives on people.</summary>
	public static bool MemberIdentityEnabled => memberIdentity;
	/// <summary>Phase 4e: the wealth stock is kept (observe, or with its readers).</summary>
	public static bool MemberWealthActive => (wealthObserve || wealthReaders) && AnnualPassActive;
	/// <summary>Phase 4e: the wealth readers (independence, money grievance, lasting fame, substance exposure) are live.</summary>
	public static bool MemberWealthReaders => wealthReaders && AnnualPassActive;
	/// <summary>Phase 4f: the exhaustion hazard reads the recovering fatigue level instead of lifetime road years.</summary>
	public static bool RoadFatigue => roadFatigue;

	/// <summary>
	/// The world seed every keyed band-life draw includes (directive §2.18). Ids come from counters, so a
	/// seedless key would give person <c>mus_000123</c> the same draw in every world and correlate the two
	/// A/B seeds on exactly these mechanics. Audit runs pass <c>--seed</c>; an interactive game has no
	/// requested seed, so it gets one of its own -- taken from the wall clock, never from the global
	/// <c>GD</c> stream -- and carries it in the save so a reload continues the same lives.
	/// </summary>
	public static ulong WorldSeed => SimulationSeedBootstrap.RequestedSeed ?? (interactiveSeed ??= DeriveInteractiveSeed());

	private static ulong DeriveInteractiveSeed() {
		unchecked {
			ulong h = (ulong)DateTime.UtcNow.Ticks ^ 0x62616e646c696665UL; // "bandlife"
			h ^= h >> 31; h *= 0xBF58476D1CE4E5B9UL; h ^= h >> 29;
			return h;
		}
	}

	/// <summary>Save/load: a restored world keeps the seed its people were drawn under.</summary>
	public static void RestoreWorldSeed(ulong? seed) {
		if (SimulationSeedBootstrap.RequestedSeed.HasValue) return;
		if (seed.HasValue) interactiveSeed = seed.Value;
	}

	/// <summary>A uniform [0,1) draw keyed on the world seed and a caller key. Touches no RNG stream.</summary>
	public static float Unit(string key) =>
		RepertoireTaxonomy.Unit(WorldSeed.ToString(CultureInfo.InvariantCulture) + "|band-life|" + key);

	/// <summary>An approximately standard-normal keyed draw (Irwin-Hall over four keyed uniforms).</summary>
	public static float Normal(string key) {
		float sum = Unit(key + "#0") + Unit(key + "#1") + Unit(key + "#2") + Unit(key + "#3");
		return (sum - 2f) * 1.7320508f; // var of 4 U(0,1) = 1/3 -> sd = 0.577; scale to unit sd
	}

	/// <summary>Keyed Bernoulli trial.</summary>
	public static bool Chance(string key, float probability) => probability > 0f && Unit(key) < probability;

	/// <summary>The world phases' shipped defaults (scene exports). Each has an --enable-X and a --disable-X flag.</summary>
	public struct WorldPhaseDefaults {
		public bool Wealth, RoadFatigue, TeamWriting, Growth, PolarMemberAxes, Identity;
	}

	public static void Configure(bool cowritingDefault, bool memberAxesDefault, LineupChurnScope churnDefault,
		IEnumerable<string> arguments, WorldPhaseDefaults phases = default) {
		if (configured) return;
		bool enableCowriting = false, disableCowriting = false, enableAxes = false, disableAxes = false;
		bool disableChurn = false, disableWealth = false, disableFatigue = false;
		bool disableTeam = false, disableGrowth = false, disablePolarAxes = false, disableIdentity = false;
		bool explicitAxesReader = false, explicitTeam = false;
		LineupChurnScope? requestedScope = null;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			switch (argument) {
				case "--enable-cowriting": enableCowriting = true; break;
				case "--disable-cowriting": disableCowriting = true; break;
				case "--enable-member-axes": enableAxes = true; break;
				case "--disable-member-axes": disableAxes = true; break;
				case "--observe-band-life": observe = true; break;
				case "--disable-lineup-churn": disableChurn = true; break;
				case "--enable-lineup-churn": requestedScope = LineupChurnScope.World; break;
				case "--enable-lineup-churn=roster": requestedScope = LineupChurnScope.Roster; break;
				case "--enable-lineup-churn=world": requestedScope = LineupChurnScope.World; break;
				case "--enable-team-writing": teamWriting = true; explicitTeam = true; break;
				case "--enable-musician-growth": musicianGrowth = true; explicitAxesReader = true; break;
				case "--shadow-musician-growth": growthShadow = true; explicitAxesReader = true; break;
				case "--polar-member-axes": polarMemberAxes = true; explicitAxesReader = true; break;
				case "--enable-member-identity": memberIdentity = true; break;
				case "--observe-member-wealth": wealthObserve = true; break;
				case "--enable-member-wealth": wealthReaders = true; break;
				case "--disable-member-wealth": disableWealth = true; break;
				case "--enable-road-fatigue": roadFatigue = true; break;
				case "--disable-road-fatigue": disableFatigue = true; break;
				case "--disable-team-writing": disableTeam = true; break;
				case "--disable-musician-growth": disableGrowth = true; break;
				case "--disable-polar-member-axes": disablePolarAxes = true; break;
				case "--disable-member-identity": disableIdentity = true; break;
			}
		}
		// Shipped defaults fill in whatever the command line didn't ask for or turn off.
		if (!disableWealth && phases.Wealth) wealthReaders = true;
		if (!disableFatigue && phases.RoadFatigue) roadFatigue = true;
		if (!disableTeam && phases.TeamWriting) teamWriting = true;
		if (!disableGrowth && phases.Growth) musicianGrowth = true;
		if (!disablePolarAxes && phases.PolarMemberAxes) polarMemberAxes = true;
		if (!disableIdentity && phases.Identity) memberIdentity = true;
		if (disableTeam) teamWriting = false;
		if (disableGrowth) { musicianGrowth = false; growthShadow = false; }
		if (disablePolarAxes) polarMemberAxes = false;
		if (disableIdentity) memberIdentity = false;
		if (enableCowriting && disableCowriting)
			throw new ArgumentException("--enable-cowriting and --disable-cowriting cannot be used together.");
		if (enableAxes && disableAxes)
			throw new ArgumentException("--enable-member-axes and --disable-member-axes cannot be used together.");
		if (requestedScope.HasValue && disableChurn)
			throw new ArgumentException("--enable-lineup-churn and --disable-lineup-churn cannot be used together.");
		cowriting = enableCowriting || (!disableCowriting && cowritingDefault);
		memberAxes = enableAxes || (!disableAxes && memberAxesDefault);
		churnScope = disableChurn ? LineupChurnScope.Off : requestedScope ?? churnDefault;
		// The annual pass hangs off the population lifecycle's year rollover; without the lifecycle there is
		// no year boundary to run on, and the legacy replay must stay untouched by construction.
		if ((observe || churnScope != LineupChurnScope.Off) && !ArtistPopulationLifecycle.Enabled) {
			if (requestedScope.HasValue || observe)
				throw new ArgumentException("Band-life observation and lineup churn require the artist population lifecycle.");
			churnScope = LineupChurnScope.Off;
		}
		// Growth, team craft and identity read the split axes and the writing teams they build on. A shipped default
		// that lost its prerequisite to a --disable flag quietly stands down; an explicit request still throws.
		if (!memberAxes && !explicitAxesReader) { musicianGrowth = false; growthShadow = false; polarMemberAxes = false; }
		if (!cowriting && !explicitTeam) teamWriting = false;
		if ((musicianGrowth || growthShadow || polarMemberAxes) && !memberAxes)
			throw new ArgumentException("Musician growth and --polar-member-axes require member axes (--enable-member-axes).");
		if (disableWealth) { wealthObserve = false; wealthReaders = false; }
		if (disableFatigue) roadFatigue = false;
		if (teamWriting && !cowriting)
			throw new ArgumentException("--enable-team-writing requires --enable-cowriting.");
		configured = true;
	}

	internal readonly struct Switches {
		public readonly bool Cowriting, MemberAxes, Observe, TeamWriting, Growth, GrowthShadow, PolarAxes, Identity;
		public readonly bool WealthObserve, WealthReaders, RoadFatigue;
		public readonly LineupChurnScope Scope;
		public Switches(bool cowriting, bool memberAxes, bool observe, LineupChurnScope scope, bool teamWriting,
			bool growth, bool growthShadow, bool polarAxes, bool identity, bool wealthObserve = false, bool wealthReaders = false,
			bool roadFatigue = false) {
			Cowriting = cowriting; MemberAxes = memberAxes; Observe = observe; Scope = scope; TeamWriting = teamWriting;
			Growth = growth; GrowthShadow = growthShadow; PolarAxes = polarAxes; Identity = identity;
			WealthObserve = wealthObserve; WealthReaders = wealthReaders; RoadFatigue = roadFatigue;
		}
	}

	internal static Switches CaptureSwitches() => new(cowriting, memberAxes, observe, churnScope, teamWriting,
		musicianGrowth, growthShadow, polarMemberAxes, memberIdentity, wealthObserve, wealthReaders, roadFatigue);

	internal static void RestoreSwitches(Switches s) {
		configured = true;
		cowriting = s.Cowriting; memberAxes = s.MemberAxes; observe = s.Observe; churnScope = s.Scope;
		teamWriting = s.TeamWriting; musicianGrowth = s.Growth; growthShadow = s.GrowthShadow;
		polarMemberAxes = s.PolarAxes; memberIdentity = s.Identity;
		wealthObserve = s.WealthObserve; wealthReaders = s.WealthReaders; roadFatigue = s.RoadFatigue;
	}

	internal static void ConfigureForProbe(bool cowritingOn = false, bool axesOn = false, bool observeOn = false,
		LineupChurnScope scope = LineupChurnScope.Off) {
		configured = true;
		cowriting = cowritingOn; memberAxes = axesOn; observe = observeOn; churnScope = scope;
		teamWriting = false; musicianGrowth = false; growthShadow = false; polarMemberAxes = false; memberIdentity = false;
	}
}
