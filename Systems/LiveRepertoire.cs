using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public sealed class ActRepertoireState {
 public int schemaVersion=1;
 public ulong preferenceSeed;
}

/// <summary>A latent, stable songbook. Access follows the existing identity drift; booking size never participates.</summary>
public static class LiveRepertoire {
 // Headless causal replay only; normal play always uses all five repair phases.
 internal static int AuditPhase=5;
 internal static bool AuditDisableAffinity, AuditSmoothRanking;
 private static readonly Dictionary<Genre,List<SongComposition>> secondary=new();
 public static void Reset()=>secondary.Clear();
 public static void Index(SongComposition song) {
  if(song.primaryGenre==song.secondaryGenre)return;
  if(!secondary.TryGetValue(song.secondaryGenre,out var pool))secondary[song.secondaryGenre]=pool=new();
  if(!pool.Contains(song))pool.Add(song);
 }
 private static ActRepertoireState State(SimulatedArtist artist)=>artist.repertoireState??=new ActRepertoireState {preferenceSeed=RepertoireTaxonomy.Hash(artist.artistId+"|repertoire")};
 public static RepertoireAffinity Affinity(SimulatedArtist artist)=>PolarRepertoireTable.Current.GenreAffinities.GetValueOrDefault(artist.primaryGenre.ToString());
 public static LiveSetMix SetMix(SimulatedArtist artist,int year)=>AuditPhase<4||AuditDisableAffinity?null:
  PolarRepertoireTable.Current.LiveSetMixes.GetValueOrDefault(artist.primaryGenre.ToString())?.FirstOrDefault(m=>year>=m.FromYear&&year<=m.ToYear);
 public static bool OwnOriginal(SongComposition song,SimulatedArtist artist)=>song.originKind==SongOriginKind.ArtistOriginal&&
  (!string.IsNullOrEmpty(song.originArtistId)&&song.originArtistId==artist.artistId||song.credits?.Any(c=>c.isArtistMember&&!string.IsNullOrEmpty(c.writerId)&&artist.members?.Any(m=>m?.personId==c.writerId)==true)==true);
 // Old/imported unknowns retain their behavior and remain explicitly unknown in reports.
 // Only creation/import evidence can label them; do not silently call them sacred.
 public static bool EligibleLive(SongComposition song,SimulatedArtist artist)=>Affinity(artist)?.SacredLiveOnly!=true||song.contentContext is SongContentContext.Sacred or SongContentContext.Unknown;
 public static float InheritedLiveShare(SimulatedArtist artist) {
  var affinity=Affinity(artist);
  if(affinity==null)return 0;
  // Stable act-level spread: an auteur and a quartet need not share the genre mean.
  return Math.Clamp(affinity.InheritedLiveShare+(RepertoireTaxonomy.Unit(State(artist).preferenceSeed+"|inherited-affinity")*2-1)*affinity.ActSpread,0,1);
 }
 public static bool Instrumental(SimulatedArtist artist)=>artist.members?.Any(m=>m!=null&&m.isActive)==true&&!PolarActProfileDeriver.Derive(artist,null,null,PolarSongTable.Current).hasVocalist;
 public static string Cohort(SimulatedArtist artist,int year) => artist.primaryGenre switch {
  Genre.Jazz=>artist.songwritingAbility>=PolarRepertoireTable.Current.N("composerLedCutoff")?"composerLed":"standardsInterpreter",
  Genre.SurfRock=>Instrumental(artist)?"instrumentalAdaptation":"vocal",
  Genre.EasyListening=>Instrumental(artist)?"contemporaryInstrumental":"standardsLed",
  Genre.BossaNova=>artist.songwritingAbility>=PolarRepertoireTable.Current.N("composerLedCutoff")?"contemporaryBrazilian":"olderSongbook",
  Genre.ContemporaryFolk=>year>=1962?"contemporaryInterpreter":"earlyRevival",
  Genre.Folk=>"traditionalRevival",Genre.Gospel=>"inheritedRepertoire",_=>"mixedContemporary"
 };
 private static float Access(SongComposition song,Genre genre) {
  var t=PolarRepertoireTable.Current;
  if(song.primaryGenre==genre)return t.N("exactAccess");
  if(song.secondaryGenre==genre)return t.N("secondaryAccess");
  GenreFamily Family(Genre g)=>GenreCatalog.TryGet(GenreCatalog.MapLegacy(g),out var p)?p.Family:GenreFamily.Pop;
  var family=Family(genre);
  if(Family(song.primaryGenre)==family)return t.N("familyAccess");
  return SongMaterialSelectionService.CoverSourceFamilies(genre).Contains(Family(song.primaryGenre))?t.N("crossSceneAccess"):0;
 }
 private static float DriftBlend(SimulatedArtist artist,int year,int month) => artist.evolution?.lastIdentityChangeYear>=0 ?
  Math.Clamp(((year-artist.evolution.lastIdentityChangeYear)*12+month-1)/12f,0,1):1;
 public static float AccessWeight(SongComposition song,SimulatedArtist artist,int year,int month) {
  float current=Access(song,artist.primaryGenre);
  if(artist.evolution?.lastIdentityChangeYear>=0)current=SongProfileDeriver.Lerp(Access(song,artist.evolution.priorArtisticCenter),current,DriftBlend(artist,year,month));
  return current;
 }
 public static List<SongComposition> Pool(SimulatedArtist artist,int year,int month) => PoolWithAdditional(artist,year,month,null);
 // In-memory diagnostic candidates only; no registration or save persistence.
 internal static List<SongComposition> PoolWithAdditional(SimulatedArtist artist,int year,int month,IEnumerable<SongComposition> additional) {
  State(artist);
  var source=new List<SongComposition>();
  var genres=new[]{artist.primaryGenre,artist.evolution?.priorArtisticCenter??artist.primaryGenre};
  foreach(var genre in genres.Distinct()) {
   source.AddRange(CompositionCatalogService.GetStandardsForGenre(genre));source.AddRange(CompositionCatalogService.GetCoverableHitsForGenre(genre));
   if(secondary.TryGetValue(genre,out var secondarySongs))source.AddRange(secondarySongs);
   foreach(var family in SongMaterialSelectionService.CoverSourceFamilies(genre)) {
    source.AddRange(CompositionCatalogService.GetStandardsForFamily(family));source.AddRange(CompositionCatalogService.GetCoverableHitsForFamily(family));
   }
  }
  if(additional!=null)source.AddRange(additional);
  var ranked=source.Where(s=>s!=null&&s.originYear<=year).DistinctBy(s=>s.songId)
   .Select(s=>new {Song=s,Weight=AccessWeight(s,artist,year,month),Draw=RepertoireTaxonomy.Unit(State(artist).preferenceSeed+"|access|"+s.songId)})
   .Where(x=>x.Weight>0).OrderBy(x=>x.Draw/x.Weight).ThenBy(x=>RepertoireTaxonomy.Hash(x.Song.songId+"|access-collision")).ToArray();
  // A four-song introductory book for acts entering a scene; independent of requested covers.
  return ranked.Where((x,i)=>x.Draw<x.Weight||i<4).Select(x=>x.Song).ToList();
 }
 public static float Preference(SongComposition song,SimulatedArtist artist,int year,int month) {
  var t=PolarRepertoireTable.Current;ulong seed=State(artist).preferenceSeed;
  float Base(Genre genre)=>RepertoireTaxonomy.Unit(seed+"|genre|"+genre+"|"+song.songId);
  float genrePreference=Base(artist.primaryGenre);
  if(artist.evolution?.lastIdentityChangeYear>=0)genrePreference=SongProfileDeriver.Lerp(Base(artist.evolution.priorArtisticCenter),genrePreference,DriftBlend(artist,year,month));
  // Four-year sinusoid, continuous at quarter boundaries; no independent random genre process.
  double season=(year*12+month-1)/3.0;
  float drift=(float)Math.Sin(season*Math.PI/8+RepertoireTaxonomy.Unit(seed+"|season|"+song.songId)*2*Math.PI)*t.N("preferenceDrift");
  return RepertoireTaxonomy.Unit(seed+"|preference|"+song.songId)+drift+genrePreference*t.N("genrePreferenceWeight")+
   song.commercialHook*t.N("hookPreference")+song.GetFamiliarityForYear(year)*t.N("familiarityPreference");
 }
 public static float WritingPropensity(SimulatedArtist artist,int year)=>Cohort(artist,year) switch {
  "composerLed"=>.85f,"standardsInterpreter"=>.20f,"traditionalRevival"=>.25f,"inheritedRepertoire"=>.30f,
  "standardsLed"=>.20f,"contemporaryInstrumental"=>.60f,"contemporaryInterpreter"=>.90f,"earlyRevival"=>.40f,
  "olderSongbook"=>.65f,"contemporaryBrazilian"=>.85f,_=>artist.primaryGenre==Genre.Country?.70f:artist.primaryGenre==Genre.SurfRock?.75f:.65f
 };
 public static int OriginalCount(SimulatedArtist artist,int year,int size) {
  if(size<=0)return 0;
  var mix=SetMix(artist,year);
  if(mix!=null) {
   if(mix.InstrumentalOriginalsOnly&&!Instrumental(artist))return 0;
   // Stochastic rounding keeps a 5% source possible in a three-to-five song set.
   // Skill affects the original's quality rather than erasing the authored source share.
   float expected=size*mix.Originals/mix.Total/(mix.InstrumentalOriginalsOnly?mix.InstrumentalActShare:1);
   int whole=(int)Math.Floor(expected);
   float draw=RepertoireTaxonomy.Unit(State(artist).preferenceSeed+"|original-count|"+year+"|"+(TimeManager.Instance?.CurrentDate.month??1));
   return Math.Min(size,whole+(draw<expected-whole?1:0));
  }
  var t=PolarRepertoireTable.Current;
  float strength=Math.Clamp((artist.songwritingAbility-t.N("writingFloor"))/(t.N("exceptionalWriter")-t.N("writingFloor")),0,1);
  int count=(int)Math.Round(size*strength*WritingPropensity(artist,year),MidpointRounding.AwayFromZero);
  float rareChance=Cohort(artist,year)=="composerLed"?.12f:.03f;
  if(artist.songwritingAbility>=t.N("exceptionalWriter")&&RepertoireTaxonomy.Unit(State(artist).preferenceSeed+"|all-original")<rareChance)return size;
  return Math.Clamp(count,0,size-1);
 }
}
