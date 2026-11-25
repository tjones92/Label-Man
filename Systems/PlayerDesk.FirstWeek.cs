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

	/// <summary>Which voice teaches the line, and how to do it. The voice is the one whose job it is (decision 2).</summary>
	public (ExecutiveVoice Voice, string How) FirstWeekHow(FirstWeekStep step) {
		bool declinedOnly = scoutingKnowledge.Approaches?.Count > 0
			&& scoutingKnowledge.Approaches.All(a => a.Status is ScoutingApproachStatus.Declined or ScoutingApproachStatus.Lapsed);
		return step switch {
			FirstWeekStep.Hear => (ExecutiveVoice.Ear,
				"FIND A SET on the A&R tab lists the sets you can still make today. GO & WAIT takes you there; press LISTEN when they start. What you hear goes in the notebook."),
			FirstWeekStep.Ask => (ExecutiveVoice.Street,
				"Most acts on a bill aren't looking for a deal. Open their card in the notebook (TALK TO THEM) and ask if they'd hear one. It's free, and a no is normal. If they're already looking, the card says TERMS instead: go straight to it."),
			FirstWeekStep.Answer when declinedOnly => (ExecutiveVoice.Street,
				"They said no for this season. Most acts on a bill do, and it's the season, not you. Go and hear somebody else and ask them."),
			FirstWeekStep.Answer => (ExecutiveVoice.Fixer,
				"Their side answers within about a week. END THE DAY and read the Morning Paper: it says who called back. Meanwhile, hear somebody else; two asks out beat one."),
			FirstWeekStep.Terms => (ExecutiveVoice.Suit,
				"Open their card and press TERMS. The advance is cash now, paid back out of their royalties; the royalty is their share of each record sold. What they ask for is usually close to fair for an act nobody's heard of, and the standard paper is fine for a first deal."),
			FirstWeekStep.Sign => (ExecutiveVoice.Suit,
				"TABLE OFFER puts it to them. If they push back, move one thing at a time: a little more advance, or a little less term. Don't chase the last few dollars."),
			_ => (ExecutiveVoice.Ear,
				"On the ROSTER tab, TAKE INTO STUDIO: pick the songs from their set, choose a studio and book the hours. That's the record started."),
		};
	}

	/// <summary>A second voice on the line, when there's something it should know: what a read means once you've heard a set,
	/// and on a Sawyer run, the singer already on the roster. Null when there's nothing to add.</summary>
	public (ExecutiveVoice Voice, string Aside)? FirstWeekAside(FirstWeekStep step) {
		bool connieWaiting = Connie != null && !FirstWeekDone(FirstWeekStep.Record);
		switch (step) {
			case FirstWeekStep.Ask:
				return (ExecutiveVoice.Ear, "About what you heard: a read is one night in one room. A roadhouse eats a voice and a coffeehouse flatters one, so don't judge a singer by a bad room alone.");
			case FirstWeekStep.Answer when connieWaiting:
				return (ExecutiveVoice.Street, $"While you wait: {ConnieStageName} is on your roster already, and Dee-Dee wants her record out this year.");
			case FirstWeekStep.Record when connieWaiting && !OwnActSigned:
				return (ExecutiveVoice.Street, $"Nobody of your own yet? {ConnieStageName} is on the roster, and Dee-Dee's girl needs songs. Her record counts.");
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
