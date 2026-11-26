using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// THE TABLE (SimTools/SigningExperienceAudit.md §3.4).
//
// The contract sheet keeps the page; this puts somebody across from it. Their manager, or the member who talks for
// them, states the ask as a line in their own manner (a hustler wants it done tonight, a shark wants it up front, a
// Svengali keeps the reins, a visionary keeps the publishing), and every term of the ask is in that line. The Suit
// reads the number against what acts from that kind of room ask, your cash and your roster, and at 5 names the term
// they'll fight for. The Fixer says whether the other label they talk about has made an offer. What they say back
// across the table, objections and counters, comes into the same transcript in their voice.
//
// Presentation only: every line wraps the ask, the talk's log, the outlook the act stated, and rival word that has
// reached you. Nothing is computed that the acceptance check doesn't already hold, and nothing is saved.
// ============================================================================================
public sealed class TableScene {
	public string ArtistId, ArtistName, Speaker;
	/// <summary>Narration: who sits down across from you.</summary>
	public string Setting;
	/// <summary>Their ask, the voices, then what's been said across the table so far, oldest first.</summary>
	public List<CallLine> Lines = new();
}

public partial class PlayerDesk {
	/// <summary>The act stated this when it believes a better-placed label could want it; no offer stands behind it.</summary>
	internal const string RivalBeliefLine = "They believe a label with wider distribution could take an interest in them, and they'd want to hear it out. No offer has been made that you know of.";

	/// <summary>The Table for a Firm or Hardball negotiation, new signing or renewal.</summary>
	public TableScene BuildTableScene(ContractTalk talk) =>
		talk?.Artist == null ? null : BuildTable(talk.Artist, talk.ask, talk.IsRenewal, talk.prospect, talk, talk.outlookStated);

	/// <summary>The Table for an easy signing, which has no talk: one offer, one counter at most.</summary>
	public TableScene BuildTableScene(Prospect prospect) =>
		prospect?.Artist == null || !prospect.HasBaseline ? null
			: BuildTable(prospect.Artist, prospect.Baseline, false, prospect, null, ContractOutlookLines(prospect.Artist, renewal: false));

	/// <summary>The Table for an easy renewal.</summary>
	public TableScene BuildTableScene(RenewalOffer offer) =>
		offer?.Artist == null ? null : BuildTable(offer.Artist, offer.Ask, true, null, null, ContractOutlookLines(offer.Artist, renewal: true));

	/// <summary>Who sits across the table: the manager when there is one, else the member who talks for the act.</summary>
	internal string TableSpeaker(SimulatedArtist act, ContractTermSheet ask, out bool manager) {
		manager = ask.Manager != ManagerArchetype.None && !string.IsNullOrEmpty(ask.ManagerName);
		return manager ? ask.ManagerName : act == null ? "They" : SpeakerFor(act);
	}

	private TableScene BuildTable(SimulatedArtist act, ContractTermSheet ask, bool renewal, Prospect prospect, ContractTalk talk, IReadOnlyList<string> stated) {
		string speaker = TableSpeaker(act, ask, out bool manager);
		var scene = new TableScene { ArtistId = act.artistId, ArtistName = act.stageName, Speaker = speaker };

		// ── Who sits down ──
		string intro = manager ? ManagerIdentity.Introduce(ManagerIdentity.Today(act)) ?? speaker : speaker;
		string who = intro == speaker ? intro : intro + ",";   // "Sol Kramer, their personal manager, sits down"
		string across = intro == act.stageName ? $"{intro} sits down across the table from you." : $"{who} sits down across the table from you, for {act.stageName}.";
		scene.Setting = renewal ? $"{act.stageName}'s paper has run out. " + across : across;

		// ── The ask, said ──
		scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, speaker = speaker, text = AskLine(ask, manager) });
		// A renewal ask can carry what they remember (a cheap signing, a promise broken): said in narration, as it was.
		string plain = AILabel.BuildDemandSummary(ask.Manager, ask.Advance, ask.RoyaltyRate, ask.LabelOwnsPublishing, ask.ArtistCreativeControl);
		if (!string.IsNullOrEmpty(ask.DemandSummary) && ask.DemandSummary.StartsWith(plain, StringComparison.Ordinal)
				&& ask.DemandSummary[plain.Length..].Trim() is { Length: > 0 } remembered)
			scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.SituationRead, speaker = "", text = remembered });

		// ── The voices ──
		void Read(ExecutiveVoice voice, string hint, string clear, string deep) {
			int score = InstinctScore(InstinctProfile, voice);
			string text = score >= 5 ? deep ?? clear ?? hint : score >= 4 ? clear ?? hint : score >= 3 ? hint : null;
			if (!string.IsNullOrEmpty(text)) scene.Lines.Add(new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = voice, text = text });
		}

		// THE SUIT: the accountant. The number against the room's usual, your cash, your roster; at 5, what they'll fight for.
		string figure = $"${ask.Advance:N0} up front";
		string verdict = null;
		if (!renewal && prospect != null) {
			var room = prospect.SceneRoomId != null ? SceneRoomCatalog.Get(prospect.SceneRoomId) : null;
			string from = room != null ? "a " + SceneRoomCharacter.KindName(room.Kind) : VenueName(prospect.Venue);
			var (low, high) = TypicalAsk(room?.Category ?? prospect.Venue);
			float mid = (low + high) / 2f;
			verdict = ask.Advance > mid * 1.4f ? $"{figure} is rich for an act out of {from}."
				: ask.Advance < mid * 0.7f ? $"{figure} is cheap. Acts out of {from} are usually cheap for a reason."
				: $"{figure} is about what acts out of {from} ask.";
		} else if (renewal) {
			verdict = act.royaltyRate > 0f && Math.Abs(ask.RoyaltyRate - act.royaltyRate) >= 0.0025f
				? $"{figure}, and {Points(ask.RoyaltyRate)} where the old paper paid {Points(act.royaltyRate)}."
				: $"{figure}, and the same points as the old paper.";
		}
		float cash = Label?.cashReserves ?? 0f;
		string money = cash <= 0f ? "" : ask.Advance >= cash ? " That's more than you have."
			: $" That's {Math.Max(1, (int)Math.Round(ask.Advance / cash * 100f))}% of the cash on hand.";
		string roster = renewal || Label == null ? "" : Label.HasRosterSpace
			? $" They'd be act {Label.CurrentRosterSize + 1} of {Label.maxRosterSize}."
			: $" The roster's full: {Label.CurrentRosterSize} of {Label.maxRosterSize}.";
		string fights = FightsFor(act, ask, talk, prospect, manager);
		if (verdict != null) Read(ExecutiveVoice.Suit, verdict, verdict + money + roster, verdict + money + roster + (fights == null ? "" : " " + fights));

		// THE FIXER: the operator. Real paper and real asking-around, when word has reached you; otherwise whether the label
		// they talk about is anything more than a hope.
		int before = scene.Lines.Count;
		FixerRivalRead(act.artistId, Read);
		bool fixerSpoke = scene.Lines.Count > before;
		if (!fixerSpoke && stated != null && stated.Contains(RivalBeliefLine) && PendingOffer(act.artistId) == null)
			Read(ExecutiveVoice.Fixer,
				"Nobody's put paper in front of them that I've heard of.",
				"That label they're talking about is a hope, not an offer. Nobody's made one that I've heard of.",
				null);

		// ── What's been said across the table ──
		if (talk != null)
			foreach (string said in Enumerable.Reverse(talk.log)) scene.Lines.AddRange(SpokenLines(said, speaker));
		return scene;
	}

	/// <summary>The ask in the speaker's own manner. Every term is in it: the advance, the points, the years, the sides
	/// owed, who keeps the publishing, who has the last word.</summary>
	internal static string AskLine(ContractTermSheet a, bool manager) {
		string we = manager ? "they" : "we", us = manager ? "them" : "us";
		string money = $"${a.Advance:N0} up front and {Points(a.RoyaltyRate)} on the records";
		string paper = $"{Plural.Of(a.TermYears, "year")}, " + (a.SinglesObligation > 0 ? $"{Plural.Of(a.SinglesObligation, "single")} owed." : "no sides owed.");
		string pub = a.LabelOwnsPublishing ? "" : $" The publishing stays with {us}.";
		string ctrl = a.ArtistCreativeControl ? $" And {we} have the last word on what gets cut." : "";
		if (manager) switch (a.Manager) {
			case ManagerArchetype.LocalHustler:
				return $"Let's not waste each other's time. {money}, and you work the record hard around town. {paper}{pub}{ctrl} We can shake on it tonight.";
			case ManagerArchetype.Shark:
				return $"{Cap(money)}. Up front means up front, not out of the first record. {paper}{pub}{ctrl} The paper stays short, too; we talk again when they're worth more.";
			case ManagerArchetype.Svengali:
				return (a.ArtistCreativeControl ? "I shape this act, and I don't hand that to a label: I keep the last word on what gets cut. " : "I shape this act. ") +
					$"{Cap(money)}. {paper} A year longer than most ask; I build acts, I don't rent them out.{pub}";
			case ManagerArchetype.Visionary when !a.LabelOwnsPublishing:
				return $"The publishing stays with {us}. That isn't a point for discussion.{ctrl} {Cap(money)}. {paper}";
		}
		return $"{Cap(we)}'d want {money}. {paper}{pub}{ctrl}";
	}

	/// <summary>A royalty as the form types it: quarter points kept ("3.25%"), whole ones plain ("5%").</summary>
	internal static string Points(float rate) => $"{rate * 100f:0.##}%";

	/// <summary>The Suit's deep read: the term this act weighs most, from the same weights the acceptance check uses.</summary>
	private static string FightsFor(SimulatedArtist act, ContractTermSheet ask, ContractTalk talk, Prospect prospect, bool manager) {
		var read = manager ? ManagerIdentity.Today(act) : null;
		string he = read == null ? "they" : read.He;
		if (ManagerProfile.Of(ask.Manager).DemandsArtistPublishing && !ask.LabelOwnsPublishing)
			return "The publishing is a hard line. Nothing you put on the money buys it back.";
		if (talk == null && prospect != null && ask.Manager == ManagerArchetype.LocalHustler && manager)
			return $"And {he} wants it done tonight. {Cap(he)}'ll take less than {he} asks before {he} walks.";
		var weights = talk?.weights ?? NegotiationWeights(act);
		var top = weights.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
		return top switch {
			ContractAxis.Advance => "It's the money up front they'll fight for. Give on the rest before you give on that.",
			ContractAxis.Royalty => "It's the points they'll fight for, more than the advance.",
			ContractAxis.Term => "It's the length of the paper they'll fight for.",
			ContractAxis.Deliverables => "It's the sides owed they'll fight for.",
			ContractAxis.Publishing => "They care about the publishing more than the money.",
			ContractAxis.CreativeControl => "They care about the last word in the studio more than the money.",
			_ => null,
		};
	}

	/// <summary>A line from the talk's log, split into what was said (in quotes, by the speaker) and what happened.</summary>
	internal static IEnumerable<CallLine> SpokenLines(string said, string speaker) {
		if (string.IsNullOrWhiteSpace(said)) yield break;
		string[] parts = said.Split('"');
		for (int i = 0; i < parts.Length; i++) {
			string text = parts[i].Trim();
			if (text.Length == 0) continue;
			bool spoken = i % 2 == 1;
			yield return new CallLine { beat = spoken ? RolodexSceneBeat.Pushback : RolodexSceneBeat.SituationRead, speaker = spoken ? speaker : "", text = text };
		}
	}

}
