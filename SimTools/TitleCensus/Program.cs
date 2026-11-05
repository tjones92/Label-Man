// SimTools/TitleCensus/Program.cs
// Generates a decade's worth of song and album titles through TitleNaming -- the class the game's
// NameGenerator delegates to -- with the genre mix of a real decade run, and reports:
//   * reuse: how many titles repeat across the world, the worst offenders, album-title space per genre;
//   * shape: the same title features measured on the real 1960-64 Billboard Hot 100 (parentheticals,
//     questions, length, "Love", the pronoun-verb-pronoun frame...);
//   * lint: the grammatical failures that make a title read as nonsense ("You Sing Me", "Mercy Down
//     the Redemption", "Songs of Shack", "The Calloways Sings", "Blue Jeanses", "Sprin'", "Meet the
//     Calloways?"), each with examples;
//   * real titles: generated titles that are exactly a real 1960-64 hit.
// Usage: TitleCensus [songs=4300] [albums=1500] [seed=1001] [--legacy] [--samples] [--hist path]
//   --legacy turns the grammar layer and world-wide title uniqueness off (the pre-census engine).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LabelMan.Naming;

public static class Program {
	// Genre mix of the seed-2002 decade run (records by genre), so the census weights genres as the game does.
	static readonly (Genre g, double w)[] Mix = {
		(Genre.TraditionalPop, 618), (Genre.TeenPop, 576), (Genre.Country, 547), (Genre.RockAndRoll, 511), (Genre.RnB, 507),
		(Genre.Soul, 309), (Genre.EasyListening, 227), (Genre.Folk, 168), (Genre.Jazz, 130), (Genre.DooWop, 122),
		(Genre.Blues, 99), (Genre.BossaNova, 66), (Genre.SurfRock, 63), (Genre.BritishPop, 61), (Genre.ContemporaryFolk, 48),
		(Genre.Gospel, 46), (Genre.BritishBeat, 39), (Genre.LatinPop, 30), (Genre.TexMex, 18), (Genre.Comedy, 16),
		(Genre.GarageRock, 11), (Genre.Childrens, 9), (Genre.Classical, 6), (Genre.FolkRock, 5), (Genre.SunshinePop, 2),
		// late-decade genres the seed run barely reached; weighted in so their sets are measured at all
		(Genre.PsychedelicRock, 40), (Genre.HardRock, 15), (Genre.Bubblegum, 15), (Genre.Motown, 40), (Genre.GirlGroup, 40) };

	public static int Main(string[] args) {
		var pos = args.Where(a => !a.StartsWith("--")).ToList();
		int nSongs = pos.Count > 0 && int.TryParse(pos[0], out var a) ? a : 4300;
		int nAlbums = pos.Count > 1 && int.TryParse(pos[1], out var b) ? b : 1500;
		ulong seed = pos.Count > 2 && ulong.TryParse(pos[2], out var c) ? c : 1001UL;
		bool legacy = args.Contains("--legacy"), samples = args.Contains("--samples");
		int hi = Array.IndexOf(args, "--hist");
		string histPath = hi >= 0 && hi + 1 < args.Length ? args[hi + 1] : FindUp(Path.Combine("SimLogs", "historical-hot100-1960-64", "hot100_weekly.csv"));

		string dir = FindUp(Path.Combine("Data", "Naming", "grammar.json"));
		dir = dir == null ? Path.Combine(Directory.GetCurrentDirectory(), "Data", "Naming") : Path.GetDirectoryName(dir);
		TitleSelection.Enabled = !legacy;
		var acts = ActNaming.Load(f => { string p = Path.Combine(dir, f); return File.Exists(p) ? File.ReadAllText(p) : null; }, out _);
		if (acts == null) { Console.Error.WriteLine("could not load " + dir); return 2; }
		var titles = new TitleNaming(acts) { WorldUniqueness = !legacy && !args.Contains("--no-world") };
		var lex = acts.Engine.Lexicon;

		var titleRng = new DeterministicRandom(seed ^ 0x9E3779B97F4A7C15UL);
		var albumRng = new DeterministicRandom(seed ^ 0x616C62756D74746CUL);
		var actRng = new DeterministicRandom(seed ^ 0x6163746E616D6573UL);
		var popRng = new DeterministicRandom(seed);
		// --genres A,B,... measures only those genres, equally weighted (small sets are a rounding error in the decade mix)
		int gi = Array.IndexOf(args, "--genres");
		var mix = gi >= 0 && gi + 1 < args.Length
			? args[gi + 1].Split(',').Select(s => (g: Enum.Parse<Genre>(s.Trim(), true), w: 1.0)).ToArray()
			: Mix;
		double wsum = mix.Sum(x => x.w);
		Genre PickGenre() { double r = popRng.NextDouble() * wsum; foreach (var x in mix) if ((r -= x.w) < 0) return x.g; return mix[^1].g; }

		// ---- songs: ~5 per act, a decade of years
		// --catalog draws through CatalogTitle (the pre-game standards a player browses in TEACH A COVER), 1959.
		bool catalog = args.Contains("--catalog");
		var songs = new List<(Genre g, int year, string t)>();
		var t0 = DateTime.UtcNow;
		for (int i = 0; i < nSongs; i++) {
			var g = PickGenre();
			int year = catalog ? 1959 : 1960 + i * 10 / Math.Max(1, nSongs);
			songs.Add((g, year, catalog ? titles.CatalogTitle(g, year, titleRng) : titles.SongTitle(g, year, "act" + (i / 5), titleRng)));
		}
		// ---- albums: a real act name, its lead single, the album title
		var albums = new List<(Genre g, string act, string single, string t)>();
		for (int i = 0; i < nAlbums; i++) {
			var g = PickGenre();
			int year = 1960 + i * 10 / Math.Max(1, nAlbums);
			var type = popRng.Chance(0.45) ? (popRng.Chance(0.75) ? ArtistType.SoloMale : ArtistType.SoloFemale) : ArtistType.Band;
			string act = acts.GenerateActName(g, year, type, null, actRng);
			string single = titles.SongTitle(g, year, "lp" + i, titleRng);
			albums.Add((g, act, single, titles.AlbumTitle(g, year, act, false, single, "lp" + i, albumRng)));
		}
		double secs = (DateTime.UtcNow - t0).TotalSeconds;

		var sb = new StringBuilder();
		void P(string s = "") { sb.AppendLine(s); Console.WriteLine(s); }
		P($"title census  seed {seed}  {(legacy ? "LEGACY (grammar layer + world uniqueness off)" : "current")}  {nSongs} songs, {nAlbums} albums  ({secs:F1}s)");

		// ---- reuse
		P(); P("== reuse");
		var songCounts = songs.GroupBy(s => Norm(s.t)).Select(gr => (t: gr.First().t, n: gr.Count())).OrderByDescending(x => x.n).ToList();
		int reused = songCounts.Where(x => x.n > 1).Sum(x => x.n);
		P($"songs: {songCounts.Count} distinct of {nSongs} ({100.0 * songCounts.Count / nSongs:F1}%); {100.0 * reused / nSongs:F1}% of songs share their title; worst: "
		  + string.Join(", ", songCounts.Take(8).Select(x => $"{x.t} x{x.n}")));
		var albumCounts = albums.GroupBy(x => Norm(Generic(x.t, x.act, x.single))).Select(gr => (t: Generic(gr.First().t, gr.First().act, gr.First().single), n: gr.Count())).OrderByDescending(x => x.n).ToList();
		P($"albums: {albumCounts.Count} distinct shapes of {nAlbums} ({100.0 * albumCounts.Count / nAlbums:F1}%, act name and single masked); "
		  + $"named after the single {Pct(albums.Count(x => x.t == x.single), nAlbums)}, self-titled {Pct(albums.Count(x => x.t == x.act), nAlbums)}; most common: "
		  + string.Join(", ", albumCounts.Take(6).Select(x => $"{x.t} x{x.n}")));
		// The masked count folds every self-titled LP into one shape and every single-named LP into another,
		// so it understates variety; split the shelf into what the title is built from.
		bool Bound(string t, string act, string single) => (!string.IsNullOrEmpty(act) && t.Contains(act)) || (!string.IsNullOrEmpty(single) && t.Contains(single));
		var free = albums.Where(x => !Bound(x.t, x.act, x.single)).ToList();
		var framed = albums.Where(x => Bound(x.t, x.act, x.single) && x.t != x.act && x.t != x.single).ToList();
		P($"albums by build: bare single {Pct(albums.Count(x => x.t == x.single), nAlbums)}, bare act {Pct(albums.Count(x => x.t == x.act), nAlbums)}, "
		  + $"act/single in a frame {Pct(framed.Count, nAlbums)} ({framed.Select(x => Norm(Generic(x.t, x.act, x.single))).Distinct().Count()} frames), "
		  + $"free {Pct(free.Count, nAlbums)} ({free.Select(x => Norm(x.t)).Distinct().Count()} distinct = {Pct(free.Select(x => Norm(x.t)).Distinct().Count(), free.Count)}); "
		  + $"literal titles distinct {Pct(albums.Select(x => Norm(x.t)).Distinct().Count(), nAlbums)}");

		// ---- word overuse: share of a genre's titles containing each content word. A browsable list
		// (the cover catalogue) reads as repetitive long before exact titles repeat ("Altar" x3 in ten rows).
		P(); P("== most-used content words per genre (% of that genre's song titles)");
		foreach (var gr in songs.GroupBy(s => s.g).OrderByDescending(gr => gr.Count())) {
			int n = gr.Count();
			var top = gr.SelectMany(s => Regex.Matches(s.t, @"[A-Za-z']+").Select(m => m.Value.ToLowerInvariant()).Where(w => w.Length > 3 && !StopWords.Contains(w)).Distinct())
				.GroupBy(w => w).Select(wg => (w: wg.Key, c: wg.Count())).OrderByDescending(x => x.c).Take(6);
			P($"{gr.Key,-18} n={n,-5} " + string.Join("  ", top.Select(x => $"{x.w} {100.0 * x.c / n:F1}%")));
		}

		// ---- shape vs history
		var hist = LoadHist(histPath);
		var songTitles = songs.Select(s => s.t).ToList();
		P(); P($"== shape vs Hot 100 1960-64 ({(hist.Count > 0 ? hist.Count + " titles" : "history file not found")})");
		P($"{"",-30} {"hist",7} {"game",7}");
		foreach (var (name, rx) in Shapes) {
			var re = new Regex(rx, RegexOptions.IgnoreCase);
			P($"{name,-30} {(hist.Count > 0 ? Pct(hist.Count(re.IsMatch), hist.Count) : "-"),7} {Pct(songTitles.Count(re.IsMatch), songTitles.Count),7}");
		}

		// ---- lint
		var lint = new Lint(lex);
		P(); P("== lint (songs + albums, per 1,000 titles)");
		var all = songTitles.Concat(albums.Select(x => x.t)).ToList();
		var hits = new Dictionary<string, List<string>>();
		foreach (var t in songTitles) lint.Check(t, null, null, hits);
		foreach (var x in albums) lint.Check(x.t, x.act, x.single, hits);
		int totalBad = 0;
		foreach (var rule in Lint.Rules) {
			hits.TryGetValue(rule, out var l); l ??= new List<string>();
			totalBad += l.Count;
			P($"{rule,-34} {1000.0 * l.Count / all.Count,6:F1}   {string.Join(" | ", l.Distinct().Take(5))}");
		}
		P($"{"ANY",-34} {1000.0 * totalBad / all.Count,6:F1}");

		// ---- real titles
		var histSet = new HashSet<string>(hist.Select(Norm));
		var real = songTitles.Where(t => histSet.Contains(Norm(t))).ToList();
		P(); P($"== real 1960-64 hit titles reproduced: {real.Count} ({Pct(real.Count, songTitles.Count)}), {real.Distinct().Count()} distinct: "
			 + string.Join(", ", real.GroupBy(x => x).OrderByDescending(gr => gr.Count()).Take(15).Select(gr => $"{gr.Key} x{gr.Count()}")));

		// ---- per-genre album space
		P(); P("== album title space per genre (distinct masked shapes / albums)");
		foreach (var gr in albums.GroupBy(x => x.g).OrderByDescending(gr => gr.Count()))
			P($"{gr.Key,-18} {gr.Select(x => Norm(Generic(x.t, x.act, x.single))).Distinct().Count(),4} / {gr.Count(),-4}  {string.Join(" | ", gr.Take(5).Select(x => x.t))}");

		if (args.Contains("--routing")) {
			P(); P("== routing (song set 1962 / 1968, album set)");
			var rr = new DeterministicRandom(seed);
			foreach (var (g, _) in mix)
				P($"{g,-18} {titles.ChooseSongSymbol(g, 1962, rr),-28} {titles.ChooseSongSymbol(g, 1968, rr),-28} {acts.Router.Resolve("albumTitle", g.ToString()) ?? "albumTitle"}");
		}
		if (args.Contains("--templates")) {
			P(); P("== template fill failure (200 fills each, 1965; templates failing >20%)");
			var drng = new DeterministicRandom(seed ^ 0x74656D706CUL);
			foreach (var sym in acts.Engine.ConstraintSymbols.Where(s => (s.StartsWith("songTitle") || s.StartsWith("albumTitle")) && !s.Contains("._")).OrderBy(s => s)) {
				var gp = acts.Engine.Models.Genres.Get(SymbolGenre(sym));
				foreach (var tpl in acts.Engine.ConstraintSet(sym)) {
					int fails = 0;
					for (int k = 0; k < 200; k++) {
						var ctx = acts.MakeContext(Genre.TraditionalPop, 1965, ArtistType.Band, drng);
						ctx.Genre = gp.Id; ctx.Slots["selfTitle"] = "The Calloways"; ctx.Slots["artist"] = "The Calloways"; ctx.Slots["leadSingle"] = "Lead Single"; ctx.Slots["titleTrack"] = "Lead Single";
						if (string.IsNullOrEmpty(acts.Engine.Templates.Fill(tpl, ctx, gp))) fails++;
					}
					if (fails > 40) P($"{sym,-26} {tpl.Id,-30} w{tpl.Weight,-4} fail {fails / 2.0,5:F1}%  {tpl.Pattern}");
				}
			}
		}
		if (args.Contains("--dump") && FindUp("SimLogs") is string dumpDir && Directory.Exists(dumpDir)) {
			File.WriteAllLines(Path.Combine(dumpDir, $"title-census-songs-{seed}.tsv"), songs.Select(s => $"{s.g}\t{s.year}\t{s.t}"));
			File.WriteAllLines(Path.Combine(dumpDir, $"title-census-albums-{seed}.tsv"), albums.Select(x => $"{x.g}\t{x.act}\t{x.single}\t{x.t}"));
		}
		if (args.Contains("--native")) {
			// CompositionCatalogService.GenerateNativeRepertoire titles: KeyedSongTitle, one fresh rng per song id.
			P(); P("== native repertoire (keyed titles, 1959)");
			foreach (var g in new[] { Genre.Folk, Genre.Classical, Genre.Comedy, Genre.Childrens, Genre.LatinPop, Genre.TexMex,
					Genre.BossaNova, Genre.ContemporaryFolk, Genre.SurfRock, Genre.EasyListening })
				P($"{g,-18} " + string.Join(" | ", Enumerable.Range(0, 10).Select(i =>
					titles.KeyedSongTitle(g, 1959, new DeterministicRandom(seed * 7919UL + (ulong)(g.GetHashCode() * 131 + i))) ?? "(null)")));
		}
		if (samples) {
			P(); P("== samples");
			foreach (var gr in songs.GroupBy(s => s.g).OrderByDescending(gr => gr.Count()))
				P($"{gr.Key,-18} {string.Join(" | ", gr.Take(14).Select(s => s.t))}");
		}
		string outDir = FindUp("SimLogs") is string sl && Directory.Exists(sl) ? sl : Directory.GetCurrentDirectory();
		string outPath = Path.Combine(outDir, $"title-census-{(legacy ? "legacy" : "current")}-{seed}.txt");
		File.WriteAllText(outPath, sb.ToString());
		Console.WriteLine($"\nwrote {outPath}");
		return 0;
	}

	static readonly (string, string)[] Shapes = {
		("parenthetical", @"\("),
		("question opener", @"^(Will|Where|Why|Do|Does|Can|What|Who|How|Is|Are|Did|Have|Would)\b"),
		("contains love", @"love"),
		("you/your", @"\b(you|your|you're|you've|you'll)\b"),
		("I/I'm/I'll/I've opener", @"^(I|I'm|I'll|I've|I'd) "),
		("one word", @"^[^ ]+$"),
		("five+ words", @"^([^ ]+ ){4,}[^ ]+$"),
		("blues", @"blues"),
		("ends ! or ?", @"[!?]$"),
		("ends !", @"!$"),
		("pronoun-verb-pronoun", @"^(I|You|Don't|Please) [A-Za-z']+ (You|Me)$"),
		("two capitalised words", @"^[A-Z][a-z]+ [A-Z][a-z]+$"),
	};

	/// <summary>The genre profile a set is written for: "songTitle.rockAndRoll" -> RockAndRoll (profiles are case-insensitive).</summary>
	static string SymbolGenre(string sym) { var p = sym.Split('.'); return p.Length > 1 ? p[1] : "_neutral"; }

	static string Generic(string t, string act, string single) {
		if (!string.IsNullOrEmpty(single)) t = t.Replace(single, "<single>");
		if (!string.IsNullOrEmpty(act)) t = t.Replace(act, "<act>");
		return t;
	}

	static List<string> LoadHist(string path) {
		var list = new List<string>();
		if (path == null || !File.Exists(path)) return list;
		var seen = new HashSet<string>();
		foreach (var line in File.ReadLines(path).Skip(1)) {
			var f = SplitCsv(line);
			if (f.Count >= 3 && seen.Add(f[2])) list.Add(f[2]);
		}
		return list;
	}

	static List<string> SplitCsv(string line) {
		var r = new List<string>(); var cur = new StringBuilder(); bool q = false;
		foreach (char ch in line) {
			if (ch == '"') q = !q;
			else if (ch == ',' && !q) { r.Add(cur.ToString()); cur.Clear(); }
			else cur.Append(ch);
		}
		r.Add(cur.ToString());
		return r;
	}

	static readonly HashSet<string> StopWords = new() {
		"that", "this", "with", "your", "from", "when", "what", "where", "there", "they", "them", "been", "were",
		"will", "just", "into", "over", "like", "than", "then", "have", "don't", "can't", "it's", "i'll", "i'm", "you're",
		"ain't", "won't", "baby", "love" };
	static string Norm(string s) => Regex.Replace((s ?? "").ToLowerInvariant(), @"[^a-z0-9 ]", "").Trim();
	static string Pct(int n, int d) => d == 0 ? "-" : $"{100.0 * n / d:F1}%";

	static string FindUp(string rel) {
		var d = new DirectoryInfo(AppContext.BaseDirectory);
		while (d != null) { string p = Path.Combine(d.FullName, rel); if (File.Exists(p) || Directory.Exists(p)) return p; d = d.Parent; }
		return null;
	}

	/// <summary>Surface checks on finished titles. Independent of the selection code where it can be (regexes);
	/// where it needs a word's class it reads the lexicon's g* feature tags, which load in either mode.</summary>
	sealed class Lint {
		public static readonly string[] Rules = {
			"verb without person object + Me/You", "abstract noun after spatial 'the'", "'down/up/along the' + non-path",
			"'the' + proper/adverb/exclamation", "bare preposition + count noun", "doubled article", "double plural",
			"plural act + singular verb", "'?' on a non-question", "-in' on a non-suffix", "'!'/'?' on a bound name",
			"abstract noun + spatial 'the'" };

		readonly Dictionary<string, WordEntry[]> _words = new(StringComparer.OrdinalIgnoreCase);
		readonly HashSet<string> _verbs = new(StringComparer.OrdinalIgnoreCase), _objVerbs = new(StringComparer.OrdinalIgnoreCase);

		public Lint(Lexicon lex) {
			foreach (var p in new[] { "noun", "place" })
				foreach (var e in lex.Pool(p)) {
					_words[e.Word] = _words.TryGetValue(e.Word, out var arr) ? arr.Append(e).ToArray() : new[] { e };
				}
			foreach (var e in lex.Pool("verb")) { _verbs.Add(e.Word); if (e.HasTag("gObjPerson")) _objVerbs.Add(e.Word); }
			// Words a set's own pool writes with their article ("the Tennessee", "the Morning") and dance names
			// ("Do the Cha-Cha") are correct after "the", whatever the shared noun pool says about the bare word.
			foreach (var pos in lex.PartsOfSpeech)
				foreach (var e in lex.Pool(pos)) {
					if (e.Word.StartsWith("the ", StringComparison.OrdinalIgnoreCase)) _theOk.Add(e.Word.Substring(4));
					if (pos == "albDance" || pos == "ltDance" || pos == "sfWave") _theOk.Add(e.Word);
				}
		}
		readonly HashSet<string> _theOk = new(StringComparer.OrdinalIgnoreCase);

		bool Noun(string w, Func<WordEntry, bool> pred) => _words.TryGetValue(w, out var arr) && arr.All(pred);
		bool IsNoun(string w) => _words.ContainsKey(w);

		static readonly Regex VerbObj = new(@"(?:^|, |\b(?:I|You|Don't|Please|Won't|Can't|Wanna|Gonna|Let)\s)([A-Za-z]+) (Me|You|Her|Him|Us)\b");
		static readonly Regex Spatial = new(@"\b(?:Down|Up|Along|Across|Through|Over|Under|Beneath|Beyond|Beside|Behind|Around|Past|Into|Onto|On|By|Near|Toward|Before|After) the ([A-Z][a-z'-]+(?: [A-Z][a-z'-]+)?)", RegexOptions.IgnoreCase);
		static readonly Regex PathRx = new(@"\b(?:Down|Up|Along) the ([A-Z][a-z'-]+(?: [A-Z][a-z'-]+)?)", RegexOptions.IgnoreCase);
		static readonly Regex TheRx = new(@"\bthe ([A-Z][A-Za-z'.-]+(?: [A-Z0-9][A-Za-z'.-]+)?)");
		static readonly Regex BarePrep = new(@"\b(?:of|at|in|on|to|from|into|onto|across|through|over|under|beyond|beside)\s+([A-Z][a-z'-]+)$");
		static readonly Regex SpatialHead = new(@"\b([A-Z][a-z'-]+) (?:Down|Up|Along|Across|Through|Over|Under|Beneath|Beyond|Beside|Behind|Around|Past|On|By|Near|Before|After) the\b");
		/// <summary>Is the word starting at <paramref name="i"/> followed by another capitalised word (a modifier, "the Parisian Keys")?</summary>
		static bool IsNounStart(string s, int i) => i + 1 < s.Length && s[i] == ' ' && char.IsUpper(s[i + 1]);
		static readonly Regex DoubleArt = new(@"\b(the|a|an) the\b", RegexOptions.IgnoreCase);
		static readonly Regex DoublePl = new(@"\b(\w+(?:ses|xes|ches|shes))\b");
		static readonly Regex Ing = new(@"\b([A-Za-z]+)in'(?=\W|$)");
		static readonly Regex SingVerb = new(@"^ (Sings|Plays|Swings|Rocks|Goes|Meets|Salutes|Celebrates|Presents|Conducts|Is|Has)\b");

		public void Check(string t, string act, string single, Dictionary<string, List<string>> hits) {
			void Hit(string rule) { if (!hits.TryGetValue(rule, out var l)) hits[rule] = l = new List<string>(); l.Add(t); }
			if (string.IsNullOrWhiteSpace(t)) return;
			string body = t;
			if (!string.IsNullOrEmpty(single)) body = body.Replace(single, "");   // the single is checked as a song
			foreach (Match m in VerbObj.Matches(body)) {
				string v = m.Groups[1].Value;
				if (_verbs.Contains(v) && !_objVerbs.Contains(v)) { Hit(Rules[0]); break; }
			}
			foreach (Match m in Spatial.Matches(body)) {
				string w = First(m.Groups[1].Value);
				if (IsNoun(w) && Noun(w, e => e.HasTag("gAbstract"))) { Hit(Rules[1]); break; }
			}
			foreach (Match m in PathRx.Matches(body)) {
				string w = m.Groups[1].Value, w1 = First(w);
				bool known = IsNoun(w) || IsNoun(w1);
				bool path = (IsNoun(w) && Noun(w, e => e.HasTag("gPath"))) || (IsNoun(w1) && Noun(w1, e => e.HasTag("gPath")));
				if (known && !path) { Hit(Rules[2]); break; }
			}
			foreach (Match m in TheRx.Matches(body)) {
				string w = m.Groups[1].Value, w1 = First(w);
				bool Bad(string x) => IsNoun(x) && Noun(x, e => e.HasTag("gProper") || e.HasTag("gAdverbial") || e.HasTag("gExclaim") || e.HasTag("gGerund"));
				if (IsNoun("the " + w) || IsNoun("the " + w1)) continue;   // "the Rio Grande" is how the word is written
				if (_theOk.Contains(w) || _theOk.Contains(w1)) continue;
				if (IsNoun(w) ? Bad(w) : (Bad(w1) && !IsNounStart(body, m.Index + 4 + w1.Length))) { Hit(Rules[3]); break; }
			}
			foreach (Match m in SpatialHead.Matches(body)) {
				string w = m.Groups[1].Value;
				if (IsNoun(w) && Noun(w, e => e.HasTag("gAbstract") || e.HasTag("gAdverbial"))) { Hit(Rules[11]); break; }
			}
			{
				var m = BarePrep.Match(body.TrimEnd('!', '?'));
				string w = m.Success ? m.Groups[1].Value : null;
				if (w != null && IsNoun(w) && Noun(w, e => !(e.HasTag("gMass") || e.HasTag("gAbstract") || e.HasTag("gProper") || e.HasTag("gPlural")
						|| e.HasTag("gArticle") || e.HasTag("gAdverbial") || e.HasTag("gExclaim") || e.HasTag("gGerund")))) Hit(Rules[4]);
			}
			if (DoubleArt.IsMatch(t)) Hit(Rules[5]);
			foreach (Match m in DoublePl.Matches(body)) {
				string w = m.Groups[1].Value, stem = w.Substring(0, w.Length - 2);
				if (IsNoun(stem) && Noun(stem, e => e.HasTag("gPlural")) || w.EndsWith("eses") && IsNoun(stem)) { Hit(Rules[6]); break; }
			}
			if (act != null && TitlePolish.IsPluralAct(act)) {
				int i = t.IndexOf(act, StringComparison.Ordinal);
				if (i >= 0 && SingVerb.IsMatch(t.Substring(i + act.Length))) Hit(Rules[7]);
			}
			if (t.EndsWith("?") && !TitlePolish.IsQuestion(t)) Hit(Rules[8]);
			foreach (Match m in Ing.Matches(body)) {
				string stem = m.Groups[1].Value;
				if (stem.IndexOfAny("aeiouyAEIOUY".ToCharArray()) < 0) { Hit(Rules[9]); break; }
			}
			foreach (var bound in new[] { act, single })
				if (!string.IsNullOrEmpty(bound) && (t == bound + "!" || t == bound + "?")) Hit(Rules[10]);
		}

		static string First(string s) { int i = s.IndexOf(' '); return i < 0 ? s : s.Substring(0, i); }
	}
}
