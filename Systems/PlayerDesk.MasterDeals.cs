using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum MasterDealKind { Lease, Sale, SaleWithAct }
public enum MasterDealStatus { AwaitingRelease, Out, Permanent, Reverted, Failed }
public enum MasterRoute { Call, Post, Visit, Inbound }
public enum MasterLedgerKind { Advance, Price, Stock, Earned, Recouped, Statement, ActRoyalty, ActRecouped, Released, Reverted, Failed }

/// <summary>One contract a buyer will sign: what it takes, what it pays, and when. Amounts are dollars; the royalty is a
/// share of the 45's suggested list price per payable unit (so 7% is 6.9 cents a copy).</summary>
public sealed class MasterDealTerms {
	public MasterDealKind Kind { get; set; }
	/// <summary>Lease and sale-with-act: an advance against the royalty, recouped before more is paid. Sale: the price,
	/// paid once, not recoupable.</summary>
	public float Advance { get; set; }
	public float RoyaltyRate { get; set; }
	/// <summary>Lease only: weeks from the buyer's release (or takeover) until the master reverts.</summary>
	public int TermWeeks { get; set; }
	/// <summary>Unreleased masters only: days after signing by which the buyer must release, or the rights revert.</summary>
	public int ReleaseWithinDays { get; set; }
	/// <summary>Sale-with-act only: the contract the act signs with the buyer.</summary>
	public ContractTermSheet? ActTerms { get; set; }
	public string Why { get; set; }
	public bool Recoupable => Kind != MasterDealKind.Sale;
	public float CentsPerUnit => RoyaltyRate * PlayerDesk.ListPrice;
}

/// <summary>An open offer from one buyer on one title: one or more contracts, open until <see cref="ExpiresDay"/>.</summary>
public sealed class MasterDealOffer {
	public string Id { get; set; }
	public string BuyerLabelId { get; set; }
	public string BuyerName { get; set; }
	public string RecordId { get; set; }
	public string Title { get; set; }
	public string ArtistId { get; set; }
	public bool Released { get; set; }
	public MasterRoute Route { get; set; }
	public int MadeDay { get; set; }
	public int ExpiresDay { get; set; }
	public string Answer { get; set; }
	/// <summary>What the buyer heard (its own read, never the hidden score); sizes its launch campaign.</summary>
	public float Read { get; set; }
	public List<MasterDealTerms> Terms { get; set; } = new();
}

public sealed class MasterLedgerEntry {
	public int Day { get; set; }
	public MasterLedgerKind Kind { get; set; }
	public float Amount { get; set; }
	public int Units { get; set; }
}

/// <summary>A signed master deal. Running balances are recomputable from <see cref="Ledger"/>.</summary>
public sealed class MasterDeal {
	public string Id { get; set; }
	public string BuyerLabelId { get; set; }
	public string BuyerName { get; set; }
	public string RecordId { get; set; }
	public string Title { get; set; }
	public string ArtistId { get; set; }
	public string ArtistName { get; set; }
	public MasterDealTerms Terms { get; set; }
	public MasterDealStatus Status { get; set; }
	public bool PickedUp { get; set; }
	public float Read { get; set; }
	public int SignedDay { get; set; }
	public int ReleaseDay { get; set; }
	public int ReleaseByDay { get; set; }
	public int? ReleasedDay { get; set; }
	public int? ExpiresDay { get; set; }
	public int NextStatementDay { get; set; }
	public int Units { get; set; }
	public float Earned { get; set; }
	public float Recouped { get; set; }
	public float Paid { get; set; }
	/// <summary>Lease: units on which the player still owes the act its royalty (settled on each statement).</summary>
	public int ActUnitsUnsettled { get; set; }
	public float ActPaid { get; set; }
	public float ActRecouped { get; set; }
	public int StockCopies { get; set; }
	public float StockValue { get; set; }
	public string EndNote { get; set; }
	public List<MasterLedgerEntry> Ledger { get; set; } = new();

	public float Unrecouped => Terms.Recoupable ? Mathf.Max(0f, R2(Terms.Advance - Recouped)) : 0f;
	public float Payable => R2(Earned - Recouped);
	public float OwedNow => Mathf.Max(0f, R2(Payable - Paid));
	public bool Live => Status is MasterDealStatus.AwaitingRelease or MasterDealStatus.Out or MasterDealStatus.Permanent;

	internal void Add(int day, MasterLedgerKind kind, float amount, int units = 0) =>
		Ledger.Add(new MasterLedgerEntry { Day = day, Kind = kind, Amount = R2(amount), Units = units });

	public bool LedgerAgrees() {
		float Sum(MasterLedgerKind k) => R2(Ledger.Where(e => e.Kind == k).Sum(e => e.Amount));
		return Ledger.Where(e => e.Kind == MasterLedgerKind.Earned).Sum(e => e.Units) == Units
			&& Math.Abs(Sum(MasterLedgerKind.Earned) - Earned) < 0.011f && Math.Abs(Sum(MasterLedgerKind.Recouped) - Recouped) < 0.011f
			&& Math.Abs(Sum(MasterLedgerKind.Statement) - Paid) < 0.011f && Math.Abs(Sum(MasterLedgerKind.ActRoyalty) - ActPaid) < 0.011f
			&& Math.Abs(Sum(MasterLedgerKind.ActRecouped) - ActRecouped) < 0.011f && Recouped <= Terms.Advance + 0.011f;
	}

	internal static float R2(float v) => Mathf.Round(v * 100f) / 100f;
}

public sealed class MasterPass {
	public string BuyerLabelId { get; set; }
	public string RecordId { get; set; }
	public int Day { get; set; }
	public string Evidence { get; set; }
	public string Reason { get; set; }
}

public sealed class MasterPosting {
	public string BuyerLabelId { get; set; }
	public string RecordId { get; set; }
	public int SentDay { get; set; }
	public int AnswerDay { get; set; }
	public bool Solicited { get; set; }
	/// <summary>Set once the answer day has come and gone with nobody listening: a further tape waits out the quiet.</summary>
	public bool NoReply { get; set; }
}

public sealed class MasterCall {
	public string BuyerLabelId { get; set; }
	public string RecordId { get; set; }
	public int Day { get; set; }
	public string Result { get; set; }
	/// <summary>A day the buyer's man will see the founder with the record, or null.</summary>
	public int? AppointmentDay { get; set; }
	public bool Welcome { get; set; }
}

/// <summary>
/// Early-game directive V2, Phase 5 (§5): shop the master. One deal state machine for every master deal the player makes,
/// from a cold call about an unreleased tape to a regional pickup off a breakout. Author decision (2026-10-14): three
/// contracts, Lease, Sale, and Sale with the act. They replace the old cash-only Sell/Lease buttons: the buyer is always
/// a real AI label, it pays out of its own cash, and it actually releases (or takes over) the record.
///
/// <b>Buyers</b> are active AI labels whose sound fits the record (its genre in their lists, or the same family) and that
/// have the cash to pay an advance today. Their offices are their own home towns.
///
/// <b>Access.</b> A call from the office (1h, in office hours, a long-distance toll) gets public answers only (no fit,
/// not buying, or "send a tape" and, when the office is in driving range, an appointment). A posted tape (1h, $5 dub +
/// $1.50 postage) is answered in 2-4 weeks, if anyone listens: 90% after a call asked for it, 40% unsolicited. A visit
/// (2h, in their town, in office hours, with an acetate or a pressed copy to play) is heard on an appointment, and gets
/// seen half the weeks as a walk-in. Driving rules apply; a far office can only be called or posted.
///
/// <b>The buyer's ear</b> reads hook and production with its own noise (the AI's scouting-ability range, wider on a
/// posted tape) and, for a released title, the regional evidence: breakout markets and copies sold. It never reads the
/// hidden score, and the noise is a fixed draw per buyer and title, so a pass stands until the evidence changes (the
/// record comes out, or breaks in another market). A different buyer is a different ear.
///
/// <b>Contracts</b> (authored, not sourced; the lease advance reference is §5.2's $150-600):
/// - <b>Lease</b>: exclusive, national, 52 weeks from the buyer's release; release within 42 days of signing or the rights
///   revert (the advance is kept). Advance against 7% of list (8% when it likes it a lot) per payable unit, recouped
///   first; quarterly statements. The act stays on the roster and the player still owes it its royalty on those units,
///   settled on each statement against its advance. At the end the record comes home with its chart history.
/// - <b>Sale</b>: the master is the buyer's for good. One price (2.5x the lease advance), not recoupable, no royalty. The
///   buyer pays the act's royalty on its sales directly; the player's advance to the act recoups only from the player.
/// - <b>Sale with the act</b> (released titles with a regional breakout, at an Independent or bigger with roster room):
///   the master is sold and the act signs with the buyer on that label's own terms. The player keeps a producer royalty
///   of 3% of list on that master, against an advance of 1.5x the lease advance, with quarterly statements.
/// Payable units are the units the buyer's settlement bills (AI returns are not modelled). Publishing is never included.
///
/// <b>Stock.</b> On signing the buyer takes the player's pressed copies of the title (at the office and in shops) at the
/// $0.14 a copy they cost, reimburses any run still at the plant, and its own salesmen take over the open calls. The
/// player has no sell-off: the title leaves every player channel the day the contract is signed.
///
/// <b>Regional pickup</b> (§5.3): when a released title breaks out in a new market, the best-fitting buyer that hears a
/// record may phone (likely, not guaranteed: a fixed draw per evidence), with the three contracts priced from the
/// evidence. The phone call is an offer like any other.
///
/// Shared-world effects in a player-present game (intended, §0.1): the buyer's cash (advance, stock, royalties, launch
/// campaign), its catalogue and chart entries, its roster (sale with the act), and the AI release stream.
/// </summary>
public partial class PlayerDesk {
	public const int MasterCallHours = 1;
	public const int MasterPostHours = 1;
	public const int MasterVisitHours = 2;
	public const int MasterSignHours = 1;
	public const float MasterTollBase = 0.50f;
	public const float MasterTollPerMile = 0.004f;
	public const float MasterTapeDub = 5f;
	public const float MasterPostage = 1.50f;
	public const int MasterPostAnswerMinDays = 14;
	public const int MasterPostAnswerSpanDays = 15;
	public const float MasterPostHeardSolicited = 0.90f;
	public const float MasterPostHeardUnsolicited = 0.40f;
	public const int MasterPostQuietDays = 91;
	public const float MasterWalkInSeen = 0.50f;
	public const int MasterCallRepeatDays = 28;
	public const int MasterOfferDays = 14;
	public const int MasterLetterOfferDays = 21;
	public const float MasterLeaseAdvanceMin = 150f;
	public const float MasterLeaseAdvanceMax = 600f;
	public const float MasterPickupAdvanceMax = 1500f;
	public const float MasterLeaseRate = 0.07f;
	public const float MasterLeaseRateStrong = 0.08f;
	public const float MasterProducerRate = 0.03f;
	public const float MasterSaleMultiple = 2.5f;
	public const float MasterWithActMultiple = 1.5f;
	public const int MasterLeaseWeeks = 52;
	public const int MasterReleaseWithinDays = 42;
	public const int MasterBuyerPlantMinDays = 14;
	public const int MasterBuyerPlantSpanDays = 15;
	public const int MasterStatementDays = 91;
	public const float MasterStockPerCopy = PressVinylPerUnit + PressSleeveLabelPerUnit;

	private readonly List<MasterDealOffer> masterOffers = new();
	private readonly List<MasterDeal> masterDeals = new();
	private readonly List<MasterPass> masterPasses = new();
	private readonly List<MasterPosting> masterPostings = new();
	private readonly List<MasterCall> masterCalls = new();
	private readonly Dictionary<string, string> masterPickupEvidence = new(StringComparer.Ordinal);
	/// <summary>Running number that keeps every offer and deal id unique (two deals can share a title and a day).</summary>
	private int masterOrdinal;
	/// <summary>Unreleased masters held for a buyer between signing and its release (they leave the shelf at signing).</summary>
	private readonly Dictionary<string, Master> heldMasters = new(StringComparer.Ordinal);

	public IReadOnlyList<MasterDealOffer> MasterOffers => masterOffers.Where(o => o.ExpiresDay >= TodayIndex()).ToList().AsReadOnly();
	public IReadOnlyList<MasterDeal> MasterDeals => masterDeals.AsReadOnly();
	public IReadOnlyList<MasterPass> MasterPasses => masterPasses.AsReadOnly();
	public IReadOnlyList<MasterPosting> MasterPostings => masterPostings.AsReadOnly();
	public IReadOnlyList<MasterCall> MasterCalls => masterCalls.AsReadOnly();

	/// <summary>True while a live deal holds the title (awaiting the buyer's release, out on lease, or sold).</summary>
	private bool MasterDealHolds(string recordId) => recordId != null && masterDeals.Any(d => d.RecordId == recordId && d.Live);
	public MasterDeal LiveMasterDeal(string recordId) => masterDeals.FirstOrDefault(d => d.RecordId == recordId && d.Live);
	private MasterDeal DealById(string id) => id == null ? null : masterDeals.FirstOrDefault(d => d.Id == id);

	// ── What can be shopped, and to whom ───────────────────────────────────────────────────────────

	/// <summary>A title the player could shop today: an unreleased master on the shelf (not in a dated release) or a
	/// released single, and not already out. Null with a reason otherwise.</summary>
	public (Record Record, bool Released, string Block) ShoppableMaster(string recordId) {
		if (Label == null) return (null, false, "You don't have a label yet.");
		if (string.IsNullOrEmpty(recordId)) return (null, false, "Pick a master.");
		if (IsMasterOut(recordId)) return (null, false, $"\"{TitleForRecord(recordId)}\" is already out on a deal.");
		var runtime = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (runtime != null)
			return runtime.baseRecord.format == ReleaseFormat.Single ? (runtime.baseRecord, true, null) : (null, true, "Only singles are shopped this way.");
		var master = masters.FirstOrDefault(m => m.Record?.recordId == recordId);
		if (master == null || master.Released) return (null, false, "That isn't a master you hold.");
		if (planned.Any(p => p.Dated && (p.Master == master || p.BSide == master)))
			return (null, false, $"\"{master.SongTitle}\" has a release date. Take it off the schedule before you shop it.");
		return (master.Record, false, null);
	}

	/// <summary>Titles the player could shop now, newest first.</summary>
	public IEnumerable<string> ShoppableMasterIds() =>
		ReleasedRecords.Where(r => r.baseRecord?.format == ReleaseFormat.Single).Select(r => r.baseRecord.recordId)
			.Concat(masters.Where(m => !m.Released && m.Record != null).Select(m => m.Record.recordId))
			.Where(id => ShoppableMaster(id).Block == null).Distinct(StringComparer.Ordinal);

	public sealed record MasterBuyer(AILabel Label, string CityId, string CityName, bool OwnSound, bool Reachable, string Block);

	private static GenreFamily? FamilyOf(Genre genre, int year) {
		try { return GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).Family; } catch (KeyNotFoundException) { return null; }
	}

	/// <summary>Whether the buyer's sound takes this record: its genre in their lists (own sound), the same family
	/// (near it), or neither (null).</summary>
	internal static bool? SoundFit(AILabel buyer, Genre genre, int year) {
		if ((buyer.preferredGenres?.Contains(genre) ?? false) || (buyer.secondaryGenres?.Contains(genre) ?? false)) return true;
		var family = FamilyOf(genre, year);
		if (family == null) return null;
		bool near = (buyer.preferredGenres ?? Array.Empty<Genre>()).Concat(buyer.secondaryGenres ?? Array.Empty<Genre>())
			.Any(g => FamilyOf(g, year) == family);
		return near ? false : null;
	}

	/// <summary>Real labels that would take a call about this title: active, the sound fits, and they have the cash to pay
	/// an advance. The public facts only -- nothing here reads the record's quality. Best fits first.</summary>
	public IReadOnlyList<MasterBuyer> MasterBuyersFor(string recordId, int max = 8) {
		var (record, _, block) = ShoppableMaster(recordId);
		if (record == null || block != null || CompetitorManager.Instance == null) return Array.Empty<MasterBuyer>();
		int year = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).year;
		var home = DistanceModel.GetCityById(Label.homeCityId);
		return CompetitorManager.Instance.GetOperatingLabels()
			.Where(l => l != null && !l.isPlayerOwned && l.IsActive)
			.Select(l => (Label: l, Fit: SoundFit(l, record.primaryGenre, year)))
			.Where(x => x.Fit.HasValue)
			.Select(x => {
				var city = DistanceModel.GetCityById(x.Label.homeCityId);
				string why = x.Label.CanAffordToSign(MasterLeaseAdvanceMin) ? null : "not buying anything this season";
				return new MasterBuyer(x.Label, city?.cityId, city?.name ?? x.Label.headquartersCity ?? "out of town", x.Fit.Value,
					city != null && home != null && (city.cityId == home.cityId || CanReach(home, city)), why);
			})
			.OrderBy(b => b.Block == null ? 0 : 1).ThenBy(b => b.OwnSound ? 0 : 1).ThenBy(b => (int)b.Label.tier)
			.ThenBy(b => b.Label.labelId, StringComparer.Ordinal)
			.Take(max).ToList();
	}

	internal static float MasterBar(AILabel buyer, bool ownSound) => buyer.tier switch {
		LabelTier.Major => .66f, LabelTier.MidTier => .60f, LabelTier.Independent => .54f, _ => .50f,
	} + (ownSound ? 0f : .04f);

	internal static float MasterTierScale(AILabel buyer) => buyer.tier switch {
		LabelTier.Major or LabelTier.MidTier => 1f, LabelTier.Independent => .85f, _ => .70f,
	};

	internal static int BreakoutMarkets(RecordRuntimeData rec) =>
		rec?.regionalData?.Values.Count(rd => rd != null && rd.breakoutStage >= RegionalBreakoutStage.RegionalBreakout) ?? 0;

	/// <summary>What a buyer's answer rests on, as a signature: a pass stands while this is unchanged.</summary>
	public string MasterEvidence(string recordId) {
		var rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		return rec == null ? "unreleased" : $"released|breakouts={BreakoutMarkets(rec)}";
	}

	/// <summary>The buyer's own read of the title: hook and production through its ear (a fixed draw per buyer and title,
	/// wider off a posted tape), plus the regional evidence for a released record. Never the hidden score itself.</summary>
	private float MasterRead(AILabel buyer, Record record, bool posted) {
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		float appeal = record.hookStrength * 0.60f + record.productionQuality * 0.40f;
		float noise = (StableUnit($"{seed}|{buyer.labelId}|{record.recordId}|MasterEar") * 2f - 1f)
			* Mathf.Lerp(0.30f, 0.10f, Mathf.Clamp(buyer.scoutingAbility, 0f, 1f)) * (posted ? 1.4f : 1f);
		var rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == record.recordId);
		float evidence = rec == null ? 0f : Mathf.Min(0.12f, 0.04f * BreakoutMarkets(rec)) + Mathf.Min(0.08f, rec.totalUnitsSold / 5000f);
		return Mathf.Clamp(appeal + noise + evidence, 0f, 1f);
	}

	internal static float Round25(float v) => Mathf.Round(v / 25f) * 25f;

	/// <summary>The buyer's answer on a heard record: a pass (with its words) or the contracts it would sign. Reads no state
	/// beyond the evidence and changes none (an offer only draws its id number).</summary>
	internal (string Pass, MasterDealOffer Offer) EvaluateMaster(AILabel buyer, string recordId, MasterRoute route) {
		var (record, released, block) = ShoppableMaster(recordId);
		if (record == null) return (block, null);
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		bool? fit = SoundFit(buyer, record.primaryGenre, today.year);
		if (fit == null) return ($"{buyer.labelName}: \"It's not our kind of record.\"", null);
		float read = MasterRead(buyer, record, route == MasterRoute.Post);
		float bar = MasterBar(buyer, fit.Value);
		if (read < bar)
			return ($"{buyer.labelName}: " + (read < bar - 0.10f ? "\"It doesn't stay with me. Thanks for thinking of us.\"" : "\"It's close, but it's not for us right now.\""), null);
		float s = Mathf.InverseLerp(bar, bar + 0.25f, read);
		var rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		int markets = BreakoutMarkets(rec);
		float lease = Mathf.Clamp(Round25(Mathf.Lerp(MasterLeaseAdvanceMin, MasterLeaseAdvanceMax, s) * MasterTierScale(buyer)), MasterLeaseAdvanceMin, MasterLeaseAdvanceMax);
		if (released && markets > 0)
			lease = Mathf.Min(MasterPickupAdvanceMax, Mathf.Max(lease, Round25(150f * markets + 0.10f * (rec?.totalUnitsSold ?? 0))));
		float rate = s >= 0.5f ? MasterLeaseRateStrong : MasterLeaseRate;
		var offer = new MasterDealOffer {
			Id = $"mo-{recordId}-{buyer.labelId}-{TodayIndex()}-{++masterOrdinal}", BuyerLabelId = buyer.labelId, BuyerName = buyer.labelName,
			RecordId = recordId, Title = TitleForRecord(recordId), ArtistId = record.artistId, Released = released, Route = route,
			MadeDay = TodayIndex(), ExpiresDay = TodayIndex() + (route == MasterRoute.Post ? MasterLetterOfferDays : MasterOfferDays), Read = read,
			Answer = $"{buyer.labelName}: " + (s < 0.33f ? "\"There's something there.\"" : s < 0.66f ? "\"I hear a record.\"" : "\"That's a hit sound.\""),
		};
		offer.Terms.Add(new MasterDealTerms {
			Kind = MasterDealKind.Lease, Advance = lease, RoyaltyRate = rate, TermWeeks = MasterLeaseWeeks,
			ReleaseWithinDays = released ? 0 : MasterReleaseWithinDays,
			Why = $"${lease:N0} advance against {rate:P0} of list ({rate * ListPrice * 100f:F1}c a copy), {MasterLeaseWeeks} weeks" +
				(released ? " from takeover" : $" from their release, which must come within {MasterReleaseWithinDays} days") +
				". The act stays yours, and you pay its royalty on their sales. Then the master comes home.",
		});
		if (s >= 0.5f || (released && markets > 0)) {
			float price = Round25(lease * MasterSaleMultiple);
			offer.Terms.Add(new MasterDealTerms {
				Kind = MasterDealKind.Sale, Advance = price, RoyaltyRate = 0f, ReleaseWithinDays = released ? 0 : MasterReleaseWithinDays,
				Why = $"${price:N0} for the master, for good. No royalty; they pay the act's royalty on their sales. The act stays yours.",
			});
		}
		var artist = ArtistManager.Instance?.GetArtist(record.artistId);
		if (released && markets > 0 && buyer.tier is LabelTier.Major or LabelTier.MidTier or LabelTier.Independent
				&& artist != null && artist.labelId == Label.labelId && buyer.CurrentRosterSize < buyer.maxRosterSize
				&& SceneRecruitmentService.CanAccess(buyer, artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0)) {
			var actTerms = RivalOfferTerms(buyer, artist, TodayIndex(), today.year);
			float advance = Round25(lease * MasterWithActMultiple);
			if (buyer.CanAffordToSign(advance + actTerms.Advance))
				offer.Terms.Add(new MasterDealTerms {
					Kind = MasterDealKind.SaleWithAct, Advance = advance, RoyaltyRate = MasterProducerRate, ActTerms = actTerms,
					Why = $"They buy the master and sign {artist.stageName} ({RivalTermsText(actTerms)}). You keep a producer's {MasterProducerRate:P0} of list " +
						$"({MasterProducerRate * ListPrice * 100f:F1}c a copy) on this record, against ${advance:N0} now.",
				});
		}
		return (null, offer);
	}

	private MasterPass StandingPass(string buyerId, string recordId) {
		string evidence = MasterEvidence(recordId);
		return masterPasses.LastOrDefault(p => p.BuyerLabelId == buyerId && p.RecordId == recordId && p.Evidence == evidence);
	}

	private MasterDealOffer OpenOffer(string buyerId, string recordId) =>
		masterOffers.FirstOrDefault(o => o.BuyerLabelId == buyerId && o.RecordId == recordId && o.ExpiresDay >= TodayIndex());

	/// <summary>Why this buyer can't be approached about this title now (any route), or null. Free to ask.</summary>
	private string MasterApproachBlock(AILabel buyer, string recordId) {
		var (record, _, block) = ShoppableMaster(recordId);
		if (record == null) return block;
		if (buyer == null || !buyer.IsActive || buyer.isPlayerOwned) return "No such label.";
		if (OpenOffer(buyer.labelId, recordId) != null) return $"{buyer.labelName}'s offer is on your desk.";
		if (StandingPass(buyer.labelId, recordId) is { } pass)
			return $"{buyer.labelName} passed on {LocalSceneRoomService.Date(pass.Day).ToShortString()}. Nothing's changed since.";
		return null;
	}

	private void RecordMasterAnswer(AILabel buyer, string recordId, string pass, MasterDealOffer offer) {
		if (pass != null) masterPasses.Add(new MasterPass { BuyerLabelId = buyer.labelId, RecordId = recordId, Day = TodayIndex(), Evidence = MasterEvidence(recordId), Reason = pass });
		else masterOffers.Add(offer);
	}

	private static string OfferSummary(MasterDealOffer offer) => string.Join(" ", offer.Terms.Select(t => t.Kind switch {
		MasterDealKind.Lease => $"Lease: ${t.Advance:N0} against {t.RoyaltyRate:P0}.",
		MasterDealKind.Sale => $"Sale: ${t.Advance:N0}.",
		_ => $"With the act: ${t.Advance:N0} against a {t.RoyaltyRate:P0} producer royalty.",
	}));

	// ── Access routes ───────────────────────────────────────────────────────────────────────────────

	private bool InOfficeHours(int hours, out string message) {
		var clock = TimeManager.Instance;
		message = clock == null ? "No clock." : clock.CurrentHour < BackerOfficeFromHour || clock.CurrentHour + hours > BackerOfficeUntilHour
			? $"Record offices keep hours, {BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m." : null;
		return message == null;
	}

	public float MasterCallToll(AILabel buyer) {
		float miles = Label == null || buyer?.homeCityId == null ? 0f : DistanceModel.GetRoadMilesBetween(Label.homeCityId, buyer.homeCityId);
		return MasterDeal.R2(MasterTollBase + MasterTollPerMile * miles);
	}

	/// <summary>A preliminary call from the office. Public answers only: no fit, not buying, or "send a tape" (and an
	/// appointment when their office is in driving range). It never reads the record.</summary>
	public bool CallMasterBuyer(string buyerId, string recordId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = MasterApproachBlock(buyer, recordId);
		if (message != null) return false;
		if (!RequireHome(out message)) { message = "Calls go out from the office."; return false; }
		if (!InOfficeHours(MasterCallHours, out message)) return false;
		int today = TodayIndex();
		var last = masterCalls.LastOrDefault(c => c.BuyerLabelId == buyerId && c.RecordId == recordId);
		if (last != null && today - last.Day < MasterCallRepeatDays) { message = $"You spoke to {buyer.labelName} on {LocalSceneRoomService.Date(last.Day).ToShortString()}: {last.Result}"; return false; }
		float toll = MasterCallToll(buyer);
		if (Label.cashReserves < toll) { message = $"You're ${toll - Label.cashReserves:N2} short of the long-distance toll."; return false; }
		if (!Require(MasterCallHours, out message)) return false;
		Spend(MasterCallHours);
		Label.cashReserves -= toll;
		Label.monthlyExpenses += toll;

		var b = MasterBuyersFor(recordId, int.MaxValue).FirstOrDefault(x => x.Label.labelId == buyerId);
		var call = new MasterCall { BuyerLabelId = buyerId, RecordId = recordId, Day = today };
		if (b == null) call.Result = $"\"It's not our kind of record.\"";
		else if (b.Block != null) call.Result = $"\"We're {b.Block}.\"";
		else {
			call.Welcome = true;
			if (b.Reachable) {
				ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
				call.AppointmentDay = today + 3 + (int)(StableUnit($"{seed}|{buyerId}|{recordId}|{today}|MasterAppointment") * 5f);
				call.Result = $"\"Send us a tape, or bring it in on {LocalSceneRoomService.Date(call.AppointmentDay.Value).ToShortString()}.\"";
			} else call.Result = "\"Send us a tape.\"";
		}
		masterCalls.Add(call);
		message = $"Called {buyer.labelName} in {b?.CityName ?? "their town"} (${toll:N2}): {call.Result}";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Posts a tape dub to a buyer's office. The answer, if anyone listens, comes by letter in two to four weeks.</summary>
	public bool PostMasterTape(string buyerId, string recordId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = MasterApproachBlock(buyer, recordId);
		if (message != null) return false;
		if (!RequireHome(out message)) { message = "Tapes go out from the office."; return false; }
		int today = TodayIndex();
		var pending = masterPostings.LastOrDefault(p => p.BuyerLabelId == buyerId && p.RecordId == recordId);
		if (pending != null && !pending.NoReply) { message = $"A tape is already on its way to {buyer.labelName}; expect word by {LocalSceneRoomService.Date(pending.AnswerDay).ToShortString()}."; return false; }
		if (pending != null && today - pending.SentDay < MasterPostQuietDays) { message = $"{buyer.labelName} never answered your last tape. Give it until {LocalSceneRoomService.Date(pending.SentDay + MasterPostQuietDays).ToShortString()}."; return false; }
		float cost = MasterTapeDub + MasterPostage;
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N2} short of the dub and postage."; return false; }
		if (!Require(MasterPostHours, out message)) return false;
		Spend(MasterPostHours);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		bool solicited = masterCalls.Any(c => c.BuyerLabelId == buyerId && c.RecordId == recordId && c.Welcome && today - c.Day <= 60);
		var post = new MasterPosting { BuyerLabelId = buyerId, RecordId = recordId, SentDay = today, Solicited = solicited,
			AnswerDay = today + MasterPostAnswerMinDays + (int)(StableUnit($"{seed}|{buyerId}|{recordId}|{today}|MasterPostDays") * MasterPostAnswerSpanDays) };
		masterPostings.Add(post);
		message = $"Posted a tape of \"{TitleForRecord(recordId)}\" to {buyer.labelName} (${cost:N2}). " +
			(solicited ? "They asked for it." : "Nobody asked for it, so it may sit in a pile.") + " Now you wait.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Plays the record to the buyer at its office. An appointment is always seen; a walk-in some weeks.</summary>
	public bool VisitMasterBuyer(string buyerId, string recordId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = MasterApproachBlock(buyer, recordId);
		if (message != null) return false;
		if (string.IsNullOrEmpty(buyer.homeCityId) || CurrentCityId != buyer.homeCityId) {
			var city = DistanceModel.GetCityById(buyer.homeCityId);
			message = $"{buyer.labelName}'s office is in {city?.name ?? "another town"}; you have to be there.";
			return false;
		}
		if (!InOfficeHours(MasterVisitHours, out message)) return false;
		var (_, released, _) = ShoppableMaster(recordId);
		bool copy = AcetatesFor(recordId) > 0 || released && (StockFor(recordId) is { } st && st.Remaining + st.PromoRemaining > 0);
		if (!copy) { message = released ? "You need a copy to play him: an acetate or a pressed 45." : "You need an acetate to play him."; return false; }
		int today = TodayIndex();
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		int weekIndex = today / 7;
		bool appointment = masterCalls.Any(c => c.BuyerLabelId == buyerId && c.RecordId == recordId && c.AppointmentDay == today);
		bool seen = appointment || StableUnit($"{seed}|{buyerId}|{weekIndex}|MasterWalkIn") < MasterWalkInSeen;
		if (!seen && masterCalls.Any(c => c.BuyerLabelId == buyerId && c.Day == today && c.Result == WalkInTurnedAway)) {
			message = $"{buyer.labelName} isn't seeing anyone without an appointment this week."; return false;
		}
		if (!Require(MasterVisitHours, out message)) return false;
		Spend(MasterVisitHours);
		if (!seen) {
			masterCalls.Add(new MasterCall { BuyerLabelId = buyerId, RecordId = recordId, Day = today, Result = WalkInTurnedAway });
			message = $"Waited in {buyer.labelName}'s front office. Nobody would see you without an appointment.";
			Note(message);
			Changed?.Invoke();
			return true;
		}
		var (pass, offer) = EvaluateMaster(buyer, recordId, MasterRoute.Visit);
		RecordMasterAnswer(buyer, recordId, pass, offer);
		message = pass ?? $"{offer.Answer} {OfferSummary(offer)} The offer stands until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}.";
		Note($"Played \"{TitleForRecord(recordId)}\" at {buyer.labelName}. {message}");
		Changed?.Invoke();
		return true;
	}

	private const string WalkInTurnedAway = "turned away at the front desk";

	// ── Signing ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Why this offer can't be signed now, or null: by phone from the office, or in person in their town.</summary>
	public string MasterSignBlock(MasterDealOffer offer, int termIndex) {
		if (offer == null || offer.ExpiresDay < TodayIndex() || !masterOffers.Contains(offer)) return "That offer isn't open.";
		if (termIndex < 0 || termIndex >= offer.Terms.Count) return "Pick one of their contracts.";
		var buyer = CompetitorManager.Instance?.GetLabel(offer.BuyerLabelId);
		if (buyer == null || !buyer.IsActive) return $"{offer.BuyerName} has gone under.";
		var (record, _, block) = ShoppableMaster(offer.RecordId);
		if (record == null) return block;
		if (!AtHome && CurrentCityId != buyer.homeCityId) return "Sign from the office, or in person at theirs.";
		var t = offer.Terms[termIndex];
		float total = t.Advance + MasterStockValue(offer.RecordId).Value + (t.ActTerms?.Advance ?? 0f);
		if (!buyer.CanAffordToSign(total)) return $"{offer.BuyerName} can't find ${total:N0} right now.";
		if (t.Kind == MasterDealKind.SaleWithAct) {
			var artist = ArtistManager.Instance?.GetArtist(offer.ArtistId);
			if (artist == null || artist.labelId != Label.labelId) return "The act isn't yours to send with it.";
			if (buyer.CurrentRosterSize >= buyer.maxRosterSize) return $"{offer.BuyerName} has no room for the act now.";
			if (!SceneRecruitmentService.CanAccess(buyer, artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0))
				return $"{offer.BuyerName} doesn't work {artist.stageName}'s scene.";
		}
		return null;
	}

	/// <summary>The player's pressed copies of a title (office, shops) and runs still at the plant, as the buyer takes them.</summary>
	public (int Copies, float Value) MasterStockValue(string recordId) {
		int copies = (StockFor(recordId) is { } st ? st.Remaining + st.PromoRemaining : 0)
			+ EnsureStops().Values.Sum(s => s.OnHand.TryGetValue(recordId, out var lot) ? Math.Max(0, lot.Remaining) : 0);
		float runs = pressOrders.Where(o => o.RecordId == recordId).Sum(o => o.Cost);
		return (copies, MasterDeal.R2(copies * MasterStockPerCopy + runs));
	}

	/// <summary>Signs one of the offer's contracts. The advance (or price) and the stock money are paid over today, out of
	/// the buyer's own cash; the title leaves every player channel; an unreleased master goes to the buyer to release.</summary>
	public bool AcceptMasterOffer(string offerId, int termIndex, out string message) {
		var offer = masterOffers.FirstOrDefault(o => o.Id == offerId);
		message = MasterSignBlock(offer, termIndex);
		if (message != null) return false;
		if (!Require(MasterSignHours, out message)) return false;
		var buyer = CompetitorManager.Instance.GetLabel(offer.BuyerLabelId);
		var t = offer.Terms[termIndex];
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = TodayIndex();
		var artist = ArtistManager.Instance?.GetArtist(offer.ArtistId);
		if (t.Kind == MasterDealKind.SaleWithAct
				&& RosterManager.Instance?.SignPlayerActToMasterBuyer(artist, Label, buyer, t.ActTerms.Value, today) != true) {
			message = $"{offer.BuyerName} couldn't take {artist?.stageName ?? "the act"} on after all.";
			return false;
		}
		Spend(MasterSignHours);

		var deal = new MasterDeal {
			Id = $"md-{offer.RecordId}-{day}-{++masterOrdinal}", BuyerLabelId = buyer.labelId, BuyerName = buyer.labelName, RecordId = offer.RecordId,
			Title = offer.Title, ArtistId = offer.ArtistId, ArtistName = artist?.stageName, Terms = t, PickedUp = offer.Released,
			Read = offer.Read, SignedDay = day,
		};
		deal.Add(day, t.Kind == MasterDealKind.Sale ? MasterLedgerKind.Price : MasterLedgerKind.Advance, t.Advance);
		var (copies, stockValue) = HandOverStock(offer.RecordId);
		deal.StockCopies = copies; deal.StockValue = stockValue;
		if (stockValue > 0f) deal.Add(day, MasterLedgerKind.Stock, stockValue, copies);
		float cash = t.Advance + stockValue;
		CompetitorManager.Instance.RecordExpense(buyer, cash);
		Label.cashReserves += cash;
		Label.monthlyRevenue += cash;

		if (offer.Released) {
			var runtime = ReleasedRecords.First(r => r.baseRecord?.recordId == offer.RecordId);
			CompetitorManager.Instance.TransferRecordToLabel(runtime, buyer, deal.Id);
			StartDealTerm(deal, day);
		} else {
			var master = masters.First(m => m.Record?.recordId == offer.RecordId);
			foreach (var p in planned.Where(p => p.Master == master || p.BSide == master).ToList()) {
				planned.Remove(p);
				if (p.Master != null) p.Master.Scheduled = false;
				if (p.BSide != null) p.BSide.Scheduled = false;
			}
			masters.Remove(master);
			heldMasters[deal.Id] = master;
			ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
			deal.ReleaseDay = day + MasterBuyerPlantMinDays + (int)(StableUnit($"{seed}|{deal.Id}|MasterBuyerPlant") * MasterBuyerPlantSpanDays);
			deal.ReleaseByDay = day + t.ReleaseWithinDays;
			deal.Status = MasterDealStatus.AwaitingRelease;
		}
		masterDeals.Add(deal);
		foreach (var other in masterOffers.Where(o => o.RecordId == offer.RecordId).ToList()) {
			masterOffers.Remove(other);
			if (other != offer) Note($"Told {other.BuyerName} \"{other.Title}\" has gone elsewhere.");
		}
		masterPostings.RemoveAll(p => p.RecordId == offer.RecordId);

		string what = t.Kind switch {
			MasterDealKind.Lease => $"leased \"{offer.Title}\" to {buyer.labelName} for ${t.Advance:N0} against {t.RoyaltyRate:P0} of list",
			MasterDealKind.Sale => $"sold \"{offer.Title}\" to {buyer.labelName} for ${t.Advance:N0}",
			_ => $"sold \"{offer.Title}\" and {artist?.stageName}'s contract to {buyer.labelName}, ${t.Advance:N0} against your {t.RoyaltyRate:P0} producer royalty",
		};
		message = $"Signed: {what}." + (copies > 0 ? $" They took your {copies:N0} copies at ${MasterStockPerCopy:F2}" : "") +
			(stockValue > copies * MasterStockPerCopy + 0.01f ? " and the run at the plant." : copies > 0 ? "." : "") +
			(offer.Released ? " Their people take it from here." : $" They'll have it out by {LocalSceneRoomService.Date(deal.ReleaseDay).ToShortString()}.");
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Moves the player's stock of a title to the buyer: copies at the office and in shops, runs at the plant, and
	/// the calls and orders its salesmen now answer. Returns what was handed over and its value.</summary>
	private (int Copies, float Value) HandOverStock(string recordId) {
		var value = MasterStockValue(recordId);
		inventory.Remove(recordId);
		foreach (var stop in EnsureStops().Values) stop.OnHand.Remove(recordId);
		pressOrders.RemoveAll(o => o.RecordId == recordId);
		inboundCalls.RemoveAll(c => c.RecordId == recordId);
		pendingMailings.RemoveAll(m => m.RecordId == recordId);
		weeklyTrunkUnits.Remove(recordId);
		acetateCopies.Remove(recordId);
		return value;
	}

	private void StartDealTerm(MasterDeal deal, int day) {
		deal.ReleasedDay = day;
		deal.NextStatementDay = day + MasterStatementDays;
		if (deal.Terms.Kind == MasterDealKind.Lease) { deal.Status = MasterDealStatus.Out; deal.ExpiresDay = day + deal.Terms.TermWeeks * 7; }
		else deal.Status = MasterDealStatus.Permanent;
		deal.Add(day, MasterLedgerKind.Released, 0f);
	}

	public bool DeclineMasterOffer(string offerId, out string message) {
		var offer = masterOffers.FirstOrDefault(o => o.Id == offerId);
		if (offer == null) { message = "No such offer."; return false; }
		masterOffers.Remove(offer);
		message = $"Passed on {offer.BuyerName}'s offer for \"{offer.Title}\".";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	// ── The buyer's weekly settlement (called from CompetitorManager for tagged records) ─────────────

	public readonly record struct MasterDealWeek(float ArtistPayment, float ArtistRecouped, float Expense);

	/// <summary>
	/// One week's units of a tagged record, as the buyer's settlement bills them. Accrues the player's royalty (recouping
	/// the advance first) and decides the act's royalty leg: on a lease the player owes it (settled on the statement), on
	/// a sale the buyer pays it at the act's contract rate, with the act the buyer's own contract runs as for any act.
	/// The buyer's expense is what it now owes: the royalty after recoupment, plus whatever act royalty it bears.
	/// </summary>
	internal MasterDealWeek SettleMasterDealWeek(string dealId, SimulatedArtist artist, int units, float artistPayment, float recouped, float royaltyExpense) {
		var d = DealById(dealId);
		if (d == null || !(d.Status is MasterDealStatus.Out or MasterDealStatus.Permanent) || units <= 0)
			return d?.Terms.Kind == MasterDealKind.SaleWithAct || d == null ? new(artistPayment, recouped, royaltyExpense) : new(0f, 0f, 0f);
		int day = TodayIndex();
		float earned = MasterDeal.R2(units * d.Terms.CentsPerUnit);
		float rec = d.Terms.Recoupable ? Mathf.Min(d.Unrecouped, earned) : 0f;
		d.Units += units; d.Earned = MasterDeal.R2(d.Earned + earned); d.Recouped = MasterDeal.R2(d.Recouped + rec);
		d.Add(day, MasterLedgerKind.Earned, earned, units);
		if (rec > 0f) d.Add(day, MasterLedgerKind.Recouped, rec);
		float payable = MasterDeal.R2(earned - rec);
		switch (d.Terms.Kind) {
			case MasterDealKind.Lease:
				d.ActUnitsUnsettled += units;
				return new(0f, 0f, payable);
			case MasterDealKind.Sale:
				float act = MasterDeal.R2(units * RoyaltyPerUnit(artist));
				return new(act, 0f, act);
			default:
				return new(artistPayment, recouped, royaltyExpense + payable);
		}
	}

	// ── The clock ───────────────────────────────────────────────────────────────────────────────────

	/// <summary>Dawn: answers to posted tapes, lapsed offers, the buyer's release, statements, reversion, and a regional
	/// pickup's phone call.</summary>
	private void ProcessMasterDealsDawn(GameDate date) {
		if (Label == null) return;
		int today = TodayIndex();
		foreach (var o in masterOffers.Where(o => o.ExpiresDay < today).ToList()) {
			masterOffers.Remove(o);
			Note($"{o.BuyerName}'s offer for \"{o.Title}\" lapsed.");
		}
		ResolveMasterPostings(today);
		foreach (var d in masterDeals.Where(d => d.Live).ToList()) {
			var buyer = CompetitorManager.Instance?.GetLabel(d.BuyerLabelId);
			bool buyerUp = buyer != null && buyer.IsActive;
			if (d.Status == MasterDealStatus.AwaitingRelease) {
				if (today >= d.ReleaseDay && buyerUp && heldMasters.TryGetValue(d.Id, out var held)
						&& CompetitorManager.Instance.ReleaseLicensedPlayerMaster(buyer, ArtistManager.Instance?.GetArtist(d.ArtistId), held.Record, d.Id, d.Read, date)) {
					heldMasters.Remove(d.Id);
					StartDealTerm(d, today);
					Note($"RELEASED: \"{d.Title}\" is out on {d.BuyerName}, on their pressing and their campaign.");
				} else if (!buyerUp || today >= d.ReleaseByDay) FailMasterDeal(d, today, buyerUp ? $"{d.BuyerName} never put it out" : $"{d.BuyerName} went under before putting it out");
				continue;
			}
			if (today >= d.NextStatementDay) { MasterStatement(d, today); d.NextStatementDay += MasterStatementDays; }
			if (d.Status == MasterDealStatus.Out && (today >= d.ExpiresDay || !buyerUp)) RevertMasterDeal(d, today, buyerUp ? "the lease ran out" : $"{d.BuyerName} went under");
		}
		CheckMasterPickups(date);
	}

	private void ResolveMasterPostings(int today) {
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		foreach (var p in masterPostings.Where(p => !p.NoReply && today >= p.AnswerDay).ToList()) {
			var buyer = CompetitorManager.Instance?.GetLabel(p.BuyerLabelId);
			if (buyer == null || !buyer.IsActive || ShoppableMaster(p.RecordId).Block != null) { masterPostings.Remove(p); continue; }
			string evidence = MasterEvidence(p.RecordId);
			bool heard = StableUnit($"{seed}|{p.BuyerLabelId}|{p.RecordId}|{evidence}|{p.SentDay}|MasterPostHeard") < (p.Solicited ? MasterPostHeardSolicited : MasterPostHeardUnsolicited);
			if (!heard) { p.NoReply = true; Note($"No word back from {buyer.labelName} about \"{TitleForRecord(p.RecordId)}\". Most tapes never get played."); continue; }
			masterPostings.Remove(p);
			var (pass, offer) = EvaluateMaster(buyer, p.RecordId, MasterRoute.Post);
			RecordMasterAnswer(buyer, p.RecordId, pass, offer);
			Note($"A letter from {buyer.labelName} about \"{TitleForRecord(p.RecordId)}\": " +
				(pass ?? $"{offer.Answer} {OfferSummary(offer)} Open until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}."));
			if (offer != null) FlagSkipStop($"{buyer.labelName} wrote about \"{TitleForRecord(p.RecordId)}\"", EventType.IncomingCall, officeOnly: true);
		}
	}

	/// <summary>A statement: what the buyer owes is paid over, and on a lease the act's royalty on those units is settled
	/// against its advance, the rest paid by the player.</summary>
	private void MasterStatement(MasterDeal d, int today) {
		float pay = d.OwedNow;
		if (pay > 0f) {
			d.Paid = MasterDeal.R2(d.Paid + pay);
			d.Add(today, MasterLedgerKind.Statement, pay);
			Label.cashReserves += pay;
			Label.monthlyRevenue += pay;
		}
		float toAct = 0f;
		if (d.Terms.Kind == MasterDealKind.Lease && d.ActUnitsUnsettled > 0) {
			var artist = ArtistManager.Instance?.GetArtist(d.ArtistId);
			float royalty = MasterDeal.R2(d.ActUnitsUnsettled * RoyaltyPerUnit(artist));
			float rec = artist != null ? MasterDeal.R2(Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), royalty)) : 0f;
			toAct = MasterDeal.R2(royalty - rec);
			if (artist != null) { artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - rec); artist.totalRoyaltyEarnings += toAct; }
			if (rec > 0f) { d.ActRecouped = MasterDeal.R2(d.ActRecouped + rec); d.Add(today, MasterLedgerKind.ActRecouped, rec, d.ActUnitsUnsettled); }
			if (toAct > 0f) { d.ActPaid = MasterDeal.R2(d.ActPaid + toAct); d.Add(today, MasterLedgerKind.ActRoyalty, toAct, d.ActUnitsUnsettled); Label.cashReserves -= toAct; Label.monthlyExpenses += toAct; }
			d.ActUnitsUnsettled = 0;
		}
		if (d.Terms.Kind != MasterDealKind.Sale)
			Note($"{d.BuyerName}'s statement on \"{d.Title}\": {d.Units:N0} copies to date, ${d.Earned:N2} earned, ${d.Recouped:N2} against the advance" +
				(pay > 0f ? $", ${pay:N2} paid to you" : ", nothing more due yet") + (toAct > 0f ? $"; ${toAct:N2} of it to {d.ArtistName}" : "") + ".");
	}

	private void RevertMasterDeal(MasterDeal d, int today, string why) {
		MasterStatement(d, today);
		var runtime = ChartManager.Instance?.GetRecordRuntimeData(d.RecordId);
		bool live = runtime?.baseRecord?.playerMasterDealId == d.Id;
		if (live) CompetitorManager.Instance?.TransferRecordToLabel(runtime, Label, null);
		d.Status = MasterDealStatus.Reverted;
		d.EndNote = why;
		d.Add(today, MasterLedgerKind.Reverted, 0f);
		Note(live ? $"\"{d.Title}\" is yours again: {why}. Their copies come off the shelves; press and sell it yourself."
			: $"\"{d.Title}\" is yours again ({why}), but it had already run its course on their books.");
	}

	private void FailMasterDeal(MasterDeal d, int today, string why) {
		if (heldMasters.Remove(d.Id, out var held)) masters.Add(held);
		d.Status = MasterDealStatus.Failed;
		d.EndNote = why;
		d.Add(today, MasterLedgerKind.Failed, 0f);
		Note($"\"{d.Title}\" comes back to your shelf: {why}. The advance is yours to keep.");
	}

	/// <summary>§5.3: a released title breaking out in a new market may bring the best-fitting buyer's call. Once per
	/// change of evidence, on a fixed draw, and only from a buyer that would hear a record in it.</summary>
	private void CheckMasterPickups(GameDate date) {
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		foreach (var rec in ReleasedRecords.Where(r => r.baseRecord?.format == ReleaseFormat.Single).ToList()) {
			string id = rec.baseRecord.recordId;
			int markets = BreakoutMarkets(rec);
			if (markets == 0 || IsMasterOut(id) || masterOffers.Any(o => o.RecordId == id)) continue;
			string evidence = MasterEvidence(id);
			if (masterPickupEvidence.TryGetValue(id, out var seen) && seen == evidence) continue;
			masterPickupEvidence[id] = evidence;
			var best = MasterBuyersFor(id, int.MaxValue).Where(b => b.Block == null && StandingPass(b.Label.labelId, id) == null)
				.Select(b => (b, Read: MasterRead(b.Label, rec.baseRecord, false), Bar: MasterBar(b.Label, b.OwnSound)))
				.Where(x => x.Read >= x.Bar)
				.OrderByDescending(x => Mathf.InverseLerp(x.Bar, x.Bar + 0.25f, x.Read)).ThenBy(x => x.b.Label.labelId, StringComparer.Ordinal)
				.FirstOrDefault();
			if (best.b == null) continue;
			float s = Mathf.InverseLerp(best.Bar, best.Bar + 0.25f, best.Read);
			float chance = Mathf.Clamp(0.35f + 0.15f * (markets - 1) + 0.30f * s, 0f, 0.85f);
			if (StableUnit($"{seed}|{id}|{evidence}|MasterPickup") >= chance) continue;
			var (pass, offer) = EvaluateMaster(best.b.Label, id, MasterRoute.Inbound);
			if (offer == null) continue;
			masterOffers.Add(offer);
			Note($"PHONE: {offer.BuyerName} called about \"{offer.Title}\" -- they've seen what it's doing. {OfferSummary(offer)} Open until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}.");
			FlagSkipStop($"{offer.BuyerName} wants \"{offer.Title}\"", EventType.IncomingCall, officeOnly: true);
		}
	}

	// ── Cash options ────────────────────────────────────────────────────────────────────────────────

	private IEnumerable<CashOption> MasterDealCashOptions(GameDate today, Func<GameDate?, bool> tooLate, Func<GameDate?, string> late) {
		foreach (var o in MasterOffers) {
			int best = Enumerable.Range(0, o.Terms.Count).OrderByDescending(i => o.Terms[i].Advance).First();
			string block = MasterSignBlock(o, best);
			float stock = MasterStockValue(o.RecordId).Value;
			yield return new("Master offer", $"{o.BuyerName} for \"{o.Title}\"", OfferSummary(o), 0f, o.Terms[best].Advance + stock, today,
				CashCertainty.Accepted, block == null && !tooLate(today),
				(block ?? $"Paid on signing, from the office or at theirs. The title leaves your channels; open until {LocalSceneRoomService.Date(o.ExpiresDay).ToShortString()}.") + late(today));
		}
		foreach (string id in ShoppableMasterIds()) {
			if (masterOffers.Any(o => o.RecordId == id)) continue;
			var buyers = MasterBuyersFor(id).Where(b => b.Block == null && MasterApproachBlock(b.Label, id) == null).ToList();
			if (buyers.Count == 0) continue;
			var when = AddDays(today, 3);
			yield return new("Shop the master", $"\"{TitleForRecord(id)}\" to {buyers.Count} label{(buyers.Count == 1 ? "" : "s")}", "a call, a posted tape or a visit",
				0f, MasterLeaseAdvanceMin, when, CashCertainty.Speculative, !tooLate(when),
				$"A lease starts around ${MasterLeaseAdvanceMin:N0}, if they hear a record. Posted tapes take weeks; a visit needs the trip." + late(when));
		}
	}

	// ── Checks ──────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Checks and the save round-trip only: put state in place without the routes.</summary>
	internal void StageMasterOfferForChecks(MasterDealOffer offer) => masterOffers.Add(offer);
	internal void StageMasterDealForChecks(MasterDeal deal, Master held = null) { masterDeals.Add(deal); if (held != null) heldMasters[deal.Id] = held; }
	internal void StageMasterPassForChecks(MasterPass pass) => masterPasses.Add(pass);
	internal void StageMasterPostingForChecks(MasterPosting post) => masterPostings.Add(post);
	internal bool HoldsMasterForChecks(string dealId) => heldMasters.ContainsKey(dealId);
	internal void ProcessMasterDealsForChecks() => ProcessMasterDealsDawn(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
	internal float MasterReadForChecks(AILabel buyer, string recordId, bool posted) {
		var (record, _, _) = ShoppableMaster(recordId);
		return record == null ? -1f : MasterRead(buyer, record, posted);
	}
	internal static float MasterBarForChecks(AILabel buyer, bool ownSound) => MasterBar(buyer, ownSound);

	/// <summary>Checks only: a finished master on the shelf for one of the roster's acts, without a studio session.</summary>
	internal Master StageShelfMasterForChecks(SimulatedArtist act, string recordId, string title, float hook, float production) {
		var record = new Record {
			recordId = recordId, title = title, artistId = act.artistId, artistName = act.stageName, labelId = Label.labelId,
			isPlayerOwned = true, format = ReleaseFormat.Single, primaryGenre = act.primaryGenre, secondaryGenre = act.secondaryGenre,
			hookStrength = hook, productionQuality = production, danceability = .5f, masterId = recordId,
		};
		var master = new Master { Record = record, ArtistId = act.artistId, SongTitle = title, ProductionCost = 60f,
			Cut = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate };
		masters.Add(master);
		return master;
	}

	/// <summary>Checks only: put a shelf master out today, through the same release the clock fires.</summary>
	internal void ReleaseMasterForChecks(Master master) {
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		FireRelease(new PlannedRelease { Master = master, Dated = true, Date = today }, today);
	}

	/// <summary>Checks only: copies of a title in a shop and an open call for it, as the trunk would leave them.</summary>
	internal void StageFieldStockForChecks(string recordId, PlayerStop stop, int copies) {
		stop.OnHand[recordId] = new ConsignmentLot { Remaining = copies, Placed = copies };
		inboundCalls.Add(new InboundCall { StopId = stop.StopId, RecordId = recordId, RequestedQty = 25, Reason = InboundCallReason.SoldOut,
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0, ExpiresWeek = (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + 4 });
	}
	internal int OpenCallsForChecks(string recordId) => inboundCalls.Count(c => c.RecordId == recordId);

	internal MasterDealWeek SettleMasterDealWeekForChecks(string dealId, SimulatedArtist artist, int units, float artistPayment, float recouped, float expense) =>
		SettleMasterDealWeek(dealId, artist, units, artistPayment, recouped, expense);
	internal (string Pass, MasterDealOffer Offer) EvaluateMasterForChecks(AILabel buyer, string recordId, MasterRoute route) => EvaluateMaster(buyer, recordId, route);

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private MasterDealsSaveData CaptureMasterDeals() {
		if (masterOffers.Count + masterDeals.Count + masterPasses.Count + masterPostings.Count + masterCalls.Count + masterPickupEvidence.Count == 0) return null;
		return new MasterDealsSaveData {
			Offers = LocalSceneRoomService.Copy(masterOffers), Deals = LocalSceneRoomService.Copy(masterDeals),
			Passes = LocalSceneRoomService.Copy(masterPasses), Postings = LocalSceneRoomService.Copy(masterPostings),
			Calls = LocalSceneRoomService.Copy(masterCalls),
			PickupEvidence = new Dictionary<string, string>(masterPickupEvidence), Ordinal = masterOrdinal,
			Held = heldMasters.Select(kv => new HeldMasterSaveData { DealId = kv.Key, Master = MasterSaveData.From(kv.Value) }).ToList(),
		};
	}

	private void RestoreMasterDeals(MasterDealsSaveData data) {
		masterOffers.Clear(); masterDeals.Clear(); masterPasses.Clear(); masterPostings.Clear(); masterCalls.Clear();
		masterPickupEvidence.Clear(); heldMasters.Clear(); masterOrdinal = 0;
		if (data == null) return;
		masterOffers.AddRange(data.Offers ?? new());
		masterDeals.AddRange((data.Deals ?? new()).Where(d => d?.Terms != null));
		masterPasses.AddRange(data.Passes ?? new());
		masterPostings.AddRange(data.Postings ?? new());
		masterCalls.AddRange(data.Calls ?? new());
		foreach (var kv in data.PickupEvidence ?? new()) masterPickupEvidence[kv.Key] = kv.Value;
		masterOrdinal = data.Ordinal;
		foreach (var h in data.Held ?? new()) if (h?.DealId != null && h.Master != null) heldMasters[h.DealId] = h.Master.ToMaster();
		foreach (var d in masterDeals.Where(d => !d.LedgerAgrees()))
			GD.PushWarning($"Master deal {d.Id} ledger disagrees with its saved balances; keeping the saved balances.");
	}
}

/// <summary>Early-game Phase 5: offers, deals and their ledgers, passes, tapes in the post, calls, pickup evidence and the
/// unreleased masters a buyer holds. Absent in older saves.</summary>
public sealed class MasterDealsSaveData {
	public List<MasterDealOffer> Offers { get; set; }
	public List<MasterDeal> Deals { get; set; }
	public List<MasterPass> Passes { get; set; }
	public List<MasterPosting> Postings { get; set; }
	public List<MasterCall> Calls { get; set; }
	public Dictionary<string, string> PickupEvidence { get; set; }
	public int Ordinal { get; set; }
	public List<HeldMasterSaveData> Held { get; set; }
}

public sealed class HeldMasterSaveData {
	public string DealId { get; set; }
	public MasterSaveData Master { get; set; }
}
