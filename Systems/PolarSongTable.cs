using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Godot;

/// <summary>Reviewable shadow tuning. Pure functions receive this immutable-by-convention input.</summary>
public sealed class PolarSongTable {
	public string ContentFingerprint { get; private set; }
	public string Version { get; set; }
	public string Provenance { get; set; }
	public string[] AxisOrder { get; set; }
	public Dictionary<string, float> Numbers { get; set; }
	public float[] IdentityWeights { get; set; }
	public Dictionary<string, PolarStylePrior> Families { get; set; }
	public Dictionary<string, PolarStylePrior> GenreOverrides { get; set; }
	public Dictionary<string, float[]> LyricModifiers { get; set; }
	public Dictionary<string, float[]> VocalModifiers { get; set; }
	public PolarArchetypeRow[] Archetypes { get; set; }
	public Dictionary<string, PolarGenreForms> GenreForms { get; set; }
	private readonly Dictionary<Genre, PolarStylePrior> derivedPriors = new();
	// Only the bounded development probes may select the frozen pre-repair fixture.
	internal static bool AuditLegacyRepair;
	public float N(string key) => Numbers[key];
	public PolarArchetypeRow Row(SongArchetype archetype) => Archetypes.First(r => r.Name == archetype.ToString());
	public PolarStylePrior Prior(Genre genre) => derivedPriors.TryGetValue(genre, out var center) ? center : LegacyPrior(genre);
	private PolarStylePrior LegacyPrior(Genre genre) => GenreOverrides.TryGetValue(genre.ToString(), out var p) ? p :
		Families[GenreCatalog.TryGet(genre, out var g) ? g.Family.ToString() : GenreFamily.Pop.ToString()];
	private static readonly Lazy<PolarSongTable> loaded = new(() => {
		using var file = Godot.FileAccess.Open("res://Data/PolarSongTable.json", Godot.FileAccess.ModeFlags.Read);
		if (file == null) throw new InvalidOperationException("Missing polar song table.");
		return Parse(file.GetAsText());
	});
	private static readonly Lazy<PolarSongTable> legacy = new(() => {
		using var file = Godot.FileAccess.Open("res://SimTools/PolarSongTable.LegacyAttribution.json", Godot.FileAccess.ModeFlags.Read);
		return Parse(file.GetAsText(), true);
	});
	public static PolarSongTable Current => AuditLegacyRepair ? legacy.Value : loaded.Value;
	internal static PolarSongTable RepairTable => loaded.Value;
	public static PolarSongTable Parse(string json) => Parse(json, false);
	private static PolarSongTable Parse(string json, bool legacyFixture) {
		var table = JsonSerializer.Deserialize<PolarSongTable>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		if (table == null || string.IsNullOrWhiteSpace(table.Version)) throw new InvalidOperationException("Unversioned polar table.");
		var required = Enum.GetValues<SongArchetype>().Where(a => a != SongArchetype.Unknown).Select(a => a.ToString()).OrderBy(s => s);
		if (!required.SequenceEqual(table.Archetypes.Select(r => r.Name).OrderBy(s => s))) throw new InvalidOperationException("Incomplete or duplicate archetype table.");
		if (!Enum.GetNames<SongAxis>().SequenceEqual(table.AxisOrder) || table.IdentityWeights.Length != SongProfile.IdentityCount ||
			table.IdentityWeights.Any(w => !float.IsFinite(w) || w <= 0)) throw new InvalidOperationException("Invalid polar axes/weights.");
		foreach (var r in table.Archetypes) {
			if (r.Axes.Length != SongProfile.AxisCount || r.Axes.Any(v => !float.IsFinite(v) || v < 0 || v > 1) ||
				!float.IsFinite(r.Plasticity) || r.Plasticity < 0 || r.Plasticity > 1 || string.IsNullOrWhiteSpace(r.Provenance))
				throw new InvalidOperationException($"Invalid archetype {r.Name}.");
		}
		foreach (var p in table.Families.Values.Concat(table.GenreOverrides.Values)) {
			if (legacyFixture && (p.Identity.Length != SongProfile.IdentityCount || p.Identity.Any(v => !float.IsFinite(v) || v < 0 || v > 1))) throw new InvalidOperationException("Invalid style prior.");
			table.Row(Enum.Parse<SongArchetype>(p.Fallback));
		}
		foreach (var delta in table.LyricModifiers.Values.Concat(table.VocalModifiers.Values))
			if (delta.Length != SongProfile.AxisCount || delta.Any(v => !float.IsFinite(v))) throw new InvalidOperationException("Invalid modifier.");
		if (table.Numbers.Values.Any(v => !float.IsFinite(v))) throw new InvalidOperationException("Non-finite tuning.");
		var weights = new[] { table.N("selectionCapability"), table.N("selectionIdentity"), table.N("selectionMoment") };
		if (weights.Any(w => w < 0 || w > 1) || Math.Abs(weights.Sum() - 1) > .00001f)
			throw new InvalidOperationException("Polar selection weights must be normalized.");
		foreach (string key in new[] { "executionPenalty", "criticIdentityPenalty", "momentConversionStrength", "rockStandardFactor", "rockTraditionalFactor",
			"recordingHookBlend", "hookCeilingStrength", "recordingQualityHookWeight", "recordingQualityProductionWeight", "promotionRealizationWeight" })
			if (table.N(key) < 0 || table.N(key) > 1) throw new InvalidOperationException("Invalid polar consumer strength: " + key);
		if (table.N("executionFullCapability") <= 0 || table.N("executionFullCapability") > 1)
			throw new InvalidOperationException("Invalid full-execution Capability threshold.");
		table.ContentFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
		if (!legacyFixture) {
			if (table.GenreForms == null || table.GenreForms.Count != Enum.GetValues<Genre>().Length || table.N("tastePseudoCount") <= 0)
				throw new InvalidOperationException("Incomplete authored genre forms or invalid taste pseudo-count.");
			foreach (var genre in Enum.GetValues<Genre>()) table.derivedPriors[genre] = new PolarStylePrior {
				Identity = table.DeriveCenter(genre), Fallback = table.LegacyPrior(genre).Fallback
			};
			table.ValidatePriorCenters();
		}
		return table;
	}
	public float[] DeriveCenter(Genre genre) {
		var spec = GenreForms[genre.ToString()];
		if ((spec.Songbook != null) == (spec.Forms != null)) throw new InvalidOperationException("Exactly one authored form source required: " + genre);
		var rows = spec.Songbook != null ? PolarRepertoireTable.Current.Assignments[spec.Songbook].Select(r => (r.Archetype, r.Weight)).ToArray() :
			spec.Forms.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => (Archetype: kv.Key, Weight: kv.Value)).ToArray();
		if (rows.Length == 0 || rows.Any(r => !float.IsFinite(r.Weight) || r.Weight <= 0)) throw new InvalidOperationException("Invalid genre shares: " + genre);
		return Enumerable.Range(0, SongProfile.IdentityCount).Select(i => rows.Sum(r => r.Weight * Row(Enum.Parse<SongArchetype>(r.Archetype)).Axes[SongProfile.DemandCount + i]) / rows.Sum(r => r.Weight)).ToArray();
	}
	public void ValidatePriorCenters() {
		foreach (var genre in Enum.GetValues<Genre>()) if (!Prior(genre).Identity.SequenceEqual(DeriveCenter(genre)))
			throw new InvalidOperationException("Stored prior diverges from authored center: " + genre);
	}
}
public sealed class PolarGenreForms { public string Songbook { get; set; } public Dictionary<string, float> Forms { get; set; } }
public sealed class PolarStylePrior { public float[] Identity { get; set; } public string Fallback { get; set; } }
public sealed class PolarArchetypeRow {
	public string Name { get; set; }
	public float[] Axes { get; set; }
	public float Plasticity { get; set; }
	public int FromYear { get; set; }
	public string[] Families { get; set; }
	public string Pace { get; set; }
	public string Mood { get; set; }
	public string Provenance { get; set; }
}
