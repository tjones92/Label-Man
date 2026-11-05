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
    /// <summary>Rebuilds the page with this act's card header held where it was on screen, so what was under the cursor
    /// stays under it when a card opens, closes or re-sorts (pursued acts sort first).</summary>
    /// <remarks>The header is measured before <paramref name="change"/> runs: a desk change can rebuild the page on its
    /// own, and a freshly built header has no layout to measure yet.</remarks>
    private void RefreshAnchored(string artistId, Action change = null) {
        int keep = contentScroll.ScrollVertical;
        float? before = scoutingActHeaders.TryGetValue(artistId, out var old) && IsInstanceValid(old) && !old.IsQueuedForDeletion()
            ? old.GlobalPosition.Y - contentRoot.GlobalPosition.Y : null;
        change?.Invoke();
        Refresh();
        GetTree().CreateTimer(0.03).Timeout += () => {
            if (!IsInstanceValid(contentScroll)) return;
            int scroll = keep;
            if (before is float was && scoutingActHeaders.TryGetValue(artistId, out var header) && IsInstanceValid(header) && !header.IsQueuedForDeletion())
                scroll += Mathf.RoundToInt(header.GlobalPosition.Y - contentRoot.GlobalPosition.Y - was);
            contentScroll.ScrollVertical = scroll;
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
        var toggle = Band((open ? "▾  " : "▸  ") + title);
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

    /// <summary>A notebook act the player has since signed: it is the roster's now, so it leaves the scouting list.</summary>
    private static bool SignedByPlayer(PlayerDesk desk, string artistId) =>
        ArtistManager.Instance.GetArtist(artistId)?.labelId is { Length: > 0 } labelId && labelId == desk.Label?.labelId;

    private void ScoutingNotebook(PlayerDesk desk) {
        scoutingActHeaders.Clear();
        var dossiers = desk.ScoutingDossiers;
        if (desk.FoundingActSummary() is { } founding) Body(founding);
        if (dossiers.Count == 0) { Body("Every heard scene act is recorded automatically. Pinning sets priorities; it is optional."); return; }
        // Playtest 2026-11-04: signed acts stayed in the scouting list with PURSUE on them. They get one short line each
        // at the foot of the page instead; their notes are still a click away.
        var signed = dossiers.Where(d => SignedByPlayer(desk, d.ArtistId)).ToArray();
        var scouting = dossiers.Where(d => !SignedByPlayer(desk, d.ArtistId)).ToArray();
        int archived = scouting.Count(d => d.Priority == ScoutingPriority.Archived);
        if (archived > 0 || showArchivedScouting) {
            var archiveView = Btn(showArchivedScouting ? "HIDE ARCHIVED NOTES" : $"SHOW ARCHIVED NOTES ({archived})");
            archiveView.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            archiveView.Pressed += () => { showArchivedScouting = !showArchivedScouting; Refresh(); }; content.AddChild(archiveView);
        }
        var visible = scouting.Where(d => showArchivedScouting || d.Priority != ScoutingPriority.Archived)
            .OrderByDescending(d => d.Priority == ScoutingPriority.Pursue).ThenByDescending(d => d.Observations.LastOrDefault()?.AcquiredDay).ToArray();
        if (visible.Length > 0) Body("Open an act's card to see what you heard, then talk to them about a deal.");
        else if (scouting.Length > 0) Body("All notes are archived. Show archived notes to review them.");
        else Body("Every act you've heard is on your label now. Hear a set to find the next one.");
        foreach (var dossier in visible) ScoutingDossierCard(desk, dossier);
        if (signed.Length > 0)
            ScoutingSection("scout-signed", $"SIGNED TO YOUR LABEL  ({signed.Length})", false, () => {
                foreach (var dossier in signed) {
                    var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 10);
                    var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
                    var name = new Label { Text = dossier.ArtistName + (artist != null ? "  ·  " + GenreNameFormatter.Format(artist.primaryGenre) : "") + "  ·  on your roster",
                        SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
                    name.AddThemeFontSizeOverride("font_size", 15); name.AddThemeColorOverride("font_color", Ink);
                    row.AddChild(name);
                    var notes = ScoutingSmallButton("SCOUTING NOTES");
                    notes.TooltipText = "What you heard before you signed them.";
                    notes.Pressed += () => OpenScoutingFullNotes(desk, dossier);
                    row.AddChild(notes);
                    content.AddChild(row);
                }
                var roster = ScoutingSmallButton("GO TO THE ROSTER");
                roster.Pressed += () => GoToTab(1, PageRoster);
                content.AddChild(roster);
            });
    }

    private void ScoutingDossierCard(PlayerDesk desk, ScoutingDossier dossier) {
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
            string mark = dossier.Priority switch { ScoutingPriority.Pursue => "  ·  pursuing", ScoutingPriority.Watch => "  ·  watching", ScoutingPriority.Archived => "  ·  archived", _ => "" };
            var header = Band($"{(expanded ? "▾" : "▸")}  {dossier.ArtistName}{mark}");
            header.Alignment = HorizontalAlignment.Left;
            header.AddThemeFontOverride("font", PaperTheme.SerifBold);
            header.AddThemeFontSizeOverride("font_size", 21);
            foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_disabled_color" }) header.AddThemeColorOverride(key, ScoutingArtistInk);
            StyleBoxFlat HeaderBox(Color fill) => new() { BgColor = fill, BorderColor = ScoutingArtistInk, BorderWidthBottom = 1,
                ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 5, ContentMarginBottom = 5 };
            header.AddThemeStyleboxOverride("normal", HeaderBox(new Color("eadbb6")));
            header.AddThemeStyleboxOverride("hover", HeaderBox(new Color("f6ebd0")));
            header.AddThemeStyleboxOverride("pressed", HeaderBox(new Color("e2d1a4")));
            header.Pressed += () => { expandedScoutingArtistId = expanded ? null : dossier.ArtistId; RefreshAnchored(dossier.ArtistId); };
            content.AddChild(header);
            scoutingActHeaders[dossier.ArtistId] = header;
            var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
            var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
            // One deal status, read from the same places the conversation reads, so the top and foot of the card agree.
            if (artist != null) Body(GenreNameFormatter.Format(artist.primaryGenre) + " · " +
                (string.IsNullOrEmpty(artist.labelId) ? "Unsigned · " + ScoutingDealStatus(desk, artist, prospect)
                : "Signed to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "another label")));
            var last = dossier.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance);
            if (last != null) Body($"Last heard: {last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name ?? "unknown room"} · {LocalSceneRoomService.Date(last.OccurredDay ?? last.AcquiredDay).ToHeadlineString()}");
            if (expanded) ScoutingActDetails(desk, dossier);
        } finally { content = parent; }
    }

    private static string ShortDay(int day) { var d = LocalSceneRoomService.Date(day); return $"{d.ShortMonthName} {d.day}"; }

    /// <summary>Where a deal with an unsigned act stands, in a few words.</summary>
    private static string ScoutingDealStatus(PlayerDesk desk, SimulatedArtist artist, PlayerDesk.Prospect prospect) {
        var approach = desk.DirectApproaches.LastOrDefault(a => a.ArtistId == artist.artistId);
        int today = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        switch (approach?.Status) {
            case ScoutingApproachStatus.Considering: return $"thinking over your interest · answer by {ShortDay(approach.UntilDay)}";
            case ScoutingApproachStatus.Ready: return "will hear an offer · talk to them after a set you've heard that day";
            case ScoutingApproachStatus.Declined when today < approach.UntilDay: return $"staying local this season · ask again after {ShortDay(approach.UntilDay)}";
        }
        if (prospect == null) return "talk to them to find out if they'd sign";
        string deal = PlayerDesk.SceneDealDescription(prospect);
        return deal == "not looking for a record deal" ? "not looking for a deal · you can ask if they'd hear one" : deal;
    }

    private void ScoutingActDetails(PlayerDesk desk, ScoutingDossier dossier) {
        var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
        if (artist == null) Body("Historical entry; this act is no longer available.");
        else if (!string.IsNullOrEmpty(artist.labelId)) Body("Under contract to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "a label") + ". Hearing an act does not make it available to sign.");
        ScoutingSection("scout-evidence:" + dossier.ArtistId, "WHAT YOU'VE HEARD", true, () => {
            ScoutingListeningSummary(desk, dossier);
        });
        // Playtest 2026-11-04: nine equal buttons, and the player couldn't tell which one was "sign them". The deal is
        // the one solid button on the card; everything else folds away under it.
        if (artist != null && string.IsNullOrEmpty(artist.labelId)) ScoutingOfferBar(desk, dossier, artist);
        else if (artist != null) {
            var talk = ScoutingSmallButton("TALK TO THEM");
            talk.TooltipText = "They're under contract, but you can still ask about their past label decisions.";
            talk.Pressed += () => OpenScoutingConversation(desk, dossier); content.AddChild(talk);
        }
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
        ScoutingSection("scout-more:" + dossier.ArtistId, "MORE  —  ASK AROUND  ·  OTHER ACTS  ·  NOTEBOOK", false, () => {
            if (dossier.Observations.Any(o => o.Kind is ScoutingEvidenceKind.RivalInterest or ScoutingEvidenceKind.PublicReport))
                ScoutingText(desk.ScoutingCompetitionSummary(dossier.ArtistId), 14, ScoutingVenueInk);
            ScoutingSection("scout-questions:" + dossier.ArtistId, "ASK A BOOKER ABOUT " + dossier.ArtistName.ToUpperInvariant(), false, () => {
                ScoutingSourceQuestions(desk, dossier);
            });
            if (desk.ScoutingOtherKnownLeads(dossier.ArtistId) is { } alternatives)
                ScoutingSection("scout-alternatives:" + dossier.ArtistId, "OTHER ACTS TO CONSIDER", false, () => {
                    Body(alternatives);
                    ScoutingLossFollowThrough(desk, dossier.ArtistId);
                });
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 6);
            if (artist == null || string.IsNullOrEmpty(artist.labelId) || artist.labelId != desk.Label.labelId) {
                // Watch / pursue / archive is one choice: the act's place in the notebook. The current one is the pressed-in button.
                row.AddChild(FaintLine("Notebook:"));
                foreach (var priority in new[] { ScoutingPriority.Watch, ScoutingPriority.Pursue, ScoutingPriority.Archived }) {
                    var chosen = priority;
                    var button = ChoiceButton(priority == ScoutingPriority.Archived ? "ARCHIVE" : priority.ToString().ToUpperInvariant(), dossier.Priority == priority,
                        () => RefreshAnchored(dossier.ArtistId, () => desk.SetScoutingPriority(dossier.ArtistId, chosen)));
                    button.TooltipText = priority switch {
                        ScoutingPriority.Watch => "Keep this act on your radar. A rival label that moves on a watched act has to come and compete for them, not sign them quietly.",
                        ScoutingPriority.Pursue => "As WATCH, and this act sorts to the top of your notebook and of other acts to consider.",
                        _ => "File the notes away. Archived acts leave the notebook until you show archived notes."
                    };
                    row.AddChild(button);
                }
                row.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0) });
            }
            ScoutingDossierExtras(desk, dossier, row);
        });
    }

    /// <summary>The card's one primary verb: talk to the act, which is where an offer is made. Opening it also looks up the
    /// asking price (free, no time), which used to be its own button.</summary>
    private void ScoutingOfferBar(PlayerDesk desk, ScoutingDossier dossier, SimulatedArtist artist) {
        var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
        var bar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddThemeConstantOverride("separation", 14);
        var offer = Primary("TALK TO THEM  ·  MAKE AN OFFER");
        offer.CustomMinimumSize = new Vector2(300, 42);
        offer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        offer.TooltipText = "Open the conversation: ask if they'd hear an offer, discuss terms, or arrange a rehearsal or demo. Opening it is free.";
        offer.Pressed += () => {
            if (desk.Slate.All(p => p.Artist?.artistId != dossier.ArtistId)) desk.OpenScoutingDossier(dossier.ArtistId, out _);
            OpenScoutingConversation(desk, dossier);
        };
        bar.AddChild(offer);
        string hint;
        if (prospect != null && desk.CanAskDirectApproach(artist)) hint = "They aren't looking for a deal, but you can ask if they'd hear one. Asking is free.";
        else if (prospect != null && PlayerDesk.SceneDealAvailable(prospect)) hint = $"Ready to talk terms · {PlayerDesk.SceneDealDescription(prospect)}.";
        else {
            var next = desk.NextReachableScoutingSet(dossier.ArtistId);
            hint = "Terms are talked after a set you've heard that day." + (next == null ? " Check the venue calendars for their next date."
                : $" Next: {next.RoomName}, {ShortDay(next.Day)} at {Hour12(next.StartMinute / 60)}.");
        }
        var note = new Label { Text = hint, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        note.AddThemeFontSizeOverride("font_size", 14); note.AddThemeColorOverride("font_color", Heard);
        bar.AddChild(note);
        content.AddChild(bar);
    }

    private void OpenScoutingFullNotes(PlayerDesk desk, ScoutingDossier dossier) {
        var modal = PaperModal.Open(this, dossier.ArtistName + " · FULL NOTES", 850);
        var scroll = ComparisonScroll(); var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; notes.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(notes); modal.Body.AddChild(scroll);
        WithinScouting(notes, () => { foreach (string line in desk.ScoutingPlayerNotes(dossier.ArtistId)) ScoutingNote(line); });
        modal.AddButton("CLOSE", null);
    }

    /// <summary>The two read-only views of an act's notes, at the foot of its card.</summary>
    private void ScoutingDossierExtras(PlayerDesk desk, ScoutingDossier dossier, HFlowContainer row) {
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
