using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The press and controversy request sources of the §12.3 singles ledger (MediaAtmosphereDirective §12.3, §7.4), and the
/// AI performers' television scale (§5.4). Each is its own opt-in switch, to be screened alone first:
/// <list type="bullet">
/// <item><c>--media-demand-press</c>: an item in a teen fan magazine or a national mainstream magazine asks for a little
/// more of the record it is about.</item>
/// <item><c>--media-demand-controversy</c>: a public controversy asks for a little less of the act's booked record.</item>
/// <item><c>--media-ai-performer-fees</c>: rival acts' network dates pay their members scale, as player dates do.</item>
/// </list>
/// Requests land in the same <see cref="MediaSaveData.DemandLift"/> the television source fills, so every cap of
/// <see cref="MediaDemand.Allocate"/> holds over all sources together. Nothing here draws a random number.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Root credits are kept as long as the label's own clippings, then forgotten.</summary>
	private const int DemandRootDays = 7 * 26;

	// ---- press (--media-demand-press) ----------------------------------------------------------------------------

	/// <summary>A tone's sign on a reader's interest. Moral alarm asks for nothing: whether alarm sells or repels a record
	/// is the controversy source's business, not the paper's tone.</summary>
	public static float PressToneFactor(StoryTone tone) => tone switch {
		StoryTone.Admiring => 1.2f,
		StoryTone.Skeptical => 0.5f,
		StoryTone.Alarmed => 0f,
		_ => 1f,
	};

	/// <summary>Is the item a press request source? Teen fan and national mainstream magazines only. Trades are business
	/// evidence (§8.1), critics count through acclaim, and an item about a TV date shares the date's root, which the
	/// broadcast already asked for (§12.4). Standards and controversy items belong to the controversy source.</summary>
	internal static bool PressRequestItem(MediaStory s, PressOutlet outlet) =>
		s != null && outlet != null && s.RecordId != null && s.AppearanceId == null &&
		(outlet.Family == PressFamily.TeenFan || outlet.Family == PressFamily.Mainstream && outlet.National) &&
		s.Angle is not (StoryAngle.TvAdvance or StoryAngle.TvNotice or StoryAngle.StandardsDispute or StoryAngle.Controversy
			or StoryAngle.Correction or StoryAngle.LabelPitch or StoryAngle.OrdersFollowUp);

	/// <summary>
	/// Once a week, after the press pass: each item whose issue came out this week asks for its record in every region
	/// (these magazines are read nationally): <see cref="MediaDemand.PressGain"/> × fit-weighted readers × the angle's read
	/// conversion relative to a feature × tone ÷ the national population. One credit per root event at the widest outlet
	/// that ran it; a later, wider retelling adds only its excess readers.
	/// </summary>
	internal static void DemandPressWeek(GameDate weekEnd) {
		if (!MediaDemand.Active || !MediaDemand.PressSource) return;
		int today = BroadcastExposure.DayIndex(weekEnd);
		var regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		double population = regions.Sum(r => (double)r.population) * 1e6;
		if (population <= 0) return;
		List<string> ranked = null;
		foreach (MediaStory s in state.Stories) {
			if (s.Status != StoryStatus.Scheduled || s.IssueDay > today || s.IssueDay <= today - 7) continue;
			PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
			if (!PressRequestItem(s, outlet)) continue;
			if (MediaDemand.Causal && !InCausalWindow(s.IssueDay)) continue;
			float tone = PressToneFactor(s.Tone);
			float angle = MediaFollowing.ReadConversion(s.Angle) / MediaFollowing.CRead;
			double readers = outlet.ReadersMillions * 1e6 * Math.Clamp(outlet.Fit(s.Genre), 0f, 1f);
			if (tone == 0f || angle <= 0f || readers <= 0) continue;
			MediaDemand.Count("pressItems", 1);
			double extra = CreditDemandRoot("press|" + (s.RootId ?? s.Id), readers, s.IssueDay);
			if (extra <= 0) { MediaDemand.Count("pressAbsorbed", 1); continue; }
			float q = (float)(MediaDemand.PressGain * angle * tone * extra / population);
			if (Math.Abs(q) < MediaDemand.RequestFloor) continue;
			string target = s.RecordId;
			if (MediaDemand.PressPermute) {
				ranked ??= PressPermuteRanking();
				target = PressSubstitute(ranked, s.RecordId, s.Id);
				if (target == null) { MediaDemand.Count("pressPermuteUnmatched", 1); continue; }
			}
			AddDemandRequest(target, regions.Select(r => r.regionId), q);
			MediaDemand.Count("pressRequests", 1);
			MediaDemand.Count("pressRequestSum", q);
		}
		PruneDemandRoots(today);
	}

	/// <summary>Live singles selling this week, by national weekly units then id (the permutation's size order).</summary>
	private static List<string> PressPermuteRanking() =>
		(ChartManager.Instance?.GetAllRecords() ?? new List<RecordRuntimeData>())
			.Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && r.unitsThisWeek > 0)
			.OrderByDescending(r => r.unitsThisWeek).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal)
			.Select(r => r.baseRecord.recordId).ToList();

	/// <summary>A keyed pick among the record's ±2 size neighbours, never the record itself; null if it isn't selling.</summary>
	private static string PressSubstitute(List<string> ranked, string recordId, string storyId) {
		int i = ranked.IndexOf(recordId);
		if (i < 0) return null;
		var pool = Enumerable.Range(Math.Max(0, i - 2), Math.Min(ranked.Count, i + 3) - Math.Max(0, i - 2))
			.Where(k => k != i).Select(k => ranked[k]).ToList();
		if (pool.Count == 0) return null;
		int pick = Math.Min(pool.Count - 1, (int)(MediaAtmosphere.Unit("press-permute|" + storyId) * pool.Count));
		return pool[pick];
	}

	private static double CreditDemandRoot(string root, double people, int day) {
		if (!state.DemandRoots.TryGetValue(root, out FameRoot r)) state.DemandRoots[root] = r = new FameRoot { Day = day };
		double extra = Math.Max(0, people - r.People);
		r.People = Math.Max(r.People, people);
		r.Day = Math.Max(r.Day, day);
		return extra;
	}

	private static void PruneDemandRoots(int today) {
		foreach (string root in state.DemandRoots.Where(p => p.Value.Day < today - DemandRootDays).Select(p => p.Key).ToList())
			state.DemandRoots.Remove(root);
	}

	private static void AddDemandRequest(string recordId, IEnumerable<string> regionIds, float q) {
		if (recordId == null || q == 0f) return;
		if (!state.DemandLift.TryGetValue(recordId, out var byRegion)) state.DemandLift[recordId] = byRegion = new Dictionary<string, float>(StringComparer.Ordinal);
		foreach (string region in regionIds) byRegion[region] = byRegion.GetValueOrDefault(region) + q;
	}

	// ---- controversy (--media-demand-controversy) ----------------------------------------------------------------

	/// <summary>
	/// A controversy goes public (§7.4): family buyers turn away from the act's booked record. One request per root, at
	/// −<see cref="MediaDemand.ControversyGain"/> × severity: national everywhere, a local incident only in the program's
	/// own region. Young listeners who grow more interested don't cancel it here (§7.4). The allocator's caps still bind;
	/// a national controversy at <see cref="MediaDemand.MajorControversySeverity"/> or more is the directive's "major
	/// controversy", with the 10% title ceiling in place of 5%. No station or retailer refusal is modelled (a supply adapter).
	/// </summary>
	internal static void DemandControversy(MediaControversy c, string homeRegionId) {
		if (!MediaDemand.Active || !MediaDemand.ControversySource || c?.RecordId == null) return;
		if (MediaDemand.Causal && !InCausalWindow(c.StartDay)) return;
		if (CreditDemandRoot("controversy|" + (c.RootId ?? c.Id), 1, c.StartDay) <= 0) return;
		var regions = ChartManager.Instance?.GetAllRegions() ?? new List<MarketRegion>();
		IEnumerable<string> where = c.National || homeRegionId == null ? regions.Select(r => r.regionId) : new[] { homeRegionId };
		float q = -MediaDemand.ControversyGain * c.Severity;
		AddDemandRequest(c.RecordId, where, q);
		MediaDemand.Count("controversies", 1);
		MediaDemand.Count("controversyRequestSum", q);
	}

	/// <summary>A national controversy at the major severity bar, active today.</summary>
	private static bool MajorControversy(string artistId) {
		if (!MediaDemand.ControversySource || artistId == null) return false;
		int today = BroadcastExposure.DayIndex(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
		return state.Controversies.Any(c => c.ArtistId == artistId && c.National && c.Severity >= MediaDemand.MajorControversySeverity
			&& c.StartDay <= today && c.ExpiresDay >= today);
	}

	// ---- AI performer fees (--media-ai-performer-fees) -----------------------------------------------------------

	private static readonly Dictionary<string, double> aiFeeLedger = new(StringComparer.Ordinal);
	private static void AiFeeLedger(string key, double amount) => aiFeeLedger[key] = aiFeeLedger.GetValueOrDefault(key) + amount;

	/// <summary>
	/// A rival act's network date has been produced (aired or cut): the production pays each active member scale, by
	/// person and year, into member wealth beside session pay, exactly as a player date (§5.4). Never label cash: the AI
	/// label's books are untouched. A resident's episode pays the residency's per-episode scale. Local shows pay nothing.
	/// Once per appearance by receipt, and only inside the causal window.
	/// </summary>
	internal static void AiPerformerFee(MediaAppearance a, TvProgram program, int day) {
		if (!MediaAtmosphere.AiPerformerFees || a == null || a.PlayerBooking || a.RepeatOfId != null) return;
		if (program == null || program.Network == TvNetwork.Local || !InCausalWindow(day)) return;
		AILabel label = a.LabelId == null ? null : CompetitorManager.Instance?.GetLabel(a.LabelId);
		if (label == null || label.isPlayerOwned) return;
		List<Musician> performers = Performers(a.ArtistId);
		if (performers.Count == 0 || !TakeReceipt("fee|" + a.Id)) return;
		float scale = NetworkScaleFee;
		if (a.Lane == BillLane.Resident) {
			MediaResidency r = state.Residencies.FirstOrDefault(x => x.ArtistId == a.ArtistId && x.ProgramId == a.ProgramId && !x.Player && x.FromDay <= day && x.ToDay >= day);
			scale = MediaResidencies.ScalePerEpisode(r?.Daily == true) * (r?.FeeMultiple ?? 1f);
			AiFeeLedger("residentEpisodes", 1);
		}
		foreach (Musician m in performers) {
			string key = m.personId + "|" + a.Date.year;
			state.PerformerFees[key] = state.PerformerFees.GetValueOrDefault(key) + scale;
		}
		AiFeeLedger("dates", 1);
		AiFeeLedger("performers", performers.Count);
		AiFeeLedger("paid", scale * performers.Count);
	}

	private static void PrintAiPerformerFees(int year) {
		if (!MediaAtmosphere.AiPerformerFees) return;
		Godot.GD.Print($"MEDIA_AI_FEES year={year} dates={aiFeeLedger.GetValueOrDefault("dates"):0} residentEpisodes={aiFeeLedger.GetValueOrDefault("residentEpisodes"):0}" +
			$" performerFees={aiFeeLedger.GetValueOrDefault("performers"):0} paid={aiFeeLedger.GetValueOrDefault("paid"):0}");
		aiFeeLedger.Clear();
	}

	// ---- the player's media year (office UI) ---------------------------------------------------------------------

	/// <summary>Pays a player media cost through the desk and books the cash by year (postage, photos, rehearsals, travel).</summary>
	private static void PayPlayerMedia(PlayerDesk desk, int hours, float cash, int promoCopies, string recordId) {
		desk.PayMedia(hours, cash, promoCopies, recordId);
		if (desk?.Label == null || cash <= 0f) return;
		int year = TimeManager.Instance?.CurrentDate.year ?? GameDate.StartDate.year;
		state.PlayerMediaSpend[year] = state.PlayerMediaSpend.GetValueOrDefault(year) + cash;
	}

	/// <summary>What media cost the label this year, and what productions paid its acts' members in scale.</summary>
	public static (float Spent, float Scale) PlayerMediaYear(PlayerDesk desk, int year) {
		float spent = state.PlayerMediaSpend.GetValueOrDefault(year);
		float scale = 0f;
		foreach (SimulatedArtist act in desk?.Label?.roster ?? new List<SimulatedArtist>())
			foreach (Musician m in act?.members ?? new List<Musician>())
				if (m != null) scale += PerformerFeesPaid(m.personId, year);
		return (spent, scale);
	}

	/// <summary>A confirmed date's bill before air: what the label pays when it is produced, and the scale the production
	/// pays the members (network dates only).</summary>
	public static (float Preparation, float Travel, float Scale, int Performers) DateBill(PlayerDesk desk, MediaPitch pitch) {
		TvProgram program = pitch == null ? null : MediaOutletCatalog.Get(pitch.ProgramId);
		if (program == null) return (0f, 0f, 0f, 0);
		int performers = Performers(pitch.ArtistId).Count;
		float scale = program.Network == TvNetwork.Local ? 0f : NetworkScaleFee * performers;
		return (PreparationCost(pitch.Preparation), TravelCost(desk?.Label?.homeCityId, program, performers), scale, performers);
	}
}
