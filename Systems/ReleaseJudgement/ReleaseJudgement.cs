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
/// <para>
/// Phase 2A is causal: <c>--release-judgement-select=perceived|fit</c> (requires the shadow) records a live AI orphan
/// single from the act's shortlist instead of the legacy source lottery. Act, format and capacity policy are unchanged; an
/// empty or refused shortlist falls back to the legacy choice. Without it the shadow stays observe-only.
/// </para>
/// </summary>
public enum ReleaseSelectMode { Off, Perceived, Fit }

public static class ReleaseJudgement {
	public const string ShadowSwitch = "--release-judgement-shadow";
	public const string EventsSwitch = "--release-judgement-events";
	public const string TuneSwitch = "--release-judgement-shadow-tune";
	public const string SelectSwitch = "--release-judgement-select";
	/// <summary>Bumped whenever an arrival, readiness or perception rule changes what a seeded world would hold.
	/// v3: shadow originals get a provisional polar fit at readiness instead of an assumed full capability.</summary>
	public const int ConfigVersion = 3;
	/// <summary>The keyed-draw namespace. Separate from <see cref="ConfigVersion"/> so a rule change that adds no draw
	/// does not re-key every arrival and read (re-keying alone moved tier gains by up to 0.008 on one seed).</summary>
	public const int StreamVersion = 2;

	private static bool configured;
	public static bool Shadow { get; private set; }
	/// <summary>Per-transition material rows (arrivals, readiness, archive, shadow issue). Requires the shadow.</summary>
	public static bool Events { get; private set; }
	/// <summary>Phase 2A: which ranking records a live orphan single from the shortlist. Off = observe-only.</summary>
	public static ReleaseSelectMode Select { get; private set; }
	public static bool Selecting => Shadow && Select != ReleaseSelectMode.Off;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		Shadow = args.Contains(ShadowSwitch);
		Events = args.Contains(EventsSwitch);
		if (Events && !Shadow) throw new ArgumentException($"{EventsSwitch} requires {ShadowSwitch}.");
		foreach (string argument in args) {
			if (argument.StartsWith(TuneSwitch + "=", StringComparison.Ordinal)) {
				if (!Shadow) throw new ArgumentException($"{TuneSwitch} requires {ShadowSwitch}.");
				Parameters.Tune(argument[(TuneSwitch.Length + 1)..]);
				continue;
			}
			if (argument.StartsWith(SelectSwitch + "=", StringComparison.Ordinal)) {
				if (!Shadow) throw new ArgumentException($"{SelectSwitch} requires {ShadowSwitch}.");
				Select = argument[(SelectSwitch.Length + 1)..] switch {
					"perceived" => ReleaseSelectMode.Perceived,
					"fit" => ReleaseSelectMode.Fit,
					var other => throw new ArgumentException($"{SelectSwitch}: '{other}' is not perceived or fit.")
				};
				continue;
			}
			if (argument.StartsWith("--release-judgement-", StringComparison.Ordinal) && argument != ShadowSwitch && argument != EventsSwitch)
				throw new ArgumentException($"{argument}: only the Phase 1 shadow and Phase 2A selection exist (later phases are not implemented).");
		}
		configured = true;
	}

	/// <summary>Probe suites flip the switch and must be able to put it back.</summary>
	internal static void SetForProbe(bool shadow, bool events = false, ReleaseSelectMode select = ReleaseSelectMode.Off) {
		Shadow = shadow; Events = events && shadow; Select = shadow ? select : ReleaseSelectMode.Off; configured = true;
	}

	/// <summary>
	/// A uniform [0,1) draw keyed on the world seed, this namespace and version, and a caller key built from stable ids and
	/// weeks. Touches no RNG stream: rereading a candidate, reordering the slate or adding an act rerolls nothing else.
	/// </summary>
	public static float Unit(string key) =>
		RepertoireTaxonomy.Unit(BandLife.WorldSeed.ToString(CultureInfo.InvariantCulture) + "|release-judgement|v" + StreamVersion + "|" + key);

	/// <summary>Phase 1 parameter hypotheses (directive "Decisions to settle with probes"). Starting probes, not constants.</summary>
	/// <para>
	/// <c>--release-judgement-shadow-tune=Name:value,...</c> overrides them for a calibration sweep. A tuned run's shadow
	/// state is not comparable with the defaults' (its save carries the same ConfigVersion), so tune probes only.
	/// </para>
	public static class Parameters {
		/// <summary>Active-consideration cap per act; not a cap on the live set, catalogue or masters.</summary>
		public static int ShortlistCap { get; private set; }
		/// <summary>Opportunities per signed act-week before source and act modifiers (~3.3-3.9 arrivals a signed act-year,
		/// about twice the A-sides an act issues; 0.22 saturated the shortlist, 2026-10-19 sweep).</summary>
		public static float BaseArrivalRate { get; private set; }
		/// <summary>Candidates an act is seeded with on first sight, already recordable (initialization provenance).
		/// Three made seeded songs 64% of first-year picks and flattered year one; one left a debut a single song.</summary>
		public static int InitialStock { get; private set; }
		/// <summary>A recent-hit cover loses its commercial window this many weeks after it reached the act.</summary>
		public static int RecentHitWindowWeeks { get; private set; }
		/// <summary>Archived or shadow-issued entries are dropped from saved state after this many weeks.</summary>
		public static int ArchiveRetentionWeeks { get; private set; }
		/// <summary>An act off every AI roster this long stops being tracked.</summary>
		public static int UntrackAfterWeeks { get; private set; }
		/// <summary>Label read error band at scoutingAbility 0 and 1 (heard material); an unheard offer reads 1.5x wider.</summary>
		public static float ReadBandMax { get; private set; }
		public static float ReadBandMin { get; private set; }
		public static float UnheardBandScale { get; private set; }
		/// <summary>Share of the band that is a persistent label/act bias rather than song-specific error. Only the song
		/// error ranks one act's candidates; the bias moves every read of that act together (a later bar's error).</summary>
		public static float BiasShare { get; private set; }

		static Parameters() => Reset();

		internal static void Reset() {
			ShortlistCap = 8; BaseArrivalRate = 0.07f; InitialStock = 2; RecentHitWindowWeeks = 40; ArchiveRetentionWeeks = 26;
			UntrackAfterWeeks = 104; ReadBandMax = 0.20f; ReadBandMin = 0.08f; UnheardBandScale = 1.5f; BiasShare = 0.25f;
		}

		/// <summary>Applies <c>Name:value</c> pairs; an unknown name or unparseable value throws.</summary>
		internal static void Tune(string spec) {
			foreach (string pair in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				string[] kv = pair.Split(':');
				if (kv.Length != 2) throw new ArgumentException($"{TuneSwitch}: '{pair}' is not Name:value.");
				float f = float.Parse(kv[1], CultureInfo.InvariantCulture);
				int i = (int)f;
				switch (kv[0]) {
					case nameof(ShortlistCap): ShortlistCap = i; break;
					case nameof(BaseArrivalRate): BaseArrivalRate = f; break;
					case nameof(InitialStock): InitialStock = i; break;
					case nameof(RecentHitWindowWeeks): RecentHitWindowWeeks = i; break;
					case nameof(ReadBandMax): ReadBandMax = f; break;
					case nameof(ReadBandMin): ReadBandMin = f; break;
					case nameof(UnheardBandScale): UnheardBandScale = f; break;
					case nameof(BiasShare): BiasShare = f; break;
					default: throw new ArgumentException($"{TuneSwitch}: unknown parameter '{kv[0]}'.");
				}
			}
		}
	}
}
