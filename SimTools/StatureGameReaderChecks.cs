using System;
using System.Text.Json;
using Godot;

/// <summary>Exercises normal startup, the programme's real financial forecast, and the game's media save path.</summary>
public static class StatureGameReaderChecks {
	public static void Run() {
		var original = new WorldSaveData();
		MediaAtmosphereService.CaptureWorld(original);
		var config = StatureObservation.ShadowConfig;
		bool enabled = ArtistStature.ProgrammeAlbumReader, persisted = MediaAtmosphereService.PersistExperimentalStature;
		var mediaMode = MediaAtmosphere.Mode;
		bool following = MediaFollowing.Active;
		int checks = 0;
		void Check(bool value, string message) {
			if (!value) throw new InvalidOperationException("Stature game reader: " + message);
			checks++;
		}
		try {
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
			MediaFollowing.SetForChecks(true);
			ArtistStature.ProgrammeAlbumReader = false;
			StatureObservation.ShadowConfig = null;
			MediaAtmosphereService.ResetForProbe();
			ArtistStature.Configure(Array.Empty<string>());
			Check(!ArtistStature.ProgrammeAlbumReader && StatureObservation.ShadowConfig == null, "ordinary default stays unchanged");
			bool rejected = false;
			try { ArtistStature.Configure(new[] { StatureObservation.ProgrammeReaderSwitch }); }
			catch (ArgumentException) { rejected = true; }
			Check(rejected, "reader requires an explicit calibration");
			ArtistStature.Configure(new[] {
				ArtistStature.CalibrationSwitch + ProjectSettings.GlobalizePath("res://SimTools/StatureShadowExperimental.json"),
				StatureObservation.ProgrammeReaderSwitch
			});
			Check(ArtistStature.ProgrammeAlbumReader && MediaAtmosphereService.PersistExperimentalStature,
				"normal startup enables persisted reader without audit or CSV switches");

			var date = new GameDate(1960, 7, 8);
			var artist = new SimulatedArtist { artistId = "stature-game-check", careerState = CareerState.Rising,
				primaryGenre = Genre.TraditionalPop, lifecycleStatus = ArtistLifecycleStatus.Active,
				vocalPower = .7f, musicianship = .7f, songwritingAbility = .7f, studioPerformance = .7f };
			var label = new AILabel { labelId = "stature-game-label", distributionStrength = .8f, ownedReach = .8f };
			var snapshot = new StatureWeeklyShadow.Snapshot { Date = new DateTime(1960, 7, 1),
				Band = StatureShadowPolicy.Band.Star, Status = StatureShadowPolicy.ResultStatus.Observed };
			MediaAtmosphereService.ExperimentalStatureShadow().Snapshots[artist.artistId] = snapshot;
			var manager = CompetitorManager.Instance;
			ArtistStature.ProgrammeAlbumReader = false;
			var control = manager.ProgrammeAlbumPriorForChecks(label, artist, date);
			artist.careerState = CareerState.Star;
			var star = manager.ProgrammeAlbumPriorForChecks(label, artist, date);
			artist.careerState = CareerState.Rising;
			ArtistStature.ProgrammeAlbumReader = true;
			var treatment = manager.ProgrammeAlbumPriorForChecks(label, artist, date);
			Check(treatment == star && treatment.Multiplier > control.Multiplier && treatment.Net > control.Net,
				"programme financial forecast applies standing through the existing table and margin calculation");
			Check(artist.careerState == CareerState.Rising && artist.contractTop40Hits == 0,
				"forecast leaves career and contract evidence intact");
			label.isPlayerOwned = true;
			ArtistStature.ProgrammeAlbumReader = false;
			var playerControl = manager.ProgrammeAlbumPriorForChecks(label, artist, date);
			ArtistStature.ProgrammeAlbumReader = true;
			Check(manager.ProgrammeAlbumPriorForChecks(label, artist, date) == playerControl, "player label uses legacy forecast");
			label.isPlayerOwned = false;
			snapshot.Date = new DateTime(1960, 6, 30);
			Check(manager.ProgrammeAlbumPriorForChecks(label, artist, date) == control, "stale snapshot uses legacy forecast");
			snapshot.Date = new DateTime(1960, 7, 1);
			snapshot.Band = StatureShadowPolicy.Band.Rising;
			artist.careerState = CareerState.Star;
			Check(manager.ProgrammeAlbumPriorForChecks(label, artist, date) == control, "cooling can lower the real forecast without an uplift floor");
			artist.careerState = CareerState.Rising;
			snapshot.Band = StatureShadowPolicy.Band.Star;

			var save = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(save);
			string json = JsonSerializer.Serialize(save.Media, SaveGameService.TestJsonOptions);
			ArtistStature.Configure(Array.Empty<string>());
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData {
				Media = JsonSerializer.Deserialize<MediaSaveData>(json, SaveGameService.TestJsonOptions)
			});
			Check(ArtistStature.ProgrammeAlbumReader && MediaAtmosphereService.PersistExperimentalStature &&
				StatureObservation.ShadowConfig.Version.Contains("sha256:"), "save restores reader and embedded, fingerprinted calibration");
			Check(manager.ProgrammeAlbumPriorForChecks(label, artist, date) == treatment,
				"restored game makes the same forecast without reopening a calibration file");
			ArtistStature.ProgrammeAlbumReader = false;
			MediaAtmosphereService.CaptureWorld(save);
			Check(!JsonSerializer.Serialize(save.Media, SaveGameService.TestJsonOptions).Contains("StatureReader"),
				"disabled reader omits its save settings");
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData());
			Check(!ArtistStature.ProgrammeAlbumReader && StatureObservation.ShadowConfig == null,
				"loading an older slot does not inherit another save's reader");
			ArtistStature.Configure(new[] {
				ArtistStature.CalibrationSwitch + ProjectSettings.GlobalizePath("res://SimTools/StatureShadowNationalHighTiers.json"),
				StatureObservation.ProgrammeReaderSwitch
			});
			Check(StatureObservation.ShadowConfig.Policy.Bands[1].EligibleRoutes[0] &&
				!StatureObservation.ShadowConfig.Policy.Bands[2].EligibleRoutes[0] &&
				!StatureObservation.ShadowConfig.Policy.Bands[3].EligibleRoutes[0], "normal startup loads national high-tier eligibility");
			MediaAtmosphereService.CaptureWorld(save);
			json = JsonSerializer.Serialize(save.Media, SaveGameService.TestJsonOptions);
			ArtistStature.Configure(Array.Empty<string>());
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData {
				Media = JsonSerializer.Deserialize<MediaSaveData>(json, SaveGameService.TestJsonOptions)
			});
			Check(ArtistStature.ProgrammeAlbumReader &&
				!StatureObservation.ShadowConfig.Policy.Bands[2].EligibleRoutes[0] &&
				StatureObservation.ShadowConfig.Policy.Version.Contains("sha256:"),
				"game save restores fingerprinted candidate eligibility without reopening its file");
			GD.Print($"STATURE_GAME_READER_CHECK passed checks={checks}");
		} finally {
			MediaAtmosphereService.RehydrateWorld(original);
			StatureObservation.ShadowConfig = config;
			ArtistStature.ProgrammeAlbumReader = enabled;
			MediaAtmosphereService.PersistExperimentalStature = persisted;
			MediaAtmosphere.SetForProbe(mediaMode);
			MediaFollowing.SetForChecks(following);
		}
	}
}
