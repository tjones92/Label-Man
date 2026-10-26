using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Opt-in own-master budget products. Parameters are scenarios, not a fit to a net market gap.
/// A product is separate from its source LP; synthetic opening owner/genre baskets have no title count.
/// Store switchers are removed only when a budget copy actually clears the common rack shelf.</summary>
public static class BudgetLpChannel {
	public const string Switch = "--budget-lps";
	public static bool Enabled { get; private set; }
	public static double ShopperScale { get; private set; } = .08;
	public static double Substitution { get; private set; } = .25;
	public static double Price { get; private set; } = 1.75;
	internal const int HoldbackWeeks = 52, OfferSlots = 25;
	internal const double WholesaleShare = .55, Packaging = .11, RoyaltyRate = .05;
	private static bool configured;
	public static BudgetLpState State { get; private set; }
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = arguments.ToArray(); Enabled = args.Contains(Switch);
		if (Enabled && (!RackJobberChannel.Enabled || !GenreMarketV2.Enabled))
			throw new ArgumentException(Switch + " requires --rack-jobbers and --enable-genre-market-v2.");
		double Read(string name, double fallback, double min, double max) {
			string value = args.FirstOrDefault(a => a.StartsWith(name, StringComparison.Ordinal));
			if (value == null) return fallback;
			if (!Enabled || !double.TryParse(value[name.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ||
				!double.IsFinite(result) || result < min || result > max) throw new ArgumentException("Invalid " + value);
			return result;
		}
		ShopperScale = Read("--budget-shopper-scale=", .08, 0, 1);
		Substitution = Read("--budget-substitution=", .25, 0, 1);
		Price = Read("--budget-price=", 1.75, .01, 3);
		configured = true;
	}
	public static BudgetLpState Capture() => Enabled ? State?.Copy() : null;
	public static void Restore(BudgetLpState state) {
		if (state != null && !Enabled) throw new InvalidOperationException("Resume budget products with " + Switch);
		if (state != null && (state.Scale != ShopperScale || state.Substitution != Substitution || state.Price != Price))
			throw new InvalidOperationException("Resume with the saved budget shopper scale, substitution and price.");
		if (state != null && state.RackTurnover != RackJobberChannel.Turnover)
			throw new InvalidOperationException("Resume with the saved rack turnover.");
		LpChannelExperiment.RequireSettings(state);
		State = state?.Copy(); weekRows.Clear(); LpChannelExperiment.BeginWeek();
	}
	internal static void ResetForChecks(bool enabled = false, double scale = .08, double substitution = .25) {
		configured = enabled; Enabled = enabled; ShopperScale = scale; Substitution = substitution; Price = 1.75;
		State = null; weekRows.Clear(); regions.Clear(); products.Clear(); finance.Clear();
	}
	private static BudgetLpState Ensure() => State ??= new BudgetLpState { Scale = ShopperScale, Substitution = Substitution, Price = Price,
		RackTurnover = RackJobberChannel.Turnover, MixedAssortment = LpChannelExperiment.MixedAssortment, RackChoice = LpChannelExperiment.ChoiceShare };
	internal static void Remember(RecordRuntimeData record, int week) {
		if (!Enabled || record?.baseRecord == null || record.baseRecord.isPlayerOwned || record.baseRecord.format != ReleaseFormat.Album ||
			record.totalUnitsSold <= 0 || string.IsNullOrEmpty(record.baseRecord.labelId)) return;
		var state = Ensure(); string id = "master:" + record.baseRecord.recordId;
		if (state.Products.ContainsKey(id)) return;
		state.Products.Add(id, new BudgetLpProduct {
			Id = id, OwnerId = record.baseRecord.labelId, SourceId = record.baseRecord.recordId, ArtistId = record.baseRecord.artistId,
			Provenance = "OwnMaster", Genre = record.baseRecord.primaryGenre, Quality = Math.Clamp(record.GetQuality(), 0, 1),
			Appeal = record.totalUnitsSold, ReleaseWeek = week + Math.Max(0, HoldbackWeeks - record.weeksSinceRelease)
		});
	}
	public static void OnAcquired(string oldOwner, string newOwner) {
		if (!Enabled || State == null || oldOwner == newOwner) return;
		foreach (var p in State.Products.Values.Where(p => p.OwnerId == oldOwner)) p.OwnerId = newOwner;
	}
	// Invoked once before clearing. No club ledger or licence is reused. Opening baskets explicitly assume
	// older own repertoire with the opening live catalogue's genre/quality mix; their title counts are unknown.
	public static void Prepare(IReadOnlyList<RecordRuntimeData> records, Func<string, AILabel> labelOf, int week) {
		if (!Enabled) return;
		var state = Ensure();
		bool Active(RecordRuntimeData r) => r.baseRecord.format == ReleaseFormat.Album && !r.baseRecord.isPlayerOwned &&
			labelOf(r.baseRecord.labelId)?.IsActive == true;
		if (!state.Initialized) {
			foreach (var group in records.Where(Active).GroupBy(r => (r.baseRecord.labelId, r.baseRecord.primaryGenre))) {
				string id = "opening:" + group.Key.labelId + ":" + (int)group.Key.primaryGenre;
				state.Products[id] = new BudgetLpProduct { Id = id, OwnerId = group.Key.labelId, Genre = group.Key.primaryGenre,
					Provenance = "OpeningSyntheticBasket", SourceId = "", ArtistId = "", ReleaseWeek = week,
					Quality = group.Average(r => Math.Clamp(r.GetQuality(), 0, 1)), Appeal = group.Sum(r => Math.Max(1d, r.totalUnitsSold)) };
			}
			state.Initialized = true;
		}
		foreach (var record in records.Where(r => Active(r) && r.weeksSinceRelease >= HoldbackWeeks)) Remember(record, week);
	}
	internal sealed class Offer {
		public BudgetLpProduct Product;
		public double Demand, SwitchDemand;
		public int Units, Switched, StoreLimit;
	}
	internal static bool HasMargin(double manufacturing) => Price * WholesaleShare * (1 - RoyaltyRate) - manufacturing
		- LabelPressingEconomics.ExpectedAlbumCompositionCost - RackJobberChannel.ReturnWriteOff((float)manufacturing) > 0;
	internal static List<Offer> Offers(MarketRegion region, int week, int year, int month, bool liveTick,
		Func<string, AILabel> labelOf, IReadOnlyDictionary<Genre, int> storeUnits) {
		weekRows.TryAdd(region.regionId, new List<Offer>());
		double manufacturing = CompetitorManager.Instance?.BudgetLpManufacturingCost ?? .51;
		if (!HasMargin(manufacturing)) return weekRows[region.regionId];
		var eligible = Ensure().Products.Values.Where(p => p.ReleaseWeek <= week && labelOf(p.OwnerId)?.IsActive == true &&
			labelOf(p.OwnerId)?.isPlayerOwned == false).OrderByDescending(p => p.Appeal * Math.Pow(.8, Math.Max(0, week - p.ReleaseWeek) / 52d))
			.ThenBy(p => p.Id, StringComparer.Ordinal).Take(OfferSlots).ToList();
		double pool = Math.Max(0, region.population) * 1e6 * Math.Sqrt(Math.Max(0, region.averageIncome)) * RackJobberChannel.Reach(year) * ShopperScale;
		var weighted = eligible.Select(p => (p, weight: Math.Sqrt(Math.Max(1, p.Appeal)) * Math.Pow(p.Quality, 2.5) *
			AlbumSimulator.CalculateAlbumExhaustion((float)(p.RegionUnits.GetValueOrDefault(region.regionId) / Math.Max(1, pool))))).ToList();
		double weights = weighted.Sum(p => p.weight);
		var offers = new List<Offer>();
		foreach (var genre in weighted.GroupBy(p => p.p.Genre)) {
			double genreWeight = genre.Sum(p => p.weight);
			// A bounded choice probability: cheap products with available repertoire can attract a fraction of
			// already-cleared same-genre store purchases. Empty/fully exhausted offerings attract none.
			double availability = genre.Any(p => p.weight > 0) ? Math.Min(1, genreWeight / Math.Max(1, genre.Sum(p => Math.Sqrt(Math.Max(1, p.p.Appeal))))) : 0;
			double switching = storeUnits.GetValueOrDefault(genre.Key) * Substitution * availability * Math.Max(0, 1 - Price / 3d);
			foreach (var (p, weight) in genre) {
				double bargain = weights > 0 ? pool * .095 * Math.Min(3, 3d / Price) *
					MarketSeasonality.GetAlbumSalesMultiplier(year, month, liveTick) * availability * weight / weights : 0;
				double switched = genreWeight > 0 ? switching * weight / genreWeight : 0;
				offers.Add(new Offer { Product = p, Demand = bargain + switched, SwitchDemand = switched, StoreLimit = storeUnits.GetValueOrDefault(genre.Key) });
			}
		}
		weekRows[region.regionId] = offers.Where(o => o.Demand > 0).ToList();
		return weekRows[region.regionId];
	}
	// Deterministic largest remainders across BOTH lines; each line never receives a duplicate shelf.
	internal static void AllocateShared(List<RackJobberChannel.Candidate> front, List<Offer> budget, int shelf) {
		var demands = front.Select(c => (id: "front:" + c.Record.baseRecord.recordId, demand: (double)c.Demand, put: (Action<int>)(n => c.Units = n)))
			.Concat(budget.Select(o => (id: "budget:" + o.Product.Id, demand: o.Demand, put: (Action<int>)(n => o.Units = n)))).OrderBy(p => p.id, StringComparer.Ordinal).ToList();
		foreach (var p in demands) p.put(0);
		double sum = demands.Sum(p => Math.Max(0, p.demand));
		int total = (int)Math.Min(Math.Max(0, shelf), Math.Floor(sum));
		if (total <= 0) return;
		var parts = demands.Select(p => (p, exact: Math.Max(0, p.demand) / sum * total)).ToList();
		int given = 0;
		foreach (var p in parts) { int n = (int)Math.Floor(p.exact); p.p.put(n); given += n; }
		var extra = parts.OrderByDescending(p => p.exact - Math.Floor(p.exact)).ThenBy(p => p.p.id, StringComparer.Ordinal).Take(total - given).Select(p => p.p.id).ToHashSet();
		foreach (var p in parts) if (extra.Contains(p.p.id)) p.p.put((int)Math.Floor(p.exact) + 1);
	}
	internal static void BoundSwitchers(List<Offer> offers, int available) {
		int total = offers.Sum(o => o.Switched);
		if (total <= Math.Max(0, available)) return;
		// Shared shelf rounding may round several products up at very small volumes. Bound the
		// resulting cohort as a group, even at full substitution with no new bargain shoppers.
		long cumulative = 0; int allocated = 0;
		foreach (var o in offers.OrderBy(o => o.Product.Id, StringComparer.Ordinal)) {
			cumulative += o.Switched;
			int to = (int)(cumulative * Math.Max(0, available) / total);
			int kept = to - allocated;
			o.Units -= o.Switched - kept; // An excess switcher is an unfulfilled copy, not an invented new buyer.
			o.Switched = kept; allocated = to;
		}
	}
	internal static void Complete(MarketRegion region, int week, int year, int shelf, int frontUnits) {
		var offers = weekRows[region.regionId];
		foreach (var o in offers)
			o.Switched = FulfilledSwitchers(o);
		foreach (var group in offers.GroupBy(o => o.Product.Genre)) BoundSwitchers(group.ToList(), group.First().StoreLimit);
		foreach (var o in offers) {
			o.Product.RegionUnits[region.regionId] = o.Product.RegionUnits.GetValueOrDefault(region.regionId) + o.Units;
			products.Add(new ProductRow { Week = week, Year = year, RegionId = region.regionId, ProductId = o.Product.Id, OwnerId = o.Product.OwnerId,
				Provenance = o.Product.Provenance, Genre = o.Product.Genre.ToString(), Demand = o.Demand, Units = o.Units, Switched = o.Switched });
		}
		regions.Add(new RegionRow { Week = week, Year = year, RegionId = region.regionId, Shelf = shelf, FrontUnits = frontUnits,
			BudgetUnits = offers.Sum(o => o.Units), Switched = offers.Sum(o => o.Switched), BudgetOffers = offers.Count, Demand = offers.Sum(o => o.Demand) });
	}
	internal static int FulfilledSwitchers(Offer offer) => offer.Demand <= 0 ? 0 : offer.Demand == offer.SwitchDemand
		? offer.Units // Exact pure-switch cohort: avoid floor(n * d / d) losing a buyer to floating-point cancellation.
		: Math.Min(offer.Units, (int)Math.Floor(offer.Units * offer.SwitchDemand / offer.Demand));
	public static int SwitchUnits(string region, Genre genre) => Enabled && weekRows.TryGetValue(region, out var offers)
		? offers.Where(o => o.Product.Genre == genre).Sum(o => o.Switched) : 0;
	// A named master's budget edition also exhausts that master's full-price buyer pool. Synthetic
	// baskets cannot be mapped to a particular master, so their overlap is the explicit weekly choice only.
	public static long SourcePurchases(string recordId, string regionId) => Enabled && State != null &&
		State.Products.TryGetValue("master:" + recordId, out var product) ? product.RegionUnits.GetValueOrDefault(regionId) : 0;
	public static IEnumerable<(BudgetLpProduct Product, int Units)> Sales(string owner) => weekRows.Values.SelectMany(o => o)
		.Where(o => o.Product.OwnerId == owner && o.Units > 0).Select(o => (o.Product, o.Units));
	public static void BeginWeek() { if (Enabled) weekRows.Clear(); }
	private static readonly Dictionary<string, List<Offer>> weekRows = new(StringComparer.Ordinal);
	public sealed class RegionRow { public int Week, Year, Shelf, FrontUnits, BudgetUnits, Switched, BudgetOffers; public string RegionId; public double Demand; }
	public sealed class ProductRow { public int Week, Year, Units, Switched; public string RegionId, ProductId, OwnerId, Provenance, Genre; public double Demand; }
	public sealed class FinanceRow { public int Week, Year; public string OwnerId; public long Units; public double Gross, Cogs, Royalty, Returns; }
	private static readonly List<RegionRow> regions = new();
	private static readonly List<ProductRow> products = new();
	private static readonly List<FinanceRow> finance = new();
	public static IReadOnlyList<RegionRow> Regions => regions;
	public static IReadOnlyList<ProductRow> Products => products;
	public static IReadOnlyList<FinanceRow> Finance => finance;
	public static void RecordFinance(FinanceRow row) => finance.Add(row);
}

public sealed class BudgetLpState {
	public bool Initialized { get; set; }
	public double Scale { get; set; } = .08;
	public double Substitution { get; set; } = .25;
	public double Price { get; set; } = 1.75;
	public float RackTurnover { get; set; } = RackJobberChannel.UnitsPerRackWeek;
	public Dictionary<string, BudgetLpProduct> Products { get; set; } = new(StringComparer.Ordinal);
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public bool MixedAssortment { get; set; }
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public double RackChoice { get; set; }
	public BudgetLpState Copy() => new() { Initialized = Initialized, Scale = Scale, Substitution = Substitution, Price = Price, RackTurnover = RackTurnover,
		MixedAssortment = MixedAssortment, RackChoice = RackChoice,
		Products = Products.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value.Copy(), StringComparer.Ordinal) };
}
public sealed class BudgetLpProduct {
	public string Id { get; set; }
	public string OwnerId { get; set; }
	public string SourceId { get; set; }
	public string ArtistId { get; set; }
	public string Provenance { get; set; }
	public Genre Genre { get; set; }
	public int ReleaseWeek { get; set; }
	public double Quality { get; set; }
	public double Appeal { get; set; }
	public Dictionary<string, long> RegionUnits { get; set; } = new(StringComparer.Ordinal);
	public BudgetLpProduct Copy() { var copy = (BudgetLpProduct)MemberwiseClone();
		copy.RegionUnits = RegionUnits.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal); return copy; }
}
