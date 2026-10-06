using System;
using Godot;

/// <summary>Frozen performances isolate consumers without world/economy feedback.</summary>
public static class PolarRecordingRealizationChecks {
	public static void Run() {
		bool enabled = PolarSongBehavior.UsePolarFitSelection;
		var masters = PolarSongMetadataService.Capture();
		var table = PolarSongTable.Current;
		float blend = table.N("recordingHookBlend"), ceiling = table.N("hookCeilingStrength");
		try {
			PolarSongBehavior.UsePolarFitSelection = true;
			ClearProbes();
			PolarSongBehavior.AuditOriginalRealization = true;
			Check(PolarSongBehavior.RealizeHook(.3f, .9f) == .3f && PolarSongBehavior.RealizeHook(.9f, .2f) == .2f,
				"original probe reproduces the old hard ceiling and omitted material lift");
			PolarSongBehavior.AuditOriginalRealization = false;
			Check(PolarSongBehavior.ExecutionFactor(.95f) == 1 && PolarSongBehavior.ExecutionFactor(0) < 1,
				"near-capable performers avoid a universal tax while severe mismatches retain loss");
			Check(PolarSongBehavior.PromotionRealizationFactor(new AlbumTrack { hookStrength = .8f, productionQuality = .8f, danceability = .8f }) >
				PolarSongBehavior.PromotionRealizationFactor(new AlbumTrack { hookStrength = .2f, productionQuality = .2f, danceability = .2f }),
				"promotion distinguishes strong and weak completed performances at identical fit");
			var actor = new SimulatedArtist { artistId = "realization-fixture", members = new() { new Musician() } };
			var strong = Material("realization-strong", .9f, 1);
			var single = Recording(strong);
			SongMaterialApplicationService.Apply(single, strong, null, actor);
			Check(single.hookStrength > .3f && Near(single.productionQuality, .8f), "strong material lifts a capable performance without a general production tax");
			var cut = new AlbumTrack { hookStrength = .3f, productionQuality = .8f, danceability = .5f, quality = .49f };
			SongMaterialApplicationService.ApplyIdentityToAlbumTrack(cut, strong);
			Check(Near(single.hookStrength, cut.hookStrength) && Near(single.productionQuality, cut.productionQuality) &&
				Near(cut.quality, Quality(single)), "single and album cut realize the same frozen performance, including quality deltas");
			var weak = Material("realization-weak", .9f, .2f);
			var failed = Recording(weak);
			SongMaterialApplicationService.Apply(failed, weak, null, actor);
			Check(failed.hookStrength < single.hookStrength && failed.productionQuality < single.productionQuality, "unsupported demands still cost hook and production");
			PolarSongBehavior.AuditNoCapabilityPenalty = true;
			var recovered = Recording(Material("realization-recovered", .9f, .2f));
			SongMaterialApplicationService.Apply(recovered, Material(recovered.recordId, .9f, .2f), null, actor);
			Check(Near(recovered.hookStrength, single.hookStrength) && Near(recovered.productionQuality, single.productionQuality), "Capability probe removes only execution loss");
			ClearProbes();
			float limited = PolarSongBehavior.RealizeHook(.9f, .2f);
			PolarSongBehavior.AuditNoHookCeiling = true;
			float unlimited = PolarSongBehavior.RealizeHook(.9f, .2f);
			Check(limited < unlimited && limited > .2f && limited < .9f, "weak material constrains the hook through the blend and soft ceiling");
			ClearProbes();
			var fit = PolarSongMetadataService.Get(single.masterId).realizedFit;
			fit.identity = 0; fit.moment = 1; fit.hasMarketEvidence = true;
			float critical = PolarSongBehavior.CriticCraft(.8f, single), moment = PolarSongBehavior.MomentMultiplier(single);
			PolarSongBehavior.AuditNoCriticAdjustment = true;
			Check(PolarSongBehavior.CriticCraft(.8f, single) == .8f && PolarSongBehavior.MomentMultiplier(single) == moment,
				"critic probe leaves Moment unchanged");
			ClearProbes(); PolarSongBehavior.AuditNoMomentConversion = true;
			Check(PolarSongBehavior.MomentMultiplier(single) == 1 && PolarSongBehavior.CriticCraft(.8f, single) == critical,
				"Moment probe leaves critic unchanged");
			PolarSongBehavior.UsePolarFitSelection = false;
			Check(PolarSongBehavior.CriticCraft(.8f, single) == .8f && PolarSongBehavior.MomentMultiplier(single) == 1,
				"disabled boundary ignores committed polar fit");
			GD.Print($"POLAR_RECORDING_REALIZATION_PASS strongQuality={Quality(single):F6} failedQuality={Quality(failed):F6} " +
				$"limitedHook={limited:F6} unlimitedHook={unlimited:F6} critic={critical:F6} moment={moment:F6}");
		} finally {
			ClearProbes(); PolarSongBehavior.UsePolarFitSelection = enabled;
			PolarSongMetadataService.Restore(masters);
			Check(table.N("recordingHookBlend") == blend && table.N("hookCeilingStrength") == ceiling, "fixtures leave configuration untouched");
		}
	}
	private static SelectedSongMaterial Material(string id, float hook, float capability) {
		var taxonomy = new SongTaxonomy { archetype = SongArchetype.BrightPopNumber, primaryGenre = Genre.RockAndRoll };
		return new SelectedSongMaterial { Source = SongMaterialSource.ArtistWritten,
			Song = new SongComposition { songId = id + "-song", commercialHook = hook, plasticityFrozen = true, demoTaxonomy = taxonomy },
			PolarProposal = new PolarArrangementProposal { plannedMasterId = id, songId = id + "-song", taxonomy = taxonomy,
				mappingVersion = PolarSongTable.Current.Version, fit = new MaterialFit(capability, .8f, .5f, 1 - capability, SongAxis.VocalNuance, .1f, .1f, false) } };
	}
	private static Record Recording(SelectedSongMaterial material) => new() { recordId = material.PolarProposal.plannedMasterId,
		format = ReleaseFormat.Single, hookStrength = .3f, productionQuality = .8f, danceability = .5f };
	private static float Quality(Record record) => record.hookStrength * .5f + record.productionQuality * .3f + record.danceability * .2f;
	private static bool Near(float a, float b) => Math.Abs(a - b) < .000001f;
	private static void ClearProbes() {
		PolarSongBehavior.AuditNoHookCeiling = PolarSongBehavior.AuditNoCapabilityPenalty =
			PolarSongBehavior.AuditNoCriticAdjustment = PolarSongBehavior.AuditNoMomentConversion = false;
		PolarSongBehavior.AuditOriginalRealization = false;
	}
	private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("POLAR_RECORDING_REALIZATION_FAIL: " + message); }
}
