using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE BAND ROOM (SimTools/BandMemberSimulationDirective.md Part B)
//
// Not a second dialogue system: the Rolodex call's grammar with different nouns, the way ContractTalk
// reuses it. A visit is a fixed run of BEATS (RolodexSceneBeat), each selecting an authored fragment whose
// tags match the real situation. The situation is captured ONCE, in a BandRoomContext built from live sim
// values -- the per-stint credit ledger, spotlight shares, road years, the worst edge and its dominant
// cause, the partner record, the substance load, the contract. Every line a member speaks is selected by a
// named condition over that context, so the game can always say why they said it.
//
// The Rolodex rule binds every line (RolodexScene.cs): reveal a real sim fact, express a real motive, or
// execute a real sim action. Romance is gossip-column and off-screen; death is written the way the period
// press wrote it -- plainly, no detail, no comedy.
// ============================================================================================

/// <summary>Why a member is in the office.</summary>
public enum BandVisitKind {
	Grievance,        // strain reached Brewing
	Ultimatum,        // strain reached Ultimatum: a stated demand
	Request,          // a fact-backed ask: a song on the record, a solo single, money, off the road
	FirstHit,         // the first hit lands and the credit conversation starts
	LifeEvent,        // drafted, married, studio-only
	Death,            // a call, not a visit: the facts and the decisions that follow
	AffairSecret,     // the Street hears it before anyone tells you
	AffairDiscovered, // it came out
	Departure,        // someone has gone; who replaces them, if anyone
	SubstanceRead,    // the Street has noticed someone using
}

public enum BandRequest { None, SongOnRecord, SoloSingle, Advance, OffTheRoad }

/// <summary>How a member talks. Derived from personality, never stored: blunt is high ego and a short fuse;
/// apologetic is loyal and modest; evasive is the unreliable one.</summary>
public enum MemberVoice { Measured, Blunt, Apologetic, Evasive }

/// <summary>Every verb in the Band Room writes the sim (directive §5.4).</summary>
public enum BandVerb {
	HearThemOut, ConcedePact, GiveThemASong, PayThem, Mediate, SideWith, SideAgainst, SpotlightThem, TimeOff,
	DryOut, LeanOnContract, TellAboutAffair, KeepQuiet, KeepApart, Fire, HireFromPool, HireNewcomer, CarryOn,
	PutOnIce, ExerciseOption, LetThemGo, ReleaseMasters, HoldMasters, Congratulate, AskPartnerOut, LookAway,
	SoloSingleYes, OffTheRoadYes, AdvanceYes, Decline,
}

/// <summary>A visit waiting in the queue. Persisted; the live scene is rebuilt from it when opened.</summary>
public sealed class BandVisit {
	public string visitId;
	public string artistId;
	public string personId;
	public string otherPersonId;
	public BandVisitKind kind;
	public StrainCause cause;
	public BandRequest request;
	public DepartureKind departureKind;
	public DeathChannel channel;
	public string lifeEvent;
	public int createdWeek;
	public int expiresWeek;
	public int year;
	public bool resolved;
	public string headline;
}

/// <summary>Every real fact a Band Room visit may reference, gathered once when the scene opens.</summary>
public sealed class BandRoomContext {
	public BandVisit visit;
	public SimulatedArtist artist;
	public Musician member;           // who came in (or, for a death call, who died)
	public Musician other;            // the other side of the grievance, when there is one
	public ExecutiveInstinctProfile instincts;
	public MemberVoice voice;
	public LineupConstitution constitution;
	public int year;

	// Grievance facts.
	public MemberRelation edge;
	public float strain;
	public StrainCause cause;
	public DepartureStage stage;
	public int actOriginals;          // originals credited to this act's members
	public int memberSongs, otherSongs;
	public float memberCreditShare, otherCreditShare;
	public bool memberWrites, otherWrites, pactBetween;
	public float memberSpotlight, otherSpotlight;
	public bool soloViable;

	// Person facts.
	public float roadYears, substanceLoad;
	public bool married, hasChildren, partnerSitsIn;
	public string partnerName;
	public float trust;
	public float morale;
	public int age;

	// Secret facts (the Street's channel).
	public MemberRelation secretAffair;  // an affair involving the member, undiscovered
	public string affairPartnerName;     // whose partner it is

	// Act / contract facts.
	public int top40Hits, charted;
	public float unrecoupedAdvance;
	public int contractExpiresYear;
	public bool leavingMemberOption;
	public float labelCash;

	public string MemberName => member?.firstName ?? "";
	public string OtherName => other?.firstName ?? "";
	public string He(Musician m) => m != null && !m.isMale ? "she" : "he";
	public string Him(Musician m) => m != null && !m.isMale ? "her" : "him";
	public string His(Musician m) => m != null && !m.isMale ? "her" : "his";
}

public static class BandRoomConditions {
	public static bool Meets(string condition, BandRoomContext c) => condition switch {
		null or "" => true,
		"Blunt" => c.voice == MemberVoice.Blunt,
		"Apologetic" => c.voice == MemberVoice.Apologetic,
		"Evasive" => c.voice == MemberVoice.Evasive,
		"Measured" => c.voice == MemberVoice.Measured,
		"HasOther" => c.other != null,
		"CreditGap" => c.memberCreditShare + 0.25f < c.otherCreditShare,
		"NoCredits" => c.memberSongs == 0 && c.actOriginals > 0,
		"SomeCredits" => c.memberSongs > 0,
		"MemberWrites" => c.memberWrites,
		"BothWrite" => c.memberWrites && c.otherWrites,
		"Pact" => c.pactBetween,
		"SpotlightGap" => c.otherSpotlight - c.memberSpotlight > 0.25f,
		"MemberIsTheName" => c.memberSpotlight >= 0.5f,
		"SoloViable" => c.soloViable,
		"RoadHeavy" => c.roadYears >= 3f,
		"Married" => c.married,
		"HasKids" => c.hasChildren,
		"PartnerSitsIn" => c.partnerSitsIn,
		"Using" => c.substanceLoad > 0.35f,
		"UsingHeavy" => c.substanceLoad > 0.55f,
		"Charting" => c.top40Hits > 0,
		"NeverCharted" => c.charted == 0,
		"Ultimatum" => c.stage == DepartureStage.Ultimatum,
		"Brewing" => c.stage == DepartureStage.Brewing,
		"MoraleLow" => c.morale < -0.10f,
		"MoraleHigh" => c.morale > 0.08f,
		"TrustLow" => c.trust < 0.35f,
		"TrustHigh" => c.trust > 0.65f,
		"Unrecouped" => c.unrecoupedAdvance > 0f,
		"HasOption" => c.leavingMemberOption,
		"SecretAffair" => c.secretAffair != null,
		"EventDrafted" => c.visit.lifeEvent == "drafted",
		"EventMarriage" => c.visit.lifeEvent == "marriage",
		"EventStudioOnly" => c.visit.lifeEvent == "studio-only",
		"DeathTravel" => c.visit.channel == DeathChannel.Travel,
		"DeathIllness" => c.visit.channel == DeathChannel.Illness,
		"DeathMisadventure" => c.visit.channel == DeathChannel.Misadventure,
		"DeathSubstance" => c.visit.channel == DeathChannel.Substance,
		_ => false,
	};

	public static bool MeetsAll(IEnumerable<string> conditions, BandRoomContext c) => conditions == null || conditions.All(k => Meets(k, c));
}

public sealed class BandRoomFragment {
	public RolodexSceneBeat beat;
	public BandVisitKind[] kinds;      // null = any
	public StrainCause? cause;         // null = any
	public string[] conditions;
	public string text;
}

/// <summary>
/// The authored library. Small on purpose: personality comes from the intersection of the member's voice,
/// the cause, and the fact that selected the line. Placeholders are filled only from the context:
/// {name} {other} {act} {mine} {theirs} {total} {years} {partner} {he} {him} {his} {ohe} {ohim} {ohis}.
/// </summary>
public static class BandRoomFragments {
	private static readonly List<BandRoomFragment> All = new();

	static BandRoomFragments() {
		BuildOpenings();
		BuildSituations();
		BuildExits();
	}

	private static void Add(RolodexSceneBeat beat, string text, BandVisitKind[] kinds = null, StrainCause? cause = null,
		params string[] conditions) =>
		All.Add(new BandRoomFragment { beat = beat, text = text, kinds = kinds, cause = cause,
			conditions = conditions.Length == 0 ? null : conditions });

	private static readonly BandVisitKind[] Complaints = { BandVisitKind.Grievance, BandVisitKind.Ultimatum };

	private static void BuildOpenings() {
		Add(RolodexSceneBeat.Opening, "{name} lets the door bang on the way in and doesn't sit down.", Complaints, null, "Blunt");
		Add(RolodexSceneBeat.Opening, "{name} is waiting in the outer office when you get back. {He} has been there a while.", Complaints, null, "Measured");
		Add(RolodexSceneBeat.Opening, "{name} knocks, says sorry for interrupting, and sits on the edge of the chair.", Complaints, null, "Apologetic");
		Add(RolodexSceneBeat.Opening, "{name} rings from a pay phone. There's traffic behind {him} and {he} keeps saying it's nothing, really.", Complaints, null, "Evasive");
		Add(RolodexSceneBeat.Opening, "{name} comes in with a list. It is written on the back of a set list.", new[] { BandVisitKind.Ultimatum }, null, "Measured");
		Add(RolodexSceneBeat.Opening, "{name} doesn't knock. \"We need to talk about this band,\" {he} says, \"and we need to do it today.\"", new[] { BandVisitKind.Ultimatum }, null, "Blunt");

		Add(RolodexSceneBeat.Opening, "{name} stops by after the session, guitar case still in hand.", new[] { BandVisitKind.Request }, null);
		Add(RolodexSceneBeat.Opening, "{name} catches you at the door on your way out.", new[] { BandVisitKind.Request }, null, "Blunt");
		Add(RolodexSceneBeat.Opening, "The record is in the Top 40 and {name} is in your office before the ink is dry on the trade.", new[] { BandVisitKind.FirstHit }, null);

		Add(RolodexSceneBeat.Opening, "The letter came on Monday. {name} puts it on your desk without a word.", new[] { BandVisitKind.LifeEvent }, null, "EventDrafted");
		Add(RolodexSceneBeat.Opening, "{name} brings cake to the office. There's a ring on {his} hand.", new[] { BandVisitKind.LifeEvent }, null, "EventMarriage");
		Add(RolodexSceneBeat.Opening, "{name} calls from home. {He} sounds more rested than you've heard {him} in years.", new[] { BandVisitKind.LifeEvent }, null, "EventStudioOnly");
		Add(RolodexSceneBeat.Opening, "The phone rings late. It's {act}'s road manager, and he asks if you're sitting down.", new[] { BandVisitKind.Death }, null);
		Add(RolodexSceneBeat.Opening, "Something you heard at the session has been sitting with you since.", new[] { BandVisitKind.AffairSecret }, null);
		Add(RolodexSceneBeat.Opening, "{name} comes in, closes the door, and for a while says nothing at all.", new[] { BandVisitKind.AffairDiscovered }, null);
		Add(RolodexSceneBeat.Opening, "The phone at the office. It's about {act}.", new[] { BandVisitKind.Departure }, null);
		Add(RolodexSceneBeat.Opening, "You've seen {name} at three sessions now. Something isn't right.", new[] { BandVisitKind.SubstanceRead }, null);
	}

	private static void BuildSituations() {
		// CreditAndMoney: the fact is the ledger.
		Add(RolodexSceneBeat.SituationRead, "\"There are {total} of our own songs on the records. My name's on {mine}. {Other}'s is on {theirs}. You do the sums.\"",
			Complaints, StrainCause.CreditAndMoney, "Blunt", "SomeCredits");
		Add(RolodexSceneBeat.SituationRead, "\"{total} originals and not one with my name on it. I'm in that room for every one of them.\"",
			Complaints, StrainCause.CreditAndMoney, "NoCredits");
		Add(RolodexSceneBeat.SituationRead, "\"I don't want to make trouble. It's only -- {total} songs, and I'm credited on {mine}. {Other} has {theirs}.\"",
			Complaints, StrainCause.CreditAndMoney, "Apologetic");
		Add(RolodexSceneBeat.SituationRead, "\"It's the publishing. You know it's the publishing. {total} songs, {mine} with my name.\"",
			Complaints, StrainCause.CreditAndMoney);
		Add(RolodexSceneBeat.SituationRead, "\"We said fifty-fifty on everything, {other} and me. I'd like it written down before it stops being true.\"",
			Complaints, StrainCause.CreditAndMoney, "Pact");

		// Spotlight: the fact is spotlight share.
		Add(RolodexSceneBeat.SituationRead, "\"Every review says '{other} and the band.' I'm the band, apparently.\"",
			Complaints, StrainCause.Spotlight, "SpotlightGap");
		Add(RolodexSceneBeat.SituationRead, "\"They scream for me. You've seen it. And I'm splitting the money five ways.\"",
			Complaints, StrainCause.Spotlight, "MemberIsTheName");
		Add(RolodexSceneBeat.SituationRead, "\"It's about who stands in the middle of the photograph. It's always been about that.\"",
			Complaints, StrainCause.Spotlight);

		// Direction.
		Add(RolodexSceneBeat.SituationRead, "\"{Other} wants to make the same record again. I'm not doing the same record again.\"",
			Complaints, StrainCause.Direction, "Blunt");
		Add(RolodexSceneBeat.SituationRead, "\"We don't want the same things any more, {other} and me. I don't know how else to say it.\"",
			Complaints, StrainCause.Direction);

		// Reliability: the complainer is the reliable one; the fact is the other's record.
		Add(RolodexSceneBeat.SituationRead, "\"{Other} is my buddy. {Ohe}'s also holding us back. Late to the date, late to the stand.\"",
			Complaints, StrainCause.Reliability, "Measured");
		Add(RolodexSceneBeat.SituationRead, "\"I'll say it if nobody else will. {Other} isn't showing up, and when {ohe} does it's worse.\"",
			Complaints, StrainCause.Reliability, "Blunt");
		Add(RolodexSceneBeat.SituationRead, "\"I'm not saying anything about anybody. It's just that some of us are there at eight and some of us aren't.\"",
			Complaints, StrainCause.Reliability, "Apologetic");

		// Burnout: the fact is road years.
		Add(RolodexSceneBeat.SituationRead, "\"{years} years in a station wagon. I'm not a kid any more.\"", Complaints, StrainCause.Burnout, "RoadHeavy");
		Add(RolodexSceneBeat.SituationRead, "\"I've got a family I see on Christmas. That's it. That's the whole visit.\"", Complaints, StrainCause.Burnout, "HasKids");
		Add(RolodexSceneBeat.SituationRead, "\"We're tired. All of us. And when we're tired we're at each other.\"", Complaints, StrainCause.Burnout);

		// Romance, gossip-column register.
		Add(RolodexSceneBeat.SituationRead, "\"You've heard, I suppose. Everybody's heard. I'm not getting in a van with {other}.\"",
			Complaints, StrainCause.Romance);
		Add(RolodexSceneBeat.SituationRead, "\"It's over between us, {other} and me. It isn't over in the band, is it. We still have to stand next to each other.\"",
			Complaints, StrainCause.Romance, "Apologetic");

		// Outsider.
		Add(RolodexSceneBeat.SituationRead, "\"Somebody who isn't in this band keeps telling this band what to do.\"", Complaints, StrainCause.Outsider);
		Add(RolodexSceneBeat.SituationRead, "\"There's a stranger on our record. You put a stranger on our record.\"", Complaints, StrainCause.Outsider, "Blunt");

		// Requests and first hit.
		Add(RolodexSceneBeat.SituationRead, "\"I've got songs. {total} sides and none of them are mine. I'd like one on the next record.\"",
			new[] { BandVisitKind.Request, BandVisitKind.FirstHit }, StrainCause.CreditAndMoney, "NoCredits");
		Add(RolodexSceneBeat.SituationRead, "\"It's a hit. Good. Now -- {total} of our songs, and my name is on {mine}. That ought to change while it's worth changing.\"",
			new[] { BandVisitKind.FirstHit }, StrainCause.CreditAndMoney, "SomeCredits");
		Add(RolodexSceneBeat.SituationRead, "\"People know my name now. I'd like to cut one under it. Just the one.\"",
			new[] { BandVisitKind.Request }, StrainCause.Spotlight);
		Add(RolodexSceneBeat.SituationRead, "\"I'm married. There'll be a baby. I need money that isn't a promise.\"",
			new[] { BandVisitKind.Request }, StrainCause.CreditAndMoney, "Married");
		Add(RolodexSceneBeat.SituationRead, "\"{years} years on the road. I want to make records. I don't want to make another mile.\"",
			new[] { BandVisitKind.Request }, StrainCause.Burnout);

		// Life events.
		Add(RolodexSceneBeat.SituationRead, "Greetings from the President of the United States. {name} reports in two weeks. Two years, the letter says.",
			new[] { BandVisitKind.LifeEvent }, null, "EventDrafted");
		Add(RolodexSceneBeat.SituationRead, "{name} is off the road for good. {He}'ll make the records. {He} won't make the dates.",
			new[] { BandVisitKind.LifeEvent }, null, "EventStudioOnly");
		Add(RolodexSceneBeat.SituationRead, "{name} got married on Saturday. {partner} has been at the last two sessions, and means to keep coming.",
			new[] { BandVisitKind.LifeEvent }, null, "EventMarriage", "PartnerSitsIn");
		Add(RolodexSceneBeat.SituationRead, "{name} got married on Saturday, to {partner}. The band were all there.",
			new[] { BandVisitKind.LifeEvent }, null, "EventMarriage");

		// Death: the period press register.
		Add(RolodexSceneBeat.SituationRead, "The plane went down short of the field. {name} was on it.", new[] { BandVisitKind.Death }, null, "DeathTravel");
		Add(RolodexSceneBeat.SituationRead, "The car left the road outside of town between dates. {name} was driving.", new[] { BandVisitKind.Death }, null, "DeathTravel");
		Add(RolodexSceneBeat.SituationRead, "{name} was found at home on Tuesday morning. That's all anyone is saying.", new[] { BandVisitKind.Death }, null, "DeathSubstance");
		Add(RolodexSceneBeat.SituationRead, "{name} died in the hospital this afternoon. {He} was {age}.", new[] { BandVisitKind.Death }, null, "DeathIllness");
		Add(RolodexSceneBeat.SituationRead, "There was an accident. {name} is dead. The police say they're looking into it.", new[] { BandVisitKind.Death }, null, "DeathMisadventure");

		Add(RolodexSceneBeat.SituationRead, "{name} has been seen with {partner} -- {other}'s {partnerRole} -- more than a bandmate would be. {Other} doesn't know.",
			new[] { BandVisitKind.AffairSecret });
		Add(RolodexSceneBeat.SituationRead, "\"Everybody knew. Everybody but me. {Other}, of all people.\"", new[] { BandVisitKind.AffairDiscovered });

		Add(RolodexSceneBeat.SituationRead, "{name} is gone. {departure}", new[] { BandVisitKind.Departure });

		Add(RolodexSceneBeat.SituationRead, "{name} nodded off between takes. The second time, nobody laughed.", new[] { BandVisitKind.SubstanceRead }, null, "UsingHeavy");
		Add(RolodexSceneBeat.SituationRead, "{name}'s pupils, the sniffing, the money {he} keeps borrowing from the drummer.", new[] { BandVisitKind.SubstanceRead });
	}

	private static void BuildExits() {
		Add(RolodexSceneBeat.Exit, "\"Think about it. I'm not doing the tour like this.\"", Complaints, StrainCause.Burnout);
		Add(RolodexSceneBeat.Exit, "\"Sort it out, or the next time you see me it'll be my lawyer.\"", new[] { BandVisitKind.Ultimatum }, null, "Blunt");
		Add(RolodexSceneBeat.Exit, "\"It's {other} or me. I'm sorry. That's where it is now.\"", new[] { BandVisitKind.Ultimatum }, null, "HasOther");
		Add(RolodexSceneBeat.Exit, "\"I'll give it till the end of the year. That's fair, isn't it?\"", new[] { BandVisitKind.Ultimatum }, null, "Apologetic");
		Add(RolodexSceneBeat.Exit, "\"Half the B-sides. That's all I'm asking. Half.\"", new[] { BandVisitKind.Ultimatum }, StrainCause.CreditAndMoney);
		Add(RolodexSceneBeat.Exit, "\"Off the road, or out of the band. You choose.\"", new[] { BandVisitKind.Ultimatum }, StrainCause.Burnout);
		Add(RolodexSceneBeat.Exit, "\"It'll keep. For now.\"", new[] { BandVisitKind.Grievance }, null);
		Add(RolodexSceneBeat.Exit, "\"I just wanted somebody to know.\"", new[] { BandVisitKind.Grievance }, null, "Apologetic");
		Add(RolodexSceneBeat.Exit, "{He} leaves the door open on the way out.", Complaints, null, "Blunt");
	}

	/// <summary>
	/// Pick a fragment for a beat. Specificity wins (the most tags that still match), and ties break on a keyed
	/// draw so the same visit always says the same thing -- reopening a visit doesn't reroll the room.
	/// </summary>
	public static string Pick(RolodexSceneBeat beat, BandRoomContext c, string salt = "") {
		var eligible = All.Where(f => f.beat == beat &&
			(f.kinds == null || f.kinds.Contains(c.visit.kind)) &&
			(f.cause == null || f.cause == c.cause) &&
			BandRoomConditions.MeetsAll(f.conditions, c)).ToList();
		if (eligible.Count == 0) return null;
		int best = eligible.Max(Specificity);
		var top = eligible.Where(f => Specificity(f) == best).ToList();
		int index = (int)(BandLife.Unit($"bandroom|{c.visit.visitId}|{beat}|{salt}") * top.Count);
		return Fill(top[Math.Clamp(index, 0, top.Count - 1)].text, c);
	}

	private static int Specificity(BandRoomFragment f) =>
		(f.kinds != null ? 1 : 0) + (f.cause != null ? 1 : 0) + (f.conditions?.Length ?? 0);

	public static string Fill(string text, BandRoomContext c) {
		static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
		return text
			.Replace("{name}", c.MemberName).Replace("{Other}", Cap(c.OtherName)).Replace("{other}", c.OtherName)
			.Replace("{act}", c.artist?.stageName ?? "the act")
			.Replace("{mine}", c.memberSongs.ToString()).Replace("{theirs}", c.otherSongs.ToString())
			.Replace("{total}", c.actOriginals.ToString()).Replace("{years}", Mathf.RoundToInt(c.roadYears).ToString())
			.Replace("{partner}", c.affairPartnerName ?? c.partnerName ?? "someone").Replace("{age}", c.age.ToString())
			.Replace("{partnerRole}", c.other?.partner?.state == PartnerState.Married ? (c.other.partner.isMale ? "husband" : "wife") : "steady")
			.Replace("{departure}", DepartureLine(c))
			.Replace("{He}", Cap(c.He(c.member))).Replace("{he}", c.He(c.member)).Replace("{him}", c.Him(c.member)).Replace("{his}", c.His(c.member))
			.Replace("{Ohe}", Cap(c.He(c.other))).Replace("{ohe}", c.He(c.other)).Replace("{ohim}", c.Him(c.other)).Replace("{ohis}", c.His(c.other));
	}

	private static string DepartureLine(BandRoomContext c) => c.visit.departureKind switch {
		DepartureKind.SoloCareer => "Going out under their own name. The trades will have it Thursday.",
		DepartureKind.Fired => "The others put it to a vote.",
		DepartureKind.Service => "Uncle Sam. Two years.",
		DepartureKind.LifeEvent => "Off the road and out of music, the way it's told.",
		DepartureKind.Dissolution => "And without them there isn't a band.",
		_ => c.visit.cause switch {
			StrainCause.CreditAndMoney => "It was the money, in the end. It usually is.",
			StrainCause.Direction => "Musical differences, is how the trades will put it.",
			StrainCause.Romance => "Nobody's saying why. Everybody knows.",
			_ => "Walked out of rehearsal and didn't come back."
		}
	};
}
