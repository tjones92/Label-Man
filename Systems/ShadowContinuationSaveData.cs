using System.Collections.Generic;

/// <summary>Experimental full-world continuation state, written only with persisted stature shadows.</summary>
public sealed class ShadowContinuationSaveData {
	public int Version { get; set; } = 1;
	public ulong GlobalRng { get; set; }
	public int? PlayerLabelIndex { get; set; }
	public Dictionary<string, int> PlayerRecordPositions { get; set; }
	public int? PlayerOperatingRosterTarget { get; set; }
	public List<string> LabelGeneratorNames { get; set; }
	public Dictionary<string, int> MasterPickupEvidence { get; set; }
	public CityReachContinuationSaveData CityReach { get; set; }
	public Dictionary<string, Dictionary<string, float>> PendingLiveCrowds { get; set; }
	public NamingContinuationSaveData Naming { get; set; }
	public CulturalContinuationSaveData Culture { get; set; }
	public SessionContinuationSaveData Sessions { get; set; }
	public Dictionary<string, RoomBackingContinuationSaveData> RoomBacking { get; set; }
	public ProjectContinuationSaveData Projects { get; set; }
	public List<List<string>> TrackAliases { get; set; }
	public EconomyContinuationSaveData Economy { get; set; }
	public PopulationContinuationSaveData Population { get; set; }
	public LabelMomentumContinuationSaveData OffChartMomentum { get; set; }
	public LabelMomentumContinuationSaveData RecordClubMomentum { get; set; }
	public StationContinuationSaveData Radio { get; set; }
	public LocalRetailContinuationSaveData Retail { get; set; }
	public Dictionary<string, Dictionary<string, LocalListeningSaveData>> Listening { get; set; }
}

public sealed class RoomBackingContinuationSaveData {
	public float Hours { get; set; }
	public float Pay { get; set; }
}

public sealed class PopulationContinuationSaveData {
	public double FormationAccumulator { get; set; }
	public int HiringOpportunities { get; set; }
	public int SeekingProspects { get; set; }
	public int LatentProspects { get; set; }
}

public sealed class LabelMomentumContinuationSaveData {
	public Dictionary<string, double> Month { get; set; }
	public Dictionary<string, List<double>> Recent { get; set; }
	internal static LabelMomentumContinuationSaveData Capture(Dictionary<string, double> month, Dictionary<string, List<double>> recent) {
		var copy = new Dictionary<string, List<double>>(System.StringComparer.Ordinal);
		foreach (var p in recent) copy.Add(p.Key, new(p.Value));
		return new() { Month = new(month, System.StringComparer.Ordinal), Recent = copy };
	}
	internal static void Restore(LabelMomentumContinuationSaveData saved, Dictionary<string, double> month, Dictionary<string, List<double>> recent) {
		month.Clear(); recent.Clear();
		if (saved == null) return;
		foreach (var p in saved.Month) month.Add(p.Key, p.Value);
		foreach (var p in saved.Recent) recent.Add(p.Key, new(p.Value));
	}
}

public sealed class EconomyContinuationSaveData {
	public int FormatYear { get; set; }
	public int FormatDecisions { get; set; }
	public int AlbumProjectsScheduled { get; set; }
	public int SoundtrackBlockbusters { get; set; }
	public int SoundtrackOriginations { get; set; }
}

public sealed class ProjectContinuationSaveData {
	public long Counter { get; set; }
	public Dictionary<string, ProjectRecordLinksSaveData> Records { get; set; }
}

public sealed class ProjectRecordLinksSaveData {
	public bool AlbumIsLive { get; set; }
	public bool PromoIsLive { get; set; }
}

public sealed class CulturalContinuationSaveData {
	public long Sequence { get; set; }
	public CulturalEvent[] Ledger { get; set; }
	public Dictionary<string, int> InfluenceCounts { get; set; }
	public Dictionary<string, CulturalMemoryService.LabelMemory> Labels { get; set; }
}

public sealed class SessionContinuationSaveData {
	public int LoadWeek { get; set; }
	public Dictionary<string, int> WeekLoad { get; set; }
	public int CommunityYear { get; set; }
	public Dictionary<string, List<string>> Communities { get; set; }
}

public sealed class NamingContinuationSaveData {
	public LabelMan.Naming.DeterministicRandomState Main { get; set; }
	public LabelMan.Naming.DeterministicRandomState Acts { get; set; }
	public LabelMan.Naming.DeterministicRandomState Titles { get; set; }
	public LabelMan.Naming.DeterministicRandomState Albums { get; set; }
	public LabelMan.Naming.DeterministicRandomState Labels { get; set; }
	public LabelMan.Naming.NameUniquenessState Uniqueness { get; set; }
}

public sealed class CityReachContinuationSaveData {
	public List<string> PlaceIds { get; set; }
	public Dictionary<string, CityReachRecordSaveData> Records { get; set; }
}

public sealed class CityReachRecordSaveData {
	public ulong Reached { get; set; }
	public float[] Exposure { get; set; }
}

public sealed class StationContinuationSaveData {
	public int Year { get; set; }
	public ulong Rng { get; set; }
	public ulong ShadowRng { get; set; }
	public List<RadioStation> Stations { get; set; }
	public List<Deejay> Deejays { get; set; }
	public Dictionary<string, StationRuntime> ShadowRuntimes { get; set; }
	public List<ResponseEvidenceSaveData> ResponseNow { get; set; }
	public List<ResponseTrialSaveData> ResponseTrials { get; set; }
}

public sealed class ResponseEvidenceSaveData {
	public string Record { get; set; }
	public string Region { get; set; }
	public StationNetwork.ResponseEvidence Evidence { get; set; }
}

public sealed class ResponseTrialSaveData {
	public string Station { get; set; }
	public string Record { get; set; }
	public StationNetwork.ResponseTrial Trial { get; set; }
}

public sealed class LocalRetailContinuationSaveData {
	public Dictionary<string, double> NationalMass { get; set; }
	public Dictionary<string, double> LocalMass { get; set; }
	public Dictionary<string, float> Scales { get; set; }
	public int Weeks { get; set; }
	public bool Seeded { get; set; }
}

public sealed class LocalListeningSaveData {
	public int Age { get; set; }
	public float Listening { get; set; }
}
