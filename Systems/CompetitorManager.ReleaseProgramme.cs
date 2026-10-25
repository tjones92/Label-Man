using System;
using System.Collections.Generic;
using System.Linq;

public partial class CompetitorManager {
	internal ReleaseProgrammeState ProgrammeState { get; private set; }
	private ReleaseSlateItem issuingSlateItem;
	private float? programmeCampaignBudget;
	private bool programmeAdvanceSlot;
	private string programmeAdvanceSingleId;
	private readonly Dictionary<string, ProgrammeCampaign> programmeCampaignByRecord = new(StringComparer.Ordinal);
	public event Action<ReleaseSlateItem, int> OnReleaseSlateReview;
	public event Action<ProgrammeResourceTelemetry> OnProgrammeResources;
	public event Action<ProgrammeCashShortfall> OnProgrammeCashShortfall;
	public event Action<ProgrammeChoiceAudit> OnProgrammeChoiceReview;
	public IReadOnlyList<ReleaseSlateItem> GetReleaseSlate() => ProgrammeState?.Items ?? (IReadOnlyList<ReleaseSlateItem>)Array.Empty<ReleaseSlateItem>();
	internal void SetProgrammeForChecks(ReleaseProgrammeState state) { ProgrammeState = state; programmeCampaignByRecord.Clear(); }

	internal float ProgrammeCashReserved(string labelId) => ProgrammeState?.Items
		.Where(i => i.LabelId == labelId && LabelReleaseProgramme.IsOpen(i) && i != issuingSlateItem)
		.Sum(i => i.ReservedProduction + i.ReservedMarketing + i.ReservedPressing) ?? 0f;

	private ProgrammeClock ProgrammeClockFor(SimulatedArtist artist) {
		if (!ProgrammeState.Clocks.TryGetValue(artist.artistId, out var clock)) {
			int SingleAge(ReleaseFormat format) {
				var releases = ChartManager.Instance.GetAllRecords().Where(r => r.baseRecord.artistId == artist.artistId && r.baseRecord.format == format).ToList();
				if (releases.Count > 0) return releases.Min(r => r.weeksSinceRelease);
				// Old saves have no independent clock: an LP must not invent a recent single. Start from the contract,
				// or the opening career's already elapsed interval, when that format has no surviving catalogue row.
				return Math.Max(0, artist.contractStartWeek >= 0 ? pipelineWeek - artist.contractStartWeek : artist.weeksSinceLastRelease);
			}
			clock = new ProgrammeClock { Identity = $"{artist.stageName}|{artist.homeRegion}|" +
				string.Join(";", (artist.members ?? new List<Musician>()).Select(m => $"{m.FullName}:{m.birthYear}").OrderBy(s => s, StringComparer.Ordinal)),
				SingleWeek = pipelineWeek - SingleAge(ReleaseFormat.Single), AlbumWeek = pipelineWeek - SingleAge(ReleaseFormat.Album) };
			ProgrammeState.Clocks.Add(artist.artistId, clock);
		}
		return clock;
	}
	private string ProgrammeLabelKey(AILabel label) {
		if (!ProgrammeState.LabelIdentities.TryGetValue(label.labelId, out string key))
			ProgrammeState.LabelIdentities[label.labelId] = key = LabelReleaseProgramme.LabelIdentity(label);
		return key;
	}

	private double ProgrammeRoll(AILabel label, SimulatedArtist artist, string slot, int week, uint domain) =>
		LabelReleaseProgramme.Roll(ProgrammeLabelKey(label), artist == null ? "" : ProgrammeClockFor(artist).Identity, slot, week, domain);

	private void EmitProgrammeResources(AILabel label, GameDate date, int attempts, int successes) {
		var due = label.roster.Where(a => pipelineWeek - ProgrammeClockFor(a).SingleWeek >= HitActCadence.DueWeeks &&
			HitActCadence.WeeksSinceTop40(a, pipelineWeek) <= HitActCadence.HotWindowWeeks).ToList();
		OnProgrammeResources?.Invoke(new ProgrammeResourceTelemetry { Week = pipelineWeek, Year = date.year, LabelId = label.labelId, Tier = label.tier.ToString(),
			Attempts = WeeklyReleaseRollsFired - attempts, Successes = WeeklySuccessfulReleases - successes,
			OpenItems = ProgrammeState.Items.Count(i => i.LabelId == label.labelId && LabelReleaseProgramme.IsOpen(i)),
			DueFollowUps = due.Count, UnplannedFollowUps = due.Count(a => !ProgrammeState.Items.Any(i => i.ArtistId == a.artistId && i.Format == ReleaseFormat.Single && LabelReleaseProgramme.IsOpen(i))),
			CashReserved = ProgrammeCashReserved(label.labelId), PromotionPool = ProgrammePromotionPool(label),
			PromotionCommittedPeak = ProgrammePromotionCommittedPeak(label.labelId),
			PromotionBooked = ProgrammeState.Campaigns.Where(c => c.LabelId == label.labelId && c.Week <= pipelineWeek && pipelineWeek < c.Week + LabelReleaseProgramme.CampaignWeeks).Sum(c => c.Spend),
			SingleWeeklyRate = ProgrammeWeeklyRate(label, date, ReleaseFormat.Single), AlbumWeeklyRate = ProgrammeWeeklyRate(label, date, ReleaseFormat.Album) });
	}

	internal void ObserveProgrammeRelease(SimulatedArtist artist, string recordId, ReleaseFormat format) {
		if (!LabelReleaseProgramme.Enabled || artist == null || artist.isPlayerOwned) return;
		ProgrammeState ??= new();
		ProgrammeClock clock = ProgrammeClockFor(artist);
		if (format == ReleaseFormat.Single) clock.SingleWeek = pipelineWeek; else clock.AlbumWeek = pipelineWeek;
		// An LP's advance single is released under the LP's item; the item keeps the LP master's identity.
		if (issuingSlateItem != null && issuingSlateItem.Format == format) issuingSlateItem.RecordId = recordId;
	}

	private void ReviewSlate(ReleaseSlateItem item, ReleaseSlateState state, string reason) {
		item.State = state; item.Reason = reason; item.LastReviewWeek = pipelineWeek;
		if (state == ReleaseSlateState.Cancelled && item.MaterialSearch?.State is ProgrammeMaterialState.Pending or ProgrammeMaterialState.Ready)
			MaterialReview(item, pipelineWeek, ProgrammeMaterialState.Cancelled, "slate-cancelled-no-refund");
		if (state == ReleaseSlateState.Cancelled) item.ReservedProduction = item.ReservedMarketing = item.ReservedPressing = 0f;
		if (state == ReleaseSlateState.Cancelled) ShelveExpiredMaster(item);
		if (state == ReleaseSlateState.Cancelled) AiRepertoireService.CancelPreparedPick(item.RecordId, pipelineWeek);
		OnReleaseSlateReview?.Invoke(item, pipelineWeek);
		if (state is ReleaseSlateState.Issued or ReleaseSlateState.Cancelled) item.PreparedRecord = null;
	}

	private bool PrepareSlateMaster(AILabel label, SimulatedArtist artist, GameDate date, ReleaseSlateItem item) {
		if (LabelReleaseProgramme.MaterialSearchEnabled) {
			AdvanceProgrammeMaterial(item, label, artist, date, pipelineWeek);
			if (item.MaterialSearch?.State == ProgrammeMaterialState.Pending && !LabelReleaseProgramme.ProjectJudgementEnabled) return false;
		}
		if (!ReviewProgrammeProject(label, artist, date, item)) return false;
		issuingSlateItem = item;
		try {
			float reserve = ProgrammeCommitmentReserve(label);
			if (label.cashReserves - reserve < item.ReservedProduction + item.ReservedMarketing) return false;
			var selection = ChooseEnabledGenreSupply(label, artist, date.year);
			var decisionArtist = CreateProjectDecisionArtist(artist, selection.Genre, artist.primaryGenre);
			var decision = BuildDecisionContext(label, decisionArtist, date.year, date.month);
			PrepareAnnualFormatCapacity(date.year);
			var plan = DecideRelease(label, decisionArtist, date.year, decision, singleProgrammeOnly: item.Format == ReleaseFormat.Single);
			plan.format = item.Format;
			plan.strategy = item.Format == ReleaseFormat.Single ? ReleaseStrategy.OrphanSingle : ReleaseStrategy.AlbumStandalone;
			if (item.Format == ReleaseFormat.Album && LabelReleaseProgramme.LpAssemblyEnabled) {
				item.LpAssembly = BuildLpAssembly(label, artist, decisionArtist, date.year, decision, item);
				plan.projectedAlbumStandaloneNet = item.LpAssembly.ProjectedNet;
			}
			if (item.Format == ReleaseFormat.Album) plan.projectedAlbumNet = plan.projectedAlbumStandaloneNet;
			if (!PassesReleaseProfitGate(label, artist, date.year, selection.Genre, plan)) return false;
			Record master = GenerateRecordFromArtist(label, artist, date.year, item.Format, judgedOrphan: item.Format == ReleaseFormat.Single);
			if (master == null) return false;
			ApplyProjectGenre(master, selection.Genre, artist.primaryGenre);
			float production = CalculateProductionCost(label, master, date);
			// The existing session/master path has now incurred this cost, even if later issue is cancelled.
			ChargeProjectCost(label, artist, production, production);
			WeeklyProductionSpend += production; WeeklyProductionEvents++;
			item.PreparedRecord = master; item.RecordId = master.recordId; item.PaidProduction = production;
			AiRepertoireService.CommitPreparedPick(artist, master, pipelineWeek, IsOwnSession(item) ? "own-session" : "material-search");
			if (item.LpAssembly != null && item.Format == ReleaseFormat.Album) CompleteLpAssembly(item, label);
			if (item.MaterialSearch?.State == ProgrammeMaterialState.Ready)
				MaterialReview(item, pipelineWeek, master.songId == item.MaterialSearch.SongId ? ProgrammeMaterialState.Consumed : ProgrammeMaterialState.Failed,
					master.songId == item.MaterialSearch.SongId ? "material-session-completed" : "player-placement-selected-instead");
			item.PreparedWeek = pipelineWeek;
			item.PreparedRetainedIdentity = selection.RetainedIdentity;
			// Work done ahead of the plan records when the master was actually ready.
			item.ReadyWeek = Math.Min(item.ReadyWeek, pipelineWeek);
			item.ReservedProduction = 0;
			item.ReservedPressing = InitialRunReserve(label, master);
			ReviewSlate(item, ReleaseSlateState.Ready, "master-recorded-and-production-paid");
			return true;
		} finally { issuingSlateItem = null; }
	}

	private float ProgrammeWeeklyRate(AILabel label, GameDate date, ReleaseFormat format) {
		if (!LabelReleaseProgramme.CapacityEnabled) {
			float seasonalAvailability = MarketSeasonality.Enabled ? MarketSeasonality.GetArtistAvailabilityMultiplier(date.year, date.month, liveTick: true) : 1f;
			// Keep the nominal control work envelope, but not its second eligibility lottery or hidden any-format
			// cooldown. Explicit slate feasibility already accounts for available acts and independent clocks.
			return CalculateLabelReleaseCapacityChance(label.releasesPerMonth * LabelCorporateModel.CapacityFactor(label),
				label.status, availableArtists: 3, seasonalAvailability) * (format == ReleaseFormat.Single ? .67f : .33f);
		}
		var capacity = LabelReleaseProgramme.MonthlyCapacity(label);
		// Staff is a work constraint; seasonal availability is a dated preparation preference. Artist eligibility is checked separately.
		float seasonal = MarketSeasonality.Enabled ? MarketSeasonality.GetArtistAvailabilityMultiplier(date.year, date.month, liveTick: true) : 1f;
		return (format == ReleaseFormat.Single ? capacity.Singles : capacity.Albums) / (52f / 12f) *
			LabelCorporateModel.CapacityFactor(label) * LabelReleaseProgramme.SlotStatusFactor(label) * Math.Max(0, seasonal);
	}

	private float ProgrammePromotionPool(AILabel label) => label.GetStandardMarketingBudget() * Math.Max(1f, Math.Min(4f, label.releasesPerMonth));

	private float AvailableProgrammeCampaign(AILabel label, int week, ReleaseSlateItem exclude = null) {
		if (!LabelReleaseProgramme.CapacityEnabled) return float.PositiveInfinity;
		float remaining = ProgrammePromotionPool(label);
		for (int w = week; w < week + LabelReleaseProgramme.CampaignWeeks; w++) {
			float booked = ProgrammeState.Campaigns.Where(c => c.LabelId == label.labelId && c.Week <= w && w < c.Week + LabelReleaseProgramme.CampaignWeeks).Sum(c => c.Spend);
			booked += ProgrammeState.Items.Where(i => i.LabelId == label.labelId && LabelReleaseProgramme.IsOpen(i) && i != issuingSlateItem && i != exclude &&
				i.DueWeek <= w && w < i.DueWeek + LabelReleaseProgramme.CampaignWeeks).Sum(i => i.ReservedMarketing);
			remaining = Math.Min(remaining, ProgrammePromotionPool(label) - booked);
		}
		return Math.Max(0, remaining);
	}

	internal float ProgrammePromotionCommittedPeak(string labelId) {
		if (ProgrammeState == null) return 0;
		var changes = new SortedDictionary<int, double>();
		void Add(int start, float amount) {
			int end = start + LabelReleaseProgramme.CampaignWeeks;
			if (amount <= 0 || end <= pipelineWeek) return;
			start = Math.Max(start, pipelineWeek);
			changes[start] = changes.GetValueOrDefault(start) + amount;
			changes[end] = changes.GetValueOrDefault(end) - amount;
		}
		foreach (var campaign in ProgrammeState.Campaigns.Where(c => c.LabelId == labelId)) Add(campaign.Week, campaign.Spend);
		foreach (var item in ProgrammeState.Items.Where(i => i.LabelId == labelId && LabelReleaseProgramme.IsOpen(i))) Add(item.DueWeek, item.ReservedMarketing);
		double running = 0, peak = 0;
		foreach (double change in changes.Values) { running += change; peak = Math.Max(peak, running); }
		return (float)peak;
	}

	internal void RescheduleProgrammeItem(ReleaseSlateItem item, int week, ReleaseSlateState state, string reason) {
		item.DueWeek = week;
		// Only unspent holds move. Keep the recorded master and its already paid production cost.
		item.ReservedProduction = item.ReservedMarketing = item.ReservedPressing = 0;
		item.MissedFollowUp |= item.Source == "hit-follow-up" && pipelineWeek >= item.IntendedWeek && week > item.IntendedWeek;
		ReviewSlate(item, state, reason);
	}

	internal float ProgrammePushFactor(RecordRuntimeData record) {
		if (!LabelReleaseProgramme.CapacityEnabled || record?.baseRecord == null || record.baseRecord.isPlayerOwned || ProgrammeState == null) return 1f;
		if (programmeCampaignByRecord.Count != ProgrammeState.Campaigns.Count) {
			programmeCampaignByRecord.Clear();
			foreach (var c in ProgrammeState.Campaigns) programmeCampaignByRecord[c.RecordId] = c;
		}
		programmeCampaignByRecord.TryGetValue(record.baseRecord.recordId, out var campaign);
		return campaign == null ? 1f : pipelineWeek < campaign.Week + LabelReleaseProgramme.CampaignWeeks ? campaign.Share : 0f;
	}

	private float ClampProgrammeMarketing(float proposed) => programmeCampaignBudget.HasValue ? Math.Min(proposed, programmeCampaignBudget.Value) : proposed;
	private float CapExternalProgrammeMarketing(AILabel label, float proposed) {
		if (!LabelReleaseProgramme.CapacityEnabled || label.isPlayerOwned) return proposed;
		ProgrammeState ??= new();
		return Math.Min(proposed, AvailableProgrammeCampaign(label, pipelineWeek));
	}

	private void TransferProgrammeCampaign(RecordRuntimeData record, AILabel to) {
		if (!LabelReleaseProgramme.CapacityEnabled || ProgrammeState == null) return;
		var booking = ProgrammeState.Campaigns.FirstOrDefault(c => c.RecordId == record.baseRecord.recordId);
		if (booking != null) ProgrammeState.Campaigns.Remove(booking);
		// Buying an existing master does not buy the selling label's promotion department. A new paid campaign
		// must draw from the buyer's pool; no new marketing payment is made by the ownership transfer itself.
		if (!to.isPlayerOwned) ProgrammeState.Campaigns.Add(new ProgrammeCampaign { LabelId = to.labelId,
			RecordId = record.baseRecord.recordId, Week = pipelineWeek, Spend = 0, Share = 0 });
		programmeCampaignByRecord.Clear();
	}

	internal void BookProgrammeCampaign(Record record, SimulatedArtist artist, AILabel label, float spend) {
		if (!LabelReleaseProgramme.CapacityEnabled || label.isPlayerOwned || ProgrammeState == null) return;
		if (ProgrammeState.Campaigns.Any(c => c.RecordId == record.recordId)) return;
		float booked = ProgrammeState.Campaigns.Where(c => c.LabelId == label.labelId && c.Week <= pipelineWeek && pipelineWeek < c.Week + LabelReleaseProgramme.CampaignWeeks).Sum(c => c.Spend);
		// A demotion shrinks the pool under bookings made at the old tier; a zero spend cannot add to that.
		if (spend > 0f && booked + spend > ProgrammePromotionPool(label) + .1f)
			throw new InvalidOperationException($"Programme promotion overbooked for {label.labelId}: {booked} + {spend}.");
		ProgrammeState.Campaigns.Add(new ProgrammeCampaign { LabelId = label.labelId, RecordId = record.recordId, Week = pipelineWeek,
			Spend = spend, Share = Math.Clamp(spend / Math.Max(1f, artist == null ? label.GetStandardMarketingBudget() : label.GetMarketingBudget(artist)), 0, 1) });
		programmeCampaignByRecord.Clear();
	}

	private bool CanProgrammeArtist(AILabel label, SimulatedArtist artist, ReleaseFormat format, int week) {
		if (!GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist) || artist.labelId != label.labelId || artist.members?.Count is not > 0) return false;
		ProgrammeClock clock = ProgrammeClockFor(artist);
		return week - (format == ReleaseFormat.Single ? clock.SingleWeek : clock.AlbumWeek) >=
			(format == ReleaseFormat.Single ? LabelReleaseProgramme.SingleFloor : LabelReleaseProgramme.AlbumFloor) &&
			(format != ReleaseFormat.Album || !HasPendingProjectForArtist(artist.artistId));
	}

	// An act may hold several open single intentions when they are spaced by the single floor; one open LP at most.
	private bool HasConflictingIntention(SimulatedArtist artist, ReleaseFormat format, int week) =>
		ProgrammeState.Items.Any(i => i.ArtistId == artist.artistId && i.Format == format && LabelReleaseProgramme.IsOpen(i) &&
			(format == ReleaseFormat.Album || Math.Abs(i.IntendedWeek - week) < LabelReleaseProgramme.SingleFloor));

	private void PlanProgramme(AILabel label, GameDate date, int? from = null) {
		int start = from ?? Math.Max(pipelineWeek + LabelReleaseProgramme.PreparationWeeks, ProgrammeState.PlannedThrough.GetValueOrDefault(label.labelId) + 1);
		for (int week = start; week <= pipelineWeek + LabelReleaseProgramme.Horizon; week++) {
			GameDate intended = date.AddDays((week - pipelineWeek) * 7);
			foreach (ReleaseFormat format in new[] { ReleaseFormat.Single, ReleaseFormat.Album }) {
				if (format == ReleaseFormat.Album && !enableAlbums) continue;
				int slots = LabelReleaseProgramme.Slots(ProgrammeWeeklyRate(label, intended, format), ProgrammeRoll(label, null, format.ToString(), week, 0));
				// --hit-follow-up-finance: dedicated slots after the base ones, open only to acts with a recent Top 40.
				int financedSlots = format == ReleaseFormat.Single ? HitFollowUpSlots(label, week) : 0;
				for (int slot = 0; slot < slots + financedSlots; slot++) {
					bool financed = slot >= slots;
					string id = $"{ProgrammeLabelKey(label)}|{intended.year:D4}-{intended.month:D2}-{intended.day:D2}|{format}|{slot}";
					var audit = OnProgrammeChoiceReview == null ? null : new List<ProgrammeCandidateAudit>();
					IEnumerable<SimulatedArtist> eligible = label.roster.Where(a => {
						// Preserve the original short circuit and its clock reads. Audit exclusions
						// without evaluating a rejected artist's forecast or drawing anything new.
						string reason = !CanProgrammeArtist(label, a, format, week) ? "artist-or-spacing-ineligible" :
							HasConflictingIntention(a, format, week) ? "conflicting-intention" :
							financed && !IsHotProgrammeAct(a, week) ? "not-eligible-for-financed-slot" : null;
						if (reason == null) return true;
						audit?.Add(new ProgrammeCandidateAudit { ArtistId = a?.artistId, CareerState = a?.careerState.ToString(), Reason = reason });
						return false;
					});
					// --label-project-judgement: heard material ranks the competing acts on one scale, each act's best read against
					// the slot's mean. Whether a song beats the act's own writing is the hold review's question, not the ranking's.
					Dictionary<string, float> materialReads = null;
					float materialMean = 0f;
					if (LabelReleaseProgramme.ProjectJudgementEnabled && format == ReleaseFormat.Single) {
						var acts = eligible.ToList();
						eligible = acts;
						materialReads = new(StringComparer.Ordinal);
						foreach (var a in acts) materialReads[a.artistId] = AiRepertoireService.Judge(label, a, intended.year, pipelineWeek).BestPerceived;
						materialMean = SlotMaterialMean(materialReads.Values);
					}
					var candidates = eligible
						.Select(a => {
							var clock = ProgrammeClockFor(a);
							int last = format == ReleaseFormat.Single ? clock.SingleWeek : clock.AlbumWeek;
							foreach (var open in ProgrammeState.Items.Where(i => i.ArtistId == a.artistId && i.Format == format &&
								LabelReleaseProgramme.IsOpen(i) && i.IntendedWeek < week)) last = Math.Max(last, open.IntendedWeek);
							int age = week - last;
							bool hot = HitActCadence.WeeksSinceTop40(a, pipelineWeek) <= HitActCadence.HotWindowWeeks;
							bool due = format == ReleaseFormat.Single && hot && age >= HitActCadence.DueWeeks;
							float obligations = format == ReleaseFormat.Single ? Math.Max(0, a.contractSinglesObligation - a.contractReleases) : 0;
							var context = BuildDecisionContext(label, a, intended.year, intended.month);
							context.careerBand = GetCareerBandIndex(a.careerState, out _);
							float net = format == ReleaseFormat.Single ? SinglePriorNetAtBookedPrice(context) :
								CalculateAlbumPriorNet(label, a, intended.year, context, CalculateCompilationCostWeight(a.primaryGenre, intended.year), ResolveHitInventory(a), out _);
							float contribution = Math.Clamp(net / Math.Max(1f, context.singleProductionCost), -1f, 1f);
							float attention = due ? 10 : 0;
							// Hit urgency remains useful, but cannot swamp material and all other business evidence.
							if (materialReads != null) attention = (due ? .75f : 0) + MaterialAttention(materialReads[a.artistId], materialMean);
							float score = attention + a.GetCareerPriority() + a.momentum + a.contractRegionalBreakouts * .1f + contribution +
								Math.Min(1f, age / 52f) + obligations * .1f + (float)ProgrammeRoll(label, a, id, week, 1) * (.5f - label.scoutingAbility * .25f);
							var read = audit == null ? null : new ProgrammeCandidateAudit {
								ArtistId = a.artistId, CareerState = a.careerState.ToString(), Eligible = true, Reason = "scored",
								Score = score, Hot = hot, Due = due, Gap = age, ExpectedNet = net, Contribution = contribution,
								QualityEstimate = context.qualityEstimate, Priority = a.GetCareerPriority(), Momentum = a.momentum,
								Obligations = obligations, AttachedAudienceAtAudit = ArtistFanbase.Heads(a.fanbase)
							};
							return (Artist: a, Score: score, Due: due, Age: age, Obligations: obligations, Net: net, Audit: read);
						}).OrderByDescending(c => c.Score).ThenBy(c => c.Artist.artistId, StringComparer.Ordinal).ToList();
					if (audit != null) {
						for (int rank = 0; rank < candidates.Count; rank++) {
							var read = candidates[rank].Audit;
							read.Rank = rank + 1; read.Selected = rank == 0;
							read.Reason = read.Selected ? "selected-best-score" : "passed-over-lower-score";
							audit.Add(read);
						}
						OnProgrammeChoiceReview?.Invoke(new ProgrammeChoiceAudit { LabelId = label.labelId, Tier = label.tier.ToString(),
							Status = label.status.ToString(), SlotId = id, Format = format, Week = pipelineWeek, Year = date.year,
							IntendedWeek = week, IntendedYear = intended.year, Financed = financed, Candidates = audit });
					}
					if (candidates.Count == 0) continue;
					var chosen = candidates[0];
					var decision = BuildDecisionContext(label, chosen.Artist, intended.year, intended.month);
					// Cash and promotion are held from the start of the preparation lead, not for the whole horizon:
					// a label funds its next few weeks of work, not eight weeks of launches at once.
					var item = new ReleaseSlateItem { Id = id, LabelId = label.labelId, ArtistId = chosen.Artist.artistId, Format = format,
						Source = chosen.Due ? "hit-follow-up" : chosen.Obligations > 0 ? "contract-delivery" : "roster-programme",
						Evidence = $"career={chosen.Artist.careerState};momentum={chosen.Artist.momentum:R};gap={chosen.Age};owed={chosen.Obligations};estimate={decision.qualityEstimate:R};priorNet={chosen.Net:R}" + (financed ? ";financedSlot=1" : ""),
						Score = chosen.Score, ProposedWeek = pipelineWeek, IntendedWeek = week, DueWeek = week, ReadyWeek = week - 1,
						IntendedDate = intended, ContractSequence = chosen.Artist.contractSequence };
					ProgrammeState.Items.Add(item);
					ReviewSlate(item, ReleaseSlateState.Proposed, "programme-review");
				}
			}
		}
		ProgrammeState.PlannedThrough[label.labelId] = pipelineWeek + LabelReleaseProgramme.Horizon;
	}

	private float CalculateAlbumPriorCost(AILabel label, SimulatedArtist artist, int year, DecisionContext decision) {
		CalculateAlbumPriorNet(label, artist, year, decision, CalculateCompilationCostWeight(artist.primaryGenre, year), ResolveHitInventory(artist), out var diagnostics);
		return diagnostics.productionCost;
	}

	private void ProcessReleaseProgramme(AILabel label, GameDate date) {
		ProgrammeState ??= new();
		if (!LabelReleaseProgramme.AllocationEnabled) {
			float rate = ProgrammeWeeklyRate(label, date, ReleaseFormat.Single) + ProgrammeWeeklyRate(label, date, ReleaseFormat.Album);
			int attempts = LabelReleaseProgramme.Slots(rate, ProgrammeRoll(label, null, "capacity", pipelineWeek, 2));
			for (int n = 0; n < attempts; n++) {
				var rates = LabelReleaseProgramme.MonthlyCapacity(label);
				programmeCampaignBudget = Math.Min(AvailableProgrammeCampaign(label, pipelineWeek), ProgrammePromotionPool(label) / Math.Max(1, rates.Singles + rates.Albums));
				try { CountProgrammeAttempt(label, TryReleaseRecord(label, date)); } finally { programmeCampaignBudget = null; }
			}
			return;
		}
		// A label's first horizon starts this week: the opening world, or a newly founded label, already has work in
		// hand, as under the control, rather than idling through the preparation lead.
		if (!ProgrammeState.PlannedThrough.ContainsKey(label.labelId)) PlanProgramme(label, date, pipelineWeek);
		SeedOpeningCatalogue();
		PlanBreakoutFollowUps(label);
		ReviewProgrammeMaterialSearches(label, date);
		var remainingSlots = new Dictionary<ReleaseFormat, int>();
		var preparationSlots = new Dictionary<ReleaseFormat, int>();
		var preparedNow = new HashSet<ReleaseSlateItem>();
		// --slate-work-budget: the planned intentions are the budget, so their sessions and issues take no second draw.
		bool budget = LabelReleaseProgramme.UsesWorkBudget(label);
		foreach (var format in new[] { ReleaseFormat.Single, ReleaseFormat.Album }) {
			if (budget) { remainingSlots[format] = preparationSlots[format] = int.MaxValue / 2; continue; }
			remainingSlots[format] = LabelReleaseProgramme.Slots(ProgrammeWeeklyRate(label, date, format),
				ProgrammeRoll(label, null, format.ToString(), pipelineWeek, 0));
			preparationSlots[format] = LabelReleaseProgramme.Slots(LabelReleaseProgramme.PreparationRate(label, ProgrammeWeeklyRate(label, date.AddDays(7), format)),
				ProgrammeRoll(label, null, format.ToString(), pipelineWeek + 1, 0));
			if (format != ReleaseFormat.Single) continue;
			// The financed work planned for these weeks also needs session and issue capacity to happen.
			remainingSlots[format] += HitFollowUpSlots(label, pipelineWeek);
			preparationSlots[format] += HitFollowUpSlots(label, pipelineWeek + 1);
		}
		foreach (var item in ProgrammeState.Items.Where(i => i.LabelId == label.labelId && LabelReleaseProgramme.IsOpen(i) && i.State != ReleaseSlateState.AwaitingDelivery).OrderBy(i => i.IntendedWeek).ThenByDescending(i => i.Score).ToList()) {
			var artist = label.roster.FirstOrDefault(a => a.artistId == item.ArtistId);
			if (artist == null || artist.contractSequence != item.ContractSequence || !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist)) {
				ReviewSlate(item, ReleaseSlateState.Cancelled, "rights-or-artist-unavailable"); continue;
			}
			if (pipelineWeek - item.IntendedWeek > LabelReleaseProgramme.Horizon) { ReviewSlate(item, ReleaseSlateState.Cancelled, "window-expired"); continue; }
			if (item.Format == ReleaseFormat.Single && item.DueWeek > pipelineWeek + LabelReleaseProgramme.PreparationWeeks &&
				HitActCadence.WeeksSinceTop40(artist, pipelineWeek) <= 2 && pipelineWeek - ProgrammeClockFor(artist).SingleWeek >= LabelReleaseProgramme.SingleFloor) {
				int rushWeek = pipelineWeek + LabelReleaseProgramme.PreparationWeeks;
				item.ReadyWeek = Math.Min(item.ReadyWeek, rushWeek - 1);
				RescheduleProgrammeItem(item, rushWeek, item.State, "observed-breakout-rush-review");
			}
			// Opening intentions dated this week were prepared before the horizon began, in this week's dated slot.
			bool opening = item.IntendedWeek == item.ProposedWeek && pipelineWeek == item.IntendedWeek;
			if (pipelineWeek >= item.ReadyWeek && item.PreparedRecord == null && !opening && preparationSlots[item.Format] <= 0) {
				// Work queued behind the dated capacity holds no cash; it re-reserves when it reaches a slot, so a
				// backlog cannot starve the label's next intention of funds.
				RescheduleProgrammeItem(item, Math.Max(item.DueWeek, pipelineWeek + 1), ReleaseSlateState.Postponed, "preparation-capacity-unavailable"); continue;
			}
			if (item.ReservedMarketing <= 0 &&
				pipelineWeek >= Math.Min(item.IntendedWeek, item.DueWeek) - LabelReleaseProgramme.PreparationWeeks) {
				if (ReserveSlate(label, artist, item, date, out string shortfall)) ReviewSlate(item,
					item.PreparedRecord == null ? ReleaseSlateState.InPreparation : ReleaseSlateState.Scheduled, "work-and-cash-reserved");
				else if (pipelineWeek >= item.ReadyWeek) {
					// Short cash postpones the work; the window, not one week's balance, decides cancellation.
					RescheduleProgrammeItem(item, Math.Max(item.DueWeek, pipelineWeek + 1), ReleaseSlateState.Postponed, shortfall); continue;
				}
			}
			if (pipelineWeek >= item.ReadyWeek && item.PreparedRecord == null) {
				if (!PrepareSlateMaster(label, artist, date, item)) {
					if (pipelineWeek - item.IntendedWeek > LabelReleaseProgramme.Horizon) ReviewSlate(item, ReleaseSlateState.Cancelled, "preparation-window-expired");
						else RescheduleProgrammeItem(item, Math.Max(item.DueWeek, pipelineWeek + 1), ReleaseSlateState.Postponed, item.ProjectReview?.Action is "hold-for-stronger-material" or "await-material-delivery" ? item.ProjectReview.Action : "preparation-material-economics-or-funding");
					continue;
				}
				if (!opening) preparationSlots[item.Format]--;
				preparedNow.Add(item);
			}
			if (item.State == ReleaseSlateState.Ready) ReviewSlate(item, ReleaseSlateState.Scheduled, "dated-issue-slot");
			if (item.DueWeek > pipelineWeek) continue;
			if (pipelineWeek < LabelReleaseProgramme.EarliestIssueWeek(item)) {
				RescheduleProgrammeItem(item, LabelReleaseProgramme.EarliestIssueWeek(item), ReleaseSlateState.Postponed, "recorded-master-needs-issue-lead"); continue;
			}
			if (pipelineWeek - item.IntendedWeek > LabelReleaseProgramme.Horizon) { ReviewSlate(item, ReleaseSlateState.Cancelled, "window-expired"); continue; }
			if (remainingSlots[item.Format] <= 0) {
				RescheduleProgrammeItem(item, pipelineWeek + 1, ReleaseSlateState.Postponed, "issue-capacity-unavailable"); continue;
			}
			if (!IssueSlate(label, artist, item, date, remainingSlots)) {
				// Release reservations when a slot fails. Re-admit next week against the live cash and promotion books.
				RescheduleProgrammeItem(item, pipelineWeek + 1, ReleaseSlateState.Postponed, "material-economics-funding-or-availability");
			}
		}
		// Dated work left idle this week (an empty, failed or cash-short slot) prepares the next intentions early, and
		// issues ones prepared in an earlier week, so one failure does not push every later intention back a week.
		// Unused work is still not banked. Only intentions dated after this week qualify: a failure is not retried,
		// and a master is never recorded and issued in the same week. A work-budget label has no idle dated work.
		if (!budget) foreach (var item in ProgrammeState.Items.Where(i => i.LabelId == label.labelId && LabelReleaseProgramme.CanUseIdleSlot(i, pipelineWeek) && !preparedNow.Contains(i))
			.OrderBy(i => i.DueWeek).ThenByDescending(i => i.Score).ToList()) {
			if (preparationSlots[item.Format] <= 0 && remainingSlots[item.Format] <= 0) continue;
			var artist = label.roster.FirstOrDefault(a => a.artistId == item.ArtistId);
			if (artist == null || artist.contractSequence != item.ContractSequence || !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist)) continue;
			if (item.PreparedRecord == null) {
				if (preparationSlots[item.Format] <= 0) continue;
				if (item.ReservedMarketing <= 0) {
					if (!ReserveSlate(label, artist, item, date, out _)) continue;
					ReviewSlate(item, ReleaseSlateState.InPreparation, "work-and-cash-reserved");
				}
				if (!PrepareSlateMaster(label, artist, date, item)) {
					RescheduleProgrammeItem(item, item.DueWeek, ReleaseSlateState.Postponed, item.ProjectReview?.Action is "hold-for-stronger-material" or "await-material-delivery" ? item.ProjectReview.Action : "idle-preparation-material-economics-or-funding"); continue;
				}
				preparationSlots[item.Format]--;
				ReviewSlate(item, ReleaseSlateState.Scheduled, "worked-ahead-into-idle-preparation");
				continue;
			}
			if (remainingSlots[item.Format] <= 0 || !CanProgrammeArtist(label, artist, item.Format, pipelineWeek)) continue;
			if (!IssueSlate(label, artist, item, date, remainingSlots))
				RescheduleProgrammeItem(item, item.DueWeek, ReleaseSlateState.Postponed, "idle-issue-material-economics-funding-or-availability");
		}
		PlanProgramme(label, date);
	}

	private float ProgrammeTaxAndServiceHold(AILabel label) => LabelReleaseProgramme.UsesGoingConcernReserve(label)
		? (float)LabelCorporateModel.ProgrammeTaxDue(label, TimeManager.Instance?.CurrentDate.month ?? 1, LabelReleaseProgramme.GoingConcernTaxMonths)
		: (float)LabelCorporateModel.TaxAndServiceReserve(label);

	// The slate's commitment reserve: the ordinary one, or its going-concern form under --slate-going-concern-reserve.
	internal float ProgrammeCommitmentReserve(AILabel label) {
		float reserve = EconomicLabelLifecycle.CommitmentReserve(label, false);
		return LabelReleaseProgramme.UsesGoingConcernReserve(label)
			? reserve - (float)LabelCorporateModel.TaxAndServiceReserve(label) + ProgrammeTaxAndServiceHold(label) : reserve;
	}

	internal bool ReserveSlate(AILabel label, SimulatedArtist artist, ReleaseSlateItem item, GameDate date, out string shortfall, int? campaignWeek = null) {
		var decision = BuildDecisionContext(label, artist, date.year, date.month);
		float production = item.PreparedRecord != null ? 0 : item.Format == ReleaseFormat.Single ? decision.singleProductionCost : CalculateAlbumPriorCost(label, artist, date.year, decision);
		float pressing = item.PreparedRecord == null ? 0 : InitialRunReserve(label, item.PreparedRecord);
		float marketing = label.GetMarketingBudget(artist);
		if (LabelReleaseProgramme.CapacityEnabled) {
			var rates = LabelReleaseProgramme.MonthlyCapacity(label);
			marketing = Math.Min(marketing, Math.Min(AvailableProgrammeCampaign(label, campaignWeek ?? Math.Max(item.DueWeek, pipelineWeek), item),
				ProgrammePromotionPool(label) / Math.Max(1, rates.Singles + rates.Albums)));
			marketing *= LabelReleaseProgramme.CampaignStatusFactor(label);
		}
		shortfall = marketing <= 0 ? "promotion-congestion" : "insufficient-unreserved-cash";
		float ownHold = item != issuingSlateItem && ProgrammeState.Items.Contains(item) && LabelReleaseProgramme.IsOpen(item)
			? item.ReservedProduction + item.ReservedMarketing + item.ReservedPressing : 0;
		if (marketing <= 0) return false;
		float unreserved = label.cashReserves - (ProgrammeCommitmentReserve(label) - ownHold);
		// --hit-follow-up-finance: a hot act's follow-up may sell receivables for the missing cash (no-op otherwise).
		float factored = 0f;
		if (unreserved < production + marketing + pressing)
			unreserved += factored = FactorForHitFollowUp(label, artist, production + marketing + pressing - unreserved);
		if (unreserved < production + marketing + pressing) {
			var parts = OnProgrammeCashShortfall != null ? LabelCorporateModel.TaxAndServiceParts(label) : default;
			if (OnProgrammeCashShortfall != null && LabelReleaseProgramme.UsesWorkBudget(label))
				OnProgrammeCashShortfall(new ProgrammeCashShortfall { Week = pipelineWeek, LabelId = label.labelId, Tier = label.tier.ToString(),
					Status = label.status.ToString(), SlotId = item.Id, ArtistId = artist.artistId, Format = item.Format, Cash = label.cashReserves,
					Overhead = label.GetMonthlyOverhead(), NegativeFlowHold = Math.Max(0f, -label.RecentMonthlyCashFlow) * 2f,
					PlantPayables = LabelPressingEconomics.PlantCredit ? label.outstandingPlantPayables : 0f,
					TaxAndService = ProgrammeTaxAndServiceHold(label), OtherProgrammeHolds = ProgrammeCashReserved(label.labelId) - ownHold,
					Production = production, Marketing = marketing, Pressing = pressing, Receivables = label.outstandingWholesaleReceivables, Factored = factored,
					TaxPayable = (float)parts.Payable, UnprovidedTax = (float)parts.Unprovided, CommercialHold = (float)parts.Commercial });
			return false;
		}
		// Only price bets that pass ordinary current funding. Do not label an existing cash shortfall a risk refusal.
		if (!PassesSingleCapitalRisk(label, artist, item, decision, marketing, unreserved)) {
			shortfall = "single-capital-risk";
			return false;
		}
		item.ReservedProduction = production; item.ReservedMarketing = marketing; item.ReservedPressing = pressing;
		return true;
	}

	// An LP may open with an advance single through the existing AlbumWithPromo project: the project's first
	// event, the single, uses the LP's dated slot and the LP drops after the existing gap. The single still
	// competes for production cash, the promotion pool and the act's single clock. Requiring a coincident free
	// single slot instead blocked ~90% of decisions on calendar accident, not on the researched selectivity.
	// Observation only: which issue-time gate refused a prepared master. Nothing reads it back.
	public event Action<ReleaseSlateItem, int, string> OnProgrammeIssueRefused;
	private string lastProgrammeIssueRefusal;

	private bool IssueSlate(AILabel label, SimulatedArtist artist, ReleaseSlateItem item, GameDate date, Dictionary<ReleaseFormat, int> remainingSlots) {
		if (item.PreparedRecord == null || pipelineWeek < LabelReleaseProgramme.EarliestIssueWeek(item)) return false;
		// An idle slot may issue before the reserved date. Requote the actual launch window and live cash,
		// excluding this item's old holds; a future booking cannot authorize an unfunded launch today.
		bool funded = ReserveSlate(label, artist, item, date, out string shortfall, campaignWeek: pipelineWeek);
		bool success = false;
		programmeAdvanceSingleId = null;
		string refusal = !funded ? "reserve:" + shortfall : item.ReservedMarketing <= 0 ? "no-reserved-marketing" : null;
		if (funded && item.ReservedMarketing > 0 && CanProgrammeArtist(label, artist, item.Format, pipelineWeek)) {
			issuingSlateItem = item;
			programmeAdvanceSlot = item.Format == ReleaseFormat.Album;
			float campaign = Math.Min(item.ReservedMarketing, AvailableProgrammeCampaign(label, pipelineWeek));
			programmeCampaignBudget = campaign;
			lastProgrammeIssueRefusal = null;
			try { if (programmeCampaignBudget > 0) success = TryReleaseRecord(label, date, item.Format == ReleaseFormat.Single, artist, item.Format == ReleaseFormat.Album); }
			finally { issuingSlateItem = null; programmeCampaignBudget = null; programmeAdvanceSlot = false; }
			refusal = campaign <= 0 ? "campaign-budget-exhausted" : lastProgrammeIssueRefusal ?? "release-failed";
		} else refusal ??= "artist-spacing-or-eligibility";
		CountProgrammeAttempt(label, success);
		if (!success) { OnProgrammeIssueRefused?.Invoke(item, pipelineWeek, refusal); return false; }
		remainingSlots[item.Format]--;
		var issued = ChartManager.Instance.GetRecordRuntimeData(item.RecordId);
		if (issued != null && issued.baseRecord.labelId == item.LabelId && Math.Abs(issued.sunkProductionCost - item.PaidProduction) > .01f)
			throw new InvalidOperationException("Programme production must enter the issued record exactly once.");
		if (programmeAdvanceSingleId != null) AwaitProgrammeDelivery(item);
		else {
			item.ActualWeek = pipelineWeek; item.ActualDate = date;
			ReviewSlate(item, ReleaseSlateState.Issued, "issued-through-existing-finance-and-catalogue");
		}
		return true;
	}

	internal void AwaitProgrammeDelivery(ReleaseSlateItem item) {
		// The existing LP project now owns the recorded master and its outstanding costs.
		item.PreparedRecord = null;
		item.ReservedProduction = item.ReservedMarketing = item.ReservedPressing = 0;
		item.ActualWeek = 0; item.ActualDate = default;
		ReviewSlate(item, ReleaseSlateState.AwaitingDelivery, "advance-single-issued-awaiting-lp");
	}

	internal void CompleteProgrammeDelivery(string recordId, GameDate date, int? actualWeek = null) {
		if (ProgrammeState == null) return;
		var item = ProgrammeState.Items.FirstOrDefault(i => i.RecordId == recordId && i.State == ReleaseSlateState.AwaitingDelivery);
		if (item == null) return;
		item.ActualWeek = actualWeek ?? pipelineWeek; item.ActualDate = date;
		ReviewSlate(item, ReleaseSlateState.Issued, "issued-delayed-lp");
	}

	internal void ReconcileProgrammeDelivery(ReleaseSlateItem item, AlbumProject project) {
		if (project == null || project.terminalState == AlbumProjectTerminalState.Cancelled) {
			ReviewSlate(item, ReleaseSlateState.Cancelled, project == null ? "lp-project-missing" : "lp-project-cancelled");
		} else if (project.terminalState == AlbumProjectTerminalState.Released) {
			CompleteProgrammeDelivery(item.RecordId, project.dropDate, project.dropWeek);
		}
	}

	private void CountProgrammeAttempt(AILabel label, bool success) {
		WeeklyReleaseRollsFired++;
		if (success) WeeklySuccessfulReleases++; else WeeklyFailedReleaseRolls++;
		RecordReleaseAttempt(label.tier, success, !success && lastReleaseAttemptFailedArtistSelection);
	}

	private void MaintainProgramme() {
		ProgrammeState ??= new();
		if (LabelReleaseProgramme.CapacityEnabled && !ProgrammeState.CampaignsInitialized) {
			foreach (var group in ChartManager.Instance.GetAllRecords().GroupBy(r => r.baseRecord.labelId)) {
				var label = GetLabel(group.Key);
				if (label == null || label.isPlayerOwned) continue;
				var recent = group.Where(r => r.weeksSinceRelease < LabelReleaseProgramme.CampaignWeeks).ToList();
				float alreadyBooked = ProgrammeState.Campaigns.Where(c => c.LabelId == label.labelId && c.Week <= pipelineWeek &&
					pipelineWeek < c.Week + LabelReleaseProgramme.CampaignWeeks).Sum(c => c.Spend);
				int unbooked = recent.Count(r => !ProgrammeState.Campaigns.Any(c => c.RecordId == r.baseRecord.recordId));
				float share = Math.Max(0, ProgrammePromotionPool(label) - alreadyBooked) / Math.Max(1, unbooked);
				foreach (var record in group) {
					if (ProgrammeState.Campaigns.Any(c => c.RecordId == record.baseRecord.recordId)) continue;
					float spend = recent.Contains(record) ? share : 0;
					ProgrammeState.Campaigns.Add(new ProgrammeCampaign { LabelId = label.labelId, RecordId = record.baseRecord.recordId,
						Week = pipelineWeek - record.weeksSinceRelease, Spend = spend,
						Share = Math.Clamp(spend / Math.Max(1f, label.GetStandardMarketingBudget()), 0, 1) });
				}
			}
			ProgrammeState.CampaignsInitialized = true;
		}
		foreach (var item in ProgrammeState.Items.Where(i => i.State == ReleaseSlateState.AwaitingDelivery ||
			i.State == ReleaseSlateState.Issued && i.Reason == "issued-advance-single-lp-to-follow")) {
			// v28 saves from the earlier implementation prematurely marked these LPs issued.
			if (item.State == ReleaseSlateState.Issued) AwaitProgrammeDelivery(item);
			ReconcileProgrammeDelivery(item, albumProjects.FirstOrDefault(p => p.albumRecord?.recordId == item.RecordId));
		}
		foreach (var item in ProgrammeState.Items.Where(LabelReleaseProgramme.IsOpen)) {
			// Contract/ownership changes are handled by the existing project transfer/delivery service.
			if (item.State == ReleaseSlateState.AwaitingDelivery) continue;
			var label = GetLabel(item.LabelId);
			if (label?.IsActive != true || label.isPlayerOwned) ReviewSlate(item, ReleaseSlateState.Cancelled, "label-inactive-or-player-owned");
			else if (!label.roster.Any(a => a.artistId == item.ArtistId && a.contractSequence == item.ContractSequence && GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a)))
				ReviewSlate(item, ReleaseSlateState.Cancelled, "rights-or-artist-unavailable");
		}
		// Keep terminal reviews for 52 weeks; campaigns retain only scalar history needed by live records.
		ProgrammeState.Items.RemoveAll(i => !LabelReleaseProgramme.IsOpen(i) && pipelineWeek - i.LastReviewWeek > 52);
		var active = ChartManager.Instance.GetAllRecords().Select(r => r.baseRecord.recordId).ToHashSet(StringComparer.Ordinal);
		if (ProgrammeState.Campaigns.RemoveAll(c => !active.Contains(c.RecordId) && pipelineWeek >= c.Week + LabelReleaseProgramme.CampaignWeeks) > 0)
			programmeCampaignByRecord.Clear();
		var artists = aiLabels.SelectMany(l => l.roster).Select(a => a.artistId).ToHashSet(StringComparer.Ordinal);
		foreach (string id in ProgrammeState.Clocks.Keys.Where(id => !artists.Contains(id)).ToList()) ProgrammeState.Clocks.Remove(id);
	}
}
