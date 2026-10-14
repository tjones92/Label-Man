using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Someone the label could hire for a date: a horn player, an arranger or a producer from the town's people.</summary>
public sealed class StudioCrewOffer {
    public StudioHireKind Kind;
    public string PersonId, Name, Text;
    public MusicianRole Role;
    public float Skill, Reading, Craft, Fee;
    public StudioBrief Leaning;
}

/// <summary>
/// Hired help beyond the act (directive §8): a horn player for a line the band can't play, an arranger for a part that
/// has to be written out, and a producer to direct the date. All of them are the town's real people (session players
/// and act members who read well and have ideas, the way Dave Appell led Cameo-Parkway's house band and arranged its
/// sessions), drawn through the same availability and payment owners as other session hires. Fees use the provisional
/// session scale; nobody is hired automatically, and hiring help never overrides the act's rights.
/// </summary>
public partial class PlayerDesk {
    /// <summary>A producer's fee for the date: a flat rate rising with their craft. Provisional authored figures.</summary>
    public static float StudioProducerFee(float craft) => Mathf.Round(40f + 60f * craft);
    public static float StudioArrangerFee(float reading) => Mathf.Round(25f + 50f * reading);
    public static float StudioHornFee(float reading) => Mathf.Round(10f + 30f * reading);
    public const int StudioHornArrivalMinutes = 30;

    private static float CrewCraft(Musician m) => .5f * m.creativity + .5f * m.sightReading;

    /// <summary>Everyone in a town who could be asked onto a session: players, horn players and singers, once each.</summary>
    private static IEnumerable<Musician> TownPeople(string cityId, int day) =>
        SessionEmploymentService.ListeningCandidates(cityId, day).Concat(SessionEmploymentService.HornCandidates(cityId, day))
            .Concat(SessionEmploymentService.SingerCandidates(cityId, day))
            .GroupBy(m => m.personId, StringComparer.Ordinal).Select(g => g.First());

    private IEnumerable<Musician> FreeTownPeople(string cityId, SimulatedArtist act, GameDate date, int start, int end, IEnumerable<string> exclude) {
        int day = LocalSceneRoomService.Day(date);
        var taken = (exclude ?? Enumerable.Empty<string>()).ToHashSet();
        return TownPeople(cityId, day).Where(m => !act.members.Any(x => x.personId == m.personId) && !taken.Contains(m.personId) &&
            !LocalSceneRoomService.HasPersonBooking(m.personId, day, start, end, null));
    }

    /// <summary>The producer the label could bring to a date in this town: the best free person who reads and has ideas.
    /// Their leaning comes from who they are: an ideas person likes to explore, a strict reader likes a tight single.</summary>
    public StudioCrewOffer StudioProducerOffer(string cityId, SimulatedArtist act, GameDate date, int start, int end, IEnumerable<string> exclude = null) {
        if (!StudioExperience.Enabled || act == null) return null;
        var person = FreeTownPeople(cityId, act, date, start, end, exclude).Where(m => m.creativity >= .55f && m.sightReading >= .60f)
            .OrderByDescending(m => CrewCraft(m) + SessionEmploymentService.RegularScore(Label.labelId, m.personId)).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if (person == null) return null;
        float craft = Math.Clamp(CrewCraft(person), 0, 1);
        var leaning = person.creativity > .70f ? StudioBrief.Explore : person.sightReading > .75f ? StudioBrief.TightenSingle : StudioBrief.CaptureLiveAct;
        return new StudioCrewOffer {
            Kind = StudioHireKind.Producer, PersonId = person.personId, Name = person.FullName, Role = person.primaryRole, Skill = person.technicalSkill,
            Reading = person.sightReading, Craft = craft, Fee = StudioProducerFee(craft), Leaning = leaning,
            Text = $"{person.FullName} ({RoleWord(person.primaryRole).ToLowerInvariant()}, produces sessions in town) would direct the date for ${StudioProducerFee(craft):N0}. " +
                $"Leans toward {(leaning == StudioBrief.Explore ? "trying ideas" : leaning == StudioBrief.TightenSingle ? "a tight single" : "capturing the act as it plays")}."
        };
    }

    /// <summary>An arranger free today in the room's town, for parts that must be written out. Null for nobody.</summary>
    public StudioCrewOffer StudioArrangerOffer() {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Room == null || session.Hires.Any(h => h.Kind == StudioHireKind.Arranger)) return null;
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        int now = (TimeManager.Instance?.CurrentHour ?? 9) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
        var person = FreeTownPeople(session.Room.CityId, act, StudioToday, now, StudioDateEnd(session), session.Hires.Select(h => h.PersonId))
            .Where(m => m.sightReading >= .65f && m.creativity >= .55f).OrderByDescending(m => m.sightReading + m.creativity).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if (person == null) return null;
        return new StudioCrewOffer { Kind = StudioHireKind.Arranger, PersonId = person.personId, Name = person.FullName, Role = person.primaryRole,
            Skill = person.technicalSkill, Reading = person.sightReading, Craft = CrewCraft(person), Fee = StudioArrangerFee(person.sightReading),
            Text = $"{person.FullName} writes parts for sessions in town: ${StudioArrangerFee(person.sightReading):N0} to write and conduct it today." };
    }

    /// <summary>A horn player free today in the room's town. Null for nobody.</summary>
    public StudioCrewOffer StudioHornOffer() {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Room == null || session.Hires.Any(h => h.Kind == StudioHireKind.Horn)) return null;
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        int day = LocalSceneRoomService.Day(StudioToday), now = (TimeManager.Instance?.CurrentHour ?? 9) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
        var person = SessionEmploymentService.HornCandidates(session.Room.CityId, day)
            .Where(m => !act.members.Any(x => x.personId == m.personId) && session.Hires.All(h => h.PersonId != m.personId) &&
                !LocalSceneRoomService.HasPersonBooking(m.personId, day, now, StudioDateEnd(session), null))
            .OrderByDescending(m => m.sightReading + m.technicalSkill + SessionEmploymentService.RegularScore(Label.labelId, m.personId)).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if (person == null) return null;
        var role = MemberRoleService.Plays(person, MusicianRole.Saxophone) ? MusicianRole.Saxophone : MusicianRole.Trumpet;
        return new StudioCrewOffer { Kind = StudioHireKind.Horn, PersonId = person.personId, Name = person.FullName, Role = role,
            Skill = person.technicalSkill, Reading = person.sightReading, Fee = StudioHornFee(person.sightReading),
            Text = $"{person.FullName} plays {RoleWord(role)} on sessions in town: ${StudioHornFee(person.sightReading):N0}, here in {StudioHornArrivalMinutes} minutes." };
    }

    /// <summary>Hires a horn player or arranger onto the date now. Paid now, not recouped (like other session fees).</summary>
    public bool HireStudioCrew(StudioHireKind kind, out string message) {
        var session = pendingSession;
        var offer = kind == StudioHireKind.Horn ? StudioHornOffer() : kind == StudioHireKind.Arranger ? StudioArrangerOffer() : null;
        if (session == null || offer == null) { message = "Nobody suitable is free in town today."; return false; }
        if (!StudioRulesV2(session)) { message = RulesV2Needed; return false; }
        if (Label.cashReserves < offer.Fee) { message = $"{offer.Name} costs ${offer.Fee:N0}."; return false; }
        int wait = kind == StudioHireKind.Horn ? StudioHornArrivalMinutes : 15;
        if (!SpendStudioWork(session, wait, kind == StudioHireKind.Horn ? $"Waited for {offer.Name} ({RoleWord(offer.Role)})" : $"{offer.Name} wrote out the part", out message, false)) return false;
        SeatCrew(session, offer);
        message = kind == StudioHireKind.Horn ? $"{offer.Name} is here with a {RoleWord(offer.Role)}." : $"{offer.Name} has the part written out.";
        Changed?.Invoke(); return true;
    }

    private void SeatCrew(PendingSession session, StudioCrewOffer offer) {
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        Label.cashReserves -= offer.Fee; Label.monthlyExpenses += offer.Fee;
        session.Hires.Add(new StudioSessionHire { Kind = offer.Kind, PersonId = offer.PersonId, Name = offer.Name, Role = offer.Role,
            Skill = offer.Kind == StudioHireKind.Producer ? offer.Craft : offer.Skill, Reading = offer.Reading, Fee = offer.Fee });
        session.Cost += offer.Fee;
        var town = TownPeople(session.Room.CityId, LocalSceneRoomService.Day(session.Date)).FirstOrDefault(m => m.personId == offer.PersonId);
        if (town != null) SessionEmploymentService.OnPlayerSession(town, Label.labelId, artist, offer.Fee);
        session.ActionLog.Add(offer.Kind switch {
            StudioHireKind.Producer => $"Producer: {offer.Name} (${offer.Fee:N0}).",
            StudioHireKind.Arranger => $"Arranger: {offer.Name} (${offer.Fee:N0}).",
            _ => $"Horn: {offer.Name} on {RoleWord(offer.Role)} (${offer.Fee:N0})."
        });
    }

    /// <summary>The end of today's booked window, in minutes from midnight.</summary>
    private int StudioDateEnd(PendingSession session) => StudioDayStart(session) + session.Hours * 60;
}
