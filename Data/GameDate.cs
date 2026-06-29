using System;
using Godot;

public struct GameDate : IComparable<GameDate> {
	public int year;
	public int month;
	public int day;
	
	public GameDate(int year, int month, int day) {
		this.year = year;
		this.month = month;
		this.day = day;
	}
	
	public static GameDate StartDate => new GameDate(1960, 1, 1);
	public static GameDate EndDate => new GameDate(1969, 12, 31);
	
	public DayOfWeek DayOfWeek => new DateTime(year, month, day).DayOfWeek;
	
	public bool IsFriday => DayOfWeek == DayOfWeek.Friday;
	public bool IsWeekend => DayOfWeek == DayOfWeek.Saturday || DayOfWeek == DayOfWeek.Sunday;
	
	public string DayName => DayOfWeek.ToString();

	public GameDate SubtractWeeks(int weeks) {
		return AddDays(-(weeks * 7));
	}

	public int WeeksDifference(GameDate other) {
		DateTime dt1 = new DateTime(this.year, this.month, this.day);
		DateTime dt2 = new DateTime(other.year, other.month, other.day);
		TimeSpan diff = dt1 - dt2;
		return Math.Abs((int)(diff.TotalDays / 7));
	}
	
	public string MonthName => month switch {
		1 => "January", 2 => "February", 3 => "March", 4 => "April",
		5 => "May", 6 => "June", 7 => "July", 8 => "August",
		9 => "September", 10 => "October", 11 => "November", 12 => "December",
		_ => "Unknown"
	};
	
	public string ShortMonthName => month switch {
		1 => "Jan", 2 => "Feb", 3 => "Mar", 4 => "Apr",
		5 => "May", 6 => "Jun", 7 => "Jul", 8 => "Aug",
		9 => "Sep", 10 => "Oct", 11 => "Nov", 12 => "Dec",
		_ => "???"
	};
	
	public string ToShortString() => $"{month}/{day}/{year}";
	public string ToLongString() => $"{DayName}, {MonthName} {day}, {year}";
	public string ToHeadlineString() => $"{ShortMonthName} {day}, {year}";
	
	public GameDate NextDay() {
		DateTime dt = new DateTime(year, month, day).AddDays(1);
		return new GameDate(dt.Year, dt.Month, dt.Day);
	}
	
	public GameDate AddDays(int days) {
		DateTime dt = new DateTime(year, month, day).AddDays(days);
		return new GameDate(dt.Year, dt.Month, dt.Day);
	}
	
	public int DaysUntil(DayOfWeek target) {
		int current = (int)DayOfWeek;
		int targetInt = (int)target;
		int diff = targetInt - current;
		if (diff <= 0) diff += 7;
		return diff;
	}
	
	public int DaysUntilFriday => IsFriday ? 0 : DaysUntil(DayOfWeek.Friday);
	
	public int CompareTo(GameDate other) {
		if (year != other.year) return year.CompareTo(other.year);
		if (month != other.month) return month.CompareTo(other.month);
		return day.CompareTo(other.day);
	}
	
	public static bool operator <(GameDate a, GameDate b) => a.CompareTo(b) < 0;
	public static bool operator >(GameDate a, GameDate b) => a.CompareTo(b) > 0;
	public static bool operator <=(GameDate a, GameDate b) => a.CompareTo(b) <= 0;
	public static bool operator >=(GameDate a, GameDate b) => a.CompareTo(b) >= 0;
	public static bool operator ==(GameDate a, GameDate b) => a.CompareTo(b) == 0;
	public static bool operator !=(GameDate a, GameDate b) => a.CompareTo(b) != 0;
	
	public override bool Equals(object obj) => obj is GameDate other && this == other;
	public override int GetHashCode() => HashCode.Combine(year, month, day);
	public override string ToString() => ToShortString();
}
