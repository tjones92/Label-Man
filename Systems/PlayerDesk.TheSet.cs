using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// THE SET (SimTools/SigningExperienceAudit.md §3.2).
//
// The moment you hear an act, staged as a short scene instead of a toast: the room, the songs, what each voice made of
// it, and the act coming off the stand. Presentation only. Every line wraps evidence the notebook already holds (the
// fogged set reads, the room's blind spots, the crowd you saw, the ask they've stated, rival word that has reached
// you); nothing is computed that the sim doesn't already know, and nothing is saved.
//
// Voices follow the Rolodex rule: under 3 a voice says nothing, 3 a hint, 4 a clear read, 5 a deep one. A read is only
// as good as the fog draw behind it, so a weak scout's Ear can be confidently wrong. While the first-week card is on its
// first two lines, the Ear also speaks a teaching verdict at any instinct (audit §5 decision 2), in its own register.
// ============================================================================================
public sealed class SetScene {
	public string ArtistId, ArtistName, RoomName;
	/// <summary>Narration: the room as you saw it.</summary>
	public List<string> Room = new();
	/// <summary>The songs you heard, in order: (title, their own). Empty when you came in partway.</summary>
	public List<(string Title, bool Original)> Songs = new();
	/// <summary>A note when the song list isn't whole (partway through, untitled songs).</summary>
	public string SongsNote;
	/// <summary>The voices, in order: Ear, Street, Suit, Fixer, then any teaching line.</summary>
	public List<CallLine> Reads = new();
	/// <summary>Narration: the act coming off the stand, and who talks for them.</summary>
	public string OffStage;
}

public partial class PlayerDesk {
	/// <summary>The scene for the last set heard by this act, or null when there's no heard set in the notebook.</summary>
	public SetScene BuildSetScene(string artistId) {
		var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
		var last = dossier == null ? null : ScoutingComparison.Performances(dossier).LastOrDefault();
		if (last?.Interpretation == null) return null;
		var act = ArtistManager.Instance?.GetArtist(artistId);
		var f = ImpressionFactsFor(last, act);
		var kind = SceneRoomCatalog.Get(last.RoomId)?.Kind;
		string key = artistId + "|" + (last.Id ?? last.EventId) + "|set";
		string V(string slot, params string[] wordings) => Phrase.Pick(key + "|" + slot, wordings);
		var scene = new SetScene {
			ArtistId = artistId, ArtistName = dossier.ArtistName ?? act?.stageName ?? "The act",
			RoomName = last.RoomName ?? SceneRoomCatalog.Get(last.RoomId)?.Name ?? "the room",
		};

		// ── The room ──
		int? present = last.SetPresent ?? last.BillAttendance;
		int capacity = last.BillCapacity;
		string kindName = kind.HasValue ? SceneRoomCharacter.KindName(kind.Value) : null;
		string crowd = present is int p && capacity > 0
			? p <= 15 ? V("thin", $"{Phrase.Capitalize(Phrase.Number(p))} people in a room that holds {capacity}.", $"{Phrase.Capitalize(Phrase.Number(p))} people, and room for {capacity}.")
			: (float)p / capacity >= .85f ? V("full", "Nearly full. You stood.", "Every table taken and people along the wall.")
			: (float)p / capacity >= .6f ? V("busy", "Busy, a few chairs empty.", "A good crowd, a few spaces at the back.")
			: (float)p / capacity >= .35f ? V("half", "About half full.", "Half the chairs taken.")
			: V("quiet", "Plenty of empty chairs.", "A quiet night; more chairs than people.")
			: null;
		// "Woodward Coffeehouse, a coffeehouse" says it twice: the kind only when the name doesn't.
		bool named = kindName == null || scene.RoomName.Contains(kindName, StringComparison.OrdinalIgnoreCase);
		scene.Room.Add(($"{scene.RoomName}{(named ? "" : ", a " + kindName)}. " + (crowd ?? "")).TrimEnd());
		if (!f.FullCoverage) scene.Room.Add(f.SetSongs > 0 ? $"You came in partway through their {Phrase.Number(f.SetSongs)}-song set." : "You came in partway through their set.");

		// ── The songs ──
		if (f.FullCoverage) {
			foreach (var song in last.Set ?? new List<SceneSetSong>())
				if (!string.IsNullOrEmpty(song.Title)) scene.Songs.Add((song.Title, song.Original));
			int untitled = (last.Set?.Count ?? 0) - scene.Songs.Count;
			if (untitled > 0) scene.SongsNote = $"and {Plural.Of(untitled, "song")} you couldn't put a name to";
		}

		// ── The voices ──
		void Read(ExecutiveVoice voice, string hint, string clear, string deep) {
			int score = InstinctScore(InstinctProfile, voice);
			string text = score >= 5 ? deep ?? clear ?? hint : score >= 4 ? clear ?? hint : score >= 3 ? hint : null;
			if (!string.IsNullOrEmpty(text)) scene.Reads.Add(new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = voice, text = text });
		}

		// THE EAR: the snob. The four fogged reads, the best and worst of them, and what the room hid.
		var axes = new List<(string Name, float Value, ScoutingQuestion Q)> {
			(f.Solo ? "the playing" : "the band", f.Execution, ScoutingQuestion.Ensemble),
			("the songs", f.Material, ScoutingQuestion.Material),
			("the way they put it across", f.Communication, ScoutingQuestion.Stage),
		};
		if (f.Vocals.HasValue) axes.Insert(0, (f.LeadSinger != null ? f.LeadSinger + "'s voice" : "the singing", f.Vocals.Value, ScoutingQuestion.Vocals));
		float mean = axes.Average(a => a.Value);
		var best = axes.OrderByDescending(a => a.Value).First();
		var worst = axes.OrderBy(a => a.Value).First();
		bool spread = best.Value - worst.Value >= .08f;
		string earHint = mean >= ScoutingImpressionPhrases.High
			? V("ear-good", "There's something real up there. I don't say that often.", "That's an act. Not a bar band: an act.", "I'd go back to hear that, and I don't go back for many.")
			: mean >= ScoutingImpressionPhrases.Middle
			? V("ear-mid", "Competent. Every bar in town has competent.", "Nothing wrong with them, and nothing that stops you either.", "Professional enough. Professional isn't the same as good.")
			: V("ear-poor", "Not ready. Maybe not ever.", "They're not there yet, and I couldn't tell you they're on the way.", "Amateurs. Some amateurs get better.");
		// "the songs were", "the band was".
		string Was(string axis) => axis == "the songs" ? "were" : "was";
		string Is(string axis) => axis == "the songs" ? "are" : "is";
		string earClear = !spread
			? earHint + " " + V("ear-even", "Even all the way through: nothing carries them, nothing sinks them.", "No one thing stands out, good or bad.")
			: best.Value >= ScoutingImpressionPhrases.High
			? earHint + " " + V("ear-spread", $"{Phrase.Capitalize(best.Name)} {Is(best.Name)} the reason to come back; {worst.Name} {Is(worst.Name)} the reason to worry.",
				$"{Phrase.Capitalize(best.Name)} {Was(best.Name)} the best of it. {Phrase.Capitalize(worst.Name)} {Was(worst.Name)} the worst.")
			: earHint + " " + V("ear-thin", $"{Phrase.Capitalize(best.Name)} {Was(best.Name)} the best of it, and that's not saying much. {Phrase.Capitalize(worst.Name)} {Was(worst.Name)} the worst.",
				$"Nothing up there was strong. {Phrase.Capitalize(worst.Name)} {Was(worst.Name)} the weakest.");
		string earDetail =
			f.Material >= ScoutingImpressionPhrases.High && f.OriginalTitle != null && f.FullCoverage ? $"And “{f.OriginalTitle}” is their own. That's the one I'd want on tape."
			: f.SetSongs > 0 && f.Originals == 0 && f.FullCoverage && f.Material < ScoutingImpressionPhrases.Middle ? "All covers, and not chosen well. Nobody up there is writing."
			: f.SetSongs > 0 && f.Originals == 0 && f.FullCoverage ? "Every song somebody else's. Good taste is not the same as having something to say."
			: f.Vocals is float voice && voice >= ScoutingImpressionPhrases.High && f.LeadSinger != null && !f.Solo ? $"{f.LeadSinger} is the act. The rest you can hire."
			: null;
		Read(ExecutiveVoice.Ear, earHint, earClear, earDetail == null ? null : earClear + " " + earDetail);
		// The room's blind spot, where it falls on a weak read: that's the read the room may have done to them.
		var hidden = kind.HasValue ? axes.Where(a => a.Value < ScoutingImpressionPhrases.Middle && SceneRoomCharacter.Masks(kind.Value, a.Q) != null)
			.OrderBy(a => a.Value).Cast<(string Name, float Value, ScoutingQuestion Q)?>().FirstOrDefault() : null;
		if (hidden is { } h)
			Read(ExecutiveVoice.Ear, null,
				$"Mind the room: {SceneRoomCharacter.Masks(kind.Value, h.Q)}. Before I believed that about {h.Name}, I'd hear them in {SceneRoomCharacter.BetterRoomFor(h.Q)}.",
				null);

		// THE STREET: the gossip. Who stayed, who came in for them, who called for songs. Only what you saw.
		bool paying = f.Room != HeardRoom.TradeEvent;
		if (paying && f.Stayed.HasValue) {
			float stayed = f.Stayed.Value;
			string held = stayed >= .75f ? V("st-held", "They held the room start to finish.", "The room stayed put for them.")
				: stayed >= .5f ? V("st-half", "Half the room stayed with them; the other half found the bar.", "They kept about half the room.")
				: V("st-lost", "They lost the room before the end.", "The room drifted off while they played.");
			int arrived = f.Arrivals ?? 0;
			string came = arrived <= 0 ? V("st-none", "Nobody came through the door for them. The crowd was the room's, not theirs.", "Nobody I could see came in for them.")
				: arrived < 8 ? V("st-few", "A handful came in for their set. It's a start.", "A few came in just for them.")
				: arrived < 20 ? V("st-dozen", "A dozen or so walked in for their set. That's the start of a following.", "About a dozen came in for them. Small, but it's theirs.")
				: V("st-many", "A good crowd came in for them. Somebody in this town already knows their name.", "People came in for this act. That's a following, not a night out.");
			string calls = (f.Requests ?? 0) > 0 ? " " + V("st-calls", "And they were calling out for songs by name. Nobody does that for a house band.", "People shouted for their songs by name.") : "";
			Read(ExecutiveVoice.Street, held, held + " " + came, held + " " + came + calls);
		}

		// THE SUIT: the accountant. Only when there's a figure to read: their ask, their paper, your roster.
		var prospect = Slate.FirstOrDefault(p => p.Artist?.artistId == artistId);
		if (act != null && !string.IsNullOrEmpty(act.labelId) && act.labelId != Label?.labelId) {
			string label = ChartManager.Instance?.GetLabelName(act.labelId) ?? "another label";
			Read(ExecutiveVoice.Suit, $"They're under paper with {label}. Whatever you heard, it isn't for sale.", null, null);
		} else if (prospect != null && act != null && SceneDealAvailable(prospect)) {
			float ask = prospect.AskingAdvance;
			string verdict = "";
			if (SceneRoomCatalog.Get(last.RoomId) is { } room) {
				var (low, high) = TypicalAsk(room.Category);
				float mid = (low + high) / 2f;
				verdict = ask > mid * 1.4f ? $" Rich, for an act out of a {kindName ?? "room like that"}."
					: ask < mid * 0.7f ? " Cheap. Acts are usually cheap for a reason."
					: $" About what a {kindName ?? "room like that"} act asks.";
			}
			string askLine = $"They're asking ${ask:N0} up front.{verdict}";
			float cash = Label?.cashReserves ?? 0f;
			string money = cash > 0 ? ask >= cash ? " That's more than you have." : $" That's {Math.Max(1, (int)Math.Round(ask / cash * 100f))}% of the cash on hand." : "";
			string roster = Label == null ? "" : Label.HasRosterSpace
				? $" They'd be act {Label.CurrentRosterSize + 1} of {Label.maxRosterSize} on the roster."
				: $" And the roster's full: {Label.CurrentRosterSize} of {Label.maxRosterSize}. Somebody would have to go.";
			Read(ExecutiveVoice.Suit, askLine, askLine + money, askLine + money + roster);
		}

		// THE FIXER: the operator. Only when another label's interest is real and word of it has reached you.
		var offer = rivalOffers.Where(o => o.ArtistId == artistId && o.Status == RivalOfferStatus.Pending && o.Disclosure != RivalOfferDisclosure.Unknown)
			.OrderBy(o => o.KnownDay).LastOrDefault();
		var courtship = rivalCourtships.Where(c => c.ArtistId == artistId && CourtshipLive(c) && c.Known.Any(k => k.Delivered)).LastOrDefault();
		if (offer != null)
			Read(ExecutiveVoice.Fixer,
				"Somebody else has an offer in front of them. Don't dawdle.",
				$"{offer.RivalLabelName} has an offer on the table. They answer by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}.",
				$"{offer.RivalLabelName} has an offer on the table" + (offer.Disclosure == RivalOfferDisclosure.Disclosed ? $": {RivalTermsText(offer.Terms)}" : "") +
				$". They answer by {LocalSceneRoomService.Date(offer.DecisionDay).ToHeadlineString()}. That's your number to beat, and your deadline.");
		else if (courtship != null) {
			var word = courtship.Known.Where(k => k.Delivered).OrderBy(k => k.Day).Last();
			Read(ExecutiveVoice.Fixer,
				"You're not the only one sniffing around this act.",
				$"{courtship.RivalLabelName} has been asking about them. You're not the only one in the room.",
				$"{courtship.RivalLabelName} has been asking about them; you had it from {word.SourceName ?? "the street"}. " +
				(courtship.Stage == RivalCourtshipStage.Meeting ? "They've already sat down together." : "No sit-down yet, as far as anybody's said."));
		}

		// First-time teaching (the first-week card's first two lines): the Ear's honest verdict, at any instinct.
		if (FirstWeekCurrent is FirstWeekStep.Hear or FirstWeekStep.Ask) {
			bool strongEar = InstinctScore(InstinctProfile, ExecutiveVoice.Ear) >= 3;
			bool spokenFor = act != null && !string.IsNullOrEmpty(act.labelId) && act.labelId != Label?.labelId;
			bool roomHidWorst = hidden is { } hw && hw.Q == worst.Q;
			string teach = spokenFor
				? "Whatever I heard, they're spoken for. Hear somebody who isn't."
				: !strongEar
				? "I can't tell you if they're any good. Ask somebody who can, or hear enough acts that you start to know."
				: mean >= ScoutingImpressionPhrases.Middle
				? "If you want them, go and talk to them now, while they're still packing up."
				: roomHidWorst
				? $"I can't judge {worst.Name} in a room like that. Hear them somewhere else, or hear somebody else, before you ask."
				: spread
				? $"{Phrase.Capitalize(worst.Name)} {Was(worst.Name)} the weak link. Hear somebody else before you ask anybody anything."
				: "Hear somebody else before you ask anybody anything.";
			scene.Reads.Add(new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = ExecutiveVoice.Ear, text = teach });
		}

		// ── Off the stand ──
		var meeting = PreviewScoutingBusinessMeeting(artistId);
		string off = f.Solo ? $"{scene.ArtistName} comes off the stand." : $"{scene.ArtistName} come off the stand.";
		if (meeting?.SourceName is { Length: > 0 } speaker && speaker != scene.ArtistName) off += $" {speaker} does the talking for them.";
		scene.OffStage = off;
		return scene;
	}
}
