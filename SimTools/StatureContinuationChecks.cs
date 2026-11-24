using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

public static class StatureContinuationChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
		T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		ulong rngBeforeNames = GameRandom.State;
		try {
			var unique = typeof(LabelGenerator).GetMethod("GenerateUniqueName", BindingFlags.NonPublic | BindingFlags.Instance);
			ulong start = GameRandom.State;
			string first = (string)unique.Invoke(new LabelGenerator(), null);
			var original = new LabelGenerator(); original.RestoreUsedNames(new[] { first });
			var savedNames = Copy(original.CaptureUsedNames());
			GameRandom.State = start;
			string expectedName = (string)unique.Invoke(original, null); ulong expectedRng = GameRandom.State;
			var restoredGenerator = new LabelGenerator(); restoredGenerator.RestoreUsedNames(savedNames);
			GameRandom.State = start;
			string resumedName = (string)unique.Invoke(restoredGenerator, null);
			Check(expectedName != first && resumedName == expectedName && GameRandom.State == expectedRng,
				"label name collision and global RNG continue identically after restore");
		} finally { GameRandom.State = rngBeforeNames; }
		var shared = new AlbumTrack { sourceRecordId = "retired", genre = Genre.Blues };
		var separate = new AlbumTrack { sourceRecordId = "retired", genre = Genre.Blues };
		var album = new Album { trackRefs = new[] { shared }, nonSingleTracks = new[] { separate } };
		var record = new Record { recordId = "album", album = album };
		var project = new AlbumProject { projectId = "project", albumRecord = record };
		var world = new WorldSaveData {
			Records = new() { new RecordRuntimeData { baseRecord = record, albumRuntime = new AlbumRuntimeData { baseAlbum = album } } },
			Competitor = new() { AlbumProjects = new() { project }, PendingAlbumProjects = new() { project } },
			RetiredTrackArchive = new() { ["retired"] = shared },
			ShadowContinuation = new()
		};
		world.ShadowContinuation.TrackAliases = TrackContinuation.Capture(world);
		var loaded = Copy(world);
		Check(!ReferenceEquals(loaded.Records[0].baseRecord.album.trackRefs[0], loaded.RetiredTrackArchive["retired"]), "JSON reproduces split shared track");
		var applyGenre = typeof(CompetitorManager).GetMethod("ApplyProjectGenre", BindingFlags.NonPublic | BindingFlags.Static);
		void Mutate(WorldSaveData w) => applyGenre.Invoke(null, new object[] {
			new Record { album = new Album { trackRefs = new[] { w.RetiredTrackArchive["retired"] } } }, Genre.TraditionalPop, Genre.Blues });
		Mutate(loaded);
		Check(loaded.Records[0].baseRecord.album.trackRefs[0].genre == Genre.Blues, "unrepaired graph loses real project genre mutation");
		loaded = Copy(world);
		TrackContinuation.Restore(loaded, loaded.ShadowContinuation.TrackAliases);
		Mutate(world); Mutate(loaded);
		foreach (var track in new[] { loaded.Records[0].baseRecord.album.trackRefs[0], loaded.Records[0].albumRuntime.baseAlbum.trackRefs[0],
			loaded.Competitor.AlbumProjects[0].albumRecord.album.trackRefs[0], loaded.Competitor.PendingAlbumProjects[0].albumRecord.album.trackRefs[0] }) {
			Check(ReferenceEquals(track, loaded.RetiredTrackArchive["retired"]), "restores archive/runtime/project identity");
			Check(track.genre == Genre.TraditionalPop, "project mutation propagates through shared graph");
		}
		Check(!ReferenceEquals(loaded.Records[0].baseRecord.album.nonSingleTracks[0], loaded.RetiredTrackArchive["retired"]), "same source ID does not merge independent snapshots");
		Check(JsonSerializer.Serialize(world, SaveGameService.TestJsonOptions) == JsonSerializer.Serialize(loaded, SaveGameService.TestJsonOptions), "repaired mutation matches uninterrupted serialization");
		Check(JsonSerializer.Serialize(TrackContinuation.Capture(loaded)) == JsonSerializer.Serialize(loaded.ShadowContinuation.TrackAliases), "alias capture stable after restore");
		TrackContinuation.Restore(loaded, null);
		Check(loaded.RetiredTrackArchive["retired"].genre == Genre.TraditionalPop, "older checkpoint without aliases supported");
		bool rejected = false;
		try { TrackContinuation.Restore(loaded, new() { new() { "missing", "also-missing" } }); } catch (InvalidOperationException) { rejected = true; }
		Check(rejected, "invalid graph rejected");

		float LegacySum(IEnumerable<float> values) { float sum = 0; foreach (float value in values) sum += value; return sum; }
		var region = new FollowingRegion { Young = new() { [0] = 0, [2] = 4, [3] = 4 } };
		region.Young.Remove(0);
		var restoredRegion = Copy(region);
		region.Young[1] = 100000000; restoredRegion.Young[1] = 100000000;
		Check(LegacySum(region.Young.Values) != LegacySum(restoredRegion.Young.Values), "reproduces cohort free-slot rounding drift");
		Check(region.YoungTotal() == restoredRegion.YoungTotal(), "cohort totals independent of free-slot history");
		var following = new MediaFollowingState { Regions = new(StringComparer.Ordinal) {
			["removed"] = new(), ["b"] = new() { Adult = 4, Young = new() { [1] = 4 } }, ["c"] = new() { Adult = 4, Young = new() { [1] = 4 } } } };
		following.Regions.Remove("removed");
		var restoredFollowing = Copy(following);
		following.Regions["a"] = new() { Adult = 100000000, Young = new() { [1] = 100000000 } };
		restoredFollowing.Regions["a"] = Copy(following.Regions["a"]);
		Check(LegacySum(following.Regions.Values.Select(r => r.Adult)) != LegacySum(restoredFollowing.Regions.Values.Select(r => r.Adult)), "reproduces regional free-slot rounding drift");
		Check(following.YoungTotal() == restoredFollowing.YoungTotal() && following.AdultTotal() == restoredFollowing.AdultTotal(), "national totals stable after remove/reinsert");
		Check(ProgrammeFollowUpScheduling.AudienceOrders(following) == ProgrammeFollowUpScheduling.AudienceOrders(restoredFollowing), "programme audience orders identical");
		var roomWorld = new WorldSaveData();
		LocalSceneRoomService.CaptureWorld(roomWorld);
		var originalBacking = LocalSceneRoomService.CaptureBackingContinuation();
		try {
			var rooms = new SceneRoomSaveData { Work = new() {
				new() { ArtistId = "a", PersonId = "backer", Year = 1960, Backing = true, FeeShare = 100000000, StageHours = 4 },
				new() { ArtistId = "b", PersonId = "backer", Year = 1960, Backing = true, FeeShare = 8, StageHours = 8 }
			} };
			LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
			float regrouped = LocalSceneRoomService.Backing("backer", 1960).Pay;
			LocalSceneRoomService.RestoreBackingContinuation(new());
			var addBacking = typeof(LocalSceneRoomService).GetMethod("AddBacking", BindingFlags.NonPublic | BindingFlags.Static);
			foreach (float pay in new[] { 100000000f, 4f, 4f }) addBacking.Invoke(null, new object[] { "backer", 1960, 4f, pay });
			var uninterrupted = LocalSceneRoomService.Backing("backer", 1960);
			Check(uninterrupted.Pay != regrouped, "reproduces event-versus-account backing-pay rounding drift");
			var backingCheckpoint = Copy(LocalSceneRoomService.CaptureBackingContinuation());
			LocalSceneRoomService.BeginRestore(Copy(rooms), backingCheckpoint);
			Check(LocalSceneRoomService.Backing("backer", 1960).Pay == 0, "deferred restore waits for room reindex");
			LocalSceneRoomService.CompleteRestore();
			Check(LocalSceneRoomService.Backing("backer", 1960) == uninterrupted, "restores exact backing hours and pay after reindex");
			Check(JsonSerializer.Serialize(LocalSceneRoomService.CaptureBackingContinuation()) == JsonSerializer.Serialize(backingCheckpoint), "backing continuation recapture identical");
			LocalSceneRoomService.BeginRestore(new SceneRoomSaveData(), new()); LocalSceneRoomService.CancelRestore();
			Check(LocalSceneRoomService.Backing("backer", 1960) == uninterrupted, "cancel restores prior exact backing cache");
			var baselinePerson = new Musician { wealth = 100, ego = .3f };
			var restoredPerson = Copy(baselinePerson);
			MemberWealthService.OnPooledYear(baselinePerson, uninterrupted.Pay);
			MemberWealthService.OnPooledYear(restoredPerson, LocalSceneRoomService.Backing("backer", 1960).Pay);
			Check(baselinePerson.lastYearIncome == SceneLiveEconomics.LiveSavingsShare * uninterrupted.Pay, "fixture exercises active annual wealth writer");
			Check(baselinePerson.wealth == restoredPerson.wealth && baselinePerson.lastYearIncome == restoredPerson.lastYearIncome, "annual pooled wealth matches with exact backing pay");
			LocalSceneRoomService.BeginRestore(rooms); LocalSceneRoomService.CompleteRestore();
			Check(LocalSceneRoomService.Backing("backer", 1960).Pay == regrouped, "older checkpoints keep account-based reconstruction");
		} finally {
			LocalSceneRoomService.BeginRestore(roomWorld.SceneRooms, originalBacking); LocalSceneRoomService.CompleteRestore();
		}
		GD.Print($"STATURE_CONTINUATION_CHECKS_PASS checks={checks}");
	}
}
