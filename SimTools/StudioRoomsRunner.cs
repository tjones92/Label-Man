using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

/// <summary>Studio slice 9: specialist owners' house players, room renown and rates that respond to the label's records,
/// overnight studio trips, and backing singers for a singer who needs them.
/// Run: SimTools/StudioRoomsRunner.tscn -- --seed=N [--studio-ui-capture]</summary>
public partial class StudioRoomsRunner : Node {
    public override async void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        IEnumerable<Node> Walk(Node node) { yield return node; foreach (var c in node.GetChildren()) foreach (var n in Walk(c)) yield return n; }
        async System.Threading.Tasks.Task Screenshot(string name) {
            if (DisplayServer.GetName() == "headless" || !OS.GetCmdlineUserArgs().Contains("--studio-ui-capture")) return;
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/studio-rooms-" + name + ".png"));
        }
        object Private(string method, params object[] args) =>
            typeof(PlayerDesk).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(PlayerDesk.Instance, args);
        bool Rejected(Action load) { try { load(); return false; } catch (InvalidOperationException) { return true; } }
        string slot = "studio-rooms-" + Guid.NewGuid().ToString("N");
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Rooms Studio", "atlanta", out string message), "Found: " + message);
            desk.Label.cashReserves = 50000;
            var startDate = clock.CurrentDate;
            void Morning(GameDate date) => clock.RestoreClock(date, 9, 0);
            var homeLabels = new Dictionary<string, string>();
            SimulatedArtist Live(SimulatedArtist a) => ArtistManager.Instance.GetArtist(a.artistId);
            void Sign(SimulatedArtist a, bool control = false) {
                a = Live(a);
                homeLabels[a.artistId] = a.labelId;
                ChartManager.Instance.GetLabelById(a.labelId)?.roster.Remove(a);
                a.labelId = desk.Label.labelId; a.artistCreativeControl = control;
                desk.Label.roster.Add(a);
            }
            void Unsign(SimulatedArtist a) {
                a = Live(a); desk.Label.roster.RemoveAll(x => x.artistId == a.artistId);
                desk.Label.roster.Remove(a);
                a.labelId = homeLabels[a.artistId];
                ChartManager.Instance.GetLabelById(a.labelId)?.roster.Add(a);
            }
            bool Free(SimulatedArtist a, GameDate d) => LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), d, 0, 1440) &&
                LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), d.NextDay(), 0, 1440);
            var signed = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && !a.instrumentalPerformance &&
                a.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active) && Free(a, startDate)).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
            var bands = signed.Where(a => !SessionEmploymentService.NeedsCrew(a) && a.members.Any(m => m.isActive && m.primaryRole == MusicianRole.Drums)).ToList();
            var loose = bands.Where(a => a.studioPerformance < PlayerDesk.StudioOwnPlayersAfterVisit).ToList();
            var tight = bands.FirstOrDefault(a => a.studioPerformance >= PlayerDesk.StudioTightBand);
            var singers = signed.Where(a => SessionEmploymentService.NeedsCrew(a) && PlayerDesk.StudioNeedsBacking(a)).ToList();
            var harmony = bands.FirstOrDefault(a => !PlayerDesk.StudioNeedsBacking(a));
            Check(loose.Count > 1 && tight != null && singers.Count > 0 && harmony != null,
                $"The world has loose bands ({loose.Count}), a tight band ({tight?.studioPerformance} of max {bands.Max(a => a.studioPerformance):F2}), solo singers ({singers.Count}) and a band with harmony singers ({harmony?.stageName}, of {bands.Count})");
            GD.Print($"STUDIO_ROOMS population bands={bands.Count} needing backing={bands.Count(PlayerDesk.StudioNeedsBacking)} singer acts={signed.Count(SessionEmploymentService.NeedsCrew)} " +
                $"of which needing backing={singers.Count}; loose={loose.Count} tight={bands.Count(a => a.studioPerformance >= PlayerDesk.StudioTightBand)}");
            var one = new List<PlayerDesk.MaterialChoice> { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Rooms Side", Detail = "their own" } };
            var two = new List<PlayerDesk.MaterialChoice> { one[0], new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Rooms Flip", Detail = "their own" } };
            int parts = Enum.GetValues<StudioSectionKind>().Length;

            // Opens a specialist room's door: a session player there who knows a member, or, in a town without any, an
            // earlier date the label cut there (a remembered visit by a fixture act).
            void Introduce(string roomId, SimulatedArtist act) {
                var r = StudioRoomCatalog.ById(roomId, out _);
                var reader = SessionEmploymentService.ListeningCandidates(r.CityId, LocalSceneRoomService.Day(clock.CurrentDate)).FirstOrDefault(m => act.members.All(x => x.personId != m.personId));
                if (reader != null) { ContactNetworkService.Link(reader.personId, act.members.First(m => m.isActive).personId, ContactKind.Session, startDate.year, 2, via: desk.Label.labelId); return; }
                var state = desk.CaptureState(); state.StudioVisits ??= new();
                state.StudioVisits.Add(new StudioVisit { SessionId = "intro:" + roomId, RoomId = roomId, RoomName = r.Name, CityId = r.CityId, EngineerId = roomId + "|house",
                    EngineerName = "Fixture", ArtistId = "intro-fixture", Date = startDate, FlaggedParts = new(new int[parts]) });
                Check(desk.RestoreState(state, out string why), "Introduction fixture: " + why);
            }

            // ================= Overnight trips =================
            // A tight band: the trip is the subject here, not an owner's terms.
            var band = tight; Sign(band);
            GD.Seed(77); uint next = GD.Randi(); GD.Seed(77);
            var offers = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).ToList();
            Check(GD.Randi() == next && Json(offers) == Json(desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).ToList()),
                "Listing rooms, house bands and renown is stable and draws no gameplay randomness");
            var overnight = offers.Where(o => o.Overnight).ToList();
            var homeCity = DistanceModel.GetCityById("atlanta");
            GD.Print("STUDIO_ROOMS reachable " + string.Join(", ", DistanceModel.GetCities().Where(c => c.cityId != "atlanta" &&
                (c.parentRegionId == homeCity.parentRegionId || DistanceModel.GetAdjacentRegions(homeCity.parentRegionId).Contains(c.parentRegionId)))
                .Select(c => (c, h: desk.DriveQuote("atlanta", c.cityId).Hours)).OrderBy(x => x.h)
                .Select(x => $"{x.c.cityId}:{x.h}h:q{PlayerDesk.StudioQualityIn(x.c.cityId, x.c.parentRegionId):F2}")) + $" home q{PlayerDesk.StudioQualityIn("atlanta", desk.Label.homeRegion):F2}");
            var voiceSpread = bands.Select(a => a.members.Where(m => m.isActive && !m.isLeadVocalist).Select(SessionEmploymentService.BackingVoice).DefaultIfEmpty(0).Max()).OrderBy(v => v).ToList();
            GD.Print($"STUDIO_ROOMS best non-lead voice per band: p25={voiceSpread[voiceSpread.Count / 4]:F2} p50={voiceSpread[voiceSpread.Count / 2]:F2} p75={voiceSpread[voiceSpread.Count * 3 / 4]:F2} p90={voiceSpread[voiceSpread.Count * 9 / 10]:F2}");
            var sameDay = offers.Where(o => !o.Local && !o.Overnight).ToList();
            Check(sameDay.All(o => o.DriveHours <= PlayerDesk.MaxStudioDriveHours) && overnight.Count is > 0 and <= PlayerDesk.MaxOvernightRooms &&
                overnight.All(o => o.DriveHours is > PlayerDesk.MaxStudioDriveHours and <= PlayerDesk.MaxOvernightDriveHours && o.StaffedFor == startDate.NextDay() && o.BandLodging > 0),
                "Rooms too far for a day are offered as overnight trips, staffed for tomorrow: " + string.Join(", ", overnight.Select(o => $"{o.Room.Name} ({o.DriveHours}h)")));
            var far = overnight.FirstOrDefault(o => o.Locked == null) ?? overnight[0];
            if (far.Locked != null && far.Room.Specialist && far.Introduction == null) {
                Introduce(far.Room.Id, band);
                far = desk.StudioOffer(far.Room.Id, band.artistId);
            }
            if (far.Locked != null) {
                // A house band out on another date, or a full calendar: drive up to a plain room in that town instead.
                GD.Print($"STUDIO_ROOMS overnight room locked ({far.Locked}); using a plain room");
            }
            GD.Print($"STUDIO_ROOMS overnight={far.Room.Name} city={far.Room.CityId} drive={far.DriveHours}h gas={far.Gas} lodging={far.BandLodging}");
            float cash0 = desk.Label.cashReserves;
            Check(!desk.StartSession(Live(band), one, far.Tier, 3, out message, overrideRefusal: true, roomId: far.Room.Id) && desk.Session == null &&
                desk.Label.cashReserves == cash0 && desk.AtHome, "An overnight room can't be booked for the same day: " + message);
            if (far.Locked == null) {
                Check(desk.DriveUpForStudio(Live(band), far.Room.Id, out message), "Drive up with the band: " + message);
                var trip = desk.ActiveStudioTrip;
                Check(trip != null && desk.CurrentCityId == far.Room.CityId && Math.Abs(cash0 - desk.Label.cashReserves - far.Gas) < .01f &&
                    clock.CurrentHour == 9 + far.DriveHours && trip.PersonIds.Count == band.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active),
                    $"The drive is the road owner's: {far.DriveHours}h and the gas, once; the trip holds the band");
                var there = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).ToList();
                Check(there.All(o => o.Local && o.Room.CityId == far.Room.CityId) && there.Any(o => o.Room.Id == far.Room.Id),
                    "Up on the trip, the booking sheet is the rooms in that town, with no drive");
                Check(desk.StudioTripHolds(trip.PersonIds[0], LocalSceneRoomService.Day(startDate.NextDay())) && !desk.StudioTripHolds(trip.PersonIds[0], LocalSceneRoomService.Day(startDate.AddDays(2))),
                    "The trip holds the band for its two days only");
                Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && Json(desk.ActiveStudioTrip) == Json(trip),
                    "The trip survives save/load: " + message);
                band = ArtistManager.Instance.GetArtist(band.artistId);
                var damaged = desk.CaptureState(); damaged.StudioTrip.Nights = -1;
                Check(!desk.RestoreState(damaged, out message), "A damaged trip is refused: " + message);
                // The night at the motel: the player's own room and the band's, billed at dawn.
                Morning(startDate.NextDay());
                float beforeNight = desk.Label.cashReserves;
                Private("ChargeHotelIfAway"); Private("ProcessStudioTripDawn", startDate.NextDay());
                Check(Math.Abs(beforeNight - desk.Label.cashReserves - PlayerDesk.HotelNightly - trip.LodgingPerNight) < .01f && desk.ActiveStudioTrip.Nights == 1 &&
                    trip.LodgingPerNight == PlayerDesk.BandLodgingPerNight(trip.PersonIds.Count), "A night away bills the player's motel and the band's rooms, two to a room");
                var room = desk.StudioOffer(far.Room.Id, band.artistId);
                float beforeDate = desk.Label.cashReserves;
                Check(room.Locked == null || room.Locked.Contains("booked solid") || room.Locked.Contains("out on another date"), "The room is open, or full today: " + room.Locked);
                if (room.Locked == null) {
                    Check(desk.StartSession(Live(band), one, room.Tier, 3, out message, overrideRefusal: true, roomId: room.Room.Id), "Book the room up there: " + message);
                    float hired = desk.Session.Hires.Sum(h => h.Fee);
                    Check(desk.Session.StartMinute == 540 && desk.ActiveStudioTrip == null && desk.Session.ActionLog[0].Contains("stayed 1 night") &&
                        Math.Abs(beforeDate - desk.Label.cashReserves - PlayerDesk.StudioRoomCost(desk.Session.Room, 3) - hired) < .01f,
                        "The date starts in the morning with no second drive; the trip is logged and done: " + desk.Session.ActionLog[0]);
                    desk.ScrapSession();
                }
                Morning(startDate.AddDays(2)); Check(desk.DriveHome(out message) && desk.AtHome && desk.ActiveStudioTrip == null, "The drive home is the ordinary one: " + message);
            }
            // A same-day room can also be driven up to the day before; a lapsed or abandoned trip sends the band home.
            Morning(startDate.AddDays(2));
            var near = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).First(o => !o.Local && !o.Overnight);
            if (near.Introduction == null && near.Room.Specialist) { Introduce(near.Room.Id, band); near = desk.StudioOffer(near.Room.Id, band.artistId); }
            Check(near.Locked == null, "A same-day room is open: " + near.Locked);
            Check(desk.DriveUpForStudio(Live(band), near.Room.Id, out message) && desk.ActiveStudioTrip != null, "Drive up to a same-day room a day early: " + message);
            float beforeLapse = desk.Label.cashReserves;
            Private("ProcessStudioTripDawn", startDate.AddDays(4));
            Check(desk.ActiveStudioTrip == null && desk.Label.cashReserves == beforeLapse, "A trip not used by the end of the next day lapses, with no night billed");
            Morning(startDate.AddDays(4)); Check(desk.DriveHome(out _) && desk.AtHome, "Home again");
            Morning(startDate.AddDays(5));
            Check(desk.DriveUpForStudio(Live(band), near.Room.Id, out message), "Another trip: " + message);
            var elsewhere = desk.ReachableCities().Where(c => c.cityId != desk.Label.homeCityId).OrderBy(c => desk.DriveQuote(desk.CurrentCityId, c.cityId).Hours).ThenBy(c => c.cityId, StringComparer.Ordinal).First();
            clock.RestoreClock(startDate.AddDays(6), 9, 0);
            Check(desk.DriveTo(elsewhere.cityId, out message) && desk.ActiveStudioTrip == null, "Driving off before the date sends the band home: " + message);
            Morning(startDate.AddDays(7)); Check(desk.DriveHome(out _) || desk.AtHome, "Home again");
            Unsign(band);

            // ================= House players =================
            Morning(startDate.AddDays(8));
            StudioRoomOffer Specialist(SimulatedArtist act) => desk.StudioRoomIds().Select(id => desk.StudioOffer(id, act.artistId)).FirstOrDefault(o => o.Room.Specialist && !o.Overnight);
            band = bands.FirstOrDefault(a => a.studioPerformance >= PlayerDesk.StudioOwnPlayersAfterVisit && a.studioPerformance < PlayerDesk.StudioTightBand && Free(a, clock.CurrentDate))
                ?? loose.First(a => Free(a, clock.CurrentDate)); Sign(band);
            var special = Specialist(band);
            Check(special != null, "A specialist room is within a day's drive");
            if (special.Introduction == null) { Introduce(special.Room.Id, band); special = Specialist(band); }
            var house = special.House;
            var drummer = band.members.First(m => m.isActive && m.primaryRole == MusicianRole.Drums);
            Check(house != null && house.Insists && house.Locked == null && house.Players.Any(p => p.Role == MusicianRole.Drums && p.ReplacesPersonId == drummer.personId) &&
                house.Players.All(p => p.Fee == PlayerDesk.SessionPlayerFee(p.Reading) && band.members.All(m => m.personId != p.PersonId)) && house.Reason.Contains(band.stageName),
                $"The owner insists on the house rhythm section for a loose band he doesn't know ({band.studioPerformance:F2}): {house?.Reason} " +
                string.Join(", ", house?.Players.Select(p => $"{p.Name} {p.Role}") ?? new string[0]));
            var houseAgain = desk.StudioHouseBand(special.Room, Live(band), clock.CurrentDate.AddDays(1), 600, 1200);
            GD.Print($"STUDIO_ROOMS house={special.Room.Name} today={string.Join(",", house.Players.Select(p => p.Name))} tomorrow={string.Join(",", houseAgain?.Players.Select(p => p.Name) ?? new string[0])}");
            Sign(tight);
            var tightHouse = Specialist(tight).House;
            Check(tightHouse != null && !tightHouse.Insists && tightHouse.Players.Count == 0 && tightHouse.Reason.Contains("tight"), "A tight band brings its own players: " + tightHouse?.Reason);
            Unsign(tight);
            Live(band).artistCreativeControl = true;
            var refused = Specialist(band);
            Check(refused.Locked != null && refused.Locked.Contains("creative control") && !desk.StartSession(Live(band), one, refused.Tier, 3, out message, overrideRefusal: true, roomId: refused.Room.Id) &&
                desk.Session == null, "An act with creative control and an owner who insists can't meet: " + message);
            Live(band).artistCreativeControl = false;
            float before = desk.Label.cashReserves, morale = Live(band).morale;
            Check(desk.StartSession(Live(band), two, special.Tier, 5, out message, overrideRefusal: true, roomId: special.Room.Id), "Book the specialist with its house band: " + message);
            var date = desk.Session;
            var houseHires = date.Hires.Where(h => h.Kind == StudioHireKind.HousePlayer).ToList();
            Check(houseHires.Count == house.Players.Count && houseHires.All(h => h.Valid && house.Players.Any(p => p.PersonId == h.PersonId)) &&
                Math.Abs(before - desk.Label.cashReserves - PlayerDesk.StudioRoomCost(date.Room, 5) - special.Gas - house.Fee) < .01f &&
                Math.Abs(date.Cost - PlayerDesk.StudioRoomCost(date.Room, 5) - house.Fee) < .01f && date.ActionLog.Any(l => l.StartsWith("Owner: ")),
                "House players are paid once at booking, logged, and kept out of the recouped room cost");
            int replaced = houseHires.Count(h => h.Replaces);
            Check(Live(band).morale <= morale && morale - Live(band).morale <= .05f * PlayerDesk.StudioHouseUpset * replaced + .0001f,
                $"The members sat out take it, but less hard than a label's own choice ({morale - band.morale:F3} for {replaced})");
            Check(!LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, houseHires.Select(h => h.PersonId), date.Date, date.StartMinute, date.StartMinute + 60),
                "House players are reserved for the date");
            var sat = houseHires.Where(h => h.Replaces).Select(h => h.ReplacesPersonId).ToHashSet();
            for (int i = 0; i < 6; i++) if (!desk.CutStudioTake(i % 2, out _)) break;
            var takes = desk.Session.Cuts.SelectMany(c => c.Takes).ToList();
            Check(takes.Count > 0 && takes.All(t => houseHires.All(h => t.SessionPersonIds?.Contains(h.PersonId) == true)) &&
                takes.Where(t => t.FaultPersonId != null).All(t => !sat.Contains(t.FaultPersonId)),
                "Every pass has the house band on it, and a member who sat out is never blamed for a breakdown");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) &&
                desk.Session.Hires.Count(h => h.Kind == StudioHireKind.HousePlayer) == houseHires.Count, "House players survive save/load: " + message);
            band = ArtistManager.Instance.GetArtist(band.artistId);
            var badHire = PendingSessionSaveData.From(desk.Session); badHire.Flow.Hires[0].Kind = StudioHireKind.BackingVoice;
            Check(Rejected(() => badHire.ToSession(Array.Empty<PlayerDesk.Song>())), "A saved backing voice that replaces a member is rejected");
            var noReplace = PendingSessionSaveData.From(desk.Session); noReplace.Flow.Hires[0].Kind = StudioHireKind.Replacement; noReplace.Flow.Hires[0].ReplacesPersonId = null;
            Check(Rejected(() => noReplace.ToSession(Array.Empty<PlayerDesk.Song>())), "A saved replacement with nobody replaced is rejected");
            for (int s = 0; s < desk.Session.Cuts.Count; s++) {
                var cut = desk.Session.Cuts[s];
                for (int i = 0; i < 4 && StudioTakeEvidence.Choose(cut.Takes) < 0; i++) desk.CutStudioTake(s, out _);
                if (StudioTakeEvidence.Choose(cut.Takes) >= 0) desk.KeepTake(s, StudioTakeEvidence.Choose(cut.Takes));
            }
            Check(desk.Session.Cuts.All(c => c.KeptTake >= 0), "Both sides have a kept take");
            int mastersBefore = desk.Masters.Count;
            Check(desk.PrintSession(out message), "Print: " + message);
            var printed = desk.Masters.Skip(mastersBefore).ToList();
            Check(printed.Count == 2 && printed.All(m => m.StudioHistory.SessionPlayers.Any(c => c.Contains("the room's house player"))),
                "The masters credit the house players: " + string.Join(" | ", printed[0].StudioHistory.SessionPlayers));
            var visit = desk.StudioVisits.Single(v => v.SessionId == date.SessionId);
            Check(visit.RecordIds?.Count == 2 && printed.All(m => visit.RecordIds.Contains(m.Record.recordId)), "The remembered date knows the records it made");
            var trusted = desk.StudioHouseBand(special.Room, Live(band), clock.CurrentDate, 600, 1200);
            Check(band.studioPerformance >= PlayerDesk.StudioOwnPlayersAfterVisit ? !trusted.Insists && trusted.Reason.Contains("before") : trusted.Insists,
                $"After a printed date the owner trusts a band that's tight enough to bring its own ({band.studioPerformance:F2}): {trusted.Reason}");
            Morning(startDate.AddDays(9)); Check(desk.DriveHome(out _) || desk.AtHome, "Home");
            Unsign(band);

            // A singer with nobody behind them: the house band fills the parts, replacing nobody.
            Morning(startDate.AddDays(10));
            var singer = singers.First(a => Free(a, clock.CurrentDate)); Sign(singer);
            var singerRoom = Specialist(singer);
            if (singerRoom.Introduction == null) { Introduce(singerRoom.Room.Id, singer); singerRoom = Specialist(singer); }
            Check(singerRoom.House.Insists && (singerRoom.House.Locked != null || singerRoom.House.Players.Count >= 2 && singerRoom.House.Players.All(p => !string.IsNullOrEmpty(p.Name) && p.ReplacesPersonId == null)),
                $"A singer's date gets the house rhythm section, filling parts nobody plays: {singerRoom.House.Reason} {singerRoom.House.Locked}");
            Unsign(singer);

            // ================= Backing singers =================
            Check(PlayerDesk.StudioBackingCall(new StudioSongStructure { Archetype = SongArchetype.SpiritualShout }, Genre.Soul) >
                PlayerDesk.StudioBackingCall(new StudioSongStructure { Archetype = SongArchetype.SlowBlues }, Genre.Blues) &&
                PlayerDesk.StudioBackingCall(new StudioSongStructure { Archetype = SongArchetype.MidTempoRocker, Instrumental = true }, Genre.DooWop) == 0 &&
                PlayerDesk.StudioBackingCall(new StudioSongStructure { Archetype = SongArchetype.MidTempoRocker }, Genre.DooWop) >
                PlayerDesk.StudioBackingCall(new StudioSongStructure { Archetype = SongArchetype.MidTempoRocker }, Genre.RockAndRoll),
                "A gospel shout calls for voices, a slow blues barely, an instrumental not at all; doo-wop calls more than rock and roll");
            Check(PlayerDesk.StudioBackingVocal(.9f, .7f) > 0 && PlayerDesk.StudioBackingVocal(.1f, .7f) < 0 && PlayerDesk.StudioBackingVocal(1, 1) <= .05f &&
                PlayerDesk.StudioBackingVocal(0, 1) >= -.02f && PlayerDesk.StudioBackingVocal(.9f, .9f) > PlayerDesk.StudioBackingVocal(.9f, .4f),
                "Voices lift a song that calls for them, clutter one that doesn't, more with a better group, within bounds");
            Morning(startDate.AddDays(12));
            singer = singers.First(a => Free(a, clock.CurrentDate)); Sign(singer);
            Sign(harmony);
            Check(desk.StudioBackingSingers("atlanta", Live(harmony), clock.CurrentDate, 540, 900) == null &&
                !desk.StartSession(Live(harmony), one, PlayerDesk.StudioTier.Budget, 3, out message, overrideRefusal: true, backingSingers: true) && desk.Session == null,
                "A band with its own singers isn't offered a backing group: " + message);
            Unsign(harmony);
            GD.Seed(78); next = GD.Randi(); GD.Seed(78);
            var group = desk.StudioBackingSingers("atlanta", Live(singer), clock.CurrentDate, 540, 900);
            Check(GD.Randi() == next && Json(group) == Json(desk.StudioBackingSingers("atlanta", Live(singer), clock.CurrentDate, 540, 900)), "Finding a backing group is pure");
            Check(group != null && group.Voices.Count is >= 2 and <= 3 && group.GroupName.EndsWith("Singers") && group.Voices.All(v => singer.members.All(m => m.personId != v.PersonId)) &&
                group.Fee == group.Voices.Sum(v => Mathf.Round(10f + 30f * v.Reading)), $"A singer can hire a local backing group: {group?.Text}");
            before = desk.Label.cashReserves;
            Check(desk.StartSession(Live(singer), two, PlayerDesk.StudioTier.Budget, 6, out message, overrideRefusal: true, backingSingers: true), "Book with backing singers: " + message);
            date = desk.Session;
            var voices = date.Hires.Where(h => h.Kind == StudioHireKind.BackingVoice).ToList();
            Check(voices.Count == group.Voices.Count && voices.All(v => v.Valid && v.GroupName == group.GroupName && !v.Replaces) &&
                Math.Abs(before - desk.Label.cashReserves - PlayerDesk.StudioRoomCost(date.Room, 6) - group.Fee) < .01f && date.ActionLog.Any(l => l.StartsWith("Backing singers:")),
                "The group's fees are charged once at booking, and nobody in the act is replaced");
            // The same pass, with and without the group, under identical draws.
            date.Cuts[0].Structure.Archetype = SongArchetype.SpiritualShout;
            var withVoices = desk.CaptureState(); var clockAt = (clock.CurrentHour, clock.CurrentMinute);
            Check(desk.CutStudioTake(0, out message), "Cut with voices: " + message);
            var withTake = desk.Session.Cuts[0].Takes[^1];
            Check(desk.RestoreState(withVoices, out message), "Rewind: " + message); clock.RestoreClock(date.Date, clockAt.CurrentHour, clockAt.CurrentMinute);
            desk.Session.Hires.RemoveAll(h => h.Kind == StudioHireKind.BackingVoice);
            Check(desk.CutStudioTake(0, out message), "Cut without voices: " + message);
            var bareTake = desk.Session.Cuts[0].Takes[^1];
            float call = PlayerDesk.StudioBackingCall(desk.Session.Cuts[0].Structure, singer.primaryGenre), expected = PlayerDesk.StudioBackingVocal(call, voices.Average(v => v.Skill));
            Check(expected > 0 && withTake.Vocal > bareTake.Vocal && withTake.Vocal - bareTake.Vocal <= expected + .0001f && withTake.SessionPersonIds?.Count == voices.Count && bareTake.SessionPersonIds == null,
                $"Under identical draws the voices lift the vocal by at most their effect ({withTake.Vocal - bareTake.Vocal:F3} of {expected:F3}), and the take records who sang");
            Check(desk.RestoreState(withVoices, out message), "Rewind: " + message); clock.RestoreClock(date.Date, clockAt.CurrentHour, clockAt.CurrentMinute);
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) &&
                desk.Session.Hires.Count(h => h.Kind == StudioHireKind.BackingVoice) == voices.Count, "Backing singers survive save/load: " + message);
            singer = ArtistManager.Instance.GetArtist(singer.artistId);
            var liftedVoice = PendingSessionSaveData.From(desk.Session); liftedVoice.Flow.Hires.First(h => h.Kind == StudioHireKind.BackingVoice).Lift = .05f;
            Check(Rejected(() => liftedVoice.ToSession(Array.Empty<PlayerDesk.Song>())), "A saved backing voice carrying a playing lift is rejected");
            desk.Session.Cuts[0].Structure.Archetype = SongArchetype.SpiritualShout;
            for (int s = 0; s < 2; s++) {
                var cut = desk.Session.Cuts[s];
                for (int i = 0; i < 4 && StudioTakeEvidence.Choose(cut.Takes) < 0; i++) desk.CutStudioTake(s, out _);
                if (StudioTakeEvidence.Choose(cut.Takes) >= 0) desk.KeepTake(s, StudioTakeEvidence.Choose(cut.Takes));
            }
            if (desk.Session.Cuts.All(c => c.KeptTake >= 0)) {
                mastersBefore = desk.Masters.Count;
                Check(desk.PrintSession(out message), "Print: " + message);
                var credit = desk.Masters.Skip(mastersBefore).First().StudioHistory.SessionPlayers;
                Check(credit.Count(c => c.StartsWith(group.GroupName) && c.Contains("backing vocals")) == 1, "The group is credited once, by name: " + string.Join(" | ", credit));
            } else desk.ScrapSession();

            // The producer's suggestion mid-date.
            Morning(startDate.AddDays(13));
            Check(desk.StartSession(Live(singer), two, PlayerDesk.StudioTier.Budget, 6, out message, overrideRefusal: true), "Book without backing: " + message);
            date = desk.Session;
            date.Cuts[0].Structure.Archetype = SongArchetype.SpiritualShout; date.Cuts[1].Structure.Archetype = SongArchetype.SpiritualShout;
            Check(desk.StudioBackingCallIn() == null, "No suggestion before anything is heard");
            for (int i = 0; i < 4 && date.Cuts[0].Takes.All(t => t.Incomplete); i++) desk.CutStudioTake(0, out _);
            var proposal = desk.StudioBackingCallIn();
            Check(proposal != null && proposal.Text.Contains(proposal.GroupName) && proposal.Text.Contains("bare") && proposal.ArrivalMinutes == PlayerDesk.StudioBackingArrivalMinutes,
                "After a complete pass on a song that calls for voices, the producer suggests a group: " + proposal?.Text);
            var choose = desk.CaptureState(); clockAt = (clock.CurrentHour, clock.CurrentMinute);
            Check(desk.DeclineBackingSingers(out _) && desk.StudioBackingCallIn() == null, "Declining ends the suggestion for the date");
            Check(desk.RestoreState(choose, out message), "Restore: " + message); clock.RestoreClock(date.Date, clockAt.CurrentHour, clockAt.CurrentMinute);
            before = desk.Label.cashReserves; float paid = desk.Session.Cost;
            int was = clock.CurrentHour * 60 + clock.CurrentMinute;
            Check(desk.CallInBackingSingers(out message), "Call them in: " + message);
            Check(Math.Abs(before - desk.Label.cashReserves - proposal.Fee) < .01f && desk.Session.Cost == paid + proposal.Fee &&
                clock.CurrentHour * 60 + clock.CurrentMinute == was + proposal.ArrivalMinutes && desk.StudioBackingCallIn() == null &&
                desk.Session.Cuts[0].Takes.All(t => t.SessionPersonIds == null), "The call-in pays once, spends the wait, and earlier takes stay as they were");
            Check(desk.RunStudioDate(out message) || message.Contains("safe"), "Delegation runs with the group: " + message);
            Check(desk.Session.Hires.Count(h => h.Kind == StudioHireKind.BackingVoice) == proposal.Voices.Count, "Delegation hires nobody more");
            desk.ScrapSession();
            Unsign(singer);

            // ================= Renown =================
            Check(PlayerDesk.StudioHitWeight(1) > PlayerDesk.StudioHitWeight(25) && PlayerDesk.StudioHitWeight(25) > PlayerDesk.StudioHitWeight(80) && PlayerDesk.StudioHitWeight(0) == 0,
                "A top-ten record counts more than a top-forty one, which counts more than a minor chart record");
            Check(PlayerDesk.StudioHitPull(0) == 0 && PlayerDesk.StudioHitPull(7 * PlayerDesk.StudioRenownLagWeeks - 1) == 0 &&
                PlayerDesk.StudioHitPull(7 * 10) is > 0 and < 1 && Math.Abs(PlayerDesk.StudioHitPull(7 * PlayerDesk.StudioRenownFullWeeks) - 1) < .001f &&
                PlayerDesk.StudioHitPull(7 * 120) < PlayerDesk.StudioHitPull(7 * 20), "A hit's pull lags, ramps in, then fades");
            Morning(startDate.AddDays(14));
            band = loose.First(a => Free(a, clock.CurrentDate)); Sign(band);
            var other = loose.Last(a => a != band && Free(a, clock.CurrentDate)); Sign(other);
            string topId = StudioRoomCatalog.LocalId("atlanta", StudioRoomCatalog.Available("atlanta", desk.Label.homeRegion)[^1]);
            var plain = desk.StudioOffer(topId, band.artistId);
            Check(plain.Room.Renown == 0 && plain.Room.HourlyRate == plain.Room.BaseRate && desk.StudioRenownText(plain.Room) == null,
                "A room the label hasn't made known posts its ordinary rate");
            var noHits = desk.CaptureState();
            // Make a real chart record one of this label's, printed at the room, and let the weekly pass find it.
            var charted = ChartManager.Instance.GetAllRecords().Where(r => r.peakPosition is > 0 and <= 10 && r.baseRecord != null).OrderBy(r => r.baseRecord.recordId, StringComparer.Ordinal).First();
            string ownerLabel = charted.baseRecord.labelId;
            var withHit = desk.CaptureState();
            withHit.StudioVisits ??= new();
            withHit.StudioVisits.Add(new StudioVisit { SessionId = "rooms-test-visit", RoomId = topId, RoomName = plain.Room.Name, CityId = "atlanta",
                EngineerId = plain.Room.EngineerId, EngineerName = plain.Room.EngineerName, ArtistId = band.artistId, Date = clock.CurrentDate, Passes = 3, KeptSides = 2,
                Printed = true, FlaggedParts = new(new int[parts]), RecordIds = new() { charted.baseRecord.recordId } });
            Check(desk.RestoreState(withHit, out message), "A printed date at the room: " + message);
            charted.baseRecord.labelId = desk.Label.labelId;
            try { Private("UpdateStudioHits", clock.CurrentDate); } finally { charted.baseRecord.labelId = ownerLabel; }
            var hit = desk.StudioVisits.Single(v => v.SessionId == "rooms-test-visit").Hits?.SingleOrDefault();
            Check(hit != null && hit.Peak == charted.peakPosition && hit.Charted == clock.CurrentDate && hit.Title == charted.baseRecord.title,
                $"The weekly pass notes the record charted (#{hit?.Peak} \"{hit?.Title}\")");
            Check(desk.StudioRenown(topId, clock.CurrentDate) == 0 && desk.StudioOffer(topId, other.artistId).Room.HourlyRate == plain.Room.HourlyRate,
                "Word of a hit takes weeks to get around: the rate is unchanged at first");
            var later = clock.CurrentDate.AddDays(7 * 20);
            Morning(later);
            float renown = desk.StudioRenown(topId, later);
            var forHit = desk.StudioOffer(topId, band.artistId); var forOther = desk.StudioOffer(topId, other.artistId);
            float baseRate = forHit.Room.BaseRate;
            Check(renown > .3f && renown < .6f && forHit.Room.Renown == renown && forHit.Room.HourlyRate == Mathf.Round(baseRate * (1 + PlayerDesk.StudioRenownRateRise * renown * PlayerDesk.StudioRegularRateShare)) &&
                forHit.Room.HourlyRate > baseRate && forHit.Room.HourlyRate < Mathf.Round(baseRate * (1 + PlayerDesk.StudioRenownRateRise * renown)) + .01f,
                $"Months later the room is known ({renown:F2}) and its rate is up, but the label that made its name pays the regular's ${forHit.Room.HourlyRate} (base ${baseRate})");
            Check(desk.StudioRenownText(forHit.Room).Contains(charted.baseRecord.title) && forHit.Room.ActCutAHit && !forOther.Room.ActCutAHit, "The card says what the room is known for: " + desk.StudioRenownText(forHit.Room));
            int chiefForHit = 0, chiefForOther = 0, chiefBefore = 0, solidForHit = 0, solidForOther = 0, chiefIn = 0;
            for (int d = 0; d < 90; d++) {
                var day = later.AddDays(d); Morning(day);
                var h = desk.StudioOffer(topId, band.artistId); var o = desk.StudioOffer(topId, other.artistId);
                bool chiefAway = h.Engineers.Single(e => e.Chief).Away;
                if (!chiefAway) chiefIn++;
                if (h.Room.EngineerHouse) chiefForHit++;
                if (o.Room.EngineerHouse) chiefForOther++;
                if (h.Locked != null) solidForHit++;
                if (o.Locked?.Contains("booked solid") == true) solidForOther++;
                var zero = h.Room.Copy(); zero.Renown = 0; zero.ActCutAHit = false;
                if (StudioPeople.Engineers(zero, PlayerDesk.StudioTier.Top, day).Single(e => e.Chief).OnByDefault) chiefBefore++;
            }
            GD.Print($"STUDIO_ROOMS renown={renown:F2} rate={baseRate}->{forHit.Room.HourlyRate} (market {Mathf.Round(baseRate * (1 + PlayerDesk.StudioRenownRateRise * renown))}) " +
                $"chief: hit act {chiefForHit}/{chiefIn}, other act {chiefForOther}, unknown room {chiefBefore}; booked solid: hit act {solidForHit}, other {solidForOther} of 90");
            Check(chiefForHit == chiefIn && chiefForOther < chiefBefore && solidForHit == 0 && solidForOther > 0 && solidForOther < 30,
                "The act that cut the hit always gets the chief and a slot; other acts find the chief busier and the room sometimes full");
            Check(SaveGameService.Save(slot, out message) && SaveGameService.Load(slot, out message) && desk.StudioRenown(topId, later.AddDays(89)) > 0 &&
                desk.StudioVisits.Single(v => v.SessionId == "rooms-test-visit").Hits.Single().Peak == hit.Peak, "Remembered hits survive save/load: " + message);
            var badHit = desk.CaptureState(); badHit.StudioVisits.Single(v => v.SessionId == "rooms-test-visit").Hits[0].Peak = 0;
            Check(!desk.RestoreState(badHit, out message), "A remembered hit with no chart position is refused: " + message);
            Morning(later.AddDays(1));
            band = ArtistManager.Instance.GetArtist(band.artistId); other = ArtistManager.Instance.GetArtist(other.artistId);
            var hot = desk.StudioOffer(topId, band.artistId);
            before = desk.Label.cashReserves;
            Check(desk.StartSession(Live(band), one, hot.Tier, 3, out message, overrideRefusal: true, roomId: topId), "Book the known room: " + message);
            Check(Math.Abs(before - desk.Label.cashReserves - hot.Room.HourlyRate * 3) < .01f && desk.Session.Room.Renown == hot.Room.Renown && desk.Session.Room.BaseRate == baseRate &&
                desk.Session.ActionLog.Any(l => l.Contains("posted before its records got around")), "The date pays the regular's rate once and records the room's standing");
            var tooKnown = PendingSessionSaveData.From(desk.Session); tooKnown.Flow.Room.Renown = 1.5f;
            Check(Rejected(() => tooKnown.ToSession(Array.Empty<PlayerDesk.Song>())), "A saved room with impossible renown is rejected");
            desk.ScrapSession();
            Check(desk.RestoreState(noHits, out message), "Back to a room nobody knows: " + message);
            Check(desk.StudioOffer(topId, other.artistId).Room.HourlyRate == desk.StudioOffer(topId, other.artistId).Room.BaseRate, "Without the hit the rate is the posted one");
            Unsign(other);

            // ================= The booking sheet =================
            Morning(startDate.AddDays(16));
            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>(); AddChild(main);
            singer = singers.Select(Live).First(a => a != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && a.labelId != desk.Label.labelId && Free(a, clock.CurrentDate)); Sign(singer);
            var withSongs = desk.CaptureState();
            foreach (string title in new[] { "Card Song", "Other Card" }) withSongs.Songs.Add(SongSaveData.From(new PlayerDesk.Song {
                SongId = "studio-rooms:" + title, Title = title, ArtistId = singer.artistId, Genre = singer.primaryGenre, Hook = .5f, Originality = .5f, Danceability = .5f, Written = clock.CurrentDate }));
            Check(desk.RestoreState(withSongs, out message), "Material for the sheet: " + message);
            main.OpenStudio(singer.artistId);
            var panel = main.GetNode<PlayerDeskPanel>("StudioWindow");
            Walk(panel).OfType<CheckBox>().First().ButtonPressed = true;
            Check(Walk(panel).OfType<Label>().Any(l => l.Text.Contains("WHO PLAYS", StringComparison.OrdinalIgnoreCase)) &&
                (Walk(panel).OfType<CheckBox>().Any(b => b.Text.StartsWith("Hire backing singers")) || Walk(panel).OfType<Label>().Any(l => l.Text.Contains("no backing singers are free"))),
                "A singer's booking sheet offers backing singers under Who plays");
            foreach (var s in Walk(panel).OfType<ScrollContainer>()) s.ScrollVertical = (int)s.GetVScrollBar().MaxValue;
            await Screenshot("booking-singer");
            List<PanelContainer> Cards() => Walk(panel).OfType<PanelContainer>().Where(p => p.MouseFilter == Control.MouseFilterEnum.Stop && p.GetChildren().OfType<VBoxContainer>().Any()).ToList();
            var farCard = Cards().LastOrDefault(c => Walk(c).OfType<Label>().Any(l => l.Text == "OVERNIGHT"));   // innermost: the sheet itself contains every card
            Check(farCard != null && Walk(panel).OfType<Label>().Any(l => l.Text == "DRIVE UP THE NIGHT BEFORE"), "Overnight rooms are grouped and tagged on the sheet");
            farCard.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left });
            for (int i = 0; i < 2; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Walk(panel).OfType<Button>().Any(b => b.Text == "DRIVE UP WITH THE BAND") && Walk(panel).OfType<Label>().Any(l => l.Text.StartsWith("Band motel tonight")),
                "Choosing an overnight room turns the booking into a trip, quoted with the band's motel: " +
                string.Join(" | ", Walk(panel).OfType<Button>().Select(b => b.Text).Where(t => t.Length > 0).Take(30)));
            var scroll = Walk(panel).OfType<ScrollContainer>().OrderByDescending(s => s.GetVScrollBar().MaxValue).First();
            scroll.ScrollVertical = (int)(scroll.GetVScrollBar().MaxValue * .55);
            await Screenshot("booking-overnight");
            scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
            await Screenshot("booking-overnight-quote");
            Unsign(singer);
            GD.Print($"STUDIO_ROOMS_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_ROOMS_CHECK_FAIL " + (ex is TargetInvocationException t ? t.InnerException : ex)); GetTree().Quit(1); }
        finally { SaveGameService.Delete(slot); }
    }
}
