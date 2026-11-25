using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Small office sounds made in code rather than shipped (the same trick as the New Game walk-in): knuckles on a wooden
/// door, a door that needs oil, a sheet of paper. Played fire-and-forget on a player that frees itself. Silent when headless.
/// </summary>
public static class OfficeFoley {
	private const float Rate = 22050;
	private static readonly Dictionary<string, AudioStreamWav> Cache = new();

	public static void Play(Node host, string sound, float volumeDb = -4f) {
		if (host == null || !GodotObject.IsInstanceValid(host) || DisplayServer.GetName() == "headless") return;
		var player = new AudioStreamPlayer { Stream = Get(sound), VolumeDb = volumeDb };
		host.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	private static AudioStreamWav Get(string key) {
		if (!Cache.TryGetValue(key, out var stream)) Cache[key] = stream = Build(key);
		return stream;
	}

	private static AudioStreamWav Build(string key) {
		var noise = new Random(1960 + key.Length);
		float N() => (float)(noise.NextDouble() * 2 - 1);
		float low = 0;
		float Rumble(float cut) => low += (N() - low) * cut;
		static float Bump(float a, float b, float c, float t) => t < a || t > c ? 0f : t < b ? (t - a) / (b - a) : (c - t) / (c - b);

		float seconds;
		Func<float, float> wave;
		switch (key) {
			case "knock":
				// Knuckles on wood: a low body, a brighter knock over it, and a burst of rap.
				seconds = .3f;
				wave = t => 1.0f * Mathf.Sin(Mathf.Tau * 150 * t) * Mathf.Exp(-t * 36) + .55f * Mathf.Sin(Mathf.Tau * 330 * t) * Mathf.Exp(-t * 60) + .45f * N() * Mathf.Exp(-t * 320);
				break;
			case "door":
				// A hinge that needs oil, then the latch.
				seconds = 1.1f;
				wave = t => .22f * Mathf.Sin(Mathf.Tau * (230 + 110 * t) * t) * (.6f + .4f * Mathf.Sin(t * 60)) * Bump(.04f, .25f, .95f, t)
					+ Rumble(.06f) * .7f * Bump(0, .15f, .9f, t)
					+ .5f * N() * Mathf.Exp(-(t - .01f) * 700) * (t > .01f ? 1f : 0f);
				break;
			default: // "paper"
				seconds = .45f;
				wave = t => N() * (.22f + .5f * Mathf.Max(0, Mathf.Sin(t * 47 + Mathf.Sin(t * 13) * 3))) * Bump(0, .05f, .42f, t);
				break;
		}
		int count = (int)(seconds * Rate);
		var data = new byte[count * 2];
		for (int i = 0; i < count; i++) {
			short v = (short)(Mathf.Clamp(wave(i / Rate), -1f, 1f) * 30000);
			data[2 * i] = (byte)(v & 0xff);
			data[2 * i + 1] = (byte)((v >> 8) & 0xff);
		}
		return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = (int)Rate, Stereo = false, Data = data };
	}
}
