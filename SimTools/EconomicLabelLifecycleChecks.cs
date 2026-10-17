using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class EconomicLabelLifecycleChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Economic label lifecycle: " + why); }
		AILabel Firm(float cash, LabelTier tier = LabelTier.Small) => new() { cashReserves = cash, tier = tier,
			monthsActive = 24, consecutiveLossMonths = 24, status = LabelStatus.Dying };
		var local = Firm(10000f);
		for (int month = 0; month < 24; month++) {
			local.cashReserves -= 150f;
			EconomicLabelLifecycle.ReviewMonth(local, -150f, true);
			Check(local.IsActive && local.status == LabelStatus.Stable, "funded losses must consume runway, not trigger mortality");
		}
		local = Firm(1000f);
		for (int month = 0; month < 48; month++) {
			local.cashReserves += 100f;
			EconomicLabelLifecycle.ReviewMonth(local, 100f, true);
			Check(local.IsActive && local.top40Hits == 0 && local.numberOneHits == 0,
				"a profitable local label can persist without national chart hits");
		}
		var thin = Firm(800f);
		EconomicLabelLifecycle.ReviewMonth(thin, -400f, true);
		Check(thin.status == LabelStatus.Struggling && EconomicLabelLifecycle.CommitmentReserve(thin, true) == 1200f &&
			EconomicLabelLifecycle.CommitmentReserve(thin, false) == 800f && !thin.CanAffordToSign(1f),
			"recent burn must tighten real commitment budgets before bills go unpaid");
		float cash = thin.cashReserves;
		EconomicLabelLifecycle.ReviewMonth(thin, 400f, true);
		EconomicLabelLifecycle.ReviewMonth(thin, 400f, true);
		EconomicLabelLifecycle.ReviewMonth(thin, 400f, true);
		Check(thin.status == LabelStatus.Rising && thin.recentMonthlyCashFlows.Count == 3 && thin.cashReserves == cash,
			"recovery replaces stale losses; reviewing the budget must not grant money");
		foreach (LabelTier tier in Enum.GetValues<LabelTier>()) {
			var unpaid = Firm(-100f, tier);
			unpaid.outstandingWholesaleReceivables = 100000f;
			EconomicLabelLifecycle.ReviewMonth(unpaid, -100f, true);
			Check(unpaid.status == LabelStatus.Dying, "one missed review permits existing receipts to arrive");
			EconomicLabelLifecycle.ReviewMonth(unpaid, -100f, true);
			Check(unpaid.status == LabelStatus.Bankrupt, "unpaid bills must eventually close every tier; uncollected invoices are not cash");
		}
		var recovered = Firm(-100f);
		EconomicLabelLifecycle.ReviewMonth(recovered, -100f, true);
		recovered.cashReserves = 1000f;
		EconomicLabelLifecycle.ReviewMonth(recovered, 1100f, true);
		Check(recovered.insolventMonthEnds == 0 && recovered.IsActive, "collections must clear the missed-payment streak");
		var disabled = Firm(-100f);
		EconomicLabelLifecycle.ReviewMonth(disabled, -100f, false);
		EconomicLabelLifecycle.ReviewMonth(disabled, -100f, false);
		Check(disabled.status == LabelStatus.Dying, "the bankruptcy setting must still be honored");
		var player = Firm(-100f); player.isPlayerOwned = true; player.status = LabelStatus.Stable;
		EconomicLabelLifecycle.ReviewMonth(player, -100f, true);
		Check(player.status == LabelStatus.Stable && player.recentMonthlyCashFlows.Count == 0,
			"the player's credit line remains owned by PlayerDesk");
		var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(thin, SaveGameService.TestJsonOptions),
			SaveGameService.TestJsonOptions);
		Check(restored.recentMonthlyCashFlows.SequenceEqual(thin.recentMonthlyCashFlows) &&
			restored.insolventMonthEnds == thin.insolventMonthEnds, "budget history and payment grace must survive saves");
		var oldSave = JsonSerializer.Deserialize<AILabel>("{\"cashReserves\":1000,\"tier\":3}", SaveGameService.TestJsonOptions);
		Check(oldSave.recentMonthlyCashFlows.Count == 0 && oldSave.insolventMonthEnds == 0, "old saves have neutral new fields");
		var missed = Firm(-100f);
		EconomicLabelLifecycle.ReviewMonth(missed, -100f, true);
		var savedMissed = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(missed, SaveGameService.TestJsonOptions),
			SaveGameService.TestJsonOptions);
		EconomicLabelLifecycle.ReviewMonth(savedMissed, -100f, true);
		Check(savedMissed.status == LabelStatus.Bankrupt, "loading must not renew an exhausted payment grace");
		GD.Seed(7321); float expected = GD.Randf(); GD.Seed(7321);
		EconomicLabelLifecycle.ReviewMonth(local, 100f, true);
		Check(GD.Randf() == expected, "financial reviews must consume no random draws");

		// Opening cash: one shared table, one draw per label, every tier unchanged value-for-value.
		foreach ((LabelTier tier, float min, float max) in new[] { (LabelTier.Major, 50000f, 200000f), (LabelTier.MidTier, 15000f, 60000f),
			(LabelTier.Independent, 5000f, 20000f), (LabelTier.Boutique, 3000f, 15000f), (LabelTier.Small, 1000f, 8000f) }) {
			GD.Seed(9104); float legacy = (float)GD.RandRange(min, max); float legacyNext = GD.Randf();
			GD.Seed(9104); float shared = LabelStartingFinances.DrawCash(tier); float sharedNext = GD.Randf();
			Check(legacy == shared && legacyNext == sharedNext, $"{tier} opening cash reproduces its former draw exactly and consumes one draw");
		}
		GD.Print("ECONOMIC_LABEL_CHECK_PASS funded local businesses, budget contraction, recovery, unpaid bills, all tiers, settings, player boundary, save migration, RNG, opening cash");
	}
}
