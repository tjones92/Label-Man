using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private bool showArchivedScouting;
    private void ScoutingNotebook(PlayerDesk desk) {
        var dossiers = desk.ScoutingDossiers;
        if (dossiers.Count == 0) { Body("Every heard scene act is recorded automatically. Pinning sets priorities; it is optional."); return; }
        if (desk.FoundingAct is { } founding) Body($"FIRST NAME ON THE LABEL: {founding.ArtistName}, {LocalSceneRoomService.Date(founding.Day).ToHeadlineString()}. {founding.Context} Advance {Money(founding.Terms.Advance)}, {founding.Terms.RoyaltyRate:P1}, {founding.Terms.TermYears} year(s).");
        var archiveView = Btn(showArchivedScouting ? "HIDE ARCHIVED NOTES" : "SHOW ARCHIVED NOTES");
        archiveView.Pressed += () => { showArchivedScouting = !showArchivedScouting; Refresh(); }; content.AddChild(archiveView);
        foreach (var dossier in dossiers.Where(d => showArchivedScouting || d.Priority != ScoutingPriority.Archived)
            .OrderByDescending(d => d.Priority == ScoutingPriority.Pursue).ThenByDescending(d => d.Observations.LastOrDefault()?.AcquiredDay)) {
            var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
            Heading($"{dossier.ArtistName} · {dossier.Priority}");
            if (artist == null) Body("Historical entry; this act is no longer available.");
            else if (!string.IsNullOrEmpty(artist.labelId)) Body("Under contract to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "a label") + ". Hearing an act does not make it available to sign.");
            foreach (string line in desk.ScoutingSummary(dossier.ArtistId)) Body(line);
            var row = new HFlowContainer();
            var open = Btn("OPEN ON THE PAD"); open.Disabled = artist == null;
            open.Pressed += () => Act(() => { bool ok = desk.OpenScoutingDossier(dossier.ArtistId, out string result); Say(result, ok); return ok; }); row.AddChild(open);
            foreach (var priority in new[] { ScoutingPriority.Watch, ScoutingPriority.Pursue, ScoutingPriority.Archived }) {
                var button = Btn(priority == ScoutingPriority.Archived ? "ARCHIVE" : priority.ToString().ToUpperInvariant());
                button.Disabled = dossier.Priority == priority;
                button.Pressed += () => { desk.SetScoutingPriority(dossier.ArtistId, priority); Refresh(); }; row.AddChild(button);
            }
            var details = Btn("SOURCES & COVERAGE"); details.Pressed += () => {
                var modal = PaperModal.OpenClipboard(this, dossier.ArtistName + " — EVIDENCE", 850);
                var scroll = ComparisonScroll();
                var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                notes.AddThemeConstantOverride("separation", 12);
                scroll.AddChild(notes); modal.Body.AddChild(scroll);
                foreach (string line in desk.ScoutingProvenance(dossier.ArtistId)) notes.AddChild(FaintLine(line));
                modal.AddButton("DONE", null, PaperModal.ButtonKind.Primary);
            }; row.AddChild(details); content.AddChild(row);
        }
    }
}
