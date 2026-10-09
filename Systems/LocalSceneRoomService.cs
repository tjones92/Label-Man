using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>World-owned bookings and performance facts. No births, roster/activation writes, global draws,
/// national cash transfers or extra development rewards. Calendar reads return detached snapshots.</summary>
public static class LocalSceneRoomService {
    private static SceneRoomSaveData state;
    private static SceneRoomSaveData previous;
    private static bool restoring;
    private static readonly Dictionary<string, SceneWorkAccount> work = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SceneRoomStanding> standing = new(StringComparer.Ordinal);
    private static SceneRoomSaveData State => state ??= new();
    public static int Day(GameDate date) => (int)(new DateTime(date.year, date.month, date.day) - new DateTime(1960, 1, 1)).TotalDays;
    public static GameDate Date(int day) => GameDate.StartDate.AddDays(day);
    internal static T Copy<T>(T value) => value == null ? default : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
    private static double Draw(string key) => LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, "room-v1|" + key);
    private static string WorkKey(string artist, string person, int year) => $"{year}|{artist}|{person}";
    private static string StandingKey(string room, string artist) => room + "|" + artist;
    public static bool Eligible(SimulatedArtist act, SceneRoomProfile room, int day) {
        if (room == null || act == null || !LocalScenePersistenceService.Present(act, room.PlaceId, day / 7)) return false;
        if (!SceneEcosystemService.AvailableAt(act.artistId, room.PlaceId, day)) return false;
        int year = Date(day).year;
        if (year < room.FromYear || year > room.ThroughYear) return false;
        GenreFamily family = GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family;
        return room.Families.Contains(family) && StagePeople(act).Length > 0;
    }
    private static string[] StagePeople(SimulatedArtist a) => a.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active)
        .Select(m => m.personId).Where(id => !string.IsNullOrEmpty(id)).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();

    public static void EnsureCalendar(GameDate date) {
        if (!LocalScenes.Rooms || restoring || Day(date) < 0) return;
        int current = Day(date) / 7;
        CancelFrozenPast(Day(date));
        // This only reconciles at the world-week boundary, never scans the registry per room/day.
        LocalScenePersistenceService.Advance(ChartManager.Instance?.GetCurrentChartWeek() ?? 0, date.year);
        if (State.LastScheduledWeek >= current + 1) return;
        // A legacy save starts now, without inventing past performances or awards.
        int first = Math.Max(current, State.LastScheduledWeek + 1);
        for (int week = first; week <= current + 1; week++) Schedule(week, Day(date));
        State.LastScheduledWeek = current + 1;
        State.Bills.RemoveAll(b => b.Day < current * 7 && b.Status != SceneBillStatus.Scheduled);
        State.Engagements.RemoveAll(e => e.ThroughDay < current * 7);
        State.Bills.RemoveAll(b => b.Day < current * 7 && b.Status == SceneBillStatus.Scheduled && State.LastResolvedDay >= b.Day);
    }
    private static void Schedule(int week, int earliestDay) {
        var reservations = new HashSet<string>(StringComparer.Ordinal);
        var personPlaces = new Dictionary<string, string>(StringComparer.Ordinal);
        var actPlaces = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var bill in State.Bills) foreach (var slot in bill.Appearances) {
            foreach (string person in slot.PersonIds) {
                personPlaces[$"{bill.Day}|{person}"] = bill.RoomId;
                for (int h = slot.StartHour; h < slot.EndHour; h++) reservations.Add($"{bill.Day}|{h}|{person}");
            }
            actPlaces[$"{bill.Day}|{slot.ArtistId}"] = bill.RoomId;
        }
        for (int day = Math.Max(week * 7, earliestDay); day < week * 7 + 7 && Date(day) <= GameDate.EndDate; day++) {
            GameDate date = Date(day);
            foreach (var room in SceneRoomCatalog.All.Where(r => r.Nights.Contains(date.DayOfWeek))) {
                if (State.Bills.Any(b => b.RoomId == room.Id && b.Day == day)) continue;
                string id = $"{room.Id}:{day}";
                var bill = new SceneBill { Id = id, RoomId = room.Id, Day = day, Year = date.year,
                    StartHour = room.OpenHour, EndHour = room.CloseHour };
                var members = LocalScenePersistenceService.MembersOf(room.PlaceId).Select(ArtistManager.Instance.GetArtist)
                    .Where(a => Eligible(a, room, day)).ToArray();
                int term = day / 91;
                var regularIds = State.Engagements.Where(e => e.RoomId == room.Id && e.StartDay <= day && e.ThroughDay >= day).Select(e => e.ArtistId).ToHashSet(StringComparer.Ordinal);
                var candidates = members.OrderBy(a => regularIds.Contains(a.artistId) ? 0 : 1)
                    .ThenBy(a => Draw($"engagement|{room.Id}|{term}|{a.artistId}"))
                    .ThenBy(a => a.artistId, StringComparer.Ordinal);
                foreach (var act in candidates) {
                    if (bill.Appearances.Count == SceneEcosystemService.Slots(room, day)) break;
                    string[] people = StagePeople(act);
                    int hour = room.OpenHour + bill.Appearances.Count;
                    // No cross-city teleporting or shared-musician overlap. We conservatively keep a
                    // performer's whole day in one room until intracity travel is modeled explicitly.
                    if (actPlaces.TryGetValue($"{day}|{act.artistId}", out var ar) && ar != room.Id ||
                        people.Any(p => reservations.Contains($"{day}|{hour}|{p}") ||
                            personPlaces.TryGetValue($"{day}|{p}", out var pr) && pr != room.Id)) continue;
                    bool regular = !SceneEcosystemService.HasPlannedMove(act.artistId) &&
                        act.sceneParticipations.Any(p => p.relationship == SceneRelationship.Resident && p.sceneId == room.PlaceId &&
                        !p.endWeek.HasValue && p.performanceLevel >= ScenePerformanceLevel.Working);
                    if (regular && !regularIds.Contains(act.artistId))
                        State.Engagements.Add(new() { RoomId = room.Id, ArtistId = act.artistId, StartDay = day, ThroughDay = (term + 1) * 91 - 1 });
                    bill.Appearances.Add(new() { ArtistId = act.artistId, PersonIds = people.ToList(), StartHour = hour, EndHour = hour + 1,
                        Role = bill.Appearances.Count == 0 ? "opening set" : "closing set", Residency = regular,
                        Set = SetFor(act, id, date.year) });
                    actPlaces[$"{day}|{act.artistId}"] = room.Id;
                    foreach (string person in people) { reservations.Add($"{day}|{hour}|{person}"); personPlaces[$"{day}|{person}"] = room.Id; }
                }
                if (bill.Appearances.Count == 3) bill.Appearances[1].Role = "middle set";
                int prior = bill.Appearances.Sum(a => standing.TryGetValue(StandingKey(room.Id, a.ArtistId), out var s) ? Math.Min(12, s.Shows) : 0);
                bill.ExpectedAudience = bill.Appearances.Count == 0 ? 0 : Math.Min(room.Capacity,
                    (int)(room.Capacity * (.30 + .15 * bill.Appearances.Count + .01 * prior)));
                State.Bills.Add(bill);
            }
        }
        State.Bills = State.Bills.OrderBy(b => b.Day).ThenBy(b => b.RoomId, StringComparer.Ordinal).ToList();
    }
    private static List<SceneSetSong> SetFor(SimulatedArtist artist, string bill, int year) {
        GenreFamily family = GenreCatalog.Get(GenreCatalog.MapLegacy(artist.primaryGenre, year)).Family;
        var songs = CompositionCatalogService.GetStandardsForGenre(artist.primaryGenre)
            .Concat(CompositionCatalogService.GetStandardsForFamily(family)).Where(s => s != null && s.originYear <= year && LiveRepertoire.EligibleLive(s, artist))
            .DistinctBy(s => s.songId).OrderBy(s => Draw($"set|{bill}|{artist.artistId}|{s.songId}")).Take(3)
            .Select(s => new SceneSetSong { SongId = s.songId, Title = s.title, Genre = s.primaryGenre, ContentContext = s.contentContext }).ToList();
        if (artist.songwritingAbility > .3f) songs.Insert(0, new() { Title = "Unrecorded original", Genre = artist.primaryGenre, Original = true });
        return songs.Take(3).ToList();
    }
    /// <summary>Called by the clock, independently of visits. Resolve each shared fact exactly once.</summary>
    public static void Advance(GameDate date, int hour) {
        if (!LocalScenes.Rooms || restoring) return;
        SceneEcosystemService.Advance(date);
        int day = Day(date);
        if (day < 0 || day < State.LastResolvedDay || day == State.LastResolvedDay && hour <= State.LastResolvedHour) return;
        CancelFrozenPast(day);
        // Resolve retained bills before pruning them or opening a new week.
        Resolve(day, hour);
        EnsureCalendar(date);
        Resolve(day, hour);
        State.LastResolvedDay = day; State.LastResolvedHour = hour;
    }
    private static void CancelFrozenPast(int day) {
        if (State.LastScheduledWeek < 0 || day / 7 <= State.LastScheduledWeek + 1) return;
        foreach (var bill in State.Bills.Where(b => b.Status == SceneBillStatus.Scheduled && b.Day < day)) {
            bill.Status = SceneBillStatus.Cancelled; bill.Response = "Calendar paused; no remaining past work was simulated.";
            foreach (var slot in bill.Appearances.Where(s => s.Status == SceneBillStatus.Scheduled)) {
                slot.Status = SceneBillStatus.Cancelled; slot.CancellationReason = bill.Response;
            }
        }
    }
    private static SceneWorkAccount Account(string artist, string person, int year) {
        string key = WorkKey(artist, person, year);
        if (work.TryGetValue(key, out var account)) return account;
        account = new() { ArtistId = artist, PersonId = person, Year = year };
        work.Add(key, account); State.Work.Add(account); return account;
    }
    private static void Resolve(int day, int hour) {
        foreach (var bill in State.Bills.Where(b => b.Status == SceneBillStatus.Scheduled && b.Day <= day)) {
            var room = SceneRoomCatalog.Get(bill.RoomId);
            foreach (var slot in bill.Appearances.Where(s => s.Status == SceneBillStatus.Scheduled && (bill.Day < day || s.EndHour <= hour))) {
                var act = ArtistManager.Instance.GetArtist(slot.ArtistId);
                bool available = Eligible(act, room, bill.Day) && StagePeople(act).SequenceEqual(slot.PersonIds);
                slot.Status = available ? SceneBillStatus.Performed : SceneBillStatus.Cancelled;
                if (!available) slot.CancellationReason = "The act's presence, career or lineup changed after booking.";
                if (!available || !room.IsPerformance) continue;
                string key = StandingKey(room.Id, slot.ArtistId);
                if (!standing.TryGetValue(key, out var rank)) {
                    rank = new() { RoomId = room.Id, ArtistId = slot.ArtistId };
                    standing.Add(key, rank); State.Standing.Add(rank);
                }
                rank.Shows++; rank.LastDay = bill.Day;
                foreach (string person in slot.PersonIds) Account(slot.ArtistId, person, bill.Year).StageHours += slot.EndHour - slot.StartHour;
                State.CompletedPerformances++;
            }
            if (bill.Day == day && bill.EndHour > hour) continue;
            var performed = bill.Appearances.Where(a => a.Status == SceneBillStatus.Performed).ToArray();
            bill.Status = performed.Length > 0 ? SceneBillStatus.Performed : SceneBillStatus.Cancelled;
            bill.Attendance = performed.Length == 0 ? 0 : Math.Min(room.Capacity,
                (int)(bill.ExpectedAudience * performed.Length / Math.Max(1f, bill.Appearances.Count) * (.8 + .35 * Draw("attendance|" + bill.Id))));
            bill.GrossReceipts = room.IsPerformance ? bill.Attendance * room.Admission : 0;
            bill.Response = performed.Length == 0 ? "No set went ahead." : bill.Attendance >= room.Capacity * .6
                ? "A busy room stayed for the sets." : "A small audience stayed for the sets.";
            foreach (var slot in performed) {
                if (!room.IsPerformance) continue;
                slot.Fee = bill.GrossReceipts * .5f / performed.Length;
                foreach (string person in slot.PersonIds) Account(slot.ArtistId, person, bill.Year).FeeShare += slot.Fee / slot.PersonIds.Count;
            }
        }
    }
    /// <summary>Attribute real work inside the existing annual allowance. The band owner alone applies
    /// fatigue/growth. Excess observed work is exposed for later calibration, never silently added twice.</summary>
    public static void AttributeBudget(string artist, string person, int year, float baselineHours) {
        if (!LocalScenes.Rooms || !work.TryGetValue(WorkKey(artist, person, year), out var account)) return;
        account.AttributedHours = Math.Min(account.StageHours, Math.Max(0, baselineHours));
        account.BackgroundHours = Math.Max(0, baselineHours - account.AttributedHours);
        account.UnbudgetedHours = Math.Max(0, account.StageHours - account.AttributedHours);
    }
    public static void AttributeRoadBudget(string artist, string person, int year, float annualRoadHours) {
        if (!LocalScenes.Rooms || !work.TryGetValue(WorkKey(artist, person, year), out var account)) return;
        account.AttributedRoadHours = Math.Min(account.StageHours, Math.Max(0, annualRoadHours));
        account.BackgroundRoadHours = Math.Max(0, annualRoadHours - account.AttributedRoadHours);
    }
    public static IReadOnlyList<SceneBill> Calendar(string placeId, GameDate date) {
        if (!LocalScenes.Rooms) return Array.Empty<SceneBill>();
        EnsureCalendar(date);
        string scene = LocalScenePersistenceService.SceneIdFor(placeId);
        return Array.AsReadOnly(Copy(State.Bills.Where(b => SceneRoomCatalog.Get(b.RoomId)?.PlaceId == scene && b.Day >= Day(date)).ToArray()));
    }
    public static SceneBill CurrentBill(string roomId, GameDate date, int hour) {
        if (!LocalScenes.Rooms) return null;
        EnsureCalendar(date);
        return Copy(State.Bills.FirstOrDefault(b => b.RoomId == roomId && b.Day == Day(date) && b.StartHour <= hour && b.EndHour >= hour + PlayerDesk.ScoutHours && b.Status == SceneBillStatus.Scheduled));
    }
    public static IReadOnlyList<SimulatedArtist> Hear(SceneBill bill, int? fromHour = null) {
        if (!LocalScenes.Rooms || bill == null) return Array.Empty<SimulatedArtist>();
        var canonical = State.Bills.FirstOrDefault(b => b.Id == bill.Id);
        var room = SceneRoomCatalog.Get(canonical?.RoomId);
        return Array.AsReadOnly((canonical?.Appearances ?? new()).Where(s => s.Status != SceneBillStatus.Cancelled && s.EndHour > (fromHour ?? bill.StartHour) && StagePeople(ArtistManager.Instance.GetArtist(s.ArtistId) ?? new SimulatedArtist()).SequenceEqual(s.PersonIds))
            .Select(s => ArtistManager.Instance.GetArtist(s.ArtistId)).Where(a => Eligible(a, room, bill.Day)).ToArray());
    }
    /// <summary>Read existing future facts. Unlike Calendar, this never ensures or advances a schedule.</summary>
    // Pure read: do not create state or schedule while examining a potential move.
    internal static bool HasFutureCommitment(string artistId, int day) =>
        state?.Bills.Any(b => b.Day >= day && b.Status == SceneBillStatus.Scheduled &&
            b.Appearances.Any(a => a.ArtistId == artistId && a.Status == SceneBillStatus.Scheduled)) == true ||
        state?.Engagements.Any(e => e.ArtistId == artistId && e.ThroughDay >= day) == true;

    internal static (int Slots, int BookedSlots, int BookedActs, int RecurringActs) ObserveWeekCapacity(string placeId, int week) {
        var rooms = SceneRoomCatalog.All.Where(r => r.PlaceId == placeId && r.IsPerformance).ToArray();
        var roomIds = rooms.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        // Schedule's existing two-appearance rule, limited to actual dated performance nights.
        int slots = rooms.Sum(r => Enumerable.Range(week * 7, 7).Sum(day => {
            var date = Date(day);
            return date <= GameDate.EndDate && date.year >= r.FromYear && date.year <= r.ThroughYear
                && r.Nights.Contains(date.DayOfWeek) ? SceneEcosystemService.Slots(r, day) : 0;
        }));
        var booked = (state?.Bills ?? new()).Where(b => roomIds.Contains(b.RoomId) && b.Day / 7 == week)
            .SelectMany(b => b.Appearances).Where(a => a.Status != SceneBillStatus.Cancelled).ToArray();
        int recurring = (state?.Engagements ?? new()).Where(e => roomIds.Contains(e.RoomId)
            && e.StartDay <= week * 7 + 6 && e.ThroughDay >= week * 7).Select(e => e.ArtistId).Distinct().Count();
        return (slots, booked.Length, booked.Select(a => a.ArtistId).Distinct().Count(), recurring);
    }
    public static void CaptureWorld(WorldSaveData world) => world.SceneRooms = Copy(state);
    public static void ValidateRestore(SceneRoomSaveData saved) {
        if (saved == null) return;
        if (saved.SchemaVersion != 1 || saved.ContentVersion != SceneRoomCatalog.Version)
            throw new InvalidOperationException("This room calendar requires a supported scene schema/content catalog.");
        if (saved.Bills == null || saved.Work == null || saved.Standing == null || saved.Engagements == null ||
            saved.Bills.Select(b => b.Id).Distinct().Count() != saved.Bills.Count ||
            saved.Bills.Any(b => SceneRoomCatalog.Get(b.RoomId) == null || b.Day < 0 || b.EndHour <= b.StartHour ||
                b.Appearances == null || b.Appearances.Any(s => s.PersonIds == null || s.Set == null)))
            throw new InvalidOperationException("Invalid saved room calendar.");
        if (saved.Work.Select(w => WorkKey(w.ArtistId, w.PersonId, w.Year)).Distinct().Count() != saved.Work.Count ||
            saved.Standing.Select(s => StandingKey(s.RoomId, s.ArtistId)).Distinct().Count() != saved.Standing.Count)
            throw new InvalidOperationException("Duplicate room work/standing IDs.");
    }
    public static void BeginRestore(SceneRoomSaveData saved) {
        ValidateRestore(saved); previous = state; state = Copy(saved); restoring = true; work.Clear(); standing.Clear();
    }
    public static void CompleteRestore() { restoring = false; previous = null; Reindex(); }
    public static void CancelRestore() { if (!restoring) return; state = previous; previous = null; restoring = false; Reindex(); }
    private static void Reindex() {
        work.Clear(); standing.Clear();
        if (state == null) return;
        foreach (var account in state.Work) work.Add(WorkKey(account.ArtistId, account.PersonId, account.Year), account);
        foreach (var row in state.Standing) standing.Add(StandingKey(row.RoomId, row.ArtistId), row);
    }
}
