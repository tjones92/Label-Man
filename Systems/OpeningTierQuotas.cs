using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Opening tier quotas (LpGenreAllocationFollowup.md §87-88): the January 1960 state that the opening draft sets is fixed by
/// tier, so the size of a tier's opening rosters (--major-opening-roster) no longer decides who holds the stars or the
/// opening chart. After the opening nothing is held: acts move, sign and chart as usual. Off unless named; off reads nothing.
///
/// --opening-star-tier-share=S: the opening Superstars and Stars (the AdultStandardBearer, CurrentHitmaker and
/// EstablishedHitmaker careers) are chosen from the launch pool before the draft, by the same genre and quality ranking the
/// stamp uses, and signed to a Major (share S of each rung, interleaved down the quality order) or a MidTier label. Within the
/// tier the act goes to a label that can reach it and has room, preferred genre first, then a keyed order. The draft then
/// fills the rest of every roster, and the stamp gives the reserved acts their careers. History: the Big-5 held ~37% of 1960's
/// acts with two or more Top 10s (studio-parent screen, 2026-10-22). --historical-independent-stars keeps Imperial's two
/// rows (reserved the same way); --studio-parent-labels is refused with it.
///
/// --opening-catalogue-major-share=S: the Major tier's share of the opening catalogue (CompetitorManager.PopulateInitialRecords,
/// ~500 records released 1-20 weeks before the start) is S of the tier-weighted split instead of its roster-weighted share.
/// Each tier's other labels keep their relative weights; the per-label cap (three records an act) still applies.
/// </summary>
public static class OpeningTierQuotas {
	public const string StarShareSwitch = "--opening-star-tier-share=";
	public const string CatalogueShareSwitch = "--opening-catalogue-major-share=";
	private static bool configured;
	/// <summary>The Major share of the opening Superstars and Stars; negative when off.</summary>
	public static float StarShare { get; private set; } = -1f;
	/// <summary>The Major share of the opening catalogue weight; negative when off.</summary>
	public static float CatalogueShare { get; private set; } = -1f;
	public static bool StarsEnabled { get { EnsureConfigured(); return StarShare >= 0f; } }
	public static bool CatalogueEnabled { get { EnsureConfigured(); return CatalogueShare >= 0f; } }

	/// <summary>An act chosen before the draft for an opening star career.</summary>
	public readonly record struct Reservation(string ArtistId, string LabelId, OpeningCareerArchetype Archetype, CareerState Rung, bool Independent);
	private static readonly List<Reservation> reserved = new();
	public static IReadOnlyList<Reservation> Reserved => reserved;
	internal static void ClearReservations() => reserved.Clear();
	internal static void Reserve(Reservation r) => reserved.Add(r);

	/// <summary>Per-archetype Major shares (--opening-star-tier-share=Archetype:S,...); empty for the single-share form.</summary>
	private static readonly Dictionary<OpeningCareerArchetype, float> archetypeShares = new();
	/// <summary>The archetypes whose stars the quota places: the three star careers of the opening stamp.</summary>
	public static readonly OpeningCareerArchetype[] StarArchetypes = {
		OpeningCareerArchetype.AdultStandardBearer, OpeningCareerArchetype.CurrentHitmaker, OpeningCareerArchetype.EstablishedHitmaker };

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		archetypeShares.Clear();
		StarShare = ParseStars(args);
		CatalogueShare = Parse(args, CatalogueShareSwitch);
		reserved.Clear();
		if (StarShare >= 0f || CatalogueShare >= 0f)
			GD.Print($"OPENING_TIER_QUOTAS starShare={StarShare:F3} " +
				string.Join(" ", archetypeShares.Select(kv => $"{kv.Key}={kv.Value:F3}")) + $" catalogueShare={CatalogueShare:F3}");
		configured = true;
	}

	/// <summary>A single share for every star archetype, or Archetype:S pairs naming all three. Returns the single share,
	/// or the pairs' mean (only read as "on") for the pair form.</summary>
	private static float ParseStars(string[] args) {
		var values = args.Where(a => a.StartsWith(StarShareSwitch, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + StarShareSwitch.TrimEnd('=') + " once.");
		if (values.Length == 0) return -1f;
		string body = values[0].Substring(StarShareSwitch.Length);
		if (!body.Contains(':')) return Share(body, StarShareSwitch);
		foreach (string pair in body.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
			string[] kv = pair.Split(':');
			if (kv.Length != 2 || !Enum.TryParse(kv[0], out OpeningCareerArchetype archetype) || !StarArchetypes.Contains(archetype)
					|| archetypeShares.ContainsKey(archetype))
				throw new ArgumentException(StarShareSwitch.TrimEnd('=') + " pairs are " + string.Join(", ", StarArchetypes) + ":S, each once.");
			archetypeShares[archetype] = Share(kv[1], StarShareSwitch);
		}
		if (archetypeShares.Count != StarArchetypes.Length)
			throw new ArgumentException(StarShareSwitch.TrimEnd('=') + " names all three star archetypes or gives one share.");
		return archetypeShares.Values.Average();
	}

	private static float Share(string text, string name) {
		if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) || !float.IsFinite(s) || s < 0f || s > 1f)
			throw new ArgumentException(name.TrimEnd('=') + " needs a share in [0, 1].");
		return s;
	}

	private static float Parse(string[] args, string name) {
		var values = args.Where(a => a.StartsWith(name, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + name.TrimEnd('=') + " once.");
		return values.Length == 0 ? -1f : Share(values[0].Substring(name.Length), name);
	}

	/// <summary>The pair form is named: each archetype is split on its own (both rungs together), not each rung across them.</summary>
	public static bool ByArchetype => archetypeShares.Count > 0;
	/// <summary>The Major share for an archetype's stars: its own under the pair form, else the single share.</summary>
	public static float StarShareFor(OpeningCareerArchetype archetype) =>
		archetypeShares.TryGetValue(archetype, out float s) ? s : StarShare;
	internal static void SetArchetypeSharesForChecks(IDictionary<OpeningCareerArchetype, float> shares) {
		archetypeShares.Clear();
		if (shares != null) foreach (var kv in shares) archetypeShares[kv.Key] = kv.Value;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }
	internal static void SetForChecks(float starShare, float catalogueShare) {
		configured = true; StarShare = starShare; CatalogueShare = catalogueShare; reserved.Clear(); archetypeShares.Clear();
	}

	/// <summary>Whether the <paramref name="index"/>-th act (0-based) of a split goes to a Major: the Major count is held at
	/// round(S × (index + 1)) as the order runs, so the split is interleaved down the quality ranking.</summary>
	public static bool NextIsMajor(int index, int majorsSoFar) => NextIsMajor(StarShare, index, majorsSoFar);
	public static bool NextIsMajor(float share, int index, int majorsSoFar) => majorsSoFar < (int)Math.Floor(share * (index + 1) + 0.5f);

	/// <summary>Scales each Major's opening-catalogue weight so the tier holds <see cref="CatalogueShare"/> of the total.
	/// Identity when off or when either side has no weight.</summary>
	public static float MajorWeightScale(float majorWeight, float otherWeight) {
		if (!CatalogueEnabled || majorWeight <= 0f || otherWeight <= 0f) return 1f;
		if (CatalogueShare >= 1f) return 1e6f;
		return CatalogueShare * otherWeight / ((1f - CatalogueShare) * majorWeight);
	}
}
