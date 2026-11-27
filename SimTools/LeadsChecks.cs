using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Leads (SigningRealismBreakdown §3.6, step 7). Runs the opening week so the rooms have a week of nights, then reads the
/// leads for every free act in every scene town at each Street level. Every Street fact names real state (a residency,
/// a full room this week, the draw the room recorded). Below 3 the Street talks the noise up and never tells a crowd from
/// a band, so loud ordinary bands get hyped (the misread); from 4 it says when the noise is the room's; at 5 it names
/// good bands nobody's talking about. A booker speaks only once you've met them, only for acts that drew in their room.
/// The Fixer speaks from 3 only when an AI market really looked, and not after four weeks or once they're signed. Rival
/// looks survive the save. Building leads moves nothing. NEXT SETS shows them.
///   --path . SimTools/LeadsChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--talk-city=detroit]
/// Prints LEADS_CHECKS_PASS / _FAIL.
/// </summary>
public partial class LeadsChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static string Json(object o) => JsonSerializer.Serialize(o, SaveGameService.TestJsonOptions);
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

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--talk-city="))?.Substring("--talk-city=".Length) ?? "detroit";
			Check(desk.FoundLabel("Leads Checks", city, out string message), message);
			desk.DismissLienMorning();
			var profile = desk.InstinctProfile;
			void Voices(int street, int fixer) { profile.TheEar = 1; profile.TheSuit = 1; profile.TheStreet = street; profile.TheFixer = fixer; }
			for (int d = 0; d < 6; d++) {
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}
			int today = LocalSceneRoomService.Day(clock.CurrentDate);

			var towns = SceneRoomCatalog.All.Select(r => r.PlaceId).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToArray();
			var acts = am.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && string.IsNullOrEmpty(a.labelId) &&
				LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) is { } s && towns.Contains(s))
				.OrderBy(a => a.artistId, StringComparer.Ordinal).ToArray();
			Check(acts.Length > 50, $"{acts.Length} free acts across {towns.Length} scene towns");
			string Town(SimulatedArtist a) => LocalScenePersistenceService.SceneIdFor(a.geography.basePlaceId);
			if (OS.GetCmdlineUserArgs().Contains("--leads-dist")) {
				string Q(IEnumerable<float> xs) { var v = xs.OrderBy(x => x).ToArray(); return v.Length == 0 ? "none" : $"n={v.Length} p10={v[v.Length/10]:0.00} p50={v[v.Length/2]:0.00} p90={v[v.Length*9/10]:0.00} p97={v[v.Length*97/100]:0.00} max={v[^1]:0.00}"; }
				var playing = acts.Where(a => SceneRoomCatalog.ForPlace(Town(a)).Any(r => LocalSceneRoomService.ShowsAt(r.Id, a.artistId) > 0)).ToArray();
				GD.Print($"DIST playing acts {playing.Length}; band all {Q(acts.Select(a => a.CalculateBaseQuality()))}; band playing {Q(playing.Select(a => a.CalculateBaseQuality()))}");
				GD.Print($"DIST livePerformance playing {Q(playing.Select(a => a.livePerformance))}");
				var shares = playing.Select(a => SceneRoomCatalog.ForPlace(Town(a)).Where(r => r.IsPerformance).Select(r => (r, d: LocalSceneRoomService.DrawAt(r.Id, a.artistId)))
					.Where(x => x.d != null && SceneLiveEconomics.Capacity(x.r) > 0).Select(x => (float)x.d.Value.AverageArrivals / SceneLiveEconomics.Capacity(x.r)).DefaultIfEmpty(-1f).Max()).Where(x => x >= 0).ToArray();
				GD.Print($"DIST draw share (any nights) {Q(shares)}");
				var nights = playing.Select(a => SceneRoomCatalog.ForPlace(Town(a)).Select(r => LocalSceneRoomService.DrawAt(r.Id, a.artistId)?.Nights ?? 0).Max()).Select(x => (float)x);
				GD.Print($"DIST draw nights {Q(nights)}");
				var fills = playing.SelectMany(a => SceneRoomCatalog.ForPlace(Town(a)).Select(r => LocalSceneRoomService.LastCompletedBill(r.Id, a.artistId, today, clock.CurrentHour * 60)))
					.Where(b => b != null).DistinctBy(b => b.Id).Select(b => b.Attendance / (float)Math.Max(1, b.Capacity));
				GD.Print($"DIST last bill fill {Q(fills)}");
			}

			// Before he's been in a room, the Street has nothing to pass on: only a room billing its regulars, in print.
			Voices(5, 5);
			var before = acts.Select(a => (a, leads: desk.LeadsFor(a.artistId, Town(a), today))).ToList();
			Check(before.All(x => x.leads.All(l => l.Voice != ExecutiveVoice.Street)), "the Street says nothing before a set has been heard");
			Check(before.Where(x => SceneRoomCatalog.Get(LocalSceneRoomService.EngagementRoom(x.a.artistId, today)) is { } r && r.PlaceId == Town(x.a))
				.All(x => x.leads.Any(l => l.Voice == ExecutiveVoice.None && l.Text.StartsWith("The room bills", StringComparison.Ordinal))), "before that, a residency shows as the room's own billing");
			desk.BeenOutForCheck();

			// Pure.
			string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
			var at5 = acts.ToDictionary(a => a.artistId, a => desk.LeadsFor(a.artistId, Town(a), today));
			Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player, "building every act's leads moves nothing");
			Check(acts.All(a => Json(desk.LeadsFor(a.artistId, Town(a), today)) == Json(at5[a.artistId])), "the same leads built twice");

			// Every Street fact is real.
			int resident = 0, full = 0, drawn = 0, heated = 0;
			foreach (var a in acts) {
				string text = at5[a.artistId].FirstOrDefault(l => l.Voice == ExecutiveVoice.Street)?.Text ?? "";
				var room = SceneRoomCatalog.Get(LocalSceneRoomService.EngagementRoom(a.artistId, today));
				bool res = room != null && room.PlaceId == Town(a);
				Check(res == text.Contains("steady") && (!res || text.Contains(room.Name)), $"{a.stageName}: a residency is named exactly when they hold one");
				var draw = SceneRoomCatalog.ForPlace(Town(a)).Where(r => r.IsPerformance).Select(r => (r, d: LocalSceneRoomService.DrawAt(r.Id, a.artistId)))
					.Where(x => x.d is { Nights: >= 1 } && SceneLiveEconomics.Capacity(x.r) > 0)
					.Select(x => (x.r, x.d.Value.AverageArrivals, share: x.d.Value.AverageArrivals / (float)SceneLiveEconomics.Capacity(x.r)))
					.OrderByDescending(x => x.share).ThenBy(x => x.r.Id, StringComparer.Ordinal).FirstOrDefault();
				bool draws = draw.r != null && draw.share >= PlayerDesk.LeadDrawShare;
				Check(draws == text.Contains(" a night.") && (!draws || text.Contains($"{draw.r.Name}, about {draw.AverageArrivals} a night")),
					$"{a.stageName}: the draw is the room's own record");
				bool wasFull = text.Contains(" was packed ");
				if (wasFull) Check(SceneRoomCatalog.ForPlace(Town(a)).Any(r => LocalSceneRoomService.LastCompletedBill(r.Id, a.artistId, today, clock.CurrentHour * 60) is { } b &&
					today - b.Day <= 7 && b.Capacity > 0 && b.Attendance >= PlayerDesk.LeadBusyRoom * b.Capacity && text.Contains(r.Name + " was packed")), $"{a.stageName}: a packed room this week, really packed: {text}");
				resident += res ? 1 : 0; full += wasFull ? 1 : 0; drawn += draws ? 1 : 0;
				heated += res || wasFull || draws ? 1 : 0;
			}
			GD.Print($"street facts: {heated} of {acts.Length} free acts have noise ({resident} residents, {full} packed rooms, {drawn} draws)");
			Check(heated > 0, "some free acts have the town talking");

			// The fog: what each Street level says about the noise.
			var bandOf = acts.ToDictionary(a => a.artistId, a => a.CalculateBaseQuality());
			int hypedOrdinary = 0, flaggedOrdinary = 0, gems = 0, gemsHeardLow = 0;
			foreach (int street in new[] { 1, 2, 3, 4, 5 }) {
				Voices(street, 1);
				foreach (var a in acts) {
					var leads = desk.LeadsFor(a.artistId, Town(a), today);
					string text = leads.FirstOrDefault(l => l.Voice == ExecutiveVoice.Street)?.Text;
					string heard5 = at5[a.artistId].FirstOrDefault(l => l.Voice == ExecutiveVoice.Street)?.Text;
					bool noisy = heard5 != null && !heard5.StartsWith("Nobody's talking", StringComparison.Ordinal);
					float band = bandOf[a.artistId];
					Check(!leads.Any(l => l.Voice == ExecutiveVoice.Fixer), "the Fixer is quiet at 1");
					if (street <= 2) {
						Check((text != null) == noisy && (text == null || (text.StartsWith("Everybody's talking", StringComparison.Ordinal) || text.StartsWith("Word in the rooms", StringComparison.Ordinal))
							&& !text.Contains("not the band") && !text.Contains("real thing")), $"Street {street}, {a.stageName}: the noise, talked up, never read");
						if (noisy && band < PlayerDesk.LeadOrdinaryBand && street == 2) hypedOrdinary++;
						if (!noisy && band >= PlayerDesk.LeadGoodBand && heard5 != null && street == 2) gemsHeardLow++;
					} else if (street == 3) Check((text != null) == noisy && (text == null || !text.Contains("not the band") && !text.Contains("real thing") && !text.Contains("Everybody")),
						$"Street 3, {a.stageName}: the noise, plainly");
					else if (street == 4) {
						Check((text != null) == noisy && (text == null || text.Contains("It's the room, not the band") == (band < PlayerDesk.LeadOrdinaryBand)),
							$"Street 4, {a.stageName}: says when the noise is the room's");
						if (noisy && band < PlayerDesk.LeadOrdinaryBand) flaggedOrdinary++;
					} else {
						bool gem = !noisy && band >= PlayerDesk.LeadGoodBand && SceneRoomCatalog.ForPlace(Town(a)).Any(r => LocalSceneRoomService.ShowsAt(r.Id, a.artistId) > 0);
						Check(gem == (text?.StartsWith("Nobody's talking", StringComparison.Ordinal) == true), $"Street 5, {a.stageName}: hears about a good band nobody's talking about");
						gems += gem ? 1 : 0;
					}
				}
			}
			GD.Print($"fog: at Street 2, {hypedOrdinary} loud ordinary bands hyped and {gemsHeardLow} good quiet ones unheard; Street 4 flags {flaggedOrdinary}; Street 5 hears {gems} quiet good bands");
			Check(hypedOrdinary == flaggedOrdinary, "every ordinary band a low Street hypes, a Street of 4 calls the room's");

			// A booker speaks only once you've met them, for acts that drew in their room.
			Voices(3, 1);
			var regular = acts.Select(a => (a, room: SceneRoomCatalog.Get(LocalSceneRoomService.EngagementRoom(a.artistId, today)))).FirstOrDefault(x => x.room != null && !desk.KnowsSceneBooker(x.room.Id));
			if (regular.a == null) GD.Print("note: no free resident with an unmet booker; the booker leg isn't exercised");
			else {
				Check(!desk.LeadsFor(regular.a.artistId, Town(regular.a), today + 2).Any(l => l.Source == regular.room.ContactName), "no booker lead before you've met them");
				desk.MeetBookerForCheck(regular.room.Id);
				var lead = desk.LeadsFor(regular.a.artistId, Town(regular.a), today + 2).FirstOrDefault(l => l.Source == regular.room.ContactName);
				Check(lead != null && lead.Text.Contains("regulars") && lead.Text.Contains(LocalSceneRoomService.Date(today + 2).DayOfWeek.ToString()),
					$"{regular.room.ContactName} recommends their regulars, on the night you'd go: {lead?.Text}");
				Check(acts.Where(a => desk.LeadsFor(a.artistId, Town(a), today).Any(l => l.Source == regular.room.ContactName))
					.All(a => LocalSceneRoomService.ShowsAt(regular.room.Id, a.artistId) > 0), "the booker only speaks for acts that played the room");
			}

			// The Fixer: only when a market really looked, from 3, for four weeks, while they're free.
			var target = acts.First(a => !at5[a.artistId].Any(l => l.Voice == ExecutiveVoice.Fixer));
			var rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned);
			Voices(3, 3);
			Check(!desk.LeadsFor(target.artistId, Town(target), today).Any(l => l.Voice == ExecutiveVoice.Fixer), "no Fixer lead without a look");
			desk.AddRivalLookForCheck(target.artistId, rival.labelId, today - 3);
			Check(desk.LeadsFor(target.artistId, Town(target), today).FirstOrDefault(l => l.Voice == ExecutiveVoice.Fixer)?.Text.Contains(rival.labelName) == true,
				"the Fixer names the label that looked");
			Voices(3, 2);
			Check(!desk.LeadsFor(target.artistId, Town(target), today).Any(l => l.Voice == ExecutiveVoice.Fixer), "a Fixer of 2 hasn't heard");
			Voices(3, 3);
			var stale = acts.First(a => a != target);
			desk.AddRivalLookForCheck(stale.artistId, rival.labelId, today - PlayerDesk.RivalLookDays - 1);
			Check(!desk.LeadsFor(stale.artistId, Town(stale), today).Any(l => l.Voice == ExecutiveVoice.Fixer), "a look more than four weeks old is gone");
			var copy = JsonSerializer.Deserialize<PlayerSaveData>(Json(desk.CaptureState()), SaveGameService.TestJsonOptions);
			Check(desk.RestoreState(copy, out message), "player state restores: " + message);
			Check(desk.LeadsFor(target.artistId, Town(target), today).Any(l => l.Voice == ExecutiveVoice.Fixer), "the look survives the save");
			GD.Print($"rival looks recorded from the AI market this week: {desk.RivalLookCountForCheck - 2}");

			// NEXT SETS shows them.
			Voices(3, 3);
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			await Frame();
			var panel = Nodes(this).OfType<PlayerDeskPanel>().Single();
			panel.OpenAtTab("A&R"); await Frame(0.5);
			var shown = desk.ScoutingItinerary().Where(o => o.Affordable && string.IsNullOrEmpty(am.GetArtist(o.ArtistId)?.labelId))
				.SelectMany(o => desk.LeadsFor(o.ArtistId, o.CityId, o.Day)).ToList();
			bool Shows(string text) => Nodes(panel).OfType<Label>().Any(l => l.IsVisibleInTree() && l.Text.Contains(text, StringComparison.Ordinal));
			if (shown.Count == 0) GD.Print("note: no set on today's NEXT SETS has a lead");
			else Check(shown.Any(l => Shows($"{l.Source}: {l.Text}")), "NEXT SETS puts the town's talk on the cards");

			GD.Print($"LEADS_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("LEADS_CHECKS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
