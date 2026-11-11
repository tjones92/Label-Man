using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private StreamWriter mediaDemandWriter, followingWriter, statureEvidenceWriter, statureShadowWriter, statureDecisionWriter, statureMarketingWriter;
	private StreamWriter openingCensusWriter, openingShadowWriter, openingMarketingWriter;
	private StreamWriter openingReleasesWriter, openingGeographyWriter;

	/// <summary>Per-title rows of the media singles adapter (MediaDemand, directive §12.3 / §15.3), shadow or causal.</summary>
	private void OpenMediaDemandOutputs(string directory) {
		string[] tvOverrides = OS.GetCmdlineUserArgs().Where(arg => arg.StartsWith(MediaDemandTrace.OverridePrefix, System.StringComparison.Ordinal)).ToArray();
		if (tvOverrides.Length > 0) {
			if (tvOverrides.Length != 1 || !MediaDemand.Causal || !OS.GetCmdlineUserArgs().Contains(MediaDemandTrace.Switch))
				throw new System.InvalidOperationException("Use one TV allocation override with causal media demand and booking observation.");
			MediaDemandTrace.ConfigureTvOverride(tvOverrides[0][MediaDemandTrace.OverridePrefix.Length..]);
		}
		if (OS.GetCmdlineUserArgs().Contains(MediaDemandTrace.Switch)) {
			if (!MediaDemand.Active) throw new System.InvalidOperationException("Media booking observation requires active media demand.");
			MediaDemandTrace.Open(
				CreateWriter(Path.Combine(directory, $"{runName}-media-allocations.csv")),
				CreateWriter(Path.Combine(directory, $"{runName}-media-booking-attempts.csv")),
				CreateWriter(Path.Combine(directory, $"{runName}-media-demand-deposits.csv")));
		}
		if (MediaFollowing.ObserveOpeningOrigin) {
			if (StatureObservation.ShadowConfig == null) throw new System.InvalidOperationException("Opening provenance requires a stature calibration.");
			openingCensusWriter = CreateWriter(Path.Combine(directory, $"{runName}-opening-following-census.csv"));
			openingShadowWriter = CreateWriter(Path.Combine(directory, $"{runName}-opening-following-shadow.csv"));
			openingMarketingWriter = CreateWriter(Path.Combine(directory, $"{runName}-opening-following-marketing.csv"));
			OpeningFollowingDiagnostics.Open(openingCensusWriter, openingShadowWriter, openingMarketingWriter);
			if (MediaFollowing.ObserveOpeningGeography) {
				openingReleasesWriter = CreateWriter(Path.Combine(directory, $"{runName}-opening-geography-releases.csv"));
				openingGeographyWriter = CreateWriter(Path.Combine(directory, $"{runName}-opening-geography-regions.csv"));
				OpeningGeographyDiagnostics.Open(openingReleasesWriter, openingGeographyWriter);
			}
		}
		if (ArtistStature.MarketingReader || OS.GetCmdlineUserArgs().Contains("--observe-stature-marketing")) {
			statureMarketingWriter = CreateWriter(Path.Combine(directory, $"{runName}-stature-marketing.csv"));
			statureMarketingWriter.WriteLine("date,labelId,artistId,context,snapshotDate,status,legacyCareer,standingCareer,legacyBudget,requestedBudget,proposedBudget");
			ArtistStature.OnMarketingRead += WriteStatureMarketingRead;
		}
		if (StatureObservation.ShadowConfig != null) {
			statureShadowWriter = CreateWriter(Path.Combine(directory, $"{runName}-stature-shadow.csv"));
			StatureObservation.ShadowLog = statureShadowWriter;
			StatureObservation.WriteShadowHeader(statureShadowWriter);
			statureDecisionWriter = CreateWriter(Path.Combine(directory, $"{runName}-stature-decisions.csv"));
			statureDecisionWriter.WriteLine("date,week,labelId,tier,slotId,artistId,eligible,selected,careerState,beforeDrop,labelCash,snapshotDate,shadowBand,readStatus,oldMultiplier,proposedMultiplier,appliedMultiplier,expectedNet");
		}
		if (OS.GetCmdlineUserArgs().Contains(StatureObservation.Switch)) {
			if (!MediaFollowing.Active) throw new System.InvalidOperationException("Stature evidence requires active media following observation.");
			statureEvidenceWriter = CreateWriter(Path.Combine(directory, $"{runName}-stature-evidence.csv"));
			StatureObservation.Log = statureEvidenceWriter;
			StatureObservation.WriteHeader(statureEvidenceWriter);
		}
		if (MediaFollowing.Active) {
			followingWriter = CreateWriter(Path.Combine(directory, $"{runName}-following.csv"));
			MediaFollowing.Log = followingWriter;
		}
		if (!MediaDemand.Active) return;
		mediaDemandWriter = CreateWriter(Path.Combine(directory, $"{runName}-media-demand.csv"));
		MediaDemand.Log = mediaDemandWriter;
	}

	private void WriteStatureMarketingRead(ArtistStature.MarketingRead read) => statureMarketingWriter?.WriteLine(string.Join(",",
		Csv(read.DecisionDate.ToShortString()), Csv(read.LabelId), Csv(read.ArtistId), Csv(read.Context),
		read.SnapshotDate == default ? "" : read.SnapshotDate.ToString("yyyy-MM-dd"), read.Status,
		read.LegacyCareer, read.StandingCareer?.ToString() ?? "", F(read.LegacyBudget), F(read.Budget), F(read.ProposedBudget)));

	private void CloseMediaDemandOutputs() {
		MediaDemandTrace.Close();
		OpeningFollowingDiagnostics.Close();
		OpeningGeographyDiagnostics.Close();
		openingReleasesWriter?.Dispose(); openingReleasesWriter = null;
		openingGeographyWriter?.Dispose(); openingGeographyWriter = null;
		openingCensusWriter?.Dispose(); openingCensusWriter = null;
		openingShadowWriter?.Dispose(); openingShadowWriter = null;
		openingMarketingWriter?.Dispose(); openingMarketingWriter = null;
		ArtistStature.OnMarketingRead -= WriteStatureMarketingRead;
		statureMarketingWriter?.Dispose(); statureMarketingWriter = null;
		statureDecisionWriter?.Dispose(); statureDecisionWriter = null;
		if (statureShadowWriter != null) { StatureObservation.ShadowLog = null; StatureObservation.ShadowConfig = null; statureShadowWriter.Dispose(); statureShadowWriter = null; }
		ArtistStature.ProgrammeAlbumReader = false;
		ArtistStature.CareerDisplay = false;
		ArtistStature.MarketingReader = false;
		MediaAtmosphereService.PersistExperimentalStature = false;
		if (statureEvidenceWriter != null) { StatureObservation.Log = null; statureEvidenceWriter.Dispose(); statureEvidenceWriter = null; }
		if (followingWriter != null) { MediaFollowing.Log = null; followingWriter.Dispose(); followingWriter = null; }
		if (mediaDemandWriter == null) return;
		MediaDemand.Log = null;
		mediaDemandWriter.Dispose(); mediaDemandWriter = null;
	}
}
