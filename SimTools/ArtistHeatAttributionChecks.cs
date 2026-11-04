using System;

public static class ArtistHeatAttributionChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Artist heat attribution: " + why); }
		bool was = ArtistHeatAttribution.SinglesWithoutAlbums;
		bool wasCurrent = ArtistHeatAttribution.SinglesWithCurrentAlbums;
		try {
			var single = new RecordRuntimeData { baseRecord = new Record { format = ReleaseFormat.Single }, currentPosition = 20, peakPosition = 5 };
			var album = new RecordRuntimeData { baseRecord = new Record { format = ReleaseFormat.Album }, currentPosition = 5, peakPosition = 5 };
			var split = ArtistHeatAttribution.Describe(new[] { single, album }, 0.75f);
			Check(Math.Abs(split.SinglesOnly - 0.3f) < 1e-6f, "one Top 40 single with a Top 10 peak contributes 0.30 including its hit bonus");
			Check(Math.Abs(split.Original - split.SinglesOnly - 0.45f) < 1e-6f, "the album contributes current, retained-peak and hit-count heat");
			ArtistHeatAttribution.SetPolicyForChecks(false);
			Check(ArtistHeatAttribution.SelectHeat(ReleaseFormat.Single, split.Original, split.SinglesOnly, split.CurrentAlbums, true) == split.Original, "off is a bit-for-bit identity");
			ArtistHeatAttribution.SetPolicyForChecks(true);
			Check(ArtistHeatAttribution.SelectHeat(ReleaseFormat.Single, split.Original, split.SinglesOnly, split.CurrentAlbums, true) == split.SinglesOnly, "the full ablation changes singles' input");
			ArtistHeatAttribution.SetPolicyForChecks(false, true);
			Check(Math.Abs(split.CurrentAlbums - 0.6f) < 1e-6f, "current Top 10 album hype retains its full 0.30 contribution");
			Check(ArtistHeatAttribution.SelectHeat(ReleaseFormat.Single, split.Original, split.SinglesOnly, split.CurrentAlbums, true) == split.CurrentAlbums, "the narrower diagnostic retains current album hype");
			Check(ArtistHeatAttribution.SelectHeat(ReleaseFormat.Album, split.Original, split.SinglesOnly, split.CurrentAlbums, true) == split.Original, "albums keep the authored heat");
			Check(ArtistHeatAttribution.SelectHeat(ReleaseFormat.Single, split.Original, split.SinglesOnly, split.CurrentAlbums, false) == split.Original, "prewarm keeps the authored heat");
			var oldAlbum = new RecordRuntimeData { baseRecord = new Record { format = ReleaseFormat.Album }, currentPosition = 0, peakPosition = 5 };
			Check(ArtistHeatAttribution.Describe(new[] { oldAlbum }, 0.15f).CurrentAlbums == 0f, "past album peaks leave durable recognition to its existing service");
			float rawSingleHeat = 0.15f + 0.1f + 0.05f;
			Check(ArtistHeatAttribution.Describe(new[] { single }, rawSingleHeat).SinglesOnly == rawSingleHeat, "a single-only catalog retains the authored addition order");
		} finally { ArtistHeatAttribution.SetPolicyForChecks(was, wasCurrent); }
	}
}
