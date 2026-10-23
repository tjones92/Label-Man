using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class LpAssemblyChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("LP assembly: " + reason); checks++; }
        var manager = CompetitorManager.Instance;
        var prior = manager.ProgrammeState;
        var flags = (LabelReleaseProgramme.AllocationEnabled, LabelReleaseProgramme.CapacityEnabled, LabelReleaseProgramme.LpAssemblyEnabled);
        var options = SaveGameService.TestJsonOptions;
        try {
            LabelReleaseProgramme.SetForChecks(true, true); LabelReleaseProgramme.SetLpAssemblyForChecks(true);
            var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
            var template = manager.GetAllLabels().Where(l => l.IsActive && !l.isPlayerOwned)
                .SelectMany(l => l.roster).First(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members.Count > 0);
            AILabel Firm(string id) {
                var l = new AILabel { labelId = id, labelName = id, tier = LabelTier.Independent, status = LabelStatus.Stable,
                    cashReserves = 1000000, homeCityId = "memphis", productionQuality = .6f, distributionStrength = .6f };
                manager.RegisterLabel(l); return l;
            }
            var label = Firm("lp-firm"); var rival = Firm("lp-rival");
            SimulatedArtist Act(string id) {
                var a = JsonSerializer.Deserialize<SimulatedArtist>(JsonSerializer.Serialize(template, options), options);
                a.artistId = "lp-" + id; a.stageName = id; a.labelId = label.labelId; a.isPlayerOwned = false;
                a.releasedSingleIds = new List<string>(); label.roster.Add(a); return a;
            }
            GameDate today = TimeManager.Instance.CurrentDate;
            int serial = 0;
            string Single(SimulatedArtist a, AILabel owner, int peak, int weeksAgo = 4) {
                var r = new Record { recordId = $"lp-single-{++serial}", title = $"Side {serial}", artistId = a.artistId, artistName = a.stageName,
                    labelId = owner.labelId, format = ReleaseFormat.Single, primaryGenre = a.primaryGenre, releaseDate = today.AddDays(-7 * weeksAgo),
                    hookStrength = .6f, productionQuality = .6f, danceability = .5f };
                ChartManager.Instance.ReleaseRecord(r);
                ChartManager.Instance.GetRecordRuntimeData(r.recordId).peakPosition = peak;
                a.releasedSingleIds.Add(r.recordId);
                manager.NoteMasterControl(owner.labelId, r.recordId);
                return r.recordId;
            }
            ReleaseSlateItem Slot(SimulatedArtist a, string id) => new() { Id = "lp-slot-" + id, LabelId = label.labelId, ArtistId = a.artistId,
                ContractSequence = a.contractSequence, Format = ReleaseFormat.Album, ProposedWeek = 10, IntendedWeek = 18 };
            int year = today.year;

            var fresh = Act("fresh");
            GameRandom.Seed(4242); float expected = GameRandom.Randf(); GameRandom.Seed(4242);
            var plain = manager.BuildLpAssemblyForChecks(label, fresh, Slot(fresh, "fresh"), year);
            Check(expected == GameRandom.Randf(), "building proposals consumes no global random draw");
            Check(plain.Kind == LpProposalKind.Ordinary && plain.NewSessions == plain.TrackCount && plain.TrackCount is >= 9 and <= 12 &&
                Math.Abs(plain.ProductionMultiplier - LpAssembly.NewSessionMultiplier) < 1e-5 && plain.Reason == "ordinary-no-recent-hit",
                "an act with no controlled catalogue gets an all-new studio LP at the full session rate");
            Check(manager.BuildLpAssemblyForChecks(label, fresh, Slot(fresh, "fresh"), year).TrackCount == plain.TrackCount,
                "the planned length is keyed to the intention, not redrawn");

            var young = Act("young");
            string hit = Single(young, label, 18);
            var led = manager.BuildLpAssemblyForChecks(label, young, Slot(young, "young"), year);
            Check(led.HitLedFeasible && !led.GreatestHitsFeasible && led.HitLedNet > led.OrdinaryNet,
                "one recent hit makes a hit-led LP feasible and its hit units outprice an ordinary LP");
            Check(led.Kind == LpProposalKind.HitLed && led.ReleasedMasterIds.SequenceEqual(new[] { hit }) &&
                led.NewSessions == led.TrackCount - 1 && led.ProductionMultiplier < LpAssembly.NewSessionMultiplier,
                "the hit-led LP carries the hit, records the rest new and pays no second fee for the reused master");
            Check(led.RelevantHits == 1, "one hit has not earned a greatest-hits package");

            var star = Act("star");
            var hits = new[] { Single(star, label, 3, 40), Single(star, label, 9, 30), Single(star, label, 25, 20) };
            for (int n = 0; n < 5; n++) Single(star, label, n < 2 ? 70 : 0, 60 + n);
            string formerLabel = Single(star, rival, 1, 80);
            var package = manager.BuildLpAssemblyForChecks(label, star, Slot(star, "star"), year);
            Check(package.ControlledSingles == 8 && package.RelevantHits == 3, "only singles this label controls count; a former label's #1 does not");
            Check(package.GreatestHitsFeasible && package.Kind == LpProposalKind.GreatestHits && package.NewSessions == 0 &&
                package.ReleasedMasterIds.Take(3).SequenceEqual(hits) && !package.ReleasedMasterIds.Contains(formerLabel) &&
                package.TrackCount == 8 && Math.Abs(package.ProductionMultiplier - .6f) < 1e-5,
                "three relevant hits and an honest catalogue make an all-reuse package led by the hits");

            var thin = Act("thin");
            foreach (int peak in new[] { 2, 5, 11 }) Single(thin, label, peak, 70);
            var shortPackage = manager.BuildLpAssemblyForChecks(label, thin, Slot(thin, "thin"), year);
            Check(!shortPackage.GreatestHitsFeasible && shortPackage.Kind == LpProposalKind.Ordinary &&
                shortPackage.Reason == "ordinary-catalogue-too-short-for-package", "three hits alone are not a complete package");

            foreach (string id in hits) ChartManager.Instance.RegisterCompUse(id);
            var repeat = manager.BuildLpAssemblyForChecks(label, star, Slot(star, "star-again"), year);
            Check(repeat.RelevantHits == 0 && !repeat.GreatestHitsFeasible, "hits already packaged cannot fund another package");

            var runtime = ChartManager.Instance.GetRecordRuntimeData(hit);
            manager.TransferRecordToLabel(runtime, rival, null);
            Check(state.MasterControl[hit].LabelId == rival.labelId &&
                !manager.BuildLpAssemblyForChecks(label, young, Slot(young, "young-sold"), year).HitLedFeasible,
                "a sold master leaves the seller's catalogue");
            manager.TransferRecordToLabel(runtime, label, null);

            state.MasterControl[hit].StudioLpUses = 1;
            Check(!manager.BuildLpAssemblyForChecks(label, young, Slot(young, "young-used"), year).HitLedFeasible,
                "a hit already on a studio LP does not lead another");
            state.MasterControl[hit].StudioLpUses = 0;

            ReleaseSlateItem Expired(SimulatedArtist a, int n) => new() { Id = $"lp-expired-{a.artistId}-{n}", LabelId = label.labelId,
                ArtistId = a.artistId, Format = ReleaseFormat.Single, PreparedRecord = new Record { recordId = $"lp-shelf-{a.artistId}-{n}",
                    title = "Unissued", artistId = a.artistId, labelId = label.labelId, format = ReleaseFormat.Single,
                    hookStrength = .3f + n * .05f, productionQuality = .5f, danceability = .5f } };
            for (int n = 0; n < LpAssembly.ShelfPerAct + 2; n++) manager.ShelveExpiredMaster(Expired(fresh, n));
            Check(state.Shelf.Count(s => s.ArtistId == fresh.artistId) == LpAssembly.ShelfPerAct, "each act's shelf is bounded");
            var shelfLp = manager.BuildLpAssemblyForChecks(label, fresh, Slot(fresh, "fresh-shelf"), year);
            Check(shelfLp.ShelvedMasterIds.Count == LpAssembly.MaxShelvedOnStudioLp && shelfLp.ShelvedMasterIds[0] == $"lp-shelf-{fresh.artistId}-7" &&
                shelfLp.NewSessions == shelfLp.TrackCount - LpAssembly.MaxShelvedOnStudioLp && shelfLp.OrdinaryNet > plain.OrdinaryNet,
                "an ordinary LP takes the strongest shelved masters and saves their session fees");
            var gone = new SimulatedArtist { artistId = "lp-gone" };
            label.roster.Remove(fresh);
            manager.ShelveExpiredMaster(Expired(star, 0));
            Check(state.Shelf.All(s => s.ArtistId != fresh.artistId), "a departed act's shelf is cleared");
            manager.ShelveExpiredMaster(Expired(gone, 0));
            Check(state.Shelf.All(s => s.ArtistId != gone.artistId), "a master of an act no longer signed is not shelved");

            string json = JsonSerializer.Serialize(state, options);
            var restored = JsonSerializer.Deserialize<ReleaseProgrammeState>(json, options);
            Check(JsonSerializer.Serialize(restored, options) == json && restored.MasterControl.Count == state.MasterControl.Count &&
                restored.Shelf[0].Record.recordId == state.Shelf[0].Record.recordId, "ledger and shelf roundtrip");
            var withPlan = new ReleaseSlateItem { LpAssembly = package };
            var planJson = JsonSerializer.Serialize(withPlan, options);
            Check(JsonSerializer.Serialize(JsonSerializer.Deserialize<ReleaseSlateItem>(planJson, options), options) == planJson, "a chosen plan roundtrips");
            Check(SaveGameService.WritingVersion == 30, "LP assembly writes save v30");
            manager.SetProgrammeForChecks(new ReleaseProgrammeState()); LabelReleaseProgramme.SetLpAssemblyForChecks(false);
            string empty = JsonSerializer.Serialize(new ReleaseProgrammeState(), options) + JsonSerializer.Serialize(new ReleaseSlateItem(), options);
            Check(SaveGameService.WritingVersion == 28 && !empty.Contains("MasterControl") && !empty.Contains("Shelf") && !empty.Contains("LpAssembly"),
                "programme worlds without assembly keep v28 and omit the new fields");
            GD.Print($"LP_ASSEMBLY_CHECK passed checks={checks} studio={StudioParentLabels.Enabled}");
        } finally {
            manager.SetProgrammeForChecks(prior); LabelReleaseProgramme.SetForChecks(flags.Item1, flags.Item2);
            LabelReleaseProgramme.SetLpAssemblyForChecks(flags.Item3);
        }
    }
}
