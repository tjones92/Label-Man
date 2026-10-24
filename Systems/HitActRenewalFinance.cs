using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Optional finance probe, not a free renewal. A profitable MidTier's recent hit can use the same
/// going-concern tax/service horizon as its funded release slate. Keeps operating, plant and slate
/// holds; pays the actual advance and all taxes normally. Fresh signings and distressed firms unchanged.
/// </summary>
public static class HitActRenewalFinance {
	public const string Switch = "--hit-act-going-concern-renewal";
	public const string AuditSwitch = "--audit-hit-act-renewals";
	public static bool Enabled { get; private set; }
	public static bool Auditing { get; private set; }
	private static bool configured;
	private static readonly Dictionary<string, (double Cash, double Reserve)> pending = new(StringComparer.Ordinal);
	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		Enabled = args.Contains(Switch); Auditing = Enabled || args.Contains(AuditSwitch);
		if (Enabled && (!HitActCadence.RetentionEnabled || !LabelReleaseProgramme.GoingConcernReserveEnabled))
			throw new ArgumentException(Switch + " requires hit-act retention and the going-concern release-slate reserve.");
		configured = true;
	}
	internal static double Reserve(double original, double fullTaxAndService, double nearTaxDue) =>
		Math.Max(0, original - fullTaxAndService + Math.Max(0, nearTaxDue));
	internal static bool Eligible(bool isActive, bool isPlayer, bool usesGoingConcernSlate, double cashFlow,
		int contractHits, int weeksSinceHit) => isActive && !isPlayer && usesGoingConcernSlate && cashFlow >= 0 &&
		contractHits > 0 && weeksSinceHit >= 0 && weeksSinceHit <= 52;
	public static bool CanAfford(AILabel label, SimulatedArtist artist, bool wantsRenewal, GameDate date) {
		// Preserve the former short circuit and all original price reads when this probe is absent.
		if (!Enabled && !Auditing) return wantsRenewal && label.CanAffordToSign(label.CalculateAdvanceOffer(artist));
		float advance = label.CalculateAdvanceOffer(artist);
		bool original = wantsRenewal && label.CanAffordToSign(advance);
		double fullReserve = EconomicLabelLifecycle.Enabled && !label.isPlayerOwned
			? EconomicLabelLifecycle.CommitmentReserve(label, signing: true) : label.GetMonthlyOverhead() * 2f;
		int since = HitActCadence.WeeksSinceTop40(artist, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		bool eligible = Eligible(label.IsActive, label.isPlayerOwned || artist.isPlayerOwned,
			LabelReleaseProgramme.UsesGoingConcernReserve(label), label.RecentMonthlyCashFlow, artist.contractTop40Hits, since);
		double taxService = eligible ? LabelCorporateModel.TaxAndServiceReserve(label) : 0;
		double due = eligible ? LabelCorporateModel.ProgrammeTaxDue(label, date.month, 3) : 0;
		double goingReserve = eligible ? Reserve(fullReserve, taxService, due) : fullReserve;
		bool proposed = wantsRenewal && eligible && label.cashReserves - advance > goingReserve;
		bool result = original || Enabled && proposed;
		if (Enabled && !original && proposed) pending[artist.artistId] = (label.cashReserves, goingReserve);
		if (Auditing) {
			string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
			GD.Print($"HIT_ACT_RENEWAL week={ChartManager.Instance?.GetCurrentChartWeek() ?? 0} artist={artist.artistId} " +
				$"label={label.labelId} tier={label.tier} wants={wantsRenewal} original={original} eligible={eligible} " +
				$"proposed={proposed} applied={Enabled && !original && proposed} cash={F(label.cashReserves)} " +
				$"advance={F(advance)} fullReserve={F(fullReserve)} goingReserve={F(goingReserve)} " +
				$"taxService={F(taxService)} nearTaxDue={F(due)} cashFlow={F(label.RecentMonthlyCashFlow)} " +
				$"contractHits={artist.contractTop40Hits} sinceHit={since} audience={F(ArtistFanbase.Heads(artist.fanbase))}");
		}
		return result;
	}
	public static void RecordPaid(AILabel label, SimulatedArtist artist, float advance) {
		if (!Enabled || !pending.Remove(artist.artistId, out var before)) return;
		double tolerance = .1 + Math.Abs(before.Cash) * 1e-6;
		if (Math.Abs(before.Cash - label.cashReserves - advance) > tolerance || label.cashReserves + tolerance < before.Reserve)
			throw new InvalidOperationException("Supported renewal did not pay its advance or preserve its cash reserve.");
		GD.Print($"HIT_ACT_RENEWAL_PAID week={ChartManager.Instance?.GetCurrentChartWeek() ?? 0} artist={artist.artistId} " +
			$"label={label.labelId} advance={advance.ToString("0.######", CultureInfo.InvariantCulture)} " +
			$"cashAfter={label.cashReserves.ToString("0.######", CultureInfo.InvariantCulture)}");
	}
}
