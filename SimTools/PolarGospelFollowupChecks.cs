using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

public static class PolarGospelFollowupChecks {
 private static void Check(bool value,string label) { if(!value)throw new InvalidOperationException("FOLLOWUP_CHECK_FAIL: "+label); }
 public static void Run() {
  var table=PolarSongTable.Current;
  var book=CompositionCatalogService.AllSongs.Where(s=>s.repertoireSeedFamily=="Gospel Standard").ToArray();
  Check(book.Length==350 && book.All(s=>s.contentContext==SongContentContext.Sacred && s.contextAuthorshipEvidence=="seeded-authored-fixture:Gospel Standard"),"fixture equals authored songbook size");
  Check(CompositionCatalogService.AllSongs.Where(s=>s.repertoireSeedFamily=="R&B Catalog").All(s=>s.contentContext==SongContentContext.Secular),"explicit secular catalogue authoring");
  Check(CompositionCatalogService.AllSongs.Where(s=>s.contentContext!=SongContentContext.Unknown).All(s=>!string.IsNullOrWhiteSpace(s.contextAuthorshipEvidence)),"known contexts have authoring evidence");
  Check(CompositionCatalogService.AllSongs.All(s=>s.shapeVariationSchemaVersion==1 && s.shapeVariation.Length==10),"all creation sources store variation");
  var a=new SongComposition {songId="followup-original-1",originKind=SongOriginKind.ArtistOriginal,demoTaxonomy=new SongTaxonomy {archetype=SongArchetype.SaloonBallad}};
  var b=new SongComposition {songId="followup-original-2",originKind=SongOriginKind.ArtistOriginal,demoTaxonomy=a.demoTaxonomy.Copy()};
  PolarSongMetadataService.EnsureComposition(a);PolarSongMetadataService.EnsureComposition(b);
  var axes=SongProfileDeriver.Derive(a,a.demoTaxonomy,table).axes;
  Check(!axes.SequenceEqual(SongProfileDeriver.Derive(b,b.demoTaxonomy,table).axes),"two same-archetype originals vary");
  var saved=JsonSerializer.Deserialize<SongComposition>(JsonSerializer.Serialize(a,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  Check(axes.SequenceEqual(SongProfileDeriver.Derive(saved,saved.demoTaxonomy,table).axes),"original save/load stable");
  var offset=(float[])a.shapeVariation.Clone();
  a.shapeVariation=null;a.shapeVariationSchemaVersion=0;PolarSongMetadataService.EnsureComposition(a);
  Check(offset.SequenceEqual(a.shapeVariation),"deterministic reopen/backfill");
  ulong seed=CompositionShapeVariation.WorldSeed;
  try {
   CompositionShapeVariation.WorldSeed=seed+1;
   var anotherWorld=new SongComposition {songId=a.songId};CompositionShapeVariation.Ensure(anotherWorld);
   Check(!offset.SequenceEqual(anotherWorld.shapeVariation),"world seed participates in composition variation");
   Check(axes.SequenceEqual(SongProfileDeriver.Derive(saved,saved.demoTaxonomy,table).axes),"stored shape survives seed/current-world changes");
  } finally {CompositionShapeVariation.WorldSeed=seed;}
  foreach(var row in table.Archetypes) {
   var taxonomy=new SongTaxonomy {archetype=Enum.Parse<SongArchetype>(row.Name),vocalPresence=SongVocalPresence.Instrumental};
   var p=SongProfileDeriver.Derive(a,taxonomy,table);
   Check(new[]{0,1,4}.All(i=>p.axes[i]==0),"instrumental constraints "+row.Name);
  }
  var cover=book[0];var artist=ArtistManager.Instance.GetUnsignedArtists().First();
  var state=JsonSerializer.Serialize(cover,SaveGameService.TestJsonOptions);
  PolarCoverResolver.Propose(cover,null,PolarActProfileDeriver.Derive(artist,null,null,table),Genre.Gospel,1960,1,"followup-cover",null,table);
  Check(state==JsonSerializer.Serialize(cover,SaveGameService.TestJsonOptions),"cover inherits context/offset without mutation");
  var world=WorldStateService.Capture();var date=TimeManager.Instance.CurrentDate;
  var roundtrip=JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(world,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  WorldStateService.Apply(roundtrip,date,SimulationSeedBootstrap.RequestedSeed);
  Check(CompositionCatalogService.GetSong(cover.songId).contentContext==SongContentContext.Sacred,"world context roundtrip");
  Check(CompositionShapeVariation.ActiveVersion==1,"world version roundtrip");
  var older=JsonNode.Parse(JsonSerializer.Serialize(world,SaveGameService.TestJsonOptions));
  var composition=older["Composition"];composition.AsObject().Remove("ShapeVariationVersion");composition.AsObject().Remove("ShapeVariationWorldSeed");
  foreach(var song in composition["Songs"].AsObject().Select(kv=>kv.Value.AsObject()))foreach(var key in new[]{"contentContext","contextAuthorshipEvidence","shapeVariation","shapeVariationSchemaVersion"})song.Remove(key);
  var legacy=older.Deserialize<WorldSaveData>(SaveGameService.TestJsonOptions);
  WorldStateService.Apply(legacy,date,SimulationSeedBootstrap.RequestedSeed);
  Check(CompositionShapeVariation.ActiveVersion==0 && CompositionCatalogService.AllSongs.All(s=>s.contentContext==SongContentContext.Unknown && s.shapeVariationSchemaVersion==1),"older save defaults unknown and future offset backfill");
  Check(world.Composition.PolarMasters.All(kv=>JsonSerializer.Serialize(kv.Value.cachedProfile,SaveGameService.TestJsonOptions)==JsonSerializer.Serialize(PolarSongMetadataService.Get(kv.Key).cachedProfile,SaveGameService.TestJsonOptions)),"older caches unchanged");
  var restoredCover=CompositionCatalogService.GetSong(cover.songId);
  var oldShape=SongProfileDeriver.Derive(restoredCover,restoredCover.demoTaxonomy,table).axes;
  CompositionShapeVariation.ActiveVersion=1;
  Check(!oldShape.SequenceEqual(SongProfileDeriver.Derive(restoredCover,restoredCover.demoTaxonomy,table).axes),"backfilled offset used for future opt-in resolutions");
  PolarSongMetadataService.WarmCommittedProfiles(table);
  Check(world.Composition.PolarMasters.All(kv=>JsonSerializer.Serialize(kv.Value.cachedProfile,SaveGameService.TestJsonOptions)==JsonSerializer.Serialize(PolarSongMetadataService.Get(kv.Key).cachedProfile,SaveGameService.TestJsonOptions)),"opting in preserves cached recordings");
  GD.Print("POLAR_GOSPEL_FOLLOWUP_CHECK_PASS context=roundtrip cover=inherits legacy=unknown fixture=350 allSources=variation originals=distinct constraints=zero caches=preserved");
 }
}
