using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Record clubs and mail-order packages (SimTools/RecordClubChannelDesign.md). History sold about 45M LPs a year
/// by mail, a fifth to a quarter of LP units, outside the store and rack market the model clears. The FTC's
/// Columbia Record Club decision (72 F.T.C. 320-411) supplies the shape: three integrated manufacturers ran clubs,
/// licensed smaller labels' masters at about 17.8c a record, sold name artists' hits at $2.37 against a $3.98 list,
/// and offered 200-400 records a month.
///
/// When on, the national volume is the Billboard club-and-mail-order series (authored, not earned). It is split
/// by catalogue state: an aggregate (label, artist) ledger that retired LPs fold into, plus live LPs past the club
/// holdback, plus a decaying synthetic opening catalogue for the pre-1960 backlist. No record is kept alive, no
/// random draws are made, and the units belong to no record: they never enter chart points, release outcomes,
/// format memory or any label's priors.
/// </summary>
public static class RecordClubChannel {
	public const string Switch = "--record-clubs";
	public const string HoldSwitch = "--record-club-hold";
	private static bool configured;
	public static bool Enabled { get; private set; }
	/// <summary>Takes club units out of the album channel capacity instead of adding them (overlap test arm).</summary>
	public static bool HoldUnits { get; private set; }

	// Billboard 1966 International Buyers Guide (1961-65) and 1969-70 International Music-Record Directory
	// pp. 10-11 (1966-68). 1960 takes 1961's figure and 1969 holds 1968's: neither is sourced.
	private static readonly Dictionary<int, float> AnnualUnitsM = new() {
		[1960] = 45f, [1961] = 45f, [1962] = 45f, [1963] = 41f, [1964] = 38f, [1965] = 46f,
		[1966] = 47f, [1967] = 49f, [1968] = 49f, [1969] = 49f
	};
	/// <summary>Packages' share of the channel: about 7M of 45M in 1961, the midpoint of the 5-9M FTC-based estimate.</summary>
	internal const double PackageShare = .15;
	/// <summary>A club offered 200-400 records a month (FTC p. 352 n. 65).</summary>
	internal const int OfferListSize = 300;
	/// <summary>Columbia licensed nine labels (FTC p. 323).</summary>
	internal const int MaxLicencesPerOperator = 9;
	internal const int LicenceTermYears = 5;
	/// <summary>Weeks after release before a live LP joins the club offer. Assumption.</summary>
	internal const int HoldbackWeeks = 13;
	/// <summary>Annual retention of catalogue appeal. Assumption.</summary>
	internal const double AnnualRetention = .80;
	internal const double PruneFloor = 25;
	/// <summary>Opening backlist: this many weeks of a label's first-week LP sales. Assumption.</summary>
	internal const double OpeningSeedWeeks = 150;

	// FTC 72 F.T.C. 320-411: club price incl. postage (p. 351), total cost per licensed record (p. 354),
	// acquisition cost and licensor royalty per record (p. 323).
	internal const float ClubPrice = 2.37f, ClubTotalCostLicensed = 2.13f, LicensedAcquisitionCost = .875f, LicensorRoyalty = .178f;
	internal const float ClubOperatingCost = ClubTotalCostLicensed - LicensedAcquisitionCost;
	/// <summary>Artists' club royalty as a fraction of their ordinary rate on the club price. The FTC confirms the
	/// rate was depressed but this extract gives no figure. Assumption.</summary>
	internal const float ArtistClubFactor = .5f;

	private static readonly string[] HistoricalOperatorPrefixes = { "Columbia", "RCA", "Capitol" };

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Contains(Switch);
		HoldUnits = args.Contains(HoldSwitch);
		if (HoldUnits && !Enabled) throw new ArgumentException(HoldSwitch + " requires " + Switch + ".");
		configured = true;
	}

	internal static void ResetForChecks() { configured = false; Enabled = false; HoldUnits = false; State = null; ResetTelemetry(); }

	internal static void SetForChecks(bool enabled, bool hold = false) {
		configured = true; Enabled = enabled; HoldUnits = hold; State = null; ResetTelemetry();
	}

	public static RecordClubState State { get; private set; }

	public static RecordClubState Capture() => Enabled ? State?.Normalized() : null;

	public static void Restore(RecordClubState state) {
		if (state != null && !Enabled) throw new InvalidOperationException("Resume a record-club world with " + Switch + ".");
		State = state?.Normalized();
	}

	public static long AnnualUnits(int year) {
		int clamped = Math.Clamp(year, 1960, 1969);
		return (long)Math.Round(AnnualUnitsM[clamped] * 1_000_000d);
	}

	/// <summary>This week's national volume. Spread over 52 weeks with exact annual totals.</summary>
	public static long WeeklyUnits(int year, int weekOfYear) {
		long annual = AnnualUnits(year);
		int week = Math.Clamp(weekOfYear, 1, 52);
		return annual * week / 52 - annual * (week - 1) / 52;
	}

	/// <summary>An album-channel capacity once the club has taken its buyers (hold arm only).</summary>
	public static int HeldAlbumChannelCapacity(int capacity, long nationalAlbumCapacity, int year) {
		if (!Enabled || !HoldUnits || capacity <= 0 || nationalAlbumCapacity <= 0) return capacity;
		double weeklyClub = AnnualUnits(year) / 52d;
		return Math.Max(0, (int)Math.Round(capacity * Math.Max(0d, 1d - weeklyClub / nationalAlbumCapacity)));
	}

	// ---------------------------------------------------------------------------------------------------------
	// Catalogue ledger
	// ---------------------------------------------------------------------------------------------------------

	internal static float AdultShare(Genre genre, int year) {
		var weights = GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).SegmentWeights;
		return Math.Clamp(weights.GetValueOrDefault(nameof(AudienceSegment.AdultMOR)) +
			weights.GetValueOrDefault(nameof(AudienceSegment.JazzHiFiClassical)) +
			weights.GetValueOrDefault(nameof(AudienceSegment.FamilyChildrens)), 0f, 1f);
	}

	private static RecordClubState EnsureState() => State ??= new RecordClubState();

	/// <summary>A retired LP folds its lifetime sales into its label's catalogue for that act, once.</summary>
	public static void FoldRetired(RecordRuntimeData record, int year) {
		if (!Enabled || record?.baseRecord == null || record.baseRecord.format != ReleaseFormat.Album) return;
		if (record.baseRecord.isPlayerOwned || string.IsNullOrEmpty(record.baseRecord.labelId)) return;
		if (record.totalUnitsSold <= 0) return;
		Fold(record.baseRecord.labelId, record.baseRecord.artistId ?? "", record.totalUnitsSold,
			AdultShare(record.baseRecord.primaryGenre, year));
	}

	internal static void Fold(string labelId, string artistId, double units, float adultShare) {
		var state = EnsureState();
		string key = RecordClubState.Key(labelId, artistId);
		if (!state.Entries.TryGetValue(key, out var entry))
			state.Entries[key] = entry = new RecordClubEntry { LabelId = labelId, ArtistId = artistId };
		entry.Titles++;
		entry.Appeal += units;
		entry.AdultAppeal += units * adultShare;
	}

	/// <summary>An absorbed label's catalogue, backlist and club pass to its acquirer.</summary>
	public static void OnAcquired(string absorbedId, string acquirerId) {
		if (!Enabled || State == null || absorbedId == null || acquirerId == null || absorbedId == acquirerId) return;
		var state = State;
		foreach (var entry in state.Entries.Values.Where(e => e.LabelId == absorbedId).OrderBy(e => e.ArtistId, StringComparer.Ordinal).ToList()) {
			state.Entries.Remove(RecordClubState.Key(absorbedId, entry.ArtistId));
			string key = RecordClubState.Key(acquirerId, entry.ArtistId);
			if (!state.Entries.TryGetValue(key, out var target))
				state.Entries[key] = target = new RecordClubEntry { LabelId = acquirerId, ArtistId = entry.ArtistId };
			target.Titles += entry.Titles; target.Appeal += entry.Appeal; target.AdultAppeal += entry.AdultAppeal;
		}
		if (state.OpeningSeed.Remove(absorbedId, out var seed)) {
			var target = state.OpeningSeed.GetValueOrDefault(acquirerId) ?? new RecordClubSeed();
			target.Appeal += seed.Appeal; target.AdultAppeal += seed.AdultAppeal;
			state.OpeningSeed[acquirerId] = target;
		}
		if (state.OperatorIds.Remove(absorbedId) && !state.OperatorIds.Contains(acquirerId)) state.OperatorIds.Add(acquirerId);
		state.OperatorIds.Sort(StringComparer.Ordinal);
		state.Licences.RemoveAll(l => l.LicensorId == absorbedId || l.LicensorId == acquirerId && state.OperatorIds.Contains(acquirerId));
		foreach (var licence in state.Licences.Where(l => l.OperatorId == absorbedId)) licence.OperatorId = acquirerId;
	}

	private static void Decay() {
		double weekly = Math.Pow(AnnualRetention, 1d / 52d);
		var state = State;
		var pruned = new List<string>();
		foreach (var (key, entry) in state.Entries) {
			entry.Appeal *= weekly; entry.AdultAppeal *= weekly;
			if (entry.Appeal < PruneFloor) pruned.Add(key);
		}
		foreach (string key in pruned) state.Entries.Remove(key);
		foreach (var seed in state.OpeningSeed.Values) { seed.Appeal *= weekly; seed.AdultAppeal *= weekly; }
	}

	// ---------------------------------------------------------------------------------------------------------
	// Weekly allocation
	// ---------------------------------------------------------------------------------------------------------

	public sealed class ArtistShare { public string ArtistId; public double Units; }

	/// <summary>One label's club week. Own and licensed-in are an operator's; licensed-out and package are what a
	/// label's catalogue earned through someone else's club or a packager.</summary>
	public sealed class LabelWeek {
		public long OwnUnits, LicensedInUnits, LicensedOutUnits, PackageUnits;
		/// <summary>Club units by act on this label's own masters (own, licensed-out and package), for royalties.</summary>
		public readonly List<ArtistShare> Artists = new();
	}

	private sealed class Source {
		public string LabelId, ArtistId;
		public double Appeal, AdultAppeal;
	}

	/// <summary>Allocates this week's channel volume. Called once per live settlement, before booking.</summary>
	public static Dictionary<string, LabelWeek> Allocate(IReadOnlyList<AILabel> labels, IEnumerable<RecordRuntimeData> records, int year) {
		var result = new Dictionary<string, LabelWeek>(StringComparer.Ordinal);
		if (!Enabled) return result;
		var active = labels.Where(l => l != null && !l.isPlayerOwned && l.IsActive && !string.IsNullOrEmpty(l.labelId))
			.GroupBy(l => l.labelId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
		var recordList = records.Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Album && !r.baseRecord.isPlayerOwned).ToList();
		var state = EnsureState();
		if (!state.Initialized) Initialize(state, active, recordList, year);
		else Decay();
		if (year != state.LicenceYear) RenewLicences(state, active, recordList, year);
		if (year != state.Year) { state.Year = year; state.WeekInYear = 0; }
		state.WeekInYear++;

		var sources = BuildSources(state, active, recordList, year);
		// A 53rd settlement week sells nothing: the 52 before it already carry the exact annual total.
		long total = state.WeekInYear <= 52 ? WeeklyUnits(year, state.WeekInYear) : 0;
		long package = (long)Math.Round(total * PackageShare);
		long club = total - package;

		// Club slice: each operator's top-N offer list from its own and licensed catalogues.
		var licensedTo = state.Licences.Where(l => active.ContainsKey(l.LicensorId) && active.ContainsKey(l.OperatorId))
			.GroupBy(l => l.OperatorId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Select(l => l.LicensorId).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
		var offers = new List<(string operatorId, List<Source> list, double weight)>();
		foreach (string operatorId in state.OperatorIds.Where(active.ContainsKey)) {
			var catalogues = licensedTo.GetValueOrDefault(operatorId) ?? new HashSet<string>(StringComparer.Ordinal);
			var list = sources.Where(s => s.LabelId == operatorId || catalogues.Contains(s.LabelId))
				.OrderByDescending(s => s.Appeal).ThenBy(s => s.LabelId, StringComparer.Ordinal).ThenBy(s => s.ArtistId, StringComparer.Ordinal)
				.Take(OfferListSize).ToList();
			double weight = list.Sum(s => s.Appeal);
			if (weight > 0) offers.Add((operatorId, list, weight));
		}
		var operatorUnits = LargestRemainder(offers.Select(o => (o.operatorId, o.weight)).ToList(), club);
		foreach (var (operatorId, list, weight) in offers) {
			long units = operatorUnits.GetValueOrDefault(operatorId);
			if (units <= 0) continue;
			var bySource = LargestRemainder(list.GroupBy(s => s.LabelId, StringComparer.Ordinal)
				.Select(g => (g.Key, g.Sum(s => s.Appeal))).ToList(), units);
			foreach (var (sourceLabel, sourceUnits) in bySource) {
				if (sourceUnits <= 0) continue;
				bool own = sourceLabel == operatorId;
				Week(result, operatorId).OwnUnits += own ? sourceUnits : 0;
				Week(result, operatorId).LicensedInUnits += own ? 0 : sourceUnits;
				if (!own) Week(result, sourceLabel).LicensedOutUnits += sourceUnits;
				AddArtists(Week(result, sourceLabel), list.Where(s => s.LabelId == sourceLabel).ToList(), sourceUnits, s => s.Appeal);
			}
		}

		// Package slice: standard adult repertoire from every catalogue, sold through outside packagers.
		var packageWeights = sources.GroupBy(s => s.LabelId, StringComparer.Ordinal)
			.Select(g => (g.Key, g.Sum(s => s.AdultAppeal))).Where(p => p.Item2 > 0).ToList();
		foreach (var (labelId, units) in LargestRemainder(packageWeights, package)) {
			if (units <= 0) continue;
			Week(result, labelId).PackageUnits += units;
			AddArtists(Week(result, labelId), sources.Where(s => s.LabelId == labelId).ToList(), units, s => s.AdultAppeal);
		}
		return result;
	}

	private static LabelWeek Week(Dictionary<string, LabelWeek> result, string labelId) {
		if (!result.TryGetValue(labelId, out var week)) result[labelId] = week = new LabelWeek();
		return week;
	}

	private static void AddArtists(LabelWeek week, List<Source> sources, long units, Func<Source, double> weightOf) {
		double total = sources.Sum(weightOf);
		if (total <= 0) return;
		foreach (var source in sources) {
			if (string.IsNullOrEmpty(source.ArtistId)) continue;   // the opening backlist has no modelled acts
			double share = units * weightOf(source) / total;
			if (share > 0) week.Artists.Add(new ArtistShare { ArtistId = source.ArtistId, Units = share });
		}
	}

	private static List<Source> BuildSources(RecordClubState state, Dictionary<string, AILabel> active, List<RecordRuntimeData> records, int year) {
		var byKey = new Dictionary<string, Source>(StringComparer.Ordinal);
		Source Get(string labelId, string artistId) {
			string key = RecordClubState.Key(labelId, artistId);
			if (!byKey.TryGetValue(key, out var source)) byKey[key] = source = new Source { LabelId = labelId, ArtistId = artistId };
			return source;
		}
		foreach (var entry in state.Entries.Values) {
			if (!active.ContainsKey(entry.LabelId)) continue;
			var source = Get(entry.LabelId, entry.ArtistId);
			source.Appeal += entry.Appeal; source.AdultAppeal += entry.AdultAppeal;
		}
		foreach (var record in records) {
			string labelId = record.baseRecord.labelId;
			if (labelId == null || !active.ContainsKey(labelId) || record.weeksSinceRelease < HoldbackWeeks || record.totalUnitsSold <= 0) continue;
			var source = Get(labelId, record.baseRecord.artistId ?? "");
			source.Appeal += record.totalUnitsSold;
			source.AdultAppeal += record.totalUnitsSold * AdultShare(record.baseRecord.primaryGenre, year);
		}
		foreach (var (labelId, seed) in state.OpeningSeed) {
			if (!active.ContainsKey(labelId)) continue;
			var source = Get(labelId, "");
			source.Appeal += seed.Appeal; source.AdultAppeal += seed.AdultAppeal;
		}
		return byKey.Values.Where(s => s.Appeal > 0).OrderBy(s => s.LabelId, StringComparer.Ordinal).ThenBy(s => s.ArtistId, StringComparer.Ordinal).ToList();
	}

	private static void Initialize(RecordClubState state, Dictionary<string, AILabel> active, List<RecordRuntimeData> records, int year) {
		foreach (var label in active.Values.OrderBy(l => l.labelId, StringComparer.Ordinal)) {
			if (label.isHistorical && HistoricalOperatorPrefixes.Any(p => label.labelName?.StartsWith(p, StringComparison.Ordinal) == true))
				state.OperatorIds.Add(label.labelId);
			var albums = records.Where(r => r.baseRecord.labelId == label.labelId).ToList();
			double weekly = albums.Sum(r => (double)Math.Max(0, r.unitsThisWeek));
			if (weekly <= 0) continue;
			double adult = albums.Sum(r => Math.Max(0, r.unitsThisWeek) * (double)AdultShare(r.baseRecord.primaryGenre, year));
			state.OpeningSeed[label.labelId] = new RecordClubSeed { Appeal = weekly * OpeningSeedWeeks, AdultAppeal = adult * OpeningSeedWeeks };
		}
		state.OperatorIds.Sort(StringComparer.Ordinal);
		state.Initialized = true;
	}

	/// <summary>Each January the clubs fill vacant licences from the strongest unlicensed catalogues, taking turns in
	/// order of their own catalogue strength. Closed labels' catalogues and backlists are dropped.</summary>
	private static void RenewLicences(RecordClubState state, Dictionary<string, AILabel> active, List<RecordRuntimeData> records, int year) {
		foreach (string key in state.Entries.Where(kv => !active.ContainsKey(kv.Value.LabelId)).Select(kv => kv.Key).ToList()) state.Entries.Remove(key);
		foreach (string id in state.OpeningSeed.Keys.Where(id => !active.ContainsKey(id)).ToList()) state.OpeningSeed.Remove(id);
		state.OperatorIds.RemoveAll(id => !active.ContainsKey(id));
		state.Licences.RemoveAll(l => l.ExpiresYear <= year || !active.ContainsKey(l.LicensorId) || !state.OperatorIds.Contains(l.OperatorId));
		var strength = BuildSources(state, active, records, year).GroupBy(s => s.LabelId, StringComparer.Ordinal)
			.ToDictionary(g => g.Key, g => g.Sum(s => s.Appeal), StringComparer.Ordinal);
		var operators = state.OperatorIds.OrderByDescending(id => strength.GetValueOrDefault(id)).ThenBy(id => id, StringComparer.Ordinal).ToList();
		var candidates = new Queue<string>(strength.Where(kv => kv.Value > 0 && !state.OperatorIds.Contains(kv.Key) &&
				!state.Licences.Any(l => l.LicensorId == kv.Key))
			.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key));
		bool assigned = true;
		while (candidates.Count > 0 && assigned) {
			assigned = false;
			foreach (string operatorId in operators) {
				if (candidates.Count == 0) break;
				if (state.Licences.Count(l => l.OperatorId == operatorId) >= MaxLicencesPerOperator) continue;
				state.Licences.Add(new RecordClubLicence { LicensorId = candidates.Dequeue(), OperatorId = operatorId, ExpiresYear = year + LicenceTermYears });
				assigned = true;
			}
		}
		state.Licences.Sort((a, b) => string.CompareOrdinal(a.LicensorId, b.LicensorId));
		state.LicenceYear = year;
		SnapshotCatalogue(state, active, strength, year);
	}

	/// <summary>Largest-remainder split of an integer total by weight; ties break by key.</summary>
	internal static Dictionary<string, long> LargestRemainder(IReadOnlyList<(string key, double weight)> weights, long total) {
		var result = new Dictionary<string, long>(StringComparer.Ordinal);
		var positive = weights.Where(w => w.weight > 0 && double.IsFinite(w.weight)).OrderBy(w => w.key, StringComparer.Ordinal).ToList();
		double sum = positive.Sum(w => w.weight);
		if (total <= 0 || sum <= 0) return result;
		var exact = positive.Select(w => (w.key, value: total * w.weight / sum)).ToList();
		long assigned = 0;
		foreach (var (key, value) in exact) { long floor = (long)Math.Floor(value); result[key] = result.GetValueOrDefault(key) + floor; assigned += floor; }
		foreach (var (key, _) in exact.OrderByDescending(e => e.value - Math.Floor(e.value)).ThenBy(e => e.key, StringComparer.Ordinal)) {
			if (assigned >= total) break;
			result[key]++; assigned++;
		}
		return result;
	}

	// ---------------------------------------------------------------------------------------------------------
	// Telemetry and the Rising test. Not saved: cumulative over the process, like the off-chart singles base.
	// ---------------------------------------------------------------------------------------------------------

	public sealed class AnnualRow {
		public int Year; public string LabelId, Tier, Role;
		public long Units; public double Gross, Net, ArtistRoyalty;
	}
	public sealed class CatalogueRow {
		public int Year; public string LabelId, Role, LicensedTo;
		public int Entries, Titles; public double EarnedAppeal, SeedAppeal, TotalAppeal;
	}
	private static readonly Dictionary<(int, string, string), AnnualRow> annual = new();
	private static readonly List<CatalogueRow> catalogue = new();
	public static IEnumerable<AnnualRow> Annual => annual.Values.OrderBy(r => r.Year).ThenBy(r => r.LabelId, StringComparer.Ordinal).ThenBy(r => r.Role, StringComparer.Ordinal);
	public static IReadOnlyList<CatalogueRow> Catalogue => catalogue;

	private static void ResetTelemetry() { annual.Clear(); catalogue.Clear(); monthNet.Clear(); recentMonthNets.Clear(); }

	public static void Record(int year, AILabel label, string role, long units, double gross, double net, double royalty) {
		if (units <= 0) return;
		var key = (year, label.labelId, role);
		if (!annual.TryGetValue(key, out var row)) annual[key] = row = new AnnualRow { Year = year, LabelId = label.labelId, Role = role };
		row.Tier = label.tier.ToString();
		row.Units += units; row.Gross += gross; row.Net += net; row.ArtistRoyalty += royalty;
		monthNet[label.labelId] = monthNet.GetValueOrDefault(label.labelId) + net;
	}

	private static void SnapshotCatalogue(RecordClubState state, Dictionary<string, AILabel> active, Dictionary<string, double> strength, int year) {
		foreach (string labelId in strength.Keys.OrderBy(id => id, StringComparer.Ordinal)) {
			var entries = state.Entries.Values.Where(e => e.LabelId == labelId).ToList();
			catalogue.Add(new CatalogueRow {
				Year = year, LabelId = labelId,
				Role = state.OperatorIds.Contains(labelId) ? "Operator" : state.Licences.Any(l => l.LicensorId == labelId) ? "Licensor" : "Package",
				LicensedTo = state.Licences.FirstOrDefault(l => l.LicensorId == labelId)?.OperatorId ?? "",
				Entries = entries.Count, Titles = entries.Sum(e => e.Titles), EarnedAppeal = entries.Sum(e => e.Appeal),
				SeedAppeal = state.OpeningSeed.GetValueOrDefault(labelId)?.Appeal ?? 0, TotalAppeal = strength[labelId]
			});
		}
	}

	// A back catalogue is steady division profit, not release momentum (as MajorOffChartSingles).
	private static readonly Dictionary<string, double> monthNet = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, List<double>> recentMonthNets = new(StringComparer.Ordinal);

	public static float CloseMonthForMomentum(AILabel label) {
		if (!Enabled || label == null) return 0f;
		if (!recentMonthNets.TryGetValue(label.labelId, out var window)) recentMonthNets[label.labelId] = window = new List<double>();
		window.Add(monthNet.GetValueOrDefault(label.labelId));
		monthNet[label.labelId] = 0d;
		if (window.Count > MajorOffChartSingles.MomentumWindowMonths) window.RemoveAt(0);
		return (float)window.Average();
	}
}

/// <summary>Saved record-club state: the catalogue ledger, opening backlists, operators and licences.</summary>
public sealed class RecordClubState {
	public bool Initialized { get; set; }
	public int LicenceYear { get; set; }
	public int Year { get; set; }
	public int WeekInYear { get; set; }
	public Dictionary<string, RecordClubEntry> Entries { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, RecordClubSeed> OpeningSeed { get; set; } = new(StringComparer.Ordinal);
	public List<string> OperatorIds { get; set; } = new();
	public List<RecordClubLicence> Licences { get; set; } = new();

	public static string Key(string labelId, string artistId) => labelId + "|" + (artistId ?? "");

	/// <summary>A copy in canonical order, so a capture and its restored capture serialize identically.</summary>
	public RecordClubState Normalized() => new() {
		Initialized = Initialized, LicenceYear = LicenceYear, Year = Year, WeekInYear = WeekInYear,
		Entries = (Entries ?? new()).OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value.Copy(), StringComparer.Ordinal),
		OpeningSeed = (OpeningSeed ?? new()).OrderBy(kv => kv.Key, StringComparer.Ordinal)
			.ToDictionary(kv => kv.Key, kv => new RecordClubSeed { Appeal = kv.Value.Appeal, AdultAppeal = kv.Value.AdultAppeal }, StringComparer.Ordinal),
		OperatorIds = (OperatorIds ?? new()).OrderBy(id => id, StringComparer.Ordinal).ToList(),
		Licences = (Licences ?? new()).OrderBy(l => l.LicensorId, StringComparer.Ordinal)
			.Select(l => new RecordClubLicence { LicensorId = l.LicensorId, OperatorId = l.OperatorId, ExpiresYear = l.ExpiresYear }).ToList()
	};
}

public sealed class RecordClubEntry {
	public string LabelId { get; set; }
	public string ArtistId { get; set; }
	public int Titles { get; set; }
	public double Appeal { get; set; }
	public double AdultAppeal { get; set; }
	public RecordClubEntry Copy() => new() { LabelId = LabelId, ArtistId = ArtistId, Titles = Titles, Appeal = Appeal, AdultAppeal = AdultAppeal };
}

public sealed class RecordClubSeed {
	public double Appeal { get; set; }
	public double AdultAppeal { get; set; }
}

public sealed class RecordClubLicence {
	public string LicensorId { get; set; }
	public string OperatorId { get; set; }
	public int ExpiresYear { get; set; }
}
