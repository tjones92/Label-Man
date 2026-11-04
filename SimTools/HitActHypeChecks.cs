using System;
using Godot;

/// <summary>--hit-act-hype-check: off reads 0 and writes no clock; on, a Top 10 is worth the full lift at once and half
/// after one half-life, a Top 40 half of that, an act with no hit nothing, and opening careers seed from 1959.</summary>
public static class HitActHypeChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Hit-act hype: " + message); checks++; }
		bool Near(float a, float b) => Math.Abs(a - b) < 1e-5f;
		try {
			HitActHype.SetForChecks(-1f);
			var off = new SimulatedArtist();
			HitActHype.OnTop10(off); HitActHype.OnTop40(off);
			Check(!HitActHype.Enabled && HitActHype.AwarenessLift(off, 10) == 0f && HitActHype.RadioLift(off) == 0f, "off reads 0");
			Check(off.hypeLastTop10Week == HitActHype.Unset && off.hypeLastTop40Week == HitActHype.Unset, "off writes no clock");

			HitActHype.SetForChecks(.2f, .05f);
			var hit = new SimulatedArtist { hypeLastTop10Week = 100, hypeLastTop40Week = 100 };
			Check(Near(HitActHype.Hype(hit, 100), 1f) && Near(HitActHype.AwarenessLift(hit, 100), .2f), "a Top 10 this week is the full lift");
			Check(Near(HitActHype.Hype(hit, 126), .5f) && Near(HitActHype.Hype(hit, 152), .25f), "halves each 26 weeks");
			var top40 = new SimulatedArtist { hypeLastTop10Week = HitActHype.Unset + 1, hypeLastTop40Week = 100 };
			Check(Near(HitActHype.Hype(top40, 100), HitActHype.Top40Share), "a Top 40 alone is worth the Top 40 share");
			var both = new SimulatedArtist { hypeLastTop10Week = 48, hypeLastTop40Week = 100 };
			Check(Near(HitActHype.Hype(both, 100), .5f), "the stronger of the two clocks wins");
			var unknown = new SimulatedArtist();
			Check(HitActHype.Hype(unknown, 50) == 0f && HitActHype.Hype(unknown, 50) == 0f, "an act with no hit has no hype, read twice");
			var vet = new SimulatedArtist();
			vet.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 8 });
			Check(Near(HitActHype.Hype(vet, 0), Mathf.Pow(.5f, 26f / 26f)) && vet.hypeLastTop10Week == -26, "an opening 1959 Top 10 seeds mid-1959");
			var deep = new SimulatedArtist();
			deep.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 30 });
			Check(Near(HitActHype.Hype(deep, 0), HitActHype.Top40Share * .5f), "an opening 1959 Top 40 seeds the Top 40 clock only");

			HitActHype.SetForChecks(.2f, 0f, 52f);
			Check(Near(HitActHype.Hype(hit, 152), .5f), "the half-life switch is read");
			GD.Print($"HIT_ACT_HYPE_CHECK passed checks={checks}");
		} finally {
			HitActHype.SetForChecks(-1f);
		}
	}
}
