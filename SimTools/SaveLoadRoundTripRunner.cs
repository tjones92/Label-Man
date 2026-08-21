using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Headless verification for the full-world save (player-loop-cleanup). Runs the generated AI world forward
/// a few weeks, then round-trips the world snapshot: Capture -> serialize (A) -> deserialize -> Apply
/// (rehydrate in place) -> Capture -> serialize (B). A byte-identical A == B proves the capture/rehydrate
/// pair is lossless for every world section implemented so far -- any field that is captured but not restored
/// (or vice versa) shows up as a diff. This is the plan's "round-trip byte-identity" check.
///
/// Launch (mirrors ChartAuditRunner), e.g.:
///   Godot_console.exe --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- \
///     --weeks=26 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
///
/// Exit code 0 = PASS, 1 = FAIL (diff printed), 3 = error.
/// </summary>
public partial class SaveLoadRoundTripRunner : Node {
	public override void _Ready() {
		try {
			int weeks = 26;
			bool integration = false;
			string inspectSlot = null;
			foreach (string arg in OS.GetCmdlineUserArgs()) {
				if (arg.StartsWith("--weeks=", StringComparison.Ordinal)) weeks = int.Parse(arg["--weeks=".Length..]);
				else if (arg == "--integration") integration = true;
				else if (arg.StartsWith("--inspect-slot=", StringComparison.Ordinal)) inspectSlot = arg["--inspect-slot=".Length..];
			}

			if (TimeManager.Instance == null || ChartManager.Instance == null)
				throw new InvalidOperationException("TimeManager and ChartManager autoloads must be available.");

			// Inspecting a real save loads it over the freshly generated world; it must NOT be run forward first.
			if (inspectSlot != null) { RunInspect(inspectSlot); return; }

			for (int w = 0; w < weeks && !TimeManager.Instance.IsGameOver; w++) AdvanceOneChartWeek();

			if (integration) { RunIntegration(weeks); return; }

			JsonSerializerOptions opts = SaveGameService.TestJsonOptions;

			WorldSaveData cap1 = WorldStateService.Capture();
			string a = JsonSerializer.Serialize(cap1, opts);

			WorldSaveData restored = JsonSerializer.Deserialize<WorldSaveData>(a, opts);
			WorldStateService.Apply(restored, TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);

			WorldSaveData cap2 = WorldStateService.Capture();
			string b = JsonSerializer.Serialize(cap2, opts);

			if (a == b) {
				// Also exercise the real gzip + Godot FileAccess buffer IO path end-to-end, and report the
				// compressed size (the actual on-disk save size for this world).
				string gzipStatus = VerifyGzipFileIo(cap1, opts, out long compressedBytes);
				GD.Print($"SAVELOAD_ROUNDTRIP_PASS weeks={weeks} bytes={a.Length} gzipBytes={compressedBytes} " +
					$"ratio={(a.Length / (double)Math.Max(1, compressedBytes)):F1}x gzipIo={gzipStatus} " +
					$"labels={cap1.Labels.Count} artists={cap1.Artists.Count} unsigned={cap1.UnsignedArtistIds.Count} " +
					$"defunct={cap1.DefunctLabelIds.Count} chartWeek={cap1.ChartWeek}");
				GetTree().Quit(gzipStatus == "ok" ? 0 : 2);
			} else {
				int i = 0;
				while (i < a.Length && i < b.Length && a[i] == b[i]) i++;
				string dir = OS.GetUserDataDir();
				System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "roundtrip_a.json"), a);
				System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "roundtrip_b.json"), b);
				GD.Print($"SAVELOAD_ROUNDTRIP_FAIL weeks={weeks} lenA={a.Length} lenB={b.Length} firstDiff={i} dumped={dir}");
				GetTree().Quit(1);
			}
		} catch (Exception exception) {
			GD.PushError("SAVELOAD_ROUNDTRIP_ERROR: " + exception);
			GetTree().Quit(3);
		}
	}

	/// <summary>Mirrors SaveGameService's gzip file IO: stream a world through GZipStream into a buffer, store it
	/// with Godot FileAccess, read it back, decompress, and confirm it deserializes+reserializes to the same
	/// JSON. Returns "ok" or a failure detail, and the compressed byte count.</summary>
	private static string VerifyGzipFileIo(WorldSaveData world, JsonSerializerOptions opts, out long compressedBytes) {
		compressedBytes = 0;
		try {
			string expected = JsonSerializer.Serialize(world, opts);
			byte[] payload;
			using (var buffer = new System.IO.MemoryStream()) {
				using (var gz = new System.IO.Compression.GZipStream(buffer, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
					JsonSerializer.Serialize(gz, world, opts);
				payload = buffer.ToArray();
			}
			compressedBytes = payload.LongLength;

			const string path = "user://saves/_roundtrip_probe.json";
			DirAccess.MakeDirRecursiveAbsolute("user://saves");
			using (Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write)) {
				if (file == null) return "open-write-failed";
				file.StoreBuffer(payload);
			}
			byte[] readBack;
			using (Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read)) {
				if (file == null) return "open-read-failed";
				readBack = file.GetBuffer((long)file.GetLength());
			}
			string actual;
			using (var input = new System.IO.MemoryStream(readBack))
			using (var gz = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress)) {
				WorldSaveData restored = JsonSerializer.Deserialize<WorldSaveData>(gz, opts);
				actual = JsonSerializer.Serialize(restored, opts);
			}
			Godot.DirAccess.RemoveAbsolute(path);
			return actual == expected ? "ok" : "content-mismatch";
		} catch (Exception e) {
			return "error:" + e.GetType().Name;
		}
	}

	/// <summary>End-to-end shipping-flow test: found a player label, save to disk (gzip), perturb the live world
	/// (advance a week, spend the player's cash), load from disk, and confirm both the AI world and the player
	/// came back to the save-time state -- proving WorldStateService.Apply + PlayerDesk.RestoreState + the gzip
	/// file path all cooperate.</summary>
	private void RunIntegration(int weeks) {
		const string slot = "_integration_probe";
		try {
			// Found a player label in the first available city.
			var cities = DistanceModel.GetCities();
			string cityId = cities.Count > 0 ? cities[0].cityId : null;
			if (cityId == null || PlayerDesk.Instance == null) { GD.Print("SAVELOAD_INTEGRATION_FAIL reason=no-city-or-desk"); GetTree().Quit(3); return; }
			if (!PlayerDesk.Instance.FoundLabel("Probe Records", cityId, out string founded)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=found:{founded}"); GetTree().Quit(3); return; }

			// Save-time snapshot of world + player.
			int week0 = ChartManager.Instance.GetCurrentChartWeek();
			int labels0 = ChartManager.Instance.GetAllLabels().Count;
			int artists0 = ArtistManager.Instance.GetAllArtists().Count;
			AILabel sample0 = ChartManager.Instance.GetAllLabels().FirstOrDefault(l => !l.isPlayerOwned);
			string sampleId = sample0?.labelId; float sampleCash0 = sample0?.cashReserves ?? 0f;
			string playerName0 = PlayerDesk.Instance.Label.labelName; float playerCash0 = PlayerDesk.Instance.Label.cashReserves;

			if (!SaveGameService.Save(slot, out string saveMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=save:{saveMsg}"); GetTree().Quit(1); return; }

			// Perturb: advance a week (moves the whole world) and drain the player's cash.
			AdvanceOneChartWeek();
			PlayerDesk.Instance.Label.cashReserves = -99999f;
			int weekPerturbed = ChartManager.Instance.GetCurrentChartWeek();

			if (!SaveGameService.Load(slot, out string loadMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=load:{loadMsg}"); GetTree().Quit(1); return; }

			// Verify everything is back to the save-time state.
			int week1 = ChartManager.Instance.GetCurrentChartWeek();
			int labels1 = ChartManager.Instance.GetAllLabels().Count;
			int artists1 = ArtistManager.Instance.GetAllArtists().Count;
			float sampleCash1 = ChartManager.Instance.GetLabelById(sampleId)?.cashReserves ?? float.NaN;
			string playerName1 = PlayerDesk.Instance.Label?.labelName;
			float playerCash1 = PlayerDesk.Instance.Label?.cashReserves ?? float.NaN;

			// Advance one week past the load. This is the exact path that regressed: the post-load settlement
			// rejects as out-of-order if CompetitorManager's lastBookedSettlementId wasn't restored in step with
			// the chart week. A throw here surfaces via the outer catch as SAVELOAD_INTEGRATION_ERROR.
			AdvanceOneChartWeek();
			int week2 = ChartManager.Instance.GetCurrentChartWeek();

			// Re-save after the post-load week. New AI records minted this week collide with restored ids and the
			// record-capture ToDictionary throws unless generatedRecordCounter was restored -- the exact overwrite
			// path that regressed. A throw surfaces via the outer catch as SAVELOAD_INTEGRATION_ERROR.
			if (!SaveGameService.Save(slot, out string resaveMsg)) { GD.Print($"SAVELOAD_INTEGRATION_FAIL reason=resave:{resaveMsg}"); GetTree().Quit(1); return; }

			var fails = new System.Collections.Generic.List<string>();
			if (week1 != week0) fails.Add($"week {week0}->{week1} (perturbed {weekPerturbed})");
			if (week2 != week1 + 1) fails.Add($"postLoadWeek {week1}->{week2} (expected +1)");
			if (labels1 != labels0) fails.Add($"labels {labels0}->{labels1}");
			if (artists1 != artists0) fails.Add($"artists {artists0}->{artists1}");
			if (Math.Abs(sampleCash1 - sampleCash0) > 0.01f) fails.Add($"aiLabelCash {sampleCash0}->{sampleCash1}");
			if (playerName1 != playerName0) fails.Add($"playerName {playerName0}->{playerName1}");
			if (Math.Abs(playerCash1 - playerCash0) > 0.01f) fails.Add($"playerCash {playerCash0}->{playerCash1}");

			SaveGameService.Delete(slot);
			if (fails.Count == 0) {
				GD.Print($"SAVELOAD_INTEGRATION_PASS weeks={weeks} week={week0} labels={labels0} artists={artists0} " +
					$"aiLabelCash={sampleCash0:F1} player={playerName0} playerCash={playerCash0:F1}");
				GetTree().Quit(0);
			} else {
				GD.Print("SAVELOAD_INTEGRATION_FAIL " + string.Join(" | ", fails));
				GetTree().Quit(1);
			}
		} catch (Exception e) {
			GD.PushError("SAVELOAD_INTEGRATION_ERROR: " + e);
			try { SaveGameService.Delete(slot); } catch { }
			GetTree().Quit(3);
		}
	}

	/// <summary>
	/// Loads a real save from disk and prints what the desk actually holds afterwards: the discography, and
	/// the town-stock readout the DISTRIBUTION screen draws from. A stock line whose title is still a bare
	/// record id ("player_2") is the symptom this exists to catch -- a record the dead-stock cull deleted out
	/// from under the references that point at it. Fails on any such line; otherwise diagnostic only.
	/// </summary>
	private void RunInspect(string slot) {
		if (PlayerDesk.Instance == null) { GD.Print("SAVELOAD_INSPECT_ERROR reason=no-desk"); GetTree().Quit(3); return; }
		if (!SaveGameService.Load(slot, out string loadMsg)) {
			GD.Print($"SAVELOAD_INSPECT_FAIL slot={slot} reason=load:{loadMsg}");
			GetTree().Quit(1);
			return;
		}
		PlayerDesk desk = PlayerDesk.Instance;
		GD.Print($"SAVELOAD_INSPECT slot={slot} message=\"{loadMsg}\"");
		int titles = 0;
		foreach (RecordRuntimeData record in desk.ReleasedRecords) {
			titles++;
			GD.Print($"  DISCOGRAPHY {record.baseRecord.recordId} \"{record.baseRecord.title}\" by {record.baseRecord.artistName}" +
				$" released={record.baseRecord.releaseDate} weeks={record.weeksSinceRelease} units={record.totalUnitsSold}");
		}

		int unresolved = 0;
		foreach ((string cityName, string title, int remaining) in desk.TownStock()) {
			bool dangling = System.Text.RegularExpressions.Regex.IsMatch(title, @"^player_\d+$");
			if (dangling) unresolved++;
			GD.Print($"  STOCK {cityName} \"{title}\" remaining={remaining} dangling={dangling}");
		}
		GD.Print(unresolved == 0
			? $"SAVELOAD_INSPECT_PASS slot={slot} discography={titles} danglingStockRefs=0"
			: $"SAVELOAD_INSPECT_FAIL slot={slot} discography={titles} danglingStockRefs={unresolved}");
		GetTree().Quit(unresolved == 0 ? 0 : 1);
	}

	private static void AdvanceOneChartWeek() {
		int start = ChartManager.Instance.GetCurrentChartWeek();
		while (ChartManager.Instance.GetCurrentChartWeek() == start && !TimeManager.Instance.IsGameOver)
			TimeManager.Instance.DebugAdvanceWeek();
	}
}
