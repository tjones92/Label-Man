// SimTools/NamingCensus/Program.cs
// Generates a launch-sized population of acts (with synthetic but game-shaped lineups) and labels
// through ActNaming -- the same policy class the game's NameGenerator delegates to -- and counts the
// failures that make a name implausible:
//   * a person named in the act's name who is not in the lineup ("Sonny 'n' Buck", no Sonny);
//   * a head-count, sex or kin word that is false of the lineup ("Burke Trio" with four, "Brothers"
//     with a woman or with three surnames);
//   * a doubled article ("The the Ozarks Players");
//   * Roman/number disambiguation ("RCA Records III") and exact duplicates;
//   * labels that reuse a historical label's name.
// Usage: NamingCensus [acts=7000] [labels=1100] [seed=1001] [--samples] [--genres A,B] [--years 1962-1965]

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LabelMan.Naming;

public static class Program {
	static readonly string[] HistoricalLabels = {
		"Columbia", "RCA Victor", "Capitol", "Decca", "Mercury", "Motown", "Stax", "Atlantic", "Chess", "King", "Sun",
		"Imperial", "Specialty", "Liberty", "Cameo-Parkway", "Chancellor", "Colpix", "Dimension", "Red Bird", "Vee-Jay",
		"Duke-Peacock", "Excello", "Ace", "Fire-Fury", "Parlophone", "Decca UK", "Pye", "EMI" };

	public static int Main(string[] args) {
		int nActs = args.Length > 0 && int.TryParse(args[0], out var a) ? a : 7000;
		int nLabels = args.Length > 1 && int.TryParse(args[1], out var b) ? b : 1100;
		ulong seed = args.Length > 2 && ulong.TryParse(args[2], out var c) ? c : 1001UL;
		bool samples = args.Contains("--samples");
		string dir = FindDataDir();
		var naming = ActNaming.Load(f => { string p = Path.Combine(dir, f); return File.Exists(p) ? File.ReadAllText(p) : null; }, out _);
		if (naming == null) { Console.Error.WriteLine("could not load " + dir); return 2; }

		var actRng = new DeterministicRandom(seed ^ 0x6163746E616D6573UL);
		var popRng = new DeterministicRandom(seed);
		var lex = naming.Engine.Lexicon;
		var firstNames = new HashSet<string>(lex.Pool("firstName").Select(e => e.Word), StringComparer.Ordinal);
		var lastNames = new HashSet<string>(lex.Pool("lastName").Select(e => e.Word), StringComparer.Ordinal);

		var genres = new (Genre g, double w)[] {
			(Genre.TraditionalPop, 9), (Genre.RockAndRoll, 10), (Genre.DooWop, 8), (Genre.TeenPop, 8), (Genre.RnB, 8),
			(Genre.Soul, 6), (Genre.Country, 9), (Genre.Folk, 5), (Genre.Jazz, 5), (Genre.Blues, 4), (Genre.EasyListening, 5),
			(Genre.Gospel, 3), (Genre.SurfRock, 3), (Genre.GarageRock, 3), (Genre.BritishBeat, 3), (Genre.PsychedelicRock, 2),
			(Genre.FolkRock, 2), (Genre.SunshinePop, 1), (Genre.LatinPop, 1), (Genre.HardRock, 1), (Genre.Bubblegum, 1) };
		// --genres A,B,... measures only those genres, equally weighted; --years 1962-1965 pins the act years
		int gi = Array.IndexOf(args, "--genres");
		if (gi >= 0 && gi + 1 < args.Length)
			genres = args[gi + 1].Split(',').Select(x => (Enum.Parse<Genre>(x.Trim(), true), 1.0)).ToArray();
		int yi = Array.IndexOf(args, "--years");
		int y0 = 1960, ySpan = 10;
		if (yi >= 0 && yi + 1 < args.Length) { var yy = args[yi + 1].Split('-'); y0 = int.Parse(yy[0]); ySpan = int.Parse(yy[^1]) - y0 + 1; }
		int si = Array.IndexOf(args, "--sample-count");
		int sampleN = si >= 0 && si + 1 < args.Length ? int.Parse(args[si + 1]) : 14;
		double gsum = genres.Sum(x => x.w);

		int persons = 0, personMiss = 0, sizeMiss = 0, kinMiss = 0, theThe = 0, roman = 0, solos = 0, bands = 0, famActs = 0, famBilled = 0;
		var names = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var missSamples = new List<string>(); var byGenre = new Dictionary<Genre, List<string>>();
		var romanRx = new Regex(@" (II|III|IV|V|VI|VII|VIII|IX)$| \(\d+\)$");
		var t0 = DateTime.UtcNow;

		for (int i = 0; i < nActs; i++) {
			double r = popRng.NextDouble() * gsum; Genre g = genres[^1].g;
			foreach (var x in genres) { if ((r -= x.w) < 0) { g = x.g; break; } }
			int year = y0 + (i * ySpan / Math.Max(1, nActs));
			double tr = popRng.NextDouble();
			ArtistType type = tr < 0.40 ? (popRng.Chance(0.75) ? ArtistType.SoloMale : ArtistType.SoloFemale)
				: tr < 0.75 ? ArtistType.Band : tr < 0.90 ? ArtistType.VocalGroup : ArtistType.Duo;
			var lineup = MakeLineup(naming, g, type, popRng, actRng);
			bool fam = ActNaming.RollFamily(g, lineup, actRng);
			if (fam) famActs++;
			string name = naming.GenerateActName(g, year, type, lineup, actRng);
			bool solo = lineup.Size == 1;
			if (solo) solos++; else bands++;
			names[name] = names.TryGetValue(name, out var n0) ? n0 + 1 : 1;
			if (!byGenre.TryGetValue(g, out var lst)) byGenre[g] = lst = new List<string>();
			if (lst.Count < sampleN) lst.Add($"{name}  [{lineup.Size}: {string.Join(", ", lineup.People)}{(lineup.Family ? " FAM" : "")}]");

			// person check: every lexicon person word in the name must belong to a member
			var memberWords = new HashSet<string>(lineup.People.SelectMany(p => new[] { p.First, p.Last }), StringComparer.OrdinalIgnoreCase);
			var words = Regex.Matches(name, @"[A-Z][A-Za-z']+").Select(m => m.Value.TrimEnd('\'', 's')).ToList();
			var raw = Regex.Matches(name, @"[A-Z][A-Za-z']+").Select(m => m.Value).ToList();
			bool named = false, miss = false;
			foreach (var w in raw) {
				string bare = w.EndsWith("'s") ? w[..^2] : w;
				bool isPerson = firstNames.Contains(bare) || lastNames.Contains(bare);
				if (!isPerson) continue;
				if (memberWords.Contains(bare) || memberWords.Contains(bare + "s") || memberWords.Any(m => bare.Length > 2 && m.StartsWith(bare[..Math.Min(3, bare.Length)], StringComparison.OrdinalIgnoreCase))) { named = true; continue; }
				// a plural group noun that happens to be a name ("The Robins") is not a person reference
				if (raw.IndexOf(w) > 0 && name.StartsWith("The ") && !name.Contains(" and ") && !name.Contains(" & ")) continue;
				named = true; miss = true;
			}
			if (named) { persons++; if (miss) { personMiss++; if (missSamples.Count < 30) missSamples.Add($"{name}  <- {string.Join(", ", lineup.People)}"); } }

			string lower = " " + name.ToLowerInvariant() + " ";
			int claimed = lower.Contains(" duo ") ? 2 : lower.Contains(" trio ") ? 3 : lower.Contains(" quartet ") ? 4 : lower.Contains(" quintet ") ? 5 : lower.Contains(" sextet ") ? 6 : -1;
			var m2 = Regex.Match(name, @"^The (Two|Three|Four|Five|Six|Seven|Eight) ");
			if (m2.Success) claimed = Lineup.CountFromWord(m2.Groups[1].Value);
			if (claimed > 0 && claimed != lineup.Size) sizeMiss++;
			if ((lower.Contains(" brothers ") && !(lineup.Family && lineup.AllMale)) || (lower.Contains(" sisters ") && !(lineup.Family && lineup.AllFemale))) kinMiss++;
			if (fam && (lower.Contains(" brothers ") || lower.Contains(" sisters ") || lower.Contains(" family ") || lower.Contains(" twins "))) famBilled++;
			if (Regex.IsMatch(name, @"\bthe the\b", RegexOptions.IgnoreCase)) theThe++;
			if (romanRx.IsMatch(name)) roman++;
		}
		var actSecs = (DateTime.UtcNow - t0).TotalSeconds;

		Console.WriteLine($"ACTS {nActs} ({solos} solo, {bands} group) in {actSecs:F1}s");
		Console.WriteLine($"  names naming a person        {persons}");
		Console.WriteLine($"  ...naming a non-member       {personMiss}  ({Pct(personMiss, persons)})");
		Console.WriteLine($"  head-count word false        {sizeMiss}");
		Console.WriteLine($"  Brothers/Sisters false       {kinMiss}");
		Console.WriteLine($"  family acts / kin-billed     {famActs} / {famBilled}");
		Console.WriteLine($"  doubled article              {theThe}");
		Console.WriteLine($"  roman/number suffix          {roman}  ({Pct(roman, nActs)})");
		Console.WriteLine($"  exact duplicate names        {names.Values.Where(v => v > 1).Sum()}");
		foreach (var s in missSamples.Take(12)) Console.WriteLine("    miss: " + s);

		// ---- labels
		naming.ReserveLabelNames(HistoricalLabels);
		var archetypes = (LabelArchetype[])Enum.GetValues(typeof(LabelArchetype));
		var labels = new List<string>(); var labelRng = new DeterministicRandom(seed ^ 0x6C6162656C73UL);
		var cities = new[] { "Chicago", "Memphis", "Detroit", "Nashville", "Philadelphia", "Houston", "Cincinnati", "New Orleans", "Cleveland", "Los Angeles", "New York", "Atlanta" };
		t0 = DateTime.UtcNow;
		for (int i = 0; i < nLabels; i++) {
			var arch = archetypes[labelRng.Next(archetypes.Length)];
			string founder = i % 2 == 0 ? lex.Query("lastName", new[] { "generic" }, new NamingContext { Rng = labelRng, Year = 1962 }) : null;
			labels.Add(naming.GenerateLabelName(arch, labelRng, founder, cities[labelRng.Next(cities.Length)], 1960 + i * 10 / nLabels));
		}
		var hcores = new HashSet<string>(HistoricalLabels.Select(ActNaming.LabelCore));
		var cores = labels.GroupBy(ActNaming.LabelCore).Where(x => x.Count() > 1).Sum(x => x.Count());
		Console.WriteLine($"LABELS {nLabels} in {(DateTime.UtcNow - t0).TotalSeconds:F1}s");
		Console.WriteLine($"  roman/number suffix          {labels.Count(l => romanRx.IsMatch(l))}");
		Console.WriteLine($"  reuse a historical name      {labels.Count(l => hcores.Contains(ActNaming.LabelCore(l)))}");
		Console.WriteLine($"  duplicate core names         {cores}");
		Console.WriteLine($"  doubled article              {labels.Count(l => Regex.IsMatch(l, @"\bthe the\b", RegexOptions.IgnoreCase))}");

		if (samples) {
			foreach (var kv in byGenre.OrderBy(k => k.Key.ToString())) {
				Console.WriteLine($"\n== {kv.Key}");
				foreach (var s in kv.Value) Console.WriteLine("  " + s);
			}
			Console.WriteLine("\n== labels");
			foreach (var l in labels.Take(60)) Console.WriteLine("  " + l);
		}
		return 0;
	}

	static string Pct(int n, int d) => d == 0 ? "-" : $"{100.0 * n / d:F1}%";

	// Lineup shapes mirror ArtistManager.GenerateMembers: solo = 1; duo = 2; vocal group 3-6;
	// band = the genre's role list (4-6). Names come from the same lexicon queries GeneratePersonName makes.
	static Lineup MakeLineup(ActNaming naming, Genre g, ArtistType type, IRandom pop, IRandom act) {
		int size = type switch {
			ArtistType.SoloMale or ArtistType.SoloFemale => 1, ArtistType.Duo => 2,
			ArtistType.VocalGroup => pop.Range(3, 6),
			_ => g switch { Genre.Jazz => 6, Genre.Folk or Genre.FolkRock => 3, Genre.Soul or Genre.RnB or Genre.Country or Genre.SurfRock or Genre.PsychedelicRock => 5, _ => 4 }
		};
		bool girlGroup = type == ArtistType.VocalGroup && (g == Genre.GirlGroup || pop.Chance(0.2));
		var l = new Lineup();
		string eth = ActNaming.RollBandEthnicity(g, act);
		for (int i = 0; i < size; i++) {
			bool female = type == ArtistType.SoloFemale || girlGroup || (type == ArtistType.SoloMale ? false : i == 0 ? pop.Chance(0.15) : pop.Chance(type == ArtistType.Duo ? 0.3 : 0.05));
			var (first, last) = naming.PersonName(!female, g, eth, act);
			l.People.Add(new LineupPerson(first, last, female));
		}
		return l;
	}

	static string FindDataDir() {
		var d = new DirectoryInfo(AppContext.BaseDirectory);
		while (d != null) {
			var p = Path.Combine(d.FullName, "Data", "Naming");
			if (File.Exists(Path.Combine(p, "grammar.json"))) return p;
			d = d.Parent;
		}
		var env = Environment.GetEnvironmentVariable("LABELMAN_NAMING_DIR");
		return env ?? Path.Combine(Directory.GetCurrentDirectory(), "Data", "Naming");
	}
}
