using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/HistoricalLabelTierBalanceResearch.md §17p), off unless named.
/// --artist-momentum-half-life=W gives <see cref="SimulatedArtist.momentum"/> a calendar half-life of W weeks.
///
/// Momentum is the "hot now" half of an act's fame. Launch awareness reads it at x0.5, release priority and the artist
/// pick read it, and so do re-signing, contract pricing and evolution confidence. At runtime it moved only on chart
/// outcomes, so a 1960 seed value or an old hit stayed at full strength until the act flopped -- a pure hit ratchet,
/// against OpeningCareerService's own definition ("momentum is what the last two years did ... a former star keeps
/// the name and loses the heat"). With the switch on, every artist's momentum is multiplied by 0.5^(1/W) once per
/// chart week. Hits still add their increments; reputation (the whole record) is untouched. Draws no RNG.
/// </summary>
public static class ArtistMomentumHalfLife {
	public const string SwitchPrefix = "--artist-momentum-half-life=";
	private static bool configured;
	private static float halfLifeWeeks;
	private static float weeklyFactor = 1f;
	private static int lastDecayedWeek = int.MinValue;
	public static bool Enabled => weeklyFactor < 1f;
	public static float HalfLifeWeeks => halfLifeWeeks;
	public static float WeeklyFactor => weeklyFactor;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --artist-momentum-half-life once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 1f))
				throw new ArgumentException("--artist-momentum-half-life needs a half-life of at least one week.");
			SetHalfLife(parsed);
		}
		configured = true;
	}

	private static void SetHalfLife(float weeks) {
		halfLifeWeeks = weeks;
		weeklyFactor = weeks > 0f ? Mathf.Pow(0.5f, 1f / weeks) : 1f;
	}

	internal static void SetHalfLifeForChecks(float weeks) {
		configured = true; lastDecayedWeek = int.MinValue;
		if (weeks > 0f) SetHalfLife(weeks); else { halfLifeWeeks = 0f; weeklyFactor = 1f; }
	}

	/// <summary>One decay step for one act; the identity when off.</summary>
	public static float Decay(float momentum) => Enabled && momentum > 0f ? momentum * weeklyFactor : momentum;

	/// <summary>One idempotent pass per chart week over every artist. A second call in the same week is a no-op.</summary>
	public static void DecayRegistryForWeek(int week, IEnumerable<SimulatedArtist> artists) {
		if (!Enabled || artists == null || week == lastDecayedWeek) return;
		lastDecayedWeek = week;
		foreach (SimulatedArtist artist in artists)
			if (artist != null) artist.momentum = Decay(artist.momentum);
	}
}
