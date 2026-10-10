using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Paid studio work for musicians outside their own act (SimTools/ContactNetworkDirective.md). When an AI label
/// releases a record by an act that can't back itself (fewer than two rhythm players), it hires a crew in its home
/// city: the best local readers, preferring the label's regulars, so standing session communities form. The crew
/// is paid union scale, which reaches their wealth; crew members and the act come to know each other; and session
/// work keeps a pooled player in music. Record quality and label costs are untouched: label recording budgets
/// already stand for what sessions cost. <c>--disable-session-employment</c> turns it off.
/// </summary>
public static class SessionEmploymentService {
	/// <summary>AFM three-hour sideman scale: $434.21 in 2019 (AFM SRLA summary), deflated by CPI-U (255.7 to 29.6)
	/// to 1960 dollars, ~$50. Derived and provisional: no 1960s scale sheet was found. The Phonograph Record Labor
	/// Agreement was national, so no town adjustment.</summary>
	public const float ScalePerSession1960 = 50f;
	public const int SessionsPerSingle = 1, SessionsPerAlbum = 3;
	public const int CrewSize = 4, FolkCrewSize = 2;
	/// <summary>A strong player (band life's session-work bar) who reads well (the player's session-hire bar).</summary>
	public const float SkillBar = 0.68f, ReadingBar = 0.55f;
	/// <summary>Two sessions a day, five days: a working week in the studio. A physical limit, not a quota.</summary>
	public const int MaxSessionsPerWeek = 10;
	/// <summary>Score a label's regular earns at ten dates with it.</summary>
	public const float RegularBonus = 0.5f;
	public const int RegularSaturation = 10;

	private static readonly MusicianRole[] RhythmRoles = {
		MusicianRole.LeadGuitar, MusicianRole.RhythmGuitar, MusicianRole.Bass, MusicianRole.Drums,
		MusicianRole.Piano, MusicianRole.Organ, MusicianRole.MultiInstrumentalist };

	public static bool Enabled { get; private set; } = true;
	/// <summary>Sessions and house-band sets count as practice for an act member (MemberGrowthService).</summary>
	public static bool EmploymentGrowth { get; private set; } = true;
	public const float HoursPerSession = 3f;
	private static readonly Dictionary<string, SessionWorkAccount> accounts = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, SessionRegular> regulars = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, int> weekLoad = new(StringComparer.Ordinal);
	private static int loadWeek = int.MinValue;
	private static readonly Dictionary<string, List<Musician>> community = new(StringComparer.Ordinal);
	private static int communityYear = int.MinValue;

	// Diagnostics, not saved.
	public static int RecordsCrewed { get; private set; }
	public static int RecordsUncrewed { get; private set; }

	public static IReadOnlyCollection<SessionWorkAccount> Accounts => accounts.Values;
	public static IReadOnlyCollection<SessionRegular> Regulars => regulars.Values;

	public static void Configure(string[] args) {
		bool enable = args.Contains("--enable-session-employment", StringComparer.Ordinal);
		bool disable = args.Contains("--disable-session-employment", StringComparer.Ordinal);
		if (enable && disable) throw new ArgumentException("--enable-session-employment and --disable-session-employment cannot be used together.");
		Enabled = !disable;
		EmploymentGrowth = !args.Contains("--disable-employment-growth", StringComparer.Ordinal);
		RecordLift = !args.Contains("--disable-session-record-lift", StringComparer.Ordinal);
	}

	private static string AccountKey(string person, int year) => year + "|" + person;
	private static string RegularKey(string label, string person) => label + "|" + person;

	public static bool IsRhythmPlayer(Musician m) => m != null && Array.IndexOf(RhythmRoles, m.primaryRole) >= 0;
	public static bool NeedsCrew(SimulatedArtist act) =>
		act?.members != null && act.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active && IsRhythmPlayer(m)) < 2;

	/// <summary>A crew's score: mean skill and reading. The record lift is measured against a typical crew.</summary>
	public static float CrewScore(IEnumerable<Musician> crew) => crew.Select(m => 0.5f * m.technicalSkill + 0.5f * m.sightReading).DefaultIfEmpty(0f).Average();
	/// <summary>The typical crew's score (net/house runs; see the directive). A crew above it lifts the record's
	/// production, one below drags it: zero-centred, because existing record calibration already stands for the
	/// session players every vocal record used.</summary>
	public const float TypicalCrewScore = 0f;
	/// <summary>Production change per point of crew score above typical (the Band Room lift is 0.10 per point of skill).</summary>
	public const float RecordLiftPerPoint = 0.10f;
	public static bool RecordLift { get; private set; }
	// Diagnostics, not saved: crew scores, to size TypicalCrewScore.
	public static double CrewScoreSum { get; private set; }

	/// <summary>Called once per AI record as it is made (GenerateRecordFromArtist). Player-owned records hire
	/// through the Band Room instead.</summary>
	public static void OnRecordMade(Record record, AILabel label, SimulatedArtist act) {
		if (!Enabled || record == null || record.isPlayerOwned || label == null || ArtistManager.Instance == null) return;
		if (!NeedsCrew(act)) return;
		string place = LocalScenePersistenceService.SceneIdFor(label.geography?.basePlaceId);
		if (string.IsNullOrEmpty(place)) { RecordsUncrewed++; return; }
		var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int year = date.year, week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week != loadWeek) { weekLoad.Clear(); loadWeek = week; }
		int sessions = record.format == ReleaseFormat.Album ? SessionsPerAlbum : SessionsPerSingle;
		GenreFamily family = GenreCatalog.Get(GenreCatalog.MapLegacy(act.primaryGenre, year)).Family;
		int size = family == GenreFamily.Folk ? FolkCrewSize : CrewSize;
		var crew = Community(place, year)
			.Where(m => m.isActive || PersonPool.Contains(m.personId))
			.Where(m => m.lifeState == MemberLifeState.Active && !act.members.Contains(m) &&
				weekLoad.GetValueOrDefault(m.personId) + sessions <= MaxSessionsPerWeek)
			.Select(m => (m, score: m.sightReading + m.technicalSkill + RegularScore(label.labelId, m.personId)
				+ 0.1f * BandLife.Unit($"session|{record.recordId}|{m.personId}")))
			.OrderByDescending(x => x.score).ThenBy(x => x.m.personId, StringComparer.Ordinal)
			.Take(size).Select(x => x.m).ToList();
		if (crew.Count == 0) { RecordsUncrewed++; return; }
		RecordsCrewed++;
		float score = CrewScore(crew);
		CrewScoreSum += score;
		if (RecordLift && TypicalCrewScore > 0f)
			record.productionQuality = Math.Clamp(record.productionQuality + RecordLiftPerPoint * (score - TypicalCrewScore), 0f, 1f);
		float pay = sessions * ScalePerSession1960 * SceneLiveEconomics.PriceLevel(year);
		foreach (Musician m in crew) {
			weekLoad[m.personId] = weekLoad.GetValueOrDefault(m.personId) + sessions;
			string key = AccountKey(m.personId, year);
			if (!accounts.TryGetValue(key, out var account)) accounts[key] = account = new SessionWorkAccount { PersonId = m.personId, Year = year };
			account.Sessions += sessions;
			account.Pay += pay;
			string rk = RegularKey(label.labelId, m.personId);
			if (!regulars.TryGetValue(rk, out var regular)) regulars[rk] = regular = new SessionRegular { LabelId = label.labelId, PersonId = m.personId };
			regular.Sessions += sessions;
			regular.LastYear = year;
		}
		for (int i = 0; i < crew.Count; i++) {
			for (int j = i + 1; j < crew.Count; j++) ContactNetworkService.Link(crew[i].personId, crew[j].personId, ContactKind.Session, year, sessions, via: label.labelId);
			foreach (Musician member in act.members)
				if (member.isActive) ContactNetworkService.Link(crew[i].personId, member.personId, ContactKind.Session, year, sessions, via: label.labelId);
		}
	}

	private static float RegularScore(string label, string person) =>
		regulars.TryGetValue(RegularKey(label, person), out var r) ? RegularBonus * Math.Min(1f, r.Sessions / (float)RegularSaturation) : 0f;

	/// <summary>The town's session-capable people this year: members of acts based there, and pooled players based
	/// there. Rebuilt once a year; availability is rechecked at each date.</summary>
	private static List<Musician> Community(string place, int year) {
		if (year != communityYear) { community.Clear(); communityYear = year; }
		if (community.TryGetValue(place, out var list)) return list;
		list = new List<Musician>();
		foreach (string artistId in LocalSceneIdentityService.ArtistsBasedAt(place)) {
			var a = ArtistManager.Instance.GetArtist(artistId);
			if (a?.members == null || a.lifecycleStatus != ArtistLifecycleStatus.Active) continue;
			foreach (Musician m in a.members) if (m.isActive && Capable(m)) list.Add(m);
		}
		foreach (PooledPerson p in PersonPool.Ordered())
			if (LocalScenePersistenceService.SceneIdFor(p.person.geography?.basePlaceId) == place && Capable(p.person)) list.Add(p.person);
		community[place] = list;
		return list;
	}
	private static bool Capable(Musician m) =>
		m.lifeState == MemberLifeState.Active && m.technicalSkill >= SkillBar && m.sightReading >= ReadingBar &&
		IsRhythmPlayer(m);

	public static int Sessions(string person, int year) => person != null && accounts.TryGetValue(AccountKey(person, year), out var a) ? a.Sessions : 0;
	public static float Pay(string person, int year) => person != null && accounts.TryGetValue(AccountKey(person, year), out var a) ? a.Pay : 0f;
	public static bool WorkedIn(string person, int year) => person != null && accounts.ContainsKey(AccountKey(person, year));

	/// <summary>Year end: pooled players bank their session pay (members do in the act pass), and old ledgers go.</summary>
	public static void OnYearEnd(int year) {
		foreach (PooledPerson p in PersonPool.Ordered())
			MemberWealthService.OnPooledYear(p.person, Pay(p.person.personId, year) + LocalSceneRoomService.Backing(p.person.personId, year).Pay);
		foreach (var key in accounts.Where(kv => kv.Value.Year < year - 1).Select(kv => kv.Key).ToList()) accounts.Remove(key);
		community.Clear(); communityYear = int.MinValue;
	}

	public static void Reset() {
		accounts.Clear(); regulars.Clear(); weekLoad.Clear(); community.Clear();
		loadWeek = communityYear = int.MinValue; RecordsCrewed = RecordsUncrewed = 0; CrewScoreSum = 0;
	}
	public static void Capture(ContactNetworkSaveData data) {
		data.SessionWork = accounts.Values.OrderBy(a => a.Year).ThenBy(a => a.PersonId, StringComparer.Ordinal)
			.Select(a => new SessionWorkAccount { PersonId = a.PersonId, Year = a.Year, Sessions = a.Sessions, Pay = a.Pay }).ToList();
		data.Regulars = regulars.Values.OrderBy(r => r.LabelId, StringComparer.Ordinal).ThenBy(r => r.PersonId, StringComparer.Ordinal)
			.Select(r => new SessionRegular { LabelId = r.LabelId, PersonId = r.PersonId, Sessions = r.Sessions, LastYear = r.LastYear }).ToList();
	}
	public static void Rehydrate(ContactNetworkSaveData data) {
		foreach (var a in data.SessionWork ?? new())
			if (!string.IsNullOrEmpty(a.PersonId)) accounts[AccountKey(a.PersonId, a.Year)] = new SessionWorkAccount { PersonId = a.PersonId, Year = a.Year, Sessions = a.Sessions, Pay = a.Pay };
		foreach (var r in data.Regulars ?? new())
			if (!string.IsNullOrEmpty(r.LabelId) && !string.IsNullOrEmpty(r.PersonId))
				regulars[RegularKey(r.LabelId, r.PersonId)] = new SessionRegular { LabelId = r.LabelId, PersonId = r.PersonId, Sessions = r.Sessions, LastYear = r.LastYear };
	}
}
