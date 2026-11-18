using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>--single-commercial-fit-shape-check: the act-record kink and the upside cap (MajorShareRootCauseHandoff.md).
/// Off, both are exact identities and capture nothing. The act record is the recency of the act's last Top 40, captured once
/// at launch on a live AI single only and saved; a recent hit moves the kink so more draws connect. The cap bounds the lift
/// above the kink, and its normalizer keeps the reference launch at mean one. Nothing draws from the global stream.</summary>
public static class SingleCommercialFitShapeChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Commercial fit shape: " + why); checks++; }
		bool Near(double a, double b, double tol = 1e-6) => Math.Abs(a - b) < tol;
		RecordRuntimeData Launch(string id, ReleaseFormat format = ReleaseFormat.Single, bool player = false, int year = 1961) =>
			new(new Record { recordId = id, artistId = "shape-act", format = format, isPlayerOwned = player,
				releaseDate = new GameDate(year, 3, 1) }) { initialLaunchAwareness = .2f };
		void Set(float actWeight, float cap, float upside = 1f) =>
			SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: upside, launchWeight: 0f, floor: 0f,
				actRecordWeight: actWeight, upsideCap: cap);
		// Mean of exp(national log) over z ~ N(0, 1) at the reference launch, by quadrature.
		double Mean(float actRecord) {
			double sum = 0d, dz = 1e-3;
			for (double z = -9d; z < 9d; z += dz)
				sum += Math.Exp(-.5d * z * z) / Math.Sqrt(2d * Math.PI) * Math.Exp(SingleCommercialFit.National(z, .2f, .35f, actRecord)) * dz;
			return sum;
		}
		ulong stateBefore = GameRandom.State;
		try {
			// Off: identities.
			Set(0f, 0f, .15f);
			foreach (double z in new[] { -2d, 0d, 1.2d, 3d })
				Check(SingleCommercialFit.National(z, .5f, .35f, 1f) == SingleCommercialFit.National(z, .5f, .35f), "off ignores the act record");
			var artist = new SimulatedArtist { artistId = "shape-act", hitCadenceLastTop40Week = 100 };
			var off = Launch("shape-off");
			SingleCommercialFit.CaptureActRecord(off, artist, 100);
			Check(!off.actChartRecordAtLaunch.HasValue, "off captures nothing");
			Check(Near(Mean(0f), 1d, 1e-3), "uncapped reference launch keeps mean one");

			// Upside cap.
			Set(0f, .5f);
			Check(Near(Mean(0f), 1d, 1e-3), "capped normalizer keeps the reference launch at mean one");
			double top = SingleCommercialFit.National(8d, .2f, .35f);
			Check(Near(top, SingleCommercialFit.National(4d, .2f, .35f)), "draws far above the kink are flat at the cap");
			Set(0f, 0f);
			double uncappedHigh = SingleCommercialFit.National(4d, .2f, .35f);
			Set(0f, .5f);
			Check(top < uncappedHigh, "the cap lowers a lucky draw");
			Check(SingleCommercialFit.National(-2d, .2f, .35f) > -1e9, "below the kink stays finite");
			Set(0f, 50f);
			double wide = SingleCommercialFit.National(1.5d, .2f, .35f);
			Set(0f, 0f);
			Check(Near(wide, SingleCommercialFit.National(1.5d, .2f, .35f), 1e-6), "a cap that never binds equals no cap");

			// Act record.
			Check(SingleCommercialFit.ActRecord(artist, 100) == 1f, "a Top 40 this week reads 1");
			Check(Near(SingleCommercialFit.ActRecord(artist, 126), .5d), "half-life 26 weeks");
			Check(SingleCommercialFit.ActRecord(artist, 100 + SingleCommercialFit.ActRecordWindowWeeks + 1) == 0f, "zero past the window");
			Check(SingleCommercialFit.ActRecord(new SimulatedArtist(), 100) == 0f && SingleCommercialFit.ActRecord(null, 100) == 0f, "never charted reads 0");
			Set(1f, 0f);
			double justBelow = -.5d; // connected = above the reference median draw (z > 0 at the reference launch)
			Check(SingleCommercialFit.National(justBelow, .2f, .35f, 1f) > SingleCommercialFit.National(justBelow, .2f, .35f, 0f), "a recent hit lifts the same draw");
			Check(SingleCommercialFit.National(justBelow, .2f, .35f, 1f) > SingleCommercialFit.UnexposedCapLog
				&& SingleCommercialFit.National(justBelow, .2f, .35f, 0f) <= SingleCommercialFit.UnexposedCapLog, "a recent hit connects a near-miss draw");
			Check(Near(Mean(0f), 1d, 1e-3), "act weight leaves the cold reference at mean one");

			// Odds only: the act record connects more draws but does not enlarge a connected one.
			SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: 1f, launchWeight: 0f, floor: 0f,
				actRecordWeight: 2f, actRecordOddsOnly: true);
			foreach (double z in new[] { 1.1d, 2d, 3.5d })
				Check(SingleCommercialFit.National(z, .2f, .35f, 1f) == SingleCommercialFit.National(z, .2f, .35f, 0f), "odds only: a draw above the reference kink sells the same");
			double justConnected = SingleCommercialFit.National(1.04d, .2f, .35f, 0f);
			Check(Near(SingleCommercialFit.National(.5d, .2f, .35f, 1f), SingleCommercialFit.National(-.5d, .2f, .35f, 1f))
				&& SingleCommercialFit.National(.5d, .2f, .35f, 1f) <= justConnected + 1e-9, "odds only: draws between the kinks sell at the just-connected level");
			Check(SingleCommercialFit.National(-.5d, .2f, .35f, 1f) > SingleCommercialFit.UnexposedCapLog
				&& SingleCommercialFit.National(-.5d, .2f, .35f, 0f) <= SingleCommercialFit.UnexposedCapLog, "odds only: a recent hit still connects a near-miss draw");
			Check(Near(Mean(0f), 1d, 1e-3), "odds only leaves the cold reference at mean one");

			// Act-record size: a connected draw by a proven act sells exp(S x record) more; misses and cold acts unchanged.
			SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: 1f, launchWeight: 0f, floor: 0f,
				actRecordWeight: 2f, actRecordOddsOnly: true, actRecordSize: .4f);
			var sizeOn = new[] { 2d, .5d, -.5d }.Select(z => SingleCommercialFit.National(z, .2f, .35f, 1f)).ToArray();
			var sizeHalf = SingleCommercialFit.National(2d, .2f, .35f, .5f);
			var sizeCold = new[] { 2d, -2.5d }.Select(z => SingleCommercialFit.National(z, .2f, .35f, 0f)).ToArray();
			double sizeMiss = SingleCommercialFit.National(-2.5d, .2f, .35f, 1f);
			SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: 1f, launchWeight: 0f, floor: 0f,
				actRecordWeight: 2f, actRecordOddsOnly: true);
			var sizeOff = new[] { 2d, .5d, -.5d }.Select(z => SingleCommercialFit.National(z, .2f, .35f, 1f)).ToArray();
			Check(sizeOn.Zip(sizeOff).All(p => Near(p.First - p.Second, .4d)), "size: a proven act's connected draw rises by S x record");
			Check(Near(sizeHalf - SingleCommercialFit.National(2d, .2f, .35f, .5f), .2d), "size scales with the act record");
			Check(Near(sizeCold[0], SingleCommercialFit.National(2d, .2f, .35f, 0f)) && Near(sizeCold[1], SingleCommercialFit.National(-2.5d, .2f, .35f, 0f)),
				"size: a cold act is unchanged");
			Check(Near(sizeMiss, SingleCommercialFit.National(-2.5d, .2f, .35f, 1f)), "size: a draw that misses even the act's kink is unchanged");
			Set(2f, 0f);
			Check(SingleCommercialFit.National(2d, .2f, .35f, 1f) > SingleCommercialFit.National(2d, .2f, .35f, 0f), "full kink mode still enlarges a connected draw");
			Set(1f, 0f);

			var hot = Launch("shape-hot");
			SingleCommercialFit.CaptureActRecord(hot, artist, 126);
			Check(Near(hot.actChartRecordAtLaunch ?? -1f, .5d), "capture freezes the recency at launch");
			artist.hitCadenceLastTop40Week = 130;
			SingleCommercialFit.CaptureActRecord(hot, artist, 130);
			Check(Near(hot.actChartRecordAtLaunch ?? -1f, .5d), "later hits never resnapshot a released single");
			var album = Launch("shape-album", ReleaseFormat.Album); SingleCommercialFit.CaptureActRecord(album, artist, 130);
			var player = Launch("shape-player", player: true); SingleCommercialFit.CaptureActRecord(player, artist, 130);
			var opening = Launch("shape-opening", year: 1959); SingleCommercialFit.CaptureActRecord(opening, artist, 130);
			Check(!album.actChartRecordAtLaunch.HasValue && !player.actChartRecordAtLaunch.HasValue && !opening.actChartRecordAtLaunch.HasValue,
				"albums, player records and opening catalog capture nothing");

			string json = JsonSerializer.Serialize(hot, SaveGameService.TestJsonOptions);
			var restored = JsonSerializer.Deserialize<RecordRuntimeData>(json, SaveGameService.TestJsonOptions);
			Check(restored.actChartRecordAtLaunch == hot.actChartRecordAtLaunch
				&& SingleCommercialFit.Multiplier(restored, "south", null) == SingleCommercialFit.Multiplier(hot, "south", null), "world JSON keeps the act record and response");
			Check(RuntimeRecordSaveData.From(hot).ToRuntime().actChartRecordAtLaunch == hot.actChartRecordAtLaunch, "record DTO keeps the act record");
			if (RegionalFitExposure.Enabled) {
				// Same record, region and keyed draw. Only the servicing date changes.
				Set(0f, 0f);
				var single = Launch("shape-listener-0");
				for (int i = 1; i <= 100 && !SingleCommercialFit.Connected(single, null); i++)
					single = Launch($"shape-listener-{i}");
				var region = new RegionalRecordData { regionId = "south", awaitingArrival = true, unitsInStores = 0 };
				single.regionalData["south"] = region;
				float heard = SingleCommercialFit.Multiplier(single, "south", null);
				region.fitExposureAge = RegionalFitExposure.NotExposed;
				Check(SingleCommercialFit.Multiplier(single, "south", null) < heard, "control caps a connected unserviced market");
				SingleCommercialFit.SetForChecks(2.5f, .5f, 1f, 1001UL, hitShare: .15f, upside: 1f, awareBuyers: true);
				Check(SingleCommercialFit.Multiplier(single, "south", null) == heard, "aware buyer receives the same keyed fit");
				Check(region.awaitingArrival && region.unitsInStores == 0 && !RegionalFitExposure.Exposed(single, "south"),
					"purchase experiment cannot open, service or stock a market");
			}
			Check(GameRandom.State == stateBefore, "no global random draw");
			GD.Print($"SINGLE_COMMERCIAL_FIT_SHAPE_CHECK passed checks={checks}");
		} finally { Set(0f, 0f, .15f); }
	}
}
