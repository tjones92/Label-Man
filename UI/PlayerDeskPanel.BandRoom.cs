using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// THE BAND ROOM tab (SimTools/BandMemberSimulationDirective.md Part B): who's waiting to see you, the scene
/// when you see them, and a card for every member of every act. Reads and calls <see cref="PlayerDesk"/> only.
/// The scene renders exactly like a Rolodex call -- same transcript, same option buttons, same voice colours --
/// because it IS the same grammar.
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
		foreach (BandVisit visit in visits) {
			SimulatedArtist act = ArtistManager.Instance?.GetArtist(visit.artistId);
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 12);
			var text = new Label {
				SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart,
				Text = $"{act?.stageName ?? "?"} — {VisitHeadline(desk, visit)}"
			};
			text.AddThemeColorOverride("font_color", visit.kind is BandVisitKind.Death or BandVisitKind.Ultimatum or BandVisitKind.Departure ? Rust : Ink);
			row.AddChild(text);
			string captured = visit.visitId;
			var see = Btn(visit.kind == BandVisitKind.Death ? "TAKE THE CALL" : visit.kind is BandVisitKind.AffairSecret or BandVisitKind.SubstanceRead ? "THINK IT OVER" : "SEE THEM");
			see.CustomMinimumSize = new Vector2(160, 38);
			see.Pressed += () => { desk.OpenVisit(captured, out string msg); Say(msg); Refresh(); };
			row.AddChild(see);
			content.AddChild(row);
		}

		foreach (SimulatedArtist act in desk.Roster.ToList()) RenderActBandCards(desk, act);
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
		Heading($"{c.artist.stageName.ToUpperInvariant()} — {c.member.FullName.ToUpperInvariant()}");
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

	/// <summary>The roster card (§5.5): every member's read, grudges, partner, stage and life -- in words, never
	/// numbers -- plus the act-level work the player books: residency, tour, a writing session, a session
	/// player, a label cut-in.</summary>
	private void RenderActBandCards(PlayerDesk desk, SimulatedArtist act) {
		Heading(act.stageName.ToUpperInvariant());
		string morale = act.morale > 0.08f ? "in good spirits" : act.morale < -0.10f ? "low" : "steady";
		LineupConstitution kind = BandLifeService.ConstitutionOf(act);
		Body($"{KindWord(kind)}  •  spirits {morale}" +
			(act.alumni?.Count > 0 ? $"  •  formerly with them: {string.Join(", ", act.alumni.TakeLast(3).Select(r => r.name))}" : ""));
		foreach (PlayerDesk.MemberCard card in desk.MemberCards(act)) {
			var lines = new List<string> { card.Line, "    " + card.Personality, "    " + card.Stage + TrustWord(card.Trust) };
			if (card.NotSpeakingTo != null) lines.Add("    " + card.NotSpeakingTo);
			if (card.Partner != null) lines.Add("    " + card.Partner);
			if (card.Life != null) lines.Add("    " + card.Life.Substring(0, 1).ToUpperInvariant() + card.Life[1..]);
			var label = new Label { Text = string.Join("\n", lines), AutowrapMode = TextServer.AutowrapMode.WordSmart };
			label.AddThemeFontSizeOverride("font_size", 15);
			label.AddThemeColorOverride("font_color", card.Member.departureStage == DepartureStage.Ultimatum ? Rust : Ink);
			content.AddChild(label);
		}

		PlayerDesk.RoadBooking booking = desk.RoadBookingFor(act.artistId);
		if (booking != null && (booking.WeeksRemaining > 0 || booking.TimeOffUntilWeek >= 0))
			Body($"    This year: {booking.ResidencyWeeks} weeks' residency, {booking.TourWeeks} on the road, {booking.WeeksRemaining} still to play" +
				(booking.TimeOffUntilWeek >= (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) ? " — on a break right now." : "."));
		if (desk.CutInSongsLeft(act.artistId) > 0) Body($"    The label is cut in on the next {desk.CutInSongsLeft(act.artistId)} original(s).");
		string promised = desk.PromisedCreditFor(act.artistId);
		if (promised != null) Body($"    You promised {act.members.FirstOrDefault(m => m.personId == promised)?.firstName ?? "someone"} a song on the next record.");
		PlayerDesk.SessionPlayerBooking sp = desk.SessionPlayerFor(act.artistId);
		if (sp != null) Body($"    {sp.Name} sits in at the next session (${sp.Fee:N0}).");

		var actions = new HBoxContainer();
		actions.AddThemeConstantOverride("separation", 10);
		SimulatedArtist captured = act;
		var residency = Btn("BOOK A RESIDENCY (8 wks)");
		residency.Pressed += () => Act(() => { desk.BookRoad(captured, 8, 0, out string m); Say(m); return true; });
		actions.AddChild(residency);
		var tour = Btn("BOOK A TOUR (4 wks)");
		tour.Pressed += () => Act(() => { desk.BookRoad(captured, 0, 4, out string m); Say(m); return true; });
		actions.AddChild(tour);
		var cut = Btn("CUT THE LABEL IN");
		cut.Disabled = desk.CutInSongsLeft(act.artistId) > 0;
		cut.Pressed += () => Act(() => { desk.CutInOnSongs(captured, out string m); Say(m); return true; });
		actions.AddChild(cut);
		content.AddChild(actions);

		var members = act.members.Where(m => m.isActive).ToList();
		if (members.Count == 0) return;
		// A writing session: two members, or one member and a staff writer.
		var write = new HBoxContainer();
		write.AddThemeConstantOverride("separation", 8);
		write.AddChild(FormLabel("Writing session:"));
		OptionButton first = Option(), second = Option();
		first.AddItem("(writer)");
		second.AddItem("a staff writer");
		foreach (Musician m in members) { first.AddItem(m.FullName); second.AddItem(m.FullName); }
		first.Selected = Math.Max(0, members.FindIndex(m => m.personId == writingFirstId) + 1);
		second.Selected = Math.Max(0, members.FindIndex(m => m.personId == writingSecondId) + 1);
		first.ItemSelected += i => writingFirstId = i <= 0 ? null : members[(int)i - 1].personId;
		second.ItemSelected += i => writingSecondId = i <= 0 ? null : members[(int)i - 1].personId;
		write.AddChild(first);
		write.AddChild(new Label { Text = "with" });
		write.AddChild(second);
		var book = Btn($"BOOK IT ({PlayerDesk.WritingSessionHours}h)");
		book.Pressed += () => Act(() => {
			Musician a = members.FirstOrDefault(m => m.personId == writingFirstId);
			Musician b = members.FirstOrDefault(m => m.personId == writingSecondId);
			desk.BookWritingSession(captured, a, b, b == null, out string msg);
			Say(msg);
			return true;
		});
		write.AddChild(book);
		content.AddChild(write);

		// A session player in place of one member, at the next date in the studio.
		if (members.Count > 1) {
			var session = new HBoxContainer();
			session.AddThemeConstantOverride("separation", 8);
			session.AddChild(FormLabel("Session player in place of:"));
			OptionButton who = Option();
			foreach (Musician m in members) who.AddItem($"{m.firstName} ({PlayerDesk.RoleWord(m.primaryRole)})");
			sessionPlayerPick.TryGetValue(act.artistId, out string picked);
			who.Selected = Math.Max(0, members.FindIndex(m => m.personId == picked));
			who.ItemSelected += i => sessionPlayerPick[captured.artistId] = members[(int)i].personId;
			session.AddChild(who);
			var hire = Btn("HIRE FOR THE NEXT DATE");
			hire.Pressed += () => Act(() => {
				sessionPlayerPick.TryGetValue(captured.artistId, out string id);
				Musician replaced = members.FirstOrDefault(m => m.personId == id) ?? members[0];
				desk.BookSessionPlayer(captured, replaced, out string msg);
				Say(msg);
				return true;
			});
			session.AddChild(hire);
			content.AddChild(session);
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
