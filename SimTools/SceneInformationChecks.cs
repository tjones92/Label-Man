using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SceneInformationChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("SCENE_INFORMATION_CHECK_FAILED: " + reason); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        LocalScenes.Configure(Array.Empty<string>());
        Check(LocalScenes.Institutions && LocalScenes.Relocation && LocalScenes.PriceFeedback && LocalScenes.AttentionFeedback, "all requested defaults");
        foreach (string off in new[] { "--disable-local-scenes", "--observe-local-scenes", "--disable-persistent-scenes", "--disable-scene-rooms", "--disable-scene-recruitment", "--disable-genre-market-v2", "--disable-artist-population-lifecycle" }) {
            LocalScenes.Configure(new[] { off });
            Check(!LocalScenes.Institutions && !LocalScenes.Relocation && !LocalScenes.PriceFeedback, "dependency off suppresses defaults: " + off);
        }
        LocalScenes.Configure(new[] { "--disable-scene-institutions" });
        Check(!LocalScenes.Institutions && !LocalScenes.Relocation && LocalScenes.PriceFeedback, "institution off suppresses relocation, keeps independent price");
        LocalScenes.Configure(new[] { "--disable-scene-relocation", "--disable-scene-price-feedback" });
        Check(LocalScenes.Institutions && !LocalScenes.Relocation && !LocalScenes.PriceFeedback, "independent off controls");
        LocalScenes.Configure(Array.Empty<string>());
        var row = new SceneRecruitmentRecord { ArtistId="probe", LabelId="rival", Week=10, Phase="DailyMarket", BasePlaceId="nashville" };
        var program = new SceneProgram { Id="program", RoomId="nashville:road", SponsorId="rival", StartDay=30, ThroughDay=121 };
        var move = new SceneFundedMove { Id="move", ArtistId="probe", FromPlaceId="memphis", ToPlaceId="nashville", ArrivalDay=40, Status=SceneMoveStatus.Planned };
        var room = SceneRoomCatalog.Get(program.RoomId);
        Check(room != null, "named-room fixture");
        Check(SceneInformationService.Project(null,77,new[] {row},new[] {program},new[] {move},id=>id,id=>id).Count==0,"unknown place cannot reveal a national feed");
        SceneNewsItem[] News(int day, SceneRecruitmentRecord[] rows=null) => SceneInformationService.Project("nashville",day, rows ?? new[] { row },new[] { program },new[] { move },id=>id,id=>id).ToArray();
        Check(!News(76).Any(n=>n.ArtistId == "probe"), "contract publication waits for event week; pending move hidden");
        Check(News(77).Count(n=>n.ArtistId == "probe") == 1, "committed contract reported after delay");
        Check(News(77,new[] {row,row}).Count(n=>n.ArtistId == "probe") == 1, "duplicate event suppression");
        row.Phase="Initialization";
        Check(!News(77).Any(n=>n.ArtistId == "probe"), "no invented opening news"); row.Phase="DailyMarket";
        Check(News(29).Length == 0 && News(30).Any(n=>n.Id.StartsWith("program-open")), "dated public program opening");
        Check(!News(121).Any(n=>n.Id.StartsWith("program-close")) && News(122).Any(n=>n.Id.StartsWith("program-close")), "dated expiry news");
        move.Status=SceneMoveStatus.Arrived;
        Check(!News(40).Any(n=>n.Id.StartsWith("arrival")) && News(41).Any(n=>n.Id.StartsWith("arrival")), "arrival, not intention, makes news");
        Check(News(1000).Length == 0, "bounded news history");
        var bill = new SceneBill { Id="fixture-bill", RoomId=room.Id, Day=5, StartHour=19, EndHour=23,
            Appearances=new() { new SceneAppearance { ArtistId="new-act", Status=SceneBillStatus.Scheduled } } };
        SceneLead Tip(string[] known, string[] heard, int day=1) => SceneInformationService.Lead(room,new[] {bill},known,heard,day,19);
        Check(Tip(Array.Empty<string>(),new[] {"one","one"}) == null, "same bill cannot farm familiarity");
        var lead=Tip(Array.Empty<string>(),new[] {"one","two"});
        Check(lead?.BillId == bill.Id && lead.ArtistId == "new-act" && lead.ExpiresDay == bill.Day, "tip is a partial pointer at a real dated bill");
        Check(Json(lead)==Json(Tip(Array.Empty<string>(),new[] {"one","two"})), "no reroll on repeated tip query");
        Check(Tip(new[] {"new-act"},new[] {"one","two"}) == null && Tip(Array.Empty<string>(),new[] {"one","two"},6)==null, "known acts and expired bills give no fresh lead");
        bill.Status=SceneBillStatus.Cancelled;
        Check(Tip(Array.Empty<string>(),new[] {"one","two"}) == null, "cancelled performance cannot produce a tip");
        bill.Status=SceneBillStatus.Scheduled; bill.Appearances[0].Status=SceneBillStatus.Cancelled;
        Check(Tip(Array.Empty<string>(),new[] {"one","two"}) == null, "cancelled act cannot produce a tip");
        string before=Json(WorldStateService.Capture()); GD.Seed(42); uint draw=GD.Randi(); GD.Seed(42);
        for(int i=0;i<3;i++) { News(77); Tip(Array.Empty<string>(),new[] {"one","two"}); }
        Check(GD.Randi()==draw && before==Json(WorldStateService.Capture()), "information reads preserve world and global RNG");
        var desk=PlayerDesk.Instance;
        Check(desk.FoundLabel("Information Checks", "nashville",out string message), "player fixture: " + message);
        var save=desk.CaptureState();
        save.SceneInformation = new() { Contacts=new() { new() { ContactId=room.ContactId, HeardBillIds=new() { "one","two" } } }, Leads=new() { lead } };
        Check(desk.RestoreState(save,out message), "player knowledge restore: " + message);
        Check(desk.SceneContactFamiliarity(room.ContactId)==2, "contact familiarity restored");
        Check(Json(save.SceneInformation)==Json(desk.CaptureState().SceneInformation), "rumor evidence, source, dates and read state roundtrip");
        TimeManager.Instance.RestoreClock(GameDate.StartDate, room.OpenHour);
        int hour = TimeManager.Instance.CurrentHour;
        var bills = LocalSceneRoomService.Calendar("nashville", GameDate.StartDate);
        var realTip = SceneInformationService.Lead(room,bills,Array.Empty<string>(),new[] {"one","two"},0,hour);
        Check(realTip != null, "actual calendar has a reachable introduction");
        Check(desk.AskSceneBooker(room.Id,out message), "real booker action: " + message);
        Check(TimeManager.Instance.CurrentHour == hour+1 && desk.CaptureState().SceneInformation.Leads.Any(l=>l.Id==realTip.Id), "conversation spends one hour and persists evidence");
        int afterHour = TimeManager.Instance.CurrentHour;
        Check(!desk.AskSceneBooker(room.Id,out _) && TimeManager.Instance.CurrentHour == afterHour, "same introduction cannot farm time or leads");
        Check(desk.SceneLeadNotes().Count > 0, "persisted tip is visible with verification status");
        var carrier=ArtistManager.Instance.GetAllArtists().First();
        carrier.sceneRecruitmentHistory ??= new();
        carrier.sceneRecruitmentHistory.AddRange(Enumerable.Range(0,12).Select(i=>new SceneRecruitmentRecord {
            ArtistId=carrier.artistId, LabelId="news-label-"+i, Week=0, Phase="DailyMarket", BasePlaceId="nashville" }));
        TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(8),19);
        Check(desk.LocalSceneNews().Count==8, "bounded recap batch");
        var shown=desk.LocalSceneNews().Select(n=>n.Id).ToArray();
        desk.MarkSceneNewsRead();
        Check(desk.LocalSceneNews().Count>=4 && !desk.LocalSceneNews().Any(n=>shown.Contains(n.Id)), "read marker preserves unseen reports in same-day backlog");
        var savedRead=desk.CaptureState();
        Check(desk.RestoreState(savedRead,out _) && !desk.LocalSceneNews().Any(n=>shown.Contains(n.Id)), "read state survives player reload");
        var heardRoom=SceneRoomCatalog.ForPlace("nashville").First(r=>r.IsPerformance);
        LocalSceneRoomService.EnsureCalendar(LocalSceneRoomService.Date(9));
        var heardBill=LocalSceneRoomService.Calendar("nashville",LocalSceneRoomService.Date(9)).First(b=>b.RoomId==heardRoom.Id && b.Status==SceneBillStatus.Scheduled && b.Appearances.Count>0);
        TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(heardBill.Day),heardBill.StartHour);
        Check(desk.ScoutSceneRoom(heardRoom.Id,out message),"actual heard bill builds contact: " + message);
        Check(desk.SceneContactFamiliarity(heardRoom.ContactId)==(heardRoom.ContactId==room.ContactId ? 3 : 1),"familiarity earned through listening");
        int familiarity = desk.SceneContactFamiliarity(room.ContactId);
        var detached=desk.CaptureState(); detached.SceneInformation.Contacts.Clear();
        Check(desk.SceneContactFamiliarity(room.ContactId)==familiarity, "save capture detaches knowledge");
        save.SceneInformation=null;
        Check(desk.RestoreState(save,out _) && desk.SceneContactFamiliarity(room.ContactId)==0, "old save invents no familiarity or leads");
        GD.Print($"SCENE_INFORMATION_CHECK_PASS checks={checks}");
    }
}
