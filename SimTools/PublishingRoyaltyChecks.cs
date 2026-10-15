using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game gate: the publisher's radio money and the songwriter's half. On the label's own sales of a song it publishes,
/// the 2c mechanical is the publisher's cent (kept) and the writer's cent (paid to the writing act); a song the act kept
/// pays the act the whole 2c as before. A cut-in keeps its share of the writer's half. Radio plays of a song the label
/// publishes are logged weekly off the record's real regional airplay (radioPlay x the region's stations x plays at full
/// rotation), at 4c a play split evenly; records of songs it doesn't publish log nothing; only the plug side is played.
/// The quarter's plays are paid on the 1st of the quarter after next: the label's half into cash before the bank's check
/// (the solvency forecast agrees with the clock), the writer's half straight to the writers by credit share, never through
/// the label's cash. The ledger agrees throughout.
///   --path . SimTools/PublishingRoyaltyChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints PUBLISHING_ROYALTY_CHECKS_PASS / _FAIL.
/// </summary>
public partial class PublishingRoyaltyChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Publishing Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;
			label.cashReserves = 2000f;
			int year = clock.CurrentDate.year;
			var books = desk.Publishing;

			var act = ArtistManager.Instance.GetAllArtists().FirstOrDefault(a => string.IsNullOrEmpty(a.labelId) && a.isActive && a.members?.Count >= 2);
			Check(act != null, $"An unsigned group: {act?.stageName} ({act?.members.Count} members)");
			act.labelId = label.labelId; label.roster.Add(act);
			act.labelOwnsPublishing = true;
			var ours = desk.StageShelfSongForChecks(act, "chk_pub_ours", "Our Song", .8f).Record;
			var song = CompositionCatalogService.GetSong(ours.songId);
			Check(ours.publishingControl == PublishingControlType.LabelAffiliate && ours.publishingControllerLabelId == label.labelId, "The label publishes the act's song");

			// The writer's half on the label's own sales.
			float writer0 = act.writerRoyaltyEarnings, perf0 = act.totalRoyaltyEarnings;
			float paid = MechanicalRoyaltyService.ChargeSide(ours.publishingControl, ours.publishingControllerLabelId, ours.publishingControllerArtistId, act, label, 500,
				id => CompetitorManager.Instance.GetLabel(id), id => ArtistManager.Instance.GetArtist(id));
			Check(Near(paid, 5f) && Near(act.writerRoyaltyEarnings - writer0, 5f) && Near(act.totalRoyaltyEarnings, perf0),
				$"500 copies of our own song: ${paid:F2} to the writers (1c a copy), the publisher's cent kept; not the performer's royalty");
			Check(Near(books.WriterMechanicalPaid, 5f) && books.Lines.Count(l => l.Kind == PublishingLineKind.WriterMechanical) == 1, "Booked once in the publishing books");
			paid = MechanicalRoyaltyService.ChargeSide(ours.publishingControl, ours.publishingControllerLabelId, null, act, label, 500,
				id => CompetitorManager.Instance.GetLabel(id), id => ArtistManager.Instance.GetArtist(id), .5f);
			Check(Near(paid, 2.5f), $"With the label cut in for half the writing, the writer's half is ${paid:F2}");
			act.labelOwnsPublishing = false;
			var kept = desk.StageShelfMasterForChecks(act, "chk_pub_kept", "Their Song", .8f, .8f);
			CompositionCatalogService.CreateArtistOriginal(kept.Record, act, label, act.primaryGenre, year, .8f, .8f, .8f, .5f);
			act.labelOwnsPublishing = true;
			float royalty0 = act.totalRoyaltyEarnings; writer0 = act.writerRoyaltyEarnings;
			paid = MechanicalRoyaltyService.ChargeSide(PublishingControlType.ArtistControlled, null, act.artistId, act, label, 500,
				id => CompetitorManager.Instance.GetLabel(id), id => ArtistManager.Instance.GetArtist(id));
			Check(Near(paid, 10f) && Near(act.totalRoyaltyEarnings - royalty0, 10f) && Near(act.writerRoyaltyEarnings, writer0),
				"A song the act kept still pays the act the whole 2c, as before");
			paid = MechanicalRoyaltyService.ChargeSide(PublishingControlType.PublicDomain, null, null, act, label, 500,
				id => CompetitorManager.Instance.GetLabel(id), id => ArtistManager.Instance.GetArtist(id));
			Check(paid == 0f, "A public-domain song pays nobody");

			// A few weeks of the world, so records are on the air.
			for (int i = 0; i < 21; i++) clock.EndDay();
			var charts = ChartManager.Instance;
			var aired = charts.GetCurrentChart().Where(r => r.baseRecord != null && !r.baseRecord.isPlayerOwned && PlayerDesk.LoggedPerformances(r) > 0)
				.OrderBy(r => r.currentPosition).ToList();
			Check(aired.Count >= 3, $"{aired.Count} charting records with logged airplay");
			var top = aired[0];
			long topPlays = PlayerDesk.LoggedPerformances(top);
			GD.Print($"  scale: #{top.currentPosition} \"{top.baseRecord.title}\" logs {topPlays:N0} plays a week = ${topPlays * PlayerDesk.PerformanceRatePerPlay:N2} " +
				$"(${topPlays * PlayerDesk.PerformanceRatePerPlay / 2f:N2} each to publisher and writers); #{aired[^1].currentPosition} logs {PlayerDesk.LoggedPerformances(aired[^1]):N0}");
			double byHand = top.regionalData.Sum(p => (double)p.Value.radioPlay * (charts.GetRegionById(p.Key)?.media?.totalRadioStations ?? 0) * PlayerDesk.PlaysPerWeekFullRotation);
			Check(Math.Abs(topPlays - byHand) <= 1, $"Plays are regional airplay x stations x {PlayerDesk.PlaysPerWeekFullRotation} a week at full rotation ({byHand:F0})");

			// Nothing is logged for songs the label doesn't publish.
			int lines0 = books.Lines.Count;
			desk.LogPerformanceWeekForChecks();
			Check(books.Lines.Count == lines0, "No record of a song we don't publish logs a play for us");

			// Make one aired record a record of our song (an AI cover, say) and another one's plug side ours; log a week.
			var cover = aired[1].baseRecord; var flip = aired[2].baseRecord;
			var keep = (cover.songId, cover.publishingControl, cover.publishingControllerLabelId, cover.publishingControllerArtistId, cover.originalArtistId,
				flip.bSideIsPlugSide, flip.bSideSongId, flip.bSidePublishingControl, flip.bSidePublishingControllerLabelId, flip.bSideTitle);
			cover.songId = ours.songId; cover.publishingControl = PublishingControlType.LabelAffiliate; cover.publishingControllerLabelId = label.labelId;
			cover.publishingControllerArtistId = null; cover.originalArtistId = act.artistId;
			flip.bSideIsPlugSide = true; flip.bSideSongId = ours.songId; flip.bSidePublishingControl = PublishingControlType.LabelAffiliate;
			flip.bSidePublishingControllerLabelId = label.labelId; flip.bSideTitle = "Our Song (flip)";
			// The flip's A-side is ours too, which must not count: only the plug side is played.
			var flipA = (flip.publishingControl, flip.publishingControllerLabelId);
			flip.publishingControl = PublishingControlType.LabelAffiliate; flip.publishingControllerLabelId = label.labelId;
			long coverPlays = PlayerDesk.LoggedPerformances(aired[1]), flipPlays = PlayerDesk.LoggedPerformances(aired[2]);
			desk.LogPerformanceWeekForChecks();
			int today = LocalSceneRoomService.Day(clock.CurrentDate);
			var logged = books.Lines.Skip(lines0).ToList();
			// Restore the borrowed records, so their own settlement routes nothing to us.
			(cover.songId, cover.publishingControl, cover.publishingControllerLabelId, cover.publishingControllerArtistId, cover.originalArtistId,
				flip.bSideIsPlugSide, flip.bSideSongId, flip.bSidePublishingControl, flip.bSidePublishingControllerLabelId, flip.bSideTitle) = keep;
			(flip.publishingControl, flip.publishingControllerLabelId) = flipA;
			Check(logged.Count == 2 && logged.All(l => l.Kind == PublishingLineKind.PerformanceLogged && l.SongId == ours.songId && l.ArtistId == act.artistId),
				"Two lines: the cover and the flip's plug side, both our song, the writer our act");
			var lc = logged.First(l => l.RecordId == cover.recordId); var lf = logged.First(l => l.RecordId == flip.recordId);
			Check(lc.Performances == coverPlays && Near(lc.Amount, coverPlays * .02f, .02f) && Near(lc.WriterAmount, lc.Amount),
				$"Cover: {coverPlays:N0} plays, ${lc.Amount:F2} to the label and ${lc.WriterAmount:F2} to the writers (4c a play, half each)");
			Check(lf.Title == "Our Song (flip)" && lf.Performances == flipPlays, $"Flip: logged under the plug side ({lf.Title}), {flipPlays:N0} plays, once");
			var payDate = LocalSceneRoomService.Date(lc.PayOnDay);
			var logDate = clock.CurrentDate;
			Check(payDate.day == 1 && (payDate.month - 1) % 3 == 0 && (payDate.year * 12 + payDate.month) - (logDate.year * 12 + (logDate.month - 1) / 3 * 3 + 1) == 6,
				$"Logged {logDate.ToShortString()}, paid {payDate.ToShortString()}: the 1st of the quarter after next");
			Check(books.LedgerAgrees() && Near(books.PerformanceOwed, lc.Amount + lf.Amount), $"Ledger agrees; ${books.PerformanceOwed:F2} owed");
			var row = desk.CashOptions().FirstOrDefault(o => o.Route == "Performance royalties");
			Check(row != null && row.Certainty == PlayerDesk.CashCertainty.Owed && Near(row.Expected, books.PerformanceOwed) && row.CashDate == payDate,
				$"CASH OPTIONS: ${row?.Expected:F2} owed, {row?.Why}");
			Check(desk.PerformanceDueBy(lc.PayOnDay - 1) == 0f && Near(desk.PerformanceDueBy(lc.PayOnDay), books.PerformanceOwed), "Not due a day before the statement");

			// The statement on the clock. Bring the pay day forward to the coming 1st so the check runs now; the forecast must match.
			while (clock.CurrentDate.AddDays(1).day != 1) clock.EndDay();
			int first = LocalSceneRoomService.Day(clock.CurrentDate.AddDays(1));
			lc.PayOnDay = first; lf.PayOnDay = first;
			float owed = books.PerformanceOwed, writers = lc.WriterAmount + lf.WriterAmount;
			var forecast = desk.NextSolvencyCheck(label.cashReserves);
			float cash = label.cashReserves, actWriter = act.writerRoyaltyEarnings;
			var people = act.members.ToDictionary(m => m.personId, m => m.writerPerformanceEarnings);
			clock.EndDay();
			Check(Near(label.cashReserves, forecast.CashAfter, .02f) && Near(label.cashReserves - cash, owed - forecast.Overhead, .02f),
				$"On the 1st: cash ${cash:F2} - ${forecast.Overhead:F2} overhead + ${owed:F2} statement = ${label.cashReserves:F2}, as forecast (${forecast.CashAfter:F2})");
			Check(lc.Settled && lf.Settled && books.PerformanceOwed == 0f && Near(books.PerformancePaid, owed) && books.LedgerAgrees(), "Both lines settled; the statement booked; ledger agrees");
			Check(Near(act.writerRoyaltyEarnings - actWriter, writers), $"${writers:F2} to {act.stageName} as writers, outside the label's cash");
			var credits = song.credits.Where(c => c.writerType == WriterEntityType.Musician && !string.IsNullOrEmpty(c.writerId) && c.share > 0f).ToList();
			double toPeople = act.members.Sum(m => m.writerPerformanceEarnings - people[m.personId]);
			Check(Math.Abs(toPeople - writers) < .02f || credits.Any(c => act.members.All(m => m.personId != c.writerId)),
				$"${toPeople:F2} reached the credited people ({credits.Count} credit{(credits.Count == 1 ? "" : "s")}: " +
				string.Join(", ", act.members.Select(m => $"{m.FullName} {m.writerPerformanceEarnings - people[m.personId]:F2}")) + ")");
			Check(!desk.CashOptions().Any(o => o.Route == "Performance royalties"), "Nothing more owed, so CASH OPTIONS drops the row");

			// The writer's money reaches a person's wealth year as writer income.
			if (BandLife.MemberWealthActive) {
				var m = act.members.OrderByDescending(x => x.writerPerformanceEarnings).First();
				MemberWealthService.OnActYear(act, act.members.ToList(), LineupConstitution.Band, 0f, year);
				float before = m.lastYearWriterIncome;
				m.writerPerformanceEarnings += 7.5;
				MemberWealthService.OnActYear(act, act.members.ToList(), LineupConstitution.Band, 0f, year + 1);
				Check(Near(m.lastYearWriterIncome, 7.5f, .02f), $"A person's year counts the performance money as writer income ({before:F2} -> {m.lastYearWriterIncome:F2})");
			}

			GD.Print($"PUBLISHING_ROYALTY_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"PUBLISHING_ROYALTY_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}
}
