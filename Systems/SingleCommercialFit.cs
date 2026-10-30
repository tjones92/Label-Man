using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// Demand probe: a persistent, record-specific commercial response on AI singles' purchase conversion.
/// Off, conversion depends on quality, push and exposure alone, so every competent release finds roughly the
/// same audience: launched singles sell a median ~14k with almost none under 2k, while period small-label
/// singles commonly sold hundreds to a few thousand. Historically most records simply failed to connect,
/// which no label could foresee (DemandAttentionResearch §8 Stage B: fit varies by record and market, is
/// persistent, and has correlated components across markets).
///
/// When on, each live-launched single draws a national and a regional factor, each with mean one, from a
/// counter RNG keyed by record and region. The national factor is lognormal, or kinked: only a hit share of
/// launches connects, and launch strength raises that share. No global random draws, no saved state, and no foreknowledge:
/// labels' priors and pressing forecasts never read it. An optional scale renormalizes aggregate units,
/// because chart feedback makes the spread convex. All three values are tuning assumptions.
/// </summary>
public static class SingleCommercialFit {
	public const string SigmaPrefix = "--single-commercial-fit=";
	public const string RegionalPrefix = "--single-commercial-fit-regional=";
	public const string ScalePrefix = "--single-commercial-fit-scale=";
	/// <summary>Kinked shape: only this share of singles connects. Below the kink the national response falls
	/// with the full sigma; above it, it rises only with the upside sigma, so luck cannot outsell a campaign.</summary>
	public const string HitSharePrefix = "--single-commercial-fit-hit-share=";
	public const string UpsidePrefix = "--single-commercial-fit-upside=";
	/// <summary>How far one unit of launch awareness moves the kink, in standard deviations.</summary>
	public const string LaunchWeightPrefix = "--single-commercial-fit-launch-weight=";
	/// <summary>Launch awareness of a typical new signing, where the hit share applies as given.</summary>
	public const float LaunchReference = .2f;
	/// <summary>Lowest national factor. A record that never connected still sold to jukebox operators, the act's
	/// own following and its home town: a few hundred copies, not a few dozen.</summary>
	public const string FloorPrefix = "--single-commercial-fit-floor=";
	/// <summary>Share of a launch's own mean that is normalized away: 0 normalizes only at the reference launch (launch
	/// strength moves expected sales), 1 gives every launch mean one (launch strength moves only the spread).</summary>
	public const string OwnNormalizationPrefix = "--single-commercial-fit-own-normalization=";
	/// <summary>How far one unit of the record's campaign push moves the kink, in standard deviations.</summary>
	public const string PushWeightPrefix = "--single-commercial-fit-push-weight=";
	/// <summary>Campaign push of a typical Independent, where the hit share applies as given.</summary>
	public const float PushReference = .35f;
	private const uint NationalDecision = 0x43460000U;
	private const uint RegionalDecision = 0x43460100U;
	private const string NationalKey = "national";
	private static bool configured;
	public static float Sigma { get; private set; }
	public static float RegionalSigma { get; private set; }
	public static float Scale { get; private set; } = 1f;
	public static float HitShare { get; private set; } = 1f;
	public static float UpsideSigma { get; private set; }
	public static float LaunchWeight { get; private set; }
	public static float PushWeight { get; private set; }
	public static float Floor { get; private set; }
	public static float OwnNormalization { get; private set; }
	private static double kink, logNormalizer;
	/// <summary>Regional sync §9s: the most an unexposed region reads of the national log, the reference launch's median
	/// draw, so a connected record sells there at the flop level and a flop is unchanged.</summary>
	public static double UnexposedCapLog { get; private set; }
	public static bool Enabled => Sigma > 0f || RegionalSigma > 0f;
	private static ulong? seed;
	private static ulong Seed => seed ??= BandLife.WorldSeed;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Sigma = Read(args, SigmaPrefix, 0f, 0f, 3f);
		RegionalSigma = Read(args, RegionalPrefix, Sigma * .5f, 0f, 3f);
		Scale = Read(args, ScalePrefix, 1f, .25f, 4f);
		HitShare = Read(args, HitSharePrefix, 1f, .01f, 1f);
		UpsideSigma = Read(args, UpsidePrefix, HitShare < 1f ? .15f : 0f, 0f, 3f);
		LaunchWeight = Read(args, LaunchWeightPrefix, 0f, 0f, 20f);
		PushWeight = Read(args, PushWeightPrefix, 0f, 0f, 20f);
		Floor = Read(args, FloorPrefix, 0f, 0f, 1f);
		OwnNormalization = Read(args, OwnNormalizationPrefix, 0f, 0f, 1f);
		if (!Enabled && (Scale != 1f || HitShare != 1f))
			throw new ArgumentException(ScalePrefix + " and " + HitSharePrefix + " require " + SigmaPrefix + ".");
		Prepare();
		configured = true;
	}

	internal static void SetForChecks(float sigma, float regionalSigma, float scale, ulong checkSeed, float hitShare = 1f, float upside = 0f,
		float launchWeight = 0f, float pushWeight = 0f, float floor = 0f, float ownNormalization = 0f) {
		configured = true; Sigma = sigma; RegionalSigma = regionalSigma; Scale = scale; seed = checkSeed;
		HitShare = hitShare; UpsideSigma = upside; LaunchWeight = launchWeight; PushWeight = pushWeight; Floor = floor; OwnNormalization = ownNormalization; Prepare();
	}

	/// <summary>Kink at the (1 - hit share) normal quantile for a launch at the reference awareness.</summary>
	private static void Prepare() {
		kink = logNormalizer = 0d;
		UnexposedCapLog = 0d;
		if (HitShare >= 1f) return;
		double low = -8d, high = 8d;
		for (int i = 0; i < 100; i++) { double mid = (low + high) / 2d; if (NormalCdf(mid) < 1d - HitShare) low = mid; else high = mid; }
		kink = (low + high) / 2d;
		logNormalizer = LogMean(kink);
		UnexposedCapLog = -Sigma * kink - logNormalizer;
		if (Floor > 0f) UnexposedCapLog = Math.Max(UnexposedCapLog, Math.Log(Floor));
	}

	private static double LogMean(double k) => Math.Log(Math.Exp(-Sigma * k + .5d * Sigma * Sigma) * NormalCdf(k - Sigma)
		+ Math.Exp(-UpsideSigma * k + .5d * UpsideSigma * UpsideSigma) * (1d - NormalCdf(k - UpsideSigma)));

	/// <summary>
	/// Log of the national factor. A stronger launch (artist recognition, campaign, label standing, all in launch
	/// awareness, and the label's campaign push, which got a record auditioned by programmers and dealers) moves
	/// the kink down, so more of its draws connect. One normalizer, at the reference launch, gives that launch
	/// mean one: a stronger launch expects more and a weaker one less. Normalizing each record fully to its own mean
	/// was tried and rejected: a weak launch that rarely connects then sells 1/p times more when it does, and
	/// on a hundred-slot chart those lottery hits from the many small releases displaced Major-family share. The
	/// reference normalizer alone moved a fifth to a third of small labels' units to Majors; own-normalization
	/// blends the two in log space.
	/// </summary>
	internal static double National(double z, float launchAwareness, float push) {
		if (HitShare >= 1f) return Lognormal(Sigma, z);
		double k = kink - LaunchWeight * (launchAwareness - LaunchReference) - PushWeight * (push - PushReference);
		double normalizer = OwnNormalization > 0f ? logNormalizer + OwnNormalization * (LogMean(k) - logNormalizer) : logNormalizer;
		double log = (z < k ? Sigma * (z - k) : UpsideSigma * (z - k)) - normalizer;
		return Floor > 0f ? Math.Max(log, Math.Log(Floor)) : log;
	}

	// Abramowitz and Stegun 7.1.26; absolute error below 1.5e-7.
	private static double NormalCdf(double z) {
		double x = Math.Abs(z) / Math.Sqrt(2d), t = 1d / (1d + .3275911d * x);
		double erf = 1d - ((((1.061405429d * t - 1.453152027d) * t + 1.421413741d) * t - .284496736d) * t + .254829592d) * t * Math.Exp(-x * x);
		return z >= 0d ? .5d * (1d + erf) : .5d * (1d - erf);
	}

	/// <summary>Opening catalog is already selling at its known level; only live launches meet the market cold.</summary>
	/// Push is read from the label's current campaign capacity, which changes rarely; a transferred master
	/// is re-read against its new owner.
	public static float Multiplier(RecordRuntimeData record, string regionId, AILabel label) {
		return Evaluate(record, regionId, label, applyHitSupport: true);
	}

	/// <summary>Read-only attribution under the current owner/push; not a counterfactual simulation.</summary>
	public static float WithoutHitSupport(RecordRuntimeData record, string regionId, AILabel label) =>
		Evaluate(record, regionId, label, applyHitSupport: false);

	/// <summary>Whether the record meets the market cold and so carries a commercial fit: a live AI release.</summary>
	public static bool Applies(RecordRuntimeData record) => Enabled && record?.baseRecord != null &&
		!record.baseRecord.isPlayerOwned && record.baseRecord.releaseDate > GameDate.StartDate;

	/// <summary>§9s: whether the record's own national draw connected, i.e. is above <see cref="UnexposedCapLog"/>.</summary>
	public static bool Connected(RecordRuntimeData record, AILabel label) =>
		Applies(record) && RawNationalLog(record, label) > UnexposedCapLog;

	/// <summary>§9x: the record's own national log draw, for a programmer's read of it (<see cref="StationPicks"/>
	/// <c>fit</c>); 0 (the reference median) when the fit does not apply.</summary>
	public static double NationalLog(RecordRuntimeData record, AILabel label) => Applies(record) ? RawNationalLog(record, label) : 0d;

	/// <summary>Audit read: the latent national draw and launch push that <see cref="NationalLog"/> combines with launch
	/// awareness. Keyed, so reading it draws nothing.</summary>
	public static (double Z, float Push) NationalInputs(RecordRuntimeData record, AILabel label) =>
		(Normal(record.baseRecord.recordId, NationalKey, NationalDecision),
			ChartSimulator.GetCampaignImpact(label) * RecordCampaignSpend.PushFactor(record));

	private static double RawNationalLog(RecordRuntimeData record, AILabel label) {
		float push = ChartSimulator.GetCampaignImpact(label) * RecordCampaignSpend.PushFactor(record);
		return National(Normal(record.baseRecord.recordId, NationalKey, NationalDecision), record.initialLaunchAwareness, push);
	}

	private static float Evaluate(RecordRuntimeData record, string regionId, AILabel label, bool applyHitSupport) {
		if (!Applies(record)) return 1f;
		string id = record.baseRecord.recordId;
		if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(regionId))
			throw new InvalidOperationException("Commercial fit requires persistent record and region IDs.");
		double nationalLog = RawNationalLog(record, label);
		// §9s: the connection is realized only where the record is exposed; a hit act's floor still applies on top.
		if (RegionalFitExposure.Enabled && !RegionalFitExposure.Exposed(record, regionId))
			nationalLog = Math.Min(nationalLog, UnexposedCapLog);
		if (applyHitSupport && HitActFitSupport.Enabled) nationalLog = HitActFitSupport.Apply(record, nationalLog);
		double log = nationalLog + Lognormal(RegionalSigma, Normal(id, regionId, RegionalDecision));
		return (float)(Scale * Math.Exp(log));
	}

	/// <summary>Log of a mean-one lognormal factor.</summary>
	private static double Lognormal(float sigma, double z) => sigma * z - .5 * sigma * sigma;

	/// <summary>Box-Muller over two counter draws; week 0 because the response never changes.</summary>
	private static double Normal(string recordId, string key, uint decision) {
		double u1 = PhiloxCounterRandom.Unit(Seed, recordId, key, 0, decision);
		double u2 = PhiloxCounterRandom.Unit(Seed, recordId, key, 0, decision | 1U);
		return Math.Sqrt(-2.0 * Math.Log(Math.Max(u1, 1e-300))) * Math.Cos(2.0 * Math.PI * u2);
	}

	private static float Read(string[] args, string prefix, float fallback, float min, float max) {
		string raw = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length);
		if (raw == null) return fallback;
		if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || value < min || value > max)
			throw new ArgumentException($"{prefix} takes a number from {min} to {max}.");
		return value;
	}
}
