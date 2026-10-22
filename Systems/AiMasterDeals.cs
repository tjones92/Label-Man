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
		configured = true;
	}

	internal static void SetEnabledForChecks(bool enabled) { configured = true; Enabled = enabled; }

	public static bool IsSeller(AILabel label) => Enabled && label != null && !label.isPlayerOwned && label.IsActive
		&& !label.IsSubsidiary && label.tier is (LabelTier.Small or LabelTier.Boutique or LabelTier.Independent);

	private static IEnumerable<(AILabel Buyer, bool OwnSound)> Buyers(AILabel seller, Genre genre, int year, float advance) =>
		(CompetitorManager.Instance?.GetOperatingLabels() ?? new List<AILabel>())
			.Where(b => b != null && !b.isPlayerOwned && b.labelId != seller.labelId && (int)b.tier < (int)seller.tier
				&& b.CanAffordToSign(advance))
			.Select(b => (Buyer: b, Fit: PlayerDesk.SoundFit(b, genre, year)))
			.Where(x => x.Fit.HasValue)
			.Select(x => (x.Buyer, OwnSound: x.Fit.Value))
			.OrderBy(x => x.OwnSound ? 0 : 1).ThenBy(x => (int)x.Buyer.tier).ThenBy(x => x.Buyer.labelId, StringComparer.Ordinal);

	/// <summary>The buyer's ear: the player's MasterRead, with the noise drawn from the counter RNG.</summary>
	private static float Read(AILabel buyer, Record record, RecordRuntimeData released, bool posted) {
		float appeal = record.hookStrength * 0.60f + record.productionQuality * 0.40f;
		float unit = (float)PhiloxCounterRandom.Unit(BandLife.WorldSeed, record.recordId, buyer.labelId, 0, EarDecision);
		float noise = (unit * 2f - 1f) * Mathf.Lerp(0.30f, 0.10f, Mathf.Clamp(buyer.scoutingAbility, 0f, 1f)) * (posted ? 1.4f : 1f);
		int markets = PlayerDesk.BreakoutMarkets(released);
		float evidence = released == null ? 0f : Mathf.Min(0.12f, 0.04f * markets) + Mathf.Min(0.08f, released.totalUnitsSold / 5000f);
		return Mathf.Clamp(appeal + noise + evidence, 0f, 1f);
	}

	private static (float Strength, float Lease, float Rate) Price(AILabel buyer, bool ownSound, float read, RecordRuntimeData released) {
		float bar = PlayerDesk.MasterBar(buyer, ownSound);
		if (read < bar) return (-1f, 0f, 0f);
		float s = Mathf.InverseLerp(bar, bar + 0.25f, read);
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
			var (s, lease, rate) = Price(buyer, ownSound, Read(buyer, record, null, posted: true), null);
			if (s < 0f) continue;
			float sale = PlayerDesk.Round25(lease * PlayerDesk.MasterSaleMultiple);
			bool sell = s >= 0.5f && buyer.CanAffordToSign(sale);
			float advance = sell ? sale : lease;
			if (!buyer.CanAffordToSign(advance)) continue;
			LabelCompanyAccounts.PayProject(seller, productionCost, productionCost);
			manager.NoteProductionSpend(seller, productionCost);
			artist.unrecoupedAdvance += productionCost;
			manager.PayMasterAdvance(buyer, seller, advance);
			if (!manager.ReleaseShoppedAiMaster(buyer, artist, record, advance, perceivedQuality, date))
				throw new InvalidOperationException($"Shopped master '{record.recordId}' failed to release at {buyer.labelId}.");
			int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
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
			int markets = PlayerDesk.BreakoutMarkets(runtime);
			if (markets == 0) continue;
			if (pickupEvidence.TryGetValue(record.recordId, out int seen) && seen >= markets) continue;
			pickupEvidence[record.recordId] = markets;
			AILabel seller = manager.GetLabel(record.labelId);
			if (!IsSeller(seller) || seller.aiMasterLeases?.Any(l => l.RecordId == record.recordId) == true) continue;
			var best = Buyers(seller, record.primaryGenre, date.year, PlayerDesk.MasterLeaseAdvanceMin)
				.Select(b => (b.Buyer, Offer: Price(b.Buyer, b.OwnSound, Read(b.Buyer, record, runtime, posted: false), runtime)))
				.Where(x => x.Offer.Strength >= 0f)
				.OrderByDescending(x => x.Offer.Strength).ThenBy(x => x.Buyer.labelId, StringComparer.Ordinal)
				.FirstOrDefault();
			if (best.Buyer == null) continue;
			float chance = Mathf.Clamp(0.35f + 0.15f * (markets - 1) + 0.30f * best.Offer.Strength, 0f, 0.85f);
			if (PhiloxCounterRandom.Unit(BandLife.WorldSeed, record.recordId, "pickup", markets, PickupDecision) >= chance) continue;
			if (!WouldLetGo(seller)) continue;
			float sale = PlayerDesk.Round25(best.Offer.Lease * PlayerDesk.MasterSaleMultiple);
			bool sell = seller.cashReserves < 2f * Math.Max(1f, seller.GetMonthlyOverhead()) && best.Buyer.CanAffordToSign(sale);
			float advance = sell ? sale : best.Offer.Lease;
			if (!best.Buyer.CanAffordToSign(advance)) continue;
			manager.PayMasterAdvance(best.Buyer, seller, advance);
			manager.TransferRecordToLabel(runtime, best.Buyer, null);
			if (!sell) {
				(seller.aiMasterLeases ??= new List<AiMasterLease>()).Add(new AiMasterLease {
					RecordId = record.recordId, BuyerLabelId = best.Buyer.labelId, Advance = advance, Rate = best.Offer.Rate,
					StartWeek = week, EndWeek = week + PlayerDesk.MasterLeaseWeeks, UnitsAtStart = runtime.totalUnitsSold, LastStatementWeek = week });
			}
			Log("pickup", sell ? "sale" : "lease", seller, best.Buyer, record.recordId, advance, sell ? 0f : best.Offer.Rate, week);
		}
	}

	/// <summary>A seller lets a breaking record go when it cannot work it nationally: a Small or Boutique label, or an
	/// Independent without three months of overhead in hand.</summary>
	private static bool WouldLetGo(AILabel seller) => seller.tier is LabelTier.Small or LabelTier.Boutique
		|| seller.cashReserves < 3f * Math.Max(1f, seller.GetMonthlyOverhead());

	private static void Log(string route, string kind, AILabel seller, AILabel buyer, string recordId, float amount, float rate, int week) =>
		GD.Print($"AI_MASTER_DEAL week={week} route={route} kind={kind} seller={seller?.labelId} sellerTier={seller?.tier} " +
			$"buyer={buyer?.labelId} buyerTier={buyer?.tier} record={recordId} amount={amount:F0} rate={rate:F2}");
}
