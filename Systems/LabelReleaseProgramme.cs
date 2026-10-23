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
		SingleFloor = (int)Read(args, "--slate-single-floor=", 6, 1, 13);
		MajorSinglesPerMonth = Read(args, "--programme-major-singles=", 10, 0, 16);
		MajorAlbumsPerMonth = Read(args, "--programme-major-albums=", 5, 0, 12);
		if (args.Any(a => a.StartsWith("--programme-major-", StringComparison.Ordinal)) && !CapacityEnabled)
			throw new ArgumentException("Programme capacity parameters require " + CapacitySwitch);
		if (args.Any(a => a.StartsWith("--slate-single-floor=", StringComparison.Ordinal)) && !AllocationEnabled)
			throw new ArgumentException("The single floor requires " + AllocationSwitch);
	}
	private static float Read(string[] args, string prefix, float fallback, float min, float max) {
		string value = args.SingleOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal));
		if (value == null) return fallback;
		if (!float.TryParse(value[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
			!float.IsFinite(x) || x < min || x > max || prefix == "--slate-single-floor=" && x != MathF.Truncate(x))
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
			(label.releasesPerMonth * .67f, label.releasesPerMonth * .33f);
	}
	internal static bool IsOpen(ReleaseSlateItem item) => item.State is not (ReleaseSlateState.Issued or ReleaseSlateState.Cancelled);
	internal static string LabelIdentity(AILabel label) => FormattableString.Invariant(
		$"{label.labelName}|{label.founderName}|{label.foundedYear}|{label.homeCityId}|{label.isHistorical}|{label.archetype}|{label.budgetLevel:R}|{label.productionQuality:R}|{label.marketingPower:R}");
	internal static void SetForChecks(bool allocation, bool capacity) { AllocationEnabled = allocation; CapacityEnabled = capacity; }
}

public enum ReleaseSlateState { Proposed, InPreparation, Ready, Scheduled, Postponed, Issued, Cancelled }
public sealed class ReleaseSlateItem {
	public string Id, LabelId, ArtistId, Source, Evidence, Reason, RecordId;
	public ReleaseFormat Format;
	public ReleaseSlateState State;
	public GameDate IntendedDate, ActualDate;
	public int ProposedWeek, IntendedWeek, DueWeek, ReadyWeek, LastReviewWeek, ActualWeek, ContractSequence;
	public float ReservedProduction, ReservedMarketing, ReservedPressing, Score;
	public Record PreparedRecord;
	public bool PreparedRetainedIdentity;
	public float PaidProduction;
	public bool MissedFollowUp;
}
public sealed class ProgrammeClock { public string Identity; public int SingleWeek, AlbumWeek; }
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
}

public sealed class ProgrammeResourceTelemetry {
	public int Week, Year, Attempts, Successes, OpenItems, DueFollowUps, UnplannedFollowUps;
	public string LabelId, Tier;
	public float CashReserved, PromotionPool, PromotionBooked, SingleWeeklyRate, AlbumWeeklyRate;
}
