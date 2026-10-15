using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>The three modes of MediaAtmosphereDirective §12.1. Causal is reserved: nothing in this build implements it.</summary>
public enum MediaAtmosphereMode { Disabled, Observe }

/// <summary>
/// Feature boundary for television, press and radio atmosphere (SimTools/MediaAtmosphereDirective.md).
/// <para>
/// Observe is the scene default: programs air, producers book rival acts, the paper prints the listings and
/// the estimated audiences. Nothing the economy reads is written, no global <c>GD.Rand*</c> draw is made, and
/// a seeded run is byte-identical to Disabled on every economy CSV (proved against SimLogs/ref-a358946-*).
/// Disabled skips all media work. A causal mode does not exist yet; the flag is rejected rather than ignored.
/// </para>
/// </summary>
public static class MediaAtmosphere {
	private static bool configured;
	private static MediaAtmosphereMode mode = MediaAtmosphereMode.Observe;

	public static MediaAtmosphereMode Mode => mode;
	public static bool Observing => mode == MediaAtmosphereMode.Observe;

	/// <summary>Bumped whenever a catalog, curve or booking rule changes what a seeded world would show.</summary>
	public const int ConfigVersion = 2;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool disable = false, observe = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--disable-media-atmosphere") disable = true;
			if (argument == "--observe-media-atmosphere") observe = true;
			if (argument == "--enable-media-atmosphere" || argument == "--causal-media-atmosphere")
				throw new ArgumentException($"{argument}: the causal media mode is not implemented (directive §12, Phase 4).");
		}
		if (disable && observe)
			throw new ArgumentException("--disable-media-atmosphere and --observe-media-atmosphere cannot be used together.");
		mode = disable ? MediaAtmosphereMode.Disabled : observe || sceneDefault ? MediaAtmosphereMode.Observe : MediaAtmosphereMode.Disabled;
		configured = true;
	}

	/// <summary>Probe suites flip the switch and must be able to put it back.</summary>
	internal static void SetForProbe(MediaAtmosphereMode value) { mode = value; configured = true; }

	/// <summary>
	/// A uniform [0,1) draw keyed on the world seed, the media namespace and version, and a caller key built from
	/// stable ids and dates (§15.1). It touches no RNG stream, so reopening an offer or re-reading the paper can
	/// never reroll anything. The world seed is the band-life seed, which saves already carry.
	/// </summary>
	public static float Unit(string key) =>
		RepertoireTaxonomy.Unit(BandLife.WorldSeed.ToString(CultureInfo.InvariantCulture) + "|media|v" + ConfigVersion + "|" + key);
}
