using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class ProgrammeMaterialChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("Programme material: " + reason); checks++; }
        var manager = CompetitorManager.Instance;
        var prior = manager.ProgrammeState;
        var flags = (LabelReleaseProgramme.AllocationEnabled, LabelReleaseProgramme.CapacityEnabled, LabelReleaseProgramme.MaterialSearchEnabled);
        var options = SaveGameService.TestJsonOptions;
        try {
            LabelReleaseProgramme.SetForChecks(true, true); LabelReleaseProgramme.SetMaterialSearchForChecks(true);
            var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
            var template = manager.GetAllLabels().Where(l => l.IsActive && !l.isPlayerOwned)
                .SelectMany(l => l.roster).First(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members.Count > 0 &&
                    CompositionCatalogService.GetProfessionalForGenre(GenreCatalog.MapLegacy(a.primaryGenre, 1960)).Count > 0);
            var label = new AILabel { labelId = "material-firm", labelName = "Material Fixture", tier = LabelTier.Independent,
                status = LabelStatus.Stable, cashReserves = 1000000, homeCityId = "memphis", scoutingAbility = .5f };
            manager.RegisterLabel(label);
            SimulatedArtist Act(string id) {
                var a = JsonSerializer.Deserialize<SimulatedArtist>(JsonSerializer.Serialize(template, options), options);
                a.artistId = "material-" + id; a.stageName = id; a.labelId = label.labelId; a.isPlayerOwned = false; label.roster.Add(a); return a;
            }
            var act = Act("one"); var second = Act("two"); var third = Act("three");
            Genre genre = GenreCatalog.MapLegacy(act.primaryGenre, 1960);
            var date = new GameDate(1960, 3, 4);
            var song = CompositionCatalogService.GetProfessionalForGenre(genre).First(s => !s.rights.exclusiveHold && s.originYear <= 1960 &&
                (!PolarSongBehavior.UsePolarFitSelection || !PolarSongBehavior.Refuses(PolarSongBehavior.Fit(s, act, genre, 1960, label: label), act, false)));
            ReleaseSlateItem Item(SimulatedArtist a, string id) {
                var i = new ReleaseSlateItem { Id = "material-slot-" + id, LabelId = label.labelId, ArtistId = a.artistId,
                    ContractSequence = a.contractSequence, Format = ReleaseFormat.Single, State = ReleaseSlateState.Proposed,
                    IntendedWeek = 15, DueWeek = 15, ReadyWeek = 14, ProposedWeek = 10 };
                state.Items.Add(i); return i;
            }
            var item = Item(act, "one"); var another = Item(second, "two");
            float cash = label.cashReserves;
            Check(!manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 14, 0, 1000) && item.MaterialSearch == null && label.cashReserves == cash,
                "work that cannot arrive before the planned session creates no charge");
            Check(!manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 10, 0, 150), "expected benefit must exceed the fee");
            Check(!manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 10, float.NaN, 1000), "nonfinite reads cannot enter saved work");
            float reserve = EconomicLabelLifecycle.CommitmentReserve(label, false);
            label.cashReserves = reserve + 149;
            Check(!manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 10, 0, 1000) && item.MaterialSearch == null,
                "supply cannot spend the operating and programme reserve");
            label.cashReserves = cash;
            GameRandom.Seed(12345); float expected = GameRandom.Randf(); GameRandom.Seed(12345);
            Check(manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 10, 0, 1000), "a worthwhile funded request starts before the deadline");
            Check(expected == GameRandom.Randf(), "material requests do not consume the global random stream");
            Check(Math.Abs(label.cashReserves - (cash - 150)) < .1 && Math.Abs(label.company.cashMovements[LabelCashKind.OtherOperating] + 150) < .1,
                "the existing company journal books the supply expense once");
            Check(item.MaterialSearch.DeliveryWeek == 11 && item.MaterialSearch.SongId == song.songId && item.MaterialSearch.PaidFee == 150,
                "the request freezes its specific composition, fee and delivery week");
            Check(!manager.TryStartProgrammeMaterial(item, label, act, song, true, date, 10, 0, 1000) && label.cashReserves == cash - 150,
                "replaying the same request cannot repay or redraw it");
            Check(!manager.TryStartProgrammeMaterial(another, label, second, song, true, date, 10, 0, 1000), "one supplier cannot take two simultaneous delivery jobs");
            var sameAct = Item(act, "same-act");
            Check(!manager.TryStartProgrammeMaterial(sameAct, label, act, song, false, date, 10, 0, 1000), "an act has at most one work item in preparation");
            var cover = CompositionCatalogService.GetStandardsForGenre(genre).First(s => s.originYear <= 1960 && !s.rights.exclusiveHold);
            Check(manager.TryStartProgrammeMaterial(another, label, second, cover, false, date, 10, 0, 1000), "another act can rehearse a specific cover alongside supply");
            Check(label.cashReserves == cash - 150 && another.MaterialSearch.PaidFee == 0 && another.MaterialSearch.ReferenceMasterId != null,
                "cover rehearsal follows the player's no-fee rule and freezes a reference");
            var thirdItem = Item(third, "three");
            Check(!manager.TryStartProgrammeMaterial(thirdItem, label, third, cover, false, date, 10, 0, 1000), "the label has at most two concurrent material preparations");
            manager.AdvanceProgrammeMaterial(item, label, act, date, 10);
            Check(item.MaterialSearch.State == ProgrammeMaterialState.Pending, "a request cannot deliver in its starting week");
            string json = JsonSerializer.Serialize(state, options);
            var restored = JsonSerializer.Deserialize<ReleaseProgrammeState>(json, options);
            Check(JsonSerializer.Serialize(restored, options) == json && restored.Items[0].MaterialSearch.SupplierId == item.MaterialSearch.SupplierId,
                "the supplier commitment and work timing roundtrip without a fresh quote");
            Check(SaveGameService.WritingVersion == (LabelReleaseProgramme.LpAssemblyEnabled ? 30 : 29), "active material work requires save v29 (v30 with LP assembly)");
            LabelReleaseProgramme.SetMaterialSearchForChecks(false);
            Check(SaveGameService.WritingVersion == (LabelReleaseProgramme.LpAssemblyEnabled ? 30 : 29), "retained work still requires v29 with the switch off");
            LabelReleaseProgramme.SetMaterialSearchForChecks(true);
            manager.AdvanceProgrammeMaterial(item, label, act, date, 11);
            Check(item.MaterialSearch.State == ProgrammeMaterialState.Ready && label.cashReserves == cash - 150,
                "delivery supplies the same song without a second charge");
            manager.AdvanceProgrammeMaterial(item, label, act, date, 11);
            var record = new Record { recordId = "material-record", artistId = act.artistId, labelId = label.labelId, primaryGenre = genre, format = ReleaseFormat.Single };
            Check(manager.BuildProgrammeMaterial(item, label, act, record, genre, 1960, 10) == null, "even a ready-state row cannot bypass its saved delivery date");
            var material = manager.BuildProgrammeMaterial(item, label, act, record, genre, 1960, 11);
            Check(material?.Song.songId == song.songId, "the actual session builder uses the supplied composition");
            item.State = ReleaseSlateState.Cancelled;
            manager.AdvanceProgrammeMaterial(item, label, act, date, 12);
            Check(item.MaterialSearch.State == ProgrammeMaterialState.Cancelled && label.cashReserves == cash - 150,
                "slate cancellation frees work without a supply-fee refund");
            // Failed delivery is terminal: the same intention cannot buy another draw.
            another.MaterialSearch.BaselineRead = 1;
            manager.AdvanceProgrammeMaterial(another, label, second, date, 13);
            Check(another.MaterialSearch.State == ProgrammeMaterialState.Failed && another.MaterialSearch.Reason == "delivered-material-disappoints",
                "hearing a delivery may disappoint rather than guarantee an improvement");
            Check(!manager.TryStartProgrammeMaterial(another, label, second, song, true, date, 13, 0, 1000), "failed searches cannot retry until they manufacture a hit");
            Check(manager.TryStartProgrammeMaterial(thirdItem, label, third, song, true, date, 11, 0, 1000), "completed work releases the supplier slot for a different act");
            third.contractSequence++;
            manager.AdvanceProgrammeMaterial(thirdItem, label, third, date, 12);
            Check(thirdItem.MaterialSearch.State == ProgrammeMaterialState.Cancelled && label.cashReserves == cash - 300,
                "contract changes cancel delivery without transferring or refunding paid work");
            float own = AiRepertoireService.ExpectedRecording(AiRepertoireService.MeanOwnOriginal(label, act, 1960, out _), act, label, out _, out _, out _);
            float offer = CompetitorManager.ProgrammeMaterialExpectation(label, act, song, true, 1960, out _);
            float onlyOwn = CompetitorManager.DefaultSessionExpectation(label, act, genre, 1960, new());
            float withOffer = CompetitorManager.DefaultSessionExpectation(label, act, genre, 1960, new() { (song, true) });
            Check(onlyOwn == own && withOffer >= Math.Min(own, offer) - 1e-6f && withOffer <= Math.Max(own, offer) + 1e-6f,
                "the no-search baseline is the session's expected cut on the offer's recorded-quality scale");
            var held = new SongComposition { songId = "held", originYear = 1960, rights = new SongRightsProfile { exclusiveHold = true,
                exclusiveHoldUntilWeek = 20, controllerLabelId = "other-label" } };
            Check(!CompetitorManager.CanObtainProgrammeSong(held, label, true, 1960, 10), "another label's exclusive hold blocks new supply");
            held.rights.controllerLabelId = label.labelId;
            Check(CompetitorManager.CanObtainProgrammeSong(held, label, true, 1960, 10), "the actual controlling label can use its own held material");
            held.rights.exclusiveHold = false; held.rights.compulsoryMechanicalAvailable = false;
            Check(!CompetitorManager.CanObtainProgrammeSong(held, label, false, 1960, 10), "a cover requires the existing rights route");
            manager.SetProgrammeForChecks(new ReleaseProgrammeState()); LabelReleaseProgramme.SetMaterialSearchForChecks(false);
            Check(SaveGameService.WritingVersion == (LabelReleaseProgramme.LpAssemblyEnabled ? 30 : 28) && !JsonSerializer.Serialize(new ReleaseSlateItem(), options).Contains("MaterialSearch"),
                "old programme worlds keep v28 and omit the optional material field");
            GD.Print($"PROGRAMME_MATERIAL_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
        } finally {
            manager.SetProgrammeForChecks(prior); LabelReleaseProgramme.SetForChecks(flags.Item1, flags.Item2);
            LabelReleaseProgramme.SetMaterialSearchForChecks(flags.Item3);
        }
    }
}
