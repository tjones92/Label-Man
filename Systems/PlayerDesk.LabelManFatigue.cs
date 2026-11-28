using System;
using System.Collections.Generic;
using System.Linq;

public enum LabelManFatigueState { Fresh, Worn, Spent }

public sealed class LabelManFatigueSaveData {
	public int OwedMinutes { get; set; }
	public LabelManFatigueState StateAtWake { get; set; }
	public int SpentTodayMinutes { get; set; }
	public int? LastDayEndMinute { get; set; }
	public int[] RecentFinishMinutes { get; set; }
	public ExecutiveVoice QuietVoice { get; set; }
	public int? WakeMinute { get; set; }
}

public partial class PlayerDesk {
	internal const string DisableFatigueSwitch = "--disable-label-man-fatigue";
	private const int MaxFatigueMinutes = 12 * 60, QuietDayMinutes = 3 * 60;
	private const int FinishHistoryDays = 7;
	private bool fatigueEnabled;
	private int fatigueSpentToday;
	private int? fatigueLastDayEnd;
	private readonly List<int> fatigueRecentFinishes = new(FinishHistoryDays);
	private int? fatigueWakeMinute;
	internal bool ShowsFatigue => fatigueEnabled && Label != null && !IsGameOver;
	internal int FatigueWakeMinute => fatigueWakeMinute ?? (TimeManager.Instance?.WorkdayStartHour ?? 9) * 60;

	internal string FatigueMorningLine => !ShowsFatigue ? null : FatigueState switch {
		LabelManFatigueState.Worn when fatigueLastDayEnd == null && TimeManager.Instance?.CurrentDate == GameDate.StartDate
			=> "Last night's still in your head. The coffee can only do so much.",
		LabelManFatigueState.Worn => "Yesterday's still in your head. Give yourself a quiet evening.",
		LabelManFatigueState.Spent => $"You slept till {ClockText(FatigueWakeMinute)}. The night's still ringing in your ears.",
		_ => null
	};

	internal string FatigueStatusTip() {
		if (!ShowsFatigue) return null;
		string why = FatigueMorningLine ?? "You woke clear-headed.";
		if (FatigueState != LabelManFatigueState.Fresh && fatigueLastDayEnd is int lastEnd)
			why += $" You finished at {ClockText(lastEnd)} yesterday.";
		var clock = TimeManager.Instance;
		int regularEnd = clock.RegularWorkdayEndHour * 60;
		int overtimeEnd = clock.HardStopHour * 60;
		int lateNights = fatigueRecentFinishes.Count(end => end > overtimeEnd);
		int overtimeEvenings = fatigueRecentFinishes.Count(end => end > regularEnd && end <= overtimeEnd);
		if (lateNights + overtimeEvenings > 0) {
			var finishes = new List<string>(2);
			if (overtimeEvenings > 0) finishes.Add($"{Plural.Of(overtimeEvenings, "evening")} past {ClockText(regularEnd)}");
			if (lateNights > 0) finishes.Add($"{Plural.Of(lateNights, "night")} past {ClockText(overtimeEnd)}");
			why += $"\nIn the past seven days: {string.Join(" and ", finishes)}.";
		}
		string costs = FatigueState switch {
			LabelManFatigueState.Worn => "Reads are foggier today. Checks take −1 once unlocked.",
			LabelManFatigueState.Spent => $"Reads are foggier, checks take −2 once unlocked, and the Suit won't sign today. The {FatigueQuietVoice} is quiet.",
			_ => "You have your usual time and instincts today."
		};
		return $"{why}\n{costs}\nTurn in now to wake {ProjectedFatigue(NowMinute).ToString().ToLowerInvariant()}. A quiet day ending by {ClockText(regularEnd)} clears the most fatigue.";
	}

	internal string TonightFatiguePreview(int endMinute, int extraOwedMinutes = 0) {
		if (!ShowsFatigue || endMinute <= NowMinute ||
			extraOwedMinutes == 0 && endMinute <= TimeManager.Instance.RegularWorkdayEndHour * 60) return null;
		var tomorrow = ProjectedFatigue(endMinute, extraOwedMinutes);
		return tomorrow == ProjectedFatigue(NowMinute) ? null : $"Until {ClockText(endMinute)} · you'll wake {tomorrow.ToString().ToLowerInvariant()}.";
	}

	private LabelManFatigueState ProjectedFatigue(int endMinute, int extraOwedMinutes = 0) =>
		FatigueBand(DebtAfterRecovery(endMinute, fatigueSpentToday + endMinute - NowMinute,
			DebtAfterWork(NowMinute, endMinute, extraOwedMinutes)));

	/// <summary>The waking state for the entire working day.</summary>
	public LabelManFatigueState FatigueState { get; private set; }
	public ExecutiveVoice FatigueQuietVoice { get; private set; }
	internal int FatigueFogSteps => (int)FatigueState;
	internal string FatigueReadKey(string key) => FatigueFogSteps == 0 ? key : $"{key}|fatigue:{FatigueFogSteps}";
	public string FatigueSigningBlock => FatigueState == LabelManFatigueState.Spent
		? "THE SUIT — Leave the paper where it is. Come back after some sleep." : null;
	public string FatigueQuietNotice => FatigueQuietVoice == ExecutiveVoice.None ? null
		: $"THE {(FatigueQuietVoice == ExecutiveVoice.Suit ? ExecutiveVoice.Fixer : ExecutiveVoice.Suit).ToString().ToUpperInvariant()} — The {FatigueQuietVoice} has gone quiet this morning.";

	private ExecutiveVoice LeastTrustedVoice() => InstinctVoices.MinBy(voice => InstinctScore(InstinctProfile, voice));
	internal int FatigueOwedMinutes { get; private set; }

	private void AttachFatigueClock(TimeManager clock) {
		fatigueEnabled = !SimulationArguments.Get().Contains(DisableFatigueSwitch, StringComparer.Ordinal);
		if (!fatigueEnabled) return;
		clock.OnMinutesSpent += AccrueFatigue;
		clock.PlayerWakeProbe = RecoverFatigue;
	}

	private void DetachFatigueClock(TimeManager clock) {
		clock.OnMinutesSpent -= AccrueFatigue;
		if (clock.PlayerWakeProbe == RecoverFatigue) clock.PlayerWakeProbe = null;
	}

	private void AccrueFatigue(int fromMinute, int toMinute) {
		if (Label == null || IsGameOver) return;
		FatigueOwedMinutes = DebtAfterWork(fromMinute, toMinute);
		fatigueSpentToday += toMinute - fromMinute;
		TeachFirstOvertime(fromMinute, toMinute);
	}

	private int DebtAfterWork(int fromMinute, int toMinute, int extraOwedMinutes = 0) {
		var clock = TimeManager.Instance;
		int regularEnd = clock.RegularWorkdayEndHour * 60;
		int overtimeEnd = clock.HardStopHour * 60;
		int overtime = Math.Max(0, Math.Min(toMinute, overtimeEnd) - Math.Max(fromMinute, regularEnd));
		int night = Math.Max(0, toMinute - Math.Max(fromMinute, overtimeEnd));
		return Math.Min(MaxFatigueMinutes, FatigueOwedMinutes + overtime + 2 * night + extraOwedMinutes);
	}

	private void ChargeBusFatigue(TransportMode mode, int hours) {
		if (fatigueEnabled && mode == TransportMode.Bus && hours >= 4)
			FatigueOwedMinutes = Math.Min(MaxFatigueMinutes, FatigueOwedMinutes + 60);
	}

	private static int DebtAfterRecovery(int endedMinute, int spentMinutes, int debt) {
		var clock = TimeManager.Instance;
		if (endedMinute <= clock.RegularWorkdayEndHour * 60) {
			int earlyRecoveryHours = spentMinutes <= QuietDayMinutes ? 7 : 5;
			return Math.Max(0, debt - earlyRecoveryHours * 60);
		}
		// Take up to two hours off the time available beyond the usual eight hours of sleep.
		int latestFullSleep = (24 + clock.WorkdayStartHour - TimeManager.SleepHoursAfterLateNight) * 60;
		int recoveryMinutes = Math.Clamp(latestFullSleep - endedMinute, 0, 2 * 60);
		return Math.Max(0, debt - recoveryMinutes);
	}

	private int RecoverFatigue(int endedMinute, int usualWakeMinute) {
		if (Label == null || IsGameOver) return usualWakeMinute;
		FatigueOwedMinutes = DebtAfterRecovery(endedMinute, fatigueSpentToday, FatigueOwedMinutes);
		fatigueLastDayEnd = endedMinute;
		if (fatigueRecentFinishes.Count == FinishHistoryDays) fatigueRecentFinishes.RemoveAt(0);
		fatigueRecentFinishes.Add(endedMinute);
		fatigueSpentToday = 0;

		FatigueState = FatigueBand(FatigueOwedMinutes);
		FatigueQuietVoice = FatigueState == LabelManFatigueState.Spent ? LeastTrustedVoice() : ExecutiveVoice.None;
		int wake = usualWakeMinute;
		if (FatigueState == LabelManFatigueState.Spent) {
			int fatigueWake = Math.Min(12, TimeManager.Instance.WorkdayStartHour + FatigueOwedMinutes / 60 - 5) * 60;
			wake = Math.Max(wake, fatigueWake);
		}
		fatigueWakeMinute = wake;
		return wake;
	}

	private static LabelManFatigueState FatigueBand(int debt) => debt switch {
		>= 6 * 60 => LabelManFatigueState.Spent,
		>= 3 * 60 => LabelManFatigueState.Worn,
		_ => LabelManFatigueState.Fresh
	};

	private void StartFatigue() {
		bool firstMorning = fatigueEnabled && TimeManager.Instance?.CurrentDate == GameDate.StartDate;
		FatigueOwedMinutes = firstMorning ? 4 * 60 : 0;
		FatigueState = firstMorning ? LabelManFatigueState.Worn : LabelManFatigueState.Fresh;
		FatigueQuietVoice = ExecutiveVoice.None;
		fatigueSpentToday = 0;
		fatigueLastDayEnd = null;
		fatigueRecentFinishes.Clear();
		fatigueWakeMinute = null;
	}

	private LabelManFatigueSaveData CaptureFatigue() => fatigueEnabled ? new() {
		OwedMinutes = FatigueOwedMinutes,
		StateAtWake = FatigueState,
		SpentTodayMinutes = fatigueSpentToday,
		LastDayEndMinute = fatigueLastDayEnd,
		RecentFinishMinutes = fatigueRecentFinishes.ToArray(),
		QuietVoice = FatigueQuietVoice,
		WakeMinute = fatigueWakeMinute
	} : null;

	private void RestoreFatigue(LabelManFatigueSaveData data) {
		if (!fatigueEnabled) data = null;
		FatigueOwedMinutes = Math.Clamp(data?.OwedMinutes ?? 0, 0, MaxFatigueMinutes);
		FatigueState = data?.StateAtWake is LabelManFatigueState.Worn or LabelManFatigueState.Spent
			? data.StateAtWake : LabelManFatigueState.Fresh;
		FatigueQuietVoice = FatigueState != LabelManFatigueState.Spent ? ExecutiveVoice.None
			: InstinctVoices.Contains(data.QuietVoice) ? data.QuietVoice : LeastTrustedVoice();
		fatigueSpentToday = Math.Max(0, data?.SpentTodayMinutes ?? 0);
		fatigueLastDayEnd = data?.LastDayEndMinute;
		fatigueRecentFinishes.Clear();
		if (data?.RecentFinishMinutes is { Length: > 0 } finishes)
			fatigueRecentFinishes.AddRange(finishes.TakeLast(FinishHistoryDays));
		else if (fatigueLastDayEnd is int lastEnd)
			fatigueRecentFinishes.Add(lastEnd);
		fatigueWakeMinute = data?.WakeMinute;
		if (fatigueWakeMinute == null && data?.LastDayEndMinute != null)
			fatigueWakeMinute = Math.Max((TimeManager.Instance?.WorkdayStartHour ?? 9) * 60, NowMinute - fatigueSpentToday);
	}
}
