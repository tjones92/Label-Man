using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Selective recording (directive slice 4): what a layout lets the date do after a pass. A vocal on its own track can
/// be sung again over the kept backing; a spare third track can take an added part; a careful engineer can splice two
/// passes of the same arrangement at a part boundary, which is what made a broken-down take's good half usable. Each
/// operation needs the layout and the source it says, costs booked time (and tape where it uses tape), and makes a new
/// take: the sources stay safe. Mono stays a complete way to make a record.
/// </summary>
public partial class PlayerDesk {
    public const int StudioSpliceMinutes = 12;
    /// <summary>A clean edit needs steady hands. Below this an engineer won't cut the tape.</summary>
    public const float StudioSpliceHands = .35f;

    private static bool UsableSource(SessionTake take) => take?.Work != null && !take.Work.Erased;
    private static int VocalOverdubMinutes(SessionCut cut, SessionTake source) =>
        Math.Max(6, (int)Math.Round(2 * Math.Max(60, source.Work.DurationSeconds > 0 ? source.Work.DurationSeconds : cut.Structure?.DurationSeconds ?? 150) / 60.0 + 4));
    private static int PartOverdubMinutes(SessionCut cut, SessionTake source) => VocalOverdubMinutes(cut, source) + 2;

    /// <summary>Why a new vocal can't go over this take, or null.</summary>
    public string StudioVocalOverdubBlocked(int side, int index) {
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (index < 0 || index >= cut.Takes.Count) return "No such take.";
        var source = cut.Takes[index];
        if (!UsableSource(source) || source.Incomplete) return "Only a complete take can be the backing for a new vocal.";
        if (source.Work.Layout == StudioLayout.Together) return "This take was recorded with everyone together: the vocal is part of the band on one track.";
        if (cut.Structure?.Instrumental == true) return "There is no vocal on an instrumental.";
        if (source.Work.Derivation == StudioDerivation.Splice) return "The engineer won't sing over a spliced tape.";
        if (StudioStockLeft(session) <= 0) return "The tape stock is used up.";
        return null;
    }

    /// <summary>Keep this take's backing and sing the vocal again. Costs the song's length twice plus a reset, tires the
    /// singer like a pass, and makes a new take; on two tracks it is a copy of the backing, a generation down.</summary>
    public bool OverdubStudioVocal(int side, int index, out string message) {
        if (StudioVocalOverdubBlocked(side, index) is { } blocked) { message = blocked; return false; }
        StudioSide(side, out var session, out var cut, out _);
        var source = cut.Takes[index];
        int minutes = VocalOverdubMinutes(cut, source);
        if (StudioMinutesLeft(session) < minutes + FinishMinutes(session) || !RequireAtStudio(session, out message)) {
            message = "A new vocal won't fit while protecting finishing."; return false;
        }
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        int number = cut.Takes.Count + 1;
        string key = $"{session.SessionId}|{side}|action:{session.ActionNumber + 1}|take:{number}|vocal-over:{source.TakeId}";
        float Noise(string purpose) => (float)(StudioTakeEvidence.Unit(key + "|" + purpose) * 2 - 1);
        float fatigue = Math.Clamp(session.WorkloadMinutes / (180 + 120 * session.StudioPerformance), 0, 1);
        float baseHook = cut.Choice.WrittenSong?.Hook ?? Math.Clamp(session.StudioPerformance * .5f + session.LivePerformance * .2f + .15f, 0, 1);
        // The singer alone, against the band in the headphones: a fresh read of the vocal, the room's effect on the voice,
        // and how the song's key now sits. The backing keeps its ensemble and capture.
        float vocal = baseHook * .75f + session.Room.Craft * .10f + .03f + .12f * Noise("vocal") * (1 + session.Tension) - fatigue * .12f;
        vocal += StudioPeople.RoomEffect(session.Room, cut.Structure).Vocal;
        var shapeStructure = StudioStructureOf(cut, source);
        float power = VoicePower(StudioLead(act));
        if (source.Work.KeyShift < 0) vocal += .015f + .05f * fatigue * (shapeStructure?.PowerDemand ?? .5f);
        else if (source.Work.KeyShift > 0) vocal += .08f * (power - .6f) - .04f * fatigue;
        float generation = session.Work?.Tracks == 2 ? .01f : 0;
        var take = new SessionTake {
            TakeId = key, Number = number, Direction = cut.Direction, Arrangement = source.Arrangement, ArrangementRevision = source.ArrangementRevision,
            Vocal = Mathf.Clamp(vocal, 0, 1), Ensemble = source.Ensemble, Capture = Mathf.Clamp(source.Capture - generation, 0, 1),
            SessionPersonIds = source.SessionPersonIds?.ToList(), Minutes = minutes, Work = source.Work.Copy()
        };
        take.Feel = Mathf.Clamp(.65f * source.Feel + .35f * (baseHook * .75f + .1f * Noise("feel")), 0, 1);
        take.Hook = Mathf.Clamp(take.Vocal * .55f + take.Feel * .30f + take.Ensemble * .15f + session.SessionLift * .3f, 0, 1);
        take.Production = take.Capture;
        take.Work.Derivation = StudioDerivation.VocalOverdub; take.Work.SourceTakeId = source.TakeId; take.Work.SecondSourceTakeId = null;
        take.Work.Incident = StudioIncident.None; take.Work.IncidentNote = null; take.Work.IncidentPersonId = null; take.Work.Erased = false;
        if (cut.Structure != null) {
            var (section, aspect, severity) = StudioSongStructures.Locate(cut.Structure, take, cut.PartRehearsals, cut.Direction, fatigue, session.ProducerCraft, key);
            take.TroubleSection = severity >= .40f ? section : -1;
            take.ProducerDetail = StudioSongStructures.TroubleNote(cut.Structure, section, aspect, severity, key);
            take.EngineerDetail = source.EngineerDetail;
        }
        if (!SpendStudioWork(session, minutes, $"{cut.Choice.Title}: new vocal over take {source.Number}'s backing", out message, false)) return false;
        cut.Takes.Add(take); session.WorkloadMinutes += minutes * .7f; session.Work.PassesUsed++;
        StudioTakeEvidence.Observe(session, cut, side, session.ProducerCraft, session.StudioPerformance);
        message = $"Take {number}: a new vocal over take {source.Number}'s backing. Take {source.Number} stays as it was.";
        Changed?.Invoke(); return true;
    }

    /// <summary>The parts this take's spare track could take now, with why not for each that can't.</summary>
    public IEnumerable<(StudioOverdubPart Part, string Blocked)> StudioOverdubOptions(int side, int index) {
        if (!StudioSide(side, out var session, out var cut, out _) || index < 0 || index >= cut.Takes.Count) yield break;
        var source = cut.Takes[index];
        if (!UsableSource(source) || source.Work.Layout != StudioLayout.ThreeTrack) yield break;
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        int spare = Math.Max(1, (session.Work?.Tracks ?? 3) - 2);
        foreach (StudioOverdubPart part in Enum.GetValues<StudioOverdubPart>()) {
            string blocked = source.Incomplete ? "Only a complete take takes an added part."
                : source.Work.Derivation == StudioDerivation.Splice ? "Not on a spliced tape."
                : (source.Work.Parts?.Count ?? 0) >= spare ? "There's no spare track left on this take."
                : source.Work.Parts?.Contains(part) == true ? "That part is already on this take."
                : part == StudioOverdubPart.BackingVoices && StudioHarmonyVoiceOn(session, act) < 0 ? "Nobody here can sing behind the lead."
                : part == StudioOverdubPart.BackingVoices && cut.Structure?.Instrumental == true ? "There are no words to sing behind."
                : part == StudioOverdubPart.HornLine && !ActivePeople(act).Any(m => MemberRoleService.Plays(m, MusicianRole.Saxophone) || MemberRoleService.Plays(m, MusicianRole.Trumpet)) &&
                    !session.Hires.Any(h => h.Kind == StudioHireKind.Horn) ? "Nobody here plays a horn."
                : part == StudioOverdubPart.HornLine && StudioPartWriter(session, act) == null ? "Somebody has to write the part out."
                : null;
            yield return (part, blocked);
        }
    }

    public static string OverdubPartName(StudioOverdubPart part) => part switch {
        StudioOverdubPart.BackingVoices => "Backing voices", StudioOverdubPart.Handclaps => "Handclaps", _ => "A horn line"
    };

    /// <summary>Lay a part onto the spare track of a three-track take. The band rests; the new part is played against the
    /// tape. Makes a new take with the part; the source keeps its empty track.</summary>
    public bool OverdubStudioPart(int side, int index, StudioOverdubPart part, out string message) {
        if (!Enum.IsDefined(part)) { message = "Unknown part."; return false; }
        var options = StudioOverdubOptions(side, index).ToList();
        if (!options.Any(o => o.Part == part)) { message = "This take has no spare track for an added part."; return false; }
        if (options.First(o => o.Part == part).Blocked is { } blocked) { message = blocked; return false; }
        StudioSide(side, out var session, out var cut, out _);
        var source = cut.Takes[index];
        int minutes = PartOverdubMinutes(cut, source);
        if (StudioMinutesLeft(session) < minutes + FinishMinutes(session) || !RequireAtStudio(session, out message)) {
            message = "The overdub won't fit while protecting finishing."; return false;
        }
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        int number = cut.Takes.Count + 1;
        string key = $"{session.SessionId}|{side}|action:{session.ActionNumber + 1}|take:{number}|part:{part}:{source.TakeId}";
        float played = (float)(StudioTakeEvidence.Unit(key + "|played") * 2 - 1) * .01f;
        float vocal = source.Vocal, feel = source.Feel, ensemble = source.Ensemble, capture = source.Capture;
        switch (part) {
            case StudioOverdubPart.BackingVoices:
                float lift = StudioBackingVocal(StudioBackingCall(cut.Structure, cut.Choice.WrittenSong?.Genre ?? act.primaryGenre), StudioHarmonyVoiceOn(session, act));
                vocal += lift + played; feel += Math.Max(0, lift) * .4f; break;
            case StudioOverdubPart.Handclaps: feel += .015f + played; break;
            default: feel += .025f + played; capture -= .005f; break;
        }
        var take = new SessionTake {
            TakeId = key, Number = number, Direction = source.Direction, Arrangement = source.Arrangement, ArrangementRevision = source.ArrangementRevision,
            Vocal = Mathf.Clamp(vocal, 0, 1), Ensemble = ensemble, Feel = Mathf.Clamp(feel, 0, 1), Capture = Mathf.Clamp(capture, 0, 1),
            TroubleSection = source.TroubleSection, ProducerDetail = source.ProducerDetail, EngineerDetail = source.EngineerDetail,
            SessionPersonIds = session.Hires.Count == 0 ? null : session.Hires.Select(h => h.PersonId).ToList(), Minutes = minutes, Work = source.Work.Copy()
        };
        take.Hook = Mathf.Clamp(take.Vocal * .55f + take.Feel * .30f + take.Ensemble * .15f + session.SessionLift * .3f, 0, 1);
        take.Production = take.Capture;
        take.Work.Derivation = StudioDerivation.PartOverdub; take.Work.SourceTakeId = source.TakeId; take.Work.SecondSourceTakeId = null;
        take.Work.Incident = StudioIncident.None; take.Work.IncidentNote = null; take.Work.IncidentPersonId = null; take.Work.Erased = false;
        take.Work.Parts = (source.Work.Parts ?? new()).Append(part).ToList();
        if (!SpendStudioWork(session, minutes, $"{cut.Choice.Title}: {OverdubPartName(part).ToLowerInvariant()} onto take {source.Number}'s spare track", out message, false)) return false;
        cut.Takes.Add(take); session.WorkloadMinutes += minutes * .3f;
        StudioTakeEvidence.Observe(session, cut, side, session.ProducerCraft, session.StudioPerformance);
        message = $"Take {number}: take {source.Number} with {OverdubPartName(part).ToLowerInvariant()} added. Take {source.Number} stays as it was.";
        Changed?.Invoke(); return true;
    }

    /// <summary>Where a splice of these two takes would fall: the part where the first went wrong, or its middle. -1 when they
    /// can't be spliced.</summary>
    public int StudioSplicePoint(SessionCut cut, int first, int second) {
        if (first < 0 || second < 0 || first >= cut.Takes.Count || second >= cut.Takes.Count || first == second || cut.Structure == null) return -1;
        var a = cut.Takes[first]; var b = cut.Takes[second];
        if (!UsableSource(a) || !UsableSource(b) || b.Incomplete || a.FalseStart) return -1;
        if (a.Work.Derivation != StudioDerivation.None || b.Work.Derivation != StudioDerivation.None) return -1;
        if (a.Arrangement != b.Arrangement || a.ArrangementRevision != b.ArrangementRevision || a.Work.KeyShift != b.Work.KeyShift ||
            a.Work.TempoShift != b.Work.TempoShift || a.Work.Trimmed != b.Work.Trimmed || a.Work.Layout != b.Work.Layout) return -1;
        int point = a.TroubleSection > 0 ? a.TroubleSection : a.Incomplete ? -1 : cut.Structure.Sections.Count / 2;
        return point >= 1 && point < cut.Structure.Sections.Count ? point : -1;
    }

    /// <summary>Why these two takes can't be spliced, or null.</summary>
    public string StudioSpliceBlocked(int side, int first, int second) {
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (!StudioRulesV2(session)) return RulesV2Needed;
        if (session.Room.EngineerHands < StudioSpliceHands) return $"{session.Room.EngineerName ?? "The engineer"} won't take a razor blade to the master.";
        if (StudioSplicePoint(cut, first, second) < 0)
            return "Those two can't be joined: they need the same arrangement, key, tempo and layout, the second has to be complete, and the first has to get somewhere before it goes wrong.";
        return null;
    }

    /// <summary>Cut two passes together at a part boundary: the first up to where it went wrong, the second from there on.
    /// Editing takes the engineer's time and no tape. The join can show if the passes differ in feel.</summary>
    public bool SpliceStudio(int side, int first, int second, out string message) {
        if (StudioSpliceBlocked(side, first, second) is { } blocked) { message = blocked; return false; }
        StudioSide(side, out var session, out var cut, out _);
        if (StudioMinutesLeft(session) < StudioSpliceMinutes + FinishMinutes(session) || !RequireAtStudio(session, out message)) {
            message = "The edit won't fit while protecting finishing."; return false;
        }
        var a = cut.Takes[first]; var b = cut.Takes[second];
        int point = StudioSplicePoint(cut, first, second);
        float share = point / (float)cut.Structure.Sections.Count;
        float Blend(float x, float y) => share * x + (1 - share) * y;
        int number = cut.Takes.Count + 1;
        string key = $"{session.SessionId}|{side}|action:{session.ActionNumber + 1}|take:{number}|splice:{a.TakeId}:{b.TakeId}";
        float hands = session.Room.EngineerHands;
        float seam = Math.Abs(a.Feel - b.Feel) > .15f ? .02f : 0;
        var take = new SessionTake {
            TakeId = key, Number = number, Direction = b.Direction, Arrangement = b.Arrangement, ArrangementRevision = b.ArrangementRevision,
            Vocal = Blend(a.Vocal, b.Vocal), Ensemble = Blend(a.Ensemble, b.Ensemble),
            Feel = Mathf.Clamp(Blend(a.Feel, b.Feel) - .01f - seam, 0, 1), Capture = Mathf.Clamp(Blend(a.Capture, b.Capture) - .005f * (1 - hands), 0, 1),
            SessionPersonIds = (a.SessionPersonIds ?? new()).Union(b.SessionPersonIds ?? new()).ToList() is { Count: > 0 } people ? people : null,
            Minutes = StudioSpliceMinutes, Work = b.Work.Copy()
        };
        take.Hook = Mathf.Clamp(take.Vocal * .55f + take.Feel * .30f + take.Ensemble * .15f + session.SessionLift * .3f, 0, 1);
        take.Production = take.Capture;
        take.Work.Derivation = StudioDerivation.Splice; take.Work.SourceTakeId = a.TakeId; take.Work.SecondSourceTakeId = b.TakeId; take.Work.SpliceSection = point;
        take.Work.Incident = StudioIncident.None; take.Work.IncidentNote = null; take.Work.IncidentPersonId = null; take.Work.Erased = false;
        var (section, aspect, severity) = StudioSongStructures.Locate(cut.Structure, take, cut.PartRehearsals, cut.Direction, 0, session.ProducerCraft, key);
        take.TroubleSection = severity >= .40f ? section : -1;
        take.ProducerDetail = seam > 0 ? $"You can hear the join going into {cut.Structure.Sections[point].Label}; the feel changes." : StudioSongStructures.TroubleNote(cut.Structure, section, aspect, severity, key);
        if (!SpendStudioWork(session, StudioSpliceMinutes, $"{cut.Choice.Title}: spliced take {a.Number} into take {b.Number} at {cut.Structure.Sections[point].Label}", out message, false)) return false;
        cut.Takes.Add(take);
        StudioTakeEvidence.Observe(session, cut, side, session.ProducerCraft, session.StudioPerformance);
        message = $"Take {number}: take {a.Number} up to {cut.Structure.Sections[point].Label}, take {b.Number} from there. Both originals stay safe.";
        Changed?.Invoke(); return true;
    }
}
