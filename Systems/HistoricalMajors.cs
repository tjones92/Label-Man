using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --historical-majors (LpGenreAllocationFollowup.md §49). In 1960 the trade counted five or six majors, defined as
/// firms with their own pressing plants and their own distribution: Columbia, RCA Victor, Decca, Capitol and Mercury,
/// with MGM sometimes counted as the sixth (history-of-rock.com, "The Major Record Companies"). MGM stays the studio
/// label that --studio-parent-labels seeds: it pressed its own records, but it sold through independent distributors
/// (FilmBroadcastLabelResearch.md), and in this model the Major tier means branch distribution and Big-5 launch reach.
///
/// With the switch on:
/// <list type="bullet">
/// <item>The procedural tier draw's Major band (roll &lt; 0.002, about one label a world, at most three seen) becomes
/// MidTier. Those firms opened as Soul, Country, Blues or Rock specialists at Major scale, which no 1960 firm was. The same
/// draw is consumed, but every later label that branches on the tier draws differently, so the world reshuffles.</item>
/// <item>The five Major templates carry their 1960 repertoire. The first genre stays TraditionalPop for all five,
/// since each was a pop-first firm and the first genre sets the related secondary genres. Sources: the 1960 Top 10
/// reconstruction (SimLogs/historical-1960-top10/) and the label histories cited in §49.</item>
/// <item>The four legacy London templates (Parlophone, Decca UK, EMI, Pye) are dropped, as --defer-british-labels does;
/// they are not part of the 1960 US game. Profile runs already carry that flag, so this changes nothing there.</item>
/// </list>
/// Labels that promote to Major in the run are untouched: promotion is earned in-run.
/// </summary>
public static class HistoricalMajors {
	public const string Switch = "--historical-majors";
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; Enabled = value; }

	/// <summary>1960 repertoire of the five Major templates, by base name.</summary>
	internal static Genre[] GenresFor(string baseName) => baseName switch {
		// Mitch Miller's pop (Mathis, Bennett, Mitchell), Percy Faith's "Theme from A Summer Place" and Ray Conniff,
		// Nashville (Marty Robbins, Johnny Horton, Johnny Cash), the Brothers Four, Miles Davis and Brubeck.
		"Columbia" => new[] { Genre.TraditionalPop, Genre.EasyListening, Genre.Country, Genre.Folk, Genre.Jazz },
		// Como and Belafonte; Chet Atkins' Nashville Sound (Jim Reeves, Hank Locklin, the Browns, Floyd Cramer: four of RCA's
		// eleven 1960 Top 10 records); Elvis; Neil Sedaka and Ray Peterson; Mancini and Living Strings.
		"RCA Victor" => new[] { Genre.TraditionalPop, Genre.Country, Genre.RockAndRoll, Genre.TeenPop, Genre.EasyListening },
		// Sinatra, Nat King Cole, Dean Martin, Peggy Lee; the Kingston Trio, the leading LP act of 1959-60; Buck Owens,
		// Ferlin Husky and Tennessee Ernie Ford; Jackie Gleason's mood LPs; Kenton and Shearing.
		"Capitol" => new[] { Genre.TraditionalPop, Genre.Folk, Genre.Country, Genre.EasyListening, Genre.Jazz },
		// Crosby and the McGuire Sisters; Owen Bradley's Nashville (Kitty Wells, Ernest Tubb, Webb Pierce, Patsy Cline from
		// 1961), the leading country label beside RCA; Brenda Lee (four of Decca's five 1960 Top 10 records);
		// Bert Kaempfert's "Wonderland by Night".
		"Decca" => new[] { Genre.TraditionalPop, Genre.Country, Genre.TeenPop, Genre.EasyListening },
		// Patti Page; Brook Benton and Dinah Washington; the Platters; Johnny Preston's "Running Bear"; George Jones
		// (Mercury-Starday); Sarah Vaughan and the EmArcy jazz line.
		"Mercury" => new[] { Genre.TraditionalPop, Genre.RnB, Genre.DooWop, Genre.TeenPop, Genre.Country, Genre.Jazz },
		_ => null
	};
}
