using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One prospective LP purchasing purse for stores and autonomous racks.
/// Routing an existing store buyer spends no second purchase. Physical stock,
/// quality-based intentions and the common rack shelf still constrain sales.</summary>
public static class RetailPurchaseEnvelope {
    public const string Switch = "--retail-purchase-envelope=";
    public const int PolicyVersion = 1;
    public static bool Enabled { get; private set; }
    public static int? SavedVersion => Enabled ? PolicyVersion : null;
    private static bool configured;
    // A small correction to the existing live-single purse. Off-chart catalogue
    // sales and clubs retain their separately reported accounting scopes.
    internal const double SinglePurchaseScale = .95;
    internal const double AutonomousRackWeight = 1.6;
    // Calibrated outlet migration: rack access gains relative weight during the
    // guide's 1966-68 transition, then holds its last measured setting.
    internal static double RackWeight(int year) => 1.3 + .15 * Math.Clamp(year - 1966, 0, 2);

    public static void Configure(IEnumerable<string> arguments) {
        if (configured) return;
        var values = arguments.Where(a => a.StartsWith(Switch, StringComparison.Ordinal)).ToArray();
        if (values.Length > 1 || values.Length == 1 && values[0] != Switch + "0" && values[0] != Switch + "1")
            throw new ArgumentException(Switch + " requires 0 or 1.");
        Enabled = values.Length == 1 && values[0] == Switch + "1";
        if (Enabled && (!arguments.Contains(RackJobberChannel.Switch) || !arguments.Contains(BudgetLpChannel.Switch)))
            throw new ArgumentException(Switch + "1 requires joint store, rack and budget LP channels.");
        configured = true;
    }

    internal static int SingleBudget(int heldBudget) => Enabled
        ? (int)Math.Floor(Math.Max(0, heldBudget) * SinglePurchaseScale) : heldBudget;

    // The internally consistent 1966-68 retail-only LP:single ratios from the
    // 1969 guide. Earlier years hold the first ratio rather than splicing its
    // revised estimates into the older reference series. 1969's 55% is the old
    // authored endpoint, NOT a historical 1969 observation.
    internal static double AlbumToSingleRatio(int year) => year <= 1966 ? 133d / 129
        : year == 1967 ? 143d / 130 : year == 1968 ? 147d / 126 : 55d / 45;

    internal static int AlbumBudget(int basePurchaseCapacity, int year) =>
        (int)Math.Floor(SingleBudget(MajorOffChartSingles.HeldSingleBudget(basePurchaseCapacity)) * AlbumToSingleRatio(year));

    internal static (int Store, int Autonomous) Split(int storeIntent, double autonomousIntent, int purse,
        double rackWeight = AutonomousRackWeight) {
        int store = Math.Max(0, storeIntent), available = Math.Max(0, purse);
        double autonomous = Math.Max(0, autonomousIntent);
        if (!double.IsFinite(autonomous) || !double.IsFinite(rackWeight) || rackWeight <= 0)
            throw new ArgumentException("Invalid retail purchase intentions.");
        if (store + autonomous <= available) return (store, (int)Math.Floor(autonomous));
        if (store == 0) return (0, (int)Math.Min(available, Math.Floor(autonomous)));
        if (autonomous == 0) return (Math.Min(store, available), 0);
        int assignedStore = (int)Math.Floor((double)available * store / (store + autonomous * rackWeight));
        int assignedAutonomous = (int)Math.Min(available - assignedStore, Math.Floor(autonomous));
        assignedStore = Math.Min(store, available - assignedAutonomous);
        return (assignedStore, assignedAutonomous);
    }

    // Demands are sorted by stable product ID before this method is called.
    // Migration and autonomous cohorts are rounded separately, so rounding can
    // never invent autonomous shoppers from a pure store transfer.
    internal static int[] Allocate(IReadOnlyList<double> demands, int capacity) {
        var units = new int[demands.Count];
        double sum = demands.Sum(d => Math.Max(0, d));
        if (!double.IsFinite(sum)) throw new ArgumentException("Nonfinite purchase intention.");
        int total = (int)Math.Min(Math.Max(0, capacity), Math.Floor(sum));
        if (total <= 0) return units;
        var fractions = new double[demands.Count];
        int given = 0;
        for (int i = 0; i < demands.Count; i++) {
            double exact = Math.Max(0, demands[i]) / sum * total;
            units[i] = (int)Math.Floor(exact); fractions[i] = exact - units[i]; given += units[i];
        }
        foreach (int index in Enumerable.Range(0, units.Length).OrderByDescending(i => fractions[i]).ThenBy(i => i).Take(total - given))
            units[index]++;
        return units;
    }

    public sealed class RegionRow {
        public int Week, Year;
        public string RegionId;
        public int Budget, StoreBefore, StoreAllocated, AutonomousAllowance, FinalStores, RackUnits, Switchers, AutonomousUnits, Unspent;
    }
    private static readonly List<RegionRow> rows = new();
    public static IReadOnlyList<RegionRow> Rows => rows;
    internal static void BeginWeek() => rows.Clear();
    internal static void Record(RegionRow row) {
        row.AutonomousUnits = row.RackUnits - row.Switchers;
        row.Unspent = row.Budget - row.FinalStores - row.RackUnits;
        if (row.FinalStores < 0 || row.Switchers < 0 || row.AutonomousUnits < 0 || row.Unspent < 0 ||
            row.StoreAllocated != row.FinalStores + row.Switchers || row.AutonomousUnits > row.AutonomousAllowance)
            throw new InvalidOperationException("Shared retail LP purchasing does not conserve buyers.");
        rows.Add(row);
    }
    public static void RequireSaved(int? version) {
        if (version != SavedVersion)
            throw new InvalidOperationException("Resume with the saved retail purchase policy (" + Switch + (version == null ? "0" : version) + ").");
    }
    internal static void SetForChecks(bool enabled) { configured = true; Enabled = enabled; rows.Clear(); }
    internal static void ResetForChecks() { configured = false; Enabled = false; rows.Clear(); }
}
