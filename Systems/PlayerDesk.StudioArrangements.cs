using System;

public enum StudioArrangement { Live, HalfTime, SimplerRhythm }

public partial class PlayerDesk {
    public const int StudioArrangementMinutes = 20;

    public static string ArrangementName(StudioArrangement arrangement) => arrangement switch {
        StudioArrangement.HalfTime => "Half-time feel",
        StudioArrangement.SimplerRhythm => "Simpler rhythm figure",
        _ => "Live arrangement"
    };

    // A proposal is descriptive and free. It does not resolve work, draw randomness or select a take.
    public string StudioArrangementProposal(int side, StudioArrangement arrangement) {
        if (!Enum.IsDefined(arrangement)) return "Unknown arrangement.";
        if (!StudioSide(side, out var session, out var cut, out string message)) return message;
        string idea = arrangement switch {
            StudioArrangement.HalfTime => "Producer: Give the phrases more space with a half-time pulse. The band will need to settle the new entrances; the record will move less briskly.",
            StudioArrangement.SimplerRhythm => "Producer: Strip back the busy rhythm figure. It may help the band stay together, but could lose some of the live drive.",
            _ => "Producer: Return to the act's live arrangement. The earlier alternatives stay on tape."
        };
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        string response = artist.artistCreativeControl && session.Tension > .55f && arrangement != StudioArrangement.Live
            ? "The act: We want to keep our arrangement; our contract gives us that choice."
            : "The act: We can try it, then compare the playback with the earlier version.";
        int minutes = arrangement == StudioArrangement.Live ? 10 : StudioArrangementMinutes;
        string protectedRead = cut.KeptTake >= 0 ? $"Kept take {cut.Takes[cut.KeptTake].Number} stays safe." : "Every earlier pass stays safe; no take has been kept yet.";
        return $"{idea}\n{response}\n{minutes} minutes to prepare, then {StudioPassMinutes(cut)} minutes for a pass; no extra fee within the booked date. {StudioMinutesLeft(session)} minutes remain, with {FinishMinutes(session)} reserved for finishing. {protectedRead}";
    }

    public bool ArrangeStudio(int side, StudioArrangement arrangement, out string message) {
        if (!Enum.IsDefined(arrangement)) { message = "Unknown arrangement."; return false; }
        if (!StudioSide(side, out var session, out var cut, out message)) return false;
        if (arrangement == cut.Arrangement) { message = "That arrangement is already prepared."; return false; }
        var artist = ArtistManager.Instance.GetArtist(session.ArtistId);
        if (artist.artistCreativeControl && session.Tension > .55f && arrangement != StudioArrangement.Live) {
            message = "The act declines the alternative under its creative control. Earlier performances remain safe."; return false;
        }
        int minutes = arrangement == StudioArrangement.Live ? 10 : StudioArrangementMinutes;
        // Do not sell preparation that leaves no time to hear its result.
        if (StudioMinutesLeft(session) < minutes + StudioPassMinutes(cut) + FinishMinutes(session)) {
            message = "There is not enough time to prepare and hear this alternative while protecting finishing. Earlier performances remain safe."; return false;
        }
        if (!SpendStudioWork(session, minutes, $"Arrangement for {cut.Choice.Title}: {ArrangementName(arrangement)}", out message, false)) return false;
        cut.Arrangement = arrangement; cut.ArrangementRevision++;
        // Coordination learned on the previous arrangement is not a free rehearsal of the new one.
        cut.Rehearsals = 0; for (int i = 0; i < cut.PartRehearsals.Count; i++) cut.PartRehearsals[i] = 0;
        session.WorkloadMinutes += minutes * .6f;
        message = "The arrangement is prepared. Cut a pass to hear it; keeping that version is still your decision.";
        Changed?.Invoke(); return true;
    }
}
