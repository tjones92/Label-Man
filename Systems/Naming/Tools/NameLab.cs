// Systems/Naming/Tools/NameLab.cs
// Standalone name-tuning tool. Run this scene with F6. Builds its UI in code and drives the
// SAME NameGenerator autoload / same JSON data the game uses — it is a front-end, not a fork.
//
// Loop: pick a category, (optionally) genre/year/type/seed, hit Spin to see fresh names, or
// type a word + tags and Add it to the lexicon (persists to lexicon.user.json, hot-reloads).

using System;
using System.Linq;
using Godot;

public partial class NameLab : Control {

	private OptionButton _symbol, _genre, _type;
	private SpinBox _year, _count;
	private LineEdit _seed, _word, _pos, _tags;
	private ItemList _results;
	private Label _status;

	public override void _Ready() {
		BuildUi();
		if (NameGenerator.Instance == null || !NameGenerator.Instance.IsReady()) {
			SetStatus("NameGenerator autoload not ready. Run this scene with F6 (not the editor).", true);
			return;
		}
		PopulateDropdowns();
		Spin();
	}

	// ------------------------------------------------------------------ UI build
	private void BuildUi() {
		SetAnchorsPreset(LayoutPreset.FullRect);
		var bg = new ColorRect { Color = new Color(0.12f, 0.12f, 0.14f) };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		foreach (var s in new[] { "left", "top", "right", "bottom" }) margin.AddThemeConstantOverride($"margin_{s}", 14);
		AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 8);
		margin.AddChild(root);

		var title = new Label { Text = "NameLab — procedural name tuner" };
		title.AddThemeFontSizeOverride("font_size", 22);
		root.AddChild(title);

		// Row 1: category / genre / year / type
		var row1 = new HBoxContainer();
		row1.AddThemeConstantOverride("separation", 8);
		root.AddChild(row1);
		row1.AddChild(new Label { Text = "Category" });
		_symbol = new OptionButton { CustomMinimumSize = new Vector2(220, 0) };
		_symbol.ItemSelected += _ => Spin();
		row1.AddChild(_symbol);
		row1.AddChild(new Label { Text = "Genre" });
		_genre = new OptionButton { CustomMinimumSize = new Vector2(170, 0) };
		_genre.ItemSelected += _ => Spin();
		row1.AddChild(_genre);
		row1.AddChild(new Label { Text = "Year" });
		_year = new SpinBox { MinValue = 1955, MaxValue = 1975, Value = 1965 };
		_year.ValueChanged += _ => Spin();
		row1.AddChild(_year);
		row1.AddChild(new Label { Text = "Type" });
		_type = new OptionButton { CustomMinimumSize = new Vector2(130, 0) };
		_type.ItemSelected += _ => Spin();
		row1.AddChild(_type);

		// Row 2: seed / count / buttons
		var row2 = new HBoxContainer();
		row2.AddThemeConstantOverride("separation", 8);
		root.AddChild(row2);
		row2.AddChild(new Label { Text = "Seed (blank = random)" });
		_seed = new LineEdit { CustomMinimumSize = new Vector2(120, 0), PlaceholderText = "e.g. 1001" };
		row2.AddChild(_seed);
		row2.AddChild(new Label { Text = "Count" });
		_count = new SpinBox { MinValue = 1, MaxValue = 100, Value = 20 };
		row2.AddChild(_count);
		var spinBtn = new Button { Text = "  Spin  " };
		spinBtn.Pressed += Spin;
		row2.AddChild(spinBtn);
		var reloadBtn = new Button { Text = "Reload data" };
		reloadBtn.Pressed += () => { NameGenerator.Instance.Reload(); PopulateDropdowns(); SetStatus("Reloaded lexicon + grammar from disk."); Spin(); };
		row2.AddChild(reloadBtn);

		// Results
		_results = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, AllowReselect = true };
		_results.AddThemeFontSizeOverride("font_size", 16);
		root.AddChild(_results);

		// Row 3: add a word
		var addBox = new PanelContainer();
		root.AddChild(addBox);
		var row3 = new HBoxContainer();
		row3.AddThemeConstantOverride("separation", 8);
		addBox.AddChild(row3);
		row3.AddChild(new Label { Text = "Add word" });
		_word = new LineEdit { CustomMinimumSize = new Vector2(160, 0), PlaceholderText = "word" };
		row3.AddChild(_word);
		row3.AddChild(new Label { Text = "pos" });
		_pos = new LineEdit { CustomMinimumSize = new Vector2(130, 0), PlaceholderText = "e.g. adjective" };
		row3.AddChild(_pos);
		row3.AddChild(new Label { Text = "tags" });
		_tags = new LineEdit { CustomMinimumSize = new Vector2(160, 0), PlaceholderText = "comma,separated" };
		row3.AddChild(_tags);
		var addBtn = new Button { Text = "Add + save" };
		addBtn.Pressed += AddWord;
		row3.AddChild(addBtn);
		var useBtn = new Button { Text = "Use current category's pos" };
		useBtn.Pressed += PrefillPosFromSymbol;
		row3.AddChild(useBtn);

		_status = new Label { Text = "" };
		_status.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
		root.AddChild(_status);
	}

	private void PopulateDropdowns() {
		_symbol.Clear();
		foreach (var s in NameGenerator.Instance.AvailableSymbols()) _symbol.AddItem(s);
		if (_genre.ItemCount == 0)
			foreach (var g in Enum.GetNames(typeof(Genre))) _genre.AddItem(g);
		if (_type.ItemCount == 0)
			foreach (var t in Enum.GetNames(typeof(ArtistType))) _type.AddItem(t);
		SelectGenre("Soul");
	}

	// ------------------------------------------------------------------ actions
	private void Spin() {
		if (NameGenerator.Instance == null || !NameGenerator.Instance.IsReady() || _symbol.ItemCount == 0) return;
		string symbol = _symbol.GetItemText(_symbol.Selected);
		Genre genre = Enum.Parse<Genre>(_genre.GetItemText(Math.Max(0, _genre.Selected)));
		ArtistType type = Enum.Parse<ArtistType>(_type.GetItemText(Math.Max(0, _type.Selected)));
		int year = (int)_year.Value;
		int count = (int)_count.Value;
		ulong? seed = ulong.TryParse(_seed.Text?.Trim(), out var s) ? s : (ulong?)null;

		var names = NameGenerator.Instance.Spin(symbol, genre, year, type, LabelArchetype.RegionalHustler, count, seed);
		_results.Clear();
		foreach (var n in names) _results.AddItem(n);
		SetStatus($"Spun {names.Length} × \"{symbol}\"  (genre={genre}, year={year}, type={type}, seed={(seed.HasValue ? seed.ToString() : "random")}).");
	}

	private void AddWord() {
		string word = _word.Text?.Trim();
		string pos = _pos.Text?.Trim();
		if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pos)) { SetStatus("Enter both a word and a pos.", true); return; }
		var tags = (_tags.Text ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
		bool ok = NameGenerator.Instance.AddWordAndSave(word, pos, tags);
		if (ok) {
			SetStatus($"Added \"{word}\" as {pos} [{string.Join(",", tags)}] -> saved to lexicon.user.json + reloaded.");
			PopulateDropdowns();
			_word.Clear();
			Spin();
		} else SetStatus("Failed to add word (see output log).", true);
	}

	private void PrefillPosFromSymbol() {
		// Best-effort: suggest a pos commonly used by the selected category.
		string sym = _symbol.Selected >= 0 ? _symbol.GetItemText(_symbol.Selected) : "";
		string guess = sym.Contains("song") ? "noun" : sym.Contains("band") || sym.Contains("Name") ? "groupNoun" : "adjective";
		_pos.Text = guess;
	}

	private void SelectGenre(string name) {
		for (int i = 0; i < _genre.ItemCount; i++)
			if (_genre.GetItemText(i) == name) { _genre.Select(i); return; }
	}

	private void SetStatus(string msg, bool error = false) {
		_status.Text = msg;
		_status.AddThemeColorOverride("font_color", error ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.85f, 1f));
	}
}
