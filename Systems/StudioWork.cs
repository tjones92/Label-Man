using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Changes to a song's shape on the date: its key, its tempo, and how long the intro and ending run.</summary>
public enum StudioShapeChange { LowerKey, RaiseKey, OriginalKey, PushTempo, EaseTempo, OriginalTempo, TrimEnds, FullEnds }
/// <summary>Where the microphones favour: the room's natural blend, the vocal, or the band.</summary>
public enum StudioBalance { Natural, VocalForward, BandForward }
/// <summary>How a date is put on tape. Mono commits everything together; separating the vocal needs two tracks and
/// a way to play one back while recording the other; a spare third track holds an added part.</summary>
public enum StudioLayout { Together, VocalApart, ThreeTrack }
/// <summary>A take made from earlier tape rather than a fresh pass.</summary>
public enum StudioDerivation { None, VocalOverdub, PartOverdub, Splice }
/// <summary>A part laid onto a spare track.</summary>
public enum StudioOverdubPart { BackingVoices, Handclaps, HornLine }
/// <summary>Small things that happen in a room: most cost a little, a few are gifts.</summary>
public enum StudioIncident { None, AmpBuzz, BrokenString, LateEntrance, CompellingPickup, UnexpectedHarmony }

/// <summary>v21 state of one song on a date: its shape, the balance, ideas taken up, and the structure before reshaping.</summary>
public sealed class StudioCutWork {
    public int KeyShift, TempoShift;   // -1, 0, +1
    public bool Trimmed, KeepPickup;
    public StudioBalance Balance;
    public StudioSongStructure BaseStructure;   // the song as derived, before any reshaping on this date
    public StudioCutWork Copy() => new() {
        KeyShift = KeyShift, TempoShift = TempoShift, Trimmed = Trimmed, KeepPickup = KeepPickup, Balance = Balance, BaseStructure = BaseStructure
    };
    public bool Valid => KeyShift is >= -1 and <= 1 && TempoShift is >= -1 and <= 1 && Enum.IsDefined(Balance) && (BaseStructure == null || BaseStructure.Valid);
}

/// <summary>v21 facts of one take: how it was set up, what happened on it, what it was made from, and whether its tape
/// was later recorded over.</summary>
public sealed class StudioTakeWork {
    public int KeyShift, TempoShift, DurationSeconds;
    public bool Trimmed, KeepPickup, Erased;
    public StudioBalance Balance;
    public StudioLayout Layout;
    public StudioIncident Incident;
    public string IncidentNote, IncidentPersonId;
    public StudioDerivation Derivation;
    public string SourceTakeId, SecondSourceTakeId;
    public int SpliceSection = -1;
    public List<StudioOverdubPart> Parts;
    public StudioTakeWork Copy() {
        var copy = (StudioTakeWork)MemberwiseClone();
        copy.Parts = Parts?.ToList();
        return copy;
    }
    public bool Valid => KeyShift is >= -1 and <= 1 && TempoShift is >= -1 and <= 1 && DurationSeconds is 0 or (>= 60 and <= 900) &&
        Enum.IsDefined(Balance) && Enum.IsDefined(Layout) && Enum.IsDefined(Incident) && Enum.IsDefined(Derivation) &&
        (Derivation == StudioDerivation.None) == string.IsNullOrEmpty(SourceTakeId) &&
        (Derivation == StudioDerivation.Splice) == !string.IsNullOrEmpty(SecondSourceTakeId) &&
        (Parts ?? new()).All(Enum.IsDefined) && (Parts ?? new()).Distinct().Count() == (Parts?.Count ?? 0);
}

/// <summary>v21 state of a whole date: how it goes on tape, the stock, open faults in the room, the producer and arranger
/// on it, the days of a lockout and whether it runs at night.</summary>
public sealed class StudioDateWork {
    public StudioLayout Layout;
    public int Tracks = 1;
    public int StockPasses, StockBought, PassesUsed;
    public List<string> Faults = new();
    public int LockoutDays = 1;
    public List<int> DayStarts = new(), DayElapsed = new();
    public bool Night;
    // A late-night date: an evening booking that runs past midnight, to the small hours.
    public bool LateNight;
    public StudioDateWork Copy() => new() {
        Layout = Layout, Tracks = Tracks, StockPasses = StockPasses, StockBought = StockBought, PassesUsed = PassesUsed,
        Faults = Faults.ToList(), LockoutDays = LockoutDays, DayStarts = DayStarts.ToList(), DayElapsed = DayElapsed.ToList(), Night = Night, LateNight = LateNight
    };
    public bool Valid => Enum.IsDefined(Layout) && Tracks is >= 1 and <= 8 && StockPasses >= 0 && StockBought >= 0 && PassesUsed >= 0 &&
        PassesUsed <= StockPasses + StockBought && LockoutDays is >= 1 and <= 3 && DayStarts.Count >= 1 && DayStarts.Count <= LockoutDays &&
        DayElapsed.Count == DayStarts.Count && DayElapsed.All(e => e >= 0) && DayStarts.All(s => s is >= 0 and < 24 * 60) &&
        (Faults ?? new()).All(f => !string.IsNullOrEmpty(f)) && PlayerDesk.StudioLayoutAllowed(Layout, Tracks) &&
        (!LateNight || Night && LockoutDays == 1);
}
