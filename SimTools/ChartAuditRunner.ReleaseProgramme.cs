using System.IO;
using System.Globalization;

public partial class ChartAuditRunner {
	private StreamWriter releaseSlateWriter;
	private StreamWriter programmeResourceWriter;
	private StreamWriter programmeMaterialWriter;
	private StreamWriter lpAssemblyWriter;
	private StreamWriter programmeCashShortfallWriter;
	private void OpenReleaseProgrammeOutputs(string directory) {
		if (!LabelReleaseProgramme.Enabled) return;
		releaseSlateWriter = CreateWriter(Path.Combine(directory, $"{runName}-release-slate.csv"));
		releaseSlateWriter.WriteLine("week,slotId,labelId,artistId,format,state,source,intendedWeek,dueWeek,readyWeek,actualWeek,intendedDate,actualDate,recordId,reservedProduction,reservedMarketing,reservedPressing,score,evidence,reason,missedFollowUp,paidProduction,preparedWeek,proposedWeek,weeklyReviewPolicy");
		CompetitorManager.Instance.OnReleaseSlateReview += WriteReleaseSlate;
		programmeResourceWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-resources.csv"));
		programmeResourceWriter.WriteLine("week,year,labelId,tier,attempts,successes,openItems,cashReserved,promotionPool,promotionBooked,singleWeeklyRate,albumWeeklyRate,dueFollowUps,unplannedFollowUps,promotionCommittedPeak");
		CompetitorManager.Instance.OnProgrammeResources += WriteProgrammeResources;
		if (LabelReleaseProgramme.MaterialSearchEnabled) {
			programmeMaterialWriter = CreateWriter(Path.Combine(directory, $"{runName}-programme-material-search.csv"));
			programmeMaterialWriter.WriteLine("week,slotId,labelId,artistId,state,reason,professional,songId,supplierId,startedWeek,deliveryWeek,holdUntilWeek,paidFee,proposalRead,baselineRead,expectedBenefit,referenceMasterId,recordId");
			CompetitorManager.Instance.OnProgrammeMaterialSearch += WriteProgrammeMaterial;
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
	private void WriteProgrammeResources(ProgrammeResourceTelemetry r) => programmeResourceWriter.WriteLine(string.Join(",", r.Week, r.Year,
		Csv(r.LabelId), Csv(r.Tier), r.Attempts, r.Successes, r.OpenItems, F(r.CashReserved), F(r.PromotionPool), F(r.PromotionBooked), F(r.SingleWeeklyRate), F(r.AlbumWeeklyRate), r.DueFollowUps, r.UnplannedFollowUps, F(r.PromotionCommittedPeak)));
	private void WriteProgrammeCashShortfall(ProgrammeCashShortfall r) => programmeCashShortfallWriter.WriteLine(string.Join(",", r.Week, Csv(r.LabelId), r.Tier, r.Status,
		Csv(r.SlotId), Csv(r.ArtistId), r.Format, F(r.Cash), F(r.Overhead), F(r.NegativeFlowHold), F(r.PlantPayables), F(r.TaxAndService), F(r.OtherProgrammeHolds),
		F(r.Production), F(r.Marketing), F(r.Pressing), F(r.Receivables), F(r.Factored), F(r.TaxPayable), F(r.UnprovidedTax), F(r.CommercialHold)));
	private void WriteReleaseSlate(ReleaseSlateItem item, int week) {
		string N(int n) => n.ToString(CultureInfo.InvariantCulture);
		releaseSlateWriter.WriteLine(string.Join(",", N(week), Csv(item.Id), Csv(item.LabelId), Csv(item.ArtistId), item.Format, item.State,
			Csv(item.Source), N(item.IntendedWeek), N(item.DueWeek), N(item.ReadyWeek), N(item.ActualWeek), Csv(item.IntendedDate.ToString()),
			Csv(item.ActualWeek > 0 ? item.ActualDate.ToString() : ""), Csv(item.RecordId), F(item.ReservedProduction), F(item.ReservedMarketing),
			F(item.ReservedPressing), F(item.Score), Csv(item.Evidence), Csv(item.Reason), item.MissedFollowUp ? "1" : "0", F(item.PaidProduction),
			item.PreparedWeek.HasValue ? N(item.PreparedWeek.Value) : "", N(item.ProposedWeek), "defer-failed-work"));
	}
	private void CloseReleaseProgrammeOutputs() {
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnReleaseSlateReview -= WriteReleaseSlate;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeResources -= WriteProgrammeResources;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeMaterialSearch -= WriteProgrammeMaterial;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnLpAssembly -= WriteLpAssembly;
		if (CompetitorManager.Instance != null) CompetitorManager.Instance.OnProgrammeCashShortfall -= WriteProgrammeCashShortfall;
		releaseSlateWriter?.Dispose(); releaseSlateWriter = null;
		programmeResourceWriter?.Dispose(); programmeResourceWriter = null;
		programmeMaterialWriter?.Dispose(); programmeMaterialWriter = null;
		lpAssemblyWriter?.Dispose(); lpAssemblyWriter = null;
		programmeCashShortfallWriter?.Dispose(); programmeCashShortfallWriter = null;
	}
}
