using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Calibration probe (LpGenreAllocationFollowup.md §53-54), off unless named.
/// --midtier-hit-act-package=PARTS gives a hot act (a Top 40 within <see cref="HitActCadence.HotWindowWeeks"/> weeks, the
/// hit-act clock) at a MidTier AI label the follow-up treatment a head firm gives a proven act. PARTS is a comma list of
/// <c>cadence</c>, <c>craft</c> and <c>material</c>, or <c>all</c>:
/// <list type="bullet">
/// <item>cadence: the label's hot acts are due a follow-up <see cref="DueWeeks"/> weeks after their last single, not 13
/// (<see cref="HitActCadence.DueWeeksFor"/>; the lane, the slate's due pick, the work queue and the review all read it).</item>
/// <item>craft: the single's label-craft term reads the Major reference (<see cref="FootprintCraftSoftening.CraftReference"/>):
/// the label's best studio, arranger and session players on its proven act. The act's own terms and the draw are unchanged.</item>
/// <item>material: a professional licence or cover that costs the label nothing (royalty licences, a cover's rehearsal)
/// is not refused because the label sits under its going-concern reserve. §53: MidTier hit acts' song searches stop on
/// "cash" 46% of the time, and under --writing-market-royalty-licences the fee is 0, so the test was cash − reserve &lt; 0.
/// A fee above 0 still needs the cash.</item>
/// </list>
/// Majors, Independents, Small labels, the player and every cold act are untouched. No random number is drawn.
/// Off: every hook returns its input.
/// </summary>
public static class MidTierHitActPackage {
	public const string SwitchPrefix = "--midtier-hit-act-package=";
	public const int DueWeeks = 8;
	private static bool configured;
	public static bool Cadence { get; private set; }
	public static bool Craft { get; private set; }
	public static bool Material { get; private set; }
	public static bool Enabled => Cadence || Craft || Material;
	public static int ZeroFeeReads { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Cadence = Craft = Material = false;
		var values = (arguments ?? Array.Empty<string>()).Where(a => a.StartsWith(SwitchPrefix, StringComparison.Ordinal)).ToArray();
		if (values.Length > 1) throw new ArgumentException("Name " + SwitchPrefix.TrimEnd('=') + " once.");
		if (values.Length == 1) {
			foreach (string part in values[0].Substring(SwitchPrefix.Length).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				switch (part) {
					case "all": Cadence = Craft = Material = true; break;
					case "cadence": Cadence = true; break;
					case "craft": Craft = true; break;
					case "material": Material = true; break;
					default: throw new ArgumentException(SwitchPrefix.TrimEnd('=') + " takes cadence, craft, material or all.");
				}
			}
			if (!Enabled) throw new ArgumentException(SwitchPrefix.TrimEnd('=') + " needs at least one part.");
			Godot.GD.Print($"MIDTIER_HIT_ACT_PACKAGE cadence={Cadence} craft={Craft} material={Material} dueWeeks={DueWeeks}");
		}
		configured = true;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }

	internal static void SetForChecks(bool cadence, bool craft, bool material) {
		configured = true; Cadence = cadence; Craft = craft; Material = material; ZeroFeeReads = 0;
	}

	public static bool InScope(AILabel label) => label != null && !label.isPlayerOwned && label.tier == LabelTier.MidTier;

	private static bool Hot(SimulatedArtist artist, int week) {
		if (artist == null || week <= 0) return false;
		if (!HitActCadence.ClockEnabled)
			throw new InvalidOperationException(SwitchPrefix.TrimEnd('=') + " reads the hit-act Top 40 clock (the release programme or a hit-act cadence switch).");
		return HitActCadence.WeeksSinceTop40(artist, week) <= HitActCadence.HotWindowWeeks;
	}

	/// <summary>cadence: the label's due interval, or <paramref name="dueWeeks"/> unchanged.</summary>
	public static int DueWeeksFor(AILabel label, int dueWeeks) {
		EnsureConfigured();
		return Cadence && InScope(label) ? Math.Min(dueWeeks, DueWeeks) : dueWeeks;
	}

	/// <summary>craft: the label-craft read for this single, or <paramref name="read"/> unchanged.</summary>
	public static float CraftRead(float read, ReleaseFormat format, AILabel label, SimulatedArtist artist, int week) {
		EnsureConfigured();
		if (!Craft || format != ReleaseFormat.Single || read >= FootprintCraftSoftening.CraftReference || !InScope(label) || !Hot(artist, week)) return read;
		return FootprintCraftSoftening.CraftReference;
	}

	/// <summary>material: whether a zero-fee song for this act clears the label's cash test regardless of its reserve.</summary>
	public static bool ClearsZeroFee(AILabel label, SimulatedArtist artist, float fee) {
		EnsureConfigured();
		return Material && ClearsZeroFee(label, artist, fee, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
	}

	internal static bool ClearsZeroFee(AILabel label, SimulatedArtist artist, float fee, int week) {
		if (!Material || !(fee <= 0f) || label?.IsActive != true || !InScope(label)) return false;
		if (!Hot(artist, week)) return false;
		ZeroFeeReads++;
		return true;
	}
}
