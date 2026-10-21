using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class RegionalSalesRandomChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool condition, string name) {
            if (!condition) throw new InvalidOperationException("REGIONAL_SALES_RNG_CHECK_FAILED: " + name);
            checks++;
        }
        var record = ChartManager.Instance.GetAllRecords().First();
        var region = ChartManager.Instance.GetAllRegions().First();
        int oldWeek = ChartManager.Instance.GetCurrentChartWeek();
        ChartManager.Instance.RestoreChartWeek(123);
        // The schema versions below are this switch's own; later default-on memory state writes newer ones.
        bool banded = CareerBandedFormatMemory.Enabled, promo = PromoSynergyFeedback.Enabled;
        CareerBandedFormatMemory.SetForChecks(false); PromoSynergyFeedback.SetForChecks(false);
        RegionalSalesRandom.Configure(Array.Empty<string>());
        GameRandom.Seed(9988); float expected = (float)GameRandom.RandRange(.96, 1.04); float next = GameRandom.Randf();
        GameRandom.Seed(9988);
        Check(RegionalSalesRandom.Multiplier(record, region, .96, 1.04) == expected && GameRandom.Randf() == next, "legacy draw and remaining stream exact");
        var legacy = new WorldSaveData();
        string legacyJson = JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions);
        Check(!legacyJson.Contains("RegionalSalesRng"), "legacy JSON omits new metadata");
        Check(SaveGameService.WritingVersion == 21, "legacy file schema unchanged");
        RegionalSalesRandom.Configure(new[] { "--regional-sales-rng=keyed-v1" });
        RegionalSalesRandom.Restore(0, 0, 1001);
        GameRandom.Seed(7788); next = GameRandom.Randf(); GameRandom.Seed(7788);
        float keyed = RegionalSalesRandom.Multiplier(record, region, .96, 1.04);
        Check(GameRandom.Randf() == next, "keyed sales do not consume global stream");
        Check(keyed >= .96f && keyed <= 1.04f, "single multiplier bounds");
        Check(SaveGameService.WritingVersion == 22, "keyed file rejects older executables");
        var saved = WorldStateService.Capture();
        Check(saved.RegionalSalesRngVersion == 1 && saved.RegionalSalesRngSeed == 1001, "world captures version and seed");
        // Restore without a command-line override, as in a later ordinary player load.
        RegionalSalesRandom.Configure(Array.Empty<string>());
        RegionalSalesRandom.Restore(saved.RegionalSalesRngVersion, saved.RegionalSalesRngSeed, 2002);
        Check(RegionalSalesRandom.Multiplier(record, region, .96, 1.04) == keyed, "saved seed overrides current launch seed");
        RegionalSalesRandom.Restore(0, 0, 2002);
        Check(RegionalSalesRandom.Version == 0, "old saves select legacy randomness");
        bool rejected = false;
        try { RegionalSalesRandom.Validate(2); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "unknown saved algorithm rejected");
        rejected = false;
        try { RegionalSalesRandom.Configure(new[] { "--regional-sales-rng=keyed-v1", "--regional-sales-rng=legacy" }); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "conflicting flags rejected");
        RegionalSalesRandom.Configure(Array.Empty<string>());
        CareerBandedFormatMemory.SetForChecks(banded); PromoSynergyFeedback.SetForChecks(promo);
        ChartManager.Instance.RestoreChartWeek(oldWeek);
        GD.Print($"REGIONAL_SALES_RNG_CHECK_PASS checks={checks}");
    }
}
