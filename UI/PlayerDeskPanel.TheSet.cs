using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The Set (SimTools/SigningExperienceAudit.md §3.2): after LISTEN, the A&amp;R page is the room for a moment, the way a
/// call takes over the Rolodex. The room, the songs, what the voices made of it, and the act coming off the stand; then
/// the one thing to do about them (talk, or sit down on terms) or let them go. Several acts on the bill come one after
/// another. The scene is built on <see cref="PlayerDesk.BuildSetScene"/> from the notebook; nothing here is saved, and
/// every set is in the notebook whatever the player picks.
/// </summary>
public partial class PlayerDeskPanel {
	/// <summary>The acts from the set just heard, still to be shown; the first is on stage.</summary>
	private readonly List<string> theSetQueue = new();

	/// <summary>After a listen: stage the acts just heard. Falls back to the notebook when there's nothing to stage.</summary>
	private void OpenTheSet(PlayerDesk desk) {
		theSetQueue.Clear();
		theSetQueue.AddRange(desk.Slate.Select(p => p.Artist?.artistId).Where(id => id != null && desk.BuildSetScene(id) != null).Distinct());
		if (theSetQueue.Count == 0) ShowLatestScoutingAct(desk);
	}

	/// <summary>The scene, when one is on. True when it drew the page.</summary>
	private bool RenderTheSet(PlayerDesk desk) {
		while (theSetQueue.Count > 0 && desk.BuildSetScene(theSetQueue[0]) == null) theSetQueue.RemoveAt(0);
		if (theSetQueue.Count == 0) return false;
		var scene = desk.BuildSetScene(theSetQueue[0]);
		var artist = ArtistManager.Instance.GetArtist(scene.ArtistId);
		var dossier = desk.ScoutingDossiers.FirstOrDefault(d => d.ArtistId == scene.ArtistId);

		// The act, typed; where and when on a rubric under it.
		var name = new Label { Text = scene.ArtistName, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		name.AddThemeFontOverride("font", PaperTheme.Elite);
		name.AddThemeFontSizeOverride("font_size", 30);
		name.AddThemeColorOverride("font_color", Ink);
		content.AddChild(name);
		var clock = TimeManager.Instance;
		string genre = artist != null ? GenreNameFormatter.Format(artist.primaryGenre) : null;
		var rubric = new Label { Text = string.Join("  •  ", new[] { "AT " + scene.RoomName.ToUpperInvariant(), genre?.ToUpperInvariant(),
			clock.CurrentDate.ToHeadlineString().ToUpperInvariant() }.Where(s => !string.IsNullOrEmpty(s))) };
		rubric.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		rubric.AddThemeFontSizeOverride("font_size", 14);
		rubric.AddThemeColorOverride("font_color", Rust);
		content.AddChild(rubric);
		content.AddChild(new ColorRect { Color = new Color(Ink, 0.8f), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });

		// The room, then the songs.
		foreach (string line in scene.Room) content.AddChild(TheSetNarration(line));
		if (scene.Songs.Count > 0 || scene.SongsNote != null) {
			var songs = new VBoxContainer();
			songs.AddThemeConstantOverride("separation", 2);
			foreach (var (title, original) in scene.Songs) {
				var row = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
				row.AddThemeFontOverride("normal_font", PaperTheme.Serif);
				row.AddThemeFontOverride("italics_font", PaperTheme.SerifItalic);
				row.AddThemeFontSizeOverride("normal_font_size", 16);
				row.AddThemeFontSizeOverride("italics_font_size", 14);
				row.AddThemeColorOverride("default_color", Ink);
				row.Text = $"♪  {title.Replace("[", "[lb]")}   [i][color=#{Heard.ToHtml(false)}]{(original ? "their own" : "a cover")}[/color][/i]";
				songs.AddChild(row);
			}
			if (scene.SongsNote != null) songs.AddChild(TheSetNarration(scene.Songs.Count > 0 ? "…" + scene.SongsNote + "." : scene.SongsNote));
			var margin = new MarginContainer();
			margin.AddThemeConstantOverride("margin_left", 18);
			margin.AddChild(songs);
			content.AddChild(margin);
		}

		// What the voices made of it: the same transcript a call uses.
		if (scene.Reads.Count > 0) {
			var call = new RolodexCall();
			call.transcript.AddRange(scene.Reads);
			RenderTranscript(call);
		}
		content.AddChild(TheSetNarration(scene.OffStage));

		// What you do about them.
		var bar = new HBoxContainer();
		bar.AddThemeConstantOverride("separation", 14);
		string id = scene.ArtistId;
		if (artist != null && dossier != null) {
			bool free = string.IsNullOrEmpty(artist.labelId);
			bool terms = free && ScoutingActStanding(desk, artist).Terms;
			// An act under contract can still be asked about its past, but that's not the evening's main business.
			var talk = free ? Primary(terms ? "SIT DOWN ON TERMS" : "TALK TO THEM") : Btn("TALK TO THEM");
			talk.CustomMinimumSize = new Vector2(240, 42);
			talk.TooltipText = terms ? "They'll hear an offer. Open the contract: their ask, your offer, and what it costs the label."
				: free ? "Go over before they pack up: ask if they'd hear an offer, arrange a rehearsal or a demo, or get a number."
				: "They're under contract, but you can still ask about their past label decisions.";
			talk.Pressed += () => {
				theSetQueue.Clear();
				scoutingView = "ACT NOTEBOOK";
				FocusScoutingAct(id);
				PressScoutingPrimary(desk, dossier, terms);
			};
			bar.AddChild(talk);
		}
		string next = theSetQueue.Count > 1 ? desk.ScoutingDossiers.FirstOrDefault(d => d.ArtistId == theSetQueue[1])?.ArtistName : null;
		var go = Btn("LET THEM GO");
		go.CustomMinimumSize = new Vector2(180, 42);
		go.TooltipText = "It's all in your notebook either way." + (next != null ? $" Next on the bill: {next}." : "");
		go.Pressed += () => {
			theSetQueue.Remove(id);
			if (theSetQueue.Count == 0) scoutingView = "FIND A SET";
			Refresh();
		};
		bar.AddChild(go);
		if (next != null) {
			var note = new Label { Text = $"Next on the bill: {next}", VerticalAlignment = VerticalAlignment.Center };
			note.AddThemeFontOverride("font", PaperTheme.SerifItalic);
			note.AddThemeFontSizeOverride("font_size", 15);
			note.AddThemeColorOverride("font_color", Heard);
			bar.AddChild(note);
		}
		content.AddChild(bar);
		var drinks = new HBoxContainer();
		foreach (var companion in new[] { AfterSetCompanion.Booker, AfterSetCompanion.Act }) {
			var companionName = desk.AfterSetDrinkCompanion(id, companion);
			if (companionName == null) continue;
			var drink = Btn($"DRINK WITH {companionName.ToUpperInvariant()}");
			drink.TooltipText = "Stay for 30 minutes and get to know them a little better. The drink adds one hour of fatigue.";
			AddTonightPreview(drink, desk, desk.NowMinute + PlayerDesk.AfterSetDrinkMinutes, PlayerDesk.AfterSetDrinkDebt);
			drink.Pressed += () => Act(() => {
				bool ok = desk.ShareAfterSetDrink(id, companion, out string message);
				Say(message, ok);
				return ok;
			});
			drinks.AddChild(drink);
		}
		if (drinks.GetChildCount() > 0) {
			content.AddChild(TheSetNarration("Stay for a drink · 30 min · 1 hour of fatigue"));
			content.AddChild(drinks);
		} else drinks.Free();
		return true;
	}

	private static Label TheSetNarration(string text) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		label.AddThemeFontSizeOverride("font_size", 17);
		label.AddThemeColorOverride("font_color", new Color("4a3d28"));
		return label;
	}
}
