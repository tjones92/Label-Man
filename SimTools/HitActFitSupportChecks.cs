using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class HitActFitSupportChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Hit-act fit support: " + why); checks++; }
		bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;
		RecordRuntimeData Launch(string id = "fit-support", ReleaseFormat format = ReleaseFormat.Single, bool player = false,
			int year = 1961) => new(new Record { recordId = id, artistId = "support-act", format = format,
				isPlayerOwned = player, releaseDate = new GameDate(year, 3, 1) }) { initialLaunchAwareness = .55f };
		try {
			SingleCommercialFit.SetForChecks(2.5f, .5f, .88f, 1001UL, hitShare: .15f, upside: .15f, launchWeight: 1.25f, floor: .12f);
			var hit = new SimulatedArtist { artistId = "support-act", hypeLastTop10Week = 100 };
			HitActFitSupport.SetForChecks(false);
			var off = Launch();
			HitActFitSupport.CaptureLaunch(off, hit, 100);
			Check(!off.hitActFitFloorAtLaunch.HasValue, "off writes no launch state");
			Check(HitActFitSupport.Apply(off, -2d) == -2d, "off is an exact arithmetic identity");
			Check(SingleCommercialFit.Multiplier(off, "south", null) == SingleCommercialFit.WithoutHitSupport(off, "south", null), "off fit path matches the old path");

			HitActFitSupport.SetForChecks(true, 1f);
			var hot = Launch();
			HitActFitSupport.CaptureLaunch(hot, hit, 100);
			Check(hot.hitActFitFloorAtLaunch == 1f, "recent Top 10 captures the peak floor");
			Check(HitActFitSupport.Apply(hot, Math.Log(.12d)) == 0d, "only the national downside is lifted");
			Check(HitActFitSupport.Apply(hot, Math.Log(3d)) == Math.Log(3d), "connected national upside is bit-for-bit unchanged");
			Check(Near(HitActFitSupport.Recency(hit, 126), .5d), "half strength after 26 weeks");
			Check(Near(HitActFitSupport.Recency(hit, 152), .25d) && HitActFitSupport.Recency(hit, 153) == 0f, "expires outside the 52-week hot window");
			Check(HitActFitSupport.Recency(hit, 99) == 0f, "future hits cannot leak backward");
			var half = Launch(); HitActFitSupport.CaptureLaunch(half, hit, 126);
			Check(Near(half.hitActFitFloorAtLaunch.Value, .56d), "half-life blends back toward the existing floor");
			var old = Launch(); HitActFitSupport.CaptureLaunch(old, hit, 153);
			Check(old.hitActFitFloorAtLaunch == 0f && HitActFitSupport.Apply(old, -3d) == -3d, "cold launches retain their response");
			var top40 = new SimulatedArtist { hypeLastTop10Week = HitActHype.Unset + 1, hypeLastTop40Week = 100 };
			Check(HitActFitSupport.Recency(top40, 100) == 0f, "Top 40 alone does not activate this probe");
			Check(HitActFitSupport.Recency(new SimulatedArtist(), 100) == 0f, "a never-hit act has no support");
			var veteran = new SimulatedArtist(); veteran.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 8 });
			Check(HitActFitSupport.Recency(veteran, 0) == .5f, "opening historical Top 10 seeds mid-1959");
			Check(veteran.hypeLastTop10Week == HitActHype.Unset, "history lookup adds no new artist state");

			var album = Launch(format: ReleaseFormat.Album); HitActFitSupport.CaptureLaunch(album, hit, 100);
			var player = Launch(player: true); HitActFitSupport.CaptureLaunch(player, hit, 100);
			var opening = Launch(year: 1959); HitActFitSupport.CaptureLaunch(opening, hit, 100);
			Check(!album.hitActFitFloorAtLaunch.HasValue && !player.hitActFitFloorAtLaunch.HasValue && !opening.hitActFitFloorAtLaunch.HasValue, "albums, player records and opening catalog are unchanged");
			var cold = Launch(); HitActFitSupport.CaptureLaunch(cold, new SimulatedArtist(), 100);
			hit.hypeLastTop10Week = 126;
			HitActFitSupport.CaptureLaunch(half, hit, 126); HitActFitSupport.CaptureLaunch(cold, hit, 126);
			Check(Near(half.hitActFitFloorAtLaunch.Value, .56d) && cold.hitActFitFloorAtLaunch == 0f, "own/later hits never resnapshot a released single");
			var legacy = Launch();
			Check(HitActFitSupport.Apply(legacy, -3d) == -3d && !legacy.hitActFitFloorAtLaunch.HasValue, "old active saves are not retroactively rescued");

			var stateBefore = GameRandom.State;
			for (int n = 0; n < 500; n++) {
				var r = Launch("fit-check-" + n); r.hitActFitFloorAtLaunch = .8f;
				float original = SingleCommercialFit.WithoutHitSupport(r, "south", null);
				float supported = SingleCommercialFit.Multiplier(r, "south", null);
				Check(supported >= original, "support never reduces a record's national response");
				Check(supported == SingleCommercialFit.Multiplier(r, "south", null), "response remains persistent");
			}
			Check(GameRandom.State == stateBefore, "capture/response adds no global random draw");
			Check(SingleCommercialFit.Multiplier(hot, "south", null) != SingleCommercialFit.Multiplier(hot, "west", null), "regional response variation remains");
			string json = JsonSerializer.Serialize(half, SaveGameService.TestJsonOptions);
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(json, SaveGameService.TestJsonOptions);
			Check(restored.hitActFitFloorAtLaunch == half.hitActFitFloorAtLaunch && SingleCommercialFit.Multiplier(restored, "south", null) == SingleCommercialFit.Multiplier(half, "south", null), "whole-world record JSON retains the launch snapshot and response");
			var dto = RuntimeRecordSaveData.From(half).ToRuntime();
			Check(dto.hitActFitFloorAtLaunch == half.hitActFitFloorAtLaunch, "player/transfer DTO retains optional state");
			HitActFitSupport.SetForChecks(false);
			Check(HitActFitSupport.Apply(hot, -2d) == -2d, "disabling the probe ignores stored support");
			GD.Print($"HIT_ACT_FIT_SUPPORT_CHECK passed checks={checks}");
		} finally { HitActFitSupport.SetForChecks(false); }
	}

	public static void CheckLiveState() {
		if (!HitActFitSupport.Enabled) return;
		var records = ChartManager.Instance.GetAllRecords();
		var captured = records.Where(r => r.hitActFitFloorAtLaunch.HasValue).ToList();
		if (captured.Count == 0 || !captured.Any(r => r.hitActFitFloorAtLaunch > 0f))
			throw new InvalidOperationException("Live fit-support round trip has no supported launch state.");
		if (captured.Any(r => r.baseRecord.format != ReleaseFormat.Single || r.baseRecord.isPlayerOwned ||
			!float.IsFinite(r.hitActFitFloorAtLaunch.Value) || r.hitActFitFloorAtLaunch < 0f || r.hitActFitFloorAtLaunch > 1f))
			throw new InvalidOperationException("Invalid live fit-support launch state.");
		GD.Print($"HIT_ACT_FIT_SUPPORT_LIVE passed captured={captured.Count} supported={captured.Count(r => r.hitActFitFloorAtLaunch > 0f)}");
	}
}
