using System;
using Godot;

/// <summary>
/// The one modal pattern for the office: a paper card over a dimmed desk, a rust title, a body the
/// caller fills, and right-aligned buttons. It replaces Godot's stock AcceptDialog / ConfirmationDialog,
/// which drew grey OS-style chrome on a warm-paper UI and left the page behind fully lit.
///
/// It lives on its own CanvasLayer so it always covers the whole viewport, whatever the office panel's
/// size or position. Escape (or <see cref="Cancel"/>) dismisses it; clicking the backdrop does not, so a
/// stray click can never answer a question about money.
/// </summary>
public partial class PaperModal : CanvasLayer {
	public enum ButtonKind { Secondary, Primary, Danger }

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Paper = new("f1e5c8");
	private static readonly Color Rust = new("6b3a1c");
	private static readonly Color Danger = new("9a2b1a");

	private HBoxContainer buttons;
	private Action onCancel;
	private bool closed;

	/// <summary>Where the caller puts the modal's content (wrapped text, a scroll of cards, a form).</summary>
	public VBoxContainer Body { get; private set; }

	/// <summary>Builds the modal under <paramref name="host"/> and shows it. <paramref name="onCancel"/>
	/// runs when the player dismisses it with Escape; it is not run when a button closes it.</summary>
	public static PaperModal Open(Node host, string title, float width = 640, Action onCancel = null) {
		var modal = new PaperModal { Layer = 60, onCancel = onCancel };
		modal.Build(title, width);
		host.AddChild(modal);
		return modal;
	}

	private void Build(string title, float width) {
		// A CanvasLayer does not inherit the window's theme, so the modal carries the office theme itself.
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Theme = PaperTheme.Build() };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		var dim = new ColorRect { Color = new Color(0.07f, 0.05f, 0.03f, 0.62f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(center);

		var card = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
		var sheet = PaperStyleBox.Sheet(Paper, 28, 22, 18, Rust);
		sheet.BorderWidth = 2; sheet.Radius = 3; sheet.ShadowAlpha = 0.5f;
		sheet.ContentMarginBottom = 20;
		card.AddThemeStyleboxOverride("panel", sheet);
		center.AddChild(card);

		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 12);
		card.AddChild(column);

		var heading = new Label { Text = title, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		heading.AddThemeFontSizeOverride("font_size", 22);
		heading.AddThemeColorOverride("font_color", Rust);
		column.AddChild(heading);

		var rule = new ColorRect { Color = new Color("b99759"), CustomMinimumSize = new Vector2(0, 2) };
		column.AddChild(rule);

		Body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		Body.AddThemeConstantOverride("separation", 10);
		column.AddChild(Body);

		buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		buttons.AddThemeConstantOverride("separation", 10);
		column.AddChild(buttons);
	}

	/// <summary>Wrapped body text in the office's ink.</summary>
	public Label AddText(string text) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		label.AddThemeFontSizeOverride("font_size", 16);
		label.AddThemeColorOverride("font_color", Ink);
		Body.AddChild(label);
		return label;
	}

	/// <summary>Adds a button to the footer. Pressing it closes the modal first, then runs
	/// <paramref name="onPress"/> (null = just close), so a handler that refreshes the page never fights it.</summary>
	public Button AddButton(string text, Action onPress, ButtonKind kind = ButtonKind.Secondary) {
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 40) };
		if (kind != ButtonKind.Secondary) StylePrimary(button, kind == ButtonKind.Danger ? Danger : Rust);
		button.Pressed += () => { Close(); onPress?.Invoke(); };
		buttons.AddChild(button);
		return button;
	}

	/// <summary>Closes without answering. Used by Escape.</summary>
	public void Cancel() {
		if (closed) return;
		Close();
		onCancel?.Invoke();
	}

	public void Close() {
		if (closed) return;
		closed = true;
		QueueFree();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (closed || @event is not InputEventKey { Pressed: true, Keycode: Key.Escape }) return;
		GetViewport().SetInputAsHandled();
		Cancel();
	}

	internal static void StylePrimary(Button button, Color fill) {
		StyleBoxFlat Box(Color color) => new() {
			BgColor = color, BorderColor = new Color("2b2115"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
			ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 6, ContentMarginBottom = 6
		};
		button.AddThemeStyleboxOverride("normal", Box(fill));
		button.AddThemeStyleboxOverride("hover", Box(fill.Lightened(.15f)));
		button.AddThemeStyleboxOverride("pressed", Box(fill.Darkened(.15f)));
		button.AddThemeStyleboxOverride("focus", Box(fill));
		foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
			button.AddThemeColorOverride(name, Colors.White);
	}
}
