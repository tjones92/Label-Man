using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    /// <summary>Own-resource forecast only; no artist acceptance, promise, booking or cash mutation.</summary>
    public sealed record OfferResourcePlan(float Advance, float CashAfterAdvance, float MonthlyOverhead,
        float SigningReserve, bool AdvanceAffordable, int RosterUsed, int RosterCapacity, bool RosterSpace,
        bool Renewal, float PendingAppointmentFees, int PendingAppointmentCount, float BudgetSessionCost,
        float CashAfterFeesAndSession, bool Home, bool ConsoleFree, int MeetingHours, bool MeetingFits,
        string AppointmentRisk, IReadOnlyList<string> FeeDetails) {
        public string Summary =>
            $"Cash after advance: ${CashAfterAdvance:N0} · signing reserve: ${SigningReserve:N0} (2 months of current ${MonthlyOverhead:N0} overhead) · " +
            (AdvanceAffordable ? "reserve covered." : $"must leave more than ${SigningReserve:N0}; lower the advance or wait for more cash.") +
            $"\nRoster: {RosterUsed}/{RosterCapacity} · " + (Renewal ? "renewal uses the existing slot." : RosterSpace ? "space for one new act." : "full; a new signing needs a free slot.") +
            $"\nAgreed unpaid appointments: ${PendingAppointmentFees:N0} across {PendingAppointmentCount} retained booking(s), payable at attendance; no cash set aside. These appointments can change or be cancelled." +
            $"\nRecording estimate: home budget room, {MinSessionHours}h, ${BudgetSessionCost:N0}; after the advance, retained appointment fees and this session: ${CashAfterFeesAndSession:N0}. " +
            (CashAfterFeesAndSession < 0 ? "This combined plan exceeds current cash. " : "") +
            (!Home ? "Return home to use this route. " : "") + (!ConsoleFree ? "Print or scrap the session on the console first. " : "") +
            "Material, consent and a free date still need checking. This is one session estimate, not a funded promise or a release budget; pressing, distribution, travel and later overhead are extra." +
            $"\nMeeting: {MeetingHours}h · " + (MeetingFits ? "fits today's remaining hours." : "does not fit today's remaining hours.") +
            (AppointmentRisk == null ? "" : "\n" + AppointmentRisk);

        /// <summary>One line of the contract form's breakdown: a short rubric, what it comes to, and whether it needs the player's eye.</summary>
        public readonly record struct Row(string Rubric, string Value, bool Warn);

        /// <summary>The same facts as <see cref="Summary"/>, one per row, so the form can lay them out as a ledger instead of a paragraph.</summary>
        public IReadOnlyList<Row> Rows {
            get {
                var rows = new List<Row> {
                    new("Cash after advance", $"${CashAfterAdvance:N0}" + (AdvanceAffordable ? "" : "  (below the signing reserve; lower the advance or wait for more cash)"), !AdvanceAffordable),
                    new("Signing reserve", $"${SigningReserve:N0}  (2 months of ${MonthlyOverhead:N0} overhead)  " + (AdvanceAffordable ? "covered" : "not covered"), !AdvanceAffordable),
                    new("Roster", $"{RosterUsed}/{RosterCapacity}  " + (Renewal ? "renewal uses the existing slot" : RosterSpace ? "space for one new act" : "full; a new signing needs a free slot"), !Renewal && !RosterSpace),
                    new("Booked fees", PendingAppointmentCount == 0 ? "none outstanding" : $"${PendingAppointmentFees:N0} across {PendingAppointmentCount} booking(s), payable at attendance", false),
                    new("Recording estimate", $"${BudgetSessionCost:N0} for one {MinSessionHours}h home budget session", false),
                    new("Left after all that", $"${CashAfterFeesAndSession:N0}" + (CashAfterFeesAndSession < 0 ? "  This combined plan exceeds current cash." : ""), CashAfterFeesAndSession < 0),
                };
                if (!Home) rows.Add(new("Studio", "Return home to use this route", true));
                if (!ConsoleFree) rows.Add(new("Console", "Print or scrap the session on the console first", true));
                rows.Add(new("Meeting", $"{MeetingHours}h  " + (MeetingFits ? "fits today's remaining hours" : "does not fit today's remaining hours"), !MeetingFits));
                if (AppointmentRisk != null) rows.Add(new("Appointment clash", AppointmentRisk, true));
                return rows;
            }
        }

        /// <summary>What the figures above do not promise; set small under the ledger rather than in it.</summary>
        public string FinePrint =>
            "Booked fees have no cash set aside and can change or be cancelled. The recording figure is one session estimate, not a funded promise or a release budget; " +
            "material, consent and a free date still need checking, and pressing, distribution, travel and later overhead are extra.";
    }

    public OfferResourcePlan PreviewOfferResources(float advance, bool renewal = false, int meetingHours = NegotiationRoundHours) {
        if (Label == null || !float.IsFinite(advance) || advance < 0 || meetingHours < 0 || meetingHours > 24) return null;
        var clock = TimeManager.Instance;
        int day = clock == null ? 0 : LocalSceneRoomService.Day(clock.CurrentDate);
        int minute = clock == null ? 0 : clock.CurrentHour * 60 + clock.CurrentMinute;
        // Retained quotes are not escrow or debt. Never refresh status or infer that signing cancels a booking.
        var fees = ScoutingMeetings.Where(m => !m.Paid && m.Fee > 0 && float.IsFinite(m.Fee) &&
            m.Status is SceneConversationStatus.Scheduled or SceneConversationStatus.InProgress &&
            (m.Day > day || m.Day == day && (m.Status == SceneConversationStatus.InProgress ? m.EndMinute > minute : m.StartMinute >= minute)))
            .OrderBy(m => m.Day).ThenBy(m => m.StartMinute).ThenBy(m => m.Id, StringComparer.Ordinal).ToArray();
        float unpaid = fees.Sum(m => m.Fee), after = Label.cashReserves - advance;
        float overhead = Label.GetMonthlyOverhead(), session = SessionCost(StudioTier.Budget, MinSessionHours);
        bool fits = clock?.CanAffordHours(meetingHours, allowOvertime: true) == true;
        return new(advance, after, overhead, overhead * 2, Label.CanAffordToSign(advance),
            Label.CurrentRosterSize, Label.maxRosterSize, Label.HasRosterSpace, renewal, unpaid, fees.Length, session,
            after - unpaid - session, AtHome, Session == null, meetingHours, fits,
            fits ? ScoutingAppointmentRiskText(day, minute + meetingHours * 60) : null,
            Array.AsReadOnly(fees.Select(m => $"{m.ArtistName} · {m.Activity.ToString().ToLowerInvariant()} · {LocalSceneRoomService.Date(m.Day).ToHeadlineString()} {m.StartMinute / 60:00}:{m.StartMinute % 60:00} · ${m.Fee:N0} unpaid").ToArray()));
    }
}
