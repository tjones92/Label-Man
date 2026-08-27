using System;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §6.1 -- THE TRADE REVIEW DESK.
//
// Player-only. One submission per record, resolved 1-2 weeks later into an outcome weighted by
// the record's real hookStrength/productionQuality and the label's reputation -- never a coin
// flip layered on top of the honest quality numbers already on the record. A live pick
// (Spotlight/FourStar/TwoLineMention) is a bounded, decaying nudge on distributor-facing levers
// only (mailing landing chance, InboundCall generation, unproven house-line acceptance, a Rolodex
// counter) -- directive invariant 4: the trades talk to the trade, not to the public, so it never
// touches consumer awareness or units directly. See PlayerDesk.TradePress.cs for the verb and the
// weekly resolution.
// ============================================================================================
public enum TradeOutcome { Pending, Nothing, TwoLineMention, FourStar, Spotlight }

public sealed class TradeSubmission {
	public string RecordId;
	public int SubmittedWeek;
	public int ResolveWeek;
	public TradeOutcome Outcome = TradeOutcome.Pending;
	// Chart week the pick's effects lapse. 0 once resolved to Nothing or once the window has passed.
	public int PickExpiresWeek;
}

/// <summary>Flat save record for one <see cref="TradeSubmission"/>.</summary>
public sealed class TradeSubmissionSaveData {
	public string RecordId { get; set; }
	public int SubmittedWeek { get; set; }
	public int ResolveWeek { get; set; }
	public int OutcomeOrdinal { get; set; }
	public int PickExpiresWeek { get; set; }

	public static TradeSubmissionSaveData From(TradeSubmission s) => new() {
		RecordId = s.RecordId, SubmittedWeek = s.SubmittedWeek, ResolveWeek = s.ResolveWeek,
		OutcomeOrdinal = (int)s.Outcome, PickExpiresWeek = s.PickExpiresWeek
	};

	public TradeSubmission ToSubmission() => new() {
		RecordId = RecordId, SubmittedWeek = SubmittedWeek, ResolveWeek = ResolveWeek,
		Outcome = Enum.IsDefined(typeof(TradeOutcome), OutcomeOrdinal) ? (TradeOutcome)OutcomeOrdinal : TradeOutcome.Pending,
		PickExpiresWeek = PickExpiresWeek
	};
}

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §6.2 -- THE TRADE AD.
//
// A paid, guaranteed (never rolled) version of the same distributor-facing signal a review pick
// earns for free -- "this label is going to work this record, stock it." Not consumer advertising:
// it adds zero direct awareness or units (invariant 4), only a bounded, time-boxed lift on the same
// distributor-facing levers §6.1 feeds, plus the cold Rolodex connect roll and a commercial-pitch
// bonus a review pick doesn't touch. See PlayerDesk.TradePress.cs for the verb and bonus math.
// ============================================================================================
public enum TradeAdTier { QuarterPage, HalfPage, FullPage }

public sealed class TradeAd {
	public string RecordId;
	public TradeAdTier Tier;
	public int PurchasedWeek;
	public int ExpiresWeek; // inclusive
}

public sealed class TradeAdSaveData {
	public string RecordId { get; set; }
	public int TierOrdinal { get; set; }
	public int PurchasedWeek { get; set; }
	public int ExpiresWeek { get; set; }

	public static TradeAdSaveData From(TradeAd a) => new() {
		RecordId = a.RecordId, TierOrdinal = (int)a.Tier, PurchasedWeek = a.PurchasedWeek, ExpiresWeek = a.ExpiresWeek
	};

	public TradeAd ToAd() => new() {
		RecordId = RecordId,
		Tier = Enum.IsDefined(typeof(TradeAdTier), TierOrdinal) ? (TradeAdTier)TierOrdinal : TradeAdTier.QuarterPage,
		PurchasedWeek = PurchasedWeek, ExpiresWeek = ExpiresWeek
	};
}
