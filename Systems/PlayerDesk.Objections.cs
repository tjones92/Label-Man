using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What the one who isn't sure says, read from who they are (SimTools/SigningRealismBreakdown.md step 9).</summary>
public enum CourtshipObjection {
	None,
	/// <summary>They write the songs, and won't sign them over to somebody they've just met.</summary>
	Songs,
	/// <summary>Children or a spouse at home: they won't live on a bus for a maybe.</summary>
	Road,
	/// <summary>They don't need the money.</summary>
	Money,
	/// <summary>Twenty-five or older with a day job worth keeping: they won't quit it for a maybe.</summary>
	Job,
	/// <summary>Older saves only: they don't know you. New holdouts say Cheated instead.</summary>
	Trust,
	/// <summary>A comfortable act's paid standing engagement.</summary>
	Engagement,
	/// <summary>A comfortable act making its living in the rooms here.</summary>
	Local,
	/// <summary>Under 21: their folks want them finishing school or learning a trade.</summary>
	School,
	/// <summary>Barely ambitious at all: they're in it for the fun, and a record is somebody else's idea. No true answer.</summary>
	Fun,
	/// <summary>Nothing else in the way: labels cheat, and everybody knows somebody who never saw a dime.</summary>
	Cheated
}

/// <summary>How you answer an objection in The Talk.</summary>
public enum ObjectionAnswer { Grounded, Wait, Without }

// ============================================================================================
// COURTSHIP OBJECTIONS (SigningRealismBreakdown.md step 9; SigningExperienceAudit.md §3.6).
//
// Two places in the signing loop where someone isn't sure now raise one grounded objection, and you answer it:
//  - The holdout. At the band's next set after the yes, the member holding out comes over and says the one thing in the
//    way, read from that person in order: they write the songs; they've a family at home; they don't need the money;
//    they're under 21 and their folks want school or a trade; they're 25 or older with a day job worth keeping; they're
//    only in it for the fun; or, failing all of those, labels cheat people.
//  - A comfortable act that trusts you enough to hear you out. Its spokesperson puts its own reason as a question: the
//    standing engagement, the member who doesn't need the money, the jobs and families, the living in the rooms here.
//
// Each objection has one answer that holds only when its fact is true: you heard their own songs, your label records in
// their town, you have a record out, they're the band's strongest player, you've sat at the family's table, an act on
// your roster has been paid royalties. A line whose fact isn't true is shown, greyed, with what would make it true. The
// member only in it for the fun has no true answer. The other answers are always open: tell a holdout to
// take their time, and they settle it at the next set or rehearsal you're at; or, in a band of four or more where the
// holdout isn't the leader, the singer or the writer, sign the rest without them, and that member leaves the band.
// No draws: the same state gives the same objection and the same answer. Player-only: only the player's desk raises or
// answers one, so AI-only worlds are untouched.
// ============================================================================================
public partial class PlayerDesk {
	/// <summary>The smallest band that can sign without its holdout: the rest are still a band of three.</summary>
	public const int SignWithoutMinMembers = 4;
	/// <summary>Nights heard that let you tell a holdout who doesn't know you that you've been coming.</summary>
	public const int TrustHeardNights = 2;
	/// <summary>Share of the heard set that's their own for you to say you came for their songs.</summary>
	public const float OwnSongsHeard = .5f;
	/// <summary>A day job is the objection only for a holdout this old or older: a settled working life, not a stopgap.</summary>
	public const int SettledJobAge = 25;
	/// <summary>A holdout below this ambition is only in it for the fun (holdouts are all below ActDisposition.HoldoutAmbition).</summary>
	public const float FunBelowAmbition = .2f;

	private static Musician ActiveMember(SimulatedArtist act, string personId) => personId == null ? null :
		act?.members?.FirstOrDefault(m => m != null && m.personId == personId && m.isActive && m.lifeState == MemberLifeState.Active);

	/// <summary>The holdout's person: by id, or on older saves by the name the step carries.</summary>
	private static Musician HoldoutMember(SimulatedArtist act, ScoutingApproachStep s) => ActiveMember(act, s.PersonId) ??
		(s.PersonId != null ? null : act?.members?.FirstOrDefault(m => m != null && m.isActive && m.lifeState == MemberLifeState.Active &&
			(m.firstName == s.Who || m.FullName.Trim() == s.Who)));

	/// <summary>The holdout's one objection, read from that person: the first of these that's true.</summary>
	public static CourtshipObjection HoldoutObjection(SimulatedArtist act, Musician m, int day) {
		if (m == null) return CourtshipObjection.Cheated;
		int age = m.birthYear > 0 ? m.GetAge(LocalSceneRoomService.Date(day).year) : 25;
		if (m.isPrimaryWriter) return CourtshipObjection.Songs;
		if (m.hasChildren || m.partner?.state == PartnerState.Married) return CourtshipObjection.Road;
		if (MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm) return CourtshipObjection.Money;
		if (age < ActDisposition.MinorAge) return CourtshipObjection.School;
		if (age >= SettledJobAge && !LocalSceneRoomService.FullTime(act, day)) return CourtshipObjection.Job;
		if (m.ambition < FunBelowAmbition) return CourtshipObjection.Fun;
		return CourtshipObjection.Cheated;
	}

	/// <summary>A comfortable act's objection: its reason for being comfortable.</summary>
	public static CourtshipObjection HearingObjection(DispositionRead r) => r.Reason switch {
		DispositionReason.Engagement => CourtshipObjection.Engagement,
		DispositionReason.NoNeedForMoney => CourtshipObjection.Money,
		DispositionReason.Settled or DispositionReason.WeekendAct => CourtshipObjection.Road,
		_ => CourtshipObjection.Local
	};

	/// <summary>What the holdout says, in their own voice.</summary>
	private static string HoldoutLine(CourtshipObjection o) => o switch {
		CourtshipObjection.Songs => "They're my songs. I'm not signing them over to somebody I've just met.",
		CourtshipObjection.Road => "I've got a family at home. I'm not living on a bus for a maybe.",
		CourtshipObjection.Money => "I don't need the money, and I don't need the trouble.",
		CourtshipObjection.Job => "I've got a good job. I'm not quitting it for a maybe.",
		CourtshipObjection.School => "My folks want me finishing school, or learning a trade. Not playing records.",
		CourtshipObjection.Fun => "I'm in this for the fun. A record's somebody else's idea.",
		CourtshipObjection.Trust => "I don't know you, and I don't know your label.",
		_ => "Labels cheat you. Everybody knows somebody who signed and never saw a dime."
	};

	/// <summary>What a comfortable act asks, for the band.</summary>
	private static string HearingQuestion(CourtshipObjection o, DispositionRead r) => o switch {
		CourtshipObjection.Engagement => $"What do we get for walking out on {r.EngagementRoom ?? "the room"}?",
		CourtshipObjection.Money => $"{r.WealthyMember ?? "One of us"} doesn't need the money. So what's in it for us?",
		CourtshipObjection.Road => "We've got jobs and families. Who's going on the road?",
		_ => "A record won't pay the rent. Why give up the rooms?"
	};

	/// <summary>The Street's few words on what the holdout will say, for its deep read.</summary>
	internal static string HoldoutHint(CourtshipObjection o) => o switch {
		CourtshipObjection.Songs => "They write the songs.",
		CourtshipObjection.Road => "There's a family at home.",
		CourtshipObjection.Money => "They don't need the money.",
		CourtshipObjection.Job => "They've a job worth keeping.",
		CourtshipObjection.School => "Their folks want them in school.",
		CourtshipObjection.Fun => "They're only in it for the fun.",
		CourtshipObjection.Trust => "They don't know you.",
		_ => "They think labels cheat."
	};

	internal void RaiseHoldoutObjection(SimulatedArtist act, ScoutingApproachStep s, int day) {
		s.Raised = true; s.RaisedDay = day;
		s.Objection = HoldoutObjection(act, HoldoutMember(act, s), day);
		s.ObjectionLine = HoldoutLine(s.Objection);
	}

	internal ScoutingApproachStep HearingObjectionStep(DispositionRead read, SimulatedArtist act, string who, int today) {
		var o = HearingObjection(read);
		return new ScoutingApproachStep { Kind = ScoutingApproachStepKind.Objection, Who = who, Day = today, Raised = true, RaisedDay = today,
			Objection = o, ObjectionLine = HearingQuestion(o, read) };
	}

	/// <summary>An objection waiting on your answer: the step, or null.</summary>
	public ScoutingApproachStep PendingObjection(SimulatedArtist act) => NextApproachStep(act) is { } s &&
		(s.Kind == ScoutingApproachStepKind.Objection || s.Kind == ScoutingApproachStepKind.Holdout && s.Raised && s.WaitFrom == null) ? s : null;

	/// <summary>The answer that meets this objection, who'd tell you to say it, and why it can't be said when its fact isn't
	/// true (null when it can).</summary>
	public (string Line, ExecutiveVoice Voice, string Why) GroundedAnswer(ScoutingApproachStep s, SimulatedArtist act) {
		bool holdout = s.Kind == ScoutingApproachStepKind.Holdout;
		string town = LocalScenePersistenceService.SceneIdFor(act?.geography?.basePlaceId);
		string townName = DistanceModel.GetCityById(town)?.name ?? "town";
		bool ownSongs = HeardOriginals(act.artistId) >= OwnSongsHeard, released = Label?.totalReleases > 0;
		const string noSongs = "You haven't heard enough of their own songs to say so. Hear a set of theirs.";
		const string noRecord = "You've no record out to point to yet.";
		switch (s.Objection) {
			case CourtshipObjection.Songs:
				return ("I came for your songs. We'll cut yours, not ours.", ExecutiveVoice.Ear, ownSongs ? null : noSongs);
			case CourtshipObjection.Money when holdout: {
				var m = HoldoutMember(act, s);
				bool best = m != null && act.members.Where(x => x != null && x.isActive && x.lifeState == MemberLifeState.Active).All(x => x.technicalSkill <= m.technicalSkill);
				return ("You're the best player up there. That belongs on a record.", ExecutiveVoice.Ear,
					best ? null : "They're not the strongest player in the band, and they'd know it.");
			}
			case CourtshipObjection.Money:
				return ("Your own songs, on a record with your name on it.", ExecutiveVoice.Ear, ownSongs ? null : noSongs);
			case CourtshipObjection.Road:
				return ($"Nobody goes on the road till a record sells. You'd record here, in {townName}.", ExecutiveVoice.Suit,
					town != null && Label?.homeCityId == town ? null : $"Your label's in {HomeTownName ?? "another town"}. They'd be travelling to record.");
			case CourtshipObjection.Job:
				return ("Keep the job. We record on weekends until one sells.", ExecutiveVoice.Suit, null);
			case CourtshipObjection.Engagement:
				return ("Keep the room. A record on the jukebox fills it.", ExecutiveVoice.Street, released ? null : noRecord);
			case CourtshipObjection.Local:
				return ("A record gets you the better rooms. Ours get played.", ExecutiveVoice.Street, released ? null : noRecord);
			case CourtshipObjection.School: {
				bool met = Agreement(act)?.Steps.Any(x => x.Kind == ScoutingApproachStepKind.Parents && x.Done) == true;
				return ("Sessions are Saturdays. School comes first, and I told your folks so at their table.", ExecutiveVoice.Suit,
					met ? null : "You haven't sat down with their family yet.");
			}
			case CourtshipObjection.Fun:
				return ("You'd have more fun on a record.", ExecutiveVoice.Street, "There's no answer to that one. Give them time, or go on without them.");
			case CourtshipObjection.Cheated: {
				var paid = Label?.roster?.FirstOrDefault(a => a != null && a.HasRecoupedCurrentContract());
				return ($"Ask {paid?.stageName ?? "our acts"}. We've paid them royalties, and the paper shows every deduction.", ExecutiveVoice.Suit,
					paid != null ? null : "You haven't paid an act a royalty yet. Nobody can vouch for you.");
			}
			default: {
				int nights = HeardNights(act.artistId);
				return ($"I've been in to hear you {(nights == 2 ? "twice" : nights + " times")}. I'm not going anywhere.", ExecutiveVoice.Street,
					nights >= TrustHeardNights ? null : "You've only heard them once. Come to another set first.");
			}
		}
	}

	/// <summary>Whether the band can sign without its holdout: four or more, and the holdout isn't who the band is built on.
	/// Null when it can; otherwise why not.</summary>
	public string SignWithoutBlocked(SimulatedArtist act, ScoutingApproachStep s) {
		if (s?.Kind != ScoutingApproachStepKind.Holdout) return "It's the whole act that isn't sure.";
		var m = HoldoutMember(act, s);
		if (m == null) return "They're not here to say it to.";
		int members = act.members.Count(x => x != null && x.isActive && x.lifeState == MemberLifeState.Active);
		if (members < SignWithoutMinMembers) return "They're a band of three. Without one of them, there's no band.";
		if (m.isBandLeader) return $"It's {s.Who}'s band.";
		if (m.isLeadVocalist) return $"{s.Who} sings. There's no record without the singer.";
		if (m.isPrimaryWriter) return $"{s.Who} writes the songs.";
		return null;
	}

	/// <summary>Answer an objection put to you. In the act's town; free, because it's a line in the conversation.</summary>
	public bool AnswerObjection(string artistId, ObjectionAnswer answer, out string message) {
		var act = ArtistManager.Instance?.GetArtist(artistId);
		var s = PendingObjection(act);
		if (s == null) { message = "Nobody's waiting on an answer."; return false; }
		string town = LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId);
		if (town != null && town != LocalScenePersistenceService.SceneIdFor(CurrentCityId)) {
			message = $"You'd have to be in {DistanceModel.GetCityById(town)?.name ?? "their town"}."; return false;
		}
		int today = SceneDay;
		bool holdout = s.Kind == ScoutingApproachStepKind.Holdout;
		switch (answer) {
			case ObjectionAnswer.Grounded: {
				var g = GroundedAnswer(s, act);
				if (g.Why != null) { message = g.Why; return false; }
				s.Done = true; s.DoneDay = today;
				s.Said = $"You told {s.Who}: \"{g.Line}\" " + (holdout ? $"{s.Who}: \"All right. If you mean that, I'm in.\"" : $"{s.Who}: \"All right. Let's hear what you're offering.\"");
				message = s.Said + " " + AfterStep(act);
				break;
			}
			case ObjectionAnswer.Wait when holdout:
				s.WaitFrom = today;
				message = $"You told {s.Who} to take their time. \"We'll see. Come to the next one.\" " + StepAsk(s, act);
				break;
			case ObjectionAnswer.Wait:
				message = $"{s.Who}: \"Come back when you have. We'll still be here.\" Their yes holds until {Headline(AgreementHoldsUntil(Agreement(act)))}.";
				Note(message);
				return true;
			case ObjectionAnswer.Without: {
				string why = SignWithoutBlocked(act, s);
				if (why != null) { message = why; return false; }
				var m = HoldoutMember(act, s);
				var kind = s.Objection switch { CourtshipObjection.Road => DepartureKind.Family, CourtshipObjection.Job => DepartureKind.DayJob, CourtshipObjection.School => DepartureKind.School, _ => DepartureKind.WalkedOut };
				BandLifeService.RemoveFromAct(act, m, kind, "wouldn't sign", LocalSceneRoomService.Date(today).year);
				s.Done = true; s.DoneDay = today;
				s.Said = $"{s.Who} shook your hand and wished them luck, and left the band. The rest of {act.stageName} will sign.";
				message = s.Said + " " + AfterStep(act);
				break;
			}
			default: message = "Not now."; return false;
		}
		Note(message);
		Changed?.Invoke();
		return true;
	}
}
