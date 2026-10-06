using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SHA256 = System.Security.Cryptography.SHA256;
using CryptoStream = System.Security.Cryptography.CryptoStream;
using CryptoStreamMode = System.Security.Cryptography.CryptoStreamMode;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

// Research controls belong to the observer, never to repertoire selection.
public partial class ChartAuditRunner {
 private string polarCensusMode = "none";
 private int polarSamplePerStratum = 4, polarDiagnosticLimit, polarDiagnosticsUsed;
 private double polarMaxSeconds = 300;
 private int polarStopAfterActs;
 private string polarLoadSnapshot;
 private bool polarVerifySnapshot;
 private bool genreFollowUpCensus;
 private bool genreRepairCensus;
 private bool ObserveGenre(Genre genre)=>!genreFollowUpCensus||genreRepairCensus||GenreFollowUpGenres.Contains(genre);
 private int polarCensusFromYear = 1960;
 private readonly HashSet<int> polarCensusMonths = new();
 private static readonly HashSet<Genre> GenreFollowUpGenres = new() { Genre.TeenPop, Genre.Country, Genre.Comedy, Genre.Classical, Genre.Childrens, Genre.TraditionalPop };
 private readonly Stopwatch polarResearchClock = Stopwatch.StartNew();
 private readonly HashSet<string> polarPanel = new(StringComparer.Ordinal);
 private StreamWriter polarSamplingWriter, polarCoverageWriter, polarTimingWriter;
 private PolarSample currentPolarSample;
 private readonly List<PolarMonthStatus> polarMonths = new();
 private bool polarResearchEnabled;
 private int polarObservedActs;
 private static readonly string[] PolarWritingBins = { "low", "middle", "high" };
 private const string PolarProvenanceDefinition = "Numerator: traditional lineage plus standards established as of observation year. Denominator: all filled live-set song slots, including newly authored material. Public-domain rights do not determine lineage.";

 public sealed class PolarMonthStatus {
  public string Date { get; set; }
  public string Status { get; set; }
  public int Population { get; set; }
  public int Planned { get; set; }
  public int Observed { get; set; }
 }
 public sealed class PolarResearchSnapshot {
  public int Version { get; set; } = 1;
  public SaveEnvelope Save { get; set; }
  public string SourceRun { get; set; }
  public int CompletedAuditWeeks { get; set; }
  public int CompletedCensusMonth { get; set; } = -1;
  public int RepertoirePhase { get; set; }
  public bool FitSelection { get; set; }
  public List<string> Panel { get; set; } = new();
 }
 private sealed class PolarSample {
  public SimulatedArtist Artist;
  public bool Unsigned, CrossSection, Panel;
  public string Cohort, WritingBin;
  public int Population, Sample;
  public double Weight => CrossSection ? Population / (double)Sample : 0;
 }
 private sealed class PolarResearchStop : Exception {
  public PolarResearchStop(string reason) : base(reason) { }
 }
 private string PolarResearchPath(string suffix) => Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), runName + suffix);
 private void ConfigurePolarResearch() {
  var args = OS.GetCmdlineUserArgs();
  polarResearchEnabled = args.Contains("--polar-final-audit");
  if (!polarResearchEnabled) return;
  foreach (string arg in args) {
   if (arg.StartsWith("--polar-census=")) polarCensusMode = arg["--polar-census=".Length..];
   if (arg.StartsWith("--polar-sample-per-stratum=")) polarSamplePerStratum = int.Parse(arg["--polar-sample-per-stratum=".Length..], CultureInfo.InvariantCulture);
   if (arg.StartsWith("--polar-diagnostic-acts=")) polarDiagnosticLimit = int.Parse(arg["--polar-diagnostic-acts=".Length..], CultureInfo.InvariantCulture);
   if (arg.StartsWith("--polar-max-seconds=")) polarMaxSeconds = double.Parse(arg["--polar-max-seconds=".Length..], CultureInfo.InvariantCulture);
   if (arg.StartsWith("--polar-stop-after-acts=")) polarStopAfterActs = int.Parse(arg["--polar-stop-after-acts=".Length..], CultureInfo.InvariantCulture);
   if (arg.StartsWith("--polar-load-snapshot=")) polarLoadSnapshot = arg["--polar-load-snapshot=".Length..];
   if (arg.StartsWith("--polar-census-from-year=")) polarCensusFromYear = int.Parse(arg["--polar-census-from-year=".Length..], CultureInfo.InvariantCulture);
   if (arg.StartsWith("--polar-census-months=")) foreach(var month in arg["--polar-census-months=".Length..].Split(',')) polarCensusMonths.Add(int.Parse(month, CultureInfo.InvariantCulture));
  }
  genreFollowUpCensus = args.Contains("--genre-followup-census");
  genreRepairCensus = args.Contains("--genre-repertoire-repair-census");
  if(polarCensusFromYear < 1960 || polarCensusFromYear > 1963 || polarCensusMonths.Any(m=>m<1||m>12)) throw new ArgumentException("Invalid census observation window.");
  if (args.Contains("--polar-no-census")) polarCensusMode = "none";
  polarVerifySnapshot = args.Contains("--polar-verify-snapshot");
  if (!new[] { "sample", "full", "none" }.Contains(polarCensusMode) || polarSamplePerStratum < 1 || polarDiagnosticLimit < 0 || !double.IsFinite(polarMaxSeconds) || polarMaxSeconds < 0 || polarStopAfterActs < 0)
   throw new ArgumentException("Invalid Polar research controls.");
  // Full is a deliberately short validation mode, not an accidental multi-year workload.
  if (polarCensusMode == "full" && requestedWeeks > 8) throw new ArgumentException("Full Polar census is limited to eight weeks. Use sampling for longer windows.");
 }
 private void OpenPolarResearch(string directory) {
  polarSamplingWriter = CreateWriter(Path.Combine(directory, runName + "-polar-sample-design.csv"));
  polarSamplingWriter.WriteLine("seed,date,genre,cohort,unsigned,writingBin,population,sample,panelObserved,panelOnly,weight");
  polarCoverageWriter = CreateWriter(Path.Combine(directory, runName + "-polar-sample-acts.csv"));
  polarCoverageWriter.WriteLine("seed,date,genre,cohort,unsigned,writingBin,artistId,population,sample,weight,crossSection,panel,requestedSlots,filledSlots,candidateEntries");
  polarTimingWriter = CreateWriter(Path.Combine(directory, runName + "-polar-research-timing.csv"));
  polarTimingWriter.WriteLine("seed,date,operation,seconds,population,observed,candidateEntries");
  WritePolarResearchManifest();
 }
 private void WritePolarResearchManifest() {
  var payload = new {
   schemaVersion = 1, run = runName, seed = requestedSeed, censusMode = polarCensusMode,
   samplePerStratum = polarSamplePerStratum, diagnosticActsPerMonth = polarDiagnosticLimit, maxSeconds = polarMaxSeconds,
   observationWindow = new { fromYear = polarCensusFromYear, months = polarCensusMonths.OrderBy(m=>m).ToArray(), genres = genreFollowUpCensus&&!genreRepairCensus ? GenreFollowUpGenres.OrderBy(g=>g).Select(g=>g.ToString()).ToArray() : null },
   definitions = new {
    provenance = PolarProvenanceDefinition,
    sampling = "Lowest seed/artist hash ranks within genre, cohort, signed/unsigned population and writing bin; no month in hash. All acts in strata smaller than the cap. Weight = population/sample. Panel-only observations have weight zero.",
    writingBins = "low <= 0.3; middle > 0.3 and <= 0.6; high > 0.6",
    panel = "First observed sample retained by artist ID across months and signing/genre changes. Report survival and use fixed genre/set sizes when analyzing retention.",
    absent = "Zero population is an absent stratum, never a successful zero-valued result.",
    completeness = "Only months marked complete may enter aggregate estimates. Partial month rows are retained for diagnosis only.",
    snapshot = "Loadable world checkpoint; repeated loads replay deterministically. Existing global RNG restore does not guarantee identity with uninterrupted continuation. Audit telemetry history is not resumed."
   }, months = polarMonths
  };
  AtomicPolarJson(PolarResearchPath("-polar-research.json"), payload);
 }
 private static void AtomicPolarJson<T>(string path, T value) {
  string temporary = path + ".tmp";
  using (var output = File.Create(temporary)) { JsonSerializer.Serialize(output, value, SaveGameService.TestJsonOptions); output.Flush(true); }
  File.Move(temporary, path, true);
 }
 private void SavePolarCheckpoint(string reason) {
  if (!polarResearchEnabled) return;
  var watch = Stopwatch.StartNew();
  var date = TimeManager.Instance.CurrentDate;
  var snapshot = new PolarResearchSnapshot {
   SourceRun = runName, CompletedAuditWeeks = currentAuditWeek,
   CompletedCensusMonth = polarMonths.LastOrDefault()?.Status == "complete" ? polarCensusMonth : -1,
   RepertoirePhase = LiveRepertoire.AuditPhase, FitSelection = PolarSongBehavior.UsePolarFitSelection,
   Panel = polarPanel.OrderBy(x => x, StringComparer.Ordinal).ToList(),
   Save = new SaveEnvelope {
    WorldSeed = requestedSeed, SavedAtUtc = DateTime.UtcNow.ToString("o"), Year = date.year, Month = date.month, Day = date.day,
    World = WorldStateService.Capture(), Player = PlayerDesk.Instance?.HasLabel == true ? PlayerDesk.Instance.CaptureState() : null
   }
  };
  string path = PolarResearchPath("-polar-checkpoint.json.gz"), temporary = path + ".tmp";
  using (var output = File.Create(temporary)) {
   using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true)) JsonSerializer.Serialize(gzip, snapshot, SaveGameService.TestJsonOptions);
   output.Flush(true);
  }
  File.Move(temporary, path, true);
  polarTimingWriter?.WriteLine(string.Join(",", requestedSeed, date.ToShortString(), "checkpoint", DF(watch.Elapsed.TotalSeconds), snapshot.Save.World.Artists.Count, 0, 0));
  polarTimingWriter?.Flush();
  GD.Print($"POLAR_CHECKPOINT run={runName} reason={reason} week={currentAuditWeek} date={date.ToShortString()} path={path}");
 }
 private void LoadPolarCheckpoint() {
  if (polarLoadSnapshot == null) return;
  using var input = File.OpenRead(polarLoadSnapshot);
  using var gzip = new GZipStream(input, CompressionMode.Decompress);
  var snapshot = JsonSerializer.Deserialize<PolarResearchSnapshot>(gzip, SaveGameService.TestJsonOptions);
  if (snapshot?.Version != 1 || snapshot.Save?.World == null || snapshot.Save.WorldSeed != requestedSeed || snapshot.RepertoirePhase != LiveRepertoire.AuditPhase || snapshot.FitSelection != PolarSongBehavior.UsePolarFitSelection)
   throw new InvalidOperationException("Snapshot seed, version or repertoire settings do not match.");
  var save = snapshot.Save;
  RepertoireProvenance.Reset(); // Fresh-process seed admissions do not describe this restored world.
  WorldStateService.Apply(save.World, new GameDate(save.Year, save.Month, save.Day), save.WorldSeed);
  if (save.Player != null && !PlayerDesk.Instance.RestoreState(save.Player, out _)) throw new InvalidOperationException("Could not restore checkpoint player state.");
  polarPanel.UnionWith(snapshot.Panel);
  polarCensusMonth = snapshot.CompletedCensusMonth;
  if (polarVerifySnapshot) {
   string before = CheckpointWorldDigest(save.World);
   string after = CheckpointWorldDigest(WorldStateService.Capture());
   if (before != after) throw new InvalidOperationException("Polar checkpoint capture/load round-trip changed world state.");
   GD.Print($"POLAR_CHECKPOINT_ROUNDTRIP_PASS source={snapshot.SourceRun} date={TimeManager.Instance.CurrentDate.ToShortString()}");
  }
  GD.Print($"POLAR_CHECKPOINT_LOADED source={snapshot.SourceRun} priorWeeks={snapshot.CompletedAuditWeeks}; new audit window begins here");
 }
 internal static string CheckpointWorldDigest(WorldSaveData world) {
  // Late worlds exceed the maximum .NET string size. Compare identical serialized
  // bytes without materializing either complete JSON string in memory.
  using var hash=SHA256.Create();
  using var sink=new CryptoStream(Stream.Null,hash,CryptoStreamMode.Write);
  JsonSerializer.Serialize(sink,world,SaveGameService.TestJsonOptions);
  sink.FlushFinalBlock();
  return Convert.ToHexString(hash.Hash);
 }
 private void CheckPolarResearchStop() {
  if (!polarResearchEnabled) return;
  if (File.Exists(PolarResearchPath(".stop"))) throw new PolarResearchStop("stop-file");
  if (polarMaxSeconds > 0 && polarResearchClock.Elapsed.TotalSeconds >= polarMaxSeconds) throw new PolarResearchStop("time-budget");
  if (polarStopAfterActs > 0 && polarObservedActs >= polarStopAfterActs) throw new PolarResearchStop("bounded-stop-check");
 }
 private void FlushPolarResearch() {
  if (!polarResearchEnabled) return;
  if (repertoireAdmissions != null) FlushRepertoireAdmissions();
  polarCensusWriter?.Flush(); polarFinalWeekWriter?.Flush();
  polarAssignmentWriter?.Flush(); polarAvoidanceWriter?.Flush();
  polarDiversitySetWriter?.Flush(); polarDiversitySongWriter?.Flush(); polarDiversityPoolWriter?.Flush();
  polarSamplingWriter?.Flush(); polarCoverageWriter?.Flush(); polarTimingWriter?.Flush();
  // Flush every open audit writer, including streams previously retained only annually.
  foreach (var field in typeof(ChartAuditRunner).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
   if (field.FieldType == typeof(StreamWriter)) (field.GetValue(this) as StreamWriter)?.Flush();
 }
 private string PolarSampleSuffix => currentPolarSample == null ? "" : string.Join(",", currentPolarSample.Cohort, currentPolarSample.WritingBin,
  currentPolarSample.Population, currentPolarSample.Sample, DF(currentPolarSample.Weight), currentPolarSample.CrossSection, currentPolarSample.Panel);
 private void ClosePolarResearch() {
  polarSamplingWriter?.Dispose(); polarCoverageWriter?.Dispose(); polarTimingWriter?.Dispose();
  polarSamplingWriter = polarCoverageWriter = polarTimingWriter = null;
 }
 private List<PolarSample> SelectPolarSample(GameDate date) {
  var unsignedIds = ArtistManager.Instance.GetUnsignedArtists().Select(a => a.artistId).ToHashSet(StringComparer.Ordinal);
  var all = ArtistManager.Instance.GetUnsignedArtists().Concat(ChartManager.Instance.GetAllLabels().SelectMany(l => l.roster))
   .Where(a => a != null).DistinctBy(a => a.artistId).Select(a => new PolarSample {
    Artist = a, Unsigned = unsignedIds.Contains(a.artistId), Cohort = LiveRepertoire.Cohort(a, date.year),
    WritingBin = a.songwritingAbility <= .3f ? "low" : a.songwritingAbility <= .6f ? "middle" : "high"
   }).ToArray();
  if(genreFollowUpCensus) all = all.Where(a=>ObserveGenre(a.Artist.primaryGenre)).ToArray();
  var strata = all.GroupBy(a => (a.Artist.primaryGenre, a.Cohort, a.Unsigned, a.WritingBin)).ToDictionary(g => g.Key, g => g.ToArray());
  bool firstSample = polarPanel.Count == 0;
  foreach (var group in strata.Values) {
   int count = polarCensusMode == "full" ? group.Length : Math.Min(polarSamplePerStratum, group.Length);
   foreach (var entry in group.OrderBy(a => RepertoireTaxonomy.Hash($"polar-sample:{requestedSeed}:{a.Artist.artistId}")).ThenBy(a => a.Artist.artistId, StringComparer.Ordinal).Take(count)) entry.CrossSection = true;
   if (firstSample) polarPanel.UnionWith(group.Where(a => a.CrossSection).Select(a => a.Artist.artistId));
   foreach (var entry in group) { entry.Population = group.Length; entry.Sample = count; entry.Panel = polarPanel.Contains(entry.Artist.artistId); }
  }
  var expected = strata.Keys.Concat(PolarRepertoireTable.Current.Bands.Where(b=>ObserveGenre(Enum.Parse<Genre>(b.Genre))).SelectMany(b => new[] { true, false }.SelectMany(u => PolarWritingBins.Select(w => (Enum.Parse<Genre>(b.Genre), b.Cohort, u, w)))))
   .Distinct().OrderBy(k => k.Item1).ThenBy(k => k.Item2, StringComparer.Ordinal).ThenBy(k => k.Item3).ThenBy(k => k.Item4, StringComparer.Ordinal);
  foreach (var key in expected) {
   var group = strata.GetValueOrDefault(key, Array.Empty<PolarSample>());
   int count = group.Count(a => a.CrossSection);
   polarSamplingWriter.WriteLine(string.Join(",", requestedSeed, date.ToShortString(), key.Item1, key.Item2, key.Item3, key.Item4,
    group.Length, count, group.Count(a => a.Panel), group.Count(a => a.Panel && !a.CrossSection), count == 0 ? "" : DF(group.Length / (double)count)));
  }
  return all.Where(a => a.CrossSection || a.Panel).OrderBy(a => a.Artist.artistId, StringComparer.Ordinal).ToList();
 }
 private void CaptureSampledPolarCensus() {
  if (polarCensusWriter == null) return;
  var date = TimeManager.Instance.CurrentDate;
  int month = date.year * 12 + date.month;
  if (date.year > 1963 || polarCensusMonth == month) return;
  // Opt-in observation windows skip census/checkpoint overhead outside requested months.
  // Stop and completion checkpoints remain unchanged; no simulation ticks are skipped.
  if(date.year < polarCensusFromYear || polarCensusMonths.Count>0&&!polarCensusMonths.Contains(date.month)) return;
  CheckPolarResearchStop();
  FlushPolarResearch();
  SavePolarCheckpoint("before-census");
  polarCensusMonth = month;
  var watch = Stopwatch.StartNew();
  if (polarCensusMode == "none") {
   polarMonths.Add(new PolarMonthStatus { Date = date.ToShortString(), Status = "disabled" });
   WritePolarResearchManifest(); return;
  }
  var sample = SelectPolarSample(date);
  var status = new PolarMonthStatus { Date = date.ToShortString(), Status = "inProgress", Planned = sample.Count };
  // Count the complete population from strata rather than from integer-weighted sample rows.
  status.Population = sample.Where(a => a.CrossSection).GroupBy(a => (a.Artist.primaryGenre, a.Cohort, a.Unsigned, a.WritingBin)).Sum(g => g.First().Population);
  polarMonths.Add(status); polarDiagnosticsUsed = 0;
  FlushPolarResearch(); WritePolarResearchManifest();
  long candidateEntries = 0;
  var legacyFull = new Dictionary<Genre, int[]>();
  foreach (var entry in sample) {
   CheckPolarResearchStop(); currentPolarSample = entry;
   var artist = entry.Artist;
   // LiveRepertoire lazily initializes a gameplay field. Restore it after observation.
   var priorRepertoire = artist.repertoireState;
   try {
    using var random = new RandomNumberGenerator { Seed = CensusSeed($"{requestedSeed}:{date.year}:{date.month}:{artist.artistId}") };
    if(genreRepairCensus)CaptureGenreRepairReference(artist,entry.Unsigned,date,priorRepertoire);
    var prospect = new PlayerDesk.Prospect { Artist = artist };
    int requested = 0; List<SongComposition> pool = null;
    PlayerDesk.Instance.BuildLiveSet(prospect, artist, date.year, 0, random, (want, songs) => { requested = want; pool = songs; });
    CapturePolarDiversitySet(artist, prospect, pool, requested, entry.Unsigned);
    if(genreFollowUpCensus) CaptureGenreFollowUp(artist, prospect, pool, requested, entry.Unsigned);
    int originals = prospect.LiveSet.Count(s => s.IsOriginal);
    if (polarCensusMode == "full" && entry.Unsigned) {
     if (!legacyFull.TryGetValue(artist.primaryGenre, out var counts)) legacyFull[artist.primaryGenre] = counts = new int[10];
     counts[0]++; counts[1] += prospect.LiveSet.Count; counts[2] += originals;
     if (prospect.LiveSet.Count < 3) counts[6]++;
     var covers = prospect.LiveSet.Where(s => !s.IsOriginal).ToArray();
     counts[7] += covers.Length - covers.Select(s => s.SongId).Distinct().Count();
     foreach (var item in covers) {
      var song = CompositionCatalogService.GetSong(item.SongId);
      if (song.isTraditional || song.isPublicDomain) counts[4]++;
      else if (song.isStandard) counts[3]++;
      else { counts[5]++; counts[song.originKind == SongOriginKind.RecentHit ? 8 : 9]++; }
     }
    }
    polarCoverageWriter.WriteLine(string.Join(",", requestedSeed, date.ToShortString(), artist.primaryGenre, entry.Cohort, entry.Unsigned, entry.WritingBin,
     Csv(artist.artistId), entry.Population, entry.Sample, DF(entry.Weight), entry.CrossSection, entry.Panel, requested + originals, prospect.LiveSet.Count, pool?.Count ?? 0));
    candidateEntries += pool?.Count ?? 0;
   } finally { artist.repertoireState = priorRepertoire; currentPolarSample = null; }
   status.Observed++; polarObservedActs++;
   if (status.Observed % 16 == 0) { FlushPolarResearch(); WritePolarResearchManifest(); }
  }
  // This compatibility stream retains its LEGACY categories and is emitted only for full observation.
  // Corrected provenance and weighted sample estimates come from repertoire-slots and the manifest.
  foreach (var group in legacyFull.OrderBy(g => g.Key))
   polarCensusWriter.WriteLine(string.Join(",", requestedSeed, date.ToShortString(), date.year, date.month, group.Key) + "," + string.Join(",", group.Value));
  status.Status = "complete";
  polarTimingWriter.WriteLine(string.Join(",", requestedSeed, date.ToShortString(), "census", DF(watch.Elapsed.TotalSeconds), status.Population, status.Observed, candidateEntries));
  FlushPolarResearch(); WritePolarResearchManifest();
  SavePolarCheckpoint("after-census");
  GD.Print($"POLAR_CENSUS_COMPLETE run={runName} mode={polarCensusMode} population={status.Population} observed={status.Observed} candidateEntries={candidateEntries} seconds={watch.Elapsed.TotalSeconds:F3}");
 }
}
