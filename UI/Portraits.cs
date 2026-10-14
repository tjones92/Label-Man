using System;
using System.Collections.Generic;
using Godot;

/// <summary>What a head-and-shoulders silhouette is wearing. The shapes are fixed; the combination is chosen from the
/// act's genre, who is in the band and a hash of its id, so the same act always has the same face and two acts in one
/// genre rarely match.</summary>
public enum PortraitHair { Short, Pompadour, MopTop, Beehive, Bob, Long, Bald }
public enum PortraitHat { None, Stetson, Fedora, Beret, FlatCap }
public enum PortraitWear { Suit, OpenCollar, Robe, Apron }

public sealed class PortraitFigure {
	public bool Female;
	public PortraitHair Hair;
	public PortraitHat Hat;
	public PortraitWear Wear;
	public bool Shades, Tie, Headphones, Mic;
}

/// <summary>
/// Halftone portraits. The acts have no photographs, so each gets a newspaper-screen silhouette: a head and shoulders,
/// or three for a group, struck as a 45-degree dot screen in the office ink on a paper mat. It is the look of a
/// publicity glossy run through a trade-paper plate, which is exactly what a label would pin to a card.
///
/// Everything is geometry tested per dot, not per pixel, and the result is baked once per (key, size) into an
/// <see cref="ImageTexture"/>. Nothing here touches a random stream: variation is an FNV hash of the key.
/// </summary>
public static class Portraits {
	private static readonly Dictionary<string, ImageTexture> Cache = new();

	/// <summary>FNV-1a over a string: stable across runs and machines (string.GetHashCode is not).</summary>
	public static uint Hash(string text) {
		uint h = 2166136261u;
		foreach (char c in text ?? "") { h ^= c; h *= 16777619u; }
		return h;
	}

	private static float Unit(uint hash, int salt) => (((hash >> (salt * 5 % 20)) ^ (hash * (uint)(salt * 2 + 1))) % 1000u) / 1000f;

	// ---- who wears what -------------------------------------------------------------------------------

	/// <summary>The look for one member of an act, chosen from genre, gender and the act's id.</summary>
	public static PortraitFigure FigureFor(Genre genre, bool female, string key, int slot = 0) {
		uint h = Hash(key + "#" + slot);
		var figure = new PortraitFigure { Female = female, Wear = PortraitWear.Suit, Tie = !female };
		float roll = Unit(h, 1), roll2 = Unit(h, 2), roll3 = Unit(h, 3);
		switch (genre) {
			case Genre.Country: case Genre.CountryRock: case Genre.TexMex: case Genre.RootsRock:
				figure.Hat = female ? (roll < 0.35f ? PortraitHat.Stetson : PortraitHat.None) : (roll < 0.8f ? PortraitHat.Stetson : PortraitHat.None);
				figure.Hair = female ? PortraitHair.Bob : PortraitHair.Short;
				figure.Wear = PortraitWear.OpenCollar; figure.Tie = false;
				break;
			case Genre.RockAndRoll: case Genre.SurfRock: case Genre.GarageRock: case Genre.DooWop:
				figure.Hair = female ? PortraitHair.Beehive : PortraitHair.Pompadour;
				figure.Wear = roll2 < 0.5f ? PortraitWear.OpenCollar : PortraitWear.Suit; figure.Tie = !female && roll2 > 0.8f;
				break;
			case Genre.TeenPop: case Genre.Bubblegum: case Genre.GirlGroup: case Genre.SunshinePop: case Genre.PopRock:
				figure.Hair = female ? (roll < 0.5f ? PortraitHair.Beehive : PortraitHair.Bob) : PortraitHair.Short;
				figure.Tie = !female && roll2 < 0.4f;
				break;
			case Genre.Jazz: case Genre.Blues: case Genre.BossaNova: case Genre.BluesRock: case Genre.BritishBlues:
				figure.Hat = roll < 0.55f ? PortraitHat.Beret : PortraitHat.None;
				figure.Shades = roll2 < 0.6f;
				figure.Hair = female ? PortraitHair.Bob : PortraitHair.Short;
				figure.Wear = PortraitWear.OpenCollar; figure.Tie = false;
				break;
			case Genre.RnB: case Genre.Soul: case Genre.Motown: case Genre.Funk: case Genre.Boogaloo:
				figure.Hair = female ? PortraitHair.Beehive : (roll < 0.5f ? PortraitHair.Pompadour : PortraitHair.Short);
				figure.Tie = !female; figure.Shades = !female && roll2 < 0.18f;
				break;
			case Genre.Gospel:
				figure.Wear = PortraitWear.Robe; figure.Tie = false;
				figure.Hair = female ? PortraitHair.Bob : PortraitHair.Short;
				break;
			case Genre.Folk: case Genre.ContemporaryFolk: case Genre.SingerSongwriter: case Genre.Skiffle: case Genre.FolkRock:
				figure.Hat = !female && roll < 0.45f ? PortraitHat.FlatCap : PortraitHat.None;
				figure.Hair = female ? PortraitHair.Long : PortraitHair.Short;
				figure.Wear = PortraitWear.OpenCollar; figure.Tie = false;
				break;
			case Genre.BritishBeat: case Genre.BritishPop: case Genre.BritishInvasion:
				figure.Hair = female ? PortraitHair.Bob : PortraitHair.MopTop;
				break;
			case Genre.Psychedelic: case Genre.PsychedelicRock: case Genre.PsychedelicPop: case Genre.AcidRock: case Genre.ProgressiveRock:
			case Genre.HardRock: case Genre.ProtoPunk: case Genre.ProtoMetal:
				figure.Hair = PortraitHair.Long; figure.Wear = PortraitWear.OpenCollar; figure.Tie = false;
				figure.Shades = roll < 0.3f;
				break;
			case Genre.Classical: case Genre.TraditionalPop: case Genre.EasyListening:
				figure.Hair = female ? PortraitHair.Bob : (roll3 < 0.15f ? PortraitHair.Bald : PortraitHair.Short);
				figure.Hat = !female && genre == Genre.TraditionalPop && roll < 0.3f ? PortraitHat.Fedora : PortraitHat.None;
				break;
			case Genre.Comedy: case Genre.Childrens:
				figure.Hat = roll < 0.5f ? PortraitHat.Fedora : PortraitHat.None;
				figure.Hair = female ? PortraitHair.Bob : PortraitHair.Short;
				break;
			default:
				figure.Hair = female ? (roll < 0.5f ? PortraitHair.Bob : PortraitHair.Beehive) : PortraitHair.Short;
				figure.Hat = !female && roll2 < 0.12f ? PortraitHat.Fedora : PortraitHat.None;
				break;
		}
		return figure;
	}

	/// <summary>A reporter on a rock station: headphones, a microphone, sometimes a hat.</summary>
	public static PortraitFigure Announcer(string key, DJArchetype archetype) {
		uint h = Hash(key);
		var figure = new PortraitFigure { Female = false, Hair = PortraitHair.Short, Wear = PortraitWear.Suit, Tie = true, Mic = true };
		figure.Headphones = archetype == DJArchetype.Personality || archetype == DJArchetype.Hustler || Unit(h, 1) < 0.35f;
		figure.Shades = archetype == DJArchetype.Tastemaker && Unit(h, 2) < 0.7f;
		figure.Hat = archetype == DJArchetype.Regional && Unit(h, 3) < 0.7f ? PortraitHat.Stetson : archetype == DJArchetype.Hustler && Unit(h, 3) < 0.5f ? PortraitHat.Fedora : PortraitHat.None;
		figure.Hair = archetype == DJArchetype.Personality && Unit(h, 4) < 0.6f ? PortraitHair.Pompadour : PortraitHair.Short;
		figure.Tie = archetype == DJArchetype.CompanyMan;
		if (figure.Headphones) figure.Hat = PortraitHat.None;
		return figure;
	}

	/// <summary>A man behind a desk or a counter: a manager, a plugger, a plant foreman, a dealer.</summary>
	public static PortraitFigure Businessman(string key, bool female = false) {
		uint h = Hash(key);
		var figure = new PortraitFigure { Female = female, Hair = female ? PortraitHair.Bob : (Unit(h, 1) < 0.2f ? PortraitHair.Bald : PortraitHair.Short), Wear = PortraitWear.Suit, Tie = !female };
		figure.Hat = !female && Unit(h, 2) < 0.28f ? PortraitHat.Fedora : PortraitHat.None;
		figure.Shades = !female && Unit(h, 3) < 0.12f;
		return figure;
	}

	public static PortraitFigure Foreman(string key) {
		uint h = Hash(key);
		return new PortraitFigure { Hair = Unit(h, 1) < 0.3f ? PortraitHair.Bald : PortraitHair.Short, Hat = Unit(h, 2) < 0.6f ? PortraitHat.FlatCap : PortraitHat.None, Wear = PortraitWear.Apron };
	}

	/// <summary>The portrait of a signed or watched act: one silhouette per active member up to three.</summary>
	public static ImageTexture ForAct(SimulatedArtist artist, int width = 240, int height = 300) {
		if (artist == null) return ForFigures("blank", new List<PortraitFigure> { Businessman("blank") }, width, height);
		var people = new List<Musician>();
		foreach (Musician member in artist.members) if (member.isActive) people.Add(member);
		if (people.Count == 0) people.AddRange(artist.members);
		var figures = new List<PortraitFigure>();
		int count = Math.Max(1, Math.Min(3, people.Count));
		for (int i = 0; i < count; i++) {
			bool female = people.Count > i ? !people[i].isMale : false;
			figures.Add(FigureFor(artist.primaryGenre, female, artist.artistId ?? artist.stageName, i));
		}
		return ForFigures($"act:{artist.artistId}:{artist.primaryGenre}:{count}:{string.Concat(figures.ConvertAll(f => f.Female ? 'f' : 'm'))}", figures, width, height);
	}

	public static ImageTexture ForFigures(string key, List<PortraitFigure> figures, int width = 240, int height = 300) {
		string cacheKey = $"{key}@{width}x{height}";
		if (Cache.TryGetValue(cacheKey, out ImageTexture cached)) return cached;
		var texture = Bake(key, figures, width, height);
		Cache[cacheKey] = texture;
		return texture;
	}

	// ---- geometry -------------------------------------------------------------------------------------

	private enum Region { None, Face, Hair, Hat, Body, Shirt, Tie, Shades, Neck, Gear }

	private static bool InEllipse(float u, float v, float cx, float cy, float rx, float ry) {
		float dx = (u - cx) / rx, dy = (v - cy) / ry;
		return dx * dx + dy * dy <= 1f;
	}

	private static bool InRect(float u, float v, float x0, float y0, float x1, float y1) => u >= x0 && u <= x1 && v >= y0 && v <= y1;

	/// <summary>Classifies a point of ONE figure drawn in its own unit square (head near the top, shoulders at the foot).</summary>
	private static Region Classify(PortraitFigure f, float u, float v) {
		float headRx = f.Female ? 0.150f : 0.168f;
		float headRy = f.Female ? 0.200f : 0.208f;
		const float headY = 0.37f;

		// gear in front of everything
		if (f.Shades && InRect(u, v, 0.355f, headY - 0.045f, 0.645f, headY + 0.02f) && !InRect(u, v, 0.485f, headY - 0.045f, 0.515f, headY - 0.02f)) return Region.Shades;
		if (f.Mic) {
			if (InEllipse(u, v, 0.30f, 0.60f, 0.052f, 0.052f)) return Region.Gear;
			if (InRect(u, v, 0.285f, 0.64f, 0.315f, 1f)) return Region.Gear;
		}
		if (f.Headphones) {
			if (InEllipse(u, v, 0.318f, headY, 0.034f, 0.075f) || InEllipse(u, v, 0.682f, headY, 0.034f, 0.075f)) return Region.Gear;
			float dx = (u - 0.5f) / (headRx + 0.028f), dy = (v - headY) / (headRy + 0.02f);
			float r = dx * dx + dy * dy;
			if (v < headY - 0.03f && r > 0.9f && r < 1.18f) return Region.Gear;
		}

		// hats
		switch (f.Hat) {
			case PortraitHat.Stetson:
				if (InEllipse(u, v, 0.5f, headY - 0.095f, 0.31f, 0.045f)) return Region.Hat;
				if (InEllipse(u, v, 0.5f, headY - 0.19f, 0.135f, 0.115f) && v < headY - 0.10f) return Region.Hat;
				break;
			case PortraitHat.Fedora:
				if (InEllipse(u, v, 0.5f, headY - 0.10f, 0.245f, 0.038f)) return Region.Hat;
				if (InEllipse(u, v, 0.5f, headY - 0.185f, 0.125f, 0.10f) && v < headY - 0.105f) return Region.Hat;
				break;
			case PortraitHat.Beret:
				if (InEllipse(u, v, 0.46f, headY - 0.145f, 0.205f, 0.075f)) return Region.Hat;
				break;
			case PortraitHat.FlatCap:
				if (InEllipse(u, v, 0.49f, headY - 0.13f, 0.175f, 0.075f) && v < headY - 0.085f) return Region.Hat;
				if (InEllipse(u, v, 0.62f, headY - 0.085f, 0.13f, 0.03f)) return Region.Hat;
				break;
		}

		// face
		bool face = InEllipse(u, v, 0.5f, headY, headRx, headRy);

		// hair (sits on top of the skull and falls past the face for the long styles)
		switch (f.Hair) {
			case PortraitHair.Short:
				if (InEllipse(u, v, 0.5f, headY - 0.05f, headRx + 0.018f, headRy - 0.01f) && v < headY - 0.095f + (Math.Abs(u - 0.5f) > headRx * 0.7f ? 0.11f : 0f)) return Region.Hair;
				break;
			case PortraitHair.Pompadour:
				if (InEllipse(u, v, 0.52f, headY - 0.20f, 0.17f, 0.085f)) return Region.Hair;
				if (InEllipse(u, v, 0.5f, headY - 0.05f, headRx + 0.02f, headRy - 0.01f) && v < headY - 0.095f + (Math.Abs(u - 0.5f) > headRx * 0.7f ? 0.11f : 0f)) return Region.Hair;
				break;
			case PortraitHair.MopTop:
				if (InEllipse(u, v, 0.5f, headY - 0.045f, headRx + 0.045f, headRy + 0.0f) && v < headY - 0.05f + (Math.Abs(u - 0.5f) > headRx * 0.6f ? 0.16f : 0f)) return Region.Hair;
				break;
			case PortraitHair.Beehive:
				if (InEllipse(u, v, 0.5f, headY - 0.19f, 0.165f, 0.165f)) return Region.Hair;
				if (InEllipse(u, v, 0.5f, headY - 0.05f, headRx + 0.025f, headRy - 0.005f) && !InEllipse(u, v, 0.5f, headY + 0.015f, headRx * 0.82f, headRy * 0.86f)) return Region.Hair;
				break;
			case PortraitHair.Bob:
				if (InEllipse(u, v, 0.5f, headY - 0.04f, headRx + 0.04f, headRy + 0.045f) && !InEllipse(u, v, 0.5f, headY + 0.02f, headRx * 0.82f, headRy * 0.88f) && v < headY + 0.12f) return Region.Hair;
				break;
			case PortraitHair.Long:
				if (InEllipse(u, v, 0.5f, headY - 0.04f, headRx + 0.05f, headRy + 0.03f) && !InEllipse(u, v, 0.5f, headY + 0.03f, headRx * 0.8f, headRy * 0.9f)) return Region.Hair;
				if ((InRect(u, v, 0.5f - headRx - 0.05f, headY, 0.5f - headRx + 0.03f, 0.72f) || InRect(u, v, 0.5f + headRx - 0.03f, headY, 0.5f + headRx + 0.05f, 0.72f))) return Region.Hair;
				break;
			case PortraitHair.Bald:
				break;
		}
		if (face) return Region.Face;

		// neck, collar, body
		if (InRect(u, v, 0.445f, headY + headRy - 0.03f, 0.555f, 0.66f)) return Region.Neck;
		bool body = InEllipse(u, v, 0.5f, 1.04f, 0.47f, 0.43f) && v < 1f;
		if (body) {
			switch (f.Wear) {
				case PortraitWear.Suit:
					// shirt front and tie in a narrow V under the chin
					if (v > 0.64f && Math.Abs(u - 0.5f) < 0.075f - (v - 0.64f) * 0.14f && v < 0.9f) return f.Tie && Math.Abs(u - 0.5f) < 0.022f ? Region.Tie : Region.Shirt;
					break;
				case PortraitWear.OpenCollar:
					if (v > 0.63f && Math.Abs(u - 0.5f) < 0.105f - (v - 0.63f) * 0.2f && v < 0.84f) return Region.Shirt;
					break;
				case PortraitWear.Robe:
					if (v > 0.63f && Math.Abs(u - 0.5f) < 0.115f - (v - 0.63f) * 0.30f) return Region.Shirt;
					break;
				case PortraitWear.Apron:
					if (v > 0.74f && Math.Abs(u - 0.5f) < 0.15f) return Region.Shirt;
					break;
			}
			return Region.Body;
		}
		return Region.None;
	}

	/// <summary>Ink density 0..1 for a classified point: the lit side (lamp, upper left) prints lighter.</summary>
	private static float Tone(Region region, float u, float v, float lit) {
		switch (region) {
			case Region.Face: return Mathf.Clamp(0.05f + 0.30f * lit + (v - 0.30f) * 0.22f, 0.03f, 0.55f);
			case Region.Neck: return 0.40f + 0.2f * lit;
			case Region.Hair: return 0.86f;
			case Region.Hat: return 0.92f;
			case Region.Body: return 0.74f + 0.14f * lit;
			case Region.Shirt: return 0.04f + 0.10f * lit;
			case Region.Tie: return 0.95f;
			case Region.Shades: return 1f;
			case Region.Gear: return 0.97f;
			default: return 0f;
		}
	}

	// ---- baking ---------------------------------------------------------------------------------------

	private static ImageTexture Bake(string key, List<PortraitFigure> figures, int width, int height) {
		Color paper = new("f6ecd0"), ink = new("2b2115");
		var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		image.Fill(paper);

		int count = figures.Count;
		// where each figure stands: (centre x, scale, drop). The middle one is drawn last, so it overlaps its neighbours.
		var slots = new List<(float cx, float scale, float drop)>();
		if (count == 1) slots.Add((0.5f, 1.0f, 0.0f));
		else if (count == 2) { slots.Add((0.30f, 0.78f, 0.18f)); slots.Add((0.70f, 0.78f, 0.18f)); }
		else { slots.Add((0.24f, 0.70f, 0.28f)); slots.Add((0.76f, 0.70f, 0.28f)); slots.Add((0.5f, 0.86f, 0.10f)); }
		// Draw order: outer first, centre last.
		var order = new List<int>();
		if (count == 3) { order.Add(0); order.Add(1); order.Add(2); } else for (int i = 0; i < count; i++) order.Add(i);

		uint seed = Hash(key);
		float spacing = width / 30f;                 // ~30 dots across the plate
		float maxRadius = spacing * 0.66f;
		float c = 0.70710678f;
		var a = new Vector2(c, c) * spacing;
		var b = new Vector2(-c, c) * spacing;
		int span = (int)(Math.Max(width, height) / spacing * 1.5f) + 2;
		for (int i = -span; i <= span; i++) {
			for (int j = -span; j <= span; j++) {
				Vector2 p = new Vector2(width * 0.5f, height * 0.5f) + a * i + b * j;
				if (p.X < -1 || p.Y < -1 || p.X > width + 1 || p.Y > height + 1) continue;
				float u = p.X / width, v = p.Y / height;

				float tone = 0.07f + 0.09f * u + 0.17f * (float)Math.Pow(Math.Abs(u - 0.5f) * 2f, 2.0) + 0.10f * v;   // studio backdrop, darker at the corners
				for (int index = 0; index < order.Count; index++) {
					var (cx, scale, drop) = slots[order[index]];
					float lu = (u - cx) / scale + 0.5f, lv = (v - drop) / scale;
					if (lu < 0f || lu > 1f || lv < 0f || lv > 1f) continue;
					PortraitFigure figure = figures[order[index]];
					Region region = Classify(figure, lu, lv);
					if (region == Region.None) continue;
					float lit = Mathf.Clamp(lu * 1.25f - 0.12f, 0f, 1f);
					tone = Tone(region, lu, lv, lit);
				}
				tone = Mathf.Clamp(tone + ((Hash($"{i},{j}") ^ seed) % 100u) / 100f * 0.04f - 0.02f, 0f, 1f);
				float radius = maxRadius * (float)Math.Sqrt(tone);
				if (radius < 0.35f) continue;
				int x0 = Math.Max(0, (int)(p.X - radius - 1)), x1 = Math.Min(width - 1, (int)(p.X + radius + 1));
				int y0 = Math.Max(0, (int)(p.Y - radius - 1)), y1 = Math.Min(height - 1, (int)(p.Y + radius + 1));
				for (int y = y0; y <= y1; y++)
					for (int x = x0; x <= x1; x++) {
						float d = new Vector2(x + 0.5f - p.X, y + 0.5f - p.Y).Length();
						float coverage = Mathf.Clamp(radius - d + 0.5f, 0f, 1f);
						if (coverage <= 0f) continue;
						image.SetPixel(x, y, image.GetPixel(x, y).Lerp(ink, coverage));
					}
			}
		}
		return ImageTexture.CreateFromImage(image);
	}
}

/// <summary>
/// A pasted-on publicity photo: the halftone plate on a cream mat with a hairline border and a hair of tilt, so a row
/// of them on cards does not read as a grid of stickers. Size is the mat; the plate sits inside it.
/// </summary>
public partial class PortraitPhoto : Control {
	private Texture2D plate;
	private Color tint = Colors.White;

	public PortraitPhoto Set(Texture2D texture, Vector2 size, string tiltKey = null) {
		plate = texture;
		CustomMinimumSize = size;
		MouseFilter = MouseFilterEnum.Ignore;
		SizeFlagsVertical = SizeFlags.ShrinkBegin;
		if (!string.IsNullOrEmpty(tiltKey)) {
			PivotOffset = size / 2f;
			RotationDegrees = (Portraits.Hash(tiltKey) % 5u - 2f) * 0.55f;
		}
		QueueRedraw();
		return this;
	}

	public override void _Draw() {
		var mat = new Rect2(Vector2.Zero, Size);
		// soft shadow and the mat
		DrawRect(new Rect2(mat.Position + new Vector2(2, 3), mat.Size), new Color(0, 0, 0, 0.22f));
		DrawRect(mat, new Color("f8f0d9"));
		DrawRect(mat, new Color("8a7048"), false, 1f);
		if (plate == null) return;
		Rect2 inner = mat.Grow(-5f);
		DrawTextureRect(plate, inner, false, tint);
		DrawRect(inner, new Color("2b2115", 0.55f), false, 1f);
	}
}
