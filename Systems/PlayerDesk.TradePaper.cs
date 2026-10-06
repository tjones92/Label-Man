using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE TRADE COLUMN -- what the Morning Paper knows that the office log does not.
// The paper used to reprint the player's own ledger, so the one object the player reads every
// morning held nothing they had not already done. These are reads of systems that already run
// (the weekly chart, regional breakout stages, the AI labels' talent market); nothing here
// writes to any of them, and none of it draws a GD.Rand* -- so the AI economy is untouched.
// Transient by design: the column is gathered each morning and handed to the paper once.
// ============================================================================================
public partial class PlayerDesk : Node {

	private const int PaperTopChartLines = 3;
	private const int PaperMoverFloor = 8;            // places gained before a climb is news
	private const int PaperRivalSigningsShown = 2;
	private const int PaperRivalQueueCap = 12;

	private readonly List<string> tradeNews = new();
	private readonly List<(float Notability, string Line)> rivalSigningQueue = new();
	private readonly HashSet<string> paperBreakoutsShown = new(StringComparer.Ordinal);
	private int paperChartWeek = -1;

	/// <summary>Hands the gathered trade column to the paper and clears it, so each story prints once --
	/// across a multi-day skip the column keeps collecting until the paper is actually opened.</summary>
	public IReadOnlyList<string> TakeTradeNews() {
		string[] taken = tradeNews.ToArray();
		tradeNews.Clear();
		return taken;
	}

	/// <summary>Called from the AI talent-market feed: remember a rival's notable signing for the paper.
	/// Only a signing that actually landed counts, and only the most notable few are kept.</summary>
	private void RecordRivalSigning(RosterManager.DailyTalentMarketAppointment appointment) {
		if (appointment?.SelectedArtist == null || appointment.Label == null || appointment.Label.isPlayerOwned) return;
		if (appointment.Outcome is not ("AcceptedUncontested" or "AcceptedArtistChoice")) return;
		AILabel winner = appointment.WinnerLabelId == null || appointment.WinnerLabelId == appointment.Label.labelId
			? appointment.Label
			: ChartManager.Instance?.GetLabelById(appointment.WinnerLabelId) ?? appointment.Label;
		if (winner == null || winner.isPlayerOwned) return;
		SimulatedArtist artist = appointment.SelectedArtist;
		float notability = Mathf.Clamp(artist.reputation, 0f, 1f) + Mathf.Clamp(artist.momentum, 0f, 1f);
		string genre = GenreNameFormatter.Format(artist.primaryGenre);
		rivalSigningQueue.Add((notability, $"{winner.labelName} signed {artist.stageName}, {("aeiouAEIOU".Contains(genre[0]) ? "an" : "a")} {genre} act."));
		if (rivalSigningQueue.Count > PaperRivalQueueCap) {
			int weakest = 0;
			for (int i = 1; i < rivalSigningQueue.Count; i++)
				if (rivalSigningQueue[i].Notability < rivalSigningQueue[weakest].Notability) weakest = i;
			rivalSigningQueue.RemoveAt(weakest);
		}
	}

	/// <summary>Gathers this morning's trade column. Runs once a morning, last, from the digest refresh.</summary>
	private void GatherTradeNews() {
		ChartManager charts = ChartManager.Instance;
		if (charts != null) {
			int week = charts.GetCurrentChartWeek();
			if (week != paperChartWeek) {
				List<RecordRuntimeData> chart = charts.GetCurrentChart();
				if (chart.Count >= PaperTopChartLines) {
					paperChartWeek = week;
					AddChartNews(charts, chart);
					AddBreakoutNews(charts, chart);
				}
			}
		}
		foreach (var signing in rivalSigningQueue.OrderByDescending(item => item.Notability).Take(PaperRivalSigningsShown))
			tradeNews.Add(signing.Line);
		rivalSigningQueue.Clear();
	}

	private void AddChartNews(ChartManager charts, List<RecordRuntimeData> chart) {
		string Entry(int position) {
			RecordRuntimeData record = chart[position];
			return $"#{position + 1} \"{record.baseRecord.title}\" by {record.baseRecord.artistName}";
		}
		tradeNews.Add("THE HOT 100: " + string.Join(", ", Enumerable.Range(0, PaperTopChartLines).Select(Entry)) + ".");

		RecordRuntimeData mover = chart
			.Where(record => record.baseRecord != null && record.lastWeekPosition > 0 && record.currentPosition > 0)
			.OrderByDescending(record => record.lastWeekPosition - record.currentPosition).FirstOrDefault();
		if (mover != null && mover.lastWeekPosition - mover.currentPosition >= PaperMoverFloor)
			tradeNews.Add($"Biggest climber: \"{mover.baseRecord.title}\" by {mover.baseRecord.artistName}, " +
				$"up {mover.lastWeekPosition - mover.currentPosition} to #{mover.currentPosition}.");

		RecordRuntimeData mine = chart.FirstOrDefault(record => record.baseRecord?.isPlayerOwned == true);
		if (mine != null) tradeNews.Add($"Yours: \"{mine.baseRecord.title}\" is at #{mine.currentPosition}.");
	}

	/// <summary>One regional breakout among the charting records the label does not own -- the player's own
	/// breakouts already lead the paper (<see cref="CheckBreakoutHeadlines"/>), with what to do about them.</summary>
	private void AddBreakoutNews(ChartManager charts, List<RecordRuntimeData> chart) {
		foreach (RecordRuntimeData record in chart) {
			if (record.baseRecord == null || record.baseRecord.isPlayerOwned || record.regionalData == null) continue;
			foreach (var pair in record.regionalData) {
				if (pair.Value == null || pair.Value.breakoutStage < RegionalBreakoutStage.RegionalBreakout) continue;
				if (!paperBreakoutsShown.Add($"{record.baseRecord.recordId}|{pair.Key}")) continue;
				string region = charts.GetRegionById(pair.Key)?.regionName ?? pair.Key;
				tradeNews.Add($"Breaking out in {region}: \"{record.baseRecord.title}\" by {record.baseRecord.artistName}.");
				return;
			}
		}
	}
}
