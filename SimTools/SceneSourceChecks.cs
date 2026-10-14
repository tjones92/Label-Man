using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneSourceChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCENE_SOURCE_CHECK_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        Check(LocalScenes.ExtendedWorld, "source layer defaults enabled");
        foreach (string off in new[] { "--observe-local-scenes", "--disable-local-scenes", "--disable-scene-rooms", "--disable-scene-recruitment", "--disable-scene-extended-world" }) {
            LocalScenes.Configure(new[] { off }); Check(!LocalScenes.ExtendedWorld, "dependency/off isolation " + off);
        }
        bool rejected = false;
        try { LocalScenes.Configure(new[] { "--enable-scene-extended-world", "--disable-scene-extended-world" }); } catch (ArgumentException) { rejected = true; }
        Check(rejected && !LocalScenes.ExtendedWorld, "contradiction rejects before config mutation");
        LocalScenes.Configure(Array.Empty<string>());
        Check(SceneSourceService.Profiles.Count == 6 && SceneSourceService.Profiles.All(p => ScenePlaceRegistry.Get(p.PlaceId)?.CountryCode == "GB" && p.Source.Length > 0), "six evidenced source contexts");
        Check(SceneSourceService.Institutions.Count == 3 && SceneSourceService.Institutions.All(i => i.Fictional && ScenePlaceRegistry.Get(i.PlaceId)?.CountryCode == "US"), "specialist US exchanges");
        Check(ScenePlaceRegistry.All.Count(p => p.Id == p.PlayableCityId) == 31 && SceneSourceService.Profiles.All(p => ScenePlaceRegistry.Get(p.PlaceId).MarketRegionId == null), "no foreign sales/playable markets added");
        Check(!ScenePlaceRegistry.TryDomesticRoadMiles("gb_london", "new_york", out _), "Atlantic never becomes domestic drive");
        var placements = Enumerable.Range(0, 1000).Select(i => SceneSourceService.FormationPlace(1001, "source-" + i, Genre.BritishBeat)).ToArray();
        Check(placements.All(p => SceneSourceService.Profile(p) != null) && placements.Distinct().Count() == 6, "British supply has varied source origins");
        Check(placements.SequenceEqual(Enumerable.Range(0,1000).Reverse().Select(i => SceneSourceService.FormationPlace(1001,"source-"+i,Genre.BritishBeat)).Reverse()), "source placement stable/order independent");
        Check(Enumerable.Range(0,3000).Any(i => SceneSourceService.FormationPlace(1001,"early-"+i,Genre.Folk)?.StartsWith("gb_") == true), "early musical life independent of commercial British genre gates");
        Check(Enumerable.Range(0,3000).Any(i => SceneSourceService.FormationPlace(1001,"country-"+i,Genre.Country) == "bakersfield"), "specialist formation prior used");
        var major = new AILabel { labelId="source-major", tier=LabelTier.Major, nationalReach=.8f, scoutingAbility=.8f,
            geography=new GeographicIdentity { basePlaceId="new_york" } };
        SceneSourceService.EnsureConnections(major);
        Check(major.sceneBusinessConnections.Count == 3, "finite named major portfolio");
        string portfolio=Json(major.sceneBusinessConnections); SceneSourceService.EnsureConnections(major);
        Check(portfolio == Json(major.sceneBusinessConnections), "one-time connection seeding");
        var foreign = new SimulatedArtist { artistId="foreign-probe", primaryGenre=Genre.BritishBeat, homeRegion="West Coast",
            geography=new GeographicIdentity { originPlaceId="gb_london",basePlaceId="gb_london" } };
        var access=SceneRecruitmentService.Explain(major,foreign,0);
        Check(access.Eligible && access.Route=="ForeignAgency" && access.RoadMiles==null && access.Evidence.Contains("source-major"), "agency access has specific evidence without travel");
        Check(!SceneRecruitmentService.Explain(major,foreign,522).Eligible, "expired agency fails commit eligibility");
        var indie=new AILabel { labelId="source-indie",tier=LabelTier.Small,scoutingAbility=1,nationalReach=1, geography=new GeographicIdentity {basePlaceId="memphis"} };
        Check(!SceneRecruitmentService.Explain(indie,foreign,0).Eligible, "distribution/capability alone never grant foreign access");
        var uk=new AILabel {labelId="source-uk-us-line",tier=LabelTier.Major,nationalReach=.8f,scoutingAbility=.8f,
            geography=new GeographicIdentity {basePlaceId="gb_liverpool"}};
        SceneSourceService.EnsureConnections(uk);
        Check(SceneRecruitmentService.Explain(uk,foreign,0).Route=="SourceCircuit", "existing US-market foreign label uses explicit agency circuit");
        SceneRecruitmentService.RecordSigning(major,foreign,0,1960,"Probe");
        Check(foreign.sceneRecruitmentHistory.Single().AccessEvidence == access.Evidence && foreign.geography.basePlaceId=="gb_london" && foreign.homeRegion=="West Coast", "signing evidence and separate sales/origin/base");
        var legacy=new SimulatedArtist {artistId="legacy-british",primaryGenre=Genre.BritishBeat,homeRegion="East Coast"};
        LocalSceneIdentityService.EnsureArtist(legacy);
        Check(ScenePlaceRegistry.Get(legacy.geography.basePlaceId)?.CountryCode=="US" && legacy.geography.originPlaceId==null, "legacy genre does not manufacture foreign birthplace");
        var supplied=new SimulatedArtist {artistId="explicit-domestic",primaryGenre=Genre.BritishBeat,homeRegion="East Coast"};
        LocalSceneIdentityService.EnsureArtist(supplied,true,"new_york");
        Check(supplied.geography.originPlaceId=="new_york", "explicit/inherited formation wins over source prior");
        var ukPerson=new Musician {geography=new GeographicIdentity {basePlaceId="gb_london"}};
        var usPerson=new Musician {geography=new GeographicIdentity {basePlaceId="new_york"}};
        Check(!SceneSourceService.CanJoinFromPool(usPerson,foreign) && !SceneSourceService.CanJoinFromPool(ukPerson,supplied), "pool cannot teleport people across the Atlantic");
        Check(SceneSourceService.CanJoinFromPool(ukPerson,foreign) && SceneSourceService.CanJoinFromPool(usPerson,supplied), "same-country pool continuity");
        Check(!SceneSourceService.CanJoinFromPool(new Musician(),foreign), "unknown personal base cannot justify foreign relocation");
        LocalScenes.Configure(new[] {"--disable-scene-extended-world"});
        Check(!SceneSourceService.CanJoinFromPool(usPerson,foreign) && SceneSourceService.FormationPlace(1001,"off-source",Genre.BritishBeat)==null,
            "disabling source creation preserves country safety for existing foreign people");
        LocalScenes.Configure(Array.Empty<string>());
        usPerson.personId="source-pool-probe"; usPerson.lifeState=MemberLifeState.Active; usPerson.primaryRole=MusicianRole.Drums; usPerson.birthYear=1940;
        PersonPool.Add(new PooledPerson {person=usPerson,lastArtistId="old-source-probe",lastGenre=Genre.BritishBeat,homeRegion="West Coast"});
        Check(BandLifeService.PlayerHire(foreign,MusicianRole.Drums,false,false,usPerson.personId,null,1960)==null && PersonPool.Contains(usPerson.personId), "cross-country player hire rejects before consuming pool person");
        Check(BandLifeService.FindPoolReplacement(foreign,MusicianRole.Drums,false,1960)?.person.personId != usPerson.personId, "replacement service excludes ocean teleport");
        PersonPool.Take(usPerson.personId);
        var pool=Enumerable.Range(0,20).Select(i=>new SimulatedArtist {artistId="submission-"+i,geography=new GeographicIdentity {basePlaceId="gb_london"}}).ToArray();
        var world=WorldStateService.Capture(); uint draw;
        GD.Seed(456); draw=GD.Randi(); GD.Seed(456);
        var slate=SceneRecruitmentService.Slate(major,pool,0,0,4,out int accessible);
        Check(slate.Count==4 && accessible==20 && slate.Select(a=>a.artistId).SequenceEqual(SceneRecruitmentService.Slate(major,pool.Reverse(),0,0,4,out _).Select(a=>a.artistId)), "finite stable demo attention");
        Check(GD.Randi()==draw && WorldStateService.Capture().PopulationRngState==world.PopulationRngState, "queries preserve random streams");
        var desk=PlayerDesk.Instance;
        Check(desk.FoundLabel("Source Checks","new_york",out string message), "player US fixture: " + message);
        TimeManager.Instance.RestoreClock(GameDate.StartDate,9);
        float cash=desk.Label.cashReserves; int count=ArtistManager.Instance.GetAllArtists().Count();
        Check(desk.ArrangeSourceIntroduction("gb_london",out message), "paid introduction: " + message);
        Check(desk.Label.cashReserves==cash-150 && TimeManager.Instance.CurrentHour==11 && ArtistManager.Instance.GetAllArtists().Count()==count, "paid intro cost/time with no birth");
        Check(!desk.ArrangeSourceIntroduction("gb_london",out _) && desk.Label.cashReserves==cash-150 && TimeManager.Instance.CurrentHour==11, "duplicate intro costs nothing");
        var saved=desk.CaptureState(); string savedJson=Json(saved);
        var detached=LabelSaveData.From(desk.Label); detached.SceneBusinessConnections.Clear();
        Check(desk.Label.sceneBusinessConnections.Count==1, "player connections captured detached");
        Check(desk.RestoreState(JsonSerializer.Deserialize<PlayerSaveData>(savedJson,SaveGameService.TestJsonOptions),out message), "player source data reload: " + message);
        Check(SceneSourceService.Connection(desk.Label,"gb_london",0)?.Id.Contains("paid-source") == true, "paid evidence survives player restore");
        var candidate=ArtistManager.Instance.GetUnsignedArtists().FirstOrDefault(a=>a.geography?.basePlaceId=="gb_london");
        if(candidate==null) {
            // Controlled fixture: reuse a canonical, eligible act rather than asking scouting to create one.
            candidate=ArtistManager.Instance.GetUnsignedArtists().First();
            candidate.geography=new GeographicIdentity {originPlaceId="gb_london",basePlaceId="gb_london",
                originEvidence=PlaceEvidence.Simulated,baseEvidence=PlaceEvidence.Simulated,assignmentSource="source check fixture"};
            LocalSceneIdentityService.EnsureArtist(candidate);
        }
        var before=WorldStateService.Capture(); count=ArtistManager.Instance.GetAllArtists().Count();
        GD.Seed(918); draw=GD.Randi(); GD.Seed(918);
        Check(desk.ReviewSourceDemos("gb_london",out message), "actual owner-budgeted demos: " + message);
        Check(desk.Slate.Count<=4 && desk.Slate.All(p=>p.SourceConnectionId!=null && p.SceneBillId==null), "demo provenance without invented live bill");
        Check(GD.Randi()==draw && before.PopulationRngState==WorldStateService.Capture().PopulationRngState && count==ArtistManager.Instance.GetAllArtists().Count(), "demo review no RNG/births");
        var prospect=desk.Slate.First(); string connectionId=prospect.SourceConnectionId;
        Check(desk.AddToNotebook(prospect,out message), "notebook source provenance");
        saved=desk.CaptureState();
        Check(desk.RestoreState(JsonSerializer.Deserialize<PlayerSaveData>(Json(saved),SaveGameService.TestJsonOptions),out message) && desk.Notebook.Any(n=>n.SourceConnectionId==connectionId), "notebook/player serialization preserves intro identity");
        string slot="_scene_source_"+Guid.NewGuid().ToString("N");
        try {
            Check(SaveGameService.Save(slot,out message), "real gzip source save: " + message);
            desk.Label.sceneBusinessConnections.Clear();
            Check(SaveGameService.Load(slot,out message), "real gzip source load: " + message);
            Check(desk.Notebook.Any(n=>n.SourceConnectionId==connectionId) && SceneSourceService.Connection(desk.Label,"gb_london",0)?.Id==connectionId, "gzip keeps paid connection and notebook proof");
        } finally { SaveGameService.Delete(slot); }
        var canonicalNote=desk.Notebook.First(n=>n.SourceConnectionId==connectionId);
        var stale=new PlayerDesk.Prospect {Artist=canonicalNote.Artist,SourceConnectionId=connectionId};
        var liveConnection=desk.Label.sceneBusinessConnections.Single(c=>c.Id==connectionId);
        int oldEnd=liveConnection.ThroughWeek, hour=TimeManager.Instance.CurrentHour;
        liveConnection.ThroughWeek=-1;
        Check(!desk.FollowUp(stale,out _) && !desk.ApproachToSign(stale,out _) && TimeManager.Instance.CurrentHour==hour, "stale demo rejects follow-up/signing without time cost");
        liveConnection.ThroughWeek=oldEnd;
        string owner=stale.Artist.labelId; stale.Artist.labelId="rival-fixture";
        Check(!desk.FollowUp(stale,out _) && !desk.ApproachToSign(stale,out _), "rival ownership defeats cached source discovery");
        stale.Artist.labelId=owner;
        GD.Print($"SCENE_SOURCE_CHECK_PASS checks={checks} foreignActs={ArtistManager.Instance.GetAllArtists().Count(a=>ScenePlaceRegistry.Get(a.geography?.basePlaceId)?.CountryCode=="GB")}");
    }
}
