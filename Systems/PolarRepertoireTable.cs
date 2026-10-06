using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public sealed class PolarRepertoireTable {
 public int Version {get;set;}
 public string Provenance {get;set;}
 public Dictionary<string,float> Numbers {get;set;}
 public Dictionary<string,RepertoireAssignment[]> Assignments {get;set;}
 public RepertoireBand[] Bands {get;set;}
 public RepertoireSupply[] Supply {get;set;}
 public Dictionary<string,RepertoireAffinity> GenreAffinities {get;set;} = new();
 public Dictionary<string,LiveSetMix[]> LiveSetMixes {get;set;} = new();
 public float N(string key)=>Numbers[key];
 private static readonly Lazy<PolarRepertoireTable> loaded=new(()=> {
  using var file=Godot.FileAccess.Open("res://Data/PolarRepertoireTable.json",Godot.FileAccess.ModeFlags.Read);
  if(file==null)throw new InvalidOperationException("Missing repertoire table");
  return Parse(file.GetAsText());
 });
 public static PolarRepertoireTable Current=>loaded.Value;
 public static PolarRepertoireTable Parse(string json) {
  var t=JsonSerializer.Deserialize<PolarRepertoireTable>(json,new JsonSerializerOptions {PropertyNameCaseInsensitive=true});
  if(t?.Version!=1||t.Numbers==null||t.Assignments==null||t.Bands==null||t.Supply==null)throw new InvalidOperationException("Invalid repertoire schema");
  if(t.Numbers.Any(kv=>!float.IsFinite(kv.Value)||kv.Value<0||(kv.Key!="establishmentAge"&&kv.Value>1)))throw new InvalidOperationException("Invalid repertoire numbers");
  if(t.N("establishmentAge")<=0||t.N("establishmentAge")%1!=0||t.N("suitabilityWindow")<=0||t.N("exceptionalWriter")<=t.N("writingFloor"))throw new InvalidOperationException("Invalid repertoire age or selection range");
  foreach(var rows in t.Assignments.Values)foreach(var r in rows) {
   if(r.Weight<=0||!float.IsFinite(r.Weight)||r.Density<0||r.Density>1||!Enum.TryParse<SongArchetype>(r.Archetype,out var a)||a==SongArchetype.Unknown||
    !Enum.TryParse<SongLyricMode>(r.Lyric,out _)||!Enum.TryParse<SongVocalPresence>(r.VocalPresence,out _)||!Enum.TryParse<SongForm>(r.Form,out _)||!Enum.TryParse<SongMeter>(r.Meter,out _)||
    r.Context!=null&&(!Enum.TryParse<SongContentContext>(r.Context,out var context)||context==SongContentContext.Unknown))throw new InvalidOperationException("Invalid repertoire assignment");
  }
  foreach(var band in t.Bands)if(band.Min<0||band.Max>100||band.Min>band.Max)throw new InvalidOperationException("Invalid historical band");
  foreach(var (genre,a) in t.GenreAffinities)if(!Enum.TryParse<Genre>(genre,out _)||a==null||
   !float.IsFinite(a.InheritedLiveShare)||!float.IsFinite(a.ActSpread)||a.InheritedLiveShare<0||a.InheritedLiveShare>1||a.ActSpread<0||a.ActSpread>1||
   a.SongbookFamily!=null&&!t.Assignments.ContainsKey(a.SongbookFamily)||a.WritingPropensity.HasValue&&(!float.IsFinite(a.WritingPropensity.Value)||a.WritingPropensity<0||a.WritingPropensity>1))
   throw new InvalidOperationException("Invalid genre repertoire affinity");
  foreach(var (genre,rows) in t.LiveSetMixes) {
   if(!Enum.TryParse<Genre>(genre,out _)||rows==null||rows.Length==0)throw new InvalidOperationException("Invalid live set genre");
   foreach(var row in rows)if(row.FromYear>row.ToYear||row.StandardFromYear>row.StandardToYear||
    new[]{row.Traditional,row.Standards,row.Contemporary,row.Originals,row.InstrumentalActShare}.Any(w=>!float.IsFinite(w)||w<0)||!float.IsFinite(row.Total)||row.Total<=0||row.CoverTotal<=0||row.InstrumentalActShare>1||
    row.InstrumentalOriginalsOnly&&(row.InstrumentalActShare<=0||row.Originals/row.Total>row.InstrumentalActShare)||
    new[]{row.RecentHitFloor,row.RecentHitCeiling}.Any(w=>!float.IsFinite(w)||w<0||w>1)||row.RecentHitFloor>row.RecentHitCeiling||row.RecentHitCeiling>0&&row.RecentHitFromYear>row.RecentHitToYear)
    throw new InvalidOperationException("Invalid live set mix");
   var ordered=rows.OrderBy(r=>r.FromYear).ToArray();
   for(int i=1;i<ordered.Length;i++)if(ordered[i].FromYear<=ordered[i-1].ToYear)throw new InvalidOperationException("Overlapping live set eras");
  }
  return t;
 }
}
public enum LiveCoverSource { Traditional, Standard, Contemporary }
public sealed class LiveSetMix {
 public int FromYear {get;set;}
 public int ToYear {get;set;} = int.MaxValue;
 public float Traditional {get;set;}
 public float Standards {get;set;}
 public float Contemporary {get;set;}
 public float Originals {get;set;}
 public int StandardFromYear {get;set;} = int.MinValue;
 public int StandardToYear {get;set;} = int.MaxValue;
 public bool InstrumentalOriginalsOnly {get;set;}
 public float InstrumentalActShare {get;set;}
 // Probability that a Contemporary-source slot is reserved for recent hits rather than the
 // merged ranking (where soundtrack themes otherwise always win). Linear era ramp; 0 = no channel.
 public float RecentHitFloor {get;set;}
 public float RecentHitCeiling {get;set;}
 public int RecentHitFromYear {get;set;}
 public int RecentHitToYear {get;set;}
 public float RecentHitShare(int year,int month)=>RecentHitCeiling<=0?0:
  RecentHitFloor+(RecentHitCeiling-RecentHitFloor)*Math.Clamp((year+(month-1)/12f-RecentHitFromYear)/Math.Max(1,RecentHitToYear-RecentHitFromYear),0,1);
 public float CoverTotal=>Traditional+Standards+Contemporary;
 public float Total=>CoverTotal+Originals;
 public LiveCoverSource Source(SongComposition song,int year)=>song.isTraditional||song.isPublicDomain?LiveCoverSource.Traditional:
  song.originYear>=StandardFromYear&&song.originYear<=StandardToYear&&song.EstablishedAsOf(year)?LiveCoverSource.Standard:LiveCoverSource.Contemporary;
 public LiveCoverSource PreferredSource(float draw)=>draw*CoverTotal<Traditional?LiveCoverSource.Traditional:
  draw*CoverTotal<Traditional+Standards?LiveCoverSource.Standard:LiveCoverSource.Contemporary;
 public float Weight(LiveCoverSource source)=>source==LiveCoverSource.Traditional?Traditional:source==LiveCoverSource.Standard?Standards:Contemporary;
}
public sealed class RepertoireAffinity {
 public float InheritedLiveShare {get;set;}
 public float ActSpread {get;set;}
 public bool SacredLiveOnly {get;set;}
 public string SongbookFamily {get;set;}
 public float? WritingPropensity {get;set;}
}
public sealed class RepertoireAssignment {
 public string Context {get;set;}
 public string Archetype {get;set;} public float Weight {get;set;} public string Lyric {get;set;}
 public string VocalPresence {get;set;} public float Density {get;set;} public string Form {get;set;} public string Meter {get;set;}
}
public sealed class RepertoireBand {public string Genre {get;set;} public string Cohort {get;set;} public float Min {get;set;} public float Max {get;set;}}
public sealed class RepertoireSupply {public string Family {get;set;} public string Genre {get;set;} public string Secondary {get;set;} public int Count {get;set;} public int FromYear {get;set;} public int ToYear {get;set;} public bool Traditional {get;set;} public int? EstablishedYear {get;set;}}

/// <summary>Persistent creation truth, independent of scouting, booking and RNG streams.</summary>
public static class RepertoireTaxonomy {
 public static ulong Hash(string key) {
  ulong hash=14695981039346656037UL;foreach(char c in key){hash^=c;hash=unchecked(hash*1099511628211UL);}
  // MurmurHash3 avalanche: adjacent catalogue IDs must not have adjacent preference draws.
  hash^=hash>>33;hash=unchecked(hash*0xff51afd7ed558ccdUL);hash^=hash>>33;hash=unchecked(hash*0xc4ceb9fe1a85ec53UL);return hash^(hash>>33);
 }
 public static float Unit(string key)=>(Hash(key)>>40)/(float)(1UL<<24);
 public static void Assign(SongComposition song,string family,int observationYear) {
  var table=PolarRepertoireTable.Current;
  var rows=table.Assignments[family].Where(r=>PolarSongTable.Current.Row(Enum.Parse<SongArchetype>(r.Archetype)).FromYear<=observationYear).ToArray();
  if(rows.Length==0)throw new InvalidOperationException("No year-appropriate authored taxonomy: "+family);
  float draw=Unit(song.songId+"|taxonomy")*rows.Sum(r=>r.Weight);var chosen=rows[^1];
  foreach(var row in rows){draw-=row.Weight;if(draw<0){chosen=row;break;}}
  var archetype=Enum.Parse<SongArchetype>(chosen.Archetype);var shape=PolarSongTable.Current.Row(archetype);
  song.demoTaxonomy=new SongTaxonomy {archetype=archetype,primaryGenre=song.primaryGenre,secondaryGenre=song.secondaryGenre,
   lyricModes=new[]{Enum.Parse<SongLyricMode>(chosen.Lyric)},vocalPresence=Enum.Parse<SongVocalPresence>(chosen.VocalPresence),
   tags=PolarSongMetadataService.ClassifyLegacyTags(song.genreTagIds),pace=shape.Pace,mood=shape.Mood,mappingId="repertoire-v1"};
  song.wordDensity=chosen.Density;song.defaultForm=Enum.Parse<SongForm>(chosen.Form);song.defaultMeter=Enum.Parse<SongMeter>(chosen.Meter);
  if(chosen.Context!=null)CompositionShapeVariation.AuthorContext(song,Enum.Parse<SongContentContext>(chosen.Context),"procedural-repertoire-authoring:"+family+":"+chosen.Archetype);
  song.repertoireSchemaVersion=1;song.repertoireSeedFamily=family;song.repertoireVariation=new float[SongProfile.AxisCount];
  for(int i=0;i<song.repertoireVariation.Length;i++)song.repertoireVariation[i]=(Unit(song.songId+"|variation|"+i)*2-1)*table.N(i<SongProfile.DemandCount?"demandVariation":"identityVariation");
 }
}
