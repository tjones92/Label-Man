using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// ROLODEX CALL SCENES
//
// A call is not a branching tree and it is not a button that returns yes/no. It is a fixed
// sequence of BEATS, each of which selects an authored fragment whose tags match the real
// situation. The situation is captured ONCE, up front, in a RolodexCallContext built from live
// simulation values -- station format admittance, regional genre acceptance and momentum, the
// record's own hook/production/originality, the artist's public recognition, the DJ's taste,
// greed and suspicion, the rapport already banked. Every line the contact speaks is selected by
// a condition over that context, so the game can always explain why he said what he said.
//
// The one rule: a line must reveal a real sim fact, express a real motive, or execute a real sim
// action. Nothing here invents a fact the simulation cannot back. Where the player is offered a
// counter-argument that is NOT true, it is labelled a bluff and can be called.
// ============================================================================================

/// <summary>The modular beats a call is assembled from, in order.</summary>
public enum RolodexSceneBeat {
	Opening,
	PassiveRead,
	SituationRead,
	PlayerPitch,
	Pushback,
	ActiveCheckPrompt,
	Success,
	Failure,
	RelationshipAftermath,
	Exit,
}

/// <summary>Which of the four voices a line or option belongs to. Drives colour and gating.</summary>
public enum ExecutiveVoice { None, Ear, Street, Suit, Fixer }

/// <summary>How well a passive read landed. The player gets interpreted intelligence, never the
/// raw stat -- a strong read is a confident sentence, not "greed 0.71".</summary>
public enum InsightStrength { None, Hint, ClearRead, DeepRead }

/// <summary>When this DJ is actually on the air. Derived deterministically from archetype + id, so
/// it is stable for a given jock across a campaign and across a save/load. This is what makes the
/// clock matter: a midnight jock is not at the station at half past nine in the morning.</summary>
public enum Daypart { Morning, Midday, Afternoon, Evening, Overnight }

/// <summary>What the contact objects to. Always sourced from a real condition on the context.</summary>
public enum Objection {
	None,
	// Directive §3.3: no RecordServicing row exists for (record, his station) -- ranked first in
	// PickObjection's severity ordering, because it is the most basic thing that can be wrong with a
	// pitch. Nothing gets played that nobody has been sent (invariant 2): Resolve() forces every
	// approach to fail while this objection stands, whatever counter is played.
	NotServiced,
	FormatShutOut,      // the station's format does not admit this genre at all
	ProductionRough,    // the record genuinely sounds cheap
	NoLocalAudience,    // regional genre acceptance is low here
	UnknownArtist,      // nobody has heard of the act
	NoSalesSupport,     // the record is not selling; radio follows sales at this station
	ManagerHeat,        // low autonomy / high suspicion -- he cannot take the risk
	WhatsInItForMe,     // greedy jock, cold relationship
	YouBurnedMeBefore,  // a prior pitch settled badly
	PlaylistFull,       // the slots are spoken for this week
}

/// <summary>The verbs. Each one is a real mechanical intention, not a flavour choice.</summary>
public enum RolodexApproach {
	PersonalPitch,
	CommercialPitch,   // ad-buy
	AskForFavor,
	OfferPayola,
	RivalPressure,
	AskForIntroduction,
	HangUp,
}

/// <summary>Where the scene currently sits. The UI renders one stage at a time.</summary>
public enum CallStage {
	Dialing,        // not yet placed
	NotConnected,   // you did not get through; why is in the transcript
	Open,           // he is on the line; pick an approach
	Pushback,       // he pushed back; press it, answer it, or drop it
	Resolved,       // the roll has happened and the effect is applied
	Ended,
}

/// <summary>One line of the call as the player sees it.</summary>
public sealed class CallLine {
	public RolodexSceneBeat beat;
	public ExecutiveVoice voice;      // None = the contact or the narrator; otherwise an instinct read
	public string speaker;            // "" for narration / instinct reads
	public string text;
	public bool isPlayer;
}

// --------------------------------------------------------------------------------------------
// CONTEXT
// --------------------------------------------------------------------------------------------

/// <summary>
/// Every real fact a call is allowed to reference, gathered once when the line connects. Fragments
/// and conditions read ONLY this object -- they never reach into the simulation themselves -- so a
/// selected line is auditable: the condition that chose it is a named predicate over these fields.
/// </summary>
public sealed class RolodexCallContext {
	public RolodexEntry entry;
	public Deejay dj;
	public RadioStation station;
	public MarketRegion region;
	public RecordRuntimeData record;
	public Record baseRecord;
	public SimulatedArtist artist;
	public AILabel playerLabel;
	public ExecutiveInstinctProfile instincts;
	public int year, week, hour;

	// Record facts.
	public float recordHook, recordProduction, recordOriginality, recordQuality;
	public float salesSupport;          // units this week against the record's own best week
	public long unitsTotal, unitsThisWeek;

	// Market facts.
	public float regionalGenreAcceptance;   // 0-1, this region, this genre, right now
	public float regionalGenreMomentum;     // signed: the direction the region is moving
	public float regionalAwareness;
	public float formatAdmittance;          // the meeting's own FormatMatch for this genre at this station
	public float artistRecognition;

	// Contact facts.
	public float djTaste, djGreed, djInfluence, djEgo, djSuspicion, djGenreAffinity;
	public float stationAutonomy;
	public bool managerPressureHigh;
	public Daypart shift;

	// Servicing facts (directive §3.2): has this station actually been sent a copy of the record
	// under discussion? Objection.NotServiced and Resolve() both read this -- see PlayerDesk.IsServiced.
	public bool isServiced;
	public float servicingConviction;

	// Trade press facts (directive §6.1): whether a live review pick exists for the record under
	// discussion, its plain-English name for CiteTradePress's line, and the CounterWeight it's worth --
	// see PlayerDesk.ActiveTradeOutcome.
	public bool hasTradePick;
	public string tradePickLabel;
	public float tradePickWeight;
	// Directive §6.2: a live trade ad's bonus to a Commercial Pitch's odds -- see
	// PlayerDesk.TradeAdConnectBonus (label-wide, same value the cold connect roll reads).
	public float tradeAdCommercialBonus;

	// Second-market facts (directive §10): a real out-of-region spin (another reporter station, a
	// different region than this call's own) or breakout listing to cite -- "it's number four on the
	// WAMO survey." When false, SuitSurvey is still offered (an explicit bluff a reachy jock can call).
	public bool hasOutOfRegionProof;
	public string outOfRegionProofLabel;
	public float outOfRegionProofWeight;

	// Relationship facts.
	public float rapport;
	public RapportTier tier;
	public bool theyOweYou, youOweThem, payolaBurned, professionallyBurned;
	public float advocacyAlready;       // live advocacy this station already carries for this record

	// Label facts.
	public float labelCash;

	public bool HasRecord => baseRecord != null;
}

// --------------------------------------------------------------------------------------------
// CONDITIONS -- the factual layer every fragment is selected through
// --------------------------------------------------------------------------------------------

public static class RolodexConditions {
	public static bool Meets(string condition, RolodexCallContext c) => condition switch {
		null or "" => true,
		"HasRecord"            => c.HasRecord,
		"FormatShutOut"        => c.HasRecord && c.formatAdmittance < 0.08f,
		"FormatMarginal"       => c.HasRecord && c.formatAdmittance >= 0.08f && c.formatAdmittance < 0.30f,
		"FormatFits"           => c.HasRecord && c.formatAdmittance >= 0.30f,
		"ProductionLow"        => c.HasRecord && c.recordProduction < 0.38f,
		"HookStrong"           => c.HasRecord && c.recordHook > 0.62f,
		"OriginalityHigh"      => c.HasRecord && c.recordOriginality > 0.70f,
		"QualityHigh"          => c.HasRecord && c.recordQuality > 0.62f,
		"QualityLow"           => c.HasRecord && c.recordQuality < 0.38f,
		"GenreAcceptanceLow"   => c.regionalGenreAcceptance < 0.38f,
		"GenreAcceptanceHigh"  => c.regionalGenreAcceptance > 0.60f,
		"GenreMomentumRising"  => c.regionalGenreMomentum > 0.08f,
		"GenreMomentumFalling" => c.regionalGenreMomentum < -0.05f,
		"UnknownArtist"        => c.artistRecognition < 0.12f,
		"KnownArtist"          => c.artistRecognition > 0.45f,
		"SalesWeak"            => c.HasRecord && c.salesSupport < 0.35f,
		"SalesStrong"          => c.HasRecord && c.salesSupport > 0.70f && c.unitsTotal > 200,
		"NoSalesYet"           => c.HasRecord && c.unitsTotal < 50,
		"ColdRelationship"     => c.rapport <= 0.02f,
		"WarmRelationship"     => c.rapport > 0.25f,
		"LoyalRelationship"    => c.rapport >= 0.60f,
		"TheyOweYou"           => c.theyOweYou,
		"YouOweThem"           => c.youOweThem,
		"HighManagerPressure"  => c.managerPressureHigh,
		"HighAutonomy"         => c.stationAutonomy > 0.60f,
		"Greedy"               => c.djGreed > 0.55f,
		"NotGreedy"            => c.djGreed < 0.25f,
		"Suspicious"           => c.djSuspicion > 0.50f,
		"HasTaste"             => c.djTaste > 0.60f,
		"NoTaste"              => c.djTaste < 0.35f,
		"BigEgo"               => c.djEgo > 0.60f,
		"LikesThisGenre"       => c.djGenreAffinity > 1.15f,
		"DislikesThisGenre"    => c.djGenreAffinity < 0.85f,
		"AlreadyCarrying"      => c.advocacyAlready > 0.01f,
		"NightShift"           => c.shift is Daypart.Evening or Daypart.Overnight,
		"BrokeLabel"           => c.labelCash < 300f,
		_ => false,
	};

	public static bool MeetsAll(IEnumerable<string> conditions, RolodexCallContext c) =>
		conditions == null || conditions.All(k => Meets(k, c));
}

// --------------------------------------------------------------------------------------------
// FRAGMENTS
// --------------------------------------------------------------------------------------------

/// <summary>One authored line, plus the tags that decide when it can be spoken.</summary>
public sealed class NarrativeFragment {
	public RolodexSceneBeat beat;
	public DJArchetype[] archetypes;          // null = any
	public RapportTier[] tiers;               // null = any
	public Objection objection = Objection.None;
	public string[] conditions;               // ALL must hold
	public int weight = 1;
	public string text;
}

/// <summary>
/// The authored fragment library. Small on purpose: personality comes from the INTERSECTION of
/// archetype, relationship tier, and the live market condition that selected the line -- not from
/// writing a bespoke tree per DJ.
/// </summary>
public static class RolodexFragments {
	private static readonly List<NarrativeFragment> All = new();

	static RolodexFragments() {
		BuildOpenings();
		BuildPushbacks();
		BuildOutcomes();
	}

	private static void BuildOpenings() {
		Add(RolodexSceneBeat.Opening, "Yeah? Make it quick. The coffee is dead and the phones are alive.",
			archetypes: new[] { DJArchetype.Personality }, tiers: new[] { RapportTier.Stranger, RapportTier.Cold });
		Add(RolodexSceneBeat.Opening, "You are calling during the weather. That means you have either got a hit or a problem.",
			archetypes: new[] { DJArchetype.Personality });
		Add(RolodexSceneBeat.Opening, "Speak up, I have got a cart machine eating a commercial back here.",
			archetypes: new[] { DJArchetype.Personality, DJArchetype.Regional });
		Add(RolodexSceneBeat.Opening, "This is the request line, technically. But go ahead.",
			archetypes: new[] { DJArchetype.Personality }, conditions: new[] { "NightShift" });

		Add(RolodexSceneBeat.Opening, "I have got about four minutes before I am back on. Use them well.",
			archetypes: new[] { DJArchetype.Tastemaker });
		Add(RolodexSceneBeat.Opening, "If this is another novelty record about a dance nobody does, I am hanging up.",
			archetypes: new[] { DJArchetype.Tastemaker }, tiers: new[] { RapportTier.Stranger, RapportTier.Cold });
		Add(RolodexSceneBeat.Opening, "Go on then. I am listening, which is more than most people in this building do.",
			archetypes: new[] { DJArchetype.Tastemaker }, tiers: new[] { RapportTier.Friendly, RapportTier.Warm, RapportTier.Loyal });

		Add(RolodexSceneBeat.Opening, "Well well. What is it worth to you today?",
			archetypes: new[] { DJArchetype.Hustler });
		Add(RolodexSceneBeat.Opening, "You caught me between spots. Talk fast, and talk in numbers.",
			archetypes: new[] { DJArchetype.Hustler });
		Add(RolodexSceneBeat.Opening, "A label man. At this hour. Somebody is desperate and it is not me.",
			archetypes: new[] { DJArchetype.Hustler }, tiers: new[] { RapportTier.Stranger, RapportTier.Cold });

		Add(RolodexSceneBeat.Opening, "You will want the program director, but go ahead.",
			archetypes: new[] { DJArchetype.CompanyMan }, tiers: new[] { RapportTier.Stranger, RapportTier.Cold });
		Add(RolodexSceneBeat.Opening, "I play the sheet. I do not write it. Whatever you are about to ask, keep that in mind.",
			archetypes: new[] { DJArchetype.CompanyMan });
		Add(RolodexSceneBeat.Opening, "Make it something I can put in front of the boss without losing an afternoon.",
			archetypes: new[] { DJArchetype.CompanyMan });

		Add(RolodexSceneBeat.Opening, "Long way from wherever you are calling from. What do you want with us?",
			archetypes: new[] { DJArchetype.Regional }, tiers: new[] { RapportTier.Stranger, RapportTier.Cold });
		Add(RolodexSceneBeat.Opening, "I know this market better than any chart does. Bear that in mind.",
			archetypes: new[] { DJArchetype.Regional });

		// Warm and loyal openings cut across archetype -- the relationship is the story now.
		Add(RolodexSceneBeat.Opening, "There he is. I was wondering when you would surface.",
			tiers: new[] { RapportTier.Warm, RapportTier.Loyal });
		Add(RolodexSceneBeat.Opening, "Whatever it is, the answer is probably yes. Ask anyway, I like the ritual.",
			tiers: new[] { RapportTier.Loyal });
		Add(RolodexSceneBeat.Opening, "You again. All right. I have got a minute for you.",
			tiers: new[] { RapportTier.Friendly, RapportTier.Acquaintance });
		Add(RolodexSceneBeat.Opening, "Still spinning that thing you talked me into. Do not push your luck.",
			conditions: new[] { "AlreadyCarrying" });
	}

	private static void BuildPushbacks() {
		Add(RolodexSceneBeat.Pushback, "I can't put on a record I haven't got, friend. Send me one.",
			objection: Objection.NotServiced);
		Add(RolodexSceneBeat.Pushback, "You want me to play something I've never even heard? Get it to me first.",
			objection: Objection.NotServiced);
		Add(RolodexSceneBeat.Pushback, "There's no copy on this desk with your label's name on it. Fix that and call me back.",
			objection: Objection.NotServiced, archetypes: new[] { DJArchetype.CompanyMan });

		Add(RolodexSceneBeat.Pushback, "It is not on my sheet and it never will be. Wrong station, friend. Wrong everything.",
			objection: Objection.FormatShutOut);
		Add(RolodexSceneBeat.Pushback, "You have heard this station, right? Actually heard it? Because that record belongs somewhere else.",
			objection: Objection.FormatShutOut);
		Add(RolodexSceneBeat.Pushback, "I would have to change the format to play it. They do not let me change the format.",
			objection: Objection.FormatShutOut, archetypes: new[] { DJArchetype.CompanyMan });

		Add(RolodexSceneBeat.Pushback, "This sounds like it was cut in a refrigerator. I have got listeners, not hostages.",
			objection: Objection.ProductionRough);
		Add(RolodexSceneBeat.Pushback, "The needle is fighting it. Whatever room you cut this in, do not go back.",
			objection: Objection.ProductionRough);
		Add(RolodexSceneBeat.Pushback, "It is rough. I do not mind rough. The transmitter minds rough.",
			objection: Objection.ProductionRough, archetypes: new[] { DJArchetype.Tastemaker, DJArchetype.Personality });

		Add(RolodexSceneBeat.Pushback, "Nobody is asking for this sound here. Not in this city. Not this year.",
			objection: Objection.NoLocalAudience);
		Add(RolodexSceneBeat.Pushback, "The kids here are buying soul records and their mothers are buying whatever keeps them quiet. That is the whole market.",
			objection: Objection.NoLocalAudience);
		Add(RolodexSceneBeat.Pushback, "I know this town. I have known it twenty years. It is not ready for that.",
			objection: Objection.NoLocalAudience, archetypes: new[] { DJArchetype.Regional });

		Add(RolodexSceneBeat.Pushback, "Who is this act? And do not tell me they are going to be big. Everybody is going to be big.",
			objection: Objection.UnknownArtist);
		Add(RolodexSceneBeat.Pushback, "Never heard the name. Neither has anybody who calls this station.",
			objection: Objection.UnknownArtist);
		Add(RolodexSceneBeat.Pushback, "New acts are a coin flip and I have flipped enough coins this month.",
			objection: Objection.UnknownArtist, archetypes: new[] { DJArchetype.CompanyMan, DJArchetype.Hustler });

		Add(RolodexSceneBeat.Pushback, "Where are the numbers? Show me a store in this city that has moved a copy.",
			objection: Objection.NoSalesSupport);
		Add(RolodexSceneBeat.Pushback, "Radio follows the racks here. Get it in the racks and then call me.",
			objection: Objection.NoSalesSupport);
		Add(RolodexSceneBeat.Pushback, "It was selling. It is not selling now. That is the part that matters to my boss.",
			objection: Objection.NoSalesSupport, conditions: new[] { "SalesWeak" });

		Add(RolodexSceneBeat.Pushback, "You want me to play it, call the station manager. You want me to keep my job, send me something I can defend.",
			objection: Objection.ManagerHeat);
		Add(RolodexSceneBeat.Pushback, "There is a man upstairs who wants to know why I played a saxophone record at eleven in the morning. I am still writing that memo.",
			objection: Objection.ManagerHeat);
		Add(RolodexSceneBeat.Pushback, "Things are hot around here right now. Somebody has been asking questions about the log.",
			objection: Objection.ManagerHeat, conditions: new[] { "Suspicious" });

		Add(RolodexSceneBeat.Pushback, "And what does this do for me, exactly? Spell it out, I am slow today.",
			objection: Objection.WhatsInItForMe);
		Add(RolodexSceneBeat.Pushback, "Everybody has got a record. Not everybody has got a reason for me to care.",
			objection: Objection.WhatsInItForMe);

		Add(RolodexSceneBeat.Pushback, "Last time I took your word I spent a month explaining myself. Why is this different?",
			objection: Objection.YouBurnedMeBefore);
		Add(RolodexSceneBeat.Pushback, "You have been wrong to me before, in this exact tone of voice.",
			objection: Objection.YouBurnedMeBefore);

		Add(RolodexSceneBeat.Pushback, "The slots are gone. They were gone Tuesday. Come back next week and ask again.",
			objection: Objection.PlaylistFull);
		Add(RolodexSceneBeat.Pushback, "I have got thirty-five records and thirty of them are somebody's favour already.",
			objection: Objection.PlaylistFull);
	}

	private static void BuildOutcomes() {
		Add(RolodexSceneBeat.Success, "All right. Midnight. One spin. If the phones stay dead this conversation never happened.",
			conditions: new[] { "NightShift" });
		Add(RolodexSceneBeat.Success, "Fine. I will carry it into the meeting. I am not promising the meeting agrees with me.");
		Add(RolodexSceneBeat.Success, "You get one look. One. I will put it up and we will see who calls.");
		Add(RolodexSceneBeat.Success, "I will spin it and I will watch the board. That is the deal, that is the whole deal.");
		Add(RolodexSceneBeat.Success, "Send me a clean copy and I will make the case for it Thursday.",
			archetypes: new[] { DJArchetype.CompanyMan });
		Add(RolodexSceneBeat.Success, "Good. Something to play that is not the same four records. Send it.",
			archetypes: new[] { DJArchetype.Tastemaker }, conditions: new[] { "OriginalityHigh" });
		Add(RolodexSceneBeat.Success, "For you? Yeah, all right. Do not make me regret the habit.",
			tiers: new[] { RapportTier.Warm, RapportTier.Loyal });

		Add(RolodexSceneBeat.Failure, "No. Nice try, though. Genuinely.");
		Add(RolodexSceneBeat.Failure, "I am going to pass, and I am going to be polite about it, which is more than you will get across town.");
		Add(RolodexSceneBeat.Failure, "Not this one. Bring me the next one.");
		Add(RolodexSceneBeat.Failure, "You did not hear a word I said, did you. The answer is the same as it was two minutes ago.");
		Add(RolodexSceneBeat.Failure, "I have got a meeting. Which is a lie, but it is the polite kind.",
			archetypes: new[] { DJArchetype.CompanyMan });
		Add(RolodexSceneBeat.Failure, "Come back with money or come back with a better record. Either works.",
			archetypes: new[] { DJArchetype.Hustler });

		// Beats between asks -- the player may still keep him on the line, so these must not assert the
		// call has ended. The actual hang-up is the player's own button, not an authored line.
		Add(RolodexSceneBeat.RelationshipAftermath, "He waits, half-listening, to see whether you're done.");
		Add(RolodexSceneBeat.RelationshipAftermath, "A beat of studio noise down the line while he decides how much of his afternoon you get.");
		Add(RolodexSceneBeat.RelationshipAftermath, "He says something to somebody in the studio, hand half over the mouthpiece, then comes back to you.");

		Add(RolodexSceneBeat.Exit, "\"Call me Thursday,\" he says. \"Not Wednesday. Thursday.\"");
		Add(RolodexSceneBeat.Exit, "\"And listen -- if it moves, I want to hear it from you first, not from the trades.\"");
		Add(RolodexSceneBeat.Exit, "\"Next time lead with the chorus,\" he says. \"Saves us both a nickel.\"");
	}

	private static void Add(RolodexSceneBeat beat, string text,
			DJArchetype[] archetypes = null, RapportTier[] tiers = null,
			Objection objection = Objection.None, string[] conditions = null, int weight = 1) =>
		All.Add(new NarrativeFragment {
			beat = beat, text = text, archetypes = archetypes, tiers = tiers,
			objection = objection, conditions = conditions, weight = weight,
		});

	/// <summary>Pick a fragment for a beat. Specificity wins: among everything that matches, the
	/// fragments carrying the most tags are preferred, so a Hustler with a cold relationship gets his
	/// own line rather than the generic one. Ties break on a weighted draw.</summary>
	public static string Pick(RolodexSceneBeat beat, RolodexCallContext c, Objection objection = Objection.None) {
		var eligible = All.Where(f =>
			f.beat == beat
			&& f.objection == objection
			&& (f.archetypes == null || (c.dj != null && f.archetypes.Contains(c.dj.archetype)))
			&& (f.tiers == null || f.tiers.Contains(c.tier))
			&& RolodexConditions.MeetsAll(f.conditions, c)).ToList();
		if (eligible.Count == 0) return null;

		int best = eligible.Max(Specificity);
		var top = eligible.Where(f => Specificity(f) == best).ToList();
		int total = top.Sum(f => f.weight);
		int roll = (int)GD.RandRange(0, Mathf.Max(0, total - 1));
		foreach (NarrativeFragment f in top) { roll -= f.weight; if (roll < 0) return f.text; }
		return top[^1].text;
	}

	private static int Specificity(NarrativeFragment f) =>
		(f.archetypes != null ? 1 : 0) + (f.tiers != null ? 1 : 0) + (f.conditions?.Length ?? 0);
}
