using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
 private StreamWriter polarAssignmentWriter, polarAvoidanceWriter;
 private void OpenPolarFollowup() {
  if(!OS.GetCmdlineUserArgs().Contains("--polar-followup-audit"))return;
  string dir=ProjectSettings.GlobalizePath("res://SimLogs");
  polarAssignmentWriter=CreateWriter(Path.Combine(dir,runName+"-polar-master-assignments.csv"));
  polarAssignmentWriter.WriteLine("seed,enabled,date,year,actGenre,projectGenre,artistId,plannedId,kind,category,songId,capability,identity,stretch,strongRefusal,source,provenanceCategory");
  polarAvoidanceWriter=CreateWriter(Path.Combine(dir,runName+"-polar-refusal-avoidance.csv"));
  polarAvoidanceWriter.WriteLine("seed,enabled,date,year,genre,artistId,plannedId,event");
  SongMaterialSelectionService.OnMasterAssignment+=WritePolarAssignment;
  SongMaterialSelectionService.OnRefusalAvoidance+=WritePolarAvoidance;
 }
 private void WritePolarAssignment(SimulatedArtist artist,Record record,SelectedSongMaterial material,string kind) {
  if(material?.Song==null||artist.isPlayerOwned)return;
  var song=material.Song; var fit=material.PolarProposal?.fit;
  string category=!material.IsCover?"original":song.isTraditional||song.isPublicDomain?"traditional":song.isStandard?"standard":material.Source==SongMaterialSource.CoverRecentHit?"recentHit":"ordinaryCover";
  bool empty=PlayerDesk.Instance.RepertoireFor(artist.artistId).Count==0&&!PlayerDesk.Instance.SongsFor(artist.artistId).Any();
  var date=TimeManager.Instance.CurrentDate;
  polarAssignmentWriter.WriteLine(string.Join(",",requestedSeed,PolarSongBehavior.UsePolarFitSelection,date.ToShortString(),date.year,artist.primaryGenre,record.primaryGenre,Csv(artist.artistId),Csv(record.recordId),kind,category,Csv(song.songId),
   fit.HasValue?fit.Value.Capability.ToString("R",CultureInfo.InvariantCulture):"",fit.HasValue?fit.Value.Identity.ToString("R",CultureInfo.InvariantCulture):"",fit.HasValue?fit.Value.Stretch.ToString("R",CultureInfo.InvariantCulture):"",fit.HasValue&&PolarSongBehavior.Refuses(fit.Value,artist,empty),material.Source,RepertoireProvenance.Category(song,date.year,!material.IsCover)));
 }
 private void WritePolarAvoidance(SimulatedArtist artist,Record record,string action) {
  var date=TimeManager.Instance.CurrentDate;
  polarAvoidanceWriter.WriteLine(string.Join(",",requestedSeed,PolarSongBehavior.UsePolarFitSelection,date.ToShortString(),date.year,artist.primaryGenre,Csv(artist.artistId),Csv(record.recordId),action));
 }
 private void ClosePolarFollowup() {
  SongMaterialSelectionService.OnMasterAssignment-=WritePolarAssignment;SongMaterialSelectionService.OnRefusalAvoidance-=WritePolarAvoidance;
  polarAssignmentWriter?.Dispose();polarAvoidanceWriter?.Dispose();polarAssignmentWriter=polarAvoidanceWriter=null;
 }
}
