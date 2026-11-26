using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Signing realism steps 2-4 (SimTools/SigningRealismBreakdown.md): the act's disposition replaces the keyed 65%
/// coin, a yes is signed tonight or after dated steps, and regard (what you've done with the act) moves the answer.
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
/// do terms open. A yes nobody acts on lapses after AgreedHoldDays, and (step 4) the act remembers it: it's let down, says
/// so, and wants you back at a set first.
/// Part F (step 4, regard) checks the shortfall rules, then builds regard on one heard act from real verbs: one night is
/// regard 1, reading it changes nothing, a comfortable act that hardly knows you says no and won't sit for a session, a
/// careful one off the market will, and a real demo at your place lifts regard to 3, which meets a careful request with no
/// second night. A second session and an attended sit-down bring it to 5: the comfortable act hears you out, stays
/// comfortable, and talks terms at the table rather than signing what it's handed.
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
					var who = ManagerIdentity.Read(band, day0);
					Check(manager.Day > day0 && manager.Who == who.Name && manager.Place == who.Place && message.Contains(manager.Manager) &&
						(who.Kind == ManagerKind.ClubBooker || !LocalSceneRoomService.Date(manager.Day.Value).IsWeekend),
						$"a shark wants a sit-down, named for who he is ({who.Kind}: {manager.Manager}, at {manager.Place})");
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
					// Step 4: they remember it. A hungry act you let down wants you back in the room before it says yes again.
					if (string.IsNullOrEmpty(waiting.labelId) && waiting.lifecycleStatus == ArtistLifecycleStatus.Active && waiting.prospectMarketStatus == ProspectMarketStatus.Latent) {
						var grudge = desk.ReadRegard(waiting);
						Check(last.LetLapse && grudge.Has(RegardSourceKind.LetTheirYesLapse) && grudge.LetDown,
							$"a yes you let lapse is remembered against you (regard {grudge.Score})");
						Check(!desk.ApproachToSign(ProspectOf(waiting), out message) && desk.DirectApproaches.Last().Status == ScoutingApproachStatus.WantsAnotherLook &&
							message.Contains("never heard from you") && desk.WaitingToBeHeardAgain(waiting), "they say so, and want you back at a set first: " + message);
						Check(desk.RegardSummary(waiting.artistId).Contains("let their yes lapse"), "the notebook says what you did");
					} else GD.Print("note: the lapsed act left the market; the let-down ask wasn't exercised");
				}

				// ---- Step 4, Part F: regard, built from real sessions ----
				{
					var careRead = new DispositionRead { Kind = DealDisposition.Careful, Reason = DispositionReason.OutOfTown };
					var burned = new DispositionRead { Kind = DealDisposition.Careful, Reason = DispositionReason.DroppedBefore };
					Check(PlayerDesk.RegardShortfall(careRead, new RegardRead { Score = 1, Level = RegardLevel.KnowYourFace }) == 1 &&
						PlayerDesk.RegardShortfall(careRead, new RegardRead { Score = 2, Level = RegardLevel.KnowYourFace }) == 0 &&
						PlayerDesk.RegardShortfall(burned, new RegardRead { Score = 2, Level = RegardLevel.KnowYourFace }) == 1,
						"a careful act wants two, one dropped before wants three");
					Check(PlayerDesk.RegardShortfall(new DispositionRead { Kind = DealDisposition.Hungry }, new RegardRead { Score = -1, Level = RegardLevel.LetDown }) == 1 &&
						PlayerDesk.RegardShortfall(new DispositionRead { Kind = DealDisposition.Hungry }, new RegardRead { Score = 0 }) == 0,
						"a hungry act wants nothing more unless you let it down");
					Check(PlayerDesk.RegardOpener(new RegardRead()) == null, "a stranger opens with nothing");
				}
				bool Session(SimulatedArtist a, ScoutingAppointmentActivity activity, bool askNumber = false) {
					var q = desk.PreviewScoutingInvitation(a.artistId, activity);
					if (q == null) { GD.Print($"{activity} with {a.stageName}: no quote"); return false; }
					if (!desk.ArrangeScoutingMeeting(a.artistId, q.Id, out string arranged, activity)) { GD.Print($"{activity} with {a.stageName}: {arranged}"); return false; }
					if (askNumber) { desk.AskActForNumber(a.artistId, out string number); GD.Print("number: " + number); }
					var m = desk.ScoutingMeetings.LastOrDefault(x => x.ArtistId == a.artistId && x.Activity == activity && x.Status == SceneConversationStatus.Scheduled);
					if (m == null) { GD.Print($"{activity} with {a.stageName}: no scheduled meeting ({string.Join("; ", desk.ScoutingMeetings.Where(x => x.ArtistId == a.artistId).Select(x => $"{x.Activity} {x.Status}"))})"); return false; }
					desk.Label.cashReserves = Math.Max(desk.Label.cashReserves, 1000);
					clock.RestoreClock(LocalSceneRoomService.Date(m.Day), m.StartMinute / 60, m.StartMinute % 60);
					desk.AttendScoutingMeeting(m.Id, out string said);
					GD.Print($"{activity} with {a.stageName}: {said}");
					return desk.ScoutingMeetings.Any(x => x.Id == m.Id && x.Status == SceneConversationStatus.Completed);
				}
				SimulatedArtist friend = null;
				for (int tries = 0; tries < 8 && friend == null; tries++) {
					var a = HearNew(x => Members(x) == 1 || x.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active && m.isBandLeader), 14);
					if (a == null) break;
					if (desk.PreviewScoutingInvitation(a.artistId, ScoutingAppointmentActivity.Demo) != null) friend = a;
				}
				if (friend == null) GD.Print("note: no heard act would take a studio session; Part F not exercised");
				else {
					Force(friend, DealDisposition.Comfortable); Adult(friend); Unmanaged(friend);
					var r0 = desk.ReadRegard(friend);
					Check(r0.Score == 1 && r0.Level == RegardLevel.KnowYourFace && r0.Has(RegardSourceKind.HeardThem) &&
						desk.RegardSummary(friend.artistId).Contains("heard once"), $"{friend.stageName}: one night heard is regard 1, and the card says so");
					string world = System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions);
					desk.ReadRegard(friend); desk.RegardSummary(friend.artistId);
					Check(System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions) == world, "reading regard changes no world state");
					Check(!desk.ApproachToSign(ProspectOf(friend), out message) && desk.StillDeclining(friend) && message.Contains("trust you"),
						"a comfortable stranger says no, and says trust could earn a hearing: " + message);
					Check(!desk.WouldMeetOffMarket(friend), "a comfortable act that hardly knows you won't sit for a session");
					// A careful act off the market will (its own disposition, not the AI market's flag, answers).
					Force(friend, DealDisposition.Careful);
					Check(desk.WouldMeetOffMarket(friend) && PlayerDesk.RegardShortfall(desk.ReadDisposition(friend), desk.ReadRegard(friend)) == 1,
						"a careful act off the market agrees to a session, and still wants one more point");
					Check(Session(friend, ScoutingAppointmentActivity.Demo, askNumber: true), "a real studio session (a demo at your place) is arranged and attended");
					var r1 = desk.ReadRegard(friend);
					Check(r1.Score == 3 && r1.Has(RegardSourceKind.Session) && r1.Level == RegardLevel.Friendly, $"a first session adds two (regard {r1.Score})");
					Check(PlayerDesk.RegardShortfall(desk.ReadDisposition(friend), r1) == 0 && !desk.WaitingToBeHeardAgain(friend),
						"a careful act's request is met by a session, with no second night");
					Force(friend, DealDisposition.Comfortable);
					Check(desk.StillDeclining(friend) && desk.WouldMeetOffMarket(friend), "regard 3 doesn't move a comfortable act, but it lets you hear it work");
					// Two more points: a second session and a sit-down, by phone if you got the number.
					if (desk.ActNumber(friend.artistId) == null) GD.Print("note: no number, so no second invitation; the hearing path wasn't exercised");
					else {
						bool demo = Session(friend, ScoutingAppointmentActivity.Demo);
						bool talk = demo && Session(friend, ScoutingAppointmentActivity.Conversation);
						var r2 = desk.ReadRegard(friend);
						if (!talk) GD.Print($"note: second session {demo}, sit-down {talk}; regard {r2.Score}, the hearing path wasn't exercised");
						else {
							Check(r2.Score == PlayerDesk.HearingRegard && r2.Level == RegardLevel.TrustYou, $"a second session and a sit-down bring regard to {r2.Score}");
							if (desk.Agreement(friend) == null) desk.ApproachToSign(ProspectOf(friend), out message);
							var hearing = desk.Agreement(friend);
							Check(hearing != null && hearing.Hearing && hearing.Said.Contains("hear you out") && desk.ReadDisposition(friend).Kind == DealDisposition.Comfortable,
								"a comfortable act that trusts you hears you out, and stays comfortable: " + hearing?.Said);
							// A lukewarm band names a holdout; go and hear the set where they settle it.
							for (int d = 0; d < 28 && desk.Agreement(friend) != null && !desk.AgreedToSign(friend); d++) {
								clock.EndDay();
								if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
								int day = LocalSceneRoomService.Day(clock.CurrentDate);
								foreach (var s in desk.Agreement(friend)?.Steps.Where(s => !s.Done && s.Kind != ScoutingApproachStepKind.Holdout && s.Day <= day).ToList() ?? new())
									desk.TakeApproachStep(friend.artistId, s.Kind, out _);
								foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
									foreach (var slot in bill.Appearances.Where(s => s.ArtistId == friend.artistId && s.EndHour <= clock.HardStopHour && s.StartHour >= clock.CurrentHour)) {
										clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
										desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out _);
									}
								if (desk.Agreement(friend) != null) desk.ApproachToSign(ProspectOf(friend), out _);
							}
							if (desk.AgreedToSign(friend)) {
								var p = ProspectOf(friend);
								if (!p.HasBaseline) desk.ApproachToSign(p, out message);
								Check(p.HasBaseline && p.Posture != NegotiationPosture.Pushover, $"at the table it talks terms ({p.Posture})");
							} else GD.Print("note: the hearing has steps first; the table posture wasn't reached");
						}
					}
				}
			} finally {
				foreach (var (m, v) in originals) m.ambition = v;
				foreach (var (m, v) in births) m.birthYear = v;
				foreach (var (a, (kind, name)) in managers) { a.manager = kind; a.managerName = name; }
			}

			// ---- Part E: who the manager is (§10), read over every managed act in the world ----
			{
				int day = LocalSceneRoomService.Day(clock.CurrentDate), y = clock.CurrentDate.year;
				string before = System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions);
				var allowed = new Dictionary<ManagerKind, ManagerArchetype[]> {
					[ManagerKind.Family] = new[] { ManagerArchetype.LocalHustler, ManagerArchetype.Svengali },
					[ManagerKind.ClubBooker] = new[] { ManagerArchetype.LocalHustler, ManagerArchetype.Shark },
					[ManagerKind.Deejay] = new[] { ManagerArchetype.LocalHustler, ManagerArchetype.Shark },
					[ManagerKind.Songwriter] = new[] { ManagerArchetype.Svengali, ManagerArchetype.Visionary },
					[ManagerKind.Agency] = new[] { ManagerArchetype.Shark, ManagerArchetype.Visionary },
					[ManagerKind.JukeboxOperator] = new[] { ManagerArchetype.Shark },
					[ManagerKind.PersonalManager] = new[] { ManagerArchetype.Shark, ManagerArchetype.Svengali, ManagerArchetype.Visionary },
					[ManagerKind.Hustler] = new[] { ManagerArchetype.LocalHustler },
				};
				var counts = new Dictionary<ManagerKind, int>();
				int unmanaged = 0;
				foreach (var a in am.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && string.IsNullOrEmpty(a.labelId)).OrderBy(a => a.artistId, StringComparer.Ordinal)) {
					var r = ManagerIdentity.Read(a, day);
					if (a.manager == ManagerArchetype.None) { if (r != null) Check(false, $"{a.stageName}: unmanaged act has no manager"); unmanaged++; continue; }
					var again = ManagerIdentity.Read(a, day);
					if (r == null || again.Kind != r.Kind || again.Name != r.Name || again.Title != r.Title) Check(false, $"{a.stageName}: the same act reads the same manager");
					counts[r.Kind] = counts.GetValueOrDefault(r.Kind) + 1;
					if (!allowed[r.Kind].Contains(a.manager)) Check(false, $"{a.stageName}: {r.Kind} sits on {a.manager}");
					string town = LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId);
					var people = a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
					string bad = r.Kind switch {
						ManagerKind.Family when !people.Any(m => r.Name.EndsWith(" " + m.lastName?.Trim())) => "family manager shares no member's surname",
						ManagerKind.ClubBooker when SceneRoomCatalog.Get(LocalSceneRoomService.EngagementRoom(a.artistId, day))?.ContactName != r.Name => "club booker isn't the engagement room's booker",
						ManagerKind.Agency when a.contractSequence == 0 && a.careerState is CareerState.Unsigned or CareerState.NewSigning => "agency man for an act with no record career",
						ManagerKind.JukeboxOperator when town is not ("new_york" or "chicago" or "detroit" or "philadelphia") ||
							a.primaryGenre is not (Genre.RnB or Genre.DooWop or Genre.Blues or Genre.Soul or Genre.RockAndRoll) => "jukebox operator outside the jukebox cities or music",
						_ => string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Place) ? "missing name, title or place" : null
					};
					if (bad != null) Check(false, $"{a.stageName} ({a.manager}, {r.Kind}): {bad}");
				}
				GD.Print("manager identities (unsigned, active): unmanaged " + unmanaged + "; " +
					string.Join(", ", counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}")));
				Check(counts.Values.Sum() > 200, $"{counts.Values.Sum()} managed unsigned acts read");
				foreach (var k in new[] { ManagerKind.Family, ManagerKind.ClubBooker, ManagerKind.Deejay, ManagerKind.Songwriter, ManagerKind.PersonalManager, ManagerKind.Hustler })
					Check(counts.GetValueOrDefault(k) > 0, $"{k} managers occur");
				Check(System.Text.Json.JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions) == before,
					"reading managers changes no world state (archetypes and generated names untouched)");

				// A family manager who is also the minor's parent: one Sunday visit, not two steps.
				var familyAct = am.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && string.IsNullOrEmpty(a.labelId) &&
						a.manager == ManagerArchetype.Svengali && ManagerIdentity.Read(a, day)?.Kind == ManagerKind.Family)
					.Select(a => (a, r: ActDisposition.Read(a, label, day, null))).FirstOrDefault(x => x.r.Minors.Count > 0);
				if (familyAct.a == null) GD.Print("note: no Svengali family manager with a minor in this world; merge not exercised");
				else {
					var steps = desk.StepsFor(familyAct.r, familyAct.a, day);
					Check(steps.Count(s => s.Kind == ScoutingApproachStepKind.Manager) == 0 &&
						steps.Single(s => s.Kind == ScoutingApproachStepKind.Parents).Manager is { } intro && intro.StartsWith(ManagerIdentity.Read(familyAct.a, day).Name),
						$"{familyAct.a.stageName}: the family is the manager, so Sunday dinner is the sit-down");
					Check(PlayerDesk.StepAsk(steps[0], familyAct.a).Contains("that's the sit-down as well"), "the yes says so");
				}
			}

			GD.Print($"DISPOSITION_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("DISPOSITION_CHECKS_FAIL: " + e);
			GetTree().Quit(1);
		}
	}
}
