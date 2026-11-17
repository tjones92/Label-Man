using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (LpGenreAllocationFollowup.md §50-51), off unless named.
/// --major-newcomer-package=F moves the launch package of a NewSigning single at a Major-tier label toward an
/// Independent's.
///
/// Under --historical-majors the 1961 Big-5 Top 10 excess is mostly new Big-5 hit acts: 16 points of slots against
/// history's 4. At equal quality a Big-5 newcomer converts like a MidTier one and at about 2.3x an Independent's rate,
/// and about 80% of that gap is the launch package: campaign impact 0.55 against 0.36, and a first run of 5.5k against 3.3k.
/// History's majors put promotion behind roster acts and gave unknowns a release without a campaign. This probe moves
/// that allocation and nothing else:
/// - Launch campaign impact: <c>impact' = Ref + F * (impact - Ref)</c> above <see cref="ImpactRef"/>, unchanged below.
///   It is read by the launch radio push and the launch marketing-awareness term only.
/// - Launch stock: the career stock scale is multiplied by <c>StockFloor + (1 - StockFloor) * F</c>.
///
/// Coverage (which regions a record reaches), weekly push, reorders and every non-newcomer or non-Major launch are left as
/// they are. No random draw is added, so a run without the switch is byte-identical.
/// </summary>
public static class MajorNewcomerPackage {
	public const string SwitchPrefix = "--major-newcomer-package=";
	// Independent mean launch campaign impact under the hm104 bundle (1961 newcomer launches, 0.36-0.37).
	public const float ImpactRef = 0.36f;
	// Independent / Big-5 median newcomer first run, 1960-61 (3.3k / 5.5k).
	public const float StockFloor = 0.60f;
	private static bool configured;
	private static float factor = 1f;
	public static float Factor => factor;
	public static bool Enabled => factor < 1f;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var values = (arguments ?? Array.Empty<string>()).Where(arg => arg.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name --major-newcomer-package once.");
		if (values.Length == 1) {
			string raw = values[0].Substring(SwitchPrefix.Length);
			if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || !(parsed >= 0f && parsed <= 1f))
				throw new ArgumentException("--major-newcomer-package needs a factor in [0, 1].");
			factor = parsed;
		}
		configured = true;
	}

	internal static void SetFactorForChecks(float value) { configured = true; factor = value; }

	/// <summary>True for a live AI single by a NewSigning act at a Major-tier label while the switch is on.</summary>
	public static bool Applies(AILabel label, SimulatedArtist artist, bool isAlbum) =>
		factor < 1f && !isAlbum && label != null && !label.isPlayerOwned && label.tier == LabelTier.Major &&
		artist != null && artist.careerState == CareerState.NewSigning;

	/// <summary>Identity when off, when not applicable, or at or below the reference.</summary>
	public static float Impact(float impact, bool applies) =>
		!applies || factor >= 1f || impact <= ImpactRef ? impact : Mathf.Clamp(ImpactRef + factor * (impact - ImpactRef), 0f, 1f);

	/// <summary>The launch stock-scale multiplier; exactly 1 when off or not applicable.</summary>
	public static float StockScale(bool applies) => !applies || factor >= 1f ? 1f : StockFloor + (1f - StockFloor) * factor;
}
