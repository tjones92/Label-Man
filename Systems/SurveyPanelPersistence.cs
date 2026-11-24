using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Calibration probe (SimTools/Top10TurnoverFindings.md §11, #1 returns), off unless named.
/// <c>--survey-panel-persistence=ρ</c> makes a record's survey error persist from week to week, as a fixed outlet panel's
/// would: most of a record's sampling error is which stores and stations happen to carry it, and that composition holds
/// while the record's footprint holds. The standardised error follows z ← ρ·z + √(1−ρ²)·ε, so each week's marginal
/// sigma (from units, <see cref="ChartSimulator.GetSurveySigma"/>) is unchanged; only its timing changes.
/// ε is the draw <see cref="ChartSimulator.DrawSurveySample"/> already makes, so the global stream keeps its count.
/// Unnamed (or ρ = 0), <see cref="Persist"/> returns its input bit for bit and a run is byte-identical.
/// The error lives in a weak table, not on <see cref="RecordRuntimeData"/>, so saves never carry it; a loaded game
/// restarts each record's panel error on its next draw.
/// </summary>
public static class SurveyPanelPersistence {
	public const string Switch = "--survey-panel-persistence=";
	private sealed class PanelError { public float z; }
	private static ConditionalWeakTable<RecordRuntimeData, PanelError> errors = new();
	private static bool configured;
	private static float rho;
	public static bool Enabled => rho > 0f;
	public static float Rho => rho;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + Switch.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0].Substring(Switch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
				|| !float.IsFinite(v) || v < 0f || v > 0.99f)
				throw new ArgumentException(Switch.TrimEnd('=') + " needs a value in [0, 0.99].");
			rho = v;
			GD.Print(FormattableString.Invariant($"SURVEY_PANEL_PERSISTENCE rho={rho}"));
		}
		configured = true;
	}

	internal static void SetForChecks(float value) {
		configured = true; rho = value; errors = new ConditionalWeakTable<RecordRuntimeData, PanelError>();
	}

	/// <summary>This week's standardised panel error for <paramref name="record"/>, given this week's fresh standard
	/// normal draw. A record's first draw is its error; later draws move it by AR(1).</summary>
	public static float Persist(RecordRuntimeData record, float standardNormal) {
		if (!Enabled || record == null) return standardNormal;
		if (!errors.TryGetValue(record, out var error)) {
			errors.Add(record, new PanelError { z = standardNormal });
			return standardNormal;
		}
		error.z = rho * error.z + Mathf.Sqrt(1f - rho * rho) * standardNormal;
		return error.z;
	}
}
