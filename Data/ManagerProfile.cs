using System.Linq;
using Godot;

/// <summary>
/// The artist's manager - the gatekeeper the label negotiates with. Stamped on
/// <see cref="SimulatedArtist"/> at generation; the effects are immutable static-modifier lookups
/// (see <see cref="ManagerProfile"/>), never an active agent that ticks.
/// </summary>
public enum ManagerArchetype {
	None,            // unmanaged - easiest to sign, no modifiers (most early-career artists)
	LocalHustler,    // easy negotiation, eager to deal; weak national reach
	Shark,           // brutal advance/royalty demands; relentless promotion (chart visibility)
	Svengali,        // demands LABEL creative control; lifts production/hook
	Visionary        // demands ARTIST creative control + publishing; grants prestige
}

/// <summary>
/// Immutable static modifiers looked up by archetype - never an agent, never ticks. Per the
/// codebase's "do not simulate managers as active agents" discipline: a lookup table. Contract
/// logic reads it when a deal is negotiated; passive career auras read it later.
/// </summary>
public static class ManagerProfile {
	public readonly struct Modifiers {
		public readonly float AdvanceDemandMult;      // scales the advance the manager demands
		public readonly float RoyaltyDemandMult;      // scales the royalty they hold out for
		public readonly float NegotiationDifficulty;  // 0 easy .. 1 brutal (drives future counter-offer cost)
		public readonly float MomentumAura;           // passive artist momentum while managed
		public readonly float ChartVisibilityAura;    // passive push (Shark's promotion machine)
		public readonly float ProductionBonus;        // Svengali - lifts realized record quality
		public readonly float PrestigeBonus;          // Visionary - critical prestige (stored for later)
		public readonly bool DemandsArtistControl;    // creative-control axis default
		public readonly bool DemandsArtistPublishing; // publishing axis default

		public Modifiers(float adv, float roy, float diff, float mom, float vis, float prod, float prestige,
			bool artistCtrl, bool artistPub) {
			AdvanceDemandMult = adv; RoyaltyDemandMult = roy; NegotiationDifficulty = diff;
			MomentumAura = mom; ChartVisibilityAura = vis; ProductionBonus = prod; PrestigeBonus = prestige;
			DemandsArtistControl = artistCtrl; DemandsArtistPublishing = artistPub;
		}
	}

	/// <summary>What the manager does for the label and what they cost it, in the player's terms. Read
	/// straight off <see cref="Of"/> so the explainer can never drift from the numbers the sim uses.</summary>
	public static (string Role, string Helps, string Costs) Describe(ManagerArchetype archetype) {
		Modifiers m = Of(archetype);
		string Pct(float mult) => $"{Mathf.Abs(mult - 1f) * 100f:0}%";
		string advance = m.AdvanceDemandMult > 1f ? $"asks {Pct(m.AdvanceDemandMult)} more up front" : m.AdvanceDemandMult < 1f ? $"asks {Pct(m.AdvanceDemandMult)} less up front" : "";
		string royalty = m.RoyaltyDemandMult > 1f ? $"holds out for {Pct(m.RoyaltyDemandMult)} more on the royalty" : m.RoyaltyDemandMult < 1f ? $"takes {Pct(m.RoyaltyDemandMult)} less on the royalty" : "";
		string money = string.Join(" and ", new[] { advance, royalty }.Where(part => part.Length > 0));
		string push = m.ChartVisibilityAura >= 0.15f ? "a strong, constant push on the act's chart visibility"
			: m.ChartVisibilityAura > 0f ? "a small lift to the act's chart visibility" : "";
		switch (archetype) {
			case ManagerArchetype.LocalHustler:
				return ("Local hustler",
					$"Eager to deal and easy to sit across from: {money}. Plus {push}.",
					"Local only. He has no weight outside the home scene.");
			case ManagerArchetype.Shark:
				return ("Shark",
					$"Works the act's promotion hard: {push}.",
					$"Brutal at the table: {money}, on a short term so he can renegotiate from strength.");
			case ManagerArchetype.Svengali:
				return ("Svengali",
					$"Sharpens the records: about {m.ProductionBonus * 100f:0} points on realized production quality, and he {royalty}.",
					"Wants the reins: a long exclusive term, and hard bargaining over the length of the deal.");
			case ManagerArchetype.Visionary:
				return ("Visionary",
					$"Protects the artist's interests and adds {push}.",
					$"Expensive: {money}. The publishing stays in the act's name, and no advance changes that. They also hold the final word on material.");
			default:
				return ("Unmanaged", "Nobody between you and the act: easy to sign.", "Nobody pushing their records for them, either.");
		}
	}

	public static Modifiers Of(ManagerArchetype archetype) => archetype switch {
		ManagerArchetype.LocalHustler => new(0.8f, 0.9f, 0.2f, 0.05f, 0.05f, 0f, 0f, false, false),
		ManagerArchetype.Shark        => new(2.5f, 1.6f, 0.9f, 0.15f, 0.20f, 0f, 0f, false, false),
		ManagerArchetype.Svengali     => new(1.0f, 0.8f, 0.6f, 0f, 0.05f, 0.15f, 0f, false, false),
		ManagerArchetype.Visionary    => new(1.2f, 1.3f, 0.7f, 0f, 0.10f, 0f, 0.20f, true, true),
		_                             => new(1.0f, 1.0f, 0.0f, 0f, 0f, 0f, 0f, false, false)
	};
}
