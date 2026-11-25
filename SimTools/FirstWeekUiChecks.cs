using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static PlayerDesk;

/// <summary>
/// Onboarding T3: plays the first week through the real desk by pressing only the button the card makes glow (or the
/// card's SHOW ME when nothing on screen does), from a fresh Sawyer founding in Detroit, and checks the card walks a new
/// player from hearing a set to an act of their own on the roster. Logs each press and the card's line; windowed runs
/// screenshot each step to SimLogs/first-week-*.png.
///   --path . SimTools/FirstWeekUiChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints FIRST_WEEK_UI_CHECKS_PASS / _FAIL.
/// </summary>
public partial class FirstWeekUiChecks : Node {
	private PlayerDeskPanel panel;
	private int checks, shots;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var n in Nodes(child)) yield return n;
	}
	private async Task Frame(double settle = 0.05) {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree().CreateTimer(settle), Timer.SignalName.Timeout);
	}
	private async Task Screenshot(string name) {
		if (DisplayServer.GetName() == "headless") return;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://SimLogs/first-week-{++shots:00}-{name}.png"));
	}
	private bool Shows(string text) => Nodes(GetTree().Root).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
	private Button Glowing() => Nodes(GetTree().Root).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.HasMeta("first_week_glow"));
	private Button Visible(string text) => Nodes(GetTree().Root).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && !b.Disabled && b.Text == text);

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;   // it ends a day through the real desk: keep out of the player's saves
			await Frame();
			var desk = PlayerDesk.Instance;
			var sawyer = new PartyMemory { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable };
			Check(desk.FoundLabel("First Week Checks", "detroit", FoundingArchetype.TradeInsider, null, sawyer, out string message), "Found: " + message);
			desk.DismissLienMorning();
			Check(desk.FirstWeekCurrent == FirstWeekStep.Hear, "A fresh label starts the card on hearing a set");
			Check(!desk.FirstWeekDone(FirstWeekStep.Record), "Connie on the roster doesn't tick the first record");

			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("A&R"); await Frame(0.7);
			Check(Shows("This week") && !Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.StartsWith("NEXT UP")), "The card takes NEXT UP's place");
			await Screenshot("start");

			var seen = new List<FirstWeekStep>();
			int presses = 0, showMes = 0;
			var started = TimeManager.Instance.CurrentDate;
			for (int i = 0; i < 60 && desk.FirstWeekCurrent is { } step && step != FirstWeekStep.Record; i++) {
				if (!seen.Contains(step)) seen.Add(step);
				await Frame(0.6);   // the watch looks twice a second
				var glow = Glowing();
				var (voice, how) = desk.FirstWeekHow(step);
				GD.Print($"[{TimeManager.Instance.CurrentDate.ToHeadlineString()} {TimeManager.Instance.GetTimeString()}] {step} · {voice}: {how}");
				if (glow == null) {
					var show = Visible("SHOW ME");
					Check(show != null && ++showMes <= 8, $"{step}: nothing glows and SHOW ME is there to press");
					GD.Print("  press SHOW ME");
					show.EmitSignal(Button.SignalName.Pressed);
					await Frame(0.7);
					if (Glowing() == null) {
						GD.Print("  visible buttons: " + string.Join(" | ", Nodes(GetTree().Root).OfType<Button>().Where(b => b.IsVisibleInTree() && !b.Disabled).Select(b => b.Text).Distinct().Take(60)));
						Check(false, $"{step}: SHOW ME leads to a glowing button");
					}
					continue;
				}
				GD.Print($"  press {glow.Text}");
				presses++;
				glow.EmitSignal(Button.SignalName.Pressed);
				await Frame(0.7);
				// The day's end puts the Morning Paper up over the desk: read it and put it down.
				await Screenshot(step.ToString().ToLowerInvariant());
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper && paper.Visible) {
					GD.Print("  put the Morning Paper down (its corner)");
					typeof(UIManager).GetMethod("DismissMorningPaper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
						?.Invoke(UIManager.Instance, null);
					await Frame(0.6);
					Check(!paper.Visible, "The paper goes down before the next press");
				}
			}
			GD.Print($"Presses on a glow: {presses}; SHOW ME: {showMes}; days: {TimeManager.Instance.CurrentDate.year * 400 + TimeManager.Instance.CurrentDate.month * 31 + TimeManager.Instance.CurrentDate.day - (started.year * 400 + started.month * 31 + started.day)}");
			Check(desk.FirstWeekDone(FirstWeekStep.Sign), "Following the glow signs an act of the player's own");
			Check(desk.FirstWeekCurrent == FirstWeekStep.Record, "The card's last line is the first record");
			await Frame(0.6);
			Check(Glowing()?.Text == "TO THE ROSTER", "The signing card's TO THE ROSTER glows");
			await Press("TO THE ROSTER");
			await Frame(0.6);
			Check(Glowing() is { } studio && studio.Text.StartsWith("TAKE INTO STUDIO") && !desk.IsHeldByHolder(ArtistManager.Instance.GetArtist(studio.GetMeta("artist_id").AsString())), "On the roster, TAKE INTO STUDIO glows for the act just signed, not Connie");
			await Screenshot("record");

			// The save keeps the card; putting it away ends it for good, and NEXT UP comes back.
			var saved = desk.CaptureFirstWeekForChecks();
			Check(saved.TermsOpened && !saved.Off && !saved.Done, "The save carries the card");
			await Press("I KNOW THE WAY");
			Check(desk.FirstWeekCurrent == null && !Shows("This week"), "I KNOW THE WAY puts the card away");
			Check(Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.StartsWith("NEXT UP")), "NEXT UP is back");
			Check(Glowing() == null, "Nothing glows once the card is away");
			desk.RestoreFirstWeekForChecks(null);
			Check(desk.FirstWeekCurrent == FirstWeekStep.Record, "An older save gets a card that ticks itself off from the run");
			GD.Print($"FIRST_WEEK_UI_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("FIRST_WEEK_UI_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private async Task Press(string text) {
		var b = Visible(text) ?? throw new InvalidOperationException("No button: " + text);
		b.EmitSignal(Button.SignalName.Pressed);
		await Frame(0.6);
	}
}
