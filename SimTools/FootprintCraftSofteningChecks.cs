using System;
using Godot;

/// <summary>--footprint-craft-softening-check: off, a single reads the label's own craft and no held region arrives by
/// the connected path; on, craft below the Major reference moves F of the way up while the order of labels holds and a
/// label at or above the reference is untouched, albums are untouched, and an unexposed region, an album or a single too
/// young for the lag never arrives. Neither path draws from the global stream.</summary>
public static class FootprintCraftSofteningChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Footprint/craft softening: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
		int lagWas = FootprintCraftSoftening.ArrivalLag; float craftWas = FootprintCraftSoftening.CraftFloor, hitWas = FootprintCraftSoftening.HitActCraftFloor;
		ulong stateWas = GameRandom.State;
		bool knownWas = KnownActAirplay.Enabled;
		try {
			FootprintCraftSoftening.SetForChecks(-1, 0f);
			foreach (float q in new[] { 0.3f, 0.655f, 0.85f, 0.97f })
				Check(FootprintCraftSoftening.Craft(q, ReleaseFormat.Single) == q, "off must read the stored craft exactly");
			var label = new AILabel { labelId = "indie", tier = LabelTier.Independent };
			var single = new RecordRuntimeData(new Record { recordId = "soft-single", artistId = "act", format = ReleaseFormat.Single,
				releaseDate = new GameDate(1961, 3, 1) }) { weeksSinceRelease = 6 };
			var exposed = new RegionalRecordData { regionId = "eastcoast", fitExposureAge = 0, awaitingArrival = true };
			Check(!FootprintCraftSoftening.Arrives(single, exposed, label), "off must never arrive");

			FootprintCraftSoftening.SetForChecks(1, 0.5f);
			float small = FootprintCraftSoftening.Craft(0.54f, ReleaseFormat.Single), indie = FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single),
				mid = FootprintCraftSoftening.Craft(0.72f, ReleaseFormat.Single), major = FootprintCraftSoftening.Craft(0.85f, ReleaseFormat.Single);
			Check(Near(indie, 0.7525f) && Near(small, 0.695f), "F = 0.5 must move craft half way to the reference");
			Check(small < indie && indie < mid && mid < major, "the order of labels must hold");
			Check(Near(major - small, 0.5f * (0.85f - 0.54f)), "the gap below the reference must shrink by 1 - F");
			Check(FootprintCraftSoftening.Craft(0.95f, ReleaseFormat.Single) == 0.95f, "a label above the reference must be untouched");
			Check(FootprintCraftSoftening.Craft(0.54f, ReleaseFormat.Album) == 0.54f, "albums must read the stored craft");

			var held = new RegionalRecordData { regionId = "westcoast", fitExposureAge = RegionalFitExposure.NotExposed, awaitingArrival = true };
			Check(!FootprintCraftSoftening.Arrives(single, held, label), "an unexposed region must wait for the existing paths");
			var album = new RecordRuntimeData(new Record { recordId = "soft-album", artistId = "act", format = ReleaseFormat.Album,
				releaseDate = new GameDate(1961, 3, 1) }) { weeksSinceRelease = 6 };
			Check(!FootprintCraftSoftening.Arrives(album, exposed, label), "albums must never arrive by this path");
			var young = new RecordRuntimeData(new Record { recordId = "soft-young", artistId = "act", format = ReleaseFormat.Single,
				releaseDate = new GameDate(1961, 3, 1) }) { weeksSinceRelease = 1 };
			Check(!FootprintCraftSoftening.Arrives(young, exposed, label), "a single must wait the lag after exposure");
			var late = new RegionalRecordData { regionId = "deepsouth", fitExposureAge = 6, awaitingArrival = true };
			Check(!FootprintCraftSoftening.Arrives(single, late, label), "a region exposed this week must wait the lag");
			// Whether a single connected is the keyed fit's own test; a single that does not connect never arrives.
			if (!SingleCommercialFit.Connected(single, label))
				Check(!FootprintCraftSoftening.Arrives(single, exposed, label), "a flop must never arrive by this path");
			// --hit-act-craft-floor: only a hot act's single at a non-Major, and only while the act is hot.
			KnownActAirplay.SetForChecks(true);
			FootprintCraftSoftening.SetForChecks(-1, 0f, 1f);
			var hot = new SimulatedArtist { artistId = "hot", hitCadenceLastTop40Week = 90 };
			var cold = new SimulatedArtist { artistId = "cold", hitCadenceLastTop40Week = 10 };
			var majorLabel = new AILabel { labelId = "major", tier = LabelTier.Major };
			Check(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single) == 0.655f, "the hit-act floor must not lift a single read without its act");
			Check(Near(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single, label, hot, 100), 0.85f), "a hot act's single must read the reference at F = 1");
			Check(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single, label, cold, 100) == 0.655f, "a cold act's single must read the label's craft");
			Check(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single, label, hot, 90 + LabelBlindLaunch.HotWindowWeeks + 1) == 0.655f, "the lift must end with the hot window");
			Check(FootprintCraftSoftening.Craft(0.80f, ReleaseFormat.Single, majorLabel, hot, 100) == 0.80f, "a Major's single must be untouched");
			Check(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Album, label, hot, 100) == 0.655f, "albums must read the stored craft");
			FootprintCraftSoftening.SetForChecks(-1, 0.5f, 0.25f);
			Check(Near(FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single, label, hot, 100), 0.7525f), "the larger of the two lifts must apply");
			FootprintCraftSoftening.SetForChecks(-1, 0f, 0f);
			var unread = new SimulatedArtist { artistId = "unread" };
			FootprintCraftSoftening.Craft(0.655f, ReleaseFormat.Single, label, unread, 100);
			Check(unread.hitCadenceLastTop40Week == HitActCadence.Unset, "off must not read the act's clock");
			Check(GameRandom.State == stateWas, "the checks must draw nothing");
			GD.Print("FOOTPRINT_CRAFT_SOFTENING_CHECK passed");
		} finally {
			FootprintCraftSoftening.SetForChecks(lagWas, craftWas, hitWas);
			KnownActAirplay.SetForChecks(knownWas);
		}
	}
}
