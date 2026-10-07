using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>The lists a 1960s trade paper actually printed.</summary>
public enum TradeChartKind { Hot100, TopLps, RnB, Country, EasyListening, Regional }

/// <summary>One printed line of a trade list. <see cref="LastRank"/> is 0 for a new entry, -1 where the list has no
/// week-on-week memory (regional action), which prints a dash and never a NEW flag.</summary>
public sealed class TradeChartRow {
	public RecordRuntimeData Record;
	public int Rank, LastRank;
}

/// <summary>A list ready to print: its title, what it ranks, and the rows (empty with a note when the list is not
/// published yet).</summary>
public sealed class TradeChartView {
	public string Title, Noun, Note;
	public List<TradeChartRow> Rows = new();
}

/// <summary>
/// The genre and regional lists on the trade sheet, built from the same records the Hot 100 ranks.
///
/// Billboard in the 1960s printed a Hot 100 of singles, a Top LPs list, R&amp;B and Country singles lists, and from
/// July 1961 an Easy Listening list; a "regional action" box listed what was selling market by market. Those are the
/// lists here. Gospel, Jazz and the rest had no weekly singles list of their own in the decade, so they get none.
///
/// The genre and regional lists are VIEWS over this week's sales, not new simulation: nothing here writes to a record,
/// draws a random number or feeds back into the market, so building one cannot move a seeded stream. A genre list is
/// ranked on units sold this week; last week's rank is the same ranking over last week's units, which gives the
/// movement column without a stored history (and so without a save field).
/// </summary>
public static class TradeCharts {
	/// <summary>The one place the trade paper's masthead is spelled. A real trade name today; swap it for a house name
	/// here (and in nothing else) before anything ships.</summary>
	public const string TradeName = "BILLBOARD";

	/// <summary>Billboard's Easy Listening list began in July 1961.</summary>
	public static readonly GameDate EasyListeningFirstIssue = new(1961, 7, 17);

	public const int GenreListSize = 50, EasyListeningSize = 40, RegionalListSize = 30;

	public static string TabName(TradeChartKind kind) => kind switch {
		TradeChartKind.Hot100 => "HOT 100",
		TradeChartKind.TopLps => "TOP LPs",
		TradeChartKind.RnB => "R&B",
		TradeChartKind.Country => "COUNTRY",
		TradeChartKind.EasyListening => "EASY LISTENING",
		_ => "REGIONAL",
	};

	/// <summary>Which genres a list carries. Hot 100 and LPs carry everything.</summary>
	public static bool Carries(TradeChartKind kind, Genre genre) {
		switch (kind) {
			case TradeChartKind.RnB:
				return genre == Genre.Motown || (GenreCatalog.TryGet(genre, out GenreProfile rnb) && rnb.Family == GenreFamily.RhythmAndSoul);
			case TradeChartKind.Country:
				return GenreCatalog.TryGet(genre, out GenreProfile country) && country.Family == GenreFamily.Country;
			case TradeChartKind.EasyListening:
				return genre is Genre.EasyListening or Genre.TraditionalPop or Genre.BossaNova or Genre.BaroquePop or Genre.SunshinePop;
			default:
				return true;
		}
	}

	public static TradeChartView Build(TradeChartKind kind, string regionId, GameDate today) {
		ChartManager manager = ChartManager.Instance;
		var view = new TradeChartView();
		if (manager == null) { view.Title = TradeName; view.Noun = "SINGLES"; return view; }

		switch (kind) {
			case TradeChartKind.Hot100:
				view.Title = $"{TradeName} HOT 100 — SINGLES";
				view.Noun = "SINGLES";
				foreach (RecordRuntimeData record in manager.GetCurrentChart())
					view.Rows.Add(new TradeChartRow { Record = record, Rank = record.currentPosition, LastRank = record.lastWeekPosition });
				return view;

			case TradeChartKind.TopLps:
				view.Title = $"{TradeName} TOP LPs";
				view.Noun = "LPs";
				foreach (RecordRuntimeData record in manager.GetCurrentAlbumChart())
					view.Rows.Add(new TradeChartRow { Record = record, Rank = record.currentPosition, LastRank = record.lastWeekPosition });
				return view;

			case TradeChartKind.Regional:
				return BuildRegional(manager, regionId);

			default:
				return BuildGenre(manager, kind, today);
		}
	}

	private static TradeChartView BuildGenre(ChartManager manager, TradeChartKind kind, GameDate today) {
		var view = new TradeChartView { Noun = "SINGLES" };
		int size = kind == TradeChartKind.EasyListening ? EasyListeningSize : GenreListSize;
		switch (kind) {
			case TradeChartKind.RnB: view.Title = $"{TradeName} HOT R&B SINGLES"; break;
			case TradeChartKind.Country: view.Title = $"{TradeName} HOT COUNTRY SINGLES"; break;
			default: view.Title = $"{TradeName} EASY LISTENING"; break;
		}
		if (kind == TradeChartKind.EasyListening && today < EasyListeningFirstIssue) {
			view.Note = "THE TRADE DOES NOT PRINT AN EASY LISTENING LIST UNTIL JULY 17, 1961.";
			return view;
		}

		List<RecordRuntimeData> pool = manager.GetAllRecords()
			.Where(r => r.baseRecord != null && r.baseRecord.format != ReleaseFormat.Album && Carries(kind, r.baseRecord.primaryGenre))
			.ToList();
		List<RecordRuntimeData> now = pool.Where(r => r.unitsThisWeek > 0)
			.OrderByDescending(r => r.unitsThisWeek).ThenByDescending(r => r.totalUnitsSold).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal)
			.Take(size).ToList();
		var before = new Dictionary<RecordRuntimeData, int>();
		int lastRank = 0;
		foreach (RecordRuntimeData record in pool.Where(r => r.unitsPreviousWeek > 0)
				.OrderByDescending(r => r.unitsPreviousWeek).ThenByDescending(r => r.totalUnitsSold).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal))
			before[record] = ++lastRank;
		for (int i = 0; i < now.Count; i++)
			view.Rows.Add(new TradeChartRow { Record = now[i], Rank = i + 1, LastRank = before.TryGetValue(now[i], out int was) && was <= size ? was : 0 });
		if (view.Rows.Count == 0) view.Note = "NOTHING OF THIS KIND SOLD THIS WEEK.";
		return view;
	}

	private static TradeChartView BuildRegional(ChartManager manager, string regionId) {
		MarketRegion region = manager.GetRegionById(regionId) ?? manager.GetAllRegions().FirstOrDefault();
		string name = region?.regionName ?? "THE REGIONS";
		var view = new TradeChartView { Title = $"{TradeName} REGIONAL ACTION — {name.ToUpperInvariant()}", Noun = "SINGLES" };
		if (region == null) return view;
		List<RecordRuntimeData> top = manager.GetAllRecords()
			.Where(r => r.baseRecord != null && r.baseRecord.format != ReleaseFormat.Album
				&& r.regionalData.TryGetValue(region.regionId, out RegionalRecordData data) && data.unitsSoldThisWeek > 0)
			.OrderByDescending(r => r.regionalData[region.regionId].unitsSoldThisWeek).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal)
			.Take(RegionalListSize).ToList();
		for (int i = 0; i < top.Count; i++)
			view.Rows.Add(new TradeChartRow { Record = top[i], Rank = i + 1, LastRank = -1 });
		if (view.Rows.Count == 0) view.Note = "NO SALES REPORTED FROM THIS MARKET THIS WEEK.";
		return view;
	}
}
