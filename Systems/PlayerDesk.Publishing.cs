using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum PublishingLineKind { PerformanceLogged, PerformanceStatement, WriterMechanical }

/// <summary>One line in the label's publishing books. Amount is the publisher's money for performance lines and the writer's
/// for <see cref="PublishingLineKind.WriterMechanical"/>; WriterAmount is the writer's half of a performance line.</summary>
public sealed class PublishingRoyaltyLine {
	public int Day { get; set; }
	public PublishingLineKind Kind { get; set; }
	public string SongId { get; set; }
	public string Title { get; set; }
	public string RecordId { get; set; }
	public string ArtistId { get; set; }
	public long Performances { get; set; }
	public int Units { get; set; }
	public float Amount { get; set; }
	public float WriterAmount { get; set; }
	/// <summary>A logged performance line: the statement day it is paid on, and whether that statement has run.</summary>
	public int PayOnDay { get; set; }
	public bool Settled { get; set; }
}

/// <summary>The label's publishing books: radio performances logged and paid, and the writer's half of the mechanical paid
/// out. Running totals are recomputable from <see cref="Lines"/>.</summary>
public sealed class PublishingRoyaltyBooks {
	public long Performances { get; set; }
	public float PerformanceEarned { get; set; }
	public float PerformancePaid { get; set; }
	public float WriterPerformancePaid { get; set; }
	public float WriterMechanicalPaid { get; set; }
	public List<PublishingRoyaltyLine> Lines { get; set; } = new();

	public float PerformanceOwed => Mathf.Max(0f, MasterDeal.R2(PerformanceEarned - PerformancePaid));

	public bool LedgerAgrees() {
		float Sum(IEnumerable<PublishingRoyaltyLine> l, Func<PublishingRoyaltyLine, float> f) => MasterDeal.R2(l.Sum(f));
		var logged = Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged).ToList();
		var settled = logged.Where(l => l.Settled).ToList();
		var statements = Lines.Where(l => l.Kind == PublishingLineKind.PerformanceStatement).ToList();
		bool Near(float a, float b) => Math.Abs(a - b) < 0.011f + 0.0005f * Math.Abs(b);
		return logged.Sum(l => l.Performances) == Performances
			&& Near(Sum(logged, l => l.Amount), PerformanceEarned)
			&& Near(Sum(statements, l => l.Amount), PerformancePaid) && Near(Sum(settled, l => l.Amount), PerformancePaid)
			&& Near(Sum(statements, l => l.WriterAmount), WriterPerformancePaid) && Near(Sum(settled, l => l.WriterAmount), WriterPerformancePaid)
			&& Near(Sum(Lines.Where(l => l.Kind == PublishingLineKind.WriterMechanical), l => l.Amount), WriterMechanicalPaid)
			&& PerformancePaid <= PerformanceEarned + 0.011f;
	}
}

/// <summary>
/// Early-game: the publisher's two income streams the 1960 trade actually paid, and the writer's half of each.
/// <para><b>Radio performance royalties.</b> A song the player's label publishes earns a performing-rights royalty when a
/// station plays it, on any record of it: the label's own, a leased or sold master, a placement, an AI cover. Each chart
/// week the logged performances are read off the record's real regional airplay: radioPlay (0-1, the region's share of a
/// full rotation) x the region's stations x <see cref="PlaysPerWeekFullRotation"/>, summed over regions. Each pays
/// <see cref="PerformanceRatePerPlay"/>, split evenly: the publisher's half to the label, the writer's half to the writers
/// directly (the society paid writers itself; it never passes through the label's cash). A quarter's performances are paid
/// on the first day of the quarter after next (January-March on 1 July). Only the plug side is played.</para>
/// <para><b>The writer's half of the mechanical.</b> The 2c was the publisher's and the writer's, half each. The label
/// keeps the publisher's cent on its own sales and on other labels' records of its songs and pays the writer's cent to the
/// writing act (<see cref="MechanicalRoyaltyService.ChargeSide"/>, <see cref="PayWriterHalfOnCut"/>). The act's people
/// already count it in their wealth (MemberWealthService derives writer income from charted units), so it lands on
/// <see cref="SimulatedArtist.writerRoyaltyEarnings"/>, not the performer's royalty. Performance money to people is new
/// income, so it is added per person by credit share (<see cref="Musician.writerPerformanceEarnings"/>).</para>
/// <para>Player-only: nothing here runs without a player label, and AI-to-AI publishing is untouched. Performance money
/// comes from outside the modelled economy (the stations' blanket licence fees), as it did. Authored, not sourced: plays a
/// week, the payment lag, and that every player publisher is a society affiliate. The 4c is anchored loosely on a reported
/// early BMI local-performance rate (1940s, secondary source), not a verified 1960 schedule.</para>
/// </summary>
public partial class PlayerDesk {
	public const float PerformanceRatePerPlay = 0.04f;
	public const float PlaysPerWeekFullRotation = 14f;

	private PublishingRoyaltyBooks publishing = new();
	public PublishingRoyaltyBooks Publishing => publishing;

	private bool PublishesSong(PublishingControlType control, string controllerLabelId, bool playerRecord) =>
		Label != null && control is PublishingControlType.LabelAffiliate or PublishingControlType.LabelBuyout
		&& (controllerLabelId == Label.labelId || playerRecord && string.IsNullOrEmpty(controllerLabelId));

	/// <summary>The act that wrote a song, for the writer's half: the song's originating act, else the given fallback.</summary>
	private static SimulatedArtist WriterActFor(string songId, string fallbackArtistId) {
		var song = string.IsNullOrEmpty(songId) ? null : CompositionCatalogService.GetSong(songId);
		return (song?.originArtistId != null ? ArtistManager.Instance?.GetArtist(song.originArtistId) : null)
			?? (fallbackArtistId != null ? ArtistManager.Instance?.GetArtist(fallbackArtistId) : null);
	}

	// ── The writer's half of the mechanical ────────────────────────────────────────────────────────

	/// <summary>Called from an AI label's settlement when it routes a player-published song's mechanical to the player: the
	/// writer's half goes straight to the writing act. Returns what was paid (0 when no writer is known; the label then keeps
	/// the whole of it, as before).</summary>
	internal float PayWriterHalfOnCut(Record record, float slice, int units) {
		if (Label == null || record == null || slice <= 0f) return 0f;
		var writer = WriterActFor(record.songId, record.originalArtistId ?? record.publishingControllerArtistId);
		if (writer == null) return 0f;
		float cutIn = MechanicalRoyaltyService.LabelCutInShare(record, Label);
		float half = slice * MechanicalRoyaltyService.WriterShare * (1f - Mathf.Clamp(cutIn, 0f, 1f));
		if (half <= 0f) return 0f;
		writer.writerRoyaltyEarnings += half;
		BookWriterMechanical(writer, half, units, record.songId, record.title, record.recordId);
		return half;
	}

	private void OnWriterHalfPaid(SimulatedArtist writer, float amount, int units) {
		if (Label == null || writer == null || amount <= 0f) return;
		BookWriterMechanical(writer, amount, units, null, null, null);
	}

	/// <summary>One writer-mechanical line per act per day; sales book many times a day.</summary>
	private void BookWriterMechanical(SimulatedArtist writer, float amount, int units, string songId, string title, string recordId) {
		int day = TodayIndex();
		var last = publishing.Lines.LastOrDefault(l => l.Kind == PublishingLineKind.WriterMechanical && l.Day == day && l.ArtistId == writer.artistId && l.SongId == songId);
		if (last == null) {
			last = new PublishingRoyaltyLine { Day = day, Kind = PublishingLineKind.WriterMechanical, ArtistId = writer.artistId, SongId = songId, Title = title ?? writer.stageName, RecordId = recordId };
			publishing.Lines.Add(last);
		}
		last.Amount += amount;
		last.Units += Math.Max(0, units);
		publishing.WriterMechanicalPaid += amount;
	}

	// ── Radio performances ─────────────────────────────────────────────────────────────────────────

	/// <summary>Logged performances of one record this week: regional airplay x stations x plays at full rotation.</summary>
	public static long LoggedPerformances(RecordRuntimeData rec) {
		var charts = ChartManager.Instance;
		if (rec?.regionalData == null || charts == null) return 0;
		double plays = 0;
		foreach (var pair in rec.regionalData) {
			if (pair.Value == null || pair.Value.radioPlay <= 0f) continue;
			int stations = charts.GetRegionById(pair.Key)?.media?.totalRadioStations ?? 0;
			plays += pair.Value.radioPlay * stations * PlaysPerWeekFullRotation;
		}
		return (long)Math.Round(plays);
	}

	/// <summary>The statement day for a performance logged on <paramref name="day"/>: the 1st of the quarter after next.</summary>
	public static int PerformancePayDay(int day) {
		var d = LocalSceneRoomService.Date(day);
		int q = (d.month - 1) / 3, month = q * 3 + 1 + 6, year = d.year;
		if (month > 12) { month -= 12; year++; }
		return LocalSceneRoomService.Day(new GameDate(year, month, 1));
	}

	/// <summary>Week end: log this week's radio performances of every record of a song the label publishes.</summary>
	private void LogPerformanceWeek() {
		var charts = ChartManager.Instance;
		if (Label == null || charts == null) return;
		int day = TodayIndex();
		foreach (var rec in charts.GetAllRecords()) {
			var r = rec?.baseRecord;
			if (r == null || rec.regionalData == null) continue;
			bool b = r.bSideIsPlugSide && !string.IsNullOrEmpty(r.bSideSongId);
			if (!PublishesSong(b ? r.bSidePublishingControl : r.publishingControl,
				b ? r.bSidePublishingControllerLabelId : r.publishingControllerLabelId, r.isPlayerOwned)) continue;
			long plays = LoggedPerformances(rec);
			if (plays <= 0) continue;
			string songId = b ? r.bSideSongId : r.songId;
			float total = plays * PerformanceRatePerPlay;
			float writerHalf = total * MechanicalRoyaltyService.WriterShare, pub = total - writerHalf, writer = 0f;
			var act = WriterActFor(songId, b ? r.bSidePublishingControllerArtistId ?? r.artistId : r.originalArtistId ?? r.artistId);
			if (act != null) {
				// A label cut-in on the plug side's writer credit is the label's own writer money.
				float cutIn = b ? 0f : Mathf.Clamp(MechanicalRoyaltyService.LabelCutInShare(r, Label), 0f, 1f);
				writer = writerHalf * (1f - cutIn);
				pub += writerHalf - writer;
			}
			// With no writer to pay, the society holds the writer's half; it isn't the label's.
			publishing.Lines.Add(new PublishingRoyaltyLine {
				Day = day, Kind = PublishingLineKind.PerformanceLogged, SongId = songId, Title = b ? r.bSideTitle : r.title, RecordId = r.recordId,
				ArtistId = act?.artistId, Performances = plays, Amount = MasterDeal.R2(pub), WriterAmount = MasterDeal.R2(writer), PayOnDay = PerformancePayDay(day),
			});
			publishing.Performances += plays;
			publishing.PerformanceEarned = MasterDeal.R2(publishing.PerformanceEarned + MasterDeal.R2(pub));
		}
	}

	/// <summary>What the next statement on or before <paramref name="day"/> would pay the label from performances already logged.</summary>
	public float PerformanceDueBy(int day) =>
		MasterDeal.R2(publishing.Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged && !l.Settled && l.PayOnDay <= day).Sum(l => l.Amount));

	/// <summary>The 1st of a quarter: the society's statement pays the label its half and the writers theirs.</summary>
	private void PerformanceStatement(GameDate date) {
		if (Label == null) return;
		int today = LocalSceneRoomService.Day(date);
		var due = publishing.Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged && !l.Settled && l.PayOnDay <= today).ToList();
		if (due.Count == 0) return;
		float pub = 0f, writers = 0f; long plays = 0;
		foreach (var l in due) {
			l.Settled = true;
			pub += l.Amount; writers += l.WriterAmount; plays += l.Performances;
			if (l.WriterAmount > 0f) PayWriterPerformance(l);
		}
		pub = MasterDeal.R2(pub); writers = MasterDeal.R2(writers);
		publishing.Lines.Add(new PublishingRoyaltyLine { Day = today, Kind = PublishingLineKind.PerformanceStatement, Performances = plays, Amount = pub, WriterAmount = writers });
		publishing.PerformancePaid = MasterDeal.R2(publishing.PerformancePaid + pub);
		publishing.WriterPerformancePaid = MasterDeal.R2(publishing.WriterPerformancePaid + writers);
		Label.cashReserves += pub;
		Label.monthlyRevenue += pub;
		var songs = due.Select(l => l.Title).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
		Note($"PERFORMANCE ROYALTIES: the performing-rights society logged {plays:N0} radio plays of your songs " +
			$"({string.Join(", ", songs.Take(3).Select(t => $"\"{t}\""))}{(songs.Count > 3 ? $" and {songs.Count - 3} more" : "")}). " +
			$"${pub:N2} to the label as publisher" + (writers > 0f ? $"; ${writers:N2} paid to the writers directly." : "."));
	}

	/// <summary>The writer's half of a performance line, to the act and, by credit share, to the people who wrote it.</summary>
	private static void PayWriterPerformance(PublishingRoyaltyLine l) {
		var act = l.ArtistId != null ? ArtistManager.Instance?.GetArtist(l.ArtistId) : null;
		if (act == null) return;
		act.writerRoyaltyEarnings += l.WriterAmount;
		var credits = CompositionCatalogService.GetSong(l.SongId)?.credits?
			.Where(c => c.writerType == WriterEntityType.Musician && !string.IsNullOrEmpty(c.writerId) && c.share > 0f).ToList();
		if (credits == null || credits.Count == 0) {
			// No person credited (an older song): the act's people share it evenly, as they share a performer's royalty.
			var people = act.members?.Where(m => m != null).ToList();
			if (people == null || people.Count == 0) return;
			foreach (var m in people) m.writerPerformanceEarnings += l.WriterAmount / people.Count;
			return;
		}
		float total = credits.Sum(c => c.share);
		foreach (var c in credits) {
			var person = FindPerson(act, c.writerId);
			if (person != null) person.writerPerformanceEarnings += l.WriterAmount * c.share / total;
		}
	}

	private IEnumerable<CashOption> PublishingCashOptions(GameDate today, Func<GameDate?, bool> tooLate, Func<GameDate?, string> late) {
		var next = publishing.Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged && !l.Settled).Select(l => l.PayOnDay).DefaultIfEmpty(-1).Min();
		if (next < 0) yield break;
		float due = PerformanceDueBy(next);
		var when = LocalSceneRoomService.Date(next);
		yield return new("Performance royalties", "radio plays of your songs", "logged by the performing-rights society", 0f, due, when,
			CashCertainty.Owed, false, $"The publisher's half, paid on the {when.ToShortString()} statement." + late(when));
	}

	// ── Save ────────────────────────────────────────────────────────────────────────────────────────

	private PublishingRoyaltyBooks CapturePublishing() => publishing.Lines.Count == 0 ? null : LocalSceneRoomService.Copy(publishing);

	private void RestorePublishing(PublishingRoyaltyBooks data) {
		publishing = data ?? new PublishingRoyaltyBooks();
		publishing.Lines ??= new();
		if (!publishing.LedgerAgrees()) GD.PushWarning("Publishing books disagree with their saved totals; keeping the saved totals.");
	}

	// ── Checks ──────────────────────────────────────────────────────────────────────────────────────

	internal void LogPerformanceWeekForChecks() => LogPerformanceWeek();
	internal void StagePublishingForChecks(PublishingRoyaltyBooks books) => publishing = books;
	internal void PerformanceStatementForChecks(GameDate date) => PerformanceStatement(date);
}
