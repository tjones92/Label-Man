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
///
/// --rock-hit-office-opening (needs the placement switch) applies the same rules to the opening catalogue, which
/// otherwise cut ~33 office songs into ~59 records, mostly at Independents, and put ~17 office records in the
/// first-quarter Top 40 against history's ~6-7. With no chart yet, the opening pitch list is the Majors plus
/// labels with an act on a hit rung (Rising, Star, Superstar). Each office song is placed once. The gate then holds
/// without a break into live play, including the first live week, which the plain switch leaves open (chart week 0).
/// </summary>
public static class RockHitOfficePlacement {
	public const string Prefix = "--rock-hit-office-placement=";
	public const string OpeningSwitch = "--rock-hit-office-opening";
	public const string OfficeId = "pub_rnr_hits";
	public static readonly float SongsPerWeek = Read();
	public static bool Enabled => SongsPerWeek >= 0f;
	public static readonly bool OpeningGate = ReadOpening();

	private static bool ReadOpening() {
		if (Array.IndexOf(SimulationArguments.Get(), OpeningSwitch) < 0) return false;
		if (!Enabled) throw new ArgumentException(OpeningSwitch + " needs " + Prefix + "W.");
		return true;
	}

	// The opening catalogue's office placements and pitch list, live only while it is being seeded.
	private static bool opening, openingSeeded;
	private static readonly HashSet<string> openingPlaced = new(StringComparer.Ordinal);
	private static readonly HashSet<string> openingPitched = new(StringComparer.Ordinal);

	internal static void BeginOpening(IEnumerable<AILabel> labels) {
		if (!OpeningGate) return;
		opening = openingSeeded = true;
		openingPlaced.Clear(); openingPitched.Clear();
		foreach (var label in labels)
			if (label.tier == LabelTier.Major || label.roster.Any(a => a.careerState is CareerState.Rising or CareerState.Star or CareerState.Superstar))
				openingPitched.Add(label.labelId);
	}

	internal static void PlacedAtOpening(Record record) {
		if (opening && IsOffice(CompositionCatalogService.GetSong(record?.songId))) openingPlaced.Add(record.songId);
	}

	internal static void EndOpening() {
		if (!opening) return;
		opening = false;
		GD.Print($"ROCK_HIT_OFFICE_OPENING pitched_labels={openingPitched.Count} placed_songs={openingPlaced.Count}");
	}

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
		if (opening) return !string.IsNullOrEmpty(song.firstCommittedMasterId) || openingPlaced.Contains(song.songId);
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
		if (opening) return openingPitched.Contains(label.labelId);
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week != pitchListWeek) { pitchList.Clear(); pitchListWeek = week; }
		if (!pitchList.TryGetValue(label.labelId, out bool listed))
			pitchList[label.labelId] = listed = CompetitorManager.Instance?.LabelHasRecentTop40(label) == true;
		return listed;
	}

	private static bool Pitchable(SongComposition song, bool listed) => !IsOffice(song) || listed && !Placed(song);

	// The first live week's releases run at chart week 0 (the chart week advances after them), so the opening gate holds
	// from seeding on rather than reading the week.
	private static bool Applies(AILabel label) => Enabled && label != null && !label.isPlayerOwned &&
		(opening || openingSeeded || (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) > 0);

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
		// Opening records may carry legacy masters, which leave firstCommittedMasterId empty: read them off the records.
		if (OpeningGate && ChartManager.Instance != null)
			foreach (var data in ChartManager.Instance.GetAllRecords())
				if (IsOffice(CompositionCatalogService.GetSong(data.baseRecord?.songId))) reserved.Add(data.baseRecord.songId);
	}

	/// <summary>The office's writing for one live week: floor(W × week) − floor(W × (week − 1)) new songs.</summary>
	public static void WriteWeek(int week, int year) {
		if (!Enabled || week <= 0) return;
		int due = Mathf.FloorToInt(SongsPerWeek * week) - Mathf.FloorToInt(SongsPerWeek * (week - 1));
		for (int i = 0; i < due; i++) CompositionCatalogService.WriteOfficeSong(OfficeId, RockHitWriters.OfficeFocus, year);
	}
}
