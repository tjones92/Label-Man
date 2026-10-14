using System;
using System.Linq;

public enum StudioArrangement { Live, HalfTime, SimplerRhythm, PianoLed, HornLine, Handclaps, CallAndResponse }

public partial class PlayerDesk {
    public const int StudioArrangementMinutes = 20;

    public static string ArrangementName(StudioArrangement arrangement) => arrangement switch {
        StudioArrangement.HalfTime => "Half-time feel",
        StudioArrangement.SimplerRhythm => "Simpler rhythm figure",
        StudioArrangement.PianoLed => "Piano carries the figure",
        StudioArrangement.HornLine => "A horn line behind the chorus",
        StudioArrangement.Handclaps => "Handclaps on the backbeat",
        StudioArrangement.CallAndResponse => "Backing answers to the lead",
        _ => "Live arrangement"
    };

    /// <summary>The arrangements this date can try: the two originals always; the rest on rules-v2 dates.</summary>
    public System.Collections.Generic.IEnumerable<StudioArrangement> StudioArrangementOptions(PendingSession session) =>
        Enum.GetValues<StudioArrangement>().Where(a => a is StudioArrangement.Live or StudioArrangement.HalfTime or StudioArrangement.SimplerRhythm || StudioRulesV2(session));

    private static int ArrangementMinutes(StudioArrangement a) => a switch {
        StudioArrangement.Live => 10, StudioArrangement.Handclaps => 15, StudioArrangement.HornLine => 25, _ => StudioArrangementMinutes
    };

    /// <summary>Why this date can't play an arrangement, or null. A piano part needs someone who plays piano; a horn line needs
    /// a horn and a written part; answers need voices behind the lead.</summary>
    private string ArrangementBlocked(PendingSession session, SimulatedArtist act, SessionCut cut, StudioArrangement a) {
        var people = ActivePeople(act);
        bool hired(MusicianRole role) => session.Hires.Any(h => h.Role == role && h.Kind is StudioHireKind.HousePlayer or StudioHireKind.Replacement or StudioHireKind.Horn);
        switch (a) {
            case StudioArrangement.PianoLed:
                return people.Any(m => MemberRoleService.Plays(m, MusicianRole.Piano) || MemberRoleService.Plays(m, MusicianRole.Organ)) || hired(MusicianRole.Piano)
                    ? null : "Nobody on this date plays piano.";
            case StudioArrangement.HornLine:
                if (!people.Any(m => MemberRoleService.Plays(m, MusicianRole.Saxophone) || MemberRoleService.Plays(m, MusicianRole.Trumpet)) &&
                    !session.Hires.Any(h => h.Kind == StudioHireKind.Horn)) return "Nobody on this date plays a horn. Hire one from town first.";
                return StudioPartWriter(session, act) == null ? "Somebody has to write the part out. Hire an arranger first." : null;
            case StudioArrangement.CallAndResponse:
                if (cut.Structure?.Instrumental == true) return "There are no words to answer on an instrumental.";
                return StudioHarmonyVoiceOn(session, act) < 0 ? "Nobody here can sing behind the lead. Call backing singers in first." : null;
            default: return null;
        }
    }

    /// <summary>Who can write a horn part on this date: a hired arranger, or a member who reads and has ideas. Null for nobody.</summary>
    internal static string StudioPartWriter(PendingSession session, SimulatedArtist act) =>
        session.Hires.FirstOrDefault(h => h.Kind == StudioHireKind.Arranger)?.Name ??
        ActivePeople(act).Where(m => m.sightReading >= StudioWriterReading && m.creativity >= StudioWriterCreativity).OrderBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault()?.firstName;
    public const float StudioWriterReading = .60f, StudioWriterCreativity = .50f;

    // A proposal is descriptive and free. It does not resolve work, draw randomness or select a take.
    public string StudioArrangementProposal(int side, StudioArrangement arrangement) {
        if (!Enum.IsDefined(arrangement)) return "Unknown arrangement.";
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (!StudioArrangementOptions(session).Contains(arrangement)) return RulesV2Needed;
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        string idea = arrangement switch {
            StudioArrangement.HalfTime => "Producer: Give the phrases more space with a half-time pulse. The band will need to settle the new entrances; the record will move less briskly.",
            StudioArrangement.SimplerRhythm => "Producer: Strip back the busy rhythm figure. It may help the band stay together, but could lose some of the live drive.",
            StudioArrangement.PianoLed => "Producer: Let the piano carry the figure instead of the guitar. It leaves the singer more room; the record gets rounder and less sharp.",
            StudioArrangement.HornLine => $"Producer: A written horn line behind the chorus. It lifts the record and gives the hook an answer; it is more to balance on one tape.{(StudioPartWriter(session, artist) is { } writer ? $" {writer} can write it." : "")}",
            StudioArrangement.Handclaps => "Producer: Everyone claps the backbeat. It's catchy and danceable; it can sound like a gimmick, and it is one more thing to keep in time.",
            StudioArrangement.CallAndResponse => "Producer: The voices answer the lead in the chorus. It makes the hook bigger; the lead has to leave room for it.",
            _ => "Producer: Return to the act's live arrangement. The earlier alternatives stay on tape."
        };
        string blocked = ArrangementBlocked(session, artist, cut, arrangement);
        var (favors, resists) = StudioRulesV2(session) ? StudioStance(artist, arrangement) : (new System.Collections.Generic.List<string>(), new System.Collections.Generic.List<string>());
        string response = artist.artistCreativeControl && session.Tension > .55f && arrangement != StudioArrangement.Live || StudioRefuses(artist, session, resists)
            ? "The act: We want to keep our arrangement; our contract gives us that choice."
            : StudioRulesV2(session) ? StanceText(favors, resists) : "The act: We can try it, then compare the playback with the earlier version.";
        int minutes = ArrangementMinutes(arrangement);
        string protectedRead = cut.KeptTake >= 0 ? $"Kept take {cut.Takes[cut.KeptTake].Number} stays safe." : "Every earlier pass stays safe; no take has been kept yet.";
        return $"{idea}\n{response}{(blocked != null ? "\nNot possible yet: " + blocked : "")}\n{minutes} minutes to prepare, then {StudioPassMinutes(cut)} minutes for a pass; no extra fee within the booked date. {StudioMinutesLeft(session)} minutes remain, with {FinishMinutes(session)} reserved for finishing. {protectedRead}";
    }

    public bool ArrangeStudio(int side, StudioArrangement arrangement, out string message) {
        if (!Enum.IsDefined(arrangement)) { message = "Unknown arrangement."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!StudioArrangementOptions(session).Contains(arrangement)) { message = RulesV2Needed; return false; }
        if (arrangement == cut.Arrangement) { message = "That arrangement is already prepared."; return false; }
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        var (_, resists) = StudioRulesV2(session) ? StudioStance(artist, arrangement) : (new System.Collections.Generic.List<string>(), new System.Collections.Generic.List<string>());
        if (artist.artistCreativeControl && session.Tension > .55f && arrangement != StudioArrangement.Live || arrangement != StudioArrangement.Live && StudioRefuses(artist, session, resists)) {
            message = "The act declines the alternative under its creative control. Earlier performances remain safe."; return false;
        }
        if (ArrangementBlocked(session, artist, cut, arrangement) is { } blocked) { message = blocked; return false; }
        int minutes = ArrangementMinutes(arrangement);
        // Do not sell preparation that leaves no time to hear its result.
        if (StudioMinutesLeft(session) < minutes + StudioPassMinutes(cut) + FinishMinutes(session)) {
            message = "There is not enough time to prepare and hear this alternative while protecting finishing. Earlier performances remain safe."; return false;
        }
        if (!SpendStudioWork(session, minutes, $"Arrangement for {cut.Choice.Title}: {ArrangementName(arrangement)}", out message, false)) return false;
        cut.Arrangement = arrangement; cut.ArrangementRevision++;
        // Coordination learned on the previous arrangement is not a free rehearsal of the new one.
        cut.Rehearsals = 0; for (int i = 0; i < cut.PartRehearsals.Count; i++) cut.PartRehearsals[i] = 0;
        session.WorkloadMinutes += minutes * .6f;
        if (StudioRulesV2(session)) NoteResistance(session, resists, ArrangementName(arrangement).ToLowerInvariant());
        message = "The arrangement is prepared. Cut a pass to hear it; keeping that version is still your decision.";
        Changed?.Invoke(); return true;
    }

    /// <summary>What the prepared arrangement does to a pass. The two original alternatives keep their original effects;
    /// the newer ones read the people who carry them. Bounded to a few hundredths; never originality.</summary>
    private static (float Vocal, float Ensemble, float Feel, float Capture) ArrangementEffect(PendingSession session, SessionCut cut, SimulatedArtist act, float read) {
        float vocal = 0, ensemble = 0, feel = 0, capture = 0;
        switch (cut.Arrangement) {
            case StudioArrangement.HalfTime: vocal += .025f + read; ensemble -= .025f; feel += read - .015f; break;
            case StudioArrangement.SimplerRhythm: ensemble += .035f; feel += read - .025f; break;
            case StudioArrangement.PianoLed: ensemble += .015f; vocal += .01f; feel += read - .01f; break;
            case StudioArrangement.HornLine:
                float reading = session.Hires.FirstOrDefault(h => h.Kind == StudioHireKind.Arranger)?.Reading ?? .6f;
                feel += .025f + read; capture -= .01f; ensemble += -.015f + .02f * (reading - .5f); break;
            case StudioArrangement.Handclaps: feel += .015f + read * .5f; ensemble -= .01f; break;
            case StudioArrangement.CallAndResponse: vocal += .02f + read * .5f; feel += .01f; break;
        }
        return (vocal, ensemble, feel, capture);
    }

    /// <summary>What an arrangement or added part does to the finished record, after the shared material blend. It changes
    /// the performance's pulse and hook, never the composition or its writing credits.</summary>
    private static void ApplyArrangementToRecord(Record record, SessionTake take) {
        switch (take.Arrangement) {
            case StudioArrangement.HalfTime: record.danceability *= .70f; break;
            case StudioArrangement.SimplerRhythm: record.danceability *= .95f; break;
            case StudioArrangement.PianoLed: record.danceability *= .97f; break;
            case StudioArrangement.HornLine: record.hookStrength += .015f; break;
            case StudioArrangement.Handclaps: record.danceability *= 1.06f; record.hookStrength += .01f; break;
            case StudioArrangement.CallAndResponse: record.hookStrength += .015f; break;
        }
        var w = take.Work;
        if (w != null) {
            if (w.TempoShift > 0) record.danceability *= 1.05f; else if (w.TempoShift < 0) record.danceability *= .95f;
            if (w.Trimmed) record.hookStrength += .01f;
            if (w.KeepPickup) record.hookStrength += .01f;
            foreach (var part in w.Parts ?? new()) {
                if (part == StudioOverdubPart.HornLine) record.hookStrength += .012f;
                else if (part == StudioOverdubPart.Handclaps) { record.danceability *= 1.04f; record.hookStrength += .008f; }
                else record.hookStrength += .01f;
            }
        }
        record.hookStrength = Math.Clamp(record.hookStrength, 0, 1);
        record.danceability = Math.Clamp(record.danceability, 0, 1);
    }
}
