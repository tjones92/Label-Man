using Godot;

public enum RadioBand { AM, FM }

public enum StationFormat {
	Top40,          // the mass driver
	RnB,            // "Black radio"
	Country,
	MOR,            // middle-of-the-road / adult standards
	FullService,    // early-60s general / personality era (broad genre admittance)
	UndergroundFM,  // 1967+ album cuts, high autonomy
	Gospel,
	Jazz
}

/// <summary>
/// An authored reporter station (design doc b). ~63 of these exist across the regions -- they ARE
/// the survey panel on the chart side. Authored/generated attributes live here; all mutable
/// per-station simulation state lives on <see cref="StationRuntime"/> (the field <c>rt</c>).
/// </summary>
[GlobalClass]
public partial class RadioStation : Resource {
	[ExportGroup("Identity")]
	[Export] public string stationId;
	[Export] public string callsign;
	[Export] public string cityName;
	[Export] public string regionId;
	[Export] public bool latinLeaning;   // Southwest flavor flag; not a separate format

	[ExportGroup("Signal")]
	[Export] public RadioBand band = RadioBand.AM;
	[Export] public int wattage = 5000;
	[Export] public bool clearChannel;
	// This station's slice of its region's TOTAL panel weight (reporters + tail sum to 1).
	[Export(PropertyHint.Range, "0,1")] public float regionReachShare;

	[ExportGroup("Format & Programming")]
	[Export] public StationFormat format = StationFormat.Top40;
	[Export] public int highSlots = 8;
	[Export] public int midSlots = 12;
	[Export] public int lightSlots = 15;
	[Export(PropertyHint.Range, "0,1")] public float djAutonomy = 0.5f;
	[Export(PropertyHint.Range, "0,1")] public float payolaSusceptibility = 0.3f;
	[Export(PropertyHint.Range, "0,1")] public float integrityRisk = 0.5f;
	[Export] public string leadDjId;

	// Runtime state - never authored, built at network construction.
	public StationRuntime rt;

	public RadioStation() {}

	/// <summary>Reporter reach into its region, after reputation. Read by the aggregation in the
	/// reporter term (Phase 2); regionReachShare is the base slice, reputation modifies it.</summary>
	public float EffectiveReach() => regionReachShare * (rt?.reachModifier ?? 1f);
}
