using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Songs and arrangements travel with people (SimTools/ContactNetworkDirective.md §5.3). A person learns a song by
/// playing it: cutting it on a record (members and session crew alike) or backing it in a club. When an act chooses
/// a cover, the songs its members carried in from other acts are weighed beside the catalogue sample, with a bonus
/// for being known. A single's carried song brings its arrangement: the new cover is modelled on the record the
/// member played on rather than on the latest version. No RNG; a carried song is only ever a candidate the act's
/// existing source pools would allow. <c>--disable-repertoire-influence</c> turns it off.
/// </summary>
public static class RepertoireInfluenceService {
	/// <summary>How many songs a person keeps ready: a working player's set's worth.</summary>
	public const int CarryLimit = 8;
	/// <summary>Selection score bonus for a song a member already plays. A design input: big enough that a known song
	/// often beats a stranger of similar fit, never enough to beat a much better one.</summary>
	public const float KnownSongBonus = 0.25f;
	/// <summary>Album tracks carried per album: the promoted ones, which are the songs people remember.</summary>
	public const int AlbumTracksCarried = 2;

	public static bool Enabled { get; private set; } = true;

	// Diagnostics, not saved.
	public static int SongsLearned { get; private set; }
	public static int CarriedConsidered { get; private set; }
	public static int CarriedChosen { get; private set; }
	public static int ArrangementsCarried { get; private set; }
	public static readonly List<(string RecordId, string ArtistId, string SongId, string CarrierId, string ViaArtistId, string Source, int Year)> Choices = new();
	internal static bool LogChoices;

	public static void Configure(string[] args) {
		bool enable = args.Contains("--enable-repertoire-influence", StringComparer.Ordinal);
		bool disable = args.Contains("--disable-repertoire-influence", StringComparer.Ordinal);
		if (enable && disable) throw new ArgumentException("--enable-repertoire-influence and --disable-repertoire-influence cannot be used together.");
		Enabled = !disable;
		LogChoices = args.Contains("--contact-network-audit", StringComparer.Ordinal);
	}

	public static void Reset() { SongsLearned = CarriedConsidered = CarriedChosen = ArrangementsCarried = 0; Choices.Clear(); }

	/// <summary>A person now plays this song. Re-learning moves it to the most recent end; the oldest falls off.</summary>
	public static void Learn(Musician person, string songId, string recordId, string viaArtistId, int year) {
		if (!Enabled || person == null || string.IsNullOrEmpty(songId)) return;
		var list = person.carriedSongs ??= new List<CarriedSong>(CarryLimit);
		int at = list.FindIndex(c => c.songId == songId);
		if (at >= 0) {
			var known = list[at];
			list.RemoveAt(at);
			known.year = year;
			// The most recent playing is the version they know.
			if (recordId != null || known.viaArtistId != viaArtistId) { known.recordId = recordId; known.viaArtistId = viaArtistId; }
			list.Add(known);
			return;
		}
		if (list.Count >= CarryLimit) list.RemoveAt(0);
		list.Add(new CarriedSong { songId = songId, recordId = recordId, viaArtistId = viaArtistId, year = year });
		SongsLearned++;
	}

	/// <summary>A record was made: its members and session crew learn its song (a single), or its promoted tracks
	/// (an album).</summary>
	public static void OnRecordMade(Record record, SimulatedArtist act, IReadOnlyList<Musician> crew) {
		if (!Enabled || record == null || act?.members == null) return;
		int year = TimeManager.Instance?.CurrentDate.year ?? GameDate.StartDate.year;
		IEnumerable<(string song, string record)> songs = record.format == ReleaseFormat.Album
			? (record.album?.trackRefs ?? Array.Empty<AlbumTrack>()).Where(t => !string.IsNullOrEmpty(t?.songId))
				.Take(AlbumTracksCarried).Select(t => (t.songId, (string)null))
			: string.IsNullOrEmpty(record.songId) ? Enumerable.Empty<(string, string)>() : new[] { (record.songId, record.recordId) };
		foreach (var (song, recordId) in songs) {
			foreach (Musician m in act.members) if (m.isActive && m.lifeState == MemberLifeState.Active) Learn(m, song, recordId, act.artistId, year);
			if (crew != null) foreach (Musician m in crew) Learn(m, song, recordId, act.artistId, year);
		}
	}

	/// <summary>A house band backed a set: the backers learn its catalogue songs.</summary>
	public static void OnBacked(IEnumerable<string> backers, IReadOnlyList<SceneSetSong> set, string artistId, int year) {
		if (!Enabled || backers == null || set == null || set.Count == 0) return;
		foreach (string id in backers) {
			var m = ArtistManager.Instance?.GetMusician(id);
			if (m == null) continue;
			foreach (var s in set) if (!s.Original && !string.IsNullOrEmpty(s.SongId)) Learn(m, s.SongId, null, artistId, year);
		}
	}

	/// <summary>
	/// The best song the act's members carried in from elsewhere that <paramref name="eligible"/> allows, if its
	/// score with the known-song bonus beats <paramref name="bestScore"/>. Songs the act learned itself or has already
	/// recorded are not "carried": they came from here.
	/// </summary>
	internal static SongComposition BestCarried(SimulatedArtist artist, Func<SongComposition, bool> eligible,
		Func<SongComposition, float> score, float bestScore, out CarriedSong carrier, out Musician carrierPerson) {
		carrier = null; carrierPerson = null;
		if (!Enabled || artist?.members == null) return null;
		SongComposition best = null;
		float top = bestScore;
		foreach (Musician m in artist.members) {
			if (!m.isActive || m.carriedSongs == null) continue;
			foreach (CarriedSong c in m.carriedSongs) {
				if (c.viaArtistId == artist.artistId) continue;
				var song = CompositionCatalogService.GetSong(c.songId);
				if (song == null || !eligible(song)) continue;
				if (song.recordings?.Any(r => r.artistId == artist.artistId) == true) continue;
				CarriedConsidered++;
				float s = score(song) * (1f + KnownSongBonus);
				if (s > top) { top = s; best = song; carrier = c; carrierPerson = m; }
			}
		}
		return best;
	}

	/// <summary>Marks a candidate as carried in: the cover follows the arrangement the carrier played, when that
	/// single's master exists.</summary>
	internal static void MarkCarried(SelectedSongMaterial material, CarriedSong carrier, Musician person, SimulatedArtist artist, Record record, int year) {
		material.Carried = carrier;
		material.CarriedBy = person;
		if (carrier.recordId != null && PolarSongMetadataService.Get(carrier.recordId) is { } master &&
			master.songId == material.Song.songId && master.recordingYear <= year)
			material.ReferenceSubjectId = carrier.recordId;
	}

	/// <summary>The act chose a carried song. It is now this act's song: the members play it here, so it is never
	/// offered to them again as one carried in.</summary>
	internal static void OnSelected(SelectedSongMaterial material, SimulatedArtist artist, Record record, int year) {
		if (material?.Carried == null) return;
		var carrier = material.Carried;
		CarriedChosen++;
		if (carrier.recordId != null && material.ReferenceSubjectId == carrier.recordId) ArrangementsCarried++;
		if (LogChoices) Choices.Add((record?.recordId, artist.artistId, material.Song.songId, material.CarriedBy?.personId, carrier.viaArtistId, material.Source.ToString(), year));
		foreach (Musician m in artist.members) if (m.isActive && m.lifeState == MemberLifeState.Active) Learn(m, material.Song.songId, carrier.recordId, artist.artistId, year);
	}
}