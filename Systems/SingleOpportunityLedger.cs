using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Ex-ante, release-time Single opportunity ledger. It deliberately contains no
/// chart, sales, inventory, revenue, or post-release state. A year's releases
/// all read only completed prior cohorts, making the frozen normalizer invariant
/// to the collection order in which that year's records are evaluated.
/// </summary>
public static class SingleOpportunityLedger {
	private const float MinimumCompletedCohortMass = 250000f;
	private sealed class Cohort { public float Enabled; public float Accepted; }
	private static readonly Dictionary<(int Year, ProjectRecordRole Lane), Cohort> cohorts = new();
	private static readonly Dictionary<ProjectRecordRole, Cohort> anchors = new();

	public static void CaptureAtRelease(RecordRuntimeData runtime, AILabel label, IEnumerable<MarketRegion> regions, int year) {
		if (runtime?.baseRecord?.format != ReleaseFormat.Single || runtime.projectRole is ProjectRecordRole.None or ProjectRecordRole.ExternalOrLegacy) return;
		float enabled = 0f, accepted = 0f;
		float quality = runtime.GetQuality();
		float intrinsic = Mathf.Pow(Mathf.Clamp(quality, .01f, 1f), 4f);
		float launch = Mathf.Clamp(runtime.initialLaunchAwareness > 0f ? runtime.initialLaunchAwareness : runtime.awareness, .01f, 1f);
		foreach (MarketRegion region in regions?.Where(item => item != null) ?? Enumerable.Empty<MarketRegion>()) {
			float population = Mathf.Max(0f, region.population * 1000000f * region.GetBuyingPopulationPercentage());
			float routed = GenreAcceptanceService.GetRegionalDemandAcceptance(runtime.baseRecord.primaryGenre,
				runtime.baseRecord.secondaryGenre, region, year, 0f);
			float legacy = region.GetLegacyGenreAcceptance(runtime.baseRecord.primaryGenre, year, includeMomentum: false);
			float format = GenreAcceptanceService.GetFormatMultiplier(runtime.baseRecord.primaryGenre,
				runtime.baseRecord.secondaryGenre, ReleaseFormat.Single, year,
				region.GetEnabledAlbumOpportunityWeight(runtime.baseRecord.primaryGenre, year));
			float distribution = 1f - region.distribution.difficulty * .30f;
			enabled += population * launch * intrinsic * GenreAcceptanceService.GetEnabledSingleDemandMultiplier(routed) * format * distribution;
			accepted += population * launch * intrinsic * (.60f + legacy * .50f) * distribution;
		}
		(int completedYear, float normalizer, bool fallback, string source) = GetFrozenNormalizer(year, runtime.projectRole);
		runtime.enabledOpportunityMass = enabled;
		runtime.acceptedOpportunityMass = accepted;
		runtime.cohortOpportunityNormalizer = normalizer;
		runtime.cohortOpportunityColdStartFallback = fallback;
		runtime.cohortOpportunityNormalizerSource = source;
		(int Year, ProjectRecordRole Lane) key = (year, runtime.projectRole);
		if (!cohorts.TryGetValue(key, out Cohort cohort)) cohorts[key] = cohort = new Cohort();
		cohort.Enabled += enabled;
		cohort.Accepted += accepted;
	}

	private static (int CompletedYear, float Normalizer, bool Fallback, string Source) GetFrozenNormalizer(int year, ProjectRecordRole lane) {
		Cohort completed = null;
		for (int candidate = year - 1; candidate >= 1960; candidate--) {
			if (cohorts.TryGetValue((candidate, lane), out Cohort cohort) && cohort.Accepted >= MinimumCompletedCohortMass) { completed = cohort; break; }
		}
		if (completed == null) return (-1, 1f, true, "ProspectiveColdStart");
		if (!anchors.TryGetValue(lane, out Cohort anchor)) anchors[lane] = anchor = new Cohort { Enabled = completed.Enabled, Accepted = completed.Accepted };
		float anchorRatio = anchor.Enabled / Mathf.Max(1f, anchor.Accepted);
		float cohortRatio = completed.Enabled / Mathf.Max(1f, completed.Accepted);
		float normalizer = anchorRatio / Mathf.Max(.000001f, cohortRatio);
		if (!float.IsFinite(normalizer) || normalizer < .25f || normalizer > 4f)
			throw new InvalidOperationException($"Single opportunity safety bound hit for {lane} in {year}: {normalizer}.");
		return (year - 1, normalizer, false, "ActualCompletedCohort");
	}
}
