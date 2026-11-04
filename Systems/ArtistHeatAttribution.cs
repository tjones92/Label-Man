using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

/// <summary>
/// Head-hit investigation: optional read-only attribution, and a separate diagnostic
/// that excludes album contributions from live singles' artist heat. A second
/// diagnostic retains current album hype but excludes repeated album peak credit.
/// Albums retain their authored heat. All flags are off unless explicitly named.
/// </summary>
public static class ArtistHeatAttribution {
	public static bool TraceEnabled { get; private set; }
	public static bool SinglesWithoutAlbums { get; private set; }
	public static bool SinglesWithCurrentAlbums { get; private set; }
	public static bool AdjustsSingles => SinglesWithoutAlbums || SinglesWithCurrentAlbums;
	private static StreamWriter heatWriter, launchWriter;
	private static bool configured;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
		TraceEnabled = args.Contains("--head-hit-attribution");
		SinglesWithoutAlbums = args.Contains("--single-artist-heat-no-albums");
		SinglesWithCurrentAlbums = args.Contains("--single-artist-heat-current-albums");
		if (SinglesWithoutAlbums && SinglesWithCurrentAlbums)
			throw new ArgumentException("Choose only one singles' artist-heat diagnostic.");
		configured = true;
	}

	public static void Open(string prefix) {
		Configure(SimulationArguments.Get());
		if (!TraceEnabled) return;
		heatWriter = new StreamWriter(prefix + "-artist-heat-attribution.csv");
		launchWriter = new StreamWriter(prefix + "-single-launch-attribution.csv");
		heatWriter.WriteLine("week,year,artistId,labelId,singleCurrentHeat,singlePeakHeat,albumCurrentHeat,albumPeakHeat,singleHits,albumHits,originalHeat,singlesOnlyHeat,albumCausedHeatDelta,currentAlbumsHeat,retainedAlbumHeatDelta");
		launchWriter.WriteLine("week,year,singleId,labelId,artistId,artistName,strategy,genre,quality,hook,production,dance,career,openingArchetype,formedYear,initialStock,initialAwareness,effectiveRecognition,recognitionRadioLift,originalCampaignImpact,spreadCampaignImpact,labelProduction,labelMarketing,labelBudget,labelPayola,artistMomentum,artistReputation,labelReputation,launchCampaignImpact");
	}

	public static void Close() {
		heatWriter?.Dispose(); launchWriter?.Dispose();
		heatWriter = null; launchWriter = null;
	}

	public static bool TraceWeek(int week) => heatWriter != null && week > 0 && (week - 1) % 4 == 0;

	public readonly struct HeatSplit {
		public readonly float SingleCurrent, SinglePeak, AlbumCurrent, AlbumPeak, Original, SinglesOnly, CurrentAlbums;
		public readonly int SingleHits, AlbumHits;
		public HeatSplit(float sc, float sp, float ac, float ap, int sh, int ah, float original, float singlesOnly, float currentAlbums) {
			SingleCurrent = sc; SinglePeak = sp; AlbumCurrent = ac; AlbumPeak = ap;
			SingleHits = sh; AlbumHits = ah; Original = original; SinglesOnly = singlesOnly; CurrentAlbums = currentAlbums;
		}
	}

	public static HeatSplit Describe(IEnumerable<RecordRuntimeData> records, float authoredRawHeat) {
		float sc = 0f, sp = 0f, ac = 0f, ap = 0f, singleRaw = 0f, currentAlbumsRaw = 0f;
		int sh = 0, ah = 0;
		foreach (var record in records) {
			float current = record.currentPosition > 0 && record.currentPosition <= 10 ? 0.3f
				: record.currentPosition > 0 && record.currentPosition <= 40 ? 0.15f
				: record.currentPosition > 0 ? 0.05f : 0f;
			float peak = record.peakPosition > 0 && record.peakPosition <= 10 ? 0.1f
				: record.peakPosition > 0 && record.peakPosition <= 40 ? 0.05f : 0f;
			currentAlbumsRaw += current;
			if (record.baseRecord.format == ReleaseFormat.Single) {
				sc += current; sp += peak;
				// Preserve the authored addition order for the single-only counterfactual.
				singleRaw += current; singleRaw += peak;
				currentAlbumsRaw += peak;
				if (peak > 0f) sh++;
			} else {
				ac += current; ap += peak;
				if (peak > 0f) ah++;
			}
		}
		float reconstructed = sc + sp + ac + ap + Mathf.Min((sh + ah) * 0.05f, 0.3f);
		if (Mathf.Abs(reconstructed - authoredRawHeat) > 0.0001f * Mathf.Max(1f, Mathf.Abs(authoredRawHeat)))
			throw new InvalidOperationException("Artist heat attribution does not reconcile with the authored calculation.");
		singleRaw += Mathf.Min(sh * 0.05f, 0.3f);
		currentAlbumsRaw += Mathf.Min(sh * 0.05f, 0.3f);
		return new HeatSplit(sc, sp, ac, ap, sh, ah, Mathf.Clamp(authoredRawHeat, 0f, 1f), Mathf.Clamp(singleRaw, 0f, 1f), Mathf.Clamp(currentAlbumsRaw, 0f, 1f));
	}

	public static float SelectHeat(ReleaseFormat format, float authoredHeat, float singlesOnlyHeat, float currentAlbumsHeat, bool live) {
		if (!live || format != ReleaseFormat.Single) return authoredHeat;
		return SinglesWithoutAlbums ? singlesOnlyHeat : SinglesWithCurrentAlbums ? currentAlbumsHeat : authoredHeat;
	}

	internal static void SetPolicyForChecks(bool withoutAlbums, bool currentAlbums = false) {
		SinglesWithoutAlbums = withoutAlbums; SinglesWithCurrentAlbums = currentAlbums;
	}

	public static void CaptureHeat(int week, int year, string artistId, HeatSplit split) {
		if (!TraceWeek(week) || string.IsNullOrEmpty(artistId)) return;
		string labelId = ArtistManager.Instance?.GetArtist(artistId)?.labelId ?? string.Empty;
		heatWriter.WriteLine(string.Join(",", new[] { week.ToString(), year.ToString(), Csv(artistId), Csv(labelId),
			F(split.SingleCurrent), F(split.SinglePeak), F(split.AlbumCurrent), F(split.AlbumPeak),
			split.SingleHits.ToString(), split.AlbumHits.ToString(), F(split.Original), F(split.SinglesOnly), F(split.Original - split.SinglesOnly),
			F(split.CurrentAlbums), F(split.Original - split.CurrentAlbums) }));
	}

	public static void CaptureLaunch(int week, ReleaseStrategyTelemetry strategy) {
		if (launchWriter == null) return;
		string id = strategy.chosenFormat == ReleaseFormat.Single ? strategy.recordId : strategy.promoSingleId;
		if (string.IsNullOrEmpty(id)) return;
		var runtime = ChartManager.Instance?.GetRecordRuntimeData(id);
		if (runtime == null) throw new InvalidOperationException("Single launch attribution cannot resolve " + id);
		var artist = ArtistManager.Instance?.GetArtist(strategy.artistId);
		var label = ChartManager.Instance?.GetLabelById(strategy.labelId);
		var record = runtime.baseRecord;
		float impact = ChartSimulator.GetCampaignImpact(label);
		launchWriter.WriteLine(string.Join(",", new[] { week.ToString(), (TimeManager.Instance?.CurrentDate.year ?? 1960).ToString(),
			Csv(id), Csv(strategy.labelId), Csv(strategy.artistId), Csv(artist?.stageName), Csv(strategy.strategy.ToString()), Csv(record.primaryGenre.ToString()),
			F(runtime.GetQuality()), F(record.hookStrength), F(record.productionQuality), F(record.danceability),
			Csv(artist?.careerState.ToString()), Csv(artist?.openingArchetype.ToString()), (artist?.formedYear ?? 0).ToString(),
			runtime.initialLaunchStock.ToString(), F(runtime.initialLaunchAwareness), F(runtime.launchEffectiveRecognition), F(runtime.launchRecognitionRadioLift),
			F(impact), F(RadioPushSpread.Apply(impact)), F(label?.productionQuality ?? 0f), F(label?.marketingPower ?? 0f), F(label?.budgetLevel ?? 0f), F(label?.payolaWillingness ?? 0f),
			F(artist?.momentum ?? 0f), F(artist?.reputation ?? 0f), F(label?.reputation ?? 0f), F(LaunchPushSaturation.Apply(impact)) }));
	}

	private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
	private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
}
