using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// Rolling autosave at dawn. A full-world save is not free -- capturing the state takes ~50 ms but serialising and
/// compressing it takes seconds (about 2.5 s on a January-1960 world, and the world only grows) -- so an autosave
/// on every morning would stall a player who is skipping through days. Two guards keep it off the hot path:
/// a wall-clock floor between saves, and a floor proportional to how long the last one took, so saving can never
/// cost more than a couple of percent of the player's real time. The write itself is deferred to the end of the
/// frame, after every day-start handler has run, so a multi-day skip saves once, on the morning it lands.
/// </summary>
public partial class PlayerDesk {
	/// <summary>Never autosave more often than this many real seconds.</summary>
	private const double AutosaveMinIntervalSeconds = 300.0;
	/// <summary>The share of real time autosaving may take: the next one waits at least lastDuration / this.</summary>
	private const double AutosaveTimeShare = 0.02;

	private ulong lastAutosaveTicksMs;
	private double lastAutosaveSeconds;
	private bool autosaveQueued;

	/// <summary>How long a given save has to wait: the wall-clock floor, or 50x the last save's duration if longer.</summary>
	internal static double AutosaveWaitSeconds(double lastDurationSeconds) =>
		Math.Max(AutosaveMinIntervalSeconds, lastDurationSeconds / AutosaveTimeShare);

	private void QueueDawnAutosave() {
		// Real play only: the headless audits and probes have no UI, and must not litter the player's saves.
		if (UIManager.Instance == null || Label == null || IsGameOver || autosaveQueued) return;
		double sinceLast = lastAutosaveTicksMs == 0 ? double.MaxValue : (Time.GetTicksMsec() - lastAutosaveTicksMs) / 1000.0;
		if (sinceLast < AutosaveWaitSeconds(lastAutosaveSeconds)) return;
		autosaveQueued = true;
		CallDeferred(nameof(RunDawnAutosave));
	}

	private void RunDawnAutosave() {
		autosaveQueued = false;
		if (Label == null || IsGameOver) return;
		var clock = Stopwatch.StartNew();
		bool ok = SaveGameService.Autosave(out string slot, out string message);
		lastAutosaveSeconds = clock.Elapsed.TotalSeconds;
		lastAutosaveTicksMs = Time.GetTicksMsec();
		GD.Print($"[Autosave] {(ok ? "wrote" : "FAILED")} {slot} in {lastAutosaveSeconds:F1}s -- next one no sooner than {AutosaveWaitSeconds(lastAutosaveSeconds):F0}s from now. {message}");
	}
}
