using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

/// <summary>Fresh-world research probes, never game defaults. Taste stays fixed; these test
/// whether local listening and purchases can transmit it through the existing finite market.
/// Session memory is deliberately not a save format: reject save/resume experiments.</summary>
public static class LocalRadioConsumptionProbe {
	public static float Contact { get; private set; }
	public static float Familiarity { get; private set; }
	public static float Peer { get; private set; }
	public static float LocalShelf { get; private set; }
	public static bool LocalSignal { get; private set; }
	private static bool listeners;
	private static bool observe;
	private static string output;
	private static StreamWriter writer;
	private sealed class Memory { public int Age = -1; public float Listening; }
	private static readonly Dictionary<RegionalRecordData, Memory> memories = new();
	public static bool Enabled => Contact > 0 || Familiarity > 0 || Peer > 0 || LocalShelf > 0 || LocalSignal;

	public static void Configure(IEnumerable<string> arguments) {
		var args = arguments.ToArray();
		Contact = Read(args, "--local-consumption-contact=");
		Familiarity = Read(args, "--local-consumption-familiarity=");
		Peer = Read(args, "--local-consumption-peer=");
		LocalShelf = Read(args, "--local-consumption-shelf=", 1f);
		LocalSignal = args.Contains("--local-consumption-radio-signal");
		listeners = args.Contains("--local-consumption-listeners");
		observe = args.Contains("--local-consumption-observe");
		if (!Enabled && !observe) return;
		if (args.Any(a => a.StartsWith("--resume-world") || a.StartsWith("--save-world")))
			throw new ArgumentException("Local consumption probes require fresh uninterrupted worlds; their session memory is not saved.");
		if (args.Contains("--station-regional-support-chart=national"))
			throw new ArgumentException("Local consumption probes cannot read shadow playlists.");
		string run = args.FirstOrDefault(a => a.StartsWith("--run="))?.Substring(6);
		if (observe && (string.IsNullOrEmpty(run) || Path.GetFileName(run) != run))
			throw new ArgumentException("Local consumption observation requires a plain --run name.");
		output = Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), run + "-local-consumption.csv");
		GD.Print($"LOCAL_CONSUMPTION contact={Contact} familiarity={Familiarity} peer={Peer} shelf={LocalShelf} localSignal={LocalSignal} observe={observe}");
	}
	private static float Read(string[] args, string prefix, float max = 20f) {
		var values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return 0;
		if (values.Length != 1 || !float.TryParse(values[0].Substring(prefix.Length), NumberStyles.Float,
			CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value) || value < 0 || value > max)
			throw new ArgumentException(prefix + " expects one finite value in [0," + max + "].");
		return value;
	}
	public static bool Applies(RecordRuntimeData r) => (Enabled || observe) && SingleCommercialFit.Applies(r)
		&& !r.baseRecord.isPlayerOwned && r.baseRecord.format != ReleaseFormat.Album;
	public static float RadioSignal(RecordRuntimeData r, string region, float original) =>
		LocalSignal && Applies(r) ? .75f + .5f * ChartManager.Instance.ReporterAirplay(r.baseRecord.recordId, region) : original;
	public static float Shelf(RecordRuntimeData r, RegionalRecordData d, float original) =>
		LocalShelf > 0 && Applies(r) && d.localSalesRank > 0
			? Mathf.Lerp(original, Mathf.Max(original, ChartSimulator.GetChartExposureWeight(d.localSalesRank)), LocalShelf) : original;

	/// <summary>Last week's panel and completed purchases only, before current sales/meeting.
	/// Contact is an odds-free bounded addition to aware audience. Familiarity is a capped
	/// conversion response to accumulated listening. Peers use prior actual buyers, not raw intent.
	/// Doses and the 1% active cohort denominator are hypotheses, not historical estimates.</summary>
	public static float Demand(RecordRuntimeData r, RegionalRecordData d, string region, float raw) {
		if (!Applies(r)) return raw;
		float panel = listeners ? ChartManager.Instance.ListenerAirplay(r.baseRecord.recordId, region)
			: ChartManager.Instance.ReporterAirplay(r.baseRecord.recordId, region);
		if (!memories.TryGetValue(d, out var m)) memories[d] = m = new Memory();
		if (m.Age != r.weeksSinceRelease) {
			m.Listening = .7f * m.Listening + panel;
			m.Age = r.weeksSinceRelease;
		}
		float audience = d.demandPotentialAudience;
		float aware = audience > 0 ? Mathf.Clamp(d.demandAwareBuyers / audience, 0, 1) : 0;
		float contact = 1f - Mathf.Exp(-Contact * panel);
		float peerPressure = d.unitsSoldThisWeek / Mathf.Max(1, audience * .01f);
		float peerContact = 1f - Mathf.Exp(-Peer * peerPressure);
		float nextAware = aware + (1 - aware) * (1 - (1 - contact) * (1 - peerContact));
		float familiar = 1 + Familiarity * (1 - Mathf.Exp(-m.Listening));
		float after = Contact > 0 || Familiarity > 0 || Peer > 0
			? audience * nextAware * d.demandIntrinsicConversionRate * familiar : raw;
		if (observe && r.weeksSinceRelease <= 14) {
			if (writer == null) {
				writer = new StreamWriter(output) { AutoFlush = true };
				writer.WriteLine("recordId,region,age,position,localRank,panel,listening,aware,afterAware,priorBuyers,audience,conversion,rawBefore,rawAfter,fitExposureAge,stock");
			}
			writer.WriteLine(FormattableString.Invariant($"{r.baseRecord.recordId},{region},{r.weeksSinceRelease},{r.currentPosition},{d.localSalesRank},{panel},{m.Listening},{aware},{nextAware},{d.unitsSoldThisWeek},{audience},{d.demandIntrinsicConversionRate},{raw},{after},{d.fitExposureAge},{d.unitsInStores}"));
		}
		return after;
	}
}
