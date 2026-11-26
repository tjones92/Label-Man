using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>How an act regards your label, as they'd put it. Read from what you've done, not stored.</summary>
public enum RegardLevel { LetDown, Stranger, KnowYourFace, Friendly, TrustYou }

/// <summary>One thing you did that the act remembers.</summary>
public enum RegardSourceKind { HeardThem, SatDown, Session, GotThemANight, MissedTheirDemo, LetTheirYesLapse, CancelledOnThem }

public sealed class RegardContact {
	public RegardSourceKind Kind;
	public int Day;
	public int Points;
	/// <summary>The room or the place, for the act's line.</summary>
	public string Where;
}

/// <summary>The act's side of the relationship (SimTools/SigningRealismBreakdown.md §3.2, step 4).</summary>
public sealed class RegardRead {
	public int Score;
	public RegardLevel Level;
	public int LastContactDay = -1;
	public List<RegardContact> Contacts = new();
	public bool Has(RegardSourceKind kind) => Contacts.Any(c => c.Kind == kind);
	public bool LetDown => Level == RegardLevel.LetDown;
}

/// <summary>
/// Regard: the relationship, read from the verbs the player already has (§3.2). Nothing is stored and nothing is drawn:
/// the ledger is the label's own records of the act, so an older save reads the same way it would have been built.
///
/// What counts, in points: each different night you heard them play (+1, at most three nights); an hour you sat down with
/// them at an arranged meeting (+1, once); a studio session together, the rehearsal visit you arranged or the demo you
/// produced (+2 for the first, +1 for a second); a night you got them from a booker that they played (+2 for the first,
/// +1 for a second). What costs: a demo you didn't turn up to produce (-2), a yes you let lapse by never acting on it (-2),
/// an arranged meeting you cancelled (-1). A bad turn fades after half a year.
///
/// Time apart: the good points fade by one for every <see cref="RegardFadeDays"/> past <see cref="RegardGraceDays"/>
/// since you last did anything with them, but never below zero; neglect isn't an insult.
///
/// What it does (PlayerDesk.DirectApproach): a careful act's request is met when regard reaches what it asked for, so
/// hearing them twice still works, and so does one night and a sit-down, or a single rehearsal. An act dropped before
/// wants a little more. A hungry act you let down wants you back in the room before it says yes again. A comfortable act
/// that trusts you will hear you out, but its reason still stands at the table: it talks terms rather than signing what
/// it's handed. Regard never makes a comfortable act hungry; the reason has to change for that.
/// Authored values, not sourced.
/// </summary>
public partial class PlayerDesk {
	public const int RegardNightsCap = 3;
	public const int RegardGraceDays = 30, RegardFadeDays = 45, RegardGrudgeDays = 182;
	/// <summary>Regard a careful act asks for before it says yes, and an act that was dropped before.</summary>
	public const int CarefulRegard = 2, BurnedRegard = 3;
	/// <summary>A hungry act you let down says yes again once regard is back to this: one night back in the room mends a single bad turn after a night heard.</summary>
	public const int MendedRegard = 0;
	/// <summary>A comfortable act will hear you out from here.</summary>
	public const int HearingRegard = 5;

	/// <summary>How the act regards your label today. Null without a label or an act.</summary>
	public RegardRead ReadRegard(SimulatedArtist artist) {
		if (Label == null || artist == null) return null;
		int today = SceneDay;
		var r = new RegardRead();
		var obs = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artist.artistId)?.Observations ?? new();

		foreach (var night in obs.Where(o => o.Kind == ScoutingEvidenceKind.Performance)
			.GroupBy(o => o.OccurredDay ?? o.AcquiredDay).OrderBy(g => g.Key).Take(RegardNightsCap))
			r.Contacts.Add(new() { Kind = RegardSourceKind.HeardThem, Day = night.Key, Points = 1, Where = night.First().RoomName });

		if (obs.Where(o => o.Kind == ScoutingEvidenceKind.MeetingStatement && o.EventId?.EndsWith("|attended") == true)
			.OrderBy(o => o.AcquiredDay).FirstOrDefault() is { } sat)
			r.Contacts.Add(new() { Kind = RegardSourceKind.SatDown, Day = sat.AcquiredDay, Points = 1, Where = sat.RoomName });
		foreach (var cancelled in obs.Where(o => o.Kind == ScoutingEvidenceKind.MeetingStatement && o.EventId?.EndsWith("|cancelled") == true &&
			o.SourceId == Label.labelId))
			r.Contacts.Add(new() { Kind = RegardSourceKind.CancelledOnThem, Day = cancelled.AcquiredDay, Points = -1 });

		int sessions = 0;
		foreach (var s in obs.Where(o => o.Kind is ScoutingEvidenceKind.RehearsalSample or ScoutingEvidenceKind.DemoSample)
			.OrderBy(o => o.OccurredDay ?? o.AcquiredDay).Take(2))
			r.Contacts.Add(new() { Kind = RegardSourceKind.Session, Day = s.OccurredDay ?? s.AcquiredDay, Points = sessions++ == 0 ? 2 : 1, Where = s.RoomName });

		int nights = 0;
		foreach (var b in LocalSceneRoomService.Bookings.Where(b => b.ArtistId == artist.artistId && b.LabelId == Label.labelId &&
			b.Status == SceneBillStatus.Performed && b.Day <= today).OrderBy(b => b.Day).Take(2))
			r.Contacts.Add(new() { Kind = RegardSourceKind.GotThemANight, Day = b.Day, Points = nights++ == 0 ? 2 : 1, Where = SceneRoomCatalog.Get(b.RoomId)?.Name });

		foreach (var job in demoJobs.Where(j => j.ArtistId == artist.artistId && j.Status == DemoStatus.LabelMissed))
			r.Contacts.Add(new() { Kind = RegardSourceKind.MissedTheirDemo, Day = job.Day, Points = -2, Where = job.RoomName });
		// A yes that ran out on you: they waited, you never came. (A yes lapsed because another label signed them isn't yours.)
		foreach (var a in scoutingKnowledge.Approaches?.Where(a => a.ArtistId == artist.artistId && a.LetLapse &&
			string.IsNullOrEmpty(artist.labelId)) ?? Enumerable.Empty<ScoutingDirectApproach>())
			r.Contacts.Add(new() { Kind = RegardSourceKind.LetTheirYesLapse, Day = AgreementHoldsUntil(a), Points = -2 });

		var good = r.Contacts.Where(c => c.Points > 0 && c.Day <= today).ToList();
		r.LastContactDay = good.Count == 0 ? -1 : good.Max(c => c.Day);
		int positive = good.Sum(c => c.Points);
		if (r.LastContactDay >= 0) {
			int apart = today - r.LastContactDay - RegardGraceDays;
			if (apart > 0) positive = Math.Max(0, positive - (apart + RegardFadeDays - 1) / RegardFadeDays);
		}
		int negative = r.Contacts.Where(c => c.Points < 0 && c.Day <= today && today - c.Day < RegardGrudgeDays).Sum(c => c.Points);
		r.Score = positive + negative;
		r.Level = r.Score < 0 ? RegardLevel.LetDown : r.Score == 0 ? RegardLevel.Stranger : r.Score < 3 ? RegardLevel.KnowYourFace
			: r.Score < HearingRegard ? RegardLevel.Friendly : RegardLevel.TrustYou;
		return r;
	}

	/// <summary>The regard a careful act asked for, by its reason: an act that was dropped before wants more.</summary>
	public static int RegardWanted(DispositionRead read) => read?.Reason == DispositionReason.DroppedBefore ? BurnedRegard : CarefulRegard;

	/// <summary>What the act says first, from the thing it remembers best (or worst, when you let it down), as the words
	/// inside the speaker's quote. Null for a stranger.</summary>
	public static string RegardOpener(RegardRead r) {
		if (r == null || r.Level == RegardLevel.Stranger) return null;
		var worst = r.Contacts.Where(c => c.Points < 0).OrderBy(c => c.Points).ThenByDescending(c => c.Day).FirstOrDefault();
		if (r.LetDown && worst != null) return worst.Kind switch {
			RegardSourceKind.MissedTheirDemo => "We booked the room and you never came.",
			RegardSourceKind.LetTheirYesLapse => "We said yes to you once, and then we never heard from you.",
			_ => "You called off on us."
		};
		var best = r.Contacts.Where(c => c.Points > 0).OrderByDescending(c => c.Points).ThenByDescending(c => c.Day).FirstOrDefault();
		if (best == null) return null;
		string day = LocalSceneRoomService.Date(best.Day).DayOfWeek.ToString();
		return best.Kind switch {
			RegardSourceKind.GotThemANight => $"You got us the night at {best.Where ?? "that room"}. Nobody's done that for us.",
			RegardSourceKind.Session => $"You came out and heard us work{(best.Where != null ? " at " + best.Where : "")}.",
			RegardSourceKind.SatDown => "You sat down with us. That counts for something.",
			_ => $"You were at {best.Where ?? "our set"} {day}, weren't you?"
		};
	}

	/// <summary>A line for the act's notebook card: where you stand with them, as they'd show it. Null for a stranger.</summary>
	public string RegardSummary(string artistId) {
		var artist = ArtistManager.Instance?.GetArtist(artistId);
		var r = ReadRegard(artist);
		if (r == null || r.Level == RegardLevel.Stranger && r.Contacts.Count == 0) return null;
		string how = r.Level switch {
			RegardLevel.LetDown => "they feel you let them down",
			RegardLevel.Stranger => "they've lost track of you",
			RegardLevel.KnowYourFace => "they know your face",
			RegardLevel.Friendly => "they're friendly with you",
			_ => "they trust you"
		};
		var done = r.Contacts.GroupBy(c => c.Kind).Select(g => g.Key switch {
			RegardSourceKind.HeardThem => g.Count() == 1 ? "heard once" : $"heard {g.Count()} nights",
			RegardSourceKind.SatDown => "sat down with them",
			RegardSourceKind.Session => g.Count() == 1 ? "a session together" : "two sessions together",
			RegardSourceKind.GotThemANight => g.Count() == 1 ? "got them a night" : "got them two nights",
			RegardSourceKind.MissedTheirDemo => "missed their demo",
			RegardSourceKind.LetTheirYesLapse => "let their yes lapse",
			_ => "cancelled a meeting"
		});
		return $"Between you: {how} ({string.Join(", ", done)}).";
	}
}

public partial class PlayerDesk {
	/// <summary>Whether an act off the market agrees to sit down with you, or to a rehearsal visit or a demo (signing step
	/// 4). Market status is the AI's plumbing; the act answers from its own disposition. A hungry or careful act you've heard
	/// says yes. A comfortable one does once it knows you (CarefulRegard): it isn't looking for a record, but it'll let a
	/// friendly face come and hear it work. An act that already said yes to you, or that you let down, still meets you.</summary>
	internal bool WouldMeetOffMarket(SimulatedArtist act) {
		if (Label == null || !IsOffTheMarket(act) || !HasScoutingEvidence(act.artistId)) return false;
		var read = ReadDisposition(act);
		return read.Kind != DealDisposition.Comfortable || ReadRegard(act).Score >= CarefulRegard;
	}
}
