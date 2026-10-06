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
		float averageSkill = members.Length == 0 ? 0 : members.Average(m => m.technicalSkill);
		p[SongAxis.Musicianship] = members.Length == 0 ? 0 : Math.Max(artist.musicianship, members.Max(m => m.technicalSkill));
		p[SongAxis.Ensemble] = members.Length == 0 ? 0 : artist.groupCohesion * table.N("ensembleCohesion") + averageSkill * table.N("ensembleSkill");
		if (lead != null) {
			float support = backup && artist.type is not ArtistType.SoloMale and not ArtistType.SoloFemale ? table.N("backupVocalFactor") : 1;
			p[SongAxis.VocalPower] = artist.vocalPower * support;
			p[SongAxis.VocalNuance] = support * (lead.technicalSkill * table.N("actorNuanceTechnical") + lead.musicalVersatility * table.N("actorNuanceVersatility") + lead.reliability * table.N("actorNuanceReliability"));
			p[SongAxis.LyricDelivery] = support * (lead.technicalSkill * table.N("deliveryTechnical") + lead.musicalVersatility * table.N("deliveryVersatility"));
		}
		float expected = label?.productionQuality ?? table.N("missingSessionCraft");
		p[SongAxis.StudioCraft] = session == null ? expected : SongProfileDeriver.Lerp(session.studioCraft, session.producerCraft, table.N("sessionProducerWeight"));
		var prior = table.Prior(artist.primaryGenre);
		for (int i = 0; i < SongProfile.IdentityCount; i++) p.axes[SongProfile.DemandCount + i] = prior.Identity[i];
		// Bounded member-history adjustments; ambition and age never declare stylistic coordinates.
		if (members.Length > 0) {
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
		p.interpretiveReach = bestCraft * table.N("reachCraft") + (evolution?.experimentalAppetite ?? table.N("missingDisposition")) * table.N("reachExperimental") + artist.groupCohesion * table.N("reachCohesion");
		p.identityRigidity = table.N("rigidityBase") + (evolution?.artisticAmbition ?? table.N("missingDisposition")) * table.N("rigidityAmbition") + (evolution?.rootsAttachment ?? table.N("missingDisposition")) * table.N("rigidityRoots");
		for (int i = 0; i < p.axes.Length; i++) p.axes[i] = SongProfileDeriver.Clamp(p.axes[i]);
		p.interpretiveReach = SongProfileDeriver.Clamp(p.interpretiveReach); p.identityRigidity = SongProfileDeriver.Clamp(p.identityRigidity);
		p.vocalApproach = lead == null ? SongVocalApproach.Unknown : p[SongAxis.VocalPower] >= p[SongAxis.VocalNuance] ? SongVocalApproach.Belted : SongVocalApproach.Crooned;
		return p;
	}
}
