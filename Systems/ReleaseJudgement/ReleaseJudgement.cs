using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Feature boundary for the Release Judgement overhaul (SimTools/ReleaseJudgementDirective.md).
/// <para>
/// Phase 1 is observe-only: <c>--release-judgement-shadow</c> keeps a private, saved song shortlist per AI act and logs
/// what the label would have judged best at each legacy single release. It writes nothing the economy reads, registers no
/// catalogue songs, mints no titles and makes no global <c>GD.Rand*</c> draw, so every pre-existing CSV is byte-identical
/// with it on. Later phases (selection, slate, calendar) are separate switches and are rejected until they exist.
/// </para>
/// </summary>
public static class ReleaseJudgement {
	public const string ShadowSwitch = "--release-judgement-shadow";
	public const string EventsSwitch = "--release-judgement-events";
	/// <summary>Bumped whenever an arrival, readiness or perception rule changes what a seeded world would hold.</summary>
	public const int ConfigVersion = 1;

	private static bool configured;
	public static bool Shadow { get; private set; }
	/// <summary>Per-transition material rows (arrivals, readiness, archive, shadow issue). Requires the shadow.</summary>
	public static bool Events { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		Shadow = args.Contains(ShadowSwitch);
		Events = args.Contains(EventsSwitch);
		if (Events && !Shadow) throw new ArgumentException($"{EventsSwitch} requires {ShadowSwitch}.");
		foreach (string argument in args)
			if (argument.StartsWith("--release-judgement-", StringComparison.Ordinal) && argument != ShadowSwitch && argument != EventsSwitch)
				throw new ArgumentException($"{argument}: only the Phase 1 shadow exists (directive Phase 2+ is not implemented).");
		configured = true;
	}

	/// <summary>Probe suites flip the switch and must be able to put it back.</summary>
	internal static void SetForProbe(bool shadow, bool events = false) { Shadow = shadow; Events = events && shadow; configured = true; }

	/// <summary>
	/// A uniform [0,1) draw keyed on the world seed, this namespace and version, and a caller key built from stable ids and
	/// weeks. Touches no RNG stream: rereading a candidate, reordering the slate or adding an act rerolls nothing else.
	/// </summary>
	public static float Unit(string key) =>
		RepertoireTaxonomy.Unit(BandLife.WorldSeed.ToString(CultureInfo.InvariantCulture) + "|release-judgement|v" + ConfigVersion + "|" + key);

	/// <summary>Phase 1 parameter hypotheses (directive "Decisions to settle with probes"). Starting probes, not constants.</summary>
	public static class Parameters {
		/// <summary>Active-consideration cap per act; not a cap on the live set, catalogue or masters.</summary>
		public const int ShortlistCap = 8;
		/// <summary>Opportunities per signed act-week before source and act modifiers (~11 a year).</summary>
		public const float BaseArrivalRate = 0.22f;
		/// <summary>Candidates an act is seeded with on first sight, already recordable (initialization provenance).</summary>
		public const int InitialStock = 3;
		/// <summary>A recent-hit cover loses its commercial window this many weeks after it reached the act.</summary>
		public const int RecentHitWindowWeeks = 40;
		/// <summary>Archived or shadow-issued entries are dropped from saved state after this many weeks.</summary>
		public const int ArchiveRetentionWeeks = 26;
		/// <summary>An act off every AI roster this long stops being tracked.</summary>
		public const int UntrackAfterWeeks = 104;
		/// <summary>Label read error band at scoutingAbility 0 and 1 (heard material); an unheard offer reads 1.5x wider.</summary>
		public const float ReadBandMax = 0.12f, ReadBandMin = 0.04f, UnheardBandScale = 1.5f;
		/// <summary>Share of the band that is a persistent label/act bias rather than song-specific error.</summary>
		public const float BiasShare = 0.5f;
	}
}
