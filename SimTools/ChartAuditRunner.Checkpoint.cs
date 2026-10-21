using System;
using System.IO;
using System.Text.Json;
using Godot;

// Restartable long runs. A decade run on a memory-tight machine can be stopped and picked up later
// without losing the years already simulated:
//
//   --checkpoint-world=<name>   at the end of every simulated year, atomically write SimLogs/worlds/<name>.world.json.gz
//                               (with the audit week and the global random state), replacing the previous one.
//                               While running, the file SimLogs/worlds/<name>.stop asks the run to stop: it finishes
//                               the week, writes the completed years' annual rows, flushes every CSV and exits 3.
//   --continue-checkpoint       with --resume-world=<name>: an exact continuation instead of a fresh window. The
//                               global stream is restored rather than reseeded, the loop resumes at the saved audit
//                               week, and --weeks is the run's total length, so CSV week numbers continue.
//
// Write the continuation under its own --run name, then join the two with SimTools/stitch_resumed_run.py, which keeps
// the first segment's rows before the checkpoint and the continuation's from it on. The launcher
// (SimLogs/<dir>/run-resumable.ps1) creates the stop file when free memory runs low and restarts later.
public partial class ChartAuditRunner {
	private string checkpointWorldName;
	private bool continueCheckpoint;
	private int firstAuditWeek = 1;

	private sealed class AuditMemoryStop : Exception {
		public AuditMemoryStop() : base("stop requested") { }
	}

	private void ParseCheckpointArguments() {
		foreach (string argument in OS.GetCmdlineUserArgs()) {
			if (argument.StartsWith("--checkpoint-world=", StringComparison.Ordinal)) checkpointWorldName = argument["--checkpoint-world=".Length..];
			else if (argument == "--continue-checkpoint") continueCheckpoint = true;
		}
		if (continueCheckpoint && resumeWorldName == null) throw new ArgumentException("--continue-checkpoint needs --resume-world.");
		// A stale request from an earlier stop must not end this run before it starts.
		if (checkpointWorldName != null && File.Exists(StopPath)) File.Delete(StopPath);
	}

	private string StopPath => Path.Combine(Path.GetDirectoryName(WorldPath(checkpointWorldName)), SanitizeFileName(checkpointWorldName) + ".stop");

	/// <summary>Week top: honour a stop request between weeks, never inside one.</summary>
	private void CheckCheckpointStop() {
		if (checkpointWorldName != null && File.Exists(StopPath)) throw new AuditMemoryStop();
	}

	/// <summary>After a completed week: the last week of a year writes the checkpoint, so the continuation starts on 1 January's week.</summary>
	private void MaybeWriteCheckpoint(int auditWeek) {
		if (checkpointWorldName == null || auditWeek >= requestedWeeks) return;
		GameDate now = TimeManager.Instance.CurrentDate;
		if (now.AddDays(7).year == now.year) return;
		FlushAnnualStreams();
		FlushAllRunnerStreams();
		SaveAuditWorld(checkpointWorldName, auditWeek, atomic: true);
		GD.Print($"CHART_AUDIT_CHECKPOINT name={checkpointWorldName} auditWeek={auditWeek} date={now.ToShortString()} rng={GameRandom.State}");
	}

	/// <summary>
	/// Every CSV stream this runner holds reaches disk at a checkpoint, so a run killed outright still has whole rows
	/// up to it. (Streams owned by static telemetry helpers flush on their own schedule; a graceful stop flushes all.)
	/// </summary>
	private void FlushAllRunnerStreams() {
		foreach (var field in typeof(ChartAuditRunner).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic))
			if (typeof(StreamWriter).IsAssignableFrom(field.FieldType)) (field.GetValue(this) as StreamWriter)?.Flush();
	}

	/// <summary>Called after the world is applied: restore the exact stream and the loop position.</summary>
	private void ContinueCheckpointIfRequested(int? auditWeek, ulong? globalRandomState) {
		if (!continueCheckpoint) return;
		if (auditWeek == null || globalRandomState == null)
			throw new InvalidDataException($"Saved world '{resumeWorldName}' is not a checkpoint; --continue-checkpoint needs one written by --checkpoint-world.");
		GameRandom.State = globalRandomState.Value;
		firstAuditWeek = auditWeek.Value + 1;
		GD.Print($"CHART_AUDIT_CONTINUING name={resumeWorldName} fromAuditWeek={firstAuditWeek} totalWeeks={requestedWeeks}");
	}

	/// <summary>A requested stop: the completed years' annual rows and every stream reach disk; nothing is snapshotted.</summary>
	private void StopForCheckpoint() {
		WriteConcentrationYear();
		WriteGenreShapeYear();
		WriteYearEndHot100Year();
		WriteMarketRevenueYear();
		WriteAnnualFormatMixRows();
		WriteDecadeAnnualYear();
		FlushAndClose();
		if (File.Exists(StopPath)) File.Delete(StopPath);
		GD.Print($"CHART_AUDIT_STOPPED_FOR_CHECKPOINT run={runName} completedWeeks={currentAuditWeek} checkpoint={checkpointWorldName}");
	}
}
