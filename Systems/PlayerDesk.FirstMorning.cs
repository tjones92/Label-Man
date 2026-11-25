using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// THE FIRST MORNING (SimTools/OnboardingTutorialDirective.md §3.1, T1).
//
// Before the lawyer's letter and its rules, a short holder-specific scene answers "what's my situation?" in story terms: who
// holds the paper and what he wants from you. The holder is named on day one for all five (author decision 1, 2026-11-25):
// each bends the run toward one thing, so the player needs to know which at once. The early mystery is *why* he wanted it.
//
// Nothing here touches the sim: the lines are read from facts the run already holds (the holder, the home room, Connie, the
// Major Floria answers to, Clyde's publisher), and the scene has no choice in it.
// ============================================================================================
public partial class PlayerDesk {
	/// <summary>The heading of the scene: the time, and where the morning starts.</summary>
	public string FirstMorningHeading() {
		var today = LienToday;
		return $"NEW YEAR'S DAY, {today.year}  ·  A LITTLE BEFORE NOON";
	}

	/// <summary>One line, plain: who holds the paper and what he wants. Also the scene's last line.</summary>
	public string HolderWantsPlain() {
		if (lien == null) return null;
		var p = LienProfile;
		return lien.Holder switch {
			LienHolder.Sawyer => $"Dee-Dee Sawyer holds your paper. He wants a record by {ConnieStageName}, out this year.",
			LienHolder.Kubiak => $"Walter Kubiak holds your paper. He wants money: {lien.Rate:P0} a year, and what you don't pay goes on the sum.",
			LienHolder.Winters => "Clyde Winters holds your paper. He charges no interest. He wants to see the label making records that sell.",
			LienHolder.Floria => $"Gene Floria holds your paper, {p.Title}. He wants to see the label grow.",
			LienHolder.Germain => $"{GermainName} holds your paper. He wants to be consulted: his boys working your records, his name on a song or two.",
			_ => null,
		};
	}

	/// <summary>The scene, line by line: what happens at the door, the voices in the margin, then the plain situation.
	/// Each voice speaks only past its threshold; the player's strongest voice always gets a word.</summary>
	public List<CallLine> FirstMorningLines() {
		var lines = new List<CallLine>();
		if (lien == null || Label == null) return lines;
		void Say(string text, string speaker = "") =>
			lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, speaker = speaker });
		var voices = new List<(ExecutiveVoice Voice, string Text)>();
		string closing = null;   // a last beat after the voices have had their say
		var p = LienProfile;

		switch (lien.Holder) {
			case LienHolder.Sawyer:
				Say("A knock wakes you, face down on the desk. Then another, patient, the kind that will keep on knocking.");
				Say("A young woman on the step, a garment bag over one arm and a folder of sheet music under the other. A good coat and cheap shoes.");
				Say($"{ConnieStageName}. Dee-Dee said you'd be expecting me.", ConnieStageName);
				Say("She holds out a carbon of a recording contract. Your signature is at the bottom of it, more or less.");
				Say("He says you're going to make me a record. He says you promised, in front of everybody.", ConnieStageName);
				voices.Add((ExecutiveVoice.Street, "A woman at the door with your name on her contract. That's a new one, even for you."));
				voices.Add((ExecutiveVoice.Suit, "Five years, his option, no release clause. You can't drop her and you can't sell her paper. She's yours till Dee-Dee says otherwise."));
				voices.Add((ExecutiveVoice.Ear, "Ask her to sing something before you promise her anything else."));
				voices.Add((ExecutiveVoice.Fixer, "She doesn't know what happened last night either. Look at her. She's been told a story."));
				closing = $"On her way out she leaves a matchbook on the desk: {Cap(lien.Venue)}. Dee-Dee's room, the biggest in town.";
				break;
			case LienHolder.Kubiak:
				Say("A knock wakes you, face down on the desk, and a key turns in the lock before you can answer it.");
				Say("A boy of about sixteen with a ring of keys on his belt crosses the office to the jukebox in the corner. You don't remember owning a jukebox.");
				Say("He unlocks the coin box, tips it into a canvas bag, and writes out a receipt against his knee.");
				Say("Collection for Mr. Kubiak. Per your agreement.", "The boy");
				Say("The receipt says KUBIAK AMUSEMENT CO., $3.40, \"against account\". He leaves it on the desk and the door open behind him.");
				voices.Add((ExecutiveVoice.Suit, "\"Against account.\" The meter's running already. A man who sends a boy for three dollars will send men for the rest."));
				voices.Add((ExecutiveVoice.Street, "Kubiak's route. Bars past the river, old men and Sinatra. He isn't a big man. He just acts like one."));
				voices.Add((ExecutiveVoice.Fixer, "He wanted you to see the keys. That's the whole message."));
				voices.Add((ExecutiveVoice.Ear, "That machine's full of records nobody's played since the war."));
				break;
			case LienHolder.Winters: {
				string pub = WintersPublisher()?.publisherName ?? "a big publisher";
				Say("Something heavy lands against the door and wakes you. By the time you get there, whoever left it is gone.");
				Say($"A carton of professional copies tied with string: thirty songs from {pub}, every one of them a plug.");
				Say("On top, a note in a neat, friendly hand: \"To get you started, partner. So glad we're in business. Clyde Winters.\" There's a number, and it isn't his home.");
				voices.Add((ExecutiveVoice.Suit, $"\"In business.\" He lent you ${lien.FaceSum:N0} and charged no interest. Nobody does that unless what he wants is worth more than interest."));
				voices.Add((ExecutiveVoice.Ear, "Some of these are good. Not the ones he's marked."));
				voices.Add((ExecutiveVoice.Street, $"Winters plugs for {pub}. Every jock in town knows him, and every one of them dodges his calls on a Friday."));
				voices.Add((ExecutiveVoice.Fixer, "Friendly is a way of getting in the door. He's in."));
				break;
			}
			case LienHolder.Floria:
				Say("A car's engine, idling, under the window. It wakes you. It's been idling a while.");
				Say("A long black sedan at the curb with company plates. A man in a camel coat in the back seat, reading. Nobody gets out.");
				Say("After a minute the driver comes to the door, hands you a card without a word, and walks back. The car pulls away.");
				Say($"The card says Gene Floria, {p.Title}. There's nothing on the back.");
				voices.Add((ExecutiveVoice.Suit, "A Major's man holding your paper. A Major doesn't need your money. It wants something you'll have later."));
				voices.Add((ExecutiveVoice.Fixer, p.FixerRead));
				voices.Add((ExecutiveVoice.Street, "Company plates. He wanted you to know whose car it was."));
				voices.Add((ExecutiveVoice.Ear, "He never looked up from whatever he was reading. That's a man who's already decided."));
				break;
			case LienHolder.Germain: {
				string call = GermainStations().FirstOrDefault()?.callsign;
				Say("The radio on the filing cabinet has been on all night. It's the morning man that wakes you" + (string.IsNullOrEmpty(call) ? "." : $", on {call}."));
				Say($"...and this one goes out from an old friend of the station, {GermainName}, to the new man at {Label.labelName}. Jack says: Happy New Year, partner. Don't lose my number.", "The morning man");
				Say("Then a record you've never heard. You'd swear it was the one playing at midnight.");
				voices.Add((ExecutiveVoice.Fixer, "Jack's been off the air since the sweep and he still gets a dedication read. That's a man with friends."));
				voices.Add((ExecutiveVoice.Suit, "A grand jury is reading Jack's books. You don't want to be in them."));
				voices.Add((ExecutiveVoice.Street, "Half the region heard that. Now they all know you're Jack's."));
				voices.Add((ExecutiveVoice.Ear, "And whatever that record was, he picked it. He still knows a record."));
				break;
			}
		}

		var inst = InstinctProfile;
		var heard = voices.Where(v => Score(inst, v.Voice) >= 3).OrderByDescending(v => Score(inst, v.Voice)).ToList();
		if (heard.Count == 0 && voices.Count > 0) heard.Add(voices.OrderByDescending(v => Score(inst, v.Voice)).First());
		foreach (var (voice, text) in heard.Take(3))
			lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, voice = voice, speaker = "" });

		if (closing != null) Say(closing);
		Say(HolderWantsPlain());
		Say("Why he wanted your paper at all, you've no idea. The lawyer's letter is still on the desk.");
		return lines;
	}

	/// <summary>He has shown his hand: the first move has landed (taken, refused or missed), or a sitting has been held. Being named
	/// isn't enough for the leads that follow from what he did (Clyde's secretary, Jack's book): the player has to have met him.
	/// The saved <c>HolderIdentified</c> flag records it (the move's answer or lapse, or a sitting opened); before T1 it meant
	/// "named", which a Sawyer run got from Connie's paper at founding.</summary>
	public bool HolderShownHand => lien != null && (lien.HolderIdentified
		|| lien.Ledger.Any(e => e.Kind is LienLedgerKind.Extended or LienLedgerKind.Seized));
}
