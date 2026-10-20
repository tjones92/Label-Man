using System;
using Godot;

/// <summary>
/// Opt-in causal probe: --album-session-production gives new album tracks the orphan-single studio formula.
/// Album-derived singles currently inherit production from album track appeal (talent, format and filler spread),
/// whereas an orphan single uses label craft, the act's studio performance and the recording region. This changes
/// the studio component only: material selection, composition hook and the existing realization remain shared.
/// No genre/year term, added GD draws, re-recording on promotion, or change to already recorded tracks.
/// </summary>
public static class AlbumSessionProduction {
	public const string Switch = "--album-session-production";
	public static readonly bool Enabled = Array.IndexOf(OS.GetCmdlineUserArgs(), Switch) >= 0;

	public static float ForTrack(float legacyProduction, AILabel label, SimulatedArtist artist, string albumId, int slot) {
		if (!Enabled) return legacyProduction;
		float studio = 1f;
		if (label.strongRegions != null && label.strongRegions.Length > 0) {
			var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
			if (region != null) studio = ChartSimulator.GetStudioQualityModifier(region);
		}
		// Same [-.05, .10] session variance as an orphan single, keyed independently of the global RNG.
		uint hash = 2166136261u;
		foreach (char c in $"{albumId}|{slot}|album-session-production") { hash ^= c; hash *= 16777619u; }
		float noise = -.05f + .15f * ((hash >> 8) / 16777216f);
		return Mathf.Clamp(label.productionQuality * .4f + artist.studioPerformance * .3f + studio * .2f + noise, 0, 1);
	}
}
