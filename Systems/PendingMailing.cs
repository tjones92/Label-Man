using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §5 -- THE MAILING, DEFERRED.
//
// Bug report: "when mailing, it should NOT tell me how much 'mailed', how much 'landed', the
// second I click to mail. I should not know for weeks, if ever." The directive's own landing-chance
// roll was being resolved and reported synchronously inside MailPromoCopies, which spoils the "most
// of it lands in the bin" premise the mechanic is built on (directive §5). This holds a mailing in
// flight between the click and the resolution, the same shape as TradeSubmission (§6.1): spend the
// hours/cash/copies now, find out what happened later. See PlayerDesk.Mailing.cs for the verb and
// the weekly resolution.
// ============================================================================================

/// <summary>One mailing in flight -- copies already spent, landing not yet rolled.</summary>
public sealed class PendingMailing {
	public string RecordId;
	public string RegionId;
	public List<string> StationIds = new();
	public int MailedWeek;
	public int ResolveWeek;
}

/// <summary>Flat save record for one <see cref="PendingMailing"/> row.</summary>
public sealed class PendingMailingSaveData {
	public string RecordId { get; set; }
	public string RegionId { get; set; }
	public List<string> StationIds { get; set; } = new();
	public int MailedWeek { get; set; }
	public int ResolveWeek { get; set; }

	public static PendingMailingSaveData From(PendingMailing m) => new() {
		RecordId = m.RecordId, RegionId = m.RegionId,
		StationIds = m.StationIds?.ToList() ?? new List<string>(),
		MailedWeek = m.MailedWeek, ResolveWeek = m.ResolveWeek,
	};

	public PendingMailing ToPendingMailing() => new() {
		RecordId = RecordId, RegionId = RegionId,
		StationIds = StationIds?.ToList() ?? new List<string>(),
		MailedWeek = MailedWeek, ResolveWeek = ResolveWeek,
	};
}
