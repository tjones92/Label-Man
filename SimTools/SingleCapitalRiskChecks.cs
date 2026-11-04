using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SingleCapitalRiskChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Single capital risk: " + name); checks++; }
		bool Near(double a, double b, double tolerance = 1e-8) => Math.Abs(a - b) <= tolerance * Math.Max(1, Math.Max(Math.Abs(a), Math.Abs(b)));
		float saved = SingleCapitalRisk.CapitalFraction;
		var savedPolicy = SingleCapitalRisk.Policy;
		try {
			string[] Args(string fraction, string policy = "log") => new[] { SingleCapitalRisk.Switch + fraction,
				"--label-release-slate", "--label-release-capacity", SingleCapitalRisk.PolicySwitch + policy };
			bool Rejects(params string[] args) {
				try { SingleCapitalRisk.ResetForChecks(); SingleCapitalRisk.Configure(args); return false; }
				catch (ArgumentException) { return true; }
			}
			foreach (string bad in new[] { "0", "-1", "NaN", "Infinity", "2", "x" }) Check(Rejects(Args(bad)), "invalid fraction " + bad);
			Check(Rejects(SingleCapitalRisk.Switch + "1"), "programme required");
			Check(Rejects(SingleCapitalRisk.PolicySwitch + "mean"), "policy requires probe");
			Check(Rejects(Args("1", "unknown")), "invalid policy");
			Check(Rejects(Args("1").Concat(new[] { SingleCapitalRisk.Switch + ".5" }).ToArray()), "duplicate fraction");
			SingleCapitalRisk.ResetForChecks(); SingleCapitalRisk.Configure(Array.Empty<string>());
			Check(!SingleCapitalRisk.Enabled, "off by default");
			SingleCapitalRisk.ResetForChecks(); SingleCapitalRisk.Configure(Args("1"));
			Check(SingleCapitalRisk.CapitalFraction == 1 && SingleCapitalRisk.Policy == SingleCapitalRiskPolicy.Log, "valid log configuration");

			FormatMemoryObservation O(float contribution, float prior = 1000) => new() { finalized = true, careerBand = 0,
				releaseWeek = 52, estimatorLane = RevenueEstimatorLane.OrphanSingle,
				capitalRiskContribution = contribution, capitalRiskExpectedContribution = prior };
			var list = new List<FormatMemoryObservation> { O(100), O(100), O(2100), O(2100) };
			var candidate = new SingleCapitalRisk.Candidate(1100, 800, 100, 100);
			SingleCapitalRisk.Distribution Build(SingleCapitalRisk.Candidate? c = null) => SingleCapitalRisk.Build(list, 52, c ?? candidate, 0);
			var d = Build();
			Check(d.Ready && Near(d.EffectiveSamples, 4), "four effective final observations");
			Check(Near(d.Mean, 100) && Near(d.Outcomes.Sum(o => o.Probability), 1), "normalized probabilities and candidate mean");
			Check(Near(d.Outcomes.Min(o => o.Net), -900) && Near(d.Outcomes.Max(o => o.Net), 1100), "candidate-scaled outcomes");
			var poor = SingleCapitalRisk.Evaluate(d, 1000);
			var rich = SingleCapitalRisk.Evaluate(d, 1e10);
			Check(poor.MeanCommit && !poor.LogCommit && rich.LogCommit, "positive mean, thin versus deep capital");
			Check(Near(poor.LossProbability, .5) && Near(poor.Mean, poor.DistributionMean), "loss probability shares the forecast distribution");
			Check(rich.Premium < .001 && rich.Premium < poor.Premium, "deep capital approaches the mean");
			Check(!SingleCapitalRisk.Evaluate(d, 900).LogCommit && Near(SingleCapitalRisk.Evaluate(d, 900).ExhaustionProbability, .5), "exhaustion veto");
			Check(!SingleCapitalRisk.Evaluate(d, 0).LogCommit, "zero capital cannot take a supported risky bet");
			SingleCapitalRisk.SetForChecks(.25f);
			Check(!SingleCapitalRisk.Evaluate(d, 4000).LogCommit, "fraction scales capital");
			SingleCapitalRisk.SetForChecks(1, SingleCapitalRiskPolicy.Mean);
			Check(SingleCapitalRisk.Evaluate(d, 1000).Commit, "mean policy accepts the same positive mean");
			SingleCapitalRisk.SetForChecks(1, SingleCapitalRiskPolicy.Observe);
			Check(SingleCapitalRisk.Evaluate(Build(new(0, 800, 100, 100)), 1000).Commit, "observe never vetoes");
			SingleCapitalRisk.SetForChecks(1);

			// A historical change in dollars or prior level cannot change a normalized candidate distribution.
			var scaled = list.Select(o => O(o.capitalRiskContribution.Value * 100, o.capitalRiskExpectedContribution.Value * 100)).ToArray();
			var scaledD = SingleCapitalRisk.Build(scaled, 52, candidate, 0);
			Check(d.Outcomes.SequenceEqual(scaledD.Outcomes), "historical dollar scale is not transferred");
			var shiftedMean = Build(new(10000, 800, 100, 100));
			Check(Near(shiftedMean.Mean, 9000), "changing candidate mean changes the actual distribution mean");
			var profitable = Build(new(1100, 10, 5, 5));
			var profitableRisk = SingleCapitalRisk.Evaluate(profitable, 30);
			Check(profitableRisk.LossProbability == 0 && profitableRisk.LogCommit && profitableRisk.AdjustedNet > 0,
				"all candidate outcomes profitable cannot be rejected by a transferred premium");

			// The same contribution forecast at two funding stages changes only the unpaid recording cost.
			var fresh = SingleCapitalRisk.FromForecast(900, 800, 100, 100, false);
			var prepared = SingleCapitalRisk.FromForecast(900, 800, 100, 100, true);
			Check(fresh.ExpectedContribution == prepared.ExpectedContribution && prepared.Recording == 0, "prepared contribution unchanged, production sunk");
			Check(Near(prepared.Mean - fresh.Mean, 800) && prepared.FutureCost == 200, "paid production removed exactly once");
			Check(Near(fresh.Mean, 800), "unprepared mean keeps the original net forecast less campaign");
			Check(SingleCapitalRisk.FromForecast(-9999, 800, 100, 100, false).ExpectedContribution == 0, "impossible forecast constrained to zero contribution");

			foreach (double meanContribution in new[] { 0.0, 10, 1100, 10000 }) {
				foreach (double cost in new[] { 0.0, 20, 1000, 20000 }) {
					var test = Build(new(meanContribution, cost * .7, cost * .1, cost * .2));
					Check(Near(test.Mean, meanContribution - cost), "mean conserved across candidate size and cost");
					Check(test.Outcomes.All(o => o.Net >= -cost - 1e-8), "candidate loss bounded by unpaid costs");
					foreach (double cash in new[] { 1.0, 1000, 1e8 }) {
						var risk = SingleCapitalRisk.Evaluate(test, cash);
						Check(risk.LossProbability >= 0 && risk.LossProbability <= 1 && risk.ExhaustionProbability <= risk.LossProbability,
							"valid downside probabilities");
						if (risk.ExhaustionProbability == 0)
							Check(risk.AdjustedNet <= risk.Mean + 1e-5, "Jensen bound on certainty equivalent");
						if (risk.LogCommit) Check(risk.MeanCommit, "log utility never accepts a nonpositive mean");
					}
				}
			}

			var excluded = new List<FormatMemoryObservation> { O(100) };
			excluded[0].finalized = false;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "provisional evidence excluded");
			excluded[0].finalized = true; excluded[0].releaseWeek = -53;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "old evidence excluded");
			excluded[0].releaseWeek = 53;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "future evidence excluded");
			excluded[0].releaseWeek = 52; excluded[0].careerBand = 1;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "other career band excluded");
			excluded[0].careerBand = 0; excluded[0].capitalRiskExpectedContribution = null;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "old saves without basis excluded");
			excluded[0].capitalRiskExpectedContribution = 1000; excluded[0].capitalRiskContribution = -1;
			Check(SingleCapitalRisk.Build(excluded, 52, candidate, 0).Samples == 0, "negative variable contribution not silently clipped");
			var empty = SingleCapitalRisk.Build(null, 52, candidate, 0);
			Check(!empty.Ready && empty.Status == "insufficient-history" && SingleCapitalRisk.Evaluate(empty, 1000).Commit, "sparse fallback declared and mean neutral");
			Check(!SingleCapitalRisk.Evaluate(SingleCapitalRisk.Build(null, 52, new(1, 100, 0, 0), 0), 1000).Commit, "sparse fallback preserves negative mean rejection");
			var zeros = SingleCapitalRisk.Build(new[] { O(0), O(0), O(0), O(0) }, 52, candidate, 0);
			Check(!zeros.Ready && zeros.Status == "zero-contribution-history", "all-zero contributions cannot invent a positive-mean shape");
			var aged = list.Select(o => O(o.capitalRiskContribution.Value)).ToList(); aged[0].releaseWeek = 0;
			var aging = SingleCapitalRisk.Build(aged, 52, candidate, 0);
			Check(Near(aging.Weight, 3.5) && aging.EffectiveSamples < 4 && !aging.Ready, "recency and effective sample threshold");

			var options = SaveGameService.TestJsonOptions;
			string json = JsonSerializer.Serialize(list, options);
			var restored = JsonSerializer.Deserialize<List<FormatMemoryObservation>>(json, options);
			Check(SingleCapitalRisk.Build(restored, 52, candidate, 0).Outcomes.SequenceEqual(d.Outcomes), "saved final evidence reproduces distribution");
			var runtime = new RecordRuntimeData(new Record()) { capitalRiskExpectedContribution = 1234 };
			var runtimeSave = RuntimeRecordSaveData.From(runtime);
			Check(runtimeSave.ToRuntime().capitalRiskExpectedContribution == 1234, "live record forecast basis round-trips");
			Check(!JsonSerializer.Serialize(new FormatMemoryObservation(), options).Contains("capitalRisk"), "off observation schema unchanged");
			Check(!JsonSerializer.Serialize(RuntimeRecordSaveData.From(new RecordRuntimeData(new Record())), options).Contains("capitalRisk"),
				"off live record schema unchanged");
			Integration(Check);
		} finally { SingleCapitalRisk.SetForChecks(saved, savedPolicy); }
		GD.Print($"SINGLE_CAPITAL_RISK_CHECK passed checks={checks} model={SingleCapitalRisk.ModelVersion}");
	}

	private static void Integration(Action<bool, string> Check) {
		var cm = CompetitorManager.Instance;
		var label = cm.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive && l.tier == LabelTier.Major && l.roster.Count > 0);
		var artist = label.roster.First(a => a.members?.Count > 0);
		float cash = label.cashReserves;
		var career = artist.careerState;
		string[] args = OS.GetCmdlineUserArgs();
		var memories = label.laneRevenueMemory;
		var state = cm.ProgrammeState;
		SingleCapitalRiskTelemetry latest = null;
		void Observe(SingleCapitalRiskTelemetry t) => latest = t;
		cm.OnSingleCapitalRisk += Observe;
		try {
			SingleCapitalRisk.SetForChecks(1, SingleCapitalRiskPolicy.Observe);
			LabelReleaseProgramme.Configure(new[] { "--label-release-slate", "--label-release-capacity" });
			cm.SetProgrammeForChecks(state ?? new());
			label.laneRevenueMemory = new();
			label.cashReserves = 1e8f;
			artist.careerState = CareerState.NewSigning;
			var item = new ReleaseSlateItem { LabelId = label.labelId, ArtistId = artist.artistId, Format = ReleaseFormat.Single, Id = "risk-native" };
			Check(cm.ReserveSlate(label, artist, item, TimeManager.Instance.CurrentDate, out _), "real funding path accepts observe");
			double newPrior = latest.Candidate.ExpectedContribution;
			artist.careerState = CareerState.Superstar;
			Check(cm.ReserveSlate(label, artist, item, TimeManager.Instance.CurrentDate, out _) && latest.Candidate.ExpectedContribution > newPrior,
				"funding prior reads the actual career band rather than the default bucket");
			Check(!latest.Prepared && latest.Candidate.Recording > 0 && latest.Candidate.Pressing >= 0, "funding candidate includes unpaid recording and pressing");
			item.PreparedRecord = new Record { format = ReleaseFormat.Single };
			Check(cm.ReserveSlate(label, artist, item, TimeManager.Instance.CurrentDate, out _) && latest.Prepared && latest.Candidate.Recording == 0,
				"prepared master also assessed without recharging recording");
			label.cashReserves = 0; latest = null;
			Check(!cm.ReserveSlate(label, artist, item, TimeManager.Instance.CurrentDate, out _) && latest == null,
				"ordinary cash failure is not counted as a risk assessment");
		} finally {
			cm.OnSingleCapitalRisk -= Observe; label.cashReserves = cash; artist.careerState = career;
			label.laneRevenueMemory = memories; cm.SetProgrammeForChecks(state);
			LabelReleaseProgramme.Configure(args);
		}
	}
}
