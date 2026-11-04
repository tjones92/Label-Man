using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>Two independent, opt-in LP channel treatments. Neither creates shoppers or shelf units.</summary>
public static class LpChannelExperiment {
	public const string AssortmentSwitch = "--rack-mixed-assortment";
	public const string ChoiceSwitch = "--rack-store-choice=";
	public const string AccessSwitch = "--rack-buyer-access";
	public static bool MixedAssortment { get; private set; }
	public static bool BuyerAccess { get; private set; }
	public static double ChoiceShare { get; private set; }
	public static bool Enabled => MixedAssortment || ChoiceShare > 0;
	private static bool configured;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = arguments.ToArray();
		MixedAssortment = args.Contains(AssortmentSwitch);
		string choice = args.FirstOrDefault(a => a.StartsWith(ChoiceSwitch, StringComparison.Ordinal));
		if (choice != null && (!double.TryParse(choice[ChoiceSwitch.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out double share) ||
			!double.IsFinite(share) || share < 0 || share > 1)) throw new ArgumentException("Invalid " + choice);
		ChoiceShare = choice == null ? 0 : double.Parse(choice[ChoiceSwitch.Length..], CultureInfo.InvariantCulture);
		BuyerAccess = args.Contains(AccessSwitch);
		if (BuyerAccess && ChoiceShare <= 0) throw new ArgumentException(AccessSwitch + " requires positive " + ChoiceSwitch);
		if ((MixedAssortment || choice != null) && (!BudgetLpChannel.Enabled || !RackJobberChannel.Enabled))
			throw new ArgumentException("LP channel treatments require --budget-lps and --rack-jobbers.");
		configured = true;
	}
	internal static void ResetForChecks(bool mixed = false, double share = 0, bool access = false) {
		configured = false; MixedAssortment = mixed; ChoiceShare = share; BuyerAccess = access; choices.Clear(); rows.Clear();
	}
	internal static int CatalogueSlots(int budgetOffers) => RackJobberChannel.CatalogueTitles - (MixedAssortment ? 0 : budgetOffers);
	public static void RequireSettings(BudgetLpState state) {
		if (state != null && (state.MixedAssortment != MixedAssortment || state.RackChoice != ChoiceShare || state.RackBuyerAccess != BuyerAccess))
			throw new InvalidOperationException("Resume with the saved mixed assortment, rack store-choice and buyer-access settings.");
	}
	internal sealed class Choice {
		public int Before, Planned, Fulfilled;
	}
	private static readonly Dictionary<(string region, string record), Choice> choices = new();
	internal static void BeginWeek() => choices.Clear();
	internal static int Plan(int stores, float reach, float display, double share, bool access = false) =>
		(int)Math.Floor(Math.Max(0, stores) * Math.Clamp(reach, 0, 1) * (access ? 1 : Math.Clamp(display, 0, 1)) * Math.Clamp(share, 0, 1));
	internal static void Plan(string region, RackJobberChannel.Candidate candidate, int year) {
		if (ChoiceShare <= 0 || candidate.Tier == RackJobberChannel.TierNone) return;
		candidate.StoreChoice = Plan(candidate.StoreUnits, RackJobberChannel.Reach(year),
			RackJobberChannel.Exposure(candidate.Tier, candidate.Rank, candidate.Record.awareness), ChoiceShare, BuyerAccess);
		choices[(region, candidate.Record.baseRecord.recordId)] = new Choice { Before = candidate.StoreUnits, Planned = candidate.StoreChoice };
	}
	// Front-line preferences reserve a disjoint cohort before the remaining store buyers can choose budget.
	// A failed rack purchase stays in stores; it is not offered a second budget purchase in the same week.
	internal static void ReserveBudgetBuyers(string region, List<BudgetLpChannel.Offer> offers, List<RackJobberChannel.Candidate> front) {
		if (ChoiceShare <= 0) return;
		var reserved = front.GroupBy(c => c.Record.baseRecord.primaryGenre).ToDictionary(g => g.Key, g => g.Sum(c => c.StoreChoice));
		foreach (var group in offers.GroupBy(o => o.Product.Genre)) {
			int before = group.First().StoreLimit, remaining = Math.Max(0, before - reserved.GetValueOrDefault(group.Key));
			foreach (var offer in group) {
				double newSwitch = before > 0 ? offer.SwitchDemand * remaining / before : 0;
				offer.Demand = Math.Max(0, offer.Demand - offer.SwitchDemand) + newSwitch;
				offer.SwitchDemand = newSwitch; offer.StoreLimit = remaining;
			}
		}
	}
	internal static int Fulfilled(int units, double totalDemand, int planned) => totalDemand <= 0 ? 0 :
		Math.Min(planned, totalDemand == planned ? units : Math.Min(units, (int)Math.Floor(units * (planned / totalDemand))));
	internal static void CompleteFront(string region, IReadOnlyList<RackJobberChannel.Candidate> candidates) {
		if (ChoiceShare <= 0) return;
		if (RetailPurchaseEnvelope.Enabled) {
			foreach (var c in candidates) if (choices.TryGetValue((region, c.Record.baseRecord.recordId), out var choice)) {
				if (c.StoreChoiceCleared < 0 || c.StoreChoiceCleared > choice.Planned || c.StoreChoiceCleared > c.Units)
					throw new InvalidOperationException("Shared LP migration exceeds its store cohort.");
				choice.Fulfilled = c.StoreChoiceCleared;
			}
			return;
		}
		var parts = candidates.Where(c => choices.ContainsKey((region, c.Record.baseRecord.recordId))).Select(c => {
			var choice = choices[(region, c.Record.baseRecord.recordId)];
			double demand = (double)c.AutonomousDemand + choice.Planned;
			double exact = demand <= 0 ? 0 : Math.Min(choice.Planned, demand == choice.Planned ? c.Units : c.Units * (choice.Planned / demand));
			return (candidate: c, choice, exact);
		}).ToList();
		int target = (int)Math.Floor(parts.Sum(p => p.exact)), given = 0;
		foreach (var part in parts) { part.choice.Fulfilled = (int)Math.Floor(part.exact); given += part.choice.Fulfilled; }
		foreach (var part in parts.OrderByDescending(p => p.exact - Math.Floor(p.exact))
			.ThenBy(p => p.candidate.Record.baseRecord.recordId, StringComparer.Ordinal).Take(target - given)) part.choice.Fulfilled++;
	}
	public static int FrontSwitches(string region, string record) => choices.TryGetValue((region, record), out var choice) ? choice.Fulfilled : 0;
	public static int BudgetEligible(string region, string record, int afterFrontSwitches) =>
		choices.TryGetValue((region, record), out var choice) ? Math.Max(0, afterFrontSwitches - (choice.Planned - choice.Fulfilled)) : afterFrontSwitches;
	public sealed class RegionRow {
		public int Week, Year;
		public string RegionId;
		public long StoreBefore, FrontPlanned, FrontSwitched, BudgetSwitched, StoreAfter;
	}
	private static readonly List<RegionRow> rows = new();
	public static IReadOnlyList<RegionRow> Rows => rows;
	public static void Record(int week, int year, string region, long before, long budgetSwitched, long after) {
		var cohort = choices.Where(p => p.Key.region == region).Select(p => p.Value).ToList();
		long switched = cohort.Sum(c => (long)c.Fulfilled), planned = cohort.Sum(c => (long)c.Planned);
		if (before != after + switched + budgetSwitched || switched > planned || cohort.Any(c => c.Fulfilled > c.Before || c.Planned > c.Before))
			throw new InvalidOperationException("LP channel buyer conservation failed.");
		rows.Add(new RegionRow { Week = week, Year = year, RegionId = region, StoreBefore = before,
			FrontPlanned = planned, FrontSwitched = switched, BudgetSwitched = budgetSwitched, StoreAfter = after });
	}
}
