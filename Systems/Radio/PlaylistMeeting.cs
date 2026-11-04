using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Deterministic seating from captured scores. Shared by live meetings and static replay;
/// contains no random draws, demand model, or access to future outcomes.</summary>
public static class PlaylistMeeting {
	public sealed class Candidate {
		public string Id { get; set; }
		public SpinTier Prior { get; set; }
		public float Score { get; set; }
		public float PickScore { get; set; }
		public bool Readd { get; set; }
		public bool Admit { get; set; }
		public bool ActiveTrial { get; set; }
		public int TrialStart { get; set; } = -1;
		public string Rejection { get; set; }
		public int LocalRank { get; set; }
		public int ReleaseAge { get; set; }
		public bool LaunchExposed { get; set; }
		public int ExposureAge { get; set; }
		public string ExposureCause { get; set; }
		public bool ResearchOffer { get; set; }
		public bool ResearchActive { get; set; }
		public bool ResearchHistory { get; set; }
		public bool ResearchAccepted { get; set; }
		public int ResearchStart { get; set; } = -1;
		public float ResearchScore { get; set; }
	}
	public sealed class Input {
		public int Week { get; set; }
		public int Year { get; set; }
		public string StationId { get; set; }
		public string City { get; set; }
		public string RegionId { get; set; }
		public int RankWeek { get; set; }
		public int High { get; set; }
		public int Mid { get; set; }
		public int Light { get; set; }
		public int PickSlots { get; set; }
		public SpinTier PickTier { get; set; }
		public bool Extra { get; set; }
		public int TrialWeeks { get; set; }
		public float ReaddMargin { get; set; } = 1.2f;
		public int AdvocacySlots { get; set; }
		public int ResearchSlots { get; set; }
		public int ResearchWeeks { get; set; } = 3;
		public List<string> Advocacy { get; set; } = new();
		public List<Candidate> Candidates { get; set; } = new();
	}
	public sealed class Seat {
		public string Id { get; set; }
		public SpinTier Tier { get; set; }
		public string Source { get; set; }
		public int TrialStart { get; set; } = -1;
	}
	public sealed class Result {
		public List<Seat> Seats { get; set; } = new();
		public Dictionary<string, string> Rejections { get; set; } = new();
		public float ReaddCutoff { get; set; }
	}
	public sealed class Capture {
		public Input Input { get; set; }
		public Result Result { get; set; }
	}
	public static Result Resolve(Input input) {
		var result = new Result();
		var seated = new HashSet<string>(StringComparer.Ordinal);
		void Add(Candidate c, SpinTier tier, string source) {
			seated.Add(c.Id);
			result.Seats.Add(new Seat { Id = c.Id, Tier = tier, Source = source,
				TrialStart = source == "research-trial" ? (c.ResearchActive ? c.ResearchStart : input.Week)
					: source == "trial" ? (c.ActiveTrial ? c.TrialStart : input.Week) : c.TrialStart });
		}
		var eligible = input.Candidates.Where(c => c.Rejection == null).ToList();
		int high = input.High, mid = input.Mid, light = input.Light;
		int researchSlots = Math.Min(input.ResearchSlots, light);
		light -= researchSlots;
		int slots = input.PickSlots;
		if (!input.Extra) {
			if (input.PickTier == SpinTier.High) { slots = Math.Min(slots, high); high -= slots; }
			else if (input.PickTier == SpinTier.Mid) { slots = Math.Min(slots, mid); mid -= slots; }
			else { slots = Math.Min(slots, light); light -= slots; }
		}
		int advocacy = input.AdvocacySlots;
		foreach (string id in input.Advocacy) {
			if (advocacy <= 0 || light <= 0) break;
			// A player's explicit commitment retains its existing override of circulation/cooldown.
			var c = input.Candidates.Find(c => c.Id == id && c.Rejection != "format");
			if (c == null || seated.Contains(id)) continue;
			Add(c, SpinTier.Light, "advocacy"); light--; advocacy--;
		}
		var primary = eligible.Where(c => !c.Readd && c.Score > 0 && (!c.ResearchHistory || c.ResearchAccepted)).ToList();
		primary.Sort((a, b) => b.Score.CompareTo(a.Score));
		int filled = 0;
		foreach (var c in primary) {
			if (seated.Contains(c.Id)) continue;
			if (filled >= high + mid + light) break;
			Add(c, filled < high ? SpinTier.High : filled < high + mid ? SpinTier.Mid : SpinTier.Light,
				c.ResearchActive ? "research-graduated" : c.ActiveTrial ? "graduated" : "ranked");
			filled++;
		}
		foreach (var c in eligible.Where(c => c.ResearchActive && input.Week - c.ResearchStart < input.ResearchWeeks
			|| c.ResearchOffer && !c.ResearchHistory).OrderByDescending(c => c.ResearchActive).ThenByDescending(c => c.ResearchScore).ThenBy(c => c.Id, StringComparer.Ordinal)) {
			if (researchSlots <= 0) break;
			if (seated.Contains(c.Id)) continue;
			Add(c, SpinTier.Light, "research-trial"); researchSlots--;
		}
		// Continuing probation has priority over new admissions, after merit-based graduation.
		var picks = eligible.Where(c => !c.ResearchHistory && (input.TrialWeeks > 0
			? c.ActiveTrial && input.Week - c.TrialStart < input.TrialWeeks || c.Admit && c.PickScore > 0
			: c.Admit && c.PickScore > 0)).ToList();
		picks.Sort((a, b) => {
			if (input.TrialWeeks > 0 && a.ActiveTrial != b.ActiveTrial) return a.ActiveTrial ? -1 : 1;
			return b.PickScore.CompareTo(a.PickScore);
		});
		foreach (var c in picks) {
			if (slots <= 0) break;
			if (seated.Contains(c.Id)) continue;
			Add(c, input.PickTier, "trial"); slots--;
		}
		// Reserved and advocacy seats are protected; re-adds challenge only ordinary Light seats.
		// Unfilled reservations remain reserved, matching the admission capacity contract.
		var lightSeats = result.Seats.Where(s => s.Tier == SpinTier.Light && (s.Source == "ranked" || s.Source == "graduated" || s.Source == "research-graduated")).ToList();
		int free = light - lightSeats.Count;
		var scores = eligible.ToDictionary(c => c.Id, c => c.Score, StringComparer.Ordinal);
		float Cutoff() => Math.Max(.05f, lightSeats.Count == 0 ? .05f : lightSeats.Min(s => scores[s.Id]) * input.ReaddMargin);
		result.ReaddCutoff = Cutoff();
		var readds = eligible.Where(c => c.Readd && c.Score > 0 && (!c.ResearchHistory || c.ResearchAccepted)).ToList();
		readds.Sort((a, b) => b.Score.CompareTo(a.Score));
		foreach (var c in readds) {
			if (seated.Contains(c.Id) || c.Score < Cutoff()) continue;
			if (free <= 0) {
				var weakest = lightSeats.OrderBy(s => scores[s.Id]).FirstOrDefault();
				if (weakest == null) continue;
				result.Seats.Remove(weakest); seated.Remove(weakest.Id); lightSeats.Remove(weakest);
			} else free--;
			Add(c, SpinTier.Light, "readd"); lightSeats.Add(result.Seats[^1]);
		}
		foreach (var c in input.Candidates) if (!seated.Contains(c.Id))
			result.Rejections[c.Id] = c.Rejection ?? (c.Readd ? "readd-cutoff" : c.ActiveTrial ? "trial-expired-or-capacity" : "below-cutoff");
		return result;
	}
}
