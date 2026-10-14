using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Observation-only provenance. Rights status never determines lineage.</summary>
public static class RepertoireProvenance {
 public static int EstablishmentAge => (int)PolarRepertoireTable.Current.N("establishmentAge");
 public static float EstablishmentDurability => PolarRepertoireTable.Current.N("establishmentDurability");
 internal static bool GameplayStandard(SongComposition song,int year)=>LiveRepertoire.AuditPhase<5?song.isStandard:song.EstablishedAsOf(year);
 /// <summary>The establishment thresholds, read from the table once for a batch of tests instead of twice per song.</summary>
 internal readonly record struct EstablishmentRule(int Age, float Durability);
 internal static EstablishmentRule CurrentEstablishmentRule() => LiveRepertoire.AuditPhase<5 ? default : new(EstablishmentAge, EstablishmentDurability);
 internal static bool GameplayStandard(SongComposition song,int year,EstablishmentRule rule)=>LiveRepertoire.AuditPhase<5?song.isStandard:song.EstablishedAsOf(year,rule);
 public static bool ComedyRoutine(SongComposition song) => song != null && (song.repertoireSeedFamily=="Comedy routines" || song.primaryGenre==Genre.Comedy);
 public static bool EstablishedAsOf(this SongComposition song, int year) => song != null && song.originYear <= year && !song.isTraditional && (!LiveRepertoire.AuditGenreRepair||!ComedyRoutine(song)) &&
  (song.establishedYear.HasValue ? song.establishedYear.Value<=year :
   year-song.originYear>=EstablishmentAge && (song.standardDurability>=EstablishmentDurability ||
    song.nationalFamiliarity>=.40f&&HasDistinctRecordingArtists(song,3)) &&
   (song.isStandard || song.repertoireFirstReleaseYear>0 || song.repertoireAdmissionRoutes.Count>0));
 // Same test as above with the thresholds supplied; keep the two in step.
 internal static bool EstablishedAsOf(this SongComposition song, int year, EstablishmentRule rule) => song != null && song.originYear <= year && !song.isTraditional && (!LiveRepertoire.AuditGenreRepair||!ComedyRoutine(song)) &&
  (song.establishedYear.HasValue ? song.establishedYear.Value<=year :
   year-song.originYear>=rule.Age && (song.standardDurability>=rule.Durability ||
    song.nationalFamiliarity>=.40f&&HasDistinctRecordingArtists(song,3)) &&
   (song.isStandard || song.repertoireFirstReleaseYear>0 || song.repertoireAdmissionRoutes.Count>0));
 // recordings.Select(artistId).Distinct().Count() >= count, stopping at count instead of hashing every recording.
 private static bool HasDistinctRecordingArtists(SongComposition song, int count) {
  var seen = new string[count]; int n = 0;
  foreach (var r in song.recordings) {
   string id = r.artistId; bool known = false;
   for (int i = 0; i < n; i++) if (string.Equals(seen[i], id)) { known = true; break; }
   if (known) continue;
   if (++n == count) return true;
   seen[n - 1] = id;
  }
  return false;
 }
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
