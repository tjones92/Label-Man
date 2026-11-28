using System.Linq;

public partial class PlayerDesk {
    internal const int AfterSetDrinkMinutes = 30, AfterSetDrinkDebt = 60;

    private (ScoutingObservation Heard, SceneRoomProfile Room, int Close, string Name) AfterSetDrinkQuote(
        string artistId, AfterSetCompanion companion) {
        if (!ShowsFatigue || !MeetingActionsEnabled || InStudio || scoutingKnowledge.Visit is not { } visit || visit.Day != SceneDay)
            return default;

        var heard = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId)?.Observations
            .LastOrDefault(o => o.Kind == ScoutingEvidenceKind.Performance && o.BillId == visit.BillId && o.AcquiredDay == SceneDay);
        if (heard?.AcquiredMinute is not int heardAt || heard.AfterSetDrink != null || !IsAtSceneRoom(heard.RoomId))
            return default;

        int now = NowMinute, end = now + AfterSetDrinkMinutes;
        var room = SceneRoomCatalog.Get(heard.RoomId);
        var bill = LocalSceneRoomService.Bill(visit.BillId);
        if (room?.IsPerformance != true || bill == null || now < heardAt || now > heardAt + 15)
            return default;
        int close = VenueStopMinute(room, bill);
        if (end > close) return default;

        if (companion == AfterSetCompanion.Booker) return (heard, room, close, room.ContactName);
        if (companion != AfterSetCompanion.Act) return default;
        var act = ArtistManager.Instance?.GetArtist(artistId);
        if (act?.lifecycleStatus != ArtistLifecycleStatus.Active || !act.members.Any(m =>
            m.isActive && m.lifeState == MemberLifeState.Active && heard.PersonIds.Contains(m.personId) &&
            !LocalSceneRoomService.IsOnStage(m.personId, SceneDay, now, end))) return default;
        return (heard, room, close, act.stageName);
    }

    internal string AfterSetDrinkCompanion(string artistId, AfterSetCompanion companion) =>
        AfterSetDrinkQuote(artistId, companion).Name;

    internal bool ShareAfterSetDrink(string artistId, AfterSetCompanion companion, out string message) {
        var (heard, room, close, name) = AfterSetDrinkQuote(artistId, companion);
        if (name == null || !TimeManager.Instance.SpendVenueMinutes(AfterSetDrinkMinutes, close)) {
            message = "There's no time for a drink together now.";
            return false;
        }

        heard.AfterSetDrink = companion;
        scoutingKnowledge.Visit.EndMinute = NowMinute;
        if (companion == AfterSetCompanion.Booker) {
            RememberBookerIntroduction(room);
            var contact = SceneContact(room);
            contact.SharedDrinks = System.Math.Min(16, contact.SharedDrinks + 1);
        }
        FatigueOwedMinutes = DebtAfterWork(NowMinute, NowMinute, AfterSetDrinkDebt);
        message = $"You share a drink with {name}. They know you a little better. The drink adds an hour of fatigue.";
        Note(message);
        Changed?.Invoke();
        return true;
    }
}
