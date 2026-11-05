using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Census of the fact-gated descriptor wording. For thousands of synthetic heard sets and records it checks every
/// gated phrase against the fact it names (a violation is a phrase printed without its state), then measures how often
/// two subjects with the same impression read word-for-word alike, against the old fixed three-line wording.
/// Exit code 1 on any gate violation.
/// </summary>
public static class Program {
	private static readonly string[] Names = { "Dale", "Ruth", "Earl", "Nell", "Gus", "Opal", "Floyd", "June" };
	private static readonly string[] Parts = { "guitar", "bass", "drums", "piano", "organ", "sax", "harmonica", "violin" };
	private static readonly string[] Genres = { "Country", "R&B", "Folk", "Rockabilly", "Gospel", "Jazz" };
	private static int violations;
	private static readonly Dictionary<string, int> violationKinds = new();

	private static void Gate(bool ok, string kind, string text) {
		if (ok) return;
		violations++;
		violationKinds[kind] = violationKinds.TryGetValue(kind, out int n) ? n + 1 : 1;
		if (violationKinds[kind] <= 2) Console.WriteLine($"  VIOLATION [{kind}]: {text}");
	}

	public static int Main(string[] args) {
		int samples = args.Length > 0 ? int.Parse(args[0]) : 4000;
		var rng = new Random(1960);
		Console.WriteLine($"Descriptor census, {samples} synthetic subjects each\n");
		Scouting(rng, samples);
		Records(rng, samples);
		Artists(rng, samples);
		Console.WriteLine(violations == 0 ? "\nAll gates held." : $"\n{violations} gate violations: " + string.Join(", ", violationKinds.Select(k => $"{k.Key} {k.Value}")));
		return violations == 0 ? 0 : 1;
	}

	// ---- scouting ---------------------------------------------------------------------------------------------------
	private static ImpressionFacts RandomSet(Random rng, int i) {
		bool solo = rng.NextDouble() < .35;
		int players = solo ? 1 : rng.Next(2, 7);
		var parts = Parts.OrderBy(_ => rng.Next()).Take(solo ? 1 : Math.Min(players, rng.Next(1, 5))).ToList();
		int leads = rng.NextDouble() < .1 ? 0 : rng.NextDouble() < .8 ? 1 : 2;
		int songs = rng.Next(0, 8), originals = songs == 0 ? 0 : rng.NextDouble() < .5 ? 0 : rng.Next(1, songs + 1);
		bool audience = rng.NextDouble() < .8;
		return new ImpressionFacts {
			Key = $"act{i}|obs{i}",
			Vocals = leads == 0 ? null : (float)rng.NextDouble(), Execution = (float)rng.NextDouble(),
			Material = (float)rng.NextDouble(), Communication = (float)rng.NextDouble(),
			Solo = solo, Players = players, SittingIn = rng.NextDouble() < .2 ? rng.Next(1, 3) : 0,
			LeadSinger = leads == 1 ? Names[rng.Next(Names.Length)] : null, LeadVoices = leads,
			BackingVoices = !solo && rng.NextDouble() < .3 ? rng.Next(1, 3) : 0,
			Instruments = parts, Drums = parts.Contains("drums"), SoloInstrument = solo ? parts[0] : null,
			SetSongs = songs, Originals = originals,
			OriginalTitle = originals > 0 ? "Midnight Train" : null, CoverTitle = songs > originals ? "Silver Moon" : null,
			SetGenres = songs == 0 ? new List<string>() : Genres.OrderBy(_ => rng.Next()).Take(rng.Next(1, 3)).ToList(),
			FullCoverage = rng.NextDouble() < .8, Room = (HeardRoom)rng.Next(0, 7),
			Present = audience ? rng.Next(5, 120) : null, Arrivals = audience ? rng.Next(0, 12) : null,
			Requests = audience ? (rng.NextDouble() < .3 ? rng.Next(1, 4) : 0) : null, Stayed = audience ? (float)rng.NextDouble() : null
		};
	}

	private static string OldRead(string axis, ImpressionFacts f) {
		string Brief(float v, string low, string mid, string high) => v >= .72f ? high : v >= .48f ? mid : low;
		bool masked = ScoutingImpressionPhrases.RoomNote(f, axis == "Stage presence" ? "Stage" : axis) != null;
		string read = axis switch {
			"Vocals" => f.Vocals.HasValue ? Brief(f.Vocals.Value, "Uncertain singing", "Carried the songs; not always convincing", "Confident and convincing singing") : "No lead vocals heard",
			"Playing" => f.Solo ? Brief(f.Execution, "Uneven playing", "Capable, but not always settled", "Assured, sure-handed playing") : Brief(f.Execution, "Uneven as a group", "Together, but not always settled", "Assured playing together"),
			"Material" => Brief(f.Material, "Songs didn't convince", "Something to offer; hear more songs", "Songs made a strong impression"),
			_ => Brief(f.Communication, "Struggled to put the set across", "Put the songs across; presence varied", "Confident presence on stage")
		};
		return read + (masked ? " · room limits detail" : "");
	}

	private static void Scouting(Random rng, int samples) {
		Console.WriteLine("SCOUTING IMPRESSIONS");
		var sets = Enumerable.Range(0, samples).Select(i => RandomSet(rng, i)).ToList();
		var byAxisNew = new Dictionary<string, List<string>>(); var byAxisOld = new Dictionary<string, List<string>>();
		foreach (var f in sets) {
			foreach (var a in ScoutingImpressionPhrases.Describe(f)) {
				string full = a.Read + (a.RoomNote != null ? " · " + a.RoomNote : "");
				(byAxisNew.TryGetValue(a.Axis, out var l) ? l : byAxisNew[a.Axis] = new()).Add(full);
				(byAxisOld.TryGetValue(a.Axis, out var o) ? o : byAxisOld[a.Axis] = new()).Add(OldRead(a.Axis, f));
				CheckImpression(f, a);
			}
		}
		Console.WriteLine($"  {"axis",-16}{"old distinct",14}{"new distinct",14}{"old P(same)",13}{"new P(same)",13}");
		foreach (var axis in byAxisNew.Keys)
			Console.WriteLine($"  {axis,-16}{byAxisOld[axis].Distinct().Count(),14}{byAxisNew[axis].Distinct().Count(),14}{Collision(byAxisOld[axis]),13:P1}{Collision(byAxisNew[axis]),13:P1}");
		// The playtest complaint: two acts in, the lines repeat. Probability that two acts share at least one identical line.
		double shareOld = 0, shareNew = 0; int pairs = 0;
		for (int i = 0; i + 1 < sets.Count; i += 2, pairs++) {
			var a = ScoutingImpressionPhrases.Describe(sets[i]); var b = ScoutingImpressionPhrases.Describe(sets[i + 1]);
			if (a.Zip(b).Any(p => p.First.Read == p.Second.Read && p.First.RoomNote == p.Second.RoomNote)) shareNew++;
			if (a.Select(x => OldRead(x.Axis, sets[i])).Zip(b.Select(x => OldRead(x.Axis, sets[i + 1]))).Any(p => p.First == p.Second)) shareOld++;
		}
		Console.WriteLine($"  two acts share an identical line: old {shareOld / pairs:P1}, new {shareNew / pairs:P1}");
		Console.WriteLine("  samples:");
		foreach (var f in sets.Take(3)) {
			Console.WriteLine($"    [{(f.Solo ? "solo" : f.Players + " players")}, {f.Room}, {f.Originals}/{f.SetSongs} originals, stayed {f.Stayed:P0}, requests {f.Requests}]");
			foreach (var a in ScoutingImpressionPhrases.Describe(f)) Console.WriteLine($"      {a.Axis,-15} {Phrase.Capitalize(a.Read)}{(a.RoomNote != null ? " · " + a.RoomNote : "")}");
		}
		Console.WriteLine();
	}

	/// <summary>Probability that two random subjects with the same impression band read identically.</summary>
	private static double Collision(List<string> lines) {
		var counts = lines.GroupBy(l => l).Select(g => (double)g.Count()).ToList();
		double n = lines.Count;
		return counts.Sum(c => c * (c - 1)) / (n * (n - 1));
	}

	private static void CheckImpression(ImpressionFacts f, AxisImpression a) {
		string t = a.Read;
		Gate(!t.Contains("calling out for songs") && !t.Contains("for songs by name") || f.Requests > 0, "requests", t);
		Gate(!t.Contains("sitting in") || f.SittingIn > 0, "sitting-in", t);
		Gate(!t.Contains("no drummer") || (!f.Drums && !f.Solo), "drummer", t);
		Gate(!t.Contains("songs their own") || (f.Originals > 0 && f.FullCoverage), "originals", t);
		Gate(!(t.Contains("covers") || t.Contains("borrowed") || t.Contains("nothing of their own") || t.Contains("competently picked")) || (f.Originals == 0 && f.SetSongs > 0), "covers", t);
		Gate(!(t.Contains("Their own songs") || t.Contains("originals") || t.Contains("Originals") || t.Contains("The writing")) || f.Originals > 0, "own-songs", t);
		Gate(!Names.Any(n => t.Contains(n)) || (f.LeadSinger != null && t.Contains(f.LeadSinger)), "lead-name", t);
		Gate(!(t.Contains("stayed to the end") || t.Contains("Held the room")) || f.Stayed >= .8f, "stayed", t);
		Gate(!(t.Contains("drifted off") || t.Contains("Lost the room")) || f.Stayed < .55f, "drifted", t);
		Gate(!t.Contains("backing voice") || f.BackingVoices > 0, "backing-voices", t);
		Gate(!t.Contains("came in for this set") || f.Arrivals > 0, "arrivals", t);
		Gate(!t.Contains("caught part of") || !f.FullCoverage, "coverage", t);
		Gate(!t.Contains("set ran from") || f.SetGenres.Count >= 2, "genres", t);
		Gate(!t.Contains("-piece") || t.Contains(Phrase.Number(f.Players) + "-piece"), "size", t);
		Gate(!(t.Contains("crowd") || t.Contains("the room") || t.Contains("the floor")) || f.Room != HeardRoom.TradeEvent, "trade-crowd", t);
		Gate(!t.Contains("lead passed between") && !t.Contains("ways;") && !t.Contains("traded well") || f.LeadVoices >= 2, "shared-lead", t);
		Gate(a.Axis != "Vocals" || f.Vocals.HasValue || t.Contains("o lead") || t.Contains("Nobody sang lead"), "no-vocals", t);
		Gate(a.RoomNote == null || f.Room is not (HeardRoom.Unknown), "room-note", a.RoomNote ?? "");
		Gate(!t.Contains("thin crowd") || f.Present <= 15, "thin-crowd", t);
		Gate(t.Length > 0, "empty", a.Axis);
	}

	// ---- records ----------------------------------------------------------------------------------------------------
	private static readonly string[] Forms = { null, "TwelveBar", "Aaba", "VerseChorus", "Strophic", "ThroughComposed" };
	private static readonly string[] Meters = { null, "ThreeFour", "SixEight", "Shuffle", "Shifting" };
	private static readonly string[] Vocals = { null, "Crooned", "Belted", "Testifying", "Shouted", "HarmonyBlend", "Spoken", "Deadpan", "Conversational" };
	private static readonly string[] Lyrics = { null, "Narrative", "Confessional", "RomanticAddress", "Nonsense", "CallAndResponse", "Reminiscence" };
	private static readonly string[] Productions = { null, "LiveInRoom", "Overdubbed", "Layered", "Constructed" };
	private static readonly string[] Tags = { "alcohol", "drug-reference", "sexual-suggestion", "anti-war", "political-protest", "violence-crime" };

	private static RecordFacts RandomRecord(Random rng, int i) {
		int pos = rng.Next(0, 101); int origin = rng.Next(0, 6);
		return new RecordFacts {
			Key = $"rec{i}|{rng.Next(0, 20)}", Title = "Silver Moon", ArtistName = "The Drifters Five", Band = rng.NextDouble() < .6,
			Album = rng.NextDouble() < .15, Position = pos, Bullet = pos > 0 && rng.NextDouble() < .08, Anchor = pos > 0 && rng.NextDouble() < .06,
			WeeksOnChart = pos > 0 ? rng.Next(1, 30) : 0, Peak = pos > 0 ? Math.Max(1, pos - rng.Next(0, 40)) : 0, WeeksInTopTen = pos <= 10 && pos > 0 ? rng.Next(0, 8) : 0,
			Hook = (float)rng.NextDouble(), Production = (float)rng.NextDouble(), Originality = (float)rng.NextDouble(), Danceability = (float)rng.NextDouble(),
			EarlierReleases = rng.Next(0, 4),
			Cover = origin == 0, OriginalArtist = origin == 0 && rng.NextDouble() < .7 ? "Hank Lowe" : null, Standard = origin == 1, Traditional = origin == 2,
			Adapted = origin == 2 && rng.NextDouble() < .5, WrittenByAct = origin is 3 or 4, CoWritten = origin == 4, ProfessionalSong = origin == 5,
			Writers = origin >= 3 && rng.NextDouble() < .8 ? new List<string> { "Ada Marsh", "Cal Booker" }.Take(rng.Next(1, 3)).ToList() : new List<string>(),
			Form = Forms[rng.Next(Forms.Length)], Meter = Meters[rng.Next(Meters.Length)], VocalApproach = Vocals[rng.Next(Vocals.Length)],
			LyricMode = Lyrics[rng.Next(Lyrics.Length)], ProductionStyle = Productions[rng.Next(Productions.Length)],
			Instrumental = rng.NextDouble() < .05, DurationSeconds = rng.NextDouble() < .7 ? (float)rng.Next(110, 260) : null,
			ContentTags = rng.NextDouble() < .06 ? new List<string> { Tags[rng.Next(Tags.Length)] } : new List<string>(),
			RadioHeat = (float)rng.NextDouble(), WordOfMouth = (float)rng.NextDouble(), RegionalBreakouts = rng.NextDouble() < .2 ? rng.Next(1, 4) : 0,
			UnitsThisWeek = rng.Next(0, 80000), UnitsLastWeek = rng.NextDouble() < .2 ? 0 : rng.Next(1, 60000),
			GrammyNominee = rng.NextDouble() < .02, GrammyWinner = false
		};
	}

	private static string OldRecord(RecordFacts r) {
		var d = new List<string>();
		if (r.Position == 1) d.Add("The #1 record in America."); else if (r.Position is > 0 and <= 10) d.Add($"A top 10 smash, currently at #{r.Position}.");
		else if (r.Position is > 10 and <= 40) d.Add($"A solid hit at #{r.Position}."); else if (r.Position > 0) d.Add($"Bubbling under at #{r.Position}.");
		if (r.Hook > .8f) d.Add("hook+"); else if (r.Hook < .3f) d.Add("hook-");
		if (r.Production > .8f) d.Add("prod+"); else if (r.Production < .3f) d.Add("prod-");
		if (r.Originality > .8f) d.Add("orig+"); else if (r.Originality < .3f) d.Add("orig-");
		return string.Join(" ", d);
	}

	private static void Records(Random rng, int samples) {
		Console.WriteLine("RECORD BLURBS");
		var recs = Enumerable.Range(0, samples).Select(i => RandomRecord(rng, i)).ToList();
		var texts = recs.Select(r => TradePressPhrases.DescribeRecord(r)).ToList();
		for (int i = 0; i < recs.Count; i++) CheckRecord(recs[i], texts[i]);
		// Sentences, not whole blurbs: how often does a given body sentence recur across the census?
		var sentences = texts.SelectMany(t => t.Split(". ", StringSplitOptions.RemoveEmptyEntries).Skip(1)).ToList();
		Console.WriteLine($"  distinct body sentences: {sentences.Distinct().Count()} (old pools: 24 fixed lines + 4 controversy lines)");
		Console.WriteLine($"  most common body sentence covers {sentences.GroupBy(s => s).Max(g => g.Count()) / (double)texts.Count:P1} of blurbs");
		Console.WriteLine($"  blurbs with no body sentence: old {recs.Count(r => OldRecord(r).Split(' ').Length <= 6 && !OldRecord(r).Contains('+') && !OldRecord(r).Contains('-')) / (double)recs.Count:P1}, new {texts.Count(t => t.Count(c => c == '.') <= 1) / (double)texts.Count:P1}");
		Console.WriteLine("  samples:");
		foreach (var t in texts.Take(5)) Console.WriteLine("    " + t);
		Console.WriteLine();
	}

	private static void CheckRecord(RecordFacts r, string t) {
		Gate(!(t.Contains("cover of") || t.Contains("had it first") || t.Contains("take on a song")) || (r.Cover && r.OriginalArtist != null), "rec-cover", t);
		Gate(!t.Contains("wrote it") || r.WrittenByAct, "rec-selfwritten", t);
		Gate(!(t.Contains("From the pen") || t.Contains("Ada Marsh song") || t.Contains("Cal Booker song")) || r.ProfessionalSong, "rec-pro", t);
		Gate(!t.Contains("standard") || r.Standard, "rec-standard", t);
		Gate(!t.Contains("traditional") || r.Traditional, "rec-traditional", t);
		Gate(!(t.Contains("drinking song") || t.Contains("bar-room")) || r.ContentTags.Contains("alcohol"), "rec-alcohol", t);
		Gate(!t.Contains("drug reference") || r.ContentTags.Contains("drug-reference"), "rec-drug", t);
		Gate(!t.Contains("anti-war") || r.ContentTags.Contains("anti-war"), "rec-antiwar", t);
		Gate(!t.Contains("Banned"), "rec-banned", t);
		Gate(!(t.Contains("radio this week") || t.Contains("Disc jockeys")) || r.RadioHeat > .6f, "rec-radio", t);
		Gate(!t.Contains("almost no airplay") || r.RadioHeat < .1f, "rec-noairplay", t);
		Gate(!t.Contains("Grammy") || r.GrammyNominee || r.GrammyWinner, "rec-grammy", t);
		Gate(!(t.Contains("Over in") || t.Contains("generous")) || r.DurationSeconds.HasValue, "rec-duration", t);
		Gate(!t.Contains("waltz") || r.Meter == "ThreeFour", "rec-waltz", t);
		Gate(!t.Contains("twelve-bar") || r.Form == "TwelveBar", "rec-12bar", t);
		Gate(!r.Album || !(t.Contains("wrote it") || t.Contains("Crooned") || t.Contains("earworm") || t.Contains("twelve-bar")), "rec-album-song-lines", t);
		Gate(!t.Contains("last one") || r.EarlierReleases > 0, "rec-lastone", t);
		Gate(!(t.Contains("fastest riser") || t.Contains("bullet")) || r.Bullet, "rec-bullet", t);
		Gate(!(t.Contains("Slipping fast") || t.Contains("steepest faller")) || r.Anchor, "rec-anchor", t);
		Gate(!t.Contains("instrumental") && !t.Contains("Not a word sung") || r.Instrumental, "rec-instrumental", t);
		Gate(!t.Contains("singing the chorus") || !r.Instrumental, "rec-chorus-instrumental", t);
		Gate(!t.Contains("regional market") || r.RegionalBreakouts > 0, "rec-breakout", t);
		Gate(!t.Contains("first week") && !t.Contains("opening week") || r.UnitsLastWeek == 0, "rec-firstweek", t);
		Gate(!t.Contains("live in the room") || r.ProductionStyle == "LiveInRoom", "rec-live", t);
		Gate(r.Position > 0 || !t.Contains("#0"), "rec-position-zero", t);
	}

	// ---- artists ----------------------------------------------------------------------------------------------------
	private static void Artists(Random rng, int samples) {
		Console.WriteLine("ACT FILES");
		var texts = new List<string>();
		for (int i = 0; i < samples; i++) {
			bool band = rng.NextDouble() < .6; int members = band ? rng.Next(2, 6) : 1; int founders = band ? rng.Next(0, members + 1) : 1;
			int charted = rng.NextDouble() < .5 ? 0 : rng.Next(1, 12);
			var a = new ArtistFacts {
				Key = "act" + i, Name = band ? "The Drifters Five" : "Ruth Haskins", Genre = Genres[rng.Next(Genres.Length)],
				SecondGenre = rng.NextDouble() < .3 ? Genres[rng.Next(Genres.Length)] : null, Origin = rng.NextDouble() < .9 ? "Memphis" : null,
				Band = band, Singer = rng.NextDouble() < .85, FormedYear = rng.NextDouble() < .8 ? rng.Next(1950, 1961) : 0,
				Signed = rng.NextDouble() < .6, LabelName = "Sunbelt Records", Charted = charted, Top40 = Math.Min(charted, rng.Next(0, 6)),
				Top10 = 0, NumberOnes = charted > 3 && rng.NextDouble() < .2 ? 1 : 0, Highest = charted > 0 ? rng.Next(1, 100) : 0,
				WeeksOnChart = charted * rng.Next(0, 12), Releases = charted + rng.Next(0, 6), Members = members, FoundersRemaining = founders,
				Departed = band ? rng.Next(0, 3) : 0, Parts = Parts.OrderBy(_ => rng.Next()).Take(band ? rng.Next(0, 4) : rng.Next(0, 2)).ToList()
			};
			string t = TradePressPhrases.DescribeArtist(a); texts.Add(t);
			Gate(!t.Contains("original lineup") || (a.Departed == 0 && a.FoundersRemaining == a.Members), "act-original", t);
			Gate(!t.Contains("Signed to") && !t.Contains("Records for") || a.Signed, "act-signed", t);
			Gate(!t.Contains("unsigned") && !t.Contains("No label") || !a.Signed, "act-unsigned", t);
			Gate(!System.Text.RegularExpressions.Regex.IsMatch(t, @"#1") || a.NumberOnes > 0, "act-no1", t);
			Gate(!t.Contains("Together since") && !t.Contains("Working since") || a.FormedYear > 0, "act-formed", t);
			Gate(!t.Contains("industry is watching"), "act-watching", t);
		}
		var sentences = texts.SelectMany(t => t.Split(". ", StringSplitOptions.RemoveEmptyEntries)).ToList();
		Console.WriteLine($"  distinct sentences: {sentences.Distinct().Count()}; identical whole files: {texts.Count - texts.Distinct().Count()} of {texts.Count}");
		foreach (var t in texts.Take(3)) Console.WriteLine("    " + t);
	}
}
