using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gate fix (LpGenreAllocationFollowup.md §54-55), off unless named.
/// --zero-fee-material-gate: a song that costs the label nothing (a royalty licence under --writing-market-royalty-licences,
/// a cover's rehearsal) is not refused because the label sits under its going-concern reserve. The material cash test
/// is <c>cash − reserve &lt; fee</c>; with a fee of 0 it read <c>cash &lt; reserve</c>, which refused a free song and sent the
/// act back to its own song or a cover. §54: 46% of MidTier and 47% of Independent hit-rung searches stopped there.
/// Applies to every AI label; the player's test already ignores the reserve. A fee above 0 still needs the cash.
/// No random number is drawn. Off: <see cref="Clears"/> is false and every test is unchanged.
/// </summary>
public static class ZeroFeeMaterialGate {
	public const string Switch = "--zero-fee-material-gate";
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		if (Enabled) Godot.GD.Print("ZERO_FEE_MATERIAL_GATE on");
		configured = true;
	}

	private static void EnsureConfigured() { if (!configured) Configure(SimulationArguments.Get()); }
	internal static void SetForChecks(bool on) { configured = true; Enabled = on; }

	/// <summary>Whether a zero fee clears the label's cash test regardless of its reserve. False when off.</summary>
	public static bool Clears(AILabel label, float fee) {
		EnsureConfigured();
		return Enabled && fee <= 0f && label != null && !label.isPlayerOwned && label.IsActive;
	}
}
