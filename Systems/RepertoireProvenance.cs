using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Observation-only provenance. Rights status never determines lineage.</summary>
public static class RepertoireProvenance {
 public static int EstablishmentAge => (int)PolarRepertoireTable.Current.N("establishmentAge");
 public static float EstablishmentDurability => PolarRepertoireTable.Current.N("establishmentDurability");
 internal static bool GameplayStandard(SongComposition song,int year)=>LiveRepertoire.AuditPhase<5?song.isStandard:song.EstablishedAsOf(year);
 public static bool ComedyRoutine(SongComposition song) => song != null && (song.repertoireSeedFamily=="Comedy routines" || song.primaryGenre==Genre.Comedy);
 public static bool EstablishedAsOf(this SongComposition song, int year) => song != null && song.originYear <= year && !song.isTraditional && (!LiveRepertoire.AuditGenreRepair||!ComedyRoutine(song)) &&
  (song.establishedYear.HasValue ? song.establishedYear.Value<=year :
   year-song.originYear>=EstablishmentAge && (song.standardDurability>=EstablishmentDurability ||
    song.nationalFamiliarity>=.40f&&song.recordings.Select(r=>r.artistId).Distinct().Count()>=3) &&
   (song.isStandard || song.repertoireFirstReleaseYear>0 || song.repertoireAdmissionRoutes.Count>0));
 public static string Category(SongComposition song, int year, bool newlyAuthored = false) => newlyAuthored ? "newlyAuthored" :
  song.isTraditional ? "traditionalLineage" : song.EstablishedAsOf(year) ? "establishedStandard" : "existingCover";
 public static string CategoryForAct(SongComposition song,SimulatedArtist artist,int year,bool newlyAuthored=false) =>
  LiveRepertoire.AuditGenreRepair && !newlyAuthored && artist.primaryGenre==Genre.Comedy && ComedyRoutine(song) && LiveRepertoire.OwnOriginal(song,artist) ? "ownAuthored" : Category(song,year,newlyAuthored);
 public sealed record Admission(string SongId, string Route, string Family, int Year, string RecordId, int? Month);
 private static readonly List<Admission> admissions = new();
 public static IReadOnlyList<Admission> Admissions => admissions;
 public static void Reset() => admissions.Clear();
 public static void Admit(SongComposition song, string route, string family = null, int year = 0, string recordId = null) {
  var date = TimeManager.Instance?.CurrentDate;
  int eventYear = year == 0 ? song?.originYear ?? 0 : year;
  if(song != null) admissions.Add(new(song.songId, route, family, eventYear, recordId, date.HasValue && date.Value.year == eventYear ? date.Value.month : null));
 }
}
