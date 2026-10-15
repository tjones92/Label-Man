using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>How strongly a piece of evidence suggests a content issue. A standards desk queries Marked content;
/// Mild content is something an anxious desk might ask about. Neither is a verdict (§7.1).</summary>
public enum ContentSeverity { Mild, Marked }

/// <summary>
/// Authored content tags for songs (MediaAtmosphereDirective §7.1). The only lyric text the game has is the title,
/// built from a tagged lexicon, plus the lyric mode some songs carry. So the tags are read from that evidence and
/// nothing else: never from genre, the act's name, its look or its members' private lives (a musician's
/// substanceLoad never makes a song a drug song). The basis is recorded, so "no tags" means "nothing in the title",
/// never "proved clean".
/// <para>Deterministic and draw-free: the same title always gives the same tags, in every world and on every load.</para>
/// </summary>
public static class SongContentTags {
	public const string Basis = "title-v1";

	public const string ExplicitLanguage = "explicit-language";
	public const string SexualSuggestion = "sexual-suggestion";
	public const string DrugReference = "drug-reference";
	public const string Alcohol = "alcohol";
	public const string ReligiousProvocation = "religious-provocation";
	public const string PoliticalProtest = "political-protest";
	public const string AntiWar = "anti-war";
	public const string ViolenceCrime = "violence-crime";

	public static readonly string[] All = { ExplicitLanguage, SexualSuggestion, DrugReference, Alcohol, ReligiousProvocation, PoliticalProtest, AntiWar, ViolenceCrime };

	private readonly record struct Cue(string Tag, ContentSeverity Severity);

	// Phrases are matched whole-word, case-insensitively, against the title. Multiword phrases first.
	// Word lists follow the naming lexicon's vice / protest / conflict groups plus the period standards vocabulary.
	private static readonly (string Phrase, Cue Cue)[] cues = new (string, Cue)[] {
		("spend the night", new(SexualSuggestion, ContentSeverity.Marked)),
		("make love", new(SexualSuggestion, ContentSeverity.Marked)),
		("all night long", new(SexualSuggestion, ContentSeverity.Mild)),
		("back door", new(SexualSuggestion, ContentSeverity.Mild)),
		("honky tonk", new(Alcohol, ContentSeverity.Mild)),
		("mary jane", new(DrugReference, ContentSeverity.Marked)),
		("we shall overcome", new(PoliticalProtest, ContentSeverity.Marked)),
		("eve of destruction", new(AntiWar, ContentSeverity.Marked)),

		("damn", new(ExplicitLanguage, ContentSeverity.Marked)),
		("goddamn", new(ExplicitLanguage, ContentSeverity.Marked)),
		("bastard", new(ExplicitLanguage, ContentSeverity.Marked)),
		("hell", new(ExplicitLanguage, ContentSeverity.Mild)),

		("backdoor", new(SexualSuggestion, ContentSeverity.Mild)),
		("skin", new(SexualSuggestion, ContentSeverity.Mild)),
		("body", new(SexualSuggestion, ContentSeverity.Mild)),
		("temptation", new(SexualSuggestion, ContentSeverity.Mild)),

		("reefer", new(DrugReference, ContentSeverity.Marked)),
		("lsd", new(DrugReference, ContentSeverity.Marked)),
		("junkie", new(DrugReference, ContentSeverity.Marked)),
		("heroin", new(DrugReference, ContentSeverity.Marked)),
		("needle", new(DrugReference, ContentSeverity.Mild)),
		("pills", new(DrugReference, ContentSeverity.Mild)),
		("acid", new(DrugReference, ContentSeverity.Mild)),
		("trip", new(DrugReference, ContentSeverity.Mild)),
		("high", new(DrugReference, ContentSeverity.Mild)),
		("grass", new(DrugReference, ContentSeverity.Mild)),
		("poison", new(DrugReference, ContentSeverity.Mild)),

		("whiskey", new(Alcohol, ContentSeverity.Mild)),
		("whisky", new(Alcohol, ContentSeverity.Mild)),
		("gin", new(Alcohol, ContentSeverity.Mild)),
		("moonshine", new(Alcohol, ContentSeverity.Mild)),
		("bourbon", new(Alcohol, ContentSeverity.Mild)),
		("tequila", new(Alcohol, ContentSeverity.Mild)),
		("beer", new(Alcohol, ContentSeverity.Mild)),
		("wine", new(Alcohol, ContentSeverity.Mild)),
		("cocktail", new(Alcohol, ContentSeverity.Mild)),
		("saloon", new(Alcohol, ContentSeverity.Mild)),
		("booze", new(Alcohol, ContentSeverity.Mild)),
		("drunk", new(Alcohol, ContentSeverity.Mild)),

		("devil", new(ReligiousProvocation, ContentSeverity.Mild)),
		("satan", new(ReligiousProvocation, ContentSeverity.Marked)),
		("lucifer", new(ReligiousProvocation, ContentSeverity.Marked)),
		("voodoo", new(ReligiousProvocation, ContentSeverity.Mild)),
		("hoodoo", new(ReligiousProvocation, ContentSeverity.Mild)),
		("sin", new(ReligiousProvocation, ContentSeverity.Mild)),
		("serpent", new(ReligiousProvocation, ContentSeverity.Mild)),

		("freedom", new(PoliticalProtest, ContentSeverity.Mild)),
		("justice", new(PoliticalProtest, ContentSeverity.Mild)),
		("liberty", new(PoliticalProtest, ContentSeverity.Mild)),
		("uprising", new(PoliticalProtest, ContentSeverity.Marked)),
		("revolution", new(PoliticalProtest, ContentSeverity.Mild)),
		("marching", new(PoliticalProtest, ContentSeverity.Mild)),
		("protest", new(PoliticalProtest, ContentSeverity.Marked)),
		("brotherhood", new(PoliticalProtest, ContentSeverity.Mild)),

		("war", new(AntiWar, ContentSeverity.Mild)),
		("soldier", new(AntiWar, ContentSeverity.Mild)),
		("draft", new(AntiWar, ContentSeverity.Mild)),
		("bomb", new(AntiWar, ContentSeverity.Mild)),
		("warhead", new(AntiWar, ContentSeverity.Mild)),
		("vietnam", new(AntiWar, ContentSeverity.Marked)),

		("gunfight", new(ViolenceCrime, ContentSeverity.Mild)),
		("killing", new(ViolenceCrime, ContentSeverity.Mild)),
		("murder", new(ViolenceCrime, ContentSeverity.Mild)),
		("riot", new(ViolenceCrime, ContentSeverity.Mild)),
		("jailhouse", new(ViolenceCrime, ContentSeverity.Mild)),
		("prison", new(ViolenceCrime, ContentSeverity.Mild)),
		("gun", new(ViolenceCrime, ContentSeverity.Mild)),
	};

	private static readonly (Regex Pattern, string Phrase, Cue Cue)[] compiled = cues
		.Select(c => (new Regex(@"(?<![\p{L}'])" + Regex.Escape(c.Phrase) + @"s?(?![\p{L}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), c.Phrase, c.Cue))
		.ToArray();

	/// <summary>One finding: which tag, how strongly, and the exact evidence (e.g. "title word 'Whiskey'").</summary>
	public readonly record struct Finding(string Tag, ContentSeverity Severity, string Evidence);

	/// <summary>Pure classification of a title and its lyric modes. Topical testimony turns protest and war words from
	/// imagery into a statement; a sacred song's sin and devil are its subject, not a provocation.</summary>
	public static List<Finding> Classify(string title, IReadOnlyCollection<SongLyricMode> lyricModes, SongContentContext context) {
		var findings = new List<Finding>();
		if (string.IsNullOrWhiteSpace(title)) return findings;
		bool topical = lyricModes != null && (lyricModes.Contains(SongLyricMode.TopicalTestimony) || lyricModes.Contains(SongLyricMode.InvectiveAccusation));
		var claimed = new List<(int Start, int End)>();
		foreach (var (pattern, phrase, cue) in compiled) {
			foreach (Match m in pattern.Matches(title)) {
				// A word inside an already-matched phrase ("honky tonk", "spend the night") is not a second finding.
				if (claimed.Any(c => m.Index >= c.Start && m.Index < c.End)) continue;
				claimed.Add((m.Index, m.Index + m.Length));
				if (cue.Tag == ReligiousProvocation && context == SongContentContext.Sacred) continue;
				ContentSeverity severity = cue.Severity;
				if (topical && (cue.Tag == PoliticalProtest || cue.Tag == AntiWar)) severity = ContentSeverity.Marked;
				findings.Add(new Finding(cue.Tag, severity, $"title word '{m.Value}'" + (topical && severity != cue.Severity ? " in a topical lyric" : "")));
			}
		}
		return findings;
	}

	/// <summary>Writes the tags and their evidence onto a song. Idempotent: re-authors only when the title or basis changed.</summary>
	public static void Author(SongComposition song) {
		if (song == null) return;
		if (song.contentTagsBasis == Basis && song.contentTagsTitle == song.title) return;
		List<Finding> findings = Classify(song.title, song.demoTaxonomy?.lyricModes, song.contentContext);
		song.contentTagIds = findings.Select(f => f.Tag).Distinct().ToArray();
		song.contentTagEvidence = findings.Select(f => f.Tag + "|" + f.Severity + "|" + f.Evidence).ToArray();
		song.contentTagsBasis = Basis;
		song.contentTagsTitle = song.title;
	}

	/// <summary>The findings for a song without writing anything (stale or unauthored songs are classified fresh).</summary>
	public static List<Finding> Read(SongComposition song) {
		if (song == null) return new();
		if (song.contentTagsBasis != Basis || song.contentTagsTitle != song.title)
			return Classify(song.title, song.demoTaxonomy?.lyricModes, song.contentContext);
		return (song.contentTagEvidence ?? Array.Empty<string>()).Select(line => {
			string[] parts = line.Split('|', 3);
			return new Finding(parts[0], Enum.TryParse(parts.ElementAtOrDefault(1), out ContentSeverity s) ? s : ContentSeverity.Mild, parts.ElementAtOrDefault(2) ?? "");
		}).ToList();
	}

	/// <summary>The strongest severity on a song, or null when the title shows nothing a standards desk would ask about.</summary>
	public static ContentSeverity? Strongest(SongComposition song) {
		var findings = Read(song);
		return findings.Count == 0 ? null : findings.Any(f => f.Severity == ContentSeverity.Marked) ? ContentSeverity.Marked : ContentSeverity.Mild;
	}
}
