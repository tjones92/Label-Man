using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 4 (SimTools/CelebrityFanbaseDesign.md §2.4): launch books, advance orders and live draw. See
/// <see cref="MediaFollowerDemand"/>. A book is opened the first time a release is seen (the AI launch, or the first
/// clearing week of any other record) from the act's followers at that moment, and closed after its third clearing week.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>This clearing week's books and their week factor, rebuilt by <see cref="PrepareFollowerDemand"/>. Never saved.</summary>
	private static readonly Dictionary<string, (FollowerLaunch Book, float Factor)> followerWeek = new(StringComparer.Ordinal);
	/// <summary>Each act's crowds this week by region (people in the room for it), for the live joiners. Consumed weekly.</summary>
	private static readonly Dictionary<string, float[]> liveCrowds = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, double> followerDemandLedger = new(StringComparer.Ordinal);
	private static void FollowerDemandLedger(string key, double amount) => followerDemandLedger[key] = followerDemandLedger.GetValueOrDefault(key) + amount;
	/// <summary>Closed books this year, for the advance-order distribution (§9). Print-only.</summary>
	private static readonly List<(string Artist, ReleaseFormat Format, double Asked, double Granted)> followerDemandBooks = new();
	private const int FollowerSeenDays = 56;
	internal static IReadOnlyList<(string Artist, ReleaseFormat Format, double Asked, double Granted)> ClosedFollowerLaunches => followerDemandBooks;
	internal static double FollowerDemandLedgerValue(string key) => followerDemandLedger.GetValueOrDefault(key);

	public static FollowerLaunch FollowerLaunchOf(string recordId) =>
		recordId != null && state.FollowerLaunches.TryGetValue(recordId, out var book) ? book : null;

	/// <summary>
	/// The record's launch book, opened from the act's followers now if this is the first look at the release. Null when
	/// the act has no following worth a book. Each release is looked at once.
	/// </summary>
	private static FollowerLaunch FollowerLaunchFor(Record b, int today) {
		if (b == null || string.IsNullOrEmpty(b.recordId) || string.IsNullOrEmpty(b.artistId)) return null;
		if (state.FollowerLaunches.TryGetValue(b.recordId, out var book)) return book;
		if (!state.FollowerLaunchSeen.TryAdd(b.recordId, today)) return null;
		MediaFollowingState fs = FollowingFor(b.artistId);
		if (fs == null) return null;
		book = new FollowerLaunch { RecordId = b.recordId, ArtistId = b.artistId, Format = b.format };
		float total = 0f;
		foreach (var (regionId, fr) in fs.Regions) {
			float n = MediaFollowerDemand.RegionIntent(fr.YoungTotal(), fr.Adult, b.format);
			if (n < 1f) continue;
			book.Intent[regionId] = n; total += n;
		}
		if (total < MediaFollowerDemand.BookFloor) return null;
		state.FollowerLaunches[b.recordId] = book;
		FollowerDemandLedger(b.format == ReleaseFormat.Single ? "booksSingles" : "booksAlbums", 1);
		return book;
	}

	/// <summary>
	/// An AI launch: the dealers' advance orders. Each region the record reached gets the followers' three-week intent on top
	/// of its launch shelf (causal only; shadow books what would have been stocked). The player's pressing is real, so the
	/// player's records are never stocked here.
	/// </summary>
	public static void StockAdvanceOrders(RecordRuntimeData record) {
		if (!MediaFollowerDemand.Active || record?.baseRecord == null || record.baseRecord.isPlayerOwned) return;
		FollowerLaunch book = FollowerLaunchFor(record.baseRecord, BroadcastExposure.DayIndex(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate));
		if (book == null || book.Stocked) return;
		book.Stocked = true;
		foreach (var (regionId, intent) in book.Intent) {
			if (!record.regionalData.TryGetValue(regionId, out RegionalRecordData data) || data.unitsInStores <= 0) continue;
			int copies = (int)MathF.Round(intent);
			book.StockedUnits += copies;
			if (MediaFollowerDemand.Causal) data.unitsInStores += copies;
		}
		FollowerDemandLedger("stocked", book.StockedUnits);
	}

	/// <summary>
	/// Before a week's demand: open books for the week's new releases, date each new book to this clearing week, close books
	/// past their third week, and set each live book's share for the week.
	/// </summary>
	public static void PrepareFollowerDemand(IEnumerable<RecordRuntimeData> records, int chartWeek, GameDate today) {
		followerWeek.Clear();
		if (!MediaFollowerDemand.Active) return;
		int day = BroadcastExposure.DayIndex(today);
		foreach (RecordRuntimeData rec in records) {
			Record b = rec?.baseRecord;
			if (b == null) continue;
			int released = BroadcastExposure.DayIndex(b.releaseDate);
			if (released > day) continue;
			// A launch may be staged before its release date: the book starts counting at the first clearing it is on sale.
			FollowerLaunch book = released > day - 7 ? FollowerLaunchFor(b, day) : FollowerLaunchOf(b.recordId);
			if (book != null && book.LaunchWeek < 0) book.LaunchWeek = chartWeek;
		}
		foreach (string id in state.FollowerLaunches.Keys.ToList()) {
			FollowerLaunch book = state.FollowerLaunches[id];
			if (book.LaunchWeek < 0) {
				// Staged but never on sale: let it go once its look has aged out.
				if (!state.FollowerLaunchSeen.ContainsKey(id)) state.FollowerLaunches.Remove(id);
				continue;
			}
			int week = chartWeek - book.LaunchWeek;
			if (week >= MediaFollowerDemand.Weeks) { CloseFollowerLaunch(book, today); continue; }
			float factor = MediaFollowerDemand.WeekFactor(week, book.Loss);
			if (factor > 0f) followerWeek[id] = (book, factor);
		}
		foreach (var (id, seen) in state.FollowerLaunchSeen.ToList())
			if (seen < day - FollowerSeenDays) state.FollowerLaunchSeen.Remove(id);
	}

	/// <summary>The followers' intent on a record in a region this clearing week, in units (0 for a record with no live book).</summary>
	public static float FollowerIntent(string recordId, string regionId) =>
		recordId != null && followerWeek.TryGetValue(recordId, out var w) ? w.Book.Intent.GetValueOrDefault(regionId) * w.Factor : 0f;

	/// <summary>What the followers asked for and what the title's stock let through, booked to the release.</summary>
	public static void RecordFollowerGrant(string recordId, float asked, int granted) {
		if (recordId == null || !followerWeek.TryGetValue(recordId, out var w)) return;
		w.Book.Asked += asked; w.Book.Granted += granted;
		bool single = w.Book.Format == ReleaseFormat.Single;
		FollowerDemandLedger(single ? "askedSingles" : "askedAlbums", asked);
		FollowerDemandLedger(single ? "grantedSingles" : "grantedAlbums", granted);
	}

	/// <summary>LP buyers the fans took from other titles' intent in a region-week (causal only).</summary>
	public static void RecordFollowerAlbumDisplacement(int units) => FollowerDemandLedger("displacedAlbums", units);
	/// <summary>Of those, the buyers booked against the displaced titles' cohorts (the share that would have cleared).</summary>
	public static void RecordFollowerAlbumSpent(long units) => FollowerDemandLedger("spentAlbums", units);

	private static void CloseFollowerLaunch(FollowerLaunch book, GameDate today) {
		state.FollowerLaunches.Remove(book.RecordId);
		followerDemandBooks.Add((book.ArtistId, book.Format, book.Asked, book.Granted));
		MediaFollowingState fs = FollowingFor(book.ArtistId);
		if (fs == null) return;
		Genre genre = ArtistManager.Instance?.GetArtist(book.ArtistId)?.primaryGenre ?? Genre.TraditionalPop;
		FollowingRow("launch", today, fs, genre, book.Asked.ToString("F0", CultureInfo.InvariantCulture),
			$"{book.Format}|granted={book.Granted:F0}|stocked={book.StockedUnits:F0}|loss={book.Loss:F3}|book={book.Intent.Values.Sum():F0}|{book.RecordId}");
	}

	/// <summary>Disappointment at a release (§2.3) reaches the release's own later weeks: the followers it lost stop buying.</summary>
	private static void FollowerLaunchLoss(string recordId, float loss) {
		if (loss > 0f && recordId != null && state.FollowerLaunches.TryGetValue(recordId, out var book)) book.Loss = Math.Max(book.Loss, loss);
	}

	// ---- live draw ----------------------------------------------------------------------------------------------------

	/// <summary>Followers who come to one of the act's bills in this region (causal only; 0 otherwise).</summary>
	public static int FollowerLiveHeads(string artistId, string regionId) {
		if (!MediaFollowerDemand.Causal || regionId == null) return 0;
		MediaFollowingState fs = FollowingFor(artistId);
		return fs != null && fs.Regions.TryGetValue(regionId, out var fr) ? MediaFollowerDemand.LiveHeads(fr.YoungTotal() + fr.Adult) : 0;
	}

	/// <summary>A set was played to this many people: the act's share of the crowd, for this week's live joiners.</summary>
	public static void NoteLiveCrowd(string artistId, string regionId, float people) {
		if (!MediaFollowerDemand.Active || artistId == null || regionId == null || people <= 0f) return;
		var regions = ChartManager.Instance?.GetAllRegions();
		if (regions == null) return;
		int i = regions.FindIndex(r => r.regionId == regionId);
		if (i < 0) return;
		if (!liveCrowds.TryGetValue(artistId, out var row) || row.Length != regions.Count) liveCrowds[artistId] = row = new float[regions.Count];
		row[i] += people;
		FollowerDemandLedger("liveCrowd", people);
	}

	/// <summary>This week's live joiners by region (young, adult), split like a single's buyers of the genre; then cleared.</summary>
	private static Dictionary<string, (float[] Young, float[] Adult)> TakeLiveJoiners(int regionCount) {
		var result = new Dictionary<string, (float[], float[])>(StringComparer.Ordinal);
		foreach (var (id, crowd) in liveCrowds) {
			if (crowd.Length != regionCount) continue;
			Genre genre = ArtistManager.Instance?.GetArtist(id)?.primaryGenre ?? Genre.TraditionalPop;
			float y = MediaFollowing.YoungBuyerShare(genre, ReleaseFormat.Single);
			var young = new float[regionCount]; var adult = new float[regionCount];
			for (int i = 0; i < regionCount; i++) { young[i] = crowd[i] * MediaFollowerDemand.CLive * y; adult[i] = crowd[i] * MediaFollowerDemand.CLive * (1f - y); }
			result[id] = (young, adult);
		}
		liveCrowds.Clear();
		return result;
	}

	/// <summary>
	/// Year end: books by format, what fans asked and what stock let through, advance orders by size (§9: a solid fanbase act
	/// 100-300k, the top act 1-2M in a phenomenon year), copies stocked, and the live ledger. Read-only.
	/// </summary>
	private static void PrintFollowerDemand(int year) {
		if (!MediaFollowerDemand.Active) return;
		double V(string key) => followerDemandLedger.GetValueOrDefault(key);
		string Sizes(ReleaseFormat format) {
			var asked = followerDemandBooks.Where(b => b.Format == format).Select(b => b.Asked).OrderBy(x => x).ToList();
			if (asked.Count == 0) return "n=0";
			double Q(double p) => asked[Math.Min(asked.Count - 1, (int)(p * asked.Count))];
			return $"n={asked.Count} median={Q(0.5) / 1e3:F1}k p90={Q(0.9) / 1e3:F1}k max={asked[^1] / 1e3:F1}k over100k={asked.Count(x => x >= 1e5)}";
		}
		var top = followerDemandBooks.OrderByDescending(b => b.Asked).Take(3)
			.Select(b => $"\"{ArtistManager.Instance?.GetArtist(b.Artist)?.stageName ?? b.Artist}\" {b.Format} {b.Asked / 1e3:F0}k/{b.Granted / 1e3:F0}k");
		Godot.GD.Print($"MEDIA_FOLLOWER_DEMAND year={year} mode={(MediaFollowerDemand.Causal ? "causal" : "shadow")}" +
			$" books[singles/LPs]={V("booksSingles"):0}/{V("booksAlbums"):0}" +
			$" asked[singles/LPs]={V("askedSingles") / 1e6:F2}M/{V("askedAlbums") / 1e6:F2}M granted={V("grantedSingles") / 1e6:F2}M/{V("grantedAlbums") / 1e6:F2}M displacedLPs={V("displacedAlbums") / 1e6:F2}M spentLPs={V("spentAlbums") / 1e6:F2}M" +
			$" stocked={V("stocked") / 1e6:F2}M singles[{Sizes(ReleaseFormat.Single)}] LPs[{Sizes(ReleaseFormat.Album)}]" +
			$" live[crowd={V("liveCrowd") / 1e6:F2}M joiners={V("joinLive") / 1e6:F3}M] top=[{string.Join(" ", top)}]");
		followerDemandLedger.Clear();
		followerDemandBooks.Clear();
	}
}
