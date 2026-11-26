using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Signed as a clock (SigningRealismBreakdown §3.4, step 6). Hears a signed act on the opening day, then: the Street reads
/// its paper in The Talk only from 3 (label, sides, what charted, from 4 when the deal's up, at 5 what the label will
/// likely do), building the talk moves nothing, and there's still no ask. "I'll keep an eye on you" files it and is
/// answered in the transcript; the card says so. As the deal comes up the Street reminds you once per deal. A renewal
/// keeps the watch. When the label lets them go, the act you watched rings the office: a number, a hold no rival can
/// sign past, an opening line that says so. The watch and the hold survive the save. The wait ends after a week and the
/// hold with it. A stranger the label lets go doesn't call.
///   --path . SimTools/SignedClockChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--talk-city=detroit]
/// Prints SIGNED_CLOCK_CHECKS_PASS / _FAIL.
/// </summary>
public partial class SignedClockChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static string Json(object o) => JsonSerializer.Serialize(o, SaveGameService.TestJsonOptions);

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance; var charts = ChartManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--talk-city="))?.Substring("--talk-city=".Length) ?? "detroit";
			Check(desk.FoundLabel("Signed Clock Checks", city, out string message), message);
			desk.DismissLienMorning();
			var profile = desk.InstinctProfile;
			void Street(int score) { profile.TheEar = 1; profile.TheSuit = 1; profile.TheFixer = 1; profile.TheStreet = score; }

			// A signed act heard on the opening day.
			SimulatedArtist act = null;
			int day = LocalSceneRoomService.Day(clock.CurrentDate);
			foreach (var bill in LocalSceneRoomService.Calendar(city, clock.CurrentDate).Where(b => b.Day == day && b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour)) {
				foreach (var slot in bill.Appearances.Where(a => a.EndHour <= clock.HardStopHour).OrderBy(a => a.StartHour)) {
					var candidate = am.GetArtist(slot.ArtistId);
					if (string.IsNullOrEmpty(candidate?.labelId) || charts.GetLabelById(candidate.labelId) == null || slot.StartHour < clock.CurrentHour) continue;
					clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
					if (desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out message)) { act = candidate; break; }
				}
				if (act != null) break;
			}
			Check(act != null, $"heard a signed act on the opening day: {act?.stageName} ({charts.GetLabelName(act?.labelId)})");
			string id = act.artistId;
			var label = charts.GetLabelById(act.labelId);

			// The Street reads their paper.
			foreach (int score in new[] { 1, 3, 4, 5 }) {
				Street(score);
				string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
				var s = desk.BuildTalkScene(id);
				var street = s.Lines.Where(l => l.voice == ExecutiveVoice.Street).Select(l => l.text).ToList();
				var read = desk.ReadSignedElsewhere(act);
				GD.Print($"--- Street {score} ---\n  " + string.Join("\n  ", s.Lines.Select(l => $"{(l.voice != ExecutiveVoice.None ? l.voice.ToString() : l.speaker)}: {l.text}")) +
					"\n  " + string.Join("\n  ", s.Options.Select(o => $"> {o.Line} [{o.Verb}{(o.Enabled ? "" : ", no: " + o.Why)}] {o.Cost}")));
				Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player, $"Street {score}: building the talk moves nothing");
				if (score == 1) Check(street.Count == 0, "the Street says nothing at 1");
				else Check(street.Count == 1 && street[0].StartsWith(read.Paper, StringComparison.Ordinal) && street[0].Contains(read.LabelName) &&
					(score < 4 || read.Clock == null || street[0].Contains(read.Clock)) && (score == 5) == street[0].EndsWith(read.Likely, StringComparison.Ordinal),
					$"Street {score} reads their paper{(score >= 4 ? ", the clock" : "")}{(score == 5 ? " and the label's likely call" : "")}: {street[0]}");
				Check(!s.Options.Any(o => o.Verb is TalkVerb.Ask or TalkVerb.Terms or TalkVerb.Handshake), "still no ask and no handshake for a signed act");
				Check(s.Options.Single(o => o.Verb == TalkVerb.Watch) is { Enabled: true } w && !string.IsNullOrEmpty(w.Cost), "\"I'll keep an eye on you\" is a line, with its cost");
			}

			// Keep an eye on them.
			Street(5);
			Check(desk.SayInTalk(id, TalkVerb.Watch, out message, out _) && desk.IsWatchingSigned(id), "watching: " + message);
			var said = desk.BuildTalkScene(id);
			Check(said.Lines.Last().speaker == said.Speaker && said.Lines.Any(l => l.isPlayer && l.text == "I'll keep an eye on you."), "you said it, and they answered in the transcript");
			Check(said.Options.Any(o => o.Verb == TalkVerb.Unwatch) && !said.Options.Any(o => o.Verb == TalkVerb.Watch), "the line becomes letting them go");
			Check(desk.SignedWatchSummary(id) is { } card && card.Contains(label.labelName), "the card says you're keeping an eye on them: " + desk.SignedWatchSummary(id));
			Check(desk.ScoutingDossiers.Any(d => d.ArtistId == id), "they're in the notebook");
			desk.EndTalk(id);

			// The Street reminds you as the deal comes up, once per deal.
			int week = charts.GetCurrentChartWeek();
			int savedExpires = act.contractExpiresWeek, savedYear = act.contractExpiresYear;
			act.contractExpiresWeek = week + PlayerDesk.SignedReminderWeeks + 5;
			int notes = desk.Log.Count(l => l.Contains(act.stageName));
			desk.AdvanceSignedWatchesForCheck();
			Check(desk.Log.Count(l => l.Contains(act.stageName)) == notes, "no reminder while the deal is months off");
			act.contractExpiresWeek = week + 4;
			desk.AdvanceSignedWatchesForCheck();
			Check(desk.Log[0].Contains(act.stageName) && desk.Log[0].Contains("Deal's up") && desk.Log[0].Contains("The Street:"), "the Street's reminder: " + desk.Log[0]);
			string first = desk.Log[0];
			desk.AdvanceSignedWatchesForCheck();
			Check(desk.Log[0] == first, "one reminder per deal");

			// A renewal keeps the watch.
			act.contractExpiresWeek = week + 156;
			desk.ContractEndedForCheck(label, act, "renewed");
			Check(desk.IsWatchingSigned(id) && desk.Log[0].Contains("picked up"), "a renewal keeps the watch: " + desk.Log[0]);

			// The label lets them go: the act you watched calls first.
			act.contractExpiresWeek = week;
			Check(am.DropArtist(act, clock.CurrentDate.year, label, ArtistDropReason.ContractExpired) && string.IsNullOrEmpty(act.labelId), "the label lets them go");
			desk.ContractEndedForCheck(label, act, "let-go");
			var call = desk.FirstCall(act);
			Check(call != null && call.LetGoBy == label.labelName && RosterManager.Instance.IsHeldForPlayer(id) && desk.ActNumber(id) != null,
				"they call first: a number, and a hold no rival signs past");
			Check(desk.Log[0].Contains("rang the office") && desk.Log[0].Contains("hear it from us first"), "the call: " + desk.Log[0]);
			var free = desk.BuildTalkScene(id);
			Check(free.Lines[0].text.Contains("hear it from us first") && free.Options.Any(o => o.Verb is TalkVerb.Ask or TalkVerb.Terms or TalkVerb.Step),
				"the talk opens with the call, and the ask is back: " + free.Lines[0].text);
			Check(desk.SignedWatchSummary(id)?.Contains("They called") == true, "the card says they called: " + desk.SignedWatchSummary(id));

			// The watch and the hold survive the save.
			var copy = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
			Check(copy.SignedWatches?.Count == 1, "the save carries the call");
			Check(desk.RestoreState(copy, out message), "player state restores: " + message);
			Check(desk.FirstCall(act) != null && RosterManager.Instance.IsHeldForPlayer(id), "the call and its hold survive the save");

			// A stranger the label lets go doesn't call.
			var stranger = am.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && charts.GetLabelById(a.labelId ?? "") != null &&
				!desk.ScoutingDossiers.Any(d => d.ArtistId == a.artistId)).OrderBy(a => a.artistId, StringComparer.Ordinal).First();
			var strangerLabel = charts.GetLabelById(stranger.labelId);
			Check(am.DropArtist(stranger, clock.CurrentDate.year, strangerLabel, ArtistDropReason.ContractExpired), "a stranger is let go");
			desk.ContractEndedForCheck(strangerLabel, stranger, "let-go");
			Check(desk.SignedWatchFor(stranger.artistId) == null && !RosterManager.Instance.IsHeldForPlayer(stranger.artistId), "an act that doesn't know you doesn't call");

			// The wait ends after a week, and the hold with it.
			for (int d = 0; d <= PlayerDesk.HandshakeDays + 1 && desk.SignedWatchFor(id) != null; d++) {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			Check(desk.SignedWatchFor(id) == null && !RosterManager.Instance.IsHeldForPlayer(id), "after a week the wait is over and the hold lifts");
			Check(desk.Log.Any(l => l.Contains(act.stageName) && l.Contains("talking to other labels")) || !string.IsNullOrEmpty(act.labelId),
				"they say they've moved on (or someone signed them the day it lifted)");

			GD.Print($"SIGNED_CLOCK_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("SIGNED_CLOCK_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
