using System.Linq;

// ============================================================================================
// THE FIRST WEEK (SimTools/OnboardingTutorialDirective.md §3.2, T3).
//
// A short to-do card in the Label Man's own hand that walks a new player through the first signing and a first record,
// riding the real verbs: hear a set, ask if they'd hear an offer, sleep on it, put a deal on the table, sign them, set up
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
			FirstWeekStep.Answer => scoutingKnowledge.Approaches?.Any(a => a.Status == ScoutingApproachStatus.Ready) == true || FirstWeekDone(FirstWeekStep.Terms),
			FirstWeekStep.Ask => scoutingKnowledge.Approaches?.Count > 0 || FirstWeekDone(FirstWeekStep.Answer),
			FirstWeekStep.Hear => scoutingKnowledge.Dossiers.Any(d => HasScoutingEvidence(d.ArtistId)) || FirstWeekDone(FirstWeekStep.Ask),
			_ => false,
		};
	}

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
		FirstWeekStep.Answer => "Sleep on it",
		FirstWeekStep.Terms => "Put a deal on the table",
		FirstWeekStep.Sign => "Sign them",
		_ => "Set up a first record",
	};

	/// <summary>Which voice teaches the line, and how. The voice is the one whose job it is (decision 2), and it talks about
	/// the work, never the buttons: the glow and SHOW ME point at those. It speaks at any instinct (SigningExperienceAudit
	/// §5 decision 2), but a weak voice (under 3) teaches in its own blunt register and claims no read it can't make.</summary>
	public (ExecutiveVoice Voice, string How) FirstWeekHow(FirstWeekStep step) {
		bool declinedOnly = scoutingKnowledge.Approaches?.Count > 0
			&& scoutingKnowledge.Approaches.All(a => a.Status is ScoutingApproachStatus.Declined or ScoutingApproachStatus.Lapsed);
		bool anotherLook = scoutingKnowledge.Approaches?.Any(a => a.Status == ScoutingApproachStatus.WantsAnotherLook) == true
			&& scoutingKnowledge.Approaches.All(a => a.Status is not (ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready));
		return step switch {
			FirstWeekStep.Hear => (ExecutiveVoice.Ear, StrongVoice(ExecutiveVoice.Ear)
				? "Handbills lie and so do managers. A set doesn't. Pick a room tonight, stand near the back, and I'll tell you what I heard."
				: "Don't ask me who's good. I can tell you who was loud. Go hear a set anyway: what we hear goes in the notebook, and that's more than a handbill gives you."),
			FirstWeekStep.Ask => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "Half the acts on any bill aren't shopping. Day jobs, steady girls, mothers who hate the business. So ask if they'd hear an offer. It's free, and no is the usual answer. If somebody's already shopping, skip the asking and sit down on terms."
				: "I couldn't tell you who's shopping in this town. Nobody can till you ask. It's free, and most of them say no."),
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
			case FirstWeekStep.Record when connieWaiting && !OwnActSigned:
				return (ExecutiveVoice.Street, $"Nobody of your own yet? Then cut {ConnieStageName}. Dee-Dee's girl is on the roster and she needs songs. Her record counts.");
			default:
				return null;
		}
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
