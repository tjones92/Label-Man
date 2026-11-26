using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Instincts in the fog (SigningExperienceAudit §3.6, inner-life §7): the player hears the music with the Ear and the
/// room with the Street. Checks that
///   - every founding archetype's scouting ability is its founding Ear on the hearing scale, so founding reads don't move;
///   - a listener with no separate stage ability reads exactly as before;
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
			foreach (var (kind, profile) in FoundingArchetypeData.All)
				Check(Math.Abs(PlayerDesk.InstinctHearing(profile.Instincts.TheEar) - profile.ScoutingAbility) < 1e-6f,
					$"{profile.Name}: founding Ear {profile.Instincts.TheEar} hears at the archetype's scouting ability {profile.ScoutingAbility}");
			for (int s = 1; s < 6; s++) Check(PlayerDesk.InstinctHearing(s + 1) > PlayerDesk.InstinctHearing(s), $"Hearing rises from {s} to {s + 1}");

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
				ScenePerformanceSnapshot At(int ear, int street) => ScenePerformanceService.Interpret(live, labelId,
					PlayerDesk.InstinctHearing(ear), obs.Coverage, room, PlayerDesk.InstinctHearing(street));
				Check(Key(obs.Interpretation) == Key(At(2, 5)), $"{obs.ArtistName}: heard at Ear 2 for the music and Street 5 for the room");
				float a = PlayerDesk.InstinctHearing(3);
				Check(Key(ScenePerformanceService.Interpret(live, labelId, a, obs.Coverage, room)) ==
					Key(ScenePerformanceService.Interpret(live, labelId, a, obs.Coverage, room, a)), $"{obs.ArtistName}: no separate stage ability reads as before");

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
			GD.Print($"FOG_INSTINCT_CHECKS_PASS {checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("FOG_INSTINCT_CHECKS_FAIL " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
