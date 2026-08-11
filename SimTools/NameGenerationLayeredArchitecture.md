# Name Generation — Six-Layer Architecture

Extends the Phase-1 overhaul (`NameGenerationOverhaulDirective.md`). Phase 1 replaced the switch
generators with a decoupled Core (tagged lexicon + Tracery grammar + Markov + NameLab tuner) on a
dedicated RNG stream. This layer adds the six-subsystem semantic stack + performance model from the
design docs, built **additively** on top of that Core: the existing grammar/lexicon keep working
unchanged, and each new layer adds capability that degrades gracefully until the data is authored.

All Core code is Godot-free (`Systems/Naming/Core`, namespace `LabelMan.Naming`). Everything below is
covered by the standalone regression harness at `SimTools/NamingCoreTests` (**137 assertions, all
green**). Run it with:

```bash
dotnet run --project SimTools/NamingCoreTests
```

The harness is excluded from the game build via a `Compile Remove` in `Label Man.csproj`, and is the
permanent test backend the directive calls for.

---

## The layers (all implemented + tested)

| # | Layer | File | What it does |
|---|-------|------|--------------|
| 1 | Genre Parameter Block | `GenreProfile.cs` | Voice vector (11 scalars), tag affinities, mood threshold, orthography, era curve. Inheritance tree (`BluesRoot → Blues/RnB/BluesRock → BritishBlues`) so a genre declares only deltas. Resolved profiles are frozen. |
| 2 | Template Constraint DSL | `TemplateEngine.cs` | Typed addressable slots `%pos#k:inflect%`, ontology filters, a constraint algebra (distinct / same / mood.match internal+directed / register bounds) with failure modes, gates, load-time satisfiability validation, and the Layer-1 §5 post-processor pipeline (apostrophe-drop, numeral style, title case, punctuation). |
| 3 | Tag Ontology | `TagOntology.cs` | Five axes: DOMAIN tree (with **precomputed closure bitsets** — doc 7's biggest win, one bitwise-AND per filter), flat MOOD, ordered REGISTER, ERA idiom buckets, LOCALE. Classifies each word's **existing** freeform tags onto the axes at load — no second tagging pass in the data files. |
| 4 | Mood Compatibility Graph | `MoodGraph.cs` | 19 moods, symmetric weighted adjacency stored as a flat `float[361]`. `mood.match` = MAX-within-pair / MIN-across-pairs. Draw-time biasing, connectivity validation, bridge finding. |
| 5 | Blend Resolution | `BlendResolver.cs` | Scalars lerp, sets union, categoricals defer to dominance. Per-dimension policy overrides, suppression union, mood-connectivity repair (lower threshold, else inject a bridge mood, else reject), era intersect with late-skew, template interleave, and year-parameterized succession (Ska→Rocksteady→Reggae). |
| 6 | Irregular Inflection | `Inflection.cs` | Lemma-first morphology: irregular verb (~85) + plural tables, gerund/3s/possessive/comparative rules, the -o→-oes/-os trap via domain default, dual-form (shone/shined) by genre+mood, US/UK variants (burnt/burned), orthographic normalizer, memoized. |
| 7 | Performance / Caching | `NamingCache.cs` | `NameModels` L0 bundle; `FilteredPool` prefix-sum O(log n) weighted pick; `PoolCache` bounded LRU keyed by (pos, filter, epoch, orthography, genre) — era-epoch collapses the year dimension 10→4; Bloom-fronted `CollisionRegistry`. |

## How it is wired live (not orphan code)

- `NameEngine` now owns a `NameModels` bundle and **classifies the lexicon onto the ontology axes at
  construction**. Ontology closures, the flat mood matrix, and inflection memoization are live.
- **Grammar modifiers route through Layer 6**: `{noun:psych.pl}` now yields *echoes*, not *echos*;
  `.ger .past .pastpart .3s .poss .comp .sup` are available in `grammar.json` immediately.
- `NameEngine.FillConstraint(symbol, ctx)` runs the Layer-2 path: gate + satisfiability prune,
  weighted pick, escalating fallback to simpler templates. `Templates` (the `TemplateEngine`) uses the
  Layer-7 `PoolCache` on the no-locked-moods fast path.
- The adapter (`NameGenerator.cs`) loads optional overrides atop the embedded defaults:
  `ontology.json`, `moods.json`, `inflection.json`, `genres.json`, `templates.json`. All optional —
  absent files simply mean the embedded defaults apply. The mood matrix is validated at load.

## Determinism / V3.1 safety

Naming already runs on its own RNG stream (Phase 1), isolated from `GD.Rand`. These layers change
*which words* naming picks but **not** the number of draws on the sim/chart/economy stream, so V3
headline metrics are unaffected by naming word choices — the whole point of the isolated stream. RNG
draw count per slot is identical on the cached and uncached paths, preserving reproducibility.

## Remaining work (data authoring, not engine)

The engine is complete; realizing full period flavor is an **incremental content pass**, exactly as the
design docs frame it. The system degrades gracefully until then (filters relax to any
affinity-positive word; unknown genres fall to a neutral profile).

1. **Tag the lexicon onto the ontology axes.** Today most `lexicon.json` tags are *style* tags
   (`psych`, `soul`) rather than DOMAIN/MOOD/REGISTER tags. Add domain leaves (`celestial`, `grit`,
   `travel`…), moods, and register to word groups so Layer-2 filters and mood coherence engage. No
   code change — `TagOntology.Classify` already sorts whatever tags are present.
2. **Author genre profiles** in `genres.json` for the full 51-genre set (bases + leaves + deltas).
   Embedded defaults cover ~30 anchors; extend/override via JSON.
3. **Author constraint templates** in `templates.json` (see `templates.sample.json` for the format and
   worked examples) and route selected genres to them from the adapter's `ChooseSongSymbol` /
   `ChooseBandSymbol` when their lexicon slices are tagged deeply enough.
4. **Blends**: register static blends (FolkRock, CountryRock, Boogaloo…) and succession chains via
   `BlendResolver`, caching the results as profiles.
5. Re-run the V3 calibration probe across the canonical seeds and **re-freeze as V3.1** (directive
   gate P1.6) once routing changes reach the live path.
