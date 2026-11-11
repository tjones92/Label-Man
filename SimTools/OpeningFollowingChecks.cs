using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class OpeningFollowingChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
		ulong rng = GameRandom.State;
		var original = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(original);
		try {
			var fr = new FollowingRegion { Young = new() { [1958] = 100 }, Adult = 40 };
			Check(!JsonSerializer.Serialize(fr, SaveGameService.TestJsonOptions).Contains("OpeningOrigin"), "Absent origin must preserve save schema");
			OpeningFollowingOrigin.Seed(fr);
			Check(fr.YoungTotal() == 100 && fr.Adult == 40 && fr.OpeningOrigin.YoungTotal == 100 && fr.OpeningOrigin.Adult == 40, "Tagging mutates no audience stock");
			fr.OpeningOrigin.Scale(.8, .5); fr.Young[1958] = 80; fr.Adult = 20;
			Check(fr.OpeningOrigin.YoungTotal == 80 && fr.OpeningOrigin.Adult == 20, "Disappointment/controversy source survival");
			double aged = fr.OpeningOrigin.AgeCohort(1958, 80, 16, 32); fr.Young[1958] = 32;
			Check(aged == 16 && fr.OpeningOrigin.YoungTotal == 32, "Age transfer and young losses");
			fr.OpeningOrigin.SettleAdult(10, aged * .5, 9, 18); fr.Adult = 9;
			Check(fr.OpeningOrigin.Adult == 9, "Capacity cap scales source mixture proportionally");
			fr.Young[1958] += 100; fr.Adult += 10; fr.OpeningOrigin.Validate(fr);
			Check(fr.OpeningOrigin.YoungTotal == 32 && fr.OpeningOrigin.Adult == 9, "New joins are not seed descendants");
			var restored = JsonSerializer.Deserialize<FollowingRegion>(JsonSerializer.Serialize(fr, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.OpeningOrigin.YoungTotal == 32 && restored.OpeningOrigin.Adult == 9, "Lineage round trips with following");
			fr.OpeningOrigin.AgeCohort(1958, 132, 0, .5f);
			Check(fr.OpeningOrigin.Young.Count == 0, "Pruned young cohorts lose their source tags");

			fr = new FollowingRegion { Young = new() { [1958] = 100 } }; OpeningFollowingOrigin.Seed(fr); fr.Young[1960] = 20;
			var fs = new MediaFollowingState { ArtistId = "origin-fixture", Regions = new() { ["region"] = fr } };
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData { Media = new MediaSaveData { Following = new() { [fs.ArtistId] = fs } } });
			var evidence = new StatureWeeklyShadow.ArtistEvidence { Components = new double?[] { .001, .001, .001, .001, 0, 0, 120, 0 },
				Regions = new() { new() { Id = "region", PopulationMillions = 1, SalesShares = new double?[] { .001, .001, .001, .001 }, Following = 120 } } };
			var shadow = new StatureWeeklyShadow();
			shadow.RegionalHistory[fs.ArtistId] = new() { ["region"] = new double?[] { .001, .001, .001, .001 } };
			var snapshot = new StatureWeeklyShadow.Snapshot { Components = evidence.Components,
				References = new double?[] { 1, 1, 1, 1, 1, 1, 50, 50 }, Scores = new double?[] { .001, .001, .001, .001, 0, 0, 1, 0 }, RegionalResponse = .01 };
			var config = new StatureWeeklyShadow.Config { LocalSalesShareReference = .1, FollowingPerMillionReference = 100 };
			var half = OpeningFollowingDiagnostics.Derive(fs.ArtistId, evidence, shadow, snapshot, config, .5);
			var zero = OpeningFollowingDiagnostics.Derive(fs.ArtistId, evidence, shadow, snapshot, config, 0);
			Check(half.Routes[1].Draw.Lower == 1 && Math.Abs(zero.Routes[1].Draw.Lower - .4) < 1e-10, "Subtract raw origin before clipping, preserving saturated half score");
			Check(Math.Abs(half.Record.Value - .7) < 1e-10 && Math.Abs(zero.Conversion.Value - .2) < 1e-10, "Recompute regional reach from remaining source counts");
			OpeningGeographyDiagnostics.Reset();
			OpeningGeographyDiagnostics.RecordRelease(new SimulatedArtist { artistId = fs.ArtistId, stageName = "Fixture", openingArchetype = OpeningCareerArchetype.RegionalName },
				new PublicReleaseEntry { year = 1958, peakPosition = 30, markets = 2, regionId = "Region Name", format = ReleaseFormat.Single }, 100, 0);
			using var releaseLog = new System.IO.StringWriter(); using var regionLog = new System.IO.StringWriter();
			OpeningGeographyDiagnostics.Open(releaseLog, regionLog);
			OpeningGeographyDiagnostics.Observe(new GameDate(1960, 7, 1), fs.ArtistId, evidence, shadow, config);
			OpeningGeographyDiagnostics.Close();
			Check(releaseLog.ToString().Contains("1958,Single,30,2,\"Region Name\",100,0"), "Release census retains named origin, market count and exact contribution");
			Check(regionLog.ToString().Contains("\"1958\"\":100") && fr.YoungTotal() == 120 && fr.OpeningOrigin.YoungTotal == 100, "Geography export preserves source cohorts and audience stock");
			evidence.Regions[0].Following = null;
			Check(OpeningFollowingDiagnostics.Derive(fs.ArtistId, evidence, shadow, snapshot, config, 0).Draw == null, "Unknown regional following is not zero");
			Check(GameRandom.State == rng, "Provenance and shadows consume no RNG");
			GD.Print($"OPENING_FOLLOWING_CHECK passed checks={checks}");
		} finally { MediaAtmosphereService.RehydrateWorld(original); }
	}
}
