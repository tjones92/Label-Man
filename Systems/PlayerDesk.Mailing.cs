using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §5 -- THE MAILING.
//
// The office-bound, cheap, weak version of §4: the only way to touch a market you can't drive to.
// Draws only from PressStock.PromoRemaining (never Remaining) and writes only into
// RecordServicing and, on the rare unprompted callback, the Rolodex -- same discipline as the
// station-stop verbs in PlayerDesk.StationStops.cs. Reads ReporterStationsInRegion read-only; no
// new random draw beyond which stations get picked and whether each roll lands.
// ============================================================================================
public partial class PlayerDesk : Node {

	// Directive §5: "ActionCosts.Planning (2h) for up to ~25 pieces, plus an hour per further 25."
	public const int MailingFreePieces = 25;
	public const int MailingPiecesPerExtraHour = 25;
	// Directive §5: "~$0.14: a record mailer plus period postage."
	public const float MailerCostPerCopy = 0.14f;

	private const float MailingLandingBaseMin = 0.20f;
	private const float MailingLandingBaseMax = 0.35f;
	private const float MailingChartedHistoryBonus = 0.08f;
	private const float MailingRolodexRelationshipBonus = 0.07f;
	// Directive §5: "conviction ~ 0.2 -- enough to clear Objection.NotServiced and nothing more."
	private const float MailingServicedConviction = 0.20f;
	// Directive §5: "a small chance the jock's card enters the Rolodex unprompted."
	private const float MailingUnpromptedCallbackChance = 0.15f;

	/// <summary>Directive §5: the chance one mailed piece lands on a desk instead of the bin. The
	/// ~20-35% base is set by label reputation; charted history on this record, an existing Rolodex
	/// relationship with the station, and a live trade review pick or breakout listing (§6.1/§6.3 --
	/// "a mailing with a story behind it is a different object from a mailing without one") each raise
	/// it further.</summary>
	private float MailingLandingChance(string recordId, string stationId) {
		float chance = Mathf.Lerp(MailingLandingBaseMin, MailingLandingBaseMax, Mathf.Clamp(Label?.reputation ?? 0f, 0f, 1f));
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (rec != null && rec.peakPosition > 0) chance += MailingChartedHistoryBonus;
		if (rolodex.Any(e => e.stationId == stationId)) chance += MailingRolodexRelationshipBonus;
		chance += TradeMailingLandingBonus(recordId);
		return Mathf.Clamp(chance, 0f, 1f);
	}

	// Bug report: "when mailing, it should NOT tell me how much 'mailed', how much 'landed', the
	// second I click to mail. I should not know for weeks, if ever." How long before a mailing's
	// fate is even findable -- see ResolvePendingMailings and PendingMailing.cs.
	private const int MailingResolveMinWeeks = 1;
	private const int MailingResolveMaxWeeks = 3;

	/// <summary>Directive §5: MailPromoCopies(recordId, regionId, count) -- home only. Up to `count`
	/// promo copies go out to up to `count` not-already-serviced reporter stations in the region.
	/// Deliberately a bad deal per copy (most of it lands in the bin) and an unbeatable deal per mile
	/// (it's the only way to touch a city you haven't driven to). The hours/cash/copies are spent now;
	/// the landing roll and any callback are deferred to <see cref="ResolvePendingMailings"/> so the
	/// player doesn't get told the outcome the instant they click the button -- a mailing was a letter
	/// dropped in a box, not a phone call.</summary>
	public bool MailPromoCopies(string recordId, string regionId, int count, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to mail."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to mail right now -- the master's out."; return false; }
		MarketRegion region = ChartManager.Instance?.GetRegionById(regionId);
		if (region == null) { message = "No such region."; return false; }
		if (count <= 0) { message = "Mail at least one copy."; return false; }

		PressStock stock = StockFor(recordId);
		int available = stock?.PromoRemaining ?? 0;
		if (available <= 0) { message = "No promo copies on hand -- press some, or strike a repress all-promo."; return false; }

		List<RadioStation> targets = (ChartManager.Instance?.ReporterStationsInRegion(regionId) ?? Array.Empty<RadioStation>())
			.Where(s => s != null && !IsServiced(recordId, s.stationId))
			.OrderBy(_ => GD.Randf())
			.Take(Mathf.Min(count, available))
			.ToList();
		if (targets.Count == 0) { message = $"Every reporter station in {region.regionName} already has a copy."; return false; }

		int hours = ActionCosts.Planning
			+ Mathf.CeilToInt(Mathf.Max(0, targets.Count - MailingFreePieces) / (float)MailingPiecesPerExtraHour);
		if (!Require(hours, out message)) return false;
		float cost = targets.Count * MailerCostPerCopy;
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N0} short of the ${cost:N0} mailing."; return false; }

		Spend(hours);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		stock.PromoRemaining -= targets.Count;

		string title = TitleForRecord(recordId);
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		pendingMailings.Add(new PendingMailing {
			RecordId = recordId, RegionId = regionId,
			StationIds = targets.Select(s => s.stationId).ToList(),
			MailedWeek = week, ResolveWeek = week + GD.RandRange(MailingResolveMinWeeks, MailingResolveMaxWeeks),
		});

		Note($"Mailed {targets.Count:N0} promo cop{(targets.Count == 1 ? "y" : "ies")} of \"{title}\" around {region.regionName} for ${cost:N0}. Now you wait.");
		message = $"{targets.Count:N0} mailed, ${cost:N0} gone. Could be weeks before you hear anything back -- if you ever do.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Rolls the landing chance for every mailing whose wait is up, weeks after the copies
	/// actually went in the mail -- see MailPromoCopies. Reports one aggregate line per mailing
	/// (how many of what you sent actually landed), same "it arrives as news" shape as
	/// ResolveTradeSubmissions and ScanForCoversOfOwnSongs, not a per-station breakdown up front.</summary>
	private void ResolvePendingMailings() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (PendingMailing mailing in pendingMailings.Where(m => week >= m.ResolveWeek).ToList()) {
			pendingMailings.Remove(mailing);
			string title = TitleForRecord(mailing.RecordId);
			MarketRegion region = ChartManager.Instance?.GetRegionById(mailing.RegionId);
			var stationsById = (ChartManager.Instance?.ReporterStationsInRegion(mailing.RegionId) ?? Array.Empty<RadioStation>())
				.Where(s => s != null).ToDictionary(s => s.stationId, StringComparer.Ordinal);

			int landed = 0;
			bool callback = false;
			foreach (string stationId in mailing.StationIds) {
				if (!stationsById.TryGetValue(stationId, out RadioStation station)) continue; // station gone (era turnover) -- silently in the bin
				if (GD.Randf() >= MailingLandingChance(mailing.RecordId, stationId)) continue; // in the bin
				landed++;
				ServiceStation(mailing.RecordId, stationId, MailingServicedConviction, ServicingSource.Mailed);
				if (!rolodex.Any(e => e.stationId == stationId) && GD.Randf() < MailingUnpromptedCallbackChance
						&& EnsureStationEntry(station, discover: true, $"He phoned about \"{title}\" -- didn't expect that.") != null)
					callback = true;
			}

			string regionName = region?.regionName ?? mailing.RegionId;
			if (callback) FlagSkipStop($"A DJ called back about \"{title}\"", EventType.IncomingCall, officeOnly: true);
			Note(callback
				? $"That mailing you sent around {regionName} for \"{title}\" -- {landed} of {mailing.StationIds.Count} landed, and one of them actually called."
				: landed > 0
					? $"Word's back on the \"{title}\" mailing to {regionName}: {landed} of {mailing.StationIds.Count} landed."
					: $"Nothing came back on the \"{title}\" mailing to {regionName}. Most of it never does.");
		}
	}
}
