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
	/// <summary>How far the act's own recent chart record moves the kink, in standard deviations (MajorShareRootCauseHandoff.md).
	/// The record is the recency of the act's last Top 40, 0.5^(weeks / <see cref="ActRecordHalfLifeWeeks"/>), zero past
	/// <see cref="ActRecordWindowWeeks"/>; captured at launch and saved, so a proven hitmaker's follow-up is a safer bet
	/// without counting label and campaign awareness a second time. Off (0) captures and reads nothing.</summary>
	public const string ActRecordWeightPrefix = "--single-commercial-fit-act-record-weight=";
	public const int ActRecordHalfLifeWeeks = 26;
	public const int ActRecordWindowWeeks = 104;
	/// <summary>With the act-record weight: the act record raises only the odds of connecting. A connected draw's size is
	/// measured from the kink without the act term, so a draw between the two kinks sells at the just-connected level.
	/// Without it, the whole kink moves, which under a wide upside also multiplies every connected draw (fit208-v2).</summary>
	public const string ActRecordOddsOnlySwitch = "--single-commercial-fit-act-record-odds-only";
	/// <summary>Most a draw above the kink can lift the national log, so luck decides which records connect but not how far
	/// past a just-connected record they sell. Off (0) leaves the upside unbounded as before.</summary>
	public const string UpsideCapPrefix = "--single-commercial-fit-upside-cap=";
	/// <summary>With the act-record weight in odds-only mode: a connected draw's log rises by S x the act record, so a
	/// proven act's record that connects sells more (climbs), not only connects more often. Field hit-act follow-ups
	/// section of Big5ChartOpportunityInvestigation.md. Off (0) is the odds-only shape exactly.</summary>
	public const string ActRecordSizePrefix = "--single-commercial-fit-act-record-size=";
	/// <summary>With the act-record size: the lift is full up to the size kink (draws between the act's kink and the size
	/// kink sell at the just-connected level) and falls linearly to zero T standard deviations above the size kink, so a proven act's marginal record is carried into the Top 40 but its strong draw is not enlarged.
	/// Needs S &lt;= T x upside so a better draw never sells less. Off (0): a flat lift.</summary>
	public const string ActRecordSizeTaperPrefix = "--single-commercial-fit-act-record-size-taper=";
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
	public static float ActRecordWeight { get; private set; }
	public static float UpsideCap { get; private set; }
	public static float ActRecordSize { get; private set; }
	public static float ActRecordSizeTaper { get; private set; }
	public static bool ActRecordOddsOnly { get; private set; }
	private static bool fitForAwareBuyers;
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
		// Purchase conversion is already applied to the reached audience. This experiment removes
		// just its servicing cap; station servicing, reach, physical delivery and the local clocks still run.
		fitForAwareBuyers = args.Contains("--fit-for-aware-buyers");
		if (fitForAwareBuyers && !args.Contains(RegionalFitExposure.Flag))
			throw new ArgumentException("--fit-for-aware-buyers requires " + RegionalFitExposure.Flag);
		Sigma = Read(args, SigmaPrefix, 0f, 0f, 3f);
		RegionalSigma = Read(args, RegionalPrefix, Sigma * .5f, 0f, 3f);
		Scale = Read(args, ScalePrefix, 1f, .25f, 4f);
		HitShare = Read(args, HitSharePrefix, 1f, .01f, 1f);
		UpsideSigma = Read(args, UpsidePrefix, HitShare < 1f ? .15f : 0f, 0f, 3f);
		LaunchWeight = Read(args, LaunchWeightPrefix, 0f, 0f, 20f);
		PushWeight = Read(args, PushWeightPrefix, 0f, 0f, 20f);
		Floor = Read(args, FloorPrefix, 0f, 0f, 1f);
		OwnNormalization = Read(args, OwnNormalizationPrefix, 0f, 0f, 1f);
		ActRecordWeight = Read(args, ActRecordWeightPrefix, 0f, 0f, 5f);
		UpsideCap = Read(args, UpsideCapPrefix, 0f, 0f, 10f);
		if ((ActRecordWeight > 0f || UpsideCap > 0f) && HitShare >= 1f)
			throw new ArgumentException(ActRecordWeightPrefix + " and " + UpsideCapPrefix + " need the kinked fit (" + HitSharePrefix + ").");
		ActRecordOddsOnly = args.Contains(ActRecordOddsOnlySwitch);
		ActRecordSize = Read(args, ActRecordSizePrefix, 0f, 0f, 3f);
		if (ActRecordSize > 0f && !ActRecordOddsOnly)
			throw new ArgumentException(ActRecordSizePrefix + " needs " + ActRecordOddsOnlySwitch + " (without it the whole kink already enlarges a connected draw).");
		ActRecordSizeTaper = Read(args, ActRecordSizeTaperPrefix, 0f, 0f, 10f);
		if (ActRecordSizeTaper > 0f && (ActRecordSize <= 0f || ActRecordSize > ActRecordSizeTaper * UpsideSigma + 1e-6f))
			throw new ArgumentException(ActRecordSizeTaperPrefix + " needs " + ActRecordSizePrefix + " with size <= taper x upside (a better draw must not sell less).");
		if (ActRecordOddsOnly && (ActRecordWeight <= 0f || OwnNormalization > 0f))
			throw new ArgumentException(ActRecordOddsOnlySwitch + " needs " + ActRecordWeightPrefix + " and no own-normalization.");
		if (ActRecordWeight > 0f && !HitActCadence.ClockEnabled)
			throw new ArgumentException(ActRecordWeightPrefix + " reads the hit-act Top 40 clock (--known-act-airplay or a hit-act cadence switch).");
		if (!Enabled && (Scale != 1f || HitShare != 1f))
			throw new ArgumentException(ScalePrefix + " and " + HitSharePrefix + " require " + SigmaPrefix + ".");
		Prepare();
		configured = true;
	}

	internal static void SetForChecks(float sigma, float regionalSigma, float scale, ulong checkSeed, float hitShare = 1f, float upside = 0f,
		float launchWeight = 0f, float pushWeight = 0f, float floor = 0f, float ownNormalization = 0f, float actRecordWeight = 0f, float upsideCap = 0f,
		bool actRecordOddsOnly = false, bool awareBuyers = false, float actRecordSize = 0f, float actRecordSizeTaper = 0f) {
		fitForAwareBuyers = awareBuyers;
		configured = true; Sigma = sigma; RegionalSigma = regionalSigma; Scale = scale; seed = checkSeed;
		HitShare = hitShare; UpsideSigma = upside; LaunchWeight = launchWeight; PushWeight = pushWeight; Floor = floor; OwnNormalization = ownNormalization;
		ActRecordWeight = actRecordWeight; UpsideCap = upsideCap; ActRecordOddsOnly = actRecordOddsOnly; ActRecordSize = actRecordSize; ActRecordSizeTaper = actRecordSizeTaper; Prepare();
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

	private static double LogMean(double k) {
		if (UpsideCap <= 0f || UpsideSigma <= 0f) return Math.Log(Math.Exp(-Sigma * k + .5d * Sigma * Sigma) * NormalCdf(k - Sigma)
			+ Math.Exp(-UpsideSigma * k + .5d * UpsideSigma * UpsideSigma) * (1d - NormalCdf(k - UpsideSigma)));
		// Capped upside: exp(U (z - k)) up to z = k + cap / U, exp(cap) beyond.
		double top = k + UpsideCap / UpsideSigma;
		return Math.Log(Math.Exp(-Sigma * k + .5d * Sigma * Sigma) * NormalCdf(k - Sigma)
			+ Math.Exp(-UpsideSigma * k + .5d * UpsideSigma * UpsideSigma) * (NormalCdf(top - UpsideSigma) - NormalCdf(k - UpsideSigma))
			+ Math.Exp(UpsideCap) * (1d - NormalCdf(top)));
	}

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
	internal static double National(double z, float launchAwareness, float push, float actRecord = 0f) {
		if (HitShare >= 1f) return Lognormal(Sigma, z);
		double k = kink - LaunchWeight * (launchAwareness - LaunchReference) - PushWeight * (push - PushReference);
		double sizeKink = k;
		if (ActRecordWeight > 0f) { k -= ActRecordWeight * actRecord; if (!ActRecordOddsOnly) sizeKink = k; }
		double normalizer = OwnNormalization > 0f ? logNormalizer + OwnNormalization * (LogMean(k) - logNormalizer) : logNormalizer;
		double above = UpsideSigma * Math.Max(0d, z - sizeKink);
		// --single-commercial-fit-act-record-size: a connected draw by a proven act sells larger. Off: nothing added.
		// --single-commercial-fit-act-record-size-taper: the lift fades to zero T above the size kink.
		if (ActRecordSize > 0f && z >= k)
			above += ActRecordSize * actRecord * (ActRecordSizeTaper > 0f ? Math.Max(0d, 1d - Math.Max(0d, z - sizeKink) / ActRecordSizeTaper) : 1d);
		if (UpsideCap > 0f) above = Math.Min(above, UpsideCap);
		double log = (z < k ? Sigma * (z - k) : above) - normalizer;
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
			ActEarnedCampaignImpact.Apply(ChartSimulator.GetCampaignImpact(label), record.baseRecord.recordId) * RecordCampaignSpend.PushFactor(record));

	private static double RawNationalLog(RecordRuntimeData record, AILabel label) {
		float push = ActEarnedCampaignImpact.Apply(ChartSimulator.GetCampaignImpact(label), record.baseRecord.recordId) * RecordCampaignSpend.PushFactor(record);
		return National(Normal(record.baseRecord.recordId, NationalKey, NationalDecision), record.initialLaunchAwareness, push,
			record.actChartRecordAtLaunch ?? 0f);
	}

	/// <summary>Recency of the act's last Top 40 at <paramref name="week"/>: 1 that week, halving every
	/// <see cref="ActRecordHalfLifeWeeks"/>, 0 past <see cref="ActRecordWindowWeeks"/> or never. An opening career's last
	/// pre-1960 Top 40 counts from mid-year (<see cref="HitActCadence.WeeksSinceTop40"/>).</summary>
	internal static float ActRecord(SimulatedArtist artist, int week) {
		if (artist == null) return 0f;
		int age = HitActCadence.WeeksSinceTop40(artist, week);
		return age > ActRecordWindowWeeks ? 0f : (float)Math.Pow(.5d, age / (double)ActRecordHalfLifeWeeks);
	}

	/// <summary>Freezes the act's chart record on a live AI single at launch; off, or already captured, does nothing.</summary>
	public static void CaptureActRecord(RecordRuntimeData record, SimulatedArtist artist, int week) {
		if (ActRecordWeight <= 0f || record?.baseRecord == null || record.actChartRecordAtLaunch.HasValue) return;
		if (record.baseRecord.format != ReleaseFormat.Single || !Applies(record) || week <= 0) return;
		record.actChartRecordAtLaunch = ActRecord(artist, week);
	}

	private static float Evaluate(RecordRuntimeData record, string regionId, AILabel label, bool applyHitSupport) {
		if (!Applies(record)) return 1f;
		string id = record.baseRecord.recordId;
		if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(regionId))
			throw new InvalidOperationException("Commercial fit requires persistent record and region IDs.");
		double nationalLog = RawNationalLog(record, label);
		// §9s: the connection is realized only where the record is exposed; a hit act's floor still applies on top.
		if (!fitForAwareBuyers && RegionalFitExposure.Enabled && !RegionalFitExposure.Exposed(record, regionId))
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
