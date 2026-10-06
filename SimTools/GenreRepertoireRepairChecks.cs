using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class GenreRepertoireRepairChecks {
 private static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("GENRE_REPAIR_FAIL: "+label);}
 public static void Run() {
  var table=PolarRepertoireTable.Current;
  Check(table.GenreAffinities["TeenPop"].InheritedLiveShare==.06f&&table.GenreAffinities["TeenPop"].ActSpread==.02f,"Approved TeenPop setting");
  Check(!table.GenreAffinities.ContainsKey("Country")&&!table.GenreAffinities.ContainsKey("TraditionalPop"),"Pending settings remain unset");
  var fit=new MaterialFit(1,1,.5f,0,SongAxis.VocalPower,0,0,false);
  var oldFit=PolarSongBehavior.AuditLiveFit;var oldProposal=PolarSongBehavior.AuditLiveProposal;var oldObserver=SongMaterialSelectionService.ObserveLiveSource;
  var originals=Enumerable.Range(0,5).Select(i=>new SongComposition {songId="repair-modern-"+i,originYear=1960,isCoverable=true}).ToArray();
  var standard=new SongComposition {songId="repair-standard",originYear=1940,establishedYear=1955,isStandard=true,isCoverable=true};
  var cue=new SongComposition {songId="repair-cue",originYear=1960,primaryGenre=Genre.EasyListening,secondaryGenre=Genre.TraditionalPop,originKind=SongOriginKind.ExternalMediaComposition,repertoireSeedFamily="Screen instrumental",isCoverable=true};
  var stage=new SongComposition {songId="repair-film-song",originYear=1960,primaryGenre=Genre.EasyListening,secondaryGenre=Genre.TraditionalPop,originKind=SongOriginKind.ExternalMediaComposition,repertoireSeedFamily="Stage and film songs",isCoverable=true};
  try {
   PolarSongBehavior.AuditLiveFit=(s,a)=>fit;PolarSongBehavior.AuditLiveProposal=(s,a)=>new PolarArrangementProposal {fit=fit};
   foreach(var genre in new[]{Genre.Country,Genre.TeenPop,Genre.TraditionalPop,Genre.EasyListening,Genre.Jazz})for(int i=0;i<500;i++) {
    var act=new SimulatedArtist {artistId="repair-media-"+i,primaryGenre=genre,type=ArtistType.Band,
     members=new(){new Musician {isActive=true,primaryRole=MusicianRole.LeadGuitar}}};
    var small=originals.Append(standard).Append(cue).Append(stage).ToArray();
    var large=small.Concat(Enumerable.Range(0,100).Select(n=>new SongComposition {songId="extra-cue-"+n,originYear=1960,primaryGenre=cue.primaryGenre,secondaryGenre=cue.secondaryGenre,originKind=cue.originKind,repertoireSeedFamily=cue.repertoireSeedFamily,isCoverable=true})).ToArray();
    var a=SongMaterialSelectionService.SelectLiveCovers(small,act,1960,1);var b=SongMaterialSelectionService.SelectLiveCovers(large.Reverse(),act,1960,1);
    Check(a.Count==1&&b.Count==1,"Supplied source fills");
    if(genre!=Genre.EasyListening) {
     Check(LiveRepertoire.ExternalMedia(a[0])==LiveRepertoire.ExternalMedia(b[0]),"Adding cues cannot increase media opportunity "+genre);
     Check(LiveRepertoire.ScreenInstrumental(a[0])==LiveRepertoire.ScreenInstrumental(b[0]),"Adding cues cannot increase screen-family opportunity "+genre);
    }
    var set=SongMaterialSelectionService.SelectLiveCovers(large,act,1960,5);
    Check(set.Select(s=>s.songId).Distinct().Count()==set.Count,"No duplicate compositions");
   }
   var vocal=new SimulatedArtist {artistId="repair-vocal",primaryGenre=Genre.TraditionalPop,members=new(){new Musician {isActive=true,isLeadVocalist=true,primaryRole=MusicianRole.LeadVocals}}};
   Check(!LiveRepertoire.EligibleLive(cue,vocal)&&LiveRepertoire.EligibleLive(stage,vocal),"Vocal film songs retained; instrumental cues excluded");
   vocal.primaryGenre=Genre.EasyListening;
   Check(LiveRepertoire.EligibleLive(cue,vocal),"Vocal Easy acts can interpret screen themes");
   Check(SongMaterialSelectionService.SelectLiveCovers(new[]{cue},vocal,1960,1).Single()==cue,"Vocal Easy screen theme resolves in live selection");
   var mixFixtures=Enumerable.Range(0,5).SelectMany(i=>new[] {
    new SongComposition {songId="repair-trad-"+i,originYear=1900,isTraditional=true,isPublicDomain=true,isCoverable=true},
    new SongComposition {songId="repair-std-"+i,originYear=1940,establishedYear=1955,isStandard=true,isCoverable=true},
    new SongComposition {songId="repair-contemp-"+i,originYear=1960,isCoverable=true}}).ToArray();
   for(int i=0;i<500;i++) {
    var easy=new SimulatedArtist {artistId="repair-easy-mix-"+i,primaryGenre=Genre.EasyListening,type=ArtistType.Band,members=new(){new Musician {isActive=true,primaryRole=MusicianRole.Piano}}};
    var mix=LiveRepertoire.SetMix(easy,1960);
    var a=SongMaterialSelectionService.SelectLiveCovers(mixFixtures,easy,1960,1);
    var b=SongMaterialSelectionService.SelectLiveCovers(mixFixtures.Append(cue).Append(stage),easy,1960,1);
    Check(mix.Source(a[0],1960)==mix.Source(b[0],1960),"Media cannot displace Easy's selected source");
    var expected=PolarSongBehavior.RankLive(mixFixtures.Append(cue).Append(stage).Where(s=>mix.Source(s,1960)==mix.Source(a[0],1960)),easy,1960).First();
    Check(b[0]==expected,"Easy soundtrack and ordinary songs compete on ranking within the selected source");
   }
   var comic=new SimulatedArtist {artistId="repair-comic",primaryGenre=Genre.Comedy};
   var routine=new SongComposition {songId="repair-routine",originKind=SongOriginKind.ArtistOriginal,primaryGenre=Genre.Comedy,originArtistId=comic.artistId,originYear=1940,establishedYear=1955,isStandard=true,isCoverable=true};
   Check(!routine.EstablishedAsOf(2000),"Comedy does not age into standards");
   Check(RepertoireProvenance.CategoryForAct(routine,comic,1960)=="ownAuthored","Own catalogued routine remains authored material");
   var stranger=new SimulatedArtist {artistId="stranger",primaryGenre=Genre.Comedy};
   Check(LiveRepertoire.EligibleLive(routine,comic)&&!LiveRepertoire.EligibleLive(routine,stranger),"No comedian borrows another's routine");
   var child=new SimulatedArtist {artistId="repair-child",primaryGenre=Genre.Childrens};
   var childSong=new SongComposition {songId="repair-child-song",primaryGenre=Genre.Childrens,repertoireSeedFamily="Children songs"};
   Check(!LiveRepertoire.EligibleLive(childSong,comic)&&!LiveRepertoire.EligibleLive(routine,child),"No Comedy/Children cross-access");
   Check(LiveRepertoire.AccessWeight(childSong,comic,1960,1)==0&&LiveRepertoire.AccessWeight(routine,child,1960,1)==0,"Family access removed both directions");
   string rights=JsonSerializer.Serialize(routine.rights,SaveGameService.TestJsonOptions);
   Check(CompositionCatalogService.MigrateComedyRepertoire(new[]{routine})==1&&CompositionCatalogService.MigrateComedyRepertoire(new[]{routine})==0,"Migration is idempotent");
   Check(rights==JsonSerializer.Serialize(routine.rights,SaveGameService.TestJsonOptions),"Migration preserves royalties");
   Check(CompositionCatalogService.AllSongs.Count(s=>s.repertoireSeedFamily=="Classical works")==120&&CompositionCatalogService.AllSongs.Count(s=>s.repertoireSeedFamily=="Children songs")==80,"Retain existing books");
   foreach(var genre in new[]{Genre.Classical,Genre.Childrens}) {
    var act=new SimulatedArtist {artistId="repair-book-"+genre,primaryGenre=genre};
    var pool=LiveRepertoire.Pool(act,1960,1);var set=SongMaterialSelectionService.SelectLiveCovers(pool,act,1960,5);
    Check(set.Count==5&&set.All(s=>s.repertoireSeedFamily==LiveRepertoire.Affinity(act).SongbookFamily),"Dedicated book "+genre);
   }
   int fallbacks=0,requests=0;
   SongMaterialSelectionService.ObserveLiveSource=d=>{if(d.InheritedRequested)requests++;if(d.FitFallback)fallbacks++;};
   PolarSongBehavior.AuditLiveProposal=(s,a)=>new PolarArrangementProposal {fit=s==standard?new MaterialFit(.2f,.2f,.5f,0,SongAxis.VocalPower,0,0,false):fit};
   for(int i=0;i<1000;i++) {
    var act=new SimulatedArtist {artistId="repair-fallback-"+i,primaryGenre=Genre.TeenPop};
    var selected=SongMaterialSelectionService.SelectLiveCovers(originals.Append(standard),act,1960,1);
    Check(selected.Count==1&&selected[0]!=standard,"Poor-fit inherited source falls back");
   }
   Check(requests>0&&fallbacks==requests,"Fit fallback instrumented with requested-source denominator");
   int gospelInherited=0;
   for(int i=0;i<100;i++) {
    var gospel=new SimulatedArtist {artistId="repair-gospel-"+i,primaryGenre=Genre.Gospel};
    IReadOnlyList<SongComposition> before;
    LiveRepertoire.AuditGenreRepair=false;
    try {before=SongMaterialSelectionService.SelectLiveCovers(originals.Append(standard),gospel,1960,1);}
    finally {LiveRepertoire.AuditGenreRepair=true;}
    var after=SongMaterialSelectionService.SelectLiveCovers(originals.Append(standard),gospel,1960,1);
    Check(before.Select(s=>s.songId).SequenceEqual(after.Select(s=>s.songId)),"Preserve Gospel's landed source selector");
    gospelInherited+=after.Count(s=>s==standard);
   }
   Check(gospelInherited>0,"Gospel keeps its source-specific fit band");
   GD.Seed(77231);float expectedRandom=GD.Randf();GD.Seed(77231);
   SongMaterialSelectionService.SelectLiveCovers(originals.Append(standard),new SimulatedArtist {artistId="repair-rng",primaryGenre=Genre.TeenPop},1960,3);
   Check(GD.Randf()==expectedRandom,"Live source decisions preserve the global RNG stream");
  } finally {PolarSongBehavior.AuditLiveFit=oldFit;PolarSongBehavior.AuditLiveProposal=oldProposal;SongMaterialSelectionService.ObserveLiveSource=oldObserver;}
  GD.Print("GENRE_REPERTOIRE_REPAIR_CHECK_PASS media=countIndependent instrumentals=compatible comedy=authorOnly books=retained fallback=measured rights=preserved migration=idempotent");
 }
}
