using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum SingleCapitalRiskPolicy { Log, Mean, Observe }

/// <summary>Opt-in v3: a candidate distribution, not a transferred dollar premium. Final contribution / ex-ante
/// contribution supplies dimensionless shocks, normalized to weighted mean one. Outcomes are today's expected
/// contribution times each shock, less today's unpaid commitments. Mean, loss floor and utility use one cost basis.
/// Four effective finalized own-label observations in the same launch career band are required; sparse/old saves
/// explicitly fall back to the mean gate. Neither that threshold nor log utility is historically calibrated.</summary>
public static class SingleCapitalRisk {
	public const int ModelVersion = 3;
	public const string Switch = "--single-capital-risk=";
	public const string PolicySwitch = "--single-capital-risk-policy=";
	internal const double MinimumEffectiveSamples = 4;
	public static float CapitalFraction { get; private set; }
	public static SingleCapitalRiskPolicy Policy { get; private set; }
	public static bool Enabled => CapitalFraction > 0;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		string[] values = args.Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Use one " + Switch + " value.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0][Switch.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float fraction) ||
				!float.IsFinite(fraction) || fraction <= 0 || fraction > 1)
				throw new ArgumentException(Switch + " requires a capital fraction greater than 0 and at most 1.");
			if (!args.Contains("--label-release-slate") || !args.Contains("--label-release-capacity"))
				throw new ArgumentException(Switch + " requires --label-release-slate and --label-release-capacity.");
			CapitalFraction = fraction;
		}
		string[] policies = args.Where(a => a.StartsWith(PolicySwitch, StringComparison.Ordinal)).ToArray();
		if (policies.Length > 1 || policies.Length == 1 && !Enabled) throw new ArgumentException("A single risk policy requires " + Switch);
		Policy = policies.Length == 0 ? SingleCapitalRiskPolicy.Log : policies[0][PolicySwitch.Length..] switch {
			"log" => SingleCapitalRiskPolicy.Log, "mean" => SingleCapitalRiskPolicy.Mean, "observe" => SingleCapitalRiskPolicy.Observe,
			_ => throw new ArgumentException(PolicySwitch + " requires log, mean or observe.")
		};
		configured = true;
	}
	internal static void ResetForChecks() { configured = false; CapitalFraction = 0; Policy = SingleCapitalRiskPolicy.Log; }
	internal static void SetForChecks(float fraction, SingleCapitalRiskPolicy policy = SingleCapitalRiskPolicy.Log) {
		configured = true; CapitalFraction = fraction; Policy = policy;
	}

	public readonly record struct Candidate(double ExpectedContribution, double Recording, double Marketing, double Pressing) {
		public double FutureCost => Recording + Marketing + Pressing;
		public double Mean => ExpectedContribution - FutureCost;
	}
	/// <summary>The existing net forecast includes the estimated recording charge and initial pressing losses.
	/// Add those back to recover contribution, then subtract only unpaid costs. Prepared production is sunk.
	/// A forecast below the zero-contribution floor is explicitly clamped and reported by the caller.</summary>
	internal static Candidate FromForecast(double forecastBeforeCampaign, double recordingEstimate, double marketing,
		double initialPressing, bool productionPaid) => new(Math.Max(0, forecastBeforeCampaign + recordingEstimate + initialPressing),
		productionPaid ? 0 : recordingEstimate, marketing, initialPressing);

	public readonly record struct Outcome(double Net, double Probability);
	public sealed record Distribution(Candidate Candidate, Outcome[] Outcomes, int Samples, double Weight,
		double EffectiveSamples, double RawMultiplierMean, string Status) {
		public bool Ready => Status == "empirical";
		public double Mean => Outcomes.Sum(o => o.Net * o.Probability);
	}
	public readonly record struct Assessment(int Samples, double Weight, double EffectiveSamples, bool Ready, string Status,
		double Mean, double DistributionMean, double MeanError, double LossFloor, double MinimumNet, double MaximumNet,
		double LossProbability, double ExhaustionProbability, double Premium, double AdjustedNet, bool MeanCommit, bool LogCommit, bool Commit);

	internal static Distribution Build(IEnumerable<FormatMemoryObservation> observations, int week, Candidate candidate, int careerBand) {
		if (!double.IsFinite(candidate.ExpectedContribution) || !double.IsFinite(candidate.FutureCost) ||
			candidate.ExpectedContribution < 0 || candidate.Recording < 0 || candidate.Marketing < 0 || candidate.Pressing < 0)
			throw new ArgumentException("Candidate contributions and unpaid costs must be finite and nonnegative.");
		var shocks = new List<(double Ratio, double Weight)>();
		double weight = 0, squared = 0, numerator = 0;
		foreach (var o in observations ?? Array.Empty<FormatMemoryObservation>()) {
			int age = week - o.releaseWeek;
			if (!o.finalized || o.estimatorLane != RevenueEstimatorLane.OrphanSingle || age < 0 || age > 104 ||
				o.careerBand != careerBand || o.capitalRiskContribution is not float contribution ||
				o.capitalRiskExpectedContribution is not float expected || !float.IsFinite(contribution) || !float.IsFinite(expected) ||
				contribution < 0 || expected <= 0) continue;
			double w = Math.Pow(.5, age / 52.0), ratio = (double)contribution / expected;
			shocks.Add((ratio, w)); weight += w; squared += w * w; numerator += w * ratio;
		}
		double effective = squared > 0 ? weight * weight / squared : 0;
		double rawMean = weight > 0 ? numerator / weight : 0;
		string status = effective + 1e-10 < MinimumEffectiveSamples ? "insufficient-history" : rawMean <= 0 ? "zero-contribution-history" : "empirical";
		// A deterministic mean is a declared fallback, not an estimated zero loss probability. Ready distinguishes it.
		if (status != "empirical") return new(candidate, new[] { new Outcome(candidate.Mean, 1) }, shocks.Count, weight, effective, rawMean, status);
		var outcomes = shocks.Select(s => new Outcome(candidate.ExpectedContribution * (s.Ratio / rawMean) - candidate.FutureCost,
			s.Weight / weight)).ToArray();
		return new(candidate, outcomes, shocks.Count, weight, effective, rawMean, status);
	}

	internal static Assessment Evaluate(Distribution distribution, double spendableCapital) {
		double capital = Math.Max(0, spendableCapital) * CapitalFraction;
		double mean = distribution.Candidate.Mean, loss = 0, exhaust = 0, logs = 0;
		foreach (var o in distribution.Outcomes) {
			if (o.Net < 0) loss += o.Probability;
			if (capital + o.Net <= 0) exhaust += o.Probability;
			else if (capital > 0) logs += o.Probability * LogOnePlus(o.Net / capital);
		}
		// Exhaustion is a veto; -capital is the finite display value for zero terminal risk capital.
		double ce = !distribution.Ready ? mean : capital > 0 && exhaust == 0 ? capital * ExpMinusOne(logs) : -capital;
		bool meanCommit = mean > 0;
		bool logCommit = meanCommit && (!distribution.Ready || capital > 0 && exhaust == 0 && ce > 0);
		bool commit = Policy == SingleCapitalRiskPolicy.Observe || (Policy == SingleCapitalRiskPolicy.Mean ? meanCommit : logCommit);
		return new(distribution.Samples, distribution.Weight, distribution.EffectiveSamples, distribution.Ready, distribution.Status,
			mean, distribution.Mean, distribution.Mean - mean, -distribution.Candidate.FutureCost,
			distribution.Outcomes.Min(o => o.Net), distribution.Outcomes.Max(o => o.Net), Math.Clamp(loss, 0, 1), Math.Clamp(exhaust, 0, 1),
			Math.Max(0, mean - ce), ce, meanCommit, logCommit, commit);
	}
	private static double LogOnePlus(double x) => Math.Abs(x) < 1e-4 ? x * (1 + x * (-.5 + x * (1.0 / 3 - x * .25))) : Math.Log(1 + x);
	private static double ExpMinusOne(double x) => Math.Abs(x) < 1e-5 ? x * (1 + x * (.5 + x * (1.0 / 6 + x / 24))) : Math.Exp(x) - 1;
}
