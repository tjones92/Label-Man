using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Additional measurement streams; no gameplay/RNG writes.
public partial class ChartAuditRunner {
 private StreamWriter repertoireSlots, repertoireAdmissions, repertoireOrdering;
 private int repertoireAdmissionCursor;
 private string repertoireDirectory;
 private readonly HashSet<string> repertoireSampled = new(StringComparer.Ordinal);
 private void OpenRepertoireAudit(string directory) {
  repertoireDirectory=directory;
  repertoireSlots=CreateWriter(Path.Combine(directory,runName+"-repertoire-slots.csv"));
  repertoireSlots.WriteLine("seed,date,year,month,genre,artistId,unsigned,slot,songId,category,publicDomain,archetype,originYear,originKind,instrumental,writingAbility,cohort,seedFamily,arrangementArchetype,sampleCohort,writingBin,stratumPopulation,stratumSample,sampleWeight,crossSection,panel");
  repertoireAdmissions=CreateWriter(Path.Combine(directory,runName+"-repertoire-admissions.csv"));
  repertoireAdmissions.WriteLine("seed,songId,route,family,admissionYear,recordId,originYear,primaryGenre,secondaryGenre,traditional,publicDomain,legacyStandard,established,chartCompletionYears,chartPeaks,tags,firstReleaseYear,recordingDates,admissionMonth");
  repertoireOrdering=CreateWriter(Path.Combine(directory,runName+"-repertoire-ordering.csv"));
  repertoireOrdering.WriteLine("seed,date,genre,unsigned,artistId,candidates,requested,changedSlots,referenceCapability,resolvedCapability");
 }
 private void CaptureRepertoireAudit(SimulatedArtist artist,PlayerDesk.Prospect prospect,List<SongComposition> pool,int requested,bool unsigned) {
  var date=TimeManager.Instance.CurrentDate;
  string prefix=string.Join(",",requestedSeed,date.ToShortString(),date.year,date.month,artist.primaryGenre,Csv(artist.artistId),unsigned);
  for(int i=0;i<prospect.LiveSet.Count;i++) {
   var item=prospect.LiveSet[i];var song=CompositionCatalogService.GetSong(item.SongId);
   var profile=song==null?null:SongProfileDeriver.Derive(song,song.demoTaxonomy,PolarSongTable.Current);
   var arrangement=song==null||item.IsOriginal?null:PolarSongBehavior.LiveProposal(song,artist,date.year);
   repertoireSlots.WriteLine(prefix+","+string.Join(",",i,Csv(item.SongId),item.IsOriginal?"newlyAuthored":RepertoireProvenance.Category(song,date.year),song?.isPublicDomain??false,profile?.archetype.ToString()??"placeholder",song?.originYear??date.year,song?.originKind.ToString()??"ArtistOriginal",LiveRepertoire.Instrumental(artist),DF(artist.songwritingAbility),LiveRepertoire.Cohort(artist,date.year),Csv(song?.repertoireSeedFamily),arrangement?.realizedProfile.archetype.ToString()??"placeholder")+","+PolarSampleSuffix);
  }
  // One act per genre/cohort/vocal role/population/month; identical actors/pools for the A/B.
  if(polarDiagnosticsUsed < polarDiagnosticLimit && repertoireSampled.Add($"{date.year}:{date.month}:{artist.primaryGenre}:{LiveRepertoire.Cohort(artist,date.year)}:{LiveRepertoire.Instrumental(artist)}:{unsigned}")) {
   polarDiagnosticsUsed++;
   var candidates=pool.Where(s=>s!=null&&s.originYear<=date.year).DistinctBy(s=>s.songId).Select(s=>new {
    Song=s,Reference=PolarSongBehavior.Fit(s,artist,artist.primaryGenre,date.year),
    Resolved=PolarSongBehavior.Prepare(new SelectedSongMaterial {Song=s,IsCover=true},artist,new Record {recordId=$"live:{artist.artistId}:{date.year}:{s.songId}",primaryGenre=artist.primaryGenre},artist.primaryGenre,date.year).PolarProposal?.fit
   }).Where(x=>x.Resolved.HasValue).ToArray();
   var reference=candidates.OrderByDescending(x=>PolarSongBehavior.SelectionScore(x.Reference)).ThenByDescending(x=>x.Song.primaryGenre==artist.primaryGenre).ThenBy(x=>x.Song.songId,StringComparer.Ordinal).Take(requested).ToArray();
   var resolved=candidates.OrderByDescending(x=>PolarSongBehavior.SelectionScore(x.Resolved.Value)).ThenByDescending(x=>x.Song.primaryGenre==artist.primaryGenre).ThenBy(x=>x.Song.songId,StringComparer.Ordinal).Take(requested).ToArray();
   var ids=reference.Select(x=>x.Song.songId).ToHashSet();
   repertoireOrdering.WriteLine(string.Join(",",requestedSeed,date.ToShortString(),artist.primaryGenre,unsigned,Csv(artist.artistId),candidates.Length,requested,resolved.Count(x=>!ids.Contains(x.Song.songId)),DF(reference.Length==0?0:reference.Average(x=>x.Reference.Capability)),DF(resolved.Length==0?0:resolved.Average(x=>x.Resolved.Value.Capability))));
  }
 }
 private void FlushRepertoireAdmissions() {
  var recordingDates=PolarSongMetadataService.Masters.Values.GroupBy(m=>m.songId).ToDictionary(g=>g.Key,g=>string.Join(";",g.Select(m=>$"{m.masterId}:{m.recordingYear}-{m.recordingMonth:D2}-{m.recordingDay:D2}")));
  foreach(var a in RepertoireProvenance.Admissions.Skip(repertoireAdmissionCursor)) {
   var s=CompositionCatalogService.GetSong(a.SongId);if(s==null)continue;
   repertoireAdmissions.WriteLine(string.Join(",",requestedSeed,Csv(s.songId),a.Route,Csv(a.Family),a.Year,Csv(a.RecordId),s.originYear,s.primaryGenre,s.secondaryGenre,s.isTraditional,s.isPublicDomain,s.isStandard,s.EstablishedAsOf(a.Year),Csv(string.Join(";",s.recordings.Select(r=>r.year))),Csv(string.Join(";",s.recordings.Select(r=>r.peakPosition))),Csv(string.Join(";",s.genreTagIds)),s.repertoireFirstReleaseYear,Csv(recordingDates.GetValueOrDefault(s.songId,"")),a.Month));
  }
  repertoireAdmissionCursor=RepertoireProvenance.Admissions.Count;
  repertoireAdmissions.Flush();repertoireSlots.Flush();repertoireOrdering.Flush();
 }
 private void CloseRepertoireAudit() {
  FlushRepertoireAdmissions();repertoireSlots.Dispose();repertoireAdmissions.Dispose();repertoireOrdering.Dispose();
  using var inventory=CreateWriter(Path.Combine(repertoireDirectory,runName+"-repertoire-inventory.csv"));
  inventory.WriteLine("songId,genre,secondaryGenre,originYear,originKind,seedFamily,archetype,traditional,publicDomain,establishedYear,firstReleaseYear,admissionRoutes");
  foreach(var s in CompositionCatalogService.AllSongs.OrderBy(s=>s.songId,StringComparer.Ordinal))inventory.WriteLine(string.Join(",",Csv(s.songId),s.primaryGenre,s.secondaryGenre,s.originYear,s.originKind,Csv(s.repertoireSeedFamily),SongProfileDeriver.Derive(s,s.demoTaxonomy,PolarSongTable.Current).archetype,s.isTraditional,s.isPublicDomain,s.establishedYear,s.repertoireFirstReleaseYear,Csv(string.Join(";",s.repertoireAdmissionRoutes))));
 }
}
