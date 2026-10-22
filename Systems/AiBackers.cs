using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>An AI label's one backer agreement. Balances are running fields; payments only ever come from cash on hand.</summary>
public sealed class AiBackerLoan {
	public PlayerDesk.BackerSource Source { get; set; }
	public float Principal { get; set; }
	public float Rate { get; set; }
	public int SignedWeek { get; set; }
	/// <summary>Month index (year * 12 + month) of the first installment: the third 1st after signing.</summary>
	public int FirstPaymentMonth { get; set; }
	public int LastServicedMonth { get; set; }
	public float OutstandingPrincipal { get; set; }
	public float AccruedInterest { get; set; }
	public float InterestPaid { get; set; }
	public float PrincipalArrears { get; set; }
	public int InstallmentsDue { get; set; }
	public int MissedPayments { get; set; }
}

/// <summary>
/// Opt-in (<c>--ai-backers</c>): the player's one early backer agreement (EarlyGameLabelManDirectiveV2 §4.3), open to
/// founder-run AI Small and Boutique labels. The historical anchor is Gordy's $800 Ber-Berry family loan at 6%; the
/// sources are the player's four non-family analogues (club owner, merchant lender, jukebox operator, private backer) and
/// their $400-800 capacities, which the directive marks as authored. AI founding archetypes are musical styles, so each
/// label's source is a fixed keyed draw.
///
/// When the label has signed an act and recorded a single it cannot afford to press and promote, it asks once. The backer
/// never hears the record: he lends the completion budget (at least $300) if it is within his means, otherwise passes.
/// One agreement per label, ever. Same terms as the player's: 6% simple interest on outstanding principal, accrued daily,
/// twelve monthly principal installments from the third 1st after signing, each with the interest accrued; paid on the
/// first chart week of each month from cash on hand only, interest first, then arrears, then the installment. What isn't
/// paid stands as arrears. Principal is financing; interest is an operating cost. Off: nothing here runs.
/// </summary>
public static class AiBackers {
	public const string Switch = "--ai-backers";
	private const uint SourceDecision = 0x42410000U;
	private static bool configured;
	public static bool Enabled { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		Enabled = (arguments ?? Array.Empty<string>()).Contains(Switch);
		configured = true;
	}

	internal static void SetEnabledForChecks(bool enabled) { configured = true; Enabled = enabled; }

	public static bool Eligible(AILabel label) => Enabled && label != null && !label.isPlayerOwned && label.IsActive
		&& !label.IsSubsidiary && label.tier is (LabelTier.Small or LabelTier.Boutique) && label.aiBackerLoan == null
		&& label.CurrentRosterSize > 0;

	public static PlayerDesk.BackerSource SourceFor(AILabel label) {
		double unit = PhiloxCounterRandom.Unit(BandLife.WorldSeed, label.labelId, "backer", 0, SourceDecision);
		return (PlayerDesk.BackerSource)Math.Min(3, (int)(unit * 4));
	}

	private static int MonthIndex(GameDate date) => date.year * 12 + date.month;

	/// <summary>Ask the label's backer for the shortfall on a recorded single. True when he lent it.</summary>
	public static bool TryBorrow(AILabel label, float shortfall, GameDate date) {
		if (!Eligible(label) || shortfall <= 0f) return false;
		var source = SourceFor(label);
		float capacity = PlayerDesk.BackerCapacity(source);
		float principal = Mathf.Max(PlayerDesk.BackerMinimumPrincipal, Mathf.Ceil(shortfall / 25f) * 25f);
		if (principal > capacity) return false;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		label.aiBackerLoan = new AiBackerLoan {
			Source = source, Principal = principal, Rate = PlayerDesk.BackerRate, SignedWeek = week,
			FirstPaymentMonth = MonthIndex(date) + PlayerDesk.BackerGraceFirsts, LastServicedMonth = MonthIndex(date),
			OutstandingPrincipal = principal,
		};
		LabelCompanyAccounts.ChangeCash(label, principal, LabelCashKind.Financing);
		GD.Print($"AI_BACKER week={week} event=signed label={label.labelId} tier={label.tier} source={source} principal={principal:F0}");
		return true;
	}

	/// <summary>Weekly: accrue interest; on the first chart week of a month, take what is due from cash on hand.</summary>
	public static void ProcessWeek(CompetitorManager manager, GameDate date) {
		if (!Enabled || manager == null) return;
		int month = MonthIndex(date);
		foreach (AILabel label in manager.GetOperatingLabels()) {
			var loan = label.aiBackerLoan;
			if (loan == null || loan.OutstandingPrincipal <= 0f && loan.AccruedInterest <= 0f) continue;
			loan.AccruedInterest += loan.OutstandingPrincipal * loan.Rate * 7f / 365f;
			if (month <= loan.LastServicedMonth || month < loan.FirstPaymentMonth) continue;
			loan.LastServicedMonth = month;
			loan.InstallmentsDue++;
			float installment = loan.InstallmentsDue >= PlayerDesk.BackerInstallments
				? loan.OutstandingPrincipal - loan.PrincipalArrears
				: Mathf.Min(loan.Principal / PlayerDesk.BackerInstallments, loan.OutstandingPrincipal - loan.PrincipalArrears);
			loan.PrincipalArrears += Mathf.Max(0f, installment);
			float cash = Mathf.Max(0f, label.cashReserves);
			float interest = Mathf.Min(cash, loan.AccruedInterest);
			if (interest > 0f) {
				manager.RecordExpense(label, interest, LabelCashKind.OtherOperating);
				loan.AccruedInterest -= interest; loan.InterestPaid += interest; cash -= interest;
			}
			float principal = Mathf.Min(cash, loan.PrincipalArrears);
			if (principal > 0f) {
				LabelCompanyAccounts.ChangeCash(label, -principal, LabelCashKind.Financing);
				loan.PrincipalArrears -= principal; loan.OutstandingPrincipal -= principal;
			}
			if (loan.PrincipalArrears > .005f || loan.AccruedInterest > .005f) loan.MissedPayments++;
			if (loan.OutstandingPrincipal <= .005f && loan.AccruedInterest <= .005f)
				GD.Print($"AI_BACKER week={ChartManager.Instance?.GetCurrentChartWeek() ?? 0} event=repaid label={label.labelId} interest={loan.InterestPaid:F0} missed={loan.MissedPayments}");
		}
	}
}
