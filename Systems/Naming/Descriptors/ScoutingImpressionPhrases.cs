using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>The kind of room a set was heard in, as the descriptor layer sees it (mirrors SceneRoomKind).</summary>
public enum HeardRoom { Unknown, Club, ListeningRoom, Coffeehouse, CommunityHall, Roadhouse, TradeEvent }

/// <summary>
/// Everything one heard set establishes, gathered by PlayerDesk.ScoutingListeningAxes from the observation, the room
/// and the lineup that was on stage. Every field is evidence the player already holds; nothing here is a hidden stat.
/// </summary>
public sealed class ImpressionFacts {
	/// <summary>Act id plus observation id: the wording key.</summary>
	public string Key;
	/// <summary>The player's interpreted reads (0-1). Vocals is null when nobody on stage sang lead.</summary>
	public float? Vocals;
	public float Execution, Material, Communication;
	/// <summary>One performer, no group to play together.</summary>
	public bool Solo;
	/// <summary>The act's own people on stage, and players from other acts sitting in behind them.</summary>
	public int Players, SittingIn;
	/// <summary>First name of the only lead vocalist on stage; null when there were none or several.</summary>
	public string LeadSinger;
	public int LeadVoices, BackingVoices;
	/// <summary>Parts played on stage (from the players' roles), in plain words, distinct.</summary>
	public List<string> Instruments = new();
	public bool Drums;
	/// <summary>The solo performer's own instrument, or null.</summary>
	public string SoloInstrument;
	/// <summary>The scheduled set: song count, originals among them, one title of each kind, and the genres it spans.</summary>
	public int SetSongs, Originals;
	public string OriginalTitle, CoverTitle;
	public List<string> SetGenres = new();
	/// <summary>True when at least nine-tenths of the set was heard.</summary>
	public bool FullCoverage;
	public HeardRoom Room;
	/// <summary>The audience of this set, as seen from the room. Null when the observation predates audience evidence.</summary>
	public int? Present, Arrivals, Requests;
	public float? Stayed;
}

public readonly record struct AxisImpression(string Axis, string Read, string RoomNote);

/// <summary>
/// Turns one heard set into the four notebook lines (vocals, playing, material, stage). Each line is a head that names
/// the strength of the impression plus, where the evidence supports one, a clause naming something the player saw:
/// who sang, what was on stage, how many songs were the act's own, whether the room stayed. Heads and clauses that
/// would name the same fact twice in one line are not paired. The room's blind spot is a separate short note.
/// </summary>
public static class ScoutingImpressionPhrases {
	public const float High = .72f, Middle = .48f;

	private readonly record struct Bit(bool When, string Text, string Fact = null);
	private static Bit B(bool when, string text, string fact = null) => new(when, text, fact);

	public static IReadOnlyList<AxisImpression> Describe(ImpressionFacts f) => new[] {
		new AxisImpression("Vocals", Vocals(f), RoomNote(f, "Vocals")),
		new AxisImpression("Playing", Playing(f), RoomNote(f, "Playing")),
		new AxisImpression("Material", Material(f), RoomNote(f, "Material")),
		new AxisImpression("Stage presence", Stage(f), RoomNote(f, "Stage"))
	};

	private static int Band(float v) => v >= High ? 2 : v >= Middle ? 1 : 0;

	/// <summary>Is this axis clearly the best (or worst) of the set's reads? Only then may a line say so.</summary>
	private static bool Standout(ImpressionFacts f, float value, bool best) {
		var others = new List<float> { f.Execution, f.Material, f.Communication };
		if (f.Vocals.HasValue) others.Add(f.Vocals.Value);
		others.Remove(value);
		if (others.Count == 0) return false;
		return best ? value >= High && value - others.Max() >= .08f : value < Middle && others.Min() - value >= .08f;
	}

	private static string Compose(string key, IEnumerable<Bit> heads, IEnumerable<Bit> clauses) {
		var liveHeads = heads.Where(h => h.When && !string.IsNullOrEmpty(h.Text)).ToArray();
		if (liveHeads.Length == 0) return "";
		var head = liveHeads[Phrase.Index(key + "|head", liveHeads.Length)];
		var liveClauses = clauses.Where(c => c.When && !string.IsNullOrEmpty(c.Text) && (c.Fact == null || c.Fact != head.Fact)).ToArray();
		if (liveClauses.Length == 0) return head.Text;
		return head.Text + "; " + liveClauses[Phrase.Index(key + "|clause", liveClauses.Length)].Text;
	}

	private static string Plural(int n, string one, string many) => $"{Phrase.Number(n)} {(n == 1 ? one : many)}";

	// ---- vocals ---------------------------------------------------------------------------------------------------
	private static string Vocals(ImpressionFacts f) {
		string key = f.Key + "|vocals";
		if (!f.Vocals.HasValue)
			return Phrase.Pick(key, Phrase.Always("No lead vocals heard"), Phrase.When(f.Players > 0, "Nobody sang lead; an instrumental set"),
				Phrase.When(f.Players > 0, "No lead singer on stage"));
		float v = f.Vocals.Value; string L = f.LeadSinger; bool named = L != null, shared = f.LeadVoices >= 2;
		bool best = Standout(f, v, true), worst = Standout(f, v, false);
		var heads = Band(v) switch {
			2 => new[] {
				B(true, "Confident, convincing singing"), B(true, "A voice that held every song"), B(true, "Sang with real command"),
				B(named, $"{L} sang with real authority", "lead"), B(named, $"{L}'s voice carried the set", "lead"),
				B(best, "The singing was the best thing you heard"), B(shared, "Strong lead voices, traded well", "shared")
			},
			1 => new[] {
				B(true, "Carried the songs; not always convincing"), B(true, "A serviceable voice that seldom lifted a song"),
				B(true, "Got the songs across without quite selling them"), B(named, $"{L} kept the tunes but seldom lifted them", "lead"),
				B(shared, $"Lead vocals split {Phrase.Number(f.LeadVoices)} ways; serviceable overall", "shared")
			},
			_ => new[] {
				B(true, "Uncertain singing"), B(true, "The voice struggled to hold the songs"), B(true, "Singing that rarely settled"),
				B(named, $"{L} struggled to hold the songs", "lead"), B(worst, "The singing was the weak spot")
			}
		};
		var clauses = new[] {
			B(f.BackingVoices > 0, $"{Plural(f.BackingVoices, "backing voice", "backing voices")} behind the lead"),
			B(shared, $"the lead passed between {Phrase.Number(f.LeadVoices)} singers", "shared")
		};
		return Compose(key, heads, clauses);
	}

	// ---- playing --------------------------------------------------------------------------------------------------
	private static string Playing(ImpressionFacts f) {
		string key = f.Key + "|playing";
		bool best = Standout(f, f.Execution, true), worst = Standout(f, f.Execution, false);
		string inst = f.SoloInstrument; bool hasInst = inst != null;
		string size = f.Players >= 2 && f.Players <= 12 ? Phrase.Number(f.Players) + "-piece" : null;
		Bit[] heads;
		if (f.Solo) heads = Band(f.Execution) switch {
			2 => new[] { B(true, "Assured, sure-handed playing"), B(hasInst, $"Clean, confident {inst}"), B(true, "Played with real assurance"),
				B(best, "The playing was the strongest thing you heard") },
			1 => new[] { B(true, "Capable, but not always settled"), B(hasInst, $"Capable {inst}, not always settled"), B(true, "Steady enough playing") },
			_ => new[] { B(true, "Uneven playing"), B(hasInst, $"Shaky {inst}"), B(true, "Playing that rarely settled"), B(worst, "The playing was the weak spot") }
		};
		else heads = Band(f.Execution) switch {
			2 => new[] { B(true, "Assured playing together"), B(true, "Locked in from the first song"), B(true, "Played as one"),
				B(size != null, $"A tight {size}", "size"), B(best, "The playing was the strongest thing you heard") },
			1 => new[] { B(true, "Together, but not always settled"), B(true, "Competent as a unit, not yet tight"),
				B(true, "Kept together; never quite locked in"), B(size != null, $"A {size} that held together, just", "size") },
			_ => new[] { B(true, "Uneven as a group"), B(true, "Loose and often out of step"),
				B(f.Players >= 2, $"The {Phrase.Number(f.Players)} players rarely settled together", "size"), B(worst, "The playing was the weak spot") }
		};
		var clauses = new[] {
			B(!f.Solo && f.Instruments.Count >= 2, Phrase.List(f.Instruments) + (f.Room == HeardRoom.Unknown ? "" : " on stage")),
			B(!f.Solo && !f.Drums && f.Players >= 3, f.Room == HeardRoom.Unknown ? "no drummer" : "no drummer on the stand"),
			B(f.SittingIn > 0, $"with {Plural(f.SittingIn, "player", "players")} sitting in")
		};
		return Compose(key, heads, clauses);
	}

	// ---- material -------------------------------------------------------------------------------------------------
	private static string Material(ImpressionFacts f) {
		string key = f.Key + "|material";
		bool own = f.Originals > 0, covers = f.SetSongs > 0 && f.Originals == 0;
		bool best = Standout(f, f.Material, true), worst = Standout(f, f.Material, false);
		bool origTitle = own && f.OriginalTitle != null && f.FullCoverage, coverTitle = f.CoverTitle != null && f.FullCoverage;
		var heads = Band(f.Material) switch {
			2 => new[] {
				B(own, "Their own songs made a strong impression", "own"), B(origTitle, $"Originals like “{f.OriginalTitle}” stood out", "own"),
				B(own, "The writing stood out", "own"), B(best, "The songs were the best thing about them"),
				B(covers, "Strong songs, all of them borrowed", "covers"), B(covers, "Well-chosen material; nothing of their own", "covers"),
				B(covers && coverTitle, $"Good taste in covers, “{f.CoverTitle}” among them", "covers"),
				B(!own && !covers, "Songs made a strong impression")
			},
			1 => new[] {
				B(true, "Something to offer; hear more songs"), B(true, "Mixed material; worth hearing more"),
				B(own, "Their own songs held up without standing out", "own"), B(covers, "Familiar songs, competently picked", "covers")
			},
			_ => new[] {
				B(true, "Songs didn't convince"), B(own, "The originals didn't convince", "own"),
				B(covers, "A weak choice of covers", "covers"), B(worst, "The songs were the weak link")
			}
		};
		var clauses = new[] {
			B(f.FullCoverage && own && f.SetSongs > 0, $"{Phrase.Number(f.Originals)} of {Phrase.Number(f.SetSongs)} songs their own", "own"),
			B(f.FullCoverage && covers, $"all {Phrase.Number(f.SetSongs)} songs covers", "covers"),
			B(f.SetGenres.Count >= 2, f.SetGenres.Count >= 2 ? $"the set ran from {f.SetGenres[0]} to {f.SetGenres[^1]}" : null),
			B(!f.FullCoverage && f.SetSongs > 0, $"you caught part of a {Phrase.Number(f.SetSongs)}-song set")
		};
		return Compose(key, heads, clauses);
	}

	// ---- stage ----------------------------------------------------------------------------------------------------
	private static string Stage(ImpressionFacts f) {
		string key = f.Key + "|stage";
		float stayed = f.Stayed ?? -1; int requests = f.Requests ?? 0, arrivals = f.Arrivals ?? 0;
		bool crowd = f.Room != HeardRoom.TradeEvent;
		bool best = Standout(f, f.Communication, true), worst = Standout(f, f.Communication, false);
		var heads = Band(f.Communication) switch {
			2 => new[] {
				B(true, "Confident presence on stage"), B(true, "Easy, commanding presence"),
				B(crowd && stayed >= .8f, "Held the room from start to finish", "stayed"),
				B(crowd && requests > 0, "Had the floor calling out for songs", "requests"), B(best, "Stage presence was their strongest suit")
			},
			1 => new[] {
				B(true, "Put the songs across; presence varied"), B(true, "Pleasant on stage, rarely gripping"),
				B(true, "Got the set across without taking charge")
			},
			_ => new[] {
				B(true, "Struggled to put the set across"), B(crowd, "Little contact with the crowd"),
				B(crowd && stayed >= 0 && stayed < .55f, "Lost the room as the set went on", "stayed"), B(worst, "Presence was the weak spot")
			}
		};
		string staying = stayed >= .85f ? "most of the room stayed to the end" : stayed >= 0 && stayed < .4f ? "most of the room drifted off"
			: stayed >= 0 && stayed < .55f ? "about half the room drifted off" : null;
		var clauses = new[] {
			B(crowd && staying != null, staying, "stayed"),
			B(crowd && requests > 0, $"{Plural(requests, "call", "calls")} for songs by name", "requests"),
			B(crowd && arrivals > 0, $"{Phrase.Number(arrivals)} came in for this set"),
			B(crowd && f.Present is > 0 and <= 15, $"to a thin crowd of {Phrase.Number(f.Present ?? 0)}")
		};
		return Compose(key, heads, clauses);
	}

	// ---- the room's blind spot ------------------------------------------------------------------------------------
	/// <summary>A short note on what this kind of room hides on this axis (the long form is SceneRoomCharacter.Masks),
	/// or null when the room hides nothing there.</summary>
	public static string RoomNote(ImpressionFacts f, string axis) {
		string key = f.Key + "|room|" + axis;
		string[] options = (f.Room, axis) switch {
			(HeardRoom.Club, "Vocals") => new[] { "club noise covered the voice", "vocal detail lost in the club", "the club swallowed the voice" },
			(HeardRoom.Club, "Material") => new[] { "words lost under the club", "lyrics hard to follow over the club", "the club buried the words" },
			(HeardRoom.Roadhouse, "Vocals") => new[] { "the voice fought the bar", "bar noise over the voice" },
			(HeardRoom.Roadhouse, "Material") => new[] { "the songs competed with the bar", "bar noise over the songs" },
			(HeardRoom.CommunityHall, "Material") => new[] { "the hall's echo blurred the words", "words lost in the echo" },
			(HeardRoom.CommunityHall, "Playing") => new[] { "fine playing blurred in the echo", "the hall's echo smeared the detail" },
			// A small acoustic room hides a band's drive; one performer has no band drive to hide.
			(HeardRoom.Coffeehouse, "Playing") when !f.Solo => new[] { "an acoustic room hides a band's drive", "too small a room to show full drive" },
			(HeardRoom.ListeningRoom, "Stage") => new[] { "a seated room tests presence little", "a polite room doesn't test command" },
			(HeardRoom.TradeEvent, "Stage") => new[] { "no paying crowd to win over", "nobody in the room to win" },
			_ => null
		};
		return options == null ? null : Phrase.Pick(key, options);
	}
}
