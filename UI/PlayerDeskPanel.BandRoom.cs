using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// THE BAND ROOM tab (SimTools/BandMemberSimulationDirective.md Part B): who's waiting to see you, the scene
/// when you see them, and a card for every member of every act. Reads and calls <see cref="PlayerDesk"/> only.
/// The scene renders exactly like a Rolodex call -- same transcript, same option buttons, same voice colours --
/// because it IS the same grammar.
///
/// On the page, the people waiting are appointment slips with a publicity photo clipped on; each act is a manila
/// personnel file with one card per member, and the act-level bookings (the road, a writing session, a session
/// player) are a printed booking order on the label's own letterhead, as the Office and Distribution pages are.
/// </summary>
public partial class PlayerDeskPanel {
	// Band Room page state survives Refresh() like the other page state.
	private string writingFirstId, writingSecondId;
	private readonly Dictionary<string, string> sessionPlayerPick = new();

	private void PageBandRoom() {
		PlayerDesk desk = PlayerDesk.Instance;
		PlayerDesk.BandRoomLive live = desk.ActiveVisit;
		if (live != null) { RenderBandVisit(desk, live); return; }

		Heading("THE BAND ROOM");
		if (!BandLife.RosterChurn) {
			Body("Your acts keep their troubles to themselves in this build (lineup churn is switched off).");
			return;
		}
		Body("Who's waiting to see you, and how everyone in your acts is getting on. What you do in here changes " +
			"who stays, who writes, and who walks.");

		var visits = desk.PendingVisits();
		Heading(visits.Count == 0 ? "NOBODY'S WAITING" : $"WAITING TO SEE YOU ({visits.Count})");
		if (visits.Count == 0) Body("The outer office is empty. Enjoy it.");
		foreach (BandVisit visit in visits) content.AddChild(VisitSlip(desk, visit));

		foreach (SimulatedArtist act in desk.Roster.ToList()) RenderActBandCards(desk, act);
	}

	/// <summary>A member's publicity photo for the band pages: a halftone of one figure in the act's genre.</summary>
	private static PortraitPhoto MemberPhoto(SimulatedArtist act, Musician member, Vector2 size) {
		if (act == null || member == null) return new PortraitPhoto().Set(null, size);
		PortraitFigure figure = Portraits.FigureFor(act.primaryGenre, !member.isMale, member.personId ?? member.FullName, 0);
		return new PortraitPhoto().Set(Portraits.ForFigures("member:" + (member.personId ?? member.FullName), new List<PortraitFigure> { figure }), size, member.personId);
	}

	/// <summary>An appointment slip: the member's photo, who and what in the typewriter, a stamp when it cannot wait, and
	/// the button that sends them in.</summary>
	private Control VisitSlip(PlayerDesk desk, BandVisit visit) {
		SimulatedArtist act = ArtistManager.Instance?.GetArtist(visit.artistId);
		Musician member = act == null ? null : act.members.FirstOrDefault(x => x.personId == visit.personId)
			?? PersonPool.Get(visit.personId)?.person ?? ArtistManager.Instance?.GetMusician(visit.personId);
		bool urgent = visit.kind is BandVisitKind.Death or BandVisitKind.Ultimatum or BandVisitKind.Departure;

		var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var paper = PaperStyleBox.Sheet(new Color("f3ebd0"), 18, 10, 8);
		paper.HeaderRule = 46f;
		card.AddThemeStyleboxOverride("panel", paper);
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 16);
		card.AddChild(row);
		row.AddChild(MemberPhoto(act, member, new Vector2(70, 88)));

		var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		text.AddThemeConstantOverride("separation", 4);
		row.AddChild(text);
		var name = new Label { Text = $"{act?.stageName ?? "?"}", ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(0, 30) };
		name.AddThemeFontOverride("font", PaperTheme.Elite);
		name.AddThemeFontSizeOverride("font_size", 22);
		name.AddThemeColorOverride("font_color", Ink);
		text.AddChild(name);
		var headline = new Label { Text = VisitHeadline(desk, visit), AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		headline.AddThemeFontOverride("font", PaperTheme.Serif);
		headline.AddThemeFontSizeOverride("font_size", 17);
		headline.AddThemeColorOverride("font_color", urgent ? Rust : Ink);
		text.AddChild(headline);

		if (urgent && visit.kind != BandVisitKind.Death)
			row.AddChild(new RubberStamp().Set(visit.kind == BandVisitKind.Ultimatum ? "Ultimatum" : "Walked out", RubberStamp.Red, -0.12f, 14));
		string captured = visit.visitId;
		var see = Btn(visit.kind == BandVisitKind.Death ? "TAKE THE CALL" : visit.kind is BandVisitKind.AffairSecret or BandVisitKind.SubstanceRead ? "THINK IT OVER" : "SEE THEM");
		see.CustomMinimumSize = new Vector2(160, 38);
		see.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		see.Pressed += () => { desk.OpenVisit(captured, out string msg); Say(msg); Refresh(); };
		row.AddChild(see);
		return card;
	}

	private static string VisitHeadline(PlayerDesk desk, BandVisit v) {
		SimulatedArtist a = ArtistManager.Instance?.GetArtist(v.artistId);
		Musician m = a?.members.FirstOrDefault(x => x.personId == v.personId) ?? PersonPool.Get(v.personId)?.person ?? ArtistManager.Instance?.GetMusician(v.personId);
		string name = m?.firstName ?? "Someone";
		return v.kind switch {
			BandVisitKind.Grievance => $"{name} wants a word about {PlayerDesk.CauseWord(v.cause)}.",
			BandVisitKind.Ultimatum => $"{name} has an ultimatum about {PlayerDesk.CauseWord(v.cause)}.",
			BandVisitKind.Request => v.request switch {
				BandRequest.SongOnRecord => $"{name} wants a song on the next record.",
				BandRequest.SoloSingle => $"{name} wants to cut one under their own name.",
				BandRequest.Advance => $"{name} needs money.",
				BandRequest.OffTheRoad => $"{name} wants off the road.",
				_ => $"{name} has a request."
			},
			BandVisitKind.FirstHit => $"There's a hit, and {name} wants to talk about the credits.",
			BandVisitKind.LifeEvent => v.lifeEvent switch {
				"drafted" => $"{name} has been drafted.", "marriage" => $"{name} got married.",
				"studio-only" => $"{name} is coming off the road.", _ => $"News about {name}."
			},
			BandVisitKind.Death => $"The phone. It's about {name}.",
			BandVisitKind.AffairSecret => $"You've heard something about {name}.",
			BandVisitKind.AffairDiscovered => $"{name} found out.",
			BandVisitKind.Departure => $"{name} is gone. Who replaces them?",
			BandVisitKind.SubstanceRead => $"Something isn't right with {name}.",
			_ => $"{name} came by."
		};
	}

	private void RenderBandVisit(PlayerDesk desk, PlayerDesk.BandRoomLive live) {
		BandRoomContext c = live.ctx;
		// Who is in the room: the photo, the name typed, the act on a rubric under it.
		var who = new HBoxContainer();
		who.AddThemeConstantOverride("separation", 16);
		who.AddChild(MemberPhoto(c.artist, c.member, new Vector2(78, 98)));
		var names = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
		names.AddThemeConstantOverride("separation", 2);
		who.AddChild(names);
		var memberName = new Label { Text = c.member.FullName };
		memberName.AddThemeFontOverride("font", PaperTheme.Elite);
		memberName.AddThemeFontSizeOverride("font_size", 30);
		memberName.AddThemeColorOverride("font_color", Ink);
		names.AddChild(memberName);
		var actLine = new Label { Text = $"{c.artist.stageName.ToUpperInvariant()}  •  {KindWord(c.constitution).ToUpperInvariant()}" };
		actLine.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		actLine.AddThemeFontSizeOverride("font_size", 14);
		actLine.AddThemeColorOverride("font_color", Rust);
		names.AddChild(actLine);
		content.AddChild(who);
		content.AddChild(new ColorRect { Color = new Color(Ink, 0.8f), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });

		RenderBandTranscript(live);
		switch (live.stage) {
			case CallStage.Open:
				Heading("WHAT DO YOU DO");
				foreach (PlayerDesk.BandOption opt in desk.VisitOptions(live))
					RenderBandOption(opt, () => { desk.ChooseBandVerb(live, opt, out string msg); Say(msg); Refresh(); });
				var later = Btn("NOT NOW");
				later.Pressed += () => { desk.CloseVisit(); Refresh(); };
				content.AddChild(later);
				break;
			case CallStage.Pushback:
				Heading("HOW DO YOU ANSWER THAT");
				foreach (PlayerDesk.BandOption opt in desk.BandCounterOptions(live))
					RenderBandOption(opt, () => { desk.PlayBandCounter(live, opt, out string msg); Say(msg); Refresh(); });
				break;
			default:
				var done = Btn("BACK TO THE BAND ROOM");
				done.CustomMinimumSize = new Vector2(260, 40);
				done.Pressed += () => { desk.CloseVisit(); Refresh(); };
				content.AddChild(done);
				break;
		}
	}

	private void RenderBandTranscript(PlayerDesk.BandRoomLive live) {
		var call = new RolodexCall();
		call.transcript.AddRange(live.transcript);
		RenderTranscript(call);
	}

	private void RenderBandOption(PlayerDesk.BandOption opt, Action onPressed) =>
		RenderOption(new CallOption {
			label = opt.label + (opt.cash > 0f && !opt.label.Contains('$') ? $"  (${opt.cash:N0})" : ""),
			subLabel = opt.subLabel, voice = opt.voice, isBluff = opt.isBluff, enabled = opt.enabled, disabledReason = opt.disabledReason
		}, onPressed);

	/// <summary>The act's personnel file (§5.5): every member's read, grudges, partner, stage and life -- in words, never
	/// numbers -- then the booking order for the act-level work the player books: residency, tour, a writing session, a
	/// session player, a label cut-in.</summary>
	private void RenderActBandCards(PlayerDesk desk, SimulatedArtist act) {
		string morale = act.morale > 0.08f ? "in good spirits" : act.morale < -0.10f ? "low" : "steady";
		LineupConstitution kind = BandLifeService.ConstitutionOf(act);

		// The folder: manila, a coffee ring on some of them, the act's name typed on the tab.
		var folder = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		folder.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("dcc088"), 22, 16, 12, new Color("70552c"))
			.Decorated(clip: false, ring: Portraits.Hash(act.artistId ?? act.stageName) % 3u == 0u, seed: (int)(Portraits.Hash(act.artistId ?? act.stageName) % 97u)));
		var file = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		file.AddThemeConstantOverride("separation", 8);
		folder.AddChild(file);
		content.AddChild(folder);

		var head = new HBoxContainer();
		head.AddThemeConstantOverride("separation", 14);
		file.AddChild(head);
		var title = new Label { Text = act.stageName, SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
		title.AddThemeFontOverride("font", PaperTheme.Elite);
		title.AddThemeFontSizeOverride("font_size", 30);
		title.AddThemeColorOverride("font_color", Ink);
		head.AddChild(title);
		if (act.morale > 0.08f) head.AddChild(new RubberStamp().Set("Good spirits", new Color("2f6b2a"), -0.07f, 13));
		else if (act.morale < -0.10f) head.AddChild(new RubberStamp().Set("Low spirits", RubberStamp.Red, 0.06f, 13));
		var rubric = new Label {
			Text = $"{KindWord(kind).ToUpperInvariant()}  •  SPIRITS {morale.ToUpperInvariant()}" +
				(act.alumni?.Count > 0 ? $"  •  FORMERLY WITH THEM: {string.Join(", ", act.alumni.TakeLast(3).Select(r => r.name)).ToUpperInvariant()}" : ""),
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		rubric.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		rubric.AddThemeFontSizeOverride("font_size", 13);
		rubric.AddThemeColorOverride("font_color", Rust);
		file.AddChild(rubric);
		file.AddChild(new ColorRect { Color = new Color(Rust, 0.5f), CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore });

		foreach (PlayerDesk.MemberCard card in desk.MemberCards(act)) file.AddChild(MemberRow(act, card));

		PlayerDesk.RoadBooking booking = desk.RoadBookingFor(act.artistId);
		var notes = new List<string>();
		if (booking != null && (booking.WeeksRemaining > 0 || booking.TimeOffUntilWeek >= 0))
			notes.Add($"This year: {booking.ResidencyWeeks} weeks' residency, {booking.TourWeeks} on the road, {booking.WeeksRemaining} still to play" +
				(booking.TimeOffUntilWeek >= (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) ? " — on a break right now." : "."));
		if (desk.CutInSongsLeft(act.artistId) > 0) notes.Add($"The label is cut in on the next {desk.CutInSongsLeft(act.artistId)} original(s).");
		string promised = desk.PromisedCreditFor(act.artistId);
		if (promised != null) notes.Add($"You promised {act.members.FirstOrDefault(m => m.personId == promised)?.firstName ?? "someone"} a song on the next record.");
		PlayerDesk.SessionPlayerBooking sp = desk.SessionPlayerFor(act.artistId);
		if (sp != null) notes.Add($"{sp.Name} sits in at the next session (${sp.Fee:N0}).");
		foreach (string note in notes) {
			var line = new Label { Text = "▸ " + note, AutowrapMode = TextServer.AutowrapMode.WordSmart };
			line.AddThemeFontOverride("font", PaperTheme.Typed);
			line.AddThemeFontSizeOverride("font_size", 15);
			line.AddThemeColorOverride("font_color", Ink);
			file.AddChild(line);
		}

		RenderBookingOrder(desk, act);
	}

	/// <summary>One member of the act, on an index card clipped into the folder: photo, name typed, the read in words,
	/// and a stamp when they have put the label on notice.</summary>
	private Control MemberRow(SimulatedArtist act, PlayerDesk.MemberCard card) {
		bool notice = card.Member.departureStage == DepartureStage.Ultimatum;
		var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var paper = PaperStyleBox.Sheet(new Color("f3ebd0"), 14, 10, 6);
		paper.Burn = 0.7f;
		panel.AddThemeStyleboxOverride("panel", paper);
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 14);
		panel.AddChild(row);
		row.AddChild(MemberPhoto(act, card.Member, new Vector2(64, 80)));

		var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 2);
		row.AddChild(text);
		Label Line(string value, Font font, int size, Color colour) {
			var label = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeFontOverride("font", font);
			label.AddThemeFontSizeOverride("font_size", size);
			label.AddThemeColorOverride("font_color", colour);
			return label;
		}
		text.AddChild(Line(card.Line.Replace(" -- ", " — "), PaperTheme.Elite, 19, Ink));
		text.AddChild(Line(card.Personality, PaperTheme.SerifItalic, 16, new Color("4a3f2f")));
		text.AddChild(Line(card.Stage + TrustWord(card.Trust), PaperTheme.Serif, 16, notice ? Rust : Ink));
		var more = new List<string>();
		if (card.NotSpeakingTo != null) more.Add(card.NotSpeakingTo);
		if (card.Partner != null) more.Add(card.Partner.Trim());
		if (card.Life != null) more.Add(card.Life.Substring(0, 1).ToUpperInvariant() + card.Life[1..]);
		foreach (string extra in more) text.AddChild(Line(extra, PaperTheme.Serif, 15, PaperTheme.Rust));

		if (notice) row.AddChild(new RubberStamp().Set("On notice", RubberStamp.Red, -0.1f, 13));
		return panel;
	}

	/// <summary>The act's bookings, as a printed order on the label's letterhead: the road, then the studio.</summary>
	private void RenderBookingOrder(PlayerDesk desk, SimulatedArtist act) {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		WorkOrder.Form form = HouseForm(desk.Label, $"Booking order — {act.stageName}", WorkOrder.Serial($"band:{act.artistId}:{week / 13}"));
		content.AddChild(form.Sheet);
		VBoxContainer order = form.Body;
		SimulatedArtist captured = act;

		order.AddChild(WorkOrder.Section("The road"));
		var road = new HBoxContainer();
		road.AddThemeConstantOverride("separation", 10);
		var residency = Btn("BOOK A RESIDENCY (8 wks)");
		residency.Pressed += () => Act(() => { desk.BookRoad(captured, 8, 0, out string m); Say(m); return true; });
		road.AddChild(residency);
		var tour = Btn("BOOK A TOUR (4 wks)");
		tour.Pressed += () => Act(() => { desk.BookRoad(captured, 0, 4, out string m); Say(m); return true; });
		road.AddChild(tour);
		var cut = Btn("CUT THE LABEL IN");
		cut.Disabled = desk.CutInSongsLeft(act.artistId) > 0;
		cut.Pressed += () => Act(() => { desk.CutInOnSongs(captured, out string m); Say(m); return true; });
		road.AddChild(cut);
		order.AddChild(road);

		var members = act.members.Where(m => m.isActive).ToList();
		if (members.Count == 0) return;

		// A writing session: two members, or one member and a staff writer.
		order.AddChild(WorkOrder.Section("The studio"));
		var write = new HBoxContainer();
		write.AddThemeConstantOverride("separation", 12);
		OptionButton first = Option(), second = Option();
		first.AddItem("(writer)");
		second.AddItem("a staff writer");
		foreach (Musician m in members) { first.AddItem(m.FullName); second.AddItem(m.FullName); }
		first.Selected = Math.Max(0, members.FindIndex(m => m.personId == writingFirstId) + 1);
		second.Selected = Math.Max(0, members.FindIndex(m => m.personId == writingSecondId) + 1);
		first.ItemSelected += i => writingFirstId = i <= 0 ? null : members[(int)i - 1].personId;
		second.ItemSelected += i => writingSecondId = i <= 0 ? null : members[(int)i - 1].personId;
		write.AddChild(WorkOrder.Field("Writing session — writer", first, 200f));
		write.AddChild(WorkOrder.Field("With", second, 200f));
		var book = Btn($"BOOK IT ({PlayerDesk.WritingSessionHours}h)");
		book.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		book.Pressed += () => Act(() => {
			Musician a = members.FirstOrDefault(m => m.personId == writingFirstId);
			Musician b = members.FirstOrDefault(m => m.personId == writingSecondId);
			desk.BookWritingSession(captured, a, b, b == null, out string msg);
			Say(msg);
			return true;
		});
		write.AddChild(book);
		order.AddChild(write);

		// A session player in place of one member, at the next date in the studio.
		if (members.Count > 1) {
			var session = new HBoxContainer();
			session.AddThemeConstantOverride("separation", 12);
			OptionButton who = Option();
			foreach (Musician m in members) who.AddItem($"{m.firstName} ({PlayerDesk.RoleWord(m.primaryRole)})");
			sessionPlayerPick.TryGetValue(act.artistId, out string picked);
			who.Selected = Math.Max(0, members.FindIndex(m => m.personId == picked));
			who.ItemSelected += i => sessionPlayerPick[captured.artistId] = members[(int)i].personId;
			session.AddChild(WorkOrder.Field("Session player in place of", who, 260f));
			var hire = Btn("HIRE FOR THE NEXT DATE");
			hire.SizeFlagsVertical = SizeFlags.ShrinkEnd;
			hire.Pressed += () => Act(() => {
				sessionPlayerPick.TryGetValue(captured.artistId, out string id);
				Musician replaced = members.FirstOrDefault(m => m.personId == id) ?? members[0];
				desk.BookSessionPlayer(captured, replaced, out string msg);
				Say(msg);
				return true;
			});
			session.AddChild(hire);
			order.AddChild(session);
		}
	}

	private static string KindWord(LineupConstitution kind) => kind switch {
		LineupConstitution.LeaderAndSidemen => "A leader and sidemen",
		LineupConstitution.NameOwned => "The manager owns the name",
		LineupConstitution.Duo => "A duo",
		LineupConstitution.Solo => "A solo act",
		_ => "A band"
	};

	private static string TrustWord(float trust) =>
		trust > 0.70f ? "  Trusts the label." : trust < 0.30f ? "  Doesn't trust the label." : "";
}
