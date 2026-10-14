using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The ROLODEX tab. The book is divided by what a person does for you (disc jockeys, managers, publishers, the pressing
/// plant, your distributor, shops and operators), one tab each, and every tab is a <see cref="RolodexDeck"/>: a stack of
/// index cards the mouse wheel flips through. Only the disc jockey cards can be rung up; the others read off the real
/// plant orders, distribution deal, stops and contracts the desk already runs (see <see cref="RolodexDirectory"/>).
/// </summary>
public partial class PlayerDeskPanel {
	private RolodexRole rolodexRole = RolodexRole.Deejay;
	private readonly Dictionary<RolodexRole, int> rolodexFocusByRole = new();

	private static readonly Dictionary<RolodexRole, Color> RoleInk = new() {
		{ RolodexRole.Deejay, new Color("1b2a52") }, { RolodexRole.Manager, new Color("8f2a22") }, { RolodexRole.Publisher, new Color("5a2d4a") },
		{ RolodexRole.Plant, new Color("4f5c2c") }, { RolodexRole.Distributor, new Color("2f5560") }, { RolodexRole.Dealer, new Color("8a5a1c") },
	};

	/// <summary>Surname first, as a real book files them.</summary>
	private static string FilingKey(string name) {
		string[] parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length == 0 ? "" : parts[^1] + ", " + string.Join(' ', parts.Take(parts.Length - 1));
	}

	private void PageRolodex() {
		PlayerDesk desk = PlayerDesk.Instance;

		// A live call takes over the page entirely -- you are on the phone, not browsing a book.
		if (desk.ActiveCall != null && desk.ActiveCall.stage != CallStage.Ended) {
			PageCall(desk, desk.ActiveCall);
			return;
		}

		RolodexTabs(desk);

		// The cards in this tab, filed by surname.
		List<RolodexEntry> djs = rolodexRole == RolodexRole.Deejay ? desk.Rolodex.OrderBy(e => FilingKey(e.displayName), StringComparer.OrdinalIgnoreCase).ToList() : null;
		List<DirectoryCard> people = rolodexRole == RolodexRole.Deejay ? null : RolodexDirectory.Cards(desk, rolodexRole);
		int count = djs?.Count ?? people.Count;

		if (count == 0) {
			Body(RolodexDirectory.EmptyNote(rolodexRole));
			if (rolodexRole == RolodexRole.Deejay) RenderWorkThePhones(desk);
			return;
		}

		string NameOf(int i) => djs != null ? djs[i].displayName : people[i].Name;
		int focus = Mathf.Clamp(rolodexFocusByRole.TryGetValue(rolodexRole, out int saved) ? saved : 0, 0, count - 1);

		var nav = new HBoxContainer();
		nav.AddThemeConstantOverride("separation", 10);
		var counter = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		counter.AddThemeColorOverride("font_color", Heard);
		void ShowCounter(int at) => counter.Text = count > 1
			? $"Card {at + 1} of {count}  ·  scroll the wheel over the cards to flip through them"
			: "One card in this book";
		ShowCounter(focus);
		RolodexDeck deck = null;
		if (count > 1) {
			var prev = Btn("‹"); prev.CustomMinimumSize = new Vector2(44, 34); prev.TooltipText = "Flip back";
			prev.Pressed += () => deck?.Flip(-1);
			nav.AddChild(prev);
		}
		nav.AddChild(counter);
		if (count > 1) {
			var next = Btn("›"); next.CustomMinimumSize = new Vector2(44, 34); next.TooltipText = "Flip forward";
			next.Pressed += () => deck?.Flip(1);
			nav.AddChild(next);
		}
		content.AddChild(nav);

		RolodexRole role = rolodexRole;
		deck = new RolodexDeck().Set(count, focus,
			i => BuildRolodexCard(desk, role, djs?[i], people?[i]),
			NameOf,
			i => { rolodexFocusByRole[role] = i; ShowCounter(i); });
		content.AddChild(deck);

		if (rolodexRole == RolodexRole.Deejay) RenderWorkThePhones(desk);
	}

	/// <summary>The divider tabs across the top of the book, one per identity function, each with how many cards it holds.</summary>
	private void RolodexTabs(PlayerDesk desk) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 6);
		foreach (RolodexRole role in Enum.GetValues<RolodexRole>()) {
			RolodexRole captured = role;
			int n = RolodexDirectory.Count(desk, role);
			bool open = role == rolodexRole;
			var tab = Btn($"{RolodexDirectory.TabName(role)}{(n > 0 ? $"  ·  {n}" : "")}");
			tab.FocusMode = FocusModeEnum.None;
			tab.CustomMinimumSize = new Vector2(0, open ? 40f : 36f);
			tab.SizeFlagsVertical = SizeFlags.ShrinkEnd;
			Color ink = RoleInk[role];
			StyleBoxFlat Face(Color fill) => new() {
				BgColor = fill, BorderColor = new Color("2b2115"), BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 4, BorderWidthBottom = 0,
				CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
				ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 6, ContentMarginBottom = 6
			};
			StyleBoxFlat face = Face(open ? new Color("f5edd2") : new Color("e2d4a8"));
			face.BorderColor = ink;
			face.BorderWidthTop = open ? 6 : 4;
			foreach (string state in new[] { "normal", "pressed", "disabled", "focus" }) tab.AddThemeStyleboxOverride(state, face);
			StyleBoxFlat hover = Face(new Color("f9f2dc")); hover.BorderColor = ink; hover.BorderWidthTop = 5;
			tab.AddThemeStyleboxOverride("hover", hover);
			tab.AddThemeFontOverride("font", open ? PaperTheme.SansBold : PaperTheme.SansSemiBold);
			tab.AddThemeFontSizeOverride("font_size", 14);
			foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
				tab.AddThemeColorOverride(name, open ? ink : Ink);
			tab.Pressed += () => { rolodexRole = captured; Refresh(); };
			row.AddChild(tab);
		}
		content.AddChild(row);
	}

	// ---- the cards ----------------------------------------------------------------------------------------

	/// <summary>One card, built fresh each time the deck asks (the deck builds the next card before it flips to it).</summary>
	private Control BuildRolodexCard(PlayerDesk desk, RolodexRole role, RolodexEntry dj, DirectoryCard person) {
		var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		body.AddThemeConstantOverride("separation", 4);
		VBoxContainer saved = null;
		// The page helpers (Body, Heading, RenderCarrying...) write into `content`; point it at this card while it is built.
		saved = content; content = body;
		try {
			if (dj != null) RenderDjCard(desk, dj, body);
			else RenderPersonCard(person, body);
		} finally { content = saved; }
		return RolodexDeck.Card(body);
	}

	private HBoxContainer CardHeader(PortraitFigure figure, string key, string name, string line1, string line2, Color accent, string line2Tip = null) {
		var head = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 108) };
		head.AddThemeConstantOverride("separation", 16);
		head.AddChild(new PortraitPhoto().Set(Portraits.ForFigures("card:" + key, new List<PortraitFigure> { figure }), new Vector2(86, 108), key));
		var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 2);
		var title = new Label { Text = name, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(0, 46), VerticalAlignment = VerticalAlignment.Center };
		title.AddThemeFontOverride("font", PaperTheme.Elite);
		title.AddThemeFontSizeOverride("font_size", 28);
		title.AddThemeColorOverride("font_color", Ink);
		column.AddChild(title);
		var a = new Label { Text = line1 };
		a.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		a.AddThemeFontSizeOverride("font_size", 15);
		a.AddThemeColorOverride("font_color", Ink);
		column.AddChild(a);
		var b = new Label { Text = line2, TooltipText = line2Tip ?? "" };
		b.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		b.AddThemeFontSizeOverride("font_size", 14);
		b.AddThemeColorOverride("font_color", accent);
		column.AddChild(b);
		head.AddChild(column);
		return head;
	}

	private void RenderPersonCard(DirectoryCard person, VBoxContainer body) {
		string line1 = string.Join("  ·  ", new[] { person.Title, person.Firm }.Where(s => !string.IsNullOrEmpty(s)));
		body.AddChild(CardHeader(person.Figure, person.Key, person.Name, line1, string.IsNullOrEmpty(person.Place) ? "" : person.Place, RoleInk[person.Role]));
		foreach ((string text, bool warn) in person.Facts) {
			var line = new Label { Text = "•  " + text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			line.AddThemeFontSizeOverride("font_size", 16);
			line.AddThemeColorOverride("font_color", warn ? Rust : Ink);
			body.AddChild(line);
		}
		if (!string.IsNullOrEmpty(person.GoToTab)) {
			string tab = person.GoToTab;
			var go = Btn(person.GoToLabel ?? "OPEN");
			go.CustomMinimumSize = new Vector2(280, 38);
			go.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			go.Pressed += () => OpenAtTab(tab);
			body.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
			body.AddChild(go);
		}
	}

	/// <summary>A reporter's card: portrait, identity, tier, what you know about his hours, and what he is currently
	/// carrying for you. The only card with a phone behind it.</summary>
	private void RenderDjCard(PlayerDesk desk, RolodexEntry entry, VBoxContainer body) {
		Deejay dj = ChartManager.Instance?.GetDeejay(entry.djId);
		RadioStation station = ChartManager.Instance?.GetRadioStation(entry.stationId);
		float rapport = station?.rt?.Rapport(desk.Label?.labelId ?? "") ?? 0f;
		RapportTier tier = RolodexEntry.EffectiveTier(entry, rapport);
		string discoveryLabel = entry.state switch {
			DiscoveryState.HeardOf => "Heard of you",
			DiscoveryState.Introduced => "Introduced",
			DiscoveryState.Known => "Known",
			DiscoveryState.Trusted => "Trusted",
			_ => "New contact"
		};
		string where = station == null ? "Disc jockey" : $"Disc jockey  ·  {station.callsign}  ·  {station.format}";
		string place = station?.cityName ?? "";
		PortraitFigure figure = Portraits.Announcer(entry.djId ?? entry.displayName, dj?.archetype ?? DJArchetype.CompanyMan);
		HBoxContainer head = CardHeader(figure, "dj:" + (entry.djId ?? entry.displayName), entry.displayName, where,
			$"{place}{(place.Length > 0 ? "  ·  " : "")}{RolodexEntry.TierLabel(tier)}  ·  {discoveryLabel}", RolodexEntry.TierColor(tier).Darkened(0.25f));
		body.AddChild(head);

		// The verb first: what you can do with this man is on the visible part of the card, the history below it.
		var openBtn = Primary($"PLACE A CALL  ({PlayerDesk.DialMinutes} min)");
		openBtn.CustomMinimumSize = new Vector2(230, 40);
		openBtn.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		openBtn.Pressed += () => {
			string rid = rolodexPitchRecordId != null && desk.RecordsOnTheTable().Any(item => item.RecordId == rolodexPitchRecordId)
				? rolodexPitchRecordId : desk.RecordsOnTheTable().Select(item => item.RecordId).FirstOrDefault();
			rolodexPitchRecordId = rid;
			desk.PlaceCall(entry, rid, out string msg);
			if (!string.IsNullOrEmpty(msg)) Say(msg);
			Refresh();
		};
		var callRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		callRow.AddThemeConstantOverride("separation", 16);
		callRow.AddChild(openBtn);
		var notes = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		notes.AddThemeConstantOverride("separation", 2);
		callRow.AddChild(notes);
		body.AddChild(callRow);
		VBoxContainer cardContent = content;
		content = notes;
		if (dj != null) Body(RolodexEntry.ArchetypeBlurb(dj.archetype));
		// When to call him. Learned, not given -- an unreached name comes with no hours.
		if (dj != null) {
			Daypart shift = RolodexShifts.ShiftOf(dj);
			if (entry.shiftKnown) {
				int hour = TimeManager.Instance?.CurrentHour ?? 12;
				bool nowGood = RolodexShifts.ReachableAt(shift, hour);
				var hours = new Label { Text = RolodexShifts.WindowAdvice(shift) + (nowGood ? "  ·  He should be in right now." : "  ·  Wrong time of day."), AutowrapMode = TextServer.AutowrapMode.WordSmart };
				hours.AddThemeColorOverride("font_color", nowGood ? new Color("4a7a4a") : Rust);
				content.AddChild(hours);
			} else {
				Body("You don't know his hours yet. Call and find out the hard way.");
			}
		}
		content = cardContent;

		if (dj != null && entry.callbackDate != null) {
			bool due = desk.CallbackDue(entry);
			var callback = new Label { Text = due ? "Your call-back is due now." : $"Call-back set for {desk.CallbackWhen(entry)}." };
			callback.AddThemeColorOverride("font_color", due ? new Color("4a7a4a") : Heard);
			content.AddChild(callback);
			var cancel = Btn("CANCEL THE CALL-BACK");
			cancel.Pressed += () => { desk.CancelCallback(entry); Refresh(); };
			content.AddChild(cancel);
		}

		if (entry.theyOweThem) Body("He owes you one.");
		if (entry.youOweThem) Body("You owe him one.");
		if (entry.payolaBurned) Body("The cash channel is closed with him.");
		if (entry.professionallyBurned) Body("He doesn't take your word any more.");

		// What he is actually carrying for you right now -- the record-level commitment, not the mood.
		RenderCarrying(desk, entry);

		if (entry.log.Count > 0) {
			Heading("HISTORY");
			foreach (string line in entry.log.TakeLast(6)) Body(line);
		}
	}
}
