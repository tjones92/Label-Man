using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private StreamWriter mediaDemandWriter, followingWriter, statureEvidenceWriter, statureShadowWriter, statureDecisionWriter;

	/// <summary>Per-title rows of the media singles adapter (MediaDemand, directive §12.3 / §15.3), shadow or causal.</summary>
	private void OpenMediaDemandOutputs(string directory) {
		var shadowArgs = OS.GetCmdlineUserArgs().Where(x => x.StartsWith(StatureObservation.ShadowSwitch, System.StringComparison.Ordinal)).ToArray();
		if (shadowArgs.Length > 1) throw new System.InvalidOperationException("Specify one stature shadow calibration.");
		MediaAtmosphereService.PersistExperimentalStature = OS.GetCmdlineUserArgs().Contains("--persist-stature-shadow");
		if (MediaAtmosphereService.PersistExperimentalStature && shadowArgs.Length == 0)
			throw new System.InvalidOperationException("Persisting stature requires a shadow calibration.");
		if (shadowArgs.Length == 1) {
			if (!MediaFollowing.Active) throw new System.InvalidOperationException("Stature shadow requires active media following.");
			if (!OS.GetCmdlineUserArgs().Contains("--label-decision-audit")) throw new System.InvalidOperationException("Stature shadow requires programme decision auditing.");
			string path = shadowArgs[0].Substring(StatureObservation.ShadowSwitch.Length);
			StatureObservation.ConfigureShadow(path, MediaAtmosphereService.PersistExperimentalStature);
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

	private void CloseMediaDemandOutputs() {
		statureDecisionWriter?.Dispose(); statureDecisionWriter = null;
		if (statureShadowWriter != null) { StatureObservation.ShadowLog = null; StatureObservation.ShadowConfig = null; statureShadowWriter.Dispose(); statureShadowWriter = null; }
		MediaAtmosphereService.PersistExperimentalStature = false;
		if (statureEvidenceWriter != null) { StatureObservation.Log = null; statureEvidenceWriter.Dispose(); statureEvidenceWriter = null; }
		if (followingWriter != null) { MediaFollowing.Log = null; followingWriter.Dispose(); followingWriter = null; }
		if (mediaDemandWriter == null) return;
		MediaDemand.Log = null;
		mediaDemandWriter.Dispose(); mediaDemandWriter = null;
	}
}
