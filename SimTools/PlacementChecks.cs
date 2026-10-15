using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 5A gate (§7.2): place a song. Only songs whose publishing the label controls, with a
/// tape to play; buyers are real labels with an act in the song's sound; a call gives public answers only; a posted tape is
/// answered weeks later if anyone listens; a visit needs the town, the hours and a copy; the ear reads the song and never
/// moves on unchanged evidence, so a pass stands; signing pays the advance out of the buyer's cash and holds the song for
/// it; the buyer's own act cuts it as its next single when it would have recorded an outside song (an act cutting its own
/// song keeps to it); the record is the buyer's, tagged, with the player's publishing intact; the song's slice of the
/// buyer's gross recoups the advance first (the buyer keeps that part) and the rest is paid on the statement, not as weekly
/// receipts; an uncut hold lapses with the advance kept; a cut of a released song is a cover of the player's record.
///   --path . SimTools/PlacementChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints PLACEMENT_CHECKS_PASS / _FAIL.
/// </summary>
public partial class PlacementChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Placement Checks", "detroit", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;
			label.cashReserves = 5000f;
			int Today() => LocalSceneRoomService.Day(clock.CurrentDate);
			int year = clock.CurrentDate.year;

			// An unsigned act in the commonest sound among the bigger labels' rosters, so plenty of acts could sing its songs.
			var genre = CompetitorManager.Instance.GetOperatingLabels().Where(l => !l.isPlayerOwned && l.IsActive && l.tier is LabelTier.Major or LabelTier.MidTier)
				.SelectMany(l => l.roster).Where(a => a.isActive && a.members?.Count > 0).GroupBy(a => a.primaryGenre)
				.OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
			var act = ArtistManager.Instance.GetAllArtists().FirstOrDefault(a => string.IsNullOrEmpty(a.labelId) && a.isActive && a.members?.Count > 0 && a.primaryGenre == genre);
			Check(act != null, $"An unsigned act in the most common sound ({genre}): {act?.stageName}");
			act.labelId = label.labelId; label.roster.Add(act);
			act.labelOwnsPublishing = true;
			var strong = desk.StageShelfSongForChecks(act, "chk_strong", "Strong Song", .95f);
			var weak = desk.StageShelfSongForChecks(act, "chk_weak", "Weak Song", .05f);
			var plain = desk.StageShelfSongForChecks(act, "chk_plain", "Plain Song", .55f);
			string sStrong = strong.Record.songId, sWeak = weak.Record.songId, sPlain = plain.Record.songId;
			Check(sStrong != null && CompositionCatalogService.GetSong(sStrong).rights.controllerLabelId == label.labelId, "Each master carries a song whose publishing is the label's");

			// What can be placed: only the label's publishing, and only with a tape to play.
			Check(desk.PlaceableSong(sStrong).Block == null && desk.PlaceableSongIds().Contains(sStrong), "The strong song is placeable");
			act.labelOwnsPublishing = false;
			var kept = desk.StageShelfMasterForChecks(act, "chk_kept", "Act's Own", .9f, .9f);
			var keptSong = CompositionCatalogService.CreateArtistOriginal(kept.Record, act, label, act.primaryGenre, year, .9f, .9f, .9f, .5f);
			act.labelOwnsPublishing = true;
			Check(desk.PlaceableSong(keptSong.songId).Block?.Contains("isn't yours") == true, "A song the act kept the publishing on can't be placed: " + desk.PlaceableSong(keptSong.songId).Block);
			var standard = CompositionCatalogService.GetStandardsForGenre(genre).FirstOrDefault() ?? CompositionCatalogService.GetStandardsForGenre(Genre.TraditionalPop).First();
			Check(desk.PlaceableSong(standard.songId).Block != null, "Nor somebody else's standard");

			// Buyers: real labels with an act in its sound, best fits first; the ear reads the song, fixed.
			var buyers = desk.PlacementBuyersFor(sStrong, int.MaxValue);
			Check(buyers.Count > 0 && buyers.All(b => !b.Label.isPlayerOwned && b.Label.IsActive && b.Label.roster.Any(a => a.isActive && a.primaryGenre == genre) == b.OwnSound),
				$"{buyers.Count} real labels with an act in its sound (own sound = an act in exactly its genre)");
			Check(buyers.SkipWhile(b => b.Block == null).All(b => b.Block != null), "Labels that can pay come before ones that can't");
			var x = buyers.Where(b => b.Block == null && b.Reachable && b.OwnSound
					&& desk.PlacementReadForChecks(b.Label, sStrong, false) >= PlayerDesk.PlacementBarForChecks(b.Label, true)
					&& desk.PlacementReadForChecks(b.Label, sWeak, false) < PlayerDesk.PlacementBarForChecks(b.Label, true))
				.OrderByDescending(b => b.Label.roster.Count(a => a.isActive && a.primaryGenre == genre)).ThenBy(b => b.Label.labelId, StringComparer.Ordinal).FirstOrDefault();
			Check(x != null, $"A reachable label that would hear the strong song and not the weak one: {x?.Label.labelName} ({x?.Label.tier}, {x?.CityName}, " +
				$"{x?.Label.roster.Count(a => a.isActive && a.primaryGenre == genre)} acts in the sound)");
			var X = x.Label;
			Check(Near(desk.PlacementReadForChecks(X, sStrong, false), desk.PlacementReadForChecks(X, sStrong, false), 0f), "The ear is a fixed read, not a reroll");
			var y = buyers.First(b => b.Block == null && b.Label != X);

			// Calls: office hours, the toll, public answers only, no repeat inside four weeks.
			if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) Check(desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _), "Wait for the office to open");
			float cash = label.cashReserves; int hour = clock.CurrentHour; float toll = desk.MasterCallToll(X);
			Check(desk.CallPlacementBuyer(X.labelId, sStrong, out var called), "Call about the strong song: " + called);
			Check(Near(cash - label.cashReserves, toll) && clock.CurrentHour == hour + PlayerDesk.MasterCallHours, $"It costs the ${toll:F2} toll and an hour");
			var callA = desk.PlacementCalls.Last();
			Check(callA.Welcome && callA.AppointmentDay is int a && a >= Today() + 3 && a <= Today() + 7, $"\"Send it over, or bring it by\" on day {callA.AppointmentDay}");
			hour = clock.CurrentHour;
			Check(!desk.CallPlacementBuyer(X.labelId, sStrong, out var again) && again.Contains("spoke") && clock.CurrentHour == hour, "A second call inside four weeks is refused free: " + again);
			Check(desk.CallPlacementBuyer(X.labelId, sWeak, out _) && desk.PlacementCalls.Last().Welcome, "A call about the weak song is just as welcome: the phone doesn't hear it");
			var callB = desk.PlacementCalls.Last();

			// Post: the dub and postage, answered weeks later; one tape in flight per label and song.
			cash = label.cashReserves;
			Check(desk.PostPlacementTape(y.Label.labelId, sPlain, out var posted), "Post a tape of the plain song to another label: " + posted);
			var post = desk.PlacementPostings.Last();
			Check(Near(cash - label.cashReserves, PlayerDesk.MasterTapeDub + PlayerDesk.MasterPostage) && !post.Solicited
				&& post.AnswerDay >= Today() + 14 && post.AnswerDay <= Today() + 28, $"Unsolicited, answer on day {post.AnswerDay}");
			Check(!desk.PostPlacementTape(y.Label.labelId, sPlain, out var twice) && twice.Contains("on its way"), "A second tape while one is in the post is refused: " + twice);

			Check(desk.CutAcetate("chk_strong", out _) && desk.CutAcetate("chk_weak", out _), "Cut acetates of both to play them");
			int firstDay = Math.Min(callA.AppointmentDay.Value, callB.AppointmentDay.Value);
			if (desk.CurrentCityId != x.CityId) { clock.EndDay(); Check(desk.DriveTo(x.CityId, out var drove), $"Drive to {x.CityName}: {drove}"); }
			while (Today() < firstDay) clock.EndDay();
			if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
			hour = clock.CurrentHour;
			Check(!desk.VisitPlacementBuyer(X.labelId, sPlain, out var noCopy) && noCopy.Contains("acetate") && clock.CurrentHour == hour, "No copy of the plain song: refused free: " + noCopy);

			SongPlacementOffer offer = null;
			for (int guard = 0; guard < 6 && (offer == null || desk.PlacementPasses.All(p => p.SongId != sWeak)); guard++) {
				if (Today() == callA.AppointmentDay && offer == null) {
					if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
					hour = clock.CurrentHour;
					Check(desk.VisitPlacementBuyer(X.labelId, sStrong, out var heard) && clock.CurrentHour == hour + PlayerDesk.MasterVisitHours, "Play the strong song on the appointment (2h): " + heard);
					offer = desk.PlacementOffers.FirstOrDefault(o => o.SongId == sStrong && o.BuyerLabelId == X.labelId);
				}
				if (Today() == callB.AppointmentDay && desk.PlacementPasses.All(p => p.SongId != sWeak)) {
					if (clock.CurrentHour < PlayerDesk.BackerOfficeFromHour) desk.PassTime(desk.HoursUntil(PlayerDesk.BackerOfficeFromHour), out _);
					Check(desk.VisitPlacementBuyer(X.labelId, sWeak, out var pass), "Play the weak song on its appointment: " + pass);
				}
				if (offer == null || desk.PlacementPasses.All(p => p.SongId != sWeak)) clock.EndDay();
			}
			Check(offer != null && offer.Route == MasterRoute.Visit && offer.ExpiresDay == offer.MadeDay + PlayerDesk.PlacementOfferDays && !offer.Cover,
				$"An offer on the spot, open {PlayerDesk.PlacementOfferDays} days, not a cover (nothing of it is out): {offer?.Answer}");
			Check(offer.Advance >= PlayerDesk.PlacementAdvanceMin && offer.Advance <= PlayerDesk.PlacementAdvanceMax && offer.HoldDays == PlayerDesk.PlacementHoldDays,
				$"${offer.Advance} advance, held {offer.HoldDays} days: {offer.Why}");
			Check(!offer.Answer.Contains("0."), "The answer is words, never the hidden score: " + offer.Answer);
			var weakPass = desk.PlacementPasses.Last(p => p.SongId == sWeak);
			Check(weakPass.BuyerLabelId == X.labelId && weakPass.Evidence == "unreleased", "The weak song was passed on: " + weakPass.Reason);
			hour = clock.CurrentHour;
			Check(!desk.VisitPlacementBuyer(X.labelId, sWeak, out var stands) && stands.Contains("passed") && clock.CurrentHour == hour, "The pass stands on unchanged evidence, free: " + stands);
			var row = desk.CashOptions().FirstOrDefault(o => o.Route == "Song offer");
			Check(row != null && row.Certainty == PlayerDesk.CashCertainty.Accepted && row.Executable && Near(row.Expected, offer.Advance), "CASH OPTIONS: the advance, agreed if you sign, signable here");

			// Sign from the office: the advance comes out of the buyer's cash; the song is held for its acts.
			if (!desk.AtHome) Check(desk.DriveHome(out _), "Drive home");
			if (clock.CurrentHour + PlayerDesk.MasterSignHours > 22) clock.EndDay();
			cash = label.cashReserves; float revenue = label.monthlyRevenue, buyerCash = X.cashReserves;
			Check(desk.AcceptPlacementOffer(offer.Id, out var signed), "Sign: " + signed);
			var deal = desk.Placements.Last();
			Check(Near(label.cashReserves - cash, offer.Advance) && Near(label.monthlyRevenue - revenue, offer.Advance) && Near(buyerCash - X.cashReserves, offer.Advance),
				$"${offer.Advance} paid over today, out of {X.labelName}'s own cash");
			Check(deal.Status == SongPlacementStatus.Held && deal.HoldUntilDay == deal.SignedDay + PlayerDesk.PlacementHoldDays && desk.Masters.Contains(strong),
				"Held for their acts; your master stays on your shelf (the song was placed, not the record)");
			Check(desk.PlaceableSong(sStrong).Block?.Contains("holds") == true && desk.PlacementBuyersFor(sStrong).Count == 0, "Nobody else can be pitched it while it's held");
			Check(deal.Ledger.Count == 1 && deal.Ledger[0].Kind == SongPlacementLedgerKind.Advance && deal.LedgerAgrees(), "One ledger line: the advance");

			// A self-writing act's single keeps to its own song; another label's single never takes it.
			var probeRec = new Record { recordId = "chk_probe", primaryGenre = genre, secondaryGenre = genre, artistId = "x" };
			var xAct = X.roster.First(r => r.isActive && r.members?.Count > 0 && r.primaryGenre == genre);
			Check(desk.PlacedSongFor(X, xAct, probeRec, new SelectedSongMaterial { Source = SongMaterialSource.ArtistWritten }, year, out _) == null,
				$"{xAct.stageName} about to cut its own song keeps to it");
			Check(desk.PlacedSongFor(y.Label, xAct, probeRec, new SelectedSongMaterial { Source = SongMaterialSource.CoverStandard }, year, out _) == null,
				"Another label's session never takes a song it doesn't hold");

			// The buyer's act cuts it in its own session, as its next single.
			float published0 = label.lifetimePublishingReceived;
			for (int i = 0; i < PlayerDesk.PlacementHoldDays && deal.Status == SongPlacementStatus.Held; i++) clock.EndDay();
			Check(deal.Status == SongPlacementStatus.Out, $"{deal.ArtistName} cut it for {X.labelName} on day {deal.ReleasedDay} ({deal.ReleasedDay - deal.SignedDay} days after signing)");
			var runtime = ChartManager.Instance.GetRecordRuntimeData(deal.RecordId);
			var cutter = ArtistManager.Instance.GetArtist(deal.ArtistId);
			Check(runtime != null && runtime.baseRecord.labelId == X.labelId && !runtime.baseRecord.isPlayerOwned && runtime.baseRecord.playerPlacementId == deal.Id
				&& cutter?.labelId == X.labelId && cutter.primaryGenre == genre, $"Their record by their own act ({cutter?.stageName}), tagged with the contract");
			Check(runtime.baseRecord.songId == sStrong && runtime.baseRecord.title == "Strong Song" && !runtime.baseRecord.isCover
				&& runtime.baseRecord.publishingControl == PublishingControlType.LabelAffiliate && runtime.baseRecord.publishingControllerLabelId == label.labelId,
				"Your song, under its own name, your publishing intact (never captured by their arm); a first recording, not a cover");
			Check(desk.ReleasedRecords.All(r => r.baseRecord.recordId != deal.RecordId) && deal.NextStatementDay == deal.ReleasedDay + PlayerDesk.PlacementStatementDays,
				"Not in your catalogue; first statement a quarter after release");

			// Their sales: the song's slice recoups the advance first, then accrues to you -- not this week's receipts.
			for (int i = 0; i < 28; i++) clock.EndDay();
			Check(deal.Units > 0 && deal.Units <= runtime.totalUnitsSold && deal.Units >= runtime.totalUnitsSold - runtime.unitsThisWeek,
				$"{deal.Units} units billed of {runtime.totalUnitsSold} sold (a week's lag at most)");
			float perUnit = deal.Earned / Mathf.Max(1, deal.Units);
			Check(perUnit > CompetitorManager.PublishingShareOfGross * .5f && perUnit < CompetitorManager.PublishingShareOfGross * 1.5f,
				$"${deal.Earned:F2} earned, {perUnit * 100f:F2}c a copy: the {CompetitorManager.PublishingShareOfGross:P0} slice of their gross");
			Check(Near(deal.Recouped, Mathf.Min(deal.Advance, deal.Earned)) && deal.LedgerAgrees(), $"${deal.Recouped:F2} against the advance; ledger agrees");
			Check(Near(label.lifetimePublishingReceived, published0), "None of it reached you as weekly receipts: it waits for the statement");

			// The statement pays what's owed past the advance.
			deal.NextStatementDay = Today();
			cash = label.cashReserves;
			float owed = deal.OwedNow;
			desk.ProcessSongPlacementsForChecks();
			Check(Near(deal.Paid, deal.Payable) && Near(label.cashReserves - cash, owed) && deal.NextStatementDay == Today() + PlayerDesk.PlacementStatementDays && deal.LedgerAgrees(),
				$"Statement: ${owed:F2} paid over; next in a quarter");

			// The same song can be pitched again now it's cut (a second version is a cover's business), but not back to X.
			Check(desk.PlaceableSong(sStrong).Block == null && desk.PlacementBuyersFor(sStrong, int.MaxValue).Any(b => b.Label == X), "Cut and out: the song is free to pitch again");
			Check(!desk.CallPlacementBuyer(X.labelId, sStrong, out var already) && already.Contains("already has"), "...but not to the label that has it: " + already);

			// Hand-settled weeks: recoupment first, the buyer keeps that part.
			desk.StagePlacementForChecks(new SongPlacement { Id = "chk_hand", BuyerLabelId = X.labelId, BuyerName = X.labelName, SongId = sPlain, Title = "Plain Song",
				Advance = 30f, Status = SongPlacementStatus.Out, RecordId = "chk_hand_rec", NextStatementDay = Today() + 91,
				Ledger = { new SongPlacementLedgerEntry { Kind = SongPlacementLedgerKind.Advance, Amount = 30f } } });
			var hand = desk.Placements.First(p => p.Id == "chk_hand");
			float k1 = desk.SettlePlacementWeekForChecks("chk_hand", "chk_hand_rec", 200, 20f);
			float k2 = desk.SettlePlacementWeekForChecks("chk_hand", "chk_hand_rec", 200, 20f);
			float k3 = desk.SettlePlacementWeekForChecks("chk_hand", "other_rec", 200, 20f);
			Check(Near(k1, 20f) && Near(k2, 10f) && Near(k3, 0f) && Near(hand.Recouped, 30f) && Near(hand.Earned, 40f) && Near(hand.OwedNow, 10f) && hand.Units == 400 && hand.LedgerAgrees(),
				"The first $30 recoups (the buyer keeps it), then $10 is owed; another record's slice is no business of this contract");

			// The buyer folds while it's out: a final statement and the end.
			var status = X.status;
			X.status = LabelStatus.Defunct;
			cash = label.cashReserves; float dealOwed = deal.OwedNow;
			desk.ProcessSongPlacementsForChecks();
			Check(hand.Status == SongPlacementStatus.Ended && hand.EndNote.Contains("went under") && Near(hand.Paid, 10f) && Near(label.cashReserves - cash, 10f + dealOwed, .02f)
				&& deal.Status == SongPlacementStatus.Ended && hand.LedgerAgrees() && deal.LedgerAgrees(), "Their label folded: final statements, then the contracts end");
			X.status = status;

			// A hold that is never cut lapses; the advance is kept and the song is free. A single that was made but never
			// shipped doesn't count as the cut.
			var z = buyers.First(b => b.Block == null && b.Label != X && b.Label != y.Label).Label;
			desk.StagePlacementForChecks(new SongPlacement { Id = "chk_pending", BuyerLabelId = z.labelId, BuyerName = z.labelName, SongId = sPlain, Title = "Plain Song",
				Advance = 25f, Status = SongPlacementStatus.Held, SignedDay = Today() - 10, HoldUntilDay = Today() + 100, PendingRecordId = "never_shipped", PendingDay = Today() - 1,
				Ledger = { new SongPlacementLedgerEntry { Kind = SongPlacementLedgerKind.Advance, Amount = 25f } } });
			var pending = desk.Placements.First(p => p.Id == "chk_pending");
			desk.ProcessSongPlacementsForChecks();
			Check(pending.Status == SongPlacementStatus.Held && pending.PendingRecordId == null, "A single made and dropped (no money for it) leaves the song held for the next one");
			pending.HoldUntilDay = Today() - 1;
			cash = label.cashReserves;
			desk.ProcessSongPlacementsForChecks();
			Check(pending.Status == SongPlacementStatus.Lapsed && Near(label.cashReserves, cash) && desk.PlaceableSong(sPlain).Block == null
				&& pending.Ledger.Select(e => e.Kind).SequenceEqual(new[] { SongPlacementLedgerKind.Advance, SongPlacementLedgerKind.Lapsed }) && pending.LedgerAgrees(),
				"Never cut in the hold: it lapses, the advance is yours, the song is free");

			// A song whose record is out: a buyer's cut is a cover of the player's record.
			desk.ReleaseMasterForChecks(plain);
			var mine = desk.ReleasedRecords.First(r => r.baseRecord.songId == sPlain);
			Check(desk.PlacementEvidence(sPlain) == "released|breakouts=0", "The evidence changed: your record of it is out");
			var zAct = z.roster.FirstOrDefault(r => r.isActive && r.members?.Count > 0 && r.primaryGenre == genre)
				?? z.roster.First(r => r.isActive && r.members?.Count > 0 && CompositionCatalogService.PoolFamilyOf(new SongComposition { primaryGenre = r.primaryGenre }) == CompositionCatalogService.PoolFamilyOf(CompositionCatalogService.GetSong(sPlain)));
			desk.StagePlacementForChecks(new SongPlacement { Id = "chk_cover", BuyerLabelId = z.labelId, BuyerName = z.labelName, SongId = sPlain, Title = "Plain Song",
				Advance = 25f, Status = SongPlacementStatus.Held, SignedDay = Today(), HoldUntilDay = Today() + 182,
				Ledger = { new SongPlacementLedgerEntry { Kind = SongPlacementLedgerKind.Advance, Amount = 25f } } });
			var coverRec = new Record { recordId = "chk_cover_rec", primaryGenre = zAct.primaryGenre, secondaryGenre = zAct.secondaryGenre, artistId = zAct.artistId, labelId = z.labelId, hookStrength = .5f };
			var mat = desk.PlacedSongFor(z, zAct, coverRec, new SelectedSongMaterial { Source = SongMaterialSource.ExternalProfessional }, year, out var coverId);
			Check(mat != null && coverId == "chk_cover" && mat.IsCover && mat.Song.songId == sPlain && mat.OriginalRecordId == mine.baseRecord.recordId,
				$"{zAct.stageName}'s cut would be a cover of your record ({mat?.Source})");
			SongMaterialApplicationService.Apply(coverRec, mat, z, zAct);
			desk.StampPlacedCut(coverRec, coverId);
			Check(coverRec.isCover && coverRec.originalRecordId == mine.baseRecord.recordId && coverRec.publishingControllerLabelId == label.labelId
				&& coverRec.playerPlacementId == "chk_cover" && desk.Placements.First(p => p.Id == "chk_cover").PendingRecordId == "chk_cover_rec",
				"Stamped: a cover, your publishing, tagged, waiting to reach the shops");

			// The posted tape: answered by letter, or never.
			while (Today() < post.AnswerDay + 1) clock.EndDay();
			var after = desk.PlacementPostings.FirstOrDefault(p => p == post);
			bool letter = desk.PlacementOffers.Any(o => o.BuyerLabelId == y.Label.labelId && o.SongId == sPlain) || desk.PlacementPasses.Any(p => p.BuyerLabelId == y.Label.labelId && p.SongId == sPlain);
			Check(after != null ? after.NoReply && !letter : letter || desk.PlaceableSong(sPlain).Block != null,
				after != null ? "No word back on the posted tape" : "The posted tape was answered (or the song was held elsewhere first)");

			Check(desk.Placements.All(d => d.LedgerAgrees()), "Every placement's ledger agrees");
			GD.Print($"PLACEMENT_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr($"PLACEMENT_CHECKS_FAIL after {checks}: {e.Message}\n{e.StackTrace}");
			GetTree().Quit(1);
		}
	}
}
