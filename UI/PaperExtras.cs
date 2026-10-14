using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

/// <summary>
/// What fills a newspaper once the news runs out, and the small things that make a day's news look printed: the
/// weather, the price board, an ad, repeated lines rolled up into one, clippings with a stamp on them, a halftone photo
/// slot on the lead, and the "extra" that a big enough day earns.
///
/// Everything here is presentation. The weather and prices are built from the date and the label's town through a hash
/// (never a random stream), so a given morning's paper reads the same however many times it is rebuilt, and no seeded
/// stream moves. The roll-up and the stamps only re-word and re-dress stories the desk has already logged.
/// </summary>
public static class PaperExtras {
	private static readonly Color Ink = new("30291d"), Rust = new("6b3a1c"), Body = new("4a4132"), Rule = new("867655");

	private static Label Text(string text, int size, Color color, Font font = null, HorizontalAlignment align = HorizontalAlignment.Left) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = align };
		label.AddThemeFontOverride("font", font ?? PaperTheme.Serif);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	// ---- rolling repeated lines up ------------------------------------------------------------------------

	private static readonly Regex TableSale = new(@"^Sold (?<n>[\d,]+) of ""(?<t>[^""]+)"" off the table at (?<where>.+?)\.$", RegexOptions.Compiled);
	private static readonly Regex AccountSale = new(@"^Sold (?<n>[\d,]+) of ""(?<t>[^""]+)"" to (?<where>.+?) \((?<terms>[^)]*)\)\.?$", RegexOptions.Compiled);

	/// <summary>Several "Sold 12 of ..." lines for one title become one: "Sold 36 copies of “X” at 4 tables." The order of the
	/// first appearance of each is kept, so a rolled-up line sits where the first of its kind did.</summary>
	public static List<string> RollUp(IEnumerable<string> stories) {
		var result = new List<string>();
		var tableTotals = new Dictionary<string, (int copies, int places, int index)>(StringComparer.Ordinal);
		var accountTotals = new Dictionary<string, (int copies, int places, int index)>(StringComparer.Ordinal);
		foreach (string story in stories) {
			Match table = TableSale.Match(story);
			if (table.Success) { Add(tableTotals, table, result); continue; }
			Match account = AccountSale.Match(story);
			if (account.Success) { Add(accountTotals, account, result); continue; }
			result.Add(story);
		}
		foreach (var pair in tableTotals) {
			var (copies, places, index) = pair.Value;
			result[index] = places == 1 ? $"Sold {copies:N0} copies of “{pair.Key}” off the table at one stop."
				: $"Sold {copies:N0} copies of “{pair.Key}” off the table at {places} stops.";
		}
		foreach (var pair in accountTotals) {
			var (copies, places, index) = pair.Value;
			if (places > 1) result[index] = $"Sold {copies:N0} copies of “{pair.Key}” to {places} accounts.";
		}
		return result;

		static void Add(Dictionary<string, (int copies, int places, int index)> totals, Match match, List<string> outList) {
			string title = match.Groups["t"].Value;
			int copies = int.Parse(match.Groups["n"].Value.Replace(",", ""));
			if (totals.TryGetValue(title, out var known)) { totals[title] = (known.copies + copies, known.places + 1, known.index); return; }
			totals[title] = (copies, 1, outList.Count);
			outList.Add(match.Value);   // placeholder, replaced once the totals are known
		}
	}

	// ---- clippings ----------------------------------------------------------------------------------------

	/// <summary>The stamp a story earns, or null: a sell-out, or a record breaking.</summary>
	public static string StampFor(string story) {
		if (story.Contains("sold out", StringComparison.OrdinalIgnoreCase) || story.Contains("ran dry", StringComparison.OrdinalIgnoreCase)
			|| story.Contains("out of stock", StringComparison.OrdinalIgnoreCase)) return "SOLD OUT";
		if (story.Contains("BREAKOUT", StringComparison.Ordinal)) return "BREAKOUT";
		if (story.Contains("CHART DEBUT", StringComparison.Ordinal)) return "NEW ON THE CHART";
		return null;
	}

	/// <summary>A story cut out of the page and pasted on: a ruled box, a hair of tilt, a rubber stamp across one corner.</summary>
	public static Control Clipping(string story, string stamp, int size, Color ink) {
		var frame = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("efe6c9"), BorderColor = Rule,
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8,
			ShadowColor = new Color(0, 0, 0, 0.18f), ShadowSize = 3, ShadowOffset = new Vector2(1, 2)
		});
		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 10);
		frame.AddChild(row);
		row.AddChild(Text(story, size, ink));
		bool red = stamp is "SOLD OUT" or "BREAKOUT";
		row.AddChild(new RubberStamp().Set(stamp, red ? RubberStamp.Red : RubberStamp.Blue, -0.2f, 13));
		frame.PivotOffset = new Vector2(120, 20);
		frame.RotationDegrees = (Portraits.Hash(story) % 5u - 2f) * 0.35f;
		return frame;
	}

	// ---- the halftone slot --------------------------------------------------------------------------------

	/// <summary>The act a lead story is about, if it is one of the player's: a RELEASED line names its act, and a story that
	/// names a signed act or one of the label's records finds it that way.</summary>
	public static SimulatedArtist ActFor(string story, PlayerDesk desk) {
		if (desk == null || string.IsNullOrEmpty(story)) return null;
		Match released = Regex.Match(story, "by (?<act>.+?) \\(");
		if (released.Success) {
			string name = released.Groups["act"].Value;
			SimulatedArtist byName = desk.Roster.FirstOrDefault(a => string.Equals(a.stageName, name, StringComparison.OrdinalIgnoreCase));
			if (byName != null) return byName;
		}
		foreach (SimulatedArtist act in desk.Roster)
			if (story.Contains(act.stageName, StringComparison.OrdinalIgnoreCase)) return act;
		foreach (RecordRuntimeData record in desk.ReleasedRecords) {
			string title = record?.baseRecord?.title;
			if (!string.IsNullOrEmpty(title) && story.Contains("\"" + title + "\"", StringComparison.OrdinalIgnoreCase))
				return desk.Roster.FirstOrDefault(a => a.artistId == record.baseRecord.artistId);
		}
		return null;
	}

	/// <summary>A halftone plate of the act with a one-line caption under it, ready to float beside a headline.</summary>
	public static Control PhotoSlot(SimulatedArtist act) {
		var column = new VBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
		column.AddThemeConstantOverride("separation", 2);
		column.AddChild(new PortraitPhoto().Set(Portraits.ForAct(act), new Vector2(150, 188), null));
		var caption = Text(act.stageName.ToUpperInvariant(), 11, Rust, PaperTheme.SansSemiBold, HorizontalAlignment.Center);
		caption.CustomMinimumSize = new Vector2(150, 0);
		caption.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		column.AddChild(caption);
		return column;
	}

	// ---- extras -------------------------------------------------------------------------------------------

	private static readonly Regex ChartMove = new(@"moved from #(?<from>\d+) to #(?<to>\d+)", RegexOptions.Compiled);

	/// <summary>A day big enough for an extra: a record breaking out of a market, or the label's record reaching the Top 10.</summary>
	public static bool IsExtra(IEnumerable<string> digests) {
		foreach (string digest in digests)
			foreach (string story in digest.Split("  •  ", StringSplitOptions.RemoveEmptyEntries)) {
				if (story.Contains("BREAKOUT", StringComparison.Ordinal)) return true;
				Match move = ChartMove.Match(story);
				if (move.Success && int.Parse(move.Groups["to"].Value) <= 10 && int.Parse(move.Groups["from"].Value) > 10) return true;
			}
		return false;
	}

	// ---- quiet-day fillers --------------------------------------------------------------------------------

	private static (float jan, float jul) Climate(string regionId) => regionId switch {
		"eastcoast" => (32f, 77f), "greatlakes" => (24f, 72f), "greatplains" => (21f, 76f), "deepsouth" => (50f, 81f),
		"southwest" => (52f, 90f), "rockies" => (27f, 71f), "westcoast" => (54f, 72f), _ => (35f, 75f),
	};

	/// <summary>The day's weather for the label's town: a seasonal mean for the region, a hashed swing, a hashed sky.</summary>
	public static string WeatherLine(GameDate date, string regionId, string town) {
		(float jan, float jul) = Climate(regionId);
		int dayOfYear = new DateTime(date.year, date.month, date.day).DayOfYear;
		float mean = (jan + jul) / 2f + (jul - jan) / 2f * Mathf.Cos(Mathf.Tau * (dayOfYear - 200) / 365f);
		uint h = Portraits.Hash($"weather:{date.year}-{date.month}-{date.day}:{regionId}");
		float swing = ((h % 17u) - 8f);
		int high = Mathf.RoundToInt(mean + 7f + swing), low = Mathf.RoundToInt(mean - 8f + swing * 0.7f);
		uint sky = (h / 17u) % 7u;
		string[] mild = { "Fair", "Clear and pleasant", "Partly cloudy", "Cloudy", "Hazy sun", "Showers by evening", "Overcast, a chance of rain" };
		string[] cold = { "Fair and cold", "Cloudy and raw", "Snow flurries", "Light snow by evening", "Clear, with a hard frost", "Overcast and bitter", "Sleet at times" };
		string[] hot = { "Fair and hot", "Hazy, hot and close", "Thunderstorms by evening", "Clear and warm", "Sultry, a chance of showers", "Partly cloudy and hot", "Fair, heat building" };
		string text = (high < 40 ? cold : high > 82 ? hot : mild)[sky];
		return $"{(string.IsNullOrEmpty(town) ? "" : town.ToUpperInvariant() + ": ")}{text}. High {high}, low {low}.";
	}

	/// <summary>The price of ordinary things that morning: what a record costs against what a stamp, a gallon and a loaf do.
	/// First-class postage is dated correctly (4c to January 1963, 5c to January 1968, then 6c).</summary>
	public static List<(string Item, string Price)> Prices(GameDate date) {
		float t = Mathf.Clamp((date.year - 1960 + (date.month - 1) / 12f) / 10f, 0f, 1f);
		int stamp = date.year < 1963 ? 4 : date.year < 1968 ? 5 : 6;
		int gas = Mathf.RoundToInt(Mathf.Lerp(25f, 35f, t)), bread = Mathf.RoundToInt(Mathf.Lerp(20f, 24f, t));
		var all = new List<(string, string)> {
			("A 45 rpm single", "69¢ to 98¢"), ("An LP, mono", "$3.98"), ("An LP, stereo", "$4.98"), ("Jukebox, three plays", "25¢"),
			("First-class stamp", stamp + "¢"), ("Gasoline, a gallon", gas + "¢"), ("A loaf of bread", bread + "¢"), ("A cup of coffee", date.year < 1966 ? "10¢" : "15¢"),
		};
		uint h = Portraits.Hash($"prices:{date.year}-{date.month}-{date.day}");
		// Always the record prices and the stamp; two of the rest by the day.
		var chosen = new List<(string, string)> { all[0], all[1], all[4] };
		chosen.Add(all[(int)(h % 3u) + 5]);
		if (h % 2u == 0) chosen.Insert(2, all[3]);
		return chosen;
	}

	/// <summary>The three fillers a quiet day gets, as controls: the weather, the price board, an ad.</summary>
	public static Control Weather(GameDate date, AILabel label, Color ink, Color rust) {
		var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 2);
		box.AddChild(PaperRuleLine());
		box.AddChild(Text("THE WEATHER", 15, rust, PaperTheme.SansSemiBold));
		string regionName = label?.headquartersCity;
		box.AddChild(Text(WeatherLine(date, label?.homeRegion, regionName), 17, ink));
		return box;
	}

	public static Control PriceBoard(GameDate date, Color ink, Color rust) {
		var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", 1);
		box.AddChild(PaperRuleLine());
		box.AddChild(Text("ON THE BOARD", 15, rust, PaperTheme.SansSemiBold));
		foreach ((string item, string price) in Prices(date)) {
			var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			var name = Text(item, 16, ink);
			name.AutowrapMode = TextServer.AutowrapMode.Off;
			row.AddChild(name);
			var dots = new Label { Text = new string('.', 22), ClipText = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
			dots.AddThemeColorOverride("font_color", new Color(Rule, 0.8f));
			dots.AddThemeFontSizeOverride("font_size", 14);
			row.AddChild(dots);
			var cost = Text(price, 16, ink, PaperTheme.TypedBold, HorizontalAlignment.Right);
			cost.AutowrapMode = TextServer.AutowrapMode.Off;
			cost.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
			row.AddChild(cost);
			box.AddChild(row);
		}
		return box;
	}

	/// <summary>A small display ad, rotating by the day: the plant, a publisher, a supplier, the diner across the street.</summary>
	public static Control Ad(GameDate date, AILabel label, Color ink) {
		uint h = Portraits.Hash($"ad:{date.year}-{date.month}-{date.day}");
		string plant = RolodexDirectory.PlantFirm(label);
		string foreman = RolodexDirectory.PersonName("plant:" + (label?.labelId ?? ""));
		var publishers = CompositionCatalogService.Publishers;
		MusicPublisher house = publishers.Count > 0 ? publishers[(int)(h % (uint)publishers.Count)] : null;
		var ads = new List<(string Head, string Line, string Small)> {
			(plant.ToUpperInvariant(), "45s in quantity. Plating, labels and sleeves, one price, no surprises.", $"Ask for {foreman}. Orders in by the first of the month."),
			("MASTERS CUT WHILE YOU WAIT", "Lacquers and acetates, mono, any hour a musician is awake.", "Dial the answering service. Rates on request."),
			("WE BUY OVERSTOCK", "45s, LPs and jukebox discs. Cash. No questions about the artist.", "Write Box 41 of this paper."),
			("OPEN LATE FOR THE BAND", "Eggs any style and coffee until the last set lets out.", "Across from the Hotel Lenox. Ask for Betty."),
		};
		if (house != null) ads.Insert(1, (house.publisherName.ToUpperInvariant(), "Songs for every voice, from staff writers who know the charts.", "See the professional manager. Appointments only."));
		var pick = ads[(int)((h / 7u) % (uint)ads.Count)];

		var frame = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color(1f, 1f, 1f, 0.10f), BorderColor = ink,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 9
		});
		var col = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		col.AddThemeConstantOverride("separation", 3);
		frame.AddChild(col);
		col.AddChild(Text(pick.Head, 19, ink, PaperTheme.Lettering(LetteringStyle.HeavySlab), HorizontalAlignment.Center));
		col.AddChild(Text(pick.Line, 15, ink, PaperTheme.SerifItalic, HorizontalAlignment.Center));
		col.AddChild(Text(pick.Small, 11, Rule.Darkened(0.3f), PaperTheme.SansSemiBold, HorizontalAlignment.Center));
		return frame;
	}

	private static Control PaperRuleLine() => new ColorRect { Color = Rule, CustomMinimumSize = new Vector2(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };
}
