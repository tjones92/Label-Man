using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Environment = System.Environment;

public partial class SaveLoadRoundTripRunner {
	public sealed class StatureCheckpoint {
		public int Version { get; set; } = 1;
		public int ProducerProcessId { get; set; }
		public int SetupWeeks { get; set; }
		public int ReplayWeeks { get; set; }
		public ulong? Seed { get; set; }
		public GameDate Date { get; set; }
		public string[] Flags { get; set; }
		public string AssemblyHash { get; set; }
		public string EngineHash { get; set; }
		public string CalibrationHash { get; set; }
		public string WorldHash { get; set; }
		public string PlayerHash { get; set; }
		public PlayerSaveData Player { get; set; }
		public WorldSaveData World { get; set; }
		public Dictionary<string, float[]> RegionalOpportunity { get; set; }
	}
	public sealed class StatureExpectedWeek {
		public DateTime Date { get; set; }
		public string Evidence { get; set; }
		public string Shadow { get; set; }
		public string Player { get; set; }
		public string Action { get; set; }
		public List<string> Stages { get; set; } = new();
	}
	public sealed class StatureCheckpointReceipt {
		public int ProducerProcessId { get; set; }
		public int Weeks { get; set; }
		public Dictionary<string, string> Files { get; set; } = new(StringComparer.Ordinal);
	}

	private static string CheckpointFileHash(string path) {
		using var stream = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(stream));
	}
	private static string CheckpointJsonHash<T>(T value) {
		using var hash = SHA256.Create();
		using var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write);
		JsonSerializer.Serialize(stream, value, SaveGameService.TestJsonOptions);
		stream.FlushFinalBlock();
		return Convert.ToHexString(hash.Hash);
	}
	private static void WriteCheckpointGzip<T>(string path, T value) {
		using var file = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write);
		using var gzip = new GZipStream(file, CompressionLevel.Fastest);
		JsonSerializer.Serialize(gzip, value, SaveGameService.TestJsonOptions);
	}
	private static T ReadCheckpointGzip<T>(string path) {
		using var file = File.OpenRead(path);
		using var gzip = new GZipStream(file, CompressionMode.Decompress);
		return JsonSerializer.Deserialize<T>(gzip, SaveGameService.TestJsonOptions);
	}
	private static Dictionary<string, float[]> CheckpointRegionalOpportunity() {
		int year = TimeManager.Instance.CurrentDate.year;
		return ChartManager.Instance.GetAllRegions().ToDictionary(region => region.regionId, region =>
			new[] { Genre.RnB, Genre.TraditionalPop, Genre.Jazz, Genre.Country }.SelectMany(genre =>
				new[] { region.GetGenreMarketSize(genre, year), region.GetAlbumMarketSize(genre, year) }).ToArray(), StringComparer.Ordinal);
	}
	private static PlayerSaveData CaptureReplayPlayer(string loadNote) {
		var player = PlayerDesk.Instance.CaptureState();
		if (loadNote != null && player.Log.RemoveAll(entry => entry == loadNote) != 1)
			throw new InvalidOperationException("Expected single player load note is missing or duplicated.");
		return player;
	}

	private bool RunStatureFreshContinuation(int setupWeeks, int replayWeeks, string calibration, bool traceWorld) {
		string[] args = OS.GetCmdlineUserArgs();
		string Value(string prefix) => args.SingleOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
		string write = Value("--stature-checkpoint-write="), read = Value("--stature-checkpoint-read=");
		if (write == null && read == null) return false;
		bool playerSave = args.Contains("--stature-player-save");
		bool activePlayer = args.Contains("--stature-active-player");
		if (activePlayer && (!playerSave || setupWeeks < 4)) throw new ArgumentException("Active player fixture requires a player save and at least four setup weeks.");
		const string playerSlot = "_stature_continuation_probe";
		if ((write != null && read != null) || calibration == null || replayWeeks < 1 || args.Contains("--stature-evidence-only-replay"))
			throw new ArgumentException("Fresh continuation requires one checkpoint mode, shadow calibration and replay weeks.");
		string directory = Path.GetFullPath(write ?? read);
		string[] flags = args.Where(a => !a.StartsWith("--stature-checkpoint-write=", StringComparison.Ordinal) &&
			!a.StartsWith("--stature-checkpoint-read=", StringComparison.Ordinal)).ToArray();
		// Godot can load the managed image from bytes, leaving Assembly.Location empty.
		string assemblyPath = typeof(SaveLoadRoundTripRunner).Assembly.Location;
		if (string.IsNullOrEmpty(assemblyPath)) assemblyPath = ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/Label Man.dll");
		string assemblyHash = CheckpointFileHash(assemblyPath);
		string engineHash = CheckpointFileHash(OS.GetExecutablePath());
		string calibrationHash = CheckpointFileHash(calibration["--stature-live-roundtrip=".Length..]);
		StatureCheckpoint checkpoint;
		StatureCheckpointReceipt receipt;
		string loadNote = null;
		string checkpointPath = Path.Combine(directory, "checkpoint.json.gz");
		string receiptPath = Path.Combine(directory, "ready.json.gz");
		if (write != null) {
			if (Directory.Exists(directory)) throw new InvalidOperationException("Checkpoint directory already exists.");
			Directory.CreateDirectory(directory);
			if (playerSave) {
				string city = DistanceModel.GetCities().FirstOrDefault()?.cityId;
				if (city == null || PlayerDesk.Instance == null) throw new InvalidOperationException("Player desk or founding city unavailable.");
				if (!PlayerDesk.Instance.FoundLabel("Stature Probe Records", city, out string founded))
					throw new InvalidOperationException("Player label founding failed: " + founded);
				// Test capital keeps the scripted label solvent across long continuations.
				if (activePlayer) PlayerDesk.Instance.Label.cashReserves = 250000f;
			}
			PlayerDesk.Master fixtureMaster = null;
			for (int w = 0; w < setupWeeks; w++) {
				if (activePlayer && w == setupWeeks - 4) {
					var act = ArtistManager.Instance.GetUnsignedArtists().Where(a => a.isActive && a.members?.Count > 0)
						.OrderBy(a => a.artistId, StringComparer.Ordinal).FirstOrDefault() ?? throw new InvalidOperationException("No unsigned fixture artist.");
					int year = TimeManager.Instance.CurrentDate.year;
					var sheet = PlayerDesk.Instance.Label.GenerateTermSheet(act, year);
					PlayerDesk.Instance.Label.SignArtist(act, year, sheet);
					ArtistManager.Instance.SignArtist(act, PlayerDesk.Instance.Label.labelId, year);
					// The direct signing fixture bypasses FinalizeSigning, which normally assigns the catalogue number.
					typeof(PlayerDesk).GetMethod("RecordSigningMoment", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
						.Invoke(PlayerDesk.Instance, new object[] { act, sheet, 0f });
					if (!act.isPlayerOwned || !PlayerDesk.Instance.Roster.Contains(act)) throw new InvalidOperationException("Fixture signing failed.");
					fixtureMaster = PlayerDesk.Instance.StageShelfMasterForChecks(act, "stature_player_record", "Stature Continuation", .7f, .6f);
				}
				if (activePlayer && w == setupWeeks - 2) PlayerDesk.Instance.ReleaseMasterForChecks(fixtureMaster);
				AdvanceOneChartWeek();
			}
			if (activePlayer && (!PlayerDesk.Instance.Roster.Any() || !PlayerDesk.Instance.ReleasedRecords.Any(r => r.baseRecord.recordId == "stature_player_record")))
				throw new InvalidOperationException("Active player checkpoint lacks its roster or release.");
			if (playerSave) {
				if (!SaveGameService.Save(playerSlot, out string message)) throw new InvalidOperationException("Player save failed: " + message);
				File.Copy(ProjectSettings.GlobalizePath("user://saves/" + playerSlot + ".json"), Path.Combine(directory, "player-save.json"));
			}
			var world = WorldStateService.Capture();
			if (world.Media?.ExperimentalStature?.Normalizer.LastProcessedDate == default || world.ShadowContinuation == null)
				throw new InvalidOperationException("Checkpoint is missing its dated shadow or continuation state.");
			checkpoint = new() { ProducerProcessId = Environment.ProcessId, SetupWeeks = setupWeeks, ReplayWeeks = replayWeeks,
				Seed = SimulationSeedBootstrap.RequestedSeed, Date = TimeManager.Instance.CurrentDate, Flags = flags,
				AssemblyHash = assemblyHash, EngineHash = engineHash, CalibrationHash = calibrationHash,
				World = world, WorldHash = CheckpointJsonHash(world), Player = playerSave ? PlayerDesk.Instance.CaptureState() : null,
				RegionalOpportunity = CheckpointRegionalOpportunity() };
			if (playerSave) checkpoint.PlayerHash = CheckpointJsonHash(checkpoint.Player);
			if (playerSave && checkpoint.Player.Log.Count >= 60) throw new InvalidOperationException("Player log is full; the load note would displace an entry.");
			WriteCheckpointGzip(checkpointPath, checkpoint);
			receipt = new() { ProducerProcessId = Environment.ProcessId, Weeks = replayWeeks };
			receipt.Files.Add("checkpoint.json.gz", CheckpointFileHash(checkpointPath));
			if (playerSave) receipt.Files.Add("player-save.json", CheckpointFileHash(Path.Combine(directory, "player-save.json")));
			GD.Print($"STATURE_CHECKPOINT_WRITTEN pid={Environment.ProcessId} week={world.ChartWeek} date={checkpoint.Date.year}-{checkpoint.Date.month:D2}-{checkpoint.Date.day:D2}");
		} else {
			receipt = ReadCheckpointGzip<StatureCheckpointReceipt>(receiptPath);
			if (receipt.ProducerProcessId == Environment.ProcessId || receipt.Weeks != replayWeeks)
				throw new InvalidOperationException("Checkpoint must come from a different process with the requested replay length.");
			foreach (var entry in receipt.Files)
				if (CheckpointFileHash(Path.Combine(directory, entry.Key)) != entry.Value) throw new InvalidOperationException("Checkpoint artifact hash differs: " + entry.Key);
			checkpoint = ReadCheckpointGzip<StatureCheckpoint>(checkpointPath);
			if (checkpoint.Version != 1 || checkpoint.ReplayWeeks != replayWeeks || checkpoint.SetupWeeks != setupWeeks ||
				checkpoint.Seed != SimulationSeedBootstrap.RequestedSeed || !checkpoint.Flags.SequenceEqual(flags) ||
				checkpoint.AssemblyHash != assemblyHash || checkpoint.EngineHash != engineHash || checkpoint.CalibrationHash != calibrationHash)
				throw new InvalidOperationException("Fresh-process checkpoint configuration or build differs.");
			if (playerSave) {
				string saves = ProjectSettings.GlobalizePath("user://saves");
				Directory.CreateDirectory(saves);
				File.Copy(Path.Combine(directory, "player-save.json"), Path.Combine(saves, playerSlot + ".json"));
				if (!SaveGameService.Load(playerSlot, out string message)) throw new InvalidOperationException("Player load failed: " + message);
				if (PlayerDesk.Instance?.HasLabel != true) throw new InvalidOperationException("Player label missing after load.");
				loadNote = $"{checkpoint.Date.ToShortString()}  {message}";
				var restoredPlayer = CaptureReplayPlayer(loadNote);
				if (CheckpointJsonHash(restoredPlayer) != checkpoint.PlayerHash)
					CompareFreshJson(JsonSerializer.Serialize(checkpoint.Player, SaveGameService.TestJsonOptions),
						JsonSerializer.Serialize(restoredPlayer, SaveGameService.TestJsonOptions), "player after load", 0);
			} else WorldStateService.Apply(checkpoint.World, checkpoint.Date, checkpoint.Seed);
			CompareFreshJson(JsonSerializer.Serialize(checkpoint.RegionalOpportunity), JsonSerializer.Serialize(CheckpointRegionalOpportunity()), "regional-opportunity", 0);
			var rehydrated = WorldStateService.Capture();
			if (CheckpointJsonHash(rehydrated) != checkpoint.WorldHash) {
				ReportFreshWorldDifferences(checkpointPath, rehydrated, 0, "World");
				throw new InvalidOperationException("Fresh-process checkpoint capture/rehydration differs.");
			}
			GD.Print($"STATURE_CHECKPOINT_RESTORED producerPid={checkpoint.ProducerProcessId} consumerPid={Environment.ProcessId} week={checkpoint.World.ChartWeek}");
		}
		DateTime start = checkpoint.World.Media.ExperimentalStature.Normalizer.LastProcessedDate;
		// The saved DTO is a live object graph after Apply; retain only the immutable start date below.
		checkpoint = null;
		try {
			for (int w = 0; w < replayWeeks; w++) {
				int observations = 0;
				var observed = new StatureExpectedWeek();
				if (activePlayer && w == 0) {
					bool ok = PlayerDesk.Instance.PurchaseAnsweringService(out string message);
					if (!ok) throw new InvalidOperationException("Fixture answering service failed: " + message);
					observed.Action = "answering-service:" + message;
				} else if (activePlayer && w == 1) {
					bool ok = PlayerDesk.Instance.WorkThePhones(out string message);
					if (!ok) throw new InvalidOperationException("Fixture phone action failed: " + message);
					observed.Action = "phones:" + message;
				}
				StatureReplayDiagnostics.Observer = traceWorld ? observed.Stages.Add : null;
				StatureObservation.ReplayEvidenceObserver = (date, evidence, _) => {
					observations++; observed.Date = new DateTime(date.year, date.month, date.day); observed.Evidence = JsonSerializer.Serialize(evidence, SaveGameService.TestJsonOptions);
				};
				if (TimeManager.Instance.IsGameOver) throw new InvalidOperationException("Game ended during fresh continuation.");
				AdvanceOneChartWeek();
				StatureReplayDiagnostics.Observer = null;
				StatureObservation.ReplayEvidenceObserver = null;
				if (observations != 1 || observed.Date != start.AddDays(7 * (w + 1))) throw new InvalidOperationException("Fresh continuation observation clock differs.");
				observed.Shadow = JsonSerializer.Serialize(MediaAtmosphereService.ExperimentalStatureShadow(), SaveGameService.TestJsonOptions);
				if (playerSave) observed.Player = JsonSerializer.Serialize(CaptureReplayPlayer(loadNote), SaveGameService.TestJsonOptions);
				if (traceWorld && (w == 0 || w == replayWeeks - 1)) {
					string worldName = $"world-week-{w + 1:D3}.json.gz", worldPath = Path.Combine(directory, worldName);
					if (write != null) { WriteCheckpointGzip(worldPath, WorldStateService.Capture()); receipt.Files.Add(worldName, CheckpointFileHash(worldPath)); }
					else {
						var actualWorld = WorldStateService.Capture();
						WriteCheckpointGzip(Path.Combine(directory, "resumed-" + worldName), actualWorld);
						ReportFreshWorldDifferences(worldPath, actualWorld, w + 1);
					}
				}
				string name = $"week-{w + 1:D3}.json.gz", path = Path.Combine(directory, name);
				if (write != null) {
					WriteCheckpointGzip(path, observed); receipt.Files.Add(name, CheckpointFileHash(path));
				} else {
					var expected = ReadCheckpointGzip<StatureExpectedWeek>(path);
					for (int s = 0; s < Math.Max(expected.Stages.Count, observed.Stages.Count); s++) {
						string a = s < expected.Stages.Count ? expected.Stages[s] : "missing", b = s < observed.Stages.Count ? observed.Stages[s] : "missing";
						if (a != b) { GD.Print($"STATURE_FRESH_STAGE_DIFF week={w + 1} index={s} previous={(s > 0 ? expected.Stages[s - 1] : "none")} expected={a} observed={b}"); break; }
					}
					if (expected.Date != observed.Date) throw new InvalidOperationException("Expected observation date differs.");
					if (expected.Action != observed.Action) throw new InvalidOperationException("Player action result differs in week " + (w + 1));
					try {
						CompareFreshJson(expected.Evidence, observed.Evidence, "evidence", w + 1);
						CompareFreshJson(expected.Shadow, observed.Shadow, "shadow", w + 1);
						if (playerSave) CompareFreshJson(expected.Player, observed.Player, "player", w + 1);
					} catch {
						WriteCheckpointGzip(Path.Combine(directory, $"mismatch-week-{w + 1:D3}.json.gz"), observed);
						throw;
					}
				}
				GD.Print($"STATURE_FRESH_WEEK_PASS mode={(write != null ? "record" : "resume")} week={w + 1} date={observed.Date:yyyy-MM-dd}");
			}
		} finally { StatureObservation.ReplayEvidenceObserver = null; StatureReplayDiagnostics.Observer = null; }
		if (write != null) { WriteCheckpointGzip(receiptPath, receipt); GD.Print($"STATURE_FRESH_REFERENCE_PASS weeks={replayWeeks} pid={Environment.ProcessId}"); }
		else GD.Print($"STATURE_FRESH_REPLAY_PASS weeks={replayWeeks} producerPid={receipt.ProducerProcessId} consumerPid={Environment.ProcessId}");
		GetTree().Quit(0);
		return true;
	}
	private static void CompareFreshJson(string expected, string observed, string kind, int week) {
		if (expected == observed) return;
		using var left = JsonDocument.Parse(expected);
		using var right = JsonDocument.Parse(observed);
		throw new InvalidOperationException($"Fresh-process {kind} differs in week {week}: " +
			(FirstJsonDifference(left.RootElement, right.RootElement, kind) ?? "serialization order differs"));
	}
	private static void ReportFreshWorldDifferences(string expectedPath, WorldSaveData observed, int week, string envelopeProperty = null) {
		using var file = File.OpenRead(expectedPath);
		using var gzip = new GZipStream(file, CompressionMode.Decompress);
		using var left = JsonDocument.Parse(gzip);
		using var right = JsonSerializer.SerializeToDocument(observed, SaveGameService.TestJsonOptions);
		var expected = envelopeProperty == null ? left.RootElement : left.RootElement.GetProperty(envelopeProperty);
		foreach (var part in expected.EnumerateObject()) {
			if (part.Name == "ShadowContinuation" && right.RootElement.TryGetProperty(part.Name, out var continuation)) {
				foreach (var item in part.Value.EnumerateObject()) {
					string detail = FirstJsonDifference(item.Value, continuation.GetProperty(item.Name), "world.ShadowContinuation." + item.Name);
					if (detail != null) GD.Print($"STATURE_FRESH_WORLD_DIFF week={week} " + detail);
				}
				continue;
			}
			string delta = right.RootElement.TryGetProperty(part.Name, out var other)
				? FirstJsonDifference(part.Value, other, "world." + part.Name) : "missing " + part.Name;
			if (delta != null) GD.Print($"STATURE_FRESH_WORLD_DIFF week={week} " + delta);
		}
	}
}
