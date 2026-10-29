using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// §9m probe (SimTools/RegionalSyncDesign.md): a block of new-release picks on each station's regional (5e) playlist that
/// is not chosen by local sales standing. Under <c>standing</c> the meeting follows each market's sellers, so the panel
/// stops lifting the young, small, mostly non-Major records the national meeting rotated, and the chart loses the
/// airplay offset that held Majors below their sales ranking (§9k). This reserves <c>--station-picks=N</c> slots in a
/// tier for records at most <c>--station-picks-age</c> weeks old, ranked by one of two non-sales scores:
/// <c>trial</c>, the national meeting's own candidacy (size-blind national support, heat, the DJ's read), and <c>ear</c>,
/// the DJ's read of quality and genre with no sales or heat term. Picks are seated after the ranking, from records it did
/// not seat. Off (0) unless named, and only the regional meeting reads it; it draws nothing of its own.
/// <para>§9x adds <c>fit</c>: the programmer hears the record's commercial connection (the commercial-fit national draw)
/// through keyed noise of <c>--station-picks-fit-noise=S</c> log units per station and record, and picks only among
/// records serviced to the station's market at release (the launch footprint of <see cref="RegionalFitExposure"/>). Picks
/// then lead local sales, so a record can be on a city's list before it charts nationally.
/// <c>--station-picks-fit-bar=B</c>: a programmer adds only a record he hears at least B log units above the connection
/// line (<see cref="SingleCommercialFit.UnexposedCapLog"/>), so slots can stay empty when the week has no record he
/// believes in. <c>--station-picks-fit-unserviced</c>: picks only among records the label could not service everywhere at
/// release (held in some region), the records a DJ break was the route for.
/// <c>--station-picks-extra</c>: the block is added on top of the tier instead of taken out of it, so picks displace no
/// record the sales ranking seats (§9x: the reserved block cost the ranking 2 Light slots a station).</para>
/// </summary>
public static class StationPicks {
	public enum PickScore { Trial, Ear, Fit }
	private const string SlotsPrefix = "--station-picks=";
	private const string TierPrefix = "--station-picks-tier=";
	private const string ScorePrefix = "--station-picks-score=";
	private const string AgePrefix = "--station-picks-age=";
	private const string FitNoisePrefix = "--station-picks-fit-noise=";
	private const string FitBarPrefix = "--station-picks-fit-bar=";
	private const string FitUnservicedFlag = "--station-picks-fit-unserviced";
	private const string ExtraFlag = "--station-picks-extra";
	private static bool configured;
	public static int Slots { get; private set; }
	public static SpinTier Tier { get; private set; } = SpinTier.Light;
	public static PickScore Score { get; private set; } = PickScore.Trial;
	public static int MaxAgeWeeks { get; private set; } = 6;
	/// <summary>§9x: sd of the programmer's error on the log fit draw.</summary>
	public static float FitNoise { get; private set; } = 1f;
	/// <summary>§9x: the heard log a pick must clear above the connection line; null (no bar) unless named.</summary>
	public static float? FitBar { get; private set; }
	public static bool FitUnserviced { get; private set; }
	/// <summary>§9x: picks are seated in slots added to the tier, not reserved from it.</summary>
	public static bool Extra { get; private set; }
	public static bool Enabled => Slots > 0;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string a in arguments ?? Array.Empty<string>()) {
			if (a.StartsWith(SlotsPrefix, StringComparison.Ordinal)) Slots = PositiveInt(a, SlotsPrefix);
			else if (a.StartsWith(AgePrefix, StringComparison.Ordinal)) MaxAgeWeeks = PositiveInt(a, AgePrefix);
			else if (a.StartsWith(TierPrefix, StringComparison.Ordinal))
				Tier = a.Substring(TierPrefix.Length) switch {
					"light" => SpinTier.Light,
					"mid" => SpinTier.Mid,
					"high" => SpinTier.High,
					_ => throw new ArgumentException(TierPrefix + " takes light, mid or high.")
				};
			else if (a.StartsWith(ScorePrefix, StringComparison.Ordinal))
				Score = a.Substring(ScorePrefix.Length) switch {
					"trial" => PickScore.Trial,
					"ear" => PickScore.Ear,
					"fit" => PickScore.Fit,
					_ => throw new ArgumentException(ScorePrefix + " takes trial, ear or fit.")
				};
			else if (a.StartsWith(FitNoisePrefix, StringComparison.Ordinal)) {
				if (!float.TryParse(a.Substring(FitNoisePrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float noise) || noise < 0f)
					throw new ArgumentException(FitNoisePrefix + " takes a number of at least 0.");
				FitNoise = noise;
			}
			else if (a.StartsWith(FitBarPrefix, StringComparison.Ordinal)) {
				if (!float.TryParse(a.Substring(FitBarPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float bar))
					throw new ArgumentException(FitBarPrefix + " takes a number.");
				FitBar = bar;
			}
			else if (a == FitUnservicedFlag) FitUnserviced = true;
			else if (a == ExtraFlag) Extra = true;
		}
		if (Enabled && Score == PickScore.Fit) {
			if (!SingleCommercialFit.Enabled) throw new ArgumentException(ScorePrefix + "fit requires " + SingleCommercialFit.SigmaPrefix + ".");
			// Read the flag itself: RegionalFitExposure is configured after this.
			if (FitUnserviced && !(arguments ?? Array.Empty<string>()).Contains(RegionalFitExposure.Flag)) throw new ArgumentException(FitUnservicedFlag + " requires " + RegionalFitExposure.Flag + ".");
			Godot.GD.Print($"STATION_PICKS fit slots={Slots} tier={Tier} age={MaxAgeWeeks} noise={FitNoise:F2} " +
				$"bar={(FitBar.HasValue ? FitBar.Value.ToString("F2", CultureInfo.InvariantCulture) : "none")} unserviced={FitUnserviced} extra={Extra}");
		}
		configured = true;
	}

	private static int PositiveInt(string argument, string prefix) {
		if (!int.TryParse(argument.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value <= 0)
			throw new ArgumentException(prefix + " takes a positive whole number.");
		return value;
	}
}
