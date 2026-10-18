using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// People between acts (SimTools/BandMemberSimulationDirective.md §4.1). A person lives in exactly one place:
/// the current act's <see cref="SimulatedArtist.members"/>, or here. Past stints are frozen
/// <see cref="AlumniRecord"/>s on the old act, never a second live copy of the Musician (§2.20).
/// <para>
/// Sparse by design. On a departure or a dissolution a person enters only if they have a career to continue
/// (<see cref="HasCareerToContinue"/>); everyone else leaves music with a reasonLeft, as before. The pool is
/// the replacement market, the free agents a label can poach, and the supply a recombination draws on --
/// all of which only exist once lineup churn is live. With churn off it stays empty.
/// </para>
/// </summary>
public static class PersonPool {
	private static readonly Dictionary<string, PooledPerson> pool = new(StringComparer.Ordinal);
	private static int lastDecayWeek = int.MinValue;
	// The pool sorted by person id. Searches read it many times a week (every town's house-band players, every
	// replacement hire), and a departure season alternates joins and hires, so a join or hire edits a copy in
	// place (binary search) instead of re-sorting; an enumeration already under way keeps its own snapshot.
	// Expiry, reset and load drop it and the next read sorts afresh.
	private static PooledPerson[] ordered;
	private static int IndexOf(PooledPerson[] sorted, string personId) {
		int lo = 0, hi = sorted.Length - 1;
		while (lo <= hi) {
			int mid = lo + ((hi - lo) >> 1), order = StringComparer.Ordinal.Compare(sorted[mid].person.personId, personId);
			if (order == 0) return mid;
			if (order < 0) lo = mid + 1; else hi = mid - 1;
		}
		return ~lo;
	}
	private static PooledPerson[] WithEntry(PooledPerson[] sorted, PooledPerson entry) {
		int at = IndexOf(sorted, entry.person.personId);
		if (at >= 0) { var replaced = (PooledPerson[])sorted.Clone(); replaced[at] = entry; return replaced; }
		at = ~at;
		var grown = new PooledPerson[sorted.Length + 1];
		Array.Copy(sorted, grown, at); grown[at] = entry; Array.Copy(sorted, at, grown, at + 1, sorted.Length - at);
		return grown;
	}
	private static PooledPerson[] WithoutId(PooledPerson[] sorted, string personId) {
		int at = IndexOf(sorted, personId);
		if (at < 0) return null;   // out of step with the pool: rebuild on the next read
		var shrunk = new PooledPerson[sorted.Length - 1];
		Array.Copy(sorted, shrunk, at); Array.Copy(sorted, at + 1, shrunk, at, sorted.Length - at - 1);
		return shrunk;
	}

	/// <summary>The generation median of technicalSkill (ArtistManager draws N(.45, .22) clamped to [0,1]).
	/// "Skill above the population median" is read against this rather than a live scan of 80k people.</summary>
	public const float GenerationMedianSkill = 0.45f;
	/// <summary>Personal recognition above this is a name somebody remembers.</summary>
	public const float RecognitionEpsilon = 0.01f;
	public const int YoungAge = 30;
	/// <summary>Nobody waits forever: a pooled person who has found no act in this many years leaves music.</summary>
	public const int MaximumPoolYears = 4;

	public static int Count => pool.Count;
	public static IReadOnlyCollection<PooledPerson> All => pool.Values;
	public static bool Contains(string personId) => personId != null && pool.ContainsKey(personId);
	public static PooledPerson Get(string personId) => personId != null && pool.TryGetValue(personId, out var p) ? p : null;

	/// <summary>
	/// Whether a person leaving an act still has a career: a name (personal recognition), writer credit,
	/// skill above the population median, or youth. Anyone else leaves music.
	/// </summary>
	public static bool HasCareerToContinue(Musician person, int year, bool hasWriterCredit) =>
		person != null && person.lifeState is not (MemberLifeState.Deceased or MemberLifeState.Retired) &&
		(person.personalRecognition > RecognitionEpsilon || hasWriterCredit ||
		 person.technicalSkill > GenerationMedianSkill || person.GetAge(year) < YoungAge);

	/// <summary>Puts a person into the pool. The caller has already removed them from their act's member list.</summary>
	public static void Add(PooledPerson entry) {
		if (entry?.person == null || string.IsNullOrEmpty(entry.person.personId)) return;
		pool[entry.person.personId] = entry;
		if (ordered != null) ordered = WithEntry(ordered, entry);
	}

	/// <summary>Takes a person out of the pool to join an act. Null if they are not here.</summary>
	public static PooledPerson Take(string personId) {
		if (personId == null || !pool.TryGetValue(personId, out var entry)) return null;
		pool.Remove(personId);
		if (ordered != null) ordered = WithoutId(ordered, personId);
		return entry;
	}

	/// <summary>The pool in a stable order (person id), so any search over it is independent of how the
	/// dictionary happens to lay itself out after removals or a load.</summary>
	/// Deferred like the OrderBy it replaces: the snapshot is taken when enumeration starts.
	public static IEnumerable<PooledPerson> Ordered() {
		ordered ??= pool.Values.OrderBy(p => p.person.personId, StringComparer.Ordinal).ToArray();
		if (StartupCaches.Verify && !ordered.SequenceEqual(pool.Values.OrderBy(p => p.person.personId, StringComparer.Ordinal)))
			throw new InvalidOperationException("PERSON_POOL_ORDER_MISMATCH count=" + pool.Count);
		foreach (PooledPerson p in ordered)
			yield return p;
	}

	/// <summary>Recognition decay for people between acts, once per week (the act registry has its own guard).</summary>
	public static void ForEachPersonOncePerWeek(int week, Action<Musician> action) {
		if (week == lastDecayWeek || pool.Count == 0) { lastDecayWeek = week; return; }
		lastDecayWeek = week;
		foreach (PooledPerson entry in pool.Values) action(entry.person);
	}

	/// <summary>People who waited too long, or died while waiting, leave the pool. Returns who left.</summary>
	public static List<PooledPerson> ExpireStale(int year) {
		var gone = pool.Values.Where(p => p.person.lifeState == MemberLifeState.Deceased ||
			p.person.lifeState == MemberLifeState.Retired ||
			(year - p.sinceYear >= MaximumPoolYears && !SessionEmploymentService.WorkedIn(p.person.personId, year) &&
			 LocalSceneRoomService.Backing(p.person.personId, year).Hours <= 0f))
			.OrderBy(p => p.person.personId, StringComparer.Ordinal).ToList();
		foreach (PooledPerson p in gone) pool.Remove(p.person.personId);
		if (gone.Count > 0) ordered = null;
		return gone;
	}

	public static void Reset() {
		pool.Clear();
		ordered = null;
		lastDecayWeek = int.MinValue;
	}

	// ---- save/load -----------------------------------------------------------------------------------

	public static List<PooledPerson> Capture() => Ordered().ToList();

	public static void Rehydrate(IEnumerable<PooledPerson> saved) {
		pool.Clear();
		ordered = null;
		lastDecayWeek = int.MinValue;
		foreach (PooledPerson entry in saved ?? Enumerable.Empty<PooledPerson>())
			if (entry?.person != null && !string.IsNullOrEmpty(entry.person.personId)) pool[entry.person.personId] = entry;
	}
}
