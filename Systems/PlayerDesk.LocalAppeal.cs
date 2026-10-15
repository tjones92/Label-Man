using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Early-game F3: local taste and following, one bounded term inside the shared record appeal
/// (<see cref="RecordAppeal"/> / <see cref="TrunkAppeal"/>), so a counter pitch, an order's size, a shelf's sell-through
/// and a room table all read the same lift.
///
/// Following is derived, not a new meter: the town's rooms already record who came in for each of an act's sets and how
/// many stayed (<see cref="LocalSceneRoomService.LocalDraw"/>). Each room's tally fades from the act's last night there,
/// and the sum saturates, because the same regulars come back. A town where the act has never played gives nothing.
/// Taste reads the city's genre fit from <see cref="SceneCityPlacement.Fit"/> (a proxy for buyer preference, not demand).
///
/// No double count: the following is never written into regional awareness or buzz, and the one existing live-draw
/// channel (<see cref="LocalScenes.LiveDrawRecognition"/>) feeds the act's national recognition, which appeal does not read.
/// Authored values, not sourced: following up to +0.06 at saturation (120 stayed heads, 8-week half-life); taste
/// 0.025 x ln(fit) within +-0.04; the two together clamped jointly to [-0.04, +0.08].
/// </summary>
public partial class PlayerDesk {
	public const float FollowingLiftMax = 0.06f;
	public const float FollowingHeadsScale = 120f;
	public const float FollowingHalfLifeDays = 56f;
	public const float TasteLiftPerLogFit = 0.025f, TasteLiftMax = 0.04f;
	public const float LocalLiftMin = -0.04f, LocalLiftMax = 0.08f;

	// Read as of the day: a set played tonight counts from tomorrow. The shelf asks for the same act and town many
	// times a day, and the room history is a scan of six rooms, so it's memoised per day.
	private readonly Dictionary<string, float> followingCache = new(StringComparer.Ordinal);
	private int followingCacheDay = int.MinValue;

	/// <summary>0-1: how much of a following the act has in this town, from the people who came in for its sets here and stayed.</summary>
	public float LocalFollowing(string artistId, string cityId) {
		if (string.IsNullOrEmpty(artistId) || string.IsNullOrEmpty(cityId)) return 0f;
		int day = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
		if (day != followingCacheDay) { followingCache.Clear(); followingCacheDay = day; }
		string key = artistId + "|" + cityId;
		if (followingCache.TryGetValue(key, out float cached)) return cached;
		float heads = LocalSceneRoomService.LocalDraw(artistId, cityId, day, FollowingHalfLifeDays);
		float following = 1f - Mathf.Exp(-heads / FollowingHeadsScale);
		followingCache[key] = following;
		return following;
	}

	/// <summary>The town's lean toward the record's genre, as an appeal term within +-<see cref="TasteLiftMax"/>.</summary>
	public static float LocalTasteLift(Genre genre, string cityId, int year) =>
		Mathf.Clamp(TasteLiftPerLogFit * Mathf.Log((float)Math.Max(1e-3, SceneCityPlacement.Fit(cityId, genre, year))), -TasteLiftMax, TasteLiftMax);

	/// <summary>Following plus taste for this act's record in this town, clamped jointly.</summary>
	public float LocalAppealLift(string artistId, Genre genre, string cityId) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		float lift = FollowingLiftMax * LocalFollowing(artistId, cityId) + LocalTasteLift(genre, cityId, year);
		return Mathf.Clamp(lift, LocalLiftMin, LocalLiftMax);
	}

	private float LocalAppealLift(Record record, string cityId) =>
		record == null ? 0f : LocalAppealLift(record.artistId, record.primaryGenre, cityId);
}
