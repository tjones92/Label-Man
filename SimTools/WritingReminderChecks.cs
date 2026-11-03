using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class WritingReminderChecks {
    public static void Run() {
        void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        Check(ProfessionalWritingMarket.PlanRemindersEnabled, "Reminder switch required");
        var state = ProfessionalWritingMarket.State;
        var label = ChartManager.Instance.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && l.roster.Any(a =>
            ProfessionalWritingMarket.OwnedDelivery(l, a) == null && !state.Orders.Any(o => o.ArtistId == a.artistId)));
        label.cashReserves = 10000000;
        var artist = label.roster.First(a => ProfessionalWritingMarket.OwnedDelivery(label, a) == null && !state.Orders.Any(o => o.ArtistId == a.artistId));
        int start = Math.Max(1, ProfessionalWritingMarket.Week);
        state.LastWeek = start;
        var date = TimeManager.Instance.CurrentDate;
        var programme = new ReleaseProgrammeState();
        CompetitorManager.Instance.SetProgrammeForChecks(programme);
        AiRepertoireService.Judge(label, artist, date.year, start);
        AiRepertoireService.Acts[artist.artistId].Candidates.Clear();
        int orders = state.Orders.Count;
        ProfessionalWritingMarket.RememberPlanlessArrival(label, artist, start);
        ProfessionalWritingMarket.RememberPlanlessArrival(label, artist, start + 1);
        Check(state.Reminders.Count == 1 && state.Reminders[0].ExpiresWeek == start + 8, "Repeated arrival neither duplicates nor refreshes reminder");
        string saved = JsonSerializer.Serialize(state, SaveGameService.TestJsonOptions);
        var restored = JsonSerializer.Deserialize<ProfessionalWritingMarketState>(saved, SaveGameService.TestJsonOptions);
        Check(saved == JsonSerializer.Serialize(restored, SaveGameService.TestJsonOptions), "Pending reminder round trips exactly");
        restored.PlanReminders = false;
        bool refused = false;
        try { ProfessionalWritingMarket.RequireSaved(restored); } catch (InvalidOperationException) { refused = true; }
        Check(refused, "Cannot resume with different reminder policy");
        ProfessionalWritingMarket.Restore(JsonSerializer.Deserialize<ProfessionalWritingMarketState>(saved, SaveGameService.TestJsonOptions));
        state = ProfessionalWritingMarket.State;
        var item = new ReleaseSlateItem { Id = "reminder-check-plan", LabelId = label.labelId, ArtistId = artist.artistId,
            ContractSequence = artist.contractSequence, Format = ReleaseFormat.Single, State = ReleaseSlateState.Proposed,
            ProposedWeek = start + 1, ReadyWeek = start + 5, IntendedWeek = start + 6, DueWeek = start + 6 };
        programme.Items.Add(item);
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start);
        Check(state.Orders.Count == orders && state.Reminders.Count == 1, "No same-week reconsideration");
        state.LastWeek = start + 1;
        item.MaterialSearch = new ProgrammeMaterialSearch { State = ProgrammeMaterialState.Declined };
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start + 1);
        Check(state.Orders.Count == orders && state.Reminders.Count == 1, "Previously reviewed material is excluded");
        item.MaterialSearch = null;
        item.WritingReminderMaterialReviewed = true;
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start + 1);
        Check(state.Orders.Count == orders && state.Reminders.Count == 1, "A failed material review cannot be retried through a reminder");
        item.WritingReminderMaterialReviewed = false;
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start + 1);
        Check(state.Orders.Count == orders + 1 && state.Orders.Last().ReleasePlanId == item.Id, "Eligible plan books one linked order");
        Check(state.Orders.Last().PlanReminder && ProfessionalWritingMarket.AwaitingReminderCommission(item), "Reminder work blocks a competing material search while pending");
        Check(item.WritingReminderAttempted && state.Reminders.Count == 0, "Attempt is consumed and marked on plan");
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start + 1);
        Check(state.Orders.Count == orders + 1, "Reprocessing cannot duplicate booking");
        string slate = JsonSerializer.Serialize(item, SaveGameService.TestJsonOptions);
        Check(JsonSerializer.Deserialize<ReleaseSlateItem>(slate, SaveGameService.TestJsonOptions).WritingReminderAttempted, "Attempt marker survives save");
        item.State = ReleaseSlateState.Cancelled;
        var second = new ReleaseSlateItem { Id = "reminder-second-plan", LabelId = label.labelId, ArtistId = artist.artistId,
            ContractSequence = artist.contractSequence, Format = ReleaseFormat.Single, State = ReleaseSlateState.Proposed,
            ReadyWeek = start + 6, IntendedWeek = start + 7, DueWeek = start + 7 };
        programme.Items.Add(second);
        state.Reminders.Add(new WritingPlanReminder { LabelId = label.labelId, ArtistId = artist.artistId, ContractSequence = artist.contractSequence,
            CreatedWeek = start, ExpiresWeek = start + 8 });
        state.LastWeek = start + 2;
        ProfessionalWritingMarket.ReconsiderPlanReminders(label, date, start + 2);
        Check(state.Orders.Count == orders + 1 && state.Reminders.Count == 0 && second.WritingReminderAttempted,
            "Pending-order guard consumes a single attempt without another purchase");
        state.Reminders.Add(new WritingPlanReminder { LabelId = label.labelId, ArtistId = artist.artistId, ContractSequence = artist.contractSequence,
            CreatedWeek = start, ExpiresWeek = start + 8 });
        ProfessionalWritingMarket.Advance(start + 9, date.AddDays(63));
        Check(state.Reminders.Count == 0, "Reminder expires after eight weeks");
        GD.Print("WRITING_REMINDER_CHECK_PASS: bounded deduplication, policy persistence, material guard, linked one-time booking and saved marker.");
    }
}
