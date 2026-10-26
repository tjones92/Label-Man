using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Rack-jobber LP channel (SimTools/RackJobberChannelDesign.md). Billboard splits 1961 store-and-rack LP units
/// into stores 68M and racks 60M; the model's one retail market already sells about the stores' figure and
/// clears all its serviceable demand. Rack jobbers ran the record racks of supermarkets, variety stores and
/// drugstores (92% of NARM's 24,000 racks in 1962; department stores 6%), stocked the current best sellers
/// (about a third of rack space) plus other proven product, and sold to household shoppers buying on impulse
/// from the display (Capitol's Brown Meggs to NARM, 1966).
///
/// When on, each region runs a second LP sale after store clearing. The rack carries a hit tier and a
/// catalogue tier of live LPs ranked by this week's store sales, with hysteresis. A racked title sells to the
/// region's own rack-shopper pool through its own exhaustion ledger; being on the rack is the awareness.
/// Rack units are ordinary sales of the record for the label's books, the artist's royalty and release
/// memory, but they never enter chart points, store exhaustion, regional signals or retirement. No random
/// draws; the units come only from records the store market already sold this week.
/// </summary>
public static class RackJobberChannel {
	public const string Switch = "--rack-jobbers";
	public const string ScaleSwitch = "--rack-shopper-scale=";
	public const string SharedSwitch = "--rack-shared-exhaustion";
	public const string ReturnsSwitch = "--rack-returns=";
	public const string TurnoverSwitch = "--rack-turnover=";
	public static float Turnover { get; private set; } = UnitsPerRackWeek;
	public static bool SharedExhaustion { get; private set; }
	public static float ReturnRate { get; private set; }
	private static bool configured;
	public static bool Enabled { get; private set; }

	/// <summary>The channel's one calibration scale: rack shoppers per region-weighted population. Fitted once on
	/// seed 1001 to front-line rack LPs in 1961 (about 36-42M: 60M less a 30-40% budget share) and frozen.</summary>
	internal const float DefaultShopperScale = .026f;
	public static float ShopperScale { get; private set; } = DefaultShopperScale;

	// Assortment. Titles per rack are unsourced; the sources give the split (a third best sellers, 1965).
	internal const int HitTitles = 25, CatalogueTitles = 50;
	internal const float HysteresisFactor = 1.25f;
	internal const int FaceOutRanks = 10;
	internal const float FaceOutExposure = 1f, HitExposure = .70f, CatalogueExposure = .35f;

	// Shelf. NARM had 24,000 racks for about half of all rack dollars in 1962, so about 47k nationally against
	// a rack era weight of .456: about 573 racks per million people at full reach. About 30 LPs per rack a week
	// is the inferred 1962 turnover (76M / 47k / 52). Inferences, not sources; telemetry reports binding.
	internal const float RacksPerMillionAtFullReach = 573f;
	internal const float UnitsPerRackWeek = 30f;

	// Same album conversion base as stores (AlbumSimulator.BasePurchaseRate).
	internal const float BasePurchaseRate = .095f;

	// Rack shoppers by audience segment, relative: household adults, families and country listeners over youth.
	private static readonly Dictionary<string, float> SegmentTilt = new() {
		[nameof(AudienceSegment.AdultMOR)] = 1f, [nameof(AudienceSegment.FamilyChildrens)] = 1f,
		[nameof(AudienceSegment.CountryWestern)] = .8f, [nameof(AudienceSegment.MainstreamAM)] = .7f,
		[nameof(AudienceSegment.JazzHiFiClassical)] = .5f, [nameof(AudienceSegment.RegionalLatin)] = .5f,
		[nameof(AudienceSegment.GospelChurch)] = .5f, [nameof(AudienceSegment.CollegeFolk)] = .4f,
		[nameof(AudienceSegment.Youth)] = .3f, [nameof(AudienceSegment.UrbanRnB)] = .3f,
		[nameof(AudienceSegment.UndergroundFM)] = .1f
	};
	/// <summary>Soundtracks and cast albums sold to the adult and family shopper whatever their mapped genre.</summary>
	internal const float SoundtrackGenreWeight = .9f;

	public const int TierNone = 0, TierHit = 1, TierCatalogue = 2;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Contains(Switch);
		SharedExhaustion = args.Contains(SharedSwitch);
		string returns = args.FirstOrDefault(a => a.StartsWith(ReturnsSwitch, StringComparison.Ordinal));
		if ((SharedExhaustion || returns != null) && !Enabled)
			throw new ArgumentException("Rack sensitivity switches require " + Switch + ".");
		if (returns != null) {
			if (!float.TryParse(returns.Substring(ReturnsSwitch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float rate) ||
				!float.IsFinite(rate) || rate < 0f || rate >= 1f) throw new ArgumentException("Invalid " + returns + ".");
			ReturnRate = rate;
		}
		string scale = args.FirstOrDefault(a => a.StartsWith(ScaleSwitch, StringComparison.Ordinal));
		string turnover = args.FirstOrDefault(a => a.StartsWith(TurnoverSwitch, StringComparison.Ordinal));
		if (turnover != null) {
			if (!Enabled || !float.TryParse(turnover[TurnoverSwitch.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
				!float.IsFinite(value) || value <= 0 || value > 1000) throw new ArgumentException("Invalid " + turnover);
			Turnover = value;
		}
		if (scale != null) {
			if (!Enabled) throw new ArgumentException(ScaleSwitch + " requires " + Switch + ".");
			if (!float.TryParse(scale.Substring(ScaleSwitch.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
				!float.IsFinite(value) || value <= 0f)
				throw new ArgumentException("Invalid " + scale + ".");
			ShopperScale = value;
		}
		configured = true;
	}

	internal static void ResetForChecks() { configured = false; Enabled = false; SharedExhaustion = false; ReturnRate = 0f; ShopperScale = DefaultShopperScale; Turnover = UnitsPerRackWeek; ResetTelemetry(); }
	internal static void SetForChecks(bool enabled, float scale = DefaultShopperScale, bool shared = false, float returns = 0f) {
		configured = true; Enabled = enabled; ShopperScale = scale; SharedExhaustion = shared; ReturnRate = returns; ResetTelemetry();
	}

	// Both ledgers stay separate for accounting and saves; only the stress arm shares their
	// numerator. Stores see last week's racks; racks also see this week's already-cleared stores.
	internal static double ExhaustionUnits(RegionalRecordData data, bool rack) =>
		Enabled && SharedExhaustion ? (double)data.unitsSoldTotal + data.rackUnitsTotal
			: rack ? data.rackUnitsTotal - (double)data.rackStoreBuyerUnitsTotal : data.unitsSoldTotal + (double)data.rackStoreBuyerUnitsTotal;

	// Sell-through is already net of returns. A fraction r of shipments returned implies
	// r/(1-r) extra unsold copies per sold copy. This stress assumes full write-off, no salvage.
	internal static float ReturnWriteOff(float soldManufacturingAndFreight) =>
		Enabled && ReturnRate > 0f ? soldManufacturingAndFreight * ReturnRate / (1f - ReturnRate) : 0f;

	/// <summary>A saved world with rack state must resume with the channel on.</summary>
	public static void RequireForResume() {
		if (!Enabled) throw new InvalidOperationException("Resume a rack-jobber world with " + Switch + ".");
	}

	public static float Reach(int year) => ChartSimulator.GetRackJobberEraWeight(year);

	public static float ShopperPool(MarketRegion region, int year) =>
		Mathf.Max(0f, region.population) * 1_000_000f * Mathf.Sqrt(Mathf.Max(0f, region.averageIncome)) * Reach(year) * ShopperScale;

	public static int Shelf(MarketRegion region, int year) =>
		Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, region.population) * RacksPerMillionAtFullReach * Reach(year) * Turnover));

	public static float GenreWeight(RecordRuntimeData record, int year) {
		Album album = record.baseRecord.album;
		if (album?.albumFormat == AlbumFormat.Soundtrack && album.externalMedia != null) return SoundtrackGenreWeight;
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year));
		float tilt = 0f;
		foreach (var pair in profile.SegmentWeights) tilt += pair.Value * SegmentTilt.GetValueOrDefault(pair.Key);
		return Mathf.Clamp(profile.GetBaseline(year) * tilt, 0f, 1f);
	}

	public static float Exposure(int tier, int rank, float awareness) {
		float display = tier == TierHit ? (rank <= FaceOutRanks ? FaceOutExposure : HitExposure)
			: tier == TierCatalogue ? CatalogueExposure : 0f;
		return display <= 0f ? 0f : Mathf.Max(display, Mathf.Clamp(awareness, 0f, 1f));
	}

	public static float Conversion(RecordRuntimeData record, int year, int month, bool liveTick) =>
		BasePurchaseRate * Mathf.Pow(Mathf.Clamp(record.GetQuality(), 0f, 1f), 2.5f) *
		(0.72f + record.wordOfMouth * 0.45f) * MarketSeasonality.GetAlbumSalesMultiplier(year, month, liveTick);

	internal sealed class Candidate {
		public RecordRuntimeData Record;
		public RegionalRecordData Data;
		public int StoreUnits, Tier, Rank;
		public int StoreChoice;
		public float AutonomousDemand;
		public bool LabelShips, PastPeak;
		public float Demand;
		public int Units;
	}

	/// <summary>
	/// Assigns the hit and catalogue tiers for one region. Candidates are ranked by store units, ties by record id.
	/// An incumbent keeps its tier while it ranks inside 1.25 times the tier size; newcomers fill the remaining
	/// slots; if incumbents overflow, the worst-ranked leave first. Demoted hits that are past peak may enter the
	/// catalogue tier. Non-candidates and unchosen candidates get no tier.
	/// </summary>
	internal static void AssignTiers(List<Candidate> candidates, int hitTitles = HitTitles, int catalogueTitles = CatalogueTitles) {
		var ranked = candidates.Where(c => c.StoreUnits > 0)
			.OrderByDescending(c => c.StoreUnits).ThenBy(c => c.Record.baseRecord.recordId, StringComparer.Ordinal).ToList();
		var previous = candidates.ToDictionary(c => c, c => c.Tier);
		foreach (var c in candidates) { c.Tier = TierNone; c.Rank = 0; }
		List<Candidate> Fill(List<Candidate> pool, int size, int tier) {
			int keepDepth = Mathf.CeilToInt(size * HysteresisFactor);
			var chosen = new List<Candidate>();
			for (int i = 0; i < pool.Count && i < keepDepth; i++) if (previous[pool[i]] == tier) chosen.Add(pool[i]);
			for (int i = 0; i < pool.Count && chosen.Count < size; i++) if (!chosen.Contains(pool[i])) chosen.Add(pool[i]);
			chosen = chosen.OrderBy(pool.IndexOf).Take(size).ToList();
			for (int i = 0; i < chosen.Count; i++) { chosen[i].Tier = tier; chosen[i].Rank = i + 1; }
			return chosen;
		}
		Fill(ranked, hitTitles, TierHit);
		Fill(ranked.Where(c => c.Tier == TierNone && c.PastPeak).ToList(), catalogueTitles, TierCatalogue);
	}

	/// <summary>Largest-remainder rounding of demand to whole units under the shelf, stable by record id.</summary>
	internal static void AllocateUnits(List<Candidate> racked, int shelf) {
		foreach (var c in racked) c.Units = 0;
		double demand = racked.Sum(c => (double)Math.Max(0f, c.Demand));
		long total = Math.Min(Math.Max(0, shelf), (long)Math.Floor(demand));
		if (total <= 0 || demand <= 0) return;
		var parts = racked.Where(c => c.Demand > 0f).OrderBy(c => c.Record.baseRecord.recordId, StringComparer.Ordinal)
			.Select(c => (c, exact: c.Demand / demand * total)).ToList();
		long given = 0;
		foreach (var (c, exact) in parts) { c.Units = (int)Math.Floor(exact); given += c.Units; }
		foreach (var (c, _) in parts.OrderByDescending(p => p.exact - Math.Floor(p.exact))
			.ThenBy(p => p.c.Record.baseRecord.recordId, StringComparer.Ordinal).Take((int)(total - given))) c.Units++;
	}

	/// <summary>
	/// The weekly rack pass, after store clearing and before weekly sales are finalized. Sets each record's and
	/// region's week-local rack units and advances the saved rack ledgers and tiers.
	/// </summary>
	public static void ClearWeek(IReadOnlyList<RecordRuntimeData> records, IReadOnlyList<MarketRegion> regions,
		Func<string, AILabel> labelOf, int week, int year, int month, bool liveTick,
		Func<RecordRuntimeData, string, int> pendingStoreUnits = null) {
		// Off, the week-local fields are never written and stay zero.
		if (!Enabled) return;
		BudgetLpChannel.BeginWeek();
		LpChannelExperiment.BeginWeek();
		foreach (RecordRuntimeData record in records) {
			record.rackUnitsThisWeek = 0;
			foreach (RegionalRecordData data in record.regionalData.Values) data.rackUnitsThisWeek = 0;
		}
		var albums = records.Where(r => r.baseRecord.format == ReleaseFormat.Album && !r.baseRecord.isPlayerOwned).ToList();
		foreach (MarketRegion region in regions) {
			var budget = BudgetLpChannel.Enabled ? BudgetLpChannel.Offers(region, week, year, month, liveTick, labelOf,
				albums.GroupBy(r => r.baseRecord.primaryGenre).ToDictionary(g => g.Key,
					g => g.Sum(r => pendingStoreUnits?.Invoke(r, region.regionId) ?? r.regionalData.GetValueOrDefault(region.regionId)?.unitsSoldThisWeek ?? 0))) : null;
			var candidates = new List<Candidate>();
			foreach (RecordRuntimeData record in albums) {
				if (!record.regionalData.TryGetValue(region.regionId, out RegionalRecordData data) || data.awaitingArrival) continue;
				AILabel label = labelOf(record.baseRecord.labelId);
				bool ships = label?.HasDistributionInRegionForRecord(region.regionId, record.baseRecord.recordId) == true;
				bool proven = ChartSimulator.GetRackJobberAccess(record.currentPosition, data.peakBreakoutScore) > 0f;
				if (!ships && !proven) { data.rackTier = TierNone; continue; }
				candidates.Add(new Candidate {
					Record = record, Data = data, StoreUnits = pendingStoreUnits?.Invoke(record, region.regionId) ?? data.unitsSoldThisWeek, Tier = data.rackTier,
					LabelShips = ships, PastPeak = record.weeksSincePeakUnits > 0
				});
			}
			AssignTiers(candidates, HitTitles, LpChannelExperiment.CatalogueSlots(budget?.Count ?? 0));
			float pool = ShopperPool(region, year);
			var racked = new List<Candidate>();
			foreach (var c in candidates) {
				c.Data.rackTier = c.Tier;
				if (c.Tier == TierNone) continue;
				float titlePool = pool * GenreWeight(c.Record, year);
				float penetration = SharedExhaustion ? (float)((ExhaustionUnits(c.Data, rack: true) + (pendingStoreUnits != null ? c.StoreUnits : 0)) / Mathf.Max(1f, titlePool))
					: (c.Data.rackUnitsTotal - c.Data.rackStoreBuyerUnitsTotal) / Mathf.Max(1f, titlePool);
				penetration += BudgetLpChannel.SourcePurchases(c.Record.baseRecord.recordId, region.regionId) / Mathf.Max(1f, titlePool);
				c.Demand = titlePool * Exposure(c.Tier, c.Rank, c.Record.awareness) * Conversion(c.Record, year, month, liveTick) *
					AlbumSimulator.CalculateAlbumExhaustion(penetration);
				racked.Add(c);
				c.AutonomousDemand = c.Demand;
				LpChannelExperiment.Plan(region.regionId, c, year);
				c.Demand += c.StoreChoice;
			}
			if (budget != null) LpChannelExperiment.ReserveBudgetBuyers(region.regionId, budget, racked);
			int shelf = Shelf(region, year);
			if (budget != null) {
				BudgetLpChannel.AllocateShared(racked, budget, shelf);
				BudgetLpChannel.Complete(region, week, year, shelf, racked.Sum(c => c.Units));
			} else AllocateUnits(racked, shelf);
			LpChannelExperiment.CompleteFront(region.regionId, racked);
			var row = new RegionWeekRow { Week = week, Year = year, RegionId = region.regionId, Pool = pool, Shelf = shelf };
			foreach (var c in racked) {
				c.Data.rackUnitsThisWeek = c.Units;
				c.Data.rackUnitsTotal += c.Units;
				c.Data.rackStoreBuyerUnitsTotal += LpChannelExperiment.FrontSwitches(region.regionId, c.Record.baseRecord.recordId);
				c.Record.rackUnitsThisWeek += c.Units;
				row.Demand += c.Demand; row.Units += c.Units;
				if (c.Tier == TierHit) { row.HitTitles++; row.HitUnits += c.Units; } else { row.CatalogueTitles++; row.CatalogueUnits += c.Units; }
				if (!c.LabelShips) row.UnshippedUnits += c.Units;
				AILabel label = labelOf(c.Record.baseRecord.labelId);
				titleRows.Add(new TitleWeekRow {
					Week = week, Year = year, RecordId = c.Record.baseRecord.recordId, LabelId = c.Record.baseRecord.labelId,
					LabelTier = label?.tier.ToString() ?? "Unknown", RegionId = region.regionId, Tier = c.Tier, Rank = c.Rank,
					Exposure = Exposure(c.Tier, c.Rank, c.Record.awareness), Demand = c.Demand, Units = c.Units,
					StoreUnits = c.StoreUnits, Age = c.Record.weeksSinceRelease, ChartPosition = c.Record.currentPosition,
					LabelShips = c.LabelShips
				});
			}
			row.ShelfBound = row.Demand + (budget?.Sum(o => o.Demand) ?? 0) >= shelf && shelf > 0;
			regionRows.Add(row);
		}
	}

	// Telemetry: process-cumulative, written by the audit runner at close. No saved state.
	public sealed class RegionWeekRow {
		public int Week, Year, Shelf, HitTitles, CatalogueTitles, Units, HitUnits, CatalogueUnits, UnshippedUnits;
		public string RegionId;
		public float Pool, Demand;
		public bool ShelfBound;
	}
	public sealed class TitleWeekRow {
		public int Week, Year, Tier, Rank, Units, StoreUnits, Age, ChartPosition;
		public string RecordId, LabelId, LabelTier, RegionId;
		public float Exposure, Demand;
		public bool LabelShips;
	}
	private static readonly List<RegionWeekRow> regionRows = new();
	private static readonly List<TitleWeekRow> titleRows = new();
	public sealed class FinanceRow {
		public int Week, Year, Units;
		public string LabelId, LabelTier;
		public double Gross, SoldCogs, ReturnWriteOff;
	}
	private static readonly Dictionary<(int, string), FinanceRow> financeRows = new();
	public static IEnumerable<FinanceRow> FinanceRows => financeRows.Values.OrderBy(r => r.Week).ThenBy(r => r.LabelId, StringComparer.Ordinal);
	public static void RecordFinance(int week, int year, AILabel label, int units, float gross, float soldCogs, float writeOff) {
		var key = (week, label.labelId);
		if (!financeRows.TryGetValue(key, out var row)) financeRows[key] = row = new FinanceRow {
			Week = week, Year = year, LabelId = label.labelId, LabelTier = label.tier.ToString()
		};
		row.Units += units; row.Gross += gross; row.SoldCogs += soldCogs; row.ReturnWriteOff += writeOff;
	}
	public static IReadOnlyList<RegionWeekRow> RegionRows => regionRows;
	public static IReadOnlyList<TitleWeekRow> TitleRows => titleRows;
	private static void ResetTelemetry() { regionRows.Clear(); titleRows.Clear(); financeRows.Clear(); }
}
