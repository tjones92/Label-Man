using Godot;

/// <summary>
/// The boxed, typed fields of an office form: a small rust caption over a value set in the typewriter, inside a ruled box. A
/// personnel card does not run its facts together as a sentence; each one has its own box, so the eye can find "Formed" or "Top 10"
/// without reading the line. Presentation only: the values are whatever the caller passes.
/// </summary>
public static class TypedFields {
	private static readonly Color Box = new("f8f0d9"), Rule = new("8a7048");

	/// <summary>One captioned box.</summary>
	public static Control Field(string caption, string value, int valueSize = 16) {
		var cell = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		cell.AddThemeConstantOverride("separation", 2);
		var cap = new Label { Text = caption.ToUpperInvariant() };
		cap.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		cap.AddThemeFontSizeOverride("font_size", 11);
		cap.AddThemeColorOverride("font_color", PaperTheme.Rust);
		cell.AddChild(cap);
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = Box, BorderColor = Rule,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 2,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 3, ContentMarginBottom = 3
		});
		var text = new Label { Text = value };
		text.AddThemeFontOverride("font", PaperTheme.Typed);
		text.AddThemeFontSizeOverride("font_size", valueSize);
		text.AddThemeColorOverride("font_color", PaperTheme.Ink);
		panel.AddChild(text);
		cell.AddChild(panel);
		return cell;
	}

	/// <summary>A row of boxes that wraps onto a second line rather than stretching its page.</summary>
	public static HFlowContainer Row(params (string Caption, string Value)[] fields) {
		var row = new HFlowContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("h_separation", 8);
		row.AddThemeConstantOverride("v_separation", 6);
		Fill(row, fields);
		return row;
	}

	/// <summary>Replaces a row's boxes, for a panel that is reused from one act to the next.</summary>
	public static void Fill(HFlowContainer row, params (string Caption, string Value)[] fields) {
		foreach (Node child in row.GetChildren()) { row.RemoveChild(child); child.QueueFree(); }
		foreach (var (caption, value) in fields) row.AddChild(Field(caption, value));
	}
}
