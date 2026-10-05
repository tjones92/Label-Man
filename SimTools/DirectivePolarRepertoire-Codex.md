# Directive: Polar Repertoire Repair and Calibration (Codex)

**Status:** DRAFT v2, scope and gate decisions incorporated from Alice (see "Decisions already made").
**Evidence base:** `PolarModelDiagnosis.md` (Oct 3 2026), `PolarFollowupReport.md`, `PolarCensus209Report.md`.
**Rename** to fit the `Directive[N]-Codex.md` numbering when filed.

## Purpose

Make a live set the product of three separate steps: (1) suitability, (2) stable, varied choice among suitable songs, (3) a candidate pool that reflects real repertoire access, drawing on a repertoire supply that exists for every scene. Make the categories we measure mean what they say. Then calibrate live-set repertoire composition toward the provisional historical bands in Phase 6.

## Decisions already made (Alice)

1. **Archetype assignment tables and variation size (old Gate 1):** Codex drafts them using its own judgment. They land as provisional/unsigned (same status as the shipped 34-archetype table). Alice reviews afterward; later phases do not wait on that review, but any change she makes is re-run through the Phase 6 report.
2. **Pool access model and weights (old Gate 2):** Codex proposes and implements on its own judgment under the same provisional/review-afterward terms.
3. **Persistence of act-song preference (old Gate 3):** a stable per-act/song base preference with **slow seasonal drift**. Align with the existing band genre-drift mechanics: when an act's genre drifts, its pool access and song preference follow from that mechanism rather than being a parallel system. Codex locates that mechanism and proposes the drift rate.
4. **Supply seeding is in scope** for this directive (Phase 5), not a follow-on.
5. **The historical bands in the diagnosis are adopted as tuning targets** (Phase 6). They remain judgment-based and provisional, so they live in one editable data table and are expected to move as archival sampling narrows them.

## Ground rules

1. **Causal confirmation before repair.** Every repair phase opens with a measurement showing the mechanism in the retained worlds and closes with a before/after on the same worlds.
2. **Numeric changes are provisional and reported.** Because Alice has delegated the drafting of tables and weights (above), Codex may merge them, but every new constant is listed in the final report with its rationale. Anything outside the delegated items (Phase 4 option choice, Phase 6 band edits, any change to non-polar systems) is surfaced to Alice first.
3. **Scope deviations are surfaced, not resolved autonomously.**
4. **Validation default:** seeds 1001 and 1002, January 1960 to December 1963, polar selection enabled, unsigned and signed rosters reported separately. Tune on these two seeds. For Phase 6 confirmation, Codex proposes one fresh single-use hold-out seed for Alice to name; it is not retuned against once consumed.
5. **Save schema:** additive, versioned fields with load-compatible defaults, only where a phase calls for them (persistent per-composition variation, persistent act-song preference/drift state, new repertoire metadata).
6. **References are by symbol, not line number.** The working tree holds uncommitted polar work. Codex records the SHA-256 of every file touched.
7. The eight reference percentages in the diagnosis reproduce from seed 1001, enabled, unsigned, all filled slots, Jan 1960 to Dec 1963. Keep that exact definition available as the comparability baseline.

## Phase 0: Category semantics and provenance (measurement only)

**Problem.** "Standard/traditional" currently mixes repertoire status, generation bucket and rights status. `GenerateStandardFamily` sets `isStandard = true` for every generated member (including all 350 R&B catalogue songs dated 1945 to 1959) and sets public domain by an independent 18% draw; the census counts public-domain songs as traditional even when `isTraditional` is false. Seed 1001 RnB's 45.14% is 5.85% standard plus 39.29% traditional/public-domain. All later A/Bs and the Phase 6 targets read off these categories, so they come first.

**Tasks.**

0.1 Define four provenance categories and report them for every filled slot: newly authored (artist or supplied professional writer); cover of a recent or ordinary existing composition; composition established as a standard **as of the observation year**; traditional lineage. Public-domain status is a separate field and never feeds "traditional".

0.2 Add a derived `EstablishedAsOf(year)` predicate (telemetry only in this phase; Phase 5 decides gameplay use).

0.3 Per-admission provenance trace: for every composition entering an indexed cover pool, record origin (seed family, seeded recent hit, chart-promoted via `OnRecordChartRunComplete`, professional catalogue), release/recording, chart completion, and primary/secondary tags.

0.4 Extend `SimTools/analyze-polar-model-diagnosis.mjs` (or a sibling) to emit, per genre and year, numerator and denominator for each category. Live-set composition selection, recording source choice and chart success are three separate reported quantities.

0.5 Record the baseline: re-run the extraction on both seeds, store JSON with input SHA-256 hashes.

**Acceptance.**
- Selection outcomes are byte-identical to the pre-phase tree for the same seed (no gameplay diff).
- The eight reference percentages still reproduce under the old definition.
- New categories partition filled slots (sum to 100% per genre/year; overlaps reported explicitly).

## Phase 1: Composition taxonomy at creation

**Problem.** `EnsureComposition` copies genre/tags only, so `SongProfileDeriver` clones the primary-genre fallback: all Country standards and recent hits begin as `CountryTwoBeat`. Persistent variance alone cannot repair this, and adding unused table rows accomplishes nothing.

**Tasks.**

1.1 Every seeded standard and seeded recent hit receives an explicit archetype, lyric mode, vocal presence, word density and form from authored per-family assignment tables instead of the genre fallback. Use existing resolver rows first (e.g. the four 1960 to 63 Country-family rows).

1.2 Small persistent per-composition variation around the archetype template, seeded from composition ID and stored. It survives scouting, reopen, save/load and arrangement lineage, and preserves hard constraints (instrumental stays instrumental; unknown metadata never silently becomes instrumental). Magnitude is Codex's draft, kept within first-listen observation bands.

1.3 Add the missing musical vocabulary the diagnosis identified, **as archetype rows or modifier combinations, not new gameplay genres**: Country (country shuffle/honky-tonk, waltz, Western swing, Nashville orchestral ballad), Bossa-specific and early modern-jazz instrumental forms, Surf instrumental, quiet hymn/quartet/choir distinctions for Gospel, Latin and TexMex forms, Classical orchestral/chamber/solo distinctions available in 1960 to 63. Respect the existing availability dates (e.g. `JamExtendedWorkout` 1965, `ChamberBaroquePiece` 1964). Codex drafts the rows and flags them as provisional.

1.4 Audit report of **generated and actually selected** archetype distribution per genre/family (selected, not table rows). Report any genre where one archetype holds a dominant share of selected slots.

**Acceptance.**
- No seeded composition stays on a fallback archetype unless a table explicitly assigns it.
- Songs sharing an archetype have distinct truth profiles; scout, reopen and save/load round-trip identically.
- Variation does not exceed first-listen observation bands (scouting uncertainty governs how differences become observable, not how large truth variance is).
- Selected-archetype distributions are reported before/after.

## Phase 2: Pool inclusion

**Problem.** The family songbook is added only when the raw exact-genre count is under four. The fourth EasyListening ordinary cover removes the entire family songbook; the gate runs before deduplication and date eligibility and is unrelated to the requested cover count. Tin Pan Alley and Christmas standards (primary TraditionalPop, secondary EasyListening) are indexed by primary genre only. A stable tie-break cannot recover excluded compositions.

**Tasks.**

2.1 Replace the discontinuous gate with a continuous or persistent model of repertoire access (exact-genre, secondary-tag and family/cross-scene access, with an access weight), evaluated after deduplication and date eligibility. Tie access to the act's genre state so existing genre drift moves it.

2.2 Make secondary-genre access explicit so EasyListening reaches TraditionalPop standards by design.

2.3 Decide and implement a persistent eligible songbook per act versus per-booking recomputation, recording the choice and rationale.

**Acceptance.**
- Regression test: EasyListening with 3, 4, 5 and many ordinary covers keeps its standards eligible.
- Pool contents are deterministic and independent of the requested cover count.
- Eligible songs per act by genre reported before/after (Latin/TexMex and Classical included).

## Phase 3: Selection (suitability versus choice)

**Problem.** After suitability, non-rock selection ranks by `.35 Capability + .45 Identity + .20 Moment`, then exact primary genre, then ascending song ID. Plateaus are deliberate, but nothing chooses among suitable songs, so the lowest IDs are reused; Country's low IDs are the seeded standards block, so the ID rule encodes catalogue-generation order. Separately, `SuitableSongs` ranks on preliminary `Fit` while `Prepare` later resolves a different arrangement for the refusal gate, and the ranking never sees that resolved capability.

**Tasks.**

3.1 **Diagnostic A/B first (no behavior change):** on identical actors and pools, compare ordering by reference `Fit` against ordering by the proposed arrangement the gate uses. Report divergence per genre before any switch.

3.2 Keep the plateau. Replace the final ID tie-break with a **stable per-act/song preference with slow seasonal drift** (decision 3), then add set context where defensible (hook, familiarity, age, repertoire history, set balance such as needing a ballad or too many up-tempo numbers).

3.3 If 3.1 confirms a material divergence, rank by the resolved arrangement; otherwise leave ranking on reference fit and record why.

3.4 Report repertoire churn (month-to-month overlap of an act's cover set) so stability is measured, not assumed.

**Acceptance.**
- No ascending-ID rule remains in any selection path.
- Exact song-ID tie share and top-ten share reported before/after for every genre in the reference table.
- Churn is bounded and reported: acts keep a recognizable repertoire with gradual seasonal change, no month-to-month reshuffling.
- Deterministic for a fixed seed.

## Phase 4: Original-slot allocation

**Problem.** `PlayerDesk.BuildLiveSet` assigns 0/1/2 placeholder originals at songwriting cutoffs .3/.6 regardless of genre, then fills 3 to 5 slots with covers. Maximum two originals, so a wholly original set is impossible and original share is mechanically tied to set size. This directly caps how far Phase 6 can move cover composition.

**Tasks.**

4.1 Make allocation genre- and cohort-aware (inputs: songwriting ability, genre/cohort, set size, availability of supplied professional new songs). Allow fully original sets for exceptional writers where the cohort supports it.

4.2 Codex presents the options considered, implements the one it recommends, and surfaces the choice in the final report. **This phase extends beyond the gates Alice has already delegated; Alice may veto or redirect on review.**

**Acceptance.**
- Original share varies by genre/cohort for the same songwriting ability.
- Phase 0 categories report the shift; the cover/original split stays consistent with set size.

## Phase 5: Repertoire supply

**Problem.** A new ordinary composition enters the cover pool only after a completed top-40 run; `isCoverable` alone does not admit it. No native Latin/TexMex, Classical, Comedy, Childrens, Bossa or ContemporaryFolk songbook exists, and the recording cross-family helper defaults unhandled families (Latin, Classical, NonMusic) to Pop/Jazz pools, which is not a historically specific source ecology. Seeded catalogue songs are all treated as established standards from startup, and there is no dynamic promotion from ordinary cover to standard.

**Tasks.**

5.1 Using the Phase 0 provenance trace, explain the Latin/Classical monthly-pool transitions in the retained worlds before claiming any treatment effect.

5.2 Seed native repertoire for the missing scenes (Latin/TexMex, Classical, Comedy, Childrens, Bossa, ContemporaryFolk) using Phase 1 taxonomy. Seeded entries carry plausible release years so contemporary material (e.g. much of early bossa, post-1962 topical folk) is **not** established standard material in 1960. Genre-correct inheritance replaces the Pop/Jazz fallback for these families.

5.3 Add admission routes independent of chart completion: unpublished songs, album repertoire, locally familiar material, supplied professional songs. Hook the route into the existing `OnRecordChartRunComplete` path without removing it.

5.4 Make established-as-of-year status a gameplay input where it is currently a static `isStandard` flag, with dynamic promotion of long-circulating covers to standards. Public-domain status stays independent of traditional lineage.

**Acceptance.**
- Every scene in the diagnosis's coverage table has a nonempty year-appropriate pool in both seeds by an early month, reported month by month.
- Provenance trace shows admissions via the new routes, separated from chart-promoted admissions.
- Seeded contemporary songs are not classified as established standards before their year.
- Save/load round-trips new repertoire metadata.

## Phase 6: Calibration toward provisional historical bands

The bands below come from the diagnosis's "Historical estimates" section. They are the report's own judgment-based starting ranges, adopted as tuning targets by Alice's decision. They are **not** measured historical rates: keep them in a single editable data table, expect them to be revised as archival sampling narrows them, and never hard-code per-genre quotas.

**Definition.** Denominator: performed live-set song slots. Numerator: traditional lineage plus standards already established at the time (Phase 0 categories). Fresh outside-written material and recent-hit covers are excluded from the numerator. Live and studio cohorts are estimated separately; this directive calibrates **live sets only**.

| Genre / cohort | Target band for numerator share |
|---|---:|
| Bossa Nova (older songbook; contemporary Brazilian compositions reported separately) | 15–40% |
| Contemporary Folk (contemporary writer/interpreter scene after 1962; earlier traditional-revival entrants may be higher) | 10–35% |
| Country (mixed contemporary acts; heritage/dance-hall acts may be higher) | 20–50% |
| Folk (traditional revival) | 50–90% |
| Gospel (inherited hymn/spiritual/established repertoire; quartet, choir and songwriter-led acts reported separately) | 50–90% |
| Jazz (standards interpreters) | 60–90% |
| Jazz (composer-led modern groups) | 10–50% |
| RnB (older standards/traditional; older R&B catalogue covers tracked separately) | 5–25% |
| Surf Rock (vocal) | 0–10% |
| Surf Rock (instrumental revival/adaptation) | 5–25% |
| Easy Listening (standards-led adult cohort; contemporary instrumental/MOR reported separately) | 50–85% |

**Tasks.**

6.1 Derive cohort assignment from existing act data where possible (genre state and drift, songwriting ability, instrumental flag, vocal/instrumental role) rather than authoring per-act labels. Codex lists any cohort the existing data cannot support and proposes the minimal new field.

6.2 Report, per genre and cohort, per year and aggregated over 1960 to 1963: observed share, band, distance to band, with numerators and denominators.

6.3 Tune **only through the mechanisms built in Phases 1 to 5** (pool access weights, preference/set-context weights, original-slot cutoffs, supply volume and release years). No ad hoc cap or quota that forces a share. A universal old-song quota would misrepresent genres with a large contemporary portion, such as Easy Listening instrumentals.

6.4 Aim for inside the band, not its edge. Where a cohort cannot reach its band without a strained mechanism, stop and report which constraint blocks it and what would relax it, rather than forcing it.

6.5 Confirm on one fresh single-use hold-out seed (Alice names). No retuning against it after it is consumed.

**Acceptance.**
- Every cohort in the table is inside its band in aggregate on both tuning seeds, or has a stated, evidenced blocker.
- The concentration measures from Phase 3 (tie share, top-ten share) do not regress when bands are met.
- Contemporary and recent-cover shares are reported alongside, so hitting a band is not achieved by suppressing outside-written material.
- Hold-out result reported unretuned.

## Non-goals

- Studio/recording-source calibration and chart-success calibration (reported separately, not tuned).
- Redesigning fit plateaus or changing perception, source-share, or chart behavior beyond what is stated.
- AI refusal avoidance (still an unimplemented draft item).
- Treating the bands as measured historical statistics.

## Required final report

1. Per phase: what changed, files/symbols touched, SHA-256 of each source file.
2. Baseline and after-run tables for both seeds, unsigned and signed separately, with numerators and denominators.
3. Phase 0 partition table and reproduction check of the eight reference percentages.
4. Phase 1 selected-archetype distribution per genre; the new archetype rows and the full assignment tables for Alice's review.
5. Phase 2 and 3 results: eligible-songs-per-act, A/B divergence, tie/top-ten shares, churn.
6. Phase 4 options considered and the choice made.
7. Phase 5 provenance of admissions by route; seeded repertoire inventory by scene and year.
8. Phase 6 band table (observed vs band per cohort, per year and aggregate), blockers, hold-out result.
9. Every new constant with rationale. Measured findings kept distinct from inferred ones; remaining unmeasured limits stated plainly.
10. Scope deviations and open questions for Alice.

## Definition of done

- Phases 0 to 6 complete, or explicitly stopped at a stated blocker with evidence.
- No gameplay diff in Phase 0; every later diff is attributable to a named phase with a before/after on the same worlds.
- Deterministic for a fixed seed; save/load round-trips every additive field.
- Final report delivered in the format above.
