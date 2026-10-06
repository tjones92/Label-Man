using System;
using Godot;

public static class PolarRefusalAvoidanceChecks {
 public static void Run() {
  var desk=PlayerDesk.Instance;
  if(!desk.FoundLabel("Avoidance fixture",DistanceModel.GetCities()[0].cityId,FoundingArchetype.ExMusician,out var message))throw new Exception(message);
  var (artist,choice)=PolarPlayerPerceptionChecks.RefusalFixture(desk);
  artist.labelId=null; // Autonomous selection; preserve the fixture's nonempty repertoire.
  SongComposition refused=null,accepted=null;
  foreach(Genre genre in Enum.GetValues<Genre>()) {
   artist.primaryGenre=genre;
   foreach(var row in PolarSongTable.Current.Archetypes) {
    var song=new SongComposition { songId="avoidance-fixture",primaryGenre=Genre.TraditionalPop,
     plasticity=1,plasticityFrozen=true,demoTaxonomy=new SongTaxonomy {primaryGenre=Genre.TraditionalPop,archetype=Enum.Parse<SongArchetype>(row.Name)} };
    var key=new Record {recordId=$"live:{artist.artistId}:1960:{song.songId}",primaryGenre=genre};
    var material=SongMaterialSelectionService.BuildCoverForSong(artist,key,song,genre,1960);
    if(PolarSongBehavior.Refuses(material.PolarProposal.fit,artist,false)){refused=song;break;}
   }
   if(refused!=null)break;
  }
  if(refused==null)throw new Exception("No real autonomous refusal fixture");
  int excluded=0,unfilled=0;
  void Observe(SimulatedArtist a,Record r,string action){if(action=="excluded")excluded++;if(action=="live-unfilled")unfilled++;}
  SongMaterialSelectionService.OnRefusalAvoidance+=Observe;
  try {
   PolarSongBehavior.UsePolarFitSelection=true;
   GD.Seed(1001);uint next=GD.Randi();GD.Seed(1001);
   var slots=SongMaterialSelectionService.SelectLiveCovers(new[]{refused},artist,1960,1);
   if(slots.Count!=0||excluded!=1||unfilled!=1)throw new Exception("All-refused pool must be empty and reported");
   if(GD.Randi()!=next)throw new Exception("Avoidance touched global RNG");
   foreach(var row in PolarSongTable.Current.Archetypes){var song=new SongComposition {songId="accepted-fixture",primaryGenre=artist.primaryGenre,plasticity=1,plasticityFrozen=true,demoTaxonomy=new SongTaxonomy{primaryGenre=artist.primaryGenre,archetype=Enum.Parse<SongArchetype>(row.Name)}};
    var key=new Record{recordId=$"live:{artist.artistId}:1960:{song.songId}",primaryGenre=artist.primaryGenre};
    var material=SongMaterialSelectionService.BuildCoverForSong(artist,key,song,artist.primaryGenre,1960);
    if(!PolarSongBehavior.Refuses(material.PolarProposal.fit,artist,false)){accepted=song;break;}}
   if(accepted==null||SongMaterialSelectionService.SelectLiveCovers(new[]{refused,accepted},artist,1960,1).Count!=1)throw new Exception("Accepted alternative not selected");
   PolarSongBehavior.UsePolarFitSelection=false;
   if(SongMaterialSelectionService.SelectLiveCovers(new[]{refused},artist,1960,1).Count!=1)throw new Exception("Disabled selection changed");
   GD.Print("POLAR_REFUSAL_AVOIDANCE_PASS sharedRule=ok allRefused=unfilled acceptedAlternative=ok auditCounts=ok rng=ok disabled=ok");
  }finally{SongMaterialSelectionService.OnRefusalAvoidance-=Observe;}
 }
}
