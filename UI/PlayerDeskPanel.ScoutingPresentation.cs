using System;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private static readonly Color ScoutingArtistInk = new("294b66");
    private static readonly Color ScoutingVenueInk = new("713d27");
    private Label ScoutingText(string text, int size, Color color, Font font = null) {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontOverride("font", font ?? PaperTheme.Sans);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        content.AddChild(label); return label;
    }
    private Button ScoutingSmallButton(string text) {
        var button = Btn(text);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        button.CustomMinimumSize = new Vector2(100, 32);
        return button;
    }
    private void WithinScouting(VBoxContainer target, Action build) {
        var parent = content; content = target;
        try { build(); } finally { content = parent; }
    }
    private VBoxContainer ScoutingPaperCard(Color accent) {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = new Color("f3e5c4"), BorderColor = accent, BorderWidthLeft = 4,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10
        });
        var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 5); panel.AddChild(body); content.AddChild(panel); return body;
    }
    private void ScoutingListeningSummary(PlayerDesk desk, ScoutingDossier dossier) {
        var samples = ScoutingComparison.Performances(dossier);
        var last = samples.LastOrDefault();
        if (last == null) { ScoutingText("No dated listening notes yet.", 14, Heard); return; }
        int dates = samples.Select(o => o.OccurredDay).Distinct().Count();
        ScoutingText($"Your impression · {samples.Length} {(samples.Length == 1 ? "set" : "sets")} · {dates} {(dates == 1 ? "date" : "dates")}", 14, Heard);
        if (last.Coverage < .9) ScoutingText("Partial listen · individual songs heard are uncertain", 14, ScoutingVenueInk);
        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 24); grid.AddThemeConstantOverride("v_separation", 8);
        foreach (var axis in desk.ScoutingListeningAxes(dossier.ArtistId)) {
            var label = new Label { Text = axis.Axis }; label.AddThemeFontOverride("font", PaperTheme.SansBold); label.AddThemeFontSizeOverride("font_size", 15); label.AddThemeColorOverride("font_color", ScoutingArtistInk);
            var read = new Label { Text = char.ToUpperInvariant(axis.Read[0]) + axis.Read[1..] + (axis.Limitation != null ? " · room limits detail" : ""),
                TooltipText = axis.Limitation ?? "Your impression of this set, not a prediction of future talent.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
            read.AddThemeFontSizeOverride("font_size", 15); read.AddThemeColorOverride("font_color", Ink);
            grid.AddChild(label); grid.AddChild(read);
        }
        content.AddChild(grid);
        if (last.Interpretation is { } interpretation) {
            var material = ScoutingText(interpretation.OriginalMaterial && last.Coverage >= .9 ? "An unrecorded original was heard" : "Original writing not established by this listen", 13, Heard);
            material.TooltipText = "Familiar material is not evidence of original writing. Partial coverage may leave announced songs unheard.";
        }
        var currentPeople = ArtistManager.Instance.GetArtist(dossier.ArtistId)?.members.Where(m => m.isActive && m.lifeState == MemberLifeState.Active).Select(m => m.personId).OrderBy(id => id, StringComparer.Ordinal);
        if (currentPeople == null || !currentPeople.SequenceEqual(last.PersonIds.OrderBy(id => id, StringComparer.Ordinal)))
            ScoutingText("Lineup changed · these notes describe the earlier players", 14, ScoutingVenueInk);
        if (dates == 1) ScoutingText("Hear another date to judge consistency", 13, Heard);
        else ScoutingText(desk.ScoutingPlayerNotes(dossier.ArtistId).First(l => l.StartsWith("Consistency:")), 14, Ink);
        int studioSamples = dossier.Observations.Count(o => o.Kind is ScoutingEvidenceKind.RehearsalSample or ScoutingEvidenceKind.DemoSample);
        if (studioSamples > 0) ScoutingText($"{studioSamples} rehearsal / demo {(studioSamples == 1 ? "sample" : "samples")} recorded · see full notes", 13, ScoutingArtistInk);
        if (dossier.Question != ScoutingQuestion.None) ScoutingText(PlayerDesk.ScoutingPlayerQuestionRead(dossier), 14, ScoutingArtistInk);
        if (PlayerDesk.ScoutingRoomGap(dossier, samples) is { } gap) {
            var hint = ScoutingText("Try a different room for a clearer read", 13, ScoutingVenueInk); hint.TooltipText = gap;
        }
        var details = ScoutingSmallButton("FULL NOTES & HISTORY");
        details.TooltipText = "Sources, audience, recording evidence, past conversations and competition reports. Missing evidence does not imply availability.";
        details.Pressed += () => {
            var modal = PaperModal.Open(this, dossier.ArtistName + " · FULL NOTES", 850);
            var scroll = ComparisonScroll(); var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; notes.AddThemeConstantOverride("separation", 10);
            scroll.AddChild(notes); modal.Body.AddChild(scroll);
            WithinScouting(notes, () => { foreach (string line in desk.ScoutingPlayerNotes(dossier.ArtistId)) ScoutingNote(line); });
            modal.AddButton("CLOSE", null);
        }; content.AddChild(details);
    }
    private void ScoutingNote(string text) {
        int colon = text.IndexOf(':');
        if (colon < 0 || colon > 32) { ScoutingText(text, 15, Ink); return; }
        string Escape(string value) => value.Replace("[", "[lb]");
        var note = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, Text = "[b]" + Escape(text[..(colon + 1)]) + "[/b]" + Escape(text[(colon + 1)..]) };
        note.AddThemeFontOverride("normal_font", PaperTheme.Sans); note.AddThemeFontOverride("bold_font", PaperTheme.SansBold);
        note.AddThemeFontSizeOverride("normal_font_size", 15); note.AddThemeColorOverride("default_color", Ink);
        content.AddChild(note);
    }
    private void ScoutingSetPreview(PlayerDesk desk, ScoutingOpportunity option, bool circuit = false, Action afterAction = null, bool linkVenue = true) {
        var card = ScoutingPaperCard(option.Playing ? new Color("42624a") : ScoutingArtistInk);
        WithinScouting(card, () => {
            ScoutingText(option.ArtistName ?? "Unannounced act", 22, ScoutingArtistInk, PaperTheme.Serif);
            var room = ScoutingText(option.RoomName + (circuit ? " · " + DistanceModel.GetCityById(option.CityId)?.name : ""), 14, ScoutingVenueInk, PaperTheme.SansBold);
            if (linkVenue) {
                room.MouseFilter = MouseFilterEnum.Stop; room.MouseDefaultCursorShape = CursorShape.PointingHand;
                room.TooltipText = "Open venue information and calendar";
                room.GuiInput += input => { if (input is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left) OpenScoutingVenue(desk, option.RoomId); };
            }
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12);
            var details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            WithinScouting(details, () => {
                string date = LocalSceneRoomService.Date(option.Day).ToHeadlineString();
                ScoutingText($"{date} · {Hour12(option.StartMinute / 60)}–{Hour12(option.EndMinute / 60)}" + (option.Playing ? " · Playing now" : ""), 16, Ink, PaperTheme.SansSemiBold);
                ScoutingText($"{option.ListeningMinutes}min listening · {Money(option.Admission)} admission" + (circuit ? $" · {option.TravelMinutes / 60}h drive" : ""), 14, Heard);
            });
            row.AddChild(details); content.AddChild(row);
            var action = ScoutingSmallButton(circuit ? "DRIVE" : option.Playing ? "LISTEN" : "WAIT");
            action.Disabled = !circuit && (option.Playing && desk.Label.cashReserves < option.Admission ||
                SceneRoomCatalog.Get(option.RoomId)?.PlaceId != LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId));
            float immediateCost = circuit ? option.TravelCost : option.Playing ? option.Admission : 0;
            string costs = circuit ? $"One-way gas: {Money(option.TravelCost)}; return trip separate. Cash after driving at today's quote: {Money(desk.Label.cashReserves - immediateCost)}."
                : option.Playing ? $"Cash after admission at today's quote: {Money(desk.Label.cashReserves - option.Admission)}."
                : $"Waiting charges no admission. Cash after admission at today's quote: {Money(desk.Label.cashReserves - option.Admission)}. Future expenses and income may change this amount.";
            action.TooltipText = $"{option.WaitingMinutes}min wait on the set day. {costs}\nWaiting advances ordinary expenses and other labels' decisions. Check the bill and price on arrival.";
            card.TooltipText = action.TooltipText;
            var clock = TimeManager.Instance;
            ScoutingTimeRisk(desk, action, circuit ? LocalSceneRoomService.Day(clock.CurrentDate) : option.Day,
                circuit ? clock.CurrentHour * 60 + clock.CurrentMinute + option.TravelMinutes : option.Playing ? option.EndMinute : option.StartMinute);
            action.Pressed += () => Act(() => {
                string result;
                bool ok = circuit ? desk.DriveTo(option.CityId, out result) : option.Playing
                    ? desk.ListenSceneAppearance(option.BillId, option.EventId, out result)
                    : desk.WaitForSceneAppearance(option.BillId, option.EventId, out result);
                if (ok && !circuit && option.Playing) ShowLatestScoutingAct(desk);
                Say(result, ok); if (ok) afterAction?.Invoke(); return ok;
            });
            row.AddChild(action);
        });
    }

    private void OpenScoutingVenue(PlayerDesk desk, string roomId) {
        var room = SceneRoomCatalog.Get(roomId); if (room == null) return;
        var modal = PaperModal.Open(this, room.Name, 960);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 660), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var page = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; page.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(page); modal.Body.AddChild(scroll);
        int selectedDay = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
        int calendarOffsetWeeks = 0;
        void Fill() {
            foreach (Node child in page.GetChildren()) { page.RemoveChild(child); child.QueueFree(); }
            WithinScouting(page, () => {
                var clock = TimeManager.Instance; var date = clock.CurrentDate;
                ScoutingText($"{SceneRoomCharacter.KindName(room.Kind)} · about {SceneLiveEconomics.Capacity(room)} people · {Hour12(room.OpenHour)}–{Hour12(room.CloseHour)}", 14, Heard);
                var info = new HBoxContainer(); info.AddThemeConstantOverride("separation", 24);
                var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1 };
                var sound = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1 };
                WithinScouting(identity, () => {
                    ScoutingText("THE ROOM", 14, ScoutingVenueInk, PaperTheme.SansBold);
                    ScoutingText(room.Programming(date.year), 16, Ink, PaperTheme.Serif);
                    ScoutingText(SceneRoomCharacter.Crowd(room.Kind), 14, Heard);
                });
                WithinScouting(sound, () => {
                    ScoutingText("LISTENING CONDITIONS", 14, ScoutingVenueInk, PaperTheme.SansBold);
                    ScoutingText(SceneRoomCharacter.HearingText(room.Kind), 14, Ink);
                    ScoutingText(SceneRoomCharacter.Talk(room.Kind), 13, Heard);
                });
                info.AddChild(identity); info.AddChild(sound); content.AddChild(info);
                var contact = new HBoxContainer(); contact.AddThemeConstantOverride("separation", 12);
                var contactInfo = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                bool known = desk.KnowsSceneBooker(room.Id);
                WithinScouting(contactInfo, () => {
                    ScoutingText(known ? room.ContactName + " · Booker" : "Booker · You haven't met yet", 16, ScoutingVenueInk, PaperTheme.SansBold);
                    ScoutingText(known ? "In your Rolodex · knows this venue's acts and dates" : "Introduce yourself at the venue during posted hours.", 13, Heard);
                });
                contact.AddChild(contactInfo);
                var meet = ScoutingSmallButton(known ? "ASK FOR A TIP" : "INTRODUCE YOURSELF");
                if (known) {
                    meet.Disabled = room.PlaceId != LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId) ||
                        clock.CurrentHour * 60 + clock.CurrentMinute < room.OpenHour * 60 ||
                        clock.CurrentHour * 60 + clock.CurrentMinute + 60 > room.CloseHour * 60 || !clock.CanAffordMinutes(60);
                    meet.TooltipText = "1h · no fee. Ask for an announced set worth hearing; this creates no listening evidence.";
                    meet.Pressed += () => Act(() => { bool ok = desk.AskSceneBooker(room.Id, out string result); Say(result, ok); Fill(); return ok; });
                } else {
                    meet.Disabled = !desk.CanIntroduceSceneBooker(room.Id, out string reason);
                    meet.TooltipText = meet.Disabled ? reason : "15min · no fee. Meet the venue's booker and add them to your Rolodex.";
                    meet.Pressed += () => Act(() => { bool ok = desk.IntroduceSceneBooker(room.Id, out string result); Say(result, ok); Fill(); return ok; });
                }
                contact.AddChild(meet); content.AddChild(contact);
                ScoutingVenueCalendar(desk, room, selectedDay, day => { selectedDay = day; Fill(); }, () => {
                    if (scoutingView == "ACT NOTEBOOK") modal.Close(); else Fill();
                }, calendarOffsetWeeks, step => { calendarOffsetWeeks += step; selectedDay = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate) + calendarOffsetWeeks * 7; Fill(); });
                if (LocalScenes.Institutions && room.IsPerformance) {
                    var toggle = ScoutingSmallButton("SUPPORT THIS VENUE"); content.AddChild(toggle);
                    var support = new VBoxContainer { Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddChild(support);
                    toggle.Pressed += () => support.Visible = !support.Visible;
                    WithinScouting(support, () => {
                        var program = SceneEcosystemService.Programs.FirstOrDefault(p => p.RoomId == room.Id && p.ThroughDay >= LocalSceneRoomService.Day(date));
                        if (program != null) Body($"Funded program: {LocalSceneRoomService.Date(program.StartDay).ToHeadlineString()}–{LocalSceneRoomService.Date(program.ThroughDay).ToHeadlineString()}.");
                        else {
                            var fund = ScoutingSmallButton($"FUND 13-WEEK PROGRAM · {Money(SceneEcosystemService.ProgramCost)}");
                            fund.TooltipText = "One extra set per performance night, starting in two weeks. Paid upfront; your operating reserve must remain.";
                            fund.Pressed += () => Act(() => { bool ok = desk.FundSceneProgram(room.Id, out string result); Say(result, ok); Fill(); return ok; }); content.AddChild(fund);
                        }
                    });
                }
            });
        }
        Fill(); modal.AddButton("CLOSE", null);
    }

    private void ScoutingVenueCalendar(PlayerDesk desk, SceneRoomProfile room, int selectedDay, Action<int> select, Action afterAction, int offsetWeeks, Action<int> shift) {
        var clock = TimeManager.Instance; int today = LocalSceneRoomService.Day(clock.CurrentDate);
        var bills = LocalSceneRoomService.AnnouncedCalendar(room.PlaceId, clock.CurrentDate).Where(b => b.RoomId == room.Id).ToArray();
        // Two weeks of real posted dates; no suggested or generated performances.
        DateTime start = new(clock.CurrentDate.year, clock.CurrentDate.month, clock.CurrentDate.day);
        start = start.AddDays(-(int)start.DayOfWeek + offsetWeeks * 7);
        Heading("POSTED CALENDAR");
        var nav = new HBoxContainer(); nav.AddThemeConstantOverride("separation", 8);
        var range = new Label { Text = start.ToString("MMM d") + " – " + start.AddDays(20).ToString("MMM d, yyyy"), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        range.AddThemeFontOverride("font", PaperTheme.SansSemiBold); range.AddThemeColorOverride("font_color", ScoutingVenueInk);
        nav.AddChild(range);
        var previous = Btn("‹"); previous.CustomMinimumSize = new Vector2(34, 28); previous.Disabled = offsetWeeks == 0; previous.TooltipText = "Previous week";
        previous.Pressed += () => shift(-1); nav.AddChild(previous);
        var next = Btn("›"); next.CustomMinimumSize = new Vector2(34, 28); next.TooltipText = "Next week of posted dates";
        int lastVisibleDay = LocalSceneRoomService.Day(new GameDate(start.Year, start.Month, start.Day)) + 20;
        next.Disabled = !bills.Any(b => b.Day > lastVisibleDay);
        next.Pressed += () => shift(1); nav.AddChild(next); content.AddChild(nav);
        ScoutingText("Green: playing now  ·  Blue: upcoming  ·  Gray: past / no bill  ·  Red: cancelled", 13, Heard);
        var grid = new GridContainer { Columns = 7, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 6); grid.AddThemeConstantOverride("v_separation", 6);
        foreach (string day in new[] { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" }) {
            var label = new Label { Text = day, HorizontalAlignment = HorizontalAlignment.Center }; label.AddThemeColorOverride("font_color", Heard); grid.AddChild(label);
        }
        for (int i = 0; i < 21; i++) {
            var actual = start.AddDays(i); var gameDate = new GameDate(actual.Year, actual.Month, actual.Day);
            int day = LocalSceneRoomService.Day(gameDate);
            var posted = bills.Where(b => b.Day == day).ToArray();
            bool future = posted.Any(b => b.Status == SceneBillStatus.Scheduled && (day > today || day == today && b.EndHour * 60 > clock.CurrentHour * 60 + clock.CurrentMinute));
            bool playing = day == today && posted.Any(b => b.Status == SceneBillStatus.Scheduled && b.StartHour <= clock.CurrentHour && b.EndHour > clock.CurrentHour);
            bool cancelled = posted.Length > 0 && posted.All(b => b.Status == SceneBillStatus.Cancelled);
            Color ink = cancelled ? new Color("8b322d") : playing ? new Color("315e3c") : future ? ScoutingArtistInk : Heard;
            var date = Btn(actual.ToString("MMM d") + (playing ? "\nNOW" : cancelled ? "\nCANCELLED" : future ? "\nBILL" : "\n—"));
            date.SetMeta("scouting_day", day);
            date.SizeFlagsHorizontal = SizeFlags.ExpandFill; date.CustomMinimumSize = new Vector2(0, 54);
            date.AddThemeFontSizeOverride("font_size", 13);
            foreach (string key in new[] { "font_color", "font_hover_color", "font_pressed_color" }) date.AddThemeColorOverride(key, ink);
            date.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = day == selectedDay ? new Color("efe1b2") : future ? new Color("dde7e8") : cancelled ? new Color("ecd3ca") : new Color("e6dfcd"),
                BorderColor = day == selectedDay ? ink : new Color("bbac8b"), BorderWidthLeft = day == selectedDay ? 3 : 1,
                BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthRight = 1, ContentMarginTop = 4, ContentMarginBottom = 4 });
            date.TooltipText = posted.Length == 0 ? "No posted bill" : string.Join("\n", posted.SelectMany(b => b.Appearances.Select(a => $"{Hour12(a.StartHour)} · {ArtistManager.Instance.GetArtist(a.ArtistId)?.stageName ?? "Unavailable act"}")));
            date.Pressed += () => select(day); grid.AddChild(date);
        }
        content.AddChild(grid);
        ScoutingText(LocalSceneRoomService.Date(selectedDay).ToHeadlineString(), 18, ScoutingVenueInk, PaperTheme.Serif);
        var selectedBills = bills.Where(b => b.Day == selectedDay).ToArray();
        if (selectedBills.Length == 0) { ScoutingText("No bill posted for this date.", 14, Heard); return; }
        foreach (var bill in selectedBills) foreach (var slot in bill.Appearances) {
            string eventId = ScenePerformanceService.EventId(bill, slot);
            int now = clock.CurrentHour * 60 + clock.CurrentMinute;
            bool playing = bill.Day == today && slot.StartHour * 60 <= now && slot.EndHour * 60 > now;
            bool future = bill.Day > today || bill.Day == today && slot.StartHour * 60 > now;
            bool scheduled = bill.Status == SceneBillStatus.Scheduled && slot.Status == SceneBillStatus.Scheduled;
            if (!scheduled || !future && !playing || slot.EndHour > clock.HardStopHour || desk.ScoutingDossiers.Any(d => d.Observations.Any(o => o.EventId == eventId && o.Kind == ScoutingEvidenceKind.Performance))) {
                bool cancelled = bill.Status == SceneBillStatus.Cancelled || slot.Status == SceneBillStatus.Cancelled;
                ScoutingText($"{Hour12(slot.StartHour)} · {ArtistManager.Instance.GetArtist(slot.ArtistId)?.stageName ?? "Unavailable act"} · " + (cancelled ? "Cancelled" : slot.EndHour > clock.HardStopHour ? "Outside working hours" : "Heard / past"), 14, Heard); continue;
            }
            float admission = bill.Day == today ? desk.AdmissionForVisit(bill, room, now) : SceneLiveEconomics.Admission(room, bill.Year);
            var option = new ScoutingOpportunity { BillId = bill.Id, EventId = eventId, ArtistId = slot.ArtistId, ArtistName = ArtistManager.Instance.GetArtist(slot.ArtistId)?.stageName,
                RoomId = room.Id, RoomName = room.Name, CityId = room.PlaceId, Day = bill.Day, StartMinute = slot.StartHour * 60, EndMinute = slot.EndHour * 60,
                Playing = playing, WaitingMinutes = Math.Max(0, slot.StartHour * 60 - (bill.Day == today ? now : clock.WorkdayStartHour * 60)),
                ListeningMinutes = slot.EndHour * 60 - (playing ? now : slot.StartHour * 60), Admission = admission };
            ScoutingSetPreview(desk, option, afterAction: afterAction, linkVenue: false);
            if (playing) ScoutingListenerControls(desk, bill, eventId);
        }
    }
}
