using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PolarSongFitChecks {
	private static readonly JsonSerializerOptions json = new() { IncludeFields = true };
	public static void Run() {
		var table = PolarSongTable.Current;
		Check(table.Archetypes.Length == Enum.GetValues<SongArchetype>().Length - 1, "full table");
		var pop = Song("f1", SongArchetype.BrightPopNumber, Genre.TeenPop);
		var beatles = Act(new[] { .62f,.55f,.55f,.78f,.45f,.70f,.70f,.45f,.75f,.45f }, .58f, .7f);
		var popProfile = SongProfileDeriver.Derive(pop, pop.demoTaxonomy, table);
		var f1 = PolarMaterialFit.Compute(popProfile, beatles, null, 1, table);
		Check(f1.Capability == 1 && f1.Deficit == 0, "F1 zero shortfall is 1.0, not spec example 0.94");
		Check(f1.ReferenceIdentityDistance > .2f && f1.Stretch > 0, "F1 identity movement distinct from capability");
		Check(!PolarMaterialFit.WouldRefuse(f1, .78f, table.N("newSigningStanding"), false, table), "F1 new signing does not refuse");
		Report("F1 Beatles", f1, "raw-reference mismatch; identity improves after pull");

		var respect = Song("f2", SongArchetype.HornDrivenSoulNumber, Genre.Soul);
		respect.plasticity = .55f; respect.plasticityFrozen = true;
		respect.demoTaxonomy.lyricModes = new[] { SongLyricMode.RomanticAddress };
		respect.demoTaxonomy.vocalApproaches = new[] { SongVocalApproach.Shouted };
		var otisReference = new SongMasterMetadata { masterId = "respect-1965", songId = respect.songId, recordingYear = 1965, taxonomy = respect.demoTaxonomy.Copy() };
		var aretha = Act(new[] { .97f,.95f,.70f,.85f,.80f,.70f,.82f,.35f,.95f,.78f }, .88f, .8f);
		aretha.hasVocalist = true; aretha.vocalApproach = SongVocalApproach.Belted;
		string untouched = JsonSerializer.Serialize(new { respect, otisReference, aretha }, json);
		var f2 = PolarCoverResolver.Propose(respect, otisReference, aretha, Genre.Soul, 1967, 1, "respect-1967", null, table);
		Check(f2.songId == respect.songId && f2.parentRecordingId == otisReference.masterId, "F2 one composition, immediate reference");
		Check(f2.taxonomy.lyricModes.Contains(SongLyricMode.DemandBoast) && f2.taxonomy.lyricModes.Contains(SongLyricMode.CallAndResponse), "F2 demand/call-response");
		Check(f2.taxonomy.vocalApproaches.Contains(SongVocalApproach.Belted), "F2 belted delivery");
		Check(PolarMaterialFit.ReinterpretationOutcome(f2.fit, aretha, .58f, table) > 0, "F2 positive reinterpretation");
		Check(untouched == JsonSerializer.Serialize(new { respect, otisReference, aretha }, json), "F2 pure proposal");
		Report("F2 Respect", f2.fit, $"archetype={f2.taxonomy.archetype} outcome={PolarMaterialFit.ReinterpretationOutcome(f2.fit, aretha, .58f, table):R}");

		var standard = Song("f3", SongArchetype.LushStandard, Genre.TraditionalPop);
		var garage = Act(new[] { .55f,.15f,.50f,.30f,.40f,.25f,.85f,.10f,.65f,.25f }, .1f, .9f);
		var lushProfile = SongProfileDeriver.Derive(standard, standard.demoTaxonomy, table);
		var f3 = PolarMaterialFit.Compute(lushProfile, garage, null, 1, table);
		Check(f3.WorstAxis == SongAxis.VocalNuance && f3.Capability < .5f, "F3 vocal nuance fails");
		Check(lushProfile[SongAxis.Ensemble] > garage[SongAxis.Ensemble], "F3 ensemble fails separately");
		Check(PolarMaterialFit.Pull(lushProfile, garage)[SongAxis.Sophistication] > garage[SongAxis.Sophistication] &&
			PolarMaterialFit.Pull(lushProfile, garage)[SongAxis.Maturity] > garage[SongAxis.Maturity], "F3 insufficient pull in sophistication/maturity");
		Report("F3 garage", f3, "Nobody in this band can sing this: unmet vocalNuance and ensemble");

		var tender = Song("f4", SongArchetype.LushStandard, Genre.TraditionalPop);
		tender.plasticity = .55f; tender.plasticityFrozen = true;
		tender.demoTaxonomy.pace = "measured"; tender.demoTaxonomy.mood = "elegant";
		tender.demoTaxonomy.lyricModes = new[] { SongLyricMode.RomanticAddress };
		tender.demoTaxonomy.vocalApproaches = new[] { SongVocalApproach.Crooned };
		var r1 = new SongMasterMetadata { masterId = "tender-1960", songId = tender.songId, recordingYear = 1960, taxonomy = tender.demoTaxonomy.Copy() };
		var otis = Act(new[] { .97f,.92f,.55f,.65f,.5f,.5f,.75f,.35f,.95f,.7f }, .9f, .6f);
		otis.hasVocalist = true; otis.vocalApproach = SongVocalApproach.Testifying;
		string before = JsonSerializer.Serialize(new { tender, r1 }, json);
		var f4 = PolarCoverResolver.Propose(tender, r1, otis, Genre.Soul, 1966, 1, "tender-1966", null, table);
		Report("F4 Tenderness", f4.fit, $"archetype={f4.taxonomy.archetype} pace={f4.taxonomy.pace} mood={f4.taxonomy.mood}");
		Check(f4.taxonomy.archetype == SongArchetype.DeepSoulPleader, "F4 general resolver reaches deep soul pleader");
		Check(f4.taxonomy.pace != r1.taxonomy.pace && f4.taxonomy.mood != r1.taxonomy.mood && f4.taxonomy.primaryGenre != r1.taxonomy.primaryGenre, "F4 shape/pace/mood/genre differ");
		Check(f4.parentRecordingId == r1.masterId && f4.songId == tender.songId, "F4 lineage");
		Check(before == JsonSerializer.Serialize(new { tender, r1 }, json), "F4 reference, composition, rights unchanged");
		Check(JsonSerializer.Serialize(f4, json) == JsonSerializer.Serialize(PolarCoverResolver.Propose(tender, r1, otis, Genre.Soul, 1966, 1, "tender-1966", null, table), json), "repeat proposal deterministic");
		var rigid = Act((float[])otis.axes.Clone(), 0, .9f);
		rigid.hasVocalist = true; rigid.vocalApproach = SongVocalApproach.Testifying;
		var noPull = PolarCoverResolver.Propose(tender, r1, rigid, Genre.Soul, 1966, 1, "no-pull", null, table);
		Check(noPull.usedFallback && noPull.fit.Stretch == 0, "zero reach cannot invent rearrangement");

		// Craft remains a ceiling: improving it cannot inflate the six performance demands.
		pop.compositionQuality = pop.commercialHook = pop.lyricQuality = 1;
		Check(popProfile.axes.SequenceEqual(SongProfileDeriver.Derive(pop, pop.demoTaxonomy, table).axes), "craft cannot raise demand");
		var instrumental = pop.demoTaxonomy.Copy(); instrumental.vocalPresence = SongVocalPresence.Instrumental;
		Check(SongProfileDeriver.Derive(pop, instrumental, table)[SongAxis.VocalPower] == 0, "instrumental vocal rule");
		Check(f2.realizedProfile.plasticity == respect.plasticity, "cover keeps composition plasticity");
		var futureTaste = new MarketTasteSnapshot { asOfWeek = 1, observations = 1 };
		Check(!PolarMaterialFit.Compute(popProfile, beatles, futureTaste, 1, table).HasMarketEvidence, "same/future week market excluded");
		futureTaste.asOfWeek = 0;
		Check(PolarMaterialFit.Compute(popProfile, beatles, futureTaste, 1, table).HasMarketEvidence, "lagged market accepted");
		var wrongReference = new SongMasterMetadata { songId = tender.songId, recordingYear = 1967 };
		bool threw = false; try { PolarCoverResolver.Propose(tender, wrongReference, otis, Genre.Soul, 1966, 1, "future", null, table); } catch (ArgumentException) { threw = true; }
		Check(threw, "future reference rejected");
		var actor = new SimulatedArtist { primaryGenre = Genre.Soul, type = ArtistType.SoloFemale, vocalPower = .9f, studioPerformance = .7f,
			members = new() { new Musician { isActive = true, technicalSkill = .8f, musicalVersatility = .6f, reliability = .9f } } };
		var inferred = PolarActProfileDeriver.Derive(actor, null, null, table);
		Check(inferred.hasVocalist && inferred.inferredCapability && inferred.inferredSession, "solo/no-label explicit inference");
		var supported = PolarActProfileDeriver.Derive(actor, null, new PolarSessionContext { producerCraft = 1, studioCraft = 1 }, table);
		Check(supported[SongAxis.StudioCraft] > inferred[SongAxis.StudioCraft] && supported[SongAxis.VocalPower] == inferred[SongAxis.VocalPower], "producer cannot invent vocalist skill");
		actor.members[0].isActive = false;
		Check(PolarActProfileDeriver.Derive(actor, null, null, table)[SongAxis.VocalPower] == 0, "inactive performers excluded");
		GD.Print("POLAR_SONG_FIT_PASS fixtures=F1,F2,F3,F4 purity=ok determinism=ok marketLag=ok adapter=ok");
	}
	private static SongComposition Song(string id, SongArchetype archetype, Genre genre) => new() { songId = id,
		demoTaxonomy = new SongTaxonomy { archetype = archetype, primaryGenre = genre }, primaryGenre = genre };
	private static ActProfile Act(float[] axes, float reach, float rigidity) => new() { axes = axes, interpretiveReach = reach, identityRigidity = rigidity };
	private static void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("POLAR_SONG_FIT_FAIL: " + reason); }
	private static void Report(string name, MaterialFit f, string extra) => GD.Print($"POLAR_FIXTURE {name} capability={f.Capability:R} identity={f.Identity:R} moment={f.Moment:R} deficit={f.Deficit:R} worst={f.WorstAxis} stretch={f.Stretch:R} referenceDistance={f.ReferenceIdentityDistance:R} {extra}");
}
