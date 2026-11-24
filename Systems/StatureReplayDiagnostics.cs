using System;

/// <summary>Explicit replay-test tap. A null observer leaves normal simulation untouched.</summary>
internal static class StatureReplayDiagnostics {
	internal static Action<string> Observer;
	internal static void Mark(string stage) => Observer?.Invoke($"{TimeManager.Instance?.CurrentDate.year:D4}-{TimeManager.Instance?.CurrentDate.month:D2}-{TimeManager.Instance?.CurrentDate.day:D2}|{stage}|rng={GameRandom.State}");
}
