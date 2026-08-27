using System.Linq;
using Godot;

// ============================================================================================
// PROMO MECHANIC DIRECTIVE §8 -- THE RECORD HOP.
//
// The legal answer to payola (Freed fired November 1959, the House hearings February 1960, the
// federal anti-payola amendment signed that September -- directive §1): a DJ MCs a teen dance, the
// act appears for gas money or nothing, the jock keeps the gate as his payment, and the player sells
// 45s at the door. Built as a verb at the Venue stop (directive §8's own stated alternative to a new
// RolodexApproach) so it reuses WorkTheHopTable's wholesale accounting and the existing
// Trusted-or-warm-rapport gate rather than growing the call scene a fourth time this branch.
// Unlike payola, this cannot be busted -- there is no cash changing hands to detect, only a room that
// did or didn't react to a record a real jock chose to stand behind.
// ============================================================================================
public partial class PlayerDesk : Node {

	// "Costs the act a night and the player a day and the drive" -- the drive is already spent
	// getting to the venue's city; this is the night itself.
	public const int RecordHopHours = 6;
	public const float RecordHopFeeMax = 25f;
	private const float RecordHopFeeChanceOfNothing = 0.4f; // "$0-25 -- or nothing"
	// Directive §4's rapport bar for DiscoveryState.Trusted (RolodexEntry.MaybePromoteState) --
	// "Trusted OR rapport over a bar" collapses to the same number, since Trusted is reached at
	// exactly this rapport in the first place.
	private const float RecordHopRelationshipBar = 0.6f;
	private const float RecordHopRapportGainGood = 0.10f;
	private const float RecordHopRapportLossBad = 0.06f;
	// "The biggest legal advocacy in the game" -- bigger than a won phone pitch (PersonalPitchAdvocacyBoost)
	// or a dealer's word (ReportingAdvocacyBoost), and it lasts longer.
	private const float RecordHopAdvocacyBoost = 0.30f;
	private const int RecordHopAdvocacyWeeks = 6;
	// "Bounded, modest" regional awareness -- an order of magnitude under a real ad buy.
	private const float RecordHopAwarenessBase = 0.02f;
	// A hop moves several times what a bare table does (directive §8) -- reuses SuggestedPlacement's
	// own Venue sizing rather than inventing a second quantity model.
	private const float RecordHopWholesaleMultiplier = 3.5f;

	/// <summary>Directive §8: "Unlock: a Rolodex jock at DiscoveryState.Trusted or with rapport over a
	/// bar, in a city with a Venue stop." Finds that jock by matching his station's city to the venue's,
	/// not by name -- the player picks the table, the game finds who in this town would MC it.</summary>
	private RolodexEntry TrustedHopMc(string venueCityId) =>
		rolodex.FirstOrDefault(e => {
			RadioStation station = ChartManager.Instance?.GetRadioStation(e.stationId);
			MarketCity city = station != null ? DistanceModel.GetCityByName(station.cityName) : null;
			if (city == null || city.cityId != venueCityId) return false;
			float rapport = station.rt?.Rapport(Label?.labelId ?? "") ?? 0f;
			return e.state == DiscoveryState.Trusted || rapport >= RecordHopRelationshipBar;
		});

	/// <summary>Directive §8: MC'd hop at a Venue stop. He MCs, the act appears, you sell at the table,
	/// and -- the payoff -- he's watched a room react: a large rapport swing, servicing at conviction
	/// 1.0, and a strong advocacy write, gated on the act's real numbers (directive: "cohesion, live-set
	/// quality, the record's hook") rather than guaranteed, so booking a weak act is a real mistake.</summary>
	public bool BookRecordHop(string stopId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Venue) { message = "No such table."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town for that."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to work the hop."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to work right now -- the master's out."; return false; }
		RecordRuntimeData rec = FindReleasedRecord(recordId);
		if (rec?.baseRecord == null) { message = "Can't book a hop for that one."; return false; }
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.baseRecord.artistId);
		if (artist == null) { message = "No act on record for that single -- nobody to appear."; return false; }
		RolodexEntry entry = TrustedHopMc(stop.CityId);
		if (entry == null) { message = "No jock in this town trusts you enough to MC a hop yet."; return false; }
		PressStock stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		if (!Require(RecordHopHours, out message)) return false;

		float fee = GD.Randf() < RecordHopFeeChanceOfNothing ? 0f : Mathf.Round((float)GD.RandRange(10.0, RecordHopFeeMax));
		if (Label.cashReserves < fee) { message = $"You're ${fee - Label.cashReserves:N0} short of the ${fee:N0} it'd take to book the room."; return false; }

		Spend(RecordHopHours);
		if (fee > 0f) { Label.cashReserves -= fee; Label.monthlyExpenses += fee; }

		int qty = Mathf.Min(Mathf.RoundToInt(SuggestedPlacement(stop) * RecordHopWholesaleMultiplier), stockOnHand.Remaining);
		stockOnHand.Remaining -= qty;
		BookTrunkSale(rec, qty, stop, cashNow: true);
		TouchStop(stop, 0.06f);
		workedCities.Add(stop.CityId);

		float year = TimeManager.Instance?.CurrentDate.year ?? 1960f;
		float eraWeight = PlayerStopFactory.HopEraWeight(year);
		// Directive §8: "Gate the outcome on real act numbers -- cohesion, live-set quality, the
		// record's hook." A weak act in front of a room is a rapport LOSS, not just a smaller gain.
		float quality = (artist.groupCohesion + artist.livePerformance) * 0.5f * 0.6f + rec.baseRecord.hookStrength * 0.4f;
		bool wentWell = GD.Randf() < Mathf.Clamp(quality, 0.05f, 0.95f);
		string title = rec.baseRecord.title;
		string regionId = DistanceModel.GetCityById(stop.CityId)?.parentRegionId;

		if (wentWell) {
			float after = ApplyRapport(entry, RecordHopRapportGainGood);
			entry.MaybePromoteState(after);
			ServiceStation(recordId, entry.stationId, 1.0f, ServicingSource.Hop);
			int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			ChartManager.Instance?.Advocacy.Grant(recordId, entry.stationId, Label.labelId, entry.djId,
				RecordHopAdvocacyBoost * Mathf.Max(0.3f, eraWeight), week, RecordHopAdvocacyWeeks, AdvocacyMethod.RecordHop);
			if (!string.IsNullOrEmpty(regionId))
				ChartManager.Instance?.AddAwareness(recordId, regionId, RecordHopAwarenessBase * Mathf.Max(0.3f, eraWeight));
			entry.log.Insert(0, $"{Today()} — MC'd the hop at {stop.DisplayName}, and the room came alive for \"{title}\".");
			Note($"{entry.displayName} MC'd a hop at {stop.DisplayName} for \"{title}\" -- sold {qty:N0} at the table, and he watched the room react.");
			message = $"The hop went great -- {entry.displayName} watched the room love \"{title}\", and sold {qty:N0} at the table.";
		} else {
			ApplyRapport(entry, -RecordHopRapportLossBad, floorAtZero: true);
			// He still physically heard it, in the worst possible way -- a weak servicing row, not none.
			ServiceStation(recordId, entry.stationId, 0.5f, ServicingSource.Hop);
			entry.log.Insert(0, $"{Today()} — The hop at {stop.DisplayName} fell flat behind \"{title}\".");
			Note($"The hop at {stop.DisplayName} for \"{title}\" fell flat -- {entry.displayName} isn't impressed. Sold {qty:N0} at the table anyway.");
			message = $"Rough night -- \"{title}\" didn't land at {stop.DisplayName}, and {entry.displayName} noticed.";
		}
		Changed?.Invoke();
		return true;
	}
}
