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
        var toggle = FoldHeading(title, open, size: 14);
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
        if (dossiers.Count == 0) { Body("Every act you hear goes in here."); return; }
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
            // A band says so when a relative handles them; any other manager you learn about when you ask.
            if (last != null && artist != null && string.IsNullOrEmpty(artist.labelId) && ManagerIdentity.Today(artist) is { Kind: ManagerKind.Family } family)
                Body($"They say: \"{ManagerIdentity.BandSays(family)}\" ({family.Name})");
            if (expanded) ScoutingActDetails(desk, dossier);
        } finally { content = parent; }
    }

    private static string ShortDay(int day) { var d = LocalSceneRoomService.Date(day); return $"{d.ShortMonthName} {d.day}"; }

    /// <summary>Where a deal with an unsigned act stands, in a few words.</summary>
    private static string ScoutingDealStatus(PlayerDesk desk, SimulatedArtist artist, PlayerDesk.Prospect prospect) {
        var approach = desk.DirectApproaches.LastOrDefault(a => a.ArtistId == artist.artistId);
        int today = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        switch (approach?.Status) {
            case ScoutingApproachStatus.Considering: return $"talking it over · answer by {ShortDay(approach.UntilDay)}";
            case ScoutingApproachStatus.Ready: return "will hear an offer · open their card to talk terms";
            case ScoutingApproachStatus.Agreed when desk.Agreement(artist) != null:
                return desk.NextApproachStep(artist) is { } step ? "said yes · " + PlayerDesk.StepShort(step) : "said yes · sit down on terms";
            case ScoutingApproachStatus.Declined when desk.StillDeclining(artist): return "said no · " + ActDisposition.Short(desk.ReadDisposition(artist));
            case ScoutingApproachStatus.WantsAnotherLook when desk.WaitingToBeHeardAgain(artist): return "want to see you again first";
        }
        if (prospect == null) return "talk to them to find out if they'd sign";
        string deal = PlayerDesk.SceneDealDescription(prospect);
        return deal == PlayerDesk.NoDealYet ? "no deal yet · nobody's asked them" : deal;
    }

    /// <summary>An open act card, in three zones (signing audit §3.1): what you heard, the one thing to do next, and a
    /// quiet row for filing and notes. Everything else (the next listen, bookers, other acts) folds under MORE.</summary>
    private void ScoutingActDetails(PlayerDesk desk, ScoutingDossier dossier) {
        var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
        if (artist == null) Body("Historical entry; this act is no longer available.");
        else if (!string.IsNullOrEmpty(artist.labelId)) Body("Under contract to " + (ChartManager.Instance.GetLabelName(artist.labelId) ?? "a label") + ". Hearing an act does not make it available to sign.");
        ScoutingSection("scout-evidence:" + dossier.ArtistId, "WHAT YOU'VE HEARD", true, () => {
            ScoutingListeningSummary(desk, dossier);
        });
        if (artist != null && string.IsNullOrEmpty(artist.labelId)) ScoutingOfferBar(desk, dossier, artist);
        else if (artist != null) {
            var talk = ScoutingSmallButton("TALK TO THEM");
            talk.TooltipText = "They're under contract, but you can still ask about their past label decisions.";
            talk.Pressed += () => OpenTheTalk(desk, dossier); content.AddChild(talk);
        }
        ScoutingNotebookStrip(desk, dossier, artist);
        ScoutingSection("scout-more:" + dossier.ArtistId, "MORE  —  NEXT LISTEN  ·  ASK AROUND", false, () => {
            ScoutingSubhead("NEXT LISTEN");
            var question = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            foreach (var value in Enum.GetValues<ScoutingQuestion>()) question.AddItem(PlayerDesk.ScoutingQuestionText(value), (int)value);
            question.Select((int)dossier.Question);
            question.TooltipText = "What to listen for next time. Choosing it takes no time.";
            question.SetMeta("scouting_question", dossier.ArtistId);
            question.ItemSelected += index => { desk.SetScoutingQuestion(dossier.ArtistId, (ScoutingQuestion)question.GetItemId((int)index)); RefreshKeepingScroll(); };
            content.AddChild(question);
            var next = desk.NextReachableScoutingSet(dossier.ArtistId);
            if (next != null) ScoutingSetPreview(desk, next);
            else ScoutingText("No date of theirs you haven't heard is posted nearby. Browse the venues or ask a booker.", 14, Heard);
            if (dossier.Observations.Any(o => o.Kind is ScoutingEvidenceKind.RivalInterest or ScoutingEvidenceKind.PublicReport)) {
                ScoutingSubhead("WHO ELSE IS INTERESTED");
                ScoutingText(desk.ScoutingCompetitionSummary(dossier.ArtistId), 14, ScoutingVenueInk);
            }
            ScoutingSubhead("ASK A BOOKER ABOUT " + dossier.ArtistName.ToUpperInvariant());
            ScoutingSourceQuestions(desk, dossier);
            if (desk.ScoutingOtherKnownLeads(dossier.ArtistId) is { } alternatives) {
                ScoutingSubhead("OTHER ACTS TO CONSIDER");
                Body(alternatives);
                ScoutingLossFollowThrough(desk, dossier.ArtistId);
            }
        });
    }

    private void ScoutingSubhead(string text) => ScoutingText(text, 13, Rust, PaperTheme.SansBold);

    /// <summary>The card's quiet row: where the act is filed, and its notes. Neither moves the act forward, so neither is
    /// a button face (playtest 049; signing audit 2026-11-25).</summary>
    private void ScoutingNotebookStrip(PlayerDesk desk, ScoutingDossier dossier, SimulatedArtist artist) {
        var row = new HFlowContainer();
        row.AddThemeConstantOverride("h_separation", 10);
        row.AddThemeConstantOverride("v_separation", 4);
        if (artist == null || string.IsNullOrEmpty(artist.labelId) || artist.labelId != desk.Label.labelId) {
            row.AddChild(ScoutingFilingStamp(desk, dossier));
            row.AddChild(ScoutingLinkDot());
        }
        var notes = ScoutingQuietLink("NOTES");
        notes.TooltipText = "Every set you've heard, the room, and where each piece of what you know came from.";
        notes.Pressed += () => OpenScoutingFullNotes(desk, dossier);
        row.AddChild(notes);
        content.AddChild(row);
    }

    private static string FiledAs(ScoutingPriority priority) => priority switch {
        ScoutingPriority.Watch => "WATCHING", ScoutingPriority.Pursue => "PURSUING", ScoutingPriority.Archived => "ARCHIVED", _ => "NOT FILED",
    };

    /// <summary>Watch, pursue and archive as one rubber stamp: the act's place in the notebook, changed from a small menu.</summary>
    private MenuButton ScoutingFilingStamp(PlayerDesk desk, ScoutingDossier dossier) {
        var stamp = new MenuButton { Text = $"FILED: {FiledAs(dossier.Priority)}  ▾", FocusMode = FocusModeEnum.None,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Where this act sits in your notebook. A rival that moves on an act you're watching has to come and compete for them." };
        stamp.SetMeta("scouting_filing", dossier.ArtistId);
        stamp.AddThemeFontOverride("font", PaperTheme.SansBold);
        stamp.AddThemeFontSizeOverride("font_size", 12);
        foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" }) stamp.AddThemeColorOverride(key, Rust);
        StyleBoxFlat Face(Color fill) => new() { BgColor = fill, BorderColor = Rust, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 2, ContentMarginBottom = 2 };
        stamp.AddThemeStyleboxOverride("normal", Face(new Color(0, 0, 0, 0)));
        stamp.AddThemeStyleboxOverride("hover", Face(new Color("f6ecd0")));
        stamp.AddThemeStyleboxOverride("pressed", Face(new Color("ead8ad")));
        stamp.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        var popup = stamp.GetPopup();
        var choices = new[] {
            (ScoutingPriority.Watch, "Watch  ·  keep them on your radar"),
            (ScoutingPriority.Pursue, "Pursue  ·  watch them, and keep them at the top"),
            (ScoutingPriority.Archived, "Archive  ·  file the notes away"),
        };
        foreach ((ScoutingPriority priority, string text) in choices) {
            popup.AddRadioCheckItem(text, (int)priority);
            popup.SetItemChecked(popup.GetItemIndex((int)priority), dossier.Priority == priority);
        }
        popup.IdPressed += id => RefreshAnchored(dossier.ArtistId, () => desk.SetScoutingPriority(dossier.ArtistId, (ScoutingPriority)(int)id));
        return stamp;
    }

    /// <summary>A secondary verb set as ink, not as a button face: no box, small type, rust on hover.</summary>
    private static Button ScoutingQuietLink(string text) {
        var link = new Button { Text = text, Flat = true, SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter,
            FocusMode = FocusModeEnum.None, MouseDefaultCursorShape = CursorShape.PointingHand };
        link.AddThemeFontSizeOverride("font_size", 13);
        foreach (string state in new[] { "normal", "hover", "pressed", "focus" })   // no box padding: the ink sits on the card's text edge
            link.AddThemeStyleboxOverride(state, new StyleBoxEmpty { ContentMarginTop = 3, ContentMarginBottom = 3 });
        link.AddThemeColorOverride("font_color", Heard);
        link.AddThemeColorOverride("font_hover_color", Rust);
        link.AddThemeColorOverride("font_pressed_color", Rust);
        link.AddThemeColorOverride("font_hover_pressed_color", Rust);
        return link;
    }
    private static Label ScoutingLinkDot() {
        var dot = new Label { Text = "·", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        dot.AddThemeColorOverride("font_color", Heard);
        return dot;
    }

    /// <summary>The button for a step before an agreed act signs.</summary>
    internal static string ApproachStepVerb(ScoutingApproachStep step) => step.Kind == ScoutingApproachStepKind.Parents
        ? "MEET THE FAMILY" : "SIT DOWN WITH THE MANAGER";

    /// <summary>Whether an act will hear an offer right now, so the card's primary goes straight to terms.</summary>
    private static bool ScoutingReadyForTerms(PlayerDesk desk, SimulatedArtist artist, PlayerDesk.Prospect prospect, bool waiting) =>
        !waiting && !desk.CanAskDirectApproach(artist) && desk.HasScoutingEvidence(artist.artistId) &&
        desk.CanTalkTerms(prospect ?? new PlayerDesk.Prospect { Artist = artist });

    /// <summary>Where the act stands with you: the last approach, a no that still holds, an answer pending, and whether
    /// the next step is the contract itself. Shared by the act card and The Set, so the two never disagree.</summary>
    private static (ScoutingDirectApproach Approach, bool Declined, bool Considering, bool Terms) ScoutingActStanding(PlayerDesk desk, SimulatedArtist artist) {
        var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == artist.artistId);
        var approach = desk.DirectApproaches.LastOrDefault(a => a.ArtistId == artist.artistId);
        int today = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        bool declined = desk.StillDeclining(artist);
        bool considering = approach?.Status == ScoutingApproachStatus.Considering;
        return (approach, declined, considering, ScoutingReadyForTerms(desk, artist, prospect, declined || considering));
    }

    /// <summary>The primary verb pressed: the contract when they'll hear an offer, otherwise the conversation.</summary>
    private void PressScoutingPrimary(PlayerDesk desk, ScoutingDossier dossier, bool terms) {
        if (desk.Slate.All(p => p.Artist?.artistId != dossier.ArtistId)) desk.OpenScoutingDossier(dossier.ArtistId, out _);
        var current = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
        if (terms && current != null) {
            bool ok = desk.ApproachToSign(current, out string result);
            if (ok) negotiating = current;
            Say(result, ok); Refresh(); return;
        }
        OpenTheTalk(desk, dossier);
    }

    /// <summary>The card's one primary verb, named for the next real step: TALK TO THEM (ask, arrange a meeting, get a
    /// number) or, once they'll hear an offer, SIT DOWN ON TERMS, which opens the contract.</summary>
    private void ScoutingOfferBar(PlayerDesk desk, ScoutingDossier dossier, SimulatedArtist artist) {
        var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
        var (approach, declined, considering, terms) = ScoutingActStanding(desk, artist);
        var bar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bar.AddThemeConstantOverride("separation", 14);
        // A yes with a step still to take: the family or the manager is the next verb. A holdout is settled at their set.
        var step = desk.NextApproachStep(artist);
        bool visit = step != null && step.Kind is ScoutingApproachStepKind.Parents or ScoutingApproachStepKind.Manager;
        var offer = Primary(visit ? ApproachStepVerb(step) : terms ? "SIT DOWN ON TERMS" : "TALK TO THEM");
        offer.CustomMinimumSize = new Vector2(240, 42);
        offer.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        offer.TooltipText = visit ? $"In their town, on or after {ShortDay(step.Day ?? 0)}. Takes {(step.Kind == ScoutingApproachStepKind.Parents ? PlayerDesk.FamilyVisitHours : PlayerDesk.ManagerSitDownHours)}h."
            : terms ? "Open the contract: their ask, your offer, and what it costs the label."
            : "Ask if they'd hear an offer, arrange a rehearsal or a demo, or get a number to call.";
        if (visit) offer.Pressed += () => { bool ok = desk.TakeApproachStep(artist.artistId, step.Kind, out string result); Say(result, ok); Refresh(); };
        else offer.Pressed += () => PressScoutingPrimary(desk, dossier, terms);
        bar.AddChild(offer);
        string hint;
        if (step != null) hint = $"They said yes. {PlayerDesk.StepAsk(step, artist)}";
        else if (desk.Agreement(artist) is { } yes && terms) hint = $"They said yes. Their yes holds until {ShortDay(PlayerDesk.AgreementHoldsUntil(yes))}.";
        else if (declined) hint = "They said no. " + ActDisposition.WhatWouldChange(desk.ReadDisposition(artist));
        else if (considering) hint = $"They're talking it over. Their answer comes by {ShortDay(approach.UntilDay)}.";
        else if (desk.WaitingToBeHeardAgain(artist)) {
            var again = desk.NextReachableScoutingSet(dossier.ArtistId);
            hint = "They want to see you again before they talk about a deal." + (again == null ? " Their next date isn't posted yet."
                : $" Next: {again.RoomName}, {ShortDay(again.Day)} at {Hour12(again.StartMinute / 60)}.");
        }
        else if (terms) hint = prospect != null ? $"They'll hear an offer · {PlayerDesk.SceneDealDescription(prospect)}." : "They'll hear an offer.";
        else if (desk.CanAskDirectApproach(artist)) hint = "Nobody's asked them about a record yet. Asking costs nothing.";
        else {
            var next = desk.NextReachableScoutingSet(dossier.ArtistId);
            hint = "Deals get talked over after a set you've heard that day." + (next == null ? " Their next date isn't posted yet."
                : $" Next: {next.RoomName}, {ShortDay(next.Day)} at {Hour12(next.StartMinute / 60)}.");
        }
        var note = new Label { Text = hint, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
        note.AddThemeFontSizeOverride("font_size", 14); note.AddThemeColorOverride("font_color", Heard);
        bar.AddChild(note);
        content.AddChild(bar);
    }

    /// <summary>One NOTES sheet for an act: every set heard, the room as you saw it, and where each piece came from. It
    /// replaces three read-only views (full notes, room impression, sources and coverage).</summary>
    private void OpenScoutingFullNotes(PlayerDesk desk, ScoutingDossier dossier) {
        var modal = PaperModal.Open(this, dossier.ArtistName + " · NOTES", 850);
        var scroll = ComparisonScroll(); var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; notes.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(notes); modal.Body.AddChild(scroll);
        WithinScouting(notes, () => {
            ScoutingSubhead("WHAT YOU'VE HEARD");
            var heard = desk.ScoutingPlayerNotes(dossier.ArtistId);
            foreach (string line in heard) ScoutingNote(line);
            // The notes already carry the audience line; the room section adds only what they don't say.
            var room = ScoutingAudience.RoomImpression(dossier).Where(line => !heard.Any(n => n.Contains(line))).ToList();
            if (room.Count > 0) {
                ScoutingSubhead("THE ROOM");
                foreach (string line in room) ScoutingText(line, 15, Ink);
            }
            var sources = desk.ScoutingProvenance(dossier.ArtistId).ToList();
            if (sources.Count > 0) {
                ScoutingSubhead("WHERE THIS CAME FROM");
                foreach (string line in sources) content.AddChild(FaintLine(line));
            }
        });
        modal.AddButton("CLOSE", null);
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
                hear.TooltipText = "Follow this announced appearance. While you wait the month's bills keep running and other labels keep making their moves. Waiting doesn't hear the set, charge admission or reserve the act. Availability is checked again when you act.";
                ScoutingTimeRisk(desk, hear, next.Day, next.Playing ? next.EndMinute : next.StartMinute);
                GreyWhy(hear, "Not enough cash for the admission.");
                hear.Pressed += () => Act(() => {
                    string result;
                    bool ok = next.Playing ? desk.ListenSceneAppearance(next.BillId, next.EventId, out result) :
                        desk.WaitForSceneAppearance(next.BillId, next.EventId, out result);
                    if (ok && next.Playing) OpenTheSet(desk);
                    Say(result, ok); return ok;
                }); actions.AddChild(hear);
            }
            content.AddChild(actions);
        }
    }
    private void ScoutingBusinessConversation(PlayerDesk desk, ScoutingDossier dossier, Action beforeAction = null) {
        var meeting = desk.PreviewScoutingBusinessMeeting(dossier.ArtistId);
        if (meeting == null) return;
        Body($"{meeting.SourceName} will talk after the set. Ask how they've dealt with labels before.");
        var ask = ScoutingSmallButton("PAST LABEL DECISIONS");
        ask.Disabled = !meeting.Available;
        ask.TooltipText = $"Ask about past label decisions. Their account may be incomplete and does not establish a current offer or deadline. {PlayerDesk.ActHistoryMinutes}min.";
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
        Body($"Ask a booker about {dossier.ArtistName} · {PlayerDesk.BookerQuestionMinutes}min · no fee, at the booker's venue. This adds a source's account, not another listening.");
        var choices = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var roomChoice = new OptionButton();
        foreach (var room in rooms) roomChoice.AddItem(room.Name + (desk.KnowsSceneBooker(room.Id) ? " — " + room.ContactName : " — booker not met"));
        var questionChoice = new OptionButton();
        foreach (var value in Enum.GetValues<ScoutingSourceQuestion>()) questionChoice.AddItem(PlayerDesk.ScoutingSourceQuestionText(value), (int)value);
        questionChoice.TooltipText = "Choose a source question; selecting or refreshing spends no time and creates no evidence.";
        var ask = Btn("ASK ABOUT " + dossier.ArtistName.ToUpperInvariant() + $" ({PlayerDesk.BookerQuestionMinutes}min · no fee)");
        var availability = FaintLine("");
        var introduce = ScoutingSmallButton("INTRODUCE YOURSELF · 15min");
        introduce.Pressed += () => Act(() => { bool ok = desk.IntroduceSceneBooker(rooms[roomChoice.Selected].Id, out string result); Say(result, ok); return ok; });
        var goThere = ScoutingSmallButton($"GO TO THE VENUE · {PlayerDesk.CrossTownMinutes}min");
        goThere.Pressed += () => Act(() => { bool ok = desk.GoToSceneRoom(rooms[roomChoice.Selected].Id, out string result); Say(result, ok); return ok; });
        // Playtest 2026-11-05: a once-a-week venue was the only way to reach an act. A booker who has had them in may give a number.
        var number = ScoutingSmallButton($"ASK FOR THEIR NUMBER · {PlayerDesk.AskBookerNumberMinutes}min");
        number.Pressed += () => Act(() => { bool ok = desk.AskBookerForNumber(rooms[roomChoice.Selected].Id, dossier.ArtistId, out string result); Say(result, ok); return ok; });
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
            var selectedRoom = rooms[roomChoice.Selected];
            goThere.Visible = !desk.IsAtSceneRoom(selectedRoom.Id);
            goThere.Disabled = !desk.CanGoToSceneRoom(selectedRoom.Id, out string goWhy);
            goThere.TooltipText = goThere.Disabled ? goWhy : "Walk across town to " + selectedRoom.Name + ". Bookers talk in their own venue.";
            string numberWhy = desk.BookerNumberBlocked(selectedRoom.Id, dossier.ArtistId);
            number.Visible = desk.ActNumber(dossier.ArtistId) == null;
            number.Disabled = numberWhy != null;
            number.TooltipText = numberWhy ?? "A booker who has had them play may pass on a number. The better they know you, the likelier; a no stands for the season.";
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
        UpdatePreview(); choices.AddChild(roomChoice); choices.AddChild(goThere); choices.AddChild(introduce); choices.AddChild(questionChoice); choices.AddChild(availability); choices.AddChild(ask); choices.AddChild(number);
        content.AddChild(choices);
    }

}
