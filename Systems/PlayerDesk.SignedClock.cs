using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// SIGNED AS A CLOCK (SimTools/SigningRealismBreakdown.md §3.4, step 6).
//
// An act on another label isn't a dead end; it's a deal with a date on it. In The Talk the Street reads their paper:
// which label, how many sides, what charted, when it's up, and at 5 what the label will likely do. "I'll keep an eye on
// you" files them here. As the deal comes up, the Street tells you so, with its read of the label's decision. When the
// label lets them go, an act that knows you (one you were watching) or that's friendly with you (one you weren't) rings
// the office first, leaves a number, and keeps other labels off for a week while it waits to hear from you.
//
// Watch and wait only: no poaching mid-contract (§5 decision 3). The read is a read, not an oracle: it follows the AI's
// renewal rule (RosterManager.ShouldResignArtist) but not its money or its loyalty roll. The only thing that touches the
// AI is the hold on an act that called you, the same hold a handshake sets, and only in a world with a player label.
// ============================================================================================

/// <summary>An act on another label that the desk is keeping an eye on, or one that has called since its deal ended.</summary>
public sealed class SignedWatch {
	public string ArtistId;
	/// <summary>The label whose deal is being watched (it changes if the act leaves for a bidder).</summary>
	public string LabelId;
	/// <summary>You said you'd keep an eye on them. False for an act that called without being watched.</summary>
	public bool Watching;
	/// <summary>The scene day you said it: they remember where you stood then.</summary>
	public int SinceDay;
	/// <summary>The deal (its chart-week expiry) the Street last reminded you about. One reminder per deal.</summary>
	public int RemindedExpiresWeek;
	/// <summary>The scene day the label let them go, and the day they'll wait to until, when they called you.</summary>
	public int? CalledDay, HeldUntilDay;
	public string LetGoBy;
}

/// <summary>The Street's read of a signed act's paper (§3.4). The read and the likely decision are said in its voice.</summary>
public sealed class SignedRead {
	public string LabelName;
	/// <summary>The term's end, or null when the deal is on a year only.</summary>
	public GameDate? Expires;
	public int WeeksLeft;
	/// <summary>"they'll keep them", "they'll let them go", or "could go either way".</summary>
	public bool? LikelyKept;
	public string Why;
	public string Paper, Clock, Likely;
}

public partial class PlayerDesk {
	public const int MaxSignedWatches = 6;
	/// <summary>The Street mentions a watched act's deal this many weeks before its term is up.</summary>
	public const int SignedReminderWeeks = 8;

	private readonly List<SignedWatch> signedWatches = new();
	public IReadOnlyList<SignedWatch> SignedWatches => signedWatches;
	public SignedWatch SignedWatchFor(string artistId) => signedWatches.FirstOrDefault(w => w.ArtistId == artistId);
	public bool IsWatchingSigned(string artistId) => SignedWatchFor(artistId)?.Watching == true;

	/// <summary>An act that rang you when its label let it go, while it's still waiting on you. Null otherwise.</summary>
	public SignedWatch FirstCall(SimulatedArtist act) =>
		act != null && string.IsNullOrEmpty(act.labelId) && SignedWatchFor(act.artistId) is { CalledDay: not null, HeldUntilDay: { } until } w && SceneDay <= until ? w : null;

	/// <summary>The Street's read of an act's deal with another label: its paper, its clock, and what the label will likely
	/// do when it's up. A pure read; null for a free act or one on your own label.</summary>
	public SignedRead ReadSignedElsewhere(SimulatedArtist act) {
		if (act == null || string.IsNullOrEmpty(act.labelId) || act.labelId == Label?.labelId) return null;
		var label = ChartManager.Instance?.GetLabelById(act.labelId);
		var r = new SignedRead { LabelName = label?.labelName ?? ChartManager.Instance?.GetLabelName(act.labelId) ?? "a label" };
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (act.contractExpiresWeek > 0) {
			r.WeeksLeft = Math.Max(0, act.contractExpiresWeek - week);
			r.Expires = today.AddDays(r.WeeksLeft * 7);
		} else r.WeeksLeft = Math.Max(0, (act.contractExpiresYear - today.year) * 52);

		string sides = act.contractReleases switch { 0 => "Nothing out yet", 1 => "One side out", _ => $"{Plural.Of(act.contractReleases, "side")} out" };
		string chart = act.contractTop40Hits > 0 ? $"{(act.contractTop40Hits == 1 ? "a" : act.contractTop40Hits.ToString())} Top 40 hit{(act.contractTop40Hits == 1 ? "" : "s")}"
			: act.contractChartedRecords > 0 ? "something charted"
			: act.contractRegionalBreakouts > 0 ? "a regional break, nothing national"
			: act.contractReleases > 0 ? "nothing charted" : null;
		r.Paper = $"They're with {r.LabelName}. {sides}{(chart != null ? ", " + chart : "")}.";
		string when = r.Expires is { } e ? $"{e.MonthName} {e.year}" : act.contractExpiresYear > 0 ? act.contractExpiresYear.ToString() : null;
		bool owed = act.contractSinglesObligation > act.contractReleases;
		r.Clock = when == null ? null : r.WeeksLeft == 0 ? "Their deal's up now."
			: $"Deal's up in {when}{(owed ? $", or sooner if they deliver the {act.contractSinglesObligation} sides they owe" : "")}.";

		// What the label will likely do: RosterManager.ShouldResignArtist, read from what anyone could see.
		(bool? kept, string why) likely = act.careerState switch {
			CareerState.Star or CareerState.Superstar => (true, "they're a star"),
			CareerState.Declining => (false, "they're on the way down"),
			CareerState.Rising when act.momentum > 0.2f => (true, "they're going up"),
			CareerState.Established when act.consecutiveFlops < 2 => (true, "they're still selling"),
			_ when act.contractTop40Hits > 0 => (true, "a hit under this deal"),
			CareerState.NewSigning when act.contractReleases >= 2 && act.contractChartedRecords == 0 && act.contractRegionalBreakouts == 0 &&
				!act.HasRecoupedCurrentContract() => (false, "two sides and nothing to show for it"),
			_ => (null, act.contractReleases == 0 ? "they've barely recorded" : "they've done enough to be kept and not enough to be sure")
		};
		(r.LikelyKept, r.Why) = likely;
		r.Likely = r.LikelyKept switch {
			true => $"{r.LabelName} will want them back: {r.Why}.",
			false => $"I'd bet {r.LabelName} lets them go: {r.Why}.",
			_ => $"Could go either way with {r.LabelName}: {r.Why}."
		};
		return r;
	}

	/// <summary>"I'll keep an eye on you." Free. Null reason when you can.</summary>
	public string WatchSignedBlocked(SimulatedArtist act) {
		if (Label == null || act == null) return "Unavailable.";
		if (string.IsNullOrEmpty(act.labelId) || act.labelId == Label.labelId) return "They're not on another label.";
		if (IsWatchingSigned(act.artistId)) return "You're already keeping an eye on them.";
		return signedWatches.Count(w => w.Watching) >= MaxSignedWatches ? $"You can keep an eye on {MaxSignedWatches} signed acts at once. Let one go first." : null;
	}

	public bool WatchSigned(string artistId, out string message) {
		var act = ArtistManager.Instance?.GetArtist(artistId);
		message = WatchSignedBlocked(act);
		if (message != null) return false;
		var w = SignedWatchFor(artistId);
		if (w == null) signedWatches.Add(w = new SignedWatch { ArtistId = artistId });
		w.Watching = true; w.SinceDay = SceneDay; w.LabelId = act.labelId;
		var read = ReadSignedElsewhere(act);
		message = read.Clock != null ? $"You'll keep an eye on {act.stageName}. {read.Clock}" : $"You'll keep an eye on {act.stageName}.";
		Note(message); Changed?.Invoke(); return true;
	}

	public bool UnwatchSigned(string artistId, out string message) {
		var w = SignedWatchFor(artistId);
		var act = ArtistManager.Instance?.GetArtist(artistId);
		if (w is not { Watching: true }) { message = "You weren't keeping an eye on them."; return false; }
		signedWatches.Remove(w);
		message = $"You stop keeping tabs on {act?.stageName ?? "them"}.";
		Changed?.Invoke(); return true;
	}

	/// <summary>Whether this act calls you when its label lets it go: one you were watching that knew your face when you said
	/// so (or does now), or one friendly with you today. Never one that feels let down.</summary>
	private bool WouldCallFirst(SimulatedArtist act, SignedWatch w, out RegardRead regard) {
		regard = ReadRegard(act);
		if (regard == null || regard.LetDown || !HasScoutingEvidence(act.artistId)) return false;
		if (w?.Watching == true) {
			var then = ReadRegard(act, w.SinceDay);
			if (then.Score > regard.Score) regard = then;
			return regard.Level >= RegardLevel.KnowYourFace;
		}
		return regard.Level >= RegardLevel.Friendly;
	}

	/// <summary>RosterManager.ContractEndObserver: how an AI contract ended. Runs inside the AI's weekly pass, after the
	/// decision, so an act that calls is held before any other label's market sees it.</summary>
	private void OnAiContractEnded(AILabel label, SimulatedArtist act, string outcome) {
		if (Label == null || act == null || label == null || label.labelId == Label.labelId) return;
		var w = SignedWatchFor(act.artistId);
		bool watching = w?.Watching == true;
		switch (outcome) {
			case "renewed":
				if (watching && ReadSignedElsewhere(act)?.Clock is { } clock)
					Note($"{label.labelName} picked up {Owned(act.stageName)} option. {clock}");
				return;
			case "bidder":
				if (watching) {
					w.LabelId = act.labelId;
					Note($"{act.stageName} left {label.labelName} for {ChartManager.Instance?.GetLabelName(act.labelId) ?? "another label"} when their deal came up.");
				}
				return;
		}
		if (!string.IsNullOrEmpty(act.labelId) || act.lifecycleStatus != ArtistLifecycleStatus.Active) return;
		if (!WouldCallFirst(act, w, out var regard)) {
			if (watching) {
				signedWatches.Remove(w);
				Note($"{label.labelName} let {act.stageName} go. They're on the market now, and they didn't call.");
				Changed?.Invoke();
			}
			return;
		}
		if (w == null) signedWatches.Add(w = new SignedWatch { ArtistId = act.artistId, LabelId = label.labelId });
		int today = SceneDay;
		w.CalledDay = today; w.HeldUntilDay = today + HandshakeDays; w.LetGoBy = label.labelName;
		RosterManager.Instance?.SetPlayerHold(act.artistId, LocalSceneRoomService.Date(w.HeldUntilDay.Value));
		var caller = NumberHolder(act);
		if (caller != null && ActNumber(act.artistId) == null)
			ActNumbers.Add(new() { ArtistId = act.artistId, PersonId = caller.personId, PersonName = caller.FullName,
				Source = $"{caller.FullName}, when {label.labelName} let them go", Day = today });
		string remembered = RegardOpener(regard, today);
		string until = LocalSceneRoomService.Date(w.HeldUntilDay.Value).ToHeadlineString();
		Note($"{caller?.FullName ?? act.stageName} rang the office: {label.labelName} didn't pick up {Owned(act.stageName)} option. " +
			$"\"We wanted you to hear it from us first.{(remembered != null ? " " + remembered : "")}\" They'll hold off other labels until {until}.");
		FlagSkipStop($"{act.stageName} called the office", EventType.IncomingCall, officeOnly: true);
		Changed?.Invoke();
	}

	/// <summary>Daily: the Street's reminder as a watched deal comes up, and the end of a first call's wait.</summary>
	private void AdvanceSignedWatches(GameDate date) {
		if (Label == null || signedWatches.Count == 0) return;
		int today = LocalSceneRoomService.Day(date), street = InstinctScore(InstinctProfile, ExecutiveVoice.Street);
		foreach (var w in signedWatches.ToList()) {
			var act = ArtistManager.Instance?.GetArtist(w.ArtistId);
			if (act == null || act.lifecycleStatus != ArtistLifecycleStatus.Active) { Drop(w); continue; }
			if (act.labelId == Label.labelId) { Drop(w); continue; } // signed with you
			if (w.CalledDay != null) {
				if (!string.IsNullOrEmpty(act.labelId)) { Drop(w); continue; } // signed elsewhere after all (a hold lapsed, or never set)
				if (w.HeldUntilDay is { } until && today > until) {
					Drop(w);
					Note($"{act.stageName} waited to hear from you. They're talking to other labels now.");
				}
				continue;
			}
			if (!w.Watching || string.IsNullOrEmpty(act.labelId)) continue;
			w.LabelId = act.labelId;
			var read = ReadSignedElsewhere(act);
			if (read == null || act.contractExpiresWeek <= 0 || read.WeeksLeft > SignedReminderWeeks || w.RemindedExpiresWeek == act.contractExpiresWeek) continue;
			w.RemindedExpiresWeek = act.contractExpiresWeek;
			string clock = read.Clock ?? $"Their deal with {read.LabelName} is nearly up.";
			Note($"{act.stageName}: {clock}" + (street >= 3 ? " The Street: " + read.Likely : ""));
		}

		void Drop(SignedWatch w) {
			signedWatches.Remove(w);
			if (w.HeldUntilDay != null) RosterManager.Instance?.ClearPlayerHold(w.ArtistId);
		}
	}

	/// <summary>A line for the act's card: the deal you're watching, or the call you haven't answered. Null otherwise.</summary>
	public string SignedWatchSummary(string artistId) {
		var w = SignedWatchFor(artistId);
		var act = ArtistManager.Instance?.GetArtist(artistId);
		if (w == null || act == null) return null;
		if (FirstCall(act) is { } called)
			return $"They called when {called.LetGoBy ?? "their label"} let them go. They'll wait to hear from you until {LocalSceneRoomService.Date(called.HeldUntilDay.Value).ToHeadlineString()}.";
		if (!w.Watching || ReadSignedElsewhere(act) is not { } read) return null;
		bool street = InstinctScore(InstinctProfile, ExecutiveVoice.Street) >= 3;
		return $"You're keeping an eye on them at {read.LabelName}.{(read.Clock != null ? " " + read.Clock : "")}{(street ? " The Street: " + read.Likely : "")}";
	}

	/// <summary>"The Ridge Fiddlers'", "Del Shannon's".</summary>
	private static string Owned(string name) => name.EndsWith("s", StringComparison.Ordinal) ? name + "'" : name + "'s";

	// ---- save --------------------------------------------------------------------------------------------------------
	private List<SignedWatch> CaptureSignedWatches() => signedWatches.Count == 0 ? null : LocalSceneRoomService.Copy(signedWatches);

	private static bool ValidateSignedWatches(List<SignedWatch> saved, out string message) {
		message = "The saved signed-act watch list is invalid; no player state was restored.";
		if (saved == null) return true; // older saves watch nobody
		var ids = new HashSet<string>(StringComparer.Ordinal);
		return saved.All(w => w != null && !string.IsNullOrEmpty(w.ArtistId) && ids.Add(w.ArtistId) && w.SinceDay >= 0 &&
			(w.CalledDay == null) == (w.HeldUntilDay == null) && (w.CalledDay == null || w.HeldUntilDay >= w.CalledDay) && (w.Watching || w.CalledDay != null));
	}

	/// <summary>Restores the list and re-sets the holds of acts still waiting on your call (RestoreState clears every hold first).</summary>
	private void RestoreSignedWatches(List<SignedWatch> saved) {
		signedWatches.Clear();
		if (saved != null) signedWatches.AddRange(LocalSceneRoomService.Copy(saved));
		foreach (var w in signedWatches.Where(w => w.HeldUntilDay != null))
			RosterManager.Instance?.SetPlayerHold(w.ArtistId, LocalSceneRoomService.Date(w.HeldUntilDay.Value));
	}

	// ---- fixture access, never rendered --------------------------------------------------------------------------------
	internal void ContractEndedForCheck(AILabel label, SimulatedArtist act, string outcome) => OnAiContractEnded(label, act, outcome);
	internal void AdvanceSignedWatchesForCheck() => AdvanceSignedWatches(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
}
