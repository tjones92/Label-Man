using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Instincts in the fog (SigningExperienceAudit §3.6, inner-life §7): the player hears the music with the Ear and the
/// room with the Street. Checks that
///   - the instinct is the main part of the fog: over every set on today's bills in every scene town, heard by many
///     labels' blind spots in their own rooms, a read lands in the wrong word band far more often at 1 than at 5;
///   - a hired listener (ability, not instinct) reads exactly as before;
///   - a set heard at the desk is read at the instincts held that night (Ear for voice, playing, material; Street for
///     stage presence), and raising an instinct afterwards never rewrites what was heard;
///   - per heard set and axis, a higher instinct is never further off, the error keeps its direction (the blind spot
///     shrinks, it doesn't move), and the Ear never touches the stage read nor the Street the music.
///   --path . SimTools/FogInstinctChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--set-city=detroit]
/// Prints FOG_INSTINCT_CHECKS_PASS / _FAIL.
/// </summary>
public partial class FogInstinctChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static string Key(ScenePerformanceSnapshot s) => $"{s.Vocals:R}|{s.Execution:R}|{s.Material:R}|{s.Communication:R}";

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			for (int s = 1; s < 6; s++) Check(PlayerDesk.InstinctNoise(s + 1) < PlayerDesk.InstinctNoise(s), $"The fog narrows from {s} to {s + 1}");

			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			string city = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--set-city="))?.Substring("--set-city=".Length) ?? "nashville";
			Check(desk.FoundLabel("Fog Instinct Checks", city, out string message), message);
			desk.DismissLienMorning();
			var profileNow = desk.InstinctProfile;
			// Heard at Ear 2, Street 5: different on purpose, so a crossed wire shows.
			profileNow.TheEar = 2; profileNow.TheStreet = 5;
			var bills = LocalSceneRoomService.Calendar(city, clock.CurrentDate)
				.Where(b => b.Status == SceneBillStatus.Scheduled).OrderBy(b => b.StartHour).ToList();
			var heard = new List<(ScenePerformanceSnapshot live, string eventId, string artistId)>();
			foreach (var bill in bills) foreach (var slot in bill.Appearances.Where(a => a.EndHour <= clock.HardStopHour).OrderBy(a => a.StartHour)) {
				if (heard.Count >= 6 || slot.StartHour < clock.CurrentHour) continue;
				clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
				string eventId = ScenePerformanceService.EventId(bill, slot);
				if (desk.ListenSceneAppearance(bill.Id, eventId, out message)) heard.Add((LocalSceneRoomService.Bill(bill.Id)?.Appearances.FirstOrDefault(a => ScenePerformanceService.EventId(bill, a) == eventId)?.Performance, eventId, slot.ArtistId));
			}
			Check(heard.Count >= 2 && heard.All(h => h.live != null), $"Heard {heard.Count} sets on the first day in {city}, each with its performance");

			string labelId = desk.Label.labelId;
			int samples = 0, moved = 0;
			foreach (var (live, eventId, artistId) in heard) {
				var obs = desk.ScoutingDossiers.First(d => d.ArtistId == artistId).Observations
					.Last(o => o.Kind == ScoutingEvidenceKind.Performance && o.EventId == eventId);
				var room = SceneRoomCatalog.Get(obs.RoomId).Kind;
				ScenePerformanceSnapshot At(int ear, int street) => ScenePerformanceService.InterpretAt(live, labelId,
					PlayerDesk.InstinctNoise(ear), PlayerDesk.InstinctNoise(street), obs.Coverage, room);
				Check(Key(obs.Interpretation) == Key(At(2, 5)), $"{obs.ArtistName}: heard at Ear 2 for the music and Street 5 for the room");
				float a = .5f, n = .08f + .20f * (1 - a);
				Check(Key(ScenePerformanceService.Interpret(live, labelId, a, obs.Coverage, room)) ==
					Key(ScenePerformanceService.InterpretAt(live, labelId, n, n, obs.Coverage, room)), $"{obs.ArtistName}: a hired listener reads exactly as before");

				// Raising the instincts after the night doesn't rewrite the notes.
				string before = Key(obs.Interpretation);
				profileNow.TheEar = 5; profileNow.TheStreet = 1;
				desk.BuildSetScene(artistId); desk.ScoutingPlayerNotes(artistId);
				var again = desk.ScoutingDossiers.First(d => d.ArtistId == artistId).Observations.Last(o => o.EventId == eventId);
				Check(Key(again.Interpretation) == before, $"{obs.ArtistName}: a later instinct leaves what was heard alone");
				profileNow.TheEar = 2; profileNow.TheStreet = 5;

				var axes = new (string name, Func<ScenePerformanceSnapshot, float?> get, bool street)[] {
					("vocals", s => s.Vocals, false), ("playing", s => s.Execution, false), ("material", s => s.Material, false), ("stage", s => s.Communication, true) };
				foreach (var (name, get, street) in axes) {
					if (get(live) is not float truth) continue;
					for (int s = 1; s <= 5; s++) {
						float lo = get(street ? At(3, s) : At(s, 3)).Value, hi = get(street ? At(3, s + 1) : At(s + 1, 3)).Value;
						Check(Math.Abs(hi - truth) <= Math.Abs(lo - truth) + 1e-6f, $"{obs.ArtistName} {name}: {s + 1} is no further off than {s}");
						if (Math.Abs(hi - truth) > 1e-4f && hi > 0 && hi < 1) Check(Math.Sign(hi - truth) == Math.Sign(lo - truth), $"{obs.ArtistName} {name}: the error keeps its direction from {s} to {s + 1}");
						samples++; if (Math.Abs(hi - lo) > 1e-6f) moved++;
						// The other instinct is inert on this axis.
						float other = get(street ? At(1, s) : At(s, 1)).Value, other5 = get(street ? At(5, s) : At(s, 5)).Value;
						Check(other == other5, $"{obs.ArtistName} {name}: the {(street ? "Ear" : "Street")} doesn't move it");
					}
					GD.Print($"  {obs.ArtistName} {name}: true {truth:0.000}; read at 1..5 " +
						string.Join(" ", Enumerable.Range(1, 5).Select(s => get(street ? At(3, s) : At(s, 3)).Value.ToString("0.000"))));
				}
			}
			Check(moved > samples / 2, $"An instinct point moves most reads ({moved} of {samples})");

			// The whole first day across the map: every performance as it would be resolved, read at full coverage in its
			// own room by 24 different blind spots (the bias is keyed per label and axis).
			int Band(float v) => v >= ScoutingImpressionPhrases.High ? 2 : v >= ScoutingImpressionPhrases.Middle ? 1 : 0;
			var lives = new List<(ScenePerformanceSnapshot live, SceneRoomKind room)>();
			foreach (var place in ScenePlaceRegistry.All) foreach (var bill in LocalSceneRoomService.Calendar(place.Id, clock.CurrentDate))
				foreach (var slot in bill.Appearances) {
					var artist = ArtistManager.Instance.GetArtist(slot.ArtistId); var kind = SceneRoomCatalog.Get(bill.RoomId)?.Kind;
					if (artist != null && kind.HasValue) lives.Add((ScenePerformanceService.Resolve(bill, slot, artist), kind.Value));
				}
			Check(lives.Count >= 200, $"{lives.Count} performances on today's bills across the scene towns");
			var observers = Enumerable.Range(0, 24).Select(i => $"fog-observer-{i}").ToArray();
			var music = new double[7]; var stage = new double[7]; var musicErr = new double[7];
			for (int s = 1; s <= 6; s++) {
				int mw = 0, mn = 0, sw = 0, sn = 0; double me = 0;
				foreach (var (live, room) in lives) foreach (string who in observers) {
					var read = ScenePerformanceService.InterpretAt(live, who, PlayerDesk.InstinctNoise(s), PlayerDesk.InstinctNoise(s), 1, room);
					foreach (var (t, r) in new[] { (live.Vocals, read.Vocals), (live.Execution, read.Execution), (live.Material, read.Material) })
						if (t is float tv && r is float rv) { mn++; me += Math.Abs(rv - tv); if (Band(rv) != Band(tv)) mw++; }
					sn++; if (Band(read.Communication) != Band(live.Communication)) sw++;
				}
				music[s] = (double)mw / mn; stage[s] = (double)sw / sn; musicErr[s] = me / mn;
				GD.Print($"  instinct {s}: music wrong band {music[s]:P0} (mean error {musicErr[s]:0.000}), stage presence wrong band {stage[s]:P0}");
			}
			for (int s = 1; s < 6; s++) Check(music[s + 1] < music[s] && stage[s + 1] < stage[s], $"Fewer wrong bands at {s + 1} than {s}");
			Check(music[1] >= .25 && music[5] <= .10 && music[1] - music[5] >= .2, $"Ear 1 misreads the music often ({music[1]:P0}), Ear 5 rarely ({music[5]:P0})");
			Check(stage[1] - stage[5] >= .2, $"Street 1 misreads the room often ({stage[1]:P0}), Street 5 rarely ({stage[5]:P0})");
			GD.Print($"FOG_INSTINCT_CHECKS_PASS {checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("FOG_INSTINCT_CHECKS_FAIL " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
