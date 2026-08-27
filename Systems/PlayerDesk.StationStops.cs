using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §4 -- THE STATION IS A STOP ON THE DAY SHEET.
//
// StopKind.Station stops are projected read-only from the real reporter stations ChartManager
// already runs (see PlayerDesk.EnsureStops) -- never invented, never a parallel random draw. The
// verbs here are the promo half of the trunk's Pitch/Consign/Service: they draw only from
// PressStock.PromoRemaining (never Remaining -- the two pools never convert into each other) and
// write only into RecordServicing, rapport, and the Rolodex -- never into OnHand or OpenBalance,
// which a Station stop doesn't have.
// ============================================================================================
public partial class PlayerDesk : Node {

	public const int DropOffMinutes = 60;
	public const int WaitForHimMinutes = 180;
	public const int LeaveWithReceptionistMinutes = 15;
	public const int AskSurveyMinutes = 30;
	public const int DropOffMaxCopies = 2;

	private const float DropOffConviction = 0.75f;
	private const float WaitForHimConviction = 0.90f;
	private const float ReceptionistConviction = 0.35f;
	private const float DropOffRapportGain = 0.03f;
	private const float WaitForHimRapportGain = 0.07f;
	// Directive §4: "the pitch scene opens in person... and a bonus to the roll." Standing in the
	// lobby is worth roughly what a well-argued counter is worth (see PlayerDesk.RolodexVerbs.CounterWeight).
	private const float WaitForHimInPersonBonus = 0.18f;

	/// <summary>Promo copies sitting in the office, ready to carry to a station. (recordId, title,
	/// promo on hand) -- the servicing-verb equivalent of PressedSinglesOnHand's sellable pool.</summary>
	public IEnumerable<(string RecordId, string Title, int PromoOnHand)> PromoSinglesOnHand() {
		foreach (var kv in inventory)
			if (kv.Value.PromoRemaining > 0) yield return (kv.Key, TitleForRecord(kv.Key), kv.Value.PromoRemaining);
	}

	/// <summary>Shared gate for every station-stop verb: a real Station stop, in the city you're
	/// standing in, a real single with promo stock on hand to give him.</summary>
	private bool ValidateStationAction(string stopId, string recordId, out PlayerStop stop,
			out RadioStation station, out PressStock stockOnHand, out string message) {
		stop = null; station = null; stockOnHand = null; message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Station) { message = "No such station."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to walk into that station."; return false; }
		station = ChartManager.Instance?.GetRadioStation(stop.StationId);
		if (station == null) { message = $"{stop.DisplayName} isn't on the air any more."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to service."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to service right now -- the master's out."; return false; }
		stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.PromoRemaining <= 0) { message = "No promo copies on hand -- press some, or strike a repress all-promo."; return false; }
		return true;
	}

	/// <summary>The Rolodex card for this station's lead jock, creating one (a discovery) when
	/// requested and none exists yet. Leave with the Receptionist passes discover:false -- directive
	/// §4: "No rapport, no discovery." <paramref name="discoveryLogLine"/> lets a non-visit discovery
	/// (directive §5's unprompted mail callback) log something truthful instead of the default
	/// in-person line.</summary>
	private RolodexEntry EnsureStationEntry(RadioStation station, bool discover, string discoveryLogLine = null) {
		RolodexEntry entry = rolodex.FirstOrDefault(e => e.stationId == station.stationId);
		if (entry != null || !discover) return entry;
		Deejay dj = ChartManager.Instance?.GetDeejay(station.leadDjId);
		if (dj == null) return null;
		entry = new RolodexEntry {
			djId = dj.djId, stationId = station.stationId,
			state = DiscoveryState.Introduced,
			displayName = SynthesizeDJName(dj, station), portraitKey = dj.archetype.ToString(),
			shiftKnown = false,
		};
		entry.log.Add($"{Today()} — {discoveryLogLine ?? $"Met him in person at {station.callsign}."}");
		rolodex.Add(entry);
		return entry;
	}

	/// <summary>Directive §4: "Drop off a copy" -- 1h, 1-2 promo copies, conviction ~0.75. A small
	/// rapport tick, and a discovery if this jock isn't in the book yet.</summary>
	public bool DropOffAtStation(string stopId, string recordId, out string message) {
		if (!ValidateStationAction(stopId, recordId, out PlayerStop stop, out RadioStation station, out PressStock stockOnHand, out message)) return false;
		if (!Require(1, out message)) return false;

		Spend(1);
		int copies = Mathf.Min(DropOffMaxCopies, stockOnHand.PromoRemaining);
		stockOnHand.PromoRemaining -= copies;
		ServiceStation(recordId, station.stationId, DropOffConviction, ServicingSource.HandDelivered);
		stop.LastVisitWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? stop.LastVisitWeek;

		RolodexEntry entry = EnsureStationEntry(station, discover: true);
		string title = TitleForRecord(recordId);
		if (entry != null) {
			float after = ApplyRapport(entry, DropOffRapportGain);
			entry.MaybePromoteState(after);
			entry.log.Insert(0, $"{Today()} — Dropped off \"{title}\" in person, {copies} cop{(copies == 1 ? "y" : "ies")}.");
		}
		Note($"Dropped {copies} promo cop{(copies == 1 ? "y" : "ies")} of \"{title}\" at {station.callsign}.");
		message = $"Left {copies} with {station.callsign}. He's got a copy now.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Directive §4: "Wait for him" -- 3h, 1-2 promo copies, conviction ~0.90, and the pitch
	/// scene opens in person: the RolodexCall beats run with the connect roll skipped and a bonus to
	/// the roll, because you're standing in the lobby, not hoping the switchboard puts you through.</summary>
	public RolodexCall WaitForHimAtStation(string stopId, string recordId, out string message) {
		if (!ValidateStationAction(stopId, recordId, out PlayerStop stop, out RadioStation station, out PressStock stockOnHand, out message)) return null;
		if (!Require(3, out message)) return null;

		RolodexEntry entry = EnsureStationEntry(station, discover: true);
		if (entry == null) { message = "There's nobody here to wait for -- the station has no lead jock on record."; return null; }

		Spend(3);
		int copies = Mathf.Min(DropOffMaxCopies, stockOnHand.PromoRemaining);
		stockOnHand.PromoRemaining -= copies;
		ServiceStation(recordId, station.stationId, WaitForHimConviction, ServicingSource.HandDelivered);
		stop.LastVisitWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? stop.LastVisitWeek;
		// You showed up -- the promise is kept, whether or not it was ever made.
		if (entry.appointmentRecordId == recordId) { entry.appointmentRecordId = ""; entry.appointmentExpiresWeek = 0; }

		float after = ApplyRapport(entry, WaitForHimRapportGain);
		entry.MaybePromoteState(after);
		string title = TitleForRecord(recordId);
		entry.log.Insert(0, $"{Today()} — Waited him out at {station.callsign} and put \"{title}\" in his hands.");
		Note($"Waited for {entry.displayName} at {station.callsign} -- \"{title}\" serviced in person.");
		// Standing in front of him IS your conversation with him today -- no phoning him again after this.
		djReachedToday.Add(entry.djId);

		RolodexCallContext ctx = BuildCallContext(entry, recordId);
		var call = new RolodexCall { entry = entry, ctx = ctx, recordId = recordId, stage = CallStage.Open, inPersonBonus = WaitForHimInPersonBonus };
		call.Say(RolodexSceneBeat.Opening, $"He takes the record out of your hand and looks at the label before he looks at you. \"All right,\" he says. \"You drove out here, so I'm listening. What is it?\"");
		ActiveCall = call;
		message = $"He's listening, in person, at {station.callsign}.";
		Changed?.Invoke();
		return call;
	}

	/// <summary>Directive §4: "Leave it with the receptionist" -- 15 min, 1 promo copy, conviction
	/// ~0.35. No rapport, no discovery -- what you do when you're behind schedule.</summary>
	public bool LeaveWithReceptionist(string stopId, string recordId, out string message) {
		if (!ValidateStationAction(stopId, recordId, out PlayerStop stop, out RadioStation station, out PressStock stockOnHand, out message)) return false;
		if (TimeManager.Instance?.CanAffordMinutes(LeaveWithReceptionistMinutes) != true) {
			message = "Not enough of the day left for even that."; return false;
		}

		SpendMinutes(LeaveWithReceptionistMinutes);
		stockOnHand.PromoRemaining -= 1;
		ServiceStation(recordId, station.stationId, ReceptionistConviction, ServicingSource.HandDelivered);

		string title = TitleForRecord(recordId);
		Note($"Left a copy of \"{title}\" with the front desk at {station.callsign} -- nobody you could put a name to.");
		message = $"Left with the desk at {station.callsign}. It's in the building, for what that's worth.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Directive §4: "Ask what's on the survey" -- 30 min, free. Two reads, and the second is the
	/// one that matters: this station's current spin tiers for the player's own records, AND "which local
	/// dealers report to it" (§7.1). That second half is the setup for everything in §7 -- the honest
	/// report verb and the hype both turn on knowing WHICH counter the survey is actually compiled from.
	/// Free information, and the cheapest way to find it.</summary>
	public bool AskWhatsOnSurvey(string stopId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Station) { message = "No such station."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to ask."; return false; }
		var chart = ChartManager.Instance;
		RadioStation station = chart?.GetRadioStation(stop.StationId);
		if (station == null) { message = $"{stop.DisplayName} isn't on the air any more."; return false; }
		if (TimeManager.Instance?.CanAffordMinutes(AskSurveyMinutes) != true) { message = "Not enough of the day left."; return false; }

		SpendMinutes(AskSurveyMinutes);
		var lines = new List<string>();
		foreach (RecordRuntimeData rec in ReleasedRecords) {
			if (rec?.baseRecord == null) continue;
			SpinTier tier = chart.SpinTierOf(station.stationId, rec.baseRecord.recordId);
			if (tier == SpinTier.None) continue;
			lines.Add($"\"{rec.baseRecord.title}\" — {TierWord(tier)} rotation");
		}

		// Directive §7.1: the other half of the verb -- which dealers this station compiles its survey
		// from. Newly-learned names are called out by name; ones the player already worked out for
		// himself across the counter aren't repeated back at him.
		var newlyLearned = new List<string>();
		foreach (PlayerStop dealer in EnsureStops().Values) {
			if (dealer.Kind != StopKind.Shop || !dealer.ReportsToStationIds.Contains(stop.StationId)) continue;
			if (!KnowsWhoReports(dealer.StopId)) newlyLearned.Add(dealer.DisplayName);
			LearnWhoReports(dealer);
		}

		string sheet = lines.Count > 0
			? $"{station.callsign}'s sheet: {string.Join("; ", lines)}."
			: $"Nothing of yours is on {station.callsign}'s sheet right now.";
		string reporters = newlyLearned.Count > 0
			? $" He mentions the numbers come off {NaturalList(newlyLearned)} — that's whose counter the survey is."
			: "";
		message = sheet + reporters;
		Note($"Asked what's on the survey at {station.callsign}."
			+ (newlyLearned.Count > 0 ? $" Learned {station.callsign} compiles it off {NaturalList(newlyLearned)}." : ""));
		Changed?.Invoke();
		return true;
	}

	/// <summary>"Kaplan's", "Kaplan's and Vogel's", "Kaplan's, Vogel's and Byrne's" -- a spoken list, since
	/// this is a jock talking, not a readout.</summary>
	private static string NaturalList(IReadOnlyList<string> names) =>
		names.Count switch {
			0 => "",
			1 => names[0],
			_ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1]
		};
}
