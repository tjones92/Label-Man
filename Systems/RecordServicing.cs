using System;

// ============================================================================================
// PROMO SERVICING (directive §3.2) -- who has actually been sent a copy of a record.
//
// Player-only, keyed per (recordId, stationId). This is the mechanic that makes vinyl and radio
// one system (directive invariant 2): a jock with no row here for the record under discussion
// cannot be talked, bought, or bluffed onto the air -- see Objection.NotServiced and
// PlayerDesk.RolodexVerbs.PickObjection/Resolve.
// ============================================================================================

/// <summary>How the copy reached him. Drives <see cref="RecordServicing.Conviction"/> at the point
/// of servicing -- a cold mailing barely clears the objection, a hand-delivered copy or a hop is the
/// real thing.</summary>
public enum ServicingSource { Mailed, HandDelivered, Hop, Trade }

/// <summary>One serviced copy. Decays with time (see <see cref="PlayerDesk.IsServiced"/>) -- a copy
/// sent months ago is in a stack somewhere, not on the turntable.</summary>
public sealed class RecordServicing {
	public string RecordId;
	public string StationId;
	public int Week;              // chart week it landed
	public float Conviction;      // 0-1: mailed cold ~0.2, hand-delivered ~0.75, hop/in-person ~1.0
	public ServicingSource Source;
}

/// <summary>Flat save record for one <see cref="RecordServicing"/> row.</summary>
public sealed class RecordServicingSaveData {
	public string RecordId { get; set; }
	public string StationId { get; set; }
	public int Week { get; set; }
	public float Conviction { get; set; }
	public int SourceOrdinal { get; set; }

	public static RecordServicingSaveData From(RecordServicing s) => new() {
		RecordId = s.RecordId, StationId = s.StationId, Week = s.Week,
		Conviction = s.Conviction, SourceOrdinal = (int)s.Source
	};

	public RecordServicing ToServicing() => new() {
		RecordId = RecordId, StationId = StationId, Week = Week, Conviction = Conviction,
		Source = Enum.IsDefined(typeof(ServicingSource), SourceOrdinal) ? (ServicingSource)SourceOrdinal : ServicingSource.Mailed
	};
}
