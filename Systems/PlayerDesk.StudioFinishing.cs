using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum StudioFinish { House, SoftenTop, GentleCompression, ChamberEcho, VocalForward }

/// <summary>A saved finish of one retained source. The source performance is never rewritten.</summary>
public sealed class StudioFinishWork {
    public string TakeId, Note;
    public StudioFinish Finish;
    public int ActionNumber, Minutes;
    public float HookChange, ProductionChange;
    public StudioFinishWork Copy() => (StudioFinishWork)MemberwiseClone();
}

public partial class PlayerDesk {
    public const int StudioProcessingMinutes = 8;
    public static string FinishName(StudioFinish finish) => finish switch {
        StudioFinish.SoftenTop => "Soften the top end",
        StudioFinish.GentleCompression => "Gentle whole-tape compression",
        StudioFinish.ChamberEcho => "Send it through the echo chamber",
        StudioFinish.VocalForward => "Bring the vocal track up",
        _ => "Included house finish"
    };
    public static IEnumerable<StudioFinish> StudioFinishes(StudioRoomSnapshot room) {
        yield return StudioFinish.House;
        if (room?.SupportsToneFilter == true) yield return StudioFinish.SoftenTop;
        if (room?.SupportsCompression == true) yield return StudioFinish.GentleCompression;
        if (room?.SupportsChamber == true) yield return StudioFinish.ChamberEcho;
    }
    /// <summary>The finishes a kept source allows: the room's, plus a vocal-forward balance where the vocal is on its own track.
    /// Never "turn down the drums" on a tape where the drums and everything else share a track.</summary>
    public static IEnumerable<StudioFinish> StudioFinishes(StudioRoomSnapshot room, SessionTake take) {
        foreach (var f in StudioFinishes(room)) yield return f;
        if (take?.Work != null && take.Work.Layout != StudioLayout.Together && !take.Work.Erased) yield return StudioFinish.VocalForward;
    }
    public static StudioFinishWork SelectedStudioFinish(SessionCut cut) => cut.KeptTake < 0 || cut.KeptTake >= cut.Takes.Count || cut.Finish == StudioFinish.House
        ? null : cut.FinishWork.LastOrDefault(w => w.TakeId == cut.Takes[cut.KeptTake].TakeId && w.Finish == cut.Finish);

    public string StudioFinishProposal(int side, StudioFinish finish) {
        if (!Enum.IsDefined(finish)) return "Unknown finish.";
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        if (!StudioFinishes(session.Room, cut.KeptTake >= 0 ? cut.Takes[cut.KeptTake] : null).Contains(finish)) return finish == StudioFinish.VocalForward ? "Only a take with the vocal on its own track can be rebalanced." : "The booked equipment does not support that processing.";
        string treatment = finish switch {
            StudioFinish.SoftenTop => "Engineer: Roll off the sharp top of the whole mono tape. It can soften the room hiss and edges, but also takes some bite from the performance.",
            StudioFinish.GentleCompression => "Engineer: Ease the peaks of the whole mono tape. The level can settle, but the loud and quiet phrases will sit closer together.",
            StudioFinish.ChamberEcho => "Engineer: Play the whole tape into the room's echo chamber and print the blend. It puts a halo round the record, and the words sit a little further back.",
            StudioFinish.VocalForward => "Engineer: The vocal is on its own track, so I can bring it up over the band in the mix down. The words land; the band gets a little smaller behind them.",
            _ => "Engineer: Use the included house finish from the retained source, with its original balance and dynamics."
        };
        if (cut.KeptTake < 0) return treatment + " Keep a performance before processing it.";
        var take = cut.Takes[cut.KeptTake];
        bool done = cut.FinishWork.Any(w => w.TakeId == take.TakeId && w.Finish == finish);
        return $"{treatment}\nThis treats every instrument together; it cannot change the vocal or drums independently. " +
            $"Take {take.Number} stays safe. " + (finish == StudioFinish.House ? "The house finish is included." : done
                ? "This processed version is already retained; comparing or selecting it costs no more time."
                : $"{StudioProcessingMinutes} extra minutes within the paid date, with no extra fee.") +
            $" {StudioMinutesLeft(session)} minutes remain; {FinishMinutes(session)} minutes stay reserved for final playback and printing.";
    }

    public bool FinishStudioSide(int side, StudioFinish finish, out string message) {
        if (!Enum.IsDefined(finish)) { message = "Unknown finish."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (cut.KeptTake < 0 || cut.KeptTake >= cut.Takes.Count) { message = "Keep a source performance before choosing its finish."; return false; }
        if (!StudioFinishes(session.Room, cut.Takes[cut.KeptTake]).Contains(finish)) { message = finish == StudioFinish.VocalForward ? "Only a take with the vocal on its own track can be rebalanced." : "The booked equipment does not support that processing."; return false; }
        if (cut.Finish == finish) { message = "That finish is already selected."; return false; }
        var take = cut.Takes[cut.KeptTake];
        if (finish != StudioFinish.House && !cut.FinishWork.Any(w => w.TakeId == take.TakeId && w.Finish == finish)) {
            if (!SpendStudioWork(session, StudioProcessingMinutes, $"Processed {cut.Choice.Title}, take {take.Number}: {FinishName(finish)}", out message, false)) return false;
            cut.FinishWork.Add(new StudioFinishWork {
                TakeId = take.TakeId, Finish = finish, ActionNumber = session.ActionNumber, Minutes = StudioProcessingMinutes,
                HookChange = finish switch { StudioFinish.SoftenTop => -.012f, StudioFinish.ChamberEcho => .015f, StudioFinish.VocalForward => .012f, _ => -.020f },
                ProductionChange = finish switch { StudioFinish.SoftenTop => .015f, StudioFinish.ChamberEcho => -.005f, StudioFinish.VocalForward => .008f, _ => .025f },
                Note = finish switch {
                    StudioFinish.SoftenTop => "Engineer: The tape's edges sound softer to me; some of the attack went with them.",
                    StudioFinish.ChamberEcho => "Engineer: The chamber gives it a glow; the words are a touch harder to pick out.",
                    StudioFinish.VocalForward => "Engineer: The vocal sits on top now. The band sounds a little further away.",
                    _ => "Engineer: The peaks sit closer together now; the performance breathes less freely."
                }
            });
        }
        cut.Finish = finish;
        message = finish == StudioFinish.House ? "The included house finish is selected; processed alternatives remain retained."
            : "The processed version is selected for this source. The original performance remains safe.";
        Changed?.Invoke(); return true;
    }

    private static void ApplyStudioFinish(Record record, StudioFinishWork finish) {
        if (finish == null) return;
        record.hookStrength = Mathf.Clamp(record.hookStrength + finish.HookChange, 0, 1);
        record.productionQuality = Mathf.Clamp(record.productionQuality + finish.ProductionChange, 0, 1);
    }
}
