# Release Judgement Directive

Status: **revised design draft; Phases 0 and 1 built (observe-only, byte-inert); Phase 2 next** (2026-10-18).
See "Implementation log" below for what exists in code.
Source: the Top 10 review arc in `HistoricalLabelTierBalanceResearch.md`, §17x-§17af, the original directive,
review of the current code, and the historical sources linked below. The original measurements are retained;
new rules and parameter ranges are proposals unless explicitly described as existing behaviour or measured evidence.

A label should release a record because it has suitable material, a reason to serve an audience now, and the means
to record and work it. It can seek songs, develop arrangements, record several sides, reject a master, hold a
follow-up, or assemble an LP from existing and new performances. The release decision should select and schedule
work that exists. It should not create its own evidence by inventing a song after choosing an act and format.

**Scope of this document:** revise the design, not start its implementation or run its multi-seed programme.
Instructions and historical author decisions inherited from other documents are design inputs, not additional
authorization. Preserve their useful constraints, but resolve conflicts explicitly here before a later build.

## Implementation log

### 2026-10-18: Phase 0 (measurement) and Phase 1 (shadow song pool), base 094cf89

**Built.**

- *Phase 0, default on, new files only:*
  - `<run>-single-launch-quality.csv`: one row per AI single launch, with `(hook + production) / 2`, hook,
    production, runtime quality and weeks since the act's previous single (−1 if the run has not seen one).
  - `<run>-act-release-years.csv`: one row per act-year, with signed weeks, orphan/promo singles, standalone
    and scheduled LPs, distinct Hot 100 records and year-start lifetime counters.
  - `SimTools/release_cadence_screen.py <run-prefix>... [--years A-B]` reads both. It prints the retrospective
    groups beside the discography sample, active-week denominators, zero-release signed years, lagged-status
    cohorts, single gaps and entry by quality quintile at the V1 edges.
  - Writers: `ChartAuditRunner.ReleaseJudgement.cs`.
- *Phase 1, `--release-judgement-shadow`, off by default:*
  - `Systems/ReleaseJudgement/AiRepertoireService.cs` keeps a saved shortlist per AI act, capped at 8 active.
  - Arrivals are keyed and dated: artist-written, professional, standard, recent-hit cover and traditional.
    `SourceMix` sets the arrival rate, not a pick at release.
  - Each song gets a 1-4 week readiness delay and a polar fit read when it becomes recordable. The existing
    refusal rule applies.
  - The label read is truth ± a persistent label/act bias ± song error fixed per evidence version. The band is
    0.12→0.04 by `scoutingAbility`, ×1.5 for an unheard offer.
  - At each legacy orphan or promo single, the best perceived recordable song is logged and marked
    `ShadowIssued`, so depletion is visible.
  - Catalogue songs are referenced by `SongId`, never registered. Shadow originals carry fixed traits and a fixed
    title seed but no title: authoring waits for a phase that commits masters.
  - Outputs: `release-judgements-shadow.csv`, `repertoire-health.csv`, and `material-events.csv` with
    `--release-judgement-events`.
  - Saves write `WorldSaveData.AiRepertoire` and v23 only when the shadow has state. Otherwise saves stay
    v21/v22 and unchanged.
  - Any other `--release-judgement-*` flag is rejected.

**Gates passed.**

- *Inertness:*
  - Reference `ref-094cf89-1001-52`: base commit, default-control flags, full telemetry.
  - All 84 pre-existing CSVs are SHA-256 identical in both `rj-off-1001-52` and `rj-shadow-1001-52`. The shadow arm
    also had `--release-judgement-events` on.
- *`SimTools/ReleaseJudgementChecks.tscn`, 23/23 passing:*
  - 60 shadow weeks leave the global RNG state, catalogue song count, record count, label cash and release
    counters unchanged.
  - Shortlists stay within the cap, with no duplicate songs and unique ids. Every reference resolves, and
    archives are pruned.
  - A reread or reversed enumeration returns the same pick and changes no state. Every read stays inside its
    band.
  - Removing one act leaves its labelmate's and another label's shortlists identical.
  - Two loads of one save continue identically, and also equal the never-saved run (21 MB indented JSON at 2,967
    acts).
  - Off means nothing is tracked, saved or versioned.

**First shadow read: one seed, 1960 only; a smoke, not a result.**

- 5,308 legacy single launches; 99.2% had a recordable shadow song.
- Shadow pick's true expected quality 0.661, against 0.612 for what legacy actually shipped. 61.5% sit above the
  0.639 cliff, against 41.2%.
- Regret against the oracle is 0.003 (82% picked the oracle song). Read error SD is 0.026.
- Pick sources: professional 40%, recent-hit cover 23%, standard 17%, artist-written 17%, traditional 4%.
- Expected gain by tier: Major +0.060, Small +0.043.
- Phase 0 readout on the same run: Q5 entry 41-47% and Q1-Q2 entry about 0%, so the cliff is present on today's
  defaults. Unknown acts still issue 75% of singles.

**Open before Phase 2 relies on any of this.**

1. *The pool saturates.* About 720 arrivals a week against about 100 shadow issues; 173 a week archived as
   passed-over. Best-of-8 is almost always available, which flatters selection (risks 1 and 11). Tune arrival
   rates against a recording-throughput constraint before reading a 2A gain.
2. *Reads may be too accurate.* With SD 0.026 and regret 0.003, labels nearly know the truth (risk 8). The band is
   a parameter hypothesis; Phase 2A must compare it with wider bands.
3. *The gain leans toward the head:* Major +0.060 against Small +0.043, from `labelAccess` in professional
   arrivals (risk 7). Read Big-5 / CR4 / CR8 in the first paired screen.
4. *The shadow refused nothing.* Refusal needs low identity and high stretch together. Confirm against the
   legacy refusal count before calling it correct.
5. *Shadow originals omit team-writing craft.* `TeamWritingService` is keyed on a record id. It must be added when
   originals are authored for real.
6. *Accounting limits of the readout:*
   - `act-release-years` counts an LP when it is scheduled, so a December promo's album lands in that year.
   - Orphan singles whose economics were not evaluated emit no strategy event, so they are absent from both
     Phase 0 files (none on default controls).
7. *Contract-counter audit (Phase 0; not changed):*
   - `contractSinglesObligation` (3-8 for NewSigning/Rising, through 1966) is described as sides.
   - `contractReleases` counts release events: an orphan single +1, a standalone LP +1, an AlbumWithPromo
     project +2 (promo, then album drop).
   - A linked project therefore meets a "sides" deliverable twice as fast as an orphan single, and no event
     counts two sides. Repair belongs in its own accounting checkpoint (§6).

## What to keep, and what to change

Keep persistent repertoire, uncertain A&R reads, commissioning and covers through the publishing system, economic
checks, separate single and LP calendars, earned compilations, staged causal probes, and private rival information.

Strengthen the design in four places:

1. Replace the act lottery with a bounded **label release slate**, ranking ready projects across the roster.
2. Separate **composition, repertoire entry, recorded master, and release package**. Their identities, costs,
   readiness and ownership are different.
3. Judge material for an act, audience and use. A single candidate, a B-side and an album track need different
   virtues; one universal hook bar would misrepresent the repertoire.
4. Treat historical counts as evidence with denominators and coverage limits. Preserve mistakes, flops, unknown
   acts and commercial pressure without giving the AI future chart knowledge or guaranteed successful releases.

## The reframe

The AI release path makes three choices in a fixed order, and the song comes last:

1. **Who:** a priority lottery over roster acts off cooldown.
2. **What format:** a margin comparison that reads the artist's base quality, revenue memory and noise.
3. **What song:** selected or authored during record generation.

Persistent material does not feed back into that initial choice. A label cannot build a release slate from songs
already in hand, reject its weakest project in favour of another act's ready master, or commission material in
anticipation of a follow-up. Existing material-fit selection and album-promo selection improve the performance
chosen inside a project; they do not provide that upstream planning loop. A label can believe it has a hit in
hand, with imperfect confidence; it must never know the future outcome.

The player already has repertoire reads, rehearsed covers, commissions, sessions and masters. The world also has
`SongComposition`, live-repertoire and material-fit systems. The missing part is an AI-owned, persistent path from
that material to recording and release commitments. Reuse the shared domain services; do not make AI gameplay
depend on a `PlayerDesk` singleton or treat a player's listening read as the song's objective quality.

## Code-verified findings (checked against the tree, 2026-10-18)

- **The roll.** `CompetitorManager.TryReleaseRecord` (CompetitorManager.cs:1927) runs:
  1. `AILabel.GetArtistForRelease` (Data/AILabel.cs:557)
  2. `ChooseEnabledGenreSupply`
  3. `DecideRelease` (:2409)
  4. the hit-act lane's format override, when on (:1967)
  5. `GenerateRecordFromArtist` (:3351), or `TryInitiateAlbumProject` (:2174)
  6. production, marketing and release

  Roll frequency is `releasesPerMonth / 4` a week (`CalculateLabelReleaseCapacityChance`, :1601).
- **Who.** `GetArtistForRelease` is a weighted lottery on `CalculateReleasePriority`: career priority, 0.4 ×
  momentum, and a cooldown bonus. It covers acts past `GetMinimumReleaseCooldown`: 12 weeks, 10 for Star or above,
  and 8 for a Superstar at a Major. With `--hit-act-cadence` on, a due hot act takes the roll first.
- **Format.** `DecideRelease` reads `artist.CalculateBaseQuality()` (`BuildDecisionContext`, :2879), the format
  revenue memories and two noise draws. It also reads an album portfolio credit and a `HitInventory`
  (`ResolveHitInventory`) that already feeds the album prior. No specific song is selected when it runs. The choice is
  margin-driven ([[album-creation-is-margin-driven]]).
- **Orphan single material.** `GenerateRecordFromArtist` constructs a record before selecting its composition.
  Its initial hook is `songwriting × 0.55 +
  baseQuality × 0.35 + U(−0.12, 0.18)`. Its production is `label × 0.4 + studioPerformance × 0.3 + studio × 0.2 +
  U(−0.05, 0.10)`. `SongMaterialSelectionService.ChooseMaterial` (:60) builds one candidate per source (artist-written,
  professional, standard, recent-hit cover, traditional). `WeightedPick` remains stochastic. With
  `PolarSongBehavior.UsePolarFitSelection` enabled (currently true for normal new games), it excludes refused
  material and weights sources by `SourceMix × SelectionScore(fit)`. It is therefore already fit-aware, but does
  not select from an act's persistent stock or maximize its perceived single potential. The application service
  then changes hook/production; the initial hook above is not the final shipped hook.
- **Promo single material.** `CreatePromoSingleFromAlbum` (:3582) selects among `nonSingleTracks`. The normal polar
  path uses `SongMaterialSelectionService.PromoSuitability` and reuses the selected track's composition, master and
  performance. The legacy structured path uses `GetLeadSingleSuitability` and title-derived traits
  ([[song-titles-feed-track-traits]]). Preserve these distinctions in controls and avoid applying realization twice.
- **Promo timing is modelled.** The promo ships on the roll, and the album drops 3-5 weeks later
  (`albumDropGapWeeksMin/Max = 3/5`; V1 seed 1001 mean 4.0, n 13,112). The album's stock and awareness carry a
  promo-performance synergy (`synergyStockMultiplier`, mean 0.87). This was built under Directive 3B
  (`AlbumProjectPipelineDirective.md`).
- **Compilations are a dice roll, not a reaction to hits.** In `GenerateAlbum`, `GetCompilationChance(genre, year)`
  (AlbumModel.cs:215) is the genre's `SingleOrientation` decayed by era. The compilation takes the act's **last four
  released singles**, whether they charted or not. Directive 3B noted there is no four-single gate.
- **The hit-act lane** (`HitActCadence`) forces album picks to orphan singles on 48% of its picks (§17af).
- **Player parity objects exist:** `RepertoireItem` (ReadHook, ReadQuality, IsCommission, SongId, Recorded),
  `TeachCover` / `ProcessCoverRehearsals`, and `CommissionSong` with `BuildProfessionalForSong`
  ([[material-mix-and-commission]]).
- **Shared foundations already exist.** `CompositionCatalogService` owns compositions and source pools;
  `PolarSongBehavior`, `PolarMaterialFit` and `PolarCoverResolver` own capability, identity, moment, arrangement and
  refusal. Reuse them. `RepertoireItem` is currently nested in `PlayerDesk` and mixes repertoire membership with
  one listener's reads; some original entries can lack a `SongId`. Sharing it requires an adapter or extraction,
  plus a deterministic way to resolve those originals, rather than copying player reads into AI truth.
- **Two-sided discs already exist.** `Record` has `masterId`, `bSideMasterId`, `PlugMasterId`, B-side traits and
  publishing snapshots. Player releases and the flip mechanism use this representation. Extend its AI population
  and accounting instead of introducing an independent second market record for every B-side.
- **The clock is shared today.** `RecordArtistRelease` (:2339) resets `weeksSinceLastRelease` for either format.
  Separate LP planning must also separate the single and LP clocks, not just add another calling site.
- **A deliverables clause is not a release deadline.** `contractSinglesObligation` is tested against
  `contractReleases` in `RosterManager` when a term matures. `RecordReleased` currently increments that counter for
  a release event. The field's historical comment speaks of sides. Reconcile the game's event/side units before
  using it in planning; it does not currently express a label promise to release a single by a particular week.
- **Creation has side effects.** `GenerateRecordFromArtist` attaches catalogue material and calls
  `SessionEmploymentService.OnRecordMade` / `RepertoireInfluenceService.OnRecordMade`; the single path's affordability
  check follows generation. A slate preview must not call this method speculatively for every candidate.

## Measured baseline (V1, five seeds, single launches 1961-64)

This baseline's quality diagnostic is `(hook + production) / 2`; chart outcomes also depend on promotion, reach,
audience and competition. Promo singles carry a higher hook but a lower production read, so their diagnostic
quality edge is small. These are the original V1 observations, not fresh measurements of the revised design:

| Path | Launches a year (per seed) | Hook | Production | Realized quality | Hot 100 entry | Top 40 |
|---|---:|---:|---:|---:|---:|---:|
| Album promo single | 2,781 | 0.716 | 0.535 | 0.626 | 12.6% | 4.0% |
| Orphan single | 2,836 | 0.526 | 0.671 | 0.598 | 11.6% | 3.9% |

Entry by realized-quality quintile (edges 0.529 / 0.588 / 0.639 / 0.696):

| Quintile | Q1 | Q2 | Q3 | Q4 | Q5 |
|---|---:|---:|---:|---:|---:|
| Promo entry | 0.3% | 0.7% | 2.0% | 7.9% | 44.4% |
| Orphan entry | 0.1% | 0.4% | 2.7% | 12.7% | 51.2% |

**What this says:**

- **Entry is a cliff in realized quality.** Below about 0.64, which is 60% of all singles, a single almost never
  charts (0.1-2.7%). Above about 0.70 it charts about half the time. At equal quality the two paths enter alike, so
  material and recording selection are promising levers. This observational comparison does not isolate all
  path effects or establish a universal quality threshold.
- **A song bar has a lot to work on.** Much of the costed supply has a low observed chance of Hot 100 entry.
  Labels that held weak songs back could redirect resources, provided other ready material exists (see risk 1).
- **Correction to §17af's framing:** its hook contrast (promo 0.80 against orphan 0.59) overstates the gap. Its
  measured entry rates for hot acts stand. The keep-the-album probe is folded into Phase 5 here.

## Historical release behaviour, 1960-64 (author decision 3)

Two sources, both in `SimLogs/historical-hot100-1960-64/`:

- **The recovered Hot 100.** Same-act debuts within three weeks are merged into an inferred release event to
  approximate two-sided issues (`release_cadence_history.py` → `release-cadence-history.txt`).
- **A discography sample.** US A-sides and LPs for 17 acts from Wikipedia, years only (`discography_sample.csv`,
  `release_cadence_compare.py` → `release-cadence-compare.txt`).
  - *Stars:* Ray Charles, Brenda Lee, the Beach Boys, Roy Orbison, Marty Robbins, Andy Williams, Jim Reeves, the
    Miracles.
  - *Mid:* Martha & the Vandellas, Dean Martin, Jimmy McGriff, Barbara Lynn, the Highwaymen.
  - *Low:* the Trashmen, Jimmy Soul, Rosie & the Originals, the Rooftop Singers.

Groups are by Hot 100 records over 1960-64: star 8+, mid 3-7, low 1-2. The sample is small (61 act-years), skews
to acts with a Wikipedia discography, and gives no release months. Treat it as an order of magnitude with a shape,
not a calibration table.

**1. Charting cadence (Hot 100, 3,426 debuts → 3,135 inferred release events; 8.4% contain multiple debuts):**

| | Acts | Charting events per active act-year, mean (median) | Weeks between charting events, median (p25-p75) | Gaps ≤ 17 weeks | Gaps > 52 weeks |
|---|---:|---:|---:|---:|---:|
| Star (8+) | 102 | 2.96 (3) | 14 (11-17) | 75% | 2% |
| Mid (3-7) | 233 | 1.92 (2) | 16 (12-26) | 58% | 9% |
| Low (1-2) | 814 | 1.11 (1) | 18 (14-38) | 46% | 21% |

A Top 10 event had another charting event observed within the study window 88% of the time, a median 14 weeks
later among observed follow-ups (p25-p75 12-17). End-of-window censoring is not corrected in this table.

**2. All releases, charting or not (discography sample, per active act-year):**

| | History: singles, mean (median, p25-p75) | History: LPs | Compilations | Holiday LPs | Sim controls: singles | Sim: LPs |
|---|---|---:|---:|---:|---:|---:|
| Star | 4.7 (4, 3-6) | 2.45 | 0.47 | 0.13 | 2.26 | 1.36 |
| Mid | 3.6 (3, 3-4) | 1.67 | 0.11 | 0.06 | 1.83 | 0.87 |
| Low | 2.8 (3, 1-4) | 0.38 | 0 | 0 | 1.65 | 0.73 |
| Never charted | — (no discographies) | | | | 1.50 | 0.77 |

The sim's groups use the same Hot 100 record bands within each run (default controls, five seeds, 1961-64).

**What history says for the model:**

- **Proven acts released all the time.** A star issued a single every ~11 weeks (4-5 a year), and only about 3 of
  those charted. Stars did not stop after a flop: country and easy-listening stars (Reeves 5-8 a year, Robbins 3-4,
  Williams 2-5) issued singles that mostly missed the Hot 100. Even low acts released about 3 a year around their
  hit; Jimmy Soul and the Trashmen issued 4-6, almost all misses. A label chased a hit with follow-ups.
- **Allocation is a plausible problem to test.** The sim's total is 82-90% of *Billboard*'s, but **70% of its
  singles come from acts that never chart**, each releasing 1.5 a year. Its charting acts release half of history's
  rate (2.3 against 4.7 for stars). The sample supports more follow-ups for proven acts. It contains no
  never-charting control, so it cannot establish the correct market-wide share for unknown acts. **The Phase 3
  hypothesis is a shift, not a blanket cut:** bring established acts toward the observed cadence while retaining
  viable debut opportunities. Passing a slot is a mechanism to test, not proof that total volume is preserved.
- **LPs ran alongside singles; they did not replace them.** Stars issued about 2.5 LPs a year, about one LP for every
  two singles. Many singles were taken from LPs: Reeves almost all, Robbins and Williams most. Non-album singles
  were common in R&B, soul and teen pop (Martha & the Vandellas' 1964 singles, several Orbison and Highwaymen
  sides). In the sim an album uses the act's release roll and resets its cooldown, so the LP cycle displaces
  singles (§17x: albums take more than half of a hit act's rolls). **LPs should be a separate production track that
  does not consume the singles roll.** An LP gathers the act's recent singles plus new material, and may in turn
  supply a single.
- **The sample supports compilations following a run of hits.** Stars averaged about one compilation every two
  years in this sample. Examples include:
  - Orbison's *Greatest Hits*, August 1962, after five Top 10s;
  - *Ray Charles' Greatest Hits*, 1962, after "Georgia", "Hit the Road Jack" and "Unchain My Heart";
  - Robbins' *More Greatest Hits*, 1961, after "Don't Worry".

  Mid acts rarely issued one, and no sampled low act did. This supports an earned greatest-hits package; it does
  not establish that all catalogue compilations require Hot 100 hits or that youth acts make only compilations.
- **Holiday releases are a Q4 pattern.** Christmas LPs or singles came from Brenda Lee, the Beach Boys, Jim Reeves,
  Andy Williams, the Miracles and Jimmy McGriff. That is a candidate for the seasonality layer, not this directive.
- **Promo lead.** Sample albums with months show the single first. For example, "Surfin' Safari" was a 1962 single
  and the LP followed in October. Months are too sparse to measure the lead, so the sim's 3-5 weeks remains a
  compatibility setting for its existing linked-promo strategy, not a measured historical rule for all albums.
  Album-first, simultaneous, later-extraction and no-single projects remain valid.
- **The breakneck cases are multi-label.** The Beatles' 1964 US output (27 Hot 100 records, about a dozen LPs) came
  from five labels at once: Vee-Jay/Tollie, Swan, MGM, Atco and Capitol. Each held different masters. Ray Charles'
  1960-61 count includes Atlantic issuing his back catalogue after he left for ABC. Old labels working a hot act's
  masters is real, but it is a catalogue-licensing behaviour, not one label's cadence. It is out of scope here and a
  candidate later feature.

**Cadence rules this sets (for Phases 3-5):**

- *Follow-up planning:* use roughly 8-13 weeks between singles and an escalation review around 16-17 weeks as
  **initial probe ranges** for commercially active proven acts. The star sample's 4.7 annual releases suggests
  about 11 weeks only if evenly spaced. Chart-debut gaps do not measure release dates or justify a mandatory
  four-month issue. Actual dates, readiness and the current record's trajectory govern individual cases.
- *Cooldown:* separate the hard production constraint from a preferred commercial interval. Current 8/10/12-week
  cooldowns remain controls; neither the chart-debut study nor an annual mean validates a universal minimum.
- *LP cadence:* retain approximately 2.45 LPs for stars and 1.67 for mid acts as broad comparison points. Separate
  new studio LPs, compilations, live/holiday titles and former-label issues before turning these into current-label
  production targets. Three distinct relevant hits is a provisional greatest-hits gate to test.

## Historical evidence: interpretation and additions

**Do not promote a chart heuristic into a release fact.** The three-week merge in
`release_cadence_history.py` joins successive nearby debuts, including possible different releases and chained
clusters. Its 8.4% is not a measured two-sided-hit rate. Verify pairings by label/catalogue number where available
and publish sensitivity to the merge window. The reported 88% follow-up rate is within the observation window,
with right censoring near 1964; it is not a measured probability that a label issued any follow-up.

**Separate retrospective comparison from AI knowledge.** Star/mid/low groups use outcomes over the whole study
window. Keep those groups for reproducing the original table; the AI must classify an act only from information
available at the decision date. Add a lagged-status comparison, exposure in signed active weeks, and zero-release
act-years. An act-year active because it charted differs from one active because it released. Include label tenure,
debut date, reissues and credited duets when comparing history with the sim.

**Hot 100 absence is not proof of a commercial flop.** Country, R&B, jazz, gospel, regional and adult audiences may
support records outside that chart. Use the game's existing audience, sales, airplay, trade-chart and profit facts
where available, each dated as of the decision. Do not fabricate a missing genre-chart result or impose a hidden
Hot 100 success quota. The proportion of commercially viable uncharted records is a research need.

**Contemporary practice supports two A&R decisions.** Motown reviewed recorded work at competitive company
meetings and sometimes sent recordings back for more work. This supports choosing songs for a session, then
choosing masters for release, with paid work left unreleased. It does not establish a universal numerical bar or
one workflow for every label. [Motown Museum, recording and review process](https://www.motownmuseum.org/legacy/).

**A disc is a package, and its preferred side can change.** Stax describes "Green Onions" being recorded as a
coupling for "Behave Yourself," attracting local radio interest, then being reissued as the A-side. Our inference
is to populate the existing two-master representation and permit evidence-driven plug-side changes, without
counting the same pressing twice. The example establishes possibility, not a target flip rate.
[Stax Records, "Green Onions"](https://staxrecords.com/number-ones/green-onions-booker-t-mgs/).

**A youth LP is not automatically a greatest-hits LP.** The Beach Boys' official catalogue lists *Surfin' Safari*
(1962) as a twelve-track programme mixing singles with other material. Allow an ordinary hit-led studio LP before
an act has three hits; reserve the stronger catalogue-evidence gate for a greatest-hits proposition.
[The Beach Boys, album catalogue](https://thebeachboys.com/pages/music).

**Existing repertoire and recorded stock can supply an LP quickly.** The Beatles' official account of
*Please Please Me* describes ten tracks recorded in one day, mixing live favourites and originals, alongside four
previously recorded sides. This supports session batches and master reuse, not twelve fresh compositions minted
at LP release. It is a UK example and must not set a US cadence or a universal session duration.
[The Beatles, *Please Please Me*](https://www.thebeatles.com/please-please-me).

**The aggregate LP ratio needs a scope audit.** The original *Billboard* table is retained below, but it includes
repertoire the sim omits. Neither a guessed coverage multiplier nor the national LP/single ratio proves how many
artist albums this simulation lacks. Obtain comparable counts before changing album volume to hit that ratio.
The linked directory was located in this review, but its PDF returned HTTP 403; its numeric transcription below
remains inherited evidence, not an independently reverified scan.

**Research ledger for the next measurement pass:** retain source URL/issue/page, territory, catalogue number,
artist and master identifiers, release-date precision, first issue/reissue, current/former label, LP type,
single/LP master overlap and confidence. A month-only date is an interval, not the first day of the month.
Expand beyond the 17 successful acts to a label-catalogue sample including acts that never charted; then add
1965-69 strata. Reproduce the original cohorts alongside corrected cohorts rather than silently replacing results.

## Operational design

### 1. Four identities, one material pipeline

| Object | Owns | Must not stand in for |
|---|---|---|
| `SongComposition` | Durable song identity, writers, craft, provenance and publishing rights | A finished performance or one label's opinion |
| Act repertoire entry | Access to a song, source, rehearsal/arrangement readiness and reservations | Ownership of a rival master or a listener's objective truth |
| Recorded master | One committed performance, session/date, immutable realized traits, credits and master control | A new song each time it appears on a disc or LP |
| Release project/package | A disc or LP track list, dates, budget, rights and promotion commitments | A recording session or a second payment for an existing master |

These are responsibilities, not a requirement for four new class hierarchies. Use `SongComposition`,
`SongMasterMetadata`, `Record`, `AlbumTrack`, `AlbumProject` and the existing player session/master types where
they already own the fact. Introduce only the missing repertoire/planning state. Extract shared repertoire
membership from `PlayerDesk.RepertoireItem` or provide a compatibility adapter; retain player reads separately.

The repertoire belongs to the act; a label's option to use material and its ownership of a particular master are
separate. Signing, dropping, switching labels or moving a writer-member cannot erase compositions or silently
transfer masters. Current-label recording rights, a song hold and a master licence answer different questions.
Use the existing publishing and master-deal systems; a cover records the composition, not the rival's master.

The intended project genre and arrangement must be part of the proposition being judged. Do not select a song
for one audience and retag the committed recording to satisfy another genre's supply target. Reuse existing
genre availability, transition and artist-evolution rules, with pure proposal reads and supply counters updated
only for the corresponding committed event. A failed slate search cannot consume the year's genre allocations.

Every candidate has a durable `SongId` before consideration for recording. Every committed master has a durable
master ID before consideration for release. Resolve legacy unnamed/original repertoire once, with provenance,
instead of manufacturing a new composition each time it is read. Fix the song title and composition traits when
authored. Fix the performance traits once recorded. Future cosmetic renaming must not reroll either.

### 2. Repertoire accumulates before the release opportunity

Maintain a bounded active shortlist per act, initially testing roughly 4-10 recording candidates. This cap applies
to active consideration, not the whole live set, catalogue, archived songs or inventory of masters. A twelve-track
LP must not be made impossible by a ten-song shortlist. Album reservations and existing recorded stock are
accounted for separately; every reservation references the same composition/master identity.

Material arrives through named, dated events:

- **Writing and co-writing:** use existing writer-members, `songwritingAbility`, creativity and team-writing
  services. Completion rate and quality are distinct; a prolific writer need not write better songs. Touring,
  sessions, personnel availability and commitments bound the work. A singing interpreter does not need to become
  a songwriter to obtain good records.
- **Professional offers and staff songs:** draw from the existing publisher/writer pools and scenes, with label
  relationships, specialism and access affecting which offers arrive. An offer is not free ownership. Reuse the
  existing hold and publishing rules; exclusive and non-exclusive opportunities must behave differently.
- **Commissions:** place a brief for a particular act/use, record the fee, writer or source, delivery date and
  hold. Delivery may disappoint. Decide from expected improvement, follow-up urgency, cost and time available;
  never retry until a guaranteed hit appears. Bound concurrent commissions and retries.
- **Covers and standards:** select known material through existing catalogue, familiarity, provenance, arrangement
  and fit rules. Nearby genre families are legitimate sources where the existing model permits them. Record a
  specific reference master or demo subject and a rehearsal delay. Existing live repertoire may already be ready.
- **Player placements:** a song offered to an AI label enters the same consideration/hold path. It must not
  replace the selected composition after the label has approved and paid for a different master.

`SourceMix` should initially guide the **arrival of opportunities**, not force a source lottery at the moment of
release. Observe both arrival and shipped mix: selection can change the latter. Introduce the change in its own
probe; do not enforce exact shipped quotas that make labels reject their best suitable material to fill a bucket.

Age changes a song's opportunity, familiarity, topical relevance or hold; it does not make its melody deteriorate.
Recent-hit covers can lose their commercial window, while standards remain useful. Archive passed-over candidates
without deleting compositions still referenced by sessions, live sets, deals, records or saves. A rework changes
an arrangement or creates a documented new version; a rejected master is never silently rerolled.

Seed a new signing from its actual live/demo repertoire and available offers. Give it a prompt first-session and
debut review, with a less demanding commercial hurdle where appropriate. No guaranteed high-quality debut, free
master or indefinite wait for the pool to become perfect. Existing acts at game start need plausible initial
material and stock, with explicit initialization provenance and no invented future hits.

### 3. A&R judges a proposition, with limited information

Use two reads: **is this song worth recording with this act?** and **is this master worth releasing now?**
Composition craft, execution capability, artist identity, audience fit, current opportunity and production are
different facts. A strong composition with an unsuitable arrangement can become a weak record. A modest song
can work through the right performance, audience and moment.

Wrap the existing polar fit/arrangement/refusal services with label perception; do not build a competing fit
engine. Before a session the label sees a demo/live read and an uncertain expected recording. After it hears a
master it can judge that performance more confidently. It never sees a future session roll, future peak,
terminal lifetime sales, or demand from next week's charts. Stamp all public commercial evidence with its
as-of week; hit inventory includes only success already observed, not eventual peaks in archived snapshots.

Perception should contain a persistent label/act or style bias and song/master-specific error, with skill
controlling uncertainty. Cache a read by listener, subject and evidence version. Reopening the slate, changing
list order or waiting four weeks supplies no free independent reroll. A new demo, rehearsal, completed take or
public response may justify an update. `ScoutingPerception` supplies the determinism pattern, but its automatic
four-week refresh is not sufficient by itself for this material model.

Keep explicit judgments rather than flattening everything into `(hook + production) / 2`:

| Use | Primary question | Reason to pass or choose another use |
|---|---|---|
| Lead/follow-up single | Can this performance reach the intended buyers and be worked now? | Weak read, excessive overlap with the active record, poor timing or an unaffordable campaign |
| B-side | Is this a credible coupling, contrast, fan offering or lower-risk trial? | Unready, unsuitable or unavailable; it need not meet the lead's hook bar |
| Ordinary LP track | Does it serve this programme, audience, running time and standard of performance? | Repetition, poor execution, inappropriate material or too little useful new content |
| Statement/concept LP | Does the act have a coherent, feasible body of work? | Ambition without material, execution, time or audience support |
| Greatest-hits track | Does this controlled master carry demonstrated catalogue appeal? | Insufficient evidence, rights or excessive repetition of an earlier package |

The per-label single standard remains: tier/archetype set its prior and commercial policy; `scoutingAbility`
affects the read. Specialism matters independently of wealth. A small label may know its local audience better
than a major; a hit factory can search more aggressively without judging every song perfectly. Do not multiply
the same resource advantage into arrival quality, read accuracy, fit, hook, promotion and acceptance unchecked.

The release hurdle may soften within a bounded range as a real commitment approaches, with the reason logged.
Keep a minimum feasible performance standard and artist consent/refusal rules. A deadline cannot create money,
rights, available musicians or a finished master. A pressured release is allowed to fail; uncertainty, commercial
risk and limited means should generate flops ([[closures-must-be-earned-not-imposed]]).

### 4. Sessions produce stock, including records that never ship

Use a compact session batch: reserve selected repertoire, a feasible date, crew and budget, then commit the
performances once. Batch size and duration depend on the material, era and available resources; do not model every
take or impose modern album development times on all early-1960s sessions. Session improvisation is legitimate:
if it produces a new song, author and credit it at that session, then judge the resulting master later.

`SongMaterialSelectionService` needs a fixed-composition realization route for originals as well as existing
`BuildProfessionalForSong` / `BuildCoverForSong`. Avoid requiring a fabricated release ID and a release-time
songwriting draw to create a pending composition. Audit the initial single hook's dependence on songwriting
ability: professional/cover performers must not be penalized twice for not writing. Changing that formula is a
separate realization probe, not an unnoticed consequence of repertoire selection.

Preview with pure value data. Commit a real session once, charge its real costs once, then register masters and
repertoire influence once. Never call `GenerateRecordFromArtist` to listen to ten hypothetical sessions for free,
hire ten crews, or increment global record/song counters during ranking. After commitment, master quality is fixed.
Re-recording is a separately costed, identified session; it is neither free nor guaranteed to improve the result.

Track repertoire readiness and master disposition separately. An illustrative path is:

```text
song access: offered -> accepted/held -> rehearsing -> recordable
master:      session committed -> recorded -> reviewed -> approved or shelved
package:     proposed -> budget/masters reserved -> scheduled -> released or cancelled
```

An entry can already be recordable; an approved master may wait or later join an LP. A recorded song remains in
the act's live repertoire. Replace the overloaded idea of `Recorded = spent` with recording history and explicit
eligibility: reuse an existing master, or make a justified new performance. Prevent duplicate concurrent sessions
for the same reserved task. Store cancellation, release of holds and unavoidable sunk costs.

### 5. Replace the lottery with a weekly label slate

The final system ranks **ready projects across the roster**. It does not pick an act and then keep asking it for
a good song. Evaluate the roster once per planning tick, update only changed reads, and retain a small ranked
slate. Keep separate proposals for future sessions and packages ready to ship.

```text
At the weekly boundary, using the last completed week's information:
  complete due writing, delivery, rehearsal and session tasks once
  update dated commercial evidence and material reads
  build eligible recording proposals and ready single/LP packages
  rank by commitments, commercial opportunity, timing and cost
  reserve shared cash, session and promotion capacity in stable order
  commit selected work; schedule ready packages into feasible slots
  release due packages through the existing release/settlement services
  retain deferred work and record why it waited
```

Order and compare by a small number of interpretable factors: a real commitment/due window; perceived material
and expected contribution; current audience/reach; time since the last single; the active record's trajectory;
available budget; and the cost of displacing another ready package. Use existing revenue priors and memory as
uncertain commercial evidence, not as an oracle. Initially expose component scores before choosing numerical
weights. A deterministic bounded search is enough; no global optimizer is required.

The default comparison is expected incremental benefit against the next feasible use of the slot, including
waiting for material already in delivery. The immediate contribution can be modest for a measured development or
contract commitment, within a declared budget. Do not put known terminal ROI into the score or require every
debut to look as safe as a star's follow-up.

Prevent starvation through an aging priority and a label-specific development allowance for new signings or
under-tested acts. This allowance funds attempts, not chart places. It expires or is reviewed after observed
results; the same unsuccessful act must not gain infinite priority simply by waiting. Talent, timing, fit and
commercial evidence can earn another attempt. This keeps discovery alive while proven acts receive timely
follow-ups. Read concentration and first/second-release rates to tune it, rather than forcing a market share.

Keep randomness in material arrival, human judgment, production and audience response. Comparable proposals
can have a stable label preference or salted tie-break. Do not redraw priorities on every evaluation or retain a
weighted act lottery underneath a new score. Any temporary randomness in opportunity timing is a capacity
compatibility measure, explicitly retired or justified later.

### 6. A release calendar with two product queues and shared resources

Keep single due dates and LP due dates independently. `weeksSinceLastRelease` may survive for biography and
legacy code; it cannot be the eligibility clock for both. A released LP does not reset the single clock, and an
album's extracted single is a single event regardless of which queue proposed it. A disc with two sides is one
release event and consumes one single slot.

Initially retain the existing label opportunity envelope to measure allocation alone. Pass an unusable slot to
the next ready slate item once; lapse only if none can ship. Record idle capacity and its blocking cause. This
preserves **opportunities**, not necessarily actual issue volume. If the bar drains all suitable supply, revisit
arrival rates, readiness, session throughput, selection policy and scope before loosening standards to force a
count. Do not mint an emergency song to consume a slot.

The final calendar should turn label capacity into a bounded forward schedule, with slots assigned to concrete
packages and reviewed as evidence changes. A later checkpoint must explicitly reconcile the current at-most-one
weekly roll and its monthly conversion with actual label throughput. Do not silently change capacity as part of
ranking. Use a short rolling horizon (initially test roughly a quarter); a longer delivery task can sit outside it.

Separate product queues still share cash, pressing/distribution limits, act availability, sessions and promotion
attention. Do not double release capacity by copying the old roll into two independent loops. Reserve resources
atomically, resolve simultaneous single/LP demands in stable order, and enforce per-act and per-label work limits.
Reuse the existing linked-project economics and record-level accounting. Where additional capacity is warranted,
make it an explicit, costed calibration change.

Start a hot act's search and next session while its current record is being worked. At planning time, a single
still gaining airplay/orders may justify a short postponement; a stalled or falling record can justify an earlier
follow-up. Singles can overlap, so do not wait for retirement or final chart outcomes. Charge any timing tradeoff
once: reuse existing cannibalization/substitution effects rather than layering a second unmeasured demand penalty.

The 16-17-week review trigger is an escalation to obtain or choose material, not an unconditional issue command.
The original first-single exception becomes a planned debut review. Keep artist deliverables, label release
promises (if modeled), and preferred commercial cadence distinct. A deliverables count must not be turned into
a new contractual release deadline; any unit/accounting repair needs its own checkpoint.

### 7. LPs arise from material, audience and catalogue

Maintain three different propositions even if existing format enums initially need additive metadata:

- **Ordinary studio or hit-led LP:** a programme of suitable new cuts, available unissued masters, covers and
  selected recent singles. One successful single can motivate it; album-oriented demand can motivate one without
  any Hot 100 hit. Not every cut has to clear a singles bar. Do not force all recent singles into every LP.
- **Greatest-hits package:** controlled existing masters with demonstrated appeal, enough distinct repertoire
  for an honest package and a reason for buyers to want it. Start by probing roughly three distinct relevant
  hits, with severity, sales, audience and freshness considered. A Hot 100 Top 10, a genre success and a marginal
  chart entry are different evidence; publish the definition used. Missing chart evidence stays missing.
- **Statement, live or seasonal project:** an appropriate act, material and identifiable project rationale. Use
  existing album legitimacy, live and seasonal systems. These cannot be alternate labels for an unearned
  greatest-hits package or random formats drawn to consume capacity.

Greatest-hits eligibility looks across the **controlled catalogue**, not just singles since the last ordinary LP.
An ordinary LP does not erase a signature hit's value. Compare overlap with earlier hits packages, time since
exploitation and new audience/material before issuing another. Three hits is not a complete LP by itself, and
"last four singles plus generated filler" is not a greatest-hits assembly rule. A clearly identified mixed
catalogue selection can include non-hits; do not mislabel it or secretly apply a hits-only assumption to its cost.

Evaluate track coverage, running time, strongest cuts, weak-tail performance and programme fit. Adapt feasibility
to genre and project: the existing 9-13-track generator is a control, not a universal historical specification.
Keep singles appeal, LP appeal and cohesion distinct. Youth buyers can want studio LPs, and adult acts can release
strong singles. `SingleOrientation`, career and era are priors rather than vetoes.

Reuse the existing margin model, portfolio credit, hit inventory, memory attribution and linked demand rules,
updated to the actual proposed track list and incremental costs. Reusing a master pays no second recording fee;
it still entails the appropriate packaging, pressing, publishing and campaign costs. Avoid counting a compilation's
previous hits as new recordings, new chart successes or fresh contract deliveries.

LP-first, single-first, simultaneous, later extraction and no-single are scheduling relationships, not a forced
format lottery. Keep the existing 3-5-week linked-promo relationship initially where it is already used. A
later-extracted single reuses its master and enters the same single calendar; it must not produce a free extra
release, reroll production or incur a second realization penalty.

### 8. Two sides, lifecycle, accounting and bounded state

Populate AI A/B masters from the same approved stock, with a useful coupling policy. A deliberate weak-side
coupling can be a label choice; it is not mandatory. Reuse an available B-side when appropriate rather than
requiring two new compositions for every issue. Snapshot both compositions' publishing facts through the existing
disc model and run the existing plug-side logic. Do not add a second sales stream or change chart-side counting
in this feature. If simultaneous two-sided chart entries remain unmodeled, declare that historical-comparison
limit; separate chart-side support would need its own audit.

Pending work must survive an act's departure, label acquisition, bankruptcy, death or dissolution. Decide from
stored rights and commitments whether a project transfers, releases, cancels or refunds an unearned amount.
Do not assume roster membership determines ownership of an already recorded master. Reuse `AlbumProject` transfer
and terminal-state logic where applicable; extend it to new jobs and reservations rather than maintaining an
unrelated lifecycle. Rival repertoire remains private to the player for now.

Budgeting must distinguish new fees, existing sunk recording spend, reserved future cash and incremental issue
costs. Store the owner and recoupability of each booking and use the existing settlement/publishing routes.
Shelved sessions remain a label expense; do not hide them from cash/closures or charge them again at release.
Preserve the existing format-memory outcome definition during selection probes. Adding unreleased-session losses
to learned project ROI is a separate accounting checkpoint, with explicit attribution and no double subtraction.

Persist candidate provenance, readiness, evidence/read versions, job deadlines, source/reference IDs, holds,
master dispositions, budgets/reservations, independent format clocks, commitments and deterministic counters.
Version the new state and migrate old saves once. A legacy world can use the legacy path or a documented one-time
initialization; it must not refill from scratch on every load. Validate IDs/ownership before relinking.

Use world-seeded, domain-separated identities for material, perception, job completion and tie-breaks. Sorted
iteration and private streams are required; adding an act or evaluating a rejected project must not reroll another
act's repertoire. Do not borrow global naming, catalogue or record counters for shadow IDs. Preview/logging must
not register catalogue songs, create public releases, hire crew, spend money or alter taste/genre-supply counters.

The current world-save contract reseeds the global economy RNG on load, so do not claim full uninterrupted-run
identity. Require exact restoration of this subsystem, repeatable continuation from the same save, and identical
subsystem event decisions under equivalent dated inputs. Measure CPU, allocations, catalogue growth and save size
over the decade. Cache per-act reads, dirty project proposals and indexed source access; no weekly full-catalogue
scans or repeated native `Record` allocation for rejected candidates.

### Worked decisions the system must explain

| Situation | Expected behaviour |
|---|---|
| A proven act has a falling single, no strong new master and an arriving commission | Plan a session/delivery against its follow-up window; compare a brief wait with the best available master, showing the reason |
| A star's current single keeps growing | Work it, prepare the follow-up, and allow a bounded delay without waiting for retirement |
| An unknown act has the label's strongest feasible debut | Give it a development opportunity; do not require prior hits or promise chart entry |
| A country act sells to its audience without entering the Hot 100 | Judge the next project from that dated audience evidence; do not automatically label the act a failure |
| A young act has one hit and enough usable stock | Consider an ordinary hit-led LP; it has not automatically earned a greatest-hits package |
| An LP track becomes the best next single | Schedule the existing master once in the single queue, preserving performance and charging only incremental issue costs |
| A label rejects everything it can afford this week | Keep the slot idle, log the binding reasons and continue costed material development; do not generate a replacement at release |
| A commissioned recording disappoints | Pay the committed cost, shelve or use it appropriately, and limit further attempts |

## Staged plan (low → high calibration risk)

These are proposed build checkpoints, not work performed by this document revision. Each behavioural checkpoint
has an off-by-default probe switch and declared dependencies; invalid combinations must fail clearly rather than
partially enabling another system. Adoption into normal games happens only after the final review. Keep the
existing legacy path available to its controls and saves. Meet the house discipline:

- byte-identical legacy simulation outputs when off, proved by CSV hash against a reference run
  ([[probe-run-byte-comparison-proves-inertness]]);
- a one-seed smoke;
- a five-seed paired screen against the default controls, and against V1 / LR where the lane is involved, with the
  §17o yearly watch;
- the decade A/B only at the end, on two seeds ([[decade-ab-only-at-audit-end]],
  [[canonical-decade-run-flags]]).

Hash the same pre-existing CSV schemas; put additive diagnostics in new files or compare a declared common-column
projection. A header change is not evidence of a changed simulation. Record commit, config, switches, seeds,
warm-up/evaluation windows and reference hashes in each screen manifest. Fix the acceptance bands before the run;
do not retune the chart, demand, retirement or unrelated population systems to make this feature pass.

### Phase 0: Measurement only (no behaviour)

- Log each launch's realized quality, path, and the act's weeks since its last single. Most of this is already in
  `single-launch-attribution.csv`, which is on only with `--head-hit-attribution`. Make the realized-quality column
  part of the default compact set.
- Write the baseline tables above for the default controls, as well as V1.
- Retain the original cadence and *Billboard* transcriptions. Complete the scope/date/censoring checks in
  "Historical evidence"; the 85-90% coverage adjustment remains a sensitivity assumption, not a measured band.
- Add the per-act release table (`release_cadence_compare.py`) to the standard screen readout, so every phase reads
  singles per act-year by group. Add active-week denominators, zero-release years, and lagged-status cohorts.
- Freeze a small dated world snapshot for selection comparisons. Baseline material-source shares, repertoire-fit
  refusals, B-side coverage, idle slots, LP types, current/former-label issues and runtime/save footprint.
- Audit contract-counter units separately. Do not silently fix them while measuring release allocation.

### Phase 1: The AI song pool, observe-only

- Build a shadow material timeline: initialization, arrivals, readiness, aging, caps and saved private state.
  Use isolated identities, naming and streams ([[one-conversion-reshuffles-the-rng-stream]]).
- Keep it outside the live catalogue indexes and getters read by legacy play. Private RNG alone is insufficient
  if registering a shadow song changes source pools, catalogue counters or player placements.
- Releases run as now. Log the best perceived recording proposition and its expected-quality distribution, not
  a supposed realized recording that has never been made. No live reservations, charges or session employment.
- Gate: common legacy outputs byte-identical with observation enabled; shadow reads also survive reload exactly.

### Phase 2: Material selection, then committed masters

- **2A, selection only:** for the legacy-selected act and orphan-single strategy, choose the best perceived
  available song, keeping act/format/capacity policy and recording formula fixed. Realize that specific
  composition once. This is an intermediate probe, not the finished slate. Compare legacy weighted selection,
  fit-only ranking and perceived single suitability on the frozen snapshot; an oracle ranking is audit-only.
- **2B, recorded stock:** introduce costed session commitments, post-session review, shelving, master reservations
  and reuse. Populate existing B-side fields from real stock and reconcile incremental session/issue costs.
  Subdivide stock persistence/reuse, release withholding and B-side population into paired checks; review can
  remain observe-only until Phase 3's hurdle is tested. Do not combine a changed realization formula,
  source-arrival mix or unlimited session supply with 2A.
- Gate: no speculative generation side effects, duplicate bookings, identity drift or future-outcome reads.
  Existing refused-material and creative-control constraints still apply. Retain disabled player behaviour.
- Read: selection regret/calibration on the frozen snapshot; shipped quality, entry, head/genre slots, source
  mix, debut outcomes and turnover in live runs. Act/format policy is held in 2A; endogenous outcomes may still
  change later release counts and must be reported rather than described as fixed volume.

### Phase 3: The release slate, bar and calendar

- **3A, allocation:** replace `GetArtistForRelease`'s lottery on this enabled path with the bounded roster slate.
  Keep the original opportunity envelope. Only ready feasible packages can clear the perceived single hurdle;
  defer others, try the next slate item, and log exhausted opportunities. Add development/aging policy and dated
  follow-up reviews. Compare allocation with and without the bar to separate two effects.
- **3B, calendar:** schedule packages and sessions explicitly, with independent single clocks, rescheduling
  reasons, shared reservations and capped concurrent commitments. Reconcile throughput with label capacity in
  a separate comparison before changing the opportunity envelope. Do not carry the lottery into the final path.
- Target the established-act cadence distribution, using the 4.7/3.6/2.8 sample means as provisional references,
  not per-act quotas. Report total volume and never-charting share; neither is guaranteed by passing a slot.
  Preserve discovery and commercial survival outside the Hot 100. An idle opportunity is a diagnostic, not a bug
  to hide by fabricating songs or bypassing feasibility.
- Retire hit-act selection priority only after the slate supplies comparable follow-up opportunities. The
  original lane remains a paired control; its album override is removed when independent LP scheduling lands.

### Phase 4: Anticipatory material search

- Activate costed commissions, publisher offers, rehearsed covers and player-placement consideration before
  follow-ups fall due. Run through existing publishing/material objects (`BuildProfessionalForSong`, cover
  builders), never a parallel economy ([[publishing-cover-song-directive]]). Share underlying delivery and fee
  rules with the player through domain services, not calls to player desk UI verbs.
- Move `SourceMix` from release-time source selection to arrival priors in a separate subprobe. Bound writer
  workloads, holds, concurrent jobs, retries and state growth. A failed search leaves a recorded failure reason.
- Compare wait/commission/cover policies against the same stock and due windows; tune expected benefit and cost,
  not the number of future hits. Audit bias from catalogue samples and repeated draws of the same offered song.
- Read fees, unreleased session spend, arrival versus shipped material mix, turnarounds, miss rates, cash,
  small-label survival and paired Big-5 / CR4 / CR8 concentration.

### Phase 5: Independent LP projects and earned catalogue packages

- **5A, feasibility and assembly:** build ordinary, hit-led and greatest-hits proposals from specific reusable
  masters and feasible new sessions. Hold the existing format opportunity policy initially. Apply the actual
  track list to the margin model and existing hit inventory; keep studio/hit-led LPs distinct from greatest hits.
  Test a three-relevant-hit gate against alternatives without pretending the sample establishes an exact cutoff.
- **5B, scheduling:** put LPs on their own queue and clock, sharing resources with singles. Retire the lane's
  album override; an LP's promo/extracted single still occupies one single slot. Keep existing linked-promo lead
  and attribution initially; add other timing relationships as separately observable cases.
- Compare LP cadence by type, genre, career and current/former label. The sample's 2.45/1.67 LP means and 0.47 star
  compilations per act-year are weak reference points. Scope-match the national counts before increasing titles
  or imposing a market-wide ratio. LP unit share and runtime remain guards.
- Gate correct master reuse, rights, transfer/cancellation, shared cash reservations and memory attribution;
  read genre mix, weak-genre follow-up entry (§17af), content overlap and paid production per new master.

### Phase 6: Bundle screen and decade A/B

- The adopted phases together. Five seeds × 260 weeks paired, then the two-seed decade A/B against V3.1 guards.
- Compare the package with controls and with each adopted feature removed. Report phase interactions, seed/year
  variation and the historical matching scope. Hold back some seeds or act/catalogue samples until final review.
  Pool size, read error, thresholds, arrival rate and capacity need sensitivity checks; one successful seed or a
  higher mean hook does not establish historical fidelity.
- Decide adoption from both plausible decisions and aggregate outcomes. Publish unresolved historical coverage,
  two-sided chart representation and save/RNG limits. Do not claim that the sim now recreates exact history.

## Calibration risks

1. **The chart has fixed weekly slots.** Better songs do not create more simultaneous chart positions. They can
   change entry rates, tenure and annual turnover as well as who wins the slots. Read all of those, not just mean
   shipped quality. Acts with deep pools or writer access may gain relative to the rest; a bar can also move the
   competitive cutoff and erase an apparent fixed-world gain.
2. **Supply-bound genre share.** Share tracks eligible records (r = 0.938, [[market-share-is-supply-bound]]). A bar
   that holds back more songs in genres whose pools run weaker cuts those genres' supply. Read genre error per seed
   and per genre (§17ae's method), from Phase 2 on.
3. **Album count is calibrated, and runtime depends on it.** Situational formats move album titles. Album titles set
   1960 LP unit share and runtime ([[album-1960-levers-bind-in-sequence]], [[album-unit-share-pinned-by-velocity]],
   [[runtime-is-album-count-bound]]), and the album chart's genre mix.
4. **Label economics.** Fewer, better releases change revenue, production spend and cash. Read Small-label closures
   ([[small-labels-never-lose-money]]), recoupment and advances. Commissioning is a new cost line.
5. **Revenue memory feeds back.** The format memories learn from realized nets. Changing what ships changes what
   the memories learn, and so the next format choice ([[album-creation-is-margin-driven]],
   `RevenueMemoryROIDirective.md`). Watch the album decision share year by year, not only at the end.
6. **RNG and hashes.** Any new draw on the main stream reshuffles the run. Pool titles feed traits through the
   title hash ([[song-titles-feed-track-traits]]), so a pool song's title must be fixed when it enters the pool, and
   must never be regenerated at release.
7. **Head concentration.** Professional-writer access scales with tier (`labelAccess` Major 0.90 against Small
   0.25). Commissioning and best-of-pool therefore favour the head. §17v showed that changes which look label-blind
   can reweight toward label resources on defaults. Read the paired Big-5 share and CR4 / CR8 in every screen.
8. **Omniscient judgement.** Exact song reads would give an unrealistic advantage, though even good songs can
   flop through reach, timing and competition ([[closures-must-be-earned-not-imposed]]). Use uncertain material
   reads and dated public evidence. Repeated comparisons must not average away the error for free.
9. **Player parity.** Share composition, repertoire membership, fit, rights, cost and realization services, with
   listener-specific reads and a compatibility adapter for the existing player object. The player's decisions
   remain under player control; extracting a domain type must not change those decisions accidentally.
10. **Save format.** Pool state is saved, so the save version changes. AI pools in a loaded world must restore
    exactly ([[panel-runtime-is-wiped-on-load]] is the failure to avoid).
11. **Selection without replenishment.** Best-of-pool spends the strongest stock first. Read depletion, time to
    replace a released master, paid search failures and late-run quality. Unlimited refill hides the constraint;
    a static high-quality initial pool can make the first year falsely reassuring.
12. **The rich-get-richer and starvation loops.** More releases create more evidence and better opportunities.
    Track local specialists, newcomers and non-writers as well as stars. Funding discovery cannot guarantee a
    hit; requiring a prior hit to fund discovery makes success inaccessible.
13. **Parallel queues can overspend.** Independent dates do not create independent cash or musicians. Check
    resource reservations, record reuse and simultaneous commitments before assessing any cadence improvement.

## Decision evidence and correctness gates

Gameplay services expose events/read-only snapshots; `ChartAuditRunner` owns CSV writers. Default output should
be compact weekly totals plus one row per considered project or state transition. Full candidate rankings belong
behind an attribution switch, not an unbounded row for every song/act/week.

Proposed diagnostic schemas (names are design suggestions):

- `material-events`: week, label/act/song/master/job IDs, source/provenance, previous/new readiness or disposition,
  evidence version, reservation owner, fee and delivery dates. A commission and its eventual cut share lineage.
- `release-judgements`: as-of week, act/project, intended use/audience, perceived read and uncertainty, hurdle,
  urgency, last-single/LP dates, active-record evidence, expected incremental contribution/cost, selected action
  and primary rejection/defer reason. Store true traits only in audit columns that the decision code cannot read.
- `release-slate`: capacity offered/used/idle, selected order, shared resources reserved, new-act opportunities,
  and the next feasible alternative's score. Count material, cash, rights, availability and timing blocks separately.
- `repertoire-health`: ready/reserved/archived song counts, usable/unissued/shelved masters, arrivals, depletion,
  distinct source songs, commissions per delivered/released cut and days from signing/session to debut.
- Extend launch/LP attribution with material/master IDs, reuse versus new recording, source of both sides, package
  type, timing relationship, track overlap, observed hit evidence and recording/issue cost split.

Compare judgment with outcomes to assess calibration and error, not to feed terminal outcomes back into earlier
decisions. Report false positives and missed opportunities by use, source, label expertise and genre. Measure
project postponements, missed follow-up windows, debut/second-release access, idle capacity, recording-to-issue
yield and new masters per shipped title alongside the chart and financial guards.

Meaningful invariant checks required when these systems are built:

1. Reading/ranking twice with the same evidence gives the same slate and changes no cash, rights, counters,
   catalogue indexes, crew employment, material state or main RNG stream. A different enumeration order agrees.
2. A mocked future outcome cannot change an earlier decision. Missing evidence does not become a zero or a
   fabricated genre hit; independent listener reads do not overwrite the composition.
3. One session commits each master and cost once. Cancelling, retrying and reloading cannot duplicate jobs,
   fees, crew credits, publishing income or material. Failed affordability checks leave no committed session.
4. LP/extracted-single reuse preserves master ID, performance traits and credits. It charges no new recording
   cost, counts its own actual release event once and never double-applies capability/realization.
5. An LP release leaves the single clock intact. A two-sided disc counts once; flips preserve its physical issue
   and settlement identity. Contract-counter behavior changes only in its declared accounting checkpoint.
6. Concurrent single/LP proposals cannot over-reserve cash, act time or a master task. Artist refusal and material
   access remain binding; a pressured deadline cannot defeat them.
7. Departure/acquisition/closure routes pending work according to stored rights. No orphan reservations, erased
   stock, free transferred masters or duplicate refunds; recording losses remain booked.
8. Old-save migration runs once. Two loads of the same saved state reproduce the new subsystem's continuation,
   including perception bias, job dates, format clocks, reservations and counters, within the existing world RNG
   contract. Observation-only and fully disabled runs pass their declared identity comparisons.

Keep recording-formula, contract-unit, capacity and unreleased-loss-memory changes in separate checkpoints. A
failure in any one is evidence about that mechanism, not permission to compensate in the chart or demand model.

## Targets and guards (read in every screen)

These are inherited research anchors. Define the year window, chart-record versus physical-release units,
carryovers, label scope and active-act denominator beside every result. In particular, distinct records appearing
in a year and new debuts in that year are not interchangeable. Use historical variation and paired seed/year
uncertainty to set bands before a screen; do not demand every year's point estimate equal a historical mean.

- **Targets:**
  - Hot 100 debuts per hit act toward history's 3.28 / 2.18 (Big-5 / other; §17ac).
  - Acts on the Hot 100 toward 386 a year.
  - Hot 100 records per charting act toward 2.03.
  - Cold-return slots toward 17 a year.
  - Weak-genre follow-up entry (§17af).
  - Cadence distributions and LP types by lagged status/genre, after correcting current/former-label scope.
  - Material arrival, recording-to-issue yield and single/LP master overlap, as better historical data arrives.
- **Guards:**
  - Hot 100 debuts about 690 a year.
  - Top 10 turnover: 105 records a year at 4.52 weeks each.
  - #1 tenure 2.55 weeks.
  - Big-5 Top 10 share, paired, and CR4 / CR8 in band.
  - Year-end genre error per seed, no worse than the control.
  - Album titles and LP unit share.
  - Small-label closures and label cash.
  - Performance departures within +20%.
  - Development opportunities and small-label/genre feasibility; no silent starvation masked by stable totals.
  - CPU time, native allocations, peak memory, catalogue growth and save size against the predeclared budget.

## Original decisions retained and qualified (2026-10-18)

This records how the draft's prior decisions are interpreted in this revision; it does not attribute the added
proposals to the original author or turn them into already approved implementation requirements.

1. **The bar is a per-label standard, read through the label's A&R noise.** It is set by tier and archetype. A
   `TeenHitMachine` may search more aggressively and be pickier, within real capacity. Perception scales with
   `scoutingAbility` and specialism, so the label's judgement is imperfect. B-sides and album cuts have their own
   role-specific judgments rather than borrowing a universal singles threshold.
2. **When nothing clears the bar, the roll passes** to the next eligible act. It lapses only when no act on the
   roster qualifies. Retain this as a capacity-preserving allocation rule: opportunities are kept available, while
   actual shipped volume still depends on ready material and affordability. The final slate compares projects
   across acts directly; it does not retain the initial act lottery.
3. **The waiting and cadence rules come from history.** Before any of them are set, measure early-1960s release
   behaviour: singles per act per year, the gap between an act's releases, LPs per year, singles taken from albums
   against non-album singles, the lead between a single and its parent LP, and when a hits compilation followed hits.
   The modelling follows what that finds. The tables above are an initial study; scope, censoring, missing
   uncharted acts and release-date precision still limit calibration. A commercial review trigger is not a
   contractual deadline.
4. **Rival pools are not readable by the player for now.** Musicians learning what their friends are working on is
   a later feature. The pool object should not rule it out.
5. **Release-volume anchor.** *Billboard*, 6 August 1966, International Music-Record Directory, "Records Released —
   1955 Thru 1965", estimated U.S. 45 rpm singles released. Original transcription retained; scan access failed
   during this revision, so verify its issue/page and counting scope before using it as a hard calibration gate.
   [Directory scan](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1966/Billboard%201966-08-06-International-Buyers-Guide.pdf).

   | Year | 1960 | 1961 | 1962 | 1963 | 1964 |
   |---|---:|---:|---:|---:|---:|
   | Billboard, singles released | 5,797 | 6,036 | 6,700 | 6,543 | 6,504 |
   | Per week | 111 | 116 | 129 | 126 | 125 |
   | Sim, default controls (five-seed mean) | 5,065 | 5,263 | 5,512 | 5,694 | 5,828 |
   | Sim ÷ Billboard | 0.87 | 0.87 | 0.82 | 0.87 | 0.90 |

   The sim counts orphan singles plus album promo singles from `release-strategy.csv`. V1 and LR are within 2% of
   the controls. **The sim releases 10-18% fewer singles than this estimated national count.** Avoid a broad
   supply cut while testing selection, and report actual idle slots and issue volume. Passing to another project
   does not guarantee an issue. The table peaks in 1962 and then levels off, while the sim rises every year.

   **LP albums, same source:**

   | Year | 1960 | 1961 | 1962 | 1963 | 1964 |
   |---|---:|---:|---:|---:|---:|
   | Billboard, LP albums released | 3,112 | 2,927 | 3,655 | 3,686 | 3,553 |
   | Per week | 60 | 56 | 70 | 71 | 68 |
   | Sim, album projects (default controls) | 1,573 | 1,779 | 2,360 | 3,074 | 3,814 |
   | Sim ÷ Billboard | 0.51 | 0.61 | 0.65 | 0.83 | 1.07 |
   | LPs per single: Billboard / sim | 0.54 / 0.31 | 0.48 / 0.34 | 0.55 / 0.43 | 0.56 / 0.54 | 0.55 / 0.65 |

   **Coverage sensitivity, not a measured band.** The original draft proposed 85-90% coverage of actual output.
   There is no supporting measurement here. If explored, report the unadjusted count and separate scenarios
   `estimate / 0.90` through `estimate / 0.85` (approximately 11-18% above it). `estimate / 0.875` is an illustrative
   midpoint, not a justified upper bound. Do not call the estimate a proven floor, add missing regional output
   without evidence, or require the simulation to match the adjusted total.

   **What the LP anchor adds:**

   - **The national LP-to-single ratio is comparatively stable, 0.48-0.56.** The sim's ratio climbs from 0.31 to
     0.65. The sample's star ratio of 0.52 is consistent in scale, but a selected successful-act sample is not a
     validation of the national genre/format mix. Investigate the steep simulated ramp rather than imposing
     precisely 0.55 in every year.
   - **The raw 1960 LP difference is about 1,500 titles, not a measured artist-album shortfall.** The existing
     research found LP unit share sensitive to title supply ([[album-creation-is-margin-driven]],
     [[album-1960-levers-bind-in-sequence]]). First identify which additional artist projects are historically
     represented by this model; do not double their count solely to close a differently scoped national total.
     Runtime scales with album count ([[runtime-is-album-count-bound]]), so any justified expansion needs a budget.
   - **Like-for-like caveat.** *Billboard*'s LP count includes classical, spoken word, children's, budget and reissue
     lines that the sim does not model as artist albums. The sim count is artist album projects (including
     compilations), and excludes the soundtracks minted by `ExternalMediaService`. The national title count and
     national LP-to-single ratio both inherit this mismatch. Use them as context until comparable subsets are
     available, not as simultaneously a floor, upper bound and exact artist-album target.

## Decisions to settle with probes; deferred scope

Set threshold levels and bounded urgency relaxation, source-arrival rates, shortlist/reservation caps, session
batch sizes and lead times, development allowances, capacity conversion, and greatest-hits evidence/overlap
criteria from measured screens. Record each as a parameter hypothesis and its observed tradeoff. The ranges in
this draft are starting probes, not historical constants. Use existing era/genre variation; gather late-1960s
evidence before extrapolating early-decade cadence unchanged.

This design includes the rights and lifecycle groundwork for catalogue reuse. A broad former-label reissue or
licensing market, EP strategy, expanded holiday campaigns, simultaneous two-sided chart entries and player
intelligence about rival material remain separate features. Existing cover access, publishing settlement,
master-deal and seasonal behavior still apply. Keep enough identity and provenance that these additions will
not need to reconstruct songs or masters later.

The intended result is visible in an act's history: songs arrive, the label and act choose material, sessions
produce enduring masters, and releases follow reasoned commitments. Successful and unsuccessful careers should
arise from that sequence, with explanations for the decisions and the historical uncertainty still visible.
