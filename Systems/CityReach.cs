using System;
using System.Collections.Generic;

/// <summary>
/// City reach (SimTools/RegionalSyncDesign.md §9d): where a single is actually in play, at city grain. At launch a
/// record reaches every place in the regions its label ships it to or is strong in, plus its home city and the
/// places within <see cref="LaunchRadiusMiles"/> of it. Each week it spreads from the places it has reached in
/// proportion to its sales standing there, decaying with road distance (<see cref="SpreadMiles"/>); a national
/// top-40 record reaches everywhere (one-stops racked the national list). A region's reach is the population share
/// of its places reached. Before a place is reached its stations cannot add the record; a region's share of the
/// national awareness blend, its chart floors and its rotation target scale with its reach, keeping a hearsay floor.
/// Launch stock and launch awareness are not held, so out-of-territory launch sales are kept. Reads settled state
/// only; no draws. State is runtime-only (probe switch, not yet saved); a record without state reads as fully reached.
/// </summary>
public static class CityReach {
	private const string Flag = "--city-reach";
	public const float LaunchRadiusMiles = 250f, SpreadMiles = 400f, HearsayShare = .25f;
	private const int ForgetAfterWeeks = 52;
	private static bool configured;
	public static bool Enabled { get; private set; }

	private sealed class State { public ulong Reached; public float[] Exposure; }
	private static readonly Dictionary<string, State> states = new(StringComparer.Ordinal);
	private static IReadOnlyList<(string Id, string RegionId, string CityId, float Population)> places;
	private static float[,] miles;
	private static readonly Dictionary<string, int> placeByCityName = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, float> regionPopulation = new(StringComparer.Ordinal);

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string a in arguments ?? Array.Empty<string>()) if (a == Flag) Enabled = true;
		configured = true;
	}

	/// <summary>Seed a live AI single's reach at launch.</summary>
	public static void Launch(RecordRuntimeData record, AILabel label, SimulatedArtist artist) {
		if (!Enabled || record?.baseRecord == null || label == null) return;
		if (record.baseRecord.format == ReleaseFormat.Album || record.baseRecord.isPlayerOwned) return;
		EnsurePlaces();
		string home = HomeCityId(label, artist);
		var state = new State { Exposure = new float[places.Count] };
		for (int i = 0; i < places.Count; i++) {
			string region = places[i].RegionId;
			bool territory = label.HasDistributionInRegionForRecord(region, record.baseRecord.recordId) ||
				(label.strongRegions != null && Array.IndexOf(label.strongRegions, region) >= 0);
			bool near = home != null && (places[i].CityId == home ||
				DistanceModel.GetRoadMilesBetween(home, places[i].CityId) <= LaunchRadiusMiles);
			if (territory || near) state.Reached |= 1UL << i;
		}
		states[record.baseRecord.recordId] = state;
	}

	/// <summary>Weekly spread, after the breakout pass and before demand. National top 40 reaches everywhere.</summary>
	public static void Spread(RecordRuntimeData record) {
		if (!Enabled || record?.baseRecord == null || !states.TryGetValue(record.baseRecord.recordId, out State state)) return;
		if (record.weeksSinceRelease > ForgetAfterWeeks) { states.Remove(record.baseRecord.recordId); return; }
		ulong all = places.Count >= 64 ? ulong.MaxValue : (1UL << places.Count) - 1;
		if (state.Reached == all) return;
		if (record.currentPosition >= 1 && record.currentPosition <= 40) { state.Reached = all; return; }
		Span<float> strength = stackalloc float[places.Count];
		bool any = false;
		for (int q = 0; q < places.Count; q++) {
			if ((state.Reached & (1UL << q)) == 0) continue;
			strength[q] = record.regionalData.TryGetValue(places[q].RegionId, out RegionalRecordData data) ? Standing(data.localSalesRank) : 0f;
			any |= strength[q] > 0f;
		}
		if (!any) return;
		for (int p = 0; p < places.Count; p++) {
			if ((state.Reached & (1UL << p)) != 0) continue;
			float add = 0f;
			for (int q = 0; q < places.Count; q++) if (strength[q] > 0f) add += strength[q] * MathF.Exp(-miles[p, q] / SpreadMiles);
			state.Exposure[p] += add;
			if (state.Exposure[p] >= 1f) state.Reached |= 1UL << p;
		}
	}

	/// <summary>Population share of the region's places the record has reached; 1 when off or untracked.</summary>
	public static float Fraction(RecordRuntimeData record, string regionId) {
		if (!Enabled || record?.baseRecord == null || !states.TryGetValue(record.baseRecord.recordId, out State state)) return 1f;
		float reached = 0f;
		for (int i = 0; i < places.Count; i++)
			if (places[i].RegionId == regionId && (state.Reached & (1UL << i)) != 0) reached += places[i].Population;
		return regionPopulation.TryGetValue(regionId, out float total) && total > 0f ? reached / total : 1f;
	}

	/// <summary>The share of the national awareness blend a region sees: a hearsay floor plus its reach.</summary>
	public static float BlendShare(float fraction) => HearsayShare + (1f - HearsayShare) * fraction;

	/// <summary>Whether a station's city has the record; a city outside the place list falls back to its region.</summary>
	public static bool CityReached(RecordRuntimeData record, string cityName, string regionId) {
		if (!Enabled || record?.baseRecord == null || !states.TryGetValue(record.baseRecord.recordId, out State state)) return true;
		if (cityName != null && placeByCityName.TryGetValue(cityName, out int i)) return (state.Reached & (1UL << i)) != 0;
		return Fraction(record, regionId) > 0f;
	}

	/// <summary>A reached place's pull on its neighbours from the record's sales rank in its region.</summary>
	private static float Standing(int localSalesRank) =>
		localSalesRank <= 0 ? 0f : localSalesRank <= 10 ? 1f : localSalesRank <= 25 ? .5f : localSalesRank <= 40 ? .2f : 0f;

	/// <summary>The act's scene city, else the label's home city, else the hub of the act's home region.</summary>
	private static string HomeCityId(AILabel label, SimulatedArtist artist) {
		string scene = ScenePlaceRegistry.Get(artist?.geography?.basePlaceId)?.PlayableCityId;
		if (!string.IsNullOrEmpty(scene) && DistanceModel.GetCityById(scene) != null) return scene;
		if (!string.IsNullOrEmpty(label.homeCityId) && DistanceModel.GetCityById(label.homeCityId) != null) return label.homeCityId;
		string region = RegionalAudience.HomeRegionId(artist);
		return region != null ? DistanceModel.GetHubCityIdForRegion(region) : null;
	}

	private static void EnsurePlaces() {
		if (places != null) return;
		places = RegionalGenreTaste.Places();
		if (places.Count > 64) throw new InvalidOperationException("City reach holds at most 64 places.");
		miles = new float[places.Count, places.Count];
		for (int p = 0; p < places.Count; p++) {
			regionPopulation[places[p].RegionId] = regionPopulation.GetValueOrDefault(places[p].RegionId) + places[p].Population;
			if (!places[p].Id.EndsWith(":rest", StringComparison.Ordinal))
				placeByCityName[DistanceModel.GetCityById(places[p].CityId).name] = p;
			for (int q = 0; q < places.Count; q++) miles[p, q] = DistanceModel.GetRoadMilesBetween(places[p].CityId, places[q].CityId);
		}
	}
}
