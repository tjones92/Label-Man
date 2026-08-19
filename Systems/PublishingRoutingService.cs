/// <summary>
/// Publishing &amp; Cover-Song Phase 3. Resolves how a record's publishing slice (the existing
/// <c>PublishingShareOfGross</c> pool) is routed at settlement, from the record's attached
/// <see cref="PublishingControlType"/> and controller id.
///
/// LOAD-BEARING DISCIPLINE (see SimTools/PublishingCoverSongDirective.md §3): this is a REALLOCATION of
/// the existing pool, never an addition to LabelNet. It only enriches WHO the counterparty is
/// (external publisher vs label affiliate vs artist vs public domain), not HOW MUCH leaves LabelNet.
///
/// Sub-phase gating: <see cref="RoutingEnabled"/> is OFF by default. When off, the settlement still
/// populates the richer counterparty telemetry (so the would-be external leakage can be measured), but
/// the money follows the legacy per-artist binary exactly, so the economy is byte-identical. Flipping
/// it on lets the control type drive the money: recording external/standard/professional material
/// (ExternalPublisher / PublicDomain control) leaks the slice to its publisher instead of the label
/// keeping it -- the historical publishing goldmine. No RNG here; a pure function of record fields.
/// </summary>
public static class PublishingRoutingService {
	// Phase 3(a) default: telemetry-only. Flip to true for 3(b) (live routing) once the decade run has
	// re-derived PublishingShareOfGross against measured tier profitability.
	public static bool RoutingEnabled = false;

	public readonly struct Decision {
		public readonly PublishingCounterparty Counterparty;
		public readonly string ControllerLabelId;
		// Fractions of the pool. Sum to 1. LabelKeep stays inside recordRevenue (informational);
		// Artist accrues off net to the artist; External leaks off net to a non-artist publisher.
		public readonly float LabelKeepFraction, ArtistFraction, ExternalFraction;
		public Decision(PublishingCounterparty counterparty, string controllerLabelId,
			float labelKeep, float artist, float external) {
			Counterparty = counterparty;
			ControllerLabelId = controllerLabelId;
			LabelKeepFraction = labelKeep;
			ArtistFraction = artist;
			ExternalFraction = external;
		}
	}

	/// <summary>
	/// Resolve the routing for one record settled under <paramref name="settlingLabelId"/>. Reads the
	/// record's publishing control; falls back to the legacy per-artist binary for Unknown control so a
	/// record that never went through the composition layer behaves exactly as before.
	/// </summary>
	public static Decision Decide(Record record, SimulatedArtist artist, string settlingLabelId) {
		PublishingControlType control = record?.publishingControl ?? PublishingControlType.Unknown;
		string controller = record?.publishingControllerLabelId;

		switch (control) {
			case PublishingControlType.ArtistControlled:
				return new Decision(PublishingCounterparty.ArtistControlled, controller, 0f, 1f, 0f);

			case PublishingControlType.PublicDomain:
				// No composition owner to pay; the label keeps the slice (nothing leaves).
				return new Decision(PublishingCounterparty.LabelKeeps, controller, 1f, 0f, 0f);

			case PublishingControlType.ExternalPublisher:
				return new Decision(PublishingCounterparty.ExternalPublisher, controller, 0f, 0f, 1f);

			case PublishingControlType.LabelAffiliate:
			case PublishingControlType.LabelBuyout:
				bool thisLabel = string.IsNullOrEmpty(controller) || controller == settlingLabelId;
				return thisLabel
					? new Decision(PublishingCounterparty.LabelKeeps, controller, 1f, 0f, 0f)
					// A different label's affiliate controls it -> the slice leaks off this label's net.
					// (Crediting the controlling label's books is a later refinement; for now it is
					// external leakage from the settling label's perspective.)
					: new Decision(PublishingCounterparty.OtherLabelAffiliate, controller, 0f, 0f, 1f);

			case PublishingControlType.SharedControl:
				return new Decision(PublishingCounterparty.Shared, controller, 0.5f, 0f, 0.5f);

			default:
				// Unknown control (a record that predates / bypassed the composition layer): reproduce the
				// legacy binary exactly -- artist kept publishing only when the label does not own it.
				bool artistOwns = artist != null && !artist.labelOwnsPublishing;
				return artistOwns
					? new Decision(PublishingCounterparty.ArtistControlled, controller, 0f, 1f, 0f)
					: new Decision(PublishingCounterparty.LabelKeeps, controller, 1f, 0f, 0f);
		}
	}
}
