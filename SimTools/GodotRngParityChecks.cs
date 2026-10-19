using System;
using Godot;

/// <summary>
/// Proves PcgRandom/GameRandom reproduce Godot's native generators bit for bit: per-instance
/// RandomNumberGenerator (seed, state, every draw) and the global GD.* stream.
/// </summary>
public partial class GodotRngParityChecks : Node {
	private int checks;

	public override void _Ready() {
		try {
			foreach (ulong seed in new ulong[] { 0, 1, 2, 42, 1001, 2002, 0x617274697374706fUL, ulong.MaxValue, 12047754176567800795UL })
				CheckInstance(seed);
			foreach (ulong seed in new ulong[] { 0, 7, 1001, 2002, 9988, ulong.MaxValue })
				CheckGlobal(seed);
			GD.Print($"GODOT_RNG_PARITY_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception exception) {
			GD.PushError($"GODOT_RNG_PARITY_FAILED: {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void Check(bool condition, string name) {
		if (!condition) throw new InvalidOperationException(name);
		checks++;
	}

	private static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
	private static bool Same(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

	private static readonly (float From, float To)[] FloatRanges = {
		(0f, 1f), (.96f, 1.04f), (-.5f, .5f), (10f, -3f), (-1000f, 2500.5f), (.15f, .15f), (0f, 1e-7f), (-1e6f, 1e6f)
	};
	private static readonly (double From, double To)[] DoubleRanges = {
		(0d, 1d), (.96d, 1.04d), (-.5d, .5d), (10d, -3d), (-1000d, 2500.5d), (.85d, 1.15d), (0d, 1e-9d)
	};
	private static readonly (int From, int To)[] IntRanges = {
		(0, 1), (1000, 10000), (12, 18), (5, 5), (9, -4), (-30, -2), (0, int.MaxValue), (int.MinValue / 2, int.MaxValue / 2), (0, 2), (0, 25)
	};

	private void CheckInstance(ulong seed) {
		using var native = new RandomNumberGenerator { Seed = seed };
		var managed = new PcgRandom { Seed = seed };
		Check(native.Seed == managed.Seed && native.State == managed.State, $"seed {seed}: seeded state");
		for (int i = 0; i < 200_000; i++) {
			int kind = i % 7;
			switch (kind) {
				case 0: Check(native.Randi() == managed.Randi(), $"seed {seed} draw {i}: Randi"); break;
				case 1:
				case 2: Check(Same(native.Randf(), managed.Randf()), $"seed {seed} draw {i}: Randf"); break;
				case 3:
				case 4: {
					var (from, to) = FloatRanges[(i / 7) % FloatRanges.Length];
					Check(Same(native.RandfRange(from, to), managed.RandfRange(from, to)), $"seed {seed} draw {i}: RandfRange({from},{to})");
					break;
				}
				default: {
					var (from, to) = IntRanges[(i / 7) % IntRanges.Length];
					Check(native.RandiRange(from, to) == managed.RandiRange(from, to), $"seed {seed} draw {i}: RandiRange({from},{to})");
					break;
				}
			}
			Check(native.State == managed.State, $"seed {seed} draw {i}: state after draw");
			if (i % 40_000 == 39_999) {
				ulong jump = native.State * 2862933555777941757UL + (ulong)i;
				native.State = jump;
				managed.State = jump;
				Check(native.State == managed.State && native.Seed == managed.Seed, $"seed {seed} draw {i}: state write");
			}
		}
	}

	private void CheckGlobal(ulong seed) {
		GameRandom.Seed(seed);   // seeds the managed global and Godot's global
		for (int i = 0; i < 200_000; i++) {
			switch (i % 6) {
				case 0: Check(GD.Randi() == GameRandom.Randi(), $"global {seed} draw {i}: Randi"); break;
				case 1:
				case 2: Check(Same(GD.Randf(), GameRandom.Randf()), $"global {seed} draw {i}: Randf"); break;
				case 3:
				case 4: {
					var (from, to) = DoubleRanges[(i / 6) % DoubleRanges.Length];
					Check(Same(GD.RandRange(from, to), GameRandom.RandRange(from, to)), $"global {seed} draw {i}: RandRange({from},{to})");
					break;
				}
				default: {
					var (from, to) = IntRanges[(i / 6) % IntRanges.Length];
					Check(GD.RandRange(from, to) == GameRandom.RandRange(from, to), $"global {seed} draw {i}: RandRange int({from},{to})");
					break;
				}
			}
		}
		// A float argument binds GD.RandRange(double, double), as call sites with float literals do.
		GameRandom.Seed(seed);
		float a = (float)GD.RandRange(.96f, 1.04f), b = (float)GameRandom.RandRange(.96f, 1.04f);
		Check(Same(a, b), $"global {seed}: float-argument RandRange");
	}
}
