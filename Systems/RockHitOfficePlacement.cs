using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/ReleaseJudgementDirective.md, 2026-10-27 office screen), off unless named; needs
/// --rock-hit-writers. --rock-hit-office-placement=W makes the rock &amp; roll office place its songs the way the Brill /
/// Aldon / Hill &amp; Range offices did.
///
/// Without it the office is a fixed 90-song catalogue every label samples forever: one seed cut 82 songs into 610
/// singles in a year (one song 33 times), 116 of them on Small labels and only 63 on Majors. Under the switch:
/// <list type="bullet">
/// <item>Pitch list: the office pitches only to Majors and to labels with a Top 40 record in the last 52 weeks.</item>
/// <item>One placement: an office song is gone once it is on an act's shortlist or has a committed master. A song
/// that charts can still come back as a hit cover through the ordinary recent-hit route.</item>
/// <item>Supply: the office writes W new songs a week (live weeks only), on a private stream keyed by serial, so the
/// rest of the catalogue and the global stream are untouched.</item>
/// </list>
/// Other publishers, the player's label and the prewarm are unchanged.
/// </summary>
public static class RockHitOfficePlacement {
	public const string Prefix = "--rock-hit-office-placement=";
	public const string OfficeId = "pub_rnr_hits";
	public static readonly float SongsPerWeek = Read();
	public static bool Enabled => SongsPerWeek >= 0f;

	private static float Read() {
		var values = SimulationArguments.Get().Where(a => a.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return -1f;
		if (values.Length > 1) throw new ArgumentException("Name " + Prefix + " once.");
		if (!RockHitWriters.Enabled) throw new ArgumentException(Prefix + " needs " + RockHitWriters.Switch + ".");
		if (!float.TryParse(values[0].Substring(Prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float w) || !(w >= 0f && w <= 20f))
			throw new ArgumentException(Prefix + " needs songs per week in [0, 20].");
		return w;
	}

	// Office songs on an act's active shortlist. Derived from the saved shortlists, so it is rebuilt, never saved.
	private static readonly HashSet<string> reserved = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, bool> pitchList = new(StringComparer.Ordinal);
	private static int pitchListWeek = int.MinValue;

	public static bool IsOffice(SongComposition song) => song?.rights?.publisherId == OfficeId;

	private static bool reservationsBuilt;

	private static bool Placed(SongComposition song) {
		if (!reservationsBuilt) RebuildReservations(AiRepertoireService.ActiveCatalogueSongIds());
		return !string.IsNullOrEmpty(song.firstCommittedMasterId) || reserved.Contains(song.songId);
	}

	/// <summary>Whether an office song on a shortlist has since been cut by another act (live weeks only).</summary>
	internal static bool PlacedElsewhere(SongComposition song) =>
		Enabled && IsOffice(song) && (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) > 0 && !string.IsNullOrEmpty(song.firstCommittedMasterId);

	/// <summary>Majors, and any label with a Top 40 record in the last 52 weeks. Read once per label per week.</summary>
	public static bool OnPitchList(AILabel label) {
		if (label == null) return false;
		if (label.tier == LabelTier.Major) return true;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week != pitchListWeek) { pitchList.Clear(); pitchListWeek = week; }
		if (!pitchList.TryGetValue(label.labelId, out bool listed))
			pitchList[label.labelId] = listed = CompetitorManager.Instance?.LabelHasRecentTop40(label) == true;
		return listed;
	}

	private static bool Pitchable(SongComposition song, bool listed) => !IsOffice(song) || listed && !Placed(song);

	private static bool Applies(AILabel label) => Enabled && label != null && !label.isPlayerOwned && (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) > 0;

	/// <summary>The professional pool this label is pitched: the pool itself when off.</summary>
	public static IReadOnlyList<SongComposition> Visible(IReadOnlyList<SongComposition> pool, AILabel label) {
		if (!Applies(label) || pool == null || pool.Count == 0) return pool;
		bool listed = OnPitchList(label);
		return pool.Where(s => Pitchable(s, listed)).ToList();
	}

	/// <summary>Whether the office would place this one song with this label (true for any other song, or when off).</summary>
	public static bool CanPlace(SongComposition song, AILabel label) => !Applies(label) || Pitchable(song, OnPitchList(label));

	/// <summary>Whether this label is pitched anything from the pool (no allocation).</summary>
	public static bool AnyVisible(IReadOnlyList<SongComposition> pool, AILabel label) {
		if (!Applies(label)) return pool != null && pool.Count > 0;
		if (pool == null) return false;
		bool listed = OnPitchList(label);
		for (int i = 0; i < pool.Count; i++) if (Pitchable(pool[i], listed)) return true;
		return false;
	}

	internal static void Reserve(SongComposition song) { if (Enabled && reservationsBuilt && IsOffice(song)) reserved.Add(song.songId); }
	internal static void Release(string songId) { if (Enabled && reservationsBuilt && songId != null) reserved.Remove(songId); }

	/// <summary>Forget the derived state (load, new world); the next read rebuilds it from the shortlists.</summary>
	internal static void Invalidate() { reserved.Clear(); reservationsBuilt = false; pitchList.Clear(); pitchListWeek = int.MinValue; }

	private static void RebuildReservations(IEnumerable<string> activeSongIds) {
		reserved.Clear();
		reservationsBuilt = true;
		if (activeSongIds == null) return;
		foreach (string id in activeSongIds) if (IsOffice(CompositionCatalogService.GetSong(id))) reserved.Add(id);
	}

	/// <summary>The office's writing for one live week: floor(W × week) − floor(W × (week − 1)) new songs.</summary>
	public static void WriteWeek(int week, int year) {
		if (!Enabled || week <= 0) return;
		int due = Mathf.FloorToInt(SongsPerWeek * week) - Mathf.FloorToInt(SongsPerWeek * (week - 1));
		for (int i = 0; i < due; i++) CompositionCatalogService.WriteOfficeSong(OfficeId, RockHitWriters.OfficeFocus, year);
	}
}
