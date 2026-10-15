using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum SongPlacementStatus { Held, Out, Lapsed, Ended }
public enum SongPlacementLedgerKind { Advance, Cut, Earned, Recouped, Statement, Lapsed, Ended }

/// <summary>An open offer from one label to have one of its acts cut one of the player's songs.</summary>
public sealed class SongPlacementOffer {
	public string Id { get; set; }
	public string BuyerLabelId { get; set; }
	public string BuyerName { get; set; }
	public string SongId { get; set; }
	public string Title { get; set; }
	public MasterRoute Route { get; set; }
	public int MadeDay { get; set; }
	public int ExpiresDay { get; set; }
	public float Read { get; set; }
	public string Answer { get; set; }
	public float Advance { get; set; }
	public int HoldDays { get; set; }
	/// <summary>True when the player's own record of the song is already out, so their cut is a cover of it.</summary>
	public bool Cover { get; set; }
	public string Why { get; set; }
}

public sealed class SongPlacementLedgerEntry {
	public int Day { get; set; }
	public SongPlacementLedgerKind Kind { get; set; }
	public float Amount { get; set; }
	public int Units { get; set; }
}

/// <summary>A signed song placement. Running balances are recomputable from <see cref="Ledger"/>.</summary>
public sealed class SongPlacement {
	public string Id { get; set; }
	public string BuyerLabelId { get; set; }
	public string BuyerName { get; set; }
	public string SongId { get; set; }
	public string Title { get; set; }
	public float Advance { get; set; }
	public SongPlacementStatus Status { get; set; }
	public float Read { get; set; }
	public int SignedDay { get; set; }
	public int HoldUntilDay { get; set; }
	/// <summary>The buyer's record cut from the song, made but not yet confirmed out (an AI single can be generated and
	/// then dropped for want of cash; the cut only counts once it is in the shops).</summary>
	public string PendingRecordId { get; set; }
	public int PendingDay { get; set; }
	public string RecordId { get; set; }
	public string ArtistId { get; set; }
	public string ArtistName { get; set; }
	public int? ReleasedDay { get; set; }
	public int NextStatementDay { get; set; }
	public int Units { get; set; }
	public float Earned { get; set; }
	public float Recouped { get; set; }
	public float Paid { get; set; }
	public string EndNote { get; set; }
	public List<SongPlacementLedgerEntry> Ledger { get; set; } = new();

	public float Unrecouped => Mathf.Max(0f, MasterDeal.R2(Advance - Recouped));
	public float Payable => MasterDeal.R2(Earned - Recouped);
	public float OwedNow => Mathf.Max(0f, MasterDeal.R2(Payable - Paid));
	public bool Live => Status is SongPlacementStatus.Held or SongPlacementStatus.Out;

	internal void Add(int day, SongPlacementLedgerKind kind, float amount, int units = 0) =>
		Ledger.Add(new SongPlacementLedgerEntry { Day = day, Kind = kind, Amount = MasterDeal.R2(amount), Units = units });

	public bool LedgerAgrees() {
		float Sum(SongPlacementLedgerKind k) => MasterDeal.R2(Ledger.Where(e => e.Kind == k).Sum(e => e.Amount));
		return Ledger.Where(e => e.Kind == SongPlacementLedgerKind.Earned).Sum(e => e.Units) == Units
			&& Math.Abs(Sum(SongPlacementLedgerKind.Advance) - Advance) < 0.011f
			&& Math.Abs(Sum(SongPlacementLedgerKind.Earned) - Earned) < 0.011f && Math.Abs(Sum(SongPlacementLedgerKind.Recouped) - Recouped) < 0.011f
			&& Math.Abs(Sum(SongPlacementLedgerKind.Statement) - Paid) < 0.011f && Recouped <= Advance + 0.011f && Paid <= Payable + 0.011f;
	}
}

public sealed class SongPlacementPass {
	public string BuyerLabelId { get; set; }
	public string SongId { get; set; }
	public int Day { get; set; }
	public string Evidence { get; set; }
	public string Reason { get; set; }
}

public sealed class SongPlacementPosting {
	public string BuyerLabelId { get; set; }
	public string SongId { get; set; }
	public int SentDay { get; set; }
	public int AnswerDay { get; set; }
	public bool Solicited { get; set; }
	public bool NoReply { get; set; }
}

public sealed class SongPlacementCall {
	public string BuyerLabelId { get; set; }
	public string SongId { get; set; }
	public int Day { get; set; }
	public string Result { get; set; }
	public int? AppointmentDay { get; set; }
	public bool Welcome { get; set; }
}

/// <summary>
/// Early-game directive V2, Phase 5A (§7.2): place a song. When the label controls a song's publishing, the founder can
/// plug it to a real AI label for one of its own acts to record. It is the same contact work as shopping a master (a call
/// from the office, a posted tape, a visit with an acetate) and the same buyers' offices, but the label is buying a song
/// for its own session, not a finished record.
///
/// <b>What can be placed.</b> A composition in the catalogue whose publishing the player's label controls (its own act's
/// song under a contract where the label keeps the publishing), with a master the player has cut of it to play from. Songs
/// the act kept, commissioned songs and covers belong to someone else and can't be placed. Song rights are separate from
/// the master: a master sold or leased away leaves its song the player's to place.
///
/// <b>Buyers</b> are active AI labels with an act on the roster in the song's sound (its genre, or the same family), that
/// can find an advance. The buyer's ear reads the song (its craft and hook, never the recording) through its own fixed
/// noise, plus the evidence of the player's own record of it if that is out (breakout markets, copies sold). A pass stands
/// until that evidence changes.
///
/// <b>Contract</b> (authored, not sourced; the advance reference is §7.2's $25-100):
/// - An advance of $25-100, paid by the label on signing out of its own cash, against the song's income from its record.
/// - A hold: for 182 days the song is that label's to cut first, and the player can't place it anywhere else meanwhile.
///   The label's act cuts it as its next single, when it next makes one and would have recorded an outside song (an act
///   about to record its own song keeps to it). No cut in the hold: the hold lapses and the advance is kept.
/// - The song's income is the publishing slice the buyer's settlement already pays out of every record's gross
///   (<see cref="CompetitorManager.PublishingShareOfGross"/>). It is routed off the song's rights exactly as for any other
///   record of a song an in-game label controls; this contract only recoups the advance from it first (the buyer keeps that
///   part) and pays the rest on a quarterly statement from the record's release. No separate mechanical is charged: an AI
///   record's composition cost is that slice. Under the act's contract the label keeps the publishing, so, as on the
///   label's own sales, no separate writer's share is paid out of it.
/// - The buyer owns its own master. Publishing is never transferred. A cut of a song the player has already released is a
///   cover, so the existing cover machinery (the trades notice, the market's competition between versions) applies.
///
/// Placements are injected explicitly in <see cref="CompetitorManager"/>'s single generation, after the AI's own material
/// choice: a world with no player has no placements, so its material draws are untouched.
/// </summary>
public partial class PlayerDesk {
	public const float PlacementAdvanceMin = 25f;
	public const float PlacementAdvanceMax = 100f;
	public const int PlacementHoldDays = 182;
	public const int PlacementOfferDays = 14;
	public const int PlacementLetterOfferDays = 21;
	public const int PlacementStatementDays = 91;
	public const float PlacementNearSoundBar = 0.04f;

	private readonly List<SongPlacementOffer> placementOffers = new();
	private readonly List<SongPlacement> placements = new();
	private readonly List<SongPlacementPass> placementPasses = new();
	private readonly List<SongPlacementPosting> placementPostings = new();
	private readonly List<SongPlacementCall> placementCalls = new();
	private int placementOrdinal;

	public IReadOnlyList<SongPlacementOffer> PlacementOffers => placementOffers.Where(o => o.ExpiresDay >= TodayIndex()).ToList().AsReadOnly();
	public IReadOnlyList<SongPlacement> Placements => placements.AsReadOnly();
	public IReadOnlyList<SongPlacementPass> PlacementPasses => placementPasses.AsReadOnly();
	public IReadOnlyList<SongPlacementPosting> PlacementPostings => placementPostings.AsReadOnly();
	public IReadOnlyList<SongPlacementCall> PlacementCalls => placementCalls.AsReadOnly();
	private SongPlacement PlacementById(string id) => id == null ? null : placements.FirstOrDefault(p => p.Id == id);

	// ── What can be placed, and with whom ─────────────────────────────────────────────────────────

	private bool LabelControls(SongComposition song) =>
		song?.rights != null && Label != null && song.rights.controllerLabelId == Label.labelId
		&& song.rights.controlType is PublishingControlType.LabelAffiliate or PublishingControlType.LabelBuyout;

	/// <summary>A record the player cut of this song, to dub a tape from: on the shelf, out, or out on a master deal.</summary>
	private string TapeOfSong(string songId) =>
		masters.Where(m => m.Record?.songId == songId).Select(m => m.Record.recordId)
			.Concat(ReleasedRecords.Where(r => r.baseRecord?.songId == songId).Select(r => r.baseRecord.recordId))
			.Concat(heldMasters.Values.Where(m => m.Record?.songId == songId).Select(m => m.Record.recordId))
			.Concat(masterDeals.Where(d => ChartManager.Instance?.GetRecordRuntimeData(d.RecordId)?.baseRecord?.songId == songId).Select(d => d.RecordId))
			.FirstOrDefault();

	/// <summary>The player's own record of the song that is out (in the player's catalogue or on a master deal), or null.</summary>
	private RecordRuntimeData PlayerRecordOfSong(string songId) =>
		ReleasedRecords.FirstOrDefault(r => r.baseRecord?.songId == songId)
		?? masterDeals.Select(d => ChartManager.Instance?.GetRecordRuntimeData(d.RecordId)).FirstOrDefault(r => r?.baseRecord?.songId == songId);

	private SongPlacement HeldPlacement(string songId) => placements.FirstOrDefault(p => p.SongId == songId && p.Status == SongPlacementStatus.Held);

	/// <summary>A song the player could plug today: its publishing is the label's, there's a tape of it, and no label holds it.
	/// Null with a reason otherwise.</summary>
	public (SongComposition Song, string Block) PlaceableSong(string songId) {
		if (Label == null) return (null, "You don't have a label yet.");
		var song = string.IsNullOrEmpty(songId) ? null : CompositionCatalogService.GetSong(songId);
		if (song == null) return (null, "Pick a song.");
		if (!LabelControls(song)) return (null, $"\"{song.title}\"'s publishing isn't yours to place.");
		if (TapeOfSong(songId) == null) return (null, $"You haven't cut \"{song.title}\"; there's nothing to play anyone.");
		if (HeldPlacement(songId) is { } held)
			return (null, $"{held.BuyerName} holds \"{song.title}\" until {LocalSceneRoomService.Date(held.HoldUntilDay).ToShortString()}.");
		return (song, null);
	}

	/// <summary>Songs the player could plug now, newest cut first.</summary>
	public IEnumerable<string> PlaceableSongIds() =>
		CompositionCatalogService.SongsControlledBy(Label?.labelId)
			.Where(s => PlaceableSong(s.songId).Block == null).OrderByDescending(s => s.originYear).Select(s => s.songId)
			.Distinct(StringComparer.Ordinal);

	public sealed record PlacementBuyer(AILabel Label, string CityId, string CityName, bool OwnSound, bool Reachable, string Block);

	/// <summary>Whether an act's sound takes the song: exactly its genre (own), the same family (near), or neither (null).</summary>
	private static bool? ActFit(SimulatedArtist act, SongComposition song, int year) {
		if (act == null || song == null || !act.isActive || act.members == null || act.members.Count == 0) return null;
		if (act.primaryGenre == song.primaryGenre) return true;
		var a = FamilyOf(act.primaryGenre, year);
		return a != null && a == FamilyOf(song.primaryGenre, year) ? false : null;
	}

	/// <summary>Whether any act on the label's roster could sing it: true for one in the song's own genre, false for one
	/// only in its family, null for none.</summary>
	private static bool? RosterFit(AILabel buyer, SongComposition song, int year) {
		bool? best = null;
		foreach (var act in buyer.roster ?? new List<SimulatedArtist>()) {
			bool? fit = ActFit(act, song, year);
			if (fit == true) return true;
			if (fit == false) best = false;
		}
		return best;
	}

	/// <summary>Real labels that would take a call about this song: active, an act in its sound, and the cash for an
	/// advance. Public facts only -- nothing here reads the song. Best fits first.</summary>
	public IReadOnlyList<PlacementBuyer> PlacementBuyersFor(string songId, int max = 8) {
		var (song, block) = PlaceableSong(songId);
		if (song == null || block != null || CompetitorManager.Instance == null) return Array.Empty<PlacementBuyer>();
		int year = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).year;
		var home = DistanceModel.GetCityById(Label.homeCityId);
		return CompetitorManager.Instance.GetOperatingLabels()
			.Where(l => l != null && !l.isPlayerOwned && l.IsActive)
			.Select(l => (Label: l, Fit: RosterFit(l, song, year)))
			.Where(x => x.Fit.HasValue)
			.Select(x => {
				var city = DistanceModel.GetCityById(x.Label.homeCityId);
				string why = x.Label.CanAffordToSign(PlacementAdvanceMin) ? null : "not taking songs this season";
				return new PlacementBuyer(x.Label, city?.cityId, city?.name ?? x.Label.headquartersCity ?? "out of town", x.Fit.Value,
					city != null && home != null && (city.cityId == home.cityId || CanReach(home, city)), why);
			})
			.OrderBy(b => b.Block == null ? 0 : 1).ThenBy(b => b.OwnSound ? 0 : 1).ThenBy(b => (int)b.Label.tier)
			.ThenBy(b => b.Label.labelId, StringComparer.Ordinal)
			.Take(max).ToList();
	}

	private static float PlacementBar(AILabel buyer, bool ownSound) => buyer.tier switch {
		LabelTier.Major => .62f, LabelTier.MidTier => .58f, LabelTier.Independent => .54f, _ => .50f,
	} + (ownSound ? 0f : PlacementNearSoundBar);

	/// <summary>What a buyer's answer on a song rests on: a pass stands while this is unchanged.</summary>
	public string PlacementEvidence(string songId) {
		var rec = PlayerRecordOfSong(songId);
		return rec == null ? "unreleased" : $"released|breakouts={BreakoutMarkets(rec)}";
	}

	/// <summary>The buyer's own read of the song: its craft and hook through its ear (a fixed draw per buyer and song, wider
	/// off a posted tape), plus the evidence of the player's record of it if that's out. Never the hidden score itself.</summary>
	private float PlacementRead(AILabel buyer, SongComposition song, bool posted) {
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		float song01 = song.GetCraftScore() * 0.5f + Mathf.Clamp(song.commercialHook, 0f, 1f) * 0.5f;
		float noise = (StableUnit($"{seed}|{buyer.labelId}|{song.songId}|PlacementEar") * 2f - 1f)
			* Mathf.Lerp(0.30f, 0.10f, Mathf.Clamp(buyer.scoutingAbility, 0f, 1f)) * (posted ? 1.4f : 1f);
		var rec = PlayerRecordOfSong(song.songId);
		float evidence = rec == null ? 0f : Mathf.Min(0.12f, 0.04f * BreakoutMarkets(rec)) + Mathf.Min(0.08f, rec.totalUnitsSold / 5000f);
		return Mathf.Clamp(song01 + noise + evidence, 0f, 1f);
	}

	private static float Round5(float v) => Mathf.Round(v / 5f) * 5f;

	/// <summary>The buyer's answer on a heard song: a pass (with its words) or its offer. Reads only the evidence.</summary>
	internal (string Pass, SongPlacementOffer Offer) EvaluatePlacement(AILabel buyer, string songId, MasterRoute route) {
		var (song, block) = PlaceableSong(songId);
		if (song == null) return (block, null);
		int year = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).year;
		bool? fit = RosterFit(buyer, song, year);
		if (fit == null) return ($"{buyer.labelName}: \"None of our people sing that kind of song.\"", null);
		float read = PlacementRead(buyer, song, route == MasterRoute.Post);
		float bar = PlacementBar(buyer, fit.Value);
		if (read < bar)
			return ($"{buyer.labelName}: " + (read < bar - 0.10f ? "\"It's a nice enough tune. Nobody here would cut it.\"" : "\"It's close. Not for anybody we have right now.\""), null);
		float s = Mathf.InverseLerp(bar, bar + 0.25f, read);
		float advance = Mathf.Clamp(Round5(Mathf.Lerp(PlacementAdvanceMin, PlacementAdvanceMax, s) * MasterTierScale(buyer)), PlacementAdvanceMin, PlacementAdvanceMax);
		bool cover = PlayerRecordOfSong(songId) != null;
		int today = TodayIndex();
		float cents = CompetitorManager.PublishingShareOfGross * ListPrice * 100f;
		return (null, new SongPlacementOffer {
			Id = $"po-{songId}-{buyer.labelId}-{today}-{++placementOrdinal}", BuyerLabelId = buyer.labelId, BuyerName = buyer.labelName,
			SongId = songId, Title = song.title, Route = route, MadeDay = today, Read = read, Advance = advance, HoldDays = PlacementHoldDays, Cover = cover,
			ExpiresDay = today + (route == MasterRoute.Post ? PlacementLetterOfferDays : PlacementOfferDays),
			Answer = $"{buyer.labelName}: " + (s < 0.33f ? "\"One of our people could do something with that.\"" : s < 0.66f ? "\"That's a song.\"" : "\"I know exactly who should sing that.\""),
			Why = $"${advance:N0} now, against the song's share of their record ({CompetitorManager.PublishingShareOfGross:P0} of their sales, about {cents:F1}c a copy at list), " +
				$"paid quarterly once the advance is back. They hold it {PlacementHoldDays / 7} weeks for one of their {(fit.Value ? "" : "nearby ")}acts to cut first" +
				(cover ? ", as a cover of your record" : "") + "; no cut in that time and the advance is yours anyway. The song stays yours; the record is theirs.",
		});
	}

	private SongPlacementPass StandingPlacementPass(string buyerId, string songId) {
		string evidence = PlacementEvidence(songId);
		return placementPasses.LastOrDefault(p => p.BuyerLabelId == buyerId && p.SongId == songId && p.Evidence == evidence);
	}

	private SongPlacementOffer OpenPlacementOffer(string buyerId, string songId) =>
		placementOffers.FirstOrDefault(o => o.BuyerLabelId == buyerId && o.SongId == songId && o.ExpiresDay >= TodayIndex());

	/// <summary>Why this buyer can't be approached about this song now (any route), or null. Free to ask.</summary>
	private string PlacementApproachBlock(AILabel buyer, string songId) {
		var (song, block) = PlaceableSong(songId);
		if (song == null) return block;
		if (buyer == null || !buyer.IsActive || buyer.isPlayerOwned) return "No such label.";
		if (OpenPlacementOffer(buyer.labelId, songId) != null) return $"{buyer.labelName}'s offer is on your desk.";
		if (placements.Any(p => p.BuyerLabelId == buyer.labelId && p.SongId == songId && p.Status != SongPlacementStatus.Lapsed))
			return $"{buyer.labelName} already has \"{song.title}\".";
		if (StandingPlacementPass(buyer.labelId, songId) is { } pass)
			return $"{buyer.labelName} passed on {LocalSceneRoomService.Date(pass.Day).ToShortString()}. Nothing's changed since.";
		return null;
	}

	private void RecordPlacementAnswer(AILabel buyer, string songId, string pass, SongPlacementOffer offer) {
		if (pass != null) placementPasses.Add(new SongPlacementPass { BuyerLabelId = buyer.labelId, SongId = songId, Day = TodayIndex(), Evidence = PlacementEvidence(songId), Reason = pass });
		else placementOffers.Add(offer);
	}

	private static string PlacementSummary(SongPlacementOffer o) => $"${o.Advance:N0} advance, held {o.HoldDays / 7} weeks for their act.";

	private string SongTitle(string songId) => CompositionCatalogService.GetSong(songId)?.title ?? songId;

	// ── Access routes (the master-shopping routes, for a song) ─────────────────────────────────────

	/// <summary>A call from the office: public answers only (no act for it, not taking songs, or "send it" and an
	/// appointment when their office is in driving range). It never hears the song.</summary>
	public bool CallPlacementBuyer(string buyerId, string songId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = PlacementApproachBlock(buyer, songId);
		if (message != null) return false;
		if (!RequireHome(out message)) { message = "Calls go out from the office."; return false; }
		if (!InOfficeHours(MasterCallHours, out message)) return false;
		int today = TodayIndex();
		var last = placementCalls.LastOrDefault(c => c.BuyerLabelId == buyerId && c.SongId == songId);
		if (last != null && today - last.Day < MasterCallRepeatDays) { message = $"You spoke to {buyer.labelName} on {LocalSceneRoomService.Date(last.Day).ToShortString()}: {last.Result}"; return false; }
		float toll = MasterCallToll(buyer);
		if (Label.cashReserves < toll) { message = $"You're ${toll - Label.cashReserves:N2} short of the long-distance toll."; return false; }
		if (!Require(MasterCallHours, out message)) return false;
		Spend(MasterCallHours);
		Label.cashReserves -= toll;
		Label.monthlyExpenses += toll;

		var b = PlacementBuyersFor(songId, int.MaxValue).FirstOrDefault(x => x.Label.labelId == buyerId);
		var call = new SongPlacementCall { BuyerLabelId = buyerId, SongId = songId, Day = today };
		if (b == null) call.Result = "\"None of our people sing that kind of song.\"";
		else if (b.Block != null) call.Result = $"\"We're {b.Block}.\"";
		else {
			call.Welcome = true;
			if (b.Reachable) {
				ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
				call.AppointmentDay = today + 3 + (int)(StableUnit($"{seed}|{buyerId}|{songId}|{today}|PlacementAppointment") * 5f);
				call.Result = $"\"Send it over, or bring it by on {LocalSceneRoomService.Date(call.AppointmentDay.Value).ToShortString()}.\"";
			} else call.Result = "\"Send it over.\"";
		}
		placementCalls.Add(call);
		message = $"Called {buyer.labelName} in {b?.CityName ?? "their town"} about \"{SongTitle(songId)}\" (${toll:N2}): {call.Result}";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Posts a tape of the song to a label's office. The answer, if anyone listens, comes by letter in two to four weeks.</summary>
	public bool PostPlacementTape(string buyerId, string songId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = PlacementApproachBlock(buyer, songId);
		if (message != null) return false;
		if (!RequireHome(out message)) { message = "Tapes go out from the office."; return false; }
		int today = TodayIndex();
		var pending = placementPostings.LastOrDefault(p => p.BuyerLabelId == buyerId && p.SongId == songId);
		if (pending != null && !pending.NoReply) { message = $"A tape is already on its way to {buyer.labelName}; expect word by {LocalSceneRoomService.Date(pending.AnswerDay).ToShortString()}."; return false; }
		if (pending != null && today - pending.SentDay < MasterPostQuietDays) { message = $"{buyer.labelName} never answered your last tape. Give it until {LocalSceneRoomService.Date(pending.SentDay + MasterPostQuietDays).ToShortString()}."; return false; }
		float cost = MasterTapeDub + MasterPostage;
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N2} short of the dub and postage."; return false; }
		if (!Require(MasterPostHours, out message)) return false;
		Spend(MasterPostHours);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		bool solicited = placementCalls.Any(c => c.BuyerLabelId == buyerId && c.SongId == songId && c.Welcome && today - c.Day <= 60);
		placementPostings.Add(new SongPlacementPosting { BuyerLabelId = buyerId, SongId = songId, SentDay = today, Solicited = solicited,
			AnswerDay = today + MasterPostAnswerMinDays + (int)(StableUnit($"{seed}|{buyerId}|{songId}|{today}|PlacementPostDays") * MasterPostAnswerSpanDays) });
		message = $"Posted a tape of \"{SongTitle(songId)}\" to {buyer.labelName} (${cost:N2}). " +
			(solicited ? "They asked for it." : "Nobody asked for it, so it may sit in a pile.") + " Now you wait.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>Plays the song to the label at its office, from an acetate or a pressed copy of the player's record of it.
	/// An appointment is always seen; a walk-in some weeks (the same front desk as a master visit).</summary>
	public bool VisitPlacementBuyer(string buyerId, string songId, out string message) {
		var buyer = CompetitorManager.Instance?.GetLabel(buyerId);
		message = PlacementApproachBlock(buyer, songId);
		if (message != null) return false;
		if (string.IsNullOrEmpty(buyer.homeCityId) || CurrentCityId != buyer.homeCityId) {
			var city = DistanceModel.GetCityById(buyer.homeCityId);
			message = $"{buyer.labelName}'s office is in {city?.name ?? "another town"}; you have to be there.";
			return false;
		}
		if (!InOfficeHours(MasterVisitHours, out message)) return false;
		bool copy = masters.Where(m => m.Record?.songId == songId).Any(m => AcetatesFor(m.Record.recordId) > 0)
			|| ReleasedRecords.Where(r => r.baseRecord?.songId == songId).Any(r => AcetatesFor(r.baseRecord.recordId) > 0
				|| StockFor(r.baseRecord.recordId) is { } st && st.Remaining + st.PromoRemaining > 0);
		if (!copy) { message = $"You need a copy of \"{SongTitle(songId)}\" to play them: an acetate or a pressed 45."; return false; }
		int today = TodayIndex();
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		bool appointment = placementCalls.Any(c => c.BuyerLabelId == buyerId && c.SongId == songId && c.AppointmentDay == today);
		bool seen = appointment || StableUnit($"{seed}|{buyerId}|{today / 7}|MasterWalkIn") < MasterWalkInSeen;
		if (!seen && (placementCalls.Any(c => c.BuyerLabelId == buyerId && c.Day == today && c.Result == WalkInTurnedAway)
				|| masterCalls.Any(c => c.BuyerLabelId == buyerId && c.Day == today && c.Result == WalkInTurnedAway))) {
			message = $"{buyer.labelName} isn't seeing anyone without an appointment this week."; return false;
		}
		if (!Require(MasterVisitHours, out message)) return false;
		Spend(MasterVisitHours);
		if (!seen) {
			placementCalls.Add(new SongPlacementCall { BuyerLabelId = buyerId, SongId = songId, Day = today, Result = WalkInTurnedAway });
			message = $"Waited in {buyer.labelName}'s front office. Nobody would see you without an appointment.";
			Note(message);
			Changed?.Invoke();
			return true;
		}
		var (pass, offer) = EvaluatePlacement(buyer, songId, MasterRoute.Visit);
		RecordPlacementAnswer(buyer, songId, pass, offer);
		message = pass ?? $"{offer.Answer} {PlacementSummary(offer)} The offer stands until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}.";
		Note($"Played \"{SongTitle(songId)}\" at {buyer.labelName}. {message}");
		Changed?.Invoke();
		return true;
	}

	// ── Signing ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Why this offer can't be signed now, or null: by phone from the office, or in person in their town.</summary>
	public string PlacementSignBlock(SongPlacementOffer offer) {
		if (offer == null || offer.ExpiresDay < TodayIndex() || !placementOffers.Contains(offer)) return "That offer isn't open.";
		var buyer = CompetitorManager.Instance?.GetLabel(offer.BuyerLabelId);
		if (buyer == null || !buyer.IsActive) return $"{offer.BuyerName} has gone under.";
		var (song, block) = PlaceableSong(offer.SongId);
		if (song == null) return block;
		if (!AtHome && CurrentCityId != buyer.homeCityId) return "Sign from the office, or in person at theirs.";
		if (!buyer.CanAffordToSign(offer.Advance)) return $"{offer.BuyerName} can't find ${offer.Advance:N0} right now.";
		return null;
	}

	/// <summary>Signs a placement: the advance is paid over today out of the buyer's cash, and the song is held for its act.</summary>
	public bool AcceptPlacementOffer(string offerId, out string message) {
		var offer = placementOffers.FirstOrDefault(o => o.Id == offerId);
		message = PlacementSignBlock(offer);
		if (message != null) return false;
		if (!Require(MasterSignHours, out message)) return false;
		Spend(MasterSignHours);
		var buyer = CompetitorManager.Instance.GetLabel(offer.BuyerLabelId);
		int day = TodayIndex();
		var deal = new SongPlacement {
			Id = $"sp-{offer.SongId}-{day}-{++placementOrdinal}", BuyerLabelId = buyer.labelId, BuyerName = buyer.labelName, SongId = offer.SongId,
			Title = offer.Title, Advance = offer.Advance, Status = SongPlacementStatus.Held, Read = offer.Read, SignedDay = day, HoldUntilDay = day + offer.HoldDays,
		};
		deal.Add(day, SongPlacementLedgerKind.Advance, offer.Advance);
		CompetitorManager.Instance.RecordExpense(buyer, offer.Advance);
		Label.cashReserves += offer.Advance;
		Label.monthlyRevenue += offer.Advance;
		placements.Add(deal);
		foreach (var other in placementOffers.Where(o => o.SongId == offer.SongId).ToList()) {
			placementOffers.Remove(other);
			if (other != offer) Note($"Told {other.BuyerName} \"{other.Title}\" is promised elsewhere for now.");
		}
		placementPostings.RemoveAll(p => p.SongId == offer.SongId);
		message = $"Signed: {buyer.labelName} takes \"{offer.Title}\" for one of their acts, ${offer.Advance:N0} now. " +
			$"They hold it until {LocalSceneRoomService.Date(deal.HoldUntilDay).ToShortString()}.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	public bool DeclinePlacementOffer(string offerId, out string message) {
		var offer = placementOffers.FirstOrDefault(o => o.Id == offerId);
		if (offer == null) { message = "No such offer."; return false; }
		placementOffers.Remove(offer);
		message = $"Passed on {offer.BuyerName}'s offer for \"{offer.Title}\".";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	// ── The buyer's session (called from CompetitorManager.GenerateRecordFromArtist) ────────────────

	/// <summary>
	/// The AI label is making a single with one of its acts. If it holds one of the player's songs that act can sing, and
	/// the act was about to cut an outside song anyway (not one of its own), the placed song is what it cuts. Returns the
	/// material to record (or null to keep the label's own choice) and the placement id to tag the record with. Pure:
	/// draws on no RNG stream; with no placements it returns at once.
	/// </summary>
	internal SelectedSongMaterial PlacedSongFor(AILabel label, SimulatedArtist artist, Record record, SelectedSongMaterial chosen, int year, out string placementId) {
		placementId = null;
		if (placements.Count == 0 || label == null || artist == null || record == null || chosen == null) return null;
		if (chosen.Source is SongMaterialSource.ArtistWritten or SongMaterialSource.ArtistCowrittenWithProfessional) return null;
		var deal = placements.Where(p => p.Status == SongPlacementStatus.Held && p.PendingRecordId == null && p.BuyerLabelId == label.labelId)
			.OrderBy(p => p.SignedDay).ThenBy(p => p.Id, StringComparer.Ordinal)
			.FirstOrDefault(p => ActFit(artist, CompositionCatalogService.GetSong(p.SongId), year).HasValue);
		if (deal == null) return null;
		var song = CompositionCatalogService.GetSong(deal.SongId);
		var original = PlayerRecordOfSong(deal.SongId);
		var material = original != null
			? SongMaterialSelectionService.BuildCoverForSong(artist, record, song, record.primaryGenre, year, original.baseRecord.recordId)
			: SongMaterialSelectionService.BuildProfessionalForSong(label.tier, artist, record, song, record.primaryGenre, year);
		if (material == null) return null;
		if (original != null) { material.OriginalRecordId = original.baseRecord.recordId; material.OriginalArtistId = original.baseRecord.artistId; }
		placementId = deal.Id;
		return material;
	}

	/// <summary>After the material is applied: the song's rights stand as they are (no label captures the player's song),
	/// the record carries the song's name and the contract tag, and the deal waits to see it in the shops.</summary>
	internal void StampPlacedCut(Record record, string placementId) {
		var deal = PlacementById(placementId);
		var song = deal == null ? null : CompositionCatalogService.GetSong(deal.SongId);
		if (record == null || song == null) return;
		record.playerPlacementId = placementId;
		record.title = song.title;
		record.publisherId = song.rights.publisherId;
		record.publishingControl = song.rights.controlType;
		record.publishingControllerLabelId = song.rights.controllerLabelId;
		record.publishingControllerArtistId = song.rights.controllerArtistId;
		deal.PendingRecordId = record.recordId;
		deal.PendingDay = TodayIndex();
	}

	private bool ConfirmCut(SongPlacement d, int day) {
		var runtime = d.PendingRecordId == null ? null : ChartManager.Instance?.GetRecordRuntimeData(d.PendingRecordId);
		if (runtime?.baseRecord?.playerPlacementId != d.Id) return false;
		var artist = ArtistManager.Instance?.GetArtist(runtime.baseRecord.artistId);
		d.RecordId = d.PendingRecordId; d.PendingRecordId = null;
		d.ArtistId = runtime.baseRecord.artistId; d.ArtistName = artist?.stageName ?? runtime.baseRecord.artistName;
		d.Status = SongPlacementStatus.Out;
		d.ReleasedDay = day;
		d.NextStatementDay = day + PlacementStatementDays;
		d.Add(day, SongPlacementLedgerKind.Cut, 0f);
		Note($"CUT: {d.ArtistName} recorded \"{d.Title}\" for {d.BuyerName}, and it's in the shops. Your song, their record.");
		return true;
	}

	// ── The buyer's weekly settlement (called from CompetitorManager for tagged records) ─────────────

	/// <summary>
	/// One week of the song's slice of a placed record's gross, as the buyer's settlement routes it to the player's label.
	/// Recoups the advance first; that part stays with the buyer, which paid it in advance. Returns the amount the buyer
	/// keeps. The rest accrues to the player and is paid on the statement.
	/// </summary>
	internal float SettlePlacementWeek(string placementId, string recordId, int units, float slice) {
		var d = PlacementById(placementId);
		if (d == null || slice <= 0f) return 0f;
		int day = TodayIndex();
		if (d.Status == SongPlacementStatus.Held && d.PendingRecordId == recordId) ConfirmCut(d, day);
		if (d.Status != SongPlacementStatus.Out || d.RecordId != recordId) return 0f;
		float earned = MasterDeal.R2(slice);
		float rec = Mathf.Min(d.Unrecouped, earned);
		d.Units += Math.Max(0, units); d.Earned = MasterDeal.R2(d.Earned + earned); d.Recouped = MasterDeal.R2(d.Recouped + rec);
		d.Add(day, SongPlacementLedgerKind.Earned, earned, Math.Max(0, units));
		if (rec > 0f) d.Add(day, SongPlacementLedgerKind.Recouped, rec);
		return rec;
	}

	// ── The clock ───────────────────────────────────────────────────────────────────────────────────

	/// <summary>Dawn: answers to posted tapes, lapsed offers, a cut reaching the shops (or a single that never got made),
	/// lapsed holds, statements, and the end of a record's run.</summary>
	private void ProcessSongPlacementsDawn() {
		if (Label == null) return;
		int today = TodayIndex();
		foreach (var o in placementOffers.Where(o => o.ExpiresDay < today).ToList()) {
			placementOffers.Remove(o);
			Note($"{o.BuyerName}'s offer for \"{o.Title}\" lapsed.");
		}
		ResolvePlacementPostings(today);
		foreach (var d in placements.Where(d => d.Live).ToList()) {
			var buyer = CompetitorManager.Instance?.GetLabel(d.BuyerLabelId);
			bool buyerUp = buyer != null && buyer.IsActive;
			if (d.Status == SongPlacementStatus.Held) {
				if (d.PendingRecordId != null && !ConfirmCut(d, today) && today > d.PendingDay) d.PendingRecordId = null;   // the single was never made
				if (d.Status == SongPlacementStatus.Held && d.PendingRecordId == null && (today > d.HoldUntilDay || !buyerUp)) {
					d.Status = SongPlacementStatus.Lapsed;
					d.EndNote = buyerUp ? "never cut" : $"{d.BuyerName} went under";
					d.Add(today, SongPlacementLedgerKind.Lapsed, 0f);
					Note($"\"{d.Title}\" is free to place again: {(buyerUp ? $"{d.BuyerName} never had anyone cut it" : d.EndNote)}. The advance is yours to keep.");
				}
				continue;
			}
			if (today >= d.NextStatementDay) { PlacementStatement(d, today); d.NextStatementDay += PlacementStatementDays; }
			bool selling = ChartManager.Instance?.GetRecordRuntimeData(d.RecordId) != null;
			if (!buyerUp || !selling) {
				if (d.OwedNow > 0f) PlacementStatement(d, today);   // a final statement settles what's owed
				d.Status = SongPlacementStatus.Ended;
				d.EndNote = !buyerUp ? $"{d.BuyerName} went under" : "the record ran its course";
				d.Add(today, SongPlacementLedgerKind.Ended, 0f);
				Note($"{d.BuyerName}'s record of \"{d.Title}\" is done: {d.EndNote}. {d.Units:N0} copies, ${d.Paid:N2} paid to you beyond the advance.");
			}
		}
	}

	private void ResolvePlacementPostings(int today) {
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		foreach (var p in placementPostings.Where(p => !p.NoReply && today >= p.AnswerDay).ToList()) {
			var buyer = CompetitorManager.Instance?.GetLabel(p.BuyerLabelId);
			if (buyer == null || !buyer.IsActive || PlaceableSong(p.SongId).Block != null) { placementPostings.Remove(p); continue; }
			string evidence = PlacementEvidence(p.SongId);
			bool heard = StableUnit($"{seed}|{p.BuyerLabelId}|{p.SongId}|{evidence}|{p.SentDay}|PlacementPostHeard") < (p.Solicited ? MasterPostHeardSolicited : MasterPostHeardUnsolicited);
			if (!heard) { p.NoReply = true; Note($"No word back from {buyer.labelName} about \"{SongTitle(p.SongId)}\". Most tapes never get played."); continue; }
			placementPostings.Remove(p);
			var (pass, offer) = EvaluatePlacement(buyer, p.SongId, MasterRoute.Post);
			RecordPlacementAnswer(buyer, p.SongId, pass, offer);
			Note($"A letter from {buyer.labelName} about \"{SongTitle(p.SongId)}\": " +
				(pass ?? $"{offer.Answer} {PlacementSummary(offer)} Open until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}."));
			if (offer != null) FlagSkipStop($"{buyer.labelName} wants \"{SongTitle(p.SongId)}\"", EventType.IncomingCall, officeOnly: true);
		}
	}

	/// <summary>A statement: what the song has earned beyond the advance is paid over.</summary>
	private void PlacementStatement(SongPlacement d, int today) {
		float pay = d.OwedNow;
		if (pay > 0f) {
			d.Paid = MasterDeal.R2(d.Paid + pay);
			d.Add(today, SongPlacementLedgerKind.Statement, pay);
			Label.cashReserves += pay;
			Label.monthlyRevenue += pay;
		}
		Note($"{d.BuyerName}'s statement on \"{d.Title}\": {d.Units:N0} copies to date, ${d.Earned:N2} earned by the song, ${d.Recouped:N2} against the advance" +
			(pay > 0f ? $", ${pay:N2} paid to you." : ", nothing more due yet."));
	}

	// ── Cash options ────────────────────────────────────────────────────────────────────────────────

	private IEnumerable<CashOption> PlacementCashOptions(GameDate today, Func<GameDate?, bool> tooLate, Func<GameDate?, string> late) {
		foreach (var o in PlacementOffers) {
			string block = PlacementSignBlock(o);
			yield return new("Song offer", $"{o.BuyerName} for \"{o.Title}\"", PlacementSummary(o), 0f, o.Advance, today,
				CashCertainty.Accepted, block == null && !tooLate(today),
				(block ?? $"Paid on signing, from the office or at theirs; the song stays yours. Open until {LocalSceneRoomService.Date(o.ExpiresDay).ToShortString()}.") + late(today));
		}
		foreach (var d in placements.Where(d => d.Status == SongPlacementStatus.Out && d.OwedNow > 0f)) {
			var when = LocalSceneRoomService.Date(d.NextStatementDay);
			yield return new("Song statement", $"{d.BuyerName} on \"{d.Title}\"", "earned past the advance", 0f, d.OwedNow, when,
				CashCertainty.Owed, false, $"Paid on their next statement, {when.ToShortString()}." + late(when));
		}
		foreach (string id in PlaceableSongIds()) {
			if (placementOffers.Any(o => o.SongId == id)) continue;
			var buyers = PlacementBuyersFor(id).Where(b => b.Block == null && PlacementApproachBlock(b.Label, id) == null).ToList();
			if (buyers.Count == 0) continue;
			var when = AddDays(today, 3);
			yield return new("Place a song", $"\"{SongTitle(id)}\" with {buyers.Count} label{(buyers.Count == 1 ? "" : "s")}", "a call, a posted tape or a visit",
				0f, PlacementAdvanceMin, when, CashCertainty.Speculative, !tooLate(when),
				$"An advance of ${PlacementAdvanceMin:N0}-{PlacementAdvanceMax:N0} if one of their acts could sing it; the song's income comes only if they cut it and it sells." + late(when));
		}
	}

	// ── Checks ──────────────────────────────────────────────────────────────────────────────────────

	internal void StagePlacementOfferForChecks(SongPlacementOffer offer) => placementOffers.Add(offer);
	internal void StagePlacementForChecks(SongPlacement deal) => placements.Add(deal);
	internal void StagePlacementPassForChecks(SongPlacementPass pass) => placementPasses.Add(pass);
	internal void StagePlacementPostingForChecks(SongPlacementPosting post) => placementPostings.Add(post);
	internal void ProcessSongPlacementsForChecks() => ProcessSongPlacementsDawn();
	internal float PlacementReadForChecks(AILabel buyer, string songId, bool posted) {
		var song = CompositionCatalogService.GetSong(songId);
		return song == null ? -1f : PlacementRead(buyer, song, posted);
	}
	internal static float PlacementBarForChecks(AILabel buyer, bool ownSound) => PlacementBar(buyer, ownSound);
	internal (string Pass, SongPlacementOffer Offer) EvaluatePlacementForChecks(AILabel buyer, string songId, MasterRoute route) => EvaluatePlacement(buyer, songId, route);
	internal float SettlePlacementWeekForChecks(string id, string recordId, int units, float slice) => SettlePlacementWeek(id, recordId, units, slice);

	/// <summary>Checks only: a shelf master of one of the roster's acts with its own composition, the publishing the label's.</summary>
	internal Master StageShelfSongForChecks(SimulatedArtist act, string recordId, string title, float craft) {
		var master = StageShelfMasterForChecks(act, recordId, title, craft, craft);
		int year = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).year;
		bool keeps = act.labelOwnsPublishing;
		act.labelOwnsPublishing = true;
		var song = CompositionCatalogService.CreateArtistOriginal(master.Record, act, Label, act.primaryGenre, year, craft, craft, craft, .5f);
		act.labelOwnsPublishing = keeps;
		song.title = title;
		CompositionCatalogService.ApplyToRecord(master.Record, song, SongMaterialSource.ArtistWritten, false, null, null, 0f, .5f, 0f);
		return master;
	}

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private SongPlacementsSaveData CaptureSongPlacements() {
		if (placementOffers.Count + placements.Count + placementPasses.Count + placementPostings.Count + placementCalls.Count == 0) return null;
		return new SongPlacementsSaveData {
			Offers = LocalSceneRoomService.Copy(placementOffers), Placements = LocalSceneRoomService.Copy(placements),
			Passes = LocalSceneRoomService.Copy(placementPasses), Postings = LocalSceneRoomService.Copy(placementPostings),
			Calls = LocalSceneRoomService.Copy(placementCalls), Ordinal = placementOrdinal,
		};
	}

	private void RestoreSongPlacements(SongPlacementsSaveData data) {
		placementOffers.Clear(); placements.Clear(); placementPasses.Clear(); placementPostings.Clear(); placementCalls.Clear(); placementOrdinal = 0;
		if (data == null) return;
		placementOffers.AddRange(data.Offers ?? new());
		placements.AddRange(data.Placements ?? new());
		placementPasses.AddRange(data.Passes ?? new());
		placementPostings.AddRange(data.Postings ?? new());
		placementCalls.AddRange(data.Calls ?? new());
		placementOrdinal = data.Ordinal;
		foreach (var d in placements.Where(d => !d.LedgerAgrees()))
			GD.PushWarning($"Song placement {d.Id} ledger disagrees with its saved balances; keeping the saved balances.");
	}
}

/// <summary>Early-game Phase 5A: song placement offers, signed placements and their ledgers, passes, tapes and calls.
/// Absent in older saves.</summary>
public sealed class SongPlacementsSaveData {
	public List<SongPlacementOffer> Offers { get; set; }
	public List<SongPlacement> Placements { get; set; }
	public List<SongPlacementPass> Passes { get; set; }
	public List<SongPlacementPosting> Postings { get; set; }
	public List<SongPlacementCall> Calls { get; set; }
	public int Ordinal { get; set; }
}
