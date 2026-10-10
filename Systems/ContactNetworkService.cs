using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Remembered relationships between people (SimTools/ContactNetworkDirective.md). Sparse: an edge exists only
/// because two people played in a band together and one left, or worked a session together. Readers: pool hiring,
/// recombination and the player's auditions prefer someone a current member knows. No RNG, no economy writes.
/// <c>--disable-contact-network</c> writes no edges and turns every reader off.
/// </summary>
public static class ContactNetworkService {
	/// <summary>Score a pool candidate gains for knowing a current member: the same weight as a region match.</summary>
	public const float HiringBonus = 1f;

	public static bool Enabled { get; private set; } = true;
	private static readonly Dictionary<string, ContactEdge> edges = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, HashSet<string>> adjacency = new(StringComparer.Ordinal);

	public static int Count => edges.Count;
	/// <summary>Diagnostics, not saved: pool hires (replacement or recombination) of someone a member knew.</summary>
	public static int ContactHires { get; set; }
	public static IReadOnlyCollection<ContactEdge> Edges => edges.Values;

	public static void Configure(IEnumerable<string> arguments) {
		string[] args = (arguments ?? Array.Empty<string>()).ToArray();
		bool enable = args.Contains("--enable-contact-network", StringComparer.Ordinal);
		bool disable = args.Contains("--disable-contact-network", StringComparer.Ordinal);
		if (enable && disable) throw new ArgumentException("--enable-contact-network and --disable-contact-network cannot be used together.");
		Enabled = !disable;
		SessionEmploymentService.Configure(args);
	}

	private static string Key(string x, string y) => string.CompareOrdinal(x, y) < 0 ? x + "|" + y : y + "|" + x;

	/// <summary>Records that two people worked together (or parted). Repeated work deepens the edge.</summary>
	public static void Link(string x, string y, ContactKind kind, int year, int jobs = 1, bool fallout = false) {
		if (!Enabled || string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || x == y) return;
		string key = Key(x, y);
		if (!edges.TryGetValue(key, out var edge)) {
			bool ordered = string.CompareOrdinal(x, y) < 0;
			edges[key] = edge = new ContactEdge { A = ordered ? x : y, B = ordered ? y : x, FirstYear = year };
			Adjacent(x).Add(y);
			Adjacent(y).Add(x);
		}
		edge.Kinds |= kind;
		edge.LastYear = Math.Max(edge.LastYear, year);
		edge.Jobs += Math.Max(1, jobs);
		edge.Fallout |= fallout;
	}
	private static HashSet<string> Adjacent(string person) {
		if (!adjacency.TryGetValue(person, out var set)) adjacency[person] = set = new(StringComparer.Ordinal);
		return set;
	}

	public static ContactEdge Edge(string x, string y) =>
		x != null && y != null && edges.TryGetValue(Key(x, y), out var e) ? e : null;
	public static IReadOnlyCollection<string> ContactsOf(string person) =>
		person != null && adjacency.TryGetValue(person, out var set) ? set : (IReadOnlyCollection<string>)Array.Empty<string>();

	/// <summary>Whether this person knows (well, without a fallout) anyone currently active in the act.</summary>
	public static bool KnowsAny(Musician person, SimulatedArtist act) {
		if (!Enabled || person == null || act?.members == null || !adjacency.TryGetValue(person.personId, out var set)) return false;
		foreach (Musician m in act.members)
			if (m.isActive && m.personId != null && set.Contains(m.personId) && !edges[Key(person.personId, m.personId)].Fallout) return true;
		return false;
	}
	public static float HiringScore(Musician person, SimulatedArtist act) => KnowsAny(person, act) ? HiringBonus : 0f;

	/// <summary>A member leaving an act: they now know each remaining member as a former bandmate.</summary>
	public static void OnMemberLeft(SimulatedArtist act, Musician leaver, DepartureKind kind, int year) {
		if (!Enabled || act?.members == null || leaver == null) return;
		bool fallout = kind is DepartureKind.Acrimony or DepartureKind.Fired or DepartureKind.WalkedOut;
		int years = Math.Max(1, year - Math.Max(leaver.joinedYear, 1950) + 1);
		foreach (Musician m in act.members)
			if (m != leaver && m.isActive) Link(leaver.personId, m.personId, ContactKind.FormerBandmate, year, years, fallout);
	}

	/// <summary>Year end: forget edges where either person has died or retired from music.</summary>
	public static void Prune() {
		if (ArtistManager.Instance == null) return;
		bool Gone(string id) => ArtistManager.Instance.GetMusician(id) is not { } m ||
			m.lifeState is MemberLifeState.Deceased or MemberLifeState.Retired;
		foreach (var key in edges.Where(kv => Gone(kv.Value.A) || Gone(kv.Value.B)).Select(kv => kv.Key).ToList()) {
			var e = edges[key];
			edges.Remove(key);
			if (adjacency.TryGetValue(e.A, out var a)) { a.Remove(e.B); if (a.Count == 0) adjacency.Remove(e.A); }
			if (adjacency.TryGetValue(e.B, out var b)) { b.Remove(e.A); if (b.Count == 0) adjacency.Remove(e.B); }
		}
	}

	public static void Reset() { edges.Clear(); adjacency.Clear(); ContactHires = 0; SessionEmploymentService.Reset(); }

	public static void CaptureWorld(WorldSaveData world) {
		world.ContactNetwork = new ContactNetworkSaveData {
			Edges = edges.Values.OrderBy(e => e.A, StringComparer.Ordinal).ThenBy(e => e.B, StringComparer.Ordinal)
				.Select(e => new ContactEdge { A = e.A, B = e.B, Kinds = e.Kinds, FirstYear = e.FirstYear, LastYear = e.LastYear, Jobs = e.Jobs, Fallout = e.Fallout })
				.ToList()
		};
		SessionEmploymentService.Capture(world.ContactNetwork);
	}

	/// <summary>An older save has no network: it starts empty, and relationships accrue from the load onward.</summary>
	public static void RehydrateWorld(WorldSaveData world) {
		Reset();
		var saved = world?.ContactNetwork;
		if (saved == null) return;
		if (saved.SchemaVersion != 1) throw new InvalidOperationException("Unsupported contact network schema.");
		foreach (var e in saved.Edges ?? new()) {
			if (string.IsNullOrEmpty(e.A) || string.IsNullOrEmpty(e.B) || e.A == e.B) continue;
			edges[Key(e.A, e.B)] = new ContactEdge { A = e.A, B = e.B, Kinds = e.Kinds, FirstYear = e.FirstYear, LastYear = e.LastYear, Jobs = e.Jobs, Fallout = e.Fallout };
			Adjacent(e.A).Add(e.B);
			Adjacent(e.B).Add(e.A);
		}
		SessionEmploymentService.Rehydrate(saved);
	}
}
