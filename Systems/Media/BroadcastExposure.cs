using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Who is watching. A viewing population only: no purchasing capacity hangs off it (§12.3).</summary>
public enum ViewerGroup { Child = 0, Teen = 1, YoungAdult = 2, Adult = 3 }

/// <summary>What one appearance delivered, kept as separate measures (§4.2). Viewer-minutes are not people.</summary>
public sealed class ExposureEstimate {
	public double PeakViewers;          // most simultaneous viewers in any 15-minute bucket of the act's segment
	public double AverageViewers;       // mean over the act's segment
	public double ViewerMinutes;        // attentive exposure: the integral, not a head count
	public double UniqueReach;          // bounded estimate of different people who saw the act
	public double[] AverageByGroup = new double[4];
	public int MarketsCleared;          // markets where the airing actually ran
	public int MarketsPreempted;
	/// <summary>Plain-language range for the player, never a precise rating (§4.5).</summary>
	public (double Low, double High) ReachRange() => (UniqueReach * 0.7, UniqueReach * 1.3);
}

/// <summary>
/// Available audience × clearance × program share × segment retention, integrated over 15-minute local-time
/// buckets (directive §4.2). All curves are initial design values, not measured ratings. Pure functions of the
/// catalog, the date and the frozen region data: no RNG, no economic reads beyond population.
/// </summary>
public static class BroadcastExposure {
	public const int BucketMinutes = 15;

	// ---- demographics -----------------------------------------------------------------------------------------

	/// <summary>Share of the population in each viewer group (initial design values; teens grow through the decade).</summary>
	public static float GroupShare(ViewerGroup g, int year) {
		float t = Math.Clamp((year - 1960) / 9f, 0f, 1f);
		return g switch {
			ViewerGroup.Child => Lerp(0.200f, 0.185f, t),
			ViewerGroup.Teen => Lerp(0.094f, 0.114f, t),
			ViewerGroup.YoungAdult => Lerp(0.215f, 0.240f, t),
			_ => Lerp(0.450f, 0.425f, t),
		};
	}

	/// <summary>Television households as a share of all households (initial design values around the national figure).</summary>
	public static float TvAccess(int year, string regionId) {
		float national = year <= 1960 ? 0.87f : year >= 1969 ? 0.95f : Lerp(0.87f, 0.95f, (year - 1960) / 9f);
		float regional = regionId switch { "rockies" => 0.93f, "greatplains" => 0.96f, "deepsouth" => 0.94f, _ => 1.0f };
		return Math.Min(0.99f, national * regional);
	}

	// ---- viewing likelihood -------------------------------------------------------------------------------------

	private enum DayKind { Weekday, Saturday, Sunday }

	// (local minute, share of the group watching any television) breakpoints, linearly interpolated.
	private static readonly Dictionary<(DayKind, ViewerGroup), (int Minute, float Value)[]> curves = new() {
		[(DayKind.Weekday, ViewerGroup.Child)] = new[] { (420, 0.05f), (480, 0.03f), (900, 0.05f), (960, 0.30f), (1050, 0.38f), (1140, 0.45f), (1200, 0.35f), (1260, 0.10f), (1320, 0.02f) },
		[(DayKind.Weekday, ViewerGroup.Teen)] = new[] { (420, 0.03f), (480, 0.01f), (900, 0.03f), (930, 0.18f), (990, 0.30f), (1080, 0.32f), (1170, 0.45f), (1260, 0.38f), (1320, 0.15f), (1380, 0.05f) },
		[(DayKind.Weekday, ViewerGroup.YoungAdult)] = new[] { (420, 0.04f), (600, 0.08f), (960, 0.10f), (1080, 0.25f), (1200, 0.45f), (1290, 0.42f), (1380, 0.18f), (1440, 0.05f) },
		[(DayKind.Weekday, ViewerGroup.Adult)] = new[] { (420, 0.06f), (600, 0.12f), (900, 0.12f), (1080, 0.30f), (1200, 0.58f), (1290, 0.55f), (1380, 0.25f), (1440, 0.06f) },
		[(DayKind.Saturday, ViewerGroup.Child)] = new[] { (420, 0.20f), (480, 0.45f), (720, 0.40f), (780, 0.20f), (1020, 0.18f), (1140, 0.40f), (1230, 0.25f), (1290, 0.05f) },
		[(DayKind.Saturday, ViewerGroup.Teen)] = new[] { (480, 0.05f), (720, 0.12f), (780, 0.20f), (1020, 0.20f), (1140, 0.38f), (1260, 0.35f), (1350, 0.15f), (1410, 0.05f) },
		[(DayKind.Saturday, ViewerGroup.YoungAdult)] = new[] { (480, 0.04f), (780, 0.10f), (1020, 0.12f), (1140, 0.30f), (1260, 0.35f), (1380, 0.20f), (1440, 0.06f) },
		[(DayKind.Saturday, ViewerGroup.Adult)] = new[] { (480, 0.06f), (780, 0.15f), (1020, 0.18f), (1140, 0.42f), (1260, 0.52f), (1380, 0.30f), (1440, 0.08f) },
		[(DayKind.Sunday, ViewerGroup.Child)] = new[] { (480, 0.10f), (720, 0.15f), (1020, 0.25f), (1140, 0.50f), (1230, 0.48f), (1290, 0.12f), (1350, 0.03f) },
		[(DayKind.Sunday, ViewerGroup.Teen)] = new[] { (480, 0.04f), (720, 0.10f), (1020, 0.20f), (1140, 0.48f), (1230, 0.50f), (1320, 0.30f), (1380, 0.10f) },
		[(DayKind.Sunday, ViewerGroup.YoungAdult)] = new[] { (480, 0.05f), (720, 0.12f), (1020, 0.20f), (1140, 0.48f), (1260, 0.46f), (1380, 0.18f), (1440, 0.05f) },
		[(DayKind.Sunday, ViewerGroup.Adult)] = new[] { (480, 0.06f), (720, 0.18f), (1020, 0.28f), (1140, 0.62f), (1260, 0.60f), (1380, 0.25f), (1440, 0.06f) },
	};

	private static bool SchoolHoliday(GameDate date) =>
		date.month == 7 || date.month == 8 || (date.month == 6 && date.day >= 15) || (date.month == 12 && date.day >= 22) || (date.month == 1 && date.day <= 2);

	/// <summary>Share of a viewer group watching any television at a local time (§4.3). Summer evenings lose viewers to
	/// other leisure; school holidays open the weekday afternoon to teens and children. Seasonality lives here, never
	/// in record sales.</summary>
	public static float ViewingLikelihood(ViewerGroup group, GameDate date, int localMinute) {
		DayKind kind = date.DayOfWeek == DayOfWeek.Saturday ? DayKind.Saturday : date.DayOfWeek == DayOfWeek.Sunday ? DayKind.Sunday : DayKind.Weekday;
		float v = Interpolate(curves[(kind, group)], localMinute);
		bool summer = date.month >= 6 && date.month <= 8;
		if (summer && localMinute >= 1080) v *= 0.82f;
		if (kind == DayKind.Weekday && SchoolHoliday(date) && (group == ViewerGroup.Teen || group == ViewerGroup.Child) && localMinute >= 540 && localMinute < 960)
			v = Math.Max(v, group == ViewerGroup.Teen ? 0.14f : 0.22f);
		return v;
	}

	private static float Interpolate((int Minute, float Value)[] points, int minute) {
		if (minute <= points[0].Minute) return points[0].Value;
		for (int i = 1; i < points.Length; i++) {
			if (minute <= points[i].Minute) {
				float t = (minute - points[i - 1].Minute) / (float)(points[i].Minute - points[i - 1].Minute);
				return Lerp(points[i - 1].Value, points[i].Value, t);
			}
		}
		return points[^1].Value;
	}

	// ---- carriage and local time ----------------------------------------------------------------------------------

	/// <summary>Local clock start in a zone for a network origin minute (§4.4). Historical schedule rules, not the machine's timezone.</summary>
	public static int LocalStart(CarriageRule rule, int originMinute, TimeZoneBand zone) => rule switch {
		CarriageRule.NetworkPrimetime => zone switch {
			TimeZoneBand.Central or TimeZoneBand.Mountain => originMinute - 60,
			_ => originMinute,
		},
		CarriageRule.NetworkSimultaneous => zone switch {
			TimeZoneBand.Central => originMinute - 60,
			TimeZoneBand.Mountain => originMinute - 120,
			// A live Eastern afternoon lands in the Pacific lunch hour; west-coast affiliates took a delayed tape
			// into their own after-school slot instead (initial carriage assumption).
			TimeZoneBand.Pacific => originMinute - 180 < 900 ? Math.Max(originMinute - 180, 960) : originMinute - 180,
			_ => originMinute,
		},
		_ => originMinute,
	};

	/// <summary>Share of a region's stations clearing a network program (§4.2 clearance). ABC was the thin network: its
	/// clearance reads each region's bandstandReach prior. CBS and NBC cleared nearly everywhere.</summary>
	public static float Clearance(TvNetwork network, MarketRegion region) => network switch {
		TvNetwork.ABC => Math.Clamp(region?.media?.bandstandReach ?? 0.6f, 0.2f, 0.95f),
		TvNetwork.CBS => 0.97f,
		TvNetwork.NBC => 0.95f,
		_ => 1f,
	};

	/// <summary>Background competition at a local time: the other channels and everything else on (§4.2 residual "other viewing").</summary>
	private static float Competition(GameDate date, int localMinute, TvProgram program) {
		bool primetime = localMinute >= 1170 && localMinute < 1350;
		float baseline = primetime ? 2.2f : localMinute >= 900 && localMinute < 1170 ? 1.2f : 1.0f;
		// A local show competes with a smaller dial in a small market; the national shows face the whole lineup.
		if (program.Network == TvNetwork.Local) baseline *= 0.9f;
		return baseline;
	}

	/// <summary>Program share of the viewers available in a group, bounded below one (§4.2). A routine guest's draw is a
	/// bounded substitution between programs, at most 10% of the baseline share. An announced event appearance (an
	/// advertised headliner with real hype behind it) may substitute far more, up to <see cref="MaxEventLift"/> of the
	/// baseline share, still taken from the competing programs, never from viewers who aren't there.</summary>
	public static float ProgramShare(TvProgram program, ViewerGroup group, GameDate date, int localMinute, float guestDraw, float eventLift = 0f) {
		float a = program.Attraction[(int)group];
		float share = a / (a + Competition(date, localMinute, program));
		return Math.Min(0.95f, share * (1f + Math.Clamp(guestDraw, 0f, 0.10f) + Math.Clamp(eventLift, 0f, MaxEventLift)));
	}

	/// <summary>Ceiling on an event's share substitution: a 0.33 family share can roughly double toward the 60% an
	/// exceptional night has been estimated at (initial design value).</summary>
	public const float MaxEventLift = 0.90f;
	/// <summary>Ceiling on an event's tune-in: people who would not have watched television at all that hour (§4.2's
	/// "not watching" outcome) turning a set on. A share of the absent, bounded by the population (initial design value).</summary>
	public const float MaxTuneIn = 0.35f;
	private const float TunedInCeiling = 0.92f;

	/// <summary>Viewing likelihood with an event's tune-in: a fraction of the group not watching switches on. Bounded so
	/// the whole group can never exceed <see cref="TunedInCeiling"/> of itself.</summary>
	public static float TunedIn(float likelihood, float tuneIn) {
		if (tuneIn <= 0f) return likelihood;
		float lifted = likelihood + Math.Clamp(tuneIn, 0f, MaxTuneIn) * (1f - likelihood);
		return Math.Min(Math.Max(likelihood, TunedInCeiling), lifted);
	}

	/// <summary>Viewers still watching at a segment's position in the running order (§4.5): no universal best slot. An
	/// opening number rides the lead-in; a closing number loses some to bedtime and switching, more on a late show.</summary>
	public static float SegmentRetention(int slotIndex, int slotCount, int localEndMinute) {
		if (slotCount <= 1) return 0.97f;
		float position = slotIndex / (float)(slotCount - 1);
		float retention = 1f - 0.10f * position;
		if (position >= 0.99f && localEndMinute >= 1290) retention -= 0.05f;
		return retention;
	}

	// ---- markets --------------------------------------------------------------------------------------------------

	/// <summary>One market an airing reaches: a slice of a region or one city's own stations, with its local clock and date.</summary>
	public readonly struct Market {
		public readonly string Id, RegionId;
		public readonly double Population;
		public readonly TimeZoneBand Zone;
		public readonly float Clearance;
		public readonly GameDate Date;
		public readonly int LocalStartMinute;
		public Market(string id, string regionId, double population, TimeZoneBand zone, float clearance, GameDate date, int localStart) {
			Id = id; RegionId = regionId; Population = population; Zone = zone; Clearance = clearance; Date = date; LocalStartMinute = localStart;
		}
	}

	/// <summary>Region slices by time zone, weighted by the metro populations of the cities in each region.</summary>
	public static IEnumerable<(TimeZoneBand Zone, double Share)> ZoneSplit(string regionId, int year) {
		var cities = MediaOutletCatalog.CityIds.Where(c => DistanceModel.GetCityById(c)?.parentRegionId == regionId).ToList();
		double total = cities.Sum(c => MediaOutletCatalog.MetroPopulationMillions(c, year));
		if (total <= 0) { yield return (TimeZoneBand.Eastern, 1.0); yield break; }
		foreach (var group in cities.GroupBy(MediaOutletCatalog.ZoneOf).OrderBy(g => g.Key))
			yield return (group.Key, group.Sum(c => MediaOutletCatalog.MetroPopulationMillions(c, year)) / total);
	}

	/// <summary>Every market an original airing on <paramref name="date"/> reaches, with its own local hour. A local
	/// show reaches only its city; a syndicated tape is a separate dated airing per market (see SyndicatedAirings).</summary>
	public static List<Market> Markets(TvProgram program, BroadcastScheduleVersion version, GameDate date, IReadOnlyList<MarketRegion> regions) {
		var markets = new List<Market>();
		if (program.Network == TvNetwork.Local) {
			string city = program.HomeCityId;
			string region = DistanceModel.GetCityById(city)?.parentRegionId;
			markets.Add(new Market(city, region, MediaOutletCatalog.MetroPopulationMillions(city, date.year) * 1e6, MediaOutletCatalog.ZoneOf(city), 1f, date, version.StartMinute));
			return markets;
		}
		foreach (MarketRegion region in regions ?? Array.Empty<MarketRegion>()) {
			float clearance = Clearance(program.Network, region);
			foreach (var (zone, share) in ZoneSplit(region.regionId, date.year))
				markets.Add(new Market(region.regionId + ":" + zone, region.regionId, region.population * 1e6 * share, zone, clearance, date,
					LocalStart(version.Carriage, version.StartMinute, zone)));
		}
		return markets;
	}

	/// <summary>The dated airings of one syndicated tape: each market on its own day and hour, weeks behind the origin.
	/// Unreceived markets get nothing; the national population is never multiplied by the number of airings.</summary>
	public static List<Market> SyndicatedAirings(TvProgram program, GameDate originDate) {
		var list = new List<Market>();
		foreach (SyndicationMarket m in program.Syndication) {
			GameDate local = originDate.AddDays(7 * m.DelayWeeks);
			local = local.AddDays(((int)m.Day - (int)local.DayOfWeek + 7) % 7);
			if (local < m.From) continue;
			string region = DistanceModel.GetCityById(m.CityId)?.parentRegionId;
			list.Add(new Market(m.CityId, region, MediaOutletCatalog.MetroPopulationMillions(m.CityId, local.year) * 1e6, MediaOutletCatalog.ZoneOf(m.CityId), 1f, local, m.LocalStartMinute));
		}
		return list;
	}

	// ---- the integral -------------------------------------------------------------------------------------------------

	/// <summary>
	/// Exposure of one act's segment across the given markets. The segment occupies its own minutes inside the episode;
	/// only buckets overlapping it are integrated. A preempted market (clearance zero) adds nothing. Novelty below one
	/// models a repeat: fewer new viewers, never the original incident again.
	/// </summary>
	public static ExposureEstimate Estimate(TvProgram program, int durationMinutes, int slotIndex, int slotCount, float guestDraw,
		IEnumerable<Market> markets, float novelty = 1f, float eventLift = 0f, float tuneIn = 0f) {
		var result = new ExposureEstimate();
		int segmentMinutes = Math.Max(3, Math.Min(6, durationMinutes / Math.Max(2, slotCount + 1)));
		int offset = (int)((durationMinutes - segmentMinutes) * (slotCount <= 1 ? 0.5f : slotIndex / (float)(slotCount - 1)));
		var peakByBucket = new Dictionary<int, double>();
		double groupMinutes0 = 0, groupMinutes1 = 0, groupMinutes2 = 0, groupMinutes3 = 0;
		foreach (Market market in markets) {
			if (market.Clearance <= 0f || market.Population <= 0) { result.MarketsPreempted++; continue; }
			result.MarketsCleared++;
			int start = market.LocalStartMinute + offset, end = start + segmentMinutes;
			float retention = SegmentRetention(slotIndex, slotCount, market.LocalStartMinute + durationMinutes);
			double marketUnique = 0;
			for (int minute = start; minute < end; minute += BucketMinutes) {
				int bucketEnd = Math.Min(end, (minute / BucketMinutes + 1) * BucketMinutes);
				int span = Math.Max(1, bucketEnd - minute);
				double bucketViewers = 0;
				for (int g = 0; g < 4; g++) {
					var group = (ViewerGroup)g;
					double available = market.Population * GroupShare(group, market.Date.year) * TvAccess(market.Date.year, market.RegionId)
						* TunedIn(ViewingLikelihood(group, market.Date, minute), tuneIn);
					double viewers = available * market.Clearance * ProgramShare(program, group, market.Date, minute, guestDraw, eventLift) * retention * novelty;
					bucketViewers += viewers;
					double vm = viewers * span;
					switch (g) { case 0: groupMinutes0 += vm; break; case 1: groupMinutes1 += vm; break; case 2: groupMinutes2 += vm; break; default: groupMinutes3 += vm; break; }
				}
				// Simultaneity is absolute time: 8 p.m. in New York and 8 p.m. on a delayed Pacific tape are three hours apart.
				int bucketKey = (DayIndex(market.Date) * 1440 + minute + ZoneOffsetMinutes(market.Zone)) / BucketMinutes;
				peakByBucket[bucketKey] = peakByBucket.GetValueOrDefault(bucketKey) + bucketViewers;
				result.ViewerMinutes += bucketViewers * span;
				marketUnique = Math.Max(marketUnique, bucketViewers);
			}
			// Viewers switch in and out during even a short segment; cap at the market's whole viewing population.
			double ceiling = market.Population * TvAccess(market.Date.year, market.RegionId) * market.Clearance;
			result.UniqueReach += Math.Min(ceiling, marketUnique * (1.0 + 0.04 * segmentMinutes / (double)BucketMinutes));
		}
		result.PeakViewers = peakByBucket.Count == 0 ? 0 : peakByBucket.Values.Max();
		result.AverageViewers = result.ViewerMinutes / segmentMinutes;
		result.AverageByGroup = new[] { groupMinutes0 / segmentMinutes, groupMinutes1 / segmentMinutes, groupMinutes2 / segmentMinutes, groupMinutes3 / segmentMinutes };
		return result;
	}

	/// <summary>Combining two airings of the same people never double-counts them (§4.5): later airings add only the share
	/// of their audience not already reached, under a simple independent-overlap assumption within the same population.</summary>
	public static double CombineReach(double reachedSoFar, double newAudience, double population) {
		if (population <= 0) return reachedSoFar;
		double p1 = Math.Clamp(reachedSoFar / population, 0, 1), p2 = Math.Clamp(newAudience / population, 0, 1);
		return population * (1 - (1 - p1) * (1 - p2));
	}

	public static int ZoneOffsetMinutes(TimeZoneBand zone) => zone switch {
		TimeZoneBand.Central => 60, TimeZoneBand.Mountain => 120, TimeZoneBand.Pacific => 180, _ => 0 };
	public static int DayIndex(GameDate date) => (int)(new DateTime(date.year, date.month, date.day) - new DateTime(1960, 1, 1)).TotalDays;

	private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
