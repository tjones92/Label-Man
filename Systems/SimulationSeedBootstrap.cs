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
		var args = OS.GetCmdlineUserArgs();
		try { LocalScenes.Configure(args); ContactNetworkService.Configure(args); }
		catch (Exception ex) {
			GD.PrintErr("LOCAL_SCENE_FLAGS_REJECTED: " + ex.Message);
			GetTree().Quit(2);
			return;
		}
		try { GospelSongbookExpansion.ValidateRequest(args); }
		catch (Exception ex) {
			GD.PrintErr("GOSPEL_SONGBOOK_V2_REJECTED: " + ex.Message);
			GetTree().Quit(2);
			return;
		}
		try { CompositionShapeVariation.ActiveVersion = CompositionShapeVariation.ResolveVersion(args, CompositionShapeVariation.LatestVersion); }
		catch (Exception ex) {
			GD.PrintErr("COMPOSITION_SHAPE_FLAGS_REJECTED: " + ex.Message);
			GetTree().Quit(2);
			return;
		}
		PolarSongTable.AuditLegacyRepair = Array.Exists(args, a => a == "--polar-repair-legacy-world");
		if (PolarSongTable.AuditLegacyRepair && !Array.Exists(args, a => a is "--seed=1001" or "--seed=1002"))
			throw new InvalidOperationException("Legacy repair comparator requires a development seed.");
		foreach(string arg in OS.GetCmdlineUserArgs())if(arg.StartsWith("--repertoire-audit-phase=")) {
			LiveRepertoire.AuditPhase=int.Parse(arg["--repertoire-audit-phase=".Length..]);
			if(LiveRepertoire.AuditPhase<0||LiveRepertoire.AuditPhase>5)throw new ArgumentOutOfRangeException("repertoire audit phase");
		}
		PolarSongBehavior.UsePolarFitSelection = !Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--disable-polar-fit-selection");
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
