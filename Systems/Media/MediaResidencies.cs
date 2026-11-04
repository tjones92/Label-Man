using System;
using System.Collections.Generic;
using System.Linq;

public enum ResidencyKind {
	/// <summary>A resident spot on every original episode of an existing program; takes no guest slot.</summary>
	Regular,
	/// <summary>A network half hour built around the act (The Monkees, Here Come the Brides).</summary>
	Vehicle,
}

/// <summary>One act's engagement on a series for a season (CelebrityFanbaseDesign.md §3.3.1). Media-owned.</summary>
public sealed class MediaResidency {
	public string Id, ProgramId, ArtistId, ArtistName, LabelId;
	public ResidencyKind Kind;
	public int Chair;
	/// <summary>Inclusive day indexes of the engagement.</summary>
	public int FromDay, ToDay;
	/// <summary>A vehicle's season on the air, 1 or 2; a regular's count of seasons in the same chair.</summary>
	public int Season = 1;
	public bool Ended;
	/// <summary>A vehicle's network, weeknight and the September its season opens.</summary>
	public TvNetwork Network;
	public DayOfWeek Night;
	public int SeasonYear;
	public string ShowName;
	// Telemetry, and the vehicle's renewal test.
	public float RecognitionAtStart, FollowingAtStart, Score;
	public bool HadTop40Before, Top40During;
	public int FirstTop40Day = -1, Episodes;
	public double SeriesJoiners;
	// Phase 3b: the player's engagement and the terms it was signed on (§3.3.2). Rival residencies keep the defaults.
	public bool Player, ShowSongs, Blocks, Daily;
	public float FeeMultiple = 1f;
	/// <summary>Share of each engaged week the taping takes from the act's diary (0 for rival acts).</summary>
	public float DiaryShare;
	/// <summary>Signed inside the causal window: the performers' fees are paid per episode (§15.2).</summary>
	public bool CausalTerms;
	/// <summary>Last day whose episodes were paid, and the last month the office was told about.</summary>
	public int PaidThroughDay = -1, ReportedMonth = -1;
}

/// <summary>What the label answers a residency offer with (§3.3.2). The producer's own terms are <see cref="Producer"/>.</summary>
public struct ResidencyTerms {
	public bool Trial, ShowSongs, Blocks;
	public float FeeMultiple;
	public static ResidencyTerms Producer(ResidencyKind kind) => new() { FeeMultiple = 1f, ShowSongs = kind == ResidencyKind.Vehicle };
	public bool Same(ResidencyTerms o) => Trial == o.Trial && ShowSongs == o.ShowSongs && Blocks == o.Blocks && Math.Abs(FeeMultiple - o.FeeMultiple) < 1e-4f;
}

public enum ResidencyOfferStatus { Open, Accepted, Declined, Lapsed }

/// <summary>A producer's or network's offer of a residency to one of the label's acts (§3.3.2). The chair waits for the answer.</summary>
public sealed class MediaResidencyOffer {
	public string Id, ProgramId, ArtistId, ArtistName, ShowName;
	public ResidencyKind Kind;
	public int Chair, OfferedDay, ExpiresDay, SeasonEndDay;
	public ResidencyOfferStatus Status;
	/// <summary>The producer already turned down one counter; only the original terms stand.</summary>
	public bool Countered;
	/// <summary>A declined or lapsed vehicle offer whose season was since offered elsewhere.</summary>
	public bool Recast;
	public float Score, Leverage;
	public bool Daily;
	// A vehicle's network, night and season.
	public TvNetwork Network;
	public DayOfWeek Night;
	public int SeasonYear;
	// The estimate shown on the card.
	public double ReachLow, ReachHigh, TeenShare;
	public bool Open => Status == ResidencyOfferStatus.Open;
}

public enum ResidencyPitchStatus { Sent, Listed, Declined, Used, Lapsed }

/// <summary>The label putting an act up for a chair, or for a series of its own (§3.3.2). Listed acts score 1.25× at the next casting.</summary>
public sealed class MediaResidencyPitch {
	public string Id, ProgramId, ArtistId, ArtistName, Reason;
	public int SentDay, AnswerDay, ExpiresDay;
	public ResidencyPitchStatus Status;
}

/// <summary>
/// Celebrity fanbase Phase 3a (CelebrityFanbaseDesign.md §3.3.1), behind <c>--media-residencies</c>: rival acts cast as
/// regulars on the documented teen and country-pop series, and from 1966 a network half hour built around a hot teen act.
/// A resident's regular viewers convert to followers every week (the series term), which registration, counting fresh
/// viewers only, can't do. Pure arithmetic here; the casting and booking live in MediaAtmosphereService.Residencies.
/// Draws only keyed values.
/// </summary>
public static class MediaResidencies {
	public const string Switch = "--media-residencies";
	private static bool configured, on;
	public static bool Active => on && MediaAtmosphere.Observing;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>())
			if (argument == Switch) on = true;
		configured = true;
	}

	internal static void SetForChecks(bool value) { on = value; configured = true; }

	/// <summary>Resident chairs on existing programs (§3.3.1 table). The programs' own dates bound them.</summary>
	public static readonly (string ProgramId, int Chairs)[] Chairs = {
		("shindig", 1), ("where_the_action_is", 2), ("happening_68", 1), ("jimmy_dean", 1),
	};

	/// <summary>Weekly viewers who join a resident's following (§3.3.1): a 12M-viewer vehicle converts about 45k young a week.</summary>
	public const float CSeries = 0.008f;
	/// <summary>Share of a resident's later airings that is new to it, per week of episodes (a returning guest gets 0.25).</summary>
	public const double NewSharePerWeek = 0.10;
	/// <summary>Least program fit for a resident, and a vehicle's least teen fit.</summary>
	public const float ChairFit = 0.8f, VehicleTeenFit = 0.9f;
	/// <summary>Recognition that alone qualifies an act without a recent hit or regional break.</summary>
	public const float RecognitionEvidence = 0.10f;
	/// <summary>Young followers that make a vehicle prospect without a fan episode, and the scale of following in the score.</summary>
	public const float VehicleFollowing = 100_000f, FollowingScale = 500_000f;
	public const int FirstVehicleYear = 1966, MaxVehicleSeasons = 2;
	/// <summary>A chair isn't cast for less than this many days of program left.</summary>
	public const int MinChairDays = 56;
	/// <summary>The incumbent's edge when its chair is recast.</summary>
	public const float IncumbentBonus = 1.5f;

	/// <summary>Producer's ranking: fit × (0.5 + recognition + young following ÷ 500k) × a keyed spread in [0.55, 1.45].</summary>
	public static float Score(float fit, float recognition, float youngFollowing, float spread) =>
		fit * (0.5f + Math.Clamp(recognition, 0f, 1f) + Math.Max(0f, youngFollowing) / FollowingScale) * (0.55f + 0.9f * Math.Clamp(spread, 0f, 1f));

	/// <summary>Fresh share of a resident's returning airing: <see cref="NewSharePerWeek"/> spread over the week's episodes.</summary>
	public static double ReturningNewShare(BroadcastScheduleVersion version) {
		if (version?.Days == null || version.Days.Length == 0) return NewSharePerWeek;
		double perWeek = version.Days.Length / (double)Math.Max(1, version.IntervalWeeks);
		return NewSharePerWeek / Math.Max(1.0, perWeek);
	}

	/// <summary>
	/// Adults convert at a quarter of the young rate. Calibration 1 (Phase 3 notes): at the full rate a folk act on the Jimmy
	/// Dean Show's adult primetime audience built a 1.9M following in one season and 4M in three, the largest in either world.
	/// </summary>
	public const float AdultSeriesShare = 0.25f;
	/// <summary>A daily show's week reaches more different people than its best episode, up to twice as many.</summary>
	public const double MaxWeekReach = 2.0;

	/// <summary>The week's different viewers: the best episode × √episodes, at most <see cref="MaxWeekReach"/>×.</summary>
	public static double WeekViewers(double bestEpisode, int episodes) =>
		Math.Max(0, bestEpisode) * Math.Min(MaxWeekReach, Math.Sqrt(Math.Max(1, episodes)));

	/// <summary>One audience's series joiners in a region: viewers × audience share × fit × performance × <see cref="CSeries"/>
	/// (× <see cref="AdultSeriesShare"/> for adults).</summary>
	public static float SeriesJoiners(double viewers, double audienceShare, float fit, float performance, bool young) =>
		(float)(Math.Max(0, viewers) * Math.Clamp(audienceShare, 0, 1) * Math.Clamp(fit, 0f, 1f) * Math.Max(0f, performance) * CSeries
			* (young ? 1f : AdultSeriesShare));

	/// <summary>The end of a chair's season: the last day of August after at least 13 weeks, or the program's end if sooner.</summary>
	public static int SeasonEndDay(TvProgram program, GameDate from) {
		int fromDay = BroadcastExposure.DayIndex(from);
		int year = from.month >= 6 ? from.year + 1 : from.year;
		int end = BroadcastExposure.DayIndex(new GameDate(year, 8, 31));
		if (end - fromDay < 91) end = BroadcastExposure.DayIndex(new GameDate(year + 1, 8, 31));
		int last = program.Schedule.Count == 0 ? end : program.Schedule.Max(v => BroadcastExposure.DayIndex(v.Last));
		return Math.Min(end, last);
	}

	/// <summary>A vehicle's title: a group named "The ..." is its own show, anyone else gets "The ... Show".</summary>
	public static string VehicleTitle(string stageName) =>
		string.IsNullOrEmpty(stageName) ? "The New Show" : stageName.StartsWith("The ", StringComparison.Ordinal) ? stageName : "The " + stageName + " Show";

	/// <summary>A vehicle's first night: the first <paramref name="night"/> on or after September 12 of its year.</summary>
	public static GameDate VehiclePremiere(int year, DayOfWeek night) {
		var d = new GameDate(year, 9, 12);
		while (d.DayOfWeek != night) d = d.AddDays(1);
		return d;
	}

	/// <summary>
	/// The program a vehicle's seasons make: a taped primetime half hour at 7:30 Eastern on its night, originals from
	/// mid-September to April and reruns May to August, no guests. Rebuilt from the saved residencies, never saved itself.
	/// </summary>
	public static TvProgram BuildVehicle(IReadOnlyList<MediaResidency> seasons) {
		MediaResidency first = seasons[0];
		var p = new TvProgram {
			Id = first.ProgramId, Name = first.ShowName, HostName = first.ArtistName, Network = first.Network, Kind = TvProgramKind.NationalTeen,
			Method = PerformanceMethod.Mime, MethodNote = "A filmed half hour built around the act; the songs are mimed to the records.",
			Attraction = new[] { 0.70f, 1.00f, 0.55f, 0.35f }, GuestSlots = 0, RebookWeeks = 52, ChartCeiling = 0,
			GenreFit = MediaOutletCatalog.TeenFit, Confidence = SourceConfidence.Fictional, Vehicle = true,
			Source = "A fictional network series made for a simulated act (CelebrityFanbaseDesign.md §3.3.1).",
		};
		foreach (MediaResidency s in seasons.OrderBy(s => s.SeasonYear)) {
			GameDate premiere = VehiclePremiere(s.SeasonYear, s.Night);
			p.Schedule.Add(new BroadcastScheduleVersion {
				First = premiere, Last = new GameDate(s.SeasonYear + 1, 8, 31), Days = new[] { s.Night }, StartMinute = 19 * 60 + 30, DurationMinutes = 30,
				Carriage = CarriageRule.NetworkPrimetime, Broadcast = BroadcastMethod.Taped, ProductionCityId = "los_angeles",
				RepeatMonths = new[] { 5, 6, 7, 8 }, Confidence = SourceConfidence.Fictional, Note = "Filmed in Hollywood; reruns from May.",
			});
		}
		return p;
	}

	// ---- Phase 3b: the player's residency (§3.3.2) ----------------------------------------------------------------

	/// <summary>The program id a label pitches for "a series of their own".</summary>
	public const string VehiclePitch = "vehicle";
	/// <summary>A listed pitch's weight at casting, and how long the producer keeps the act on the list.</summary>
	public const float PitchBonus = 1.25f;
	public const int PitchListDays = 182, OfferDays = 7, TrialWeeks = 13;
	/// <summary>Scale per performer per episode (simplified assumption, matching a network guest date).</summary>
	public const float WeeklyScale = 150f, DailyScale = 60f;
	/// <summary>A label's road weeks in a year, which the diary weeks come out of.</summary>
	public const int RoadWeeksPerYear = 48;
	/// <summary>Band-life road load per diary week: the rate of a club week.</summary>
	public const float DiaryLoadPerWeek = 0.015f;
	public static readonly float[] FeeMultiples = { 1f, 1.5f, 2f };

	/// <summary>A program that airs three or more days a week books its regulars as a daily chair.</summary>
	public static bool IsDaily(BroadcastScheduleVersion version) =>
		version?.Days != null && version.Days.Length / (double)Math.Max(1, version.IntervalWeeks) >= 3;

	public static float ScalePerEpisode(bool daily) => daily ? DailyScale : WeeklyScale;

	/// <summary>Share of each engaged week the taping takes (§3.3.2 diary table).</summary>
	public static float DiaryShare(ResidencyKind kind, bool daily, bool blocks) =>
		kind == ResidencyKind.Vehicle ? (blocks ? 0.50f : 0.80f) : daily ? (blocks ? 0.30f : 0.60f) : (blocks ? 0.15f : 0.35f);

	/// <summary>The producer's yes to a counter: the product of each term's factor × leverage (1-1.5), at most 0.95.
	/// The producer's own terms are always accepted.</summary>
	public static float AcceptanceChance(ResidencyKind kind, bool daily, ResidencyTerms terms, float leverage) {
		if (terms.Same(ResidencyTerms.Producer(kind))) return 1f;
		float p = 1f;
		if (terms.Trial && kind == ResidencyKind.Regular) p *= 0.9f;
		p *= terms.FeeMultiple >= 1.99f ? 0.35f : terms.FeeMultiple >= 1.49f ? 0.65f : 1f;
		if (kind == ResidencyKind.Vehicle && !terms.ShowSongs) p *= 0.6f;
		if (kind == ResidencyKind.Regular && terms.ShowSongs) p *= 1.15f;
		if (terms.Blocks) p *= kind == ResidencyKind.Regular && !daily ? 0.75f : 0.5f;
		return Math.Min(0.95f, p * Math.Clamp(leverage, 1f, 1.5f));
	}

	/// <summary>Leverage: the act's casting score over the runner-up's, 1 to 1.5 (1.5 when nobody else qualified).</summary>
	public static float Leverage(float best, float runnerUp) => runnerUp <= 0f ? 1.5f : Math.Clamp(best / runnerUp, 1f, 1.5f);

	/// <summary>The engagement's days in a calendar year, as weeks × its diary share.</summary>
	public static float DiaryWeeks(MediaResidency r, int year) {
		if (r == null || r.DiaryShare <= 0f) return 0f;
		int from = Math.Max(r.FromDay, BroadcastExposure.DayIndex(new GameDate(year, 1, 1)));
		int to = Math.Min(r.ToDay, BroadcastExposure.DayIndex(new GameDate(year, 12, 31)));
		return to < from ? 0f : (to - from + 1) / 7f * r.DiaryShare;
	}

	/// <summary>Vehicles for every saved vehicle residency, one program per act's series.</summary>
	public static List<TvProgram> Vehicles(IEnumerable<MediaResidency> residencies) =>
		(residencies ?? Enumerable.Empty<MediaResidency>()).Where(r => r.Kind == ResidencyKind.Vehicle)
			.GroupBy(r => r.ProgramId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
			.Select(g => BuildVehicle(g.ToList())).ToList();
}
