using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 1 (SimTools/CelebrityFanbaseDesign.md §5, <see cref="MediaMemberFame"/>): the camera's and the
/// photographers' faces become known by name. Television deposits as the week's airings are tallied, photographs when a
/// session is given. Camera promises from the band room bias who the lens finds.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Year ledger for MEDIA_MEMBER_FAME, print-only and cleared each year end.</summary>
	private static readonly Dictionary<string, double> memberFameLedger = new(StringComparer.Ordinal);
	private static void MemberFameLedger(string key, double amount) => memberFameLedger[key] = memberFameLedger.GetValueOrDefault(key) + amount;
	/// <summary>Others' attention excess for this week's registration, computed once at the tally (never saved).</summary>
	private static Dictionary<string, float> attentionExcess;
	private static float attentionTotalExcess;
	private static bool memberFameEventsHooked;

	public static IReadOnlyDictionary<string, MemberFameEntry> MemberFame => state.MemberFame;
	public static IReadOnlyDictionary<string, CameraPromise> CameraPromises => state.CameraPromises;

	/// <summary>Band-life spinouts and spotlight departures are counted against the media-earned fame of the people involved.</summary>
	internal static void HookMemberFameEvents() {
		if (memberFameEventsHooked) return;
		memberFameEventsHooked = true;
		BandLifeService.OnEvent += OnBandLifeEventForMemberFame;
	}

	/// <summary>
	/// Each other act above the sensation bar takes its excess from everyone else's registration (§3.1, §7.3). Read from the
	/// followings as they stood after last week; with no followings kept, attention is 1.
	/// </summary>
	private static (Dictionary<string, float> Excess, float Total) AttentionExcess() {
		var excess = new Dictionary<string, float>(StringComparer.Ordinal);
		float total = 0f;
		foreach (MediaFollowingState s in state.Following.Values) {
			float sal = s.Salience();
			if (sal <= MediaFollowing.SensationShare) continue;
			excess[s.ArtistId] = sal - MediaFollowing.SensationShare;
			total += excess[s.ArtistId];
		}
		return (excess, total);
	}

	/// <summary>
	/// What registration reads from an airing (§3.1): its young and adult shares of viewers, the act's national dates before
	/// this one, and the attention left after other acts' excess this week.
	/// </summary>
	private static (double Young, double Adult, int DatesBefore, float Attention) AiringAudience(MediaAppearance a, MediaAiring airing, MediaActTally t) {
		ExposureEstimate x = airing.Exposure;
		double groups = x?.AverageByGroup?.Sum() ?? 0;
		if (groups <= 0) return (0, 0, 0, 1f);
		double young = (x.AverageByGroup[(int)ViewerGroup.Teen] + x.AverageByGroup[(int)ViewerGroup.YoungAdult]) / groups;
		double adult = x.AverageByGroup[(int)ViewerGroup.Adult] / groups;
		bool national = MediaOutletCatalog.Get(a.ProgramId)?.Network != TvNetwork.Local;
		int datesBefore = Math.Max(0, t.NationalAppearances - (national ? 1 : 0));
		float attention = MediaFollowing.Attention(attentionTotalExcess - (attentionExcess?.GetValueOrDefault(a.ArtistId) ?? 0f));
		return (young, adult, datesBefore, attention);
	}

	/// <summary>Once a week, before the tally: the media-earned part fades on recognition's clock; lapsed promises go.</summary>
	private static void MemberFameWeekStart(GameDate weekEnd) {
		foreach (MemberFameEntry e in state.MemberFame.Values) if (e.Earned > 0f) e.Earned *= ArtistRecognitionService.PublicRecognitionDecay;
		foreach (string key in state.MemberFame.Where(p => p.Value.Earned < 1e-4f && p.Value.Year < weekEnd.year).Select(p => p.Key).ToList())
			state.MemberFame.Remove(key);
		int today = BroadcastExposure.DayIndex(weekEnd);
		foreach (string key in state.CameraPromises.Keys.ToList()) {
			CameraPromise p = state.CameraPromises[key];
			SimulatedArtist a = ArtistManager.Instance?.GetArtist(p.ArtistId);
			if (a == null || !a.isActive || !PromiseHolds(p, a, today) || a.members?.Any(m => m != null && m.personId == key && m.isActive) != true)
				state.CameraPromises.Remove(key);
		}
	}

	private static bool PromiseHolds(CameraPromise p, SimulatedArtist a, int day) =>
		p.Billing ? string.Equals(a.stageName, p.StageName, StringComparison.Ordinal) : day <= p.UntilDay;

	/// <summary>Camera shares with any standing promise for this act applied (MediaFans.CameraShares).</summary>
	private static List<(Musician Member, float Weight)> CameraSharesFor(SimulatedArtist artist, string key, int year, int day) {
		if (artist == null || state.CameraPromises.Count == 0) return MediaFans.CameraShares(artist, key, year);
		return MediaFans.CameraShares(artist, key, year, m => state.CameraPromises.TryGetValue(m.personId, out CameraPromise p)
			&& p.ArtistId == artist.artistId && PromiseHolds(p, artist, day));
	}

	/// <summary>
	/// The office's side of the spotlight (§5, audit finding 10): the label promises a member the camera instead of minting
	/// fame. Returns false when the causal member-fame writer is off, so the caller keeps its old behavior.
	/// </summary>
	public static bool PromiseCamera(SimulatedArtist artist, Musician member, bool billing, GameDate today) {
		if (!MediaMemberFame.Causal || artist == null || member == null || string.IsNullOrEmpty(member.personId)) return false;
		int day = BroadcastExposure.DayIndex(today);
		state.CameraPromises[member.personId] = new CameraPromise {
			ArtistId = artist.artistId, StageName = artist.stageName, Billing = billing, FromDay = day,
			UntilDay = billing ? -1 : day + MediaMemberFame.SoloPromiseDays,
		};
		MemberFameLedger(billing ? "promisesBilling" : "promisesSolo", 1);
		return true;
	}

	/// <summary>One airing of a group: its registered viewers, split by sharpened camera share, become known by name (§5).</summary>
	private static void MemberFameAiring(MediaAppearance a, MediaAiring airing, MediaActTally t, double fresh, double population) {
		ExposureEstimate x = airing.Exposure;
		if (x == null || x.UniqueReach <= 0 || fresh <= 0 || population <= 0) return;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(a.ArtistId);
		if (artist?.members == null || artist.members.Count(m => m != null && m.isActive) < 2) return;
		int day = BroadcastExposure.DayIndex(airing.Date);
		if (MediaMemberFame.Causal && !InCausalWindow(day)) return;
		var (young, adult, datesBefore, attention) = AiringAudience(a, airing, t);
		if (young + adult <= 0) return;
		double registered = MediaMemberFame.RegisteredViewers(fresh, young, adult, artist.primaryGenre, a.Performance, datesBefore, attention);
		float raw = (float)(registered / population);
		if (raw <= 0f) return;
		MemberFameLedger("tvDeposits", 1);
		MemberFameLedger("tvRaw", raw);
		foreach (var (member, share) in MediaMemberFame.FameShares(CameraSharesFor(artist, "tv|" + (a.RepeatOfId ?? a.Id), airing.Date.year, day)))
			MemberFameApply(artist, member, raw * share, tv: true, airing.Date.year);
	}

	/// <summary>A photo session in a magazine's pages: its registered readers learn the photographed member's face (§5).</summary>
	private static void MemberFamePhoto(SimulatedArtist artist, Musician member, PressOutlet outlet, Genre genre, bool cover, GameDate today) {
		if (!MediaMemberFame.Active || artist == null || member == null || outlet == null) return;
		if (MediaMemberFame.Causal && !InCausalWindow(BroadcastExposure.DayIndex(today))) return;
		double population = (ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>()).Sum(r => r.population) * 1e6;
		if (population <= 0) return;
		float raw = (float)(MediaMemberFame.RegisteredReaders(outlet.ReadersMillions * 1e6, outlet.Fit(genre), cover) / population);
		if (raw <= 0f) return;
		MemberFameLedger(cover ? "covers" : "photoFeatures", 1);
		MemberFameLedger("pressRaw", raw);
		MemberFameApply(artist, member, raw, tv: false, today.year);
	}

	/// <summary>
	/// Shadow: read against the member's real recognition plus the shadow already built, written only to the ledger. Causal:
	/// written to personal recognition with diminishing returns, and the realized gain booked as the media-earned part.
	/// </summary>
	private static void MemberFameApply(SimulatedArtist artist, Musician member, float raw, bool tv, int year) {
		if (raw <= 0f || string.IsNullOrEmpty(member?.personId)) return;
		if (!state.MemberFame.TryGetValue(member.personId, out MemberFameEntry e)) state.MemberFame[member.personId] = e = new MemberFameEntry();
		e.ArtistId = artist.artistId; e.Name = member.FullName;
		if (e.Year != year) { e.Year = year; e.TvYear = 0f; e.PressYear = 0f; }
		float applied;
		if (MediaMemberFame.Causal) {
			float before = member.personalRecognition;
			member.personalRecognition = MediaMemberFame.Gain(before, raw);
			applied = member.personalRecognition - before;
			e.Earned = Math.Clamp(e.Earned + applied, 0f, member.personalRecognition);
		} else {
			applied = raw * (1f - Math.Clamp(member.personalRecognition + e.Earned, 0f, 1f));
			e.Earned = Math.Clamp(e.Earned + applied, 0f, 1f);
		}
		if (tv) e.TvYear += applied; else e.PressYear += applied;
		MemberFameLedger(tv ? "tvApplied" : "pressApplied", applied);
	}

	/// <summary>A member's personal recognition without and with what television and photographs earned.</summary>
	private static (float NoMedia, float WithMedia) MemberRecognitionWithAndWithoutMedia(Musician m) {
		float earned = m?.personId != null && state.MemberFame.TryGetValue(m.personId, out MemberFameEntry e) ? e.Earned : 0f;
		float real = m?.personalRecognition ?? 0f;
		return MediaMemberFame.Causal ? (Math.Max(0f, real - earned), real) : (real, Math.Clamp(real + earned, 0f, 1f));
	}

	private static void OnBandLifeEventForMemberFame(BandLifeEvent evt) {
		if (!MediaMemberFame.Active || evt == null || !evt.applied || string.IsNullOrEmpty(evt.personId)) return;
		bool spinout = evt.eventType == "solo-spinout";
		if (!spinout && evt.cause != StrainCause.Spotlight) return;
		float earned = state.MemberFame.TryGetValue(evt.personId, out MemberFameEntry e) ? e.Earned : 0f;
		if (!spinout) {
			MemberFameLedger("spotlight:" + evt.eventType, 1);
			if (earned > 0f) MemberFameLedger("spotlightMedia:" + evt.eventType, 1);
			return;
		}
		MemberFameLedger("spinouts", 1);
		if (earned <= 0f) return;
		MemberFameLedger("spinoutsMedia", 1);
		Musician person = ArtistManager.Instance?.GetArtist(evt.artistId)?.members?.FirstOrDefault(m => m?.personId == evt.personId);
		var (noMedia, withMedia) = MemberRecognitionWithAndWithoutMedia(person);
		if (noMedia < BandLifeService.SoloLaunchBar && withMedia >= BandLifeService.SoloLaunchBar) MemberFameLedger("spinoutsMadeByMedia", 1);
		Godot.GD.Print($"MEDIA_MEMBER_SPINOUT year={evt.year} person=\"{evt.personName}\" solo=\"{evt.stageName}\" recog[noMedia/withMedia]={noMedia:F3}/{withMedia:F3}" +
			$" earned={earned:F3} cause={evt.cause}");
	}

	/// <summary>
	/// Year end: what television and photographs deposited, the members they carried over the solo bar, and the spinouts
	/// and spotlight quarrels since the last line. Read-only.
	/// </summary>
	private static void PrintMemberFame(int year) {
		if (!MediaMemberFame.Active || ArtistManager.Instance == null) return;
		double V(string key) => memberFameLedger.GetValueOrDefault(key);
		int known = 0, overBar = 0, viableNo = 0, viableWith = 0;
		var members = new List<(MemberFameEntry Entry, Musician Member, SimulatedArtist Act)>();
		foreach (var (personId, e) in state.MemberFame) {
			SimulatedArtist act = ArtistManager.Instance.GetArtist(e.ArtistId);
			Musician m = act?.members?.FirstOrDefault(x => x != null && x.isActive && x.personId == personId);
			if (m == null || e.Earned <= 0f) continue;
			members.Add((e, m, act));
			if (e.Earned >= 0.001f) known++;
			var (noMedia, withMedia) = MemberRecognitionWithAndWithoutMedia(m);
			if (noMedia < BandLifeService.SoloLaunchBar && withMedia >= BandLifeService.SoloLaunchBar) overBar++;
		}
		foreach (SimulatedArtist act in members.Select(x => x.Act).Distinct()) {
			foreach (Musician m in act.members.Where(x => x != null && x.isActive)) {
				if (BandLifeService.IsSoloViable(m, act, year, x => MemberRecognitionWithAndWithoutMedia(x).NoMedia)) viableNo++;
				if (BandLifeService.IsSoloViable(m, act, year, x => MemberRecognitionWithAndWithoutMedia(x).WithMedia)) viableWith++;
			}
		}
		var top = members.Where(x => x.Entry.Year == year && x.Entry.TvYear + x.Entry.PressYear > 0f)
			.OrderByDescending(x => x.Entry.TvYear + x.Entry.PressYear).ThenBy(x => x.Member.personId, StringComparer.Ordinal).Take(5)
			.Select(x => $"\"{x.Entry.Name}\"/\"{x.Act.stageName}\" {x.Entry.TvYear + x.Entry.PressYear:F4} ({MemberRecognitionWithAndWithoutMedia(x.Member).WithMedia:F3})");
		string spotlight = string.Join(" ", memberFameLedger.Where(p => p.Key.StartsWith("spotlight:", StringComparison.Ordinal)).OrderBy(p => p.Key, StringComparer.Ordinal)
			.Select(p => $"{p.Key["spotlight:".Length..]}={p.Value:0}/{V("spotlightMedia:" + p.Key["spotlight:".Length..]):0}"));
		Godot.GD.Print($"MEDIA_MEMBER_FAME year={year} mode={(MediaMemberFame.Causal ? "causal" : "shadow")}" +
			$" tv[deposits/raw/applied]={V("tvDeposits"):0}/{V("tvRaw"):F3}/{V("tvApplied"):F3}" +
			$" press[features/covers/raw/applied]={V("photoFeatures"):0}/{V("covers"):0}/{V("pressRaw"):F4}/{V("pressApplied"):F4}" +
			$" members={known} earned={members.Sum(x => x.Entry.Earned):F3} overSoloBar={overBar} soloViable[noMedia/withMedia]={viableNo}/{viableWith}" +
			$" spinouts[all/media/madeByMedia]={V("spinouts"):0}/{V("spinoutsMedia"):0}/{V("spinoutsMadeByMedia"):0}" +
			$" spotlight[all/media]=[{spotlight}] promises[billing/solo]={V("promisesBilling"):0}/{V("promisesSolo"):0}" +
			$" top=[{string.Join(" ", top)}]");
		memberFameLedger.Clear();
	}
}
