using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The Table (SigningExperienceAudit §3.4): hears a real set, makes the act free, and sits down on terms through the desk.
/// Checks that somebody sits across the sheet (the manager when there is one, else a member), that their ask is a line
/// carrying every term, that each manager kind says it in its own manner, that the Suit and Fixer follow the Rolodex
/// gating (under 3 silent, the deep read built on the hint), that the Fixer calls a rival belief a hope only when no
/// offer stands behind it, that their objections come back as spoken lines that never name the speaker, that building
/// the scene moves nothing, and that the A&amp;R page draws it in place of the old "Their ask" line.
///   --path . SimTools/TheTableChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints THE_TABLE_CHECKS_PASS / _FAIL. Windowed runs screenshot SimLogs/the-table-*.png.
/// </summary>
public partial class TheTableChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var n in Nodes(child)) yield return n;
	}
	private async Task Frame(double settle = 0.1) {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(settle), Timer.SignalName.Timeout);
	}
	private async Task Screenshot(string name) {
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://SimLogs/the-table-{name}.png"));
	}
	private static string Hash(object value) {
		using var sha = System.Security.Cryptography.SHA256.Create();
		using (var sink = new System.Security.Cryptography.CryptoStream(System.IO.Stream.Null, sha, System.Security.Cryptography.CryptoStreamMode.Write))
			JsonSerializer.Serialize(sink, value, SaveGameService.TestJsonOptions);
		return Convert.ToHexString(sha.Hash);
	}
	private static string Dump(TableScene s) => "  " + s.Setting + "\n" + string.Join("\n",
		s.Lines.Select(l => l.voice != ExecutiveVoice.None ? $"  {l.voice}: {l.text}" : string.IsNullOrEmpty(l.speaker) ? $"  ({l.text})" : $"  {l.speaker}: {l.text}"));
	private static bool HasTerms(string line, ContractTermSheet a) =>
		line.Contains($"${a.Advance:N0}") && line.Contains(PlayerDesk.Points(a.RoyaltyRate)) && line.Contains(Plural.Of(a.TermYears, "year"))
		&& (a.SinglesObligation > 0 ? line.Contains(Plural.Of(a.SinglesObligation, "single")) : line.Contains("no sides owed"))
		&& line.Contains("publishing") == !a.LabelOwnsPublishing && line.Contains("last word") == a.ArtistCreativeControl;

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			Check(desk.FoundLabel("The Table Checks", "nashville", out string message), message);
			desk.DismissLienMorning();
			var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate)
				.First(b => b.Status == SceneBillStatus.Scheduled && b.Appearances.Count > 0 && b.Appearances[0].EndHour <= clock.HardStopHour && b.Appearances[0].StartHour >= clock.CurrentHour);
			var act = am.GetArtist(bill.Appearances[0].ArtistId);
			clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), bill.Appearances[0].StartHour);
			Check(desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, bill.Appearances[0]), out message), "heard a real set: " + message);
			// A free act that wants a deal, with a shark doing its business: the table is a negotiation.
			act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
			act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.Shark; act.managerName = "Sol Kramer";
			clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), 10, 0); desk.Label.cashReserves = 100000;
			var p = desk.Slate.Single(x => x.Artist.artistId == act.artistId);
			Check(desk.ApproachToSign(p, out message) && p.Talk != null, "a shark-managed act sits down to negotiate: " + message);
			Check(message.StartsWith("You sit down with", StringComparison.Ordinal) && !message.Contains("Hard-nosed"), "the toast says who you sit down with, not the ask");
			var talk = p.Talk;

			var profile = desk.InstinctProfile;
			void SetAll(int score) { profile.TheEar = score; profile.TheStreet = score; profile.TheSuit = score; profile.TheFixer = score; }

			// ── Who sits across, and the ask said in full ──
			var scenes = new Dictionary<int, TableScene>();
			bool still = true;
			foreach (int score in new[] { 1, 3, 4, 5 }) {
				SetAll(score);
				string knowledge = Hash(desk.CaptureState()), world = Hash(WorldStateService.Capture());
				var scene = desk.BuildTableScene(talk);
				still &= Hash(desk.CaptureState()) == knowledge && Hash(WorldStateService.Capture()) == world;
				GD.Print($"--- {scene.ArtistName}, every voice {score} ---\n" + Dump(scene));
				Check(Dump(desk.BuildTableScene(talk)) == Dump(scene), $"voices {score}: the same table built twice");
				scenes[score] = scene;
			}
			Check(still, "building the table moves neither the save nor the world");
			var ask = talk.ask;
			var opening = scenes[1].Lines[0];
			Check(scenes[1].Speaker == ask.ManagerName && opening.speaker == ask.ManagerName && opening.beat == RolodexSceneBeat.Opening,
				$"the manager ({ask.ManagerName}) sits across and opens");
			Check(HasTerms(opening.text, ask) && opening.text.Contains("Up front means up front"), "the shark's ask carries every term, in his manner: " + opening.text);
			Check(scenes[1].Setting.Contains(act.stageName), "the setting says who they're there for");
			Check(scenes[1].Lines.All(l => l.voice == ExecutiveVoice.None), "every voice at 1 is silent");
			string suit3 = scenes[3].Lines.Single(l => l.voice == ExecutiveVoice.Suit).text;
			string suit4 = scenes[4].Lines.Single(l => l.voice == ExecutiveVoice.Suit).text;
			string suit5 = scenes[5].Lines.Single(l => l.voice == ExecutiveVoice.Suit).text;
			Check(suit3.StartsWith($"${ask.Advance:N0} up front is", StringComparison.Ordinal), "Suit 3 reads the number against the room: " + suit3);
			Check(suit4.StartsWith(suit3, StringComparison.Ordinal) && suit4.Contains("cash on hand") && suit4.Contains($"of {desk.Label.maxRosterSize}"), "Suit 4 adds your cash and roster");
			Check(suit5.StartsWith(suit4, StringComparison.Ordinal) && suit5.Length > suit4.Length, "Suit 5 names what they'll fight for: " + suit5);

			// ── The Fixer: a rival belief is a hope unless an offer stands behind it ──
			Check(scenes[5].Lines.All(l => l.voice != ExecutiveVoice.Fixer) || desk.PendingRivalOffer(act.artistId) != null || talk.outlookStated.Contains(PlayerDesk.RivalBeliefLine),
				"the Fixer is silent with no rival word and no belief");
			var statedBefore = talk.outlookStated.ToList();
			if (!talk.outlookStated.Contains(PlayerDesk.RivalBeliefLine)) talk.outlookStated.Add(PlayerDesk.RivalBeliefLine);
			SetAll(3);
			var fixer3 = desk.BuildTableScene(talk).Lines.Where(l => l.voice == ExecutiveVoice.Fixer).ToList();
			SetAll(4);
			var fixer4 = desk.BuildTableScene(talk).Lines.Where(l => l.voice == ExecutiveVoice.Fixer).ToList();
			Check(desk.PendingRivalOffer(act.artistId) != null || (fixer3.Count == 1 && fixer3[0].text.StartsWith("Nobody's put paper") && fixer4.Single().text.Contains("a hope, not an offer")),
				"a rival they only believe in: the Fixer calls it a hope");
			talk.outlookStated.Clear(); talk.outlookStated.AddRange(statedBefore);

			// ── Each manager says it in their own manner; with no manager, a member says it ──
			var kinds = new (ManagerArchetype Kind, string Mark)[] {
				(ManagerArchetype.LocalHustler, "shake on it tonight"), (ManagerArchetype.Shark, "Up front means up front"),
				(ManagerArchetype.Svengali, "I shape this act"), (ManagerArchetype.Visionary, "isn't a point for discussion"), (ManagerArchetype.None, "We'd want") };
			foreach (var (kind, mark) in kinds) foreach (bool pub in new[] { true, false }) foreach (bool ctrl in new[] { false, true }) {
				if (kind == ManagerArchetype.Visionary && pub) continue;   // a visionary never leaves the publishing with the label
				var sheet = new ContractTermSheet(85, 0.0325f, 3, kind == ManagerArchetype.Shark ? 0 : 4, pub, ctrl, .5f, kind, kind == ManagerArchetype.None ? null : "Ray Dunn", "");
				string line = PlayerDesk.AskLine(sheet, kind != ManagerArchetype.None);
				Check(HasTerms(line, sheet) && line.Contains(mark), $"{kind} (publishing {(pub ? "label" : "act")}, control {(ctrl ? "act" : "label")}): every term, in its manner: {line}");
			}
			SetAll(3);
			var easy = new PlayerDesk.Prospect { Artist = act, Venue = p.Venue, SceneRoomId = p.SceneRoomId, HasBaseline = true,
				Baseline = new ContractTermSheet(ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false, .1f, ManagerArchetype.None, null, "") };
			var easyScene = desk.BuildTableScene(easy);
			GD.Print("--- no manager ---\n" + Dump(easyScene));
			Check(easyScene.Speaker != ask.ManagerName && act.members.Any(m => m.FullName.Trim() == easyScene.Speaker) && easyScene.Lines[0].text.StartsWith("We'd want"),
				"with no manager, a member sits across and says the ask: " + easyScene.Speaker);

			// ── Their objection comes back as a line across the table ──
			SetAll(5);
			Check(desk.TableOffer(talk, MathF.Round(ask.Advance * .3f), ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false, out message), "a low offer goes on the table: " + message);
			if (talk.stage == ContractTalkStage.Objection) {
				var said = desk.BuildTableScene(talk);
				GD.Print("--- after a low offer ---\n" + Dump(said));
				var back = said.Lines.Last();
				Check(back.beat == RolodexSceneBeat.Pushback && back.speaker == ask.ManagerName, "their objection is the last line, said by the manager");
				Check(said.Lines.Where(l => l.beat == RolodexSceneBeat.Pushback).All(l => !l.text.Contains(ask.ManagerName)), "the manager never names himself");
				if (desk.PlayNegotiationCounter(talk, ContractCounter.HoldFirm, out message) && talk.stage == ContractTalkStage.Objection) {
					var held = desk.BuildTableScene(talk).Lines;
					Check(held.Any(l => l.beat == RolodexSceneBeat.SituationRead && l.text.StartsWith(ask.ManagerName + " doesn't move much", StringComparison.Ordinal)),
						"holding firm: the narration names who's across the table");
				}
			} else GD.Print("(the act walked at the first low offer; objection legs not reached)");

			// ── The page draws the table ──
			if (p.Talk == null && !desk.ApproachToSign(p, out message)) { p.CooldownUntil = null; Check(desk.ApproachToSign(p, out message), "back to the table: " + message); }
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			var panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			typeof(PlayerDeskPanel).GetField("negotiating", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(panel, p);
			panel.OpenAtTab("A&R"); await Frame(0.5);
			bool Shows(string text) => Nodes(panel).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
			var staged = p.Talk != null ? desk.BuildTableScene(p.Talk) : desk.BuildTableScene(p);
			Check(Shows("ACROSS THE TABLE: " + staged.Speaker.ToUpperInvariant()) && Shows(staged.Lines[0].text.Substring(0, 30)), "the page shows who's across the table and their ask");
			Check(!Shows("Their ask:") && !Shows("HE COUNTERS"), "the old system lines are gone");
			await Frame(0.4);
			await Screenshot("negotiation");
			GD.Print($"THE_TABLE_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("THE_TABLE_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
