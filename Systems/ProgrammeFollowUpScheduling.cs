using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Experimental follow-up allocation inside single slots; LP intentions and capacity are untouched.</summary>
public static class ProgrammeFollowUpScheduling {
	public const string Switch = "--programme-follow-up-scheduling";
	public const string DeliverySwitch = "--programme-follow-up-delivery";
	public const string MaterialSwitch = "--programme-follow-up-material";
	public static bool Enabled { get; private set; }
	public static bool DeliveryEnabled { get; private set; }
	public static bool MaterialEnabled { get; private set; }
	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		Enabled = args.Contains(Switch);
		DeliveryEnabled = args.Contains(DeliverySwitch);
		MaterialEnabled = args.Contains(MaterialSwitch);
		if (Enabled && !args.Contains(LabelReleaseProgramme.AllocationSwitch))
			throw new ArgumentException(Switch + " requires " + LabelReleaseProgramme.AllocationSwitch);
		if (DeliveryEnabled && !Enabled)
			throw new ArgumentException(DeliverySwitch + " requires " + Switch);
		if (MaterialEnabled && !(Enabled && args.Contains(LabelReleaseProgramme.ProjectJudgementSwitch)))
			throw new ArgumentException(MaterialSwitch + " requires " + Switch + " and " + LabelReleaseProgramme.ProjectJudgementSwitch);
	}

	// Following is an economic reader only in the causal demand arm. Shadow/observation remains observational.
	internal static float AudienceOrders(MediaFollowingState following) => following == null ? 0f :
		MediaFollowerDemand.RegionIntent(following.YoungTotal(), following.AdultTotal(), ReleaseFormat.Single);

	internal static float AudienceStrength(float orders, float nationalOrders) =>
		Math.Clamp(orders / Math.Max(MediaFollowerDemand.BookFloor, nationalOrders), 0f, 1f);

	// One additional score point per overdue quarter lets an established act overtake a slightly higher-scoring
	// labelmate while it retains current draw. Audience evidence is continuous;
	// it does not open an order book or change the launch-demand rates.
	internal static float WaitingPriority(ReleaseFormat format, int gap, bool hot, float audienceStrength) =>
		format != ReleaseFormat.Single ? 0f :
			Math.Max(0f, (gap - HitActCadence.DueWeeks) / (float)HitActCadence.DueWeeks) *
			Math.Max(hot ? 1f : 0f, Math.Clamp(audienceStrength, 0f, 1f));
	internal static float WorkPriority(int gap, bool hot, float audienceStrength) => gap < HitActCadence.DueWeeks ? 0f :
		Math.Max(hot ? 1f : 0f, Math.Clamp(audienceStrength, 0f, 1f)) +
		WaitingPriority(ReleaseFormat.Single, gap, hot, audienceStrength);

	// Use the existing own-session fallback in time for a due follow-up's ordinary one-week master lead.
	// An ordered commission still gets its delivery; the rule never records an LP or a future/unproven act early.
	internal static bool RoutineFollowUpDue(ReleaseSlateItem item, int week, int gap, bool hot,
		bool established, float audienceOrders) => item.Format == ReleaseFormat.Single &&
		item.ReadyWeek <= week && week + 1 >= item.IntendedWeek &&
		week < item.IntendedWeek + LabelReleaseProgramme.Horizon && gap + 1 >= HitActCadence.DueWeeks &&
		item.MaterialSearch?.State != ProgrammeMaterialState.Pending &&
		(hot || established && audienceOrders >= MediaFollowerDemand.BookFloor);

	// Preserve every LP's position in the mixed queue. Only single work that can be considered now competes
	// for precedence; future work cannot jump ahead merely because its act has a high score.
	internal static List<ReleaseSlateItem> OrderWork(IEnumerable<ReleaseSlateItem> dated, int week,
		Func<ReleaseSlateItem, float> priority, bool idle = false) {
		var result = dated.ToList();
		var singles = result.Where(i => i.Format == ReleaseFormat.Single)
			.Select((item, index) => (Item: item, Index: index,
				Priority: idle || (item.PreparedRecord == null ? item.ReadyWeek <= week : item.DueWeek <= week) ? priority(item) : 0f))
			.OrderByDescending(i => i.Priority).ThenBy(i => i.Index).Select(i => i.Item).ToList();
		int single = 0;
		for (int i = 0; i < result.Count; i++) if (result[i].Format == ReleaseFormat.Single) result[i] = singles[single++];
		return result;
	}
}
