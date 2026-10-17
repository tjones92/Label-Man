using System;
using System.Collections.Generic;
using Godot;

/// <summary>--artist-momentum-half-life-check: identity when off, the named half-life when on, one step per chart week.</summary>
public static class ArtistMomentumHalfLifeChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Artist momentum half-life: " + why); }
		float was = ArtistMomentumHalfLife.HalfLifeWeeks;
		try {
			ArtistMomentumHalfLife.SetHalfLifeForChecks(0f);
			Check(!ArtistMomentumHalfLife.Enabled, "no half-life must read as off");
			foreach (float m in new[] { 0f, 0.1f, 0.5f, 1f })
				Check(ArtistMomentumHalfLife.Decay(m) == m, "off must return momentum bit-for-bit");
			var off = new SimulatedArtist { momentum = 0.4f, reputation = 0.6f };
			ArtistMomentumHalfLife.DecayRegistryForWeek(5, new List<SimulatedArtist> { off });
			Check(off.momentum == 0.4f, "an off pass must not touch momentum");

			ArtistMomentumHalfLife.SetHalfLifeForChecks(104f);
			Check(ArtistMomentumHalfLife.Enabled, "a half-life must read as on");
			var act = new SimulatedArtist { momentum = 0.8f, reputation = 0.6f };
			var acts = new List<SimulatedArtist> { act, null };
			for (int week = 1; week <= 104; week++) {
				ArtistMomentumHalfLife.DecayRegistryForWeek(week, acts);
				ArtistMomentumHalfLife.DecayRegistryForWeek(week, acts); // same week again: no-op
			}
			Check(Math.Abs(act.momentum - 0.4f) < 1e-3f, $"104 weekly steps must halve momentum (got {act.momentum})");
			Check(act.reputation == 0.6f, "reputation is the whole record and must not decay");
			Check(ArtistMomentumHalfLife.Decay(0f) == 0f, "zero momentum stays zero");
			GD.Print("ARTIST_MOMENTUM_HALF_LIFE_CHECK passed");
		} finally {
			ArtistMomentumHalfLife.SetHalfLifeForChecks(was);
		}
	}
}
