using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

public static class GospelPreferenceChecks {
 private static ulong CensusSeed(string key) {ulong h=14695981039346656037UL;foreach(char c in key){h^=c;h=unchecked(h*1099511628211UL);}return h;}
 private static void Check(bool value,string message) {if(!value)throw new InvalidOperationException(message);}
 public static void Run() {
  var artist=ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster).First(a=>a.primaryGenre==Genre.Gospel);
  var record=new Record {recordId="gospel-context-check",title="Authored sacred original",primaryGenre=Genre.RnB,secondaryGenre=Genre.Gospel};
  var song=CompositionCatalogService.CreateArtistOriginal(record,artist,null,Genre.RnB,1960,.5f,.5f,.5f,.5f);
  Check(song.contentContext==SongContentContext.Sacred&&!string.IsNullOrWhiteSpace(song.contextAuthorshipEvidence),"Gospel act original must be sacred even with another project genre");
  record.recordId="gospel-context-attach-check";
  CompositionCatalogService.AttachArtistOriginal(record,artist,null,1960);
  Check(CompositionCatalogService.GetSong(record.songId).contentContext==SongContentContext.Sacred,"Legacy original creation context");
  var secular=CompositionCatalogService.AllSongs.First(s=>s.contentContext==SongContentContext.Secular);
  var rights=JsonSerializer.Serialize(secular.rights,SaveGameService.TestJsonOptions);
  SongMaterialSelectionService.BuildCoverForSong(artist,record,secular,Genre.Gospel,1960);
  Check(secular.contentContext==SongContentContext.Secular&&rights==JsonSerializer.Serialize(secular.rights,SaveGameService.TestJsonOptions),"Cover must retain context and rights");
  Check(!LiveRepertoire.EligibleLive(secular,artist)&&LiveRepertoire.EligibleLive(new SongComposition(),artist),"Sacred live filter excludes known secular; legacy unknown remains unknown and usable");
  Check(CompositionCatalogService.AllSongs.Count(s=>s.repertoireSeedFamily=="Gospel Standard")==350,"Keep 350-song book");
  var pool=LiveRepertoire.Pool(artist,1960,1).Select(s=>s.songId).ToArray();
  Check(pool.SequenceEqual(LiveRepertoire.Pool(artist,1960,2).Select(s=>s.songId)),"Stable act access");
  var saved=JsonSerializer.Serialize(song,SaveGameService.TestJsonOptions);
  var copy=JsonSerializer.Deserialize<SongComposition>(saved,SaveGameService.TestJsonOptions);
  Check(copy.contentContext==SongContentContext.Sacred&&copy.contextAuthorshipEvidence==song.contextAuthorshipEvidence,"Context save/load");
  var old=JsonSerializer.Deserialize<SongComposition>("{\"songId\":\"old-unknown\"}",SaveGameService.TestJsonOptions);
  Check(old.contentContext==SongContentContext.Unknown,"Absent old context remains unknown");
  var item=new PlayerDesk.RepertoireItem {IsOriginal=true,ContentContext=SongContentContext.Sacred};
  var itemSaved=JsonSerializer.Serialize(RepertoireSaveData.From(item),SaveGameService.TestJsonOptions);
  Check(JsonSerializer.Deserialize<RepertoireSaveData>(itemSaved,SaveGameService.TestJsonOptions).ToItem().ContentContext==SongContentContext.Sacred,"Live original context survives player save/load");
  Check(JsonSerializer.Deserialize<RepertoireSaveData>("{}",SaveGameService.TestJsonOptions).ToItem().ContentContext==SongContentContext.Unknown,"Old repertoire context defaults unknown");
  var states=ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster).Where(a=>a.primaryGenre==Genre.Gospel).ToArray();
  var standard=CompositionCatalogService.AllSongs.First(s=>s.repertoireSeedFamily=="Gospel Standard");
  Check(states.Select(LiveRepertoire.InheritedLiveShare).Distinct().Count()>1,"Act-level affinity spread");
  var folk=ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster).First(a=>a.primaryGenre==Genre.Folk);
  var country=new SimulatedArtist {primaryGenre=Genre.Country};
  Check(LiveRepertoire.Affinity(country)==null&&LiveRepertoire.SetMix(country,1960)==null,"Country live selector stays neutral");
  Check(LiveRepertoire.Cohort(folk,1960)=="traditionalRevival"&&LiveRepertoire.WritingPropensity(folk,1960)==.25f,"Cohort drives writing propensity");
  var fit=new MaterialFit(1,1,.5f,0,SongAxis.VocalPower,0,0,false);
  PolarSongBehavior.AuditLiveFit=(s,a)=>fit;
  PolarSongBehavior.AuditLiveProposal=(s,a)=>new PolarArrangementProposal {fit=fit};
  try {
   var bookFixture=new SongComposition {songId="book-fixture",originYear=1940,establishedYear=1955,isStandard=true,contentContext=SongContentContext.Sacred};
   var modern=Enumerable.Range(0,50).Select(i=>new SongComposition {songId="modern-fixture-"+i,originYear=1960,contentContext=SongContentContext.Sacred}).ToArray();
   foreach(var a in states) {
    var small=SongMaterialSelectionService.SelectLiveCovers(new[]{bookFixture,modern[0]},a,1960,1);
    var large=SongMaterialSelectionService.SelectLiveCovers(modern.Prepend(bookFixture),a,1960,1);
    Check(small[0].isStandard==large[0].isStandard,"Catalogue size must not change the preferred source");
    var repeat=SongMaterialSelectionService.SelectLiveCovers(modern.Prepend(bookFixture),a,1960,1);
    Check(large[0].songId==repeat[0].songId,"Repeated live selection is deterministic");
   }
  } finally {PolarSongBehavior.AuditLiveFit=null;PolarSongBehavior.AuditLiveProposal=null;}
  GD.Print("GOSPEL_PREFERENCE_CHECK_PASS");
 }
 public static void Probe() {
  var args=OS.GetCmdlineUserArgs();string run=args.First(a=>a.StartsWith("--run="))[6..];
  Check(!string.IsNullOrWhiteSpace(run)&&run.All(c=>char.IsAsciiLetterOrDigit(c)||c=='-'),"Safe probe run name");
  string snapshot=args.FirstOrDefault(a=>a.StartsWith("--snapshot="))?[11..];
  if(snapshot!=null) {
   using var file=File.OpenRead(snapshot);using var gz=new GZipStream(file,CompressionMode.Decompress);
   var data=JsonSerializer.Deserialize<ChartAuditRunner.PolarResearchSnapshot>(gz,SaveGameService.TestJsonOptions);
   WorldStateService.Apply(data.Save.World,new GameDate(data.Save.Year,data.Save.Month,data.Save.Day),data.Save.WorldSeed);
  }
  // Fixed-frame historical snapshots predate tagging. Explicit diagnostic fixtures
  // author their procedural songs only; this is not an old-save migration.
  int fixtures=0;
  if(snapshot!=null)foreach(var s in CompositionCatalogService.AllSongs.Where(s=>s.contentContext==SongContentContext.Unknown)) {
   var context=s.repertoireSeedFamily=="Gospel Standard"||s.primaryGenre==Genre.Gospel?SongContentContext.Sacred:SongContentContext.Secular;
   CompositionShapeVariation.AuthorContext(s,context,"fixed-frame-diagnostic-fixture");fixtures++;
  }
  CompositionShapeVariation.ActiveVersion=2;
  if(snapshot!=null)foreach(var s in CompositionCatalogService.AllSongs.Where(s=>s.shapeVariationSchemaVersion!=2)) {
   s.shapeVariation=CompositionShapeVariation.V2Offsets(s,PolarSongTable.Current);s.shapeVariationSchemaVersion=2;
  }
  var date=TimeManager.Instance.CurrentDate;
  var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet();
  var actors=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId)
   .Where(a=>a.primaryGenre is Genre.Gospel or Genre.Folk or Genre.Country or Genre.Blues or Genre.EasyListening or Genre.Classical)
   .GroupBy(a=>(a.primaryGenre,Unsigned:unsigned.Contains(a.artistId))).SelectMany(g=>g.OrderBy(a=>RepertoireTaxonomy.Hash(a.artistId+"|repair-panel")).Take(g.Key.primaryGenre==Genre.Gospel?int.MaxValue:4)).ToArray();
  var rows=new List<object>();
  foreach(var variant in new[]{"v1Baseline","v2Baseline","v2SmoothOnly","v2SourceAffinity","v2SourceSmooth"}) {
   CompositionShapeVariation.ActiveVersion=variant.StartsWith("v1")?1:2;
   LiveRepertoire.AuditDisableAffinity=variant is "v1Baseline" or "v2Baseline" or "v2SmoothOnly";
   LiveRepertoire.AuditSmoothRanking=variant is "v2SmoothOnly" or "v2SourceSmooth";
   foreach(var artist in actors) {
    using var rng=new RandomNumberGenerator {Seed=CensusSeed($"{SimulationSeedBootstrap.RequestedSeed}:{date.year}:{date.month}:{artist.artistId}")};
    var set=new PlayerDesk.Prospect {Artist=artist};int access=0;
    PlayerDesk.Instance.BuildLiveSet(set,artist,date.year,0,rng,(n,p)=>access=p.Count(s=>s.repertoireSeedFamily=="Gospel Standard"));
    rows.Add(new {variant,artistId=artist.artistId,genre=artist.primaryGenre.ToString(),unsigned=unsigned.Contains(artist.artistId),access,
     slots=set.LiveSet.Select(item=> {var s=CompositionCatalogService.GetSong(item.SongId);return new {songId=item.SongId,original=item.IsOriginal,
      inherited=!item.IsOriginal&&(s.isTraditional||s.EstablishedAsOf(date.year)),context=(s?.contentContext??item.ContentContext).ToString()};}).ToArray()});
   }
  }
  LiveRepertoire.AuditDisableAffinity=LiveRepertoire.AuditSmoothRanking=false;
  CompositionShapeVariation.ActiveVersion=2;
  var path=ProjectSettings.GlobalizePath("res://SimLogs/"+run+"-preference.json");
  Check(!File.Exists(path),"Use a fresh probe name");
  File.WriteAllText(path,JsonSerializer.Serialize(new {seed=SimulationSeedBootstrap.RequestedSeed,date=date.ToShortString(),diagnosticContextFixtures=fixtures,rows},new JsonSerializerOptions {WriteIndented=true}));
  GD.Print("GOSPEL_PREFERENCE_PROBE_PASS run="+run);
 }
}
