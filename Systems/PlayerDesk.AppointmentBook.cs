using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What the book shows: one line of the Label Man's diary.</summary>
public enum BookEntryKind { SitDown, SundayDinner, PickATime, Rehearsal, Demo, Meeting, DemoJob, Studio, HoldoutSet, Sitting }

/// <summary>One entry in the appointment book. A pure read of real state: nothing here is stored apart from the things it
/// names.</summary>
public sealed class BookEntry {
    public BookEntryKind Kind;
    /// <summary>The day, and the start in minutes past midnight (null when only the day is known).</summary>
    public int Day;
    public int? Minute;
    public int Hours;
    public string Who, Place, What, ArtistId;
    /// <summary>A sit-down or a dinner today, in town, not yet started: it can be kept from the book.</summary>
    public bool CanGo;
    /// <summary>PickATime: the times their side proposed.</summary>
    public List<ApproachTime> Offered;
    public ScoutingApproachStepKind Step;
}

/// <summary>The appointment book (OnboardingTutorialDirective.md §4c, T5 step 1). A sit-down with a manager or Sunday dinner
/// with a minor's family is the first time the Label Man has somewhere to be at a certain hour. Their side proposes two
/// times; he picks one, and it goes in the book. The book is his memory and only that: it never reminds him, and nothing
/// flags a clash. At the hour he has to be in town and free. If the clock passes the start without him, he missed it.
///
/// What a miss costs: a family doesn't ask twice (a parent has to sign, and you didn't come), so the yes lapses. A manager
/// proposes two new times, and what else it costs depends on who he is (PlayerDesk.ManagerRapport): the rapport he loses,
/// and how many misses he'll stand (a shark or a svengali two, a hustler or a visionary three).
///
/// Player-side only: the steps live in the player's scouting knowledge, the times are keyed on the act and the day with no
/// RNG draw, and nothing the AI reads moves.</summary>
public partial class PlayerDesk {
    /// <summary>A manager who's been stood up this many times stops waiting.</summary>
    public const int MissesBeforeLapse = 2;
    /// <summary>Hours a manager keeps for a sit-down at his office.</summary>
    private static readonly int[] OfficeHours = { 10, 11, 14, 15, 16 };
    /// <summary>Sunday dinner, after church.</summary>
    public const int SundayDinnerHour = 13;

    private static readonly DayOfWeek[] Weekdays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    /// <summary>Two times their side proposes, both on or after <paramref name="from"/>: two Sundays at dinner for a family,
    /// two nights the room is open (before the doors) for a club booker, two weekdays at office hours for anyone else.
    /// Keyed on the act, so the same yes on the same day proposes the same times.</summary>
    internal static List<ApproachTime> ProposedTimes(ScoutingApproachStepKind kind, ManagerRead manager, string artistId, int from) {
        if (kind == ScoutingApproachStepKind.Parents || manager?.Kind == ManagerKind.Family) {
            int sunday = NextDayOn(from, DayOfWeek.Sunday);
            return new() { new() { Day = sunday, Minute = SundayDinnerHour * 60 }, new() { Day = sunday + 7, Minute = SundayDinnerHour * 60 } };
        }
        if (manager?.Kind == ManagerKind.ClubBooker && SceneRoomCatalog.Get(manager.RoomId) is { Nights.Count: > 0 } room) {
            int hour = Math.Clamp(room.OpenHour - 1, 10, 20);
            int first = NextDayOn(from, room.Nights.ToArray());
            return new() { new() { Day = first, Minute = hour * 60 }, new() { Day = NextDayOn(first + 1, room.Nights.ToArray()), Minute = hour * 60 } };
        }
        int h = StableHash((artistId ?? "") + "|" + from);
        int a = OfficeHours[h % OfficeHours.Length], b = OfficeHours[(h / OfficeHours.Length + 1 + h % OfficeHours.Length) % OfficeHours.Length];
        if (b == a) b = OfficeHours[(Array.IndexOf(OfficeHours, a) + 2) % OfficeHours.Length];
        int d1 = NextDayOn(from, Weekdays), d2 = NextDayOn(d1 + 1, Weekdays);
        return new() { new() { Day = d1, Minute = a * 60 }, new() { Day = d2, Minute = b * 60 } };
    }

    /// <summary>"3 PM", "10 AM", "1:30 PM".</summary>
    public static string ClockText(int minute) {
        int h = minute / 60 % 24, m = minute % 60;
        string ampm = h < 12 ? "AM" : "PM";
        int h12 = h % 12 == 0 ? 12 : h % 12;
        return m == 0 ? $"{h12} {ampm}" : $"{h12}:{m:00} {ampm}";
    }

    /// <summary>"Monday, Jan 4, at 3 PM".</summary>
    public static string WhenText(int day, int minute) {
        var d = LocalSceneRoomService.Date(day);
        return $"{d.DayName}, {d.ShortMonthName} {d.day}, at {ClockText(minute)}";
    }

    /// <summary>The step waits on you to pick one of their times.</summary>
    public static bool NeedsATime(ScoutingApproachStep s) => s is { Done: false, Minute: null, Offered.Count: > 0 } &&
        s.Kind is ScoutingApproachStepKind.Parents or ScoutingApproachStepKind.Manager;

    /// <summary>A visit (family or manager) that's in the book.</summary>
    public static bool Booked(ScoutingApproachStep s) => s is { Minute: not null } && s.Kind is ScoutingApproachStepKind.Parents or ScoutingApproachStepKind.Manager;

    /// <summary>The visit of an agreed act still waiting on you to pick a time (family first), or null.</summary>
    public ScoutingApproachStep StepNeedingATime(SimulatedArtist artist) =>
        Agreement(artist)?.Steps.Where(NeedsATime).OrderBy(s => s.Kind).FirstOrDefault();

    public static int VisitHours(ScoutingApproachStepKind kind) => kind == ScoutingApproachStepKind.Parents ? FamilyVisitHours : ManagerSitDownHours;


    /// <summary>Pick one of the times their side proposed. Free, and from anywhere: it goes in the book.</summary>
    public bool BookApproachStep(string artistId, ScoutingApproachStepKind kind, int choice, out string message) {
        var artist = ArtistManager.Instance?.GetArtist(artistId);
        var step = Agreement(artist)?.Steps.FirstOrDefault(s => s.Kind == kind && NeedsATime(s));
        if (step == null) { message = "There's no time waiting to be picked."; return false; }
        if (choice < 0 || choice >= step.Offered.Count) { message = "That isn't one of the times they offered."; return false; }
        var t = step.Offered[choice];
        if (t.Day < SceneDay || t.Day == SceneDay && t.Minute <= NowMinute) { message = "That time has gone."; return false; }
        step.Day = t.Day; step.Minute = t.Minute;
        message = $"In the book: {BookLine(step)}.";
        Note(message);
        Changed?.Invoke();
        return true;
    }

    /// <summary>"Sunday dinner with Ricky Sims's family, Sunday, Jan 3, at 1 PM" / "Sit-down with Sol Kramer, Monday, Jan 4,
    /// at 3 PM, at his office on Woodward".</summary>
    private static string BookLine(ScoutingApproachStep s) => s.Kind == ScoutingApproachStepKind.Parents
        ? $"Sunday dinner with {s.Who}'s {(s.Who.Contains(" and ") ? "families" : "family")}, {WhenText(s.Day ?? 0, s.Minute ?? 0)}"
        : $"sit-down with {s.Who}, {WhenText(s.Day ?? 0, s.Minute ?? 0)}, at {s.Place ?? (s.Female ? "her office" : "his office")}";

    /// <summary>The proposed times, as their side puts them: "Monday, Jan 4, at 3 PM or Tuesday, Jan 5, at 10 AM".</summary>
    public static string OfferedText(ScoutingApproachStep s) => string.Join(" or ", s.Offered.Select(t => WhenText(t.Day, t.Minute)));

    /// <summary>The clock has passed a booked start without him. Runs on every clock move (the hour hand and the dawn),
    /// so a miss is noticed when it happens, and it's idempotent: a step is only missed once per booking.</summary>
    private static string his(ScoutingApproachStep s) => s.Female ? "her" : "his";

    private void SweepMissedAppointments() {
        if (Label == null || scoutingKnowledge.Approaches == null) return;
        int today = SceneDay, now = NowMinute;
        foreach (var a in Approaches.Where(a => a.Status == ScoutingApproachStatus.Agreed).ToList()) {
            var artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
            if (artist == null || !string.IsNullOrEmpty(artist.labelId)) continue;
            foreach (var s in a.Steps.Where(s => !s.Done && Booked(s)).ToList()) {
                if (today < s.Day || today == s.Day && now <= s.Minute) continue;
                string place = s.Place ?? (s.Female ? "her office" : "his office");
                s.Missed++;
                if (s.Kind == ScoutingApproachStepKind.Parents) {
                    // A parent has to sign, and you didn't come. They don't ask twice.
                    a.Status = ScoutingApproachStatus.Lapsed; a.LetLapse = true; a.UntilDay = today;
                    bool several = s.Who.Contains(" and ");
                    Note($"{s.Who}'s {(several ? "families" : "family")} set a place for you at Sunday dinner, and you never came. A parent has to sign, " +
                        $"and that won't happen now. The yes from {a.ArtistName} is off.");
                    break;
                }
                // What it costs depends on who he is (ManagerRapport): the rapport loss, and how many he'll put up with.
                var manager = ManagerIdentity.Read(artist, today);
                var archetype = manager?.Archetype ?? artist.manager;
                AddRapport(artist, RapportSourceKind.MissedSitDown, MissPoints(archetype, s.Missed), manager);
                string he = s.Female ? "she" : "he", He = s.Female ? "She" : "He";
                if (s.Missed >= MissesBeforeLapseFor(archetype)) {
                    a.Status = ScoutingApproachStatus.Lapsed; a.LetLapse = true; a.UntilDay = today;
                    Note(archetype switch {
                        ManagerArchetype.Shark => $"{s.Who} waited at {place} again, and you never came again. {He}'s shopping {a.ArtistName} to other labels now. The yes is off.",
                        ManagerArchetype.Svengali => $"{s.Who} won't be stood up twice. {He} told {a.ArtistName} you aren't serious, and they believe {(s.Female ? "her" : "him")}. The yes is off.",
                        _ => $"{s.Who} waited at {place} again, and you never came again. The yes from {a.ArtistName} is off."
                    });
                    break;
                }
                s.Offered = ProposedTimes(s.Kind, manager, artist.artistId, today + 2);
                s.Day = s.Offered[0].Day; s.Minute = null;
                string times = $"{OfferedText(s)}. Pick one from {a.ArtistName}'s card.";
                Note(archetype switch {
                    ManagerArchetype.LocalHustler => $"{s.Who} waited at {place} and you never came. A note under the door, no hard feelings: {he}'ll see you {times}",
                    ManagerArchetype.Shark => $"{s.Who} waited at {place} and you never came. A note under the door: \"You owe me an afternoon. It'll be on the bill.\" {He}'ll see you {times}",
                    ManagerArchetype.Svengali => $"{s.Who} waited at {place} and you never came. {He} took it personally. A short note under the door: {he}'ll see you {times}",
                    ManagerArchetype.Visionary when s.Missed >= 2 => $"{s.Who} waited at {place} and you never came, again. {He}'s running out of patience with you. {He}'ll see you {times}",
                    ManagerArchetype.Visionary => $"{s.Who} waited at {place} and you never came. The note under the door is cooler than {his(s)} last: {he}'ll see you {times}",
                    _ => $"{s.Who} waited at {place} and you never came. A note under the door: {he}'ll see you {times}"
                });
            }
        }
    }

    /// <summary>The book, soonest first: the signing visits (booked, and those still waiting on a time), the rehearsals,
    /// demos and meetings arranged with acts, demo jobs, the studio date, a holdout's set, and the lien sitting.</summary>
    public List<BookEntry> AppointmentBook() {
        var list = new List<BookEntry>();
        if (Label == null) return list;
        int today = SceneDay, now = NowMinute;
        string here = LocalScenePersistenceService.SceneIdFor(CurrentCityId);
        foreach (var a in scoutingKnowledge.Approaches?.Where(a => a.Status == ScoutingApproachStatus.Agreed) ?? Enumerable.Empty<ScoutingDirectApproach>()) {
            var artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
            if (artist == null || !string.IsNullOrEmpty(artist.labelId)) continue;
            string town = LocalScenePersistenceService.SceneIdFor(artist.geography?.basePlaceId);
            foreach (var s in a.Steps.Where(s => !s.Done)) {
                if (NeedsATime(s))
                    list.Add(new BookEntry { Kind = BookEntryKind.PickATime, Day = s.Offered[0].Day, Who = s.Who, ArtistId = a.ArtistId, Step = s.Kind,
                        Offered = s.Offered, Place = s.Kind == ScoutingApproachStepKind.Parents ? "at the family's" : s.Place,
                        What = s.Kind == ScoutingApproachStepKind.Parents ? $"{a.ArtistName}: Sunday dinner with the family. Pick a time." : $"{a.ArtistName}: {s.Who} wants a sit-down. Pick a time." });
                else if (Booked(s))
                    list.Add(new BookEntry { Kind = s.Kind == ScoutingApproachStepKind.Parents ? BookEntryKind.SundayDinner : BookEntryKind.SitDown,
                        Day = s.Day ?? today, Minute = s.Minute, Hours = VisitHours(s.Kind), Who = s.Who, ArtistId = a.ArtistId, Step = s.Kind,
                        Place = s.Kind == ScoutingApproachStepKind.Parents ? $"the family's, {DistanceModel.GetCityById(town)?.name}" : s.Place ?? (s.Female ? "her office" : "his office"),
                        What = s.Kind == ScoutingApproachStepKind.Parents ? $"Sunday dinner with {s.Who}'s family ({a.ArtistName})" : $"Sit down with {s.Who} ({a.ArtistName})",
                        CanGo = s.Day == today && now <= s.Minute && (town == null || town == here) });
                else if (s.Kind == ScoutingApproachStepKind.Holdout && !s.Raised && s.Day is { } d && d >= today)
                    list.Add(new BookEntry { Kind = BookEntryKind.HoldoutSet, Day = d, Who = s.Who, ArtistId = a.ArtistId, Place = s.RoomName,
                        What = $"{a.ArtistName}'s set: {s.Who} wants you there" });
            }
        }
        foreach (var m in ScoutingMeetings.Where(m => m.Status == SceneConversationStatus.Scheduled && (m.Day > today || m.Day == today && m.StartMinute >= now)))
            list.Add(new BookEntry { Kind = m.Activity switch { ScoutingAppointmentActivity.Rehearsal => BookEntryKind.Rehearsal, ScoutingAppointmentActivity.Demo => BookEntryKind.Demo, _ => BookEntryKind.Meeting },
                Day = m.Day, Minute = m.StartMinute, Hours = Math.Max(1, (m.EndMinute - m.StartMinute) / 60), Who = m.PersonName, ArtistId = m.ArtistId,
                Place = m.Activity == ScoutingAppointmentActivity.Demo ? "your home studio" : SceneRoomCatalog.Get(m.RoomId)?.Name,
                What = m.Activity switch { ScoutingAppointmentActivity.Rehearsal => $"{m.ArtistName} rehearse for you", ScoutingAppointmentActivity.Demo => $"{m.ArtistName} put a song on tape", _ => $"Meet {m.PersonName} ({m.ArtistName})" } });
        foreach (var j in DemoJobs.Where(j => j.Status == DemoStatus.Booked && j.Day >= today))
            list.Add(new BookEntry { Kind = BookEntryKind.DemoJob, Day = j.Day, Minute = j.StartHour * 60, Hours = j.Hours, ArtistId = j.ArtistId, Place = j.RoomName,
                What = $"Demo for {j.ArtistName} (paid work)" });
        if (pendingSession != null && LocalSceneRoomService.Day(pendingSession.Date) >= today)
            list.Add(new BookEntry { Kind = BookEntryKind.Studio, Day = LocalSceneRoomService.Day(pendingSession.Date), Minute = pendingSession.StartMinute,
                Hours = pendingSession.Hours, ArtistId = pendingSession.ArtistId, Place = pendingSession.Room?.Name,
                What = $"Studio date: {ArtistManager.Instance?.GetArtist(pendingSession.ArtistId)?.stageName ?? "your act"}" });
        if (lien != null && !lien.Extended && LienProfile is { } profile)
            list.Add(new BookEntry { Kind = BookEntryKind.Sitting, Day = LocalSceneRoomService.Day(AddDays(lien.Deadline, -LienWindowDays)), Who = profile.Lawyer,
                What = $"From today, {profile.Lawyer} will sit down about the lien, any day up to {lien.Deadline.ToHeadlineString()}" });
        return list.OrderBy(e => e.Day).ThenBy(e => e.Minute ?? 24 * 60).ThenBy(e => e.What, StringComparer.Ordinal).ToList();
    }
}
