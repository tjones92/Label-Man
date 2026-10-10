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
	}

	private static string AccountKey(string person, int year) => year + "|" + person;
	private static string RegularKey(string label, string person) => label + "|" + person;

	public static bool NeedsCrew(SimulatedArtist act) =>
		act?.members != null && act.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active && Array.IndexOf(RhythmRoles, m.primaryRole) >= 0) < 2;

	/// <summary>Called once per released record. Player-owned records hire through the Band Room instead.</summary>
	public static void OnRecordReleased(Record record, AILabel label) {
		if (!Enabled || record == null || record.isPlayerOwned || ArtistManager.Instance == null) return;
		// Most release paths pass no label; the record knows its own.
		label ??= ChartManager.Instance?.GetLabelById(record.labelId);
		if (label == null) return;
		var act = ArtistManager.Instance.GetArtist(record.artistId);
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
			for (int j = i + 1; j < crew.Count; j++) ContactNetworkService.Link(crew[i].personId, crew[j].personId, ContactKind.Session, year, sessions);
			foreach (Musician member in act.members)
				if (member.isActive) ContactNetworkService.Link(crew[i].personId, member.personId, ContactKind.Session, year, sessions);
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
		Array.IndexOf(RhythmRoles, m.primaryRole) >= 0;

	public static float Pay(string person, int year) => person != null && accounts.TryGetValue(AccountKey(person, year), out var a) ? a.Pay : 0f;
	public static bool WorkedIn(string person, int year) => person != null && accounts.ContainsKey(AccountKey(person, year));

	/// <summary>Year end: pooled players bank their session pay (members do in the act pass), and old ledgers go.</summary>
	public static void OnYearEnd(int year) {
		foreach (PooledPerson p in PersonPool.Ordered()) MemberWealthService.OnPooledYear(p.person, Pay(p.person.personId, year));
		foreach (var key in accounts.Where(kv => kv.Value.Year < year - 1).Select(kv => kv.Key).ToList()) accounts.Remove(key);
		community.Clear(); communityYear = int.MinValue;
	}

	public static void Reset() {
		accounts.Clear(); regulars.Clear(); weekLoad.Clear(); community.Clear();
		loadWeek = communityYear = int.MinValue; RecordsCrewed = RecordsUncrewed = 0;
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
