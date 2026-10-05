using System;
using System.Collections.Generic;
using System.IO;
using SHA256 = System.Security.Cryptography.SHA256;
using System.Text;
using System.Linq;
using System.Text.Json;
using Godot;

public static class FolkEasyListeningChecks {
 private static void Check(bool ok,string message) {if(!ok)throw new InvalidOperationException("FOLK_EASY_FAIL: "+message);}
 public static void Run() {
  var digestFixture=new WorldSaveData();
  var directDigest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(digestFixture,SaveGameService.TestJsonOptions))));
  Check(directDigest==ChartAuditRunner.CheckpointWorldDigest(digestFixture),"Streaming checkpoint digest matches canonical serialized bytes");
  var date=TimeManager.Instance.CurrentDate;
  int hour=TimeManager.Instance.CurrentHour,minute=TimeManager.Instance.CurrentMinute;
  var rows=new List<object>();
  var fit=new MaterialFit(1,1,.5f,0,SongAxis.VocalPower,0,0,false);
  var fixtures=Enumerable.Range(0,5).SelectMany(i=>new[] {
   new SongComposition {songId="traditional-"+i,originYear=1900,isTraditional=true,isPublicDomain=true},
   new SongComposition {songId="standard-"+i,originYear=1940,establishedYear=1955,isStandard=true},
   new SongComposition {songId="contemporary-"+i,originYear=1959}
  }).ToArray();
  PolarSongBehavior.AuditLiveFit=(s,a)=>fit;
  PolarSongBehavior.AuditLiveProposal=(s,a)=>new PolarArrangementProposal {fit=fit};
  try {
   foreach(var genre in new[]{Genre.Folk,Genre.ContemporaryFolk,Genre.EasyListening})foreach(int year in new[]{1960,1961,1962,1963}) {
    TimeManager.Instance.RestoreClock(new GameDate(year,1,1),hour,minute);
    int[] totals=new int[4];
    for(int i=0;i<2000;i++) {
     var act=new SimulatedArtist {artistId="folk-easy-source-check-"+i,primaryGenre=genre,type=ArtistType.Band,songwritingAbility=.6f,
      members=new(){new Musician {isActive=true,primaryRole=MusicianRole.LeadVocals,isLeadVocalist=true,technicalSkill=.8f}}};
     ArtistManager.ConfigureEasyListeningBandleader(act,year);
     int size=3+i%3,originals=LiveRepertoire.OriginalCount(act,year,size);totals[3]+=originals;
     var mix=LiveRepertoire.SetMix(act,year);
     var covers=SongMaterialSelectionService.SelectLiveCovers(fixtures,act,year,size-originals);
     Check(covers.Count==size-originals&&covers.DistinctBy(s=>s.songId).Count()==covers.Count,"Fully supplied set fills uniquely");
     foreach(var song in covers)totals[(int)mix.Source(song,year)]++;
     if(i<50) {
      var first=SongMaterialSelectionService.SelectLiveCovers(fixtures,act,year,1);
      var extra=Enumerable.Range(0,50).Select(n=>new SongComposition {songId="extra-modern-"+n,originYear=1959});
      var expanded=SongMaterialSelectionService.SelectLiveCovers(fixtures.Concat(extra).Reverse(),act,year,1);
      Check(mix.Source(first[0],year)==mix.Source(expanded[0],year),"Source independent of catalogue size/order");
      Check(covers.Select(s=>s.songId).SequenceEqual(SongMaterialSelectionService.SelectLiveCovers(fixtures.Reverse(),act,year,size-originals).Select(s=>s.songId)),"Repeat/order determinism");
      var unavailable=SongMaterialSelectionService.SelectLiveCovers(fixtures.Where(s=>s.songId.StartsWith("contemporary")),act,year,size-originals);
      Check(unavailable.Count==size-originals&&unavailable.All(s=>mix.Source(s,year)==LiveCoverSource.Contemporary),"Exhausted sources fall back to eligible covers");
      var own=new SongComposition {songId="own-original",originYear=1960,originKind=SongOriginKind.ArtistOriginal,originArtistId=act.artistId};
      Check(!SongMaterialSelectionService.SelectLiveCovers(fixtures.Append(own),act,year,5).Contains(own),"Own authored material cannot leak into the cover buckets");
     }
    }
    var policy=LiveRepertoire.SetMix(new SimulatedArtist {primaryGenre=genre},year);
    var expected=new[]{policy.Traditional,policy.Standards,policy.Contemporary,policy.Originals}.Select(w=>w/policy.Total).ToArray();
    var shares=totals.Select(n=>n/(double)totals.Sum()).ToArray();
    for(int i=0;i<4;i++)Check(Math.Abs(shares[i]-expected[i])<.025,"Source probability calibration "+genre+" "+year+" "+i);
    rows.Add(new {kind="fullySuppliedFixture",genre=genre.ToString(),year,slots=totals.Sum(),totals,shares,expected});
   }
   var vocal=new SimulatedArtist {artistId="easy-vocal-check",primaryGenre=Genre.EasyListening,songwritingAbility=1,
    members=new(){new Musician {isActive=true,isLeadVocalist=true,primaryRole=MusicianRole.LeadVocals}}};
   Check(Enumerable.Range(1960,10).All(y=>LiveRepertoire.OriginalCount(vocal,y,5)==0),"Easy Listening vocalists never take the bandleader original exception");
   var standard=new SongComposition {originYear=1955,establishedYear=1955};
   var easy=LiveRepertoire.SetMix(vocal,1960);
   Check(easy.Source(standard,1960)==LiveCoverSource.Contemporary,"Easy standard cutoff excludes 1955");
   standard.originYear=1954;Check(easy.Source(standard,1960)==LiveCoverSource.Standard,"Easy standard cutoff includes 1954");
   standard.isPublicDomain=true;Check(easy.Source(standard,1960)==LiveCoverSource.Traditional,"Requested PD bucket precedes standard bucket");
  } finally {PolarSongBehavior.AuditLiveFit=null;PolarSongBehavior.AuditLiveProposal=null;TimeManager.Instance.RestoreClock(date,hour,minute);}
  Check(CompositionCatalogService.AllSongs.Count(s=>s.repertoireSeedFamily=="Folk Standards")==240,"Dedicated authored Folk standards book");
  Check(CompositionCatalogService.AllSongs.Count(s=>s.repertoireSeedFamily=="Gospel Standard")==350,"Gospel book retained");
  var oldSolo=new SimulatedArtist {type=ArtistType.SoloMale,members=new(){new Musician {isActive=true,primaryRole=MusicianRole.Piano}}};
  Check(PolarActProfileDeriver.Derive(oldSolo,null,null,PolarSongTable.Current).hasVocalist,"Old solo role fallback retained");
  oldSolo.instrumentalPerformance=true;
  var roleCopy=JsonSerializer.Deserialize<SimulatedArtist>(JsonSerializer.Serialize(oldSolo,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  Check(roleCopy.instrumentalPerformance&&!PolarActProfileDeriver.Derive(roleCopy,null,null,PolarSongTable.Current).hasVocalist,"Explicit solo instrumental role survives save/load");
  var venue=PlayerDesk.ScoutingVenue.TheatresAndSupperClubs;
  Check(PlayerDesk.AdmitsGenre(venue,false,PlayerDesk.FamiliesFor(venue),Genre.EasyListening,1960),"Supper club scouting admits Easy Listening");
  foreach(var type in Enum.GetValues<ExternalMediaSourceType>()) {
   var record=new Record {recordId="folk-easy-media-check-"+type,title="Theme fixture",format=ReleaseFormat.Album,releaseDate=new GameDate(1960,1,1),
    album=new Album {albumFormat=AlbumFormat.Soundtrack,externalMedia=new ExternalMediaProfile {sourceType=type,sourcePopularity=.6f,criticalPrestige=.7f},trackRefs=Array.Empty<AlbumTrack>(),nonSingleTracks=Array.Empty<AlbumTrack>()}};
   CompositionCatalogService.OnRecordReleased(record);
   var theme=CompositionCatalogService.AllSongs.Single(s=>s.externalMediaSourceRecordId==record.recordId);
   Check(theme.originYear==1960&&!theme.isStandard&&!theme.isPublicDomain&&theme.repertoireAdmissionRoutes.Contains("externalMediaTheme"),"Released media creates a contemporary theme");
   Check(theme.demoTaxonomy.vocalPresence==(type==ExternalMediaSourceType.FilmScore?SongVocalPresence.Instrumental:SongVocalPresence.Present),"Film score is instrumental; cast/film songs are vocal");
   Check(ReferenceEquals(theme,CompositionCatalogService.AdmitExternalMediaTheme(record,1960)),"Media theme admission is idempotent");
   string saved=JsonSerializer.Serialize(theme,SaveGameService.TestJsonOptions);
   Check(saved==JsonSerializer.Serialize(JsonSerializer.Deserialize<SongComposition>(saved,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions),"Media theme metadata/rights survive save/load");
   Check(CompositionCatalogService.GetCoverableHitsForGenre(Genre.EasyListening).Contains(theme),"Released theme is available to cover selection");
  }
  // Future year readings of a fresh world, explicitly separate from evolved trajectories.
  var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet();
  var population=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId)
   .Where(a=>a.primaryGenre is Genre.Folk or Genre.ContemporaryFolk or Genre.EasyListening)
   .GroupBy(a=>(a.primaryGenre,Unsigned:unsigned.Contains(a.artistId),Instrumental:LiveRepertoire.Instrumental(a)))
   .SelectMany(g=>g.OrderBy(a=>RepertoireTaxonomy.Hash(a.artistId+"|folk-easy-panel")).Take(12)).ToArray();
  try {
   foreach(int year in new[]{1960,1961,1962,1963}) {
    TimeManager.Instance.RestoreClock(new GameDate(year,1,1),hour,minute);
    foreach(var act in population) {
     using var random=new RandomNumberGenerator {Seed=RepertoireTaxonomy.Hash($"{SimulationSeedBootstrap.RequestedSeed}:{year}:{act.artistId}")};
     var prospect=new PlayerDesk.Prospect {Artist=act};var mix=LiveRepertoire.SetMix(act,year);
     PlayerDesk.Instance.BuildLiveSet(prospect,act,year,0,random);
     Check(prospect.LiveSet.Count>=3&&prospect.LiveSet.Count<=5,"Fresh-world real-fit sets fill");
     Check(prospect.LiveSet.Where(s=>s.IsOriginal).All(s=>s.SongId==null),"Unpublished supplied songs do not inflate the artist-original bucket");
     rows.Add(new {kind="freshWorldProjectedReading",genre=act.primaryGenre.ToString(),year,artistId=act.artistId,unsigned=unsigned.Contains(act.artistId),instrumental=LiveRepertoire.Instrumental(act),
      slots=prospect.LiveSet.Select(item=>new {category=item.IsOriginal?"Original":mix.Source(CompositionCatalogService.GetSong(item.SongId),year).ToString(),songId=item.SongId}).ToArray()});
    }
   }
  } finally {TimeManager.Instance.RestoreClock(date,hour,minute);}
  string path=ProjectSettings.GlobalizePath($"res://SimLogs/folk-easy-check-{SimulationSeedBootstrap.RequestedSeed}.json");
  File.WriteAllText(path,JsonSerializer.Serialize(new {seed=SimulationSeedBootstrap.RequestedSeed,rows},new JsonSerializerOptions {WriteIndented=true}));
  GD.Print("FOLK_EASY_CHECK_PASS "+path);
 }
}
