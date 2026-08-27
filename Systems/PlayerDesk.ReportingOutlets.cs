using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §7.1-7.2 -- REPORTING OUTLETS AND THE HONEST VERB.
//
// §7.1 (who reports) is generated data: PlayerStop.ReportsToTrades/ReportsToStationIds, set once
// in PlayerStopFactory and EnsureStops -- see Systems/PlayerStopFactory.cs and PlayerDesk.cs.
// §7.2 is the one verb this file adds: ask a reporting dealer to put a genuinely-moving record on
// his report. Every gate is a real number already on the stop -- stock, actual sell-through,
// relationship -- so there is nothing here to fake. Effect is a bounded StationAdvocacy grant
// (exactly the write a won Rolodex call makes) plus, at a trade-reporting dealer, a small chance
// of a free trade-listing mention -- never a units or awareness lever (invariant 4).
// ============================================================================================
public partial class PlayerDesk : Node {

	public const int AskForReportMinutes = 30;
	private const float ReportingRelationshipFloor = 0.35f; // "the relationship is warm"
	private const float ReportingAdvocacyBoost = 0.12f;
	private const int ReportingAdvocacyWeeks = 2;
	// A dealer's word to his own station is a small thing next to a trade pick -- this is why the
	// mention it sometimes buys is the weakest tier (TwoLineMention), never better.
	private const float ReportingTradeMentionChance = 0.12f;

	/// <summary>Directive §7.2: "ask the dealer to put it on his report." All three gates are numbers
	/// already on the stop -- real stock, real sell-through, a warm relationship -- so a shop that
	/// isn't actually moving the record honestly can't be asked.</summary>
	public bool AskForTheReport(string stopId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Shop) { message = "Not a shop that keeps a report."; return false; }
		if (stop.HypeBurned) { message = $"{stop.DisplayName} doesn't do you favours any more, not after that."; return false; }
		if (!stop.ReportsToTrades && stop.ReportsToStationIds.Count == 0) { message = $"{stop.DisplayName} doesn't report to anybody."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to ask him."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to talk up."; return false; }
		if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Remaining <= 0) {
			message = $"{stop.DisplayName} isn't holding \"{TitleForRecord(recordId)}\" right now -- nothing to report.";
			return false;
		}
		if (lot.Placed <= lot.Remaining) { message = "Nothing's actually moved off his counter -- he won't lie for you."; return false; }
		if (stop.Relationship < ReportingRelationshipFloor) { message = $"{stop.DisplayName} doesn't know you well enough to do you that favour."; return false; }
		if (TimeManager.Instance?.CanAffordMinutes(AskForReportMinutes) != true) { message = "Not enough of the day left."; return false; }

		SpendMinutes(AskForReportMinutes);
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		string title = TitleForRecord(recordId);
		var chart = ChartManager.Instance;

		int stationsToldCount = 0;
		foreach (string stationId in stop.ReportsToStationIds) {
			RadioStation station = chart?.GetRadioStation(stationId);
			if (station == null) continue;
			chart.Advocacy.Grant(recordId, stationId, Label.labelId, station.leadDjId,
				ReportingAdvocacyBoost, week, ReportingAdvocacyWeeks, AdvocacyMethod.DealerReport);
			stationsToldCount++;
		}

		bool tradeMention = stop.ReportsToTrades && !HasEverSubmittedToTrade(recordId) && GD.Randf() < ReportingTradeMentionChance;
		if (tradeMention) {
			tradeSubmissions.Add(new TradeSubmission {
				RecordId = recordId, SubmittedWeek = week, ResolveWeek = week,
				Outcome = TradeOutcome.TwoLineMention, PickExpiresWeek = week + TradePickWindowWeeks,
			});
		}

		Note(stationsToldCount > 0
			? $"{stop.DisplayName} put \"{title}\" on his report — his numbers go to {stationsToldCount} station{(stationsToldCount == 1 ? "" : "s")}."
			: $"{stop.DisplayName} put \"{title}\" on his report.");
		message = tradeMention
			? $"He's reporting it — and word's gotten to the trades besides."
			: stationsToldCount > 0
				? $"{stop.DisplayName} is reporting it to his station{(stationsToldCount == 1 ? "" : "s")}."
				: $"{stop.DisplayName} is reporting it.";
		Changed?.Invoke();
		return true;
	}

	// ── §7.3: hype the count -- the dishonest verb ──────────────────────────────────────────────

	public const int HypeTheCountMinutes = 45;
	public const int HypeTheCountMinFixer = 3;
	private const float HypeDetectionBase = 0.06f;
	private const float HypeDetectionPerCopy = 0.015f;
	private const float HypeDetectionRelationshipShield = 0.55f;
	private const float HypeBurnRelationshipFloor = 0.10f;

	/// <summary>Directive §7.3: "buy your own record off the counter." Modelled honestly -- the copies
	/// leave the shop's OnHand lot as genuinely sold (his report is real; no number anywhere in the sim
	/// is falsified), but the label books NO revenue on them: full list price is gone, and the lot needs
	/// restocking out of stock already paid for at the plant. Chart-hopeless by design against a market
	/// where a slot costs on the order of half a million units (invariant 7) -- it only ever works as the
	/// local survey play it historically was. Detection scales with how many copies (a dozen is obvious),
	/// how warm the relationship is (a stranger buying his own record stands out; a regular doesn't), and
	/// the live payola-hearings heat (RadioEra.RegulatoryHeat -- worst in 1960-61, exactly as the ground
	/// truth in §1 says). Getting caught burns the stop permanently (PassedRecordIds plus a relationship
	/// floor -- HypeTheCount and AskForTheReport never run here again) and, via the same payolaBurned
	/// channel-burn field a cash scandal trips, any station this dealer already reports to that the
	/// player has a live Rolodex relationship with.</summary>
	public bool HypeTheCount(string stopId, string recordId, int count, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (InstinctProfile.TheFixer < HypeTheCountMinFixer) { message = "This isn't a move you'd even think to make."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Shop) { message = "Not a counter you can work like that."; return false; }
		if (stop.HypeBurned) { message = $"{stop.DisplayName} is watching you too closely for that, after last time."; return false; }
		if (!stop.ReportsToTrades && stop.ReportsToStationIds.Count == 0) { message = $"{stop.DisplayName} doesn't report to anybody -- there's no count to hype."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to work the counter yourself."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to hype."; return false; }
		if (count <= 0) { message = "Buy at least one copy."; return false; }
		if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Remaining < count) {
			message = $"{stop.DisplayName} isn't holding {count:N0} of \"{TitleForRecord(recordId)}\" to buy back.";
			return false;
		}
		float cost = count * SinglePrice;
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N0} short of the ${cost:N0} it'd take."; return false; }
		if (TimeManager.Instance?.CanAffordMinutes(HypeTheCountMinutes) != true) { message = "Not enough of the day left."; return false; }

		SpendMinutes(HypeTheCountMinutes);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		lot.Remaining -= count; // genuinely sold, through kids and cousins -- his report is real
		string title = TitleForRecord(recordId);

		GameDate date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		float heat = RadioEra.RegulatoryHeat(date.year, date.month);
		float countTerm = Mathf.Min(0.30f, count * HypeDetectionPerCopy);
		float shield = (1f - stop.Relationship) * HypeDetectionRelationshipShield;
		float detectionChance = Mathf.Clamp(HypeDetectionBase + countTerm + shield * heat, 0.03f, 0.85f);

		if (GD.Randf() < detectionChance) {
			stop.PassedRecordIds.Add(recordId);
			stop.Relationship = Mathf.Min(stop.Relationship, HypeBurnRelationshipFloor);
			stop.HypeBurned = true;
			int burnedStations = 0;
			foreach (string stationId in stop.ReportsToStationIds) {
				RolodexEntry entry = rolodex.FirstOrDefault(e => e.stationId == stationId);
				if (entry == null || entry.payolaBurned) continue;
				entry.payolaBurned = true;
				entry.log.Insert(0, $"{Today()} — Word got back that you'd been working {stop.DisplayName}'s counter on \"{title}\".");
				burnedStations++;
			}
			Note($"{stop.DisplayName} caught you buying back \"{title}\" off his own counter -- he's done with you"
				+ (burnedStations > 0 ? $", and word reached {burnedStations} station{(burnedStations == 1 ? "" : "s")}." : "."));
			message = $"He made you. {stop.DisplayName} is burned for good" + (burnedStations > 0 ? " -- and so is the word that reached his stations." : ".");
		} else {
			Note($"Bought {count:N0} of \"{title}\" back off {stop.DisplayName}'s own counter for ${cost:N0} -- his report is real, you just paid for it twice.");
			message = $"{count:N0} bought back, ${cost:N0} gone, no revenue booked. Nobody noticed -- yet.";
		}
		Changed?.Invoke();
		return true;
	}
}
