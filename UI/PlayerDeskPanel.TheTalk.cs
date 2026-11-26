using System.Linq;
using Godot;

/// <summary>
/// The Talk (SimTools/SigningExperienceAudit.md §3.3, decided: it takes over the A&amp;R page the way a call takes over
/// the Rolodex). Their spokesperson opens, the voices read, and what you can say is a list of lines, each an existing
/// verb with its cost underneath. The answer comes back into the transcript, not a toast. Built on
/// <see cref="PlayerDesk.BuildTalkScene"/>; the transcript is the evening's and isn't saved.
/// </summary>
public partial class PlayerDeskPanel {
	/// <summary>The act you're talking to, when The Talk has the page.</summary>
	private string theTalkArtistId;

	private void OpenTheTalk(PlayerDesk desk, ScoutingDossier dossier) {
		theSetQueue.Clear();
		theTalkArtistId = dossier.ArtistId;
		Refresh();
	}

	private void EndTheTalk(PlayerDesk desk, string artistId) {
		desk.EndTalk(artistId);
		theTalkArtistId = null;
		scoutingView = "ACT NOTEBOOK";
		FocusScoutingAct(artistId);
	}

	/// <summary>The conversation, when one is on. True when it drew the page.</summary>
	private bool RenderTheTalk(PlayerDesk desk) {
		if (theTalkArtistId == null) return false;
		var scene = desk.BuildTalkScene(theTalkArtistId);
		if (scene == null) { theTalkArtistId = null; return false; }
		var artist = ArtistManager.Instance.GetArtist(scene.ArtistId);

		var name = new Label { Text = scene.ArtistName, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		name.AddThemeFontOverride("font", PaperTheme.Elite);
		name.AddThemeFontSizeOverride("font_size", 30);
		name.AddThemeColorOverride("font_color", Ink);
		content.AddChild(name);
		string genre = artist != null ? GenreNameFormatter.Format(artist.primaryGenre) : null;
		var rubric = new Label { Text = string.Join("  •  ", new[] { "TALKING WITH " + scene.Speaker.ToUpperInvariant(), genre?.ToUpperInvariant(),
			TimeManager.Instance.CurrentDate.ToHeadlineString().ToUpperInvariant() }.Where(s => !string.IsNullOrEmpty(s))) };
		rubric.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		rubric.AddThemeFontSizeOverride("font_size", 14);
		rubric.AddThemeColorOverride("font_color", Rust);
		content.AddChild(rubric);
		content.AddChild(new ColorRect { Color = new Color(Ink, 0.8f), CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });
		content.AddChild(TheSetNarration(scene.Setting));

		var call = new RolodexCall();
		call.transcript.AddRange(scene.Lines);
		call.transcript.AddRange(FirstWeekReads(desk, FirstWeekPlace.TheTalk));
		RenderTranscript(call);

		Heading("WHAT DO YOU SAY");
		string id = scene.ArtistId;
		foreach (var option in scene.Options) {
			var verb = option.Verb;
			RenderOption(new CallOption { label = option.Line, subLabel = option.Cost, voice = option.Voice, enabled = option.Enabled, disabledReason = option.Why }, () => {
				bool ok = desk.SayInTalk(id, verb, out string message, out var terms);
				if (terms != null) { EndTheTalk(desk, id); scoutingView = "ACT NOTEBOOK"; negotiating = terms; }
				// The answer is in the transcript; a toast only when nothing was said.
				else if (!ok) Say(message, false);
				Refresh();
			});
		}
		var end = Btn("END THE CONVERSATION");
		end.CustomMinimumSize = new Vector2(240, 40);
		end.TooltipText = "Back to their card. What was said is in your notes; tonight's talk isn't kept word for word.";
		end.Pressed += () => { EndTheTalk(desk, id); Refresh(); };
		content.AddChild(end);
		return true;
	}
}
