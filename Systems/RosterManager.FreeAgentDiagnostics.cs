using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Print-only: how unsigned former hitmakers (a lifetime top-40 hit) fare in the AI daily talent market.
/// Reads the slates and scores the market has already computed; makes no draws and writes nothing,
/// so the economy is unchanged. Printed once a year as ROSTER_FREEAGENTS / ROSTER_FREEAGENT_POOL.
/// </summary>
public partial class RosterManager {
	private static readonly Dictionary<string, double> freeAgentMarket = new(StringComparer.Ordinal);
	private static readonly HashSet<string> freeAgentsSeenInSlate = new(StringComparer.Ordinal);

	private static bool IsFormerHitmaker(SimulatedArtist artist) => artist != null && artist.top40Hits > 0;

	private static void CountFreeAgent(string key, double amount = 1) =>
		freeAgentMarket[key] = (freeAgentMarket.TryGetValue(key, out double value) ? value : 0) + amount;

	private static void ObserveFreeAgentNomination(AILabel label, List<SimulatedArtist> experienced, int experiencedAccessible,
		AILabel.SigningEvaluation experiencedEvaluation, AILabel.SigningEvaluation freshEvaluation, SimulatedArtist selected, string lane) {
		CountFreeAgent("appointments");
		CountFreeAgent("expAccessibleSum", experiencedAccessible);
		CountFreeAgent("expSlateSum", experienced.Count);
		List<AILabel.SigningCandidateScore> hits = experiencedEvaluation.CandidateScores?.Where(s => IsFormerHitmaker(s.Artist)).ToList();
		if (hits == null || hits.Count == 0) return;
		CountFreeAgent("apptsWithHit");
		CountFreeAgent("hitsInSlate", hits.Count);
		foreach (var hit in hits) {
			freeAgentsSeenInSlate.Add(hit.Artist.artistId);
			CountFreeAgent("hitScoreSum", hit.Score);
			if (hit.Score < .3f) CountFreeAgent("hitBelowMinimum");
			else if (!label.CanAffordToSign(label.CalculateAdvanceOffer(hit.Artist))) CountFreeAgent("hitUnaffordable");
			else CountFreeAgent("hitQualified");
		}
		var top = hits.OrderByDescending(s => s.Score).First();
		if (experiencedEvaluation.BestCandidate == top.Artist || experiencedEvaluation.CandidateScores.Max(s => s.Score) <= top.Score) CountFreeAgent("hitTopOfExperienced");
		if (freshEvaluation.BestCandidateScore is float fresh) { CountFreeAgent("bestFreshScoreSum", fresh); CountFreeAgent("bestFreshScoreN"); }
		CountFreeAgent("topHitScoreSum", top.Score);
		if (selected != null && IsFormerHitmaker(selected)) CountFreeAgent("hitNominated");
		else CountFreeAgent(selected == null ? "lostToNobody" : lane == "FreshPotential" ? "lostToFresh" : "lostToOtherExperienced");
	}

	/// <summary>Where the year's unsigned active hitmakers sit: the first gate that keeps each off a slate.</summary>
	private static void PrintFreeAgentYear(int year, int week) {
		if (ArtistManager.Instance == null) return;
		var pool = new HashSet<SimulatedArtist>(ArtistManager.Instance.GetUnsignedArtists());
		var labels = (ChartManager.Instance?.GetAllLabels() ?? new List<AILabel>()).Where(IsEligibleForEnabledScouting).ToList();
		var gates = new Dictionary<string, int>(StringComparer.Ordinal);
		var reach = new Dictionary<string, int>(StringComparer.Ordinal);
		double hitQuality = 0, hitMomentum = 0, hitReputation = 0; int open = 0;
		foreach (SimulatedArtist artist in ArtistManager.Instance.GetAllArtists()) {
			if (!IsFormerHitmaker(artist) || !artist.isActive || !string.IsNullOrEmpty(artist.labelId) || artist.isPlayerOwned) continue;
			string gate = artist.prospectMarketStatus == ProspectMarketStatus.Latent ? "latent"
				: ArtistManager.IsPopulationCooldownBlockedForProbe(artist, week) ? "cooldown"
				: !pool.Contains(artist) ? "notInPool"
				: !ArtistManager.Instance.IsEligibleForPopulationSigning(artist, week) ? "ineligible"
				: !GenreSupplyService.IsAvailableForNewSupply(artist.primaryGenre, year) ? "genreClosed"
				: Instance?.IsHeldForPlayer(artist.artistId) == true ? "playerHold" : "open";
			gates[gate] = gates.TryGetValue(gate, out int g) ? g + 1 : 1;
			if (gate != "open") continue;
			open++; hitQuality += artist.CalculateBaseQuality(); hitMomentum += artist.momentum; hitReputation += artist.reputation;
			int access = !LocalScenes.Recruitment ? labels.Count : labels.Count(label => SceneRecruitmentService.CanAccess(label, artist, week));
			string bucket = access == 0 ? "0" : access <= 2 ? "1-2" : access <= 9 ? "3-9" : "10+";
			reach[bucket] = reach.TryGetValue(bucket, out int r) ? r + 1 : 1;
		}
		var experiencedPool = pool.Where(a => !IsFreshSupplyCandidate(a) && !IsFormerHitmaker(a)).ToList();
		double V(string key) => freeAgentMarket.TryGetValue(key, out double value) ? value : 0;
		double appts = Math.Max(1, V("appointments")), withHit = Math.Max(1, V("apptsWithHit")), inSlate = Math.Max(1, V("hitsInSlate"));
		GD.Print($"ROSTER_FREEAGENTS year={year} appointments={V("appointments"):0} expAccessibleMean={V("expAccessibleSum") / appts:0.0} expSlateMean={V("expSlateSum") / appts:0.0}" +
			$" apptsWithHit={V("apptsWithHit"):0} hitsInSlate={V("hitsInSlate"):0} distinctHitsSeen={freeAgentsSeenInSlate.Count}" +
			$" hitBelowMinimum={V("hitBelowMinimum"):0} hitUnaffordable={V("hitUnaffordable"):0} hitQualified={V("hitQualified"):0}" +
			$" hitTopOfExperienced={V("hitTopOfExperienced"):0} hitNominated={V("hitNominated"):0} lostToFresh={V("lostToFresh"):0}" +
			$" lostToOtherExperienced={V("lostToOtherExperienced"):0} lostToNobody={V("lostToNobody"):0}" +
			$" meanHitScore={V("hitScoreSum") / inSlate:0.000} meanTopHitScore={V("topHitScoreSum") / withHit:0.000} meanBestFresh={V("bestFreshScoreSum") / Math.Max(1, V("bestFreshScoreN")):0.000}");
		GD.Print($"ROSTER_FREEAGENT_POOL year={year} unsignedActiveHitmakers={gates.Values.Sum()} gates={string.Join(",", gates.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + ":" + kv.Value))}" +
			$" openReachByLabels={string.Join(",", reach.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + ":" + kv.Value))}" +
			$" openHitQuality={(open > 0 ? hitQuality / open : 0):0.000} openHitMomentum={(open > 0 ? hitMomentum / open : 0):0.000} openHitReputation={(open > 0 ? hitReputation / open : 0):0.000}" +
			$" experiencedPool={experiencedPool.Count} experiencedQuality={(experiencedPool.Count > 0 ? experiencedPool.Average(a => a.CalculateBaseQuality()) : 0):0.000}" +
			$" experiencedMomentum={(experiencedPool.Count > 0 ? experiencedPool.Average(a => a.momentum) : 0):0.000} experiencedReputation={(experiencedPool.Count > 0 ? experiencedPool.Average(a => a.reputation) : 0):0.000}");
		freeAgentMarket.Clear(); freeAgentsSeenInSlate.Clear();
	}
}
