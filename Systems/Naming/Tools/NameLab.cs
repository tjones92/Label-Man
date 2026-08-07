// Systems/Naming/Tools/NameLab.cs
// Standalone name-tuning tool. Run this scene with F6. Builds its UI in code and drives the
// SAME NameGenerator autoload / same JSON data the game uses — it is a front-end, not a fork.
//
// Left: pick a category (a "by genre" routed category or a raw grammar symbol), set genre/year/
// type/seed, optionally a band name, and Spin. Right: the dictionary panel shows the exact word
// groups the chosen category queries — view, add (with an optional era window), rename, or delete
// words. All edits persist to lexicon.user.json (an overlay) and hot-reload, so the base
// lexicon.json stays pristine and tuning immediately reaches the game.

using System;
using System.Linq;
using Godot;

public partial class NameLab : Control {

	private OptionButton _symbol, _genre, _type;
	private SpinBox _year, _count;
	private LineEdit _seed, _artist;
	private CheckBox _coin;
	private ItemList _results;
	private Label _status;

	// dictionary panel
	private OptionButton _group;
	private ItemList _words;
	private LineEdit _word, _eraStart, _eraEnd, _tags;
	private NameGenerator.LexGroupView[] _groups = Array.Empty<NameGenerator.LexGroupView>();

	// engine inspector panel
	private ItemList _profile;
	private LineEdit _inflWord;
	private ItemList _inflOut;

	public override void _Ready() {
		BuildUi();
		if (NameGenerator.Instance == null || !NameGenerator.Instance.IsReady()) {
			SetStatus("NameGenerator autoload not ready. Run this scene with F6 (not the editor).", true);
			return;
		}
		PopulateDropdowns();
		RefreshGroups();
		RefreshInspector();
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

		var outer = new VBoxContainer();
		outer.AddThemeConstantOverride("separation", 8);
		margin.AddChild(outer);

		var title = new Label { Text = "NameLab — procedural name tuner" };
		title.AddThemeFontSizeOverride("font_size", 22);
		outer.AddChild(title);

		// two columns: generator (left) | dictionary (right)
		var cols = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		cols.AddThemeConstantOverride("separation", 16);
		outer.AddChild(cols);

		cols.AddChild(BuildGeneratorColumn());
		cols.AddChild(new VSeparator());
		cols.AddChild(BuildDictionaryColumn());
		cols.AddChild(new VSeparator());
		cols.AddChild(BuildEngineColumn());

		_status = new Label { Text = "" };
		_status.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
		outer.AddChild(_status);
	}

	private Control BuildGeneratorColumn() {
		var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		col.AddThemeConstantOverride("separation", 8);

		// Row 1: category / genre
		var row1 = Row(col);
		row1.AddChild(new Label { Text = "Category" });
		_symbol = new OptionButton { CustomMinimumSize = new Vector2(240, 0) };
		_symbol.ItemSelected += _ => { RefreshGroups(); Spin(); };
		row1.AddChild(_symbol);
		row1.AddChild(new Label { Text = "Genre" });
		_genre = new OptionButton { CustomMinimumSize = new Vector2(160, 0) };
		_genre.ItemSelected += _ => { RefreshGroups(); RefreshInspector(); Spin(); };
		row1.AddChild(_genre);

		// Row 2: year / type / coin surname
		var row2 = Row(col);
		row2.AddChild(new Label { Text = "Year" });
		_year = new SpinBox { MinValue = 1955, MaxValue = 1975, Value = 1965 };
		_year.ValueChanged += _ => { RefreshGroups(); Spin(); };
		row2.AddChild(_year);
		row2.AddChild(new Label { Text = "Type / gender" });
		_type = new OptionButton { CustomMinimumSize = new Vector2(130, 0) };
		_type.ItemSelected += _ => { RefreshGroups(); Spin(); };
		row2.AddChild(_type);
		_coin = new CheckBox { Text = "Markov surname (Person)" };
		_coin.Toggled += _ => Spin();
		row2.AddChild(_coin);

		// Row 3: band name (for album/tour/fan-club/etc.)
		var row3 = Row(col);
		row3.AddChild(new Label { Text = "Band name" });
		_artist = new LineEdit { CustomMinimumSize = new Vector2(220, 0), PlaceholderText = "blank = auto-generate" };
		_artist.TextSubmitted += _ => Spin();
		row3.AddChild(_artist);

		// Row 4: seed / count / buttons
		var row4 = Row(col);
		row4.AddChild(new Label { Text = "Seed" });
		_seed = new LineEdit { CustomMinimumSize = new Vector2(90, 0), PlaceholderText = "random" };
		row4.AddChild(_seed);
		row4.AddChild(new Label { Text = "Count" });
		_count = new SpinBox { MinValue = 1, MaxValue = 100, Value = 20 };
		row4.AddChild(_count);
		var spinBtn = new Button { Text = "  Spin  " };
		spinBtn.Pressed += Spin;
		row4.AddChild(spinBtn);
		var reloadBtn = new Button { Text = "Reload data" };
		reloadBtn.Pressed += () => { NameGenerator.Instance.Reload(); PopulateDropdowns(); RefreshGroups(); RefreshInspector(); SetStatus("Reloaded lexicon + grammar + models (ontology/moods/inflection/genres/templates) from disk."); Spin(); };
		row4.AddChild(reloadBtn);

		_results = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, AllowReselect = true, CustomMinimumSize = new Vector2(420, 0) };
		_results.AddThemeFontSizeOverride("font_size", 16);
		col.AddChild(_results);

		return col;
	}

	private Control BuildDictionaryColumn() {
		var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		col.AddThemeConstantOverride("separation", 8);

		var head = new Label { Text = "Dictionary — words this category uses" };
		head.AddThemeFontSizeOverride("font_size", 16);
		col.AddChild(head);

		var gr = Row(col);
		gr.AddChild(new Label { Text = "Word group" });
		_group = new OptionButton { CustomMinimumSize = new Vector2(260, 0) };
		_group.ItemSelected += _ => RefreshWordList();
		gr.AddChild(_group);

		_words = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, AllowReselect = true, CustomMinimumSize = new Vector2(320, 0) };
		_words.ItemSelected += idx => PrefillFromWord((int)idx);
		col.AddChild(_words);

		// add / edit row
		var er = Row(col);
		er.AddChild(new Label { Text = "Word" });
		_word = new LineEdit { CustomMinimumSize = new Vector2(150, 0), PlaceholderText = "word" };
		er.AddChild(_word);
		er.AddChild(new Label { Text = "era" });
		_eraStart = new LineEdit { CustomMinimumSize = new Vector2(52, 0), PlaceholderText = "from" };
		er.AddChild(_eraStart);
		_eraEnd = new LineEdit { CustomMinimumSize = new Vector2(52, 0), PlaceholderText = "to" };
		er.AddChild(_eraEnd);

		// ontology-axis tags to attach to a new word (domain/mood/register/era/locale)
		var tr = Row(col);
		tr.AddChild(new Label { Text = "+ axis tags" });
		_tags = new LineEdit { CustomMinimumSize = new Vector2(230, 0), PlaceholderText = "e.g. celestial, dreamy, ornate" };
		tr.AddChild(_tags);

		var br = Row(col);
		var addBtn = new Button { Text = "Add" };
		addBtn.Pressed += AddWord;
		br.AddChild(addBtn);
		var editBtn = new Button { Text = "Rename" };
		editBtn.Pressed += EditWord;
		br.AddChild(editBtn);
		var retagBtn = new Button { Text = "Retag selected" };
		retagBtn.Pressed += RetagWord;
		br.AddChild(retagBtn);
		var delBtn = new Button { Text = "Delete" };
		delBtn.Pressed += DeleteWord;
		br.AddChild(delBtn);

		var note = new Label { Text = "Select a word to see its axis tags in the tags box. Retag = apply edited DOMAIN/mood/register\ntags to that word (celestial→NATURE, dreamy→mood, ornate→register). Add = new word with those tags." };
		note.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.66f));
		note.AddThemeFontSizeOverride("font_size", 11);
		col.AddChild(note);

		return col;
	}

	// third column: read-only genre-profile inspector + inflection tester (Layers 1 & 6)
	private Control BuildEngineColumn() {
		var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		col.AddThemeConstantOverride("separation", 8);

		var head = new Label { Text = "Genre profile (Layer 1)" };
		head.AddThemeFontSizeOverride("font_size", 16);
		col.AddChild(head);
		var subhead = new Label { Text = "resolved voice + affinities — edit in genres.json, then Reload" };
		subhead.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.66f));
		subhead.AddThemeFontSizeOverride("font_size", 11);
		col.AddChild(subhead);

		_profile = new ItemList { SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(300, 0) };
		_profile.AddThemeFontSizeOverride("font_size", 13);
		col.AddChild(_profile);

		var infHead = new Label { Text = "Inflection tester (Layer 6)" };
		infHead.AddThemeFontSizeOverride("font_size", 16);
		col.AddChild(infHead);
		var ir = Row(col);
		ir.AddChild(new Label { Text = "Lemma" });
		_inflWord = new LineEdit { CustomMinimumSize = new Vector2(150, 0), PlaceholderText = "e.g. echo" };
		_inflWord.TextChanged += _ => RefreshInflection();
		ir.AddChild(_inflWord);
		_inflOut = new ItemList { CustomMinimumSize = new Vector2(300, 150) };
		_inflOut.AddThemeFontSizeOverride("font_size", 13);
		col.AddChild(_inflOut);

		return col;
	}

	private void RefreshInspector() {
		if (_profile == null || NameGenerator.Instance == null || !NameGenerator.Instance.IsReady()) return;
		_profile.Clear();
		var v = NameGenerator.Instance.InspectGenre(CurrentGenre());
		if (v == null) return;
		_profile.AddItem($"id: {v.Id}" + (string.IsNullOrEmpty(v.Extends) ? "" : $"  (extends {v.Extends})"));
		_profile.AddItem($"orthography: {v.Orthography}    moodThreshold: {v.MoodThreshold:0.00}");
		_profile.AddItem("— voice —");
		foreach (var (dim, val) in v.Voice) _profile.AddItem($"   {dim}: {val:0.00}");
		if (v.DomainAffinity.Length > 0) { _profile.AddItem("— domain affinity —");
			foreach (var (tag, w) in v.DomainAffinity) _profile.AddItem($"   {tag}: ×{w:0.0}"); }
		if (v.MoodAffinity.Length > 0) { _profile.AddItem("— mood affinity —");
			foreach (var (mood, w) in v.MoodAffinity) _profile.AddItem($"   {mood}: ×{w:0.0}"); }
		if (v.Suppress.Length > 0) _profile.AddItem("suppress: " + string.Join(", ", v.Suppress));
	}

	private void RefreshInflection() {
		if (_inflOut == null) return;
		_inflOut.Clear();
		foreach (var line in NameGenerator.Instance.InflectForms(_inflWord.Text)) _inflOut.AddItem(line);
	}

	private static HBoxContainer Row(Control parent) {
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		parent.AddChild(row);
		return row;
	}

	private void PopulateDropdowns() {
		_symbol.Clear();
		foreach (var c in NameGenerator.Instance.RoutedCategories()) _symbol.AddItem(c);
		var constraints = NameGenerator.Instance.ConstraintCategories();
		if (constraints.Length > 0) {
			_symbol.AddSeparator("constraint templates (Layer 2)");
			foreach (var c in constraints) _symbol.AddItem(c);
		}
		_symbol.AddSeparator("raw grammar symbols");
		foreach (var s in NameGenerator.Instance.AvailableSymbols()) _symbol.AddItem(s);
		if (_genre.ItemCount == 0)
			foreach (var g in Enum.GetNames(typeof(Genre))) _genre.AddItem(g);
		if (_type.ItemCount == 0)
			foreach (var t in Enum.GetNames(typeof(ArtistType))) _type.AddItem(t);
		SelectGenre("Soul");
	}

	// ------------------------------------------------------------------ generator actions
	private string CurrentCategory() => _symbol.Selected >= 0 ? _symbol.GetItemText(_symbol.Selected) : "";
	private Genre CurrentGenre() => Enum.Parse<Genre>(_genre.GetItemText(Math.Max(0, _genre.Selected)));
	private ArtistType CurrentType() => Enum.Parse<ArtistType>(_type.GetItemText(Math.Max(0, _type.Selected)));

	private void Spin() {
		if (NameGenerator.Instance == null || !NameGenerator.Instance.IsReady() || _symbol.ItemCount == 0) return;
		string category = CurrentCategory();
		if (string.IsNullOrEmpty(category)) return;
		Genre genre = CurrentGenre();
		ArtistType type = CurrentType();
		int year = (int)_year.Value;
		int count = (int)_count.Value;
		ulong? seed = ulong.TryParse(_seed.Text?.Trim(), out var s) ? s : (ulong?)null;

		var names = NameGenerator.Instance.Spin(category, genre, year, type, LabelArchetype.RegionalHustler,
												_artist.Text, _coin.ButtonPressed, count, seed);
		_results.Clear();
		foreach (var n in names) _results.AddItem(n);
		SetStatus($"Spun {names.Length} × \"{category}\"  (genre={genre}, year={year}, type={type}, seed={(seed.HasValue ? seed.ToString() : "random")}).");
	}

	// ------------------------------------------------------------------ dictionary actions
	private void RefreshGroups() {
		if (NameGenerator.Instance == null || !NameGenerator.Instance.IsReady()) return;
		string category = CurrentCategory();
		if (string.IsNullOrEmpty(category)) return;
		_groups = NameGenerator.Instance.GroupsForCategory(category, CurrentGenre(), (int)_year.Value, CurrentType());
		_group.Clear();
		if (_groups.Length == 0) {
			_group.AddItem("(no editable word groups)");
			_words.Clear();
			return;
		}
		foreach (var g in _groups) _group.AddItem(g.Label);
		_group.Select(0);
		RefreshWordList();
	}

	private NameGenerator.LexGroupView SelectedGroup() {
		int i = _group.Selected;
		return (_groups.Length == 0 || i < 0 || i >= _groups.Length) ? null : _groups[i];
	}

	private void RefreshWordList() {
		_words.Clear();
		var g = SelectedGroup();
		if (g == null) return;
		foreach (var w in g.Words) {
			string era = (w.EraStart.HasValue || w.EraEnd.HasValue) ? $"  ({w.EraStart?.ToString() ?? "…"}–{w.EraEnd?.ToString() ?? "…"})" : "";
			string tags = (w.Tags != null && w.Tags.Length > 0) ? $"   [{string.Join(", ", w.Tags)}]" : "";
			_words.AddItem(w.Word + tags + era);
		}
	}

	private void PrefillFromWord(int idx) {
		var g = SelectedGroup();
		if (g == null || idx < 0 || idx >= g.Words.Length) return;
		var w = g.Words[idx];
		_word.Text = w.Word;
		// surface the word's axis tags for viewing/editing; Retag applies edits back
		_tags.Text = (w.Tags != null) ? string.Join(", ", w.Tags) : "";
		_eraStart.Text = w.EraStart?.ToString() ?? "";
		_eraEnd.Text = w.EraEnd?.ToString() ?? "";
	}

	private void RetagWord() {
		var g = SelectedGroup();
		if (g == null) { SetStatus("Select a word group first.", true); return; }
		int idx = _words.IsAnythingSelected() ? _words.GetSelectedItems()[0] : -1;
		if (idx < 0 || idx >= g.Words.Length) { SetStatus("Select a word to retag.", true); return; }
		var w = g.Words[idx];
		var newTags = (_tags.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
		if (newTags.Count == 0) { SetStatus("Enter axis tags (comma-separated) to apply.", true); return; }
		// replace ALL of the word's entries with one new-tagged entry (duplicate-free)
		bool ok = NameGenerator.Instance.RetagWord(g.Pos, w.Word, newTags, ParseEra(_eraStart.Text), ParseEra(_eraEnd.Text));
		AfterEdit(ok, ok ? $"Retagged \"{w.Word}\" → [{string.Join(", ", newTags)}]." : "Retag failed (see output log).");
	}

	private static int? ParseEra(string s) => int.TryParse(s?.Trim(), out var v) ? v : (int?)null;

	private void AddWord() {
		var g = SelectedGroup();
		if (g == null) { SetStatus("Select a word group first.", true); return; }
		string word = _word.Text?.Trim();
		if (string.IsNullOrEmpty(word)) { SetStatus("Enter a word to add.", true); return; }
		// merge the group's tags with any ontology-axis tags the user typed
		var tags = g.Tags.ToList();
		foreach (var t in (_tags.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			if (!tags.Contains(t, StringComparer.OrdinalIgnoreCase)) tags.Add(t);
		bool ok = NameGenerator.Instance.AddWord(word, g.Pos, tags, ParseEra(_eraStart.Text), ParseEra(_eraEnd.Text));
		string extra = string.IsNullOrWhiteSpace(_tags.Text) ? "" : $" [{_tags.Text.Trim()}]";
		AfterEdit(ok, ok ? $"Added \"{word}\"{extra} to {g.Pos}." : "Add failed (see output log).");
	}

	private void EditWord() {
		var g = SelectedGroup();
		if (g == null) { SetStatus("Select a word group first.", true); return; }
		int idx = _words.IsAnythingSelected() ? _words.GetSelectedItems()[0] : -1;
		if (idx < 0 || idx >= g.Words.Length) { SetStatus("Select a word in the list to rename.", true); return; }
		string oldWord = g.Words[idx].Word;
		string newWord = _word.Text?.Trim();
		if (string.IsNullOrEmpty(newWord)) { SetStatus("Enter the new spelling in the Word box.", true); return; }
		bool ok = NameGenerator.Instance.EditWord(g.Pos, g.Tags, oldWord, newWord, ParseEra(_eraStart.Text), ParseEra(_eraEnd.Text));
		AfterEdit(ok, ok ? $"Renamed \"{oldWord}\" → \"{newWord}\"." : "Rename failed (see output log).");
	}

	private void DeleteWord() {
		var g = SelectedGroup();
		if (g == null) { SetStatus("Select a word group first.", true); return; }
		int idx = _words.IsAnythingSelected() ? _words.GetSelectedItems()[0] : -1;
		if (idx < 0 || idx >= g.Words.Length) { SetStatus("Select a word in the list to delete.", true); return; }
		string word = g.Words[idx].Word;
		bool ok = NameGenerator.Instance.DeleteWordEverywhere(g.Pos, word);
		AfterEdit(ok, ok ? $"Deleted \"{word}\" ({g.Pos}) — all tag variants." : "Delete failed (see output log).");
	}

	private void AfterEdit(bool ok, string msg) {
		SetStatus(msg, !ok);
		if (!ok) return;
		int keepGroup = _group.Selected;
		RefreshGroups();
		if (keepGroup >= 0 && keepGroup < _groups.Length) { _group.Select(keepGroup); RefreshWordList(); }
		_word.Clear();
		_tags?.Clear();
		Spin();
	}

	// ------------------------------------------------------------------ helpers
	private void SelectGenre(string name) {
		for (int i = 0; i < _genre.ItemCount; i++)
			if (_genre.GetItemText(i) == name) { _genre.Select(i); return; }
	}

	private void SetStatus(string msg, bool error = false) {
		_status.Text = msg;
		_status.AddThemeColorOverride("font_color", error ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 0.85f, 1f));
	}
}
