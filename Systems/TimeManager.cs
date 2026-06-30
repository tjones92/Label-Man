using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class TimeManager : Node {
	public static TimeManager Instance { get; private set; }

	[ExportGroup("Current State")]
	private GameDate currentDate;
	[Export] private int currentHour = 9;

	[ExportGroup("Settings")]
	[Export] private int workDayStartHour = 9;
	[Export] private int workDayEndHour = 18;
	[Export] private int maxOvertimeHours = 3;

	private List<ScheduledEvent> scheduledEvents = new List<ScheduledEvent>();

	public GameDate CurrentDate => currentDate;
	public int CurrentHour => currentHour;
	public int HoursRemaining => Mathf.Max(0, workDayEndHour - currentHour);
	public int HoursRemainingWithOvertime => Mathf.Max(0, (workDayEndHour + maxOvertimeHours) - currentHour);
	public bool IsWorkDay => !currentDate.IsWeekend;
	public bool IsOvertime => currentHour >= workDayEndHour;
	public bool IsDayOver => currentHour >= workDayEndHour + maxOvertimeHours;
	public bool IsFriday => currentDate.IsFriday;
	public bool IsGameOver => currentDate > GameDate.EndDate;

	public event Action<GameDate> OnDayStarted;
	public event Action<GameDate> OnDayEnded;
	public event Action<GameDate> OnWeekEnded;
	public event Action<GameDate> OnMonthChanged;
	public event Action<GameDate> OnYearChanged;
	public event Action<int> OnHourChanged;
	public event Action<ScheduledEvent> OnEventTriggered;
	public event Action<ScheduledEvent> OnSkipInterrupted;
	public event Action OnGameEnded;

	public override void _EnterTree() {
		if (Instance != null && Instance != this) {
			QueueFree();
			return;
		}
		Instance = this;
	}

	public override void _Ready() {
		currentDate = GameDate.StartDate;
		currentHour = workDayStartHour;

		ScheduleChartDays();
		ScheduleGrammyAwards();

		OnDayStarted?.Invoke(currentDate);
	}

	public bool CanAffordHours(int hours, bool allowOvertime = false) {
		int available = allowOvertime ? HoursRemainingWithOvertime : HoursRemaining;
		return hours <= available;
	}

	public bool SpendHours(int hours, bool allowOvertime = false) {
		if (!CanAffordHours(hours, allowOvertime)) return false;
		currentHour += hours;
		OnHourChanged?.Invoke(currentHour);
		return true;
	}

	public void ForceSpendHours(int hours) {
		currentHour += hours;
		OnHourChanged?.Invoke(currentHour);
		if (IsDayOver) EndDay();
	}

	public void EndDay() {
		ProcessDayEnd();
		AdvanceToNextDay();
	}

	private void ProcessDayEnd() {
		OnDayEnded?.Invoke(currentDate);
		if (currentDate.IsFriday) {
			OnWeekEnded?.Invoke(currentDate);
		}
	}

	private void ScheduleGrammyAwards() {
		for (int year = GameDate.StartDate.year + 1; year <= GameDate.EndDate.year; year++) {
			GameDate febFirst = new GameDate(year, 2, 1);
			int daysUntilFirstFriday = febFirst.DaysUntil(DayOfWeek.Friday);
			GameDate lastFriday = febFirst.AddDays(daysUntilFirstFriday + 21);

			var grammyEvent = new ScheduledEvent("Grammy Awards", lastFriday, EventType.GrammyAwards) {
				priority = EventPriority.Critical,
				interruptsSkip = true,
				description = "The annual Grammy Awards are announced today for last year's music."
			};
			scheduledEvents.Add(grammyEvent);
		}
	}

	private void AdvanceToNextDay() {
		int previousMonth = currentDate.month;
		int previousYear = currentDate.year;

		currentDate = currentDate.NextDay();
		currentHour = workDayStartHour;

		if (currentDate.month != previousMonth) OnMonthChanged?.Invoke(currentDate);
		if (currentDate.year != previousYear) OnYearChanged?.Invoke(currentDate);

		if (IsGameOver) {
			OnGameEnded?.Invoke();
			return;
		}

		Rolodex.Instance?.AdvanceDay();
		TriggerEventsForDate(currentDate);
		OnDayStarted?.Invoke(currentDate);
	}

	public ScheduledEvent SkipToDate(GameDate targetDate, EventPriority minimumInterruptPriority = EventPriority.High) {
		while (currentDate < targetDate && !IsGameOver) {
			ProcessDayEnd();
			GameDate tomorrow = currentDate.NextDay();
			var interruptEvent = GetInterruptEventForDate(tomorrow, minimumInterruptPriority);

			if (interruptEvent != null) {
				AdvanceToNextDay();
				OnSkipInterrupted?.Invoke(interruptEvent);
				return interruptEvent;
			}
			AdvanceToNextDay();
		}
		return null;
	}

	public ScheduledEvent SkipToFriday() {
		if (currentDate.IsFriday) {
			return SkipToDate(currentDate.AddDays(7), EventPriority.High);
		}
		return SkipToDate(currentDate.AddDays(currentDate.DaysUntilFriday), EventPriority.High);
	}

	public ScheduledEvent SkipDays(int days) {
		return SkipToDate(currentDate.AddDays(days), EventPriority.Critical);
	}

	public ScheduledEvent SkipToNextEvent() {
		var nextEvent = GetNextEvent();
		if (nextEvent != null && nextEvent.date > currentDate) {
			return SkipToDate(nextEvent.date, EventPriority.High);
		}
		return null;
	}

	private ScheduledEvent GetInterruptEventForDate(GameDate date, EventPriority minimumPriority) {
		return scheduledEvents
			.Where(e => e.date == date && e.interruptsSkip && e.priority >= minimumPriority)
			.OrderByDescending(e => e.priority)
			.FirstOrDefault();
	}

	public void ScheduleEvent(ScheduledEvent evt) {
		scheduledEvents.Add(evt);
		scheduledEvents = scheduledEvents.OrderBy(e => e.date).ToList();
	}

	public void CancelEvent(string eventId) {
		scheduledEvents.RemoveAll(e => e.eventId == eventId);
	}

	public List<ScheduledEvent> GetEventsForDate(GameDate date) {
		return scheduledEvents.Where(e => e.date == date).ToList();
	}

	public List<ScheduledEvent> GetUpcomingEvents(int days = 7) {
		GameDate endDate = currentDate.AddDays(days);
		return scheduledEvents.Where(e => e.date >= currentDate && e.date <= endDate).ToList();
	}

	public ScheduledEvent GetNextEvent() {
		return scheduledEvents.Where(e => e.date > currentDate).OrderBy(e => e.date).FirstOrDefault();
	}

	private ScheduledEvent GetInterruptEventForDate(GameDate date) {
		return scheduledEvents.Where(e => e.date == date && e.interruptsSkip).OrderByDescending(e => e.priority).FirstOrDefault();
	}

	private void TriggerEventsForDate(GameDate date) {
		var todaysEvents = GetEventsForDate(date);
		foreach (var evt in todaysEvents) {
			OnEventTriggered?.Invoke(evt);
		}
		scheduledEvents.RemoveAll(e => e.date == date && e.eventType != EventType.ChartDay);
	}

	private void ScheduleChartDays() {
		GameDate date = GameDate.StartDate;
		while (date <= GameDate.EndDate) {
			if (date.IsFriday) {
				var chartDay = new ScheduledEvent("Chart Day", date, EventType.ChartDay) {
					priority = EventPriority.High,
					interruptsSkip = true,
					description = "Billboard Hot 100 updates"
				};
				scheduledEvents.Add(chartDay);
			}
			date = date.NextDay();
		}
	}

	public int DaysUntilEvent(EventType type) {
		var evt = scheduledEvents.Where(e => e.eventType == type && e.date > currentDate).OrderBy(e => e.date).FirstOrDefault();
		if (evt == null) return -1;
		return DaysBetween(currentDate, evt.date);
	}

	public int DaysBetween(GameDate from, GameDate to) {
		DateTime fromDt = new DateTime(from.year, from.month, from.day);
		DateTime toDt = new DateTime(to.year, to.month, to.day);
		return (int)(toDt - fromDt).TotalDays;
	}

	public string GetTimeString() {
		int displayHour = currentHour > 12 ? currentHour - 12 : currentHour;
		if (displayHour == 0) displayHour = 12;
		string ampm = currentHour >= 12 ? "PM" : "AM";
		return $"{displayHour}:00 {ampm}";
	}

	public string GetDayStatus() {
		if (IsDayOver) return "Day Over";
		if (IsOvertime) return "Overtime";
		if (HoursRemaining <= 2) return "Late Afternoon";
		if (currentHour < 12) return "Morning";
		return "Afternoon";
	}

	// Removed [ContextMenu] attributes for Godot
	public void DebugAdvanceDay() {
		EndDay();
		GD.Print($"Advanced to {currentDate.ToLongString()}");
	}

	public void DebugAdvanceWeek() {
		SkipDays(7);
		GD.Print($"Advanced to {currentDate.ToLongString()}");
	}

	public void DebugSkipToFriday() {
		var result = SkipToFriday();
		if (result != null) GD.Print($"Skip interrupted by: {result.title}");
		else GD.Print($"Skipped to Friday: {currentDate.ToLongString()}");
	}

	public void DebugLogUpcomingEvents() {
		var events = GetUpcomingEvents(14);
		GD.Print($"=== Upcoming Events (next 14 days) ===");
		foreach (var evt in events) {
			GD.Print($"{evt.date.ToShortString()}: {evt.title} ({evt.eventType})");
		}
	}

	public void DebugSpendHours() {
		if (SpendHours(2)) {
			GD.Print($"Spent 2 hours. Now: {GetTimeString()}, {HoursRemaining}h remaining");
		} else {
			GD.Print("Not enough hours remaining");
		}
	}
}
