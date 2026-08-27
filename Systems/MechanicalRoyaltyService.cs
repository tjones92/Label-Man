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
		System.Func<string, AILabel> getLabel, System.Func<string, SimulatedArtist> getArtist) {
		if (units <= 0 || sellingLabel == null) return 0f;

		float pool = units * RatePerCopy;
		PublishingRoutingService.Decision routing = PublishingRoutingService.Decide(
			control, controllerLabelId, controllerArtistId, performingArtist, sellingLabel.labelId);

		float writerSlice = pool * routing.WriterArtistFraction;
		float transferSlice = pool * routing.TransferLabelFraction;
		float leakSlice = pool * routing.ExternalLeakFraction;
		// The payer's total is fixed the moment the fractions are drawn -- whether transferSlice lands
		// on a live label or leaks (its controller vanished) changes who receives it, never what it cost.
		float totalExpense = writerSlice + transferSlice + leakSlice;

		if (writerSlice > 0f) {
			SimulatedArtist writer = (!string.IsNullOrEmpty(routing.ControllerArtistId)
				? getArtist?.Invoke(routing.ControllerArtistId) : null) ?? performingArtist;
			if (writer != null) writer.totalRoyaltyEarnings += writerSlice;
		}
		if (transferSlice > 0f) {
			AILabel controller = !string.IsNullOrEmpty(routing.ControllerLabelId) ? getLabel?.Invoke(routing.ControllerLabelId) : null;
			if (controller != null && !ReferenceEquals(controller, sellingLabel)) {
				controller.cashReserves += transferSlice;
				controller.monthlyRevenue += transferSlice;
			}
			// A vanished controller's slice is already folded into totalExpense above as a leak --
			// nothing further to do; it simply has no recipient.
		}
		return totalExpense;
	}
}
