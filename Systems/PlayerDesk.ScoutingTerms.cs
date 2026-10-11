using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerDesk {
    private bool CanRememberScoutingBusiness(Prospect prospect) => Label != null && LocalScenes.ScoutingExperience &&
        !LocalScenes.ScoutingExperienceObserver && prospect?.Artist != null && HasScoutingEvidence(prospect.Artist.artistId);

    private void StartScoutingBusinessDiscussion(Prospect prospect) {
        // An off-mode new discussion must not reuse the identity of an earlier acquired one.
        prospect.BusinessDiscussionId = null;
        if (!CanRememberScoutingBusiness(prospect)) return;
        var dossier = scoutingKnowledge.Dossiers.Single(d => d.ArtistId == prospect.Artist.artistId);
        prospect.BusinessDiscussionId = $"business-discussion-v1|{Label.labelId}|{dossier.ArtistId}|{++dossier.BusinessDiscussionCount}";
        RememberScoutingBusinessStatement(prospect, "opening", ScoutingBusinessStatementKind.OpeningAsk,
            "Opening ask: " + ScoutingTermsText(ScoutingStatedTerms.From(prospect.Baseline)) +
            " These are the stated starting terms, not a minimum acceptable package or a promise to sign.", prospect.Baseline);
    }

    private void RememberScoutingBusinessStatement(Prospect prospect, string occurrence,
        ScoutingBusinessStatementKind kind, string claim, ContractTermSheet? terms = null) {
        if (!CanRememberScoutingBusiness(prospect)) return;
        var dossier = scoutingKnowledge.Dossiers.Single(d => d.ArtistId == prospect.Artist.artistId);
        // Legacy live negotiations acquire only their next actual response, never a fabricated opening.
        if (string.IsNullOrEmpty(prospect.BusinessDiscussionId))
            prospect.BusinessDiscussionId = $"business-discussion-v1|{Label.labelId}|{dossier.ArtistId}|{++dossier.BusinessDiscussionCount}";
        string eventId = prospect.BusinessDiscussionId + "|" + occurrence;
        if (dossier.Observations.Any(o => o.EventId == eventId)) return;
        var clock = TimeManager.Instance;
        int day = LocalSceneRoomService.Day(clock.CurrentDate);
        string source = string.IsNullOrEmpty(prospect.Baseline.ManagerName) ? dossier.ArtistName + "'s side" : prospect.Baseline.ManagerName;
        dossier.Observations.Add(new() { Id = "business-statement-v1|" + eventId, EventId = eventId,
            LabelId = Label.labelId, ArtistId = dossier.ArtistId, ArtistName = dossier.ArtistName,
            Kind = ScoutingEvidenceKind.BusinessStatement, BusinessDiscussionId = prospect.BusinessDiscussionId,
            BusinessStatementKind = kind, StatedTerms = terms.HasValue ? ScoutingStatedTerms.From(terms.Value) : null,
            SourceId = kind == ScoutingBusinessStatementKind.SteppedBack ? Label.labelId : prospect.Artist.artistId,
            SourceName = kind == ScoutingBusinessStatementKind.SteppedBack ? "Your label" : source,
            Channel = "contract discussion", PlaceId = LocalScenePersistenceService.SceneIdFor(CurrentCityId),
            OccurredDay = day, AcquiredDay = day, AcquiredMinute = clock.CurrentHour * 60 + clock.CurrentMinute,
            Credibility = "recorded statement in this label's contract discussion; historical, not a current offer or a guarantee of consent",
            Claim = claim });
    }

    private string NextScoutingBusinessOccurrence(Prospect prospect, string action) => action + "|" +
        (scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == prospect?.Artist?.artistId)?.Observations
            .Count(o => o.BusinessDiscussionId == prospect.BusinessDiscussionId && o.Kind == ScoutingEvidenceKind.BusinessStatement) ?? 0);

    public static string ScoutingTermsText(ScoutingStatedTerms terms) =>
        $"${terms.Advance:N0} advance, {terms.RoyaltyRate:P1} royalty, {terms.TermYears} year(s), {terms.SinglesObligation} sides; " +
        (terms.LabelOwnsPublishing ? "label owns publishing" : "artist keeps publishing") + "; " +
        (terms.ArtistCreativeControl ? "artist has creative control." : "label has creative control.");

    public IReadOnlyList<string> ScoutingBusinessStatements(string artistId) {
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        var all = dossier?.Observations.Where(o => o.Kind == ScoutingEvidenceKind.BusinessStatement).ToArray();
        var opening = all?.LastOrDefault(o => o.BusinessStatementKind == ScoutingBusinessStatementKind.OpeningAsk);
        var statements = opening == null ? all?.TakeLast(3).ToArray() :
            new[] { opening }.Concat(all.Where(o => o.Id != opening.Id).TakeLast(2)).ToArray();
        if (statements == null || statements.Length == 0)
            return new[] { "What they want: no terms discussion has been recorded. Hearing an act does not establish its business wishes." };
        return statements.Select(o => $"Business discussion · {LocalSceneRoomService.Date(o.OccurredDay ?? o.AcquiredDay).ToHeadlineString()} · " +
            $"{o.SourceName}: {o.Claim}" + (o.BusinessStatementKind == ScoutingBusinessStatementKind.Counter && o.StatedTerms != null ?
                " Counter terms: " + ScoutingTermsText(o.StatedTerms) : "") +
            " Recorded then; current availability and terms must be checked again.").ToArray();
    }
}
