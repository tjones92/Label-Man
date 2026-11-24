using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/Top10TurnoverFindings.md, trajectory shape), off unless <c>--single-appeal-drift=S</c>
/// is named. A live single's demand otherwise moves on clocks every record shares (release ramp, radio build, age
/// decay, fatigue) plus rank gates, and its only randomness is weekly: a ±4% regional sales draw and the survey
/// sample, both independent from week to week. Week-to-week flicker reverses 10-15% of pre-peak weeks against
/// history's 1-5%, yet every record turns at about the same age, so fortunes are uniform and climbs are not.
/// <para>
/// This gives each single a persistent fortune instead: a mean-reverting AR(1) log-multiplier on conversion,
/// z(a) = Σ_{k=0..K} ρ^k ε(a−k), ε ~ N(0, S²(1−ρ²)), so z has stationary standard deviation S and one-week
/// autocorrelation ρ (<c>--single-appeal-drift-rho=</c>, default 0.8). It stands for what moved a real single on its
/// own: a regional breakout, a TV spot, a DJ adopting it, a rival cover. Same multiplier in every region.
/// </para>
/// <para>
/// Stateless and keyed: each ε is a Philox draw on (record id, release age, decision), so the global stream is not
/// touched, nothing is saved, and the draw for a record-age is the same whenever it is read. The multiplier is
/// exp(z − S²/2) clamped to ±<see cref="ClampLog"/> in log, so its mean is about 1 and it stays in bounds.
/// </para>
/// </summary>
public static class SingleAppealDrift {
	public const string Switch = "--single-appeal-drift=";
	public const string RhoSwitch = "--single-appeal-drift-rho=";
	public const int Terms = 16;
	public const float ClampLog = 0.9f;
	private const uint Decision = 0x41505044U; // "APPD"
	private static bool configured;
	public static float Sigma { get; private set; }
	public static float Rho { get; private set; } = 0.8f;
	public static bool Enabled => Sigma > 0f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Sigma = Read(args, Switch, 0f, 0.6f, 0f);
		Rho = Read(args, RhoSwitch, 0f, 0.97f, 0.8f);
		if (!Enabled && args.Any(a => a.StartsWith(RhoSwitch, StringComparison.Ordinal)))
			throw new ArgumentException(RhoSwitch.TrimEnd('=') + " needs " + Switch.TrimEnd('=') + ".");
		if (Enabled) Godot.GD.Print(FormattableString.Invariant($"SINGLE_APPEAL_DRIFT sigma={Sigma} rho={Rho}"));
		configured = true;
	}

	private static float Read(string[] args, string prefix, float min, float max, float fallback) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return fallback;
		if (values.Length > 1) throw new ArgumentException("Name " + prefix.TrimEnd('=') + " once.");
		if (!float.TryParse(values[0].Substring(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
			|| !float.IsFinite(v) || v < min || v > max)
			throw new ArgumentException(FormattableString.Invariant($"{prefix.TrimEnd('=')} needs a value in [{min}, {max}]."));
		return v;
	}

	internal static void SetForChecks(float sigma, float rho = 0.8f) { configured = true; Sigma = sigma; Rho = rho; }

	/// <summary>The record's log drift at release age <paramref name="age"/>; 0 when off.</summary>
	internal static float LogDrift(string recordId, int age, ulong seed) {
		if (!Enabled || string.IsNullOrEmpty(recordId) || age < 1) return 0f;
		double innovation = Sigma * Math.Sqrt(1d - Rho * Rho), z = 0d, weight = 1d;
		for (int k = 0; k < Terms && age - k >= 1; k++, weight *= Rho)
			z += weight * innovation * Normal(seed, recordId, age - k);
		return (float)z;
	}

	/// <summary>This week's conversion multiplier for a live single; exactly 1 when off.</summary>
	public static float Multiplier(RecordRuntimeData record) {
		if (!Enabled || record?.baseRecord == null || record.baseRecord.format == ReleaseFormat.Album) return 1f;
		float z = LogDrift(record.baseRecord.recordId, record.weeksSinceRelease, RegionalSalesRandom.SavedSeedOrWorld);
		return (float)Math.Exp(Math.Clamp(z - 0.5 * Sigma * Sigma, -ClampLog, ClampLog));
	}

	private static double Normal(ulong seed, string recordId, int age) {
		double u1 = Math.Max(1e-12, PhiloxCounterRandom.Unit(seed, recordId, "drift", age, Decision));
		double u2 = PhiloxCounterRandom.Unit(seed, recordId, "drift", age, Decision ^ 0x1U);
		return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
	}
}
