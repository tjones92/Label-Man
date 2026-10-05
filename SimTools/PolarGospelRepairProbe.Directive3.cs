using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public partial class PolarGospelRepairProbe {
 private readonly Dictionary<string,SongComposition> directive3V2=new(StringComparer.Ordinal);
 private SongComposition[] directive3Placeholders;
 private SongComposition V2Song(SongComposition song,PolarSongTable table) {
  if(directive3V2.TryGetValue(song.songId,out var copy))return copy;
  copy=JsonSerializer.Deserialize<SongComposition>(JsonSerializer.Serialize(song,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  copy.shapeVariation=CompositionShapeVariation.V2Offsets(copy,table);copy.shapeVariationSchemaVersion=2;
  directive3V2.Add(song.songId,copy);return copy;
 }
 private void WriteDirective3Shapes(string prefix,PolarSongTable table) {
  var book=CompositionCatalogService.AllSongs.Where(GospelBook).OrderBy(s=>s.songId,StringComparer.Ordinal).ToArray();
  File.WriteAllText(prefix+"-directive3-dose-source.json",JsonSerializer.Serialize(book.Select(s=>new {
   s.songId,archetype=s.demoTaxonomy?.archetype.ToString(),s.commercialHook,s.nationalFamiliarity,s.adultFamiliarity,s.teenFamiliarity,
   s.originYear,s.repertoireVariation,context=s.contentContext.ToString(),s.contextAuthorshipEvidence
  })));
  using var output=new StreamWriter(prefix+"-directive3-shapes.jsonl");
  int initial=CompositionShapeVariation.ActiveVersion;
  try {
   foreach(var song in CompositionCatalogService.AllSongs.OrderBy(s=>s.songId,StringComparer.Ordinal)) {
    var rows=new Dictionary<string,float[]>();
    foreach(int version in new[]{0,1,2}) {
     CompositionShapeVariation.ActiveVersion=version;
     var shape=SongProfileDeriver.Derive(version==2?V2Song(song,table):song,song.demoTaxonomy,table);
     rows[version==0?"legacy":version==1?"v1":"v2"]=shape.axes;
    }
    var p=SongProfileDeriver.Derive(song,song.demoTaxonomy,table);
    output.WriteLine(JsonSerializer.Serialize(new {songId=song.songId,title=song.title,source=song.originKind==SongOriginKind.ArtistOriginal?"original":song.repertoireSeedFamily!=null?"seeded":"scouted-imported-professional",archetype=p.archetype.ToString(),shapes=rows,offsets=directive3V2[song.songId].shapeVariation,template=table.Row(p.archetype).Axes}));
   }
  } finally {CompositionShapeVariation.ActiveVersion=initial;}
 }
 private SongComposition[] Placeholders() {
  if(directive3Placeholders!=null)return directive3Placeholders;
  var book=CompositionCatalogService.AllSongs.Where(GospelBook).OrderBy(s=>s.songId,StringComparer.Ordinal).ToArray();
  if(book.Length!=350)throw new InvalidOperationException("D2 requires current 350-song authored book");
  directive3Placeholders=Enumerable.Range(0,700).Select(i=> {
   var copy=JsonSerializer.Deserialize<SongComposition>(JsonSerializer.Serialize(book[i%350],SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
   copy.songId="directive3-placeholder-"+i;copy.title="[PROBE PLACEHOLDER] "+copy.title;
   copy.contentContext=SongContentContext.Sacred;copy.contextAuthorshipEvidence="probe-placeholder:seeded-authored-fixture:Gospel Standard";
   return copy;
  }).ToArray();return directive3Placeholders;
 }
 private void WriteDirective3Actor(StreamWriter output,StreamWriter fits,SimulatedArtist artist,List<Candidate> candidates,int requested,int originals,bool empty,
  ActProfile oldAct,ActProfile act,MarketTasteSnapshot taste,MarketTasteSnapshot shrunk,PolarSongTable table,GameDate date,int week,bool unsigned) {
  int initial=CompositionShapeVariation.ActiveVersion;
  try {
   var variants=new List<object>();
   var recent=candidates.Where(c=>c.Song.repertoireAdmissionRoutes.Contains("albumRepertoire"))
    .OrderByDescending(c=>c.Song.recordings.Select(r=>r.year).DefaultIfEmpty(c.Song.repertoireFirstReleaseYear).Max()).ThenBy(c=>c.Song.songId,StringComparer.Ordinal).Take(12).Select(c=>c.Song.songId).ToHashSet();
   var actRecent=PolarSongMetadataService.Masters.Values.Where(m=>m.artistId==artist.artistId)
    .OrderByDescending(m=>m.recordingYear).ThenByDescending(m=>m.recordingMonth).ThenByDescending(m=>m.recordingDay)
    .ThenBy(m=>m.masterId,StringComparer.Ordinal).Take(12).Select(m=>m.songId).ToHashSet();
   // Shared composition recording years are an explicitly labelled recency proxy, not invented act history.
   foreach(var c in candidates) {
    foreach(int version in new[]{1,2}) {
     CompositionShapeVariation.ActiveVersion=version;var song=version==2?V2Song(c.Song,table):c.Song;
     var reference=PolarSongBehavior.Reference(c.Song,date.year);
     var profile=SongProfileDeriver.Derive(song,c.Proposal.taxonomy,table);
     var heard=SongProfileDeriver.Derive(song,reference?.taxonomy??song.demoTaxonomy,table);
     c.RepairFits["v"+version+"Frozen"]=PolarMaterialFit.Evaluate(heard,profile,act,shrunk,week,table);
     c.RepairFits["v"+version+"Resolved"]=PolarCoverResolver.Propose(song,reference,act,artist.primaryGenre,date.year,week,$"live:{artist.artistId}:{date.year}:{song.songId}",shrunk,table).fit;
     c.ReferenceFits["v"+version+"Frozen"]=PolarMaterialFit.Compute(heard,act,shrunk,week,table);
     c.ReferenceFits["v"+version+"Resolved"]=c.ReferenceFits["v"+version+"Frozen"];
     foreach(var name in new[]{"v"+version+"Frozen","v"+version+"Resolved"}) {
      var fit=c.RepairFits[name];fits.WriteLine(string.Join(",",artist.artistId,artist.primaryGenre,c.Song.songId,profile.archetype,name,F(fit.Capability),F(fit.Identity),F(fit.Moment)));
     }
    }
    CompositionShapeVariation.ActiveVersion=0;
    for(int axis=0;axis<4;axis++) {
     var partial=PolarActProfileDeriver.Derive(artist,ChartManager.Instance.GetLabelById(artist.labelId),null,PolarSongTable.Current);partial.axes[SongProfile.DemandCount+axis]=act.axes[SongProfile.DemandCount+axis];
     c.RepairFits["centerAxis"+axis]=PolarMaterialFit.Evaluate(c.Proposal.referenceProfile,c.Proposal.realizedProfile,partial,taste,week,table);
     c.ReferenceFits["centerAxis"+axis]=PolarMaterialFit.Compute(c.Proposal.referenceProfile,partial,taste,week,table);
    }
   }
   void Observe(string name,Candidate[] eligible,string fitName) {
    MaterialFit Fit(Candidate c)=>c.RepairFits[fitName];float Score(Candidate c)=>PolarSongBehavior.SelectionScore(Fit(c));
    var attempts=new List<object>();Candidate[] selected;
    if(SongMaterialSelectionService.IsRockSongbookContext(artist.primaryGenre)) {
     var byId=eligible.ToDictionary(c=>c.Song.songId);
     PolarSongBehavior.AuditLiveFit=(s,a)=>byId[s.songId].ReferenceFits[fitName];
     PolarSongBehavior.AuditLiveProposal=(s,a)=>new PolarArrangementProposal {fit=Fit(byId[s.songId])};
     try {selected=SongMaterialSelectionService.SelectLiveCovers(eligible.Select(c=>c.Song),artist,date.year,requested).Select(s=>byId[s.songId]).ToArray();}
     finally {PolarSongBehavior.AuditLiveFit=null;PolarSongBehavior.AuditLiveProposal=null;}
    } else selected=SelectCached(eligible,artist,date.year,requested,empty,Score,Fit,null,o=>attempts.Add(o)).ToArray();
    float best=eligible.Length==0?0:eligible.Max(Score),window=PolarRepertoireTable.Current.N("suitabilityWindow");
    var first=eligible.Where(c=>(int)((best-Score(c))/window)==0).ToArray();
    variants.Add(new {variant=name,eligible=eligible.Length,bookAccessible=eligible.Count(c=>GospelBook(c.Song)),firstFamilies=first.GroupBy(c=>Family(c.Song)).ToDictionary(g=>g.Key,g=>g.Count()),
     originals,requestedCovers=requested,filledCovers=selected.Length,inheritedSlots=selected.Count(c=>c.Song.isTraditional||c.Song.EstablishedAsOf(date.year)),
     selections=selected.Select(c=>new {songId=c.Song.songId,family=Family(c.Song),origin=c.Song.originKind.ToString(),context=Context(c.Song),category=RepertoireProvenance.Category(c.Song,date.year)}),attempts});
   }
   foreach(var fitName in new[]{"packageAB","packageABResolved","v1Frozen","v1Resolved","v2Frozen","v2Resolved"})Observe(fitName,candidates.ToArray(),fitName);
   for(int axis=0;axis<4;axis++)Observe("centerAxis"+axis,candidates.ToArray(),"centerAxis"+axis);
   foreach(string fit in new[]{"packageAB","packageABResolved"}) {
    Observe(fit+"NoAlbum",candidates.Where(c=>!c.Song.repertoireAdmissionRoutes.Contains("albumRepertoire")).ToArray(),fit);
    Observe(fit+"RecentAlbum12",candidates.Where(c=>!c.Song.repertoireAdmissionRoutes.Contains("albumRepertoire")||recent.Contains(c.Song.songId)).ToArray(),fit);
    Observe(fit+"ActRecentAlbum12",candidates.Where(c=>!c.Song.repertoireAdmissionRoutes.Contains("albumRepertoire")||actRecent.Contains(c.Song.songId)).ToArray(),fit);
   }
   CompositionShapeVariation.ActiveVersion=0;
   var placeholders=Placeholders();
   var extraCandidates=new Dictionary<string,Candidate>();
   var accessible=new[]{525,700,1050}.SelectMany(size=>LiveRepertoire.PoolWithAdditional(artist,date.year,date.month,placeholders.Take(size-350))).Select(s=>s.songId).ToHashSet();
   foreach(var song in placeholders.Where(s=>accessible.Contains(s.songId))) {
    var proposal=PolarCoverResolver.Propose(song,null,oldAct,artist.primaryGenre,date.year,week,$"live:{artist.artistId}:{date.year}:{song.songId}",taste,PolarSongTable.Current);
    var c=new Candidate {Song=song,Proposal=proposal};
    c.RepairFits["packageAB"]=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,act,shrunk,week,table);
    c.RepairFits["packageABResolved"]=PolarCoverResolver.Propose(song,null,act,artist.primaryGenre,date.year,week,$"live:{artist.artistId}:{date.year}:{song.songId}",shrunk,table).fit;
    c.ReferenceFits["packageAB"]=PolarMaterialFit.Compute(proposal.referenceProfile,act,shrunk,week,table);
    c.ReferenceFits["packageABResolved"]=c.ReferenceFits["packageAB"];
    extraCandidates.Add(song.songId,c);
   }
   foreach(int size in new[]{525,700,1050}) {
    var ids=LiveRepertoire.PoolWithAdditional(artist,date.year,date.month,placeholders.Take(size-350)).Select(s=>s.songId).ToHashSet();
    // An introductory-four displacement can introduce a clone absent from the full-dose pool.
    var missing=ids.Where(id=>id.StartsWith("directive3-placeholder-")&&!extraCandidates.ContainsKey(id)).ToArray();
    if(missing.Length>0)throw new InvalidOperationException("D2 introductory access displacement requires explicit additional evaluation");
    var pool=candidates.Concat(extraCandidates.Values).Where(c=>ids.Contains(c.Song.songId)).ToArray();
    foreach(string fit in new[]{"packageAB","packageABResolved"})Observe("dose"+size+fit,pool,fit);
   }
   output.WriteLine(JsonSerializer.Serialize(new {artistId=artist.artistId,genre=artist.primaryGenre.ToString(),unsigned,date=date.ToShortString(),albumCount=candidates.Count(c=>c.Song.repertoireAdmissionRoutes.Contains("albumRepertoire")),albumRecency="RecentAlbum12=shared composition year proxy; ActRecentAlbum12=act's last 12 masters by full date then master ID; candidate pool filter before bucket ranking",actRecentMasterSongs=actRecent.Count,variants}));
   output.Flush();fits.Flush();
  } finally {CompositionShapeVariation.ActiveVersion=initial;}
 }
}
