using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
    private void ScoutingMeetingAgenda(PlayerDesk desk) {
        var pending = desk.ScoutingMeetings.Where(m => m.Status == SceneConversationStatus.Scheduled).OrderBy(m => m.Day).ThenBy(m => m.StartMinute).ToArray();
        if (pending.Length == 0) return;
        Heading("AGREED MEETINGS");
        foreach (var meeting in pending) {
            var clock = TimeManager.Instance; int day = LocalSceneRoomService.Day(clock.CurrentDate), now = clock.CurrentHour * 60 + clock.CurrentMinute;
            bool ready = day == meeting.Day && now == meeting.StartMinute;
            Body($"{meeting.ArtistName} · {meeting.PersonName} · {(meeting.Activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(meeting.RoomId)?.Name)}, " +
                $"{LocalSceneRoomService.Date(meeting.Day).ToHeadlineString()}, {meeting.StartMinute / 60:00}:{meeting.StartMinute % 60:00}–{meeting.EndMinute / 60:00}:{meeting.EndMinute % 60:00}. " +
                $"{meeting.Activity} · {(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0} at attendance. Cancel before starting without charge; consumed sessions are not refunded. No hold on the act.");
            if (meeting.Activity != ScoutingAppointmentActivity.Conversation) Body("Agreed material: " + meeting.Material.FirstOrDefault()?.Title + ". Lineup: " +
                string.Join(", ", meeting.ParticipantIds.Select(id => ArtistManager.Instance.GetArtist(meeting.ArtistId)?.members.FirstOrDefault(m => m.personId == id)?.FullName ?? "unavailable participant")) + ".");
            var actions = new HFlowContainer();
            var attend = Btn(ready ? meeting.Activity == ScoutingAppointmentActivity.Conversation ? "ATTEND MEETING (1h · no fee)" : $"ATTEND {meeting.Activity.ToString().ToUpperInvariant()} ({(meeting.EndMinute - meeting.StartMinute) / 60}h · ${meeting.Fee:0})" : "WAIT FOR MEETING");
            attend.Disabled = !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver || !LocalScenes.Rooms ||
                day > meeting.Day || day == meeting.Day && now > meeting.StartMinute;
            attend.TooltipText = "Waiting advances ordinary expenses and other labels' decisions. Attend at the agreed start; current speaker, place, bookings and willingness are checked again. A substantial conversation creates no simultaneous listening evidence.";
            GreyWhy(attend, "The agreed start has passed.");
            ScoutingTimeRisk(desk, attend, meeting.Day, ready ? meeting.EndMinute : meeting.StartMinute, meeting.Id);
            attend.Pressed += () => Act(() => {
                bool ok = ready ? desk.AttendScoutingMeeting(meeting.Id, out string result) : desk.WaitForScoutingMeeting(meeting.Id, out result);
                if (ready && ok && meeting.Activity == ScoutingAppointmentActivity.Conversation) negotiating = desk.Slate.FirstOrDefault(p => p.Artist.artistId == meeting.ArtistId && p.HasBaseline);
                Say(result, ok); return ok;
            }); actions.AddChild(attend);
            var cancel = Btn("CANCEL MEETING");
            cancel.TooltipText = "Release this appointment without a fee or action time. Cancelling grants no relationship credit or new musical evidence.";
            cancel.Pressed += () => Act(() => { bool ok = desk.CancelScoutingMeeting(meeting.Id, out string result); Say(result, ok); return ok; });
            actions.AddChild(cancel); content.AddChild(actions);
        }
    }
    /// <summary>Talking to an act after a set. Playtest 2026-11-04 (shot 020): a plain sentence sat among the buttons. The
    /// pop-up now says who you are talking to and where things stand first, then lists what you can ask, one per line,
    /// each with its cost (or, when it can't be asked today, why) beside it.</summary>
    private void OpenScoutingConversation(PlayerDesk desk, ScoutingDossier dossier) {
        var modal = PaperModal.Open(this, dossier.ArtistName + " · CONVERSATION", 750);
        WithinScouting(modal.Body, () => {
            var prospect = desk.Slate.FirstOrDefault(p => p.Artist?.artistId == dossier.ArtistId);
            var meeting = desk.PreviewScoutingBusinessMeeting(dossier.ArtistId);
            var artist = ArtistManager.Instance.GetArtist(dossier.ArtistId);
            string standing = prospect != null ? PlayerDesk.SceneDealDescription(prospect) : null;
            string genre = artist != null ? GenreNameFormatter.Format(artist.primaryGenre) : null;
            var phoneNumber = desk.ActNumber(dossier.ArtistId);
            string phoneWhy = phoneNumber != null ? desk.PhoneBlocked(dossier.ArtistId) : null;
            bool afterSet = desk.ActNumberAfterSetBlocked(dossier.ArtistId) == null || (phoneNumber == null && meeting != null);
            string talkingTo = phoneNumber != null && desk.PreviewScoutingInvitation(dossier.ArtistId) != null && !afterSet
                ? $"You can ring {phoneNumber.PersonName} (number from {phoneNumber.Source}); a call takes {PlayerDesk.PhoneCallMinutes} minutes."
                : meeting != null ? $"You're talking with {meeting.SourceName}; this is conversation, not more listening." : null;
            ScoutingText(string.Join("  ", new[] {
                genre == null && standing == null ? null : string.Join(" · ", new[] { genre, standing }.Where(s => !string.IsNullOrEmpty(s))) + ".",
                talkingTo }.Where(s => !string.IsNullOrEmpty(s))), 16, Ink);

            ScoutingText("THINGS TO ASK", 13, Rust, PaperTheme.SansBold);
            var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            list.AddThemeConstantOverride("separation", 6);
            content.AddChild(list);
            void Line(Button ask, string note) {
                ask.CustomMinimumSize = new Vector2(280, 32);
                var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
                row.AddThemeConstantOverride("separation", 12);
                row.AddChild(ask);
                var text = new Label { Text = note ?? "", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill, VerticalAlignment = VerticalAlignment.Center };
                text.AddThemeFontSizeOverride("font_size", 14);
                text.AddThemeColorOverride("font_color", ask.Disabled ? Rust : Heard);
                row.AddChild(text);
                list.AddChild(row);
            }

            if (prospect != null) {
                bool askFirst = desk.CanAskDirectApproach(prospect.Artist);
                var deal = ScoutingSmallButton(askFirst ? "IF THEY'D HEAR AN OFFER" : "TERMS — MAKE AN OFFER");
                bool evidence = prospect.FollowedUp || desk.HasScoutingEvidence(prospect.Artist.artistId);
                deal.Disabled = !askFirst && (!PlayerDesk.SceneDealAvailable(prospect) || !evidence);
                deal.TooltipText = askFirst ? "They aren't looking for a record deal. Their side may think it over and answer within about a week, or say no for this season."
                    : PlayerDesk.SceneDealDescription(prospect);
                deal.Pressed += () => { modal.Close(); bool ok = desk.ApproachToSign(prospect, out string result); if (ok) negotiating = prospect; Say(result, ok); Refresh(); };
                Line(deal, deal.Disabled ? (!evidence ? "Hear them play first." : "They aren't ready to talk terms.") : "Free · no time.");
            }
            if (meeting != null) {
                var history = ScoutingSmallButton("PAST LABEL DECISIONS");
                history.Disabled = !meeting.Available;
                history.TooltipText = "Their account may be incomplete and does not establish a current offer or deadline. Later listening requires admission again.";
                history.Pressed += () => Act(() => {
                    modal.Close();
                    bool ok = desk.AskScoutingBusinessHistory(dossier.ArtistId, meeting.QuoteId, out string result);
                    Say(result, ok); return ok;
                });
                Line(history, meeting.Available ? $"{PlayerDesk.ActHistoryMinutes}min." : meeting.UnavailableReason);
            }
            ScoutingMeetingInvitation(desk, dossier, modal, Line);
            // Reaching them away from their sets (playtest 2026-11-05).
            if (phoneNumber == null) {
                var askNumber = ScoutingSmallButton($"ASK FOR THEIR NUMBER · {PlayerDesk.AskActNumberMinutes}min");
                string why = desk.ActNumberAfterSetBlocked(dossier.ArtistId);
                askNumber.Disabled = why != null;
                askNumber.TooltipText = why ?? "Most players will give you a number to call; a no stands for the season.";
                askNumber.Pressed += () => { modal.Close(); Act(() => { bool ok = desk.AskActForNumber(dossier.ArtistId, out string result); Say(result, ok); return ok; }); };
                Line(askNumber, why ?? "With a number you can call to set up meetings from anywhere.");
                if (why != null) ScoutingText("No number yet. Ask a member after a set, or ask a booker who has had them play (from the booker questions on their card).", 13, Heard);
            } else ScoutingText($"Number: {phoneNumber.PersonName}, from {phoneNumber.Source}." + (phoneWhy != null ? " " + phoneWhy : $" Calls take {PlayerDesk.PhoneCallMinutes} min, {PlayerDesk.PhoneFromHour}:00–{PlayerDesk.PhoneToHour}:00."), 13, Heard);
        });
        modal.AddButton("BACK", null);
    }
    private void ScoutingMeetingInvitation(PlayerDesk desk, ScoutingDossier dossier, PaperModal modal, System.Action<Button, string> line) {
        foreach (var activity in new[] { ScoutingAppointmentActivity.Rehearsal, ScoutingAppointmentActivity.Demo, ScoutingAppointmentActivity.Conversation }) {
            var quote = desk.PreviewScoutingInvitation(dossier.ArtistId, activity);
            string label = activity == ScoutingAppointmentActivity.Conversation ? "RETURN MEETING" : activity.ToString().ToUpperInvariant();
            var ask = ScoutingSmallButton("ASK FOR " + label);
            ask.Disabled = quote == null;
            string asking = desk.ActNumber(dossier.ArtistId) != null ? $"Asking takes {PlayerDesk.ScoutingInvitationMinutes}min in person or a {PlayerDesk.PhoneCallMinutes}-minute call" : $"Asking takes {PlayerDesk.ScoutingInvitationMinutes}min";
            ask.TooltipText = quote == null ? (desk.ActNumber(dossier.ArtistId) != null ? desk.PhoneBlocked(dossier.ArtistId) ?? "No free slot to propose right now." : "Talk after a set you've heard today, or get their number to call.")
                : $"Propose {LocalSceneRoomService.Date(quote.Day).ToHeadlineString()}, {quote.StartMinute / 60:00}:{quote.StartMinute % 60:00} at " +
                  (activity == ScoutingAppointmentActivity.Demo ? "Home budget studio" : SceneRoomCatalog.Get(quote.RoomId)?.Name) +
                  $". {asking}, with no fee. The act may decline. The visit takes {(quote.EndMinute - quote.StartMinute) / 60}h and costs ${quote.Fee:0} when you attend." +
                  (activity == ScoutingAppointmentActivity.Conversation ? " A meeting doesn't reserve the act or commit them to a deal." :
                  "\nSong: " + quote.Material.Single().Title + ". Players: " + string.Join(", ", quote.ParticipantIds.Select(id => ArtistManager.Instance.GetArtist(dossier.ArtistId).members.First(m => m.personId == id).FullName)) +
                  ". Cancel before starting for free; time and fees already spent aren't refunded.");
            ask.Pressed += () => { modal.Close(); Act(() => { bool ok = desk.ArrangeScoutingMeeting(dossier.ArtistId, quote.Id, out string result, activity); Say(result, ok); return ok; }); };
            line(ask, quote == null ? (desk.ActNumber(dossier.ArtistId) != null ? desk.PhoneBlocked(dossier.ArtistId) ?? "No free slot to propose." : "After a set you've heard today, or by phone with their number.")
                : $"{LocalSceneRoomService.Date(quote.Day).ToHeadlineString()} · {(quote.EndMinute - quote.StartMinute) / 60}h · {(quote.Fee > 0 ? $"${quote.Fee:0}" : "no fee")} when you go.");
        }
    }

}
