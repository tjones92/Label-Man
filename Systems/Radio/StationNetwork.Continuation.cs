using System.Collections.Generic;
using System.Linq;

public sealed partial class StationNetwork {
	internal StationContinuationSaveData CaptureContinuation() => new() {
		Year = currentYear, Rng = rng.State, ShadowRng = shadowRng.State,
		Stations = AllStations().ToList(), Deejays = djsById.Values.ToList(),
		ShadowRuntimes = new(shadowRuntimes, System.StringComparer.Ordinal),
		ResponseNow = responseNow.Select(p => new ResponseEvidenceSaveData { Record = p.Key.Item1, Region = p.Key.Item2, Evidence = p.Value }).ToList(),
		ResponseTrials = responseTrials.Select(p => new ResponseTrialSaveData { Station = p.Key.Item1, Record = p.Key.Item2, Trial = p.Value }).ToList()
	};

	internal void RestoreContinuation(StationContinuationSaveData saved, IEnumerable<MarketRegion> regions) {
		stationsByRegion.Clear(); stationsById.Clear(); djsById.Clear(); shadowRuntimes.Clear();
		currentYear = saved.Year;
		foreach (var s in saved.Stations) {
			if (!stationsByRegion.TryGetValue(s.regionId, out var list)) stationsByRegion[s.regionId] = list = new();
			list.Add(s); stationsById.Add(s.stationId, s);
		}
		foreach (var dj in saved.Deejays) djsById.Add(dj.djId, dj);
		foreach (var p in saved.ShadowRuntimes) shadowRuntimes.Add(p.Key, p.Value);
		responsePrevious.Clear(); responseNow.Clear(); responseOffers.Clear(); responseTrials.Clear();
		foreach (var p in saved.ResponseNow) responseNow.Add((p.Record, p.Region), p.Evidence);
		foreach (var p in saved.ResponseTrials) responseTrials.Add((p.Station, p.Record), p.Trial);
		var regionList = regions.ToList();
		ComputeNationalFormatMeans(regionList, currentYear);
		tailFormatReachByRegion.Clear();
		foreach (var region in regionList) RebuildTailFormatReach(region, currentYear);
		rng.State = saved.Rng; shadowRng.State = saved.ShadowRng;
	}
}
