using System;
using System.Collections.Generic;
using System.Linq;

public enum TvNetwork { Local, ABC, CBS, NBC, Syndicated }
public enum TvProgramKind {
	LocalTeenDance, NationalTeen, PrimetimeVariety,
	/// <summary>Late-night and daytime talk: a host, a couch, a singer and a comic most nights.</summary>
	TalkShow,
	/// <summary>A specialist music hour: country, rhythm and soul, or folk, booked for its own audience.</summary>
	MusicShow,
}

/// <summary>Which part of a bill a guest fills (§5.2 bill balance).</summary>
public enum BillLane {
	/// <summary>A current record off this week's evidence: the chart, a region's report, the discovery slot.</summary>
	Chart,
	/// <summary>An act the public already knows, booked on standing fame with or without a current single.</summary>
	Standing,
	/// <summary>A comedian who makes records.</summary>
	Comedy,
}

/// <summary>
/// How a program fills its bill from a date. Chart slots read this week's records, standing slots the recording acts the
/// audience already knows, comedy slots the comedians who make records. The rest of a real variety bill (Broadway casts,
/// opera and concert artists, dancers, circus and novelty acts) are not record-business guests and are not modelled.
/// </summary>
public sealed class BillPlan {
	public GameDate From;
	public int Chart, Standing, Comedy;
	public int Total => Chart + Standing + Comedy;
}
/// <summary>How the music is made on camera (§3, §6). Independent of how the picture reaches viewers.</summary>
public enum PerformanceMethod { Mime, LiveVocalOverTrack, LiveBandAndVocal, HouseOrchestra }
/// <summary>How the picture reaches viewers (§3). A taped show can still carry a live musical performance.</summary>
public enum BroadcastMethod { Live, Taped }
/// <summary>How firmly a catalog fact is known. Approximate and Fictional facts print as such to the player.</summary>
public enum SourceConfidence { Documented, Approximate, Fictional }
/// <summary>Who carries an airing where, and at what local hour (§4.4).</summary>
public enum CarriageRule {
	/// <summary>Only the home city's station.</summary>
	LocalOnly,
	/// <summary>Network daytime: one simultaneous live feed, so the local clock hour slides earlier going west.</summary>
	NetworkSimultaneous,
	/// <summary>Network primetime: East and West at the same clock hour (the West on a delayed tape); Central and Mountain an hour earlier.</summary>
	NetworkPrimetime,
	/// <summary>A tape mailed station to station: its own date and local hour in each market.</summary>
	Syndicated,
}
public enum TimeZoneBand { Eastern, Central, Mountain, Pacific }

/// <summary>One effective-dated schedule for a program (§4.1). Dates are inclusive; outside them the program is not on.</summary>
public sealed class BroadcastScheduleVersion {
	public GameDate First, Last;
	public DayOfWeek[] Days;
	/// <summary>Start in the clock of the origin feed: Eastern for networks, local for a local or syndicated show.</summary>
	public int StartMinute;
	public int DurationMinutes;
	public CarriageRule Carriage;
	public BroadcastMethod Broadcast;
	public string ProductionCityId;
	/// <summary>Months with no original episodes; the slot carries repeats of earlier appearances instead.</summary>
	public int[] RepeatMonths = Array.Empty<int>();
	public SourceConfidence Confidence;
	public string Note;
	/// <summary>Weeks between episodes: 1 weekly, 4 for a show that became a monthly special. Counted from <see cref="First"/>.</summary>
	public int IntervalWeeks = 1;
	public bool Covers(GameDate date) => date >= First && date <= Last && Days.Contains(date.DayOfWeek)
		&& (IntervalWeeks <= 1 || (BroadcastExposure.DayIndex(date) - BroadcastExposure.DayIndex(First)) / 7 % IntervalWeeks == 0);
}

/// <summary>A market a syndicated tape reaches, from a date, a number of weeks behind the origin, at its own local hour.</summary>
public sealed class SyndicationMarket {
	public string CityId;
	public GameDate From;
	public int DelayWeeks;
	public DayOfWeek Day;
	public int LocalStartMinute;
}

public sealed class TvProgram {
	public string Id, Name, HostName, Station;
	public TvProgramKind Kind;
	public TvNetwork Network;
	/// <summary>The city whose station makes the show (local and syndicated programs); null for a network show.</summary>
	public string HomeCityId;
	public PerformanceMethod Method;
	public string MethodNote;
	/// <summary>How strongly the program pulls each viewer group against background competition (§4.2). Not a rating.</summary>
	public float[] Attraction = new float[4];
	public int GuestSlots;
	/// <summary>One slot the producer fills from records below the top of the chart (§5.2 discovery allocation).</summary>
	public bool DiscoverySlot;
	/// <summary>Weeks before the same act can be booked again.</summary>
	public int RebookWeeks;
	/// <summary>Best chart position a national booker will consider for an ordinary slot; 0 = any record on sale.</summary>
	public int ChartCeiling;
	public Func<Genre, float> GenreFit;
	public List<BroadcastScheduleVersion> Schedule = new();
	public List<SyndicationMarket> Syndication = new();
	public SourceConfidence Confidence;
	public string Source;
	/// <summary>Dated bill plans, oldest first. None: every guest slot is a chart slot (the teen and dance shows).</summary>
	public List<BillPlan> Bills = new();
	/// <summary>The public recognition a standing guest needs before this booker calls (§5.2: national shows read broad response).</summary>
	public float StandingFloor = 0.20f;
	/// <summary>The same for a comedian. A comic's name is made in clubs and on television more than on records, so the
	/// recognition the game tracks (record-driven) runs low for them.</summary>
	public float ComedyFloor = 0.06f;
	/// <summary>Books from the national pool even though it is made by one station (a nationally syndicated talk show).</summary>
	public bool NationalBooking;
	/// <summary>Share of stations carrying a nationally syndicated program (initial design value; no network clears it).</summary>
	public float SyndicatedClearance = 0.5f;
	public bool BooksNationally => NationalBooking || Network != TvNetwork.Local;
	/// <summary>Announces a headliner weeks ahead and builds an event night around it (§4.5 advertised guest).</summary>
	public bool AnnouncesHeadliners;

	public BillPlan BillOn(GameDate date) => Bills.LastOrDefault(b => b.From <= date) ?? new BillPlan { From = date, Chart = GuestSlots };
	public int SlotsOn(GameDate date) => BillOn(date).Total;

	public BroadcastScheduleVersion VersionOn(GameDate date) => Schedule.FirstOrDefault(v => v.Covers(date));
	public bool OnAir(GameDate date) => Schedule.Any(v => date >= v.First && date <= v.Last);
	public bool Runs(GameDate date) => VersionOn(date) != null;
}

public enum PressFamily { Trade, TeenFan, Mainstream, RockCriticism, LocalDaily, Specialist }

public enum PressCadence { Daily, Weekly, Monthly }

/// <summary>A publication with its real opening date (§8), its issue rhythm, print lead time and a finite music budget.</summary>
public sealed class PressOutlet {
	public string Id, Name;
	public PressFamily Family;
	public GameDate From;
	public GameDate Until = GameDate.EndDate.AddDays(3650);
	public string Note;
	public SourceConfidence Confidence;
	public bool Publishing(GameDate date) => date >= From && date <= Until;
	public PressCadence Cadence = PressCadence.Weekly;
	/// <summary>Cover date of a weekly. A monthly is on the stands the first of its month; a daily's entertainment page runs every day.</summary>
	public DayOfWeek IssueDay = DayOfWeek.Saturday;
	/// <summary>Days between the editor's decision and the issue: a monthly's feature can reach the stands after the campaign peaked.</summary>
	public int LeadDays = 4;
	/// <summary>Music items an issue can carry (a daily: per week). Finite page space is the editor's real constraint (§8).</summary>
	public int Slots = 4;
	/// <summary>A city paper's home; null for a national publication.</summary>
	public string HomeCityId;
	/// <summary>The audience the outlet speaks for (§8: specialist influence is real inside its audience).</summary>
	public Func<Genre, float> Fit = _ => 1f;
	/// <summary>Readers in millions, pass-along included (initial design values, not audited circulation).</summary>
	public float ReadersMillions = 0.1f;
	public bool National => HomeCityId == null;
}

/// <summary>
/// Historical television and press institutions (directive §3, §8). The calendar controls institutions; simulated
/// acts supply the guests, so no real performer appears. Verified examples sit only inside their verified dates and
/// cities; everything else is a labelled approximation or a fictional local program. Fictional local shows are keyed
/// on the world seed, so two worlds differ and one world never changes on reload.
/// </summary>
public static partial class MediaOutletCatalog {
	public const int ViewerGroups = 4; // Child, Teen, YoungAdult, Adult — see BroadcastExposure.ViewerGroup

	private static readonly Dictionary<string, TimeZoneBand> zones = new(StringComparer.Ordinal) {
		["new_york"] = TimeZoneBand.Eastern, ["boston"] = TimeZoneBand.Eastern, ["philadelphia"] = TimeZoneBand.Eastern,
		["baltimore"] = TimeZoneBand.Eastern, ["washington"] = TimeZoneBand.Eastern, ["pittsburgh"] = TimeZoneBand.Eastern,
		["detroit"] = TimeZoneBand.Eastern, ["cleveland"] = TimeZoneBand.Eastern, ["cincinnati"] = TimeZoneBand.Eastern,
		["atlanta"] = TimeZoneBand.Eastern, ["miami"] = TimeZoneBand.Eastern,
		["chicago"] = TimeZoneBand.Central, ["minneapolis"] = TimeZoneBand.Central, ["st_louis"] = TimeZoneBand.Central,
		["kansas_city"] = TimeZoneBand.Central, ["omaha"] = TimeZoneBand.Central, ["nashville"] = TimeZoneBand.Central,
		["memphis"] = TimeZoneBand.Central, ["new_orleans"] = TimeZoneBand.Central, ["dallas"] = TimeZoneBand.Central,
		["houston"] = TimeZoneBand.Central, ["san_antonio"] = TimeZoneBand.Central,
		["phoenix"] = TimeZoneBand.Mountain, ["albuquerque"] = TimeZoneBand.Mountain, ["denver"] = TimeZoneBand.Mountain,
		["salt_lake_city"] = TimeZoneBand.Mountain, ["billings"] = TimeZoneBand.Mountain,
		["los_angeles"] = TimeZoneBand.Pacific, ["san_francisco"] = TimeZoneBand.Pacific, ["seattle"] = TimeZoneBand.Pacific,
		["portland"] = TimeZoneBand.Pacific,
	};

	/// <summary>Approximate 1960 metropolitan population in millions: the reach of one city's own stations (initial design values).</summary>
	private static readonly Dictionary<string, float> metro1960 = new(StringComparer.Ordinal) {
		["new_york"] = 14.0f, ["boston"] = 3.0f, ["philadelphia"] = 4.3f, ["baltimore"] = 1.8f, ["washington"] = 2.0f,
		["pittsburgh"] = 2.4f, ["chicago"] = 6.8f, ["detroit"] = 3.8f, ["cleveland"] = 2.0f, ["cincinnati"] = 1.3f,
		["minneapolis"] = 1.5f, ["st_louis"] = 2.1f, ["kansas_city"] = 1.1f, ["omaha"] = 0.46f, ["nashville"] = 0.40f,
		["memphis"] = 0.63f, ["atlanta"] = 1.0f, ["new_orleans"] = 0.87f, ["miami"] = 0.93f, ["dallas"] = 1.1f,
		["houston"] = 1.2f, ["san_antonio"] = 0.69f, ["phoenix"] = 0.66f, ["albuquerque"] = 0.26f, ["denver"] = 0.93f,
		["salt_lake_city"] = 0.45f, ["billings"] = 0.08f, ["los_angeles"] = 6.7f, ["san_francisco"] = 2.6f,
		["seattle"] = 1.1f, ["portland"] = 0.82f,
	};
	private static readonly HashSet<string> sunbelt = new(StringComparer.Ordinal) {
		"phoenix", "los_angeles", "houston", "dallas", "atlanta", "denver", "san_antonio", "miami", "albuquerque", "san_francisco", "seattle" };

	public static TimeZoneBand ZoneOf(string cityId) => cityId != null && zones.TryGetValue(cityId, out var z) ? z : TimeZoneBand.Eastern;
	public static IEnumerable<string> CityIds => metro1960.Keys;
	public static float MetroPopulationMillions(string cityId, int year) =>
		cityId != null && metro1960.TryGetValue(cityId, out float p) ? p * (1f + (sunbelt.Contains(cityId) ? 0.03f : 0.013f) * Math.Max(0, year - 1960)) : 0f;

	private static List<TvProgram> programs;
	private static List<PressOutlet> press;
	private static ulong builtForSeed;

	public static IReadOnlyList<TvProgram> Programs { get { Ensure(); return programs; } }
	public static IReadOnlyList<PressOutlet> Press { get { Ensure(); return press; } }
	public static TvProgram Get(string id) => Programs.FirstOrDefault(p => p.Id == id);
	public static IEnumerable<TvProgram> LocalProgramsIn(string cityId) =>
		Programs.Where(p => p.HomeCityId == cityId || p.Syndication.Any(m => m.CityId == cityId));

	private static void Ensure() {
		ulong seed = BandLife.WorldSeed;
		if (programs != null && builtForSeed == seed) return;
		builtForSeed = seed;
		programs = new List<TvProgram>();
		AddNational();
		// The original national hours stage advertised headline nights (§4.5); the singers' hours, talk shows and
		// specialist programs added later book their bills without an event build-up.
		foreach (TvProgram p in programs) p.AnnouncesHeadliners = true;
		AddHistoricalLocals();
		AddVarietyAndTalk();
		AddSpecialistAndLocalMusic();
		AddFictionalLocals();
		press = BuildPress();
	}

	private static GameDate D(int y, int m, int d) => new(y, m, d);
	private static readonly DayOfWeek[] Weekdays = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
	private static readonly DayOfWeek[] MonSat = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday };
	private static int Clock(int h, int m = 0) => h * 60 + m;
	private static readonly GameDate Horizon = D(1970, 12, 31);

	// ---- genre fit: an audience emphasis, not a gate (§3.2) -------------------------------------------------

	public static float TeenFit(Genre g) => g switch {
		Genre.TeenPop or Genre.Bubblegum or Genre.BritishInvasion or Genre.BritishBeat or Genre.GirlGroup => 1.0f,
		Genre.RockAndRoll or Genre.DooWop or Genre.Motown or Genre.SurfRock or Genre.SunshinePop or Genre.PopRock => 0.9f,
		Genre.Soul or Genre.GarageRock or Genre.BritishPop => 0.8f,
		Genre.FolkRock or Genre.Boogaloo or Genre.Funk or Genre.RnB => 0.65f,
		Genre.Psychedelic or Genre.PsychedelicRock or Genre.BaroquePop or Genre.SkaRocksteady or Genre.Ska or Genre.LatinPop => 0.5f,
		Genre.Folk or Genre.ContemporaryFolk or Genre.CountryRock or Genre.TexMex or Genre.BluesRock or Genre.HardRock => 0.4f,
		Genre.Country or Genre.TraditionalPop or Genre.Comedy or Genre.BossaNova => 0.25f,
		_ => 0.15f,
	};
	public static float FamilyVarietyFit(Genre g) => g switch {
		Genre.TraditionalPop or Genre.Comedy or Genre.BritishInvasion or Genre.BritishBeat => 1.0f,
		Genre.EasyListening or Genre.Motown or Genre.TeenPop or Genre.GirlGroup or Genre.Folk or Genre.BossaNova => 0.85f,
		Genre.Country or Genre.Soul or Genre.SunshinePop or Genre.PopRock or Genre.DooWop or Genre.LatinPop => 0.7f,
		Genre.RockAndRoll or Genre.Gospel or Genre.Jazz or Genre.FolkRock or Genre.BritishPop or Genre.Childrens => 0.6f,
		Genre.Classical or Genre.Psychedelic or Genre.BaroquePop or Genre.RnB or Genre.Bubblegum => 0.45f,
		_ => 0.3f,
	};
	public static float TopicalVarietyFit(Genre g) => g switch {
		Genre.Folk or Genre.FolkRock or Genre.ContemporaryFolk or Genre.SingerSongwriter or Genre.Psychedelic or Genre.PsychedelicRock => 1.0f,
		Genre.Comedy or Genre.BluesRock or Genre.Soul or Genre.SunshinePop or Genre.BaroquePop or Genre.PopRock => 0.8f,
		Genre.AcidRock or Genre.HardRock or Genre.BritishInvasion or Genre.Motown or Genre.CountryRock or Genre.Funk => 0.65f,
		Genre.TraditionalPop or Genre.Country or Genre.Jazz or Genre.Gospel => 0.45f,
		_ => 0.3f,
	};

	// ---- national programs -----------------------------------------------------------------------------------

	private static void AddNational() {
		var bandstand = new TvProgram {
			Id = "american_bandstand", Name = "American Bandstand", HostName = "Dick Clark", Network = TvNetwork.ABC,
			Kind = TvProgramKind.NationalTeen, Method = PerformanceMethod.Mime,
			MethodNote = "Guests lip-sync to the record; the show's normal requirement, stated before you accept.",
			Attraction = new[] { 0.25f, 1.20f, 0.30f, 0.08f }, GuestSlots = 2, DiscoverySlot = true, RebookWeeks = 10, ChartCeiling = 60,
			GenreFit = TeenFit, Confidence = SourceConfidence.Documented,
			Source = "Paley T86:0317; 1962 cut 90→60 min; weekday run ended Aug 30 1963; Saturday from Sep 7 1963; production to Los Angeles Feb 8 1964 (pophistorydig, mcrfb trade archive).",
		};
		bandstand.Schedule.Add(new BroadcastScheduleVersion { First = D(1957, 8, 5), Last = D(1962, 9, 28), Days = Weekdays, StartMinute = Clock(16), DurationMinutes = 90,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Live, ProductionCityId = "philadelphia",
			Confidence = SourceConfidence.Approximate, Note = "Weekday afternoons from Philadelphia, live; exact Eastern start approximate." });
		bandstand.Schedule.Add(new BroadcastScheduleVersion { First = D(1962, 10, 1), Last = D(1963, 8, 30), Days = Weekdays, StartMinute = Clock(16), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Live, ProductionCityId = "philadelphia",
			Confidence = SourceConfidence.Approximate, Note = "Cut to an hour in 1962." });
		bandstand.Schedule.Add(new BroadcastScheduleVersion { First = D(1963, 9, 7), Last = D(1964, 2, 7), Days = new[] { DayOfWeek.Saturday }, StartMinute = Clock(13, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Taped, ProductionCityId = "philadelphia",
			Confidence = SourceConfidence.Documented, Note = "Saturdays only, 1:30-2:30 ET, taped." });
		bandstand.Schedule.Add(new BroadcastScheduleVersion { First = D(1964, 2, 8), Last = Horizon, Days = new[] { DayOfWeek.Saturday }, StartMinute = Clock(13, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles",
			Confidence = SourceConfidence.Approximate, Note = "From Los Angeles; affiliates set their own Saturday hour." });
		programs.Add(bandstand);

		var action = new TvProgram {
			Id = "where_the_action_is", Name = "Where the Action Is", HostName = "Linda Scott and Steve Alaimo", Network = TvNetwork.ABC,
			Kind = TvProgramKind.NationalTeen, Method = PerformanceMethod.Mime, MethodNote = "Location segments mimed to the record.",
			Attraction = new[] { 0.20f, 1.00f, 0.25f, 0.06f }, GuestSlots = 2, DiscoverySlot = true, RebookWeeks = 8, ChartCeiling = 70,
			GenreFit = TeenFit, Confidence = SourceConfidence.Documented, Source = "ABC Jun 28 1965 – Mar 31 1967; 2 p.m. ET summer, 4:30 p.m. ET from fall 1965.",
		};
		action.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 6, 28), Last = D(1965, 9, 3), Days = Weekdays, StartMinute = Clock(14), DurationMinutes = 30,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Documented });
		action.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 9, 6), Last = D(1967, 3, 31), Days = Weekdays, StartMinute = Clock(16, 30), DurationMinutes = 30,
			Carriage = CarriageRule.NetworkSimultaneous, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Documented });
		programs.Add(action);

		var shindig = new TvProgram {
			Id = "shindig", Name = "Shindig!", Network = TvNetwork.ABC, Kind = TvProgramKind.NationalTeen,
			Method = PerformanceMethod.LiveVocalOverTrack, MethodNote = "Live vocals over a prepared backing, with the house band and episode arrangements; not every booking is a mime.",
			Attraction = new[] { 0.30f, 1.30f, 0.45f, 0.15f }, GuestSlots = 4, DiscoverySlot = true, RebookWeeks = 8, ChartCeiling = 50,
			GenreFit = TeenFit, Confidence = SourceConfidence.Documented, Source = "ABC Sep 16 1964 – Jan 8 1966; hour from Jan 1965; Thu/Sat 7:30 halves fall 1965; Television Academy oral histories.",
		};
		shindig.Schedule.Add(new BroadcastScheduleVersion { First = D(1964, 9, 16), Last = D(1965, 1, 6), Days = new[] { DayOfWeek.Wednesday }, StartMinute = Clock(20, 30), DurationMinutes = 30,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Approximate, Note = "Wednesday half hour; the Eastern start is approximate." });
		shindig.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 1, 13), Last = D(1965, 9, 8), Days = new[] { DayOfWeek.Wednesday }, StartMinute = Clock(20, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", RepeatMonths = new[] { 7, 8 },
			Confidence = SourceConfidence.Approximate, Note = "Expanded to an hour in January 1965." });
		shindig.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 9, 16), Last = D(1966, 1, 8), Days = new[] { DayOfWeek.Thursday, DayOfWeek.Saturday }, StartMinute = Clock(19, 30), DurationMinutes = 30,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Documented });
		programs.Add(shindig);

		var hullabaloo = new TvProgram {
			Id = "hullabaloo", Name = "Hullabaloo", Network = TvNetwork.NBC, Kind = TvProgramKind.NationalTeen,
			Method = PerformanceMethod.LiveVocalOverTrack, MethodNote = "Produced numbers with dancers and hosts; live, track or mime is set per production.",
			Attraction = new[] { 0.30f, 1.20f, 0.45f, 0.18f }, GuestSlots = 4, DiscoverySlot = false, RebookWeeks = 8, ChartCeiling = 40,
			GenreFit = TeenFit, Confidence = SourceConfidence.Approximate, Source = "NBC Jan 12 1965 – Aug 29 1966; Paley T80:0076 lists 7:30 p.m. on Apr 11 1966.",
		};
		hullabaloo.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 1, 12), Last = D(1965, 9, 7), Days = new[] { DayOfWeek.Tuesday }, StartMinute = Clock(20, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "new_york", RepeatMonths = new[] { 7, 8 }, Confidence = SourceConfidence.Approximate });
		hullabaloo.Schedule.Add(new BroadcastScheduleVersion { First = D(1965, 9, 13), Last = D(1966, 8, 29), Days = new[] { DayOfWeek.Monday }, StartMinute = Clock(19, 30), DurationMinutes = 30,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "new_york", RepeatMonths = new[] { 6, 7, 8 }, Confidence = SourceConfidence.Documented });
		programs.Add(hullabaloo);

		var sullivan = new TvProgram {
			Id = "ed_sullivan", Name = "The Ed Sullivan Show", HostName = "Ed Sullivan", Network = TvNetwork.CBS, Kind = TvProgramKind.PrimetimeVariety,
			Method = PerformanceMethod.LiveVocalOverTrack, MethodNote = "Live from New York before a studio audience; the act sings live, often over a prepared track or with the house orchestra; standards department reviews lyrics.",
			Attraction = new[] { 0.70f, 0.95f, 0.85f, 1.10f }, GuestSlots = 2, DiscoverySlot = false, RebookWeeks = 6, ChartCeiling = 25,
			GenreFit = FamilyVarietyFit, Confidence = SourceConfidence.Documented, Source = "CBS Sunday 8 p.m. throughout the decade (edsullivan.com archive).",
		};
		sullivan.Schedule.Add(new BroadcastScheduleVersion { First = D(1948, 6, 20), Last = Horizon, Days = new[] { DayOfWeek.Sunday }, StartMinute = Clock(20), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Live, ProductionCityId = "new_york", RepeatMonths = new[] { 7, 8 },
			Confidence = SourceConfidence.Documented, Note = "Live in the East; the West sees it on a delayed tape at the same clock hour." });
		// CTVA episode guides 1959-70 (SimTools/MediaTvHeadlinerBookingDesign.md): about four musical guests an episode, of
		// whom 0.6 (1960-63) to 1.0 (1964-69) had a current hit; a quarter to a third were Broadway, opera and concert
		// artists; the rest were recording stars booked on their names. One or two comedians most weeks.
		sullivan.Bills.Add(new BillPlan { From = D(1948, 6, 20), Chart = 1, Standing = 2, Comedy = 1 });
		sullivan.Bills.Add(new BillPlan { From = D(1964, 1, 1), Chart = 2, Standing = 2, Comedy = 1 });
		programs.Add(sullivan);

		var palace = new TvProgram {
			Id = "hollywood_palace", Name = "The Hollywood Palace", HostName = "a rotating guest host", Network = TvNetwork.ABC, Kind = TvProgramKind.PrimetimeVariety,
			Method = PerformanceMethod.HouseOrchestra, MethodNote = "A polished variety bill with the house orchestra; substantial production rehearsal.",
			Attraction = new[] { 0.35f, 0.60f, 0.75f, 1.00f }, GuestSlots = 2, DiscoverySlot = false, RebookWeeks = 8, ChartCeiling = 30,
			GenreFit = FamilyVarietyFit, Confidence = SourceConfidence.Documented, Source = "ABC Jan 4 1964 – Feb 7 1970, Saturday 9:30; Tuesdays Sep 1967 – Jan 1968 (Paley T80:0233).",
		};
		palace.Schedule.Add(new BroadcastScheduleVersion { First = D(1964, 1, 4), Last = D(1967, 8, 31), Days = new[] { DayOfWeek.Saturday }, StartMinute = Clock(21, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", RepeatMonths = new[] { 7, 8 }, Confidence = SourceConfidence.Documented });
		palace.Schedule.Add(new BroadcastScheduleVersion { First = D(1967, 9, 1), Last = D(1968, 1, 31), Days = new[] { DayOfWeek.Tuesday }, StartMinute = Clock(21, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Approximate, Note = "Tuesday run; hour approximate." });
		palace.Schedule.Add(new BroadcastScheduleVersion { First = D(1968, 2, 1), Last = D(1970, 2, 7), Days = new[] { DayOfWeek.Saturday }, StartMinute = Clock(21, 30), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", RepeatMonths = new[] { 7, 8 }, Confidence = SourceConfidence.Documented });
		palace.Bills.Add(new BillPlan { From = D(1964, 1, 4), Chart = 1, Standing = 2, Comedy = 1 });
		programs.Add(palace);

		var smothers = new TvProgram {
			Id = "smothers_brothers", Name = "The Smothers Brothers Comedy Hour", HostName = "Tom and Dick Smothers", Network = TvNetwork.CBS, Kind = TvProgramKind.PrimetimeVariety,
			Method = PerformanceMethod.LiveBandAndVocal, MethodNote = "Taped before broadcast; a real live performance can still be retaken, edited or removed by network standards.",
			Attraction = new[] { 0.40f, 1.00f, 1.00f, 0.70f }, GuestSlots = 2, DiscoverySlot = false, RebookWeeks = 10, ChartCeiling = 40,
			GenreFit = TopicalVarietyFit, Confidence = SourceConfidence.Documented, Source = "CBS Feb 5 1967 – Apr 1969, Sunday 9 p.m. (Television Academy interviews).",
		};
		smothers.Schedule.Add(new BroadcastScheduleVersion { First = D(1967, 2, 5), Last = D(1969, 4, 6), Days = new[] { DayOfWeek.Sunday }, StartMinute = Clock(21), DurationMinutes = 60,
			Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", RepeatMonths = new[] { 6, 7, 8 }, Confidence = SourceConfidence.Documented });
		programs.Add(smothers);
	}

	// ---- verified local and syndicated programs --------------------------------------------------------------

	private static void AddHistoricalLocals() {
		var deane = new TvProgram {
			Id = "buddy_deane", Name = "The Buddy Deane Show", HostName = "Buddy Deane", Station = "Channel 13", HomeCityId = "baltimore",
			Network = TvNetwork.Local, Kind = TvProgramKind.LocalTeenDance, Method = PerformanceMethod.Mime, MethodNote = "Guests mime to the record for the dancers.",
			Attraction = new[] { 0.30f, 1.40f, 0.25f, 0.06f }, GuestSlots = 1, DiscoverySlot = true, RebookWeeks = 4, GenreFit = TeenFit,
			Confidence = SourceConfidence.Documented, Source = "WJZ-TV Baltimore, six afternoons a week, 1957 – early 1964; hour approximate.",
		};
		deane.Schedule.Add(new BroadcastScheduleVersion { First = D(1957, 9, 1), Last = D(1964, 1, 4), Days = MonSat, StartMinute = Clock(15, 30), DurationMinutes = 90,
			Carriage = CarriageRule.LocalOnly, Broadcast = BroadcastMethod.Live, ProductionCityId = "baltimore", Confidence = SourceConfidence.Approximate });
		programs.Add(deane);

		var thaxton = new TvProgram {
			Id = "lloyd_thaxton", Name = "The Lloyd Thaxton Show", HostName = "Lloyd Thaxton", Station = "Channel 13", HomeCityId = "los_angeles",
			Network = TvNetwork.Local, Kind = TvProgramKind.LocalTeenDance, Method = PerformanceMethod.Mime, MethodNote = "Mostly records and mimed guests, with the host's comic lip-sync bits.",
			Attraction = new[] { 0.30f, 1.20f, 0.25f, 0.06f }, GuestSlots = 1, DiscoverySlot = true, RebookWeeks = 4, GenreFit = TeenFit,
			Confidence = SourceConfidence.Approximate, Source = "KCOP Los Angeles from c. 1961; syndicated nationally from 1964 to about 1968 (LA Times via louielouie.net).",
		};
		thaxton.Schedule.Add(new BroadcastScheduleVersion { First = D(1961, 9, 1), Last = D(1968, 6, 28), Days = Weekdays, StartMinute = Clock(17), DurationMinutes = 60,
			Carriage = CarriageRule.LocalOnly, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles", Confidence = SourceConfidence.Approximate, Note = "Weekday evenings; hour approximate." });
		foreach ((string city, int month) in new[] { ("san_francisco", 1), ("seattle", 3), ("portland", 3), ("denver", 6), ("phoenix", 6), ("dallas", 9), ("chicago", 9) })
			thaxton.Syndication.Add(new SyndicationMarket { CityId = city, From = D(1964, month, 1), DelayWeeks = 2, Day = DayOfWeek.Saturday, LocalStartMinute = Clock(16) });
		programs.Add(thaxton);

		var upbeat = new TvProgram {
			Id = "upbeat", Name = "Upbeat", Station = "Channel 5", HomeCityId = "cleveland", Network = TvNetwork.Local, Kind = TvProgramKind.LocalTeenDance,
			Method = PerformanceMethod.Mime, MethodNote = "A multi-act music showcase; most guests mime, some sing live.",
			Attraction = new[] { 0.30f, 1.30f, 0.35f, 0.08f }, GuestSlots = 3, DiscoverySlot = true, RebookWeeks = 6, GenreFit = TeenFit,
			Confidence = SourceConfidence.Approximate,
			Source = "WEWS Cleveland: began locally as The Big 5 Show, Saturday 5 p.m.; tapes moved between markets over several weeks (News5 Cleveland retrospective). Syndication footprint is a composite.",
		};
		upbeat.Schedule.Add(new BroadcastScheduleVersion { First = D(1964, 6, 6), Last = Horizon, Days = new[] { DayOfWeek.Saturday }, StartMinute = Clock(17), DurationMinutes = 60,
			Carriage = CarriageRule.LocalOnly, Broadcast = BroadcastMethod.Taped, ProductionCityId = "cleveland", Confidence = SourceConfidence.Approximate,
			Note = "Called The Big 5 Show until 1966." });
		foreach ((string city, int year, int month, int delay) in new[] { ("pittsburgh", 1966, 3, 1), ("detroit", 1966, 3, 1), ("cincinnati", 1966, 6, 2),
			("chicago", 1966, 9, 2), ("buffalo", 1966, 9, 2), ("st_louis", 1967, 1, 3), ("atlanta", 1967, 1, 3), ("new_york", 1967, 6, 3),
			("philadelphia", 1967, 6, 3), ("houston", 1968, 1, 4), ("denver", 1968, 1, 4), ("los_angeles", 1968, 6, 4) }) {
			if (!metro1960.ContainsKey(city)) continue; // a market outside the game's cities is real but unserved here
			upbeat.Syndication.Add(new SyndicationMarket { CityId = city, From = D(year, month, 1), DelayWeeks = delay, Day = DayOfWeek.Saturday, LocalStartMinute = Clock(16, 30) });
		}
		programs.Add(upbeat);
	}

	/// <summary>Upbeat's name in a given year: it was The Big 5 Show before 1966 (§3.1).</summary>
	public static string DisplayName(TvProgram program, GameDate date) =>
		program.Id == "upbeat" && date.year < 1966 ? "The Big 5 Show" : program.Name;

	// ---- fictional local dance shows -------------------------------------------------------------------------

	private static readonly string[] LocalShowNames = { "Teen Time", "Dance Party", "Record Hop", "Bandstand Matinee", "Saturday Hop", "Teen Beat Club",
		"After School Hop", "The Platter Party", "Hi-Fi Club", "The Dance Line", "Spin Time", "Teen Town" };
	private static readonly string[] LocalHostFirst = { "Jerry", "Bob", "Dale", "Ray", "Hal", "Bill", "Jack", "Don", "Art", "Lee", "Ron", "Gene" };
	private static readonly string[] LocalHostLast = { "Kane", "Dawson", "Marsh", "Tully", "Brenner", "Crowell", "Fisk", "Hollis", "Lacey", "Mercer", "Pryor", "Stroud" };

	private static void AddFictionalLocals() {
		// Only a documented dance show displaces the fictional one; a country hour or a talk show in the same town doesn't.
		var documented = programs.Where(p => p.HomeCityId != null && p.Kind == TvProgramKind.LocalTeenDance).GroupBy(p => p.HomeCityId)
			.ToDictionary(g => g.Key, g => g.SelectMany(p => p.Schedule).ToList(), StringComparer.Ordinal);
		foreach (string city in metro1960.Keys.OrderBy(c => c, StringComparer.Ordinal)) {
			// A second fictional show runs in the two biggest markets, where several stations fought for the after-school crowd.
			int count = city == "new_york" || city == "chicago" ? 2 : 1;
			for (int i = 0; i < count; i++) {
				TvProgram show = FictionalLocal(city, i);
				// Baltimore, Los Angeles and Cleveland keep their documented shows: the fictional one only fills the
				// years around them (Cleveland before The Big 5 Show, Baltimore after Buddy Deane).
				if (i == 0 && documented.TryGetValue(city, out var real) && !ClipAround(show, real)) continue;
				programs.Add(show);
			}
		}
	}

	/// <summary>Trims a fictional show's run to the longest stretch the documented schedules leave free. False if none is left.</summary>
	private static bool ClipAround(TvProgram show, List<BroadcastScheduleVersion> real) {
		var version = show.Schedule[0];
		GameDate realFirst = real.Min(v => v.First), realLast = real.Max(v => v.Last);
		GameDate before = realFirst.AddDays(-1), after = realLast.AddDays(1);
		int daysBefore = before >= version.First ? BroadcastExposure.DayIndex(before) - BroadcastExposure.DayIndex(version.First) : -1;
		int daysAfter = version.Last >= after ? BroadcastExposure.DayIndex(version.Last) - BroadcastExposure.DayIndex(after) : -1;
		if (daysBefore < 0 && daysAfter < 0) return false;
		if (daysBefore >= daysAfter) version.Last = before; else version.First = after;
		return true;
	}

	private static TvProgram FictionalLocal(string city, int index) {
		string key = "local|" + city + "|" + index;
		float U(string purpose) => MediaAtmosphere.Unit(key + "|" + purpose);
		string name = LocalShowNames[(int)(U("name") * LocalShowNames.Length) % LocalShowNames.Length];
		string host = LocalHostFirst[(int)(U("host-first") * LocalHostFirst.Length) % LocalHostFirst.Length] + " " +
			LocalHostLast[(int)(U("host-last") * LocalHostLast.Length) % LocalHostLast.Length];
		int channel = new[] { 2, 4, 5, 7, 8, 9, 11, 13 }[(int)(U("channel") * 8) % 8];
		bool saturday = U("day") < 0.4f;
		int startYear = U("start") < 0.65f ? 1958 : 1960 + (int)(U("start-year") * 4);
		// Local dance shows thinned out in the later sixties as stations moved to news and syndication.
		int endYear = 1964 + (int)(U("end-year") * 6);
		var program = new TvProgram {
			Id = "local_" + city + (index == 0 ? "" : "_" + index), Name = (index == 0 ? "" : "Channel " + channel + "'s ") + name, HostName = host,
			Station = "Channel " + channel, HomeCityId = city, Network = TvNetwork.Local, Kind = TvProgramKind.LocalTeenDance,
			Method = PerformanceMethod.Mime, MethodNote = "Guests mime to the record for the studio dancers; a short chat with the host.",
			Attraction = new[] { 0.25f, 1.00f + 0.3f * U("pull"), 0.25f, 0.05f }, GuestSlots = saturday ? 2 : 1, DiscoverySlot = true, RebookWeeks = 4,
			GenreFit = TeenFit, Confidence = SourceConfidence.Fictional, Source = "Fictional local program (§3.1: use fictional local programs where research is incomplete).",
		};
		program.Schedule.Add(new BroadcastScheduleVersion {
			First = D(startYear, 1 + (int)(U("start-month") * 12) % 12, 1), Last = D(endYear, 1 + (int)(U("end-month") * 12) % 12, 1).AddDays(-1),
			Days = saturday ? new[] { DayOfWeek.Saturday } : Weekdays,
			StartMinute = saturday ? Clock(13 + (int)(U("hour") * 4)) : Clock(16) + (U("half") < 0.5f ? 0 : 30),
			DurationMinutes = saturday ? 60 : (U("length") < 0.5f ? 30 : 60),
			Carriage = CarriageRule.LocalOnly, Broadcast = BroadcastMethod.Live, ProductionCityId = city, Confidence = SourceConfidence.Fictional });
		return program;
	}

	// ---- press -------------------------------------------------------------------------------------------------

	// ---- press audiences: an emphasis, not a gate (§8) -------------------------------------------------------

	public static float SoulPressFit(Genre g) => g switch {
		Genre.RnB or Genre.Soul or Genre.Motown or Genre.Gospel or Genre.Funk or Genre.Blues => 1.0f,
		Genre.DooWop or Genre.Jazz or Genre.Boogaloo or Genre.GirlGroup => 0.8f,
		Genre.RockAndRoll or Genre.BluesRock => 0.45f,
		_ => 0.12f,
	};
	public static float CountryPressFit(Genre g) => g switch {
		Genre.Country => 1.0f, Genre.CountryRock or Genre.TexMex => 0.75f, Genre.Folk or Genre.Gospel => 0.35f, _ => 0.08f,
	};
	public static float JazzPressFit(Genre g) => g switch {
		Genre.Jazz => 1.0f, Genre.BossaNova or Genre.Blues => 0.7f, Genre.Soul or Genre.RnB or Genre.Funk => 0.4f, _ => 0.08f,
	};
	public static float FolkPressFit(Genre g) => g switch {
		Genre.Folk or Genre.ContemporaryFolk => 1.0f, Genre.FolkRock or Genre.SingerSongwriter => 0.8f, Genre.Country or Genre.Blues or Genre.Gospel => 0.35f, _ => 0.05f,
	};
	public static float CriticFit(Genre g) => g switch {
		Genre.FolkRock or Genre.PsychedelicRock or Genre.Psychedelic or Genre.AcidRock or Genre.BluesRock or Genre.BritishBlues or Genre.SingerSongwriter
			or Genre.BaroquePop or Genre.ProtoPunk or Genre.ProgressiveRock or Genre.HardRock => 1.0f,
		Genre.Soul or Genre.Folk or Genre.ContemporaryFolk or Genre.BritishBeat or Genre.Blues or Genre.CountryRock or Genre.GarageRock or Genre.Jazz => 0.7f,
		Genre.Motown or Genre.Funk or Genre.SunshinePop or Genre.PopRock or Genre.RockAndRoll or Genre.BritishPop => 0.45f,
		_ => 0.15f,
	};

	private static readonly string[] Mastheads = { "Herald", "Press", "Courier", "Evening Star", "Times-Union", "Journal", "Daily News", "Register", "Gazette", "Post" };

	private static List<PressOutlet> BuildPress() {
		var list = new List<PressOutlet> {
			new() { Id = "billboard", Name = "Billboard", Family = PressFamily.Trade, From = D(1894, 11, 1), Confidence = SourceConfidence.Documented,
				Note = "Breadth of industry and radio reporting.", IssueDay = DayOfWeek.Monday, LeadDays = 5, Slots = 9, ReadersMillions = 0.05f },
			new() { Id = "cash_box", Name = "Cash Box", Family = PressFamily.Trade, From = D(1942, 7, 1), Confidence = SourceConfidence.Documented,
				Note = "Operator- and retail-facing.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 8, ReadersMillions = 0.03f },
			new() { Id = "music_vendor", Name = "Music Vendor", Family = PressFamily.Trade, From = D(1946, 1, 1), Until = D(1964, 9, 30), Confidence = SourceConfidence.Approximate,
				Note = "Renamed Record World in 1964; a Sep 26 1964 issue still carries the old masthead.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 6, ReadersMillions = 0.02f },
			new() { Id = "record_world", Name = "Record World", Family = PressFamily.Trade, From = D(1964, 10, 1), Confidence = SourceConfidence.Approximate,
				Note = "Competing reviews and trade coverage.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 7, ReadersMillions = 0.02f },
			new() { Id = "sixteen", Name = "16", Family = PressFamily.TeenFan, From = D(1957, 1, 1), Confidence = SourceConfidence.Documented,
				Note = "Reader mail decides which faces it prints.", Cadence = PressCadence.Monthly, LeadDays = 42, Slots = 6, Fit = TeenFit, ReadersMillions = 1.5f },
			new() { Id = "tiger_beat", Name = "Tiger Beat", Family = PressFamily.TeenFan, From = D(1965, 9, 1), Confidence = SourceConfidence.Documented,
				Note = "First issue September 1965 (Northwestern catalog).", Cadence = PressCadence.Monthly, LeadDays = 42, Slots = 6, Fit = TeenFit, ReadersMillions = 1.2f },
			new() { Id = "life", Name = "Life", Family = PressFamily.Mainstream, From = D(1936, 11, 23), Confidence = SourceConfidence.Documented, Note = "Photo essays.",
				IssueDay = DayOfWeek.Friday, LeadDays = 14, Slots = 1, ReadersMillions = 20f },
			new() { Id = "look", Name = "Look", Family = PressFamily.Mainstream, From = D(1937, 1, 5), Confidence = SourceConfidence.Documented, Note = "Photo essays, every other week.",
				IssueDay = DayOfWeek.Tuesday, LeadDays = 21, Slots = 1, ReadersMillions = 16f },
			new() { Id = "time", Name = "Time", Family = PressFamily.Mainstream, From = D(1923, 3, 3), Confidence = SourceConfidence.Documented, Note = "Sourced, skeptical features.",
				IssueDay = DayOfWeek.Friday, LeadDays = 7, Slots = 1, ReadersMillions = 12f },
			new() { Id = "newsweek", Name = "Newsweek", Family = PressFamily.Mainstream, From = D(1933, 2, 17), Confidence = SourceConfidence.Documented, Note = "Sourced, skeptical features.",
				IssueDay = DayOfWeek.Monday, LeadDays = 7, Slots = 1, ReadersMillions = 8f },
			new() { Id = "village_voice", Name = "The Village Voice", Family = PressFamily.RockCriticism, From = D(1955, 10, 26), Confidence = SourceConfidence.Documented,
				Note = "Founded 1955; its music coverage grows with the scene.", HomeCityId = "new_york", IssueDay = DayOfWeek.Thursday, LeadDays = 6, Slots = 2, Fit = CriticFit, ReadersMillions = 0.15f },
			new() { Id = "crawdaddy", Name = "Crawdaddy!", Family = PressFamily.RockCriticism, From = D(1966, 2, 7), Confidence = SourceConfidence.Approximate,
				Note = "Small, influential readership from 1966.", Cadence = PressCadence.Monthly, LeadDays = 21, Slots = 4, Fit = CriticFit, ReadersMillions = 0.02f },
			new() { Id = "rolling_stone", Name = "Rolling Stone", Family = PressFamily.RockCriticism, From = D(1967, 11, 9), Confidence = SourceConfidence.Documented,
				Note = "First issue November 9, 1967; small at first.", IssueDay = DayOfWeek.Saturday, LeadDays = 14, Slots = 4, Fit = CriticFit, ReadersMillions = 0.04f },

			// Specialist and community press (§8): highly influential inside an audience, not a lesser tier.
			new() { Id = "chicago_defender", Name = "The Chicago Defender", Family = PressFamily.Specialist, From = D(1905, 5, 5), HomeCityId = "chicago",
				Confidence = SourceConfidence.Documented, Note = "Black daily with a national readership.", Cadence = PressCadence.Daily, LeadDays = 1, Slots = 3, Fit = SoulPressFit, ReadersMillions = 0.25f },
			new() { Id = "pittsburgh_courier", Name = "The Pittsburgh Courier", Family = PressFamily.Specialist, From = D(1907, 1, 15), HomeCityId = "pittsburgh",
				Confidence = SourceConfidence.Approximate, Note = "Black weekly with national editions.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 3, Fit = SoulPressFit, ReadersMillions = 0.2f },
			new() { Id = "amsterdam_news", Name = "The Amsterdam News", Family = PressFamily.Specialist, From = D(1909, 12, 4), HomeCityId = "new_york",
				Confidence = SourceConfidence.Documented, Note = "Harlem's weekly.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 3, Fit = SoulPressFit, ReadersMillions = 0.12f },
			new() { Id = "afro_american", Name = "The Afro-American", Family = PressFamily.Specialist, From = D(1892, 8, 13), HomeCityId = "baltimore",
				Confidence = SourceConfidence.Documented, Note = "Black weekly with regional editions.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 3, Fit = SoulPressFit, ReadersMillions = 0.15f },
			new() { Id = "philadelphia_tribune", Name = "The Philadelphia Tribune", Family = PressFamily.Specialist, From = D(1884, 11, 1), HomeCityId = "philadelphia",
				Confidence = SourceConfidence.Documented, Note = "Black newspaper of record in Philadelphia.", IssueDay = DayOfWeek.Tuesday, LeadDays = 3, Slots = 2, Fit = SoulPressFit, ReadersMillions = 0.06f },
			new() { Id = "michigan_chronicle", Name = "The Michigan Chronicle", Family = PressFamily.Specialist, From = D(1936, 1, 1), HomeCityId = "detroit",
				Confidence = SourceConfidence.Approximate, Note = "Detroit's Black weekly.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 3, Fit = SoulPressFit, ReadersMillions = 0.06f },
			new() { Id = "la_sentinel", Name = "The Los Angeles Sentinel", Family = PressFamily.Specialist, From = D(1933, 1, 1), HomeCityId = "los_angeles",
				Confidence = SourceConfidence.Approximate, Note = "Black weekly in Los Angeles.", IssueDay = DayOfWeek.Thursday, LeadDays = 4, Slots = 2, Fit = SoulPressFit, ReadersMillions = 0.05f },
			new() { Id = "atlanta_daily_world", Name = "The Atlanta Daily World", Family = PressFamily.Specialist, From = D(1928, 8, 5), HomeCityId = "atlanta",
				Confidence = SourceConfidence.Approximate, Note = "Black daily in Atlanta.", Cadence = PressCadence.Daily, LeadDays = 1, Slots = 2, Fit = SoulPressFit, ReadersMillions = 0.03f },
			new() { Id = "call_and_post", Name = "The Call and Post", Family = PressFamily.Specialist, From = D(1927, 1, 1), HomeCityId = "cleveland",
				Confidence = SourceConfidence.Approximate, Note = "Cleveland's Black weekly.", IssueDay = DayOfWeek.Saturday, LeadDays = 4, Slots = 2, Fit = SoulPressFit, ReadersMillions = 0.04f },
			new() { Id = "jet", Name = "Jet", Family = PressFamily.Specialist, From = D(1951, 11, 1), Confidence = SourceConfidence.Documented,
				Note = "Weekly national news magazine for Black readers.", IssueDay = DayOfWeek.Thursday, LeadDays = 10, Slots = 3, Fit = SoulPressFit, ReadersMillions = 2.0f },
			new() { Id = "ebony", Name = "Ebony", Family = PressFamily.Specialist, From = D(1945, 11, 1), Confidence = SourceConfidence.Documented,
				Note = "Monthly picture magazine.", Cadence = PressCadence.Monthly, LeadDays = 42, Slots = 2, Fit = SoulPressFit, ReadersMillions = 3.0f },
			new() { Id = "country_song_roundup", Name = "Country Song Roundup", Family = PressFamily.Specialist, From = D(1949, 1, 1), Confidence = SourceConfidence.Approximate,
				Note = "Lyrics and stories for country fans.", Cadence = PressCadence.Monthly, LeadDays = 42, Slots = 4, Fit = CountryPressFit, ReadersMillions = 0.3f },
			new() { Id = "music_city_news", Name = "Music City News", Family = PressFamily.Specialist, From = D(1963, 7, 1), HomeCityId = "nashville", Confidence = SourceConfidence.Approximate,
				Note = "Nashville's country paper from 1963.", Cadence = PressCadence.Monthly, LeadDays = 21, Slots = 5, Fit = CountryPressFit, ReadersMillions = 0.1f },
			new() { Id = "down_beat", Name = "Down Beat", Family = PressFamily.Specialist, From = D(1934, 7, 1), HomeCityId = "chicago", Confidence = SourceConfidence.Documented,
				Note = "The jazz magazine; reviews by the musicians' own standard.", IssueDay = DayOfWeek.Thursday, LeadDays = 21, Slots = 3, Fit = JazzPressFit, ReadersMillions = 0.1f },
			new() { Id = "sing_out", Name = "Sing Out!", Family = PressFamily.Specialist, From = D(1950, 5, 1), HomeCityId = "new_york", Confidence = SourceConfidence.Documented,
				Note = "The folk song magazine.", Cadence = PressCadence.Monthly, LeadDays = 42, Slots = 3, Fit = FolkPressFit, ReadersMillions = 0.03f },
			new() { Id = "broadside", Name = "Broadside", Family = PressFamily.Specialist, From = D(1962, 2, 1), HomeCityId = "new_york", Confidence = SourceConfidence.Approximate,
				Note = "Mimeographed topical-song sheet.", Cadence = PressCadence.Monthly, LeadDays = 14, Slots = 2, Fit = FolkPressFit, ReadersMillions = 0.005f },
			// The underground press: small, local, and the first to write about a scene.
			new() { Id = "la_free_press", Name = "The Los Angeles Free Press", Family = PressFamily.RockCriticism, From = D(1964, 5, 1), HomeCityId = "los_angeles",
				Confidence = SourceConfidence.Approximate, Note = "Underground weekly from 1964.", IssueDay = DayOfWeek.Friday, LeadDays = 5, Slots = 2, Fit = CriticFit, ReadersMillions = 0.03f },
			new() { Id = "east_village_other", Name = "The East Village Other", Family = PressFamily.RockCriticism, From = D(1965, 10, 1), HomeCityId = "new_york",
				Confidence = SourceConfidence.Approximate, Note = "Underground paper from 1965.", IssueDay = DayOfWeek.Wednesday, LeadDays = 7, Slots = 2, Fit = CriticFit, ReadersMillions = 0.02f },
			new() { Id = "berkeley_barb", Name = "The Berkeley Barb", Family = PressFamily.RockCriticism, From = D(1965, 8, 13), HomeCityId = "san_francisco",
				Confidence = SourceConfidence.Approximate, Note = "Underground weekly across the bay.", IssueDay = DayOfWeek.Friday, LeadDays = 5, Slots = 2, Fit = CriticFit, ReadersMillions = 0.02f },
		};
		// Every playable city has a daily paper with an entertainment column (§8 local general press): fictional mastheads,
		// keyed per world. Often the first earned clipping a small label ever gets.
		foreach (string city in metro1960.Keys.OrderBy(c => c, StringComparer.Ordinal)) {
			string cityName = DistanceModel.GetCityById(city)?.name ?? city;
			string masthead = Mastheads[(int)(MediaAtmosphere.Unit("daily|" + city) * Mastheads.Length) % Mastheads.Length];
			list.Add(new PressOutlet {
				Id = "daily_" + city, Name = $"The {cityName} {masthead}", Family = PressFamily.LocalDaily, From = D(1900, 1, 1), HomeCityId = city,
				Confidence = SourceConfidence.Fictional, Note = "A city daily's entertainment page (fictional masthead).",
				Cadence = PressCadence.Daily, LeadDays = 1, Slots = 2, ReadersMillions = MetroPopulationMillions(city, 1960) * 0.25f,
			});
		}
		return list;
	}

	public static PressOutlet GetPress(string id) => Press.FirstOrDefault(p => p.Id == id);

	/// <summary>Probe hook: forget the seed-keyed build so a different seed rebuilds it.</summary>
	internal static void Reset() { programs = null; press = null; }
}
