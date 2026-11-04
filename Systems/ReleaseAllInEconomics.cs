using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Audit A4: campaign costs in forecasts/outcomes and a pre-recording positive-profit gate.</summary>
public static class ReleaseAllInEconomics {
	public const string Switch = "--release-all-in-economics";
	private static bool configured, enabled;
	private static bool? liveForChecks;
	public static bool Enabled => enabled;
	public static bool IsLive => liveForChecks ?? (enabled && GenreMarketV2.Enabled &&
		(ChartManager.Instance?.IsGenreMarketV2Live == true || CompetitorManager.Instance?.AlbumPracticeLiveTick == true));
	public static bool HasState => enabled ||
		ChartManager.Instance?.GetAllRecords().Any(r => r.sunkMarketingCost.HasValue) == true ||
		CompetitorManager.Instance?.GetAlbumProjects().Any(p => p.allInEconomics == true) == true ||
		CompetitorManager.Instance?.GetAllLabels().Any(l => l.laneRevenueMemory?.Values.Any(m =>
			m.observations?.Any(o => o.allInEconomics == true) == true) == true) == true;
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && (!GenreMarketV2.Enabled || !LabelPressingEconomics.Enabled))
			throw new ArgumentException(Switch + " requires the live genre market and dealer-price economics.");
		configured = true;
	}
	internal static void SetForChecks(bool value, bool? live = null) { configured = true; enabled = value; liveForChecks = live; }
	internal static float ExpectedCampaign(float baseBudget, float estimatedQuality) =>
		Math.Max(0f, baseBudget) * (.6f + .8f * Math.Clamp(estimatedQuality, 0f, 1f));
	internal static bool ShouldCommit(float expectedProfit) => float.IsFinite(expectedProfit) && expectedProfit > 0f;

	/// <summary>
	/// Opt-in, <c>--lp-assembly-gate-uncredited</c>. The profit gate removes the album portfolio credit from the LP
	/// projection, because DecideRelease added it. With <c>--lp-assembly</c> the slate replaces that projection with the
	/// raw assembly prior, which never held the credit, so the gate removed it a second time. The credit scales with the
	/// album-era weight: about $1 in 1960-61 (immaterial, LpProfitGateFunnelInvestigation.md), about $28k from 1966, when it
	/// refused ~90% of LP preparations every week and halved late-decade LP volume (SimTools/BranchEndDecadeReview.md).
	/// With the switch the gate tests the assembly prior net of the expected campaign (which the assembly prior also
	/// lacked), and removes no credit. Inert unless named.
	/// </summary>
	public const string AssemblyGateUncreditedSwitch = "--lp-assembly-gate-uncredited";
	public static readonly bool AssemblyGateUncredited = SimulationArguments.Get().Contains(AssemblyGateUncreditedSwitch);
}
