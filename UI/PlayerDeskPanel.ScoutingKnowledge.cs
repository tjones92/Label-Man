using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private bool showArchivedScouting;
    private string expandedScoutingArtistId;
    private readonly System.Collections.Generic.Dictionary<string, Control> scoutingActHeaders = new();
    private void FocusScoutingAct(string artistId) {
        expandedScoutingArtistId = artistId;
        Refresh();
        GetTree().CreateTimer(0.03).Timeout += () => {
            if (scoutingActHeaders.TryGetValue(artistId, out var header) && IsInstanceValid(header) && !header.IsQueuedForDeletion())
                contentScroll.ScrollVertical += Mathf.RoundToInt(header.GlobalPosition.Y - contentScroll.GlobalPosition.Y);
        };
    }
    private void ShowLatestScoutingAct(PlayerDesk desk) {
        scoutingView = "ACT NOTEBOOK";
        if (desk.Slate.LastOrDefault()?.Artist?.artistId is { } artistId) FocusScoutingAct(artistId);
    }
    // Keep nested controls inside their act even when another section changes content.
    private void ScoutingSection(string key, string title, bool defaultOpen, Action build) {
        var parent = content;
        bool open = sectionOpen.TryGetValue(key, out bool chosen) ? chosen : defaultOpen;
        var toggle = Btn((open ? "▾  " : "▸  ") + title);
        toggle.Alignment = HorizontalAlignment.Left;
        toggle.Pressed += () => { sectionOpen[key] = !open; RefreshKeepingScroll(); };
        parent.AddChild(toggle);
        if (!open) return;
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 8);
        parent.AddChild(body);
        content = body;
        try { build(); } finally { content = parent; }
    }

    private void ScoutingNotebook(PlayerDesk desk) {
        scoutingActHeaders.Clear();
        var dossiers = desk.ScoutingDossiers;
        if (desk.FoundingActSummary() is { } founding) Body(founding);
        if (dossiers.Count == 0) { Body("Every heard scene act is recorded automatically. Pinning sets priorities; it is optional."); return; }
        var archiveView = Btn(showArchivedScouting ? "HIDE ARCHIVED NOTES" : "SHOW ARCHIVED NOTES");
        archiveView.Pressed += () => { showArchivedScouting = !showArchivedScouting; Refresh(); }; content.AddChild(archiveView);
        var visible = dossiers.Where(d => showArchivedScouting || d.Priority != ScoutingPriority.Archived)
            .OrderByDescending(d => d.Priority == ScoutingPriority.Pursue).ThenByDescending(d => d.Observations.LastOrDefault()?.AcquiredDay).ToArray();
        Body("Choose an act to review its evidence, plan another listen, or discuss a deal.");
        if (visible.Length == 0) Body("All notes are archived. Show archived notes to review them.");
        foreach (var dossier in visible) {
            var parent = content;
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            card.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
                BgColor = new Color("f3e4bb"), BorderColor = new Color("bca77e"),
                BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
                ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 12, ContentMarginBottom = 12
            });
            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            body.AddThemeConstantOverride("separation", 8); card.AddChild(body); parent.AddChild(card);
            content = body;
            try {
                bool expanded = expandedScoutingArtistId == dossier.ArtistId;
                var header = Btn($"{(expanded ? "▾" : "▸")}  {dossier.ArtistName} · {dossier.Priority}");
                header.Alignment = HorizontalAlignment.Left;
                header.AddThemeFontOverride("font", PaperTheme.SerifBold);
                header.AddThemeFontSizeOverride("font_size", 21);
                foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color" }) header.AddThemeColorOverride(key, ScoutingArtistInk);
                StyleBoxFlat HeaderBox(Color fill) => new() { BgColor = fill, BorderColor = ScoutingArtistInk, BorderWidthBottom = 1,
                    ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 5, ContentMarginBottom = 5 };
                header.AddThemeStyleboxOverride("normal", HeaderBox(new Color("eadbb6")));
                header.AddThemeStyleboxOverride("hover", HeaderBox(new Color("f6ebd0")));
                header.AddThemeStyleboxOverride("pressed", HeaderBox(new Color("e2d1a4")));
                header.Pressed += () => { if (expanded) { expandedScoutingArtistId = null; RefreshKeepingScroll(); } else FocusScoutingAct(dossier.ArtistId); };
                content.AddChild(header);
                scoutingActHeaders[dossier.ArtistId] = header;
                var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
                if (artist != null) Body(GenreNameFormatter.Format(artist.primaryGenre) + " · " +
                    (string.IsNullOrEmpty(artist.labelId) ? "Unsigned · interest in a deal needs checking" :
                    artist.labelId == desk.Label.labelId ? "On your roster" : "Signed to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "another label")));
                var last = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
                if (last != null) Body($"Last heard: {last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name ?? "unknown room"} · {LocalSceneRoomService.Date(last.OccurredDay ?? last.AcquiredDay).ToHeadlineString()}");
                if (!expanded) continue;
                ScoutingActDetails(desk, dossier);
            } finally { content = parent; }
        }
    }

    private void ScoutingActDetails(PlayerDesk desk, ScoutingDossier dossier) {
        var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
        if (artist == null) Body("Historical entry; this act is no longer available.");
        else if (!string.IsNullOrEmpty(artist.labelId)) Body("Under contract to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "a label") + ". Hearing an act does not make it available to sign.");
        ScoutingSection("scout-evidence:" + dossier.ArtistId, "WHAT YOU'VE HEARD", true, () => {
            ScoutingListeningSummary(desk, dossier);
        });
        ScoutingSection("scout-listen:" + dossier.ArtistId, "PLAN ANOTHER LISTEN", false, () => {
            var question = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            foreach (var value in Enum.GetValues<ScoutingQuestion>()) question.AddItem(PlayerDesk.ScoutingQuestionText(value), (int)value);
            question.Select((int)dossier.Question);
            question.TooltipText = "Pin a question for another date. Choosing a question spends no time and changes no evidence.";
            question.ItemSelected += index => { desk.SetScoutingQuestion(dossier.ArtistId, (ScoutingQuestion)question.GetItemId((int)index)); RefreshKeepingScroll(); };
            content.AddChild(question);
            var next = desk.NextReachableScoutingSet(dossier.ArtistId);
            if (next != null) ScoutingSetPreview(desk, next);
            else ScoutingText("No unheard local date posted. Browse venues or ask a booker.", 14, Heard);
        });
        ScoutingSection("scout-questions:" + dossier.ArtistId, "ASK ABOUT " + dossier.ArtistName.ToUpperInvariant(), false, () => {
            ScoutingSourceQuestions(desk, dossier);
        });
        var talk = ScoutingSmallButton("TALK TO ACT");
        talk.Disabled = artist == null;
        talk.Pressed += () => OpenScoutingConversation(desk, dossier); content.AddChild(talk);
        if (desk.ScoutingOtherKnownLeads(dossier.ArtistId) is { } alternatives)
            ScoutingSection("scout-alternatives:" + dossier.ArtistId, "OTHER ACTS TO CONSIDER", false, () => {
                Body(alternatives);
                ScoutingLossFollowThrough(desk, dossier.ArtistId);
            });
        Heading("DEAL & NOTEBOOK");
        if (dossier.Observations.Any(o => o.Kind is ScoutingEvidenceKind.RivalInterest or ScoutingEvidenceKind.PublicReport))
            ScoutingText(desk.ScoutingCompetitionSummary(dossier.ArtistId), 14, ScoutingVenueInk);
        var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
        if (prospect != null) ScoutingText(PlayerDesk.SceneDealDescription(prospect), 14, Heard);
        else {
            var open = Btn("REVIEW DEAL OPTIONS"); open.Disabled = artist == null;
            open.Pressed += () => Act(() => { bool ok = desk.OpenScoutingDossier(dossier.ArtistId, out string result); Say(result, ok); return ok; }); content.AddChild(open);
        }
        var row = new HFlowContainer();
        foreach (var priority in new[] { ScoutingPriority.Watch, ScoutingPriority.Pursue, ScoutingPriority.Archived }) {
            var button = Btn(priority == ScoutingPriority.Archived ? "ARCHIVE" : priority.ToString().ToUpperInvariant());
            button.Disabled = dossier.Priority == priority;
            button.Pressed += () => { desk.SetScoutingPriority(dossier.ArtistId, priority); Refresh(); }; row.AddChild(button);
        }
        var audience = Btn("ROOM IMPRESSION"); audience.Pressed += () => {
            var modal = PaperModal.Open(this, dossier.ArtistName + " · ROOM IMPRESSION", 680);
            WithinScouting(modal.Body, () => { foreach (string line in ScoutingAudience.RoomImpression(dossier)) ScoutingText(line, 17, Ink); });
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
    private void ScoutingLossFollowThrough(PlayerDesk desk, string artistId) {
        foreach (var lead in desk.ScoutingAlternativeLeads(artistId)) {
            Body($"{lead.ArtistName}: last heard at {lead.RoomName}, {LocalSceneRoomService.Date(lead.LastHeardDay).ToHeadlineString()}. " +
                (lead.NextSet == null ? "No unheard local set is announced within your working hours." :
                $"Next: {lead.NextSet.RoomName}, {LocalSceneRoomService.Date(lead.NextSet.Day).ToHeadlineString()}, {lead.NextSet.StartMinute / 60:00}:{lead.NextSet.StartMinute % 60:00}; {lead.NextSet.ListeningMinutes}min, {Money(lead.NextSet.Admission)} admission at the current quote."));
            var actions = new HFlowContainer();
            var notes = Btn("REVIEW " + lead.ArtistName.ToUpperInvariant());
            notes.Pressed += () => { showArchivedScouting = false; FocusScoutingAct(lead.ArtistId); };
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
    private void ScoutingBusinessConversation(PlayerDesk desk, ScoutingDossier dossier, Action beforeAction = null) {
        var meeting = desk.PreviewScoutingBusinessMeeting(dossier.ArtistId);
        if (meeting == null) return;
        Body($"Speak with {meeting.SourceName} after the set. Ask about past label decisions; this is a conversation, not more listening.");
        var ask = ScoutingSmallButton("PAST LABEL DECISIONS");
        ask.Disabled = !meeting.Available;
        ask.TooltipText = "Ask about past label decisions. Their account may be incomplete and does not establish a current offer or deadline. The conversation takes one hour and ends this room visit; later listening requires admission again.";
        if (!meeting.Available) Body(meeting.UnavailableReason);
        ask.Pressed += () => Act(() => {
            beforeAction?.Invoke();
            bool ok = desk.AskScoutingBusinessHistory(dossier.ArtistId, meeting.QuoteId, out string result);
            Say(result, ok); return ok;
        }); content.AddChild(ask);
    }
    private void ScoutingSourceQuestions(PlayerDesk desk, ScoutingDossier dossier) {
        if (!LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || !LocalScenes.Rooms) return;
        var rooms = dossier.Observations.Select(o => o.RoomId).Distinct().Select(SceneRoomCatalog.Get)
            .Where(r => r != null && r.PlaceId == LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId)).ToArray();
        if (rooms.Length == 0) return;
        Body($"Ask a booker about {dossier.ArtistName} · 1h · no fee. This adds a source's account, not another listening.");
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var roomChoice = new OptionButton();
        foreach (var room in rooms) roomChoice.AddItem(room.Name + (desk.KnowsSceneBooker(room.Id) ? " — " + room.ContactName : " — booker not met"));
        var questionChoice = new OptionButton();
        foreach (var value in Enum.GetValues<ScoutingSourceQuestion>()) questionChoice.AddItem(PlayerDesk.ScoutingSourceQuestionText(value), (int)value);
        questionChoice.TooltipText = "Choose a source question; selecting or refreshing spends no time and creates no evidence.";
        var ask = Btn("ASK ABOUT " + dossier.ArtistName.ToUpperInvariant() + " (1h · no fee)");
        var availability = FaintLine("");
        var introduce = ScoutingSmallButton("INTRODUCE YOURSELF · 15min");
        introduce.Pressed += () => Act(() => { bool ok = desk.IntroduceSceneBooker(rooms[roomChoice.Selected].Id, out string result); Say(result, ok); return ok; });
        ScoutingSourceAnswer preview = null;
        void UpdatePreview() {
            preview = desk.PreviewScoutingSource(rooms[roomChoice.Selected].Id, dossier.ArtistId,
                (ScoutingSourceQuestion)questionChoice.GetItemId(questionChoice.Selected));
            bool known = desk.KnowsSceneBooker(rooms[roomChoice.Selected].Id);
            ask.Disabled = !known || preview?.Available != true;
            questionChoice.Disabled = !known;
            introduce.Visible = !known;
            introduce.Disabled = !desk.CanIntroduceSceneBooker(rooms[roomChoice.Selected].Id, out string reason);
            introduce.TooltipText = introduce.Disabled ? reason : "15min · no fee. Add this booker to your Rolodex.";
            availability.Text = preview == null ? "This source is unavailable." : preview.Available ?
                "Available during posted room hours; the conversation advances ordinary expenses and decisions." : preview.UnavailableReason;
            if (!known) availability.Text = "Meet this venue's booker before asking about the act.";
        }
        roomChoice.ItemSelected += _ => UpdatePreview(); questionChoice.ItemSelected += _ => UpdatePreview();
        ask.Pressed += () => Act(() => {
            bool ok = desk.AskScoutingSource(rooms[roomChoice.Selected].Id, dossier.ArtistId,
                (ScoutingSourceQuestion)questionChoice.GetItemId(questionChoice.Selected), preview?.QuoteId, out string result);
            Say(result, ok); return ok;
        });
        UpdatePreview(); choices.AddChild(roomChoice); choices.AddChild(introduce); choices.AddChild(questionChoice); choices.AddChild(availability); choices.AddChild(ask);
        content.AddChild(choices);
    }

}
