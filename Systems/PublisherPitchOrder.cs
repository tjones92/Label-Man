using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/ReleaseJudgementDirective.md, 2026-10-19 rock hit-writer screen), off unless named.
/// --publisher-pitch-order: publishers pitch their strongest songs to the biggest labels' acts first.
///
/// Without it every label samples the same professional catalogue, so a Major's professional R&amp;R single carried a
/// hook only ~0.02 above an Independent's (label access reaches the hook only through polish x 0.10). Under the switch
/// an AI label sees a hook-ranked slice of the genre's office catalogue: a Major the top 40%, a MidTier the top 70%,
/// and everyone else the bottom 80%, since the top fifth has already been placed with the majors. Selection inside the
/// slice is unchanged (polar fit). Pure ordering, no draws; player labels keep the whole catalogue.
/// </summary>
public static class PublisherPitchOrder {
	public const string Switch = "--publisher-pitch-order";
	public static readonly bool Enabled = Array.IndexOf(OS.GetCmdlineUserArgs(), Switch) >= 0;

	private static readonly Dictionary<(IReadOnlyList<SongComposition>, int, int), IReadOnlyList<SongComposition>> slices = new();

	/// <summary>The slice of a professional pool this label is pitched; the pool itself when off.</summary>
	public static IReadOnlyList<SongComposition> Visible(IReadOnlyList<SongComposition> pool, AILabel label) {
		if (!Enabled || pool == null || pool.Count < 5 || label == null || label.isPlayerOwned) return pool;
		int band = label.tier switch { LabelTier.Major => 0, LabelTier.MidTier => 1, _ => 2 };
		var key = (pool, pool.Count, band);
		if (slices.TryGetValue(key, out var slice)) return slice;
		var ranked = pool.OrderByDescending(s => s.commercialHook).ThenBy(s => s.songId, StringComparer.Ordinal).ToList();
		int n = ranked.Count;
		slice = band switch {
			0 => ranked.Take(Mathf.Max(1, Mathf.CeilToInt(n * 0.40f))).ToList(),
			1 => ranked.Take(Mathf.Max(1, Mathf.CeilToInt(n * 0.70f))).ToList(),
			_ => ranked.Skip(n / 5).ToList()
		};
		slices[key] = slice;
		return slice;
	}
}
