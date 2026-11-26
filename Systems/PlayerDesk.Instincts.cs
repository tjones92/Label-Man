using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// INSTINCT GROWTH: THE DECEMBER POINT (SimTools/LabelManInnerLifeDirective.md §4.3, Phase 5 first slice).
//
// Instincts stop being frozen. The first legible channel: every December sitting that rolls the lien (any bar met) earns one
// point, spent on one instinct, up to 5. It's earned by a year survived; there's no XP bar, and repeating an action never
// raises a stat. The 6 (a gold record) stays feat-gated and isn't reachable this way. Every change goes on a ledger with its
// date and reason, so later channels (convictions) write to the same place.
//
// The profile is the run's own copy: the archetype's table is shared static data and is never written. Player-only.
// ============================================================================================
public partial class PlayerDesk {
	/// <summary>The highest a December point can raise an instinct. 6 needs a career feat (§4.3), not a point.</summary>
	public const int InstinctPointCap = 5;

	public sealed class InstinctChange {
		public GameDate Date;
		public ExecutiveVoice Voice;
		public int From, To;
		public string Why;
	}

	/// <summary>December points earned and not yet spent.</summary>
	public int InstinctPoints { get; private set; }
	private readonly List<InstinctChange> instinctLedger = new();
	public IReadOnlyList<InstinctChange> InstinctLedger => instinctLedger;

	private static ExecutiveInstinctProfile CopyInstincts(ExecutiveInstinctProfile p) => new() {
		TheEar = p?.TheEar ?? 1, TheStreet = p?.TheStreet ?? 1, TheSuit = p?.TheSuit ?? 1, TheFixer = p?.TheFixer ?? 1,
	};

	/// <summary>How far off an instinct can be on one hearing: the spread of a read before the room and partial coverage
	/// add their own. The instinct is the main part of the fog (signing audit §3.11): a read lands in the wrong word band
	/// (under .48 / .48–.72 / from .72) about 31% of the time at 1, 16% at 3 and 4% at 5. The blind spot's direction
	/// (keyed per label and axis) stays put as a point narrows it. Instincts in the fog (inner-life §7). Player-only.</summary>
	public static float InstinctNoise(int score) => score switch { <= 1 => .45f, 2 => .33f, 3 => .22f, 4 => .13f, 5 => .06f, _ => .03f };
	/// <summary>The Ear hears the music: voice, playing, material, and the songs.</summary>
	public float EarNoise => InstinctNoise(InstinctProfile?.TheEar ?? 3);
	/// <summary>The Street reads the room: how the crowd takes the act (stage presence).</summary>
	public float StreetNoise => InstinctNoise(InstinctProfile?.TheStreet ?? 3);

	public static int InstinctScore(ExecutiveInstinctProfile p, ExecutiveVoice v) => v switch {
		ExecutiveVoice.Ear => p.TheEar, ExecutiveVoice.Street => p.TheStreet, ExecutiveVoice.Suit => p.TheSuit, ExecutiveVoice.Fixer => p.TheFixer, _ => 0,
	};

	private static void SetInstinct(ExecutiveInstinctProfile p, ExecutiveVoice v, int value) {
		switch (v) {
			case ExecutiveVoice.Ear: p.TheEar = value; break;
			case ExecutiveVoice.Street: p.TheStreet = value; break;
			case ExecutiveVoice.Suit: p.TheSuit = value; break;
			case ExecutiveVoice.Fixer: p.TheFixer = value; break;
		}
	}

	public static readonly ExecutiveVoice[] InstinctVoices = { ExecutiveVoice.Ear, ExecutiveVoice.Street, ExecutiveVoice.Suit, ExecutiveVoice.Fixer };

	/// <summary>Whether a point can still go anywhere: some instinct is below the cap.</summary>
	private bool AnyInstinctBelowCap => InstinctVoices.Any(v => InstinctScore(InstinctProfile, v) < InstinctPointCap);

	/// <summary>A sitting rolled the lien: a year survived earns a point, if there's anywhere left to spend it.</summary>
	private void AwardDecemberPoint(int year) {
		if (!AnyInstinctBelowCap) return;
		InstinctPoints++;
		Note($"You got through {year}'s sitting. That's a point to put into one of your instincts: the OFFICE tab, under your name.");
	}

	/// <summary>Why a point can't go on this instinct, or null.</summary>
	public string InstinctPointBlock(ExecutiveVoice v) {
		if (InstinctPoints <= 0) return "No point to spend.";
		if (!InstinctVoices.Contains(v)) return "That isn't an instinct.";
		if (InstinctScore(InstinctProfile, v) >= InstinctPointCap) return $"Already {InstinctPointCap}. A 6 takes a gold record, not a point.";
		return null;
	}

	/// <summary>Spend a December point: one instinct up by one, on the ledger.</summary>
	public bool SpendInstinctPoint(ExecutiveVoice v, out string message) {
		message = InstinctPointBlock(v);
		if (message != null) return false;
		int from = InstinctScore(InstinctProfile, v);
		SetInstinct(InstinctProfile, v, from + 1);
		InstinctPoints--;
		instinctLedger.Add(new InstinctChange { Date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate, Voice = v, From = from, To = from + 1, Why = "December point" });
		message = v switch {
			ExecutiveVoice.Ear => $"The Ear: {from} to {from + 1}. A year of listening to what sold and what didn't.",
			ExecutiveVoice.Street => $"The Street: {from} to {from + 1}. You know more people than you did a year ago, and more of them know you.",
			ExecutiveVoice.Suit => $"The Suit: {from} to {from + 1}. A year of reading paper you didn't write.",
			_ => $"The Fixer: {from} to {from + 1}. You've seen how things actually get done.",
		};
		Note(message);
		Changed?.Invoke();
		return true;
	}

	// ── save ───────────────────────────────────────────────────────────────────────────────────────

	private List<InstinctChangeSaveData> CaptureInstinctLedger() =>
		instinctLedger.Select(c => new InstinctChangeSaveData { Date = DateSave.From(c.Date), Voice = (int)c.Voice, From = c.From, To = c.To, Why = c.Why }).ToList();

	private void RestoreInstinctGrowth(int points, List<InstinctChangeSaveData> ledger) {
		InstinctPoints = System.Math.Max(0, points);
		instinctLedger.Clear();
		foreach (var c in ledger ?? new())
			if (c?.Date?.Valid == true) instinctLedger.Add(new InstinctChange { Date = c.Date.ToDate(), Voice = (ExecutiveVoice)c.Voice, From = c.From, To = c.To, Why = c.Why });
	}

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────

	internal void SetInstinctPointsForChecks(int points) => InstinctPoints = points;
	internal (int Points, List<InstinctChangeSaveData> Ledger) CaptureInstinctsForChecks() => (InstinctPoints, CaptureInstinctLedger());
	internal void RestoreInstinctsForChecks(int points, List<InstinctChangeSaveData> ledger) => RestoreInstinctGrowth(points, ledger);
}

/// <summary>One instinct change (§4.3): when, which, from and to, and why. Absent in older saves.</summary>
public sealed class InstinctChangeSaveData {
	public DateSave Date { get; set; }
	public int Voice { get; set; }
	public int From { get; set; }
	public int To { get; set; }
	public string Why { get; set; }
}
