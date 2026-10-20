using System;
using Godot;

/// <summary>
/// --album-scale-career-milestones-check: identity when off; when on, the percentile conversion, the run's best
/// equivalent across a chart-size change, the album #1 and Star rule, the hit-cadence clock, and the never-charted
/// single's momentum bucket (audit B1 and B2).
/// </summary>
public static class AlbumScaleCareerMilestonesChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Album-scale career milestones: " + why); }
		bool was = AlbumScaleCareerMilestones.Enabled;
		bool wasLane = HitActCadence.LaneEnabled, wasRetention = HitActCadence.RetentionEnabled;
		RecordRuntimeData Album(int peak) => new RecordRuntimeData {
			baseRecord = new Record { format = ReleaseFormat.Album }, peakPosition = peak, currentPosition = peak };
		RecordRuntimeData Single(int peak) => new RecordRuntimeData {
			baseRecord = new Record { format = ReleaseFormat.Single }, peakPosition = peak, currentPosition = peak };
		SimulatedArtist Established() => new SimulatedArtist {
			artistId = "a_est", careerState = CareerState.Established, momentum = 0.5f, reputation = 0.5f };
		try {
			// Off: every read is the raw peak, nothing is written, a never-charted single costs nothing.
			AlbumScaleCareerMilestones.SetForChecks(false);
			var offAlbum = Album(120);
			Check(AlbumScaleCareerMilestones.ObserveCareerPeak(offAlbum, 150) == 120, "off must observe the raw peak");
			Check(offAlbum.albumCareerPeak == 0, "off must not write the career peak");
			Check(AlbumScaleCareerMilestones.CareerPeak(offAlbum, 150) == 120, "off must score the raw peak");
			Check(!AlbumScaleCareerMilestones.PenalizesNeverCharted(Single(0)), "off must not penalise a never-charted single");
			var offAct = Established();
			offAct.CompleteChartRun(0, 0, 0, true, 0, AlbumScaleCareerMilestones.PenalizesNeverCharted(Single(0)));
			Check(offAct.momentum == 0.5f, "off: a never-charted single must cost no momentum");

			// The conversion: percentile of chart size on a 100-slot scale; #1 alone stays #1; 100 slots is the identity.
			for (int p = 0; p <= 100; p++)
				Check(AlbumScaleCareerMilestones.SinglesEquivalent(p, 100) == p, $"a 100-slot chart must be the identity at {p}");
			(int pos, int size, int want)[] table = {
				(1, 40, 1), (2, 40, 5), (16, 40, 40), (17, 40, 43), (24, 40, 60), (25, 40, 63), (40, 40, 100),
				(1, 150, 1), (2, 150, 2), (15, 150, 10), (16, 150, 11), (60, 150, 40), (61, 150, 41),
				(90, 150, 60), (91, 150, 61), (150, 150, 100), (80, 200, 40), (120, 200, 60), (121, 200, 61) };
			foreach (var (pos, size, want) in table) {
				int got = AlbumScaleCareerMilestones.SinglesEquivalent(pos, size);
				Check(got == want, $"#{pos} of {size} must read as #{want} (got {got})");
			}

			AlbumScaleCareerMilestones.SetForChecks(true);
			// The run's best equivalent, each week on that week's chart size; it never worsens.
			var crossing = Album(45);
			Check(AlbumScaleCareerMilestones.ObserveCareerPeak(crossing, 50) == 90, "#45 of 50 must read as #90");
			crossing.currentPosition = 50; crossing.peakPosition = 45;
			Check(AlbumScaleCareerMilestones.ObserveCareerPeak(crossing, 150) == 34, "#50 of 150 must improve the run to #34");
			crossing.currentPosition = 140;
			Check(AlbumScaleCareerMilestones.ObserveCareerPeak(crossing, 150) == 34, "a lower week must not worsen the run");
			Check(AlbumScaleCareerMilestones.CareerPeak(crossing, 200) == 34, "completion must score the observed best");
			Check(AlbumScaleCareerMilestones.CareerPeak(Album(30), 150) == 20, "an unobserved album must fall back to today's chart");
			Check(AlbumScaleCareerMilestones.CareerPeak(Album(0), 150) == 0, "a never-charted album stays at 0");
			Check(AlbumScaleCareerMilestones.CareerPeak(Single(75), 40) == 75, "a single must keep its raw peak");
			Check(AlbumScaleCareerMilestones.ObserveCareerPeak(Single(75), 40) == 75, "a single must observe its raw peak");

			// B1, the ladder: an Established act's album #1 and two upper-half LPs on the 150-slot chart make a Star.
			// Off, the same raw peaks (#1, #50, #60) are one hit and two neutral runs.
			int[] rawPeaks = { 1, 50, 60 };
			var onAct = Established();
			foreach (int raw in rawPeaks)
				onAct.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(raw), 150), 10, 1000);
			Check(onAct.numberOnes == 1, "an album #1 must count as a #1");
			Check(onAct.careerState == CareerState.Star, $"#1, #50 and #60 of 150 must make a Star (got {onAct.careerState})");
			AlbumScaleCareerMilestones.SetForChecks(false);
			var rawAct = Established();
			foreach (int raw in rawPeaks)
				rawAct.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(raw), 150), 10, 1000);
			Check(rawAct.careerState == CareerState.Established, "off, #50 and #60 must not count as hits");
			AlbumScaleCareerMilestones.SetForChecks(true);

			// B1, the 1963 flop: #120 of 150 is a flop on singles thresholds (-0.15, streak reset) but #80 on its own
			// scale (-0.10); #95 of 150 (#64) is still a flop. A 1960 #30 of 40 (#75) is a flop, not a Top 40 hit.
			var lp = Established(); lp.consecutiveHits = 2;
			lp.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(120), 150), 5, 500);
			Check(Mathf.IsEqualApprox(lp.momentum, 0.40f), $"#120 of 150 must take the #61-100 bucket (got {lp.momentum})");
			var early = Established();
			early.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(30), 40), 5, 500);
			Check(early.top40Hits == 0 && early.consecutiveFlops == 1, "#30 of 40 must score as a flop, not a Top 40 hit");
			var upper = Established();
			upper.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(60), 150), 5, 500);
			Check(upper.top40Hits == 1 && upper.consecutiveFlops == 0, "#60 of 150 must score as a Top 40 hit");

			// The hit-cadence clock follows the converted Top 40, and only it.
			HitActCadence.SetForChecks(true, false);
			var clocked = Established();
			clocked.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(60), 150), 5, 500);
			Check(clocked.hitCadenceLastTop40Week != HitActCadence.Unset, "an album Top 40 on its own scale must set the cadence clock");
			var unclocked = Established();
			unclocked.UpdateAfterChartRun(AlbumScaleCareerMilestones.CareerPeak(Album(30), 40), 5, 500);
			Check(unclocked.hitCadenceLastTop40Week == HitActCadence.Unset, "a 1960 #30 of 40 must not set the cadence clock");
			HitActCadence.SetForChecks(wasLane, wasRetention);

			// B2: a never-charted single takes the worst bucket; a never-charted album does not; #75 is unchanged.
			var miss = Established();
			miss.UpdateAfterChartRun(0, 0, 100, true, 0, AlbumScaleCareerMilestones.PenalizesNeverCharted(Single(0)));
			Check(Mathf.IsEqualApprox(miss.momentum, 0.35f), $"a never-charted single must cost 0.15 momentum (got {miss.momentum})");
			Check(miss.consecutiveFlops == 1, "a never-charted single is still a flop");
			var lowChart = Established();
			lowChart.UpdateAfterChartRun(75, 2, 100, true, 0, AlbumScaleCareerMilestones.PenalizesNeverCharted(Single(75)));
			Check(Mathf.IsEqualApprox(lowChart.momentum, 0.40f), "a #75 single keeps its -0.10");
			Check(miss.momentum < lowChart.momentum, "never charting must cost more than charting low");
			var lpMiss = Established();
			lpMiss.UpdateAfterChartRun(0, 0, 100, true, 0, AlbumScaleCareerMilestones.PenalizesNeverCharted(Album(0)));
			Check(lpMiss.momentum == 0.5f, "a never-charted album must cost no momentum");
			Check(lpMiss.consecutiveFlops == 1, "a never-charted album is still a flop");

			// The other readers (landmark recognition, critical acclaim, era tally, label credit) all take CareerPeak.
			// A 1964 #120 of 150 LP read nothing commercial before (past #100); on its own scale it is #80.
			var deepLp = Album(120);
			Check(CulturalRecognitionService.GetCommercialRecognition(AlbumScaleCareerMilestones.CareerPeak(deepLp, 150)) > 0f,
				"on, a #120 of 150 LP must earn commercial recognition");
			Check(ArtistCriticalAcclaimService.GetCommercialScore(AlbumScaleCareerMilestones.CareerPeak(deepLp, 150)) > 0f,
				"on, a #120 of 150 LP must earn a commercial acclaim score");
			AlbumScaleCareerMilestones.SetForChecks(false);
			Check(CulturalRecognitionService.GetCommercialRecognition(AlbumScaleCareerMilestones.CareerPeak(deepLp, 150)) == 0f,
				"off, a #120 LP reads as uncharted, as before");
			AlbumScaleCareerMilestones.SetForChecks(true);

			GD.Print("ALBUM_SCALE_CAREER_MILESTONES_CHECK passed");
		} finally {
			AlbumScaleCareerMilestones.SetForChecks(was);
			HitActCadence.SetForChecks(wasLane, wasRetention);
		}
	}
}
