using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 0 (§10): a headless, player-present opening probe. It drives the real desk verbs
/// with a scripted policy and REPORTS ONLY -- it never patches the economy, invents clients, waives access, or
/// forces a negotiation to succeed. Every refusal the desk gives is recorded as evidence, not worked around.
///
/// One process per (seed, archetype, town, policy): the desk founds once per process, and a fresh world per run
/// keeps the scenarios independent. Arguments (after --):
///   --seed=1001 --archetype=ExMusician --town=new_york --policy=frugal|eager|replay|extras --days=120
///   [--road=none|residency]  residency: pitch bookers for nights for every signed act, then ask each room that has had
///     the act twice for a standing run (residencies are a booker's yes since 2026-10-14, paying a booking commission).
///   [--sell=home|working]  home = pitch/consign at home-town shops and ops only; working = also service reorders, work
///     the hop tables, open the one-stop, drop promos at stations, repress a selling title, and take a weekly road trip.
///   [--sequence=batch|staggered]  staggered cuts the second session only after the first single is out (the playtest order).
///   --policy=oracle picks the act and songs by probe TRUTH (never available to a player) to test how a strong record is treated.
///   --run=NAME  output prefix under SimLogs/ (letters, digits and hyphens).
/// Writes SimLogs/NAME-opening.json and prints OPENING_CASH_LOCK_DONE / OPENING_CASH_LOCK_FAIL.
/// </summary>
public partial class OpeningCashLockRunner : Node {
	private enum Stage { Scout, Negotiate, Record, Press, Wait, Sell, Closed }

	private PlayerDesk Desk => PlayerDesk.Instance;
	private TimeManager Clock => TimeManager.Instance;
	private string run, town, policy, road, prep, sell, sequence, backerPlan;
	// Phase 2 (§4.3): --backer=ask meets the founder's backer as soon as he'd say yes (an act he'd back), and signs the
	// largest terms he offers. none never asks. Report-only either way.
	private int? backerMeetDay, backerSignedDay, backerRefusals;
	private FoundingArchetype archetype;
	private int days;

	// Policy shape (§10): frugal = one act, cheapest room, one single; eager = two acts, mid room, two singles;
	// replay = the old playtest's $350 advance and two sessions; extras = frugal plus quoted studio extras.
	private int ActsWanted => policy is "eager" ? 2 : 1;
	private int SessionsWanted => policy is "eager" or "replay" ? 2 : 1;

	private Stage stage = Stage.Scout;
	private readonly List<string> signedIds = new();
	private PlayerDesk.Prospect negotiating;
	private readonly HashSet<string> triedProspects = new();
	private int sessionsDone, scoutNights, failedOffers;
	private readonly List<string> pressedRecordIds = new();

	// Evidence.
	private readonly List<object> actions = new();
	private readonly List<object> daily = new();
	private readonly List<object> spend = new();
	private readonly Dictionary<string, int> repeats = new();
	private int? firstL1Day, firstPreL1Day, firstNoRouteDay, stockoutDays, firstReleaseDay, firstSaleDay;
	private float? firstL1Shortfall;
	private string firstL1Detail, firstPreL1Detail;
	private object firstL1Routes;
	private List<string> firstCashOptions;   // the §4.2 card as the player would see it at the first L1 / pre-L1
	private int dayIndex;
	private GameDate startDate;
	private float startCash;

	public override void _Ready() {
		try {
			var args = OS.GetCmdlineUserArgs();
			string Arg(string name, string fallback) => args.FirstOrDefault(a => a.StartsWith("--" + name + "="))?[(name.Length + 3)..] ?? fallback;
			town = Arg("town", "new_york");
			policy = Arg("policy", "frugal");
			road = Arg("road", "none");
			prep = Arg("prep", "none");
			sell = Arg("sell", "home");
			sequence = Arg("sequence", "batch");
			backerPlan = Arg("backer", "none");
			demoPlan = Arg("demos", "none");
			if (demoPlan is not ("none" or "work")) throw new InvalidOperationException("Unknown demo plan " + demoPlan);
			if (backerPlan is not ("none" or "ask")) throw new InvalidOperationException("Unknown backer plan " + backerPlan);
			days = int.Parse(Arg("days", "120"));
			archetype = Enum.Parse<FoundingArchetype>(Arg("archetype", "ExMusician"));
			run = Arg("run", $"opening-{SimulationSeedBootstrap.RequestedSeed}-{archetype}-{town}-{policy}");
			if (run.Any(c => !char.IsLetterOrDigit(c) && c != '-')) throw new InvalidOperationException("Invalid run name");
			if (policy is not ("frugal" or "eager" or "replay" or "extras" or "discerning" or "oracle")) throw new InvalidOperationException("Unknown policy " + policy);
			if (road is not ("none" or "residency")) throw new InvalidOperationException("Unknown road option " + road);
			if (prep is not ("none" or "dealer" or "radio" or "live")) throw new InvalidOperationException("Unknown prep plan " + prep);
			if (sell is not ("home" or "working")) throw new InvalidOperationException("Unknown sell plan " + sell);
			if (sequence is not ("batch" or "staggered")) throw new InvalidOperationException("Unknown sequence " + sequence);

			if (!Desk.FoundLabel($"Probe {archetype} Records", town, archetype, out string message))
				throw new InvalidOperationException("Found: " + message);
			startDate = Clock.CurrentDate; startCash = Desk.Label.cashReserves;
			Act(true, "found", message);

			for (dayIndex = 0; dayIndex < days && !Desk.IsGameOver; dayIndex++) {
				Morning();
				if (Desk.IsGameOver) break;
				try { Step(); } catch (Exception e) { Act(false, "policy-exception", e.Message); }
				Evening();
				Clock.EndDay();
			}
			Write("done");
			GD.Print($"OPENING_CASH_LOCK_DONE run={run} seed={SimulationSeedBootstrap.RequestedSeed} days={dayIndex} closed={Desk.IsGameOver} cash={Desk.Label.cashReserves:F2} firstL1={firstL1Day?.ToString() ?? "-"} stage={stage}");
			GetTree().Quit(0);
		} catch (Exception e) {
			actions.Add(new { day = dayIndex, action = "fatal", ok = false, message = e.ToString() });
			if (run != null) Write("fail");
			GD.PushError("OPENING_CASH_LOCK_FAIL " + e);
			GetTree().Quit(3);
		}
	}

	// ── Policy ──────────────────────────────────────────────────────────────────────────────

	private void Step() {
		if (!Desk.AtHome) Act(Desk.DriveHome(out var home), "drive-home", home);
		AskBacker();
		ProduceDemosToday();
		switch (stage) {
			case Stage.Scout: Scout(); break;
			case Stage.Negotiate: Negotiate(); break;
			case Stage.Record: Record(); break;
			case Stage.Press: Press(); break;
			case Stage.Wait: case Stage.Sell:
				Prepare(); Sell();
				// Live work after the day's selling: bookers keep afternoon hours.
				if (road == "residency" && signedIds.Count > 0 && Desk.AtHome) { WorkTheRooms(); AskForResidencies(); }
				break;
		}
		PitchDemos();
	}

	// Phase 4 (§7.1): --demos=work produces every booked demo on its morning and, after the day's work, pitches up to two
	// of tonight's unsigned acts between sets. none never does. Report-only.
	private string demoPlan;
	private int demoPitches, demoNightsTried;
	private readonly Dictionary<string, int> demoBlocks = new();
	private readonly Dictionary<string, float> demoFeesByMonth = new();

	private void ProduceDemosToday() {
		if (demoPlan != "work" || !Desk.AtHome) return;
		int today = LocalSceneRoomService.Day(Clock.CurrentDate);
		foreach (var job in Desk.DemoJobs.Where(j => j.Status == PlayerDesk.DemoStatus.Booked && j.Day == today).ToList()) {
			if (Clock.CurrentHour < job.StartHour) Desk.PassTime(job.StartHour - Clock.CurrentHour, out _);
			bool ok = Desk.ProduceDemo(job.Id, out var message);
			Act(ok, "demo-produce", message, job.ArtistName, job.Fee);
			if (ok && job.Status == PlayerDesk.DemoStatus.Delivered) {
				string month = $"{Clock.CurrentDate.year}-{Clock.CurrentDate.month:00}";
				demoFeesByMonth[month] = demoFeesByMonth.GetValueOrDefault(month) + job.Fee;
			}
		}
	}

	private void PitchDemos() {
		if (demoPlan != "work" || !Desk.AtHome || Desk.IsGameOver) return;
		var tonight = Desk.DemoProspects(Desk.CurrentCityId);
		if (tonight.Count == 0) return;
		demoNightsTried++;
		foreach (var p in tonight) {
			string why = p.Block == null ? "pitchable" : p.Block.Contains("looking") ? "not looking" : p.Block.Contains("money") ? "no money"
				: p.Block.Contains("said no") ? "said no" : p.Block.Contains("booked") ? "already booked" : p.Block.Contains("room") ? "no room" : "not yet on";
			demoBlocks[why] = demoBlocks.GetValueOrDefault(why) + 1;
		}
		int pitched = 0;
		foreach (var p in tonight.Where(p => p.Block == null || p.Block.StartsWith("Catch them")).ToList()) {
			if (pitched >= 2) break;
			if (Clock.CurrentHour < p.Bill.StartHour) {
				if (Clock.HoursRemainingWithOvertime < p.Bill.StartHour - Clock.CurrentHour + PlayerDesk.DemoOfferHours) break;
				Desk.PassTime(p.Bill.StartHour - Clock.CurrentHour, out _);
			}
			bool ok = Desk.OfferDemo(p.Act.artistId, out var message);
			Act(ok, "demo-pitch", message, p.Act.stageName);
			if (ok) { demoPitches++; pitched++; }
			if (!ok && message != null && message.Contains("hard stop")) break;
		}
	}

	private object DemoSummary() {
		var jobs = Desk.DemoJobs;
		return new {
			plan = demoPlan, nightsTried = demoNightsTried, pitches = demoPitches, booked = jobs.Count,
			delivered = jobs.Count(j => j.Status == PlayerDesk.DemoStatus.Delivered),
			missed = jobs.Count(j => j.Status == PlayerDesk.DemoStatus.LabelMissed),
			clientCancelled = jobs.Count(j => j.Status == PlayerDesk.DemoStatus.ClientCancelled),
			stillBooked = jobs.Count(j => j.Status == PlayerDesk.DemoStatus.Booked),
			fees = jobs.Where(j => j.Status == PlayerDesk.DemoStatus.Delivered).Sum(j => j.Fee),
			feesByMonth = demoFeesByMonth, blocks = demoBlocks,
			grossToClients = jobs.Where(j => j.Status == PlayerDesk.DemoStatus.Delivered).Sum(j => j.Gross),
		};
	}

	/// <summary>§4.3 policy: once the backer would say yes on what he's told he wants, meet him in his hours and sign the
	/// largest terms. The meeting's hours are spent like any player's; nothing is forced.</summary>
	private void AskBacker() {
		if (backerPlan != "ask" || Desk.Backer != null) return;
		var p = Desk.BackerProspectFor();
		if (p == null) return;
		if (Desk.PendingBackerOffer is { } open) { Sign(open); return; }
		if (Desk.EvaluateBacker(p).Refusal is { } wont) { if (signedIds.Count > 0) Repeat("backer-wont: " + wont); return; }
		int from = p.Source == PlayerDesk.BackerSource.ClubOwner ? PlayerDesk.BookerFromHour : PlayerDesk.BackerOfficeFromHour;
		if (Clock.CurrentHour < from && Desk.BackerMeetingBlock(p) is { } hours && !hours.Contains("said no")) Desk.PassTime(Desk.HoursUntil(from), out _);
		if (Desk.BackerMeetingBlock(p) is { } block) { Repeat("backer-blocked: " + block); return; }
		bool ok = Desk.MeetBacker(out var message);
		Act(ok, "backer-meet", message, p.Source.ToString(), p.Name);
		backerMeetDay ??= dayIndex;
		if (Desk.PendingBackerOffer is { } offer) Sign(offer); else backerRefusals = (backerRefusals ?? 0) + 1;

		void Sign(PlayerDesk.BackerOffer o) {
			int best = o.Terms.FindIndex(t => t.Principal == o.Terms.Max(x => x.Principal));
			if (Act(Desk.AcceptBackerTerms(best, out var signed), "backer-sign", signed, o.Terms[best].Principal, o.Terms[best].Rate)) backerSignedDay = dayIndex;
		}
	}

	private object BackerSummary() {
		var loan = Desk.Backer;
		var today = Clock.CurrentDate;
		return new {
			plan = backerPlan, source = Desk.BackerProspectFor()?.Source.ToString(), meetDay = backerMeetDay, signedDay = backerSignedDay, refusals = backerRefusals,
			principal = loan?.Principal, rate = loan?.Rate, principalPaid = loan?.PrincipalPaid, interestPaid = loan?.InterestPaid,
			outstanding = loan?.Outstanding, interestDue = loan?.InterestDue, arrears = loan?.PrincipalArrears(today), missed = loan?.Missed, standing = loan?.Standing,
			seizedCopies = loan?.Ledger.Where(e => e.Kind is PlayerDesk.BackerLedgerKind.CollateralInterest or PlayerDesk.BackerLedgerKind.CollateralPrincipal).Sum(e => e.Copies),
			repaid = loan?.Repaid, ledgerAgrees = loan?.LedgerAgrees(),
		};
	}

	// Off-market acts asked to think an offer over (PlayerDesk.DirectApproach), kept until their side answers.
	private readonly Dictionary<string, PlayerDesk.Prospect> considering = new(StringComparer.Ordinal);
	// Discerning policy: every act heard so far, so it can compare across nights.
	private readonly Dictionary<string, PlayerDesk.Prospect> heard = new(StringComparer.Ordinal);
	private static readonly PlayerDesk.ScoutingVenue[] ScoutRotation = {
		PlayerDesk.ScoutingVenue.ClubsAndRoadhouses, PlayerDesk.ScoutingVenue.HonkyTonks, PlayerDesk.ScoutingVenue.TheatresAndSupperClubs };

	private void Scout() {
		// An act that agreed to hear an offer comes first: that is a real, reachable deal.
		foreach (var (id, waiting) in considering.ToList()) {
			var status = Desk.DirectApproaches.LastOrDefault(a => a.ArtistId == id)?.Status;
			if (status == ScoutingApproachStatus.Considering) continue;
			considering.Remove(id);
			if (status != ScoutingApproachStatus.Ready || !string.IsNullOrEmpty(waiting.Artist.labelId)) continue;
			if (Act(Desk.ApproachToSign(waiting, out var ready), "approach-ready", ready, waiting.Artist.stageName, waiting.AskingAdvance)) {
				negotiating = waiting; stage = Stage.Negotiate; return;
			}
		}
		if (scoutNights >= 30) { Repeat("scout-exhausted"); return; }
		if (Clock.CurrentHour < 17 && !Desk.PassTime(Desk.HoursUntil(17), out var wait)) { Act(false, "wait-for-clubs", wait); return; }
		scoutNights++;
		// Rotate the kinds of room a founder would try: clubs, then roadhouses and coffeehouses, then supper rooms. A town whose
		// acts work the roadhouses (Billings) is otherwise invisible to a clubs-only policy.
		var venue = ScoutRotation[(scoutNights - 1) % ScoutRotation.Length];
		if (!Act(Desk.ScoutVenue(venue, out var message), "scout-" + venue, message)) { Repeat("scout-no-bill"); return; }
		// Frugal-style pick: the smallest ask among unsigned acts with enough heard material for an A and a B side,
		// acts already on the market before off-market ones. Replay's fixed $350 needs an ask that advance can meet.
		var picks = Desk.Slate.Where(p => string.IsNullOrEmpty(p.Artist.labelId) && !triedProspects.Contains(p.Artist.artistId) && p.LiveSet.Count >= 2)
			.Where(p => policy != "replay" || p.AskingAdvance <= 350f)
			.OrderBy(p => Desk.CanAskDirectApproach(p.Artist) ? 1 : 0).ThenBy(p => p.AskingAdvance).ThenBy(p => p.Artist.artistId, StringComparer.Ordinal).ToList();
		if (policy is "discerning" or "oracle") {
			// A competent opening (§10): the best act the player can READ, among asks the label can afford while still
			// cutting and pressing a single; it listens around for a few nights before committing. Perceived only, no truth.
			float budget = Desk.Label.cashReserves - 2f * Desk.Label.GetMonthlyOverhead() - Desk.PreviewReleaseCompletion().Remaining
				.Where(s => s.Kind != PlayerDesk.CompletionStageKind.Signing).Sum(s => s.Cost);
			foreach (var p in Desk.Slate.Where(p => string.IsNullOrEmpty(p.Artist.labelId) && p.LiveSet.Count >= 2)) heard[p.Artist.artistId] = p;
			picks = heard.Values.Where(p => string.IsNullOrEmpty(p.Artist.labelId) && !triedProspects.Contains(p.Artist.artistId) && p.AskingAdvance <= budget)
				.OrderByDescending(p => policy == "oracle" ? TrueActHook(p.Artist) : p.ReadQuality).ThenBy(p => p.AskingAdvance).ThenBy(p => p.Artist.artistId, StringComparer.Ordinal).ToList();
			if (policy == "discerning" && scoutNights < 5 && !picks.Any(p => p.ReadQuality >= .6f)) { Repeat("discerning-still-listening"); return; }
			// Oracle: listen around for two weeks, then take the truly strongest affordable act.
			if (policy == "oracle" && scoutNights < 14) { Repeat("oracle-still-listening"); return; }
		}
		if (picks.Count == 0) { Repeat("scout-no-suitable"); return; }
		foreach (var pick in picks.Take(3)) {
			triedProspects.Add(pick.Artist.artistId);
			bool open = Desk.ApproachToSign(pick, out message);
			Act(open, "approach", message, pick.Artist.stageName, pick.AskingAdvance);
			if (open) { negotiating = pick; stage = Stage.Negotiate; return; }
			if (Desk.DirectApproaches.LastOrDefault(a => a.ArtistId == pick.Artist.artistId)?.Status == ScoutingApproachStatus.Considering)
				considering[pick.Artist.artistId] = pick;
		}
	}

	private void Negotiate() {
		var p = negotiating;
		var terms = p.Talk?.ask ?? p.Baseline;
		float advance = policy == "replay" ? Math.Max(350f, terms.Advance) : terms.Advance;
		var plan = Desk.PreviewOfferResources(advance);
		string warned = Desk.CompletionWarning(advance + (plan?.PendingAppointmentFees ?? 0f), PlayerDesk.CompletionStageKind.Signing);   // §4.1, shown on the offer form
		float cashBefore = Desk.Label.cashReserves;
		bool signed = p.Talk != null
			? Desk.TableOffer(p.Talk, advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation, terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out var message)
			: Desk.OfferContract(p, advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation, terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out message);
		signed &= p.Artist.labelId == Desk.Label.labelId;
		Act(signed, "offer", message, p.Artist.stageName, advance, plan?.CashAfterFeesAndSession, warned);
		if (!signed) {
			negotiating = null;
			stage = ++failedOffers >= 6 ? Stage.Closed : Stage.Scout;
			return;
		}
		Spent("advance", cashBefore - Desk.Label.cashReserves);
		signedIds.Add(p.Artist.artistId); negotiating = null;
		stage = signedIds.Count < ActsWanted ? Stage.Scout : Stage.Record;
	}

	private void Record() {
		if (Desk.Session != null) { FinishSession(); return; }
		var act = ArtistManager.Instance.GetArtist(signedIds[Math.Min(sessionsDone, signedIds.Count - 1)]);
		var options = Desk.MaterialOptionsFor(act);
		var refused = Desk.MaterialRefusals(act, options).ToHashSet();
		if (policy == "discerning") {
			var reads = Desk.RepertoireFor(act.artistId).GroupBy(r => r.Title).ToDictionary(g => g.Key, g => g.Max(r => r.ReadHook));
			options = options.OrderByDescending(o => reads.GetValueOrDefault(o.Title)).ThenBy(o => o.Title, StringComparer.Ordinal).ToList();
		} else if (policy == "oracle") {
			options = options.OrderByDescending(o => o.WrittenSong?.Hook ?? (o.HasSong ? o.Hook : 0f)).ThenBy(o => o.Title, StringComparer.Ordinal).ToList();
		}
		var picks = options.Where(o => !refused.Any(r => r.Contains($"“{o.Title}”"))).Take(2).ToList();
		bool overrideRefusal = false;
		if (picks.Count < 2 && !act.artistCreativeControl) { picks = options.Take(2).ToList(); overrideRefusal = true; }
		if (picks.Count == 0) { Repeat("record-no-material"); return; }

		// Room: the cheapest bookable local room for frugal/extras/replay, a middle room for eager.
		var offers = Desk.StudioRoomIds().Select(id => Desk.StudioOffer(id, act.artistId)).Where(o => o != null && o.Locked == null && o.Local).ToList();
		if (offers.Count == 0) { Repeat("record-no-room"); return; }
		var offer = policy == "eager"
			? offers.OrderBy(o => Math.Abs((int)o.Tier - (int)PlayerDesk.StudioTier.Mid)).ThenBy(o => o.Room.HourlyRate).First()
			: offers.OrderBy(o => o.Room.HourlyRate).First();
		int hours = PlayerDesk.MinSessionHours;
		var structures = picks.Select(c => StudioSongStructures.For(c, act, Clock.CurrentDate.year)).ToList();
		while (hours < 8 && offer.Room.SetupMinutes + structures.Sum(s => StudioSongStructures.PassMinutes(s) * 2 + PlayerDesk.StudioFinishMinutesPerSide) > hours * 60) hours++;

		if (policy == "extras" && act.members.Count > 1) {
			var weakest = act.members.Where(m => m.isActive).OrderBy(m => m.technicalSkill).First();
			float before = Desk.Label.cashReserves;
			Act(Desk.BookSessionPlayer(act, weakest, out var hire), "book-session-player", hire, weakest.firstName);
			Spent("session-player-booking", before - Desk.Label.cashReserves);
		}
		if (Clock.CurrentHour > 10) { Repeat("record-wait-morning"); return; }
		string warnedStudio = Desk.CompletionWarning(PlayerDesk.StudioRoomCost(offer.Room, hours) + offer.Gas, PlayerDesk.CompletionStageKind.Studio);   // §4.1, shown on the booking sheet
		float cash = Desk.Label.cashReserves;
		bool backing = policy == "extras" && PlayerDesk.StudioNeedsBacking(act);
		bool ok = Desk.StartSession(act, picks, offer.Tier, hours, out var message, overrideRefusal, roomId: offer.Room.Id, backingSingers: backing);
		Act(ok, "start-session", message, offer.Room.Name, hours, PlayerDesk.StudioRoomCost(offer.Room, hours), warnedStudio);
		if (!ok) { NoteUnfunded("studio", PlayerDesk.StudioRoomCost(offer.Room, hours) + offer.Gas); return; }
		Spent("studio", cash - Desk.Label.cashReserves);
		FinishSession();
	}

	private void FinishSession() {
		var session = Desk.Session;
		for (int guard = 0; guard < 40; guard++) {
			int side = session.Cuts.Select((c, i) => (c, i)).OrderBy(x => x.c.Takes.Count).First().i;
			if (!Desk.CutStudioTake(side, out _)) break;
		}
		Desk.DelegateTakeSelection();
		bool printed = Desk.PrintSession(out var message);
		Act(printed, "print-session", message, Desk.Masters.Count);
		if (!printed) { Repeat("print-blocked"); return; }
		sessionsDone++;
		stage = sequence == "batch" && sessionsDone < SessionsWanted ? Stage.Record : Stage.Press;
	}

	private void Press() {
		// Pair unassembled masters into singles (A and B), then press every assembled, unpressed single.
		var shelf = Desk.Masters.Where(m => !m.Scheduled && !m.Released).ToList();
		while (shelf.Count >= 2) {
			var a = shelf[0]; var b = shelf.Skip(1).FirstOrDefault(m => m.Record.artistId == a.Record.artistId) ?? shelf[1];
			Act(Desk.AssembleSingle(a, b, out var assembled), "assemble-single", assembled);
			shelf.Remove(a); shelf.Remove(b);
		}
		foreach (var single in Desk.Planned.ToList()) {
			string id = single.Master.Record.recordId;
			if (pressedRecordIds.Contains(id)) continue;
			int qty = Desk.MinimumPressRun(id);
			float cost = PlayerDesk.PressingCost(qty, Desk.HasBeenPressed(id));
			float cash = Desk.Label.cashReserves;
			bool ok = Desk.OrderPressing(id, qty, Desk.SuggestedPromoCount(id, qty), out var message);
			Act(ok, "order-pressing", message, id, qty, cost);
			if (!ok) { if (unfundedNoted.Add(id + "|" + dayIndex / 7)) NoteUnfunded("pressing", cost); continue; }
			Spent("pressing", cash - Desk.Label.cashReserves);
			pressedRecordIds.Add(id);
			var order = Desk.PressingOrderFor(id);
			int lead = Math.Max(1, DaysBetween(Clock.CurrentDate, order.Arrives));
			Act(Desk.SetReleaseDate(single, lead, 0, out message), "set-release-date", message, lead);
		}
		if (pressedRecordIds.Count > 0 && (Desk.Planned.All(p => pressedRecordIds.Contains(p.Master.Record.recordId)) || Desk.ReleasedRecords.Any()))
			stage = Desk.ReleasedRecords.Any() ? Stage.Sell : Stage.Wait;
	}

	/// <summary>"Appropriate selling": work the home town's shops and ops with what's on hand, one approach per
	/// account per day as the desk enforces; press-to-fill when calls are open and the cash is there.</summary>
	private void Sell() {
		// Staggered (the playtest's order): the next session is cut a week after the first single is out, on what's left.
		if (sequence == "staggered" && sessionsDone < SessionsWanted && firstReleaseDay != null && dayIndex >= firstReleaseDay + 7) {
			Record();
			if (stage == Stage.Record) return;
		}
		if (Desk.Planned.Any(p => !pressedRecordIds.Contains(p.Master.Record.recordId)) || Desk.Masters.Count(m => !m.Scheduled && !m.Released) >= 2) { Press(); }
		if (sell == "working") { SellWorking(); return; }
		var released = Desk.ReleasedRecords.Select(r => r.baseRecord.recordId).Where(id => !Desk.IsMasterOut(id)).ToList();
		if (released.Count > 0 && firstReleaseDay == null) firstReleaseDay = dayIndex;
		if (released.Count > 0) stage = Stage.Sell;
		foreach (string id in released) {
			var stock = Desk.StockFor(id);
			if (stock == null || stock.Remaining <= 0) {
				if (Desk.PressingOrderFor(id) != null) continue;
				if (Desk.OpenCallDemand(id) > 0) {
					float cost = PlayerDesk.PressingCost(Desk.PressToFillQuantity(id), Desk.HasBeenPressed(id));
					if (Desk.Label.cashReserves >= cost) { float c = Desk.Label.cashReserves; Act(Desk.PressToFill(id, out var m), "press-to-fill", m, id); Spent("repress", c - Desk.Label.cashReserves); }
					else if (Desk.PlantCreditEligible(id)) Act(Desk.RequestPlantCredit(id, out var m), "plant-credit", m, id);
				}
				continue;
			}
			foreach (var stop in Desk.StopsInCity(Desk.Label.homeCityId).Where(s => s.Kind is PlayerDesk.StopKind.Shop or PlayerDesk.StopKind.Op).ToList()) {
				if (Desk.HasWorkedStopToday(stop.StopId) || (Desk.StockFor(id)?.Remaining ?? 0) <= 0) continue;
				if (stop.OnHand.TryGetValue(id, out var lot) && lot.Remaining > 2) continue;   // still stocked: leave it to sell
				bool passed = stop.PassedRecordIds.Contains(id);
				bool ok = passed ? Desk.ConsignAtStop(stop.StopId, id, out var message) : Desk.PitchAtStop(stop.StopId, id, out message);
				if (!ok && message != null && message.Contains("hard stop")) { Repeat("sell-day-full"); return; }   // the day is spent
				Act(ok, passed ? "consign" : "pitch", message, stop.DisplayName);
				if (ok && firstSaleDay == null) firstSaleDay = dayIndex;
			}
		}
		if (released.Count == 0) Repeat("wait-for-plant");
	}


	// ── Working sell plan: every channel a player in 1960 could work with the stock on hand ─────────

	private readonly HashSet<string> unfundedNoted = new(StringComparer.Ordinal);
	private readonly HashSet<string> droppedAtStation = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> channelActs = new(StringComparer.Ordinal);
	private readonly List<string> citiesWorked = new();
	private int lastCartonDay = -99, roadTrips;
	private void Channel(string what) => channelActs[what] = channelActs.GetValueOrDefault(what) + 1;

	private void SellWorking() {
		var released = Desk.ReleasedRecords.Select(r => r.baseRecord.recordId).Where(id => !Desk.IsMasterOut(id)).ToList();
		if (released.Count > 0) { firstReleaseDay ??= dayIndex; stage = Stage.Sell; }
		if (released.Count == 0) { Repeat("wait-for-plant"); return; }
		foreach (string id in released) Restock(id);
		var selling = released.Where(id => (Desk.StockFor(id)?.Remaining ?? 0) > 0).ToList();
		if (selling.Count == 0) return;
		// Once a week, with a box to spare, take the record to the next town over and come back the next morning.
		if (dayIndex % 7 == 3 && selling.Sum(id => Desk.StockFor(id).Remaining) >= 60) {
			string here = Desk.Label.homeCityId;
			var near = Desk.ReachableCities().OrderBy(c => DistanceModel.GetRoadMilesBetween(here, c.cityId)).ThenBy(c => c.cityId, StringComparer.Ordinal).Take(3).ToList();
			if (near.Count > 0) {
				var city = near[roadTrips % near.Count];
				float gas = Desk.Label.cashReserves;
				// Only with the gas home and something over: a founder who can't buy the drive back is stranded (see notes).
				float roundTrip = 2f * DistanceModel.GetRoadMilesBetween(here, city.cityId) * 0.03f;
				if (Desk.Label.cashReserves >= roundTrip + 50f && Act(Desk.DriveTo(city.cityId, out var drove), "drive-to", drove, city.cityId)) {
					roadTrips++; citiesWorked.Add(city.cityId); Spent("gas", gas - Desk.Label.cashReserves);
				}
			}
		}
		// F1: a room's table is a dated night. An early bill (a hall or roadhouse afternoon) comes first; otherwise the
		// counters until the booker's hours, ask for table rights, the counters until set-up, then the night.
		var tonight = TableNightToday();
		int setUp = tonight != null ? tonight.Bill.StartHour - 1 : 99;
		if (tonight != null && setUp < PlayerDesk.BookerFromHour) {
			foreach (string id in selling) if (!WorkTown(Desk.CurrentCityId, id, setUp)) return;
			WorkTableNight(tonight, selling); tonight = null;
		}
		foreach (string id in selling) if (!WorkTown(Desk.CurrentCityId, id, Math.Min(setUp, PlayerDesk.BookerFromHour))) return;
		AskTableRights();
		if (tonight != null) {
			foreach (string id in selling) if (!WorkTown(Desk.CurrentCityId, id, setUp)) return;
			WorkTableNight(tonight, selling);
		}
		foreach (string id in selling) if (!WorkTown(Desk.CurrentCityId, id)) return;
	}

	// ── F1 room tables ─────────────────────────────────────────────────────────────────────────
	private int tableAsks, tableRightsWon, tableNights, tableNightsCancelled, tableNightsEmpty, tableCopies, tableNightsOwnAct;
	private readonly Dictionary<string, int> tableAskedDay = new(StringComparer.Ordinal);

	private PlayerDesk.RoomTableNight TableNightToday() {
		if (!PlayerDesk.RoomTablesActive(Desk.CurrentCityId)) return null;
		int today = LocalSceneRoomService.Day(Clock.CurrentDate), now = Clock.CurrentHour * 60 + Clock.CurrentMinute;
		return Desk.RoomTableNights(Desk.CurrentCityId).Where(n => n.Bill.Day == today && (n.Bill.StartHour - 1) * 60 >= now)
			.OrderByDescending(n => n.OwnAct).ThenByDescending(n => n.Bill.ExpectedAudience).ThenBy(n => n.Room.Id, StringComparer.Ordinal).FirstOrDefault();
	}

	/// <summary>At home, once a term per room: ask each selling room's booker for a table (the desk enforces a standing no).</summary>
	private void AskTableRights() {
		if (!Desk.AtHome || !PlayerDesk.RoomTablesActive(Desk.CurrentCityId)) return;
		if (Clock.CurrentHour > PlayerDesk.BookerFromHour) return;
		var rooms = SceneRoomCatalog.ForPlace(Desk.CurrentCityId).Where(PlayerDesk.RoomSellsRecords)
			.Where(r => Desk.TableRightsAt(r.Id) == null && (!tableAskedDay.TryGetValue(r.Id, out int last) || dayIndex - last >= PlayerDesk.TableRightsTermDays / 3)).ToList();
		if (rooms.Count == 0) return;
		if (Clock.CurrentHour < PlayerDesk.BookerFromHour && !Desk.PassTime(Desk.HoursUntil(PlayerDesk.BookerFromHour), out _)) return;
		foreach (var room in rooms) {
			if (Clock.CurrentHour >= room.CloseHour) continue;
			tableAskedDay[room.Id] = dayIndex;
			bool ok = Desk.AskForTableRights(room.Id, out var message);
			if (!ok) { Repeat("table-rights-not-now"); continue; }
			tableAsks++;
			bool won = Desk.TableRightsAt(room.Id) != null; if (won) tableRightsWon++;
			Act(true, won ? "table-rights-won" : "table-rights-refused", message, room.Name);
		}
	}

	/// <summary>Set up an hour before the first set with the title the room's regulars want most, and stay to the end.</summary>
	private void WorkTableNight(PlayerDesk.RoomTableNight night, List<string> selling) {
		string id = selling.Where(s => (Desk.StockFor(s)?.Remaining ?? 0) > 0).OrderByDescending(s => Desk.RoomTableBuyersLeft(night.Room.Id, s)).ThenBy(s => s, StringComparer.Ordinal).FirstOrDefault();
		if (id == null) return;
		int setUp = night.Bill.StartHour - 1;
		if (Clock.CurrentHour < setUp && !Desk.PassTime(Desk.HoursUntil(setUp), out _)) return;
		int before = Desk.StockFor(id).Remaining;
		bool ok = Desk.WorkRoomTable(night.Room.Id, id, out var message);
		Act(ok, "room-table", message, night.Room.Name, night.OwnAct);
		if (!ok) return;
		int sold = before - (Desk.StockFor(id)?.Remaining ?? 0);
		tableNights++; tableCopies += sold;
		if (night.OwnAct) tableNightsOwnAct++;
		if (message.Contains("didn't go ahead")) tableNightsCancelled++;
		else if (sold == 0) tableNightsEmpty++;
		if (sold > 0) { Channel("room-table"); firstSaleDay ??= dayIndex; }
	}

	/// <summary>Repress a title that sold out: to fill open calls, or when it has already moved half a run.</summary>
	private void Restock(string id) {
		var stock = Desk.StockFor(id);
		if (stock != null && stock.Remaining > 0) return;
		if (Desk.PressingOrderFor(id) != null) return;
		var rec = Desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord.recordId == id);
		bool demand = Desk.OpenCallDemand(id) > 0 || (rec?.totalUnitsSold ?? 0) >= PlayerDesk.PressMinimumOrder / 2;
		if (!demand) return;
		float cost = PlayerDesk.PressingCost(Desk.PressToFillQuantity(id), Desk.HasBeenPressed(id));
		if (Desk.OpenCallDemand(id) > 0 && Desk.Label.cashReserves >= cost) {
			float c = Desk.Label.cashReserves; Act(Desk.PressToFill(id, out var m), "press-to-fill", m, id); Spent("repress", c - Desk.Label.cashReserves); return;
		}
		int qty = Desk.MinimumPressRun(id);
		cost = PlayerDesk.PressingCost(qty, Desk.HasBeenPressed(id));
		if (Desk.Label.cashReserves >= cost) {
			float c = Desk.Label.cashReserves;
			Act(Desk.OrderPressing(id, qty, 0, out var m), "repress", m, id, qty, cost); Spent("repress", c - Desk.Label.cashReserves);
		} else if (Desk.PlantCreditEligible(id)) Act(Desk.RequestPlantCredit(id, out var m), "plant-credit", m, id);
		else if (unfundedNoted.Add("repress|" + id + "|" + dayIndex / 7)) NoteUnfunded("repress", cost);
	}

	/// <summary>Work every account in a town for one title. False once the day is spent.</summary>
	private bool WorkTown(string cityId, string id, int untilHour = 99) {
		bool Out() => (Desk.StockFor(id)?.Remaining ?? 0) <= 0;
		bool Full(bool ok, string message) => !ok && message != null && message.Contains("hard stop");
		foreach (var stop in Desk.StopsInCity(cityId).ToList()) {
			if (Out()) return true;
			if (Clock.CurrentHour + 2 > untilHour) return true;   // leave time for a booker or a table night
			if (Desk.HasWorkedStopToday(stop.StopId)) continue;
			bool ok; string message; string verb;
			switch (stop.Kind) {
				case PlayerDesk.StopKind.Shop: case PlayerDesk.StopKind.Op:
					stop.OnHand.TryGetValue(id, out var lot);
					if (lot != null && lot.ReturnRequested && lot.Remaining > 0) {
						ok = Desk.AcceptReturn(stop.StopId, id, out message); verb = "accept-return";
					} else if (lot != null && (Desk.HasOpenCall(stop.StopId, id) || lot.Remaining <= 2 || stop.OpenBalance > 0.5f)) {
						ok = Desk.ServiceStop(stop.StopId, id, out message); verb = "service";
					} else if (lot != null) continue;   // still stocked: leave it to sell
					else if (stop.PassedRecordIds.Contains(id)) { ok = Desk.ConsignAtStop(stop.StopId, id, out message); verb = "consign"; }
					else { ok = Desk.PitchAtStop(stop.StopId, id, out message); verb = "pitch"; }
					break;
				case PlayerDesk.StopKind.Venue:
					if (PlayerDesk.RoomTablesActive(cityId)) continue;   // tables are room nights now (WorkTableNight)
					ok = Desk.WorkTheHopTable(stop.StopId, id, out message); verb = "table";
					break;
				case PlayerDesk.StopKind.OneStop:
					if (!stop.OneStopUnlocked) {
						if (!Desk.HasOpenCall(stop.StopId, id)) continue;
						ok = Desk.VisitOneStopWarehouse(stop.StopId, out message); verb = "one-stop-open";
					} else {
						int onHand = Desk.StockFor(id)?.Remaining ?? 0;
						if (dayIndex - lastCartonDay < 14 || onHand < 150) continue;
						ok = Desk.SellCartonToOneStop(stop.StopId, id, onHand / 3, out message); verb = "one-stop-carton";
						if (ok) lastCartonDay = dayIndex;
					}
					break;
				case PlayerDesk.StopKind.Station:
					if (!droppedAtStation.Add(stop.StopId + "|" + id)) continue;
					ok = Desk.DropOffAtStation(stop.StopId, id, out message); verb = "station-drop";
					break;
				default: continue;
			}
			if (Full(ok, message)) { Repeat("sell-day-full"); return false; }
			Act(ok, verb, message, stop.DisplayName, cityId);
			if (ok) { Channel(verb); if (verb is "pitch" or "consign" or "table" or "one-stop-carton") firstSaleDay ??= dayIndex; }
		}
		return true;
	}

	/// <summary>Probe TRUTH proxy for how strong an act's own take will be (the studio hook formula's inputs).</summary>
	private static float TrueActHook(SimulatedArtist a) => a.studioPerformance * .5f + a.livePerformance * .2f + a.vocalPower * .3f;

	// ── §6.4 preparation plans, run while the plant has the record ─────────────────────────────

	private int acetatePitches, acetateOrders, callsPlaced, callsConnected, listensArranged, acetatesDelivered, bookerPitches, bookingsWon;
	private readonly HashSet<string> deliveredAcetates = new(StringComparer.Ordinal);
	private readonly HashSet<string> pitchedRooms = new(StringComparer.Ordinal);

	/// <summary>The A-side of the first pressed single while it is still unreleased, or null.</summary>
	private PlayerDesk.Master PrepMaster() {
		var released = Desk.ReleasedRecords.Select(r => r.baseRecord.recordId).ToHashSet();
		return Desk.Planned.Where(p => pressedRecordIds.Contains(p.Master.Record.recordId) && !released.Contains(p.Master.Record.recordId))
			.Select(p => Desk.Masters.FirstOrDefault(m => m.Record?.recordId == p.Master.Record.recordId)).FirstOrDefault(m => m != null && !m.Released);
	}

	private void Prepare() {
		if (prep == "none" || stage != Stage.Wait) return;
		var master = PrepMaster();
		if (master == null) return;
		string id = master.Record.recordId;
		if (prep is "dealer" or "radio" && Desk.AcetatesFor(id) == 0 && !deliveredAcetates.Contains(id) && Desk.Label.cashReserves >= 20f) {
			float c = Desk.Label.cashReserves;
			Act(Desk.CutAcetate(id, out var cut), "cut-acetate", cut, id);
			Spent("acetate", c - Desk.Label.cashReserves);
		}
		if (prep == "dealer") {
			if (Desk.AcetatesFor(id) == 0) return;
			foreach (var stop in Desk.StopsInCity(Desk.Label.homeCityId).Where(s => s.Kind is PlayerDesk.StopKind.Shop or PlayerDesk.StopKind.Op).ToList()) {
				if (Desk.HasWorkedStopToday(stop.StopId) || stop.PassedRecordIds.Contains(id) || Desk.HasOpenCall(stop.StopId, id)) continue;
				int before = Desk.AdvanceOrdersFor(id).Copies;
				bool ok = Desk.PitchAcetateAtStop(stop.StopId, id, out var message);
				if (!ok && message != null && message.Contains("hard stop")) { Repeat("prep-day-full"); return; }
				if (!ok) { Repeat("prep-acetate-refused"); continue; }
				acetatePitches++;
				bool took = Desk.AdvanceOrdersFor(id).Copies > before; if (took) acetateOrders++;
				Act(true, took ? "acetate-order" : "acetate-pass", message, stop.DisplayName);
			}
		} else if (prep == "radio") {
			if (Desk.Rolodex.Count < 3 && Desk.CanStillWorkThePhones()) Act(Desk.WorkThePhones(out var phones), "work-phones", phones);
			// Call each man inside the hours his card gives, once they're known (the first miss teaches them), as a
			// player reading the card would. Later windows are reached by waiting at the office until they open.
			foreach (var entry in Desk.Rolodex.OrderBy(e => EntryFrom(e)).ToList()) {
				if (entry.shiftKnown && entry.appointmentRecordId != id) {
					var (from, to) = RolodexShifts.ReachableWindow(RolodexShifts.ShiftOf(ChartManager.Instance.GetDeejay(entry.djId)));
					if (Clock.CurrentHour > to) { Repeat("prep-call-outside-window"); continue; }
					if (Clock.CurrentHour < from && !Desk.PassTime(from - Clock.CurrentHour, out _)) continue;
				}
				// A listen he agreed to is kept by putting the acetate in his hands at the station.
				if (entry.appointmentRecordId == id && Desk.AcetatesFor(id) > 0) {
					bool ok = Desk.DeliverAcetateToStation(id, entry.stationId, out var delivered);
					Act(ok, "deliver-acetate", delivered, entry.displayName);
					if (ok) { acetatesDelivered++; deliveredAcetates.Add(id); }
					continue;
				}
				var call = Desk.PlaceCall(entry, null, out var dial);
				if (call == null) { Repeat("prep-call-refused"); continue; }
				callsPlaced++;
				if (call.stage == CallStage.Open) {
					callsConnected++;
					var choice = Desk.ApproachOptions(call).FirstOrDefault(o => o.enabled && o.approach is RolodexApproach.ArrangeListen or RolodexApproach.IntroduceLabel or RolodexApproach.AskWhatsMoving);
					if (choice != null) {
						Desk.ChooseApproach(call, choice.approach, null, out var said);
						if (choice.approach == RolodexApproach.ArrangeListen) listensArranged++;
						Act(true, "courtesy-" + choice.approach, said, entry.displayName);
						if (choice.approach == RolodexApproach.IntroduceLabel && !introduced.Add(entry.djId)) Violation($"introduced the label twice to {entry.displayName}");
					} else Repeat("prep-nothing-new-to-say");
				} else Act(false, "call-missed", dial, entry.displayName);
				Desk.EndCall(call);
			}
		} else if (prep == "live" && road != "residency") WorkTheRooms();
	}

	/// <summary>§6.3: pitch every suitable room's booker for a night for each signed act (once per room and act until it plays).</summary>
	private void WorkTheRooms() {
		{
			if (Clock.CurrentHour < PlayerDesk.BookerFromHour && !Desk.PassTime(Desk.HoursUntil(PlayerDesk.BookerFromHour), out _)) return;
			foreach (string artistId in signedIds) {
				var act = ArtistManager.Instance.GetArtist(artistId);
				foreach (var room in SceneRoomCatalog.ForPlace(Desk.CurrentCityId).Where(r => r.IsPerformance)) {
					string key = room.Id + "|" + artistId;
					if (pitchedRooms.Contains(key) || Clock.CurrentHour >= room.CloseHour) continue;
					int year = Clock.CurrentDate.year;
					if (!room.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family)) continue;
					int first = LocalSceneRoomService.FirstBookableDay, night = -1;
					for (int d = first; d <= first + LocalSceneRoomService.BookingHorizonDays && night < 0; d++)
						if (SceneLiveEconomics.OpenOn(room, LocalSceneRoomService.Date(d).DayOfWeek)) night = d;
					if (night < 0) continue;
					pitchedRooms.Add(key);
					int before = LocalSceneRoomService.Bookings.Count;
					bool ok = Desk.PitchActToBooker(room.Id, artistId, night, out var message);
					if (ok) bookerPitches++;
					if (LocalSceneRoomService.Bookings.Count > before) bookingsWon++;
					Act(ok, "booker-pitch", message, room.Name, act.stageName);
				}
			}
		}
	}

	private int residencyAsks, residenciesWon;
	private readonly Dictionary<string, int> lastResidencyAsk = new(StringComparer.Ordinal);
	/// <summary>Once a week per act, ask for a standing run; the desk names the room (one the act has played twice) or says why not.</summary>
	private void AskForResidencies() {
		if (Clock.CurrentHour < PlayerDesk.BookerFromHour && !Desk.PassTime(Desk.HoursUntil(PlayerDesk.BookerFromHour), out _)) return;
		foreach (string artistId in signedIds) {
			if (Desk.RoadBookingFor(artistId)?.ResidencyRoomId != null) continue;
			if (lastResidencyAsk.TryGetValue(artistId, out int last) && dayIndex - last < 7) continue;
			lastResidencyAsk[artistId] = dayIndex;
			var act = ArtistManager.Instance.GetArtist(artistId);
			bool ok = Desk.BookRoad(act, 8, 0, out var message);
			if (!ok) { Repeat("residency-not-yet"); continue; }
			residencyAsks++;
			bool won = Desk.RoadBookingFor(artistId)?.ResidencyRoomId != null; if (won) residenciesWon++;
			Act(true, won ? "residency-won" : "residency-refused", message, act.stageName, Desk.RoadBookingFor(artistId)?.ResidencyWeeklyCommission);
		}
	}

	/// <summary>Probe-only TRUTH for each pressed single (never shown to, or used by, the policy): the hidden hook and
	/// production, the act's abilities, where the hook sits among every single released that year, and what it sold.
	/// It lets a closure be read as a weak record meeting the market, or a good one the market wouldn't take.</summary>
	private object RecordTruth() {
		var all = ChartManager.Instance.GetAllRecords().Where(r => r.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single).ToList();
		return pressedRecordIds.Select(id => {
			var released = Desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord.recordId == id);
			var rec = released?.baseRecord ?? Desk.Masters.FirstOrDefault(m => m.Record?.recordId == id)?.Record;
			if (rec == null) return null;
			var act = ArtistManager.Instance.GetArtist(rec.artistId);
			var sameYear = all.Where(r => r.baseRecord.releaseDate.year == (rec.releaseDate.year > 0 ? rec.releaseDate.year : Clock.CurrentDate.year)).ToList();
			float Pct(Func<Record, float> f) => sameYear.Count == 0 ? float.NaN : sameYear.Count(r => f(r.baseRecord) <= f(rec)) / (float)sameYear.Count;
			var stock = Desk.StockFor(id);
			return new {
				id, title = rec.title, rec.hookStrength, rec.productionQuality,
				hookPct = Pct(r => r.hookStrength), productionPct = Pct(r => r.productionQuality), singlesThatYear = sameYear.Count,
				act = act?.stageName, live = act?.livePerformance, studio = act?.studioPerformance, act?.songwritingAbility, act?.reputation,
				pressed = stock?.TotalPressed, onHand = stock?.Remaining, unitsSold = released?.totalUnitsSold,
				firstRunBreakEvenCopies = (int)Math.Ceiling(PlayerDesk.PressingCost(PlayerDesk.PressMinimumOrder) / PlayerDesk.DealerPrice)
			};
		}).Where(x => x != null).ToList();
	}

	private static int EntryFrom(RolodexEntry e) => e.shiftKnown ? RolodexShifts.ReachableWindow(RolodexShifts.ShiftOf(ChartManager.Instance.GetDeejay(e.djId))).From : 0;

	private object PrepSummary() {
		var orders = Desk.Planned.Select(p => p.Master.Record.recordId).Concat(Desk.ReleasedRecords.Select(r => r.baseRecord.recordId)).Distinct()
			.Select(id => Desk.AdvanceOrdersFor(id)).ToList();
		var bookings = LocalSceneRoomService.Bookings.Where(b => b.LabelId == Desk.Label?.labelId).ToList();
		return new {
			prep, acetatePitches, acetateOrders, ordersStillHeld = orders.Sum(o => o.Copies),
			callsPlaced, callsConnected, listensArranged, acetatesDelivered,
			bookerPitches, bookingsWon,
			bookingsPerformed = bookings.Count(b => b.Status == SceneBillStatus.Performed),
			bookingsFellThrough = bookings.Count(b => b.Status == SceneBillStatus.Cancelled),
			residencyAsks, residenciesWon,
			tableAsks, tableRightsWon, tableNights, tableNightsOwnAct, tableNightsCancelled, tableNightsEmpty, tableCopies,
			following = signedIds.Select(a => new {
				act = a, home = Math.Round(Desk.LocalFollowing(a, Desk.Label?.homeCityId), 3),
				lift = Desk.Label == null ? 0 : Math.Round(Desk.LocalAppealLift(a, ArtistManager.Instance.GetArtist(a)?.primaryGenre ?? default, Desk.Label.homeCityId), 3),
			}).ToList(),
			bookingNotes = bookings.Select(b => $"{b.RoomId} d{b.Day} {b.Status}: {b.Note}").ToList()
		};
	}

	// ── Diagnostics (§1.2) ──────────────────────────────────────────────────────────────────

	private float stockoutCash;
	private int unpressableDays;
	private int? firstUnpressableDay;
	private void Morning() { }

	private readonly List<string> violations = new();
	private readonly HashSet<string> introduced = new(StringComparer.Ordinal);
	private HashSet<string> heldYesterday = new(StringComparer.Ordinal);
	private void Violation(string what) { if (violations.Count < 50) violations.Add($"day {dayIndex}: {what}"); }

	/// <summary>Phase 1B invariants, checked every evening: a pre-order never lapses before its vinyl lands; a booking whose
	/// night has passed is resolved (billed and played, or cancelled with a note).</summary>
	private void CheckInvariants() {
		var released = Desk.ReleasedRecords.Select(r => r.baseRecord.recordId).ToHashSet();
		var held = Desk.PendingCalls().Where(c => c.Call.Reason == PlayerDesk.InboundCallReason.PreOrder).Select(c => c.Call.StopId + "|" + c.Call.RecordId).ToHashSet(StringComparer.Ordinal);
		foreach (string gone in heldYesterday.Where(k => !held.Contains(k))) {
			string rec = gone[(gone.IndexOf('|') + 1)..];
			if (!released.Contains(rec) && Desk.PressingOrderFor(rec) != null) Violation($"pre-order {gone} lapsed while its run was still at the plant");
		}
		heldYesterday = held;
		int today = LocalSceneRoomService.Day(Clock.CurrentDate);
		foreach (var b in LocalSceneRoomService.Bookings.Where(b => b.Day < today - 1 && b.Status == SceneBillStatus.Scheduled))
			Violation($"booking {b.RoomId} day {b.Day} still unresolved (billed={b.Billed})");
	}

	private void Evening() {
		CheckInvariants();
		var label = Desk.Label;
		var assembled = Desk.Planned.Select(p => p.Master.Record.recordId).Concat(Desk.ReleasedRecords.Select(r => r.baseRecord.recordId))
			.Where(id => id != null && !Desk.IsMasterOut(id)).Distinct().ToList();
		int sellable = assembled.Sum(id => Desk.StockFor(id)?.Remaining ?? 0);
		int inFlight = assembled.Count(id => Desk.PressingOrderFor(id) != null);
		var stops = Desk.StopsInCity(label.homeCityId).ToList();
		float accountBalances = Desk.KnownAccounts().Sum(s => s.OpenBalance);
		int consigned = Desk.KnownAccounts().Sum(s => s.OnHand.Values.Sum(l => l.Remaining));
		float wholesale = label.wholesaleReceivables?.Sum(r => r.Amount) ?? 0f;
		float cheapest = assembled.Count == 0 ? float.NaN : assembled.Min(id => PlayerDesk.PressingCost(Desk.MinimumPressRun(id), Desk.HasBeenPressed(id)));
		bool released = Desk.ReleasedRecords.Any();
		var waiting = Desk.Planned.Where(p => !pressedRecordIds.Contains(p.Master.Record.recordId) && Desk.PressingOrderFor(p.Master.Record.recordId) == null).ToList();
		if (waiting.Count > 0 && waiting.All(p => label.cashReserves < PlayerDesk.PressingCost(Desk.MinimumPressRun(p.Master.Record.recordId), false))) {
			unpressableDays++; firstUnpressableDay ??= dayIndex;
		}
		if (released && sellable == 0 && inFlight == 0) stockoutDays = (stockoutDays ?? 0) + 1;

		// L1: an assembled title exists, cash can't fund its cheapest legal run, and nothing is coming in.
		bool l1 = assembled.Count > 0 && label.cashReserves < cheapest && sellable == 0 && inFlight == 0 && accountBalances <= 0 && wholesale <= 0 && consigned == 0;
		// Before any single is assembled: the cheapest remaining completion (a budget session + a first run) is unfunded.
		float completion = Desk.Masters.Count >= 2 ? PlayerDesk.PressingCost(PlayerDesk.PressMinimumOrder)
			: Desk.SessionCost(PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours) + PlayerDesk.PressingCost(PlayerDesk.PressMinimumOrder);
		bool preL1 = assembled.Count == 0 && signedIds.Count > 0 && label.cashReserves < completion;
		var routes = l1 || preL1 ? Routes(assembled) : null;
		if ((l1 || preL1) && firstCashOptions == null) firstCashOptions = Desk.CashOptions().Select(o => $"{(o.Executable ? "live" : "blocked")} {o.Certainty}: {o.Route} -- {o.Why}").ToList();
		if (l1 && firstL1Day == null) { firstL1Day = dayIndex; firstL1Shortfall = cheapest - label.cashReserves; firstL1Routes = routes; firstL1Detail = $"cheapest legal run ${cheapest:F0}, cash ${label.cashReserves:F0}"; }
		if (preL1 && firstPreL1Day == null) { firstPreL1Day = dayIndex; firstPreL1Detail = $"completion ${completion:F0} ({(Desk.Masters.Count >= 2 ? "pressing" : "session+pressing")}), cash ${label.cashReserves:F0}"; firstL1Routes ??= routes; }
		if ((l1 || preL1) && firstNoRouteDay == null && routes is RouteRead r && r.Executable == 0) firstNoRouteDay = dayIndex;

		daily.Add(new {
			day = dayIndex, date = Clock.CurrentDate.ToShortString(), stage = stage.ToString(), cash = Round(label.cashReserves),
			overhead = Round(label.GetMonthlyOverhead()), red = Desk.MonthsInTheRed, sellable, inFlight, consigned,
			balances = Round(accountBalances), wholesale = Round(wholesale), l1, preL1,
			residencies = signedIds.Count(id => Desk.RoadBookingFor(id)?.WeeksRemaining > 0)
		});
	}

	private sealed record RouteRead(int Executable, int Speculative, int Blocked, List<string> Lines);

	/// <summary>§1.2 L1b: inspect every existing recovery route independently and classify it. Read-only.</summary>
	private RouteRead Routes(List<string> assembled) {
		var lines = new List<string>(); int exec = 0, spec = 0, blocked = 0;
		void Line(string kind, string text) { lines.Add(kind + ": " + text); if (kind == "executable") exec++; else if (kind == "speculative") spec++; else blocked++; }
		var releasedIds = Desk.ReleasedRecords.Select(r => r.baseRecord.recordId).ToList();
		foreach (var offer in Desk.MasterOffers)
			Line("executable", $"master offer from {offer.BuyerName} on {offer.RecordId}: best ${offer.Terms.Max(t => t.Advance):F0}");
		var shoppable = Desk.ShoppableMasterIds().ToList();
		if (shoppable.Count == 0) Line("blocked", "master deal: nothing recorded to shop");
		foreach (string id in shoppable) {
			int buyers = Desk.MasterBuyersFor(id).Count(b => b.Block == null);
			Line(buyers > 0 ? "speculative" : "blocked", $"shop the master {id}: {buyers} label(s) in its sound with cash to pay");
		}
		foreach (string id in releasedIds) {
			if (Desk.PlantCreditEligible(id)) Line("executable", $"plant credit on {id} (open calls {Desk.OpenCallDemand(id)})");
			else Line("blocked", $"plant credit on {id}: open calls {Desk.OpenCallDemand(id)} under threshold or one owed");
		}
		int residencies = signedIds.Count(id => Desk.RoadBookingFor(id)?.WeeksRemaining > 0);
		if (residencies > 0) Line("executable", $"{residencies} active residency booking(s) paying ${signedIds.Sum(id => Desk.RoadBookingFor(id)?.ResidencyWeeklyCommission ?? 0f):F0}/wk in commission");
		else if (signedIds.Count > 0) Line("speculative", "ask a room the act has played twice for a residency (booking commission)");
		else Line("blocked", "road income: no signed act");
		Line("speculative", "distribution deal: an offer may or may not come and pays nothing up front");
		return new RouteRead(exec, spec, blocked, lines);
	}

	private void NoteUnfunded(string stageName, float cost) =>
		actions.Add(new { day = dayIndex, action = "unfunded", ok = false, stage = stageName, cost = Round(cost), cash = Round(Desk.Label.cashReserves) });

	// ── Bookkeeping ─────────────────────────────────────────────────────────────────────────

	private bool Act(bool ok, string action, string message, params object[] detail) {
		actions.Add(new { day = dayIndex, date = Clock.CurrentDate.ToShortString(), hour = Clock.CurrentHour, action, ok, message, cash = Round(Desk.Label?.cashReserves ?? 0), detail });
		return ok;
	}
	private void Repeat(string what) => repeats[what] = repeats.GetValueOrDefault(what) + 1;
	private void Spent(string what, float amount) { if (amount > 0.001f) spend.Add(new { day = dayIndex, what, amount = Round(amount) }); }
	private static float Round(float v) => float.IsFinite(v) ? MathF.Round(v, 2) : v;
	private static int DaysBetween(GameDate a, GameDate b) =>
		(int)(new DateTime(b.year, b.month, b.day) - new DateTime(a.year, a.month, a.day)).TotalDays;
	private object CashAt(int d) => daily.Count > d ? daily[d] : null;

	private void Write(string status) {
		var label = Desk.Label;
		var summary = new {
			status, run, seed = SimulationSeedBootstrap.RequestedSeed, archetype = archetype.ToString(), town, policy, road, prep, sell, sequence, days,
			start = startDate.ToShortString(), startCash, overhead = label?.GetMonthlyOverhead(), creditFloor = Desk.CreditFloor,
			closed = Desk.IsGameOver, closure = Desk.GameOverReason, closureDay = Desk.IsGameOver ? dayIndex : (int?)null,
			finalCash = label?.cashReserves, stage = stage.ToString(), signed = signedIds.Count, sessionsDone, singlesPressed = pressedRecordIds.Count,
			firstPreL1Day, firstPreL1Detail, firstL1Day, firstL1Shortfall, firstL1Detail, firstL1Routes, firstCashOptions, firstNoRouteDay,
			firstReleaseDay, firstSaleDay, stockoutDays, firstUnpressableDay, unpressableDays, roadTrips, citiesWorked, channelActs,
			prepared = PrepSummary(),
			backer = BackerSummary(),
			demos = DemoSummary(),
			violations,
			records = RecordTruth(),
			day30 = CashAt(30), day60 = CashAt(60), day90 = CashAt(90),
			spend, repeats, actions, daily
		};
		File.WriteAllText(ProjectSettings.GlobalizePath($"res://SimLogs/{run}-opening.json"),
			JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true,
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }));
	}
}
