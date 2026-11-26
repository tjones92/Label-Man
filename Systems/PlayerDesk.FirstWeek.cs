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
public enum FirstWeekPlace { FindASet, TheTalk, TheTable, Roster }

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

	/// <summary>Every act asked so far said no (or let its yes lapse): the answer line's work is hearing somebody else and
	/// asking them, not waiting.</summary>
	public bool FirstWeekNeedsAnotherAct => scoutingKnowledge.Approaches?.Count > 0
		&& scoutingKnowledge.Approaches.All(a => a.Status is ScoutingApproachStatus.Declined or ScoutingApproachStatus.Lapsed);

	/// <summary>The card is still on finding an act: hearing, asking, or hearing somebody else after a no.</summary>
	public bool FirstWeekLooking => FirstWeekCurrent is FirstWeekStep.Hear or FirstWeekStep.Ask
		|| FirstWeekCurrent == FirstWeekStep.Answer && FirstWeekNeedsAnotherAct;

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
		bool anotherLook = scoutingKnowledge.Approaches?.Any(a => a.Status == ScoutingApproachStatus.WantsAnotherLook) == true
			&& scoutingKnowledge.Approaches.All(a => a.Status is not (ScoutingApproachStatus.Considering or ScoutingApproachStatus.Ready or ScoutingApproachStatus.Agreed));
		var pending = FirstPendingApproachStep?.Kind;
		return step switch {
			FirstWeekStep.Answer when pending == ScoutingApproachStepKind.Parents => (ExecutiveVoice.Street, StrongVoice(ExecutiveVoice.Street)
				? "That's a yes, but somebody in that act is under 21, and in this business the mother signs. Go to Sunday dinner. Eat what's put in front of you and tell her the truth about the money."
				: "They said yes, but one of them's under 21, so a parent signs. Go and meet the family on Sunday."),
			FirstWeekStep.Answer when pending == ScoutingApproachStepKind.Manager => (ExecutiveVoice.Suit, StrongVoice(ExecutiveVoice.Suit)
				? "A yes with a manager attached means the paper goes through him. Sit down with him on the day he named. Listen more than you talk: whatever he asks for, he'll ask for again at the table."
				: "They've got a manager, so it goes through him. Go to the sit-down he named."),
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
				Line(step); break;
			// Opening the table ticks "put a deal on the table", so the sheet is where both lines are taught.
			case FirstWeekPlace.TheTable when step is FirstWeekStep.Terms or FirstWeekStep.Sign:
				Line(FirstWeekStep.Terms); Line(FirstWeekStep.Sign); break;
			case FirstWeekPlace.Roster when step == FirstWeekStep.Record:
				Line(step); Aside(step); break;
		}
		return lines;
	}

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
