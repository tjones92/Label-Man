using System;

/// <summary>
/// --known-act-airplay-check: identity when off; when on, the hot-at-launch decision (made once, on the lane's clock
/// and window), the label-blind push for a hot act's charting single only, the week factor kept, and the parse rules.
/// </summary>
public static class KnownActAirplayChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Known-act airplay: " + why); }
		bool wasOn = KnownActAirplay.Enabled; float wasPush = KnownActAirplay.Push;
		bool wasLane = HitActCadence.LaneEnabled, wasRetention = HitActCadence.RetentionEnabled;
		RecordRuntimeData Single(string id, int position, ReleaseFormat format = ReleaseFormat.Single) =>
			new RecordRuntimeData {
				baseRecord = new Record { recordId = id, artistId = "act", labelId = "L", format = format,
					hookStrength = 0.8f, productionQuality = 0.8f, danceability = 0.8f },
				currentPosition = position, weeksSinceRelease = 8, currentLabelPush = 0.42f };
		const float Eps = 1e-6f;
		try {
			HitActCadence.SetForChecks(false, false);

			// Off: the radio reads the label's own push, nothing is decided or written, and the clock stays unset.
			KnownActAirplay.SetForChecks(false);
			var hotOff = new SimulatedArtist { hitCadenceLastTop40Week = 90 };
			var offRec = Single("off", 12);
			Check(KnownActAirplay.RadioPush(offRec, 0.85f, hotOff, 100) == -1f, "off must write no push");
			Check(offRec.knownActHotAtLaunch == -1, "off must decide nothing");
			Check(KnownActAirplay.ForRadio(offRec) == 0.42f, "off must read currentLabelPush");
			var offClock = new SimulatedArtist();
			HitActCadence.OnTop40(offClock, 10);
			Check(offClock.hitCadenceLastTop40Week == HitActCadence.Unset && !HitActCadence.ClockEnabled, "off must not write the clock");
			var offLabel = new AILabel { marketingPower = 0.9f, budgetLevel = 0.9f };
			var viaPush = Single("via", 5);
			ChartSimulator.UpdateLabelPushForChecks(viaPush, offLabel);
			Check(viaPush.knownActRadioPush == -1f && viaPush.knownActHotAtLaunch == -1, "off UpdateLabelPush must leave the probe fields unset");

			// On alone writes the clock (the lane and retention stay off).
			KnownActAirplay.SetForChecks(true);
			var clock = new SimulatedArtist();
			HitActCadence.OnTop40(clock, 40);
			Check(clock.hitCadenceLastTop40Week == 40 && HitActCadence.ClockEnabled && !HitActCadence.AnyEnabled,
				"airplay alone must write the hot clock without turning on the lane or retention");

			// A hot act's charting single: label-blind push x the record's own week factor.
			var hot = new SimulatedArtist { hitCadenceLastTop40Week = 100 };
			var hotRec = Single("hot", 15);
			float p = KnownActAirplay.RadioPush(hotRec, 0.85f, hot, 100 + HitActCadence.HotWindowWeeks);
			Check(Math.Abs(p - KnownActAirplay.DefaultPush * 0.85f) < Eps && hotRec.knownActHotAtLaunch == 1,
				"52 weeks after a Top 40 is hot; push must be P x week factor");
			hotRec.knownActRadioPush = p;
			Check(Math.Abs(KnownActAirplay.ForRadio(hotRec) - p) < Eps, "the radio must read the known-act push");
			var edge = Single("edge", 15);
			Check(KnownActAirplay.RadioPush(edge, 0.85f, new SimulatedArtist { hitCadenceLastTop40Week = 100 }, 101 + HitActCadence.HotWindowWeeks) == -1f &&
				edge.knownActHotAtLaunch == 0, "53 weeks after is cold");

			// The decision is made once: a hot-at-launch record keeps it after the act cools; a cold one stays cold
			// after its own Top 40 makes the act hot.
			Check(KnownActAirplay.RadioPush(hotRec, 0.85f, hot, 400) > 0f, "a hot launch must stay hot as the act cools");
			var cold = new SimulatedArtist();
			var coldRec = Single("cold", 60);
			Check(KnownActAirplay.RadioPush(coldRec, 1f, cold, 200) == -1f && coldRec.knownActHotAtLaunch == 0, "a never-hot act is cold");
			HitActCadence.OnTop40(cold, 201);
			coldRec.currentPosition = 30;
			Check(KnownActAirplay.RadioPush(coldRec, 1f, cold, 202) == -1f, "a cold launch must stay cold after its own Top 40");
			Check(KnownActAirplay.RadioPush(Single("noartist", 10), 1f, null, 50) == -1f, "an unknown act is cold");

			// Only while on the Hot 100, and only for singles.
			var offChart = Single("pre", 0);
			Check(KnownActAirplay.RadioPush(offChart, 1f, hot, 120) == -1f && offChart.knownActHotAtLaunch == 1,
				"before entry the label's push stands, though the launch is already decided hot");
			offChart.currentPosition = 100;
			Check(KnownActAirplay.RadioPush(offChart, 1f, hot, 121) > 0f, "#100 is on the chart");
			offChart.currentPosition = 101;
			Check(KnownActAirplay.RadioPush(offChart, 1f, hot, 122) == -1f, "#101 is off it");
			var lp = Single("lp", 3, ReleaseFormat.Album);
			Check(KnownActAirplay.RadioPush(lp, 1f, hot, 120) == -1f && lp.knownActHotAtLaunch == -1, "an album is never touched");

			// Seeded clock: a 1959 Top 40 is hot at week 0 (26 weeks), a 1958 one is not (78 weeks).
			var vet59 = new SimulatedArtist(); vet59.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 12 });
			var vet58 = new SimulatedArtist(); vet58.publicHistory.Add(new PublicReleaseEntry { year = 1958, peakPosition = 12 });
			Check(KnownActAirplay.RadioPush(Single("v59", 20), 1f, vet59, 0) > 0f, "a 1959 Top 40 is hot at week 0");
			Check(KnownActAirplay.RadioPush(Single("v58", 20), 1f, vet58, 0) == -1f, "a 1958 Top 40 is not");

			// A custom P, clamped output, and the counters.
			KnownActAirplay.SetForChecks(true, 0.4f);
			Check(Math.Abs(KnownActAirplay.RadioPush(Single("p", 8), 0.5f, hot, 120) - 0.2f) < Eps, "P = 0.4 at week factor 0.5 must give 0.2");
			Check(KnownActAirplay.HotLaunches == 1 && KnownActAirplay.AppliedRecordWeeks == 1, "counters must count decisions and applied weeks");

			// A label-less record clears any earlier value.
			var orphan = Single("orphan", 5); orphan.knownActRadioPush = 0.3f;
			ChartSimulator.UpdateLabelPushForChecks(orphan, null);
			Check(orphan.knownActRadioPush == -1f && orphan.currentLabelPush == 0.02f, "a label-less record must read the 0.02 floor");

			// Parse.
			Check(KnownActAirplay.Parse(new[] { "--x" }) == (false, KnownActAirplay.DefaultPush), "absent is off");
			Check(KnownActAirplay.Parse(new[] { KnownActAirplay.Switch }) == (true, KnownActAirplay.DefaultPush), "bare switch uses the default");
			Check(KnownActAirplay.Parse(new[] { KnownActAirplay.Switch + "=0.45" }) == (true, 0.45f), "=P sets P");
			bool Throws(params string[] a) { try { KnownActAirplay.Parse(a); return false; } catch (ArgumentException) { return true; } }
			Check(Throws(KnownActAirplay.Switch + "=1.2") && Throws(KnownActAirplay.Switch + "=x") &&
				Throws(KnownActAirplay.Switch, KnownActAirplay.Switch + "=0.5"), "bad or repeated values must throw");
			Godot.GD.Print("KNOWN_ACT_AIRPLAY_CHECK passed");
		} finally {
			KnownActAirplay.SetForChecks(wasOn, wasPush);
			HitActCadence.SetForChecks(wasLane, wasRetention);
		}
	}
}
