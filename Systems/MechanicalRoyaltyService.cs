/// <summary>
/// Publishing &amp; Cover-Song Directive Part II, §II.0: the compulsory mechanical royalty -- a flat
/// 2¢ per composition per copy SOLD, unmoved from the 1909 Act until the 1976 Act took effect on
/// 1 January 1978, so it needs no curve or keyframe across this game's 1960s window. It attaches to
/// manufacture-and-sale, never to writing, teaching, rehearsing, or performing a song, and never to
/// <c>PressStock.PromoRemaining</c> (free goods aren't royalty-bearing).
///
/// Unlike <see cref="PublishingRoutingService"/>'s existing 0.11-of-gross pool -- a REALLOCATION of
/// money the settlement already counted -- the mechanical is a brand-new liability that did not exist
/// in the economy before. So it is charged unconditionally here, independent of
/// <see cref="PublishingRoutingService.RoutingEnabled"/> (which only gates whether the *pre-existing*
/// pool's routing is live). It reuses <see cref="PublishingRoutingService.Decide"/> purely for the
/// counterparty split: who gets paid is the same question either royalty asks of the same
/// <see cref="SongComposition.rights"/>.
///
/// Player-only. Both call sites (PlayerDesk.BookSale/SellCartonToOneStop, and the isPlayerOwned branch
/// of CompetitorManager.CalculateLabelRevenue) are gated so this class never executes for an AI
/// label -- an AI record's composition cost is already inside its calibrated pressing/COGS numbers.
/// Structurally unreachable in any headless AI-only run.
/// </summary>
public static class MechanicalRoyaltyService {
	public const float RatePerCopy = 0.02f;
	/// <summary>The writer's share of a song's publishing income (the period's even publisher/writer split).</summary>
	public const float WriterShare = 0.50f;

	/// <summary>Player-side books: the writer's half of a mechanical was paid to an act (writer, amount, units). Set by
	/// PlayerDesk; null in AI-only runs, where nothing here executes anyway.</summary>
	public static System.Action<SimulatedArtist, float, int> WriterHalfPaid;

	/// <summary>Writer ids that start with this are people outside the label and the act whose name went on a song (a cut-in
	/// that is not the label's own: Jack Germain's, inner-life §3.5 H5). Their share of the writer's money is paid to them.</summary>
	public const string OutsiderWriterPrefix = "outsider:";

	/// <summary>Player-side books: an outsider's slice of a song's writer money was paid (amount, units). Set by PlayerDesk;
	/// null in AI-only runs, where no record carries an outsider credit.</summary>
	public static System.Action<float, int> OutsiderCutInPaid;

	/// <summary>The share of a record's A-side writer credit held by outsiders (a third-party cut-in). 0 for every AI record.</summary>
	public static float OutsiderCutInShare(Record record) {
		if (record?.songwriterIds == null) return 0f;
		float share = 0f;
		for (int i = 0; i < record.songwriterIds.Length && i < (record.songwriterShares?.Length ?? 0); i++)
			if (record.songwriterIds[i] != null && record.songwriterIds[i].StartsWith(OutsiderWriterPrefix, System.StringComparison.Ordinal)) share += record.songwriterShares[i];
		return share;
	}

	/// <summary>The share of a record's A-side writer credit held by the selling label itself (a cut-in).</summary>
	public static float LabelCutInShare(Record record, AILabel label) {
		if (record?.songwriterIds == null || label == null) return 0f;
		string id = CowritingService.LabelWriterId(label.labelId);
		float share = 0f;
		for (int i = 0; i < record.songwriterIds.Length && i < (record.songwriterShares?.Length ?? 0); i++)
			if (record.songwriterIds[i] == id) share += record.songwriterShares[i];
		return share;
	}

	/// <summary>
	/// Charges ONE composition's mechanical for <paramref name="units"/> copies, resolves who actually
	/// controls the song (self, a writer-artist, another in-game label, or nobody in-game), and applies
	/// the RECIPIENT-side credit directly (a writer's royalty accrual, or another label's cash). Does
	/// NOT touch <paramref name="sellingLabel"/>'s own cash -- callers subtract the returned total
	/// expense from wherever their own bookkeeping convention keeps it (BookSale's cashReserves,
	/// CalculateLabelRevenue's recordRevenue). Returns 0 when the selling label controls the song
	/// itself: paying yourself is a wash, not a transaction.
	/// </summary>
	public static float ChargeSide(
		PublishingControlType control, string controllerLabelId, string controllerArtistId,
		SimulatedArtist performingArtist, AILabel sellingLabel, int units,
		System.Func<string, AILabel> getLabel, System.Func<string, SimulatedArtist> getArtist,
		float sellingLabelWriterCutIn = 0f, float outsiderCutIn = 0f) {
		if (units <= 0 || sellingLabel == null) return 0f;

		float pool = units * RatePerCopy;
		PublishingRoutingService.Decision routing = PublishingRoutingService.Decide(
			control, controllerLabelId, controllerArtistId, performingArtist, sellingLabel.labelId);

		// A label cut-in (band-member simulation §4.14 layer 4): the selling label is credited on the song, so
		// its share of the writer's slice never leaves the building. Player-only callers pass it; 0 otherwise.
		float labelCut = System.Math.Clamp(sellingLabelWriterCutIn, 0f, 1f);
		// An outsider's cut-in (a third-party payee): it comes out of the writer's share like the label's, but the label still
		// pays it, to him. The label's expense is the same with or without it; only who receives it changes.
		float outsider = System.Math.Clamp(outsiderCutIn, 0f, 1f - labelCut);
		float writerSlice = pool * routing.WriterArtistFraction * (1f - labelCut - outsider);
		float outsiderSlice = pool * routing.WriterArtistFraction * outsider;
		float transferSlice = pool * routing.TransferLabelFraction;
		float leakSlice = pool * routing.ExternalLeakFraction;
		// The payer's total is fixed the moment the fractions are drawn -- whether transferSlice lands
		// on a live label or leaks (its controller vanished) changes who receives it, never what it cost.
		float totalExpense = writerSlice + outsiderSlice + transferSlice + leakSlice;
		float outsiderPaid = outsiderSlice;

		// Early-game: the songwriter's half. In 1960 the 2c was split between publisher and writer, so a label that
		// publishes its own act's song keeps only the publisher's cent and owes the writer the other. (A cut-in's share of
		// the writer's half is the label's own, as above.) Public-domain material has no writer to pay.
		if (routing.Counterparty == PublishingCounterparty.LabelKeeps
			&& control is PublishingControlType.LabelAffiliate or PublishingControlType.LabelBuyout) {
			float writerHalf = pool * WriterShare * (1f - labelCut - outsider);
			float outsiderHalf = pool * WriterShare * outsider;
			SimulatedArtist writer = (!string.IsNullOrEmpty(controllerArtistId) ? getArtist?.Invoke(controllerArtistId) : null) ?? performingArtist;
			if (writer != null && writerHalf > 0f) {
				writer.writerRoyaltyEarnings += writerHalf;
				totalExpense += writerHalf;
				WriterHalfPaid?.Invoke(writer, writerHalf, units);
			}
			if (writer != null && outsiderHalf > 0f) { totalExpense += outsiderHalf; outsiderPaid += outsiderHalf; }
		}
		if (outsiderPaid > 0f) OutsiderCutInPaid?.Invoke(outsiderPaid, units);

		if (writerSlice > 0f) {
			SimulatedArtist writer = (!string.IsNullOrEmpty(routing.ControllerArtistId)
				? getArtist?.Invoke(routing.ControllerArtistId) : null) ?? performingArtist;
			if (writer != null) writer.totalRoyaltyEarnings += writerSlice;
		}
		if (transferSlice > 0f) {
			AILabel controller = !string.IsNullOrEmpty(routing.ControllerLabelId) ? getLabel?.Invoke(routing.ControllerLabelId) : null;
			if (controller != null && !ReferenceEquals(controller, sellingLabel)) {
				LabelCompanyAccounts.ChangeCash(controller, transferSlice, LabelCashKind.PublishingReceipts);
				controller.monthlyRevenue += transferSlice;
			}
			// A vanished controller's slice is already folded into totalExpense above as a leak --
			// nothing further to do; it simply has no recipient.
		}
		return totalExpense;
	}
}
