using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

/// <summary>
/// Screen probe (SimTools/MediaTvHeadlinerBookingDesign.md, "Why free agents with hits are not signed again"), off
/// unless named.
///
/// An AI label scores an experienced free agent on the same quality and genre terms as a fresh prospect, plus
/// <c>momentum × 0.5 + reputation × 0.3</c>, and halves the score toward <c>riskTolerance</c> when reputation is below
/// 0.1. The fresh lane has no such penalty, and lifetime Top 40s are not read, so a free agent with a hit history loses
/// the slate to an unknown in 97-99% of appointments. History's labels picked such names up when their old deals ended.
/// <list type="bullet">
/// <item><c>--hit-history-signing</c>: an act with a lifetime Top 40 is exempt from the low-reputation penalty.</item>
/// <item><c>--hit-history-signing=B</c>: also adds B per lifetime Top 40, capped at <see cref="MaxCountedHits"/> hits.</item>
/// <item><c>--hit-history-signing-recovery</c>: see <see cref="RecoveryCompetes"/>. Independent of the scoring switch.</item>
/// </list>
/// Label-blind and draws no RNG. Off: every hook is the identity.
/// </summary>
public static class HitHistorySigning {
	public const string Switch = "--hit-history-signing";
	public const int MaxCountedHits = 2;

	public const string RecoverySwitch = "--hit-history-signing-recovery";

	private static bool configured;
	private static bool enabled;
	private static bool recovery;
	private static float bonusPerHit;
	public static bool Enabled => enabled;
	public static float BonusPerHit => bonusPerHit;
	/// <summary>A label in Recovery takes a qualifying fresh prospect without looking at the experienced lane. With this
	/// on, the best-weighted free agent with a hit history competes with that pick on score.</summary>
	public static bool RecoveryCompetes => recovery;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		recovery = (arguments ?? Array.Empty<string>()).Contains(RecoverySwitch);
		string[] matches = (arguments ?? Array.Empty<string>()).Where(a => a == Switch || a.StartsWith(Switch + "=", StringComparison.Ordinal)).ToArray();
		if (matches.Length > 1) throw new ArgumentException("Name --hit-history-signing once.");
		enabled = matches.Length == 1;
		bonusPerHit = 0f;
		if (enabled && matches[0].Length > Switch.Length) {
			bonusPerHit = float.Parse(matches[0][(Switch.Length + 1)..], CultureInfo.InvariantCulture);
			if (!(bonusPerHit >= 0f && bonusPerHit <= 0.5f)) throw new ArgumentException("--hit-history-signing=B needs 0 <= B <= 0.5.");
		}
		configured = true;
	}

	internal static void SetForChecks(bool on, float bonus = 0f, bool recoveryOn = false) { configured = true; enabled = on; bonusPerHit = bonus; recovery = recoveryOn; }

	/// <summary>True when the low-reputation penalty applies; always true when off.</summary>
	public static bool AppliesLowReputationPenalty(SimulatedArtist artist) => !enabled || artist == null || artist.top40Hits <= 0;

	/// <summary>The score added for a hit history; 0 when off.</summary>
	public static float HistoryBonus(SimulatedArtist artist) =>
		!enabled || artist == null || artist.top40Hits <= 0 ? 0f : bonusPerHit * Math.Min(artist.top40Hits, MaxCountedHits);
}
