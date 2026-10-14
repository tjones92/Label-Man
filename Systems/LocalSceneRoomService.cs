using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>World-owned bookings and performance facts. No births, roster/activation writes, global draws,
/// national cash transfers or extra development rewards. Calendar reads return detached snapshots.</summary>
public static partial class LocalSceneRoomService {
    private static SceneRoomSaveData state;
    private static SceneRoomSaveData previous;
    private static bool restoring;
    private static readonly Dictionary<string, SceneWorkAccount> work = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SceneRoomStanding> standing = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, SceneHouseBandStanding> houseStanding = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, (float Hours, float Pay)> backing = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> actShows = new(StringComparer.Ordinal);
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
    // Floor, so a pre-rolled day before the start belongs to the term that ends at the start, not to term 0.
    private static int Term(int day) => day >= 0 ? day / 91 : (day - 90) / 91;
    /// <param name="asOfDay">Pre-roll only: book the past with the world as it stands on this day (see Prewarm).</param>
    private static void Schedule(int week, int earliestDay, int? asOfDay = null) {
        bool cacheReads = StartupCaches.Enabled;
        var billed = cacheReads ? State.Bills.Select(b => (b.RoomId, b.Day)).ToHashSet() : null;
        var lineups = new Dictionary<string, string[]>(StringComparer.Ordinal);
        string[] PeopleFor(SimulatedArtist act) {
            if (!cacheReads) return StagePeople(act);
            if (!lineups.TryGetValue(act.artistId, out var people)) lineups.Add(act.artistId, people = StagePeople(act));
            return people;
        }
        var reservations = new HashSet<string>(StringComparer.Ordinal);
        var personPlaces = new Dictionary<string, string>(StringComparer.Ordinal);
        var actPlaces = new Dictionary<string, string>(StringComparer.Ordinal);
        // Each person's and act's sets by day, for a second room (WorkingWeek). Only kept when double rooms are on.
        var actSets = new Dictionary<string, List<DaySet>>(StringComparer.Ordinal);
        var personSets = new Dictionary<string, List<DaySet>>(StringComparer.Ordinal);
        bool doubles = LocalScenes.DoubleRooms, workingWeek = LocalScenes.DayJobs || doubles;
        void Track(SceneBill b, SceneAppearance slot) {
            AddDaySet(actSets, $"{b.Day}|{slot.ArtistId}", new(b.RoomId, slot.ArtistId, slot.StartHour, slot.EndHour, false));
            foreach (string person in slot.PersonIds) AddDaySet(personSets, $"{b.Day}|{person}", new(b.RoomId, slot.ArtistId, slot.StartHour, slot.EndHour, false));
            foreach (string person in slot.BackingPersonIds ?? new()) AddDaySet(personSets, $"{b.Day}|{person}", new(b.RoomId, slot.ArtistId, slot.StartHour, slot.EndHour, true));
        }
        foreach (var bill in State.Bills) foreach (var slot in bill.Appearances) {
            foreach (string person in slot.PersonIds.Concat(slot.BackingPersonIds ?? new())) {
                personPlaces[$"{bill.Day}|{person}"] = bill.RoomId;
                for (int h = slot.StartHour; h < slot.EndHour; h++) reservations.Add($"{bill.Day}|{h}|{person}");
            }
            actPlaces[$"{bill.Day}|{slot.ArtistId}"] = bill.RoomId;
            if (doubles) Track(bill, slot);
        }
        for (int day = Math.Max(week * 7, earliestDay); day < week * 7 + 7 && Date(day) <= GameDate.EndDate; day++) {
            GameDate date = Date(day);
            // Acts holding a standing engagement today: full-time by that alone (WorkingWeek).
            var engaged = workingWeek ? State.Engagements.Where(e => e.StartDay <= day && e.ThroughDay >= day)
                .Select(e => e.ArtistId).ToHashSet(StringComparer.Ordinal) : null;
            foreach (var room in SceneRoomCatalog.All.Where(r => SceneLiveEconomics.OpenOn(r, date.DayOfWeek))) {
                if (cacheReads ? billed.Contains((room.Id, day)) : State.Bills.Any(b => b.RoomId == room.Id && b.Day == day)) continue;
                string id = $"{room.Id}:{day}";
                int slots = SceneEcosystemService.Slots(room, day);
                // A funded program's extra set runs past the room's usual close.
                var bill = new SceneBill { Id = id, RoomId = room.Id, Day = day, Year = date.year,
                    StartHour = room.OpenHour, EndHour = Math.Max(room.CloseHour, room.OpenHour + slots) };
                var members = asOfDay.HasValue ? EligibleBefore(room, day, asOfDay.Value) : LocalScenePersistenceService.MembersOf(room.PlaceId)
                    .Select(ArtistManager.Instance.GetArtist).Where(a => Eligible(a, room, day)).ToArray();
                int term = Term(day);
                var regularIds = State.Engagements.Where(e => e.RoomId == room.Id && e.StartDay <= day && e.ThroughDay >= day).Select(e => e.ArtistId).ToHashSet(StringComparer.Ordinal);
                bool rotation = LocalScenes.RoomRotation;
                if (rotation) OfferEngagements(room, day, members, regularIds, asOfDay.HasValue);
                engaged?.UnionWith(regularIds);
                var candidates = rotation ? NightOrder(room, day, members, regularIds, asOfDay.HasValue) : members.OrderBy(a => regularIds.Contains(a.artistId) ? 0 : 1)
                    .ThenBy(a => Draw($"engagement|{room.Id}|{term}|{a.artistId}"))
                    .ThenBy(a => a.artistId, StringComparer.Ordinal);
                if (HasBookings) candidates = WithBookedActs(room, day, candidates, regularIds);   // early-game §6.3; never in AI-only worlds
                foreach (var act in candidates) {
                    if (bill.Appearances.Count == slots) break;
                    bool fullTime = workingWeek && FullTime(act, engaged);
                    // A day-job act plays weekends (WorkingWeek).
                    if (!FitsWorkingWeek(act, room, day, fullTime)) continue;
                    string[] people = PeopleFor(act);
                    // Rotation books the top of the bill first, so it fills the night from the closing set back.
                    int hour = rotation ? room.OpenHour + slots - 1 - bill.Appearances.Count : room.OpenHour + bill.Appearances.Count;
                    // No cross-city teleporting or shared-musician overlap. Without double rooms a performer's whole
                    // day stays in one room; with them, a full-time act may cross town to one more room (WorkingWeek).
                    if (doubles ? people.Any(p => reservations.Contains($"{day}|{hour}|{p}")) ||
                            !DayAllows(act, room, day, hour, people, fullTime, actSets, personSets)
                        : actPlaces.TryGetValue($"{day}|{act.artistId}", out var ar) && ar != room.Id ||
                        people.Any(p => reservations.Contains($"{day}|{hour}|{p}") ||
                            personPlaces.TryGetValue($"{day}|{p}", out var pr) && pr != room.Id)) continue;
                    // Rotation: only an earned engagement makes a regular (OfferEngagements); otherwise booking confers one.
                    bool regular = rotation ? regularIds.Contains(act.artistId) : Regular(act, room);
                    if (!rotation && regular && !regularIds.Contains(act.artistId))
                        State.Engagements.Add(new() { RoomId = room.Id, ArtistId = act.artistId, StartDay = day, ThroughDay = (term + 1) * 91 - 1 });
                    var backers = SceneHouseBandService.Needs(act, room)
                        ? SceneHouseBandService.Pick(room, act, day, hour, people, personPlaces, reservations, HouseShows) : null;
                    bill.Appearances.Add(new() { ArtistId = act.artistId, PersonIds = people.ToList(), StartHour = hour, EndHour = hour + 1,
                        Role = bill.Appearances.Count == 0 ? "opening set" : "closing set", Residency = regular,
                        Set = asOfDay.HasValue ? new() : SetFor(act, id, date.year), BackingPersonIds = backers?.Count > 0 ? backers : null });
                    actPlaces[$"{day}|{act.artistId}"] = room.Id;
                    foreach (string person in people.Concat(backers ?? new())) { reservations.Add($"{day}|{hour}|{person}"); personPlaces[$"{day}|{person}"] = room.Id; }
                    // A player crossing town to a second room is nobody's house band today: backers stay in one room.
                    if (doubles && actSets.TryGetValue($"{day}|{act.artistId}", out var earlier) && earlier.Any(x => x.RoomId != room.Id))
                        foreach (string person in people) personPlaces[$"{day}|{person}"] = SecondRoom;
                }
                if (rotation) {
                    // Filled from the top of the bill back: close any gap at the start, then list in running order.
                    int shift = slots - bill.Appearances.Count;
                    foreach (var slot in bill.Appearances) { slot.StartHour -= shift; slot.EndHour -= shift; }
                    bill.Appearances = bill.Appearances.OrderBy(a => a.StartHour).ToList();
                    for (int i = 0; i < bill.Appearances.Count; i++)
                        bill.Appearances[i].Role = i == 0 ? "opening set" : i == bill.Appearances.Count - 1 ? "closing set" : "middle set";
                }
                else if (bill.Appearances.Count == 3) bill.Appearances[1].Role = "middle set";
                // Final hours, after the running order closes up: what a second room's travel gap is measured against.
                if (doubles) foreach (var slot in bill.Appearances) Track(bill, slot);
                // Live shows only: pre-rolled history (PriorShows) is not a live draw.
                int prior = bill.Appearances.Sum(a => standing.TryGetValue(StandingKey(room.Id, a.ArtistId), out var s) ? Math.Min(12, s.Shows) : 0);
                bill.Capacity = SceneLiveEconomics.Capacity(room);
                // The room's crowd, not a crowd per act: past two acts, more sets do not draw more people (keeps the
                // live-cal-v1 fill), and the acts' standing counts as their average, scaled to two acts.
                int drawing = rotation ? Math.Min(2, bill.Appearances.Count) : bill.Appearances.Count;
                double standingDraw = rotation && bill.Appearances.Count > 0 ? .01 * prior * drawing / bill.Appearances.Count : .01 * prior;
                bill.ExpectedAudience = bill.Appearances.Count == 0 ? 0 : Math.Min(bill.Capacity,
                    (int)(bill.Capacity * (.30 + .15 * drawing + standingDraw)));
                if (HasBookings) PlaceBookings(bill, room);
                State.Bills.Add(bill);
                billed?.Add((room.Id, day));
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
        if (artist.songwritingAbility > .3f) songs.Insert(0, new() { Title = OriginalTitle(artist, year), Genre = artist.primaryGenre, Original = true });
        return songs.Take(3).ToList();
    }
    /// <summary>The title of a scene act's own unrecorded song: keyed on the act, the year and its place in the act's own
    /// material, so every bill and every read names the same song the same way, and drawing it moves no title stream.</summary>
    internal static string OriginalTitle(SimulatedArtist artist, int year, int index = 0) =>
        NameGenerator.Instance?.KeyedSongTitle(artist.primaryGenre, year, $"scene-original|{artist.artistId}|{year}|{index}") ?? "Their own song";
    /// <summary>Called by the clock, independently of visits. Resolve each shared fact exactly once.</summary>
    public static void Advance(GameDate date, int hour) {
        if (!LocalScenes.Rooms || restoring) return;
        SceneEcosystemService.Advance(date);
        int day = Day(date);
        AdvanceConversations(day, hour * 60 + (hour == TimeManager.Instance?.CurrentHour ? TimeManager.Instance.CurrentMinute : 0));
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
    private static SceneWorkAccount Account(string artist, string person, int year, bool backed = false) {
        string key = WorkKey(artist, person, year);
        if (work.TryGetValue(key, out var account)) return account;
        account = new() { ArtistId = artist, PersonId = person, Year = year, Backing = backed };
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
                if (HasBookings) SettleBooking(bill, slot);
                if (available && LocalScenes.ScoutingExperience) slot.Performance ??= ScenePerformanceService.Resolve(bill, slot, act);
                if (!available || !room.IsPerformance) continue;
                string key = StandingKey(room.Id, slot.ArtistId);
                if (!standing.TryGetValue(key, out var rank)) {
                    rank = new() { RoomId = room.Id, ArtistId = slot.ArtistId };
                    standing.Add(key, rank); State.Standing.Add(rank);
                }
                rank.Shows++; rank.LastDay = bill.Day;
                if (slot.Performance != null && slot.Performance.Audience == null) AttributeSetAudience(bill, slot, act, rank);
                actShows[slot.ArtistId] = ActShows(slot.ArtistId) + 1;
                foreach (string person in slot.PersonIds) Account(slot.ArtistId, person, bill.Year).StageHours += slot.EndHour - slot.StartHour;
                slot.BackingPersonIds?.RemoveAll(p => ArtistManager.Instance.GetMusician(p)?.lifeState != MemberLifeState.Active);
                foreach (string person in slot.BackingPersonIds ?? new()) {
                    var account = Account(slot.ArtistId, person, bill.Year, backed: true);
                    account.StageHours += slot.EndHour - slot.StartHour;
                    AddBacking(person, bill.Year, slot.EndHour - slot.StartHour, 0f);
                    string hk = room.Id + "|" + person;
                    if (!houseStanding.TryGetValue(hk, out var hs)) { houseStanding[hk] = hs = new() { RoomId = room.Id, PersonId = person }; State.HouseStanding.Add(hs); }
                    hs.Shows++; hs.LastDay = bill.Day;
                    foreach (string other in slot.PersonIds.Concat(slot.BackingPersonIds).Where(o => o != person))
                        ContactNetworkService.Link(person, other, ContactKind.HouseBand, bill.Year, via: slot.ArtistId);
                }
                RepertoireInfluenceService.OnBacked(slot.BackingPersonIds, slot.Set, slot.ArtistId, bill.Year);
                State.CompletedPerformances++;
            }
            if (bill.Day == day && bill.EndHour > hour) continue;
            var performed = bill.Appearances.Where(a => a.Status == SceneBillStatus.Performed).ToArray();
            bill.Status = performed.Length > 0 ? SceneBillStatus.Performed : SceneBillStatus.Cancelled;
            int capacity = CapacityOf(bill);
            bill.Attendance = performed.Length == 0 ? 0 : Math.Min(capacity,
                (int)(bill.ExpectedAudience * performed.Length / Math.Max(1f, bill.Appearances.Count) * (.8 + .35 * Draw("attendance|" + bill.Id))));
            bill.GrossReceipts = room.IsPerformance ? bill.Attendance * SceneLiveEconomics.Admission(room, bill.Year) : 0;
            bill.Response = performed.Length == 0 ? "No set went ahead." : bill.Attendance >= capacity * .6
                ? "A busy room stayed for the sets." : "A small audience stayed for the sets.";
            foreach (var slot in performed) {
                if (!room.IsPerformance) continue;
                slot.Fee = SceneLiveEconomics.SlotFee(room, bill, slot, performed.Length);
                int band = slot.PersonIds.Count + (slot.BackingPersonIds?.Count ?? 0);
                foreach (string person in slot.PersonIds) Account(slot.ArtistId, person, bill.Year).FeeShare += slot.Fee / band;
                foreach (string person in slot.BackingPersonIds ?? new()) {
                    Account(slot.ArtistId, person, bill.Year, backed: true).FeeShare += slot.Fee / band;
                    AddBacking(person, bill.Year, 0f, slot.Fee / band);
                }
            }
            RecordRoomYear(room, bill, performed);
        }
    }
    /// <summary>Attribute real work inside the existing annual allowance. The band owner alone applies
    /// fatigue/growth. Excess observed work is exposed for later calibration, never silently added twice.</summary>
    /// <summary>A bill's physical bound: the capacity recorded when it was made, or the room's on an older save.</summary>
    public static int CapacityOf(SceneBill bill) => bill == null ? 0 : bill.Capacity > 0 ? bill.Capacity
        : SceneRoomCatalog.Get(bill.RoomId)?.Capacity ?? 0;

    /// <summary>Diagnostics only (the live-calibration audit): one row per room and year. Not saved, so a resumed
    /// world reports from the resume onward.</summary>
    public sealed class RoomYear {
        public string RoomId; public int Year, Capacity, Bills, Sets, Attendance, PlayerNights;
        public float Admission, Receipts, ActPay;
    }
    private static readonly Dictionary<string, RoomYear> roomYears = new(StringComparer.Ordinal);
    public static IReadOnlyCollection<RoomYear> RoomYears => roomYears.Values;
    private static void RecordRoomYear(SceneRoomProfile room, SceneBill bill, SceneAppearance[] performed) {
        if (room == null || !room.IsPerformance || performed.Length == 0) return;
        string key = room.Id + "|" + bill.Year;
        if (!roomYears.TryGetValue(key, out var row)) roomYears[key] = row = new() { RoomId = room.Id, Year = bill.Year,
            Capacity = CapacityOf(bill), Admission = SceneLiveEconomics.Admission(room, bill.Year) };
        row.Bills++; row.Sets += performed.Length; row.Attendance += bill.Attendance; row.Receipts += bill.GrossReceipts;
        foreach (var slot in performed) { row.ActPay += slot.Fee; row.PlayerNights += slot.PersonIds.Count + (slot.BackingPersonIds?.Count ?? 0); }
    }

    /// <summary>Who came for one set, resolved once when it ends (Phase 5). The crowd is the bill's expected audience at the
    /// same keyed draw its attendance uses; it splits the way the expected audience was built -- the room's own .30 nobody
    /// is credited with, and each act's billing share plus its standing in this room -- so a packed room never reads as one
    /// act's following. How much of the room stays reads the set's stage communication; requests need nights here before.
    /// Keyed draws only; nothing here moves fees, work, attendance or development.</summary>
    private static void AttributeSetAudience(SceneBill bill, SceneAppearance slot, SimulatedArtist act, SceneRoomStanding rank) {
        bool rotation = LocalScenes.RoomRotation;
        int n = Math.Max(1, bill.Appearances.Count);
        int drawing = rotation ? Math.Min(2, n) : n;
        double scale = rotation ? (double)drawing / n : 1d;
        // Standing before this bill: a slot already resolved in this bill has been counted once.
        int Prior(SceneAppearance a) => standing.TryGetValue(StandingKey(bill.RoomId, a.ArtistId), out var s)
            ? Math.Min(12, Math.Max(0, s.Shows - (a.Status == SceneBillStatus.Performed ? 1 : 0))) : 0;
        double Share(SceneAppearance a) => scale * (.15 + .01 * Prior(a));
        double total = .30 + bill.Appearances.Sum(Share);
        int capacity = CapacityOf(bill);
        int present = Math.Min(capacity, (int)(bill.ExpectedAudience * (.8 + .35 * Draw("attendance|" + bill.Id))));
        string id = slot.Performance.Id;
        int arrived = Math.Min(present, (int)Math.Round(present * Share(slot) / total * (.75 + .5 * Draw("set-arrivals|" + id))));
        float stayed = Math.Clamp(.35f + .45f * slot.Performance.Communication + .2f * slot.Performance.Execution + ((float)Draw("set-stayed|" + id) - .5f) * .16f, .15f, 1f);
        int prior = Prior(slot);
        int requests = prior < 3 ? 0 : (int)(arrived * Math.Min(1d, prior / 12d) * .12 * (.5 + Draw("set-requests|" + id)));
        slot.Performance.Audience = new() { Present = present, ArrivedFor = arrived, StayedShare = stayed, Requests = requests };
        rank.DrawSets++; rank.DrawTotal += arrived; rank.StayedTotal += stayed;
        // A bounded, once-per-set contribution to the act's public recognition through its owner (diminishing returns
        // there). A strong local draw is worth about a chart entry a year; a room's regulars are worth nothing.
        if (LocalScenes.LiveDrawRecognition)
            ArtistRecognitionService.AddPublicRecognition(act, LiveDrawRecognitionPerHead * Math.Min(arrived, LiveDrawRecognitionHeadCap));
    }
    /// <summary>Recognition per person who came in for a set, and the most heads one set can count.</summary>
    public const float LiveDrawRecognitionPerHead = 2e-6f;
    public const int LiveDrawRecognitionHeadCap = 60;

    /// <summary>The room's record of an act's draw here: nights attributed, average who came in for them, average share that
    /// stayed. Null before any attributed night.</summary>
    public static (int Nights, int AverageArrivals, float AverageStayed)? DrawAt(string roomId, string artistId) =>
        standing.TryGetValue(StandingKey(roomId, artistId), out var s) && s.DrawSets > 0
            ? (s.DrawSets, (int)Math.Round((double)s.DrawTotal / s.DrawSets), s.StayedTotal / s.DrawSets) : null;

    /// <summary>A person's realized stage hours and room pay for a year (zero without an account).</summary>
    public static (float StageHours, float Pay) Realized(string artist, string person, int year) =>
        work.TryGetValue(WorkKey(artist, person, year), out var account) ? (account.StageHours, account.FeeShare) : (0f, 0f);

    private static int HouseShows(string room, string person) => houseStanding.TryGetValue(room + "|" + person, out var s) ? s.Shows : 0;
    private static void AddBacking(string person, int year, float hours, float pay) {
        string key = year + "|" + person;
        var (h, p) = backing.GetValueOrDefault(key);
        backing[key] = (h + hours, p + pay);
    }
    /// <summary>A person's house-band work for a year, across every act they backed.</summary>
    public static (float Hours, float Pay) Backing(string person, int year) => person == null ? (0f, 0f) : backing.GetValueOrDefault(year + "|" + person);
    /// <summary>Everyone who played house-band sets in a year, in person-id order.</summary>
    public static IEnumerable<string> BackersIn(int year) {
        string prefix = year + "|";
        return backing.Where(kv => kv.Value.Hours > 0f && kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kv => kv.Key.Substring(prefix.Length)).OrderBy(p => p, StringComparer.Ordinal);
    }
    public static IReadOnlyCollection<SceneHouseBandStanding> HouseStanding => houseStanding.Values;

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
        return AnnouncedCalendar(placeId, date);
    }
    // Planning reads only the calendar already posted by the world clock.
    public static IReadOnlyList<SceneBill> AnnouncedCalendar(string placeId, GameDate date) {
        if (!LocalScenes.Rooms || state == null) return Array.Empty<SceneBill>();
        string scene = LocalScenePersistenceService.SceneIdFor(placeId);
        return Array.AsReadOnly(Copy(state.Bills.Where(b => SceneRoomCatalog.Get(b.RoomId)?.PlaceId == scene && b.Day >= Day(date)).ToArray()));
    }
    public static SceneBill CurrentBill(string roomId, GameDate date, int hour) {
        if (!LocalScenes.Rooms) return null;
        EnsureCalendar(date);
        return Copy(State.Bills.FirstOrDefault(b => b.RoomId == roomId && b.Day == Day(date) && b.StartHour <= hour && b.EndHour >= hour + PlayerDesk.ScoutHours && b.Status == SceneBillStatus.Scheduled));
    }
    /// <summary>Every appearance the room remembers for the act, including pre-rolled history.</summary>
    public static int ShowsAt(string roomId, string artistId) => standing.GetValueOrDefault(StandingKey(roomId, artistId)) is { } s ? s.Shows + s.PriorShows : 0;
    // A source may consult retained past bills, unlike the forward-only itinerary.
    public static SceneBill LastCompletedBill(string roomId, string artistId, int day, int minute) => Copy(State.Bills
        .Where(b => b.RoomId == roomId && b.Status == SceneBillStatus.Performed && b.Capacity > 0 &&
            (b.Day < day || b.Day == day && b.EndHour * 60 <= minute) &&
            b.Appearances.Any(a => a.ArtistId == artistId && a.Status == SceneBillStatus.Performed))
        .OrderByDescending(b => b.Day).ThenByDescending(b => b.EndHour).ThenBy(b => b.Id, StringComparer.Ordinal).FirstOrDefault());
    public static SceneBill Bill(string id) => Copy(state?.Bills.FirstOrDefault(b => b.Id == id));
    public static IReadOnlyList<SimulatedArtist> Hear(SceneBill bill, int? fromHour = null, int? throughHour = null) {
        if (!LocalScenes.Rooms || bill == null) return Array.Empty<SimulatedArtist>();
        if ((throughHour ?? bill.EndHour) <= (fromHour ?? bill.StartHour)) return Array.Empty<SimulatedArtist>();
        var canonical = State.Bills.FirstOrDefault(b => b.Id == bill.Id);
        var room = SceneRoomCatalog.Get(canonical?.RoomId);
        return Array.AsReadOnly((canonical?.Appearances ?? new()).Where(s => s.Status != SceneBillStatus.Cancelled && s.EndHour > (fromHour ?? bill.StartHour) && s.StartHour < (throughHour ?? bill.EndHour) && StagePeople(ArtistManager.Instance.GetArtist(s.ArtistId) ?? new SimulatedArtist()).SequenceEqual(s.PersonIds))
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
                && SceneLiveEconomics.OpenOn(r, date.DayOfWeek) ? SceneEcosystemService.Slots(r, day) : 0;
        }));
        var booked = (state?.Bills ?? new()).Where(b => roomIds.Contains(b.RoomId) && b.Day / 7 == week)
            .SelectMany(b => b.Appearances).Where(a => a.Status != SceneBillStatus.Cancelled).ToArray();
        int recurring = (state?.Engagements ?? new()).Where(e => roomIds.Contains(e.RoomId)
            && e.StartDay <= week * 7 + 6 && e.ThroughDay >= week * 7).Select(e => e.ArtistId).Distinct().Count();
        return (slots, booked.Length, booked.Select(a => a.ArtistId).Distinct().Count(), recurring);
    }
    // Room scheduling conservatively keeps a person in one room per day until travel is modeled.
    public static bool HasPersonBooking(string personId, int day, int startMinute, int endMinute, string roomId) =>
        HasPerformanceBooking(personId, day, startMinute, endMinute, roomId) || ConversationBooking(personId, day, startMinute, endMinute, roomId);
    private static bool HasPerformanceBooking(string personId, int day, int startMinute, int endMinute, string roomId) =>
        (state?.Bills ?? new()).Where(b => b.Day == day && b.Status != SceneBillStatus.Cancelled)
            .Any(b => b.Appearances.Any(a => a.Status != SceneBillStatus.Cancelled &&
                (b.RoomId != roomId || a.StartHour * 60 < endMinute && a.EndHour * 60 > startMinute) &&
                (a.PersonIds.Contains(personId) || (a.BackingPersonIds?.Contains(personId) ?? false))));
    public static void CaptureWorld(WorldSaveData world) => world.SceneRooms = Copy(state);
    public static void ValidateRestore(SceneRoomSaveData saved) {
        if (saved == null) return;
        ValidateConversations(saved);
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
        if (saved.PriorWeeks < 0 || saved.Standing.Any(s => s == null || s.Shows < 0 || s.PriorShows < 0))
            throw new InvalidOperationException("Invalid saved room standing.");
    }
    public static void BeginRestore(SceneRoomSaveData saved) {
        ValidateRestore(saved); previous = state; state = Copy(saved); if (state != null) state.Conversations ??= new(); restoring = true; work.Clear(); standing.Clear(); houseStanding.Clear(); backing.Clear(); actShows.Clear();
    }
    public static void CompleteRestore() { restoring = false; previous = null; Reindex(); }
    public static void CancelRestore() { if (!restoring) return; state = previous; previous = null; restoring = false; Reindex(); }
    private static void Reindex() {
        work.Clear(); standing.Clear(); houseStanding.Clear(); backing.Clear(); actShows.Clear();
        if (state == null) return;
        state.HouseStanding ??= new();
        foreach (var account in state.Work) {
            work.Add(WorkKey(account.ArtistId, account.PersonId, account.Year), account);
            if (account.Backing) AddBacking(account.PersonId, account.Year, account.StageHours, account.FeeShare);
        }
        foreach (var row in state.Standing) { standing.Add(StandingKey(row.RoomId, row.ArtistId), row); actShows[row.ArtistId] = ActShows(row.ArtistId) + row.Shows; }
        foreach (var row in state.HouseStanding) houseStanding[row.RoomId + "|" + row.PersonId] = row;
    }
}
