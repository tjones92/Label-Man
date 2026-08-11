# Directive: Name Generation Overhaul

## Objective

Replace the hardcoded switch-statement naming layer with a data-driven generative
engine: a tagged lexicon, a Tracery-style grammar interpreter, a properly-integrated
Markov word model, and a standalone tuning tool. The naming logic is extracted into a
plain C# core with no Godot dependency so that both the game and the tuner consume the
**same** engine and the **same** JSON data files.

Implement in phases. **Phase 1 (MVP)** is the only phase authorized to begin. Phases 2
and 3 are specified for direction only and must not be started until Phase 1's hard
gates pass and are re-frozen as V3.1.

Do not begin any Phase 1 section that changes runtime naming until the decoupled core
and its parity harness (P1.0) exist and pass. If a hard gate cannot pass within the
authorized changes, stop and report evidence instead of altering sim demand, chart, or
economy constants.

---

## Verified starting point

Treat these as repository facts, not assumptions to rediscover.

- `NameGenerator` is a Godot autoload (`Node`, `project.godot [autoload]`), singleton via
  `Instance`. `NameDatabase` derives from `Resource` but is instantiated as a plain field
  (`new NameDatabase()`), not loaded from a `.tres`. All word lists are hardcoded C#
  `string[]` fields in `NameDatabase.cs`.
- Naming draws randomness exclusively through `GD.RandRange` / `GD.Randf`
  (`NameGenerator.RandInt`/`Randf`, `NameDatabase.GetRandom`/`GetWeighted`). This is the
  **global** RNG stream, seeded by the `SimulationSeedBootstrap` autoload and shared with
  all chart/economy math.
- `Genre.cs` defines **51** genre values. `NameGenerator` has bespoke handling for ~15;
  the rest (`BaroquePop`, `SunshinePop`, `Funk`, `Reggae`, `Comedy`, `Bubblegum`,
  `HardRock`, `SingerSongwriter`, `PopRock`, etc.) fall through to
  `GenerateDefaultBandName` / `GenerateDefaultSongTitle`.
- The `MarkovChain` class (letter-level, order 2/3) is trained in `NameGenerator._Ready()`
  but its only two consumers, `GenerateMarkovPersonName` and `GenerateMarkovBandName`, have
  **zero callers**. It is dead code. Its fallback returns the literal `"Unknown"`.
- The public methods the game actually calls, and their only call sites, are:
  - `GenerateArtistName(genre, year, artistType, regionId?, labelStyle?)` — `ArtistManager.cs:348`
  - `GeneratePersonName(isMale)` — `ArtistManager.cs:466`
  - `GenerateSongTitle(genre, year, artistName?)` — `CompetitorManager.cs:1183, 2943, 3016`, and via `GenerateAlbumTitle` at `3052`
  - `GenerateLabelName(archetype)` — `AILabelFactory.cs:269`
  - (defined and available, callers to confirm during migration) `GenerateAlbumTitle`,
    `GenerateVenueName`, `GenerateRadioStationName`, `GeneratePublicationName`,
    `GenerateTourName`, `GenerateAwardName`, `GenerateSongwriterName`,
    `GenerateProducerName`, `GenerateFanClubName`, `GenerateBandMemberName`,
    `GenerateBSideTitle`, `GenerateInstrumentalTitle`.
- Exactly **5** files reference `NameGenerator.Instance`. The integration surface is small;
  the public method signatures above are the compatibility contract.
- Uniqueness is tracked in `NameDatabase` via `HashSet<string>` (`usedArtistNames`,
  `usedSongTitles`, `usedAlbumTitles`) with exact-match, lowercased keys. Artist-name
  fallback after 50 failed attempts appends `" (City)"`; song-title fallback appends `" '{yy}"`.
- Naming is invoked at entity-creation events (artist creation, record release), not inside
  a per-week hot loop.
- D7 is complete. A V3 game state (genre balances, healthy economy/chart) is frozen and is
  the baseline this directive must protect.

---

## The determinism constraint (read before writing any code)

Because current naming consumes the global `GD.Rand` stream interleaved with sim math,
moving naming onto its own RNG **will shift the global stream** exactly as a seed change
would. The frozen V3 run will therefore **not** reproduce byte-identically after this work.
That is expected and acceptable, but it means:

- **Naming gets its own dedicated deterministic RNG**, seeded independently of and never
  touching `GD.Rand`. This is a founding requirement, not an option. It guarantees that
  from V3.1 forward, tuning word lists or grammars never again perturbs sim outcomes.
- **V3 must be re-baselined to V3.1.** The acceptance test is not byte identity. It is:
  run the standard calibration probe on the frozen config across the canonical seed set,
  and confirm every headline metric stays inside the already-characterized seed-noise band
  (per memory: ~±3–4 for large pop genres, up to ~50 pts for unreachable genres; use
  2–3 seeds for anything below the ~50-pt floor). Then re-freeze as V3.1.
- The naming RNG's seed is derived deterministically from the sim seed (e.g. a fixed
  offset or hash) so runs remain fully reproducible, but on a **separate stream**.

---

## Definitions

- **Core** — the plain C# naming library (no Godot types, no `GD.*`). Depends only on
  BCL + `System.Text.Json`. Consumed identically by the game adapter and the tuner.
- **Adapter** — the thin `NameGenerator : Node` autoload that owns the seeded naming RNG,
  resolves `res://`/`user://` paths to OS paths (`ProjectSettings.GlobalizePath`), builds a
  Core `NameEngine`, and re-exposes the existing public method signatures unchanged.
- **Lexicon** — the tagged word database. A flat set of `WordEntry` records loaded from JSON.
- **WordEntry** — one word plus metadata: `word`, `partOfSpeech`, `tags` (string set),
  `genreAffinity` (genre→weight map, optional), `eraStart`/`eraEnd` (optional),
  `weight` (optional, default 1). Replaces the per-combination `string[]` arrays.
- **Grammar** — a named set of expansion rules (Tracery-style) loaded from JSON. A rule is
  a weighted list of patterns; a pattern is a template string with `#symbol#` expansions and
  `[tag,tag]` lexicon queries. Rules may nest and recurse.
- **NamingContext** — the single parameter object threaded through every generation call
  (replaces the growing loose-parameter lists). Carries at minimum: `genre`, `year`,
  `artistType`, `regionId`, `labelArchetype`, and a reference to the RNG. Extensible without
  touching call sites.
- **NameEngine** — the Core entry point. Given a top-level grammar symbol and a
  NamingContext, it expands the grammar, resolves lexicon queries, optionally splices Markov
  output, scores candidates, and enforces uniqueness.
- **IRandom** — the Core's RNG abstraction (`int Next(int maxExclusive)`, `double NextDouble()`).
  The adapter and the tuner each supply a concrete deterministic implementation.

---

## Non-goals (Phase 1)

- No change to sim demand curves, chart scoring, chart capacity, retirement constants,
  release-generation distributions, or any economy math.
- No byte-identical V3 reproduction (see determinism constraint). The gate is aggregate
  parity within seed noise, re-frozen as V3.1.
- No NameProfile persistence, thematic album coherence, trend propagation, or answer-song
  mechanics — those are Phase 2/3.
- No direct CSV/file I/O from gameplay classes at runtime. The adapter loads JSON once at
  startup; the tuner owns interactive read/write.
- No new gameplay-facing entity types in Phase 1. Migrate the existing generators only.
- No visual/UX polish on the tuner beyond a functional select-category / spin / add-word loop.

---

# Phase 1 — MVP

## P1.0 Decoupled core + parity harness (must land first)

1. Create `Systems/Naming/Core/` as a Godot-independent C# namespace: `WordEntry`,
   `Lexicon`, `GrammarEngine`, `NamingContext`, `MarkovModel`, `NameEngine`, `IRandom`,
   `DeterministicRandom`. No `using Godot;` anywhere in Core.
2. Core loads data from a directory path via `System.IO` + `System.Text.Json`. The adapter
   passes `ProjectSettings.GlobalizePath("res://Data/Naming")` (or `user://` for tuner-saved
   overrides). Core never sees a Godot type.
3. Keep `NameGenerator : Node` as the adapter. It constructs the naming RNG (dedicated
   stream, seed derived from sim seed), builds the Core `NameEngine`, and keeps **every**
   existing public method signature. The 5 caller files must compile and run unchanged.
4. **Parity harness:** a Core-level dump mode that, given a fixed IRandom seed, emits N
   generated names for each existing generator category to a text file. Capture a
   pre-refactor baseline from the current system first (temporary shim allowed), so each
   subsequent step can be diffed against intent. This harness is also the tuner's backend.

**Hard gate P1.0:** project builds; all 5 callers unchanged; Core has no Godot dependency
(verify by grep for `Godot` under `Core/`); parity harness produces deterministic output
for a fixed seed across repeated runs.

## P1.1 JSON lexicon

1. Author `Data/Naming/lexicon.json` as a flat array of `WordEntry`. Migrate every existing
   `NameDatabase` array into tagged entries (e.g. `adjectivesPsych` → entries with
   `partOfSpeech: adjective`, `tags: [psych]`; `citiesBritish` → `tags: [city, british]`).
   No word is lost; tags encode what the array name previously encoded.
2. `Lexicon.Query(partOfSpeech?, requiredTags, genre?, year?, rng)` returns a weighted random
   entry, filtered by tags, optionally biased by `genreAffinity` and era window. Missing
   filters degrade gracefully (never throw; never return `"Unknown"` — return a sensible
   fallback from the nearest satisfiable query).
3. Delete the hardcoded arrays from `NameDatabase` only after every generator reads from the
   lexicon.

**Hard gate P1.1:** every word present in the pre-refactor arrays is queryable via tags;
a tag audit lists zero orphaned/untagged words; no query path can return `"Unknown"`.

## P1.2 Grammar engine

1. Implement `GrammarEngine`: parse `Data/Naming/grammar.json`, expand a named symbol,
   resolve `#symbol#` (recursive, weighted) and `[tag,tag]` lexicon queries, apply modifiers
   (capitalize, pluralize, trim-plural, possessive) via a `.modifier` suffix syntax.
2. Port the existing pattern `switch` blocks into grammar rules, one rule-set per current
   generator (`psychBandName`, `soulSongTitle`, `countrySongTitle`, `venue.smallClub`, …).
   Patterns become data; weights replace the flat `RandInt(0, N)` uniform selection (default
   all-equal to preserve current distribution, tune later).
3. Because rules are data, the ~36 genres that currently fall through to defaults get real
   coverage by composing existing fragments with genre tags — no new C# per genre.

**Hard gate P1.2:** every current generator method is reproduced by a grammar rule-set and
now delegates to `NameEngine.Expand(symbol, context)`; the switch bodies are removed; output
for a fixed seed is structurally equivalent (same pattern shapes) to the ported intent.

## P1.3 Markov integration (scored + hybrid)

1. Replace the dead letter-level `MarkovChain` with a Core `MarkovModel` used for **invented**
   words only (obscure band nouns, surnames, label coinages) — not whole names.
2. Requirements: order-3 with backoff to order-2 (eliminate dead-ends), generate-and-score
   (produce 5–10 candidates, score on pronounceability — no triple consonants, vowel
   presence, target syllable band — reject collisions), and hybrid splicing (real lexicon
   word ~80%, Markov-coined word ~20%) at grammar-designated slots (a `#markov:tag#` symbol).
3. No `"Unknown"` fallback: if scoring rejects all candidates, fall back to a lexicon query
   for the same slot.

**Hard gate P1.3:** a 1000-sample dump for a fixed seed contains zero `"Unknown"`, zero
triple-consonant clusters, and every sample is within its declared length/syllable band.

## P1.4 NamingContext + collision handling

1. Thread `NamingContext` through `NameEngine`; the adapter builds it from the existing
   method parameters. Adding a future field must not touch the 5 caller files.
2. Uniqueness moves into Core with a pluggable scope: song titles unique per artist (as
   today), artist/label names globally. Replace exact-match-only with a near-duplicate guard
   (normalized-form + optional Levenshtein threshold). Replace the `" (City)"` /`" '{yy}"`
   fallbacks with a re-roll through a *different* grammar pattern before any suffix mutation.

**Hard gate P1.4:** no generator exposes loose parameters beyond `NamingContext`; a stress
run generating 50k artist names + 50k song titles produces no exact collisions and no
`" (City)"`-style fallback under normal lexicon size.

## P1.5 Standalone tuner

1. A Godot **tool scene** (`Systems/Naming/Tools/NameLab.tscn`, run with F6) that constructs
   a Core `NameEngine` from the same JSON files the game uses. It is a front-end, not a fork.
2. Minimum interaction loop: pick a category (grammar symbol) from a dropdown; optional genre
   / year / seed inputs; a **Spin** button that renders 20 fresh names; a text box + **Add**
   button that appends a tagged word to `lexicon.json` and takes effect on the next spin;
   a **Reload** button to hot-reload edited JSON. Setting a seed reproduces a batch exactly.
3. Saves write to the canonical `Data/Naming/*.json` (or a `user://` overlay the game also
   loads) so tuning immediately reaches the game — no second data copy.

**Hard gate P1.5:** from a cold launch of NameLab, a user can spin any category, add a new
word, and see it appear in output, without recompiling; a fixed seed reproduces a batch.

## P1.6 V3.1 re-baseline

1. Run the standard calibration probe on the frozen V3 config across the canonical seed set
   with the new naming engine active.
2. Confirm each headline metric (genre chart shares, LP unit share, chart health, economy
   aggregates) stays inside the documented seed-noise band. Investigate any excursion beyond
   noise as a real regression before proceeding.
3. Re-freeze as **V3.1** and record the new reference numbers.

**Hard gate P1.6:** all headline metrics within seed noise across seeds; V3.1 frozen and
documented. This gate closes Phase 1.

---

# Phase 2 — Identity & coherence (specified, not authorized)

- **NameProfile persistence (doc item 4):** generate one profile per artist/band/label
  (ethnicity/region flavor, naming archetype, stage-vs-legal name, label house style) and
  thread it through all future related calls (member names, songwriter credits, fan clubs,
  nicknames) so an entity reads as one consistent identity. Sub-imprints inherit parent DNA.
  Unlocks in-fiction rebrand mechanics.
- **Thematic coherence (doc item 5, first half):** roll an album mood before its tracklist,
  then bias every track's lexicon queries toward that mood's tag cluster via a lightweight
  word-association network.

# Phase 3 — Culture simulation (specified, not authorized)

- **Trend propagation:** after a hit ships with a distinctive pattern/word, temporarily bump
  that pattern/word's weight sim-wide (models 60s copycatting). Ties naming into the economy.
- **Answer songs / knockoffs:** rival labels mutate a chart-topping title into a competitor
  track. Hook for the piracy/lawsuit subsystem.

---

## Tooling & maintainability notes (carry through all phases)

- Version naming data by era/expansion so a future "70s layer" adds JSON without touching the
  60s base.
- Keep a small regression list of approved/rejected sample outputs; diff it on tuning changes.
- The parity harness (P1.0) is the permanent test backend — reuse it for every phase.
