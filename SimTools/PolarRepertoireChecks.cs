using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PolarRepertoireChecks {
 public static void FitProbe() {
  int year=TimeManager.Instance.CurrentDate.year;
  string root=ProjectSettings.GlobalizePath("res://SimLogs"),path=System.IO.Path.Combine(root,$"polar-repertoire-fit-probe-{SimulationSeedBootstrap.RequestedSeed}.csv");
  using var output=new System.IO.StreamWriter(path);
  output.WriteLine("seed,artistId,genre,knownPerformers,vocalist,family,candidates,bestScore,meanCapability,meanIdentity,withinTopWindow");
  var actors=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId).Where(a=>a.primaryGenre==Genre.Gospel).ToArray();
  foreach(var actor in actors) {
   var act=PolarActProfileDeriver.Derive(actor,null,null,PolarSongTable.Current);
   var candidates=LiveRepertoire.Pool(actor,year,1).Select(song=>new {song,fit=PolarSongBehavior.LiveProposal(song,actor,year).fit}).ToArray();
   float best=candidates.Length==0?0:candidates.Max(c=>PolarSongBehavior.SelectionScore(c.fit)),window=PolarRepertoireTable.Current.N("suitabilityWindow");
   foreach(var group in candidates.GroupBy(c=>c.song.repertoireSeedFamily??c.song.originKind.ToString()))output.WriteLine(string.Join(",",SimulationSeedBootstrap.RequestedSeed,actor.artistId,actor.primaryGenre,act.hasKnownPerformers,act.hasVocalist,group.Key,group.Count(),group.Max(c=>PolarSongBehavior.SelectionScore(c.fit)).ToString("R",System.Globalization.CultureInfo.InvariantCulture),group.Average(c=>c.fit.Capability).ToString("R",System.Globalization.CultureInfo.InvariantCulture),group.Average(c=>c.fit.Identity).ToString("R",System.Globalization.CultureInfo.InvariantCulture),group.Count(c=>PolarSongBehavior.SelectionScore(c.fit)>best-window)));
  }
  GD.Print($"POLAR_REPERTOIRE_FIT_PROBE_PASS seed={SimulationSeedBootstrap.RequestedSeed} actors={actors.Length}");
 }
 public static void Run() {
  var table=PolarRepertoireTable.Current;
  var seeded=CompositionCatalogService.AllSongs.Where(s=>s.repertoireSeedFamily!=null).ToArray();
  Check(seeded.Length>4000,"seeded authored repertoire present");
  Check(seeded.All(s=>s.demoTaxonomy?.archetype!=SongArchetype.Unknown),"all seed families explicitly assigned");
  var country=seeded.Where(s=>s.primaryGenre==Genre.Country).ToArray();
  Check(country.Select(s=>s.demoTaxonomy.archetype).Distinct().Count()>=6,"country creation uses multiple authored forms");
  foreach(var s in seeded) {
   Check(s.repertoireVariation.Length==SongProfile.AxisCount,"truth variation stored");
   for(int i=0;i<s.repertoireVariation.Length;i++)Check(Math.Abs(s.repertoireVariation[i])<=(i<SongProfile.DemandCount?.025f:.02f),"variation stays inside first-listen bands");
   var profile=SongProfileDeriver.Derive(s,s.demoTaxonomy,PolarSongTable.Current);
   if(s.demoTaxonomy.vocalPresence==SongVocalPresence.Instrumental)Check(profile[SongAxis.VocalPower]==0&&profile[SongAxis.VocalNuance]==0&&profile[SongAxis.LyricDelivery]==0,"instrumental hard constraint survives variation");
  }
  var pair=country.GroupBy(s=>s.demoTaxonomy.archetype).First(g=>g.Count()>1).Take(2).ToArray();
  Check(!SongProfileDeriver.Derive(pair[0],pair[0].demoTaxonomy,PolarSongTable.Current).axes.SequenceEqual(SongProfileDeriver.Derive(pair[1],pair[1].demoTaxonomy,PolarSongTable.Current).axes),"shared archetype has different truth");
  string json=JsonSerializer.Serialize(pair[0],SaveGameService.TestJsonOptions);
  var restored=JsonSerializer.Deserialize<SongComposition>(json,SaveGameService.TestJsonOptions);
  Check(json==JsonSerializer.Serialize(restored,SaveGameService.TestJsonOptions),"composition additive fields round trip");
  Check(SongProfileDeriver.Derive(pair[0],pair[0].demoTaxonomy,PolarSongTable.Current).axes.SequenceEqual(SongProfileDeriver.Derive(restored,restored.demoTaxonomy,PolarSongTable.Current).axes),"save/load and reopen preserve truth");
  var unknown=new SongComposition {songId="unknown",primaryGenre=Genre.Classical};
  Check(SongProfileDeriver.Derive(unknown,null,PolarSongTable.Current)[SongAxis.VocalNuance]>0,"unknown metadata never silently instrumental");
  var unknownAct=new SimulatedArtist {artistId="unknown-role",primaryGenre=Genre.Classical,musicianship=.9f,groupCohesion=.9f};
  Check(PolarSongBehavior.LiveProposal(unknown,unknownAct,1960).realizedProfile[SongAxis.VocalNuance]>0,"resolver does not turn unknown vocal metadata into instrumental truth");
  var instrumentAct=new SimulatedArtist {artistId="known-instrumental",primaryGenre=Genre.Jazz,type=ArtistType.Band,musicianship=.8f,groupCohesion=.8f,
   members=new(){new Musician {isActive=true,primaryRole=MusicianRole.Piano,technicalSkill=.8f,musicalVersatility=.8f,reliability=.8f}}};
  var knownVocal=seeded.First(s=>s.primaryGenre==Genre.Jazz&&s.demoTaxonomy.vocalPresence==SongVocalPresence.Present);
  Check(PolarSongBehavior.LiveProposal(knownVocal,instrumentAct,1960).taxonomy.vocalPresence==SongVocalPresence.Instrumental,"known vocal standard receives an explicit instrumental reading");
  Check(RepertoireProvenance.Category(new SongComposition {isPublicDomain=true,originYear=1900},1960)=="existingCover","public domain independent of traditional lineage");
  Check(!new SongComposition {originYear=1959,isStandard=true,establishedYear=1974}.EstablishedAsOf(1960),"contemporary catalogue not prematurely standard");
  Check(new SongComposition {originYear=1940,repertoireFirstReleaseYear=1940,standardDurability=.8f}.EstablishedAsOf(1960),"long circulating cover dynamically establishes");
  var actor=new SimulatedArtist {artistId="repertoire-access-probe",primaryGenre=Genre.EasyListening,type=ArtistType.SoloFemale,songwritingAbility=.5f,
   vocalPower=.65f,musicianship=.65f,groupCohesion=.65f,studioPerformance=.65f,members=new(){new Musician {isActive=true,isLeadVocalist=true,technicalSkill=.65f,musicalVersatility=.65f,reliability=.65f}}};
  foreach(int count in new[]{3,4,5,80}) {
   for(int i=0;i<count;i++)CompositionCatalogService.RegisterCoverableHit(new SongComposition {songId=$"access-probe-{count}-{i}",primaryGenre=Genre.EasyListening,secondaryGenre=Genre.EasyListening,originYear=1959});
   var pool=LiveRepertoire.Pool(actor,1960,1);
   Check(pool.Any(s=>s.secondaryGenre==Genre.EasyListening&&s.repertoireSeedFamily=="Tin Pan Alley"),"standards remain accessible at exact count "+count);
   Check(pool.Select(s=>s.songId).Distinct().Count()==pool.Count&&pool.All(s=>s.originYear<=1960),"eligibility after dedup and dates");
  }
  var eligible=LiveRepertoire.Pool(actor,1960,1);
  Check(PolarSongBehavior.RankLive(eligible,actor,1960).Select(s=>s.songId).SequenceEqual(PolarSongBehavior.RankLive(eligible.AsEnumerable().Reverse(),actor,1960).Select(s=>s.songId)),"preference independent of pool iteration order");
  var actCopy=JsonSerializer.Deserialize<SimulatedArtist>(JsonSerializer.Serialize(actor,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  Check(eligible.Select(s=>s.songId).SequenceEqual(LiveRepertoire.Pool(actCopy,1960,1).Select(s=>s.songId)),"latent songbook survives act save/load");
  Check(eligible.All(s=>Math.Abs(LiveRepertoire.Preference(s,actor,1960,1)-LiveRepertoire.Preference(s,actor,1960,2))<.01f),"slow monthly preference change");
  var inherited=seeded.First(s=>s.primaryGenre==Genre.Gospel&&s.secondaryGenre==Genre.Soul);
  actor.primaryGenre=Genre.Gospel;float oldPreference=LiveRepertoire.Preference(inherited,actor,1960,1);
  actor.primaryGenre=Genre.Soul;actor.evolution=new ArtistEvolutionProfile {priorArtisticCenter=Genre.Gospel,lastIdentityChangeYear=1960};
  Check(LiveRepertoire.Preference(inherited,actor,1960,1)==oldPreference,"genre drift begins with the existing song preference");
  Check(LiveRepertoire.AccessWeight(inherited,actor,1960,1)>LiveRepertoire.AccessWeight(inherited,actor,1960,12),"access follows existing genre-drift state gradually");
  foreach(var scene in table.Supply)Check(seeded.Any(s=>s.primaryGenre.ToString()==scene.Genre&&s.originYear<=1960),"native early pool "+scene.Genre);
  var counts=new int[2];
  for(int i=0;i<5000;i++)foreach(int g in new[]{0,1}) {
   var writer=new SimulatedArtist {artistId="writer-probe-"+i,primaryGenre=g==0?Genre.Country:Genre.Jazz,songwritingAbility=.99f};
   if(LiveRepertoire.OriginalCount(writer,1960,5)==5)counts[g]++;
  }
  Check(counts[0]>0&&counts[0]<300&&counts[1]>counts[0]&&counts[1]<900,"fully original sets rare, more frequent in composer-led jazz");
  GD.Print($"POLAR_REPERTOIRE_PASS seeded={seeded.Length} countryArchetypes={country.Select(s=>s.demoTaxonomy.archetype).Distinct().Count()} fullOriginalCountry={counts[0]}/5000 fullOriginalJazz={counts[1]}/5000");
 }
 private static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException("POLAR_REPERTOIRE_FAIL: "+label);}
}
