using System;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2, Phase 1A gate: correct quotes, prerequisites, due dates and event order for the release
/// completion preflight, the CASH OPTIONS card and the solvency wording. It predicts the monthly reckoning with
/// NextSolvencyCheck / ClosureCheckIfNothingChanges, then advances the real clock and compares against what the
/// bank actually did, so the wording cannot drift from OnMonthChanged's order (overhead first, then the check).
///   --path . SimTools/CashPlanningChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints CASH_PLANNING_CHECKS_PASS / _FAIL.
/// </summary>
public partial class CashPlanningChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			Check(desk.FoundLabel("Cash Planning Checks", "new_york", FoundingArchetype.ExMusician, out var message), "Found: " + message);
			var label = desk.Label;
			float overhead = label.GetMonthlyOverhead();
			Check(Math.Abs(overhead - CityProfiles.MonthlyOverhead("new_york")) < .01f, $"NYC overhead read from the city profile ({overhead})");

			// Due dates: the next check is the 1st of next month, after that month's overhead.
			var next = desk.NextSolvencyCheck(label.cashReserves);
			Check(next.Date == Next(clock.CurrentDate), "Next check is the 1st of next month");
			Check(next.Days == (int)(new DateTime(next.Date.year, next.Date.month, 1) - new DateTime(clock.CurrentDate.year, clock.CurrentDate.month, clock.CurrentDate.day)).TotalDays, "Days to the check");
			Check(Math.Abs(next.CashAfter - (label.cashReserves - overhead)) < .01f && next.Outcome == "in the black", "Black check reads cash less overhead");
			Check(PlayerDesk.SolvencyRuleText.Contains("1st") && PlayerDesk.SolvencyRuleText.Contains("after that month's overhead"), "F7 wording names the real order");

			// Quotes and prerequisites: a fresh label owes a signing, a studio date and a first run.
			var plan = desk.PreviewReleaseCompletion();
			Check(plan.Remaining.Select(s => s.Kind).SequenceEqual(new[] { PlayerDesk.CompletionStageKind.Signing, PlayerDesk.CompletionStageKind.Studio, PlayerDesk.CompletionStageKind.Pressing }),
				"Fresh label: sign, record, press");
			Check(plan.Remaining[0].Estimate && plan.Remaining[0].Cost == 0f, "Signing is an estimate with no invented advance");
			Check(Math.Abs(plan.Remaining[1].Cost - desk.SessionCost(PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours)) < .01f && plan.Remaining[1].Estimate,
				"Without an act the studio stage is the budget-room estimate");
			Check(Math.Abs(plan.Remaining[2].Cost - PlayerDesk.PressingCost(PlayerDesk.PressMinimumOrder)) < .01f && Math.Abs(plan.Remaining[2].Cost - 128f) < .01f,
				"First run is the code's 500-copy price ($128)");
			Check(!plan.HasIncome && plan.SellableStock == 0 && plan.PressingsInFlight == 0, "Nothing is working for a fresh label");
			var signingOnly = desk.PreviewReleaseCompletion(100f, PlayerDesk.CompletionStageKind.Signing);
			Check(signingOnly.Remaining.All(s => s.Kind != PlayerDesk.CompletionStageKind.Signing) && Math.Abs(signingOnly.CashAfter - (label.cashReserves - 100f)) < .01f,
				"A spend that covers a stage drops it and is deducted once");

			// Warning thresholds: funded route = no warning; a spend that strands the studio names it.
			float route = plan.RemainingCost;
			Check(desk.CompletionWarning(0f) == null, "A funded opening raises no warning");
			string studioShort = desk.CompletionWarning(label.cashReserves - route + 1f, PlayerDesk.CompletionStageKind.Signing);
			Check(studioShort != null && studioShort.Contains("press a first run is unfunded"), "Leaving $1 short names the pressing stage: " + studioShort);
			string bothShort = desk.CompletionWarning(label.cashReserves - 10f, PlayerDesk.CompletionStageKind.Signing);
			Check(bothShort != null && bothShort.Contains("record an a and a b side is unfunded"), "Leaving $10 names the studio stage first: " + bothShort);
			Check(!bothShort.Contains("Already working for you"), "No qualification without stock, runs or receivables");

			// CASH OPTIONS can be empty, and says so (§4.2: the never-empty invariant is removed).
			Check(desk.CashOptions().Count == 0, "A fresh label with nothing pressed has no cash routes");

			// Stranded-away fix: out of town on negative cash, the drive home still goes, gas on the tab like
			// the motel; any other trip stays gated on cash.
			{
				float cashSaved = label.cashReserves, expensesSaved = label.monthlyExpenses;
				var near = desk.ReachableCities().OrderBy(c => desk.DriveQuote(desk.CurrentCityId, c.cityId).Hours).ThenBy(c => c.cityId, StringComparer.Ordinal).First();
				Check(desk.DriveTo(near.cityId, out var drove) && !desk.AtHome, "Drive out: " + drove);
				label.cashReserves = -391f;
				var onward = desk.ReachableCities().First(c => c.cityId != label.homeCityId && desk.DriveQuote(desk.CurrentCityId, c.cityId).Gas > 0f);
				Check(!desk.DriveTo(onward.cityId, out var refused) && refused.Contains("short of"), "Outbound trips stay cash-gated: " + refused);
				float gasHome = desk.DriveQuote(desk.CurrentCityId, label.homeCityId).Gas;
				Check(desk.DriveHome(out var home) && desk.AtHome, "The drive home goes on negative cash: " + home);
				Check(Math.Abs(label.cashReserves - (-391f - gasHome)) < .01f, $"Gas home books on the tab ({label.cashReserves:F2})");
				label.cashReserves = cashSaved; label.monthlyExpenses = expensesSaved;
			}

			// Event order, run on the real clock. Cash below one month's overhead: the first check goes red.
			label.cashReserves = overhead * .5f;
			var predicted = desk.NextSolvencyCheck(label.cashReserves);
			Check(predicted.CashAfter < 0 && predicted.Outcome == "red check 1 of 3" && !predicted.WouldClose, "Predicted first red check");
			var closing = desk.ClosureCheckIfNothingChanges();
			var expectedClose = predicted.Date; for (int i = 0; i < 2; i++) expectedClose = Next(expectedClose);
			Check(closing.HasValue && closing.Value == expectedClose, $"Closure predicted on the third red check ({closing?.ToShortString()} vs {expectedClose.ToShortString()})");
			float cashBefore = label.cashReserves;
			AdvanceTo(predicted.Date);
			Check(desk.MonthsInTheRed == 1 && !desk.IsGameOver, $"Actual: one red check on {predicted.Date.ToShortString()} (count {desk.MonthsInTheRed})");
			Check(Math.Abs(label.cashReserves - (cashBefore - overhead)) < .01f, $"Actual cash after the 1st is cash less overhead only ({label.cashReserves:F2} vs {cashBefore - overhead:F2})");
			var second = desk.NextSolvencyCheck(label.cashReserves);
			Check(second.Outcome == "red check 2 of 3" && desk.ClosureCheckIfNothingChanges() == expectedClose, "Second check and closure date stay consistent");

			// Climbing back above $0 before the check resets the count (the existing rule, unchanged).
			label.cashReserves = overhead + 50f;
			var black = desk.NextSolvencyCheck(label.cashReserves);
			// $50 after the reset still drains: three fresh red checks follow, so the forecast closes on the third of them.
			Check(black.Outcome == "in the black: the red count resets" && desk.ClosureCheckIfNothingChanges() == Next(Next(Next(black.Date))),
				$"Back above water resets the count, then the forecast restarts it ({desk.ClosureCheckIfNothingChanges()?.ToShortString()})");
			AdvanceTo(black.Date);
			Check(desk.MonthsInTheRed == 0 && Math.Abs(label.cashReserves - 50f) < .01f, "Actual: the count resets after overhead leaves cash at or above $0");

			// The ceiling: a check below the credit floor closes the label at once, and the forecast says so.
			label.cashReserves = desk.CreditFloor + overhead - 1f;
			var floor = desk.NextSolvencyCheck(label.cashReserves);
			Check(floor.WouldClose && floor.Outcome.Contains("overdraft ceiling") && desk.ClosureCheckIfNothingChanges() == floor.Date, "Ceiling closure predicted");
			AdvanceTo(floor.Date);
			Check(desk.IsGameOver && desk.GameOverReason.Contains("credit line"), "Actual: the ceiling closes the label on that check");

			GD.Print($"CASH_PLANNING_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PushError($"CASH_PLANNING_CHECKS_FAIL after {checks} checks: {e}");
			GetTree().Quit(3);
		}
	}

	private static GameDate Next(GameDate d) => d.month == 12 ? new GameDate(d.year + 1, 1, 1) : new GameDate(d.year, d.month + 1, 1);
	private static void AdvanceTo(GameDate date) {
		for (int guard = 0; guard < 40 && TimeManager.Instance.CurrentDate < date; guard++) TimeManager.Instance.EndDay();
		if (TimeManager.Instance.CurrentDate < date) throw new InvalidOperationException("Clock did not reach " + date.ToShortString());
	}
}
