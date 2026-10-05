using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Incremental repair A/B on the same January actors and catalogue, without advancing time.</summary>
public partial class PolarRepertoirePhaseProbe : Node {
 public override void _Ready()=>CallDeferred(nameof(Run));
 public void Run() {
  try {
   if(LiveRepertoire.AuditPhase!=0)throw new InvalidOperationException("Start phase probe with --repertoire-audit-phase=0");
   ulong seed=SimulationSeedBootstrap.RequestedSeed??throw new InvalidOperationException("Explicit tuning seed required");
   if(seed is not 1001 and not 1002)throw new InvalidOperationException("Phase probes use tuning seeds only");
   string root=ProjectSettings.GlobalizePath("res://SimLogs"),prefix=$"polar-repertoire-phases-{seed}";
   using var sets=new StreamWriter(Path.Combine(root,prefix+"-sets.csv"));
   using var slots=new StreamWriter(Path.Combine(root,prefix+"-slots.csv"));
   using var inventory=new StreamWriter(Path.Combine(root,prefix+"-inventory.csv"));
   using var ordering=new StreamWriter(Path.Combine(root,prefix+"-ordering.csv"));
   ordering.WriteLine("seed,genre,unsigned,artistId,instrumental,candidates,requested,changedSlots");
   sets.WriteLine("phase,seed,genre,unsigned,artistId,eligible,filledCovers,originals,slots");
   slots.WriteLine("phase,seed,genre,unsigned,artistId,songId,category,archetype");
   inventory.WriteLine("phase,genre,seedFamily,archetype,count");
   var unsigned=ArtistManager.Instance.GetUnsignedArtists().Select(a=>a.artistId).ToHashSet();
   var actors=ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster))
    .DistinctBy(a=>a.artistId).OrderBy(a=>a.artistId,StringComparer.Ordinal).ToArray();
   var actorsBefore=JsonSerializer.Serialize(actors.Select(a=>new {a.artistId,a.primaryGenre,a.songwritingAbility,a.careerState}),SaveGameService.TestJsonOptions);
   for(int phase=0;phase<=5;phase++) {
    if(phase>0)CompositionCatalogService.ApplyRepertoireAuditPhase(phase,1960);
    foreach(var artist in actors) {
     using var random=new RandomNumberGenerator {Seed=CensusKey($"{seed}:1960:1:{artist.artistId}")};
     var prospect=new PlayerDesk.Prospect {Artist=artist};List<SongComposition> pool=null;
     PlayerDesk.Instance.BuildLiveSet(prospect,artist,1960,0,random,(_,p)=>pool=p);
     var eligible=pool.Where(s=>s!=null&&s.originYear<=1960).DistinctBy(s=>s.songId).Count();
     if(phase==2) {
      int wanted=prospect.LiveSet.Count(s=>!s.IsOriginal);
      var refIds=pool.Where(s=>s!=null&&s.originYear<=1960).DistinctBy(s=>s.songId).OrderByDescending(s=>PolarSongBehavior.SelectionScore(PolarSongBehavior.Fit(s,artist,artist.primaryGenre,1960))).ThenByDescending(s=>s.primaryGenre==artist.primaryGenre).ThenBy(s=>s.songId,StringComparer.Ordinal).Take(wanted).Select(s=>s.songId).ToHashSet();
      var resolved=pool.Where(s=>s!=null&&s.originYear<=1960).DistinctBy(s=>s.songId).OrderByDescending(s=>PolarSongBehavior.SelectionScore(PolarSongBehavior.LiveProposal(s,artist,1960).fit)).ThenByDescending(s=>s.primaryGenre==artist.primaryGenre).ThenBy(s=>s.songId,StringComparer.Ordinal).Take(wanted).ToArray();
      ordering.WriteLine(string.Join(",",seed,artist.primaryGenre,unsigned.Contains(artist.artistId),artist.artistId,LiveRepertoire.Instrumental(artist),eligible,wanted,resolved.Count(s=>!refIds.Contains(s.songId))));
     }
     sets.WriteLine(string.Join(",",phase,seed,artist.primaryGenre,unsigned.Contains(artist.artistId),artist.artistId,eligible,prospect.LiveSet.Count(s=>!s.IsOriginal),prospect.LiveSet.Count(s=>s.IsOriginal),prospect.LiveSet.Count));
     foreach(var item in prospect.LiveSet) {
      var s=CompositionCatalogService.GetSong(item.SongId);
      slots.WriteLine(string.Join(",",phase,seed,artist.primaryGenre,unsigned.Contains(artist.artistId),artist.artistId,item.SongId,item.IsOriginal?"newlyAuthored":RepertoireProvenance.Category(s,1960),s==null?"placeholder":SongProfileDeriver.Derive(s,s.demoTaxonomy,PolarSongTable.Current).archetype));
     }
    }
    foreach(var group in CompositionCatalogService.AllSongs.Where(s=>s.originKind is SongOriginKind.PreGameStandard or SongOriginKind.RecentHit or SongOriginKind.Traditional or SongOriginKind.PreGameCatalog)
     .GroupBy(s=>new {Genre=s.primaryGenre,Family=s.repertoireSeedFamily??"legacy",Archetype=SongProfileDeriver.Derive(s,s.demoTaxonomy,PolarSongTable.Current).archetype}))inventory.WriteLine(string.Join(",",phase,group.Key.Genre,group.Key.Family,group.Key.Archetype,group.Count()));
    sets.Flush();slots.Flush();inventory.Flush();ordering.Flush();GD.Print($"POLAR_REPERTOIRE_PHASE_PROGRESS seed={seed} phase={phase} actors={actors.Length}");
   }
   string actorsAfter=JsonSerializer.Serialize(actors.Select(a=>new {a.artistId,a.primaryGenre,a.songwritingAbility,a.careerState}),SaveGameService.TestJsonOptions);
   if(actorsBefore!=actorsAfter)throw new InvalidOperationException("Phase probe altered actor identity or skill");
   GD.Print($"POLAR_REPERTOIRE_PHASE_PASS seed={seed} sameWorld=True phases=0-5 actors={actors.Length}");GetTree().Quit(0);
  }catch(Exception ex){GD.PrintErr(ex);GetTree().Quit(1);}
 }
 private static ulong CensusKey(string key) {ulong h=14695981039346656037UL;foreach(char c in key){h^=c;h=unchecked(h*1099511628211UL);}return h;}
}
