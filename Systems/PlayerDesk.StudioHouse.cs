using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>One of a specialist room's house players on a booking: who, on what, and whom they sit in for (if anyone).</summary>
public sealed class StudioHousePlayer {
    public string PersonId, Name, ReplacesPersonId, ReplacesName;
    public string DeputyFor;   // the regular this player is standing in for, when the crew's own is out
    public MusicianRole Role;
    public float Skill, Reading, Fee;
}

/// <summary>What a specialist room's owner asks of this act: whether the house rhythm section plays the date, who that is
/// today, and why. Pure: built for the booking card, books and pays nobody.</summary>
public sealed class StudioHousePlan {
    public bool Insists;
    public string Reason;
    /// <summary>Why the owner won't record this act today at all. Null when the room can be booked.</summary>
    public string Locked;
    public List<StudioHousePlayer> Players = new();
    public float Fee => Players.Sum(p => p.Fee);
}

/// <summary>A hired backing group: three (or two) local singers under one name, for a singer with nobody to sing behind them.</summary>
public sealed class StudioBackingOffer {
    public string GroupName;
    public List<StudioHousePlayer> Voices = new();
    public float Fee => Voices.Sum(v => v.Fee);
    public float Voice;      // the group's average backing voice, 0-1
    public int ArrivalMinutes;
    public string Text;
}

/// <summary>Specialist owners and their house players, and backing singers for a singer who needs them.
///
/// House players: the period's distinctive rooms ran with their own rhythm sections, and acts who came to them mostly
/// recorded with those players: Dave Bartholomew's band backed nearly every date at Cosimo Matassa's J&amp;M in New Orleans;
/// Booker T. &amp; the M.G.'s backed Stax's acts and the Atlantic singers Jerry Wexler sent to Memphis; FAME's successive
/// rhythm sections were the reason singers drove to Muscle Shoals. Many singers didn't tour with a band at all and arrived
/// with charts. So a specialist room supplies its house rhythm section to a singer who has none, and its owner insists on
/// it for a band he hasn't reason to trust, but lets a tight band, or one that has cut there before, bring its own.
///
/// Backing singers: Nashville's session vocal groups (the Jordanaires, the Anita Kerr Singers) sang behind a large share
/// of early-sixties sides, several sessions a day. A singer with no one in the act to sing harmony can hire a local group
/// for songs that call for voices behind the lead; on a hushed or plain-spoken song they clutter it.
/// Every effect is a bounded design input and every value here is keyed, never a gameplay random draw.</summary>
public partial class PlayerDesk {
    public const float StudioHouseUpset = .5f;          // a house rule stings a replaced member half as much as the label's choice
    public const float StudioOwnPlayersAfterVisit = .50f; // a band that has printed a date here before may bring its own at this tightness
    public const int StudioBackingArrivalMinutes = 30;
    private static readonly MusicianRole[] HouseRhythm = { MusicianRole.Drums, MusicianRole.Bass };
    private static readonly MusicianRole[] ChordRoles = { MusicianRole.Piano, MusicianRole.Organ, MusicianRole.RhythmGuitar, MusicianRole.LeadGuitar, MusicianRole.MultiInstrumentalist };

    private static bool PlaysRole(Musician m, MusicianRole role) => MemberRoleService.Plays(m, role) || m.primaryRole == MusicianRole.MultiInstrumentalist;
    private static List<Musician> ActivePeople(SimulatedArtist act) => act.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).ToList();
    private static string OwnerName(StudioRoomSnapshot room) => room.Name.Split(' ')[0];

    /// <summary>The owner's terms for this act at a specialist room on this date, with the house players who would play.</summary>
    public StudioHousePlan StudioHouseBand(StudioRoomSnapshot room, SimulatedArtist act, GameDate date, int startMinute, int endMinute) {
        if (room == null || !room.Specialist || act == null || !StudioExperience.Enabled) return null;
        var plan = new StudioHousePlan();
        string owner = OwnerName(room);
        var people = ActivePeople(act);
        // A Band Room session player is already a professional the owner accepts.
        string bookedOut = SessionPlayerFor(act.artistId)?.ReplacesPersonId;
        var own = people.Where(m => m.personId != bookedOut).ToList();
        bool singer = SessionEmploymentService.NeedsCrew(act);
        var roles = HouseRhythm.ToList();
        if (singer && !own.Any(m => ChordRoles.Contains(m.primaryRole))) roles.Add(MusicianRole.Piano);
        if (singer) {
            plan.Insists = true;
            plan.Reason = $"{act.stageName} has no rhythm section of its own; {owner}'s house players play the date.";
            roles = roles.Where(r => !own.Any(m => m.primaryRole == r)).ToList();
        } else if (act.studioPerformance >= StudioTightBand) {
            plan.Reason = $"{owner} has heard {act.stageName} is a tight band and lets them bring their own players.";
            return plan;
        } else if (studioVisits.Any(v => v.RoomId == room.Id && v.ArtistId == act.artistId && v.Printed) && act.studioPerformance >= StudioOwnPlayersAfterVisit) {
            plan.Reason = $"{act.stageName} cut a record here before; {owner} lets them use their own players.";
            return plan;
        } else {
            plan.Insists = true;
            plan.Reason = $"{owner} doesn't know {act.stageName} and records outside bands with the house rhythm section: the house drummer and bass player sit in.";
        }
        int day = LocalSceneRoomService.Day(date);
        var town = SessionEmploymentService.ListeningCandidates(room.CityId, day).Where(m => !act.members.Any(x => x.personId == m.personId)).ToList();
        // An owner can only insist on a part somebody in town plays on sessions. Where nobody does, the act plays it.
        roles = roles.Where(r => town.Any(m => PlaysRole(m, r))).ToList();
        if (roles.Count == 0) {
            plan.Insists = false;
            plan.Reason = $"{owner} has no regular rhythm section to put on the date; {act.stageName} plays it with what it has.";
            return plan;
        }
        if (!singer && act.artistCreativeControl && own.Any(m => roles.Contains(m.primaryRole))) {
            plan.Locked = $"{act.stageName} holds creative control and won't record without its own players, and {owner} insists on the house rhythm section. Book another room.";
            return plan;
        }
        var pool = town.Where(m => !LocalSceneRoomService.HasPersonBooking(m.personId, day, startMinute, endMinute, null)).ToList();
        var crew = HouseCrewFor(room, day, date.year);
        if (crew != null) plan.Reason += $" The house band is {crew.Name}.";
        foreach (var role in roles) {
            // The house players are the owner's regulars: the same best readers come up date after date.
            // The crew's regular on this part when free; otherwise the owner calls a deputy, the next best free reader.
            var regular = crew?.Seats.FirstOrDefault(s => s.Role == role);
            var player = regular == null ? null : pool.FirstOrDefault(m => m.personId == regular.PersonId && plan.Players.All(p => p.PersonId != m.personId));
            string deputyFor = null;
            if (player == null) {
                player = pool.Where(m => PlaysRole(m, role) && plan.Players.All(p => p.PersonId != m.personId))
                    .OrderByDescending(m => m.technicalSkill + m.sightReading + .5f * (float)StudioTakeEvidence.Unit($"{room.Id}|house|{m.personId}"))
                    .ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
                if (player != null && regular != null) deputyFor = regular.Name;
            }
            if (player == null) {
                plan.Locked = $"{owner}'s house {RoleWord(role).ToLowerInvariant()} player is out on another date today, and the owner won't record {act.stageName} without the house band. Try another day.";
                plan.Players.Clear(); return plan;
            }
            var sits = singer ? null : own.Where(m => m.primaryRole == role).OrderBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
            plan.Players.Add(new StudioHousePlayer {
                PersonId = player.personId, Name = player.FullName, Role = role, Skill = player.technicalSkill, Reading = player.sightReading,
                Fee = SessionPlayerFee(player.sightReading), ReplacesPersonId = sits?.personId, ReplacesName = sits?.firstName, DeputyFor = deputyFor
            });
        }
        return plan;
    }

    /// <summary>Puts the house players on a new date: paid now, not recouped, credited on the takes they play.</summary>
    private void SeatHousePlayers(PendingSession session, SimulatedArtist artist, StudioHousePlan plan) {
        if (plan?.Insists != true) return;
        var town = SessionEmploymentService.ListeningCandidates(session.Room.CityId, LocalSceneRoomService.Day(session.Date));
        foreach (var p in plan.Players) {
            var member = p.ReplacesPersonId == null ? null : artist.members.FirstOrDefault(m => m.personId == p.ReplacesPersonId);
            var hire = new StudioSessionHire {
                Kind = StudioHireKind.HousePlayer, PersonId = p.PersonId, Name = p.Name, Role = p.Role, Skill = p.Skill, Reading = p.Reading, Fee = p.Fee,
                ReplacesPersonId = member?.personId, ReplacesName = member?.firstName
            };
            hire.Lift = ApplySessionHire(artist, member, hire.Name, hire.Skill, hire.Reading, hire.Fee, StudioHouseUpset);
            // Sitting in for a looser member tightens the band; filling a part a singer had nobody for tightens it against the act's own level.
            hire.EnsembleLift = Math.Clamp(.25f * (hire.Skill - (member != null ? MemberPlaying(member) : artist.studioPerformance)), 0, .06f);
            session.Hires.Add(hire);
            session.SessionLift = Math.Min(.10f, session.SessionLift + hire.Lift);
            session.Cost += hire.Fee;
            if (town.FirstOrDefault(m => m.personId == p.PersonId) is { } person) SessionEmploymentService.OnPlayerSession(person, Label.labelId, artist, hire.Fee);
            session.ActionLog.Add($"House player: {hire.Name} on {RoleWord(hire.Role).ToLowerInvariant()}{(member != null ? $", in place of {member.firstName}" : "")}{(p.DeputyFor != null ? $", deputising for {p.DeputyFor}" : "")} (${hire.Fee:N0}).");
        }
    }

    // ---- Backing singers ----

    /// <summary>A singer needs backing voices when nobody else in the act can sing behind them: no one else is a singer,
    /// nobody sings harmony on stage (a second role, with a voice that holds a part), and no player has the voice to pick
    /// a part up unprompted. That last bar sits below a hired singer's: a bandmate learns the part over months, not in the
    /// room. At .45 alone about a quarter of the opening population's bands had a harmony voice.</summary>
    public const float StudioHarmonyBar = .45f;
    /// <summary>The studio rules a new date is booked under (PendingSession.RulesVersion). 2: own harmony singers.</summary>
    public const int StudioRulesCurrent = 2;
    public static bool StudioNeedsBacking(SimulatedArtist act) =>
        act != null && !act.instrumentalPerformance && !ActivePeople(act).Any(m => !m.isLeadVocalist &&
            (MemberRoleService.SingsHarmony(m) || SessionEmploymentService.BackingVoice(m) >= StudioHarmonyBar));

    /// <summary>The act's own voices behind the lead on this date: members (not sitting out) who sing a part, best three.
    /// Empty for dates booked before own harmony counted (RulesVersion &lt; 2).</summary>
    internal static List<Musician> StudioOwnHarmony(PendingSession session, SimulatedArtist act) {
        if (session == null || act == null || act.instrumentalPerformance || session.RulesVersion < 2) return new();
        var sittingOut = session.Hires.Where(h => h.Replaces).Select(h => h.ReplacesPersonId).ToHashSet();
        return ActivePeople(act).Where(m => !sittingOut.Contains(m.personId) && !m.isLeadVocalist &&
                (MemberRoleService.SingsHarmony(m) || SessionEmploymentService.BackingVoice(m) >= StudioHarmonyBar))
            .OrderByDescending(SessionEmploymentService.BackingVoice).ThenBy(m => m.personId, StringComparer.Ordinal).Take(3).ToList();
    }

    /// <summary>The voices behind the lead on this pass, -1 for none: a hired group when there is one (they take the
    /// parts), otherwise the act's own singers, whose average voice is discounted when only one or two sing.</summary>
    internal static float StudioHarmonyVoiceOn(PendingSession session, SimulatedArtist act) {
        float hired = StudioBackingVoiceOn(session);
        if (hired >= 0) return hired;
        var own = StudioOwnHarmony(session, act);
        if (own.Count == 0) return -1;
        return own.Average(SessionEmploymentService.BackingVoice) * (own.Count == 1 ? .6f : own.Count == 2 ? .85f : 1f);
    }

    /// <summary>How much a song calls for voices behind the lead, 0-1: pop numbers, big ballads, gospel shouts and the
    /// Nashville sound call loudly; blues, folk and message songs barely at all. Provisional authored weights.</summary>
    public static float StudioBackingCall(StudioSongStructure song, Genre genre) {
        if (song == null || song.Instrumental) return 0;
        float call = song.Archetype switch {
            SongArchetype.SpiritualShout => .9f, SongArchetype.SingAlongChant => .8f,
            SongArchetype.BrightPopNumber or SongArchetype.MidTempoPopSong or SongArchetype.DramaticBalladBigBuild => .7f,
            SongArchetype.DanceNumber or SongArchetype.LushStandard or SongArchetype.HornDrivenSoulNumber => .6f,
            SongArchetype.SlowBallad or SongArchetype.DeepSoulPleader or SongArchetype.CountryTwoBeat => .55f,
            SongArchetype.CharmSong or SongArchetype.StomperRocker => .4f,
            SongArchetype.MidTempoRocker or SongArchetype.Swinger or SongArchetype.SaloonBallad or SongArchetype.GrooveRiffVamp => .3f,
            SongArchetype.ShuffleTwelveBar or SongArchetype.VerseDrivenSong or SongArchetype.ReverieMoodPiece => .2f,
            SongArchetype.SlowBlues or SongArchetype.ProtestMessageSong or SongArchetype.SpokenWord => .05f,
            _ => .3f
        };
        call += genre switch {
            Genre.DooWop or Genre.GirlGroup or Genre.Soul or Genre.Motown or Genre.Country or Genre.TeenPop => .2f,
            Genre.Folk or Genre.Blues or Genre.Jazz => -.15f,
            _ => 0
        };
        return Math.Clamp(call, 0, 1);
    }
    public const float StudioBackingCallBar = .5f;

    /// <summary>What backing voices do to a pass: a lift on a song that calls for them, scaled by how good the group is;
    /// a little clutter on one that doesn't. Bounded to a few hundredths on the vocal.</summary>
    public static float StudioBackingVocal(float call, float voice) => Math.Clamp(.09f * (call - .4f) * (.5f + voice), -.02f, .05f);

    /// <summary>The local group the producer would call for this act: the best free backing voices in the room's town,
    /// under the leader's name. Null when the act doesn't need one or fewer than two singers are free. Read-only.</summary>
    public StudioBackingOffer StudioBackingSingers(string cityId, SimulatedArtist act, GameDate date, int startMinute, int endMinute, IEnumerable<string> exclude = null) {
        if (!StudioExperience.Enabled || !StudioNeedsBacking(act)) return null;
        int day = LocalSceneRoomService.Day(date);
        var taken = (exclude ?? Enumerable.Empty<string>()).ToHashSet();
        var voices = SessionEmploymentService.SingerCandidates(cityId, day)
            .Where(m => !act.members.Any(x => x.personId == m.personId) && !taken.Contains(m.personId) && !LocalSceneRoomService.HasPersonBooking(m.personId, day, startMinute, endMinute, null))
            .OrderByDescending(m => SessionEmploymentService.BackingVoice(m) + m.sightReading + SessionEmploymentService.RegularScore(Label.labelId, m.personId))
            .ThenBy(m => m.personId, StringComparer.Ordinal).Take(3).ToList();
        if (voices.Count < 2) return null;
        var offer = new StudioBackingOffer {
            GroupName = $"The {voices[0].lastName} Singers", Voice = voices.Average(SessionEmploymentService.BackingVoice), ArrivalMinutes = StudioBackingArrivalMinutes,
            Voices = voices.Select(m => new StudioHousePlayer { PersonId = m.personId, Name = m.FullName, Role = MusicianRole.BackingVocals,
                Skill = SessionEmploymentService.BackingVoice(m), Reading = m.sightReading, Fee = Mathf.Round(10f + 30f * m.sightReading) }).ToList()
        };
        offer.Text = $"{offer.GroupName} ({string.Join(", ", offer.Voices.Select(v => v.Name))}) sing sessions in town: ${offer.Fee:N0} for the date.";
        return offer;
    }

    /// <summary>Puts a backing group on the date. Each voice is paid now and is not recouped; the act loses nobody.</summary>
    private void SeatBackingSingers(PendingSession session, SimulatedArtist artist, StudioBackingOffer offer) {
        var town = SessionEmploymentService.SingerCandidates(session.Room.CityId, LocalSceneRoomService.Day(session.Date));
        foreach (var v in offer.Voices) {
            Label.cashReserves -= v.Fee; Label.monthlyExpenses += v.Fee;
            session.Hires.Add(new StudioSessionHire {
                Kind = StudioHireKind.BackingVoice, GroupName = offer.GroupName, PersonId = v.PersonId, Name = v.Name, Role = MusicianRole.BackingVocals,
                Skill = v.Skill, Reading = v.Reading, Fee = v.Fee
            });
            session.Cost += v.Fee;
            if (town.FirstOrDefault(m => m.personId == v.PersonId) is { } person) SessionEmploymentService.OnPlayerSession(person, Label.labelId, artist, v.Fee);
        }
        session.ActionLog.Add($"Backing singers: {offer.GroupName} ({string.Join(", ", offer.Voices.Select(x => x.Name))}), ${offer.Fee:N0}.");
    }

    /// <summary>The backing group's average voice on this date, or -1 with none hired.</summary>
    internal static float StudioBackingVoiceOn(PendingSession session) {
        var voices = session.Hires.Where(h => h.Kind == StudioHireKind.BackingVoice).ToList();
        return voices.Count == 0 ? -1 : voices.Average(h => h.Skill);
    }

    public const string StudioBackingDeclineKey = "backing-singers";

    /// <summary>The producer's suggestion to call backing singers in, once a song that calls for them has a complete pass
    /// and sounds bare. Null when the act has singers, a group is already here, the player said no, or nobody is free.</summary>
    public StudioBackingOffer StudioBackingCallIn() {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Room == null || session.Hires.Any(h => h.Kind == StudioHireKind.BackingVoice) ||
            session.DeclinedCallIns.Contains(StudioBackingDeclineKey)) return null;
        var artist = ArtistManager.Instance?.GetArtist(session.ArtistId);
        if (!StudioNeedsBacking(artist)) return null;
        var bare = session.Cuts.FirstOrDefault(c => c.KeptTake < 0 && c.Takes.Any(t => !t.Incomplete) &&
            StudioBackingCall(c.Structure, c.Choice.WrittenSong?.Genre ?? artist.primaryGenre) >= StudioBackingCallBar);
        if (bare == null) return null;
        int now = (TimeManager.Instance?.CurrentHour ?? 9) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
        var offer = StudioBackingSingers(session.Room.CityId, artist, StudioToday, now, session.StartMinute + session.Hours * 60, session.Hires.Select(h => h.PersonId));
        if (offer == null) return null;
        string singer = ActivePeople(artist).FirstOrDefault(m => m.isLeadVocalist)?.firstName ?? artist.stageName;
        string part = bare.Structure?.Parts.Contains(StudioSectionKind.Chorus) == true ? "the choruses" : "the last verse";
        bool fits = StudioMinutesLeft(session) >= offer.ArrivalMinutes + FinishMinutes(session) + StudioPassMinutes(bare);
        offer.Text = $"Producer: \"“{bare.Choice.Title}” sounds bare in {part} with nobody behind {singer}. {offer.GroupName} sing sessions around town and are free today: " +
            $"${offer.Fee:N0} for the three of them, here in about {offer.ArrivalMinutes} minutes.\" Takes already cut stay as they are. On a hushed or plain-spoken song, voices can clutter it." +
            (fits ? "" : " There is not enough time left for the wait and another pass.");
        return offer;
    }

    /// <summary>Calls the suggested backing group in: the wait spends room time (the band rests), the fees are paid now.</summary>
    public bool CallInBackingSingers(out string message) {
        var offer = StudioBackingCallIn();
        if (offer == null) { message = "The producer no longer suggests backing singers, or none are free."; return false; }
        var session = pendingSession;
        if (Label.cashReserves < offer.Fee) { message = $"{offer.GroupName} cost ${offer.Fee:N0}; you are ${offer.Fee - Label.cashReserves:N0} short."; return false; }
        if (!SpendStudioWork(session, offer.ArrivalMinutes, $"Called in {offer.GroupName}; the band rested while waiting", out message, false)) return false;
        session.WorkloadMinutes = Math.Max(0, session.WorkloadMinutes - 8);
        SeatBackingSingers(session, ArtistManager.Instance.GetArtist(session.ArtistId), offer);
        message = $"{offer.GroupName} are at the microphones. Takes already cut stay as they are.";
        Changed?.Invoke(); return true;
    }

    public bool DeclineBackingSingers(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.DeclinedCallIns.Contains(StudioBackingDeclineKey)) { message = "Nothing to decline."; return false; }
        session.DeclinedCallIns.Add(StudioBackingDeclineKey);
        session.ActionLog.Add("Kept the record to the act's own voices after the producer suggested backing singers.");
        message = "No backing singers on this date."; Changed?.Invoke(); return true;
    }
}
