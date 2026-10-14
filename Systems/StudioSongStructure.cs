using System;
using System.Collections.Generic;
using System.Linq;

public enum StudioSectionKind { Intro, Verse, Chorus, MiddleEight, Break, Ending }
public enum StudioAspect { Power, Lyric, Ensemble, Feel }

public sealed class StudioSection {
    public StudioSectionKind Kind;
    public string Label;
    public bool Final;
}

/// <summary>The playable shape of a song on a studio date: its parts, length and what it asks of the performers.
/// Derived from the composition's archetype and saved form, or from the act's genre for an original that has no
/// composition until printing. Derivation is pure and keyed; it never draws gameplay randomness.</summary>
public sealed class StudioSongStructure {
    public int Version = 1;
    public SongArchetype Archetype;
    public SongForm Form;
    public bool Instrumental, Inferred;
    public int DurationSeconds;
    public float PowerDemand, LyricDemand, EnsembleDemand, FeelDemand;
    public List<StudioSection> Sections = new();

    public float Demand(StudioAspect aspect) => aspect switch {
        StudioAspect.Power => PowerDemand, StudioAspect.Lyric => LyricDemand, StudioAspect.Ensemble => EnsembleDemand, _ => FeelDemand
    };
    public IEnumerable<StudioSectionKind> Parts => Sections.Select(s => s.Kind).Distinct().OrderBy(k => k);
    public bool Valid => Version == 1 && Enum.IsDefined(Archetype) && Enum.IsDefined(Form) && DurationSeconds is >= 60 and <= 900 &&
        new[] { PowerDemand, LyricDemand, EnsembleDemand, FeelDemand }.All(d => float.IsFinite(d) && d >= 0 && d <= 1) &&
        Sections is { Count: > 0 } && Sections.All(s => s != null && Enum.IsDefined(s.Kind) && !string.IsNullOrEmpty(s.Label));
}

public static class StudioSongStructures {
    // How much each part of a song asks of each aspect of a performance (power, lyric, ensemble, feel).
    private static readonly float[][] Weights = {
        new[] { 0f, 0f, .55f, .45f },     // Intro
        new[] { .15f, .55f, .10f, .20f }, // Verse
        new[] { .45f, .10f, .30f, .15f }, // Chorus
        new[] { .30f, .15f, .40f, .15f }, // Middle eight
        new[] { 0f, 0f, .45f, .55f },     // Break
        new[] { .20f, 0f, .65f, .15f }    // Ending
    };
    private static readonly float[] Prominence = { .6f, 1f, 1f, 1f, .8f, .8f };
    public static float Weight(StudioSectionKind kind, StudioAspect aspect) => Weights[(int)kind][(int)aspect];

    /// <summary>Early-sixties single lengths by archetype, in seconds. Pop sides 2:10-2:40, ballads near three
    /// minutes, workouts and extended pieces beyond. Provisional authored ranges, not a researched catalogue.</summary>
    private static (int Low, int High) LengthRange(SongArchetype archetype) => archetype switch {
        SongArchetype.SlowBallad or SongArchetype.NashvilleBallad or SongArchetype.SaloonBallad or SongArchetype.LushStandard or
        SongArchetype.DeepSoulPleader or SongArchetype.LatinBolero or SongArchetype.QuietHymn or SongArchetype.ReverieMoodPiece or
        SongArchetype.SlowBlues => (165, 195),
        SongArchetype.DramaticBalladBigBuild or SongArchetype.ChamberBaroquePiece or SongArchetype.LightAndShadeEpic or
        SongArchetype.SpiritualShout or SongArchetype.GospelChoir => (175, 215),
        SongArchetype.JamExtendedWorkout or SongArchetype.FunkWorkout or SongArchetype.ModernJazzInstrumental or SongArchetype.RagaModalDrone or
        SongArchetype.SuiteMultiPart or SongArchetype.Medley or SongArchetype.LivePartyRecord or SongArchetype.Collage or SongArchetype.SpokenWord => (200, 330),
        SongArchetype.ClassicalOrchestral or SongArchetype.ClassicalChamber or SongArchetype.ClassicalSolo => (240, 420),
        SongArchetype.MidTempoRocker or SongArchetype.ShuffleTwelveBar or SongArchetype.GrooveRiffVamp or SongArchetype.VerseDrivenSong or
        SongArchetype.ProtestMessageSong or SongArchetype.HornDrivenSoulNumber or SongArchetype.GospelQuartet or SongArchetype.WesternSwing or
        SongArchetype.Swinger or SongArchetype.CountryWaltz => (140, 175),
        _ => (130, 160)
    };

    /// <summary>How much longer the period's singles ran than in 1960-64: steady to 1964, then about four percent a year
    /// (three-minute sides were common by 1967 and much longer ones charted by 1968). Provisional authored drift.</summary>
    public static float PeriodLength(int year) => 1f + .04f * Math.Clamp(year - 1964, 0, 6);

    public static StudioSongStructure For(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, int year = 1960) {
        var table = PolarSongTable.Current;
        var song = string.IsNullOrEmpty(choice?.SongId) ? null : CompositionCatalogService.GetSong(choice.SongId);
        string key = $"structure:{choice?.SongId ?? (artist?.artistId + ":" + choice?.Title)}";
        var structure = new StudioSongStructure();
        float[] axes; SongForm form; float? authoredLength = null; bool vocalAbsent = false;
        if (song != null) {
            var taxonomy = song.demoTaxonomy;
            var profile = SongProfileDeriver.Derive(song, taxonomy, table);
            structure.Archetype = profile.archetype; structure.Inferred = profile.inferredTaxonomy;
            axes = profile.axes;
            form = taxonomy?.formOverride ?? song.defaultForm;
            authoredLength = taxonomy?.durationSeconds;
            vocalAbsent = taxonomy?.vocalPresence == SongVocalPresence.Instrumental;
        } else {
            // An original is not a composition until it is printed. Use the genre's authored song shapes,
            // picked by a stable private key so the same number always has the same shape.
            var genre = choice?.WrittenSong?.Genre ?? artist?.primaryGenre ?? Genre.TraditionalPop;
            (structure.Archetype, form, vocalAbsent) = GenreDefault(table, genre, key);
            structure.Inferred = true;
            axes = table.Row(structure.Archetype).Axes;
        }
        if (form == SongForm.Unknown) form = FormFor(structure.Archetype);
        structure.Form = form;
        structure.Instrumental = vocalAbsent || artist?.instrumentalPerformance == true ||
            axes[(int)SongAxis.VocalPower] == 0 && axes[(int)SongAxis.VocalNuance] == 0 && axes[(int)SongAxis.LyricDelivery] == 0;
        structure.PowerDemand = structure.Instrumental ? 0 : Math.Clamp(Math.Max(axes[(int)SongAxis.VocalPower], axes[(int)SongAxis.VocalNuance]), .2f, 1);
        structure.LyricDemand = structure.Instrumental ? 0 : Math.Clamp(axes[(int)SongAxis.LyricDelivery], .2f, 1);
        structure.EnsembleDemand = Math.Clamp(axes[(int)SongAxis.Ensemble], .2f, 1);
        structure.FeelDemand = Math.Clamp(axes[(int)SongAxis.Musicianship], .2f, 1);
        var (low, high) = LengthRange(structure.Archetype);
        structure.DurationSeconds = authoredLength is > 60 and < 900 ? (int)Math.Round(authoredLength.Value)
            : (int)Math.Round((low + StudioTakeEvidence.Unit(key + "|length") * (high - low)) * PeriodLength(year));
        structure.Sections = Build(form, structure.DurationSeconds, structure.Instrumental);
        return structure;
    }

    private static (SongArchetype, SongForm, bool) GenreDefault(PolarSongTable table, Genre genre, string key) {
        var fallback = Enum.Parse<SongArchetype>(table.Prior(genre).Fallback);
        if (table.GenreForms == null || !table.GenreForms.TryGetValue(genre.ToString(), out var spec)) return (fallback, SongForm.Unknown, false);
        var rows = spec.Songbook != null && PolarRepertoireTable.Current.Assignments.TryGetValue(spec.Songbook, out var book)
            ? book.Select(r => (Archetype: r.Archetype, r.Weight, Form: r.Form, Instrumental: r.VocalPresence == nameof(SongVocalPresence.Instrumental))).ToArray()
            : (spec.Forms ?? new()).OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (Archetype: kv.Key, Weight: kv.Value, Form: (string)null, Instrumental: false)).ToArray();
        if (rows.Length == 0) return (fallback, SongForm.Unknown, false);
        double pick = StudioTakeEvidence.Unit(key + "|shape") * rows.Sum(r => r.Weight);
        foreach (var row in rows) {
            pick -= row.Weight;
            if (pick <= 0) return (Enum.Parse<SongArchetype>(row.Archetype), row.Form == null ? SongForm.Unknown : Enum.Parse<SongForm>(row.Form), row.Instrumental);
        }
        var last = rows[^1];
        return (Enum.Parse<SongArchetype>(last.Archetype), last.Form == null ? SongForm.Unknown : Enum.Parse<SongForm>(last.Form), last.Instrumental);
    }

    /// <summary>The usual form of an archetype in the authored songbooks; a plain verse-chorus otherwise.</summary>
    public static SongForm FormFor(SongArchetype archetype) {
        string name = archetype.ToString();
        var form = PolarRepertoireTable.Current.Assignments.Values.SelectMany(rows => rows).Where(r => r.Archetype == name)
            .GroupBy(r => r.Form).OrderByDescending(g => g.Sum(r => r.Weight)).ThenBy(g => g.Key, StringComparer.Ordinal).FirstOrDefault()?.Key;
        return form != null ? Enum.Parse<SongForm>(form) : archetype switch {
            SongArchetype.ClassicalOrchestral or SongArchetype.ClassicalChamber or SongArchetype.ClassicalSolo or
            SongArchetype.SuiteMultiPart or SongArchetype.LightAndShadeEpic or SongArchetype.Collage => SongForm.ThroughComposed,
            _ => SongForm.VerseChorus
        };
    }

    private static List<StudioSection> Build(SongForm form, int seconds, bool instrumental) {
        var list = new List<StudioSection>();
        void Add(StudioSectionKind kind, string sung, string played = null, bool final = false) =>
            list.Add(new StudioSection { Kind = kind, Label = instrumental && played != null ? played : sung, Final = final });
        Add(StudioSectionKind.Intro, form == SongForm.ThroughComposed ? "the opening" : "the intro");
        switch (form) {
            case SongForm.VerseChorus:
                Add(StudioSectionKind.Verse, "the first verse", "the first theme");
                Add(StudioSectionKind.Chorus, "the first chorus", "the first hook");
                Add(StudioSectionKind.Verse, "the second verse", "the second theme");
                Add(StudioSectionKind.Chorus, "the second chorus", "the second hook");
                if (seconds >= 150) Add(StudioSectionKind.MiddleEight, "the bridge");
                Add(StudioSectionKind.Chorus, "the last chorus", "the last hook", true);
                break;
            case SongForm.TwelveBar:
                Add(StudioSectionKind.Verse, "the first verse", "the first chorus");
                Add(StudioSectionKind.Verse, "the second verse", "the second chorus");
                Add(StudioSectionKind.Break, "the solo");
                if (seconds >= 170) Add(StudioSectionKind.Verse, "the third verse", "the third chorus");
                Add(StudioSectionKind.Verse, "the last verse", "the last chorus", true);
                break;
            case SongForm.Strophic:
                Add(StudioSectionKind.Verse, "the first verse", "the first strain");
                Add(StudioSectionKind.Verse, "the second verse", "the second strain");
                if (seconds >= 150) Add(StudioSectionKind.Verse, "the third verse", "the third strain");
                Add(StudioSectionKind.Verse, "the last verse", "the last strain", true);
                break;
            case SongForm.ThroughComposed:
                Add(StudioSectionKind.Verse, "the first section");
                Add(StudioSectionKind.MiddleEight, "the middle section");
                Add(StudioSectionKind.Chorus, "the climax", final: true);
                break;
            default: // AABA, the period's common song form, and the default for an unknown shape.
                Add(StudioSectionKind.Verse, "the first verse", "the first theme");
                Add(StudioSectionKind.Verse, "the second verse", "the second theme");
                Add(StudioSectionKind.MiddleEight, "the middle eight", "the bridge");
                if (seconds >= 190) {
                    Add(StudioSectionKind.Verse, "the third verse", "the third theme");
                    Add(StudioSectionKind.Break, "the instrumental break", "the solo");
                    Add(StudioSectionKind.MiddleEight, "the second middle eight", "the second bridge");
                }
                Add(StudioSectionKind.Verse, "the last verse", "the closing theme", true);
                break;
        }
        Add(StudioSectionKind.Ending, form == SongForm.ThroughComposed ? "the close" : "the ending");
        return list;
    }

    public static string FormName(SongForm form) => form switch {
        SongForm.Aaba => "AABA", SongForm.VerseChorus => "verse-chorus", SongForm.TwelveBar => "twelve-bar",
        SongForm.Strophic => "strophic", SongForm.ThroughComposed => "through-composed", _ => "song"
    };
    public static string PartName(StudioSectionKind kind) => kind switch {
        StudioSectionKind.Intro => "intro", StudioSectionKind.Verse => "verses", StudioSectionKind.Chorus => "chorus",
        StudioSectionKind.MiddleEight => "middle eight", StudioSectionKind.Break => "break", _ => "ending"
    };
    public static string Length(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

    /// <summary>A pass is the count-in, the performance, rewinding and one listen back: about twice the song plus a reset.</summary>
    public static int PassMinutes(StudioSongStructure s) => s == null ? PlayerDesk.StudioTakeMinutes
        : Math.Clamp((int)Math.Round(2 * s.DurationSeconds / 60.0 + 6), 9, 25);
    /// <summary>Running the whole song down a few times without tape.</summary>
    public static int RehearsalMinutes(StudioSongStructure s) => s == null ? PlayerDesk.StudioRehearsalMinutes
        : Math.Max(PlayerDesk.StudioRehearsalMinutes, (int)Math.Round(3 * s.DurationSeconds / 60.0 + 6));

    /// <summary>Practising one part improves the aspects that part demands, within bounds.</summary>
    public static float PartLift(StudioSongStructure s, IReadOnlyList<int> parts, StudioAspect aspect) {
        if (s == null || parts == null) return 0;
        float lift = 0;
        foreach (var kind in s.Parts) lift += (kind < (StudioSectionKind)parts.Count ? parts[(int)kind] : 0) * Weight(kind, aspect) * .04f;
        return Math.Min(.05f, lift);
    }

    /// <summary>Where this pass is weakest, as heard. The take's own aspects point at the part that demands them most;
    /// rehearsal and direction make a part less likely to fail; producer craft bounds how well the read locates it.</summary>
    public static (int Section, StudioAspect Aspect, float Severity) Locate(StudioSongStructure s, PlayerDesk.SessionTake take,
        IReadOnlyList<int> parts, StudioDirection direction, float fatigue, float craft, string key) {
        float Value(StudioAspect a) => a switch { StudioAspect.Ensemble => take.Ensemble, StudioAspect.Feel => take.Feel, _ => take.Vocal };
        float spread = .15f + .25f * (1 - Math.Clamp(craft, 0, 1));
        int best = -1; StudioAspect bestAspect = StudioAspect.Ensemble; float bestScore = -1;
        for (int i = 0; i < s.Sections.Count; i++) {
            var section = s.Sections[i];
            float rehearsed = 1 + .7f * ((int)section.Kind < (parts?.Count ?? 0) ? parts[(int)section.Kind] : 0);
            float late = 1 + .15f * fatigue * i / Math.Max(1, s.Sections.Count - 1);
            float score = 0, top = -1; StudioAspect topAspect = StudioAspect.Ensemble;
            foreach (StudioAspect aspect in Enum.GetValues<StudioAspect>()) {
                float weight = Weight(section.Kind, aspect);
                if (section.Final && aspect == StudioAspect.Power) weight *= 1.3f + fatigue;
                if (direction == StudioDirection.LeanIntoLyric && section.Kind == StudioSectionKind.Verse && aspect == StudioAspect.Lyric ||
                    direction == StudioDirection.TightenEnding && section.Kind == StudioSectionKind.Ending && aspect == StudioAspect.Ensemble ||
                    direction == StudioDirection.HoldBackDrums && section.Kind == StudioSectionKind.Chorus && aspect == StudioAspect.Ensemble) weight *= .7f;
                float part = weight * s.Demand(aspect) * (1 - Value(aspect));
                score += part;
                if (part > top) { top = part; topAspect = aspect; }
            }
            score *= Prominence[(int)section.Kind] * late / rehearsed * (float)(1 + (StudioTakeEvidence.Unit($"{key}|section:{i}") * 2 - 1) * spread);
            if (score > bestScore) { bestScore = score; best = i; bestAspect = topAspect; }
        }
        return (best, bestAspect, 1 - Value(bestAspect));
    }

    private static readonly Dictionary<(StudioAspect, StudioSectionKind), string[]> Trouble = new() {
        [(StudioAspect.Power, StudioSectionKind.Chorus)] = new[] { "The singer strains at {0}.", "The voice thins out in {0}." },
        [(StudioAspect.Power, StudioSectionKind.Verse)] = new[] { "The singer runs short of breath in {0}.", "The voice sits back too far in {0}." },
        [(StudioAspect.Power, StudioSectionKind.MiddleEight)] = new[] { "The high notes in {0} are a reach.", "The singer pulls back from {0}." },
        [(StudioAspect.Power, StudioSectionKind.Ending)] = new[] { "The held note at {0} wavers.", "The singer has nothing left for {0}." },
        [(StudioAspect.Lyric, StudioSectionKind.Verse)] = new[] { "The words get swallowed in {0}.", "{0} sounds read rather than meant." },
        [(StudioAspect.Lyric, StudioSectionKind.Chorus)] = new[] { "The words blur in {0}.", "{0} is sung, but not quite said." },
        [(StudioAspect.Lyric, StudioSectionKind.MiddleEight)] = new[] { "The lyric loses its thread in {0}.", "{0} sounds unsure of its words." },
        [(StudioAspect.Ensemble, StudioSectionKind.Intro)] = new[] { "The count-in wobbles; {0} takes a bar to settle.", "{0} starts ragged." },
        [(StudioAspect.Ensemble, StudioSectionKind.Verse)] = new[] { "The entrances into {0} are late.", "The band drifts apart in {0}." },
        [(StudioAspect.Ensemble, StudioSectionKind.Chorus)] = new[] { "The band crowds {0}.", "Everyone pushes at once in {0}." },
        [(StudioAspect.Ensemble, StudioSectionKind.MiddleEight)] = new[] { "{0} came apart.", "Nobody quite agrees on the changes in {0}." },
        [(StudioAspect.Ensemble, StudioSectionKind.Break)] = new[] { "{0} loses the band behind it.", "The band falls out from under {0}." },
        [(StudioAspect.Ensemble, StudioSectionKind.Ending)] = new[] { "{0} is ragged; nobody agrees where it stops.", "{0} falls apart in the last bars." },
        [(StudioAspect.Feel, StudioSectionKind.Intro)] = new[] { "{0} sets off too stiffly.", "{0} has no lift." },
        [(StudioAspect.Feel, StudioSectionKind.Verse)] = new[] { "{0} drags behind the beat.", "{0} sits flat." },
        [(StudioAspect.Feel, StudioSectionKind.Chorus)] = new[] { "{0} rushes.", "{0} never takes off." },
        [(StudioAspect.Feel, StudioSectionKind.MiddleEight)] = new[] { "{0} goes stiff.", "The groove sags through {0}." },
        [(StudioAspect.Feel, StudioSectionKind.Break)] = new[] { "{0} loses the groove.", "{0} wanders." },
        [(StudioAspect.Feel, StudioSectionKind.Ending)] = new[] { "{0} hurries.", "{0} feels rushed." }
    };

    /// <summary>The producer's located read of a pass. A sound pass gets a located compliment instead of a fault.</summary>
    public static string TroubleNote(StudioSongStructure s, int section, StudioAspect aspect, float severity, string key) {
        if (section < 0 || section >= s.Sections.Count) return null;
        var place = s.Sections[section];
        if (severity < .40f) {
            var good = s.Sections.LastOrDefault(x => x.Final) ?? place;
            return StudioTakeEvidence.Unit(key + "|praise") < .5 ? $"{Capital(good.Label)} finally felt right." : $"Nothing came apart; {good.Label} sat well.";
        }
        if (!Trouble.TryGetValue((aspect, place.Kind), out var lines)) lines = new[] { "{0} is the weak spot." };
        string line = lines[StudioTakeEvidence.Unit(key + "|phrase") < .5 ? 0 : lines.Length - 1];
        return line.StartsWith("{0}", StringComparison.Ordinal) ? string.Format(line, Capital(place.Label)) : string.Format(line, place.Label);
    }

    /// <summary>The engineer's located read of the room balance, when the capture is crowded.</summary>
    public static string BalanceNote(StudioSongStructure s, SimulatedArtist artist, float capture, string key, float uncertainty = .1f) {
        if (capture + (StudioTakeEvidence.Unit(key + "|balance") - .5) * uncertainty >= .45) return null;
        var loud = s.Sections.LastOrDefault(x => x.Kind == StudioSectionKind.Chorus) ?? s.Sections.LastOrDefault(x => x.Kind == StudioSectionKind.Break)
            ?? s.Sections.LastOrDefault(x => x.Final) ?? s.Sections[^1];
        var roles = artist?.members.Where(m => m.isActive).SelectMany(m => new[] { m.primaryRole, MemberRoleService.Secondary(m) }).ToHashSet() ?? new();
        string source = roles.Contains(MusicianRole.LeadGuitar) || roles.Contains(MusicianRole.RhythmGuitar) ? "The guitar"
            : roles.Contains(MusicianRole.Organ) ? "The organ" : roles.Contains(MusicianRole.Saxophone) || roles.Contains(MusicianRole.Trumpet) ? "The horns"
            : roles.Contains(MusicianRole.Piano) ? "The piano" : roles.Contains(MusicianRole.Drums) ? "The drums" : "The band";
        return s.Instrumental ? $"{source} swamps the lead in {loud.Label}." : $"{source} is crowding the vocal in {loud.Label}.";
    }

    private static string Capital(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
