using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17u), off unless named.
/// --senior-cooling gives senior status a clock: the weeks since any record by the act entered the Top 40.
/// --senior-cooling-launch-only runs the launch-read half without the ladder half (screen arm A).
///
/// History's multi-hit acts return to the Top 10 at 38.5% a year while hot, 9.3% after one fallow year and not at
/// all after two. The sim matches the hot rate but keeps returning cold stars, because the fame inputs a launch reads
/// (reputation and the recognition stock) do not know how long ago the act last had a hit, and the career ladder only
/// falls back on consecutive flops. With the switch on:
/// <list type="bullet">
/// <item>Launch reads of reputation and recognition (awareness, stock, radio) are multiplied by <see cref="Cool"/>:
/// 1 for the first <see cref="GraceWeeks"/> weeks, then F + (1 - F) * 0.5^((k - G) / H). The floor is "keeps the
/// name". The recognition stock itself, cultural standing and every UI read are untouched. Momentum is excluded: its
/// own half-life probe (--artist-momentum-half-life) already decays it.</item>
/// <item>A senior act (Established and above) whose clock passes G steps down one rung, then one more every H weeks
/// (Superstar -> Star -> Established -> Declining). A Top 40 resets the clock but restores no rung; the ladder's own
/// rules re-earn them (Declining -> Established on one Top 40).</item>
/// </list>
/// Label-blind, applies to player-signed acts too, and draws no RNG. Off: every read is the identity.
/// </summary>
public static class SeniorCooling {
	public const string Switch = "--senior-cooling";
	public const string LaunchOnlySwitch = "--senior-cooling-launch-only";
	public const int GraceWeeks = 52;
	public const float HalfLifeWeeks = 26f;
	public const float Floor = 0.35f;
	/// <summary>An unset clock; seeded on first read from the act's public history, else from that week.</summary>
	public const int Unset = int.MinValue;

	private static bool configured;
	private static bool enabled;
	private static bool ladder;
	private static int lastLadderWeek = int.MinValue;
	public static bool Enabled => enabled;
	public static bool LadderEnabled => enabled && ladder;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		bool both = args.Contains(Switch), launchOnly = args.Contains(LaunchOnlySwitch);
		if (both && launchOnly) throw new ArgumentException("Name --senior-cooling or --senior-cooling-launch-only, not both.");
		enabled = both || launchOnly;
		ladder = both;
		configured = true;
	}

	internal static void SetForChecks(bool on, bool withLadder) {
		configured = true; enabled = on; ladder = withLadder; lastLadderWeek = int.MinValue;
	}

	private static int CurrentWeek => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;

	/// <summary>Called when the act is credited a Top 40. A no-op when off, so the field is never written.</summary>
	public static void OnTop40(SimulatedArtist artist) => OnTop40(artist, CurrentWeek);

	internal static void OnTop40(SimulatedArtist artist, int week) {
		if (!enabled || artist == null) return;
		artist.seniorCoolingLastTop40Week = week;
	}

	/// <summary>Weeks since the act's last Top 40 entry; seeds an unset clock. Chart week 0 is the first week of 1960.</summary>
	public static int WeeksSinceTop40(SimulatedArtist artist, int week) {
		if (artist.seniorCoolingLastTop40Week == Unset) artist.seniorCoolingLastTop40Week = SeedWeek(artist, week);
		return Math.Max(0, week - artist.seniorCoolingLastTop40Week);
	}

	/// <summary>
	/// An opening career's last pre-1960 Top 40 is placed mid-year (1959 -> 26 weeks before the run); an act with no
	/// such side starts its clock at the week of first read, which is week 0 for the 1960 roster.
	/// </summary>
	private static int SeedWeek(SimulatedArtist artist, int week) {
		int lastYear = artist.publicHistory?.Where(e => e.peakPosition is > 0 and <= 40).Select(e => e.year).DefaultIfEmpty(0).Max() ?? 0;
		return lastYear > 0 ? (lastYear - 1960) * 52 + 26 : week;
	}

	public static float Cool(int weeksSince) {
		if (weeksSince <= GraceWeeks) return 1f;
		return Floor + (1f - Floor) * Mathf.Pow(0.5f, (weeksSince - GraceWeeks) / HalfLifeWeeks);
	}

	/// <summary>The factor on the act's launch fame reads; exactly 1 when off.</summary>
	public static float LaunchFactor(SimulatedArtist artist) => !enabled || artist == null ? 1f : Cool(WeeksSinceTop40(artist, CurrentWeek));

	/// <summary>One idempotent ladder pass per chart week. A second call in the same week is a no-op.</summary>
	public static void CoolLadderForWeek(int week, IEnumerable<SimulatedArtist> artists) {
		if (!LadderEnabled || artists == null || week == lastLadderWeek) return;
		lastLadderWeek = week;
		foreach (SimulatedArtist artist in artists) CoolOne(artist, week);
	}

	internal static void CoolOne(SimulatedArtist artist, int week) {
		if (artist == null || artist.careerState < CareerState.Established || artist.careerState > CareerState.Superstar) return;
		if (ArtistPopulationLifecycle.Enabled && artist.IsContractPerformanceProbationPending()) return;
		int since = WeeksSinceTop40(artist, week);
		if (since <= GraceWeeks) return;
		bool steppedThisSpell = artist.seniorCoolingLastStepWeek != Unset && artist.seniorCoolingLastStepWeek > artist.seniorCoolingLastTop40Week;
		if (steppedThisSpell && week - artist.seniorCoolingLastStepWeek < HalfLifeWeeks) return;
		artist.careerState = artist.careerState switch {
			CareerState.Superstar => CareerState.Star,
			CareerState.Star => CareerState.Established,
			_ => CareerState.Declining
		};
		artist.seniorCoolingLastStepWeek = week;
		artist.seniorCoolingSteps++;
	}
}
