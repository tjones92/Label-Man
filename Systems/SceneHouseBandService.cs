using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>House bands (SimTools/ContactNetworkDirective.md §4): an act that can't back itself (fewer than two
/// rhythm players) on a club, listening-room or roadhouse bill is backed by local players. They come from members of
/// local acts and pooled players based in the town. A room's regulars and the singer's contacts are preferred, so a
/// standing house band forms. Backers are reserved like performers, paid per player, and linked as contacts.
/// <c>--disable-scene-house-bands</c> turns it off.</summary>
public static class SceneHouseBandService {
	public const int BandSize = 3;
	/// <summary>A working player: below the session bar (0.68), because a house band reads charts less than a studio.</summary>
	public const float SkillBar = 0.55f;
	public const float RegularBonus = 1f, ContactBonus = 0.5f;
	public const int RegularSaturation = 20;

	private static readonly Dictionary<string, List<Musician>> players = new(StringComparer.Ordinal);
	private static int playersWeek = int.MinValue;

	public static bool Backs(SceneRoomKind kind) => kind is SceneRoomKind.Club or SceneRoomKind.ListeningRoom or SceneRoomKind.Roadhouse;
	public static bool Needs(SimulatedArtist act, SceneRoomProfile room) =>
		LocalScenes.HouseBands && room != null && Backs(room.Kind) && SessionEmploymentService.NeedsCrew(act);

	/// <summary>Backers for one set, chosen at scheduling. Nobody already working another room that day or this hour.</summary>
	public static List<string> Pick(SceneRoomProfile room, SimulatedArtist act, int day, int hour, string[] people,
		Dictionary<string, string> personPlaces, HashSet<string> reservations, Func<string, string, int> regularShows) {
		var pool = Players(room.PlaceId, day / 7);
		return pool
			.Where(m => m.lifeState == MemberLifeState.Active && !people.Contains(m.personId) &&
				!reservations.Contains($"{day}|{hour}|{m.personId}") &&
				!(personPlaces.TryGetValue($"{day}|{m.personId}", out var place) && place != room.Id))
			.Select(m => (m, score: RegularBonus * Math.Min(1f, regularShows(room.Id, m.personId) / (float)RegularSaturation)
				+ (people.Any(p => ContactNetworkService.Edge(p, m.personId) is { Fallout: false }) ? ContactBonus : 0f)
				+ m.technicalSkill + 0.1f * BandLife.Unit($"house|{room.Id}|{day}|{m.personId}")))
			.OrderByDescending(x => x.score).ThenBy(x => x.m.personId, StringComparer.Ordinal)
			.Take(BandSize).Select(x => x.m.personId).ToList();
	}

	/// <summary>The town's rhythm players this week: members of acts present there and pooled players based there.</summary>
	private static List<Musician> Players(string place, int week) {
		if (week != playersWeek) { players.Clear(); playersWeek = week; }
		if (players.TryGetValue(place, out var list)) return list;
		list = new List<Musician>();
		foreach (string id in LocalScenePersistenceService.MembersOf(place)) {
			var a = ArtistManager.Instance?.GetArtist(id);
			if (a?.members == null) continue;
			foreach (Musician m in a.members) if (m.isActive && Capable(m)) list.Add(m);
		}
		foreach (PooledPerson p in PersonPool.Ordered())
			if (LocalScenePersistenceService.SceneIdFor(p.person.geography?.basePlaceId) == place && Capable(p.person)) list.Add(p.person);
		players[place] = list;
		return list;
	}
	private static bool Capable(Musician m) => m.lifeState == MemberLifeState.Active && m.technicalSkill >= SkillBar &&
		SessionEmploymentService.IsRhythmPlayer(m);

	public static void Reset() { players.Clear(); playersWeek = int.MinValue; }
}
