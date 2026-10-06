using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Opt-in measurement only. Uses the same pools, proposals and refusal predicate as live selection.
// Does not register songs, write gameplay state or consume a global random stream.
public partial class ChartAuditRunner {
 private StreamWriter genreFollowUpSlots, genreFollowUpPools, genreFollowUpCatalog,genreFollowUpSources;
 private StreamWriter genreRepairReference;
 private Action<SongMaterialSelectionService.LiveSourceDecision> priorLiveSourceObserver;
 private readonly HashSet<string> genreFollowUpCatalogDates = new();
 private void OpenGenreFollowUp(string directory) {
  genreFollowUpSlots=CreateWriter(Path.Combine(directory,runName+"-genre-followup-slots.csv"));
  genreFollowUpSlots.WriteLine("seed,date,genre,unsigned,artistId,artistName,formedYear,cohort,writingBin,writingAbility,instrumental,slot,songId,songTitle,category,originYear,originKind,primaryGenre,secondaryGenre,seedFamily,publicDomain,rightsStatus,sampleWeight,crossSection,vocalPresence,originArtistId,ownAuthored,sourceCharted");
  genreFollowUpPools=CreateWriter(Path.Combine(directory,runName+"-genre-followup-pools.csv"));
  genreFollowUpPools.WriteLine("seed,date,genre,unsigned,artistId,artistName,formedYear,cohort,writingBin,writingAbility,instrumental,sampleWeight,crossSection,requestedCovers,filledSlots,category,seedFamily,primaryGenre,secondaryGenre,accessed,accepted,bestScore,meanScore,topFitBand,inheritedSongIds");
  genreFollowUpCatalog=CreateWriter(Path.Combine(directory,runName+"-genre-followup-catalog.csv"));
  genreFollowUpCatalog.WriteLine("seed,date,category,seedFamily,primaryGenre,secondaryGenre,count");
  if(genreRepairCensus) {
   genreRepairReference=CreateWriter(Path.Combine(directory,runName+"-genre-repair-reference-slots.csv"));
   genreRepairReference.WriteLine("seed,date,genre,unsigned,artistId,slot,songId,category,originYear,originKind,primaryGenre,secondaryGenre,seedFamily,publicDomain,sampleWeight,crossSection,contentContext");
  }
  genreFollowUpSources=CreateWriter(Path.Combine(directory,runName+"-genre-followup-sources.csv"));
  genreFollowUpSources.WriteLine("seed,date,genre,unsigned,artistId,coverSlot,inheritedRequested,fitFallback,source,songId,sampleWeight,crossSection");
  priorLiveSourceObserver=SongMaterialSelectionService.ObserveLiveSource;
  SongMaterialSelectionService.ObserveLiveSource=decision=> {
   priorLiveSourceObserver?.Invoke(decision);
   if(currentPolarSample==null||decision.Artist.artistId!=currentPolarSample.Artist.artistId)return;
   genreFollowUpSources.WriteLine(string.Join(",",requestedSeed,TimeManager.Instance.CurrentDate.ToShortString(),decision.Artist.primaryGenre,currentPolarSample.Unsigned,Csv(decision.Artist.artistId),decision.Slot,
    decision.InheritedRequested,decision.FitFallback,decision.Source,Csv(decision.SongId),DF(currentPolarSample.Weight),currentPolarSample.CrossSection));
  };
 }
 private void CaptureGenreFollowUp(SimulatedArtist artist,PlayerDesk.Prospect prospect,List<SongComposition> pool,int requested,bool unsigned) {
  var date=TimeManager.Instance.CurrentDate;int year=date.year;
  string prefix=string.Join(",",requestedSeed,date.ToShortString(),artist.primaryGenre,unsigned,Csv(artist.artistId),Csv(artist.stageName),artist.formedYear,
   LiveRepertoire.Cohort(artist,year),currentPolarSample.WritingBin,DF(artist.songwritingAbility),LiveRepertoire.Instrumental(artist));
  for(int slot=0;slot<prospect.LiveSet.Count;slot++) {
   var item=prospect.LiveSet[slot];var song=CompositionCatalogService.GetSong(item.SongId);
   genreFollowUpSlots.WriteLine(prefix+","+string.Join(",",slot,Csv(item.SongId),Csv(song?.title??"New live original"),RepertoireProvenance.CategoryForAct(song,artist,year,item.IsOriginal),
    song?.originYear??year,song?.originKind.ToString()??"ArtistOriginal",song?.primaryGenre.ToString()??artist.primaryGenre.ToString(),song?.secondaryGenre.ToString()??"",Csv(song?.repertoireSeedFamily),song?.isPublicDomain??false,
    song?.rights?.controlType.ToString()??"Unknown",DF(currentPolarSample.Weight),currentPolarSample.CrossSection,song?.demoTaxonomy?.vocalPresence.ToString()??"placeholder",Csv(song?.originArtistId),song!=null&&LiveRepertoire.OwnOriginal(song,artist),song?.recordings?.Any(r=>r.peakPosition>0)??false));
  }
  bool empty=PlayerDesk.Instance.RepertoireFor(artist.artistId).Count==0&&!PlayerDesk.Instance.SongsFor(artist.artistId).Any();
  bool bypass=ChartManager.Instance?.GetLabelById(artist.labelId)?.isPlayerOwned==true;
  var candidates=pool.Where(s=>s!=null&&s.originYear<=year&&LiveRepertoire.EligibleLive(s,artist)).DistinctBy(s=>s.songId).Select(s=> {
   var proposal=PolarSongBehavior.LiveProposal(s,artist,year);
   return new {Song=s,Category=RepertoireProvenance.CategoryForAct(s,artist,year),Score=PolarSongBehavior.SelectionScore(proposal.fit),Accepted=bypass||!PolarSongBehavior.Refuses(proposal.fit,artist,empty)};
  }).ToArray();
  double best=candidates.Where(c=>c.Accepted).Select(c=>(double)c.Score).DefaultIfEmpty(-1).Max();
  foreach(var group in candidates.GroupBy(c=>(c.Category,c.Song.repertoireSeedFamily,c.Song.primaryGenre,c.Song.secondaryGenre))) {
   var accepted=group.Where(c=>c.Accepted).ToArray();
   genreFollowUpPools.WriteLine(prefix+","+string.Join(",",DF(currentPolarSample.Weight),currentPolarSample.CrossSection,requested,prospect.LiveSet.Count,group.Key.Category,Csv(group.Key.repertoireSeedFamily),group.Key.primaryGenre,group.Key.secondaryGenre,
    group.Count(),accepted.Length,accepted.Length==0?"":DF(accepted.Max(c=>c.Score)),accepted.Length==0?"":DF(accepted.Average(c=>c.Score)),accepted.Count(c=>best-c.Score<PolarRepertoireTable.Current.N("suitabilityWindow")),
    Csv(group.Key.Category is "traditionalLineage" or "establishedStandard"?string.Join(";",accepted.Select(c=>c.Song.songId).OrderBy(id=>id,StringComparer.Ordinal)):"")));
  }
  if(genreFollowUpCatalogDates.Add(date.ToShortString())) foreach(var group in CompositionCatalogService.AllSongs.Where(s=>s.originYear<=year).GroupBy(s=>(Category:RepertoireProvenance.Category(s,year),s.repertoireSeedFamily,s.primaryGenre,s.secondaryGenre)))
   genreFollowUpCatalog.WriteLine(string.Join(",",requestedSeed,date.ToShortString(),group.Key.Category,Csv(group.Key.repertoireSeedFamily),group.Key.primaryGenre,group.Key.secondaryGenre,group.Count()));
 }
 private void CloseGenreFollowUp() {
  if(genreFollowUpSources!=null)SongMaterialSelectionService.ObserveLiveSource=priorLiveSourceObserver;
  genreFollowUpSlots?.Dispose();genreFollowUpPools?.Dispose();genreFollowUpCatalog?.Dispose();genreFollowUpSources?.Dispose();genreFollowUpSources=null;genreRepairReference?.Dispose();
 }
 private void CaptureGenreRepairReference(SimulatedArtist artist,bool unsigned,GameDate date,ActRepertoireState priorState) {
  bool mode=LiveRepertoire.AuditGenreRepair,easyMode=LiveRepertoire.AuditEasySoundtracks;var observer=SongMaterialSelectionService.ObserveLiveSource;
  try {
   if(easySoundtrackCensus)LiveRepertoire.AuditEasySoundtracks=false;
   else LiveRepertoire.AuditGenreRepair=false;
   SongMaterialSelectionService.ObserveLiveSource=null;
   using var random=new Godot.RandomNumberGenerator {Seed=CensusSeed($"{requestedSeed}:{date.year}:{date.month}:{artist.artistId}")};
   var set=new PlayerDesk.Prospect {Artist=artist};PlayerDesk.Instance.BuildLiveSet(set,artist,date.year,0,random);
   for(int i=0;i<set.LiveSet.Count;i++) {
    var item=set.LiveSet[i];var song=CompositionCatalogService.GetSong(item.SongId);
    genreRepairReference.WriteLine(string.Join(",",requestedSeed,date.ToShortString(),artist.primaryGenre,unsigned,Csv(artist.artistId),i,Csv(item.SongId),RepertoireProvenance.CategoryForAct(song,artist,date.year,item.IsOriginal),
     song?.originYear??date.year,song?.originKind.ToString()??"ArtistOriginal",song?.primaryGenre.ToString()??artist.primaryGenre.ToString(),song?.secondaryGenre.ToString()??"",Csv(song?.repertoireSeedFamily),song?.isPublicDomain??false,DF(currentPolarSample.Weight),currentPolarSample.CrossSection,
     (song?.contentContext??item.ContentContext).ToString()));
   }
  } finally {LiveRepertoire.AuditGenreRepair=mode;LiveRepertoire.AuditEasySoundtracks=easyMode;SongMaterialSelectionService.ObserveLiveSource=observer;artist.repertoireState=priorState;}
 }
}
