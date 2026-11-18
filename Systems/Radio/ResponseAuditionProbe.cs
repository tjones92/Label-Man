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
	public static bool DjTips { get; private set; }
	public static bool TipsAcrossGeography { get; private set; }
	public static bool Enabled => Mode != null;
	public static StreamWriter Writer { get; private set; }
	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		var flags = args.Where(a => a.StartsWith("--response-auditions=")).ToArray();
		var tips = args.Where(a => a == "--dj-word-of-mouth" || a.StartsWith("--dj-word-of-mouth=", StringComparison.Ordinal)).ToArray();
		if (tips.Length > 1 || tips.Length == 1 && tips[0] is not ("--dj-word-of-mouth" or "--dj-word-of-mouth=serviced"))
			throw new ArgumentException("Use --dj-word-of-mouth or --dj-word-of-mouth=serviced for the geographic-veto control.");
		DjTips = tips.Length == 1 && flags.Contains("--response-auditions=response");
		TipsAcrossGeography = DjTips && tips[0] == "--dj-word-of-mouth";
		if (flags.Length == 0) return;
		if (flags.Length != 1) throw new ArgumentException("One response-auditions mode required");
		Mode = flags[0].Split('=')[1];
		if (Mode != "random" && Mode != "response") throw new ArgumentException("response-auditions takes random or response");
		if (!StationRegionalSupport.Enabled || StationRegionalSupport.ChartReadsNational || StationPicks.Extra)
			throw new ArgumentException("Response auditions require live regional support and reserved picks");
		// The mechanism runs everywhere (it is in the default profile). Only the screen's audition log needs a fresh,
		// uninterrupted world with a plain run name: trial state is runtime-only, so a resumed or loaded world starts
		// its trials empty, as the station panel's runtime does on load.
		string run = args.FirstOrDefault(a => a.StartsWith("--run="))?.Substring(6);
		bool interrupted = args.Any(a => a.StartsWith("--resume-world") || a.StartsWith("--save-world"));
		if (string.IsNullOrEmpty(run) || interrupted) return;
		if (Path.GetFileName(run) != run) throw new ArgumentException("Plain run name required");
		Writer = new StreamWriter(Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-response-auditions.jsonl")) { AutoFlush = true };
	}
	// Units per million population per rotation-equivalent, shrunk toward zero by 100 purchases.
	// .02 exposure floor and threshold 100 are screening assumptions, not measured conversion rates.
	public static float Response(int units, float populationMillions, float radio) =>
		units <= 0 || populationMillions <= 0 ? 0 : units / (populationMillions * (Math.Max(0, radio) + .02f)) * units / (units + 100f);
}

public sealed partial class StationNetwork {
	public sealed class ResponseEvidence {
		public int Week, Streak, Units, Backorders;
		public float Radio, Panel, Score;
		public string Region, SourceStation;
		[System.Text.Json.Serialization.JsonIgnore] public int ShelfBeforeMeetings, PendingBeforeMeetings;
		[System.Text.Json.Serialization.JsonIgnore] public bool HeldBeforeMeetings;
	}
	public sealed class ResponseTrial { public int Start; public bool Active; }
	private Dictionary<(string, string), ResponseEvidence> responsePrevious = new(), responseNow = new();
	private readonly Dictionary<(string, string), ResponseTrial> responseTrials = new();
	private readonly Dictionary<(string, string), ResponseEvidence> responseOffers = new();
	private readonly Dictionary<string, RadioStation[]> tipContacts = new();
	private int contactYear = -1;
	private void PrepareResponseAuditions(IReadOnlyList<RecordRuntimeData> records, MarketRegion[] regions, int week) {
		if (!ResponseAuditionProbe.Enabled) return;
		responsePrevious = responseNow; responseNow = new(); responseOffers.Clear();
		var reports = responsePrevious.Where(p => p.Value.Week == week - 1 && p.Value.Streak >= 2)
			.GroupBy(p => p.Key.Item1).ToDictionary(g => g.Key, g => g.Select(p => p.Value).ToList());
		foreach (var r in records) {
			if (r?.baseRecord == null || r.baseRecord.format == ReleaseFormat.Album) continue;
			// A DJ can hear a player's current single on the same terms as a rival's.
			if (ResponseAuditionProbe.DjTips ? r.baseRecord.releaseDate <= GameDate.StartDate : !SingleCommercialFit.Applies(r)) continue;
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
				var observation = new ResponseEvidence { Week = week, Region = region.regionId,
					Units = d.unitsSoldThisWeek, Backorders = d.unitsBackordered, Panel = panel, Radio = radio,
					Score = ResponseAuditionProbe.Response(d.unitsSoldThisWeek, region.EconomicPopulation, radio),
					Streak = ordinary && panel > 0 && d.unitsSoldThisWeek >= 20 ? (prior?.Week == week - 1 ? prior.Streak : 0) + 1 : 0 };
				responseNow[(id, region.regionId)] = observation;
				if (ResponseAuditionProbe.DjTips && ResponseAuditionProbe.Writer != null) {
					observation.ShelfBeforeMeetings = d.unitsInStores;
					observation.PendingBeforeMeetings = d.pendingLaunchStock;
					observation.HeldBeforeMeetings = d.awaitingArrival;
				}
				if (ResponseAuditionProbe.DjTips && observation.Streak >= 2) {
					// Identify a witness from the completed playlists. A probationary spin cannot generate a tip.
					var witness = roster?.Where(s => s.rt != null && s.rt.sustainedWeeks.GetValueOrDefault(id) >= 2
						&& !s.rt.activeTrials.Contains(id) && responseTrials.GetValueOrDefault((s.stationId, id))?.Active != true)
						.OrderByDescending(s => s.rt.sustainedWeeks[id]).ThenBy(s => s.stationId, StringComparer.Ordinal).FirstOrDefault();
					observation.SourceStation = witness?.stationId;
				}
			}
			// Reports are one meeting old, with two completed ordinary-support observations.
			if (ResponseAuditionProbe.DjTips || !reports.TryGetValue(id, out var sources)) continue;
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
		if (ResponseAuditionProbe.DjTips) RouteDjTips(reports, regions, week);
	}

	// A working contact book: one other city in this region, one neighboring market,
	// and one longer-distance format peer. Ties persist across records. A report carries
	// the source market's measured response; it never conveys a forecast of national fit.
	private void RouteDjTips(Dictionary<string, List<ResponseEvidence>> reports, MarketRegion[] regions, int week) {
		if (contactYear != currentYear) {
			tipContacts.Clear();
			foreach (var sender in AllStations()) {
				var neighbors = ChartManager.GetNeighborRegionIds(sender.regionId);
				var peers = AllStations().Where(s => s.cityName != sender.cityName)
					.OrderBy(s => PhiloxCounterRandom.Unit(BandLife.WorldSeed, sender.stationId, s.stationId, 0, 0x52415020U))
					.ThenBy(s => s.stationId, StringComparer.Ordinal).ToList();
				tipContacts[sender.stationId] = new[] {
					peers.FirstOrDefault(s => s.regionId == sender.regionId),
					peers.FirstOrDefault(s => neighbors.Contains(s.regionId)),
					peers.FirstOrDefault(s => s.format == sender.format && s.regionId != sender.regionId && !neighbors.Contains(s.regionId))
				}.Where(s => s != null).ToArray();
			}
			contactYear = currentYear;
		}
		var integration = regions.ToDictionary(r => r.regionId, r => r.currentIntegration);
		var calls = new List<(string Record, string Destination, ResponseEvidence Source, double Tie)>();
		foreach (var (id, observations) in reports) {
			if (!factorById.TryGetValue(id, out var f) || !f.inCirculation) continue;
			foreach (var source in observations) {
				if (source.SourceStation == null || !tipContacts.TryGetValue(source.SourceStation, out var peers)) continue;
				foreach (var peer in peers) {
					var key = (peer.stationId, id);
					if (peer.rt == null || peer.rt.TierOf(id) != SpinTier.None || peer.rt.heardDjTips.Contains(id) || responseTrials.ContainsKey(key)
						|| FormatAdmittance(f.rec.baseRecord.primaryGenre, peer, currentYear, integration[peer.regionId]) <= 0) continue;
					double tie = PhiloxCounterRandom.Unit(BandLife.WorldSeed, id, peer.stationId, week, 0x52415021U);
					calls.Add((id, peer.stationId, source, tie));
				}
			}
		}
		var perSender = new Dictionary<string, int>();
		foreach (var call in calls.OrderByDescending(c => c.Source.Score).ThenBy(c => c.Tie)
				.ThenBy(c => c.Record, StringComparer.Ordinal).ThenBy(c => c.Destination, StringComparer.Ordinal)) {
			string sender = call.Source.SourceStation;
			var key = (call.Destination, call.Record);
			if (perSender.GetValueOrDefault(sender) >= 2 ||
				!GetStation(call.Destination).rt.heardDjTips.Add(call.Record)) continue;
			responseOffers[key] = call.Source;
			perSender[sender] = perSender.GetValueOrDefault(sender) + 1;
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
			bool? retailUnchanged = null;
			if (ResponseAuditionProbe.Writer != null && source?.SourceStation != null && seat?.Source == "research-trial") {
				var shelf = factorById[c.Id].rec.regionalData[station.regionId];
				retailUnchanged = local != null && shelf.unitsInStores == local.ShelfBeforeMeetings &&
					shelf.pendingLaunchStock == local.PendingBeforeMeetings && shelf.awaitingArrival == local.HeldBeforeMeetings;
			}
			ResponseAuditionProbe.Writer?.WriteLine(JsonSerializer.Serialize(new {
				week = meeting.Week, station = station.stationId, record = c.Id, region = station.regionId,
				sourceRegion = source?.Region, sourceStation = source?.SourceStation, sourceWeek = source?.Week, sourceScore = source?.Score,
				geography = c.Rejection is "awaiting-arrival" or "city-not-reached" ? c.Rejection : null,
				tipMayAudition = c.TipMayAudition, retailUnchanged,
				localScore = local?.Score, localUnits = local?.Units, localPanel = local?.Panel, backorders = local?.Backorders,
				offer = c.ResearchOffer, active = c.ResearchActive, history = c.ResearchHistory, accepted = c.ResearchAccepted,
				start = c.ResearchStart, rejection = c.TipMayAudition && seat?.Source == "research-trial" ? null :
					c.Rejection ?? result.Rejections.GetValueOrDefault(c.Id), seat = seat?.Source,
				connected = SingleCommercialFit.Connected(factorById[c.Id].rec, ChartManager.Instance?.GetLabelById(factorById[c.Id].labelId)) }));
			if (seat?.Source == "research-trial") {
				if (!responseTrials.TryGetValue(key, out var trial)) responseTrials[key] = trial = new ResponseTrial { Start = meeting.Week };
				trial.Active = true;
			} else if (responseTrials.TryGetValue(key, out var old)) old.Active = false;
		}
	}
}
