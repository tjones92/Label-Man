using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager : Node {
	public static CompetitorManager Instance { get; private set; }
	public const float DealReinvestRate = 0.02f;
	public const float DealReinvestCost = 5000000f;
	// A self-built network tops out below the seeded Majors' 0.88-0.90 national reach.
	public const float SelfBuiltReachCeiling = 0.75f;
	public const float SelfBuiltNationalReachCeiling = 0.70f;
	public const float CompletedDealNationalReachCeiling = 0.80f;
	public const float DealDependencyLow = 0.35f;
	public const float DealDependencyHigh = 0.56f;
	
	[ExportGroup("Configuration")]
	[Export] private int targetActiveRecords = 500;
	[Export] private int historicalRecordsCount = 150;
	
	[ExportGroup("Economic Settings")]
	[Export] private float baseRoyaltyRate = 0.04f;
	[Export] private Godot.Collections.Dictionary<string, float> pressingCostPerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.30f },
		{ nameof(ReleaseFormat.Album), 0.95f },
		{ nameof(ReleaseFormat.EP), 0.55f }
	};
	[Export] private Godot.Collections.Dictionary<string, float> pricePerUnitByFormat = new() {
		{ nameof(ReleaseFormat.Single), 0.89f },
		{ "Album", 3.98f }
	};
	[Export] private float bankruptcyThreshold = 200f;
	[Export] private float monthlyOverheadRate = 0.02f;
	[Export] private bool enableBankruptcy = true;
	[Export] private bool enableAlbums = true;
	[Export(PropertyHint.Range, "0,1,0.01")] private float albumPackagingCostPerUnit = 0.22f;
	[Export] private float albumPackagingFixedCost = 1500f;
	[Export(PropertyHint.Range, "0,2,0.05")] private float compilationProductionMultiplier = 0.60f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float revenueMemoryAlpha = 0.30f;
	[Export(PropertyHint.Range, "0.1,20,0.1")] private float revenueMemoryConfidenceK = 4.0f;
	private const int ResponsiveMemoryMaximumHistoryWeeks = 104;
	private const float ResponsiveMemoryHalfLifeWeeks = 52f;
	private const float ResponsiveMemoryMaximumConfidence = .65f;
	private const float ResponsiveMemoryResidualLimit = 3f;
	private const int MaximumAlbumProjectsPerArtistYear = 2;
	private const int AlbumProjectPressureMinimumDecisions = 100;
	private const float AlbumProjectPressureShare = 0.75f;
	private const float PromoProjectEligibilityWeight = 0.75f;
	// The live route no longer needs an eligibility thumb. It compensated for a
	// binary fork that discarded every near-miss Album, so it had to be biased up
	// to keep crossover projects alive. The probabilistic chooser now carries that
	// behavior explicitly and count-neutrally, leaving this as an unearned live-only
	// project-count uplift. The scale stays plumbed through ResolveAlbumDecision so
	// the seam remains explicit, bounded, and independently probeable.
	private const float LiveAlbumDecisionEligibilityScale = 1f;
	private const float FormatChoiceExplorationFloor = 0.02f;
	private const float FormatChoiceLogitSlope = 10f;
	// The live revision model observes high-variance, annualized outcomes and
	// therefore needs more evidence than the frozen retirement-time EMA. Keeping
	// this separate also preserves the disabled route's legacy K=4 behavior.
	private const float ResponsiveMemoryConfidenceK = 12f;
	// Ceiling retained from the former Major-tier coefficient so a fully-capable
	// label lands where majors were already calibrated; the reference roster depth is
	// the point past which a catalogue can keep an LP program fed on its own.
	private const float AlbumPortfolioCommitmentCeiling = 1.50f;
	private const float AlbumProgramRosterDepth = 12f;
	private const float AlbumProgramReachWeight = .55f;
	// A promo Single both diverts Album buyers (cannibalizationLoss) and recruits them
	// (CalculatePromoAlbumSynergyGain). Diversion is substitutionK * albumDemand * shelf
	// overlap; recruitment is PromoAlbumConversionK * albumDemand * awareness headroom.
	// Holding the base conversion below substitutionK made recruitment net-dilutive at
	// every awareness level, so as the LP market matured the promo proposition decayed to
	// non-viable for the highest-volume acts first, evacuating the Major Singles chart
	// after 1966 (30.0% of 1969 chart entries against a 35-50% band). Parity with
	// substitutionK (1.0) recovered 1969 Major entry share to 35.3% but left it on the
	// band floor, with ~71 Major Album decisions a year still dropping the promo Single.
	// A promo Single is the Album's primary advertisement and for a breaking act net-
	// expands the Album audience rather than reallocating it, so the base is set half
	// again above substitutionK to hold late-decade Major entry share off the floor. It
	// stays below 2.4, where even a well-known act's promo would cease to be dilutive, so
	// the awareness-gated crossover is preserved: a net Album driver at real headroom,
	// mildly dilutive at the floor. Only post-1966 standalone decisions move — 1960-65
	// promo already wins every Album decision, so early-decade calibration is untouched.
	private const float PromoAlbumConversionK = 1.50f;
	// A promo Single recruits Album buyers partly on novelty and partly on being the
	// Album's advertisement. Gating recruitment purely on awareness headroom made the
	// second effect vanish exactly where it was strongest: a famous act's hit Single
	// still sold enormous LP volume in 1967-69. Measured headroom falls .90 -> .44
	// across 1966-68 while shelf overlap rises, and that asymmetry — not the level —
	// is what re-opened the crossover a year later.
	private const float PromoAwarenessConversionFloor = .25f;
	private const float AlbumPriorEarlyEraDiscount = .78f;
	private const int AlbumPriorCalibrationBootstrapYear = 1960;
	private const int AlbumPriorCalibrationRetiredYear = 1964;
	// PREWARM SEEDING (2026-08, WIP task B): established-catalog awareness floor for seeded 1960 albums,
	// so the pre-existing LP catalog converts at steady-state from week 1. Note: 1960 is channel/supply-
	// bound, so this proved nearly inert on its own (see D7 handoff); kept for continuation.
	[Export] private float albumPrewarmAwarenessFloor = 0.85f;
	[Export] private float albumPrewarmStockMultiplier = 1.0f;
	// TITLE-COUNT / RUNTIME LEVER (2026-08, WIP). 175000 -> 55000. Album creation is margin-driven, not
	// demand-driven: projectedAlbumNet ran ~2.2x projectedSingleNet, so 62% of releases were albums,
	// and albums accumulate (they outlive singles many-fold). This scales the album prior's expected
	// units -- and only the PRIOR, not realized sales -- so fewer albums are chosen (share 62%->33%,
	// active albums at 1960 2750->1552) while survivors still saturate the channel in channel-bound
	// years. WARNING: this reduces the album population, which shrinks the very quadratics just fixed;
	// for measuring runtime against the committed economy, restore 175000.
	[Export] private float priorUnitScalarAlbum = 55000f;
	[Export] private float priorCompHitUnitScalar = 20000f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float hitRecencyDecay = 0.75f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float priorAssumedAlbumPackaging = 0.50f;
	[Export(PropertyHint.Range, "1958,1965,0.1")] private float albumEraWeightStartYear = 1960f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumEraWeightEndYear = 1968f;
	[Export(PropertyHint.Range, "1960,1967,0.1")] private float albumCohesionRiseStartYear = 1964f;
	[Export(PropertyHint.Range, "1965,1972,0.1")] private float albumCohesionRiseEndYear = 1968f;

	[ExportGroup("Album Project Pipeline")]
	[Export(PropertyHint.Range, "1,12,1")] private int albumDropGapWeeksMin = 3;
	[Export(PropertyHint.Range, "1,12,1")] private int albumDropGapWeeksMax = 5;
	[Export(PropertyHint.Range, "1,100,1")] private int promoFlopThreshold = 80;
	[Export(PropertyHint.Range, "0,1,0.01")] private float promoAwarenessBonusMax = 0.25f;
	[Export(PropertyHint.Range, "0,2,0.01")] private float promoStockBonusMax = 0.80f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float promoStockFlopFloor = 0.85f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float cannibalizationStrength = 0.15f;
	[Export] private float singleNetMarginPerUnit = 0.40f;
	[Export] private float substitutionK = 1.00f;
	// Substitution saturates — a large part of the 45 market never converts to LPs at
	// any level of album dominance — but the binding cap is deliberately loose. Once
	// the absolute promo veto was removed, a declining promo advantage produces the
	// control's gradual .66 -> .44 slide instead of a collapse, so cannibalization is
	// allowed to keep growing. Capping it hard instead (.35) left promo unopposed and
	// overshot the unit ceiling to 1.38 at 1967.
	[Export(PropertyHint.Range, "0,1,0.01")] private float substitutionCap = 0.60f;
	[Export] private float expectedPromoLiftScalar = 10000f;
	[Export] private float expectedOverlapWeeks = 10f;
	// Flattened row-major because Godot does not export multidimensional arrays.
	// Rows are quality quartiles; columns are career bands.
	[Export] private float[] expectedPeakScoreByBucket = {
		0.008805f, 0.042022f, 0.177743f, 0.042022f,
		0.025389f, 0.118921f, 0.177743f, 0.177743f,
		0.056773f, 0.241402f, 0.405063f, 0.405063f,
		0.178960f, 0.505133f, 0.739346f, 0.739346f
	};

	[ExportGroup("Distribution Deals")]
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPullOfferProbability = 0.40f;
	// Path A (section 26): the push route -- Majors courting proven independents -- is the
	// historical late-1960s consolidation engine (WB-Atlantic, MCA, ABC). It was effectively dead
	// (4 signings all decade), so the high-dependency Major-distributed deals absorption feeds on
	// were never created. The courting that precedes the 1967-69 absorption wave ramps mid-decade,
	// as the US indie scene scaled (Motown/Stax growing, the post-Invasion indie proliferation), so
	// the ramp now starts 1964 -- a deal signed 1965-67 with its realistic 78-156wk term then
	// expires inside the 1966+ window before the decade run ends. The base is kept low so the ramp,
	// not a decade-wide flat probability, drives courting into the productive mid-decade years
	// (base-only 1960 push expires pre-window and is wasted).
	[Export(PropertyHint.Range, "0,1,0.001")] private float monthlyPushOfferProbability = 0.05f;
	[Export] private int consolidationCourtingRampStartYear = 1964;
	[Export(PropertyHint.Range, "0,1,0.001")] private float annualCourtingRampPerYear = 0.12f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float pushMastersOwnershipRate = 0.80f;
	// Section 33: monthly chance a label with proven regional evidence places its line with a
	// wholesale house in one more market. Assembling national coverage was months of travelling
	// and pitching after a hit, not an immediate consequence of one, so this paces the spread at
	// roughly a market a quarter rather than letting a single breakout go national at once.
	[Export(PropertyHint.Range, "0,1,0.01")] private float independentDistributionMonthlyChance = 0.35f;
	// Share of a market's retail one wholesale house actually reaches. A label placing its
	// line with a single house in a region gets most of that market, not all of it.
	[Export(PropertyHint.Range, "0,1,0.01")] private float independentCoverageReachFactor = 0.60f;
	// Share of an unreliable house's arrears that is settled anyway. The wait is the squeeze;
	// outright loss is the residue.
	internal const float WholesaleSettledShareOfArrears = 0.70f;
	// Concurrent independent imprints one Major distributes, ramped across the decade and then
	// scaled by the network the firm actually owns. See IsEligibleDistributor.
	[Export(PropertyHint.Range, "2,32,1")] private int majorDistributionClientCeilingEarly = 6;
	[Export(PropertyHint.Range, "4,40,1")] private int majorDistributionClientCeilingLate = 16;
	[Export] private int majorDistributionCeilingRampStartYear = 1964;
	[Export] private int majorDistributionCeilingRampFullYear = 1969;
	// The independent distribution trade did not survive the decade. Regional houses failed or
	// were bought out as major branch systems and rack jobbing took over the wholesale business,
	// and that collapse is a large part of why independents sold or signed in 1968-71. Without
	// it the channel is exactly as strong in 1969 as in 1960, which is what left owner-Major
	// flat near 40% across the decade run.
	[Export] private int independentTradeDeclineStartYear = 1966;
	[Export] private int independentTradeDeclineFullYear = 1970;
	[Export(PropertyHint.Range, "0.1,1,0.01")] private float independentTradeSurvivalLate = 0.50f;
	// Section 28: a Major distributor takes masters on this share of its deals at minimum (P&D
	// era), so its distributed records fold into the major corporate/control chart share.
	// Section 29: this rate ramps across the decade rather than being flat. Early-60s indie deals
	// were mostly distribution-only (the indie kept its masters); the late-60s P&D consolidation is
	// when majors increasingly took the masters. A flat rate stacks a constant slab on every year
	// and can only produce a flat owner-Major line; the ramp produces the historical dip (indie
	// boom erodes major share mid-decade) then rise (majors take masters and absorb late-decade).
	[Export(PropertyHint.Range, "0,1,0.01")] private float majorDistributorMastersOwnershipRateEarly = 0.15f;
	// Late endpoint trimmed 0.45 -> 0.40: 0.45 overshot the 45-52 owner-Major band on the harder
	// holdout seed (1969 owner-Major 57.4 with births-9). This is the metric-only master-control
	// surplus lever; it does not touch deal economics, breadth, or the album economy.
	[Export(PropertyHint.Range, "0,1,0.01")] private float majorDistributorMastersOwnershipRateLate = 0.40f;
	[Export] private int majorMastersRampStartYear = 1962;
	[Export] private int majorMastersRampFullYear = 1968;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMin = 0.15f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pullMarginSkimMax = 0.25f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMin = 0.20f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] private float pushMarginSkimMax = 0.35f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealReinvestRate = DealReinvestRate;
	[Export] private float dealReinvestCost = DealReinvestCost;
	// Net income must clear this multiple of monthly overhead before a label can widen its own
	// distribution, which is what keeps the self-built route uncommon rather than automatic.
	[Export] private float selfBuiltReachSurplusMultiple = 2f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float selfBuiltReachMonthlyGain = 0.004f;
	[Export(PropertyHint.Range, "0,1,0.001")] private float selfBuiltNationalReachMonthlyGain = 0.008f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float selfBuiltReachReinvestRate = 0.10f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float completedDealNationalReachRetention = 0.25f;
	// LocalTraction begins at .20 in UpdateRegionalBreakoutState. That is the
	// observed-market boundary at which a label has evidence worth taking to a
	// distributor; the later .40 RegionalBreakout stage is intentionally not a
	// prerequisite for obtaining the network that helps create it. This tracks
	// LocalTractionActivationScore: the offer-attempt telemetry showed the sign rate
	// cliff sitting exactly on this boundary (~36% just below, ~84% just above), so the
	// two must move together or a record can climb the chart ramp yet be denied the deal.
	[Export(PropertyHint.Range, "0,1,0.01")] private float regionalBreakoutDealThreshold = 0.20f;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyLow = DealDependencyLow;
	[Export(PropertyHint.Range, "0,1,0.01")] private float dealDependencyHigh = DealDependencyHigh;

	// Late-1960s major consolidation. The historical wave (WB-Atlantic 1967, MCA from
	// Decca/Kapp/Uni, ABC absorbing imprints) had majors absorb *charted* independents
	// through the distribution relationship, lifting major-distributed chart share into
	// 1968-69. Absorption reuses the existing deal-expiry -> AbsorbLabel path but is now
	// gated to this window, to Major (optionally national MidTier) acquirers, to indie
	// clients that have charted, and to a decade cap, so it cannot recreate the ungated
	// early-decade random-tier absorptions or crush the independent imprint tail. These
	// only bite from consolidationStartYear on, so 1960-65 behaviour is unchanged.
	[Export] private int consolidationStartYear = 1966;
	// Applied only to high-dependency deal expiries (the Stax->Atlantic branch); the roll and
	// cap bound how many of those charted, major-distributed dependents are absorbed late-decade.
	// DO NOT trim this to reduce owner-Major overshoot -- it backfires. Measured 0.75 -> 0.60 on the
	// hard holdout seed (2029): owner-Major 1969 rose 55.5 -> 58.6. Averting an absorption does not
	// free the client to chart as an Independent; it RENEWS the client on its Major P&D deal, which
	// stays Major-owned via master-control (ownsMasters deal rows 755 -> 775). The client also loses
	// the subsidiary reach boost, so total chart entries shrink (980 -> 958), lifting the ratio from
	// both ends. Net owner-Major goes UP, not down.
	[Export(PropertyHint.Range, "0,1,0.01")] private float consolidationAbsorbChance = 0.75f;
	[Export] private int maxDecadeConsolidationAbsorptions = 40;
	[Export] private bool consolidationRequireCharted = true;
	[Export] private bool consolidationAllowNationalMidTier = false;

	[ExportGroup("Historical Records")]
	[Export] private Record[] historicalRecords;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private int generatedRecordCounter = 0;
	private long generatedProjectCounter;
	private int pipelineWeek;
	private readonly List<AlbumProject> albumProjects = new();
	private readonly List<AlbumProject> pendingAlbumProjects = new();
	private readonly Dictionary<string, AlbumProject> projectById = new();
	private readonly Dictionary<string, AlbumProject> projectByRecordId = new();
	private readonly Dictionary<(string ArtistId, int Year), int> annualAlbumProjectsByArtist = new();
	private int annualFormatCapacityYear = int.MinValue;
	private int annualFormatDecisions;
	private int annualAlbumProjectsScheduled;
	// Album-with-promo performance contains a market-wide structural component.
	// Pool it for shrinkage, then let each label refine that baseline locally.
	private readonly FormatRevenueMemory pooledAlbumWithPromoMemory = new();
	private Dictionary<string, List<string>> labelActiveRecords = new Dictionary<string, List<string>>();
	internal readonly struct LabelRecordHistoryEntry {
		public readonly int ReleaseWeek;
		public readonly bool Charted;
		public readonly bool Top40;
		public LabelRecordHistoryEntry(int releaseWeek, bool charted, bool top40) {
			ReleaseWeek = releaseWeek;
			Charted = charted;
			Top40 = top40;
		}
	}
	private readonly Dictionary<string, List<LabelRecordHistoryEntry>> retiredLabelRecordHistory = new();
	// The regional independent-distribution layer (handoff section 33): the wholesale
	// houses that carried independent labels' lines into shops. Held here rather than in
	// aiLabels because a house is not a firm that releases records, and because coverage
	// it grants must never flow through activeDeal -- that is what keeps owner-Major
	// attribution untouched by an independently distributed record.
	private readonly List<IndependentDistributor> independentDistributors = new();
	private readonly Dictionary<string, List<IndependentDistributor>> independentDistributorsByRegion =
		new(System.StringComparer.Ordinal);
	private int independentDistributorsAtStart;
	private readonly HashSet<string> creditedLabelTop40RecordIds = new(System.StringComparer.Ordinal);
	private readonly HashSet<string> creditedLabelNumberOneRecordIds = new(System.StringComparer.Ordinal);
	// Proven-winner signal for the consolidation lever: a label id enters this set
	// the first time any of its records reaches a national chart position (1-100).
	// A subsidiary keeps its own imprint labelId after absorption, so this set keeps
	// reporting the client's own charting history at deal expiry.
	private readonly HashSet<string> chartedLabelIds = new(System.StringComparer.Ordinal);
	// Late-decade major-consolidation absorptions completed this run, bounded by
	// maxDecadeConsolidationAbsorptions so the lever cannot crush the indie tail.
	private int consolidationAbsorptionsThisDecade;
	// Test-only: forces the next deal resolution of these clients to absorb regardless
	// of window/tier/charted/roll, so the forced-deal integration harness can exercise
	// the AbsorbLabel path deterministically. Empty in every simulation run.
	private readonly HashSet<string> forcedConsolidationClients = new(System.StringComparer.Ordinal);
	private ChartManager chartRecordEventSource;
	private Dictionary<string, LabelFinancialHistory> labelFinancials = new Dictionary<string, LabelFinancialHistory>();
	private readonly Dictionary<string, Dictionary<Genre, int>> annualGenreSupplyByLabel = new();
	private readonly Dictionary<Genre, int> annualGenreSupplyGlobal = new();
	private int genreSupplyYear = int.MinValue;
	
	private List<AILabel> aiLabels;
	private bool distributionOfferProcessingEnabled = true;
	private readonly Dictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> weeklyRevenueByLabelAndFormat = new();
	private int lastBookedSettlementId;
	public int DistributorCollapseCount { get; private set; }
	public int WeeklyReleaseRollsFired { get; private set; }
	public int WeeklySuccessfulReleases { get; private set; }
	public int WeeklyFailedReleaseRolls { get; private set; }
	public int WeeklyCooldownMismatchRolls { get; private set; }
	public int WeeklyPipelineAlbumDrops { get; private set; }
	public int WeeklySingleReleases { get; private set; }
	public int WeeklyAlbumProjectsScheduled { get; private set; }
	public float WeeklyProductionSpend { get; private set; }
	public int WeeklyProductionEvents { get; private set; }
	public float WeeklyMarketingSpend { get; private set; }
	public int WeeklyMarketingEvents { get; private set; }
	private readonly Dictionary<LabelTier, ReleaseLifecycleFlow> weeklyReleaseLifecycleByTier = new();

	public readonly struct ReleaseLifecycleFlow {
		public readonly int Attempts;
		public readonly int SuccessfulReleases;
		public readonly int ArtistSelectionFailures;
		public ReleaseLifecycleFlow(int attempts, int successfulReleases, int artistSelectionFailures) {
			Attempts = attempts;
			SuccessfulReleases = successfulReleases;
			ArtistSelectionFailures = artistSelectionFailures;
		}
	}

	internal readonly struct RegionalDealEvidence {
		public readonly float BestAnyRegionPeak;
		public readonly float BestStrongRegionPeak;
		public readonly float BestPersistentEvidenceQuality;
		public readonly bool HasPersistentRegionalTraction;
		public readonly bool PassesLegacyQualityAndCurrentSalesGate;
		public readonly string EarningRecordId;

		public RegionalDealEvidence(float bestAnyRegionPeak, float bestStrongRegionPeak,
			float bestPersistentEvidenceQuality, bool hasPersistentRegionalTraction,
			bool passesLegacyQualityAndCurrentSalesGate, string earningRecordId = null) {
			BestAnyRegionPeak = bestAnyRegionPeak;
			BestStrongRegionPeak = bestStrongRegionPeak;
			BestPersistentEvidenceQuality = bestPersistentEvidenceQuality;
			HasPersistentRegionalTraction = hasPersistentRegionalTraction;
			PassesLegacyQualityAndCurrentSalesGate = passesLegacyQualityAndCurrentSalesGate;
			EarningRecordId = earningRecordId;
		}
	}

	public int DistributionOffersGenerated { get; private set; }
	public int DistributionOffersAccepted { get; private set; }
	public float CannibalizationStrength => cannibalizationStrength;
	public float CalculateSubstitutionPropensity(Genre genre, int year) =>
		Mathf.Clamp(substitutionK * CalculateAlbumDemandFactor(genre, year), 0f, substitutionCap);
	private bool lastReleaseAttemptFailedArtistSelection;
	public event System.Action<DistributionDealTelemetry> OnDistributionDealEvent;
	public event System.Action<DistributionOfferAttemptTelemetry> OnDistributionOfferAttempt;
	public event System.Action<IndependentDistributionTelemetry> OnIndependentDistributionSigned;
	public event System.Action<IndependentTradeFailureTelemetry> OnIndependentTradeFailure;
	public event System.Action<ReleaseStrategyTelemetry> OnReleaseStrategy;
	public event System.Action<CalibrationDecisionTelemetry> OnCalibrationDecision;
	public event System.Action<ReleaseOutcomeTelemetry> OnReleaseOutcome;
	public event System.Action<FormatMemoryRevisionTelemetry> OnFormatMemoryRevision;
	public event System.Action<SupplySelectionTelemetry> OnSupplySelection;
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}
	
	public override void _Ready() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded += OnWeekEnded;
			TimeManager.Instance.OnMonthChanged += OnMonthChanged;
		}
		EnsureChartRecordSubscription();
	}
	
	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
			TimeManager.Instance.OnMonthChanged -= OnMonthChanged;
		}
		if (chartRecordEventSource != null) {
			chartRecordEventSource.OnRecordChartUpdated -= OnRecordChartUpdated;
			chartRecordEventSource = null;
		}
	}
	
	public void Initialize(List<AILabel> labels) {
		// CompetitorManager precedes ChartManager in autoload order, so ChartManager
		// may not exist during this node's _Ready. Initialize is invoked by
		// ChartManager after its singleton is live and is therefore the reliable
		// subscription boundary for record-outcome bookkeeping.
		EnsureChartRecordSubscription();
		retiredLabelRecordHistory.Clear();
		creditedLabelTop40RecordIds.Clear();
		creditedLabelNumberOneRecordIds.Clear();
		chartedLabelIds.Clear();
		consolidationAbsorptionsThisDecade = 0;
		forcedConsolidationClients.Clear();
		AlbumModel.EraWeightStartYear = albumEraWeightStartYear;
		AlbumModel.EraWeightEndYear = albumEraWeightEndYear;
		AlbumModel.CohesionRiseStartYear = albumCohesionRiseStartYear;
		AlbumModel.CohesionRiseEndYear = albumCohesionRiseEndYear;
		aiLabels = labels;
		foreach (var label in aiLabels) {
			labelActiveRecords[label.labelId] = new List<string>();
			retiredLabelRecordHistory[label.labelId] = new List<LabelRecordHistoryEntry>();
			labelFinancials[label.labelId] = new LabelFinancialHistory();
		}
		BuildIndependentDistributionLayer();
		PopulateInitialRecords();
		GD.Print($"CompetitorManager: Initialized with {aiLabels.Count} labels");
	}

	/// <summary>
	/// Stands up the regional independent-distribution layer (handoff section 33). Slice 1
	/// generates and registers the houses only -- nothing reads them yet, so this is inert
	/// on the simulation by construction and a probe run must stay byte-identical.
	/// </summary>
	private void BuildIndependentDistributionLayer() {
		independentDistributors.Clear();
		independentDistributorsByRegion.Clear();
		var regions = ChartManager.Instance?.GetAllRegions();
		if (regions == null || regions.Count == 0) return;

		independentDistributors.AddRange(
			IndependentDistributorFactory.Generate(regions, SimulationSeedBootstrap.RequestedSeed ?? 0UL));
		foreach (IndependentDistributor house in independentDistributors) {
			if (string.IsNullOrEmpty(house.regionId)) continue;
			if (!independentDistributorsByRegion.TryGetValue(house.regionId, out var inRegion))
				independentDistributorsByRegion[house.regionId] = inRegion = new List<IndependentDistributor>();
			inRegion.Add(house);
		}
		independentDistributorsAtStart = independentDistributors.Count;
		GD.Print($"CompetitorManager: independent distribution layer -- {independentDistributors.Count} houses " +
			$"across {independentDistributorsByRegion.Count} regions");
	}

	/// <summary>
	/// The independent route to national reach (handoff section 33). A label with a record
	/// proven in some market places its line with a wholesale house there, and then with
	/// houses in bordering markets, one market at a time. This is what an independent label
	/// actually did in the 1960s, and until it existed the only way to reach the country was
	/// to become a bigger label's client -- which put a floor under major-owned chart share
	/// that no calibration could move (section 32.5).
	///
	/// Deliberately grants coverage and nothing else. No DistributionDeal is created, no
	/// reach is borrowed, no masters change hands, and nothing enters the acquisition chain,
	/// so a record distributed this way is attributable to nobody.
	/// </summary>
	private void PursueIndependentDistribution(AILabel label) {
		if (label == null || !label.IsActive || independentDistributors.Count == 0) return;
		// A Major runs its own branch distribution and does not place its line with a
		// regional wholesaler.
		if (label.tier == LabelTier.Major) return;
		// Under a P&D contract the distributor presses and ships; the label is not also
		// building wholesale relationships. Markets it already holds stay held -- they are
		// its own asset and are still there when the contract ends.
		if (label.activeDeal != null || label.IsSubsidiary) return;
		// Section 27: a dependent hitmaker leans on its distributor rather than building
		// its own network, which is what keeps it a high-dependency absorption target.
		if (label.distributionDependentHitmaker) return;

		var proven = GetProvenBreakoutRegions(label);
		if (proven.Count == 0) return;

		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		string target = SelectIndependentDistributionTarget(label, proven, currentWeek);
		if (target == null) return;

		IndependentDistributor house = SelectIndependentHouse(label, target, currentWeek);
		if (house == null || !house.AddClient(label.labelId)) return;

		label.independentDistributionRegions.Add(target);
		float coveredShare = ChartManager.Instance?.GetNationalMarketShareForRegions(label.AllCoveredRegions()) ?? 0f;
		float before = label.ownedReach;
		// Credit only the market this placement actually opened. Re-deriving owned reach from
		// total coverage instead paid a label for the regions it was generated with, so the
		// first placement handed the largest existing networks a windfall and the channel
		// inflated MidTier incumbents rather than opening the chart to independents -- the
		// section 12 trap in miniature, caught in the first 52-week measurement. One house
		// reaches much of a market's retail but not all of it, hence the coverage factor.
		float marginalShare = ChartManager.Instance?.GetNationalMarketShareForRegions(new[] { target }) ?? 0f;
		label.ownedReach = Mathf.Min(SelfBuiltReachCeiling,
			label.ownedReach + (marginalShare * independentCoverageReachFactor));

		OnIndependentDistributionSigned?.Invoke(new IndependentDistributionTelemetry {
			week = currentWeek,
			labelId = label.labelId,
			labelName = label.labelName,
			labelTier = label.tier,
			distributorId = house.distributorId,
			distributorName = house.distributorName,
			regionId = target,
			provenInRegion = proven.Contains(target),
			coveredRegionCount = label.independentDistributionRegions.Count,
			coveredMarketShare = coveredShare,
			ownedReachBefore = before,
			ownedReachAfter = label.ownedReach,
			houseClientCount = house.CurrentClientCount,
			houseClientCapacity = house.clientCapacity
		});
	}

	/// <summary>
	/// Markets where this label has actually proven a record. A wholesaler took a line it
	/// could sell, so the evidence bar is the same regional breakout that earns a P&amp;D
	/// offer -- not the profit the old self-built gate demanded, which a label with no
	/// distribution could never earn (section 32.5).
	/// </summary>
	private HashSet<string> GetProvenBreakoutRegions(AILabel label) {
		var proven = new HashSet<string>(System.StringComparer.Ordinal);
		foreach (RecordRuntimeData record in ChartManager.Instance?.GetAllRecords() ?? Enumerable.Empty<RecordRuntimeData>()) {
			if (record?.baseRecord?.labelId != label.labelId) continue;
			foreach (var pair in record.regionalData) {
				if ((pair.Value?.peakBreakoutScore ?? 0f) >= regionalBreakoutDealThreshold) proven.Add(pair.Key);
			}
		}
		return proven;
	}

	// Proven markets first, then the markets bordering them. A house that took the line had
	// standing arrangements with its peers one region over, which is how a regional hit
	// spread before it was a national one.
	private string SelectIndependentDistributionTarget(AILabel label, HashSet<string> proven, int currentWeek) {
		// Only markets that can actually be placed in are candidates. Counting a market with
		// no independent trade -- the Rockies -- would have consumed the month's opportunity
		// and then failed, which left all 18 Rockies-home labels unable to place a single
		// line. A label in a market without wholesalers has to reach the ones next door.
		var candidates = new List<string>();
		foreach (string regionId in proven)
			if (CanPlaceLineIn(label, regionId)) candidates.Add(regionId);
		if (candidates.Count == 0) {
			foreach (string regionId in proven)
				foreach (string neighbour in DistanceModel.GetAdjacentRegions(regionId))
					if (CanPlaceLineIn(label, neighbour) && !candidates.Contains(neighbour)) candidates.Add(neighbour);
		}
		if (candidates.Count == 0) return null;

		// Placing a line took months of travelling and pitching, so a label adds markets
		// steadily rather than going national the month after a hit.
		float roll = GetDeterministicIndependentDistributionRoll(label.labelId, "target", currentWeek);
		if (roll >= independentDistributionMonthlyChance) return null;
		int index = (int)(GetDeterministicIndependentDistributionRoll(label.labelId, "pick", currentWeek) * candidates.Count);
		return candidates[Mathf.Clamp(index, 0, candidates.Count - 1)];
	}

	// A house drops a dead label's line and the slot returns to the market. Without this the
	// layer would fill with closed labels and saturate on ghosts rather than on real demand.
	private void ReleaseIndependentDistribution(AILabel label) {
		if (label == null || label.independentDistributionRegions.Count == 0) return;
		foreach (IndependentDistributor house in independentDistributors) house.RemoveClient(label.labelId);
		label.independentDistributionRegions.Clear();
	}

	/// <summary>
	/// Finds a Major that is full but would rather carry this proven client than its own weakest
	/// imprint, and names the imprint it drops. The candidate must still add a region the client
	/// does not have and must be able to fund the advance -- only the client ceiling is waived.
	/// The imprint dropped is the one charting least; a Major will not drop a client that is
	/// selling at least as well as the one being courted.
	/// </summary>
	private (AILabel Major, AILabel Dropped) SelectMajorWillingToDropWeakestClient(AILabel client) {
		int courtedCharting = GetRecentChartingRecordCount(client.labelId);
		AILabel bestMajor = null, bestDrop = null;
		int bestDropCharting = int.MaxValue;
		foreach (AILabel major in aiLabels) {
			if (major.tier != LabelTier.Major || !major.IsActive || major == client) continue;
			if (WouldCreateCircularDeal(client, major)) continue;
			if (major.cashReserves <= major.GetMonthlyOverhead() * 3f) continue;
			if (!(major.distributionRegions ?? System.Array.Empty<string>())
				.Any(region => !client.HasDistributionInRegion(region))) continue;
			foreach (AILabel held in aiLabels) {
				if (held.activeDeal?.distributorId != major.labelId || held == client) continue;
				if (held.IsSubsidiary) continue;
				int charting = GetRecentChartingRecordCount(held.labelId);
				if (charting >= courtedCharting || charting >= bestDropCharting) continue;
				bestMajor = major;
				bestDrop = held;
				bestDropCharting = charting;
			}
		}
		return (bestMajor, bestDrop);
	}

	private bool CanPlaceLineIn(AILabel label, string regionId) =>
		!label.HasDistributionInRegion(regionId) &&
		GetIndependentDistributorsInRegion(regionId).Any(house => house.HasCapacity && !house.CarriesLabel(label.labelId));

	private IndependentDistributor SelectIndependentHouse(AILabel label, string regionId, int currentWeek) {
		var open = GetIndependentDistributorsInRegion(regionId)
			.Where(house => house.HasCapacity && !house.CarriesLabel(label.labelId))
			.ToList();
		if (open.Count == 0) return null;
		int index = (int)(GetDeterministicIndependentDistributionRoll(label.labelId, "house" + regionId, currentWeek) * open.Count);
		return open[Mathf.Clamp(index, 0, open.Count - 1)];
	}

	// Seed-stable and drawn off the global RNG stream, exactly as the masters renewal roll
	// is (section 30.1). A new decision route that consumed global draws would reorder every
	// downstream sampler, and the resulting breadth and tier changes could not be attributed
	// to independent distribution rather than to RNG reordering (section 12).
	internal static float GetDeterministicIndependentDistributionRoll(string labelId, string salt, int week) {
		uint hash = 2166136261u;
		foreach (char value in
			$"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{labelId}|{salt}|{week}|IndependentDistributionV1") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	public IReadOnlyList<IndependentDistributor> GetIndependentDistributors() => independentDistributors;

	public IReadOnlyList<IndependentDistributor> GetIndependentDistributorsInRegion(string regionId) =>
		!string.IsNullOrEmpty(regionId) && independentDistributorsByRegion.TryGetValue(regionId, out var houses)
			? houses
			: (IReadOnlyList<IndependentDistributor>)System.Array.Empty<IndependentDistributor>();

	private void EnsureChartRecordSubscription() {
		ChartManager source = ChartManager.Instance;
		if (source == null || source == chartRecordEventSource) return;
		if (chartRecordEventSource != null)
			chartRecordEventSource.OnRecordChartUpdated -= OnRecordChartUpdated;
		source.OnRecordChartUpdated += OnRecordChartUpdated;
		chartRecordEventSource = source;
	}
	
	private void PopulateInitialRecords() {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate <= TimeManager.Instance.CurrentDate) {
					ChartManager.Instance.ReleaseRecord(record);
				}
			}
		}
		
		int historicalCount = historicalRecords?.Count(r => r.releaseDate <= TimeManager.Instance.CurrentDate) ?? 0;
		int needed = targetActiveRecords - historicalCount;
		
		var releaseQuotas = CalculateInitialQuotas(needed);
		
		foreach (var label in aiLabels) {
			if (!releaseQuotas.TryGetValue(label.labelId, out int quota)) continue;
			for (int i = 0; i < quota; i++) {
				if (label.roster.Count == 0) continue;
				var artist = label.roster[(int)GD.RandRange(0, label.roster.Count - 1)];
				// PREWARM SEEDING (2026-08, WIP task B): the opening catalog was 100% singles, so the album
				// channel was empty at week 1 and 1960 read low LP unit share. Seed a genre-realistic share
				// as the pre-existing 1960 LP catalog (jazz, classical, mood/MOR, Broadway); the album
				// affinity skew makes adult genres seed albums often and teen/rock rarely, matching the era.
				Genre seedGenre = GenreCatalog.MapLegacy(artist.primaryGenre, year);
				var format = GD.Randf() < MarketRegion.GetAlbumSeedAffinity(seedGenre)
					? ReleaseFormat.Album : ReleaseFormat.Single;
				var record = GenerateRecordFromArtist(label, artist, year, format);
				int weeksAgo = (int)GD.RandRange(1, 20);
				record.releaseDate = TimeManager.Instance.CurrentDate.SubtractWeeks(weeksAgo);
				ChartManager.Instance.ReleaseRecord(record);
				BootstrapPrewarmRecord(record, artist, label, weeksAgo);
				TrackRelease(label.labelId, record.recordId);
				artist.totalReleases++;
				artist.weeksSinceLastRelease = weeksAgo;
				artist.releaseHistory.Add(record.recordId);
				// Seeded albums must not enter releasedSingleIds -- it feeds compilation track resolution
				// and the single-oriented catalog, neither of which should see an LP.
				if (format != ReleaseFormat.Album) artist.releasedSingleIds.Add(record.recordId);
			}
		}
		if (debugMode) GD.Print($"CompetitorManager: Populated {needed} initial records from rosters");
	}
	
	private Dictionary<string, int> CalculateInitialQuotas(int totalNeeded) {
		var quotas = new Dictionary<string, int>();
		float totalWeight = aiLabels.Sum(l => GetTierWeight(l.tier) * l.roster.Count);
		if (totalWeight <= 0) {
			int perLabel = totalNeeded / Mathf.Max(1, aiLabels.Count);
			foreach (var label in aiLabels) quotas[label.labelId] = perLabel;
			return quotas;
		}
		foreach (var label in aiLabels) {
			float weight = GetTierWeight(label.tier) * label.roster.Count;
			int quota = Mathf.RoundToInt((weight / totalWeight) * totalNeeded);
			quota = Mathf.Min(quota, label.roster.Count * 3);
			quotas[label.labelId] = quota;
		}
		return quotas;
	}
	
	private float GetTierWeight(LabelTier tier) => tier switch {
		LabelTier.Major => 5f, LabelTier.MidTier => 3f, LabelTier.Independent => 2f,
		LabelTier.Small => 1f, LabelTier.Boutique => 1.5f, _ => 1f
	};
	
	private void BootstrapPrewarmRecord(Record record, SimulatedArtist artist, AILabel label, int weeksOld) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		
		float quality = runtimeData.GetQuality();
		float ageFactor = Mathf.Pow(0.92f, weeksOld);
		
		float campaignImpact = ChartSimulator.GetCampaignImpact(label);
		runtimeData.awareness = Mathf.Clamp(0.15f + (artist.reputation * 0.3f) + (artist.momentum * 0.2f) + (campaignImpact * 0.2f * ageFactor), 0f, 1f);
		runtimeData.radioHeat = Mathf.Clamp((quality * 0.4f + campaignImpact * 0.3f) * ageFactor, 0f, 1f);
		// PREWARM SEEDING (2026-08, WIP task B): seeded albums represent the established pre-1960 LP
		// catalog (jazz, classical, mood/MOR, Broadway) that was ALREADY selling, not fresh drops. Give
		// them a catalog-staple awareness floor. NOTE: 1960 is channel/supply-bound, so this barely moved
		// LP unit share -- kept for continuation, not a proven lever.
		if (record.format == ReleaseFormat.Album)
			runtimeData.awareness = Mathf.Max(runtimeData.awareness, albumPrewarmAwarenessFloor);
		
		if (quality > 0.7f && GD.Randf() < 0.4f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(2, weeksOld);
			runtimeData.peakPosition = (int)GD.RandRange(10, 60);
		} else if (quality > 0.5f && GD.Randf() < 0.3f) {
			runtimeData.weeksOnChart = (int)GD.RandRange(1, weeksOld / 2);
			runtimeData.peakPosition = (int)GD.RandRange(40, 90);
		}
		
		var regions = ChartManager.Instance.GetAllRegions();
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			bool isStrongRegion = label.strongRegions?.Contains(region.regionId) ?? false;
			float regionMod = isStrongRegion ? 1.4f : 1f;
			
			regionalData.awareness = runtimeData.awareness * regionMod * (float)GD.RandRange(0.7, 1.1);
			regionalData.radioPlay = runtimeData.radioHeat * regionMod * (float)GD.RandRange(0.6, 1.0);
			regionalData.sentiment = 0.5f + (quality * 0.3f) + (float)GD.RandRange(-0.1, 0.15);
			// Prewarm stock formerly bypassed the physical distribution model outright,
			// seeding 5,000-20,000 units into every region regardless of the label's
			// tier, owned reach, or whether it had any distribution there at all. A
			// live Small-label release into an uncovered region receives roughly 530
			// units from the same shelf the chart reads, so the seeded 1960 cohort
			// began with national distribution no live entrant could ever obtain.
			// Route prewarm through the same function live releases use.
			int shelf = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId, 1f, 1f, record.recordId);
			// Established-catalog albums carry deeper shelf stock than a fresh small-label single (multiplier
			// currently 1.0 = neutral; the ~530-unit small-label shelf otherwise stock-caps a catalog staple).
			float prewarmStock = record.format == ReleaseFormat.Album ? shelf * albumPrewarmStockMultiplier : shelf;
			regionalData.unitsInStores = Mathf.RoundToInt(prewarmStock * (float)GD.RandRange(0.7, 1.1));
			// A record already this old has sold part of its shelf through.
			regionalData.unitsSoldTotal = Mathf.RoundToInt(shelf * (1f - ageFactor) * (float)GD.RandRange(0.6, 1.2));
		}
	}
	
	private void OnWeekEnded(GameDate date) {
		long profileStart = SimulationPerformanceProfiler.Begin();
		OnWeekEndedCore(date);
		SimulationPerformanceProfiler.EndCompetitorWeek(profileStart);
	}

	private void OnWeekEndedCore(GameDate date) {
		if (historicalRecords != null) {
			foreach (var record in historicalRecords) {
				if (record.releaseDate == date) {
					ChartManager.Instance.ReleaseRecord(record);
					GD.Print($"Historical release: {record.title} by {record.artistName}");
				}
			}
		}
		// The disabled route is a byte-frozen compatibility boundary. It retains
		// the historical prior-week booking timing and never consumes settlement
		// state, spillover, or responsive-memory observations.
		if (!GenreMarketV2.Enabled) ProcessWeeklyRevenue(CreateLegacyRevenueSettlement(date));
		pipelineWeek++;
		ResetWeeklyReleaseCounters();
		ProcessDueAlbumProjects(date);
		ProcessWeeklyReleases(date);
		ProcessWeeklySoundtrackOrigination(date);
	}

	/// <summary>Explicit, ordered booking transition for a frozen live settlement.</summary>
	public void BookCompletedWeekSettlement(ChartManager.CompletedWeekSettlement settlement) {
		if (settlement == null || !GenreMarketV2.Enabled || ChartManager.Instance?.IsGenreMarketV2Live != true)
			throw new System.InvalidOperationException("Attempted to book a non-live settlement.");
		if (settlement.IsBooked || settlement.SettlementId <= 0 || settlement.SettlementId != lastBookedSettlementId + 1)
			throw new System.InvalidOperationException($"Rejected duplicate, stale, skipped, or out-of-order settlement {settlement.SettlementId} after {lastBookedSettlementId}.");
		ProcessWeeklyRevenue(settlement);
		var projectMemoryUpdates = new HashSet<string>(System.StringComparer.Ordinal);
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.Entries) {
			RecordRuntimeData record = entry.Record;
			if (record?.revenueMemoryEligible != true) continue;
			int age = record.weeksSinceRelease;
			if (age == 13 || age == 26 || (record.baseRecord.format == ReleaseFormat.Album && age == 52))
				UpdateApplicableResponsiveMemoryObservations(record, finalized: false);
			if (!string.IsNullOrEmpty(record.albumProjectId) &&
				record.projectRole is ProjectRecordRole.PromoSingle or ProjectRecordRole.LinkedAlbum &&
				(age == 13 || age == 26 || (record.baseRecord.format == ReleaseFormat.Album && age == 52)))
				projectMemoryUpdates.Add(record.albumProjectId);
		}
		foreach (string projectId in projectMemoryUpdates) if (projectById.TryGetValue(projectId, out AlbumProject project))
			UpdateAlbumWithPromoProjectMemoryObservation(project, finalized: false);
		settlement.IsBooked = true;
		lastBookedSettlementId = settlement.SettlementId;
	}

	private ChartManager.CompletedWeekSettlement CreateLegacyRevenueSettlement(GameDate date) => new() {
		SettlementId = -1,
		Date = date,
		Entries = ChartManager.Instance?.GetAllRecords().Select(record => new ChartManager.CompletedWeekSettlementEntry {
			Record = record, RecordId = record.baseRecord.recordId, LabelId = record.baseRecord.labelId,
			Format = record.baseRecord.format, Units = record.unitsThisWeek
		}).ToArray() ?? System.Array.Empty<ChartManager.CompletedWeekSettlementEntry>()
	};
	
	private void ProcessWeeklyRevenue(ChartManager.CompletedWeekSettlement settlement) {
		weeklyRevenueByLabelAndFormat.Clear();
		foreach (var label in aiLabels) {
			label.weeklyGrossRevenue = 0f;
			label.weeklyCogs = 0f;
			label.weeklyDistributionSkim = 0f;
			label.weeklyArtistRoyalty = 0f;
			label.weeklyNetRevenue = 0f;
			label.weeklyDistributionIncome = 0f;
		}
		foreach (var label in aiLabels) {
			// A frozen enabled settlement may still contain a record from a label whose
			// lifecycle status changed earlier in the week. It remains an economic fact
			// and must be booked exactly once before retirement. The legacy route keeps
			// its historical inactive-label exclusion for byte-identical replay.
			if (!label.IsActive && settlement.SettlementId < 1) continue;
			float weeklyRevenue = CalculateLabelRevenue(label, settlement);
			// Billings against a wholesale house are booked but not banked: the house pays on
			// its own terms, and only for what it admits it sold.
			weeklyRevenue -= DeferWholesaleBillings(label, settlement, weeklyRevenue);
			weeklyRevenue += CollectMaturedWholesaleReceivables(label);
			label.cashReserves += weeklyRevenue;
			label.monthlyRevenue += weeklyRevenue;
			if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
				financials.lastMonthRevenue += weeklyRevenue;
			}
		}
	}

	/// <summary>
	/// Moves this week's revenue earned in wholesale-served markets out of cash and into
	/// receivables (handoff section 33.1 stage 3). Returns the amount deferred. Revenue from
	/// markets the label ships to itself, and from anything a distribution contract carries,
	/// is unaffected -- this is the wholesale channel's payment behaviour, not a general tax.
	/// </summary>
	private float DeferWholesaleBillings(AILabel label, ChartManager.CompletedWeekSettlement settlement, float weeklyRevenue) {
		if (weeklyRevenue <= 0f || label.independentDistributionRegions.Count == 0 || settlement?.Entries == null) return 0f;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		long wholesaleUnits = 0, totalUnits = 0;
		var unitsByRegion = new Dictionary<string, long>(System.StringComparer.Ordinal);
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.EntriesForLabel(label.labelId)) {
			if (entry.Regions == null) continue;
			foreach (ChartManager.CompletedWeekSettlementRegion region in entry.Regions) {
				if (region.FinalCleared <= 0) continue;
				totalUnits += region.FinalCleared;
				if (!label.independentDistributionRegions.Contains(region.RegionId)) continue;
				wholesaleUnits += region.FinalCleared;
				unitsByRegion[region.RegionId] = unitsByRegion.GetValueOrDefault(region.RegionId) + region.FinalCleared;
			}
		}
		if (wholesaleUnits <= 0 || totalUnits <= 0) return 0f;

		float deferred = 0f;
		foreach (var pair in unitsByRegion) {
			IndependentDistributor house = GetIndependentDistributorsInRegion(pair.Key)
				.FirstOrDefault(candidate => candidate.CarriesLabel(label.labelId));
			if (house == null) continue;
			float billed = weeklyRevenue * (pair.Value / (float)totalUnits);
			if (billed <= 0f) continue;
			// Under-reporting was endemic: the label is billed for what the house admits it
			// sold, never sees the rest, and never knows it existed. The return allowance is
			// deliberately NOT applied here -- returns are units that shipped and did not
			// sell, and the settlement this bills against is already units sold, so charging
			// it again would take the same loss twice.
			float collectable = billed * house.reportingHonesty;
			label.wholesaleReceivables.Add(new WholesaleReceivable(
				currentWeek + house.paymentTermWeeks, house.distributorId, collectable));
			label.outstandingWholesaleReceivables += collectable;
			label.lifetimeWholesaleWriteOffs += billed - collectable;
			deferred += billed;
		}
		return deferred;
	}

	/// <summary>
	/// Pays out receivables whose terms have run out. A house that is a poor payer settles
	/// short rather than late -- the label writes the difference off, as it did.
	/// </summary>
	private float CollectMaturedWholesaleReceivables(AILabel label) {
		if (label.wholesaleReceivables.Count == 0) return 0f;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		float collected = 0f;
		for (int index = label.wholesaleReceivables.Count - 1; index >= 0; index--) {
			WholesaleReceivable receivable = label.wholesaleReceivables[index];
			if (receivable.DueWeek > currentWeek) continue;
			label.wholesaleReceivables.RemoveAt(index);
			label.outstandingWholesaleReceivables -= receivable.Amount;
			IndependentDistributor house = independentDistributors
				.FirstOrDefault(candidate => candidate.distributorId == receivable.DistributorId);
			// Most invoices were eventually settled -- the damage was the wait, already
			// charged by the term above. Reliability is the residue: the slow payer settles
			// short and the label writes the rest off. Treating it as a flat pay-rate instead
			// compounded with under-reporting into a ~40% realisation, which is not a squeeze,
			// it is an economy that cannot run.
			float reliability = Mathf.Clamp(house?.reliability ?? 1f, 0f, 1f);
			float paid = receivable.Amount * (reliability + ((1f - reliability) * WholesaleSettledShareOfArrears));
			collected += paid;
			label.lifetimeWholesaleWriteOffs += receivable.Amount - paid;
		}
		label.outstandingWholesaleReceivables = Mathf.Max(0f, label.outstandingWholesaleReceivables);
		return collected;
	}
	
	private float CalculateLabelRevenue(AILabel label, ChartManager.CompletedWeekSettlement settlement) {
		if (settlement?.Entries == null) return 0f;
		long profileStart = SimulationPerformanceProfiler.Begin();
		float totalRevenue = 0f;
		
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.EntriesForLabel(label.labelId)) {
			long lookupProfileStart = SimulationPerformanceProfiler.Begin();
			var runtimeData = entry.Record;
			SimulationPerformanceProfiler.EndRecordLookup(lookupProfileStart);
			if (runtimeData == null) continue;
			
			float weeklyUnits = entry.Units;
			ReleaseFormat format = runtimeData.baseRecord.format;
			float pricePerUnit = GetPricePerUnit(format);
			float pressingCost = GetPressingCostPerUnit(format);
			if (format == ReleaseFormat.Album) pressingCost += albumPackagingCostPerUnit * (runtimeData.baseRecord.album?.packaging ?? 0f);
			var artist = ArtistManager.Instance?.GetArtist(runtimeData.baseRecord.artistId);
			float artistRoyalty = artist?.royaltyRate ?? 0.05f;
			float skimFraction = GetSettlementDistributionSkimFraction(label, runtimeData, weeklyUnits,
				liveSettlement: GenreMarketV2.Enabled && settlement.SettlementId > 0);
			float retailGross = weeklyUnits * pricePerUnit;
			// DISTANCE-4B: 4a sums by current-region hub with a neutral cost factor; 4b
			// makes manufacturing margin, skim basis, and artist recoupment region-weighted.
			float cogs = CalculateRegionalCogs(label, runtimeData, pressingCost);
			float grossAfterCogs = Mathf.Max(0f, retailGross - cogs);
			float skimAmount = grossAfterCogs * skimFraction;
			// Keep the existing artist contract convention (royalty on retail). The
			// distribution skim is based on revenue after manufacturing cost.
			float artistPayment = retailGross * artistRoyalty;
			float recordRevenue = grossAfterCogs - skimAmount - artistPayment;
			entry.Gross = retailGross;
			entry.ManufacturingCost = cogs;
			entry.ArtistRoyalty = artistPayment;
			entry.DistributionSkim = skimAmount;
			entry.LabelNet = recordRevenue;
			entry.MarketNet = recordRevenue;
			entry.DistributionIncome = 0f;
			entry.DistributionRecipientLabelId = string.Empty;
			entry.BookedCount = 1;
			runtimeData.lifetimeLabelNet += recordRevenue;
			totalRevenue += recordRevenue;
			label.weeklyGrossRevenue += retailGross;
			label.weeklyCogs += cogs;
			label.weeklyDistributionSkim += skimAmount;
			label.weeklyArtistRoyalty += artistPayment;
			label.weeklyNetRevenue += recordRevenue;
			RevenueTelemetry formatRevenue = GetOrCreateRevenueTelemetry(label.labelId, format);
			formatRevenue.gross += retailGross;
			formatRevenue.cogs += cogs;
			formatRevenue.distributionSkim += skimAmount;
			formatRevenue.artistRoyalty += artistPayment;
			formatRevenue.labelNet += recordRevenue;
			RouteDistributionSkim(label, skimAmount, format, entry);
			
			if (artist != null) {
				float recouped = Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), artistPayment);
				artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
				artist.totalRoyaltyEarnings += artistPayment - recouped;
			}
			
		}
		
		SimulationPerformanceProfiler.EndCalculateLabelRevenue(profileStart);
		return totalRevenue;
	}

	/// <summary>
	/// A distribution contract may skim only the regions it actually grants.
	/// Applying its margin to the client's owned or otherwise ungranted sales
	/// transfers unrelated label revenue to the distributor. The frozen no-deal
	/// route retains its established owned-reach formula.
	/// </summary>
	private static float GetSettlementDistributionSkimFraction(AILabel label,
		RecordRuntimeData runtimeData, float weeklyUnits, bool liveSettlement) {
		if (!liveSettlement) return GetLegacyDistributionSkimFraction(label);
		IEnumerable<KeyValuePair<string, int>> regionalUnits = runtimeData?.regionalData?
			.Select(pair => new KeyValuePair<string, int>(pair.Key,
				Mathf.Max(0, pair.Value?.unitsSoldThisWeek ?? 0)));
		return GetSettlementDistributionSkimFraction(label, regionalUnits, weeklyUnits);
	}

	private static float GetLegacyDistributionSkimFraction(AILabel label) =>
		label?.activeDeal != null
			? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
			: 0.25f * (1f - (label?.ownedReach ?? 0f));

	private static float GetSettlementDistributionSkimFraction(AILabel label,
		IEnumerable<KeyValuePair<string, int>> regionalUnits, float weeklyUnits) {
		if (label?.activeDeal == null) return 0.25f * (1f - Mathf.Clamp(label?.ownedReach ?? 0f, 0f, 1f));
		if (weeklyUnits <= 0f || regionalUnits == null) return 0f;
		var granted = new HashSet<string>(label.activeDeal.grantedRegions ?? System.Array.Empty<string>(),
			System.StringComparer.Ordinal);
		int grantedUnits = regionalUnits
			.Where(pair => granted.Contains(pair.Key))
			.Sum(pair => Mathf.Max(0, pair.Value));
		float grantedShare = Mathf.Clamp(grantedUnits / weeklyUnits, 0f, 1f);
		return Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f) * grantedShare;
	}

	internal static float GetSettlementDistributionSkimFractionForProbe(AILabel label,
		IReadOnlyDictionary<string, int> regionalUnits, int totalUnits, bool liveSettlement = true) =>
		liveSettlement
			? GetSettlementDistributionSkimFraction(label, regionalUnits, totalUnits)
			: GetLegacyDistributionSkimFraction(label);

	private float CalculateRegionalCogs(AILabel label, RecordRuntimeData runtimeData, float pressingCost) {
		if (runtimeData.regionalData == null || runtimeData.regionalData.Count == 0) {
			return runtimeData.unitsThisWeek * pressingCost * DistanceModel.GetDistributionCostFactor(label, label.headquartersCity);
		}

		float cogs = 0f;
		int regionalUnits = 0;
		foreach (var pair in runtimeData.regionalData) {
			int units = pair.Value?.unitsSoldThisWeek ?? 0;
			if (units <= 0) continue;
			regionalUnits += units;
			string regionHubCity = DistanceModel.GetHubCityIdForRegion(pair.Key);
			cogs += units * pressingCost * DistanceModel.GetDistributionCostFactor(label, regionHubCity);
		}

		int unassignedUnits = runtimeData.unitsThisWeek - regionalUnits;
		if (unassignedUnits != 0) {
			cogs += unassignedUnits * pressingCost * DistanceModel.GetDistributionCostFactor(label, label.headquartersCity);
		}
		return cogs;
	}

	private float GetPricePerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pricePerUnitByFormat != null && pricePerUnitByFormat.TryGetValue(key, out float price)
			? price
			: 0.89f;
	}

	public float GetPricePerUnitForAudit(ReleaseFormat format) => GetPricePerUnit(format);

	private float GetPressingCostPerUnit(ReleaseFormat format) {
		string key = format.ToString();
		return pressingCostPerUnitByFormat != null && pressingCostPerUnitByFormat.TryGetValue(key, out float cost)
			? cost
			: 0.30f;
	}

	private RevenueTelemetry GetOrCreateRevenueTelemetry(string labelId, ReleaseFormat format) {
		var key = (labelId, format);
		if (!weeklyRevenueByLabelAndFormat.TryGetValue(key, out RevenueTelemetry telemetry)) {
			telemetry = new RevenueTelemetry();
			weeklyRevenueByLabelAndFormat[key] = telemetry;
		}
		return telemetry;
	}

	public IReadOnlyDictionary<(string LabelId, ReleaseFormat Format), RevenueTelemetry> GetWeeklyRevenueByLabelAndFormat() =>
		weeklyRevenueByLabelAndFormat;

	public void SetAlbumsEnabled(bool enabled) => enableAlbums = enabled;

	private void RouteDistributionSkim(AILabel client, float skimAmount, ReleaseFormat format,
		ChartManager.CompletedWeekSettlementEntry sourceEntry = null) {
		DistributionDeal deal = client.activeDeal;
		if (deal == null || skimAmount <= 0f) return;
		AILabel distributor = GetLabel(deal.distributorId);
		if (distributor == null || distributor == client) return;
		if (sourceEntry != null) {
			sourceEntry.DistributionIncome = skimAmount;
			sourceEntry.DistributionRecipientLabelId = distributor.labelId;
			sourceEntry.MarketNet = sourceEntry.LabelNet + skimAmount;
		}

		float recouped = Mathf.Min(Mathf.Max(0f, deal.unrecoupedAdvance), skimAmount);
		deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - recouped);
		distributor.cashReserves += skimAmount;
		distributor.monthlyRevenue += skimAmount;
		distributor.weeklyDistributionIncome += skimAmount;
		GetOrCreateRevenueTelemetry(distributor.labelId, format).distributionIncome += skimAmount;
		if (labelFinancials.TryGetValue(distributor.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthRevenue += skimAmount;
		}
	}
	
	private void ProcessWeeklyReleases(GameDate date) {
		int releasesThisWeek = 0;
		foreach (var label in aiLabels) {
			if (!label.IsActive) continue;
			if (label.roster.Count == 0) continue;
			
			float releaseChance = CalculateWeeklyReleaseChance(label, date.year, date.month);
			if (GD.Randf() < releaseChance) {
				WeeklyReleaseRollsFired++;
				if (TryReleaseRecord(label, date)) {
					releasesThisWeek++;
					WeeklySuccessfulReleases++;
					RecordReleaseAttempt(label.tier, success: true, artistSelectionFailure: false);
				} else {
					WeeklyFailedReleaseRolls++;
					RecordReleaseAttempt(label.tier, success: false, artistSelectionFailure: lastReleaseAttemptFailedArtistSelection);
					if (lastReleaseAttemptFailedArtistSelection) {
						WeeklyCooldownMismatchRolls++;
					}
				}
			}
		}
		if (debugMode && releasesThisWeek > 0) GD.Print($"Week {date}: {releasesThisWeek} new releases");
	}

	// Blockbuster soundtracks minted so far this run; the anti-monoculture cap (0-3/decade) is enforced
	// against this. A fresh CompetitorManager per audit run starts this at 0.
	private int soundtrackBlockbustersThisRun;
	public int SoundtrackOriginationsThisRun { get; private set; }

	// Externally-originated soundtrack/cast-album pipeline (D7 soundtrack subsystem, phase 3). Runs once
	// per week: with a small probability derived from the annual origination rate, generate one
	// opportunity, pick a capable licensee, and mint + release the Soundtrack album. Gated to the live
	// enabled market path -- the disabled route is a byte-frozen compatibility boundary and must never
	// see soundtracks. See SimTools/D7SoundtrackCastAlbumHandoff.md §3.2, §5 and ExternalMediaService.
	private void ProcessWeeklySoundtrackOrigination(GameDate date) {
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)) return;
		if (GD.Randf() >= ExternalMediaService.OriginationsForYear(date.year) / 52f) return;
		bool allowBlockbuster = soundtrackBlockbustersThisRun < ExternalMediaService.BlockbusterDecadeCap;
		ExternalMediaProfile profile = ExternalMediaService.GenerateProfile(date.year, allowBlockbuster);
		AILabel label = ExternalMediaService.SelectLabel(aiLabels, profile);
		if (label == null) return; // nobody could front the license advance this week
		MintSoundtrackRecord(label, profile, date);
	}

	private void MintSoundtrackRecord(AILabel label, ExternalMediaProfile profile, GameDate date) {
		Genre genre = ExternalMediaService.MapGenre(profile.sourceType);
		// Resolve the stored production-cost MULTIPLE into an actual currency advance against this
		// label's cost basis, charge it, and overwrite the field with the realized fee for telemetry.
		float licenseFee = label.GetProductionCost() * profile.upfrontLicenseFee;
		profile.upfrontLicenseFee = licenseFee;

		generatedRecordCounter++;
		float pooledAppeal = ExternalMediaService.PooledAppeal(profile);
		var album = new Album {
			albumId = $"album_{generatedRecordCounter}",
			albumFormat = AlbumFormat.Soundtrack,
			externalMedia = profile,
			trackRefs = System.Array.Empty<AlbumTrack>(),
			nonSingleTracks = System.Array.Empty<AlbumTrack>(),
			runtimeMinutes = (float)GD.RandRange(28.0, 46.0),
			thematicCohesion = Mathf.Clamp(0.6f + profile.criticalPrestige * 0.3f, 0f, 1f),
			pooledAppeal = pooledAppeal,
			packaging = Mathf.Clamp(0.45f + profile.boxOfficeTrajectory * 0.35f + (float)GD.RandRange(-0.08, 0.10), 0.2f, 1f),
			isStereo = date.year >= 1968 || GD.Randf() < Mathf.Lerp(0.2f, 0.8f, Mathf.Clamp((date.year - 1960f) / 8f, 0f, 1f))
		};

		var record = new Record {
			recordId = $"gen_{generatedRecordCounter}",
			labelId = label.labelId,
			format = ReleaseFormat.Album,
			isPlayerOwned = false,
			album = album,
			artistId = string.Empty, // externally originated -- not a roster artist
			artistName = SoundtrackCreditName(profile.sourceType),
			primaryGenre = genre,
			secondaryGenre = genre,
			// Album quality reads straight off pooledAppeal; mirror it onto the scalar fields so any
			// non-album code path still sees a coherent quality.
			hookStrength = pooledAppeal,
			productionQuality = pooledAppeal,
			danceability = pooledAppeal,
			projectRole = ProjectRecordRole.None,
			albumProjectId = string.Empty
		};
		record.title = NameGenerator.Instance?.GenerateSongTitle(genre, date.year, record.artistName) ?? $"Soundtrack {generatedRecordCounter}";

		// Licensing economics: a high upfront advance, booked like any production spend.
		label.cashReserves -= licenseFee;
		label.monthlyExpenses += licenseFee;
		WeeklyProductionSpend += licenseFee;
		WeeklyProductionEvents++;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) financials.lastMonthExpenses += licenseFee;

		record.releaseDate = date;
		ChartManager.Instance.ReleaseRecord(record, label);
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData != null) {
			// Anchor launch awareness to the film/show's own premiere buzz -- soundtracks are not
			// artist-heat driven, so without this seed they would never build awareness or chart.
			// (Phase 4 replaces this static seed with the box-office demand trajectory.)
			runtimeData.awareness = Mathf.Max(runtimeData.awareness, profile.sourcePopularity);
			runtimeData.sunkProductionCost = licenseFee;
			runtimeData.revenueMemoryEligible = false; // no artist/project memory to fold
			runtimeData.projectRole = ProjectRecordRole.None;
		}
		TrackRelease(label.labelId, record.recordId);

		soundtrackBlockbustersThisRun += profile.isBlockbuster ? 1 : 0;
		SoundtrackOriginationsThisRun++;
		if (debugMode) GD.Print($"Soundtrack minted: {record.title} ({genre}, {profile.sourceType}, bo={profile.boxOfficeTrajectory:F2}) by {label.labelName}, fee={licenseFee:F0}");
	}

	private static string SoundtrackCreditName(ExternalMediaSourceType sourceType) => sourceType switch {
		ExternalMediaSourceType.StageCast => "Original Broadway Cast",
		ExternalMediaSourceType.FilmScore => "Original Film Score",
		_ => "Original Soundtrack"
	};

	private void ResetWeeklyReleaseCounters() {
		weeklyReleaseLifecycleByTier.Clear();
		WeeklyReleaseRollsFired = 0;
		WeeklySuccessfulReleases = 0;
		WeeklyFailedReleaseRolls = 0;
		WeeklyCooldownMismatchRolls = 0;
		WeeklyPipelineAlbumDrops = 0;
		WeeklySingleReleases = 0;
		WeeklyAlbumProjectsScheduled = 0;
		WeeklyProductionSpend = 0f;
		WeeklyProductionEvents = 0;
		WeeklyMarketingSpend = 0f;
		WeeklyMarketingEvents = 0;
	}

	public ReleaseLifecycleFlow GetWeeklyReleaseLifecycleFlow(LabelTier tier) =>
		weeklyReleaseLifecycleByTier.TryGetValue(tier, out ReleaseLifecycleFlow flow) ? flow : default;

	private void RecordReleaseAttempt(LabelTier tier, bool success, bool artistSelectionFailure) {
		ReleaseLifecycleFlow current = GetWeeklyReleaseLifecycleFlow(tier);
		weeklyReleaseLifecycleByTier[tier] = new ReleaseLifecycleFlow(current.Attempts + 1,
			current.SuccessfulReleases + (success ? 1 : 0), current.ArtistSelectionFailures + (artistSelectionFailure ? 1 : 0));
	}
	
	private float CalculateWeeklyReleaseChance(AILabel label, int year, int month) {
		int availableArtists = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
			? label.CountArtistsEligibleForRelease(year)
			: label.roster.Count(a => a.weeksSinceLastRelease >= 10);
		float seasonalityMultiplier = MarketSeasonality.Enabled
			? MarketSeasonality.GetArtistAvailabilityMultiplier(year, month, liveTick: true)
			: 1f;
		return CalculateLabelReleaseCapacityChance(label.releasesPerMonth, label.status, availableArtists, seasonalityMultiplier);
	}

	/// <summary>
	/// Converts a label's modeled monthly release capacity into one weekly release
	/// opportunity. Calendar time must not expand this capacity implicitly; any
	/// future label growth belongs in an explicit investment/capability system.
	/// </summary>
	internal static float CalculateLabelReleaseCapacityChance(float releasesPerMonth, LabelStatus status,
		int availableArtists, float seasonalityMultiplier = 1f) {
		if (availableArtists <= 0) return 0f;
		float weeklyCapacity = Mathf.Max(0f, releasesPerMonth) / 4f;
		float statusMod = status switch {
			LabelStatus.Bankrupt => 0f, LabelStatus.Defunct => 0f, LabelStatus.Dying => 0.3f,
			LabelStatus.Struggling => 0.5f, LabelStatus.Stable => 1f, LabelStatus.Rising => 1.2f,
			LabelStatus.Acquired => 0.8f, _ => 1f
		};
		float availabilityMod = Mathf.Clamp((float)availableArtists / 3f, 0f, 1f);
		return Mathf.Clamp(weeklyCapacity * statusMod * availabilityMod * Mathf.Max(0f, seasonalityMultiplier), 0f, 1f);
	}

	public void RecordRetired(RecordRuntimeData runtimeData) {
		if (runtimeData?.baseRecord == null) return;
		string labelId = runtimeData.baseRecord.labelId;
		string recordId = runtimeData.baseRecord.recordId;
		if (!string.IsNullOrEmpty(labelId) && !string.IsNullOrEmpty(recordId) &&
			labelActiveRecords.TryGetValue(labelId, out var recordIds)) recordIds.Remove(recordId);
		if (!string.IsNullOrEmpty(labelId)) {
			if (!retiredLabelRecordHistory.TryGetValue(labelId, out List<LabelRecordHistoryEntry> history)) {
				history = new List<LabelRecordHistoryEntry>();
				retiredLabelRecordHistory[labelId] = history;
			}
			int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			history.Add(new LabelRecordHistoryEntry(
				Mathf.Max(0, currentWeek - Mathf.Max(0, runtimeData.weeksSinceRelease)),
				runtimeData.weeksOnChart > 0,
				runtimeData.peakPosition > 0 && runtimeData.peakPosition <= 40));
		}

		float realizedNet = runtimeData.lifetimeLabelNet - runtimeData.sunkProductionCost;
		OnReleaseOutcome?.Invoke(new ReleaseOutcomeTelemetry {
			labelId = labelId,
			recordId = recordId,
			format = runtimeData.baseRecord.format,
			genre = runtimeData.baseRecord.primaryGenre,
			memoryEligible = runtimeData.revenueMemoryEligible,
			lifetimeLabelNet = runtimeData.lifetimeLabelNet,
			sunkProductionCost = runtimeData.sunkProductionCost,
			realizedNet = realizedNet
		});

		if (!runtimeData.revenueMemoryEligible) return;
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			UpdateApplicableResponsiveMemoryObservations(runtimeData, finalized: true);
		if (!string.IsNullOrEmpty(runtimeData.albumProjectId) && projectById.TryGetValue(runtimeData.albumProjectId, out AlbumProject project)) {
			if (runtimeData.projectRole == ProjectRecordRole.PromoSingle) {
				project.promoRetired = true;
				project.heldPromoOutcome = realizedNet;
				project.promoOutcomeState = ProjectOutcomeState.Retired;
				if (runtimeData.peakPosition > 0) project.promoPeakAtDrop = runtimeData.peakPosition;
				if (project.terminalState == AlbumProjectTerminalState.Cancelled) RedirectCancelledPromoOutcome(project);
				else TryFoldProjectMemory(project);
				return;
			}
			if (runtimeData.projectRole is ProjectRecordRole.LinkedAlbum or ProjectRecordRole.StandaloneAlbum) {
				project.rawDemandBeforeCannibalization = runtimeData.rawAlbumDemandBeforeCannibalization;
				project.suppressedDemand = runtimeData.suppressedAlbumDemand;
				project.demandWithActiveLinkedPromo = runtimeData.albumDemandWithActiveLinkedPromo;
				project.demandWithInactiveLinkedPromo = runtimeData.albumDemandWithInactiveLinkedPromo;
				project.demandWeightedSingleHeat = runtimeData.albumDemandWeightedSingleHeat;
				project.demandWeightedSubstitutionPropensity = runtimeData.albumDemandWeightedSubstitutionPropensity;
				project.demandWeightedSuppression = runtimeData.albumDemandWeightedSuppression;
				project.albumRetired = true;
				project.heldAlbumOutcome = realizedNet;
				project.albumOutcomeState = ProjectOutcomeState.Retired;
				TryFoldProjectMemory(project);
				return;
			}
		}
		// The release keyed residual is authoritative only in the enabled live
		// route. The frozen disabled route retains its historical EMA update.
		if (!GenreMarketV2.Enabled) ApplyMemoryObservation(labelId, runtimeData.baseRecord.format, realizedNet);
	}

	private void ApplyMemoryObservation(string labelId, ReleaseFormat format, float realizedNet) {
		// Retained only for project-state compatibility and diagnostics. The live
		// decision path uses UpdateResponsiveMemoryObservation instead.
		AILabel label = GetLabel(labelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(format);
		float alpha = Mathf.Clamp(revenueMemoryAlpha, 0f, 1f);
		memory.emaNetPerRelease = memory.releasesObserved == 0 ? realizedNet : Mathf.Lerp(memory.emaNetPerRelease, realizedNet, alpha);
		memory.releasesObserved++;
	}

	private void UpdateApplicableResponsiveMemoryObservations(RecordRuntimeData runtimeData, bool finalized) {
		if (runtimeData == null) return;
		foreach (RevenueEstimatorLane lane in GetApplicableEstimatorLanes(runtimeData.projectRole))
			UpdateResponsiveMemoryObservation(runtimeData, lane, finalized);
	}

	internal static RevenueEstimatorLane[] GetApplicableEstimatorLanes(ProjectRecordRole role) => role switch {
		ProjectRecordRole.OrphanSingle => new[] { RevenueEstimatorLane.OrphanSingle },
		ProjectRecordRole.PromoSingle => new[] { RevenueEstimatorLane.PromoSingle },
		// One physical outcome is applicable to both general Album eligibility
		// and the standalone-strategy estimator. Finance is still posted once.
		ProjectRecordRole.StandaloneAlbum => new[] { RevenueEstimatorLane.AlbumComponent, RevenueEstimatorLane.StandaloneAlbum },
		// Combined promo-plus-Album economics are observed separately by project
		// memory; the physical Album component owns eligibility feedback.
		ProjectRecordRole.LinkedAlbum => new[] { RevenueEstimatorLane.AlbumComponent },
		_ => System.Array.Empty<RevenueEstimatorLane>()
	};

	private void UpdateResponsiveMemoryObservation(RecordRuntimeData runtimeData, RevenueEstimatorLane lane, bool finalized) {
		if (runtimeData?.baseRecord == null || string.IsNullOrEmpty(runtimeData.baseRecord.labelId)) return;
		AILabel label = GetLabel(runtimeData.baseRecord.labelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(lane);
		string releaseId = runtimeData.baseRecord.recordId;
		int age = Mathf.Max(0, runtimeData.weeksSinceRelease);
		float expectedNet = runtimeData.releaseTimeExpectedNet;
		float scale = Mathf.Max(1f, Mathf.Max(runtimeData.releaseTimeOpportunityScale, Mathf.Abs(expectedNet)));
		float terminalAge = runtimeData.baseRecord.format == ReleaseFormat.Album ? 52f : 20f;
		float maturity = finalized ? 1f : Mathf.Clamp((age + 1f) / terminalAge, .05f, 1f);
		float realizedToDate = runtimeData.lifetimeLabelNet - runtimeData.sunkProductionCost;
		// Production is a one-time sunk cost. Annualize only the accumulating
		// revenue, then subtract that cost once; dividing realized net by maturity
		// repeatedly charged the same production cost and made young Albums appear
		// catastrophically unprofitable.
		float estimatedOutcome = EstimateResponsiveMemoryOutcome(runtimeData.lifetimeLabelNet,
			runtimeData.sunkProductionCost, maturity, finalized);
		float ageMatchedExpectedNet = GetAgeMatchedExpectedNet(expectedNet,
			runtimeData.sunkProductionCost, maturity);
		float residual = Mathf.Clamp((estimatedOutcome - expectedNet) / scale, -ResponsiveMemoryResidualLimit, ResponsiveMemoryResidualLimit);
		if (float.IsNaN(residual) || float.IsInfinity(residual)) return;
		FormatMemoryObservation observation = memory.observations.FirstOrDefault(item => item.releaseId == releaseId);
		if (observation == null) {
			observation = new FormatMemoryObservation { releaseId = releaseId, projectId = runtimeData.albumProjectId,
				releaseLane = runtimeData.projectRole, estimatorLane = lane,
				releaseWeek = runtimeData.releaseMemoryWeek, expectedNet = expectedNet, opportunityScale = scale };
			memory.observations.Add(observation);
		}
		if (!TryAdvanceResponsiveMemoryRevision(observation, age, finalized,
			out bool replacedPriorRevision, out int revisionOrdinal)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturity;
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = releaseId, labelId = runtimeData.baseRecord.labelId, format = runtimeData.baseRecord.format,
			projectId = runtimeData.albumProjectId, releaseLane = runtimeData.projectRole, estimatorLane = lane,
			genre = runtimeData.baseRecord.primaryGenre, releaseAge = age, revisionKind = finalized ? "Final" : $"Age{age}",
			revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = expectedNet, ageMatchedExpectedNet = ageMatchedExpectedNet,
			realizedNetToDate = realizedToDate, estimatedOutcomeNet = estimatedOutcome, opportunityScale = scale,
			normalizedResidual = residual, maturityWeight = maturity,
			recencyWeight = 1f, replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	private static float EstimateResponsiveMemoryOutcome(float lifetimeLabelNet, float sunkProductionCost,
		float maturity, bool finalized) {
		float realizedToDate = lifetimeLabelNet - sunkProductionCost;
		if (finalized) return realizedToDate;
		return lifetimeLabelNet / Mathf.Max(.05f, maturity) - sunkProductionCost;
	}

	private static float GetAgeMatchedExpectedNet(float terminalExpectedNet, float sunkProductionCost, float maturity) =>
		(terminalExpectedNet + sunkProductionCost) * Mathf.Clamp(maturity, .05f, 1f) - sunkProductionCost;

	internal static (float EstimatedOutcome, float AgeMatchedExpected) GetResponsiveMemoryEconomicsForProbe(
		float lifetimeLabelNet, float sunkProductionCost, float terminalExpectedNet, float maturity, bool finalized) =>
		(EstimateResponsiveMemoryOutcome(lifetimeLabelNet, sunkProductionCost, maturity, finalized),
			GetAgeMatchedExpectedNet(terminalExpectedNet, sunkProductionCost, finalized ? 1f : maturity));

	/// <summary>
	/// Advances one release-keyed memory observation. The first revision is not a
	/// replacement; provisional duplicates, backward ages, and all post-final
	/// revisions are rejected. A final revision may replace a provisional row at
	/// the same age.
	/// </summary>
	internal static bool TryAdvanceResponsiveMemoryRevision(FormatMemoryObservation observation, int age, bool finalized,
		out bool replacedPriorRevision, out int revisionOrdinal) {
		replacedPriorRevision = false;
		revisionOrdinal = observation?.revisionOrdinal ?? 0;
		if (observation == null || age < 0 || observation.finalized || age < observation.lastRevisionAge) return false;
		if (!finalized && age <= observation.lastRevisionAge) return false;
		replacedPriorRevision = observation.lastRevisionAge >= 0;
		observation.lastRevisionAge = age;
		observation.revisionOrdinal++;
		observation.finalized = finalized;
		revisionOrdinal = observation.revisionOrdinal;
		return true;
	}

	private (float Residual, float EffectiveWeight, float Confidence) GetResponsiveMemory(FormatRevenueMemory memory, int currentWeek) {
		if (memory?.observations == null) return (0f, 0f, 0f);
		float weightedResidual = 0f, weight = 0f;
		foreach (FormatMemoryObservation observation in memory.observations) {
			int age = Mathf.Max(0, currentWeek - observation.releaseWeek);
			if (age > ResponsiveMemoryMaximumHistoryWeeks) continue;
			float recency = Mathf.Pow(.5f, age / ResponsiveMemoryHalfLifeWeeks);
			float itemWeight = recency * Mathf.Clamp(observation.maturityWeight, .05f, 1f);
			weightedResidual += observation.normalizedResidual * itemWeight;
			weight += itemWeight;
		}
		float confidence = CalculateResponsiveMemoryConfidence(weight);
		return (weight > 0f ? weightedResidual / weight : 0f, weight, confidence);
	}

	private static float CalculateResponsiveMemoryConfidence(float effectiveWeight) =>
		Mathf.Min(ResponsiveMemoryMaximumConfidence,
			Mathf.Max(0f, effectiveWeight) / (Mathf.Max(0f, effectiveWeight) + ResponsiveMemoryConfidenceK));

	internal static float GetResponsiveMemoryConfidenceForProbe(float effectiveWeight) =>
		CalculateResponsiveMemoryConfidence(effectiveWeight);

	private void TryFoldProjectMemory(AlbumProject project) {
		if (project == null || project.albumMemoryFolded || project.heldAlbumOutcome == null) return;
		if (project.strategy == ReleaseStrategy.AlbumWithPromo && project.heldPromoOutcome == null) return;
		float combined = project.heldAlbumOutcome.Value + (project.heldPromoOutcome ?? 0f);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true) {
			UpdateAlbumWithPromoProjectMemoryObservation(project, finalized: true);
		} else ApplyMemoryObservation(project.currentLabelId, ReleaseFormat.Album, combined);
		project.projectRealizedNet = combined;
		project.albumMemoryFolded = true;
		project.albumOutcomeState = ProjectOutcomeState.FoldedToAlbum;
		if (project.strategy == ReleaseStrategy.AlbumWithPromo) project.promoOutcomeState = ProjectOutcomeState.FoldedToAlbum;
	}

	private void UpdateAlbumWithPromoProjectMemoryObservation(AlbumProject project, bool finalized) {
		if (project?.strategy != ReleaseStrategy.AlbumWithPromo || string.IsNullOrEmpty(project.projectId)) return;
		AILabel label = GetLabel(project.currentLabelId);
		if (label == null) return;
		FormatRevenueMemory memory = label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumWithPromo);
		FormatMemoryObservation observation = memory.observations.FirstOrDefault(item => item.releaseId == project.projectId);
		if (observation == null) {
			observation = new FormatMemoryObservation {
				releaseId = project.projectId, projectId = project.projectId,
				releaseLane = ProjectRecordRole.LinkedAlbum, estimatorLane = RevenueEstimatorLane.AlbumWithPromo,
				releaseWeek = project.scheduledWeek, expectedNet = project.projectedProjectNet,
				opportunityScale = Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet))
			};
			memory.observations.Add(observation);
		}

		float promoDelta = GetProjectComponentOutcomeDelta(project.promoSingleId, project.heldPromoOutcome,
			project.projectedPromoSingleNet, terminalAge: 20f, finalized, out float promoMaturity);
		float albumDelta = GetProjectComponentOutcomeDelta(project.albumRecord?.recordId, project.heldAlbumOutcome,
			project.projectedAlbumNet, terminalAge: 52f, finalized, out float albumMaturity);
		float estimatedProject = project.projectedProjectNet + promoDelta + albumDelta;
		float residual = Mathf.Clamp((estimatedProject - project.projectedProjectNet) /
			Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet)), -ResponsiveMemoryResidualLimit, ResponsiveMemoryResidualLimit);
		int projectAge = Mathf.Max(0, (ChartManager.Instance?.GetCurrentChartWeek() ?? project.scheduledWeek) - project.scheduledWeek);
		if (!TryAdvanceResponsiveMemoryRevision(observation, projectAge, finalized,
			out bool replacedPriorRevision, out int revisionOrdinal)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = finalized ? 1f : Mathf.Clamp((promoMaturity + albumMaturity) * .5f, .05f, 1f);
		UpdatePooledAlbumWithPromoObservation(project, projectAge, finalized, residual, observation.maturityWeight);
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = project.projectId, projectId = project.projectId, labelId = project.currentLabelId,
			format = ReleaseFormat.Album, releaseLane = ProjectRecordRole.LinkedAlbum,
			estimatorLane = RevenueEstimatorLane.AlbumWithPromo, genre = project.genre,
			releaseAge = projectAge, revisionKind = finalized ? "Final" : $"Age{projectAge}", revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = project.projectedProjectNet, ageMatchedExpectedNet = project.projectedProjectNet,
			realizedNetToDate = (project.heldPromoOutcome ?? 0f) + (project.heldAlbumOutcome ?? 0f),
			estimatedOutcomeNet = estimatedProject, opportunityScale = observation.opportunityScale,
			normalizedResidual = residual, maturityWeight = observation.maturityWeight, recencyWeight = 1f,
			replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	private void UpdatePooledAlbumWithPromoObservation(AlbumProject project, int projectAge, bool finalized,
		float residual, float maturityWeight) {
		FormatMemoryObservation observation = pooledAlbumWithPromoMemory.observations
			.FirstOrDefault(item => item.releaseId == project.projectId);
		if (observation == null) {
			observation = new FormatMemoryObservation {
				releaseId = project.projectId, projectId = project.projectId,
				releaseLane = ProjectRecordRole.LinkedAlbum, estimatorLane = RevenueEstimatorLane.AlbumWithPromo,
				releaseWeek = project.scheduledWeek, expectedNet = project.projectedProjectNet,
				opportunityScale = Mathf.Max(1f, Mathf.Abs(project.projectedProjectNet))
			};
			pooledAlbumWithPromoMemory.observations.Add(observation);
		}
		if (!TryAdvanceResponsiveMemoryRevision(observation, projectAge, finalized, out _, out _)) return;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturityWeight;
	}

	private float GetProjectComponentOutcomeDelta(string recordId, float? heldOutcome, float expectedNet,
		float terminalAge, bool finalized, out float maturity) {
		if (heldOutcome.HasValue) {
			maturity = 1f;
			return heldOutcome.Value - expectedNet;
		}
		RecordRuntimeData runtime = string.IsNullOrEmpty(recordId) ? null : ChartManager.Instance?.GetRecordRuntimeData(recordId);
		if (runtime == null) {
			maturity = finalized ? 1f : 0f;
			return 0f;
		}
		maturity = Mathf.Clamp((runtime.weeksSinceRelease + 1f) / terminalAge, .05f, 1f);
		float estimated = EstimateResponsiveMemoryOutcome(runtime.lifetimeLabelNet, runtime.sunkProductionCost, maturity, finalized: false);
		return estimated - expectedNet;
	}

	private void RedirectCancelledPromoOutcome(AlbumProject project) {
		if (project?.heldPromoOutcome == null || project.promoOutcomeState == ProjectOutcomeState.RedirectedToSingle) return;
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true))
			ApplyMemoryObservation(project.currentLabelId, ReleaseFormat.Single, project.heldPromoOutcome.Value);
		project.promoOutcomeState = ProjectOutcomeState.RedirectedToSingle;
	}
	
	private bool TryReleaseRecord(AILabel label, GameDate date) {
		lastReleaseAttemptFailedArtistSelection = false;
		PrepareAnnualFormatCapacity(date.year);
		var artist = RosterManager.Instance?.GetArtistForRelease(label) ?? label.GetArtistForRelease(date.year);
		if (artist == null) {
			lastReleaseAttemptFailedArtistSelection = true;
			return false;
		}
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true &&
			!IsEligibleForEnabledFormatDecision(artist)) {
			GD.PushError($"Enabled release invariant failed: terminal artist {artist.artistId} reached release selection.");
			lastReleaseAttemptFailedArtistSelection = true;
			return false;
		}
		Genre artistPrimary = artist.primaryGenre;
		Genre artistSecondary = artist.secondaryGenre;
		GenreSupplyService.GenreSelection projectSelection = ChooseEnabledGenreSupply(label, artist, date.year);
		Genre projectGenre = projectSelection.Genre;
		bool explicitProjectIdentity = ArtistPopulationLifecycle.IsLive;
		SimulatedArtist decisionArtist = explicitProjectIdentity
			? CreateProjectDecisionArtist(artist, projectGenre, artistPrimary)
			: artist;
		if (!explicitProjectIdentity) {
			if (projectGenre != GenreCatalog.MapLegacy(artistPrimary, date.year)) artist.secondaryGenre = GenreCatalog.MapLegacy(artistPrimary, date.year);
			artist.primaryGenre = projectGenre;
		}

		// Snapshot only information available at the release fork. These pure reads are
		// also emitted for album-disabled calibration runs; they consume no RNG.
		DecisionContext decision = BuildDecisionContext(label, decisionArtist, date.year, date.month);
		decision.nonRetainedEmergingProject = IsNonRetainedEmergingProjectForFormatMemory(projectGenre,
			projectSelection.RetainedIdentity, date.year, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		ReleasePlan plan = DecideRelease(label, decisionArtist, date.year, decision);
		if (!explicitProjectIdentity) {
			artist.primaryGenre = artistPrimary;
			artist.secondaryGenre = artistSecondary;
		}
		if (plan.format == ReleaseFormat.Album) {
			return TryInitiateAlbumProject(label, artist, date, decision, plan, projectGenre);
		}
		var record = GenerateRecordFromArtist(label, artist, date.year, plan.format);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(record, projectGenre, artistPrimary);
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		float perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);

		float productionCost = CalculateProductionCost(label, record, date);
		float marketingBudget = label.GetMarketingBudget(artist) * perceivedQualityMult;
		float totalCost = productionCost + marketingBudget;
		float minReserve = label.GetMonthlyOverhead();
		
		if (label.cashReserves - totalCost < minReserve) {
			float available = label.cashReserves - minReserve - productionCost;
			if (available < 0) return false;
			marketingBudget = available * 0.8f;
			totalCost = productionCost + marketingBudget;
		}
		
		label.cashReserves -= totalCost;
		label.monthlyExpenses += totalCost;
		artist.unrecoupedAdvance += productionCost;
		WeeklyProductionSpend += productionCost;
		WeeklyProductionEvents++;
		WeeklyMarketingSpend += marketingBudget;
		WeeklyMarketingEvents++;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
			financials.lastMonthExpenses += totalCost;
		}
		
		record.releaseDate = date;
		record.projectRole = ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;
		ChartManager.Instance.ReleaseRecord(record);
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) throw new System.InvalidOperationException($"Released record '{record.recordId}' has no runtime data.");
		runtimeData.sunkProductionCost = productionCost;
		runtimeData.revenueMemoryEligible = true;
		runtimeData.releaseTimeExpectedNet = plan.format == ReleaseFormat.Album ? plan.priorAlbumNet : plan.priorSingleNet;
		runtimeData.releaseTimeOpportunityScale = Mathf.Max(1f, Mathf.Max(Mathf.Abs(runtimeData.releaseTimeExpectedNet), productionCost));
		runtimeData.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtimeData.projectRole = record.projectRole;
		ApplyReleasePromotion(record, artist, label, marketingBudget, perceivedQualityMult);
		CaptureSingleOpportunity(runtimeData, label, date.year);
		TrackRelease(label.labelId, record.recordId);
		WeeklySingleReleases++;
		RecordArtistRelease(artist, record.recordId, record.format);
		annualFormatDecisions++;
		OnCalibrationDecision?.Invoke(new CalibrationDecisionTelemetry {
			recordId = record.recordId,
			labelId = label.labelId,
			artistId = artist.artistId,
			genre = record.primaryGenre,
			careerState = artist.careerState,
			qualityEstimate = decision.qualityEstimate,
			reachFactor = decision.reachFactor,
			genreSinglesMarketFactor = decision.genreSinglesMarketFactor,
			singleProductionCost = decision.singleProductionCost,
			chosenFormat = plan.format
		});
		if (plan.economicsEvaluated) {
		OnReleaseStrategy?.Invoke(new ReleaseStrategyTelemetry {
				recordId = record.recordId,
				labelId = label.labelId,
				tier = label.tier,
				artistId = artist.artistId,
			genre = record.primaryGenre,
			secondaryGenre = record.secondaryGenre,
				careerState = artist.careerState,
				careerBand = GetCareerBandLabel(plan.careerBand, plan.unexpectedCareerState),
				qualityEstimate = decision.qualityEstimate,
				qualityQuartile = $"Q{plan.qualityQuartile + 1}",
				reachFactor = decision.reachFactor,
				genreSinglesMarketFactor = decision.genreSinglesMarketFactor,
				priorSingleNet = plan.priorSingleNet,
				priorAlbumNet = plan.priorAlbumNet,
				projectedSingleNet = plan.projectedSingleNet,
				projectedAlbumNet = plan.projectedAlbumNet,
				confidenceSingle = plan.confidenceSingle,
				confidenceAlbum = plan.confidenceAlbum,
				rawConfidenceSingle = plan.rawConfidenceSingle, rawConfidenceAlbum = plan.rawConfidenceAlbum,
				singleMemoryCapApplied = plan.singleMemoryCapApplied, albumMemoryCapApplied = plan.albumMemoryCapApplied,
				chosenFormat = plan.format,
				assumedCompilationCost = plan.legacyFourResolvableSingles,
				compCostWeight = plan.compCostWeight,
				expectedFormatMultiplier = plan.expectedFormatMultiplier,
				releasedSingleIdsExamined = plan.releasedSingleIdsExamined,
				resolvedSingles = plan.resolvedSingles,
				chartedSingles = plan.chartedSingles,
				hitScore = plan.hitScore,
				unweightedHitUnits = plan.unweightedHitUnits,
				weightedHitUnits = plan.weightedHitUnits,
				affinityUnits = plan.affinityUnits,
				totalExpectedAlbumUnits = plan.totalExpectedAlbumUnits,
				actualAlbumFormat = record.album?.albumFormat,
				strategy = ReleaseStrategy.OrphanSingle,
				projectedOrphanSingleNet = plan.projectedSingleNet,
				projectedAlbumStandaloneNet = plan.projectedAlbumStandaloneNet,
				projectedAlbumWithPromoNet = plan.projectedAlbumWithPromoNet,
				singlePreTiltContribution = plan.singlePreTiltContribution, singleFormatTilt = plan.singleFormatTilt,
				albumAffinity = plan.albumAffinity, albumOpportunity = plan.albumOpportunity,
				albumFormatTilt = plan.albumFormatTilt, albumPreTiltContribution = plan.albumPreTiltContribution,
				albumProductionCost = plan.albumProductionCost, singleProductionCost = plan.singleProductionCost, singleMemoryEma = plan.singleMemoryEma,
				albumMemoryEma = plan.albumMemoryEma, singleMemoryBlend = plan.singleMemoryBlend,
				albumMemoryBlend = plan.albumMemoryBlend, singleNoiseMultiplier = plan.singleNoiseMultiplier,
				albumChoiceProbability = plan.albumChoiceProbability, formatChoiceRoll = plan.formatChoiceRoll,
				labelFormatMemoryBypassed = plan.labelFormatMemoryBypassed,
				albumNoiseMultiplier = plan.albumNoiseMultiplier
			});
		}
		
		if (debugMode) {
			GD.Print($"🎵 {label.labelName}: '{record.title}' by {artist.stageName} (Quality: {(record.hookStrength + record.productionQuality) / 2f:F2}, Budget: ${totalCost:N0})");
		}
		return true;
	}

	private static SimulatedArtist CreateProjectDecisionArtist(SimulatedArtist source, Genre projectGenre, Genre identityGenre) => new() {
		artistId = source.artistId, stageName = source.stageName, type = source.type, members = source.members,
		primaryGenre = projectGenre, secondaryGenre = projectGenre == identityGenre ? source.secondaryGenre : identityGenre,
		formedYear = source.formedYear, vocalPower = source.vocalPower, musicianship = source.musicianship,
		songwritingAbility = source.songwritingAbility, livePerformance = source.livePerformance,
		studioPerformance = source.studioPerformance, groupCohesion = source.groupCohesion, careerState = source.careerState,
		momentum = source.momentum, reputation = source.reputation, totalReleases = source.totalReleases,
		charted = source.charted, top40Hits = source.top40Hits, top10Hits = source.top10Hits, numberOnes = source.numberOnes,
		weeksSinceLastRelease = source.weeksSinceLastRelease, releasedSingleIds = source.releasedSingleIds
	};
	internal static SimulatedArtist CreateProjectDecisionArtistForProbe(SimulatedArtist source, Genre projectGenre) =>
		CreateProjectDecisionArtist(source, projectGenre, source.primaryGenre);

	private GenreSupplyService.GenreSelection ChooseEnabledGenreSupply(AILabel label, SimulatedArtist artist, int year) {
		if (!GenreMarketV2.Enabled || ChartManager.Instance?.IsGenreMarketV2Live != true)
			return new GenreSupplyService.GenreSelection(artist.primaryGenre, retainedIdentity: true);
		if (genreSupplyYear != year) {
			genreSupplyYear = year;
			annualGenreSupplyByLabel.Clear();
			annualGenreSupplyGlobal.Clear();
		}
		if (!annualGenreSupplyByLabel.TryGetValue(label.labelId, out Dictionary<Genre, int> recent)) {
			recent = new Dictionary<Genre, int>();
			annualGenreSupplyByLabel[label.labelId] = recent;
		}
		MarketRegion region = ChartManager.Instance?.GetRegionById(label.homeRegion);
		Genre[] required = GenreSupplyService.GetAvailableGenres(year)
			.Where(genre => annualGenreSupplyGlobal.GetValueOrDefault(genre) < 3).ToArray();
		bool annualFloor = required.Length > 0;
		GenreSupplyService.GenreSelection selection = GenreSupplyService.ChooseGenreWithSelection(label, artist, region, year, recent,
			GetDeterministicSupplyRoll(label.labelId, artist.artistId, year, pipelineWeek, recent.Values.Sum()),
			annualFloor ? required : null, annualGenreSupplyGlobal, applyPsychedelicTransitionCompatibility: true);
		Genre chosen = selection.Genre;
		OnSupplySelection?.Invoke(new SupplySelectionTelemetry {
			labelId = label.labelId, artistId = artist.artistId, artistIdentity = artist.primaryGenre, chosenProjectGenre = chosen,
			artistIdentityAvailableForNewSupply = GenreSupplyService.IsAvailableForNewSupply(artist.primaryGenre, year),
			annualFloorRequested = annualFloor,
			annualFloorReroutedToNormalCandidates = annualFloor && !selection.RetainedIdentity && !selection.UsedCandidateOverride,
			selectionMode = selection.UsedCandidateOverride ? SupplySelectionMode.AnnualFloor :
				selection.RetainedIdentity ? SupplySelectionMode.Retained : SupplySelectionMode.WeightedTransition
		});
		recent[chosen] = recent.GetValueOrDefault(chosen) + 1;
		annualGenreSupplyGlobal[chosen] = annualGenreSupplyGlobal.GetValueOrDefault(chosen) + 1;
		return selection;
	}

	/// <summary>
	/// A label-wide format-result memory has no genre evidence for a novel,
	/// non-retained late-emerging project. The catalog introduction year, rather
	/// than a one-calendar-year lifecycle enum, keeps the rule applicable to the
	/// Psychedelic projects that remain new to an artist in 1969. Retained
	/// identities and all disabled/prewarm paths preserve the established
	/// label-format memory route exactly.
	/// </summary>
	internal static bool IsNonRetainedEmergingProjectForFormatMemory(Genre projectGenre, bool retainedIdentity, int year, bool live) {
		GenreProfile profile = GenreCatalog.Get(GenreCatalog.MapLegacy(projectGenre, year));
		return live && !retainedIdentity && profile.EmergenceYear >= 1966 && year >= profile.EmergenceYear;
	}

	internal static float GetProjectFormatMemoryConfidence(float labelFormatConfidence, bool nonRetainedEmergingProject) =>
		nonRetainedEmergingProject ? 0f : labelFormatConfidence;

	private static float GetDeterministicSupplyRoll(string labelId, string artistId, int year, int week, int sequence) {
		uint hash = 2166136261u;
		foreach (char value in $"{labelId}|{artistId}|{year}|{week}|{sequence}") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	private bool TryInitiateAlbumProject(AILabel label, SimulatedArtist artist, GameDate date, DecisionContext decision, ReleasePlan plan, Genre projectGenre) {
		Record album = GenerateRecordFromArtist(label, artist, date.year, ReleaseFormat.Album);
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(album, projectGenre, artist.primaryGenre);
		float albumProductionCost = CalculateProductionCost(label, album, date);
		float albumPerceivedMult;
		float albumMarketingPlanned;
		Record promo = null;
		int gapWeeks = 0;
		PromotionSnapshot albumPromotion = null;
		float promoProductionCost = 0f;
		float promoMarketingBudget = 0f;
		float promoPerceivedMult = 1f;

		if (plan.strategy == ReleaseStrategy.AlbumWithPromo) {
			promo = CreatePromoSingleFromAlbum(album);
			gapWeeks = (int)GD.RandRange(Mathf.Min(albumDropGapWeeksMin, albumDropGapWeeksMax), Mathf.Max(albumDropGapWeeksMin, albumDropGapWeeksMax));
			albumPerceivedMult = DrawPerceivedQualityMultiplier(album, label);
			albumMarketingPlanned = label.GetMarketingBudget(artist) * albumPerceivedMult;
			albumPromotion = BuildPromotionSnapshot(album, artist, albumPerceivedMult);
			promoProductionCost = CalculateProductionCost(label, promo, date);
			promoPerceivedMult = DrawPerceivedQualityMultiplier(promo, label);
			promoMarketingBudget = label.GetMarketingBudget(artist) * promoPerceivedMult;
		} else {
			albumPerceivedMult = DrawPerceivedQualityMultiplier(album, label);
			albumMarketingPlanned = label.GetMarketingBudget(artist) * albumPerceivedMult;
		}

		float firstMarketing = plan.strategy == ReleaseStrategy.AlbumStandalone ? albumMarketingPlanned : promoMarketingBudget;
		float productionTotal = albumProductionCost + promoProductionCost;
		if (!TryClampFirstEventCost(label, productionTotal, ref firstMarketing)) return false;
		if (plan.strategy == ReleaseStrategy.AlbumStandalone) albumMarketingPlanned = firstMarketing;
		else promoMarketingBudget = firstMarketing;
		ChargeProjectCost(label, artist, productionTotal + firstMarketing, productionTotal);
		WeeklyProductionSpend += productionTotal;
		WeeklyProductionEvents += promo == null ? 1 : 2;
		WeeklyMarketingSpend += firstMarketing;
		WeeklyMarketingEvents++;

		string projectId = $"project_{++generatedProjectCounter}";
		var project = new AlbumProject {
			projectId = projectId, creationSequence = generatedProjectCounter,
			originalLabelId = label.labelId, currentLabelId = label.labelId, tierAtSchedule = label.tier,
			artistId = artist.artistId, genre = projectGenre, careerStateAtSchedule = artist.careerState,
			careerStateBeforeDropAtSchedule = artist.careerStateBeforeDrop,
			contractEntryCareerStateAtSchedule = artist.contractEntryCareerState,
			contractSequenceAtSchedule = artist.contractSequence, contractStartWeekAtSchedule = artist.contractStartWeek,
			scheduledWeek = pipelineWeek, scheduledDate = date, dropWeek = pipelineWeek + gapWeeks,
			dropDate = date.AddDays(gapWeeks * 7), strategy = plan.strategy, albumRecord = album,
			promoSingleRecord = promo, promoSingleId = promo?.recordId, albumProductionCost = albumProductionCost,
			promoProductionCost = promoProductionCost, albumPromotionSnapshot = albumPromotion,
			albumMarketingBudgetPlanned = albumMarketingPlanned, releaseTimeAlbumExpectedNet = plan.priorAlbumNet,
			projectedAlbumNet = plan.projectedAlbumStandaloneNet,
			projectedPromoSingleNet = plan.expectedPromoSingleNet, projectedProjectNet = plan.projectedAlbumWithPromoNet,
			albumOutcomeState = ProjectOutcomeState.Pending,
			promoOutcomeState = promo == null ? ProjectOutcomeState.None : ProjectOutcomeState.Pending
		};
		albumProjects.Add(project);
		(string ArtistId, int Year) artistYear = (artist.artistId, date.year);
		annualAlbumProjectsByArtist[artistYear] = annualAlbumProjectsByArtist.GetValueOrDefault(artistYear) + 1;
		annualAlbumProjectsScheduled++;
		annualFormatDecisions++;
		WeeklyAlbumProjectsScheduled++;
		projectById[projectId] = project;
		projectByRecordId[album.recordId] = project;
		if (promo != null) projectByRecordId[promo.recordId] = project;

		if (plan.strategy == ReleaseStrategy.AlbumStandalone) {
			project.terminalState = AlbumProjectTerminalState.Released;
			ReleasePreparedRecord(album, artist, label, date, albumProductionCost, plan.priorAlbumNet,
				ProjectRecordRole.StandaloneAlbum, projectId);
			ApplyReleasePromotion(album, artist, label, albumMarketingPlanned, albumPerceivedMult);
		} else {
			pendingAlbumProjects.Add(project);
			ReleasePreparedRecord(promo, artist, label, date, promoProductionCost, plan.expectedPromoSingleNet,
				ProjectRecordRole.PromoSingle, projectId);
			ApplyReleasePromotion(promo, artist, label, promoMarketingBudget, promoPerceivedMult);
			CaptureSingleOpportunity(ChartManager.Instance.GetRecordRuntimeData(promo.recordId), label, date.year);
		}
		artist.weeksSinceLastRelease = 0;
		EmitAlbumDecisionTelemetry(label, artist, decision, plan, album, project);
		return true;
	}

	private static void ApplyProjectGenre(Record record, Genre projectGenre, Genre artistGenre) {
		if (record == null) return;
		record.primaryGenre = projectGenre;
		if (projectGenre != GenreCatalog.MapLegacy(artistGenre, record.releaseDate.year > 0 ? record.releaseDate.year : null))
			record.secondaryGenre = GenreCatalog.MapLegacy(artistGenre, record.releaseDate.year > 0 ? record.releaseDate.year : null);
		if (record.album == null) return;
		foreach (AlbumTrack track in record.album.GetAllTracks()) if (track != null) track.genre = projectGenre;
	}

	private float DrawPerceivedQualityMultiplier(Record record, AILabel label) {
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GD.RandRange(-noiseRange, noiseRange), 0f, 1f);
		return 0.6f + perceivedQuality * 0.8f;
	}

	private bool TryClampFirstEventCost(AILabel label, float productionCost, ref float marketingBudget) {
		float minReserve = label.GetMonthlyOverhead();
		if (label.cashReserves - productionCost - marketingBudget >= minReserve) return true;
		float available = label.cashReserves - minReserve - productionCost;
		if (available < 0f) return false;
		marketingBudget = available * 0.8f;
		return true;
	}

	private void ChargeProjectCost(AILabel label, SimulatedArtist artist, float totalCost, float productionCost) {
		label.cashReserves -= totalCost;
		label.monthlyExpenses += totalCost;
		artist.unrecoupedAdvance += productionCost;
		if (labelFinancials.TryGetValue(label.labelId, out var financials)) financials.lastMonthExpenses += totalCost;
	}

	private void ReleasePreparedRecord(Record record, SimulatedArtist artist, AILabel label, GameDate date, float productionCost,
		float releaseTimeExpectedNet, ProjectRecordRole role, string projectId) {
		record.labelId = label.labelId;
		record.releaseDate = date;
		record.projectRole = role;
		record.albumProjectId = projectId;
		ChartManager.Instance.ReleaseRecord(record);
		if (record.format == ReleaseFormat.Album && record.album?.albumFormat == AlbumFormat.Compilation) {
			foreach (AlbumTrack track in record.album.trackRefs ?? System.Array.Empty<AlbumTrack>()) {
				ChartManager.Instance.RegisterCompUse(track?.sourceRecordId);
			}
		}
		RecordRuntimeData runtime = ChartManager.Instance.GetRecordRuntimeData(record.recordId)
			?? throw new System.InvalidOperationException($"Released record '{record.recordId}' has no runtime data.");
		runtime.sunkProductionCost = productionCost;
		runtime.revenueMemoryEligible = true;
		runtime.releaseTimeExpectedNet = releaseTimeExpectedNet;
		runtime.releaseTimeOpportunityScale = Mathf.Max(1f, Mathf.Max(Mathf.Abs(releaseTimeExpectedNet), productionCost));
		runtime.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtime.projectRole = record.projectRole;
		runtime.albumProjectId = record.albumProjectId;
		if (role == ProjectRecordRole.LinkedAlbum && projectById.TryGetValue(projectId, out AlbumProject project)) runtime.linkedPromoSingleId = project.promoSingleId;
		TrackRelease(label.labelId, record.recordId);
		RecordArtistRelease(artist, record.recordId, record.format);
	}

	// RosterManager.RecordReleased already appends to releaseHistory, so the second
	// append this replaces double-counted every live release. GenreSupplyService caps
	// project history at three (Systems/GenreSupplyService.cs:211-212), so an artist hit
	// that cap after two releases instead of three and carried up to 0.06 of unearned
	// project-identity retention. The prewarm path is unaffected: it never calls
	// RecordReleased and already appended exactly once. The fallback preserves the
	// bookkeeping when the RosterManager singleton is unavailable, which is the reason
	// the redundant append existed at the call sites.
	internal static void RecordArtistRelease(SimulatedArtist artist, string recordId, ReleaseFormat format) {
		if (artist == null || string.IsNullOrEmpty(recordId)) return;
		if (RosterManager.Instance != null) {
			RosterManager.Instance.RecordReleased(artist, recordId);
		} else {
			artist.totalReleases++;
			artist.releaseHistory.Add(recordId);
		}
		artist.weeksSinceLastRelease = 0;
		if (format == ReleaseFormat.Single) artist.releasedSingleIds.Add(recordId);
	}

	private static void CaptureSingleOpportunity(RecordRuntimeData runtime, AILabel label, int year) {
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			SingleOpportunityLedger.CaptureAtRelease(runtime, label, ChartManager.Instance.GetAllRegions(), year);
	}

	private void EmitAlbumDecisionTelemetry(AILabel label, SimulatedArtist artist, DecisionContext decision, ReleasePlan plan,
		Record album, AlbumProject project) {
		OnCalibrationDecision?.Invoke(new CalibrationDecisionTelemetry {
			recordId = album.recordId, labelId = label.labelId, artistId = artist.artistId, genre = album.primaryGenre,
			careerState = artist.careerState, qualityEstimate = decision.qualityEstimate, reachFactor = decision.reachFactor,
			genreSinglesMarketFactor = decision.genreSinglesMarketFactor, singleProductionCost = decision.singleProductionCost,
			chosenFormat = ReleaseFormat.Album
		});
		OnReleaseStrategy?.Invoke(BuildReleaseStrategyTelemetry(label, artist, decision, plan, album, project));
	}

	private ReleaseStrategyTelemetry BuildReleaseStrategyTelemetry(AILabel label, SimulatedArtist artist, DecisionContext decision,
		ReleasePlan plan, Record album, AlbumProject project) => new() {
		recordId = album.recordId, labelId = label.labelId, tier = label.tier, artistId = artist.artistId,
		// The decision was evaluated with projectGenre, then the artist identity was
		// restored before this event. Telemetry must describe the released project,
		// not that restored identity, or enabled project routing is misclassified.
		genre = album.primaryGenre, secondaryGenre = album.secondaryGenre, careerState = artist.careerState,
		careerBand = GetCareerBandLabel(plan.careerBand, plan.unexpectedCareerState), qualityEstimate = decision.qualityEstimate,
		qualityQuartile = $"Q{plan.qualityQuartile + 1}", reachFactor = decision.reachFactor,
		genreSinglesMarketFactor = decision.genreSinglesMarketFactor, priorSingleNet = plan.priorSingleNet,
		priorAlbumNet = plan.priorAlbumNet, projectedSingleNet = plan.projectedSingleNet, projectedAlbumNet = plan.projectedAlbumNet,
		confidenceSingle = plan.confidenceSingle, confidenceAlbum = plan.confidenceAlbum, chosenFormat = ReleaseFormat.Album,
		rawConfidenceSingle = plan.rawConfidenceSingle, rawConfidenceAlbum = plan.rawConfidenceAlbum,
		singleMemoryCapApplied = plan.singleMemoryCapApplied, albumMemoryCapApplied = plan.albumMemoryCapApplied,
		assumedCompilationCost = plan.legacyFourResolvableSingles, compCostWeight = plan.compCostWeight,
		expectedFormatMultiplier = plan.expectedFormatMultiplier, releasedSingleIdsExamined = plan.releasedSingleIdsExamined,
		resolvedSingles = plan.resolvedSingles, chartedSingles = plan.chartedSingles, hitScore = plan.hitScore,
		unweightedHitUnits = plan.unweightedHitUnits, weightedHitUnits = plan.weightedHitUnits,
		affinityUnits = plan.affinityUnits, totalExpectedAlbumUnits = plan.totalExpectedAlbumUnits,
		actualAlbumFormat = album.album?.albumFormat, projectId = project.projectId, strategy = plan.strategy,
		projectedOrphanSingleNet = plan.projectedOrphanSingleNet, projectedAlbumStandaloneNet = plan.projectedAlbumStandaloneNet,
		projectedAlbumWithPromoNet = plan.projectedAlbumWithPromoNet, promoSingleId = project.promoSingleId,
		albumStrategyEvaluated = plan.albumStrategyEvaluated, singleProductionCost = plan.singleProductionCost,
		singleNetMarginPerUnit = plan.singleNetMarginPerUnit, expectedSingleUnits = plan.expectedSingleUnits,
		albumDemandFactor = plan.albumDemandFactor, substitutionK = plan.substitutionK,
		substitutionCap = plan.substitutionCap, substitutionPropensity = plan.substitutionPropensity,
		expectedOverlapFraction = plan.expectedOverlapFraction, divertedUnits = plan.divertedUnits,
		albumMarginPerUnit = plan.albumMarginPerUnit, cannibalizationLoss = plan.cannibalizationLoss,
		cannibalizationCharged = plan.cannibalizationCharged,
		expectedPromoLift = plan.expectedPromoLift, expectedPromoSingleNet = plan.expectedPromoSingleNet,
		promoAdvantage = plan.promoAdvantage, singlePreTiltContribution = plan.singlePreTiltContribution,
		singleFormatTilt = plan.singleFormatTilt, albumAffinity = plan.albumAffinity,
		albumOpportunity = plan.albumOpportunity, albumFormatTilt = plan.albumFormatTilt,
		albumPreTiltContribution = plan.albumPreTiltContribution, albumProductionCost = plan.albumProductionCost,
		singleMemoryEma = plan.singleMemoryEma, albumMemoryEma = plan.albumMemoryEma,
		singleMemoryBlend = plan.singleMemoryBlend, albumMemoryBlend = plan.albumMemoryBlend,
		labelFormatMemoryBypassed = plan.labelFormatMemoryBypassed,
		singleNoiseMultiplier = plan.singleNoiseMultiplier, albumNoiseMultiplier = plan.albumNoiseMultiplier,
		albumChoiceProbability = plan.albumChoiceProbability, formatChoiceRoll = plan.formatChoiceRoll,
		albumCapacityReroute = plan.albumCapacityReroute
	};

	private ReleasePlan DecideRelease(AILabel label, SimulatedArtist artist, int year, DecisionContext decision) {
		if (!enableAlbums) return new() { format = ReleaseFormat.Single, strategy = ReleaseStrategy.OrphanSingle };

		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.careerBand = GetCareerBandIndex(artist.careerState, out bool unexpectedCareerState);
		decision.unexpectedCareerState = unexpectedCareerState;
		float compCostWeight = CalculateCompilationCostWeight(artist.primaryGenre, year);
		HitInventory hitInventory = ResolveHitInventory(artist);
		float priorSingle = CalculateSinglePriorNet(decision);
		float priorAlbum = CalculateAlbumPriorNet(label, artist, year, decision, compCostWeight, hitInventory, out AlbumPriorDiagnostics albumPrior);
		bool useResponsiveMemory = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		// Live choices read only the estimator that answers that exact decision.
		// Legacy format memories remain untouched for disabled replay compatibility.
		FormatRevenueMemory singleMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.OrphanSingle) : label.GetOrCreateRevenueMemory(ReleaseFormat.Single);
		FormatRevenueMemory albumMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumComponent) : label.GetOrCreateRevenueMemory(ReleaseFormat.Album);
		FormatRevenueMemory standaloneMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.StandaloneAlbum) : albumMemory;
		FormatRevenueMemory promoMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.PromoSingle) : singleMemory;
		FormatRevenueMemory projectMemory = useResponsiveMemory
			? label.GetOrCreateRevenueMemory(RevenueEstimatorLane.AlbumWithPromo) : albumMemory;
		var singleResponsive = GetResponsiveMemory(singleMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var albumResponsive = GetResponsiveMemory(albumMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var standaloneResponsive = GetResponsiveMemory(standaloneMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		var promoResponsive = GetResponsiveMemory(promoMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		var labelProjectResponsive = GetResponsiveMemory(projectMemory, currentWeek);
		var pooledProjectResponsive = GetResponsiveMemory(pooledAlbumWithPromoMemory, currentWeek);
		float projectLocalBlend = labelProjectResponsive.Confidence /
			Mathf.Max(.000001f, labelProjectResponsive.Confidence + pooledProjectResponsive.Confidence);
		var projectResponsive = (
			Residual: pooledProjectResponsive.Confidence > 0f
				? Mathf.Lerp(pooledProjectResponsive.Residual, labelProjectResponsive.Residual, projectLocalBlend)
				: labelProjectResponsive.Residual,
			EffectiveWeight: pooledProjectResponsive.EffectiveWeight + labelProjectResponsive.EffectiveWeight,
			Confidence: Mathf.Max(pooledProjectResponsive.Confidence, labelProjectResponsive.Confidence));
		float confidenceSingle = useResponsiveMemory ? singleResponsive.Confidence : singleMemory.releasesObserved / (singleMemory.releasesObserved + Mathf.Max(.1f, revenueMemoryConfidenceK));
		float confidenceAlbum = useResponsiveMemory ? albumResponsive.Confidence : albumMemory.releasesObserved / (albumMemory.releasesObserved + Mathf.Max(.1f, revenueMemoryConfidenceK));
		float confidenceStandalone = useResponsiveMemory ? standaloneResponsive.Confidence : confidenceAlbum;
		confidenceSingle = GetProjectFormatMemoryConfidence(confidenceSingle, decision.nonRetainedEmergingProject);
		confidenceAlbum = GetProjectFormatMemoryConfidence(confidenceAlbum, decision.nonRetainedEmergingProject);
		confidenceStandalone = GetProjectFormatMemoryConfidence(confidenceStandalone, decision.nonRetainedEmergingProject);
		float rawConfidenceSingle = confidenceSingle;
		float rawConfidenceAlbum = confidenceAlbum;
		bool applyLiveMemoryCeiling = useResponsiveMemory;
		// Current priors remain the centre of the decision; memory is only a bounded
		// relative-performance adjustment scaled to the opportunity visible today.
		float projectedSingle = useResponsiveMemory
			? priorSingle + confidenceSingle * singleResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorSingle))
			: Mathf.Lerp(priorSingle, singleMemory.emaNetPerRelease, confidenceSingle);
		float projectedAlbum = useResponsiveMemory
			? priorAlbum + confidenceAlbum * albumResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorAlbum))
			: Mathf.Lerp(priorAlbum, albumMemory.emaNetPerRelease, confidenceAlbum);
		float projectedStandaloneAlbum = useResponsiveMemory && confidenceStandalone > 0f
			? priorAlbum + confidenceStandalone * standaloneResponsive.Residual * Mathf.Max(1f, Mathf.Abs(priorAlbum))
			: projectedAlbum;

		float noiseRange = Mathf.Lerp(0.50f, 0.15f, Mathf.Clamp(label.scoutingAbility, 0f, 1f));
		float singleNoiseMultiplier = 1f + (float)GD.RandRange(-noiseRange, noiseRange);
		float albumNoiseMultiplier = 1f + (float)GD.RandRange(-noiseRange, noiseRange);
		projectedSingle *= singleNoiseMultiplier;
		projectedAlbum *= albumNoiseMultiplier;
		projectedStandaloneAlbum *= albumNoiseMultiplier;
		// Commitment is a portfolio subsidy, not a revenue estimate, so it is applied
		// as an additive credit on a positive scale rather than as a scalar on a signed
		// net. Scaling the net inverted the intent: an Album projected to lose money is
		// the normal marginal case the commitment exists to carry, and multiplying it
		// pushed it further below the Single hurdle, harder every year as era weight
		// rose. The credit uses the same pre-noise prior scale as the memory residual
		// above, and stays outside the noise draw because it is policy, not estimation.
		//
		// Live-only. The commitment exists to stop the responsive lane split abandoning
		// Albums as the LP market matures — a failure mode the disabled route does not
		// have, because it has no lane split. Applying it there contaminated the gate
		// reference with a mechanism authored for the route being measured against it,
		// and the capacity derivation made that far worse than the old tier lookup: what
		// had reached only Major labels now reached every label, pushing the control's
		// Album decision share to .84/.91/.96 across 1965-69 and taking
		// scheduledAlbumProjects out of band at 1963.
		float albumPortfolioCommitment = useResponsiveMemory ? GetAlbumPortfolioCommitmentMultiplier(label, year) : 1f;
		float albumPortfolioCredit = CalculateAlbumPortfolioCredit(albumPortfolioCommitment, priorAlbum);
		projectedAlbum += albumPortfolioCredit;
		projectedStandaloneAlbum += albumPortfolioCredit;
		float singleFormatTilt = GetFormatPriorMultiplier(artist.primaryGenre, ReleaseFormat.Single, year);
		float singlePreTiltContribution = (priorSingle + decision.singleProductionCost) / Mathf.Max(.000001f, singleFormatTilt);
		float projectedLaunchAwareness = ProjectLaunchAwareness(label, artist, label.GetMarketingBudget(artist));
		float expectedPromoLift = (1f - Mathf.Clamp(projectedLaunchAwareness, 0f, 1f)) * expectedPromoLiftScalar;
		float meanAlbumDropGapWeeks = (albumDropGapWeeksMin + albumDropGapWeeksMax) * 0.5f;
		float expectedOverlapFraction = Mathf.Clamp(
			(expectedOverlapWeeks - meanAlbumDropGapWeeks) / Mathf.Max(1f, expectedOverlapWeeks), 0f, 1f);
		float expectedPromoSingleNet = CalculateSinglePriorNet(decision);
		float promoConfidence = useResponsiveMemory ? promoResponsive.Confidence : confidenceSingle;
		if (useResponsiveMemory) expectedPromoSingleNet += promoConfidence * promoResponsive.Residual * Mathf.Max(1f, Mathf.Abs(expectedPromoSingleNet));
		float expectedSingleUnits = Mathf.Max(0f,
			(expectedPromoSingleNet + decision.singleProductionCost) / Mathf.Max(singleNetMarginPerUnit, 0.000001f));
		float albumDemandFactor = CalculateAlbumDemandFactor(artist.primaryGenre, year);
		float substitutionPropensity = Mathf.Clamp(substitutionK * albumDemandFactor, 0f, substitutionCap);
		float divertedUnits = substitutionPropensity * expectedOverlapFraction * expectedSingleUnits;
		float cannibalizationLoss = divertedUnits * albumPrior.marginPerUnit;
		// A promo Single does not only divert Album buyers, it recruits them — that is
		// the entire reason a label runs one. Only the diversion was modelled, and it
		// scales with albumDemandFactor while the awareness lift is a fixed scalar, so
		// past a crossover year cannibalization exceeded the whole promo proposition
		// and the strategy became permanently non-viable market-wide. Recruitment now
		// scales on the same terms as diversion, so the two stay in proportion as the
		// LP market matures instead of one outgrowing the other without bound.
		//
		// Deliberately NOT gated to the live route, unlike the portfolio credit above.
		// The asymmetry it corrects is shared: cannibalizationLoss scales with
		// albumDemandFactor and expectedPromoLift is a fixed scalar on both routes, so
		// both model a promo Single that steals ever more from the Album and never sells
		// one. That is an error in the economics, not a defect in the live lane split.
		// Only the absorbing state it interacts with is live-specific, and that is fixed
		// separately in ResolveAlbumDecision. Gating this was measured and reverted: it
		// dropped the control's promo share to .37 by 1969, and since a promo project
		// emits two products and a standalone emits one, the control lost the Singles
		// those Album projects would have carried — Single units fell to 99.3M and took
		// totalUnits, grossRevenue, labelNet and marketNet out of band at 1968-69.
		float promoSynergyGain = CalculatePromoAlbumSynergyGain(albumDemandFactor,
			1f - Mathf.Clamp(projectedLaunchAwareness, 0f, 1f), expectedSingleUnits, albumPrior.marginPerUnit);
		// projectedAlbum above is the Album-component projection, already moved off its
		// prior by the AlbumComponent lane residual at weight confidenceAlbum — and that
		// lane observes realized Albums that were themselves released alongside a promo
		// Single. Whatever diversion the promo actually caused is therefore already
		// inside the projection being adjusted, so subtracting the full modelled
		// cannibalizationLoss on top charges it twice, at exactly that weight.
		//
		// The duplicate share scales with album unit economics while the terms opposing
		// it — the promo Single's own net and a fixed awareness scalar — do not, so the
		// strategy decays to non-viable precisely as the LP market matures, and it decays
		// first for whoever carries the largest expectedSingleUnits. Measured over the
		// 48,155 decisions of d7-evidence-repairs-522-1001, mean Major promoAdvantage
		// falls 46,070 (1960) to -2,378 (1969); charged once it is flat, 47,344 to 55,267.
		// Majors abandoned the promo Single for 274 of 495 decisions in 1969 against 0 of
		// 360 in 1960, and a standalone Album emits no Single, which is the whole of the
		// late-decade Major Singles collapse.
		float chargedCannibalizationLoss = CalculateChargedPromoCannibalization(cannibalizationLoss, confidenceAlbum);
		float promoAdvantage = expectedPromoLift + promoSynergyGain + expectedPromoSingleNet - chargedCannibalizationLoss;
		float componentProjectedAlbumWithPromo = projectedAlbum + promoAdvantage;
		float projectedAlbumWithPromo = componentProjectedAlbumWithPromo;
		if (useResponsiveMemory) projectedAlbumWithPromo += projectResponsive.Confidence * projectResponsive.Residual *
			Mathf.Max(1f, Mathf.Abs(projectedAlbumWithPromo));
		float promoProjectDelayPremium = meanAlbumDropGapWeeks / 52f;

		(bool economicAlbumWins, bool promoPreferred, float albumGateProjection) = useResponsiveMemory
			? ResolveAlbumDecision(projectedSingle, projectedAlbum, projectedStandaloneAlbum,
				componentProjectedAlbumWithPromo, projectedAlbumWithPromo, promoProjectDelayPremium,
				LiveAlbumDecisionEligibilityScale)
			: (projectedAlbum > projectedSingle, componentProjectedAlbumWithPromo > projectedAlbum, projectedAlbum);
		float decisionSingleHurdle = promoPreferred
			? projectedSingle + Mathf.Max(0f, promoProjectDelayPremium) * Mathf.Max(1f, Mathf.Abs(projectedSingle))
			: projectedSingle;
		float albumChoiceProbability = useResponsiveMemory
			? CalculateAlbumChoiceProbability(decisionSingleHurdle, albumGateProjection)
			: (economicAlbumWins ? 1f : 0f);
		float formatChoiceRoll = useResponsiveMemory
			? GetDeterministicFormatChoiceRoll(label.labelId, artist.artistId, year, currentWeek, annualFormatDecisions)
			: 0f;
		bool albumWins = useResponsiveMemory
			? ResolvePositiveFormatChoice(decisionSingleHurdle, albumGateProjection, formatChoiceRoll)
			: economicAlbumWins;
		bool albumProjectPressure = useResponsiveMemory &&
			IsAlbumProjectSharePressureHigh(annualFormatDecisions, annualAlbumProjectsScheduled);
		bool albumCapacityReroute = false;
		if (albumWins && !CanScheduleAnnualAlbumProject(
			annualAlbumProjectsByArtist.GetValueOrDefault((artist.artistId, year)), albumProjectPressure)) {
			albumWins = false;
			promoPreferred = false;
			albumCapacityReroute = true;
		}
		// Physical Album-component memory owns format eligibility. Standalone and
		// promo component memories rank the strategies. Total-project memory observes
		// the same component outcomes, so stacking its residual would count their
		// underperformance twice; it is only a fail-closed viability guard here.
		float decisionAlbumConfidence = confidenceAlbum;
		float decisionAlbumResidual = albumResponsive.Residual;

		ReleasePlan plan = new() {
			format = albumWins ? ReleaseFormat.Album : ReleaseFormat.Single,
			strategy = albumWins ? (promoPreferred ? ReleaseStrategy.AlbumWithPromo : ReleaseStrategy.AlbumStandalone) : ReleaseStrategy.OrphanSingle,
			economicsEvaluated = true,
			priorSingleNet = priorSingle,
			priorAlbumNet = priorAlbum,
			projectedSingleNet = projectedSingle,
			projectedAlbumNet = albumGateProjection,
			projectedOrphanSingleNet = projectedSingle,
			projectedAlbumStandaloneNet = projectedStandaloneAlbum,
			projectedAlbumWithPromoNet = albumWins ? projectedAlbumWithPromo : projectedAlbum,
			confidenceSingle = confidenceSingle,
			confidenceAlbum = decisionAlbumConfidence,
			rawConfidenceSingle = rawConfidenceSingle,
			rawConfidenceAlbum = rawConfidenceAlbum,
			singleMemoryCapApplied = applyLiveMemoryCeiling && rawConfidenceSingle > confidenceSingle,
			albumMemoryCapApplied = applyLiveMemoryCeiling && rawConfidenceAlbum > confidenceAlbum,
			legacyFourResolvableSingles = hitInventory.resolvedSingles >= 4,
			compCostWeight = compCostWeight,
			expectedFormatMultiplier = albumPrior.expectedFormatMultiplier,
			releasedSingleIdsExamined = hitInventory.idsExamined,
			resolvedSingles = hitInventory.resolvedSingles,
			chartedSingles = hitInventory.chartedSingles,
			hitScore = hitInventory.hitScore,
			unweightedHitUnits = albumPrior.unweightedHitUnits,
			weightedHitUnits = albumPrior.weightedHitUnits,
			affinityUnits = albumPrior.affinityUnits,
			totalExpectedAlbumUnits = albumPrior.totalExpectedUnits,
			qualityQuartile = decision.qualityQuartile,
			careerBand = decision.careerBand,
			unexpectedCareerState = decision.unexpectedCareerState,
			singleProductionCost = decision.singleProductionCost,
			singlePreTiltContribution = singlePreTiltContribution, singleFormatTilt = singleFormatTilt,
			albumAffinity = albumPrior.albumAffinity, albumOpportunity = albumPrior.albumOpportunity,
			albumFormatTilt = albumPrior.formatTilt, albumPreTiltContribution = albumPrior.preTiltAffinityUnits,
			albumProductionCost = albumPrior.productionCost,
			singleMemoryEma = useResponsiveMemory ? singleResponsive.Residual : singleMemory.emaNetPerRelease,
			albumMemoryEma = useResponsiveMemory ? decisionAlbumResidual : albumMemory.emaNetPerRelease,
			singleMemoryBlend = useResponsiveMemory ? projectedSingle : Mathf.Lerp(priorSingle, singleMemory.emaNetPerRelease, confidenceSingle),
			albumMemoryBlend = useResponsiveMemory ? albumGateProjection : Mathf.Lerp(priorAlbum, albumMemory.emaNetPerRelease, confidenceAlbum),
			labelFormatMemoryBypassed = decision.nonRetainedEmergingProject,
			singleNoiseMultiplier = singleNoiseMultiplier, albumNoiseMultiplier = albumNoiseMultiplier,
			expectedPromoSingleNet = expectedPromoSingleNet,
			albumStrategyEvaluated = albumWins,
			singleNetMarginPerUnit = singleNetMarginPerUnit,
			expectedSingleUnits = expectedSingleUnits,
			albumDemandFactor = albumDemandFactor,
			substitutionK = substitutionK,
			substitutionCap = substitutionCap,
			substitutionPropensity = substitutionPropensity,
			expectedOverlapFraction = expectedOverlapFraction,
			divertedUnits = divertedUnits,
			albumMarginPerUnit = albumPrior.marginPerUnit,
			cannibalizationLoss = cannibalizationLoss,
			cannibalizationCharged = chargedCannibalizationLoss,
			expectedPromoLift = expectedPromoLift,
			promoAdvantage = promoAdvantage,
			albumChoiceProbability = albumChoiceProbability,
			formatChoiceRoll = formatChoiceRoll,
			albumCapacityReroute = albumCapacityReroute
		};
		return plan;
	}

	internal static (bool AlbumWins, bool PromoPreferred, float AlbumGateProjection) ResolveAlbumDecision(
		float projectedSingle, float projectedAlbumEligibility, float projectedStandaloneAlbum,
		float componentProjectedAlbumWithPromo, float totalProjectMemoryProjection, float promoProjectDelayPremium,
		float albumEligibilityScale = 1f) {
		// Viability is judged on current component economics, not on the memory-adjusted
		// projection. Gating it on total-project memory made the strategy self-trapping:
		// a negative lane residual vetoed promo everywhere at once, and because a vetoed
		// strategy generates no further evidence, the lane could never recover. Promo
		// share collapsed .53 -> .004 in one year with component promoAdvantage at
		// +24,763. Memory still ranks strategies through the component projections; it
		// no longer holds a permanent veto over one.
		bool promoPreferred = componentProjectedAlbumWithPromo > projectedStandaloneAlbum &&
			componentProjectedAlbumWithPromo > 0f;
		// A promo project consumes two release products. Preserve its calibrated
		// portfolio eligibility weight here; the physical Album component still
		// has to beat the orphan-Single alternative after the configured drop
		// delay's annualized opportunity cost. Total-project memory is viability-only.
		float albumGateProjection = promoPreferred
			? Mathf.Min(projectedAlbumEligibility, componentProjectedAlbumWithPromo * PromoProjectEligibilityWeight)
			: projectedAlbumEligibility;
		albumGateProjection *= Mathf.Max(0f, albumEligibilityScale);
		float delayHurdle = promoPreferred
			? projectedSingle + Mathf.Max(0f, promoProjectDelayPremium) * Mathf.Max(1f, Mathf.Abs(projectedSingle))
			: projectedSingle;
		return (albumGateProjection > delayHurdle, promoPreferred, albumGateProjection);
	}

	internal static float CalculateAlbumChoiceProbability(float projectedSingle, float projectedAlbum) {
		if (projectedAlbum <= 0f) return 0f;
		if (projectedSingle <= 0f) return 1f;
		float economicShare = projectedAlbum / Mathf.Max(.000001f, projectedSingle + projectedAlbum);
		// A bounded logistic preserves crossover choices near the economic fork
		// without letting the much larger population of weak Album propositions
		// accumulate a systemic project-count increase. Equal propositions remain
		// exactly 50/50 and the isolated roll preserves deterministic replay.
		float centeredShare = Mathf.Clamp(economicShare, 0f, 1f) - .5f;
		float logisticShare = 1f / (1f + Mathf.Exp(-FormatChoiceLogitSlope * centeredShare));
		return Mathf.Lerp(FormatChoiceExplorationFloor, 1f - FormatChoiceExplorationFloor, logisticShare);
	}

	internal static bool ResolvePositiveFormatChoice(float projectedSingle, float projectedAlbum, float roll) =>
		projectedAlbum > 0f && (projectedSingle <= 0f ||
			Mathf.Clamp(roll, 0f, 1f) < CalculateAlbumChoiceProbability(projectedSingle, projectedAlbum));

	internal static float GetDeterministicFormatChoiceRoll(
		string labelId, string artistId, int year, int week, int sequence) {
		uint hash = 2166136261u;
		foreach (char value in
			$"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{labelId}|{artistId}|{year}|{week}|{sequence}|FormatChoiceV1") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	// A deal's masters ownership is renegotiated when the contract renews: during the late-60s
	// P&D consolidation a major increasingly took the masters at renewal, which is what folds the
	// renewed catalogue into the major's control chart share. GenerateDealTerms only rolls masters
	// at original signing, so without this the ramped rate never propagates to the large pool of
	// long-lived renewing deals and the owner-Major line stays flat. ownsMasters is a metric-only
	// flag (it feeds IsMajorMasterControlled and telemetry, nothing in the live sim), so re-rolling
	// it at renewal changes no economics. The roll uses the seed-stable FNV hash rather than the
	// global RNG stream, so breadth and tier composition stay byte-identical.
	internal static float GetDeterministicMastersRenewalRoll(
		string clientId, string distributorId, int year, int week) {
		uint hash = 2166136261u;
		foreach (char value in
			$"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{clientId}|{distributorId}|{year}|{week}|MastersRenewalV1") {
			hash ^= value;
			hash *= 16777619u;
		}
		return (hash & 0x00ffffffu) / 16777216f;
	}

	private float CurrentDealMastersRate(AILabel distributor, DealOrigin origin, int year) {
		float rate = origin == DealOrigin.DistributorCourted ? pushMastersOwnershipRate : 0.15f;
		if (distributor != null && distributor.tier == LabelTier.Major)
			rate = Mathf.Max(rate, GetMajorMastersOwnershipRate(year));
		return rate;
	}

	private void RerollMastersOnRenewal(AILabel client, AILabel distributor, DistributionDeal deal, int year, int currentWeek) {
		if (client == null || distributor == null || deal == null) return;
		deal.ownsMasters = GetDeterministicMastersRenewalRoll(client.labelId, distributor.labelId, year, currentWeek)
			< CurrentDealMastersRate(distributor, deal.origin, year);
	}

	/// <summary>
	/// LP programs are portfolio commitments, not independent one-week products. The
	/// lane split's short-horizon component memory otherwise makes a label
	/// progressively abandon Albums exactly as the LP market matures.
	///
	/// Commitment is earned, not conferred by tier. Running an LP program needs shelf
	/// space to place it and roster depth to keep it fed; a label holding both commits
	/// like a major whatever its tier reads, and one holding neither stays a jobbing
	/// singles house. The former tier lookup was approximating this, but it reached
	/// only the Major tier — roughly a tenth of release volume — so the market-wide
	/// LP shift could not emerge from it.
	/// </summary>
	private static float GetAlbumPortfolioCommitmentMultiplier(AILabel label, int year) {
		float era = AlbumModel.GetAlbumEraWeight(year);
		if (label == null || era <= 0f) return 1f;
		return 1f + AlbumPortfolioCommitmentCeiling * era *
			CalculateAlbumPortfolioCapacity(label.distributionStrength, label.CurrentRosterSize);
	}

	/// <summary>Shared, fixed-input Album portfolio capacity seam for probes.</summary>
	internal static float CalculateAlbumPortfolioCapacity(float distributionStrength, int rosterSize) =>
		Mathf.Clamp(AlbumProgramReachWeight * Mathf.Clamp(distributionStrength, 0f, 1f) +
			(1f - AlbumProgramReachWeight) *
			Mathf.Clamp(rosterSize / AlbumProgramRosterDepth, 0f, 1f), 0f, 1f);

	internal static float GetAlbumPortfolioCommitmentMultiplierForProbe(
		float distributionStrength, int rosterSize, int year) =>
		1f + AlbumPortfolioCommitmentCeiling * AlbumModel.GetAlbumEraWeight(year) *
			CalculateAlbumPortfolioCapacity(distributionStrength, rosterSize);

	/// <summary>
	/// Album units recruited by a promo Single, on the same terms as the diverted
	/// units it is weighed against: both scale with album demand, the Single's reach
	/// and the Album's margin. Recruitment is gated on awareness headroom because a
	/// Single adds least where the launch is already well known, which is exactly
	/// where diversion is gated on shared shelf overlap instead.
	/// </summary>
	internal static float CalculatePromoAlbumSynergyGain(float albumDemandFactor,
		float awarenessHeadroom, float expectedSingleUnits, float albumMarginPerUnit) =>
		Mathf.Max(0f, PromoAlbumConversionK * Mathf.Max(0f, albumDemandFactor) *
			Mathf.Lerp(PromoAwarenessConversionFloor, 1f, Mathf.Clamp(awarenessHeadroom, 0f, 1f)) *
			Mathf.Max(0f, expectedSingleUnits) * albumMarginPerUnit);

	/// <summary>
	/// The share of modelled promo cannibalization the format decision must still
	/// charge explicitly. The Album-component projection it adjusts is already
	/// memory-blended at <paramref name="albumMemoryConfidence"/> against realized
	/// Albums released with a promo Single, so that much of the diversion is priced
	/// in before the explicit charge is applied. Charging only the complement keeps
	/// the promo proposition on one accounting of the same effect, and is inert
	/// while the lane has no evidence.
	/// </summary>
	internal static float CalculateChargedPromoCannibalization(
		float cannibalizationLoss, float albumMemoryConfidence) =>
		Mathf.Max(0f, cannibalizationLoss) * (1f - Mathf.Clamp(albumMemoryConfidence, 0f, 1f));

	/// <summary>
	/// The Album prior's opportunity term is flat before `albumDemandRiseStartYear`
	/// while the Album market it predicts is already growing, so the prior overstates a
	/// niche early-decade LP and converges once the market matures. Decomposed against
	/// completed cohorts the error is on the revenue side — realized production cost is
	/// only ~9% of expected revenue — so it is genuinely a unit forecast error.
	///
	/// The correction is deliberately partial. Fitting it fully (.65/.66/.74/1.00, from
	/// measured revenue realization) aborts the gate at 1961 — 798 Album projects against
	/// 1257 — because the control's early-decade Album counts *require* an over-projecting
	/// prior. That is not obviously a defect: the prior is the label's belief, not ground
	/// truth, and early-60s A&R genuinely over-committed to LPs. The error also closes on
	/// its own as the market matures (realized/prior .949 by 1964) with the memory residual
	/// as the learning mechanism, so only the pre-boom years are damped.
	///
	/// Both endpoints are excluded. 1960 is the bootstrap year on a seeded catalog and
	/// already measures nearly correct (.913); 1964 onward is correct without help, and
	/// discounting it over-corrected to 1.106.
	///
	/// Applied on the live route only. It is a calibration of the live Album prior against
	/// live realized outcomes, so applying it to the disabled route would move the gate
	/// reference by the same amount it moves the thing being measured.
	/// </summary>
	internal static float CalculateAlbumPriorEraCalibration(int year) {
		if (year <= AlbumPriorCalibrationBootstrapYear) return 1f;
		if (year >= AlbumPriorCalibrationRetiredYear) return 1f;
		return Mathf.Lerp(AlbumPriorEarlyEraDiscount, 1f, AlbumModel.GetAlbumEraWeight(year));
	}

	/// <summary>
	/// Sign-safe portfolio commitment. Equivalent in magnitude to the former scalar
	/// when the projection sits near its prior, but always moves the Album up, so a
	/// marginal or negative-net proposition is carried rather than pushed away.
	/// </summary>
	internal static float CalculateAlbumPortfolioCredit(float commitmentMultiplier, float priorAlbum) =>
		Mathf.Max(0f, commitmentMultiplier - 1f) * Mathf.Max(1f, Mathf.Abs(priorAlbum));

	private void PrepareAnnualFormatCapacity(int year) {
		if (annualFormatCapacityYear == year) return;
		annualFormatCapacityYear = year;
		annualFormatDecisions = 0;
		annualAlbumProjectsScheduled = 0;
	}

	private static bool IsAlbumProjectSharePressureHigh(int decisions, int albumProjects) =>
		decisions >= AlbumProjectPressureMinimumDecisions &&
		(float)albumProjects / Mathf.Max(1, decisions) >= AlbumProjectPressureShare;
	private static bool CanScheduleAnnualAlbumProject(int projectsAlreadyScheduled, bool albumProjectPressure) =>
		!albumProjectPressure || projectsAlreadyScheduled < MaximumAlbumProjectsPerArtistYear;
	internal static bool IsAlbumProjectSharePressureHighForProbe(int decisions, int albumProjects) =>
		IsAlbumProjectSharePressureHigh(decisions, albumProjects);
	internal static bool CanScheduleAnnualAlbumProjectForProbe(int projectsAlreadyScheduled, bool albumProjectPressure) =>
		CanScheduleAnnualAlbumProject(projectsAlreadyScheduled, albumProjectPressure);

	private float ProjectLaunchAwareness(AILabel label, SimulatedArtist artist, float marketingBudget) {
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		return Mathf.Clamp(0.04f + artistAwareness + marketingAwareness + label.reputation * 0.1f, 0f, 1f);
	}

	private static readonly float[] SinglePriorQualityCutPoints = { 0.465511f, 0.550559f, 0.623968f };
	// Rows are Q1-Q4; columns are New/Unsigned, Rising, Established, Star/Superstar.
	// Sparse cells contain the prescribed borrowed effective value, not an implicit fallback.
	private static readonly float[,] SinglePriorNormalizedContribution = {
		{ 7765.792292f, 12767.924457f, 12767.924457f, 12767.924457f },
		{ 12230.041078f, 34084.882066f, 34084.882066f, 34084.882066f },
		{ 19197.010405f, 39072.383069f, 119155.241436f, 39072.383069f },
		{ 47135.125181f, 84751.249664f, 119155.241436f, 119155.241436f }
	};
	private static readonly int[,] SinglePriorRawSampleCounts = {
		{ 3481, 105, 3, 0 }, { 3452, 124, 8, 0 },
		{ 3380, 191, 16, 0 }, { 3156, 407, 22, 0 }
	};
	// Encoded as quality-row * 4 + career-column for the effective source bucket.
	private static readonly int[,] SinglePriorSourceBuckets = {
		{ 0, 1, 1, 1 }, { 4, 5, 5, 5 },
		{ 8, 9, 14, 9 }, { 12, 13, 14, 14 }
	};
	private static readonly int[,] SinglePriorSourceSampleCounts = {
		{ 3481, 105, 105, 105 }, { 3452, 124, 124, 124 },
		{ 3380, 191, 22, 191 }, { 3156, 407, 22, 22 }
	};

	private static DecisionContext BuildDecisionContext(AILabel label, SimulatedArtist artist, int year, int month) {
		float qualityEstimate = artist.CalculateBaseQuality();
		float recordingCostMultiplier = MarketSeasonality.Enabled
			? MarketSeasonality.GetRecordingCostMultiplier(year, month, liveTick: true) : 1f;
		float singleProductionCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * recordingCostMultiplier : label.GetProductionCost();
		return new DecisionContext {
			qualityEstimate = qualityEstimate,
			reachFactor = Mathf.Max(0f, label.distributionStrength),
			genreSinglesMarketFactor = CalculateSingleGenreMarketFactor(artist.primaryGenre, year),
			singleProductionCost = singleProductionCost,
			recordingCostMultiplier = recordingCostMultiplier
		};
	}

	private static float CalculateSinglePriorNet(DecisionContext decision) {
		float expectedContribution = SinglePriorNormalizedContribution[decision.qualityQuartile, decision.careerBand]
			* decision.reachFactor * decision.genreSinglesMarketFactor;
		return expectedContribution - decision.singleProductionCost;
	}

	private float CalculateAlbumPriorNet(AILabel label, SimulatedArtist artist, int year, DecisionContext decision,
		float compCostWeight, HitInventory hitInventory, out AlbumPriorDiagnostics diagnostics) {
		float statureMultiplier = artist.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null ? ChartManager.Instance.GetAllRegions() : Enumerable.Empty<MarketRegion>();
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		AlbumPriorExplanation opportunity = GetAlbumPriorExplanation(artist.primaryGenre, regions, year, live);
		float baseAffinityUnits = priorUnitScalarAlbum * decision.qualityEstimate * statureMultiplier *
			decision.reachFactor * (live ? CalculateAlbumPriorEraCalibration(year) : 1f);
		float preTiltAffinityUnits = baseAffinityUnits * opportunity.UntiltedAlbumDemandFactor * opportunity.MarketReconciliation;
		float unweightedHitUnits = priorCompHitUnitScalar * hitInventory.hitScore;
		float weightedHitUnits = compCostWeight * unweightedHitUnits;
		// Format suitability applies to the whole Album proposition. Applying it
		// only to affinity units let compilation/hit inventory bypass the catalog
		// orientation and dominate the late format fork.
		float preTiltExpectedUnits = preTiltAffinityUnits + weightedHitUnits;
		float affinityUnits = preTiltAffinityUnits * opportunity.FormatTilt;
		float expectedUnits = CalculateFormatTiltedAlbumExpectedUnits(
			preTiltAffinityUnits, weightedHitUnits, opportunity.FormatTilt);
		float manufacturingPerUnit = GetPressingCostPerUnit(ReleaseFormat.Album) + albumPackagingCostPerUnit * priorAssumedAlbumPackaging;
		float grossAfterManufacturing = Mathf.Max(0f, GetPricePerUnit(ReleaseFormat.Album) - manufacturingPerUnit);
		float skimFraction = label.activeDeal != null
			? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
			: 0.25f * (1f - label.ownedReach);
		float royaltyRate = artist?.royaltyRate ?? baseRoyaltyRate;
		float marginPerUnit = grossAfterManufacturing * (1f - skimFraction) - GetPricePerUnit(ReleaseFormat.Album) * royaltyRate;
		float multiplier = compCostWeight * compilationProductionMultiplier + (1f - compCostWeight) * 2.4f;
		float productionCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * decision.recordingCostMultiplier * multiplier + albumPackagingFixedCost * priorAssumedAlbumPackaging
			: label.GetProductionCost() * multiplier + albumPackagingFixedCost * priorAssumedAlbumPackaging;
		float expectedRevenueAtMargin = expectedUnits * marginPerUnit;
		diagnostics = new AlbumPriorDiagnostics {
			expectedFormatMultiplier = multiplier,
			affinityUnits = affinityUnits,
			unweightedHitUnits = unweightedHitUnits,
			weightedHitUnits = weightedHitUnits,
			totalExpectedUnits = expectedUnits,
			expectedRevenueAtMargin = expectedRevenueAtMargin,
			marginPerUnit = marginPerUnit,
			albumAffinity = opportunity.AlbumAffinity,
			albumOpportunity = opportunity.UntiltedAlbumDemandFactor,
			formatTilt = opportunity.FormatTilt,
			preTiltAffinityUnits = preTiltExpectedUnits,
			productionCost = productionCost
		};
		return expectedRevenueAtMargin - productionCost;
	}

	internal static float CalculateFormatTiltedAlbumExpectedUnits(
		float preTiltAffinityUnits, float weightedHitUnits, float formatTilt) =>
		Mathf.Max(0f, preTiltAffinityUnits + weightedHitUnits) * Mathf.Max(0f, formatTilt);

	private static int GetQualityQuartile(float qualityEstimate) {
		if (qualityEstimate <= SinglePriorQualityCutPoints[0]) return 0;
		if (qualityEstimate <= SinglePriorQualityCutPoints[1]) return 1;
		if (qualityEstimate <= SinglePriorQualityCutPoints[2]) return 2;
		return 3;
	}

	private float GetExpectedPeakScore(int qualityQuartile, int careerBand) {
		int index = Mathf.Clamp(qualityQuartile, 0, 3) * 4 + Mathf.Clamp(careerBand, 0, 3);
		return expectedPeakScoreByBucket != null && index < expectedPeakScoreByBucket.Length
			? Mathf.Clamp(expectedPeakScoreByBucket[index], 0f, 1f) : 0f;
	}

	private static float CalculatePromoPeakScore(float peakPosition, int flopThreshold) =>
		peakPosition <= 0f || peakPosition > flopThreshold ? 0f :
			(flopThreshold - peakPosition) / Mathf.Max(1f, flopThreshold - 1f);

	private static int GetCareerBandIndex(CareerState state, out bool unexpected) {
		unexpected = false;
		return state switch {
			CareerState.NewSigning or CareerState.Unsigned => 0,
			CareerState.Rising => 1,
			CareerState.Established => 2,
			CareerState.Star or CareerState.Superstar => 3,
			_ => UnexpectedCareerFallback(out unexpected)
		};
	}

	internal static bool IsEligibleForEnabledFormatDecision(SimulatedArtist artist) =>
		GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist);

	private static int UnexpectedCareerFallback(out bool unexpected) { unexpected = true; return 0; }
	private static string GetCareerBandLabel(int band, bool unexpected) => unexpected ? "New/Unsigned (unexpected-state fallback)" :
		band switch { 0 => "New/Unsigned", 1 => "Rising", 2 => "Established", _ => "Star/Superstar" };

	private float CalculateProductionCost(AILabel label, Record record, GameDate date) {
		float baseCost = MarketSeasonality.Enabled
			? label.GetProductionCost() * MarketSeasonality.GetRecordingCostMultiplier(date.year, date.month, liveTick: true)
			: label.GetProductionCost();
		if (record?.format != ReleaseFormat.Album) return baseCost;
		float multiplier = record.album?.albumFormat == AlbumFormat.Compilation
			? compilationProductionMultiplier
			: 2.4f;
		return baseCost * multiplier + albumPackagingFixedCost * (record.album?.packaging ?? 0f);
	}

	/// <summary>
	/// The prior a label budgets against before the format is drawn, so it has to be the
	/// same probability <see cref="GenerateAlbum"/> actually rolls. When the two drift, the
	/// projected album economics stop matching the realised ones -- which is what
	/// prior-cost-assumptions.csv exists to catch.
	/// </summary>
	private static float CalculateCompilationCostWeight(Genre genre, int year) =>
		AlbumModel.GetCompilationChance(genre, year);

	private HitInventory ResolveHitInventory(SimulatedArtist artist) {
		var result = new HitInventory();
		if (artist?.releasedSingleIds == null || ChartManager.Instance == null) return result;
		foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
			result.idsExamined++;
			if (!ChartManager.Instance.TryGetTrackSnapshot(recordId, out AlbumTrack track)) continue;
			result.resolvedSingles++;
			if (track.peakPosition is >= 1 and <= 100) {
				result.chartedSingles++;
				float freshness = GetSourceHitFreshness(recordId, track);
				result.hitScore += freshness * (101f - track.peakPosition) / 100f;
			}
			if (result.resolvedSingles >= 4) break;
		}
		return result;
	}

	private float GetSourceHitFreshness(string recordId, AlbumTrack track = null) {
		float perUseFreshness = ChartManager.Instance?.GetCompFreshness(recordId) ?? 1f;
		if (track == null && (ChartManager.Instance == null ||
			!ChartManager.Instance.TryGetTrackSnapshot(recordId, out track))) return perUseFreshness;
		if (track.releaseDate.year <= 0 || TimeManager.Instance == null) return perUseFreshness;

		GameDate currentDate = TimeManager.Instance.CurrentDate;
		int ageWeeks = currentDate >= track.releaseDate ? currentDate.WeeksDifference(track.releaseDate) : 0;
		float annualDecay = Mathf.Clamp(hitRecencyDecay, 0f, 1f);
		return perUseFreshness * Mathf.Pow(annualDecay, ageWeeks / 52f);
	}

	private static float CalculateSingleGenreMarketFactor(Genre genre, int year) {
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null
			? ChartManager.Instance.GetAllRegions()
			: Enumerable.Empty<MarketRegion>();
		MarketRegion[] regionArray = regions.ToArray();
		if (regionArray.Length == 0) return 1f;
		float selected = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
		// New enabled projects can only be supplied by this year's lifecycle-filtered
		// catalog. Including unavailable future genres in the comparison pool lowers
		// the denominator and turns nearly every live decision into the cap.
		IReadOnlyList<Genre> genres = GenreMarketV2.Enabled
			? GenreSupplyService.GetAvailableGenres(year)
			: GenreDomains.Current;
		float relativeMarket = CalculateRelativeSingleMarketFactor(selected,
			genres.Select(candidate => regionArray.Sum(region => region.GetGenreMarketSize(candidate, year))));
		return relativeMarket * GetFormatPriorMultiplier(genre, ReleaseFormat.Single, year);
	}

	/// <summary>Shared, fixed-input relative-market seam for live AI decisions and audit probes.</summary>
	public static float CalculateRelativeSingleMarketFactor(float selectedMarket, IEnumerable<float> comparisonMarkets) {
		float[] markets = comparisonMarkets?.ToArray() ?? System.Array.Empty<float>();
		if (markets.Length == 0) return 1f;
		float average = markets.Average();
		return Mathf.Clamp(selectedMarket / Mathf.Max(1f, average), 0.70f, 1.30f);
	}

	/// <summary>AI format priors deliberately share the realized demand tilt seam.</summary>
	public static float GetFormatPriorMultiplier(Genre genre, ReleaseFormat format, int year,
		bool? liveOverride = null, float? albumOpportunityOverride = null) {
		bool live = liveOverride ?? (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		return GenreAcceptanceService.GetLiveFormatMultiplier(genre, genre, format, year,
			albumOpportunityOverride ?? GetNationalAlbumOpportunity(genre, year, live), live);
	}

	private static float GetNationalAlbumOpportunity(Genre genre, int year, bool live) {
		MarketRegion[] regions = ChartManager.Instance?.GetAllRegions()?.Where(region => region != null).ToArray()
			?? System.Array.Empty<MarketRegion>();
		if (regions.Length == 0) return .5f;
		// The live branch centers on the genre-blind market split, because that is what realized
		// demand centers on and this seam exists to share it. The accepted branch keeps its frozen
		// genre-scoped pool ratio.
		return live
			? CalculateMarketAlbumOpportunityFactor(regions, year)
			: CalculateAcceptedAlbumOpportunityFactor(genre, regions, year);
	}

	/// <summary>
	/// Genre-blind national Album share of the market, weighted by buying population. This is the
	/// format-centering counterpart to CalculateEnabledAlbumOpportunityFactor, which remains
	/// genre-scoped because it sizes Album demand rather than centering the tilt.
	/// </summary>
	public static float CalculateMarketAlbumOpportunityFactor(IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float buyingPopulation = regionArray.Sum(region => region.population * 1000000f * region.GetBuyingPopulationPercentage());
		if (buyingPopulation <= 0f) return .5f;
		return Mathf.Clamp(regionArray.Sum(region => region.population * 1000000f * region.GetBuyingPopulationPercentage() *
			region.GetMarketAlbumOpportunityWeight(year)) / buyingPopulation, 0f, 1f);
	}

	/// <summary>
	/// Shared fixed-input Album opportunity. Both format centering and the Album
	/// AI prior use the accepted pre-tilt Album pool over the accepted legacy
	/// genre pool; neither side may substitute the enabled routed genre market.
	/// </summary>
	public static float CalculateAcceptedAlbumOpportunityFactor(Genre genre, IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float acceptedAlbumPool = regionArray.Sum(region => region.GetAcceptedPreTiltAlbumMarketSize(genre, year));
		float acceptedGenrePool = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		return Mathf.Clamp(acceptedAlbumPool / Mathf.Max(1f, acceptedGenrePool), 0f, 1f);
	}

	/// <summary>Enabled Album opportunity, weighted by the routed canonical genre market.</summary>
	public static float CalculateEnabledAlbumOpportunityFactor(Genre genre, IEnumerable<MarketRegion> regions, float year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float routedGenrePool = regionArray.Sum(region => region.GetGenreMarketSize(genre, (int)year));
		if (routedGenrePool <= 0f) return 0f;
		float weightedOpportunity = regionArray.Sum(region =>
			region.GetGenreMarketSize(genre, (int)year) * region.GetEnabledAlbumOpportunityWeight(genre, year));
		return Mathf.Clamp(weightedOpportunity / routedGenrePool, 0f, 1f);
	}

	public readonly struct AlbumPriorExplanation {
		public readonly float AcceptedAlbumPool, AcceptedLegacyGenrePool, AlbumAffinity, UntiltedAlbumDemandFactor;
		public readonly float MarketReconciliation, FormatTilt, AlbumPrior;
		public AlbumPriorExplanation(float acceptedAlbumPool, float acceptedLegacyGenrePool, float untiltedAlbumDemandFactor,
			float albumAffinity, float marketReconciliation, float formatTilt, float albumPrior) {
			AcceptedAlbumPool = acceptedAlbumPool;
			AcceptedLegacyGenrePool = acceptedLegacyGenrePool;
			AlbumAffinity = albumAffinity;
			UntiltedAlbumDemandFactor = untiltedAlbumDemandFactor;
			MarketReconciliation = marketReconciliation;
			FormatTilt = formatTilt;
			AlbumPrior = albumPrior;
		}
	}

	/// <summary>Fixed-input decomposition for the Album AI-prior audit seam.</summary>
	public static AlbumPriorExplanation GetAlbumPriorExplanation(Genre genre, IEnumerable<MarketRegion> regions, int year, bool live) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		float acceptedAlbumPool = regionArray.Sum(region => region.GetAcceptedPreTiltAlbumMarketSize(genre, year));
		float acceptedGenrePool = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		float albumAffinity;
		if (live) {
			float routedGenrePool = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
			albumAffinity = regionArray.Sum(region =>
				region.GetGenreMarketSize(genre, year) * region.GetAlbumAffinity(genre, year)) /
				Mathf.Max(1f, routedGenrePool);
		} else {
			albumAffinity = regionArray.Sum(region =>
				region.GetAcceptedLegacyGenreMarketSize(genre, year) * region.GetAlbumAffinity(genre, year)) /
				Mathf.Max(1f, acceptedGenrePool);
		}
		float untilted = live
			? CalculateEnabledAlbumOpportunityFactor(genre, regionArray, year)
			: Mathf.Clamp(acceptedAlbumPool / Mathf.Max(1f, acceptedGenrePool), 0f, 1f);
		float marketReconciliation = live
			? CalculateAlbumPriorMarketReconciliation(genre, regionArray, year)
			: 1f;
		// UntiltedAlbumDemandFactor sizes Album demand and stays genre-scoped; the tilt it is
		// multiplied by centers on the genre-blind market split, matching realized demand.
		float centeringOpportunity = live ? CalculateMarketAlbumOpportunityFactor(regionArray, year) : untilted;
		float formatTilt = GetFormatPriorMultiplier(genre, ReleaseFormat.Album, year, live, centeringOpportunity);
		return new AlbumPriorExplanation(acceptedAlbumPool, acceptedGenrePool, untilted, albumAffinity,
			marketReconciliation, formatTilt, untilted * marketReconciliation * formatTilt);
	}

	/// <summary>
	/// Live Album and Single priors use the same routed relative-market factor.
	/// The Album prior's unit scalars were calibrated against the legacy relative
	/// market, so a genre that has one must still be renormalized by it; dropping
	/// that divisor handed every large legacy genre an unearned Album uplift at the
	/// fork. Genres with no legacy comparator keep the bare routed factor, which is
	/// what the divisor form could not express without erasing their Album affinity.
	/// </summary>
	public static float CalculateAlbumPriorMarketReconciliation(Genre genre, IEnumerable<MarketRegion> regions, int year) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		if (regionArray.Length == 0) return 1f;
		float routedSelected = regionArray.Sum(region => region.GetGenreMarketSize(genre, year));
		IReadOnlyList<Genre> supplied = GenreSupplyService.GetAvailableGenres(year);
		float routedRelative = CalculateRelativeSingleMarketFactor(routedSelected,
			supplied.Select(candidate => regionArray.Sum(region => region.GetGenreMarketSize(candidate, year))));
		float acceptedSelected = regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(genre, year));
		if (acceptedSelected <= 0f) return routedRelative;
		float acceptedRelative = CalculateRelativeSingleMarketFactor(acceptedSelected,
			GenreDomains.LegacyDomain.Select(candidate => regionArray.Sum(region => region.GetAcceptedLegacyGenreMarketSize(candidate, year))));
		return Mathf.Clamp(routedRelative / Mathf.Max(.000001f, acceptedRelative), .25f, 4f);
	}

	/// <summary>Side-effect-free binary format decision decomposition for fixed probes.</summary>
	public readonly struct FormatDecisionExplanation {
		public readonly float SinglePreTiltContribution, AlbumPreTiltContribution, AlbumAffinity, AlbumOpportunity;
		public readonly float SingleTilt, AlbumTilt, SingleProductionCost, AlbumProductionCost;
		public readonly float SingleMemoryBlend, AlbumMemoryBlend, SingleNoise, AlbumNoise, FinalSingleMargin, FinalAlbumMargin;
		public readonly ReleaseFormat Choice;
		public FormatDecisionExplanation(float singlePreTiltContribution, float albumPreTiltContribution, float albumAffinity,
			float albumOpportunity, float singleTilt, float albumTilt, float singleProductionCost, float albumProductionCost,
			float singleMemoryBlend, float albumMemoryBlend, float singleNoise, float albumNoise) {
			SinglePreTiltContribution = singlePreTiltContribution;
			AlbumPreTiltContribution = albumPreTiltContribution;
			AlbumAffinity = albumAffinity;
			AlbumOpportunity = albumOpportunity;
			SingleTilt = singleTilt;
			AlbumTilt = albumTilt;
			SingleProductionCost = singleProductionCost;
			AlbumProductionCost = albumProductionCost;
			SingleMemoryBlend = singleMemoryBlend;
			AlbumMemoryBlend = albumMemoryBlend;
			SingleNoise = singleNoise;
			AlbumNoise = albumNoise;
			FinalSingleMargin = singleMemoryBlend * singleNoise;
			FinalAlbumMargin = albumMemoryBlend * albumNoise;
			Choice = FinalAlbumMargin > FinalSingleMargin ? ReleaseFormat.Album : ReleaseFormat.Single;
		}
	}

	public static FormatDecisionExplanation ExplainFixedFormatDecision(float singlePreTiltContribution, float albumPreTiltContribution,
		float albumAffinity, float albumOpportunity, float singleTilt, float albumTilt, float singleProductionCost,
		float albumProductionCost, float singleMemory = 0f, float albumMemory = 0f, float singleNoise = 1f, float albumNoise = 1f) {
		float singlePrior = singlePreTiltContribution * singleTilt - singleProductionCost;
		float albumPrior = albumPreTiltContribution * albumTilt - albumProductionCost;
		float singleBlend = singleMemory == 0f ? singlePrior : singleMemory;
		float albumBlend = albumMemory == 0f ? albumPrior : albumMemory;
		return new FormatDecisionExplanation(singlePreTiltContribution, albumPreTiltContribution, albumAffinity, albumOpportunity, singleTilt, albumTilt,
			singleProductionCost, albumProductionCost, singleBlend, albumBlend, singleNoise, albumNoise);
	}

	public static float CalculateAlbumDemandFactor(Genre genre, int year) {
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null
			? ChartManager.Instance.GetAllRegions()
			: Enumerable.Empty<MarketRegion>();
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		return GetAlbumPriorExplanation(genre, regions, year, live).AlbumPrior;
	}

	private struct ReleasePlan {
		public ReleaseFormat format;
		public ReleaseStrategy strategy;
		public bool economicsEvaluated;
		public float priorSingleNet;
		public float priorAlbumNet;
		public float projectedSingleNet;
		public float projectedAlbumNet;
		public float projectedOrphanSingleNet;
		public float projectedAlbumStandaloneNet;
		public float projectedAlbumWithPromoNet;
		public float expectedPromoSingleNet;
		public bool albumStrategyEvaluated;
		public float singleProductionCost;
		public float singleNetMarginPerUnit;
		public float expectedSingleUnits;
		public float albumDemandFactor;
		public float substitutionK;
		public float substitutionCap;
		public float substitutionPropensity;
		public float expectedOverlapFraction;
		public float divertedUnits;
		public float albumMarginPerUnit;
		public float cannibalizationLoss;
		public float cannibalizationCharged;
		public float expectedPromoLift;
		public float promoAdvantage;
		public float albumChoiceProbability;
		public float formatChoiceRoll;
		public bool albumCapacityReroute;
		public float singlePreTiltContribution;
		public float singleFormatTilt;
		public float albumAffinity;
		public float albumOpportunity;
		public float albumFormatTilt;
		public float albumPreTiltContribution;
		public float albumProductionCost;
		public float singleMemoryEma;
		public float albumMemoryEma;
	public float singleMemoryBlend;
	public float albumMemoryBlend;
	public bool labelFormatMemoryBypassed;
	public float singleNoiseMultiplier;
		public float albumNoiseMultiplier;
		public float confidenceSingle;
		public float confidenceAlbum;
		public float rawConfidenceSingle;
		public float rawConfidenceAlbum;
		public bool singleMemoryCapApplied;
		public bool albumMemoryCapApplied;
		public bool legacyFourResolvableSingles;
		public float compCostWeight;
		public float expectedFormatMultiplier;
		public int releasedSingleIdsExamined;
		public int resolvedSingles;
		public int chartedSingles;
		public float hitScore;
		public float unweightedHitUnits;
		public float weightedHitUnits;
		public float affinityUnits;
		public float totalExpectedAlbumUnits;
		public int qualityQuartile;
		public int careerBand;
		public bool unexpectedCareerState;
	}

	private struct HitInventory {
		public int idsExamined;
		public int resolvedSingles;
		public int chartedSingles;
		public float hitScore;
	}

	private struct AlbumPriorDiagnostics {
		public float expectedFormatMultiplier;
		public float affinityUnits;
		public float unweightedHitUnits;
		public float weightedHitUnits;
		public float totalExpectedUnits;
		public float expectedRevenueAtMargin;
		public float marginPerUnit;
		public float albumAffinity;
		public float albumOpportunity;
		public float formatTilt;
		public float preTiltAffinityUnits;
		public float productionCost;
	}

	private struct DecisionContext {
		public float qualityEstimate;
		public int qualityQuartile;
		public int careerBand;
		public bool unexpectedCareerState;
		public float reachFactor;
		public float genreSinglesMarketFactor;
		public float singleProductionCost;
		public float recordingCostMultiplier;
		public bool nonRetainedEmergingProject;
	}
	
	private Record GenerateRecordFromArtist(AILabel label, SimulatedArtist artist, int year, ReleaseFormat format = ReleaseFormat.Single) {
		var record = new Record(); // Godot Resource instantiation
		generatedRecordCounter++;
		record.recordId = $"gen_{generatedRecordCounter}";
		record.labelId = label.labelId;
		record.format = format;
		record.isPlayerOwned = false;
		record.artistName = artist.stageName;
		record.artistId = artist.artistId;
		record.primaryGenre = artist.primaryGenre;
		record.secondaryGenre = artist.secondaryGenre;
		
		if (NameGenerator.Instance != null) {
			record.title = NameGenerator.Instance.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		} else {
			record.title = $"Song {generatedRecordCounter}";
		}
		
		float artistQuality = artist.CalculateRecordQuality();
		float studioMod = 1f;
		if (label.strongRegions != null && label.strongRegions.Length > 0) {
			var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
			if (region != null) studioMod = ChartSimulator.GetStudioQualityModifier(region);
		}
		
		float baseQuality = (artistQuality * 0.82f) + (studioMod * 0.18f);
		baseQuality *= studioMod;
		
		record.hookStrength = Mathf.Clamp((artist.songwritingAbility * 0.55f) + (baseQuality * 0.35f) + (float)GD.RandRange(-0.12, 0.18), 0f, 1f);
		record.productionQuality = Mathf.Clamp((label.productionQuality * 0.4f) + (artist.studioPerformance * 0.3f) + (studioMod * 0.2f) + (float)GD.RandRange(-0.05, 0.1), 0f, 1f);
		record.originality = Mathf.Clamp(artist.members.Max(m => m.creativity) * 0.7f + (float)GD.RandRange(0f, 0.3f), 0f, 1f);
		record.danceability = (float)GD.RandRange(0.3, 0.95);
		record.controversy = (float)GD.RandRange(0f, 0.2f);
		
		if (record.primaryGenre == Genre.Gospel) record.controversy = Mathf.Min(record.controversy, 0.05f);
		else if (record.primaryGenre == Genre.RockAndRoll || record.primaryGenre == Genre.GarageRock) record.danceability = Mathf.Max(record.danceability, 0.5f);

		if (format == ReleaseFormat.Album) {
			record.album = GenerateAlbum(label, artist, year);
			record.title = GenerateAlbumTitle(record, year);
			record.hookStrength = record.album.pooledAppeal;
			record.productionQuality = Mathf.Clamp(record.album.pooledAppeal * 0.75f + label.productionQuality * 0.25f, 0f, 1f);
			record.danceability = record.album.pooledAppeal;
		}
		
		return record;
	}

	private Album GenerateAlbum(AILabel label, SimulatedArtist artist, int year) {
		bool useStructuredPromoTracks = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		float artistTalent = artist.CalculateBaseQuality();
		float luckyRoll = GD.Randf();
		float cohesionCeiling = AlbumModel.GetMaximumAchievableCohesion(year, artistTalent, label.productionQuality, luckyRoll);
		float thematicCohesion = Mathf.Clamp((float)GD.RandRange(0.10, cohesionCeiling), 0f, cohesionCeiling);

		AlbumFormat albumFormat;
		bool statementViable = cohesionCeiling >= 0.72f && thematicCohesion >= 0.62f;
		if (statementViable && year >= AlbumModel.EarlyStatementYear && GD.Randf() < 0.24f) {
			albumFormat = AlbumFormat.Concept;
			thematicCohesion = Mathf.Max(thematicCohesion, 0.68f);
		} else if (GD.Randf() < AlbumModel.GetCompilationChance(artist.primaryGenre, year)) {
			albumFormat = AlbumFormat.Compilation;
		} else {
			// SOUNDTRACK ORIGINATION (2026-08, D7 soundtrack subsystem): the old cosmetic
			// `typeRoll < 0.12f ? Soundtrack` branch was removed. A soundtrack/cast album is an
			// externally-originated object (film score, stage cast, tie-in) with its own demand
			// curve and economics -- it must NOT be a dice roll on an ordinary artist album. The
			// former 12% Soundtrack band folds into Standard; Live keeps its ~12% band. Real
			// soundtracks are minted by ExternalMediaService (see D7SoundtrackCastAlbumHandoff.md).
			float typeRoll = GD.Randf();
			albumFormat = typeRoll < 0.12f ? AlbumFormat.Live : AlbumFormat.Standard;
		}

		var referencedSingles = new List<AlbumTrack>();
		if (albumFormat == AlbumFormat.Compilation) {
			foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
				if (referencedSingles.Count >= 4) break;
				if (ChartManager.Instance.TryResolveTrackSnapshot(recordId, out AlbumTrack track, out _)) referencedSingles.Add(track);
			}
		}

		int targetTracks = (int)GD.RandRange(9, 13);
		var nonSingleTracks = new List<AlbumTrack>();
		float originalMaterialScale = albumFormat == AlbumFormat.Compilation ? 0.68f : albumFormat == AlbumFormat.Live ? 0.80f : 0.88f;
		while (referencedSingles.Count + nonSingleTracks.Count < targetTracks) {
			float trackQuality = Mathf.Clamp(artistTalent * originalMaterialScale + label.productionQuality * 0.12f + (float)GD.RandRange(-0.16, 0.12), 0.12f, 0.95f);
			string trackTitle = NameGenerator.Instance?.GenerateSongTitle(artist.primaryGenre, year, artist.stageName) ?? $"Album Track {nonSingleTracks.Count + 1}";
			(float hook, float production, float dance) = useStructuredPromoTracks
				? GetDeterministicTrackTraits(trackQuality, trackTitle, artist.primaryGenre)
				: (0f, 0f, 0f);
			nonSingleTracks.Add(new AlbumTrack {
				title = trackTitle,
				genre = artist.primaryGenre,
				quality = trackQuality,
				hookStrength = hook, productionQuality = production, danceability = dance,
				isReleasedSingle = false
			});
		}

		float avgTrackMinutes = (float)GD.RandRange(2.45, year >= 1967 ? 4.10 : 3.35);
		float[] referencedFreshness = referencedSingles
			.Select(track => GetSourceHitFreshness(track.sourceRecordId, track)).ToArray();
		int[] referencedCompUses = referencedSingles
			.Select(track => ChartManager.Instance.GetCompUseCount(track.sourceRecordId)).ToArray();
		var album = new Album {
			albumId = $"album_{generatedRecordCounter}",
			albumFormat = albumFormat,
			trackRefs = referencedSingles.ToArray(),
			trackRefFreshnessApplied = referencedFreshness,
			trackRefCompUsesAtGeneration = referencedCompUses,
			nonSingleTracks = nonSingleTracks.ToArray(),
			runtimeMinutes = targetTracks * avgTrackMinutes,
			thematicCohesion = thematicCohesion,
			packaging = Mathf.Clamp(label.productionQuality * Mathf.Lerp(0.35f, 0.85f, AlbumModel.GetAlbumEraWeight(year)) + (float)GD.RandRange(-0.10, 0.12), 0.05f, 1f),
			isStereo = year >= 1968 || GD.Randf() < Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp((year - 1960f) / 8f, 0f, 1f))
		};
		IEnumerable<float> qualities = referencedSingles.Select((track, index) => track.quality * referencedFreshness[index])
			.Concat(nonSingleTracks.Select(track => track.quality));
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(qualities, album.thematicCohesion, year);
		return album;
	}

	private static string GenerateAlbumTitle(Record record, int year) {
		string generated = NameGenerator.Instance?.GenerateSongTitle(record.primaryGenre, year, record.artistName);
		return string.IsNullOrWhiteSpace(generated) ? $"{record.artistName} Album" : generated;
	}

	private Record CreatePromoSingleFromAlbum(Record albumRecord) {
		Album album = albumRecord.album ?? throw new System.InvalidOperationException("Promo project requires a generated album.");
		if (album.nonSingleTracks == null || album.nonSingleTracks.Length == 0) throw new System.InvalidOperationException("Promo project album has no eligible original track.");
		bool useStructuredPromoTracks = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		int bestIndex = 0;
		for (int i = 1; i < album.nonSingleTracks.Length; i++) {
			if (useStructuredPromoTracks) {
				int selectionYear = albumRecord.releaseDate.year > 0 ? albumRecord.releaseDate.year : (TimeManager.Instance?.CurrentDate.year ?? 1960);
				float candidate = GetLeadSingleSuitability(album.nonSingleTracks[i], albumRecord.primaryGenre, selectionYear);
				float current = GetLeadSingleSuitability(album.nonSingleTracks[bestIndex], albumRecord.primaryGenre, selectionYear);
				if (candidate > current || (Mathf.IsEqualApprox(candidate, current) && string.CompareOrdinal(album.nonSingleTracks[i].title, album.nonSingleTracks[bestIndex].title) < 0)) bestIndex = i;
			} else if (album.nonSingleTracks[i].quality > album.nonSingleTracks[bestIndex].quality) {
				// Preserve the frozen disabled branch exactly: max scalar quality wins,
				// with the first occurrence retaining ties.
				bestIndex = i;
			}
		}
		AlbumTrack source = album.nonSingleTracks[bestIndex];
		(float hook, float production, float dance) = useStructuredPromoTracks
			? GetTrackTraitsForPromo(source)
			: (source.quality, source.quality, source.quality);
		var promo = new Record {
			recordId = $"gen_{++generatedRecordCounter}", title = source.title, artistName = albumRecord.artistName,
			artistId = albumRecord.artistId, labelId = albumRecord.labelId, format = ReleaseFormat.Single,
			isPlayerOwned = false, isNPC = albumRecord.isNPC, primaryGenre = source.genre,
			secondaryGenre = albumRecord.secondaryGenre, hookStrength = hook,
			productionQuality = production, danceability = dance,
			originality = albumRecord.originality, controversy = albumRecord.controversy
		};
		var remaining = album.nonSingleTracks.ToList();
		remaining.RemoveAt(bestIndex);
		var refs = album.trackRefs?.ToList() ?? new List<AlbumTrack>();
		refs.Add(new AlbumTrack {
			sourceRecordId = promo.recordId, title = source.title, genre = source.genre,
			quality = source.quality,
			hookStrength = useStructuredPromoTracks ? hook : 0f,
			productionQuality = useStructuredPromoTracks ? production : 0f,
			danceability = useStructuredPromoTracks ? dance : 0f,
			isReleasedSingle = true, peakPosition = 0
		});
		album.nonSingleTracks = remaining.ToArray();
		album.trackRefs = refs.ToArray();
		album.trackRefFreshnessApplied = (album.trackRefFreshnessApplied ?? System.Array.Empty<float>()).Append(1f).ToArray();
		album.trackRefCompUsesAtGeneration = (album.trackRefCompUsesAtGeneration ?? System.Array.Empty<int>()).Append(0).ToArray();
		album.leadSingleIds = (album.leadSingleIds ?? System.Array.Empty<string>()).Append(promo.recordId).ToArray();
		IEnumerable<float> qualities = album.trackRefs.Select((track, index) => track.quality *
			(index < album.trackRefFreshnessApplied.Length ? album.trackRefFreshnessApplied[index] : 1f))
			.Concat(album.nonSingleTracks.Select(track => track.quality));
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(qualities, album.thematicCohesion, albumRecord.releaseDate.year > 0 ? albumRecord.releaseDate.year : (TimeManager.Instance?.CurrentDate.year ?? 1960));
		return promo;
	}

	// This projection is used only for pre-trait saved tracks. It is deterministic,
	// consumes no RNG, and leaves newly generated tracks on their stored values.
	private static (float hook, float production, float dance) GetTrackTraitsForPromo(AlbumTrack track) =>
		track != null && track.HasStoredComponents
			? (track.hookStrength, track.productionQuality, track.danceability)
			: GetDeterministicTrackTraits(track?.quality ?? 0f, track?.title ?? string.Empty, track?.genre ?? Genre.TraditionalPop);

	private static (float hook, float production, float dance) GetDeterministicTrackTraits(float quality, string identity, Genre genre) {
		uint hash = 2166136261u;
		foreach (char value in $"{identity}|{genre}") { hash ^= value; hash *= 16777619u; }
		float a = ((hash & 1023u) / 1023f - .5f) * .18f;
		float b = (((hash >> 10) & 1023u) / 1023f - .5f) * .14f;
		return (Mathf.Clamp(quality + a, .02f, .98f), Mathf.Clamp(quality + b, .02f, .98f),
			Mathf.Clamp(quality - a * .45f - b * .25f, .02f, .98f));
	}

	private static float GetLeadSingleSuitability(AlbumTrack track, Genre albumGenre, int year) {
		(float hook, float production, float dance) = GetTrackTraitsForPromo(track);
		float eraDanceWeight = year >= 1965 ? .24f : .14f;
		float genreContinuity = track.genre == albumGenre ? .08f : 0f;
		return hook * .52f + production * (1f - .52f - eraDanceWeight) + dance * eraDanceWeight + genreContinuity;
	}

	private PromotionSnapshot BuildPromotionSnapshot(Record record, SimulatedArtist artist, float perceivedQualityMult) {
		var snapshot = new PromotionSnapshot {
			careerState = artist.careerState,
			artistAwareness = artist.GetNewReleaseAwarenessBonus(),
			perceivedQualityMultiplier = perceivedQualityMult
		};
		foreach (MarketRegion region in ChartManager.Instance.GetAllRegions()) {
			snapshot.regions.Add(new RegionalPromotionSnapshot {
				regionId = region.regionId,
				awarenessRandom = (float)GD.RandRange(0.8, 1.1),
				radioRandom = 1f,
				sentimentRandom = (float)GD.RandRange(-0.1, 0.15)
			});
		}
		return snapshot;
	}

	private void ApplyPromotionSnapshot(Record record, AILabel label, float marketingBudget, PromotionSnapshot snapshot,
		float awarenessBonus, float stockMultiplier) {
		RecordRuntimeData runtime = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtime == null || snapshot == null) return;
		float quality = runtime.GetQuality();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float baseAwareness = 0.04f + snapshot.artistAwareness + marketingAwareness + label.reputation * 0.1f;
		runtime.awareness = Mathf.Clamp(baseAwareness + awarenessBonus, 0f, 1f);
		runtime.radioHeat = 0f;
		float careerStockScale = snapshot.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		var regions = snapshot.regions.Select(regional => ChartManager.Instance.GetRegionById(regional.regionId))
			.Where(region => region != null).ToArray();
		var initialStock = new Dictionary<string, int>(System.StringComparer.Ordinal);
		foreach (RegionalPromotionSnapshot regional in snapshot.regions) {
			MarketRegion region = ChartManager.Instance.GetRegionById(regional.regionId);
			if (region == null) continue;
			var data = new RegionalRecordData(region.regionId);
			runtime.regionalData[region.regionId] = data;
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId, runtime.baseRecord?.recordId);
			int baseStock = Mathf.RoundToInt(ChartSimulator.CalculateInitialRegionalStock(label, region.regionId,
				careerStockScale * 0.45f, snapshot.perceivedQualityMultiplier, runtime.baseRecord?.recordId) * stockMultiplier);
			initialStock[region.regionId] = baseStock;
			data.unitsInStores = baseStock;
			data.awareness = Mathf.Clamp(runtime.awareness * regionStrength * regional.awarenessRandom, 0f, 1f);
			data.radioPlay = 0f;
			float genreFit = GetGenreFit(record.primaryGenre, region);
			data.sentiment = Mathf.Clamp(quality * 0.6f + genreFit * 0.3f + regional.sentimentRandom, -1f, 1f);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(record.primaryGenre,
			record.releaseDate.year, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true, regions, initialStock);
		foreach (MarketRegion region in regions) {
			if (runtime.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}
		runtime.initialLaunchAwareness = runtime.awareness;
		runtime.initialLaunchStock = runtime.regionalData.Values.Sum(data => data.unitsInStores);
		runtime.launchCareerState = snapshot.careerState;
		runtime.perceivedQualityMultiplier = snapshot.perceivedQualityMultiplier;
	}

	private void ProcessDueAlbumProjects(GameDate date) {
		long profileStart = SimulationPerformanceProfiler.Begin();
		for (int index = 0; index < pendingAlbumProjects.Count;) {
			AlbumProject project = pendingAlbumProjects[index];
			if (project.dropWeek > pipelineWeek) {
				index++;
				continue;
			}
			AILabel owner = GetLabel(project.currentLabelId);
			if (owner == null || !owner.IsActive) {
				project.terminalState = AlbumProjectTerminalState.Cancelled;
				RedirectCancelledPromoOutcome(project);
				pendingAlbumProjects.RemoveAt(index);
				continue;
			}
			float marketingBudget = project.albumMarketingBudgetPlanned;
			float available = Mathf.Max(0f, owner.cashReserves - owner.GetMonthlyOverhead());
			marketingBudget = Mathf.Min(marketingBudget, available * 0.8f);
			owner.cashReserves -= marketingBudget;
			owner.monthlyExpenses += marketingBudget;
			if (labelFinancials.TryGetValue(owner.labelId, out LabelFinancialHistory financials)) financials.lastMonthExpenses += marketingBudget;
			WeeklyMarketingSpend += marketingBudget;
			WeeklyMarketingEvents++;

			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(project.artistId);
			ReleasePreparedRecord(project.albumRecord, artist, owner, date, project.albumProductionCost,
				project.releaseTimeAlbumExpectedNet, ProjectRecordRole.LinkedAlbum, project.projectId);
			RecordRuntimeData promoRuntime = ChartManager.Instance.GetRecordRuntimeData(project.promoSingleId);
			int promoPeak = promoRuntime?.peakPosition ?? project.promoPeakAtDrop;
			project.promoPeakAtDrop = promoPeak;
			project.promoPeakScore = CalculatePromoPeakScore(promoPeak, promoFlopThreshold);
			project.synergyAwarenessApplied = promoAwarenessBonusMax * project.promoPeakScore;
			project.synergyStockMultiplier = project.promoPeakScore == 0f ? promoStockFlopFloor : 1f + promoStockBonusMax * project.promoPeakScore;
			ApplyPromotionSnapshot(project.albumRecord, owner, marketingBudget, project.albumPromotionSnapshot,
				project.synergyAwarenessApplied, project.synergyStockMultiplier);
			RecordRuntimeData albumRuntime = ChartManager.Instance.GetRecordRuntimeData(project.albumRecord.recordId);
			project.initialLaunchAwareness = albumRuntime?.initialLaunchAwareness ?? 0f;
			project.initialLaunchStock = albumRuntime?.initialLaunchStock ?? 0;
			if (artist != null) artist.weeksSinceLastRelease = 0;
			project.terminalState = AlbumProjectTerminalState.Released;
			WeeklyPipelineAlbumDrops++;
			pendingAlbumProjects.RemoveAt(index);
		}
		SimulationPerformanceProfiler.EndDueAlbumProjects(profileStart);
	}

	public IReadOnlyList<AlbumProject> GetAlbumProjects() => albumProjects;
	public AlbumProject GetAlbumProject(string projectId) => !string.IsNullOrEmpty(projectId) &&
		projectById.TryGetValue(projectId, out AlbumProject project) ? project : null;
	public bool HasPendingProjectForArtist(string artistId) => pendingAlbumProjects.Any(project => project.artistId == artistId);
	
	private void ApplyReleasePromotion(Record record, SimulatedArtist artist, AILabel label, float marketingBudget, float perceivedQualityMult) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		bool isAlbum = record.format == ReleaseFormat.Album;
		
		float quality = runtimeData.GetQuality();
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float labelAwareness = label.reputation * 0.1f;

		runtimeData.awareness = Mathf.Clamp((isAlbum ? 0.04f : 0.08f) + artistAwareness + marketingAwareness + labelAwareness, 0f, 1f);

		float baseRadio = quality * 0.3f;
		float pushRadio = ChartSimulator.GetCampaignImpact(label) * 0.3f;
		float payolaRadio = label.payolaWillingness * 0.15f;
		runtimeData.radioHeat = isAlbum ? 0f : Mathf.Clamp(baseRadio + pushRadio + payolaRadio, 0f, 1f);
		
		var regions = ChartManager.Instance.GetAllRegions();
		float stockScale = artist.careerState switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2.0f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1.0f
		};
		var initialStock = new Dictionary<string, int>(System.StringComparer.Ordinal);
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			float regionStrength = ChartSimulator.GetRegionalLaunchFactor(label, region.regionId, runtimeData.baseRecord?.recordId);
			int baseStock = ChartSimulator.CalculateInitialRegionalStock(label, region.regionId,
				stockScale * (isAlbum ? 0.45f : 1f), perceivedQualityMult, runtimeData.baseRecord?.recordId);
			initialStock[region.regionId] = baseStock;
			regionalData.unitsInStores = baseStock;
			regionalData.awareness = Mathf.Clamp(runtimeData.awareness * regionStrength * (float)GD.RandRange(0.8, 1.1), 0f, 1f);
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			float genreRadio = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
				? GenreAcceptanceService.GetRegionalRadioOpportunity(record.primaryGenre, record.secondaryGenre, region,
					TimeManager.Instance?.CurrentDate.year ?? 1960, ChartManager.Instance?.GetGenreMomentum(record.primaryGenre) ?? 0f)
				: 1f;
			if (MarketSeasonality.Enabled) {
				float radioOpportunity = MarketSeasonality.GetRadioOpportunity(TimeManager.Instance?.CurrentDate.year ?? 1960,
					TimeManager.Instance?.CurrentDate.month ?? 1, liveTick: true);
				regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0) * radioOpportunity * genreRadio, 0f, 1f);
			} else regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GD.RandRange(0.7, 1.0) * genreRadio, 0f, 1f);
			float genreFit = GetGenreFit(record.primaryGenre, region);
			regionalData.sentiment = Mathf.Clamp((quality * 0.6f) + (genreFit * 0.3f) + (float)GD.RandRange(-0.1, 0.15), -1f, 1f);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(record.primaryGenre,
			TimeManager.Instance?.CurrentDate.year ?? 1960, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true,
			regions, initialStock);
		foreach (MarketRegion region in regions) {
			if (runtimeData.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}

		runtimeData.initialLaunchAwareness = runtimeData.awareness;
		runtimeData.initialLaunchStock = runtimeData.regionalData.Values.Sum(data => data.unitsInStores);
		runtimeData.launchCareerState = artist.careerState;
		runtimeData.perceivedQualityMultiplier = perceivedQualityMult;
		
		if (debugMode) GD.Print($"  Promotion: Awareness={runtimeData.awareness:F2}, Radio={runtimeData.radioHeat:F2}");
	}
	
	private float BudgetToImpact(float budget, LabelTier tier) {
		float baseline = tier switch {
			LabelTier.Major => 3000f, LabelTier.MidTier => 1500f, LabelTier.Independent => 600f,
			LabelTier.Small => 250f, LabelTier.Boutique => 400f, _ => 500f
		};
		float normalized = budget / baseline;
		return Mathf.Clamp((Mathf.Log(1 + normalized * 9) / Mathf.Log(10)) / 1.5f, 0f, 1f);
	}

	private float GetSeasonalMarketingImpact(float marketingBudget, AILabel label) {
		if (!MarketSeasonality.Enabled) return BudgetToImpact(marketingBudget, label.tier) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int month = TimeManager.Instance?.CurrentDate.month ?? 1;
		float impact = BudgetToImpact(marketingBudget, label.tier) *
			MarketSeasonality.GetMarketingEfficiencyMultiplier(year, month, liveTick: true);
		return Mathf.Clamp(impact, 0f, 1f) * ChartSimulator.GetCampaignImpact(label) * 0.35f;
	}
	
	private float GetGenreFit(Genre genre, MarketRegion region) {
		if (region.genrePreferences == null) return 0.6f;
		var pref = region.genrePreferences.FirstOrDefault(p => p.genre == genre);
		return pref != null ? 0.5f + (pref.affinity * 0.5f) : 0.5f;
	}
	
	private void TrackRelease(string labelId, string recordId) {
		if (!labelActiveRecords.ContainsKey(labelId)) labelActiveRecords[labelId] = new List<string>();
		if (labelActiveRecords[labelId].Contains(recordId)) return;
		labelActiveRecords[labelId].Add(recordId);
		AILabel label = GetLabel(labelId);
		if (label == null) return;
		label.totalReleases++;
		// Output released while a distribution contract is running goes out through
		// that distributor's network. Records already in the market when the deal was
		// signed do not retroactively enter it.
		label.activeDeal?.Cover(recordId);
	}

	private void OnRecordChartUpdated(RecordRuntimeData record) {
		if (record?.baseRecord == null || string.IsNullOrEmpty(record.baseRecord.recordId)) return;
		AILabel label = GetLabel(record.baseRecord.labelId);
		if (label == null) return;
		if (record.peakPosition >= 1 && record.peakPosition <= 100) chartedLabelIds.Add(record.baseRecord.labelId);
		if (record.peakPosition > 0 && record.peakPosition <= 40 &&
			creditedLabelTop40RecordIds.Add(record.baseRecord.recordId)) label.top40Hits++;
		if (record.peakPosition == 1 &&
			creditedLabelNumberOneRecordIds.Add(record.baseRecord.recordId)) label.numberOneHits++;
	}
	
	private void OnMonthChanged(GameDate date) {
		foreach (var label in aiLabels) ProcessLabelMonth(label, date);
		if (date.month == 1) ProcessIndependentTradeDecline(date.year);
		ProcessDistributionDeals(date);
		if (debugMode) PrintMonthlyReport(date);
	}
	
	private void ProcessLabelMonth(AILabel label, GameDate date) {
		if (!label.IsActive) { ReleaseIndependentDistribution(label); return; }

		var financials = labelFinancials.TryGetValue(label.labelId, out var f) ? f : null;
		if (financials == null) {
			financials = new LabelFinancialHistory();
			labelFinancials[label.labelId] = financials;
		}
		float overhead = label.GetMonthlyOverhead();
		label.cashReserves -= overhead;
		label.monthlyExpenses += overhead;
		financials.lastMonthExpenses += overhead;
		
		float netIncome = financials.lastMonthRevenue - financials.lastMonthExpenses;
		label.lastMonthlyProfit = netIncome;
		ReinvestDistributionProfit(label, netIncome);
		GrowSelfBuiltDistributionReach(label, netIncome);
		PursueIndependentDistribution(label);
		UpdateLabelStatus(label, financials, netIncome);
		
		label.monthlyRevenue = 0f;
		label.monthlyExpenses = 0f;
		financials.lastMonthRevenue = 0f;
		financials.lastMonthExpenses = 0f;
		
		if (date.month == 1) financials.totalReleasesThisYear = 0;
	}

	public void SetDistributionOfferProcessingEnabled(bool enabled) => distributionOfferProcessingEnabled = enabled;

	// Test-only control for the forced-deal integration harness: force this client's
	// next expiry to absorb, bypassing the consolidation gate and roll.
	public void ForceConsolidationForTest(string labelId) {
		if (!string.IsNullOrEmpty(labelId)) forcedConsolidationClients.Add(labelId);
	}

	private void ReinvestDistributionProfit(AILabel label, float netIncome) {
		if (label.activeDeal == null || netIncome <= 0f) return;
		float reinvestment = netIncome * dealReinvestRate;
		label.cashReserves -= reinvestment;
		label.ownedReach = Mathf.Min(1f, label.ownedReach + (reinvestment / Mathf.Max(1f, dealReinvestCost)));
	}

	// A label with no distributor builds its own network out of retained profit. Before this
	// existed, ownedReach was written in exactly three places -- once at generation, and twice
	// here behind an activeDeal null-check -- so a label that never signed a deal was frozen at
	// its birth reach for life. Small labels are generated near 0.26 against a MidTier 0.66 and
	// a Major 0.88, which left them at roughly an eighth of the chart cutoff and locked 72% of
	// the population out of the national chart permanently.
	//
	// Surplus is measured against the label's own overhead rather than in absolute cash. The
	// deal-backed path above reinvests 0.02 of net against a 5,000,000 cost per reach point,
	// which is calibrated for Major-scale cash flow and gives a Small label nothing measurable;
	// a ratio keeps the route open to a label that is thriving at its own scale. It stays
	// uncommon because it needs profit at a multiple of overhead in a month with no loss
	// history, which most Small labels never reach, and the ceiling keeps a self-built network
	// short of the national reach the seeded Majors carry.
	private void GrowSelfBuiltDistributionReach(AILabel label, float netIncome) {
		// Section 27: a dependent "Stax" hitmaker reinvests in music, not its own distribution
		// network, so it never self-builds national reach -- it stays dependent on the major it
		// distributes through and is absorbed late-decade. This is what lets a genuinely charting
		// label remain a high-dependency absorption target instead of graduating to independence.
		if (label.distributionDependentHitmaker) return;
		if (label.activeDeal != null || netIncome <= 0f || label.consecutiveLossMonths > 0) return;
		if (label.ownedReach >= SelfBuiltReachCeiling) return;
		float overhead = Mathf.Max(1f, label.GetMonthlyOverhead());
		float surplusMultiple = netIncome / overhead;
		if (surplusMultiple < selfBuiltReachSurplusMultiple) return;
		float investment = netIncome * selfBuiltReachReinvestRate;
		label.cashReserves -= investment;
		label.ownedReach = Mathf.Min(SelfBuiltReachCeiling, label.ownedReach + selfBuiltReachMonthlyGain);
		label.nationalReach = CalculateNationalReachAfterSelfBuiltGain(
			label.nationalReach, selfBuiltNationalReachMonthlyGain, SelfBuiltNationalReachCeiling);
	}

	internal static float CalculateNationalReachAfterSelfBuiltGain(float currentReach, float monthlyGain, float ceiling) {
		float boundedCurrent = Mathf.Clamp(currentReach, 0f, 1f);
		return Mathf.Max(boundedCurrent,
			Mathf.Min(Mathf.Clamp(ceiling, 0f, 1f), boundedCurrent + Mathf.Max(0f, monthlyGain)));
	}

	internal static float CalculateNationalReachAfterCompletedDeal(
		float currentReach, float dealReachGranted, float retention, float ceiling) {
		float boundedCurrent = Mathf.Clamp(currentReach, 0f, 1f);
		return Mathf.Max(boundedCurrent, Mathf.Min(Mathf.Clamp(ceiling, 0f, 1f),
			boundedCurrent + Mathf.Max(0f, dealReachGranted) * Mathf.Max(0f, retention)));
	}

	/// <summary>
	/// Concurrent imprints a Major distributes. Two corrections to the flat ceiling.
	///
	/// It ramps: a 1960 major carried a handful of distributed imprints, and by 1968-71 the
	/// majors had taken over the independent distribution business wholesale. Holding it flat
	/// made 1960 too consolidated and left the late decade with nowhere to put the labels the
	/// independent trade's collapse displaces.
	///
	/// It scales with the network the firm owns. A label promoted into Major tier used to get
	/// the same roster as RCA the moment it crossed the line: at the decade run's 1968, the two
	/// promoted Majors charted 13.5 records each against the seeded majors' 41.1, and still held
	/// 20 of the 95 client slots. You can only distribute where you have a network, so the
	/// ceiling is worth the share of the country that network actually reaches.
	/// </summary>
	private int MajorDistributionCapacityFor(AILabel distributor) {
		int year = TimeManager.Instance?.CurrentDate.year ?? majorDistributionCeilingRampStartYear;
		float ceiling = MajorDistributionCeilingForYear(year, majorDistributionClientCeilingEarly,
			majorDistributionClientCeilingLate, majorDistributionCeilingRampStartYear, majorDistributionCeilingRampFullYear);
		float networkShare = ChartManager.Instance?.GetNationalMarketShareForRegions(distributor.distributionRegions) ?? 1f;
		return Mathf.Max(1, Mathf.RoundToInt(ceiling * Mathf.Clamp(networkShare, 0f, 1f)));
	}

	internal static float MajorDistributionCeilingForYear(int year, int early, int late, int rampStart, int rampFull) {
		if (year <= rampStart) return early;
		if (rampFull <= rampStart || year >= rampFull) return late;
		return Mathf.Lerp(early, late, (year - rampStart) / (float)(rampFull - rampStart));
	}

	/// <summary>
	/// Share of the independent distribution trade still operating in a given year. Flat until
	/// the decline starts, then falling to the late-decade survival rate.
	/// </summary>
	internal static float IndependentTradeSurvivalRate(int year, int startYear, int fullYear, float lateRate) {
		if (year <= startYear) return 1f;
		if (fullYear <= startYear) return Mathf.Clamp(lateRate, 0f, 1f);
		float progress = Mathf.Clamp((year - startYear) / (float)(fullYear - startYear), 0f, 1f);
		return Mathf.Lerp(1f, Mathf.Clamp(lateRate, 0f, 1f), progress);
	}

	/// <summary>
	/// Retires part of the independent distribution trade each year of the decline. The houses
	/// carrying the fewest lines go first -- the thinnest business failed first -- and every
	/// label they carried loses that market and the reach the placement granted. Reach earned
	/// through wholesale placement has to be reversible or the decline is toothless: the label
	/// would keep the national footprint of a network that no longer exists.
	/// </summary>
	private void ProcessIndependentTradeDecline(int year) {
		if (independentDistributorsAtStart <= 0) return;
		float survival = IndependentTradeSurvivalRate(year, independentTradeDeclineStartYear,
			independentTradeDeclineFullYear, independentTradeSurvivalLate);
		int target = Mathf.Max(0, Mathf.RoundToInt(independentDistributorsAtStart * survival));
		if (independentDistributors.Count <= target) return;

		var failing = independentDistributors
			.OrderBy(house => house.CurrentClientCount)
			.ThenBy(house => house.distributorId, System.StringComparer.Ordinal)
			.Take(independentDistributors.Count - target)
			.ToList();
		foreach (IndependentDistributor house in failing) {
			float marginalShare = ChartManager.Instance?.GetNationalMarketShareForRegions(new[] { house.regionId }) ?? 0f;
			float reachLost = marginalShare * independentCoverageReachFactor;
			int carried = house.CurrentClientCount;
			foreach (string clientId in house.clientLabelIds.ToList()) {
				AILabel client = GetLabel(clientId);
				if (client == null) continue;
				if (!client.independentDistributionRegions.Remove(house.regionId)) continue;
				client.ownedReach = Mathf.Max(0f, client.ownedReach - reachLost);
			}
			house.clientLabelIds.Clear();
			independentDistributors.Remove(house);
			if (independentDistributorsByRegion.TryGetValue(house.regionId, out var inRegion)) inRegion.Remove(house);
			OnIndependentTradeFailure?.Invoke(new IndependentTradeFailureTelemetry {
				year = year,
				distributorId = house.distributorId,
				distributorName = house.distributorName,
				regionId = house.regionId,
				clientsDropped = carried,
				reachLostPerClient = reachLost,
				housesRemaining = independentDistributors.Count,
				survivalRate = survival
			});
		}
	}

	private void ProcessDistributionDeals(GameDate date) {
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (AILabel client in aiLabels.Where(label => label.activeDeal != null).ToList()) {
			DistributionDeal deal = client.activeDeal;
			AILabel distributor = GetLabel(deal.distributorId);
			if (!client.IsActive) {
				deal.unrecoupedAdvance = 0f;
				EmitDealEvent(client, distributor, deal, DealResolution.ClientClosed, client.DistributionDependency);
				client.activeDeal = null;
				continue;
			}
			if (distributor == null || !distributor.IsActive) {
				EmitDealEvent(client, distributor, deal, DealResolution.DistributorCollapsed, client.DistributionDependency);
				client.activeDeal = null;
				DistributorCollapseCount++;
				continue;
			}
			if (currentWeek >= deal.signedWeek + deal.termWeeks) ResolveDistributionDeal(client, distributor, currentWeek, date.year);
		}

		if (!distributionOfferProcessingEnabled) return;
		foreach (AILabel client in aiLabels.Where(label => label.IsActive && label.activeDeal == null).ToList()) {
			TryGenerateDistributionOffer(client, date.year, currentWeek);
		}
		// Section 33.3: a Major courting a proven label that ALREADY has a distributor. Without
		// this pass the courting route could only reach labels with no distributor at all, and
		// every label worth courting had one -- so the entire late-60s consolidation engine
		// fired 5-16 times per decade and every push-side constant was inert (section 32.3).
		// A major took over a label that was already selling through somebody else; it did not
		// sign up orphans.
		foreach (AILabel client in aiLabels.Where(label => label.IsActive && label.activeDeal != null).ToList()) {
			TryPoachDistributedClient(client, date.year, currentWeek);
		}
	}

	/// <summary>
	/// Tiers that can enter a distribution contract. A label above these runs at a scale that
	/// negotiates its own distribution. This is the single definition both the signing route
	/// and the renewal check use -- keeping them in one place is what stops the two sides
	/// drifting apart again, which is how promoted labels ended up renewing contracts they
	/// could never have signed for eight years running.
	/// </summary>
	internal static bool CanSignDistributionDeal(LabelTier tier) =>
		tier == LabelTier.Small || tier == LabelTier.Boutique || tier == LabelTier.Independent;

	private void TryPoachDistributedClient(AILabel client, int year, int currentWeek) {
		// Poaching is the consolidation wave, and the wave is late. The majors sat out rock and
		// roll and the independents carried it, so an early-60s major taking a proven indie off
		// another distributor is the wrong story in the wrong year (section 29's correction to
		// the target shape). Measured at the base rate it was also poor value: six poaches in
		// 1960 cost thirteen unique labels their first chart entry. Gate it to the courting
		// ramp so it acts where the history and the target arc both put it.
		if (year < consolidationCourtingRampStartYear) return;
		if (client.IsSubsidiary || !CanSignDistributionDeal(client.tier)) return;
		DistributionDeal current = client.activeDeal;
		AILabel incumbent = GetLabel(current?.distributorId);
		// A Major does not poach another Major's client; this is the indie-to-major flow.
		if (incumbent == null || incumbent.tier == LabelTier.Major) return;

		// The same proven-label bar the courting route already used, and the same ramp, so
		// courting still concentrates into the late-60s consolidation years.
		if (!(client.momentumScore > 0.60f || HasRecentTop40Record(client))) return;
		float pushChance = monthlyPushOfferProbability +
			(Mathf.Max(0, year - consolidationCourtingRampStartYear) * annualCourtingRampPerYear);
		// Seed-stable and off the global stream. This roll runs for every distributed client
		// every month, so drawing it from GD.Randf would reorder every downstream sampler and
		// the resulting chart movement could not be attributed to poaching rather than to RNG
		// reordering (section 12) -- the first 52-week measurement showed exactly that, with
		// 34 entries moving on the back of 5 actual poaches.
		if (GetDeterministicIndependentDistributionRoll(client.labelId, "poach", currentWeek) >= pushChance) return;

		AILabel major = SelectDistributor(client, DealOrigin.DistributorCourted, requireMajorDistributor: true);
		// Every Major sat at the client ceiling by the 312-week checkpoint, so poaching starved
		// on capacity and fired three times in two years. A major taking on a proven independent
		// did not wait for a vacancy -- it made room by dropping an imprint that was not selling.
		AILabel dropped = null;
		if (major == null) {
			(major, dropped) = SelectMajorWillingToDropWeakestClient(client);
			if (major == null) return;
		}

		DistributionDeal offer = GenerateDealTerms(client, major, DealOrigin.DistributorCourted, year, currentWeek);
		DistributionOffersGenerated++;
		if (!ShouldAcceptDeal(client, offer)) return;
		DistributionOffersAccepted++;

		RegionalDealEvidence evidence = EvaluateRegionalDealEvidence(
			ChartManager.Instance?.GetAllRecords(), client.labelId, client.strongRegions, regionalBreakoutDealThreshold);
		offer.Cover(evidence.EarningRecordId);
		if (dropped != null) {
			// The dropped imprint keeps what a completed term leaves behind, exactly as an
			// ordinary exit does, and is free to place its line with wholesale houses instead.
			DistributionDeal endedDeal = dropped.activeDeal;
			dropped.ownedReach = Mathf.Min(1f, dropped.ownedReach + (endedDeal.reachGranted * 0.50f));
			EmitDealEvent(dropped, major, endedDeal, DealResolution.Dropped, dropped.DistributionDependency);
			dropped.activeDeal = null;
		}
		EmitDealEvent(client, incumbent, current, DealResolution.Poached, client.DistributionDependency);
		client.activeDeal = offer;
		client.cashReserves += offer.advance;
		major.cashReserves -= offer.advance;
		EmitDealEvent(client, major, offer, DealResolution.Signed, client.DistributionDependency);
	}

	private void TryGenerateDistributionOffer(AILabel client, int year, int currentWeek) {
		// A subsidiary already distributes through the parent's national network and does not
		// sign its own deals.
		if (client.IsSubsidiary) return;
		if (!CanSignDistributionDeal(client.tier)) return;

		RegionalDealEvidence regionalEvidence = EvaluateRegionalDealEvidence(
			ChartManager.Instance?.GetAllRecords(), client.labelId, client.strongRegions, regionalBreakoutDealThreshold);
		// A scalar national-awareness estimate is not a physical network. The old
		// <0.40 test excluded every otherwise-qualified runtime Independent above
		// that arbitrary line even when a distributor still had six new regions to
		// offer. SelectDistributor already enforces the real boundary: at least one
		// distributor region the client does not currently cover.
		bool pullTrigger = IsPullDealTrigger(client, regionalEvidence);
		float pushChance = monthlyPushOfferProbability +
			(Mathf.Max(0, year - consolidationCourtingRampStartYear) * annualCourtingRampPerYear);
		bool pushEvidence = client.momentumScore > 0.60f || HasRecentTop40Record(client);
		bool pushTrigger = pushEvidence && GD.Randf() < pushChance;
		bool pullOffer = pullTrigger && GD.Randf() < monthlyPullOfferProbability;
		var attempt = new DistributionOfferAttemptTelemetry {
			week = currentWeek,
			year = year,
			clientId = client.labelId,
			clientName = client.labelName,
			clientTier = client.tier,
			clientOrigin = client.populationOrigin,
			monthsActive = client.monthsActive,
			ownedReach = client.ownedReach,
			nationalReach = client.nationalReach,
			bestAnyRegionPeak = regionalEvidence.BestAnyRegionPeak,
			bestStrongRegionPeak = regionalEvidence.BestStrongRegionPeak,
			bestPersistentEvidenceQuality = regionalEvidence.BestPersistentEvidenceQuality,
			persistentRegionalEvidence = regionalEvidence.HasPersistentRegionalTraction,
			legacyQualityAndCurrentSalesEvidence = regionalEvidence.PassesLegacyQualityAndCurrentSalesGate,
			legacyNationalReachGate = client.nationalReach < 0.40f,
			pushEvidence = pushEvidence,
			pushChancePassed = pushTrigger,
			pullChancePassed = pullOffer
		};
		if (!pushTrigger && !pullOffer) {
			attempt.outcome = pushEvidence || pullTrigger ? "OfferChanceMiss" : "NoEvidence";
			OnDistributionOfferAttempt?.Invoke(attempt);
			return;
		}

		DealOrigin origin = pushTrigger ? DealOrigin.DistributorCourted : DealOrigin.LabelSought;
		AILabel distributor = SelectDistributor(client, origin);
		if (distributor == null) {
			attempt.outcome = "NoDistributor";
			OnDistributionOfferAttempt?.Invoke(attempt);
			return;
		}
		attempt.distributorId = distributor.labelId;
		DistributionDeal offer = GenerateDealTerms(client, distributor, origin, year, currentWeek);
		DistributionOffersGenerated++;
		if (!ShouldAcceptDeal(client, offer)) {
			attempt.outcome = "Rejected";
			OnDistributionOfferAttempt?.Invoke(attempt);
			return;
		}
		DistributionOffersAccepted++;

		// The deal is struck to carry the record that broke out regionally. Everything
		// the label releases while the contract runs then goes out through the same
		// network; the back catalog stays on the label's own distribution.
		offer.Cover(regionalEvidence.EarningRecordId);
		client.activeDeal = offer;
		client.cashReserves += offer.advance;
		distributor.cashReserves -= offer.advance;
		attempt.outcome = "Signed";
		OnDistributionOfferAttempt?.Invoke(attempt);
		EmitDealEvent(client, distributor, offer, DealResolution.Signed, client.DistributionDependency);
	}

	// This is the pull trigger: a label with a record selling hard in its own region
	// goes looking for a distributor that can carry it national. The first repair
	// replaced a national chart requirement with peakRegionalBreakoutStrength, but
	// then re-closed most of the route in two subtler ways: it demanded omniscient
	// intrinsic quality > .70 and a sale in the current processing week. A proven
	// regional record therefore stopped being evidence whenever its shelf stock
	// happened to be empty at the monthly check.
	//
	// RegionalRecordData.peakBreakoutScore already blends fulfilled and raw sales,
	// velocity, sustained growth, audience, media, genre fit, quality, and unmet
	// demand. Treat LocalTraction in any actual market as persistent observed
	// evidence. A static launch-time "strong region" is useful attribution
	// telemetry, but it is not an exclusive list of places where a record may prove
	// itself organically. The former strong-region-only rule discarded 29 of 96
	// runtime founders with a >=.30 market peak in the measured 1960-65 checkpoint.

	internal static RegionalDealEvidence EvaluateRegionalDealEvidence(
		IEnumerable<RecordRuntimeData> records, string labelId, IEnumerable<string> strongRegionIds, float threshold) {
		var strongRegions = (strongRegionIds ?? System.Array.Empty<string>())
			.Where(regionId => !string.IsNullOrEmpty(regionId))
			.ToHashSet(System.StringComparer.Ordinal);
		float boundedThreshold = Mathf.Clamp(threshold, 0f, 1f);
		float bestAny = 0f;
		float bestStrong = 0f;
		float bestEvidenceQuality = 0f;
		bool persistent = false;
		bool legacy = false;
		// The record whose regional breakout actually earns the deal. The contract is
		// struck to carry this record beyond its home market, so it is the one the
		// deal must cover from the moment it is signed.
		string earningRecordId = null;
		float earningRecordPeak = 0f;
		foreach (RecordRuntimeData record in records ?? System.Array.Empty<RecordRuntimeData>()) {
			if (record?.baseRecord?.labelId != labelId) continue;
			float quality = record.GetQuality();
			bool currentStrongSale = false;
			bool recordHasPersistentEvidence = false;
			float recordBestPeak = 0f;
			foreach (var pair in record.regionalData) {
				float peak = Mathf.Max(0f, pair.Value?.peakBreakoutScore ?? 0f);
				bestAny = Mathf.Max(bestAny, peak);
				recordBestPeak = Mathf.Max(recordBestPeak, peak);
				if (peak >= boundedThreshold) recordHasPersistentEvidence = true;
				if (!strongRegions.Contains(pair.Key)) continue;
				bestStrong = Mathf.Max(bestStrong, peak);
				if ((pair.Value?.unitsSoldThisWeek ?? 0) > 0) currentStrongSale = true;
			}
			if (recordHasPersistentEvidence) {
				persistent = true;
				bestEvidenceQuality = Mathf.Max(bestEvidenceQuality, quality);
			}
			// Tracked for every record, not only qualifying ones, so a distributor-courted
			// deal still binds to the label's strongest current release. When the pull
			// route qualifies, the strongest record is by construction a qualifying one.
			if (earningRecordId == null || recordBestPeak > earningRecordPeak) {
				earningRecordPeak = recordBestPeak;
				earningRecordId = record.baseRecord.recordId;
			}
			if (record.peakRegionalBreakoutStrength >= boundedThreshold &&
				quality > 0.70f && currentStrongSale) legacy = true;
		}
		return new RegionalDealEvidence(bestAny, bestStrong, bestEvidenceQuality, persistent, legacy, earningRecordId);
	}

	internal static bool IsPullDealTrigger(AILabel client, RegionalDealEvidence evidence) =>
		client != null && evidence.HasPersistentRegionalTraction;

	private bool HasRecentTop40Record(AILabel label) => label != null &&
		GetRecentTop40RecordCount(label.labelId, 52) > 0;

	private AILabel SelectDistributor(AILabel client, DealOrigin origin, bool requireMajorDistributor = false) {
		var weighted = new List<(AILabel Label, float Weight)>();
		foreach (AILabel distributor in aiLabels) {
			if (requireMajorDistributor && distributor.tier != LabelTier.Major) continue;
			if (!IsEligibleDistributor(distributor, client, origin)) continue;
			bool genreFit = distributor.preferredGenres?.Intersect(client.preferredGenres ?? System.Array.Empty<Genre>()).Any() ?? false;
			float weight = (distributor.ownedReach * 0.50f) + (distributor.reputation * 0.30f) + (genreFit ? 0.20f : 0f);
			// Path A (section 26): a Major is the historically dominant distributor of independents,
			// and only a Major-distributed deal is absorb-eligible. At the old 6x weight only 18% of
			// deals routed to the 8 Majors (the many MidTier distributors diluted them), so most
			// dependent labels signed with a distributor that could never absorb them and the
			// absorb-eligible pool stayed tiny despite ample unused Major capacity. 12x routes the
			// dependent population toward Majors without touching the high-dependency absorption gate.
			weight *= distributor.tier switch { LabelTier.Major => 12f, LabelTier.MidTier => 1.5f, _ => 1f };
			if (weight > 0f) weighted.Add((distributor, weight));
		}
		if (weighted.Count == 0) return null;

		float total = weighted.Sum(entry => entry.Weight);
		float roll = GD.Randf() * total;
		foreach (var entry in weighted) {
			roll -= entry.Weight;
			if (roll <= 0f) return entry.Label;
		}
		return weighted[^1].Label;
	}

	private bool IsEligibleDistributor(AILabel distributor, AILabel client, DealOrigin origin) {
		if (distributor == null || distributor == client || !distributor.IsActive) return false;
		bool validTier = distributor.tier == LabelTier.Major || distributor.tier == LabelTier.MidTier ||
			(distributor.tier == LabelTier.Independent && distributor.ownedReach >= 0.65f);
		if (!validTier || WouldCreateCircularDeal(client, distributor)) return false;
		// Section 33 slice 6. The ceiling of 24 was set in section 27 purely to widen the pool
		// absorption feeds on, and absorption fires 12-19 times per decade. What it actually built
		// was a Major-distributed roster of 212 that saturated in 1960 and never turned over --
		// median client tenure 8.4 years, 54% of it the 1960-61 cohort -- and that frozen roster,
		// not the masters rate, is what drives the owner-Major overshoot (section 32.2).
		//
		// A 1960s major carried a handful of distributed imprints, not two dozen: ABC around seven,
		// MGM about five, Mercury about four, and most of those were owned subsidiaries rather than
		// independent clients. Ten is the realistic figure and is what the user's own research put
		// the range at.
		//
		// This deliberately ships LAST. Major distribution is the chart-access gate for independents
		// (60% of Major-distributed Independents chart, against 34% on a MidTier distributor and
		// 2.5% undistributed, section 32.4), so cutting it before the independent channel existed
		// would have stranded the displaced labels and cost breadth with nothing to catch them.
		// Slices 1-5 built what catches them.
		int capacity = distributor.tier == LabelTier.Major
			? MajorDistributionCapacityFor(distributor)
			: distributor.tier == LabelTier.MidTier ? 6 : 3;
		if (aiLabels.Count(label => label.activeDeal?.distributorId == distributor.labelId) >= capacity) return false;
		float minimumAdvance = origin == DealOrigin.DistributorCourted ? client.GetMonthlyOverhead() * 6f : 0f;
		if (distributor.cashReserves - minimumAdvance <= distributor.GetMonthlyOverhead() * 3f) return false;

		var offeredRegions = distributor.distributionRegions ?? System.Array.Empty<string>();
		return offeredRegions.Any(region => !client.HasDistributionInRegion(region));
	}

	private static bool WouldCreateCircularDeal(AILabel client, AILabel distributor) {
		var visited = new HashSet<string>(System.StringComparer.Ordinal);
		AILabel current = distributor;
		while (current?.activeDeal != null && visited.Add(current.labelId)) {
			if (current.activeDeal.distributorId == client.labelId) return true;
			current = Instance?.GetLabel(current.activeDeal.distributorId);
		}
		return false;
	}

	// Section 29: the Major master-ownership floor ramps linearly from the early rate to the late
	// rate between the ramp-start and ramp-full years, modeling the late-60s P&D consolidation. A
	// flat rate produced a high, flat owner-Major line; ramping produces the desired dip-then-rise.
	private float GetMajorMastersOwnershipRate(int year) {
		if (year <= majorMastersRampStartYear) return majorDistributorMastersOwnershipRateEarly;
		if (year >= majorMastersRampFullYear) return majorDistributorMastersOwnershipRateLate;
		float t = (float)(year - majorMastersRampStartYear) / (majorMastersRampFullYear - majorMastersRampStartYear);
		return Mathf.Lerp(majorDistributorMastersOwnershipRateEarly, majorDistributorMastersOwnershipRateLate, t);
	}

	private DistributionDeal GenerateDealTerms(AILabel client, AILabel distributor, DealOrigin origin, int year, int currentWeek) {
		bool push = origin == DealOrigin.DistributorCourted;
		string[] availableRegions = GetGrantedDistributionRegions(client, distributor.distributionRegions);
		float advance = push
			? client.GetMonthlyOverhead() * (float)GD.RandRange(6f, 12f)
			: (GD.Randf() < 0.35f ? 0f : client.GetMonthlyOverhead() * (float)GD.RandRange(0.5f, 2f));
		advance = Mathf.Min(advance, Mathf.Max(0f, distributor.cashReserves - (distributor.GetMonthlyOverhead() * 3f)));
		// Borrowed reach is worth the market the distributor's owned network actually
		// reaches. It was formerly an independent random draw, so a distributor owning
		// three regions could grant more national reach than one owning all seven.
		// Scaling the negotiated range by that coverage leaves a genuinely national
		// distributor at the authored terms and correctly discounts a partial one.
		float distributorCoverage = ChartManager.Instance?.GetNationalMarketShareForRegions(distributor.distributionRegions) ?? 1f;
		float negotiatedReach = push ? (float)GD.RandRange(0.50f, 0.80f) : (float)GD.RandRange(0.30f, 0.50f);
		// Section 28: majors distributing independents in the 1960s routinely owned the masters
		// (the "P&D" deal), which is what folds those records into the major's corporate/distributor
		// chart share (section 27.1). A Major distributor therefore takes masters far more often than
		// the old flat 0.15 pull rate; the push rate already reflects an aggressive courting deal.
		float mastersRate = push ? pushMastersOwnershipRate : 0.15f;
		if (distributor.tier == LabelTier.Major) mastersRate = Mathf.Max(mastersRate, GetMajorMastersOwnershipRate(year));
		return new DistributionDeal {
			distributorId = distributor.labelId,
			reachGranted = negotiatedReach * distributorCoverage,
			// The deal exists to carry a proven regional record beyond its home market.
			// The former pull path intersected the distributor's network with the client's
			// strong regions, so it granted only the market the client already served and
			// never supplied the national bridge the contract represented.
			grantedRegions = availableRegions,
			marginSkim = push ? (float)GD.RandRange(pushMarginSkimMin, pushMarginSkimMax) : (float)GD.RandRange(pullMarginSkimMin, pullMarginSkimMax),
			ownsMasters = GD.Randf() < mastersRate,
			advance = advance,
			unrecoupedAdvance = advance,
			signedWeek = currentWeek,
			termWeeks = push ? (int)GD.RandRange(78, year >= 1967 ? 104 : 156) : (int)GD.RandRange(52, 104),
			origin = origin
		};
	}

	internal static string[] GetGrantedDistributionRegions(AILabel client, IEnumerable<string> distributorRegions) =>
		(distributorRegions ?? System.Array.Empty<string>())
			.Where(region => client != null && !client.HasDistributionInRegion(region))
			.Distinct(System.StringComparer.Ordinal)
			.ToArray();

	private static bool ShouldAcceptDeal(AILabel client, DistributionDeal offer) {
		float currentReach = client.distributionStrength;
		float projectedReach = Mathf.Clamp(client.ownedReach + offer.reachGranted, 0f, 1f);
		if (projectedReach <= currentReach + 0.05f) return false;

		bool cashPressured = client.cashReserves < client.GetMonthlyOverhead() * 6f || client.consecutiveLossMonths >= 3;
		bool momentumHungry = client.momentumScore > 0.55f || client.status == LabelStatus.Rising;
		bool courted = offer.origin == DealOrigin.DistributorCourted;
		float acceptance = 0.20f + (cashPressured ? 0.35f : 0f) + (momentumHungry ? 0.30f : 0f);
		if (offer.origin == DealOrigin.LabelSought) acceptance += 0.20f;
		// A Major actively courting a proven independent -- with a 6-12 month advance -- is the
		// historical way most indies were pulled into a major's fold late-decade (path A, section
		// 26). It was getting rejected ~83% of the time: the "successful indie stays independent"
		// penalty below fired on exactly these ownedReach>=0.45 courting targets, making
		// independence the rule when history made it the Motown exception. A courting offer now
		// carries a strong acceptance bonus AND is exempt from that penalty, so most courted
		// dependent labels sign, stay dependent, and are absorbable when the window opens.
		if (courted) acceptance += 0.35f;
		if (!courted && client.tier == LabelTier.Independent && client.ownedReach >= 0.45f && !cashPressured) acceptance -= 0.35f;
		return GD.Randf() < Mathf.Clamp(acceptance, 0.05f, 0.95f);
	}

	private void ResolveDistributionDeal(AILabel client, AILabel distributor, int currentWeek, int year) {
		DistributionDeal deal = client.activeDeal;
		float dependency = client.DistributionDependency;
		// A completed term leaves a client with national relationships and market knowledge even
		// when the contract renews. Before this coupling, nationalReach was a generation-only
		// attribute, so labels could build or borrow a national distribution network for years
		// while remaining permanently regional in the propagation path that actually feeds the
		// chart. The retained fraction is bounded below seeded Major reach and consumes no RNG.
		client.nationalReach = CalculateNationalReachAfterCompletedDeal(client.nationalReach,
			deal.reachGranted, completedDealNationalReachRetention, CompletedDealNationalReachCeiling);
		// Section 32.2: TryGenerateDistributionOffer bars a MidTier from SIGNING a contract,
		// and nothing here barred one from RENEWING it, so a client promoted while under
		// contract renewed indefinitely -- 29-44 of them per decade run, at a median tenure of
		// 8.4 years, and they were the single largest block of the owner-Major surplus. A label
		// that has grown past the tiers that can sign now leaves the contract at term instead,
		// keeping half the reach it borrowed, exactly as a low-dependency exit does.
		if (!CanSignDistributionDeal(client.tier)) {
			client.ownedReach = Mathf.Min(1f, client.ownedReach + (deal.reachGranted * 0.50f));
			EmitDealEvent(client, distributor, deal, DealResolution.Graduated, dependency);
			client.activeDeal = null;
			return;
		}
		if (dependency < dealDependencyLow) {
			// Low dependency: the client has built enough of its own reach to leverage the
			// deal and stay independent (early Motown). It exits and keeps part of the reach.
			client.ownedReach = Mathf.Min(1f, client.ownedReach + (deal.reachGranted * 0.50f));
			EmitDealEvent(client, distributor, deal, DealResolution.Exit, dependency);
			client.activeDeal = null;
		} else if (dependency < dealDependencyHigh) {
			RerollMastersOnRenewal(client, distributor, deal, year, currentWeek);
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.signedWeek = currentWeek;
		} else if (ShouldConsolidate(client, distributor, year) && AbsorbLabel(client, distributor, dependency)) {
			// High dependency: the client leaned on the major's network rather than building
			// its own (Stax -> Atlantic). Late-decade, a charted such independent is absorbed
			// into the major family, reattributing its chart records to the owner while its
			// release imprint still counts for breadth. Absorption stays gated to high
			// dependency by design -- it is never a blanket effect across all deals.
		} else {
			// A high-dependency client that is renewed rather than absorbed (pre-1966, or an
			// uncharted/non-Major-distributed one in-window) keeps leaning on the network. The
			// reach erosion here was steep enough (0.85x/cycle) that high-dependency labels
			// eroded down into the mid band before the 1966 consolidation window opened -- the
			// high-dep expiry pool collapsed from ~20/yr in 1961-63 to ~4-6/yr by 1966-69, which
			// starved absorption. Path A (section 26) softens it to 0.93x so a genuinely dependent
			// label stays dependent and is still absorbable when the window opens.
			RerollMastersOnRenewal(client, distributor, deal, year, currentWeek);
			EmitDealEvent(client, distributor, deal, DealResolution.Renew, dependency);
			deal.marginSkim = Mathf.Min(0.50f, deal.marginSkim + 0.05f);
			deal.reachGranted = Mathf.Max(0.10f, deal.reachGranted * 0.93f);
			deal.signedWeek = currentWeek;
		}
	}

	// Deterministic consolidation gate, split from the random roll so it can be probed
	// without a simulation. An absorption is eligible only inside the historical window,
	// under the decade cap, from a Major (or, when enabled, a national MidTier) acquirer,
	// against an independent-family client that has already charted. The random roll is
	// applied separately in ShouldConsolidate so a false gate consumes no RNG -- which is
	// what keeps pre-window years byte-identical to the pre-lever configuration.
	internal static bool IsConsolidationEligible(int year, int consolidationStartYear,
		LabelTier distributorTier, bool distributorNational, LabelTier clientTier, bool clientHasCharted,
		bool requireCharted, bool allowNationalMidTier, int absorptionsSoFar, int cap) {
		if (year < consolidationStartYear) return false;
		if (absorptionsSoFar >= cap) return false;
		bool eligibleDistributor = distributorTier == LabelTier.Major ||
			(allowNationalMidTier && distributorTier == LabelTier.MidTier && distributorNational);
		if (!eligibleDistributor) return false;
		// Section 28: the historically dominant late-60s consolidation was majors absorbing
		// high-volume MidTier labels (WB->Atlantic 1967, MCA->Kapp/Uni), not only tiny indies.
		// A MidTier client is therefore absorbable; only a Major client (a peer, not a target)
		// is excluded. Absorbing individually low-volume Small/Independent labels alone cannot
		// bridge the chart-share gap -- a dependent MidTier hitmaker carries real chart volume.
		bool absorbableClient = clientTier != LabelTier.Major;
		if (!absorbableClient) return false;
		return !requireCharted || clientHasCharted;
	}

	private bool ShouldConsolidate(AILabel client, AILabel distributor, int year) {
		if (forcedConsolidationClients.Contains(client.labelId)) return true;
		bool distributorNational = consolidationAllowNationalMidTier &&
			(ChartManager.Instance?.GetNationalMarketShareForRegions(distributor.distributionRegions) ?? 0f) >= 0.80f;
		if (!IsConsolidationEligible(year, consolidationStartYear, distributor.tier, distributorNational,
				client.tier, chartedLabelIds.Contains(client.labelId), consolidationRequireCharted,
				consolidationAllowNationalMidTier, consolidationAbsorptionsThisDecade,
				maxDecadeConsolidationAbsorptions)) {
			return false;
		}
		return GD.Randf() < consolidationAbsorbChance;
	}

	// Absorption is the subsidiary model (section 24). An absorbed independent is NOT shut
	// down: it keeps its own roster, records, release imprint and album projects, and keeps
	// charting. Only ownership rolls up to the Major -- via ownerLabelId here and the
	// acquiredBy chain the audit builds from the Absorb event -- which is what lets each
	// subsidiary keep producing as Major-owned so consolidation actually raises owner-Major
	// chart share instead of bottlenecking the absorbed roster onto a capacity-bound Major.
	private bool AbsorbLabel(AILabel client, AILabel distributor, float dependency) {
		if (client == null || distributor == null || client == distributor || !client.IsActive || !distributor.IsActive) return false;
		if (client.IsSubsidiary) return false;
		DistributionDeal deal = client.activeDeal;
		if (deal == null || deal.distributorId != distributor.labelId || WouldCreateCircularDeal(client, distributor)) return false;

		consolidationAbsorptionsThisDecade++;
		EmitDealEvent(client, distributor, deal, DealResolution.Absorb, dependency);
		ApplySubsidiaryAbsorption(client, distributor);
		if (LabelLifecycleManager.Instance != null) LabelLifecycleManager.Instance.MarkLabelSubsidiary(client, distributor);
		return true;
	}

	// Pure ownership/reach transfer for a subsidiary absorption, split out so it can be probed
	// without a running simulation. Borrowed reach came from the now-terminated deal, so it is
	// folded into ownedReach permanently (the subsidiary is now part of the parent's national
	// network) and the parent's distribution regions are unioned in so per-region coverage
	// persists after the deal is nulled. The roster, records, imprint and album projects are
	// deliberately left untouched -- the subsidiary keeps operating and charting.
	internal static void ApplySubsidiaryAbsorption(AILabel client, AILabel distributor) {
		float borrowed = client.borrowedReach;
		client.ownedReach = Mathf.Clamp(client.ownedReach + borrowed, 0f, 1f);
		client.distributionRegions = (client.distributionRegions ?? System.Array.Empty<string>())
			.Union(distributor.distributionRegions ?? System.Array.Empty<string>(), System.StringComparer.Ordinal)
			.ToArray();
		client.ownerLabelId = distributor.labelId;
		client.activeDeal = null;
	}

	private void EmitDealEvent(AILabel client, AILabel distributor, DistributionDeal deal, DealResolution resolution, float dependency) {
		OnDistributionDealEvent?.Invoke(new DistributionDealTelemetry {
			resolution = resolution,
			origin = deal.origin,
			distributorId = deal.distributorId,
			distributorName = distributor?.labelName ?? deal.distributorId,
			clientId = client?.labelId,
			clientName = client?.labelName,
			reachGranted = deal.reachGranted,
			marginSkim = deal.marginSkim,
			ownsMasters = deal.ownsMasters,
			advance = deal.advance,
			signedWeek = deal.signedWeek,
			termWeeks = deal.termWeeks,
			dependency = dependency
		});
	}
	
	private void UpdateLabelStatus(AILabel label, LabelFinancialHistory financials, float netIncome) {
		if (netIncome < 0) financials.consecutiveLossMonths++;
		else financials.consecutiveLossMonths = Mathf.Max(0, financials.consecutiveLossMonths - 1);
		label.consecutiveLossMonths = financials.consecutiveLossMonths;
		
		if (label.cashReserves < bankruptcyThreshold) {
			if (enableBankruptcy && financials.consecutiveLossMonths >= 6) {
				label.status = LabelStatus.Bankrupt;
				GD.Print($"💀 {label.labelName} has gone bankrupt!");
				return;
			}
			label.status = LabelStatus.Dying;
		} else if (label.cashReserves < label.GetMonthlyOverhead() * 3) {
			label.status = LabelStatus.Struggling;
		} else if (LabelLifecycleManager.IsRuntimeFounderInEmergenceRunway(label)) {
			label.status = netIncome > label.GetMonthlyOverhead() * 2f ? LabelStatus.Rising : LabelStatus.Stable;
		} else if (financials.consecutiveLossMonths >= 3) {
			label.status = LabelStatus.Dying;
		} else if (netIncome > label.GetMonthlyOverhead() * 2) {
			label.status = LabelStatus.Rising;
		} else if (netIncome > 0) {
			label.status = LabelStatus.Stable;
		}
	}
	
	public AILabel GetLabel(string labelId) => aiLabels?.FirstOrDefault(l => l.labelId == labelId);
	public IReadOnlyList<AILabel> GetAllLabels() => aiLabels ?? (IReadOnlyList<AILabel>)System.Array.Empty<AILabel>();

	public void RegisterLabel(AILabel label) {
		if (label == null || string.IsNullOrEmpty(label.labelId)) return;
		if (aiLabels != null && !aiLabels.Contains(label)) aiLabels.Add(label);
		if (!labelActiveRecords.ContainsKey(label.labelId)) labelActiveRecords[label.labelId] = new List<string>();
		if (!retiredLabelRecordHistory.ContainsKey(label.labelId))
			retiredLabelRecordHistory[label.labelId] = new List<LabelRecordHistoryEntry>();
		if (!labelFinancials.ContainsKey(label.labelId)) labelFinancials[label.labelId] = new LabelFinancialHistory();
	}

	public void RecordExpense(AILabel label, float amount) {
		if (label == null || amount <= 0f) return;
		label.cashReserves -= amount;
		label.monthlyExpenses += amount;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthExpenses += amount;
		}
	}
	
	public List<AILabel> GetActiveLabelsByStatus(LabelStatus status) => aiLabels?.Where(l => l.status == status).ToList() ?? new List<AILabel>();
	
	public List<AILabel> GetOperatingLabels() => aiLabels?.Where(l => l.IsActive).ToList() ?? new List<AILabel>();
	
	public int GetLabelActiveRecordCount(string labelId) => labelActiveRecords.TryGetValue(labelId, out var records) ? records.Count : 0;

	public int GetRecentChartingRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks &&
			record.weeksOnChart > 0);
		return active + CountRecentRetiredRecordEvidence(
			retiredLabelRecordHistory.GetValueOrDefault(labelId), ChartManager.Instance.GetCurrentChartWeek(),
			maxAgeWeeks, requireCharted: true, requireTop40: false);
	}

	public int GetRecentReleasedRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks);
		return active + CountRecentRetiredRecordEvidence(
			retiredLabelRecordHistory.GetValueOrDefault(labelId), ChartManager.Instance.GetCurrentChartWeek(),
			maxAgeWeeks, requireCharted: false, requireTop40: false);
	}

	private int GetRecentTop40RecordCount(string labelId, int maxAgeWeeks) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetAllRecords().Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks &&
			record.peakPosition > 0 && record.peakPosition <= 40);
		return active + CountRecentRetiredRecordEvidence(
			retiredLabelRecordHistory.GetValueOrDefault(labelId), ChartManager.Instance.GetCurrentChartWeek(),
			maxAgeWeeks, requireCharted: false, requireTop40: true);
	}

	internal static int CountRecentRetiredRecordEvidence(IEnumerable<LabelRecordHistoryEntry> history,
		int currentWeek, int maxAgeWeeks, bool requireCharted, bool requireTop40) {
		int boundedAge = Mathf.Max(0, maxAgeWeeks);
		return (history ?? System.Array.Empty<LabelRecordHistoryEntry>()).Count(entry =>
			currentWeek - entry.ReleaseWeek >= 0 &&
			currentWeek - entry.ReleaseWeek <= boundedAge &&
			(!requireCharted || entry.Charted) &&
			(!requireTop40 || entry.Top40));
	}
	
	private void PrintMonthlyReport(GameDate date) {
		GD.Print($"=== INDUSTRY REPORT - {date.month}/{date.year} ===");
		var byStatus = aiLabels.GroupBy(l => l.status);
		foreach (var group in byStatus.OrderBy(g => (int)g.Key)) GD.Print($"{group.Key}: {group.Count()} labels");
		
		var topLabels = aiLabels.Where(l => l.status != LabelStatus.Bankrupt).OrderByDescending(l => l.cashReserves).Take(5);
		GD.Print("Top 5 Labels by Cash:");
		foreach (var label in topLabels) {
			int chartingCount = labelActiveRecords.TryGetValue(label.labelId, out var recs) ? recs.Count : 0;
			GD.Print($"  {label.labelName}: ${label.cashReserves:N0} | Roster: {label.roster.Count} | Charting: {chartingCount}");
		}
	}
	
	public void DebugPrintReleaseStats() {
		int totalActive = labelActiveRecords.Values.Sum(l => l.Count);
		GD.Print($"=== RELEASE STATS ===\nTotal Active Records: {totalActive}");
		var topByReleases = labelActiveRecords.OrderByDescending(kvp => kvp.Value.Count).Take(10);
		foreach (var (labelId, records) in topByReleases) {
			var label = GetLabel(labelId);
			GD.Print($"  {label?.labelName ?? labelId}: {records.Count} active");
		}
	}
	
	public void DebugForceRelease() {
		if (aiLabels == null || aiLabels.Count == 0) return;
		var label = aiLabels.Where(l => l.status != LabelStatus.Bankrupt && l.roster.Count > 0).OrderBy(l => GD.Randf()).FirstOrDefault();
		if (label != null) {
			var date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			TryReleaseRecord(label, date);
		}
	}
}

public class LabelFinancialHistory {
	public float lastMonthRevenue;
	public float lastMonthExpenses;
	public int consecutiveLossMonths;
	public int totalReleasesThisYear;
}

public sealed class RevenueTelemetry {
	public float gross;
	public float cogs;
	public float distributionSkim;
	public float artistRoyalty;
	public float labelNet;
	public float distributionIncome;
	public float MarketNet => labelNet + distributionIncome;
}

public sealed class ReleaseStrategyTelemetry {
	public string recordId;
	public string labelId;
	public LabelTier tier;
	public string artistId;
	public Genre genre;
	/// <summary>Unmapped secondary identity at the format decision seam; retained for offline cohort migration.</summary>
	public Genre secondaryGenre;
	public CareerState careerState;
	public string careerBand;
	public float qualityEstimate;
	public string qualityQuartile;
	public float reachFactor;
	public float genreSinglesMarketFactor;
	public float priorSingleNet;
	public float priorAlbumNet;
	public float projectedSingleNet;
	public float projectedAlbumNet;
	public float confidenceSingle;
	public float confidenceAlbum;
	public float rawConfidenceSingle;
	public float rawConfidenceAlbum;
	public bool singleMemoryCapApplied;
	public bool albumMemoryCapApplied;
	public ReleaseFormat chosenFormat;
	// Preserves the A2 diagnostic column's meaning; no longer participates in the prior.
	public bool assumedCompilationCost;
	public float compCostWeight;
	public float expectedFormatMultiplier;
	public int releasedSingleIdsExamined;
	public int resolvedSingles;
	public int chartedSingles;
	public float hitScore;
	public float unweightedHitUnits;
	public float weightedHitUnits;
	public float affinityUnits;
	public float totalExpectedAlbumUnits;
	public AlbumFormat? actualAlbumFormat;
	public string projectId;
	public ReleaseStrategy strategy;
	public float projectedOrphanSingleNet;
	public float projectedAlbumStandaloneNet;
	public float projectedAlbumWithPromoNet;
	public string promoSingleId;
	public bool albumStrategyEvaluated;
	public float singleProductionCost;
	public float singleNetMarginPerUnit;
	public float expectedSingleUnits;
	public float albumDemandFactor;
	public float substitutionK;
	public float substitutionCap;
	public float substitutionPropensity;
	public float expectedOverlapFraction;
	public float divertedUnits;
	public float albumMarginPerUnit;
	public float cannibalizationLoss;
	public float cannibalizationCharged;
	public float expectedPromoLift;
	public float expectedPromoSingleNet;
	public float promoAdvantage;
	public float singlePreTiltContribution;
	public float singleFormatTilt;
	public float albumAffinity;
	public float albumOpportunity;
	public float albumFormatTilt;
	public float albumPreTiltContribution;
	public float albumProductionCost;
	public float singleMemoryEma;
	public float albumMemoryEma;
		public float singleMemoryBlend;
		public float albumMemoryBlend;
		public bool labelFormatMemoryBypassed;
		public float singleNoiseMultiplier;
	public float albumNoiseMultiplier;
	public float albumChoiceProbability;
	public float formatChoiceRoll;
	public bool albumCapacityReroute;
}

public sealed class CalibrationDecisionTelemetry {
	public string recordId;
	public string labelId;
	public string artistId;
	public Genre genre;
	public CareerState careerState;
	public float qualityEstimate;
	public float reachFactor;
	public float genreSinglesMarketFactor;
	public float singleProductionCost;
	public ReleaseFormat chosenFormat;
}

public enum SupplySelectionMode { Retained, AnnualFloor, WeightedTransition }

public sealed class SupplySelectionTelemetry {
	public string labelId;
	public string artistId;
	public Genre artistIdentity;
	public Genre chosenProjectGenre;
	public bool artistIdentityAvailableForNewSupply;
	public bool annualFloorRequested;
	public bool annualFloorReroutedToNormalCandidates;
	public SupplySelectionMode selectionMode;
}

public sealed class ReleaseOutcomeTelemetry {
	public string labelId;
	public string recordId;
	public ReleaseFormat format;
	public Genre genre;
	public bool memoryEligible;
	public float lifetimeLabelNet;
	public float sunkProductionCost;
	public float realizedNet;
}

public sealed class FormatMemoryRevisionTelemetry {
	public string releaseId, labelId, revisionKind;
	public string projectId;
	public ReleaseFormat format;
	public ProjectRecordRole releaseLane;
	public RevenueEstimatorLane estimatorLane;
	public Genre genre;
	public int releaseAge, revisionOrdinal;
	public float releaseTimeExpectedNet, ageMatchedExpectedNet, realizedNetToDate, estimatedOutcomeNet, opportunityScale;
	public float normalizedResidual, maturityWeight, recencyWeight;
	public bool replacedPriorRevision, finalized;
}
