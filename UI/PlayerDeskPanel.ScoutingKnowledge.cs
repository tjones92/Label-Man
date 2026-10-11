using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private bool showArchivedScouting;
    private void ScoutingNotebook(PlayerDesk desk) {
        if (!LocalScenes.Rooms) ScoutingMeetingAgenda(desk);
        var dossiers = desk.ScoutingDossiers;
        if (desk.FoundingActSummary() is { } founding) Body(founding);
        if (dossiers.Count == 0) { Body("Every heard scene act is recorded automatically. Pinning sets priorities; it is optional."); return; }
        var archiveView = Btn(showArchivedScouting ? "HIDE ARCHIVED NOTES" : "SHOW ARCHIVED NOTES");
        archiveView.Pressed += () => { showArchivedScouting = !showArchivedScouting; Refresh(); }; content.AddChild(archiveView);
        foreach (var dossier in dossiers.Where(d => showArchivedScouting || d.Priority != ScoutingPriority.Archived)
            .OrderByDescending(d => d.Priority == ScoutingPriority.Pursue).ThenByDescending(d => d.Observations.LastOrDefault()?.AcquiredDay)) {
            var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
            Heading($"{dossier.ArtistName} · {dossier.Priority}");
            if (artist == null) Body("Historical entry; this act is no longer available.");
            else if (!string.IsNullOrEmpty(artist.labelId)) Body("Under contract to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "a label") + ". Hearing an act does not make it available to sign.");
            foreach (string line in desk.ScoutingSummary(dossier.ArtistId)) Body(line);
            ScoutingLossFollowThrough(desk, dossier.ArtistId);
            var questionRow = new HBoxContainer();
            var question = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            foreach (var value in Enum.GetValues<ScoutingQuestion>()) question.AddItem(PlayerDesk.ScoutingQuestionText(value), (int)value);
            question.Select((int)dossier.Question);
            question.TooltipText = "Pin a question for another date. Choosing a question spends no time and changes no evidence.";
            question.ItemSelected += index => { desk.SetScoutingQuestion(dossier.ArtistId, (ScoutingQuestion)question.GetItemId((int)index)); Refresh(); };
            questionRow.AddChild(question); content.AddChild(questionRow);
            ScoutingSourceQuestions(desk, dossier);
            ScoutingBusinessConversation(desk, dossier);
            ScoutingMeetingInvitation(desk, dossier);
            var row = new HFlowContainer();
            var next = desk.NextReachableScoutingSet(dossier.ArtistId);
            if (next != null) {
                var follow = Btn(next.Playing ? $"HEAR NEXT SET ({next.ListeningMinutes}min · {Money(next.Admission)})" : "WAIT FOR NEXT SET");
                follow.Disabled = next.Playing && desk.Label.cashReserves < next.Admission;
                follow.TooltipText = next.Playing ? "Listen to this actual appearance; its availability is rechecked." :
                    $"Wait until {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}, {next.StartMinute / 60:00}:{next.StartMinute % 60:00}. Ordinary expenses and decisions continue. Admission is charged only when you choose to listen.";
                ScoutingTimeRisk(desk, follow, next.Day, next.Playing ? next.EndMinute : next.StartMinute);
                follow.Pressed += () => Act(() => {
                    string result;
                    bool ok = next.Playing ? desk.ListenSceneAppearance(next.BillId, next.EventId, out result) :
                        desk.WaitForSceneAppearance(next.BillId, next.EventId, out result);
                    Say(result, ok); return ok;
                });
                row.AddChild(follow);
            }
            var open = Btn("OPEN ON THE PAD"); open.Disabled = artist == null;
            open.Pressed += () => Act(() => { bool ok = desk.OpenScoutingDossier(dossier.ArtistId, out string result); Say(result, ok); return ok; }); row.AddChild(open);
            foreach (var priority in new[] { ScoutingPriority.Watch, ScoutingPriority.Pursue, ScoutingPriority.Archived }) {
                var button = Btn(priority == ScoutingPriority.Archived ? "ARCHIVE" : priority.ToString().ToUpperInvariant());
                button.Disabled = dossier.Priority == priority;
                button.Pressed += () => { desk.SetScoutingPriority(dossier.ArtistId, priority); Refresh(); }; row.AddChild(button);
            }
            var audience = Btn("AUDIENCE EVIDENCE"); audience.Pressed += () => {
                var modal = PaperModal.OpenClipboard(this, dossier.ArtistName + " — AUDIENCE", 850);
                var scroll = ComparisonScroll();
                var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                notes.AddThemeConstantOverride("separation", 12);
                scroll.AddChild(notes); modal.Body.AddChild(scroll);
                foreach (string line in desk.ScoutingAudienceDetails(dossier.ArtistId)) notes.AddChild(FaintLine(line));
                modal.AddButton("DONE", null, PaperModal.ButtonKind.Primary);
            }; row.AddChild(audience);
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
    private void ScoutingLossFollowThrough(PlayerDesk desk, string artistId) {
        foreach (var lead in desk.ScoutingAlternativeLeads(artistId)) {
            Body($"{lead.ArtistName}: last heard at {lead.RoomName}, {LocalSceneRoomService.Date(lead.LastHeardDay).ToHeadlineString()}. " +
                (lead.NextSet == null ? "No unheard local set is announced within your working hours." :
                $"Next: {lead.NextSet.RoomName}, {LocalSceneRoomService.Date(lead.NextSet.Day).ToHeadlineString()}, {lead.NextSet.StartMinute / 60:00}:{lead.NextSet.StartMinute % 60:00}; {lead.NextSet.ListeningMinutes}min, {Money(lead.NextSet.Admission)} admission at the current quote."));
            var actions = new HFlowContainer();
            var notes = Btn("REVIEW " + lead.ArtistName.ToUpperInvariant());
            notes.Pressed += () => Act(() => { bool ok = desk.OpenScoutingDossier(lead.ArtistId, out string result); Say(result, ok); return ok; });
            actions.AddChild(notes);
            if (lead.NextSet is { } next) {
                var hear = Btn(next.Playing ? $"HEAR {lead.ArtistName.ToUpperInvariant()} ({next.ListeningMinutes}min · {Money(next.Admission)})" : "WAIT FOR " + lead.ArtistName.ToUpperInvariant());
                hear.Disabled = next.Playing && desk.Label.cashReserves < next.Admission;
                hear.TooltipText = "Follow this announced appearance. Waiting advances ordinary expenses and decisions; it does not listen, charge admission or reserve the act. Availability is checked again when you act.";
                ScoutingTimeRisk(desk, hear, next.Day, next.Playing ? next.EndMinute : next.StartMinute);
                hear.Pressed += () => Act(() => {
                    string result;
                    bool ok = next.Playing ? desk.ListenSceneAppearance(next.BillId, next.EventId, out result) :
                        desk.WaitForSceneAppearance(next.BillId, next.EventId, out result);
                    Say(result, ok); return ok;
                }); actions.AddChild(hear);
            }
            content.AddChild(actions);
        }
    }
    private void ScoutingBusinessConversation(PlayerDesk desk, ScoutingDossier dossier) {
        var meeting = desk.PreviewScoutingBusinessMeeting(dossier.ArtistId);
        if (meeting == null) return;
        Body($"Speak with {meeting.SourceName} after the set. Ask about past label decisions; this is a conversation, not more listening.");
        var ask = Btn("ASK ACT ABOUT PAST LABEL DECISIONS (1h · no fee)");
        ask.Disabled = !meeting.Available;
        ask.TooltipText = "Ask about past label decisions. Their account may be incomplete and does not establish a current offer or deadline. The conversation takes one hour and ends this room visit; later listening requires admission again.";
        if (!meeting.Available) Body(meeting.UnavailableReason);
        ask.Pressed += () => Act(() => {
            bool ok = desk.AskScoutingBusinessHistory(dossier.ArtistId, meeting.QuoteId, out string result);
            Say(result, ok); return ok;
        }); content.AddChild(ask);
    }
    private void ScoutingSourceQuestions(PlayerDesk desk, ScoutingDossier dossier) {
        if (!LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || !LocalScenes.Rooms) return;
        var rooms = dossier.Observations.Select(o => o.RoomId).Distinct().Select(SceneRoomCatalog.Get)
            .Where(r => r != null && r.PlaceId == LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId)).ToArray();
        if (rooms.Length == 0) return;
        Body("Ask a local booker: one hour, no conversation fee. A source answer does not count as another listening.");
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var roomChoice = new OptionButton();
        foreach (var room in rooms) roomChoice.AddItem(room.Name + " — " + room.ContactName);
        var questionChoice = new OptionButton();
        foreach (var value in Enum.GetValues<ScoutingSourceQuestion>()) questionChoice.AddItem(PlayerDesk.ScoutingSourceQuestionText(value), (int)value);
        questionChoice.TooltipText = "Choose a source question; selecting or refreshing spends no time and creates no evidence.";
        var ask = Btn("ASK BOOKER (1h · no fee)");
        var availability = FaintLine("");
        ScoutingSourceAnswer preview = null;
        void UpdatePreview() {
            preview = desk.PreviewScoutingSource(rooms[roomChoice.Selected].Id, dossier.ArtistId,
                (ScoutingSourceQuestion)questionChoice.GetItemId(questionChoice.Selected));
            ask.Disabled = preview?.Available != true;
            availability.Text = preview == null ? "This source is unavailable." : preview.Available ?
                "Available during posted room hours; the conversation advances ordinary expenses and decisions." : preview.UnavailableReason;
        }
        roomChoice.ItemSelected += _ => UpdatePreview(); questionChoice.ItemSelected += _ => UpdatePreview();
        ask.Pressed += () => Act(() => {
            bool ok = desk.AskScoutingSource(rooms[roomChoice.Selected].Id, dossier.ArtistId,
                (ScoutingSourceQuestion)questionChoice.GetItemId(questionChoice.Selected), preview?.QuoteId, out string result);
            Say(result, ok); return ok;
        });
        UpdatePreview(); choices.AddChild(roomChoice); choices.AddChild(questionChoice); choices.AddChild(availability); choices.AddChild(ask);
        content.AddChild(choices);
    }

}
