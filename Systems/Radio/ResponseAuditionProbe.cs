using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Fresh-world mechanism screen. Regional evidence is a proxy, never attributed
/// to an individual station. No writes to taste, demand, exposure ages or lifecycle clocks.</summary>
public static class ResponseAuditionProbe {
	public static string Mode { get; private set; }
	public static bool Enabled => Mode != null;
	public static StreamWriter Writer { get; private set; }
	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		var flags = args.Where(a => a.StartsWith("--response-auditions=")).ToArray();
		if (flags.Length == 0) return;
		if (flags.Length != 1) throw new ArgumentException("One response-auditions mode required");
		Mode = flags[0].Split('=')[1];
		if (Mode != "random" && Mode != "response") throw new ArgumentException("response-auditions takes random or response");
		if (!StationRegionalSupport.Enabled || StationRegionalSupport.ChartReadsNational || StationPicks.Extra)
			throw new ArgumentException("Response auditions require live regional support and reserved picks");
		if (args.Any(a => a.StartsWith("--resume-world") || a.StartsWith("--save-world")))
			throw new ArgumentException("Response auditions require fresh uninterrupted worlds");
		string run = args.FirstOrDefault(a => a.StartsWith("--run="))?.Substring(6);
		if (string.IsNullOrEmpty(run) || Path.GetFileName(run) != run) throw new ArgumentException("Plain run name required");
		Writer = new StreamWriter(Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-response-auditions.jsonl")) { AutoFlush = true };
	}
	// Units per million population per rotation-equivalent, shrunk toward zero by 100 purchases.
	// .02 exposure floor and threshold 100 are screening assumptions, not measured conversion rates.
	public static float Response(int units, float populationMillions, float radio) =>
		units <= 0 || populationMillions <= 0 ? 0 : units / (populationMillions * (Math.Max(0, radio) + .02f)) * units / (units + 100f);
}

public sealed partial class StationNetwork {
	private sealed class ResponseEvidence {
		public int Week, Streak, Units, Backorders;
		public float Radio, Panel, Score;
		public string Region;
	}
	private sealed class ResponseTrial { public int Start; public bool Active; }
	private Dictionary<(string, string), ResponseEvidence> responsePrevious = new(), responseNow = new();
	private readonly Dictionary<(string, string), ResponseTrial> responseTrials = new();
	private readonly Dictionary<(string, string), ResponseEvidence> responseOffers = new();
	private void PrepareResponseAuditions(IReadOnlyList<RecordRuntimeData> records, MarketRegion[] regions, int week) {
		if (!ResponseAuditionProbe.Enabled) return;
		responsePrevious = responseNow; responseNow = new(); responseOffers.Clear();
		var reports = responsePrevious.Where(p => p.Value.Week == week - 1 && p.Value.Streak >= 2)
			.GroupBy(p => p.Key.Item1).ToDictionary(g => g.Key, g => g.Select(p => p.Value).ToList());
		foreach (var r in records) {
			if (!SingleCommercialFit.Applies(r) || r.baseRecord.format == ReleaseFormat.Album) continue;
			string id = r.baseRecord.recordId;
			foreach (var region in regions) {
				if (!r.regionalData.TryGetValue(region.regionId, out var d)) continue;
				float panel = ListenerRadioPlay(id, region.regionId);
				// Read all preceding playlists before any station is mutated this week.
				bool ordinary = stationsByRegion.TryGetValue(region.regionId, out var roster) && roster.Any(s =>
					s.rt?.TierOf(id) != SpinTier.None && s.rt != null && !s.rt.activeTrials.Contains(id)
					&& !(responseTrials.TryGetValue((s.stationId, id), out var trial) && trial.Active));
				responsePrevious.TryGetValue((id, region.regionId), out var prior);
				float radio = Math.Max(panel, d.salesRegionalRadioPlayThisWeek);
				responseNow[(id, region.regionId)] = new ResponseEvidence { Week = week, Region = region.regionId,
					Units = d.unitsSoldThisWeek, Backorders = d.unitsBackordered, Panel = panel, Radio = radio,
					Score = ResponseAuditionProbe.Response(d.unitsSoldThisWeek, region.population, radio),
					Streak = ordinary && panel > 0 && d.unitsSoldThisWeek >= 20 ? (prior?.Week == week - 1 ? prior.Streak : 0) + 1 : 0 };
			}
			// Reports are one meeting old, with two completed ordinary-support observations.
			if (!reports.TryGetValue(id, out var sources)) continue;
			var destinations = stationsByRegion.Values.SelectMany(x => x).Where(s => s.rt != null && s.rt.TierOf(id) == SpinTier.None
				&& !responseTrials.ContainsKey((s.stationId, id)) && sources.Any(e => e.Region != s.regionId)
				&& FormatAdmittance(r.baseRecord.primaryGenre, s, currentYear, regions.First(x => x.regionId == s.regionId).currentIntegration) > 0)
				.OrderBy(s => PhiloxCounterRandom.Unit(BandLife.WorldSeed, id, s.stationId, week, 0x52415010U)).ThenBy(s => s.stationId).Take(2);
			foreach (var s in destinations) {
				// Identical eligible report pool in both arms; only ranking at the destination differs.
				var source = sources.Where(e => e.Region != s.regionId).OrderBy(e => e.Region, StringComparer.Ordinal).First();
				responseOffers[(s.stationId, id)] = source;
			}
		}
	}
	private void ApplyResponseAudition(RadioStation station, Deejay dj, RecordRuntimeData record, PlaylistMeeting.Candidate c, float formatMatch, int week) {
		if (!ResponseAuditionProbe.Enabled) return;
		var key = (station.stationId, c.Id);
		responseNow.TryGetValue((c.Id, station.regionId), out var local);
		responsePrevious.TryGetValue((c.Id, station.regionId), out var previous);
		if (responseTrials.TryGetValue(key, out var trial)) {
			c.ResearchHistory = true; c.ResearchActive = trial.Active; c.ResearchStart = trial.Start;
			// Two sales passes after admission; local evidence alone gates AND scores retention.
			c.ResearchAccepted = week - trial.Start >= 2 && local?.Panel > 0 && local.Units >= 20 && local.Score >= 100
				&& previous?.Week == week - 1 && previous.Panel > 0 && previous.Units >= 20 && previous.Score >= 100;
			c.Score = c.ResearchAccepted ? formatMatch * Math.Min(2f, local.Score / 1000f) : 0;
		}
		if (!c.ResearchHistory && responseOffers.TryGetValue(key, out var source)) {
			var label = ChartManager.Instance?.GetLabelById(record.baseRecord.labelId);
			bool allowed = !(StationPicks.TastemakerIndie && dj?.archetype == DJArchetype.Tastemaker && label?.tier == LabelTier.Major);
			c.ResearchOffer = allowed && c.Prior == SpinTier.None;
			c.ResearchScore = ResponseAuditionProbe.Mode == "response" ? source.Score
				: (float)PhiloxCounterRandom.Unit(BandLife.WorldSeed, c.Id, station.stationId, week, 0x52415012U);
		}
	}
	private void FinishResponseAuditions(RadioStation station, PlaylistMeeting.Input meeting, PlaylistMeeting.Result result) {
		if (!ResponseAuditionProbe.Enabled) return;
		foreach (var c in meeting.Candidates) {
			var key = (station.stationId, c.Id);
			if (!responseOffers.ContainsKey(key) && !c.ResearchHistory) continue;
			var seat = result.Seats.FirstOrDefault(s => s.Id == c.Id);
			responseOffers.TryGetValue(key, out var source);
			responseNow.TryGetValue((c.Id, station.regionId), out var local);
			ResponseAuditionProbe.Writer.WriteLine(JsonSerializer.Serialize(new {
				week = meeting.Week, station = station.stationId, record = c.Id, region = station.regionId,
				sourceRegion = source?.Region, sourceWeek = source?.Week, sourceScore = source?.Score,
				localScore = local?.Score, localUnits = local?.Units, localPanel = local?.Panel, backorders = local?.Backorders,
				offer = c.ResearchOffer, active = c.ResearchActive, history = c.ResearchHistory, accepted = c.ResearchAccepted,
				start = c.ResearchStart, rejection = c.Rejection ?? result.Rejections.GetValueOrDefault(c.Id), seat = seat?.Source,
				connected = SingleCommercialFit.Connected(factorById[c.Id].rec, ChartManager.Instance?.GetLabelById(factorById[c.Id].labelId)) }));
			if (seat?.Source == "research-trial") {
				if (!responseTrials.TryGetValue(key, out var trial)) responseTrials[key] = trial = new ResponseTrial { Start = meeting.Week };
				trial.Active = true;
			} else if (responseTrials.TryGetValue(key, out var old)) old.Active = false;
		}
	}
}
