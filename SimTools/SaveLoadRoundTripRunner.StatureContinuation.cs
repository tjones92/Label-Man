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
		public WorldSaveData World { get; set; }
	}
	public sealed class StatureExpectedWeek {
		public DateTime Date { get; set; }
		public string Evidence { get; set; }
		public string Shadow { get; set; }
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

	private bool RunStatureFreshContinuation(int setupWeeks, int replayWeeks, string calibration, bool traceWorld) {
		string[] args = OS.GetCmdlineUserArgs();
		string Value(string prefix) => args.SingleOrDefault(a => a.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
		string write = Value("--stature-checkpoint-write="), read = Value("--stature-checkpoint-read=");
		if (write == null && read == null) return false;
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
		string checkpointPath = Path.Combine(directory, "checkpoint.json.gz");
		string receiptPath = Path.Combine(directory, "ready.json.gz");
		if (write != null) {
			if (Directory.Exists(directory)) throw new InvalidOperationException("Checkpoint directory already exists.");
			Directory.CreateDirectory(directory);
			for (int w = 0; w < setupWeeks; w++) AdvanceOneChartWeek();
			var world = WorldStateService.Capture();
			if (world.Media?.ExperimentalStature?.Normalizer.LastProcessedDate == default || world.ShadowContinuation == null)
				throw new InvalidOperationException("Checkpoint is missing its dated shadow or continuation state.");
			checkpoint = new() { ProducerProcessId = Environment.ProcessId, SetupWeeks = setupWeeks, ReplayWeeks = replayWeeks,
				Seed = SimulationSeedBootstrap.RequestedSeed, Date = TimeManager.Instance.CurrentDate, Flags = flags,
				AssemblyHash = assemblyHash, EngineHash = engineHash, CalibrationHash = calibrationHash,
				World = world, WorldHash = CheckpointJsonHash(world) };
			WriteCheckpointGzip(checkpointPath, checkpoint);
			receipt = new() { ProducerProcessId = Environment.ProcessId, Weeks = replayWeeks };
			receipt.Files.Add("checkpoint.json.gz", CheckpointFileHash(checkpointPath));
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
			WorldStateService.Apply(checkpoint.World, checkpoint.Date, checkpoint.Seed);
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
				StatureObservation.ReplayEvidenceObserver = (date, evidence, _) => {
					observations++; observed.Date = new DateTime(date.year, date.month, date.day); observed.Evidence = JsonSerializer.Serialize(evidence, SaveGameService.TestJsonOptions);
				};
				if (TimeManager.Instance.IsGameOver) throw new InvalidOperationException("Game ended during fresh continuation.");
				AdvanceOneChartWeek();
				StatureObservation.ReplayEvidenceObserver = null;
				if (observations != 1 || observed.Date != start.AddDays(7 * (w + 1))) throw new InvalidOperationException("Fresh continuation observation clock differs.");
				observed.Shadow = JsonSerializer.Serialize(MediaAtmosphereService.ExperimentalStatureShadow(), SaveGameService.TestJsonOptions);
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
					if (expected.Date != observed.Date) throw new InvalidOperationException("Expected observation date differs.");
					try {
						CompareFreshJson(expected.Evidence, observed.Evidence, "evidence", w + 1);
						CompareFreshJson(expected.Shadow, observed.Shadow, "shadow", w + 1);
					} catch {
						WriteCheckpointGzip(Path.Combine(directory, $"mismatch-week-{w + 1:D3}.json.gz"), observed);
						throw;
					}
				}
				GD.Print($"STATURE_FRESH_WEEK_PASS mode={(write != null ? "record" : "resume")} week={w + 1} date={observed.Date:yyyy-MM-dd}");
			}
		} finally { StatureObservation.ReplayEvidenceObserver = null; }
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
