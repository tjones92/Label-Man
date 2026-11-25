using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Who an act's manager is (SimTools/SigningRealismBreakdown.md §10). Player-facing only.</summary>
public enum ManagerKind { None, Family, ClubBooker, Deejay, Songwriter, Agency, JukeboxOperator, PersonalManager, Hustler }

/// <summary>One read of who manages an act: the person you'd sit down with, what he is to them, and where.</summary>
public sealed class ManagerRead {
	public ManagerKind Kind;
	public ManagerArchetype Archetype;
	/// <summary>The person: a member's relative, the room's booker, or the act's generated manager name.</summary>
	public string Name;
	/// <summary>What he is to the act, as a clause: "Sal's father", "books the Michigan Roadhouse".</summary>
	public string Title;
	/// <summary>Where a sit-down happens: "the Caldwells' kitchen table", a room's name, "the WJLB studios".</summary>
	public string Place;
	/// <summary>Club booker: the room he books, so the sit-down falls on a night it's open.</summary>
	public string RoomId;
	public bool Female;
	/// <summary>Family: the member he's related to.</summary>
	public string Member;
	public string He => Female ? "she" : "he";
	public string Him => Female ? "her" : "him";
	public string His => Female ? "her" : "his";
}

/// <summary>
/// The manager identity: who is across the table, layered on the archetype the act already rolled. The archetype
/// (ManagerProfile) stays the deal posture the AI economy reads, so its numbers, the generation roll and every AI term
/// sheet are unchanged. This is a pure read: no draw, nothing stored. Shares within a rule are keyed on the world seed and
/// the act's id (BandLife.Unit), so a save always reads the same manager.
///
/// Rules run in order and the act takes the first that fits (the archetypes each may sit on are in brackets):
///   1 Family [Hustler, Svengali]: a member under 21, or members sharing a surname; half of those.
///   2 Club booker [Hustler, Shark]: the act holds a standing engagement; it's that room's booker.
///   3 Deejay [Hustler, Shark]: a station in the act's town; a share of those.
///   4 Songwriter-producer [Svengali, Visionary]: a Svengali on an act that writes little, or some vocal groups.
///   5 Agency man [Shark, Visionary]: only an act with a record career behind it (a past contract, or Rising and up).
///   6 Jukebox-route operator [Shark]: only R&amp;B, doo-wop, blues, soul and rock and roll acts in the big jukebox
///     cities; a share of those.
///   7 Personal manager [Shark, Svengali, Visionary]: the fallback.
///   8 Neighbourhood hustler [Hustler]: the fallback.
/// </summary>
public static class ManagerIdentity {
	public const float FamilyShare = .5f, DeejayShare = .15f, VocalGroupSongwriterShare = .4f, JukeboxShare = .5f;
	public const float SongwriterWritingBelow = .4f;

	private static readonly HashSet<string> JukeboxCities = new(StringComparer.Ordinal) { "new_york", "chicago", "detroit", "philadelphia" };
	private static readonly HashSet<Genre> JukeboxGenres = new() { Genre.RnB, Genre.DooWop, Genre.Blues, Genre.Soul, Genre.RockAndRoll };
	// Fictional agencies: the period had a handful of big booking offices in New York, Chicago and Los Angeles.
	private static readonly string[] Agencies = { "Consolidated Artists", "National Talent Bureau", "Continental Booking", "Allied Artists Agency" };
	private static readonly string[] Fathers = { "Harold", "Walter", "Frank", "Earl", "Lloyd", "Clarence", "Roy", "Murray", "Leonard", "Otis", "Herman", "Vernon" };
	private static readonly string[] Mothers = { "Mae", "Ruth", "Hazel", "Edna", "Lillian", "Gladys", "Pearl", "Irene", "Bernice", "Viola" };
	private static readonly string[] Brothers = { "Eddie", "Jimmy", "Bobby", "Ray", "Charlie", "Gene", "Tommy", "Lou" };
	private static readonly string[] Hustles = {
		"sells cars and knows a booker", "cuts hair and books their dates", "a friend from the neighborhood who books their dates",
		"tends bar and handles their dates", "runs a record shop and books their dates" };

	private static float Unit(SimulatedArtist act, string salt) => BandLife.Unit($"manager-identity|{act.artistId}|{salt}");
	private static T Pick<T>(SimulatedArtist act, string salt, IReadOnlyList<T> list) => list[Math.Min(list.Count - 1, (int)(Unit(act, salt) * list.Count))];

	/// <summary>Who manages this act today, or null when nobody does.</summary>
	public static ManagerRead Read(SimulatedArtist act, int day) {
		if (act == null || act.manager == ManagerArchetype.None) return null;
		var kind = act.manager;
		string name = string.IsNullOrWhiteSpace(act.managerName) ? "their manager" : act.managerName.Trim();
		string town = LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId);
		int year = LocalSceneRoomService.Date(day).year;
		var people = (act.members ?? new()).Where(m => m != null && m.isActive && m.lifeState == MemberLifeState.Active).ToList();
		var r = new ManagerRead { Archetype = kind, Name = name };

		// 1 Family
		if (kind is ManagerArchetype.LocalHustler or ManagerArchetype.Svengali) {
			var minors = people.Where(m => m.birthYear > 0 && m.GetAge(year) < ActDisposition.MinorAge).OrderBy(m => m.GetAge(year))
				.ThenBy(m => m.personId, StringComparer.Ordinal).ToList();
			var shared = people.Where(m => !string.IsNullOrWhiteSpace(m.lastName)).GroupBy(m => m.lastName.Trim())
				.Where(g => g.Count() >= 2).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).FirstOrDefault();
			var member = minors.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.lastName)) ?? shared?.OrderBy(m => m.personId, StringComparer.Ordinal).First();
			if (member != null && Unit(act, "family") < FamilyShare) {
				float rel = Unit(act, "relation");
				string relation = rel < .55f ? "father" : rel < .75f ? "mother" : rel < .88f ? "uncle" : "older brother";
				string first = relation == "mother" ? Pick(act, "first", Mothers) : relation == "older brother" ? Pick(act, "first", Brothers) : Pick(act, "first", Fathers);
				string surname = member.lastName.Trim();
				r.Kind = ManagerKind.Family; r.Female = relation == "mother"; r.Name = $"{first} {surname}";
				r.Member = string.IsNullOrWhiteSpace(member.firstName) ? surname : member.firstName.Trim();
				r.Title = shared != null && shared.Key == surname && shared.Count() >= 2 && relation != "older brother"
					? $"the {surname}s' {relation}" : $"{r.Member}'s {relation}";
				r.Place = $"the {surname}s' kitchen table";
				return r;
			}
		}
		// 2 Club booker
		if (kind is ManagerArchetype.LocalHustler or ManagerArchetype.Shark &&
			LocalSceneRoomService.EngagementRoom(act.artistId, day) is { } roomId && SceneRoomCatalog.Get(roomId) is { } room &&
			!string.IsNullOrWhiteSpace(room.ContactName)) {
			r.Kind = ManagerKind.ClubBooker; r.Name = room.ContactName; r.Title = $"books {room.Name}"; r.Place = room.Name; r.RoomId = room.Id;
			return r;
		}
		// 3 Deejay
		if (kind is ManagerArchetype.LocalHustler or ManagerArchetype.Shark && town != null && Unit(act, "deejay") < DeejayShare) {
			string cityName = DistanceModel.GetCityById(town)?.name;
			var stations = (ChartManager.Instance?.AllReporterStations ?? Enumerable.Empty<RadioStation>())
				.Where(s => s != null && cityName != null && string.Equals(s.cityName, cityName, StringComparison.OrdinalIgnoreCase))
				.OrderBy(s => s.stationId, StringComparer.Ordinal).ToList();
			if (stations.Count > 0) {
				var station = Pick(act, "station", stations);
				r.Kind = ManagerKind.Deejay; r.Title = $"spins records at {station.callsign} and runs its record hops"; r.Place = $"the {station.callsign} studios";
				return r;
			}
		}
		// 4 Songwriter-producer
		bool vocalGroup = act.type == ArtistType.VocalGroup;
		if (kind == ManagerArchetype.Svengali && (act.songwritingAbility < SongwriterWritingBelow || vocalGroup) ||
			kind == ManagerArchetype.Visionary && vocalGroup && Unit(act, "songwriter") < VocalGroupSongwriterShare) {
			r.Kind = ManagerKind.Songwriter; r.Title = "writes their songs and produces their sessions"; r.Place = "the piano room at his publisher's";
			return r;
		}
		// 5 Agency man: only an act with a record career behind it.
		bool career = act.contractSequence > 0 || act.careerState is CareerState.Rising or CareerState.Established or CareerState.Star
			or CareerState.Superstar or CareerState.Declining;
		if (kind is ManagerArchetype.Shark or ManagerArchetype.Visionary && career) {
			string agency = Pick(act, "agency", Agencies);
			r.Kind = ManagerKind.Agency; r.Title = $"of {agency}, which books their dates"; r.Place = $"the {agency} office";
			return r;
		}
		// 6 Jukebox-route operator: only the music the boxes played, in the cities where the routes were money.
		if (kind == ManagerArchetype.Shark && town != null && JukeboxCities.Contains(town) && JukeboxGenres.Contains(act.primaryGenre) &&
			Unit(act, "jukebox") < JukeboxShare) {
			r.Kind = ManagerKind.JukeboxOperator; r.Title = "runs a jukebox route and puts money behind acts"; r.Place = "a back booth at a restaurant he owns a piece of";
			return r;
		}
		// 7 / 8 Fallbacks
		if (kind == ManagerArchetype.LocalHustler) {
			r.Kind = ManagerKind.Hustler; r.Title = Pick(act, "hustle", Hustles); r.Place = "the diner where he does his business";
		} else {
			r.Kind = ManagerKind.PersonalManager; r.Title = "their personal manager"; r.Place = "his office";
		}
		return r;
	}

	/// <summary>Who manages the act on today's date.</summary>
	public static ManagerRead Today(SimulatedArtist act) =>
		Read(act, LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate));

	/// <summary>The name to show for the act's manager: the identity's person, else the generated name.</summary>
	public static string NameOf(SimulatedArtist act) => Today(act)?.Name ?? act?.managerName;

	/// <summary>"Harold Caldwell, Sal's father" or "Sol Kramer, their personal manager".</summary>
	public static string Introduce(ManagerRead r) => r == null ? null : $"{r.Name}, {r.Title}";

	/// <summary>How the act says who handles them, when someone asks after a set. Family is the one they always say.</summary>
	public static string BandSays(ManagerRead r) => r?.Kind switch {
		ManagerKind.Family => $"{Cap(r.Title)} handles our business.",
		ManagerKind.ClubBooker => $"{r.Name} books us at {r.Place}, and he handles our business too.",
		null => null,
		_ => $"{r.Name} handles our business."
	};

	private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
