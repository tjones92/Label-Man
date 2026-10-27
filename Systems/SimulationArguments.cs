using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>The adopted ordinary-game simulation profile. Headless audits keep their explicit flags.</summary>
public static class SimulationArguments {
    public const string DefaultSwitch = "--default-simulation-profile";
    public const string LegacySwitch = "--legacy-simulation-profile";
    public const string ProfileName = "lp-retail-v2";
    private static string[] effective;
    // The complete validated base matters: LP calibration depends on its release and audience systems.
    // Telemetry-only --calibration, --small-label-finance and profiling flags are deliberately excluded.
    private static readonly string[] Profile = {
        "--single-commercial-fit=2.5",
        "--single-commercial-fit-hit-share=0.15",
        "--single-commercial-fit-regional=0.5",
        "--single-commercial-fit-scale=0.88",
        "--single-commercial-fit-launch-weight=1.25",
        "--single-commercial-fit-floor=0.12",
        "--launch-stock-share=0.1",
        "--sell-through-reorders",
        "--ai-master-deals",
        "--ai-backers",
        "--ai-master-national-pickups",
        "--ai-master-capacity-decisions",
        "--major-offchart-singles=0.12",
        "--major-offchart-singles-hold-units",
        "--home-operated-micro-labels",
        "--home-label-running-cost=25",
        "--founder-label-dormancy",
        "--founder-surplus-compensation",
        "--distress-master-sales",
        "--prior-year-corporate-tax",
        "--hit-act-cadence",
        "--hit-act-retention",
        "--workload-scaled-staffing",
        "--defer-british-labels",
        "--midtier-national-ar",
        "--midtier-going-concern-cash",
        "--label-release-slate",
        "--label-release-capacity",
        "--studio-parent-labels",
        "--chart-exposure-plateau-rank=20",
        "--radio-position-bonus-scale=0",
        "--post-peak-demand-decay=0.8",
        "--post-peak-demand-grace=1",
        "--hit-act-hype=0.3",
        "--post-peak-demand-decay-top=0.9",
        "--slate-work-budget=0.67",
        "--slate-work-budget-midtier=1",
        "--slate-going-concern-reserve",
        "--slate-breakout-planning",
        "--artist-fanbase-recruit=0.35",
        "--artist-fanbase-half-life=104",
        "--artist-fanbase=0.25",
        "--hit-act-fit-floor=0.6",
        "--label-project-judgement",
        "--lp-programme-scale=2",
        "--lp-purchase-share=0.48",
        "--record-clubs",
        "--rack-jobbers",
        "--budget-lps",
        "--budget-price=1.75",
        "--rack-shopper-scale=0.02652",
        "--budget-shopper-scale=0.0816",
        "--rack-mixed-assortment",
        "--rack-buyer-access",
        "--rack-store-choice=1",
        "--rack-turnover=45",
        "--lp-demand-scale=0.98",
        "--retail-purchase-envelope=1",
    };

    public static string[] Get() => effective ??= Resolve(OS.GetCmdlineUserArgs(),
        !Engine.IsEditorHint() && DisplayServer.GetName() != "headless");

    internal static string[] Resolve(IEnumerable<string> arguments, bool ordinaryGame) {
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        bool force = args.Contains(DefaultSwitch), legacy = args.Contains(LegacySwitch);
        if (force && legacy) throw new ArgumentException("Choose the default or legacy simulation profile, not both.");
        if (legacy || !ordinaryGame && !force) return args;
        // An explicit parameter replaces its profile value. Preserve duplicate explicit parameters
        // so the owning parser can reject them rather than silently choosing one.
        static string Key(string arg) { int split = arg.IndexOf('='); return split < 0 ? arg : arg[..split]; }
        var explicitKeys = args.Select(Key).ToHashSet(StringComparer.Ordinal);
        return Profile.Where(flag => !explicitKeys.Contains(Key(flag))).Concat(args).ToArray();
    }
}
