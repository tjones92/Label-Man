using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>An AI label's master out on lease to a bigger AI label: advance against a royalty on list, recouped first,
/// settled on quarterly statements; the master comes home at the end of the term.</summary>
public sealed class AiMasterLease {
	public string RecordId { get; set; }
	public string BuyerLabelId { get; set; }
	public float Advance { get; set; }
	public float Rate { get; set; }
	public int StartWeek { get; set; }
	public int EndWeek { get; set; }
	public int UnitsAtStart { get; set; }
	/// <summary>Royalty earned over the term, before recoupment.</summary>
	public float Earned { get; set; }
	/// <summary>Cash paid to the licensor on statements, after the advance recouped.</summary>
	public float Paid { get; set; }
	public int LastStatementWeek { get; set; }
}

/// <summary>
/// Opt-in (<c>--ai-master-deals</c>): the player's early-game master routes (EarlyGameLabelManDirectiveV2 §2, §5), open to
/// AI Small, Boutique and Independent labels selling to bigger AI labels. Same buyer ear, bars, prices and terms as the
/// player's desk; no new calibration.
///
/// <b>Shop the master</b> (Gordy's Miracles masters leased to End): a label that can pay for a session but not for pressing
/// and promotion cuts the single and offers it to bigger labels whose sound fits, up to <see cref="MaxApproaches"/> offices.
/// The first buyer that hears a record offers a lease ($150-600 advance against 7-8% of list, 52 weeks from its release),
/// and a sale at 2.5x when it likes it a lot. A cash-short seller takes the sale when offered.
///
/// <b>Regional pickup</b> (Tamla's "Come to Me" to United Artists): when a seller's single breaks out in a new market, the
/// best-fitting bigger buyer that hears it may call, likely but not guaranteed (the player's chance, one keyed draw per
/// evidence). The lease is priced from the evidence (up to $1,500). The seller takes it when it cannot work the record
/// nationally itself: a Small or Boutique label, or a thin Independent. The live record changes hands with its chart
/// history. A sale-with-act is not offered: AI rosters move through their own courtship path.
///
/// Buyers read hook and production through their own fixed noise (counter RNG keyed by buyer and record), never the
/// hidden score, so a pass stands until the evidence changes. Artists stay on the seller's roster. Publishing is never
/// included. Off: nothing here runs.
/// </summary>
public static class AiMasterDeals {
	public const string Switch = "--ai-master-deals";
	// Screen against the original route before adopting: real hit evidence, a national
	// buyer that improves reach, and no re-leasing another firm's outstanding lease.
	public const string RegionalHitSwitch = "--ai-master-national-pickups";
	public static bool RegionalHitGate { get; private set; }
	public const string CapacitySwitch = "--ai-master-capacity-decisions";
	public static bool CapacityDecisions { get; private set; }
	// Sun's Presley contract to RCA: a cash-short indie sells a live hit to a bigger firm before its
	// overdraft closes it. Screen before adopting (SimTools/WorkingCapitalResearch.md).
	public const string DistressSwitch = "--distress-master-sales";
	public static bool DistressSales { get; private set; }
	// Scenario coefficients, not measured history: a forced seller's buyer pays half its evidence-based
	// estimate, and never extrapolates a climb beyond a 0.9 weekly carry (about nine weeks of current sales).
	public const float DistressDiscount = 0.5f;
	public const float DistressMaxCarry = 0.9f;
	public const int MaxApproaches = 4;
	public const int PickupMinWeek = 2;
	public const int PickupMaxWeek = 16;
	public const int StatementWeeks = 13;
	private const uint EarDecision = 0x4D530000U;
	private const uint PickupDecision = 0x4D530100U;
	private static bool configured;
	public static bool Enabled { get; private set; }
	/// <summary>Breakout markets last heard per record: a call comes only on new evidence. Not saved; a resumed run
	/// re-hears current evidence, and the keyed draw gives the same answer.</summary>
	private static readonly Dictionary<string, int> pickupEvidence = new(StringComparer.Ordinal);

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		RegionalHitGate = (arguments ?? Array.Empty<string>()).Contains(RegionalHitSwitch);
		CapacityDecisions = (arguments ?? Array.Empty<string>()).Contains(CapacitySwitch);
		DistressSales = (arguments ?? Array.Empty<string>()).Contains(DistressSwitch);
		if (DistressSales && !Enabled) throw new ArgumentException(DistressSwitch + " requires " + Switch);
		if (CapacityDecisions && !RegionalHitGate) throw new ArgumentException(CapacitySwitch + " requires " + RegionalHitSwitch);
		if (RegionalHitGate && !Enabled) throw new ArgumentException(RegionalHitSwitch + " requires " + Switch);
		if (RegionalHitGate && (arguments ?? Array.Empty<string>()).Contains("--no-regional-hit-deal-gate"))
			throw new ArgumentException(RegionalHitSwitch + " requires regional-hit evidence stamping.");
		configured = true;
	}

	internal static void SetEnabledForChecks(bool enabled) { configured = true; Enabled = enabled; }
	internal static void SetDistressForChecks(bool enabled) => DistressSales = enabled;

	public static bool IsSeller(AILabel label) => Enabled && label != null && !label.isPlayerOwned && label.IsActive
		&& !label.IsSubsidiary && label.tier is (LabelTier.Small or LabelTier.Boutique or LabelTier.Independent)
		&& !StudioParentLabels.IsStudioLabel(label);

	private static IEnumerable<(AILabel Buyer, bool OwnSound)> Buyers(AILabel seller, Genre genre, int year, float advance) =>
		(CompetitorManager.Instance?.GetOperatingLabels() ?? new List<AILabel>())
			.Where(b => b != null && !b.isPlayerOwned && b.labelId != seller.labelId && (int)b.tier < (int)seller.tier
				&& StudioParentLabels.CanFund(b, advance))
			.Select(b => (Buyer: b, Fit: PlayerDesk.SoundFit(b, genre, year)))
			.Where(x => x.Fit.HasValue)
			.Select(x => (x.Buyer, OwnSound: x.Fit.Value))
			// --studio-parent-labels: a parent-backed buyer calls first after own-sound fit (constant key when off).
			.OrderBy(x => x.OwnSound ? 0 : 1).ThenBy(x => StudioParentLabels.IsStudioLabel(x.Buyer) ? 0 : 1).ThenBy(x => (int)x.Buyer.tier).ThenBy(x => x.Buyer.labelId, StringComparer.Ordinal);

	/// <summary>The buyer's ear: the player's MasterRead, with the noise drawn from the counter RNG.</summary>
	private static float Read(AILabel buyer, Record record, RecordRuntimeData released, bool posted) {
		float appeal = record.hookStrength * 0.60f + record.productionQuality * 0.40f;
		float unit = (float)PhiloxCounterRandom.Unit(BandLife.WorldSeed, record.recordId, buyer.labelId, 0, EarDecision);
		float noise = (unit * 2f - 1f) * Mathf.Lerp(0.30f, 0.10f, Mathf.Clamp(buyer.scoutingAbility, 0f, 1f)) * (posted ? 1.4f : 1f);
		int markets = PlayerDesk.BreakoutMarkets(released);
		float evidence = released == null ? 0f : Mathf.Min(0.12f, 0.04f * markets) + Mathf.Min(0.08f, released.totalUnitsSold / 5000f);
		return Mathf.Clamp(appeal + noise + evidence, 0f, 1f);
	}

	private static (float Strength, float Lease, float Rate) Price(AILabel buyer, bool ownSound, float read, RecordRuntimeData released,
		float premium = 0f) {
		float bar = PlayerDesk.MasterBar(buyer, ownSound);
		if (read < bar) return (-1f, 0f, 0f);
		// A studio parent's premium (StudioParentLabels.PickupPremium) raises the offer, and so the price; zero otherwise.
		float s = Mathf.InverseLerp(bar, bar + 0.25f, read) + premium;
		float lease = Mathf.Clamp(PlayerDesk.Round25(Mathf.Lerp(PlayerDesk.MasterLeaseAdvanceMin, PlayerDesk.MasterLeaseAdvanceMax, s)
			* PlayerDesk.MasterTierScale(buyer)), PlayerDesk.MasterLeaseAdvanceMin, PlayerDesk.MasterLeaseAdvanceMax);
		int markets = PlayerDesk.BreakoutMarkets(released);
		if (released != null && markets > 0)
			lease = Mathf.Min(PlayerDesk.MasterPickupAdvanceMax, Mathf.Max(lease, PlayerDesk.Round25(150f * markets + 0.10f * released.totalUnitsSold)));
		return (s, lease, s >= 0.5f ? PlayerDesk.MasterLeaseRateStrong : PlayerDesk.MasterLeaseRate);
	}

	/// <summary>
	/// Shop a freshly recorded single the seller cannot afford to press and promote. Called from the AI release path after
	/// it has generated the record and priced the session, before any cash moves. True when a buyer took it and released it.
	/// </summary>
	public static bool TryShopUnreleased(CompetitorManager manager, AILabel seller, SimulatedArtist artist, Record record,
		float productionCost, float perceivedQuality, GameDate date) {
		if (!IsSeller(seller) || record == null || record.format != ReleaseFormat.Single || artist == null) return false;
		float reserve = EconomicLabelLifecycle.Enabled ? EconomicLabelLifecycle.CommitmentReserve(seller, signing: false)
			: seller.GetMonthlyOverhead();
		if (seller.cashReserves - productionCost < reserve) return false;
		foreach (var (buyer, ownSound) in Buyers(seller, record.primaryGenre, date.year, PlayerDesk.MasterLeaseAdvanceMin).Take(MaxApproaches)) {
			var (s, lease, rate) = Price(buyer, ownSound, Read(buyer, record, null, posted: true), null,
				StudioParentLabels.PickupPremium(buyer, PlayerDesk.MasterLeaseAdvanceMin));
			if (s < 0f) continue;
			float sale = PlayerDesk.Round25(lease * PlayerDesk.MasterSaleMultiple);
			bool sell = s >= 0.5f && StudioParentLabels.CanFund(buyer, sale);
			float advance = sell ? sale : lease;
			if (!StudioParentLabels.CanFund(buyer, advance)) continue;
			LabelCompanyAccounts.PayProject(seller, productionCost, productionCost);
			manager.NoteProductionSpend(seller, productionCost);
			artist.unrecoupedAdvance += productionCost;
			StudioParentLabels.FundAcquisition(buyer, advance, "master-shop", date);
			manager.PayMasterAdvance(buyer, seller, advance);
			if (!manager.ReleaseShoppedAiMaster(buyer, artist, record, advance, perceivedQuality, date))
				throw new InvalidOperationException($"Shopped master '{record.recordId}' failed to release at {buyer.labelId}.");
			int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			FounderLabelLifecycle.NoteRelease(seller, date);
			if (!sell) {
				(seller.aiMasterLeases ??= new List<AiMasterLease>()).Add(new AiMasterLease {
					RecordId = record.recordId, BuyerLabelId = buyer.labelId, Advance = advance, Rate = rate, StartWeek = week,
					EndWeek = week + PlayerDesk.MasterLeaseWeeks, UnitsAtStart = 0, LastStatementWeek = week });
			}
			Log("shop", sell ? "sale" : "lease", seller, buyer, record.recordId, advance, sell ? 0f : rate, week);
			return true;
		}
		return false;
	}

	/// <summary>Weekly: settle lease statements, bring finished leases home, and place regional pickup calls.</summary>
	public static void ProcessWeek(CompetitorManager manager, GameDate date) {
		if (!Enabled || manager == null || ChartManager.Instance == null) return;
		int week = ChartManager.Instance.GetCurrentChartWeek();
		foreach (AILabel seller in manager.GetOperatingLabels()) {
			if (seller.aiMasterLeases == null || seller.aiMasterLeases.Count == 0) continue;
			for (int i = seller.aiMasterLeases.Count - 1; i >= 0; i--) {
				var lease = seller.aiMasterLeases[i];
				var runtime = ChartManager.Instance.GetRecordRuntimeData(lease.RecordId);
				AILabel buyer = manager.GetLabel(lease.BuyerLabelId);
				bool ended = runtime == null || week >= lease.EndWeek || buyer == null || !buyer.IsActive;
				if (runtime != null) lease.Earned = Mathf.Max(lease.Earned,
					(runtime.totalUnitsSold - lease.UnitsAtStart) * PlayerDesk.ListPrice * lease.Rate);
				if (ended || week - lease.LastStatementWeek >= StatementWeeks) {
					float owed = Mathf.Max(0f, lease.Earned - lease.Advance - lease.Paid);
					if (owed > 0f && buyer != null && buyer.IsActive) { manager.PayMasterAdvance(buyer, seller, owed); lease.Paid += owed; }
					lease.LastStatementWeek = week;
				}
				if (!ended) continue;
				seller.aiMasterLeases.RemoveAt(i);
				if (runtime != null && seller.IsActive && runtime.baseRecord.labelId == lease.BuyerLabelId) {
					manager.TransferRecordToLabel(runtime, seller, null);
					Log("revert", "lease", seller, buyer, lease.RecordId, lease.Paid, lease.Rate, week);
				}
			}
		}
		CheckPickups(manager, week, date);
	}

	private static void CheckPickups(CompetitorManager manager, int week, GameDate date) {
		foreach (RecordRuntimeData runtime in ChartManager.Instance.GetAllRecords().ToList()) {
			Record record = runtime?.baseRecord;
			if (record == null || record.isPlayerOwned || record.format != ReleaseFormat.Single) continue;
			if (runtime.weeksSinceRelease < PickupMinWeek || runtime.weeksSinceRelease > PickupMaxWeek) continue;
			if (runtime.currentPosition > 0 && runtime.currentPosition <= 40) continue;
			int markets = PickupMarkets(runtime, RegionalHitGate);
			if (markets == 0) continue;
			if (pickupEvidence.TryGetValue(record.recordId, out int seen) && seen >= markets) continue;
			pickupEvidence[record.recordId] = markets;
			AILabel seller = manager.GetLabel(record.labelId);
			if (!IsSeller(seller) || seller.aiMasterLeases?.Any(l => l.RecordId == record.recordId) == true) continue;
			if (RegionalHitGate && manager.GetAllLabels().Any(l => l.aiMasterLeases?.Any(lease => lease.RecordId == record.recordId) == true)) continue;
			var best = Buyers(seller, record.primaryGenre, date.year, PlayerDesk.MasterLeaseAdvanceMin)
				.Where(b => !RegionalHitGate || IsNationalBuyer(seller, b.Buyer))
				.Select(b => (b.Buyer, Offer: Price(b.Buyer, b.OwnSound, Read(b.Buyer, record, runtime, posted: false), runtime,
					StudioParentLabels.PickupPremium(b.Buyer, PlayerDesk.MasterLeaseAdvanceMin))))
				.Where(x => x.Offer.Strength >= 0f)
				.OrderByDescending(x => x.Offer.Strength).ThenBy(x => x.Buyer.labelId, StringComparer.Ordinal)
				.FirstOrDefault();
			if (best.Buyer == null) continue;
			float chance = Mathf.Clamp(0.35f + 0.15f * (markets - 1) + 0.30f * best.Offer.Strength, 0f, 0.85f);
			if (PhiloxCounterRandom.Unit(BandLife.WorldSeed, record.recordId, "pickup", markets, PickupDecision) >= chance) continue;
			float fundingNeed = PickupWorkingCapitalNeed(runtime);
			float freeCash = seller.cashReserves - (EconomicLabelLifecycle.Enabled
				? EconomicLabelLifecycle.CommitmentReserve(seller, signing: false) : seller.GetMonthlyOverhead());
			if (CapacityDecisions ? fundingNeed <= Math.Max(0f, freeCash) : !WouldLetGo(seller)) continue;
			float sale = PlayerDesk.Round25(best.Offer.Lease * PlayerDesk.MasterSaleMultiple);
			bool sell = seller.cashReserves < 2f * Math.Max(1f, seller.GetMonthlyOverhead()) && StudioParentLabels.CanFund(best.Buyer, sale);
			float advance = sell ? sale : best.Offer.Lease;
			if (!StudioParentLabels.CanFund(best.Buyer, advance)) continue;
			StudioParentLabels.FundAcquisition(best.Buyer, advance, "master-pickup", date);
			manager.PayMasterAdvance(best.Buyer, seller, advance);
			manager.TransferRecordToLabel(runtime, best.Buyer, null);
			if (!sell) {
				(seller.aiMasterLeases ??= new List<AiMasterLease>()).Add(new AiMasterLease {
					RecordId = record.recordId, BuyerLabelId = best.Buyer.labelId, Advance = advance, Rate = best.Offer.Rate,
					StartWeek = week, EndWeek = week + PlayerDesk.MasterLeaseWeeks, UnitsAtStart = runtime.totalUnitsSold, LastStatementWeek = week });
			}
			Log("pickup", sell ? "sale" : "lease", seller, best.Buyer, record.recordId, advance, sell ? 0f : best.Offer.Rate, week);
			GD.Print($"AI_MASTER_PICKUP_EVIDENCE week={week} record={record.recordId} age={runtime.weeksSinceRelease} " +
				$"units={runtime.unitsThisWeek} totalUnits={runtime.totalUnitsSold} stageMarkets={PlayerDesk.BreakoutMarkets(runtime)} " +
				$"hitMarkets={PickupMarkets(runtime, true)} sellerCash={seller.cashReserves:F0} sellerReach={seller.effectiveNationalReach:F3} " +
				$"fundingNeed={fundingNeed:F0} freeCash={freeCash:F0}");
		}
	}

	internal static int PickupMarkets(RecordRuntimeData runtime, bool requireHit) => requireHit
		? runtime?.regionalData?.Values.Count(data => RegionalHitDealGate.IsRecent(data, runtime.weeksSinceRelease)) ?? 0
		: PlayerDesk.BreakoutMarkets(runtime);

	internal static bool IsNationalBuyer(AILabel seller, AILabel buyer) =>
		buyer.tier is LabelTier.Major or LabelTier.MidTier or LabelTier.Independent
		&& buyer.effectiveNationalReach > seller.effectiveNationalReach;

	/// <summary>Cash for one plant-credit interval at observed weekly sales, or current open dealer orders,
	/// whichever is larger, after usable stock. This is a disclosed scenario rule, not a historical rate
	/// or a forecast of the hidden commercial-fit draw. The ordinary reserve protects existing liabilities.</summary>
	internal static float PickupWorkingCapitalNeed(RecordRuntimeData runtime) {
		long shelves = runtime.regionalData.Values.Where(d => d != null).Sum(d => (long)d.unitsInStores);
		long orders = runtime.regionalData.Values.Where(d => d != null).Sum(d => (long)d.unitsBackordered);
		long stock = shelves + (runtime.physicalInventory?.Warehouse ?? 0);
		long demand = Math.Max((long)Math.Max(0, runtime.unitsThisWeek) * LabelPressingEconomics.PlantTermWeeks, orders);
		int copies = (int)Math.Clamp(demand - stock, 0L, int.MaxValue);
		return LabelPressingEconomics.PlantBill(ReleaseFormat.Single, copies, repress: true,
			LabelPressingEconomics.UnitPressingCost(ReleaseFormat.Single, .30f));
	}

	/// <summary>Cash a label needs from a distress sale: its overdraft plus one month of mandatory overhead, and only
	/// when last month already closed negative, so the coming month-end would be its second and close it.</summary>
	internal static float DistressNeed(AILabel seller) => DistressNeed(seller, EconomicLabelLifecycle.Enabled);

	internal static float DistressNeed(AILabel seller, bool economicLifecycle) =>
		DistressSales && IsSeller(seller) && economicLifecycle && seller.insolventMonthEnds >= 1 && seller.cashReserves < 0f
			? -seller.cashReserves + Math.Max(1f, seller.GetMonthlyOverhead()) : 0f;

	/// <summary>What a buyer pays for a live master from its own sales evidence: current weekly units carried at the
	/// observed week-on-week rate (capped; a climb is valued as if at its peak), times the record's realized label net
	/// per unit, discounted as a forced sale. Reads no hidden score and draws nothing.</summary>
	internal static float DistressValue(RecordRuntimeData runtime) {
		if (runtime == null || runtime.unitsThisWeek <= 0 || runtime.totalUnitsSold <= 0) return 0f;
		float netPerUnit = Math.Max(0f, runtime.lifetimeLabelNet / runtime.totalUnitsSold);
		float carry = runtime.weeksSincePeakUnits == 0 || runtime.unitsPreviousWeek <= 0 ? DistressMaxCarry
			: Mathf.Clamp(runtime.unitsThisWeek / (float)runtime.unitsPreviousWeek, 0f, DistressMaxCarry);
		float remainingUnits = runtime.unitsThisWeek * carry / (1f - carry);
		return PlayerDesk.Round25(remainingUnits * netPerUnit * DistressDiscount);
	}

	/// <summary>
	/// At a month close, before the solvency review: an independent that would close at this month-end offers its live
	/// singles to bigger firms, most valuable first. A buyer whose ear accepts the record pays the evidence price from cash
	/// above its own reserve. The seller sells only a record whose price clears its need; otherwise nothing happens and
	/// the ordinary review bankrupts it. Receivables already billed stay with the seller; the act stays on its roster.
	/// Returns the proceeds, which the caller keeps out of operating cash flow.
	/// </summary>
	public static float TryDistressSale(CompetitorManager manager, AILabel seller, GameDate date) {
		float need = DistressNeed(seller);
		if (need <= 0f || manager == null || ChartManager.Instance == null) return 0f;
		var leased = new HashSet<string>(manager.GetAllLabels().Where(l => l.aiMasterLeases != null)
			.SelectMany(l => l.aiMasterLeases.Select(lease => lease.RecordId)), StringComparer.Ordinal);
		var masters = ChartManager.Instance.GetAllRecords()
			.Where(r => r?.baseRecord != null && r.baseRecord.labelId == seller.labelId && !r.baseRecord.isPlayerOwned
				&& r.baseRecord.format == ReleaseFormat.Single && !leased.Contains(r.baseRecord.recordId))
			.Select(r => (Runtime: r, Value: DistressValue(r)))
			.Where(x => x.Value >= need)
			.OrderByDescending(x => x.Value).ThenBy(x => x.Runtime.baseRecord.recordId, StringComparer.Ordinal)
			.ToList();
		int week = ChartManager.Instance.GetCurrentChartWeek();
		foreach (var (runtime, value) in masters) {
			Record record = runtime.baseRecord;
			foreach (var (buyer, ownSound) in Buyers(seller, record.primaryGenre, date.year, value).Take(MaxApproaches)) {
				if (Price(buyer, ownSound, Read(buyer, record, runtime, posted: false), runtime).Strength < 0f) continue;
				float cashBefore = seller.cashReserves;
				StudioParentLabels.FundAcquisition(buyer, value, "master-distress", date);
				manager.PayMasterAdvance(buyer, seller, value);
				manager.TransferRecordToLabel(runtime, buyer, null);
				Log("distress", "sale", seller, buyer, record.recordId, value, 0f, week);
				GD.Print($"AI_MASTER_DISTRESS_EVIDENCE week={week} record={record.recordId} age={runtime.weeksSinceRelease} " +
					$"position={runtime.currentPosition} units={runtime.unitsThisWeek} totalUnits={runtime.totalUnitsSold} " +
					$"sellerCash={cashBefore:F0} need={need:F0} price={value:F0} receivables={seller.outstandingWholesaleReceivables:F0} " +
					$"insolventMonthEnds={seller.insolventMonthEnds}");
				return value;
			}
		}
		return 0f;
	}

	/// <summary>A seller lets a breaking record go when it cannot work it nationally: a Small or Boutique label, or an
	/// Independent without three months of overhead in hand.</summary>
	private static bool WouldLetGo(AILabel seller) => seller.tier is LabelTier.Small or LabelTier.Boutique
		|| seller.cashReserves < 3f * Math.Max(1f, seller.GetMonthlyOverhead());

	private static void Log(string route, string kind, AILabel seller, AILabel buyer, string recordId, float amount, float rate, int week) =>
		GD.Print($"AI_MASTER_DEAL week={week} route={route} kind={kind} seller={seller?.labelId} sellerTier={seller?.tier} " +
			$"buyer={buyer?.labelId} buyerTier={buyer?.tier} record={recordId} amount={amount:F0} rate={rate:F2}");
}
