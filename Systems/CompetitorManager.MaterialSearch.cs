using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum ProgrammeMaterialState { Pending, Ready, Consumed, Failed, Cancelled, Declined }
public sealed class ProgrammeMaterialSearch {
    public string SongId, SupplierId, ReferenceMasterId, Reason;
    public bool Professional;
    public int StartedWeek, DeliveryWeek, HoldUntilWeek;
    public float PaidFee, ProposalRead, BaselineRead, ExpectedBenefit;
    public ProgrammeMaterialState State;
}

public partial class CompetitorManager {
    public event Action<ReleaseSlateItem, int> OnProgrammeMaterialSearch;
    internal bool HasMaterialSearchState => LabelReleaseProgramme.MaterialSearchEnabled ||
        ProgrammeState?.Items.Any(i => i.MaterialSearch != null) == true;

    private void MaterialReview(ReleaseSlateItem item, int week, ProgrammeMaterialState state, string reason) {
        item.MaterialSearch.State = state; item.MaterialSearch.Reason = reason;
        OnProgrammeMaterialSearch?.Invoke(item, week);
    }

    internal static bool CanObtainProgrammeSong(SongComposition song, AILabel label, bool professional, int year, int week) =>
        song != null && song.originYear <= year &&
        (!song.rights.exclusiveHold || song.rights.exclusiveHoldUntilWeek < week || song.rights.controllerLabelId == label.labelId) &&
        (professional || song.isPublicDomain || song.rights.compulsoryMechanicalAvailable) &&
        (!professional || RockHitOfficePlacement.CanPlace(song, label));

    private static string MaterialSupplier(SongComposition song) => song.credits
        .FirstOrDefault(c => c.writerType == WriterEntityType.ProfessionalSongwriter && !string.IsNullOrEmpty(c.writerId))?.writerId
        ?? song.rights.publisherId ?? song.songId;

    // Expected recorded quality of this act cutting this song at this label (ExpectedRecording's scale); -1 when refused.
    internal static float ProgrammeMaterialExpectation(AILabel label, SimulatedArtist artist, SongComposition song, bool professional, int year, out float selection) {
        var candidate = new ShadowCandidate { Source = professional ? SongMaterialSource.ExternalProfessional : SongMaterialSource.CoverStandard,
            Craft = song.compositionQuality, Hook = song.commercialHook, Lyric = song.lyricQuality, Originality = song.originality };
        selection = 1;
        if (PolarSongBehavior.UsePolarFitSelection) {
            var fit = PolarSongBehavior.Fit(song, artist, GenreCatalog.MapLegacy(artist.primaryGenre, year), year, label: label);
            if (PolarSongBehavior.Refuses(fit, artist, emptySongbook: false)) return -1;
            candidate.Capability = fit.Capability; candidate.Identity = fit.Identity; candidate.Moment = fit.Moment;
            selection = PolarSongBehavior.SelectionScore(fit);
        }
        return AiRepertoireService.ExpectedRecording(candidate, artist, label, out _, out _, out _);
    }

    // What the existing session policy is expected to record without a search, on the same scale as an offer read: the
    // act's own song and the same sampled pools, weighted as ChooseMaterial weights sources (source mix x polar selection).
    // The act's raw talent (qualityEstimate) omits the label's production and studio terms, so every offer beat it.
    internal static float DefaultSessionExpectation(AILabel label, SimulatedArtist artist, Genre genre, int year,
        List<(SongComposition Song, bool Professional)> sample) {
        var own = AiRepertoireService.MeanOwnOriginal(label, artist, year, out var ownFit);
        float ownExpected = AiRepertoireService.ExpectedRecording(own, artist, label, out _, out _, out _);
        float ownWeight = SongMaterialSelectionService.SourceMix(SongMaterialSource.ArtistWritten, genre, year, artist.primaryGenre) *
            (ownFit.HasValue ? PolarSongBehavior.SelectionScore(ownFit.Value) : 1);
        double sum = ownWeight * ownExpected, total = ownWeight;
        foreach (bool professional in new[] { true, false }) {
            var usable = sample.Where(s => s.Professional == professional)
                .Select(s => (Expected: ProgrammeMaterialExpectation(label, artist, s.Song, professional, year, out float selection), Selection: selection))
                .Where(s => s.Expected >= 0).ToList();
            if (usable.Count == 0) continue;
            float mix = SongMaterialSelectionService.SourceMix(professional ? SongMaterialSource.ExternalProfessional : SongMaterialSource.CoverStandard,
                genre, year, artist.primaryGenre) / usable.Count;
            foreach (var s in usable) { sum += mix * s.Selection * s.Expected; total += mix * s.Selection; }
        }
        return total > 0 ? (float)(sum / total) : ownExpected;
    }

    private float ProgrammeMaterialRead(AILabel label, SimulatedArtist artist, SongComposition song, bool professional, int year, bool heard) =>
        ProgrammeMaterialRead(label, artist, song, professional, year, heard, out _);

    private float ProgrammeMaterialRead(AILabel label, SimulatedArtist artist, SongComposition song, bool professional, int year, bool heard, out float band) {
        band = Mathf.Lerp(.20f, .08f, Math.Clamp(label.scoutingAbility, 0, 1)) * (heard ? 1 : 1.5f);
        float expectation = ProgrammeMaterialExpectation(label, artist, song, professional, year, out _);
        if (expectation < 0) return -1;
        // Stable label/act/song evidence, independent of slot and reread count. No free averaging by waiting.
        double error = ProgrammeRoll(label, artist, "material-read|" + song.songId + (heard ? "|heard" : "|offer"), 0, 20);
        return Math.Clamp(expectation + ((float)error * 2 - 1) * band, 0, 1);
    }

    internal bool TryStartProgrammeMaterial(ReleaseSlateItem item, AILabel label, SimulatedArtist artist,
        SongComposition song, bool professional, GameDate date, int week, float baseline, float benefit) {
        if (!LabelReleaseProgramme.MaterialSearchEnabled || item == null || item.MaterialSearch != null || item.PreparedRecord != null ||
            item.Format != ReleaseFormat.Single || !LabelReleaseProgramme.IsOpen(item) || label?.IsActive != true || label.isPlayerOwned ||
            artist?.labelId != label.labelId || artist.contractSequence != item.ContractSequence ||
            !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist) ||
            !CanObtainProgrammeSong(song, label, professional, date.year, week)) return false;
        int delay = professional ? (MaterialPreparationService.CommissionDeliveryDays + 6) / 7 : (MaterialPreparationService.CoverLearnDays(artist) + 6) / 7;
        float fee = professional ? MaterialPreparationService.CommissionFee : 0;
        if (!float.IsFinite(baseline) || baseline < 0 || baseline > 1 || !float.IsFinite(benefit) || benefit <= fee || week + delay > item.ReadyWeek ||
            label.cashReserves - EconomicLabelLifecycle.CommitmentReserve(label, false) < fee) return false;
        string supplier = professional ? MaterialSupplier(song) : null;
        var pending = ProgrammeState.Items.Where(i => i.MaterialSearch?.State == ProgrammeMaterialState.Pending).ToList();
        if (pending.Any(i => i.ArtistId == artist.artistId) || pending.Count(i => i.LabelId == label.labelId) >= 2 ||
            (professional && pending.Any(i => i.MaterialSearch.Professional && i.MaterialSearch.SupplierId == supplier))) return false;
        item.MaterialSearch = new ProgrammeMaterialSearch { SongId = song.songId, SupplierId = supplier, Professional = professional,
            StartedWeek = week, DeliveryWeek = week + delay, HoldUntilWeek = item.IntendedWeek + LabelReleaseProgramme.Horizon,
            PaidFee = fee, BaselineRead = baseline, ExpectedBenefit = benefit,
            ProposalRead = ProgrammeMaterialRead(label, artist, song, professional, date.year, false),
            ReferenceMasterId = professional ? null : PolarSongBehavior.Reference(song, date.year)?.masterId ?? "demo:" + song.songId };
        // Supply expense is paid once now, not capitalized, refunded, or silently added to a later session bill.
        if (fee > 0) RecordExpense(label, fee, LabelCashKind.OtherOperating);
        MaterialReview(item, week, ProgrammeMaterialState.Pending, professional ? "professional-supply-ordered" : "cover-rehearsal-started");
        return true;
    }

    internal void AdvanceProgrammeMaterial(ReleaseSlateItem item, AILabel label, SimulatedArtist artist, GameDate date, int week) {
        var search = item.MaterialSearch;
        if (search == null || search.State is not (ProgrammeMaterialState.Pending or ProgrammeMaterialState.Ready)) return;
        if (!LabelReleaseProgramme.IsOpen(item) || label?.IsActive != true || label.isPlayerOwned ||
            artist?.labelId != item.LabelId || artist.contractSequence != item.ContractSequence ||
            !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist)) {
            MaterialReview(item, week, ProgrammeMaterialState.Cancelled, "slate-or-contract-ended"); return;
        }
        if (week > search.HoldUntilWeek) { MaterialReview(item, week, ProgrammeMaterialState.Failed, "material-window-expired"); return; }
        var song = CompositionCatalogService.GetSong(search.SongId);
        if (!CanObtainProgrammeSong(song, label, search.Professional, date.year, week)) {
            MaterialReview(item, week, ProgrammeMaterialState.Failed, "material-rights-unavailable"); return;
        }
        if (search.State == ProgrammeMaterialState.Pending && week >= search.DeliveryWeek) {
            bool useful = ProgrammeMaterialRead(label, artist, song, search.Professional, date.year, true) > search.BaselineRead;
            MaterialReview(item, week, useful ? ProgrammeMaterialState.Ready : ProgrammeMaterialState.Failed,
                useful ? "material-delivered-and-heard" : "delivered-material-disappoints");
        }
    }

    private void ReviewProgrammeMaterialSearches(AILabel label, GameDate date) {
        if (!LabelReleaseProgramme.MaterialSearchEnabled) return;
        foreach (var item in ProgrammeState.Items.Where(i => i.LabelId == label.labelId && i.Format == ReleaseFormat.Single &&
            LabelReleaseProgramme.IsOpen(i) && i.PreparedRecord == null).OrderBy(i => i.IntendedWeek).ThenBy(i => i.Id, StringComparer.Ordinal)) {
            var artist = label.roster.FirstOrDefault(a => a.artistId == item.ArtistId);
            AdvanceProgrammeMaterial(item, label, artist, date, pipelineWeek);
            if (item.MaterialSearch != null || artist == null || artist.contractSequence != item.ContractSequence ||
                !GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist) || item.ReadyWeek <= pipelineWeek) continue;
            var decision = BuildDecisionContext(label, artist, date.year, date.month);
            float exposureValue = Math.Max(0, SinglePriorNetAtBookedPrice(decision)) + decision.singleProductionCost;
            Genre genre = GenreCatalog.MapLegacy(artist.primaryGenre, date.year);
            var sample = new List<(SongComposition Song, bool Professional)>();
            foreach (bool professional in new[] { true, false }) {
                var pool = professional ? CompositionCatalogService.GetProfessionalForGenre(genre) : CompositionCatalogService.GetStandardsForGenre(genre);
                if (pool.Count == 0) continue;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                // A fixed bounded sample, keyed to the original intention. A declined offer is never redrawn next week.
                for (int n = 0; n < 4; n++) {
                    var song = pool[Math.Min(pool.Count - 1, (int)(ProgrammeRoll(label, artist, item.Id + "|material-offer", item.ProposedWeek, (uint)(30 + n + (professional ? 0 : 4))) * pool.Count))];
                    if (seen.Add(song.songId) && CanObtainProgrammeSong(song, label, professional, date.year, pipelineWeek)) sample.Add((song, professional));
                }
            }
            float baseline = DefaultSessionExpectation(label, artist, genre, date.year, sample);
            if (LabelReleaseProgramme.ProjectJudgementEnabled)
                baseline = Math.Max(RoutineProjectRead(label, artist, date.year, out _), AiRepertoireService.Judge(label, artist, date.year, pipelineWeek).BestPerceived);
            var proposals = new List<(SongComposition Song, bool Professional, float Benefit, float Net)>();
            foreach (var (song, professional) in sample) {
                float read = ProgrammeMaterialRead(label, artist, song, professional, date.year, false);
                float benefit = Math.Max(0, read - baseline) * exposureValue;
                proposals.Add((song, professional, benefit, benefit - (professional ? MaterialPreparationService.CommissionFee : 0)));
            }
            var best = proposals.OrderByDescending(p => p.Net).ThenBy(p => p.Song.songId, StringComparer.Ordinal).FirstOrDefault();
            if (best.Song != null && best.Net > 0) {
                // Capacity or cash can defer this same fixed proposal, without a new sample or a payment.
                TryStartProgrammeMaterial(item, label, artist, best.Song, best.Professional, date, pipelineWeek, baseline, best.Benefit);
            } else {
                item.MaterialSearch = new ProgrammeMaterialSearch { StartedWeek = pipelineWeek, BaselineRead = baseline };
                MaterialReview(item, pipelineWeek, ProgrammeMaterialState.Declined, "wait-existing-session-policy-no-worthwhile-offer");
            }
        }
    }

    internal SelectedSongMaterial BuildProgrammeMaterial(ReleaseSlateItem item, AILabel label, SimulatedArtist artist, Record record, Genre genre, int year, int week) {
        var search = item?.MaterialSearch;
        if (!LabelReleaseProgramme.MaterialSearchEnabled || search?.State != ProgrammeMaterialState.Ready || record.format != ReleaseFormat.Single ||
            week < search.DeliveryWeek || week > search.HoldUntilWeek || artist.labelId != item.LabelId || artist.contractSequence != item.ContractSequence) return null;
        var song = CompositionCatalogService.GetSong(search.SongId);
        if (!CanObtainProgrammeSong(song, label, search.Professional, year, week)) {
            MaterialReview(item, week, ProgrammeMaterialState.Failed, "material-rights-unavailable-at-session"); return null;
        }
        return search.Professional ? SongMaterialSelectionService.BuildProfessionalForSong(label.tier, artist, record, song, genre, year)
            : SongMaterialSelectionService.BuildCoverForSong(artist, record, song, genre, year, search.ReferenceMasterId);
    }
}
