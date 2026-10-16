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
		IEnumerable<string> arguments) {
		if (configured) return;
		var args = new List<string>(arguments ?? Array.Empty<string>());
		localLifecycleClock = Switch(args, "local-lifecycle-clock", localLifecycleClockDefault);
		localRadioFatigue = Switch(args, "local-radio-fatigue", localRadioFatigueDefault);
		hometownAudience = Switch(args, "hometown-audience", hometownAudienceDefault);
		configured = true;
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

	internal static void ConfigureForProbe(bool localClock, bool localRadio = false, bool hometown = false) {
		configured = true; localLifecycleClock = localClock; localRadioFatigue = localRadio; hometownAudience = hometown;
	}
}
