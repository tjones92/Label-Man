using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 2 (SimTools/CelebrityFanbaseDesign.md §4, <see cref="MediaRecognition.RegistrationSwitch"/>):
/// the papers and a public controversy make an act's name known. Each published item deposits the readers it registered
/// into the act's recognition, once per root event at the strength of the widest outlet that ran it, so a TV night and
/// its notices are one grant (directive §12.4). A controversy deposits notoriety and splits the act's followings. Shadow
/// or causal as the recognition adapter is.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Root credits are kept as long as the label's own clippings, then forgotten.</summary>
	private const int FameRootDays = 7 * 26;

	private static double NationalPopulation() =>
		(ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>()).Sum(r => (double)r.population) * 1e6;

	private static MediaActTally EnsureTally(string artistId, string name) {
		if (!state.Tallies.TryGetValue(artistId, out MediaActTally t)) state.Tallies[artistId] = t = new MediaActTally { ArtistId = artistId, ArtistName = name };
		return t;
	}

	/// <summary>
	/// Once a week, after the press pass: the items whose issues came out this week deposit their registered readers. Trades
	/// and teen magazines register nobody's name here (teen readers join followings instead); critics' items are counted,
	/// since cultural standing isn't opened to media by §12.4.
	/// </summary>
	private static void RecognitionPressWeek(GameDate weekEnd) {
		if (!MediaRecognition.Registration) return;
		int today = BroadcastExposure.DayIndex(weekEnd);
		double population = NationalPopulation();
		if (population <= 0) return;
		foreach (MediaStory s in state.Stories) {
			if (s.Status != StoryStatus.Scheduled || s.ArtistId == null || s.IssueDay > today || s.IssueDay <= today - 7) continue;
			PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
			if (outlet == null) continue;
			if (outlet.Family == PressFamily.RockCriticism) { RecognitionLedger("criticItems", 1); continue; }
			double registered = MediaRecognition.RegisteredReaders(outlet, s.Genre);
			if (registered <= 0) continue;
			string family = outlet.Family.ToString();
			RecognitionLedger("pressItems" + family, 1);
			if (MediaRecognition.Causal && !InCausalWindow(s.IssueDay)) continue;
			double extra = CreditRoot(s.RootId ?? s.Id, registered, s.IssueDay);
			if (extra <= 0) { RecognitionLedger("pressAbsorbed", 1); continue; }
			float gain = (float)(extra / population);
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(s.ArtistId);
			if (!RecognitionApply(artist, EnsureTally(s.ArtistId, s.ArtistName), gain, s.IssueDay, out float applied)) continue;
			RecognitionLedger("pressRaw", gain);
			RecognitionLedger("pressApplied", applied);
			RecognitionLedger("pressApplied" + family, applied);
		}
		foreach (string root in state.FameRoots.Where(p => p.Value.Day < today - FameRootDays).Select(p => p.Key).ToList()) state.FameRoots.Remove(root);
	}

	/// <summary>
	/// A controversy goes public (§4.2): notoriety is familiarity, so recognition rises by severity, charged to the
	/// controversy's root so the papers retelling it add only beyond. The adult following and adult regard fall, the young
	/// following loses its marginal members, and the core hardens for a year. Booking access already falls through the
	/// breach memory (the program for three years, the network's review for one).
	/// </summary>
	internal static void ControversyFame(MediaControversy c) {
		if (!MediaRecognition.Registration || c?.ArtistId == null) return;
		double population = NationalPopulation();
		if (population <= 0) return;
		RecognitionLedger("controversies", 1);
		if (!MediaRecognition.Causal || InCausalWindow(c.StartDay)) {
			double extra = CreditRoot(c.RootId ?? c.Id, c.Severity * MediaRecognition.NotorietyGain * population, c.StartDay);
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(c.ArtistId);
			if (extra > 0 && RecognitionApply(artist, EnsureTally(c.ArtistId, c.ArtistName), (float)(extra / population), c.StartDay, out float applied))
				RecognitionLedger("controversyApplied", applied);
		}
		if (!MediaFollowing.Active || !state.Following.TryGetValue(c.ArtistId, out MediaFollowingState fs)) return;
		float adultKeep = 1f - Math.Clamp(c.Severity * MediaFollowing.ControversyAdultLoss, 0f, 1f);
		float youngKeep = 1f - Math.Clamp(c.Severity * MediaFollowing.ControversyYoungLoss, 0f, 1f);
		float before = fs.YoungTotal() + fs.AdultTotal();
		foreach (FollowingRegion fr in fs.Regions.Values) {
			foreach (int cohort in fr.Young.Keys.ToList()) fr.Young[cohort] *= youngKeep;
			fr.Adult *= adultKeep;
		}
		foreach (string region in fs.Regard.Keys.ToList()) fs.Regard[region] *= adultKeep;
		fs.HardenedUntilDay = Math.Max(fs.HardenedUntilDay, c.StartDay + MediaFollowing.HardenedDays);
		FollowingLedger("controversy", before - fs.YoungTotal() - fs.AdultTotal());
	}
}
