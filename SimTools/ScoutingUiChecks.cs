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
        var matches = Nodes(panel).OfType<Button>().Where(b => prefix ? b.Text.StartsWith(text) : b.Text == text).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException($"{matches.Length} buttons match \"{text}\". Visible: " +
            string.Join(" | ", Nodes(panel).OfType<Button>().Where(b => b.IsVisibleInTree()).Select(b => b.Text)));
        var button = matches[0];
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
            PlayerDesk.AutosaveOffForChecks = true;
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            Check(desk.FoundLabel("Scouting UI Checks", "nashville", out string message), message);
            desk.DismissLienMorning();   // the first-morning scene would cover every screenshot
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
            Check(Nodes(panel).OfType<Button>().Where(b => b.Text == "GO & WAIT" || b.Text == "LISTEN").All(b => b.TooltipText.Contains("Cash after admission") && b.SizeFlagsHorizontal == Control.SizeFlags.ShrinkBegin), "Compact actions disclose cash quotes on hover");
            await Press("VENUES & CALENDARS");
            // The venues are a row of room buttons with the picked room's calendar inline (no handbills, no modal).
            Check(Nodes(panel).OfType<Button>().Any(b => b.HasMeta("scouting_room_id")), "Room calendars remain accessible");
            var venue = Nodes(panel).OfType<Button>().Single(b => b.HasMeta("scouting_room_id") && b.GetMeta("scouting_room_id").AsString() == bill.RoomId);
            Check(!desk.KnowsSceneBooker(bill.RoomId), "Listening alone does not introduce a booker");
            venue.EmitSignal(Button.SignalName.Pressed); await Frame();
            var room = SceneRoomCatalog.Get(bill.RoomId);
            Check(Nodes(panel).OfType<Label>().All(l => !l.Text.Contains(room.ContactName)), "Unmet booker identity is hidden");
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text.Contains(first.ArtistName) && l.Text.Contains("Heard / past")), "Earlier heard sets are not marked cancelled");
            string calendarState = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            int firstVisibleDay = Nodes(panel).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32());
            var nextWeek = Nodes(panel).OfType<Button>().Single(b => b.TooltipText == "Next week of posted dates");
            if (!nextWeek.Disabled) {
                nextWeek.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(Nodes(panel).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32()) > firstVisibleDay, "Calendar advances to later posted weeks");
                var previousWeek = Nodes(panel).OfType<Button>().Single(b => b.TooltipText == "Previous week");
                previousWeek.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(Nodes(panel).OfType<Button>().Where(b => b.HasMeta("scouting_day")).Min(b => b.GetMeta("scouting_day").AsInt32()) == firstVisibleDay, "Calendar returns to the current week");
            }
            var dayButton = Nodes(panel).OfType<Button>().First(b => b.HasMeta("scouting_day") && b.GetMeta("scouting_day").AsInt32() == bill.Day + 1);
            dayButton.EmitSignal(Button.SignalName.Pressed); await Frame();
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == calendarState, "Calendar selection does not advance time or evidence");
            await Screenshot("venue");
            int introMinute = clock.CurrentHour * 60 + clock.CurrentMinute; float introCash = desk.Label.cashReserves;
            await Press("INTRODUCE YOURSELF", true);
            Check(desk.KnowsSceneBooker(bill.RoomId) && clock.CurrentHour * 60 + clock.CurrentMinute == introMinute + 15 && desk.Label.cashReserves == introCash, "Introduction spends 15 minutes with no fee");
            Check(desk.CaptureState().ScoutingKnowledge.Visit?.EndMinute == introMinute + 15 && desk.AdmissionForVisit(bill, room, introMinute + 15) == 0, "Short introduction preserves the paid room visit");
            Check(RolodexDirectory.Cards(desk, RolodexRole.Booker).Single().Name == room.ContactName, "Introduced booker appears in Rolodex");
            var introducedSave = JsonSerializer.Deserialize<PlayerSaveData>(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
            Check(desk.RestoreState(introducedSave, out _) && desk.KnowsSceneBooker(bill.RoomId), "Introduction survives serialized save restore");
            Check(!desk.IntroduceSceneBooker(bill.RoomId, out _) && clock.CurrentHour * 60 + clock.CurrentMinute == introMinute + 15, "Repeated introduction rejected without spending time");
            await Press("ACT NOTEBOOK");
            Check(!Nodes(panel).OfType<OptionButton>().Any(), "Collapsed notebook has no orphan questions");
            await Screenshot("notebook");
            await Press("▸  " + first.ArtistName, true);
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text == "Stage presence") && !Nodes(panel).OfType<Label>().Any(l => l.Text.StartsWith("Competition:") || l.Text.StartsWith("What they want:")), "Evidence shows concise axes and hides business boilerplate");
            Check(!Nodes(panel).OfType<OptionButton>().Any(), "Follow-up planning is hidden until requested");
            Check(!Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("REVIEW " + second.ArtistName.ToUpperInvariant())), "Other act review is hidden from main evidence");
            // Signing audit §3.1: one quiet row under the verb, a filing stamp and NOTES; the three read-only views are one sheet.
            foreach (string gone in new[] { "WATCH", "PURSUE", "ARCHIVE", "ROOM IMPRESSION", "SOURCES & COVERAGE", "FULL NOTES & HISTORY", "PLAN ANOTHER LISTEN" })
                Check(!Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == gone), "Decluttered card has no " + gone);
            Check(Nodes(panel).OfType<MenuButton>().Count(b => b.HasMeta("scouting_filing")) == 1, "An act signed elsewhere can still be filed away");
            string noteState = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            await Press("NOTES");
            var fullNotes = Nodes(panel).OfType<PaperModal>().Single();
            Check(Nodes(fullNotes).OfType<RichTextLabel>().Any(l => l.Text.StartsWith("[b]Vocals:[/b]")) &&
                Nodes(fullNotes).OfType<RichTextLabel>().Any(l => l.Text.StartsWith("[b]Ensemble:[/b]")), "Full notes emphasize skill labels");
            Check(Nodes(fullNotes).OfType<Control>().Any(c => c is Label l && l.Text.Contains("when you watched") || c is RichTextLabel r && r.Text.Contains("when you watched")), "The notes carry the room impression");
            Check(Nodes(fullNotes).OfType<Label>().Any(l => l.Text == "WHERE THIS CAME FROM"), "The notes carry the sources");
            Check(!desk.ScoutingPlayerNotes(first.ArtistId).Any(l => l.Contains("held together, with questions") || l.Contains("cannot establish consistency") || l.Contains("whole-bill")), "Player notes avoid technical verdicts");
            await Screenshot("full-notes"); fullNotes.Close(); await Frame();
            await Press("TALK TO THEM");
            // The Talk (signing audit §3.3) takes the page: what you can ask is lines you say. This act is under contract
            // elsewhere, so there's no ask and no rehearsal or demo, but their history and their number are still lines.
            string[] invitationLines = { "▸  Can I hear you rehearse, somewhere quieter?", "▸  Put a song on tape for me, at my place.", "▸  Can we sit down properly this week, just us?" };
            var talkLines = Nodes(panel).OfType<Button>().Where(b => b.IsVisibleInTree() && b.Text.StartsWith("▸  ")).Select(b => b.Text).ToArray();
            bool signedElsewhere = !string.IsNullOrEmpty(ArtistManager.Instance.GetArtist(first.ArtistId)?.labelId);
            Check(!Nodes(panel).OfType<PaperModal>().Any() && Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "END THE CONVERSATION") &&
                (signedElsewhere ? !talkLines.Intersect(invitationLines).Any() && !talkLines.Contains("▸  Would you hear an offer from us?") && talkLines.Length > 0
                    : invitationLines.All(talkLines.Contains)),
                $"The Talk is on the page with the lines that fit the act ({string.Join(" / ", talkLines)})");
            await Screenshot("conversation"); await Press("END THE CONVERSATION");
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == noteState, "Reading notes and opening conversation spends nothing and creates no evidence");
            await Press("MORE", true);
            OptionButton QuestionFor(string artistId) => Nodes(panel).OfType<OptionButton>().Single(o => o.HasMeta("scouting_question") && o.GetMeta("scouting_question").AsString() == artistId);
            var question = QuestionFor(first.ArtistId);
            question.Select((int)ScoutingQuestion.Vocals);
            question.EmitSignal(OptionButton.SignalName.ItemSelected, (long)ScoutingQuestion.Vocals); await Frame();
            Check(desk.ScoutingDossiers.Single(d => d.ArtistId == first.ArtistId).Question == ScoutingQuestion.Vocals &&
                desk.ScoutingDossiers.Single(d => d.ArtistId == second.ArtistId).Question == ScoutingQuestion.None, "Listening question belongs only to selected act");
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text == "OTHER ACTS TO CONSIDER"), "Other acts sit in MORE without a nested fold");
            await Frame();   // the question change rebuilds the page
            // Whether the second act counts as a local lead depends on the world drawn (windowed and headless runs differ).
            if (desk.ScoutingAlternativeLeads(first.ArtistId).Any(l => l.ArtistId == second.ArtistId)) {
            string snapshot = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
            await Press("REVIEW " + second.ArtistName.ToUpperInvariant());
            var reviewed = Nodes(panel).OfType<Button>().SingleOrDefault(b => b.IsVisibleInTree() && b.Text.StartsWith("▾  " + second.ArtistName));
            Check(reviewed != null, "Review opens the named act's card");
            var scroll = Nodes(panel).OfType<ScrollContainer>().First(s => s.IsVisibleInTree());
            Check(scroll.GetGlobalRect().Grow(1).Encloses(reviewed.GetGlobalRect()), "Selected act scrolls into view");
            Check(JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions) == snapshot, "Review spends nothing and does not change evidence or pad");
            } else {
                GD.Print("note: the second act is not a local lead in this world; the REVIEW leg is skipped");
                await Press("▸  " + second.ArtistName, true);
            }
            await Press("MORE", true);
            Check(QuestionFor(second.ArtistId).Selected == (int)ScoutingQuestion.None, "Switching acts restores that act's own question");
            var ask = Nodes(panel).OfType<Button>().Single(b => b.Text.StartsWith("ASK ABOUT " + second.ArtistName.ToUpperInvariant() + " ("));
            Check(ask.IsVisibleInTree(), "Booker questions sit in the act's MORE");
            await Screenshot("expanded");
            var stamp = Nodes(panel).OfType<MenuButton>().Single(b => b.HasMeta("scouting_filing") && b.GetMeta("scouting_filing").AsString() == second.ArtistId);
            Check(stamp.Text.StartsWith("FILED: "), "Filing is one stamp");
            stamp.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, (long)ScoutingPriority.Archived); await Frame(); await Frame();
            Check(!Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + second.ArtistName)), "Archived act leaves active notebook");
            await Press("SHOW ARCHIVED NOTES", true);
            Check(Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + second.ArtistName)), "Archived act remains reviewable");
            await Press("FIND A SET");
            await Press("NEXT SETS");
            // Nothing may be playing this minute: go and wait for the soonest set, then listen.
            if (!Nodes(panel).OfType<Button>().Any(b => b.Text == "LISTEN")) {
                Nodes(panel).OfType<Button>().First(b => b.Text == "GO & WAIT").EmitSignal(Button.SignalName.Pressed); await Frame(); await Frame();
            }
            Nodes(panel).OfType<Button>().First(b => b.Text == "LISTEN").EmitSignal(Button.SignalName.Pressed); await Frame();
            // Listening stages The Set (signing audit §3.2) over the page; letting the act go files them in the notebook.
            await Press("LISTEN CLOSELY");
            Check(Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "LET THEM GO") && !Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "FIND A SET"),
                "Listening stages the set");
            while (Nodes(panel).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.Text == "LET THEM GO") is { } letGo) { letGo.EmitSignal(Button.SignalName.Pressed); await Frame();
                if (Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text == "LISTEN CLOSELY")) await Press("LISTEN CLOSELY"); }
            await Press("ACT NOTEBOOK");
            if (Nodes(panel).OfType<Button>().FirstOrDefault(b => b.Text.StartsWith("▸  " + desk.Slate.Last().Artist.stageName)) is { } closed) { closed.EmitSignal(Button.SignalName.Pressed); await Frame(); }
            Check(Nodes(panel).OfType<Button>().Any(b => b.Text.StartsWith("▾  " + desk.Slate.Last().Artist.stageName)), "Newly heard act is expanded");
            Check(Nodes(panel).OfType<Button>().Count(b => b.Text is "TALK TO THEM" or "SIT DOWN ON TERMS") == 1, "The open card has one primary verb");
            // Waiting overnight brings the Morning Paper up over the desk: put it down before the picture.
            if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper && paper.Visible) {
                typeof(UIManager).GetMethod("DismissMorningPaper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(UIManager.Instance, null);
                await Frame(); await Frame();
            }
            foreach (var sc in Nodes(panel).OfType<ScrollContainer>()) sc.ScrollVertical = 160;
            await Frame();
            await Screenshot("new-listen");
            // An act already looking for a deal gets SIT DOWN ON TERMS instead, and that leads to the contract, not the talk.
            if (Nodes(panel).OfType<Button>().Any(b => b.Text == "TALK TO THEM")) {
            await Press("TALK TO THEM");
            var newlyHeard = desk.Slate.Last().Artist.artistId;
            var rehearsalQuote = desk.PreviewScoutingInvitation(newlyHeard, ScoutingAppointmentActivity.Rehearsal);
            var rehearsalButton = Nodes(panel).OfType<Button>().Single(b => b.IsVisibleInTree() && b.Text == "▸  Can I hear you rehearse, somewhere quieter?");
            Check(rehearsalButton.Disabled == (rehearsalQuote == null), "The Talk's rehearsal line respects the real post-set quote");
            await Screenshot("conversation-current");
            if (rehearsalQuote != null) {
                int invitationMinute = clock.CurrentHour * 60 + clock.CurrentMinute; float invitationCash = desk.Label.cashReserves;
                int heardBefore = desk.ScoutingDossiers.Single(d => d.ArtistId == newlyHeard).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance);
                rehearsalButton.EmitSignal(Button.SignalName.Pressed); await Frame();
                Check(clock.CurrentHour * 60 + clock.CurrentMinute == invitationMinute + 15 && desk.Label.cashReserves == invitationCash &&
                    desk.BuildTalkScene(newlyHeard).Lines.Any(l => l.isPlayer), "Asking spends 15 minutes, charges no attendance fee, and the answer comes back into The Talk");
                Check(desk.ScoutingDossiers.Single(d => d.ArtistId == newlyHeard).Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) == heardBefore, "Scheduling does not create another listening");
            }
            await Press("END THE CONVERSATION");
            }
            panel.OpenAtTab("ROLODEX"); await Frame();
            Check(panel.RolodexOpen && !panel.FilesOpen, "Rolodex opens independently of Files");
            await Press("BOOKERS", true);
            Check(Nodes(panel).OfType<Label>().Any(l => l.Text == room.ContactName), "Booker card renders in the real Rolodex");
            await Screenshot("rolodex");
            await Press("VENUE & CALENDAR");
            Check(Nodes(panel).OfType<PaperModal>().Count() == 1 && !Nodes(panel).OfType<Button>().Any(b => b.Text == "INTRODUCE YOURSELF"), "Rolodex opens the known booker's venue calendar");
            Nodes(panel).OfType<PaperModal>().Single().Close(); await Frame();
            var partialSet = desk.ScoutingItinerary().First(o => o.EndMinute - o.StartMinute >= 60);
            // Arrive three-quarters through the set: leave early by the walk when you're in another room (NEXT SETS orders by
            // leads, so which room the earlier press left you in depends on the town's talk).
            int partialStart = partialSet.StartMinute + (partialSet.EndMinute - partialSet.StartMinute) * 3 / 4 -
                (desk.IsAtSceneRoom(partialSet.RoomId) ? 0 : PlayerDesk.CrossTownMinutes);
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
