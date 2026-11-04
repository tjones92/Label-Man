using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Plain-language wording for the media layer: clock times, schedules, audiences. Text only; it never decides anything (§9.4).</summary>
public static class MediaText {
	public static string Clock(int minute) {
		minute = ((minute % 1440) + 1440) % 1440;
		int h = minute / 60, m = minute % 60;
		string ampm = h < 12 ? "a.m." : "p.m.";
		int h12 = h % 12 == 0 ? 12 : h % 12;
		return m == 0 ? $"{h12} {ampm}" : $"{h12}:{m:00} {ampm}";
	}

	public static string Days(DayOfWeek[] days) {
		if (days.Length == 5 && !days.Contains(DayOfWeek.Saturday) && !days.Contains(DayOfWeek.Sunday)) return "Weekdays";
		if (days.Length == 6 && !days.Contains(DayOfWeek.Sunday)) return "Monday to Saturday";
		return string.Join(" and ", days.Select(d => d + "s"));
	}

	public static string NetworkName(TvProgram program) => program.Network switch {
		TvNetwork.Local => program.Station ?? "local",
		TvNetwork.Syndicated => "syndicated",
		_ => program.Network.ToString(),
	};

	/// <summary>A program's slot as seen from one city: its local clock, not the network's.</summary>
	public static string SlotIn(TvProgram program, BroadcastScheduleVersion version, string cityId) {
		int local = program.Network == TvNetwork.Local ? version.StartMinute
			: BroadcastExposure.LocalStart(version.Carriage, version.StartMinute, MediaOutletCatalog.ZoneOf(cityId));
		string approx = version.Confidence == SourceConfidence.Approximate ? " (hour approximate)" : "";
		return $"{Days(version.Days)} at {Clock(local)}{approx}, {version.DurationMinutes} minutes, {(version.Broadcast == BroadcastMethod.Live ? "live" : "on tape")}";
	}

	/// <summary>An audience as a rounded range, never a precise rating (§4.5).</summary>
	public static string Audience(double people) {
		if (people >= 1_000_000) {
			double low = Math.Max(1, Math.Round(people * 0.8 / 1_000_000)), high = Math.Max(low + 1, Math.Round(people * 1.2 / 1_000_000));
			return $"{low:0}-{high:0} million";
		}
		if (people >= 10_000) return $"perhaps {Math.Round(people / 10_000) * 10_000:N0}";
		return "a few thousand";
	}

	public static string Verb(PerformanceMethod method) => method switch {
		PerformanceMethod.Mime => "mime",
		PerformanceMethod.LiveBandAndVocal => "play",
		PerformanceMethod.HouseOrchestra => "sing with the orchestra",
		_ => "sing",
	};

	public static string ProducerWants(TvProgram program) => program.Kind switch {
		TvProgramKind.LocalTeenDance => "A clean record the dancers like, an act that arrives on time, a short usable segment, and some sign local teenagers care. National chart status helps but isn't required.",
		TvProgramKind.NationalTeen => program.DiscoverySlot
			? $"Current danceable singles, mostly from the national top {program.ChartCeiling}, plus one slot for a record breaking in a region before the chart catches it."
			: $"Current singles from the national top {program.ChartCeiling}, with a presentable act that can work the cameras.",
		TvProgramKind.TalkShow => "A singer the audience already knows and a comic who can sit on the couch afterwards. A current record helps; the host books names.",
		TvProgramKind.MusicShow => "Acts its own audience follows, whether or not the pop chart shows them, who can play or sing their songs on the night.",
		_ when program.Bills.Any(b => b.Standing > 0) =>
			"Names the whole family already knows, singing what they're known for, around a current hit or two" +
			(program.ChartCeiling > 0 ? $" from the top {program.ChartCeiling}" : "") + ". Strong production and standards authority.",
		_ => $"Records the whole family has heard, usually from the top {program.ChartCeiling}, by an act that can carry a polished segment. Strong production and standards authority.",
	};
}
