using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §6.1 & §6.3 -- THE TRADE REVIEW DESK AND THE BREAKOUT LISTING.
//
// §6.1: one submission per record, resolved 1-2 weeks later against the record's own quality
// numbers, never a coin flip. §6.3 needs no new state at all -- it is a live read of
// RegionalRecordData.peakBreakoutScore, the same evidence bar CompetitorManager already applies
// to the AI's own independent-distribution path (GetProvenBreakoutRegions), reused rather than a
// parallel bar (directive invariant: never falsify or duplicate a number to make a mechanic work).
// Both feed only distributor-facing levers -- mailing landing chance, InboundCall generation, the
// unproven house-line accept roll, and a Rolodex counter -- never a direct units or awareness
// multiplier (invariant 4).
// ============================================================================================
public partial class PlayerDesk : Node {

	// Directive §6.1: "postage" for the review copy.
	public const float TradeReviewPostage = 0.15f;
	private const int TradeReviewResolveMinWeeks = 1;
	private const int TradeReviewResolveMaxWeeks = 2;
	// "Big multiplier... for ~4 weeks" -- how long a live pick keeps paying off.
	private const int TradePickWindowWeeks = 4;

	private RecordRuntimeData FindReleasedRecord(string recordId) =>
		ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);

	/// <summary>Directive §6.1: "Send it to the review desk." Free but for one promo copy and postage;
	/// one submission per record, ever -- a second copy to the same desk isn't a second review.</summary>
	public bool SubmitToReviewDesk(string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to submit."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to submit right now -- the master's out."; return false; }
		if (tradeSubmissions.Any(s => s.RecordId == recordId)) { message = "Already sent that one to the desk -- one submission is what it gets."; return false; }

		PressStock stock = StockFor(recordId);
		if (stock == null || stock.PromoRemaining <= 0) { message = "No promo copies on hand to send -- press some, or strike a repress all-promo."; return false; }
		if (!Require(ActionCosts.Paperwork, out message)) return false;
		if (Label.cashReserves < TradeReviewPostage) { message = $"You're short the ${TradeReviewPostage:F2} postage."; return false; }

		Spend(ActionCosts.Paperwork);
		Label.cashReserves -= TradeReviewPostage;
		Label.monthlyExpenses += TradeReviewPostage;
		stock.PromoRemaining -= 1;

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		tradeSubmissions.Add(new TradeSubmission {
			RecordId = recordId, SubmittedWeek = week,
			ResolveWeek = week + GD.RandRange(TradeReviewResolveMinWeeks, TradeReviewResolveMaxWeeks),
		});

		string title = TitleForRecord(recordId);
		Note($"Sent \"{title}\" to the trade review desk. A week or two before you hear anything.");
		message = $"\"{title}\" is on its way to the desk.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Directive §6.1: resolves 1-2 weeks after submission, weighted by the record's real
	/// hookStrength/productionQuality and label reputation -- most records got nothing, and this rolls
	/// that honestly rather than guaranteeing a payoff for pressing the button.</summary>
	private void ResolveTradeSubmissions() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (TradeSubmission sub in tradeSubmissions.Where(s => s.Outcome == TradeOutcome.Pending && week >= s.ResolveWeek).ToList()) {
			RecordRuntimeData rec = FindReleasedRecord(sub.RecordId);
			float quality = rec?.baseRecord != null ? (rec.baseRecord.hookStrength + rec.baseRecord.productionQuality) * 0.5f : 0.3f;
			float standing = Mathf.Clamp(Label?.reputation ?? 0f, 0f, 1f);
			float score = quality + standing * 0.15f;
			float roll = GD.Randf();

			TradeOutcome outcome;
			if (roll < 0.03f + score * 0.05f) outcome = TradeOutcome.Spotlight;
			else if (roll < 0.12f + score * 0.14f) outcome = TradeOutcome.FourStar;
			else if (roll < 0.32f + score * 0.22f) outcome = TradeOutcome.TwoLineMention;
			else outcome = TradeOutcome.Nothing;

			sub.Outcome = outcome;
			sub.PickExpiresWeek = outcome == TradeOutcome.Nothing ? 0 : week + TradePickWindowWeeks;

			string title = TitleForRecord(sub.RecordId);
			Note(outcome switch {
				TradeOutcome.Spotlight => $"Cash Box put \"{title}\" in the Spotlight column. The phone is going to ring.",
				TradeOutcome.FourStar => $"\"{title}\" came back a Best Bet in the trades.",
				TradeOutcome.TwoLineMention => $"\"{title}\" got a two-line mention in the trades. Not nothing.",
				_ => $"Nothing came back on \"{title}\" from the trades. Most records got nothing.",
			});
		}
	}

	/// <summary>The live pick on this record, or <see cref="TradeOutcome.Nothing"/> once it has lapsed
	/// or never landed. Every distributor-facing bonus below reads this rather than the raw submission,
	/// so a pick that has aged out stops paying off exactly like the directive's "~4 weeks" says.</summary>
	public TradeOutcome ActiveTradeOutcome(string recordId) {
		TradeSubmission sub = tradeSubmissions.FirstOrDefault(s => s.RecordId == recordId && s.Outcome != TradeOutcome.Pending);
		if (sub == null || sub.PickExpiresWeek <= 0) return TradeOutcome.Nothing;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return week <= sub.PickExpiresWeek ? sub.Outcome : TradeOutcome.Nothing;
	}

	/// <summary>Whether a submission is sitting at the desk, unresolved -- for the OFFICE readout.</summary>
	public bool HasPendingTradeSubmission(string recordId) =>
		tradeSubmissions.Any(s => s.RecordId == recordId && s.Outcome == TradeOutcome.Pending);

	public bool HasEverSubmittedToTrade(string recordId) => tradeSubmissions.Any(s => s.RecordId == recordId);

	public static string TradeOutcomeLabel(TradeOutcome outcome) => outcome switch {
		TradeOutcome.Spotlight => "Cash Box Spotlight pick",
		TradeOutcome.FourStar => "a Best Bet",
		TradeOutcome.TwoLineMention => "a two-line mention",
		_ => "nothing",
	};

	// ── §6.3: the breakout listing -- a live read, no new state ────────────────────────────────

	/// <summary>Directive §6.3: regions where this record has crossed the same evidence bar
	/// CompetitorManager.GetProvenBreakoutRegions already applies to the AI's own path -- reused, not
	/// re-derived. Live every call; nothing here is stored or rolled.</summary>
	public IEnumerable<string> BreakoutRegionNames(string recordId) {
		RecordRuntimeData rec = FindReleasedRecord(recordId);
		if (rec == null) yield break;
		float threshold = CompetitorManager.Instance?.RegionalBreakoutDealThreshold ?? 0.20f;
		foreach (var pair in rec.regionalData) {
			if ((pair.Value?.peakBreakoutScore ?? 0f) < threshold) continue;
			yield return ChartManager.Instance?.GetRegionById(pair.Key)?.regionName ?? pair.Key;
		}
	}

	public bool HasAnyBreakout(string recordId) => BreakoutRegionNames(recordId).Any();

	/// <summary>Directive §6.3: "other labels' breakouts included... because a player who can read the
	/// column can see a rival coming." One row per rival record currently over the bar in any region,
	/// drawn from the same live regionalData every AI record already carries.</summary>
	public IEnumerable<(string LabelName, string Title, string RegionName)> RivalBreakoutListings() {
		var chart = ChartManager.Instance;
		var competitors = CompetitorManager.Instance;
		if (chart == null || competitors == null) yield break;
		float threshold = competitors.RegionalBreakoutDealThreshold;
		foreach (RecordRuntimeData rec in chart.GetAllRecords() ?? Enumerable.Empty<RecordRuntimeData>()) {
			if (rec?.baseRecord == null || rec.baseRecord.labelId == Label?.labelId) continue;
			foreach (var pair in rec.regionalData) {
				if ((pair.Value?.peakBreakoutScore ?? 0f) < threshold) continue;
				string labelName = competitors.GetLabelDisplayName(rec.baseRecord.labelId) ?? "Somebody";
				string regionName = chart.GetRegionById(pair.Key)?.regionName ?? pair.Key;
				yield return (labelName, rec.baseRecord.title, regionName);
				break; // one row per rival record is the point, not every region it cleared
			}
		}
	}

	// ── Distributor-facing bonuses fed by §6.1 and §6.3 (invariant 4: never a units/awareness lever) ──

	/// <summary>Directive §5's mailing-landing stub: "a trade review bonus here... plugs in once §6.1
	/// exists." §6.3's breakout listing gives the same kind of story to a cold mailing, so it stacks.</summary>
	public float TradeMailingLandingBonus(string recordId) {
		float bonus = ActiveTradeOutcome(recordId) switch {
			TradeOutcome.Spotlight => 0.25f,
			TradeOutcome.FourStar => 0.15f,
			TradeOutcome.TwoLineMention => 0.06f,
			_ => 0f,
		};
		if (HasAnyBreakout(recordId)) bonus += 0.15f;
		TradeAd ad = ActiveTradeAd(recordId);
		if (ad != null) bonus += TradeAdMailingBonus(ad.Tier);
		return bonus;
	}

	/// <summary>Directive §6.1 ("big multiplier on InboundCall generation") and §6.3 ("the loudest
	/// inbound generator in the game") both act on the same lever -- GenerateInboundCalls' stranger and
	/// OneStopTest rolls -- rather than each inventing its own channel.</summary>
	public float TradeInboundCallMultiplier(string recordId) {
		float mult = 1f + ActiveTradeOutcome(recordId) switch {
			TradeOutcome.Spotlight => 1.5f,
			TradeOutcome.FourStar => 0.8f,
			TradeOutcome.TwoLineMention => 0.3f,
			_ => 0f,
		};
		if (HasAnyBreakout(recordId)) mult += 1.8f;
		TradeAd ad = ActiveTradeAd(recordId);
		if (ad != null) mult += TradeAdInboundBonus(ad.Tier);
		return mult;
	}

	/// <summary>Directive §6.1: "a bonus to house-visit acceptance." PlaceLine isn't pitched with one
	/// particular record in hand, so this reads the label's best live trade standing across its whole
	/// catalogue -- the same "the label's name is known" logic a real distributor would apply.</summary>
	public float TradeHouseAcceptBonus() =>
		ReleasedRecords.Select(r => ActiveTradeOutcome(r.baseRecord?.recordId) switch {
			TradeOutcome.Spotlight => 0.30f,
			TradeOutcome.FourStar => 0.15f,
			_ => 0f,
		}).DefaultIfEmpty(0f).Max();

	// ── §6.2: the trade ad -- paid, guaranteed, never rolled ────────────────────────────────────

	public static float TradeAdCost(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => 75f, TradeAdTier.HalfPage => 250f, TradeAdTier.FullPage => 600f, _ => 75f
	};
	public static string TradeAdTierName(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => "Quarter-Page", TradeAdTier.HalfPage => "Half-Page", TradeAdTier.FullPage => "Full-Page", _ => "Ad"
	};
	private static int TradeAdWeeks(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => 3, TradeAdTier.HalfPage => 4, TradeAdTier.FullPage => 5, _ => 3
	};
	private static float TradeAdInboundBonus(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => 0.4f, TradeAdTier.HalfPage => 0.9f, TradeAdTier.FullPage => 1.6f, _ => 0f
	};
	private static float TradeAdMailingBonus(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => 0.08f, TradeAdTier.HalfPage => 0.14f, TradeAdTier.FullPage => 0.22f, _ => 0f
	};
	private static float TradeAdCommercialBonus(TradeAdTier tier) => tier switch {
		TradeAdTier.QuarterPage => 0.04f, TradeAdTier.HalfPage => 0.08f, TradeAdTier.FullPage => 0.14f, _ => 0f
	};

	/// <summary>Directive §6.2: "$75/$250/$600... era rate, not budget-scaled -- revise if a real rate
	/// card surfaces, but do not re-derive these from FoundingCapital again." Guaranteed to run --
	/// nothing here is rolled, unlike §6.1's review outcome. A second buy on the same record takes the
	/// stronger tier and later expiry rather than stacking, same discipline as StationAdvocacy.Grant.</summary>
	public bool BuyTradeAd(string recordId, TradeAdTier tier, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to advertise."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to advertise right now -- the master's out."; return false; }
		float cost = TradeAdCost(tier);
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N0} short of the ${cost:N0} {TradeAdTierName(tier)} rate."; return false; }
		if (!Require(ActionCosts.Paperwork, out message)) return false;

		Spend(ActionCosts.Paperwork);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		int expires = week + TradeAdWeeks(tier) - 1;
		TradeAd existing = tradeAds.FirstOrDefault(a => a.RecordId == recordId);
		if (existing != null) {
			if ((int)tier >= (int)existing.Tier) existing.Tier = tier;
			existing.ExpiresWeek = Mathf.Max(existing.ExpiresWeek, expires);
		} else {
			tradeAds.Add(new TradeAd { RecordId = recordId, Tier = tier, PurchasedWeek = week, ExpiresWeek = expires });
		}

		string title = TitleForRecord(recordId);
		Note($"Bought a {TradeAdTierName(tier)} trade ad for \"{title}\" -- ${cost:N0}. Distributors and jocks read this, not kids.");
		message = $"The {TradeAdTierName(tier)} ad runs for \"{title}\" -- ${cost:N0} gone, whether or not it pays off.";
		Changed?.Invoke();
		return true;
	}

	private TradeAd ActiveTradeAd(string recordId) {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return tradeAds.FirstOrDefault(a => a.RecordId == recordId && week <= a.ExpiresWeek);
	}

	public TradeAdTier? ActiveTradeAdTier(string recordId) => ActiveTradeAd(recordId)?.Tier;

	/// <summary>Directive §6.2: "a bonus to cold Rolodex connect rolls... and to CommercialPitch." Not
	/// tied to any one call's record -- the switchboard has seen the LABEL's name -- so this reads the
	/// strongest currently-running ad across the whole catalogue, same shape as TradeHouseAcceptBonus.</summary>
	public float TradeAdConnectBonus() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return tradeAds.Where(a => week <= a.ExpiresWeek)
			.Select(a => TradeAdCommercialBonus(a.Tier)).DefaultIfEmpty(0f).Max();
	}
}
