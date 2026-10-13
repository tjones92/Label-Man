using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Why an outsider is on the date: a session player the label chose in place of a member, a specialist room's
/// house player the owner insisted on (in place of a member, or filling a part a singer has no one for), or one voice of
/// a hired backing group. Older saves hold only replacements.</summary>
public enum StudioHireKind { Replacement, HousePlayer, BackingVoice }

/// <summary>A named session musician on a studio date, in place of one member. Credited on every master whose kept take
/// they played on; a take cut before they arrived is the band's own performance.</summary>
public sealed class StudioSessionHire {
    public string PersonId, Name, ReplacesPersonId, ReplacesName;
    public MusicianRole Role;
    public float Skill, Reading, Fee, Lift, EnsembleLift;
    public bool CalledIn;   // called in on the day, rather than booked ahead in the Band Room
    // v20: house players and backing voices. A backing voice belongs to a named group and replaces nobody.
    public StudioHireKind Kind;
    public string GroupName;
    public bool Replaces => !string.IsNullOrEmpty(ReplacesPersonId);
    public StudioSessionHire Copy() => (StudioSessionHire)MemberwiseClone();
    public bool Valid => !string.IsNullOrEmpty(PersonId) && !string.IsNullOrEmpty(Name) && Enum.IsDefined(Kind) && Enum.IsDefined(Role) &&
        (Kind != StudioHireKind.Replacement || Replaces) && (Kind != StudioHireKind.BackingVoice || !Replaces && !string.IsNullOrEmpty(GroupName) && Lift == 0 && EnsembleLift == 0) &&
        new[] { Skill, Reading, Fee, Lift, EnsembleLift }.All(float.IsFinite) && Fee >= 0 && Lift is >= 0 and <= .10f && EnsembleLift is >= 0 and <= .06f;
}

/// <summary>The producer's suggestion to call a session player in for a member whose part keeps falling apart.</summary>
public sealed class StudioCallInProposal {
    public string MemberId, MemberName, CandidateId, CandidateName;
    public MusicianRole Role;
    public float Fee, Skill, Reading;
    public int Breakdowns, ArrivalMinutes;
    public string Text;
}

public partial class PlayerDesk {
    /// <summary>A breakdown needs a weak spot: below this severity a pass never falls apart.</summary>
    public const float StudioBreakdownSeverity = .45f;
    public const int StudioBreakdownResetMinutes = 3, StudioCallInMinutes = 30, StudioCallInAfterBreakdowns = 2;

    /// <summary>How likely a pass is to fall apart at its weak part: zero for a sound pass or a tight band, rising with
    /// how badly the part is going, how loose the player it rests on is, room tension and fatigue. A design input sized
    /// so that a tight band almost never stops and a rough one stops on a few passes in ten. Studio performance in the
    /// opening population runs about .35-.65, so the bar sits at .58.</summary>
    public const float StudioTightBand = .58f;
    public static float StudioBreakdownChance(float severity, float tightness, float tension, float fatigue) =>
        Math.Clamp(Math.Clamp((severity - StudioBreakdownSeverity) * 2.2f, 0, .6f) * Math.Clamp((StudioTightBand - tightness) / .25f, 0, 1.4f) *
            (1 + .6f * Math.Clamp(tension, 0, 1)) * (1 + .5f * Math.Clamp(fatigue, 0, 1)), 0, .75f);

    /// <summary>A flub: any band, however tight, false-starts or fluffs an entrance now and then. About one pass in
    /// twenty-five fresh, more when tired. It costs a minute or two, not a pass, and points at nobody's part.</summary>
    public const float StudioFlubRate = .04f;
    public static float StudioFlubChance(float fatigue) => StudioFlubRate * (1 + Math.Clamp(fatigue, 0, 1));

    private static readonly string[] Flubs = {
        "{0} came in a beat early and everyone stopped.", "{0} fluffed the count-in.", "{0} started in the wrong key; they stopped and laughed.",
        "A string went on the downbeat; they stopped to change it.", "Somebody knocked a microphone stand in {1}.", "{0} missed the cue at {1}."
    };

    /// <summary>Marks a false start. The stop lands in the first part or two; the take can't be kept, but it is not a
    /// breakdown and never counts toward a session player's call-in.</summary>
    private static void Flub(PendingSession session, PlayerDesk.SessionTake take, StudioSongStructure shape, SimulatedArtist artist, string key) {
        int section = Math.Min(shape.Sections.Count - 1, StudioTakeEvidence.Unit(key + "|flub-where") < .6 ? 0 : 1);
        var people = artist.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).OrderBy(m => m.personId, StringComparer.Ordinal).ToList();
        string who = people.Count == 0 ? "Somebody" : people[(int)(StudioTakeEvidence.Unit(key + "|flub-who") * people.Count) % people.Count].firstName;
        int stopped = (int)Math.Round(shape.DurationSeconds * (section + .3f) / Math.Max(1, shape.Sections.Count));
        take.Incomplete = true; take.FalseStart = true; take.FaultPersonId = null;
        take.Minutes = Math.Max(2, (int)Math.Round(stopped / 60.0 + 1.5));
        string line = Flubs[(int)(StudioTakeEvidence.Unit(key + "|flub-line") * Flubs.Length) % Flubs.Length];
        take.ProducerNote = "False start.";
        take.ProducerDetail = string.Format(line, who, shape.Sections[section].Label);
        take.ProducerDetail = char.ToUpperInvariant(take.ProducerDetail[0]) + take.ProducerDetail[1..];
        take.EngineerNote = $"Stopped {StudioSongStructures.Length(stopped)} in. Rolling again.";
        take.EngineerDetail = null;
        take.PerformerNote = "Sorry, sorry. From the top.";
        take.BandNote = "Just a flub. Go again.";
        // Where it stopped is not where the song is weak: leave the located trouble on the take's real read.
    }

    private static float MemberPlaying(Musician m) => m.axesVersion > 0 ? m.instrumentalSkill : m.technicalSkill;
    private static float MemberSinging(Musician m) => m.axesVersion > 0 ? (m.vocalPower + m.vocalControl) / 2 : m.technicalSkill;
    private static bool Sings(MusicianRole role) => role is MusicianRole.LeadVocals or MusicianRole.BackingVocals;

    /// <summary>The performer a weak aspect rests on: the lead voice for power and lyric, otherwise the loosest player on
    /// the floor. A session player stands in for the member they replaced. Ties go to the stable person id.</summary>
    private static (string PersonId, string Name, float Skill, bool Hired) StudioFault(PendingSession session, SimulatedArtist artist, StudioAspect aspect) {
        var replaced = session.Hires.Where(h => h.Replaces).Select(h => h.ReplacesPersonId).ToHashSet();
        var members = artist.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active && !replaced.Contains(m.personId)).ToList();
        if (aspect is StudioAspect.Power or StudioAspect.Lyric) {
            var voice = members.Where(m => m.isLeadVocalist).OrderBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
            if (voice != null) return (voice.personId, voice.firstName, MemberSinging(voice), false);
        }
        var players = members.Where(m => !Sings(m.primaryRole)).Select(m => (PersonId: m.personId, Name: m.firstName, Skill: MemberPlaying(m), Hired: false))
            .Concat(session.Hires.Where(h => h.Kind != StudioHireKind.BackingVoice).Select(h => (h.PersonId, Name: h.Name.Split(' ')[0], h.Skill, Hired: true)))
            .OrderBy(x => x.Skill).ThenBy(x => x.PersonId, StringComparer.Ordinal).ToList();
        if (players.Count > 0) return players[0];
        var anyone = members.OrderBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        return anyone == null ? (null, "the band", session.StudioPerformance, false) : (anyone.personId, anyone.firstName, MemberPlaying(anyone), false);
    }

    private static readonly Dictionary<StudioAspect, string[]> Collapse = new() {
        [StudioAspect.Ensemble] = new[] { "{0} lost the changes in {1} and the band stopped.", "{0} came in wrong at {1}; everyone pulled up." },
        [StudioAspect.Feel] = new[] { "{0} dropped the groove in {1}; the band stopped.", "The beat fell out from under {1} when {0} rushed it." },
        [StudioAspect.Power] = new[] { "{0}'s voice gave out in {1}.", "{0} cracked on the high note in {1} and stopped." },
        [StudioAspect.Lyric] = new[] { "{0} lost the words in {1}.", "{0} sang the wrong verse in {1} and stopped." }
    };

    /// <summary>Marks a pass that fell apart: what it cost, where it stopped and who it rested on. The take keeps its
    /// number and its located read, but it is incomplete and can never be kept.</summary>
    private static void BreakDown(PlayerDesk.SessionTake take, StudioSongStructure shape, int section, StudioAspect aspect, (string PersonId, string Name, float Skill, bool Hired) fault, string key) {
        take.Incomplete = true;
        take.FaultPersonId = fault.PersonId;
        take.TroubleSection = section;
        float through = (section + .5f) / Math.Max(1, shape.Sections.Count);
        int stopped = (int)Math.Round(shape.DurationSeconds * through);
        take.Minutes = Math.Max(StudioBreakdownResetMinutes + 1, (int)Math.Round(stopped / 60.0 + StudioBreakdownResetMinutes));
        var lines = Collapse[aspect];
        string place = shape.Sections[section].Label;
        take.ProducerNote = "It fell apart; we stopped the tape.";
        take.ProducerDetail = string.Format(lines[StudioTakeEvidence.Unit(key + "|collapse") < .5 ? 0 : 1], fault.Name, place);
        take.ProducerDetail = char.ToUpperInvariant(take.ProducerDetail[0]) + take.ProducerDetail[1..];
        take.EngineerNote = $"We stopped the tape {StudioSongStructures.Length(stopped)} in.";
        take.EngineerDetail = null;
        take.PerformerNote = "Sorry. Let's go again.";
        take.BandNote = $"We lost it at {place}.";
    }

    /// <summary>The producer's suggestion, when one member's part has fallen apart at least twice on this date and a
    /// session player who plays it is free in the room's town. Null otherwise. Pure: books nobody and draws nothing.</summary>
    public StudioCallInProposal StudioCallIn() {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Room == null) return null;
        var artist = ArtistManager.Instance?.GetArtist(session.ArtistId);
        if (artist == null) return null;
        var replaced = session.Hires.Where(h => h.Replaces).Select(h => h.ReplacesPersonId).ToHashSet();
        var member = session.Cuts.SelectMany(c => c.Takes).Where(t => t.Incomplete && t.FaultPersonId != null)
            .GroupBy(t => t.FaultPersonId).Where(g => g.Count() >= StudioCallInAfterBreakdowns && !session.DeclinedCallIns.Contains(g.Key) && !replaced.Contains(g.Key))
            .Select(g => (Member: artist.members.FirstOrDefault(m => m.personId == g.Key && m.isActive), Count: g.Count()))
            .Where(x => x.Member != null && !Sings(x.Member.primaryRole)).OrderByDescending(x => x.Count).ThenBy(x => x.Member.personId, StringComparer.Ordinal).FirstOrDefault();
        if (member.Member == null) return null;
        int left = StudioMinutesLeft(session);
        var candidate = StudioSessionCandidates(session.Room.CityId, member.Member, artist, StudioToday, session.StartMinute + session.Hours * 60)
            .FirstOrDefault(m => session.Hires.All(h => h.PersonId != m.personId));
        if (candidate == null) return null;
        string role = RoleWord(member.Member.primaryRole);
        float fee = SessionPlayerFee(candidate.sightReading);
        bool fits = left >= StudioCallInMinutes + FinishMinutes(session) + session.Cuts.Where(c => c.KeptTake < 0).Select(StudioPassMinutes).DefaultIfEmpty(0).Min();
        return new() {
            MemberId = member.Member.personId, MemberName = member.Member.firstName, CandidateId = candidate.personId, CandidateName = candidate.FullName,
            Role = member.Member.primaryRole, Fee = fee, Skill = candidate.technicalSkill, Reading = candidate.sightReading, Breakdowns = member.Count, ArrivalMinutes = StudioCallInMinutes,
            Text = $"Producer: \"{member.Member.firstName}'s {role} has come apart {member.Count} times now. {candidate.FullName} plays {role} on sessions around town and is free today. " +
                $"${fee:N0}, here in about {StudioCallInMinutes} minutes.\" {member.Member.firstName} would sit out the rest of the date and is likely to take it hard; " +
                $"takes already cut stay the band's own.{(fits ? "" : " There is not enough time left for the wait and another pass.")}"
        };
    }

    /// <summary>Session-capable people in a town who play this member's part and are free for the rest of the date,
    /// best readers first, a label's regulars ahead of strangers. Read-only.</summary>
    private IEnumerable<Musician> StudioSessionCandidates(string cityId, Musician member, SimulatedArtist act, GameDate date, int endMinute) {
        int day = LocalSceneRoomService.Day(date), now = (TimeManager.Instance?.CurrentHour ?? 9) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
        return SessionEmploymentService.ListeningCandidates(cityId, day)
            .Where(m => (m.primaryRole == member.primaryRole || m.secondaryRole == member.primaryRole || m.primaryRole == MusicianRole.MultiInstrumentalist) &&
                !act.members.Any(x => x.personId == m.personId) && !LocalSceneRoomService.HasPersonBooking(m.personId, day, now, endMinute, null))
            .OrderByDescending(m => m.sightReading + m.technicalSkill + SessionEmploymentService.RegularScore(Label.labelId, m.personId))
            .ThenBy(m => m.personId, StringComparer.Ordinal);
    }

    /// <summary>A session reader's fee: the Band Room's established rate, which rises with how well they read.</summary>
    public static float SessionPlayerFee(float sightReading) => Mathf.Round(15f + 60f * sightReading);

    /// <summary>Calls the proposed session player in. They take the member's part for every later pass; the wait spends
    /// room time (the band rests meanwhile); the fee is paid now and is not recouped; the member is put out.</summary>
    public bool CallInSessionPlayer(string memberId, string candidateId, out string message) {
        var proposal = StudioCallIn();
        if (proposal == null || proposal.MemberId != memberId || proposal.CandidateId != candidateId) {
            message = "That session player is no longer available, or the producer no longer suggests it."; return false;
        }
        var session = pendingSession;
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        var member = artist.members.First(m => m.personId == memberId);
        if (Label.cashReserves < proposal.Fee) { message = $"{proposal.CandidateName} costs ${proposal.Fee:N0}; you are ${proposal.Fee - Label.cashReserves:N0} short."; return false; }
        if (!SpendStudioWork(session, proposal.ArrivalMinutes, $"Called in {proposal.CandidateName} on {RoleWord(proposal.Role)} for {member.firstName}; the band rested while waiting", out message, false)) return false;
        session.WorkloadMinutes = Math.Max(0, session.WorkloadMinutes - 8);
        var person = SessionEmploymentService.ListeningCandidates(session.Room.CityId, LocalSceneRoomService.Day(StudioToday)).FirstOrDefault(m => m.personId == candidateId);
        var hire = new StudioSessionHire {
            PersonId = candidateId, Name = proposal.CandidateName, ReplacesPersonId = memberId, ReplacesName = member.firstName, Role = proposal.Role,
            Skill = proposal.Skill, Reading = proposal.Reading, Fee = proposal.Fee, CalledIn = true
        };
        hire.Lift = ApplySessionHire(artist, member, hire.Name, hire.Skill, hire.Reading, hire.Fee);
        hire.EnsembleLift = Math.Clamp(.25f * (hire.Skill - MemberPlaying(member)), 0, .06f);
        session.Hires.Add(hire);
        session.SessionLift = Math.Min(.10f, session.SessionLift + hire.Lift);
        session.Cost += hire.Fee;
        if (person != null) SessionEmploymentService.OnPlayerSession(person, Label.labelId, artist, hire.Fee);
        message = $"{hire.Name} is set up on {RoleWord(hire.Role)}. Takes already cut stay {member.firstName}'s.";
        Changed?.Invoke(); return true;
    }

    /// <summary>Keeps the member on the part. The producer does not raise it again for them on this date.</summary>
    public bool DeclineCallIn(string memberId, out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || string.IsNullOrEmpty(memberId) || session.DeclinedCallIns.Contains(memberId)) { message = "Nothing to decline."; return false; }
        session.DeclinedCallIns.Add(memberId);
        session.ActionLog.Add("Kept the band's own player on the part after the producer suggested a session player.");
        message = "The band keeps its own player."; Changed?.Invoke(); return true;
    }

    /// <summary>The credits for a kept take: the session players who played on it, house players as the room's, and a
    /// backing group once, by its name, with its voices.</summary>
    public static List<string> StudioSessionCredits(PendingSession session, SessionTake take) {
        var on = session.Hires.Where(h => take.SessionPersonIds?.Contains(h.PersonId) == true).ToList();
        return on.Where(h => h.Kind != StudioHireKind.BackingVoice).Select(h => $"{h.Name} ({RoleWord(h.Role).ToLowerInvariant()}" +
                (h.Kind == StudioHireKind.HousePlayer ? ", the room's house player" : "") + (h.Replaces ? $", in place of {h.ReplacesName})" : ")"))
            .Concat(on.Where(h => h.Kind == StudioHireKind.BackingVoice).GroupBy(h => h.GroupName)
                .Select(g => $"{g.Key} (backing vocals: {string.Join(", ", g.Select(h => h.Name))})")).ToList();
    }
}
