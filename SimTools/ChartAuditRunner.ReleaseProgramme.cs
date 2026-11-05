using System.IO;
using System.Globalization;
using System.Linq;

public partial class ChartAuditRunner {
	private StreamWriter releaseSlateWriter;
	private StreamWriter programmeResourceWriter;
	private StreamWriter programmeMaterialWriter;
	private StreamWriter materialFunnelWriter;
	private StreamWriter lpAssemblyWriter;
	private StreamWriter programmeCashShortfallWriter;
	private StreamWriter programmeChoiceWriter;
	private StreamWriter programmeProjectWriter;
	private StreamWriter programmeIssueRefusalWriter;
	private void OpenReleaseProgrammeOutputs(string directory) {
		if (!LabelReleaseProgramme.Enabled) return;
		if (LabelReleaseProgramme.ProjectJudgementEnabled) {
			programmeProjectWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-projects.csv"));
			programmeProjectWriter.WriteLine("week,slotId,labelId,artistId,source,candidateId,action,perceived,routineRead,useSearchedMaterial,uncertainty,ownSessionTiming");
			CompetitorManager.Instance.OnProgrammeProjectReview += WriteProgrammeProject;
		}
		if (System.Linq.Enumerable.Contains(Godot.OS.GetCmdlineUserArgs(), "--label-decision-audit")) {
			programmeChoiceWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-choices.csv"));
			programmeChoiceWriter.WriteLine("week,year,labelId,tier,status,slotId,format,intendedWeek,intendedYear,financed,artistId,careerState,eligible,reason,rank,selected,score,hot,due,gap,expectedNet,contribution,qualityEstimate,careerPriority,momentum,obligations,attachedAudienceAtAudit,followUpScheduling,waitingPriority,audienceOrders,nationalAudienceOrders");
			CompetitorManager.Instance.OnProgrammeChoiceReview += WriteProgrammeChoice;
		}
		releaseSlateWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-slate.csv"));
		releaseSlateWriter.WriteLine("week,slotId,labelId,artistId,format,state,source,intendedWeek,dueWeek,readyWeek,actualWeek,intendedDate,actualDate,recordId,reservedProduction,reservedMarketing,reservedPressing,score,evidence,reason,missedFollowUp,paidProduction,preparedWeek,proposedWeek,weeklyReviewPolicy");
		CompetitorManager.Instance.OnReleaseSlateReview += WriteReleaseSlate;
		programmeIssueRefusalWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-issue-refusals.csv"));
		programmeIssueRefusalWriter.WriteLine("week,slotId,labelId,artistId,format,source,intendedWeek,paidProduction,gate");
		CompetitorManager.Instance.OnProgrammeIssueRefused += WriteProgrammeIssueRefusal;
		programmeResourceWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-resources.csv"));
		programmeResourceWriter.WriteLine("week,year,labelId,tier,attempts,successes,openItems,cashReserved,promotionPool,promotionBooked,singleWeeklyRate,albumWeeklyRate,dueFollowUps,unplannedFollowUps,promotionCommittedPeak,rosterActs,releaseEligibleActs,programmeCapacityActive,nominalSingleMonthlyCapacity,nominalAlbumMonthlyCapacity,staffCoverage,slotStatusFactor,seasonalAvailability");
		CompetitorManager.Instance.OnProgrammeResources += WriteProgrammeResources;
		if (LabelReleaseProgramme.MaterialSearchEnabled) {
			programmeMaterialWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-material-search.csv"));
			programmeMaterialWriter.WriteLine("week,slotId,labelId,artistId,state,reason,professional,songId,supplierId,startedWeek,deliveryWeek,holdUntilWeek,paidFee,proposalRead,baselineRead,expectedBenefit,referenceMasterId,recordId");
			CompetitorManager.Instance.OnProgrammeMaterialSearch += WriteProgrammeMaterial;
			if (System.Linq.Enumerable.Contains(SimulationArguments.Get(), "--material-funnel-audit")) {
				materialFunnelWriter = CreateWriter(Path.Combine(directory, $"{runName}-material-funnel.csv"));
				materialFunnelWriter.WriteLine("week,slotId,labelId,artistId,career,genre,ability,craft,pool,draws,unique,accessible,owned,future,held,placement,license,fitRefused,positiveProfessional,positiveCover,baseline,bestProfessionalNet,bestCoverNet,selected,started,startBlock,selectedSongId,blockingSlots");
				CompetitorManager.Instance.OnMaterialFunnelAudit += WriteMaterialFunnel;
			}
		}
		if (LabelReleaseProgramme.WorkBudgetSingles > 0) {
			programmeCashShortfallWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-cash-shortfalls.csv"));
			programmeCashShortfallWriter.WriteLine("week,labelId,tier,status,slotId,artistId,format,cash,overhead,negativeFlowHold,plantPayables,taxAndService,otherProgrammeHolds,production,marketing,pressing,receivables,factored,taxPayable,unprovidedTax,commercialHold");
			CompetitorManager.Instance.OnProgrammeCashShortfall += WriteProgrammeCashShortfall;
		}
		if (LabelReleaseProgramme.LpAssemblyEnabled) {
			lpAssemblyWriter = CreateWriter(Path.Combine(directory, $"{runName}-lp-assembly.csv"));
			lpAssemblyWriter.WriteLine("week,year,slotId,labelId,tier,artistId,careerState,kind,albumFormat,recordId,trackCount,releasedMasters,shelvedMasters,newSessions,relevantHits,controlledSingles,productionMultiplier,paidProduction,projectedNet,ordinaryNet,hitLedFeasible,hitLedNet,greatestHitsFeasible,greatestHitsNet,reason,appealScale,openingMasters,packageSpacingBlocked");
			CompetitorManager.Instance.OnLpAssembly += WriteLpAssembly;
		}
	}
	private void WriteMaterialFunnel(MaterialFunnelAudit a) {
		string R(float n) => float.IsFinite(n) ? n.ToString("R", CultureInfo.InvariantCulture) : "";
		materialFunnelWriter.WriteLine(string.Join(",", a.Week, Csv(a.Item.Id), Csv(a.Item.LabelId), Csv(a.Item.ArtistId), a.Artist.careerState,
			Csv(a.Genre), R(a.Artist.songwritingAbility), R(SongMaterialSelectionService.ArtistWriterCraft(a.Artist)), a.Pool, a.Draws, a.Unique,
			a.Accessible, a.Owned, a.Future, a.Held, a.Placement, a.License, a.FitRefused, a.PositiveProfessional, a.PositiveCover,
			R(a.Baseline), R(a.BestProfessionalNet), R(a.BestCoverNet), a.Selected, a.Started ? 1 : 0, Csv(a.StartBlock), Csv(a.SelectedSongId), Csv(a.BlockingSlots)));
	}
	private void WriteLpAssembly(ReleaseSlateItem item, AILabel label, int week) {
		var p = item.LpAssembly;
		var artist = label.roster.Find(a => a.artistId == item.ArtistId);
		string R(float n) => n.ToString("R", CultureInfo.InvariantCulture);
		lpAssemblyWriter.WriteLine(string.Join(",", week, item.IntendedDate.year, Csv(item.Id), Csv(label.labelId), label.tier, Csv(item.ArtistId),
			artist?.careerState.ToString() ?? "", p.Kind, item.PreparedRecord?.album?.albumFormat.ToString() ?? "", Csv(item.RecordId), p.TrackCount,
			p.ReleasedMasterIds.Count, p.ShelvedMasterIds.Count, p.NewSessions, p.RelevantHits, p.ControlledSingles, R(p.ProductionMultiplier),
			R(item.PaidProduction), R(p.ProjectedNet), R(p.OrdinaryNet), p.HitLedFeasible ? 1 : 0, R(p.HitLedNet), p.GreatestHitsFeasible ? 1 : 0,
			R(p.GreatestHitsNet), Csv(p.Reason), R(p.AppealScale), p.OpeningMasters, p.PackageSpacingBlocked ? 1 : 0));
	}
	private void WriteProgrammeMaterial(ReleaseSlateItem item, int week) {
		var s = item.MaterialSearch;
		string R(float n) => n.ToString("R", CultureInfo.InvariantCulture);
		programmeMaterialWriter.WriteLine(string.Join(",", week, Csv(item.Id), Csv(item.LabelId), Csv(item.ArtistId), s.State, Csv(s.Reason),
			s.Professional ? 1 : 0, Csv(s.SongId), Csv(s.SupplierId), s.StartedWeek, s.DeliveryWeek, s.HoldUntilWeek,
			R(s.PaidFee), R(s.ProposalRead), R(s.BaselineRead), R(s.ExpectedBenefit), Csv(s.ReferenceMasterId), Csv(item.RecordId)));
	}
	private void WriteProgrammeProject(ReleaseSlateItem item, int week) {
		var p = item.ProjectReview;
		programmeProjectWriter.WriteLine(string.Join(",", week, Csv(item.Id), Csv(item.LabelId), Csv(item.ArtistId), Csv(item.Source),
			Csv(p.CandidateId), Csv(p.Action), p.Perceived.ToString("R", CultureInfo.InvariantCulture),
			p.RoutineRead.ToString("R", CultureInfo.InvariantCulture), p.UseSearchedMaterial ? 1 : 0,
			p.Uncertainty.ToString("R", CultureInfo.InvariantCulture),
			p.Action == CompetitorManager.OwnSessionAction ? (p.Week < item.IntendedWeek + LabelReleaseProgramme.Horizon - LabelReleaseProgramme.PreparationWeeks ? "follow-up-due" : "window-close") : ""));
	}
	private void WriteProgrammeResources(ProgrammeResourceTelemetry r) => programmeResourceWriter.WriteLine(string.Join(",", r.Week, r.Year,
		Csv(r.LabelId), Csv(r.Tier), r.Attempts, r.Successes, r.OpenItems, F(r.CashReserved), F(r.PromotionPool), F(r.PromotionBooked), F(r.SingleWeeklyRate), F(r.AlbumWeeklyRate), r.DueFollowUps, r.UnplannedFollowUps, F(r.PromotionCommittedPeak),
		r.RosterActs, r.ReleaseEligibleActs, r.ProgrammeCapacityActive ? 1 : 0, F(r.NominalSingleMonthlyCapacity), F(r.NominalAlbumMonthlyCapacity), F(r.StaffCoverage), F(r.SlotStatusFactor), F(r.SeasonalAvailability)));
	private void WriteProgrammeCashShortfall(ProgrammeCashShortfall r) => programmeCashShortfallWriter.WriteLine(string.Join(",", r.Week, Csv(r.LabelId), r.Tier, r.Status,
		Csv(r.SlotId), Csv(r.ArtistId), r.Format, F(r.Cash), F(r.Overhead), F(r.NegativeFlowHold), F(r.PlantPayables), F(r.TaxAndService), F(r.OtherProgrammeHolds),
		F(r.Production), F(r.Marketing), F(r.Pressing), F(r.Receivables), F(r.Factored), F(r.TaxPayable), F(r.UnprovidedTax), F(r.CommercialHold)));
	private void WriteProgrammeChoice(ProgrammeChoiceAudit r) {
		var statureLabel = statureDecisionWriter == null ? null : CompetitorManager.Instance.GetAllLabels().FirstOrDefault(x => x != null && x.labelId == r.LabelId);
		void Row(ProgrammeCandidateAudit a) {
			string N(int n) => n.ToString(CultureInfo.InvariantCulture);
			string Read(float value) => a?.Eligible == true ? value.ToString("R", CultureInfo.InvariantCulture) : "";
			programmeChoiceWriter.WriteLine(string.Join(",", N(r.Week), N(r.Year), Csv(r.LabelId), Csv(r.Tier), Csv(r.Status),
				Csv(r.SlotId), r.Format, N(r.IntendedWeek), N(r.IntendedYear), r.Financed ? "1" : "0",
				Csv(a?.ArtistId), Csv(a?.CareerState), a?.Eligible == true ? "1" : "0", Csv(a?.Reason ?? "empty-roster"),
				a?.Eligible == true ? N(a.Rank) : "", a?.Selected == true ? "1" : "0", Read(a?.Score ?? 0),
				a?.Eligible == true ? (a.Hot ? "1" : "0") : "", a?.Eligible == true ? (a.Due ? "1" : "0") : "",
				a?.Eligible == true ? N(a.Gap) : "", Read(a?.ExpectedNet ?? 0), Read(a?.Contribution ?? 0),
				Read(a?.QualityEstimate ?? 0), Read(a?.Priority ?? 0), Read(a?.Momentum ?? 0), Read(a?.Obligations ?? 0),
				a?.Eligible == true ? a.AttachedAudienceAtAudit.ToString("R", CultureInfo.InvariantCulture) : "",
				a?.Eligible == true ? (a.FollowUpScheduling ? "1" : "0") : "", Read(a?.WaitingPriority ?? 0),
				Read(a?.AudienceOrders ?? 0), Read(a?.NationalAudienceOrders ?? 0)));
			if (statureDecisionWriter != null && r.Format == ReleaseFormat.Album && a != null) {
				var gameDate = TimeManager.Instance.CurrentDate;
				var decisionDate = new System.DateTime(gameDate.year, gameDate.month, gameDate.day);
				var parsed = System.Enum.TryParse(a.CareerState, out CareerState current);
				var actualArtist = statureLabel?.roster?.FirstOrDefault(x => x != null && x.artistId == a.ArtistId);
				StatureWeeklyShadow.Snapshot snapshot = null;
				if (!string.IsNullOrEmpty(a.ArtistId)) MediaAtmosphereService.ExperimentalStatureShadow().Snapshots.TryGetValue(a.ArtistId, out snapshot);
				var read = StatureDecisionSnapshot.Resolve(snapshot, decisionDate, current,
					statureLabel?.isPlayerOwned == true || actualArtist?.isPlayerOwned == true,
					actualArtist?.lifecycleStatus ?? ArtistLifecycleStatus.Inactive);
				bool eligible = a.Eligible && parsed && actualArtist != null && statureLabel != null;
				bool live = ChartManager.Instance?.IsGenreMarketV2Live == true || AlbumPriorRecalibration.IsLive;
				float old = eligible ? CompetitorManager.GetAlbumPriorCareerMultiplier(current, live) : 0;
				string proposed = eligible && read.Status == StatureDecisionSnapshot.Status.Available
					? F(CompetitorManager.GetAlbumPriorCareerMultiplier(read.CareerState, live)) : "";
				statureDecisionWriter.WriteLine(string.Join(",", Csv(gameDate.ToShortString()), N(r.Week), Csv(r.LabelId), Csv(r.Tier), Csv(r.SlotId),
					Csv(a.ArtistId), eligible ? "1" : "0", a.Selected ? "1" : "0", Csv(a.CareerState),
					actualArtist?.careerStateBeforeDrop.ToString() ?? "", statureLabel != null ? F(statureLabel.cashReserves) : "",
					snapshot?.Date.ToString("yyyy-MM-dd") ?? "", snapshot?.Band.ToString() ?? "", eligible ? read.Status.ToString() : "Ineligible",
					eligible ? F(old) : "", proposed, eligible ? F(old) : "", eligible ? F(a.ExpectedNet) : ""));
			}
		}
		if (r.Candidates.Count == 0) Row(null);
		else foreach (var candidate in r.Candidates) Row(candidate);
	}
	private void WriteReleaseSlate(ReleaseSlateItem item, int week) {
		string N(int n) => n.ToString(CultureInfo.InvariantCulture);
		releaseSlateWriter.WriteLine(string.Join(",", N(week), Csv(item.Id), Csv(item.LabelId), Csv(item.ArtistId), item.Format, item.State,
			Csv(item.Source), N(item.IntendedWeek), N(item.DueWeek), N(item.ReadyWeek), N(item.ActualWeek), Csv(item.IntendedDate.ToString()),
			Csv(item.ActualWeek > 0 ? item.ActualDate.ToString() : ""), Csv(item.RecordId), F(item.ReservedProduction), F(item.ReservedMarketing),
			F(item.ReservedPressing), F(item.Score), Csv(item.Evidence), Csv(item.Reason), item.MissedFollowUp ? "1" : "0", F(item.PaidProduction),
			item.PreparedWeek.HasValue ? N(item.PreparedWeek.Value) : "", N(item.ProposedWeek), "defer-failed-work"));
	}
	private void WriteProgrammeIssueRefusal(ReleaseSlateItem item, int week, string gate) =>
		programmeIssueRefusalWriter.WriteLine(string.Join(",", week, Csv(item.Id), Csv(item.LabelId), Csv(item.ArtistId), item.Format,
			Csv(item.Source), item.IntendedWeek, item.PaidProduction.ToString("R", CultureInfo.InvariantCulture), Csv(gate)));
	private void CloseReleaseProgrammeOutputs() {
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnMaterialFunnelAudit -= WriteMaterialFunnel;
		materialFunnelWriter?.Dispose(); materialFunnelWriter = null;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeIssueRefused -= WriteProgrammeIssueRefusal;
		programmeIssueRefusalWriter?.Dispose(); programmeIssueRefusalWriter = null;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeProjectReview -= WriteProgrammeProject;
		programmeProjectWriter?.Dispose(); programmeProjectWriter = null;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnReleaseSlateReview -= WriteReleaseSlate;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeResources -= WriteProgrammeResources;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeMaterialSearch -= WriteProgrammeMaterial;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnLpAssembly -= WriteLpAssembly;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeCashShortfall -= WriteProgrammeCashShortfall;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeChoiceReview -= WriteProgrammeChoice;
		releaseSlateWriter?.Dispose(); releaseSlateWriter = null;
		programmeResourceWriter?.Dispose(); programmeResourceWriter = null;
		programmeMaterialWriter?.Dispose(); programmeMaterialWriter = null;
		lpAssemblyWriter?.Dispose(); lpAssemblyWriter = null;
		programmeCashShortfallWriter?.Dispose(); programmeCashShortfallWriter = null;
		programmeChoiceWriter?.Dispose(); programmeChoiceWriter = null;
	}
}
