using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §6.1 (Phase 1e): the holder's first move on the first single. Ordering the first run stages the move two days
/// out; it lands at dawn on its day and names the holder; each holder's accept and refuse do what the directive says (Sawyer
/// waives his booking gate, Kubiak takes copies at cost on delivery or shuts the home town's jukebox operators, Winters takes
/// the flip's publishing, Germain takes his fee, Floria's favour moves the run up and can be put back); a run that arrives first
/// lapses the move and still names him; a second single never stages one; and the move survives a save.
///   --path . SimTools/HolderMoveChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints HOLDER_MOVE_CHECKS_PASS / _FAIL.
/// </summary>
public partial class HolderMoveChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	private static readonly Dictionary<LienHolder, PartyMemory> Own = new() {
		[LienHolder.Sawyer] = new() { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable },
		[LienHolder.Kubiak] = new() { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.TheLabel, End = PartyEnd.Car },
		[LienHolder.Winters] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall },
		[LienHolder.Floria] = new() { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall },
		[LienHolder.Germain] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car },
	};

	private static readonly Dictionary<LienHolder, string> Name = new() {
		[LienHolder.Sawyer] = "Dee-Dee Sawyer", [LienHolder.Kubiak] = "Walter Kubiak", [LienHolder.Winters] = "Clyde Winters",
		[LienHolder.Floria] = "Gene Floria", [LienHolder.Germain] = "Jack Germain",
	};

	private const int Quantity = 1000;
	private const float Cost = 1300f;
	private GameDate today, arrives;
	private int serial;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Holder Move Checks", "detroit", FoundingArchetype.PromoMan, null, Own[LienHolder.Sawyer], out var message), "Found: " + message);
			desk.Label.cashReserves = 5000f;
			today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			arrives = today.AddDays(20);
			foreach (var h in Enum.GetValues<LienHolder>()) { Accept(desk, h); Refuse(desk, h); }
			Lapse(desk);
			SecondSingle(desk);
			SaveRoundTrip(desk);
			SawyerPromise(desk, connieAfterDays: null);
			SawyerPromise(desk, connieAfterDays: 14);
			SawyerPromise(desk, connieAfterDays: 0);
			GD.Print($"HOLDER_MOVE_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("HOLDER_MOVE_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private PressOrder Fresh(PlayerDesk desk, LienHolder h, out string id, bool flip = false) {
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Own[h], h);
		id = $"holder-move-{h}-{++serial}";
		var b = flip ? new Record { recordId = id + "-b", title = "The Flip", publishingControl = PublishingControlType.LabelAffiliate,
			publishingControllerLabelId = desk.Label.labelId } : null;
		var order = desk.PressFirstSingleForChecks(id, Quantity, Cost, today, arrives, b);
		var lien = desk.Lien;
		Check(lien.Move == HolderMoveStage.Due && lien.MoveRecordId == id && lien.MoveDue == today.AddDays(HolderMoveDelayDays),
			$"{h}: the first run stages his move {HolderMoveDelayDays} days out");
		Check(desk.LienHolderIdentified == (h == LienHolder.Sawyer), $"{h}: " + (h == LienHolder.Sawyer ? "Connie's paper already names him" : "nobody's named yet"));
		desk.ProcessHolderMoveDawnForChecks(today.AddDays(1));
		Check(lien.Move == HolderMoveStage.Due, $"{h}: the day before, he hasn't come");
		desk.ProcessHolderMoveDawnForChecks(lien.MoveDue);
		Check(lien.Move == HolderMoveStage.Offered, $"{h}: on the day, his move is on the desk");
		var lines = desk.HolderMoveLines();
		Check(lines.Any(l => l.text.Contains(Name[h]) || l.speaker == Name[h]), $"{h}: the scene names him");
		Check(lines.All(l => !l.text.Contains("{venue}")), $"{h}: no line carries a raw placeholder");
		Check(desk.HolderMoveOptions().Count == 2, $"{h}: two answers");
		return order;
	}

	private void Accept(PlayerDesk desk, LienHolder h) {
		var order = Fresh(desk, h, out var id, flip: h == LienHolder.Winters);
		var lien = desk.Lien;
		float standing = lien.Standing, cash = desk.Label.cashReserves;
		if (h == LienHolder.Floria) Check(order.Arrives == arrives.AddDays(-FloriaFavourDays) && lien.MoveDays == FloriaFavourDays,
			$"Floria: the plant moved the run up {FloriaFavourDays} days before you heard of it");
		Check(desk.ResolveHolderMove(true, out var m), $"{h}: accepted: {m}");
		Check(lien.Move == HolderMoveStage.Accepted && desk.LienHolderIdentified, $"{h}: accepting names him");
		Check(Near(lien.Standing, Math.Min(1f, standing + HolderMoveAcceptStanding)), $"{h}: the lawyer's standing rises");
		Check(lien.Ledger.Any(e => e.Kind == LienLedgerKind.HolderMove && e.Detail.EndsWith("accepted")), $"{h}: it's on the ledger");
		Check(!desk.ResolveHolderMove(false, out _), $"{h}: he can't be answered twice");
		switch (h) {
			case LienHolder.Sawyer:
				Check(desk.SawyerLaunchWaivesForChecks(today.year) && !desk.SawyerLaunchWaivesForChecks(today.year + 1), "Sawyer: the booking gate is waived for this year only");
				break;
			case LienHolder.Kubiak: {
				int claim = Math.Min(KubiakClaimCopies, (int)(Quantity * KubiakClaimMaxShare));
				desk.DeliverArrivedPressingsForChecks(arrives);
				var stock = desk.StockForChecks(id);
				Check(stock != null && stock.Remaining == Quantity - claim, $"Kubiak: {claim} copies come off the run on delivery ({stock?.Remaining})");
				Check(Near(desk.Label.cashReserves, cash + claim * Cost / Quantity), $"Kubiak: he pays the plant's price in cash ({desk.Label.cashReserves - cash:N2})");
				Check(lien.MoveSettled, "Kubiak: the claim is settled once");
				break;
			}
			case LienHolder.Winters: {
				var flip = desk.Planned.First(p => p.Master.Record.recordId == id).BSide.Record;
				Check(flip.publishingControl == PublishingControlType.ExternalPublisher && flip.publishingControllerLabelId == null,
					"Winters: the flip's publishing is his publisher's now");
				break;
			}
			case LienHolder.Germain:
				Check(Near(desk.Label.cashReserves, cash - GermainNetworkFee), "Germain: his consulting fee is paid up front");
				Check(!lien.MoveSettled, "Germain: his boys wait for the street date");
				break;
			case LienHolder.Floria:
				Check(order.Arrives == arrives.AddDays(-FloriaFavourDays), "Floria: taking the favour keeps the earlier date");
				break;
		}
		DecemberSitting(desk, h, counted: true);
		desk.Label.cashReserves = 5000f;
	}

	/// <summary>That year's December sitting reads the answer: a favour is a Concede booked against half the interest; a refusal
	/// or a lapse is a line from the lawyer and nothing more.</summary>
	private void DecemberSitting(PlayerDesk desk, LienHolder h, bool counted) {
		var clock = TimeManager.Instance;
		var lien = desk.Lien;
		desk.CloseLienSitting();
		clock.RestoreClock(new GameDate(today.year, 12, 20), 9);
		desk.Label.cashReserves = 5000f;
		string line = desk.HolderMoveSittingLine();
		Check(!string.IsNullOrEmpty(line), $"{h}: the lawyer has a line on the answer");
		Check(desk.OpenLienSitting(out var m), $"{h}: the December sitting opens: {m}");
		var live = desk.ActiveSitting;
		Check(live.transcript.Any(l => l.text == line && l.speaker == desk.LienProfile.Lawyer), $"{h}: he opens on it: {line}");
		var favour = desk.LienSittingOptions(live).FirstOrDefault(o => o.verb == LienVerb.ConcedeFavour);
		if (!counted) {
			Check(favour == null, $"{h}: a refused or lapsed move is no favour");
		} else {
			Check(favour != null && favour.enabled, $"{h}: the favour is on the table as this year's Concede: {favour?.label}");
			float interest = lien.InterestDueBy(clock.CurrentDate), cash = desk.Label.cashReserves, standing = lien.Standing;
			float credit = (float)Math.Round(interest * HolderFavourInterestShare, 2);
			Check(desk.ChooseLienOption(live, favour, out _), $"{h}: the favour is chosen");
			Check(lien.Extended && lien.MoveCredited, $"{h}: it rolls the lien, once");
			Check(lien.Ledger.Any(e => e.Kind == LienLedgerKind.HolderMove && e.Detail.EndsWith("favour counted") && Near(e.Amount, credit)),
				$"{h}: half the interest (${credit:N2}) is booked against it");
			Check(Near(lien.InterestDue, 0f) && Near(cash - desk.Label.cashReserves, interest - credit, .05f),
				$"{h}: the till pays only the other half (${cash - desk.Label.cashReserves:N2} of ${interest:N2})");
			bool stressed = desk.LienProfile.Stresses == LienBar.Concede;
			Check(Near(lien.Standing, stressed ? Math.Min(1f, standing + .1f) : standing), $"{h}: standing moves only if Concede is his ask");
			Check(desk.LienSittingOptions(live).All(o => o.verb != LienVerb.ConcedeFavour), $"{h}: he won't count it twice");
		}
		desk.CloseLienSitting();
		clock.RestoreClock(today, 9);
	}

	private void Refuse(PlayerDesk desk, LienHolder h) {
		var order = Fresh(desk, h, out _, flip: false);
		var lien = desk.Lien;
		float standing = lien.Standing;
		if (h == LienHolder.Winters) Check(!desk.HolderMoveOptions().First(o => o.accept).enabled, "Winters: with no flip of yours, there's nothing to give him");
		Check(desk.ResolveHolderMove(false, out var m), $"{h}: refused: {m}");
		Check(lien.Move == HolderMoveStage.Refused && desk.LienHolderIdentified, $"{h}: refusing still names him");
		Check(Near(lien.Standing, Math.Max(0f, standing - HolderMoveRefuseStanding)), $"{h}: the lawyer's standing falls");
		switch (h) {
			case LienHolder.Sawyer: Check(!desk.SawyerLaunchWaivesForChecks(today.year), "Sawyer: the booking gate stands"); break;
			case LienHolder.Kubiak: {
				var op = desk.HomeOpForChecks();
				Check(op != null && desk.KubiakRouteRefusalForChecks(op) != null, "Kubiak: the home town's operators won't take your records this year");
				Check(lien.KubiakRefusedYear == today.year, "Kubiak: the refusal is for this year");
				break;
			}
			case LienHolder.Floria: Check(order.Arrives == arrives && lien.MoveDays == 0, "Floria: the run is back on its own date"); break;
		}
		DecemberSitting(desk, h, counted: false);
	}

	private void Lapse(PlayerDesk desk) {
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Own[LienHolder.Floria], LienHolder.Floria);
		var order = desk.PressFirstSingleForChecks("holder-move-lapse", Quantity, Cost, today, today.AddDays(1));
		var lien = desk.Lien;
		Check(lien.Move == HolderMoveStage.Due && lien.MoveDue == today, "A run due tomorrow stages the move for today");
		order.Arrives = today;   // the plant beat him to it
		desk.ProcessHolderMoveDawnForChecks(today);
		Check(lien.Move == HolderMoveStage.Lapsed && desk.LienHolderIdentified, "A run that comes in first lapses the move, and he's still named");
		Check(lien.Ledger.Any(e => e.Kind == LienLedgerKind.HolderMove && e.Detail.EndsWith("lapsed")), "The lapse is on the ledger");
		Check(desk.HolderMoveSittingLine()?.Contains("never gave him an answer") == true, "The sitting reads a lapse as no answer");
		DecemberSitting(desk, LienHolder.Floria, counted: false);
	}

	private void SecondSingle(PlayerDesk desk) {
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Own[LienHolder.Germain], LienHolder.Germain);
		desk.PressFirstSingleForChecks("holder-move-first", Quantity, Cost, today, arrives);
		var lien = desk.Lien;
		lien.Move = HolderMoveStage.None;
		desk.PressFirstSingleForChecks("holder-move-second", Quantity, Cost, today, arrives);
		Check(lien.Move == HolderMoveStage.None, "A second run at the plant isn't a first single: no move");
	}

	/// <summary>Taking Dee-Dee's launch is a promise: Connie's record follows yours within the weeks he gave, or he takes the
	/// waiver back, the lawyer's standing drops, and the sitting reads it as a debt. Null = Connie never releases.</summary>
	private void SawyerPromise(PlayerDesk desk, int? connieAfterDays) {
		var clock = TimeManager.Instance;
		string tag = connieAfterDays == null ? "never" : connieAfterDays == 0 ? "first" : $"after {connieAfterDays} days";
		desk.ClearPressingForChecks();
		clock.RestoreClock(today, 9);
		desk.FoundLienForChecks(Own[LienHolder.Sawyer], LienHolder.Sawyer);
		var lien = desk.Lien;
		var connie = desk.Connie;
		var act = ArtistManager.Instance.GetUnsignedArtists().First();
		string id = $"sawyer-launch-{++serial}";
		var single = desk.StageShelfMasterForChecks(act, id, "Launch Night", .5f, .5f);
		lien.Move = HolderMoveStage.Accepted; lien.MoveRecordId = id; lien.MoveDue = today; lien.SawyerLaunchYear = today.year;
		float standing = lien.Standing;
		if (connieAfterDays == 0) desk.ReleaseMasterForChecks(desk.StageShelfMasterForChecks(connie, id + "-connie", "Connie's Side", .3f, .3f));
		desk.ReleaseMasterForChecks(single);
		desk.SettleHolderMoveAfterReleaseForChecks(today);
		var due = today.AddDays(7 * SawyerPromiseWeeks);
		Check(lien.MoveSettled && lien.SawyerPromiseDue == due, $"Sawyer ({tag}): the launch sets Connie's date, {SawyerPromiseWeeks} weeks after the street date");
		if (connieAfterDays is { } days and > 0) {
			clock.RestoreClock(today.AddDays(days), 9);
			desk.ReleaseMasterForChecks(desk.StageShelfMasterForChecks(connie, id + "-connie", "Connie's Side", .3f, .3f));
		}
		desk.ProcessHolderMoveDawnForChecks(due);
		Check(!lien.SawyerPromiseBroken, $"Sawyer ({tag}): nothing happens on the day itself");
		desk.ProcessHolderMoveDawnForChecks(due.AddDays(1));
		bool kept = connieAfterDays != null;
		Check(lien.SawyerPromiseBroken == !kept && lien.SawyerPromiseDue == null, $"Sawyer ({tag}): the promise is {(kept ? "kept" : "broken")} the day after");
		Check(desk.SawyerLaunchWaivesForChecks(today.year) == kept, $"Sawyer ({tag}): the waiver " + (kept ? "stands" : "is taken back"));
		Check(Near(lien.Standing, kept ? standing : standing - SawyerPromiseBrokenStanding), $"Sawyer ({tag}): standing " + (kept ? "holds" : "drops"));
		string line = desk.HolderMoveSittingLine();
		Check(line != null && line.Contains("debt") == !kept, $"Sawyer ({tag}): the sitting reads it: {line}");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.SawyerPromiseBroken == !kept, $"Sawyer ({tag}): it survives a save");
		clock.RestoreClock(today, 9);
	}

	private void SaveRoundTrip(PlayerDesk desk) {
		Fresh(desk, LienHolder.Kubiak, out var id);
		desk.ResolveHolderMove(true, out _);
		var before = desk.Lien;
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		var after = desk.Lien;
		Check(after.Move == HolderMoveStage.Accepted && after.MoveRecordId == id && after.MoveDue == before.MoveDue && after.MoveSettled == before.MoveSettled,
			"The move survives a save");
		Check(desk.LienHolderIdentified, "So does his name");
		after.KubiakRefusedYear = 1960; after.SawyerLaunchYear = 1961; after.MoveDays = 3; after.MoveCredited = true;
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.KubiakRefusedYear == 1960 && desk.Lien.SawyerLaunchYear == 1961 && desk.Lien.MoveDays == 3, "The refusal and waiver years survive a save");
		Check(desk.Lien.MoveCredited, "So does a favour already counted");
		var old = desk.CaptureLienForChecks();
		old.Move = 0; old.MoveDue = null; old.MoveRecordId = null;
		desk.RestoreLienForChecks(old);
		Check(desk.Lien.Move == HolderMoveStage.None, "An older save has no move");
	}
}
