using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Pure derivation: no mutation of composition, reference taxonomy, RNG or world caches.</summary>
public static class SongProfileDeriver {
	public static SongProfile Derive(SongComposition song, SongTaxonomy taxonomy, PolarSongTable table) {
		if (song == null) throw new ArgumentNullException(nameof(song));
		taxonomy ??= song.demoTaxonomy ?? new SongTaxonomy { primaryGenre = song.primaryGenre, secondaryGenre = song.secondaryGenre,
			tags = PolarSongMetadataService.ClassifyLegacyTags(song.genreTagIds) };
		return Derive(Prepare(song, taxonomy, table), taxonomy.archetype, taxonomy.primaryGenre);
	}

	/// <summary>
	/// Everything Derive reads except the archetype, resolved once. A cover resolver scores one song against every
	/// archetype row with an otherwise identical taxonomy; Derive(prepared, ...) replays the same float operations
	/// in the same order for each row, so results are bit-identical to a full Derive.
	/// </summary>
	public sealed class Prepared {
		internal PolarSongTable table;
		internal SongComposition song;
		internal float[] variation;
		internal readonly List<float[]> modifiers = new();
		internal bool hasWordDensity, hasProduction, wallOfSound, shifting, complex, instrumental, reflect, plasticityFrozen;
		internal float wordDensity, wordDensityBlend, productionDelta, productionStep, instrumentationDelta, techniqueDelta, formStep;
		internal float frozenPlasticity, densityPenalty, formPenalty;
	}

	public static Prepared Prepare(SongComposition song, SongTaxonomy taxonomy, PolarSongTable table) {
		var p = new Prepared { table = table, song = song };
		var variation = CompositionShapeVariation.ActiveVersion == 1 && song.shapeVariationSchemaVersion == 2 ? CompositionShapeVariation.V1Offsets(song) :
			CompositionShapeVariation.ActiveVersion > 0 ? song.shapeVariation : song.repertoireVariation;
		if (variation != null) {
			if(variation.Length != SongProfile.AxisCount || variation.Any(v => !float.IsFinite(v))) throw new InvalidOperationException("Invalid persistent song variation");
			p.variation = variation;
		}
		foreach (var mode in (taxonomy.lyricModes ?? Array.Empty<SongLyricMode>()).Distinct()) p.modifiers.Add(table.LyricModifiers.GetValueOrDefault(mode.ToString()));
		foreach (var approach in (taxonomy.vocalApproaches ?? Array.Empty<SongVocalApproach>()).Distinct()) p.modifiers.Add(table.VocalModifiers.GetValueOrDefault(approach.ToString()));
		if (song.wordDensity.HasValue) { p.hasWordDensity = true; p.wordDensity = Clamp(song.wordDensity.Value); p.wordDensityBlend = table.N("wordDensityBlend"); }
		if (taxonomy.production != SongProduction.Unknown) { p.hasProduction = true; p.productionDelta = ((int)taxonomy.production - (int)SongProduction.LiveInRoom) * table.N("productionDemandStep"); }
		var tags = taxonomy.tags ?? new PolarSongTags();
		p.instrumentationDelta = (tags.instrumentation ?? Array.Empty<string>()).Distinct().Count() * table.N("instrumentationDemandStep");
		p.techniqueDelta = (tags.technique ?? Array.Empty<string>()).Distinct().Count() * table.N("techniqueDemandStep");
		if ((tags.production ?? Array.Empty<string>()).Any(t => t is "WallOfSound" or "wall-of-sound")) { p.wallOfSound = true; p.productionStep = table.N("productionDemandStep"); }
		var meter = taxonomy.meterOverride ?? song.defaultMeter;
		var form = taxonomy.formOverride ?? song.defaultForm;
		p.shifting = meter == SongMeter.Shifting;
		p.complex = form == SongForm.ThroughComposed;
		if (p.shifting || p.complex) p.formStep = table.N("formDemandStep");
		p.instrumental = taxonomy.vocalPresence == SongVocalPresence.Instrumental || (taxonomy.lyricModes ?? Array.Empty<SongLyricMode>()).Contains(SongLyricMode.Instrumental);
		p.reflect = CompositionShapeVariation.ActiveVersion == 2 && song.shapeVariationSchemaVersion == 2;
		p.plasticityFrozen = song.plasticityFrozen && song.plasticity.HasValue;
		if (p.plasticityFrozen) p.frozenPlasticity = Clamp(song.plasticity.Value);
		else {
			p.densityPenalty = table.N("plasticityDensityPenalty");
			if (p.complex) p.formPenalty = table.N("plasticityFormPenalty");
		}
		return p;
	}

	/// <summary>The profile for one archetype; Unknown infers the genre's fallback archetype as Derive does.</summary>
	public static SongProfile Derive(Prepared prepared, SongArchetype archetype, Genre primaryGenre) {
		var table = prepared.table;
		var song = prepared.song;
		bool inferred = archetype == SongArchetype.Unknown;
		if (inferred) archetype = Enum.Parse<SongArchetype>(table.Prior(primaryGenre).Fallback);
		var row = table.Row(archetype);
		var p = new SongProfile { axes = (float[])row.Axes.Clone(), archetype = archetype, inferredTaxonomy = inferred, mappingVersion = table.Version };
		if (prepared.variation != null) for(int i=0;i<p.axes.Length;i++) p.axes[i] += prepared.variation[i];
		foreach (var delta in prepared.modifiers) Apply(p, delta);
		if (prepared.hasWordDensity) p[SongAxis.LyricDelivery] = Lerp(p[SongAxis.LyricDelivery], prepared.wordDensity, prepared.wordDensityBlend);
		if (prepared.hasProduction) p[SongAxis.StudioCraft] += prepared.productionDelta;
		p[SongAxis.Ensemble] += prepared.instrumentationDelta;
		p[SongAxis.StudioCraft] += prepared.techniqueDelta;
		if (prepared.wallOfSound) p[SongAxis.StudioCraft] += prepared.productionStep;
		if (prepared.shifting) p[SongAxis.Musicianship] += prepared.formStep;
		if (prepared.complex) p[SongAxis.Ensemble] += prepared.formStep;
		if ((CompositionShapeVariation.ActiveVersion > 0 && row.Axes[0] == 0 && row.Axes[1] == 0 && row.Axes[4] == 0) || prepared.instrumental) {
			p[SongAxis.VocalPower] = p[SongAxis.VocalNuance] = p[SongAxis.LyricDelivery] = 0;
		}
		for (int i = 0; i < p.axes.Length; i++) p.axes[i] = prepared.reflect ? CompositionShapeVariation.Reflect(p.axes[i]) : Clamp(p.axes[i]);
		// Frozen composition plasticity overrides every later recording's archetype.
		p.plasticity = prepared.plasticityFrozen ? prepared.frozenPlasticity :
			Clamp(row.Plasticity - (song.wordDensity ?? 0) * prepared.densityPenalty - (prepared.complex ? prepared.formPenalty : 0));
		p.dominantDemand = MaxDemand(p.axes);
		return p;
	}
	/// <summary>axes.Take(DemandCount).Max() without the iterator, including LINQ's skip-leading-NaN rule.</summary>
	public static float MaxDemand(float[] axes) {
		int i = 0;
		float value = axes[0];
		while (float.IsNaN(value) && ++i < SongProfile.DemandCount) value = axes[i];
		for (i++; i < SongProfile.DemandCount; i++) if (axes[i] > value) value = axes[i];
		return value;
	}
	private static void Apply(SongProfile p, float[] delta) { if (delta != null) for (int i = 0; i < p.axes.Length; i++) p.axes[i] += delta[i]; }
	public static float Clamp(float v) => Math.Clamp(v, 0f, 1f);
	public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
