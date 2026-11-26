using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>How hard a check is: the number 2d6 + instinct + modifiers has to reach.</summary>
public enum CheckDifficulty { Trivial = 6, Easy = 8, Medium = 10, Challenging = 12, Formidable = 13, Legendary = 14, Heroic = 15 }

/// <summary>One check as it stands before you commit: who's rolling, against what, with what in their favour.</summary>
public sealed class CheckRead {
	public ExecutiveVoice Voice;
	public int Instinct;
	public CheckDifficulty Difficulty;
	/// <summary>What real state adds or takes away, each named ("they trust you", "you let them down").</summary>
	public List<(string Why, int Points)> Modifiers = new();
	public int Bonus => Instinct + Modifiers.Sum(m => m.Points);
	public float Odds => PlayerDesk.CheckOdds(Bonus, (int)Difficulty);
	/// <summary>The state the dice are keyed on. Same key, same roll.</summary>
	public string Key;
	/// <summary>A white check that failed on this same state: why it's shut, or null when it's open.</summary>
	public string Locked;
}

// ============================================================================================
// CHECKS AT NAMED MOMENTS (LabelManInnerLifeDirective.md §2 principle 2, amended 2026-11-26).
//
// Deterministic reads stay the dominant mode. A few named moments resolve as a visible check, so an instinct you've put
// points into buys real swings: 2d6 + the instinct + modifiers from real state, against a named difficulty. Double 1
// always fails, double 6 always passes. The dice are keyed on the act, the moment and the state that feeds them, so the
// same situation always rolls the same: reloading doesn't reroll it. A white check that failed reopens only when that
// state changes (an instinct point, another night heard, more regard); a red check is one shot. Not in a new player's
// first signing. Player-only: no AI code reads any of this.
// ============================================================================================
public partial class PlayerDesk {
	/// <summary>Checks open once your first record is out: a new player's first signing runs on reads alone.</summary>
	public bool ChecksOpen => Label?.totalReleases > 0;

	/// <summary>The chance 2d6 + bonus reaches the difficulty, with double 1 a fail and double 6 a pass.</summary>
	public static float CheckOdds(int bonus, int difficulty) {
		int pass = 0;
		for (int a = 1; a <= 6; a++) for (int b = 1; b <= 6; b++) if (CheckPasses(a, b, bonus, difficulty)) pass++;
		return pass / 36f;
	}

	public static bool CheckPasses(int d1, int d2, int bonus, int difficulty) =>
		d1 == 6 && d2 == 6 || !(d1 == 1 && d2 == 1) && d1 + d2 + bonus >= difficulty;

	/// <summary>The two dice for a check, keyed on its state. Not the shared RNG: nothing else moves.</summary>
	public static (int D1, int D2) CheckDice(string key) {
		int Die(string which) => 1 + Math.Min(5, (int)(LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, $"check-v1|{key}|{which}") * 6));
		return (Die("a"), Die("b"));
	}

	public static string DifficultyName(CheckDifficulty d) => d.ToString();

	/// <summary>The odds as the voice puts them, before you commit.</summary>
	public static string OddsWords(float odds) => odds >= .8f ? "They'll buy it." : odds >= .55f ? "Better than even." :
		odds >= .35f ? "Coin toss, near enough." : odds >= .15f ? "Long shot." : "Forget it.";

	/// <summary>The small print under a check's line: the instinct, the modifiers, the difficulty and the odds.</summary>
	public static string CheckSummary(CheckRead c) {
		string mods = string.Concat(c.Modifiers.Where(m => m.Points != 0).Select(m => $" {(m.Points > 0 ? "+" : "−")}{Math.Abs(m.Points)} {m.Why}"));
		return $"{c.Voice} {c.Instinct}{mods} against {DifficultyName(c.Difficulty)} {(int)c.Difficulty}: {c.Odds:P0}. {OddsWords(c.Odds)}";
	}
}
