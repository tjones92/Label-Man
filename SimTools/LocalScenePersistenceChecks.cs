using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Isolated headless fixtures exercise real owners/player save seams; never ordinary startup.</summary>
public static class LocalScenePersistenceChecks {
    public static void Run() {
        if (!LocalScenes.Persisting) throw new InvalidOperationException("Use --enable-persistent-scenes for the persistence checks.");
        var am = ArtistManager.Instance;
        var desk = PlayerDesk.Instance;
        var opts = SaveGameService.TestJsonOptions;
        int checks = 0;
        void Check(bool ok, string detail) { if (!ok) throw new InvalidOperationException("SCENE_PERSISTENCE_CHECK_FAILED: " + detail); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, opts);
        int week = ChartManager.Instance.GetCurrentChartWeek();
        GameDate date = TimeManager.Instance.CurrentDate;
        var origin = WorldStateService.Capture();
        SimulatedArtist Fixture(string id, string place, ProspectMarketStatus market = ProspectMarketStatus.Seeking) {
            var a = new SimulatedArtist { artistId = id, stageName = id, type = ArtistType.Band, homeRegion = "Deep South", primaryGenre = Genre.Country,
                secondaryGenre = Genre.Folk, formedYear = date.year, prospectMarketStatus = market,
                geography = new GeographicIdentity { originPlaceId = place, originEvidence = PlaceEvidence.Explicit,
                    basePlaceId = place, baseEvidence = PlaceEvidence.Explicit },
                members = { new Musician { personId = id + "-person", isActive = true, isLeadVocalist = true, birthYear = date.year - 23, lifeState = MemberLifeState.Active,
                    technicalSkill = .5f, creativity = .5f } } };
            am.RestoreArtist(a); am.AdoptScoutingDiscovery(a.artistId);
            a.sceneParticipations.Single(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue).performanceLevel = ScenePerformanceLevel.Working;
            return a;
        }
        // Named fixtures are explicit test objects, not production births or counter draws.
        var seek = Fixture("scene-fixture-seeking", "nashville");
        var latent = Fixture("scene-fixture-latent", "nashville", ProspectMarketStatus.Latent);
        var signed = Fixture("scene-fixture-signed", "nashville");
        var guest = Fixture("scene-fixture-guest", "memphis");
        var ended = Fixture("scene-fixture-ended", "nashville");
        var satellite = Fixture("scene-fixture-oakland", "oakland");
        Check(satellite.geography.basePlaceId == "oakland" && LocalScenePersistenceService.MembersOf("san_francisco").Contains(satellite.artistId), "satellite identity and catchment membership distinct");
        string membersBefore = Json(latent.members);
        Check(am.GetUnsignedArtists().Contains(seek) && !am.GetUnsignedArtists().Contains(latent), "adoption uses existing activation/search eligibility");
        Check(latent.prospectMarketStatus == ProspectMarketStatus.Latent && Json(latent.members) == membersBefore, "adoption neither activates nor rerolls people");
        Check(LocalScenePersistenceService.MembersOf("nashville").Contains(latent.artistId), "latent deal seeker remains a performer/resident");
        Check(!am.RemoveUnsignedArtist(seek.artistId), "slate cleanup cannot destroy persistent act");
        am.AdoptScoutingDiscovery(seek.artistId);
        Check(am.GetEnabledDuplicatePoolEntries() == 0 && am.GetMusician(seek.members[0].personId) == seek.members[0], "adoption unique and person reference canonical");
        var owner = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned);
        owner.SignArtist(signed, date.year);
        am.SignArtist(signed, owner.labelId, date.year);
        Check(LocalScenePersistenceService.MembersOf("nashville").Contains(signed.artistId), "signing preserves residence");
        Check(!am.IsEligibleForPopulationSigning(signed, week), "signed resident not eligible for another exclusive contract");
        string guestOrigin = guest.geography.originPlaceId;
        Check(LocalScenePersistenceService.RecordGuestPresence(guest.artistId, ScenePlaceRegistry.Get("nashville"), week, week + 1,
            SceneRelationship.TouringGuest, "Fixed probe of owner-supplied tour presence"), "dated guest presence accepted for existing act");
        Check(LocalScenePersistenceService.MembersOf("nashville").Contains(guest.artistId) && guest.geography.basePlaceId == "memphis" &&
            guest.geography.originPlaceId == guestOrigin, "guest presence never relocates birthplace or working base");
        LocalScenePersistenceService.RecordGuestPresence(guest.artistId, ScenePlaceRegistry.Get("nashville"), week, week + 1,
            SceneRelationship.TouringGuest, "Fixed probe of owner-supplied tour presence");
        Check(guest.sceneParticipations.Count(p => p.relationship == SceneRelationship.TouringGuest) == 1, "guest replay idempotent");
        Check(!LocalScenePersistenceService.RecordGuestPresence(guest.artistId, ScenePlaceRegistry.Get("miami"), week, week + 1,
            SceneRelationship.TouringGuest, "Conflicting tour probe"), "overlapping distant presence rejected");
        Check(!LocalScenePersistenceService.PreviewCast("memphis", PlayerDesk.ScoutingVenue.HonkyTonks, week, date.year).Contains(guest.artistId),
            "known away guest excluded from home performance cast");
        Check(!LocalScenePersistenceService.RecordGuestPresence("nonexistent-act", ScenePlaceRegistry.Get("nashville"), week, week + 1,
            SceneRelationship.TouringGuest, "probe"), "presence never materializes missing act");
        am.EndActForBandLife(ended, date.year, "Fixed probe terminal exit", group: true);
        Check(am.GetArtist(ended.artistId) == ended && !LocalScenePersistenceService.MembersOf("nashville").Contains(ended.artistId) &&
            ended.sceneParticipations.All(p => p.endWeek.HasValue), "terminal act retains archive and leaves active cast");
        Check(!LocalScenePersistenceService.PreviewCast("nashville", PlayerDesk.ScoutingVenue.HonkyTonks, week + 2, date.year).Contains(guest.artistId), "expired tour presence excluded");
        string originId = seek.geography.originPlaceId;
        seek.geography.basePlaceId = "miami";
        LocalScenePersistenceService.ObserveArtist(seek);
        Check(!LocalScenePersistenceService.MembersOf("nashville").Contains(seek.artistId) && LocalScenePersistenceService.MembersOf("miami").Contains(seek.artistId) &&
            seek.geography.originPlaceId == originId && seek.sceneParticipations.Count(p => p.relationship == SceneRelationship.Resident && !p.endWeek.HasValue) == 1,
            "existing base change moves membership once, preserves hometown/history");
        seek.geography.basePlaceId = "nashville"; LocalScenePersistenceService.ObserveArtist(seek);
        Check(desk.FoundLabel("Scene Probe Records", "nashville", out _), "real player label founded");
        // Both observations remain inside one date/week: no simulated-time population tick is hidden in this test.
        TimeManager.Instance.RestoreClock(date, 12);
        var beforeScout = WorldStateService.Capture();
        var idsBefore = am.GetAllArtists().Select(a => a.artistId).OrderBy(id => id).ToArray();
        Check(desk.ScoutVenue(PlayerDesk.ScoutingVenue.HonkyTonks, out _), "first real scouting visit");
        string slate1 = string.Join('|', desk.Slate.Select(p => p.Artist.artistId));
        string sets1 = Json(desk.Slate.Select(p => p.LiveSet).ToList());
        Check(desk.Slate.Count > 0, "persistent local cast discoverable");
        TimeManager.Instance.RestoreClock(date, 12);
        GD.Seed(771122);
        float expectedNextDraw = GD.Randf();
        GD.Seed(771122);
        Check(desk.ScoutVenue(PlayerDesk.ScoutingVenue.HonkyTonks, out _), "second real scouting visit");
        Check(GD.Randf() == expectedNextDraw, "scouting observations leave the global RNG stream untouched");
        Check(slate1 == string.Join('|', desk.Slate.Select(p => p.Artist.artistId)) && sets1 == Json(desk.Slate.Select(p => p.LiveSet).ToList()),
            "repeat visit preserves cast IDs and read repertoire");
        Check(idsBefore.SequenceEqual(am.GetAllArtists().Select(a => a.artistId).OrderBy(id => id)), "visits never create/delete population");
        var afterScout = WorldStateService.Capture();
        Check(beforeScout.ArtistIdCounter == afterScout.ArtistIdCounter && beforeScout.MusicianIdCounter == afterScout.MusicianIdCounter &&
            beforeScout.PopulationRngState == afterScout.PopulationRngState && beforeScout.BandLife.FormationDebt == afterScout.BandLife.FormationDebt,
            "visits leave birth counters, population RNG and formation debt unchanged");
        Check(desk.SceneDiscoveries.Count == desk.Slate.Count && desk.SceneDiscoveries.All(d => am.GetArtist(d.ArtistId) != null), "knowledge references existing IDs and repeat encounter deduplicates");
        var noted = desk.Slate.First();
        Check(desk.AddToNotebook(noted, out _), "notebook records persistent discovery");
        Check(desk.RemoveFromNotebook(noted.Artist.artistId) && am.GetArtist(noted.Artist.artistId) == noted.Artist, "removing notebook entry never deletes resident");
        var latentCard = new PlayerDesk.Prospect { Artist = latent, FollowedUp = true };
        Check(!desk.ApproachToSign(latentCard, out _), "hearing latent performer does not unlock contract search");
        var stale = new PlayerDesk.Prospect { Artist = new SimulatedArtist { artistId = seek.artistId }, FollowedUp = true };
        Check(!desk.ApproachToSign(stale, out _), "stale noncanonical discovery rejected");
        var signedCard = new PlayerDesk.Prospect { Artist = signed, FollowedUp = true };
        Check(!desk.ApproachToSign(signedCard, out _) && signed.labelId == owner.labelId, "rival ownership cannot be overwritten from observed cast");
        Check(!PlayerDesk.SceneDealAvailable(latentCard) && PlayerDesk.SceneDealDescription(latentCard).Contains("not looking"), "latent card shows deal unavailable rather than a price");
        Check(!PlayerDesk.SceneDealAvailable(signedCard) && PlayerDesk.SceneDealDescription(signedCard).Contains("under contract"), "signed card explains unavailable contract");
        int hourBeforeStale = TimeManager.Instance.CurrentHour;
        float moneyBeforeStale = desk.Label.cashReserves;
        stale.HasBaseline = true;
        Check(!desk.OfferContract(stale, 0, .05f, 1, 1, true, false, out _) &&
            TimeManager.Instance.CurrentHour == hourBeforeStale && desk.Label.cashReserves == moneyBeforeStale, "stale offer rejected before time/payment");
        bool futureRejected = false;
        try { WorldStateService.Apply(new WorldSaveData { ScenePersistence = new ScenePersistenceSaveData { SchemaVersion = 99 } }, date, SimulationSeedBootstrap.RequestedSeed); }
        catch (InvalidOperationException) { futureRejected = true; }
        Check(futureRejected && idsBefore.SequenceEqual(am.GetAllArtists().Select(a => a.artistId).OrderBy(id => id)), "future persistence schema rejected before world mutation");
        string slot = "Scene probe " + Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot, out _), "real gzip world/player save");
            string labelGeo = Json(desk.Label.geography);
            string discoveries = Json(desk.SceneDiscoveries);
            LocalScenes.Configure(new[] { "--disable-local-scenes" });
            Check(SaveGameService.Load(slot, out _), "real player load succeeds with scenes disabled");
            Check(Json(desk.Label.geography) == labelGeo && Json(desk.SceneDiscoveries) == discoveries, "off-after-on preserves label identity and discovery ledger");
            Check(am.GetAllArtists().Any(a => a.sceneParticipations?.Count > 0), "disabled load preserves resident histories");
            LocalScenes.Configure(new[] { "--enable-persistent-scenes" });
            LocalSceneIdentityService.CompleteDirectPlayerRestore();
            Check(SaveGameService.Load(slot, out _), "real player load succeeds with scenes enabled");
            TimeManager.Instance.RestoreClock(date, 12);
            Check(desk.ScoutVenue(PlayerDesk.ScoutingVenue.HonkyTonks, out _), "post-load scouting visit");
            Check(slate1 == string.Join('|', desk.Slate.Select(p => p.Artist.artistId)), "save/reload preserves current cast");
            Check(desk.Slate.All(p => ReferenceEquals(am.GetArtist(p.Artist.artistId), p.Artist)), "loaded discovery resolves canonical actor");
            var world = WorldStateService.Capture();
            var restored = JsonSerializer.Deserialize<WorldSaveData>(Json(world), opts);
            string a = Json(world);
            WorldStateService.Apply(restored, date, SimulationSeedBootstrap.RequestedSeed);
            Check(Json(WorldStateService.Capture()) == a, "persistent world round trip is byte-identical");
        } finally { SaveGameService.Delete(slot); }
        Check(WorldStateService.Capture().ArtistIdCounter == origin.ArtistIdCounter, "fixtures did not use production birth counter");
        GD.Print($"SCENE_PERSISTENCE_CHECK_PASS checks={checks}");
    }
}
