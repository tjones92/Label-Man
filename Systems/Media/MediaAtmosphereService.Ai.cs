using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 3 AI characterization (directive §13, §12.2), observe-only. Rival labels act on the same programs, slots, desks and
/// page budgets as the player: a weekly planner sends a capped shortlist of discs and sheets, and when a producer calls,
/// the label answers from its policy, its act's consent and the TV line of its existing promotion envelope. Every amount
/// is an estimate of money the release charge already covered; nothing is debited and no economic system reads any of it.
/// </summary>
public static partial class MediaAtmosphereService {
	private static Dictionary<string, AILabel> labelIndex;
	/// <summary>recordId|programId of the AI disc pitches producers have on their desks this pass.</summary>
	private static HashSet<string> pitchedTv = new(StringComparer.Ordinal);
	/// <summary>Rival declines while booking the current episode (telemetry only).</summary>
	private static int episodeDeclines;

	public static IReadOnlyDictionary<string, AiMediaAllocation> AiAllocations => state.AiAllocations;
	public static IReadOnlyList<AiMediaPitch> AiPitches => state.AiPitches;
	public static IReadOnlyDictionary<string, int> AiLedger => state.AiLedger;

	private static void Ledger(string key, int n = 1) => state.AiLedger[key] = state.AiLedger.GetValueOrDefault(key) + n;

	private static AILabel LabelOf(string labelId) {
		if (labelId == null) return null;
		labelIndex ??= (CompetitorManager.Instance?.GetAllLabels() ?? Array.Empty<AILabel>()).Where(l => l?.labelId != null && !l.isPlayerOwned)
			.GroupBy(l => l.labelId, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
		return labelIndex.TryGetValue(labelId, out var l) ? l : null;
	}

	private static bool InControversy(string artistId, int day) =>
		artistId != null && state.Controversies.Any(c => c.ArtistId == artistId && c.StartDay <= day && c.ExpiresDay >= day);

	/// <summary>The act's current publicity policy, or null for a record no AI label owns.</summary>
	public static MediaPolicy? PolicyOf(string labelId, string artistId, int day) {
		AILabel label = LabelOf(labelId);
		return label == null ? null : AiMediaPlanner.PolicyFor(label, ArtistManager.Instance?.GetArtist(artistId), InControversy(artistId, day));
	}

	/// <summary>The campaign's allocation view, created once from the label's existing envelope when the record first matters to media.</summary>
	private static AiMediaAllocation AllocationFor(RecordRuntimeData r, int day) {
		Record rec = r?.baseRecord;
		if (rec?.recordId == null || rec.isPlayerOwned) return null;
		if (state.AiAllocations.TryGetValue(rec.recordId, out var a)) return a;
		AILabel label = LabelOf(rec.labelId);
		if (label == null) return null;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.artistId);
		a = AiMediaPlanner.Allocate(rec.recordId, label.labelId, rec.artistId, AiMediaPlanner.PolicyFor(label, artist, InControversy(rec.artistId, day)),
			AiMediaPlanner.EnvelopeEstimate(label, artist), day);
		state.AiAllocations[rec.recordId] = a;
		Ledger("policy-" + a.Policy);
		return a;
	}

	private static int ActiveMembers(string artistId) =>
		Math.Max(1, ArtistManager.Instance?.GetArtist(artistId)?.members?.Count(m => m != null && m.isActive) ?? 1);

	/// <summary>
	/// A producer has a slot for a rival record: does the label take it (§13)? Policy and the act's consent first; then the
	/// TV line of the campaign envelope must cover the travel and rehearsal, trying a cheaper rehearsal before declining.
	/// Accepting reserves the cost once under a receipt for this episode; a second read never reserves again.
	/// </summary>
	private static bool AiAccepts(Candidate c, TvProgram program, GameDate date, string episodeId, Evidence evidence) {
		if (c.Pitch != null) return true;
		Record rec = c.Record.baseRecord;
		int day = BroadcastExposure.DayIndex(date);
		AiMediaAllocation alloc = AllocationFor(c.Record, day);
		if (alloc == null) return true;
		string receipt = "tv|" + episodeId;
		if (alloc.Receipts.ContainsKey(receipt)) return true;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.artistId);
		MediaPolicy policy = InControversy(rec.artistId, day) ? MediaPolicy.CautiousCrisis : alloc.Policy;
		if (!AiMediaPlanner.Considers(program, policy, artist, rec.primaryGenre, day, out string reason)) {
			alloc.Declined++; episodeDeclines++; Ledger("decline-" + reason); return false;
		}
		string city = evidence.CityOf(rec.artistId);
		int members = ActiveMembers(rec.artistId);
		MediaPreparation prep = AiPreparation(rec.labelId);
		long cost = AiMediaPlanner.AppearanceCents(program, date, city, members, prep);
		if (cost > alloc.TvRemaining && prep != MediaPreparation.Minimal) {
			prep = MediaPreparation.Minimal;
			cost = AiMediaPlanner.AppearanceCents(program, date, city, members, prep);
		}
		if (cost > alloc.TvRemaining) { alloc.Declined++; episodeDeclines++; Ledger("decline-budget"); return false; }
		c.Prep = prep;
		alloc.Receipts[receipt] = cost;
		alloc.TvReservedCents += cost;
		alloc.Accepted++;
		Ledger(program.Network == TvNetwork.Local ? "accept-local" : "accept-network");
		return true;
	}

	/// <summary>A booking that fell through before air (a withdrawal at standards) gives its reservation back.</summary>
	private static void ReleaseReservation(Candidate c, string episodeId) {
		if (c.Pitch != null || c.Record?.baseRecord?.recordId == null) return;
		if (!state.AiAllocations.TryGetValue(c.Record.baseRecord.recordId, out var alloc)) return;
		if (alloc.Receipts.Remove("tv|" + episodeId, out long cost)) { alloc.TvReservedCents -= cost; alloc.Accepted--; }
	}

	// ---- the weekly plan ---------------------------------------------------------------------------------------------

	/// <summary>
	/// A small bounded plan at the weekly boundary (§13): each rival label picks at most two campaign records (on the national
	/// chart or near the top of a regional report), and for each sends at most one disc to a producer and one sheet to a
	/// desk, ranked by the policy's audience fit and what the envelope can afford. Pitches reserve nothing for television
	/// until a date is taken; a press sheet is a real mailing and reserves its few cents once.
	/// </summary>
	private static void AiPlanWeek(Evidence evidence, GameDate weekEnd) {
		int today = BroadcastExposure.DayIndex(weekEnd);
		foreach (AiMediaPitch p in state.AiPitches)
			if (p.Status == PitchStatus.Sent && p.ExpiresDay <= today) p.Status = PitchStatus.Lapsed;
		state.AiPitches.RemoveAll(p => p.Day < today - 56);
		foreach (string id in state.AiAllocations.Keys.ToList()) {
			AiMediaAllocation a = state.AiAllocations[id];
			bool live = evidence.ById.TryGetValue(id, out var rr) && (rr.currentPosition > 0 || rr.unitsThisWeek > 0);
			if (!live && today - a.Day > 28) state.AiAllocations.Remove(id);
		}

		var focus = new Dictionary<string, RecordRuntimeData>(StringComparer.Ordinal);
		foreach (RecordRuntimeData r in evidence.Chart) if (r.currentPosition is > 0 and <= 100) focus[r.baseRecord.recordId] = r;
		foreach (var list in evidence.RegionalBest.Values) foreach (var (r, _) in list.Take(10)) focus[r.baseRecord.recordId] = r;
		var byLabel = focus.Values.Where(r => LabelOf(r.baseRecord.labelId) != null)
			.GroupBy(r => r.baseRecord.labelId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal);

		// Programs on the air in the coming two weeks, once for the pass.
		var airing = MediaOutletCatalog.Programs.Where(p => Enumerable.Range(1, 14).Any(d => p.Runs(weekEnd.AddDays(d)))).ToList();
		GameDate soon = weekEnd.AddDays(7);
		foreach (var group in byLabel) {
			foreach (RecordRuntimeData r in group.OrderByDescending(x => x.unitsThisWeek).ThenBy(x => x.baseRecord.recordId, StringComparer.Ordinal).Take(AiMediaPlanner.FocusRecordsPerLabel)) {
				AiMediaAllocation alloc = AllocationFor(r, today);
				if (alloc == null) continue;
				Record rec = r.baseRecord;
				SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.artistId);
				MediaPolicy policy = InControversy(rec.artistId, today) ? MediaPolicy.CautiousCrisis : alloc.Policy;
				string city = evidence.CityOf(rec.artistId);
				int members = ActiveMembers(rec.artistId);
				string actRegion = DistanceModel.GetCityById(city ?? "")?.parentRegionId;

				// One disc to the producer the policy most wants and the envelope can afford.
				if (!state.AiPitches.Any(p => p.Kind == PitchKind.Television && p.RecordId == rec.recordId && p.Status == PitchStatus.Sent)) {
					TvProgram target = null;
					float bestScore = 0f;
					foreach (TvProgram program in airing) {
						if (program.Network == TvNetwork.Local) {
							string region = DistanceModel.GetCityById(program.HomeCityId)?.parentRegionId;
							bool sells = region != null && r.regionalData.TryGetValue(region, out var rd) && rd != null && rd.unitsSoldThisWeek > 0;
							if (!sells && region != actRegion) continue;
						}
						if (ProgramMemoryFor(program.Id, rec.artistId) is { } m && m.ExcludedUntilDay > today) continue;
						float appeal = AiMediaPlanner.PitchAppeal(program, policy, rec.primaryGenre, r.currentPosition);
						if (appeal <= 0f) continue;
						if (AiMediaPlanner.AppearanceCents(program, soon, city, members, MediaPreparation.Minimal) > alloc.TvRemaining) continue;
						if (!AiMediaPlanner.Considers(program, policy, artist, rec.primaryGenre, today, out _)) continue;
						float score = appeal * (0.85f + 0.3f * MediaAtmosphere.Unit("ai-tv|" + rec.recordId + "|" + program.Id + "|" + today));
						if (score > bestScore) { bestScore = score; target = program; }
					}
					if (target != null) {
						state.AiPitches.Add(new AiMediaPitch {
							Id = "ai-tv|" + rec.recordId + "|" + target.Id + "|" + today, Kind = PitchKind.Television, LabelId = rec.labelId, RecordId = rec.recordId,
							ArtistId = rec.artistId, ArtistName = rec.artistName, ProgramId = target.Id, Policy = policy, Day = today,
							ExpiresDay = today + AiMediaPlanner.PitchLifeDays, Status = PitchStatus.Sent,
						});
						Ledger("pitch-tv-" + target.Kind);
					}
				}

				// One sheet to the desk the policy writes to first.
				if (alloc.PressRemaining < AiMediaPlanner.PressSheetCents) continue;
				PressOutlet desk = null;
				foreach (PressFamily family in AiMediaPlanner.PressPreference(policy)) {
					desk = MediaOutletCatalog.Press.Where(o => o.Family == family && o.Publishing(weekEnd)
							&& (o.National || o.HomeCityId == city || o.Family == PressFamily.Specialist && DistanceModel.GetCityById(o.HomeCityId ?? "")?.parentRegionId == actRegion)
							&& o.Fit(rec.primaryGenre) >= 0.5f
							&& !state.AiPitches.Any(p => p.Kind == PitchKind.Press && p.RecordId == rec.recordId && p.OutletId == o.Id))
						.OrderByDescending(o => o.Fit(rec.primaryGenre) * (0.8f + 0.4f * MediaAtmosphere.Unit("ai-desk|" + rec.recordId + "|" + o.Id)))
						.ThenBy(o => o.Id, StringComparer.Ordinal).FirstOrDefault();
					if (desk != null) break;
				}
				if (desk == null) continue;
				string pressReceipt = "press|" + desk.Id + "|" + rec.recordId;
				if (alloc.Receipts.ContainsKey(pressReceipt)) continue;
				alloc.Receipts[pressReceipt] = AiMediaPlanner.PressSheetCents;
				alloc.PressReservedCents += AiMediaPlanner.PressSheetCents;
				state.AiPitches.Add(new AiMediaPitch {
					Id = "ai-press|" + rec.recordId + "|" + desk.Id, Kind = PitchKind.Press, LabelId = rec.labelId, RecordId = rec.recordId, ArtistId = rec.artistId,
					ArtistName = rec.artistName, OutletId = desk.Id, Policy = policy, Day = today, ExpiresDay = today + 7, Status = PitchStatus.Sent,
				});
				Ledger("pitch-press-" + desk.Family);
			}
		}
	}

	/// <summary>Rival press sheets become leads for the one desk each was sent to, scored by the same rule as the player's.</summary>
	private static void AiPressLeads(List<MediaPress.Lead> leads, Evidence evidence, int today) {
		foreach (AiMediaPitch pitch in state.AiPitches.Where(p => p.Kind == PitchKind.Press && p.Status == PitchStatus.Sent && p.Day < today)) {
			PressOutlet outlet = MediaOutletCatalog.GetPress(pitch.OutletId);
			if (outlet == null || !evidence.ById.TryGetValue(pitch.RecordId ?? "", out RecordRuntimeData r)) { pitch.Status = PitchStatus.Passed; continue; }
			bool hometown = outlet.HomeCityId != null && evidence.CityOf(r.baseRecord.artistId) == outlet.HomeCityId;
			StoryAngle angle = hometown && outlet.Family == PressFamily.LocalDaily ? StoryAngle.Hometown : StoryAngle.LabelPitch;
			float interest = SheetInterest(outlet, r, evidence, hometown, angle, 0f);
			var lead = NewLead("aipitch|" + pitch.Id, outlet.Family == PressFamily.RockCriticism ? StoryAngle.Review : angle, r, today, interest, evidence);
			Fact(lead, EvidenceSource.LabelStatement, $"the label's press sheet on \"{r.baseRecord.title}\"", pitch.Day, firsthand: true, confidence: 0.6f);
			int broke = evidence.BrokeCount(r);
			if (broke > 0) Fact(lead, EvidenceSource.RegionalSales, $"selling in {broke} regional market(s) on dealers' reports", today);
			if (r.currentPosition > 0) Fact(lead, EvidenceSource.NationalChart, $"No. {r.currentPosition} on the national chart", today);
			if (outlet.Family == PressFamily.RockCriticism) Fact(lead, EvidenceSource.RecordItself, "the record", today, confidence: (r.baseRecord.hookStrength + r.baseRecord.productionQuality) * 0.5f);
			lead.Outlets.Add(outlet.Id);
			leads.Add(lead);
			pitch.Status = PitchStatus.Passed; // until the editor's decision says otherwise
		}
	}

	/// <summary>After editors decide: which rival sheets ran, and how rival labels answered teen editors' photo requests.</summary>
	private static void AiAfterDecide(int today) {
		foreach (AiMediaPitch pitch in state.AiPitches.Where(p => p.Kind == PitchKind.Press && p.Status == PitchStatus.Passed && p.Day < today && p.Day >= today - 8)) {
			if (state.Stories.Any(s => s.RootId == "aipitch|" + pitch.Id && s.Status == StoryStatus.Scheduled)) { pitch.Status = PitchStatus.Placed; Ledger("sheet-placed"); }
		}
		int year = FromDayIndex(today).year;
		foreach (MediaStory s in state.Stories.Where(s => s.Status == StoryStatus.Scheduled && s.LeadDay == today && s.Angle == StoryAngle.TeenFeature && !s.PlayerSubject && s.PhotoNote == null)) {
			MediaPolicy? policy = PolicyOf(s.LabelId, s.ArtistId, today);
			if (policy == null) continue;
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(s.ArtistId);
			string answer = AiMediaPlanner.PhotoAnswer(policy.Value, artist, s.Id);
			Ledger("photo-" + answer);
			if (answer == "declined") { s.PhotoNote = "the act declined a photo session; the magazine ran an old publicity still"; continue; }
			MediaFanState fs = EnsureFanState(s.ArtistId, s.ArtistName);
			string face = artist == null ? null : PhotographMember(fs, artist, s.Id, year);
			s.PhotoNote = answer == "exclusive" ? (face != null ? $"exclusive new photographs, {face} on the cover" : "exclusive new photographs") : "photographs supplied by the label";
			if (face != null && s.Face == null) s.Face = face;
		}
	}

	/// <summary>The AI standards answer, now from the act's publicity policy rather than a bare disposition rule (§13).</summary>
	private static StandardsResponse AiStandardsChoice(StandardsQuery query, SimulatedArtist artist, TvProgram program, bool hasAlternate, string labelId, int day) {
		MediaPolicy policy = PolicyOf(labelId, artist?.artistId, day) ?? MediaPolicy.CommercialPragmatism;
		return MediaStandards.AiChoice(query, artist, program.Method, hasAlternate, policy);
	}
}
