using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE HOLDER'S FIRST MOVE (SimTools/LabelManInnerLifeDirective.md §6.1, Phase 1e).
//
// Every holder's 1960 Show is "a record out", so the first single is the one thing all five care about. While it is at the
// plant the holder starts circling it, each in his own way. He's been named since the first morning (PlayerDesk.FirstMorning.cs);
// the move is the first time he shows what he wants from the label, and it opens the leads that follow from meeting him.
//
// One move, one real choice. Each move is an offer on a channel that already exists:
//   Sawyer   launch at his room       accept: the room's Connie booking gate is waived for the year
//   Kubiak   200 copies "for the route" accept: copies come off the run at the plant's cost, paid in cash on delivery
//                                      refuse: the home town's jukebox operators won't take your records this year
//   Winters  the flip's publishing     accept: the B-side's publishing goes to his publisher
//   Germain  his boys, $300            accept: the fee now; his network works the single from its street date
//   Floria   the plant moves you up    the favour is done when you hear of it; you can put the run back on its date
// Each answer goes on the lien ledger and moves the lawyer's standing. If the run comes in before the Label Man is at the desk
// to hear the move, the holder has still shown himself, and the move counts as turned down.
//
// Player-only and AI-inert: nothing here runs without a lien, the plant queue is the player's own (pressOrders), and no draw
// touches any random stream.
// ============================================================================================
public partial class PlayerDesk {
	public enum HolderMoveStage { None, Due, Offered, Accepted, Refused, Lapsed }

	/// <summary>Days after the first order that the holder makes his move (provisional), never on or after the run's arrival.</summary>
	public const int HolderMoveDelayDays = 2;
	/// <summary>Kubiak's claim on the first run, and the most of a small run he takes.</summary>
	public const int KubiakClaimCopies = 200;
	public const float KubiakClaimMaxShare = 0.5f;
	/// <summary>How far Floria's friend moves the run up the plant's queue.</summary>
	public const int FloriaFavourDays = 7;
	/// <summary>Standing with the lawyer for taking the favour, and for turning it down (provisional).</summary>
	public const float HolderMoveAcceptStanding = 0.1f, HolderMoveRefuseStanding = 0.05f;

	/// <summary>The move is on the desk: he has shown himself and is waiting for an answer.</summary>
	public bool HolderMovePending => lien != null && lien.Move == HolderMoveStage.Offered && lien.OpeningSeen && !IsGameOver
		&& activeSitting == null && AtHome;

	/// <summary>Sawyer accepted the launch at his room: the room's Connie gate is waived for this year.</summary>
	private bool SawyerLaunchWaives(int year) => lien?.Holder == LienHolder.Sawyer && lien.SawyerLaunchYear == year;

	/// <summary>Kubiak was turned down: his jukebox rights stand in the home town for the rest of that year.</summary>
	private string KubiakRouteRefusal(PlayerStop stop) {
		if (lien?.Holder != LienHolder.Kubiak || lien.KubiakRefusedYear == 0 || stop?.Kind != StopKind.Op || Label == null) return null;
		if (stop.CityId != Label.homeCityId || LienToday.year != lien.KubiakRefusedYear) return null;
		return $"{stop.DisplayName} won't put your records on a machine in this town. Walter Kubiak holds the jukebox rights here, and you turned him down.";
	}

	// ── staging ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>The first run of the first single has gone to the plant: the holder will make his move in a couple of days.</summary>
	private void StageHolderMove(PressOrder order, GameDate today) {
		// Every holder is named from the first morning now; the move is him turning up about the record, and why he wants it.
		// A run whose holder showed himself another way first (a sitting before any record) doesn't get one; Sawyer is exempt,
		// as older saves marked him met from Connie's paper at founding.
		if (lien == null || order == null || IsGameOver || lien.Move != HolderMoveStage.None || HolderShownHand && lien.Holder != LienHolder.Sawyer) return;
		// Only the first single: nothing out yet, nothing pressed before, and this the only run at the plant.
		if (ReleasedRecords.Any() || inventory.Values.Any(s => s.TotalPressed > 0) || pressOrders.Count != 1) return;
		var due = today.AddDays(HolderMoveDelayDays);
		if (due >= order.Arrives) due = order.Arrives.AddDays(-1);
		if (due < today) due = today;
		lien.Move = HolderMoveStage.Due;
		lien.MoveRecordId = order.RecordId;
		lien.MoveDue = due;
	}

	/// <summary>Dawn, before the plant delivers: the move lands on its day, and lapses if the run comes in first.</summary>
	private void ProcessHolderMoveDawn(GameDate date) {
		if (lien == null || IsGameOver) return;
		CheckSawyerPromise(date);
		if (lien.Move is HolderMoveStage.Due or HolderMoveStage.Offered) {
			var order = PressingOrderFor(lien.MoveRecordId);
			if (order == null || order.Arrives <= date) { LapseHolderMove(date); return; }
			if (lien.Move == HolderMoveStage.Due && date >= lien.MoveDue) {
				lien.Move = HolderMoveStage.Offered;
				// Floria's favour is done before anyone asks you: the plant man tells you when you get there.
				if (lien.Holder == LienHolder.Floria) MoveRunUp(order, date);
			}
		}
	}

	/// <summary>Weeks after your single's street date that Dee-Dee gives Connie's record to follow it (provisional).</summary>
	public const int SawyerPromiseWeeks = 8;
	/// <summary>Standing lost with the lawyer when Connie's record doesn't follow (provisional).</summary>
	public const float SawyerPromiseBrokenStanding = 0.15f;

	/// <summary>Connie has a record out on or before this date.</summary>
	private bool ConnieOutBy(GameDate date) => lien?.ConnieId != null
		&& ReleasedRecords.Any(r => r.baseRecord?.artistId == lien.ConnieId && r.baseRecord.releaseDate <= date);

	/// <summary>The launch at his room was a promise: once your single ships, Connie's record has to follow within the weeks he gave.</summary>
	private void SettleSawyerLaunch(GameDate date) {
		if (lien?.Holder != LienHolder.Sawyer || lien.Move != HolderMoveStage.Accepted || lien.MoveSettled || IsGameOver) return;
		var record = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == lien.MoveRecordId);
		if (record == null) return;
		lien.MoveSettled = true;
		lien.SawyerPromiseDue = record.baseRecord.releaseDate.AddDays(7 * SawyerPromiseWeeks);
		Note(ConnieOutBy(date)
			? $"\"{TitleForRecord(lien.MoveRecordId)}\" launched at {lien.Venue}, with Connie's record already out. Dee-Dee calls it the best night the room's had."
			: $"\"{TitleForRecord(lien.MoveRecordId)}\" launched at {lien.Venue}, with Connie on the bill. Dee-Dee wants her record out by {lien.SawyerPromiseDue.Value.ToHeadlineString()}.");
	}

	/// <summary>Dawn after the date he gave: no Connie record, and he takes it as an insult.</summary>
	private void CheckSawyerPromise(GameDate date) {
		if (lien?.Holder != LienHolder.Sawyer || lien.SawyerPromiseDue is not { } due || lien.SawyerPromiseBroken || IsGameOver) return;
		if (date <= due) return;
		if (ConnieOutBy(due)) { lien.SawyerPromiseDue = null; return; }
		lien.SawyerPromiseBroken = true;
		lien.SawyerPromiseDue = null;
		if (lien.SawyerLaunchYear == due.year || lien.SawyerLaunchYear == date.year) lien.SawyerLaunchYear = 0;
		lien.Standing = Mathf.Max(0f, lien.Standing - SawyerPromiseBrokenStanding);
		lien.Add(date, LienLedgerKind.HolderMove, 0f, "Sawyer: promise broken");
		Note($"Connie's record never followed yours. Dee-Dee Sawyer has taken back his word on {lien.Venue}: your other acts can't play it until she has a record out, and he'll bring it up in December.");
	}

	/// <summary>After the day's releases: Dee-Dee's launch starts its clock, and Jack's boys go to work the day the single ships.</summary>
	private void SettleHolderMoveAfterRelease(GameDate date) {
		SettleSawyerLaunch(date);
		if (lien?.Holder != LienHolder.Germain || lien.Move != HolderMoveStage.Accepted || lien.MoveSettled || IsGameOver) return;
		var record = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == lien.MoveRecordId);
		if (record == null) return;
		lien.MoveSettled = true;
		var stations = GermainStations();
		var placed = stations.Count == 0 ? null : ChartManager.Instance?.PlacePromoterNetwork(GermainPromoterId, GermainName + "'s boys", record.baseRecord.recordId, Label.labelId,
			stations.Select(s => s.stationId).ToArray(), GermainNetworkEffectiveness, GermainDiscretionNow, GermainNetworkMob, GermainNetworkWeeks, 0f);
		string title = TitleForRecord(record.baseRecord.recordId);
		if (placed == null || placed.Count == 0) { Note($"Jack's boys couldn't get anyone to pick up the phone for \"{title}\". He kept the fee."); return; }
		lien.NetworkUses++;
		Note($"{GermainName}'s boys are working \"{title}\" on {Plural.Of(placed.Count, "station")} for {GermainNetworkWeeks} weeks, the way he promised.");
	}

	/// <summary>The plant delivered: Kubiak's man is on the dock for his copies.</summary>
	private void SettleKubiakClaim(PressOrder order, PressStock stock) {
		if (lien?.Holder != LienHolder.Kubiak || lien.Move != HolderMoveStage.Accepted || lien.MoveSettled || order.RecordId != lien.MoveRecordId) return;
		lien.MoveSettled = true;
		int copies = KubiakClaimFor(order.Quantity - Mathf.Clamp(order.PromoQuantity, 0, order.Quantity));
		copies = Math.Min(copies, stock.Remaining);
		if (copies <= 0) return;
		float perCopy = order.Quantity > 0 ? order.Cost / order.Quantity : 0f;
		float paid = R2(copies * perCopy);
		stock.Remaining -= copies;
		Label.cashReserves += paid;
		lien.KubiakRouteDate = LienToday;   // on his machines from today: the home town's bars hear it (PlayerDesk.Kubiak.cs)
		lien.Add(LienToday, LienLedgerKind.HolderMove, paid, $"Kubiak took {copies} copies at cost");
		Note($"Kubiak's man was on the plant's dock for {copies:N0} copies of \"{TitleForRecord(order.RecordId)}\". He paid the plant's price, ${paid:N0}, in cash. They're going on the route.");
	}

	private static int KubiakClaimFor(int saleable) => Math.Max(0, Math.Min(KubiakClaimCopies, (int)(saleable * KubiakClaimMaxShare)));

	private void MoveRunUp(PressOrder order, GameDate today) {
		var earliest = today.AddDays(1);
		int days = Math.Min(FloriaFavourDays, Math.Max(0, DaysBetween(earliest, order.Arrives)));
		if (days <= 0) return;
		order.Arrives = order.Arrives.AddDays(-days);
		if (order.QueueComplete > order.Arrives) order.QueueComplete = order.Arrives;
		lien.MoveDays = days;
	}

	private void LapseHolderMove(GameDate date) {
		lien.Move = HolderMoveStage.Lapsed;
		lien.HolderIdentified = true;
		ApplyHolderMoveRefusal(date, lapsed: true);
		Note(HolderMoveLapsedLine());
	}

	// ── the scene ──────────────────────────────────────────────────────────────────────────────────

	public sealed class HolderMoveOption {
		public bool accept;
		public string label, subLabel, disabledReason;
		public ExecutiveVoice voice;
		public bool enabled = true;
		public float cash;
	}

	/// <summary>The head of the scene: who came, and what it looks like at the office door.</summary>
	public string HolderMoveSetting() => lien?.Holder switch {
		LienHolder.Sawyer => "The door opens without a knock. A big man in a good suit fills it.",
		LienHolder.Kubiak => "You stop in at the plant to chase the date. The plant man has a paper for you, and he looks sorry about it.",
		LienHolder.Winters => "A polite knock. A thin man with a trade paper under his arm and a lead sheet in his hand.",
		LienHolder.Germain => "The phone rings at the office. You know the voice before he says his name. Everybody in the region does.",
		LienHolder.Floria => "You stop in at the plant to chase the date. The plant man has news, and he's pleased with it.",
		_ => "",
	};

	/// <summary>The move, line by line: the holder in his own voice, then the voices, each only past its threshold and only about
	/// what is true. The voice whose morning theory fits him gloats; one whose theory doesn't is told so.</summary>
	public List<CallLine> HolderMoveLines() {
		var lines = new List<CallLine>();
		if (lien == null || Label == null) return lines;
		void Say(string text, string speaker = "", ExecutiveVoice voice = ExecutiveVoice.None) =>
			lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, speaker = speaker, voice = voice });
		string title = TitleForRecord(lien.MoveRecordId);
		var p = LienProfile;
		var inst = InstinctProfile;
		Say(HolderMoveSetting());
		switch (lien.Holder) {
			case LienHolder.Sawyer:
				Say("\"Dee-Dee Sawyer. We met. You lost.\"", "Dee-Dee Sawyer");
				Say("He laughs at that a long time. Your watch is on his wrist.");
				Say($"\"I hear you've got a record at the plant. A label's first record gets launched at {lien.Venue}, where people can see it. And Connie goes on that stage the same night. She's on your label, kid. I made sure of that.\"", "Dee-Dee Sawyer");
				if (inst.TheSuit >= 2) Say("Every favour from this man is one he'll collect. That's the paper's whole idea.", "", ExecutiveVoice.Suit);
				if (inst.TheFixer >= 3) Say(p.FixerRead, "", ExecutiveVoice.Fixer);
				break;
			case LienHolder.Kubiak: {
				int copies = KubiakClaimFor(MoveSaleableCopies());
				Say("The plant man reads it out like a man reading a will.");
				Say($"\"Operator's claim, clause fourteen. {copies:N0} copies of '{title}' off the run at the plant's price, for Mr. Walter Kubiak's route.\"", "The plant man");
				Say("\"He says to tell you the jukebox rights in this town are his anyway. You signed them over. He says you'd remember.\"", "The plant man");
				if (inst.TheSuit >= 3) Say($"At cost he pays the plant's price and nothing for the margin. That's ${R2(copies * (DealerPrice - MovePerCopyCost())):N0} you don't see. Then again, it's cash this month.", "", ExecutiveVoice.Suit);
				if (inst.TheSuit >= 3) Say("The notary's stamp on that form. Leon Marsh. Remember the name.", "", ExecutiveVoice.Suit);
				if (inst.TheStreet >= 3) Say("Kubiak's route doesn't reach past the river. His machines are in bars nobody young goes to.", "", ExecutiveVoice.Street);
				break;
			}
			case LienHolder.Winters:
				Say("\"Clyde Winters. We met at a party. You probably don't remember, I wouldn't. You've got a single at the plant, and I've told my publisher I placed a song with you. I'd like that to be true.\"", "Clyde Winters");
				Say(WintersFlip() is { } flip
					? $"\"It's too late for my song, I know. Let me have the publishing on the flip, '{flip.title}'. My office sees my name on your record, and everybody's happy.\""
					: "\"It's too late for my song, I know. I was hoping for the publishing on the flip, but it isn't yours to give.\"", "Clyde Winters");
				if (inst.TheStreet >= 3) Say("He just named a jock whose home number is in your address book and nowhere else. Ask yourself how.", "", ExecutiveVoice.Street);
				if (inst.TheEar >= 3) Say("He knows a song. He's not the enemy you'd pick.", "", ExecutiveVoice.Ear);
				break;
			case LienHolder.Germain:
				Say($"\"Jack Germain, kid. You were in the booth with me at midnight. You told the whole region you had a record coming. Now you have.\"", "Jack Germain");
				Say($"\"My boys can have '{title}' on the air the day it ships. ${GermainNetworkFee:N0}, consulting. That's a fee, not a bribe, kid. Write it down that way.\"", "Jack Germain");
				if (inst.TheFixer >= 3) Say("It'll work. It always works with Jack. That's how he got caught the first time.", "", ExecutiveVoice.Fixer);
				if (inst.TheSuit >= 3) Say("Every consulting fee is a line in a book somebody will read one day. The grand jury is reading Jack's books right now.", "", ExecutiveVoice.Suit);
				if (inst.TheStreet >= 3) Say($"{Plural.Of(GermainStations().Count, "station")} in the home region still take his calls.", "", ExecutiveVoice.Street);
				break;
			case LienHolder.Floria: {
				var order = PressingOrderFor(lien.MoveRecordId);
				Say($"\"Good news. Your run's moved up {Plural.Of(lien.MoveDays, "day")}" + (order != null ? $": in by {order.Arrives.ToHeadlineString()}" : "") + ". A man from the regional office took care of it. Very quiet fella, camel coat. Left his card.\"", "The plant man");
				Say($"The card says Gene Floria, {p.Title}. On the back, in a small neat hand: \"A label has to grow.\"");
				if (inst.TheSuit >= 2) Say("Nobody moves a nobody up a plant's queue for nothing. He's fattening you up.", "", ExecutiveVoice.Suit);
				if (inst.TheFixer >= 3) Say(p.FixerRead, "", ExecutiveVoice.Fixer);
				break;
			}
		}
		foreach (var line in TheoryLines()) lines.Add(line);
		return lines;
	}

	/// <summary>The morning's theories meet the man: a voice whose answer fits him gloats, and one whose doesn't hears about it.</summary>
	private IEnumerable<CallLine> TheoryLines() {
		var m = lien.Memory;
		var fields = new (ExecutiveVoice Voice, bool Answered, PartyMemory Single)[] {
			(ExecutiveVoice.Fixer, m.End != PartyEnd.Nothing, new PartyMemory { End = m.End }),
			(ExecutiveVoice.Suit, m.Where != PartyWhere.Nothing, new PartyMemory { Where = m.Where }),
			(ExecutiveVoice.Ear, m.Brag != PartyBrag.Nothing, new PartyMemory { Brag = m.Brag }),
			(ExecutiveVoice.Street, m.Who != PartyWho.Nothing, new PartyMemory { Who = m.Who }),
		};
		var inst = InstinctProfile;
		var right = fields.Where(f => f.Answered && Fits(f.Single).Contains(lien.Holder)).OrderByDescending(f => Score(inst, f.Voice)).FirstOrDefault();
		var wrong = fields.Where(f => f.Answered && !Fits(f.Single).Contains(lien.Holder)).OrderBy(f => Score(inst, f.Voice)).FirstOrDefault();
		if (right.Answered) yield return new CallLine { beat = RolodexSceneBeat.Opening, voice = right.Voice, text = right.Voice switch {
			ExecutiveVoice.Fixer => "What did I say about how it ended? Write it down. I want it on the record.",
			ExecutiveVoice.Suit => "I said somebody in that room had paper in his pocket. There he is.",
			ExecutiveVoice.Ear => "I told you somebody was listening to you brag. That's him.",
			_ => "Told you. I know a type when I hear one described.",
		} };
		if (wrong.Answered && right.Answered && wrong.Voice != right.Voice) yield return new CallLine { beat = RolodexSceneBeat.Opening, voice = right.Voice, text = wrong.Voice switch {
			ExecutiveVoice.Fixer => "And the Fixer's big theory about the end of the night? Not this fella. Not even close.",
			ExecutiveVoice.Suit => "And the Suit had the room all worked out. Wrong room.",
			ExecutiveVoice.Ear => "And the Ear's story about what you were bragging about? He doesn't fit it.",
			_ => "And the Street's man at the party? This isn't him. Somebody remembered wrong.",
		} };
	}

	public List<HolderMoveOption> HolderMoveOptions() {
		var list = new List<HolderMoveOption>();
		if (lien == null || lien.Move != HolderMoveStage.Offered) return list;
		float cash = Mathf.Max(0f, Label.cashReserves);
		switch (lien.Holder) {
			case LienHolder.Sawyer:
				list.Add(new HolderMoveOption { accept = true, label = $"Launch it at {lien.Venue}, with Connie on the bill",
					subLabel = $"Dee-Dee waives his rule for {LienToday.year}: your acts can play his room this year before Connie has a record out. " +
						$"In return Connie's record follows yours within {SawyerPromiseWeeks} weeks of its street date. If it doesn't, he takes it all back." });
				list.Add(new HolderMoveOption { label = "Tell him you'll launch it your own way",
					subLabel = "His room stays shut to your other acts until Connie has a record out this year. He'll remember that too." });
				break;
			case LienHolder.Kubiak: {
				int copies = KubiakClaimFor(MoveSaleableCopies());
				float paid = R2(copies * MovePerCopyCost());
				list.Add(new HolderMoveOption { accept = true, label = $"Let him have the {copies:N0} copies", cash = paid,
					subLabel = $"They come off the run when it's delivered. He pays the plant's price, about ${paid:N0} in cash, and they go on his machines." });
				list.Add(new HolderMoveOption { label = "Tear up his claim",
					subLabel = $"His jukebox rights stand: no operator in this town will take your records for the rest of {LienToday.year}." });
				break;
			}
			case LienHolder.Winters: {
				var flip = WintersFlip();
				list.Add(new HolderMoveOption { accept = true, label = flip == null ? "Give him the publishing on the flip" : $"Give him the publishing on \"{flip.title}\"",
					subLabel = "The B-side's publishing goes to his publisher for good. If a jock ever flips the record, that's real money.",
					enabled = flip != null, disabledReason = "The flip's publishing isn't yours to give." });
				list.Add(new HolderMoveOption { label = "Send him back to his publisher with nothing",
					subLabel = "He goes away disappointed. He still has your address book." });
				break;
			}
			case LienHolder.Germain: {
				int stations = GermainStations().Count;
				list.Add(new HolderMoveOption { accept = true, label = $"Pay Jack's consulting fee (${GermainNetworkFee:N0})", cash = GermainNetworkFee,
					subLabel = $"His boys work the single on {Plural.Of(stations, "station")} for {GermainNetworkWeeks} weeks from its street date. It works. It can get you caught.",
					enabled = cash >= GermainNetworkFee && stations > 0,
					disabledReason = stations == 0 ? "He has no stations in your home region to call." : $"You have ${cash:N0}." });
				list.Add(new HolderMoveOption { label = "Tell him you'll get it played on your own",
					subLabel = "He talks for another twenty minutes about how that never works." });
				break;
			}
			case LienHolder.Floria:
				list.Add(new HolderMoveOption { accept = true, label = "Take the earlier date",
					subLabel = $"The run comes in {Plural.Of(lien.MoveDays, "day")} sooner. He'll count it as a favour." });
				list.Add(new HolderMoveOption { label = "Put the run back on its own date",
					subLabel = $"You lose the {Plural.Of(lien.MoveDays, "day")}, and you owe him nothing." });
				break;
		}
		return list;
	}

	/// <summary>Answer the move. Accepting goes on the lien ledger and raises the lawyer's standing; refusing lowers it.</summary>
	public bool ResolveHolderMove(bool accept, out string message) {
		message = null;
		if (lien == null || lien.Move != HolderMoveStage.Offered || IsGameOver) { message = "Nobody's waiting on an answer."; return false; }
		var opt = HolderMoveOptions().FirstOrDefault(o => o.accept == accept);
		if (opt == null || !opt.enabled) { message = opt?.disabledReason ?? "You can't do that."; return false; }
		var today = LienToday;
		lien.HolderIdentified = true;
		if (accept) {
			switch (lien.Holder) {
				case LienHolder.Sawyer:
					lien.SawyerLaunchYear = today.year;
					message = $"Dee-Dee shakes your hand like he's won it. The launch is at {lien.Venue}, and Connie's on the bill. " +
						$"\"Her record's out inside {SawyerPromiseWeeks} weeks of yours, kid. You said so.\"";
					break;
				case LienHolder.Kubiak:
					message = "The plant man files the claim. Kubiak's man will be on the dock when the run comes in.";
					break;
				case LienHolder.Winters: {
					var flip = WintersFlip();
					var pub = WintersPublisher();
					flip.publishingControl = PublishingControlType.ExternalPublisher;
					flip.publishingControllerLabelId = null;
					if (pub != null) flip.publisherId = pub.publisherId;
					message = $"Clyde writes \"{flip.title}\" in a notebook and underlines it. \"You won't regret it.\" The flip's publishing is his publisher's now.";
					break;
				}
				case LienHolder.Germain:
					Label.cashReserves -= GermainNetworkFee;
					Label.monthlyExpenses += GermainNetworkFee;
					ChartManager.Instance?.AddPayolaExposure(Label.labelId, GermainFeeExposure);
					message = $"\"Now you're in the business, kid.\" ${GermainNetworkFee:N0} goes on the books as consulting. His boys start the day it ships.";
					break;
				case LienHolder.Floria:
					message = "You thank the plant man. He says to thank the fella in the camel coat.";
					break;
			}
			lien.Move = HolderMoveStage.Accepted;
			lien.Standing = Mathf.Min(1f, lien.Standing + HolderMoveAcceptStanding);
			lien.Add(today, LienLedgerKind.HolderMove, opt.cash, $"{lien.Holder}: accepted");
		} else {
			lien.Move = HolderMoveStage.Refused;
			message = ApplyHolderMoveRefusal(today, lapsed: false);
		}
		Note(message);
		Changed?.Invoke();
		return true;
	}

	private string ApplyHolderMoveRefusal(GameDate today, bool lapsed) {
		lien.Standing = Mathf.Max(0f, lien.Standing - HolderMoveRefuseStanding);
		lien.Add(today, LienLedgerKind.HolderMove, 0f, $"{lien.Holder}: {(lapsed ? "lapsed" : "refused")}");
		switch (lien.Holder) {
			case LienHolder.Kubiak:
				lien.KubiakRefusedYear = today.year;
				return $"The plant man tears up the claim. \"He won't like that.\" No operator in town will touch your records for the rest of {today.year}.";
			case LienHolder.Floria: {
				// The favour stands if you never came to hear about it. Turning it down puts the run back where it was.
				if (lapsed || lien.MoveDays <= 0) return "";
				var order = PressingOrderFor(lien.MoveRecordId);
				if (order != null) order.Arrives = order.Arrives.AddDays(lien.MoveDays);
				int days = lien.MoveDays;
				lien.MoveDays = 0;
				return $"You ask the plant man to put the run back on its own date. He looks at you like you've lost your mind. That's {Plural.Of(days, "day")} you've given back.";
			}
			case LienHolder.Sawyer: return "Dee-Dee laughs, but not long. \"Your way. Sure, kid.\" His room stays shut to your acts until Connie has a record out.";
			case LienHolder.Winters:
				if (lapsed) return "";
				return "Clyde takes it well. He takes everything well. He leaves with your address book still in his coat." + BurnOneWintersCard();
			case LienHolder.Germain: return "Jack talks for another twenty minutes about how it never works without his boys. Then he hangs up.";
			default: return "";
		}
	}

	private string HolderMoveLapsedLine() => lien.Holder switch {
		LienHolder.Sawyer => "Dee-Dee Sawyer came by the office while you were out. He left word about launching your record at his room, and your own watch on the desk to tell the time by until you call. He took it back the next day.",
		LienHolder.Kubiak => "The plant man says Walter Kubiak's lawyer came by with an operator's claim on your run. Nobody was there to sign it, so he tore it up. Kubiak's jukebox rights stand in this town for the rest of the year.",
		LienHolder.Winters => "A trade paper under the door with a note in it: Clyde Winters called about the publishing on your flip. He'll try again another time.",
		LienHolder.Germain => "There are three messages from Jack Germain at the office. He says his boys were ready. He says it's your loss.",
		LienHolder.Floria => $"The plant moved your run up {Plural.Of(lien.MoveDays, "day")}. The plant man says a man from the regional office took care of it. Gene Floria. He left a card.",
		_ => "",
	};

	// ── the December sitting reads the answer ───────────────────────────────────────────────────────
	//
	// The lawyer opens that year's sitting on the answer. A taken favour (or Floria's, which stands if nobody sent it back) is
	// that year's Concede: it rolls the lien like any Concede, and he books it against half the year's interest. A refusal or
	// a lapse costs nothing more here; its price is the holder's own handicap, already paid during the year.

	/// <summary>Share of the interest due at the sitting that the favour is booked against (provisional).</summary>
	public const float HolderFavourInterestShare = 0.5f;

	/// <summary>The sitting is the one for the year the move was answered in.</summary>
	private bool MoveSittingYear => lien != null && lien.Move is HolderMoveStage.Accepted or HolderMoveStage.Refused or HolderMoveStage.Lapsed
		&& lien.MoveDue.year == lien.BarYear && !lien.IsReckoning;

	/// <summary>He counts the move as a favour you owe him: taken, or Floria's left standing. Once.</summary>
	private bool HolderFavourStands => MoveSittingYear && !lien.MoveCredited
		&& (lien.Holder == LienHolder.Floria ? lien.MoveDays > 0 && lien.Move != HolderMoveStage.Refused : lien.Move == HolderMoveStage.Accepted)
		&& !lien.SawyerPromiseBroken;

	/// <summary>What the favour is booked against: half the interest owed today, without accruing it.</summary>
	private float HolderFavourCredit() => lien == null ? 0f : R2(lien.InterestDueBy(LienToday) * HolderFavourInterestShare);

	/// <summary>The lawyer's line on the answer, and a voice on it, for that year's sitting.</summary>
	private void SayHolderMoveAtSitting(LienSittingLive live) {
		if (HolderMoveSittingLine() is not { } line) return;
		bool favour = HolderFavourStands;
		live.Say(line, LienProfile.Lawyer);
		var inst = InstinctProfile;
		if (favour && inst.TheSuit >= 2)
			live.Say(lien.Rate > 0f ? $"That favour was a concession, and he's counting it. Make him count it against the interest: ${HolderFavourCredit():N0}."
				: "That favour was a concession, and he's counting it. Make him say so.", "", ExecutiveVoice.Suit);
		else if (!favour && inst.TheFixer >= 3)
			live.Say("He's sore about it. Don't hand him another reason tonight.", "", ExecutiveVoice.Fixer);
	}

	/// <summary>The lawyer's line on the answer at that year's sitting, or null in any other year.</summary>
	internal string HolderMoveSittingLine() {
		if (!MoveSittingYear) return null;
		bool favour = HolderFavourStands || lien.MoveCredited;
		string first = LienProfile.Name.Split(' ')[0];
		if (lien.SawyerPromiseBroken)
			return $"\"Mr. Sawyer launched your record at {lien.Venue} on your word that Miss Boursaw's would follow. It didn't. He considers that a debt, not a favour.\"";
		return favour ? lien.Holder switch {
			LienHolder.Sawyer => $"\"Mr. Sawyer enjoyed the launch. Miss Boursaw on the bill, {lien.Venue} full. He asked me to say he hasn't forgotten it.\"",
			LienHolder.Kubiak => "\"Mr. Kubiak's route had your copies at cost. He's put that in the book in your favour. Not very far in your favour.\"",
			LienHolder.Winters => "\"Mr. Winters's office saw his name on your record. He was very pleased. He asked me to say so first.\"",
			LienHolder.Germain => "\"Jack says his boys did right by your record. He says you'd want that remembered tonight.\"",
			_ => "\"Mr. Floria was glad to help at the plant. He'd like that recognised.\"",
		} : lien.Move == HolderMoveStage.Lapsed
			? $"\"{first} came to you about your record. You never gave him an answer. He's noted that.\""
			: lien.Holder switch {
				LienHolder.Sawyer => "\"Mr. Sawyer notes you launched your record somewhere else. He asked me to mention it. Twice.\"",
				LienHolder.Kubiak => "\"Mr. Kubiak's claim was torn up at the plant. His jukebox rights stand. So does his interest.\"",
				LienHolder.Winters => "\"Mr. Winters asked for very little. He'd hoped you'd remember that.\"",
				LienHolder.Germain => "\"Jack says you got it played on your own. He says that's fine. He said it four times.\"",
				_ => "\"Mr. Floria tells me you sent a favour back. He found that interesting.\"",
			};
	}

	/// <summary>The favour as that year's Concede, when he counts one.</summary>
	private LienOption HolderFavourOption() {
		if (!HolderFavourStands) return null;
		float credit = HolderFavourCredit();
		string label = lien.Holder switch {
			LienHolder.Sawyer => $"Remind him the launch was at {lien.Venue}, with Connie on the bill",
			LienHolder.Kubiak => "Remind him his route had your copies at cost",
			LienHolder.Winters => "Remind him his publisher has your flip",
			LienHolder.Germain => "Remind him his boys had their fee",
			_ => "Remind him whose favour moved your run",
		};
		return new LienOption { verb = LienVerb.ConcedeFavour, label = label,
			subLabel = credit > 0f ? $"He counts it as this year's concession and books it against ${credit:N0} of the interest. The rest comes out of the till if it's there."
				: "He counts it as this year's concession." };
	}

	/// <summary>Book the favour: half the interest owed today counts as paid, the rest is taken if the till has it.</summary>
	private void CreditHolderFavour(GameDate today) {
		lien.AccrueTo(today);
		float credit = R2(Mathf.Min(lien.InterestDue, lien.InterestDue * HolderFavourInterestShare));
		lien.MoveCredited = true;
		if (credit > 0f) lien.InterestPaid = R2(lien.InterestPaid + credit);
		lien.Add(today, LienLedgerKind.HolderMove, credit, $"{lien.Holder}: favour counted");
		if (!LienRefusesPay) PayInterestIfAny(today, onlyIfCash: true);
	}

	// ── helpers ────────────────────────────────────────────────────────────────────────────────────

	private int MoveSaleableCopies() {
		var order = PressingOrderFor(lien.MoveRecordId);
		return order == null ? 0 : order.Quantity - Mathf.Clamp(order.PromoQuantity, 0, order.Quantity);
	}

	private float MovePerCopyCost() {
		var order = PressingOrderFor(lien.MoveRecordId);
		return order == null || order.Quantity <= 0 ? 0f : order.Cost / order.Quantity;
	}

	/// <summary>The flip of the single at the plant, when its publishing is the label's to give.</summary>
	private Record WintersFlip() {
		var release = planned.FirstOrDefault(e => e.Master?.Record?.recordId == lien?.MoveRecordId);
		var b = release?.BSide?.Record;
		return b != null && PublishesSong(b.publishingControl, b.publishingControllerLabelId, true) ? b : null;
	}

	/// <summary>Clyde's employer: the biggest publisher in the catalogue, by songs, ties by id.</summary>
	private static MusicPublisher WintersPublisher() => CompositionCatalogService.Publishers
		.OrderByDescending(p => p.catalogSongIds?.Count ?? 0).ThenBy(p => p.publisherId, StringComparer.Ordinal).FirstOrDefault();

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────

	internal void ProcessHolderMoveDawnForChecks(GameDate date) => ProcessHolderMoveDawn(date);
	internal void SettleHolderMoveAfterReleaseForChecks(GameDate date) => SettleHolderMoveAfterRelease(date);
	internal void DeliverArrivedPressingsForChecks(GameDate date) => DeliverArrivedPressings(date);

	/// <summary>Checks only: put a first single (with a flip, if given) at the plant as OrderPressing would, without a studio.</summary>
	internal PressOrder PressFirstSingleForChecks(string recordId, int quantity, float cost, GameDate today, GameDate arrives, Record flip = null) {
		// An assembled single's masters stay on the shelf until it ships, which is where its title is read from.
		var master = new Master { Record = new Record { recordId = recordId, title = "Pink Pedal Pushers" }, SongTitle = "Pink Pedal Pushers", Scheduled = true };
		var bside = flip == null ? null : new Master { Record = flip, SongTitle = flip.title, Scheduled = true };
		masters.Add(master);
		if (bside != null) masters.Add(bside);
		planned.Add(new PlannedRelease { Master = master, BSide = bside });
		var order = new PressOrder { RecordId = recordId, Quantity = quantity, Cost = cost, Ordered = today, Mailed = today,
			PlatingComplete = today, QueueComplete = arrives, Arrives = arrives };
		pressOrders.Add(order);
		StageHolderMove(order, today);
		return order;
	}

	/// <summary>Checks only: clear the plant, the shelf and the plan, so the next holder sees a first single again.</summary>
	internal void ClearPressingForChecks() {
		foreach (var p in planned) { masters.Remove(p.Master); if (p.BSide != null) masters.Remove(p.BSide); }
		planned.Clear(); pressOrders.Clear(); inventory.Clear();
	}

	internal PressStock StockForChecks(string recordId) => inventory.TryGetValue(recordId, out var s) ? s : null;
	internal PlayerStop HomeOpForChecks() => Label == null ? null : StopsInCity(Label.homeCityId).FirstOrDefault(s => s.Kind == StopKind.Op);
	internal string KubiakRouteRefusalForChecks(PlayerStop stop) => KubiakRouteRefusal(stop);
	internal bool SawyerLaunchWaivesForChecks(int year) => SawyerLaunchWaives(year);
}
