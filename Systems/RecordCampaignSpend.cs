using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit B5: a record's own campaign money moves its radio push, and a label can work a breaking
/// record harder or let a dead one go.
///
/// Off (the default), weekly radio push is GetCampaignImpact(label), a fixed function of the label's
/// marketingPower and budgetLevel, identical for every record the label releases; the launch budget
/// reaches only the launch awareness, through a log normalized by tier. A star's 2x budget and a
/// breaking record are pushed exactly like a new act's flop, so cash has almost no use.
///
/// When on, for AI singles:
/// - Launch and weekly push carry a per-record factor, sqrt(budget / the label's standard budget),
///   clamped to [0.6, 1.6]. The standard budget is the label's own NewSigning-to-Rising budget
///   (artist multiplier 1), so the label trait still sets the level and the mix stays near 1.
/// - From week 2 to 12, a single inside the Top 40 and not falling earns a follow-up tranche of half
///   the act's budget (at most two), paid from cash above three months' overhead and attributed to
///   the record's marketing cost. Each raises the factor by 1.3x, capped at 2.
/// - A single that has not charted by week 6 is cut: the factor drops to 0.6x of its value, once.
///
/// The cross-tier launch normalization is deliberately unchanged: putting every tier on one absolute
/// spend scale moves the Major launch edge, which the head-of-chart calibration is most sensitive to.
/// That is a decade A/B question, not part of this switch.
/// </summary>
public static class RecordCampaignSpend {
	public const string Switch = "--record-campaign-spend";
	internal const float MinFactor = .6f, MaxLaunchFactor = 1.6f, MaxFactor = 2f;
	internal const float TrancheShare = .5f, TrancheLift = 1.3f, CutFactor = .6f, ReserveMonths = 3f;
	internal const int MaxTranches = 2, FirstFollowUpWeek = 2, LastFollowUpWeek = 12, CutWeek = 6;
	private static bool configured, enabled;
	public static bool Enabled => enabled;
	public static bool HasState => enabled || ChartManager.Instance?.GetAllRecords().Any(r => r.campaignPushFactor.HasValue) == true;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetForChecks(bool value) { configured = true; enabled = value; }

	internal static float LaunchFactor(float budget, float standardBudget) =>
		standardBudget <= 0f ? 1f : Math.Clamp(MathF.Sqrt(Math.Max(0f, budget) / standardBudget), MinFactor, MaxLaunchFactor);

	/// <summary>Weekly push multiplier: 1 for any record without a campaign factor (off, albums, player records).</summary>
	internal static float PushFactor(RecordRuntimeData record) => record?.campaignPushFactor ?? 1f;

	internal static bool IsBreaking(RecordRuntimeData r) =>
		r.weeksSinceRelease >= FirstFollowUpWeek && r.weeksSinceRelease <= LastFollowUpWeek &&
		r.currentPosition >= 1 && r.currentPosition <= 40 &&
		(r.lastWeekPosition == 0 || r.currentPosition <= r.lastWeekPosition);

	internal static bool IsDead(RecordRuntimeData r) => r.weeksSinceRelease >= CutWeek && r.peakPosition == 0;
}

public sealed class RecordCampaignTelemetry {
	public int Week, WeeksSinceRelease, Position;
	/// <summary>FollowUp or Cut.</summary>
	public string Kind;
	public string RecordId, LabelId, ArtistId;
	public float Spend, FactorBefore, FactorAfter, CashAfter;
}
