using System.Linq;
using Godot;

/// <summary>Instincts and stats drawn as prints and vinyl, in place of the star glyphs.</summary>
public partial class PlayerDeskPanel {
	private static InstinctKind StrongestInstinct(ExecutiveInstinctProfile spread) {
		int ear = spread.TheEar, street = spread.TheStreet, suit = spread.TheSuit, fixer = spread.TheFixer;
		int top = System.Math.Max(System.Math.Max(ear, street), System.Math.Max(suit, fixer));
		return ear == top ? InstinctKind.Ear : street == top ? InstinctKind.Street : suit == top ? InstinctKind.Suit : InstinctKind.Fixer;
	}

	/// <summary>The four instincts side by side: each a linocut, its name and a row of five discs (pressed for what you have).</summary>
	private Control InstinctRow(ExecutiveInstinctProfile spread, bool showNumbers) {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 22);
		void Cell(InstinctKind kind, string name, int value, string tip) {
			var cell = new HBoxContainer { TooltipText = tip, MouseFilter = Control.MouseFilterEnum.Pass };
			cell.AddThemeConstantOverride("separation", 8);
			cell.AddChild(new LinocutIcon().Set(kind, 46f));
			var stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
			stack.AddThemeConstantOverride("separation", 1);
			var label = new Label { Text = showNumbers ? $"{name}  ({value})" : name };
			label.AddThemeFontOverride("font", PaperTheme.SansBold);
			label.AddThemeFontSizeOverride("font_size", 13);
			label.AddThemeColorOverride("font_color", Ink);
			stack.AddChild(label);
			stack.AddChild(new VinylRating().SetOutOfFive(value, 15f));
			cell.AddChild(stack);
			row.AddChild(cell);
		}
		Cell(InstinctKind.Ear, "THE EAR", spread.TheEar, "How well you read a record and an act.");
		Cell(InstinctKind.Street, "THE STREET", spread.TheStreet, "How well you read a shop, a station and a town.");
		Cell(InstinctKind.Suit, "THE SUIT", spread.TheSuit, "How you carry yourself in an office and across a table.");
		Cell(InstinctKind.Fixer, "THE FIXER", spread.TheFixer, "What you can get done off the record.");
		return row;
	}

	/// <summary>A line of named 0..1 stats, each as discs.</summary>
	private Control StatRow(params (string Name, float Value)[] stats) {
		var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 24);
		foreach ((string name, float value) in stats) {
			var cell = new HBoxContainer();
			cell.AddThemeConstantOverride("separation", 8);
			var label = new Label { Text = name };
			label.AddThemeColorOverride("font_color", Ink);
			cell.AddChild(label);
			cell.AddChild(new VinylRating().Set(value, 14f));
			row.AddChild(cell);
		}
		return row;
	}

	/// <summary>The title card the founding page opens on: the game's name set like a record sleeve's banner, a year and a row of discs.</summary>
	private Control FoundingTitleCard() {
		var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		var paper = PaperStyleBox.Sheet(new Color("f3ead0"), 24, 14, 10, new Color("2b2115"));
		paper.BorderWidth = 3; paper.Burn = 0.7f; paper.Falloff = 0.6f;
		card.AddThemeStyleboxOverride("panel", paper);
		var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		column.AddThemeConstantOverride("separation", 2);
		card.AddChild(column);
		var kicker = new Label { Text = "A  RECORD  COMPANY  IN  THE  YEAR  1960", HorizontalAlignment = HorizontalAlignment.Center };
		kicker.AddThemeFontOverride("font", PaperTheme.SansBold);
		kicker.AddThemeFontSizeOverride("font_size", 13);
		kicker.AddThemeColorOverride("font_color", Rust);
		column.AddChild(kicker);
		var title = new Label { Text = "Start a Label", HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontOverride("font", PaperTheme.Lettering(LetteringStyle.Didone));
		title.AddThemeFontSizeOverride("font_size", 54);
		title.AddThemeColorOverride("font_color", Ink);
		column.AddChild(title);
		var discs = new CenterContainer();
		discs.AddChild(new VinylRating().SetOutOfFive(5, 15f));
		column.AddChild(discs);
		var line = new Label { Text = "Find the acts. Cut the records. Keep the doors open.", HorizontalAlignment = HorizontalAlignment.Center };
		line.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		line.AddThemeFontSizeOverride("font_size", 16);
		line.AddThemeColorOverride("font_color", Ink);
		column.AddChild(line);
		return card;
	}
}
