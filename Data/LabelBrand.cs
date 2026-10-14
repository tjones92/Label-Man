using System;
using System.Text;
using Godot;

public enum CrestShape { Roundel, Shield, Banner, Burst, Star, Bar }

/// <summary>The five lettering faces a label can print its name in (see <c>PaperTheme.Lettering</c>).</summary>
public enum LetteringStyle { Slab, Didone, HeavySlab, Script, Deco }

/// <summary>
/// A label's visual identity: a crest shape, a period colour pair, a lettering face and a monogram. The one
/// saturated colour on any page belongs to a label, and this is where it comes from.
///
/// Every label has one. The player picks theirs at founding and it is stored on <see cref="AILabel.brand"/>;
/// every other label (and any player save from before the brand kit) derives its brand from a stable hash of
/// its id, so the chart shows a crest for all of them and nothing needs a migration. The derivation is plain
/// FNV-1a, never <c>string.GetHashCode</c> (which differs per process) and never the sim's RNG, so asking for a
/// brand cannot move a seeded run.
/// </summary>
public sealed class LabelBrand {
	/// <summary>One period colour pair. <see cref="Ground"/> fills the crest, <see cref="Mark"/> is lettered on it.
	/// <see cref="Ink"/> is the dark side that may carry text on cream paper (the bright pairs only pass as fills),
	/// and <see cref="Accent"/> is the colour for a rule or stripe on cream.</summary>
	public readonly record struct Palette(string Name, Color Ground, Color Mark, Color Ink, Color Accent);

	public static readonly Palette[] Palettes = {
		new("Sun yellow",   new Color("1d1915"), new Color("f2c12e"), new Color("1d1915"), new Color("e0a920")),
		new("Chess blue",   new Color("1f4e9c"), new Color("f4ead0"), new Color("1f4e9c"), new Color("1f4e9c")),
		new("Atlantic red", new Color("c23a2b"), new Color("f4ead0"), new Color("a63024"), new Color("c23a2b")),
		new("Olive",        new Color("5f6b2d"), new Color("f4ead0"), new Color("4b5524"), new Color("5f6b2d")),
		new("King orange",  new Color("231a14"), new Color("e8761e"), new Color("231a14"), new Color("d9651a")),
	};

	public const int MaxMonogramLength = 3;

	public CrestShape Crest;
	public int PaletteIndex;
	public LetteringStyle Lettering;
	/// <summary>Up to three capital letters. Empty means "use the initials of the name".</summary>
	public string Monogram = "";

	[System.Text.Json.Serialization.JsonIgnore]
	public Palette Pair => Palettes[Math.Clamp(PaletteIndex, 0, Palettes.Length - 1)];

	public LabelBrand Clone() => new() { Crest = Crest, PaletteIndex = PaletteIndex, Lettering = Lettering, Monogram = Monogram };

	/// <summary>The brand a label wears: its own if it chose one, else the one its id hashes to.</summary>
	public static LabelBrand For(AILabel label) {
		if (label == null) return Derive("");
		if (label.brand != null) return label.brand;
		// A player label with no stored brand (an old save) hashes its name, not the shared "player_label" id,
		// so two players' labels do not wear the same crest.
		return Derive(label.isPlayerOwned ? label.labelName : label.labelId);
	}

	/// <summary>A stable, unique-feeling brand for a seed. The monogram is left empty: it follows the name.</summary>
	public static LabelBrand Derive(string seed) {
		ulong hash = Fnv64(seed ?? "");
		return new LabelBrand {
			Crest = (CrestShape)(int)(((hash >> 0) & 0xFFFF) % 6),
			PaletteIndex = (int)(((hash >> 16) & 0xFFFF) % (ulong)Palettes.Length),
			Lettering = (LetteringStyle)(int)(((hash >> 32) & 0xFFFF) % 5),
			Monogram = ""
		};
	}

	/// <summary>The letters on the crest: the chosen monogram, or the initials of the name.</summary>
	public string MonogramFor(string labelName) =>
		string.IsNullOrWhiteSpace(Monogram) ? InitialsOf(labelName) : Monogram;

	/// <summary>A name in the form its lettering prints it: capitals, except the script, which reads as handwriting.</summary>
	public string DisplayName(string labelName) {
		string name = labelName ?? "";
		return Lettering == LetteringStyle.Script ? name : name.ToUpperInvariant();
	}

	// Words a label tacks on after its real name; "Sun Records" is an S, not an SR.
	private static readonly string[] TrailingWords = {
		"records", "record", "recordings", "recording", "recording co.", "music", "sound", "sounds", "productions",
		"co.", "co", "company", "inc.", "inc", "corp.", "corp", "corporation", "label", "enterprises", "industries", "ltd.", "ltd"
	};
	private static readonly string[] SilentWords = { "the", "and", "of", "&" };

	public static string InitialsOf(string labelName) {
		if (string.IsNullOrWhiteSpace(labelName)) return "";
		var words = new System.Collections.Generic.List<string>(labelName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
		while (words.Count > 1 && Array.IndexOf(TrailingWords, words[^1].ToLowerInvariant()) >= 0) words.RemoveAt(words.Count - 1);
		var letters = new StringBuilder();
		foreach (string word in words) {
			if (letters.Length >= MaxMonogramLength) break;
			if (words.Count > 1 && Array.IndexOf(SilentWords, word.ToLowerInvariant()) >= 0) continue;
			foreach (char c in word) {
				if (!char.IsLetterOrDigit(c)) continue;
				letters.Append(char.ToUpperInvariant(c));
				break;
			}
		}
		return letters.ToString();
	}

	/// <summary>Cleans a typed monogram: letters and digits only, capitals, at most three.</summary>
	public static string CleanMonogram(string text) {
		var cleaned = new StringBuilder();
		foreach (char c in text ?? "") {
			if (!char.IsLetterOrDigit(c)) continue;
			cleaned.Append(char.ToUpperInvariant(c));
			if (cleaned.Length >= MaxMonogramLength) break;
		}
		return cleaned.ToString();
	}

	private static ulong Fnv64(string text) {
		const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
		ulong hash = offset;
		foreach (byte b in Encoding.UTF8.GetBytes(text)) { hash ^= b; hash *= prime; }
		// A last avalanche so the low and high bit-slices used above are not correlated for short ids.
		hash ^= hash >> 33; hash *= 0xff51afd7ed558ccdUL; hash ^= hash >> 33; hash *= 0xc4ceb9fe1a85ec53UL; hash ^= hash >> 33;
		return hash;
	}
}
