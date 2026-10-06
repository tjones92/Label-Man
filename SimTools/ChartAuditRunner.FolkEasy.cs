using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
 private StreamWriter folkEasySlots;
 private int folkEasyMonth=-1;
 private void CaptureFolkEasyWeek() {
  if(!OS.GetCmdlineUserArgs().Contains("--folk-easy-trajectory"))return;
  var date=TimeManager.Instance.CurrentDate;
  if(date.day>8||folkEasyMonth==date.year*12+date.month)return;
  folkEasyMonth=date.year*12+date.month;
  folkEasySlots??=CreateWriter(PolarResearchPath("-folk-easy-slots.csv"));
  if(folkEasySlots.BaseStream.Position==0)folkEasySlots.WriteLine("seed,date,year,month,genre,unsigned,instrumental,artistId,population,sample,slot,category,songId,originYear,seedFamily,mediaSource,mediaRecordId,setSize,requestedCovers,traditionalAccess,standardAccess,contemporaryAccess");
  var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet();
  var all=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId)
   .Where(a=>a.primaryGenre is Genre.Folk or Genre.ContemporaryFolk or Genre.EasyListening).ToArray();
  foreach(var cell in all.GroupBy(a=>(a.primaryGenre,Unsigned:unsigned.Contains(a.artistId),Instrumental:LiveRepertoire.Instrumental(a)))) {
   var panel=cell.OrderBy(a=>RepertoireTaxonomy.Hash(a.artistId+"|folk-easy-panel")).Take(12).ToArray();
   foreach(var act in panel) {
    var prior=act.repertoireState;
    try {
     using var random=new RandomNumberGenerator {Seed=CensusSeed($"{requestedSeed}:{date.year}:{date.month}:{act.artistId}")};
     var set=new PlayerDesk.Prospect {Artist=act};var mix=LiveRepertoire.SetMix(act,date.year);
     int wanted=0;int[] access=new int[3];
     PlayerDesk.Instance.BuildLiveSet(set,act,date.year,0,random,(count,pool)=> {wanted=count;foreach(var song in pool)access[(int)mix.Source(song,date.year)]++;});
     for(int slot=0;slot<set.LiveSet.Count;slot++) {
      var item=set.LiveSet[slot];var song=CompositionCatalogService.GetSong(item.SongId);
      folkEasySlots.WriteLine(string.Join(",",requestedSeed,date.ToShortString(),date.year,date.month,act.primaryGenre,cell.Key.Unsigned,cell.Key.Instrumental,Csv(act.artistId),cell.Count(),panel.Length,slot,
       item.IsOriginal?"Original":mix.Source(song,date.year).ToString(),Csv(item.SongId),song?.originYear,Csv(song?.repertoireSeedFamily),song?.externalMediaSourceType,Csv(song?.externalMediaSourceRecordId),set.LiveSet.Count,wanted,access[0],access[1],access[2]));
     }
    } finally {act.repertoireState=prior;}
   }
  }
  folkEasySlots.Flush();
 }
}
