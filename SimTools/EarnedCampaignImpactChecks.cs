using System;
using Godot;

/// <summary>--earned-campaign-impact-check: off is the identity and writes nothing; on, a charting label's impact climbs
/// toward the ceiling, fades when it goes cold, never drops below its founding draw, and leaves Majors and the player alone.</summary>
public static class EarnedCampaignImpactChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Earned campaign impact: " + why); }
		float was = EarnedCampaignImpact.Ceiling, recordsWas = EarnedCampaignImpact.RecordsForFullShare;
		EarnedCampaignImpact.SetRecordsForChecks(16f);
		try {
			var indie = new AILabel { tier = LabelTier.Independent, marketingPower = 0.50f, budgetLevel = 0.40f };
			float founding = Mathf.Clamp(indie.marketingPower * (0.45f + indie.budgetLevel * 0.55f), 0f, 1f);
			EarnedCampaignImpact.SetCeilingForChecks(0f);
			Check(!EarnedCampaignImpact.Enabled, "ceiling 0 must read as off");
			EarnedCampaignImpact.Update(indie, 30);
			Check(indie.earnedCampaignShare == 0f, "off must write nothing");
			Check(ChartSimulator.GetCampaignImpact(indie) == founding, "off must return the founding impact bit-for-bit");

			EarnedCampaignImpact.SetCeilingForChecks(0.55f);
			Check(ChartSimulator.GetCampaignImpact(indie) == founding, "nothing earned must return the founding impact");
			EarnedCampaignImpact.Update(indie, 16);
			Check(Math.Abs(indie.earnedCampaignShare - EarnedCampaignImpact.MonthlyRate) < 1e-6f, "one month at full evidence moves a quarter of the way");
			for (int i = 0; i < 24; i++) EarnedCampaignImpact.Update(indie, 16);
			Check(Math.Abs(ChartSimulator.GetCampaignImpact(indie) - 0.55f) < 0.01f, "sustained charting must reach the ceiling");
			for (int i = 0; i < 24; i++) EarnedCampaignImpact.Update(indie, 4);
			Check(Math.Abs(indie.earnedCampaignShare - 0.25f) < 0.01f, "the share must settle at the evidence ratio (4/16)");
			for (int i = 0; i < 36; i++) EarnedCampaignImpact.Update(indie, 0);
			float cold = ChartSimulator.GetCampaignImpact(indie);
			Check(cold >= founding && cold - founding < 0.001f, "a cold label must fade back to, and never below, its founding impact");

			var major = new AILabel { tier = LabelTier.Major, marketingPower = 0.85f, budgetLevel = 0.90f };
			float majorFounding = ChartSimulator.GetCampaignImpact(major);
			for (int i = 0; i < 24; i++) EarnedCampaignImpact.Update(major, 40);
			Check(ChartSimulator.GetCampaignImpact(major) == majorFounding, "a label above the ceiling must be unchanged");

			var player = new AILabel { tier = LabelTier.Small, marketingPower = 0.30f, budgetLevel = 0.15f, isPlayerOwned = true };
			EarnedCampaignImpact.Update(player, 16);
			Check(player.earnedCampaignShare == 0f, "the player's label must never earn");
			// A lower evidence bar: 6 records buy the full share.
			EarnedCampaignImpact.SetRecordsForChecks(6f);
			var vj = new AILabel { tier = LabelTier.Independent, marketingPower = 0.50f, budgetLevel = 0.40f };
			for (int i = 0; i < 24; i++) EarnedCampaignImpact.Update(vj, 6);
			Check(Math.Abs(ChartSimulator.GetCampaignImpact(vj) - 0.55f) < 0.01f, "at R = 6, six records a year must reach the ceiling");
			GD.Print("EARNED_CAMPAIGN_IMPACT_CHECK passed");
		} finally {
			EarnedCampaignImpact.SetCeilingForChecks(was);
			EarnedCampaignImpact.SetRecordsForChecks(recordsWas);
		}
	}
}
