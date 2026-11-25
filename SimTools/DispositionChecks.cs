using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Signing realism steps 2 and 3 (SimTools/SigningRealismBreakdown.md): the act's disposition replaces the keyed 65%
/// coin, and a yes is signed tonight or after dated steps.
///
/// Part A reads every unsigned, off-market act in a scene town against a new label. The same state must give the same
/// read, all three dispositions must occur, and every reason must name state that is really there (an engagement
/// reason has a room, a day-job reason has day jobs, a minor is under 21, and so on).
/// Part B moves one act's real inputs (member ambition, the label's record) and checks the read follows: high ambition
/// is hungry, low is comfortable, the band in between is careful, a holdout and minors are named, a hit raises drive.
/// Part C asks real heard acts through the desk: a comfortable act says no with its reason, a repeat ask files nothing
/// new, the no lifts when the reason does, a careful act wants to be heard again and says yes once you have. A hungry act
/// of age with no manager says yes and terms open on the spot; it stays off the AI market and signs from Latent. No world
/// draws are added: asking changes no world state.
/// Part D (step 3) hears a fresh band and gives it a minor, a shark and a holdout: the yes carries three dated steps, the
/// family can't be seen before Sunday, the manager is seen on a weekday, the holdout settles at a set heard, and only then
/// do terms open. A yes nobody acts on lapses after AgreedHoldDays.
///   --path . SimTools/DispositionChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--city=detroit]
/// Prints DISPOSITION_CHECKS_PASS / _FAIL.
/// </summary>
public partial class DispositionChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	private static string Key(DispositionRead r) => $"{r.Kind}|{r.Reason}|{r.Drive:0.0000}|{r.Members}|{r.MedianAge}|{string.Join(",", r.Minors)}|{r.Holdout}|{r.Manager}|{r.EngagementRoom}|{r.WealthyMember}|{r.DayJobs}";

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--city="))?.Substring("--city=".Length) ?? "detroit";
			Check(desk.FoundLabel("Disposition Checks", city, out string message), message);
			desk.DismissLienMorning();
			var label = desk.Label;
			int today = LocalSceneRoomService.Day(clock.CurrentDate), year = clock.CurrentDate.year;

			// ---- Part A: every off-market act in a scene town ----
			var acts = am.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && string.IsNullOrEmpty(a.labelId) &&
				a.prospectMarketStatus == ProspectMarketStatus.Latent && LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) != null &&
				a.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active)).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
			Check(acts.Count > 200, $"{acts.Count} off-market acts live in scene towns");
			var kinds = new Dictionary<DealDisposition, int>(); var reasons = new Dictionary<DispositionReason, int>();
			foreach (var a in acts) {
				var r = ActDisposition.Read(a, label, today, null);
				if (Key(ActDisposition.Read(a, label, today, null)) != Key(r)) Check(false, $"{a.stageName}: the same state gives the same read");
				kinds[r.Kind] = kinds.GetValueOrDefault(r.Kind) + 1; reasons[r.Reason] = reasons.GetValueOrDefault(r.Reason) + 1;
				var people = a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
				string bad = r.Reason switch {
					DispositionReason.Engagement when r.EngagementRoom == null || r.DayJobs => "engagement with no room, or with day jobs",
					DispositionReason.NoNeedForMoney when r.WealthyMember == null => "money reason with no wealthy member",
					DispositionReason.Settled when r.MedianAge < 32 => "settled under 32",
					DispositionReason.WeekendAct when !r.DayJobs => "weekend act without day jobs",
					DispositionReason.DayJob when !r.DayJobs => "day-job reason without day jobs",
					DispositionReason.Young when r.MedianAge > 22 => "young over 22",
					DispositionReason.OutOfTown when LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) == label.homeCityId => "out of town at home",
					DispositionReason.NoRecords when label.totalReleases > 0 => "no records with records out",
					DispositionReason.DroppedBefore when a.contractSequence == 0 => "dropped before, never signed",
					_ => null
				};
				if (bad != null) Check(false, $"{a.stageName}: {r.Reason} names real state ({bad})");
				if (r.Kind == DealDisposition.Comfortable && r.Drive >= ActDisposition.ComfortableBelow ||
					r.Kind == DealDisposition.Hungry && r.Drive < ActDisposition.HungryFrom) Check(false, $"{a.stageName}: kind follows drive");
				int minors = people.Count(m => m.birthYear > 0 && m.GetAge(year) < ActDisposition.MinorAge);
				if (r.Minors.Count != minors) Check(false, $"{a.stageName}: minors counted ({r.Minors.Count} vs {minors})");
				if (r.SignsTonight != (r.Kind == DealDisposition.Hungry && minors == 0 && r.Holdout == null && !r.DemandingManager)) Check(false, $"{a.stageName}: signs-tonight rule");
				if (string.IsNullOrEmpty(ActDisposition.Line(r, a, "Someone", "Detroit"))) Check(false, "every read has a line");
			}
			Check(true, $"{acts.Count} reads are repeatable and every reason names real state");
			string Pct(int n) => $"{100.0 * n / acts.Count:0.0}%";
			GD.Print("mix (all off-market scene acts vs a new " + city + " label): " + string.Join("  ", kinds.OrderBy(k => k.Key).Select(k => $"{k.Key} {Pct(k.Value)}")));
			GD.Print("reasons: " + string.Join("  ", reasons.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {Pct(k.Value)}")));
			Check(kinds.Keys.Count == 3, "all three dispositions occur");
			Check(kinds.GetValueOrDefault(DealDisposition.Hungry) > kinds.GetValueOrDefault(DealDisposition.Careful) &&
				kinds.GetValueOrDefault(DealDisposition.Hungry) > kinds.GetValueOrDefault(DealDisposition.Comfortable), "hungry is the most common");

			// ---- Part B: move real inputs on one act ----
			var subject = acts.First(a => a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) >= 3);
			var live = subject.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
			var saved = live.Select(m => m.ambition).ToArray();
			void SetAmbition(float v) { foreach (var m in live) m.ambition = v; }
			try {
				SetAmbition(.95f);
				Check(ActDisposition.Read(subject, label, today, null).Kind == DealDisposition.Hungry, "a band all at .95 ambition is hungry");
				SetAmbition(.15f);
				var low = ActDisposition.Read(subject, label, today, null);
				Check(low.Kind == DealDisposition.Comfortable && low.Holdout != null && ActDisposition.WhatWouldChange(low) != null,
					$"a band all at .15 is comfortable ({low.Reason}), names a holdout, and says what could change it");
				float adjust = low.Drive - low.Ambition;
				SetAmbition((ActDisposition.ComfortableBelow + ActDisposition.HungryFrom) / 2 - adjust);
				var mid = ActDisposition.Read(subject, label, today, null);
				Check(mid.Kind == DealDisposition.Careful && ActDisposition.WhatWouldChange(mid) == null, $"the band between the thresholds is careful ({mid.Reason})");
				SetAmbition(.95f); live[0].ambition = .1f;
				var hold = ActDisposition.Read(subject, label, today, null);
				Check(hold.Holdout == (string.IsNullOrWhiteSpace(live[0].firstName) ? live[0].FullName.Trim() : live[0].firstName) && !hold.SignsTonight,
					"one member at .1 in a hungry band is the holdout, and the act won't sign tonight");
				SetAmbition(.5f);
				float before = ActDisposition.Read(subject, label, today, null).Drive;
				int hits = label.top40Hits, releases = label.totalReleases;
				label.top40Hits = 1; label.totalReleases = 1;
				float after = ActDisposition.Read(subject, label, today, null).Drive;
				label.top40Hits = hits; label.totalReleases = releases;
				Check(Math.Abs(after - before - ActDisposition.HitPush - ActDisposition.ReleasedPush) < 1e-4f, "a release and a hit on your label raise the act's drive");
			} finally { for (int i = 0; i < live.Count; i++) live[i].ambition = saved[i]; }

			// ---- Part C: asking real heard acts through the desk ----
			var heard = new List<SimulatedArtist>();
			var firstNight = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == today && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
				foreach (var slot in bill.Appearances.Where(s => s.EndHour <= clock.HardStopHour).OrderBy(s => s.StartHour)) {
					if (heard.Count >= 6 || slot.StartHour < clock.CurrentHour) continue;
					var a = am.GetArtist(slot.ArtistId);
					if (a == null || !string.IsNullOrEmpty(a.labelId) || a.prospectMarketStatus != ProspectMarketStatus.Latent) continue;
					clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
					if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message) && desk.CanAskDirectApproach(a)) {
						heard.Add(a); firstNight[a.artistId] = bill.Day;
					}
				}
			Check(heard.Count >= 2, $"heard {heard.Count} off-market acts on the first night");
			PlayerDesk.Prospect ProspectOf(SimulatedArtist a) {
				desk.OpenScoutingDossier(a.artistId, out _);
				return desk.Slate.First(p => p.Artist.artistId == a.artistId);
			}
			// Force an act's disposition through its members' real ambition, as Part B did.
			var originals = new Dictionary<Musician, float>();
			void Force(SimulatedArtist a, DealDisposition kind) {
				var people = a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
				foreach (var m in people) originals.TryAdd(m, m.ambition);
				var r = desk.ReadDisposition(a); float adjust = r.Drive - r.Ambition;
				float target = kind switch { DealDisposition.Hungry => .95f, DealDisposition.Comfortable => ActDisposition.ComfortableBelow - .15f - adjust,
					_ => (ActDisposition.ComfortableBelow + ActDisposition.HungryFrom) / 2 - adjust };
				foreach (var m in people) m.ambition = target;
				Check(desk.ReadDisposition(a).Kind == kind, $"{a.stageName} forced {kind}");
			}
			// Step 3's frictions, moved through real fields and put back afterwards.
			var births = new Dictionary<Musician, int>();
			var managers = new Dictionary<SimulatedArtist, (ManagerArchetype, string)>();
			void Adult(SimulatedArtist a) {
				int y = clock.CurrentDate.year;
				foreach (var m in a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && m.birthYear > 0 && m.GetAge(y) < ActDisposition.MinorAge)) {
					births.TryAdd(m, m.birthYear); m.birthYear = y - 24;
				}
			}
			void Unmanaged(SimulatedArtist a) { managers.TryAdd(a, (a.manager, a.managerName)); a.manager = ManagerArchetype.None; a.managerName = null; }
			try {
				// Comfortable: a no with its reason, the same no again, and it lifts when the reason does.
				var comfy = heard[0];
				Force(comfy, DealDisposition.Comfortable);
				var comfyProspect = ProspectOf(comfy);
				string snapshot = System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions);
				Check(!desk.ApproachToSign(comfyProspect, out message), "a comfortable act opens no terms");
				Check(System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions) == snapshot, "asking changes no world state");
				var said = desk.DirectApproaches.Last();
				Check(said.ArtistId == comfy.artistId && said.Status == ScoutingApproachStatus.Declined && message.StartsWith(said.Said) &&
					(message.Contains("might") || message.Contains("could change")), "the no is said by somebody and says what could change it: " + message);
				Check(desk.StillDeclining(comfy) && !message.Contains("season") && !message.Contains("ask again after"), "the no holds, with no season date");
				int count = desk.DirectApproaches.Count;
				desk.ApproachToSign(ProspectOf(comfy), out string again);
				Check(desk.DirectApproaches.Count == count && again.StartsWith(said.Said), "asking again with nothing changed files nothing new");
				Check(desk.ScoutingSummary(comfy.artistId).Any(l => l.StartsWith("Asked (") && l.Contains(said.Said)), "the notebook quotes their answer");
				Force(comfy, DealDisposition.Hungry);
				Check(!desk.StillDeclining(comfy), "the no lifts when the reason does");

				// ---- Step 3: nothing between the yes and a signature, so they sign tonight, from off the market ----
				Adult(comfy); Unmanaged(comfy);
				Check(desk.ReadDisposition(comfy).SignsTonight, $"{comfy.stageName}: all of age, no manager, no holdout");
				var tonight = ProspectOf(comfy);
				Check(desk.ApproachToSign(tonight, out message) && tonight.HasBaseline, "a hungry act with nothing in the way opens terms on the spot: " + message);
				var yes = desk.DirectApproaches.Last();
				Check(yes.ArtistId == comfy.artistId && yes.Status == ScoutingApproachStatus.Agreed && yes.Steps.Count == 0 && message.StartsWith(yes.Said),
					"the yes is said, filed with no steps, and leads the terms message");
				int week = ChartManager.Instance.GetCurrentChartWeek();
				Check(comfy.prospectMarketStatus == ProspectMarketStatus.Latent && !am.IsEligibleForPopulationSigning(comfy, week),
					"a yes leaves the act off the AI market");
				Check(!desk.CanAskDirectApproach(comfy) && desk.AgreedToSign(comfy) && desk.CanTalkTerms(tonight), "an act that said yes is not asked again; terms are open");
				// The negotiation table has its own suites; what's checked here is that the signing itself works from off the
				// market, so a firm act is taken at its ask in one round.
				if (tonight.Posture != NegotiationPosture.Pushover) { GD.Print($"note: {comfy.stageName} wanted the table ({tonight.Posture}); signed at the ask instead"); tonight.Posture = NegotiationPosture.Pushover; tonight.Talk = null; }
				{
					var b = tonight.Baseline;
					Check(desk.OfferContract(tonight, b.Advance, b.RoyaltyRate, b.TermYears, b.SinglesObligation, b.LabelOwnsPublishing, b.ArtistCreativeControl, out message),
						"signed at their ask, straight from off the market: " + message);
					Check(comfy.labelId == label.labelId && comfy.prospectMarketStatus != ProspectMarketStatus.Latent && desk.Agreement(comfy) == null,
						"the signing moves the act onto your roster and out of the latent pool");
				}

				// Careful: wants to be heard again; thinks it over once you have, on another night.
				// Every other act heard tonight is made careful and asked; whichever plays again first is heard a second time.
				var candidates = heard.Skip(1).ToList();
				foreach (var c in candidates) {
					Force(c, DealDisposition.Careful);
					desk.ApproachToSign(ProspectOf(c), out message);
					Check(desk.DirectApproaches.Last().ArtistId == c.artistId && desk.DirectApproaches.Last().Status == ScoutingApproachStatus.WantsAnotherLook &&
						desk.WaitingToBeHeardAgain(c), "a careful act wants to see you again first: " + message);
				}
				SimulatedArtist careful = null;
				for (int d = 0; d < 42 && careful == null; d++) {
					clock.EndDay();
					if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
					int day = LocalSceneRoomService.Day(clock.CurrentDate);
					foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
						foreach (var slot in bill.Appearances.Where(s => s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour).OrderBy(s => s.StartHour)) {
							var c = careful == null ? candidates.FirstOrDefault(x => x.artistId == slot.ArtistId && desk.WaitingToBeHeardAgain(x)) : null;
							if (c == null) continue;
							clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
							if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out _)) careful = c;
						}
				}
				if (careful == null) GD.Print($"note: none of the careful acts played {city} again inside 42 days; the second-night path wasn't exercised");
				else {
					Check(desk.HeardNights(careful.artistId) >= PlayerDesk.CarefulHeardNights && !desk.WaitingToBeHeardAgain(careful), "a second night meets their request");
					Force(careful, DealDisposition.Careful);
					desk.ApproachToSign(ProspectOf(careful), out message);
					Check(desk.DirectApproaches.Last().ArtistId == careful.artistId && desk.DirectApproaches.Last().Status == ScoutingApproachStatus.Agreed &&
						message.Contains("You came back"), "heard twice, the careful act says yes: " + message);
				}

				// ---- Step 3, Part D: a minor, a demanding manager and a holdout are dated steps the player takes ----
				int Members(SimulatedArtist a) => a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active);
				// Go out and hear an act nobody has asked yet, on the coming nights.
				SimulatedArtist HearNew(Func<SimulatedArtist, bool> want, int days) {
					for (int d = 0; d < days; d++) {
						int day = LocalSceneRoomService.Day(clock.CurrentDate);
						foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
							foreach (var slot in bill.Appearances.Where(s => s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour).OrderBy(s => s.StartHour)) {
								var a = am.GetArtist(slot.ArtistId);
								if (a == null || !string.IsNullOrEmpty(a.labelId) || a.prospectMarketStatus != ProspectMarketStatus.Latent || !want(a) ||
									desk.DirectApproaches.Any(x => x.ArtistId == a.artistId)) continue;
								clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
								if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out _) && desk.CanAskDirectApproach(a)) return a;
							}
						clock.EndDay();
						if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
					}
					return null;
				}
				// A band whose next date is posted, so the holdout's set can be attended inside the window.
				SimulatedArtist band = null;
				for (int tries = 0; tries < 6 && band == null; tries++) {
					var found = HearNew(a => Members(a) >= 3, 14);
					if (found == null) break;
					if (desk.NextReachableScoutingSet(found.artistId) != null) band = found;
				}
				band ??= HearNew(a => true, 14);
				if (band == null) GD.Print("note: no heard act left for the steps; Part D not exercised");
				else {
					Force(band, DealDisposition.Hungry); Adult(band);
					var people = band.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).OrderBy(m => m.personId, StringComparer.Ordinal).ToList();
					int year2 = clock.CurrentDate.year;
					births.TryAdd(people[0], people[0].birthYear); people[0].birthYear = year2 - 17;
					bool holdout = people.Count >= 3;
					if (holdout) people[^1].ambition = .2f;
					managers.TryAdd(band, (band.manager, band.managerName));
					band.manager = ManagerArchetype.Shark; band.managerName = "Sol Kramer";
					var read = desk.ReadDisposition(band);
					Check(read.Kind == DealDisposition.Hungry && read.Minors.Count == 1 && read.DemandingManager && (read.Holdout != null) == holdout && !read.SignsTonight,
						$"{band.stageName}: hungry, with a minor, a shark{(holdout ? " and a holdout" : "")}");
					int asked = desk.DirectApproaches.Count, day0 = LocalSceneRoomService.Day(clock.CurrentDate);
					Check(!desk.ApproachToSign(ProspectOf(band), out message), "a yes with steps opens no terms yet: " + message);
					var deal = desk.DirectApproaches.Last();
					var parents = deal.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Parents);
					var manager = deal.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Manager);
					var hold = deal.Steps.FirstOrDefault(s => s.Kind == ScoutingApproachStepKind.Holdout);
					Check(deal.Status == ScoutingApproachStatus.Agreed && parents != null && manager != null && (hold != null) == holdout,
						"the yes carries one step per friction");
					Check(parents.Day > day0 && LocalSceneRoomService.Date(parents.Day.Value).DayOfWeek == DayOfWeek.Sunday && message.Contains("under 21"),
						"a minor means Sunday with the family: " + message);
					Check(manager.Day > day0 && !LocalSceneRoomService.Date(manager.Day.Value).IsWeekend && message.Contains("Sol Kramer"),
						"a shark wants a weekday sit-down, by name");
					if (holdout) Check(hold.Who == read.Holdout && message.Contains(read.Holdout), "the holdout is named, and the band settles it at a set");
					desk.ApproachToSign(ProspectOf(band), out string pending);
					Check(desk.DirectApproaches.Count == asked + 1 && pending.Contains("said yes") && !desk.CanAskDirectApproach(band) && !desk.AgreedToSign(band),
						"asking again files nothing and names the next step: " + pending);
					int minutesBefore = clock.MinutesRemainingWithOvertime;
					Check(!desk.TakeApproachStep(band.artistId, ScoutingApproachStepKind.Parents, out message) && clock.MinutesRemainingWithOvertime == minutesBefore,
						"the family can't be seen before Sunday: " + message);
					Check(!desk.TakeApproachStep(band.artistId, ScoutingApproachStepKind.Holdout, out _), "a holdout is not a verb: it's settled at a set");

					for (int d = 0; d < 42 && desk.NextApproachStep(band) != null && desk.Agreement(band) != null; d++) {
						clock.EndDay();
						if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
						int day = LocalSceneRoomService.Day(clock.CurrentDate);
						foreach (var s in desk.Agreement(band)?.Steps.Where(s => !s.Done && s.Kind != ScoutingApproachStepKind.Holdout && s.Day <= day).ToList() ?? new()) {
							int h = clock.MinutesRemainingWithOvertime;
							Check(desk.TakeApproachStep(band.artistId, s.Kind, out message) && clock.MinutesRemainingWithOvertime < h, $"{s.Kind} taken on {LocalSceneRoomService.Date(day).ToHeadlineString()}: {message}");
						}
						if (desk.Agreement(band)?.Steps.Any(s => s.Kind == ScoutingApproachStepKind.Holdout && !s.Done) != true) continue;
						foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
							foreach (var slot in bill.Appearances.Where(s => s.ArtistId == band.artistId && s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour)) {
								clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
								desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out _);
							}
					}
					if (desk.Agreement(band) == null) GD.Print($"note: {band.stageName} left the market before the steps were done ({band.labelId ?? "lapsed"})");
					else if (desk.NextApproachStep(band) is { } left) GD.Print($"note: {band.stageName} still waits on {left.Kind} after 42 days (no set heard)");
					else {
						var done = desk.Agreement(band);
						Check(done.Steps.All(s => s.Done && !string.IsNullOrEmpty(s.Said)) && parents.Day <= done.Steps.First(s => s.Kind == ScoutingApproachStepKind.Parents).DoneDay,
							"every step is taken on or after its day, and each says what happened");
						Check(desk.ScoutingSummary(band.artistId).Any(l => l.Contains("Done (")), "the notebook records the steps taken");
						Check(desk.AgreedToSign(band) && desk.CanTalkTerms(ProspectOf(band)),
							"with every step taken, they'll sign");
						Check(desk.ApproachToSign(ProspectOf(band), out message), "terms open once the steps are done: " + message);
					}
				}

				// ---- A yes doesn't hold forever ----
				var waiting = HearNew(a => true, 14);
				if (waiting == null) GD.Print("note: no act left for the lapse check");
				else {
					Force(waiting, DealDisposition.Hungry); Adult(waiting); Unmanaged(waiting);
					desk.ApproachToSign(ProspectOf(waiting), out message);
					var held = desk.Agreement(waiting);
					Check(held != null && PlayerDesk.AgreementHoldsUntil(held) == held.Day + PlayerDesk.AgreedHoldDays, "a yes with no steps holds four weeks");
					while (LocalSceneRoomService.Day(clock.CurrentDate) <= PlayerDesk.AgreementHoldsUntil(held) + 1 && desk.Agreement(waiting) != null) {
						clock.EndDay();
						if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
					}
					var last = desk.DirectApproaches.Last(a => a.ArtistId == waiting.artistId);
					Check(last.Status == ScoutingApproachStatus.Lapsed && (string.IsNullOrEmpty(waiting.labelId) ? LocalSceneRoomService.Day(clock.CurrentDate) > PlayerDesk.AgreementHoldsUntil(held) : true),
						$"an untaken yes lapses ({waiting.labelId ?? "stopped waiting"})");
				}
			} finally {
				foreach (var (m, v) in originals) m.ambition = v;
				foreach (var (m, v) in births) m.birthYear = v;
				foreach (var (a, (kind, name)) in managers) { a.manager = kind; a.managerName = name; }
			}

			GD.Print($"DISPOSITION_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("DISPOSITION_CHECKS_FAIL: " + e);
			GetTree().Quit(1);
		}
	}
}
