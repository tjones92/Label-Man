using System;
using System.Collections.Generic;
using Godot;

/// <summary>--senior-cooling-check: identity when off; the clock, cool(k), the ladder steps and the comeback when on.</summary>
public static class SeniorCoolingChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Senior cooling: " + why); }
		bool wasOn = SeniorCooling.Enabled, wasLadder = SeniorCooling.LadderEnabled;
		try {
			// Off: every read is the identity and nothing is written.
			SeniorCooling.SetForChecks(false, false);
			var off = new SimulatedArtist { careerState = CareerState.Star, reputation = 0.6f, momentum = 0.3f };
			Check(SeniorCooling.LaunchFactor(off) == 1f, "off must read a factor of exactly 1");
			Check(off.GetNewReleaseAwarenessBonus() == off.momentum * 0.5f + off.reputation * 0.3f +
				(ArtistRecognition.Enabled ? ArtistRecognitionService.EffectiveRecognition(off) * ArtistRecognitionService.MaxAwarenessLift
					: 0.15f), "off must leave launch awareness bit-for-bit");
			Check(ArtistRecognitionService.LaunchRecognition(off) == ArtistRecognitionService.EffectiveRecognition(off), "off must leave launch recognition bit-for-bit");
			SeniorCooling.OnTop40(off, 40);
			Check(off.seniorCoolingLastTop40Week == SeniorCooling.Unset, "off must not write the clock");
			SeniorCooling.CoolLadderForWeek(500, new List<SimulatedArtist> { off });
			Check(off.careerState == CareerState.Star, "an off pass must not touch the ladder");

			// cool(k)
			SeniorCooling.SetForChecks(true, true);
			Check(SeniorCooling.Cool(0) == 1f && SeniorCooling.Cool(SeniorCooling.GraceWeeks) == 1f, "the grace year must read 1");
			float atH = SeniorCooling.Cool(SeniorCooling.GraceWeeks + (int)SeniorCooling.HalfLifeWeeks);
			Check(Math.Abs(atH - (SeniorCooling.Floor + (1f - SeniorCooling.Floor) * 0.5f)) < 1e-5f, $"G + H must be halfway to the floor (got {atH})");
			Check(Math.Abs(SeniorCooling.Cool(10000) - SeniorCooling.Floor) < 1e-4f, "the factor must settle on the floor");

			// The clock: a Top 40 resets it, an opening career seeds from its history, others start at first read.
			var hit = new SimulatedArtist();
			hit.publicHistory.Add(new PublicReleaseEntry { year = 1958, peakPosition = 12 });
			hit.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 55 });
			Check(SeniorCooling.WeeksSinceTop40(hit, 10) == 10 + 78, "a 1958 Top 40 seeds 78 weeks before week 0");
			var fresh = new SimulatedArtist();
			Check(SeniorCooling.WeeksSinceTop40(fresh, 30) == 0 && SeniorCooling.WeeksSinceTop40(fresh, 31) == 1, "no history starts the clock at first read");
			SeniorCooling.OnTop40(hit, 100);
			Check(SeniorCooling.WeeksSinceTop40(hit, 120) == 20, "a Top 40 resets the clock");

			// The ladder: one rung after G, one more per H, Declining is the bottom, no RNG.
			var star = new SimulatedArtist { careerState = CareerState.Superstar };
			SeniorCooling.OnTop40(star, 0);
			var acts = new List<SimulatedArtist> { star, null };
			var seen = new List<(int week, CareerState state)>();
			for (int week = 1; week <= 200; week++) {
				CareerState before = star.careerState;
				SeniorCooling.CoolLadderForWeek(week, acts);
				SeniorCooling.CoolLadderForWeek(week, acts); // same week again: no-op
				if (star.careerState != before) seen.Add((week, star.careerState));
			}
			Check(seen.Count == 3, $"Superstar must take three steps to Declining (took {seen.Count})");
			Check(seen[0] == (53, CareerState.Star) && seen[1] == (79, CareerState.Established) && seen[2] == (105, CareerState.Declining),
				"steps must land at G + 1, then every H weeks: " + string.Join(", ", seen));
			Check(star.seniorCoolingSteps == 3, "the step counter must count steps");

			// The comeback: a Top 40 resets the clock and the ladder's own rule restores Established.
			star.consecutiveHits = 0;
			star.RegisterTop40Hit();
			Check(star.careerState == CareerState.Established, "Declining -> Established on one Top 40 must be untouched");
			Check(star.seniorCoolingLastTop40Week == (ChartManager.Instance?.GetCurrentChartWeek() ?? 0), "RegisterTop40Hit must reset the clock");
			const int restart = 300; // the synthetic weeks above ran to 200; place the comeback after them
			SeniorCooling.OnTop40(star, restart);
			for (int week = restart + 1; week <= restart + SeniorCooling.GraceWeeks; week++) SeniorCooling.CoolOne(star, week);
			Check(star.careerState == CareerState.Established, "a hot act must not cool inside the grace year");
			SeniorCooling.CoolOne(star, restart + SeniorCooling.GraceWeeks + 1);
			Check(star.careerState == CareerState.Declining, "a fresh spell must step at G + 1 again");

			// Launch-only: the reads cool, the ladder does not.
			SeniorCooling.SetForChecks(true, false);
			var held = new SimulatedArtist { careerState = CareerState.Star, reputation = 0.8f };
			SeniorCooling.OnTop40(held, 0);
			SeniorCooling.CoolLadderForWeek(300, new List<SimulatedArtist> { held });
			Check(held.careerState == CareerState.Star, "launch-only must not run the ladder pass");
			Check(!SeniorCooling.LadderEnabled, "launch-only must read the ladder as off");
			GD.Print("SENIOR_COOLING_CHECK passed");
		} finally {
			SeniorCooling.SetForChecks(wasOn, wasLadder);
		}
	}
}
