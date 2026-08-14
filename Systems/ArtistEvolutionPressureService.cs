using Godot;

/// <summary>
/// Motive. Six pressures and the resistance that opposes them, all read from state that
/// already exists on the artist, the lineup, the label and the industry's shared memory.
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

	// ---- SALIENCE ------------------------------------------------------------------------------
	// What counts as a LOUD reading of each pressure, used to decide which motive won.
	//
	// This exists because the first cut compared the six raw floats with a max(), and they
	// are not on one scale: commercial pressure is a sum of three near-saturated terms while
	// peer pressure is a product of five sub-unit factors. Measured over 8,371 observations
	// the means came out commercial .748, internal .436, artistic .410, label .170, peer
	// .0021 -- so a raw max() was not comparing motives, it was comparing formula shapes,
	// and it returned CommercialFailure for 92% of conversions on that basis alone.
	//
	// Dividing by salience asks the honest question instead: which pressure is unusually
	// high FOR THAT PRESSURE.
	//
	// Sized against the rebuilt formulas as measured over a 1960-62 run: commercial .429,
	// artistic .449, internal .434, peer .308 (before the hit/landmark weight split), label
	// .291, critical .080. Each scale sits near its own mean, so a pressure at its typical
	// value scores near 1.0 and it is the DEVIATION that decides the motive. Commercial sits
	// slightly below its mean on purpose: most acts really are chasing a hit, and the point
	// of this file is to stop that being the ONLY thing anyone is ever doing -- not to
	// pretend it is rare.
	public const float CommercialSalience = .45f;
	public const float ArtisticSalience = .52f;
	// Below its mean, because acclaim is near zero for almost everyone and the acts that have
	// any are exactly the ones this motive is for.
	public const float CriticalSalience = .30f;
	public const float PeerSalience = .30f;
	// Below its mean for the same reason as the critical scale: the acts a label is actually
	// leaning on are the tail, not the middle. At .42 it produced 2 conversions in three
	// years, which is not meaningfully different from the dead lever it replaced.
	public const float LabelSalience = .36f;
	public const float InternalSalience = .58f;

	public static void Evaluate(SimulatedArtist artist, AILabel label, int year) {
		ArtistEvolutionProfile profile = artist.evolution;
		if (profile == null) return;

		profile.commercialPressure = Commercial(artist);
		profile.artisticPressure = Artistic(artist, profile);
		profile.criticalPressure = Critical(artist, profile);
		profile.peerPressure = Peer(profile, year);
		profile.labelPressure = Label(artist, profile, label, year);
		profile.internalPressure = Internal(artist, profile);
		profile.resistance = Resistance(artist, profile);

		// WHETHER an act is restless is a question about raw magnitude, so it is answered
		// from the unnormalised pressures. WHICH motive won is a question about relative
		// loudness and is answered below by Dominant(). Keeping them apart is deliberate:
		// normalising here would inflate every act's restlessness and quietly raise the
		// conversion rate while pretending to only relabel it.
		float net = Mathf.Max(Mathf.Max(Mathf.Max(profile.commercialPressure, profile.artisticPressure),
				Mathf.Max(profile.peerPressure, profile.labelPressure)),
			Mathf.Max(profile.internalPressure, profile.criticalPressure));
		profile.restlessness = Mathf.Clamp(net - profile.resistance, 0f, 1f);
		profile.dominantTrigger = Dominant(profile, out float dominantSalience);
		profile.dominantSalience = dominantSalience;
		// The band that strips back to blues after two failed pop singles is this same
		// mechanism running backwards, and it is half of what makes the arc feel authored.
		profile.rootsMode = profile.rootsAttachment >= .55f &&
			profile.commercialPressure >= profile.artisticPressure &&
			profile.commercialPressure >= profile.peerPressure;
		profile.confidence = Mathf.Clamp(.5f + .5f * artist.momentum - .4f * profile.commercialPressure, 0f, 1f);
		profile.frustration = Mathf.Clamp(profile.commercialPressure * .6f + profile.internalPressure * .4f, 0f, 1f);
		profile.lastReleaseIntent = Intent(profile);
		profile.acclaimAtLastProject = Mathf.Clamp(artist.criticalAcclaim, 0f, 1f);
	}

	/// <summary>
	/// The fear of not selling. The FLOP STREAK is the whole spine of it: a cold act with a
	/// precarious career is more frightened when the records miss, but an act whose records
	/// have not missed is not under commercial pressure however anonymous it is.
	/// <para>
	/// This was <c>.50*streak + .30*cold + state</c>, which gave every act a ~0.40 floor
	/// before a single flop -- momentum sits near zero for almost everyone, because careers
	/// here are two records long and most never chart. That floor was not a measurement of
	/// commercial desperation; it was a constant with a flop-shaped wiggle on top, and it
	/// beat all five other motives on a raw max() essentially always.
	/// </para>
	/// </summary>
	private static float Commercial(SimulatedArtist artist) {
		float streak = Mathf.Clamp(
			Mathf.Max(artist.consecutiveFlops, artist.contractConsecutiveFlops) / (float)(FlopStreakForPressure * 2), 0f, 1f);
		if (streak <= 0f) return 0f;
		float cold = 1f - Mathf.Clamp(artist.momentum, 0f, 1f);
		float exposure = artist.careerState switch {
			CareerState.Declining => 1f,
			CareerState.NewSigning => .70f,
			CareerState.Superstar or CareerState.Star => .10f,
			_ => .40f
		};
		return Mathf.Clamp(streak * (.60f + .25f * cold + .15f * exposure), 0f, 1f);
	}

	/// <summary>
	/// Wanting to make an important record. This is disposition, not circumstance: it is
	/// what the act is like, and it is why an ambitious band with a conceptual writer
	/// wanders even while the records are selling.
	/// </summary>
	private static float Artistic(SimulatedArtist artist, ArtistEvolutionProfile profile) => Mathf.Clamp(
		.50f * profile.artisticAmbition + .35f * profile.conceptualThinking +
		.15f * Mathf.Clamp(artist.criticalAcclaim, 0f, 1f), 0f, 1f);

	/// <summary>
	/// Being taken seriously, and wanting more of it. The Pet Sounds shape: standing with
	/// the critics that is RISING while the singles are not, which is the one motive the
	/// model had no way to express and the reason CriticalBreakthrough never fired.
	/// <para>
	/// Reads the acclaim field only -- it is deliberately not gated on a press system
	/// existing. When one does, it raises acclaim through its own writer and this term picks
	/// the change up with no edit here.
	/// </para>
	/// </summary>
	private static float Critical(SimulatedArtist artist, ArtistEvolutionProfile profile) {
		float level = Mathf.Clamp(artist.criticalAcclaim, 0f, 1f);
		if (level <= 0f) return 0f;
		// Trend over the last project, scaled so a single strong critical record registers.
		// A per-release gain is capped around .32, so 3x puts a good one near the top.
		float trend = Mathf.Clamp((level - profile.acclaimAtLastProject) * 3f, 0f, 1f);
		float unsold = 1f - Mathf.Clamp(artist.momentum, 0f, 1f);
		// An acclaimed act that is also selling has less to prove; the pull is strongest for
		// the act the critics rate and the public has not caught up with. Ambition SHADES the
		// term rather than gating it -- as a bare multiplier it made this a product of three
		// sub-unit factors, which is the same shape that held peer pressure at a tenth of
		// what it should have been.
		return Mathf.Clamp((.55f * level + .45f * trend) * (.55f + .45f * unsold) *
			(.55f + .45f * profile.artisticAmbition), 0f, 1f);
	}

	/// <summary>
	/// Other people's records. Influence memories are written by the cultural-memory ledger;
	/// with that phase off this term is zero, which is exactly the neutral case.
	/// <para>
	/// peerSensitivity is NOT applied here. It is applied once, where the memory is formed.
	/// Applying it at both ends squared a sub-unit term and cost roughly 4x at a typical
	/// .5 -- which, with the ledger's other faults, held the measured peak peer pressure to
	/// .0845 against a median commercial pressure of .80.
	/// </para>
	/// </summary>
	private static float Peer(ArtistEvolutionProfile profile, int year) {
		profile.dominantInfluence = ArtistInfluenceType.HitSingle;
		if (profile.influences == null || profile.influences.Count == 0) return 0f;
		float strongest = 0f;
		foreach (ArtistInfluenceMemory memory in profile.influences) {
			float age = Mathf.Clamp(1f - (year - memory.year) / (float)CulturalMemoryService.InfluenceMemoryYears, 0f, 1f);
			float weighted = memory.strength * age;
			if (weighted <= strongest) continue;
			strongest = weighted;
			// Which KIND of record moved them is the difference between "we heard a hit and
			// chased it" and "somebody made an album that changed what we thought a record
			// could be". The ledger has always recorded the distinction; nothing read it.
			profile.dominantInfluence = memory.type;
		}
		return Mathf.Clamp(strongest, 0f, 1f);
	}

	/// <summary>
	/// The label leaning on the act. For the player's roster this is a directive the player
	/// set: AI acts evolve autonomously and the player's influence on them is this term, not
	/// a veto.
	/// <para>
	/// For AI labels it has two halves. Impatience is the old term -- a label with no
	/// loyalty and a failing act. Appetite is new, and is the half that matters: a label
	/// that has noticed somebody else's record working in a genre it believes in wants some
	/// of that, whether or not the act in front of it is failing. The old formula multiplied
	/// BOTH its terms by the flop streak, which made it a scaled-down copy of commercial
	/// pressure driven by the identical variable -- it could not win a max() against its own
	/// parent under any parameter values, and it never did.
	/// </para>
	/// </summary>
	private static float Label(SimulatedArtist artist, ArtistEvolutionProfile profile, AILabel label, int year) {
		if (artist.isPlayerOwned) return Mathf.Clamp(profile.labelPressureDirective, 0f, 1f);
		if (label == null) return 0f;
		float impatience = 1f - Mathf.Clamp(label.artistLoyalty, 0f, 1f);
		float failing = Mathf.Clamp(artist.consecutiveFlops / (float)(FlopStreakForPressure * 2), 0f, 1f);
		float pushing = .45f * impatience * failing;

		(Genre? wanted, float strength) = CulturalMemoryService.AbsorbForLabel(label, year);
		profile.labelWantsGenre = wanted;
		if (wanted == null || strength <= 0f) return Mathf.Clamp(pushing, 0f, 1f);
		// A cautious label notices and does nothing; a hungry one starts booking sessions.
		float chasing = strength * Mathf.Clamp(.35f + .65f * label.riskTolerance, 0f, 1f);
		// It is only pressure on THIS act if the act could plausibly deliver it.
		float reachable = GenreMarketMomentumService.GetAdjacency(
			GenreCatalog.MapLegacy(artist.primaryGenre, year), wanted.Value);
		if (reachable < ArtistEvolution.AdjacencyFloor) return Mathf.Clamp(pushing, 0f, 1f);
		return Mathf.Clamp(pushing + .95f * chasing * reachable, 0f, 1f);
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

	/// <summary>
	/// Which motive won, judged on how loud each pressure is relative to its own scale.
	/// <paramref name="salience"/> returns the winner's normalised score so the caller can
	/// weigh a candidate-dependent motive -- a genre climate shift -- against it.
	/// </summary>
	private static ArtistEvolutionTrigger Dominant(ArtistEvolutionProfile profile, out float salience) {
		salience = 0f;
		var trigger = ArtistEvolutionTrigger.None;
		Consider(profile.commercialPressure, CommercialSalience, ArtistEvolutionTrigger.CommercialFailure, ref salience, ref trigger);
		Consider(profile.artisticPressure, ArtisticSalience, ArtistEvolutionTrigger.PersonalAmbition, ref salience, ref trigger);
		Consider(profile.criticalPressure, CriticalSalience, ArtistEvolutionTrigger.CriticalBreakthrough, ref salience, ref trigger);
		Consider(profile.labelPressure, LabelSalience, ArtistEvolutionTrigger.LabelPressure, ref salience, ref trigger);
		Consider(profile.internalPressure, InternalSalience, ArtistEvolutionTrigger.InternalTension, ref salience, ref trigger);
		// The peer channel resolves to a different motive depending on what kind of record
		// reached them. This is the Rubber Soul -> Pet Sounds route: an album that hung
		// together, heard by somebody who then reached for the same thing.
		Consider(profile.peerPressure, PeerSalience,
			profile.dominantInfluence == ArtistInfluenceType.CohesiveAlbum
				? ArtistEvolutionTrigger.CohesiveAlbumMovement
				: ArtistEvolutionTrigger.PeerInfluence, ref salience, ref trigger);
		return trigger;
	}

	private static void Consider(float pressure, float salienceScale, ArtistEvolutionTrigger candidate,
		ref float best, ref ArtistEvolutionTrigger trigger) {
		if (pressure <= 0f) return;
		float score = pressure / salienceScale;
		if (score <= best) return;
		best = score;
		trigger = candidate;
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
			ArtistEvolutionTrigger.CohesiveAlbumMovement or ArtistEvolutionTrigger.CriticalBreakthrough
				=> ReleaseCreativeIntent.Statement,
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
