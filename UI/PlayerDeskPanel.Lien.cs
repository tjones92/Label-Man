using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The lien on the desk (SimTools/LabelManInnerLifeDirective.md §3): THE PAPER on the OFFICE tab, the December sitting
/// with the holder's lawyer, and the voices' morning-after argument about the night. The rules live on
/// <see cref="PlayerDesk"/> (PlayerDesk.Lien.cs); this only draws them.
/// </summary>
public partial class PlayerDeskPanel {
	private const int OfficeTab = 5;
	private bool lienMorningOpen;

	/// <summary>The holder is a name only once his lawyer has sat down with you, or his men have come on the 1st.</summary>
	private static bool LienHolderKnown(PlayerDesk desk) => desk.LienHolderIdentified;

	// ── THE PAPER (OFFICE tab) ───────────────────────────────────────────────────────────────────────

	private void PaperSection(PlayerDesk desk) {
		var lien = desk.Lien;
		if (lien == null) return;
		var p = desk.LienProfile;
		Heading("THE PAPER", "The lien you signed on New Year's Eve 1959. Each December the holder's lawyer sits down with you, " +
			"and the lien rolls to the next 1 January if you meet one bar: pay, show him the label's grown, or give something up. " +
			"Miss it and he seizes what he can and gives you three months; miss twice running and he takes the label. " +
			"The paper forbids paying it off before 1 January 1970.");

		GameDate due = lien.PendingDeadline ?? lien.Deadline;
		string holder = LienHolderKnown(desk) ? $"{p.Name}, {p.Title}" : "Not named. The letter only says \"for the holder\"";
		string rate = lien.Rate <= 0f ? "No interest" : $"{lien.Rate:P0} a year" + (p.Compounds ? ", unpaid interest goes onto the paper" : "");
		var rows = new List<LedgerTable.Row> {
			LedgerTable.Entry("Held by", holder),
			LedgerTable.Entry("His lawyer", p.Lawyer),
			LedgerTable.Entry("Face sum", LedgerTable.Money(lien.FaceSum)),
		};
		if (lien.PrincipalPaid > 0.5f) rows.Add(LedgerTable.Entry("Principal paid down", LedgerTable.Deduct(lien.PrincipalPaid)));
		rows.Add(LedgerTable.Entry("Interest", rate));
		float interestBy = lien.InterestDueBy(lien.Deadline);
		if (interestBy > 0.5f) rows.Add(LedgerTable.Entry($"Interest owed by {lien.Deadline.ToHeadlineString()}", LedgerTable.Money(interestBy)));
		rows.Add(LedgerTable.Total(lien.IsReckoning && !lien.Extended ? "Owed in full at the reckoning" : "Outstanding", LedgerTable.Money(lien.Outstanding)));
		content.AddChild(new LedgerTable().Set(lien.Extended ? $"Runs to {due.ToHeadlineString()}" : $"Falls due {due.ToHeadlineString()}", null, rows));

		Body("Gone since the night: " + string.Join(", ", desk.LienGoneList()) + ".");
		var reread = Btn("READ THE LETTER AGAIN");
		reread.Pressed += () => OpenLienMorning(PlayerDesk.Instance, replay: true);
		content.AddChild(reread);

		if (desk.GermainLedgerLead() is { } lead) {
			Body(lead);
			if (lien.GermainLedger == PlayerDesk.GermainLedgerStage.Found) {
				var read = Btn("READ JACK'S ACCOUNT BOOK (1h)");
				read.Disabled = desk.InstinctProfile.TheFixer < PlayerDesk.GermainLedgerFixer;
				if (read.Disabled) read.TooltipText = $"Fixer {PlayerDesk.GermainLedgerFixer} needed.";
				read.Pressed += () => Act(() => { bool ok = desk.ReadGermainLedger(out string m); Say(m, ok); return true; });
				content.AddChild(read);
			}
		}
		if (desk.LienHolderIdentified && lien.Holder == PlayerDesk.LienHolder.Floria && lien.BarYear >= 1967 && !lien.FloriaDismissed) {
			if (lien.FloriaBuyoutWaived) Body("New York dropped the purchase option. The lien still falls due at the reckoning.");
			else {
				var (value, regions) = desk.FloriaValuation();
				Body($"Purchase option review: the catalogue is valued at ${value:N0}, with meaningful sales in {regions} regions. " +
					$"New York turns it down at ${PlayerDesk.FloriaValuationBar:N0} and four regions.");
			}
		}

		if (lien.ConsecutiveFailures > 0)
			Warn($"You missed the last deadline. Miss {lien.Deadline.ToHeadlineString()} too and {(LienHolderKnown(desk) ? p.Name : "the holder")} takes the label.");

		string block = desk.LienSittingBlock();
		if (!lien.Extended) {
			// What the bars are this year, in plain figures. The voices' reads at the sitting are about what he'll stress.
			if (desk.LienHolderIdentified && desk.FloriaPurchaseOptionPending) Body("Floria's firm is reviewing its purchase option at this sitting.");
			else if (lien.IsReckoning) Body($"1 January 1970 is the reckoning: only money counts, ${desk.LienPayBar():N0} of it.");
			else {
				var show = desk.LienShow();
				Body(desk.LienRefusesPay
					? $"Pay: he won't take money at the sitting any more.  •  Show: {show.What}"
					: $"Pay: ${desk.LienPayBar():N0} (the interest and this year's slice).  •  Show: {show.What}");
				// Her name is on the paper, so his is: what Dee-Dee wants for Connie, and what an unmet ask does to Pay.
				if (desk.Connie != null) {
					var ask = desk.ConnieAsk();
					Body($"Dee-Dee wants: {ask.What}" + (ask.Met ? "" : $" She hasn't moved, so money costs {PlayerDesk.ConnieStalledPayMultiple:0.#}× this year."));
				}
			}
		}
		if (block == null) {
			var sit = Primary($"SIT DOWN WITH {p.Lawyer.ToUpperInvariant()}  ({PlayerDesk.LienSittingHours}h)");
			sit.CustomMinimumSize = new Vector2(360, 40);
			sit.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			sit.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.OpenLienSitting(out string m); if (!ok) Say(m, false); return ok; });
			content.AddChild(sit);
		} else Body(block);
	}

	private void Warn(string text) {
		var line = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		line.AddThemeColorOverride("font_color", Rust);
		content.AddChild(line);
	}

	// The sitting itself is a full-screen stage: PlayerDeskPanel.LienSitting.cs.

	private void RenderLienOption(PlayerDesk desk, PlayerDesk.LienSittingLive live, PlayerDesk.LienOption opt, Container into = null) {
		RenderOption(new CallOption {
			label = opt.label, subLabel = opt.subLabel, voice = opt.voice, isBluff = opt.isBluff,
			enabled = opt.enabled, disabledReason = opt.disabledReason, cash = opt.cash,
		}, () => {
			desk.ChooseLienOption(live, opt, out string m);
			Refresh();
		}, into);
	}

	// ── the morning after ───────────────────────────────────────────────────────────────────────────

	// The morning after is a full-screen scene: PlayerDeskPanel.LienMorning.cs.

	// ── the end ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>The paper's account on the game-over page, when the run had one.</summary>
	private void PaperEpilogue(PlayerDesk desk) {
		var lien = desk.Lien;
		if (lien == null) return;
		var p = desk.LienProfile;
		var rows = new List<LedgerTable.Row> {
			LedgerTable.Entry("Held by", $"{p.Name}, {p.Title}"),
			LedgerTable.Entry("Signed", lien.Signed.ToHeadlineString()),
			LedgerTable.Entry("Face sum at the end", LedgerTable.Money(lien.FaceSum)),
			LedgerTable.Entry("Principal paid", LedgerTable.Money(lien.PrincipalPaid)),
			LedgerTable.Entry("Interest paid", LedgerTable.Money(lien.InterestPaid)),
			LedgerTable.Entry("Sittings won", lien.Ledger.Count(e => e.Kind == PlayerDesk.LienLedgerKind.Extended && e.Detail != null && !e.Detail.StartsWith("seizure") && !e.Detail.StartsWith("no-show")).ToString()),
			LedgerTable.Entry("Seizures", lien.Ledger.Count(e => e.Kind == PlayerDesk.LienLedgerKind.Extended && e.Detail?.StartsWith("seizure") == true).ToString()),
		};
		var sale = lien.Ledger.LastOrDefault(e => e.Kind == PlayerDesk.LienLedgerKind.FloriaBuyout);
		if (sale != null) {
			rows.Add(LedgerTable.Entry("Buyer", sale.Detail));
			rows.Add(LedgerTable.Entry("Purchase price", LedgerTable.Money(sale.Amount)));
		}
		content.AddChild(new LedgerTable().Set("The paper", null, rows));
	}
}
