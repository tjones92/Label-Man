using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Something the Label Man did that a manager remembers.</summary>
public enum RapportSourceKind {
	/// <summary>Came to the sit-down (or the family's Sunday dinner, when the family is the manager) at the booked hour.</summary>
	KeptSitDown,
	/// <summary>Booked the sit-down and never came.</summary>
	MissedSitDown,
	/// <summary>Held firm on his act's paper at the table.</summary>
	HeldFirm,
	/// <summary>Pushed until the talks broke down and the act walked.</summary>
	TalksBrokeDown,
	/// <summary>Signed his act at (or within a hair of) what he asked.</summary>
	PaidHisPrice,
	/// <summary>Read, not stored: a promise to his act kept or broken (ContractCommitments).</summary>
	PromiseKept, PromiseBroken,
	/// <summary>Read, not stored: a record by his act on this label made the Top 40.</summary>
	RecordDidWell,
}

/// <summary>One stored line of the rapport ledger: player-only, saved with the scouting knowledge.</summary>
public sealed class ManagerRapportEntry {
	/// <summary>The person (<see cref="PlayerDesk.ManagerKey"/>): what he is and his name, so a club booker who manages two
	/// acts is one man.</summary>
	public string Key { get; set; }
	public string Name { get; set; }
	public string ArtistId { get; set; }
	public RapportSourceKind Kind { get; set; }
	public int Day { get; set; }
	public int Points { get; set; }
}

public enum RapportLevel { Sour, Cool, Neutral, Warm, Close }

/// <summary>Where the label stands with one manager.</summary>
public sealed class RapportRead {
	public string Key, Name;
	public ManagerArchetype Archetype;
	public int Score;
	public RapportLevel Level;
	/// <summary>Sit-downs booked and missed, still remembered (inside the grudge).</summary>
	public int Misses;
	public List<ManagerRapportEntry> Entries = new();
}

/// <summary>
/// Manager rapport (OnboardingTutorialDirective.md §4c, T5 step 2). Managers run the act's career and its paper, so the
/// label carries a relationship with each one, keyed on the person: a club booker who manages two acts is one man, and he
/// remembers you across both. The archetype the act rolled (ManagerProfile) stays the deal posture the AI reads; rapport
/// is player-only state on top of it and only ever moves the player's own talks.
///
/// What moves it, in points (authored, not sourced): a kept sit-down +1 (twice at most); a missed one by who he is (the
/// table below); holding firm at the table -1 each (-2 at most); talks pushed until the act walked -1; signing at his
/// price +1. Read from the label's records rather than stored: a promise to his act kept +1 or broken -2, and a Top 40
/// record by his act on this label +1 (twice at most). Bad turns are remembered for half a year
/// (<see cref="RegardGrudgeDays"/>), good ones for two years.
///
/// A missed sit-down, by archetype (first / again): a hustler shrugs and reschedules (-1 / -1, and he only gives up at
/// the third); a shark uses it, "you owe me", and his price at the table climbs with every miss (-1 / -1, and the second
/// time he shops the act: the yes ends); a svengali takes it personally (-3 / the yes ends, -2); a visionary cools and
/// has less patience at the table for every miss (-1 / -2, and he gives up at the third). A family that's also the
/// manager doesn't set a place twice (AppointmentBook).
///
/// What it does, in the player's own talks (applied when the table opens): patience (Sour -2, Cool -1, Warm +1, Close +2
/// rounds, a visionary one fewer for each miss) and how far he'll come (the reservation: Sour +0.06, Cool +0.03, Warm
/// -0.02, Close -0.04, plus a shark's +0.05 for each miss). He says which on the page. Who can read it: the Fixer at 3+
/// names it on the act's card; below that nothing is shown but what he says at the table.
///
/// Not built yet (later steps): the renewal ask, listening to a rival's offer, and bringing you his next act.
/// </summary>
public partial class PlayerDesk {
	public const int RapportKeptCap = 2, RapportHeldFirmCap = 2, RapportHitsCap = 2, RapportGoodDays = 730;
	public const float SharkMissPremium = .05f;
	public const float PaidHisPriceUnderAsk = .05f;

	/// <summary>The person: kind and name, so the same booker is one man across his acts.</summary>
	public static string ManagerKey(ManagerRead m) => m == null ? null : $"{m.Kind}|{(m.Name ?? "").Trim().ToLowerInvariant()}";

	/// <summary>Misses before a manager stops waiting and the yes ends, by who he is.</summary>
	public static int MissesBeforeLapseFor(ManagerArchetype archetype) => archetype switch {
		ManagerArchetype.LocalHustler or ManagerArchetype.Visionary => 3,
		_ => MissesBeforeLapse,
	};

	/// <summary>Rapport a missed sit-down costs, by who he is and how many times it's happened.</summary>
	public static int MissPoints(ManagerArchetype archetype, int missNumber) => archetype switch {
		ManagerArchetype.Svengali => missNumber <= 1 ? -3 : -2,
		ManagerArchetype.Visionary => missNumber <= 1 ? -1 : -2,
		_ => -1,
	};

	private List<ManagerRapportEntry> RapportLedger => scoutingKnowledge.Rapport ??= new();

	/// <summary>Writes one line in the ledger for the act's manager today. Nothing for an unmanaged act.</summary>
	private void AddRapport(SimulatedArtist artist, RapportSourceKind kind, int points, ManagerRead manager = null) {
		if (Label == null || artist == null) return;
		manager ??= ManagerIdentity.Read(artist, SceneDay);
		if (manager == null) return;
		RapportLedger.Add(new ManagerRapportEntry { Key = ManagerKey(manager), Name = manager.Name, ArtistId = artist.artistId,
			Kind = kind, Day = SceneDay, Points = points });
	}

	/// <summary>The acts this label has had dealings with, whose manager is this person today.</summary>
	private IEnumerable<SimulatedArtist> ActsManagedBy(string key, int day) {
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (var id in RapportLedger.Where(e => e.Key == key).Select(e => e.ArtistId)) ids.Add(id);
		foreach (var a in Label?.roster ?? new()) if (a != null) ids.Add(a.artistId);
		foreach (var d in scoutingKnowledge.Dossiers) ids.Add(d.ArtistId);
		foreach (var id in ids) {
			var act = ArtistManager.Instance?.GetArtist(id);
			if (act != null && ManagerKey(ManagerIdentity.Read(act, day)) == key) yield return act;
		}
	}

	/// <summary>Where the label stands with this manager today. Null without a label or a manager.</summary>
	public RapportRead ReadRapport(ManagerRead manager, int? asOf = null) {
		if (Label == null || manager == null) return null;
		int today = asOf ?? SceneDay;
		string key = ManagerKey(manager);
		var r = new RapportRead { Key = key, Name = manager.Name, Archetype = manager.Archetype };
		bool Remembered(ManagerRapportEntry e) => e.Day <= today && today - e.Day < (e.Points < 0 ? RegardGrudgeDays : RapportGoodDays);
		var stored = RapportLedger.Where(e => e.Key == key && Remembered(e)).ToList();
		r.Entries.AddRange(stored.Where(e => e.Kind == RapportSourceKind.KeptSitDown).OrderBy(e => e.Day).Take(RapportKeptCap));
		r.Entries.AddRange(stored.Where(e => e.Kind is RapportSourceKind.MissedSitDown or RapportSourceKind.TalksBrokeDown or RapportSourceKind.PaidHisPrice));
		r.Entries.AddRange(stored.Where(e => e.Kind == RapportSourceKind.HeldFirm).OrderByDescending(e => e.Day).Take(RapportHeldFirmCap));
		r.Misses = stored.Count(e => e.Kind == RapportSourceKind.MissedSitDown);

		var acts = ActsManagedBy(key, today).ToList();
		var actIds = new HashSet<string>(acts.Select(a => a.artistId), StringComparer.Ordinal);
		foreach (var c in contractCommitments.Where(c => actIds.Contains(c.ArtistId) && c.ResolvedDay is { } d && d <= today)) {
			var e = c.Status switch {
				ContractCommitmentStatus.Kept => new ManagerRapportEntry { Kind = RapportSourceKind.PromiseKept, Points = 1 },
				ContractCommitmentStatus.Missed => new ManagerRapportEntry { Kind = RapportSourceKind.PromiseBroken, Points = -2 },
				_ => null };
			if (e == null) continue;
			e.Key = key; e.Name = manager.Name; e.ArtistId = c.ArtistId; e.Day = c.ResolvedDay.Value;
			if (Remembered(e)) r.Entries.Add(e);
		}
		foreach (var hit in ReleasedRecords.Where(x => x?.baseRecord != null && actIds.Contains(x.baseRecord.artistId) &&
				x.peakPosition is > 0 and <= 40 && ReleaseDayOf(x.baseRecord) is { } released && released <= today)
				.OrderBy(x => ReleaseDayOf(x.baseRecord)).Take(RapportHitsCap)) {
			var e = new ManagerRapportEntry { Key = key, Name = manager.Name, ArtistId = hit.baseRecord.artistId, Kind = RapportSourceKind.RecordDidWell,
				Day = ReleaseDayOf(hit.baseRecord).Value, Points = 1 };
			if (Remembered(e)) r.Entries.Add(e);
		}
		r.Score = r.Entries.Sum(e => e.Points);
		r.Level = r.Score <= -3 ? RapportLevel.Sour : r.Score < 0 ? RapportLevel.Cool : r.Score == 0 ? RapportLevel.Neutral
			: r.Score < 3 ? RapportLevel.Warm : RapportLevel.Close;
		return r;
	}

	/// <summary>Rapport with the act's manager today, or null.</summary>
	public RapportRead ReadRapport(SimulatedArtist artist) => artist == null ? null : ReadRapport(ManagerIdentity.Read(artist, SceneDay));

	/// <summary>Rounds of patience rapport adds or takes at the table.</summary>
	public static int RapportPatience(RapportRead r) => r == null ? 0 : (r.Level switch {
		RapportLevel.Sour => -2, RapportLevel.Cool => -1, RapportLevel.Warm => 1, RapportLevel.Close => 2, _ => 0
	}) - (r.Archetype == ManagerArchetype.Visionary ? r.Misses : 0);

	/// <summary>What rapport adds to the reservation (how far he won't come): positive is a harder table.</summary>
	public static float RapportReservation(RapportRead r) => r == null ? 0f : (r.Level switch {
		RapportLevel.Sour => .06f, RapportLevel.Cool => .03f, RapportLevel.Warm => -.02f, RapportLevel.Close => -.04f, _ => 0f
	}) + (r.Archetype == ManagerArchetype.Shark ? SharkMissPremium * r.Misses : 0f);

	/// <summary>Rapport bends the player's own table when it opens: patience and how far he'll come, and he says so.</summary>
	private void ApplyRapport(ContractTalk talk) {
		if (talk?.Artist == null || ManagerIdentity.Read(talk.Artist, SceneDay) is not { } manager) return;
		var r = ReadRapport(manager);
		int patience = RapportPatience(r);
		float shift = RapportReservation(r);
		if (patience == 0 && Mathf.IsZeroApprox(shift)) return;
		talk.patienceMax = Math.Max(1, talk.patienceMax + patience);
		talk.patienceLeft = talk.patienceMax;
		talk.reservation = Mathf.Clamp(talk.reservation + shift, .5f, .97f);
		if (RapportTableLine(r, manager) is { } line) talk.outlookStated.Add(line);
	}

	/// <summary>What the manager says when he sits down, from where you stand with him. Null when there's nothing to say.</summary>
	public static string RapportTableLine(RapportRead r, ManagerRead m) {
		if (r == null || m == null) return null;
		string name = m.Name.Split(' ')[0];
		if (r.Archetype == ManagerArchetype.Shark && r.Misses > 0)
			return $"{name}: \"You owe me for {(r.Misses == 1 ? "the afternoon I sat waiting" : "the afternoons I sat waiting")}. It's on the bill.\"";
		var worst = r.Entries.Where(e => e.Points < 0).OrderBy(e => e.Points).ThenByDescending(e => e.Day).FirstOrDefault();
		var best = r.Entries.Where(e => e.Points > 0).OrderByDescending(e => e.Points).ThenByDescending(e => e.Day).FirstOrDefault();
		return r.Level switch {
			RapportLevel.Sour or RapportLevel.Cool when worst != null => worst.Kind switch {
				RapportSourceKind.MissedSitDown => $"{name}: \"I kept an afternoon for you once. I won't keep this table waiting.\"",
				RapportSourceKind.PromiseBroken => $"{name}: \"Last time you made my act a promise. Put it in writing this time.\"",
				RapportSourceKind.HeldFirm or RapportSourceKind.TalksBrokeDown => $"{name}: \"You squeezed me last time. Don't expect me to come far.\"",
				_ => $"{name} isn't in a hurry to make this easy."
			},
			RapportLevel.Warm or RapportLevel.Close when best != null => best.Kind switch {
				RapportSourceKind.RecordDidWell => $"{name}: \"You did right by my act's record. Let's not haggle all day.\"",
				RapportSourceKind.PromiseKept => $"{name}: \"You keep your word. That's rare in this business.\"",
				RapportSourceKind.PaidHisPrice => $"{name}: \"You paid fair last time. I'll be fair back.\"",
				_ => $"{name}: \"You turned up when you said you would. That counts with me.\""
			},
			_ => null
		};
	}

	/// <summary>The act's card: where you stand with their manager, as the Fixer reads it (3+). Null otherwise.</summary>
	public string RapportSummary(string artistId) {
		var artist = ArtistManager.Instance?.GetArtist(artistId);
		var m = artist == null ? null : ManagerIdentity.Read(artist, SceneDay);
		if (m == null || InstinctScore(InstinctProfile, ExecutiveVoice.Fixer) < 3) return null;
		var r = ReadRapport(m);
		if (r == null || r.Entries.Count == 0) return null;
		string how = r.Level switch {
			RapportLevel.Sour => "sore at you", RapportLevel.Cool => "cool", RapportLevel.Warm => "warm", RapportLevel.Close => "in your corner", _ => "even"
		};
		var done = r.Entries.GroupBy(e => e.Kind).Select(g => g.Key switch {
			RapportSourceKind.KeptSitDown => g.Count() == 1 ? "kept a sit-down" : "kept two sit-downs",
			RapportSourceKind.MissedSitDown => g.Count() == 1 ? "missed a sit-down" : $"missed {g.Count()} sit-downs",
			RapportSourceKind.HeldFirm => $"held firm on {m.Him}",
			RapportSourceKind.TalksBrokeDown => "talks broke down",
			RapportSourceKind.PaidHisPrice => $"paid {m.His} price",
			RapportSourceKind.PromiseKept => $"kept a promise to {m.His} act",
			RapportSourceKind.PromiseBroken => $"broke a promise to {m.His} act",
			_ => $"a record by {m.His} act did well"
		});
		return $"The Fixer: {m.Name}: {how} ({string.Join(", ", done)}).";
	}
}
