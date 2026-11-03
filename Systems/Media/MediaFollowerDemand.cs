using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// One release's follower demand (CelebrityFanbaseDesign.md §2.4): the act's followers by region at launch, turned into
/// intent with the per-follower rates and spread over the first three clearing weeks. Media-owned and saved.
/// </summary>
public sealed class FollowerLaunch {
	public string RecordId, ArtistId;
	public ReleaseFormat Format;
	/// <summary>The chart week of the record's first clearing; −1 until that clearing sees it.</summary>
	public int LaunchWeek = -1;
	/// <summary>Three-week intent by region, in units, as the followers stood at launch.</summary>
	public Dictionary<string, float> Intent = new(StringComparer.Ordinal);
	/// <summary>Disappointment at the release (§2.3): weeks after the first lose this share. Advance orders are bought unheard.</summary>
	public float Loss;
	/// <summary>Dealers already stocked the advance orders (causal AI launch only).</summary>
	public bool Stocked;
	// Telemetry: units asked for, units the physical stock let through, and advance copies stocked.
	public double Asked, Granted, StockedUnits;
}

/// <summary>
/// Celebrity fanbase Phase 4 (CelebrityFanbaseDesign.md §2.4, §10): a following does something.
/// <list type="bullet">
/// <item><b>Release-on-sight demand.</b> When an act releases, each region's followers add launch intent to that record's
/// regional singles or LP intent, after serviceable intent and its physical limit are known and before the format budgets
/// clear: <see cref="MediaFollowing.AdvancePerFollower"/> per follower, spread <see cref="WeekShare"/> over the first three
/// clearing weeks. It competes inside the existing purse, so where the purse binds fans move units toward their act rather
/// than create them. It never passes the record's stock or the region's shelf capacity that week.</item>
/// <item><b>Advance orders.</b> At an AI launch the dealers stock the followers' three-week intent on top of the ordinary
/// launch shelf, in every region the record reached. The player's pressing is real, so the player's records get the
/// intent but no free copies.</item>
/// <item><b>Live draw.</b> Followers in a room's region come to its bills (<see cref="LiveNightShare"/> of them a night,
/// up to the room's capacity), and the act's share of every crowd joins its following at <see cref="CLive"/>.</item>
/// </list>
/// <c>--observe-follower-demand</c> (shadow) builds the launch books and live joiners and logs what fans would have
/// asked; the economy reads nothing. <c>--follower-demand</c> (causal) needs <see cref="MediaAtmosphere.CausalSwitch"/> and
/// <see cref="MediaFollowing.ObserveSwitch"/>. This is directive amendments §0.1 and §0.2: follower purchases are
/// audience behavior, not media requests, so they sit outside the MediaDemand ledger and its caps. Draws no RNG.
/// </summary>
public static class MediaFollowerDemand {
	public const string ShadowSwitch = "--observe-follower-demand";
	public const string CausalSwitch = "--follower-demand";
	/// <summary>Experimental singles-only calibration; 1 retains the screened 0.35 / 0.10 rates.</summary>
	public static float SingleScale { get; private set; } = 1f;

	/// <summary>Launch intent by clearing week: most fans buy in the first week, the rest as the record reaches them.</summary>
	public static readonly float[] WeekShare = { 0.5f, 0.3f, 0.2f };
	public static int Weeks => WeekShare.Length;
	/// <summary>A launch book is kept only when the followers would ask for at least this many copies in all.</summary>
	public const float BookFloor = 100f;
	/// <summary>Share of an act's followers in a room's region who come to one of its bills (initial value): 1 in 2,000.</summary>
	public const float LiveNightShare = 0.0005f;
	/// <summary>§2.2: the room is where die-hards are made. Share of an act's crowd who join its following.</summary>
	public const float CLive = 0.15f;

	private static bool configured, shadow, causal;
	/// <summary>Books, live joiners and telemetry; the economy reads nothing.</summary>
	public static bool Shadow => shadow && !causal && MediaFollowing.Active;
	/// <summary>Follower intent clears, dealers stock the advance and rooms count the followers.</summary>
	public static bool Causal => causal && MediaAtmosphere.Causal && MediaFollowing.Active;
	public static bool Active => Shadow || Causal;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == ShadowSwitch) shadow = true;
			if (argument == CausalSwitch) causal = true;
			if (argument.StartsWith("--follower-single-scale=", StringComparison.Ordinal)) {
				if (!float.TryParse(argument["--follower-single-scale=".Length..], NumberStyles.Float,
					CultureInfo.InvariantCulture, out float scale) || !float.IsFinite(scale) || scale < 0f || scale > 2f)
					throw new ArgumentException("--follower-single-scale must be finite and between 0 and 2.");
				SingleScale = scale;
			}
		}
		if (SingleScale != 1f && !causal && !shadow)
			throw new ArgumentException("--follower-single-scale requires follower demand or its shadow.");
		if (causal && shadow)
			throw new ArgumentException($"{CausalSwitch} and {ShadowSwitch} cannot be used together; the causal run prints the same telemetry.");
		if (causal && !MediaAtmosphere.Causal)
			throw new ArgumentException($"{CausalSwitch} is an economic writer; it requires {MediaAtmosphere.CausalSwitch}.");
		if ((causal || shadow) && !MediaFollowing.Active)
			throw new ArgumentException($"Follower demand reads followings; it requires {MediaFollowing.ObserveSwitch}.");
		configured = true;
	}

	internal static void SetForChecks(bool on, bool writeCausal = false) { shadow = on && !writeCausal; causal = on && writeCausal; configured = true; }

	/// <summary>A region's three-week intent from its followers: young × the young rate + adult × the adult rate (§2.4).</summary>
	public static float RegionIntent(float young, float adult, ReleaseFormat format) => RegionIntent(young, adult, format, SingleScale);

	internal static float RegionIntent(float young, float adult, ReleaseFormat format, float singleScale) {
		var (perY, perA) = MediaFollowing.AdvancePerFollower(format);
		float intent = Math.Max(0f, young) * perY + Math.Max(0f, adult) * perA;
		return format == ReleaseFormat.Single ? intent * singleScale : intent;
	}

	/// <summary>
	/// This clearing week's share of a book's intent: <see cref="WeekShare"/>[week], and after the first week only the
	/// followers the record didn't disappoint. Zero before the launch week or after the third.
	/// </summary>
	public static float WeekFactor(int week, float loss) =>
		week < 0 || week >= WeekShare.Length ? 0f : WeekShare[week] * (week == 0 ? 1f : 1f - Math.Clamp(loss, 0f, 1f));

	/// <summary>Fan intent added to a title's serviceable intent: never past the title's physical limit that week.</summary>
	public static int Grant(int serviceable, int physicalLimit, float fanIntent) {
		if (fanIntent <= 0f) return 0;
		int headroom = Math.Max(0, physicalLimit - Math.Max(0, serviceable));
		return Math.Min(headroom, (int)MathF.Floor(fanIntent));
	}

	/// <summary>
	/// Fans move LP buyers, they don't add them. The singles purse binds almost every region-week, so a single's fan
	/// intent displaces other singles inside it; the LP purse rarely binds, so fan LP intent would mostly be extra albums.
	/// Here a region's LP intent is held at its pre-fan total: the fans' grants (scaled down if they exceed it) are taken
	/// from every title's own intent in proportion, including the fan title's. Returns each title's new serviceable intent,
	/// the part of it that is the title's own buyers (base − kept is what it lost to fans), and the units displaced; each
	/// new intent stays within base + grant, so within the physical limit.
	/// </summary>
	public static (int[] Serviceable, int[] Kept, int Displaced) ConserveAlbumIntent(IReadOnlyList<int> baseIntent, IReadOnlyList<int> fanGrant) {
		int n = baseIntent.Count;
		var result = new int[n];
		long total = 0, fans = 0;
		for (int i = 0; i < n; i++) { total += Math.Max(0, baseIntent[i]); fans += Math.Max(0, fanGrant[i]); }
		int pool = (int)Math.Min(int.MaxValue, total);
		var grants = new double[n];
		var bases = new double[n];
		for (int i = 0; i < n; i++) { grants[i] = Math.Max(0, fanGrant[i]); bases[i] = Math.Max(0, baseIntent[i]); }
		int[] fanUnits = fans > pool ? RetailPurchaseEnvelope.Allocate(grants, pool) : Array.ConvertAll(grants, g => (int)g);
		int granted = 0;
		foreach (int g in fanUnits) granted += g;
		int[] kept = RetailPurchaseEnvelope.Allocate(bases, pool - granted);
		for (int i = 0; i < n; i++) result[i] = kept[i] + fanUnits[i];
		return (result, kept, granted);
	}

	/// <summary>Followers who come to a bill: <see cref="LiveNightShare"/> of the act's followers in the room's region.</summary>
	public static int LiveHeads(float regionalFollowers) => (int)MathF.Floor(Math.Max(0f, regionalFollowers) * LiveNightShare);
}
