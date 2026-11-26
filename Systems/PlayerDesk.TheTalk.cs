using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// THE TALK (SimTools/SigningExperienceAudit.md §3.3; SigningRealismBreakdown.md step 5).
//
// Talking to an act, staged as a conversation instead of a menu of appointments. Their spokesperson, a real member,
// opens with a line chosen by where things stand: signed elsewhere, a yes with a step still to take, a no that still
// holds, a request you haven't met, shopping themselves, or nobody's asked yet (then their manner follows their
// disposition, and an act that knows you says what it remembers). The Street reads what they want and what's in the
// way; the Fixer speaks only when a rival's interest is real and has reached you. What you can say is a list of lines,
// each one an existing verb (the ask, terms, the family or the manager, their history, a rehearsal, a demo, a return
// meeting, their number, their next date, a handshake), with its cost underneath. The answer comes back into the
// transcript.
//
// Presentation only: every verb is the desk's own, with its own rules and costs. Nothing here is saved; the transcript
// is the evening's and is dropped when you end the conversation.
// ============================================================================================
public enum TalkVerb { Ask, Terms, Step, History, Rehearsal, Demo, Meeting, Number, NextSet, Handshake, Watch, Unwatch,
	/// <summary>Step 9: answer an objection with its grounded line, tell a holdout to take their time, or sign the rest without them.</summary>
	Answer, TakeYourTime, SignWithout }

/// <summary>One thing you can say in The Talk, mapped onto an existing verb.</summary>
public sealed class TalkOption {
	public TalkVerb Verb;
	/// <summary>What you say.</summary>
	public string Line;
	/// <summary>What it costs or does, shown small underneath.</summary>
	public string Cost;
	/// <summary>Why it can't be said now, when it can't.</summary>
	public string Why;
	public ExecutiveVoice Voice;
	public bool Enabled = true;
	/// <summary>Step: which one.</summary>
	public ScoutingApproachStepKind Step;
}

public sealed class TalkScene {
	public string ArtistId, ArtistName, Speaker;
	/// <summary>Narration: who you're talking to, and how (after the set, on the phone).</summary>
	public string Setting;
	/// <summary>Their opening line, the voices, then what's been said so far tonight.</summary>
	public List<CallLine> Lines = new();
	public List<TalkOption> Options = new();
}

public partial class PlayerDesk {
	/// <summary>Tonight's conversations, by act. Not saved: a transcript is the evening's.</summary>
	private readonly Dictionary<string, List<CallLine>> talkTranscripts = new(StringComparer.Ordinal);

	/// <summary>Drops the evening's transcript with this act (the player ended the conversation).</summary>
	public void EndTalk(string artistId) { if (artistId != null) talkTranscripts.Remove(artistId); }

	/// <summary>The conversation with this act as it stands, or null when there's nobody in the notebook to talk to.
	/// A pure read: building it twice gives the same scene, and nothing in the world or the save moves.</summary>
	public TalkScene BuildTalkScene(string artistId) {
		var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
		var act = ArtistManager.Instance?.GetArtist(artistId);
		if (dossier == null || act == null || Label == null) return null;
		int today = SceneDay;
		string who = SpeakerFor(act);
		var scene = new TalkScene { ArtistId = artistId, ArtistName = dossier.ArtistName ?? act.stageName, Speaker = who };
		var prospect = Slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
		bool mine = act.labelId == Label.labelId, elsewhere = !string.IsNullOrEmpty(act.labelId) && !mine;
		bool free = string.IsNullOrEmpty(act.labelId) && act.lifecycleStatus == ArtistLifecycleStatus.Active;

		// ── How you're talking ──
		var number = ActNumber(artistId);
		bool inPerson = InvitationSpeaker(artistId).person != null || PreviewScoutingBusinessMeeting(artistId) != null;
		bool byPhone = !inPerson && number != null && PhoneBlocked(artistId) == null;
		string with = who == scene.ArtistName ? who : $"{who}, for {scene.ArtistName}";
		scene.Setting = inPerson ? $"You catch {with}{(with == who ? "" : ",")} after the set."
			: byPhone ? $"You ring {number.PersonName} ({number.Source})."
			: $"You look up {with}. Nobody's in front of you tonight, so what you can ask is what doesn't need them here.";
		if (number != null && !byPhone) scene.Setting += $" You have {number.PersonName}'s number.";

		// ── Their opening line ──
		var agreed = Agreement(act);
		var step = NextApproachStep(act);
		var read = free ? ReadDisposition(act) : null;
		var regard = ReadRegard(act);
		string opening;
		if (mine) opening = "We're with you now. What do you need?";
		else if (elsewhere) opening = $"We're with {ChartManager.Instance?.GetLabelName(act.labelId) ?? "a label"}. We can't talk about a record with you, but ask what you like.";
		else if (agreed == null && FirstCall(act) is { } called) opening = $"{called.LetGoBy ?? "Our label"} let us go. We wanted you to hear it from us first.";
		else if (agreed != null) opening = step == null ? "We meant what we said. We're ready when you are."
			: step.Kind == ScoutingApproachStepKind.Objection ? $"We meant what we said. But before anything's signed: {step.ObjectionLine}"
			: step.Kind == ScoutingApproachStepKind.Holdout && step.Raised && step.WaitFrom == null ? $"We meant what we said. But {step.Who} has something to say."
			: step.Kind == ScoutingApproachStepKind.Holdout && step.Raised ? $"We meant what we said. {step.Who} said they'd see you at the next one."
			: step.Kind == ScoutingApproachStepKind.Holdout ? $"We meant what we said. {step.Who} still wants to see you at a set first."
			: "We meant what we said. " + StepAsk(step, act);
		else if (StillDeclining(act)) opening = "We told you where we stand. Nothing's changed for us since.";
		else if (WaitingToBeHeardAgain(act)) opening = regard.LetDown ? RegardOpener(regard, today) + " Come and hear us again before we talk."
			: "We said we'd want to see you again before we talk about a record.";
		else if (prospect != null && SceneDealAvailable(prospect)) opening = "We're looking for a deal, if that's why you're here.";
		else {
			string manner = read?.Kind switch {
				DealDisposition.Hungry => "Are you from a record company? We've been hoping somebody would come out.",
				DealDisposition.Careful => "You're with a label? Which one?",
				_ => "Thanks for staying for the set."
			};
			opening = regard.Level >= RegardLevel.KnowYourFace && RegardOpener(regard, today) is { } opener ? opener + " " + manner : manner;
		}
		// The band says so when a relative manages them (§10 decision 2).
		if (free && ManagerIdentity.Read(act, today) is { Kind: ManagerKind.Family } family)
			opening += $" {ManagerIdentity.Introduce(family)}, handles our business.";
		scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, speaker = who, text = opening });
		// The holdout says it themselves (step 9).
		var pendingObjection = free ? PendingObjection(act) : null;
		if (pendingObjection?.Kind == ScoutingApproachStepKind.Holdout)
			scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.Pushback, speaker = HoldoutMember(act, pendingObjection)?.FullName.Trim() ?? pendingObjection.Who,
				text = pendingObjection.ObjectionLine });

		// ── The voices: the Street on what they want, the Fixer on rivals ──
		void Read(ExecutiveVoice voice, string hint, string clear, string deep) {
			int score = InstinctScore(InstinctProfile, voice);
			string text = score >= 5 ? deep ?? clear ?? hint : score >= 4 ? clear ?? hint : score >= 3 ? hint : null;
			if (!string.IsNullOrEmpty(text)) scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = voice, text = text });
		}
		if (free && agreed == null) {
			if (prospect != null && SceneDealAvailable(prospect))
				Read(ExecutiveVoice.Street, "They're shopping themselves around. Other labels will hear it too.", null, null);
			else if (read != null) {
				string hint = read.Kind switch {
					DealDisposition.Hungry => "They want a record. You can see it on them.",
					DealDisposition.Careful => "They'll want to know who you are before they talk money.",
					_ => "They're happy as they are. Don't expect a yes tonight."
				};
				string why = StreetReason(read);
				string clear = hint + " " + why;
				var frictions = new List<string>();
				if (read.Kind != DealDisposition.Comfortable) {
					if (read.Minors.Count > 0) frictions.Add($"{Names(read.Minors)} {(read.Minors.Count == 1 ? "is" : "are")} under 21, so a parent signs.");
					if (read.Holdout != null) frictions.Add($"{read.Holdout} is the one who isn't sure. " +
						HoldoutHint(HoldoutObjection(act, ActiveMember(act, read.HoldoutId), today)));
					if (read.DemandingManager && ManagerIdentity.Read(act, today) is { } m) frictions.Add($"{m.Name} does their business, and he'll want a sit-down first.");
				}
				Read(ExecutiveVoice.Street, hint, clear, frictions.Count == 0 ? clear : clear + " " + string.Join(" ", frictions));
			}
		}
		// Signed as a clock (step 6): the Street reads their paper, and at 5 what the label will likely do when it's up.
		if (elsewhere && ReadSignedElsewhere(act) is { } paper) {
			string clear = paper.Clock == null ? paper.Paper : paper.Paper + " " + paper.Clock;
			Read(ExecutiveVoice.Street, paper.Paper, clear, clear + " " + paper.Likely);
		}
		FixerRivalRead(artistId, Read);

		// ── What's been said tonight ──
		if (talkTranscripts.TryGetValue(artistId, out var said)) scene.Lines.AddRange(said);

		// ── What you can say ──
		var o = scene.Options;
		if (free && pendingObjection != null) {
			// Step 9: one objection, and the ways to answer it. Free: it's a line in the conversation.
			string town = LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId);
			string away = town == null || town == LocalScenePersistenceService.SceneIdFor(CurrentCityId) ? null
				: $"You'd have to be in {DistanceModel.GetCityById(town)?.name ?? "their town"}.";
			var g = GroundedAnswer(pendingObjection, act);
			o.Add(new TalkOption { Verb = TalkVerb.Answer, Voice = g.Voice, Line = g.Line, Enabled = away == null && g.Why == null, Why = away ?? g.Why,
				Cost = "Free. If it's true, it answers them." });
			bool holdout = pendingObjection.Kind == ScoutingApproachStepKind.Holdout;
			o.Add(new TalkOption { Verb = TalkVerb.TakeYourTime, Line = holdout ? "Take your time. I'll be at the next one." : "I'll come back when I've more to show you.",
				Enabled = away == null, Why = away,
				Cost = holdout ? "Free. Be at their next set or a rehearsal, and they'll settle it there." : $"Free. Their yes holds until {Headline(AgreementHoldsUntil(agreed))}." });
			if (holdout && act.members.Count(x => x != null && x.isActive && x.lifeState == MemberLifeState.Active) >= SignWithoutMinMembers) {
				string why = away ?? SignWithoutBlocked(act, pendingObjection);
				o.Add(new TalkOption { Verb = TalkVerb.SignWithout, Voice = ExecutiveVoice.Fixer, Line = "Then the rest of you can sign without you.",
					Enabled = why == null, Why = why, Cost = $"{pendingObjection.Who} leaves the band. The rest sign." });
			}
		} else if (free) {
			if (step != null && step.Kind is ScoutingApproachStepKind.Parents or ScoutingApproachStepKind.Manager) {
				string town = LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId);
				bool here = town == null || town == LocalScenePersistenceService.SceneIdFor(CurrentCityId);
				bool due = step.Day is not { } d || today >= d;
				int hours = step.Kind == ScoutingApproachStepKind.Parents ? FamilyVisitHours : ManagerSitDownHours;
				o.Add(new TalkOption { Verb = TalkVerb.Step, Step = step.Kind,
					Line = step.Kind == ScoutingApproachStepKind.Parents ? "I'll come to Sunday dinner and meet the family." : $"I'll sit down with {step.Who}.",
					Cost = $"{hours}h, {(step.Kind == ScoutingApproachStepKind.Parents ? "at the family's" : "at " + (step.Place ?? "his office"))}.",
					Enabled = due && here,
					Why = !due ? $"Not before {LocalSceneRoomService.Date(step.Day.Value).ToHeadlineString()}." : $"You'd have to be in {DistanceModel.GetCityById(town)?.name ?? "their town"}." });
			} else if (prospect != null && CanTalkTerms(prospect))
				o.Add(new TalkOption { Verb = TalkVerb.Terms, Line = "Let's sit down on terms.", Cost = "Opens the contract: their ask, your offer, what it costs the label." });
			else if (agreed == null && CanAskDirectApproach(act))
				o.Add(new TalkOption { Verb = TalkVerb.Ask, Line = "Would you hear an offer from us?",
					Cost = LatestApproach(artistId) is null ? "Free. They'll tell you where they stand, and why." : "Free. They'll tell you again where they stand; if something's changed, so will the answer." });
		}
		if (PreviewScoutingBusinessMeeting(artistId) is { } meeting)
			o.Add(new TalkOption { Verb = TalkVerb.History, Voice = ExecutiveVoice.Suit, Line = "Who've you dealt with before?",
				Cost = $"{ActHistoryMinutes} min. They'll tell you what they remember, which may not be all of it.", Enabled = meeting.Available, Why = meeting.UnavailableReason });
		if (free) foreach (var (activity, verb, line, voice) in new[] {
			(ScoutingAppointmentActivity.Rehearsal, TalkVerb.Rehearsal, "Can I hear you rehearse, somewhere quieter?", ExecutiveVoice.Ear),
			(ScoutingAppointmentActivity.Demo, TalkVerb.Demo, "Put a song on tape for me, at my place.", ExecutiveVoice.Ear),
			(ScoutingAppointmentActivity.Conversation, TalkVerb.Meeting, "Can we sit down properly this week, just us?", ExecutiveVoice.None) }) {
			var quote = PreviewScoutingInvitation(artistId, activity);
			string asking = number != null && !inPerson ? $"a {PhoneCallMinutes}-minute call" : $"{ScoutingInvitationMinutes} min to ask";
			o.Add(new TalkOption { Verb = verb, Voice = voice, Line = line, Enabled = quote != null,
				Cost = quote == null ? null : $"{LocalSceneRoomService.Date(quote.Day).ToHeadlineString()}, {quote.StartMinute / 60:00}:{quote.StartMinute % 60:00} at " +
					$"{(activity == ScoutingAppointmentActivity.Demo ? "your home studio" : SceneRoomCatalog.Get(quote.RoomId)?.Name)} · {(quote.EndMinute - quote.StartMinute) / 60}h · " +
					$"{(quote.Fee > 0 ? $"${quote.Fee:0} when you go" : "no fee")} · {asking}. They may say no.",
				Why = number != null ? PhoneBlocked(artistId) ?? "No free slot to propose right now." : "Only after a set you've heard today, or by phone with their number." });
		}
		if (number == null) {
			string why = ActNumberAfterSetBlocked(artistId);
			o.Add(new TalkOption { Verb = TalkVerb.Number, Voice = ExecutiveVoice.Street, Line = "How do I reach you?", Enabled = why == null, Why = why,
				Cost = $"{AskActNumberMinutes} min. With a number you can ring them from anywhere." });
		}
		if (elsewhere) {
			if (IsWatchingSigned(artistId))
				o.Add(new TalkOption { Verb = TalkVerb.Unwatch, Voice = ExecutiveVoice.Street, Line = "Good luck with it. I'll stop keeping tabs.",
					Cost = "Free. The Street stops telling you about their deal." });
			else {
				string why = WatchSignedBlocked(act);
				o.Add(new TalkOption { Verb = TalkVerb.Watch, Voice = ExecutiveVoice.Street, Line = "I'll keep an eye on you.", Enabled = why == null, Why = why,
					Cost = "Free. The Street tells you when their deal comes up, and they'll remember you said it." });
			}
		}
		if (free) o.Add(new TalkOption { Verb = TalkVerb.NextSet, Voice = ExecutiveVoice.Street, Line = "When are you on next?", Cost = "Free." });
		if (free && notebook.FirstOrDefault(n => n?.Artist?.artistId == artistId) is { } note) {
			bool can = CanHoldWithHandshake(note, out string reason);
			o.Add(new TalkOption { Verb = TalkVerb.Handshake, Voice = ExecutiveVoice.Fixer, Line = "Give me a week.", Enabled = can, Why = reason,
				Cost = $"{HandshakeHours}h. No rival signs them for {HandshakeDays} days." });
		}
		return scene;
	}

	/// <summary>The Street's one fact about what they want, from the disposition's reason.</summary>
	private static string StreetReason(DispositionRead r) => r.Reason switch {
		DispositionReason.DayJob => "Day jobs, the lot of them. A record's the way out.",
		DispositionReason.Young => "Young, and in a hurry.",
		DispositionReason.OwnSongs => "They write. Writers want their songs on wax.",
		DispositionReason.Ambitious => "They've got ambition, and they don't hide it.",
		DispositionReason.DroppedBefore => "They've been on a label and got dropped. Once bitten.",
		DispositionReason.OutOfTown => "They don't know your label. You're from out of town.",
		DispositionReason.NoRecords => "They've never heard a record of yours.",
		DispositionReason.WantsAnotherLook => "They don't know you yet.",
		DispositionReason.Engagement => $"They've got {r.EngagementRoom} steady, and it pays.",
		DispositionReason.NoNeedForMoney => $"{r.WealthyMember} doesn't need the money.",
		DispositionReason.Settled => "Families and mortgages. Weekends suit them.",
		DispositionReason.WeekendAct => "Good day jobs. Weekends suit them.",
		_ => "They make a living in the rooms here."
	};

	/// <summary>Say a line in The Talk: the verb runs with its own rules and costs, and what you said and what came back go
	/// into tonight's transcript. True when the verb went ahead. <paramref name="terms"/> is the prospect whose contract
	/// opened, when one did.</summary>
	public bool SayInTalk(string artistId, TalkVerb verb, out string message, out Prospect terms) {
		terms = null; message = "They're not there to talk to.";
		var scene = BuildTalkScene(artistId);
		var option = scene?.Options.FirstOrDefault(x => x.Verb == verb);
		if (option == null) return false;
		if (!option.Enabled) { message = option.Why ?? "Not now."; return false; }
		if (Slate.All(p => p.Artist?.artistId != artistId)) OpenScoutingDossier(artistId, out _);
		var prospect = Slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
		var act = ArtistManager.Instance.GetArtist(artistId);
		bool ok;
		string answer = null;
		switch (verb) {
			case TalkVerb.Ask:
			case TalkVerb.Terms:
				if (prospect == null) return false;
				ok = ApproachToSign(prospect, out message);
				if (ok && prospect.HasBaseline) terms = prospect;
				// A conversation that went somewhere: a refusal here is still their answer, not a failure to talk.
				answer = message;
				ok = ok || !string.IsNullOrEmpty(message);
				break;
			case TalkVerb.Step: ok = TakeApproachStep(artistId, option.Step, out message); answer = message; break;
			case TalkVerb.History: ok = AskScoutingBusinessHistory(artistId, PreviewScoutingBusinessMeeting(artistId)?.QuoteId, out message); answer = message; break;
			case TalkVerb.Rehearsal:
			case TalkVerb.Demo:
			case TalkVerb.Meeting: {
				var activity = verb switch { TalkVerb.Rehearsal => ScoutingAppointmentActivity.Rehearsal, TalkVerb.Demo => ScoutingAppointmentActivity.Demo, _ => ScoutingAppointmentActivity.Conversation };
				var quote = PreviewScoutingInvitation(artistId, activity);
				ok = quote != null && ArrangeScoutingMeeting(artistId, quote.Id, out message, activity);
				answer = message;
				break;
			}
			case TalkVerb.Number: ok = AskActForNumber(artistId, out message); answer = message; break;
			case TalkVerb.NextSet: {
				var next = NextReachableScoutingSet(artistId);
				message = next == null ? "Nothing's posted yet. Ask a booker, or call us." : $"{next.RoomName}, {LocalSceneRoomService.Date(next.Day).ToHeadlineString()}, {next.StartMinute / 60:00}:{next.StartMinute % 60:00}. Come by.";
				ok = true;
				Talk(artistId, option.Line, new CallLine { beat = RolodexSceneBeat.Success, speaker = scene.Speaker, text = message });
				return true;
			}
			case TalkVerb.Handshake: ok = HoldWithHandshake(artistId, out message); answer = message; break;
			case TalkVerb.Answer: ok = AnswerObjection(artistId, ObjectionAnswer.Grounded, out message); answer = message; break;
			case TalkVerb.TakeYourTime: ok = AnswerObjection(artistId, ObjectionAnswer.Wait, out message); answer = message; break;
			case TalkVerb.SignWithout: ok = AnswerObjection(artistId, ObjectionAnswer.Without, out message); answer = message; break;
			case TalkVerb.Watch:
			case TalkVerb.Unwatch:
				ok = verb == TalkVerb.Watch ? WatchSigned(artistId, out message) : UnwatchSigned(artistId, out message);
				if (ok) Talk(artistId, option.Line, new CallLine { beat = RolodexSceneBeat.Success, speaker = scene.Speaker,
					text = verb == TalkVerb.Watch ? "We'll remember you said that." : "Thanks for coming out." });
				return ok;
			default: return false;
		}
		Talk(artistId, option.Line, string.IsNullOrEmpty(answer) ? null : new CallLine { beat = ok ? RolodexSceneBeat.Success : RolodexSceneBeat.Failure, text = answer });
		return ok;
	}

	private void Talk(string artistId, string said, CallLine answer) {
		if (!talkTranscripts.TryGetValue(artistId, out var lines)) talkTranscripts[artistId] = lines = new();
		lines.Add(new CallLine { beat = RolodexSceneBeat.PlayerPitch, isPlayer = true, speaker = "You", text = said });
		if (answer != null) lines.Add(answer);
	}
}
