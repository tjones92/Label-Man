using System;
using System.Linq;
using Godot;

/// <summary>Versioned regional-sales RNG pilot. Other stochastic systems keep their own existing streams.</summary>
public static class RegionalSalesRandom {
    public const int LatestVersion = 1;
    public static int Version { get; private set; }
    private static ulong? seed;
    private static int? requestedVersion;
    public static ulong SavedSeed => Version == 0 ? 0 : seed ??= SimulationSeedBootstrap.RequestedSeed ?? BandLife.WorldSeed;

    public static void Configure(string[] arguments) {
        var choices = arguments.Where(a => a.StartsWith("--regional-sales-rng=", StringComparison.Ordinal)).Distinct().ToArray();
        if (choices.Length > 1) throw new ArgumentException("Choose one regional-sales RNG version.");
        requestedVersion = choices.Length == 0 ? null : choices[0]["--regional-sales-rng=".Length..] switch {
            "legacy" => 0, "keyed-v1" => 1, _ => throw new ArgumentException("Regional-sales RNG must be legacy or keyed-v1.")
        };
        Version = requestedVersion ?? 0; seed = null;
    }
    public static void Validate(int version) {
        if (version is < 0 or > LatestVersion) throw new InvalidOperationException("Unsupported regional-sales RNG version in save.");
    }
    public static void Restore(int version, ulong savedSeed, ulong? worldSeed) {
        Validate(version);
        Version = requestedVersion ?? version;
        seed = Version == 0 ? null : version == Version ? savedSeed : worldSeed ?? BandLife.WorldSeed;
    }
    public static float Multiplier(RecordRuntimeData record, MarketRegion region, double minimum, double maximum) {
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        // Keep both the normal legacy path and the frozen startup/prewarm stream exact.
        if (Version == 0 || week <= 0) return (float)GameRandom.RandRange(minimum, maximum);
        if (string.IsNullOrEmpty(record.baseRecord.recordId) || string.IsNullOrEmpty(region.regionId))
            throw new InvalidOperationException("Keyed sales require persistent record and region IDs.");
        uint decision = 0x534C0000U | (record.baseRecord.format == ReleaseFormat.Album ? 1U : 0U);
        double unit = PhiloxCounterRandom.Unit(SavedSeed, record.baseRecord.recordId, region.regionId, week, decision);
        return (float)(minimum + (maximum - minimum) * unit);
    }
}
