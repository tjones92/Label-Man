using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// THE FIRST WEEK (SimTools/OnboardingTutorialDirective.md §3.2, T3).
//
// A short to-do card in the Label Man's own hand that walks a new player through the first signing and a first record,
// riding the real verbs: hear a set, ask if they'd hear an offer, get their yes, put a deal on the table, sign them, set up
// a first record. Each line ticks from real state, never from a button press the card counted, so any step can be done
// out of order, or skipped, and the card just catches up. It ends with an act of the player's own on the roster and a
// first record set up (author decision 3): a session booked, or anything past it.
//
// Each voice teaches its own task (decision 2): the Ear hearing a set, the Street the rooms and who's moving, the Suit the
// contract and the money, the Fixer the numbers and the waiting. A Sawyer run's "first record" can be Connie's.
//
// Player-only: the state is three flags on the player's save, read by nothing in the sim.
// ============================================================================================
public enum FirstWeekStep { Hear, Ask, Answer, Terms, Sign, Record }

/// <summary>Where the work of a line happens, and so where its voice teaches it (SigningExperienceAudit §3.5): the card
/// itself only lists the lines and points the way.</summary>
public enum FirstWeekPlace { FindASet, TheTalk, TheTable, Roster, Studio }

/// <summary>The first-week card's own state. Absent in older saves: a fresh card, which ticks itself off from what the
/// run has already done, so a save past its first record never sees it.</summary>
public sealed class FirstWeekSaveData {
	/// <summary>The player put the card away ("I know the way").</summary>
	public bool Off { get; set; }
	/// <summary>Every line ticked once; it doesn't come back.</summary>
	public bool Done { get; set; }
	/// <summary>A terms discussion has been opened with an act (the contract sheet was on the table).</summary>
	public bool TermsOpened { get; set; }
}

public partial class PlayerDesk {
	private FirstWeekSaveData firstWeek = new();

	public static readonly FirstWeekStep[] FirstWeekSteps = System.Enum.GetValues<FirstWeekStep>();

	/// <summary>The card is on the desk: a label, not over, not put away, not finished.</summary>
	public bool FirstWeekActive {
		get {
			if (Label == null || IsGameOver || firstWeek.Off || firstWeek.Done) return false;
			if (FirstWeekSteps.All(FirstWeekDone)) { firstWeek.Done = true; return false; }
			return true;
		}
	}

	/// <summary>The first line not yet ticked, or null when the card is off the desk.</summary>
	public FirstWeekStep? FirstWeekCurrent => FirstWeekActive ? FirstWeekSteps.First(s => !FirstWeekDone(s)) : null;

	private bool OwnActSigned => Label?.roster?.Any(a => !IsHeldByHolder(a)) == true;

	/// <summary>Whether a line is ticked, read from the run's own state. A later step done ticks the ones it implies.</summary>
	public bool FirstWeekDone(FirstWeekStep step) {
		if (Label == null) return false;
		return step switch {
			FirstWeekStep.Record => pendingSession != null || masters.Count > 0 || planned.Count > 0 || pressOrders.Count > 0 || ReleasedRecords.Any(),
			FirstWeekStep.Sign => OwnActSigned,
			FirstWeekStep.Terms => firstWeek.TermsOpened || OwnActSigned,
			FirstWeekStep.Answer => scoutingKnowledge.Approaches?.Any(a => a.Status == ScoutingApproachStatus.Ready ||
				a.Status == ScoutingApproachStatus.Agreed && a.Steps.All(s => s.Done)) == true || FirstWeekDone(FirstWeekStep.Terms),
			FirstWeekStep.Ask => scoutingKnowledge.Approaches?.Count > 0 || FirstWeekDone(FirstWeekStep.Answer),
			FirstWeekStep.Hear => scoutingKnowledge.Dossiers.Any(d => HasScoutingEvidence(d.ArtistId)) || FirstWeekDone(FirstWeekStep.Ask),
			_ => false,
		};
	}

	/// <summary>The earliest step still to take before any act that said yes signs, or null.</summary>
	public ScoutingApproachStep FirstPendingApproachStep => scoutingKnowledge.Approaches?
		.Where(a => a.Status == ScoutingApproachStatus.Agreed)
		.Select(a => NextApproachStep(ArtistManager.Instance?.GetArtist(a.ArtistId))).Where(s => s != null)
		.OrderBy(s => s.Day ?? int.MaxValue).FirstOrDefault();

	/// <summary>The first visit (Sunday dinner or a sit-down) still to make before an act that said yes signs, or null.</summary>
	public ScoutingApproachStep FirstWeekVisit => scoutingKnowledge.Approaches?
		.Where(a => a.Status == ScoutingApproachStatus.Agreed)
		.SelectMany(a => a.Steps.Where(s => !s.Done && s.Kind is ScoutingApproachStepKind.Parents or ScoutingApproachStepKind.Manager))
		.OrderBy(s => s.Day ?? int.MaxValue).FirstOrDefault();

	/// <summary>The first-week visit is booked for today, and its hour hasn't come.</summary>
	public bool FirstWeekVisitToday => FirstWeekVisit is { Minute: { } m, Day: { } d } && d == SceneDay && m >= NowMinute;

	/// <summary>How long before a visit the first week sends him out of the door: the walk, and a little early.</summary>
	public const int VisitLeaveMinutes = 60;

	/// <summary>The minute the first week says to leave for today's visit, or null when there's none today.</summary>
	public int? FirstWeekLeaveAt => FirstWeekVisitToday && FirstWeekVisit.Minute is { } m ? m - VisitLeaveMinutes : null;

	/// <summary>Today's visit isn't for a while: the hours before it are his (playtest 2026-11-27: the glow sent him to GO
	/// THERE at 9 AM, and a 4 PM sit-down ate the day). The first week points at what fits before he has to leave.</summary>
	public bool FirstWeekVisitLater => FirstWeekLeaveAt is { } leave && NowMinute < leave;

	/// <summary>Every act asked so far said no (or let its yes lapse): the answer line's work is hearing somebody else and
	/// asking them, not waiting.</summary>
	public bool FirstWeekNeedsAnotherAct => scoutingKnowledge.Approaches?.Count > 0
		&& scoutingKnowledge.Approaches.All(a => a.Status is ScoutingApproachStatus.Declined or ScoutingApproachStatus.Lapsed);

	/// <summary>An act asked to be heard again before it answers, and nobody else is on the way to a yes: its id. The work is
	/// catching its next set and asking again, not waiting by the phone.</summary>
	public string FirstWeekHearAgainId => FirstWeekCurrent == FirstWeekStep.Answer
		&& scoutingKnowledge.Approaches?.All(a => a.Status is not (ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready or ScoutingApproachStatus.Agreed)) == true
		? scoutingKnowledge.Approaches.LastOrDefault(a => a.Status == ScoutingApproachStatus.WantsAnotherLook)?.ArtistId : null;

	/// <summary>The card is still on finding an act: hearing, asking, hearing somebody else after a no, or hearing the act
	/// that asked to be heard again.</summary>
	public bool FirstWeekLooking => FirstWeekCurrent is FirstWeekStep.Hear or FirstWeekStep.Ask
		|| FirstWeekCurrent == FirstWeekStep.Answer && (FirstWeekNeedsAnotherAct || FirstWeekHearAgainId != null);

	/// <summary>A terms discussion opened (PlayerDesk.ApproachToSign): the "put a deal on the table" line ticks.</summary>
	private void MarkFirstWeekTerms() => firstWeek.TermsOpened = true;

	/// <summary>"I know the way": the card goes in the drawer for good.</summary>
	public void PutAwayFirstWeek() {
		firstWeek.Off = true;
		Changed?.Invoke();
	}

	/// <summary>The line as it's written on the card, in the Label Man's hand.</summary>
	public static string FirstWeekTitle(FirstWeekStep step) => step switch {
		FirstWeekStep.Hear => "Go out and hear somebody",
		FirstWeekStep.Ask => "Ask if they'd hear an offer",
		FirstWeekStep.Answer => "Get their yes",
		FirstWeekStep.Terms => "Put a deal on the table",
		FirstWeekStep.Sign => "Sign them",
		_ => "Set up a first record",
	};

	/// <summary>Which voice teaches the line, and how. The voice is the one whose job it is (decision 2), and it talks about
	/// the work, never the buttons: the glow and SHOW ME point at those. It speaks at any instinct (SigningExperienceAudit
	/// §5 decision 2), but a weak voice (under 3) teaches in its own blunt register and claims no read it can't make.</summary>
	public (ExecutiveVoice Voice, string How) FirstWeekHow(FirstWeekStep step) {
		bool declinedOnly = FirstWeekNeedsAnotherAct;
		bool anotherLook = FirstWeekHearAgainId != null;
		var pending = FirstPendingApproachStep?.Kind;
		bool she = FirstPendingApproachStep?.Female == true;
		string him = she ? "her" : "him", his = she ? "her" : "his", he = she ? "she" : "he", He = she ? "She" : "He";
		return step switch {
			// The first appointment teaches the book, once (§4c): pick a time, write it down, and turn up. Nobody reminds you.
			FirstWeekStep.Answer when pending == ScoutingApproachStepKind.Parents => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "That's a yes, but somebody in that act is under 21, and in this business the mother signs. Pick a Sunday and write it in the book: a mother doesn't set a place twice, and nobody in this office is going to remind you. Eat what's put in front of you and tell her the truth about the money."
				: "They said yes, but one of them's under 21, so a parent signs. Pick a Sunday, write it in the book, and don't miss it."),
			FirstWeekStep.Answer when pending == ScoutingApproachStepKind.Manager => (ExecutiveVoice.Suit, StrongVoice(ExecutiveVoice.Suit)
				? $"A yes with a manager attached means the paper goes through {him}. {He}'ll give you two times: take one and write it in the book, because nobody in this office is going to remind you. Then be there on the hour. Listen more than you talk: whatever {he} asks for, {he}'ll ask for again at the table."
				: $"They've got a manager, so it goes through {him}. Take one of {his} times, write it in the book, and turn up."),
			FirstWeekStep.Answer when pending == ScoutingApproachStepKind.Holdout => (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
				? "One of them isn't sold, and a band moves at the speed of its slowest yes. Be in the room for their next set. A holdout comes round for a label that turns up."
				: "One of them's not sure. Go to their next set and stand where they can see you."),
			FirstWeekStep.Hear => (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
				? "Handbills lie and so do managers. A set doesn't. Pick a room tonight, stand near the back, and I'll tell you what I heard."
				: "Don't ask me who's good. I can tell you who was loud. Go hear a set anyway: what we hear goes in the notebook, and that's more than a handbill gives you."),
			FirstWeekStep.Ask => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "Most acts on a bill would jump at a record, but there's always something: a kid whose mother has to sign, a manager who wants his sit-down, a steady job. So ask if they'd hear an offer. It's free, and they'll tell you straight. If somebody's already shopping, skip the asking and sit down on terms."
				: "I couldn't tell you who's shopping in this town. Nobody can till you ask. It's free, and they'll tell you why if it's no."),
			FirstWeekStep.Answer when anotherLook => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "They want to see you come back. That's not a no: a label that turns up twice means it. Catch their next set, then ask again."
				: "They want you to come back and hear them again. So go back. Then ask again."),
			FirstWeekStep.Answer when declinedOnly => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "They passed, and they told you why. Believe them: that's what they want, not what they think of you. Somebody else is on a stage tonight."
				: "They said no. Don't ask me why. Go hear somebody else and ask them."),
			FirstWeekStep.Answer => (ExecutiveVoice.Fixer, StrongVoice(ExecutiveVoice.Fixer)
				? "Now you wait, and you don't wait by the phone. Their side calls back inside a week; the morning paper says who. One ask out is a hope. Two is a plan."
				: "Waiting's the job now. They'll call back inside a week, and it'll be in the morning paper. Meanwhile go hear somebody else."),
			FirstWeekStep.Terms => (ExecutiveVoice.Suit, StrongVoice(ExecutiveVoice.Suit)
				? "Here's the arithmetic. The advance is their money paid early: it goes out now and comes back out of their royalty, if the records sell. The royalty is their cut of every copy. What an act nobody's heard of asks for is usually near fair. Don't get clever on a first contract."
				: "Paper isn't my strong suit, so keep it plain. The advance is cash now, and it comes back out of their royalty; the royalty is their share of each record. Give them close to what they ask and use the standard paper."),
			FirstWeekStep.Sign => (ExecutiveVoice.Suit, StrongVoice(ExecutiveVoice.Suit)
				? "Put it in front of them. If they push back, give one inch at a time: a little more advance or a little less term, never both. The last twenty dollars isn't worth the act."
				: "Put it to them. If they balk, give a little and try again. Don't fight over pocket change."),
			_ => (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
				? "Now the part that matters. Take the songs that held the room, book the hours, and get them on tape while they still sound the way they did that night."
				: "Get them in front of a microphone. Use the songs from their set: somebody in that room liked them, even if I couldn't tell you which."),
		};
	}

	/// <summary>The pop-over slip at the glow (OnboardingTutorialDirective.md §4b): a short line from the voice whose job it
	/// is, steering a body that doesn't know what it's doing. It names the thing to press and why, claims no read (so it
	/// speaks at any instinct), and leaves the explaining to the lines on the page. <paramref name="target"/> is the glowing
	/// control's text, or "song" / "blank" / "give" for the studio's song boxes and the terms sheet's fields;
	/// <paramref name="several"/> is true when more than one thing glows. Null when there's nothing to say.</summary>
	public (ExecutiveVoice Voice, string Text)? FirstWeekNudge(string target, bool several) {
		if (FirstWeekCurrent is not { } step || string.IsNullOrEmpty(target)) return null;
		bool Is(string prefix) => target.StartsWith(prefix, System.StringComparison.Ordinal);
		// The morning after is real: on New Year's Day and the day after, he is hungover (the fatigue mechanic takes this
		// over once it's built: LabelManFatigueDirective.md §2.4).
		var date = TimeManager.Instance?.CurrentDate;
		bool hungover = date is { year: 1960, month: 1, day: <= 2 };
		// The appointment book: their times, the book itself, and the day it's for.
		if (target.EndsWith("suits me.", System.StringComparison.Ordinal))
			return (ExecutiveVoice.Street, "Pick one and write it down. Nobody in this office is going to remind you.");
		if (target == "THE BOOK")
			return (ExecutiveVoice.Street, "Check the book. It's today, isn't it? The book won't tell you. You have to look.");
		// Hours to spare before today's visit: use them, and be back to leave on time.
		if (FirstWeekVisitLater && FirstWeekVisit is { } later && (several || target is "GO & WAIT" or "LISTEN" or "WAIT 1h" or "WAIT 3h")) {
			string at = ClockText(later.Minute ?? 0), leave = ClockText(FirstWeekLeaveAt ?? 0);
			string who = later.Kind == ScoutingApproachStepKind.Parents ? "Dinner" : later.Who.Split(' ')[0];
			return (ExecutiveVoice.Suit, several || target == "GO & WAIT"
				? $"{who} isn't till {at}. The day's yours till {leave}: hear a set that's done by then. Don't sit in a waiting room all day."
				: target == "LISTEN" ? $"Listen, then get out. You leave for {who} at {leave}."
				: $"Nothing on that's over by {leave}. Sit by the phone till it's time to go.");
		}
		if (Is("GO THERE"))
			return (ExecutiveVoice.Suit, "Time to go. Early is on time.");
		if (target == "END THE DAY" && FirstWeekVisit is { } visit && Booked(visit))
			return (ExecutiveVoice.Fixer, "It's in the book. Nothing to do about it till the day. Go home.");
		if (several || target is "GO & WAIT")
			return (ExecutiveVoice.Street, step == FirstWeekStep.Hear && !scoutingKnowledge.Dossiers.Any(d => HasScoutingEvidence(d.ArtistId))
				? "You have no clue how this all works, do you? We need to sign a fuckin' act! Pick a room. Any of them."
				: "Somebody else, then. Pick a set and get out the door.");
		if (target is "FIND A SET" or "NEXT SETS")
			return (ExecutiveVoice.Street, "Not the paperwork. The sets. Where the bands are.");
		if (target == "LISTEN")
			return (ExecutiveVoice.Ear, "Shut up and listen. That's the whole job tonight.");
		if (target is "TALK TO THEM" or "SIT DOWN ON TERMS")
			return (ExecutiveVoice.Street, "That one. Go and talk to them before they pack up the van.");
		if (target == "LET THEM GO")
			return (ExecutiveVoice.Ear, "Not them. Let them go and wait for the one we came for.");
		if (Is("▸  Would you hear an offer"))
			return (ExecutiveVoice.Street, "Ask them. Just ask! It's free, and a no won't kill you.");
		if (Is("▸  I'll come to Sunday dinner") || target == "MEET THE FAMILY")
			return (ExecutiveVoice.Street, "Sunday dinner. Clean shirt, if you've still got one. Say grace.");
		if (Is("▸  I'll sit down with") || target == "SIT DOWN WITH THE MANAGER")
			return (ExecutiveVoice.Suit, FirstPendingApproachStep?.Female == true
				? "The manager. Listen more than you talk. Whatever she asks for, she'll ask for again."
				: "The manager. Listen more than you talk. Whatever he asks for, he'll ask for again.");
		if (Is("▸  Let's sit down on terms"))
			return (ExecutiveVoice.Suit, "Now the paper. Let me do the thinking.");
		if (target == "END THE CONVERSATION")
			return (ExecutiveVoice.Fixer, "We're done here. Walk away like you meant to.");
		if (target == "END THE DAY")
			return step == FirstWeekStep.Answer
				? (ExecutiveVoice.Fixer, "Go home. They'll answer. Nobody calls back a man standing in their doorway.")
				: (ExecutiveVoice.Fixer, "That's the day. Go home before you do something else.");
		if (Is("TABLE OFFER"))
			return (ExecutiveVoice.Suit, hungover
				? "Look at you, hungover. You can't even put a proposal together. The standard paper's fine. Don't touch anything."
				: "That's the standard paper, and it's fine for a first deal. Put it down before you fiddle with it.");
		if (target == "SWEETEN IT")
			return (ExecutiveVoice.Suit, "That's a price, not a no. Give them a little. A little!");
		if (target == "blank")
			return (ExecutiveVoice.Suit, "That number. Halfway to theirs. Halfway, not all the way.");
		if (target == "give")
			return (ExecutiveVoice.Suit, "That box. Give it to them. It's not worth losing the act over.");
		if (Is("TABLE AGAIN"))
			return (ExecutiveVoice.Suit, "Put it back in front of them.");
		if (target == "NOT NOW")
			return (ExecutiveVoice.Fixer, "Nobody signs anything good this late. Ask me how I know.");
		if (target == "TO THE ROSTER")
			return (ExecutiveVoice.Ear, "We've got a band. Now we need a record.");
		if (Is("TAKE INTO STUDIO") || Is("RETURN TO STUDIO"))
			return (ExecutiveVoice.Ear, "Studio. Don't overthink it.");
		if (target == "song")
			return (ExecutiveVoice.Ear, "Two songs. The ones they play best, not the ones you like.");
		if (Is("BOOK THE ROOM") || Is("BOOK IT"))
			return (ExecutiveVoice.Suit, "Book it before you lose your nerve. Or the money.");
		if (target == "SET THIS ASIDE")
			return (ExecutiveVoice.Ear, "They hate it. Drop it and pick another.");
		if (Is("INSIST"))
			return (ExecutiveVoice.Ear, "Nothing else on the sheet they'll sing. Push, or go back for a song they want.");
		if (Is("‹ BACK"))
			return (ExecutiveVoice.Fixer, "Leave it for today. Come back in the morning.");
		return null;
	}

	/// <summary>A voice that can read (3 and up, the Rolodex threshold); under that it teaches but claims nothing.</summary>
	private bool StrongVoice(ExecutiveVoice voice) => InstinctScore(InstinctProfile, voice) >= 3;

	/// <summary>A second voice on the line, when there's something it should know: what a read means once you've heard a set,
	/// and on a Sawyer run, the singer already on the roster. Null when there's nothing to add.</summary>
	public (ExecutiveVoice Voice, string Aside)? FirstWeekAside(FirstWeekStep step) {
		bool connieWaiting = Connie != null && !FirstWeekDone(FirstWeekStep.Record);
		switch (step) {
			case FirstWeekStep.Ask:
				return (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
					? "Whatever I heard, it was one night in one room. A roadhouse eats a voice and a coffeehouse flatters one. Don't bury a singer for a bad room."
					: "Whatever I wrote down, it was one night in one room, and rooms lie too.");
			case FirstWeekStep.Answer when connieWaiting:
				return (ExecutiveVoice.Street, $"While you wait: Dee-Dee wants {ConnieStageName}'s record out this year, and her girl's already on your roster.");
			case FirstWeekStep.Record when FirstWeekStudioTomorrow:
				return FirstWeekTooLate();
			case FirstWeekStep.Record when connieWaiting && !OwnActSigned:
				return (ExecutiveVoice.Street, $"Nobody of your own yet? Then cut {ConnieStageName}. Dee-Dee's girl is on the roster and she needs songs. Her record counts.");
			default:
				return null;
		}
	}

	/// <summary>The current line, taught in the place its work happens (SigningExperienceAudit §3.5): the voice is in the
	/// room when the thing happens, not on a card above the tabs. Hearing and waiting at the set finder; asking, and the
	/// visit a yes still needs, in The Talk; the deal and the signature at The Table; the first record on the roster. The
	/// Set carries its own teaching verdict (PlayerDesk.TheSet). Empty when the card is away or the line isn't this place's.</summary>
	public List<(ExecutiveVoice Voice, string Text)> FirstWeekLinesAt(FirstWeekPlace place) {
		var lines = new List<(ExecutiveVoice, string)>();
		if (FirstWeekCurrent is not { } step) return lines;
		void Line(FirstWeekStep s) => lines.Add(FirstWeekHow(s));
		void Aside(FirstWeekStep s) { if (FirstWeekAside(s) is { } a) lines.Add((a.Voice, a.Aside)); }
		switch (place) {
			case FirstWeekPlace.FindASet when step is FirstWeekStep.Hear or FirstWeekStep.Ask or FirstWeekStep.Answer:
				Line(step); Aside(step); break;
			case FirstWeekPlace.TheTalk when step == FirstWeekStep.Ask:
			case FirstWeekPlace.TheTalk when step == FirstWeekStep.Answer && FirstWeekNeedsAnotherAct:
				Line(FirstWeekStep.Ask); break;
			case FirstWeekPlace.TheTalk when step == FirstWeekStep.Answer && FirstPendingApproachStep != null:
			case FirstWeekPlace.TheTalk when step == FirstWeekStep.Answer && FirstWeekHearAgainId != null:
				Line(step); break;
			// Opening the table ticks "put a deal on the table", so the sheet is where both lines are taught.
			case FirstWeekPlace.TheTable when step is FirstWeekStep.Terms or FirstWeekStep.Sign:
				Line(FirstWeekStep.Terms); Line(FirstWeekStep.Sign);
				if (FirstWeekTableTomorrow) lines.Add((ExecutiveVoice.Fixer, StrongVoice(ExecutiveVoice.Fixer)
					? $"Not tonight. Sitting down on paper is a {NegotiationRoundHours}-hour meeting and there isn't that left in the day. They'll keep: say good night, end the day, and come back to the table in the morning."
					: "Too late for a meeting. End the day and come back in the morning."));
				break;
			case FirstWeekPlace.Roster when step == FirstWeekStep.Record:
				Line(step); Aside(step); break;
			// The booking sheet is long and every line of it is a choice: the Ear on the songs, the Suit on leaving the rest.
			case FirstWeekPlace.Studio when step == FirstWeekStep.Record:
				lines.AddRange(FirstWeekBooking()); break;
		}
		return lines;
	}

	/// <summary>The record line, but too late in the day for even the shortest date: the work is ending the day and booking
	/// in the morning. (A late-night evening rate could still fit; a first record doesn't need one.)</summary>
	public bool FirstWeekStudioTomorrow => FirstWeekCurrent == FirstWeekStep.Record && pendingSession == null
		&& TimeManager.Instance is { } time && time.MinutesRemainingWithOvertime < MinSessionHours * 60;

	/// <summary>The deal line, but too late in the day for the meeting that puts the paper down: the work is tomorrow's.</summary>
	public bool FirstWeekTableTomorrow => FirstWeekCurrent == FirstWeekStep.Sign
		&& TimeManager.Instance is { } time && !time.CanAffordHours(NegotiationRoundHours, allowOvertime: true);

	/// <summary>The first booking, taught on the sheet: which songs (the Ear), and that the rest of the sheet is fine as it
	/// stands for a first single (the Suit). The sheet's defaults are the town's first room and the standard hours.</summary>
	private IEnumerable<(ExecutiveVoice, string)> FirstWeekBooking() {
		if (FirstWeekStudioTomorrow) {
			yield return FirstWeekTooLate();
			yield break;
		}
		yield return (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
			? "Two songs make a single: one to sell it and one for the back. Tick the two they'd play first if the room was walking out. COMPARE MATERIAL costs nothing, if you want a second opinion."
			: "Tick two songs. I won't pretend to know the hit. Take the ones they know best: a nervous band plays its old songs better.");
		yield return (ExecutiveVoice.Suit, StrongVoice(ExecutiveVoice.Suit)
			? "Everything under the songs is for later. The first room in town at the hours on the sheet is the right size for a first single: cheap, and plenty of time for two sides. No producer, no extra singers, no lockout. Book it and spend the money on pressing."
			: "Leave the rest of the sheet as it is. The first room in town and the hours it shows will do. Don't buy extras on a first record.");
	}

	/// <summary>The deal line when the act counters at the table. On the counter, the Suit says it's a price, not a no:
	/// give them part of what they named and put it back. Back on the sheet, he names the term they're stuck on, their
	/// number and yours, and says to move that one about halfway. Empty for a renewal, off the deal line, or before a counter.</summary>
	public List<(ExecutiveVoice Voice, string Text)> FirstWeekCounter(ContractTalk talk) {
		var lines = new List<(ExecutiveVoice, string)>();
		if (talk == null || talk.IsRenewal || FirstWeekCurrent != FirstWeekStep.Sign || talk.objectionAxis is not { } axis) return lines;
		bool strong = StrongVoice(ExecutiveVoice.Suit);
		bool flag = axis is ContractAxis.Publishing or ContractAxis.CreativeControl;
		if (talk.stage == ContractTalkStage.Objection) {
			lines.Add((ExecutiveVoice.Suit, flag
				? strong
					? $"That's the {FirstWeekAxisName(axis)}, and you can't give half of it. Let them have it and put the paper back. On a first deal the record is worth more to you than the {FirstWeekAxisName(axis)}."
					: $"They want the {FirstWeekAxisName(axis)}. Give it to them and try again."
				: talk.capabilityObjection.HasValue
				? strong
					? "That isn't about the money. It's what a label this size can't do for them yet. Better terms can still carry it: give them a little more of what they asked for and put the paper back."
					: "It's not the money, it's us. Give them a bit more anyway and try again."
				: strong
					? $"That's not a no, it's a price. They named the {FirstWeekAxisName(axis)}. Give them part of it and put the paper back. A promise is a bill that comes later, and holding firm on a first deal is how you lose the act."
					: $"They want more on the {FirstWeekAxisName(axis)}. Give them a bit of it and try again."));
			return lines;
		}
		if (talk.stage != ContractTalkStage.Tabling || talk.roundsPlayed == 0) return lines;
		var offer = CurrentOffer(talk);
		string theirs = FirstWeekAxisValue(axis, talk.ask), ours = FirstWeekAxisValue(axis, offer);
		lines.Add((ExecutiveVoice.Suit, flag
			? strong
				? $"They're stuck on the {FirstWeekAxisName(axis)}: they want {theirs}. Let them have it, leave everything else as it is, and put it back on the table."
				: $"They want {theirs}. Give it to them and try again."
			: strong
				? $"They're stuck on the {FirstWeekAxisName(axis)}: they asked {theirs}, the paper says {ours}. Move that one about halfway toward them, leave everything else as it is, and put it back on the table."
				: $"The {FirstWeekAxisName(axis)}: they asked {theirs}, you wrote {ours}. Meet them halfway and try again."));
		return lines;
	}

	private static string FirstWeekAxisName(ContractAxis axis) => axis switch {
		ContractAxis.Advance => "advance",
		ContractAxis.Royalty => "royalty",
		ContractAxis.Term => "term",
		ContractAxis.Deliverables => "singles owed",
		ContractAxis.Publishing => "publishing",
		_ => "creative control",
	};

	private static string FirstWeekAxisValue(ContractAxis axis, ContractTermSheet sheet) => axis switch {
		ContractAxis.Advance => $"${sheet.Advance:N0}",
		ContractAxis.Royalty => $"{sheet.RoyaltyRate * 100f:0.##}%",
		ContractAxis.Term => Plural.Of(sheet.TermYears, "year"),
		ContractAxis.Deliverables => Plural.Of(sheet.SinglesObligation, "single"),
		ContractAxis.Publishing => sheet.LabelOwnsPublishing ? "the label to keep the publishing" : "to keep their publishing",
		_ => sheet.ArtistCreativeControl ? "the final word on the records" : "the label to have the final word",
	};

	/// <summary>The act turned a song down at booking (THE ACT PUSHES BACK). The Ear teaches it: a no to the song, not to the
	/// record. <paramref name="othersLeft"/>: the sheet has a song they haven't turned down, so set this one aside and tick
	/// that. Otherwise they've refused everything: insist, or (and always, under creative control) go back to the roster
	/// and get them a song they want. Empty when the card isn't on the record line.</summary>
	public List<(ExecutiveVoice Voice, string Text)> FirstWeekPushBack(bool othersLeft, bool canInsist) {
		var lines = new List<(ExecutiveVoice, string)>();
		if (FirstWeekCurrent != FirstWeekStep.Record) return lines;
		bool strong = StrongVoice(ExecutiveVoice.Ear);
		lines.Add((ExecutiveVoice.Ear, othersLeft
			? strong
				? "That's a no to the song, not to the record. A band that doesn't believe a song plays it like strangers, and the tape hears every bar. Set it aside and tick one they'd fight for. Their own songs are the safe bet on a first date."
				: "They don't want that one, and a band that doesn't want a song plays it badly. Set it aside and tick another."
			: canInsist
				? strong
					? "They've turned down everything on the sheet. You can insist: it's your room and your money, but you'll get a take of a song they don't believe in. Or go back to the roster and get them a song they want: let them write one, or teach them a cover."
					: "They don't want any of it. Insist and take what the tape gives you, or go back to the roster and find them a song."
				: strong
					? "They've turned down everything on the sheet, and the contract gives them the last word. Go back to the roster and get them a song they want: let them write one, or teach them a cover."
					: "They don't want any of it, and it's their call. Go back to the roster and find them a song."));
		return lines;
	}

	/// <summary>The Fixer on a date that won't fit today: the shortest booking is longer than what's left before the hard stop.</summary>
	private (ExecutiveVoice, string) FirstWeekTooLate() => (ExecutiveVoice.Fixer, StrongVoice(ExecutiveVoice.Fixer)
		? $"A studio date runs {MinSessionHours} hours at the least, and there aren't {MinSessionHours} left in the day. You could pay for a late night, but a tired band won't give you a take worth keeping. End the day. Book the room first thing tomorrow, while they're fresh."
		: "Too late for a studio today. End the day and book it in the morning.");

	/// <summary>On the hearing line, the voices on tonight's sets, so the player picks one: the Ear on what each room lets
	/// you hear, the Street on whether the act's kind of music sells in this town (the region's genre affinities). Facts
	/// about rooms and markets, never a read of an act nobody has heard; a weak Street says it can't tell.</summary>
	public List<(ExecutiveVoice Voice, string Text)> FirstWeekSetNotes(IReadOnlyList<ScoutingOpportunity> options) {
		var notes = new List<(ExecutiveVoice, string)>();
		if (FirstWeekCurrent != FirstWeekStep.Hear || options == null || options.Count < 2) return notes;
		static string Hears(SceneRoomKind kind) => kind switch {
			SceneRoomKind.Club => "you'll hear the band and the floor, not the words",
			SceneRoomKind.ListeningRoom => "every word comes through, but nobody has to win over a seated room",
			SceneRoomKind.Coffeehouse => "you'll hear the songs, not a band at full strength",
			SceneRoomKind.CommunityHall => "the echo takes the words at the back",
			SceneRoomKind.Roadhouse => "you'll see whether they can hold a bar, and the voice fights the noise",
			SceneRoomKind.TradeEvent => "there's no crowd to win",
			_ => null,
		};
		var rooms = options.Select(o => SceneRoomCatalog.Get(o.RoomId)).Where(r => r != null && Hears(r.Kind) != null).DistinctBy(r => r.Id).Take(3).ToList();
		if (rooms.Count > 0)
			notes.Add((ExecutiveVoice.Ear, "Know the room before you trust what you hear in it. " +
				string.Join(" ", rooms.Select(r => $"{r.Name} is a {SceneRoomCharacter.KindName(r.Kind)}: {Hears(r.Kind)}.")) + " Pick one."));
		var region = ChartManager.Instance?.GetRegionById(DistanceModel.GetCityById(CurrentCityId)?.parentRegionId);
		if (!StrongVoice(ExecutiveVoice.Street) || region?.genrePreferences == null) {
			notes.Add((ExecutiveVoice.Street, "Don't ask me what this town buys. Hear them and find out."));
			return notes;
		}
		var top = region.genrePreferences.OrderByDescending(p => p.affinity).Take(3).Select(p => p.genre).ToHashSet();
		var said = new List<string>();
		foreach (var o in options.Take(3)) {
			var act = ArtistManager.Instance?.GetArtist(o.ArtistId);
			if (act == null) continue;
			var pref = region.genrePreferences.FirstOrDefault(p => p.genre == act.primaryGenre);
			string genre = GenreNameFormatter.Format(act.primaryGenre).ToLowerInvariant();
			string play = (act.members?.Count(m => m.isActive) ?? 0) == 1 ? "plays" : "play";
			if (top.Contains(act.primaryGenre)) said.Add($"{o.ArtistName} {play} {genre}, and this town buys {genre}.");
			else if (pref != null && pref.affinity < 0f) said.Add($"{o.ArtistName} {play} {genre}, a hard sell around here.");
		}
		notes.Add((ExecutiveVoice.Street, said.Count > 0 ? string.Join(" ", said) : "Nothing on tonight's bills is what this town buys most. That's not a reason to stay home."));
		return notes;
	}

	/// <summary>Restore: the card's flags, or a fresh card for an older save.</summary>
	private void RestoreFirstWeek(FirstWeekSaveData saved) =>
		firstWeek = saved == null ? new FirstWeekSaveData() : new FirstWeekSaveData { Off = saved.Off, Done = saved.Done, TermsOpened = saved.TermsOpened };

	private FirstWeekSaveData CaptureFirstWeek() => new() { Off = firstWeek.Off, Done = firstWeek.Done, TermsOpened = firstWeek.TermsOpened };

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────
	internal FirstWeekSaveData CaptureFirstWeekForChecks() => CaptureFirstWeek();
	internal void RestoreFirstWeekForChecks(FirstWeekSaveData saved) => RestoreFirstWeek(saved);
	internal void MarkFirstWeekTermsForChecks() => MarkFirstWeekTerms();
}
