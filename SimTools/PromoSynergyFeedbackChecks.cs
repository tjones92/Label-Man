using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PromoSynergyFeedbackChecks {
	public static void Run() {
		bool saved = PromoSynergyFeedback.Enabled;
		int checks = 0;
		void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Promo synergy feedback: " + why); checks++; }
		bool Near(double a, double b, double tolerance = 1e-5) => Math.Abs(a - b) <= tolerance * Math.Max(1d, Math.Abs(b));
		try {
			PromoSynergyFeedback.SetForChecks(false, false);
			// Estimator.
			Check(PromoSynergyFeedback.Estimate(Array.Empty<PromoSynergyEvidence>(), 100, 0) == (1f, 1f, 0f), "no evidence leaves both credits whole");
			PromoSynergyEvidence Final(int band, float credited, double recruited, float cannib, double suppressed, int week = 100) => new() {
				careerBand = band, creditedRecruitment = credited, modeledCannibalization = cannib, albumMarginPerUnit = 2f,
				finalWeek = week, realizedRecruitmentUnits = recruited, realizedSuppressedUnits = suppressed
			};
			var flops = Enumerable.Range(0, 10).Select(_ => Final(0, 1000f, 0d, 500f, 25d)).ToList();
			var newActs = PromoSynergyFeedback.Estimate(flops, 100, 0);
			// Pooled: ratio 0 at confidence 10/20 -> .5; band (same ten) shrinks .5 toward 0 at .5 -> .25.
			Check(Near(newActs.Recruitment, .25f) && Near(newActs.Cannibalization, .325f),
				"realized/modelled ratios shrink toward the pooled ratio, and that toward 1");
			var stars = PromoSynergyFeedback.Estimate(flops, 100, 3);
			Check(Near(stars.Recruitment, .5f) && stars.Weight == 0f, "a cohort with no evidence reads the pooled ratio");
			Check(Near(PromoSynergyFeedback.RecencyWeight(152, 100), .5f) && PromoSynergyFeedback.RecencyWeight(90, 100) == 1f,
				"evidence halves in weight every 52 weeks and never exceeds 1");
			var hot = new List<PromoSynergyEvidence> { Final(1, 100f, 1000d, 0f, 0d) };
			Check(PromoSynergyFeedback.Estimate(hot, 100, 1).Recruitment <= PromoSynergyFeedback.MaxRatio, "a realized windfall is clamped");
			var pending = new List<PromoSynergyEvidence> { new() { careerBand = 0, creditedRecruitment = 1000f, albumMarginPerUnit = 1f } };
			Check(PromoSynergyFeedback.Estimate(pending, 100, 0) == (1f, 1f, 0f), "unfinalized projects teach nothing");

			// Counterfactual identity in the album weekly update.
			var manager = CompetitorManager.Instance;
			AILabel label = manager.GetAllLabels().First(l => l.IsActive);
			var record = new Record { recordId = "promo-shadow-probe", format = ReleaseFormat.Album, hookStrength = .6f, productionQuality = .6f, primaryGenre = Genre.TraditionalPop };
			var same = new RecordRuntimeData(record) { awareness = .3f, promoShadowAwareness = .3f, promoShadowDemand = 0d };
			var lifted = new RecordRuntimeData(record) { awareness = .45f, promoShadowAwareness = .3f, promoShadowDemand = 0d };
			for (int age = 0; age < 20; age++) {
				same.weeksSinceRelease = lifted.weeksSinceRelease = age;
				AlbumSimulator.UpdateAlbum(same, label, .2f, 0f);
				AlbumSimulator.UpdateAlbum(lifted, label, .2f, 0f);
			}
			Check(same.promoShadowAwareness == same.awareness, "without a bonus the counterfactual is the actual path");
			Check(lifted.promoShadowAwareness < lifted.awareness &&
				Near((1d - lifted.promoShadowAwareness.Value) / (1d - lifted.awareness), (1d - .3d) / (1d - .45d), 1e-4),
				"a launch bonus persists as a constant ratio of awareness headroom");
			var data = new RegionalRecordData("probe") { awareness = .2f, promoShadowAwareness = .2f };
			AlbumSimulator.UpdateRegionalState(same, data);
			Check(data.promoShadowAwareness == data.awareness, "regional growth mirrors the actual path");
			var open = new RegionalRecordData("late") { awareness = .2f };
			lifted.currentPosition = 0;
			Check(Near(AlbumSimulator.CalculatePromoShadowAwareness(lifted, open), lifted.promoShadowAwareness.Value * .45f + .2f * .55f),
				"a region opened after launch has no bonus to remove");
			lifted.currentPosition = 5;
			Check(AlbumSimulator.CalculatePromoShadowAwareness(lifted, open) == .48f, "chart-position awareness floors apply to both paths");
			lifted.currentPosition = 0;

			// Demand: the counterfactual draws less, and never suppresses.
			MarketRegion region = ChartManager.Instance.GetAllRegions().First();
			GameDate date = TimeManager.Instance.CurrentDate;
			var regionalSame = new RegionalRecordData(region.regionId) { awareness = .3f, promoShadowAwareness = .3f, unitsInStores = 100000 };
			var regionalLifted = new RegionalRecordData(region.regionId) { awareness = .45f, promoShadowAwareness = .3f, unitsInStores = 100000 };
			same.promoShadowDemand = 0d; same.rawAlbumDemandBeforeCannibalization = 0d; same.cannibalizationSuppression = .2f;
			lifted.promoShadowDemand = 0d; lifted.rawAlbumDemandBeforeCannibalization = 0d;
			AlbumSimulator.CalculateRegionalSales(same, region, regionalSame, date.year, date.month, false, label);
			AlbumSimulator.CalculateRegionalSales(lifted, region, regionalLifted, date.year, date.month, false, label);
			Check(same.rawAlbumDemandBeforeCannibalization > 0d && Near(same.promoShadowDemand.Value, same.rawAlbumDemandBeforeCannibalization),
				"equal awareness gives equal counterfactual demand, before cannibalization");
			Check(lifted.promoShadowDemand < lifted.rawAlbumDemandBeforeCannibalization, "the bonus shows up as recruited demand");

			// Finalization at retirement, once.
			var project = new AlbumProject { projectId = "promo-shadow-project", promoPeakAtDrop = 30,
				promoFeedback = new PromoSynergyEvidence { careerBand = 1, creditedRecruitment = 900f, modeledCannibalization = 300f, albumMarginPerUnit = 2f } };
			lifted.baseRecord.labelId = label.labelId; lifted.suppressedAlbumDemand = 7d;
			manager.FinalizePromoSynergyEvidence(project, lifted);
			PromoSynergyEvidence evidence = project.promoFeedback;
			Check(evidence.finalWeek != null && Near(evidence.realizedRecruitmentUnits.Value, lifted.rawAlbumDemandBeforeCannibalization - lifted.promoShadowDemand.Value) &&
				evidence.realizedSuppressedUnits == 7d && Near(evidence.RealizedSuppression, 14d), "retirement records recruited and suppressed units at the decision margin");
			lifted.suppressedAlbumDemand = 99d; manager.FinalizePromoSynergyEvidence(project, lifted);
			Check(evidence.realizedSuppressedUnits == 7d, "evidence is finalized once");

			// Persistence.
			var legacy = new RecordRuntimeData(record);
			string legacyJson = JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions);
			Check(!legacyJson.Contains("promoShadow"), "records without a counterfactual keep the legacy schema");
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(JsonSerializer.Serialize(lifted, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.promoShadowAwareness == lifted.promoShadowAwareness && restored.promoShadowDemand == lifted.promoShadowDemand,
				"an in-flight counterfactual round-trips");
			Check(!JsonSerializer.Serialize(new RegionalRecordData("x"), SaveGameService.TestJsonOptions).Contains("promoShadow"),
				"regional data without a counterfactual keeps the legacy schema");
			Check(!JsonSerializer.Serialize(new AlbumProject(), SaveGameService.TestJsonOptions).Contains("promoFeedback"),
				"projects without evidence keep the legacy schema");
			var restoredProject = JsonSerializer.Deserialize<AlbumProject>(JsonSerializer.Serialize(project, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restoredProject.promoFeedback?.finalWeek == evidence.finalWeek &&
				restoredProject.promoFeedback.realizedRecruitmentUnits == evidence.realizedRecruitmentUnits &&
				restoredProject.promoFeedback.creditedRecruitment == 900f, "finalized evidence round-trips");

			// Live estimate reads world projects, and retained evidence requires v26.
			int week = ChartManager.Instance.GetCurrentChartWeek();
			Check(!manager.GetAlbumProjects().Any(p => p.promoFeedback != null) && manager.PromoSynergyScalesForProbe(0, week) == (1f, 1f, 0f) &&
				SaveGameService.WritingVersion != 26, "a world without evidence keeps the modelled credit and its save version");
			var worldEvidence = new AlbumProject { projectId = "promo-world-probe", promoFeedback = Final(2, 1000f, 0d, 500f, 0d, week) };
			var unfinished = new AlbumProject { projectId = "promo-world-pending", promoFeedback = new() { careerBand = 2, creditedRecruitment = 1000f } };
			try {
				manager.AddAlbumProjectForProbe(worldEvidence); manager.AddAlbumProjectForProbe(unfinished);
				var scaled = manager.PromoSynergyScalesForProbe(2, week);
				Check(Near(scaled.Recruitment, PromoSynergyFeedback.Estimate(new[] { worldEvidence.promoFeedback }, week, 2).Recruitment) && scaled.Recruitment < 1f,
					"the decision reads finalized world evidence for its cohort");
				Check(manager.PromoSynergyScalesForProbe(2, week + 52).Weight < scaled.Weight, "the cached estimate refreshes as evidence ages");
				Check(SaveGameService.WritingVersion == 26, "retained promo evidence requires v26 even with the switch off");
			} finally { manager.RemoveAlbumProjectForProbe(worldEvidence); manager.RemoveAlbumProjectForProbe(unfinished); }
			Check(manager.PromoSynergyScalesForProbe(2, week) == (1f, 1f, 0f), "removing the evidence restores the modelled credit");
			PromoSynergyFeedback.SetForChecks(true, true);
			Check(SaveGameService.WritingVersion == 26, "an enabled world writes v26");
			GD.Print($"PROMO_SYNERGY_FEEDBACK_CHECK passed checks={checks}");
		} finally { PromoSynergyFeedback.SetForChecks(saved); }
	}
}
