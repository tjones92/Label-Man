using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;

/// <summary>Presentation lines selected from the evidence already observed at a set.</summary>
internal static class GigLines {
	private sealed class Pack {
		[JsonPropertyName("lines")] public List<Line> Lines { get; set; }
	}

	private sealed class Line {
		[JsonPropertyName("id")] public string Id { get; set; }
		[JsonPropertyName("slot")] public string Slot { get; set; }
		[JsonPropertyName("conditions")] public Dictionary<string, JsonElement> Conditions { get; set; }
		[JsonPropertyName("chance")] public double? Chance { get; set; }
		[JsonPropertyName("text")] public string Text { get; set; }
	}

	private static readonly Lazy<Line[]> Lines = new(Load);
	private static readonly Regex Token = new(@"{([a-z_]+)}", RegexOptions.CultureInvariant);

	private static Line[] Load() {
		const string path = "res://Data/gig_lines.json";
		try {
			string json = FileAccess.GetFileAsString(path);
			return JsonSerializer.Deserialize<Pack>(json)?.Lines?
				.Where(line => line?.Id != null && line.Slot != null && !string.IsNullOrWhiteSpace(line.Text))
				.OrderBy(line => line.Id, StringComparer.Ordinal).ToArray() ?? Array.Empty<Line>();
		} catch (JsonException ex) {
			GD.PushWarning($"Could not read the set narration in {path}. {ex.Message}");
			return Array.Empty<Line>();
		}
	}

	internal static string Pick(string key, string slot, IReadOnlyDictionary<string, string> facts, IReadOnlyDictionary<string, string> names) {
		var candidates = Lines.Value.Where(line => line.Slot == slot &&
			(line.Conditions == null || line.Conditions.All(condition =>
				facts.TryGetValue(condition.Key, out var actual) &&
				string.Equals(actual, condition.Value.ToString(), StringComparison.OrdinalIgnoreCase))) &&
			(line.Chance is not double chance || Phrase.Unit(key + "|" + slot + "|" + line.Id) < chance) &&
			Token.Matches(line.Text).All(match => names.TryGetValue(match.Groups[1].Value, out var value) && !string.IsNullOrWhiteSpace(value)))
			.ToArray();
		if (candidates.Length == 0) return null;
		int specificity = candidates.Max(line => line.Conditions?.Count ?? 0);
		string template = Phrase.Pick(key + "|" + slot, candidates.Where(line => (line.Conditions?.Count ?? 0) == specificity)
			.Select(line => line.Text).ToArray());
		return Token.Replace(template, match => names[match.Groups[1].Value]);
	}
}
