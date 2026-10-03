using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class PolarSongBehaviorChecks {
	public static void Run() {
		bool enabled = PolarSongBehavior.UsePolarFitSelection;
		var masters = PolarSongMetadataService.Capture();
		var tastes = PolarSongBehavior.CaptureTaste();
		try {
			Check(!new CompositionSaveData().UsePolarFitSelection, "old/absent flag defaults off");
			var artist = new SimulatedArtist { artistId = "polar-probe-act", primaryGenre = Genre.RockAndRoll,
				type = ArtistType.Band, musicianship = .45f, vocalPower = .45f, studioPerformance = .3f, groupCohesion = .35f,
				members = new() { new Musician { isActive = true, isLeadVocalist = true, technicalSkill = .2f, musicalVersatility = .2f, reliability = .5f } } };
			var playable = Song("polar-probe-playable", SongArchetype.BrightPopNumber, Genre.RockAndRoll, .2f);
			var lush = Song("polar-probe-lush", SongArchetype.LushStandard, Genre.TraditionalPop, 1f);
			// An extreme craft advantage cannot overcome unsupported vocal/ensemble demands.
			Check(PolarSongBehavior.SuitableSongs(new[] { lush, playable, playable }, artist, 1960).First() == playable, "act suitability wins over global craft");
			Check(PolarSongBehavior.SuitableSongs(new[] { lush, playable, playable }, artist, 1960).Count() == 2, "live-set candidate deduplication");
			var record = new Record { recordId = "polar-probe-master", primaryGenre = Genre.RockAndRoll, format = ReleaseFormat.Single,
				artistId = artist.artistId, hookStrength = .7f, productionQuality = .8f, originality = .6f };
			var reference = new Record { recordId = "polar-probe-reference", format = ReleaseFormat.Single, primaryGenre = Genre.TraditionalPop };
			PolarSongBehavior.UsePolarFitSelection = true;
			PolarSongMetadataService.RegisterRecord(reference, lush, new GameDate(1959, 1, 1));
			var referenceMaster = PolarSongMetadataService.Get(reference.masterId);
			string before = JsonSerializer.Serialize(referenceMaster, SaveGameService.TestJsonOptions);
			PolarSongBehavior.UsePolarFitSelection = false;
			var legacy = SongMaterialSelectionService.BuildCoverForSong(artist, record, lush, Genre.RockAndRoll, 1960, reference.masterId);
			Check(legacy.PolarProposal == null, "disabled player cover retains legacy contract");
			PolarSongBehavior.UsePolarFitSelection = true;
			var liveSet = new PlayerDesk.Prospect { Artist = artist };
			PlayerDesk.Instance.BuildLiveSet(liveSet, artist, 1960, 0);
			Check(liveSet.LiveSet.Count >= 3 && liveSet.LiveSet.Count <= 5 && liveSet.LiveSet.Select(s => s.SongId).Distinct().Count() == liveSet.LiveSet.Count,
				"actual player live set retains length and distinct covers");
			var material = SongMaterialSelectionService.BuildCoverForSong(artist, record, lush, Genre.RockAndRoll, 1960, reference.masterId);
			Check(material.PolarProposal.parentRecordingId == reference.masterId && material.PolarProposal.songId == lush.songId, "explicit heard reference carried through proposal");
			var demo = SongMaterialSelectionService.BuildCoverForSong(artist, record, lush, Genre.RockAndRoll, 1960, "demo:" + lush.songId);
			Check(demo.PolarProposal.parentRecordingId == null, "explicit demo stays demo despite later masters");
			SongMaterialApplicationService.Apply(record, material, null, artist);
			var committed = PolarSongMetadataService.Get(record.masterId);
			Check(committed.parentRecordingId == reference.masterId && committed.realizedFit != null, "realized fit and lineage committed");
			Check(before == JsonSerializer.Serialize(referenceMaster, SaveGameService.TestJsonOptions), "cover commit leaves prior master untouched");
			Check(record.songwriterNames.SequenceEqual(new[] { "Original Writer" }) && record.publishingControllerArtistId == "original-controller", "composition credits and rights retained");
			Check(record.hookStrength <= lush.commercialHook && record.productionQuality < .8f, "unsupported demands affect execution");
			Check(record.arrangementOriginality == material.PolarProposal.fit.Stretch, "arrangement originality uses realized movement");
			committed.realizedFit.identity = 0;
			float criticized = ArtisticMeritService.Evaluate(record, .8f);
			committed.realizedFit.identity = 1;
			Check(ArtisticMeritService.Evaluate(record, .8f) > criticized, "identity reaches critical merit independently");
			committed.realizedFit.moment = 1; committed.realizedFit.hasMarketEvidence = false;
			Check(PolarSongBehavior.MomentMultiplier(record) == 1, "missing market evidence is neutral");
			committed.realizedFit.hasMarketEvidence = true;
			Check(PolarSongBehavior.MomentMultiplier(record) > 1, "moment reaches conversion");
			float craft = ArtisticMeritService.Evaluate(record, .8f);
			committed.realizedFit.moment = 0;
			Check(PolarSongBehavior.MomentMultiplier(record) < 1 && ArtisticMeritService.Evaluate(record, .8f) == craft, "market does not leak into critical merit");
			PolarSongBehavior.UsePolarFitSelection = false;
			Check(PolarSongBehavior.MomentMultiplier(record) == 1 && PolarSongBehavior.CriticCraft(.8f, record) == .8f, "boundary disables downstream consumers even for saved polar masters");
			var snapshot = new CompositionSaveData { UsePolarFitSelection = true, PolarMasters = PolarSongMetadataService.Capture(),
				PolarTaste = new() { [Genre.RockAndRoll] = new MarketTasteSnapshot { asOfWeek = 4, observations = 3 } } };
			var restored = JsonSerializer.Deserialize<CompositionSaveData>(JsonSerializer.Serialize(snapshot, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			Check(restored.UsePolarFitSelection && restored.PolarTaste[Genre.RockAndRoll].asOfWeek == 4 &&
				restored.PolarMasters[record.masterId].realizedFit.moment == 0, "flag/taste/explicit zero fit persist");
			var repertoire = new PlayerDesk.RepertoireItem { SongId = lush.songId, ReferenceMasterId = reference.masterId };
			Check(RepertoireSaveData.From(repertoire).ToItem().ReferenceMasterId == reference.masterId, "heard version persists in player repertoire");
			GD.Print("POLAR_SONG_BEHAVIOR_PASS selection=ok liveSet=ok lineage=ok execution=ok critic=ok moment=ok disabled=ok persistence=ok");
		} finally {
			PolarSongBehavior.UsePolarFitSelection = enabled;
			PolarSongMetadataService.Restore(masters);
			PolarSongBehavior.RestoreTaste(tastes);
		}
	}
	private static SongComposition Song(string id, SongArchetype archetype, Genre genre, float craft) => new() {
		songId = id, title = id, primaryGenre = genre, originYear = 1959, compositionQuality = craft, commercialHook = craft,
		lyricQuality = craft, melodicStrength = craft, isStandard = true, plasticity = 0, plasticityFrozen = true,
		demoTaxonomy = new SongTaxonomy { archetype = archetype, primaryGenre = genre },
		credits = new() { new SongwriterCredit { writerName = "Original Writer", share = 1 } },
		rights = new SongRightsProfile { controllerArtistId = "original-controller", controlType = PublishingControlType.ArtistControlled }
	};
	private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("POLAR_SONG_BEHAVIOR_FAIL: " + message); }
}
