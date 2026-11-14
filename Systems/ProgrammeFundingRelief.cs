using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Two switches on how a non-Major AI label funds its next release when the money it earned is still owed to it.
/// Off, a label with a hit and its sales on house or one-stop terms reserves tax on income it has booked but not
/// collected, plus twice its recent commercial spend, before a release counts as funded; Independent launches fell
/// 13-22% under chart-proven wholesale lines with bankruptcies flat (SimTools/MajorTop10ShareFableHandoff.md).
/// <list type="bullet">
/// <item>--slate-going-concern-reserve-all-tiers: the --slate-going-concern-reserve rule (hold only the tax that leaves
/// within the next two month-ends; no commercial hold) applies to every work-budget label, not only MidTiers.</item>
/// <item>--programme-overdraft-funding: a release by an act with a recent Top 40 may draw the label's unused
/// receivable-backed overdraft (ReceivableBackedOverdraft.Share x receivables, less any overdraft already drawn) for
/// the cash it is short -- the label pressing the follow-up on its credit line.</item>
/// </list>
/// Both are off by default and draw no random numbers.
/// </summary>
public static class ProgrammeFundingRelief {
	public const string AllTiersSwitch = "--slate-going-concern-reserve-all-tiers";
	public const string OverdraftSwitch = "--programme-overdraft-funding";
	private static bool configured;
	public static bool AllTiers { get; private set; }
	public static bool OverdraftFunding { get; private set; }

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		var args = (arguments ?? Array.Empty<string>()).ToList();
		AllTiers = args.Contains(AllTiersSwitch);
		OverdraftFunding = args.Contains(OverdraftSwitch);
		if (AllTiers && !args.Contains(LabelReleaseProgramme.GoingConcernReserveSwitch))
			throw new ArgumentException(AllTiersSwitch + " requires " + LabelReleaseProgramme.GoingConcernReserveSwitch);
		if (OverdraftFunding && !args.Any(a => a.StartsWith(ReceivableBackedOverdraft.Switch + "=", StringComparison.Ordinal)))
			throw new ArgumentException(OverdraftSwitch + " requires " + ReceivableBackedOverdraft.Switch);
		if (AllTiers || OverdraftFunding) Godot.GD.Print($"PROGRAMME_FUNDING_RELIEF allTiers={AllTiers} overdraftFunding={OverdraftFunding}");
		configured = true;
	}

	internal static void SetForChecks(bool allTiers, bool overdraftFunding) { configured = true; AllTiers = allTiers; OverdraftFunding = overdraftFunding; }

	/// <summary>Unused overdraft headroom: Share x receivables less the overdraft already drawn. 0 when off.</summary>
	public static float OverdraftHeadroom(float cash, float receivables) =>
		!OverdraftFunding ? 0f : Math.Max(0f, ReceivableBackedOverdraft.Share * Math.Max(0f, receivables) - Math.Max(0f, -cash));

	/// <summary>What a release short by <paramref name="shortfall"/> may draw: the shortfall in full if the headroom covers
	/// it, else nothing (a partial draw would leave the slot short). 0 when off.</summary>
	public static float Draw(float shortfall, float cash, float receivables) {
		if (!OverdraftFunding || shortfall <= 0f) return 0f;
		return OverdraftHeadroom(cash, receivables) >= shortfall ? shortfall : 0f;
	}
}
