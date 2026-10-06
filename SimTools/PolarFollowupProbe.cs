using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

// Fixed January population probe. Does not advance time or claim a 1960–63 census.
public partial class PolarFollowupProbe : Node {
 public override void _Ready() => CallDeferred(nameof(Run));
 private static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);
 private static string Category(SongComposition s) => s.isTraditional || s.isPublicDomain ? "traditional" : s.isStandard ? "standard" : s.originKind == SongOriginKind.RecentHit ? "recentHit" : "ordinaryCover";
 private static List<SongComposition> Pool(SimulatedArtist artist, bool on) {
  var pool = new List<SongComposition>();
  pool.AddRange(CompositionCatalogService.GetStandardsForGenre(artist.primaryGenre));
  pool.AddRange(CompositionCatalogService.GetCoverableHitsForGenre(artist.primaryGenre));
  if (pool.Count < 4) {
   var family = GenreCatalog.Get(artist.primaryGenre).Family;
   pool.AddRange(CompositionCatalogService.GetStandardsForFamily(family));
   pool.AddRange(CompositionCatalogService.GetCoverableHitsForFamily(family));
  }
  if(on && SongMaterialSelectionService.IsRockSongbookContext(artist.primaryGenre)) pool.AddRange(SongMaterialSelectionService.RockLiveCoverPool(artist.primaryGenre));
  return pool;
 }
 public void Run() {
  bool initial = PolarSongBehavior.UsePolarFitSelection;
  try {
   var seed = SimulationSeedBootstrap.RequestedSeed ?? throw new Exception("Development seed required");
   if(seed != 1001 && seed != 1002) throw new Exception("Only development seeds 1001/1002");
   bool allActs=OS.GetCmdlineUserArgs().Contains("--probe-all-acts");
   string root=ProjectSettings.GlobalizePath("res://SimLogs"), prefix=$"polar-followup-probe-{seed}"+(allActs?"-all":"");
   if(File.Exists(Path.Combine(root,prefix+"-pool.csv"))) throw new Exception("Probe already exists");
   using var poolWriter = new StreamWriter(Path.Combine(root,prefix+"-pool.csv"));
   using var fitWriter = new StreamWriter(Path.Combine(root,prefix+"-fit.csv"));
   using var setWriter = new StreamWriter(Path.Combine(root,prefix+"-sets.csv"));
   poolWriter.WriteLine("seed,mode,genre,category,rawEntries,distinctSongs,eligibleSongs,futureSongs,meanPreliminaryCapability,meanPreliminaryIdentity,meanPreliminaryStretch,capabilityAbove095");
   fitWriter.WriteLine("seed,mode,genre,phase,count,meanCapability,meanIdentity,meanStretch,capabilityAbove095");
   setWriter.WriteLine("seed,mode,genre,sets,slots,originals,traditional,standard,recentHit,ordinaryCover,shortSets,emptyEligiblePoolSets,thinEligiblePoolSets,futureExcluded,allCoverCounterfactualSlots,allCoverCounterfactualSongbookSlots");
   var unsigned=ArtistManager.Instance.GetUnsignedArtists();
   var artists=(allActs?ChartManager.Instance.GetAllLabels().SelectMany(l=>l.roster).Concat(unsigned):unsigned)
    .GroupBy(a=>a.artistId).Select(g=>g.First()).OrderBy(a=>a.artistId,StringComparer.Ordinal).ToArray();
   foreach(bool on in new[]{false,true}) {
    PolarSongBehavior.UsePolarFitSelection=on;
    foreach(var group in artists.GroupBy(a=>a.primaryGenre)) {
     var categoryRows=new Dictionary<string,List<MaterialFit>>();
     var categories=new Dictionary<string,(int raw,int distinct,int eligible,int future)>();
     var candidates=new List<MaterialFit>(); var selected=new List<MaterialFit>();
     int sets=0,slots=0,originals=0,trad=0,std=0,hit=0,other=0,shorts=0,empty=0,thin=0,future=0,cfSlots=0,cfSongbook=0;
     foreach(var artist in group) {
      var raw=Pool(artist,on);var distinct=raw.Where(s=>s!=null).GroupBy(s=>s.songId).Select(g=>g.First()).ToArray();var eligible=distinct.Where(s=>s.originYear<=1960).ToArray();
      foreach(var cat in distinct.GroupBy(Category)) {
       var old=categories.GetValueOrDefault(cat.Key); categories[cat.Key]=(old.raw+raw.Count(s=>s!=null&&Category(s)==cat.Key),old.distinct+cat.Count(),old.eligible+cat.Count(s=>s.originYear<=1960),old.future+cat.Count(s=>s.originYear>1960));
      }
      foreach(var s in eligible){var fit=PolarSongBehavior.Fit(s,artist,artist.primaryGenre,1960);candidates.Add(fit);if(!categoryRows.TryGetValue(Category(s),out var list))categoryRows[Category(s)]=list=new();list.Add(fit);}
      using var random = new RandomNumberGenerator {Seed=Hash($"{seed}:1960:1:{artist.artistId}")};
      var prospect=new PlayerDesk.Prospect {Artist=artist};PlayerDesk.Instance.BuildLiveSet(prospect,artist,1960,0,random);
      sets++;slots+=prospect.LiveSet.Count;originals+=prospect.LiveSet.Count(s=>s.IsOriginal);
      if(prospect.LiveSet.Count<3)shorts++;if(eligible.Length==0)empty++;if(eligible.Length<3)thin++;future+=distinct.Length-eligible.Length;
      foreach(var item in prospect.LiveSet.Where(i=>!i.IsOriginal)){
       var s=CompositionCatalogService.GetSong(item.SongId);if(s==null)throw new Exception("Missing selected song");
       switch(Category(s)){case "traditional":trad++;break;case "standard":std++;break;case "recentHit":hit++;break;default:other++;break;}
       selected.Add(PolarSongBehavior.Fit(s,artist,artist.primaryGenre,1960));
      }
      if(on){ // Change only the audit's requested cover count, never the table or gameplay writing thresholds.
       var cf=SongMaterialSelectionService.SelectLiveCovers(raw,artist,1960,prospect.LiveSet.Count);
       cfSlots+=cf.Count;cfSongbook+=cf.Count(s=>Category(s) is "traditional" or "standard");
      }
     }
     string mode=on?"on":"off";
     foreach(var pair in categories){var fits=categoryRows.GetValueOrDefault(pair.Key)??new();var n=pair.Value;poolWriter.WriteLine(string.Join(",",seed,mode,group.Key,pair.Key,n.raw,n.distinct,n.eligible,n.future,F(fits.Count==0?0:fits.Average(f=>f.Capability)),F(fits.Count==0?0:fits.Average(f=>f.Identity)),F(fits.Count==0?0:fits.Average(f=>f.Stretch)),fits.Count(f=>f.Capability>.95f)));}
     foreach(var pair in new[]{("candidate",candidates),("selectedCover",selected)}){var list=pair.Item2;fitWriter.WriteLine(string.Join(",",seed,mode,group.Key,pair.Item1,list.Count,F(list.Count==0?0:list.Average(f=>f.Capability)),F(list.Count==0?0:list.Average(f=>f.Identity)),F(list.Count==0?0:list.Average(f=>f.Stretch)),list.Count(f=>f.Capability>.95f)));}
     setWriter.WriteLine(string.Join(",",seed,mode,group.Key,sets,slots,originals,trad,std,hit,other,shorts,empty,thin,future,cfSlots,cfSongbook));
    }
   }
   GD.Print($"POLAR_FOLLOWUP_PROBE_PASS seed={seed} population={artists.Length} date={TimeManager.Instance.CurrentDate.ToShortString()} fixedWorld=True allActs={allActs}");
   GetTree().Quit(0);
  }catch(Exception ex){GD.PrintErr(ex);GetTree().Quit(1);}finally{PolarSongBehavior.UsePolarFitSelection=initial;}
 }
 private static ulong Hash(string key){ulong h=14695981039346656037UL;foreach(char c in key){h^=c;h=unchecked(h*1099511628211UL);}return h;}
}
