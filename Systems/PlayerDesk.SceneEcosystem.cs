using System.Linq;

public partial class PlayerDesk {
    public bool FundSceneProgram(string roomId, out string message) {
        bool success = SceneEcosystemService.FundProgram(Label, roomId, out message);
        if (success) { Note(message); Changed?.Invoke(); }
        return success;
    }
    public bool RelocateAct(string artistId, string programId, out string message) {
        if (RoadBookingFor(artistId) is { WeeksRemaining: > 0 } || SessionPlayerFor(artistId) != null) {
            message = "The act already has a road or session booking."; return false;
        }
        bool success = SceneEcosystemService.ProposeMove(Label, artistId, programId, out message);
        if (success) { Note(message); Changed?.Invoke(); }
        return success;
    }
}
