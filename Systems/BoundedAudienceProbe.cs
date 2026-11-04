using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

/// <summary>Session-only consumer replacement for fresh-world research. Fan demand
/// remains in ArtistFanbase; only its anonymous partition is replaced here.</summary>
public static class BoundedAudienceProbe {
	public static bool Enabled { get; private set; }
	public static bool Sharing { get; private set; }
	private static StreamWriter writer;
	private static int week;
	private sealed class Memory {
		public BoundedAudienceState State;
		public RecordRuntimeData Record;
		public string Region;
		public int PreparedWeek = -1;
		public double FanIntent, TotalIntent, Radio, Peer, External, Rate, Fit, LegacyRaw, FanCapacity;
		public bool Connected;
	}
	private static readonly Dictionary<RegionalRecordData, Memory> memories = new();
	private static readonly Dictionary<string, double> panelTotals = new(), enthusiastTotals = new();
	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		var flags = args.Where(a => a.StartsWith("--bounded-audience=")).ToArray();
		if (flags.Length == 0) return;
		if (flags.Length != 1 || flags[0] != "--bounded-audience=radio" && flags[0] != "--bounded-audience=sharing")
			throw new ArgumentException("bounded-audience takes radio or sharing, once");
		Enabled = true; Sharing = flags[0].EndsWith("=sharing");
		if (args.Any(a => a.StartsWith("--resume-world") || a.StartsWith("--save-world"))
			|| LocalRadioConsumptionProbe.Enabled || StationRegionalSupport.ChartReadsNational)
			throw new ArgumentException("Bounded audience requires fresh worlds, live playlists, and no other consumer treatment");
		string run = args.FirstOrDefault(a => a.StartsWith("--run="))?.Substring(6);
		if (string.IsNullOrEmpty(run) || Path.GetFileName(run) != run) throw new ArgumentException("Plain run name required");
		writer = new StreamWriter(Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-bounded-audience.csv")) { AutoFlush = true };
		writer.WriteLine("week,recordId,region,age,capacity,fanCapacity,beforeInterest,beforeOwners,beforeEnthusiasts,radioContacts,peerContacts,externalContacts,acquired,newEnthusiasts,rate,fit,connected,legacyRaw,anonymousIntent,fanIntent,totalIntent,cleared,fanCleared,anonymousCleared,owners,interested,enthusiasts,fanOwners");
	}
	private static bool Applies(RecordRuntimeData record) => Enabled && SingleCommercialFit.Applies(record)
		&& record.baseRecord.format == ReleaseFormat.Single;
	public static void PrepareWeek(IEnumerable<RecordRuntimeData> records, IEnumerable<MarketRegion> regions, int currentWeek) {
		if (!Enabled) return;
		week = currentWeek; panelTotals.Clear(); enthusiastTotals.Clear();
		foreach (var region in regions) {
			double panel = 0, enthusiasts = 0;
			foreach (var r in records) {
				if (r.baseRecord.format == ReleaseFormat.Album) continue;
				panel += ChartManager.Instance.ListenerAirplay(r.baseRecord.recordId, region.regionId);
				if (r.regionalData.TryGetValue(region.regionId, out var data) && memories.TryGetValue(data, out var m))
					enthusiasts += m.State.Enthusiasts;
			}
			panelTotals[region.regionId] = panel;
			enthusiastTotals[region.regionId] = enthusiasts;
		}
	}
	public static float Demand(RecordRuntimeData record, RegionalRecordData data, MarketRegion region, AILabel label, float original) {
		if (!Applies(record)) return original;
		FanReleaseRegion fan = null;
		if (ArtistFanbase.Enabled) record.fanAudience?.regions.TryGetValue(region.regionId, out fan);
		double initialFans = ArtistFanbase.TrialRate > 0 ? fan?.initial.Values.Sum() ?? 0 : 0;
		double population = data.demandPotentialAudience;
		double fanFraction = Math.Clamp(initialFans / Math.Max(1, region.GetRecordBuyingPopulation()), 0, 1);
		if (!memories.TryGetValue(data, out var m)) {
			if (data.unitsSoldTotal != 0) throw new InvalidOperationException("Bounded audience must start before title purchases");
			memories[data] = m = new Memory { Record = record, Region = region.regionId, FanCapacity = initialFans,
				State = new BoundedAudienceState(population * (1 - fanFraction), population > 0 ? data.demandAwareBuyers / population : 0) };
		}
		if (m.PreparedWeek == week) throw new InvalidOperationException("Duplicate bounded audience demand");
		m.PreparedWeek = week; m.LegacyRaw = original;
		m.Radio = 2 * ChartManager.Instance.ListenerAirplay(record.baseRecord.recordId, region.regionId) / Math.Max(1, panelTotals[region.regionId]);
		m.Peer = Sharing ? 2 * m.State.Enthusiasts / Math.Max(region.GetRecordBuyingPopulation(), enthusiastTotals[region.regionId]) : 0;
		m.External = .02 * Math.Clamp(data.demandAwareBuyers / Math.Max(1, population), 0, 1);
		m.Rate = Math.Max(0, data.demandIntrinsicConversionRate);
		m.Fit = SingleCommercialFit.Multiplier(record, region.regionId, label);
		m.Connected = SingleCommercialFit.Connected(record, label);
		double enjoyment = Math.Pow(Math.Clamp(record.GetQuality(), 0, 1), 2);
		double anonymous = m.State.Prepare(m.Radio + m.Peer, m.External, enjoyment, m.Rate);
		m.FanIntent = fan?.fanRaw ?? 0;
		if (fan != null) fan.anonymousRaw = anonymous;
		m.TotalIntent = anonymous + m.FanIntent;
		return (float)m.TotalIntent;
	}
	public static int CapServiceable(RegionalRecordData data, int serviceable) =>
		Enabled && memories.TryGetValue(data, out var m) && m.PreparedWeek == week
			? Math.Min(serviceable, (int)Math.Min(int.MaxValue, Math.Floor(m.TotalIntent))) : serviceable;
	// Called after ArtistFanbase settlement and before retirement. Fractional partition
	// sums to the integer cleared ledger, while both owner pools remain conserved.
	public static void SettleWeek(IEnumerable<RecordRuntimeData> records) {
		if (!Enabled) return;
		foreach (var record in records) foreach (var pair in record.regionalData) {
			var data = pair.Value;
			if (!memories.TryGetValue(data, out var m) || m.PreparedWeek != week) continue;
			FanReleaseRegion fan = null;
			if (ArtistFanbase.Enabled) record.fanAudience?.regions.TryGetValue(pair.Key, out fan);
			double fanCleared = record.weeksSinceRelease <= 26 ? fan?.clearedFans ?? 0 : 0;
			double cleared = data.unitsSoldThisWeek, anonymous = cleared - fanCleared;
			m.State.Settle(anonymous);
			double fanOwners = fan?.purchased.Values.Sum() ?? 0;
			if (Math.Abs(m.State.Owners + fanOwners - data.unitsSoldTotal) > .001)
				throw new InvalidOperationException("Bounded owners do not reconcile with settled title units");
			var s = m.State;
			writer.WriteLine(FormattableString.Invariant($"{week},{record.baseRecord.recordId},{pair.Key},{record.weeksSinceRelease},{s.Capacity},{m.FanCapacity},{s.BeforeInterest},{s.BeforeOwners},{s.BeforeEnthusiasts},{m.Radio},{m.Peer},{m.External},{s.Acquired},{s.NewEnthusiasts},{m.Rate},{m.Fit},{m.Connected},{m.LegacyRaw},{s.Intent},{m.FanIntent},{m.TotalIntent},{cleared},{fanCleared},{anonymous},{s.Owners},{s.Interested},{s.Enthusiasts},{fanOwners}"));
		}
	}
}
