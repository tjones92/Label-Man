using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// The Talk (SigningExperienceAudit §3.3, SigningRealismBreakdown step 5): hears the first day's sets through the desk,
/// then builds the conversation with each act. Every scene opens with a real member speaking, building it twice gives
/// the same scene and moves nothing, an act signed elsewhere is offered no ask, and for an act heard tonight every verb
/// of the old conversation modal is a line (the ask or terms, their history, a rehearsal, a demo, a return meeting,
/// their number) along with their next date. The Street reads what they want only from 3, building on its hint at 5.
/// Saying lines runs the real verbs: their next date is answered in the transcript at no cost, the ask's answer comes
/// back into it, ending the talk drops it. Then the A&amp;R page stages The Talk and puts the card back.
///   --path . SimTools/TheTalkChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--talk-city=detroit]
/// Prints THE_TALK_CHECKS_PASS / _FAIL.
/// </summary>
public partial class TheTalkChecks : Node {
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
		GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://SimLogs/the-talk-{name}.png"));
	}
	private static string Dump(TalkScene s) => string.Join("\n", new[] { "  " + s.Setting }
		.Concat(s.Lines.Select(l => $"  {(l.voice != ExecutiveVoice.None ? l.voice.ToString() : l.speaker ?? "")}: {l.text}"))
		.Concat(s.Options.Select(o => $"  > {o.Line} [{o.Verb}{(o.Enabled ? "" : ", no: " + o.Why)}] {o.Cost}")));
	private static string Json(object o) => JsonSerializer.Serialize(o, SaveGameService.TestJsonOptions);

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--talk-city="))?.Substring("--talk-city=".Length) ?? "detroit";
			Check(desk.FoundLabel("The Talk Checks", city, out string message), message);
			desk.DismissLienMorning();
			var profile = desk.InstinctProfile;
			void SetAll(int score) { profile.TheEar = score; profile.TheStreet = score; profile.TheSuit = score; profile.TheFixer = score; }

			// The sets of the opening days, one act at a time; the talk with each is built right after its set. Most acts on a
			// first-day bill are signed (SigningRealismBreakdown §1.3), so it keeps going until it has met unsigned acts too.
			int built = 0, free = 0, signed = 0;
			SimulatedArtist fresh = null;
			var seen = new HashSet<string>();
			for (int d = 0; d < 10 && (free < 3 || built < 6); d++) {
			if (d > 0) {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			int day = LocalSceneRoomService.Day(clock.CurrentDate);
			var bills = LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList();
			foreach (var bill in bills) foreach (var slot in bill.Appearances.Where(a => a.EndHour <= clock.HardStopHour).OrderBy(a => a.StartHour)) {
				if (built >= 12 || slot.StartHour < clock.CurrentHour || !seen.Add(slot.ArtistId)) continue;
				// Past the first day, only unsigned acts are worth the hour.
				if (d > 0 && !string.IsNullOrEmpty(am.GetArtist(slot.ArtistId)?.labelId)) continue;
				clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
				if (!desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message)) continue;
				var act = am.GetArtist(slot.ArtistId);
				built++;
				var scenes = new Dictionary<int, TalkScene>();
				foreach (int score in new[] { 1, 3, 5 }) {
					SetAll(score);
					string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
					var s = desk.BuildTalkScene(act.artistId);
					Check(s != null && s.Lines.Count > 0 && s.Lines[0].speaker == s.Speaker && !string.IsNullOrEmpty(s.Lines[0].text) && !string.IsNullOrEmpty(s.Setting),
						$"{act.stageName}: the talk opens with {s?.Speaker} speaking (voices at {score})");
					GD.Print($"--- {s.ArtistName}, voices {score} ---\n" + Dump(s));
					Check(Dump(desk.BuildTalkScene(act.artistId)) == Dump(s), $"{act.stageName}: the same talk built twice");
					Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player, $"{act.stageName}: building the talk moves nothing");
					scenes[score] = s;
				}
				Check(scenes[1].Lines.All(l => l.voice == ExecutiveVoice.None), "every voice at 1 is silent");
				var st3 = scenes[3].Lines.Where(l => l.voice == ExecutiveVoice.Street).ToList();
				var st5 = scenes[5].Lines.Where(l => l.voice == ExecutiveVoice.Street).ToList();
				Check(st3.Count == st5.Count && st3.Count <= 1 && (st3.Count == 0 || st5[0].text.StartsWith(st3[0].text, StringComparison.Ordinal)),
					"the Street reads once from 3, and its deep read builds on the hint");
				var verbs = scenes[3].Options.Select(o => o.Verb).ToHashSet();
				if (!string.IsNullOrEmpty(act.labelId)) {
					signed++;
					Check(!verbs.Contains(TalkVerb.Ask) && !verbs.Contains(TalkVerb.Terms) && !verbs.Contains(TalkVerb.Rehearsal) &&
						scenes[3].Lines[0].text.Contains(ChartManager.Instance.GetLabelName(act.labelId) ?? "label"),
						$"{act.stageName} is signed elsewhere: they say who with, and there's no ask");
				} else {
					free++;
					// Every verb the old conversation modal held is a line here.
					Check((verbs.Contains(TalkVerb.Ask) || verbs.Contains(TalkVerb.Terms) || verbs.Contains(TalkVerb.Step)) &&
						verbs.Contains(TalkVerb.History) && verbs.Contains(TalkVerb.Rehearsal) && verbs.Contains(TalkVerb.Demo) && verbs.Contains(TalkVerb.Meeting) &&
						(verbs.Contains(TalkVerb.Number) || desk.ActNumber(act.artistId) != null) && verbs.Contains(TalkVerb.NextSet),
						$"{act.stageName}: the ask, their history, a rehearsal, a demo, a return meeting, their number and their next date are all lines");
					Check(scenes[3].Options.All(o => o.Enabled ? !string.IsNullOrEmpty(o.Cost) : !string.IsNullOrEmpty(o.Why)),
						"every line says what it costs, or why not");
					if (fresh == null && desk.CanAskDirectApproach(act)) fresh = act;
				}
			}
			}
			Check(built >= 3 && free >= 1, $"built the talk with {built} acts heard in the opening days ({free} free, {signed} signed elsewhere)");

			// Saying lines runs the real verbs, and the answers come back into the transcript.
			SetAll(3);
			if (fresh == null) GD.Print("note: no free, unasked act heard tonight; the ask leg isn't exercised");
			else {
				string id = fresh.artistId;
				string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
				int minutes = clock.MinutesRemainingWithOvertime;
				Check(desk.SayInTalk(id, TalkVerb.NextSet, out message, out var none) && none == null, "When are you on next? " + message);
				var after = desk.BuildTalkScene(id);
				Check(after.Lines.Count(l => l.isPlayer) == 1 && after.Lines.Last().speaker == after.Speaker && after.Lines.Last().text == message,
					"you said it, and they answered, in the transcript");
				Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player && clock.MinutesRemainingWithOvertime == minutes,
					"asking their next date costs nothing and moves nothing");
				int approaches = desk.DirectApproaches.Count;
				desk.SayInTalk(id, TalkVerb.Ask, out message, out var terms);
				var asked = desk.DirectApproaches.LastOrDefault();
				Check(asked?.ArtistId == id && desk.DirectApproaches.Count == approaches + 1 && desk.BuildTalkScene(id).Lines.Last().text == message,
					$"the ask is the desk's ask, and its answer is in the transcript ({asked?.Status}): {message}");
				Check(terms == null || terms.HasBaseline && desk.CanTalkTerms(terms), "when terms open, The Talk hands over the contract");
				var later = desk.BuildTalkScene(id);
				Check(!later.Options.Any(o => o.Verb == TalkVerb.Ask) || desk.CanAskDirectApproach(fresh),
					"after the answer, the line offered is the next real step");
				GD.Print("--- after the ask ---\n" + Dump(later));
				desk.EndTalk(id);
				Check(!desk.BuildTalkScene(id).Lines.Any(l => l.isPlayer), "ending the conversation drops tonight's transcript");
			}

			// The A&R page stages The Talk and puts the card back.
			var target = fresh ?? am.GetArtist(desk.ScoutingDossiers.Last().ArtistId);
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			var panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("A&R"); await Frame(0.5);
			var dossier = desk.ScoutingDossiers.First(d => d.ArtistId == target.artistId);
			typeof(PlayerDeskPanel).GetMethod("OpenTheTalk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(panel, new object[] { desk, dossier });
			panel.OpenAtTab("A&R"); await Frame(0.5);
			var staged = desk.BuildTalkScene(target.artistId);
			bool Shows(string text) => Nodes(panel).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
			Button Visible(string text) => Nodes(panel).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.Text == text);
			Check(Shows(staged.Lines[0].text.Trim('"')) && Visible("END THE CONVERSATION") != null && Visible("FIND A SET") == null, "The Talk takes the page: they open, and you can end it");
			Check(staged.Options.All(o => Visible("▸  " + o.Line) is { } b && b.Disabled == !o.Enabled), "every line is on the page, greyed exactly when it can't be said");
			await Frame(0.3);
			await Screenshot("talk");
			Visible("▸  When are you on next?")?.EmitSignal(Button.SignalName.Pressed);
			await Frame(0.4);
			var answered = desk.BuildTalkScene(target.artistId).Lines.Last();
			Check(Shows(answered.text), "pressing a line puts their answer on the page");
			Visible("END THE CONVERSATION").EmitSignal(Button.SignalName.Pressed);
			await Frame(0.5);
			Check(Visible("END THE CONVERSATION") == null && Visible("ACT NOTEBOOK") != null, "ending the conversation goes back to the notebook");
			GD.Print($"THE_TALK_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("THE_TALK_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
