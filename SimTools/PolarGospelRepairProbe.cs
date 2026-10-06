using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

// Fixed-world diagnostic. Counterfactuals never change a composition, table or live selector.
public partial class PolarGospelRepairProbe : Node {
 private sealed class Candidate {
  public SongComposition Song;
  public MaterialFit Fit, ReferenceFit, LegacyTaxonomyFit, CenterFixedProfileFit, CenterResolvedFit;
  public PolarArrangementProposal Proposal, RepairProposal, ShapeProposal;
  public SongProfile ShapeFrozenProfile;
  public Dictionary<string, MaterialFit> RepairFits = new();
  public Dictionary<string, MaterialFit> ReferenceFits = new();
  public string Context, Evidence;
 }
 private static string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
 private static string Csv(string v)=>"\""+(v??"").Replace("\"","\"\"")+"\"";
 private static string Hash(string v)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v)));
 private static bool GospelBook(SongComposition s)=>s.repertoireSeedFamily=="Gospel Standard";
 private static string Context(SongComposition s) {
  return s.contentContext.ToString().ToLowerInvariant();
 }
 private static string Family(SongComposition s)=>GospelBook(s)?"Gospel Standard":s.originKind==SongOriginKind.ArtistOriginal?
  "ArtistOriginal:"+string.Join("+",s.repertoireAdmissionRoutes.OrderBy(x=>x,StringComparer.Ordinal)):s.repertoireSeedFamily=="R&B Catalog"?"R&B Catalog":"other";
 public override void _Ready()=>CallDeferred(nameof(Run));
 public void Run() {
  try {
   ulong seed=SimulationSeedBootstrap.RequestedSeed??throw new InvalidOperationException("Explicit development seed required");
   if(seed is not 1001 and not 1002 || LiveRepertoire.AuditPhase!=5 || !PolarSongBehavior.UsePolarFitSelection)throw new InvalidOperationException("Development seeds and current selector required");
   string run=null,snapshotPath=null;double maxSeconds=120;bool followup=OS.GetCmdlineUserArgs().Contains("--polar-repair-followup");
   foreach(string arg in OS.GetCmdlineUserArgs()) {
    if(arg.StartsWith("--run="))run=arg[6..];
    if(arg.StartsWith("--snapshot="))snapshotPath=arg[11..];
    if(arg.StartsWith("--max-seconds="))maxSeconds=double.Parse(arg[14..],CultureInfo.InvariantCulture);
   }
   if(string.IsNullOrWhiteSpace(run)||run.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-')||!double.IsFinite(maxSeconds)||maxSeconds<=0)throw new ArgumentException("Invalid run or budget");
   string root=ProjectSettings.GlobalizePath("res://SimLogs"),prefix=Path.Combine(root,run);
   if(File.Exists(prefix+"-gospel-candidates.csv")||File.Exists(prefix+"-gospel-summary.json"))throw new InvalidOperationException("Existing probe artifacts");
   if(snapshotPath!=null) {
    using var input=File.OpenRead(snapshotPath);using var gzip=new GZipStream(input,CompressionMode.Decompress);
    var snapshot=JsonSerializer.Deserialize<ChartAuditRunner.PolarResearchSnapshot>(gzip,SaveGameService.TestJsonOptions);
    if(snapshot?.Version!=1||snapshot.Save?.World==null||snapshot.Save.WorldSeed!=seed||snapshot.RepertoirePhase!=5||!snapshot.FitSelection)throw new InvalidOperationException("Snapshot settings mismatch");
    RepertoireProvenance.Reset();var s=snapshot.Save;
    WorldStateService.Apply(s.World,new GameDate(s.Year,s.Month,s.Day),s.WorldSeed);
    if(s.Player!=null&&!PlayerDesk.Instance.RestoreState(s.Player,out _))throw new InvalidOperationException("Snapshot player restore failed");
   }
   if(followup) CompositionShapeVariation.ActiveVersion=0;
   var clock=Stopwatch.StartNew();var date=TimeManager.Instance.CurrentDate;int year=date.year,week=ChartManager.Instance.GetCurrentChartWeek()+1;
   string worldJson=JsonSerializer.Serialize(WorldStateService.Capture(),SaveGameService.TestJsonOptions);
   string before=Hash(worldJson);
   var projected=JsonNode.Parse(worldJson);
   var savedComposition=projected["Composition"].AsObject();
   savedComposition.Remove("ShapeVariationVersion");savedComposition.Remove("ShapeVariationWorldSeed");
   foreach(var song in savedComposition["Songs"].AsObject().Select(kv=>kv.Value.AsObject()))
    foreach(string key in new[]{"contentContext","contextAuthorshipEvidence","shapeVariationSchemaVersion","shapeVariation"})song.Remove(key);
   string projectedWorldHash=Hash(projected.ToJsonString(SaveGameService.TestJsonOptions));
   if(OS.GetCmdlineUserArgs().Contains("--capture-only")) {
    File.WriteAllText(prefix+"-world-equivalence.json",JsonSerializer.Serialize(new {seed,date=date.ToShortString(),worldHash=before,projectedWorldHash,removedFields=new[]{"ShapeVariationVersion","ShapeVariationWorldSeed","contentContext","contextAuthorshipEvidence","shapeVariationSchemaVersion","shapeVariation"}}));
    GD.Print($"POLAR_GOSPEL_REPAIR_PASS run={run} captureOnly=True");GetTree().Quit(0);return;
   }
   var table=PolarSongTable.Current;var repair=PolarSongTable.RepairTable;
   if(!PolarSongTable.AuditLegacyRepair)throw new InvalidOperationException("Legacy fixed-world initialization required");
   var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet(StringComparer.Ordinal);
   var actors=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId).OrderBy(a=>a.artistId,StringComparer.Ordinal).ToArray();
   // All Gospel acts; a bounded deterministic panel in every other populated genre.
   actors=actors.GroupBy(a=>(a.primaryGenre, unsigned.Contains(a.artistId))).SelectMany(g=>g.Key.primaryGenre==Genre.Gospel?g:g.OrderBy(a=>RepertoireTaxonomy.Hash(a.artistId+"|repair-panel")).Take(4)).OrderBy(a=>a.artistId,StringComparer.Ordinal).ToArray();
   if(OS.GetCmdlineUserArgs().Contains("--rock-only"))actors=actors.Where(a=>SongMaterialSelectionService.IsRockSongbookContext(a.primaryGenre)).ToArray();
   var population=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId).GroupBy(a=>a.primaryGenre).ToDictionary(g=>g.Key.ToString(),g=>g.Count());
   var nativeRows=PolarRepertoireTable.Current.Assignments["Gospel Standard"];
   var center=Enumerable.Range(0,SongProfile.IdentityCount).Select(i=>nativeRows.Sum(r=>r.Weight*table.Row(Enum.Parse<SongArchetype>(r.Archetype)).Axes[SongProfile.DemandCount+i])/nativeRows.Sum(r=>r.Weight)).ToArray();
   using var output=new StreamWriter(prefix+"-gospel-candidates.csv");
   using var selection=new StreamWriter(prefix+"-gospel-selections.csv");
   using var fitsOutput=new StreamWriter(prefix+"-repair-fits.csv");
   fitsOutput.WriteLine("artistId,genre,songId,variant,capability,identity,moment,score,shape,archetype,referenceArchetype,instrumental");
   using var actorOutput=new StreamWriter(prefix+"-gospel-actors.csv");
   output.WriteLine("seed,date,artistId,unsigned,songId,family,originKind,primaryGenre,category,subjectContext,contextEvidence,contentTags,admissionRoutes,referenceArchetype,resolvedArchetype,capability,identity,moment,score,referenceScore,legacyTaxonomyScore,centerFixedProfileScore,centerResolvedScore,referenceDistance,refused,actToughness,actSophistication,actSincerity,actMaturity,realizedToughness,realizedSophistication,realizedSincerity,realizedMaturity");
   selection.WriteLine("seed,date,artistId,unsigned,variant,slot,songId,category,family,originKind,subjectContext");
   actorOutput.WriteLine("seed,date,artistId,unsigned,knownPerformers,vocalist,artistType,members,writingAbility,cohort,candidateCount,poolHash,requestedCovers,originals,filledCovers,requestedSlots,baselineMatch,genre,cohortLabel");
   var summaries=new List<object>();long candidateCount=0;
   using var bucketOutput=new StreamWriter(prefix+"-within-bucket.jsonl");
   if(followup)WriteCompositionMeasures(prefix,repair);
   bool directive3=OS.GetCmdlineUserArgs().Contains("--polar-directive3-probe");
   using var directive3Output=directive3?new StreamWriter(prefix+"-directive3-actors.jsonl"):null;
   using var directive3Fits=directive3?new StreamWriter(prefix+"-directive3-fits.csv"):null;
   directive3Fits?.WriteLine("artistId,genre,songId,archetype,variant,capability,identity,moment");
   if(directive3)WriteDirective3Shapes(prefix,repair);
   var variants=new[]{"baseline","withoutIdentityScore","withoutCapabilityScore","neutralMomentScore","referenceReading","legacyGospelTaxonomy","nativeCenterFixedProfile","nativeCenterResolved","gospelBookEligibilityProxy","explicitSacredOnly","packageA","packageB","packageAB","packageABResolved","packageABHalfK","packageABDoubleK"};
   foreach(var artist in actors) {
    if(clock.Elapsed.TotalSeconds>maxSeconds)throw new InvalidOperationException("Probe budget exhausted; partial outputs are not acceptance evidence");
    var prior=artist.repertoireState;
    try {
     var taste=PolarSongBehavior.CaptureTaste().GetValueOrDefault(artist.primaryGenre);
     var label=ChartManager.Instance.GetLabelById(artist.labelId);var act=PolarActProfileDeriver.Derive(artist,label,null,table);
     var centered=PolarActProfileDeriver.Derive(artist,label,null,table);var genrePrior=table.Prior(Genre.Gospel);
     for(int i=0;i<center.Length;i++)centered.axes[SongProfile.DemandCount+i]=Math.Clamp(act.axes[SongProfile.DemandCount+i]+center[i]-genrePrior.Identity[i],0,1);
     var repairedAct=PolarActProfileDeriver.Derive(artist,label,null,repair);
     var shrunk=PolarSongBehavior.ShrinkTaste(taste,artist.primaryGenre,repair);
     var pool=LiveRepertoire.Pool(artist,year,date.month).Where(s=>s!=null&&s.originYear<=year).DistinctBy(s=>s.songId).ToArray();
     string poolHash=Hash(string.Join("\n",pool.Select(s=>s.songId).OrderBy(s=>s,StringComparer.Ordinal)));
     bool empty=PlayerDesk.Instance.RepertoireFor(artist.artistId).Count==0&&!PlayerDesk.Instance.SongsFor(artist.artistId).Any();
     var candidates=new List<Candidate>();
     foreach(var song in pool) {
      var proposal=PolarSongBehavior.LiveProposal(song,artist,year);var reference=PolarSongBehavior.Reference(song,year);
      var legacyFit=proposal.fit;
      if(GospelBook(song)) {
       // Isolate replacing the four authored Gospel forms with the former fallback.
       var taxonomy=(reference?.taxonomy??song.demoTaxonomy).Copy();taxonomy.archetype=Enum.Parse<SongArchetype>(genrePrior.Fallback);
       taxonomy.pace=table.Row(taxonomy.archetype).Pace;taxonomy.mood=table.Row(taxonomy.archetype).Mood;
       var altered=new SongMasterMetadata {masterId=reference?.masterId,songId=song.songId,recordingYear=reference?.recordingYear??year,taxonomy=taxonomy};
       legacyFit=PolarCoverResolver.Propose(song,altered,act,Genre.Gospel,year,week,$"live:{artist.artistId}:{year}:{song.songId}",taste,table).fit;
      }
      var c=new Candidate {Song=song,Proposal=proposal,Fit=proposal.fit,ReferenceFit=PolarSongBehavior.Fit(song,artist,Genre.Gospel,year),LegacyTaxonomyFit=legacyFit,
       CenterFixedProfileFit=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,centered,taste,week,table),
       CenterResolvedFit=PolarCoverResolver.Propose(song,reference,centered,Genre.Gospel,year,week,$"live:{artist.artistId}:{year}:{song.songId}",taste,table).fit,
       Context=Context(song),Evidence=song.contextAuthorshipEvidence??"noAuthorshipEvidence"};
      c.RepairProposal=PolarCoverResolver.Propose(song,reference,repairedAct,artist.primaryGenre,year,week,$"live:{artist.artistId}:{year}:{song.songId}",shrunk,repair);
      c.RepairFits["packageA"]=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,repairedAct,taste,week,repair);
      c.RepairFits["packageB"]=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,act,shrunk,week,repair);
      c.RepairFits["packageAB"]=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,repairedAct,shrunk,week,repair);
      c.RepairFits["packageABResolved"]=c.RepairProposal.fit;
      c.ReferenceFits["baseline"]=PolarMaterialFit.Compute(proposal.referenceProfile,act,taste,week,table);
      c.ReferenceFits["packageA"]=PolarMaterialFit.Compute(proposal.referenceProfile,repairedAct,taste,week,repair);
      c.ReferenceFits["packageB"]=PolarMaterialFit.Compute(proposal.referenceProfile,act,shrunk,week,repair);
      c.ReferenceFits["packageAB"]=PolarMaterialFit.Compute(proposal.referenceProfile,repairedAct,shrunk,week,repair);
      c.ReferenceFits["packageABResolved"]=c.ReferenceFits["packageAB"];
      if(followup) {
       try {
        CompositionShapeVariation.ActiveVersion=1;
        c.ShapeProposal=PolarCoverResolver.Propose(song,reference,repairedAct,artist.primaryGenre,year,week,$"live:{artist.artistId}:{year}:{song.songId}",shrunk,repair);
        c.RepairFits["packageABHResolved"]=c.ShapeProposal.fit;
        c.ShapeFrozenProfile=SongProfileDeriver.Derive(song,proposal.taxonomy,repair);
        c.RepairFits["packageABH"]=PolarMaterialFit.Evaluate(SongProfileDeriver.Derive(song,reference?.taxonomy??song.demoTaxonomy,repair),c.ShapeFrozenProfile,repairedAct,shrunk,week,repair);
        c.ReferenceFits["packageABH"]=PolarMaterialFit.Compute(SongProfileDeriver.Derive(song,reference?.taxonomy??song.demoTaxonomy,repair),repairedAct,shrunk,week,repair);
        c.ReferenceFits["packageABHResolved"]=c.ReferenceFits["packageABH"];
       } finally {CompositionShapeVariation.ActiveVersion=0;}
      }
      foreach(var (name,k) in new[]{("packageABHalfK",2f),("packageABDoubleK",8f)}) c.RepairFits[name]=PolarMaterialFit.Evaluate(proposal.referenceProfile,proposal.realizedProfile,repairedAct,PolarSongBehavior.ShrinkTaste(taste,artist.primaryGenre,repair,k),week,repair);
      foreach(var v in c.RepairFits.Append(new KeyValuePair<string,MaterialFit>("baseline",c.Fit))) {
       var shape=v.Key=="packageABH"?c.ShapeFrozenProfile:v.Key=="packageABHResolved"?c.ShapeProposal.realizedProfile:v.Key=="packageABResolved"?c.RepairProposal.realizedProfile:proposal.realizedProfile;
       fitsOutput.WriteLine(string.Join(",",artist.artistId,artist.primaryGenre,song.songId,v.Key,F(v.Value.Capability),F(v.Value.Identity),F(v.Value.Moment),F(PolarSongBehavior.SelectionScore(v.Value)),Csv(string.Join(";",shape.axes.Select(x=>F(x)))),shape.archetype,proposal.referenceProfile.archetype,c.RepairProposal.taxonomy.vocalPresence==SongVocalPresence.Instrumental));
      }
      candidates.Add(c);
      output.WriteLine(string.Join(",",seed,date.ToShortString(),artist.artistId,unsigned.Contains(artist.artistId),song.songId,Csv(song.repertoireSeedFamily??song.originKind.ToString()),song.originKind,song.primaryGenre,RepertoireProvenance.Category(song,year),c.Context,c.Evidence,Csv(string.Join(";",song.contentTagIds.Concat(song.demoTaxonomy?.tags?.content??Array.Empty<string>()))),Csv(string.Join(";",song.repertoireAdmissionRoutes)),proposal.referenceProfile.archetype,proposal.realizedProfile.archetype,
       F(c.Fit.Capability),F(c.Fit.Identity),F(c.Fit.Moment),F(PolarSongBehavior.SelectionScore(c.Fit)),F(PolarSongBehavior.SelectionScore(c.ReferenceFit)),F(PolarSongBehavior.SelectionScore(c.LegacyTaxonomyFit)),F(PolarSongBehavior.SelectionScore(c.CenterFixedProfileFit)),F(PolarSongBehavior.SelectionScore(c.CenterResolvedFit)),F(c.Fit.ReferenceIdentityDistance),PolarSongBehavior.Refuses(c.Fit,artist,empty),
       F(act[SongAxis.Toughness]),F(act[SongAxis.Sophistication]),F(act[SongAxis.Sincerity]),F(act[SongAxis.Maturity]),F(proposal.realizedProfile[SongAxis.Toughness]),F(proposal.realizedProfile[SongAxis.Sophistication]),F(proposal.realizedProfile[SongAxis.Sincerity]),F(proposal.realizedProfile[SongAxis.Maturity])));
     }
     using var random=new Godot.RandomNumberGenerator {Seed=CensusSeed($"{seed}:{year}:{date.month}:{artist.artistId}")};
     var prospect=new PlayerDesk.Prospect {Artist=artist};int requested=0;
     PlayerDesk.Instance.BuildLiveSet(prospect,artist,year,0,random,(want,songs)=>{requested=want;if(!songs.Select(s=>s.songId).ToHashSet().SetEquals(pool.Select(s=>s.songId)))throw new InvalidOperationException("Live set pool differs from fixed diagnostic pool");});
     int originals=prospect.LiveSet.Count(s=>s.IsOriginal);var actual=prospect.LiveSet.Where(s=>!s.IsOriginal).Select(s=>s.SongId).ToArray();
     var actorVariants=new List<object>();
     var actorNames=artist.primaryGenre==Genre.Gospel?variants:new[]{"baseline","packageA","packageB","packageAB","packageABResolved"};
     if(followup)actorNames=actorNames.Concat(new[]{"packageABH","packageABHResolved","packageABSacredFixture","packageABNeutralPreference","packageABFamilyPreference","packageABResolvedNeutralPreference","packageABResolvedFamilyPreference"}).ToArray();
     foreach(string variant in actorNames) {
      string fitVariant=variant=="packageABSacredFixture"?"packageABResolved":variant.Replace("NeutralPreference","").Replace("FamilyPreference","");
      MaterialFit Fit(Candidate c)=>c.RepairFits.TryGetValue(fitVariant,out var repairFit)?repairFit:variant switch {"referenceReading"=>c.ReferenceFit,"legacyGospelTaxonomy"=>c.LegacyTaxonomyFit,"nativeCenterFixedProfile"=>c.CenterFixedProfileFit,"nativeCenterResolved"=>c.CenterResolvedFit,_=>c.Fit};
      float Score(Candidate c)=>variant=="withoutIdentityScore"?c.Fit.Capability*table.N("selectionCapability")+c.Fit.Moment*table.N("selectionMoment"):
       variant=="withoutCapabilityScore"?c.Fit.Identity*table.N("selectionIdentity")+c.Fit.Moment*table.N("selectionMoment"):
       variant=="neutralMomentScore"?c.Fit.Capability*table.N("selectionCapability")+c.Fit.Identity*table.N("selectionIdentity")+table.N("neutralMoment")*table.N("selectionMoment"):PolarSongBehavior.SelectionScore(Fit(c));
      var eligible=candidates.Where(c=>variant!="gospelBookEligibilityProxy"||GospelBook(c.Song)).Where(c=>variant is not "explicitSacredOnly" and not "packageABSacredFixture"||c.Context=="sacred").ToArray();
      Candidate[] selected;
      var attempts=new List<object>();
      if(SongMaterialSelectionService.IsRockSongbookContext(artist.primaryGenre)) {
       var byId=eligible.ToDictionary(c=>c.Song.songId);
       PolarSongBehavior.AuditLiveFit=(song,a)=>byId[song.songId].ReferenceFits[fitVariant];
       PolarSongBehavior.AuditLiveProposal=(song,a)=>new PolarArrangementProposal {fit=Fit(byId[song.songId])};
       try {selected=SongMaterialSelectionService.SelectLiveCovers(eligible.Select(c=>c.Song),artist,year,requested).Select(s=>byId[s.songId]).ToArray();}
       finally {PolarSongBehavior.AuditLiveFit=null;PolarSongBehavior.AuditLiveProposal=null;}
      } else {
       var familyMeans=eligible.GroupBy(c=>Family(c.Song)).ToDictionary(g=>g.Key,g=>g.Average(c=>LiveRepertoire.Preference(c.Song,artist,year,date.month)));
       float Preference(Candidate c)=>variant.EndsWith("NeutralPreference")?0:variant.EndsWith("FamilyPreference")?familyMeans[Family(c.Song)]:LiveRepertoire.Preference(c.Song,artist,year,date.month);
       selected=SelectCached(eligible,artist,year,requested,empty,Score,Fit,Preference,followup && variant is "packageAB" or "packageABResolved" ? o=>attempts.Add(o):null).ToArray();
      }
      if(variant=="baseline"&&!selected.Select(c=>c.Song.songId).SequenceEqual(actual))throw new InvalidOperationException("Cached baseline selector differs from production");
      float best=eligible.Length==0?0:eligible.Max(Score),window=PolarRepertoireTable.Current.N("suitabilityWindow");
      var gospel=eligible.Where(c=>artist.primaryGenre==Genre.Gospel?GospelBook(c.Song):c.Song.isTraditional||c.Song.EstablishedAsOf(year)).ToArray();
      if(followup && variant is "packageAB" or "packageABResolved") {
       var first=eligible.Where(c=>(int)((best-Score(c))/window)==0).OrderByDescending(c=>LiveRepertoire.Preference(c.Song,artist,year,date.month)).ThenBy(c=>RepertoireTaxonomy.Hash(artist.artistId+"|collision|"+c.Song.songId)).ToArray();
       bucketOutput.WriteLine(JsonSerializer.Serialize(new {seed,date=date.ToShortString(),artistId=artist.artistId,genre=artist.primaryGenre.ToString(),variant,
        firstBucket=first.Length,families=first.GroupBy(c=>Family(c.Song)).ToDictionary(g=>g.Key,g=>g.Count()),
        candidates=first.Select((c,i)=>new {songId=c.Song.songId,family=Family(c.Song),rank=i+1,preference=LiveRepertoire.Preference(c.Song,artist,year,date.month),hook=c.Song.commercialHook,familiarity=c.Song.GetFamiliarityForYear(year)}),
        selected=selected.Select(c=>new {songId=c.Song.songId,family=Family(c.Song),rank=Array.IndexOf(first,c)+1}),attempts,originals,requested}));
      }
      var winner=eligible.OrderByDescending(Score).FirstOrDefault();var gospelWinner=gospel.OrderByDescending(Score).FirstOrDefault();
      var wf=winner==null?default:Fit(winner);var gf=gospelWinner==null?default:Fit(gospelWinner);
      actorVariants.Add(new {gapCapability=winner==null||gospelWinner==null?(float?)null:.35f*(wf.Capability-gf.Capability),gapIdentity=winner==null||gospelWinner==null?(float?)null:.45f*(wf.Identity-gf.Identity),gapMoment=winner==null||gospelWinner==null?(float?)null:.20f*(wf.Moment-gf.Moment),variant,eligible=eligible.Length,gospelCandidates=gospel.Length,gospelWithinTopWindow=gospel.Count(c=>Score(c)>best-window),bestScore=best,bestGospelScore=gospelWinner==null?(float?)null:Score(gospelWinner),bestSongId=winner?.Song.songId,bestGospelSongId=gospelWinner?.Song.songId,requestedCovers=requested,filledCovers=selected.Length,originals,inheritedSlots=selected.Count(c=>c.Song.isTraditional||c.Song.EstablishedAsOf(year)),gospelBookSlots=selected.Count(c=>GospelBook(c.Song))});
      for(int i=0;i<selected.Length;i++){var s=selected[i].Song;selection.WriteLine(string.Join(",",seed,date.ToShortString(),artist.artistId,unsigned.Contains(artist.artistId),variant,i,s.songId,RepertoireProvenance.Category(s,year),Csv(s.repertoireSeedFamily??s.originKind.ToString()),s.originKind,Context(s)));}
     }
     if(directive3)WriteDirective3Actor(directive3Output,directive3Fits,artist,candidates,requested,originals,empty,act,repairedAct,taste,shrunk,repair,date,week,unsigned.Contains(artist.artistId));
     actorOutput.WriteLine(string.Join(",",seed,date.ToShortString(),artist.artistId,unsigned.Contains(artist.artistId),act.hasKnownPerformers,act.hasVocalist,artist.type,artist.members?.Count(m=>m.isActive)??0,F(artist.songwritingAbility),LiveRepertoire.Cohort(artist,year),pool.Length,poolHash,requested,originals,actual.Length,requested+originals,true,artist.primaryGenre,LiveRepertoire.Cohort(artist,year)));
     summaries.Add(new {genre=artist.primaryGenre.ToString(),cohort=LiveRepertoire.Cohort(artist,year),artistId=artist.artistId,unsigned=unsigned.Contains(artist.artistId),poolHash,candidates=pool.Length,variants=actorVariants});candidateCount+=pool.Length;
    } finally {artist.repertoireState=prior;}
    output.Flush();selection.Flush();actorOutput.Flush();fitsOutput.Flush();
   }
   string after=Hash(JsonSerializer.Serialize(WorldStateService.Capture(),SaveGameService.TestJsonOptions));
   if(before!=after)throw new InvalidOperationException("Attribution probe mutated the world");
   var summary=new {schemaVersion=2,population,actorHash=Hash(string.Join("\n",actors.Select(a=>a.artistId))),run,seed,date=date.ToShortString(),snapshotPath,actors=actors.Length,candidateInstances=candidateCount,worldUnchanged=true,baselineSelectorMatched=true,worldHash=before,projectedWorldHash,seconds=clock.Elapsed.TotalSeconds,
    contextDefinition="Persisted composition subject with explicit authorship evidence; retained frames load as unknown. Sacred-only masks on those frames have no observations. Fresh authored Gospel fixtures are diagnosed separately.",
    variantsDefinition="Paired diagnostics on identical act/pool IDs. Score-component ablations do not change refusal fits. Taxonomy control changes only Gospel-book reference archetype/pace/mood to the prior fallback. Native-center controls derive their counterfactual center from weighted authored Gospel rows, retaining performer offsets. Eligibility proxies are sensitivity masks, not approved context metadata or repertoire repairs.",
    originalIdentityCenter=table.Prior(Genre.Gospel).Identity,nativeTaxonomyCenter=center,marketTaste=PolarSongBehavior.CaptureTaste(),cohortDefinition="Current cohort function exposes only inheritedRepertoire for Gospel; member count/type are structural proxies, not quartet/choir/songwriter-led identities.",summaries};
   File.WriteAllText(prefix+"-gospel-summary.json",JsonSerializer.Serialize(summary,new JsonSerializerOptions {WriteIndented=true,IncludeFields=true})+"\n");
   GD.Print($"POLAR_GOSPEL_REPAIR_PASS run={run} seed={seed} actors={actors.Length} candidateInstances={candidateCount} seconds={clock.Elapsed.TotalSeconds:F2} worldUnchanged=True baselineMatch=True");GetTree().Quit(0);
  } catch(Exception ex){GD.PrintErr(ex);GetTree().Quit(1);}
 }
 private static IEnumerable<Candidate> SelectCached(Candidate[] candidates,SimulatedArtist artist,int year,int requested,bool empty,Func<Candidate,float> score,Func<Candidate,MaterialFit> fit,Func<Candidate,float> preference=null,Action<object> trace=null) {
  if(candidates.Length==0)yield break;
  float best=candidates.Max(score),window=PolarRepertoireTable.Current.N("suitabilityWindow");int chosen=0,filled=0;bool hasBallad=false;var remaining=candidates.ToList();
  while(remaining.Count>0&&filled<requested) {
   bool Ballad(Candidate c)=>c.Song.demoTaxonomy?.pace=="slow";
   var next=remaining.OrderBy(c=>(int)((best-score(c))/window)).ThenByDescending(c=>(preference?.Invoke(c)??LiveRepertoire.Preference(c.Song,artist,year,TimeManager.Instance.CurrentDate.month))+(chosen>0&&!hasBallad&&Ballad(c)?PolarRepertoireTable.Current.N("balladBalance"):0)).ThenBy(c=>RepertoireTaxonomy.Hash(artist.artistId+"|collision|"+c.Song.songId)).First();
   bool refused=PolarSongBehavior.Refuses(fit(next),artist,empty);
   if(trace!=null) {
    var first=remaining.Where(c=>(int)((best-score(c))/window)==0).ToArray();
    trace(new {songId=next.Song.songId,family=Family(next.Song),bucket=(int)((best-score(next))/window),refused,
     bookAndOtherAvailable=first.Any(c=>GospelBook(c.Song))&&first.Any(c=>!GospelBook(c.Song)),balladBonusApplied=chosen>0&&!hasBallad&&Ballad(next)});
   }
   chosen++;hasBallad|=Ballad(next);remaining.Remove(next);
   if(refused)continue;
   filled++;yield return next;
  }
 }
 private static void WriteCompositionMeasures(string prefix,PolarSongTable table) {
  using var output=new StreamWriter(prefix+"-composition-shapes.csv");
  output.WriteLine("songId,source,variant,archetype,shape,instrumental,covered,recordings");
  var recorded=PolarSongMetadataService.Masters.Values.GroupBy(m=>m.songId).ToDictionary(g=>g.Key,g=>g.ToArray());
  foreach(var song in CompositionCatalogService.AllSongs.OrderBy(s=>s.songId,StringComparer.Ordinal)) {
   var masters=recorded.GetValueOrDefault(song.songId)??Array.Empty<SongMasterMetadata>();
   string source=song.originKind==SongOriginKind.ArtistOriginal?"original":song.repertoireSeedFamily!=null?"seeded":"scouted-imported-professional";
   foreach(int version in new[]{0,1}) {
    CompositionShapeVariation.ActiveVersion=version;var p=SongProfileDeriver.Derive(song,song.demoTaxonomy,table);
    bool instrumental=song.demoTaxonomy?.vocalPresence==SongVocalPresence.Instrumental || table.Row(p.archetype).Axes.Where((v,i)=>i is 0 or 1 or 4).All(v=>v==0);
    output.WriteLine(string.Join(",",song.songId,source,version==0?"off":"on",p.archetype,Csv(string.Join(";",p.axes.Select(v=>F(v)))),instrumental,masters.Any(m=>m.parentRecordingId!=null),masters.Length));
   }
  }
  CompositionShapeVariation.ActiveVersion=0;
 }
 private static ulong CensusSeed(string key){ulong h=14695981039346656037UL;foreach(char c in key){h^=c;h=unchecked(h*1099511628211UL);}return h;}
}
