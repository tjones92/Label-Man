using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>--studio-parent-labels: parent support, the annual review, routes and inertness (FilmBroadcastLabelResearch.md).</summary>
public static class StudioParentLabelsChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string why) { if (!value) throw new InvalidOperationException("Studio parent labels: " + why); checks++; }
		bool Near(double a, double b) => Math.Abs(a - b) < .01;
		AILabel Firm(string id, LabelTier tier, float cash, string parent = null, params Genre[] genres) {
			var label = new AILabel { labelId = id, labelName = id, tier = tier, cashReserves = cash, status = LabelStatus.Stable,
				homeCityId = "new_york", studioParent = parent, maxRosterSize = 20, isHistorical = true,
				preferredGenres = genres.Length > 0 ? genres : new[] { Genre.TeenPop }, secondaryGenres = Array.Empty<Genre>() };
			LabelCompanyAccounts.Open(label);
			return label;
		}
		// One closed month of market gross per label, which is what SupportMonth reads.
		AILabel Market(string id, double gross) {
			var label = Firm(id, LabelTier.Major, 100000);
			label.company.lastStatement = new LabelCompanyStatement { recordGross = gross };
			return label;
		}
		LabelCorporateModel.SetForChecks(true, staff: true);
		var february = new GameDate(1960, 2, 1);   // closes January 1960

		// ---- off: nothing reads the parent ----
		StudioParentLabels.SetEnabledForChecks(false);
		var dormantStudio = Firm("studio-off", LabelTier.MidTier, -500, StudioParentLabels.Warner);
		Check(!StudioParentLabels.IsStudioLabel(dormantStudio) && !StudioParentLabels.HoldsMajorPromotion(dormantStudio),
			"off: a stored parent is ignored");
		Check(StudioParentLabels.SupportMonth(dormantStudio, new[] { Market("m-off", 1e6) }, february) == 0f && Near(dormantStudio.cashReserves, -500),
			"off: no support is paid");
		Check(StudioParentLabels.PickupPremium(dormantStudio, 150) == 0f, "off: no pickup premium");
		Check(StudioParentLabels.CanFund(dormantStudio, 100) == dormantStudio.CanAffordToSign(100), "off: funding is the label's own");
		GameRandom.Seed(4411); float next = GameRandom.Randf(); GameRandom.Seed(4411);
		Check(StudioParentLabels.RouteSoundtrack(new[] { dormantStudio }, new ExternalMediaProfile { sourceType = ExternalMediaSourceType.FilmScore }, 1961) == null
			&& next == GameRandom.Randf(), "off: soundtrack routing returns null and draws nothing");
		Check(StudioParentLabels.ParentFor("Dot") == StudioParentLabels.Paramount && StudioParentLabels.ParentFor("Colpix") == StudioParentLabels.Columbia
			&& StudioParentLabels.ParentFor("Columbia") == null, "template parents");

		// ---- support: tops up to the reserve, never above it ----
		StudioParentLabels.SetEnabledForChecks(true);
		var market = new List<AILabel> { Market("m1", 600000), Market("m2", 400000) };
		var studio = Firm("studio", LabelTier.MidTier, -2000, StudioParentLabels.Warner);
		market.Add(studio);
		float paid = StudioParentLabels.SupportMonth(studio, market, february);
		float reserve = EconomicLabelLifecycle.CommitmentReserve(studio, signing: false);
		Check(Near(studio.parentSupportAccrued.Single(), 1e6 * StudioParentLabels.SupportShareOfMarketGross), "a month accrues 1.25% of market gross");
		Check(Near(studio.cashReserves, reserve) && Near(paid, reserve + 2000), "a deficit is topped up to the commitment reserve exactly");
		Check(Near(StudioParentLabels.Capacity(studio), 12500 - paid), "the draw reduces capacity");
		var statement = LabelCompanyAccounts.CloseMonth(studio, LabelCorporateModel.Period(february) - 1);
		Check(Near(statement.ownerTransfersNet, paid) && Near(statement.pretaxEarnings, 0), "parent capital is an owner transfer, not earnings");

		var healthy = Firm("healthy", LabelTier.MidTier, 50000, StudioParentLabels.Mgm);
		Check(StudioParentLabels.SupportMonth(healthy, market, february) == 0f && Near(healthy.cashReserves, 50000), "a solvent label draws nothing");

		var capped = Firm("capped", LabelTier.MidTier, -1e6f, StudioParentLabels.Mgm);
		StudioParentLabels.SupportMonth(capped, market, february);
		Check(Near(capped.cashReserves, -1e6 + 12500) && StudioParentLabels.Capacity(capped) == 0f, "support is capped at the accrued budget");
		for (int m = 0; m < 14; m++) StudioParentLabels.SupportMonth(healthy, market, new GameDate(1960, 3, 1));
		Check(healthy.parentSupportAccrued.Count == StudioParentLabels.SupportWindowMonths, "the window keeps twelve months");

		// ---- the annual review ----
		var review = Firm("review", LabelTier.MidTier, 0, StudioParentLabels.Columbia);
		for (int year = 1960; year < 1960 + StudioParentLabels.ClosureLossYears; year++) {
			review.parentSupportYearDrawn = 100;
			StudioParentLabels.ReviewYear(review, new GameDate(year + 1, 1, 1));
			Check(review.parentClosureDue == (year == 1960 + StudioParentLabels.ClosureLossYears - 1), "closure only after six straight years");
		}
		review.parentClosureDue = false; review.parentSupportYearDrawn = 0;
		StudioParentLabels.ReviewYear(review, new GameDate(1967, 1, 1));
		Check(review.parentLossYears == 0 && !review.parentClosureDue, "a year without support resets the count");
		StudioParentLabels.ReviewYear(review, new GameDate(1967, 6, 1));
		Check(review.parentLossYears == 0, "the review runs only at the December close");
		Check(StudioParentLabels.HoldsMajorPromotion(review), "a studio label may not promote to Major");

		var restored = JsonSerializer.Deserialize<AILabel>(JsonSerializer.Serialize(studio, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		Check(restored.studioParent == studio.studioParent && Near(StudioParentLabels.Capacity(restored), StudioParentLabels.Capacity(studio))
			&& restored.parentSupportYear == studio.parentSupportYear && Near(restored.parentSupportLifetime, studio.parentSupportLifetime),
			"parent state survives a save");

		// ---- acquisitions ----
		var buyer = Firm("buyer", LabelTier.MidTier, 0, StudioParentLabels.Paramount);
		StudioParentLabels.SupportMonth(buyer, market, february);
		Check(StudioParentLabels.CanFund(buyer, 1000) && !buyer.CanAffordToSign(1000), "the parent funds what the label cannot");
		Check(StudioParentLabels.PickupPremium(buyer, 150) == StudioParentLabels.PickupStrengthPremium, "a funded studio buyer bids harder");
		float before = buyer.cashReserves, freeCash = Math.Max(0f, before - StudioParentLabels.SigningReserve(buyer));
		StudioParentLabels.FundAcquisition(buyer, 1000, "check", february);
		Check(Near(buyer.cashReserves - before, 1000 - freeCash), "the parent pays what the label's free cash cannot");

		var indie = Firm("indie", LabelTier.Independent, 50000);
		var major = Firm("major", LabelTier.Major, 500000);
		var hit = new SimulatedArtist { artistId = "hit-act", primaryGenre = Genre.TeenPop, careerState = CareerState.Rising, contractTop40Hits = 1 };
		var unproven = new SimulatedArtist { artistId = "unproven", primaryGenre = Genre.TeenPop, careerState = CareerState.Declining };
		var bidders = new List<AILabel> { buyer };
		Check(StudioParentLabels.SelectBidder(major, hit, 1961, bidders).Bidder == null, "no bid for a Major's act");
		Check(StudioParentLabels.SelectBidder(indie, unproven, 1961, bidders).Bidder == null, "no bid for an unproven act");
		Check(StudioParentLabels.SelectBidder(buyer, hit, 1961, bidders).Bidder == null, "no bid against a studio label");
		int accepted = 0;
		for (int i = 0; i < 200; i++) {
			var act = new SimulatedArtist { artistId = "act-" + i, primaryGenre = Genre.TeenPop, careerState = CareerState.Established };
			var (winner, guarantee) = StudioParentLabels.SelectBidder(indie, act, 1961, bidders);
			if (winner == null) continue;
			accepted++;
			Check(Near(guarantee, StudioParentLabels.BidGuaranteeMultiple * Math.Max(1f, indie.CalculateAdvanceOffer(act))), "the guarantee doubles the renewal offer");
		}
		Check(accepted > 70 && accepted < 130, $"about half of proven acts accept ({accepted}/200)");
		buyer.parentBidYear = 1961;
		Check(StudioParentLabels.SelectBidder(indie, hit, 1961, bidders).Bidder == null, "one won bid per label per year");

		// ---- soundtracks ----
		var studios = new List<AILabel> {
			Firm("mgm", LabelTier.MidTier, 5e5f, StudioParentLabels.Mgm), Firm("wb", LabelTier.MidTier, 5e5f, StudioParentLabels.Warner),
			Firm("ua", LabelTier.MidTier, 5e5f, StudioParentLabels.UnitedArtists), Firm("dot", LabelTier.MidTier, 5e5f, StudioParentLabels.Paramount),
			Firm("colpix", LabelTier.Independent, 5e5f, StudioParentLabels.Columbia), Firm("label_0004", LabelTier.Major, 5e5f) };
		GameRandom.Seed(9182);
		Check(StudioParentLabels.RouteSoundtrack(studios, new ExternalMediaProfile { sourceType = ExternalMediaSourceType.StageCast }, 1961) == null,
			"stage casts go to the open bid");
		int own = 0, decca1961 = 0, decca1963 = 0;
		for (int i = 0; i < 2000; i++) {
			var profile = new ExternalMediaProfile { sourceType = ExternalMediaSourceType.FilmScore, upfrontLicenseFee = 1.5f };
			AILabel routed = StudioParentLabels.RouteSoundtrack(studios, profile, i % 2 == 0 ? 1961 : 1963);
			if (routed == null) continue;
			own++;
			Check(routed.labelId == "label_0004" ? profile.studioOfOrigin == StudioParentLabels.Universal : routed.studioParent == profile.studioOfOrigin,
				"a routed score goes to its own studio's label");
			if (routed.labelId == "label_0004") { if (i % 2 == 0) decca1961++; else decca1963++; }
		}
		float share = own / 2000f;
		Check(share > .22f && share < .38f, $"about a third of ordinary scores go to their own studio ({share:P0})");
		Check(decca1961 == 0 && decca1963 > 0, "Universal's scores reach Decca only from 1962");

		StudioParentLabels.SetEnabledForChecks(false);
		LabelCorporateModel.SetForChecks(false);
		GD.Print($"STUDIO_PARENT_LABELS_CHECK_PASSED checks={checks}");
	}
}
