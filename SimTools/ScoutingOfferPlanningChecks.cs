using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ScoutingOfferPlanningChecks {
    // Real bill, actual listening and invitation. Controlled eligibility isolates this resource forecast.
    public static SceneConversation PrepareDemo(PlayerDesk desk) {
        var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        SceneBill bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, clock.CurrentDate).First(b => {
            if (b.Status != SceneBillStatus.Scheduled || b.Appearances.Count == 0) return false;
            var a = b.Appearances[0]; var room = SceneRoomCatalog.Get(b.RoomId);
            if (a.Set.Count == 0 || a.EndHour * 60 + PlayerDesk.ScoutingInvitationMinutes > Math.Min(clock.HardStopHour, room.CloseHour) * 60) return false;
            clock.RestoreClock(LocalSceneRoomService.Date(b.Day), a.EndHour);
            var act = am.GetArtist(a.ArtistId);
            var leader = act.members.FirstOrDefault(m => m.isActive && m.lifeState == MemberLifeState.Active &&
                (m.isBandLeader || act.members.Count == 1) && a.PersonIds.Contains(m.personId) &&
                !LocalSceneRoomService.HasPersonBooking(m.personId, b.Day, a.EndHour * 60, a.EndHour * 60 + 15, room.Id));
            return leader != null && LocalSceneRoomService.QuoteConversation(desk.Label, a.ArtistId, leader.personId,
                room.Id, ScenePerformanceService.EventId(b, a), ScoutingAppointmentActivity.Demo) != null;
        });
        var slot = bill.Appearances[0];
        clock.RestoreClock(LocalSceneRoomService.Date(bill.Day), slot.StartHour);
        if (!desk.ListenSceneAppearance(bill.Id, ScenePerformanceService.EventId(bill, slot), out string message)) throw new InvalidOperationException(message);
        var artist = am.GetArtist(slot.ArtistId);
        artist.labelId = null; artist.careerState = CareerState.Unsigned; artist.lifecycleStatus = ArtistLifecycleStatus.Active;
        artist.prospectMarketStatus = ProspectMarketStatus.Seeking; artist.manager = ManagerArchetype.None;
        var quote = desk.PreviewScoutingInvitation(artist.artistId, ScoutingAppointmentActivity.Demo);
        if (quote == null || !desk.ArrangeScoutingMeeting(artist.artistId, quote.Id, out message, ScoutingAppointmentActivity.Demo)) throw new InvalidOperationException(message);
        return desk.ScoutingMeetings.Single();
    }

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_OFFER_PLANNING_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
        Check(desk.PreviewOfferResources(0) == null, "no label yields no invented budget");
        Check(desk.FoundLabel("Offer Planning", "nashville", out string message), "found label: " + message);
        var booking = PrepareDemo(desk);
        float advance = 100, reserve = desk.Label.GetMonthlyOverhead() * 2;
        desk.Label.cashReserves = advance + reserve;
        Check(!desk.PreviewOfferResources(advance).AdvanceAffordable, "strict reserve boundary matches signing owner");
        desk.Label.cashReserves += 1;
        Check(desk.PreviewOfferResources(advance).AdvanceAffordable, "one dollar over reserve clears current cash rule");
        desk.Label.cashReserves = 10000;
        var plan = desk.PreviewOfferResources(advance);
        Check(plan.PendingAppointmentFees == booking.Fee && plan.PendingAppointmentCount == 1 && plan.FeeDetails.Single().Contains(booking.ArtistName), "real unpaid demo has named frozen fee");
        Check(plan.BudgetSessionCost == desk.SessionCost(PlayerDesk.StudioTier.Budget, PlayerDesk.MinSessionHours) &&
            plan.CashAfterFeesAndSession == desk.Label.cashReserves - advance - booking.Fee - plan.BudgetSessionCost, "combines existing quote and actual fee without treating either as paid");
        Check(plan.Summary.Contains("not a funded promise") && plan.Summary.Contains("distribution") && plan.Summary.Contains("current") &&
            !plan.Summary.Contains("reservation") && !plan.Summary.Contains("patience"), "forecast labels limits without hidden acceptance values");
        Check(desk.PreviewOfferResources(float.NaN) == null && desk.PreviewOfferResources(float.PositiveInfinity) == null &&
            desk.PreviewOfferResources(-1) == null && desk.PreviewOfferResources(0, meetingHours: -1) == null, "invalid forecasts cannot appear affordable");
        var baselineWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var baselinePlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var baselineDate = clock.CurrentDate; int hour = clock.CurrentHour, minute = clock.CurrentMinute;
        void Reset() {
            LocalScenes.Configure(Array.Empty<string>());
            WorldStateService.Apply(LocalSceneRoomService.Copy(baselineWorld), baselineDate, SimulationSeedBootstrap.RequestedSeed);
            Check(desk.RestoreState(LocalSceneRoomService.Copy(baselinePlayer), out message), "restore booked baseline");
            clock.RestoreClock(baselineDate, hour, minute);
        }
        string world = Json(WorldStateService.Capture()), player = Json(desk.CaptureState());
        GD.Seed(741); uint draw = GD.Randi(); GD.Seed(741);
        for (int i = 0; i < 8; i++) desk.PreviewOfferResources(advance + i, i % 2 == 0);
        Check(Json(WorldStateService.Capture()) == world && Json(desk.CaptureState()) == player && GD.Randi() == draw, "editing forecasts preserves all world/player state and RNG");
        var rooms = WorldStateService.Capture().SceneRooms;
        rooms.Conversations.Single().Fee += 5;
        Check(desk.PreviewOfferResources(advance).PendingAppointmentFees == booking.Fee, "external captures cannot rewrite forecast fees");
        desk.Label.maxRosterSize = desk.Label.CurrentRosterSize;
        Check(!desk.PreviewOfferResources(advance).RosterSpace && desk.PreviewOfferResources(advance, true).Summary.Contains("existing slot"), "full roster is distinguished from renewal slot");
        Reset();
        clock.RestoreClock(LocalSceneRoomService.Date(booking.Day), booking.StartMinute / 60 - 2, booking.StartMinute % 60);
        Check(desk.PreviewOfferResources(advance).AppointmentRisk == null, "meeting ending exactly at start is safe");
        clock.RestoreClock(clock.CurrentDate, clock.CurrentHour, clock.CurrentMinute + 1);
        Check(desk.PreviewOfferResources(advance).AppointmentRisk?.Contains(booking.ArtistName) == true, "passing start identifies actual obligation");
        clock.RestoreClock(clock.CurrentDate, clock.HardStopHour - 1);
        Check(!desk.PreviewOfferResources(advance).MeetingFits && desk.PreviewOfferResources(advance).AppointmentRisk == null, "blocked late meeting does not predict consumed time");
        Reset();
        foreach (var mode in new[] { "--disable-scouting-experience", "--observe-scouting-experience" }) {
            LocalScenes.Configure(new[] { mode });
            Check(desk.PreviewOfferResources(advance).PendingAppointmentFees == booking.Fee, "feature mode retains booked cost history: " + mode);
        }
        LocalScenes.Configure(Array.Empty<string>());
        string slotName = "Offer plan " + Guid.NewGuid().ToString("N");
        string forecast = Json(desk.PreviewOfferResources(advance));
        try {
            Check(SaveGameService.Save(slotName, out message) && SaveGameService.Load(slotName, out message), "real compressed save/load: " + message);
            Check(Json(desk.PreviewOfferResources(advance)) == forecast, "reload reproduces forecast without new persisted fields");
        } finally { SaveGameService.Delete(slotName); }
        float cash = desk.Label.cashReserves;
        Check(desk.CancelScoutingMeeting(booking.Id, out message) && desk.Label.cashReserves == cash && desk.PreviewOfferResources(advance).PendingAppointmentFees == 0, "actual free cancellation removes fee forecast");
        Reset(); desk.Label.cashReserves = booking.Fee - 1;
        Check(desk.PreviewOfferResources(0).CashAfterFeesAndSession < 0 && desk.PreviewOfferResources(0).Summary.Contains("exceeds current cash"), "combined-plan shortfall is explicit");
        Reset();
        Check(desk.WaitForScoutingMeeting(booking.Id, out message), "wait for real demo: " + message);
        cash = desk.Label.cashReserves;
        Check(desk.AttendScoutingMeeting(booking.Id, out message), "attend actual demo: " + message);
        Check(desk.Label.cashReserves == cash - booking.Fee && desk.PreviewOfferResources(advance).PendingAppointmentFees == 0 &&
            desk.ScoutingMeetings.Single().Paid, "paid completed demo is not deducted twice");
        GD.Print($"SCOUTING_OFFER_PLANNING_PASS checks={checks}");
    }
}
