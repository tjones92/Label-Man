using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --opening-programme-clocks (LpGenreAllocationFollowup.md §85-86). The release programme keeps, per act, the week of its
/// last single and last LP, and spaces new work from them (the single floor, <see cref="LabelReleaseProgramme.AlbumFloor"/>
/// for LPs). An act with no surviving catalogue row of a format is dated from its contract start. The opening roster is
/// signed through the ordinary signing seam at chart week 0, so every opening act reads as signed this week: no opening
/// act may issue an LP before week 18, and only acts with a prewarm single row may issue a single before week 6. Majors
/// then issue ~30 LPs in the first quarter against ~140 in the next, and every tier's LP launches are near zero.
///
/// With the switch on, an act of the opening roster (snapshotted before the first live week) with no row of the format
/// is dated from its pre-1960 history instead:
/// - single: its elapsed interval since its last release (<c>weeksSinceLastRelease</c>, set at roster seeding);
/// - LP: the year of its last pre-1960 LP in its public history (opening careers), at a keyed week of that year; with no
///   LP in its history, a keyed uniform age in [0, 2 × AlbumFloor). In the control's steady state (acis260, week 130)
///   the acts about to issue an LP had last issued one 8 / 16 / 29 weeks before (quartiles), and half had never issued
///   one; the uniform's quartiles are 9 / 18 / 27.
/// Acts signed later, and old saves, keep the contract rule. No global draws. Off: nothing is read or written.
/// </summary>
public static class OpeningProgrammeClocks {
	public const string Switch = "--opening-programme-clocks";
	public const uint RollDomain = 44;
	private static bool read, enabled;
	public static bool Enabled { get { if (!read) { enabled = GameyAuditArguments.Has(Switch); read = true; } return enabled; } }
	internal static void SetForChecks(bool value) { read = true; enabled = value; }

	private static HashSet<string> openingActs;
	private static int openingYear;
	/// <summary>Snapshots the opening roster before the first live week. Not saved: clocks are made in the first week and
	/// saved with the programme, so a world loaded later never needs it.</summary>
	public static void Capture(IEnumerable<AILabel> labels, int year) {
		if (!Enabled) return;
		openingYear = year;
		openingActs = new HashSet<string>(StringComparer.Ordinal);
		foreach (AILabel label in labels ?? Enumerable.Empty<AILabel>())
			if (label != null && !label.isPlayerOwned)
				foreach (SimulatedArtist artist in label.roster ?? new List<SimulatedArtist>())
					if (artist != null) openingActs.Add(artist.artistId);
	}
	internal static void SetOpeningActsForChecks(IEnumerable<string> ids, int year) {
		openingActs = new HashSet<string>(ids ?? Enumerable.Empty<string>(), StringComparer.Ordinal); openingYear = year;
	}

	public static bool Applies(SimulatedArtist artist) =>
		Enabled && artist != null && openingActs != null && openingActs.Contains(artist.artistId);

	/// <summary>Weeks since the act's last release of the format, before the first live week; see the class summary.</summary>
	public static int Age(SimulatedArtist artist, ReleaseFormat format) {
		if (format == ReleaseFormat.Single) return Math.Clamp(artist.weeksSinceLastRelease, 0, 52 * 5);
		var lastLp = (artist.publicHistory ?? new List<PublicReleaseEntry>())
			.Where(e => e != null && e.format == ReleaseFormat.Album && e.year < openingYear).Select(e => e.year).DefaultIfEmpty(0).Max();
		if (lastLp > 0) {
			int weekOfYear = (int)(LabelReleaseProgramme.Roll("opening-programme-clock", artist.artistId, "last-lp-week", lastLp, RollDomain) * 52);
			return (openingYear - lastLp - 1) * 52 + (52 - Math.Clamp(weekOfYear, 0, 51));
		}
		return (int)(LabelReleaseProgramme.Roll("opening-programme-clock", artist.artistId, "lp-age", 0, RollDomain) * 2 * LabelReleaseProgramme.AlbumFloor);
	}
}
