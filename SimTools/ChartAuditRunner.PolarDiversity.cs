using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

// Census-only observations. No settings, catalogue entries or global RNG are changed.
public partial class ChartAuditRunner {
 private StreamWriter polarDiversitySetWriter, polarDiversitySongWriter, polarDiversityPoolWriter;
 private static string DiversityCategory(SongComposition s) => s.isTraditional || s.isPublicDomain ? "traditional" : s.isStandard ? "standard" : s.originKind == SongOriginKind.RecentHit ? "recentHit" : "ordinaryCover";
 private static string DF(double v) => v.ToString("R", CultureInfo.InvariantCulture);
 private void OpenPolarDiversity(string directory) {
  OpenRepertoireAudit(directory);
  polarDiversitySetWriter=CreateWriter(Path.Combine(directory,runName+"-polar-diversity-sets.csv"));
  polarDiversitySetWriter.WriteLine("seed,date,year,month,genre,artistId,unsigned,requestedCovers,filledCovers,originals,shortSet,eligibleSongs,futureSongs,tieAuditApplicable,exactTieSelected,idTieSelected,reverseIdChangedSlots,genreTieChangedSlots,nearTieBoundaryGap,cohort,writingBin,stratumPopulation,stratumSample,sampleWeight,crossSection,panel,candidateDiagnosticsMeasured");
  polarDiversitySongWriter=CreateWriter(Path.Combine(directory,runName+"-polar-diversity-songs.csv"));
  polarDiversitySongWriter.WriteLine("seed,date,year,month,genre,artistId,unsigned,slot,songId,category,capability,identity,stretch,score");
  polarDiversityPoolWriter=CreateWriter(Path.Combine(directory,runName+"-polar-diversity-pools.csv"));
  polarDiversityPoolWriter.WriteLine("seed,date,year,month,genre,artistId,unsigned,category,count,meanCapability,meanIdentity,meanStretch,capabilityAbove095");
 }
 private void CapturePolarDiversitySet(SimulatedArtist artist,PlayerDesk.Prospect prospect,List<SongComposition> pool,int requested,bool unsigned) {
  if(polarDiversitySetWriter==null)return;
  var date=TimeManager.Instance.CurrentDate;int year=date.year;
  int diagnosticsBefore=polarDiagnosticsUsed;
  CaptureRepertoireAudit(artist,prospect,pool,requested,unsigned);
  var distinct=pool.Where(s=>s!=null).GroupBy(s=>s.songId).Select(g=>g.First()).ToArray();
  if(polarDiagnosticsUsed == diagnosticsBefore) {
   var eligible=distinct.Where(s=>s.originYear<=year).ToArray();
   var performed=prospect.LiveSet.Where(s=>!s.IsOriginal).ToArray();
   if(performed.Select(s=>s.SongId).Distinct().Count()!=performed.Length || performed.Any(s=>!eligible.Any(e=>e.songId==s.SongId))) throw new InvalidOperationException("Invalid performed set");
   string fastPrefix=string.Join(",",requestedSeed,date.ToShortString(),year,date.month,artist.primaryGenre,Csv(artist.artistId),unsigned);
   for(int i=0;i<performed.Length;i++) {
    var song=CompositionCatalogService.GetSong(performed[i].SongId);var fit=PolarSongBehavior.Fit(song,artist,artist.primaryGenre,year);
    polarDiversitySongWriter.WriteLine(fastPrefix+","+string.Join(",",i,Csv(song.songId),DiversityCategory(song),DF(fit.Capability),DF(fit.Identity),DF(fit.Stretch),DF(PolarSongBehavior.SelectionScore(fit))));
   }
   polarDiversitySetWriter.WriteLine(fastPrefix+","+string.Join(",",requested,performed.Length,prospect.LiveSet.Count(s=>s.IsOriginal),prospect.LiveSet.Count<3,eligible.Length,distinct.Length-eligible.Length,false,"","","","","")+","+PolarSampleSuffix+",False");
   return;
  }
  var candidates=distinct.Where(s=>s.originYear<=year).Select(s=>new {Song=s,Fit=PolarSongBehavior.Fit(s,artist,artist.primaryGenre,year)}).Select(x=>new {x.Song,x.Fit,Score=PolarSongBehavior.SelectionScore(x.Fit),ExactGenre=x.Song.primaryGenre==artist.primaryGenre}).ToArray();
  string prefix=string.Join(",",requestedSeed,date.ToShortString(),year,date.month,artist.primaryGenre,Csv(artist.artistId),unsigned);
  foreach(var group in candidates.GroupBy(x=>DiversityCategory(x.Song))) polarDiversityPoolWriter.WriteLine(prefix+","+string.Join(",",group.Key,group.Count(),DF(group.Average(x=>x.Fit.Capability)),DF(group.Average(x=>x.Fit.Identity)),DF(group.Average(x=>x.Fit.Stretch)),group.Count(x=>x.Fit.Capability>.95f)));
  var covers=prospect.LiveSet.Where(s=>!s.IsOriginal).ToArray();
  for(int i=0;i<covers.Length;i++) {
   var song=CompositionCatalogService.GetSong(covers[i].SongId);var fit=PolarSongBehavior.Fit(song,artist,artist.primaryGenre,year);
   polarDiversitySongWriter.WriteLine(prefix+","+string.Join(",",i,Csv(song.songId),DiversityCategory(song),DF(fit.Capability),DF(fit.Identity),DF(fit.Stretch),DF(PolarSongBehavior.SelectionScore(fit))));
  }
  bool applicable=PolarSongBehavior.UsePolarFitSelection&&!SongMaterialSelectionService.IsRockSongbookContext(artist.primaryGenre);
  int exact=0,id=0,changed=0,genreChanged=0;string gap="";
  if(applicable) {
   bool empty=PlayerDesk.Instance.RepertoireFor(artist.artistId).Count==0&&!PlayerDesk.Instance.SongsFor(artist.artistId).Any();
   // Same live proposal key and resolved refusal as SelectLiveCovers, without emitting avoidance events.
   bool Allowed(SongComposition song) => !PolarSongBehavior.Refuses(PolarSongBehavior.Prepare(new SelectedSongMaterial {Song=song,IsCover=true},artist,new Record {recordId=$"live:{artist.artistId}:{year}:{song.songId}",primaryGenre=artist.primaryGenre},artist.primaryGenre,year).PolarProposal.fit,artist,empty);
   var ranked=candidates.OrderByDescending(x=>x.Score).ThenByDescending(x=>x.ExactGenre).ThenBy(x=>x.Song.songId,StringComparer.Ordinal).ToArray();
   // After repair, observe the already-performed set rather than resolving every candidate twice.
   // The causal phase probe and selection checks verify the deterministic ranking independently.
   var selected=LiveRepertoire.AuditPhase<3 ? PolarSongBehavior.RankLive(distinct,artist,year).Where(Allowed).Take(requested).Select(x=>x.songId).ToHashSet(StringComparer.Ordinal) : covers.Select(x=>x.SongId).ToHashSet(StringComparer.Ordinal);
   if(selected.Count!=covers.Length||!selected.IsSubsetOf(candidates.Select(x=>x.Song.songId).ToHashSet(StringComparer.Ordinal))||!selected.SetEquals(covers.Select(x=>x.SongId)))throw new InvalidOperationException("Diversity selection did not match eligible performed compositions");
   exact=ranked.Where(x=>selected.Contains(x.Song.songId)).Count(x=>candidates.Count(y=>y.Score==x.Score)>1);
   // ID is no longer a selection decision. Retain fit-plateau measurement in exactTieSelected.
   id=0;changed=0;genreChanged=0;
   if(LiveRepertoire.AuditPhase<3) {
    id=ranked.Where(x=>selected.Contains(x.Song.songId)).Count(x=>candidates.Count(y=>y.Score==x.Score&&y.ExactGenre==x.ExactGenre)>1);
    changed=ranked.OrderByDescending(x=>x.Score).ThenByDescending(x=>x.ExactGenre).ThenByDescending(x=>x.Song.songId,StringComparer.Ordinal).Where(x=>Allowed(x.Song)).Take(requested).Count(x=>!selected.Contains(x.Song.songId));
    genreChanged=candidates.OrderByDescending(x=>x.Score).ThenBy(x=>x.ExactGenre).ThenBy(x=>x.Song.songId,StringComparer.Ordinal).Where(x=>Allowed(x.Song)).Take(requested).Count(x=>!selected.Contains(x.Song.songId));
   }
   if(requested>0&&ranked.Length>requested)gap=DF(ranked[requested-1].Score-ranked[requested].Score);
  }
  polarDiversitySetWriter.WriteLine(prefix+","+string.Join(",",requested,covers.Length,prospect.LiveSet.Count(x=>x.IsOriginal),prospect.LiveSet.Count<3,candidates.Length,distinct.Length-candidates.Length,applicable,exact,id,changed,genreChanged,gap)+","+PolarSampleSuffix+",True");
 }
 private void ClosePolarDiversity() {if(polarDiversitySetWriter!=null)CloseRepertoireAudit();polarDiversitySetWriter?.Dispose();polarDiversitySongWriter?.Dispose();polarDiversityPoolWriter?.Dispose();polarDiversitySetWriter=polarDiversitySongWriter=polarDiversityPoolWriter=null;}
}
