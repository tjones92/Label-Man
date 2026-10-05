using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Pure derivation: no mutation of composition, reference taxonomy, RNG or world caches.</summary>
public static class SongProfileDeriver {
	public static SongProfile Derive(SongComposition song, SongTaxonomy taxonomy, PolarSongTable table) {
		if (song == null) throw new ArgumentNullException(nameof(song));
		taxonomy ??= song.demoTaxonomy ?? new SongTaxonomy { primaryGenre = song.primaryGenre, secondaryGenre = song.secondaryGenre,
			tags = PolarSongMetadataService.ClassifyLegacyTags(song.genreTagIds) };
		bool inferred = taxonomy.archetype == SongArchetype.Unknown;
		var archetype = inferred ? Enum.Parse<SongArchetype>(table.Prior(taxonomy.primaryGenre).Fallback) : taxonomy.archetype;
		var row = table.Row(archetype);
		var p = new SongProfile { axes = (float[])row.Axes.Clone(), archetype = archetype, inferredTaxonomy = inferred, mappingVersion = table.Version };
		var variation = CompositionShapeVariation.ActiveVersion == 1 && song.shapeVariationSchemaVersion == 2 ? CompositionShapeVariation.V1Offsets(song) :
			CompositionShapeVariation.ActiveVersion > 0 ? song.shapeVariation : song.repertoireVariation;
		if (variation != null) {
			if(variation.Length != SongProfile.AxisCount || variation.Any(v => !float.IsFinite(v))) throw new InvalidOperationException("Invalid persistent song variation");
			for(int i=0;i<p.axes.Length;i++) p.axes[i] += variation[i];
		}
		foreach (var mode in (taxonomy.lyricModes ?? Array.Empty<SongLyricMode>()).Distinct()) Apply(p, table.LyricModifiers.GetValueOrDefault(mode.ToString()));
		foreach (var approach in (taxonomy.vocalApproaches ?? Array.Empty<SongVocalApproach>()).Distinct()) Apply(p, table.VocalModifiers.GetValueOrDefault(approach.ToString()));
		if (song.wordDensity.HasValue) p[SongAxis.LyricDelivery] = Lerp(p[SongAxis.LyricDelivery], Clamp(song.wordDensity.Value), table.N("wordDensityBlend"));
		if (taxonomy.production != SongProduction.Unknown) p[SongAxis.StudioCraft] += ((int)taxonomy.production - (int)SongProduction.LiveInRoom) * table.N("productionDemandStep");
		var tags = taxonomy.tags ?? new PolarSongTags();
		p[SongAxis.Ensemble] += (tags.instrumentation ?? Array.Empty<string>()).Distinct().Count() * table.N("instrumentationDemandStep");
		p[SongAxis.StudioCraft] += (tags.technique ?? Array.Empty<string>()).Distinct().Count() * table.N("techniqueDemandStep");
		if ((tags.production ?? Array.Empty<string>()).Any(t => t is "WallOfSound" or "wall-of-sound")) p[SongAxis.StudioCraft] += table.N("productionDemandStep");
		var meter = taxonomy.meterOverride ?? song.defaultMeter;
		var form = taxonomy.formOverride ?? song.defaultForm;
		if (meter == SongMeter.Shifting) p[SongAxis.Musicianship] += table.N("formDemandStep");
		bool complex = form == SongForm.ThroughComposed;
		if (complex) p[SongAxis.Ensemble] += table.N("formDemandStep");
		if ((CompositionShapeVariation.ActiveVersion > 0 && row.Axes[0] == 0 && row.Axes[1] == 0 && row.Axes[4] == 0) || taxonomy.vocalPresence == SongVocalPresence.Instrumental || (taxonomy.lyricModes ?? Array.Empty<SongLyricMode>()).Contains(SongLyricMode.Instrumental)) {
			p[SongAxis.VocalPower] = p[SongAxis.VocalNuance] = p[SongAxis.LyricDelivery] = 0;
		}
		for (int i = 0; i < p.axes.Length; i++) p.axes[i] = CompositionShapeVariation.ActiveVersion == 2 && song.shapeVariationSchemaVersion == 2 ? CompositionShapeVariation.Reflect(p.axes[i]) : Clamp(p.axes[i]);
		// Frozen composition plasticity overrides every later recording's archetype.
		p.plasticity = song.plasticityFrozen && song.plasticity.HasValue ? Clamp(song.plasticity.Value) :
			Clamp(row.Plasticity - (song.wordDensity ?? 0) * table.N("plasticityDensityPenalty") - (complex ? table.N("plasticityFormPenalty") : 0));
		p.dominantDemand = p.axes.Take(SongProfile.DemandCount).Max();
		return p;
	}
	private static void Apply(SongProfile p, float[] delta) { if (delta != null) for (int i = 0; i < p.axes.Length; i++) p.axes[i] += delta[i]; }
	public static float Clamp(float v) => Math.Clamp(v, 0f, 1f);
	public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
