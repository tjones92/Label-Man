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

	public static Modifiers Of(ManagerArchetype archetype) => archetype switch {
		ManagerArchetype.LocalHustler => new(0.8f, 0.9f, 0.2f, 0.05f, 0.05f, 0f, 0f, false, false),
		ManagerArchetype.Shark        => new(2.5f, 1.6f, 0.9f, 0.15f, 0.20f, 0f, 0f, false, false),
		ManagerArchetype.Svengali     => new(1.0f, 0.8f, 0.6f, 0f, 0.05f, 0.15f, 0f, false, false),
		ManagerArchetype.Visionary    => new(1.2f, 1.3f, 0.7f, 0f, 0.10f, 0f, 0.20f, true, true),
		_                             => new(1.0f, 1.0f, 0.0f, 0f, 0f, 0f, 0f, false, false)
	};
}
