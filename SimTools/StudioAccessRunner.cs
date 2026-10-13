using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Studio slice 2 completion and breakdowns: rooms in other towns and the drive to them, specialist rooms reached
/// by an introduction, asking for an engineer by name, named session players, passes that fall apart at their weak part,
/// and the producer's call-in suggestion. Run: SimTools/StudioAccessRunner.tscn -- --seed=N [--studio-ui-capture]</summary>
public partial class StudioAccessRunner : Node {
    public override async void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        IEnumerable<Node> Walk(Node node) { yield return node; foreach (var c in node.GetChildren()) foreach (var n in Walk(c)) yield return n; }
        async System.Threading.Tasks.Task Screenshot(string name) {
            if (DisplayServer.GetName() == "headless" || !OS.GetCmdlineUserArgs().Contains("--studio-ui-capture")) return;
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/studio-access-" + name + ".png"));
        }
        string slot = "studio-access-" + Guid.NewGuid().ToString("N");
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Access Studio", "atlanta", out string message), "Found: " + message);
            desk.Label.cashReserves = 20000;
            var startDate = clock.CurrentDate;
            void Morning(GameDate date) => clock.RestoreClock(date, 9, 0);
            var homeLabels = new Dictionary<string, string>();
            void Sign(SimulatedArtist a) {
                homeLabels[a.artistId] = a.labelId;
                ChartManager.Instance.GetLabelById(a.labelId)?.roster.Remove(a);
                a.labelId = desk.Label.labelId; a.artistCreativeControl = false;
                desk.Label.roster.Add(a);
            }
            void Unsign(SimulatedArtist a) {
                desk.Label.roster.Remove(a);
                a.labelId = homeLabels[a.artistId];
                ChartManager.Instance.GetLabelById(a.labelId)?.roster.Add(a);
            }
            bool Free(SimulatedArtist a, GameDate d) => LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), d, 540, 1260);
            var bandPool = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId != null && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
                a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) > 2 && a.members.Any(m => m.isActive && m.primaryRole == MusicianRole.Drums) &&
                !a.instrumentalPerformance && Free(a, startDate)).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
            var artist = bandPool.First(); Sign(artist);
            var one = new List<PlayerDesk.MaterialChoice> { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Access Side", Detail = "their own" } };

            // ---- Rooms: the town's own, then rooms a drive away. Pure and RNG-free. ----
            GD.Seed(901); uint next = GD.Randi(); GD.Seed(901);
            var ids = desk.StudioRoomIds();
            var offers = ids.Select(id => desk.StudioOffer(id, artist.artistId)).ToList();
            Check(GD.Randi() == next && Json(offers) == Json(desk.StudioRoomIds().Select(id => desk.StudioOffer(id, artist.artistId)).ToList()),
                "Listing rooms is stable and draws no gameplay randomness");
            var local = offers.Where(o => o.Local).ToList(); var away = offers.Where(o => !o.Local).ToList();
            Check(local.Count == StudioRoomCatalog.Available(desk.Label.homeCityId, desk.Label.homeRegion).Length && local.All(o => o.Room.CityId == "atlanta"),
                "Every local room is offered: " + string.Join(", ", local.Select(o => o.Room.Name)));
            Check(away.Count is > 0 and <= PlayerDesk.MaxOutOfTownRooms && away.All(o => o.DriveHours is > 0 and <= PlayerDesk.MaxStudioDriveHours && o.Gas > 0) &&
                away.All(o => PlayerDesk.StudioQualityIn(o.Room.CityId, DistanceModel.GetCityById(o.Room.CityId).parentRegionId) > PlayerDesk.StudioQualityIn("atlanta", desk.Label.homeRegion)),
                "Out-of-town rooms are a day's drive away and in better recording towns: " + string.Join(", ", away.Select(o => $"{o.Room.Name} ({o.Room.CityId}, {o.DriveHours}h, ${o.Gas})")));
            var special = away.FirstOrDefault(o => o.Room.Specialist);
            Check(special != null && special.Room.HourlyRate > away.Where(o => !o.Room.Specialist).Select(o => o.Room.HourlyRate).DefaultIfEmpty(0).Max() - 1 &&
                !string.IsNullOrEmpty(special.Room.Specialty) && special.Room.CapabilityVersion == 4, $"A strong recording town offers a specialist room: {special?.Room.Name}, {special?.Room.Specialty}");
            Check(PlayerDesk.StudioFinishes(special.Room).Contains(StudioFinish.ChamberEcho) == special.Room.SupportsChamber &&
                offers.Where(o => !o.Room.Specialist).All(o => !PlayerDesk.StudioFinishes(o.Room).Contains(StudioFinish.ChamberEcho)),
                "Only a room with an echo chamber offers the chamber finish");
            Check(StudioRoomCatalog.ById("studio:atlanta:2", out _) == null && StudioRoomCatalog.ById("studio:nowhere:0", out _) == null &&
                !desk.StartSession(artist, one, PlayerDesk.StudioTier.Budget, 3, out _, overrideRefusal: true, roomId: "studio:los_angeles:2"),
                "A room the town doesn't have, or one out of reach, can't be booked");

            // ---- Specialist access: the door opens through an introduction. ----
            float cash0 = desk.Label.cashReserves;
            string unlockedBy;
            if (special.Locked != null) {
                Check(!desk.StartSession(artist, one, PlayerDesk.StudioTier.Top, 3, out message, overrideRefusal: true, roomId: special.Room.Id) &&
                    desk.Session == null && desk.Label.cashReserves == cash0 && desk.AtHome, "A specialist room without an introduction refuses the booking: " + message);
                var reader = SessionEmploymentService.ListeningCandidates(special.Room.CityId, LocalSceneRoomService.Day(clock.CurrentDate))
                    .FirstOrDefault(m => artist.members.All(x => x.personId != m.personId));
                Check(reader != null, "The specialist town has session players to know");
                var member = artist.members.First(m => m.isActive);
                ContactNetworkService.Link(reader.personId, member.personId, ContactKind.Session, clock.CurrentDate.year, 2, via: desk.Label.labelId);
                special = desk.StudioOffer(special.Room.Id, artist.artistId);
                Check(special.Locked == null && special.Introduction.Contains(reader.FullName) && special.Introduction.Contains(member.firstName),
                    "A session player there who knows a member opens the door: " + special.Introduction);
                unlockedBy = "contact";
            } else unlockedBy = "already introduced: " + special.Introduction;
            GD.Print($"STUDIO_ACCESS specialist={special.Room.Name} city={special.Room.CityId} kind={special.Room.Specialty} unlocked={unlockedBy}");

            // ---- Asking for an engineer by name. ----
            StudioRoomOffer Find(Func<StudioRoomOffer, bool> wanted, out GameDate on) {
                on = startDate;
                for (int d = 0; d < 30; d++) {
                    on = startDate.AddDays(d); Morning(on);
                    var hit = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, artist.artistId)).FirstOrDefault(o => o.Local && wanted(o));
                    if (hit != null) return hit;
                }
                return null;
            }
            var busyChief = Find(o => o.Engineers.Any(e => e.Chief && !e.Away && !e.OnByDefault), out var busyDay);
            Check(busyChief != null && !busyChief.Room.EngineerHouse && busyChief.Room.EngineerPremium == 0, "Some days the room puts its second engineer on a new label's date");
            var chief = busyChief.Engineers.Single(e => e.Chief);
            var asked = desk.StudioOffer(busyChief.Room.Id, artist.artistId, chief.Id);
            Check(asked.Room.EngineerId == chief.Id && asked.Room.EngineerRequested && asked.Room.EngineerPremium == StudioPeople.ChiefPremium(asked.Room) &&
                asked.Room.EngineerEar == chief.Ear && PlayerDesk.StudioRoomCost(asked.Room, 3) == (asked.Room.HourlyRate + asked.Room.EngineerPremium) * 3,
                $"Asking for the chief by name puts them on the board at ${asked.Room.EngineerPremium}/h more");
            float before = desk.Label.cashReserves;
            Check(desk.StartSession(artist, one, busyChief.Tier, 3, out message, overrideRefusal: true, roomId: asked.Room.Id, engineerId: chief.Id), "Book the chief: " + message);
            Check(desk.Session.Room.EngineerId == chief.Id && Math.Abs(before - desk.Label.cashReserves - PlayerDesk.StudioRoomCost(asked.Room, 3)) < .01f &&
                desk.Session.ActionLog.Any(l => l.Contains("asked for by name")), "The booked date has the chief, charges the premium once and logs the request");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && desk.Session.Room.EngineerPremium == asked.Room.EngineerPremium &&
                desk.Session.Room.EngineerRequested && desk.Session.BreakdownVersion == 1, "A v4 room, its request and the breakdown flag survive save/load: " + message);
            artist = ArtistManager.Instance.GetArtist(artist.artistId);
            var badPremium = PendingSessionSaveData.From(desk.Session); badPremium.Flow.Room.EngineerPremium = -4;
            bool rejected = false; try { badPremium.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "A saved room with an impossible premium is rejected");
            desk.ScrapSession();
            var awayChief = Find(o => o.Engineers.Any(e => e.Chief && e.Away), out var awayDay);
            Check(awayChief != null, "Some days the chief is cutting elsewhere");
            var absent = awayChief.Engineers.Single(e => e.Chief);
            cash0 = desk.Label.cashReserves;
            Check(desk.StudioOffer(awayChief.Room.Id, artist.artistId, absent.Id).Room.EngineerId != absent.Id &&
                !desk.StartSession(artist, one, awayChief.Tier, 3, out message, overrideRefusal: true, roomId: awayChief.Room.Id, engineerId: absent.Id) && desk.Session == null && desk.Label.cashReserves == cash0,
                "An engineer who is away can't be booked by name: " + message);

            // ---- Travel: the drive is the road owner's, and the player is in that town afterwards. ----
            Morning(startDate);
            special = desk.StudioOffer(special.Room.Id, artist.artistId);
            before = desk.Label.cashReserves;
            Check(desk.StartSession(artist, one, special.Tier, 3, out message, overrideRefusal: true, roomId: special.Room.Id), "Book out of town: " + message);
            var trip = desk.Session;
            Check(desk.CurrentCityId == special.Room.CityId && !desk.AtHome && trip.StartMinute == 540 + special.DriveHours * 60 &&
                clock.CurrentHour * 60 + clock.CurrentMinute == trip.StartMinute + trip.Room.SetupMinutes,
                $"The drive comes out of the day before the date starts ({special.DriveHours}h), and the player is in {special.Room.CityId}");
            Check(Math.Abs(before - desk.Label.cashReserves - (PlayerDesk.StudioRoomCost(trip.Room, 3) + special.Gas)) < .01f && trip.Cost == PlayerDesk.StudioRoomCost(trip.Room, 3) &&
                trip.ActionLog[0].StartsWith("Drove "), "Room and gas are each charged once; gas is not studio cost");
            Check(!desk.DriveHome(out message) && desk.CurrentCityId == special.Room.CityId, "Driving off mid-date is refused: " + message);
            Check(desk.CutStudioTake(0, out message), "Work at the out-of-town room: " + message);
            desk.ScrapSession();
            Check(desk.CurrentCityId == special.Room.CityId && desk.Session == null, "Abandoning the date leaves the player where the room is");
            Morning(startDate.AddDays(1));
            Check(desk.DriveHome(out message) && desk.AtHome, "The drive home is the ordinary road trip: " + message);
            clock.RestoreClock(startDate.AddDays(1), 17, 0);
            cash0 = desk.Label.cashReserves;
            Check(!desk.StartSession(artist, one, special.Tier, 3, out message, overrideRefusal: true, roomId: special.Room.Id) && desk.AtHome && desk.Label.cashReserves == cash0 &&
                clock.CurrentHour == 17, "A late drive that can't fit the date is refused before anything is spent: " + message);

            // ---- Breakdown chance: a sound pass or a tight band never stops. ----
            Check(PlayerDesk.StudioBreakdownChance(.44f, .2f, 1, 1) == 0 && PlayerDesk.StudioBreakdownChance(.8f, PlayerDesk.StudioTightBand, 1, 1) == 0 &&
                PlayerDesk.StudioBreakdownChance(.7f, .3f, 0, 0) > PlayerDesk.StudioBreakdownChance(.6f, .3f, 0, 0) &&
                PlayerDesk.StudioBreakdownChance(.6f, .3f, 0, 0) > PlayerDesk.StudioBreakdownChance(.6f, .5f, 0, 0) &&
                PlayerDesk.StudioBreakdownChance(.6f, .4f, .8f, .8f) > PlayerDesk.StudioBreakdownChance(.6f, .4f, 0, 0) &&
                PlayerDesk.StudioBreakdownChance(1, 0, 1, 1) <= .75f, "The breakdown chance needs a weak spot and a loose player, rises with tension and fatigue, and is bounded");

            // ---- Breakdowns across real bands: three passes each on a fresh date. ----
            Unsign(artist);
            var sample = new List<(SimulatedArtist Act, int Passes, int Broke, int Flubs)>();
            var baseline = desk.CaptureState();
            foreach (var act in bandPool.Take(60).Select(a => ArtistManager.Instance.GetArtist(a.artistId)).ToList()) {
                Morning(startDate.AddDays(2));
                if (!Free(act, clock.CurrentDate)) continue;
                Sign(act);
                if (desk.StartSession(act, one, PlayerDesk.StudioTier.Budget, 3, out _, overrideRefusal: true)) {
                    for (int i = 0; i < 3; i++) if (!desk.CutStudioTake(0, out string why) && sample.Count < 3) GD.Print($"STUDIO_ACCESS cut refused: {why} same={ReferenceEquals(ArtistManager.Instance.GetArtist(act.artistId), act)} label={act.labelId} desk={desk.Label.labelId} lineup={string.Join(",", desk.Session.ParticipantIds)} members={string.Join(",", act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId))}");
                    var takes = desk.Session.Cuts[0].Takes;
                    sample.Add((act, takes.Count, takes.Count(t => t.Incomplete && !t.FalseStart), takes.Count(t => t.FalseStart)));
                    Check(takes.Where(t => t.FalseStart).All(t => t.FaultPersonId == null && t.Minutes < PlayerDesk.StudioPassMinutes(desk.Session.Cuts[0]) && desk.StudioCallIn() == null),
                        "A false start costs a minute or two, blames nobody's part and never brings a call-in");
                }
                Check(desk.RestoreState(baseline, out message), "Reset after sampling: " + message);
                Unsign(act);
            }
            var ordered = sample.OrderBy(s => s.Act.studioPerformance).ToList();
            int third = ordered.Count / 3;
            double Rate(IEnumerable<(SimulatedArtist Act, int Passes, int Broke, int Flubs)> rows) => rows.Sum(r => r.Broke) / (double)Math.Max(1, rows.Sum(r => r.Passes));
            double FlubRate(IEnumerable<(SimulatedArtist Act, int Passes, int Broke, int Flubs)> rows) => rows.Sum(r => r.Flubs) / (double)Math.Max(1, rows.Sum(r => r.Passes));
            var loose = ordered.Take(third).ToList(); var tight = ordered.Skip(ordered.Count - third).ToList();
            GD.Print($"STUDIO_ACCESS breakdowns sampled={sample.Count} overall={Rate(sample):P1} loose={Rate(loose):P1} (perf<={loose.Last().Act.studioPerformance:F2}) " +
                $"middle={Rate(ordered.Skip(third).Take(ordered.Count - 2 * third)):P1} tight={Rate(tight):P1} (perf>={tight.First().Act.studioPerformance:F2}) " +
                $"flubs overall={FlubRate(sample):P1} loose={FlubRate(loose):P1} tight={FlubRate(tight):P1}");
            Check(PlayerDesk.StudioFlubChance(0) > 0 && PlayerDesk.StudioFlubChance(1) > PlayerDesk.StudioFlubChance(0) && sample.Sum(s => s.Flubs) > 0 && FlubRate(sample) < .12,
                "Even the best bands flub now and then, more when tired; flubs stay occasional");
            Check(sample.Count >= 30 && Rate(tight) <= .05 && Rate(loose) > Rate(tight) && Rate(sample) < .35,
                "Tight bands almost never break down; loose bands break down more, without a disaster deck");

            // ---- A loose band's date: breakdowns, the call-in and the credits. ----
            var rough = ordered.Where(s => s.Broke > 0).Select(s => s.Act).FirstOrDefault();
            Check(rough != null, "Some band in the sample breaks down");
            Morning(startDate.AddDays(2)); Sign(rough);
            var two = new List<PlayerDesk.MaterialChoice> { one[0], new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Rough Flip", Detail = "their own" } };
            Check(desk.StartSession(rough, two, PlayerDesk.StudioTier.Budget, 8, out message, overrideRefusal: true), "Book the rough band: " + message);
            var date = desk.Session;
            PlayerDesk.SessionTake broken = null;
            for (int i = 0; i < 12 && broken == null; i++) {
                int was = clock.CurrentHour * 60 + clock.CurrentMinute;
                var snapshot = desk.CaptureState();
                Check(desk.CutStudioTake(0, out message), "Cut: " + message);
                var take = desk.Session.Cuts[0].Takes[^1];
                if (!take.Incomplete || take.FalseStart) continue;
                broken = take;
                Check(take.Minutes < PlayerDesk.StudioPassMinutes(desk.Session.Cuts[0]) && clock.CurrentHour * 60 + clock.CurrentMinute == was + take.Minutes &&
                    take.TroubleSection >= 0 && take.FaultPersonId != null && take.ProducerDetail != null && message.Contains("broke down"),
                    $"A pass that falls apart costs the time to its weak part plus a reset ({take.Minutes}m of {PlayerDesk.StudioPassMinutes(desk.Session.Cuts[0])}m): {take.ProducerDetail}");
                string brokeJson = Json(PendingSessionSaveData.From(desk.Session));
                Check(desk.RestoreState(snapshot, out message), "Rewind: " + message); clock.RestoreClock(date.Date, was / 60, was % 60);
                Check(desk.CutStudioTake(0, out _) && Json(PendingSessionSaveData.From(desk.Session)) == brokeJson, "The same saved action breaks down the same way");
            }
            Check(broken != null, "The rough band breaks down within twelve passes");
            var cut = desk.Session.Cuts[0];
            int brokenIndex = cut.Takes.FindIndex(t => t.Incomplete && !t.FalseStart);
            desk.KeepTake(0, brokenIndex);
            Check(cut.KeptTake != brokenIndex && (StudioTakeEvidence.Choose(cut.Takes) < 0 || !cut.Takes[StudioTakeEvidence.Choose(cut.Takes)].Incomplete),
                "An incomplete take can't be kept by the player or the producer");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && desk.Session.Cuts[0].Takes[brokenIndex].Incomplete &&
                desk.Session.Cuts[0].Takes[brokenIndex].FaultPersonId == broken.FaultPersonId, "Breakdowns survive save/load: " + message);
            rough = ArtistManager.Instance.GetArtist(rough.artistId);
            var tampered = PendingSessionSaveData.From(desk.Session); tampered.Cuts[0].KeptTake = brokenIndex;
            rejected = false; try { tampered.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "A save that keeps an incomplete take is rejected");
            var older = PendingSessionSaveData.From(desk.Session); older.Flow.BreakdownVersion = 0;
            rejected = false; try { older.ToSession(Array.Empty<PlayerDesk.Song>()); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "A pre-breakdown date can't carry an incomplete take");
            var legacy = desk.CaptureState();
            desk.Session.BreakdownVersion = 0;
            for (int i = 0; i < 6; i++) desk.CutStudioTake(1, out _);
            Check(desk.Session.Cuts[1].Takes.Count == 6 && desk.Session.Cuts[1].Takes.All(t => !t.Incomplete), "Dates begun before breakdowns existed never break down or flub");
            Check(desk.RestoreState(legacy, out message), "Restore: " + message); clock.RestoreClock(date.Date, (date.StartMinute + desk.Session.ElapsedMinutes) / 60, (date.StartMinute + desk.Session.ElapsedMinutes) % 60);

            // Keep cutting until one member's part has fallen apart twice.
            StudioCallInProposal proposal = null;
            for (int i = 0; i < 16 && (proposal = desk.StudioCallIn()) == null; i++) if (!desk.CutStudioTake(i % 2, out _)) break;
            var faults = desk.Session.Cuts.SelectMany(c => c.Takes).Where(t => t.Incomplete).GroupBy(t => t.FaultPersonId).Select(g => $"{g.Key}x{g.Count()}");
            GD.Print($"STUDIO_ACCESS rough={rough.stageName} perf={rough.studioPerformance:F2} passes={desk.Session.Cuts.Sum(c => c.Takes.Count)} faults={string.Join(",", faults)} proposal={proposal?.CandidateName ?? "none"}");
            if (proposal != null) {
                var member = rough.members.First(m => m.personId == proposal.MemberId);
                Check(proposal.Breakdowns >= PlayerDesk.StudioCallInAfterBreakdowns && member.primaryRole == proposal.Role && proposal.Fee == PlayerDesk.SessionPlayerFee(proposal.Reading) &&
                    proposal.Text.Contains(proposal.CandidateName) && proposal.Text.Contains(member.firstName), "The producer names the member, the player, the fee and the cost to the band: " + proposal.Text);
                var choose = desk.CaptureState(); var chooseClock = (clock.CurrentHour, clock.CurrentMinute);
                Check(desk.DeclineCallIn(proposal.MemberId, out _) && desk.StudioCallIn()?.MemberId != proposal.MemberId, "Keeping the member on the part ends the suggestion for them");
                Check(desk.RestoreState(choose, out message), "Restore: " + message); clock.RestoreClock(date.Date, chooseClock.CurrentHour, chooseClock.CurrentMinute);
                before = desk.Label.cashReserves; float morale = rough.morale, paid = desk.Session.Cost;
                int sessions = SessionEmploymentService.Sessions(proposal.CandidateId, date.Date.year);
                int waited = clock.CurrentHour * 60 + clock.CurrentMinute;
                Check(desk.CallInSessionPlayer(proposal.MemberId, proposal.CandidateId, out message), "Call in: " + message);
                var hire = desk.Session.Hires.Single();
                Check(Math.Abs(before - desk.Label.cashReserves - proposal.Fee) < .01f && desk.Session.Cost == paid + proposal.Fee && rough.morale < morale &&
                    clock.CurrentHour * 60 + clock.CurrentMinute == waited + proposal.ArrivalMinutes && hire.CalledIn && hire.ReplacesPersonId == member.personId,
                    "The call-in pays the fee once, spends the wait and puts the member out");
                Check(SessionEmploymentService.Sessions(proposal.CandidateId, date.Date.year) == sessions + 1 &&
                    !LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, new[] { proposal.CandidateId }, date.Date, waited, waited + 60),
                    "The session player is paid through the session-work owner and reserved for the rest of the date");
                Check(desk.StudioCallIn()?.MemberId != member.personId, "A replaced member is not suggested again");
                int side = desk.Session.Cuts[0].KeptTake >= 0 ? 1 : 0;
                bool kept = false;
                for (int i = 0; i < 8 && !kept; i++) {
                    if (!desk.CutStudioTake(side, out message)) break;
                    var take = desk.Session.Cuts[side].Takes[^1];
                    Check(take.SessionPersonIds?.Contains(hire.PersonId) == true, "Every pass after the call-in has the session player on it");
                    if (!take.Incomplete) { desk.KeepTake(side, desk.Session.Cuts[side].Takes.Count - 1); kept = true; }
                }
                Check(kept, "A complete pass with the session player is cut");
                Check(desk.Session.Cuts.All(c => c.Takes.SkipWhile(t => t.SessionPersonIds == null).All(t => t.SessionPersonIds != null)) &&
                    desk.Session.Cuts.Any(c => c.Takes.Any(t => t.SessionPersonIds == null)), "Takes cut before the call-in stay the band's own"); 
                Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && desk.Session.Hires.Single().PersonId == hire.PersonId,
                    "The hire survives save/load: " + message);
                rough = ArtistManager.Instance.GetArtist(rough.artistId);
                for (int s = 0; s < desk.Session.Cuts.Count; s++) {
                    var each = desk.Session.Cuts[s];
                    if (each.KeptTake >= 0) continue;
                    int pick = StudioTakeEvidence.Choose(each.Takes);
                    if (pick < 0) { desk.CutStudioTake(s, out _); pick = StudioTakeEvidence.Choose(each.Takes); }
                    if (pick >= 0) desk.KeepTake(s, pick);
                }
                if (desk.Session.Cuts.All(c => c.KeptTake >= 0)) {
                    int masters = desk.Masters.Count;
                    Check(desk.PrintSession(out message), "Print: " + message);
                    var withHire = desk.Masters.Skip(masters).FirstOrDefault(m => m.StudioHistory?.SessionPlayers.Count > 0);
                    Check(withHire != null && withHire.StudioHistory.SessionPlayers.Single().Contains(hire.Name) && withHire.StudioHistory.SessionPlayers.Single().Contains(member.firstName),
                        "The master credits the session player who played on its kept take: " + withHire.StudioHistory.SessionPlayers.Single());
                } else desk.ScrapSession();
            } else {
                GD.Print("STUDIO_ACCESS no call-in: nobody local plays the part, or no member broke down twice");
                desk.ScrapSession();
            }
            Unsign(rough);

            // ---- Delegation works through breakdowns without hiring anyone. ----
            Morning(startDate.AddDays(3)); Sign(rough);
            before = desk.Label.cashReserves;
            Check(desk.StartSession(rough, two, PlayerDesk.StudioTier.Budget, 8, out message, overrideRefusal: true), "Book for delegation: " + message);
            float booked = before - desk.Label.cashReserves;
            bool ran = desk.RunStudioDate(out message);
            Check(desk.Session.Hires.Count == 0 && Math.Abs(before - desk.Label.cashReserves - booked) < .01f &&
                desk.Session.Cuts.All(c => c.KeptTake < 0 || !c.Takes[c.KeptTake].Incomplete) && (ran || message.Contains("safe")),
                $"Delegation keeps only complete passes and never hires: {message} ({desk.Session.Cuts.Sum(c => c.Takes.Count(t => t.Incomplete))} breakdowns)");
            desk.ScrapSession(); Unsign(rough);

            // ---- A Band Room booking names a real local player when one plays the part. ----
            Morning(startDate.AddDays(4)); Sign(artist);
            var drummer = artist.members.First(m => m.isActive && m.primaryRole == MusicianRole.Drums);
            Check(desk.BookSessionPlayer(artist, drummer, out message), "Band Room booking: " + message);
            var booking = desk.SessionPlayerFor(artist.artistId);
            var localDrummers = SessionEmploymentService.ListeningCandidates("atlanta", LocalSceneRoomService.Day(clock.CurrentDate))
                .Where(m => m.primaryRole == MusicianRole.Drums || m.secondaryRole == MusicianRole.Drums || m.primaryRole == MusicianRole.MultiInstrumentalist).ToList();
            Check(localDrummers.Count == 0 ? booking.PersonId == null : localDrummers.Any(m => m.personId == booking.PersonId && m.FullName == booking.Name),
                $"The booked session player is a named local drummer when there is one ({booking.Name}; {localDrummers.Count} in town)");
            Check(desk.StartSession(artist, one, PlayerDesk.StudioTier.Budget, 3, out message, overrideRefusal: true), "Book with the session player: " + message);
            Check(desk.Session.Hires.Count == 1 && desk.Session.Hires[0].Name == booking.Name && !desk.Session.Hires[0].CalledIn && desk.SessionPlayerFor(artist.artistId) == null,
                "A Band Room booking arrives on the date as a named hire");

            // ---- The booking sheet: one card per room, the chosen one opened up. ----
            desk.ScrapSession();
            Morning(startDate.AddDays(4));
            var withSongs = desk.CaptureState();
            foreach (string title in new[] { "Card Song", "Other Card" }) withSongs.Songs.Add(SongSaveData.From(new PlayerDesk.Song {
                SongId = "studio-access:" + title, Title = title, ArtistId = artist.artistId, Hook = .5f, Originality = .5f, Danceability = .5f, Written = clock.CurrentDate }));
            Check(desk.RestoreState(withSongs, out message), "Material for the sheet: " + message);
            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>(); AddChild(main);
            main.OpenStudio(artist.artistId);
            var panel = main.GetNode<PlayerDeskPanel>("StudioWindow");
            Walk(panel).OfType<CheckBox>().First().ButtonPressed = true;
            var names = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, artist.artistId).Room.Name).ToList();
            List<PanelContainer> Cards() => Walk(panel).OfType<PanelContainer>().Where(p => p.MouseFilter == Control.MouseFilterEnum.Stop &&
                p.GetChildren().OfType<VBoxContainer>().Any() && Walk(p).OfType<Label>().Count(l => names.Contains(l.Text)) == 1).ToList();
            bool Opened(PanelContainer card) => Walk(card).OfType<Label>().Any(l => l.Text == "ENGINEER");
            var cards = Cards();
            Check(cards.Count == names.Count && cards.Count(Opened) == 1 && Walk(panel).OfType<Label>().Any(l => l.Text == "A DRIVE AWAY"),
                $"Each room is its own card, grouped in town and a drive away, with only the chosen one opened ({cards.Count} cards)");
            await Screenshot("booking-local");
            var awayCard = cards.First(c => Walk(c).OfType<Label>().Any(l => l.Text == special.Room.Name));
            awayCard.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left });
            cards = Cards();
            var openedNow = cards.Single(Opened);
            Check(Walk(openedNow).OfType<Label>().Any(l => l.Text == special.Room.Name) && Walk(openedNow).OfType<Label>().Any(l => l.Text == "TRAVEL"),
                "Clicking a room opens its card, with the drive spelled out");
            await Screenshot("booking-away");
            var askButton = Walk(openedNow).OfType<Button>().FirstOrDefault(b => b.Text.StartsWith("ASK FOR ", StringComparison.Ordinal));
            if (askButton != null) {
                askButton.EmitSignal(Button.SignalName.Pressed);
                Check(Walk(panel).OfType<Button>().Any(b => b.Text == "LET THE ROOM CHOOSE"), "Asking for the other engineer is reflected on the card");
            }
            GD.Print($"STUDIO_ACCESS_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_ACCESS_CHECK_FAIL " + ex); GetTree().Quit(1); }
        finally { SaveGameService.Delete(slot); }
    }
}
