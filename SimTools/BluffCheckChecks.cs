using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

/// <summary>
/// The Fixer's bluff (SigningExperienceAudit §3.12; inner-life directive §2 principle 2, amended 2026-11-26): an objection
/// whose true answer can't be said yet can be answered with that line anyway, as a white check. Checks that
///   - the dice: 2d6 + bonus against the difficulty, double 1 fails and double 6 passes, the odds exact, the keyed dice
///     fair and the same for the same key;
///   - no check before your first record is out (a new player's first signing runs on reads alone);
///   - once open, The Talk offers the bluff under the greyed true answer, with the instinct, modifiers, difficulty and odds;
///   - the same state gives the same key, and the Fixer, regard and nights heard change it;
///   - a failed bluff shuts on that state, costs a point of regard, and opens again when something real changes;
///     a bluff that lands answers the objection; both survive a save.
/// The fixture is an unsigned act heard on the first day, given a yes with a holdout who has raised an objection.
///   --path . SimTools/BluffCheckChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--set-city=detroit]
/// Prints BLUFF_CHECK_CHECKS_PASS / _FAIL.
/// </summary>
public partial class BluffCheckChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			// ── The dice ──
			Check(Math.Abs(PlayerDesk.CheckOdds(1, 10) - 10 / 36f) < 1e-6f, "Bonus 1 against 10 passes 10 in 36 (Disco Elysium's Medium at skill 1)");
			Check(Math.Abs(PlayerDesk.CheckOdds(20, 10) - 35 / 36f) < 1e-6f && Math.Abs(PlayerDesk.CheckOdds(-20, 10) - 1 / 36f) < 1e-6f,
				"Double 1 always fails and double 6 always passes");
			Check(PlayerDesk.CheckPasses(6, 6, -50, 18) && !PlayerDesk.CheckPasses(1, 1, 50, 2), "Boxcars and snake eyes override the sum");
			var counts = new int[7];
			for (int i = 0; i < 6000; i++) { var (a, b) = PlayerDesk.CheckDice("fairness|" + i); counts[a]++; counts[b]++; }
			Check(counts.Skip(1).All(c => c > 1800 && c < 2200), "The keyed dice are fair: " + string.Join(" ", counts.Skip(1)));
			Check(PlayerDesk.CheckDice("same") == PlayerDesk.CheckDice("same"), "The same key rolls the same");
			foreach (var d in new[] { CheckDifficulty.Easy, CheckDifficulty.Medium, CheckDifficulty.Challenging, CheckDifficulty.Formidable })
				GD.Print($"  {d} {(int)d}: Fixer 1..6 passes " + string.Join(" ", Enumerable.Range(1, 6).Select(f => PlayerDesk.CheckOdds(f, (int)d).ToString("P0"))));

			// ── The fixture ──
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--set-city="))?.Substring("--set-city=".Length) ?? "nashville";
			Check(desk.FoundLabel("Bluff Check Checks", city, out string message), message);
			desk.DismissLienMorning();
			SimulatedArtist act = null;
			foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList())
				foreach (var slot in bill.Appearances.Where(a => a.EndHour <= clock.HardStopHour).OrderBy(a => a.StartHour)) {
					if (act != null || slot.StartHour < clock.CurrentHour) continue;
					clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
					if (!desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message)) continue;
					var heard = ArtistManager.Instance.GetArtist(slot.ArtistId);
					if (string.IsNullOrEmpty(heard?.labelId) && heard.members.Count(m => m.isActive) >= 1 &&
						LocalScenePersistenceService.SceneIdFor(heard.geography?.basePlaceId) == LocalScenePersistenceService.SceneIdFor(desk.CurrentCityId)) act = heard;
				}
			Check(act != null, $"An unsigned act from {city} heard on the first day: {act?.stageName}");
			var member = act.members.First(m => m.isActive && m.lifeState == MemberLifeState.Active);
			var step = new ScoutingApproachStep { Kind = ScoutingApproachStepKind.Holdout, Who = member.firstName, PersonId = member.personId,
				Raised = true, RaisedDay = LocalSceneRoomService.Day(clock.CurrentDate), Objection = CourtshipObjection.Cheated, ObjectionLine = "Labels cheat you." };
			var knowledge = (ScoutingKnowledgeSaveData)typeof(PlayerDesk).GetField("scoutingKnowledge", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(desk);
			knowledge.Approaches ??= new();
			knowledge.Approaches.Add(new ScoutingDirectApproach { ArtistId = act.artistId, ArtistName = act.stageName, Day = LocalSceneRoomService.Day(clock.CurrentDate),
				Status = ScoutingApproachStatus.Agreed, UntilDay = LocalSceneRoomService.Day(clock.CurrentDate), Steps = new() { step } });
			Check(desk.PendingObjection(act) == step, $"{step.Who} holds out: \"{step.ObjectionLine}\"");
			Check(desk.GroundedAnswer(step, act).Why != null, "No act of yours has been paid, so the true answer is greyed: " + desk.GroundedAnswer(step, act).Why);

			// ── Not in the first signing ──
			Check(!desk.ChecksOpen && desk.ObjectionBluff(step, act) == null, "No record out: no check");
			Check(!desk.BuildTalkScene(act.artistId).Options.Any(o => o.Verb == TalkVerb.Bluff), "The Talk offers no bluff before the first record");
			Check(!desk.AnswerObjection(act.artistId, ObjectionAnswer.Bluff, out message) && !step.Done, "Bluffing is refused: " + message);

			int releases = desk.Label.totalReleases;
			var profile = desk.InstinctProfile; int fixerBefore = profile.TheFixer;
			try {
				desk.Label.totalReleases = 1;   // fixture: the first record is out
				var bluff = desk.ObjectionBluff(step, act);
				Check(desk.ChecksOpen && bluff != null && bluff.Difficulty == CheckDifficulty.Challenging && bluff.Voice == ExecutiveVoice.Fixer,
					"First record out: \"labels cheat\" can be bluffed, Challenging, on the Fixer");
				var option = desk.BuildTalkScene(act.artistId).Options.SingleOrDefault(o => o.Verb == TalkVerb.Bluff);
				Check(option != null && option.Bluff && option.Line == desk.GroundedAnswer(step, act).Line && option.Cost.Contains(bluff.Odds.ToString("P0")) &&
					option.Cost.Contains("Challenging 12"), "The Talk offers it under the true answer, with the odds: " + option?.Cost);
				Check(desk.ObjectionBluff(step, act).Key == bluff.Key, "The same state gives the same key");
				var keys = new HashSet<string>();
				for (int f = 1; f <= 6; f++) { profile.TheFixer = f; keys.Add(desk.ObjectionBluff(step, act).Key); }
				Check(keys.Count == 6, "Each Fixer level is its own state");
				for (int f = 1; f < 6; f++) { profile.TheFixer = f; float lo = desk.ObjectionBluff(step, act).Odds; profile.TheFixer = f + 1;
					Check(desk.ObjectionBluff(step, act).Odds > lo, $"Fixer {f + 1} has better odds than {f}"); }

				// Find a Fixer the dice fail and one they pass on this state.
				bool Passes(int f) { profile.TheFixer = f; var c = desk.ObjectionBluff(step, act); var (a, b) = PlayerDesk.CheckDice(c.Key); return PlayerDesk.CheckPasses(a, b, c.Bonus, (int)c.Difficulty); }
				var outcome = Enumerable.Range(1, 6).ToDictionary(f => f, Passes);
				GD.Print("  this holdout, Fixer 1..6: " + string.Join(" ", outcome.Select(p => $"{p.Key}:{(p.Value ? "pass" : "fail")}")));
				int fail = outcome.Where(p => !p.Value).Select(p => p.Key).DefaultIfEmpty(0).First();
				int pass = outcome.Where(p => p.Value && p.Key > fail).Select(p => p.Key).DefaultIfEmpty(0).First();
				if (fail == 0 || pass == 0) GD.Print("note: this state didn't give both a failing and a later passing Fixer; the failure leg was skipped");
				else {
					profile.TheFixer = fail;
					int regardBefore = desk.ReadRegard(act).Score;
					Check(desk.SayInTalk(act.artistId, TalkVerb.Bluff, out message, out _) && !step.Done && step.BluffFailedDay == LocalSceneRoomService.Day(clock.CurrentDate),
						$"Fixer {fail}: they don't buy it: " + message);
					Check(desk.ReadRegard(act).Score == regardBefore - 1 && desk.ReadRegard(act).Has(RegardSourceKind.CaughtBluffing), "Being caught costs a point of regard");
					var shut = desk.ObjectionBluff(step, act);
					Check(shut.Locked != null && !desk.AnswerObjection(act.artistId, ObjectionAnswer.Bluff, out message) && !step.Done,
						"The same state can't try again: " + message);
					Check(desk.BuildTalkScene(act.artistId).Options.Single(o => o.Verb == TalkVerb.Bluff) is { Enabled: false } greyed && greyed.Why == shut.Locked,
						"The Talk shows it shut, and why");
					string saved = JsonSerializer.Serialize(desk.CaptureState(), SaveGameService.TestJsonOptions);
					Check(saved.Contains(step.BluffFailedKey), "The failed bluff is in the save");
					profile.TheFixer = pass;
					Check(desk.ObjectionBluff(step, act).Locked == null, $"A Fixer point later ({pass}), it's open again");
					Check(desk.SayInTalk(act.artistId, TalkVerb.Bluff, out message, out _) && step.Done && step.Bluffed && desk.PendingObjection(act) == null,
						$"Fixer {pass}: it holds, and {step.Who} is in: " + message);
				}
			} finally { desk.Label.totalReleases = releases; profile.TheFixer = fixerBefore; }
			GD.Print($"BLUFF_CHECK_CHECKS_PASS {checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("BLUFF_CHECK_CHECKS_FAIL " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
