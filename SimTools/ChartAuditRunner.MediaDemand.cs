using System.IO;

public partial class ChartAuditRunner {
	private StreamWriter mediaDemandWriter;

	/// <summary>Per-title rows of the media singles adapter (MediaDemand, directive §12.3 / §15.3), shadow or causal.</summary>
	private void OpenMediaDemandOutputs(string directory) {
		if (!MediaDemand.Active) return;
		mediaDemandWriter = CreateWriter(Path.Combine(directory, $"{runName}-media-demand.csv"));
		MediaDemand.Log = mediaDemandWriter;
	}

	private void CloseMediaDemandOutputs() {
		if (mediaDemandWriter == null) return;
		MediaDemand.Log = null;
		mediaDemandWriter.Dispose(); mediaDemandWriter = null;
	}
}
