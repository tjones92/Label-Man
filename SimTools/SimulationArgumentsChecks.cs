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
        Check(ordinary.Length == 216 && ordinary.Distinct().Count() == 216, "Complete ordinary-game profile");
        Check(ordinary.Contains("--causal-media-atmosphere") && ordinary.Contains("--professional-writing-market") &&
            ordinary.Contains("--lp-assembly-gate-uncredited") && ordinary.Contains("--media-demand-press") &&
            ordinary.Contains("--response-auditions=response") && !ordinary.Contains("--media-demand-controversy"),
            "media-atmosphere-v3 branch set");
        var previous = SimulationArguments.Resolve(new[] { SimulationArguments.PreviousSwitch }, false)
            .Except(new[] { SimulationArguments.PreviousSwitch }).ToArray();
        Check(previous.Length == 58 && previous.SequenceEqual(ordinary.Take(58)) && !previous.Contains("--causal-media-atmosphere"),
            "Previous switch reproduces lp-retail-v2");
        Check(ordinary.Contains("--lp-demand-scale=0.98") && ordinary.Contains("--rack-shopper-scale=0.02652") &&
            ordinary.Contains("--budget-shopper-scale=0.0816"), "Adopted LP calibration");
        Check(SimulationArguments.Resolve(Array.Empty<string>(), false).Length == 0, "Historical headless audits stay explicit");
        var forced = SimulationArguments.Resolve(new[] { SimulationArguments.DefaultSwitch }, false);
        Check(forced.Except(new[] { SimulationArguments.DefaultSwitch }).SequenceEqual(ordinary), "Headless default reproduces ordinary game");
        var explicitArgs = new[] { "--seed=1001", "--lp-demand-scale=0.96" };
        var overridden = SimulationArguments.Resolve(explicitArgs, true);
        Check(overridden.Count(a => a.StartsWith("--lp-demand-scale=")) == 1 && overridden.Contains(explicitArgs[1]) &&
            overridden.Contains(explicitArgs[0]), "Explicit parameters override profile values");
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
        if (OS.GetCmdlineUserArgs().Contains(SimulationArguments.DefaultSwitch)) {
            Check(LpDemandCalibration.Scale == .98, "Default demand configured before world startup");
            Check(RackJobberChannel.Enabled && Math.Abs(RackJobberChannel.ShopperScale - .02652) < 1e-8 &&
                RackJobberChannel.Turnover == 45, "Default rack capacity and shoppers active");
            Check(BudgetLpChannel.Enabled && BudgetLpChannel.ShopperScale == .0816 && RecordClubChannel.Enabled,
                "Default budget and club channels active");
            Check(LpChannelExperiment.MixedAssortment && LpChannelExperiment.BuyerAccess && LpChannelExperiment.ChoiceShare == 1,
                "Default channel routing active");
            Check(LabelReleaseProgramme.ProjectJudgementEnabled && LabelReleaseProgramme.Enabled &&
                Math.Abs(LpCapacityScreen.PurchaseShare - .48) < 1e-7 && LpCapacityScreen.ProgrammeScale == 2,
                "Complete release and LP capacity baseline active");
            Check(RetailPurchaseEnvelope.Enabled && SaveGameService.WritingVersion >= 36 &&
                WorldStateService.Capture().LpDemandScale == .98 && WorldStateService.Capture().RetailPurchasePolicy == 1,
                "Default purchase policy captured in saves");
            Check(MediaAtmosphere.Causal && ResponseAuditionProbe.Enabled && ResponseAuditionProbe.Writer == null,
                "Branch systems active; audition log needs a run name");
        }
        GD.Print($"SIMULATION_ARGUMENTS_CHECKS_OK {checks} profile={SimulationArguments.ProfileName} saveVersion={SaveGameService.WritingVersion}");
    }
}
