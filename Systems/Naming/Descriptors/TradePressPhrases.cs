using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What a record card may say about one record. JournalisticDescriptor fills it from the runtime record, its
/// composition and its master metadata; null or zero means the fact is absent and nothing is said about it.</summary>
public sealed class RecordFacts {
	public string Key;
	public string Title, ArtistName, LabelName;
	public bool Band, Album;
	/// <summary>Position on the list being shown (Hot 100 unless ListName is set); 0 = not on it.</summary>
	public int Position;
	public string ListName;
	/// <summary>Hot 100 run facts (ignored when ListName is set).</summary>
	public bool Bullet, Anchor;
	public int WeeksOnChart, Peak, WeeksInTopTen;
	/// <summary>The record's own traits, 0-1.</summary>
	public float Hook, Production, Originality, Danceability;
	/// <summary>Releases by this act before this one.</summary>
	public int EarlierReleases;
	/// <summary>Origin of the song: a cover (with whose record it was, when known), a standard, a traditional song, or
	/// written by the act or by named professional writers.</summary>
	public bool Cover, Standard, Traditional, Adapted, WrittenByAct, CoWritten, ProfessionalSong;
	public string OriginalArtist;
	/// <summary>Named writers only (placeholder staff credits are dropped), and the song's publisher.</summary>
	public List<string> Writers = new();
	public string Publisher;
	/// <summary>Master taxonomy, enum names as strings ("TwelveBar", "ThreeFour", "Belted", "Narrative", "LiveInRoom").</summary>
	public string Form, Meter, VocalApproach, LyricMode, ProductionStyle;
	public bool Instrumental;
	public float? DurationSeconds;
	/// <summary>SongContentTags ids found in the title.</summary>
	public List<string> ContentTags = new();
	public float RadioHeat, WordOfMouth;
	public int RegionalBreakouts;
	public int UnitsThisWeek, UnitsLastWeek;
	public bool GrammyNominee, GrammyWinner;
	public string Genre, SecondGenre;
}

public sealed class ArtistFacts {
	public string Key, Name, Genre, SecondGenre, Origin, Base;
	public bool BaseInferred, Band, Singer;
	public int FormedYear;
	public string LabelName;
	public bool Signed;
	public int Charted, Top40, Top10, NumberOnes, Highest, WeeksOnChart, Releases;
	public int Members, FoundersRemaining, Departed;
	/// <summary>Parts the current members play, in plain words.</summary>
	public List<string> Parts = new();
}

public sealed class LabelFacts {
	public string Key, Name, City, Founder;
	public string Tier;
	public int FoundedYear, Roster, Releases, Top40, NumberOnes;
	public List<string> Genres = new();
	public float RiskTolerance;
}

/// <summary>
/// The trade-paper voice for chart cards, act files and label files. A blurb is an opening line plus a few sentences,
/// each drawn from a different family (the chart run, the sound, the song, who wrote it, the buzz...), and a family
/// only speaks when its fact is on the record. Every pick is keyed on the subject, so two records with the same
/// numbers usually read differently, and nothing here draws from a game stream.
/// </summary>
public static class TradePressPhrases {
	private static Phrase.Option W(bool when, string text) => Phrase.When(when, text);

	// ==== records ===================================================================================================
	public static string DescribeRecord(RecordFacts r, int extra = 3) {
		string k = r.Key ?? r.Title ?? "";
		var lines = new List<string>();
		string opening = RecordOpening(r, k);
		if (opening != null) lines.Add(opening);
		// An LP's master taxonomy and writer credits describe its lead song, not the album, so an LP gets no song-level
		// families (hook, shape, origin, title content).
		var families = r.Album
			? new List<string> { Run(r, k), Sound(r, k), Buzz(r, k), Sales(r, k), Awards(r, k) }
			: new List<string> { Run(r, k), Hook(r, k), Sound(r, k), Shape(r, k), Origin(r, k), Content(r, k), Buzz(r, k), Sales(r, k), Awards(r, k) };
		lines.AddRange(Phrase.Some(k + "|families", extra, families));
		return string.Join(" ", lines);
	}

	private static string RecordOpening(RecordFacts r, string k) {
		// An LP's position is on the LP chart, never the Hot 100.
		string listName = r.ListName ?? (r.Album ? "LP" : null);
		int p = r.Position; bool list = listName != null;
		string chart = list ? $"the {listName} {(r.ListName == null ? "chart" : "list")}" : null;
		string on = list ? " on " + chart : "";
		string kind = r.Album ? "album" : "record";
		if (p == 1) return Phrase.Pick(k + "|open",
			W(!list, $"The #1 {kind} in America."), W(list, $"The #1 {kind}{on}."),
			W(true, $"Number one{(list ? on : " in the country")} this week."), W(true, $"Sitting at the top of {chart ?? "the Hot 100"}."));
		if (p >= 2 && p <= 10) return Phrase.Pick(k + "|open",
			W(true, $"A top 10 smash{on}, currently at #{p}."), W(true, $"Up among the top ten{on} at #{p}."), W(true, $"#{p}{on} and one of the biggest {kind}s going."));
		if (p >= 11 && p <= 40) return Phrase.Pick(k + "|open",
			W(true, $"A solid hit{on} at #{p}."), W(true, $"Inside the Top 40{on} at #{p}."), W(true, $"#{p}{on}: a real hit, short of the top ten."));
		if (p > 40) return Phrase.Pick(k + "|open",
			W(true, $"Bubbling under{on} at #{p}."), W(true, $"Charting at #{p}{on}."), W(true, $"Lower down{(list ? on : " the Hot 100")} at #{p}."));
		return null;
	}

	private static string Run(RecordFacts r, string k) {
		if (r.ListName != null) return null;
		return Phrase.Pick(k + "|run",
			W(r.Bullet, "The fastest riser on the chart this week."), W(r.Bullet, "Carrying the bullet: nothing climbed faster this week."),
			W(r.Anchor, "Slipping fast down the charts."), W(r.Anchor, "The steepest faller on the chart this week."),
			W(r.WeeksOnChart >= 20, $"A perennial favorite, now in its {Phrase.Ordinal(r.WeeksOnChart)} week."),
			W(r.WeeksOnChart >= 20, $"{Phrase.Capitalize(Phrase.Number(r.WeeksOnChart))} weeks on the chart and still there."),
			W(r.WeeksOnChart >= 10 && r.WeeksOnChart < 20, "Showing real staying power."),
			W(r.WeeksOnChart >= 10 && r.WeeksOnChart < 20, $"{Phrase.Capitalize(Phrase.Number(r.WeeksOnChart))} weeks in and not going anywhere."),
			W(r.Peak > 0 && r.Peak <= 10 && r.Peak < r.Position, $"Previously peaked at #{r.Peak}."),
			W(r.Peak > 0 && r.Peak <= 10 && r.Peak < r.Position, $"Off its #{r.Peak} peak now."),
			W(r.WeeksInTopTen >= 4, $"{Phrase.Capitalize(Phrase.Number(r.WeeksInTopTen))} weeks in the top ten so far."));
	}

	private static string Hook(RecordFacts r, string k) => Phrase.Pick(k + "|hook",
		W(r.Hook > .8f, "An undeniable earworm."), W(r.Hook > .8f, "Try getting this one out of your head."),
		W(r.Hook > .8f, "The hook is absolutely killer."), W(r.Hook > .8f, "Catchier than a summer cold."),
		W(r.Hook > .8f && !r.Instrumental, "One listen and you're singing the chorus."),
		W(r.Hook < .3f, "More of a slow burn."), W(r.Hook < .3f, "Not exactly a sing-along."),
		W(r.Hook < .3f, "Rewards patient listening."), W(r.Hook < .3f, "The melody takes some getting used to."),
		W(r.Danceability > .78f, "Built for the dance floor."), W(r.Danceability > .78f, "Impossible to sit still through."),
		W(r.Danceability < .2f, "Not one for dancing."));

	private static string Sound(RecordFacts r, string k) => Phrase.Pick(k + "|sound",
		W(r.Production > .8f, "Immaculately produced."), W(r.Production > .8f, "Studio craftsmanship at its finest."),
		W(r.Production > .8f, "Every note in its perfect place."), W(r.Production > .8f, "Polished to a mirror shine."),
		W(r.Production < .3f, "Rough around the edges."), W(r.Production < .3f, "Has a certain raw charm."),
		W(r.Production < .3f, "The lo-fi sound is... intentional?"),
		W(r.Production < .3f && r.ProductionStyle == "LiveInRoom", "Cut live in the room, warts and all."),
		W(r.Production >= .3f && r.ProductionStyle == "LiveInRoom", "Cut live in the room, and it shows in the best way."),
		W(r.ProductionStyle == "Overdubbed", "Built up with overdubs."),
		W(r.ProductionStyle == "Layered", "Layer upon layer of sound."),
		W(r.ProductionStyle == "Constructed", "A studio construction from the ground up."),
		W(r.Originality > .8f, "Unlike anything else on the radio."), W(r.Originality > .8f, "Genuinely pushing boundaries."),
		W(r.Originality > .8f, "You've never heard anything quite like it."), W(r.Originality > .8f, "The sound of tomorrow, today."),
		W(r.Originality < .3f, "Familiar territory, competently executed."), W(r.Originality < .3f, "Critics might call it 'safe.'"),
		W(r.Originality < .3f, "Not exactly reinventing the wheel."),
		W(r.Originality < .3f && r.EarlierReleases > 0, "If you liked their last one, you'll like this."));

	private static string Shape(RecordFacts r, string k) {
		string dur = r.DurationSeconds is > 0 ? $"{(int)r.DurationSeconds.Value / 60}:{(int)r.DurationSeconds.Value % 60:00}" : null;
		return Phrase.Pick(k + "|shape",
			W(r.Instrumental, "An instrumental; no words needed."), W(r.Instrumental, "Not a word sung, and it doesn't need one."),
			W(r.Form == "TwelveBar", "Built on the twelve-bar."), W(r.Form == "TwelveBar", "A twelve-bar frame, played straight."),
			W(r.Form == "Strophic", "Verse after verse, no chorus to speak of."),
			W(r.Form == "ThroughComposed", "Never repeats a section the same way twice."),
			W(r.Meter == "ThreeFour", "In waltz time."), W(r.Meter == "SixEight", "Rolls along in six-eight."),
			W(r.Meter == "Shuffle", "A shuffle beat under the whole thing."), W(r.Meter == "Shifting", "The time signature won't sit still."),
			W(r.VocalApproach == "Crooned", "Crooned, not sung."), W(r.VocalApproach == "Belted", "Belted to the back row."),
			W(r.VocalApproach == "Testifying", "Sung like testimony."), W(r.VocalApproach == "Shouted", "Shouted more than sung."),
			W(r.VocalApproach == "HarmonyBlend", "Close harmony from start to finish."), W(r.VocalApproach == "Spoken", "Half of it is spoken."),
			W(r.VocalApproach == "Deadpan", "Delivered deadpan."), W(r.VocalApproach == "Conversational", "Sung like a conversation across a table."),
			W(r.LyricMode == "Narrative", "Tells a story start to finish."), W(r.LyricMode == "CharacterSketch", "A character sketch set to music."),
			W(r.LyricMode == "Confessional", "A confessional lyric."), W(r.LyricMode == "TopicalTestimony", "Has something to say about the times."),
			W(r.LyricMode == "Nonsense", "The words are joyful nonsense."), W(r.LyricMode == "CallAndResponse", "Call-and-response all the way."),
			W(r.LyricMode == "Dialogue", "Written as a dialogue."), W(r.LyricMode == "Reminiscence", "A look back at better days."),
			W(r.LyricMode == "RomanticAddress", "Sung straight to a sweetheart."), W(r.LyricMode == "InvectiveAccusation", "An accusing lyric: somebody did somebody wrong."),
			W(r.LyricMode == "DemandBoast", "All swagger."), W(r.LyricMode == "AdviceExhortation", "Hands out advice, whether you asked or not."),
			W(r.LyricMode == "SceneVignette", "Paints a scene and lets you stand in it."), W(r.LyricMode == "SurrealCatalogue", "A string of strange images."),
			W(dur != null && r.DurationSeconds < 150 && !r.Album, $"Over in {dur}."), W(dur != null && r.DurationSeconds > 200 && !r.Album, $"Runs a generous {dur}."));
	}

	private static string Origin(RecordFacts r, string k) {
		string writers = r.Writers.Count == 0 ? null : Phrase.List(r.Writers.Take(2).ToList());
		string selfWord = r.Band ? "themselves" : "it";
		return Phrase.Pick(k + "|origin",
			W(r.Cover && r.OriginalArtist != null, $"A cover of {r.OriginalArtist}'s record."),
			W(r.Cover && r.OriginalArtist != null, $"{r.OriginalArtist} had it first."),
			W(r.Cover && r.OriginalArtist != null, $"Their take on a song {r.OriginalArtist} recorded."),
			W(r.Standard, "A standard, freshly cut."), W(r.Standard, "An old standard in new clothes."),
			W(r.Traditional && !r.Adapted, "An old traditional tune."), W(r.Adapted, "A traditional song, rearranged."),
			W(r.WrittenByAct && !r.CoWritten, r.Band ? $"{r.ArtistName} wrote it themselves." : $"{r.ArtistName} wrote {selfWord}."),
			W(r.WrittenByAct && !r.CoWritten && writers != null, $"Written by {writers}."),
			W(r.CoWritten && writers != null, $"A co-write: {writers}."),
			W(r.ProfessionalSong && writers != null, $"From the pen of {writers}."),
			W(r.ProfessionalSong && writers != null, $"A {writers} song."),
			W(r.ProfessionalSong && r.Publisher != null, $"A song out of the {r.Publisher} catalogue."),
			W(r.ProfessionalSong && r.Publisher != null && writers == null, $"Placed by {r.Publisher}."));
	}

	private static string Content(RecordFacts r, string k) {
		bool T(string id) => r.ContentTags.Contains(id);
		return Phrase.Pick(k + "|content",
			W(T("alcohol"), "A drinking song, and the title says so."), W(T("alcohol"), "Wears its bar-room subject in the title."),
			W(T("drug-reference"), "The title flirts with a drug reference."),
			W(T("sexual-suggestion"), "Suggestive, starting with the title."),
			W(T("explicit-language"), "Rough language, right there in the title."),
			W(T("religious-provocation"), "The title takes liberties with the sacred."),
			W(T("political-protest"), "A protest song, and it says so up front."),
			W(T("anti-war"), "An anti-war song, plainly titled."),
			W(T("violence-crime"), "Trouble with the law is the subject."));
	}

	private static string Buzz(RecordFacts r, string k) => Phrase.Pick(k + "|buzz",
		W(r.RadioHeat > .6f, "All over the radio this week."), W(r.RadioHeat > .6f, "Disc jockeys can't leave it alone."),
		W(r.RadioHeat < .1f && r.Position > 0 && !r.Album, "Selling with almost no airplay."),
		W(r.WordOfMouth > .5f, "People are talking about it."), W(r.WordOfMouth > .5f, "Word of mouth is doing the selling."),
		W(r.RegionalBreakouts > 0, r.RegionalBreakouts == 1 ? "Broke out in one regional market first." : $"Broke out in {Phrase.Number(r.RegionalBreakouts)} regional markets."));

	private static string Sales(RecordFacts r, string k) {
		int now = r.UnitsThisWeek, before = r.UnitsLastWeek;
		if (now <= 0) return null;
		if (before <= 0) return Phrase.Pick(k + "|sales",
			W(now > 50000, "Explosive first week sales."), W(now > 50000, "Flying off the shelves from day one."),
			W(now > 10000 && now <= 50000, "A respectable opening week."), W(now > 10000 && now <= 50000, "Off to a promising start in the stores."),
			W(now <= 10000, "A quiet first week in the stores."));
		float change = (float)now / before;
		return Phrase.Pick(k + "|sales",
			W(change >= 2f, "Sales have doubled."), W(change >= 2f, "Can't press them fast enough."),
			W(change >= 1.5f && change < 2f, "Sales surging."), W(change >= 1.1f && change < 1.5f, "Store sales still building."),
			W(change >= .9f && change < 1.1f, "Store sales holding steady."), W(change >= .7f && change < .9f, "Sales starting to cool."), W(change >= .7f && change < .9f, "Store sales easing off."),
			W(change >= .5f && change < .7f, "Sales losing steam."), W(change < .5f, "Sales have fallen away."));
	}

	private static string Awards(RecordFacts r, string k) => Phrase.Pick(k + "|awards",
		W(r.GrammyWinner, "A Grammy winner."), W(r.GrammyNominee && !r.GrammyWinner, "Up for a Grammy."));

	// ==== chart movement ===========================================================================================
	public static string Movement(string key, int lastWeek, int current) {
		if (current <= 0) return "";
		if (lastWeek == 0) return current <= 10
			? Phrase.Pick(key, "Exploding onto the chart!", "A stunning debut!", "Where did THIS come from?", "The hottest new entry.", $"In at #{current}, straight into the top ten.")
			: Phrase.Pick(key, "New this week.", "Just entering the chart.", "A fresh face.", "Making its debut.", $"In at #{current}.");
		int m = lastWeek - current;
		if (m >= 20) return Phrase.Pick(key, "Absolutely rocketing up the chart!", "Unstoppable momentum!", "On a tear!", $"Up {m} places in a week!");
		if (m >= 10) return Phrase.Pick(key, "Flying up the chart.", "Major upward movement.", "Gaining serious steam.", $"Up {m} to #{current}.");
		if (m >= 3) return Phrase.Pick(key, "Climbing nicely.", "Solid gains this week.", "Moving on up.", $"Up {m} to #{current}.");
		if (m > 0) return Phrase.Pick(key, "Inching upward.", "Slight improvement.", "Modest gains.", $"Up {m} to #{current}.");
		if (m == 0) return Phrase.Pick(key, "Holding steady.", "No change this week.", "Staying put.", $"Holding at #{current}.");
		if (m >= -3) return Phrase.Pick(key, "Slight dip.", "Minor slide.", "Losing a little ground.", $"Down {-m} to #{current}.");
		if (m >= -10) return Phrase.Pick(key, "Slipping.", "On the way down.", "Losing altitude.", $"Down {-m} to #{current}.");
		return Phrase.Pick(key, "In freefall!", "Plummeting!", "The bottom is falling out!", $"Down {-m} places in a week.");
	}

	// ==== regions ===================================================================================================
	/// <summary>Where the record is known, from each region's awareness. Says nothing when the regions say nothing.</summary>
	public static string Regions(string key, IReadOnlyList<(string Region, float Awareness)> regions) {
		var known = regions.Where(r => r.Awareness > 0).OrderByDescending(r => r.Awareness).ToList();
		if (known.Count == 0) return "";
		var top = known[0]; var bottom = known[^1];
		int strong = known.Count(r => r.Awareness > .6f);
		string inTop = Phrase.InRegion(top.Region), inBottom = Phrase.InRegion(bottom.Region);
		bool even = known.Count >= 3 && top.Awareness - bottom.Awareness < .15f;
		return Phrase.Pick(key,
			W(top.Awareness > .6f && !even, $"Particularly strong {inTop}."), W(top.Awareness > .6f && !even, $"Listeners {inTop} have taken it to heart."),
			W(top.Awareness > .6f && !even, $"Best known {inTop}."),
			W(strong >= 3, $"Strong in {Phrase.Number(strong)} regions at once."),
			W(top.Awareness <= .6f && bottom.Awareness < .2f && known.Count >= 2, $"{Phrase.Capitalize(inBottom)}, it still hasn't caught on."),
			W(top.Awareness <= .6f && bottom.Awareness < .2f && known.Count >= 2, $"Nobody {inBottom} has noticed it yet."),
			W(even, $"Known about evenly across {Phrase.Number(known.Count)} regions."), W(even, "No region stands out; it travels everywhere alike."),
			W(even && top.Awareness < .3f, "Known a little everywhere, a lot nowhere."),
			W(known.Count == 1, $"So far only listeners {inTop} know it."))
			?? "";
	}

	// ==== artists ===================================================================================================
	public static string DescribeArtist(ArtistFacts a, int extra = 3) {
		string k = a.Key ?? a.Name ?? "";
		string an(string word) => !string.IsNullOrEmpty(word) && ("AEIOU".IndexOf(char.ToUpperInvariant(word[0])) >= 0 || word.StartsWith("R&B", StringComparison.Ordinal)) ? "an" : "a";
		string streak = a.SecondGenre != null && a.SecondGenre != a.Genre ? $" with {an(a.SecondGenre)} {a.SecondGenre} streak" : "";
		string from = a.Origin ?? "parts unknown";
		string what = a.Singer ? "singer" : "artist";
		var lines = new List<string> {
			a.Band
				? Phrase.Pick(k + "|open", $"{a.Name} are {an(a.Genre)} {a.Genre} act out of {from}{streak}.", $"Out of {from}, {a.Name} play {a.Genre}{streak}.")
				: Phrase.Pick(k + "|open", $"{a.Name} is {an(a.Genre)} {a.Genre} {what} from {from}{streak}.", $"From {from}, {a.Name} works in {a.Genre}{streak}.")
		};
		if (a.Base != null && a.Base != a.Origin) lines.Add($"Their working base is {a.Base}{(a.BaseInferred ? " (inferred)" : "")}.");
		lines.Add(Phrase.Pick(k + "|label",
			W(a.Signed && a.LabelName != null, $"Records for {a.LabelName}."), W(a.Signed && a.LabelName != null, $"Signed to {a.LabelName}."),
			W(!a.Signed, "Currently unsigned."), W(!a.Signed, "No label behind them right now.")) ?? "");
		lines.Add(Phrase.Pick(k + "|chart",
			W(a.Charted == 0 && a.Releases == 0, "Nothing released yet."),
			W(a.Charted == 0 && a.Releases > 0, $"{Phrase.Capitalize(Phrase.Number(a.Releases))} {(a.Releases == 1 ? "release" : "releases")}, none on the chart yet."),
			W(a.Charted == 0 && a.Releases > 0, "Still waiting on a first chart entry."),
			W(a.Charted == 1, "They've had one chart entry so far."), W(a.Charted == 1, "One record on the chart to their name."),
			W(a.Charted >= 2 && a.Charted < 5, $"They've placed {a.Charted} records on the Hot 100."),
			W(a.Charted >= 2 && a.Charted < 5, $"{Phrase.Capitalize(Phrase.Number(a.Charted))} chart entries so far."),
			W(a.Charted >= 5, $"A proven hitmaker with {a.Charted} charting records."),
			W(a.Charted >= 5, $"{a.Charted} chart entries and counting.")) ?? "");
		var families = new List<string> {
			Phrase.Pick(k + "|peak",
				W(a.NumberOnes > 1, $"They've topped the charts {Phrase.Number(a.NumberOnes)} times."),
				W(a.NumberOnes == 1, "They've reached the summit with a #1 hit."), W(a.NumberOnes == 1, "One #1 to their name."),
				W(a.NumberOnes == 0 && a.Highest > 0 && a.Highest <= 10, $"Their highest charting single reached #{a.Highest}."),
				W(a.NumberOnes == 0 && a.Top10 >= 2, $"{Phrase.Capitalize(Phrase.Number(a.Top10))} top ten hits, no #1 yet."),
				W(a.Top10 == 0 && a.Top40 >= 2, $"{Phrase.Capitalize(Phrase.Number(a.Top40))} Top 40 hits, none higher than #{a.Highest}.")),
			Phrase.Pick(k + "|lineup",
				W(a.Band && a.Members >= 2 && a.Parts.Count >= 2, $"A {Phrase.Number(a.Members)}-piece playing {Phrase.List(a.Parts)}."),
				W(a.Band && a.Members >= 2, $"{Phrase.Capitalize(Phrase.Number(a.Members))} members in the current lineup."),
				W(!a.Band && a.Parts.Count == 1, a.Parts.Count == 1 ? $"Plays {a.Parts[0]}." : null)),
			Phrase.Pick(k + "|history",
				W(a.FormedYear > 0 && a.Band, $"Together since {a.FormedYear}."), W(a.FormedYear > 0 && !a.Band, $"Working since {a.FormedYear}."),
				W(a.Band && a.Departed == 0 && a.FoundersRemaining == a.Members && a.Members > 1, "Still the original lineup."),
				W(a.Band && a.FoundersRemaining > 0 && a.FoundersRemaining < a.Members, a.FoundersRemaining == 1 ? "One founder remains." : $"{Phrase.Capitalize(Phrase.Number(a.FoundersRemaining))} of the founders remain."),
				W(a.Band && a.FoundersRemaining == 0 && a.Departed > 0, "None of the founders are left."),
				W(a.Departed >= 2, $"{Phrase.Capitalize(Phrase.Number(a.Departed))} former members have passed through.")),
			Phrase.Pick(k + "|weeks",
				W(a.WeeksOnChart >= 52, "More than a year's worth of chart weeks, all told."),
				W(a.WeeksOnChart >= 20 && a.WeeksOnChart < 52, $"{a.WeeksOnChart} weeks on the chart, all told."))
		};
		lines.AddRange(Phrase.Some(k + "|families", extra, families));
		return string.Join(" ", lines.Where(l => !string.IsNullOrEmpty(l)));
	}

	// ==== labels ====================================================================================================
	public static string DescribeLabel(LabelFacts l, int extra = 2) {
		string k = l.Key ?? l.Name ?? "";
		var lines = new List<string> { $"{l.Name}." };
		if (l.Genres.Count == 1) lines.Add($"Known primarily for {l.Genres[0]}.");
		else if (l.Genres.Count is > 1 and <= 3) lines.Add($"Specializing in {Phrase.List(l.Genres)}.");
		else if (l.Genres.Count > 3) lines.Add("A diverse roster spanning multiple genres.");
		lines.Add(Phrase.Pick(k + "|tier",
			W(l.Tier == "Major", "One of the majors."), W(l.Tier == "Major", "A major label, with a major's reach."),
			W(l.Tier == "MidTier", "A leading independent, big enough to trade blows with the majors."), W(l.Tier == "MidTier", "A serious contender just below the majors."),
			W(l.Tier == "Independent", "An established independent."), W(l.Tier == "Independent", "Independent, and settled in."),
			W(l.Tier == "Small", "A small label working its home territory."), W(l.Tier == "Small", "Small, and hungry."),
			W(l.Tier == "Boutique", "A specialist outfit."), W(l.Tier == "Boutique", "A boutique label with a narrow focus.")) ?? "");
		var families = new List<string> {
			Phrase.Pick(k + "|founded",
				W(l.FoundedYear > 0 && l.City != null, $"Operating out of {l.City} since {l.FoundedYear}."),
				W(l.FoundedYear > 0, $"Founded in {l.FoundedYear}{(l.Founder != null ? " by " + l.Founder : "")}.")),
			Phrase.Pick(k + "|roster",
				W(l.Roster >= 2, $"{Phrase.Capitalize(Phrase.Number(l.Roster))} acts on the roster."), W(l.Roster == 1, "A single act on the roster.")),
			Phrase.Pick(k + "|hits",
				W(l.NumberOnes > 0, $"{Phrase.Capitalize(Phrase.Number(l.NumberOnes))} #1 {(l.NumberOnes == 1 ? "record" : "records")} to its name."),
				W(l.Top40 > 0, $"{Phrase.Capitalize(Phrase.Number(l.Top40))} Top 40 {(l.Top40 == 1 ? "hit" : "hits")} so far."),
				W(l.Top40 == 0 && l.Releases > 0, $"{Phrase.Capitalize(Phrase.Number(l.Releases))} {(l.Releases == 1 ? "release" : "releases")}, no Top 40 hit yet.")),
			Phrase.Pick(k + "|risk",
				W(l.RiskTolerance > .7f, "Known for taking chances on unconventional acts."), W(l.RiskTolerance > .7f, "Willing to gamble on a strange record."),
				W(l.RiskTolerance < .3f, "They play it safe with proven formulas."), W(l.RiskTolerance < .3f, "Sticks to what has sold before."))
		};
		lines.AddRange(Phrase.Some(k + "|families", extra, families));
		return string.Join(" ", lines.Where(s => !string.IsNullOrEmpty(s)));
	}
}
