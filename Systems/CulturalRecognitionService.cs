using System.Collections.Generic;
using Godot;

/// <summary>Where a record's public standing came from. Extended, not replaced, by later systems.</summary>
public enum RecognitionChannel {
	/// <summary>The public answered: it charted, so people heard it.</summary>
	Commercial,
	/// <summary>Other acts and other rooms were talking about it.</summary>
	Word,
	/// <summary>The trade press said so. Nothing writes this yet -- the journalism seam.</summary>
	Press
}

/// <summary>
/// LAYER 2 of the cultural stack: how widely a record's merit is KNOWN.
/// <para>
/// Recognition is mutable and channel-fed, where merit is fixed. Today exactly one channel
/// writes it -- the chart, because a record that charted was self-evidently heard. That is
/// a limitation of what exists, not a statement that commercial success is the only route
/// to public standing, and the whole file is shaped so that it isn't.
/// </para>
/// <para>
/// THE JOURNALISM SEAM. When magazines exist they call <see cref="Deposit"/> with
/// <see cref="RecognitionChannel.Press"/> and a record acquires public standing WITHOUT
/// having charted. Nothing downstream needs editing: the landmark rule in
/// <see cref="CulturalMemoryService"/> reads merit x recognition and does not care which
/// channel supplied the recognition. Adding journalism is adding a caller, not changing a
/// rule -- which is the property this split exists to guarantee.
/// </para>
/// </summary>
public static class CulturalRecognitionService {
	/// <summary>
	/// Deposits are held only until the record's chart run completes and are dropped
	/// wholesale each year. An unbounded per-record table across 41k albums a decade is a
	/// leak; this one cannot outgrow a year's worth of press.
	/// </summary>
	public const int MaxPendingDeposits = 1024;

	private readonly struct Deposit_ {
		public readonly float Amount;
		public readonly RecognitionChannel Channel;
		public Deposit_(float amount, RecognitionChannel channel) { Amount = amount; Channel = channel; }
	}

	private static readonly Dictionary<string, Deposit_> Pending = new();
	private static int pendingYear = int.MinValue;

	/// <summary>How loudly the public answered. 0 for a record that never charted.</summary>
	public static float GetCommercialRecognition(int peakPosition) =>
		peakPosition <= 0 || peakPosition > 100 ? 0f : Mathf.Clamp((101f - peakPosition) / 100f, 0f, 1f);

	/// <summary>
	/// Public standing an outside system has conferred on a record before its chart run
	/// closed. The journalism entry point; nothing calls it yet.
	/// </summary>
	public static void Deposit(string recordId, float amount, RecognitionChannel channel, int year) {
		if (string.IsNullOrEmpty(recordId) || amount <= 0f) return;
		if (pendingYear != year) { Pending.Clear(); pendingYear = year; }
		if (Pending.Count >= MaxPendingDeposits && !Pending.ContainsKey(recordId)) return;
		float existing = Pending.TryGetValue(recordId, out Deposit_ prior) ? prior.Amount : 0f;
		Pending[recordId] = new Deposit_(Mathf.Clamp(existing + amount, 0f, 1f), channel);
	}

	/// <summary>
	/// The record's total public standing at the moment its run closed, and which channel
	/// is most responsible for it. Consumes any pending deposit: recognition is conferred
	/// once, not re-counted on every read.
	/// </summary>
	public static (float Recognition, RecognitionChannel Channel) Consume(string recordId, int peakPosition,
		float artistStanding) {
		float commercial = GetCommercialRecognition(peakPosition);
		// An act the room already respects lends its next record some standing before
		// anyone has heard it. Bounded well below the commercial term: reputation opens
		// the door, it does not carry the record.
		float word = .35f * Mathf.Clamp(artistStanding, 0f, 1f);
		float press = 0f;
		RecognitionChannel channel = commercial >= word ? RecognitionChannel.Commercial : RecognitionChannel.Word;
		if (!string.IsNullOrEmpty(recordId) && Pending.TryGetValue(recordId, out Deposit_ deposit)) {
			Pending.Remove(recordId);
			press = deposit.Amount;
			if (press > commercial && press > word) channel = deposit.Channel;
		}
		// Channels combine as independent chances of having reached someone rather than by
		// addition, so no single channel can saturate public standing on its own.
		float unheard = (1f - commercial) * (1f - word) * (1f - press);
		return (Mathf.Clamp(1f - unheard, 0f, 1f), channel);
	}

	internal static void ResetForProbe() { Pending.Clear(); pendingYear = int.MinValue; }
	internal static int PendingCountForProbe => Pending.Count;
}
