using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

// Targeted monthly diagnostics are separate from the disabled ecosystem census.
public partial class ChartAuditRunner {
 private StreamWriter directive2Taste, directive2Slots;
 private StreamWriter directive3Census;
 private StreamWriter directive3AReplay;
 private int directive2Quarter=-1;
 private void CaptureDirective2Week() {
  CaptureFolkEasyWeek();
  if(!OS.GetCmdlineUserArgs().Contains("--polar-directive2-trajectory"))return;
  var date=TimeManager.Instance.CurrentDate;
  directive2Taste ??= CreateWriter(PolarResearchPath("-directive2-taste.csv"));
  if(directive2Taste.BaseStream.Position==0)directive2Taste.WriteLine("date,year,month,n,raw,adjusted");
  var taste=PolarSongBehavior.CaptureTaste().GetValueOrDefault(Genre.Gospel);
  var adjusted=PolarSongBehavior.ShrinkTaste(taste,Genre.Gospel,PolarSongTable.RepairTable);
  directive2Taste.WriteLine(string.Join(",",date.ToShortString(),date.year,date.month,taste?.observedRecordIds?.Length??0,
   Csv(string.Join(";",taste?.identity.Select(v=>DF(v))??Array.Empty<string>())),Csv(string.Join(";",adjusted?.identity.Select(v=>DF(v))??Array.Empty<string>()))));
  directive2Taste.Flush();
  // First completed chart week of each month, matching retained monthly aggregation.
  if(date.day>8 || directive2Quarter==date.year*12+date.month)return;
  directive2Quarter=date.year*12+date.month;
  directive2Slots ??= CreateWriter(PolarResearchPath("-directive2-quarter-slots.csv"));
  bool directive3=OS.GetCmdlineUserArgs().Contains("--polar-directive3-trajectory");
  if(directive2Slots.BaseStream.Position==0)directive2Slots.WriteLine("date,year,quarter,genre,unsigned,artistId,cohort,population,sample,slot,songId,category,admissionRoutes"+(directive3?",family,origin,context,albumCount":""));
  var genres=new[]{Genre.Gospel,Genre.Folk,Genre.Country,Genre.EasyListening,Genre.Classical,Genre.Jazz,Genre.Blues,Genre.TraditionalPop};
  var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet(StringComparer.Ordinal);
  var census=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId).ToArray();
  var slotCounts=new System.Collections.Generic.Dictionary<string,int>();
  var all=census.Where(a=>genres.Contains(a.primaryGenre));
  foreach(var cell in all.GroupBy(a=>(a.primaryGenre,Unsigned:unsigned.Contains(a.artistId),Cohort:LiveRepertoire.Cohort(a,date.year)))) {
   // Gospel is a full genre cohort; other genres retain the fixed-world four-act cap.
   var panel=cell.OrderBy(a=>RepertoireTaxonomy.Hash(a.artistId+"|repair-panel")).Take(cell.Key.primaryGenre==Genre.Gospel?int.MaxValue:4).ToArray();
   foreach(var artist in panel) {
    var prior=artist.repertoireState;
    try {
     using var random=new RandomNumberGenerator {Seed=CensusSeed($"{requestedSeed}:{date.year}:{date.month}:{artist.artistId}")};
     var prospect=new PlayerDesk.Prospect {Artist=artist};
     int requested=0;
     PlayerDesk.Instance.BuildLiveSet(prospect,artist,date.year,0,random,(want,pool)=>requested=want);
     if(OS.GetCmdlineUserArgs().Contains("--polar-directive3-a-replay")&&artist.primaryGenre is Genre.Folk or Genre.EasyListening) {
      directive3AReplay??=CreateWriter(PolarResearchPath("-directive3-a-replay.jsonl"));
      PolarGospelRepairProbe.WriteAReplay(directive3AReplay,artist,prospect,requested,cell.Key.Unsigned,date,ChartManager.Instance.GetCurrentChartWeek()+1);
     }
     slotCounts[artist.artistId]=prospect.LiveSet.Count;
     int albumCount=directive3?LiveRepertoire.Pool(artist,date.year,date.month).Count(s=>s.repertoireAdmissionRoutes.Contains("albumRepertoire")):0;
     for(int i=0;i<prospect.LiveSet.Count;i++) {
      var item=prospect.LiveSet[i];var song=CompositionCatalogService.GetSong(item.SongId);
       directive2Slots.WriteLine(string.Join(",",date.ToShortString(),date.year,(date.month+2)/3,artist.primaryGenre,cell.Key.Unsigned,artist.artistId,cell.Key.Cohort,cell.Count(),panel.Length,i,Csv(item.SongId),item.IsOriginal?"newlyAuthored":RepertoireProvenance.Category(song,date.year),Csv(string.Join(";",song?.repertoireAdmissionRoutes??new())))+(directive3?","+string.Join(",",Csv(song?.repertoireSeedFamily??""),song?.originKind.ToString()??"newOriginal",(song?.contentContext??item.ContentContext).ToString(),albumCount):""));
     }
    } finally {artist.repertoireState=prior;}
   }
  }
  directive2Slots.Flush();
  if(directive3) {
   directive3Census??=CreateWriter(PolarResearchPath("-directive3-census.jsonl"));
   foreach(var a in census.OrderBy(a=>a.artistId,StringComparer.Ordinal))directive3Census.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {date=date.ToShortString(),year=date.year,month=date.month,artistId=a.artistId,genre=a.primaryGenre.ToString(),unsigned=unsigned.Contains(a.artistId),formationGenre=a.formationPrimaryGenre.ToString(),formationCohort=a.cohort.ToString(),formedYear=a.formedYear,lifecycle=a.lifecycleStatus.ToString(),contractSequence=a.contractSequence,slots=slotCounts.TryGetValue(a.artistId,out int n)?(int?)n:null}));
   directive3Census.Flush();
  }
 }
 private void CloseDirective2() {folkEasySlots?.Dispose();directive2Taste?.Dispose();directive2Slots?.Dispose();directive3Census?.Dispose();directive3AReplay?.Dispose();}
}
