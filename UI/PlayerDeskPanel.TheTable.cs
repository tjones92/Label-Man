using Godot;

/// <summary>
/// The Table (SimTools/SigningExperienceAudit.md §3.4): somebody sits across from the carbon sheet. Who it is, their ask
/// in their own words, what the Suit and the Fixer make of it, and everything said across the table since, in the same
/// transcript a call uses. Built on <see cref="PlayerDesk.BuildTableScene(ContractTalk)"/>; nothing here is saved.
/// </summary>
public partial class PlayerDeskPanel {
	private void RenderTable(TableScene scene) {
		if (scene == null) return;
		var rubric = new Label { Name = "TableSpeaker", Text = "ACROSS THE TABLE: " + scene.Speaker.ToUpperInvariant(), AutowrapMode = TextServer.AutowrapMode.WordSmart };
		rubric.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		rubric.AddThemeFontSizeOverride("font_size", 14);
		rubric.AddThemeColorOverride("font_color", Rust);
		content.AddChild(rubric);
		content.AddChild(TheSetNarration(scene.Setting));
		var call = new RolodexCall();
		call.transcript.AddRange(scene.Lines);
		RenderTranscript(call);
	}
}
