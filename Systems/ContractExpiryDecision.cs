/// <summary>
/// Observe-only telemetry for GameyBehaviorAudit finding 4 follow-up: what an AI label decided when an act's contract
/// matured, and the evidence it had. Built before the decision so the counters are the ones the rule read. Reads fields
/// only; draws no random number.
/// </summary>
public sealed class ContractExpiryDecision {
	public int Week, Year, ContractStartWeek, ContractLength, ContractReleases, ContractSinglesObligation, ContractTop40Hits,
		ContractChartedRecords, ContractRegionalBreakouts, ConsecutiveFlops, WeeksSinceLastRelease, Top40Hits;
	public string LabelId, Tier, ArtistId, CareerState, Matured, Outcome;
	public float Loyalty, Momentum, UnrecoupedAdvance, DealNet, NextAdvance;
	public bool Recouped, Wanted, Renewed, LiveEvidence;

	internal static ContractExpiryDecision Before(AILabel label, SimulatedArtist artist, int year, int currentWeek) {
		bool term = artist.contractExpiresWeek > 0 ? currentWeek >= artist.contractExpiresWeek : artist.contractExpiresYear <= year;
		bool delivery = artist.contractSinglesObligation > 0 && artist.contractReleases >= artist.contractSinglesObligation;
		return new ContractExpiryDecision {
			Week = currentWeek, Year = year, LabelId = label.labelId, Tier = label.tier.ToString(), ArtistId = artist.artistId,
			CareerState = artist.careerState.ToString(), Matured = term && delivery ? "both" : term ? "term" : "delivery",
			ContractStartWeek = artist.contractStartWeek, ContractLength = artist.contractLength,
			ContractReleases = artist.contractReleases, ContractSinglesObligation = artist.contractSinglesObligation,
			ContractTop40Hits = artist.contractTop40Hits, ContractChartedRecords = artist.contractChartedRecords,
			ContractRegionalBreakouts = artist.contractRegionalBreakouts, ConsecutiveFlops = artist.consecutiveFlops,
			WeeksSinceLastRelease = artist.weeksSinceLastRelease, Top40Hits = artist.top40Hits, Loyalty = label.artistLoyalty,
			Momentum = artist.momentum, UnrecoupedAdvance = artist.unrecoupedAdvance, Recouped = artist.HasRecoupedCurrentContract(),
			LiveEvidence = RenewalLiveEvidence.HasLiveEvidence(artist), DealNet = RenewalEarnedOption.DealNet(artist),
			NextAdvance = label.CalculateAdvanceOffer(artist)
		};
	}

	internal void Emit(string outcome, bool wanted, bool renewed) {
		Outcome = outcome; Wanted = wanted; Renewed = renewed;
		RosterManager.RaiseContractExpiry(this);
	}
}
