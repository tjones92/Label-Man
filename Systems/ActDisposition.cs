using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>How an unsigned act feels about a record deal (SimTools/SigningRealismBreakdown.md §3.1, step 2).</summary>
public enum DealDisposition { Hungry, Careful, Comfortable }

/// <summary>Why, in one fact the player could have found out. Each names real state.</summary>
public enum DispositionReason {
	// Hungry
	DayJob, Young, OwnSongs, Ambitious,
	// Careful
	DroppedBefore, OutOfTown, NoRecords, WantsAnotherLook,
	// Comfortable
	Engagement, NoNeedForMoney, Settled, WeekendAct, LocalLiving
}

/// <summary>One read of an act's disposition toward a label. Pure: nothing is stored, nothing is drawn.</summary>
public sealed class DispositionRead {
	public DealDisposition Kind;
	public DispositionReason Reason;
	/// <summary>Ambition adjusted by money, age, songs, comfort and how the act sees the label (0..1-ish).</summary>
	public float Drive;
	public int Members, MedianAge;
	public float Ambition;
	public bool DayJobs;
	/// <summary>Members under 21: in 1960 a parent signs for them (period fact; step 3 makes it a visit).</summary>
	public List<string> Minors = new();
	/// <summary>In a group of three or more, the least ambitious member when they're well below the rest (step 3).</summary>
	public string Holdout;
	public ManagerArchetype Manager;
	/// <summary>Room name of a standing engagement, when the act holds one.</summary>
	public string EngagementRoom;
	/// <summary>A member who can live without the money, when there is one.</summary>
	public string WealthyMember;

	/// <summary>A shark, svengali or visionary negotiates hard. A local hustler is eager to deal (ManagerProfile).</summary>
	public bool DemandingManager => Manager is ManagerArchetype.Shark or ManagerArchetype.Svengali or ManagerArchetype.Visionary;
	/// <summary>A hungry act with nothing between the ask and a signature: no member under 21, no holdout, no demanding manager.</summary>
	public bool SignsTonight => Kind == DealDisposition.Hungry && Minors.Count == 0 && Holdout == null && !DemandingManager;
}

/// <summary>
/// The act's own side of the ask, replacing the keyed 65% coin (PlayerDesk.DirectApproach). Market status
/// (Latent/Seeking) stays the AI labour market's plumbing; this is what the act wants, read from real state only:
/// member ambition, day jobs (LocalSceneRoomService.FullTime), ages, how much of the set is their own, a paid standing
/// engagement, a member who doesn't need the money, a past drop, a manager, and how the act sees the label (records out
/// and a hit raise drive; being from another town is a careful act's reason, not a pull, because it would count against
/// every act a probe hears away from home). No draws, so asking twice gets the same answer until something real changes.
/// A manager shapes terms and pace, not willingness: a local hustler adds drive, a demanding one is a friction (step 3).
///
/// Thresholds fitted 2026-11-25 (SigningAvailabilityProbe, seed 1001, Detroit, 56 days, all towns: 64 / 21 / 15.5;
/// Detroit alone 76 / 11 / 13) to the agreed opening mix of about 60% hungry, 20-25% careful, 10-15% comfortable.
/// Design inputs; check 1961, 1963 and a second seed before locking them.
/// </summary>
public static class ActDisposition {
	public const float ComfortableBelow = 0.48f, HungryFrom = 0.59f;
	public const int MinorAge = 21;
	public const float HoldoutAmbition = 0.35f;

	// Drive adjustments. Design inputs, measured in the probe.
	public const float DayJobPush = .08f, YouthPush = .06f, AgePull = .08f, OwnSongsPush = .05f, HustlerPush = .05f,
		EngagementPull = .10f, MoneyPull = .08f, ReleasedPush = .02f, HitPush = .05f;

	/// <param name="label">The label asking (the player's). Null reads the act with no label in view.</param>
	/// <param name="originals">Share of the heard set that's their own; null when nothing was heard with titles.</param>
	public static DispositionRead Read(SimulatedArtist act, AILabel label, int day, float? originals) {
		if (act == null) throw new ArgumentNullException(nameof(act));
		int year = LocalSceneRoomService.Date(day).year;
		var people = (act.members ?? new()).Where(m => m != null && m.isActive && m.lifeState == MemberLifeState.Active).ToList();
		var aged = people.Where(m => m.birthYear > 0).OrderBy(m => m.GetAge(year)).ToList();
		var r = new DispositionRead {
			Members = people.Count,
			MedianAge = aged.Count == 0 ? 25 : aged[aged.Count / 2].GetAge(year),
			Ambition = people.Count == 0 ? .5f : people.Average(m => m.ambition),
			DayJobs = !LocalSceneRoomService.FullTime(act, day),
			Manager = act.manager,
		};
		r.Minors.AddRange(aged.Where(m => m.GetAge(year) < MinorAge).Select(Name));
		if (people.Count >= 3) {
			var low = people.OrderBy(m => m.ambition).ThenBy(m => m.personId, StringComparer.Ordinal).First();
			if (low.ambition < HoldoutAmbition) r.Holdout = Name(low);
		}
		string engagement = LocalSceneRoomService.EngagementRoom(act.artistId, day);
		if (engagement != null) r.EngagementRoom = SceneRoomCatalog.Get(engagement)?.Name ?? "a room in town";
		var rich = people.Where(m => MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm)
			.OrderByDescending(MemberWealthService.Norm).ThenBy(m => m.personId, StringComparer.Ordinal).FirstOrDefault();
		if (rich != null) r.WealthyMember = Name(rich);

		bool young = r.MedianAge <= 22, settled = r.MedianAge >= 32, ownSongs = originals >= .5f;
		bool engaged = r.EngagementRoom != null && !r.DayJobs;
		bool outOfTown = label != null && !string.IsNullOrEmpty(label.homeCityId) &&
			LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId) is { } scene && scene != label.homeCityId;
		bool released = label?.totalReleases > 0, hit = label?.top40Hits > 0;

		float drive = r.Ambition;
		if (r.DayJobs) drive += DayJobPush;
		if (young) drive += YouthPush; else if (settled) drive -= AgePull;
		if (ownSongs) drive += OwnSongsPush;
		if (act.manager == ManagerArchetype.LocalHustler) drive += HustlerPush;
		if (engaged) drive -= EngagementPull;
		if (rich != null) drive -= MoneyPull;
		if (released) drive += ReleasedPush;
		if (hit) drive += HitPush;
		r.Drive = drive;

		bool dropped = act.contractSequence > 0 &&
			act.lastDropReason is ArtistDropReason.Performance or ArtistDropReason.PerformanceExhaustion;
		if (drive < ComfortableBelow) {
			r.Kind = DealDisposition.Comfortable;
			r.Reason = engaged ? DispositionReason.Engagement : rich != null ? DispositionReason.NoNeedForMoney
				: settled ? DispositionReason.Settled : r.DayJobs ? DispositionReason.WeekendAct : DispositionReason.LocalLiving;
		} else if (dropped || drive < HungryFrom) {
			r.Kind = DealDisposition.Careful;
			r.Reason = dropped ? DispositionReason.DroppedBefore : outOfTown ? DispositionReason.OutOfTown
				: !released ? DispositionReason.NoRecords : DispositionReason.WantsAnotherLook;
		} else {
			r.Kind = DealDisposition.Hungry;
			r.Reason = r.DayJobs ? DispositionReason.DayJob : young ? DispositionReason.Young
				: ownSongs ? DispositionReason.OwnSongs : DispositionReason.Ambitious;
		}
		return r;
	}

	private static string Name(Musician m) => string.IsNullOrWhiteSpace(m.firstName) ? m.FullName?.Trim() ?? "one of them" : m.firstName;

	/// <summary>What the act's side says, in a sentence. <paramref name="who"/> is whoever does the talking.</summary>
	public static string Line(DispositionRead r, SimulatedArtist act, string who, string labelTown) {
		string they = act.stageName;
		return r.Reason switch {
			DispositionReason.DayJob => $"{who}: \"We're all working jobs we'd quit tomorrow for a record. Yes, we'd hear an offer.\"",
			DispositionReason.Young => $"{who}: \"We've been waiting for somebody to ask. Yes.\"",
			DispositionReason.OwnSongs => $"{who}: \"We've got songs nobody's heard yet. We want them on a record. Yes.\"",
			DispositionReason.Ambitious => $"{who}: \"We want a record. We'd hear an offer.\"",
			DispositionReason.DroppedBefore => $"{who}: \"We've been on a label before, and they dropped us. We'll want to see you a few times, and see you put some work in, before we talk money.\"",
			DispositionReason.OutOfTown => $"{who}: \"You're from {labelTown ?? "out of town"}? We play here. Come back and hear us again, and we'll talk.\"",
			DispositionReason.NoRecords => $"{who}: \"We've never heard a record of yours. Come and hear us again, or come out to a rehearsal and hear what we do with a song, and we'll talk.\"",
			DispositionReason.WantsAnotherLook => $"{who}: \"We'd want to know you better first. Come and hear us again.\"",
			DispositionReason.Engagement => $"{who}: \"We've got {r.EngagementRoom} steady, and it pays. We're not giving that up for a maybe.\"",
			DispositionReason.NoNeedForMoney => $"{who}: \"We do this because we like it. {r.WealthyMember} doesn't need the money, and the rest of us don't want the road.\"",
			DispositionReason.Settled => $"{who}: \"We've got families. This is what we do on weekends, and it suits us.\"",
			DispositionReason.WeekendAct => $"{who}: \"We keep our jobs and play weekends. We like it that way.\"",
			_ => $"{who}: \"We make a living in the rooms here. We don't want the road.\""
		};
	}

	/// <summary>For a comfortable act: what could change their mind, honestly. Null for the other dispositions.</summary>
	public static string WhatWouldChange(DispositionRead r) => r.Kind != DealDisposition.Comfortable ? null : r.Reason switch {
		DispositionReason.Engagement => $"That could change when the {r.EngagementRoom} engagement ends, or if your label has a hit. If they come to trust you, they might at least hear you out.",
		_ => "A hit on your label might change their minds. If they come to trust you, they might at least hear you out."
	};

	/// <summary>A few words for the notebook card.</summary>
	public static string Short(DispositionRead r) => r.Reason switch {
		DispositionReason.Engagement => "happy with their engagement",
		DispositionReason.NoNeedForMoney => "don't need the money",
		DispositionReason.Settled => "settled, families",
		DispositionReason.WeekendAct => "happy as a weekend act",
		DispositionReason.LocalLiving => "happy working the rooms here",
		DispositionReason.DroppedBefore => "dropped before, careful",
		DispositionReason.OutOfTown => "wary of an out-of-town label",
		DispositionReason.NoRecords => "never heard a record of yours",
		DispositionReason.WantsAnotherLook => "want to know you better",
		_ => "want a record"
	};
}
