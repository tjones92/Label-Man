using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Experimental returning audience, earned from settled purchases and saved with the artist.
/// National reach is the sum of regional/segment cohorts. Replaces their share of anonymous demand;
/// all intent still passes through inventory, store capacity and the existing consumer purse.
/// --artist-fanbase=0 observes acquisitions with an exact identity demand path. No global RNG.
/// This first probe covers AI singles, not the player's separately settled daily trunk purchases.
/// </summary>
public static class ArtistFanbase {
	public const string TrialPrefix = "--artist-fanbase=";
	public const string RecruitPrefix = "--artist-fanbase-recruit=";
	public const string HalfLifePrefix = "--artist-fanbase-half-life=";
	public const double CohortCapacityShare = .20;
	public static bool Enabled { get; private set; }
	public static double TrialRate { get; private set; }
	public static double Recruitment { get; private set; } = .35;
	public static double HalfLife { get; private set; } = 104;
	private static bool configured;
	private static readonly Dictionary<string, double> attention = new(StringComparer.Ordinal);
	private static readonly Dictionary<(string, int, Genre), Dictionary<string, double>> routes = new();
	private static readonly Dictionary<(string, int), Dictionary<string, double>> capacities = new();

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Any(a => a.StartsWith(TrialPrefix, StringComparison.Ordinal));
		TrialRate = Read(args, TrialPrefix, 0, 0, 1);
		Recruitment = Read(args, RecruitPrefix, .35, 0, 1);
		HalfLife = Read(args, HalfLifePrefix, 104, 13, 520);
		if (!Enabled && args.Any(a => a.StartsWith(RecruitPrefix) || a.StartsWith(HalfLifePrefix)))
			throw new ArgumentException("Fanbase recruitment/retention requires " + TrialPrefix);
		configured = true;
	}
	private static double Read(string[] args, string prefix, double fallback, double min, double max) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + prefix + " once.");
		if (values.Length == 0) return fallback;
		if (!double.TryParse(values[0][prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
			|| !double.IsFinite(v) || v < min || v > max) throw new ArgumentException(prefix + $" requires [{min}, {max}].");
		return v;
	}
	internal static void SetForChecks(bool enabled, double trial = .25, double recruitment = .35, double halfLife = 104) {
		configured = true; Enabled = enabled; TrialRate = trial; Recruitment = recruitment; HalfLife = halfLife;
		attention.Clear(); routes.Clear(); capacities.Clear();
	}
	private static bool Single(RecordRuntimeData record) => record?.baseRecord != null &&
		record.baseRecord.format == ReleaseFormat.Single && !record.baseRecord.isPlayerOwned;
	private static int Year => TimeManager.Instance?.CurrentDate.year ?? 1960;
	private static int Week => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
	private static double Weight(RecordRuntimeData record) => Math.Pow(.5, Math.Max(0, record.weeksSinceRelease - 1) / 4d);

	private static Dictionary<string, double> Capacity(MarketRegion region, int year) {
		var key = (region.regionId, year);
		if (capacities.TryGetValue(key, out var result)) return result;
		var model = region.segmentCapacities ?? SegmentCapacityModel.Create(region, year);
		double population = Math.Max(0, region.GetRecordBuyingPopulation());
		result = new();
		foreach (var pair in model.Shares) result[pair.Key.ToString()] = population * pair.Value * CohortCapacityShare;
		capacities[key] = result; return result;
	}
	private static Dictionary<string, double> Route(Genre genre, MarketRegion region, int year) {
		genre = GenreCatalog.MapLegacy(genre, year);
		var key = (region.regionId, year, genre);
		if (routes.TryGetValue(key, out var result)) return result;
		result = new();
		if (GenreCatalog.TryGet(genre, out GenreProfile profile)) {
			var caps = Capacity(region, year);
			foreach (var pair in profile.SegmentWeights)
				if (pair.Value > 0 && caps.GetValueOrDefault(pair.Key) > 0) result[pair.Key] = pair.Value * caps[pair.Key];
			double total = result.Values.Sum();
			if (total > 0) foreach (string segment in result.Keys.ToArray()) result[segment] /= total;
		}
		routes[key] = result; return result;
	}
	internal static ArtistFanbaseData Ensure(SimulatedArtist artist, IEnumerable<MarketRegion> regions, int week, int year) {
		if (artist.fanbase != null) return artist.fanbase;
		var data = new ArtistFanbaseData { lastDecayWeek = week };
		// Explicit opening estimate, using only pre-simulation public history, never current/future chart success.
		double strength = artist.publicHistory?.Where(e => e.year < 1960 && e.peakPosition is > 0 and <= 100)
			.Sum(e => (e.peakPosition == 1 ? .14 : e.peakPosition <= 10 ? .07 : e.peakPosition <= 40 ? .025 : .005)
				* Math.Pow(.5, (year - e.year) / 3d)) ?? 0;
		strength = 1 - Math.Exp(-strength);
		if (strength > 0) foreach (var region in regions) {
			var heads = new Dictionary<string, double>();
			var cap = Capacity(region, year);
			foreach (var pair in Route(artist.primaryGenre, region, year)) {
				double n = Math.Min(cap[pair.Key], region.GetRecordBuyingPopulation() * .02 * strength * pair.Value);
				if (n >= 1) heads[pair.Key] = n;
			}
			if (heads.Count > 0) data.regions[region.regionId] = heads;
		}
		data.openingHeads = Heads(data);
		artist.fanbase = data; return data;
	}
	public static double Heads(ArtistFanbaseData data) => data?.regions?.Values.Sum(x => x.Values.Sum()) ?? 0;
	internal static void Decay(ArtistFanbaseData data, int week) {
		if (week <= data.lastDecayWeek) return;
		double retention = Math.Pow(.5, (week - data.lastDecayWeek) / HalfLife);
		foreach (var region in data.regions.Values) foreach (string segment in region.Keys.ToArray()) {
			double before = region[segment]; region[segment] *= retention;
			data.departedHeads += before - region[segment];
		}
		data.lastDecayWeek = week;
		data.newHeadsThisWeek = data.returningPurchasesThisWeek = 0;
	}
	public static void PrepareWeek(IEnumerable<SimulatedArtist> artists, IEnumerable<RecordRuntimeData> records,
		IEnumerable<MarketRegion> regions, int week, int year) {
		if (!Enabled) return;
		attention.Clear();
		foreach (var artist in artists) {
			if (artist == null || artist.isPlayerOwned) continue;
			if (artist.fanbase != null || artist.publicHistory?.Any(e => e.year < 1960) == true)
				Decay(Ensure(artist, regions, week - 1, year), week);
		}
		foreach (var record in records) if (Single(record) && record.fanAudience != null && record.weeksSinceRelease <= 26)
			attention[record.baseRecord.artistId] = attention.GetValueOrDefault(record.baseRecord.artistId) + Weight(record);
	}
	public static void CaptureLaunch(RecordRuntimeData record, SimulatedArtist artist, IEnumerable<MarketRegion> regions, int week, int year) {
		if (!Enabled || !Single(record) || artist == null || artist.isPlayerOwned || week <= 0 ||
			record.baseRecord.releaseDate <= GameDate.StartDate || record.fanAudience != null) return;
		var audience = Ensure(artist, regions, week, year);
		Decay(audience, week);
		var snapshot = new FanReleaseAudience { launchWeek = week };
		foreach (var region in regions) if (audience.regions.TryGetValue(region.regionId, out var heads)) {
			var route = Route(record.baseRecord.primaryGenre, region, year);
			if (route.Count == 0) continue;
			double max = route.Values.Max();
			var cohort = new FanReleaseRegion();
			foreach (var pair in heads) if (route.TryGetValue(pair.Key, out double compatibility)) {
				double n = pair.Value * Math.Min(1, Math.Sqrt(compatibility / max));
				if (n >= 1) cohort.initial[pair.Key] = n;
			}
			if (cohort.initial.Count > 0) snapshot.regions[region.regionId] = cohort;
		}
		record.fanAudience = snapshot; // Even a cold empty snapshot cannot refresh from its own later success.
	}
	public static float Demand(RecordRuntimeData record, MarketRegion region, float quality, float awareness,
		float originalRaw, AILabel label) {
		if (!Enabled || !Single(record) || record.fanAudience == null ||
			!record.fanAudience.regions.TryGetValue(region.regionId, out var cohort)) return originalRaw;
		if (TrialRate == 0) {
			cohort.pending.Clear(); cohort.anonymousRaw = originalRaw; cohort.fanRaw = cohort.clearedFans = 0;
			return originalRaw;
		}
		if (record.weeksSinceRelease > 26)
			return Blend(cohort, originalRaw, region.GetRecordBuyingPopulation(), quality, awareness, 1, 0, 0);
		double cold = SingleCommercialFit.WithoutHitSupport(record, region.regionId, label);
		double attentionShare = Math.Min(1, Weight(record) / Math.Max(Weight(record), attention.GetValueOrDefault(record.baseRecord.artistId)));
		return Blend(cohort, originalRaw, region.GetRecordBuyingPopulation(), quality, awareness, cold,
			attentionShare, Math.Max(0, Week - record.fanAudience.launchWeek));
	}
	internal static float Blend(FanReleaseRegion cohort, float originalRaw, double population, double quality,
		double awareness, double coldFit, double attentionShare, int age) {
		cohort.pending.Clear(); cohort.fanRaw = cohort.clearedFans = 0;
		double initial = cohort.initial.Values.Sum();
		double excluded = Math.Clamp(initial / Math.Max(1, population), 0, 1);
		cohort.anonymousRaw = originalRaw * (1 - excluded);
		// Fans are more willing to audition a disappointing record, not guaranteed to buy it.
		double response = .5 + .5 * Math.Clamp(coldFit, 0, 1);
		double probability = TrialRate * Math.Pow(Math.Clamp(quality, 0, 1), 1.5) * response
			* Math.Clamp(awareness + .35, 0, 1) * attentionShare * Math.Pow(.5, age / 8d);
		foreach (var pair in cohort.initial) {
			double remaining = Math.Max(0, pair.Value - cohort.purchased.GetValueOrDefault(pair.Key));
			double demand = remaining * Math.Clamp(probability, 0, 1);
			cohort.pending[pair.Key] = demand; cohort.fanRaw += demand;
		}
		return (float)(cohort.anonymousRaw + cohort.fanRaw);
	}
	internal static double SettleCohort(FanReleaseRegion cohort, double cleared) {
		double total = cohort.anonymousRaw + cohort.fanRaw;
		double fans = total > 0 ? Math.Min(cleared, cleared * cohort.fanRaw / total) : 0;
		cohort.clearedFans = 0;
		if (cohort.fanRaw > 0) foreach (var pair in cohort.pending) {
			double remaining = Math.Max(0, cohort.initial[pair.Key] - cohort.purchased.GetValueOrDefault(pair.Key));
			double n = Math.Min(remaining, fans * pair.Value / cohort.fanRaw);
			cohort.purchased[pair.Key] = cohort.purchased.GetValueOrDefault(pair.Key) + n;
			cohort.clearedFans += n;
		}
		return cohort.clearedFans;
	}
	public static void SettleWeek(IEnumerable<RecordRuntimeData> records, IEnumerable<MarketRegion> regions, int week, int year) {
		if (!Enabled) return;
		var markets = regions.ToDictionary(r => r.regionId);
		var gains = new Dictionary<(SimulatedArtist Artist, string Region, string Segment), (double Gain, double Loss)>();
		var touched = new HashSet<SimulatedArtist>();
		foreach (var record in records) {
			if (!Single(record)) continue;
			var artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
			if (artist == null || artist.isPlayerOwned || artist.fanbase?.lastSettlementWeek == week) continue;
			var audience = Ensure(artist, regions, week, year);
			touched.Add(artist);
			double q = Math.Clamp(record.GetQuality(), 0, 1);
			foreach (var pair in record.regionalData) {
				if (!markets.TryGetValue(pair.Key, out var region)) continue;
				double cleared = Math.Max(0, pair.Value.unitsSoldThisWeek);
				if (cleared <= 0) continue;
				FanReleaseRegion cohort = null;
				record.fanAudience?.regions.TryGetValue(pair.Key, out cohort);
				double returning = cohort != null && record.weeksSinceRelease <= 26 ? SettleCohort(cohort, cleared) : 0;
				audience.returningPurchasesThisWeek += returning;
				double acquire = Math.Max(0, cleared - returning) * Recruitment * Math.Clamp((q - .4) / .4, 0, 1);
				double loss = returning * .25 * Math.Clamp((.6 - q) / .3, 0, 1);
				foreach (var route in Route(record.baseRecord.primaryGenre, region, year)) {
					var key = (artist, pair.Key, route.Key); var old = gains.GetValueOrDefault(key);
					gains[key] = (old.Gain + acquire * route.Value, old.Loss + loss * route.Value);
				}
			}
		}
		foreach (var pair in gains) {
			var key = pair.Key; var data = key.Artist.fanbase;
			if (!data.regions.TryGetValue(key.Region, out var heads)) data.regions[key.Region] = heads = new();
			double before = heads.GetValueOrDefault(key.Segment);
			double cap = Capacity(markets[key.Region], year)[key.Segment];
			double kept = Math.Max(0, before - pair.Value.Loss);
			double gained = cap > 0 ? Math.Max(0, cap - kept) * (1 - Math.Exp(-pair.Value.Gain / cap)) : 0;
			heads[key.Segment] = Math.Min(cap, kept + gained);
			data.recruitedHeads += gained; data.newHeadsThisWeek += gained;
			data.departedHeads += before - kept + Math.Max(0, kept - cap);
		}
		foreach (var artist in touched) artist.fanbase.lastSettlementWeek = week;
	}
}
