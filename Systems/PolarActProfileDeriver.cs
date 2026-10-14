using System;
using System.Linq;

/// <summary>Versioned minimal adapter over existing active performers; all vocal nuance/diction is inferred.</summary>
public static class PolarActProfileDeriver {
	public static ActProfile Derive(SimulatedArtist artist, AILabel label, PolarSessionContext session, PolarSongTable table) {
		if (artist == null) throw new ArgumentNullException(nameof(artist));
		var members = (artist.members ?? new()).Where(m => m != null && m.isActive).ToArray();
		var lead = members.FirstOrDefault(m => m.isLeadVocalist) ?? members.FirstOrDefault(m => m.primaryRole == MusicianRole.LeadVocals);
		bool backup = lead == null;
		lead ??= members.FirstOrDefault(m => m.primaryRole == MusicianRole.BackingVocals);
		// Solo acts without an explicit lead flag still have a known solo performer.
		if (lead == null && !artist.instrumentalPerformance && (artist.type is ArtistType.SoloMale or ArtistType.SoloFemale)) lead = members.FirstOrDefault();
		var p = new ActProfile { mappingVersion = table.Version, hasVocalist = lead != null, hasKnownPerformers = members.Length > 0, inferredSession = session == null };
		// Phase 5b (--polar-member-axes): read the split axes where a person has them -- the players' hands for
		// Musicianship, the lead's power, control and diction for the vocal axes -- instead of one technicalSkill.
		bool axes = BandLife.PolarMemberAxes;
		float Hands(Musician m) => axes && m.axesVersion > 0 ? m.instrumentalSkill : m.technicalSkill;
		float averageSkill = members.Length == 0 ? 0 : members.Average(m => m.technicalSkill);
		p[SongAxis.Musicianship] = members.Length == 0 ? 0 : Math.Max(artist.musicianship, members.Max(Hands));
		p[SongAxis.Ensemble] = members.Length == 0 ? 0 : artist.groupCohesion * table.N("ensembleCohesion") + averageSkill * table.N("ensembleSkill");
		if (lead != null) {
			float support = backup && artist.type is not ArtistType.SoloMale and not ArtistType.SoloFemale ? table.N("backupVocalFactor") : 1;
			bool split = axes && lead.axesVersion > 0;
			float power = split ? lead.vocalPower * 0.6f + lead.stagePresence * 0.4f : artist.vocalPower;
			float control = split ? lead.vocalControl : lead.technicalSkill;
			float diction = split ? lead.diction : lead.technicalSkill;
			p[SongAxis.VocalPower] = power * support;
			p[SongAxis.VocalNuance] = support * (control * table.N("actorNuanceTechnical") + lead.musicalVersatility * table.N("actorNuanceVersatility") + lead.reliability * table.N("actorNuanceReliability"));
			p[SongAxis.LyricDelivery] = support * (diction * table.N("deliveryTechnical") + lead.musicalVersatility * table.N("deliveryVersatility"));
		}
		float expected = label?.productionQuality ?? table.N("missingSessionCraft");
		p[SongAxis.StudioCraft] = session == null ? expected : SongProfileDeriver.Lerp(session.studioCraft, session.producerCraft, table.N("sessionProducerWeight"));
		var prior = table.Prior(artist.primaryGenre);
		for (int i = 0; i < SongProfile.IdentityCount; i++) p.axes[SongProfile.DemandCount + i] = prior.Identity[i];
		// Phase 7 (--enable-member-identity): the act is its people -- the voice-weighted mean of their identities, which
		// already carry the trait adjustments below person by person.
		if (MemberIdentityService.Enabled) {
			int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
			foreach (Musician m in members) MemberIdentityService.EnsureIdentity(m, artist, year);
		}
		float[] people = MemberIdentityService.Enabled ? MemberIdentityService.Aggregate(artist, members) : null;
		if (people != null) {
			for (int i = 0; i < SongProfile.IdentityCount; i++) p.axes[SongProfile.DemandCount + i] = people[i];
		} else if (members.Length > 0) {
			// Bounded member-history adjustments; ambition and age never declare stylistic coordinates.
			p[SongAxis.Toughness] += (table.N("neutralTrait") - members.Average(m => m.temperament)) * table.N("traitIdentityAdjustment");
			p[SongAxis.Sophistication] += (members.Average(m => m.musicalVersatility) - table.N("neutralTrait")) * table.N("traitIdentityAdjustment");
			p[SongAxis.Sincerity] += (members.Average(m => m.reliability) - table.N("neutralTrait")) * table.N("traitIdentityAdjustment");
		}
		var evolution = artist.evolution;
		if (evolution?.recentProjectGenres != null && evolution.WindowLength > 0) {
			var history = evolution.recentProjectGenres.Take(evolution.WindowLength).Select(table.Prior).ToArray();
			for (int i = 0; i < SongProfile.IdentityCount; i++) {
				int axis = SongProfile.DemandCount + i;
				p.axes[axis] = SongProfileDeriver.Lerp(p.axes[axis], history.Average(h => h.Identity[i]), table.N("historyIdentityBlend"));
			}
		}
		float bestCraft = members.Length == 0 ? 0 : members.Max(m => m.creativity);
		p.interpretiveReach = bestCraft * table.N("reachCraft") + (evolution?.experimentalAppetite ?? table.N("missingDisposition")) * table.N("reachExperimental") + artist.groupCohesion * table.N("reachCohesion")
			+ MemberIdentityService.ReachBonus(artist, members);
		p.identityRigidity = table.N("rigidityBase") + (evolution?.artisticAmbition ?? table.N("missingDisposition")) * table.N("rigidityAmbition") + (evolution?.rootsAttachment ?? table.N("missingDisposition")) * table.N("rigidityRoots");
		for (int i = 0; i < p.axes.Length; i++) p.axes[i] = SongProfileDeriver.Clamp(p.axes[i]);
		p.interpretiveReach = SongProfileDeriver.Clamp(p.interpretiveReach); p.identityRigidity = SongProfileDeriver.Clamp(p.identityRigidity);
		p.vocalApproach = lead == null ? SongVocalApproach.Unknown : p[SongAxis.VocalPower] >= p[SongAxis.VocalNuance] ? SongVocalApproach.Belted : SongVocalApproach.Crooned;
		return p;
	}
}
