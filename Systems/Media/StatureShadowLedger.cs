using System;
using System.Collections.Generic;

/// <summary>
/// Reusable weekly shadow state. The caller supplies completed-week evidence and
/// owns collection, save integration and a version uniquely identifying the
/// calibration contents. This type has no readers
/// in the game yet. A missing result retains the last determinate band.
/// </summary>
public sealed class StatureShadowLedger {
	public sealed class ArtistState {
		public StatureShadowPolicy.Band PriorBand { get; set; }
		public StatureShadowPolicy.ResultStatus Status { get; set; }
		public DateTime ObservationDate { get; set; }
	}

	public DateTime LastProcessedDate { get; set; }
	public string CalibrationVersion { get; set; }
	public Dictionary<string, ArtistState> Artists { get; set; } = new(StringComparer.Ordinal);

	/// <returns>False for an already-published week; true when a new week was applied.</returns>
	public bool Advance(DateTime date, IReadOnlyDictionary<string, StatureShadowPolicy.Route[]> evidence,
		StatureShadowPolicy.Calibration calibration) {
		date = date.Date;
		if (date == default || evidence == null) throw new ArgumentException("A date and evidence are required.");
		StatureShadowPolicy.Validate(calibration);
		if (date < calibration.EffectiveDate.Date) throw new ArgumentException("Calibration is not yet effective.");
		if (LastProcessedDate != default && !string.Equals(CalibrationVersion, calibration.Version, StringComparison.Ordinal))
			throw new InvalidOperationException("A published ledger cannot silently change calibration.");
		if (date == LastProcessedDate.Date) return false;
		if (LastProcessedDate != default && (date - LastProcessedDate.Date).Days != 7)
			throw new InvalidOperationException("Weeks must advance exactly once in chronological order.");
		if (Artists == null) throw new InvalidOperationException("Missing ledger state.");
		// Prepare all results before mutating the ledger, so bad input cannot
		// publish a partial week.
		var prepared = new Dictionary<string, ArtistState>(StringComparer.Ordinal);
		foreach (var pair in evidence) {
			if (string.IsNullOrEmpty(pair.Key)) throw new ArgumentException("An artist ID is required.");
			Artists.TryGetValue(pair.Key, out ArtistState old);
			var prior = old?.PriorBand ?? StatureShadowPolicy.Band.BelowRising;
			var result = StatureShadowPolicy.Evaluate(pair.Value, calibration, date, prior);
			prepared.Add(pair.Key, new ArtistState {
				PriorBand = result.Status == StatureShadowPolicy.ResultStatus.MissingEvidence ? prior : result.Band,
				Status = result.Status, ObservationDate = date
			});
		}
		foreach (var pair in prepared) Artists[pair.Key] = pair.Value;
		CalibrationVersion = calibration.Version;
		LastProcessedDate = date;
		return true;
	}
}
