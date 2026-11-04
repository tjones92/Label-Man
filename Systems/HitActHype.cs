using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (LabelReleaseProgrammeImplementation.md, "Hit-act hype probe"), off unless named.
///
/// History's hit acts put 2.43 records on the Hot 100 in the 52 weeks after their first Top 10 and those records were
/// ordinary: P(Top 10 | entry) .19 against .15 for everyone. The sim's put 1.07 on, converting at .27. A Top 10 adds
/// ~0.07 public recognition, which is ~+0.01 launch awareness, so a fresh hit act's next single launches nearly as
/// anonymous as a debut (awareness .24 Rising, .19 NewSigning, .79 Star) and its ordinary material does not chart.
///
/// <c>--hit-act-hype=A</c> adds <c>A * h</c> to an AI single's launch awareness and <c>--hit-act-hype-radio=R</c> adds
/// <c>R * h</c> to its launch radio heat, where the act's hype is
/// <c>h = max(0.5^(t10 / H), TOP40_SHARE * 0.5^(t40 / H))</c>, t10 / t40 the weeks since the act's last Top 10 / Top 40
/// and H <c>--hit-act-hype-half-life</c> (default 26). It is recency, not stock: a fallow act loses it in about a year,
/// so it does not feed the cold-return excess durable recognition already carries (§17u). It reads at launch only and
/// rides the existing concurrent-record share, so two singles at once split it. Albums, label push and the format
/// decision's projected awareness are untouched. Clocks are written only while the switch is on; an opening career's
/// last pre-1960 hit is placed mid-1959, and an act with none has no hype. Off: every read is 0 and nothing is written.
/// </summary>
public static class HitActHype {
	public const string AwarenessPrefix = "--hit-act-hype=";
	public const string RadioPrefix = "--hit-act-hype-radio=";
	public const string HalfLifePrefix = "--hit-act-hype-half-life=";
	public const float DefaultHalfLifeWeeks = 26f;
	public const float Top40Share = 0.5f;
	public const int Unset = int.MinValue;
	private static bool configured;
	private static float awarenessLift = -1f;
	private static float radioLift;
	private static float halfLife = DefaultHalfLifeWeeks;
	public static bool Enabled => awarenessLift >= 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string a = Single(args, AwarenessPrefix), r = Single(args, RadioPrefix), h = Single(args, HalfLifePrefix);
		if (a != null) awarenessLift = Parse(a, AwarenessPrefix, 0f, 1f);
		if ((r != null || h != null) && a == null) throw new ArgumentException(RadioPrefix + " and " + HalfLifePrefix + " need " + AwarenessPrefix + ".");
		if (r != null) radioLift = Parse(r, RadioPrefix, 0f, 1f);
		if (h != null) halfLife = Parse(h, HalfLifePrefix, 1f, 260f);
		configured = true;
	}

	private static float Parse(string raw, string prefix, float lo, float hi) {
		if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !(value >= lo && value <= hi))
			throw new ArgumentException(prefix + $" needs a value in [{lo}, {hi}].");
		return value;
	}

	private static string Single(string[] args, string prefix) {
		var values = args.Where(arg => arg.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
		return values.Length == 1 ? values[0].Substring(prefix.Length) : null;
	}

	internal static void SetForChecks(float awareness, float radio = 0f, float halfLifeWeeks = DefaultHalfLifeWeeks) {
		configured = true; awarenessLift = awareness; radioLift = radio; halfLife = halfLifeWeeks;
	}

	private static int CurrentWeek => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;

	// ---- clocks, written only while the switch is on ---------------------------------------------------

	public static void OnTop40(SimulatedArtist artist) { if (Enabled && artist != null) artist.hypeLastTop40Week = CurrentWeek; }
	public static void OnTop10(SimulatedArtist artist) { if (Enabled && artist != null) artist.hypeLastTop10Week = CurrentWeek; }

	/// <summary>The act's hype in [0, 1] at chart week <paramref name="week"/>; seeds unset clocks from public history.</summary>
	internal static float Hype(SimulatedArtist artist, int week) {
		if (!Enabled || artist == null) return 0f;
		if (artist.hypeLastTop10Week == Unset) artist.hypeLastTop10Week = SeedWeek(artist, 10);
		if (artist.hypeLastTop40Week == Unset) artist.hypeLastTop40Week = SeedWeek(artist, 40);
		return Mathf.Max(Decay(artist.hypeLastTop10Week, week), Top40Share * Decay(artist.hypeLastTop40Week, week));
	}

	private static float Decay(int hitWeek, int week) =>
		hitWeek == Unset || hitWeek == Unset + 1 ? 0f : Mathf.Pow(0.5f, Math.Max(0, week - hitWeek) / halfLife);

	// Unset + 1 marks "seeded, never hit", so the seed is read once and an act with no hit stays at zero.
	private static int SeedWeek(SimulatedArtist artist, int bar) {
		int lastYear = artist.publicHistory?.Where(e => e.peakPosition > 0 && e.peakPosition <= bar).Select(e => e.year)
			.DefaultIfEmpty(0).Max() ?? 0;
		return lastYear > 0 ? (lastYear - 1960) * 52 + 26 : Unset + 1;
	}

	/// <summary>Launch awareness added to an AI single; 0 when off.</summary>
	public static float AwarenessLift(SimulatedArtist artist) => AwarenessLift(artist, CurrentWeek);

	internal static float AwarenessLift(SimulatedArtist artist, int week) => awarenessLift * Hype(artist, week);

	/// <summary>Launch radio heat added to an AI single; 0 when off or with no radio lift.</summary>
	public static float RadioLift(SimulatedArtist artist) => radioLift * Hype(artist, CurrentWeek);
}
