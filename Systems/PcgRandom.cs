using System;
using System.Diagnostics;
using System.Numerics;

/// <summary>
/// Managed copy of Godot's RandomPCG (core/math/random_pcg.h, pcg.cpp): the same PCG32 stream, the
/// same float construction and the same range arithmetic, so every draw is bit-identical to
/// RandomNumberGenerator for the same Seed/State. Godot's generator is a native object, so each draw
/// was an interop call; the radio playlist alone spent ~9% of a 1962 run crossing into it.
/// GodotRngParityChecks proves the streams match draw for draw.
/// </summary>
public sealed class PcgRandom : IDisposable {
	public const ulong DefaultSeed = 12047754176567800795UL;
	public const ulong DefaultInc = 1442695040888963407UL;

	private ulong state;
	private ulong inc;
	private ulong seed;

	/// <summary>Like RandomNumberGenerator(): a fresh generator starts randomized.</summary>
	public PcgRandom() {
		inc = (DefaultInc << 1) | 1UL;
		Reseed(DefaultSeed);
		Randomize();
	}

	private PcgRandom(ulong seed, ulong increment) {
		inc = (increment << 1) | 1UL;
		Reseed(seed);
	}

	/// <summary>Godot's global default_rand before any GD.Seed: default seed, default increment.</summary>
	internal static PcgRandom CreateGodotDefault() => new(DefaultSeed, DefaultInc);

	public ulong Seed {
		get => seed;
		set => Reseed(value);
	}

	public ulong State {
		get => state;
		set => state = value;
	}

	private void Reseed(ulong value) {
		seed = value;
		// pcg32_srandom_r with Godot's fixed increment (initseq): zero, step, add seed, step.
		state = 0UL;
		Next();
		state += value;
		Next();
	}

	public void Randomize() =>
		Reseed(((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (ulong)(Stopwatch.GetTimestamp() / 10)) * state + DefaultInc);

	private uint Next() {
		ulong old = state;
		state = unchecked(old * 6364136223846793005UL + inc);
		uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
		int rot = (int)(old >> 59);
		return BitOperations.RotateRight(xorshifted, rot);
	}

	private uint Bounded(uint bound) {
		uint threshold = unchecked((0u - bound) % bound);
		while (true) {
			uint r = Next();
			if (r >= threshold) return r % bound;
		}
	}

	public uint Randi() => Next();

	/// <summary>RandomPCG::randf: the draw is the fraction of an infinite binary number.</summary>
	public float Randf() {
		uint protoExpOffset = Next();
		if (protoExpOffset == 0) return 0f;
		return MathF.ScaleB((float)(Next() | 0x80000001u), -32 - BitOperations.LeadingZeroCount(protoExpOffset));
	}

	/// <summary>RandomPCG::randd.</summary>
	public double Randd() {
		uint protoExpOffset = Next();
		if (protoExpOffset == 0) return 0d;
		ulong significand = ((ulong)Next() << 32) | Next() | 0x8000000000000001UL;
		// C++ converts the 64-bit significand with one correct rounding; .NET's (double)ulong rounds
		// twice above 2^63. The top bit is always set, so round to 53 bits by hand (the low bit is set,
		// so there is never a tie) and convert a value that fits exactly.
		long mantissa = (long)(significand >> 11) + ((significand & 0x7FFUL) > 0x400UL ? 1L : 0L);
		return Math.ScaleB((double)mantissa, 11 - 64 - BitOperations.LeadingZeroCount(protoExpOffset));
	}

	public float RandfRange(float from, float to) => Randf() * (to - from) + from;

	public double RanddRange(double from, double to) => Randd() * (to - from) + from;

	public int RandiRange(int from, int to) {
		if (from == to) return from;
		return unchecked((int)Bounded((uint)Math.Abs(from - to) + 1u) + Math.Min(from, to));
	}

	public void Dispose() { }
}

/// <summary>
/// Drop-in for GD.Seed/Randf/Randi/RandRange/RandiRange over Godot's global generator, on a managed
/// PcgRandom with Godot's defaults. Every simulation draw from the global stream goes through here;
/// Seed also seeds Godot's own global generator so any stray GD.* draw stays deterministic.
/// </summary>
public static class GameRandom {
	private static readonly PcgRandom Global = PcgRandom.CreateGodotDefault();
	/// <summary>Probe-only: lets an observe-only check prove it drew nothing from the global stream.</summary>
	internal static ulong StateForChecks => Global.State;
	/// <summary>Audit checkpoints: the global stream's position, so a continued run draws what the unbroken one would.</summary>
	internal static ulong State { get => Global.State; set => Global.State = value; }

	public static void Seed(ulong seed) {
		Global.Seed = seed;
		Godot.GD.Seed(seed);
	}

	public static void Randomize() {
		Global.Randomize();
		Godot.GD.Randomize();
	}

	/// <summary>GD.Randf is Math::randf, not RandomPCG::randf: one draw over (float)UINT32_MAX.</summary>
	public static float Randf() => (float)Global.Randi() / (float)uint.MaxValue;

	public static uint Randi() => Global.Randi();

	/// <summary>GD.RandRange(double, double): Math::random(double, double), a randd draw.</summary>
	public static double RandRange(double from, double to) => Global.RanddRange(from, to);

	/// <summary>GD.RandRange(int, int): Math::random(int, int).</summary>
	public static int RandRange(int from, int to) => Global.RandiRange(from, to);
}
