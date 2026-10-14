using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Bills that rotate (LocalSceneRoomService.Rotation): kind-sized bills, a fresh cast every night, regulars
/// that are earned in the room and close most nights. Drives only the room clock, day by day, for twelve weeks.</summary>
public static class SceneRoomRotationChecks {
    public static void Run() {
        if (!LocalScenes.Rooms) throw new InvalidOperationException("Scene rooms must be enabled for these checks.");
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_ROOM_ROTATION_CHECK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        string[] args = OS.GetCmdlineUserArgs();
        var date = TimeManager.Instance.CurrentDate;
        var am = ArtistManager.Instance;
        SceneRoomSaveData Snapshot() { var w = new WorldSaveData(); LocalSceneRoomService.CaptureWorld(w); return w.SceneRooms; }
        void RestoreRooms(SceneRoomSaveData saved) { LocalSceneRoomService.BeginRestore(saved); LocalSceneRoomService.CompleteRestore(); }
        SceneRoomSaveData Fresh(SceneRoomSaveData from) {
            var s = LocalSceneRoomService.Copy(from);
            s.Bills.Clear(); s.Engagements.Clear(); s.Standing.Clear(); s.Work.Clear(); s.HouseStanding.Clear();
            s.LastScheduledWeek = -1; s.LastResolvedDay = -1; s.LastResolvedHour = -1; s.CompletedPerformances = 0;
            return s;
        }
        _ = LocalSceneRoomService.Calendar("nashville", date);
        var start = Snapshot();
        try {
            // 1. Flags.
            bool Rejects(params string[] extra) { try { LocalScenes.Configure(args.Concat(extra)); return false; } catch (ArgumentException) { return true; } }
            Check(Rejects("--enable-scene-room-rotation", "--disable-scene-room-rotation"), "enable and disable together are refused");
            Check(Rejects("--enable-scene-room-rotation", "--disable-scene-rooms"), "rotation requires scene rooms");
            LocalScenes.Configure(args);
            Check(LocalScenes.RoomRotation, "on by default with rooms");
            Check(Rejects("--enable-scene-day-jobs", "--disable-scene-day-jobs"), "day jobs: enable and disable together are refused");
            Check(Rejects("--enable-scene-double-rooms", "--disable-scene-room-rotation"), "double rooms require room rotation");
            Check(LocalScenes.DayJobs && LocalScenes.DoubleRooms, "day jobs and double rooms are on with rotation");
            LocalScenes.Configure(args.Append("--disable-scene-room-rotation"));
            Check(!LocalScenes.DayJobs && !LocalScenes.DoubleRooms, "and off with it");
            LocalScenes.Configure(args);

            // 2. The opening calendar: bill size by room kind, running order, nothing earned yet.
            var perf = start.Bills.Where(b => SceneRoomCatalog.Get(b.RoomId).IsPerformance && b.Appearances.Count > 0).ToList();
            Check(perf.All(b => b.Appearances.Count <= SceneEcosystemService.Slots(SceneRoomCatalog.Get(b.RoomId), b.Day)), "no bill exceeds its slots");
            Check(perf.Where(b => SceneRoomCatalog.Get(b.RoomId).Kind == SceneRoomKind.ListeningRoom).All(b => b.Appearances.Count <= 2 + 1), "listening rooms book a featured act and one other (plus any program set)");
            var clubs = perf.Where(b => SceneRoomCatalog.Get(b.RoomId).Kind == SceneRoomKind.Club).ToList();
            Check(clubs.Count > 0 && clubs.Count(b => b.Appearances.Count >= 3) >= .9 * clubs.Count, "club nights carry three acts");
            Check(perf.All(b => b.Appearances.Select((a, i) => a.StartHour == b.StartHour + i && a.EndHour == a.StartHour + 1).All(x => x)),
                "sets run back to back from the room's opening hour");
            Check(perf.All(b => b.Appearances.Select((a, i) => a.Role == (i == 0 ? "opening set" : i == b.Appearances.Count - 1 ? "closing set" : "middle set")).All(x => x)),
                "roles follow running order");
            Check(perf.All(b => b.EndHour >= b.Appearances.Max(a => a.EndHour)), "every set ends inside its bill");
            Check(start.Engagements.GroupBy(e => e.RoomId).All(g => g.Count() <= LocalSceneRoomService.RegularsPerRoom), "a room names at most RegularsPerRoom regulars");
            Check(perf.All(b => b.Appearances.All(a => a.Residency == start.Engagements.Any(e => e.RoomId == b.RoomId && e.ArtistId == a.ArtistId && e.StartDay <= b.Day && e.ThroughDay >= b.Day))),
                "the residency flag means exactly a standing engagement");
            var room0 = SceneRoomCatalog.Get(clubs[0].RoomId);
            int cap0 = LocalSceneRoomService.CapacityOf(clubs[0]);
            Check(clubs.Where(b => b.Appearances.Count == 3).All(b => b.ExpectedAudience == Math.Min(LocalSceneRoomService.CapacityOf(b), (int)(LocalSceneRoomService.CapacityOf(b) * .60))),
                "a third act does not draw a third crowd (live-cal fill kept)");

            // 3. Determinism: a fresh calendar books the same nights from the keyed draws.
            RestoreRooms(Fresh(start));
            _ = LocalSceneRoomService.Calendar("nashville", date);
            Check(Json(Snapshot().Bills) == Json(start.Bills), "the same world books the same bills");

            // 4. Twelve weeks of the room clock.
            ulong population = WorldStateService.Capture().PopulationRngState;
            GD.Seed(4417); float next = GD.Randf(); GD.Seed(4417);
            const string club = "nashville:club";
            var nights = new List<SceneBill>();
            var allBills = new Dictionary<string, SceneBill>(StringComparer.Ordinal);
            for (int d = 0; d < 84; d++) {
                LocalSceneRoomService.Advance(date.AddDays(d), 24);
                foreach (var b in Snapshot().Bills.Where(b => b.Day == d)) allBills[b.Id] = b;
            }
            Check(GD.Randf() == next && WorldStateService.Capture().PopulationRngState == population, "booking consumes no global or population RNG");
            nights = allBills.Values.Where(b => b.RoomId == club && b.Status == SceneBillStatus.Performed).OrderBy(b => b.Day).ToList();
            var slotsHeld = allBills.Values.SelectMany(b => b.Appearances.SelectMany(s => s.PersonIds.Concat(s.BackingPersonIds ?? new())
                .Select(p => (Key: $"{b.Day}|{s.StartHour}|{p}", DayPerson: $"{b.Day}|{p}", b.RoomId)))).ToList();
            Check(slotsHeld.Select(r => r.Key).Distinct().Count() == slotsHeld.Count, "no person plays two sets at once");
            WorkingWeek(allBills.Values.ToList(), Snapshot().Engagements);

            int distinct = nights.SelectMany(b => b.Appearances).Select(a => a.ArtistId).Distinct().Count();
            int casts = nights.Select(b => string.Join("|", b.Appearances.Select(a => a.ArtistId).OrderBy(x => x, StringComparer.Ordinal))).Distinct().Count();
            var plays = nights.SelectMany(b => b.Appearances.Where(a => a.Status == SceneBillStatus.Performed)).GroupBy(a => a.ArtistId)
                .Select(g => g.Count()).OrderByDescending(n => n).ToArray();
            int sets = plays.Sum();
            GD.Print($"SCENE_ROOM_ROTATION {club} nights={nights.Count} sets={sets} distinctActs={distinct} distinctCasts={casts} top={string.Join(",", plays.Take(10))} " +
                $"top10share={plays.Take(10).Sum() / (float)sets:0.00} once={plays.Count(n => n == 1)}");
            Check(nights.Count >= 50, "the club works five nights a week");
            Check(distinct >= 25, "a rotating cast: at least 25 different acts in twelve weeks");
            // Day jobs (WorkingWeek): weeknights belong to the town's working pros, so their bills repeat more; the
            // rotating cast is a weekend thing.
            var weekendNights = nights.Where(b => LocalSceneRoomService.Weekend(LocalSceneRoomService.Date(b.Day).DayOfWeek)).ToList();
            int weekendCasts = weekendNights.Select(b => string.Join("|", b.Appearances.Select(a => a.ArtistId).OrderBy(x => x, StringComparer.Ordinal))).Distinct().Count();
            GD.Print($"SCENE_ROOM_ROTATION {club} weekendNights={weekendNights.Count} weekendCasts={weekendCasts}");
            Check(weekendCasts >= .8 * weekendNights.Count, "nearly every weekend night's bill is a different line-up");
            Check(casts >= (LocalScenes.DayJobs ? .5 : .8) * nights.Count, "most nights' bills are different line-ups");
            var regularsHere = Snapshot().Engagements.Where(e => e.RoomId == club).Select(e => e.ArtistId).ToHashSet(StringComparer.Ordinal);
            var supportPlays = nights.SelectMany(b => b.Appearances.Where(a => a.Status == SceneBillStatus.Performed && !regularsHere.Contains(a.ArtistId)))
                .GroupBy(a => a.ArtistId).Select(g => g.Count()).ToArray();
            Check(supportPlays.Count(n => n >= 3) >= 5, "a core of support acts the room keeps rebooking");
            Check(plays.Length > 0 && plays[0] >= .35 * nights.Count, "the room's leading act plays a large share of its nights");
            Check(Snapshot().Engagements.All(e => SceneRoomCatalog.Get(e.RoomId).IsPerformance), "trade appointments hold no residencies");

            var end = Snapshot();
            var engaged = end.Engagements.Where(e => e.ThroughDay >= 83).ToList();
            Check(end.Engagements.Count > 0, "regulars emerge from work in the room");
            Check(end.Engagements.GroupBy(e => (e.RoomId, e.StartDay / 91)).All(g => g.Select(e => e.ArtistId).Distinct().Count() <= LocalSceneRoomService.RegularsPerRoom * 2),
                "a room holds a bounded set of regulars per term");
            Check(end.Engagements.All(e => am.GetArtist(e.ArtistId)?.sceneParticipations?.Any(p => p.relationship == SceneRelationship.Resident &&
                p.sceneId == SceneRoomCatalog.Get(e.RoomId).PlaceId && p.performanceLevel >= ScenePerformanceLevel.Working) == true),
                "regulars are working residents of the room's own scene");
            foreach (var g in end.Bills.GroupBy(b => (b.RoomId, b.Day)))
                Check(end.Engagements.Count(e => e.RoomId == g.Key.RoomId && e.StartDay <= g.Key.Day && e.ThroughDay >= g.Key.Day) <= LocalSceneRoomService.RegularsPerRoom,
                    "never more than RegularsPerRoom engagements at once");
            var clubRegulars = end.Engagements.Where(e => e.RoomId == club).ToList();
            var withRegular = nights.Where(b => clubRegulars.Any(e => e.StartDay <= b.Day && e.ThroughDay >= b.Day)).ToList();
            if (withRegular.Count > 0) {
                float closed = withRegular.Count(b => b.Appearances.Last().Residency) / (float)withRegular.Count;
                float onBill = withRegular.Count(b => b.Appearances.Any(a => a.Residency)) / (float)withRegular.Count;
                foreach (var r in clubRegulars.Select(e => e.ArtistId).Distinct()) {
                    var act = am.GetArtist(r);
                    var mine = allBills.Values.Where(x => x.Appearances.Any(y => y.ArtistId == r && y.Status == SceneBillStatus.Performed)).ToList();
                    GD.Print($"SCENE_ROOM_ROTATION regular {act.stageName} live={act.livePerformance:0.00} rep={act.reputation:0.00} clubNights={mine.Count(x => x.RoomId == club)} " +
                        $"otherRoomNights={mine.Count(x => x.RoomId != club)} rooms={string.Join("/", mine.Select(x => x.RoomId).Distinct())} engagedAt={string.Join("/", end.Engagements.Where(e => e.ArtistId == r).Select(e => e.RoomId + ":" + e.StartDay).Distinct())}");
                }
                foreach (var x in nights.Take(10)) GD.Print($"SCENE_ROOM_ROTATION night {x.Day}: " + string.Join(" | ", x.Appearances.Select(y => $"{am.GetArtist(y.ArtistId).stageName}{(y.Residency ? "*" : "")}")));
                GD.Print($"SCENE_ROOM_ROTATION {club} regularNights={withRegular.Count} regularOnBill={onBill:0.00} regularCloses={closed:0.00}");
                Check(onBill >= .7f, "once a room has a regular, a regular is on most bills");
                Check(closed >= .6f, "and usually closes the night");
            }
            Check(plays.Length > 0 && plays[0] <= nights.Count, "no act plays more nights than the room was open");

            // 5. Off control: two acts drawn once per term, engagements conferred by booking.
            LocalScenes.Configure(args.Append("--disable-scene-room-rotation"));
            Check(!LocalScenes.RoomRotation && LocalSceneRoomService.BaseSlots(room0) == 2, "off restores the two-act bill");
            RestoreRooms(Fresh(start));
            var off = LocalSceneRoomService.Calendar("nashville", date);
            Check(off.Where(b => SceneRoomCatalog.Get(b.RoomId).IsPerformance).All(b => b.Appearances.Count <= SceneEcosystemService.Slots(SceneRoomCatalog.Get(b.RoomId), b.Day)) &&
                Snapshot().Engagements.Count > 0, "off books by term and confers engagements on booking");
            GD.Print($"SCENE_ROOM_ROTATION club0={room0.Id} capacity={cap0}");
        } finally {
            LocalScenes.Configure(args);
            RestoreRooms(start);
        }
        GD.Print($"SCENE_ROOM_ROTATION_PASS checks={checks}");

        // The working week (LocalSceneRoomService.WorkingWeek), over every room in the country for the twelve weeks.
        void WorkingWeek(List<SceneBill> bills, List<SceneEngagement> engagements) {
            bool FullTime(SimulatedArtist a, int day) => LocalSceneRoomService.Traction(a) || LocalSceneRoomService.GonePro(a) || LocalSceneRoomService.ActShows(a.artistId) >= LocalSceneRoomService.ProShows ||
                engagements.Any(e => e.ArtistId == a.artistId && e.StartDay <= day && e.ThroughDay >= day) ||
                a.members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active && MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm);
            var sets = bills.Where(b => SceneRoomCatalog.Get(b.RoomId).IsPerformance).SelectMany(b => b.Appearances.SelectMany(s =>
                s.PersonIds.Select(p => (Person: p, Backing: false)).Concat((s.BackingPersonIds ?? new()).Select(p => (Person: p, Backing: true)))
                .Select(x => (b.Day, b.RoomId, s.ArtistId, s.StartHour, s.EndHour, x.Person, x.Backing)))).ToList();
            int doubledDays = 0;
            var doublers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in sets.GroupBy(x => (x.Day, x.Person))) {
                var rooms = g.Select(x => x.RoomId).Distinct().ToArray();
                Check(rooms.Length <= LocalSceneRoomService.MaxRoomsPerDay, "at most two rooms per person per day");
                if (rooms.Length < 2) continue;
                var dayActs = g.Select(x => x.ArtistId).Distinct().ToArray();
                Check(dayActs.Length == 1 && g.All(x => !x.Backing), "a second room is the same act, as a member (backers stay in one room)");
                Check(FullTime(am.GetArtist(dayActs[0]), g.Key.Day), "only a full-time act works two rooms in a day");
                Check(rooms.Select(r => SceneRoomCatalog.Get(r).PlaceId).Distinct().Count() == 1, "both rooms are in the act's town");
                Check(g.All(x => g.Where(y => y.RoomId != x.RoomId).All(y => x.EndHour + LocalSceneRoomService.TravelHours <= y.StartHour ||
                    y.EndHour + LocalSceneRoomService.TravelHours <= x.StartHour)), "time to cross town between rooms");
                doubledDays++; doublers.Add(dayActs[0]);
            }
            var played = bills.Where(b => SceneRoomCatalog.Get(b.RoomId).IsPerformance)
                .SelectMany(b => b.Appearances.Select(a => (Bill: b, a.ArtistId))).ToList();
            var weekday = played.Where(x => !LocalSceneRoomService.Weekend(LocalSceneRoomService.Date(x.Bill.Day).DayOfWeek) &&
                SceneRoomCatalog.Get(x.Bill.RoomId).Kind != SceneRoomKind.Coffeehouse).ToList();
            Check(weekday.All(x => FullTime(am.GetArtist(x.ArtistId), x.Bill.Day)), "day-job acts play weekends (and coffeehouse open stages)");
            var acts = played.Select(x => x.ArtistId).Distinct().ToArray();
            int fullTimeActs = acts.Count(id => FullTime(am.GetArtist(id), 83));
            GD.Print($"SCENE_WORKING_WEEK_WHY signed={acts.Count(id => !string.IsNullOrEmpty(am.GetArtist(id).labelId))} traction={acts.Count(id => LocalSceneRoomService.Traction(am.GetArtist(id)))} gonePro={acts.Count(id => LocalSceneRoomService.GonePro(am.GetArtist(id)))} proShows={acts.Count(id => LocalSceneRoomService.ActShows(id) >= LocalSceneRoomService.ProShows)} " +
                $"engaged={acts.Count(id => engagements.Any(e => e.ArtistId == id))} " +
                $"wealthy={acts.Count(id => am.GetArtist(id).members.Any(m => m.isActive && m.lifeState == MemberLifeState.Active && MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm))} " +
                $"wealthNorm p50={Pct(.5):0.00} p75={Pct(.75):0.00} p90={Pct(.9):0.00}");
            double Pct(double q) { var v = acts.Select(id => am.GetArtist(id).members.Where(m => m.isActive).Select(MemberWealthService.Norm).DefaultIfEmpty(0f).Max()).OrderBy(x => x).ToArray(); return v[(int)(q * (v.Length - 1))]; }
            var actDays = played.GroupBy(x => (x.Bill.Day, x.ArtistId)).Count();
            var actRoomDays = played.Select(x => (x.Bill.Day, x.ArtistId, x.Bill.RoomId)).Distinct().Count();
            var weekdayClub = bills.Where(b => SceneRoomCatalog.Get(b.RoomId).Kind == SceneRoomKind.Club &&
                !LocalSceneRoomService.Weekend(LocalSceneRoomService.Date(b.Day).DayOfWeek) && b.Appearances.Count > 0).ToList();
            var weekendClub = bills.Where(b => SceneRoomCatalog.Get(b.RoomId).Kind == SceneRoomKind.Club &&
                LocalSceneRoomService.Weekend(LocalSceneRoomService.Date(b.Day).DayOfWeek) && b.Appearances.Count > 0).ToList();
            GD.Print($"SCENE_WORKING_WEEK sets={played.Count} acts={acts.Length} fullTimeActs={fullTimeActs} weekdaySets={weekday.Count} " +
                $"actDays={actDays} doubledActDays={actRoomDays - actDays} doublers={doublers.Count} personDoubleDays={doubledDays} " +
                $"clubActsPerBill weekday={(weekdayClub.Count == 0 ? 0 : weekdayClub.Average(b => b.Appearances.Count)):0.00} weekend={(weekendClub.Count == 0 ? 0 : weekendClub.Average(b => b.Appearances.Count)):0.00}");
            if (LocalScenes.DoubleRooms) Check(doublers.Count > 0, "some full-time acts work two rooms in a day");
            Check(actRoomDays - actDays <= .05 * actDays, "doubling is the exception: at most one act-day in twenty");
        }
    }
}
