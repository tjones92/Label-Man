using System;
using System.Collections.Generic;

/// <summary>
/// Artist-evolution feature boundary and tunables. Deliberately independent of the
/// Genre Market V2 and population-lifecycle switches so a run with evolution off
/// does no new work, makes no allocation, and draws nothing.
/// <para>
/// Observation is separable from ratification on purpose. The counterfactual --
/// how often WOULD an act have converted, and to what -- is the measurement that
/// sizes the channel before it is allowed to carry anything.
/// </para>
/// </summary>
public static class ArtistEvolution {
	private static bool configured;
	private static bool enabled;
	private static bool observeOnly;
	private static bool pressureEnabled;
	private static bool albumLegitimacyEnabled;
	private static bool culturalMemoryEnabled;
	private static bool adjacencyIdentityFit;

	/// <summary>Ratification may write identity.</summary>
	public static bool Enabled => enabled;
	/// <summary>Evolution profiles exist and observation rows are emitted. Superset of Enabled.</summary>
	public static bool Observing => enabled || observeOnly;
	/// <summary>
	/// Ratification is meaningful only where the project-genre selection actually
	/// runs: the enabled genre market with a live chart. The legacy replay never
	/// reaches it, which keeps the old stream untouched by construction.
	/// </summary>
	public static bool IsLive => enabled && ArtistPopulationLifecycle.IsLive;
	public static bool IsObservingLive => Observing && ArtistPopulationLifecycle.IsLive;

	/// <summary>
	/// Phase 2. Pressure biases WHETHER an act drifts; it never gets a say in where supply
	/// goes. Separately flagged because each phase is independently gated and independently
	/// revertible, and because the neutral setting must reproduce Phase 1 exactly.
	/// </summary>
	public static bool PressureEnabled => enabled && pressureEnabled;
	/// <summary>Phase 4. The phase that can break the economy, so it carries its own switch.</summary>
	public static bool AlbumLegitimacyEnabled => enabled && albumLegitimacyEnabled;
	/// <summary>
	/// Phase 5. The shared industry memory that lets acts and labels hear each other's
	/// records. Rides on the legitimacy switch because the landmark path is what fills the
	/// ledger, but carries its own flag so the propagation half can be reverted without
	/// taking the cohesion ceiling with it.
	/// </summary>
	public static bool CulturalMemoryEnabled => AlbumLegitimacyEnabled && culturalMemoryEnabled;
	/// <summary>
	/// Deliberately NOT gated on <see cref="Enabled"/>. This is a change to the calibrated
	/// supply weight, not a part of the evolution feature, and it has to be A/B-able on its
	/// own against a baseline with evolution off. It also depends on the adjacency edge fill
	/// landing first -- see GenreSupplyService's note.
	/// </summary>
	public static bool AdjacencyIdentityFit => adjacencyIdentityFit;

	// A distinct namespace from ArtistManager's "artistpo" population stream. Evolution
	// is deterministic by design and should need no draw at all; this exists so that a
	// future tie-break has somewhere to go that is not the global stream.
	public const ulong RngNamespace = 0x61727465766f6c75UL; // "artevolu"

	// ---- ratification rule ---------------------------------------------------------------------
	/// <summary>Projects held in the drift window. Fixed capacity; never a growing list.</summary>
	public const int DriftWindow = 4;
	/// <summary>
	/// Strict majority of the window, retained as the ceiling case of the coherence vote
	/// below (an exact repeat scores 1.0 against itself). It is no longer the rule.
	/// </summary>
	public const int DriftMajority = 3;

	// MEASURED, and the reason the majority rule was replaced. Against mix8's 59,231 real
	// selections:
	//   * the median artist releases TWO projects in the whole decade (mean 2.63), so a
	//     window of four is a window most careers never fill;
	//   * a single non-identity genre holds 3 of 4 in 0.13% of windows, and 99.96% of
	//     artists never once reach it. Run live, the rule converted 8 acts in ten years.
	// The bar was asking for a repeat that this population structurally cannot produce.
	//
	// What acts DO produce is scattered drift: a folk act cuts a contemporary-folk side,
	// then a folk-rock side, then a singer-songwriter side. No genre repeats, and the act
	// has plainly moved. So the evidence is COHERENCE rather than repetition -- do the
	// recent off-identity sides all point the same way, and is that way reachable from
	// where the act stands? An exact repeat remains the strongest possible evidence under
	// this rule; it is simply no longer the only admissible kind.
	/// <summary>Projects that must be on record before drift can be read at all.</summary>
	public const int DriftEvidenceWindow = 3;
	/// <summary>Off-identity sides required inside that window. One side is a detour, not a move.</summary>
	public const int DriftOffIdentityMinimum = 2;
	/// <summary>
	/// Mean adjacency from the off-identity sides to the destination they elect.
	/// <para>
	/// Sized against mix8's real selections, and .50 is a cliff rather than a slope: at
	/// exactly the authored two-side minimum the destination's own vote is 1.0, so any bar
	/// at or below .50 is satisfied by the candidate alone and the OTHER side is never
	/// examined. That admits 3.72% of artists on evidence that is really one record. Just
	/// above the cliff the second side has to be genuinely related, which admits 1.19%
	/// across a decade -- ~27 conversions a year against a budget of ~135, so the circuit
	/// breaker stays a circuit breaker rather than becoming the design.
	/// </para>
	/// </summary>
	public const float DriftCoherenceBar = .55f;
	/// <summary>
	/// Musical path requirement. .12f is exactly GenreMarketMomentumService's same-family
	/// floor, so ratification admits an explicit authored edge or a same-family move and
	/// nothing else. If a historically real path has no edge, add the edge -- deliberately,
	/// in its own commit -- rather than lowering this.
	/// </summary>
	public const float AdjacencyFloor = .12f;
	/// <summary>Careers, not weathervanes.</summary>
	public const int IdentityChangeCooldownYears = 2;

	// ---- guardrails ----------------------------------------------------------------------------
	/// <summary>
	/// Migration is self-reinforcing: retention is computed from the identity genre's own
	/// baseline curve, so converting onto a rising genre raises the odds of staying there.
	/// An uncapped rule can cascade a genre pool in two years. This is the circuit breaker,
	/// not the design.
	/// </summary>
	public const float AnnualConversionBudgetShare = .03f;
	/// <summary>A scene thins; it does not evaporate. Share of a genre's identity population per year.</summary>
	public const float AnnualGenreOutflowCap = .15f;

	// ---- pressure bounds (Phase 2) --------------------------------------------------------------
	// The current GetIdentityFit constants become the NEUTRAL case, reproduced exactly when
	// evolution is disabled or the artist is unpressured. Under pressure the primary anchor
	// softens and adjacent candidates lift -- never below today's "other" floor, never above
	// today's primary. This is a restlessness term, not a second genre picker.
	public const float IdentityFitPrimaryNeutral = 4f;
	public const float IdentityFitPrimaryRestless = 2.6f;
	public const float IdentityFitAdjacentNeutral = 1.45f;
	public const float IdentityFitAdjacentRestless = 2.1f;
	/// <summary>
	/// Under real pressure an act may ratify on a shorter but UNANIMOUS window -- three
	/// folk-rock sides and nothing else. Weak evidence never qualifies: the shortened
	/// window still requires every project in it to agree.
	/// </summary>
	public const int PressuredWindowMinimum = 3;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false, observe = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-artist-evolution") enable = true;
			if (argument == "--disable-artist-evolution") disable = true;
			if (argument == "--observe-artist-evolution") observe = true;
			if (argument == "--enable-evolution-pressure") pressureEnabled = true;
			if (argument == "--enable-album-legitimacy") albumLegitimacyEnabled = true;
			if (argument == "--enable-cultural-memory") culturalMemoryEnabled = true;
			if (argument == "--enable-adjacency-identity-fit") adjacencyIdentityFit = true;
		}
		if (enable && disable)
			throw new ArgumentException("--enable-artist-evolution and --disable-artist-evolution cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		if (enabled && !ArtistPopulationLifecycle.Enabled)
			throw new ArgumentException("Artist evolution requires the artist population lifecycle to be enabled.");
		// Observation is a diagnostic overlay on the disabled path and is meaningless once
		// ratification is live, where every observation is already an outcome.
		observeOnly = !enabled && observe;
		if (observeOnly && !ArtistPopulationLifecycle.Enabled)
			throw new ArgumentException("--observe-artist-evolution requires the artist population lifecycle to be enabled.");
		configured = true;
	}

	/// <summary>
	/// The raw switch positions, so a probe suite can put every one of them back. Restoring
	/// only <c>enabled</c> and <c>observeOnly</c> silently switched the phase flags OFF for
	/// the remainder of any run that also passed --artist-evolution-probes, which reads in
	/// the telemetry as every pressure being exactly zero rather than as an error.
	/// </summary>
	internal readonly struct Switches {
		public readonly bool Enabled, ObserveOnly, Pressure, AlbumLegitimacy, CulturalMemory, AdjacencyFit;
		public Switches(bool enabled, bool observeOnly, bool pressure, bool albumLegitimacy,
			bool culturalMemory, bool adjacencyFit) {
			Enabled = enabled; ObserveOnly = observeOnly; Pressure = pressure;
			AlbumLegitimacy = albumLegitimacy; CulturalMemory = culturalMemory; AdjacencyFit = adjacencyFit;
		}
	}

	internal static Switches CaptureSwitches() =>
		new(enabled, observeOnly, pressureEnabled, albumLegitimacyEnabled, culturalMemoryEnabled, adjacencyIdentityFit);

	internal static void RestoreSwitches(Switches switches) {
		configured = true;
		enabled = switches.Enabled;
		observeOnly = switches.ObserveOnly;
		pressureEnabled = switches.Pressure;
		albumLegitimacyEnabled = switches.AlbumLegitimacy;
		culturalMemoryEnabled = switches.CulturalMemory;
		adjacencyIdentityFit = switches.AdjacencyFit;
	}

	internal static void ConfigureForProbe(bool enable, bool observe, bool pressure = false, bool legitimacy = false,
		bool adjacencyFit = false, bool culturalMemory = false) {
		configured = true;
		enabled = enable;
		observeOnly = !enable && observe;
		pressureEnabled = pressure;
		albumLegitimacyEnabled = legitimacy;
		culturalMemoryEnabled = culturalMemory;
		adjacencyIdentityFit = adjacencyFit;
	}

	internal static void ResetForProbe() {
		configured = false; enabled = false; observeOnly = false;
		pressureEnabled = false; albumLegitimacyEnabled = false; culturalMemoryEnabled = false;
		adjacencyIdentityFit = false;
	}
}
