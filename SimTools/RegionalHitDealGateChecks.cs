using System;
using System.Text.Json;
using Godot;

/// <summary>Fixed checks for RegionalHitDealGate (SaveLoadRoundTripRunner --regional-hit-deal-check).</summary>
public static class RegionalHitDealGateChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Regional hit deal gate: " + why); }
		bool was = RegionalHitDealGate.Enabled, wasSpread = WholesaleSpreadGate.Enabled;
		try {
			WholesaleSpreadGate.SetEnabledForChecks(false);
			RecordRuntimeData Single(string id, int age) => new(new Record { recordId = id, labelId = "gate-label",
				format = ReleaseFormat.Single }) { weeksSinceRelease = age };
			var client = new AILabel { labelId = "gate-label" };

			// A launch ramp: the peak crossed LocalTraction's .20 and never went further.
			var ramp = Single("gate-ramp", 6);
			ramp.regionalData["greatlakes"] = new RegionalRecordData("greatlakes") { peakBreakoutScore = .26f, breakoutScore = .12f };

			RegionalHitDealGate.SetEnabledForChecks(false);
			var data = new RegionalRecordData("eastcoast") { breakoutScore = .60f };
			RegionalHitDealGate.Observe(data, .90f, 5);
			Check(data.dealEvidenceAge == -1, "switched off, the breakout pass stamps nothing");
			var evidence = CompetitorManager.EvaluateRegionalDealEvidence(new[] { ramp }, "gate-label", null, .20f);
			Check(CompetitorManager.IsPullDealTrigger(client, evidence), "switched off, the running .20 peak is still evidence");

			RegionalHitDealGate.SetEnabledForChecks(true);
			evidence = CompetitorManager.EvaluateRegionalDealEvidence(new[] { ramp }, "gate-label", null, .20f);
			Check(!CompetitorManager.IsPullDealTrigger(client, evidence) && Mathf.IsEqualApprox(evidence.BestAnyRegionPeak, .26f),
				"switched on, a launch ramp's old peak is not deal evidence (the peak stays as telemetry)");

			data = new RegionalRecordData("eastcoast") { breakoutScore = .45f };
			RegionalHitDealGate.Observe(data, RegionalHitDealGate.VolumeBar - .01f, 5);
			Check(data.dealEvidenceAge == -1, "a .40 score without regional volume is not a hit");
			data.breakoutScore = RegionalHitDealGate.ScoreBar - .01f;
			RegionalHitDealGate.Observe(data, .90f, 5);
			Check(data.dealEvidenceAge == -1, "volume below the RegionalBreakout score is not a hit");
			data.breakoutScore = .45f;
			RegionalHitDealGate.Observe(data, RegionalHitDealGate.VolumeBar, 5);
			Check(data.dealEvidenceAge == 5, "score and volume at their bars stamp the record's age");
			RegionalHitDealGate.Observe(new RegionalRecordData("x") { breakoutScore = .10f }, .10f, 6);
			Check(RegionalHitDealGate.IsRecent(data, 5 + RegionalHitDealGate.RecentWeeks - 1) &&
				!RegionalHitDealGate.IsRecent(data, 5 + RegionalHitDealGate.RecentWeeks),
				"the evidence lasts one check interval and then lapses");

			// The old smash with a stale stamp versus a modest record that is selling now: the deal carries the latter.
			var oldSmash = Single("gate-old-smash", 30);
			oldSmash.peakRegionalBreakoutStrength = .90f;
			oldSmash.regionalData["westcoast"] = new RegionalRecordData("westcoast") { peakBreakoutScore = .90f,
				breakoutScore = .70f, dealEvidenceAge = 20 };
			var current = Single("gate-current", 8);
			current.regionalData["deepsouth"] = new RegionalRecordData("deepsouth") { peakBreakoutScore = .44f,
				breakoutScore = .42f, dealEvidenceAge = 7 };
			evidence = CompetitorManager.EvaluateRegionalDealEvidence(new[] { oldSmash, current, ramp }, "gate-label", null, .20f);
			Check(CompetitorManager.IsPullDealTrigger(client, evidence) && evidence.EarningRecordId == "gate-current",
				"a recent regional hit qualifies and is the record the deal covers, not the stale bigger peak");
			current.weeksSinceRelease = 7 + RegionalHitDealGate.RecentWeeks;
			evidence = CompetitorManager.EvaluateRegionalDealEvidence(new[] { oldSmash, current, ramp }, "gate-label", null, .20f);
			Check(!CompetitorManager.IsPullDealTrigger(client, evidence) && evidence.EarningRecordId == "gate-old-smash",
				"once every hit has lapsed there is no pull evidence, and courting falls back to the strongest record");

			// WholesaleSpreadGate reads the same stamp for its neighbour hop, so it must be written with only that switch on.
			RegionalHitDealGate.SetEnabledForChecks(false);
			WholesaleSpreadGate.SetEnabledForChecks(true);
			data = new RegionalRecordData("eastcoast") { breakoutScore = .45f };
			RegionalHitDealGate.Observe(data, .60f, 9);
			evidence = CompetitorManager.EvaluateRegionalDealEvidence(new[] { ramp }, "gate-label", null, .20f);
			Check(data.dealEvidenceAge == 9 && CompetitorManager.IsPullDealTrigger(client, evidence),
				"the slow-spread switch stamps hits for its hop without turning on the deal gate");
			Check(!WholesaleSpreadGate.IsSustained(new RegionalRecordData("x") { tractionWeeks = WholesaleSpreadGate.SustainedTractionWeeks - 1, peakBreakoutScore = .9f }) &&
				WholesaleSpreadGate.IsSustained(new RegionalRecordData("x") { tractionWeeks = WholesaleSpreadGate.SustainedTractionWeeks }),
				"a direct placement needs sustained traction in the market, not a peak it once touched");
			WholesaleSpreadGate.SetEnabledForChecks(false);
			data = new RegionalRecordData("eastcoast") { breakoutScore = .45f };
			RegionalHitDealGate.Observe(data, .60f, 9);
			Check(data.dealEvidenceAge == -1, "with both switches off nothing is stamped");
			RegionalHitDealGate.SetEnabledForChecks(true);

			var saved = JsonSerializer.Deserialize<RegionalRecordSaveData>(
				JsonSerializer.Serialize(RegionalRecordSaveData.From(new RegionalRecordData("greatlakes") { dealEvidenceAge = 11 })));
			Check(saved.ToRegional().dealEvidenceAge == 11, "the qualifying age survives save and load");
			var legacy = JsonSerializer.Deserialize<RegionalRecordSaveData>("{\"regionId\":\"greatlakes\"}");
			Check(legacy.ToRegional().dealEvidenceAge == -1, "an older save has no stamp");
			GD.Print("REGIONAL_HIT_DEAL_CHECK_PASS switch-off inert, ramp rejected, volume required, evidence lapses, deal covers the current hit, slow-spread stamp + sustained bar, save/load");
		} finally {
			RegionalHitDealGate.SetEnabledForChecks(was);
			WholesaleSpreadGate.SetEnabledForChecks(wasSpread);
		}
	}
}
