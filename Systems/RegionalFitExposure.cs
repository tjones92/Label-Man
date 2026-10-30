using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Regional sync §9s (SimTools/RegionalSyncDesign.md): exposure-gated fit realization. Off, a live single that draws a
/// connected commercial fit connects in all seven regions from its first week, so a hit goes national at once and no
/// record breaks in one market and spreads. When on (<c>--regional-fit-exposure</c>), the draw is kept but the
/// connection is realized only where the record is exposed. An unexposed region reads the national factor capped at
/// the reference launch's median (<see cref="SingleCommercialFit.UnexposedCapLog"/>), so flops, which are under the cap
/// anyway, lose nothing, and launch stock and awareness are never held (the cost that sank the §6c arrival arms).
/// <para>
/// Exposure: at launch, the act's and the label's home regions, plus each other region on a keyed draw under
/// <see cref="LaunchBase"/> + <see cref="LaunchReachSlope"/> × the label's national reach for the record, so Big-5
/// and the large independents are exposed everywhere. Later, for connected records only: a neighbour of an exposed
/// region where the record is in the local top <see cref="NeighbourRank"/>, <see cref="NeighbourLag"/> weeks on; and
/// every region once it is in the national top <see cref="NationalRank"/>, the next week. A late region's
/// <c>localIntroAge</c> restarts at its exposure, so 3b's demand-age clock runs from there; on the national clock the
/// late region would get an already-aged record and the stagger would vanish. Keyed draws only; the RNG stream is
/// untouched.
/// </para>
/// </summary>
public static class RegionalFitExposure {
	public const string StationsFlag = "--regional-fit-exposure-stations";
	public static bool StationSupport { get; private set; }
	public const string Flag = "--regional-fit-exposure";
	/// <summary>
	/// §9u: a region exposed late relives the record's rise. Its chart exposure weight (rack space, jukebox slots) reads
	/// the national position the record held at the region's local age, and the release ramp runs on that age. Without
	/// it the late region is handed the record's current national exposure and enters its top 40 the week it is exposed,
	/// so no stagger opens (the 3b clock only moves the post-week-8 decay).
	/// </summary>
	public const string ReliveFlag = "--regional-fit-exposure-relive";
	/// <summary>
	/// §9u: with <see cref="ReliveFlag"/>, a late region also relives the record's awareness: the national blend, the
	/// chart floors and the chart visibility read the record's awareness and position at the region's local age.
	/// </summary>
	public const string ReliveAwarenessFlag = "--regional-fit-exposure-relive-awareness";
	public const string LaunchBasePrefix = "--regional-fit-exposure-launch-base=";
	public const float LaunchReachSlope = .7f;
	public static float LaunchBase { get; private set; } = .3f;
	public const int NeighbourRank = 40, NeighbourLag = 2, NationalRank = 40, NationalLag = 1;
	/// <summary><c>fitExposureAge</c> of a region the record has not reached. 0 (the field default) is exposed from launch.</summary>
	public const int NotExposed = int.MaxValue;
	private const uint LaunchDecision = 0x43460200U;
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static bool Relive { get; private set; }
	public static bool ReliveAwareness { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Contains(Flag);
		StationSupport = args.Contains(StationsFlag);
		if (StationSupport && (!Enabled || StationRegionalSupport.ChartReadsNational))
			throw new ArgumentException(StationsFlag + " requires live regional exposure and cannot use shadow playlists.");
		Relive = args.Contains(ReliveFlag);
		ReliveAwareness = args.Contains(ReliveAwarenessFlag);
		if (ReliveAwareness && !Relive) throw new ArgumentException(ReliveAwarenessFlag + " requires " + ReliveFlag + ".");
		string rawBase = args.FirstOrDefault(a => a.StartsWith(LaunchBasePrefix, StringComparison.Ordinal))?.Substring(LaunchBasePrefix.Length);
		if (rawBase != null) {
			if (!float.TryParse(rawBase, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
				out float value) || value < 0f || value > 1f)
				throw new ArgumentException(LaunchBasePrefix + " takes a number from 0 to 1.");
			LaunchBase = value;
		}
		if ((Relive || rawBase != null) && !Enabled)
			throw new ArgumentException(ReliveFlag + " and " + LaunchBasePrefix + " require " + Flag + ".");
		if (Enabled && !(SingleCommercialFit.Enabled && SingleCommercialFit.HitShare < 1f))
			throw new ArgumentException(Flag + " requires the kinked commercial fit (" + SingleCommercialFit.HitSharePrefix + ").");
		if (Enabled && !RegionalAudience.LocalLifecycleClock)
			throw new ArgumentException(Flag + " requires the local lifecycle clock (3b), which runs a late region from its exposure.");
		if (Enabled) Godot.GD.Print($"REGIONAL_FIT_EXPOSURE on capLog={SingleCommercialFit.UnexposedCapLog:F3} launchBase={LaunchBase:F2} relive={Relive} reliveAwareness={ReliveAwareness}");
		configured = true;
	}

	/// <summary>
	/// §9u: log the chart position this week's demand reads (the internal previous position) and, with awareness relive,
	/// the record's national awareness, at the record's age, so a late region can relive them. Live AI singles only,
	/// before the week's sales.
	/// </summary>
	public static void NotePosition(RecordRuntimeData record, int position) {
		if (!Relive || record.baseRecord.format == ReleaseFormat.Album || !SingleCommercialFit.Applies(record)) return;
		int age = record.weeksSinceRelease;
		Note(record.fitRelivePositions ??= new List<int>(), age, position);
		if (ReliveAwareness) Note(record.fitReliveAwareness ??= new List<float>(), age, record.awareness);
	}

	private static void Note<T>(List<T> log, int age, T value) {
		while (log.Count < age) log.Add(default);
		if (log.Count == age) log.Add(value); else log[age] = value;
	}

	/// <summary>§9u: the record's national awareness at that age; the current awareness if not logged.</summary>
	public static float AwarenessAt(RecordRuntimeData record, int age) {
		var log = record.fitReliveAwareness;
		return log != null && age >= 0 && age < log.Count ? log[age] : record.awareness;
	}

	/// <summary>§9u: the region's local age while it relives the record (exposed late, from its exposure); 0 otherwise.</summary>
	public static int ReliveAge(RecordRuntimeData record, RegionalRecordData data) {
		if (!Relive || data == null || data.fitExposureAge <= 1 || data.fitExposureAge == NotExposed) return 0;
		int age = record.weeksSinceRelease;
		return age >= data.fitExposureAge ? age - data.fitExposureAge + 1 : 0;
	}

	/// <summary>§9u: the position the record's demand read at that age; 0 (uncharted) if not logged.</summary>
	public static int PositionAt(RecordRuntimeData record, int age) {
		var log = record.fitRelivePositions;
		return log != null && age >= 0 && age < log.Count ? log[age] : 0;
	}

	/// <summary>Whether the record's commercial connection is realized in this region this week.</summary>
	public static bool Exposed(RecordRuntimeData record, string regionId) =>
		!record.regionalData.TryGetValue(regionId, out RegionalRecordData data) || record.weeksSinceRelease >= data.fitExposureAge;

	/// <summary>§9x: whether the region is in the record's launch footprint (where the label's promotion reached at release).
	/// Every region when the switch is off.</summary>
	public static bool LaunchExposed(RecordRuntimeData record, string regionId) =>
		!Enabled || !record.regionalData.TryGetValue(regionId, out RegionalRecordData data) || data.fitExposureAge == 0;

	/// <summary>§9x: whether the label could not service the record everywhere at release (some region held at launch).
	/// A region exposed later by the spread rules is still read as held.</summary>
	public static bool HeldAtLaunch(RecordRuntimeData record) {
		if (!Enabled) return false;
		foreach (var data in record.regionalData.Values) if (data.fitExposureAge != 0) return true;
		return false;
	}

	/// <summary>At launch: mark every region outside the launch footprint unexposed. Live AI singles only.</summary>
	public static void Launch(RecordRuntimeData record, AILabel label, SimulatedArtist artist) {
		if (!Enabled || !SingleCommercialFit.Applies(record) || record.baseRecord.format == ReleaseFormat.Album || label == null) return;
		string recordId = record.baseRecord.recordId;
		string actHome = RegionalAudience.HometownAudience ? RegionalAudience.HomeRegionId(artist) : null;
		float reach = label.EffectiveNationalReachForRecord(recordId);
		float chance = LaunchBase + LaunchReachSlope * reach;
		foreach (var pair in record.regionalData) {
			bool home = pair.Key == label.homeRegion || pair.Key == actHome;
			bool exposed = home || PhiloxCounterRandom.Unit(BandLife.WorldSeed, recordId, pair.Key, 0, LaunchDecision) < chance;
			pair.Value.fitExposureAge = exposed ? 0 : NotExposed;
			pair.Value.fitExposureCause = exposed ? "launch" : "unexposed";
			if (exposed) launchExposed++; else launchHeld++;
		}
		if (++launches % 500 == 0) Print();
	}

	/// <summary>
	/// End of a settled week, after this week's local sales ranks and chart positions: schedule the spread, then restart
	/// the local clock in each region whose exposure lands next week. Connected records only, so a flop's clock is never
	/// touched. Reads settled state only; no draws.
	/// </summary>
	public static void UpdateWeek(IEnumerable<RecordRuntimeData> records, Func<string, AILabel> labelOf, Func<string, string, bool> sustainedSupport = null) {
		if (!Enabled) return;
		foreach (var record in records) {
			if (record.baseRecord.format == ReleaseFormat.Album || !SingleCommercialFit.Applies(record)) continue;
			int age = record.weeksSinceRelease;
			bool pending = false;
			foreach (var data in record.regionalData.Values) pending |= data.fitExposureAge > age;
			if (!pending || !SingleCommercialFit.Connected(record, labelOf(record.baseRecord.labelId))) continue;
			if (record.currentPosition >= 1 && record.currentPosition <= NationalRank)
				foreach (var data in record.regionalData.Values) Schedule(data, age + NationalLag, "national-chart");
			foreach (var pair in record.regionalData) {
				RegionalRecordData source = pair.Value;
				bool station = StationSupport && sustainedSupport?.Invoke(record.baseRecord.recordId, pair.Key) == true;
				if (station) Schedule(source, age + 1, "station-sustained");
				if (source.fitExposureAge > age) continue;
				bool sales = source.localSalesRank >= 1 && source.localSalesRank <= NeighbourRank;
				if (!sales && !station) continue;
				foreach (string neighbourId in ChartManager.GetNeighborRegionIds(pair.Key))
					if (record.regionalData.TryGetValue(neighbourId, out RegionalRecordData neighbour))
						Schedule(neighbour, age + NeighbourLag, sales ? "neighbour-sales" : "neighbour-station");
			}
			foreach (var data in record.regionalData.Values)
				if (data.fitExposureAge == age + 1 && data.fitExposureAge > 1) data.localIntroAge = data.fitExposureAge;
		}
	}

	private static void Schedule(RegionalRecordData data, int exposureAge, string cause) {
		if (data.fitExposureAge <= exposureAge) return;
		if (data.fitExposureAge == NotExposed) {
			if (cause == "national-chart") spreadNational++;
			else if (cause == "station-sustained" || cause == "neighbour-station") spreadStation++;
			else spreadNeighbour++;
			if ((spreadNational + spreadNeighbour + spreadStation) % 250 == 0) Print();
		}
		data.fitExposureAge = exposureAge;
		data.fitExposureCause = cause;
	}

	// Console diagnostics only (no CSV reads them).
	private static int launches,launchExposed, launchHeld, spreadNational, spreadNeighbour, spreadStation;
	private static void Print() =>
		Godot.GD.Print($"REGIONAL_FIT_EXPOSURE launches={launches} launchExposed={launchExposed} launchHeld={launchHeld} " +
			$"spreadNeighbour={spreadNeighbour} spreadNational={spreadNational} spreadStation={spreadStation}");
}
