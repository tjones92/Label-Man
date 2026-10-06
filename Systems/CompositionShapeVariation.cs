using System;
using System.Linq;

public enum SongContentContext { Unknown, Sacred, Secular, Mixed }

/// <summary>Versioned composition truth, independent of recording IDs and all RNG streams.</summary>
public static class CompositionShapeVariation {
 public const int LegacyVersion = 1;
 public const int CurrentVersion = 2;
 public const int LatestVersion = CurrentVersion;
 public static int ActiveVersion = LatestVersion;
 public static ulong WorldSeed;
 public static float[] V1Offsets(SongComposition song) => Enumerable.Range(0,SongProfile.AxisCount).Select(i=>
  (RepertoireTaxonomy.Unit(WorldSeed+"|composition-shape-v1|"+song.songId+"|"+i)*2-1)*(i<SongProfile.DemandCount?.012f:.008f)).ToArray();
 // World version controls new compositions. Existing compositions retain their stored schema.
 public static int ResolveVersion(string[] args, int savedVersion) {
  if(savedVersion is <0 or >LatestVersion) throw new InvalidOperationException("Unsupported composition shape version");
  int version=savedVersion;
  foreach(var name in new[]{"composition-shape-v1","composition-shape-v2"}) {
   var flags=args.Where(a=>a.StartsWith("--"+name+"=",StringComparison.Ordinal)).ToArray();
   if(flags.Length>1 || flags.Any(a=>a!="--"+name+"=on" && a!="--"+name+"=off")) throw new ArgumentException("Invalid or duplicate "+name+" flag");
   if(flags.Length==0)continue;
   if(name.EndsWith("v1"))version=flags[0].EndsWith("=on")?1:0;
   else if(flags[0].EndsWith("=on"))version=2;
   else if(version==2)version=1;
  }
  return version;
 }
 public static float Reflect(float value) {
  if(!float.IsFinite(value)) throw new InvalidOperationException("Non-finite composition axis");
  float wrapped=value%2; if(wrapped<0)wrapped+=2;
  return wrapped<=1?wrapped:2-wrapped;
 }
 public static float[] V2Offsets(SongComposition song, PolarSongTable table) {
  if(string.IsNullOrWhiteSpace(song?.songId))throw new ArgumentException("Composition ID required");
  var archetype=song.demoTaxonomy?.archetype??SongArchetype.Unknown;
  if(archetype==SongArchetype.Unknown)archetype=Enum.Parse<SongArchetype>(table.Prior(song.demoTaxonomy?.primaryGenre??song.primaryGenre).Fallback);
  var row=table.Row(archetype);
  bool instrumental=row.Axes[0]==0 && row.Axes[1]==0 && row.Axes[4]==0;
  var eligible=Enumerable.Range(0,SongProfile.AxisCount).Where(i=>!instrumental || i is not 0 and not 1 and not 4).ToArray();
  string key=WorldSeed+"|composition-shape-v2|"+song.songId;
  int leadCount=RepertoireTaxonomy.Unit(key+"|count")<.5f?2:3;
  var lead=eligible.Where(i=>i<SongProfile.DemandCount).OrderBy(i=>RepertoireTaxonomy.Hash(key+"|lead|"+i)).Take(2).ToList();
  lead.AddRange(eligible.Except(lead).OrderBy(i=>RepertoireTaxonomy.Hash(key+"|lead|"+i)).Take(leadCount-lead.Count));
  double distance=table.Archetypes.Where(r=>r.Name!=row.Name).Min(r=>Math.Sqrt(r.Axes.Select((v,i)=>(v-row.Axes[i])*(v-row.Axes[i])).Sum()));
  float amplitude=Math.Clamp((float)distance*.55f,.07f,.16f);
  return Enumerable.Range(0,SongProfile.AxisCount).Select(i=> {
   if(!eligible.Contains(i))return 0f;
   float budget=lead.Contains(i)?amplitude:i<SongProfile.DemandCount?.025f:.020f;
   return Reflect(row.Axes[i]+(RepertoireTaxonomy.Unit(key+"|axis|"+i)*2-1)*budget)-row.Axes[i];
  }).ToArray();
 }
 public static void Ensure(SongComposition song) {
  if(song == null) return;
  if(!Enum.IsDefined(song.contentContext) || (song.contentContext != SongContentContext.Unknown && string.IsNullOrWhiteSpace(song.contextAuthorshipEvidence)))
   throw new InvalidOperationException("Known composition context requires explicit authorship evidence");
  if(song.shapeVariationSchemaVersion != 0) {
   if(song.shapeVariationSchemaVersion is not 1 and not 2 || song.shapeVariation?.Length != SongProfile.AxisCount ||
    song.shapeVariation.Where((v,i)=>!float.IsFinite(v) || Math.Abs(v)>(song.shapeVariationSchemaVersion==2?.16001f:i<SongProfile.DemandCount?.012f:.008f)).Any()) throw new InvalidOperationException("Invalid composition shape variation");
   return;
  }
  if(string.IsNullOrWhiteSpace(song.songId)) throw new InvalidOperationException("Composition ID required for shape variation");
  song.shapeVariation = ActiveVersion==2 ? V2Offsets(song,PolarSongTable.Current) : V1Offsets(song);
  song.shapeVariationSchemaVersion = ActiveVersion==2?2:1;
 }
 public static void AuthorContext(SongComposition song, SongContentContext context, string evidence) {
  if(song == null || !Enum.IsDefined(context) || string.IsNullOrWhiteSpace(evidence))
   throw new ArgumentException("Explicit composition and authorship evidence required");
  song.contentContext=context; song.contextAuthorshipEvidence=evidence;
 }
}
