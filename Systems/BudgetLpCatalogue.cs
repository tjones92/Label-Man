using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// V2 supply screen for a budget reissue line. These are catalogue aggregates, not issued
/// products, licensed rights, demand or sales. Reads the club ledger without advancing it.
/// Retired entries have no surviving title ages; opening seeds have no title count or acts.
/// </summary>
public static class BudgetLpCatalogue {
	public const string AuditSwitch = "--budget-lp-audit";
	// Sensitivity screen, not an assertion about historical reissue eligibility.
	public const int LiveAgeWeeks = 52;
	public sealed class Source {
		public string LabelId, ArtistId, Kind;
		public int? Titles;
		public double Appeal, AdultAppeal;
	}

	public static IReadOnlyList<Source> Snapshot(RecordClubState state, IEnumerable<AILabel> labels,
		IEnumerable<RecordRuntimeData> records, int year) {
		var active = labels.Where(l => l != null && l.IsActive && !l.isPlayerOwned && !string.IsNullOrEmpty(l.labelId))
			.Select(l => l.labelId).ToHashSet(StringComparer.Ordinal);
		var sources = new Dictionary<(string label, string artist, string kind), Source>();
		void Add(string label, string artist, string kind, int? titles, double appeal, double adult) {
			if (label == null || !active.Contains(label) || !double.IsFinite(appeal) || appeal <= 0 ||
				!double.IsFinite(adult) || adult < 0) return;
			artist ??= "";
			var key = (label, artist, kind);
			if (!sources.TryGetValue(key, out var source)) sources[key] = source = new Source {
				LabelId = label, ArtistId = artist, Kind = kind, Titles = titles.HasValue ? 0 : null
			};
			if (source.Titles.HasValue && titles.HasValue) source.Titles += titles.Value;
			source.Appeal += appeal;
			source.AdultAppeal += Math.Min(appeal, adult);
		}
		if (state != null) {
			foreach (var entry in state.Entries.Values)
				Add(entry.LabelId, entry.ArtistId, "Retired", entry.Titles, entry.Appeal, entry.AdultAppeal);
			foreach (var (label, seed) in state.OpeningSeed)
				Add(label, "", "OpeningSynthetic", null, seed.Appeal, seed.AdultAppeal);
		}
		foreach (var runtime in records) {
			var record = runtime?.baseRecord;
			if (record == null || record.isPlayerOwned || record.format != ReleaseFormat.Album ||
				runtime.weeksSinceRelease < LiveAgeWeeks || runtime.totalUnitsSold <= 0) continue;
			Add(record.labelId, record.artistId, "LiveAged52", 1, runtime.totalUnitsSold,
				runtime.totalUnitsSold * (double)RecordClubChannel.AdultShare(record.primaryGenre, year));
		}
		return sources.Values.OrderBy(s => s.LabelId, StringComparer.Ordinal).ThenBy(s => s.ArtistId, StringComparer.Ordinal)
			.ThenBy(s => s.Kind, StringComparer.Ordinal).ToList();
	}
}
