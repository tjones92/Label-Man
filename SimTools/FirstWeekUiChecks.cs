using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using static PlayerDesk;

/// <summary>
/// Onboarding T3: plays the first week through the real desk from a fresh Sawyer founding in Detroit, and checks the card
/// walks a new player from hearing a set to an act of their own on the roster. The card never picks the act
/// (SigningExperienceAudit §2.7, §3.5): on the hearing line nothing on a set glows until you've walked to it, so the bot
/// hears sets in the order the finder lists them and asks the first act its Ear says to talk to. From there it presses
/// only the button the card makes glow (or SHOW ME when nothing on screen does). Logs each press and the card's line; windowed runs
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
	private bool Shows(string text) => Nodes(GetTree().Root).OfType<Control>().Any(c => c.IsVisibleInTree() &&
		(c is Label l && l.Text.Contains(text, StringComparison.Ordinal) || c is RichTextLabel r && r.Text.Contains(text, StringComparison.Ordinal)));
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
			string chosen = null;
			for (int i = 0; i < 60 && desk.FirstWeekCurrent is { } step && step != FirstWeekStep.Record; i++) {
				if (!seen.Contains(step)) seen.Add(step);
				if (chosen == null && step is FirstWeekStep.Hear or FirstWeekStep.Ask) { chosen = await ChooseAnAct(desk); continue; }
				// A no: the card points back at the sets, never at the day's end, and the bot chooses again.
				if (step == FirstWeekStep.Answer && desk.FirstWeekNeedsAnotherAct && Glowing()?.Text.Contains("Would you hear an offer") != true) {
					await Frame(0.6);
					Check(Glowing()?.Text is "END THE CONVERSATION" or "FIND A SET" or "NEXT SETS", $"After a no, the card points back at the sets, not at the day's end or the same act (glow: {Glowing()?.Text})");
					GD.Print("  they said no: hear somebody else");
					chosen = await ChooseAnAct(desk); continue;
				}
				await Frame(0.6);   // the watch looks twice a second
				var glow = Glowing();
				var (voice, how) = desk.FirstWeekHow(step);
				// Each line is taught where its work happens, never on the card.
				string firstWords = how.Substring(0, Math.Min(40, how.Length));
				Check(!Nodes(panel).OfType<RichTextLabel>().Any(r => r.IsVisibleInTree() && r.Text.Contains(firstWords, StringComparison.Ordinal) &&
					Nodes(panel).OfType<PanelContainer>().Any(card => card.Visible && card.IsAncestorOf(r) && Nodes(card).OfType<Label>().Any(l => l.Text == "This week"))),
					$"{step}: the card carries no teaching line");
				if (step == FirstWeekStep.Ask && Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.Contains("Would you hear an offer from us?"))) Check(Shows(firstWords), "The Street teaches asking in The Talk");
				if (step == FirstWeekStep.Sign && glow?.Text.StartsWith("TABLE OFFER") == true)
					Check(Shows(desk.FirstWeekHow(FirstWeekStep.Terms).How[..30]) && Shows(firstWords), "The Suit teaches the deal at The Table");
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
			Check(desk.Label.roster.Any(a => a.artistId == chosen), "The act signed is the one the bot chose by its Ear's verdict");
			Check(desk.FirstWeekCurrent == FirstWeekStep.Record, "The card's last line is the first record");
			await Frame(0.6);
			Check(Glowing()?.Text == "TO THE ROSTER", "The signing card's TO THE ROSTER glows");
			await Press("TO THE ROSTER");
			await Frame(0.6);
			Check(Shows(desk.FirstWeekHow(FirstWeekStep.Record).How[..30]), "The Ear teaches the first record on the roster");
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

	/// <summary>Hearing, the way a player would: no set glows until you've walked to it. Hear sets in the finder's order
	/// and talk to the first act the Ear says to talk to now; let the rest go. Returns the act chosen.</summary>
	private async Task<string> ChooseAnAct(PlayerDesk desk) {
		bool Row(Button b) => b.Text is "GO & WAIT" or "LISTEN";
		for (int heard = 0, days = 0; heard < 20 && days < 10;) {
			panel.OpenAtTab("A&R"); await Frame(0.6);
			if (Visible("END THE CONVERSATION") is { } end) { GD.Print("  press END THE CONVERSATION"); end.EmitSignal(Button.SignalName.Pressed); await Frame(0.6); }
			if (Visible("FIND A SET") is { } find) { find.EmitSignal(Button.SignalName.Pressed); await Frame(0.6); }
			if (Visible("NEXT SETS") is { } next) { next.EmitSignal(Button.SignalName.Pressed); await Frame(0.6); }
			var glow = Glowing();
			Check(glow == null || !Row(glow) || glow.HasMeta("first_week_here"), "Hearing: no set glows until you've walked to it" + (glow != null ? $" (glow: {glow.Text})" : ""));
			if (desk.FirstWeekCurrent == FirstWeekStep.Hear && heard == 0) {
				Check(Nodes(panel).OfType<Node>().Any(n => n.Name == "FirstWeekTeaching" && n is Control c && c.IsVisibleInTree()), "The Ear teaches hearing on the set finder, not the card");
				Check(Shows("Know the room before you trust what you hear in it"), "The Ear talks about tonight's rooms");
				await Screenshot("finder");
			}
			var row = Nodes(panel).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && !b.Disabled && Row(b));
			if (row == null) {
				GD.Print("  nothing left to hear today: END THE DAY");
				await Press("END THE DAY"); days++;
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper && paper.Visible)
					typeof(UIManager).GetMethod("DismissMorningPaper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(UIManager.Instance, null);
				await Frame(0.6);
				continue;
			}
			GD.Print($"  hear the next set: {row.Text}");
			bool walk = row.Text == "GO & WAIT";
			row.EmitSignal(Button.SignalName.Pressed); await Frame(0.7);
			if (walk) {
				await Frame(0.6);
				var here = Glowing();
				Check(here != null && here.Text == "LISTEN" && here.HasMeta("first_week_here"), "At the set you walked to, its LISTEN glows");
				here.EmitSignal(Button.SignalName.Pressed); await Frame(0.7);
			}
			heard++;
			// The Set: each act on the bill comes off the stand; the Ear's teaching verdict says whether to talk to them.
			while (Visible("LET THEM GO") is { } go) {
				var ear = Nodes(panel).OfType<Label>().Where(l => l.IsVisibleInTree() && l.Text.StartsWith(VoiceLabel(ExecutiveVoice.Ear))).Select(l => l.Text).ToList();
				foreach (string line in ear) GD.Print("    " + line);
				var queue = (List<string>)typeof(PlayerDeskPanel).GetField("theSetQueue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel);
				string onStage = queue.FirstOrDefault();
				bool askedAlready = desk.DirectApproaches.Any(a => a.ArtistId == onStage);
				if (!askedAlready && ear.Any(l => l.Contains("go and talk to them now"))) {
					var talk = Visible("TALK TO THEM") ?? Visible("SIT DOWN ON TERMS");
					Check(talk != null, "A good verdict comes with something to do about it");
					talk.EmitSignal(Button.SignalName.Pressed); await Frame(0.7);
					string id = (string)typeof(PlayerDeskPanel).GetField("theTalkArtistId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel)
						?? desk.Slate.FirstOrDefault(p => p.HasBaseline)?.Artist.artistId;
					GD.Print($"  chose {ArtistManager.Instance.GetArtist(id)?.stageName} after {heard} set(s)");
					Check(id != null, "Talking to the act the Ear liked");
					return id;
				}
				go.EmitSignal(Button.SignalName.Pressed); await Frame(0.6);
			}
		}
		Check(false, "Within twenty sets and ten days, the Ear said to talk to somebody new");
		return null;
	}

	private static string VoiceLabel(ExecutiveVoice voice) =>
		(string)typeof(PlayerDeskPanel).GetMethod("VoiceName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!.Invoke(null, new object[] { voice }) + " —";

	private async Task Press(string text) {
		var b = Visible(text) ?? throw new InvalidOperationException("No button: " + text);
		b.EmitSignal(Button.SignalName.Pressed);
		await Frame(0.6);
	}
}
