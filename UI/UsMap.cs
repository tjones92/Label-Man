using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// A printed road-atlas map of the lower 48: states in the office's ink on paper, each sales region in its own pale tint,
/// and the game's towns as dots placed from their real latitude and longitude. Clicking a town raises
/// <see cref="CityPressed"/>; hovering shows its name and the caller's note for it.
///
/// The outlines are U.S. Census Bureau cartographic boundaries (via us-atlas 3.0.1, states-10m), simplified once per shared
/// border so neighbours meet exactly; see Data/Map/UsStates.json. Projection: Albers equal-area conic on the standard US
/// parallels (29.5°N, 45.5°N), centred on 96°W, 37.5°N -- the familiar atlas shape.
/// </summary>
public partial class UsMap : Control {
	private const string DataPath = "res://Data/Map/UsStates.json";

	// ---- what the caller sets ---------------------------------------------------------------------------------------

	/// <summary>Raised with a town's id when the player clicks it.</summary>
	public event Action<string> CityPressed;
	/// <summary>The town drawn as chosen (oxblood ring, bold label).</summary>
	public string SelectedCityId { get => selected; set { selected = value; QueueRedraw(); } }
	/// <summary>Where the player is now (a filled marker with a "you" flag).</summary>
	public string HereCityId { get => here; set { here = value; QueueRedraw(); } }
	/// <summary>The label's home office (a small square).</summary>
	public string HomeCityId { get => home; set { home = value; QueueRedraw(); } }
	/// <summary>Towns to draw; null draws every town in the game.</summary>
	public ICollection<string> Cities { get => cities; set { cities = value; labels = null; QueueRedraw(); } }
	/// <summary>Towns that can't be picked right now (drawn hollow and faded, not clickable).</summary>
	public ICollection<string> Unavailable { get => unavailable; set { unavailable = value ?? Array.Empty<string>(); QueueRedraw(); } }
	/// <summary>Towns marked as worked (a tick-ring around the dot).</summary>
	public ICollection<string> Marked { get => marked; set { marked = value ?? Array.Empty<string>(); QueueRedraw(); } }
	/// <summary>A one-line note per town for the hover tip (drive time, why it's closed...).</summary>
	public Func<string, string> CityNote { get; set; }
	/// <summary>Optional emphasis per region id: null leaves the region its plain tint.</summary>
	public Func<string, Color?> RegionEmphasis { get; set; }
	/// <summary>The region drawn picked out (its tint deepens and its edge is inked).</summary>
	public string HighlightRegionId { get => highlightRegion; set { highlightRegion = value; QueueRedraw(); } }

	private string selected, here, home, highlightRegion, hovered;
	private ICollection<string> cities, unavailable = Array.Empty<string>(), marked = Array.Empty<string>();

	// ---- palette ------------------------------------------------------------------------------------------------------

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Water = new("dfe3d2");
	private static readonly Color Oxblood = new("8f2a22");
	private static readonly Color Faded = new("8a7a5a");
	/// <summary>Pale atlas tints, one per sales region; neighbouring regions never share a hue family.</summary>
	public static readonly IReadOnlyDictionary<string, Color> RegionTint = new Dictionary<string, Color> {
		["eastcoast"] = new("e9d3a8"), ["greatlakes"] = new("d9dfb4"), ["greatplains"] = new("ecdcb6"),
		["deepsouth"] = new("e6c6a6"), ["southwest"] = new("ecd09a"), ["rockies"] = new("d3d6b8"), ["westcoast"] = new("e2cfb0")
	};

	// ---- shape data (loaded once) ----------------------------------------------------------------------------------

	private sealed class Ring { public Vector2[] Geo; public int[] Triangles; }
	private sealed class State { public string Name, Region; public List<Ring> Rings = new(); }
	private static List<State> states;
	private static Rect2 projectedBounds;

	private static void EnsureLoaded() {
		if (states != null) return;
		states = new List<State>();
		using var file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
		if (file == null) { GD.PushWarning("UsMap: missing " + DataPath); return; }
		using var doc = JsonDocument.Parse(file.GetAsText());
		bool first = true;
		foreach (var s in doc.RootElement.GetProperty("states").EnumerateArray()) {
			var state = new State { Name = s.GetProperty("name").GetString(), Region = s.GetProperty("region").GetString() };
			foreach (var r in s.GetProperty("rings").EnumerateArray()) {
				var flat = r.EnumerateArray().Select(v => v.GetSingle()).ToArray();
				var pts = new Vector2[flat.Length / 2];
				for (int i = 0; i < pts.Length; i++) {
					pts[i] = Project(flat[2 * i + 1], flat[2 * i]);
					if (first) { projectedBounds = new Rect2(pts[i], Vector2.Zero); first = false; } else projectedBounds = projectedBounds.Expand(pts[i]);
				}
				// Triangulated once here; a ring the triangulator can't take (a simplification pinch) keeps its outline only.
				state.Rings.Add(new Ring { Geo = pts, Triangles = Geometry2D.TriangulatePolygon(pts) });
			}
			states.Add(state);
		}
	}

	/// <summary>Albers equal-area conic, US standard parallels. Returns unit-sphere plane coords with y pointing down.</summary>
	public static Vector2 Project(float latitude, float longitude) {
		const double phi1 = 29.5 * Math.PI / 180, phi2 = 45.5 * Math.PI / 180, phi0 = 37.5 * Math.PI / 180, lambda0 = -96 * Math.PI / 180;
		double n = (Math.Sin(phi1) + Math.Sin(phi2)) / 2, c = Math.Cos(phi1) * Math.Cos(phi1) + 2 * n * Math.Sin(phi1);
		double rho0 = Math.Sqrt(c - 2 * n * Math.Sin(phi0)) / n;
		double phi = latitude * Math.PI / 180, lambda = longitude * Math.PI / 180;
		double rho = Math.Sqrt(c - 2 * n * Math.Sin(phi)) / n, theta = n * (lambda - lambda0);
		return new Vector2((float)(rho * Math.Sin(theta)), -(float)(rho0 - rho * Math.Cos(theta)));
	}

	// ---- layout -----------------------------------------------------------------------------------------------------

	private float scale; private Vector2 offset;
	private Vector2 ToScreen(Vector2 p) => offset + (p - projectedBounds.Position) * scale;

	private void Fit() {
		EnsureLoaded();
		var area = new Rect2(Vector2.Zero, Size).Grow(-12);
		if (projectedBounds.Size.X <= 0 || area.Size.X <= 0) { scale = 0; return; }
		scale = Mathf.Min(area.Size.X / projectedBounds.Size.X, area.Size.Y / projectedBounds.Size.Y);
		offset = area.Position + (area.Size - projectedBounds.Size * scale) / 2;
	}

	private IEnumerable<MarketCity> Towns() => DistanceModel.GetCities().Where(c => cities == null || cities.Contains(c.cityId));
	private Vector2 TownPoint(MarketCity city) { var (lat, lon) = DistanceModel.LatLon(city); return ToScreen(Project(lat, lon)); }

	public UsMap() {
		MouseFilter = MouseFilterEnum.Stop;
		CustomMinimumSize = new Vector2(480, 300);
		TooltipText = " ";   // non-empty so Godot asks _GetTooltip
		Resized += () => { labels = null; QueueRedraw(); };
	}

	// ---- drawing ----------------------------------------------------------------------------------------------------

	public override void _Draw() {
		Fit();
		if (scale <= 0) return;
		var font = PaperTheme.Sans; var bold = PaperTheme.SansBold;
		DrawRect(new Rect2(Vector2.Zero, Size), Water);
		foreach (var state in states) {
			Color fill = RegionTint.TryGetValue(state.Region, out var tint) ? tint : new Color("e8dcc0");
			if (RegionEmphasis?.Invoke(state.Region) is Color emphasis) fill = fill.Lerp(emphasis, 0.55f);
			if (state.Region == highlightRegion) fill = fill.Darkened(0.12f);
			foreach (var ring in state.Rings) {
				var pts = ring.Geo.Select(ToScreen).ToArray();
				if (ring.Triangles.Length > 0) RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), ring.Triangles, pts, new[] { fill });
				var closed = pts.Append(pts[0]).ToArray();
				DrawPolyline(closed, new Color(Ink, state.Region == highlightRegion ? 0.75f : 0.38f), state.Region == highlightRegion ? 1.6f : 1f, true);
			}
		}
		// Towns: dot, then label where it fits.
		labels ??= PlaceLabels(font, bold);
		foreach (var city in Towns()) {
			var p = TownPoint(city);
			bool off = unavailable.Contains(city.cityId), chosen = city.cityId == selected, hot = city.cityId == hovered;
			float r = city.isRegionalHub ? 5.5f : 4f;
			if (chosen || hot) DrawCircle(p, r + 6, new Color(Oxblood, chosen ? 0.22f : 0.12f));
			if (marked.Contains(city.cityId)) DrawArc(p, r + 3, 0, Mathf.Tau, 20, new Color("315e3c"), 2f, true);
			if (city.cityId == home) DrawRect(new Rect2(p - new Vector2(r + 1, r + 1), new Vector2(2 * r + 2, 2 * r + 2)), Ink, false, 1.5f);
			if (off) DrawArc(p, r, 0, Mathf.Tau, 16, Faded, 1.5f, true);
			else DrawCircle(p, r, chosen ? Oxblood : Ink);
			if (city.cityId == here) {
				DrawLine(p, p + new Vector2(0, -18), Ink, 1.5f, true);
				DrawColoredPolygon(new[] { p + new Vector2(0, -18), p + new Vector2(12, -14), p + new Vector2(0, -10) }, Oxblood);
			}
		}
		foreach (var (cityId, rect, text, isBold) in labels) {
			bool chosen = cityId == selected || cityId == hovered;
			var color = unavailable.Contains(cityId) ? Faded : chosen ? Oxblood : Ink;
			// A paper halo keeps the name readable over borders.
			var f = chosen || isBold ? bold : font; int size = chosen ? 14 : 12;
			var baseline = rect.Position + new Vector2(0, rect.Size.Y - 3);
			DrawStringOutline(f, baseline, text, HorizontalAlignment.Left, -1, size, 4, new Color("f3e8cc", 0.85f));
			DrawString(f, baseline, text, HorizontalAlignment.Left, -1, size, color);
		}
		DrawRect(new Rect2(Vector2.Zero, Size), new Color(Ink, 0.6f), false, 1.5f);
	}

	private List<(string CityId, Rect2 Rect, string Text, bool Bold)> labels;

	/// <summary>Greedy label placement: chosen and hub towns first, each tried right, left, above and below its dot;
	/// a label that would overlap another (or a dot) is left off and the name stays on hover.</summary>
	private List<(string, Rect2, string, bool)> PlaceLabels(Font font, Font bold) {
		var placed = new List<(string, Rect2, string, bool)>();
		var taken = Towns().Select(c => { var p = TownPoint(c); return new Rect2(p - new Vector2(5, 5), new Vector2(10, 10)); }).ToList();
		foreach (var city in Towns().OrderByDescending(c => c.cityId == selected || c.cityId == here).ThenByDescending(c => c.isRegionalHub)
				.ThenBy(c => c.distributionTier).ThenBy(c => c.name, StringComparer.Ordinal)) {
			bool important = city.cityId == selected || city.cityId == here || city.cityId == hovered;
			var f = important || city.isRegionalHub ? bold : font; int size = important ? 14 : 12;
			var textSize = f.GetStringSize(city.name, HorizontalAlignment.Left, -1, size);
			var p = TownPoint(city);
			var tries = new[] { p + new Vector2(9, -textSize.Y / 2), p + new Vector2(-9 - textSize.X, -textSize.Y / 2),
				p + new Vector2(-textSize.X / 2, -textSize.Y - 7), p + new Vector2(-textSize.X / 2, 7) };
			foreach (var at in tries) {
				var rect = new Rect2(at, textSize);
				if (!new Rect2(Vector2.Zero, Size).Encloses(rect)) continue;
				if (taken.Any(t => t.Intersects(rect.Grow(1)) && !t.HasPoint(p))) continue;
				placed.Add((city.cityId, rect, city.name, city.isRegionalHub)); taken.Add(rect); break;
			}
		}
		return placed;
	}

	// ---- input ------------------------------------------------------------------------------------------------------

	private string CityAt(Vector2 at) {
		Fit();
		MarketCity best = null; float bestDistance = 14;
		foreach (var city in Towns()) {
			float d = TownPoint(city).DistanceTo(at);
			if (d < bestDistance) { best = city; bestDistance = d; }
		}
		if (best != null) return best.cityId;
		// A click on a town's label counts as a click on the town.
		return labels?.FirstOrDefault(l => l.Rect.Grow(2).HasPoint(at)).CityId;
	}

	/// <summary>The sales region under a point, from the state outlines.</summary>
	public string RegionAt(Vector2 at) {
		Fit();
		foreach (var state in states)
			foreach (var ring in state.Rings)
				if (Geometry2D.IsPointInPolygon(at, ring.Geo.Select(ToScreen).ToArray())) return state.Region;
		return null;
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseMotion motion) {
			string now = CityAt(motion.Position);
			if (now != hovered) { hovered = now; labels = null; QueueRedraw(); }
			MouseDefaultCursorShape = now != null && !unavailable.Contains(now) ? CursorShape.PointingHand : CursorShape.Arrow;
		} else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) {
			string city = CityAt(click.Position);
			if (city != null && !unavailable.Contains(city)) { CityPressed?.Invoke(city); AcceptEvent(); }
		}
	}

	public override void _Notification(int what) {
		if (what == NotificationMouseExit && hovered != null) { hovered = null; labels = null; QueueRedraw(); }
	}

	public override string _GetTooltip(Vector2 atPosition) {
		string cityId = CityAt(atPosition);
		if (cityId == null) return "";
		var city = DistanceModel.GetCityById(cityId);
		string note = CityNote?.Invoke(cityId);
		return city.name + (string.IsNullOrEmpty(note) ? "" : "\n" + note);
	}
}
