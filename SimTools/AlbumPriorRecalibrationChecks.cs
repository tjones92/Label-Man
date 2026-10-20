using System;
using Godot;

public static class AlbumPriorRecalibrationChecks {
	public static void Run() {
		bool saved = AlbumPriorRecalibration.Enabled;
		try {
			AlbumPriorRecalibration.SetForChecks(false);
			float legacy = CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Established, true);
			AlbumPriorRecalibration.SetForChecks(true);
			if (legacy != 1.5f || CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Established, false) != legacy ||
				CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Established, true) <= legacy * 2f ||
				AlbumPriorRecalibration.CareerAffinity(CareerState.Superstar) != AlbumPriorRecalibration.CareerAffinity(CareerState.Star) * 1.25f)
				throw new InvalidOperationException("Album prior: career correction, prewarm identity or Superstar premium failed.");
			CareerState[] states = { CareerState.NewSigning, CareerState.Rising, CareerState.Established, CareerState.Star, CareerState.Superstar };
			for (int i = 1; i < states.Length; i++)
				if (AlbumPriorRecalibration.CareerAffinity(states[i]) < AlbumPriorRecalibration.CareerAffinity(states[i - 1]))
					throw new InvalidOperationException("Album prior: inverted career affinity.");
			AlbumPriorRecalibration.SetForChecks(false);
			if (CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Established, true) != legacy)
				throw new InvalidOperationException("Album prior: off result changed.");
			if (AlbumMasterCosts.CompilationMultiplier(0, .6f) != 2.4f ||
				Math.Abs(AlbumMasterCosts.CompilationMultiplier(4, .6f) - .6f) > .000001f ||
				Math.Abs(AlbumMasterCosts.CompilationMultiplier(2, .6f) - 1.5f) > .000001f ||
				Math.Abs(AlbumMasterCosts.ExpectedMultiplier(.9f, 0, .6f) - 2.4f) > .000001f ||
				Math.Abs(AlbumMasterCosts.ExpectedMultiplier(.8f, 2, .6f) - 1.68f) > .000001f)
				throw new InvalidOperationException("Album costs: no-master, partial inventory or forecast/booking basis failed.");
			GD.Print("ALBUM_PRIOR_RECALIBRATION_CHECK passed");
		} finally { AlbumPriorRecalibration.SetForChecks(saved); }
	}
}
