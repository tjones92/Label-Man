using System;
using System.Globalization;
using System.IO;

/// <summary>Optional audit hooks joining AI allocation decisions to TV demand deposits.</summary>
internal static class MediaDemandTrace {
	internal const string Switch = "--observe-media-demand-bookings";
	internal const string OverridePrefix = "--audit-tv-allocation-override=";
	private static TextWriter allocations, bookings, deposits;
	private sealed record TvOverride(string Date, string Episode, string Record, string Label, string Artist, long Expected, long Replacement);
	private static TvOverride tvOverride;
	private static bool overrideApplied;

	internal static void Open(TextWriter allocationLog, TextWriter bookingLog, TextWriter depositLog) {
		allocations = allocationLog;
		bookings = bookingLog;
		deposits = depositLog;
		allocations.WriteLine("decisionDate,allocationDay,episodeId,recordId,labelId,artistId,policy,envelopeCents,tvCents");
		bookings.WriteLine("decisionDate,episodeId,recordId,labelId,artistId,allocationDay,result,preparation,costCents,tvRemainingBeforeCents");
		deposits.WriteLine("tallyThrough,episodeId,appearanceId,repeatOfId,airDate,cityId,startMinute,repeat,syndicated,recordId,labelId,artistId,regionId,request");
	}

	internal static void Close() {
		allocations?.Dispose(); allocations = null;
		bookings?.Dispose(); bookings = null;
		deposits?.Dispose(); deposits = null;
		tvOverride = null;
		overrideApplied = false;
	}

	private static string Date(GameDate date) => FormattableString.Invariant($"{date.year:D4}-{date.month:D2}-{date.day:D2}");
	private static string DecisionDate() => Date(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);

	internal static void ConfigureTvOverride(string spec) {
		string[] fields = spec.Split(',');
		if (fields.Length != 7 || !DateOnly.TryParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
			|| Array.Exists(fields, string.IsNullOrWhiteSpace)
			|| !long.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out long expected)
			|| !long.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out long replacement)
			|| expected == replacement)
			throw new ArgumentException("Expected date,episodeId,recordId,labelId,artistId,expectedTvCents,replacementTvCents.");
		tvOverride = new TvOverride(fields[0], fields[1], fields[2], fields[3], fields[4], expected, replacement);
	}

	/// <summary>Move cents between TV and reserve for one traced allocation. The envelope and other lines stay balanced.</summary>
	internal static void ApplyTvOverride(AiMediaAllocation a, string episodeId) {
		TvOverride rule = tvOverride;
		if (rule == null || rule.Record != a.RecordId || rule.Label != a.LabelId || rule.Artist != a.ArtistId
			|| rule.Episode != episodeId || rule.Date != DecisionDate()) return;
		if (overrideApplied || !a.Balanced || a.TvCents != rule.Expected || rule.Replacement > a.TvCents + a.ReserveCents)
			throw new InvalidOperationException("TV allocation override does not match an untouched, funded allocation.");
		a.ReserveCents += a.TvCents - rule.Replacement;
		a.TvCents = rule.Replacement;
		overrideApplied = true;
		Godot.GD.Print(FormattableString.Invariant($"AUDIT_TV_ALLOCATION_OVERRIDE date={rule.Date} episode={episodeId} record={a.RecordId} label={a.LabelId} artist={a.ArtistId} tvCents={rule.Expected}->{a.TvCents} reserveCents={a.ReserveCents} envelopeCents={a.EnvelopeCents}"));
	}

	internal static void ValidateTvOverrideCompleted() {
		if (tvOverride != null && !overrideApplied)
			throw new InvalidOperationException("The configured TV allocation override never matched.");
	}

	internal static void Allocated(AiMediaAllocation a, string episodeId) {
		if (allocations == null) return;
		allocations.WriteLine(FormattableString.Invariant($"{DecisionDate()},{a.Day},{episodeId},{a.RecordId},{a.LabelId},{a.ArtistId},{a.Policy},{a.EnvelopeCents},{a.TvCents}"));
	}

	internal static void Booking(AiMediaAllocation a, string episodeId, string result, MediaPreparation? prep = null, long? cost = null) {
		if (bookings == null) return;
		bookings.WriteLine(FormattableString.Invariant($"{DecisionDate()},{episodeId},{a.RecordId},{a.LabelId},{a.ArtistId},{a.Day},{result},{prep},{cost},{a.TvRemaining}"));
	}

	internal static void Deposit(MediaAppearance a, MediaAiring airing, GameDate weekEnd, string regionId, float request) {
		if (deposits == null) return;
		deposits.WriteLine(FormattableString.Invariant($"{Date(weekEnd)},{a.EpisodeId},{a.Id},{a.RepeatOfId},{Date(airing.Date)},{airing.CityId},{airing.StartMinute},{airing.Repeat},{airing.Syndicated},{a.RecordId},{a.LabelId},{a.ArtistId},{regionId},{request:R}"));
	}
}
