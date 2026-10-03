using System;
using System.Globalization;
using Godot;

/// <summary>
/// Applies an audit seed before any population-generating autoload enters the tree.
/// Normal game startup is unchanged when no --seed argument is present.
/// </summary>
public partial class SimulationSeedBootstrap : Node {
	public static ulong? RequestedSeed { get; private set; }

	public override void _EnterTree() {
		PolarSongBehavior.UsePolarFitSelection = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--use-polar-fit-selection");
		PolarSongBehavior.AuditLegacyRealization = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-legacy-realization");
		PolarSongBehavior.AuditLegacyPromotion = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-legacy-promotion");
		PolarSongBehavior.AuditNoHookCeiling = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-no-hook-ceiling");
		PolarSongBehavior.AuditNoCapabilityPenalty = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-no-capability-penalty");
		PolarSongBehavior.AuditNoCriticAdjustment = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-no-critic-adjustment");
		PolarSongBehavior.AuditNoMomentConversion = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-no-moment-conversion");
		PolarSongBehavior.AuditOriginalRealization = Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--polar-fit-audit-original-realization");
		foreach (string argument in OS.GetCmdlineUserArgs()) {
			if (!argument.StartsWith("--seed=", StringComparison.Ordinal)) continue;

			RequestedSeed = ulong.Parse(argument[7..], CultureInfo.InvariantCulture);
			GD.Seed(RequestedSeed.Value);
			GD.Print($"SIMULATION_SEED_APPLIED seed={RequestedSeed.Value} phase=pre_autoload_population");
			break;
		}
	}
}
