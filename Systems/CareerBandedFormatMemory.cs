using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit A3: learn format residuals from releases priced for the same career band as today's decision.
/// --career-banded-format-memory is off by default and requires the live genre market. The original lane
/// dictionaries remain intact; observations carry an optional ex-ante band and reads filter within a lane.
/// Cohorts 0..3 match the four prior bands; cohort 4 keeps legacy fallback states such as Declining separate.
/// Untagged old-save evidence is excluded on this route until a fresh revision can tag it from immutable
/// launch/project context. No evidence means zero confidence in the correction, leaving the prior in charge.
/// New vs proven LP market economics and the album prior's stature coefficients are separate A3 work.
/// </summary>
public static class CareerBandedFormatMemory {
	public const string Switch = "--career-banded-format-memory";
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static bool HasState => enabled || HasTaggedEvidence(CompetitorManager.Instance?.GetAllLabels());

	internal static bool HasTaggedEvidence(IEnumerable<AILabel> labels) =>
		(labels ?? Enumerable.Empty<AILabel>()).Any(label => label?.laneRevenueMemory?.Values.Any(
			memory => memory?.observations?.Any(observation => observation?.careerBand != null) == true) == true);

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (enabled && !GenreMarketV2.Enabled)
			throw new ArgumentException(Switch + " requires the live genre market.");
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }
}
