using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PolarGospelRepairChecks {
 public static void Run() {
  var table=PolarSongTable.RepairTable;
  table.ValidatePriorCenters();
  var gospel=table.Prior(Genre.Gospel).Identity;
  Check(Math.Abs(gospel[0]-.35f)<1e-6,"Gospel authored center");
  float original=gospel[0];gospel[0]+=.01f;
  bool rejected=false;try{table.ValidatePriorCenters();}catch(InvalidOperationException){rejected=true;}finally{gospel[0]=original;}
  Check(rejected,"divergent cached prior rejected");
  var raw=new MarketTasteSnapshot {identity=new[]{.9f,.2f,.1f,.8f},observations=1,asOfWeek=4};
  var shrunk=PolarSongBehavior.ShrinkTaste(raw,Genre.Gospel,table);
  Check(Math.Abs(shrunk.identity[0]-(.9f+4*.35f)/5)<1e-6 && raw.identity[0]==.9f,"one observation formula and input isolation");
  Check(PolarSongBehavior.ShrinkTaste(null,Genre.Gospel,table)==null,"startup constant");
  var act=new ActProfile();var profile=new SongProfile();
  Check(!PolarMaterialFit.Evaluate(profile,profile,act,shrunk,4,table).HasMarketEvidence && PolarMaterialFit.Evaluate(profile,profile,act,shrunk,5,table).HasMarketEvidence,"as-of-week retained");
  raw.observationSchemaVersion=1;raw.observedRecordIds=new[]{"single1","single2"};raw.legacyObservationCount=1;
  var saved=JsonSerializer.Deserialize<MarketTasteSnapshot>(JsonSerializer.Serialize(raw,SaveGameService.TestJsonOptions),SaveGameService.TestJsonOptions);
  Check(saved.observedRecordIds.SequenceEqual(raw.observedRecordIds) && saved.legacyObservationCount==1,"observation evidence save/load");
  Check(Math.Abs(PolarSongBehavior.ShrinkTaste(saved,Genre.Gospel,table).identity[0]-(2*.9f+4*.35f)/6)<1e-6,"legacy count floor avoids double counting");
  var previousTaste=PolarSongBehavior.CaptureTaste();bool enabled=PolarSongBehavior.UsePolarFitSelection;
  try {
   PolarSongBehavior.UsePolarFitSelection=true;PolarSongBehavior.ResetTaste();
   var song=CompositionCatalogService.AllSongs.First();
   var single=new RecordRuntimeData {baseRecord=new Record {recordId="repair-hit-1",songId=song.songId,primaryGenre=Genre.Gospel,format=ReleaseFormat.Single}};
   PolarSongBehavior.ObserveCompletedWeek(new[]{single},1);
   var first=PolarSongBehavior.CaptureTaste()[Genre.Gospel];
   PolarSongBehavior.ObserveCompletedWeek(new[]{single},2);
   var repeated=PolarSongBehavior.CaptureTaste()[Genre.Gospel];
   Check(repeated.observedRecordIds.Length==1 && repeated.asOfWeek==2 && repeated.identity.SequenceEqual(first.identity),"repeat chart single keeps count and drift");
   var second=new RecordRuntimeData {baseRecord=new Record {recordId="repair-hit-2",songId=song.songId,primaryGenre=Genre.Gospel,format=ReleaseFormat.Single}};
   PolarSongBehavior.ObserveCompletedWeek(new[]{single,second},3);
   Check(PolarSongBehavior.CaptureTaste()[Genre.Gospel].observedRecordIds.Length==2,"new chart single accumulates evidence");
  } finally {PolarSongBehavior.RestoreTaste(previousTaste);PolarSongBehavior.UsePolarFitSelection=enabled;}
  var artist=new SimulatedArtist {primaryGenre=Genre.Gospel,members=new(){new Musician {isActive=true,temperament=.2f,musicalVersatility=.7f,reliability=.8f}}};
  var derived=PolarActProfileDeriver.Derive(artist,null,null,table);
  Check(Math.Abs(derived[SongAxis.Toughness]-(.35f+(.5f-.2f)*table.N("traitIdentityAdjustment")))<1e-6,"performer offset preserved");
  GD.Print("POLAR_GOSPEL_REPAIR_CHECK_PASS centers=51 divergence=checked shrinkage=checked persistence=checked startup=checked asOfWeek=checked offsets=checked");
 }
 private static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException("POLAR_GOSPEL_REPAIR_CHECK_FAIL: "+message);}
}
