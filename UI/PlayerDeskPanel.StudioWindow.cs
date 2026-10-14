using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The studio, set as the room's own paperwork rather than an office page: a booking sheet before the date, then the
/// session sheet on the console -- the room's clock, the song on the floor, the take sheet and the finish. Look and
/// layout only; every action calls the same PlayerDesk owner it always has.
/// </summary>
public partial class PlayerDeskPanel {
    private bool studioCompare, studioReview, studioBacking;
    private readonly HashSet<string> studioSongs = new(), studioOpenTakes = new();
    private int studioRoomIndex, studioHours = 3, studioBrief;
    private int studioLayout, studioDays = 1;
    private bool studioEvening, studioLate, studioProducer;
    private string studioRoomId;
    private readonly Dictionary<string, string> studioEngineerAsk = new();   // room id -> engineer asked for by name
    private static string StudioSongKey(PlayerDesk.MaterialChoice choice) => $"{choice.Kind}|{choice.SongId}|{choice.Title}";

    private static readonly PlayerDesk.StudioTier[] Tiers =
        { PlayerDesk.StudioTier.Budget, PlayerDesk.StudioTier.Mid, PlayerDesk.StudioTier.Top };

    private static readonly string[] BriefNames = { "Capture the live act", "Tighten it for a single", "Leave room to explore" };
    private static readonly string[] BriefNotes = {
        "Get them down the way they play it on stage. If you hand the date over, the producer keeps the work simple.",
        "Aim for a tight radio record. Handed over, the producer rehearses where the time allows.",
        "Keep time free to try something different. Handed over, the producer may test another arrangement."
    };

    private Button StudioHelp(string title, string explanation, Container host = null) {
        var help = Btn("? " + title);
        help.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        help.Pressed += () => {
            var modal = PaperModal.Open(this, title.ToUpperInvariant(), 760);
            modal.AddText(explanation); modal.AddButton("GOT IT", null);
        };
        (host ?? content).AddChild(help);
        return help;
    }

    private static Label StudioText(Container host, string text, int size = 16, Color? color = null, Font font = null) {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Ink);
        if (font != null) label.AddThemeFontOverride("font", font);
        host.AddChild(label);
        return label;
    }

    private static HBoxContainer StudioRow(Container host, int separation = 10) {
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", separation);
        host.AddChild(row);
        return row;
    }

    private static HFlowContainer StudioFlow(Container host, int separation = 8) {
        var flow = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        flow.AddThemeConstantOverride("h_separation", separation);
        flow.AddThemeConstantOverride("v_separation", separation);
        host.AddChild(flow);
        return flow;
    }

    private static Control StudioSpacer() => new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };

    /// <summary>A highlighted note on the floor (an idea, a suggestion, a shortage) with a heading. Returns its body.</summary>
    private static VBoxContainer StudioNotice(Container host, string heading) {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(PaperTheme.Highlighter, 0.30f), BorderColor = Rust,
            BorderWidthLeft = 4, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
        var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 6); panel.AddChild(box);
        StudioText(box, heading, 13, Rust, PaperTheme.SansBold);
        host.AddChild(panel);
        return box;
    }

    /// <summary>A sheet of the studio's own paperwork, headed with the room's name.</summary>
    private WorkOrder.Form StudioSheet(string room, string kind, string key, string stamp = null, Color? stampInk = null) {
        var form = WorkOrder.Begin(room, PaperTheme.Lettering(LetteringStyle.HeavySlab), 19, kind, WorkOrder.Serial(key), stamp, stampInk, copyTag: "Studio");
        content.AddChild(form.Sheet);
        return form;
    }

    private void RenderStudioWindow(PlayerDesk desk) {
        var artist = desk.Roster.FirstOrDefault(a => a.artistId == studioArtistId);
        if (artist == null) { Body("This act is no longer on the roster. Your retained tapes are unchanged."); return; }
        titleLabel.Text = "STUDIO  •  " + artist.stageName;
        var session = desk.Session;

        var top = StudioRow(content);
        var back = Btn(session == null ? "‹ BACK TO ROSTER" : "‹ BACK TO THE OFFICE");
        back.TooltipText = session == null ? "" : "The date stays open. Closing the studio pauses your decisions; it does not end or refund the paid date.";
        back.Pressed += ClosePanel; top.AddChild(back);
        bool ownDate = session != null && session.ArtistId == artist.artistId;
        int stage = !ownDate ? 0 : session.Cuts.All(c => c.KeptTake >= 0) ? 2 : 1;
        string Step(int index, string name) => (index < stage ? "✓ " : index == stage ? "● " : "") + name;
        var trail = new Label {
            Text = $"{Step(0, "BOOK")}    ▸    {Step(1, "RECORD & KEEP")}    ▸    {Step(2, "FINISH & PRINT")}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        trail.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
        trail.AddThemeFontSizeOverride("font_size", 13);
        trail.AddThemeColorOverride("font_color", WorkOrder.FormInk);
        top.AddChild(trail);
        StudioHelp("How a studio date works", "Choose the songs, a local room and a time limit. Two songs make a two-sided single. The quote includes setup, tape stock, basic playback and house finishing; booked musicians are separate. Booking begins a paid date, with no refund for incurred work.\n\nCut a pass, read the attributed listening notes and keep a performance. Another attempt never destroys an earlier take. Rehearsal, direction and alternatives use room time; a break partially recovers fatigue across songs. The producer can run the remaining work within the quote, preserving your kept choices.\n\nOnce every included song has a kept take, use the included house finish or supported whole-tape processing. A mono tape cannot turn the drums or vocal down independently. Finish and print to send the masters to Catalog for A/B selection, pressing and release. Closing this window pauses your decisions without ending or refunding a paid date.", top);

        if (session != null && !ownDate) {
            SimulatedArtist other = ArtistManager.Instance?.GetArtist(session.ArtistId);
            Body($"{other?.stageName ?? "Another act"} has a date on the console. Finish or abandon it before booking this act.");
            var resume = Primary("RETURN TO THE OPEN DATE");
            resume.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            resume.Pressed += () => { studioArtistId = session.ArtistId; studioSide = 0; Refresh(); };
            content.AddChild(resume); return;
        }
        if (ownDate) TakesView(session, artist); else BookingSheet(desk, artist);
    }

    // ========================================================================
    // BOOKING SHEET
    // ========================================================================

    private void BookingSheet(PlayerDesk desk, SimulatedArtist artist) {
        bool incremental = StudioExperience.Enabled;
        // Incremental dates book named rooms: the town's own, its specialist room, and rooms a drive away.
        List<StudioRoomOffer> offers = incremental ? desk.StudioRoomIds().Select(id => desk.StudioOffer(id, artist.artistId, studioEngineerAsk.GetValueOrDefault(id)))
            .Where(o => o != null).ToList() : null;
        if (incremental && !offers.Any(o => o.Room.Id == studioRoomId)) studioRoomId = offers[0].Room.Id;
        StudioRoomOffer offer = incremental ? offers.First(o => o.Room.Id == studioRoomId) : null;
        studioRoomIndex = Mathf.Clamp(studioRoomIndex, 0, Tiers.Length - 1);
        PlayerDesk.StudioTier tier = incremental ? offer.Tier : Tiers[studioRoomIndex];
        string roomName = incremental ? offer.Room.Name : PlayerDesk.StudioTierName(tier);
        var time = TimeManager.Instance;
        int now = (time?.CurrentHour ?? 9) * 60 + (time?.CurrentMinute ?? 0);

        var form = StudioSheet(roomName, "Session booking", "studio-booking:" + artist.artistId);
        var sheet = form.Body;
        var facts = StudioFlow(sheet);
        facts.AddChild(WorkOrder.Typed("Artist", artist.stageName, 240));
        facts.AddChild(WorkOrder.Typed("Date", time?.CurrentDate.ToHeadlineString() ?? "", 150));
        facts.AddChild(WorkOrder.Typed("Cash on hand", Money(desk.Label.cashReserves), 130));

        // 1. Songs
        sheet.AddChild(WorkOrder.Section("1   Songs to cut"));
        StudioText(sheet, "Tick two for a single. You pair the sides at the release desk afterwards.", 14, Heard);
        List<PlayerDesk.MaterialChoice> options = desk.MaterialOptionsFor(artist).ToList();
        if (options.Count <= 1) StudioText(sheet, "Need more material? Return to Manage on the roster to write a song or learn a cover.", 14, Rust);
        var checks = new List<(CheckBox Box, PlayerDesk.MaterialChoice Choice)>();
        var shapes = new Dictionary<PlayerDesk.MaterialChoice, StudioSongStructure>();
        foreach (PlayerDesk.MaterialChoice option in options) {
            string key = StudioSongKey(option);
            if (incremental) shapes[option] = StudioSongStructures.For(option, artist, TimeManager.Instance?.CurrentDate.year ?? 1960);
            string shape = incremental ? $"   ·   {StudioSongStructures.Length(shapes[option].DurationSeconds)}, {StudioSongStructures.FormName(shapes[option].Form)}" : "";
            var box = Check(option.Describe() + shape, studioSongs.Contains(key));
            box.Toggled += selected => { if (selected) studioSongs.Add(key); else studioSongs.Remove(key); };
            sheet.AddChild(box);
            checks.Add((box, option));
        }
        var compare = Btn(studioCompare ? "HIDE MATERIAL COMPARISON" : "COMPARE MATERIAL…");
        compare.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        compare.TooltipText = "Hear how the songs suit the act. Comparing spends no time or money.";
        compare.Pressed += () => { studioCompare = !studioCompare; RefreshKeepingScroll(); };
        sheet.AddChild(compare);
        if (studioCompare && PolarSongBehavior.UsePolarFitSelection && options.Count > 0) MaterialComparison(desk, artist, options, checks, tier, sheet);

        // 2. Room
        sheet.AddChild(WorkOrder.Section("2   The room"));
        if (!incremental) {
            for (int i = 0; i < Tiers.Length; i++) {
                var card = Btn($"{PlayerDesk.StudioTierName(Tiers[i])}   —   ${desk.StudioHourlyRate(Tiers[i]):N0} an hour");
                card.Alignment = HorizontalAlignment.Left; card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                StyleToggle(card, i == studioRoomIndex);
                int index = i;
                card.Pressed += () => { if (studioRoomIndex == index) return; studioRoomIndex = index; RefreshKeepingScroll(); };
                sheet.AddChild(card);
            }
        } else {
            var local = offers.Where(o => o.Local).ToList(); var away = offers.Where(o => !o.Local && !o.Overnight).ToList();
            var overnight = offers.Where(o => o.Overnight).ToList();
            bool onTrip = desk.ActiveStudioTrip is { } trip && trip.CityId == desk.CurrentCityId;
            StudioRoomGroup(sheet, onTrip ? "In " + desk.CurrentCity?.name : "In town", onTrip ? "You drove up with the band. Any room in town will do."
                : local.Count == 1 ? "The only recording service in town." : "Click a room for its sound, engineer and finishing.");
            foreach (var each in local) StudioRoomCard(desk, artist, each, each == offer, sheet);
            if (away.Count > 0) {
                StudioRoomGroup(sheet, "A drive away", "The band rides with you. The drive comes out of the day, and you'll be in that town when the date ends.");
                foreach (var each in away) StudioRoomCard(desk, artist, each, each == offer, sheet);
            }
            if (overnight.Count > 0) {
                StudioRoomGroup(sheet, "Drive up the night before", "Too far to drive and record the same day. Drive the band up today, stay at a motel, and book the room there tomorrow.");
                foreach (var each in overnight) StudioRoomCard(desk, artist, each, each == offer, sheet);
            }
        }

        // What the engineer on duty expects of the ticked songs in the chosen room. Opinion, not a rating.
        var roomTalk = StudioText(sheet, "", 14, Heard);
        void UpdateRoomTalk() {
            var lines = !incremental ? new List<string>() : checks.Where(c => c.Box.ButtonPressed && shapes.ContainsKey(c.Choice))
                .Select(c => StudioPeople.SongInRoom(offer.Room, shapes[c.Choice], c.Choice.Title)).Where(line => line != null).ToList();
            roomTalk.Text = string.Join("\n", lines);
            roomTalk.Visible = lines.Count > 0;
        }
        foreach (var item in checks) item.Box.Toggled += _ => UpdateRoomTalk();
        UpdateRoomTalk();

        // 3. Time and brief
        sheet.AddChild(WorkOrder.Section(incremental ? "3   Time & brief" : "3   Time"));
        var timeRow = StudioRow(sheet, 16);
        var hoursInput = Spin(PlayerDesk.MinSessionHours, PlayerDesk.MaxSessionHours, 1, studioHours);
        timeRow.AddChild(WorkOrder.Field("Hours booked", hoursInput, 120));
        var window = new Label { VerticalAlignment = VerticalAlignment.Bottom, SizeFlagsVertical = SizeFlags.ShrinkEnd };
        window.AddThemeFontOverride("font", PaperTheme.Typed);
        window.AddThemeFontSizeOverride("font_size", 16);
        window.AddThemeColorOverride("font_color", Ink);
        timeRow.AddChild(window);
        if (incremental) {
            var briefs = StudioFlow(sheet);
            for (int i = 0; i < BriefNames.Length; i++) {
                var choice = Btn(BriefNames[i]);
                StyleToggle(choice, i == studioBrief);
                int index = i;
                choice.Pressed += () => { if (studioBrief == index) return; studioBrief = index; RefreshKeepingScroll(); };
                briefs.AddChild(choice);
            }
            StudioText(sheet, BriefNotes[Mathf.Clamp(studioBrief, 0, BriefNotes.Length - 1)] + " A brief adds no quality by itself, and you can change course on the day.", 14, Heard);
        }

        // 4. Who plays: the owner's house players, a booked session musician, backing singers for a singer who needs them.
        StudioBackingOffer backingOffer = null;
        CheckBox backingBox = null;
        Label backingTalk = null;
        var booked = incremental ? desk.SessionPlayerFor(artist.artistId) : null;
        bool needsBacking = incremental && PlayerDesk.StudioNeedsBacking(artist);
        if (incremental && (offer.House != null || booked != null || needsBacking)) {
            sheet.AddChild(WorkOrder.Section("4   Who plays"));
            if (offer.House != null) {
                StudioText(sheet, (offer.House.Insists ? "House players, at the owner's insistence: " : "") + offer.House.Reason, 14, offer.House.Insists ? Ink : Heard);
                foreach (var p in offer.House.Insists ? offer.House.Players : new List<StudioHousePlayer>())
                    StudioText(sheet, $"   {p.Name} on {PlayerDesk.RoleWord(p.Role).ToLowerInvariant()}{(p.ReplacesName != null ? $", in place of {p.ReplacesName}" : "")}  ·  ${p.Fee:N0}", 14, Ink, PaperTheme.Typed);
            }
            if (booked != null) StudioText(sheet, $"Booked in the Band Room: {booked.Name}, sitting in  ·  ${booked.Fee:N0}", 14, Ink);
            if (needsBacking) {
                int bStart = now + offer.DriveHours * 60;
                backingOffer = offer.Overnight ? null : desk.StudioBackingSingers(offer.Room.CityId, artist, time?.CurrentDate ?? GameDate.StartDate, bStart, bStart + studioHours * 60,
                    offer.House?.Insists == true ? offer.House.Players.Select(p => p.PersonId) : null);
                if (backingOffer == null) StudioText(sheet, offer.Overnight ? "Nobody in the act sings behind the lead. Backing singers can be hired when you book the room up there."
                    : "Nobody in the act sings behind the lead, and no backing singers are free in that town for this date.", 14, Heard);
                else {
                    backingBox = Check($"Hire backing singers: {backingOffer.Text}", studioBacking);
                    backingBox.Toggled += on => studioBacking = on;
                    sheet.AddChild(backingBox);
                    backingTalk = StudioText(sheet, "", 14, Heard);
                }
            }
        }
        void UpdateBackingTalk() {
            if (backingTalk == null) return;
            var lines = checks.Where(c => c.Box.ButtonPressed && shapes.ContainsKey(c.Choice)).Select(c => {
                float call = PlayerDesk.StudioBackingCall(shapes[c.Choice], c.Choice.WrittenSong?.Genre ?? artist.primaryGenre);
                return $"\"{c.Choice.Title}\": " + (call >= PlayerDesk.StudioBackingCallBar ? "calls for voices behind the singer."
                    : call < .4f ? "voices would clutter it." : "voices would make little difference.");
            }).ToList();
            backingTalk.Text = string.Join("\n", lines);
            backingTalk.Visible = lines.Count > 0;
        }
        foreach (var item in checks) item.Box.Toggled += _ => UpdateBackingTalk();
        UpdateBackingTalk();

        // 5. How the date goes on tape, how long the room is held, and who directs it.
        StudioCrewOffer producerOffer = null;
        CheckBox eveningBox = null, lateBox = null, producerBox = null;
        SpinBox daysInput = null;
        if (incremental && !offer.Overnight) {
            sheet.AddChild(WorkOrder.Section("5   On tape"));
            StudioText(sheet, StudioEquipment.LayoutText(Math.Max(1, offer.Room.Tracks)) + ".", 14, Ink);
            var layouts = StudioFlow(sheet);
            foreach (StudioLayout layout in Enum.GetValues<StudioLayout>()) {
                var choice = Btn(PlayerDesk.StudioLayoutName(layout));
                bool allowed = PlayerDesk.StudioLayoutAllowed(layout, Math.Max(1, offer.Room.Tracks));
                if (!allowed && studioLayout == (int)layout) studioLayout = 0;
                StyleToggle(choice, studioLayout == (int)layout);
                choice.Disabled = !allowed;
                choice.TooltipText = layout switch {
                    StudioLayout.VocalApart => allowed ? "The singer goes behind a screen on the second track. Cleaner to balance, and the vocal can be sung again over a kept backing; the singer loses a little of the room." : "Needs a machine with two tracks.",
                    StudioLayout.ThreeTrack => allowed ? "Band, vocal, and a spare track: a part can be added later, on top of a kept take." : "Needs a machine with three tracks.",
                    _ => "Everyone at once, as the act plays. Nothing can be separated later; plenty of great records were made this way."
                };
                int index = (int)layout;
                choice.Pressed += () => { if (studioLayout == index) return; studioLayout = index; RefreshKeepingScroll(); };
                layouts.AddChild(choice);
            }
            var lockRow = StudioRow(sheet, 16);
            daysInput = Spin(1, PlayerDesk.StudioMaxLockoutDays, 1, studioDays);
            lockRow.AddChild(WorkOrder.Field("Days (lockout)", daysInput, 140));
            StudioText(lockRow, "Hold the room for more than one day at a block rate. It stays set up overnight; the band sleeps off most of the day's wear.", 13, Heard);
            if (offer.Room.EveningWork) {
                int startAt = now + offer.DriveHours * 60;
                bool eveningOpen = startAt >= PlayerDesk.StudioEveningStartHour * 60;
                if (!eveningOpen) studioEvening = false;
                eveningBox = Check($"Evening rate ({PlayerDesk.StudioEveningRateFactor:P0} of the day rate, from {PlayerDesk.StudioEveningStartHour - 12} p.m.; the band comes in tired)", studioEvening);
                eveningBox.Disabled = !eveningOpen;
                eveningBox.TooltipText = eveningOpen ? "" : $"The owner takes evening work from {PlayerDesk.StudioEveningStartHour - 12} p.m.";
                eveningBox.Toggled += on => studioEvening = on;
                sheet.AddChild(eveningBox);
                // Booked to run late, the evening goes on past midnight to the small hours; the next morning starts late.
                if (!studioEvening) studioLate = false;
                lateBox = Check("Run on past midnight (to 2 a.m. at the latest; the small hours tire the band, and you sleep in the next morning)", studioLate);
                lateBox.Disabled = !studioEvening;
                lateBox.TooltipText = studioEvening ? "" : "Tick the evening rate first: a late night is an evening date that runs on.";
                lateBox.Toggled += on => studioLate = on;
                eveningBox.Toggled += on => { lateBox.Disabled = !on; if (!on) lateBox.ButtonPressed = false; };
                sheet.AddChild(lateBox);
            }
            int pStart = now + offer.DriveHours * 60;
            producerOffer = desk.StudioProducerOffer(offer.Room.CityId, artist, time?.CurrentDate ?? GameDate.StartDate, pStart, pStart + studioHours * 60);
            if (producerOffer != null) {
                producerBox = Check($"Bring in a producer: {producerOffer.Text}", studioProducer);
                producerBox.Toggled += on => studioProducer = on;
                sheet.AddChild(producerBox);
            } else studioProducer = false;
        }
        int Days() => (int)(daysInput?.Value ?? 1);
        bool Evening() => eveningBox?.ButtonPressed == true;
        bool Late() => Evening() && lateBox?.ButtonPressed == true;
        float ProducerFee() => producerBox?.ButtonPressed == true ? producerOffer.Fee : 0;
        StudioBookingOptions Options() => new() { Layout = (StudioLayout)studioLayout, Days = Days(), Evening = Evening(), LateNight = Late(), ProducerId = producerBox?.ButtonPressed == true ? producerOffer.PersonId : null };

        // Quote
        sheet.AddChild(WorkOrder.Section("Quote"));
        var quote = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        quote.AddThemeConstantOverride("separation", 2);
        sheet.AddChild(quote);
        var plan = StudioText(sheet, "", 14, Heard);
        var footer = StudioRow(sheet);
        footer.AddChild(StudioSpacer());
        bool overnightRoom = incremental && offer.Overnight;
        // A same-day room can also be driven up to the day before, to start the date with a full day.
        Button driveUp = null;
        if (incremental && !offer.Local && !overnightRoom) {
            driveUp = Btn("DRIVE UP TODAY, RECORD TOMORROW");
            driveUp.CustomMinimumSize = new Vector2(0, 44);
            driveUp.TooltipText = $"Drive the band up now and book the room there tomorrow with a full day. Band motel ${offer.BandLodging:N0} a night, plus your own.";
            footer.AddChild(driveUp);
        }
        var book = Primary(overnightRoom ? "DRIVE UP WITH THE BAND" : "BOOK THE ROOM");
        book.CustomMinimumSize = new Vector2(260, 44);
        footer.AddChild(book);

        void QuoteLine(string what, string amount, bool total = false) {
            var row = StudioRow(quote, 6);
            var left = new Label { Text = what, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var right = new Label { Text = amount, HorizontalAlignment = HorizontalAlignment.Right };
            foreach (var label in new[] { left, right }) {
                label.AddThemeFontOverride("font", total ? PaperTheme.TypedBold : PaperTheme.Typed);
                label.AddThemeFontSizeOverride("font_size", 16);
                label.AddThemeColorOverride("font_color", Ink);
                row.AddChild(label);
            }
        }
        int drive = offer?.DriveHours ?? 0;
        float gas = offer?.Gas ?? 0;
        float RoomCost(int hours) => incremental ? Mathf.Round(PlayerDesk.StudioRoomCost(offer.Room, hours) * PlayerDesk.StudioLockoutFactor(Days()) * (Evening() ? PlayerDesk.StudioEveningRateFactor : 1)) : desk.SessionCost(tier, hours);
        bool onThisTrip = desk.ActiveStudioTrip is { } myTrip && myTrip.ArtistId == artist.artistId && myTrip.CityId == desk.CurrentCityId;
        float houseFee = incremental && offer.House?.Insists == true ? offer.House.Fee : 0;
        float BackingFee() => backingBox?.ButtonPressed == true ? backingOffer.Fee : 0;
        void UpdateCost() {
            int hours = (int)hoursInput.Value;
            float roomCost = RoomCost(hours);
            float musicianFee = (incremental ? desk.SessionPlayerFor(artist.artistId)?.Fee ?? 0 : 0) + houseFee + BackingFee() + ProducerFee();
            int songCount = checks.Count(check => check.Box.ButtonPressed);
            foreach (Node child in quote.GetChildren()) { quote.RemoveChild(child); child.QueueFree(); }
            float rate = incremental ? offer.Room.HourlyRate : desk.StudioHourlyRate(tier);
            if (overnightRoom) {
                // Today is the drive and a night at the motel; the room is booked and paid up there tomorrow.
                float trip = gas + offer.BandLodging + PlayerDesk.HotelNightly;
                window.Text = $"Tomorrow, after a {drive}h drive today";
                QuoteLine($"Gas for the {drive}h drive up (the drive home costs the same)", $"${gas:N0}");
                QuoteLine("Band motel tonight, two to a room", $"${offer.BandLodging:N0}");
                QuoteLine("Your own motel tonight", $"${PlayerDesk.HotelNightly:N0}");
                QuoteLine("Total today", $"${trip:N0}", true);
                QuoteLine($"Then tomorrow: room, {hours} h at ${rate:N0}{(musicianFee > 0 ? ", plus musicians" : "")}, paid when you book it there", $"${roomCost + musicianFee:N0}");
                QuoteLine("Cash after the trip and the date", Money(desk.Label.cashReserves - trip - roomCost - musicianFee));
                string tripProblem = offer.Locked != null ? offer.Locked
                    : !desk.AtHome ? "You need to be back in town to set off with the band."
                    : time != null && drive * 60 > time.MinutesRemainingWithOvertime ? $"The {drive}h drive won't fit before the {time.HardStopTime} hard stop. Set off early in the day."
                    : desk.Label.cashReserves < gas + offer.BandLodging ? $"You're {Money(gas + offer.BandLodging - desk.Label.cashReserves)} short of the gas and the band's motel."
                    : null;
                plan.Text = tripProblem ?? $"You'll be in {DistanceModel.GetCityById(offer.Room.CityId)?.name} tonight with {artist.stageName}. Book the room there tomorrow (or today, if there's light left); the songs you tick here stay ticked.";
                plan.AddThemeColorOverride("font_color", tripProblem == null ? Heard : Rust);
                book.Disabled = tripProblem != null;
                return;
            }
            float after = desk.Label.cashReserves - roomCost - musicianFee - gas;
            int start = now + drive * 60;
            window.Text = $"{StudioClockStrip.ClockText(start)} to {StudioClockStrip.ClockText(start + hours * 60)}" + (drive > 0 ? $", after a {drive}h drive" : ", starting now");
            float factor = incremental ? PlayerDesk.StudioLockoutFactor(Days()) * (Evening() ? PlayerDesk.StudioEveningRateFactor : 1) : 1;
            QuoteLine($"Room, {hours} h at ${rate:N0}" + (Days() > 1 ? $" a day, {Days()} days at the block rate" : "") + (Evening() ? ", evening rate" : "") +
                (incremental && offer.Room.BaseRate > 0 && rate > offer.Room.BaseRate ? $" (was ${offer.Room.BaseRate:N0} before its records got around)" : ""), $"${Mathf.Round(rate * hours * factor):N0}");
            if (incremental && offer.Room.EngineerPremium > 0) QuoteLine($"{offer.Room.EngineerName} {(offer.Room.EngineerHomeRoomId != null ? "brought in from across town" : "asked for by name")}, {hours} h at ${offer.Room.EngineerPremium:N0}", $"${Mathf.Round(offer.Room.EngineerPremium * hours * factor):N0}");
            if (incremental && desk.SessionPlayerFor(artist.artistId) is { } bandRoom) QuoteLine("Booked session musician", $"${bandRoom.Fee:N0}");
            if (houseFee > 0) QuoteLine($"House players ({offer.House.Players.Count}), at the owner's insistence", $"${houseFee:N0}");
            if (BackingFee() > 0) QuoteLine($"Backing singers: {backingOffer.GroupName}", $"${BackingFee():N0}");
            if (ProducerFee() > 0) QuoteLine($"Producer: {producerOffer.Name}", $"${ProducerFee():N0}");
            if (drive > 0) QuoteLine($"Gas for the {drive}h drive out (the drive home costs the same)", $"${gas:N0}");
            if (incremental) QuoteLine($"Setup, tape stock (about {PlayerDesk.StudioStockIncluded(Math.Max(1, songCount), hours * Days(), checks.Where(c => c.Box.ButtonPressed).Select(c => StudioSongStructures.PassMinutes(shapes[c.Choice])).DefaultIfEmpty(PlayerDesk.StudioTakeMinutes).Average())} passes), playback, house finish", "included");
            QuoteLine("Total", $"${roomCost + musicianFee + gas:N0}", true);
            QuoteLine("Cash after", Money(after));

            var passes = checks.Where(check => check.Box.ButtonPressed).Select(check => incremental ? StudioSongStructures.PassMinutes(shapes[check.Choice]) : PlayerDesk.StudioTakeMinutes).ToList();
            int setup = incremental ? offer.Room.SetupMinutes : PlayerDesk.StudioSetupMinutes;
            int minimum = setup + passes.Sum() + songCount * PlayerDesk.StudioFinishMinutesPerSide;
            bool tooTight = incremental && minimum > hours * 60 * Days();
            bool lateDrive = drive > 0 && time != null && (drive + (incremental && Late() ? 0 : hours)) * 60 > time.MinutesRemainingWithOvertime;
            string problem = offer?.Locked != null ? "No introduction yet: this room books its regulars and people who come recommended."
                : songCount == 0 ? "Tick at least one song to book."
                : !desk.AtHome && !onThisTrip ? "You need to be back in town, or up here with this act on a studio trip, to book a room."
                : lateDrive ? $"The drive and a {hours}h date won't fit before the {time.HardStopTime} hard stop. Book this room early in the day."
                : incremental && Late() && now + drive * 60 + hours * 60 > PlayerDesk.StudioLateStopMinute ? $"A late night runs to 2 a.m. at the latest. Book fewer hours."
                : incremental && Evening() && !Late() && time != null && hours * 60 > time.MinutesRemainingWithOvertime ? $"An evening date ends at the {time.HardStopTime} hard stop. Run it on past midnight, or book fewer hours."
                : after < 0 ? $"You're {Money(-after)} short of this booking."
                : tooTight ? "Not enough time: setup, a pass per song and finishing won't fit. Add an hour or drop a song."
                : null;
            plan.Text = problem ?? (incremental
                ? $"Plan: {setup}m setup, then one pass per song ({string.Join(", ", passes.Select(m => m + "m"))}: the song twice, for the take and a listen back, plus a reset) and {PlayerDesk.StudioFinishMinutesPerSide}m finish each — {minimum}m of {hours * 60}m. That leaves {StudioClockStrip.DurationText(hours * 60 - minimum)} for rehearsal, retakes and direction."
                : $"{songCount} song(s) in {hours} hours.");
            plan.AddThemeColorOverride("font_color", problem == null ? Heard : Rust);
            book.Disabled = problem != null;
            if (driveUp != null) driveUp.Disabled = offer.Locked != null || !desk.AtHome || desk.Label.cashReserves < gas + offer.BandLodging ||
                time != null && drive * 60 > time.MinutesRemainingWithOvertime;
        }
        hoursInput.ValueChanged += value => { studioHours = (int)value; UpdateCost(); };
        if (daysInput != null) daysInput.ValueChanged += value => { studioDays = (int)value; UpdateCost(); };
        if (eveningBox != null) eveningBox.Toggled += _ => UpdateCost();
        if (lateBox != null) lateBox.Toggled += _ => UpdateCost();
        if (producerBox != null) producerBox.Toggled += _ => UpdateCost();
        foreach (var item in checks) item.Box.Toggled += _ => UpdateCost();
        if (backingBox != null) backingBox.Toggled += _ => UpdateCost();
        UpdateCost();

        string roomId = offer?.Room.Id, engineerId = roomId == null ? null : studioEngineerAsk.GetValueOrDefault(roomId);
        void DriveUp() {
            bool ok = desk.DriveUpForStudio(artist, roomId, out string message);
            Say(message, ok);
            Refresh();
        }
        if (driveUp != null) driveUp.Pressed += DriveUp;
        book.Pressed += () => {
            if (overnightRoom) { DriveUp(); return; }
            var chosen = checks.Where(c => c.Box.ButtonPressed).Select(c => c.Choice).ToList();
            var brief = (StudioBrief)studioBrief;
            bool backing = backingBox?.ButtonPressed == true;
            var responses = PlayerDesk.Instance.MaterialRefusals(artist, chosen, tier);
            if (responses.Count > 0) { RefusalDialog(artist, chosen, tier, (int)hoursInput.Value, responses, checks, brief, roomId, engineerId, backing, incremental ? Options() : null); return; }
            int bookHours = (int)hoursInput.Value;
            void Book() {
                bool ok = PlayerDesk.Instance.StartSession(artist, chosen, tier, bookHours, out string message, brief: brief, roomId: roomId, engineerId: engineerId, backingSingers: backing, options: incremental ? Options() : null);
                if (ok) { studioSide = 0; studioReview = false; studioOpenTakes.Clear(); studioBacking = false; studioProducer = false; studioEvening = false; studioLate = false; studioDays = 1; }
                Say(message, ok);
                Refresh();
            }
            float dateCost = RoomCost(bookHours) + (desk.SessionPlayerFor(artist.artistId)?.Fee ?? 0) + houseFee + BackingFee() + ProducerFee() + gas;
            if (!WarnIfUnderOverhead($"{roomName} for {bookHours}h", dateCost, $"BOOK IT — ${dateCost:N0}", Book)) Book();
        };
    }

    private static void StudioRoomGroup(VBoxContainer sheet, string title, string note) {
        var head = StudioRow(sheet, 10);
        var name = new Label { Text = title.ToUpperInvariant() };
        name.AddThemeFontOverride("font", PaperTheme.SansBold);
        name.AddThemeFontSizeOverride("font_size", 13);
        name.AddThemeColorOverride("font_color", Rust);
        head.AddChild(name);
        StudioText(head, note, 13, Heard);
    }

    /// <summary>One room on the booking sheet. Every card shows the name, the price and the sound in a word; the chosen
    /// card opens up with its engineer, finishing, travel and anything the engineer remembers about the act.</summary>
    private void StudioRoomCard(PlayerDesk desk, SimulatedArtist artist, StudioRoomOffer offer, bool selected, VBoxContainer sheet) {
        var room = offer.Room;
        bool locked = offer.Locked != null;
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop,
            MouseDefaultCursorShape = selected ? CursorShape.Arrow : CursorShape.PointingHand };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = selected ? PaperTheme.PaperLight : locked ? new Color("e2d3a6") : new Color("e9d8a8"),
            BorderColor = selected ? Ink : new Color("70552c"),
            BorderWidthLeft = selected ? 6 : 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = selected ? 2 : 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8
        });
        if (!selected) card.GuiInput += input => {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) { studioRoomId = room.Id; RefreshKeepingScroll(); }
        };
        sheet.AddChild(card);
        var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        body.AddThemeConstantOverride("separation", selected ? 6 : 2);
        card.AddChild(body);

        Label Text(Container host, string text, int size, Color color, Font font = null, bool wrap = true) {
            var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = wrap ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin,
                AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off, VerticalAlignment = VerticalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);
            if (font != null) label.AddThemeFontOverride("font", font);
            host.AddChild(label);
            return label;
        }
        void Chip(Container host, string text, Color ink) {
            var chip = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            chip.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(ink, .12f), BorderColor = new Color(ink, .6f),
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
                ContentMarginLeft = 7, ContentMarginRight = 7, ContentMarginTop = 1, ContentMarginBottom = 1 });
            Text(chip, text.ToUpperInvariant(), 11, ink, PaperTheme.SansBold, wrap: false);
            host.AddChild(chip);
        }

        var head = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        head.AddThemeConstantOverride("separation", 10);
        body.AddChild(head);
        Text(head, room.Name, 19, Ink, PaperTheme.SerifBold, wrap: false);
        if (StudioPeople.SoundTag(room) is { } tag) Chip(head, tag, room.Specialist ? PaperTheme.Oxblood : WorkOrder.FormInk);
        if (locked) Chip(head, "Introduction needed", PaperTheme.Oxblood);
        else if (offer.Introduction != null) Chip(head, "Introduced", new Color("3f6b3a"));
        if (desk.StudioMemory(room, artist.artistId) != null) Chip(head, "Knows this act", new Color("3f6b3a"));
        if (offer.House?.Insists == true && offer.House.Locked == null) Chip(head, "House band", WorkOrder.FormInk);
        if (room.Renown >= .15f) Chip(head, "In demand", PaperTheme.Oxblood);
        if (offer.Overnight) Chip(head, "Overnight", Rust);
        head.AddChild(StudioSpacer());
        Text(head, $"${room.HourlyRate:N0} an hour", 18, Ink, PaperTheme.TypedBold, wrap: false);

        string where = offer.Local ? "" : $"{DistanceModel.GetCityById(room.CityId)?.name} · {offer.DriveHours}h drive each way · ${offer.Gas:N0} gas";
        if (!selected) {
            string sub = string.Join("   ·   ", new[] { where, room.EngineerName == null ? null : (offer.Overnight ? "Engineer tomorrow: " : "Engineer today: ") + room.EngineerName, StudioPeople.AcousticsShort(room) }
                .Where(s => !string.IsNullOrEmpty(s)));
            Text(body, sub, 14, Heard);
            return;
        }

        // The chosen room, in labelled rows.
        void Row(string label, string text, Color? color = null) {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
            row.AddThemeConstantOverride("separation", 10);
            body.AddChild(row);
            var name = Text(row, label.ToUpperInvariant(), 11, Rust, PaperTheme.SansBold, wrap: false);
            name.CustomMinimumSize = new Vector2(92, 0); name.VerticalAlignment = VerticalAlignment.Top;
            Text(row, text, 15, color ?? Ink);
        }
        string town = DistanceModel.GetCityById(room.CityId)?.name;
        if (offer.Overnight) Row("Travel", $"{where}. Too far to drive and record the same day: drive the band up today, stay the night " +
            $"(band motel ${offer.BandLodging:N0}, two to a room, plus your own), and book the room in {town} tomorrow. Motel and gas are the label's, not studio cost.");
        else if (!offer.Local) Row("Travel", $"{where}. You'll be in {town} when the date ends. Or drive up today and record tomorrow with a full day (band motel ${offer.BandLodging:N0} a night).");
        Row("Sound", StudioPeople.AcousticsText(room) + " " + room.Layout + ".");
        if (locked) Row("Access", offer.Locked, PaperTheme.Oxblood);
        else if (offer.Introduction != null) Row("Access", offer.Introduction);
        if (offer.House is { Locked: null } house)
            Row("House band", house.Insists ? house.Reason + " " + string.Join("; ", house.Players.Select(p => $"{p.Name} ({PlayerDesk.RoleWord(p.Role).ToLowerInvariant()}, ${p.Fee:N0})")) + "."
                : house.Reason, house.Insists ? Ink : new Color("3f6b3a"));
        if (desk.StudioRenownText(room) is { } renown) Row("Reputation", renown);
        if (room.EngineerName != null) {
            string first = room.EngineerName.Split(' ')[0];
            Row("Engineer", $"{room.EngineerName}, {StudioPeople.RoleText(room)}{(room.EngineerRequested ? ", as you asked" : "")}. {first} {StudioPeople.HabitText(room.EngineerHabit)}." +
                (room.EngineerHomeRoomId != null ? $" ${room.EngineerPremium:N0} an hour on top of the room; with a board to learn, setup runs {room.SetupMinutes}m." : ""));
            HBoxContainer EngineerLine() {
                var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
                line.AddThemeConstantOverride("separation", 10);
                body.AddChild(line);
                line.AddChild(new Control { CustomMinimumSize = new Vector2(92, 0), MouseFilter = MouseFilterEnum.Ignore });
                return line;
            }
            if (room.EngineerHomeRoomId != null) {
                string hostId = room.Id;
                var back = Btn("LET THE ROOM CHOOSE");
                back.Pressed += () => { studioEngineerAsk.Remove(hostId); RefreshKeepingScroll(); };
                EngineerLine().AddChild(back);
            }
            // The room's other engineer: ask for them by name when they're in.
            var other = room.EngineerHomeRoomId != null ? null : offer.Engineers.FirstOrDefault(e => e.Id != room.EngineerId);
            if (other != null) {
                var line = EngineerLine();
                bool knows = desk.StudioVisits.Any(v => v.EngineerId == other.Id && v.ArtistId == artist.artistId);
                string otherRole = other.Chief ? "the chief" : "the second engineer";
                if (other.Away) Text(line, $"{other.Name}, {otherRole}, is cutting a date elsewhere today{(knows ? " (and knows this act)" : "")}.", 14, Heard);
                else {
                    bool asking = room.EngineerRequested;
                    var ask = Btn(asking ? "LET THE ROOM CHOOSE"
                        : $"ASK FOR {other.Name.ToUpperInvariant()}, {(other.Chief ? "THE CHIEF" : "THE SECOND")}{(other.Chief ? $"  ·  +${StudioPeople.ChiefPremium(room):N0}/h" : "")}");
                    ask.TooltipText = $"{other.Name.Split(' ')[0]} {StudioPeople.HabitText(other.Habit)}." + (knows ? $" {other.Name.Split(' ')[0]} has worked with this act before." : "");
                    string roomId = room.Id, otherId = other.Id;
                    ask.Pressed += () => { if (asking) studioEngineerAsk.Remove(roomId); else studioEngineerAsk[roomId] = otherId; RefreshKeepingScroll(); };
                    line.AddChild(ask);
                    if (knows && !asking) Text(line, $"{other.Name.Split(' ')[0]} knows this act.", 14, new Color("3f6b3a"));
                }
            }
            // Engineers the label has worked with at other rooms in town: they come across on a quiet day at their own room.
            if (room.EngineerHomeRoomId == null)
                foreach (var (guest, home, fee, refusal) in desk.StudioGuestEngineers(room, artist.artistId, offer.StaffedFor)) {
                    var line = EngineerLine();
                    string guestFirst = guest.Name.Split(' ')[0];
                    bool knows = desk.StudioVisits.Any(v => v.EngineerId == guest.Id && v.ArtistId == artist.artistId);
                    if (refusal != null) { Text(line, refusal, 14, Heard); continue; }
                    var bring = Btn($"BRING IN {guest.Name.ToUpperInvariant()} FROM {home.Name.ToUpperInvariant()}  ·  +${fee:N0}/h");
                    bring.TooltipText = $"{guestFirst}, {(guest.Chief ? "the chief" : "the second engineer")} at {home.Name}, {StudioPeople.HabitText(guest.Habit)}. " +
                        $"{guestFirst} brings an ear and hands but not the room: this room's sound stays its own, and a strange board takes {StudioPeople.GuestSetupMinutes} more minutes to set up." +
                        (knows ? $" {guestFirst} has worked with this act before." : "");
                    string hostId = room.Id, guestId = guest.Id;
                    bring.Pressed += () => { studioEngineerAsk[hostId] = guestId; RefreshKeepingScroll(); };
                    line.AddChild(bring);
                    if (knows) Text(line, $"{guestFirst} knows this act.", 14, new Color("3f6b3a"));
                }
        }
        if (desk.StudioMemory(room, artist.artistId) is { } memory) Row("Remembers", memory, new Color("3f6b3a"));
        Row("Finishing", string.Join(" · ", PlayerDesk.StudioFinishes(room).Select(PlayerDesk.FinishName)));
    }

    private void MaterialComparison(PlayerDesk desk, SimulatedArtist artist, List<PlayerDesk.MaterialChoice> options,
            List<(CheckBox Box, PlayerDesk.MaterialChoice Choice)> checks, PlayerDesk.StudioTier tier, VBoxContainer host) {
        StudioText(host, "These reads estimate audience demand and delivery, with uncertainty. They are not ratings of a recorded take.", 14, Heard);
        var comparePickers = StudioRow(host);
        comparePickers.AddChild(FormLabel("SONG A"));
        var songA = Option(); songA.SizeFlagsHorizontal = SizeFlags.ExpandFill; songA.ClipText = true; songA.CustomMinimumSize = new Vector2(200, 36);
        comparePickers.AddChild(songA);
        comparePickers.AddChild(FormLabel("SONG B"));
        var songB = Option(); songB.SizeFlagsHorizontal = SizeFlags.ExpandFill; songB.ClipText = true; songB.CustomMinimumSize = new Vector2(200, 36);
        comparePickers.AddChild(songB);
        for (int i = 0; i < options.Count; i++) {
            string character = PolarPlayerPerception.DescribeCharacter(options[i]);
            string pickerText = character.Length == 0 ? options[i].Describe() : $"{options[i].Describe()}  —  {character}";
            songA.AddItem(pickerText);
            songB.AddItem(pickerText);
        }
        songA.Selected = 0;
        songB.Selected = options.Count > 1 ? 1 : 0;
        songB.Disabled = options.Count < 2;
        // The same song twice would only draw one chart, so each picker rules out whatever the other has chosen.
        void RuleOutDuplicate() {
            for (int i = 0; i < options.Count; i++) {
                songA.SetItemDisabled(i, options.Count > 1 && i == songB.Selected);
                songB.SetItemDisabled(i, options.Count > 1 && i == songA.Selected);
            }
        }
        RuleOutDuplicate();
        var previewHost = new VBoxContainer(); host.AddChild(previewHost);
        void UpdatePreview() {
            foreach (Node child in previewHost.GetChildren()) { previewHost.RemoveChild(child); child.QueueFree(); }
            var cards = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            cards.AddThemeConstantOverride("h_separation", 12);
            cards.AddThemeConstantOverride("v_separation", 10);
            foreach (int pickIndex in new[] { (int)songA.Selected, (int)songB.Selected }.Distinct()) {
                PlayerDesk.MaterialChoice option = options[Mathf.Clamp(pickIndex, 0, options.Count - 1)];
                string songId = option.SongId ?? option.Title;
                int slot = checks.Take(pickIndex).Count(c => c.Box.ButtonPressed);
                var gate = option.Kind == PlayerDesk.MaterialKind.LiveCover ? PolarEvidenceGate.Rehearsal : PolarEvidenceGate.Demo;
                Control card = ComparisonCard(artist, option, gate, "repertoire:" + artist.artistId + ":" + songId,
                    desk.PreviewSessionContext(tier, artist), slot, actFixed: true);
                card.CustomMinimumSize = new Vector2(420, 0);
                card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                cards.AddChild(card);
            }
            previewHost.AddChild(cards);
        }
        songA.ItemSelected += _ => { RuleOutDuplicate(); UpdatePreview(); };
        songB.ItemSelected += _ => { RuleOutDuplicate(); UpdatePreview(); };
        UpdatePreview();
    }

    // ========================================================================
    // SESSION SHEET (the console)
    // ========================================================================

    private static string SideLetter(int index) => index == 0 ? "A" : index == 1 ? "B" : (index + 1).ToString();

    /// <summary>The console: the room's clock, the song on the floor, its take sheet and its finish. Keeping a take is free.</summary>
    private void TakesView(PlayerDesk.PendingSession session, SimulatedArtist artist) {
        var desk = PlayerDesk.Instance;
        bool incremental = session.FlowVersion > 0;
        int unselected = session.Cuts.Count(cut => cut.KeptTake < 0);
        bool allKept = unselected == 0;
        studioSide = Mathf.Clamp(studioSide, 0, session.Cuts.Count - 1);
        PlayerDesk.SessionCut cut = session.Cuts[studioSide];
        bool finishingStage = allKept && !studioReview;
        int left = incremental ? desk.StudioMinutesLeft(session) : 0;
        int held = PlayerDesk.StudioFinishMinutesPerSide * session.Cuts.Count;

        var form = StudioSheet(incremental ? session.Room.Name : PlayerDesk.StudioTierName(session.Tier), "Session sheet",
            "studio-session:" + (session.SessionId ?? artist.artistId), allKept ? "Ready to print" : null, RubberStamp.Blue);
        var sheet = form.Body;
        var facts = StudioFlow(sheet);
        facts.AddChild(WorkOrder.Typed("Artist", artist.stageName, 220));
        facts.AddChild(WorkOrder.Typed("Booked", $"{session.Hours} h", 70));
        facts.AddChild(WorkOrder.Typed("Paid", $"${session.Cost:N0}", 80));
        if (incremental) facts.AddChild(WorkOrder.Typed("Brief", BriefNames[Mathf.Clamp((int)session.Brief, 0, BriefNames.Length - 1)], 250));
        if (!string.IsNullOrEmpty(session.Room?.EngineerName)) facts.AddChild(WorkOrder.Typed("Engineer", session.Room.EngineerName, 200));
        foreach (var hire in session.Hires.Where(h => h.Kind != StudioHireKind.BackingVoice))
            facts.AddChild(WorkOrder.Typed(hire.Kind == StudioHireKind.Producer ? "Producer" : hire.Kind == StudioHireKind.Arranger ? "Arranger" : (hire.Kind == StudioHireKind.HousePlayer ? "House " : "Session ") + PlayerDesk.RoleWord(hire.Role).ToLowerInvariant(),
                hire.Replaces ? $"{hire.Name} for {hire.ReplacesName}" : hire.Name, 260));
        foreach (var group in session.Hires.Where(h => h.Kind == StudioHireKind.BackingVoice).GroupBy(h => h.GroupName))
            facts.AddChild(WorkOrder.Typed("Backing vocals", $"{group.Key} ({group.Count()})", 260));
        if (incremental && session.Work is { } dateWork) {
            facts.AddChild(WorkOrder.Typed("On tape", PlayerDesk.StudioLayoutName(dateWork.Layout), 200));
            facts.AddChild(WorkOrder.Typed("Tape left", $"~{PlayerDesk.StudioStockLeft(session)} passes", 120));
            if (dateWork.LockoutDays > 1) facts.AddChild(WorkOrder.Typed("Lockout", $"day {Math.Max(1, desk.StudioDayIndex(session) + 1)} of {dateWork.LockoutDays}", 130));
        }

        // The room's clock.
        if (incremental) {
            var clockHead = StudioRow(sheet);
            StudioText(clockHead, $"{StudioClockStrip.DurationText(left)} left on the clock  ·  {held}m of it held for finishing", 15, Ink, PaperTheme.SansSemiBold);
            var rest = Btn($"TAKE A BREAK  ·  {10}m");
            rest.TooltipText = "Everyone gets some energy back. It doesn't fully reset a long day.";
            rest.Pressed += () => { bool ok = desk.BreakStudio(out string m); Say(m, ok); Refresh(); };
            clockHead.AddChild(rest);
            var log = Btn("DATE LOG…");
            log.Pressed += () => {
                var modal = PaperModal.Open(this, "DATE LOG", 760);
                var scroll = ComparisonScroll(); scroll.CustomMinimumSize = new Vector2(0, 400);
                var entries = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(entries);
                foreach (string entry in session.ActionLog) entries.AddChild(FaintLine(entry));
                modal.Body.AddChild(scroll); modal.AddButton("BACK TO THE DATE", null);
            };
            clockHead.AddChild(log);
            if (desk.StudioHasFault(PlayerDesk.StudioBuzzFault)) {
                var fix = Btn($"FIND THE BUZZ  ·  {PlayerDesk.StudioFixMinutes}m");
                fix.TooltipText = "An amplifier is buzzing onto the tape. Every pass carries it until it's found.";
                fix.Pressed += () => { bool ok = desk.FixStudioFault(out string m); Say(m, ok); Refresh(); };
                clockHead.AddChild(fix);
            }
            sheet.AddChild(new StudioClockStrip().Set(session.StartMinute, session.Hours * 60, session.Hours * 60 - left, held));
            if (desk.StudioSleepsOn) StudioText(sheet, "The room is yours again tomorrow: when today's work is done, end the day from the calendar and come back in the morning. The kit stays set up.", 13, Heard);
        }

        // The tapes: one per song on the date.
        var sides = StudioFlow(sheet);
        for (int i = 0; i < session.Cuts.Count; i++) {
            int side = i; var each = session.Cuts[i];
            int complete = each.Takes.Count(t => !t.Incomplete), broke = each.Takes.Count(t => t.Incomplete && !t.FalseStart);
            string status = each.KeptTake >= 0 ? $"KEPT TAKE {each.Takes[each.KeptTake].Number}"
                : each.Takes.Count == 0 ? "UNCUT" : complete == 0 ? "NOTHING TO KEEP YET"
                : $"{complete} {(complete == 1 ? "TAKE" : "TAKES")}{(broke > 0 ? $" (+{broke} BROKE DOWN)" : "")}, NONE KEPT";
            var sideButton = Btn($"{SideLetter(i)} · {each.Choice.Title} · {status}");
            sideButton.CustomMinimumSize = new Vector2(0, 38);
            StyleToggle(sideButton, studioSide == i);
            sideButton.Pressed += () => { studioSide = side; Refresh(); contentScroll.ScrollVertical = 0; };
            sides.AddChild(sideButton);
        }

        // The running order, while nothing has been cut: the band is freshest for the first song.
        if (incremental && studioSide > 0 && session.Cuts.All(c => c.Takes.Count == 0)) {
            var first = Btn($"CUT \"{cut.Choice.Title.ToUpperInvariant()}\" FIRST");
            first.TooltipText = "Change the running order before tape rolls. The band is freshest for the first song, and the producer works in this order.";
            first.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            first.Pressed += () => { bool ok = desk.CutStudioSideFirst(studioSide, out string m); if (ok) studioSide = 0; Say(m, ok); Refresh(); };
            sheet.AddChild(first);
        }

        // The song on the floor.
        sheet.AddChild(WorkOrder.Section(finishingStage ? "Finishing" : "On the floor"));
        StudioText(sheet, $"\"{cut.Choice.Title}\"  ({cut.Choice.Detail})", 22, Ink, PaperTheme.SerifBold);
        if (incremental && !finishingStage) {
            string direction = cut.Direction == StudioDirection.None ? "no direction given" : "direction: " + PlayerDesk.DirectionName(cut.Direction).ToLowerInvariant();
            string rehearsed = cut.Rehearsals == 0 ? "not rehearsed" : cut.Rehearsals == 1 ? "rehearsed once" : $"rehearsed {cut.Rehearsals} times";
            var practised = cut.Structure == null ? new List<string>() : cut.Structure.Parts.Where(p => cut.PartRehearsals[(int)p] > 0)
                .Select(p => cut.PartRehearsals[(int)p] == 1 ? "the " + StudioSongStructures.PartName(p) : $"the {StudioSongStructures.PartName(p)} \u00d7{cut.PartRehearsals[(int)p]}").ToList();
            if (practised.Count > 0) rehearsed += "; worked on " + string.Join(", ", practised);
            StudioText(sheet, $"{PlayerDesk.ArrangementName(cut.Arrangement)}  ·  {direction}  ·  {rehearsed}", 14, Heard);
            if (cut.Work != null) {
                var shapeWords = new List<string>();
                if (cut.Work.KeyShift != 0) shapeWords.Add(cut.Work.KeyShift < 0 ? "a step lower" : "a step higher");
                if (cut.Work.TempoShift != 0) shapeWords.Add(cut.Work.TempoShift > 0 ? "tempo pushed" : "tempo eased");
                if (cut.Work.Trimmed) shapeWords.Add("short intro, clean ending");
                if (cut.Work.KeepPickup) shapeWords.Add("with the pickup");
                if (cut.Work.Balance != StudioBalance.Natural) shapeWords.Add(PlayerDesk.BalanceName(cut.Work.Balance).ToLowerInvariant());
                if (shapeWords.Count > 0) StudioText(sheet, string.Join("  ·  ", shapeWords), 14, Heard);
            }
            if (cut.Structure is { } map) StudioText(sheet, $"Song map ({StudioSongStructures.FormName(map.Form)}, {StudioSongStructures.Length(map.DurationSeconds)}):  " +
                string.Join("  ›  ", map.Sections.Select(x => x.Label.Replace("the ", ""))), 13, Heard);
        }
        StudioText(sheet, cut.Takes.Count == 0 ? "Next: cut a first pass. Rehearsal is optional."
            : cut.Takes.All(t => t.Incomplete) ? "Every pass so far has broken down. Work the part that fell apart, give a direction, or go again."
            : cut.KeptTake < 0 ? "Next: read the notes and keep a performance, or try another pass. Earlier takes stay safe."
            : !allKept ? "This performance is kept. Move to the next song, or keep working on this one."
            : "Every song has a kept performance. Choose a finish, or print with the included house finish.", 15, Rust, PaperTheme.SansSemiBold);

        if (allKept) {
            var review = Btn(studioReview ? "‹ BACK TO FINISHING" : "REVIEW TAKES & MORE WORK…");
            review.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            review.Pressed += () => { studioReview = !studioReview; Refresh(); };
            sheet.AddChild(review);
        }
        if (incremental && !finishingStage) FloorActions(session, cut, studioSide, sheet);

        // The take sheet.
        if (cut.Takes.Count > 0) {
            if (!finishingStage) sheet.AddChild(WorkOrder.Section("Take sheet"));
            for (int t = cut.Takes.Count - 1; t >= 0; t--) {
                if (finishingStage && t != cut.KeptTake) continue;
                TakeRow(cut, studioSide, t, incremental, t == cut.Takes.Count - 1 || t == cut.KeptTake || finishingStage, sheet);
            }
        }

        // The finish for the kept performance.
        if (incremental && cut.KeptTake >= 0) {
            if (!finishingStage) sheet.AddChild(WorkOrder.Section("Finish"));
            StudioText(sheet, $"Finish for take {cut.Takes[cut.KeptTake].Number}. The house finish comes with the room; other processing treats the whole tape and takes a few minutes.", 14, Heard);
            var finishes = StudioFlow(sheet);
            int side = studioSide;
            foreach (StudioFinish finish in PlayerDesk.StudioFinishes(session.Room, cut.Takes[cut.KeptTake])) {
                var finishButton = Btn(PlayerDesk.FinishName(finish));
                bool current = cut.Finish == finish;
                StyleToggle(finishButton, current);
                if (!current) finishButton.Pressed += () => {
                    var dialog = PaperModal.Open(this, "FINISH THE KEPT SOURCE", 780);
                    dialog.AddText(desk.StudioFinishProposal(side, finish));
                    dialog.AddButton("USE THIS FINISH", () => { bool ok = desk.FinishStudioSide(side, finish, out string m); Say(m, ok); Refresh(); }, PaperModal.ButtonKind.Primary);
                    dialog.AddButton("LEAVE IT AS IT IS", null);
                };
                finishes.AddChild(finishButton);
            }
            if (PlayerDesk.SelectedStudioFinish(cut) is { } processed) StudioText(sheet, processed.Note, 14, Heard);
        }

        // Signing off the sheet.
        sheet.AddChild(new ColorRect { Color = new Color(WorkOrder.FormInk, 0.6f), CustomMinimumSize = new Vector2(0, 1.5f), MouseFilter = MouseFilterEnum.Ignore });
        StudioText(sheet, allKept ? "Every side has a kept performance." : $"{unselected} {(unselected == 1 ? "side still needs" : "sides still need")} a kept performance.", 14, Heard);
        var buttons = StudioRow(sheet, 12);
        if (cut.KeptTake >= 0 && !allKept) {
            var nextSide = Primary("WORK ON THE NEXT SONG");
            nextSide.CustomMinimumSize = new Vector2(0, 44);
            nextSide.Pressed += () => { studioSide = session.Cuts.FindIndex(c => c.KeptTake < 0); Refresh(); contentScroll.ScrollVertical = 0; };
            buttons.AddChild(nextSide);
        }
        string printText = incremental ? "FINISH & PRINT MASTERS" : "PRINT MASTERS";
        var print = allKept ? Primary(printText) : Btn(printText);
        print.Disabled = !allKept;
        print.CustomMinimumSize = new Vector2(240, 44);
        print.TooltipText = allKept ? "Print the masters and send them to the Catalog, where you pair the sides and press the record." : "Keep a performance for every side first.";
        print.Pressed += () => {
            bool ok = desk.PrintSession(out string message);
            Say(message, ok);
            // A finished master's next stop is its card on the Catalog board, where it is assembled into a 45.
            if (ok) UIManager.Instance?.OpenStudioCatalog();
            else Refresh();
        };
        buttons.AddChild(print);
        var delegateChoice = Btn(incremental ? "RUN THE REST OF THE DATE" : "LET THE PRODUCER CHOOSE");
        delegateChoice.CustomMinimumSize = new Vector2(0, 44);
        delegateChoice.Disabled = allKept;
        delegateChoice.TooltipText = incremental
            ? "Hand the remaining work to the producer under your brief. Same clock, same notes, and your kept choices stay. You still review and print."
            : "The producer picks from the listening notes for the sides you haven't kept.";
        delegateChoice.Pressed += () => {
            if (!incremental) { desk.DelegateTakeSelection(); Say("The producer chose from the listening notes for the unselected sides.", true); Refresh(); return; }
            var dialog = PaperModal.Open(this, "RUN THE REST OF THE DATE", 720);
            dialog.AddText("The producer works on under your brief: the same clock, notes and refusals as you. Your kept takes stay kept. Set what may be spent and when to hand back.");
            var limitRow = new HBoxContainer(); limitRow.AddThemeConstantOverride("separation", 12);
            var limit = Spin(0, 200, 5, 0);
            limitRow.AddChild(WorkOrder.Field("Spending limit ($)", limit, 160));
            var limitNote = new Label { Text = "Zero: hire nobody and buy nothing. Otherwise tape stock and backing singers, up to the limit.", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            limitNote.AddThemeColorOverride("font_color", Heard);
            limitRow.AddChild(limitNote);
            dialog.Body.AddChild(limitRow);
            var promising = Check("Stop at a take that sounds like the record", true);
            var idea = Check("Stop when someone has an idea for the song", true);
            var threat = Check("Stop if a side is running out of time", true);
            foreach (var box in new[] { promising, idea, threat }) dialog.Body.AddChild(box);
            dialog.AddButton("RUN IT", () => {
                bool ok = desk.RunStudioDate(new StudioDelegation { SpendLimit = (float)limit.Value, StopAtPromising = promising.ButtonPressed, StopAtIdea = idea.ButtonPressed, StopAtThreat = threat.ButtonPressed }, out string m);
                Say(m, ok); Refresh();
            }, PaperModal.ButtonKind.Primary);
            dialog.AddButton("NOT YET", null);
        };
        buttons.AddChild(delegateChoice);
        buttons.AddChild(StudioSpacer());
        var scrap = Btn("ABANDON DATE…");
        scrap.CustomMinimumSize = new Vector2(0, 44);
        scrap.Pressed += () => {
            var confirm = PaperModal.Open(this, "ABANDON THIS DATE?", 640);
            confirm.AddText($"The ${session.Cost:N0} already paid will not be refunded. These unprinted performances will be discarded and remaining reservations released.");
            confirm.AddButton("KEEP WORKING", null);
            confirm.AddButton("ABANDON DATE", () => { desk.ScrapSession(); Say("Date abandoned.", true); Refresh(); }, PaperModal.ButtonKind.Danger);
        };
        buttons.AddChild(scrap);
    }

    /// <summary>What can be done on the floor for this song: a pass, a rehearsal, a word on the talkback, another arrangement.</summary>
    private void FloorActions(PlayerDesk.PendingSession session, PlayerDesk.SessionCut cut, int side, VBoxContainer sheet) {
        var desk = PlayerDesk.Instance;
        int left = desk.StudioMinutesLeft(session), held = PlayerDesk.StudioFinishMinutesPerSide * session.Cuts.Count;
        const string OutOfTime = "Not enough room time left beyond what's held for finishing.";
        // The producer's suggestion after one member's part keeps falling apart: call a session player in, or don't.
        if (desk.StudioCallIn() is { } callIn) {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(PaperTheme.Highlighter, 0.30f), BorderColor = Rust,
                BorderWidthLeft = 4, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
            var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 6); panel.AddChild(box);
            StudioText(box, $"CALL IN A SESSION {PlayerDesk.RoleWord(callIn.Role).ToUpperInvariant()}?", 13, Rust, PaperTheme.SansBold);
            StudioText(box, callIn.Text, 15, Ink);
            var choices = StudioFlow(box);
            var call = Primary($"CALL {callIn.CandidateName.ToUpperInvariant()} IN  ·  ${callIn.Fee:N0}  ·  {callIn.ArrivalMinutes}m");
            call.Disabled = left < callIn.ArrivalMinutes + held;
            call.Pressed += () => { bool ok = desk.CallInSessionPlayer(callIn.MemberId, callIn.CandidateId, out string m); Say(m, ok); Refresh(); };
            choices.AddChild(call);
            var keep = Btn($"KEEP {callIn.MemberName.ToUpperInvariant()} ON IT");
            keep.Pressed += () => { bool ok = desk.DeclineCallIn(callIn.MemberId, out string m); Say(m, ok); Refresh(); };
            choices.AddChild(keep);
            sheet.AddChild(panel);
        }
        // The producer's suggestion when a singer's song sounds bare with nobody behind them.
        if (desk.StudioBackingCallIn() is { } backing) {
            var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(PaperTheme.Highlighter, 0.30f), BorderColor = Rust,
                BorderWidthLeft = 4, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 });
            var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 6); panel.AddChild(box);
            StudioText(box, "CALL IN BACKING SINGERS?", 13, Rust, PaperTheme.SansBold);
            StudioText(box, backing.Text, 15, Ink);
            var choices = StudioFlow(box);
            var call = Primary($"CALL {backing.GroupName.ToUpperInvariant()} IN  ·  ${backing.Fee:N0}  ·  {backing.ArrivalMinutes}m");
            call.Disabled = left < backing.ArrivalMinutes + held;
            call.Pressed += () => { bool ok = desk.CallInBackingSingers(out string m); Say(m, ok); Refresh(); };
            choices.AddChild(call);
            var keep = Btn("KEEP IT TO THE ACT'S OWN VOICES");
            keep.Pressed += () => { bool ok = desk.DeclineBackingSingers(out string m); Say(m, ok); Refresh(); };
            choices.AddChild(keep);
            sheet.AddChild(panel);
        }
        // Ideas from the room: who had it, who likes it, and a free choice to try it or pass.
        foreach (var idea in desk.StudioIdeas(side)) {
            var box = StudioNotice(sheet, $"AN IDEA FROM {idea.From.ToUpperInvariant()}");
            StudioText(box, idea.Text, 15, Ink);
            if (idea.Favors.Count + idea.Resists.Count > 0)
                StudioText(box, (idea.Favors.Count > 0 ? $"For it: {string.Join(", ", idea.Favors)}. " : "") + (idea.Resists.Count > 0 ? $"Against: {string.Join(", ", idea.Resists)}." : ""), 13, Heard);
            var choices = StudioFlow(box);
            string id = idea.Id;
            var tryIt = Primary("TRY IT");
            tryIt.Pressed += () => { bool ok = desk.TakeStudioIdea(side, id, out string m); Say(m, ok); Refresh(); };
            choices.AddChild(tryIt);
            var notNow = Btn("NOT NOW");
            notNow.Pressed += () => { bool ok = desk.PassOnStudioIdea(side, id, out string m); Say(m, ok); Refresh(); };
            choices.AddChild(notNow);
        }
        // Out of tape: buy a reel, or choose a take to record over (and confirm it).
        if (session.Work != null && PlayerDesk.StudioStockLeft(session) <= 0) {
            var box = StudioNotice(sheet, "OUT OF TAPE");
            StudioText(box, "Every reel on the quote is used. Buy another, or record over a take you don't need. A take recorded over is gone.", 15, Ink);
            var choices = StudioFlow(box);
            var buy = Primary($"BUY A REEL  ·  ${PlayerDesk.StudioReelPrice:N0}");
            buy.Pressed += () => { bool ok = desk.BuyStudioStock(out string m); Say(m, ok); Refresh(); };
            choices.AddChild(buy);
            for (int c = 0; c < session.Cuts.Count; c++) {
                var each = session.Cuts[c];
                for (int t = 0; t < each.Takes.Count; t++) {
                    if (!PlayerDesk.StudioTakeReusable(each, t)) continue;
                    int cutIndex = c, takeIndex = t;
                    var over = Btn($"RECORD OVER {SideLetter(c)} TAKE {each.Takes[t].Number}…");
                    over.Pressed += () => {
                        var confirm = PaperModal.Open(this, "RECORD OVER THIS TAKE?", 600);
                        confirm.AddText($"\"{each.Choice.Title}\", take {each.Takes[takeIndex].Number}: {each.Takes[takeIndex].ProducerNote} Recording over it frees tape for one pass. It cannot be kept afterwards.");
                        confirm.AddButton("KEEP IT", null);
                        confirm.AddButton("RECORD OVER IT", () => { bool ok = desk.ReuseStudioTape(cutIndex, takeIndex, out string m); Say(m, ok); Refresh(); }, PaperModal.ButtonKind.Danger);
                    };
                    choices.AddChild(over);
                }
            }
        }
        var work = StudioFlow(sheet);
        int passMinutes = PlayerDesk.StudioPassMinutes(cut), rehearsalMinutes = PlayerDesk.StudioRehearsalMinutesFor(cut);
        var pass = Primary($"CUT A TAKE  ·  {passMinutes}m");
        pass.Disabled = left < passMinutes + held;
        pass.TooltipText = pass.Disabled ? OutOfTime : cut.Structure == null ? "Roll tape on a full pass. Earlier takes stay safe."
            : $"A {StudioSongStructures.Length(cut.Structure.DurationSeconds)} song: count-in, the pass, rewind and a listen back. Earlier takes stay safe.";
        pass.Pressed += () => { bool ok = desk.CutStudioTake(side, out string m); if (ok) studioOpenTakes.Clear(); Say(m, ok); Refresh(); };
        work.AddChild(pass);
        // The part the last pass fell down in, as the producer heard it, gets a direct button.
        var lastTake = cut.Takes.Count > 0 ? cut.Takes[^1] : null;
        if (cut.Structure != null && lastTake?.TroubleSection >= 0 && !lastTake.FalseStart) {
            var flagged = cut.Structure.Sections[lastTake.TroubleSection].Kind;
            var target = Btn($"REHEARSE THE {StudioSongStructures.PartName(flagged).ToUpperInvariant()}  ·  {PlayerDesk.StudioPartRehearsalMinutes}m");
            target.Disabled = left < PlayerDesk.StudioPartRehearsalMinutes + held;
            target.TooltipText = target.Disabled ? OutOfTime : lastTake.Incomplete ? $"Work the {StudioSongStructures.PartName(flagged)} without tape: take {lastTake.Number} fell apart there."
                : $"Work the {StudioSongStructures.PartName(flagged)} without tape: the producer heard take {lastTake.Number} fall down there.";
            target.Pressed += () => { bool ok = desk.RehearseStudioPart(side, flagged, out string m); Say(m, ok); Refresh(); };
            work.AddChild(target);
        }
        var rehearse = Btn(cut.Structure == null ? $"REHEARSE  ·  {rehearsalMinutes}m" : "REHEARSE…");
        rehearse.Disabled = left < (cut.Structure == null ? rehearsalMinutes : Math.Min(rehearsalMinutes, PlayerDesk.StudioPartRehearsalMinutes)) + held;
        rehearse.TooltipText = rehearse.Disabled ? OutOfTime : "Run the song, or one part of it, without rolling tape. It tightens the band, but it is tiring.";
        rehearse.Pressed += () => {
            if (cut.Structure == null) { bool done = desk.RehearseStudio(side, out string said); Say(said, done); Refresh(); return; }
            var dialog = PaperModal.Open(this, "REHEARSE", 720);
            dialog.AddText($"Running the whole song tightens the band in general. Working one part fixes where it keeps falling down and helps what that part asks for. Either way the band tires. {left - held} minutes are free before finishing.");
            var song = Primary($"RUN THE WHOLE SONG  ·  {rehearsalMinutes}m");
            song.Disabled = left < rehearsalMinutes + held;
            song.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            song.Pressed += () => { dialog.Cancel(); bool ok = desk.RehearseStudio(side, out string m); Say(m, ok); Refresh(); };
            dialog.Body.AddChild(song);
            var partRow = new HFlowContainer(); partRow.AddThemeConstantOverride("h_separation", 8); partRow.AddThemeConstantOverride("v_separation", 8);
            foreach (StudioSectionKind part in cut.Structure.Parts) {
                StudioSectionKind chosen = part;
                int heard = cut.Takes.Count(t => t.TroubleSection >= 0 && cut.Structure.Sections[t.TroubleSection].Kind == part);
                var button = Btn($"THE {StudioSongStructures.PartName(part).ToUpperInvariant()}  ·  {PlayerDesk.StudioPartRehearsalMinutes}m" + (heard > 0 ? $"  (flagged {heard}\u00d7)" : ""));
                button.Disabled = left < PlayerDesk.StudioPartRehearsalMinutes + held;
                button.Pressed += () => { dialog.Cancel(); bool ok = desk.RehearseStudioPart(side, chosen, out string m); Say(m, ok); Refresh(); };
                partRow.AddChild(button);
            }
            dialog.Body.AddChild(partRow);
            dialog.AddButton("NEVER MIND", null);
        };
        work.AddChild(rehearse);

        var talkback = Btn("TALKBACK…");
        talkback.TooltipText = "Give the band one direction for the next pass.";
        talkback.Pressed += () => {
            var dialog = PaperModal.Open(this, "TALKBACK", 680);
            dialog.AddText($"One direction for the next pass at \"{cut.Choice.Title}\". It helps one thing and may cost another. About 10 minutes to talk it through and run the part.");
            dialog.AddText(cut.Direction == StudioDirection.None ? "No direction given yet." : "Current direction: " + PlayerDesk.DirectionName(cut.Direction) + ".");
            foreach (StudioDirection d in Enum.GetValues<StudioDirection>()) {
                if (d == cut.Direction) continue;
                StudioDirection chosen = d;
                string label = d == StudioDirection.None ? "LET THEM PLAY IT THEIR WAY" : (PlayerDesk.DirectionName(d) + PlayerDesk.DirectionPlace(d, cut.Structure)).ToUpperInvariant();
                dialog.AddButton(label + "  ·  10m", () => { bool ok = desk.DirectStudio(side, chosen, out string m); Say(m, ok); Refresh(); });
            }
            dialog.AddButton("NEVER MIND", null);
        };
        work.AddChild(talkback);
        if (session.RulesVersion >= 2) {
            var reshape = Btn("KEY, TEMPO & ENDS…");
            reshape.TooltipText = "Change the key, the tempo or how long the intro and ending run. Asking is free.";
            reshape.Pressed += () => {
                var dialog = PaperModal.Open(this, "THE SHAPE OF THE SONG", 820);
                foreach (StudioShapeChange change in desk.StudioShapeOptions(side)) {
                    StudioShapeChange chosen = change;
                    var heading = new Label { Text = PlayerDesk.ShapeName(change).ToUpperInvariant() };
                    heading.AddThemeFontOverride("font", PaperTheme.SansBold); heading.AddThemeColorOverride("font_color", Rust);
                    dialog.Body.AddChild(heading);
                    dialog.AddText(desk.StudioShapeProposal(side, change));
                    var go = Primary(PlayerDesk.ShapeName(change).ToUpperInvariant());
                    go.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                    go.Pressed += () => { dialog.Cancel(); bool ok = desk.ShapeStudio(side, chosen, out string m); Say(m, ok); Refresh(); };
                    dialog.Body.AddChild(go);
                }
                dialog.AddButton("LEAVE IT AS IT IS", null);
            };
            work.AddChild(reshape);
            var balance = Btn("ROOM BALANCE…");
            balance.TooltipText = "Move the microphones: favour the vocal or open up the band. It applies to the next passes.";
            balance.Pressed += () => {
                var dialog = PaperModal.Open(this, "ROOM BALANCE", 720);
                foreach (StudioBalance b in Enum.GetValues<StudioBalance>()) {
                    if (b == (cut.Work?.Balance ?? StudioBalance.Natural)) continue;
                    StudioBalance chosen = b;
                    dialog.AddText(desk.StudioBalanceProposal(side, b));
                    var go = Primary(PlayerDesk.BalanceName(b).ToUpperInvariant() + $"  ·  {PlayerDesk.StudioBalanceMinutes}m");
                    go.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                    go.Pressed += () => { dialog.Cancel(); bool ok = desk.BalanceStudio(side, chosen, out string m); Say(m, ok); Refresh(); };
                    dialog.Body.AddChild(go);
                }
                dialog.AddButton("NEVER MIND", null);
            };
            work.AddChild(balance);
        }

        var arrange = Btn("TRY ANOTHER ARRANGEMENT…");
        arrange.TooltipText = "Hear the producer's ideas for a different arrangement. Asking is free; preparing one takes time.";
        arrange.Pressed += () => {
            var dialog = PaperModal.Open(this, "ANOTHER ARRANGEMENT", 820);
            dialog.AddText($"Now prepared: {PlayerDesk.ArrangementName(cut.Arrangement)}.");
            foreach (StudioArrangement alternative in desk.StudioArrangementOptions(session)) {
                if (alternative == cut.Arrangement) continue;
                StudioArrangement chosen = alternative;
                var heading = new Label { Text = PlayerDesk.ArrangementName(alternative).ToUpperInvariant() };
                heading.AddThemeFontOverride("font", PaperTheme.SansBold);
                heading.AddThemeColorOverride("font_color", Rust);
                dialog.Body.AddChild(heading);
                dialog.AddText(desk.StudioArrangementProposal(side, alternative));
                var prepare = Primary("PREPARE: " + PlayerDesk.ArrangementName(alternative).ToUpperInvariant());
                prepare.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                prepare.Pressed += () => { dialog.Cancel(); bool ok = desk.ArrangeStudio(side, chosen, out string m); Say(m, ok); Refresh(); };
                dialog.Body.AddChild(prepare);
            }
            if (session.RulesVersion >= 2) {
                var hires = new HFlowContainer(); hires.AddThemeConstantOverride("h_separation", 8);
                if (desk.StudioHornOffer() is { } horn) {
                    var b = Btn($"HIRE A HORN: {horn.Name.ToUpperInvariant()}  ·  ${horn.Fee:N0}  ·  {PlayerDesk.StudioHornArrivalMinutes}m");
                    b.TooltipText = horn.Text;
                    b.Pressed += () => { dialog.Cancel(); bool ok = desk.HireStudioCrew(StudioHireKind.Horn, out string m); Say(m, ok); Refresh(); };
                    hires.AddChild(b);
                }
                if (desk.StudioArrangerOffer() is { } arranger) {
                    var b = Btn($"HIRE AN ARRANGER: {arranger.Name.ToUpperInvariant()}  ·  ${arranger.Fee:N0}");
                    b.TooltipText = arranger.Text;
                    b.Pressed += () => { dialog.Cancel(); bool ok = desk.HireStudioCrew(StudioHireKind.Arranger, out string m); Say(m, ok); Refresh(); };
                    hires.AddChild(b);
                }
                if (hires.GetChildCount() > 0) { dialog.AddText("Help from town for a part the act can't play or write:"); dialog.Body.AddChild(hires); }
            }
            dialog.AddButton("KEEP WORKING AS WE ARE", null);
        };
        work.AddChild(arrange);
    }

    /// <summary>One line of the take sheet: the take, what was tried, what people said about it, and keep.</summary>
    private void TakeRow(PlayerDesk.SessionCut cut, int side, int index, bool incremental, bool openByDefault, VBoxContainer sheet) {
        PlayerDesk.SessionTake take = cut.Takes[index];
        bool kept = index == cut.KeptTake;
        string key = take.TakeId ?? $"{side}:{index}";
        bool open = openByDefault || studioOpenTakes.Contains(key);

        var row = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
            BgColor = kept ? new Color(PaperTheme.Highlighter, 0.35f) : new Color(0, 0, 0, 0),
            BorderColor = new Color(WorkOrder.FormInk, 0.35f), BorderWidthBottom = 1,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6
        });
        sheet.AddChild(row);
        var line = new HBoxContainer(); line.AddThemeConstantOverride("separation", 14); row.AddChild(line);

        var number = new VBoxContainer { CustomMinimumSize = new Vector2(96, 0) };
        number.AddThemeConstantOverride("separation", 0);
        StudioText(number, $"TAKE {take.Number}", 18, take.Incomplete ? Heard : WorkOrder.FormInk, PaperTheme.TypedBold);
        if (take.Incomplete) StudioText(number, take.FalseStart ? "false start" : "broke down", 12, PaperTheme.Oxblood, PaperTheme.SansBold);
        if (take.SessionPersonIds?.Count > 0) {
            var on = PlayerDesk.Instance.Session?.Hires.Where(h => take.SessionPersonIds.Contains(h.PersonId)).ToList() ?? new();
            if (on.Any(h => h.Kind == StudioHireKind.HousePlayer)) StudioText(number, "with house band", 12, Heard);
            if (on.Any(h => h.Kind == StudioHireKind.Replacement)) StudioText(number, "with session player", 12, Heard);
            if (on.Any(h => h.Kind == StudioHireKind.BackingVoice)) StudioText(number, "with backing vocals", 12, Heard);
        }
        if (incremental) {
            if (take.Arrangement != StudioArrangement.Live) StudioText(number, PlayerDesk.ArrangementName(take.Arrangement) + (take.ArrangementRevision > 1 ? $" (rev. {take.ArrangementRevision})" : ""), 12, Heard);
            if (take.Direction != StudioDirection.None) StudioText(number, PlayerDesk.DirectionName(take.Direction), 12, Heard);
        }
        line.AddChild(number);

        var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        notes.AddThemeConstantOverride("separation", 2);
        line.AddChild(notes);
        string With(string note, string detail) => detail == null ? note : note + " " + detail;
        var all = new[] { "Engineer: " + With(take.EngineerNote, take.EngineerDetail), "Performer: " + take.PerformerNote, take.BandNote == null ? null : "Band: " + take.BandNote, "Producer: " + With(take.ProducerNote, take.ProducerDetail) }
            .Where(n => n != null).ToArray();
        foreach (string note in open ? all : all.Where(n => n.StartsWith("Producer: ", StringComparison.Ordinal)))
            StudioText(notes, note, 15, Heard);
        if (take.Work != null) {
            var setup = PlayerDesk.StudioTakeSetup(PlayerDesk.Instance.Session, take).Where(l => l != take.Work.IncidentNote).ToList();
            if (setup.Count > 0) StudioText(notes, string.Join("  ·  ", setup), 13, Heard);
            if (take.Work.IncidentNote != null) StudioText(notes, take.Work.IncidentNote, 14, Rust);
        }
        if (!openByDefault) {
            var more = new LinkButton { Text = open ? "fewer notes" : "everyone's notes", Underline = LinkButton.UnderlineMode.OnHover };
            more.AddThemeColorOverride("font_color", Rust);
            more.AddThemeFontSizeOverride("font_size", 13);
            more.Pressed += () => { if (!studioOpenTakes.Add(key)) studioOpenTakes.Remove(key); RefreshKeepingScroll(); };
            notes.AddChild(more);
        }

        if (incremental && take.Work != null && !take.Work.Erased && PlayerDesk.Instance.Session is { } here) {
            var desk = PlayerDesk.Instance;
            bool vocal = desk.StudioVocalOverdubBlocked(side, index) == null;
            var parts = desk.StudioOverdubOptions(side, index).Where(o => o.Blocked == null).ToList();
            var partners = Enumerable.Range(0, cut.Takes.Count).Where(j => desk.StudioSpliceBlocked(side, index, j) == null).ToList();
            if (vocal || parts.Count > 0 || partners.Count > 0) {
                var more = Btn("TAPE…");
                more.TooltipText = "What this take's tape allows: a new vocal, an added part, or a splice.";
                more.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                more.Pressed += () => {
                    var dialog = PaperModal.Open(this, $"TAKE {take.Number}: THE TAPE", 720);
                    dialog.AddText("Each of these makes a new take. This one stays as it is.");
                    if (vocal) dialog.AddButton("SING THE VOCAL AGAIN OVER THIS BACKING", () => { bool ok = desk.OverdubStudioVocal(side, index, out string m); Say(m, ok); Refresh(); });
                    foreach (var p in parts) { var part = p.Part; dialog.AddButton($"ADD {PlayerDesk.OverdubPartName(part).ToUpperInvariant()} ON THE SPARE TRACK", () => { bool ok = desk.OverdubStudioPart(side, index, part, out string m); Say(m, ok); Refresh(); }); }
                    foreach (int j in partners) { int other = j; dialog.AddButton($"SPLICE INTO TAKE {cut.Takes[other].Number} AT {cut.Structure.Sections[desk.StudioSplicePoint(cut, index, other)].Label.ToUpperInvariant()}  ·  {PlayerDesk.StudioSpliceMinutes}m", () => { bool ok = desk.SpliceStudio(side, index, other, out string m); Say(m, ok); Refresh(); }); }
                    dialog.AddButton("NEVER MIND", null);
                };
                line.AddChild(more);
            }
        }
        if (take.Work?.Erased == true) line.AddChild(new RubberStamp().Set("Recorded over", RubberStamp.Blue, 0.08f, 15));
        else if (kept) line.AddChild(new RubberStamp().Set("Kept", RubberStamp.Red, -0.12f, 18));
        else if (take.Incomplete) line.AddChild(new RubberStamp().Set(take.FalseStart ? "False start" : "Incomplete", RubberStamp.Blue, 0.08f, 15));
        else {
            var keep = Btn($"KEEP TAKE {take.Number}");
            keep.CustomMinimumSize = new Vector2(150, 38);
            keep.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            int cutIndex = side, takeIndex = index;
            keep.Pressed += () => { PlayerDesk.Instance.KeepTake(cutIndex, takeIndex); Refresh(); };
            line.AddChild(keep);
        }
    }
}
