using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The Set (SigningExperienceAudit §3.2): hears real sets through the desk, then builds the scene at Ear / Street / Suit /
/// Fixer 1, 3 and 5 and checks the Rolodex gating (under 3 silent, 3 a hint, 5 the deep read built on the hint), that the
/// first-week teaching line speaks at any instinct and only while the card is on its first lines, that building the scene
/// is repeatable, and that the A&amp;R page stages it after a listen and lets the act go. Logs every scene's lines so the
/// prose can be read; windowed runs screenshot SimLogs/the-set-*.png.
///   --path . SimTools/TheSetChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints THE_SET_CHECKS_PASS / _FAIL.
/// </summary>
public partial class TheSetChecks : Node {
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
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://SimLogs/the-set-{name}.png"));
	}
	private static string Dump(SetScene s) => string.Join("\n", new[] { "  ROOM: " + string.Join(" / ", s.Room),
		"  SONGS: " + string.Join(", ", s.Songs.Select(x => x.Title + (x.Original ? " (own)" : ""))) + (s.SongsNote != null ? " · " + s.SongsNote : "") }
		.Concat(s.Reads.Select(r => $"  {r.voice}: {r.text}")).Append("  OFF: " + s.OffStage));

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--set-city="))?.Substring("--set-city=".Length) ?? "nashville";
			Check(desk.FoundLabel("The Set Checks", city, out string message), message);
			desk.DismissLienMorning();
			// Every set of the first day that can be heard before the hard stop, one act at a time.
			var bills = LocalSceneRoomService.Calendar(city, clock.CurrentDate)
				.Where(b => b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList();
			var heard = new List<string>();
			foreach (var bill in bills) foreach (var slot in bill.Appearances.Where(a => a.EndHour <= clock.HardStopHour).OrderBy(a => a.StartHour)) {
				if (heard.Count >= 6) break;
				if (slot.StartHour < clock.CurrentHour) continue;
				clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
				if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message)) heard.Add(slot.ArtistId);
			}
			Check(heard.Count >= 2, $"Heard {heard.Count} sets on the first day");
			Check(desk.FirstWeekCurrent == FirstWeekStep.Ask, "After a set the first-week card is on asking");

			var profile = desk.InstinctProfile;
			var voices = new[] { ExecutiveVoice.Ear, ExecutiveVoice.Street, ExecutiveVoice.Suit, ExecutiveVoice.Fixer };
			void SetAll(int score) { profile.TheEar = score; profile.TheStreet = score; profile.TheSuit = score; profile.TheFixer = score; }
			foreach (string id in heard) {
				var scenes = new Dictionary<int, SetScene>();
				foreach (int score in new[] { 1, 3, 5 }) {
					SetAll(score);
					var scene = desk.BuildSetScene(id);
					Check(scene != null && scene.Room.Count > 0 && !string.IsNullOrEmpty(scene.OffStage), $"{id}: a heard set builds a scene at {score}");
					GD.Print($"--- {scene.ArtistName} at {scene.RoomName}, every voice {score} ---\n" + Dump(scene));
					Check(Dump(desk.BuildSetScene(id)) == Dump(scene), $"{scene.ArtistName}: the scene is the same built twice");
					scenes[score] = scene;
				}
				var teach = scenes[1].Reads.Where(r => r.voice == ExecutiveVoice.Ear).ToList();
				Check(teach.Count == 1 && (teach[0].text.StartsWith("I can't tell you", StringComparison.Ordinal) || teach[0].text.Contains("spoken for", StringComparison.Ordinal)),
					"Ear 1: silent except the blunt first-week line");
				Check(scenes[1].Reads.All(r => r.voice == ExecutiveVoice.Ear), "Every voice at 1 is silent (teaching aside)");
				foreach (var v in voices.Skip(1)) {
					var low = scenes[3].Reads.Where(r => r.voice == v).Select(r => r.text).ToList();
					var high = scenes[5].Reads.Where(r => r.voice == v).Select(r => r.text).ToList();
					Check(low.Count <= 1 && high.Count == low.Count, $"{v}: one read at 3 and 5, or none at both");
					if (low.Count == 1 && v != ExecutiveVoice.Fixer) Check(high[0].StartsWith(low[0], StringComparison.Ordinal), $"{v}: the deep read builds on the hint");
				}
				var ear3 = scenes[3].Reads.Where(r => r.voice == ExecutiveVoice.Ear).ToList();
				var ear5 = scenes[5].Reads.Where(r => r.voice == ExecutiveVoice.Ear).ToList();
				Check(ear3.Count == 2 && ear5.Count >= 2 && ear5[0].text.StartsWith(ear3[0].text, StringComparison.Ordinal),
					"Ear 3 gives a hint plus the teaching verdict; Ear 5 builds on the same hint");
				Check(!ear3[1].text.StartsWith("I can't tell you", StringComparison.Ordinal), "A strong Ear's teaching line is a verdict, not a shrug");
			}

			// The teaching line goes with the first-week card.
			var savedCard = desk.CaptureFirstWeekForChecks();
			desk.PutAwayFirstWeek();
			SetAll(1);
			Check(desk.BuildSetScene(heard[0]).Reads.Count == 0, "Card away, every voice at 1: nobody speaks");
			desk.RestoreFirstWeekForChecks(savedCard);
			SetAll(5);   // the screenshot shows every voice

			// The A&R page stages the set, then lets the act go.
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			var panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("A&R"); await Frame(0.5);
			typeof(PlayerDeskPanel).GetMethod("OpenTheSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(panel, new object[] { desk });
			panel.OpenAtTab("A&R"); await Frame(0.5);
			var staged = desk.BuildSetScene(desk.Slate.Last().Artist.artistId);
			bool Shows(string text) => Nodes(panel).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
			Button Visible(string text) => Nodes(panel).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.Text == text);
			Check(Shows(staged.OffStage) && Shows(staged.ArtistName), "After a listen, the page is the set: the act comes off the stand");
			Check((Visible("TALK TO THEM") ?? Visible("SIT DOWN ON TERMS")) != null && Visible("LET THEM GO") != null, "Two things to do: talk to them or let them go");
			Check(Visible("FIND A SET") == null, "The scene takes the page; the finder waits");
			await Frame(0.4);
			await Screenshot("scene");
			Visible("LET THEM GO").EmitSignal(Button.SignalName.Pressed);
			await Frame(0.5);
			Check(Visible("FIND A SET") != null && !Shows(staged.OffStage), "LET THEM GO puts the finder back");
			Check(desk.ScoutingDossiers.Any(d => d.ArtistId == staged.ArtistId), "The act stays in the notebook after you let them go");
			GD.Print($"THE_SET_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("THE_SET_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
