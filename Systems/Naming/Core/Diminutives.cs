// Systems/Naming/Core/Diminutives.cs
// First-name grooming table (solo-act layer, doc B §4). Teen idols were manufactured with soft
// diminutive first names (Robert Velline -> Bobby Vee); trad-pop/jazz kept formal ones (Frank
// Sinatra, John Coltrane). Grooming captures the difference from ONE shared first-name pool rather
// than maintaining separate "teen names" / "jazz names" lists. Bidirectional: casual<->formal.
// Godot-free.

using System;
using System.Collections.Generic;

namespace LabelMan.Naming {

	public sealed class Diminutives {
		private readonly Dictionary<string, List<string>> _casual = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _country = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _toFull = new(StringComparer.OrdinalIgnoreCase); // casual -> formal

		public int Count => _casual.Count;

		/// <summary>Load the top-level "diminutives" object: { "Robert": { "casual":[...], "country":"Bob" }, ... }.</summary>
		public void LoadJson(string json) {
			if (string.IsNullOrWhiteSpace(json)) return;
			using var doc = System.Text.Json.JsonDocument.Parse(json, new System.Text.Json.JsonDocumentOptions {
				CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
			if (!doc.RootElement.TryGetProperty("diminutives", out var map)) return;
			foreach (var kv in map.EnumerateObject()) {
				if (kv.Value.ValueKind != System.Text.Json.JsonValueKind.Object) continue;   // skip the "//" note
				string full = kv.Name;
				if (kv.Value.TryGetProperty("casual", out var ca) && ca.ValueKind == System.Text.Json.JsonValueKind.Array) {
					var list = new List<string>();
					foreach (var c in ca.EnumerateArray()) { var s = c.GetString(); if (!string.IsNullOrEmpty(s)) { list.Add(s); if (!_toFull.ContainsKey(s)) _toFull[s] = full; } }
					if (list.Count > 0) _casual[full] = list;
				}
				if (kv.Value.TryGetProperty("country", out var co) && co.ValueKind == System.Text.Json.JsonValueKind.String)
					_country[full] = co.GetString();
			}
		}

		/// <summary>Apply a grooming mode to a first name. Unknown names / modes pass through unchanged,
		/// so grooming is always safe to request.</summary>
		public string Groom(string name, string mode, IRandom rng) {
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(mode)) return name;
			switch (mode.ToLowerInvariant()) {
				case "diminutive":                                  // ~50% chance -> a casual form
					return _casual.TryGetValue(name, out var d) && rng != null && rng.Chance(0.5) ? Pick(d, rng) : name;
				case "forcediminutive":                             // always a casual form when one exists
					return _casual.TryGetValue(name, out var f) ? Pick(f, rng) : name;
				case "formal":                                      // expand a casual form back to the full name
					return _toFull.TryGetValue(name, out var full) ? full : name;
				case "country":                                     // prefer the country-coded variant
					if (_country.TryGetValue(name, out var cy)) return cy;
					return _casual.TryGetValue(name, out var c) ? c[0] : name;
				default:
					return name;
			}
		}

		private static string Pick(List<string> list, IRandom rng) =>
			list.Count == 0 ? null : list[rng == null ? 0 : rng.Next(list.Count)];
	}
}
