using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

/// <summary>
/// AI unit economics on the period price ladder (SimTools/SmallLabelAttritionAndChartMethod.md).
/// Enabled by default; --no-dealer-price-pressing retains the old counter-price comparison.
///
/// Price: an AI label used to book $0.89 / $3.98 a unit -- the counter price -- and then pay a distributor skim
/// on top, so it kept the dealer's margin as well as its own. It now books what a dealer pays, as the player's
/// shop sales already do (PlayerDesk.DealerPrice), and the existing skim stays the distributor's cut of that.
/// Artist royalty and the publishing pool stay on the old list basis: the period paid the act on list, not on
/// whichever channel moved the copy.
///
/// Pressing: manufacturing used to follow copies SOLD, so a flop cost nothing to make and no record ever left
/// unsold stock behind. The label now keeps a pressing book apart from the stores' shelf stock (that stock is a
/// demand-side availability device sized around 2.5x lifetime sales, so it cannot double as a pressing run).
/// It presses an initial run against its own forecast when the record first sells, re-presses in lots when
/// sales catch up, and is billed for every copy pressed. Copies pressed and never sold are unsold booked
/// stock, not measured customer returns. The initial run is a release-time commitment and joins sunk
/// production cost; re-presses follow sales
/// and come out of the record's net, so the format memory sees both without extrapolating a week-one bill.
/// --physical-pressing-inventory replaces this replenishment rule with received warehouse stock,
/// bounded shipments and delayed orders. Press then books sell-through and drains receipt invoices only.
/// </summary>
public static class LabelPressingEconomics {
	public const string Switch = "--dealer-price-pressing";
	/// <summary>Plant credit (§11): the plant bills on 30-day terms instead of cash with the order.</summary>
	public const string PlantCreditSwitch = "--plant-credit";
	public const string PeriodSingleCostsSwitch = "--period-single-costs";
	public const string PeriodCostsSwitch = "--period-pressing-costs";
	public const string LegacyCostsSwitch = "--legacy-pressing-costs";
	private static bool configured, enabled = true, plantCredit = true, periodSingleCosts = true, periodAlbumCosts = true;
	public static bool Enabled => enabled;
	public static bool PlantCredit => enabled && plantCredit;
	public static bool PeriodSingleCosts => enabled && periodSingleCosts;
	public static bool PeriodAlbumCosts => enabled && periodAlbumCosts;
	public static float InitialCommitment { get; private set; } = InitialRunShareOfForecast;
	public static float RepressWeeks { get; private set; } = RepressCoverWeeks;
	public static float AlbumInitialCommitment { get; private set; } = InitialRunShareOfForecast;
	public static float AlbumRepressWeeks { get; private set; } = RepressCoverWeeks;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		enabled = !args.Contains("--no-dealer-price-pressing");
		plantCredit = !args.Contains("--no-plant-credit");
		bool legacyCosts = args.Contains(LegacyCostsSwitch) || args.Contains("--legacy-single-costs");
		bool singleOnly = args.Contains(PeriodSingleCostsSwitch), paired = args.Contains(PeriodCostsSwitch);
		if (legacyCosts && (singleOnly || paired) || singleOnly && paired)
			throw new ArgumentException("Conflicting pressing-cost switches.");
		periodSingleCosts = !legacyCosts;
		periodAlbumCosts = !legacyCosts && !singleOnly;
		if ((args.Contains(Switch) && !enabled) || (args.Contains(PlantCreditSwitch) && !plantCredit))
			throw new ArgumentException("Conflicting pressing/plant-credit switches.");
		if (args.Contains(PlantCreditSwitch) && !enabled)
			throw new ArgumentException(PlantCreditSwitch + " requires " + Switch + ".");
		if ((singleOnly || paired) && !enabled)
			throw new ArgumentException("Period pressing costs require " + Switch + ".");
		InitialCommitment = ReadParameter(args, "--pressing-initial-forecast-share=", InitialRunShareOfForecast, .5f, 2f);
		RepressWeeks = ReadParameter(args, "--pressing-repress-weeks=", RepressCoverWeeks, 3f, 26f);
		AlbumInitialCommitment = ReadParameter(args, "--pressing-album-initial-forecast-share=", InitialCommitment, .5f, 2f);
		AlbumRepressWeeks = ReadParameter(args, "--pressing-album-repress-weeks=", RepressWeeks, 3f, 26f);
		if (!enabled && (InitialCommitment != InitialRunShareOfForecast || RepressWeeks != RepressCoverWeeks
			|| AlbumInitialCommitment != InitialRunShareOfForecast || AlbumRepressWeeks != RepressCoverWeeks))
			throw new ArgumentException("Pressing commitments require " + Switch + ".");
		configured = true;
	}

	internal static float ReadParameter(string[] args, string prefix, float fallback, float minimum, float maximum) {
		string[] values = args.Where(a => a.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return fallback;
		if (values.Length != 1 || !float.TryParse(values[0][prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
			|| !float.IsFinite(value) || value < minimum || value > maximum)
			throw new ArgumentException($"{prefix} requires one finite value in [{minimum}, {maximum}].");
		return value;
	}

	internal static void SetCommitmentsForChecks(float initialShare, float repressWeeks) {
		InitialCommitment = initialShare;
		RepressWeeks = repressWeeks;
		AlbumInitialCommitment = initialShare;
		AlbumRepressWeeks = repressWeeks;
	}
	internal static void SetAlbumCommitmentsForChecks(float initialShare, float repressWeeks) {
		AlbumInitialCommitment = initialShare; AlbumRepressWeeks = repressWeeks;
	}

	internal static void SetEnabledForChecks(bool value) { configured = true; enabled = value; }
	internal static void SetPlantCreditForChecks(bool value) { configured = true; plantCredit = value; }
	internal static void SetPeriodSingleCostsForChecks(bool value) { configured = true; periodSingleCosts = value; }
	internal static void SetPeriodAlbumCostsForChecks(bool value) { configured = true; periodAlbumCosts = value; }

	// The old 30c COGS bundled composition costs into manufacturing. Keep two sides'
	// 2c mechanical allowance on SOLD copies; it must not become part of the plant invoice.
	// Publishing-control transfers retain their existing routing here.
	public static float SingleCompositionCost => 2f * MechanicalRoyaltyService.RatePerCopy;
	// The generator makes 9-13 tracks (mean 11). Opening/external records without
	// track snapshots use the same conservative allowance; live albums use their tracks.
	public static float ExpectedAlbumCompositionCost => 11f * MechanicalRoyaltyService.RatePerCopy;
	public static float CompositionUnitCost(Record record) {
		if (record == null) return 0f;
		if (UsesPeriodSingleCosts(record.format)) return SingleCompositionCost;
		if (!PeriodAlbumCosts || record.format != ReleaseFormat.Album) return 0f;
		int tracks = (record.album?.trackRefs?.Length ?? 0) + (record.album?.nonSingleTracks?.Length ?? 0);
		return tracks > 0 ? tracks * MechanicalRoyaltyService.RatePerCopy : ExpectedAlbumCompositionCost;
	}
	public static bool UsesPeriodSingleCosts(ReleaseFormat format) => PeriodSingleCosts && format == ReleaseFormat.Single;
	public static float UnitPressingCost(ReleaseFormat format, float legacyCost) =>
		UsesPeriodSingleCosts(format) ? PlayerDesk.PressVinylPerUnit + PlayerDesk.PressSleeveLabelPerUnit :
		PeriodAlbumCosts && format == ReleaseFormat.Album ? .40f : legacyCost;
	public static float PlantBill(ReleaseFormat format, int copies, bool repress, float unitCost) {
		if (copies <= 0) return 0f;
		if (UsesPeriodSingleCosts(format)) return PlayerDesk.PressingCost(copies, repress);
		// Billboard 1960-06-20: custom vinyl LPs normally about 40c, before jackets.
		// Caller includes its existing per-unit packaging cost. Setup/shipping reuse
		// the player's order assumptions, rather than fitting a new tariff to closures.
		return copies * unitCost + (PeriodAlbumCosts && format == ReleaseFormat.Album
			? PlayerDesk.PressShipping + (repress ? 0f : PlayerDesk.PressLacquerSetup) : 0f);
	}

	// Model assumption: "net 30" read as four sim weeks. Founders ran on plant and distributor credit
	// (SimTools/SmallLabelAttritionAndChartMethod.md §8); no measured period term backs the length.
	public const int PlantTermWeeks = 4;

	/// <summary>Bills this week's plant run and pays whatever falls due. Without credit the whole bill is paid now.
	/// With credit it becomes a payable due in <see cref="PlantTermWeeks"/>, and older payables now due are paid.
	/// Returns the cash paid this week. Consumes no random draws.</summary>
	public static float SettlePlantAccount(AILabel label, float billedThisWeek, int currentWeek) {
		if (!PlantCredit || label == null || label.isPlayerOwned) return billedThisWeek;
		label.plantPayables ??= new List<PlantPayable>();
		if (billedThisWeek > 0f) {
			label.plantPayables.Add(new PlantPayable(currentWeek + PlantTermWeeks, billedThisWeek));
			label.outstandingPlantPayables += billedThisWeek;
		}
		float paid = 0f;
		for (int index = label.plantPayables.Count - 1; index >= 0; index--) {
			if (label.plantPayables[index].DueWeek > currentWeek) continue;
			paid += label.plantPayables[index].Amount;
			label.plantPayables.RemoveAt(index);
		}
		label.outstandingPlantPayables = label.plantPayables.Count == 0 ? 0f : Math.Max(0f, label.outstandingPlantPayables - paid);
		return paid;
	}

	// What a dealer paid against suggested list on the player's 45 ladder (0.63 / 0.98), applied to every format.
	public static readonly float DealerShareOfList = PlayerDesk.DealerPrice / PlayerDesk.ListPrice;

	// Model assumptions, not measured period terms. A plant's minimum economic run of a 45 or an LP; the share of
	// its own forecast a label committed before orders came in; re-press lots sized to weeks of current sales.
	public const int MinimumRun = 500;
	public const float InitialRunShareOfForecast = 0.5f;
	public const float RepressCoverWeeks = 3f;
	public const float ForecastMemoryAlpha = 0.3f;
	// A record first booked later than this was already in the shops (prewarm catalog): what sold before the book
	// opened was pressed and paid for before it, so it pays no fresh initial run. Opening catalog (on sale at
	// the run's first day) also starts with stock on hand; a live record first selling late does not.
	public const int NewReleaseMaxAgeWeeks = 2;

	public static float LabelPrice(ReleaseFormat format, float listPrice) =>
		format == ReleaseFormat.Single ? PlayerDesk.DealerPrice : listPrice * DealerShareOfList;

	/// <summary>The label's forecast of a release's lifetime copies: geometric memory of its own retired records,
	/// so one smash does not set the next run. No history means no forecast, and the plant minimum.</summary>
	public static float ForecastUnits(AILabel label, ReleaseFormat format) {
		float log = format == ReleaseFormat.Album ? label?.pressingForecastAlbumLogUnits ?? 0f : label?.pressingForecastSingleLogUnits ?? 0f;
		return log > 0f ? MathF.Exp(log) - 1f : 0f;
	}

	/// <summary>Cash a label must keep back to pay for a release's initial run. It is a reservation at the
	/// go/no-go decision, not a charge: the plant bills when the record first ships. Zero while switched off.</summary>
	public static float InitialRunReserve(AILabel label, ReleaseFormat format, float unitPressingCost) =>
		enabled && label != null && !label.isPlayerOwned
			? PlantBill(format, InitialCopies(label, format), false, unitPressingCost)
			: 0f;

	public readonly struct PressingWeek {
		public readonly int InitialRun, Repressed;
		public PressingWeek(int initialRun, int repressed) { InitialRun = initialRun; Repressed = repressed; }
	}

	/// <summary>Books this week's sold copies. The default book replenishes after sales; the opt-in
	/// physical book only invoices receipts already manufactured before shipment.</summary>
	public static PressingWeek Press(RecordRuntimeData record, AILabel label, int unitsSold) {
		if (PhysicalPressingInventory.Applies(record)) return PhysicalPressingInventory.BookSales(record, unitsSold);
		int initial = 0;
		int sold = Math.Max(0, unitsSold);
		if (!record.pressingBookOpened) {
			record.pressingBookOpened = true;
			if (record.weeksSinceRelease <= NewReleaseMaxAgeWeeks) {
				initial = InitialCopies(label, record.baseRecord?.format ?? ReleaseFormat.Single);
				record.unitsPressed = initial;
			} else if (OnSaleAtRunStart(record)) {
				// Opening catalog was pressed and paid for before 1960: this week's copies already exist, and part
				// of the last pre-1960 lot is still in the warehouse. Without this every catalog record re-pressed
				// three weeks of sales in week one (1.33x that week's gross). The lot phase is per record so the
				// catalog's first re-presses spread over the cycle instead of landing together.
				// A calibration of NEW orders must not grant extra free opening inventory.
				record.unitsPressed = sold + (int)(LotPhase(record.baseRecord?.recordId) *
					Math.Max(MinimumRun, (int)MathF.Ceiling(sold * RepressCoverWeeks)));
			}
		}
		record.pressingBookSold += sold;
		int before = record.unitsPressed;
		int lot = RepressLot(sold, record.baseRecord?.format ?? ReleaseFormat.Single);
		while (record.unitsPressed < record.pressingBookSold) record.unitsPressed += lot;
		return new PressingWeek(initial, record.unitsPressed - before);
	}

	internal static int InitialCopies(AILabel label, ReleaseFormat format) =>
		Math.Max(MinimumRun, (int)MathF.Round((format == ReleaseFormat.Album ? AlbumInitialCommitment : InitialCommitment) * ForecastUnits(label, format)));
	internal static int RepressLot(int weeklySold, ReleaseFormat format) => Math.Max(MinimumRun,
		(int)MathF.Ceiling(weeklySold * (format == ReleaseFormat.Album ? AlbumRepressWeeks : RepressWeeks)));

	/// <summary>Released on or before the run's first day: seeded opening catalog, not a live release that
	/// happened to sell late.</summary>
	private static bool OnSaleAtRunStart(RecordRuntimeData record) =>
		record.baseRecord != null && record.baseRecord.releaseDate.year > 0 && record.baseRecord.releaseDate <= GameDate.StartDate;

	/// <summary>Where in its re-press cycle a catalog record stood on day one, in [0, 1): FNV-1a of the record id,
	/// so it is stable across runs and saves and consumes no random draws. FNV's high bits barely move between
	/// ids that differ in the last digit (gen_1, gen_2, ...), so the fmix64 finalizer spreads them.</summary>
	private static float LotPhase(string recordId) {
		ulong hash = 14695981039346656037UL;
		foreach (char value in recordId ?? string.Empty) { hash ^= value; hash *= 1099511628211UL; }
		hash ^= hash >> 33; hash *= 0xff51afd7ed558ccdUL; hash ^= hash >> 33; hash *= 0xc4ceb9fe1a85ec53UL; hash ^= hash >> 33;
		return (hash >> 40) / (float)(1UL << 24);
	}

	/// <summary>Folds a retired record's lifetime sales into its label's forecast for that format.</summary>
	public static void Learn(AILabel label, RecordRuntimeData record) {
		if (label == null || label.isPlayerOwned || record == null || !record.pressingBookOpened) return;
		float observed = MathF.Log(1f + Math.Max(0, record.pressingBookSold));
		if (record.baseRecord?.format == ReleaseFormat.Album)
			label.pressingForecastAlbumLogUnits = Blend(label.pressingForecastAlbumLogUnits, observed);
		else label.pressingForecastSingleLogUnits = Blend(label.pressingForecastSingleLogUnits, observed);
	}

	private static float Blend(float previous, float observed) =>
		previous > 0f ? previous + ForecastMemoryAlpha * (observed - previous) : observed;
}

/// <summary>A pressing-plant invoice on credit terms (<see cref="LabelPressingEconomics.PlantCredit"/>).</summary>
public struct PlantPayable {
	public int DueWeek;
	public float Amount;

	public PlantPayable(int dueWeek, float amount) {
		DueWeek = dueWeek;
		Amount = amount;
	}
}
