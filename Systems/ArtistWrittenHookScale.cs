using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calibration probe (SimTools/ReleaseJudgementDirective.md, 2026-10-19 1964 genre diagnosis), off unless named.
/// --artist-written-hook-scale puts an artist-written composition's hook on the same scale as catalogue material.
///
/// The legacy hook is <c>0.75 * (0.8 * craft + 0.1 * era + noise) + 0.15 * performance</c>. A median writer
/// (craft ~0.54) lands near 0.45 and a craft-1.0 writer only reaches ~0.80, while professional songs are minted at
/// N(0.68, 0.15). Writer craft is genre-neutral, but rock draws ~65% of its singles from its own writers, so every
/// self-writing genre shipped hooks ~0.2 below the staff-led ones. Under the switch the hook reads the writer alone:
/// <c>0.85 * (craft + noise) + 0.15 * performance (+ team)</c>. A median self-writer sits below the staff median, and a
/// top-decile writer meets or beats it. The genre-weighted era term stays in craft score, lyric, identity fit and the
/// source-mix preference, never in the hook. Pure arithmetic, no draws, so the RNG stream is unchanged.
/// </summary>
public static class ArtistWrittenHookScale {
	public const string Switch = "--artist-written-hook-scale";
	public const float CraftWeight = 0.85f, PerformanceWeight = 0.15f;
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; Enabled = value; }

	/// <summary>The composition hook for an artist-original: the legacy blend when off, the writer's craft scale when on.</summary>
	public static float Hook(float legacyCompositionQuality, float writerCraft, float noise, float performanceHook, float teamHookLift) =>
		Godot.Mathf.Clamp(Enabled
			? CraftWeight * (writerCraft + noise) + PerformanceWeight * performanceHook + teamHookLift
			: legacyCompositionQuality * 0.75f + performanceHook * 0.15f + teamHookLift, 0f, 1f);
}
