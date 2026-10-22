using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Opt-in film and broadcast parents (SimTools/FilmBroadcastLabelResearch.md), off unless named.
/// --studio-parent-labels seeds MGM, ABC-Paramount, Warner Bros., United Artists and Dot as standard MidTier labels and
/// gives them, plus the existing Colpix, a parent company. History's studio labels had neither branch distribution nor
/// the Big-5's launch machinery; they had parents that absorbed losses (Warner ~$3M a year, 1958-62) and bought or leased
/// repertoire. So the switch adds money and routes, not launch access:
/// <list type="bullet">
/// <item>the parent tops cash up to the commitment reserve, never above it, so it pays bills but funds no new launches;</item>
/// <item>it funds acquisitions: master pickups and leases, a bid for a proven act at contract expiry, own-studio soundtracks;</item>
/// <item>an annual review closes a label that has drawn support for five straight years (Colpix's 1966 end is the precedent).</item>
/// </list>
/// Studio labels may not promote to Major. Every coefficient below is a disclosed scenario value, not a measured rate.
/// </summary>
public static class StudioParentLabels {
	public const string Switch = "--studio-parent-labels";
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Contains(Switch);
		if (Enabled && args.Contains(HeadScaleTemplates.Switch))
			throw new ArgumentException(Switch + " and " + HeadScaleTemplates.Switch + " seed the same firms; use one.");
		if (Enabled && (!EconomicLabelLifecycle.Enabled || !LabelCorporateModel.ChangesEconomy))
			throw new ArgumentException(Switch + " requires economic exits and the corporate label model.");
		configured = true;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; Enabled = value; }

	// ---- parents -------------------------------------------------------------------------------------------------

	public const string Mgm = "Loew's (Metro-Goldwyn-Mayer)", Abpt = "American Broadcasting-Paramount Theatres",
		Warner = "Warner Bros. Pictures", UnitedArtists = "United Artists Corporation", Paramount = "Paramount Pictures",
		Columbia = "Columbia Pictures", Universal = "Universal Pictures", Fox = "20th Century-Fox", OtherStudio = "Other";

	/// <summary>The parent of a seeded template by base name, null for every other label.</summary>
	public static string ParentFor(string baseName) => baseName switch {
		"MGM" => Mgm, "ABC-Paramount" => Abpt, "Warner Bros." => Warner, "United Artists" => UnitedArtists,
		"Dot" => Paramount, "Colpix" => Columbia, _ => null
	};

	public static bool IsStudioLabel(AILabel label) => Enabled && label != null && !string.IsNullOrEmpty(label.studioParent);

	/// <summary>Studio labels keep their 1960 standing: none may become a Major distributor.</summary>
	internal static bool HoldsMajorPromotion(AILabel label) => IsStudioLabel(label);

	// ---- parent support ------------------------------------------------------------------------------------------

	// Warner lost about $3M a year in 1958-62 on a market of about $480M at retail (1960); MGM Records lost $18M over
	// fiscal 1968-69 on a market roughly three times larger. Both are about 0.6% of retail, about 1.25% of the factory
	// value the game's record gross tracks. A parent's yearly support is that share of the market, accrued monthly.
	public const double SupportShareOfMarketGross = 0.0125;
	public const int SupportWindowMonths = 12;
	// Consecutive calendar years with any parent draw before the parent closes the label. Warner drew for five years
	// (1958-62) and was kept, so the review closes only after a sixth; a disclosed scenario choice.
	public const int ClosureLossYears = 6;

	/// <summary>Support still available: accrued minus drawn over the trailing twelve months.</summary>
	public static float Capacity(AILabel label) => !IsStudioLabel(label) ? 0f
		: Math.Max(0f, (label.parentSupportAccrued?.Sum() ?? 0f) - (label.parentSupportDrawn?.Sum() ?? 0f));

	/// <summary>
	/// Month close, before the solvency review: accrue this month's support and top cash up to the commitment reserve.
	/// The market month is the sum of every label's last closed statement (some this month, some last; one month each).
	/// Returns the amount the parent contributed. <paramref name="date"/> is the month-change date, as for every month
	/// close: the closing period is the month before it.
	/// </summary>
	internal static float SupportMonth(AILabel label, IEnumerable<AILabel> labels, GameDate date) {
		if (!IsStudioLabel(label) || !label.IsActive) return 0f;
		int closingYear = ClosingYear(date);
		double market = (labels ?? Enumerable.Empty<AILabel>()).Where(l => l?.company?.lastStatement != null)
			.Sum(l => l.company.lastStatement.recordGross);
		float accrual = (float)(market * SupportShareOfMarketGross);
		if (label.parentSupportYear != closingYear) {
			label.parentSupportYear = closingYear;
			label.parentSupportYearDrawn = label.parentSupportYearAccrued = 0f;
		}
		Push(label.parentSupportAccrued ??= new List<float>(), accrual);
		Push(label.parentSupportDrawn ??= new List<float>(), 0f);
		label.parentSupportYearAccrued += accrual;
		float need = EconomicLabelLifecycle.CommitmentReserve(label, signing: false) - label.cashReserves;
		float amount = Math.Min(Math.Max(0f, need), Capacity(label));
		if (amount > 0.005f) Contribute(label, amount, "support", date);
		return amount;
	}

	/// <summary>The parent pays <paramref name="amount"/> in as capital, drawn against the support window.</summary>
	internal static void Contribute(AILabel label, float amount, string purpose, GameDate date) {
		if (amount <= 0f) return;
		LabelCompanyAccounts.ChangeCash(label, amount, LabelCashKind.ParentCapital);
		label.parentSupportDrawn ??= new List<float> { 0f };
		if (label.parentSupportDrawn.Count == 0) label.parentSupportDrawn.Add(0f);
		label.parentSupportDrawn[^1] += amount;
		label.parentSupportYearDrawn += amount;
		label.parentSupportLifetime += amount;
		GD.Print($"STUDIO_PARENT_CAPITAL year={date.year} month={date.month} label={label.labelId} parent=\"{label.studioParent}\" " +
			$"purpose={purpose} amount={amount:F0} capacityAfter={Capacity(label):F0} cash={label.cashReserves:F0}");
	}

	/// <summary>True when the label can pay <paramref name="cost"/> itself or its parent can fund it.</summary>
	internal static bool CanFund(AILabel label, float cost) =>
		label != null && (label.CanAffordToSign(cost) || IsStudioLabel(label) && label.IsActive && Capacity(label) >= cost);

	/// <summary>Before an acquisition: the parent pays in whatever the label's own free cash cannot cover.</summary>
	internal static void FundAcquisition(AILabel label, float cost, string purpose, GameDate date) {
		if (!IsStudioLabel(label) || label.CanAffordToSign(cost)) return;
		float own = Math.Max(0f, label.cashReserves - SigningReserve(label));
		Contribute(label, Math.Min(Capacity(label), cost - own), purpose, date);
	}

	// The reserve AILabel.CanAffordToSign keeps; an acquisition leaves the label at exactly this after paying.
	internal static float SigningReserve(AILabel label) => EconomicLabelLifecycle.Enabled && !label.isPlayerOwned
		? EconomicLabelLifecycle.CommitmentReserve(label, signing: true) : label.GetMonthlyOverhead() * 2f;

	/// <summary>December close (the January month change): count the year and decide whether the parent closes the label.</summary>
	internal static void ReviewYear(AILabel label, GameDate date) {
		if (!IsStudioLabel(label) || !label.IsActive || date.month != 1) return;
		label.parentLossYears = label.parentSupportYearDrawn > 0.005f ? label.parentLossYears + 1 : 0;
		label.parentClosureDue = label.parentLossYears >= ClosureLossYears;
		GD.Print($"STUDIO_PARENT_REVIEW year={ClosingYear(date)} label={label.labelId} parent=\"{label.studioParent}\" " +
			$"drawn={label.parentSupportYearDrawn:F0} accrued={label.parentSupportYearAccrued:F0} lossYears={label.parentLossYears} " +
			$"closure={label.parentClosureDue} cash={label.cashReserves:F0} lifetime={label.parentSupportLifetime:F0}");
	}

	private static int ClosingYear(GameDate date) => (LabelCorporateModel.Period(date) - 1) / 12;

	private static void Push(List<float> window, float value) {
		window.Add(value);
		while (window.Count > SupportWindowMonths) window.RemoveAt(0);
	}

	// ---- master pickups ------------------------------------------------------------------------------------------

	// Dot leased finished hits from independent producers and UA took early Motown acts: with a parent's money behind
	// them, studio labels bid harder for a proven master. The premium raises their offer strength, and so their price.
	public const float PickupStrengthPremium = 0.25f;

	internal static float PickupPremium(AILabel buyer, float advance) =>
		IsStudioLabel(buyer) && CanFund(buyer, advance) ? PickupStrengthPremium : 0f;

	// ---- free agents at contract expiry --------------------------------------------------------------------------

	private const uint BidDecision = 0x53500100U;
	// Warner's reported $525,000 Everly guarantee was many times a 1950s indie deal; a 2x guarantee over the incumbent's
	// renewal offer and an even chance the act takes it are scenario values. One won bid per label per year.
	public const float BidGuaranteeMultiple = 2f;
	public const float BidAcceptance = 0.5f;

	/// <summary>
	/// A studio label's bid for a proven act whose non-Major, non-studio contract has matured. Null when none bids or the
	/// act stays (one keyed draw per act, label and year). The incumbent's renewal decision runs only when this is null.
	/// </summary>
	internal static (AILabel Bidder, float Guarantee) SelectBidder(AILabel incumbent, SimulatedArtist artist, int year,
		IEnumerable<AILabel> labels, Func<AILabel, bool> canReach = null) {
		if (!Enabled || incumbent == null || artist == null || incumbent.isPlayerOwned || incumbent.tier == LabelTier.Major ||
			IsStudioLabel(incumbent)) return (null, 0f);
		if (artist.contractTop40Hits < 1 && artist.careerState is not (CareerState.Established or CareerState.Star or CareerState.Superstar))
			return (null, 0f);
		float guarantee = BidGuaranteeMultiple * Math.Max(1f, incumbent.CalculateAdvanceOffer(artist));
		var bidder = (labels ?? Enumerable.Empty<AILabel>())
			.Where(l => IsStudioLabel(l) && l.IsActive && l.parentBidYear != year && l.CurrentRosterSize < l.maxRosterSize &&
				CanFund(l, guarantee) && (canReach?.Invoke(l) ?? true))
			.Select(l => (Label: l, Fit: PlayerDesk.SoundFit(l, artist.primaryGenre, year)))
			.Where(x => x.Fit.HasValue)
			.OrderBy(x => x.Fit.Value ? 0 : 1).ThenByDescending(x => Capacity(x.Label)).ThenBy(x => x.Label.labelId, StringComparer.Ordinal)
			.Select(x => x.Label).FirstOrDefault();
		if (bidder == null) return (null, 0f);
		bool accepted = PhiloxCounterRandom.Unit(BandLife.WorldSeed, artist.artistId, bidder.labelId, year, BidDecision) < BidAcceptance;
		GD.Print($"STUDIO_FREE_AGENT_BID year={year} artist={artist.artistId} incumbent={incumbent.labelId} incumbentTier={incumbent.tier} " +
			$"bidder={bidder.labelId} guarantee={guarantee:F0} career={artist.careerState} contractTop40={artist.contractTop40Hits} accepted={accepted}");
		return accepted ? (bidder, guarantee) : (null, 0f);
	}

	// ---- soundtracks ---------------------------------------------------------------------------------------------

	// Of the seven film soundtrack #1 LPs of 1960-64, five went to another label through the performer's or composer's
	// contract or a licensing bid (Elvis, Mancini, Exodus, West Side Story) and two to their own studio's label. Scenario
	// values: a contracted performer or composer takes 35% of ordinary scores and 60% of blockbusters to the open bid;
	// otherwise the own studio's label takes the album with probability 0.6. Stage casts always go to the open bid.
	public const float ContractRouteShare = 0.35f, BlockbusterContractRouteShare = 0.60f, OwnStudioFirstRefusal = 0.60f;

	// Roughly even shares across the eight majors' and other producers' releases; not a measured release count.
	private static readonly (string Studio, float Weight)[] studioWeights = {
		(Mgm, .14f), (Paramount, .13f), (Warner, .12f), (UnitedArtists, .13f), (Columbia, .13f),
		(Universal, .11f), (Fox, .14f), (OtherStudio, .10f)
	};

	/// <summary>
	/// Route a film soundtrack before the open bid: draws a studio of origin, then the contract route and the own-studio
	/// first refusal. Returns the own studio's label, or null for the open bid. Universal's label is Decca from 1962 (MCA).
	/// Uses the global stream only with the switch on.
	/// </summary>
	internal static AILabel RouteSoundtrack(IReadOnlyList<AILabel> labels, ExternalMediaProfile profile, int year) {
		if (!Enabled || profile == null || labels == null) return null;
		if (profile.sourceType == ExternalMediaSourceType.StageCast) { Log(profile, year, "stage-open", null); return null; }
		float r = GameRandom.Randf(), cumulative = 0f;
		profile.studioOfOrigin = OtherStudio;
		foreach (var (studio, weight) in studioWeights) {
			cumulative += weight;
			if (r < cumulative) { profile.studioOfOrigin = studio; break; }
		}
		float contract = profile.isBlockbuster ? BlockbusterContractRouteShare : ContractRouteShare;
		if (GameRandom.Randf() < contract) { Log(profile, year, "contract-open", null); return null; }
		AILabel own = OwnStudioLabel(labels, profile.studioOfOrigin, year);
		if (own == null) { Log(profile, year, "no-studio-label", null); return null; }
		if (GameRandom.Randf() >= OwnStudioFirstRefusal) { Log(profile, year, "studio-declined", own); return null; }
		float fee = own.GetProductionCost() * profile.upfrontLicenseFee;
		if (own.cashReserves <= fee + own.GetMonthlyOverhead() && !(IsStudioLabel(own) && Capacity(own) >= fee)) {
			Log(profile, year, "studio-unaffordable", own); return null;
		}
		Log(profile, year, "own-studio", own);
		return own;
	}

	private static AILabel OwnStudioLabel(IReadOnlyList<AILabel> labels, string studio, int year) {
		if (studio == Universal)
			return year >= 1962 ? labels.FirstOrDefault(l => l != null && l.IsActive && l.isHistorical && l.labelId == "label_0004") : null;
		return labels.FirstOrDefault(l => IsStudioLabel(l) && l.IsActive && l.studioParent == studio);
	}

	private static void Log(ExternalMediaProfile profile, int year, string route, AILabel label) =>
		GD.Print($"STUDIO_SOUNDTRACK_ROUTE year={year} source={profile.sourceType} studio=\"{profile.studioOfOrigin}\" " +
			$"blockbuster={profile.isBlockbuster} route={route} label={label?.labelId}");
}
