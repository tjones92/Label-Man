public partial class ArtistManager {
	internal PopulationContinuationSaveData CapturePopulationContinuation() => new() {
		FormationAccumulator = formationAccumulator, HiringOpportunities = lastHiringOpportunities,
		SeekingProspects = lastSeekingProspects, LatentProspects = lastLatentProspects
	};
	internal void RestorePopulationContinuation(PopulationContinuationSaveData saved) {
		if (saved == null) return;
		formationAccumulator = saved.FormationAccumulator; lastHiringOpportunities = saved.HiringOpportunities;
		lastSeekingProspects = saved.SeekingProspects; lastLatentProspects = saved.LatentProspects;
	}
}
