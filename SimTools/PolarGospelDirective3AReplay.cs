using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public partial class PolarGospelRepairProbe {
 // Monthly frozen-arrangement counterfactuals on the original legacy-center world path.
 public static void WriteAReplay(StreamWriter output,SimulatedArtist artist,PlayerDesk.Prospect observed,int requested,bool unsigned,GameDate date,int week) {
  if(!PolarSongTable.AuditLegacyRepair||CompositionShapeVariation.ActiveVersion!=0)throw new InvalidOperationException("D4 replay requires old centers and both shape flags off");
  var old=PolarSongTable.Current;var repair=PolarSongTable.RepairTable;
  var label=ChartManager.Instance.GetLabelById(artist.labelId);
  var oldAct=PolarActProfileDeriver.Derive(artist,label,null,old);var newAct=PolarActProfileDeriver.Derive(artist,label,null,repair);
  var taste=PolarSongBehavior.CaptureTaste().GetValueOrDefault(artist.primaryGenre);var shrunk=PolarSongBehavior.ShrinkTaste(taste,artist.primaryGenre,repair);
  var pool=LiveRepertoire.Pool(artist,date.year,date.month).DistinctBy(s=>s.songId).ToArray();
  bool empty=PlayerDesk.Instance.RepertoireFor(artist.artistId).Count==0&&!PlayerDesk.Instance.SongsFor(artist.artistId).Any();
  int originals=observed.LiveSet.Count(s=>s.IsOriginal);
  var candidates=pool.Select(song=>new Candidate {Song=song,Proposal=PolarSongBehavior.LiveProposal(song,artist,date.year)}).ToArray();
  string poolHash=Hash(string.Join("\n",pool.Select(s=>s.songId).OrderBy(s=>s,StringComparer.Ordinal)));
  foreach(string name in new[]{"baseline","packageA","packageB","packageAB","axis0","axis1","axis2","axis3","axis0WithB","axis1WithB","axis2WithB","axis3WithB"}) {
   var act=PolarActProfileDeriver.Derive(artist,label,null,old);
   if(name is "packageA" or "packageAB")act=newAct;
   if(name.StartsWith("axis",StringComparison.Ordinal)){int axis=name[4]-'0';act.axes[SongProfile.DemandCount+axis]=newAct.axes[SongProfile.DemandCount+axis];}
   var market=name is "packageB" or "packageAB"||name.EndsWith("WithB",StringComparison.Ordinal)?shrunk:taste;
   foreach(var c in candidates)c.RepairFits[name]=PolarMaterialFit.Evaluate(c.Proposal.referenceProfile,c.Proposal.realizedProfile,act,market,week,repair);
   MaterialFit Fit(Candidate c)=>c.RepairFits[name];
   var selected=SelectCached(candidates,artist,date.year,requested,empty,c=>PolarSongBehavior.SelectionScore(Fit(c)),Fit).ToArray();
   if(name=="baseline"&&!selected.Select(c=>c.Song.songId).SequenceEqual(observed.LiveSet.Where(s=>!s.IsOriginal).Select(s=>s.SongId)))throw new InvalidOperationException("D4 monthly baseline selector mismatch");
   output.WriteLine(JsonSerializer.Serialize(new {date=date.ToShortString(),artistId=artist.artistId,genre=artist.primaryGenre.ToString(),unsigned,cohort=LiveRepertoire.Cohort(artist,date.year),variant=name,originals,requestedCovers=requested,filledCovers=selected.Length,inherited=selected.Count(c=>c.Song.isTraditional||c.Song.EstablishedAsOf(date.year)),slots=originals+selected.Length,poolHash,baselineMatched=true}));
  }
  output.Flush();
 }
}
