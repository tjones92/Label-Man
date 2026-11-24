using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/Top10TurnoverFindings.md, trajectory shape), off unless named. Each switch replaces one
/// authored constant that shapes a single's week-by-week path, so its effect on Top 10 run length can be screened
/// dynamically. Unnamed, every accessor returns the authored value bit-for-bit, so a run without them is byte-identical.
/// <list type="bullet">
/// <item><c>--airplay-convexity=K</c> replaces AIRPLAY_CONVEXITY (5) in <see cref="ChartSimulator.CalculateChartPoints"/>.
/// The release-age radio build and the post-peak fatigue both sit inside that power, so K sets how sharply airplay
/// points rise and fall over a record's life, as well as how far apart records sit.</item>
/// <item><c>--demand-age-decay=R</c> replaces DEMAND_AGE_DECAY_RATE (0.91), the weekly conversion decay after
/// demand age 8.</item>
/// <item><c>--radio-fatigue-decay=F</c> replaces RADIO_FATIGUE_DECAY (0.88), the weekly cut in the radio heat target
/// after a record's (national or local) sales peak.</item>
/// </list>
/// </summary>
public static class TrajectoryShapeProbe {
	public const string ConvexitySwitch = "--airplay-convexity=";
	public const string AgeDecaySwitch = "--demand-age-decay=";
	public const string FatigueSwitch = "--radio-fatigue-decay=";
	/// <summary>Scales SURVEY_NOISE_SCALE (1), the weekly independent survey draw. In (0, 2], so every record still
	/// draws and the global stream keeps its count.</summary>
	public const string SurveyNoiseSwitch = "--survey-noise-scale=";
	private static bool configured;
	private static float convexity = -1f, ageDecay = -1f, fatigue = -1f, surveyNoise = -1f;
	public static bool Enabled => convexity >= 0f || ageDecay >= 0f || fatigue >= 0f || surveyNoise >= 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		convexity = Read(args, ConvexitySwitch, 1f, 8f);
		ageDecay = Read(args, AgeDecaySwitch, 0.5f, 1f);
		fatigue = Read(args, FatigueSwitch, 0.5f, 1f);
		surveyNoise = Read(args, SurveyNoiseSwitch, 0.01f, 2f);
		if (Enabled) Godot.GD.Print(FormattableString.Invariant(
			$"TRAJECTORY_SHAPE_PROBE convexity={convexity} ageDecay={ageDecay} fatigue={fatigue} surveyNoise={surveyNoise}"));
		configured = true;
	}

	private static float Read(string[] args, string prefix, float min, float max) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return -1f;
		if (values.Length > 1) throw new ArgumentException("Name " + prefix.TrimEnd('=') + " once.");
		if (!float.TryParse(values[0].Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
			|| !float.IsFinite(v) || v < min || v > max)
			throw new ArgumentException(FormattableString.Invariant($"{prefix.TrimEnd('=')} needs a value in [{min}, {max}]."));
		return v;
	}

	internal static void SetForChecks(float convexityValue = -1f, float ageDecayValue = -1f, float fatigueValue = -1f) {
		configured = true; convexity = convexityValue; ageDecay = ageDecayValue; fatigue = fatigueValue;
	}

	public static float Convexity(float authored) => convexity < 0f ? authored : convexity;
	public static float AgeDecay(float authored) => ageDecay < 0f ? authored : ageDecay;
	public static float Fatigue(float authored) => fatigue < 0f ? authored : fatigue;
	public static float SurveyNoise(float authored) => surveyNoise < 0f ? authored : authored * surveyNoise;
}
