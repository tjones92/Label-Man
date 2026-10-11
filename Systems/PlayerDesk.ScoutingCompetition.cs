using System;
using System.Collections.Generic;
using System.Linq;

public sealed class ScoutingAlternativeLead {
    public string ArtistId { get; init; }
    public string ArtistName { get; init; }
    public int LastHeardDay { get; init; }
    public string RoomName { get; init; }
    public ScoutingOpportunity NextSet { get; init; }
}

public partial class PlayerDesk {
    // Acquisition happens only when the player explicitly reads the public recap.
    // The existing information owner controls publication, geography and event identity.
    private void RememberScoutingReport(SceneNewsItem item) {
        if (Label == null || !LocalScenes.ScoutingExperience || LocalScenes.ScoutingExperienceObserver ||
            !item.Id.StartsWith("contract|", StringComparison.Ordinal)) return;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == item.ArtistId);
        if (dossier == null || dossier.Observations.Any(o => o.EventId == item.Id)) return;
        var clock = TimeManager.Instance;
        dossier.Observations.Add(new() {
            Id = $"public-report-v1|{Label.labelId}|{item.Id}", EventId = item.Id,
            LabelId = Label.labelId, ArtistId = dossier.ArtistId, ArtistName = dossier.ArtistName,
            Kind = ScoutingEvidenceKind.PublicReport, SourceId = "local-trade-report|" + item.PlaceId,
            SourceName = item.Source, Channel = "local trade recap", PlaceId = item.PlaceId,
            OccurredWeek = item.EventDay / 7, PublishedDay = item.AvailableDay,
            AcquiredDay = LocalSceneRoomService.Day(clock.CurrentDate), AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute,
            Credibility = "published signing report; event week known, exact signing day unrecorded",
            Claim = item.Text.Trim() + " Signing occurred during the week beginning " +
                LocalSceneRoomService.Date(item.EventDay).ToHeadlineString() +
                "; published " + LocalSceneRoomService.Date(item.AvailableDay).ToHeadlineString() +
                ". This reports a completed signing, not earlier inquiries, offers or a deadline."
        });
    }

    public string ScoutingCompetitionSummary(string artistId) {
        string interest = RivalInterestHistory(artistId);
        string offers = RivalOfferHistory(artistId);
        string summary = ScoutingReportSummary(artistId);
        return string.Join(" ", new[] { interest, offers, summary }.Where(s => s != null));
    }

    /// <summary>What the player knows of other labels' offers for this act: pending, and how earlier ones ended.</summary>
    private string RivalOfferHistory(string artistId) {
        // An offer nobody told the player about stays out of the notebook; its outcome may surface as a public report.
        var known = rivalOffers.Where(o => o.ArtistId == artistId && o.Disclosure != RivalOfferDisclosure.Unknown).OrderBy(o => o.MadeDay).ToList();
        if (known.Count == 0) return null;
        return string.Join(" ", known.Select(o => {
            string made = LocalSceneRoomService.Date(o.KnownDay).ToHeadlineString();
            string terms = o.Disclosure == RivalOfferDisclosure.Disclosed ? $" ({RivalTermsText(o.Terms)})" : " (terms not heard)";
            string end = o.ResolvedDay.HasValue ? LocalSceneRoomService.Date(o.ResolvedDay.Value).ToHeadlineString() : null;
            return o.Status switch {
                RivalOfferStatus.Pending => $"Offer from {o.RivalLabelName}, heard {made}{terms}; they answer by {LocalSceneRoomService.Date(o.DecisionDay).ToHeadlineString()}.",
                RivalOfferStatus.Accepted => $"{o.RivalLabelName}'s offer{terms}, heard {made}, was accepted {end}.",
                RivalOfferStatus.Declined => $"{o.RivalLabelName}'s offer{terms}, heard {made}, was turned down {end} when they signed with you.",
                RivalOfferStatus.Withdrawn => $"{o.RivalLabelName}'s offer{terms}, heard {made}, fell through {end}.",
                _ => $"{o.RivalLabelName}'s offer{terms}, heard {made}, lapsed {end}.",
            };
        }));
    }

    private string ScoutingReportSummary(string artistId) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var report = dossier?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.PublicReport);
        var account = dossier?.Observations.LastOrDefault(o => o.Kind == ScoutingEvidenceKind.ActBusinessClaim);
        if (account != null) return $"{account.SourceName}, received {LocalSceneRoomService.Date(account.AcquiredDay).ToHeadlineString()} (post-set act account): {account.Claim}" +
            (report == null ? " No public signing report has been saved." : $" Public report received {LocalSceneRoomService.Date(report.AcquiredDay).ToHeadlineString()}: {report.Claim}");
        if (report == null) return "Competition: no public signing report has been saved for this act. Private inquiries and offers are unknown; silence is not a guarantee of availability.";
        return $"{report.SourceName}, received {LocalSceneRoomService.Date(report.AcquiredDay).ToHeadlineString()} (public signing report): {report.Claim}";
    }

    public IReadOnlyList<ScoutingAlternativeLead> ScoutingAlternativeLeads(string artistId) {
        var act = ArtistManager.Instance?.GetArtist(artistId);
        if (act == null || string.IsNullOrEmpty(act.labelId) || act.labelId == Label?.labelId)
            return Array.Empty<ScoutingAlternativeLead>();
        string place = LocalScenePersistenceService.SceneIdFor(CurrentCityId);
        return scoutingKnowledge.Dossiers.Where(d => d.ArtistId != artistId && d.Priority != ScoutingPriority.Archived)
            .Where(d => ArtistManager.Instance.GetArtist(d.ArtistId) is { lifecycleStatus: ArtistLifecycleStatus.Active } other &&
                string.IsNullOrEmpty(other.labelId))
            .Select(d => (dossier: d, heard: d.Observations.Where(o => o.Kind == ScoutingEvidenceKind.Performance &&
                o.OccurredDay.HasValue && SceneRoomCatalog.Get(o.RoomId)?.PlaceId == place)
                .OrderByDescending(o => o.OccurredDay).ThenByDescending(o => o.EndMinute).FirstOrDefault()))
            .Where(x => x.heard != null)
            .OrderByDescending(x => x.dossier.Priority == ScoutingPriority.Pursue)
            .ThenByDescending(x => x.heard.OccurredDay).ThenBy(x => x.dossier.ArtistId, StringComparer.Ordinal)
            .Take(3).Select(x => new ScoutingAlternativeLead {
                ArtistId = x.dossier.ArtistId, ArtistName = x.dossier.ArtistName,
                LastHeardDay = x.heard.OccurredDay.Value,
                RoomName = x.heard.RoomName ?? SceneRoomCatalog.Get(x.heard.RoomId)?.Name,
                NextSet = NextReachableScoutingSet(x.dossier.ArtistId)
            }).ToArray();
    }

    public string ScoutingOtherKnownLeads(string artistId) {
        var act = ArtistManager.Instance?.GetArtist(artistId);
        if (act == null || string.IsNullOrEmpty(act.labelId) || act.labelId == Label?.labelId) return null;
        var leads = ScoutingAlternativeLeads(artistId);
        return leads.Count == 0 ? "Other leads: no other active unsigned act you heard locally is on the active notebook. Check announced bills or ask a booker for a listening tip."
            : "Other leads already heard locally: " + string.Join(", ", leads.Select(l => l.ArtistName)) +
                ". Your earlier notes remain available. Listening does not establish interest in a deal or reserve an act.";
    }
}
