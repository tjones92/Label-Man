using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>One idea someone in the room has for the song: what it is, who it came from, and who likes it.</summary>
public sealed class StudioIdea {
    public string Id, From, Text;
    public StudioArrangement? Arrangement;
    public StudioShapeChange? Shape;
    public StudioBalance? Balance;
    public bool Pickup;
    public List<string> Favors = new(), Resists = new();
}

/// <summary>
/// The early creative palette (directive §5 and §7) on dates booked under studio rules v2: a different key or tempo, a
/// shorter intro and ending, the room balance, small incidents in the room and the ideas people bring. Each change is a
/// decision that costs booked time, says what it trades, and applies to this performance rather than the composition.
/// Every draw is keyed per take and purpose; nothing here touches gameplay randomness.
/// </summary>
public partial class PlayerDesk {
    public const int StudioKeyMinutes = 15, StudioTempoMinutes = 10, StudioTrimMinutes = 10, StudioBalanceMinutes = 8, StudioFixMinutes = 10;
    /// <summary>How much an intro and ending lose when trimmed, in seconds.</summary>
    public const int StudioTrimSeconds = 12;

    private const string RulesV2Needed = "This date was booked before that kind of work was offered here.";
    private static bool StudioRulesV2(PendingSession session) => session?.RulesVersion >= 2;
    private static StudioCutWork CutWorkFor(SessionCut cut) => cut.Work ??= new StudioCutWork();
    private static Musician StudioLead(SimulatedArtist act) => act?.members.FirstOrDefault(m => m.isActive && m.isLeadVocalist);
    private static float VoicePower(Musician m) => m == null ? .5f : m.axesVersion > 0 ? m.vocalPower : m.technicalSkill;

    // ---- Shape: key, tempo, intro and ending ----

    public static string ShapeName(StudioShapeChange change) => change switch {
        StudioShapeChange.LowerKey => "Lower the key", StudioShapeChange.RaiseKey => "Raise the key", StudioShapeChange.OriginalKey => "Back to the original key",
        StudioShapeChange.PushTempo => "Push the tempo", StudioShapeChange.EaseTempo => "Ease the tempo back", StudioShapeChange.OriginalTempo => "Back to the original tempo",
        StudioShapeChange.TrimEnds => "Shorten the intro and ending", _ => "Restore the full intro and ending"
    };

    private static int ShapeMinutes(StudioShapeChange change) => change switch {
        StudioShapeChange.LowerKey or StudioShapeChange.RaiseKey => StudioKeyMinutes,
        StudioShapeChange.OriginalKey => 10,
        StudioShapeChange.TrimEnds or StudioShapeChange.FullEnds => StudioTrimMinutes,
        _ => StudioTempoMinutes
    };

    /// <summary>The changes that make sense for this song now: never the one already in place.</summary>
    public IEnumerable<StudioShapeChange> StudioShapeOptions(int side) {
        if (!StudioSide(side, out var session, out var cut, out _) || !StudioRulesV2(session)) yield break;
        var work = cut.Work ?? new StudioCutWork();
        bool sung = cut.Structure?.Instrumental != true;
        if (sung && work.KeyShift != -1) yield return StudioShapeChange.LowerKey;
        if (sung && work.KeyShift != 1) yield return StudioShapeChange.RaiseKey;
        if (work.KeyShift != 0) yield return StudioShapeChange.OriginalKey;
        if (work.TempoShift != 1) yield return StudioShapeChange.PushTempo;
        if (work.TempoShift != -1) yield return StudioShapeChange.EaseTempo;
        if (work.TempoShift != 0) yield return StudioShapeChange.OriginalTempo;
        yield return work.Trimmed ? StudioShapeChange.FullEnds : StudioShapeChange.TrimEnds;
    }

    /// <summary>A song's structure reshaped for this date. Its parts stay as they are (located reads keep their meaning);
    /// the length and what the song asks of the voice change. Pure.</summary>
    public static StudioSongStructure ShapedStructure(StudioSongStructure baseShape, int keyShift, int tempoShift, bool trimmed) {
        if (baseShape == null) return null;
        var s = new StudioSongStructure {
            Version = baseShape.Version, Archetype = baseShape.Archetype, Form = baseShape.Form, Instrumental = baseShape.Instrumental, Inferred = baseShape.Inferred,
            PowerDemand = baseShape.PowerDemand, LyricDemand = baseShape.LyricDemand, EnsembleDemand = baseShape.EnsembleDemand, FeelDemand = baseShape.FeelDemand,
            Sections = baseShape.Sections.Select(x => new StudioSection { Kind = x.Kind, Label = x.Label, Final = x.Final }).ToList()
        };
        double seconds = baseShape.DurationSeconds * (tempoShift > 0 ? .92 : tempoShift < 0 ? 1.08 : 1) - (trimmed ? StudioTrimSeconds : 0);
        s.DurationSeconds = Math.Clamp((int)Math.Round(seconds), 60, 900);
        if (!s.Instrumental) s.PowerDemand = Math.Clamp(baseShape.PowerDemand * (keyShift < 0 ? .8f : keyShift > 0 ? 1.15f : 1f), .1f, 1);
        if (tempoShift > 0) s.EnsembleDemand = Math.Clamp(baseShape.EnsembleDemand + .05f, 0, 1);
        return s;
    }

    private static bool IsBallad(SongArchetype a) => a is SongArchetype.SlowBallad or SongArchetype.NashvilleBallad or SongArchetype.SaloonBallad or
        SongArchetype.LushStandard or SongArchetype.DeepSoulPleader or SongArchetype.LatinBolero or SongArchetype.QuietHymn or
        SongArchetype.ReverieMoodPiece or SongArchetype.SlowBlues or SongArchetype.DramaticBalladBigBuild;

    // A proposal is free: it reads the room and the clock, and draws nothing.
    public string StudioShapeProposal(int side, StudioShapeChange change) {
        if (!Enum.IsDefined(change)) return "Unknown change.";
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (!StudioRulesV2(session)) return RulesV2Needed;
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        var lead = StudioLead(act);
        string singer = lead?.firstName ?? "The singer";
        bool ballad = cut.Structure != null && IsBallad(cut.Structure.Archetype);
        string idea = change switch {
            StudioShapeChange.LowerKey => $"Producer: Take it down a step so {singer} isn't reaching for the top of the chorus. It should hold up late in the day; the record will sound a shade darker.",
            StudioShapeChange.RaiseKey => VoicePower(lead) >= .6f
                ? $"Producer: Put it up a step and let {singer}'s voice ring out. It's a showcase, and it is harder to sustain as the day goes on."
                : $"Producer: Up a step would brighten it, but {singer} will be at the edge of the range; expect strain late in the day.",
            StudioShapeChange.PushTempo => ballad ? "Producer: Push it along a little. It will be shorter and keep moving, but a ballad can lose its weight." : "Producer: Push the tempo. More drive and a shorter side, if the band can stay together at the faster count.",
            StudioShapeChange.EaseTempo => ballad ? "Producer: Ease it back and let it breathe. Longer, steadier, more room for the words." : "Producer: Ease it back so the band settles. It will be steadier and a bit longer, and it may lose some push.",
            StudioShapeChange.TrimEnds => $"Producer: Cut the intro to a couple of bars and stop it clean. About {StudioTrimSeconds} seconds shorter, and it gets to the hook sooner; there's less of the band's setup on the record.",
            StudioShapeChange.FullEnds => "Producer: Put the full intro and ending back as the act plays them.",
            _ => "Producer: Back to how the act plays it."
        };
        var (favors, resists) = StudioStance(act, change);
        int minutes = ShapeMinutes(change);
        var shaped = PreviewShape(cut, change);
        string length = cut.Structure != null && shaped != null && shaped.DurationSeconds != cut.Structure.DurationSeconds
            ? $" The side would run {StudioSongStructures.Length(shaped.DurationSeconds)} instead of {StudioSongStructures.Length(cut.Structure.DurationSeconds)}; a pass would take {StudioSongStructures.PassMinutes(shaped)} minutes." : "";
        return $"{idea}\n{StanceText(favors, resists)}\n{minutes} minutes to work it out. Earlier takes keep the shape they were cut in.{length} {StudioMinutesLeft(session)} minutes remain, {FinishMinutes(session)} reserved for finishing.";
    }

    private static StudioSongStructure PreviewShape(SessionCut cut, StudioShapeChange change) {
        var work = (cut.Work ?? new StudioCutWork()).Copy();
        ApplyShape(work, change);
        return ShapedStructure(work.BaseStructure ?? cut.Structure, work.KeyShift, work.TempoShift, work.Trimmed);
    }
    private static void ApplyShape(StudioCutWork work, StudioShapeChange change) {
        switch (change) {
            case StudioShapeChange.LowerKey: work.KeyShift = -1; break;
            case StudioShapeChange.RaiseKey: work.KeyShift = 1; break;
            case StudioShapeChange.OriginalKey: work.KeyShift = 0; break;
            case StudioShapeChange.PushTempo: work.TempoShift = 1; break;
            case StudioShapeChange.EaseTempo: work.TempoShift = -1; break;
            case StudioShapeChange.OriginalTempo: work.TempoShift = 0; break;
            case StudioShapeChange.TrimEnds: work.Trimmed = true; break;
            case StudioShapeChange.FullEnds: work.Trimmed = false; break;
        }
    }

    public bool ShapeStudio(int side, StudioShapeChange change, out string message) {
        if (!Enum.IsDefined(change)) { message = "Unknown change."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!StudioRulesV2(session)) { message = RulesV2Needed; return false; }
        if (!StudioShapeOptions(side).Contains(change)) { message = "That change doesn't apply to this song as it stands."; return false; }
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        var (favors, resists) = StudioStance(act, change);
        if (StudioRefuses(act, session, resists)) { message = "The act declines that change under its creative control. Earlier takes remain safe."; return false; }
        int minutes = ShapeMinutes(change);
        var shaped = PreviewShape(cut, change);
        if (StudioMinutesLeft(session) < minutes + StudioSongStructures.PassMinutes(shaped) + FinishMinutes(session)) {
            message = "There isn't time to work that out and hear it while protecting finishing. Earlier takes remain safe."; return false;
        }
        if (!SpendStudioWork(session, minutes, $"{cut.Choice.Title}: {ShapeName(change)}", out message, false)) return false;
        var work = CutWorkFor(cut);
        work.BaseStructure ??= cut.Structure;
        ApplyShape(work, change);
        cut.Structure = ShapedStructure(work.BaseStructure, work.KeyShift, work.TempoShift, work.Trimmed);
        // A new key or count unsettles what the band had drilled; the parts themselves are the same.
        if (change is StudioShapeChange.LowerKey or StudioShapeChange.RaiseKey or StudioShapeChange.PushTempo or StudioShapeChange.EaseTempo)
            cut.Rehearsals = cut.Rehearsals / 2;
        session.WorkloadMinutes += minutes * .5f;
        NoteResistance(session, resists, ShapeName(change).ToLowerInvariant());
        message = $"{ShapeName(change)}: prepared. Cut a pass to hear it; the earlier takes keep their own shape.";
        Changed?.Invoke(); return true;
    }

    // ---- Room balance ----

    public static string BalanceName(StudioBalance balance) => balance switch {
        StudioBalance.VocalForward => "Bring the vocal forward", StudioBalance.BandForward => "Open the band up", _ => "The room's natural blend"
    };

    public string StudioBalanceProposal(int side, StudioBalance balance) {
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (!StudioRulesV2(session)) return RulesV2Needed;
        bool crowded = cut.Takes.Count > 0 && cut.Takes[^1].EngineerDetail != null;
        string text = balance switch {
            StudioBalance.VocalForward => $"Engineer: Move the singer closer and turn the amps away from that mic.{(crowded ? " It's the fix for what crowded the last pass." : "")} The band's blend gets a little less natural.",
            StudioBalance.BandForward => "Engineer: Pull the room mics up and let the band fill the sound. More excitement, and the vocal and edges are harder to hear clearly.",
            _ => "Engineer: Put the microphones back where the room sounds most like the band."
        };
        return $"{text}\n{StudioBalanceMinutes} minutes to move things and check levels. Every earlier take keeps the balance it was cut with. This is a recording decision: on a mono tape it can't be changed afterwards.";
    }

    public bool BalanceStudio(int side, StudioBalance balance, out string message) {
        if (!Enum.IsDefined(balance)) { message = "Unknown balance."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!StudioRulesV2(session)) { message = RulesV2Needed; return false; }
        if ((cut.Work?.Balance ?? StudioBalance.Natural) == balance) { message = "The room is already set that way."; return false; }
        if (!SpendStudioWork(session, StudioBalanceMinutes, $"{cut.Choice.Title}: {BalanceName(balance)}", out message, false)) return false;
        CutWorkFor(cut).Balance = balance;
        message = $"{BalanceName(balance)}: the microphones are moved. The next pass will tell.";
        Changed?.Invoke(); return true;
    }

    // ---- Faults in the room ----

    public const string StudioBuzzFault = "buzz";
    public bool StudioHasFault(string fault) => pendingSession?.Work?.Faults.Contains(fault) == true;

    /// <summary>Find and fix the buzz: a cable, a valve, a ground. Time, no fee.</summary>
    public bool FixStudioFault(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Work == null || !session.Work.Faults.Contains(StudioBuzzFault)) { message = "Nothing in the room needs fixing."; return false; }
        if (!SpendStudioWork(session, StudioFixMinutes, "Found and fixed the amplifier buzz", out message, false)) return false;
        session.Work.Faults.Remove(StudioBuzzFault);
        message = "The engineer swapped a cable and the buzz is gone. Takes cut with it still have it.";
        Changed?.Invoke(); return true;
    }

    public bool KeepStudioPickup(int side, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (!StudioRulesV2(session)) { message = RulesV2Needed; return false; }
        if (!cut.Takes.Any(t => t.Work?.Incident == StudioIncident.CompellingPickup)) { message = "Nobody has played anything worth keeping yet."; return false; }
        if (cut.Work?.KeepPickup == true) { message = "It's already part of the arrangement."; return false; }
        if (!SpendStudioWork(session, 5, $"{cut.Choice.Title}: kept the pickup into the first verse", out message, false)) return false;
        CutWorkFor(cut).KeepPickup = true;
        message = "The pickup is in the arrangement now; the band will play it on every pass.";
        Changed?.Invoke(); return true;
    }

    // ---- Who favours and who resists a change ----

    /// <summary>Who in the act would welcome a change and who would resist it: the people whose part grows like it, the
    /// people whose part shrinks don't, and strong egos dislike gimmicks. Read-only.</summary>
    internal static (List<string> Favors, List<string> Resists) StudioStance(SimulatedArtist act, object change) {
        var favors = new List<string>(); var resists = new List<string>();
        if (act == null) return (favors, resists);
        foreach (var m in ActivePeople(act)) {
            bool guitar = MemberRoleService.Plays(m, MusicianRole.LeadGuitar) || MemberRoleService.Plays(m, MusicianRole.RhythmGuitar);
            bool keys = MemberRoleService.Plays(m, MusicianRole.Piano) || MemberRoleService.Plays(m, MusicianRole.Organ);
            bool horn = MemberRoleService.Plays(m, MusicianRole.Saxophone) || MemberRoleService.Plays(m, MusicianRole.Trumpet);
            bool drums = m.primaryRole == MusicianRole.Drums;
            int lean = change switch {
                StudioArrangement.PianoLed => keys ? 1 : m.primaryRole is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar && !keys ? -1 : 0,
                StudioArrangement.HornLine => horn ? 1 : m.primaryRole == MusicianRole.LeadGuitar && m.ego > .6f ? -1 : 0,
                StudioArrangement.Handclaps => m.ego > .7f ? -1 : m.primaryRole == MusicianRole.Drums ? 1 : 0,
                StudioArrangement.CallAndResponse => m.isLeadVocalist && m.ego > .65f ? -1 : MemberRoleService.SingsHarmony(m) ? 1 : 0,
                StudioArrangement.SimplerRhythm => drums || guitar && m.primaryRole == MusicianRole.RhythmGuitar ? -1 : 0,
                StudioArrangement.HalfTime => drums && m.ego > .5f ? -1 : m.isLeadVocalist ? 1 : 0,
                StudioShapeChange.LowerKey => m.isLeadVocalist ? (VoicePower(m) >= .65f && m.ego > .55f ? -1 : 1) : 0,
                StudioShapeChange.RaiseKey => m.isLeadVocalist ? (VoicePower(m) >= .6f ? 1 : -1) : 0,
                StudioShapeChange.PushTempo => drums ? 1 : m.isLeadVocalist && m.ego > .6f ? -1 : 0,
                StudioShapeChange.EaseTempo => drums && m.ego > .55f ? -1 : 0,
                StudioShapeChange.TrimEnds => m.primaryRole == MusicianRole.LeadGuitar && m.ego > .55f ? -1 : 0,
                _ => 0
            };
            if (lean > 0) favors.Add(m.firstName); else if (lean < 0) resists.Add(m.firstName);
        }
        return (favors, resists);
    }

    private static string StanceText(List<string> favors, List<string> resists) {
        string Names(List<string> n) => n.Count == 1 ? n[0] : string.Join(", ", n.Take(n.Count - 1)) + " and " + n[^1];
        if (favors.Count == 0 && resists.Count == 0) return "Nobody in the act feels strongly either way.";
        return (favors.Count > 0 ? $"{Names(favors)} {(favors.Count == 1 ? "likes" : "like")} it. " : "") +
            (resists.Count > 0 ? $"{Names(resists)} {(resists.Count == 1 ? "thinks" : "think")} it takes something away from the song." : "");
    }

    /// <summary>Creative control lets the act refuse: when the leader is against it and the room is already tense.</summary>
    private static bool StudioRefuses(SimulatedArtist act, PendingSession session, List<string> resists) =>
        act?.artistCreativeControl == true && session.Tension > .45f && act.members.Any(m => m.isActive && m.isBandLeader && resists.Contains(m.firstName));

    /// <summary>Going ahead over someone's objection costs a little goodwill in the room, logged by name.</summary>
    private static void NoteResistance(PendingSession session, List<string> resists, string what) {
        if (resists.Count == 0) return;
        session.Tension = Math.Min(1, session.Tension + Math.Min(.10f, .03f * resists.Count));
        session.ActionLog.Add($"{string.Join(" and ", resists)} went along with {what}, not happily.");
    }

    // ---- What a pass is like, and what happens in the room ----

    /// <summary>The effect of the song's shape, the balance, the room's open faults and a kept pickup on a pass.
    /// Zero for dates booked before rules v2.</summary>
    private static (float Vocal, float Ensemble, float Feel, float Capture) StudioShapeEffect(PendingSession session, SessionCut cut, SimulatedArtist act, float fatigue, SessionTake last) {
        if (!StudioRulesV2(session)) return (0, 0, 0, 0);
        var w = cut.Work ?? new StudioCutWork();
        float vocal = 0, ensemble = 0, feel = 0, capture = 0;
        var baseShape = w.BaseStructure ?? cut.Structure;
        bool ballad = baseShape != null && IsBallad(baseShape.Archetype);
        float power = VoicePower(StudioLead(act));
        if (w.KeyShift < 0) { vocal += .015f + .05f * fatigue * (baseShape?.PowerDemand ?? .5f); feel -= .015f; }
        else if (w.KeyShift > 0) { vocal += .08f * (power - .6f) - .04f * fatigue; feel += .01f; }
        if (w.TempoShift > 0) { feel += ballad ? -.02f : .02f; ensemble -= .02f; }
        else if (w.TempoShift < 0) { ensemble += .02f; feel += ballad ? .01f : -.015f; }
        if (w.Trimmed) feel -= .005f;
        if (w.KeepPickup) feel += .015f;
        bool crowded = last?.EngineerDetail != null;
        switch (w.Balance) {
            case StudioBalance.VocalForward: capture += crowded ? .035f : .012f; vocal += .01f; feel -= .015f; break;
            case StudioBalance.BandForward: feel += .02f; capture -= .02f; if (cut.Structure?.Instrumental != true) vocal -= .01f; break;
        }
        if (session.Work?.Faults.Contains(StudioBuzzFault) == true) capture -= .04f;
        // How the date goes on tape: a separated vocal is cleaner to balance, but the singer is behind glass.
        if (session.Work != null && session.Work.Layout != StudioLayout.Together) {
            capture += .025f + .02f * (session.Room.EngineerHands - .5f);
            feel -= .02f * (1 - session.Room.EngineerHands);
        }
        return (vocal, ensemble, feel, capture);
    }

    /// <summary>Small things that happen on a complete pass, perhaps: a buzz, a broken string, a late entrance, or a
    /// gift. Keyed per take; at most one; never on dates before rules v2. Returns what the incident does to the pass.</summary>
    private (float Vocal, float Ensemble, float Feel, float Capture) StudioIncidentRoll(PendingSession session, SessionCut cut, SimulatedArtist act, SessionTake take, float fatigue, string key) {
        if (!StudioRulesV2(session) || take.Incomplete || cut.Structure == null) return (0, 0, 0, 0);
        float chance = .08f + .06f * fatigue + .04f * session.Tension;
        if (StudioTakeEvidence.Unit(key + "|incident") >= chance) return (0, 0, 0, 0);
        var people = ActivePeople(act);
        Musician Pick(Func<Musician, bool> fits, string purpose) {
            var list = people.Where(fits).OrderBy(m => m.personId, StringComparer.Ordinal).ToList();
            return list.Count == 0 ? null : list[(int)(StudioTakeEvidence.Unit(key + "|" + purpose) * list.Count) % list.Count];
        }
        var electric = Pick(m => m.primaryRole is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar or MusicianRole.Bass or MusicianRole.Organ, "amp");
        var strings = Pick(m => m.primaryRole is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar or MusicianRole.Bass or MusicianRole.Violin, "string");
        var anyone = Pick(m => !m.isLeadVocalist, "player");
        var hummer = Pick(m => !m.isLeadVocalist && SessionEmploymentService.BackingVoice(m) >= .40f, "hum");
        var options = new List<(StudioIncident Kind, float Weight, Musician Who)>();
        if (electric != null && session.Work?.Faults.Contains(StudioBuzzFault) != true) options.Add((StudioIncident.AmpBuzz, .25f, electric));
        if (strings != null && session.BreakdownVersion > 0) options.Add((StudioIncident.BrokenString, .20f, strings));
        if (anyone != null) options.Add((StudioIncident.LateEntrance, .25f, anyone));
        if (anyone != null) options.Add((StudioIncident.CompellingPickup, .15f, anyone));
        if (hummer != null && !cut.Structure.Instrumental) options.Add((StudioIncident.UnexpectedHarmony, .15f, hummer));
        if (options.Count == 0) return (0, 0, 0, 0);
        double pick = StudioTakeEvidence.Unit(key + "|incident-kind") * options.Sum(o => o.Weight);
        var chosen = options[^1];
        foreach (var o in options) { pick -= o.Weight; if (pick < 0) { chosen = o; break; } }
        var w = take.Work;
        w.Incident = chosen.Kind; w.IncidentPersonId = chosen.Who.personId;
        string name = chosen.Who.firstName;
        string section = cut.Structure.Sections[(int)(StudioTakeEvidence.Unit(key + "|incident-section") * cut.Structure.Sections.Count) % cut.Structure.Sections.Count].Label;
        switch (chosen.Kind) {
            case StudioIncident.AmpBuzz:
                session.Work.Faults.Add(StudioBuzzFault);
                w.IncidentNote = $"Engineer: There's a buzz from {name}'s amplifier on the tape. It will be on every pass until we find it.";
                return (0, 0, 0, -.04f);
            case StudioIncident.BrokenString:
                take.Incomplete = true; take.FalseStart = true; take.TroubleSection = -1;
                take.Minutes = Math.Min(take.Minutes, 4) + 4;
                w.IncidentNote = $"{name} broke a string in {section}; the band stopped while it was changed.";
                take.ProducerDetail = w.IncidentNote;
                return (0, 0, 0, 0);
            case StudioIncident.LateEntrance:
                w.IncidentNote = $"{name} came in late after {section}; it's audible, but the rest of the pass held.";
                return (0, -.03f, 0, 0);
            case StudioIncident.CompellingPickup:
                w.IncidentNote = $"{name} played a little pickup into the first verse that nobody had planned, and everyone liked it.";
                return (0, 0, .03f, 0);
            default:
                w.IncidentNote = $"{name} started singing a harmony under the last chorus without being asked, and it worked.";
                return (.02f, 0, .01f, 0);
        }
    }

    // ---- Ideas from the room ----

    public const string StudioIdeaDeclinePrefix = "idea:";

    /// <summary>Ideas people in the room have for this song right now: the act's own players, the producer after a part
    /// keeps failing or the band has played it over and over, the engineer after a crowded pass, a hired producer. At
    /// most two, read-only, and the same on every refresh.</summary>
    public List<StudioIdea> StudioIdeas(int side) {
        var ideas = new List<StudioIdea>();
        if (!StudioSide(side, out var session, out var cut, out _) || !StudioRulesV2(session) || cut.Structure == null) return ideas;
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        var people = ActivePeople(act);
        var w = cut.Work ?? new StudioCutWork();
        var complete = cut.Takes.Where(t => !t.Incomplete && t.Work?.Erased != true).ToList();
        void Add(string id, string from, string text, StudioArrangement? arrangement = null, StudioShapeChange? shape = null, StudioBalance? balance = null, bool pickup = false) {
            if (ideas.Count >= 2 || session.DeclinedCallIns.Contains($"{StudioIdeaDeclinePrefix}{side}:{id}")) return;
            if (arrangement == cut.Arrangement || shape != null && !StudioShapeOptions(side).Contains(shape.Value) || balance == w.Balance) return;
            var (favors, resists) = StudioStance(act, (object)arrangement ?? shape ?? (object)balance ?? "pickup");
            ideas.Add(new StudioIdea { Id = id, From = from, Text = text, Arrangement = arrangement, Shape = shape, Balance = balance, Pickup = pickup, Favors = favors, Resists = resists });
        }
        var pickupTake = cut.Takes.LastOrDefault(t => t.Work?.Incident == StudioIncident.CompellingPickup);
        if (pickupTake != null && !w.KeepPickup) {
            string who = act.members.FirstOrDefault(m => m.personId == pickupTake.Work.IncidentPersonId)?.firstName ?? "Someone";
            Add("pickup", who, $"{who}: That pickup into the first verse on take {pickupTake.Number}? I could play it every time.", pickup: true);
        }
        int strained = cut.Takes.Count(t => t.TroubleSection >= 0 && t.ProducerDetail != null && (t.ProducerDetail.Contains("strain") || t.ProducerDetail.Contains("reach") || t.ProducerDetail.Contains("nothing left") || t.ProducerDetail.Contains("wavers")));
        if (strained >= 2 && w.KeyShift >= 0) Add("lower-key", "Producer", $"Producer: That's {strained} passes where the top of it is a reach. Take it down a step?", shape: StudioShapeChange.LowerKey);
        var failed = cut.Takes.Where(t => t.Incomplete && !t.FalseStart && t.TroubleSection >= 0).GroupBy(t => cut.Structure.Sections[t.TroubleSection].Kind).FirstOrDefault(g => g.Count() >= 2);
        if (failed != null && cut.Arrangement == StudioArrangement.Live)
            Add("simpler", "Producer", $"Producer: The {StudioSongStructures.PartName(failed.Key)} has fallen apart twice. A simpler figure there might hold together.", StudioArrangement.SimplerRhythm);
        if (complete.Count > 0 && complete[^1].EngineerDetail != null && w.Balance == StudioBalance.Natural)
            Add("vocal-forward", session.Room.EngineerName ?? "Engineer", $"{session.Room.EngineerName ?? "Engineer"}: {complete[^1].EngineerDetail} Let me move the singer in closer.", balance: StudioBalance.VocalForward);
        var harmony = cut.Takes.LastOrDefault(t => t.Work?.Incident == StudioIncident.UnexpectedHarmony);
        var singer = people.Where(m => !m.isLeadVocalist && MemberRoleService.SingsHarmony(m)).OrderByDescending(m => m.creativity).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if ((harmony != null || singer != null && StudioBackingCall(cut.Structure, cut.Choice.WrittenSong?.Genre ?? act.primaryGenre) >= StudioBackingCallBar) &&
            StudioHarmonyVoiceOn(session, act) >= 0) {
            string who = harmony != null ? act.members.FirstOrDefault(m => m.personId == harmony.Work.IncidentPersonId)?.firstName ?? singer?.firstName : singer.firstName;
            who ??= "Someone";
            Add("answers", who, $"{who}: Let us sing answers to the lead in the choruses.", StudioArrangement.CallAndResponse);
        }
        var pianist = people.Where(m => m.creativity >= .55f && (MemberRoleService.Plays(m, MusicianRole.Piano) || MemberRoleService.Plays(m, MusicianRole.Organ)) &&
            people.Any(x => x.primaryRole is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar)).OrderByDescending(m => m.creativity).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if (pianist != null && complete.Count >= 1)
            Add("piano", pianist.firstName, $"{pianist.firstName}: I've found an answering figure on the piano. Let me carry it instead of the busy guitar part.", StudioArrangement.PianoLed);
        var hornPlayer = people.FirstOrDefault(m => MemberRoleService.Plays(m, MusicianRole.Saxophone) || MemberRoleService.Plays(m, MusicianRole.Trumpet));
        if (hornPlayer != null && complete.Count >= 1)
            Add("horns", hornPlayer.firstName, $"{hornPlayer.firstName}: Give me a line to blow behind the chorus.", StudioArrangement.HornLine);
        // Playing the same arrangement over and over makes people restless.
        int sameWay = complete.Count(t => t.Arrangement == cut.Arrangement && t.ArrangementRevision == cut.ArrangementRevision);
        var restless = people.Where(m => !m.isLeadVocalist).OrderByDescending(m => m.creativity).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
        if (sameWay >= 4 && restless != null)
            Add("restless", restless.firstName, $"{restless.firstName}: We've played it the same way {sameWay} times. Let's put handclaps on the backbeat and wake it up.", StudioArrangement.Handclaps);
        if (session.Hires.FirstOrDefault(h => h.Kind == StudioHireKind.Producer) is { } producer && complete.Count >= 2 && cut.KeptTake < 0)
            Add("producer-tempo", producer.Name, $"{producer.Name}: It's dragging. Push the count.", shape: StudioShapeChange.PushTempo);
        return ideas;
    }

    /// <summary>Take an idea up: the same action, time and cost as asking for it directly.</summary>
    public bool TakeStudioIdea(int side, string id, out string message) {
        var idea = StudioIdeas(side).FirstOrDefault(i => i.Id == id);
        if (idea == null) { message = "That idea isn't on the table any more."; return false; }
        if (idea.Pickup) return KeepStudioPickup(side, out message);
        if (idea.Arrangement is { } a) return ArrangeStudio(side, a, out message);
        if (idea.Shape is { } s) return ShapeStudio(side, s, out message);
        if (idea.Balance is { } b) return BalanceStudio(side, b, out message);
        message = "Nothing to do."; return false;
    }

    /// <summary>Pass on an idea for this side. Saying no is free; the person who offered it may feel it.</summary>
    public bool PassOnStudioIdea(int side, string id, out string message) {
        var session = pendingSession;
        var idea = StudioIdeas(side).FirstOrDefault(i => i.Id == id);
        if (session == null || idea == null) { message = "That idea isn't on the table any more."; return false; }
        session.DeclinedCallIns.Add($"{StudioIdeaDeclinePrefix}{side}:{id}");
        var act = ArtistManager.Instance.GetArtist(session.ArtistId);
        if (act?.members.FirstOrDefault(m => m.isActive && m.firstName == idea.From) is { } person && person.ego > .6f) {
            session.Tension = Math.Min(1, session.Tension + .02f);
            session.ActionLog.Add($"Passed on {person.firstName}'s idea; {person.firstName} went quiet for a while.");
        } else session.ActionLog.Add($"Passed on {idea.From}'s idea.");
        message = "Noted. The song stays as it is.";
        Changed?.Invoke(); return true;
    }

    // ---- Tape stock ----

    /// <summary>A reel of stock: about four full passes of a single at 15 ips. Provisional 1960 price.</summary>
    public const int StudioReelPasses = 4;
    public const float StudioReelPrice = 8f;
    /// <summary>The stock the quote includes: four passes a side and two spare, or enough for passes over most of the booked
    /// time, whichever is more. Running out takes a long run of passes; then it is a visible choice.</summary>
    public static int StudioStockIncluded(int sides, int bookedHours, double passMinutes) =>
        Math.Max(4 * sides + 2, (int)Math.Round(.6 * bookedHours * 60 / Math.Max(6, passMinutes)));
    public static int StudioStockLeft(PendingSession session) => session?.Work == null ? int.MaxValue : session.Work.StockPasses + session.Work.StockBought - session.Work.PassesUsed;

    public bool BuyStudioStock(out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || session.Work == null) { message = "No date with a stock allowance."; return false; }
        if (Label.cashReserves < StudioReelPrice) { message = $"Another reel costs ${StudioReelPrice:N0}."; return false; }
        if (!RequireAtStudio(session, out message)) return false;
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        Label.cashReserves -= StudioReelPrice; Label.monthlyExpenses += StudioReelPrice;
        if (artist != null) artist.unrecoupedAdvance += StudioReelPrice;   // stock is studio cost, recouped like the room
        session.Cost += StudioReelPrice; session.Work.StockBought += StudioReelPasses;
        session.ActionLog.Add($"Bought another reel of tape (${StudioReelPrice:N0}, about {StudioReelPasses} passes).");
        message = $"Another reel: room for about {StudioReelPasses} more passes."; Changed?.Invoke(); return true;
    }

    /// <summary>Whether this take's tape can be recorded over: not kept, not the source of a kept finish or another take.</summary>
    public static bool StudioTakeReusable(SessionCut cut, int index) {
        if (index < 0 || index >= cut.Takes.Count || index == cut.KeptTake) return false;
        var take = cut.Takes[index];
        if (take.Work == null || take.Work.Erased || take.Work.Derivation == StudioDerivation.Splice) return false;
        return !cut.Takes.Any(t => t.Work?.SourceTakeId == take.TakeId || t.Work?.SecondSourceTakeId == take.TakeId) &&
            !cut.FinishWork.Any(f => f.TakeId == take.TakeId);
    }

    /// <summary>Record over a take you don't need, to make room for another pass. The take is gone for good: its notes stay
    /// in the log, it can never be kept.</summary>
    public bool ReuseStudioTape(int side, int index, out string message) {
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (session.Work == null) { message = RulesV2Needed; return false; }
        if (!StudioTakeReusable(cut, index)) { message = "That take is kept, used for another take, or already gone. Choose another."; return false; }
        var take = cut.Takes[index];
        take.Work.Erased = true;
        session.Work.PassesUsed = Math.Max(0, session.Work.PassesUsed - 1);
        session.ActionLog.Add($"Recorded over {cut.Choice.Title}, take {take.Number}, to free tape.");
        message = $"Take {take.Number} will be recorded over. That pass is gone."; Changed?.Invoke(); return true;
    }

    // ---- Validation ----

    /// <summary>Saved per-take work is consistent: only on rules-v2 dates, sources point at earlier takes on the same tape,
    /// and nothing kept was recorded over.</summary>
    internal static bool StudioTakeWorkValid(PendingSession session, IReadOnlyList<SessionTake> takes, int kept) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < takes.Count; i++) {
            var w = takes[i].Work;
            if (w != null && (session.RulesVersion < 2 || !w.Valid || w.SourceTakeId != null && !seen.Contains(w.SourceTakeId) ||
                w.SecondSourceTakeId != null && !seen.Contains(w.SecondSourceTakeId) || w.Erased && i == kept)) return false;
            if (takes[i].TakeId != null) seen.Add(takes[i].TakeId);
        }
        return true;
    }
}

public partial class PlayerDesk {
    /// <summary>The structure of the song as this take was cut: the date's base shape with the take's own key, tempo and ends.</summary>
    public static StudioSongStructure StudioStructureOf(SessionCut cut, SessionTake take) =>
        take?.Work != null && cut.Work?.BaseStructure != null ? ShapedStructure(cut.Work.BaseStructure, take.Work.KeyShift, take.Work.TempoShift, take.Work.Trimmed) : cut.Structure;

    /// <summary>How a take was set up and made, in words, for its card and its master's history. Empty for older takes.</summary>
    public static List<string> StudioTakeSetup(PendingSession session, SessionTake take) {
        var lines = new List<string>();
        var w = take?.Work;
        if (w == null) return lines;
        if (w.KeyShift != 0) lines.Add(w.KeyShift < 0 ? "a step lower" : "a step higher");
        if (w.TempoShift != 0) lines.Add(w.TempoShift > 0 ? "tempo pushed" : "tempo eased back");
        if (w.Trimmed) lines.Add("short intro and clean ending");
        if (w.KeepPickup) lines.Add("with the pickup into the first verse");
        if (w.Balance != StudioBalance.Natural) lines.Add(w.Balance == StudioBalance.VocalForward ? "vocal miked forward" : "band opened up");
        if (w.Layout != StudioLayout.Together) lines.Add(StudioLayoutName(w.Layout).ToLowerInvariant());
        if (w.Incident != StudioIncident.None && w.IncidentNote != null) lines.Add(w.IncidentNote);
        string Source(string id) => session?.Cuts.SelectMany(c => c.Takes).FirstOrDefault(t => t.TakeId == id) is { } s ? $"take {s.Number}" : "an earlier take";
        if (w.Derivation == StudioDerivation.VocalOverdub) lines.Add($"new vocal over the backing of {Source(w.SourceTakeId)}");
        if (w.Derivation == StudioDerivation.PartOverdub) lines.Add($"added to {Source(w.SourceTakeId)}");
        if (w.Derivation == StudioDerivation.Splice) lines.Add($"spliced: {Source(w.SourceTakeId)} into {Source(w.SecondSourceTakeId)}");
        foreach (var part in w.Parts ?? new()) lines.Add(part switch {
            StudioOverdubPart.BackingVoices => "backing voices overdubbed", StudioOverdubPart.Handclaps => "handclaps overdubbed", _ => "horn line overdubbed" });
        if (w.Erased) lines.Add("recorded over");
        return lines;
    }
}

public partial class PlayerDesk {
    /// <summary>Put a song first in the date's order, before anything is cut: the producer works in this order, and the
    /// band is freshest for the first song. Free; once tape has rolled the order is what was played.</summary>
    public bool CutStudioSideFirst(int side, out string message) {
        var session = pendingSession;
        if (session?.FlowVersion != 1 || side <= 0 || side >= session.Cuts.Count) { message = "No such side to move."; return false; }
        if (session.Cuts.Any(c => c.Takes.Count > 0)) { message = "Tape has rolled; the order is what you played."; return false; }
        var cut = session.Cuts[side];
        session.Cuts.RemoveAt(side); session.Cuts.Insert(0, cut);
        session.ActionLog.Add($"Running order: \"{cut.Choice.Title}\" first.");
        message = $"\"{cut.Choice.Title}\" goes first, while everyone is fresh."; Changed?.Invoke(); return true;
    }
}
