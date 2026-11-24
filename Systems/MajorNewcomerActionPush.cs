using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (LpGenreAllocationFollowup.md §55), off unless named.
/// --major-newcomer-action-push=F: promotion follows action. The weekly label push on a live AI single by an act that was
/// NewSigning at launch, at a Major-tier label, reads <c>Ref + F x (push - Ref)</c> (<see cref="ImpactRef"/> = the Independent
/// campaign impact) until the record shows action: any week on the Hot 100, or a regional breakout. From then on it reads
/// the label's full push. The release-age taper, the top-20 hold and the momentum floor apply on top as before.
///
/// History (§55): the Big-5 issued ~1,080 singles in 1960 (LabelReleaseProgrammeImplementation.md) and placed ~4.5 newcomer
/// records a year in the 1961-64 Top 10; the sim's Big-5 issue ~460 and place ~12. Their promotion staff and distributors'
/// promo men were finite (HistoricalLabelTierBalanceResearch.md), so most of those singles were released and shipped
/// but worked only once a market reported action. §51 cut the newcomer launch package (launch impact, first-run stock)
/// and found it inert: newcomers enter about six weeks after release, under the weekly push this probe gates.
/// Distribution, coverage, stock, launch terms, established acts, albums, other tiers and the player are untouched.
/// No random number is drawn. Off: <see cref="Apply"/> returns its input.
/// </summary>
public static class MajorNewcomerActionPush {
	public const string SwitchPrefix = "--major-newcomer-action-push=";
	public const float ImpactRef = 0.36f;
	private static bool configured;
	private static float factor = 1f;
	public static float Factor => factor;
	public static bool Enabled => factor < 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		factor = 1f;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + SwitchPrefix.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			if (!float.TryParse(values[0].Substring(SwitchPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
				!(f >= 0f && f < 1f))
				throw new ArgumentException(SwitchPrefix.TrimEnd('=') + " needs a factor in [0, 1).");
			factor = f;
			GD.Print(FormattableString.Invariant($"MAJOR_NEWCOMER_ACTION_PUSH factor={factor:F2} ref={ImpactRef:F2}"));
		}
		configured = true;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }
	internal static void SetForChecks(float f) { configured = true; factor = f; }

	/// <summary>A Major's newcomer single that has not yet shown action.</summary>
	public static bool Waiting(RecordRuntimeData record, AILabel label) =>
		record?.baseRecord != null && record.baseRecord.format == ReleaseFormat.Single && label != null && !label.isPlayerOwned &&
		label.tier == LabelTier.Major && record.launchCareerState == CareerState.NewSigning &&
		record.weeksOnChart == 0 && record.currentPosition <= 0 && record.regionalBreakoutCount == 0;

	/// <summary>The weekly push base; <paramref name="push"/> unchanged when off, out of scope, after action, or at or below the reference.</summary>
	public static float Apply(float push, RecordRuntimeData record, AILabel label) {
		EnsureConfigured();
		if (factor >= 1f || push <= ImpactRef || !Waiting(record, label)) return push;
		return ImpactRef + factor * (push - ImpactRef);
	}
}
