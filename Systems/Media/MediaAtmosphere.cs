using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>The three modes of MediaAtmosphereDirective §12.1.</summary>
public enum MediaAtmosphereMode { Disabled, Observe, Causal }

/// <summary>
/// Feature boundary for television, press and radio atmosphere (SimTools/MediaAtmosphereDirective.md).
/// <para>
/// Observe is the scene default: programs air, producers book rival acts, the paper prints the listings and
/// the estimated audiences. Nothing the economy reads is written, no global <c>GD.Rand*</c> draw is made, and
/// a seeded run is byte-identical to Disabled on every economy CSV (proved against SimLogs/ref-a358946-*).
/// Disabled skips all media work.
/// </para>
/// <para>
/// Causal (<c>--causal-media-atmosphere</c>) runs the same media world and adds what Phase 4 makes real: the player's
/// media costs are charged (§5.4), and the bounded writers named on the command line may run (the §12.3 singles demand
/// adapter, <see cref="MediaDemand"/>; the recognition adapter, <see cref="MediaRecognition"/>). AI media stays an
/// allocation of the promotion envelope it already pays (§12.2), so causal mode alone adds no AI debit.
/// </para>
/// </summary>
public static class MediaAtmosphere {
	private static bool configured;
	private static MediaAtmosphereMode mode = MediaAtmosphereMode.Observe;

	public static MediaAtmosphereMode Mode => mode;
	/// <summary>The media world runs (observe or causal): programs book, air, and get written about.</summary>
	public static bool Observing => mode != MediaAtmosphereMode.Disabled;
	/// <summary>Player media actions cost real cash, hours and copies, and the named writers may run.</summary>
	public static bool Causal => mode == MediaAtmosphereMode.Causal;

	public const string CausalSwitch = "--causal-media-atmosphere";

	/// <summary>Opt-in (requires causal): rival acts' network dates pay their members scale into member wealth, as player
	/// dates do (§5.4). Never label cash. The accounting experiment the Phase 4 notes left open.</summary>
	public const string AiPerformerFeesSwitch = "--media-ai-performer-fees";
	public static bool AiPerformerFees { get; private set; }

	/// <summary>Bumped whenever a catalog, curve or booking rule changes what a seeded world would show.</summary>
	public const int ConfigVersion = 4;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool disable = false, observe = false, causal = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--disable-media-atmosphere") disable = true;
			if (argument == "--observe-media-atmosphere") observe = true;
			if (argument == CausalSwitch || argument == "--enable-media-atmosphere") causal = true;
			if (argument.StartsWith("--media-fan-hot=", StringComparison.Ordinal))
				MediaFans.HotAt = float.Parse(argument["--media-fan-hot=".Length..], CultureInfo.InvariantCulture);
			if (argument == "--media-fan-diagnostics") MediaFans.Diagnostics = true;
			if (argument == "--media-hard-sales-rank") MediaFans.SoftSalesStanding = false;
			if (argument == MediaFans.EventHypeSwitch) MediaFans.EventHype = true;
			if (argument == AiPerformerFeesSwitch) AiPerformerFees = true;
		}
		if (AiPerformerFees && !causal)
			throw new ArgumentException($"{AiPerformerFeesSwitch} pays real performer income; it requires {CausalSwitch}.");
		if (disable && (observe || causal))
			throw new ArgumentException("--disable-media-atmosphere cannot be combined with another media mode.");
		if (observe && causal)
			throw new ArgumentException($"--observe-media-atmosphere and {CausalSwitch} cannot be used together.");
		mode = disable ? MediaAtmosphereMode.Disabled
			: causal ? MediaAtmosphereMode.Causal
			: observe || sceneDefault ? MediaAtmosphereMode.Observe : MediaAtmosphereMode.Disabled;
		configured = true;
	}

	/// <summary>Probe suites flip the switch and must be able to put it back.</summary>
	internal static void SetForProbe(MediaAtmosphereMode value) { mode = value; configured = true; }
	internal static void SetAiPerformerFeesForProbe(bool value) => AiPerformerFees = value;

	/// <summary>
	/// A uniform [0,1) draw keyed on the world seed, the media namespace and version, and a caller key built from
	/// stable ids and dates (§15.1). It touches no RNG stream, so reopening an offer or re-reading the paper can
	/// never reroll anything. The world seed is the band-life seed, which saves already carry.
	/// </summary>
	public static float Unit(string key) =>
		RepertoireTaxonomy.Unit(BandLife.WorldSeed.ToString(CultureInfo.InvariantCulture) + "|media|v" + ConfigVersion + "|" + key);
}
