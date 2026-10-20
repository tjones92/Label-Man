using System;
using System.Text.Json;
using Godot;

public static class CareerBandedFormatMemoryChecks {
	public static void Run() {
		bool saved = CareerBandedFormatMemory.Enabled;
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Career-banded memory: " + why); checks++; }
		try {
			CareerBandedFormatMemory.SetForChecks(false);
			int legacyVersion = SaveGameService.WritingVersion;
			var memory = new FormatRevenueMemory();
			memory.observations.Add(new() { releaseId = "new", careerBand = 0, normalizedResidual = -.5f, maturityWeight = 1f });
			memory.observations.Add(new() { releaseId = "star", careerBand = 3, normalizedResidual = 1.5f, maturityWeight = 1f });
			memory.observations.Add(new() { releaseId = "legacy", normalizedResidual = 2f, maturityWeight = 1f });
			CompetitorManager manager = CompetitorManager.Instance;
			var pooled = manager.ResponsiveMemoryForProbe(memory, 10, 0);
			Check(Math.Abs(pooled.Residual - 1f) < .00001f,
				"off reads every observation, including untagged legacy history, in the original order");
			CareerBandedFormatMemory.SetForChecks(true);
			var debut = manager.ResponsiveMemoryForProbe(memory, 10, 0);
			var star = manager.ResponsiveMemoryForProbe(memory, 10, 3);
			Check(Math.Abs(debut.Residual + .5f) < .00001f && Math.Abs(star.Residual - 1.5f) < .00001f && debut.Confidence < pooled.Confidence,
				"career cohorts learn opposite residuals and confidence counts only their own evidence");
			Check(Math.Abs(manager.ResponsiveMemoryForProbe(memory, 10, 0).Residual + .5f) < .00001f,
				"same-week cache cannot reuse a different career band's summary");
			var unsupported = manager.ResponsiveMemoryForProbe(memory, 10, 2);
			Check(unsupported == (0f, 0f, 0f), "a band with no evidence stays at its prior rather than borrowing the pooled residual");
			Check(CompetitorManager.GetMemoryCareerBand(CareerState.Star, CareerState.NewSigning) == 0,
				"delayed projects retain the career context used to price the project");
			Check(CompetitorManager.GetMemoryCareerBand(CareerState.Star, CareerState.NewSigning, ProjectRecordRole.AlbumExtraction) == 3,
				"a later extraction uses its own launch context rather than its source album's old cohort");
			Check(CompetitorManager.GetMemoryCareerBand(CareerState.Declining, null) == 4,
				"legacy fallback careers do not teach the New/Unsigned memory cohort");
			var legacy = new FormatMemoryObservation { releaseId = "legacy" };
			string legacyJson = JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions);
			Check(!legacyJson.Contains("\"careerBand\"") &&
				JsonSerializer.Deserialize<FormatMemoryObservation>(legacyJson, SaveGameService.TestJsonOptions).careerBand == null,
				"untagged observations preserve the old JSON schema and load as unknown");
			legacy.careerBand = 3;
			var restored = JsonSerializer.Deserialize<FormatMemoryObservation>(
				JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.careerBand == 3 && SaveGameService.WritingVersion == 24,
				"tagged evidence round-trips and active banded worlds require the new reader version");
			CareerBandedFormatMemory.SetForChecks(false);
			Check(manager.ResponsiveMemoryForProbe(memory, 10, 3) == pooled && SaveGameService.WritingVersion == legacyVersion,
				"switch off restores pooled arithmetic and the existing writing version");
			using (var retained = new AILabel()) {
				retained.laneRevenueMemory[RevenueEstimatorLane.OrphanSingle] = memory;
				Check(CareerBandedFormatMemory.HasTaggedEvidence(new[] { retained }),
					"retained tagged history still requires the new schema when the switch is off");
			}
			GD.Print($"CAREER_BANDED_FORMAT_MEMORY_CHECK passed checks={checks}");
		} finally { CareerBandedFormatMemory.SetForChecks(saved); }
	}
}
