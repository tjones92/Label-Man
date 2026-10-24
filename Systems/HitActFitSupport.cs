using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Opt-in downside-only commercial response probe. A recent Top 10 earns a bounded national response
/// floor on a later AI single. It does not change the connected upside, regional response draws, material,
/// campaign, release budget or consumer purse. The floor is captured before the single can chart and saved
/// with that record: its own success, later hits and ownership transfers cannot refresh its support.
/// </summary>
public static class HitActFitSupport {
	public const string FloorPrefix = "--hit-act-fit-floor=";
	public const int HalfLifeWeeks = 26;
	public const int WindowWeeks = 52;
	private static bool configured;
	public static bool Enabled { get; private set; }
	public static float PeakFloor { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(FloorPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + FloorPrefix + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0][FloorPrefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float floor)
				|| !float.IsFinite(floor) || floor < SingleCommercialFit.Floor || floor > 1f)
				throw new ArgumentException(FloorPrefix + " needs a floor between the base commercial floor and 1.");
			if (!SingleCommercialFit.Enabled || !HitActHype.Enabled)
				throw new ArgumentException(FloorPrefix + " requires commercial fit and hit-act hype (the saved Top 10 clock).");
			Enabled = true;
			PeakFloor = floor;
		}
		configured = true;
	}

	internal static void SetForChecks(bool enabled, float floor = 1f) {
		configured = true; Enabled = enabled; PeakFloor = floor;
	}

	internal static float Recency(SimulatedArtist artist, int week) {
		if (artist == null) return 0f;
		int last = artist.hypeLastTop10Week;
		// Only historical opening evidence is inferred; a loaded active record never gets a retrospective snapshot.
		if (last == HitActHype.Unset) {
			int year = artist.publicHistory?.Where(e => e.year < 1960 && e.peakPosition is > 0 and <= 10)
				.Select(e => e.year).DefaultIfEmpty(0).Max() ?? 0;
			if (year == 0) return 0f;
			last = (year - 1960) * 52 + 26;
		}
		if (last == HitActHype.Unset + 1 || last > week) return 0f;
		long age = (long)week - last;
		return age > WindowWeeks ? 0f : (float)Math.Pow(.5d, age / (double)HalfLifeWeeks);
	}

	private static bool Eligible(RecordRuntimeData record) => record?.baseRecord != null &&
		record.baseRecord.format == ReleaseFormat.Single && !record.baseRecord.isPlayerOwned &&
		record.baseRecord.releaseDate > GameDate.StartDate;

	public static void CaptureLaunch(RecordRuntimeData record, SimulatedArtist artist, int week) {
		if (!Enabled || !Eligible(record) || week <= 0 || record.hitActFitFloorAtLaunch.HasValue) return;
		float recency = Recency(artist, week);
		record.hitActFitFloorAtLaunch = recency > 0f
			? SingleCommercialFit.Floor + Math.Max(0f, PeakFloor - SingleCommercialFit.Floor) * recency : 0f;
	}

	internal static double Apply(RecordRuntimeData record, double nationalLog) {
		if (!Enabled || !Eligible(record) || !(record.hitActFitFloorAtLaunch is > 0f)) return nationalLog;
		return Math.Max(nationalLog, Math.Log(record.hitActFitFloorAtLaunch.Value));
	}
}
