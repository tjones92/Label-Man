using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ProfessionalWritingMarketChecks {
	public static void Run() {
		void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
		Check(ProfessionalWritingMarket.Enabled, "Use --professional-writing-market for market checks");
		var opts = SaveGameService.TestJsonOptions;
		var label = ChartManager.Instance.GetAllLabels().First(l => l.IsActive && l.roster.Count >= 2);
		var rival = ChartManager.Instance.GetAllLabels().First(l => l.IsActive && l.labelId != label.labelId && l.roster.Count > 0);
		label.cashReserves = rival.cashReserves = 10000000;
		label.isPlayerOwned = rival.isPlayerOwned = true; // keep the forced fixture out of autonomous repeat orders
		var artist = label.roster[0]; var other = rival.roster[0];
		var date = TimeManager.Instance.CurrentDate;
		var state = ProfessionalWritingMarket.State;
		var offer = ProfessionalWritingMarket.Offers(label, artist).First(o => o.Available && CompositionCatalogService.AllSongs.Any(s =>
			s.originKind == SongOriginKind.ProfessionalOffice && ProfessionalWritingMarket.SupplierFor(s) == o.WriterId && ProfessionalWritingMarket.CanLicense(s, rival, other)));
		var backCatalogue = CompositionCatalogService.AllSongs.First(s => s.originKind == SongOriginKind.ProfessionalOffice &&
			ProfessionalWritingMarket.SupplierFor(s) == offer.WriterId && ProfessionalWritingMarket.CanLicense(s, rival, other));
		float price = ProfessionalWritingMarket.LicenseFee(backCatalogue, rival, other);
		float hook = backCatalogue.commercialHook;
		backCatalogue.commercialHook = 1 - hook;
		Check(price == ProfessionalWritingMarket.LicenseFee(backCatalogue, rival, other), "Fees do not reveal hidden song quality");
		backCatalogue.commercialHook = hook;
		Check(ProfessionalWritingMarket.TryContract(label, offer.WriterId, WritingContractKind.Exclusive, out _), "Sign capacity contract");
		Check(!ProfessionalWritingMarket.TryContract(rival, offer.WriterId, WritingContractKind.Preferred, out _), "Conflicting supplier contract refused");
		Check(ProfessionalWritingMarket.CanLicense(backCatalogue, rival, other), "Exclusivity excludes pre-existing songs");
		Check(!ProfessionalWritingMarket.TryCommission(rival, other, offer.WriterId, date, out _, out _), "Rival cannot buy reserved capacity");
		float cash = label.cashReserves;
		Check(ProfessionalWritingMarket.TryCommission(label, artist, offer.WriterId, date, out var order, out _), "New writing assignment starts");
		Check(order.SongId == null && order.Status == WritingOrderStatus.Pending, "A commission has no preselected song");
		Check(Math.Abs(cash - label.cashReserves - order.Fee) < 1, "Commission charged once");
		cash = label.cashReserves;
		Check(!ProfessionalWritingMarket.TryCommission(label, label.roster[1], offer.WriterId, date, out _, out _), "Solo or team capacity cannot be double booked");
		Check(label.cashReserves == cash, "Rejected booking leaves cash unchanged");
		label.isPlayerOwned = false;
		Check(!ProfessionalWritingMarket.CanAiCommission(label, artist), "AI cannot reorder while work is pending");
		label.isPlayerOwned = true;
		ProfessionalWritingMarket.Deliver(order.DueDate.AddDays(-1));
		Check(order.Status == WritingOrderStatus.Pending, "No premature delivery");
		string pending = JsonSerializer.Serialize(state, opts);
		ProfessionalWritingMarket.Restore(JsonSerializer.Deserialize<ProfessionalWritingMarketState>(pending, opts));
		Check(pending == JsonSerializer.Serialize(ProfessionalWritingMarket.State, opts), "Pending work and contracts round-trip exactly");
		state = ProfessionalWritingMarket.State;
		order = state.Orders.First(o => o.Id == order.Id);
		ProfessionalWritingMarket.Advance(order.DeliveryWeek, order.DueDate);
		Check(order.Status == WritingOrderStatus.Delivered, "Dated delivery");
		var song = CompositionCatalogService.GetSong(order.SongId);
		Check(song != null && ProfessionalWritingMarket.CanLicense(song, label, artist) && !ProfessionalWritingMarket.CanLicense(song, rival, other), "Delivered song belongs to the assigned act");
		label.isPlayerOwned = false;
		Check(!ProfessionalWritingMarket.CanAiCommission(label, artist), "AI uses an unrecorded paid delivery before buying more work");
		label.isPlayerOwned = true;
		var coverAttempt = SongMaterialSelectionService.BuildCoverForSong(other, new Record { recordId = "market-cover-attempt", primaryGenre = song.primaryGenre }, song, song.primaryGenre, date.year);
		Check(coverAttempt == null, "An unreleased commission cannot bypass first-recording rights through a cover request");
		int count = CompositionCatalogService.SongCount;
		ProfessionalWritingMarket.Deliver(order.DueDate);
		ProfessionalWritingMarket.Advance(order.DeliveryWeek, order.DueDate);
		Check(count == CompositionCatalogService.SongCount, "Repeated daily or weekly processing creates no extra output");
		cash = label.cashReserves;
		int heldUntil = state.Demos[song.songId].HoldUntilWeek;
		Check(ProfessionalWritingMarket.TryLicense(song, label, artist, out _), "Delivered commission requires no license fee");
		Check(label.cashReserves == cash && heldUntil == state.Demos[song.songId].HoldUntilWeek, "Re-reading neither charges nor extends the hold");
		ProfessionalWritingMarket.MarkRecorded(song, artist.artistId);
		label.isPlayerOwned = false;
		Check(!ProfessionalWritingMarket.CanAiCommission(label, artist), "AI commission interval survives recording and applies across suppliers");
		label.isPlayerOwned = true;
		Check(!ProfessionalWritingMarket.Visible(new[] {song}, label, artist).Any(), "Recorded songs leave the professional first-recording market");
		var record = new Record { recordId = "market-check-hit", labelId = label.labelId, artistId = artist.artistId, songId = song.songId };
		var hit = new RecordRuntimeData { baseRecord = record, currentPosition = 20 };
		ProfessionalWritingMarket.ObserveChart(new[] { hit, hit });
		Check(state.Suppliers[offer.WriterId].Hits == 1, "A hit is credited once, not every chart week");
		Check(ProfessionalWritingMarket.Offers(label, artist).First(o => o.WriterId == offer.WriterId).PreviousHits == 1, "Successful partnership remembered");
		cash = rival.cashReserves;
		Check(ProfessionalWritingMarket.TryLicense(backCatalogue, rival, other, out _), "Existing song can be licensed separately");
		Check(rival.cashReserves < cash && !ProfessionalWritingMarket.CanLicense(backCatalogue, label, artist), "Paid first recording cannot be sold twice");
		var contract = state.Contracts.First(c => c.WriterId == offer.WriterId);
		cash = label.cashReserves;
		ProfessionalWritingMarket.Advance(4, date.AddDays(28));
		Check(Math.Abs(cash - label.cashReserves - contract.Retainer) < 1, "Recurring retainer booked once");
		cash = label.cashReserves;
		ProfessionalWritingMarket.Advance(4, date.AddDays(28));
		Check(label.cashReserves == cash, "Week replay cannot double bill");
		label.cashReserves = 0;
		ProfessionalWritingMarket.Advance(8, date.AddDays(56));
		Check(contract.Ended && label.cashReserves == 0, "Unfunded contract ends without overdraft");
		label.isPlayerOwned = false;
		Check(ProfessionalWritingMarket.CanAiCommission(label, artist), "AI commission interval opens after eight weeks");
		Check(!ProfessionalWritingMarket.TryAiCommission(label, artist, offer.WriterId, date.AddDays(56)), "AI respects its operating cash buffer");
		label.isPlayerOwned = true;
		label.cashReserves = 10000000;
		Check(ProfessionalWritingMarket.TryContract(label, offer.WriterId, WritingContractKind.Preferred, out _), "Supplier can offer first refusal after the exclusive term ends");
		state.Suppliers[offer.WriterId].NextWritingWeek = 9;
		int supplyBefore = state.Demos.Count;
		ProfessionalWritingMarket.Advance(9, date.AddDays(63));
		Check(state.Demos.Count > supplyBefore, "Idle writers produce a finite flow of demos");
		var priority = state.Demos.Values.Last(d => d.WriterId == offer.WriterId && d.AvailableWeek == 9);
		Check(priority.PriorityUntilWeek == 11 && priority.LabelId == label.labelId, "Preferred contract supplies a two-week first-refusal window");
		if (priority.Status == WritingDemoStatus.Available)
			Check(!ProfessionalWritingMarket.CanLicense(CompositionCatalogService.GetSong(priority.SongId), rival, other), "Priority is enforced against competitors");
		ProfessionalWritingMarket.Advance(11, date.AddDays(77));
		if (priority.Status == WritingDemoStatus.Available)
			Check(ProfessionalWritingMarket.CanLicense(CompositionCatalogService.GetSong(priority.SongId), rival, other), "Unexercised first refusal returns material to open competition");
		var member = artist.members.First(m => m.isActive);
		Check(ProfessionalWritingMarket.TryCommission(label, artist, offer.WriterId, date.AddDays(77), out var coWrite, out _, member.personId), "Member-plus-professional work uses real capacity");
		ProfessionalWritingMarket.Advance(16, date.AddDays(112));
		Check(coWrite.Status == WritingOrderStatus.Delivered && CompositionCatalogService.GetSong(coWrite.SongId).credits.Any(c => c.writerId == member.personId && c.isArtistMember), "Member co-writing credit survives delivery");
		Check(CompositionCatalogService.GetSong(coWrite.SongId).writingAssignmentFamiliarity > 0, "Repeat recording history informs the assignment");
		Check(ProfessionalWritingMarket.CanLicense(backCatalogue, label, artist), "Unused license expires and returns to the market");
		bool rejected = false;
		try { ProfessionalWritingMarket.RequireSaved(null); } catch (InvalidOperationException) { rejected = true; }
		Check(rejected && SaveGameService.WritingVersion == 37, "Saved policy and version enforced");
		GD.Print("ProfessionalWritingMarketChecks PASS: finite capacity, delayed delivery, exclusive services, open back catalogue, fee truth boundary, competing licenses, partnerships, retainers, replay safety and persistence.");
	}

	public static void StageRoundTrip() {
		var label = ChartManager.Instance.GetAllLabels().First(l => l.IsActive && l.roster.Count > 0);
		label.cashReserves = 10000000;
		var artist = label.roster[0];
		var offer = ProfessionalWritingMarket.Offers(label, artist).First(o => o.Available);
		if (!ProfessionalWritingMarket.TryContract(label, offer.WriterId, WritingContractKind.Preferred, out _) ||
			!ProfessionalWritingMarket.TryCommission(label, artist, offer.WriterId, TimeManager.Instance.CurrentDate, out _, out _))
			throw new InvalidOperationException("Could not stage the market round-trip fixture.");
	}

	public static void CheckLiveState() {
		if (!ProfessionalWritingMarket.Enabled) return;
		var state = ProfessionalWritingMarket.State;
		if (state == null || state.Orders.Where(o => o.Status == WritingOrderStatus.Pending).GroupBy(o => o.WriterId).Any(g => g.Count() > 1) ||
			state.Orders.Where(o => o.Status == WritingOrderStatus.Pending).GroupBy(o => o.ArtistId).Any(g => g.Count() > 1) ||
			state.Demos.Values.Any(d => CompositionCatalogService.GetSong(d.SongId) == null || !float.IsFinite(d.FeePaid) || d.FeePaid < 0) ||
			state.Contracts.Where(c => !c.Ended && c.EndWeek > state.LastWeek).GroupBy(c => c.WriterId).Any(g => g.Count() > 1))
			throw new InvalidOperationException("Writing market capacity, rights or financial invariant failed.");
		GD.Print($"WRITING_MARKET_STATE week={state.LastWeek} suppliers={state.Suppliers.Count} demos={state.Demos.Count} " +
			$"pending={state.Orders.Count(o => o.Status == WritingOrderStatus.Pending)} delivered={state.Orders.Count(o => o.Status == WritingOrderStatus.Delivered)} " +
			$"shelved={state.Demos.Values.Count(d => d.Status == WritingDemoStatus.Shelved)} recorded={state.Demos.Values.Count(d => d.Status == WritingDemoStatus.Recorded)} " +
			$"contracts={state.Contracts.Count(c => !c.Ended && c.EndWeek > state.LastWeek)} partnerships={state.Partnerships.Count}");
	}
}
