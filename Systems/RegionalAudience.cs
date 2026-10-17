using System;
using System.Collections.Generic;

/// <summary>
/// Feature boundary for regional audience causality (SimTools/RegionalAudienceDesign.md, audit step 3). Each
/// sub-step has its own switch so it can be measured alone and kept off as the decade A/B's control.
/// <para>
/// <see cref="LocalLifecycleClock"/> (3b): a single's post-week-8 demand-age decay in a region runs on that
/// region's local age (weeks since the record came into play there) instead of the record's national age, so a
/// region that discovers a record late gets its own life rather than inheriting a record already aged elsewhere.
/// Live (Genre Market V2) demand only; the frozen pre-live route is unchanged.
/// </para>
/// <para>
/// <see cref="LocalRadioFatigue"/> (3c): a region's stations drop a record on that region's own sales support and
/// weeks since its local best week, and the rotation a region's stations move toward blends national heat with
/// the heat its own sales list would earn (<see cref="LocalHeatWeight"/>). Changes the drop-roll count, so the RNG
/// stream moves with it.
/// </para>
/// <para>
/// <see cref="HometownAudience"/> (3g): an act's home region, and any region where it has a live following, is a
/// real audience -- the local seed 3b and 3c need before regions can diverge. At launch a single's awareness there
/// gets a headroom deposit (<see cref="ApplyLaunchDeposit"/>); in the home region's local radio heat, DJs champion
/// an act the nation does not know yet (<see cref="HometownRadioBoost"/>). Reads saved state only; no draws.
/// </para>
/// </summary>
public static class RegionalAudience {
	private static bool configured;
	private static bool localLifecycleClock, localRadioFatigue, hometownAudience;
	public static bool LocalLifecycleClock => localLifecycleClock;
	public static bool LocalRadioFatigue => localRadioFatigue;
	public static bool HometownAudience => hometownAudience;
	/// <summary>λ: the share of a region's radio target taken from its own sales list; national heat keeps the rest.</summary>
	public const float LocalHeatWeight = 0.35f;
	/// <summary>3g launch deposit: a flat hometown term plus a term on the act's live following in the region.</summary>
	public const float HomeLaunchAwareness = 0.06f, FollowingLaunchAwareness = 0.10f;
	/// <summary>Launch deposits fill headroom up to the local discovery ceiling, never above it.</summary>
	public const float LaunchDepositCeiling = 0.58f;
	/// <summary>3g: the home region's extra local radio heat for an act with no national recognition.</summary>
	public const float HometownRadioHeat = 0.10f;

	public static void Configure(bool localLifecycleClockDefault, bool localRadioFatigueDefault, bool hometownAudienceDefault,
		bool localSignalsReadDemandDefault, bool regionalArrivalDefault, IEnumerable<string> arguments) {
		if (configured) return;
		var args = new List<string>(arguments ?? Array.Empty<string>());
		localLifecycleClock = Switch(args, "local-lifecycle-clock", localLifecycleClockDefault);
		localRadioFatigue = Switch(args, "local-radio-fatigue", localRadioFatigueDefault);
		hometownAudience = Switch(args, "hometown-audience", hometownAudienceDefault);
		localSignalsReadDemand = Switch(args, "local-signals-read-demand", localSignalsReadDemandDefault);
		regionalArrival = Switch(args, "regional-arrival", regionalArrivalDefault);
		regionalRetail = Switch(args, "regional-retail-access", false);
		regionalCampaignFocus = Switch(args, "regional-campaign-focus", false);
		regionalRadioBuildClock = Switch(args, "regional-radio-build-clock", false);
		if (regionalRetail && !regionalArrival)
			throw new ArgumentException("--regional-retail-access requires --regional-arrival.");
		if (regionalCampaignFocus && !regionalArrival)
			throw new ArgumentException("--regional-campaign-focus requires --regional-arrival.");
		if (regionalRadioBuildClock && !regionalArrival)
			throw new ArgumentException("--regional-radio-build-clock requires --regional-arrival.");
		probeRegionalAwarenessOnly = args.Contains("--probe-sync-regional-awareness-only");
		probeNoFloors = probeRegionalAwarenessOnly || args.Contains("--probe-sync-no-floors");
		probeNoBlend = probeRegionalAwarenessOnly || args.Contains("--probe-sync-no-blend");
		probeLateUncovered = args.Contains("--probe-sync-late-uncovered");
		configured = true;
	}

	// Sync diagnostics (SimTools/RegionalSyncDesign.md), off unless named. Not calibrated, never scene defaults.
	// Regional-awareness-only: live single demand reads regional awareness alone, with no national blend and no
	// top-10/top-40 floors. Late-uncovered: a single launches with zero awareness and rotation in regions its
	// label does not ship to, so those regions start from nothing and are driven only by the weekly radio pass.
	// The two halves of regional-awareness-only can be named alone: --probe-sync-no-floors, --probe-sync-no-blend.
	private static bool probeRegionalAwarenessOnly, probeLateUncovered, probeNoFloors, probeNoBlend;
	public static bool ProbeNoFloors => probeNoFloors;
	public static bool ProbeNoBlend => probeNoBlend;

	// Regional sync (SimTools/RegionalSyncDesign.md). 5d: the local readers -- local peak and intro, the region's
	// sales list, the 3c support ratio and local heat, the 3f sales standing -- read the week's raw demand instead of
	// cleared units, so they measure buyers rather than restock timing. 5a-5c: a live single reaches a region its
	// label does not ship to only when one of the existing local-access paths fires; until then its promotion is
	// held and it has only the hearsay awareness blend, no rotation target and (optionally) finite shop stock.
	private static bool localSignalsReadDemand, regionalArrival;
	public static bool LocalSignalsReadDemand => localSignalsReadDemand;
	public static bool RegionalArrival => regionalArrival;
	// A shop can carry a few imported copies before the regional promotion/servicing launch.
	// Keep that finite retail allocation available; it does not open rotation or automatic restock.
	private static bool regionalRetail;
	public static bool RegionalRetail => regionalRetail;
	private static bool regionalCampaignFocus;
	public static bool RegionalCampaignFocus => regionalCampaignFocus;
	// Calibration probe: an arriving market builds rotation on its own servicing clock.
	// Off by default until its effect on the revenue recovery and chart is measured.
	private static bool regionalRadioBuildClock;
	public static bool RegionalRadioBuildClock => regionalRadioBuildClock;
	internal static int RadioBuildAge(int recordAge, RegionalRecordData data) =>
		regionalRadioBuildClock && data.arrivalAge >= 1 ? Math.Max(1, recordAge - data.arrivalAge + 1) : recordAge;
	/// <summary>The neighbour-market strength ChartManager already counts as a test market.</summary>
	public const float ArrivalTestMarketStrength = 0.08f;
	/// <summary>The region's own breakout score at which CalculateRegionalSales starts paying it discovery.</summary>
	public const float ArrivalBreakoutScore = 0.24f;
	/// <summary>One-stops racked the national list: a national top-40 record ships everywhere. The pass reads last
	/// week's chart and the deposit reaches demand the week after, which is the one-week shipping lag.</summary>
	public const int ArrivalChartDepth = 40;
	/// <summary>The earlier arrival experiment retains its shared hearsay input. Under campaign focus, discovery
	/// outside the serviced footprint must come through the region's own audience and neighbouring markets.</summary>
	public static float HearsayShare => regionalCampaignFocus ? 0f : 0.25f;

	/// <summary>This week's local sales evidence for a region: raw demand under 5d, else cleared units; 0 before arrival.</summary>
	public static int LocalSignalUnits(RegionalRecordData data) {
		if (data == null || data.awaitingArrival) return 0;
		return localSignalsReadDemand ? Math.Max(0, (int)Math.Round(data.rawDemandThisWeek)) : data.unitsSoldThisWeek;
	}

	/// <summary>
	/// 5a launch: a live single arrives at once where its label ships it, in the label's strong regions and in the
	/// act's home region. Everywhere else the launch seed the launch loop just drew is held aside -- the draws are
	/// unchanged -- and the region waits for <see cref="TryArrive"/>.
	/// </summary>
	public static void HoldUntilArrival(RecordRuntimeData record, AILabel label, SimulatedArtist artist) {
		if (!regionalArrival || record?.regionalData == null || label == null) return;
		if (record.baseRecord.format == ReleaseFormat.Album || record.baseRecord.isPlayerOwned) return;
		string home = HomeRegionId(artist);
		int age = Math.Max(1, record.weeksSinceRelease);
		foreach (var pair in record.regionalData) {
			RegionalRecordData data = pair.Value;
			if (label.HasDistributionInRegionForRecord(pair.Key, record.baseRecord.recordId) ||
				(label.strongRegions != null && Array.IndexOf(label.strongRegions, pair.Key) >= 0) || pair.Key == home) {
				// A caller may reseed promotion after the initial chart registration.
				// The current seed replaces the previous one; stale held state must not survive it.
				data.awaitingArrival = false;
				data.pendingLaunchAwareness = data.pendingLaunchRadio = 0f;
				data.pendingLaunchStock = 0;
				data.arrivalAge = age;
				arrivedAtLaunch++;
				continue;
			}
			data.awaitingArrival = true;
			data.pendingLaunchAwareness = data.awareness;
			data.pendingLaunchRadio = data.radioPlay;
			data.pendingLaunchStock = regionalRetail ? 0 : data.unitsInStores;
			data.awareness = 0f;
			data.radioPlay = 0f;
			if (!regionalRetail) data.unitsInStores = 0;
			if (++held % 2000 == 0) PrintArrival();
		}
		if (regionalCampaignFocus) ConcentrateLaunchAudience(record, ChartManager.Instance.GetAllRegions());
	}

	/// <summary>
	/// The launch campaign has a finite audience budget. Spend the part formerly assigned to unserved markets
	/// in the markets being promoted now, weighted by buying population and bounded by remaining awareness.
	/// The cancelled remote deposit cannot also land when that market is later serviced. No tier, cash or sales
	/// target enters this allocation; a national launch has nothing to move.
	/// </summary>
	internal static void ConcentrateLaunchAudience(RecordRuntimeData record, IEnumerable<MarketRegion> regions) {
		var markets = new List<(RegionalRecordData Data, float Buyers)>();
		double budget = 0, headroom = 0;
		foreach (MarketRegion region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out var data)) continue;
			float buyers = Math.Max(0f, region.GetRecordBuyingPopulation());
			if (data.awaitingArrival) {
				budget += buyers * 0.6f * data.pendingLaunchAwareness;
			} else {
				markets.Add((data, buyers));
				headroom += buyers * 0.6f * Math.Max(0f, 1f - data.awareness);
			}
		}
		if (headroom <= 0 || budget <= 0) return;
		float fill = (float)Math.Min(1d, budget / headroom);
		foreach (var market in markets)
			market.Data.awareness += (1f - market.Data.awareness) * fill;
		foreach (RegionalRecordData data in record.regionalData.Values)
			if (data.awaitingArrival) data.pendingLaunchAwareness = 0f;
	}

	/// <summary>
	/// Allocate this week's national campaign audience to the markets currently being serviced. A held market
	/// keeps its hearsay share; the unused remainder is concentrated into served markets' available audience.
	/// Recompute from the current footprint before demand, so expansion spreads the same budget more widely.
	/// This is an awareness allocation, never a sales entitlement: conversion, stock and local clearing still bind.
	/// </summary>
	internal static void PrepareCampaignAudience(RecordRuntimeData record, IEnumerable<MarketRegion> regions) {
		foreach (var data in record.regionalData.Values) data.campaignAwarenessThisWeek = 0f;
		if (!regionalCampaignFocus || record.baseRecord.format == ReleaseFormat.Album || record.baseRecord.isPlayerOwned) return;
		var markets = new List<(RegionalRecordData Data, float Headroom)>();
		double budget = 0, headroom = 0;
		foreach (MarketRegion region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out var data)) continue;
			float buyers = Math.Max(0f, region.GetRecordBuyingPopulation());
			if (data.awaitingArrival) {
				budget += buyers * 0.4f * record.awareness * (1f - HearsayShare);
			} else {
				float available = Math.Max(0f, 1f - (0.4f * record.awareness + 0.6f * data.awareness));
				markets.Add((data, available));
				headroom += buyers * available;
			}
		}
		if (headroom <= 0 || budget <= 0) return;
		float fill = (float)Math.Min(1d, budget / headroom);
		foreach (var market in markets) market.Data.campaignAwarenessThisWeek = market.Headroom * fill;
	}

	/// <summary>
	/// 5a weekly: a waiting region takes the record the week any local-access path fires -- a neighbour test, its
	/// own breakout, a rack jobber buying it on regional proof, the national top 40, or distribution the label has
	/// actually acquired for this record since launch. The held launch seed lands
	/// then. Reads settled state only; no draws.
	/// </summary>
	public static void TryArrive(RecordRuntimeData record, RegionalRecordData data, AILabel label = null) {
		if (data == null || !data.awaitingArrival) return;
		int trigger =
			label?.HasDistributionInRegionForRecord(data.regionId, record.baseRecord?.recordId) == true ? 4
			: record.currentPosition >= 1 && record.currentPosition <= ArrivalChartDepth ? 0
			: data.neighboringMarketTestStrength >= ArrivalTestMarketStrength ? 1
			: data.breakoutScore > ArrivalBreakoutScore ? 2
			: ChartSimulator.GetRackJobberAccess(0, data.peakBreakoutScore) > 0f ? 3
			: -1;
		if (trigger < 0) return;
		int age = Math.Max(1, record.weeksSinceRelease);
		data.awaitingArrival = false;
		data.arrivalAge = age;
		if (data.localIntroAge < 0) data.localIntroAge = age;
		data.awareness = Math.Clamp(data.awareness + data.pendingLaunchAwareness, 0f, 1f);
		data.radioPlay = Math.Clamp(data.radioPlay + data.pendingLaunchRadio, 0f, 1f);
		data.unitsInStores += data.pendingLaunchStock;
		data.pendingLaunchAwareness = data.pendingLaunchRadio = 0f;
		data.pendingLaunchStock = 0;
		arrivalsByTrigger[trigger]++;
		arrivalAgeSum += age;
		if (ArrivalCount() % 250 == 0) PrintArrival();
	}
	// Console diagnostics only (no CSV reads them).
	private static int arrivedAtLaunch, held;
	private static readonly int[] arrivalsByTrigger = new int[5];
	private static long arrivalAgeSum;
	private static int ArrivalCount() => arrivalsByTrigger[0] + arrivalsByTrigger[1] + arrivalsByTrigger[2] + arrivalsByTrigger[3] + arrivalsByTrigger[4];
	private static void PrintArrival() =>
		Godot.GD.Print($"REGIONAL_ARRIVAL atLaunch={arrivedAtLaunch} held={held} chart={arrivalsByTrigger[0]} " +
			$"test={arrivalsByTrigger[1]} breakout={arrivalsByTrigger[2]} rack={arrivalsByTrigger[3]} distribution={arrivalsByTrigger[4]} " +
			$"meanAge={arrivalAgeSum / (double)Math.Max(1, ArrivalCount()):F2}");

	public static void ApplyLaunchProbe(RecordRuntimeData record, AILabel label) {
		if (!probeLateUncovered || record?.regionalData == null || label == null) return;
		if (record.baseRecord.format == ReleaseFormat.Album || record.baseRecord.isPlayerOwned) return;
		foreach (var pair in record.regionalData) {
			if (label.HasDistributionInRegionForRecord(pair.Key, record.baseRecord.recordId)) continue;
			pair.Value.awareness = 0f;
			pair.Value.radioPlay = 0f;
		}
	}

	/// <summary>The market region the act is based in: its scene base place, else its legacy home-region name.</summary>
	public static string HomeRegionId(SimulatedArtist artist) {
		if (artist == null) return null;
		string placeRegion = ScenePlaceRegistry.Get(artist.geography?.basePlaceId)?.MarketRegionId;
		return !string.IsNullOrEmpty(placeRegion) ? placeRegion : SceneRegionAdapter.ToId(artist.homeRegion);
	}

	/// <summary>
	/// 0-1: the act's live following in a region, the best of its towns. The player's LocalFollowing arithmetic
	/// (people who came in for its sets and stayed, fading over 56 days, saturating at 120 heads); 0 for an act
	/// that has never played there.
	/// </summary>
	public static float RegionalFollowing(string artistId, string regionId, int day) {
		if (string.IsNullOrEmpty(artistId) || string.IsNullOrEmpty(regionId)) return 0f;
		float best = 0f;
		foreach (ScenePlace place in PlayableIn(regionId)) {
			float heads = LocalSceneRoomService.LocalDraw(artistId, place.Id, day, PlayerDesk.FollowingHalfLifeDays);
			if (heads > 0f) best = Math.Max(best, 1f - (float)Math.Exp(-heads / PlayerDesk.FollowingHeadsScale));
		}
		return best;
	}

	/// <summary>
	/// 3g launch: a single's awareness in the act's home region, and in any region where it has a live following,
	/// gets a deposit into its remaining headroom. Called after the launch's draws, and adds none.
	/// </summary>
	public static void ApplyLaunchDeposit(RecordRuntimeData record, SimulatedArtist artist, GameDate date) {
		if (!hometownAudience || record?.regionalData == null || artist == null) return;
		if (record.baseRecord.format == ReleaseFormat.Album || record.baseRecord.isPlayerOwned) return;
		string home = HomeRegionId(artist);
		int day = LocalSceneRoomService.Day(date);
		launches++;
		if (home != null && record.regionalData.ContainsKey(home)) homeResolved++;
		foreach (var pair in record.regionalData) {
			float following = RegionalFollowing(artist.artistId, pair.Key, day);
			if (following > 0f) { followingRegions++; followingSum += following; }
			float deposit = (pair.Key == home ? HomeLaunchAwareness : 0f) + FollowingLaunchAwareness * following;
			if (deposit > 0f) depositSum += ChartManager.AddWithinHeadroom(ref pair.Value.awareness, deposit, LaunchDepositCeiling);
		}
		if (launches % 2000 == 0)
			Godot.GD.Print($"HOMETOWN_AUDIENCE launches={launches} homeResolved={homeResolved} followingRegions={followingRegions} " +
				$"meanFollowing={(followingRegions > 0 ? followingSum / followingRegions : 0f):F3} depositPerLaunch={depositSum / launches:F4}");
	}
	// Console diagnostics only (no CSV reads them).
	private static int launches, homeResolved, followingRegions;
	private static float followingSum, depositSum;

	/// <summary>3g radio: the extra local heat the home region's DJs give an act, fading as the nation learns its name.</summary>
	public static float HometownRadioBoost(SimulatedArtist artist, string regionId) =>
		hometownAudience && artist != null && regionId != null && regionId == HomeRegionId(artist)
			? HometownRadioHeat * (1f - ArtistRecognitionService.EffectiveRecognition(artist)) : 0f;

	private static readonly Dictionary<string, IReadOnlyList<ScenePlace>> playableByRegion = new(StringComparer.Ordinal);
	private static IReadOnlyList<ScenePlace> PlayableIn(string regionId) {
		if (!playableByRegion.TryGetValue(regionId, out var places))
			playableByRegion[regionId] = places = ScenePlaceRegistry.PlayableInRegion(regionId);
		return places;
	}

	private static bool Switch(List<string> args, string name, bool sceneDefault) {
		bool enable = args.Contains("--" + name), disable = args.Contains("--no-" + name);
		if (enable && disable) throw new ArgumentException($"--{name} and --no-{name} cannot be used together.");
		return enable || (!disable && sceneDefault);
	}

	internal static void ConfigureForProbe(bool localClock, bool localRadio = false, bool hometown = false,
		bool signalsReadDemand = false, bool arrival = false, bool retail = false, bool campaignFocus = false,
		bool radioBuildClock = false) {
		configured = true; localLifecycleClock = localClock; localRadioFatigue = localRadio; hometownAudience = hometown;
		localSignalsReadDemand = signalsReadDemand; regionalArrival = arrival;
		regionalRetail = retail;
		regionalCampaignFocus = campaignFocus;
		regionalRadioBuildClock = radioBuildClock;
	}
}
