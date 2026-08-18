using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for the act-level celebrity-recognition stock. Copies the
/// <see cref="ArtistEvolution"/> pattern deliberately: observation is separated from
/// ratification so the counterfactual can be measured before it is allowed to move a
/// launch. With the flag off, no field is written, no draw is made, and every release
/// path is byte-identical to today.
/// <para>
/// See SimTools/CelebrityRecognitionDirective.md. Recognition is a rich-get-richer term
/// on a fixed 52,100 slot-weeks ([[chart-slot-weeks-identity]]), so it ships behind its
/// own switch and its own A/B and never rides along on another phase.
/// </para>
/// </summary>
public static class ArtistRecognition {
	private static bool configured;
	private static bool enabled;
	private static bool observeOnly;

	/// <summary>Stock is written AND consumed at launch.</summary>
	public static bool Enabled => enabled;
	/// <summary>Stock is written and logged; launch consumes nothing. Superset of Enabled.</summary>
	public static bool Observing => enabled || observeOnly;
	/// <summary>Consumption is meaningful only where the enabled genre market runs a live chart.</summary>
	public static bool IsLive => enabled && ArtistPopulationLifecycle.IsLive;
	/// <summary>Write-back and decay run in observe-only as well, but only on the live market.</summary>
	public static bool IsObservingLive => Observing && ArtistPopulationLifecycle.IsLive;

	// A distinct namespace from the population and evolution streams. Recognition is
	// deterministic by design and should need no draw at all; this exists so a future
	// tie-break has somewhere to go that is not the global stream.
	public const ulong RngNamespace = 0x6172746372636f67UL; // "artcrcog"

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false, observe = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-artist-recognition") enable = true;
			if (argument == "--disable-artist-recognition") disable = true;
			if (argument == "--observe-artist-recognition") observe = true;
		}
		if (enable && disable)
			throw new ArgumentException("--enable-artist-recognition and --disable-artist-recognition cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		if (enabled && !ArtistPopulationLifecycle.Enabled)
			throw new ArgumentException("Artist recognition requires the artist population lifecycle to be enabled.");
		// Observation is a diagnostic overlay; it needs the same live population to read against.
		observeOnly = !enabled && observe;
		if (observeOnly && !ArtistPopulationLifecycle.Enabled)
			throw new ArgumentException("--observe-artist-recognition requires the artist population lifecycle to be enabled.");
		configured = true;
	}

	/// <summary>The raw switch positions, so a probe suite can put every one of them back.</summary>
	internal readonly struct Switches {
		public readonly bool Enabled, ObserveOnly;
		public Switches(bool enabled, bool observeOnly) { Enabled = enabled; ObserveOnly = observeOnly; }
	}

	internal static Switches CaptureSwitches() => new(enabled, observeOnly);

	internal static void RestoreSwitches(Switches switches) {
		configured = true;
		enabled = switches.Enabled;
		observeOnly = switches.ObserveOnly;
	}

	internal static void ConfigureForProbe(bool enable, bool observe) {
		configured = true;
		enabled = enable;
		observeOnly = !enable && observe;
	}

	internal static void ResetForProbe() {
		configured = false; enabled = false; observeOnly = false;
	}
}
