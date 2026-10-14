using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// What it is actually like to run a record label out of one particular town, 1960. The region presets
/// (<see cref="MarketRegion"/>) say what a market BUYS; this says what it costs and what it is like to
/// WORK there. Every number is player-side only -- the AI economy does not read it -- and every number
/// is surfaced to the player in plain language rather than as a tier or a percentage.
///
/// Four levers, all authored per city, all pulling the same way so a big town is a trade and not a
/// strictly-better pick:
///   Rent       monthly rent for the office, on top of <see cref="CityProfiles.OfficeBaseOverhead"/>.
///              Period money: the 1960 census median gross rent was ~$71 a month nationally; a small
///              Manhattan suite near the trade cost two to three times a Billings storefront.
///   AskScale   what unsigned acts ask when you find them here. Acts in a trade town know the going rate
///              and have people around who will tell them.
///   Crowding   how many other record men work the same rooms. Scales how quickly an act you have been
///              looking at gets snapped up by somebody else.
///   Studio     the town's studio quality, 0..1 (hourly rates and the production the rooms can reach both
///              follow it). Absolute, not a shift on the region: the region presets are too flat to separate
///              one recording town from the next.
/// </summary>
public sealed class CityProfile {
	public readonly float Rent, AskScale, Crowding, Studio;
	public readonly string Character;

	public CityProfile(float rent, float askScale, float crowding, float studio, string character) {
		Rent = rent; AskScale = askScale; Crowding = crowding; Studio = studio; Character = character;
	}
}

public static class CityProfiles {
	/// <summary>The phone line, the postage and the filing cabinet -- the same in every town.</summary>
	public const float OfficeBaseOverhead = 35f;

	// A town nobody has authored costs what every town used to: $35 + $40 = the old flat $75.
	// A negative studio quality means "unauthored: fall back to the region's rooms".
	private static readonly CityProfile Fallback = new(40f, 1.00f, 1.00f, -1f, "A town like any other.");

	private static readonly Dictionary<string, CityProfile> Table = new(StringComparer.Ordinal) {
		//                          rent  ask   crowd  studio
		["new_york"]       = new(140f, 1.45f, 2.40f,  1.00f, "The center of the trade. Every publisher, agent and trade paper is here, and so is every rival you have."),
		["boston"]         = new( 60f, 1.10f, 1.10f,  0.75f, "A college town with a serious record trade and a tight scene."),
		["philadelphia"]   = new( 50f, 1.10f, 1.30f,  0.85f, "Teen television on the doorstep and a string of small labels, a short train ride from New York's money."),
		["baltimore"]      = new( 38f, 0.95f, 0.80f, 0.55f, "A working port. Cheaper rooms, and the big-city rivals are a train ride away."),
		["washington"]     = new( 60f, 1.00f, 0.80f, 0.55f, "A government town: steady salaries, plenty of listeners and very few record men."),
		["pittsburgh"]     = new( 38f, 0.90f, 0.70f, 0.55f, "Mill-town money and loyal local crowds. The trade seldom comes through."),

		["chicago"]        = new( 70f, 1.25f, 1.80f,  0.93f, "The biggest independent-label town in the country, with a club scene that never closes."),
		["detroit"]        = new( 45f, 1.05f, 1.10f,  0.80f, "Factory wages and teenagers with cars. A new label can make some noise here."),
		["cleveland"]      = new( 38f, 0.95f, 0.80f,  0.65f, "A big radio town where a DJ's word still sells records."),
		["cincinnati"]     = new( 32f, 0.85f, 0.60f,  0.70f, "A river town with a proud pressing and recording trade of its own, and little else competing for your acts."),

		["minneapolis"]    = new( 40f, 0.90f, 0.70f, 0.60f, "Cold winters and loyal listeners, with the whole Upper Midwest to sell to."),
		["st_louis"]       = new( 36f, 0.90f, 0.70f,  0.65f, "A river city with a deep club scene and few record men working it."),
		["kansas_city"]    = new( 32f, 0.85f, 0.55f,  0.55f, "A music town that exports its talent. Rooms are cheap and rivals are few."),
		["omaha"]          = new( 25f, 0.80f, 0.40f, 0.35f, "Small, quiet and cheap. You will be the only record man in the room."),

		["nashville"]      = new( 40f, 1.15f, 1.30f,  0.95f, "A radio-and-publishing town with a close-knit trade. Word gets round fast, and so do the acts."),
		["memphis"]        = new( 32f, 0.90f, 0.90f,  0.90f, "Small studios and a big sound. A few shrewd operators already work the same rooms."),
		["atlanta"]        = new( 40f, 0.95f, 0.90f,  0.65f, "The growing hub of the Southeast: cheap to work out of, and getting busier."),
		["new_orleans"]    = new( 35f, 0.90f, 0.70f,  0.65f, "A port with a music tradition all its own. Long nights and a thin trade."),
		["miami"]          = new( 48f, 0.95f, 0.60f, 0.50f, "A tourist town of hotel bands and cheap sunshine. The record trade mostly passes through."),

		["dallas"]         = new( 45f, 1.00f, 0.90f,  0.65f, "Oil money and a young market. The trade is arriving but has not crowded in yet."),
		["houston"]        = new( 42f, 0.95f, 0.80f,  0.60f, "A boom town with a few small labels of its own and plenty of room to start."),
		["san_antonio"]    = new( 28f, 0.85f, 0.50f, 0.40f, "A quiet, low-rent town. Local acts are cheap and rivals are rare."),
		["phoenix"]        = new( 30f, 0.85f, 0.40f, 0.30f, "A desert town with few studios and fewer rivals. Almost everything ships in."),
		["albuquerque"]    = new( 25f, 0.80f, 0.30f, 0.25f, "A small, sunburnt town a long way from anyone who buys records."),

		["denver"]         = new( 40f, 0.95f, 0.60f, 0.45f, "The only real market town in the Rockies. Quiet trade, and everything past it is a long drive."),
		["salt_lake_city"] = new( 28f, 0.85f, 0.35f, 0.25f, "Orderly, quiet and cheap. The trade rarely stops here."),
		["billings"]       = new( 18f, 0.80f, 0.20f, 0.15f, "Cheap to live in and cheap to sign in, and a long way from everyone who buys records."),

		["los_angeles"]    = new( 90f, 1.30f, 1.90f,  0.97f, "Film money, studio players and a trade that never sleeps. Rents and egos run high."),
		["san_francisco"]  = new( 65f, 1.15f, 1.00f,  0.80f, "A port city with a restless crowd and few record men of its own."),
		["seattle"]        = new( 40f, 0.95f, 0.60f, 0.55f, "A quiet northwest port. Few rivals, and few buyers within a day's drive."),
		["portland"]       = new( 32f, 0.85f, 0.45f, 0.40f, "Small, damp and cheap. Nobody from the trade will fight you for a good act."),
	};

	public static CityProfile Get(string cityId) =>
		!string.IsNullOrEmpty(cityId) && Table.TryGetValue(cityId, out CityProfile profile) ? profile : Fallback;

	/// <summary>What it costs each month to keep this office open: the fixed line plus the town's rent.</summary>
	public static float MonthlyOverhead(string cityId) => OfficeBaseOverhead + Get(cityId).Rent;

	/// <summary>The cheapest and dearest towns to keep an office in, for the founding screen's summary line.</summary>
	public static (float Low, float High) OverheadRange() {
		float low = float.MaxValue, high = 0f;
		foreach (CityProfile profile in Table.Values) {
			low = Mathf.Min(low, OfficeBaseOverhead + profile.Rent);
			high = Mathf.Max(high, OfficeBaseOverhead + profile.Rent);
		}
		return (low, high);
	}

	/// <summary>How the rival record men in town read to a player, in words.</summary>
	public static string CrowdingText(CityProfile profile) =>
		profile.Crowding >= 1.8f ? "Everyone in the business works these rooms. Good acts get snapped up within days."
		: profile.Crowding >= 1.1f ? "A busy scene. Other labels are in the same clubs, and a good act will not wait long."
		: profile.Crowding >= 0.7f ? "A modest scene. You will have most rooms to yourself, most nights."
		: "Hardly anyone from the trade comes through. An act you like will still be there next week.";

	/// <summary>What unsigned acts here ask, against the national going rate, in words.</summary>
	public static string AskText(CityProfile profile) {
		int percent = Mathf.RoundToInt(Mathf.Abs(profile.AskScale - 1f) * 100f);
		return percent < 5 ? "Acts here ask about the going rate."
			: profile.AskScale > 1f ? $"Acts here know what they are worth: they ask about {percent}% over the going rate."
			: $"Acts here are glad of the work: they ask about {percent}% under the going rate.";
	}

	/// <summary>How freight and wholesale actually work in this town, in words. Replaces the bare
	/// "distribution tier" number: a player can read this and reason about it without a legend.</summary>
	public static string ShippingText(MarketCity city) {
		return (city?.distributionTier ?? 3) switch {
			<= 1 => "Every wholesaler keeps a dock here. Records move by the truckload and a reorder is on a shelf by supper.",
			2 => "A solid wholesale trade. Most houses call weekly and the plants are a short haul.",
			3 => "A few jobbers and a one-stop at best. Orders leave when the truck does.",
			_ => "Records arrive by mail and rail from somewhere else. Wholesalers rarely come through."
		};
	}

	/// <summary>The studio quality (0..1) of this town. An unauthored town falls back to its region's rooms.</summary>
	public static float StudioQuality(string cityId, float regionQuality) {
		float authored = Get(cityId).Studio;
		return authored >= 0f ? Mathf.Clamp(authored, 0f, 1f) : Mathf.Clamp(regionQuality, 0f, 1f);
	}

	public static string StudioSoundText(float quality) =>
		quality >= 0.75f ? "a name sound that other towns come to borrow"
		: quality >= 0.5f ? "a solid, professional sound"
		: quality >= 0.25f ? "an honest, workmanlike sound"
		: "a thin local sound";
}
