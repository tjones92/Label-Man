using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Godot;

// Late-decade windows (SimTools/BandMemberSimulationDirective.md §6 "Test tiers"). A run can write the AI world at
// the start of a chosen year and keep going, and a later run can start from that world instead of 1960:
//
//   --save-world-at-year=1965 --save-world=<name>   capture after the first chart week dated in 1965
//   --save-world-at-year=1966,1967,1968,1969        one world per year, named <name>-<year> (default <run>-<year>)
//   --resume-world=<name>                            load it before week one; --weeks counts from there
//
// The world goes through the same WorldStateService the game's save uses, so it carries every captured stream
// state. Two constraints, both by construction of the game's load:
//   * The global stream is RESEEDED on load (WorldStateService.Apply), not restored. A resumed run is not a
//     continuation of the run that wrote the world; its control is another resume of the same world.
//   * Feature flags are parsed by the autoloads, not stored in the world. A resume must pass the same flags; the
//     ones that wrote the world are recorded beside it and every difference is printed as RESUME_FLAG_MISMATCH.
public partial class ChartAuditRunner {
	private readonly SortedSet<int> saveWorldYears = new();
	private string saveWorldName;
	private string resumeWorldName;
	private bool keepExactSaveName;

	/// <summary>The world file. Lives under SimLogs (gitignored): a run artifact, not source.</summary>
	private sealed class AuditWorldFile {
		public int Version { get; set; }
		public ulong? WorldSeed { get; set; }
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public string[] Flags { get; set; } = Array.Empty<string>();
		public WorldSaveData World { get; set; }
	}

	private static string WorldPath(string name) =>
		Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), "worlds", SanitizeFileName(name) + ".world.json.gz");

	private void ParseResumeArguments() {
		foreach (string argument in OS.GetCmdlineUserArgs()) {
			if (argument.StartsWith("--save-world-at-year=", StringComparison.Ordinal))
				foreach (string year in argument["--save-world-at-year=".Length..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					saveWorldYears.Add(int.Parse(year, System.Globalization.CultureInfo.InvariantCulture));
			else if (argument.StartsWith("--save-world=", StringComparison.Ordinal))
				saveWorldName = argument["--save-world=".Length..];
			else if (argument.StartsWith("--resume-world=", StringComparison.Ordinal))
				resumeWorldName = argument["--resume-world=".Length..];
		}
		keepExactSaveName = saveWorldYears.Count == 1 && !string.IsNullOrWhiteSpace(saveWorldName);
		if (saveWorldYears.Count > 0 && string.IsNullOrWhiteSpace(saveWorldName)) saveWorldName = runName;
		if (resumeWorldName != null && (catastrophicFailFast || catastrophicControlPreflight))
			throw new ArgumentException("--resume-world can't be combined with the catastrophic gates: their controls are 1960-start runs.");
	}

	/// <summary>The flags that shape the simulation, i.e. everything except where output goes and how long it runs.</summary>
	private static string[] SimulationFlags() => OS.GetCmdlineUserArgs()
		.Where(a => !a.StartsWith("--run=", StringComparison.Ordinal) && !a.StartsWith("--weeks=", StringComparison.Ordinal) &&
			!a.StartsWith("--save-world", StringComparison.Ordinal) && !a.StartsWith("--resume-world=", StringComparison.Ordinal) &&
			a != "--calibration" && a != "--lean-probe" && a != "--aggregate-only" && a != "--profile-performance")
		.OrderBy(a => a, StringComparer.Ordinal).ToArray();

	/// <summary>
	/// Called after each completed week. Writes the world once per target year, the first time the clock is in it.
	/// A single year with an explicit --save-world keeps that exact name; otherwise each file is suffixed -year.
	/// </summary>
	private void MaybeSaveWorld() {
		if (saveWorldYears.Count == 0 || TimeManager.Instance.CurrentDate.year < saveWorldYears.Min) return;
		int target = saveWorldYears.Max(y => y <= TimeManager.Instance.CurrentDate.year ? y : int.MinValue);
		saveWorldYears.RemoveWhere(y => y <= target);
		string name = keepExactSaveName ? saveWorldName : $"{saveWorldName}-{target}";
		GameDate now = TimeManager.Instance.CurrentDate;
		var file = new AuditWorldFile {
			Version = SaveGameService.CurrentVersion,
			WorldSeed = SimulationSeedBootstrap.RequestedSeed,
			Year = now.year, Month = now.month, Day = now.day,
			Flags = SimulationFlags(),
			World = WorldStateService.Capture()
		};
		string path = WorldPath(name);
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		using (FileStream stream = File.Create(path))
		using (var gzip = new GZipStream(stream, CompressionLevel.Optimal))
			JsonSerializer.Serialize(gzip, file, SaveGameService.TestJsonOptions);
		GD.Print($"CHART_AUDIT_WORLD_SAVED name={name} date={now.ToShortString()} chartWeek={file.World.ChartWeek} path={path}");
	}

	/// <summary>Loads a saved world over the freshly generated one, before any telemetry baseline is taken.</summary>
	private void ResumeWorldIfRequested() {
		if (resumeWorldName == null) return;
		string path = WorldPath(resumeWorldName);
		if (!File.Exists(path)) throw new FileNotFoundException($"No saved world '{resumeWorldName}'.", path);
		AuditWorldFile file;
		using (FileStream stream = File.OpenRead(path))
		using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
			file = JsonSerializer.Deserialize<AuditWorldFile>(gzip, SaveGameService.TestJsonOptions);
		if (file?.World == null) throw new InvalidDataException($"Saved world '{resumeWorldName}' is unreadable.");
		if (file.Version > SaveGameService.CurrentVersion) throw new InvalidDataException($"Saved world '{resumeWorldName}' is from a newer save version.");

		// Every keyed draw (band life, repertoire, evolution) hashes the requested seed, so a different --seed would
		// run a different world's dice over this world's people.
		if (file.WorldSeed != SimulationSeedBootstrap.RequestedSeed)
			throw new ArgumentException($"Saved world '{resumeWorldName}' was written with seed {file.WorldSeed}; pass --seed={file.WorldSeed}.");
		string[] now = SimulationFlags();
		foreach (string missing in file.Flags.Except(now, StringComparer.Ordinal)) GD.Print($"RESUME_FLAG_MISMATCH saved-with={missing}");
		foreach (string added in now.Except(file.Flags, StringComparer.Ordinal)) GD.Print($"RESUME_FLAG_MISMATCH resumed-with={added}");

		var date = new GameDate(file.Year, file.Month, file.Day);
		WorldStateService.Apply(file.World, date, file.WorldSeed);
		RescaleMemberFameForResume(file.Flags);
		GD.Print($"CHART_AUDIT_WORLD_RESUMED name={resumeWorldName} date={date.ToShortString()} chartWeek={file.World.ChartWeek}");
	}

	/// <summary>
	/// A world saved under one --member-fame-share and resumed under another (Phase 4d) holds member fame banked at
	/// the old slice. Every deposit is proportional to the slice and decay is multiplicative, so while the stocks are
	/// far from saturation (the best member sits near .13) a 1960-start run under the new slice would hold the old
	/// stock times new/old. Rescale once, so the window measures the treatment rather than a ramp toward it.
	/// </summary>
	private static void RescaleMemberFameForResume(string[] savedFlags) {
		float saved = MusicianRecognitionService.ParseFameShare(savedFlags) ?? MusicianRecognitionService.DefaultMemberFameShare;
		float now = MusicianRecognitionService.MemberFameShare;
		if (Mathf.IsEqualApprox(saved, now) || saved <= 0f) return;
		float ratio = now / saved;
		var seen = new HashSet<Musician>();
		int rescaled = 0;
		void Rescale(Musician m) {
			if (m == null || !seen.Add(m) || m.personalRecognition <= 0f) return;
			m.personalRecognition = Mathf.Clamp(m.personalRecognition * ratio, 0f, 1f);
			rescaled++;
		}
		foreach (SimulatedArtist artist in ArtistManager.Instance.GetAllArtists())
			if (artist?.members != null) foreach (Musician m in artist.members) Rescale(m);
		foreach (PooledPerson pooled in PersonPool.All) Rescale(pooled.person);
		GD.Print($"CHART_AUDIT_MEMBER_FAME_RESCALED saved={saved:0.###} now={now:0.###} people={rescaled}");
	}
}
