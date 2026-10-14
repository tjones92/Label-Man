using System;
using System.Collections.Generic;
using System.Linq;

public partial class RosterManager {
    internal const int ScoutingMarketHistoryLimit = 32;
    // Called only after daily clearing has committed (or rejected) the selected deals.
    // Observer mode still records world facts. No scoring, RNG, access, money or ownership writes.
    internal static void RecordScoutingMarketDecisions(IEnumerable<DailyTalentMarketAppointment> appointments) {
        if (!LocalScenes.ScoutingMarketHistory) return;
        foreach (var item in appointments) {
            if (item?.SelectedArtist == null || item.Label == null || item.Label.isPlayerOwned ||
                !ReferenceEquals(ArtistManager.Instance?.GetArtist(item.SelectedArtist.artistId), item.SelectedArtist) ||
                !ReferenceEquals(ChartManager.Instance?.GetLabelById(item.Label.labelId), item.Label)) continue;
            var act = item.SelectedArtist;
            SceneMarketDecisionKind kind;
            if (item.Outcome is "AcceptedUncontested" or "AcceptedArtistChoice") {
                if (act.labelId != item.Label.labelId) continue;
                kind = SceneMarketDecisionKind.Signed;
            } else if (item.Outcome == "LostArtistChoice" && item.CollisionOfferCount > 1 &&
                !string.IsNullOrEmpty(item.WinnerLabelId) && item.WinnerLabelId != item.Label.labelId)
                kind = SceneMarketDecisionKind.AlternativeNotSelected;
            else if (item.Outcome == "InvalidatedBeforeCommit") kind = SceneMarketDecisionKind.SigningAttemptNotCompleted;
            else continue; // A search nomination is not a communicated offer or market decision.
            int day = LocalSceneRoomService.Day(item.Date);
            if (day < 0) continue;
            string id = $"market-decision-v1|{LocalSceneIdentityService.KeyedSeed}|{act.artistId}|{item.Label.labelId}|{day}|{item.Label.scoutingAppointmentOrdinal}";
            act.sceneMarketHistory ??= new();
            if (act.sceneMarketHistory.Any(h => h.Id == id)) continue;
            string selected = item.WinnerLabelId;
            bool completed = !string.IsNullOrEmpty(selected) && act.labelId == selected &&
                appointments.Any(a => a.SelectedArtist?.artistId == act.artistId && a.Label?.labelId == selected &&
                    a.Date == item.Date && a.Outcome is "AcceptedUncontested" or "AcceptedArtistChoice");
            act.sceneMarketHistory.Add(new() { Id = id, ArtistId = act.artistId, LabelId = item.Label.labelId,
                LabelName = item.Label.labelName, Day = day, Kind = kind, SelectedLabelId = selected,
                SelectedLabelName = ChartManager.Instance.GetLabelName(selected), SelectedDealCompleted = completed });
            act.sceneMarketHistory = act.sceneMarketHistory.OrderBy(h => h.Day).ThenBy(h => h.Id, StringComparer.Ordinal)
                .TakeLast(ScoutingMarketHistoryLimit).ToList();
        }
    }
}
