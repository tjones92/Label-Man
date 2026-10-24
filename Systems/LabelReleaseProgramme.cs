using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>Experimental programme parameters, not historical quotas. Off consumes no random draws or save fields.</summary>
public static class LabelReleaseProgramme {
	public const string AllocationSwitch = "--label-release-slate";
	public const string CapacitySwitch = "--label-release-capacity";
	public static bool AllocationEnabled { get; private set; }
	public static bool CapacityEnabled { get; private set; }
	public static bool MaterialSearchEnabled { get; private set; }
	public static bool LpAssemblyEnabled { get; private set; }
	public static bool Enabled => AllocationEnabled || CapacityEnabled;
	public static int SingleFloor { get; private set; } = 6;
	public const int Horizon = 8, PreparationWeeks = 2, AlbumFloor = 18, CampaignWeeks = 4;
	public static float MajorSinglesPerMonth { get; private set; } = 10f;
	public static float MajorAlbumsPerMonth { get; private set; } = 5f;
	public static bool HasState => Enabled || CompetitorManager.Instance?.ProgrammeState != null;
	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		AllocationEnabled = args.Contains(AllocationSwitch);
		CapacityEnabled = args.Contains(CapacitySwitch);
		MaterialSearchEnabled = args.Contains("--anticipatory-material-search");
		if (MaterialSearchEnabled && !AllocationEnabled)
			throw new ArgumentException("--anticipatory-material-search requires " + AllocationSwitch);
		LpAssemblyEnabled = args.Contains(LpAssembly.Switch);
		if (LpAssemblyEnabled && !AllocationEnabled) throw new ArgumentException(LpAssembly.Switch + " requires " + AllocationSwitch);
		LpAssembly.GreatestHitsHits = (int)Read(args, LpAssembly.GreatestHitsSwitch, 3, 1, 8);
		if (args.Any(a => a.StartsWith(LpAssembly.GreatestHitsSwitch, StringComparison.Ordinal)) && !LpAssemblyEnabled)
			throw new ArgumentException(LpAssembly.GreatestHitsSwitch + " requires " + LpAssembly.Switch);
		LpAssembly.PackageGapWeeks = (int)Read(args, LpAssembly.PackageGapSwitch, 104, 0, 520);
		if (args.Any(a => a.StartsWith(LpAssembly.PackageGapSwitch, StringComparison.Ordinal)) && !LpAssemblyEnabled)
			throw new ArgumentException(LpAssembly.PackageGapSwitch + " requires " + LpAssembly.Switch);
		SingleFloor = (int)Read(args, "--slate-single-floor=", 6, 1, 13);
		MajorSinglesPerMonth = Read(args, "--programme-major-singles=", 10, 0, 16);
		MajorAlbumsPerMonth = Read(args, "--programme-major-albums=", 5, 0, 12);
		if (args.Any(a => a.StartsWith("--programme-major-", StringComparison.Ordinal)) && !CapacityEnabled)
			throw new ArgumentException("Programme capacity parameters require " + CapacitySwitch);
		if (args.Any(a => a.StartsWith("--slate-single-floor=", StringComparison.Ordinal)) && !AllocationEnabled)
			throw new ArgumentException("The single floor requires " + AllocationSwitch);
		HitFollowUpFinanceEnabled = args.Contains(HitFollowUpFinanceSwitch);
		if (HitFollowUpFinanceEnabled && !(AllocationEnabled && CapacityEnabled))
			throw new ArgumentException(HitFollowUpFinanceSwitch + " requires " + AllocationSwitch + " and " + CapacitySwitch);
		HitFollowUpSinglesPerYear = Read(args, HitFollowUpSinglesSwitch, 2, 0, 6);
		if (args.Any(a => a.StartsWith(HitFollowUpSinglesSwitch, StringComparison.Ordinal)) && !HitFollowUpFinanceEnabled)
			throw new ArgumentException(HitFollowUpSinglesSwitch + " requires " + HitFollowUpFinanceSwitch);
		PreparationHeadroom = Read(args, PreparationHeadroomSwitch, 1, 1, 4);
		if (args.Any(a => a.StartsWith(PreparationHeadroomSwitch, StringComparison.Ordinal)) && !AllocationEnabled)
			throw new ArgumentException(PreparationHeadroomSwitch + " requires " + AllocationSwitch);
		WorkBudgetSingles = Read(args, WorkBudgetSwitch, 0, .25f, 3);
		if (args.Any(a => a.StartsWith(WorkBudgetSwitch, StringComparison.Ordinal)) && !(AllocationEnabled && CapacityEnabled))
			throw new ArgumentException(WorkBudgetSwitch + " requires " + AllocationSwitch + " and " + CapacitySwitch);
		if (WorkBudgetSingles > 0 && PreparationHeadroom != 1f)
			throw new ArgumentException(WorkBudgetSwitch + " replaces the preparation draw; drop " + PreparationHeadroomSwitch);
		WorkBudgetMidTierSingles = Read(args, WorkBudgetMidTierSwitch, 0, .25f, 3);
		if (args.Any(a => a.StartsWith(WorkBudgetMidTierSwitch, StringComparison.Ordinal)) && WorkBudgetSingles <= 0)
			throw new ArgumentException(WorkBudgetMidTierSwitch + " requires " + WorkBudgetSwitch);
		GoingConcernReserveEnabled = args.Contains(GoingConcernReserveSwitch);
		if (GoingConcernReserveEnabled && WorkBudgetSingles <= 0)
			throw new ArgumentException(GoingConcernReserveSwitch + " requires " + WorkBudgetSwitch);
		BreakoutPlanningEnabled = args.Contains(BreakoutPlanningSwitch);
		if (BreakoutPlanningEnabled && WorkBudgetSingles <= 0)
			throw new ArgumentException(BreakoutPlanningSwitch + " requires " + WorkBudgetSwitch);
		if (BreakoutPlanningEnabled && HitFollowUpFinanceEnabled)
			throw new ArgumentException(BreakoutPlanningSwitch + " uses existing work only; drop " + HitFollowUpFinanceSwitch);
	}
	public const string BreakoutPlanningSwitch = "--slate-breakout-planning";
	public static bool BreakoutPlanningEnabled { get; private set; }
	// --slate-going-concern-reserve: a work-budget MidTier tests its slate reservations against a going-concern reserve.
	// Overhead, the negative-flow hold, plant payables and programme holds stay; the tax-and-service hold is replaced by
	// the tax that falls due within the reservation lead (two month-ends). wbg-v2: surviving MidTiers were refused $2.5k
	// singles holding a median $87k, behind accrued tax due the next March/June and two months of variable selling costs.
	public const string GoingConcernReserveSwitch = "--slate-going-concern-reserve";
	public const int GoingConcernTaxMonths = 2;
	public static bool GoingConcernReserveEnabled { get; private set; }
	internal static void SetGoingConcernReserveForChecks(bool value) => GoingConcernReserveEnabled = value;
	internal static bool UsesGoingConcernReserve(AILabel label) => GoingConcernReserveEnabled && UsesWorkBudget(label) && label.tier == LabelTier.MidTier;
	// --slate-work-budget=S: a non-Major label's planned intentions are its whole work budget, S singles a month per unit of
	// releasesPerMonth (LPs keep .33). Preparation and issue take no further dated draws, so planned work is lost only to
	// cash, material, economics or the window. A Struggling label keeps its slots and halves its campaign spend instead
	// (LabelReleaseProgrammeImplementation.md, "Preparation headroom"). A probe sized to pre-slate output, not a sourced rate.
	public const string WorkBudgetSwitch = "--slate-work-budget=";
	public static float WorkBudgetSingles { get; private set; }
	// --slate-work-budget-midtier=SM: MidTiers take SM in place of S; the smaller tiers keep S (wbg-v1: at one S the small
	// tiers added nine singles in ten and cut r by .11 while MidTiers stayed below their pre-slate output).
	public const string WorkBudgetMidTierSwitch = "--slate-work-budget-midtier=";
	public static float WorkBudgetMidTierSingles { get; private set; }
	internal static void SetWorkBudgetForChecks(float value, float midTier = 0f) { WorkBudgetSingles = value; WorkBudgetMidTierSingles = midTier; }
	internal static float WorkBudgetFor(AILabel label) =>
		label.tier == LabelTier.MidTier && WorkBudgetMidTierSingles > 0 ? WorkBudgetMidTierSingles : WorkBudgetSingles;
	internal static bool UsesWorkBudget(AILabel label) => WorkBudgetSingles > 0 && label.tier != LabelTier.Major && !label.isPlayerOwned;
	internal static float SlotStatusFactor(AILabel label) =>
		UsesWorkBudget(label) && label.status == LabelStatus.Struggling ? 1f : StatusFactor(label.status);
	internal static float CampaignStatusFactor(AILabel label) =>
		UsesWorkBudget(label) && label.status == LabelStatus.Struggling ? StatusFactor(LabelStatus.Struggling) : 1f;
	// --slate-preparation-headroom=H: a non-Major label's weekly preparation draw runs at H x its planning rate. Planning,
	// preparation and issue otherwise share one rate, so work that misses a preparation draw queues at utilization one
	// and expires at the horizon (LabelReleaseProgrammeImplementation.md, "Why the control's field repeat acts collapse").
	// A probe, not a sourced rate. At 1 the rate passes through unchanged.
	public const string PreparationHeadroomSwitch = "--slate-preparation-headroom=";
	public static float PreparationHeadroom { get; private set; } = 1f;
	internal static void SetPreparationHeadroomForChecks(float value) => PreparationHeadroom = value;
	internal static float PreparationRate(AILabel label, float planningRate) =>
		PreparationHeadroom == 1f || label.tier == LabelTier.Major ? planningRate : planningRate * PreparationHeadroom;
	// --hit-follow-up-finance: a non-Major label's act with a recent Top 40 gets dedicated single work, and a cash-short
	// follow-up may factor the label's wholesale receivables at the player's terms. A scenario, not a sourced rate.
	public const string HitFollowUpFinanceSwitch = "--hit-follow-up-finance";
	public const string HitFollowUpSinglesSwitch = "--hit-follow-up-finance-singles=";
	public static bool HitFollowUpFinanceEnabled { get; private set; }
	public static float HitFollowUpSinglesPerYear { get; private set; } = 2f;
	internal static void SetHitFollowUpFinanceForChecks(bool value, float singlesPerYear = 2f) {
		HitFollowUpFinanceEnabled = value; HitFollowUpSinglesPerYear = singlesPerYear;
	}
	// The player's factoring terms (PlayerDesk.FactorRate): 70-85 cents on the dollar by the house's reliability.
	internal static float FactorRate(float? houseReliability) => Math.Clamp(0.70f + 0.15f * (houseReliability ?? 0.5f), 0.70f, 0.85f);
	private static float Read(string[] args, string prefix, float fallback, float min, float max) {
		string value = args.SingleOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
		if (value == null) return fallback;
		if (!float.TryParse(value[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
			!float.IsFinite(x) || x < min || x > max || (prefix == "--slate-single-floor=" || prefix == LpAssembly.GreatestHitsSwitch || prefix == LpAssembly.PackageGapSwitch) && x != MathF.Truncate(x))
			throw new ArgumentException($"{prefix} requires a value between {min} and {max}.");
		return x;
	}
	internal static double Roll(string label, string act, string slot, int week, uint domain) =>
		PhiloxCounterRandom.Unit(SimulationSeedBootstrap.RequestedSeed ?? 0UL,
			$"release-programme-v1|{label}|{act}|{slot}", "programme", week, domain);
	// Stochastic rounding of dated work slots, never a saved bank of unused opportunities.
	internal static int Slots(float weeklyRate, double roll) => (int)MathF.Floor(Math.Max(0, weeklyRate)) +
		(roll < weeklyRate - MathF.Floor(weeklyRate) ? 1 : 0);
	internal static float StatusFactor(LabelStatus status) => status switch {
		LabelStatus.Bankrupt or LabelStatus.Defunct or LabelStatus.Acquired => 0,
		LabelStatus.Dying => .3f, LabelStatus.Struggling => .5f, LabelStatus.Rising => 1.2f, _ => 1
	};
	internal static (float Singles, float Albums) MonthlyCapacity(AILabel label) {
		// RCA's 2-3 weekly singles is a head scenario only. Other tiers retain their own envelope pending evidence.
		return label.tier == LabelTier.Major ? (MajorSinglesPerMonth, MajorAlbumsPerMonth) :
			(label.releasesPerMonth * (UsesWorkBudget(label) ? WorkBudgetFor(label) : .67f), label.releasesPerMonth * .33f);
	}
	internal static bool IsOpen(ReleaseSlateItem item) => item.State is not (ReleaseSlateState.Issued or ReleaseSlateState.Cancelled);
	internal static bool CanUseIdleSlot(ReleaseSlateItem item, int week) =>
		IsOpen(item) && item.State != ReleaseSlateState.AwaitingDelivery && item.IntendedWeek > week && item.DueWeek > week &&
		!(item.State == ReleaseSlateState.Postponed && item.LastReviewWeek == week);
	internal static int EarliestIssueWeek(ReleaseSlateItem item) => item.PreparedWeek is int prepared
		? prepared == item.ProposedWeek && prepared == item.IntendedWeek ? prepared : prepared + 1
		: item.ReadyWeek; // Older saves did not record the actual session week.
	internal static string LabelIdentity(AILabel label) => FormattableString.Invariant(
		$"{label.labelName}|{label.founderName}|{label.foundedYear}|{label.homeCityId}|{label.isHistorical}|{label.archetype}|{label.budgetLevel:R}|{label.productionQuality:R}|{label.marketingPower:R}");
	internal static void SetForChecks(bool allocation, bool capacity) { AllocationEnabled = allocation; CapacityEnabled = capacity; }
	internal static void SetMaterialSearchForChecks(bool value) => MaterialSearchEnabled = value;
	internal static void SetLpAssemblyForChecks(bool value) => LpAssemblyEnabled = value;
}

public enum ReleaseSlateState { Proposed, InPreparation, Ready, Scheduled, Postponed, Issued, Cancelled, AwaitingDelivery }
public sealed class ReleaseSlateItem {
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public ProgrammeMaterialSearch MaterialSearch;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public LpAssemblyPlan LpAssembly;
	public string Id, LabelId, ArtistId, Source, Evidence, Reason, RecordId;
	public ReleaseFormat Format;
	public ReleaseSlateState State;
	public GameDate IntendedDate, ActualDate;
	public int ProposedWeek, IntendedWeek, DueWeek, ReadyWeek, LastReviewWeek, ActualWeek, ContractSequence;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public int? PreparedWeek;
	public float ReservedProduction, ReservedMarketing, ReservedPressing, Score;
	public Record PreparedRecord;
	public bool PreparedRetainedIdentity;
	public float PaidProduction;
	public bool MissedFollowUp;
}
public sealed class ProgrammeClock {
	public string Identity; public int SingleWeek, AlbumWeek;
	// LP assembly: the act's last greatest-hits package (seeded for opening acts); absent otherwise.
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public int? PackageWeek;
}
public sealed class ProgrammeCampaign {
	public string LabelId, RecordId;
	public int Week;
	public float Spend, Share;
}
public sealed class ReleaseProgrammeState {
	public bool CampaignsInitialized;
	public List<ReleaseSlateItem> Items = new();
	public Dictionary<string, ProgrammeClock> Clocks = new(StringComparer.Ordinal);
	public List<ProgrammeCampaign> Campaigns = new();
	public Dictionary<string, int> PlannedThrough = new(StringComparer.Ordinal);
	public Dictionary<string, string> LabelIdentities = new(StringComparer.Ordinal);
	// LP assembly only; null otherwise so earlier programme worlds keep their exact shape.
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, LpMasterControl> MasterControl;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public List<ShelvedMaster> Shelf;
	// Pre-1960 discography sides as masters, by act; non-null once seeded (empty when no act has a history).
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, List<AlbumTrack>> OpeningMasters;
}

/// <summary>A work-budget label's slate reservation refused for cash, with the commitment reserve split into its parts.</summary>
public sealed class ProgrammeCashShortfall {
	public int Week;
	public string LabelId, Tier, Status, SlotId, ArtistId;
	public ReleaseFormat Format;
	public float Cash, Overhead, NegativeFlowHold, PlantPayables, TaxAndService, OtherProgrammeHolds, Production, Marketing, Pressing,
		Receivables, Factored, TaxPayable, UnprovidedTax, CommercialHold;
}

public sealed class ProgrammeResourceTelemetry {
	public int Week, Year, Attempts, Successes, OpenItems, DueFollowUps, UnplannedFollowUps;
	public string LabelId, Tier;
	public float CashReserved, PromotionPool, PromotionBooked, PromotionCommittedPeak, SingleWeeklyRate, AlbumWeeklyRate;
}
