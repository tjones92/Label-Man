using System;
using System.Linq;
using Godot;

public static class SimulationArgumentsChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool condition, string message) {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        var ordinary = SimulationArguments.Resolve(Array.Empty<string>(), true);
        static string Key(string arg) => arg.Split('=')[0];
        Check(ordinary.Length == 305 && ordinary.Distinct().Count() == 305 &&
            ordinary.Select(Key).Distinct().Count() == 305, "Complete ordinary-game profile, one value per key");
        Check(ordinary.Contains("--causal-media-atmosphere") && ordinary.Contains("--professional-writing-market") &&
            ordinary.Contains("--lp-assembly-gate-uncredited") && ordinary.Contains("--media-demand-press") &&
            ordinary.Contains("--response-auditions=response") && !ordinary.Contains("--dj-word-of-mouth") &&
            !ordinary.Contains("--fit-for-aware-buyers") && !ordinary.Contains("--media-demand-controversy"),
            "media-atmosphere-v3 branch set");
        Check(ordinary.Contains("--local-promotion=3") && ordinary.Contains("--local-promotion-midtier=off") &&
            ordinary.Contains("--singles-ration-exponent=1.0") && ordinary.Contains("--chart-faller-report-depth=60") &&
            ordinary.Contains("--act-lp-audience-net-credit") && ordinary.Contains("--census-population") &&
            ordinary.Contains("--opening-star-tier-share=0.37") && !ordinary.Any(a => a.StartsWith("--stature-calibration")) &&
            !ordinary.Contains("--calibration") && !ordinary.Contains("--small-label-finance"),
            "stature-fanbase-v1 candidate set, no telemetry");
        var previous = SimulationArguments.Resolve(new[] { SimulationArguments.PreviousSwitch }, false)
            .Except(new[] { SimulationArguments.PreviousSwitch }).ToArray();
        Check(previous.Length == 58 && !previous.Contains("--causal-media-atmosphere") &&
            previous.Contains("--lp-demand-scale=0.98") && previous.Contains("--slate-work-budget=0.67"),
            "Previous switch reproduces lp-retail-v2");
        var media = SimulationArguments.Resolve(new[] { SimulationArguments.MediaAtmosphereSwitch }, false)
            .Except(new[] { SimulationArguments.MediaAtmosphereSwitch }).ToArray();
        Check(media.Length == 216 && media.Take(58).SequenceEqual(previous) && media.Contains("--causal-media-atmosphere") &&
            !media.Contains("--local-promotion=3"), "media-atmosphere-v3 switch reproduces the old default");
        Check(ordinary.Contains("--lp-demand-scale=2.65") && ordinary.Contains("--rack-shopper-scale=0.030") &&
            ordinary.Contains("--budget-shopper-scale=0.13") && !ordinary.Contains("--lp-demand-scale=0.98"),
            "Adopted candidate LP calibration");
        Check(SimulationArguments.Resolve(Array.Empty<string>(), false).Length == 0, "Historical headless audits stay explicit");
        var forced = SimulationArguments.Resolve(new[] { SimulationArguments.DefaultSwitch }, false);
        Check(forced.Except(new[] { SimulationArguments.DefaultSwitch }).SequenceEqual(ordinary), "Headless default reproduces ordinary game");
        var explicitArgs = new[] { "--seed=1001", "--lp-demand-scale=0.96" };
        var overridden = SimulationArguments.Resolve(explicitArgs, true);
        Check(overridden.Count(a => a.StartsWith("--lp-demand-scale=")) == 1 && overridden.Contains(explicitArgs[1]) &&
            overridden.Contains(explicitArgs[0]), "Explicit parameters override profile values");
        var negated = SimulationArguments.Resolve(new[] { "--no-regional-radio-build-clock" }, true);
        Check(ordinary.Contains("--regional-radio-build-clock") && !negated.Contains("--regional-radio-build-clock") &&
            negated.Contains("--no-regional-radio-build-clock") && negated.Length == ordinary.Length,
            "An explicit --no-X switches off the profile's --X");
        Check(SimulationArguments.Resolve(new[] { SimulationArguments.LegacySwitch }.Concat(explicitArgs), true)
            .SequenceEqual(new[] { SimulationArguments.LegacySwitch }.Concat(explicitArgs)), "Legacy comparison bypasses profile");
        bool rejected = false;
        try { SimulationArguments.Resolve(new[] { SimulationArguments.DefaultSwitch, SimulationArguments.LegacySwitch }, true); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Conflicting profiles rejected");
        rejected = false;
        try { SimulationArguments.Resolve(new[] { SimulationArguments.DefaultSwitch, SimulationArguments.PreviousSwitch }, true); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Default and previous profiles rejected together");
        rejected = false;
        try { SimulationArguments.Resolve(new[] { SimulationArguments.DefaultSwitch, SimulationArguments.MediaAtmosphereSwitch }, true); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "Default and media-atmosphere-v3 profiles rejected together");
        if (OS.GetCmdlineUserArgs().Contains(SimulationArguments.DefaultSwitch)) {
            Check(Math.Abs(LpDemandCalibration.Scale - 2.65) < 1e-9, "Default demand configured before world startup");
            Check(RackJobberChannel.Enabled && Math.Abs(RackJobberChannel.ShopperScale - .030) < 1e-8 &&
                RackJobberChannel.Turnover == 45, "Default rack capacity and shoppers active");
            Check(BudgetLpChannel.Enabled && Math.Abs(BudgetLpChannel.ShopperScale - .13) < 1e-8 && RecordClubChannel.Enabled,
                "Default budget and club channels active");
            Check(LpChannelExperiment.MixedAssortment && LpChannelExperiment.BuyerAccess && LpChannelExperiment.ChoiceShare == 1,
                "Default channel routing active");
            Check(LabelReleaseProgramme.ProjectJudgementEnabled && LabelReleaseProgramme.Enabled &&
                Math.Abs(LpCapacityScreen.PurchaseShare - .48) < 1e-7 && LpCapacityScreen.ProgrammeScale == 2,
                "Complete release and LP capacity baseline active");
            Check(RetailPurchaseEnvelope.Enabled && SaveGameService.WritingVersion >= 36 &&
                Math.Abs((WorldStateService.Capture().LpDemandScale ?? 0) - 2.65) < 1e-9 && WorldStateService.Capture().RetailPurchasePolicy == 1,
                "Default purchase policy captured in saves");
            Check(MediaAtmosphere.Causal && ResponseAuditionProbe.Enabled && !ResponseAuditionProbe.DjTips && ResponseAuditionProbe.Writer == null,
                "Branch systems active; audition log needs a run name");
        }
        GD.Print($"SIMULATION_ARGUMENTS_CHECKS_OK {checks} profile={SimulationArguments.ProfileName} saveVersion={SaveGameService.WritingVersion}");
    }
}
