using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>--historical-majors-check: off leaves the Major templates and the procedural Major band alone; on, the five
/// templates carry their 1960 repertoire and no procedural label opens as a Major.</summary>
public static class HistoricalMajorsChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Historical majors: " + why); }
		bool was = HistoricalMajors.Enabled;
		string[] majors = { "Columbia", "RCA Victor", "Capitol", "Decca", "Mercury" };
		List<AILabel> Generate(bool on, ulong seed) { HistoricalMajors.SetEnabledForChecks(on); GameRandom.Seed(seed); return AILabelFactory.GenerateAllLabels(600); }
		string Traits(AILabel l) => $"{l.labelId}|{l.tier}|{l.archetype}|{l.cashReserves}|{string.Join(",", l.preferredGenres)}";
		try {
			int proceduralMajorsOff = 0;
			foreach (ulong seed in new ulong[] { 1001, 2002, 3003, 4004, 5005, 17017 }) {
				var off = Generate(false, seed);
				var on = Generate(true, seed);
				proceduralMajorsOff += off.Count(l => !l.isHistorical && l.tier == LabelTier.Major);
				Check(!on.Any(l => !l.isHistorical && l.tier == LabelTier.Major), $"seed {seed}: no procedural label may open as a Major");
				Check(on.Count(l => l.tier == LabelTier.Major) == 5, $"seed {seed}: exactly the five US templates must be Majors");
				Check(!on.Any(l => l.isHistorical && l.headquartersCity == "London"), $"seed {seed}: no London template may be generated");
				Check(on.Count == off.Count - (BritishLabelDeferral.Enabled ? 0 : 4), $"seed {seed}: on must drop only the four London templates");
				for (int i = 0; i < 5; i++) {
					Check(on[i].labelName.StartsWith(majors[i], StringComparison.Ordinal), $"label_000{i + 1} must be {majors[i]}");
					Check(on[i].preferredGenres.SequenceEqual(HistoricalMajors.GenresFor(majors[i])), $"{majors[i]} must carry its 1960 repertoire");
					Check(on[i].preferredGenres[0] == Genre.TraditionalPop, $"{majors[i]} must stay pop-first");
					Check(off[i].preferredGenres.Length == 3, $"off must keep {majors[i]}'s template genres");
				}
				var again = Generate(true, seed);
				Check(Enumerable.Range(0, on.Count).All(i => Traits(on[i]) == Traits(again[i])), $"seed {seed}: on must be deterministic");
				var offAgain = Generate(false, seed);
				Check(Enumerable.Range(0, off.Count).All(i => Traits(off[i]) == Traits(offAgain[i])), $"seed {seed}: off must be deterministic");
			}
			Check(proceduralMajorsOff > 0, "the check seeds must include a procedural Major with the switch off");
			Check(HistoricalMajors.GenresFor("Decca UK") == null && HistoricalMajors.GenresFor("MGM") == null,
				"only the five US Major templates are changed");
		} finally {
			HistoricalMajors.SetEnabledForChecks(was);
		}
		Godot.GD.Print("HISTORICAL_MAJORS_CHECK ok");
	}
}
