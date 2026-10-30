using System;
using System.Collections.Generic;

/// <summary>
/// The rest of the 1960s television ecosystem (SimTools/MediaTvHeadlinerBookingDesign.md): network variety hours, talk
/// shows, the specialist country, soul and folk programs, and more documented local dance shows. Run dates come from the
/// sources cited on each program; time slots that no source pinned down are marked Approximate. Programs that booked
/// almost no outside guests (Lawrence Welk, Sing Along with Mitch, The King Family) and the London-made This Is Tom Jones
/// are left out.
/// </summary>
public static partial class MediaOutletCatalog {
	// ---- genre fit for the new audiences ---------------------------------------------------------------------

	/// <summary>A talk show's couch: singers and comics the grown-ups know, a current pop act now and then.</summary>
	public static float TalkFit(Genre g) => g switch {
		Genre.TraditionalPop or Genre.Comedy or Genre.Jazz or Genre.EasyListening => 1.0f,
		Genre.Folk or Genre.BossaNova or Genre.Country or Genre.Soul or Genre.LatinPop or Genre.Classical or Genre.ContemporaryFolk => 0.8f,
		Genre.PopRock or Genre.SingerSongwriter or Genre.SunshinePop or Genre.Motown or Genre.Gospel or Genre.Blues => 0.7f,
		Genre.TeenPop or Genre.RockAndRoll or Genre.DooWop or Genre.BritishBeat or Genre.BritishInvasion or Genre.BritishPop or Genre.GirlGroup or Genre.FolkRock => 0.55f,
		_ => 0.4f,
	};
	public static float CountryFit(Genre g) => g switch {
		Genre.Country => 1.0f,
		Genre.CountryRock => 0.9f,
		Genre.TexMex or Genre.Folk or Genre.Gospel => 0.7f,
		Genre.Comedy => 0.6f,
		Genre.RockAndRoll or Genre.TraditionalPop or Genre.RootsRock => 0.5f,
		Genre.Blues => 0.4f,
		_ => 0.2f,
	};
	/// <summary>Country-and-pop hosts (Jimmy Dean, Glen Campbell, Johnny Cash) booked across the line.</summary>
	public static float CountryPopFit(Genre g) => g switch {
		Genre.Country or Genre.CountryRock => 1.0f,
		Genre.Folk or Genre.ContemporaryFolk or Genre.RootsRock => 0.85f,
		Genre.TraditionalPop or Genre.PopRock or Genre.SunshinePop or Genre.SingerSongwriter => 0.75f,
		Genre.Gospel or Genre.Comedy or Genre.RockAndRoll or Genre.FolkRock or Genre.TexMex => 0.7f,
		Genre.Soul or Genre.TeenPop or Genre.EasyListening => 0.55f,
		_ => 0.4f,
	};
	public static float SoulFit(Genre g) => g switch {
		Genre.Soul or Genre.RnB or Genre.Motown or Genre.Funk or Genre.Boogaloo => 1.0f,
		Genre.Gospel => 0.9f,
		Genre.Blues or Genre.DooWop => 0.85f,
		Genre.GirlGroup => 0.75f,
		Genre.Jazz or Genre.RockAndRoll => 0.6f,
		_ => 0.25f,
	};
	public static float FolkFit(Genre g) => g switch {
		Genre.Folk or Genre.ContemporaryFolk => 1.0f,
		Genre.FolkRock => 0.85f,
		Genre.SingerSongwriter => 0.8f,
		Genre.Skiffle => 0.7f,
		Genre.Country or Genre.Gospel or Genre.Blues => 0.6f,
		Genre.Comedy => 0.5f,
		Genre.TraditionalPop => 0.3f,
		_ => 0.2f,
	};
	/// <summary>The Bell Telephone Hour: concert artists, Broadway and the better popular singers.</summary>
	public static float ConcertFit(Genre g) => g switch {
		Genre.Classical => 1.0f,
		Genre.TraditionalPop => 0.8f,
		Genre.Jazz => 0.75f,
		Genre.EasyListening => 0.7f,
		Genre.Folk or Genre.Gospel or Genre.BossaNova => 0.6f,
		Genre.Country => 0.4f,
		_ => 0.2f,
	};

	// ---- builders --------------------------------------------------------------------------------------------

	private static DayOfWeek[] On(params DayOfWeek[] days) => days;

	private static TvProgram Show(string id, string name, string host, TvNetwork network, TvProgramKind kind, PerformanceMethod method, string methodNote,
		float[] attraction, int rebookWeeks, int chartCeiling, Func<Genre, float> fit, SourceConfidence confidence, string source) => new() {
			Id = id, Name = name, HostName = host, Network = network, Kind = kind, Method = method, MethodNote = methodNote, Attraction = attraction,
			RebookWeeks = rebookWeeks, ChartCeiling = chartCeiling, GenreFit = fit, Confidence = confidence, Source = source,
		};

	private static void Air(TvProgram p, GameDate first, GameDate last, DayOfWeek[] days, int start, int minutes, CarriageRule carriage, BroadcastMethod broadcast,
		string city, SourceConfidence confidence, string note = null, int[] repeatMonths = null, int intervalWeeks = 1) =>
		p.Schedule.Add(new BroadcastScheduleVersion {
			First = first, Last = last, Days = days, StartMinute = start, DurationMinutes = minutes, Carriage = carriage, Broadcast = broadcast,
			ProductionCityId = city, Confidence = confidence, Note = note, RepeatMonths = repeatMonths ?? Array.Empty<int>(), IntervalWeeks = intervalWeeks,
		});

	/// <summary>Adds a dated bill plan and keeps <see cref="TvProgram.GuestSlots"/> at the widest bill for older readers.</summary>
	private static void Bill(TvProgram p, GameDate from, int chart, int standing, int comedy = 0) {
		p.Bills.Add(new BillPlan { From = from, Chart = chart, Standing = standing, Comedy = comedy });
		p.GuestSlots = Math.Max(p.GuestSlots, chart + standing + comedy);
	}

	private static void Syndicate(TvProgram p, int year, int month, DayOfWeek day, int localStart, params (string City, int Delay)[] markets) {
		foreach (var (city, delay) in markets) {
			if (!metro1960.ContainsKey(city) || city == p.HomeCityId) continue;
			p.Syndication.Add(new SyndicationMarket { CityId = city, From = D(year, month, 1), DelayWeeks = delay, Day = day, LocalStartMinute = localStart });
		}
	}

	private static readonly int[] Summer = { 7, 8 };
	private static readonly int[] LateSummer = { 6, 7, 8 };

	// ---- network variety and talk ----------------------------------------------------------------------------

	private static void AddVarietyAndTalk() {
		const SourceConfidence Doc = SourceConfidence.Documented, Approx = SourceConfidence.Approximate;
		const CarriageRule Prime = CarriageRule.NetworkPrimetime;
		const BroadcastMethod Live = BroadcastMethod.Live, Taped = BroadcastMethod.Taped;

		var como = Show("perry_como", "Perry Como's Kraft Music Hall", "Perry Como", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Guests sing with Mitchell Ayres' orchestra and the Ray Charles Singers; duets with the host are routine.",
			new[] { 0.45f, 0.55f, 0.80f, 1.10f }, 8, 30, FamilyVarietyFit, Doc,
			"Kraft Music Hall, NBC Oct 8 1958 – Sep 1 1971; Como hosted weekly from 1959, then monthly specials 1963-67 (Wikipedia; CTVA Perry Como Show guides).");
		Air(como, D(1959, 9, 30), D(1963, 6, 12), On(DayOfWeek.Wednesday), Clock(21), 60, Prime, Taped, "new_york", Approx, "Wednesday at nine; taped in New York.", Summer);
		Air(como, D(1963, 10, 3), D(1967, 5, 25), On(DayOfWeek.Thursday), Clock(21), 60, Prime, Taped, "new_york", Approx, "About once a month as a special.", intervalWeeks: 4);
		Bill(como, D(1959, 9, 30), 1, 2, 1);
		programs.Add(como);

		var kmh = Show("kraft_music_hall", "The Kraft Music Hall", "a rotating host", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"A themed hour with a different host each week; guests work with the house orchestra.",
			new[] { 0.30f, 0.60f, 0.90f, 1.00f }, 8, 30, FamilyVarietyFit, Doc, "Weekly again from fall 1967 without Como, to Sep 1 1971 (Wikipedia).");
		Air(kmh, D(1967, 9, 13), Horizon, On(DayOfWeek.Wednesday), Clock(21), 60, Prime, Taped, "new_york", Approx, repeatMonths: Summer);
		Bill(kmh, D(1967, 9, 13), 1, 2, 1);
		programs.Add(kmh);

		var dinah = Show("dinah_shore", "The Dinah Shore Chevy Show", "Dinah Shore", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Songs, sketches and production numbers with the host; guests sing with the orchestra.",
			new[] { 0.40f, 0.50f, 0.80f, 1.05f }, 8, 30, FamilyVarietyFit, Doc, "NBC Oct 5 1956 – May 12 1963; final season Sundays at 10 (Wikipedia).");
		Air(dinah, D(1959, 10, 4), D(1961, 6, 25), On(DayOfWeek.Sunday), Clock(21), 60, Prime, Live, "los_angeles", Approx, repeatMonths: Summer);
		Air(dinah, D(1961, 10, 6), D(1962, 6, 1), On(DayOfWeek.Friday), Clock(22), 60, Prime, Taped, "los_angeles", Approx);
		Air(dinah, D(1962, 10, 14), D(1963, 5, 12), On(DayOfWeek.Sunday), Clock(22), 60, Prime, Taped, "los_angeles", Doc, "Sunday nights at ten in the final season.");
		Bill(dinah, D(1959, 10, 4), 1, 1, 1);
		programs.Add(dinah);

		var moore = Show("garry_moore", "The Garry Moore Show", "Garry Moore", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Comedy with the resident company; a guest singer works with Howard Smith's orchestra.",
			new[] { 0.30f, 0.40f, 0.80f, 1.00f }, 8, 30, FamilyVarietyFit, Doc, "CBS evening hour 1958-64, revived Sep 1966 – Jan 8 1967 (Wikipedia).");
		Air(moore, D(1959, 9, 29), D(1964, 6, 16), On(DayOfWeek.Tuesday), Clock(22), 60, Prime, Taped, "new_york", Approx, repeatMonths: Summer);
		Air(moore, D(1966, 9, 11), D(1967, 1, 8), On(DayOfWeek.Sunday), Clock(21), 60, Prime, Taped, "new_york", Approx);
		Bill(moore, D(1959, 9, 29), 0, 1, 1);
		programs.Add(moore);

		var bell = Show("bell_telephone_hour", "The Bell Telephone Hour", "Donald Voorhees", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Concert, Broadway and popular singers with the Bell Telephone Orchestra; generous rehearsal.",
			new[] { 0.15f, 0.20f, 0.50f, 0.90f }, 12, 0, ConcertFit, Approx,
			"NBC 1959-68: Fridays at 10, Tuesdays at 10 from 1963, Sundays at 6:30 from fall 1965 (Wikipedia). Cadence approximate.");
		Air(bell, D(1959, 10, 9), D(1963, 6, 7), On(DayOfWeek.Friday), Clock(22), 60, Prime, Live, "new_york", Approx, intervalWeeks: 2);
		Air(bell, D(1963, 9, 24), D(1965, 6, 1), On(DayOfWeek.Tuesday), Clock(22), 60, Prime, Taped, "new_york", Approx, intervalWeeks: 2);
		Air(bell, D(1965, 9, 19), D(1968, 6, 9), On(DayOfWeek.Sunday), Clock(18, 30), 60, Prime, Taped, "new_york", Approx, intervalWeeks: 2);
		Bill(bell, D(1959, 10, 9), 0, 2);
		bell.StandingFloor = 0.12f;
		programs.Add(bell);

		var williams = Show("andy_williams", "The Andy Williams Show", "Andy Williams", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"A singer's hour: guests sing with the orchestra and duet with the host.",
			new[] { 0.50f, 0.70f, 0.90f, 1.05f }, 8, 30, FamilyVarietyFit, Doc, "NBC weekly 1962-67 and 1969-71 (Wikipedia). Night and hour approximate.");
		Air(williams, D(1962, 9, 27), D(1963, 5, 30), On(DayOfWeek.Thursday), Clock(22), 60, Prime, Taped, "los_angeles", Approx);
		Air(williams, D(1963, 9, 23), D(1967, 5, 22), On(DayOfWeek.Monday), Clock(21), 60, Prime, Taped, "los_angeles", Approx, repeatMonths: Summer);
		Air(williams, D(1969, 9, 20), Horizon, On(DayOfWeek.Saturday), Clock(19, 30), 60, Prime, Taped, "los_angeles", Approx, repeatMonths: Summer);
		Bill(williams, D(1962, 9, 27), 1, 2);
		programs.Add(williams);

		var kaye = Show("danny_kaye", "The Danny Kaye Show", "Danny Kaye", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"The host does the comedy; guest singers work with Paul Weston's orchestra.",
			new[] { 0.40f, 0.40f, 0.80f, 1.00f }, 8, 30, FamilyVarietyFit, Doc, "CBS Wednesdays at 10, Sep 25 1963 – Jun 28 1967 (Wikipedia).");
		Air(kaye, D(1963, 9, 25), D(1967, 6, 28), On(DayOfWeek.Wednesday), Clock(22), 60, Prime, Taped, "los_angeles", Doc, repeatMonths: Summer);
		Bill(kaye, D(1963, 9, 25), 0, 2);
		programs.Add(kaye);

		var garland = Show("judy_garland", "The Judy Garland Show", "Judy Garland", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Guest singers with Mort Lindsey's orchestra; long duets with the host.",
			new[] { 0.30f, 0.40f, 0.80f, 0.95f }, 10, 30, FamilyVarietyFit, Doc, "CBS Sundays at 9, Sep 29 1963 – Mar 29 1964 (Wikipedia).");
		Air(garland, D(1963, 9, 29), D(1964, 3, 29), On(DayOfWeek.Sunday), Clock(21), 60, Prime, Taped, "los_angeles", Doc);
		Bill(garland, D(1963, 9, 29), 0, 2);
		programs.Add(garland);

		var gleason = Show("jackie_gleason", "The Jackie Gleason Show", "Jackie Gleason", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"The host's comedy and the June Taylor Dancers; one guest singer with Sammy Spear's orchestra.",
			new[] { 0.60f, 0.60f, 0.80f, 1.05f }, 10, 30, FamilyVarietyFit, Approx,
			"CBS Saturdays: American Scene Magazine from 1962 (New York, then Miami Beach from 1964); The Jackie Gleason Show 1966-70.");
		Air(gleason, D(1962, 9, 29), D(1964, 6, 27), On(DayOfWeek.Saturday), Clock(20, 30), 60, Prime, Taped, "new_york", Approx, repeatMonths: Summer);
		Air(gleason, D(1964, 9, 26), D(1966, 6, 25), On(DayOfWeek.Saturday), Clock(20, 30), 60, Prime, Taped, "miami", Approx, repeatMonths: Summer);
		Air(gleason, D(1966, 9, 17), D(1970, 6, 27), On(DayOfWeek.Saturday), Clock(19, 30), 60, Prime, Taped, "miami", Approx, repeatMonths: Summer);
		Bill(gleason, D(1962, 9, 29), 0, 1);
		programs.Add(gleason);

		var skelton = Show("red_skelton", "The Red Skelton Hour", "Red Skelton", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"The host's sketches; a guest singer with David Rose's orchestra.",
			new[] { 0.80f, 0.70f, 0.80f, 1.05f }, 10, 30, FamilyVarietyFit, Doc, "CBS Tuesdays; an hour from 1962 (Wikipedia ratings table).");
		Air(skelton, D(1962, 9, 25), D(1963, 6, 25), On(DayOfWeek.Tuesday), Clock(20, 30), 60, Prime, Taped, "los_angeles", Doc, repeatMonths: Summer);
		Air(skelton, D(1963, 9, 24), D(1964, 6, 23), On(DayOfWeek.Tuesday), Clock(20), 60, Prime, Taped, "los_angeles", Doc, repeatMonths: Summer);
		Air(skelton, D(1964, 9, 22), D(1970, 6, 23), On(DayOfWeek.Tuesday), Clock(20, 30), 60, Prime, Taped, "los_angeles", Approx, repeatMonths: Summer);
		Bill(skelton, D(1962, 9, 25), 0, 1);
		programs.Add(skelton);

		var dean = Show("jimmy_dean", "The Jimmy Dean Show", "Jimmy Dean", TvNetwork.ABC, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveVocalOverTrack,
			"Country and pop guests with the house band; Rowlf the puppet works the host's desk.",
			new[] { 0.30f, 0.40f, 0.70f, 0.95f }, 8, 40, CountryPopFit, Doc, "ABC Sep 19 1963 – Apr 1 1966, from New York (Wikipedia schedule).");
		Air(dean, D(1963, 9, 19), D(1964, 3, 5), On(DayOfWeek.Thursday), Clock(21), 60, Prime, Taped, "new_york", Doc);
		Air(dean, D(1964, 3, 12), D(1964, 8, 27), On(DayOfWeek.Thursday), Clock(21, 30), 60, Prime, Taped, "new_york", Doc);
		Air(dean, D(1964, 9, 3), D(1965, 9, 2), On(DayOfWeek.Thursday), Clock(22), 60, Prime, Taped, "new_york", Doc);
		Air(dean, D(1965, 9, 10), D(1966, 4, 1), On(DayOfWeek.Friday), Clock(22), 60, Prime, Taped, "new_york", Doc);
		Bill(dean, D(1963, 9, 19), 1, 1);
		programs.Add(dean);

		var martin = Show("dean_martin", "The Dean Martin Show", "Dean Martin", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"A loose hour from Burbank: guests rehearse with the orchestra, the host barely at all.",
			new[] { 0.30f, 0.50f, 1.00f, 1.10f }, 8, 30, FamilyVarietyFit, Doc, "NBC Thursdays at 10 from Sep 16 1965 (Wikipedia).");
		Air(martin, D(1965, 9, 16), Horizon, On(DayOfWeek.Thursday), Clock(22), 60, Prime, Taped, "los_angeles", Doc, repeatMonths: Summer);
		Bill(martin, D(1965, 9, 16), 1, 2, 1);
		programs.Add(martin);

		var burnett = Show("carol_burnett", "The Carol Burnett Show", "Carol Burnett", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"Sketch comedy with the company; a guest singer with Harry Zimmerman's orchestra.",
			new[] { 0.40f, 0.60f, 1.00f, 1.00f }, 10, 30, FamilyVarietyFit, Doc, "CBS Mondays at 10 from Sep 11 1967 (Wikipedia).");
		Air(burnett, D(1967, 9, 11), Horizon, On(DayOfWeek.Monday), Clock(22), 60, Prime, Taped, "los_angeles", Doc, repeatMonths: Summer);
		Bill(burnett, D(1967, 9, 11), 0, 1);
		programs.Add(burnett);

		var campbell = Show("glen_campbell", "The Glen Campbell Goodtime Hour", "Glen Campbell", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveBandAndVocal,
			"Country and pop guests, often playing with the host.",
			new[] { 0.50f, 0.80f, 1.00f, 1.00f }, 8, 40, CountryPopFit, Doc, "CBS from Jan 29 1969 (Wikipedia). Slots approximate.");
		Air(campbell, D(1969, 1, 29), D(1969, 6, 25), On(DayOfWeek.Wednesday), Clock(19, 30), 60, Prime, Taped, "los_angeles", Approx);
		Air(campbell, D(1969, 9, 21), Horizon, On(DayOfWeek.Sunday), Clock(21), 60, Prime, Taped, "los_angeles", Approx, repeatMonths: Summer);
		Bill(campbell, D(1969, 1, 29), 1, 1);
		programs.Add(campbell);

		var cash = Show("johnny_cash", "The Johnny Cash Show", "Johnny Cash", TvNetwork.ABC, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveBandAndVocal,
			"Live music from the Ryman Auditorium; guests play their own sets.",
			new[] { 0.30f, 0.70f, 1.00f, 0.90f }, 8, 40, CountryPopFit, Doc, "ABC Jun 7 1969 – Mar 31 1971, from Nashville (Wikipedia). Slots approximate.");
		Air(cash, D(1969, 6, 7), D(1969, 9, 27), On(DayOfWeek.Saturday), Clock(21, 30), 60, Prime, Taped, "nashville", Approx);
		Air(cash, D(1970, 1, 21), Horizon, On(DayOfWeek.Wednesday), Clock(21), 60, Prime, Taped, "nashville", Approx, repeatMonths: Summer);
		Bill(cash, D(1969, 6, 7), 1, 1);
		programs.Add(cash);

		var heehaw = Show("hee_haw", "Hee Haw", "Buck Owens and Roy Clark", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveVocalOverTrack,
			"Cornfield gags from the cast between country songs; guests tape their numbers in Nashville.",
			new[] { 0.60f, 0.60f, 0.80f, 1.00f }, 10, 0, CountryFit, Doc, "CBS from Jun 15 1969: Sundays at 9, then Wednesdays at 7:30 (Wikipedia ratings table).");
		Air(heehaw, D(1969, 6, 15), D(1969, 9, 7), On(DayOfWeek.Sunday), Clock(21), 60, Prime, Taped, "nashville", Doc);
		Air(heehaw, D(1969, 12, 17), Horizon, On(DayOfWeek.Wednesday), Clock(19, 30), 60, Prime, Taped, "nashville", Doc, repeatMonths: Summer);
		Bill(heehaw, D(1969, 6, 15), 1, 1);
		heehaw.StandingFloor = 0.08f;
		programs.Add(heehaw);

		var lively = Show("lively_ones", "The Lively Ones", "Vic Damone", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveVocalOverTrack,
			"A summer half hour of staged musical numbers, some on location.",
			new[] { 0.30f, 0.80f, 0.90f, 0.70f }, 6, 40, FamilyVarietyFit, Approx, "NBC summer replacement, 1962 and 1963.");
		Air(lively, D(1962, 7, 26), D(1962, 9, 13), On(DayOfWeek.Thursday), Clock(21, 30), 30, Prime, Taped, "los_angeles", Approx);
		Air(lively, D(1963, 7, 18), D(1963, 9, 12), On(DayOfWeek.Thursday), Clock(21, 30), 30, Prime, Taped, "los_angeles", Approx);
		Bill(lively, D(1962, 7, 26), 1, 1);
		programs.Add(lively);

		var lawrence = Show("steve_lawrence", "The Steve Lawrence Show", "Steve Lawrence", TvNetwork.CBS, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"A singer's hour with comedy guests.", new[] { 0.30f, 0.50f, 0.90f, 1.00f }, 6, 30, FamilyVarietyFit, Approx, "CBS Mondays, fall 1965 only.");
		Air(lawrence, D(1965, 9, 13), D(1965, 12, 13), On(DayOfWeek.Monday), Clock(22), 60, Prime, Taped, "new_york", Approx);
		Bill(lawrence, D(1965, 9, 13), 1, 1, 1);
		programs.Add(lawrence);

		var miller = Show("roger_miller", "The Roger Miller Show", "Roger Miller", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.LiveBandAndVocal,
			"A half hour of the host's songs and a guest.", new[] { 0.40f, 0.70f, 0.90f, 0.90f }, 6, 40, CountryPopFit, Approx, "NBC Mondays, fall 1966 only.");
		Air(miller, D(1966, 9, 12), D(1966, 12, 26), On(DayOfWeek.Monday), Clock(20, 30), 30, Prime, Taped, "los_angeles", Approx);
		Bill(miller, D(1966, 9, 12), 1, 1);
		programs.Add(miller);

		var summerHall = Show("kraft_summer_music_hall", "Kraft Summer Music Hall", "John Davidson", TvNetwork.NBC, TvProgramKind.PrimetimeVariety, PerformanceMethod.HouseOrchestra,
			"A summer hour with young singers and comics.", new[] { 0.30f, 0.80f, 0.90f, 0.80f }, 6, 40, FamilyVarietyFit, Approx, "NBC summer 1966.");
		Air(summerHall, D(1966, 6, 6), D(1966, 8, 29), On(DayOfWeek.Monday), Clock(21), 60, Prime, Taped, "los_angeles", Approx);
		Bill(summerHall, D(1966, 6, 6), 1, 1, 1);
		programs.Add(summerHall);

		var beechnut = Show("dick_clark_saturday", "The Dick Clark Show", "Dick Clark", TvNetwork.ABC, TvProgramKind.NationalTeen, PerformanceMethod.Mime,
			"Saturday night from the Little Theater: the hits of the week, mimed.", new[] { 0.30f, 1.20f, 0.50f, 0.15f }, 6, 40, TeenFit, Doc,
			"ABC Saturday night, Feb 15 1958 – Sep 10 1960, from New York (Wikipedia).");
		Air(beechnut, D(1958, 2, 15), D(1960, 9, 10), On(DayOfWeek.Saturday), Clock(19, 30), 30, Prime, Live, "new_york", Approx, "Saturday at 7:30; hour approximate.");
		beechnut.GuestSlots = 3;
		programs.Add(beechnut);

		var happening = Show("happening_68", "Happening '68", "Paul Revere and Mark Lindsay", TvNetwork.ABC, TvProgramKind.NationalTeen, PerformanceMethod.Mime,
			"Saturday after Bandstand: current records mimed, with a talent contest.", new[] { 0.25f, 1.10f, 0.40f, 0.08f }, 6, 60, TeenFit, Doc,
			"ABC Jan 6 1968 – Sep 20 1969 (Wikipedia). Hour approximate.");
		Air(happening, D(1968, 1, 6), D(1969, 9, 20), On(DayOfWeek.Saturday), Clock(14, 30), 30, CarriageRule.NetworkSimultaneous, Taped, "los_angeles", Approx);
		happening.GuestSlots = 2;
		happening.DiscoverySlot = true;
		programs.Add(happening);

		var jubilee = Show("jubilee_usa", "Jubilee USA", "Red Foley", TvNetwork.ABC, TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"Country music from the Jewell Theatre in Springfield, Missouri.", new[] { 0.40f, 0.40f, 0.60f, 0.90f }, 6, 0, CountryFit, Doc,
			"ABC; Ozark Jubilee from 1955, Jubilee USA to Sep 24 1960 (Wikipedia). Springfield is not a game city: Kansas City stands in for travel.");
		Air(jubilee, D(1959, 10, 3), D(1960, 9, 24), On(DayOfWeek.Saturday), Clock(22), 60, Prime, Live, "kansas_city", Approx);
		Bill(jubilee, D(1959, 10, 3), 1, 1);
		jubilee.StandingFloor = 0.05f;
		programs.Add(jubilee);

		var hoot = Show("hootenanny", "Hootenanny", "Jack Linkletter", TvNetwork.ABC, TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"Folk acts taped before college audiences on a different campus each week.", new[] { 0.30f, 0.90f, 1.10f, 0.60f }, 6, 0, FolkFit, Doc,
			"ABC Apr 6 1963 – Sep 12 1964 (Wikipedia). Taped on campuses; New York stands in for travel.");
		Air(hoot, D(1963, 4, 6), D(1964, 9, 12), On(DayOfWeek.Saturday), Clock(20, 30), 60, Prime, Taped, "new_york", Approx);
		Bill(hoot, D(1963, 4, 6), 1, 2);
		hoot.StandingFloor = 0.06f;
		programs.Add(hoot);

		// ---- talk ----

		var paar = Show("tonight_paar", "Tonight Starring Jack Paar", "Jack Paar", TvNetwork.NBC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A song with José Melis's band, then the couch.", new[] { 0.02f, 0.30f, 1.00f, 1.00f }, 4, 0, TalkFit, Doc,
			"NBC weeknights, Jul 29 1957 – Mar 30 1962, from New York (Wikipedia).");
		Air(paar, D(1957, 7, 29), D(1962, 3, 30), Weekdays, Clock(23, 15), 105, Prime, Taped, "new_york", Approx);
		Bill(paar, D(1957, 7, 29), 0, 1, 1);
		programs.Add(paar);

		var interim = Show("tonight_interim", "The Tonight Show", "guest hosts", TvNetwork.NBC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A different guest host each week between Paar and Carson.", new[] { 0.02f, 0.30f, 0.90f, 0.90f }, 4, 0, TalkFit, Doc,
			"NBC, Apr 2 – Sep 28 1962 (Wikipedia, Tonight Show history).");
		Air(interim, D(1962, 4, 2), D(1962, 9, 28), Weekdays, Clock(23, 15), 105, Prime, Taped, "new_york", Approx);
		Bill(interim, D(1962, 4, 2), 0, 1, 1);
		programs.Add(interim);

		var carson = Show("tonight_carson", "The Tonight Show Starring Johnny Carson", "Johnny Carson", TvNetwork.NBC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A number with the band, a chair next to the host.", new[] { 0.02f, 0.35f, 1.00f, 1.00f }, 4, 30, TalkFit, Doc,
			"NBC from Oct 1 1962, New York until 1972; 11:15 to 1966, 11:30 from Jan 2 1967 (Wikipedia).");
		Air(carson, D(1962, 10, 1), D(1966, 12, 30), Weekdays, Clock(23, 15), 105, Prime, Taped, "new_york", Doc);
		Air(carson, D(1967, 1, 2), Horizon, Weekdays, Clock(23, 30), 90, Prime, Taped, "new_york", Doc);
		Bill(carson, D(1962, 10, 1), 1, 1, 1);
		programs.Add(carson);

		var paarProgram = Show("jack_paar_program", "The Jack Paar Program", "Jack Paar", TvNetwork.NBC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A primetime talk hour from Studio 6B; film clips and a guest number.", new[] { 0.10f, 0.40f, 0.90f, 1.00f }, 8, 40, TalkFit, Doc,
			"NBC Fridays at 10, Sep 21 1962 – Sep 10 1965 (Wikipedia).");
		Air(paarProgram, D(1962, 9, 21), D(1965, 9, 10), On(DayOfWeek.Friday), Clock(22), 60, Prime, Taped, "new_york", Doc, repeatMonths: Summer);
		Bill(paarProgram, D(1962, 9, 21), 1, 1, 1);
		programs.Add(paarProgram);

		var mervNbc = Show("merv_griffin_nbc", "The Merv Griffin Show", "Merv Griffin", TvNetwork.NBC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A daytime talk hour.", new[] { 0.10f, 0.25f, 0.60f, 1.00f }, 6, 40, TalkFit, Approx, "NBC daytime, Oct 1962 – Mar 1963, New York.");
		Air(mervNbc, D(1962, 10, 1), D(1963, 3, 29), Weekdays, Clock(14), 55, CarriageRule.NetworkSimultaneous, Taped, "new_york", Approx);
		Bill(mervNbc, D(1962, 10, 1), 1, 1, 1);
		programs.Add(mervNbc);

		var merv = Show("merv_griffin", "The Merv Griffin Show", "Merv Griffin", TvNetwork.Syndicated, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"Westinghouse's talk hour from the Little Theatre; Arthur Treacher at the door.", new[] { 0.05f, 0.30f, 0.80f, 1.00f }, 6, 40, TalkFit, Approx,
			"Westinghouse syndication May 1965 – Aug 1969, then CBS late night (Wikipedia: New York 1965-70).");
		Air(merv, D(1965, 5, 10), D(1969, 8, 8), Weekdays, Clock(16, 30), 90, CarriageRule.Syndicated, Taped, "new_york", Approx, "Stations chose their own hour; many ran it late at night.");
		Bill(merv, D(1965, 5, 10), 1, 1, 1);
		merv.SyndicatedClearance = 0.45f;
		programs.Add(merv);

		var mervCbs = Show("merv_griffin_cbs", "The Merv Griffin Show", "Merv Griffin", TvNetwork.CBS, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"CBS's answer to Carson.", new[] { 0.02f, 0.30f, 0.90f, 0.90f }, 4, 40, TalkFit, Approx, "CBS late night from Aug 18 1969, New York.");
		Air(mervCbs, D(1969, 8, 18), Horizon, Weekdays, Clock(23, 30), 90, Prime, Taped, "new_york", Approx);
		Bill(mervCbs, D(1969, 8, 18), 1, 1, 1);
		programs.Add(mervCbs);

		var douglasLocal = Show("mike_douglas_local", "The Mike Douglas Show", "Mike Douglas", TvNetwork.Local, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A daytime talk-and-music hour with a celebrity co-host for the week.", new[] { 0.10f, 0.25f, 0.60f, 1.00f }, 6, 0, TalkFit, Doc,
			"KYW-TV Cleveland from Dec 11 1961 (Wikipedia); local until Westinghouse syndicated it.");
		douglasLocal.Station = "KYW-TV"; douglasLocal.HomeCityId = "cleveland"; douglasLocal.NationalBooking = true;
		Air(douglasLocal, D(1961, 12, 11), D(1963, 7, 31), Weekdays, Clock(13), 90, CarriageRule.LocalOnly, Live, "cleveland", Approx);
		Bill(douglasLocal, D(1961, 12, 11), 1, 1, 1);
		douglasLocal.StandingFloor = 0.12f;
		programs.Add(douglasLocal);

		var douglas = Show("mike_douglas", "The Mike Douglas Show", "Mike Douglas", TvNetwork.Syndicated, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"Ninety minutes of talk and music with a celebrity co-host for the week.", new[] { 0.10f, 0.25f, 0.60f, 1.00f }, 6, 40, TalkFit, Doc,
			"Westinghouse syndication; Cleveland to 1965, Philadelphia from 1965 (Wikipedia).");
		Air(douglas, D(1963, 8, 1), D(1965, 7, 30), Weekdays, Clock(13), 90, CarriageRule.Syndicated, Taped, "cleveland", Approx);
		Air(douglas, D(1965, 8, 2), Horizon, Weekdays, Clock(13), 90, CarriageRule.Syndicated, Taped, "philadelphia", Approx);
		Bill(douglas, D(1963, 8, 1), 1, 1, 1);
		douglas.SyndicatedClearance = 0.55f;
		programs.Add(douglas);

		var allen = Show("steve_allen_westinghouse", "The Steve Allen Show", "Steve Allen", TvNetwork.Syndicated, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"A late talk-and-stunts hour from a converted Hollywood theater; the oddest guests on television.", new[] { 0.02f, 0.40f, 0.90f, 0.80f }, 6, 40, TalkFit, Approx,
			"Westinghouse syndication, 1962-64, Hollywood.");
		Air(allen, D(1962, 6, 4), D(1964, 6, 26), Weekdays, Clock(23, 15), 90, CarriageRule.Syndicated, Taped, "los_angeles", Approx);
		Bill(allen, D(1962, 6, 4), 1, 1, 1);
		allen.SyndicatedClearance = 0.35f;
		programs.Add(allen);

		var bishop = Show("joey_bishop", "The Joey Bishop Show", "Joey Bishop", TvNetwork.ABC, TvProgramKind.TalkShow, PerformanceMethod.HouseOrchestra,
			"ABC's late-night talk show from Hollywood.", new[] { 0.02f, 0.30f, 0.90f, 0.90f }, 4, 0, TalkFit, Doc,
			"ABC late night, Apr 17 1967 – Dec 26 1969, Los Angeles (Wikipedia).");
		Air(bishop, D(1967, 4, 17), D(1969, 12, 26), Weekdays, Clock(23, 30), 90, Prime, Taped, "los_angeles", Doc);
		Bill(bishop, D(1967, 4, 17), 0, 1, 1);
		programs.Add(bishop);
	}

	// ---- specialist music and more documented local shows -------------------------------------------------------

	private static readonly (string City, int Delay)[] CountryMarkets = {
		("memphis", 1), ("atlanta", 1), ("new_orleans", 2), ("dallas", 1), ("houston", 2), ("san_antonio", 2), ("st_louis", 2),
		("kansas_city", 2), ("omaha", 3), ("cincinnati", 2), ("denver", 3), ("phoenix", 3), ("albuquerque", 3), ("billings", 4),
		("salt_lake_city", 4), ("pittsburgh", 3) };

	private static void AddSpecialistAndLocalMusic() {
		const SourceConfidence Doc = SourceConfidence.Documented, Approx = SourceConfidence.Approximate;
		const BroadcastMethod Live = BroadcastMethod.Live, Taped = BroadcastMethod.Taped;

		TvProgram Local(string id, string name, string host, string station, string city, TvProgramKind kind, PerformanceMethod method, string methodNote,
			float[] attraction, int slots, bool discovery, int rebook, Func<Genre, float> fit, SourceConfidence confidence, string source) {
			var p = Show(id, name, host, TvNetwork.Local, kind, method, methodNote, attraction, rebook, 0, fit, confidence, source);
			p.Station = station; p.HomeCityId = city; p.GuestSlots = slots; p.DiscoverySlot = discovery;
			return p;
		}

		// Country: Nashville tapes mailed through the South and West, a few weeks behind.
		var porter = Local("porter_wagoner", "The Porter Wagoner Show", "Porter Wagoner", "WSM-TV", "nashville", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"The Wagonmasters back a guest or two between the host's songs.", new[] { 0.30f, 0.30f, 0.60f, 1.00f }, 1, false, 8, CountryFit, Approx,
			"Syndicated half hour from Nashville from 1960 (Chattanooga Medicine Co.); market list a composite.");
		Air(porter, D(1960, 9, 10), Horizon, On(DayOfWeek.Saturday), Clock(17, 30), 30, CarriageRule.LocalOnly, Taped, "nashville", Approx);
		Syndicate(porter, 1960, 10, DayOfWeek.Saturday, Clock(17, 30), CountryMarkets);
		Bill(porter, D(1960, 9, 10), 1, 1);
		porter.StandingFloor = 0.05f;
		programs.Add(porter);

		var wilburn = Local("wilburn_brothers", "The Wilburn Brothers Show", "Doyle and Teddy Wilburn", "WSM-TV", "nashville", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"A syndicated country half hour; Loretta Lynn was the regular girl singer.", new[] { 0.30f, 0.30f, 0.60f, 1.00f }, 1, false, 8, CountryFit, Approx,
			"Syndicated from Nashville from 1963; market list a composite.");
		Air(wilburn, D(1963, 1, 5), Horizon, On(DayOfWeek.Sunday), Clock(13), 30, CarriageRule.LocalOnly, Taped, "nashville", Approx);
		Syndicate(wilburn, 1963, 3, DayOfWeek.Sunday, Clock(13), CountryMarkets);
		Bill(wilburn, D(1963, 1, 5), 0, 1);
		wilburn.StandingFloor = 0.05f;
		programs.Add(wilburn);

		var tubb = Local("ernest_tubb", "The Ernest Tubb Show", "Ernest Tubb", "WSM-TV", "nashville", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"The Texas Troubadours and one guest.", new[] { 0.25f, 0.20f, 0.50f, 1.00f }, 1, false, 8, CountryFit, Approx, "Syndicated from Nashville, 1965-68.");
		Air(tubb, D(1965, 1, 9), D(1968, 12, 28), On(DayOfWeek.Saturday), Clock(18), 30, CarriageRule.LocalOnly, Taped, "nashville", Approx);
		Syndicate(tubb, 1965, 3, DayOfWeek.Saturday, Clock(18), CountryMarkets);
		Bill(tubb, D(1965, 1, 9), 0, 1);
		tubb.StandingFloor = 0.05f;
		programs.Add(tubb);

		var anderson = Local("bill_anderson", "The Bill Anderson Show", "Bill Anderson", "WSM-TV", "nashville", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"The Po' Boys, Jan Howard and a guest.", new[] { 0.25f, 0.25f, 0.55f, 1.00f }, 1, false, 8, CountryFit, Approx, "Syndicated from Nashville from 1965.");
		Air(anderson, D(1965, 9, 11), Horizon, On(DayOfWeek.Saturday), Clock(16, 30), 30, CarriageRule.LocalOnly, Taped, "nashville", Approx);
		Syndicate(anderson, 1965, 11, DayOfWeek.Saturday, Clock(16, 30), CountryMarkets);
		Bill(anderson, D(1965, 9, 11), 1, 0);
		programs.Add(anderson);

		// Rhythm and soul.
		var night = Local("night_train", "Night Train", "Noble Blackwell", "WLAC-TV", "nashville", TvProgramKind.MusicShow, PerformanceMethod.Mime,
			"Rhythm-and-blues acts and the Night Train dancers; most guests mime, some play live.", new[] { 0.10f, 1.00f, 0.80f, 0.20f }, 2, true, 4, SoulFit, Doc,
			"WLAC-TV Nashville, Oct 1964 – 1966 (Wikipedia). Night and hour approximate.");
		Air(night, D(1964, 10, 3), D(1966, 6, 25), On(DayOfWeek.Saturday), Clock(22, 30), 60, CarriageRule.LocalOnly, Taped, "nashville", Approx);
		programs.Add(night);

		var beat = Local("the_beat", "The !!!! Beat", "Bill \"Hoss\" Allen", "WFAA-TV", "dallas", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"Soul acts sing live with Clarence \"Gatemouth\" Brown's house band.", new[] { 0.10f, 0.90f, 0.90f, 0.30f }, 3, true, 6, SoulFit, Doc,
			"Syndicated soul music show, 1966 (Wikipedia). Production city and markets approximate.");
		Air(beat, D(1966, 1, 8), D(1966, 7, 30), On(DayOfWeek.Saturday), Clock(23), 60, CarriageRule.LocalOnly, Taped, "dallas", Approx);
		Syndicate(beat, 1966, 2, DayOfWeek.Saturday, Clock(23), ("houston", 1), ("new_orleans", 1), ("memphis", 2), ("atlanta", 2), ("nashville", 2),
			("st_louis", 3), ("chicago", 3), ("detroit", 3), ("cleveland", 4), ("philadelphia", 4), ("new_york", 4), ("los_angeles", 4), ("baltimore", 4));
		Bill(beat, D(1966, 1, 8), 2, 1);
		beat.StandingFloor = 0.08f;
		programs.Add(beat);

		var soul = Local("soul", "Soul!", "Ellis Haizlip", "WNDT Channel 13", "new_york", TvProgramKind.MusicShow, PerformanceMethod.LiveBandAndVocal,
			"Public television's Black variety hour: live music, poetry and long conversation.", new[] { 0.05f, 0.40f, 0.70f, 0.40f }, 2, true, 8, SoulFit, Doc,
			"WNDT/WNET New York from Sep 12 1968 (Wikipedia). Night approximate.");
		Air(soul, D(1968, 9, 12), Horizon, On(DayOfWeek.Thursday), Clock(21), 60, CarriageRule.LocalOnly, Live, "new_york", Approx);
		Bill(soul, D(1968, 9, 12), 1, 1);
		soul.StandingFloor = 0.08f;
		programs.Add(soul);

		// More documented teen dance shows.
		var cole = Local("clay_cole", "The Clay Cole Show", "Clay Cole", "WNTA / WPIX", "new_york", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"Saturday night dancers and a stack of guests miming the week's records.", new[] { 0.30f, 1.30f, 0.40f, 0.08f }, 3, true, 4, TeenFit, Doc,
			"New York, Sep 1959 – Dec 16 1967 (Wikipedia). Night and hour approximate.");
		Air(cole, D(1959, 9, 5), D(1967, 12, 16), On(DayOfWeek.Saturday), Clock(19), 60, CarriageRule.LocalOnly, Live, "new_york", Approx);
		programs.Add(cole);

		var grant = Local("milt_grant", "The Milt Grant Show", "Milt Grant", "WTTG", "washington", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"Washington's after-school dance party.", new[] { 0.30f, 1.30f, 0.25f, 0.06f }, 1, true, 4, TeenFit, Approx, "WTTG Washington, 1956 – 1961.");
		Air(grant, D(1956, 9, 3), D(1961, 6, 30), Weekdays, Clock(16), 90, CarriageRule.LocalOnly, Live, "washington", Approx);
		programs.Add(grant);

		var swingin = Local("swingin_time", "Swingin' Time", "Robin Seymour", "CKLW-TV", "detroit", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"From the Windsor station across the river: Detroit's dance party, heavy on Motown.", new[] { 0.30f, 1.30f, 0.30f, 0.06f }, 2, true, 4, TeenFit, Doc,
			"CKLW-TV Windsor/Detroit, 1965 – 1968 (Wikipedia). Hour approximate.");
		Air(swingin, D(1965, 1, 4), D(1968, 12, 27), Weekdays, Clock(16, 30), 60, CarriageRule.LocalOnly, Live, "detroit", Approx);
		programs.Add(swingin);

		var shebang = Local("shebang", "Shebang", "Casey Kasem", "KTLA", "los_angeles", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"An after-school dance show; guests mime.", new[] { 0.30f, 1.10f, 0.25f, 0.05f }, 1, true, 4, TeenFit, Approx, "KTLA Los Angeles, 1964 – 1965.");
		Air(shebang, D(1964, 6, 1), D(1965, 12, 31), Weekdays, Clock(17), 30, CarriageRule.LocalOnly, Taped, "los_angeles", Approx);
		programs.Add(shebang);

		var shivaree = Local("shivaree", "Shivaree", "Gene Weed", "KABC-TV", "los_angeles", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"A syndicated Hollywood dance hour with three or four acts.", new[] { 0.30f, 1.20f, 0.35f, 0.06f }, 3, true, 6, TeenFit, Doc,
			"KABC Los Angeles, syndicated, Apr 1965 – May 1966 (Wikipedia). Markets approximate.");
		Air(shivaree, D(1965, 4, 3), D(1966, 5, 28), On(DayOfWeek.Saturday), Clock(17), 60, CarriageRule.LocalOnly, Taped, "los_angeles", Approx);
		Syndicate(shivaree, 1965, 6, DayOfWeek.Saturday, Clock(17), ("san_francisco", 2), ("seattle", 2), ("portland", 3), ("denver", 3), ("phoenix", 2),
			("dallas", 3), ("houston", 3), ("chicago", 4), ("detroit", 4), ("new_york", 4), ("atlanta", 4));
		programs.Add(shivaree);

		var goGo = Local("hollywood_a_go_go", "Hollywood a Go Go", "Sam Riddle", "KHJ-TV", "los_angeles", TvProgramKind.LocalTeenDance, PerformanceMethod.Mime,
			"The Gazzarri Dancers and a long bill of mimed guests.", new[] { 0.30f, 1.20f, 0.35f, 0.06f }, 3, true, 6, TeenFit, Doc,
			"KHJ Los Angeles, syndicated, 1965 – 1966 (Wikipedia). Markets approximate.");
		Air(goGo, D(1965, 1, 2), D(1966, 6, 25), On(DayOfWeek.Saturday), Clock(18), 60, CarriageRule.LocalOnly, Taped, "los_angeles", Approx);
		Syndicate(goGo, 1965, 3, DayOfWeek.Saturday, Clock(18), ("san_francisco", 2), ("san_antonio", 3), ("seattle", 3), ("salt_lake_city", 3),
			("kansas_city", 4), ("st_louis", 4), ("boston", 4), ("miami", 4));
		programs.Add(goGo);
	}
}
