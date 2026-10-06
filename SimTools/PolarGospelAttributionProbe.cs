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
using Godot;

// Fixed-world diagnostic. Counterfactuals never change a composition, table or live selector.
public partial class PolarGospelAttributionProbe : Node {
 private sealed class Candidate {
  public SongComposition Song;
  public MaterialFit Fit, ReferenceFit, LegacyTaxonomyFit, CenterFixedProfileFit, CenterResolvedFit;
  public PolarArrangementProposal Proposal;
  public string Context, Evidence;
 }
 private static string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
 private static string Csv(string v)=>"\""+(v??"").Replace("\"","\"\"")+"\"";
 private static string Hash(string v)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v)));
 private static bool GospelBook(SongComposition s)=>s.repertoireSeedFamily=="Gospel Standard";
 private static string Context(SongComposition s) {
  var tags=(s.contentTagIds??Array.Empty<string>()).Concat(s.demoTaxonomy?.tags?.content??Array.Empty<string>()).Select(t=>t.ToLowerInvariant()).ToArray();
  bool sacred=tags.Any(t=>t is "sacred" or "religious" or "christian" or "worship");
  bool secular=tags.Any(t=>t=="secular");
  return sacred&&secular?"mixed":sacred?"sacred":secular?"secular":"unknown";
 }
 public override void _Ready()=>CallDeferred(nameof(Run));
 public void Run() {
  try {
   ulong seed=SimulationSeedBootstrap.RequestedSeed??throw new InvalidOperationException("Explicit development seed required");
   if(seed is not 1001 and not 1002 || LiveRepertoire.AuditPhase!=5 || !PolarSongBehavior.UsePolarFitSelection)throw new InvalidOperationException("Development seeds and current selector required");
   string run=null,snapshotPath=null;double maxSeconds=120;
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
   var clock=Stopwatch.StartNew();var date=TimeManager.Instance.CurrentDate;int year=date.year,week=ChartManager.Instance.GetCurrentChartWeek()+1;
   string before=Hash(JsonSerializer.Serialize(WorldStateService.Capture(),SaveGameService.TestJsonOptions));
   var table=PolarSongTable.Current;var taste=PolarSongBehavior.CaptureTaste().GetValueOrDefault(Genre.Gospel);
   var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet(StringComparer.Ordinal);
   var actors=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster)).DistinctBy(a=>a.artistId).Where(a=>a.primaryGenre==Genre.Gospel).OrderBy(a=>a.artistId,StringComparer.Ordinal).ToArray();
   var nativeRows=PolarRepertoireTable.Current.Assignments["Gospel Standard"];
   var center=Enumerable.Range(0,SongProfile.IdentityCount).Select(i=>nativeRows.Sum(r=>r.Weight*table.Row(Enum.Parse<SongArchetype>(r.Archetype)).Axes[SongProfile.DemandCount+i])/nativeRows.Sum(r=>r.Weight)).ToArray();
   using var output=new StreamWriter(prefix+"-gospel-candidates.csv");
   using var selection=new StreamWriter(prefix+"-gospel-selections.csv");
   using var actorOutput=new StreamWriter(prefix+"-gospel-actors.csv");
   output.WriteLine("seed,date,artistId,unsigned,songId,family,originKind,primaryGenre,category,subjectContext,contextEvidence,contentTags,admissionRoutes,referenceArchetype,resolvedArchetype,capability,identity,moment,score,referenceScore,legacyTaxonomyScore,centerFixedProfileScore,centerResolvedScore,referenceDistance,refused,actToughness,actSophistication,actSincerity,actMaturity,realizedToughness,realizedSophistication,realizedSincerity,realizedMaturity");
   selection.WriteLine("seed,date,artistId,unsigned,variant,slot,songId,category,family,originKind,subjectContext");
   actorOutput.WriteLine("seed,date,artistId,unsigned,knownPerformers,vocalist,artistType,members,writingAbility,cohort,candidateCount,poolHash,requestedCovers,originals,filledCovers,requestedSlots,baselineMatch");
   var summaries=new List<object>();long candidateCount=0;
   var variants=new[]{"baseline","withoutIdentityScore","withoutCapabilityScore","neutralMomentScore","referenceReading","legacyGospelTaxonomy","nativeCenterFixedProfile","nativeCenterResolved","gospelBookEligibilityProxy","explicitSacredOnly"};
   foreach(var artist in actors) {
    if(clock.Elapsed.TotalSeconds>maxSeconds)throw new InvalidOperationException("Probe budget exhausted; partial outputs are not acceptance evidence");
    var prior=artist.repertoireState;
    try {
     var label=ChartManager.Instance.GetLabelById(artist.labelId);var act=PolarActProfileDeriver.Derive(artist,label,null,table);
     var centered=PolarActProfileDeriver.Derive(artist,label,null,table);var genrePrior=table.Prior(Genre.Gospel);
     for(int i=0;i<center.Length;i++)centered.axes[SongProfile.DemandCount+i]=Math.Clamp(act.axes[SongProfile.DemandCount+i]+center[i]-genrePrior.Identity[i],0,1);
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
       Context=Context(song),Evidence=GospelBook(song)?"namedGospelSongbookProxy":song.primaryGenre==Genre.Gospel?"GospelSceneOnly":"noSubjectEvidence"};
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
     foreach(string variant in variants) {
      MaterialFit Fit(Candidate c)=>variant switch {"referenceReading"=>c.ReferenceFit,"legacyGospelTaxonomy"=>c.LegacyTaxonomyFit,"nativeCenterFixedProfile"=>c.CenterFixedProfileFit,"nativeCenterResolved"=>c.CenterResolvedFit,_=>c.Fit};
      float Score(Candidate c)=>variant=="withoutIdentityScore"?c.Fit.Capability*table.N("selectionCapability")+c.Fit.Moment*table.N("selectionMoment"):
       variant=="withoutCapabilityScore"?c.Fit.Identity*table.N("selectionIdentity")+c.Fit.Moment*table.N("selectionMoment"):
       variant=="neutralMomentScore"?c.Fit.Capability*table.N("selectionCapability")+c.Fit.Identity*table.N("selectionIdentity")+table.N("neutralMoment")*table.N("selectionMoment"):PolarSongBehavior.SelectionScore(Fit(c));
      var eligible=candidates.Where(c=>variant!="gospelBookEligibilityProxy"||GospelBook(c.Song)).Where(c=>variant!="explicitSacredOnly"||c.Context=="sacred").ToArray();
      var selected=SelectCached(eligible,artist,year,requested,empty,Score,Fit).ToArray();
      if(variant=="baseline"&&!selected.Select(c=>c.Song.songId).SequenceEqual(actual))throw new InvalidOperationException("Cached baseline selector differs from production");
      float best=eligible.Length==0?0:eligible.Max(Score),window=PolarRepertoireTable.Current.N("suitabilityWindow");
      var gospel=eligible.Where(c=>GospelBook(c.Song)).ToArray();
      var winner=eligible.OrderByDescending(Score).FirstOrDefault();var gospelWinner=gospel.OrderByDescending(Score).FirstOrDefault();
      actorVariants.Add(new {variant,eligible=eligible.Length,gospelCandidates=gospel.Length,gospelWithinTopWindow=gospel.Count(c=>Score(c)>best-window),bestScore=best,bestGospelScore=gospelWinner==null?(float?)null:Score(gospelWinner),bestSongId=winner?.Song.songId,bestGospelSongId=gospelWinner?.Song.songId,requestedCovers=requested,filledCovers=selected.Length,originals,inheritedSlots=selected.Count(c=>c.Song.isTraditional||c.Song.EstablishedAsOf(year)),gospelBookSlots=selected.Count(c=>GospelBook(c.Song))});
      for(int i=0;i<selected.Length;i++){var s=selected[i].Song;selection.WriteLine(string.Join(",",seed,date.ToShortString(),artist.artistId,unsigned.Contains(artist.artistId),variant,i,s.songId,RepertoireProvenance.Category(s,year),Csv(s.repertoireSeedFamily??s.originKind.ToString()),s.originKind,Context(s)));}
     }
     actorOutput.WriteLine(string.Join(",",seed,date.ToShortString(),artist.artistId,unsigned.Contains(artist.artistId),act.hasKnownPerformers,act.hasVocalist,artist.type,artist.members?.Count(m=>m.isActive)??0,F(artist.songwritingAbility),LiveRepertoire.Cohort(artist,year),pool.Length,poolHash,requested,originals,actual.Length,requested+originals,true));
     summaries.Add(new {artistId=artist.artistId,unsigned=unsigned.Contains(artist.artistId),poolHash,candidates=pool.Length,variants=actorVariants});candidateCount+=pool.Length;
    } finally {artist.repertoireState=prior;}
    output.Flush();selection.Flush();actorOutput.Flush();
   }
   string after=Hash(JsonSerializer.Serialize(WorldStateService.Capture(),SaveGameService.TestJsonOptions));
   if(before!=after)throw new InvalidOperationException("Attribution probe mutated the world");
   var summary=new {schemaVersion=1,run,seed,date=date.ToShortString(),snapshotPath,actors=actors.Length,candidateInstances=candidateCount,worldUnchanged=true,baselineSelectorMatched=true,worldHash=before,seconds=clock.Elapsed.TotalSeconds,
    contextDefinition="Only explicit composition/demo content tags establish sacred/secular subject. Songbook, genre, title, publishing scene and musical archetype are separate proxy evidence; unknown is not secular.",
    variantsDefinition="Paired diagnostics on identical act/pool IDs. Score-component ablations do not change refusal fits. Taxonomy control changes only Gospel-book reference archetype/pace/mood to the prior fallback. Native-center controls derive their counterfactual center from weighted authored Gospel rows, retaining performer offsets. Eligibility proxies are sensitivity masks, not approved context metadata or repertoire repairs.",
    originalIdentityCenter=table.Prior(Genre.Gospel).Identity,nativeTaxonomyCenter=center,marketTaste=taste,cohortDefinition="Current cohort function exposes only inheritedRepertoire for Gospel; member count/type are structural proxies, not quartet/choir/songwriter-led identities.",summaries};
   File.WriteAllText(prefix+"-gospel-summary.json",JsonSerializer.Serialize(summary,new JsonSerializerOptions {WriteIndented=true,IncludeFields=true})+"\n");
   GD.Print($"POLAR_GOSPEL_ATTRIBUTION_PASS run={run} seed={seed} actors={actors.Length} candidateInstances={candidateCount} seconds={clock.Elapsed.TotalSeconds:F2} worldUnchanged=True baselineMatch=True");GetTree().Quit(0);
  } catch(Exception ex){GD.PrintErr(ex);GetTree().Quit(1);}
 }
 private static IEnumerable<Candidate> SelectCached(Candidate[] candidates,SimulatedArtist artist,int year,int requested,bool empty,Func<Candidate,float> score,Func<Candidate,MaterialFit> fit) {
  if(candidates.Length==0)yield break;
  float best=candidates.Max(score),window=PolarRepertoireTable.Current.N("suitabilityWindow");int chosen=0,filled=0;bool hasBallad=false;var remaining=candidates.ToList();
  while(remaining.Count>0&&filled<requested) {
   bool Ballad(Candidate c)=>c.Song.demoTaxonomy?.pace=="slow";
   var next=remaining.OrderBy(c=>(int)((best-score(c))/window)).ThenByDescending(c=>LiveRepertoire.Preference(c.Song,artist,year,TimeManager.Instance.CurrentDate.month)+(chosen>0&&!hasBallad&&Ballad(c)?PolarRepertoireTable.Current.N("balladBalance"):0)).ThenBy(c=>RepertoireTaxonomy.Hash(artist.artistId+"|collision|"+c.Song.songId)).First();
   chosen++;hasBallad|=Ballad(next);remaining.Remove(next);
   if(PolarSongBehavior.Refuses(fit(next),artist,empty))continue;
   filled++;yield return next;
  }
 }
 private static ulong CensusSeed(string key){ulong h=14695981039346656037UL;foreach(char c in key){h^=c;h=unchecked(h*1099511628211UL);}return h;}
}
