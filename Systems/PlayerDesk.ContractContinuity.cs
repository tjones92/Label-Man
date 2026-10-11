using System;
using System.Collections.Generic;
using System.Linq;

// Identifies the commercial contract under discussion; cash/debt and release progress may change normally.
public sealed record RenewalContractIdentity(int Sequence, int StartWeek, int SignedYear, int ExpiresWeek,
    int ExpiresYear, int Term, int Singles, float Royalty, bool LabelPublishing, bool ArtistControl) {
    public static RenewalContractIdentity From(SimulatedArtist a) => new(a.contractSequence, a.contractStartWeek,
        a.signedYear, a.contractExpiresWeek, a.contractExpiresYear, a.contractLength, a.contractSinglesObligation,
        a.royaltyRate, a.labelOwnsPublishing, a.artistCreativeControl);
}

// IDs and detached conversation state only. Never serializes another live artist graph.
public sealed class RenewalDiscussionSaveData {
    public string LabelId { get; set; }
    public string ArtistId { get; set; }
    public RenewalContractIdentity OriginalContract { get; set; }
    public ContractTermSheet Ask { get; set; }
    public ContractTermSheet? Draft { get; set; }
    public NegotiationPosture Posture { get; set; }
    public ContractTalk Talk { get; set; }
    public List<string> Log { get; set; } = new();
}

public partial class PlayerDesk {
    private bool CurrentRenewalArtist(SimulatedArtist artist, out string message) {
        message = "";
        if (artist == null || !ReferenceEquals(ArtistManager.Instance?.GetArtist(artist.artistId), artist)) {
            message = "This act's entry is no longer current. Return to the roster."; return false;
        }
        if (Label?.roster?.Contains(artist) != true || artist.labelId != Label.labelId) {
            message = "They're not on your roster any more."; return false;
        }
        if (!artist.isActive || artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
            message = "This act is no longer available for a renewal."; return false;
        }
        if (!RosterManager.IsContractMatured(artist, TimeManager.Instance?.CurrentDate.year ?? 1960,
                ChartManager.Instance?.GetCurrentChartWeek() ?? 0)) {
            message = "The current contract is not up for renewal."; return false;
        }
        return true;
    }

    private bool CurrentRenewal(SimulatedArtist artist, out string message) {
        if (!CurrentRenewalArtist(artist, out message)) return false;
        if (PendingRenewal?.Artist != artist) { message = "Return to the current renewal discussion first."; return false; }
        if (PendingRenewal.OriginalContract == null || PendingRenewal.OriginalContract != RenewalContractIdentity.From(artist)) {
            message = "The contract changed since this discussion opened. Step back and review the current agreement."; return false;
        }
        return true;
    }

    private bool CurrentNewOffer(Prospect prospect, out string message) {
        if (!CanCommitSceneSigning(prospect, out message)) return false;
        if (!prospect.HasBaseline) { message = "Approach them first."; return false; }
        if (prospect.CooldownUntil is { } until && (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate) < until) {
            message = "They are not ready to discuss another offer yet."; return false;
        }
        return true;
    }

    private bool CurrentTalk(ContractTalk talk, out string message) {
        message = "This is no longer the current discussion.";
        if (talk == null || talk.stage == ContractTalkStage.Done) return false;
        if (talk.IsRenewal) {
            if (!ReferenceEquals(PendingRenewal?.Talk, talk)) return false;
            return CurrentRenewal(talk.Artist, out message);
        }
        if (!ReferenceEquals(talk.prospect?.Talk, talk)) return false;
        return CurrentNewOffer(talk.prospect, out message);
    }

    public string RenewalDiscussionUnavailable() => PendingRenewal == null ? null :
        CurrentRenewal(PendingRenewal.Artist, out string message) ? null : message;

    public bool StepBackFromRenewal(out string message) {
        if (PendingRenewal == null) { message = "No renewal discussion is open."; return false; }
        // Voluntary dismissal never drops an act or changes its contract, including a stale discussion.
        if (PendingRenewal.Talk != null) PendingRenewal.Talk.stage = ContractTalkStage.Done;
        PendingRenewal = null; message = "Stepped back from the renewal. No contract or ownership changed.";
        Changed?.Invoke(); return true;
    }

    private static bool FiniteOffer(float advance, float royalty, out string message) {
        message = "The advance and royalty must be finite numbers.";
        return float.IsFinite(advance) && float.IsFinite(royalty);
    }

    private bool CurrentContractCash(ContractTermSheet sheet, out string message) {
        if (!FiniteOffer(sheet.Advance, sheet.RoyaltyRate, out message)) return false;
        if (Label?.CanAffordToSign(sheet.Advance) == true) { message = ""; return true; }
        message = "The label can no longer afford these terms. The meeting time was consumed; no deal or advance payment completed. Adjust the offer or wait for more cash.";
        return false;
    }

    private RenewalDiscussionSaveData CaptureRenewalDiscussion() => PendingRenewal == null ? null : new() {
        LabelId = Label?.labelId, ArtistId = PendingRenewal.Artist?.artistId, OriginalContract = PendingRenewal.OriginalContract,
        Ask = PendingRenewal.Ask, Draft = PendingRenewal.Draft, Posture = PendingRenewal.Posture,
        Talk = DetachedTalk(PendingRenewal.Talk), Log = PendingRenewal.Talk?.log.ToList() ?? new()
    };

    private static bool ValidSavedTerms(ContractTermSheet t) => float.IsFinite(t.Advance) && t.Advance >= 0 &&
        float.IsFinite(t.RoyaltyRate) && t.RoyaltyRate >= 0 && t.RoyaltyRate <= 1 && t.TermYears is >= 1 and <= 7 &&
        t.SinglesObligation is >= 0 and <= 30 && float.IsFinite(t.NegotiationDifficulty) &&
        t.NegotiationDifficulty is >= 0 and <= 1 && Enum.IsDefined(t.Manager);

    private static bool ValidateRenewalDiscussion(RenewalDiscussionSaveData state, string labelId, out string message) {
        message = "The saved renewal discussion is invalid; no player state was restored.";
        if (state == null) return true; // Older saves contain no invented negotiation.
        if (state.LabelId != labelId || string.IsNullOrEmpty(state.ArtistId) || state.OriginalContract == null ||
            !float.IsFinite(state.OriginalContract.Royalty) || !ValidSavedTerms(state.Ask) ||
            state.Draft.HasValue && !ValidSavedTerms(state.Draft.Value) || !Enum.IsDefined(state.Posture)) return false;
        var t = state.Talk;
        if (state.Posture == NegotiationPosture.Pushover) return t == null;
        return t != null && t.prospect == null && t.renewalArtist == null && t.ask.Equals(state.Ask) && t.posture == state.Posture &&
            Enum.IsDefined(t.stage) && t.stage != ContractTalkStage.Done && t.patienceMax > 0 && t.patienceLeft >= 0 &&
            t.patienceLeft <= t.patienceMax && t.roundsPlayed >= 0 && float.IsFinite(t.reservation) && t.reservation >= 0 &&
            float.IsFinite(t.lastOfferValue) && t.weights?.Count == Enum.GetValues<ContractAxis>().Length &&
            t.weights.All(kv => Enum.IsDefined(kv.Key) && float.IsFinite(kv.Value) && kv.Value >= 0) && t.weights.Values.Sum() > 0 &&
            (!t.objectionAxis.HasValue || Enum.IsDefined(t.objectionAxis.Value)) &&
            (t.stage != ContractTalkStage.Objection || t.lastOffer.HasValue) &&
            (!t.lastOffer.HasValue || ValidSavedTerms(t.lastOffer.Value)) && (!t.draftOffer.HasValue || ValidSavedTerms(t.draftOffer.Value));
    }

    /// <summary>The outlook a saved talk carries must be one the reader could have produced. Older saves carry none.</summary>
    private static bool ValidSavedOutlook(ContractTalk t) => t == null ||
        float.IsFinite(t.outlookCredit) && t.outlookCredit >= -0.31f && t.outlookCredit <= 0.15f &&
        float.IsFinite(t.outlookLift) && t.outlookLift >= 0f && t.outlookLift <= 0.5f && t.outlookLift <= t.reservation &&
        (!t.capabilityGap.HasValue || Enum.IsDefined(t.capabilityGap.Value)) &&
        (!t.capabilityObjection.HasValue || Enum.IsDefined(t.capabilityObjection.Value)) &&
        (t.outlookStated == null || t.outlookStated.All(line => line != null));

    private static bool ValidateSavedOutlooks(PlayerSaveData data, out string message) {
        message = "A saved negotiation carries an invalid outlook; no player state was restored.";
        return ValidSavedOutlook(data.RenewalDiscussion?.Talk) &&
            (data.ScoutingKnowledge?.Negotiations ?? new()).All(n => n != null && ValidSavedOutlook(n.Talk));
    }

    private void RestoreRenewalDiscussion(RenewalDiscussionSaveData state) {
        PendingRenewal = null;
        if (state == null) return;
        var artist = ArtistManager.Instance?.GetArtist(state.ArtistId);
        if (artist == null) { Note("The saved renewal act could not be found. No replacement discussion was created."); return; }
        PendingRenewal = new() { Artist = artist, Ask = state.Ask, Draft = state.Draft, Posture = state.Posture,
            OriginalContract = state.OriginalContract, Talk = DetachedTalk(state.Talk) };
        if (PendingRenewal.Talk != null) {
            PendingRenewal.Talk.renewalArtist = artist;
            PendingRenewal.Talk.log.Clear(); PendingRenewal.Talk.log.AddRange(state.Log ?? new());
        }
    }
}
