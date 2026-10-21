using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Native checks for the economy audit's B3-B7 and section C switches. Run with --economy-audit-b-check.
/// Every switch is restored to its configured value afterwards.
/// </summary>
public static class EconomyAuditBChecks {
	private static int checks;
	private static void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Economy audit B/C: " + why); checks++; }
	private static bool Near(double a, double b, double tolerance = 1e-4) => Math.Abs(a - b) <= tolerance * Math.Max(1d, Math.Abs(b));

	public static void Run() {
		checks = 0;
		DistributionAdvance();
		RoyaltyTiers();
		RecordCampaigns();
		AiPublishing();
		Renewal();
		SectionC();
		Godot.GD.Print($"ECONOMY_AUDIT_B_CHECK passed checks={checks}");
	}

	private static void DistributionAdvance() {
		bool saved = DistributionAdvanceRecoupment.Enabled;
		var manager = CompetitorManager.Instance;
		List<AILabel> labels = manager.GetAllLabels().Where(l => l.IsActive && !l.isPlayerOwned).Take(3).ToList();
		AILabel client = labels[0], distributor = labels[1], major = labels[2];
		var savedState = labels.Select(l => (l, l.cashReserves, l.activeDeal)).ToList();
		DistributionDeal Deal(AILabel owner, float balance) => new() {
			distributorId = owner.labelId, marginSkim = .2f, advance = balance, unrecoupedAdvance = balance, grantedRegions = Array.Empty<string>()
		};
		try {
			var deal = Deal(distributor, 1000f);
			Check(DistributionAdvanceRecoupment.CarriedShare(deal, .2f) == 1f && Near(DistributionAdvanceRecoupment.CarriedShare(deal, .05f), .25f)
				&& DistributionAdvanceRecoupment.CarriedShare(deal, 0f) == 0f, "the carried share is the skim's granted-region fraction");
			Check(DistributionAdvanceRecoupment.Withhold(1000f, 300f, 500f) == 300f && DistributionAdvanceRecoupment.Withhold(1000f, 300f, 100f) == 100f
				&& DistributionAdvanceRecoupment.Withhold(200f, 300f, 500f) == 200f && DistributionAdvanceRecoupment.Withhold(200f, 300f, -50f) == 0f,
				"withholding is capped by the balance, the carried receipts and the banked revenue");
			Check(DistributionAdvanceRecoupment.ExitSettlement(1000f, 5000f, 1000f) == 1000f && DistributionAdvanceRecoupment.ExitSettlement(1000f, 3500f, 1000f) == 500f
				&& DistributionAdvanceRecoupment.ExitSettlement(1000f, 2000f, 1000f) == 0f, "an exit pays from cash above three months' overhead");

			DistributionAdvanceRecoupment.SetForChecks(false);
			client.activeDeal = deal;
			manager.SettleEndedDistributionAdvance(client, deal, DealResolution.Exit);
			Check(deal.unrecoupedAdvance == 1000f, "off, an ended deal's balance is left as it was");

			DistributionAdvanceRecoupment.SetForChecks(true);
			float clientCash = client.cashReserves = 50000f, distributorCash = distributor.cashReserves = 50000f;
			float withheld = manager.WithholdDistributionAdvance(client, 300f, 400f);
			Check(withheld == 300f && deal.unrecoupedAdvance == 700f && distributor.cashReserves == distributorCash + 300f
				&& client.cashReserves == clientCash, "the distributor keeps the carried receipts; the caller takes them off the client's cash");
			Check(manager.WithholdDistributionAdvance(client, 0f, 400f) == 0f, "uncarried receipts are never withheld");

			float overhead = client.GetMonthlyOverhead();
			client.cashReserves = overhead * 3f + 250f; distributorCash = distributor.cashReserves;
			manager.SettleEndedDistributionAdvance(client, deal, DealResolution.Exit);
			Check(deal.unrecoupedAdvance == 0f && Near(client.cashReserves, overhead * 3f) && Near(distributor.cashReserves, distributorCash + 250f),
				"an exit settles what the client can spare and writes off the rest");

			foreach (DealResolution resolution in new[] { DealResolution.Dropped, DealResolution.Absorb, DealResolution.DistributorCollapsed, DealResolution.ClientClosed }) {
				var forgiven = Deal(distributor, 600f);
				clientCash = client.cashReserves; distributorCash = distributor.cashReserves;
				manager.SettleEndedDistributionAdvance(client, forgiven, resolution);
				Check(forgiven.unrecoupedAdvance == 0f && client.cashReserves == clientCash && distributor.cashReserves == distributorCash,
					resolution + " forgives the balance without moving cash");
			}

			var current = Deal(distributor, 800f);
			var offer = Deal(major, 2000f);
			float majorCash = major.cashReserves = 50000f; distributorCash = distributor.cashReserves;
			manager.BuyOutDistributionAdvance(client, current, distributor, offer, major);
			Check(current.unrecoupedAdvance == 0f && offer.unrecoupedAdvance == 2800f && offer.advance == 2000f
				&& major.cashReserves == majorCash - 800f && distributor.cashReserves == distributorCash + 800f,
				"a poaching Major pays the incumbent and adds the balance to its own advance");
		} finally {
			foreach (var (label, cash, activeDeal) in savedState) { label.cashReserves = cash; label.activeDeal = activeDeal; }
			DistributionAdvanceRecoupment.SetForChecks(saved);
		}
	}

	private static void RoyaltyTiers() {
		bool saved = PeriodRoyaltyTiers.Enabled;
		var labels = CompetitorManager.Instance.GetAllLabels();
		AILabel Of(LabelTier tier) => labels.FirstOrDefault(l => l.tier == tier);
		AILabel majorLabel = Of(LabelTier.Major), small = Of(LabelTier.Small);
		SimulatedArtist act = ArtistManager.Instance.GetAllArtists().First();
		CareerState savedState = act.careerState;
		try {
			PeriodRoyaltyTiers.SetForChecks(false);
			act.careerState = CareerState.NewSigning;
			Check(majorLabel.CalculateRoyaltyRate(act) == .03f && small.CalculateRoyaltyRate(act) == .10f, "off, the authored tier rates stand");
			PeriodRoyaltyTiers.SetForChecks(true);
			Check(majorLabel.CalculateRoyaltyRate(act) > small.CalculateRoyaltyRate(act), "on, a Major outpays a Small label for a new act");
			act.careerState = CareerState.Superstar;
			Check(Near(majorLabel.CalculateRoyaltyRate(act), .12f) && Near(small.CalculateRoyaltyRate(act), .025f + .08f * .4f),
				"a star's leverage climbs fastest at the label that can pay it");
			Check(Enum.GetValues<LabelTier>().All(t => PeriodRoyaltyTiers.BaseRate(t) >= .02f && PeriodRoyaltyTiers.BaseRate(t) + .08f * PeriodRoyaltyTiers.LeverageShare(t) <= .15f),
				"every tier stays inside the contract clamp");
		} finally { act.careerState = savedState; PeriodRoyaltyTiers.SetForChecks(saved); }
	}

	private static void RecordCampaigns() {
		bool saved = RecordCampaignSpend.Enabled;
		var manager = CompetitorManager.Instance;
		try {
			Check(RecordCampaignSpend.LaunchFactor(1000f, 1000f) == 1f && Near(RecordCampaignSpend.LaunchFactor(4000f, 1000f), 1.6f)
				&& RecordCampaignSpend.LaunchFactor(100f, 1000f) == .6f && Near(RecordCampaignSpend.LaunchFactor(2000f, 1000f), MathF.Sqrt(2f)),
				"a record's launch factor is the square root of its budget against the label's standard, clamped");
			var probe = new RecordRuntimeData(new Record { recordId = "campaign-probe", format = ReleaseFormat.Single });
			Check(RecordCampaignSpend.PushFactor(probe) == 1f && RecordCampaignSpend.PushFactor(null) == 1f, "a record without a campaign factor is pushed as before");
			probe.weeksSinceRelease = 4; probe.currentPosition = 30; probe.lastWeekPosition = 45;
			Check(RecordCampaignSpend.IsBreaking(probe), "a climbing Top 40 single inside the window is breaking");
			probe.currentPosition = 50; Check(!RecordCampaignSpend.IsBreaking(probe), "outside the Top 40 is not breaking");
			probe.currentPosition = 30; probe.lastWeekPosition = 20; Check(!RecordCampaignSpend.IsBreaking(probe), "a falling single is not breaking");
			probe.weeksSinceRelease = 6; probe.peakPosition = 0; Check(RecordCampaignSpend.IsDead(probe), "uncharted by week 6 is dead");
			probe.peakPosition = 90; Check(!RecordCampaignSpend.IsDead(probe), "a single that charted is not dead");
			string legacyJson = JsonSerializer.Serialize(new RecordRuntimeData(new Record { recordId = "legacy" }), SaveGameService.TestJsonOptions);
			Check(!legacyJson.Contains("campaignPushFactor") && !legacyJson.Contains("campaignTranches"), "records without a campaign keep the legacy schema");
			probe.campaignPushFactor = 1.3f; probe.campaignTranches = 1;
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(JsonSerializer.Serialize(probe, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.campaignPushFactor == 1.3f && restored.campaignTranches == 1, "a record's campaign round-trips");

			// Live: a breaking AI single gets a tranche, paid from cash and attributed; a dead one is cut.
			RecordCampaignSpend.SetForChecks(true);
			AILabel label = manager.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && manager.GetLabelActiveRecordIdsForChecks(l.labelId)
				.Select(id => ChartManager.Instance.GetRecordRuntimeData(id)).Count(r => r?.baseRecord.format == ReleaseFormat.Single) >= 2);
			List<RecordRuntimeData> singles = manager.GetLabelActiveRecordIdsForChecks(label.labelId)
				.Select(id => ChartManager.Instance.GetRecordRuntimeData(id)).Where(r => r?.baseRecord.format == ReleaseFormat.Single).Take(2).ToList();
			RecordRuntimeData hot = singles[0], cold = singles[1];
			var savedRecords = singles.Select(r => (r, r.weeksSinceRelease, r.currentPosition, r.lastWeekPosition, r.peakPosition, r.campaignPushFactor, r.campaignTranches, r.sunkMarketingCost)).ToList();
			float savedCash = label.cashReserves;
			var others = manager.GetLabelActiveRecordIdsForChecks(label.labelId).Select(id => ChartManager.Instance.GetRecordRuntimeData(id))
				.Where(r => r != null && !singles.Contains(r)).Select(r => (r, r.campaignPushFactor)).ToList();
			try {
				foreach (var (other, _) in others) other.campaignPushFactor = null;
				hot.weeksSinceRelease = 3; hot.currentPosition = 25; hot.lastWeekPosition = 40; hot.peakPosition = 25;
				hot.campaignPushFactor = 1f; hot.campaignTranches = 0; hot.sunkMarketingCost = 100f;
				cold.weeksSinceRelease = 7; cold.currentPosition = 0; cold.lastWeekPosition = 0; cold.peakPosition = 0;
				cold.campaignPushFactor = 1f; cold.campaignTranches = 0;
				label.cashReserves = label.GetMonthlyOverhead() * 3f + 1_000_000f;
				float cash = label.cashReserves;
				SimulatedArtist artist = ArtistManager.Instance.GetArtist(hot.baseRecord.artistId);
				float tranche = (artist != null ? label.GetMarketingBudget(artist) : label.GetStandardMarketingBudget()) * RecordCampaignSpend.TrancheShare;
				manager.ProcessRecordCampaignsForChecks();
				Check(Near(hot.campaignPushFactor.Value, 1.3f) && hot.campaignTranches == 1 && Near(label.cashReserves, cash - tranche)
					&& Near(hot.sunkMarketingCost.Value, 100f + tranche), "a breaking single is worked: cash spent, attributed, push raised");
				Check(Near(cold.campaignPushFactor.Value, .6f) && cold.campaignTranches == -1, "a dead single is cut once");
				manager.ProcessRecordCampaignsForChecks(); manager.ProcessRecordCampaignsForChecks();
				Check(hot.campaignTranches == 2 && Near(hot.campaignPushFactor.Value, Math.Min(2f, 1.3f * 1.3f)) && Near(cold.campaignPushFactor.Value, .6f),
					"at most two tranches, and a cut is not repeated");
				hot.campaignTranches = 0; label.cashReserves = label.GetMonthlyOverhead() * 3f; float poor = label.cashReserves;
				manager.ProcessRecordCampaignsForChecks();
				Check(hot.campaignTranches == 0 && label.cashReserves == poor, "no tranche is paid out of the cash reserve");
			} finally {
				foreach (var s in savedRecords) {
					s.r.weeksSinceRelease = s.weeksSinceRelease; s.r.currentPosition = s.currentPosition; s.r.lastWeekPosition = s.lastWeekPosition;
					s.r.peakPosition = s.peakPosition; s.r.campaignPushFactor = s.campaignPushFactor; s.r.campaignTranches = s.campaignTranches;
					s.r.sunkMarketingCost = s.sunkMarketingCost;
				}
				foreach (var (other, factor) in others) other.campaignPushFactor = factor;
				label.cashReserves = savedCash;
			}
			RecordCampaignSpend.SetForChecks(false);
			Check(!ChartManager.Instance.GetAllRecords().Any(r => r.campaignPushFactor.HasValue) && SaveGameService.WritingVersion != 27,
				"a world without campaigns keeps its save version");
			hot.campaignPushFactor = 1f;
			Check(SaveGameService.WritingVersion == 27, "a retained campaign requires v27 even with the switch off");
			hot.campaignPushFactor = null;
		} finally { RecordCampaignSpend.SetForChecks(saved); }
	}

	private static void AiPublishing() {
		float rate = MechanicalRoyaltyService.RatePerCopy;
		Check(Near(PeriodAiPublishing.Pool(ReleaseFormat.Single, 1000f, 1000f * 2f * rate), 1000f * rate),
			"a single routes the A-side's mechanical only");
		Check(Near(PeriodAiPublishing.Pool(ReleaseFormat.Album, 100f, 100f * 12f * rate), 100f * 12f * rate)
			&& Near(PeriodAiPublishing.Pool(ReleaseFormat.Album, 100f, 0f), 100f * LabelPressingEconomics.ExpectedAlbumCompositionCost),
			"an LP routes every track's mechanical");
		float units = 1000f, expensed = units * 2f * rate, pool = units * rate;
		Check(Near(PeriodAiPublishing.NetAdjustment(expensed, 0f, 0f, 0f), expensed), "a label that owns the song gets its own mechanical back");
		Check(Near(PeriodAiPublishing.NetAdjustment(expensed, 0f, pool, 0f), expensed - pool),
			"a cover pays the controlling label 2c a copy, out of what it already expensed");
		Check(Near(PeriodAiPublishing.NetAdjustment(0f, 0f, pool, 0f), -pool), "without an expensed mechanical the transfer comes off net");
		Check(pool / units < .11f * .89f / 4f, "the routed cover mechanical is under a quarter of the old 11% pool");
	}

	private static void Renewal() {
		bool saved = CrossCollateralizedRenewal.Enabled;
		try {
			CrossCollateralizedRenewal.SetForChecks(false);
			Check(CrossCollateralizedRenewal.RenewedBalance(700f, 2000f) == 2000f, "off, a renewal wipes the balance");
			CrossCollateralizedRenewal.SetForChecks(true);
			Check(CrossCollateralizedRenewal.RenewedBalance(700f, 2000f) == 2700f && CrossCollateralizedRenewal.RenewedBalance(-5f, 2000f) == 2000f,
				"on, the old balance carries into the new advance");
		} finally { CrossCollateralizedRenewal.SetForChecks(saved); }
	}

	private static void SectionC() {
		bool savedPrices = PeriodListPrices.Enabled, savedStudio = SingleStudioModifier.Enabled;
		try {
			// C1
			Check(AlbumLaunchReserve.ClampCampaign(1000f, 5000f, 4000f) == 800f && AlbumLaunchReserve.ClampCampaign(1000f, 5000f, 1000f) == 1000f
				&& AlbumLaunchReserve.ClampCampaign(1000f, 3000f, 4000f) == 0f, "an LP campaign is clamped above the full launch reserve");
			// C2
			PeriodListPrices.SetForChecks(false);
			Check(PeriodListPrices.FormatList(ReleaseFormat.Single, .89f) == .89f, "off, the authored single list stands");
			PeriodListPrices.SetForChecks(true);
			Check(PeriodListPrices.FormatList(ReleaseFormat.Single, .89f) == PlayerDesk.ListPrice && PeriodListPrices.FormatList(ReleaseFormat.Album, 3.98f) == 3.98f,
				"on, a single lists at the player's 98c");
			var stereo = new Record { format = ReleaseFormat.Album, album = new Album { isStereo = true } };
			var mono = new Record { format = ReleaseFormat.Album, album = new Album { isStereo = false } };
			Check(Near(PeriodListPrices.RecordList(stereo, 3.98f), 4.98f) && PeriodListPrices.RecordList(mono, 3.98f) == 3.98f,
				"a stereo LP lists a dollar above mono");
			Check(Near(CompetitorManager.Instance.GetPricePerUnitForAudit(ReleaseFormat.Single), PlayerDesk.ListPrice), "the live price table reads the period list");
			// C3
			SingleStudioModifier.SetForChecks(false);
			Check(Near(SingleStudioModifier.BaseQuality(.6f, .83f), (.6f * .82f + .83f * .18f) * .83f), "off, the studio is blended and multiplied");
			SingleStudioModifier.SetForChecks(true);
			float low = SingleStudioModifier.BaseQuality(.6f, .83f), high = SingleStudioModifier.BaseQuality(.6f, 1.15f);
			Check(Near(low, .6f * .82f + .83f * .18f) && high - low < .06f, "on, HQ moves base quality by the blend alone");
			// C4
			Check(SelfExcludedArtistHeat.Heat(.45f, 1) == .5f && SelfExcludedArtistHeat.Heat(2f, 9) == 1f, "heat parts reassemble the cached heat");
			float sum = SelfExcludedArtistHeat.Contribution(5, 3).Heat + SelfExcludedArtistHeat.Contribution(60, 30).Heat;
			Check(Near(SelfExcludedArtistHeat.Excluding(sum, 2, 5, 3), SelfExcludedArtistHeat.Heat(SelfExcludedArtistHeat.Contribution(60, 30).Heat, 1)),
				"a record reads its act's heat without its own run");
			Check(SelfExcludedArtistHeat.Excluding(SelfExcludedArtistHeat.Contribution(5, 3).Heat, 1, 5, 3) == 0f, "a lone record gets no heat from itself");
		} finally { PeriodListPrices.SetForChecks(savedPrices); SingleStudioModifier.SetForChecks(savedStudio); }
	}
}
