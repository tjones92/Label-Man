using System;
using System.Collections.Generic;
using Godot;

/// <summary>A street atlas for the record-shop route. Shop pins represent fictional counters in real districts.</summary>
public partial class CityShopMap : Control {
	private sealed record Place(string Name, float X, float Y);
	private sealed record Sheet(Vector2[][] Water, Vector2[][] Streets, Place[] Labels, Vector2[] ShopPins, Vector2[][] Rivers = null);

	// Detroit Historical Society central-area map, Chicago Public Library street guide (1964),
	// and the Library of Congress's 1964 New York City street map. Geometry is schematic.
	private static readonly Dictionary<string, Sheet> Sheets = new(StringComparer.Ordinal) {
		["detroit"] = new(
			new[] { Points(0,.94f, .24f,.95f, .47f,.88f, .66f,.79f, .83f,.64f, 1,.48f, 1,1, 0,1) },
			new[] { Points(.51f,.76f, .48f,.51f, .40f,0), Points(.51f,.76f, .24f,.64f, 0,.60f),
				Points(.51f,.76f, .32f,.48f, .06f,0), Points(.51f,.76f, .71f,.52f, 1,.21f),
				Points(0,.84f, .34f,.82f, .63f,.69f, 1,.38f), Points(.10f,.33f, .78f,.22f, .91f,.34f) },
			new[] { new Place("CORKTOWN",.12f,.72f), new Place("DOWNTOWN",.53f,.83f),
				new Place("CULTURAL CENTER",.49f,.32f), new Place("EAST SIDE",.77f,.30f),
				new Place("WOODWARD",.34f,.12f), new Place("MICHIGAN AVE.",.08f,.59f), new Place("DETROIT RIVER",.70f,.94f) },
			Points(.53f,.73f, .38f,.71f, .61f,.69f, .23f,.65f, .45f,.54f, .65f,.56f, .28f,.49f, .76f,.48f, .53f,.49f,
				.16f,.46f, .34f,.35f, .60f,.39f, .85f,.41f, .25f,.23f, .47f,.22f, .69f,.26f, .18f,.14f, .83f,.18f)),
		["chicago"] = new(
			new[] { Points(.79f,0, .80f,.22f, .83f,.36f, .87f,.55f, .90f,.76f, .94f,1, 1,1, 1,0) },
			new[] { Points(.65f,0, .65f,1), Points(.52f,0, .52f,1), Points(.31f,0, .31f,1),
				Points(0,.25f, .79f,.25f), Points(0,.46f, .85f,.46f), Points(0,.65f, .88f,.65f), Points(0,.83f, .91f,.83f) },
			new[] { new Place("NEAR NORTH SIDE",.56f,.15f), new Place("THE LOOP",.55f,.57f),
				new Place("WEST SIDE",.14f,.53f), new Place("SOUTH SIDE",.52f,.91f),
				new Place("MICHIGAN AVE.",.67f,.68f), new Place("CHICAGO RIVER",.61f,.36f), new Place("LAKE MICHIGAN",.84f,.14f) },
			Points(.60f,.45f, .71f,.43f, .44f,.46f, .29f,.44f, .67f,.30f, .55f,.26f, .46f,.23f, .22f,.31f, .33f,.25f,
				.36f,.57f, .51f,.64f, .64f,.62f, .75f,.59f, .22f,.68f, .42f,.75f, .60f,.77f, .73f,.79f, .32f,.87f),
			new[] { Points(.48f,0, .48f,.24f, .40f,.35f, .86f,.37f), Points(.40f,.35f, .34f,.59f, .17f,.75f, 0,.79f) }),
		["new_york"] = new(
			new[] { Points(0,0, .56f,0, .55f,.16f, .45f,.35f, .33f,.56f, .20f,.77f, .16f,.91f, .23f,1, 0,1),
				Points(.86f,0, .89f,.19f, .76f,.38f, .66f,.48f, .65f,.61f, .52f,.83f, .36f,.99f, .26f,1,
					.62f,1, .69f,.86f, .78f,.73f, .88f,.46f, 1,.28f, 1,0) },
			new[] { Points(.29f,.94f, .36f,.77f, .45f,.58f, .54f,.48f, .63f,.33f, .77f,.08f),
				Points(.35f,.94f, .43f,.78f, .51f,.59f, .63f,.41f, .75f,.25f, .83f,.07f),
				Points(.25f,.77f, .52f,.86f), Points(.31f,.63f, .61f,.72f), Points(.38f,.49f, .66f,.56f), Points(.55f,.18f, .82f,.29f) },
			new[] { new Place("HARLEM",.72f,.13f), new Place("CENTRAL PARK",.56f,.35f), new Place("MIDTOWN",.47f,.55f),
				new Place("GREENWICH VILLAGE",.27f,.72f), new Place("LOWER MANHATTAN",.05f,.91f),
				new Place("HUDSON RIVER",.10f,.35f), new Place("EAST RIVER",.77f,.63f), new Place("BROOKLYN",.78f,.92f) },
			Points(.36f,.87f, .43f,.80f, .31f,.81f, .48f,.69f, .39f,.65f, .57f,.61f, .47f,.48f, .59f,.49f, .67f,.42f,
				.46f,.40f, .69f,.34f, .56f,.25f, .65f,.21f, .78f,.23f, .62f,.13f, .79f,.10f, .81f,.82f, .75f,.88f))
	};

	private readonly List<Button> pins = new();
	private Sheet sheet;
	private static readonly Color Land = new("eee1bd"), WaterInk = new("bdd0c5"), Road = new("aa9268"), MapInk = new("624e2f");

	public static bool HasSheet(string cityId) => Sheets.ContainsKey(cityId);

	public void SetShops(string cityId, IReadOnlyList<PlayerDesk.PlayerStop> shops, string selectedId, Func<string, string> orderAt, Action<string> select) {
		sheet = Sheets[cityId];
		CustomMinimumSize = new Vector2(420, 340);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = true;
		Resized += PositionPins;
		foreach (var shop in shops) {
			string stopId = shop.StopId;
			bool chosen = stopId == selectedId;
			string order = orderAt(stopId);
			var pin = new Button {
				Name = "ShopPin_" + stopId, Text = order != null ? "▣" : chosen ? "◉" : "●",
				TooltipText = order == null ? shop.DisplayName : $"{shop.DisplayName}\n{order}",
				Size = new Vector2(30, 30), MouseDefaultCursorShape = CursorShape.PointingHand
			};
			pin.AddThemeStyleboxOverride("normal", PinStyle(chosen ? PaperTheme.Highlighter : PaperTheme.Field));
			pin.AddThemeStyleboxOverride("hover", PinStyle(PaperTheme.Highlighter));
			pin.AddThemeStyleboxOverride("pressed", PinStyle(PaperTheme.Highlighter));
			pin.AddThemeColorOverride("font_color", PaperTheme.Oxblood);
			pin.AddThemeFontSizeOverride("font_size", 15);
			pin.Pressed += () => select(stopId);
			pins.Add(pin);
			AddChild(pin);
		}
		PositionPins();
	}

	private static StyleBoxFlat PinStyle(Color fill) => new() {
		BgColor = fill, BorderColor = PaperTheme.Oxblood,
		BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
		CornerRadiusTopLeft = 15, CornerRadiusTopRight = 15, CornerRadiusBottomLeft = 15, CornerRadiusBottomRight = 15,
		ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0
	};

	private static Vector2[] Points(params float[] xy) {
		var points = new Vector2[xy.Length / 2];
		for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
		return points;
	}

	private Vector2[] FitToMap(Vector2[] points) => Array.ConvertAll(points, p => p * Size);

	private void PositionPins() {
		for (int i = 0; i < pins.Count; i++) pins[i].Position = sheet.ShopPins[i] * Size - pins[i].Size / 2;
		QueueRedraw();
	}

	public override void _Draw() {
		if (sheet == null) return;
		var bounds = new Rect2(Vector2.Zero, Size);
		DrawRect(bounds, Land);
		foreach (var water in sheet.Water) DrawColoredPolygon(FitToMap(water), WaterInk);
		foreach (var river in sheet.Rivers ?? Array.Empty<Vector2[]>()) DrawPolyline(FitToMap(river), WaterInk, 9, true);
		foreach (var street in sheet.Streets) DrawPolyline(FitToMap(street), Road, 2, true);
		var font = PaperTheme.TypedBold;
		foreach (var label in sheet.Labels) {
			float width = font.GetStringSize(label.Name, fontSize: 12).X;
			float left = Mathf.Min(label.X * Size.X, Size.X - width - 8);
			DrawString(font, new Vector2(left, label.Y * Size.Y), label.Name, fontSize: 12, modulate: MapInk);
		}
		DrawRect(bounds.Grow(-0.5f), PaperTheme.Edge, false, 1);
	}
}
