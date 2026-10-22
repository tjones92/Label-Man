using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CompetitorManager : Node {
	public static CompetitorManager Instance { get; private set; }
	public const float DealReinvestRate = 0.02f;
	public const float DealReinvestCost = 5000000f;
	// Publishing (Scouting Mechanic Phase 4): the composition-royalty slice of a record's gross -
	// historically the "goldmine", mechanical + performance royalties on the song. Only reallocated
	// when the artist kept publishing (a Visionary deal), so it is inert when managers are off. A
	// calibration guess flagged for re-derivation against measured label profitability.
	public const float PublishingShareOfGross = 0.11f;
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
	// Under --regional-hit-deal-gate the P&D pull gate stops reading this and uses RegionalHitDealGate's
	// current-hit bar instead; wholesale placement, trade press and media still read it.
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
	// Never assigned by any scene; kept as the hook the historical-seed paths read. Not exported: Record is a plain C# class.
	private Record[] historicalRecords = null;
	
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
	public readonly struct LabelRecordHistoryEntry {
		public readonly int ReleaseWeek;
		public readonly bool Charted;
		public readonly bool Top40;
		// [JsonConstructor] so the full-world save deserializes via this ctor -- a readonly struct otherwise
		// round-trips through the implicit parameterless ctor and leaves the readonly fields at default.
		[System.Text.Json.Serialization.JsonConstructor]
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
	// Phase 3b publishing goldmine: id->label map rebuilt each settlement week (routing-live only).
	private Dictionary<string, AILabel> settlementLabelLookup;
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
	public event System.Action<DistributionAdvanceRecoupmentTelemetry> OnDistributionAdvanceRecoupment;
	public event System.Action<RecordCampaignTelemetry> OnRecordCampaign;
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
		// Publishing & Cover-Song layer (Phase 0): stand up the composition catalog BEFORE any
		// records are populated so every release can attach a song. Uses its own seed-salted RNG
		// stream (never GD), so this is inert to the simulated economy.
		CompositionCatalogService.Initialize(1960, aiLabels, SimulationSeedBootstrap.RequestedSeed ?? 0UL);
		PopulateInitialRecords();
		if (LabelCorporateModel.Ledger) foreach (var label in aiLabels) LabelCompanyAccounts.Open(label);
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
		// The player places their own lines, one market at a time, from the desk.
		if (label.isPlayerOwned) return;
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
		// WholesaleSpreadGate: a hop to a bordering market needs a regional hit in the source market, so the hop
		// sources are separate from the proven markets and can be the only evidence a label has this month.
		HashSet<string> hopSources = WholesaleSpreadGate.Enabled ? GetRecentHitRegions(label) : proven;
		if (proven.Count == 0 && hopSources.Count == 0) return;

		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		string target = SelectIndependentDistributionTarget(label, proven, currentWeek, hopSources);
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
		bool sustained = WholesaleSpreadGate.Enabled;
		foreach (RecordRuntimeData record in ChartManager.Instance?.GetRecordsForLabel(label.labelId) ?? (IEnumerable<RecordRuntimeData>)System.Array.Empty<RecordRuntimeData>()) {
			foreach (var pair in record.regionalData) {
				// WholesaleSpreadGate: proof is a record holding LocalTraction there now, not a peak it once touched.
				if (sustained ? WholesaleSpreadGate.IsSustained(pair.Value)
					: (pair.Value?.peakBreakoutScore ?? 0f) >= regionalBreakoutDealThreshold) proven.Add(pair.Key);
			}
		}
		return proven;
	}

	/// <summary>Markets where one of this label's records is a regional hit now (RegionalHitDealGate's bar and window).</summary>
	private static HashSet<string> GetRecentHitRegions(AILabel label) {
		var hits = new HashSet<string>(System.StringComparer.Ordinal);
		foreach (RecordRuntimeData record in ChartManager.Instance?.GetRecordsForLabel(label.labelId) ?? (IEnumerable<RecordRuntimeData>)System.Array.Empty<RecordRuntimeData>()) {
			foreach (var pair in record.regionalData)
				if (RegionalHitDealGate.IsRecent(pair.Value, record.weeksSinceRelease)) hits.Add(pair.Key);
		}
		return hits;
	}

	// Proven markets first, then the markets bordering them. A house that took the line had
	// standing arrangements with its peers one region over, which is how a regional hit
	// spread before it was a national one. hopSources is the proven set itself unless
	// WholesaleSpreadGate is on, when only a market with a current regional hit can hop.
	private string SelectIndependentDistributionTarget(AILabel label, HashSet<string> proven, int currentWeek, HashSet<string> hopSources) {
		// Only markets that can actually be placed in are candidates. Counting a market with
		// no independent trade -- the Rockies -- would have consumed the month's opportunity
		// and then failed, which left all 18 Rockies-home labels unable to place a single
		// line. A label in a market without wholesalers has to reach the ones next door.
		var candidates = new List<string>();
		foreach (string regionId in proven)
			if (CanPlaceLineIn(label, regionId)) candidates.Add(regionId);
		if (candidates.Count == 0) {
			foreach (string regionId in hopSources)
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
		// Pure read over every Major's held clients; count all labels' recent charting records in one pass.
		System.Func<string, int> chartingCount = RecentChartingRecordCounter();
		int courtedCharting = chartingCount(client.labelId);
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
				int charting = chartingCount(held.labelId);
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
				var artist = label.roster[(int)GameRandom.RandRange(0, label.roster.Count - 1)];
				// PREWARM SEEDING (2026-08, WIP task B): the opening catalog was 100% singles, so the album
				// channel was empty at week 1 and 1960 read low LP unit share. Seed a genre-realistic share
				// as the pre-existing 1960 LP catalog (jazz, classical, mood/MOR, Broadway); the album
				// affinity skew makes adult genres seed albums often and teen/rock rarely, matching the era.
				Genre seedGenre = GenreCatalog.MapLegacy(artist.primaryGenre, year);
				var format = GameRandom.Randf() < MarketRegion.GetAlbumSeedAffinity(seedGenre)
					? ReleaseFormat.Album : ReleaseFormat.Single;
				var record = GenerateRecordFromArtist(label, artist, year, format);
				int weeksAgo = (int)GameRandom.RandRange(1, 20);
				if (record == null) continue;
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
		
		if (quality > 0.7f && GameRandom.Randf() < 0.4f) {
			runtimeData.weeksOnChart = (int)GameRandom.RandRange(2, weeksOld);
			runtimeData.peakPosition = (int)GameRandom.RandRange(10, 60);
		} else if (quality > 0.5f && GameRandom.Randf() < 0.3f) {
			runtimeData.weeksOnChart = (int)GameRandom.RandRange(1, weeksOld / 2);
			runtimeData.peakPosition = (int)GameRandom.RandRange(40, 90);
		}
		
		var regions = ChartManager.Instance.GetAllRegions();
		foreach (var region in regions) {
			if (!runtimeData.regionalData.ContainsKey(region.regionId)) {
				runtimeData.regionalData[region.regionId] = new RegionalRecordData(region.regionId);
			}
			var regionalData = runtimeData.regionalData[region.regionId];
			bool isStrongRegion = label.strongRegions?.Contains(region.regionId) ?? false;
			float regionMod = isStrongRegion ? 1.4f : 1f;
			
			regionalData.awareness = runtimeData.awareness * regionMod * (float)GameRandom.RandRange(0.7, 1.1);
			regionalData.radioPlay = runtimeData.radioHeat * regionMod * (float)GameRandom.RandRange(0.6, 1.0);
			regionalData.sentiment = 0.5f + (quality * 0.3f) + (float)GameRandom.RandRange(-0.1, 0.15);
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
			regionalData.unitsInStores = Mathf.RoundToInt(prewarmStock * (float)GameRandom.RandRange(0.7, 1.1));
			// A record already this old has sold part of its shelf through.
			regionalData.unitsSoldTotal = Mathf.RoundToInt(shelf * (1f - ageFactor) * (float)GameRandom.RandRange(0.6, 1.2));
		}
	}
	
	private void OnWeekEnded(GameDate date) {
		var profileStart = SimulationPerformanceProfiler.Begin();
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
		if (RecordCampaignSpend.Enabled) ProcessRecordCampaigns();
		AiMasterDeals.ProcessWeek(this, date);
		AiBackers.ProcessWeek(this, date);
	}

	/// <summary>
	/// Audit B5: AI labels work their breaking singles and drop the dead ones. Draws no random numbers.
	/// Labels and records are visited in list order, so a resumed world makes the same calls.
	/// </summary>
	private void ProcessRecordCampaigns() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (AILabel label in aiLabels) {
			if (!label.IsActive || label.isPlayerOwned || !labelActiveRecords.TryGetValue(label.labelId, out List<string> recordIds)) continue;
			foreach (string recordId in recordIds) {
				RecordRuntimeData record = ChartManager.Instance?.GetRecordRuntimeData(recordId);
				if (record?.campaignPushFactor is not float factor || record.campaignTranches is not int tranches || tranches < 0) continue;
				if (RecordCampaignSpend.FollowUps && RecordCampaignSpend.IsBreaking(record) && tranches < RecordCampaignSpend.MaxTranches) {
					SimulatedArtist artist = ArtistManager.Instance?.GetArtist(record.baseRecord.artistId);
					float spend = (artist != null ? label.GetMarketingBudget(artist) : label.GetStandardMarketingBudget()) * RecordCampaignSpend.TrancheShare;
					float reserve = label.GetMonthlyOverhead() * RecordCampaignSpend.ReserveMonths
						+ (float)LabelCorporateModel.TaxAndServiceReserve(label);
					if (spend <= 0f || label.cashReserves - spend < reserve) continue;
					LabelCompanyAccounts.ChangeCash(label, -spend, LabelCashKind.Campaign);
					label.monthlyExpenses += spend;
					if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) financials.lastMonthExpenses += spend;
					if (record.sunkMarketingCost.HasValue) record.sunkMarketingCost += spend;
					record.campaignPushFactor = Mathf.Min(RecordCampaignSpend.MaxFactor, factor * RecordCampaignSpend.TrancheLift);
					record.campaignTranches = tranches + 1;
					EmitRecordCampaign("FollowUp", week, label, record, spend, factor);
				} else if (RecordCampaignSpend.Cuts && RecordCampaignSpend.IsDead(record)) {
					record.campaignPushFactor = factor * RecordCampaignSpend.CutFactor;
					record.campaignTranches = -1;
					EmitRecordCampaign("Cut", week, label, record, 0f, factor);
				}
			}
		}
	}

	private void EmitRecordCampaign(string kind, int week, AILabel label, RecordRuntimeData record, float spend, float factorBefore) =>
		OnRecordCampaign?.Invoke(new RecordCampaignTelemetry {
			Week = week, Kind = kind, RecordId = record.baseRecord.recordId, LabelId = label.labelId, ArtistId = record.baseRecord.artistId,
			WeeksSinceRelease = record.weeksSinceRelease, Position = record.currentPosition, Spend = spend,
			FactorBefore = factorBefore, FactorAfter = record.campaignPushFactor ?? 1f, CashAfter = label.cashReserves
		});

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
		// Publishing goldmine (Phase 3b): a per-week id->label map so a cover's publishing slice can be
		// transferred to whichever in-game label controls the composition (built each week to include
		// newborn labels and exclude the dead). Only used when routing is live.
		if (PublishingRoutingService.RoutingEnabled) {
			settlementLabelLookup ??= new Dictionary<string, AILabel>(System.StringComparer.Ordinal);
			settlementLabelLookup.Clear();
			foreach (var l in aiLabels) settlementLabelLookup[l.labelId] = l;
		}
		foreach (var label in aiLabels) {
			label.weeklyGrossRevenue = 0f;
			label.weeklyCogs = 0f;
			label.weeklyDistributionSkim = 0f;
			label.weeklyArtistRoyalty = 0f;
			label.weeklyNetRevenue = 0f;
			label.weeklyDistributionIncome = 0f;
			label.weeklyWholesaleDeferred = 0f;
			label.weeklyWholesaleCollected = 0f;
			label.weeklyPublishingReceipts = 0f;
			label.weeklyPressingSpend = 0f;
			label.weeklyPlantPaid = 0f;
		}
		Dictionary<string, int> offChartUnits = MajorOffChartSingles.Enabled && settlement.SettlementId > 0
			? MajorOffChartSingles.Allocate(aiLabels, settlement.Entries.Where(entry => entry.Format == ReleaseFormat.Single)
				.Sum(entry => (long)System.Math.Max(0, entry.Units)))
			: null;
		foreach (var label in aiLabels) {
			// A frozen enabled settlement may still contain a record from a label whose
			// lifecycle status changed earlier in the week. It remains an economic fact
			// and must be booked exactly once before retirement. The legacy route keeps
			// its historical inactive-label exclusion for byte-identical replay.
			if (!label.IsActive && settlement.SettlementId < 1) continue;
			float weeklyRevenue = CalculateLabelRevenue(label, settlement);
			// Billings against a wholesale house are booked but not banked: the house pays on
			// its own terms, and only for what it admits it sold.
			float deferred = DeferWholesaleBillings(label, settlement, weeklyRevenue);
			weeklyRevenue -= deferred;
			float collected = CollectMaturedWholesaleReceivables(label);
			weeklyRevenue += collected;
			float reservesReleased = ReleaseOrForfeitWholesaleReturnsReserves(label);
			weeklyRevenue += reservesReleased;
			// Off-chart catalog and specialty singles ship through the Major's own branches: never deferred.
			if (offChartUnits != null && offChartUnits.TryGetValue(label.labelId, out int catalogUnits))
				weeklyRevenue += SettleOffChartSingles(label, catalogUnits);
			label.weeklyWholesaleDeferred = deferred;
			label.weeklyWholesaleCollected = collected + reservesReleased;
			// Financing, not P&L: the withheld receipts still count as the client's revenue below.
			if (DistributionAdvanceRecoupment.Enabled) LabelCompanyAccounts.ChangeCash(label, -WithholdDistributionAdvance(label, settlementDistributorCarriedNet, weeklyRevenue), LabelCashKind.Financing);
			LabelCompanyAccounts.ChangeCash(label, weeklyRevenue, LabelCashKind.RecordReceipts);
			label.monthlyRevenue += weeklyRevenue;
			if (labelFinancials.TryGetValue(label.labelId, out var financials)) {
				financials.lastMonthRevenue += weeklyRevenue;
			}
			// LabelPressingEconomics: the pressing plant is paid on its own bill, whatever the houses still owe.
			// Under --plant-credit the bill is booked on terms and what falls due this week is paid instead.
			float plantPaid = LabelPressingEconomics.PlantCredit
				? LabelPressingEconomics.SettlePlantAccount(label, label.weeklyPressingSpend, ChartManager.Instance?.GetCurrentChartWeek() ?? 0)
				: label.weeklyPressingSpend;
			label.weeklyPlantPaid = plantPaid;
			if (plantPaid > 0f) {
				LabelCompanyAccounts.ChangeCash(label, -plantPaid, LabelCashKind.PlantPayments);
				label.monthlyExpenses += plantPaid;
				if (financials != null) financials.lastMonthExpenses += plantPaid;
			}
		}
		// Publishing goldmine banking pass (Phase 3b): credit each label the composition income it
		// collected this week from covers of songs it owns. Done AFTER the main loop so it is order-
		// independent (a receipt accrued while settling a later label still lands). Reallocation only:
		// these funds were already taken off the paying labels' net during their settlement.
		if (PublishingRoutingService.RoutingEnabled) {
			foreach (var label in aiLabels) {
				float receipts = label.weeklyPublishingReceipts;
				if (receipts == 0f) continue;
				LabelCompanyAccounts.ChangeCash(label, receipts, LabelCashKind.PublishingReceipts);
				label.monthlyRevenue += receipts;
				label.weeklyNetRevenue += receipts;
				label.lifetimePublishingReceived += receipts;
				if (labelFinancials.TryGetValue(label.labelId, out var fin)) fin.lastMonthRevenue += receipts;
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
			label.lifetimeWholesaleWriteOffs += billed - collectable;
			deferred += billed;

			// Dealer-margin-and-flip directive §4, R3: the real returns mechanism the comment above is
			// correctly refusing to model as a second charge here -- a HOLD ON CASH, never a haircut on
			// billed. The house withholds the reserve OUT OF what it owes, so receivable + reserve is the
			// collectable amount: release pays back the withheld part, forfeiture writes it off, and
			// receipts plus losses always reconcile to billed. (It was once booked ON TOP of the full
			// receivable, so a surviving relationship paid out 1 + returnAllowance of collectable.)
			// Player-only: an AI label never gets a row here, which is the whole inertness proof
			// (SimTools/DealerMarginFlipReturnsDirective.md §6) -- for AI, withheld is always zero.
			float withheld = 0f;
			if (label.isPlayerOwned) {
				withheld = Mathf.Min(collectable, billed * house.returnAllowance);
				if (withheld > 0f) {
					label.wholesaleReturnsReserves.Add(new WholesaleReturnsReserve(
						currentWeek + house.paymentTermWeeks, house.distributorId, pair.Key, withheld));
					label.outstandingWholesaleReturnsReserve += withheld;
				}
			}
			float receivable = collectable - withheld;
			label.wholesaleReceivables.Add(new WholesaleReceivable(
				currentWeek + house.paymentTermWeeks, house.distributorId, receivable));
			label.outstandingWholesaleReceivables += receivable;
		}
		return deferred;
	}

	/// <summary>
	/// Directive §4, R3: settle each reserve on its own survival test once its window closes -- released
	/// in full to cash if the label is still shipping through that house in that region, forfeited
	/// (write-off, same ledger a short-paid receivable uses) if that distribution relationship has gone
	/// cold since. No unit is charged twice by this: the underlying billing was already on units sold:
	/// this is purely a timing-and-survival claim on money the label earned but was not yet given.
	/// Iterates every label the same way CollectMaturedWholesaleReceivables does; trivially a no-op for
	/// an AI label, whose wholesaleReturnsReserves list is never populated.
	/// </summary>
	private float ReleaseOrForfeitWholesaleReturnsReserves(AILabel label) {
		if (label.wholesaleReturnsReserves.Count == 0) return 0f;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		float released = 0f;
		for (int index = label.wholesaleReturnsReserves.Count - 1; index >= 0; index--) {
			WholesaleReturnsReserve reserve = label.wholesaleReturnsReserves[index];
			if (reserve.ReleaseWeek > currentWeek) continue;
			label.wholesaleReturnsReserves.RemoveAt(index);
			label.outstandingWholesaleReturnsReserve -= reserve.Amount;
			IndependentDistributor house = independentDistributors
				.FirstOrDefault(candidate => candidate.distributorId == reserve.DistributorId);
			bool stillSelling = label.independentDistributionRegions.Contains(reserve.RegionId)
				&& house != null && house.CarriesLabel(label.labelId);
			if (stillSelling) released += reserve.Amount;
			else label.lifetimeWholesaleWriteOffs += reserve.Amount;
		}
		label.outstandingWholesaleReturnsReserve = Mathf.Max(0f, label.outstandingWholesaleReturnsReserve);
		return released;
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
	
	// --distribution-advance-recoupment: the label net on records the active deal carried, set by CalculateLabelRevenue.
	private float settlementDistributorCarriedNet;

	private float CalculateLabelRevenue(AILabel label, ChartManager.CompletedWeekSettlement settlement) {
		settlementDistributorCarriedNet = 0f;
		if (settlement?.Entries == null) return 0f;
		var profileStart = SimulationPerformanceProfiler.Begin();
		float totalRevenue = 0f;
		
		foreach (ChartManager.CompletedWeekSettlementEntry entry in settlement.EntriesForLabel(label.labelId)) {
			var lookupProfileStart = SimulationPerformanceProfiler.Begin();
			var runtimeData = entry.Record;
			SimulationPerformanceProfiler.EndRecordLookup(lookupProfileStart);
			if (runtimeData == null) continue;
			
			// A player record's unitsThisWeek carries its trunk units folded in for the chart (see
			// ChartSimulator.FinalizeWeeklySales); those were already paid to the player as spot cash in
			// PlayerDesk and must NOT be billed again here. So a player record is billed only for what
			// actually cleared through stores -- the settlement's frozen per-region snapshot, which the
			// trunk never touches. AI records keep entry.Units verbatim: the branch leaves their path
			// byte-for-byte unchanged.
			float weeklyUnits = runtimeData.baseRecord.isPlayerOwned
				? (entry.Regions?.Sum(region => Mathf.Max(0, region.FinalCleared)) ?? 0f)
				: entry.Units;
			ReleaseFormat format = runtimeData.baseRecord.format;
			// Dealer-margin directive §2.2: the settlement's job is to book what the LABEL receives on a
			// unit a dealer sold, not what the kid at the counter paid. Player-only, singles only (the
			// directive's ladder is the 45's); the existing marginSkim below is then the distributor's cut
			// of that dealer price, exactly as it already was -- no new number, no double count. AI records
			// (and player albums, out of the directive's scope) keep GetPricePerUnit(format) untouched.
			bool pressingBook = LabelPressingEconomics.Enabled && !runtimeData.baseRecord.isPlayerOwned;
			float listPrice = runtimeData.baseRecord.isPlayerOwned ? GetPricePerUnit(format)
				: PeriodListPrices.RecordList(runtimeData.baseRecord, GetPricePerUnit(format));
			float pricePerUnit = runtimeData.baseRecord.isPlayerOwned && format == ReleaseFormat.Single
				? PlayerDesk.DealerPrice
				: pressingBook ? LabelPressingEconomics.LabelPrice(format, listPrice) : listPrice;
			float pressingCost = GetPressingCostPerUnit(format);
			if (format == ReleaseFormat.Album) pressingCost += albumPackagingCostPerUnit * (runtimeData.baseRecord.album?.packaging ?? 0f);
			// Player-only: the player pressed and paid for the vinyl up front when ordering the run, so
			// no per-unit manufacturing cost is taken again at the sale. The retail price stands; a
			// wholesale house's cut is still the region-gated skim below (the trunk keeps its own margin).
			// Inert for AI records (never player-owned).
			if (runtimeData.baseRecord.isPlayerOwned) pressingCost = 0f;
			var artist = ArtistManager.Instance?.GetArtist(runtimeData.baseRecord.artistId);
			float artistRoyalty = artist?.royaltyRate ?? 0.05f;
			float skimFraction = GetSettlementDistributionSkimFraction(label, runtimeData, weeklyUnits,
				liveSettlement: GenreMarketV2.Enabled && settlement.SettlementId > 0);
			float retailGross = weeklyUnits * pricePerUnit;
			LabelCompanyAccounts.RecordSale(label, retailGross, weeklyUnits);
			float selfShare = label.activeDeal == null ? Mathf.Clamp(label.ownedReach, 0f, 1f)
				: 1f - DistributionAdvanceRecoupment.CarriedShare(label.activeDeal, skimFraction);
			float companySaleCost = LabelCorporateModel.PaySaleOperatingCosts(label, retailGross, selfShare,
				TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
			label.monthlyExpenses += companySaleCost;
			if (labelFinancials.TryGetValue(label.labelId, out var companyFinancials)) companyFinancials.lastMonthExpenses += companySaleCost;
			// LabelPressingEconomics: the act and the composition are still paid on list, not on the dealer price
			// the label now books. Off, this is retailGross exactly.
			float royaltyBasisGross = pressingBook ? weeklyUnits * listPrice : retailGross;
			// DISTANCE-4B: 4a sums by current-region hub with a neutral cost factor; 4b
			// makes manufacturing margin, skim basis, and artist recoupment region-weighted.
			float cogs = CalculateRegionalCogs(label, runtimeData, pressingCost);
			float compositionCost = !runtimeData.baseRecord.isPlayerOwned
				? weeklyUnits * LabelPressingEconomics.CompositionUnitCost(runtimeData.baseRecord) : 0f;
			cogs += compositionCost;
			float grossAfterCogs = Mathf.Max(0f, retailGross - cogs);
			float skimAmount = grossAfterCogs * skimFraction;
			// The skim stays the distributor's cut after manufacturing, as before. The label's own manufacturing
			// bill becomes freight on what sold plus the plant bill for whatever its pressing book ran this week.
			float bookedCogs = cogs, pressingSpend = 0f, initialRunSpend = 0f;
			if (pressingBook) {
				bookedCogs = Mathf.Max(0f, cogs - weeklyUnits * pressingCost);
				LabelPressingEconomics.PressingWeek press = LabelPressingEconomics.Press(runtimeData, label, Mathf.RoundToInt(weeklyUnits));
				initialRunSpend = LabelPressingEconomics.PlantBill(format, press.InitialRun, false, pressingCost);
				pressingSpend = initialRunSpend + LabelPressingEconomics.PlantBill(format, press.Repressed, true, pressingCost);
				LabelCompanyAccounts.Inventory(label, runtimeData, press.InitialRun, press.Repressed, Mathf.RoundToInt(weeklyUnits), pressingCost);
			}
			// Keep the existing artist contract convention (royalty on retail). The
			// distribution skim is based on revenue after manufacturing cost.
			// Dealer-margin directive §2.4/A5: a player single's royalty is flat off suggested list
			// (the period convention -- see PlayerDesk.RoyaltyPerUnit), independent of which channel's
			// price just got booked into retailGross above. AI records, and player albums (out of this
			// directive's scope), keep the existing retail-based convention untouched.
			float artistPayment = runtimeData.baseRecord.isPlayerOwned && format == ReleaseFormat.Single
				? weeklyUnits * PlayerDesk.RoyaltyPerUnit(artist)
				: royaltyBasisGross * artistRoyalty;
			// RECOUPMENT. The advance is cash the label already paid out (RecordExpense at signing), and
			// the period convention is that it comes back out of the artist's royalty account before the
			// artist sees a cent. Both figures below are computed for everyone and the artist-side
			// bookkeeping is identical for everyone; only who bears the cost differs.
			float recouped = artist != null
				? Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), artistPayment)
				: 0f;
			float royaltyToArtist = artistPayment - recouped;   // what the act actually banks
			// The label bears only what the act actually banks; the recouped slice stays with the label
			// that already paid it out as the advance. AI labels used to be charged the FULL royalty here
			// while the recouped slice reached nobody -- money destroyed, every AI advance paid twice --
			// and the AI economy was calibrated with that in place (CelebrityEconomyAudit-2026-10-15 §14).
			// Seeded 1960 balances (RosterManager.InitialSignArtist) are pre-1960 advances the opening
			// cash already reflects, so recouping them is a real artist-to-label transfer, not new money.
			float royaltyExpense = royaltyToArtist;
			// Early-game Phase 5 (§5.2): a master the player leased or sold to this label. The contract decides who
			// is owed what on these units (the licensor's royalty, the act's, the producer's), so it replaces the
			// royalty leg here. Null on every AI-made record: unreachable in an AI-only world.
			if (runtimeData.baseRecord.playerMasterDealId != null && PlayerDesk.Instance != null) {
				var dealWeek = PlayerDesk.Instance.SettleMasterDealWeek(runtimeData.baseRecord.playerMasterDealId, artist,
					Mathf.RoundToInt(weeklyUnits), artistPayment, recouped, royaltyExpense);
				artistPayment = dealWeek.ArtistPayment; recouped = dealWeek.ArtistRecouped;
				royaltyToArtist = artistPayment - recouped; royaltyExpense = dealWeek.Expense;
			}
			// The decomposition identity gross - cogs - skim - royalty = net has to hold, so the royalty
			// leg books what the label actually bore.
			float recordRevenue = (pressingBook ? retailGross - bookedCogs : grossAfterCogs) - skimAmount - royaltyExpense;
			// Publishing & Cover-Song Directive Part II, §II.0: the compulsory mechanical, 2c per
			// composition per copy, on wholesale-settled units. PLAYER-ONLY, DELIBERATELY -- an AI record's
			// composition cost is already folded into its calibrated pressing/COGS numbers, so this stays
			// structurally unreachable (and therefore inert) for every AI label. Singles only (a two-sided
			// 45 is the directive's whole worked example); the B-side's song-control facts were snapshotted
			// onto the A-side Record at release (PlayerDesk.FireRelease) since the B-side is never a market
			// record of its own. Unlike the 0.11-of-gross pool below, this is a brand-new liability, not a
			// reallocation, so it is charged regardless of PublishingRoutingService.RoutingEnabled.
			float mechanicalRoyalty = 0f;
			if (label.isPlayerOwned && runtimeData.baseRecord.format == ReleaseFormat.Single) {
				int mechanicalUnits = Mathf.RoundToInt(weeklyUnits);
				mechanicalRoyalty += MechanicalRoyaltyService.ChargeSide(
					runtimeData.baseRecord.publishingControl, runtimeData.baseRecord.publishingControllerLabelId,
					runtimeData.baseRecord.publishingControllerArtistId, artist, label, mechanicalUnits,
					id => GetLabel(id), id => ArtistManager.Instance?.GetArtist(id),
					MechanicalRoyaltyService.LabelCutInShare(runtimeData.baseRecord, label));
				if (!string.IsNullOrEmpty(runtimeData.baseRecord.bSideSongId)) {
					mechanicalRoyalty += MechanicalRoyaltyService.ChargeSide(
						runtimeData.baseRecord.bSidePublishingControl, runtimeData.baseRecord.bSidePublishingControllerLabelId,
						runtimeData.baseRecord.bSidePublishingControllerArtistId, artist, label, mechanicalUnits,
						id => GetLabel(id), id => ArtistManager.Instance?.GetArtist(id));
				}
				recordRevenue -= mechanicalRoyalty;
			}
			// Publishing (Scouting Mechanic Phase 4). A composition-royalty slice of gross. When the
			// label owns publishing (default, and always when managers are off) it is already inside
			// recordRevenue - nothing moves, so the economy is unchanged. When the artist kept
			// publishing (a Visionary deal) the label forgoes this slice: it comes off the label's net
			// and accrues to the artist (publishing is the artist's own composition income, not
			// advance-recoupable). PublishingShareOfGross is a calibration guess - start conservative.
			float publishingPool = royaltyBasisGross * PublishingShareOfGross;
			// --period-ai-publishing: an AI record's pool is the period mechanical its COGS already paid (audit B6).
			bool periodAiPublishing = PeriodAiPublishing.Enabled && PublishingRoutingService.RoutingEnabled && !runtimeData.baseRecord.isPlayerOwned;
			if (periodAiPublishing) publishingPool = PeriodAiPublishing.Pool(format, weeklyUnits, compositionCost);
			// Phase 3: resolve the counterparty from the composition's control type. Computed ALWAYS so
			// the telemetry reflects the would-be routing; the money only follows it when routing is live.
			PublishingRoutingService.Decision routing =
				PublishingRoutingService.Decide(runtimeData.baseRecord, artist, label.labelId);
			float labelPublishingIncome;
			float externalLeakage;
			float publishingTransferOut = 0f;
			bool artistOwnsPublishing;
			if (PublishingRoutingService.RoutingEnabled) {
				// Live routing (3b, the goldmine): the control type splits the existing pool four ways.
				// Kept stays inside recordRevenue; writer/transfer/leak come OFF net (reallocation, never
				// an addition). A transfer moves to the in-game label that OWNS the composition -- the
				// income stream from covers of a song you control.
				float keepSlice = publishingPool * routing.LabelKeepFraction;
				float writerSlice = publishingPool * routing.WriterArtistFraction;
				float transferSlice = publishingPool * routing.TransferLabelFraction;
				float leakSlice = publishingPool * routing.ExternalLeakFraction;
				if (periodAiPublishing) recordRevenue += PeriodAiPublishing.NetAdjustment(compositionCost, writerSlice, transferSlice, leakSlice);
				else recordRevenue -= writerSlice + transferSlice + leakSlice;

				// Writer royalty -> the controlling artist (on a cover, the ORIGINAL writer; falls back to
				// the performer when no controller id, i.e. the legacy artist-owns case).
				if (writerSlice > 0f) {
					SimulatedArtist writer = (!string.IsNullOrEmpty(routing.ControllerArtistId)
						? ArtistManager.Instance?.GetArtist(routing.ControllerArtistId) : null) ?? artist;
					if (writer != null) writer.totalRoyaltyEarnings += writerSlice;
				}
				// Transfer -> the in-game label that controls the composition. If it has vanished, the
				// slice leaks out of the game instead of being credited to a dead firm.
				if (transferSlice > 0f) {
					AILabel controller = routing.ControllerLabelId != null && settlementLabelLookup != null
						&& settlementLabelLookup.TryGetValue(routing.ControllerLabelId, out AILabel cl) ? cl : null;
					if (controller != null && !ReferenceEquals(controller, label)) {
						// The player's songs on an AI record earn the period mechanical, not the AI economy's 11%
							// composition pool (~9.8c a copy, five times the 1909 Act's 2c). The pool's remainder stays
							// with the buyer. Player-owned controllers only, so AI-to-AI routing is untouched.
							if (controller.isPlayerOwned && publishingPool > 0f) {
								float overPeriod = transferSlice * (1f - Mathf.Min(1f, weeklyUnits * MechanicalRoyaltyService.RatePerCopy / publishingPool));
								recordRevenue += overPeriod;
								transferSlice -= overPeriod;
							}
							// Early-game: the 2c was the publisher's and the writer's, half each. The writer's half of a
							// player-published song goes to its writer; only the publisher's half reaches the player's label.
							float writerHalf = controller.isPlayerOwned && PlayerDesk.Instance != null
								? PlayerDesk.Instance.PayWriterHalfOnCut(runtimeData.baseRecord, transferSlice, Mathf.RoundToInt(weeklyUnits))
								: 0f;
							transferSlice -= writerHalf;
							// Early-game Phase 5A: a cut of a song the player placed here. The placement's advance recoups first
						// out of this slice (the buyer, which paid it, keeps that part); the rest accrues to the player's
						// statement instead of this week's receipts. Null on every AI-chosen record.
						float buyerKeeps = runtimeData.baseRecord.playerPlacementId != null && controller.isPlayerOwned && PlayerDesk.Instance != null
							? PlayerDesk.Instance.SettlePlacementWeek(runtimeData.baseRecord.playerPlacementId, runtimeData.baseRecord.recordId,
								Mathf.RoundToInt(weeklyUnits), transferSlice)
							: -1f;
						if (buyerKeeps < 0f) controller.weeklyPublishingReceipts += transferSlice;
						else recordRevenue += buyerKeeps;
						publishingTransferOut = transferSlice - Mathf.Max(0f, buyerKeeps) + writerHalf;
					} else {
						leakSlice += transferSlice;
					}
				}
				labelPublishingIncome = keepSlice;
				externalLeakage = leakSlice;
				artistOwnsPublishing = writerSlice > 0f;
				entry.ArtistRoyalty = artistPayment + writerSlice;
				label.lifetimePublishingKept += keepSlice;
				label.lifetimePublishingTransferredOut += publishingTransferOut;
				label.lifetimePublishingLeaked += leakSlice;
			} else {
				// Legacy binary (3a: byte-identical). Artist keeps publishing only when the label does not
				// own it; the composition-model counterparty is still recorded below for measurement.
				artistOwnsPublishing = artist != null && !artist.labelOwnsPublishing;
				labelPublishingIncome = 0f;
				externalLeakage = 0f;
				if (artistOwnsPublishing) {
					recordRevenue -= publishingPool;
					artist.totalRoyaltyEarnings += publishingPool;
				} else {
					labelPublishingIncome = publishingPool;   // informational: already within recordRevenue
				}
				entry.ArtistRoyalty = artistPayment + (artistOwnsPublishing ? publishingPool : 0f);
			}
			entry.Gross = retailGross;
			entry.ManufacturingCost = bookedCogs;
			entry.PressingCost = pressingSpend;
			entry.DistributionSkim = skimAmount;
			entry.LabelNet = recordRevenue;
			entry.MarketNet = recordRevenue;
			entry.PublishingIncome = labelPublishingIncome;
			entry.ArtistOwnsPublishing = artistOwnsPublishing;
			entry.PublishingControl = runtimeData.baseRecord.publishingControl;
			entry.PublishingCounterparty = routing.Counterparty;
			entry.PublishingControllerLabelId = routing.ControllerLabelId ?? string.Empty;
			entry.ExternalPublishingLeakage = externalLeakage;
			entry.PublishingTransferOut = publishingTransferOut;
			entry.MechanicalRoyalty = mechanicalRoyalty;
			entry.DistributionIncome = 0f;
			entry.DistributionRecipientLabelId = string.Empty;
			entry.BookedCount = 1;
			// The plant bill is paid in cash by the caller, outside the receipts a wholesale house may defer. The
			// initial run is a release-time commitment, so it joins sunk production cost; re-presses follow sales.
			runtimeData.lifetimeLabelNet += recordRevenue - (pressingSpend - initialRunSpend) - companySaleCost;
			runtimeData.sunkProductionCost += initialRunSpend;
			totalRevenue += recordRevenue;
			label.weeklyGrossRevenue += retailGross;
			label.weeklyCogs += bookedCogs + pressingSpend;
			label.weeklyDistributionSkim += skimAmount;
			label.weeklyArtistRoyalty += royaltyExpense;
			label.weeklyNetRevenue += recordRevenue - pressingSpend;
			label.weeklyPressingSpend += pressingSpend;
			RevenueTelemetry formatRevenue = GetOrCreateRevenueTelemetry(label.labelId, format);
			formatRevenue.gross += retailGross;
			formatRevenue.cogs += bookedCogs + pressingSpend;
			formatRevenue.distributionSkim += skimAmount;
			formatRevenue.artistRoyalty += royaltyExpense;
			formatRevenue.labelNet += recordRevenue - pressingSpend;
			if (DistributionAdvanceRecoupment.Enabled && label.activeDeal is { unrecoupedAdvance: > 0f } carryingDeal)
				settlementDistributorCarriedNet += Mathf.Max(0f, recordRevenue) * DistributionAdvanceRecoupment.CarriedShare(carryingDeal, skimFraction);
			RouteDistributionSkim(label, skimAmount, format, entry);
			
			// Artist-side bookkeeping is unchanged for everyone -- this is exactly what the pre-existing
			// code did, so HasRecoupedCurrentContract and every lifecycle read of it are untouched.
			if (artist != null) {
				artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
				artist.totalRoyaltyEarnings += royaltyToArtist;
			}
			
		}
		
		SimulationPerformanceProfiler.EndCalculateLabelRevenue(profileStart);
		return totalRevenue;
	}

	/// <summary>
	/// MajorOffChartSingles: books this week's catalog and specialty 45s on the same per-copy terms as a live AI
	/// single (list or dealer price, plant cost, two sides' mechanical, the default 5% royalty on list, the
	/// label's own-reach skim and corporate selling costs). The copies are pressed as they sell, so there is no
	/// first run or unsold stock. Returns the receipts; the plant bill joins this week's pressing spend.
	/// </summary>
	private float SettleOffChartSingles(AILabel label, int units) {
		if (units <= 0) return 0f;
		const ReleaseFormat format = ReleaseFormat.Single;
		bool pressingBook = LabelPressingEconomics.Enabled;
		float listPrice = GetPricePerUnit(format);
		float gross = units * (pressingBook ? LabelPressingEconomics.LabelPrice(format, listPrice) : listPrice);
		float pressing = units * GetPressingCostPerUnit(format);
		float composition = LabelPressingEconomics.UsesPeriodSingleCosts(format) ? units * LabelPressingEconomics.SingleCompositionCost : 0f;
		float cogs = pressing + composition;
		float skim = Mathf.Max(0f, gross - cogs) * GetLegacyDistributionSkimFraction(label);
		float royalty = units * listPrice * .05f;
		LabelCompanyAccounts.RecordSale(label, gross, units);
		float companySaleCost = LabelCorporateModel.PaySaleOperatingCosts(label, gross, Mathf.Clamp(label.ownedReach, 0f, 1f),
			TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
		label.monthlyExpenses += companySaleCost;
		if (labelFinancials.TryGetValue(label.labelId, out var companyFinancials)) companyFinancials.lastMonthExpenses += companySaleCost;
		float plantBill = pressingBook ? pressing : 0f;
		float receipts = gross - (cogs - plantBill) - skim - royalty;
		label.weeklyGrossRevenue += gross;
		label.weeklyCogs += cogs;
		label.weeklyDistributionSkim += skim;
		label.weeklyArtistRoyalty += royalty;
		label.weeklyNetRevenue += receipts - plantBill;
		label.weeklyPressingSpend += plantBill;
		RevenueTelemetry formatRevenue = GetOrCreateRevenueTelemetry(label.labelId, format);
		formatRevenue.gross += gross;
		formatRevenue.cogs += cogs;
		formatRevenue.distributionSkim += skim;
		formatRevenue.artistRoyalty += royalty;
		formatRevenue.labelNet += receipts - plantBill;
		MajorOffChartSingles.Record(label, units, gross, receipts - plantBill - companySaleCost);
		return receipts;
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

	// The exported tables are Godot dictionaries: each TryGetValue marshals the key into a native
	// string. They are fixed after load, so each format is resolved once.
	private readonly Dictionary<ReleaseFormat, float> pricePerUnitCache = new(), legacyPressingCostCache = new();

	private float GetPricePerUnit(ReleaseFormat format) {
		if (pricePerUnitCache.TryGetValue(format, out float cached)) return PeriodListPrices.FormatList(format, cached);
		string key = format.ToString();
		float authored = pricePerUnitCache[format] = pricePerUnitByFormat != null && pricePerUnitByFormat.TryGetValue(key, out float price)
			? price
			: 0.89f;
		return PeriodListPrices.FormatList(format, authored);
	}

	public float GetPricePerUnitForAudit(ReleaseFormat format) => GetPricePerUnit(format);

	private float GetPressingCostPerUnit(ReleaseFormat format) {
		return LabelPressingEconomics.UnitPressingCost(format, GetLegacyPressingCostPerUnit(format));
	}

	private float GetLegacyPressingCostPerUnit(ReleaseFormat format) {
		if (legacyPressingCostCache.TryGetValue(format, out float cached)) return cached;
		string key = format.ToString();
		return legacyPressingCostCache[format] = pressingCostPerUnitByFormat != null && pressingCostPerUnitByFormat.TryGetValue(key, out float cost)
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

	/// <summary>
	/// Audit B3: the distributor keeps the client's receipts on the records it carried until the advance
	/// is repaid. Returns the cash withheld from the client, which goes to the distributor's cash only.
	/// </summary>
	internal float WithholdDistributionAdvance(AILabel client, float carriedNet, float bankedRevenue) {
		DistributionDeal deal = client.activeDeal;
		if (deal == null || deal.unrecoupedAdvance <= 0f) return 0f;
		AILabel distributor = GetLabel(deal.distributorId);
		if (distributor == null || distributor == client) return 0f;
		float withheld = DistributionAdvanceRecoupment.Withhold(deal.unrecoupedAdvance, carriedNet, bankedRevenue);
		if (withheld <= 0f) return 0f;
		deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - withheld);
		LabelCompanyAccounts.ChangeCash(distributor, withheld, LabelCashKind.Financing);
		EmitAdvanceRecoupment("Withheld", client, deal, null, withheld, carriedNet);
		return withheld;
	}

	/// <summary>Audit B3: what happens to an unrepaid balance when a deal ends. Off, the balance is simply abandoned.</summary>
	internal void SettleEndedDistributionAdvance(AILabel client, DistributionDeal deal, DealResolution resolution) {
		if (!DistributionAdvanceRecoupment.Enabled || deal == null || deal.unrecoupedAdvance <= 0f) return;
		float balance = deal.unrecoupedAdvance;
		if (resolution == DealResolution.Exit || resolution == DealResolution.Graduated) {
			AILabel distributor = GetLabel(deal.distributorId);
			float paid = distributor == null || distributor == client ? 0f
				: DistributionAdvanceRecoupment.ExitSettlement(balance, client.cashReserves, client.GetMonthlyOverhead());
			if (paid > 0f) {
				LabelCompanyAccounts.ChangeCash(client, -paid, LabelCashKind.Financing);
				LabelCompanyAccounts.ChangeCash(distributor, paid, LabelCashKind.Financing);
				deal.unrecoupedAdvance = balance - paid;
				EmitAdvanceRecoupment("Settled", client, deal, resolution, paid, 0f);
			}
			deal.unrecoupedAdvance = 0f;
			LabelCompanyAccounts.ForgiveFinancing(client, distributor, balance - paid);
			if (balance - paid > 0f) EmitAdvanceRecoupment("WrittenOff", client, deal, resolution, balance - paid, 0f);
			return;
		}
		deal.unrecoupedAdvance = 0f;
		LabelCompanyAccounts.ForgiveFinancing(client, GetLabel(deal.distributorId), balance);
		EmitAdvanceRecoupment("Forgiven", client, deal, resolution, balance, 0f);
	}

	/// <summary>Audit B3: a poaching Major buys the incumbent's balance out and carries it on the new deal.</summary>
	internal void BuyOutDistributionAdvance(AILabel client, DistributionDeal current, AILabel incumbent, DistributionDeal offer, AILabel major) {
		if (!DistributionAdvanceRecoupment.Enabled || current == null || current.unrecoupedAdvance <= 0f) return;
		float balance = current.unrecoupedAdvance;
		current.unrecoupedAdvance = 0f;
		LabelCompanyAccounts.ChangeCash(major, -balance, LabelCashKind.Financing);
		LabelCompanyAccounts.ChangeCash(incumbent, balance, LabelCashKind.Financing);
		offer.unrecoupedAdvance += balance;
		EmitAdvanceRecoupment("BoughtOut", client, current, DealResolution.Poached, balance, 0f);
	}

	private void EmitAdvanceRecoupment(string kind, AILabel client, DistributionDeal deal, DealResolution? resolution, float amount, float carriedNet) =>
		OnDistributionAdvanceRecoupment?.Invoke(new DistributionAdvanceRecoupmentTelemetry {
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0, Kind = kind, ClientId = client?.labelId,
			DistributorId = deal.distributorId, Resolution = resolution?.ToString() ?? string.Empty,
			Amount = amount, CarriedNet = carriedNet, BalanceAfter = deal.unrecoupedAdvance, Advance = deal.advance
		});

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

		// Off: the fee the distributor takes anyway also counts as repayment, so the advance never comes back.
		// --distribution-advance-recoupment withholds the client's receipts instead (WithholdDistributionAdvance).
		if (!DistributionAdvanceRecoupment.Enabled) {
			float recouped = Mathf.Min(Mathf.Max(0f, deal.unrecoupedAdvance), skimAmount);
			deal.unrecoupedAdvance = Mathf.Max(0f, deal.unrecoupedAdvance - recouped);
		}
		LabelCompanyAccounts.ChangeCash(distributor, skimAmount, LabelCashKind.DistributionFees);
		if (LabelCorporateModel.Commercial && LabelCorporateModel.Applies(distributor)) {
			float serviceCost = skimAmount * .85f;
			LabelCompanyAccounts.ChangeCash(distributor, -serviceCost, LabelCashKind.DistributionOperations);
			distributor.monthlyExpenses += serviceCost;
			if (labelFinancials.TryGetValue(distributor.labelId, out var serviceFinancials)) serviceFinancials.lastMonthExpenses += serviceCost;
		}
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
			// The player's label releases when the player says so, not on the AI's weekly roll.
			if (label.isPlayerOwned) continue;
			if (label.roster.Count == 0) continue;

			float releaseChance = CalculateWeeklyReleaseChance(label, date.year, date.month);
			if (GameRandom.Randf() < releaseChance) {
				WeeklyReleaseRollsFired++;
				if (TryReleaseRecord(label, date)) {
					releasesThisWeek++;
					WeeklySuccessfulReleases++;
					RecordReleaseAttempt(label.tier, success: true, artistSelectionFailure: false);
					TryIndependentSinglesProgramme(label, date);
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
		// Older worlds may have a representative theme but no album cuts. Fill those in place;
		// the album remains the sole market release, with its original release year and license.
		if (LiveRepertoire.AuditPhase >= 5)
			foreach (var runtime in ChartManager.Instance.GetAllRecords()) {
				var existing = runtime.baseRecord;
				if (existing.format == ReleaseFormat.Album && existing.album?.albumFormat == AlbumFormat.Soundtrack &&
					existing.album.externalMedia != null && existing.album.GetAllTracks().Length == 0)
					CompositionCatalogService.OnRecordReleased(existing);
			}
		if (GameRandom.Randf() >= ExternalMediaService.OriginationsForYear(date.year) / 52f) return;
		bool allowBlockbuster = soundtrackBlockbustersThisRun < ExternalMediaService.BlockbusterDecadeCap;
		ExternalMediaProfile profile = ExternalMediaService.GenerateProfile(date.year, allowBlockbuster);
		// --studio-parent-labels: a film score may go to its own studio's label before the open bid.
		AILabel label = StudioParentLabels.RouteSoundtrack(aiLabels, profile, date.year) ?? ExternalMediaService.SelectLabel(aiLabels, profile);
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
			runtimeMinutes = (float)GameRandom.RandRange(28.0, 46.0),
			thematicCohesion = Mathf.Clamp(0.6f + profile.criticalPrestige * 0.3f, 0f, 1f),
			pooledAppeal = pooledAppeal,
			packaging = Mathf.Clamp(0.45f + profile.boxOfficeTrajectory * 0.35f + (float)GameRandom.RandRange(-0.08, 0.10), 0.2f, 1f),
			isStereo = date.year >= 1968 || GameRandom.Randf() < Mathf.Lerp(0.2f, 0.8f, Mathf.Clamp((date.year - 1960f) / 8f, 0f, 1f))
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
		record.title = NameGenerator.Instance?.GenerateSongTitle(genre, date.year, record.artistId) ?? $"Soundtrack {generatedRecordCounter}";
		// Set for consistency; a soundtrack carries no roster artistId, so it can never publish
		// a cultural event -- an externally originated tie-in has no act to be influenced BY.
		album.artisticMerit = ArtisticMeritService.Evaluate(record, label.productionQuality);

		// Licensing economics: a high upfront advance, booked like any production spend.
		if (StudioParentLabels.IsStudioLabel(label) && label.cashReserves - licenseFee <= label.GetMonthlyOverhead())
			StudioParentLabels.Contribute(label, Mathf.Min(StudioParentLabels.Capacity(label), licenseFee), "soundtrack", date);
		LabelCompanyAccounts.ChangeCash(label, -licenseFee, LabelCashKind.Licensing);
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
		return CalculateLabelReleaseCapacityChance(label.releasesPerMonth * LabelCorporateModel.CapacityFactor(label), label.status, availableArtists, seasonalityMultiplier);
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
		LabelCompanyAccounts.RetireInventory(GetLabel(labelId), recordId);
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

		if (LabelPressingEconomics.Enabled) LabelPressingEconomics.Learn(GetLabel(labelId), runtimeData);
		float realizedNet = runtimeData.lifetimeLabelNet - runtimeData.SunkReleaseCost;
		OnReleaseOutcome?.Invoke(new ReleaseOutcomeTelemetry {
			unitsPressed = runtimeData.unitsPressed,
			pressingBookSold = runtimeData.pressingBookSold,
			labelId = labelId,
			recordId = recordId,
			format = runtimeData.baseRecord.format,
			genre = runtimeData.baseRecord.primaryGenre,
			memoryEligible = runtimeData.revenueMemoryEligible,
			lifetimeLabelNet = runtimeData.lifetimeLabelNet,
			sunkProductionCost = runtimeData.sunkProductionCost,
			sunkMarketingCost = runtimeData.sunkMarketingCost,
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
				FinalizePromoSynergyEvidence(project, runtimeData);
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
		ProjectRecordRole.AlbumExtraction => new[] { RevenueEstimatorLane.AlbumExtraction },
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
		float maturity = finalized ? 1f : GetResponsiveMemoryMaturity(runtimeData.baseRecord.format == ReleaseFormat.Album, age);
		float realizedToDate = runtimeData.lifetimeLabelNet - runtimeData.SunkReleaseCost;
		// Production is a one-time sunk cost. Annualize only the accumulating
		// revenue, then subtract that cost once; dividing realized net by maturity
		// repeatedly charged the same production cost and made young Albums appear
		// catastrophically unprofitable.
		float estimatedOutcome = EstimateResponsiveMemoryOutcome(runtimeData.lifetimeLabelNet,
			runtimeData.SunkReleaseCost, maturity, finalized);
		float ageMatchedExpectedNet = GetAgeMatchedExpectedNet(expectedNet,
			runtimeData.SunkReleaseCost, maturity);
		float residual = Mathf.Clamp((estimatedOutcome - expectedNet) / scale, -ResponsiveMemoryResidualLimit, ResponsiveMemoryResidualLimit);
		if (float.IsNaN(residual) || float.IsInfinity(residual)) return;
		FormatMemoryObservation observation = FindObservation(memory.observations, releaseId);
		if (observation == null) {
			observation = new FormatMemoryObservation { releaseId = releaseId, projectId = runtimeData.albumProjectId,
				releaseLane = runtimeData.projectRole, estimatorLane = lane,
				releaseWeek = runtimeData.releaseMemoryWeek, expectedNet = expectedNet, opportunityScale = scale };
			memory.observations.Add(observation);
		}
		if (!TryAdvanceResponsiveMemoryRevision(observation, age, finalized,
			out bool replacedPriorRevision, out int revisionOrdinal)) return;
		if (CareerBandedFormatMemory.Enabled) observation.careerBand = GetReleaseMemoryCareerBand(runtimeData);
		observation.allInEconomics = runtimeData.sunkMarketingCost.HasValue ? true : null;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturity;
		InvalidateResponsiveMemory(memory);
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = releaseId, labelId = runtimeData.baseRecord.labelId, format = runtimeData.baseRecord.format,
			projectId = runtimeData.albumProjectId, releaseLane = runtimeData.projectRole, estimatorLane = lane,
			careerBand = observation.careerBand, releaseWeek = observation.releaseWeek,
			genre = runtimeData.baseRecord.primaryGenre, releaseAge = age, revisionKind = finalized ? "Final" : $"Age{age}",
			revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = expectedNet, ageMatchedExpectedNet = ageMatchedExpectedNet,
			realizedNetToDate = realizedToDate, estimatedOutcomeNet = estimatedOutcome, opportunityScale = scale,
			normalizedResidual = residual, maturityWeight = maturity,
			recencyWeight = 1f, replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	// Share of lifetime label net earned by this age: linear on the frozen route, the empirical
	// front-loaded curve behind --format-memory-maturity-curve (audit A1; see FormatMemoryMaturityCurve).
	internal static float GetResponsiveMemoryMaturity(bool album, int age) =>
		FormatMemoryMaturityCurve.Enabled ? FormatMemoryMaturityCurve.Fraction(album, age)
			: Mathf.Clamp((age + 1f) / (album ? 52f : 20f), .05f, 1f);

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

	private (float Residual, float EffectiveWeight, float Confidence) GetResponsiveMemory(FormatRevenueMemory memory, int currentWeek, int careerBand = -1) {
		using var profile = SimulationPerformanceProfiler.Measure(SimulationPerformanceProfiler.Phase.RevenueMemory);
		if (memory?.observations == null) return (0f, 0f, 0f);
		ResponsiveMemorySummary rootSummary = responsiveMemorySummaries.GetValue(memory.observations, _ => new());
		bool allIn = ReleaseAllInEconomics.IsLive;
		ResponsiveMemorySummary summary = rootSummary.ForCareerBand(allIn ? careerBand + 6 : careerBand);
		if (summary.Week == currentWeek && summary.Count == memory.observations.Count && summary.CareerBand == careerBand) return summary.Value;
		float weightedResidual = 0f, weight = 0f;
		foreach (FormatMemoryObservation observation in memory.observations) {
			if (allIn && observation.allInEconomics != true) continue;
			if (careerBand >= 0 && observation.careerBand != careerBand) continue;
			int age = Mathf.Max(0, currentWeek - observation.releaseWeek);
			if (age > ResponsiveMemoryMaximumHistoryWeeks) continue;
			float recency = Mathf.Pow(.5f, age / ResponsiveMemoryHalfLifeWeeks);
			float itemWeight = recency * Mathf.Clamp(observation.maturityWeight, .05f, 1f);
			weightedResidual += observation.normalizedResidual * itemWeight;
			weight += itemWeight;
		}
		float confidence = CalculateResponsiveMemoryConfidence(weight);
		summary.Week = currentWeek;
		summary.Count = memory.observations.Count;
		summary.CareerBand = careerBand;
		return summary.Value = (weight > 0f ? weightedResidual / weight : 0f, weight, confidence);
	}

	// A release decision used to re-walk the entire pooled project history for every
	// label. Reuse the exact ordered sum until its week, list length or evidence changes.
	// Weak keys also discard summaries when a save loads replacement observation lists.
	private sealed class ResponsiveMemorySummary {
		public int Week = int.MinValue, Count = -1;
		public int CareerBand = -1;
		public (float Residual, float EffectiveWeight, float Confidence) Value;
		// The pooled project lane is shared across labels. Keep each band's cached sum so interleaved
		// debut/Star decisions do not re-walk its full history on every decision. Off allocates no array.
		private ResponsiveMemorySummary[] careerSummaries;
		public ResponsiveMemorySummary ForCareerBand(int band) {
			if (band < 0) return this;
			careerSummaries ??= new ResponsiveMemorySummary[11];
			return careerSummaries[band] ??= new ResponsiveMemorySummary();
		}
	}
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<List<FormatMemoryObservation>, ResponsiveMemorySummary> responsiveMemorySummaries = new();
	private static void InvalidateResponsiveMemory(FormatRevenueMemory memory) => responsiveMemorySummaries.Remove(memory.observations);

	private static float CalculateResponsiveMemoryConfidence(float effectiveWeight) =>
		Mathf.Min(ResponsiveMemoryMaximumConfidence,
			Mathf.Max(0f, effectiveWeight) / (Mathf.Max(0f, effectiveWeight) + ResponsiveMemoryConfidenceK));

	internal static float GetResponsiveMemoryConfidenceForProbe(float effectiveWeight) =>
		CalculateResponsiveMemoryConfidence(effectiveWeight);

	private int GetReleaseMemoryCareerBand(RecordRuntimeData runtime) {
		CareerState? scheduled = !string.IsNullOrEmpty(runtime.albumProjectId) &&
			projectById.TryGetValue(runtime.albumProjectId, out AlbumProject project) ? project.careerStateAtSchedule : null;
		return GetMemoryCareerBand(runtime.launchCareerState, scheduled, runtime.projectRole);
	}

	internal static int GetMemoryCareerBand(CareerState launch, CareerState? scheduled,
		ProjectRecordRole role = ProjectRecordRole.LinkedAlbum) {
		CareerState state = role is ProjectRecordRole.LinkedAlbum or ProjectRecordRole.StandaloneAlbum or ProjectRecordRole.PromoSingle
			? scheduled ?? launch : launch;
		int band = GetCareerBandIndex(state, out bool fallback);
		// Preserve the prior's legacy fallback, but do not teach debutants from Declining/Dropped acts.
		return fallback ? 4 : band;
	}

	internal (float Residual, float EffectiveWeight, float Confidence) ResponsiveMemoryForProbe(
		FormatRevenueMemory memory, int currentWeek, int careerBand) =>
		GetResponsiveMemory(memory, currentWeek, CareerBandedFormatMemory.Enabled ? careerBand : -1);

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
		FormatMemoryObservation observation = FindObservation(memory.observations, project.projectId);
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
		if (CareerBandedFormatMemory.Enabled) observation.careerBand = GetMemoryCareerBand(project.careerStateAtSchedule, null);
		observation.allInEconomics = project.allInEconomics;
		observation.normalizedResidual = residual;
		observation.maturityWeight = finalized ? 1f : Mathf.Clamp((promoMaturity + albumMaturity) * .5f, .05f, 1f);
		InvalidateResponsiveMemory(memory);
		UpdatePooledAlbumWithPromoObservation(project, projectAge, finalized, residual, observation.maturityWeight);
		OnFormatMemoryRevision?.Invoke(new FormatMemoryRevisionTelemetry {
			releaseId = project.projectId, projectId = project.projectId, labelId = project.currentLabelId,
			format = ReleaseFormat.Album, releaseLane = ProjectRecordRole.LinkedAlbum,
			estimatorLane = RevenueEstimatorLane.AlbumWithPromo, genre = project.genre,
			careerBand = observation.careerBand, releaseWeek = observation.releaseWeek,
			releaseAge = projectAge, revisionKind = finalized ? "Final" : $"Age{projectAge}", revisionOrdinal = revisionOrdinal,
			releaseTimeExpectedNet = project.projectedProjectNet, ageMatchedExpectedNet = project.projectedProjectNet,
			realizedNetToDate = (project.heldPromoOutcome ?? 0f) + (project.heldAlbumOutcome ?? 0f),
			estimatedOutcomeNet = estimatedProject, opportunityScale = observation.opportunityScale,
			normalizedResidual = residual, maturityWeight = observation.maturityWeight, recencyWeight = 1f,
			replacedPriorRevision = replacedPriorRevision, finalized = observation.finalized
		});
	}

	// Observation lists only ever grow by Add (a load brings new lists), so a per-list index that catches up on the
	// tail answers FirstOrDefault(o => o.releaseId == id) exactly: the first observation with an id wins. The linear
	// scan over the pooled album memory was ~17% of a late-decade run.
	private sealed class ObservationIndex {
		public readonly Dictionary<string, FormatMemoryObservation> ById = new(System.StringComparer.Ordinal);
		public FormatMemoryObservation FirstNullId;
		public int Synced;
	}
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<List<FormatMemoryObservation>, ObservationIndex> observationIndexes = new();
	private static FormatMemoryObservation FindObservation(List<FormatMemoryObservation> observations, string releaseId) {
		ObservationIndex index = observationIndexes.GetValue(observations, _ => new ObservationIndex());
		if (index.Synced > observations.Count) observationIndexes.AddOrUpdate(observations, index = new ObservationIndex());
		while (index.Synced < observations.Count) {
			FormatMemoryObservation item = observations[index.Synced++];
			if (item.releaseId == null) index.FirstNullId ??= item;
			else index.ById.TryAdd(item.releaseId, item);
		}
		if (releaseId == null) return index.FirstNullId;
		return index.ById.TryGetValue(releaseId, out FormatMemoryObservation found) ? found : null;
	}

	private void UpdatePooledAlbumWithPromoObservation(AlbumProject project, int projectAge, bool finalized,
		float residual, float maturityWeight) {
		FormatMemoryObservation observation = FindObservation(pooledAlbumWithPromoMemory.observations, project.projectId);
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
		if (CareerBandedFormatMemory.Enabled) observation.careerBand = GetMemoryCareerBand(project.careerStateAtSchedule, null);
		observation.allInEconomics = project.allInEconomics;
		observation.normalizedResidual = residual;
		observation.maturityWeight = maturityWeight;
		InvalidateResponsiveMemory(pooledAlbumWithPromoMemory);
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
		maturity = FormatMemoryMaturityCurve.Enabled
			? FormatMemoryMaturityCurve.Fraction(terminalAge >= 52f, runtime.weeksSinceRelease)
			: Mathf.Clamp((runtime.weeksSinceRelease + 1f) / terminalAge, .05f, 1f);
		float estimated = EstimateResponsiveMemoryOutcome(runtime.lifetimeLabelNet, runtime.SunkReleaseCost, maturity, finalized: false);
		return estimated - expectedNet;
	}

	private void RedirectCancelledPromoOutcome(AlbumProject project) {
		if (project?.heldPromoOutcome == null || project.promoOutcomeState == ProjectOutcomeState.RedirectedToSingle) return;
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true))
			ApplyMemoryObservation(project.currentLabelId, ReleaseFormat.Single, project.heldPromoOutcome.Value);
		project.promoOutcomeState = ProjectOutcomeState.RedirectedToSingle;
	}
	
	private bool TryReleaseRecord(AILabel label, GameDate date, bool singlesOnly = false, SimulatedArtist scheduledSingleArtist = null) {
		lastReleaseAttemptFailedArtistSelection = false;
		lastPracticeStandalone = false;
		PrepareAnnualFormatCapacity(date.year);
		var artist = scheduledSingleArtist ?? RosterManager.Instance?.GetArtistForRelease(label) ?? label.GetArtistForRelease(date.year);
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
		// World lineup churn keeps an act whose only (or last) member is in the service on ice rather than ending it
		// (BandLifeService.WouldDissolve). Nobody is there to record until they come home. Never true without churn.
		if (artist.members == null || artist.members.Count == 0) {
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
		ReleasePlan plan = DecideRelease(label, decisionArtist, date.year, decision, singleProgrammeOnly: singlesOnly);
		if (singlesOnly) {
			if (!ReleaseAllInEconomics.IsLive && plan.projectedOrphanSingleNet <= label.GetMarketingBudget(artist)) {
				if (!explicitProjectIdentity) { artist.primaryGenre = artistPrimary; artist.secondaryGenre = artistSecondary; }
				return false;
			}
			plan.format = ReleaseFormat.Single; plan.strategy = ReleaseStrategy.OrphanSingle;
		}
		// --hit-act-cadence (§17x): a lane release is the act's follow-up single; its album waits for a later roll.
		if (HitActCadence.LaneEnabled && HitActCadence.ShouldForceSingle(plan.format == ReleaseFormat.Album)) {
			plan.format = ReleaseFormat.Single;
			plan.strategy = ReleaseStrategy.OrphanSingle;
		}
		if (!explicitProjectIdentity) {
			artist.primaryGenre = artistPrimary;
			artist.secondaryGenre = artistSecondary;
		}
		if (plan.format == ReleaseFormat.Album) {
			if (!PassesReleaseProfitGate(label, artist, date.year, projectGenre, plan)) return false;
			return TryInitiateAlbumProject(label, artist, date, decision, plan, projectGenre);
		}
		// A catalogue extraction already has its own incremental-profit and funding checks.
		// Evaluate that cheaper option before rejecting a newly recorded single.
		var record = TryExtractSuccessfulAlbumSingle(label, artist, date, decision, ref plan);
		if (record == null) {
			if (!PassesReleaseProfitGate(label, artist, date.year, projectGenre, plan)) return false;
			record = GenerateRecordFromArtist(label, artist, date.year, plan.format, judgedOrphan: true);
		}
		if (record == null) return false;
		if (plan.strategy != ReleaseStrategy.AlbumExtraction && GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(record, projectGenre, artistPrimary);
		float realizedQuality = (record.hookStrength + record.productionQuality) / 2f;
		float noiseRange = Mathf.Lerp(0.30f, 0.10f, label.scoutingAbility);
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GameRandom.RandRange(-noiseRange, noiseRange), 0f, 1f);
		float perceivedQualityMult = 0.6f + (perceivedQuality * 0.8f);

		float productionCost = CalculateProductionCost(label, record, date);
		float marketingBudget = label.GetMarketingBudget(artist) * perceivedQualityMult;
		float totalCost = productionCost + marketingBudget;
		float minReserve = (EconomicLabelLifecycle.Enabled && !label.isPlayerOwned
			? EconomicLabelLifecycle.CommitmentReserve(label, signing: false) : label.GetMonthlyOverhead())
			+ InitialRunReserve(label, record);

		if (label.cashReserves - totalCost < minReserve) {
			float available = label.cashReserves - minReserve - productionCost;
			// Opt-in early-game routes, in the founder's order: borrow from the one backer (keeps the master), else shop it.
			if (available < 0 && AiBackers.TryBorrow(label, -available, date)) available = label.cashReserves - minReserve - productionCost;
			if (available < 0) return AiMasterDeals.TryShopUnreleased(this, label, artist, record, productionCost, perceivedQuality, date);
			marketingBudget = available * 0.8f;
			totalCost = productionCost + marketingBudget;
		}
		
		LabelCompanyAccounts.PayProject(label, totalCost, productionCost);
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
		record.projectRole = plan.strategy == ReleaseStrategy.AlbumExtraction ? ProjectRecordRole.AlbumExtraction : ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;
		ChartManager.Instance.ReleaseRecord(record);
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) throw new System.InvalidOperationException($"Released record '{record.recordId}' has no runtime data.");
		runtimeData.sunkProductionCost = productionCost;
		if (ReleaseAllInEconomics.IsLive) runtimeData.sunkMarketingCost = 0f;
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
		AiRepertoireService.ObserveSingleRelease(label, artist, record, plan.strategy, date);
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
				strategy = plan.strategy,
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
		// Last, after every economic and telemetry effect of THIS record has landed. Any
		// identity ratification here reaches the artist only on the next project selection,
		// which is exactly what "lagging ratification" means.
		ArtistEvolutionService.OnProjectReleased(artist, record.primaryGenre, date.year, label);
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
		if (album == null) return false;
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			ApplyProjectGenre(album, projectGenre, artist.primaryGenre);
		plan = ResolveRecordedAlbumCampaign(label, artist, date, decision, plan, album);
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
			promo = CreatePromoSingleFromAlbum(album, artist, label, date.year);
			if (PolarSongBehavior.UsePolarFitSelection && promo == null) return false;
			if (AlbumReleasePractice.IsLive && promo != null) promo.sourceAlbumRecordId = album.recordId;
			gapWeeks = (int)GameRandom.RandRange(Mathf.Min(albumDropGapWeeksMin, albumDropGapWeeksMax), Mathf.Max(albumDropGapWeeksMin, albumDropGapWeeksMax));
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
		float pressingReserve = InitialRunReserve(label, album) + (promo != null ? InitialRunReserve(label, promo) : 0f);
		if (!TryClampFirstEventCost(label, productionTotal, ref firstMarketing, pressingReserve)) return false;
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
			allInEconomics = ReleaseAllInEconomics.IsLive ? true : null,
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
		AttachPromoSynergyEvidence(project, label, artist, plan);

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
			AiRepertoireService.ObserveSingleRelease(label, artist, promo, ReleaseStrategy.AlbumWithPromo, date);
		}
		artist.weeksSinceLastRelease = 0;
		EmitAlbumDecisionTelemetry(label, artist, decision, plan, album, project);
		// One push per project. An album with a promo single is two records and one
		// creative decision; the linked album's later drop must not vote a second time.
		ArtistEvolutionService.OnProjectReleased(artist, projectGenre, date.year, label);
		if (AlbumReleasePractice.IsLive && plan.strategy == ReleaseStrategy.AlbumStandalone) {
			lastPracticeStandalone = true; lastPracticeAlbumGenre = projectGenre; lastPracticeAlbumId = album.recordId;
			lastPracticeAlbumArtist = artist;
		}
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
		float perceivedQuality = Mathf.Clamp(realizedQuality + (float)GameRandom.RandRange(-noiseRange, noiseRange), 0f, 1f);
		return 0.6f + perceivedQuality * 0.8f;
	}

	/// <summary>LabelPressingEconomics: the record's expected initial run at its own unit pressing cost (0 when off).</summary>
	private float InitialRunReserve(AILabel label, Record record) {
		if (!LabelPressingEconomics.Enabled || record == null) return 0f;
		float unit = GetPressingCostPerUnit(record.format);
		if (record.format == ReleaseFormat.Album) unit += albumPackagingCostPerUnit * (record.album?.packaging ?? 0f);
		return LabelPressingEconomics.InitialRunReserve(label, record.format, unit);
	}

	private bool TryClampFirstEventCost(AILabel label, float productionCost, ref float marketingBudget, float pressingReserve = 0f) {
		float minReserve = (EconomicLabelLifecycle.Enabled && !label.isPlayerOwned
			? EconomicLabelLifecycle.CommitmentReserve(label, signing: false) : label.GetMonthlyOverhead()) + pressingReserve;
		if (label.cashReserves - productionCost - marketingBudget >= minReserve) return true;
		float available = label.cashReserves - minReserve - productionCost;
		if (available < 0f) return false;
		marketingBudget = available * 0.8f;
		return true;
	}

	private void ChargeProjectCost(AILabel label, SimulatedArtist artist, float totalCost, float productionCost) {
		LabelCompanyAccounts.PayProject(label, totalCost, productionCost);
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
		if (ReleaseAllInEconomics.IsLive || (!string.IsNullOrEmpty(projectId) && projectById.TryGetValue(projectId, out var costProject) && costProject.allInEconomics == true))
			runtime.sunkMarketingCost = 0f;
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

	private ReleasePlan DecideRelease(AILabel label, SimulatedArtist artist, int year, DecisionContext decision, bool singleProgrammeOnly = false) {
		using var profile = SimulationPerformanceProfiler.Measure(SimulationPerformanceProfiler.Phase.ReleaseDecisions);
		if (!enableAlbums && !ReleaseAllInEconomics.IsLive) return new() { format = ReleaseFormat.Single, strategy = ReleaseStrategy.OrphanSingle };
		singleProgrammeOnly |= !enableAlbums;

		decision.qualityQuartile = GetQualityQuartile(decision.qualityEstimate);
		decision.careerBand = GetCareerBandIndex(artist.careerState, out bool unexpectedCareerState);
		decision.unexpectedCareerState = unexpectedCareerState;
		float compCostWeight = CalculateCompilationCostWeight(artist.primaryGenre, year);
		HitInventory hitInventory = ResolveHitInventory(artist);
		float priorSingle = SinglePriorNetAtBookedPrice(decision);
		float priorAlbum = CalculateAlbumPriorNet(label, artist, year, decision, compCostWeight, hitInventory, out AlbumPriorDiagnostics albumPrior);
		float expectedMarketing = ReleaseAllInEconomics.IsLive
			? ReleaseAllInEconomics.ExpectedCampaign(label.GetMarketingBudget(artist), decision.qualityEstimate) : 0f;
		if (ReleaseAllInEconomics.IsLive) { priorSingle -= expectedMarketing; priorAlbum -= expectedMarketing; }
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
		int memoryCareerBand = CareerBandedFormatMemory.Enabled ? GetMemoryCareerBand(artist.careerState, null) : -1;
		var singleResponsive = GetResponsiveMemory(singleMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0, memoryCareerBand);
		var albumResponsive = GetResponsiveMemory(albumMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0, memoryCareerBand);
		var standaloneResponsive = GetResponsiveMemory(standaloneMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0, memoryCareerBand);
		var promoResponsive = GetResponsiveMemory(promoMemory, ChartManager.Instance?.GetCurrentChartWeek() ?? 0, memoryCareerBand);
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		var labelProjectResponsive = GetResponsiveMemory(projectMemory, currentWeek, memoryCareerBand);
		var pooledProjectResponsive = GetResponsiveMemory(pooledAlbumWithPromoMemory, currentWeek, memoryCareerBand);
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
		float singleNoiseMultiplier = 1f + (float)GameRandom.RandRange(-noiseRange, noiseRange);
		float albumNoiseMultiplier = 1f + (float)GameRandom.RandRange(-noiseRange, noiseRange);
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
		float expectedPromoSingleNet = SinglePriorNetAtBookedPrice(decision);
		if (ReleaseAllInEconomics.IsLive) expectedPromoSingleNet -= expectedMarketing;
		float promoConfidence = useResponsiveMemory ? promoResponsive.Confidence : confidenceSingle;
		if (useResponsiveMemory) expectedPromoSingleNet += promoConfidence * promoResponsive.Residual * Mathf.Max(1f, Mathf.Abs(expectedPromoSingleNet));
		float promoUnitMargin = ReleaseAllInEconomics.IsLive
			? LabelPressingEconomics.LabelPrice(ReleaseFormat.Single, GetPricePerUnit(ReleaseFormat.Single))
				- GetPressingCostPerUnit(ReleaseFormat.Single) - (LabelPressingEconomics.PeriodSingleCosts ? LabelPressingEconomics.SingleCompositionCost : 0f) - GetPricePerUnit(ReleaseFormat.Single) * .05f
			: singleNetMarginPerUnit;
		promoUnitMargin -= decision.companySingleUnitCost;
		float expectedSingleUnits = Mathf.Max(0f,
			(expectedPromoSingleNet + decision.singleProductionCost + expectedMarketing) / Mathf.Max(promoUnitMargin, 0.000001f));
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
		//
		// A5: neither album-side term had any feedback. When live, realized recruitment and
		// suppression of finalized linked LPs scale the modelled terms; no evidence leaves 1.
		bool promoFeedbackLive = useResponsiveMemory && PromoSynergyFeedback.IsLive;
		int promoFeedbackBand = promoFeedbackLive ? GetMemoryCareerBand(artist.careerState, null) : 0;
		var promoScales = promoFeedbackLive ? GetPromoSynergyScales(promoFeedbackBand, currentWeek) : (Recruitment: 1f, Cannibalization: 1f, Weight: 0f);
		float chargedCannibalizationLoss = CalculateChargedPromoCannibalization(
			promoFeedbackLive ? cannibalizationLoss * promoScales.Cannibalization : cannibalizationLoss, confidenceAlbum);
		float promoAdvantage = promoFeedbackLive
			? (expectedPromoLift + promoSynergyGain) * promoScales.Recruitment + expectedPromoSingleNet - chargedCannibalizationLoss
			: expectedPromoLift + promoSynergyGain + expectedPromoSingleNet - chargedCannibalizationLoss;
		float componentProjectedAlbumWithPromo = projectedAlbum + promoAdvantage;
		float projectedAlbumWithPromo = componentProjectedAlbumWithPromo;
		if (useResponsiveMemory) projectedAlbumWithPromo += projectResponsive.Confidence * projectResponsive.Residual *
			Mathf.Max(1f, Mathf.Abs(projectedAlbumWithPromo));
		float promoProjectDelayPremium = meanAlbumDropGapWeeks / 52f;
		// Commit the physical LP on its own proposition. A future campaign cannot
		// manufacture eligibility before any album material has been recorded.
		if (AlbumReleasePractice.IsLive) {
			componentProjectedAlbumWithPromo = projectedStandaloneAlbum;
			projectedAlbumWithPromo = projectedStandaloneAlbum;
			promoProjectDelayPremium = 0f;
		}

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
		if (singleProgrammeOnly) { albumWins = false; promoPreferred = false; }
		bool albumProjectPressure = useResponsiveMemory &&
			IsAlbumProjectSharePressureHigh(annualFormatDecisions, annualAlbumProjectsScheduled);
		bool albumCapacityReroute = false;
		if (albumWins && !CanScheduleAnnualAlbumProject(
			annualAlbumProjectsByArtist.GetValueOrDefault((artist.artistId, year)), albumProjectPressure)) {
			albumWins = false;
			promoPreferred = false;
			albumCapacityReroute = true;
		}
		// In single-led markets usually try the single programme before the LP.
		// A regional/specialist single need not enter the national pop chart to
		// establish that programme. Actual hits still inform the economic prior.
		if (albumWins && AlbumReleasePractice.IsLive && AlbumReleasePractice.HitLedAlbums &&
			AlbumReleasePractice.NeedsSingleAudience(artist.primaryGenre, year, hitInventory.resolvedSingles,
				AlbumReleasePractice.Roll("lp-programme", label.labelId, artist.artistId, "", pipelineWeek))) {
			albumWins = false; promoPreferred = false;
			OnAlbumPracticeDecision?.Invoke(new AlbumPracticeTelemetry {
				Year = year, Week = pipelineWeek, LabelId = label.labelId, ArtistId = artist.artistId,
				Genre = artist.primaryGenre.ToString(), Route = "SingleBeforeAlbum", Reason = "establish-single-programme-first"
			});
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
			expectedMarketingBudget = expectedMarketing, albumPortfolioCredit = albumPortfolioCredit,
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
			singleNetMarginPerUnit = promoUnitMargin,
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
			promoFeedbackEvaluated = promoFeedbackLive && !AlbumReleasePractice.IsLive,
			promoFeedbackCareerBand = promoFeedbackBand,
			promoSynergyGain = promoSynergyGain,
			promoRecruitmentScale = promoScales.Recruitment,
			promoCannibalizationScale = promoScales.Cannibalization,
			promoFeedbackEvidenceWeight = promoScales.Weight,
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
			companySingleUnitCost = LabelCorporateModel.SaleOperatingCost(label, PlayerDesk.DealerPrice,
				label.ownedReach, new GameDate(year, month, 1)),
			qualityEstimate = qualityEstimate,
			reachFactor = Mathf.Max(0f, label.distributionStrength),
			genreSinglesMarketFactor = CalculateSingleGenreMarketFactor(artist.primaryGenre, year),
			singleProductionCost = singleProductionCost,
			recordingCostMultiplier = recordingCostMultiplier
		};
	}

	/// <summary>
	/// The single prior's contribution table was fitted at the counter price. Under LabelPressingEconomics it is
	/// rescaled by the per-unit margin the label now actually books, as the album prior already re-prices itself,
	/// so neither format gains a spurious edge from the price change. Off, this is CalculateSinglePriorNet.
	/// </summary>
	private float SinglePriorNetAtBookedPrice(DecisionContext decision) =>
		SinglePriorNetAtBookedPrice(decision, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);

	private float SinglePriorNetAtBookedPrice(DecisionContext decision, bool live) {
		if (SinglePriorRecalibration.Enabled && live) {
			float currentList = GetPricePerUnit(ReleaseFormat.Single);
			float currentMargin = LabelPressingEconomics.LabelPrice(ReleaseFormat.Single, currentList)
				- GetPressingCostPerUnit(ReleaseFormat.Single) - LabelPressingEconomics.SingleCompositionCost - currentList * .05f - decision.companySingleUnitCost;
			return SinglePriorRecalibration.EstimateNet(decision.qualityEstimate, decision.careerBand,
				decision.reachFactor, decision.genreSinglesMarketFactor, decision.singleProductionCost, currentMargin);
		}
		float net = CalculateSinglePriorNet(decision);
		if (!LabelPressingEconomics.Enabled) return net;
		float list = GetPricePerUnit(ReleaseFormat.Single), pressing = GetPressingCostPerUnit(ReleaseFormat.Single);
		float composition = LabelPressingEconomics.PeriodSingleCosts ? LabelPressingEconomics.SingleCompositionCost : 0f;
		float royalty = list * 0.05f;
		float scale = Mathf.Max(0f, LabelPressingEconomics.LabelPrice(ReleaseFormat.Single, list) - pressing - composition - royalty - decision.companySingleUnitCost) /
			Mathf.Max(0.01f, list - GetLegacyPressingCostPerUnit(ReleaseFormat.Single) - royalty);
		return (net + decision.singleProductionCost) * scale - decision.singleProductionCost;
	}

	internal float SinglePriorNetForProbe(float quality, int careerBand, float reach, float genreFactor,
		float productionCost, bool live) => SinglePriorNetAtBookedPrice(new DecisionContext {
			qualityEstimate = quality, qualityQuartile = GetQualityQuartile(quality), careerBand = careerBand,
			reachFactor = reach, genreSinglesMarketFactor = genreFactor, singleProductionCost = productionCost
		}, live);

	private static float CalculateSinglePriorNet(DecisionContext decision) {
		float expectedContribution = SinglePriorNormalizedContribution[decision.qualityQuartile, decision.careerBand]
			* decision.reachFactor * decision.genreSinglesMarketFactor;
		return expectedContribution - decision.singleProductionCost;
	}

	private float CalculateAlbumPriorNet(AILabel label, SimulatedArtist artist, int year, DecisionContext decision,
		float compCostWeight, HitInventory hitInventory, out AlbumPriorDiagnostics diagnostics) {
		IEnumerable<MarketRegion> regions = ChartManager.Instance != null ? ChartManager.Instance.GetAllRegions() : Enumerable.Empty<MarketRegion>();
		bool live = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		AlbumPriorExplanation opportunity = GetAlbumPriorExplanation(artist.primaryGenre, regions, year, live);
		float statureMultiplier = GetAlbumPriorCareerMultiplier(artist.careerState, live || AlbumPriorRecalibration.IsLive);
		float baseAffinityUnits = priorUnitScalarAlbum * decision.qualityEstimate * statureMultiplier *
			decision.reachFactor * (live ? CalculateAlbumPriorEraCalibration(year) : 1f);
		float preTiltAffinityUnits = baseAffinityUnits * opportunity.UntiltedAlbumDemandFactor * opportunity.MarketReconciliation;
		float unweightedHitUnits = priorCompHitUnitScalar * hitInventory.hitScore;
		if (AlbumPriorRecalibration.IsLive) unweightedHitUnits *= AlbumPriorRecalibration.HitCoefficient;
		float weightedHitUnits = compCostWeight * unweightedHitUnits;
		// Format suitability applies to the whole Album proposition. Applying it
		// only to affinity units let compilation/hit inventory bypass the catalog
		// orientation and dominate the late format fork.
		float preTiltExpectedUnits = preTiltAffinityUnits + weightedHitUnits;
		float affinityUnits = preTiltAffinityUnits * opportunity.FormatTilt;
		float expectedUnits = CalculateFormatTiltedAlbumExpectedUnits(
			preTiltAffinityUnits, weightedHitUnits, opportunity.FormatTilt);
		if (live) {
			expectedUnits *= AlbumSalesVolume.Multiplier;
			affinityUnits *= AlbumSalesVolume.Multiplier;
			preTiltExpectedUnits *= AlbumSalesVolume.Multiplier;
			weightedHitUnits *= AlbumSalesVolume.Multiplier;
			unweightedHitUnits *= AlbumSalesVolume.Multiplier;
		}
		float manufacturingPerUnit = GetPressingCostPerUnit(ReleaseFormat.Album) + albumPackagingCostPerUnit * priorAssumedAlbumPackaging;
		if (LabelPressingEconomics.PeriodAlbumCosts) manufacturingPerUnit += LabelPressingEconomics.ExpectedAlbumCompositionCost;
		float albumBookedPrice = LabelPressingEconomics.Enabled
			? LabelPressingEconomics.LabelPrice(ReleaseFormat.Album, GetPricePerUnit(ReleaseFormat.Album))
			: GetPricePerUnit(ReleaseFormat.Album);
		float grossAfterManufacturing = Mathf.Max(0f, albumBookedPrice - manufacturingPerUnit);
		float skimFraction = label.activeDeal != null
			? Mathf.Clamp(label.activeDeal.marginSkim, 0f, 1f)
			: 0.25f * (1f - label.ownedReach);
		float royaltyRate = artist?.royaltyRate ?? baseRoyaltyRate;
		float marginPerUnit = grossAfterManufacturing * (1f - skimFraction) - GetPricePerUnit(ReleaseFormat.Album) * royaltyRate;
		marginPerUnit -= LabelCorporateModel.SaleOperatingCost(label, albumBookedPrice, label.ownedReach,
			TimeManager.Instance?.CurrentDate ?? new GameDate(year, 1, 1));
		float multiplier = compCostWeight * compilationProductionMultiplier + (1f - compCostWeight) * 2.4f;
		if (AlbumMasterCosts.IsLive) multiplier = AlbumMasterCosts.ExpectedMultiplier(compCostWeight, hitInventory.resolvedSingles, compilationProductionMultiplier);
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

	internal static float GetAlbumPriorCareerMultiplier(CareerState state, bool live) =>
		AlbumPriorRecalibration.Enabled && live ? AlbumPriorRecalibration.CareerAffinity(state) : state switch {
			CareerState.Superstar => 2.5f, CareerState.Star => 2f, CareerState.Established => 1.5f,
			CareerState.Rising => 1.2f, _ => 1f
		};

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
		if (AlbumReleasePractice.IsLive && !string.IsNullOrEmpty(record?.sourceAlbumRecordId)) return baseCost * .15f;
		if (record?.format != ReleaseFormat.Album) return baseCost;
		float multiplier = record.album?.albumFormat == AlbumFormat.Compilation
			? compilationProductionMultiplier
			: 2.4f;
		if (AlbumMasterCosts.IsLive && record.album?.albumFormat == AlbumFormat.Compilation)
			multiplier = AlbumMasterCosts.CompilationMultiplier(record.album.trackRefs?.Length ?? 0, compilationProductionMultiplier);
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
		public float expectedMarketingBudget, albumPortfolioCredit;
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
		// A5: unscaled synergy term and the realized-evidence scales applied to it.
		public bool promoFeedbackEvaluated;
		public int promoFeedbackCareerBand;
		public float promoSynergyGain;
		public float promoRecruitmentScale;
		public float promoCannibalizationScale;
		public float promoFeedbackEvidenceWeight;
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
		public float companySingleUnitCost;
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
	
	private Record GenerateRecordFromArtist(AILabel label, SimulatedArtist artist, int year, ReleaseFormat format = ReleaseFormat.Single,
		bool judgedOrphan = false) {
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
			record.title = NameGenerator.Instance.GenerateSongTitle(record.primaryGenre, year, record.artistId);
		} else {
			record.title = $"Song {generatedRecordCounter}";
		}
		
		float artistQuality = artist.CalculateRecordQuality();
		float studioMod = 1f;
		if (label.strongRegions != null && label.strongRegions.Length > 0) {
			var region = ChartManager.Instance?.GetRegionById(label.strongRegions[0]);
			if (region != null) studioMod = ChartSimulator.GetStudioQualityModifier(region);
		}
		
		// --single-studio-modifier applies the HQ studio once; off, it is blended and then multiplied again (audit C3).
		float baseQuality = SingleStudioModifier.BaseQuality(artistQuality, studioMod);
		
		record.hookStrength = Mathf.Clamp((artist.songwritingAbility * 0.55f) + (baseQuality * 0.35f) + (float)GameRandom.RandRange(-0.12, 0.18), 0f, 1f);
		record.productionQuality = Mathf.Clamp((label.productionQuality * 0.4f) + (artist.studioPerformance * 0.3f) + (studioMod * 0.2f) + (float)GameRandom.RandRange(-0.05, 0.1), 0f, 1f);
		record.originality = Mathf.Clamp(artist.members.Max(m => m.creativity) * 0.7f + (float)GameRandom.RandRange(0f, 0.3f), 0f, 1f);
		record.danceability = (float)GameRandom.RandRange(0.3, 0.95);
		record.controversy = (float)GameRandom.RandRange(0f, 0.2f);
		
		if (record.primaryGenre == Genre.Gospel) record.controversy = Mathf.Min(record.controversy, 0.05f);
		else if (record.primaryGenre == Genre.RockAndRoll || record.primaryGenre == Genre.GarageRock) record.danceability = Mathf.Max(record.danceability, 0.5f);

		if (format == ReleaseFormat.Album) {
			record.album = GenerateAlbum(label, artist, year, record.recordId, record.secondaryGenre);
			if (PolarSongBehavior.UsePolarFitSelection && record.album.trackRefs.Length + record.album.nonSingleTracks.Length == 0) return null;
			record.title = GenerateAlbumTitle(record, year);
			record.hookStrength = record.album.pooledAppeal;
			record.productionQuality = Mathf.Clamp(record.album.pooledAppeal * 0.75f + label.productionQuality * 0.25f, 0f, 1f);
			record.danceability = record.album.pooledAppeal;
			// Fixed the day it is pressed and never revisited. Nothing downstream may change it:
			// the press layer changes how widely a record's merit is KNOWN, never the merit.
			record.album.artisticMerit = ArtisticMeritService.Evaluate(record, label.productionQuality);
		}

		// Publishing & Cover-Song layer: choose the release's material (artist-written / professional /
		// cover / standard / traditional) and stamp it onto the record. Selection is a pure stable
		// hash (never GD), so it does not perturb the RNG schedule; material now shapes hook/originality
		// (chart-facing) but not money (settlement still keys off labelOwnsPublishing until Phase 3).
		if (SongMaterialSelectionService.Enabled) {
			int chartWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			// Release Judgement 2A (--release-judgement-select): a live orphan single is cut from the act's shortlist; an empty
			// or refused shortlist, and every other caller, keeps the legacy source choice. Off: never reached.
			SelectedSongMaterial material = judgedOrphan && format == ReleaseFormat.Single && ReleaseJudgement.Selecting
				? AiRepertoireService.SelectMaterial(label, artist, record, record.primaryGenre, year) : null;
			material ??= SongMaterialSelectionService.ChooseMaterial(label, artist, record, record.primaryGenre, year, chartWeek);
			if (PolarSongBehavior.UsePolarFitSelection && material == null) return null;
			// Early-game Phase 5A (§7.2): a song the player placed with this label, cut in place of the outside song the act
			// was about to record. Injected after the label's own choice, so its draws are unchanged; a world with no player
			// holds no placements and never takes this branch.
			string placementId = null;
			if (format == ReleaseFormat.Single && PlayerDesk.Instance != null
					&& PlayerDesk.Instance.PlacedSongFor(label, artist, record, material, year, out placementId) is { } placed)
				{ material = placed; AiRepertoireService.ReleasePick(record); }
			SongMaterialApplicationService.Apply(record, material, label, artist);
			if (placementId != null) PlayerDesk.Instance.StampPlacedCut(record, placementId);
			if (record.format == ReleaseFormat.Single) SongMaterialSelectionService.ObserveMasterAssignment(artist, record, material, "single");
		} else {
			CompositionCatalogService.AttachArtistOriginal(record, artist, label, year);
		}

		// The session happens when the record is made: hire the crew now, so it can shape the record.
		var crew = SessionEmploymentService.OnRecordMade(record, label, artist);
		// Everyone who played on it now knows the song (repertoire influence).
		RepertoireInfluenceService.OnRecordMade(record, artist, crew);
		return record;
	}

	private Album GenerateAlbum(AILabel label, SimulatedArtist artist, int year, string albumRecordId = null, Genre secondaryGenre = Genre.TraditionalPop) {
		bool useStructuredPromoTracks = PolarSongBehavior.UsePolarFitSelection || (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		float artistTalent = artist.CalculateBaseQuality();
		float luckyRoll = GameRandom.Randf();
		float cohesionCeiling = AlbumModel.GetMaximumAchievableCohesion(year, artistTalent, label.productionQuality, luckyRoll);
		float thematicCohesion = Mathf.Clamp((float)GameRandom.RandRange(0.10, cohesionCeiling), 0f, cohesionCeiling);

		AlbumFormat albumFormat;
		bool statementViable = cohesionCeiling >= 0.72f && thematicCohesion >= 0.62f;
		if (statementViable && year >= AlbumModel.EarlyStatementYear && GameRandom.Randf() < 0.24f) {
			albumFormat = AlbumFormat.Concept;
			thematicCohesion = Mathf.Max(thematicCohesion, 0.68f);
		} else if (GameRandom.Randf() < AlbumModel.GetCompilationChance(artist.primaryGenre, year)) {
			albumFormat = AlbumFormat.Compilation;
		} else {
			// SOUNDTRACK ORIGINATION (2026-08, D7 soundtrack subsystem): the old cosmetic
			// `typeRoll < 0.12f ? Soundtrack` branch was removed. A soundtrack/cast album is an
			// externally-originated object (film score, stage cast, tie-in) with its own demand
			// curve and economics -- it must NOT be a dice roll on an ordinary artist album. The
			// former 12% Soundtrack band folds into Standard; Live keeps its ~12% band. Real
			// soundtracks are minted by ExternalMediaService (see D7SoundtrackCastAlbumHandoff.md).
			float typeRoll = GameRandom.Randf();
			albumFormat = typeRoll < 0.12f ? AlbumFormat.Live : AlbumFormat.Standard;
		}

		var referencedSingles = new List<AlbumTrack>();
		if (albumFormat == AlbumFormat.Compilation) {
			foreach (string recordId in artist.releasedSingleIds.AsEnumerable().Reverse()) {
				if (referencedSingles.Count >= 4) break;
				if (ChartManager.Instance.TryResolveTrackSnapshot(recordId, out AlbumTrack track, out _)) referencedSingles.Add(track);
			}
		}

		int targetTracks = (int)GameRandom.RandRange(9, 13);
		var nonSingleTracks = new List<AlbumTrack>();
		float originalMaterialScale = albumFormat == AlbumFormat.Compilation ? 0.68f : albumFormat == AlbumFormat.Live ? 0.80f : 0.88f;
		// How unevenly this family's albums are put together in this year. A jazz LP in 1960 is
		// already a body of work; a pop LP in 1960 is a hit with filler around it, and rock
		// travels from the second to the first across the decade. Scales the spread only -- the
		// draw count is unchanged, so the RNG stream is untouched.
		float trackSpread = AlbumModel.GetTrackSpreadMultiplier(GenreCatalog.Get(artist.primaryGenre).Family, year);
		int albumChartWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		// AlbumMaterialPlan (§15): give the LP a coherent source strategy -- a cohesive record concentrates
		// on one source, a hits-and-filler record stays spread -- instead of rolling each track blind. The
		// plan comes from the same calibrated decade prior as the singles, so the aggregate is preserved;
		// cohesion only reshapes WITHIN the album. Deterministic (no GD); album tracks never settle, so this
		// is inert to the economy. Empty pools fall back to the free mix inside ChooseMaterial.
		SongMaterialSource[] planSlots = null;
		int nonSingleTarget = Mathf.Max(0, targetTracks - referencedSingles.Count);
		if (AlbumMaterialPlanner.Enabled && SongMaterialSelectionService.Enabled && albumRecordId != null && nonSingleTarget > 0) {
			AlbumMaterialPlan plan = AlbumMaterialPlanner.Plan(artist.primaryGenre, year, thematicCohesion, nonSingleTarget, artist.artistId, albumRecordId);
			planSlots = AlbumMaterialPlanner.ExpandSlots(plan, artist.artistId, albumRecordId);
		}
		for (int attemptedSlot = 0; attemptedSlot < nonSingleTarget; attemptedSlot++) {
			float trackQuality = Mathf.Clamp(artistTalent * originalMaterialScale + label.productionQuality * 0.12f
				+ (float)GameRandom.RandRange(-0.16 * trackSpread, 0.12 * trackSpread), 0.12f, 0.95f);
			string trackTitle = NameGenerator.Instance?.GenerateSongTitle(artist.primaryGenre, year, artist.artistId) ?? $"Album Track {nonSingleTracks.Count + 1}";
			(float hook, float production, float dance) = useStructuredPromoTracks
				? GetDeterministicTrackTraits(trackQuality, trackTitle, artist.primaryGenre)
				: (0f, 0f, 0f);
			if (useStructuredPromoTracks && AlbumSessionProduction.Enabled) {
				float sessionProduction = AlbumSessionProduction.ForTrack(production, label, artist, albumRecordId, attemptedSlot);
				// Keep LP track appeal consistent with the recording-component delta, as material realization does below.
				trackQuality = Mathf.Clamp(trackQuality + PolarSongTable.Current.N("recordingQualityProductionWeight")
					* (sessionProduction - production), 0, 1);
				production = sessionProduction;
			}
			var track = new AlbumTrack {
				title = trackTitle,
				genre = artist.primaryGenre,
				quality = trackQuality,
				hookStrength = hook, productionQuality = production, danceability = dance,
				isReleasedSingle = false
			};
			// Publishing & Cover-Song §15: give every album cut a composition origin. Selection is the
			// same pure stable hash as the singles path (never GD), keyed on a per-track synthetic id so
			// tracks diverge; it stamps identity ONLY and leaves quality/hook/production/danceability
			// untouched, so album pooledAppeal and promo-single selection are byte-identical.
			if (SongMaterialSelectionService.Enabled && albumRecordId != null) {
				var trackKey = new Record {
					recordId = $"{albumRecordId}_t{attemptedSlot}",
					primaryGenre = artist.primaryGenre, secondaryGenre = secondaryGenre, title = trackTitle,
					hookStrength = hook, danceability = dance,
					originality = Mathf.Clamp((artist.members.Count > 0 ? artist.members.Max(m => m.creativity) : 0.4f) * 0.7f + 0.15f, 0f, 1f)
				};
				SongMaterialSource? forcedSource = planSlots != null && attemptedSlot < planSlots.Length
					? planSlots[attemptedSlot] : (SongMaterialSource?)null;
				SelectedSongMaterial trackMaterial = SongMaterialSelectionService.ChooseMaterial(
					label, artist, trackKey, artist.primaryGenre, year, albumChartWeek, forcedSource);
				if (PolarSongBehavior.UsePolarFitSelection && trackMaterial == null) continue;
				SongMaterialApplicationService.ApplyIdentityToAlbumTrack(track, trackMaterial);
				PolarSongMetadataService.RegisterTrack(track, trackMaterial?.Song, trackKey.recordId, artist.artistId,
					TimeManager.Instance?.CurrentDate ?? default, proposal: PolarSongBehavior.UsePolarFitSelection ? trackMaterial?.PolarProposal : null);
				SongMaterialSelectionService.ObserveMasterAssignment(artist, trackKey, trackMaterial, "albumTrack");
			}
			nonSingleTracks.Add(track);
		}

		float avgTrackMinutes = (float)GameRandom.RandRange(2.45, year >= 1967 ? 4.10 : 3.35);
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
			// Diagnostics. thematicCohesion is a draw from [0.10, ceiling], so neither the era
			// ramp's reach nor the pioneer bar's hit rate is recoverable from it after the fact.
			cohesionCeiling = cohesionCeiling,
			statementExcellence = AlbumModel.GetStatementExcellence(artistTalent, label.productionQuality),
			packaging = Mathf.Clamp(label.productionQuality * Mathf.Lerp(0.35f, 0.85f, AlbumModel.GetAlbumEraWeight(year)) + (float)GameRandom.RandRange(-0.10, 0.12), 0.05f, 1f),
			isStereo = year >= 1968 || GameRandom.Randf() < Mathf.Lerp(0.12f, 0.75f, Mathf.Clamp((year - 1960f) / 8f, 0f, 1f))
		};
		IEnumerable<float> qualities = referencedSingles.Select((track, index) => track.quality * referencedFreshness[index])
			.Concat(nonSingleTracks.Select(track => track.quality));
		album.pooledAppeal = AlbumModel.CalculatePooledAppeal(qualities, album.thematicCohesion, year);
		// Integrity reads UNDECAYED quality. Freshness is a statement about how much commercial
		// life a side has left, not about how good it is, and a record does not stop being a
		// body of work because two of its songs were hits first -- Rubber Soul and Pet Sounds
		// both carried singles. What distinguishes them is that the rest of the record is as
		// strong, which is exactly what the mean-against-peak ratio measures.
		album.bodyOfWork = AlbumModel.GetAlbumIntegrity(
			referencedSingles.Select(track => track.quality).Concat(nonSingleTracks.Select(track => track.quality)));
		return album;
	}

	private static string GenerateAlbumTitle(Record record, int year) {
		// The tracklist is already built (GenerateAlbum ran before this), so we can designate a lead
		// single and route through the genre album sets — "Music for Lovers", "Meet The Ravens",
		// lead-single-titled — instead of borrowing the song-title generator (doc C).
		//
		// The name and the key are passed SEPARATELY here. artistName fills the title slots and is
		// what a self-titled album is called; artistId is the near-duplicate bucket key, matching
		// the re-key applied to GenerateSongTitle. Collapsing the two -- passing the id for both,
		// as the song-title call does -- would ship albums called artist_04047.
		string leadSingle = DesignateLeadSingleTitle(record.album);
		string generated = NameGenerator.Instance?.GenerateAlbumTitle(record.primaryGenre, year, record.artistName,
			false, leadSingle, record.artistId);
		return string.IsNullOrWhiteSpace(generated) ? $"{record.artistName} Album" : generated;
	}

	/// <summary>Pick the track whose title frames the album: a referenced hit for a compilation, else
	/// the strongest original. Titling only — does not release a single (that is the promo pipeline).</summary>
	private static string DesignateLeadSingleTitle(Album album) {
		if (album == null) return null;
		if (album.trackRefs != null && album.trackRefs.Length > 0) return album.trackRefs[0].title;
		if (album.nonSingleTracks != null && album.nonSingleTracks.Length > 0) {
			var best = album.nonSingleTracks[0];
			foreach (var t in album.nonSingleTracks) if (t.quality > best.quality) best = t;
			return best.title;
		}
		return null;
	}

	private Record CreatePromoSingleFromAlbum(Record albumRecord, SimulatedArtist artist, AILabel label, int year) {
		Album album = albumRecord.album ?? throw new System.InvalidOperationException("Promo project requires a generated album.");
		// Cast/film originals are album-only. An artist's separately recorded interpretation
		// uses normal material selection and never extracts the external-media master.
		if (album.albumFormat == AlbumFormat.Soundtrack && album.externalMedia != null) return null;
		if (PolarSongBehavior.UsePolarFitSelection && (album.nonSingleTracks == null || album.nonSingleTracks.Length == 0)) return null;
		if (album.nonSingleTracks == null || album.nonSingleTracks.Length == 0) throw new System.InvalidOperationException("Promo project album has no eligible original track.");
		bool useStructuredPromoTracks = PolarSongBehavior.UsePolarFitSelection || (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true);
		int bestIndex = FindAlbumSingleTrackIndex(albumRecord, artist, year);
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
		// A promo single is a chart single like any other -- give it composition origin so it obeys the
		// material mix (without this, ~38% of runtime singles bypassed the song layer). Selection is a
		// pure stable hash (no GD draw), so the album-project RNG stream is unchanged.
		if (SongMaterialSelectionService.Enabled && artist != null && PolarSongBehavior.UsePolarFitSelection && !PolarSongBehavior.AuditLegacyPromotion && CompositionCatalogService.GetSong(source.songId) is SongComposition sourceSong) {
			// Promotion reuses the album performance; no second material selection or execution penalty.
			promo.masterId = source.masterId;
			CompositionCatalogService.ApplyToRecord(promo, sourceSong, source.songSource, source.isCover,
				source.originalRecordId, source.originalArtistId, source.songFamiliarityAtRelease, source.arrangementOriginality, 0);
		} else if (SongMaterialSelectionService.Enabled && artist != null) {
			int promoChartWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			SelectedSongMaterial promoMaterial = SongMaterialSelectionService.ChooseMaterial(label, artist, promo, promo.primaryGenre, year, promoChartWeek);
			SongMaterialApplicationService.Apply(promo, promoMaterial, label, artist);
		}
		var remaining = album.nonSingleTracks.ToList();
		remaining.RemoveAt(bestIndex);
		var refs = album.trackRefs?.ToList() ?? new List<AlbumTrack>();
		refs.Add(new AlbumTrack {
			sourceRecordId = promo.recordId, masterId = promo.masterId, title = source.title, genre = source.genre,
			quality = source.quality,
			hookStrength = useStructuredPromoTracks ? hook : 0f,
			productionQuality = useStructuredPromoTracks ? production : 0f,
			danceability = useStructuredPromoTracks ? dance : 0f,
			isReleasedSingle = true, peakPosition = 0,
			// The promo already ran material selection; mirror its song identity onto the album trackRef
			// so the on-album representation carries the same composition origin (§15).
			songId = promo.songId, songSource = promo.songSource, isCover = promo.isCover,
			originalRecordId = promo.originalRecordId, originalArtistId = promo.originalArtistId,
			publisherId = promo.publisherId, songwriterNames = promo.songwriterNames,
			compositionQuality = promo.compositionQuality, compositionHook = promo.compositionHook,
			lyricQuality = promo.lyricQuality, songFamiliarityAtRelease = promo.songFamiliarityAtRelease,
			standardDurability = promo.standardDurability, arrangementOriginality = promo.arrangementOriginality
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
		// Lifting a promo single off the record changes its track list, so the body-of-work
		// reading is retaken. Pulling a hit single off an album must not, by itself, stop it
		// being an album.
		album.bodyOfWork = AlbumModel.GetAlbumIntegrity(
			album.trackRefs.Select(track => track.quality).Concat(album.nonSingleTracks.Select(track => track.quality)));
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
				awarenessRandom = (float)GameRandom.RandRange(0.8, 1.1),
				radioRandom = 1f,
				sentimentRandom = (float)GameRandom.RandRange(-0.1, 0.15)
			});
		}
		return snapshot;
	}

	private void ApplyPromotionSnapshot(Record record, AILabel label, float marketingBudget, PromotionSnapshot snapshot,
		float awarenessBonus, float stockMultiplier, bool trackPromoShadow = false) {
		RecordRuntimeData runtime = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtime == null || snapshot == null) return;
		float quality = runtime.GetQuality();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float baseAwareness = 0.04f + snapshot.artistAwareness + marketingAwareness + label.reputation * 0.1f;
		runtime.awareness = Mathf.Clamp(baseAwareness + awarenessBonus, 0f, 1f);
		// A5: the same launch without the promo's awareness bonus.
		if (trackPromoShadow) { runtime.promoShadowAwareness = Mathf.Clamp(baseAwareness, 0f, 1f); runtime.promoShadowDemand = 0d; }
		runtime.radioHeat = 0f;
		// Phase C: same stock-cliff replacement as the single path. The snapshot carries only
		// careerState, so recognition reads from the live artist; the promo-synergy stockMultiplier
		// applied downstream is untouched.
		SimulatedArtist albumArtist = ArtistManager.Instance?.GetArtist(record.artistId);
		float careerStockScale = ArtistRecognition.Enabled && albumArtist != null
			? 1f + ArtistRecognitionService.LaunchRecognition(albumArtist) * ArtistRecognitionService.MaxStockBonus
			: snapshot.careerState switch {
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
			if (runtime.promoShadowAwareness is float shadowAwareness)
				data.promoShadowAwareness = Mathf.Clamp(shadowAwareness * regionStrength * regional.awarenessRandom, 0f, 1f);
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
		ArtistRecognitionService.RecordLaunchAudit(ArtistManager.Instance?.GetArtist(record.artistId), runtime);
	}

	private void ProcessDueAlbumProjects(GameDate date) {
		var profileStart = SimulationPerformanceProfiler.Begin();
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
			if (AlbumLaunchReserve.Enabled) {
				float reserve = (EconomicLabelLifecycle.Enabled && !owner.isPlayerOwned
					? EconomicLabelLifecycle.CommitmentReserve(owner, signing: false) : owner.GetMonthlyOverhead())
					+ InitialRunReserve(owner, project.albumRecord);
				marketingBudget = AlbumLaunchReserve.ClampCampaign(marketingBudget, owner.cashReserves, reserve);
			} else {
				float available = Mathf.Max(0f, owner.cashReserves - owner.GetMonthlyOverhead()
					- (float)LabelCorporateModel.TaxAndServiceReserve(owner));
				marketingBudget = Mathf.Min(marketingBudget, available * 0.8f);
			}
			LabelCompanyAccounts.ChangeCash(owner, -marketingBudget, LabelCashKind.Campaign);
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
				project.synergyAwarenessApplied, project.synergyStockMultiplier, trackPromoShadow: project.promoFeedback != null);
			RecordRuntimeData albumRuntime = ChartManager.Instance.GetRecordRuntimeData(project.albumRecord.recordId);
			AttributeReleaseCampaign(albumRuntime, marketingBudget);
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
	internal bool HasOpenFounderCommitments(string labelId) => albumProjects.Concat(pendingAlbumProjects)
		.Any(p => p.currentLabelId == labelId && p.terminalState == AlbumProjectTerminalState.PendingAtAuditEnd)
		|| aiLabels.Any(l => l.aiMasterLeases?.Any(lease => lease.BuyerLabelId == labelId) == true);
	public AlbumProject GetAlbumProject(string projectId) => !string.IsNullOrEmpty(projectId) &&
		projectById.TryGetValue(projectId, out AlbumProject project) ? project : null;
	public bool HasPendingProjectForArtist(string artistId) => pendingAlbumProjects.Any(project => project.artistId == artistId);
	
	private void ApplyReleasePromotion(Record record, SimulatedArtist artist, AILabel label, float marketingBudget, float perceivedQualityMult) {
		var runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return;
		AttributeReleaseCampaign(runtimeData, marketingBudget);
		bool isAlbum = record.format == ReleaseFormat.Album;
		
		float quality = runtimeData.GetQuality();
		float artistAwareness = artist.GetNewReleaseAwarenessBonus();
		float marketingAwareness = GetSeasonalMarketingImpact(marketingBudget, label);
		float labelAwareness = label.reputation * 0.1f;
		// --hit-act-cadence (§17y): a single launched while the act already has singles on the Hot 100 shares the act's
		// airplay with them (push and recognition radio), and cools theirs. Fans still buy it: awareness and stock are
		// whole. share is exactly 1 when off or when nothing else by the act is charting.
		float share = 1f;
		if (HitActCadence.SplitEnabled && !isAlbum) {
			List<RecordRuntimeData> predecessors = HitActCadence.ChartingPredecessors(artist.artistId, record.recordId);
			share = HitActCadence.LaunchShare(predecessors.Count);
			HitActCadence.CutPredecessors(predecessors);
		}

		runtimeData.awareness = Mathf.Clamp((isAlbum ? 0.04f : 0.08f) + artistAwareness + marketingAwareness + labelAwareness, 0f, 1f);

		float baseRadio = quality * 0.3f;
		// LaunchPushSaturation is a probe on the launch reads of campaign impact only; identity unless named.
		float pushRadio = LaunchPushSaturation.Apply(ChartSimulator.GetCampaignImpact(label)) * 0.3f;
		float payolaRadio = label.payolaWillingness * 0.15f;
		// Phase D: a small programmer-confidence lift for recognized acts. Singles only, weaker than
		// the awareness lift, and it rides the same /radioDifficulty regional divide downstream.
		// Airplay sits inside a high exponent ([[airplay-convexity-amplifies-label-push]]), so this
		// is the smallest of the three ceilings on purpose. Off: byte-identical.
		float recognitionRadio = ArtistRecognition.Enabled
			? ArtistRecognitionService.LaunchRecognition(artist) * ArtistRecognitionService.MaxRadioLift : 0f;
		if (share < 1f) { pushRadio *= share; recognitionRadio *= share; }
		if (RecordCampaignSpend.Enabled && !isAlbum && !record.isPlayerOwned && !label.isPlayerOwned) {
			float campaignFactor = RecordCampaignSpend.LaunchFactor(marketingBudget, label.GetStandardMarketingBudget());
			runtimeData.campaignPushFactor = campaignFactor;
			runtimeData.campaignTranches = 0;
			pushRadio *= campaignFactor;
		}
		runtimeData.radioHeat = isAlbum ? 0f : Mathf.Clamp(baseRadio + pushRadio + payolaRadio + recognitionRadio, 0f, 1f);
		
		var regions = ChartManager.Instance.GetAllRegions();
		// Phase C: the discrete careerState stock cliff (2.5x for the ~92 Star+ acts) is replaced
		// by a continuous recognition multiplier capped at ~1.3x that any act climbs to over time,
		// not a cliff five acts stand on (directive §0/§3). Off: byte-identical careerState switch.
		float stockScale = ArtistRecognition.Enabled
			? 1f + ArtistRecognitionService.LaunchRecognition(artist) * ArtistRecognitionService.MaxStockBonus
			: artist.careerState switch {
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
			regionalData.awareness = Mathf.Clamp(runtimeData.awareness * regionStrength * (float)GameRandom.RandRange(0.8, 1.1), 0f, 1f);
			float radioDifficulty = ChartSimulator.GetRadioDifficulty(region);
			float genreRadio = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true
				? GenreAcceptanceService.GetRegionalRadioOpportunity(record.primaryGenre, record.secondaryGenre, region,
					TimeManager.Instance?.CurrentDate.year ?? 1960, ChartManager.Instance?.GetGenreMomentum(record.primaryGenre) ?? 0f)
				: 1f;
			if (MarketSeasonality.Enabled) {
				float radioOpportunity = MarketSeasonality.GetRadioOpportunity(TimeManager.Instance?.CurrentDate.year ?? 1960,
					TimeManager.Instance?.CurrentDate.month ?? 1, liveTick: true);
				regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GameRandom.RandRange(0.7, 1.0) * radioOpportunity * genreRadio, 0f, 1f);
			} else regionalData.radioPlay = isAlbum ? 0f : Mathf.Clamp(runtimeData.radioHeat * regionStrength / radioDifficulty * (float)GameRandom.RandRange(0.7, 1.0) * genreRadio, 0f, 1f);
			float genreFit = GetGenreFit(record.primaryGenre, region);
			regionalData.sentiment = Mathf.Clamp((quality * 0.6f) + (genreFit * 0.3f) + (float)GameRandom.RandRange(-0.1, 0.15), -1f, 1f);
		}
		IReadOnlyDictionary<string, int> allocatedStock = ChartSimulator.RedistributeInitialRegionalStockAllocation(record.primaryGenre,
			TimeManager.Instance?.CurrentDate.year ?? 1960, GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true,
			regions, initialStock);
		foreach (MarketRegion region in regions) {
			if (runtimeData.regionalData.TryGetValue(region.regionId, out RegionalRecordData data))
				data.unitsInStores = allocatedStock.GetValueOrDefault(region.regionId);
		}

		// The player gets no free national distribution. A player record reaches a region's shops only where
		// the label has actually placed a wholesale line (the house presses stock there); everywhere else --
		// the home market included -- it sells out of the trunk, town by town, settled in PlayerDesk, not by
		// the weekly store engine. So drop the auto-seeded inventory in every region with no line. Without
		// this the player's record was distributed nationwide like an AI release and the engine sold (and
		// skimmed) hundreds of units the player never pressed. Inert for AI records (never player-owned).
		if (record.isPlayerOwned) {
			foreach (MarketRegion region in regions)
				if (runtimeData.regionalData.TryGetValue(region.regionId, out RegionalRecordData data)
					&& !label.HasDistributionInRegion(region.regionId))
					data.unitsInStores = 0;
		}

		if (LaunchStocking.AppliesToLaunch(record))
			foreach (RegionalRecordData data in runtimeData.regionalData.Values) data.unitsInStores = LaunchStocking.ScaleLaunch(data.unitsInStores);

		runtimeData.initialLaunchAwareness = runtimeData.awareness;
		runtimeData.initialLaunchStock =runtimeData.regionalData.Values.Sum(data => data.unitsInStores);
		runtimeData.launchCareerState = artist.careerState;
		runtimeData.perceivedQualityMultiplier = perceivedQualityMult;
		if (ChartManager.Instance?.IsGenreMarketV2Live == true && TimeManager.Instance != null)
			RegionalAudience.ApplyLaunchDeposit(runtimeData, artist, TimeManager.Instance.CurrentDate);
		if (ChartManager.Instance?.IsGenreMarketV2Live == true) RegionalAudience.ApplyLaunchProbe(runtimeData, label);
		if (ChartManager.Instance?.IsGenreMarketV2Live == true) RegionalAudience.HoldUntilArrival(runtimeData, label, artist);
		ArtistRecognitionService.RecordLaunchAudit(artist, runtimeData);

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
		float campaignImpact = LaunchPushSaturation.Apply(ChartSimulator.GetCampaignImpact(label));
		if (!MarketSeasonality.Enabled) return BudgetToImpact(marketingBudget, label.tier) * campaignImpact * 0.35f;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int month = TimeManager.Instance?.CurrentDate.month ?? 1;
		float impact = BudgetToImpact(marketingBudget, label.tier) *
			MarketSeasonality.GetMarketingEfficiencyMultiplier(year, month, liveTick: true);
		return Mathf.Clamp(impact, 0f, 1f) * campaignImpact * 0.35f;
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
		// --album-scale-career-milestones: an album's peak on the album chart's own scale; the raw peak otherwise.
		int peak = AlbumScaleCareerMilestones.ObserveCareerPeak(record);
		if (peak >= 1 && peak <= 100) chartedLabelIds.Add(record.baseRecord.labelId);
		if (peak > 0 && peak <= 40 &&
			creditedLabelTop40RecordIds.Add(record.baseRecord.recordId)) label.top40Hits++;
		if (peak == 1 &&
			creditedLabelNumberOneRecordIds.Add(record.baseRecord.recordId)) label.numberOneHits++;
	}
	
	private void OnMonthChanged(GameDate date) {
		using var timingScope = SimulationPerformanceProfiler.Measure(SimulationPerformanceProfiler.Phase.CompetitorMonth);
		foreach (var label in aiLabels) ProcessLabelMonth(label, date);
		// All companies close first. Owner transfers occur on the first day of the next period,
		// so receiving parents do not get different accounting periods depending on list order.
		if (LabelCorporateModel.Distributions)
			LabelCorporateModel.PayAllDistributions(aiLabels, LabelCorporateModel.Period(date) - 1, GetLabel);
		if (date.month == 1) ProcessIndependentTradeDecline(date.year);
		ProcessDistributionDeals(date);
		if (debugMode) PrintMonthlyReport(date);
	}
	
	private void ProcessLabelMonth(AILabel label, GameDate date) {
		if (LabelCorporateModel.Applies(label) && label.company?.lastClosedPeriod >= LabelCorporateModel.Period(date) - 1) return;
		if (!label.IsActive) {
			ReleaseIndependentDistribution(label);
			if (label.company?.accountsOpened == true) LabelCompanyAccounts.CloseMonth(label, LabelCorporateModel.Period(date) - 1);
			return;
		}

		var financials = labelFinancials.TryGetValue(label.labelId, out var f) ? f : null;
		if (financials == null) {
			financials = new LabelFinancialHistory();
			labelFinancials[label.labelId] = financials;
		}
		float overhead = label.GetMonthlyOverhead();
		LabelCompanyAccounts.OverheadPaid(label, overhead);
		label.monthlyExpenses += overhead;
		financials.lastMonthExpenses += overhead;
		financials.lastMonthExpenses += FounderCompensation.Pay(label);
		// Owner-manager work is paid from positive cash surplus after ordinary bills.
		// This is an operating expense, separate from a record's unit contribution.
		financials.lastMonthExpenses += LabelOperatorPay.Pay(label, financials.lastMonthRevenue - financials.lastMonthExpenses);
		
		float netIncome = financials.lastMonthRevenue - financials.lastMonthExpenses;
		label.lastMonthlyProfit = netIncome;
		float cashBeforeExpansion = label.cashReserves;
		ReinvestDistributionProfit(label, netIncome);
		GrowSelfBuiltDistributionReach(label, netIncome);
		PursueIndependentDistribution(label);
		float netCashFlow = netIncome + label.cashReserves - cashBeforeExpansion;
		// After pay and reinvestment decisions, so a distress sale funds neither; its proceeds are
		// kept out of the operating cash flow the solvency review and commitment reserve read.
		float distressProceeds = AiMasterDeals.TryDistressSale(this, label, date);
		// --studio-parent-labels: the parent tops cash up to the commitment reserve before the books close. It is an
		// owner transfer, so the operating cash flow the solvency review reads still shows the label's own losses.
		StudioParentLabels.SupportMonth(label, aiLabels, date);
		StudioParentLabels.ReviewYear(label, date);
		if (LabelCorporateModel.Applies(label)) {
			int period = LabelCorporateModel.Period(date) - 1;
			int clients = aiLabels.Count(client => client.IsActive && client.activeDeal?.distributorId == label.labelId);
			LabelCorporateModel.ReviewOrganization(label, period, clients, GetRecentReleasedRecordCount(label.labelId, 13) / 3.0);
			var statement = LabelCompanyAccounts.CloseMonth(label, period);
			if (statement != null && LabelCorporateModel.ChangesEconomy) netCashFlow = (float)(statement.closingCash - statement.openingCash - statement.financingNet - statement.ownerTransfersNet) - distressProceeds;
		}
		UpdateLabelStatus(label, financials, netIncome, netCashFlow);
		
		label.monthlyRevenue = 0f;
		label.monthlyExpenses = 0f;
		financials.lastMonthRevenue = 0f;
		financials.lastMonthExpenses = 0f;
		
		if (date.month == 1) financials.totalReleasesThisYear = 0;
	}
	internal void ProcessLabelMonthForCompanyChecks(AILabel label, GameDate date) => ProcessLabelMonth(label, date);

	public void SetDistributionOfferProcessingEnabled(bool enabled) => distributionOfferProcessingEnabled = enabled;

	// Test-only control for the forced-deal integration harness: force this client's
	// next expiry to absorb, bypassing the consolidation gate and roll.
	public void ForceConsolidationForTest(string labelId) {
		if (!string.IsNullOrEmpty(labelId)) forcedConsolidationClients.Add(labelId);
	}

	private void ReinvestDistributionProfit(AILabel label, float netIncome) {
		if (label.activeDeal == null || netIncome <= 0f) return;
		float reinvestment = netIncome * dealReinvestRate;
		if (LabelCorporateModel.Capital && LabelCorporateModel.Applies(label)) {
			reinvestment = Mathf.Min(reinvestment, (1f - label.ownedReach) * Mathf.Max(1f, dealReinvestCost));
			reinvestment = Mathf.Min(reinvestment, (float)System.Math.Max(0, label.cashReserves - LabelCorporateModel.ProtectedCash(label)));
		}
		LabelCompanyAccounts.ChangeCash(label, -reinvestment, LabelCashKind.DistributionInvestment);
		LabelCorporateModel.AddAsset(label, "distribution network", reinvestment, 120);
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
		if (LabelCorporateModel.Capital && LabelCorporateModel.Applies(label) &&
			label.cashReserves - LabelCorporateModel.ProtectedCash(label) < investment) return;
		LabelCompanyAccounts.ChangeCash(label, -investment, LabelCashKind.DistributionInvestment);
		LabelCorporateModel.AddAsset(label, "distribution network", investment, 120);
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
				SettleEndedDistributionAdvance(client, deal, DealResolution.ClientClosed);
				deal.unrecoupedAdvance = 0f;
				EmitDealEvent(client, distributor, deal, DealResolution.ClientClosed, client.DistributionDependency);
				client.activeDeal = null;
				continue;
			}
			if (distributor == null || !distributor.IsActive) {
				SettleEndedDistributionAdvance(client, deal, DealResolution.DistributorCollapsed);
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
		// Directive §9: this whole loop signs a deal on the client's behalf with no dialogue --
		// ShouldAcceptDeal is a coin flip, not a choice. That's fine for the AI economy; for the
		// player it silently attaches an advance-and-masters contract behind their back, the exact
		// opposite of "player buttons, not an AI deal brain rewrite." The player's own front door is
		// PursueDistributionDeal below.
		if (client.isPlayerOwned) return;
		// --studio-parent-labels: a studio's label is its parent's division, not a P&D client.
		if (client.IsSubsidiary || !CanSignDistributionDeal(client.tier) || StudioParentLabels.IsStudioLabel(client)) return;
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
		// every month, so drawing it from GameRandom.Randf would reorder every downstream sampler and
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
			ChartManager.Instance?.GetRecordsForLabel(client.labelId), client.labelId, client.strongRegions, regionalBreakoutDealThreshold);
		offer.Cover(evidence.EarningRecordId);
		if (dropped != null) {
			// The dropped imprint keeps what a completed term leaves behind, exactly as an
			// ordinary exit does, and is free to place its line with wholesale houses instead.
			DistributionDeal endedDeal = dropped.activeDeal;
			dropped.ownedReach = Mathf.Min(1f, dropped.ownedReach + (endedDeal.reachGranted * 0.50f));
			SettleEndedDistributionAdvance(dropped, endedDeal, DealResolution.Dropped);
			EmitDealEvent(dropped, major, endedDeal, DealResolution.Dropped, dropped.DistributionDependency);
			dropped.activeDeal = null;
		}
		BuyOutDistributionAdvance(client, current, incumbent, offer, major);
		EmitDealEvent(client, incumbent, current, DealResolution.Poached, client.DistributionDependency);
		client.activeDeal = offer;
		LabelCompanyAccounts.ChangeCash(client, offer.advance, LabelCashKind.Financing);
		LabelCompanyAccounts.ChangeCash(major, -offer.advance, LabelCashKind.Financing);
		EmitDealEvent(client, major, offer, DealResolution.Signed, client.DistributionDependency);
	}

	private void TryGenerateDistributionOffer(AILabel client, int year, int currentWeek) {
		// A subsidiary already distributes through the parent's national network and does not
		// sign its own deals.
		if (client.IsSubsidiary) return;
		// Directive §9: the player's label sits in aiLabels like any other (RegisterLabel), so
		// without this guard the exact same push/pull rolls below could silently sign a P&D deal --
		// advance banked, masters possibly gone -- with no dialogue at all. "MajorPD ... player front
		// door only" means the player walks in (PursueDistributionDeal); nobody walks in on them.
		if (client.isPlayerOwned) return;
		if (!CanSignDistributionDeal(client.tier) || StudioParentLabels.IsStudioLabel(client)) return;

		RegionalDealEvidence regionalEvidence = EvaluateRegionalDealEvidence(
			ChartManager.Instance?.GetRecordsForLabel(client.labelId), client.labelId, client.strongRegions, regionalBreakoutDealThreshold);
		// A scalar national-awareness estimate is not a physical network. The old
		// <0.40 test excluded every otherwise-qualified runtime Independent above
		// that arbitrary line even when a distributor still had six new regions to
		// offer. SelectDistributor already enforces the real boundary: at least one
		// distributor region the client does not currently cover.
		bool pullTrigger = IsPullDealTrigger(client, regionalEvidence);
		float pushChance = monthlyPushOfferProbability +
			(Mathf.Max(0, year - consolidationCourtingRampStartYear) * annualCourtingRampPerYear);
		bool pushEvidence = client.momentumScore > 0.60f || HasRecentTop40Record(client);
		bool pushTrigger = pushEvidence && GameRandom.Randf() < pushChance;
		bool pullOffer = pullTrigger && GameRandom.Randf() < monthlyPullOfferProbability;
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
		LabelCompanyAccounts.ChangeCash(client, offer.advance, LabelCashKind.Financing);
		LabelCompanyAccounts.ChangeCash(distributor, -offer.advance, LabelCashKind.Financing);
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
		// RegionalHitDealGate: evidence is a regional hit within the last few weeks, not the running peak, and the
		// deal carries the record that is selling now (highest current score among the qualifying ones).
		bool hitGate = RegionalHitDealGate.Enabled;
		string hitRecordId = null;
		float hitRecordScore = -1f;
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
				if (hitGate) {
					if (RegionalHitDealGate.IsRecent(pair.Value, record.weeksSinceRelease)) {
						recordHasPersistentEvidence = true;
						if (pair.Value.breakoutScore > hitRecordScore) {
							hitRecordScore = pair.Value.breakoutScore;
							hitRecordId = record.baseRecord.recordId;
						}
					}
				} else if (peak >= boundedThreshold) recordHasPersistentEvidence = true;
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
		if (hitRecordId != null) earningRecordId = hitRecordId;
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
		float roll = GameRandom.Randf() * total;
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
			? client.GetMonthlyOverhead() * (float)GameRandom.RandRange(6f, 12f)
			: (GameRandom.Randf() < 0.35f ? 0f : client.GetMonthlyOverhead() * (float)GameRandom.RandRange(0.5f, 2f));
		advance = Mathf.Min(advance, Mathf.Max(0f, distributor.cashReserves - (distributor.GetMonthlyOverhead() * 3f)));
		// Borrowed reach is worth the market the distributor's owned network actually
		// reaches. It was formerly an independent random draw, so a distributor owning
		// three regions could grant more national reach than one owning all seven.
		// Scaling the negotiated range by that coverage leaves a genuinely national
		// distributor at the authored terms and correctly discounts a partial one.
		float distributorCoverage = ChartManager.Instance?.GetNationalMarketShareForRegions(distributor.distributionRegions) ?? 1f;
		float negotiatedReach = push ? (float)GameRandom.RandRange(0.50f, 0.80f) : (float)GameRandom.RandRange(0.30f, 0.50f);
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
			marginSkim = push ? (float)GameRandom.RandRange(pushMarginSkimMin, pushMarginSkimMax) : (float)GameRandom.RandRange(pullMarginSkimMin, pullMarginSkimMax),
			ownsMasters = GameRandom.Randf() < mastersRate,
			advance = advance,
			unrecoupedAdvance = advance,
			signedWeek = currentWeek,
			termWeeks = push ? (int)GameRandom.RandRange(78, year >= 1967 ? 104 : 156) : (int)GameRandom.RandRange(52, 104),
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
		return GameRandom.Randf() < Mathf.Clamp(acceptance, 0.05f, 0.95f);
	}

	/// <summary>
	/// Directive §9: the player's front door into a P&amp;D distribution deal. TryGenerateDistributionOffer
	/// and TryPoachDistributedClient now skip the player's label entirely (see the isPlayerOwned guards
	/// on both) -- this is what replaces them: the same pull-route evidence bar
	/// (EvaluateRegionalDealEvidence / IsPullDealTrigger), the same distributor selection
	/// (SelectDistributor), and the same term generator (GenerateDealTerms) the AI itself uses.
	/// ShouldAcceptDeal is deliberately NOT called -- that coin flip is the client deciding whether to
	/// sign, and here the client is the player, so the decision goes to <see cref="SignDistributionDeal"/>
	/// instead. <paramref name="consulted"/> distinguishes "you don't qualify to even ask" (no hours
	/// should be spent finding that out) from "you made the rounds and nobody bit" (a real pitch that
	/// costs the sit-down either way) -- mirrors the anyHouseAvailable split in PlacePlayerLine (§5.1).
	/// </summary>
	public DistributionDeal PursueDistributionDeal(AILabel client, out bool consulted, out string message) {
		consulted = false;
		message = null;
		if (client == null) { message = "No label."; return null; }
		if (client.activeDeal != null) { message = "You're already under a distribution deal -- ride it out or let it lapse first."; return null; }
		if (client.IsSubsidiary || !CanSignDistributionDeal(client.tier)) {
			message = "You've outgrown a P&D contract -- distributors negotiate around you now, not for you.";
			return null;
		}

		RegionalDealEvidence evidence = EvaluateRegionalDealEvidence(
			ChartManager.Instance?.GetRecordsForLabel(client.labelId), client.labelId, client.strongRegions, regionalBreakoutDealThreshold);
		if (!IsPullDealTrigger(client, evidence)) {
			message = "Nobody's biting -- you don't have the regional proof to bring to this table yet.";
			return null;
		}

		consulted = true; // from here on you made the rounds -- it costs the sit-down whether or not anyone bites
		AILabel distributor = SelectDistributor(client, DealOrigin.LabelSought);
		if (distributor == null) {
			message = "Made the rounds -- nobody's got the room or the cash for you right now.";
			return null;
		}

		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		DistributionDeal offer = GenerateDealTerms(client, distributor, DealOrigin.LabelSought, year, currentWeek);
		offer.Cover(evidence.EarningRecordId);
		message = $"{distributor.labelName} will take you on -- {offer.marginSkim:P0} skim, {offer.termWeeks}-week term" +
			(offer.advance > 0f ? $", ${offer.advance:N0} advance" : ", no advance") +
			(offer.ownsMasters ? ", and they take the masters." : ", masters stay yours.");
		return offer;
	}

	/// <summary>Commits an offer <see cref="PursueDistributionDeal"/> handed back -- the player's own
	/// accept, standing in for the ShouldAcceptDeal roll the AI path uses on itself. Same side effects
	/// as the AI's own signing (TryGenerateDistributionOffer): advance changes hands, the deal event
	/// fires, and everything downstream (RouteDistributionSkim, ResolveDistributionDeal at term) picks
	/// the player up exactly like any other client from here -- no separate settlement path needed.</summary>
	public void SignDistributionDeal(AILabel client, DistributionDeal offer) {
		if (client == null || offer == null || client.activeDeal != null) return;
		AILabel distributor = GetLabel(offer.distributorId);
		client.activeDeal = offer;
		LabelCompanyAccounts.ChangeCash(client, offer.advance, LabelCashKind.Financing);
		if (distributor != null) LabelCompanyAccounts.ChangeCash(distributor, -offer.advance, LabelCashKind.Financing);
		EmitDealEvent(client, distributor, offer, DealResolution.Signed, client.DistributionDependency);
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
			SettleEndedDistributionAdvance(client, deal, DealResolution.Graduated);
			EmitDealEvent(client, distributor, deal, DealResolution.Graduated, dependency);
			client.activeDeal = null;
			return;
		}
		if (dependency < dealDependencyLow) {
			// Low dependency: the client has built enough of its own reach to leverage the
			// deal and stay independent (early Motown). It exits and keeps part of the reach.
			client.ownedReach = Mathf.Min(1f, client.ownedReach + (deal.reachGranted * 0.50f));
			SettleEndedDistributionAdvance(client, deal, DealResolution.Exit);
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
		return GameRandom.Randf() < consolidationAbsorbChance;
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
		SettleEndedDistributionAdvance(client, deal, DealResolution.Absorb);
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
		if (LabelCorporateModel.Applies(client)) LabelCorporateModel.Ensure(client).ownership = LabelOwnershipForm.CorporateDivision;
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
	
	private void UpdateLabelStatus(AILabel label, LabelFinancialHistory financials, float netIncome, float netCashFlow) {
		if (EconomicLabelLifecycle.Enabled && !label.isPlayerOwned) {
			financials.consecutiveLossMonths = netIncome < 0f ? financials.consecutiveLossMonths + 1 : 0;
			label.consecutiveLossMonths = financials.consecutiveLossMonths;
			EconomicLabelLifecycle.ReviewMonth(label, netCashFlow, enableBankruptcy);
			return;
		}
		if (netIncome < 0) financials.consecutiveLossMonths++;
		else financials.consecutiveLossMonths = Mathf.Max(0, financials.consecutiveLossMonths - 1);
		label.consecutiveLossMonths = financials.consecutiveLossMonths;
		
		if (label.cashReserves < bankruptcyThreshold) {
			// The player's own solvency/loss is owned by PlayerDesk (an overdraft credit line + a grace
			// period), so the AI's hard-bankruptcy never fires on the player label -- it would flip them to
			// an inactive dead state behind the game-over card. They still take the Dying/Struggling flavor.
			if (enableBankruptcy && financials.consecutiveLossMonths >= 6 && !label.isPlayerOwned) {
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
	
	public AILabel GetLabel(string labelId) {
		if (aiLabels == null) return null;
		if (labelId == null) return aiLabels.FirstOrDefault(l => l.labelId == null);
		// The first label with this id, as a linear search finds it. The list is shared with ChartManager and only ever
		// grows or is cleared and refilled on load, so the index is current while the list, its count and its end
		// entries are unchanged.
		int count = aiLabels.Count;
		if (!ReferenceEquals(labelIndexList, aiLabels) || labelIndexCount != count ||
			count > 0 && (!ReferenceEquals(labelIndexFirst, aiLabels[0]) || !ReferenceEquals(labelIndexLast, aiLabels[count - 1]))) {
			labelIndex.Clear();
			foreach (AILabel label in aiLabels) if (label.labelId != null) labelIndex.TryAdd(label.labelId, label);
			labelIndexList = aiLabels; labelIndexCount = count;
			labelIndexFirst = count > 0 ? aiLabels[0] : null; labelIndexLast = count > 0 ? aiLabels[count - 1] : null;
		}
		return labelIndex.TryGetValue(labelId, out AILabel found) ? found : null;
	}
	private readonly Dictionary<string, AILabel> labelIndex = new(System.StringComparer.Ordinal);
	private List<AILabel> labelIndexList;
	private int labelIndexCount;
	private AILabel labelIndexFirst, labelIndexLast;
	public IReadOnlyList<AILabel> GetAllLabels() => aiLabels ?? (IReadOnlyList<AILabel>)System.Array.Empty<AILabel>();

	// ========================================================================
	// FULL-WORLD SAVE -- the competitor economy. Album projects and independent distributors serialize whole;
	// their id-keyed indices and the Record object refs inside a project are relinked on load. Transient weekly
	// telemetry (weeklyRevenueByLabelAndFormat, weeklyReleaseLifecycleByTier) is not saved -- it re-accumulates.
	// ========================================================================

	/// <summary>Snapshots the competitor economy into the world save.</summary>
	public void CaptureEconomy(WorldSaveData w) {
		var c = new CompetitorSaveData {
			AlbumProjects = albumProjects.ToList(),
			PendingAlbumProjects = pendingAlbumProjects.ToList(),
			ProjectById = projectById.Where(kv => kv.Value?.projectId != null).ToDictionary(kv => kv.Key, kv => kv.Value.projectId),
			ProjectByRecordId = projectByRecordId.Where(kv => kv.Value?.projectId != null).ToDictionary(kv => kv.Key, kv => kv.Value.projectId),
			AnnualAlbumProjectsByArtist = annualAlbumProjectsByArtist
				.Select(kv => new AnnualArtistProjectCount { ArtistId = kv.Key.ArtistId, Year = kv.Key.Year, Count = kv.Value }).ToList(),
			LabelActiveRecords = labelActiveRecords.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value)),
			RetiredLabelRecordHistory = retiredLabelRecordHistory.ToDictionary(kv => kv.Key, kv => new List<LabelRecordHistoryEntry>(kv.Value)),
			IndependentDistributors = independentDistributors.ToList(),
			IndependentDistributorsByRegion = independentDistributorsByRegion
				.ToDictionary(kv => kv.Key, kv => kv.Value.Where(d => d?.distributorId != null).Select(d => d.distributorId).ToList()),
			CreditedLabelTop40RecordIds = creditedLabelTop40RecordIds.ToList(),
			CreditedLabelNumberOneRecordIds = creditedLabelNumberOneRecordIds.ToList(),
			ChartedLabelIds = chartedLabelIds.ToList(),
			ForcedConsolidationClients = forcedConsolidationClients.ToList(),
			LabelFinancials = new Dictionary<string, LabelFinancialHistory>(labelFinancials),
			AnnualGenreSupplyByLabel = annualGenreSupplyByLabel
				.ToDictionary(kv => kv.Key, kv => kv.Value.ToDictionary(g => (int)g.Key, g => g.Value)),
			AnnualGenreSupplyGlobal = annualGenreSupplyGlobal.ToDictionary(kv => (int)kv.Key, kv => kv.Value),
			ConsolidationAbsorptionsThisDecade = consolidationAbsorptionsThisDecade,
			LastBookedSettlementId = lastBookedSettlementId,
			PipelineWeek = pipelineWeek,
			GenreSupplyYear = genreSupplyYear,
			GeneratedRecordCounter = generatedRecordCounter
		};
		w.Competitor = c;
	}

	/// <summary>Rehydrates the competitor economy in place. Runs after records + labels are restored, so the
	/// project record refs and the shared label list relink correctly.</summary>
	public void RehydrateEconomy(WorldSaveData w) {
		CompetitorSaveData c = w.Competitor;
		if (c == null) return;

		// The label list is the same object ChartManager rebuilt in place; re-point defensively.
		aiLabels = ChartManager.Instance?.GetAllLabels();
		settlementLabelLookup = null;   // rebuilt lazily against the restored labels

		albumProjects.Clear(); albumProjects.AddRange(c.AlbumProjects ?? new List<AlbumProject>());
		pendingAlbumProjects.Clear(); pendingAlbumProjects.AddRange(c.PendingAlbumProjects ?? new List<AlbumProject>());

		// A project's Record refs: relink released records to the shared allRecords object (identity); a pending
		// project's record isn't in allRecords, so its deserialized copy stays.
		foreach (AlbumProject project in albumProjects.Concat(pendingAlbumProjects)) RelinkProjectRecords(project);

		var projectByProjectId = albumProjects.Concat(pendingAlbumProjects)
			.Where(p => !string.IsNullOrEmpty(p.projectId))
			.GroupBy(p => p.projectId).ToDictionary(g => g.Key, g => g.First());
		AlbumProject ResolveProject(string projectId) =>
			projectId != null && projectByProjectId.TryGetValue(projectId, out AlbumProject p) ? p : null;

		projectById.Clear();
		foreach (var kv in c.ProjectById ?? new Dictionary<string, string>()) { AlbumProject p = ResolveProject(kv.Value); if (p != null) projectById[kv.Key] = p; }
		projectByRecordId.Clear();
		foreach (var kv in c.ProjectByRecordId ?? new Dictionary<string, string>()) { AlbumProject p = ResolveProject(kv.Value); if (p != null) projectByRecordId[kv.Key] = p; }

		annualAlbumProjectsByArtist.Clear();
		foreach (AnnualArtistProjectCount e in c.AnnualAlbumProjectsByArtist ?? new List<AnnualArtistProjectCount>())
			annualAlbumProjectsByArtist[(e.ArtistId, e.Year)] = e.Count;

		labelActiveRecords = (c.LabelActiveRecords ?? new Dictionary<string, List<string>>())
			.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value ?? new List<string>()));

		retiredLabelRecordHistory.Clear();
		foreach (var kv in c.RetiredLabelRecordHistory ?? new Dictionary<string, List<LabelRecordHistoryEntry>>())
			retiredLabelRecordHistory[kv.Key] = new List<LabelRecordHistoryEntry>(kv.Value ?? new List<LabelRecordHistoryEntry>());

		independentDistributors.Clear(); independentDistributors.AddRange(c.IndependentDistributors ?? new List<IndependentDistributor>());
		var distributorById = independentDistributors.Where(d => !string.IsNullOrEmpty(d.distributorId))
			.GroupBy(d => d.distributorId).ToDictionary(g => g.Key, g => g.First());
		independentDistributorsByRegion.Clear();
		foreach (var kv in c.IndependentDistributorsByRegion ?? new Dictionary<string, List<string>>()) {
			var list = new List<IndependentDistributor>();
			foreach (string id in kv.Value ?? new List<string>()) if (distributorById.TryGetValue(id, out IndependentDistributor d)) list.Add(d);
			independentDistributorsByRegion[kv.Key] = list;
		}

		RefillSet(creditedLabelTop40RecordIds, c.CreditedLabelTop40RecordIds);
		RefillSet(creditedLabelNumberOneRecordIds, c.CreditedLabelNumberOneRecordIds);
		RefillSet(chartedLabelIds, c.ChartedLabelIds);
		RefillSet(forcedConsolidationClients, c.ForcedConsolidationClients);

		labelFinancials = new Dictionary<string, LabelFinancialHistory>(c.LabelFinancials ?? new Dictionary<string, LabelFinancialHistory>());

		annualGenreSupplyByLabel.Clear();
		foreach (var kv in c.AnnualGenreSupplyByLabel ?? new Dictionary<string, Dictionary<int, int>>())
			annualGenreSupplyByLabel[kv.Key] = (kv.Value ?? new Dictionary<int, int>()).ToDictionary(g => (Genre)g.Key, g => g.Value);
		annualGenreSupplyGlobal.Clear();
		foreach (var kv in c.AnnualGenreSupplyGlobal ?? new Dictionary<int, int>()) annualGenreSupplyGlobal[(Genre)kv.Key] = kv.Value;

		consolidationAbsorptionsThisDecade = c.ConsolidationAbsorptionsThisDecade;
		// These counters advance in lockstep with the chart week, so a pre-fix save (which never stored them)
		// can recover from w.ChartWeek rather than crash on the next settlement / stall its in-flight drops.
		lastBookedSettlementId = c.LastBookedSettlementId > 0 ? c.LastBookedSettlementId : w.ChartWeek;
		pipelineWeek = c.PipelineWeek > 0 ? c.PipelineWeek : w.ChartWeek;
		genreSupplyYear = c.GenreSupplyYear;
		// A pre-fix save never stored this, so fall back to the highest restored "gen_N" id + 1 to guarantee the
		// next mint can't collide with an existing record (which would crash the following Save).
		generatedRecordCounter = c.GeneratedRecordCounter > 0 ? c.GeneratedRecordCounter : HighestGeneratedRecordId(w);
	}

	/// <summary>Highest N across restored "gen_N" record ids (0 if none), used as a pre-fix-save fallback for
	/// <see cref="generatedRecordCounter"/>.</summary>
	private static int HighestGeneratedRecordId(WorldSaveData w) {
		int highest = 0;
		foreach (RecordRuntimeData record in w.Records ?? new List<RecordRuntimeData>()) {
			string id = record?.baseRecord?.recordId;
			if (id != null && id.StartsWith("gen_", System.StringComparison.Ordinal) &&
				int.TryParse(id.Substring(4), out int n) && n > highest) highest = n;
		}
		return highest;
	}

	private static void RelinkProjectRecords(AlbumProject project) {
		if (project == null) return;
		if (project.albumRecord?.recordId != null) {
			RecordRuntimeData live = ChartManager.Instance?.GetRecordRuntimeData(project.albumRecord.recordId);
			if (live?.baseRecord != null) project.albumRecord = live.baseRecord;
		}
		if (project.promoSingleRecord?.recordId != null) {
			RecordRuntimeData live = ChartManager.Instance?.GetRecordRuntimeData(project.promoSingleRecord.recordId);
			if (live?.baseRecord != null) project.promoSingleRecord = live.baseRecord;
		}
	}

	private static void RefillSet(HashSet<string> target, List<string> source) {
		target.Clear();
		foreach (string id in source ?? new List<string>()) if (id != null) target.Add(id);
	}

	public void RegisterLabel(AILabel label) {
		if (label == null || string.IsNullOrEmpty(label.labelId)) return;
		if (aiLabels != null && !aiLabels.Contains(label)) aiLabels.Add(label);
		if (!labelActiveRecords.ContainsKey(label.labelId)) labelActiveRecords[label.labelId] = new List<string>();
		if (!retiredLabelRecordHistory.ContainsKey(label.labelId))
			retiredLabelRecordHistory[label.labelId] = new List<LabelRecordHistoryEntry>();
		if (!labelFinancials.ContainsKey(label.labelId)) labelFinancials[label.labelId] = new LabelFinancialHistory();
		LabelCompanyAccounts.Open(label);
	}

	// ========================================================================
	// PLAYER-DRIVEN ACTIONS
	// The player's label is a registered AILabel that the AI decision loops skip,
	// so everything it does has to be pushed in from the desk. These two entry
	// points reuse the same release and distribution plumbing the AI runs on --
	// the player is not on a parallel economy, only on a manual one.
	// ========================================================================

	/// <summary>
	/// Puts a master the player has already paid to record into the market on
	/// <paramref name="date"/>. Production cost is passed in because the player was
	/// charged for it at the session, not here.
	/// </summary>
	public bool ReleasePlayerRecord(AILabel label, SimulatedArtist artist, Record record,
		float marketingBudget, float sunkProductionCost, GameDate date) {
		if (label == null || artist == null || record == null || ChartManager.Instance == null) return false;

		record.labelId = label.labelId;
		record.isPlayerOwned = true;
		record.releaseDate = date;
		record.projectRole = ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;

		ChartManager.Instance.ReleaseRecord(record);
		RecordRuntimeData runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return false;

		runtimeData.sunkProductionCost = sunkProductionCost;
		if (ReleaseAllInEconomics.IsLive) runtimeData.sunkMarketingCost = 0f;
		runtimeData.revenueMemoryEligible = true;
		runtimeData.releaseTimeExpectedNet = 0f;
		runtimeData.releaseTimeOpportunityScale = Mathf.Max(1f, sunkProductionCost);
		runtimeData.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtimeData.projectRole = record.projectRole;

		// The player knows exactly what they cut, so there is no perceived-quality
		// misread to apply -- the AI's noise term is its scouting ear, not a market effect.
		ApplyReleasePromotion(record, artist, label, marketingBudget, 1f);
		CaptureSingleOpportunity(runtimeData, label, date.year);
		TrackRelease(label.labelId, record.recordId);
		WeeklySingleReleases++;
		RecordArtistRelease(artist, record.recordId, record.format);
		return true;
	}

	/// <summary>
	/// Early-game Phase 5 (§5.4): an AI label releases a master it leased or bought from the player, tagged with the
	/// contract id. The buyer funds the pressing (per-unit COGS in its own settlement) and the launch campaign, on its
	/// own marketing rule; the recording was paid for by the player, so no production cost is charged here. Only the
	/// player desk calls this.
	/// </summary>
	public bool ReleaseLicensedPlayerMaster(AILabel buyer, SimulatedArtist artist, Record record, string dealId, float perceivedQuality, GameDate date) {
		if (buyer == null || record == null || ChartManager.Instance == null || !buyer.IsActive || buyer.isPlayerOwned) return false;
		record.labelId = buyer.labelId;
		record.isPlayerOwned = false;
		record.playerMasterDealId = dealId;
		record.releaseDate = date;
		record.projectRole = ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;

		float marketingBudget = artist != null ? buyer.GetMarketingBudget(artist) : 0f;
		float minReserve = buyer.GetMonthlyOverhead();
		if (buyer.cashReserves - marketingBudget < minReserve) marketingBudget = Mathf.Max(0f, (buyer.cashReserves - minReserve) * 0.8f);
		RecordExpense(buyer, marketingBudget, LabelCashKind.Campaign);
		WeeklyMarketingSpend += marketingBudget;
		WeeklyMarketingEvents++;

		// Same order as the AI's own single (TryReleaseRecord): release, then the label's campaign on what it heard.
		float perceivedQualityMult = 0.6f + Mathf.Clamp(perceivedQuality, 0f, 1f) * 0.8f;
		ChartManager.Instance.ReleaseRecord(record);
		RecordRuntimeData runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return false;
		runtimeData.sunkProductionCost = 0f;
		if (ReleaseAllInEconomics.IsLive) runtimeData.sunkMarketingCost = 0f;
		runtimeData.revenueMemoryEligible = false;   // the buyer's format memory learns from its own records
		runtimeData.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtimeData.projectRole = record.projectRole;
		if (artist != null) ApplyReleasePromotion(record, artist, buyer, marketingBudget, perceivedQualityMult);
		else AttributeReleaseCampaign(runtimeData, marketingBudget);
		TrackRelease(buyer.labelId, record.recordId);
		WeeklySingleReleases++;
		if (artist != null) RecordArtistRelease(artist, record.recordId, record.format);
		return true;
	}

	/// <summary>
	/// --ai-master-deals: a bigger AI label releases a single it leased or bought from a smaller one before release. The
	/// seller paid the session; the buyer funds pressing (per-unit COGS in its settlement) and its own launch campaign,
	/// as for a master leased from the player. The buyer's acquisition is its advance, booked as the record's sunk cost.
	/// </summary>
	public bool ReleaseShoppedAiMaster(AILabel buyer, SimulatedArtist artist, Record record, float advance, float perceivedQuality, GameDate date) {
		if (buyer == null || record == null || ChartManager.Instance == null || !buyer.IsActive || buyer.isPlayerOwned) return false;
		record.labelId = buyer.labelId;
		record.isPlayerOwned = false;
		record.releaseDate = date;
		record.projectRole = ProjectRecordRole.OrphanSingle;
		record.albumProjectId = string.Empty;
		float marketingBudget = artist != null ? buyer.GetMarketingBudget(artist) : 0f;
		float minReserve = EconomicLabelLifecycle.Enabled ? EconomicLabelLifecycle.CommitmentReserve(buyer, signing: false) : buyer.GetMonthlyOverhead();
		if (buyer.cashReserves - marketingBudget < minReserve) marketingBudget = Mathf.Max(0f, (buyer.cashReserves - minReserve) * 0.8f);
		RecordExpense(buyer, marketingBudget, LabelCashKind.Campaign);
		WeeklyMarketingSpend += marketingBudget;
		WeeklyMarketingEvents++;
		float perceivedQualityMult = 0.6f + Mathf.Clamp(perceivedQuality, 0f, 1f) * 0.8f;
		ChartManager.Instance.ReleaseRecord(record);
		RecordRuntimeData runtimeData = ChartManager.Instance.GetRecordRuntimeData(record.recordId);
		if (runtimeData == null) return false;
		runtimeData.sunkProductionCost = advance;
		if (ReleaseAllInEconomics.IsLive) runtimeData.sunkMarketingCost = 0f;
		runtimeData.revenueMemoryEligible = false;   // the buyer's format memory learns from its own records
		runtimeData.releaseMemoryWeek = ChartManager.Instance.GetCurrentChartWeek();
		runtimeData.projectRole = record.projectRole;
		ApplyReleasePromotion(record, artist, buyer, marketingBudget, perceivedQualityMult);
		TrackRelease(buyer.labelId, record.recordId);
		WeeklySingleReleases++;
		RecordArtistRelease(artist, record.recordId, record.format);
		return true;
	}

	/// <summary>--ai-master-deals: an advance, statement or sale price from one AI label to another, booked as licensing.</summary>
	public void PayMasterAdvance(AILabel payer, AILabel payee, float amount) {
		if (payer == null || payee == null || amount <= 0f) return;
		RecordExpense(payer, amount, LabelCashKind.Licensing);
		LabelCompanyAccounts.ChangeCash(payee, amount, LabelCashKind.Licensing);
		payee.monthlyRevenue += amount;
		if (labelFinancials.TryGetValue(payee.labelId, out LabelFinancialHistory financials)) financials.lastMonthRevenue += amount;
	}

	/// <summary>--ai-master-deals: a session the seller paid for, counted like any production spend.</summary>
	public void NoteProductionSpend(AILabel label, float productionCost) {
		label.monthlyExpenses += productionCost;
		WeeklyProductionSpend += productionCost;
		WeeklyProductionEvents++;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) financials.lastMonthExpenses += productionCost;
	}

	/// <summary>
	/// Early-game Phase 5: one live record changes hands (a regional pickup, or a lease reverting), keeping its chart
	/// history -- one runtime record per master, never a second chart entry. The new owner's shops are stocked by its
	/// own reach; the old owner's copies in stores are withdrawn. A player owner sells only where it has a line, as at
	/// release. <paramref name="dealId"/> is the contract tag (null when the record comes home to the player).
	/// </summary>
	public void TransferRecordToLabel(RecordRuntimeData runtimeData, AILabel to, string dealId) {
		if (runtimeData?.baseRecord == null || to == null) return;
		if (runtimeData.physicalInventory != null && to.isPlayerOwned)
			throw new System.InvalidOperationException("Physical inventory transfer into PlayerDesk is not supported by the AI inventory experiment.");
		string recordId = runtimeData.baseRecord.recordId;
		string from = runtimeData.baseRecord.labelId;
		if (!string.IsNullOrEmpty(from) && labelActiveRecords.TryGetValue(from, out var fromIds)) fromIds.Remove(recordId);
		runtimeData.baseRecord.labelId = to.labelId;
		ChartManager.Instance?.NoteRecordOwnershipChanged();
		runtimeData.baseRecord.isPlayerOwned = to.isPlayerOwned;
		runtimeData.baseRecord.playerMasterDealId = dealId;
		float quality = runtimeData.perceivedQualityMultiplier > 0f ? runtimeData.perceivedQualityMultiplier : 1f;
		PhysicalPressingInventory.Replan(runtimeData, () => { foreach (var pair in runtimeData.regionalData) {
			if (pair.Value == null) continue;
			pair.Value.unitsInStores = to.isPlayerOwned && !to.HasDistributionInRegion(pair.Key) ? 0
				: ChartSimulator.CalculateInitialRegionalStock(to, pair.Key, 1f, quality, recordId);
		} });
		TrackRelease(to.labelId, recordId);
	}

	/// <summary>Whether label has proven a real regional breakout in regionId -- the exact evidence bar
	/// <see cref="GetProvenBreakoutRegions"/> already applies to the AI's own independent-distribution
	/// path. Exposed read-only for the player's house-line proof gate (directive §5.1); no new
	/// calibration, just the same regionalData reused instead of a parallel bar.</summary>
	public bool HasProvenBreakoutIn(AILabel label, string regionId) =>
		label != null && !string.IsNullOrEmpty(regionId) && GetProvenBreakoutRegions(label).Contains(regionId);

	/// <summary>The same regional-breakout bar <see cref="GetProvenBreakoutRegions"/> applies, exposed
	/// read-only for the promo mechanic directive's §6.3 breakout listing -- a per-RECORD read of
	/// RegionalRecordData.peakBreakoutScore, not the label-wide aggregate above.</summary>
	public float RegionalBreakoutDealThreshold => regionalBreakoutDealThreshold;

	/// <summary>Directive §6.3: another label's display name, for the trade page's breakout column
	/// ("other labels' breakouts included"). Null if the id doesn't resolve to an AI label.</summary>
	public string GetLabelDisplayName(string labelId) => GetLabel(labelId)?.labelName;

	// Directive §5.1: below regionalBreakoutDealThreshold, a house visit is a real pitch that can be
	// turned down ("I don't hear it") rather than an automatic yes -- and if it lands anyway, it lands
	// cold: back of the pile, not a courted line. A proven region always gets taken -- that's the payoff
	// for the proof, and the whole point of the gate.
	private const float UnprovenLineAcceptChance = 0.40f;
	private const float UnprovenLineSeedScale = 0.35f;

	/// <summary>
	/// Places the player's line with a wholesale house in one market. Same grant as
	/// <see cref="PursueIndependentDistribution"/>: coverage and the marginal reach it
	/// opens, no deal, no borrowed reach, no master ownership -- gated on the same regional-breakout
	/// evidence the AI path requires (directive §5.1). <paramref name="anyHouseAvailable"/> tells the
	/// caller whether there was even a house to pitch, so a structural "no room anywhere" reads
	/// differently from a real pitch that got turned down.
	/// </summary>
	/// <summary><paramref name="acceptBonus"/> (promo mechanic directive §6.1): a live trade-press pick
	/// on the label's catalogue nudges an unproven house past "I don't hear it" -- the trades talking to
	/// the trade, exactly the distributor-facing event a review was historically for. Zero by the one
	/// caller (PlayerDesk.PlaceLine) whenever nothing is currently in print, so behaviour is unchanged
	/// until §6.1 has something to say.</summary>
	public IndependentDistributor PlacePlayerLine(AILabel label, string regionId, float acceptBonus, out bool proven, out bool anyHouseAvailable) {
		proven = false;
		anyHouseAvailable = false;
		if (label == null || string.IsNullOrEmpty(regionId)) return null;
		if (label.HasDistributionInRegion(regionId)) return null;

		proven = HasProvenBreakoutIn(label, regionId);

		IndependentDistributor house = GetIndependentDistributorsInRegion(regionId)
			.FirstOrDefault(candidate => candidate.HasCapacity && !candidate.CarriesLabel(label.labelId));
		if (house == null) return null;
		anyHouseAvailable = true;

		// Below the bar, "I don't hear it" is the honest outcome most of the time -- proof is what
		// turns a warehouse visit into a sure thing.
		if (!proven && GameRandom.Randf() > UnprovenLineAcceptChance + Mathf.Max(0f, acceptBonus)) return null;
		if (!house.AddClient(label.labelId)) return null;

		label.independentDistributionRegions.Add(regionId);
		float before = label.ownedReach;
		float marginalShare = ChartManager.Instance?.GetNationalMarketShareForRegions(new[] { regionId }) ?? 0f;
		label.ownedReach = Mathf.Min(SelfBuiltReachCeiling,
			label.ownedReach + (marginalShare * independentCoverageReachFactor));

		// The house presses a run for this market's shops. Player records are otherwise unseeded in the
		// store engine (they sell out of the trunk elsewhere), so without this a line placed after release
		// would put nothing on the shelves. Seed each of the label's in-market singles now that the region
		// is covered; the weekly engine sells and restocks from here. A cold, unproven take gets a thinner
		// initial seed than a courted one -- "back-of-the-pile priority" (§5.1), not a hot-pile push.
		float seedScale = proven ? 1f : UnprovenLineSeedScale;
		foreach (RecordRuntimeData playerRecord in ChartManager.Instance?.GetPlayerRecords() ?? new List<RecordRuntimeData>()) {
			if (playerRecord.baseRecord.labelId != label.labelId) continue;
			if (!playerRecord.regionalData.TryGetValue(regionId, out RegionalRecordData data)) {
				data = new RegionalRecordData(regionId);
				playerRecord.regionalData[regionId] = data;
			}
			data.unitsInStores = ChartSimulator.CalculateInitialRegionalStock(label, regionId, seedScale,
				playerRecord.perceivedQualityMultiplier > 0f ? playerRecord.perceivedQualityMultiplier : 1f,
				playerRecord.baseRecord.recordId);
		}

		OnIndependentDistributionSigned?.Invoke(new IndependentDistributionTelemetry {
			week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			labelId = label.labelId, labelName = label.labelName, labelTier = label.tier,
			distributorId = house.distributorId, distributorName = house.distributorName,
			regionId = regionId, provenInRegion = proven,
			coveredRegionCount = label.independentDistributionRegions.Count,
			coveredMarketShare = ChartManager.Instance?.GetNationalMarketShareForRegions(label.AllCoveredRegions()) ?? 0f,
			ownedReachBefore = before, ownedReachAfter = label.ownedReach,
			houseClientCount = house.CurrentClientCount, houseClientCapacity = house.clientCapacity
		});
		return house;
	}

	/// <summary>Production cost for one player session, on the same cost basis the AI pays.</summary>
	public float GetPlayerProductionCost(AILabel label, Record record, GameDate date) =>
		CalculateProductionCost(label, record, date);

	public void RecordExpense(AILabel label, float amount, LabelCashKind kind = LabelCashKind.OtherOperating) {
		if (label == null || amount <= 0f) return;
		LabelCompanyAccounts.ChangeCash(label, -amount, kind);
		label.monthlyExpenses += amount;
		if (labelFinancials.TryGetValue(label.labelId, out LabelFinancialHistory financials)) {
			financials.lastMonthExpenses += amount;
		}
	}
	
	public List<AILabel> GetActiveLabelsByStatus(LabelStatus status) => aiLabels?.Where(l => l.status == status).ToList() ?? new List<AILabel>();
	
	public List<AILabel> GetOperatingLabels() => aiLabels?.Where(l => l.IsActive).ToList() ?? new List<AILabel>();
	
	public int GetLabelActiveRecordCount(string labelId) => labelActiveRecords.TryGetValue(labelId, out var records) ? records.Count : 0;
	internal IReadOnlyList<string> GetLabelActiveRecordIdsForChecks(string labelId) =>
		labelActiveRecords.TryGetValue(labelId, out var records) ? records : System.Array.Empty<string>();
	internal void ProcessRecordCampaignsForChecks() => ProcessRecordCampaigns();

	public int GetRecentChartingRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetRecordsForLabel(labelId).Count(record =>
			record.baseRecord.labelId == labelId && IsRecentChartingRecord(record, maxAgeWeeks));
		return active + RecentRetiredChartingCount(labelId, maxAgeWeeks);
	}
	private static bool IsRecentChartingRecord(RecordRuntimeData record, int maxAgeWeeks) =>
		record.weeksSinceRelease <= maxAgeWeeks && record.weeksOnChart > 0;
	private int RecentRetiredChartingCount(string labelId, int maxAgeWeeks) => CountRecentRetiredRecordEvidence(
		retiredLabelRecordHistory.GetValueOrDefault(labelId), ChartManager.Instance.GetCurrentChartWeek(),
		maxAgeWeeks, requireCharted: true, requireTop40: false);
	/// <summary>GetRecentChartingRecordCount for every label at once, for callers that ask about many labels while
	/// nothing changes: one pass over the records instead of one per label. Same predicate, same totals.</summary>
	private System.Func<string, int> RecentChartingRecordCounter(int maxAgeWeeks = 52) {
		if (ChartManager.Instance == null) return _ => 0;
		var active = new Dictionary<string, int>(System.StringComparer.Ordinal);
		foreach (RecordRuntimeData record in ChartManager.Instance.GetAllRecords()) {
			string id = record.baseRecord.labelId;
			if (id != null && IsRecentChartingRecord(record, maxAgeWeeks)) active[id] = active.GetValueOrDefault(id) + 1;
		}
		var totals = new Dictionary<string, int>(System.StringComparer.Ordinal);
		return labelId => string.IsNullOrEmpty(labelId) ? 0 : totals.TryGetValue(labelId, out int n) ? n :
			totals[labelId] = active.GetValueOrDefault(labelId) + RecentRetiredChartingCount(labelId, maxAgeWeeks);
	}

	public int GetRecentReleasedRecordCount(string labelId, int maxAgeWeeks = 52) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetRecordsForLabel(labelId).Count(record =>
			record.baseRecord.labelId == labelId &&
			record.weeksSinceRelease <= maxAgeWeeks);
		return active + CountRecentRetiredRecordEvidence(
			retiredLabelRecordHistory.GetValueOrDefault(labelId), ChartManager.Instance.GetCurrentChartWeek(),
			maxAgeWeeks, requireCharted: false, requireTop40: false);
	}

	private int GetRecentTop40RecordCount(string labelId, int maxAgeWeeks) {
		if (string.IsNullOrEmpty(labelId) || ChartManager.Instance == null) return 0;
		int active = ChartManager.Instance.GetRecordsForLabel(labelId).Count(record =>
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
		var label = aiLabels.Where(l => l.status != LabelStatus.Bankrupt && l.roster.Count > 0).OrderBy(l => GameRandom.Randf()).FirstOrDefault();
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
	public float? sunkMarketingCost;
	public float realizedNet;
	// LabelPressingEconomics book at retirement (0 / 0 while the switch is off).
	public int unitsPressed, pressingBookSold;
}

public sealed class FormatMemoryRevisionTelemetry {
	public int? careerBand;
	public int releaseWeek;
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
