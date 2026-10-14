using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

// Exercises the actual desk controls with two heard acts and a rival signing.
public partial class ScoutingUiChecks : Node {
    private PlayerDeskPanel panel;
    private int checks;
    private IEnumerable<Node> Nodes(Node root) {
        if (root.IsQueuedForDeletion()) yield break;
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
    }
    private void Check(bool ok, string message) {
        if (!ok) throw new InvalidOperationException(message);
        checks++;
    }
    private async Task Frame() {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree().CreateTimer(0.05), Timer.SignalName.Timeout);
    }
    private async Task Press(string text, bool prefix = false) {
        var button = Nodes(panel).OfType<Button>().Single(b => prefix ? b.Text.StartsWith(text) : b.Text == text);
        Check(!button.Disabled, "Enabled action: " + text);
        button.EmitSignal(Button.SignalName.Pressed);
        await Frame();
    }
    private async Task Screenshot(string name) {
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/scouting-ui-" + name + ".png"));
    }
    public override async void _Ready() {
        try {
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Scouting UI Checks", "nashville", out string message), message);
            var bill = LocalSceneRoomService.Calendar("nashville", clock.CurrentDate).First(b =>
                b.Status == SceneBillStatus.Scheduled && b.Appearances.Count >= 2 && b.Appearances.Take(2).All(a => a.EndHour <= clock.HardStopHour));
            foreach (var slot in bill.Appearances.Take(2)) {
                clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
                Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message), message);
            }
            var first = desk.ScoutingDossiers[0]; var second = desk.ScoutingDossiers[1];
            var artist = ArtistManager.Instance.GetArtist(first.ArtistId);
            artist.labelId = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId;
            Check(!desk.ScoutingSummary(first.ArtistId, false).Any(l => l.StartsWith("Other leads")), "Focused evidence excludes other acts");
            AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
            panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
            panel.OpenAtTab("A&R"); await Frame();
            Check(!Nodes(panel).OfType<OptionButton>().Any(), "Quick finder hides room calendars and source forms");
            await Screenshot("finder");
            Check(Nodes(panel).OfType<Label>().All(l => !l.Text.Contains("min wait")), "Wait duration stays out of visible previews");
            Check(Nodes(panel).OfType<Button>().Where(b => b.Text == "WAIT" || b.Text == "LISTEN").All(b => b.TooltipText.Contains("Cash after admission") && b.SizeFlagsHorizontal == Control.SizeFlags.ShrinkBegin), "Compact actions disclose cash quotes on hover");
            await Press("VENUES & DATES");
            Check(Nodes(panel).OfType<VenueHandbill>().Any(), "Room calendars remain accessible");
            var venue = Nodes(panel).OfType<VenueHandbill>().Single(v => v.GetMeta("scouting_room_id").AsString() == bill.RoomId);
            Check(!desk.KnowsSceneBooker(bill.RoomId), "Listening alone does not introduce a booker");
            venue.EmitSignal(Button.SignalName.Pressed); await Frame();
            var roomModal = Nodes(panel).OfType<PaperModal>().Single();
            var room = SceneRoomCatalog.Get(bill.RoomId);
            Check(Nodes(roomModal).OfType<Label>().All(l => !l.Text.Contains(room.ContactName)), "Unmet booker identity is hidden");
            Check(Nodes(roomModal).OfType<Label>().Any(l => l.Text.Contains(first.ArtistName) && l.Text.Contains("Heard / past")), "Earlier heard sets are not marked cancelled");
            string calendarState = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            int firstVisibleDay = Nodes(roomModal).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32());
            var nextWeek = Nodes(roomModal).OfType<Button>().Single(b => b.TooltipText == "Next week of posted dates");
            if (!nextWeek.Disabled) {
                nextWeek.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(Nodes(roomModal).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32()) > firstVisibleDay, "Calendar advances to later posted weeks");
                var previousWeek = Nodes(roomModal).OfType<Button>().Single(b => b.TooltipText == "Previous week");
                previousWeek.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(Nodes(roomModal).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32()) == firstVisibleDay, "Calendar returns to the current week");
            }
            var dayButton = Nodes(roomModal).OfType<Button>().First(b => b.HasMeta("scouting_day") && b.GetMeta("scouting_day").AsInt32() == bill.Day + 1);
            dayButton.EmitSignal(Button.SignalName.Pressed); await Frame();
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == calendarState, "Calendar selection does not advance time or evidence");
            await Screenshot("venue");
            int introMinute = clock.CurrentHour * 60 + clock.CurrentMinute; float introCash = desk.Label.cashReserves;
            await Press("INTRODUCE YOURSELF");
            Check(desk.KnowsSceneBooker(bill.RoomId) && clock.CurrentHour * 60 + clock.CurrentMinute == introMinute + 15 && desk.Label.cashReserves == introCash, "Introduction spends 15 minutes with no fee");
            Check(desk.CaptureState().ScoutingKnowledge.Visit?.EndMinute == introMinute + 15 && desk.AdmissionForVisit(bill, room, introMinute + 15) == 0, "Short introduction preserves the paid room visit");
            Check(RolodexDirectory.Cards(desk, RolodexRole.Booker).Single().Name == room.ContactName, "Introduced booker appears in Rolodex");
            var introducedSave = JsonSerializer.Deserialize<PlayerSaveData>(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
            Check(desk.RestoreState(introducedSave, out _) && desk.KnowsSceneBooker(bill.RoomId), "Introduction survives serialized save restore");
            Check(!desk.IntroduceSceneBooker(bill.RoomId, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == introMinute + 15, "Repeated introduction rejected without spending time");
            roomModal.Close(); await Frame();
            await Press("ACT NOTEBOOK");
            Check(!Nodes(panel).OfType<OptionButton>().Any(), "Collapsed notebook has no orphan questions");
            await Screenshot("notebook");
            await Press("▸  " + first.ArtistName, true);
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text == "Stage presence") && !Nodes(panel).OfType<Label>().Any(l => l.Text.StartsWith("Competition:") || l.Text.StartsWith("What they want:")), "Evidence shows concise axes and hides business boilerplate");
            Check(!Nodes(panel).OfType<OptionButton>().Any(), "Follow-up planning is hidden until requested");
            string noteState = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            await Press("FULL NOTES & HISTORY");
            var fullNotes = Nodes(panel).OfType<PaperModal>().Single();
            Check(Nodes(fullNotes).OfType<RichTextLabel>().Any(l => l.Text.StartsWith("[b]Vocals:[/b]")) &&
                Nodes(fullNotes).OfType<RichTextLabel>().Any(l => l.Text.StartsWith("[b]Ensemble:[/b]")), "Full notes emphasize skill labels");
            Check(!desk.ScoutingPlayerNotes(first.ArtistId).Any(l => l.Contains("held together, with questions") || l.Contains("cannot establish consistency") || l.Contains("whole-bill")), "Player notes avoid technical verdicts");
            await Screenshot("full-notes"); fullNotes.Close(); await Frame();
            await Press("ROOM IMPRESSION");
            var audienceModal = Nodes(panel).OfType<PaperModal>().Single();
            Check(Nodes(audienceModal).OfType<Label>().Any(l => l.Text.Contains("when you watched")) && !Nodes(audienceModal).OfType<ScrollContainer>().Any(), "Crowd recollection is a short room impression");
            await Screenshot("audience"); audienceModal.Close(); await Frame();
            await Press("TALK TO ACT");
            var conversation = Nodes(panel).OfType<PaperModal>().Single();
            var invitations = Nodes(conversation).OfType<Button>().Where(b => b.Text.StartsWith("ASK FOR ")).ToArray();
            Check(invitations.Length == 3 && invitations.All(b => b.SizeFlagsHorizontal == Control.SizeFlags.ShrinkBegin && b.TooltipText.Length > 0), "Meeting requests live in the conversation with compact buttons and help tips");
            await Screenshot("conversation"); conversation.Close(); await Frame();
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == noteState, "Reading notes and opening conversation spends nothing and creates no evidence");
            await Press("▸  PLAN ANOTHER LISTEN");
            var question = Nodes(panel).OfType<OptionButton>().Single();
            question.Select((int)ScoutingQuestion.Vocals);
            question.EmitSignal(OptionButton.SignalName.ItemSelected, (long)ScoutingQuestion.Vocals); await Frame();
            Check(desk.ScoutingDossiers.Single(d => d.ArtistId == first.ArtistId).Question == ScoutingQuestion.Vocals &&
                desk.ScoutingDossiers.Single(d => d.ArtistId == second.ArtistId).Question == ScoutingQuestion.None, "Listening question belongs only to selected act");
            Check(!Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("REVIEW " + second.ArtistName.ToUpperInvariant())), "Other act review is hidden from main evidence");
            await Press("▸  OTHER ACTS TO CONSIDER");
            string snapshot = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            await Press("REVIEW " + second.ArtistName.ToUpperInvariant());
            Check(Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + second.ArtistName)), "Review opens the named act's card");
            var scroll = Nodes(panel).OfType<ScrollContainer>().First();
            Check(scroll.ScrollVertical > 0, "Selected act scrolls into view");
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == snapshot, "Review spends nothing and does not change evidence or pad");
            await Press("▸  PLAN ANOTHER LISTEN");
            question = Nodes(panel).OfType<OptionButton>().Single();
            Check(question.Selected == (int)ScoutingQuestion.None, "Switching acts restores that act's own question");
            await Press("▸  ASK ABOUT " + second.ArtistName.ToUpperInvariant());
            var ask = Nodes(panel).OfType<Button>().Single(b => b.Text.StartsWith("ASK ABOUT " + second.ArtistName.ToUpperInvariant() + " (1h"));
            Check(ask.GetParent().GetParent().GetParent() is VBoxContainer, "Booker controls are nested in act card");
            await Screenshot("expanded");
            await Press("ARCHIVE");
            Check(!Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + second.ArtistName)), "Archived act leaves active notebook");
            await Press("SHOW ARCHIVED NOTES");
            Check(Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + second.ArtistName)), "Archived act remains reviewable");
            await Press("FIND A SET");
            await Press("NEXT SETS");
            await Press("LISTEN");
            Check(Nodes(panel).OfType<Button>().Single(b => b.Text == "ACT NOTEBOOK").Disabled, "Listening opens the notebook automatically");
            Check(Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + desk.Slate.Last().Artist.stageName)), "Newly heard act is expanded");
            await Screenshot("new-listen");
            await Press("TALK TO ACT");
            conversation = Nodes(panel).OfType<PaperModal>().Single();
            var newlyHeard = desk.Slate.Last().Artist.artistId;
            var rehearsalQuote = desk.PreviewScoutingInvitation(newlyHeard, ScoutingAppointmentActivity.Rehearsal);
            var rehearsalButton = Nodes(conversation).OfType<Button>().Single(b => b.Text == "ASK FOR REHEARSAL");
            Check(rehearsalButton.Disabled == (rehearsalQuote == null), "Conversation invitation respects the real post-set quote");
            await Screenshot("conversation-current");
            if (rehearsalQuote != null) {
                int invitationMinute = clock.CurrentHour * 60 + clock.CurrentMinute; float invitationCash = desk.Label.cashReserves;
                int heardBefore = desk.ScoutingDossiers.Single(d => d.ArtistId == newlyHeard).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance);
                rehearsalButton.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(!Nodes(panel).OfType<PaperModal>().Any() && clock.CurrentHour * 60 + clock.CurrentMinute == invitationMinute + 15 && desk.Label.cashReserves == invitationCash, "Conversation request closes, spends 15 minutes and charges no attendance fee");
                Check(desk.ScoutingDossiers.Single(d => d.ArtistId == newlyHeard).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == heardBefore, "Scheduling does not create another listening");
            } else { conversation.Close(); await Frame(); }
            panel.OpenAtTab("ROLODEX"); await Frame();
            await Press("BOOKERS", true);
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text == room.ContactName), "Booker card renders in the real Rolodex");
            await Screenshot("rolodex");
            await Press("VENUE & CALENDAR");
            Check(Nodes(panel).OfType<PaperModal>().Count() == 1 && !Nodes(panel).OfType<Button>().Any(b => b.Text == "INTRODUCE YOURSELF"), "Rolodex opens the known booker's venue calendar");
            Nodes(panel).OfType<PaperModal>().Single().Close(); await Frame();
            var partialSet = desk.ScoutingItinerary().First(o => o.EndMinute - o.StartMinute >= 60);
            int partialStart = partialSet.StartMinute + (partialSet.EndMinute - partialSet.StartMinute) * 3 / 4;
            clock.RestoreClock(LocalSceneRoomService.Date(partialSet.Day), partialStart / 60, partialStart % 60);
            Check(desk.ListenSceneAppearance(partialSet.BillId, partialSet.EventId, out message), "Actual short listen: " + message);
            var partialDossier = desk.ScoutingDossiers.Single(d => d.ArtistId == partialSet.ArtistId);
            var partialNote = partialDossier.Observations.Single(o => o.EventId == partialSet.EventId && o.Kind == ScoutingEvidenceKind.Performance);
            Check(partialNote.Coverage < .5f && partialNote.SetPresent.HasValue && partialNote.SetArrivals == null && partialNote.SetStayedShare == null && partialNote.SetRequests == null,
                "Short listen remembers visible occupancy without borrowing arrivals, retention or song requests");
            Check(ScoutingAudience.RoomImpression(partialDossier).Any(l => l.Contains("when you watched")), "Player can recall how the room looked during a short listen");
            GD.Print($"SCOUTING_UI_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("SCOUTING_UI_FAILED: " + ex); GetTree().Quit(1); }
    }
}
