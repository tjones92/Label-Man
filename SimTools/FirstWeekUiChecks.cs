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
	private Control GlowingField() => Nodes(GetTree().Root).OfType<Control>().FirstOrDefault(c => c is not Button && c.IsVisibleInTree() && c.HasMeta("first_week_glow"));
	private bool SlipShowing() => GetTree().Root.FindChild("FirstWeekSlip", true, false) is Control slip && slip.IsVisibleInTree();
	private Button Visible(string text) => Nodes(GetTree().Root).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && !b.Disabled && b.Text == text);

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;   // it ends a day through the real desk: keep out of the player's saves
			await Frame();
			var desk = PlayerDesk.Instance;
			// --holder=NAME picks the answers that make that man the only fit (PlayerDesk.Lien Fits); Sawyer by default.
			string holderArg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--holder="))?["--holder=".Length..] ?? "sawyer";
			var holder = Enum.Parse<LienHolder>(holderArg, true);
			var memory = holder switch {
				LienHolder.Kubiak => new PartyMemory { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.TheLabel, End = PartyEnd.Car },
				LienHolder.Winters => new PartyMemory { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall },
				LienHolder.Floria => new PartyMemory { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall },
				LienHolder.Germain => new PartyMemory { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car },
				_ => new PartyMemory { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable },
			};
			Check(desk.FoundLabel("First Week Checks", "detroit", FoundingArchetype.TradeInsider, null, memory, out string message), "Found: " + message);
			Check(desk.Lien?.Holder == holder, $"The answers make {holder} the holder");
			Check((desk.Connie != null) == (holder == LienHolder.Sawyer), "Only a Sawyer run starts with Connie on the roster");
			desk.DismissLienMorning();
			Check(desk.FirstWeekCurrent == FirstWeekStep.Hear, "A fresh label starts the card on hearing a set");
			Check(!desk.FirstWeekDone(FirstWeekStep.Record), "Connie on the roster doesn't tick the first record");

			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			var calendar = Nodes(this).OfType<Button>().Single(b => b.Name == "CalendarBtn");
			var clock = TimeManager.Instance;
			var openedOn = clock.CurrentDate;
			calendar.EmitSignal(BaseButton.SignalName.Pressed);
			Check(clock.CurrentDate == openedOn.AddDays(1) && !PaperModal.IsOpen,
				"Pressing the room calendar immediately advances one day without a calendar dialog");
			await Frame();
			await PaperDown();
			await Frame(0.6);
			Check(Glowing()?.Name == "OfficeDoor", "The room points a new player to the door");
			await Screenshot("office-room");
			Glowing().EmitSignal(BaseButton.SignalName.Pressed); await Frame();
			Check(Shows("HIT THE TOWN") && Shows("PLAYING TODAY"), "The door opens the city map and today’s bill");
			await Screenshot("city-map");
			await Press("ALL UPCOMING SETS / VENUES");
			await Frame(0.7);
			Check(!Shows("This week") && !Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.StartsWith("NEXT UP")), "Street pages use the slim header without the desk checklist");
			await Screenshot("start");

			var seen = new List<FirstWeekStep>();
			int presses = 0, showMes = 0, counters = 0;
			bool pickedTime = false, openedBook = false, wentThere = false, sparedTheMorning = false;
			bool lowball = OS.GetCmdlineUserArgs().Contains("--lowball");
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
				// A visit later today: the hours before it are his. The glow is on a set that's over in time, or the wait,
				// never on going there hours early (playtest 2026-11-27).
				if (step == FirstWeekStep.Answer && desk.FirstWeekVisitLater && glow != null) {
					string g = glow.Text.Trim();
					Check(!g.StartsWith("GO THERE") && g != "THE BOOK", $"A visit at {PlayerDesk.ClockText(desk.FirstWeekVisit.Minute ?? 0)} with {TimeManager.Instance.GetTimeString()} on the clock: no GO THERE yet (glow: {g})");
					if (g is "GO & WAIT" or "WAIT 1h" or "WAIT 3h") {
						sparedTheMorning = true;
						if (g == "GO & WAIT") Check(glow.GetMeta("set_ends").AsInt32() <= desk.SceneDay * 1440 + (desk.FirstWeekLeaveAt ?? 0), "The glowing set is over before it's time to leave");
					}
				}
				var (voice, how) = desk.FirstWeekHow(step);
				// Each line is taught where its work happens, never on the card.
				string firstWords = how.Substring(0, Math.Min(40, how.Length));
				var card = (Control)typeof(PlayerDeskPanel).GetField("firstWeekCard", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel);
				Check(!Nodes(card).OfType<RichTextLabel>().Any(r => r.IsVisibleInTree() && r.Text.Contains(firstWords, StringComparison.Ordinal)),
					$"{step}: the card carries no teaching line");
				if (step == FirstWeekStep.Ask && Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.Contains("Would you hear an offer from us?"))) Check(Shows(firstWords), "The Street teaches asking in The Talk");
				if (step == FirstWeekStep.Sign && glow?.Text.StartsWith("TABLE OFFER") == true)
					Check(Shows(desk.FirstWeekHow(FirstWeekStep.Terms).How[..30]) && Shows(firstWords), "The Suit teaches the deal at The Table");
				GD.Print($"[{TimeManager.Instance.CurrentDate.ToHeadlineString()} {TimeManager.Instance.GetTimeString()}] {step} · {voice}: {how}");
				// A counter at the table: the Suit says it's a price, and SWEETEN IT glows.
				if (step == FirstWeekStep.Sign && Visible("WALK AWAY") != null && Visible("SWEETEN IT") != null) {
					counters++;
					Check(glow?.Text == "SWEETEN IT", $"On a counter, SWEETEN IT glows (glow: {glow?.Text})");
					Check(Shows("That's not a no, it's a price") || Shows("That isn't about the money") || Shows("They want more on the") || Shows("It's not the money, it's us") || Shows("you can't give half of it") || Shows("Give it to them and try again"),
						"The Suit teaches the counter on the page");
					await Frame(0.6);   // the glow scrolls it into view on the watch's next tick
					Check(OnScreen(glow), "SWEETEN IT is scrolled into view");
					await Screenshot("counter");
				}
				// Back on the sheet after a counter: the blank they're stuck on glows. Type about halfway to their number.
				if (glow == null && GlowingField() is SpinBox blank) {
					Check(Shows("They're stuck on the") || Shows("you wrote"), "Back on the sheet, the Suit names the term they're stuck on");
					double from = blank.GetMeta("first_week_from").AsDouble(), to = blank.GetMeta("first_week_to").AsDouble();
					double half = Math.Round((from + to) / 2 / blank.Step) * blank.Step;
					if (half == from) half = from + Math.Sign(to - from) * blank.Step;
					GD.Print($"  type {half} in the glowing blank ({from} → their {to})");
					await Frame(0.6);   // scrolled into view on the watch's next tick
					Check(OnScreen(blank), "The glowing blank is scrolled into view");
					await Screenshot("sweeten");
					blank.Value = half; presses++;
					await Frame(0.7);
					Check(Glowing()?.Text.StartsWith("TABLE AGAIN") == true, $"Once the blank moves, TABLE AGAIN glows (glow: {Glowing()?.Text})");
					continue;
				}
				if (glow is CheckBox give && step == FirstWeekStep.Sign) {
					Check(Shows("They're stuck on the") || Shows("Give it to them"), "Back on the sheet, the Suit names the give they want");
					GD.Print($"  tick the glowing give: {give.Text}");
					give.ButtonPressed = !give.ButtonPressed; presses++;
					await Frame(0.7);
					continue;
				}
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
				// --lowball: the player writes a third of the advance on the first offer, to draw a counter on the money.
				if (lowball && glow.Text.StartsWith("TABLE OFFER") && Nodes(panel).OfType<SpinBox>().FirstOrDefault(sp => sp.IsVisibleInTree() && sp.MaxValue >= 100000) is { } advanceBlank) {
					lowball = false;
					GD.Print($"  (lowball) write ${Math.Round(advanceBlank.Value / 3)} for the advance instead of ${advanceBlank.Value}");
					advanceBlank.Value = Math.Round(advanceBlank.Value / 3);
					await Frame(0.3);
				}
				// T6: whatever glows has its voice's slip beside it, when that voice has something to say about it.
				// Scrolled out of sight (the terms sheet's TABLE OFFER), it's pinned at the page's edge, pointing off it.
				if (desk.FirstWeekNudge(glow.Text.Trim(), false) is { } nudge)
					Check(SlipShowing() && Shows(nudge.Text[..Math.Min(30, nudge.Text.Length)]), $"The slip is up at {glow.Text.Trim()}{(OnScreen(glow) ? "" : " (below the fold)")}: {nudge.Text}");
				// The appointment book (onboarding T5): their times glow together in The Talk; on the day, the book, then GO THERE.
				string pressed = glow.Text.Trim();
				if (pressed.EndsWith("suits me.")) { pickedTime = true; await Screenshot("book-times"); }
				if (pressed == "THE BOOK") openedBook = true;
				var visitBefore = pressed.StartsWith("GO THERE") ? desk.FirstWeekVisit : null;
				if (pressed.StartsWith("GO THERE")) { wentThere = true; await Screenshot("book-open"); }
				GD.Print($"  press {glow.Text}");
				presses++;
				glow.EmitSignal(Button.SignalName.Pressed);
				await Frame(0.7);
				if (visitBefore != null) {
					Check(visitBefore.Done && visitBefore.DoneDay == LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate), $"GO THERE kept the visit ({visitBefore.Kind} with {visitBefore.Who}, {TimeManager.Instance.GetTimeString()})");
					var act = ArtistManager.Instance.GetArtist(chosen);
					bool atTerms = Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.StartsWith("TABLE OFFER"));
					Check(atTerms == desk.AgreedToSign(act),
						$"GO THERE {(atTerms ? "opened terms" : "left terms pending")} after the {visitBefore.Kind} visit");
				}
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
			GD.Print($"Presses on a glow: {presses}; SHOW ME: {showMes}; counters: {counters}; days: {TimeManager.Instance.CurrentDate.year * 400 + TimeManager.Instance.CurrentDate.month * 31 + TimeManager.Instance.CurrentDate.day - (started.year * 400 + started.month * 31 + started.day)}");
			if (OS.GetCmdlineUserArgs().Contains("--manager"))
				Check(pickedTime && openedBook && wentThere && sparedTheMorning, $"With a manager, the glow walks the book: a time picked in The Talk ({pickedTime}), the hours before the visit left free ({sparedTheMorning}), the book opened in the last hour ({openedBook}), GO THERE ({wentThere})");
			Check(desk.FirstWeekDone(FirstWeekStep.Sign), "Following the glow signs an act of the player's own");
			Check(desk.Label.roster.Any(a => a.artistId == chosen), "The act signed is the one the bot chose by its Ear's verdict");
			Check(desk.FirstWeekCurrent == FirstWeekStep.Record, "The card moves on to booking the first record");
			await Frame(0.6);
			Check(Glowing()?.Text == "TO THE ROSTER", "The signing card's TO THE ROSTER glows");
			await Press("TO THE ROSTER");
			await Frame(0.6);
			Check(Shows(desk.FirstWeekHow(FirstWeekStep.Record).How[..30]), "The Ear teaches the first record on the roster");
			if (desk.FirstWeekStudioTomorrow)
				Check(Glowing()?.Text == "END THE DAY" && Shows(desk.FirstWeekAside(FirstWeekStep.Record).Value.Aside[..30]), "Signed too late for a date: the Fixer says to end the day, and END THE DAY glows");
			else
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
			panel.OpenAtTab("ROSTER"); await Frame(0.7);

			await TheStudio(desk);
			await TheFirstSingle(desk);

			// The trace follows the run from the first desk all the way to shipment.
			await Frame(0.3);
			var trace = FirstSessionTrace.Current;
			Check(trace != null && trace.Finished, "The first-session trace ran and finished at the first release");
			Check(trace.Milestones.Contains("✓ " + PlayerDesk.FirstWeekTitle(FirstWeekStep.Sign)) && trace.Milestones.Contains("End: First single released")
				&& trace.Milestones.Contains("Card put away"), "The trace marks the signing, the card put away and the release");
			Check(trace.Presses > 0 && trace.GlowPresses > 0 && trace.GlowPresses <= trace.Presses, $"The trace counts presses ({trace.Presses}, {trace.GlowPresses} on the glow)");
			Check(System.IO.File.Exists(trace.SummaryPath), "The trace's summary is on disk: " + trace.SummaryPath);
			Check(trace.PressOutcomes.Where(p => p.Text is "GO & WAIT" or "LISTEN" or "END THE DAY" || p.Text.StartsWith("TABLE OFFER")).All(p => p.Outcome == "ok"),
				"Presses that moved the clock or the desk read as ok: " + string.Join("; ", trace.PressOutcomes.Select(p => p.Text + " → " + p.Outcome)));
			GD.Print(trace.Summary());
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
			await Frame(0.6);   // the watch's next tick
			var glow = Glowing();
			var rows = Nodes(panel).OfType<Button>().Where(b => b.IsVisibleInTree() && !b.Disabled && Row(b) && b.HasMeta("scouting_artist")).ToList();
			// Choosing who to hear: every listed set glows (the player picks), and FIND A SET doesn't, since it's already open.
			if (desk.FirstWeekCurrent == FirstWeekStep.Hear && rows.Count > 0)
				Check(rows.All(b => b.HasMeta("first_week_glow")) && Visible("FIND A SET")?.HasMeta("first_week_glow") != true,
					$"Hearing: all {rows.Count} listed sets glow, and FIND A SET doesn't (glow: {glow?.Text})");
			if (desk.FirstWeekCurrent == FirstWeekStep.Hear && heard == 0) {
				Check(Nodes(panel).OfType<Node>().Any(n => n.Name == "FirstWeekTeaching" && n is Control c && c.IsVisibleInTree()), "The Ear teaches hearing on the set finder, not the card");
				Check(Shows("Know the room before you trust what you hear in it"), "The Ear talks about tonight's rooms");
				// T6: the Street's slip pops over beside the sets, with an arrow to each one.
				Check(SlipShowing() && Shows("We need to sign a fuckin' act"), "The Street's slip is up beside the sets on day one");
				var arrows = (System.Collections.IList)typeof(PlayerDeskPanel).GetField("firstWeekArrowTargets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel);
				Check(arrows.Count == Math.Min(4, rows.Count(OnScreen)), $"An arrow to each set on screen ({arrows.Count} arrows, {rows.Count(OnScreen)} sets on screen)");
				await Screenshot("finder");
				var close = Nodes(GetTree().Root).OfType<Button>().First(b => b.Name == "FirstWeekSlipClose");
				close.EmitSignal(Button.SignalName.Pressed); await Frame(0.7);
				Check(!SlipShowing(), "The × puts the slip away while the same sets glow");
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
				if (heard == 0) Check(SlipShowing() && Shows("Shut up and listen"), "The slip comes back when the glow moves: the Ear at LISTEN");
				here.EmitSignal(Button.SignalName.Pressed); await Frame(0.7);
			}
			heard++;
			// The Set: each act on the bill comes off the stand; the Ear's teaching verdict says whether to talk to them.
			while (Visible("LISTEN CLOSELY") is { } closeListen) {
				closeListen.EmitSignal(Button.SignalName.Pressed); await Frame(0.6);
				if (Visible("LET THEM GO") is not { } go) break;
				var ear = Nodes(panel).OfType<Label>().Where(l => l.IsVisibleInTree() && l.HasMeta("set_voice") && l.GetMeta("set_voice").AsInt32() == (int)ExecutiveVoice.Ear).Select(l => l.Text).ToList();
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
					// --manager: a shark manages them (and they're hungry, with nobody holding out), so the yes comes with a sit-down to book.
					if (OS.GetCmdlineUserArgs().Contains("--manager") && ArtistManager.Instance.GetArtist(id) is { } act && string.IsNullOrEmpty(act.labelId)) {
						act.manager = ManagerArchetype.Shark; act.managerName ??= "Sol Kramer";
						foreach (var m in act.members.Where(m => m.isActive)) m.ambition = Math.Max(m.ambition, .95f);
						GD.Print("  (--manager) a shark manages them now");
					}
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

	private void Dump(string at) {
		GD.Print($"  --- {at}: visible text ---");
		foreach (var c in Nodes(GetTree().Root).OfType<Control>().Where(c => c.IsVisibleInTree())) {
			string t = c switch { Button b => "[" + b.Text + (b.Disabled ? " (off)" : "") + (b.HasMeta("first_week_glow") ? " *GLOW*" : "") + "]", Label l => l.Text, RichTextLabel r => r.Text, _ => null };
			if (!string.IsNullOrWhiteSpace(t)) GD.Print("    " + t.Replace("\n", " / "));
		}
	}

	/// <summary>Follow the glow from the roster to a booked studio date.</summary>
	private async Task TheStudio(PlayerDesk desk) {
		int presses = 0;
		bool taught = false;
		// --push-back: the player ticks a song the act will turn down before following the glow, so the booking meets
		// THE ACT PUSHES BACK (the Ear teaches it, the glow sets the song aside, and never walks back to it).
		bool pushBack = OS.GetCmdlineUserArgs().Contains("--push-back");
		string refusedTitle = null;
		bool pushedBack = false, insisted = false;
		string keptTitle = null;   // a song ticked beside the refused one, which they were happy with
		for (int i = 0; i < 14 && desk.Session == null; i++) {
			await Frame(0.6);
			var glow = Glowing();
			if (glow == null) Dump("nothing glows");
			Check(glow != null, "Something glows on the way to the first record");
			// The booking sheet, the first time it's up with time to book: the Ear and the Suit teach it.
			if (!taught && !desk.FirstWeekStudioTomorrow && Nodes(GetTree().Root).OfType<CheckBox>().Where(b => b.IsVisibleInTree() && b.HasMeta("first_week_song")).ToList() is { Count: > 0 } songs && songs.All(s => !s.ButtonPressed)) {
				var lines = desk.FirstWeekLinesAt(FirstWeekPlace.Studio);
				Check(lines.Count == 2 && Shows(lines[0].Text[..30]) && Shows(lines[1].Text[..30]), "The Ear and the Suit teach the booking on the sheet");
				await Screenshot("studio");
				taught = true;
				if (pushBack) {
					var act = desk.Label.roster.First(a => !desk.IsHeldByHolder(a));
					var tier = desk.StudioOffer(desk.StudioRoomIds()[0], act.artistId).Tier;
					var options = desk.MaterialOptionsFor(act).ToList();
					var bad = options.FirstOrDefault(o => desk.MaterialRefusals(act, new[] { o }, tier).Count > 0);
					GD.Print($"  {act.stageName}'s sheet: {string.Join(" | ", options.Select(o => o.Title + (desk.MaterialRefusals(act, new[] { o }, tier).Count > 0 ? " (would refuse)" : "")))}");
					if (bad == null) {
						// No real song is turned down: a new act's fit never stretches far enough (seeds 1001-1005; across the whole
						// catalogue, stretch stays under a third of what the gate needs). So the act's answer is forced: the player
						// ticks the first two songs and the real dialog opens on the first, as the BOOK THE ROOM press would. The dialog, the
						// Ear's line, the glow and SET THIS ASIDE are the real ones.
						bad = options[0];
						refusedTitle = bad.Title;
						keptTitle = options.Count > 1 ? options[1].Title : null;
						foreach (var o in options.Take(2)) songs.First(s => s.Text.StartsWith(o.Describe(), StringComparison.Ordinal)).ButtonPressed = true;
						var sheetChecks = options.Select(o => ((CheckBox)songs.First(s => s.Text.StartsWith(o.Describe(), StringComparison.Ordinal)), o)).ToList();
						var answer = new List<string> { $"{act.stageName} on “{bad.Title}”: “This doesn't feel like us. We'd rather work on our own material.”" };
						GD.Print($"  (forced) {act.stageName} turns down “{bad.Title}”; the real dialog opens on it");
						typeof(PlayerDeskPanel).GetMethod("RefusalDialog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
							.Invoke(Nodes(GetTree().Root).OfType<PlayerDeskPanel>().First(p => p.Name == "StudioWindow"), new object[] { act, options.Take(2).ToList(), tier, MinSessionHours, answer, sheetChecks, StudioBrief.CaptureLiveAct, null, null, false, null });
						await Frame(0.7);
						continue;
					}
					refusedTitle = bad.Title;
					var badBox = songs.First(s => s.Text.StartsWith(bad.Describe(), StringComparison.Ordinal));
					GD.Print($"  tick “{bad.Title}” myself (the act will turn it down)");
					badBox.ButtonPressed = true;
					await Frame(0.7);
					continue;
				}
			}
			// THE ACT PUSHES BACK: the Ear says what a no at booking means, and the glow answers it.
			if (Visible("SET THIS ASIDE") != null) {
				pushedBack = true;
				var lines = desk.FirstWeekPushBack(true, true);
				await Screenshot("push-back");
				Check(Shows(lines[0].Text[..30]) || Shows(desk.FirstWeekPushBack(false, true)[0].Text[..30]) || Shows(desk.FirstWeekPushBack(false, false)[0].Text[..30]),
					"The Ear teaches the push-back in the dialog");
				Check(glow.Text == "SET THIS ASIDE" || glow.Text.StartsWith("INSIST"), $"On the push-back, SET THIS ASIDE (or INSIST, with nothing else on the sheet) glows (glow: {glow.Text})");
			}
			if (glow.Text.StartsWith("INSIST")) insisted = true;
			if (pushedBack && glow is CheckBox again && again.Text.Contains(refusedTitle, StringComparison.Ordinal))
				Check(false, $"After the push-back, the glow never walks back to “{refusedTitle}”");
			if (desk.FirstWeekStudioTomorrow && glow.Text.StartsWith("‹ BACK"))
				Check(Shows(desk.FirstWeekLinesAt(FirstWeekPlace.Studio)[0].Text[..30]), "Too late in the day: the Fixer says so on the sheet and the way back glows");
			GD.Print($"  press {glow.Text}");
			Check(OnScreen(glow), $"The glow ({glow.Text}) is scrolled into view");
			presses++;
			bool wasBook = glow.Text.StartsWith("BOOK THE ROOM"), wasAside = glow.Text == "SET THIS ASIDE";
			if (glow is CheckBox box) box.ButtonPressed = true;
			else glow.EmitSignal(Button.SignalName.Pressed);
			await Frame(0.7);
			await PaperDown();
			if (wasBook && desk.Session == null && Visible("SET THIS ASIDE") == null) Dump("after BOOK THE ROOM");
			if (wasAside) {
				var sheet = Nodes(GetTree().Root).OfType<CheckBox>().Where(b => b.IsVisibleInTree() && b.HasMeta("first_week_song")).ToList();
				Check(sheet.Where(b => b.Text.Contains(refusedTitle ?? "\u0000", StringComparison.Ordinal)).All(b => !b.ButtonPressed),
					"SET THIS ASIDE unticks the song they turned down");
				if (keptTitle != null) Check(sheet.Any(b => b.ButtonPressed && b.Text.Contains(keptTitle, StringComparison.Ordinal)),
					$"...and leaves “{keptTitle}”, which they were happy with, ticked");
			}
		}
		Check(taught, "The booking sheet was taught on the way");
		if (pushBack) Check(pushedBack, "--push-back: the booking met THE ACT PUSHES BACK");
		await Screenshot("booked");
		Check(desk.Session != null, $"Following the glow books a date ({presses} presses)");
		GD.Print($"  booked: {string.Join(", ", desk.Session.Cuts.Select(c => c.Choice.Title))}");
		Check(insisted ? desk.Session.Cuts.Count >= 1 : pushBack ? desk.Session.Cuts.Count >= 1 && desk.Session.Cuts.All(c => c.Choice.Title != refusedTitle) : desk.Session.Cuts.Count == 2,
			insisted ? "Turned down everything: insisting books the date" : pushBack ? "The date is cut without the song they turned down" : "Two songs on the date: a single");
		Check(desk.FirstWeekDone(FirstWeekStep.Record) && desk.FirstWeekCurrent == FirstWeekStep.Cut, "Booking a room moves the card to cutting the sides");
		Check(Glowing()?.Text.StartsWith("CUT A TAKE") == true, "The first pass glows on the take sheet");
		Dump("takes");
	}

	private async Task TheFirstSingle(PlayerDesk desk) {
		var saved = desk.CaptureFirstWeekForChecks();
		saved.Done = true; // The old tutorial stopped at booking and wrote Done.
		desk.RestoreFirstWeekForChecks(saved);
		Check(desk.FirstWeekCurrent == FirstWeekStep.Cut, "A save from the old tutorial picks up at the booked studio date");
		var seen = new HashSet<FirstWeekStep>();
		for (int i = 0; i < 55 && desk.FirstWeekCurrent is { } step; i++) {
			await PaperDown();
			if (desk.FirstWeekCurrent == null) break;
			var holderReply = desk.HolderMoveOptions().FirstOrDefault(o => !o.accept && o.enabled);
			if (desk.HolderMovePending && holderReply != null && Visible("▸  " + holderReply.label) != null)
				await Press("▸  " + holderReply.label);
			seen.Add(step);
			var glow = Glowing();
			if (glow == null) { await Frame(0.6); glow = Glowing(); }
			if (glow == null) Dump($"no glow at {step}");
			Check(glow != null && !glow.Disabled, $"A live control is guided during {step}");
			GD.Print($"  {step}: {glow.Text}");
			if (step == FirstWeekStep.Release) {
				Check(desk.FirstWeekSingle?.Dated == true && !desk.ReleasedRecords.Any(), "Dating a single leaves the tutorial active until shipment");
			}
			if (glow is CheckBox song) song.ButtonPressed = !song.ButtonPressed;
			glow.EmitSignal(Button.SignalName.Pressed);
			await Frame(0.8);
		}
		await PaperDown();
		Check(new[] { FirstWeekStep.Cut, FirstWeekStep.Assemble, FirstWeekStep.Press, FirstWeekStep.Date, FirstWeekStep.Release }.All(seen.Contains),
			"The glow walks through the studio, assembly, pressing, dating and the wait");
		Check(desk.FirstWeekDone(FirstWeekStep.Release) && desk.FirstWeekCurrent == null, "The first single is out and the tutorial is complete");
		Check(Glowing() == null && !Shows("This week"), "The tutorial card and glow are gone after shipment");
		Check(Nodes(panel).OfType<Button>().Any(b => b.IsVisibleInTree() && b.Text.StartsWith("NEXT UP")), "NEXT UP takes over after release");
		saved = desk.CaptureFirstWeekForChecks();
		desk.RestoreFirstWeekForChecks(saved);
		Check(saved.SingleOut && desk.FirstWeekCurrent == null, "Completion stays put after restoring the tutorial state");
		await Screenshot("first-single-out");
	}

	/// <summary>Inside the scroll that holds it, and so where a player would see it.</summary>
	private static bool OnScreen(Control c) {
		for (var p = c.GetParent(); p != null; p = p.GetParent())
			if (p is ScrollContainer s) return s.GetGlobalRect().Grow(1).Encloses(c.GetGlobalRect());
		return true;
	}

	private async Task Press(string text) {
		var b = Visible(text) ?? throw new InvalidOperationException("No button: " + text);
		b.EmitSignal(Button.SignalName.Pressed);
		await Frame(0.6);
	}

	/// <summary>The day's end puts the Morning Paper up over the desk: read it and put it down (its corner).</summary>
	private async Task PaperDown() {
		if (GetTree().Root.FindChild("MorningPaper", true, false) is not Control paper || !paper.Visible) return;
		GD.Print("  put the Morning Paper down (its corner)");
		typeof(UIManager).GetMethod("DismissMorningPaper", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
			?.Invoke(UIManager.Instance, null);
		await Frame(0.6);
		Check(!paper.Visible, "The paper goes down before the next press");
	}
}
