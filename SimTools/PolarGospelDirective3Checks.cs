using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PolarGospelDirective3Checks {
 private static void Check(bool valid,string label) {if(!valid)throw new InvalidOperationException("DIRECTIVE3_CHECK_FAIL: "+label);}
 public static void Run() {
  int initial=CompositionShapeVariation.ActiveVersion;ulong seed=CompositionShapeVariation.WorldSeed;
  try {
   var table=PolarSongTable.Current;
   Check(CompositionShapeVariation.ResolveVersion(Array.Empty<string>(),2)==2,"new worlds v2 on");
   Check(CompositionShapeVariation.ResolveVersion(Array.Empty<string>(),0)==0,"old saves off");
   Check(CompositionShapeVariation.ResolveVersion(Array.Empty<string>(),1)==1,"v1 saves preserved");
   Check(CompositionShapeVariation.ResolveVersion(new[]{"--composition-shape-v2=off"},2)==1,"v2 off restores v1");
   Check(CompositionShapeVariation.ResolveVersion(new[]{"--composition-shape-v1=off","--composition-shape-v2=off"},2)==0,"both off");
   Check(CompositionShapeVariation.ResolveVersion(new[]{"--composition-shape-v2=on"},0)==2,"explicit load opt-in");
   foreach(var flags in new[]{new[]{"--composition-shape-v2=bad"},new[]{"--composition-shape-v2=on","--composition-shape-v2=off"}}) {
    bool failed=false;try{CompositionShapeVariation.ResolveVersion(flags,2);}catch(ArgumentException){failed=true;}Check(failed,"invalid flags rejected");
   }
   var v1=new SongComposition {songId="directive3-v1",demoTaxonomy=new SongTaxonomy {archetype=SongArchetype.QuietHymn}};
   CompositionShapeVariation.ActiveVersion=1;CompositionShapeVariation.Ensure(v1);var stored=(float[])v1.shapeVariation.Clone();
   CompositionShapeVariation.ActiveVersion=2;CompositionShapeVariation.Ensure(v1);Check(v1.shapeVariationSchemaVersion==1&&stored.SequenceEqual(v1.shapeVariation),"v1 offsets never rewritten");
   foreach(var row in table.Archetypes)for(int n=0;n<16;n++) {
    var song=new SongComposition {songId="directive3-"+row.Name+"-"+n,demoTaxonomy=new SongTaxonomy {archetype=Enum.Parse<SongArchetype>(row.Name)}};
    CompositionShapeVariation.Ensure(song);var profile=SongProfileDeriver.Derive(song,song.demoTaxonomy,table);
    Check(profile.axes.All(v=>float.IsFinite(v)&&v>=0&&v<=1),"constraints "+row.Name);
    if(row.Axes[0]==0&&row.Axes[1]==0&&row.Axes[4]==0)Check(new[]{0,1,4}.All(i=>profile.axes[i]==0&&song.shapeVariation[i]==0),"instrumental "+row.Name);
    Check(song.shapeVariation.SequenceEqual(CompositionShapeVariation.V2Offsets(song,table)),"deterministic");
    var copy=JsonSerializer.Deserialize<SongComposition>(JsonSerializer.Serialize(song,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
    Check(profile.axes.SequenceEqual(SongProfileDeriver.Derive(copy,copy.demoTaxonomy,table).axes),"song roundtrip");
   }
   Check(Math.Abs(CompositionShapeVariation.Reflect(-.12f)-.12f)<1e-6&&Math.Abs(CompositionShapeVariation.Reflect(1.12f)-.88f)<1e-6,"reflection");
   var toggleSong=CompositionCatalogService.AllSongs.First(s=>s.shapeVariationSchemaVersion==2);
   var v2Before=SongProfileDeriver.Derive(toggleSong,toggleSong.demoTaxonomy,table).axes;
   string toggleBefore=JsonSerializer.Serialize(toggleSong,SaveGameService.TestJsonOptions);
   CompositionShapeVariation.ActiveVersion=1;
   var v1Reading=SongProfileDeriver.Derive(toggleSong,toggleSong.demoTaxonomy,table).axes;
   Check(!v2Before.SequenceEqual(v1Reading)&&toggleBefore==JsonSerializer.Serialize(toggleSong,SaveGameService.TestJsonOptions),"v2 off changes future reading without rewriting offsets");
   CompositionShapeVariation.ActiveVersion=2;
   Check(v2Before.SequenceEqual(SongProfileDeriver.Derive(toggleSong,toggleSong.demoTaxonomy,table).axes),"v2 toggle roundtrip");
   var world=WorldStateService.Capture();var date=TimeManager.Instance.CurrentDate;
   var caches=world.Composition.PolarMasters.ToDictionary(kv=>kv.Key,kv=>JsonSerializer.Serialize(kv.Value.cachedProfile,SaveGameService.TestJsonOptions));
   var saved=JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(world,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
   WorldStateService.Apply(saved,date,SimulationSeedBootstrap.RequestedSeed);
   Check(CompositionShapeVariation.ActiveVersion==2,"v2 world roundtrip");
   foreach(var kv in caches)Check(JsonSerializer.Serialize(PolarSongMetadataService.Get(kv.Key).cachedProfile,SaveGameService.TestJsonOptions)==kv.Value,"cached recordings preserved");
   var legacy=JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(saved,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
   legacy.Composition.ShapeVariationVersion=0;
   foreach(var song in legacy.Composition.Songs.Values){song.shapeVariationSchemaVersion=0;song.shapeVariation=null;song.contentContext=SongContentContext.Unknown;song.contextAuthorshipEvidence=null;}
   WorldStateService.Apply(legacy,date,SimulationSeedBootstrap.RequestedSeed);
   Check(CompositionShapeVariation.ActiveVersion==0&&CompositionCatalogService.AllSongs.All(s=>s.shapeVariationSchemaVersion==1&&s.contentContext==SongContentContext.Unknown),"actual older-save load defaults off and preserves unknown context");
   foreach(var kv in caches)Check(JsonSerializer.Serialize(PolarSongMetadataService.Get(kv.Key).cachedProfile,SaveGameService.TestJsonOptions)==kv.Value,"older-save caches preserved");
   WorldStateService.Apply(saved,date,SimulationSeedBootstrap.RequestedSeed);
   GospelSongbookExpansion.ValidateRequest(Array.Empty<string>());GospelSongbookExpansion.ValidateRequest(new[]{"--gospel-songbook-v2=off"});
   bool blocked=false;try{GospelSongbookExpansion.ValidateRequest(new[]{"--gospel-songbook-v2=on"});}catch(InvalidOperationException){blocked=true;}Check(blocked,"unresearched expansion fails explicitly");
   var book=CompositionCatalogService.AllSongs.Where(s=>s.repertoireSeedFamily=="Gospel Standard").ToArray();
   Check(book.Length==350&&book.All(s=>s.contentContext==SongContentContext.Sacred),"sacred fixture unchanged");
   var artist=ArtistManager.Instance.GetUnsignedArtists().First();var songForCover=book[0];
   string before=JsonSerializer.Serialize(songForCover,SaveGameService.TestJsonOptions);
   var act=PolarActProfileDeriver.Derive(artist,null,null,table);
   var a=PolarCoverResolver.Propose(songForCover,null,act,Genre.Gospel,1960,1,"directive3-cover",null,table);
   var b=PolarSongBehavior.LiveProposal(songForCover,artist,1960);
   Check(a.songId==songForCover.songId&&b.songId==songForCover.songId&&before==JsonSerializer.Serialize(songForCover,SaveGameService.TestJsonOptions),"cover/scouting inherit without mutation");
   CompositionShapeVariation.WorldSeed=seed+1;Check(!songForCover.shapeVariation.SequenceEqual(CompositionShapeVariation.V2Offsets(songForCover,table)),"world-keyed variation");
   CompositionShapeVariation.WorldSeed=seed;
   PolarGospelRepairChecks.Run();PolarSongFitChecks.Run();PolarSongBehaviorChecks.Run();PolarPlayerPerceptionChecks.Run();
   GD.Print("POLAR_DIRECTIVE3_CHECK_PASS deterministic=True constraints=zero instrumental=exact worldRoundtrip=True v1Preserved=True covers=True scouting=True caches=True flags=True");
  } finally {CompositionShapeVariation.ActiveVersion=initial;CompositionShapeVariation.WorldSeed=seed;}
 }
}
