using Godot;

/// <summary>
/// Motive. Five pressures and the resistance that opposes them, all read from state that
/// already exists on the artist, the lineup and the label.
/// <para>
/// Pressure feeds exactly two things and nothing else: the ratification threshold, and a
/// bounded artist-side reweight inside the existing supply weight. It never picks a genre
/// and never adds or removes a release, so at the neutral setting -- unpressured act, or
/// the phase switched off -- the simulation reproduces Phase 1 exactly.
/// </para>
/// </summary>
public static class ArtistEvolutionPressureService {
	/// <summary>
	/// Two flops is the point at which a 1960s label starts having opinions about what the
	/// next side sounds like, and it is the same streak the drop rules already turn on.
	/// </summary>
	public const int FlopStreakForPressure = 2;

	public static void Evaluate(SimulatedArtist artist, AILabel label, int year) {
		ArtistEvolutionProfile profile = artist.evolution;
		if (profile == null) return;

		profile.commercialPressure = Commercial(artist);
		profile.artisticPressure = Artistic(artist, profile);
		profile.peerPressure = Peer(profile, year);
		profile.labelPressure = Label(artist, profile, label);
		profile.internalPressure = Internal(artist, profile);
		profile.resistance = Resistance(artist, profile);

		float net = Mathf.Max(Mathf.Max(profile.commercialPressure, profile.artisticPressure),
			Mathf.Max(Mathf.Max(profile.peerPressure, profile.labelPressure), profile.internalPressure));
		profile.restlessness = Mathf.Clamp(net - profile.resistance, 0f, 1f);
		profile.dominantTrigger = Dominant(profile);
		// The band that strips back to blues after two failed pop singles is this same
		// mechanism running backwards, and it is half of what makes the arc feel authored.
		profile.rootsMode = profile.rootsAttachment >= .55f &&
			profile.commercialPressure >= profile.artisticPressure &&
			profile.commercialPressure >= profile.peerPressure;
		profile.confidence = Mathf.Clamp(.5f + .5f * artist.momentum - .4f * profile.commercialPressure, 0f, 1f);
		profile.frustration = Mathf.Clamp(profile.commercialPressure * .6f + profile.internalPressure * .4f, 0f, 1f);
		profile.lastReleaseIntent = Intent(profile);
	}

	private static float Commercial(SimulatedArtist artist) {
		float streak = Mathf.Clamp(
			Mathf.Max(artist.consecutiveFlops, artist.contractConsecutiveFlops) / (float)(FlopStreakForPressure * 2), 0f, 1f);
		float cold = 1f - Mathf.Clamp(artist.momentum, 0f, 1f);
		float state = artist.careerState switch {
			CareerState.Declining => .40f,
			CareerState.NewSigning => .25f,
			CareerState.Superstar or CareerState.Star => 0f,
			_ => .10f
		};
		return Mathf.Clamp(.50f * streak + .30f * cold + state, 0f, 1f);
	}

	/// <summary>
	/// Wanting to make an important record. criticalAcclaim is a real signal now that it has
	/// a writer -- an act the critics rate has both the appetite and the licence to reach.
	/// </summary>
	private static float Artistic(SimulatedArtist artist, ArtistEvolutionProfile profile) => Mathf.Clamp(
		.45f * profile.artisticAmbition + .30f * profile.conceptualThinking +
		.25f * Mathf.Clamp(artist.criticalAcclaim, 0f, 1f), 0f, 1f);

	/// <summary>
	/// Other people's records. Influence memories are written by the Phase-4 landmark loop;
	/// with that phase off this term is zero, which is exactly the neutral case.
	/// </summary>
	private static float Peer(ArtistEvolutionProfile profile, int year) {
		if (profile.influences == null || profile.influences.Count == 0) return 0f;
		float strongest = 0f;
		foreach (ArtistInfluenceMemory memory in profile.influences) {
			float age = Mathf.Clamp(1f - (year - memory.year) / 3f, 0f, 1f);
			strongest = Mathf.Max(strongest, memory.strength * age);
		}
		return Mathf.Clamp(strongest * profile.peerSensitivity, 0f, 1f);
	}

	/// <summary>
	/// The label leaning on the act. For the player's roster this is a directive the player
	/// set: AI acts evolve autonomously and the player's influence on them is this term, not
	/// a veto. For AI labels it is derived from how much rope the label has and how badly
	/// the act is doing.
	/// </summary>
	private static float Label(SimulatedArtist artist, ArtistEvolutionProfile profile, AILabel label) {
		if (artist.isPlayerOwned) return Mathf.Clamp(profile.labelPressureDirective, 0f, 1f);
		if (label == null) return 0f;
		float impatience = 1f - Mathf.Clamp(label.artistLoyalty, 0f, 1f);
		float appetite = Mathf.Clamp(label.riskTolerance, 0f, 1f);
		float failing = Mathf.Clamp(artist.consecutiveFlops / (float)(FlopStreakForPressure * 2), 0f, 1f);
		return Mathf.Clamp(.55f * impatience * failing + .25f * appetite * failing, 0f, 1f);
	}

	private static float Internal(SimulatedArtist artist, ArtistEvolutionProfile profile) => Mathf.Clamp(
		.55f * profile.volatility + .45f * (1f - Mathf.Clamp(artist.groupCohesion, 0f, 1f)), 0f, 1f);

	/// <summary>
	/// A settled star with three hits does not wander. Reputation, cohesion and attachment
	/// to the original sound are what hold an act still.
	/// </summary>
	private static float Resistance(SimulatedArtist artist, ArtistEvolutionProfile profile) {
		float settled = artist.careerState switch {
			CareerState.Superstar => .32f,
			CareerState.Star => .26f,
			CareerState.Established => .16f,
			_ => 0f
		};
		return Mathf.Clamp(.40f * profile.rootsAttachment + .22f * Mathf.Clamp(artist.reputation, 0f, 1f) +
			.18f * Mathf.Clamp(artist.groupCohesion, 0f, 1f) + settled, 0f, 1f);
	}

	private static ArtistEvolutionTrigger Dominant(ArtistEvolutionProfile profile) {
		float best = profile.commercialPressure;
		ArtistEvolutionTrigger trigger = ArtistEvolutionTrigger.CommercialFailure;
		if (profile.artisticPressure > best) { best = profile.artisticPressure; trigger = ArtistEvolutionTrigger.PersonalAmbition; }
		if (profile.peerPressure > best) { best = profile.peerPressure; trigger = ArtistEvolutionTrigger.PeerInfluence; }
		if (profile.labelPressure > best) { best = profile.labelPressure; trigger = ArtistEvolutionTrigger.LabelPressure; }
		if (profile.internalPressure > best) { best = profile.internalPressure; trigger = ArtistEvolutionTrigger.InternalTension; }
		return best <= 0f ? ArtistEvolutionTrigger.None : trigger;
	}

	/// <summary>
	/// A derived LABEL for what the release was reaching for, not an input that changes what
	/// gets made. It is flavor with a paper trail; promoting it to a causal input is a
	/// separate question with its own gate.
	/// </summary>
	private static ReleaseCreativeIntent Intent(ArtistEvolutionProfile profile) {
		if (profile.rootsMode) return ReleaseCreativeIntent.ReturnToRoots;
		return profile.dominantTrigger switch {
			ArtistEvolutionTrigger.CommercialFailure or ArtistEvolutionTrigger.LabelPressure => ReleaseCreativeIntent.ChaseHit,
			ArtistEvolutionTrigger.PeerInfluence => ReleaseCreativeIntent.FollowPeer,
			ArtistEvolutionTrigger.PersonalAmbition => profile.conceptualThinking >= .60f
				? ReleaseCreativeIntent.Statement : ReleaseCreativeIntent.Experiment,
			ArtistEvolutionTrigger.InternalTension => ReleaseCreativeIntent.Experiment,
			_ => ReleaseCreativeIntent.Consolidate
		};
	}

	/// <summary>
	/// The player's lever, and the whole of the player's influence over an act's direction.
	/// Acts still evolve autonomously; this only leans on them.
	/// </summary>
	public static void SetLabelPressure(SimulatedArtist artist, float amount) {
		if (artist?.evolution == null) return;
		artist.evolution.labelPressureDirective = Mathf.Clamp(amount, 0f, 1f);
	}
}
