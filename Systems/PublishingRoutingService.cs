/// <summary>
/// Publishing &amp; Cover-Song Phase 3. Resolves how a record's publishing slice (the existing
/// <c>PublishingShareOfGross</c> pool) is routed at settlement, from the record's attached
/// <see cref="PublishingControlType"/> and controller ids.
///
/// LOAD-BEARING DISCIPLINE (see SimTools/PublishingCoverSongDirective.md §3): this is a REALLOCATION of
/// the existing pool, never an addition to LabelNet. It enriches WHO the counterparty is (external
/// publisher vs the label that keeps it vs another in-game label that OWNS the composition vs the
/// writer-artist vs public domain), not HOW MUCH leaves LabelNet.
///
/// The goldmine (3b): when the composition is controlled by an IN-GAME entity other than the recording
/// label -- e.g. a late-decade act covers an early-decade hit -- the slice is TRANSFERRED to that owner
/// (another label's publishing receipts, or the writer-artist's royalties), not leaked. Only truly
/// external rights holders (pre-game Tin Pan Alley / Brill publishers, uncaptured office songs) leak out
/// of the game. So owning a hit's publishing becomes an appreciating income stream.
///
/// Sub-phase gating: <see cref="RoutingEnabled"/> is OFF by default. When off, the settlement still
/// populates the counterparty telemetry but the money follows the legacy per-artist binary exactly, so
/// the economy is byte-identical. No RNG here; a pure function of record fields.
/// </summary>
public static class PublishingRoutingService {
	public static bool RoutingEnabled = false;

	public readonly struct Decision {
		public readonly PublishingCounterparty Counterparty;
		public readonly string ControllerLabelId;   // an in-game label that owns the composition (transfer target)
		public readonly string ControllerArtistId;  // an artist that owns the composition (writer royalty target)
		// Fractions of the pool; sum to 1. Keep stays inside recordRevenue (informational). Writer accrues
		// to the controlling artist off net. TransferLabel moves off net to another in-game label. Leak
		// leaves the game (external publisher / vanished owner).
		public readonly float LabelKeepFraction, WriterArtistFraction, TransferLabelFraction, ExternalLeakFraction;
		public Decision(PublishingCounterparty counterparty, string controllerLabelId, string controllerArtistId,
			float keep, float writer, float transfer, float leak) {
			Counterparty = counterparty;
			ControllerLabelId = controllerLabelId;
			ControllerArtistId = controllerArtistId;
			LabelKeepFraction = keep;
			WriterArtistFraction = writer;
			TransferLabelFraction = transfer;
			ExternalLeakFraction = leak;
		}
	}

	public static Decision Decide(Record record, SimulatedArtist artist, string settlingLabelId) {
		PublishingControlType control = record?.publishingControl ?? PublishingControlType.Unknown;
		string ctrlLabel = record?.publishingControllerLabelId;
		string ctrlArtist = record?.publishingControllerArtistId;

		switch (control) {
			case PublishingControlType.ArtistControlled:
				// Pays the WRITER (the controlling artist), which on a cover is the original artist, not
				// the performer. Settlement falls back to the performer only when no controller is set.
				return new Decision(PublishingCounterparty.ArtistControlled, null, ctrlArtist, 0f, 1f, 0f, 0f);

			case PublishingControlType.PublicDomain:
				// No owner; the label keeps the slice (nothing leaves).
				return new Decision(PublishingCounterparty.LabelKeeps, ctrlLabel, ctrlArtist, 1f, 0f, 0f, 0f);

			case PublishingControlType.ExternalPublisher:
				// A pre-game / office publisher that is not an in-game label: the slice leaves the game.
				return new Decision(PublishingCounterparty.ExternalPublisher, ctrlLabel, ctrlArtist, 0f, 0f, 0f, 1f);

			case PublishingControlType.LabelAffiliate:
			case PublishingControlType.LabelBuyout:
				bool thisLabel = string.IsNullOrEmpty(ctrlLabel) || ctrlLabel == settlingLabelId;
				return thisLabel
					? new Decision(PublishingCounterparty.LabelKeeps, ctrlLabel, ctrlArtist, 1f, 0f, 0f, 0f)
					// Another in-game label owns the composition -> transfer the slice to it (the goldmine).
					: new Decision(PublishingCounterparty.OtherLabelAffiliate, ctrlLabel, ctrlArtist, 0f, 0f, 1f, 0f);

			case PublishingControlType.SharedControl:
				return new Decision(PublishingCounterparty.Shared, ctrlLabel, ctrlArtist, 0.5f, 0f, 0f, 0.5f);

			default:
				// Unknown control (a record that predates / bypassed the composition layer): reproduce the
				// legacy binary -- artist keeps publishing only when the label does not own it. No controller
				// id, so the writer slice falls back to the performing artist in settlement.
				bool artistOwns = artist != null && !artist.labelOwnsPublishing;
				return artistOwns
					? new Decision(PublishingCounterparty.ArtistControlled, null, null, 0f, 1f, 0f, 0f)
					: new Decision(PublishingCounterparty.LabelKeeps, null, null, 1f, 0f, 0f, 0f);
		}
	}
}
