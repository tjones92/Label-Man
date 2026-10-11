using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum ContractCommitmentKind { FundedSession, FundedPromotion, Distribution }
public enum ContractCommitmentStatus { Open, Kept, Missed, Void }

/// <summary>
/// One enforceable promise written into a player contract. A funded session: the label books and pays for a studio
/// session with the act before <see cref="DueDay"/>. A funded promotion: the label buys at least a half-page trade ad
/// for the act's first record within eight weeks of its release. Distribution: a wholesale house (a placed line, or a
/// distribution deal covering the record) carries their first record within eight weeks of its release. The same record carries the agreement, its
/// fulfillment, a miss and the remedy, so nothing about the promise is reconstructed from the roster later. IDs only.
/// </summary>
public sealed class ContractCommitment {
    public string Id { get; set; }
    public string LabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public ContractCommitmentKind Kind { get; set; }
    public bool Renewal { get; set; }
    public int AgreedDay { get; set; }
    /// <summary>For a promotion or distribution promise, the release deadline until the first record is out, then eight weeks after it.</summary>
    public int DueDay { get; set; }
    /// <summary>The cheapest qualifying session, or the half-page rate, when agreed. A quote for planning, not escrow.</summary>
    public float QuotedCost { get; set; }
    public ContractCommitmentStatus Status { get; set; }
    public int? ResolvedDay { get; set; }
    /// <summary>A missed promise is answered once, by the next renewal ask that carries it.</summary>
    public bool RemedyApplied { get; set; }
    /// <summary>Promotion and distribution only: the day the act's first record after the agreement came out. Absent on older saves.</summary>
    public int? ReleaseDay { get; set; }
}

public partial class PlayerDesk {
    /// <summary>Days after signing within which a promised session must be booked and paid.</summary>
    public const int SessionPromiseDays = 56;
    /// <summary>Days after the first record's release within which the promised trade ad must run.</summary>
    public const int PromotionPromiseDays = 56;
    /// <summary>Days after the first record's release within which a wholesale house must be carrying it.</summary>
    public const int DistributionPromiseDays = 56;
    /// <summary>A promotion or distribution promise for a record that never comes out is broken at this many days after signing.</summary>
    public const int PromotionReleaseDeadlineDays = 273;
    /// <summary>The smallest push that keeps a promotion promise.</summary>
    public const TradeAdTier PromotionPromiseTier = TradeAdTier.HalfPage;
    /// <summary>How much a renewal ask's advance rises for each promise the label never kept.</summary>
    public const float MissedSessionAdvancePremium = 0.25f;

    private readonly List<ContractCommitment> contractCommitments = new();
    public IReadOnlyList<ContractCommitment> ContractCommitments => LocalSceneRoomService.Copy(contractCommitments).AsReadOnly();

    /// <summary>The cheapest session that keeps the promise, at today's home rate.</summary>
    public float SessionPromiseQuote() => SessionCost(StudioTier.Budget, MinSessionHours);

    /// <summary>The half-page rate: the era rate card, not budget-scaled.</summary>
    public static float PromotionPromiseQuote() => TradeAdCost(PromotionPromiseTier);

    /// <summary>What a funded session is worth to this act at the table. Ambitious acts want a date in the
    /// studio more than pragmatic ones. Read only by the acceptance check; never shown as a number.</summary>
    internal static float SessionPromiseValue(SimulatedArtist artist) =>
        0.02f + 0.06f * Mathf.Clamp(artist?.evolution?.artisticAmbition ?? 0.5f, 0f, 1f);

    /// <summary>What a promised push is worth: the same range as the session, weighted to pragmatic acts, who want the
    /// record sold. Read only by the acceptance check; never shown as a number.</summary>
    internal static float PromotionPromiseValue(SimulatedArtist artist) =>
        0.02f + 0.06f * Mathf.Clamp(artist?.evolution?.commercialPragmatism ?? 0.5f, 0f, 1f);

    /// <summary>What a promised wholesale line is worth: the same range, weighted to acts that want the record in shops past
    /// town -- the pragmatic, and those who think they could go further. Read only by the acceptance check; never shown.</summary>
    internal static float DistributionPromiseValue(SimulatedArtist artist, float prospects) =>
        0.02f + 0.06f * Mathf.Clamp(0.6f * (artist?.evolution?.commercialPragmatism ?? 0.5f) + 0.4f * prospects, 0f, 1f);

    private float PromiseValue(ContractTalk talk) =>
        (talk?.sessionPromised == true ? SessionPromiseValue(talk.Artist) : 0f) +
        (talk?.promotionPromised == true ? PromotionPromiseValue(talk.Artist) : 0f) +
        (talk?.distributionPromised == true ? DistributionPromiseValue(talk.Artist, ProspectsNow(talk.Artist)) : 0f);

    /// <summary>A wholesale house already carrying the label's line, or a distribution deal in force. The act can see it.</summary>
    private bool LabelHasWholesaleLine() => Label != null && (Label.AllCoveredRegions().Any() || Label.activeDeal != null);

    /// <summary>A record carried past the trunk: a house that takes the label's line, or a deal that covers this record.</summary>
    private bool RecordCarried(string recordId) => Label != null && (Label.AllCoveredRegions().Any() ||
        Label.RecordCoveredByActiveDeal(recordId) && (Label.activeDeal.grantedRegions?.Length ?? 0) > 0);

    private float PromisedCash(ContractTalk talk, bool withSession, bool withPromotion) =>
        (withSession ? SessionPromiseQuote() : 0f) + (withPromotion ? PromotionPromiseQuote() : 0f);

    private bool CanAffordCommitments(ContractTalk talk, float advance, out string message) {
        message = "";
        return talk?.sessionPromised != true && talk?.promotionPromised != true ||
            CanAffordWithPromises(advance, talk.sessionPromised, talk.promotionPromised, out message);
    }

    private bool CanAffordWithPromises(float advance, bool withSession, bool withPromotion, out string message) {
        message = "";
        float promised = PromisedCash(null, withSession, withPromotion);
        if (Label?.CanAffordToSign(advance + promised) == true) return true;
        string what = withSession && withPromotion ? "the promised session and trade ad" : withSession ? "the promised session" : "the promised trade ad";
        message = $"The advance plus {what} (about ${promised:N0}) would leave less than two months of overhead. " +
            "Lower the advance, drop a promise or wait for more cash.";
        return false;
    }

    private bool OpenPromiseFor(string artistId, ContractCommitmentKind kind) =>
        contractCommitments.Any(c => c.ArtistId == artistId && c.Kind == kind && c.Status == ContractCommitmentStatus.Open);

    /// <summary>Attach a funded-session promise to the package. Costs nothing now; it is scored only when the
    /// next offer is tabled, and it is paid only when the session is actually booked. Available on the tabling
    /// form and as the answer to an objection.</summary>
    public bool AddSessionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage is not (ContractTalkStage.Tabling or ContractTalkStage.Objection)) { message = "Not at that point in the talks."; return false; }
        if (talk.sessionPromised) { message = "The session is already part of your offer."; return false; }
        if (OpenPromiseFor(talk.Artist.artistId, ContractCommitmentKind.FundedSession)) {
            message = "You still owe them an earlier promised session. Keep that one before promising another."; return false;
        }
        if (!CanAffordWithPromises(CurrentOffer(talk).Advance, true, talk.promotionPromised, out message)) return false;
        talk.sessionPromised = true;
        talk.stage = ContractTalkStage.Tabling;
        message = $"You add a funded studio session within {SessionPromiseDays / 7} weeks of signing. Table the package to see where they stand.";
        Changed?.Invoke();
        return true;
    }

    private bool PlayPromise(ContractTalk talk, out string message) => AddSessionPromise(talk, out message);

    /// <summary>Take the promise back off the table before the next round. Nothing has been agreed yet.</summary>
    public bool WithdrawSessionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage != ContractTalkStage.Tabling || !talk.sessionPromised) { message = "There's no session promise on the table."; return false; }
        talk.sessionPromised = false;
        message = "The session promise is off the table.";
        Changed?.Invoke();
        return true;
    }

    /// <summary>Attach a promotion promise: at least a half-page trade ad for their first record, within
    /// <see cref="PromotionPromiseDays"/> of its release. Costs nothing now; paid when the ad is bought.</summary>
    public bool AddPromotionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage is not (ContractTalkStage.Tabling or ContractTalkStage.Objection)) { message = "Not at that point in the talks."; return false; }
        if (talk.promotionPromised) { message = "The push is already part of your offer."; return false; }
        if (OpenPromiseFor(talk.Artist.artistId, ContractCommitmentKind.FundedPromotion)) {
            message = "You still owe them an earlier promised push. Keep that one before promising another."; return false;
        }
        if (!CanAffordWithPromises(CurrentOffer(talk).Advance, talk.sessionPromised, true, out message)) return false;
        talk.promotionPromised = true;
        talk.stage = ContractTalkStage.Tabling;
        message = $"You add a {TradeAdTierName(PromotionPromiseTier).ToLowerInvariant()} trade ad for their first record within {PromotionPromiseDays / 7} weeks of its release. " +
            "Table the package to see where they stand.";
        Changed?.Invoke();
        return true;
    }

    /// <summary>Attach a distribution promise: a wholesale house carries their first record within
    /// <see cref="DistributionPromiseDays"/> of its release. Costs nothing now; placing a line costs the trip, as ever.
    /// Only a label with no line yet can promise one -- a line the act can already see is not a promise.</summary>
    public bool AddDistributionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage is not (ContractTalkStage.Tabling or ContractTalkStage.Objection)) { message = "Not at that point in the talks."; return false; }
        if (talk.distributionPromised) { message = "Distribution is already part of your offer."; return false; }
        if (LabelHasWholesaleLine()) { message = "A wholesale house already carries your line, and they can see it. There's nothing to promise."; return false; }
        if (OpenPromiseFor(talk.Artist.artistId, ContractCommitmentKind.Distribution)) {
            message = "You still owe them the distribution you promised before. Keep that one before promising it again."; return false;
        }
        talk.distributionPromised = true;
        talk.stage = ContractTalkStage.Tabling;
        message = $"You promise a wholesale house will carry their first record within {DistributionPromiseDays / 7} weeks of its release. " +
            "Table the package to see where they stand.";
        Changed?.Invoke();
        return true;
    }

    public bool WithdrawDistributionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage != ContractTalkStage.Tabling || !talk.distributionPromised) { message = "There's no distribution promise on the table."; return false; }
        talk.distributionPromised = false;
        message = "The distribution promise is off the table.";
        Changed?.Invoke();
        return true;
    }

    public bool WithdrawPromotionPromise(ContractTalk talk, out string message) {
        if (!CurrentTalk(talk, out message)) return false;
        if (talk.stage != ContractTalkStage.Tabling || !talk.promotionPromised) { message = "There's no promotion promise on the table."; return false; }
        talk.promotionPromised = false;
        message = "The promotion promise is off the table.";
        Changed?.Invoke();
        return true;
    }

    /// <summary>Written only after the contract itself has committed.</summary>
    private void RecordPromises(ContractTalk talk) {
        if (talk == null || Label == null) return;
        var artist = talk.Artist;
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        void Write(ContractCommitmentKind kind, string tag, int due, float quote) {
            int ordinal = contractCommitments.Count(c => c.ArtistId == artist.artistId);
            contractCommitments.Add(new ContractCommitment {
                Id = $"{Label.labelId}|{artist.artistId}|{tag}|{today}|{ordinal}", LabelId = Label.labelId,
                ArtistId = artist.artistId, ArtistName = artist.stageName, Kind = kind,
                Renewal = talk.IsRenewal, AgreedDay = today, DueDay = due, QuotedCost = quote, Status = ContractCommitmentStatus.Open });
        }
        if (talk.sessionPromised) {
            Write(ContractCommitmentKind.FundedSession, "session", today + SessionPromiseDays, SessionPromiseQuote());
            Note($"Promised {artist.stageName} a funded session by {LocalSceneRoomService.Date(today + SessionPromiseDays).ToShortString()}.");
        }
        if (talk.promotionPromised) {
            Write(ContractCommitmentKind.FundedPromotion, "promotion", today + PromotionReleaseDeadlineDays, PromotionPromiseQuote());
            Note($"Promised {artist.stageName} a {TradeAdTierName(PromotionPromiseTier).ToLowerInvariant()} trade ad within {PromotionPromiseDays / 7} weeks of their first record " +
                $"(the record itself out by {LocalSceneRoomService.Date(today + PromotionReleaseDeadlineDays).ToShortString()}).");
        }
        if (talk.distributionPromised) {
            Write(ContractCommitmentKind.Distribution, "distribution", today + PromotionReleaseDeadlineDays, 0f);
            Note($"Promised {artist.stageName} a wholesale house for their first record within {DistributionPromiseDays / 7} weeks of its release " +
                $"(the record itself out by {LocalSceneRoomService.Date(today + PromotionReleaseDeadlineDays).ToShortString()}).");
        }
    }

    /// <summary>A paid session with the act keeps any open session promise to them. Called after the money moved.</summary>
    private void KeepSessionPromises(SimulatedArtist artist) {
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        foreach (var c in contractCommitments.Where(c => c.ArtistId == artist.artistId && c.Kind == ContractCommitmentKind.FundedSession &&
                c.Status == ContractCommitmentStatus.Open && today <= c.DueDay)) {
            c.Status = ContractCommitmentStatus.Kept; c.ResolvedDay = today;
            Note($"Kept the promise: {artist.stageName} got their session.");
        }
    }

    /// <summary>A bought trade ad of the promised size, on a record by the act, keeps an open promotion promise. Called after the money moved.</summary>
    private void KeepPromotionPromises(string recordId, TradeAdTier tier) {
        if ((int)tier < (int)PromotionPromiseTier) return;
        var record = FindReleasedRecord(recordId)?.baseRecord;
        if (record?.artistId == null) return;
        int today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
        foreach (var c in contractCommitments.Where(c => c.ArtistId == record.artistId && c.Kind == ContractCommitmentKind.FundedPromotion &&
                c.Status == ContractCommitmentStatus.Open && today >= c.AgreedDay && today <= c.DueDay)) {
            c.Status = ContractCommitmentStatus.Kept; c.ResolvedDay = today;
            c.ReleaseDay ??= Math.Max(c.AgreedDay, Math.Min(today, ReleaseDayOf(record) ?? today));
            Note($"Kept the promise: {c.ArtistName}'s record got its push.");
        }
    }

    private static int? ReleaseDayOf(Record record) =>
        record == null || record.releaseDate.year < 1900 ? null : LocalSceneRoomService.Day(record.releaseDate);

    /// <summary>The act's first record out on this label on or after the agreement, if any.</summary>
    private int? FirstReleaseSince(string artistId, int day) => ReleasedRecords
        .Where(r => r.baseRecord?.artistId == artistId).Select(r => ReleaseDayOf(r.baseRecord))
        .Where(d => d >= day).Min();

    /// <summary>Dawn check. An act that left the roster voids the promise; a passed due date is a miss. A promotion
    /// promise's clock starts when the first record comes out.</summary>
    private void ProcessContractCommitments(GameDate date) {
        int today = LocalSceneRoomService.Day(date);
        foreach (var c in contractCommitments.Where(c => c.Status == ContractCommitmentStatus.Open)) {
            var artist = ArtistManager.Instance?.GetArtist(c.ArtistId);
            if (artist == null || artist.labelId != c.LabelId || Label?.roster?.Contains(artist) != true) {
                c.Status = ContractCommitmentStatus.Void; c.ResolvedDay = today; continue;
            }
            if (c.Kind is ContractCommitmentKind.FundedPromotion or ContractCommitmentKind.Distribution && !c.ReleaseDay.HasValue &&
                    FirstReleaseSince(c.ArtistId, c.AgreedDay) is int released) {
                c.ReleaseDay = released;
                c.DueDay = Math.Min(c.DueDay, released + (c.Kind == ContractCommitmentKind.Distribution ? DistributionPromiseDays : PromotionPromiseDays));
            }
            // A wholesale house carrying any of their records since the agreement keeps it; checked before a miss.
            if (c.Kind == ContractCommitmentKind.Distribution && c.ReleaseDay.HasValue && today <= c.DueDay &&
                    ReleasedRecords.Any(r => r.baseRecord?.artistId == c.ArtistId && ReleaseDayOf(r.baseRecord) >= c.AgreedDay && RecordCarried(r.baseRecord.recordId))) {
                c.Status = ContractCommitmentStatus.Kept; c.ResolvedDay = today;
                Note($"Kept the promise: a wholesale house is carrying {c.ArtistName}'s record.");
                continue;
            }
            if (today <= c.DueDay) continue;
            c.Status = ContractCommitmentStatus.Missed; c.ResolvedDay = today;
            string broken = c.Kind switch {
                ContractCommitmentKind.FundedSession => "never booked the session it promised",
                ContractCommitmentKind.Distribution => c.ReleaseDay.HasValue ? "never got the record into shops past the trunk as it promised"
                    : "never put out the record it promised to distribute",
                _ => c.ReleaseDay.HasValue ? "never pushed the record it promised to promote" : "never put out the record it promised to promote",
            };
            artist.careerEvents.Add($"{date.year}: {Label.labelName} {broken}.");
            Note(c.Kind switch {
                ContractCommitmentKind.FundedSession => $"You never booked the session you promised {artist.stageName}. They'll remember it when the paper comes up.",
                ContractCommitmentKind.Distribution => $"You never got {artist.stageName}'s record into a wholesale house as you promised. They'll remember it when the paper comes up.",
                _ => $"You never gave {artist.stageName}'s record the push you promised. They'll remember it when the paper comes up.",
            });
            OnPromiseBroken(c, artist);
        }
    }

    private int UnansweredMissedPromises(string artistId, ContractCommitmentKind kind) => contractCommitments.Count(c =>
        c.ArtistId == artistId && c.Kind == kind && c.Status == ContractCommitmentStatus.Missed && !c.RemedyApplied);

    /// <summary>The remedy for a missed promise: the act asks more up front at renewal, and says why.</summary>
    private ContractTermSheet WithMissedPromises(ContractTermSheet ask, string artistId) {
        int sessions = UnansweredMissedPromises(artistId, ContractCommitmentKind.FundedSession);
        int pushes = UnansweredMissedPromises(artistId, ContractCommitmentKind.FundedPromotion);
        int lines = UnansweredMissedPromises(artistId, ContractCommitmentKind.Distribution);
        int missed = sessions + pushes + lines;
        if (missed == 0) return ask;
        float advance = Mathf.Ceil(ask.Advance * (1f + MissedSessionAdvancePremium * missed) / 5f) * 5f;
        string summary = ask.DemandSummary ?? "";
        if (sessions > 0) summary += sessions == 1
            ? " They remember the session you promised and never booked -- they want more up front this time."
            : $" They remember {sessions} promised sessions you never booked -- they want more up front this time.";
        if (pushes > 0) summary += pushes == 1
            ? " They remember the push you promised their record and never gave it -- they want more up front this time."
            : $" They remember {pushes} promised pushes you never gave -- they want more up front this time.";
        if (lines > 0) summary += lines == 1
            ? " They remember the distribution you promised and never delivered -- they want more up front this time."
            : $" They remember {lines} records you promised to get into shops and didn't -- they want more up front this time.";
        return new ContractTermSheet(advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing,
            ask.ArtistCreativeControl, ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, summary.Trim());
    }

    /// <summary>A completed renewal has answered every miss its ask carried.</summary>
    private void SettleMissedPromises(string artistId) {
        foreach (var c in contractCommitments.Where(c => c.ArtistId == artistId && c.Status == ContractCommitmentStatus.Missed)) c.RemedyApplied = true;
    }

    /// <summary>Open promise for the roster card, if any.</summary>
    public ContractCommitment OpenCommitmentFor(string artistId) => LocalSceneRoomService.Copy(
        contractCommitments.FirstOrDefault(c => c.ArtistId == artistId && c.Status == ContractCommitmentStatus.Open));

    /// <summary>Every open promise to one act, for the roster card.</summary>
    public IReadOnlyList<ContractCommitment> OpenCommitmentsFor(string artistId) => LocalSceneRoomService.Copy(
        contractCommitments.Where(c => c.ArtistId == artistId && c.Status == ContractCommitmentStatus.Open).ToList()).AsReadOnly();

    private List<ContractCommitment> CaptureContractCommitments() =>
        contractCommitments.Count == 0 ? null : LocalSceneRoomService.Copy(contractCommitments);

    private static bool ValidateContractCommitments(List<ContractCommitment> saved, string labelId, out string message) {
        message = "The saved contract commitments are invalid; no player state was restored.";
        if (saved == null) return true; // Older saves hold no promises; none are invented.
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in saved) {
            if (c == null || string.IsNullOrEmpty(c.Id) || !ids.Add(c.Id) || c.LabelId != labelId || string.IsNullOrEmpty(c.ArtistId) ||
                !Enum.IsDefined(c.Kind) || !Enum.IsDefined(c.Status) || c.DueDay < c.AgreedDay || c.AgreedDay < 0 ||
                !float.IsFinite(c.QuotedCost) || c.QuotedCost < 0 ||
                (c.Status == ContractCommitmentStatus.Open) == c.ResolvedDay.HasValue ||
                c.ResolvedDay < c.AgreedDay || c.RemedyApplied && c.Status != ContractCommitmentStatus.Missed ||
                c.ReleaseDay.HasValue && (c.Kind == ContractCommitmentKind.FundedSession || c.ReleaseDay < c.AgreedDay) ||
                c.Kind != ContractCommitmentKind.FundedSession && c.DueDay > c.AgreedDay + PromotionReleaseDeadlineDays) return false;
        }
        return true;
    }

    private void RestoreContractCommitments(List<ContractCommitment> saved) {
        contractCommitments.Clear();
        if (saved != null) contractCommitments.AddRange(LocalSceneRoomService.Copy(saved));
    }
}
