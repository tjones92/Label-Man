# Release Judgement Directive

Status: **revised design draft; Phases 0-1 built and calibrated (shadow ConfigVersion 4); Phase 2A built behind
`--release-judgement-select` (2026-10-19), five-seed screened; v4 (access by scouting, not tier) halved the tier
gradient. Not adopted: under launch saturation F = 0.75 + push spread 0.25 its head effect is a small entry-side
+2.6 Big-5 Top 10 points; adoption is an author call on that, genre +1.8 and debuts +24. Then read-level fix and 2B.**
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

### 2026-10-19: Phase 1 calibration (pool depth and read accuracy), still observe-only

Covers open items 1-4 above. `ConfigVersion` is now 2, which re-keys every shadow draw. A v1 shadow save resets on
load, as designed.

**Method.**

- 21 seed-1001 arms on default-control flags, with `--release-judgement-shadow --release-judgement-events`:
  - 18 run 52 weeks; 3 run 104 weeks (0.07 / 3, 0.07 / 2 and 0.05 / 2).
  - The control arm also has `--polar-followup-audit`.
- Every 52-week arm matches all 84 pre-existing CSVs in `ref-094cf89-1001-52` by SHA-256. The legacy world is
  therefore identical in every arm, and releases pair by `recordId`.
- The refactored v1 control reproduces the original Phase 1 shadow files byte for byte.
- New probe switch: `--release-judgement-shadow-tune=Name:value,...`. It requires the shadow, overrides
  `ReleaseJudgement.Parameters` for a sweep, and rejects unknown names. A tuned save carries the same
  `ConfigVersion`, so use it for probes only.
- `ReleaseJudgementChecks.tscn` passes 23/23 on the new defaults. The 60-week probe state fell from 21 MB to 9 MB.
- Definitions:
  - "Strong" means best expected quality ≥ the 0.639 V1 cliff.
  - An empty shortlist counts as not strong. For quality gaps it falls back to what legacy shipped, so its gain is 0.
  - Regret is the oracle's expected quality minus the pick's.
  - A false positive (FP) is a pick read ≥ 0.639 whose truth is below 0.639, as a share of picks read ≥ 0.639.

**1. Pool depth.** Read band held at v1 (0.12 / 0.04 / 0.5); weekly rates are means over weeks 2-52.

| Base rate / init | Arrivals per act-yr | Empty | No strong song, H2 | Recordable at issue | Act-weeks at cap | Passed over / wk | Seeded picks | Gain vs legacy (H2) |
|---|---|---|---|---|---|---|---|---|
| 0.22 / 3 (v1) | 9.9 | 0.8% | 36.6% | 5.3 | 27.4% | 176 | 41% | +0.049 (+0.058) |
| 0.14 / 3 | 6.4 | 1.4% | 44.2% | 4.5 | 11.0% | 46 | 49% | +0.036 (+0.041) |
| 0.10 / 3 | 4.6 | 2.0% | 49.5% | 3.8 | 4.2% | 13 | 56% | +0.028 (+0.029) |
| 0.07 / 3 | 3.3 | 3.1% | 55.6% | 3.3 | 1.4% | 3 | 64% | +0.020 (+0.017) |
| 0.05 / 3 | 2.4 | 3.9% | 60.5% | 2.9 | 0.4% | 0.7 | 70% | +0.014 (+0.007) |
| 0.035 / 3 | 1.7 | 5.0% | 64.8% | 2.6 | 0.1% | 0.2 | 77% | +0.009 (−0.001) |
| 0.07 / 2 | 3.2 | 6.3% | 60.6% | 2.5 | 0.5% | 1.2 | 56% | +0.008 (+0.009) |
| 0.05 / 2 | 2.3 | 8.4% | 66.3% | 2.1 | 0.1% | 0.3 | 63% | +0.002 (−0.001) |
| 0.07 / 1 | 3.2 | 15.2% | 65.0% | 1.7 | 0.2% | 0.4 | 42% | −0.005 (+0.003) |
| 0.07 / 3, cap 5 | 3.3 | 3.1% | 55.6% | 3.1 | 15.2% | 33 | 64% | +0.020 (+0.017) |

All arms issue about 1.8 singles per signed act-year.

- **Initial stock decides a one-year read.**
  - At init 3, seeded songs are 64% of first-year picks, and the year flatters itself. The 104-week 0.07 / 3 arm
    shows this: gain falls from +0.021 (1960) to +0.015 (1961), and passed-over rises from 3 to 13 a week as stock
    climbs back toward the cap.
  - At 0.07 / 2 the run is stationary:

    | | 1960 | 1961 |
    |---|---|---|
    | Empty | 6.3% | 8.0% |
    | Gain vs legacy | +0.0086 | +0.0088 |
    | Seeded share of picks | 55% | 28% |
    | Recordable per act at year end | 2.8 | 3.0 |

  - Init 1 is too thin: one song per new act gives 15% empty, and the shadow does worse than legacy.
  - 0.05 / 2 runs empty 8.5% then 13.1%, with gain ≈ 0 both years.
- **The cap does not bind once arrivals are credible.** Cap 5 changes no pick at 0.07; it only adds 33 passed-over
  archives a week. Cap 8 stays as the bound on state size.
- **Legacy is not a lottery baseline.** `ChooseMaterial` takes the best of a 6-song sample per source, weighted by an
  exact fit score, from an unlimited refill. A thin shortlist can therefore lose to it, and does at init 1 and at
  0.05 in steady state.
  - The v1 gain of +0.05 was mostly pool depth.
  - With a credible pool, a 2A selection gain is about +0.01, not +0.05.

**2. Read accuracy.** Pool held at 0.07 / 2, which leaves about 2.5 recordable songs at issue.

| Max / Min / BiasShare | Song-error half-width (max → min) | Read error SD | Regret | Oracle pick | Winner's curse | FP | FN | Gain vs legacy |
|---|---|---|---|---|---|---|---|---|
| 0.12 / 0.04 / 0.50 (v1) | 0.06 → 0.02 | 0.026 | 0.0015 | 89% | +0.002 | 11% | 9% | +0.008 |
| 0.12 / 0.04 / 0.25 | 0.09 → 0.03 | 0.029 | 0.0031 | 85% | +0.005 | 13% | 9% | +0.007 |
| 0.20 / 0.08 / 0.50 | 0.10 → 0.04 | 0.047 | 0.0042 | 83% | +0.007 | 19% | 14% | +0.006 |
| **0.20 / 0.08 / 0.25** | 0.15 → 0.06 | 0.051 | 0.0081 | 78% | +0.014 | 24% | 14% | +0.004 |
| 0.30 / 0.12 / 0.50 | 0.15 → 0.06 | 0.069 | 0.0081 | 78% | +0.013 | 28% | 18% | +0.004 |
| 0.30 / 0.12 / 0.25 | 0.23 → 0.09 | 0.075 | 0.0143 | 71% | +0.027 | 34% | 17% | −0.001 |
| 0.40 / 0.16 / 0.25 | 0.30 → 0.12 | 0.098 | 0.0198 | 67% | +0.040 | 42% | 19% | −0.005 |

- **Only the song-specific error ranks one act's candidates.** 0.20 / 0.25 and 0.30 / 0.50 have the same song error
  and pick identically (regret 0.0081 each). They differ only in the persistent bias: FP 24% against 28%.
  - Selection accuracy is therefore governed by `band × (1 − BiasShare)`.
  - A later bar's false positives are governed by the bias.
- **Regret scales with pool depth.** In the saturated v1 pool, 0.30 / 0.12 / 0.25 has regret 0.024, picks the
  oracle 54% of the time, and still gains +0.030. In the credible pool the same reads lose to legacy.
- **Chosen: 0.20 / 0.08 / 0.25.**
  - It keeps the persistent-bias half-width where v1 had it (0.05 → 0.02, against 0.06 → 0.02).
  - It widens only the song error, 2.5×. Song-error SD is about 0.035-0.087, against a best-minus-mean shortlist
    spread of about 0.040.
  - Labels pick the best available song about three times in four. Regret is 5× v1, and the shadow still edges
    legacy.
  - Wider reads make the shadow strictly worse than legacy's exact-fit sampling. That would test a wrong
    comparison rather than A&R.

**3. Head concentration.** Shadow gain by tier, seed 1001:

| Setting | Major | MidTier | Boutique | Independent | Small | Major − Small |
|---|---|---|---|---|---|---|
| v1 (0.22 / 3, v1 reads) | +0.059 | +0.041 | +0.055 | +0.049 | +0.043 | 0.016 |
| 0.07 / 3, v1 reads | +0.032 | +0.012 | +0.025 | +0.021 | +0.015 | 0.017 |
| 0.07 / 2, v1 reads | +0.020 | −0.001 | +0.011 | +0.010 | +0.003 | 0.017 |
| 0.07 / 2, 0.20 / 0.08 / 0.25 (tuned) | +0.017 | −0.005 | +0.005 | +0.006 | −0.003 | 0.020 |
| v2 defaults (re-keyed) | +0.014 | −0.001 | +0.013 | +0.001 | −0.005 | 0.019 |

- **The absolute Major − Small gap does not move with the setting.** It is 0.015-0.020 under every pool and read
  setting, while every tier's gain shrinks. In relative terms the shadow's gain becomes almost all head.
- **Where it comes from:**
  - At 0.07 / 3, Majors hold 3.65 recordable songs at issue against 3.29 for Small, through `labelAccess` in
    professional arrivals.
  - Their read band is narrower: 0.099 against 0.125 on v2.
  - Their legacy baseline is also higher: 0.630 against 0.593.
- **Re-keying alone moved tier gains by up to ±0.008.** Compare the tuned row with the v2 row; Boutique went from
  +0.005 to +0.013. Tier differences below about 0.01 are noise on one seed.
- **Phase 2A must read the head on paired seeds.** Read Big-5 / CR4 / CR8 with ≥ 2 seeds, and consider moving
  access from arrival count to arrival quality before reading tier outcomes.

**4. Refusals.** In the same world, legacy refusals are also zero:

- `polar-refusal-avoidance.csv` has 0 events ("excluded", "unfilled" and "live-unfilled" alike).
- 0 of 21,244 polar master assignments (3,736 singles, 17,508 album tracks) carry `strongRefusal`.
- Refusal needs identity < threshold and stretch ≥ 0.9 × capability at once:
  - The AI threshold is at most (0.30 + 0.35) × 0.5 = 0.325, because AI acts have no `PlayerDesk` songbook and
    get `emptySongbookSoftening`.
  - Shipped identity is never below 0.55, and stretch never exceeds 0.33 against median capability 0.98.
- **The shadow's zero is therefore correct: the rule is dormant for AI acts on 1960 defaults.**
- The shadow is the stricter of the two:
  - It passes `emptySongbook: false`, an unsoftened threshold up to 0.65.
  - It reads the raw `Fit` rather than `Prepare`'s arrangement proposal.
- `emptySongbook: false` stays: once the shortlist is the songbook, an AI act has songs. Phase 2A should judge
  refusal on the proposal fit it will actually realize.

**Chosen v2 defaults:**

- `BaseArrivalRate` 0.07, about 3.2 arrivals per signed act-year in 1960 and 3.9 in 1961.
- `InitialStock` 2.
- `ShortlistCap` 8, unchanged.
- Reads 0.20 / 0.08 / `BiasShare` 0.25.
- `RecentHitWindowWeeks` and the retention windows are unchanged.

**v2 default arm `rjt-v2-1001-52`:**

- Inertness: 84/84 hashes match.
- Pool: empty 6.1% (H2 8.1%); no strong song 60.8%; 2.5 recordable at issue; 0.5% of act-weeks at cap.
- Turnover per week: 0.9 passed over, 16 window-closed, 96 issued. Stock now turns over through issue and the
  recent-hit window, not at the cap.
- Reads: regret 0.0079, oracle pick 77%, read error SD 0.051, FP 23%.
- Gain vs legacy +0.003.

**Tradeoffs to carry into Phase 2:**

- **0.07 is about twice the A-side issue rate.** The shadow consumes one song per single. Once 2B populates
  B-sides from stock, consumption roughly doubles, and 0.07 will deplete hard. Re-tune arrivals together with 2B
  rather than reuse this number.
- **Arrivals rise into 1961** (3.2 → 3.9 a year, passed-over 1 → 8 a week) as recent hits accumulate in the
  catalogue. Watch decade drift before trusting a late-decade read.
- **2A's honest expected gain on this pool is about +0.003-0.009 per single.** That is within one-seed noise, so it
  needs paired seeds.
- **A gain only appears with a deeper pool, with more accurate reads, or both.** Both are levers that favour the
  head (item 3).

### 2026-10-19: Phase 2A (selection only), ConfigVersion 3

**Built.**

- `--release-judgement-select=perceived|fit` (requires the shadow). Any other value is rejected.
- *Where it applies:* only the live AI orphan single. `TryReleaseRecord` passes `judgedOrphan: true` to
  `GenerateRecordFromArtist`; prewarm, album and promo paths are untouched.
- *What it picks:*
  - The act's top recordable shortlist song by the arm's score.
  - Perceived: the label's read. Fit: the exact polar selection score alone, with no song-quality read.
- *How it realizes the pick (once, through the fixed-composition routes):*
  - Covers and traditional songs use `BuildCoverForSong`; professional songs use `BuildProfessionalForSong`.
  - Originals use the new `SongMaterialSelectionService.BuildArtistOriginalForSong`.
  - An original is authored with `CreateArtistOriginal` on its first recording. The candidate keeps that
    `SongId`, so a later take (for example after a failed affordability check) reuses the composition.
- *Refusal and fallback:*
  - Refusal is judged on the arrangement proposal it will realize, trying up to 3 candidates.
  - An empty or refused shortlist falls back to legacy `ChooseMaterial`.
  - A player placement still overrides the pick, and the pick is then not consumed.
- *Consumption:*
  - The release consumes exactly the song it was cut from. A fallback consumes nothing.
  - A promo single still consumes the best perceived song counterfactually, as Phase 1 calibrated it.
- *Shadow v3:* an unauthored original is heard as a provisional, unregistered demo, so its capability is read
  rather than assumed perfect.
  - Draw keys moved to a separate `StreamVersion` (still 2), so this rule change re-keyed nothing.
- *Telemetry:* `release-judgements-shadow.csv` gains `selectMode`, `outcome`, `selectedId` and `selectedSource`.
  `SimTools/release_selection_screen.py` reads a paired screen.

**Correctness.**

- `ReleaseJudgementChecks` passes 32/32. The 9 new checks cover:
  - the cut comes from the top-ranked song under each arm;
  - no global RNG draw;
  - an original is authored once, and a second take reuses it;
  - the realized record carries the composition;
  - the release consumes exactly that song, and a consumed song is not cut again;
  - an empty shortlist falls back and consumes nothing.
- With selection off and the shadow on, `rj2a-inert-1001-52` matches all 84 legacy CSVs in
  `ref-094cf89-1001-52`.
- On the live arms, 93.7% of orphan singles were cut from the pool and 6.3% fell back on an empty shortlist.
  Nothing was refused at session.

**Paired screen.**

- Seeds 1001 and 2002, 260 weeks, default-control flags with `--calibration`.
- The control arm runs the shadow without selection, so it is legacy-identical.
- Arm − control, mean of two seeds. Every perceived row has the same sign in both seeds; fit-only debuts, CR4 and
  CR8 do not:

| | Control (1001 / 2002) | Perceived | Fit-only |
|---|---|---|---|
| Orphan realized quality | 0.600 / 0.598 | **+0.029** | +0.002 |
| Orphan > 0.639 cliff | 35.7% / 34.2% | +11.9 pts | +1.6 pts |
| Orphan Hot 100 entry | 12.2% / 12.4% | +3.1 pts | +1.0 pt |
| Orphan Top 40 | 4.1% / 4.0% | +1.0 pt | +0.5 pt |
| Hot 100 debuts / yr (guard about 690) | 748 / 732 | +22.7 | +2.5 |
| Major-family chart share | 0.535 / 0.491 | +0.045 | +0.036 |
| CR4 / CR8 | 0.213, 0.310 / 0.212, 0.323 | +0.030 / +0.035 | +0.003 / +0.013 |
| Top 40 entries: Major / Independent | | +9.0 / −12.3 | +3.7 / −7.3 |

- **Perceived selection works, and works much better than the shadow predicted.**
  - Realized quality +0.029 against the shadow's +0.003-0.009 estimate.
  - Two reasons:
    - The shadow's expectation runs 0.012 low on both hook and production for every source. Realization adds
      the session crew's production lift and uses the arrangement proposal's capability, which is higher than the
      raw `Fit` the shadow reads.
    - Over five years the pool deepens (best expected 0.613 → 0.632 in the arm).
  - The bias is roughly uniform across sources, so ranking is barely affected. A Phase 3 bar must still correct
    it before reading absolute levels.
- **Fit-only is a near-null on quality:** +0.002 at the same pool and consumption. Exact fit without a song read
  selects no better than legacy's best-of-6 sampling.
- **The head.**
  - Quality gain by tier in the perceived arm: Major +0.044, Boutique +0.040, MidTier +0.034, Independent +0.025,
    Small +0.025.
  - That is the same ~0.02 Major − Small gradient the shadow showed.
  - Major-family share rises in both arms, most in 1960 (+0.08), with almost no change in Major chart entries.
    That is the reshuffled hit lottery reweighting a few big records, not selection.
  - The fit arm shares the pool and consumption but gains no quality, so it serves as the reshuffle-and-pool
    comparator. Perceived − fit isolates the ranking's head effect: major share +0.009, CR4 +0.027,
    CR8 +0.022, Major Top 40 +5.3, Independent Top 40 −5.
  - The +0.036 that both arms share is pool-versus-legacy plus reshuffle noise. Two seeds cannot separate them.
- **The shipped source mix moves (1960, seed 1001, full telemetry):**
  - Under perceived, professional rises 20-28% → 30-40% by tier, and artist-written falls about 30% → 20-27%.
  - Standards fall for Majors (17.5% → 10%), and familiarity at release falls (0.13 → 0.11).
  - Perceived selection prefers professional hooks, which runs against the decade's rise of self-writing. This is
    the "arrival versus shipped mix" risk the design names; read it on the decade before adoption.
- Hot 100 debuts were already above the 690 guard in the control (748). Perceived adds about 23 a year.

**Status and next.**

- 2A is correct, inert when off, and delivers a real material-quality gain (+0.029, +3 pts entry) on paired seeds.
- It is not adoptable on this evidence:
  - The head drift (CR4 +0.03, Independent Top 40 −12) needs a five-seed paired screen with the fit arm as the
    reshuffle comparator, plus the §17o yearly watch.
  - The source-mix drift needs a decade read.
- Before Phase 3's bar, fix the read model's level bias (crew lift and proposal capability), because a bar reads
  absolute levels.
- Before 2B, revisit arrival rates. Consumption doubles once B-sides draw from stock.

### 2026-10-19: Phase 2A five-seed paired head screen (measurement only, 6bf67b1)

**Method.**

- Seeds 1001-5005, 260 weeks, default-control flags with `--calibration`. Three arms per seed: control
  (`--release-judgement-shadow`), perceived and fit (`--release-judgement-select=perceived|fit`).
- 1001/2002 are the earlier screen's runs. 3003/4004/5005 are new; all nine exited cleanly. They were renamed to the
  same `SimLogs/rj2a-{ctl,per,fit}-<seed>-260-*` scheme.
- Read with `release_selection_screen.py` over all five pairs. The yearly view came from `concentration.csv` and
  weekly Top 10 slot-weeks in `records.csv`, using the §17o definitions (Top 10 CR4 / CR8, Big-5 = `label_0001-0005`).
  Closures are a label's first `Defunct` week in `label-finance.csv`.
- "Run mean" averages the five chart years. SE is the across-seed SD / √5. The sign count is the number of seeds
  above zero out of 5.

**1. Material gain: robust in every seed.**

| Arm − control | Perceived, mean (seed range) | Fit, mean (seed range) |
|---|---|---|
| Orphan realized quality | **+0.029** (+0.028 to +0.031) | +0.003 (+0.002 to +0.005) |
| Orphan > 0.639 cliff | +11.5 pts (+10.7 to +12.3) | +1.4 pts (+0.9 to +2.2) |
| Orphan Hot 100 entry | +2.8 pts (+2.3 to +3.4) | +0.6 pt (−0.1 to +1.1) |
| Orphan Top 40 | +1.0 pt (+1.0 to +1.1) | +0.2 pt (+0.1 to +0.5) |

- **The tier gradient is structural.** Perceived quality gain by tier, five-seed mean (range):
  - Major +0.044 (0.036-0.054), Boutique +0.038, MidTier +0.036.
  - Independent +0.024 (0.022-0.027), Small +0.025 (0.022-0.030).
  - Major's lowest seed beats Small's highest. The ~0.02 head-to-field gap that Phase 1 predicted holds in all five
    seeds.
- **The shipped mix moves in every seed.** Perceived picks 37.8-39.1% professional and 27.4-29.0% artist-written.
  Fit picks 33.7-35.5% and 30.7-32.6% from the same pool.

**2. The ranking's head effect (perceived − fit) is established.** Run means; chart shares are in points.

| Perceived − fit | Mean | SE | Seeds > 0 |
|---|---|---|---|
| Major-family chart share | +2.1 | 0.9 | **5/5** |
| CR4 / CR8 (chart units) | +2.2 / +2.7 | 0.9 / 1.3 | 4/5, 4/5 |
| Top 10 CR4 / CR8 (slot-weeks) | +2.1 / +2.5 | 1.5 / 1.5 | 4/5, 4/5 |
| Big-5 share of the Top 10 | +2.7 | 1.5 | 3/5 |
| Top 40 entries a year: Major | +7.3 | 1.9 | **5/5** |
| Top 40: Boutique | +5.3 | 1.1 | **5/5** |
| Top 40: MidTier | −2.9 | 2.2 | 1/5 |
| Top 40: Independent | −6.4 | 0.9 | **0/5** |
| Hot 100 debuts a year | +14.5 | 2.8 | 5/5 |

- **Seed 3003 is the only reversal** on CR4 / CR8, and it is small (−1.0 / −1.5).
- **The tiers that gain least lose chart space.** Independent loses Top 40 in every seed, and its quality gain is
  the smallest (+0.024). Boutique gains slots (+0.038) as Majors do. This follows the per-tier quality gradient, not
  firm size alone.
- **It is not reshuffle.** The fit arm takes the same pool and consumption and gains +0.003 in quality. The head moves
  with the ranking's quality gain.

**3. The shared pool-versus-legacy shift (fit − control) is mostly reshuffle noise.**

| Fit − control | Two-seed mean (earlier screen) | Five-seed mean (SE) | Seeds > 0 |
|---|---|---|---|
| Major-family share | +3.6 | +1.7 (1.2) | 4/5 |
| CR4 / CR8 | +0.3 / +1.3 | +0.4 / +1.0 (1.1 / 1.3) | 3/5, 3/5 |
| Top 10 CR4 | | +0.6 (1.6) | 3/5 |
| Top 40 Major / Independent | +3.7 / −7.3 | +1.4 / −3.7 (5.3 / 2.5) | 3/5, 2/5 |

- The two original seeds were the high ones; 2002 alone gave +5.4 major-family share. Concentration's share is gone.
- A +1.7 major-family residual (1.5 SE) may be a small real pool effect. Five seeds cannot confirm it, and nothing
  in it is large enough to act on.
- Perceived − control is therefore mostly ranking (55-85% of each head row below), with the rest shared or noise:
  - Major-family share +3.9 (SE 1.3, 4/5); CR4 +2.6 (1.5, 4/5); CR8 +3.7 (1.6, 4/5).
  - Top 40 Major +8.7 (4/5); Top 40 Independent −10.1 (**0/5**).

**4. Yearly watch (§17n/§17o review bands; Top 10 slot-weeks).**

| Year | Top 10 CR4: ctl / per / fit | Band | Top 10 CR8: ctl / per / fit | Band |
|---|---|---|---|---|
| 1960 | 34.5 / 36.2 / 34.5 | 30-40 | 48.0 / 52.7 / 50.4 | 48-58 |
| 1961 | 31.8 / **34.2** / 30.3 | 22-32 | 47.0 / **49.6** / 44.4 | 38-48 |
| 1962 | 29.5 / **36.5** / **35.0** | 22-32 | 43.3 / **51.3** / **48.7** | 38-48 |
| 1963 | 34.5 / **36.0** / 34.4 | 22-32 | 48.0 / **49.0** / 48.2 | 38-48 |
| 1964 | 31.4 / 32.0 / 30.5 | 22-32 | 43.0 / 46.1 / 44.6 | 38-48 |

Bold = an arm outside the band. The control is itself 2.5 above the CR4 band in 1963.

- **Perceived breaches the §17n stop rule** (more than 2 points outside the band and worse than the control):
  - CR4 in 1961 (2.2), 1962 (4.5) and 1963 (4.0, against the control's 2.5).
  - CR8 in 1962 (3.3).
- **Fit also breaches in 1962** (CR4 3.0), so that year is partly shared. 1961 and 1963 are the perceived arm alone.
- Big-5 firms in the top four do not move (+0.0 per − fit). The head gain spreads across the top eight rather than
  displacing a Big-5 leader.

**5. Other guards.**

- **Hot 100 debuts:**
  - The control is already above the ~690 guard: 737 a year (719-748).
  - Perceived adds +19.2 a year (5/5, SE 3.5), to about 756 (+9.6% over the guard). Fit adds +4.7 (3/5).
  - Top 10 records a year are unchanged (+0.7; guard 105).
- **Small-label closures fall under perceived:**
  - −9.0 over five years (5/5, SE 1.1), against a control of 59-82 per seed. All closures fall −12.6 (4/5).
  - Fit gives −5.8 (2/5), and perceived − fit is −3.2 (2/2/1 split).
  - Better material helps small labels survive, partly through the shared pool. Nothing here is a guard failure.
- Promo-single quality and orphan volume do not move: +0.001, and +24 a year on about 2,960 (mixed signs).

**Recommendation: do not adopt 2A as built. Keep it behind the flag, off by default.**

- The material gain is real, robust and large: +0.029 quality and +2.8 pts entry in 5/5 seeds.
- But it is delivered unevenly, and the unevenness reaches the chart in every seed:
  - perceived − fit major-family share +2.1 (5/5);
  - Top 40 Major +7.3 (5/5) against Independent −6.4 (0/5);
  - Top 10 CR4 out of band in 1961-63.
- This is risk 7 and Phase 1 open item 3, now measured on the chart. The ~0.02 gradient comes from `labelAccess`
  in professional arrivals (pool depth) and from Majors' narrower read band. Selection converts both into a quality
  edge.
- Fix the source before re-screening, rather than adding a head brake:
  - move access from arrival *count* to arrival *quality* (or flatten it);
  - or decouple the read band from tier-correlated `scoutingAbility` for pool ranking.
  Then re-run this five-seed screen. The pass condition is Independent Top 40 ≥ −2 with mixed signs, perceived − fit
  major-family share within 1 SE of zero, and the 1961-63 Top 10 CR4 inside the stop rule.
- Carry these forward:
  - **The debut overshoot.** Perceived widens an existing +47 over the guard to +66. The guard needs its own
    look, in the control first.
  - **The source-mix shift.** Professional +4 pts and artist-written −3 pts against fit. It still needs a decade read.
  - **The read-level bias fix before Phase 3**, unchanged from the entry above.

Evidence: `SimLogs/rj2a-{ctl,per,fit}-{1001,2002,3003,4004,5005}-260-*`.

### 2026-10-19: ConfigVersion 4, professional access by label scouting, not tier

**Diagnosis first (control arms, five seeds, shadow log).** The shadow's gain over legacy splits into pool headroom
(oracle minus what legacy shipped) and read regret (oracle minus the pick):

| Tier | Recordable at issue | Read band | Regret | Headroom | Shadow gain |
|---|---|---|---|---|---|
| Major | 3.90 | 0.102 | 0.0091 | **0.0356** | 0.0265 |
| Boutique | 3.43 | 0.112 | 0.0096 | 0.0335 | 0.0240 |
| MidTier | 3.12 | 0.112 | 0.0083 | 0.0293 | 0.0211 |
| Independent | 2.97 | 0.116 | 0.0089 | **0.0208** | 0.0119 |
| Small | 3.14 | 0.126 | 0.0103 | 0.0205 | 0.0102 |

- **The read band is not the channel.** Regret is 0.008-0.010 in every tier, about 0.001 of a ~0.015 gap.
  - `scoutingAbility` is drawn per label from overlapping tier envelopes (Major 0.68-0.96, Independent 0.44-0.91,
    Small 0.34-0.84). It is an on-average edge, not a blanket bonus, so it is unchanged.
- **The gap is pool headroom.** Majors hold deeper shortlists, and their best song beats the pool mean by more.
- **The one true blanket bonus is `LabelAccess(tier)`:** a fixed table (Major 0.90, MidTier 0.65, Boutique 0.50,
  Independent 0.38, Small 0.25). It scales professional arrivals, so a Major got ~1.9× a Small label's.

**Change.**

- `LabelAccess(label) = 0.63 × scoutingAbility`. Publishers' pluggers now reach a label through its A&R strength.
- The 0.63 holds the orphan-weighted mean arrival multiplier at v3's (~0.93). The professional pick share is
  unchanged: 38.8% perceived, 34.9% fit.
- The Major / Small multiplier ratio falls from 1.87 to about 1.17, and individual labels overlap across tiers.
- The legacy-realization `polish` term (polar off only) reads the same function.
- `ConfigVersion` 4 (a v3 shadow save resets on load). `StreamVersion` is unchanged, and the change adds no draws.

**Gates.**

- `ReleaseJudgementChecks` passes 32/32.
- `rj2a-v4inert-1001` (shadow on, selection off, 52 weeks, full telemetry) matches all 84 legacy CSVs in
  `ref-094cf89-1001-52`. The controls `rj2a-ctl-*-260` are therefore still valid and were reused.

**Five-seed screen** (`rj2a4-{per,fit}-<seed>-260`, same flags). Changes are against the v3 arms above.

| | v3 | v4 |
|---|---|---|
| Orphan quality, perceived − control | +0.029 | +0.029 (5/5) |
| Orphan Hot 100 entry, perceived − control | +2.8 pts | +2.6 pts (5/5) |
| Quality gain: Major / Independent / Small | +0.044 / +0.024 / +0.025 | +0.037 / +0.027 / +0.027 |
| Major − Independent gain gap | 0.020, no seed overlap | **0.010**, ranges overlap |
| Top 40 Independent, perceived − fit | −6.4 (0/5) | **−1.6** (3/5, SE 4.0) |
| Top 40 Independent, perceived − control | −10.1 (0/5) | −4.3 (1/5) |
| Top 40 Boutique, perceived − fit | +5.3 (5/5) | +2.8 (5/5) |
| Top 40 Major, perceived − fit | +7.3 (5/5) | +7.7 (5/5) |
| Major-family share, perceived − fit | +2.1 (5/5) | +2.0 (4/5, SE 1.2) |
| CR4 / CR8, perceived − fit | +2.2 / +2.7 | +1.9 / +2.0 (5/5) |
| Big-5 share of the Top 10, perceived − fit | +2.7 (3/5) | +4.3 (5/5) |
| Major-family share, fit − control | +1.7 | +0.8 (3/5) |
| Hot 100 debuts / yr, perceived − control | +19.2 | +16.4 (5/5) |
| Small closures over 5 years, perceived − control | −9.0 (5/5 fewer) | −3.8 (mixed) |

**Yearly Top 10, perceived v4:**

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| CR4 | 35.5 | 31.7 | **35.7** | **36.9** | 30.9 |
| CR8 | 51.3 | 46.4 | **50.6** | **51.1** | 43.5 |

- 1961 is now inside both bands.
- 1962-63 still breach the §17n stop rule, by 2.6-4.9 points. The control misses only 1963 CR4, by 2.5.
- The fit arm is inside the bands both years (1962 CR4 32.1, at the edge), so this breach is now the ranking's alone.

**Against the pass conditions set above:**

1. **Pass:** Independent Top 40 ≥ −2 with mixed signs.
2. **Fail:** perceived − fit major-family share within 1 SE of zero. It is +2.0 at 1.6 SE.
3. **Half:** Top 10 CR4 is inside the stop rule in 1961 but not in 1962-63.

**What is left is conversion, not material.** Control Top 40 rate by tier, five seeds:

| Orphan quality | Major | MidTier | Independent | Boutique | Small |
|---|---|---|---|---|---|
| 0.639-0.68 | 18.3% | 5.3% | 0.6% | 0.3% | 0.1% |
| > 0.68 | 59.5% | 33.4% | 17.0% | 12.9% | 4.3% |

- A Major single at the cliff band reaches the Top 40 about 30× as often as an Independent one.
- Within a band, perceived does not raise any tier's Top 40 rate over fit; every tier is 0 to −5 pts, from more
  competition.
- The residual head shift is therefore the existing launch / promotion conversion gap (Mode A,
  `top40-is-a-launch-week-bar`) applied to better material, plus the 0.010 average quality edge from better A&R. No
  song-selection rule can remove it without handicapping the Majors' A&R.

**Status.**

- v4 is kept. It removes a hard-coded tier bonus, halves the tier gradient and ends the Independent Top 40 loss
  against fit. It is inert when off.
- 2A stays off by default. Adoption needs one of two things:
  - an author decision that a residual +2 major-family share and the 1962-63 Top 10 breach are acceptable when they
    come from conversion;
  - or a fix to the conversion gap itself, which belongs to the chart / launch work, not Release Judgement.
- Still owed: the debut overshoot (control +47 over the guard; perceived +16), the decade source-mix read, and
  the read-level bias fix before Phase 3.

Evidence: `SimLogs/rj2a4-{per,fit}-{1001,...,5005}-260-*`, `SimLogs/rj2a-v4inert-1001-*`, with controls
`SimLogs/rj2a-ctl-*-260-*`.

### 2026-10-19: Why a Major converts the same song better (analysis only, author: keep 2A off until this is settled)

**Method.**

- Four control seeds (1001-4004) re-run with the read-only `--head-hit-attribution`, which logs launch inputs for
  every single, misses included.
- `records`, `concentration`, `single-launch-quality` and `label-finance` are SHA-256 identical to `rj2a-ctl-*`.
- 59,229 orphan launches 1960-64. Logits of Top 40 and Hot 100 entry on:
  - quality (with a square term) and campaign impact as read (after `--radio-push-spread`);
  - payola, log launch stock, launch awareness, recognition, label reputation;
  - career state, genre, year and seed.
- Pseudo R² is 0.60 for Top 40 and 0.58 for entry. Each row below moves one input group of the Major launches to
  the Independent mean. The rows are not additive.

**At the same song (quality 0.639-0.68), launch inputs:**

| | Major | MidTier | Independent | Small |
|---|---|---|---|---|
| Campaign impact as read (raw) | 0.62 (0.76) | 0.50 (0.52) | 0.41 (0.35) | 0.35 (0.22) |
| Launch stock | 71.6k | 56.2k | 37.9k | 16.5k |
| Launch awareness | 0.44 | 0.29 | 0.20 | 0.16 |
| Label reputation | 0.72 | 0.49 | 0.35 | 0.20 |
| Hot 100 entry / Top 40 | 51.7% / 16.4% | 25.3% / 4.8% | 10.3% / 0.5% | 2.5% / 0.1% |

**What carries the Major − Independent gap** (all orphan launches; Top 40 19.9% against 3.6%, entry 42.2% against
11.7%):

| Moved to the Independent mean | Top 40 gap closed | Entry gap closed |
|---|---|---|
| Label push (campaign impact) | **43%** | 35% |
| Record quality (0.629 against 0.599) | 38% | 31% |
| Launch stock | 28% | 16% |
| Launch awareness | 21% | **37%** |
| Label reputation | 11% | 9% |
| Recognition and career state | 8% | −1% |
| Payola | 0% | 0% |
| Genre | −13% | −9% |
| All groups | 94% | 96% |

- **Three label-side launch reads carry most of it:** campaign impact in launch radio
  (`0.3 × impact` beside `0.3 × quality`), stock and launch awareness. Awareness itself is mostly marketing
  (`GetSeasonalMarketingImpact`) plus `0.1 × reputation`.
- **The act's own standing carries little.** Payola is flat across tiers. Genre runs against the Majors.

**Why 2A's gain lands on the head.** The Top 40 points bought by +0.029 quality (2A's gain), predicted per tier:

| | Major | MidTier | Independent | Boutique | Small |
|---|---|---|---|---|---|
| Top 40 points per +0.029 | **7.4** | 4.9 | 2.4 | 2.2 | 0.7 |

- With Major push, awareness and stock moved to the Independent mean, a Major's +0.029 buys 3.7 points; with push
  alone, 5.6.
- The cliff is in realized quality. Label push moves a Major's single to its edge, so every quality gain is worth
  about 3× as much there.

**Against history.**

- The control's Big-5 share of the Top 10 is 31.9 / 29.2 / 36.1 / 30.9 in 1961-64. History is 20.6 / 21.2 / 26.0 /
  18.7, so the control is about 10 points high before 2A.
- §17aa found history's Big-5 hit acts converted a Hot 100 debut into the Top 10 at exactly the field's rate (ratio
  1.00 ± 0.18). Their edge was cadence, not conversion.
- History cannot give a per-launch rate for cold acts. But the head the model's conversion premium produces is
  already ahistorical on defaults, and 2A only scales it.

**Prior levers on the same channel (`HistoricalLabelTierBalanceResearch.md`):**

- `--launch-push-saturation=0.5` (§17o) trims the launch reads of campaign impact (radio and seasonal marketing).
  It cut the Big-5 Top 10 share −5.5 / −5.7 in 1960-61 and faded by 1962. It was not adopted: it failed the
  "Big-5 firms in the top four" kill, and senior acts carried the head.
- `--known-act-airplay` (§17ab-ac) was rejected.
- `--radio-push-spread=0.5` is already in these controls (raw 0.76 → 0.62 for Majors). It narrows but does not close
  the gap.
- None was tested on cold orphan singles' conversion, which is where 2A acts.

**Proposed next screen (not run).**

- 2A under `--launch-push-saturation=0.5`: control, perceived and fit arms, five seeds × 260 weeks (15 runs, about
  50 minutes).
- Read perceived − fit against the v4 screen for major-family share, Top 40 Major / Independent and the 1962-63
  Top 10 CR4.
- Read the saturation arm's control against `rj2a-ctl-*` for the Big-5 Top 10 share against history.
- *Pass for 2A:*
  - perceived − fit major-family share within 1 SE of zero;
  - Top 10 CR4 inside the stop rule in 1961-63.
- *If saturation alone leaves the head:* the remaining launch reads are stock and awareness. They need their own
  probe, because distribution "fulfills demand rather than creating it" by the code's own rule, yet stock carries
  28% of the Top 40 gap.

Evidence: `SimLogs/rj2aconv-ctl-{1001,2002,3003,4004}-260-*` (with `single-launch-attribution.csv`). The
script `conv_decomp.py` and its output are in the session notes folder.

### 2026-10-19: Screen result, 2A under `--launch-push-saturation=0.5` (`rj2als-*`, v4 code)

**Runs.** Control, perceived and fit arms, five seeds × 260 weeks, the same flags as `rj2a-*` plus the saturation
switch. All 15 completed cleanly.

**1. The material gain is untouched.** Perceived − control:

- Orphan quality +0.031 (5/5, +0.026 to +0.033); share above the cliff +12.5 pts.
- Hot 100 entry +2.9 pts; Top 40 +1.05 pts.

**2. The Top 40 head shift is gone; a smaller Top 10 shift remains.**

| | Perceived − fit, v4 | Perceived − fit, saturation | Perceived − control, saturation |
|---|---|---|---|
| Major-family share | +2.0 (SE 1.2, 4/5) | +1.6 (1.4, 3/5) | **+0.3** (1.3, 2/5) |
| Top 40 Major | +7.7 (5/5) | **+1.4** (4.1, 3/5) | −2.1 (4.7, 2/5) |
| Top 40 Independent | −1.6 (3/5) | −2.3 (2.7, 1/5) | +0.5 (2.2, 2/5) |
| CR4 / CR8 (units) | +1.9 / +2.0 (5/5) | +1.2 / +1.1 (3/5) | +0.2 / +0.0 (3/5) |
| Top 10 CR4 | +3.4 | +2.3 (2.2, 4/5) | +0.7 (2.4, 3/5) |
| Big-5 share of the Top 10 | +4.3 (5/5) | +3.1 (2.7, 4/5) | +2.4 (4.2, 3/5) |
| Hot 100 debuts / yr | +8.4 | +18.2 (5/5) | +23.0 (5/5) |

- Against its own control, perceived selection no longer moves the head on any row.
- Against fit, two rows carry over from v4:
  - Big-5 Top 10 +3.1 (4/5) and Top 10 CR4 +2.3. These are post-entry, where §17aa placed the weekly label push in
    radio. Saturation only trims the *launch* reads.
  - Independent Top 40 −2.3 (1/5). The fit arm's own drift (Top 40 Major −3.4, major share −1.3 against the
    control) inflates the contrast.
- **Against the pass conditions:**
  1. Independent Top 40 ≥ −2 with mixed signs: borderline. Perceived − fit is −2.3 (SE 2.7); perceived − control
     is +0.5, mixed.
  2. Perceived − fit major-family share within 1 SE of zero: borderline at 1.2 SE. Perceived − control is
     +0.3, inside.
  3. Top 10 CR4 inside the stop rule in 1961-63: **pass** (perceived 31.2 / 27.6 / 27.2).

**3. Saturation on these defaults fixes 1961-64 and breaks 1960.** Saturation control − base control, paired (SE,
seeds up):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Big-5 Top 10 share: base / saturation / history | 31.7 / 29.6 / 34.2 | 31.8 / **25.0** / 20.6 | 29.2 / **24.5** / 21.2 | 36.1 / **28.3** / 26.0 | 30.9 / **25.4** / 18.7 |
| Δ Big-5 Top 10 share | −2.1 (1.8) | −6.9 (4.3) | −4.7 (2.6) | −7.8 (3.6) | −5.5 (1.9) |
| Δ Top 10 CR8 | **−5.2 (2.4), 0/5** | −6.2 (2.7) | −2.2 (3.0) | −5.0 (5.2) | −1.4 (2.5) |
| Big-5 firms in the top four | +0.6 | −0.6 | −0.6 | −0.4 | −1.0 |

- **1961-64 move toward history in every year.** Big-5 Top 10 excess falls from +10.4 to +4.2 points. The "Big-5
  in the top four" count also falls, the guard §17o failed on.
- **1960 fails the stop rule.** Saturation Top 10 CR8 is 42.9 against the 48 floor, with the base control at 48.0.
  Top 10 CR4 is 28.5 against 30. The same switch passed 1960 on V1 (§17o, CR8 49.9). Here it stacks on
  `--radio-push-spread=0.5`, and like §17ac's A40 it cuts the year history leads with the majors.
- **Year-end genre error** (14 genres, per seed):
  - Saturation alone: +0.2 (SE 2.2).
  - Perceived under saturation: +2.8 (SE 1.7, 4/5) against its control.
  - v4 perceived on the base: +1.4 (SE 2.2).
  - So 2A carries a small genre cost in both conditions, not resolved at five seeds.
- Small closures and Top 40 by tier do not move outside noise.

**Verdict.**

- **The conversion diagnosis holds.** Trimming the launch reads of label push removes 2A's Top 40 head effect
  without touching the material gain. The remaining Top 10 edge sits in post-entry push, as §17aa found.
- **2A stays off.** It can be adopted only together with a conversion lever that passes the guards, and
  saturation 0.5 on these defaults does not: 1960 Top 10 CR8 fails by 5 points in all five seeds.
- **Debut guard:** 2A adds +18 to +23 Hot 100 debuts a year under saturation (5/5), against a control already
  about 42 over the ~690 guard (732 a year).

**Next (probe candidates, not run):**

- *Lighter saturation* (F = 0.75): about half the launch trim. Read whether 1960 CR8 clears 48 and 1961-64 keep
  most of the move.
- *Saturation from 1961.* The 1959-60 payola hearings are a historical reason for label push to buy less airplay
  after 1960. That needs its evidence written down first, so it is not tuning to a year.
- The post-entry Big-5 Top 10 edge is the §17aa weekly-push question. It stays with the label-tier research arc.

Evidence: `SimLogs/rj2als-{ctl,per,fit}-{1001,...,5005}-260-*`. Scripts `ls_vs_base.py` and `rj2a_yearly_g.py`
and their outputs are in the session notes folder.

### 2026-10-19: Screen result, F = 0.75 (`rj2als75-*`): the saturation control passes; 2A keeps a Top 10 edge

**Runs.** The same 15-run design as `rj2als-*`, with `--launch-push-saturation=0.75`: a quarter of a launch
campaign's excess over the 0.55 knee is trimmed, against half at F = 0.5. All 15 completed cleanly.

**1. Saturation control against the base control** (paired, SE, seeds up):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Top 10 CR8: base / F 0.75 (band) | 48.0 / **47.9** (48-58) | 47.0 / 39.9 (38-48) | 43.3 / 42.1 | 48.0 / 40.2 | 43.0 / 38.0 |
| Δ Top 10 CR8 | −0.2 (2.6) | −7.2 (4.0) | −1.2 (2.5) | −7.8 (4.5) | −5.0 (3.3) |
| Top 10 CR4: F 0.75 (band) | 31.3 (30-40) | 25.1 (22-32) | 28.7 | 26.7 | 25.4 |
| Big-5 Top 10 share: base / F 0.75 / history | 31.7 / 32.1 / 34.2 | 31.8 / **24.5** / 20.6 | 29.2 / **25.5** / 21.2 | 36.1 / **24.3** / 26.0 | 30.9 / **21.3** / 18.7 |
| Δ Big-5 Top 10 share | +0.3 (2.8) | −7.4 (3.6), 0/5 | −3.7 (4.0) | −11.8 (2.5), 0/5 | −9.5 (4.3) |
| Δ Big-5 firms in the top four | +0.4 | −0.4 | −0.6 | −0.8 | −1.2 |

- **1960 clears.** CR8 sits at the floor with the base control (47.9 against 48.0), and the Big-5 lead is unchanged.
- **1961-64 keep the move.** Big-5 Top 10 excess over history falls from +10.4 (base) to **+2.3**; F = 0.5 gave
  +4.2.
  - The two factors are inside each other's noise after 1960, so the extra trim at 0.5 bought nothing but the
    1960 break.
  - 1963 is now 1.7 under history.
- **"Big-5 in the top four"** falls every year from 1961 (0/5 seeds up in 1962-64). That is §17o's failed guard.
- **Other guards:** 1964 CR8 is at the band floor (38.0). Year-end genre error is +0.3 (SE 1.6). Debuts,
  closures and major-family share all stay inside noise.
- **F = 0.75 passes every guard read here.** It is the first conversion lever on these defaults to move the 1961-64
  head to history without breaking 1960.

**2. 2A under F = 0.75.**

- **Material gain:** quality +0.029 (5/5), Hot 100 entry +2.7 pts, Top 40 +1.0 pt. The pool is used 93.7% of the
  time.
- **Head:**

| | Perceived − control | Perceived − fit |
|---|---|---|
| Major-family share | **−0.1** (SE 1.2, 2/5) | +1.9 (0.8, 4/5) |
| Top 40 Major / Independent | +3.5 (3.0, 2/5) / +2.4 (3.2, 4/5) | +2.3 (1.7) / +0.5 (1.5, mixed) |
| CR4 / CR8 (units) | +1.9 / +1.5 (3/5) | +0.8 / +0.9 |
| Top 10 CR4 | **+4.8** (2.1, 4/5) | +2.6 (0.9, 5/5) |
| Big-5 share of the Top 10 | **+5.7** (2.2, 4/5) | +2.0 (0.8, 4/5) |
| Hot 100 debuts / yr | +19.8 (5/5) | +18.2 |
| Year-end genre error | +1.3 (SE 1.3) | |

- **Against the pass conditions:**
  1. Independent Top 40 ≥ −2 with mixed signs: **pass** (+0.5 against fit, +2.4 against the control).
  2. Perceived − fit major-family share within 1 SE: **fail** (+1.9 at 2.5 SE). Against the control it is −0.1;
     the fit arm sits −2.0 under its control here (+1.7, +0.8, −1.3, −2.0 across the four screens), so this
     contrast carries fit's own reshuffle.
  3. Top 10 CR4 inside the stop rule in 1961-63: **fail by a hair**. 1963 is 34.2 against a ceiling of 32, a 2.2
     miss with the control inside.
- **The residual is a Top 10 edge, not a Top 40 one.** The Top 10 rows are the only head rows that hold across
  conditions. Perceived − control Big-5 Top 10 share was +2.9 on the v4 base, +2.4 at F = 0.5 and +5.7 at F = 0.75.
  That pools to about +3.5.
- Saturation trims only the launch reads. Above the Top 40, conversion is post-entry: the weekly label push in the
  radio target (§17aa). That is where perceived selection's better Big-5 records turn into extra Top 10 weeks.

**Verdict.**

- **Saturation F = 0.75 is the candidate conversion lever.** It passes the 1960 floor, moves 1961-64 to within
  +2.3 of history, and fixes the top-four count. It needs its own confirmation as a default (decade read, #1
  tenure, turnover) under the label-tier research arc. That is not part of 2A.
- **2A stays off.** Under F = 0.75 its Top 40 head effect is gone. A Big-5 Top 10 edge of about +3.5 points
  remains, which takes 1963 just past the stop rule. Adopting 2A needs the post-entry channel addressed, or an
  author call that +3.5 Big-5 Top 10 points is acceptable on a base now 2.3 over history.
- 2A's other open items stand: +18 to +23 Hot 100 debuts a year, the source-mix decade read, and the read-level
  bias.

Evidence: `SimLogs/rj2als75-{ctl,per,fit}-{1001,...,5005}-260-*`; `ls_vs_base_g.py`, `screen_ls75.txt` and
`yearly_ls75.txt` are in the session notes folder.

### 2026-10-19: The post-entry push, diagnosed and probed (`rj2asp25-*`: F = 0.75 + `--radio-push-spread=0.25`)

**Diagnosis (F = 0.75 arms).** Big-5 Top 10 slot-weeks a year split into entrants × P(Top 10 | entry) × Top 10
weeks per Top 10 record. Perceived − control is +29.6 (SE 11.5), of which:

- entrants +1.5, conversion +17.9, stay +9.8, so almost all of it comes after entry;
- Big-5 entrant quality is +0.017.

In the control at matched entry quality (records quality 0.775-0.836, five seeds):

| | Big-5 | MidTier | Independent |
|---|---|---|---|
| P(Top 10 \| entry), new signings (recognition ≈ 0) | **35%** | 20% | **11%** |
| P(Top 10 \| entry), established acts | 53% | 36% | 18% |
| Radio heat, chart weeks 1-6, new signings | 0.45-0.47 | 0.38-0.41 | 0.35-0.37 |

The premium holds for acts with no recognition, so it belongs to the label, not the act. The radio target's label
term is `0.4 × push × weekFactor`, where push is campaign impact after `--radio-push-spread=0.5` (Big-5 about 0.62,
Independent 0.41). At a week factor of 0.85-1 that is about 0.07-0.085 of the 0.12 heat gap. The rest is the
position bonus that sales earn, which compounds the same push.

**Probe.** No build: the existing `--radio-push-spread` reads exactly that weekly push. At 0.25, Big-5 push is about
0.55 and Independent about 0.45, against 0.62 / 0.41 at 0.5. Launch reads stay under saturation F = 0.75. Fifteen
runs, all completed cleanly.

**1. The control.**

- **1960 passes.** Top 10 CR8 is 47.1 against the base's 48.0 and F = 0.75's 47.9. That is paired −0.8 (seeds
  −8.2 / +6.0 / +0.9 / +2.1 / −4.8), 0.9 under the floor.
  - Big-5 1960 share is 33.7 (history 34.2).
- **1961-64:**

  | | 1961 | 1962 | 1963 | 1964 |
  |---|---|---|---|---|
  | Big-5 Top 10 share | 27.4 | 23.1 | 29.0 | 26.1 |
  | Paired against the base | −4.4 | −6.2 | −7.1 | −4.8 |
  | History | 20.6 | 21.2 | 26.0 | 18.7 |

  - That is +4.8 over history, against +2.3 for F = 0.75 alone. The two are inside each other's noise
    (SE 4-6), so spread 0.25 does not cut the control's head further.
- Top 10 CR4 / CR8 sit in band 1961-64.
- Top 10 turnover (96.5 records a year, 5.39 weeks each) and #1 tenure (2.27) are unchanged.
- Genre error is +0.9 (SE 2.8).

**2. 2A under it.**

- **Material gain:** quality +0.030 (5/5), entry +2.8 pts, Top 40 +1.2 pts.
- **The post-entry part of 2A's Big-5 Top 10 gain is cut.** It is a fifth to a half of the other bases', depending
  on the base:

| Perceived − control, Big-5 Top 10 slot-weeks a year | Total (SE) | Entrants | Conversion | Stay |
|---|---|---|---|---|
| v4 base (spread 0.5, no saturation) | +15.0 (14.9) | +2.7 | +8.8 | +3.8 |
| F = 0.5 | +12.2 (21.6) | −0.8 | +8.2 | +5.4 |
| F = 0.75 | +29.6 (11.5) | +1.5 | +17.9 | +9.8 |
| **F = 0.75 + spread 0.25** | **+13.7 (3.7)** | **+8.4** | **+4.0** | **+1.2** |

- **The total edge does not shrink.** F = 0.75's +29.6 was the high draw. Spread 0.25 moves the edge from
  post-entry to entry: Big-5 entrants +6.4 (SE 1.8), the weekly push before entry.
- It also tightens the edge a lot (SE 3.7 against 11.5-21.6). It is +2.6 Big-5 Top 10 points (SE 0.7, 4/5).

| | Perceived − control | Perceived − fit |
|---|---|---|
| Major-family share | **−0.8** (SE 1.2, 2/5) | +1.0 (0.9, 3/5) |
| Top 40 Major / Independent / MidTier | +5.8 (5/5) / +6.0 (3/5) / −7.8 (1/5) | +3.2 / −1.6 (mixed) / −1.3 |
| Top 10 CR4 | +1.8 (0.6, 4/5) | +2.3 (1.1) |
| Big-5 share of the Top 10 | **+2.6** (0.7, 4/5) | +4.7 (1.4, 5/5) |
| Hot 100 debuts / yr | +23.8 (5/5) | +21.9 |
| Year-end genre error | **+1.8** (SE 0.6, 5/5) | |

**Against the pass conditions:**

1. Independent Top 40 ≥ −2 with mixed signs: **pass** (−1.6 against fit, mixed; +6.0 against the control).
2. Perceived − fit major-family share within 1 SE: **borderline**, +1.0 at 1.2 SE. Against the control it is −0.8.
   The fit arm again sits under its control (−1.8).
3. Top 10 CR4 inside the stop rule in 1961-63: **pass**. 1962 is 33.6, a 1.6 miss, under the 2-point margin.

**Flag:** perceived 1960 Top 10 CR8 is 44.4, 3.6 under the floor, while the control misses by 0.9. That trips the
stop rule in the *low* direction. At F = 0.75 the same arm was +2.65 in 1960, so this looks like seed noise, but it
is a rule hit.

**Verdict.**

- **Spread 0.25 does what was asked of it.** It removes most of the post-entry push behind 2A's Big-5 Top 10 gain
  (conversion and stay +5.2 slot-weeks, against +12.6 to +27.7 elsewhere). It costs 1960 nothing and moves no
  turnover, tenure or genre guard.
- **What remains of 2A's head effect is small and entry-side:** about +2.6 Big-5 Top 10 points. Major-family share
  does not rise against the control, and Independents gain Top 40 entries. MidTier pays (−7.8).
- **2A's own costs are now resolved at five seeds:**
  - genre error +1.8 (SE 0.6, 5/5);
  - Hot 100 debuts +24 a year (5/5) on a control already about 48 over the ~690 guard (738 a year);
  - the professional-for-artist-written source shift.
- **2A is not adopted here.** It stays off by default. Adoption is now an author decision on those three costs and on
  the +2.6 point edge, on a lever base (F = 0.75 + spread 0.25) that needs its own decade confirmation under the
  label-tier arc.

Evidence: `SimLogs/rj2asp25-{ctl,per,fit}-{1001,...,5005}-260-*`. `t10_funnel_g.py`, `t10_career.py`,
`screen_sp25.txt` and `yearly_sp25.txt` are in the session notes folder.

### 2026-10-19: The base's own Big-5 excess: weekly push is exhausted; 1964 is a genre problem (controls only)

The author asked to keep pushing the base's Big-5 Top 10 share toward history before any decade run. Two pieces
of work went in: an attribution re-run of the spread-0.25 control (`rj2asp25conv-ctl-1001..4004`, byte-identical
to `rj2asp25-ctl`), and control-only screens at spread 0.1 and 0.0 (`rj2asp10-ctl-*`, `rj2asp00-ctl-*`, both at
F = 0.75).

**1. Where the remaining excess sits (spread 0.25, 1961-64).**

- **Big-5 convert an entry 1.92× as often as other labels; history's hit acts are 1.00 ± 0.18 (§17aa).**
  - Established acts: 25.9% against 17.0%. New signings: 14.8% against 9.1%.
  - Established acts hold three-quarters of Big-5 Top 10 weeks.
  - At the field's rate by seniority, Big-5 share would be about 16.5%, below history's 21.6. So the target is
    about half the premium, not all of it.
- **Logit of P(Top 10 | entry)** on 11,555 entrants (pseudo R² 0.50):
  - Big-5 entrants are *lower* quality (0.711 against 0.743), so quality widens the gap.
  - Moving campaign impact to the field mean closes 103% of it. With impact in the model, a Big-5 dummy adds
    nothing (z 0.3).
  - Entry-week radio heat (0.455 against 0.363) plus entry units close 100%. Big-5 records enter on fewer units
    (2.7k against 3.3k) and more airplay, so the premium is in place at entry.

**2. Pushing the weekly read further does nothing for 1961-64 and breaks 1960 at zero.**

| Control at F = 0.75 | 1960 Top 10 CR8 (band 48-58) | Big-5 Top 10 share, 1961 / 62 / 63 / 64 | 1961-64 mean | Conversion ratio |
|---|---|---|---|---|
| Base (no saturation, spread 0.5) | 48.0 | 31.8 / 29.2 / 36.1 / 30.9 | 32.0 | 2.24 |
| Spread 0.5 | 47.9 | 24.5 / 25.5 / 24.3 / 21.3 | 23.9 | |
| Spread 0.25 | 47.1 | 27.4 / 23.1 / 29.0 / 26.1 | 26.4 | 1.92 |
| Spread 0.1 | **48.2** | 24.3 / 25.7 / 26.9 / 27.0 | 26.0 | 1.96 |
| Spread 0.0 (label-blind) | **42.1** (0/5 seeds up) | 28.2 / 26.4 / 28.8 / 19.7 | 25.8 | 1.81 |
| History | 34.2 Big-5 share | 20.6 / 21.2 / 26.0 / 18.7 | 21.6 | 1.00 |

- **The base's head is set by saturation, not spread.** Once saturation is on, every spread from 0.5 to 0.0 lands
  within seed noise in 1961-64 (23.9-26.4). Spread changes *where* 2A's increment lands (§ above), not the base's
  level.
- Spread 0.0 breaks 1960 the way saturation F = 0.5 did.
- **Why weekly push stops mattering:** the premium is set at launch. Launch radio is `0.3 × saturated raw
  impact`, which still differs by about 0.11 at F = 0.75. A heat above its weekly target decays only at
  `lerpRate 0.10` for 12 weeks, so the launch push carries past entry.
- **1960 cannot be spared by seniority.** NewSigning launches hold 30% of 1960's Big-5 Top 10 weeks, more than any
  later year (21-27%), so a newcomer-only lever hits 1960 first.

**3. The year shape at F = 0.75 + spread 0.1.**

- 1960 and 1963 sit on history (33.8 / 34.2 and 26.9 / 26.0). 1961-62 are +3.7 / +4.5, about 1-2 SE.
- **1964 is +8.3, and it is genre, not push.**
  - Year-end slots run TraditionalPop 18.8 (bench 14) and TeenPop 13.8 (bench 5), against Soul 8.8 (14) and
    RockAndRoll 4.2 (8). The Big-5 hold 44% of TraditionalPop's 1964 Top 10 weeks.
  - British Beat supply is there: 21.8 slots against a bench of 24, 8.6% of it Big-5.
  - The 1964 excess is the decline of the adult/teen-pop genres running late. Big-5 hold those genres, so it shows
    up as Big-5 share.

**Status and options.**

- Best base so far: **F = 0.75 + spread 0.1.** 1960 is in band, 1961-64 Big-5 is +4.4 over history (1961-63
  +3.0), turnover and #1 tenure are unchanged, and genre error is neutral.
- Further cuts to label push now cost 1960 before they help 1961-64. History has a 13.6-point Big-5 drop from 1960
  to 1961, which a constant-strength push cannot make. Three directions remain:
  1. **Genre decline in 1964** (TraditionalPop / TeenPop late, Soul / RockAndRoll short). This goes in the genre
     calibration arc. It takes the largest single year without touching label push.
  2. **Launch-heat persistence.** A probe making a launch-seeded heat fall to its weekly target at the normal rate
     (0.28 rather than 0.10). Label push would then buy launch weeks, not a quarter of airplay. It is likely to touch
     1960 too.
  3. **Time-varying push effectiveness from 1961** (the 1959-60 payola hearings and the 1960 Communications Act
     amendment). This is a historical modelling decision for the author, and its evidence must be written down
     first.

Evidence: `SimLogs/rj2asp25conv-ctl-*`, `rj2asp10-ctl-*`, `rj2asp00-ctl-*`. `conv_after_entry.py`,
`big5_excess.py`, `big5_by_year_seniority.py` and `watch1960_multi.py` are in the session notes folder.

### 2026-10-19: Why 1964 runs TraditionalPop/TeenPop long and Soul/RockAndRoll short (data only, no runs)

All of this comes from the existing F = 0.75 + spread 0.1 controls (`rj2asp10-ctl-*`, five seeds). No new runs.

**1. Each genre misses through a different channel.** These are 1964 means, model against history. Share is
against `AdjustedHistoricalGenreShareTargets.csv`; slots are against the §3 hand count.

| 1964 | Market share | Year-end slots | Which channel |
|---|---|---|---|
| RockAndRoll | 3.6 / 6.8 (×0.53) | 4.2 / 8 (×0.53) | Slots per share point is on history, so the whole miss is per-record demand |
| TeenPop | 10.6 / 7.9 (×1.34) | 13.8 / 5 (×2.76) | Mostly conversion; the keyframe also *rises* to .46 in 1964 while the target falls |
| TraditionalPop | 12.6 / 11.0 (×1.15) | 18.8 / 14 (×1.34) | A little of both; Big-5 hold 33% of its chart weeks and enter at 66% against 17% |
| Soul | 9.9 / 12.2 (×0.81) | 8.8 / 14 (×0.63) | Both; entry-week radio heat is the lowest of any genre (0.32) |

- By family in 1964: **Pop is +4.0 share points but +18.6 slots.** Rock is −6.1 / −11.4 and Rhythm & Soul
  −4.0 / −3.6.
- TeenPop and Soul together are on target in 1963 (share 21.7 against 22.3, slots 23.4 against 23). The model
  splits that pair wrongly.
  - A hypothesis to check, not a finding: the hand count may file 1963-64 girl-group records as Soul, while the
    model maps `GirlGroup` → TeenPop.

**2. RockAndRoll: low hooks from the source mix, not demand or the act pool.**

- R&R ships as many singles as TeenPop (499 against 509 in 1964), but only 4.7% enter the Hot 100, against 26.5%.
- **At matched hook, R&R enters as well as the youth genres:**

  | P(entry), singles 1962-64 | Hook .55-.65 | .65-.75 | .75-.85 |
  |---|---|---|---|
  | RockAndRoll | 0.13 | 0.32 | 0.58 |
  | TeenPop | 0.16 | 0.32 | 0.50 |
  | BritishBeat | 0.22 | 0.34 | 0.52 |
  | Soul | 0.02 | 0.08 | 0.20 |
  | TraditionalPop | 0.04 | 0.11 | 0.23 |

  - Given the other genres' hook distribution at its own per-bin rates, R&R would enter at 0.33 instead of 0.057.
- **R&R's mean hook is 0.44 in every year from 1960 to 1964.** Soul is 0.74 and TradPop 0.70-0.75.
  - The gap is the same across every tier, career state and act cohort.
  - It is not the acts: songwritingAbility is 0.52 for R&R, 0.53 for Soul and 0.53 for TradPop.
  - Production goes the other way (R&R 0.65, Soul 0.56), so polar execution loss is not the cause.
- **The cause is the source-mix prior combined with the artist-written hook mapping:**
  - Rock-family singles draw about 65% artist-written, 28% recent-hit covers and 7% professional material. Polar
    zeroes rock standards and traditional songs (`rockStandardFactor` .02). Soul draws about 40% professional,
    and TradPop and TeenPop draw mostly standards and staff material.
  - The artist-written hook is `0.75 × (0.8 × craft + 0.1 × era + noise) + 0.15 × performance hook (+ 0.2 × team)`
    (`SongMaterialSelectionService.BuildArtistWritten`). A median writer (craft ≈ 0.6) lands near 0.45, and even a
    craft-1.0 writer only reaches about 0.80.
  - Professional songs are minted at N(0.68, 0.15) whoever records them, plus label polish.
  - So self-written material starts about 0.2 below staff material, and the genres that write their own songs
    inherit the gap.
  - In-game hits become cover material at their *realised* hook (`commercialHook = record.hookStrength`), so low
    rock hooks also fill rock's recent-hit cover pool.
  - SurfRock (0.53) and BritishBeat (0.57) carry the same handicap.
- The flat 0.44 sets R&R's *level*. Its 1961-64 slide (share ×0.74 → ×0.53 of target) comes through falling
  release counts (930 → 645) and is a separate supply question.

**3. Soul and TradPop: high hooks hide weak conversion.**

- At the same hook they enter at about half the youth-genre rate (table above).
- For Soul this is the flat `RadioAcceptance` 0.55, which was sized against a late-decade Soul over-charge and
  therefore also strips 1963-64.
- TradPop's high hooks (standards and staff writers) and its Big-5 concentration carry its slots.
- The per-genre hook skew and the per-genre chart levers (keyframes, RA) have been calibrated *against each other*.

**4. TeenPop.**

- At matched hook TeenPop converts like rock. Its excess is supply plus a keyframe that rises in 1964 (.42 → .46)
  while history collapses (share 10.8 → 7.9, slots 8 → 5).
- History's TeenPop chart efficiency halves from 1962 to 1963 (slots per share point 1.63 → 0.74). The model's
  holds at 1.3.

**Implication: recalibrate artist-written hooks (author: "Lennon/McCartney were not writing 0.45 melodies").**

- This is one change and it moves 1964 the right way on every row: R&R, SurfRock and Soul up, TeenPop and TradPop
  relatively diluted.
- It also removes a structural bias against the self-writing genres, which matters most after 1964.
- Risks to screen:
  1. 1960-61 R&R is near target today (12.6 / 14, 12.0 / 12) only because high per-hook conversion offsets low
     hooks. Rock keyframes and the R&R RA 1.10 were tuned against the low hooks.
  2. BritishBeat (21.8 / 24) has little headroom.
  3. Shipped hook drives units, so this is a market-share change as well as a chart change, and it will move
     genre error.
- Proposed shape: give artist-written material the same hook *scale* as professional material at equal craft,
  rather than a lower one. A median craft writer should sit somewhat below the staff median, and a top-decile
  writer should reach or pass it.
- Read first: the 1960-64 genre share and slot tables, R&R 1960-61, BritishBeat 1964, and the Big-5 rows above.

Evidence: `SimLogs/rj2asp10-ctl-{1001,...,5005}-260-*`. `g64_shape.py`, `g64_family.py`, `g64_funnel.py`,
`g64_singles.py`, `g64_material.py`, `g64_creativity.py` and `g64_hookbins.py` are in the session notes folder.

### 2026-10-19: Screen result, `--artist-written-hook-scale` (`awhs-on-*` against `rj2asp10-ctl-*`)

**The change** (`Systems/ArtistWrittenHookScale.cs`, off unless named).

- An artist-original's composition hook becomes `0.85 × (writer craft + noise) + 0.15 × performance hook + team lift`.
  That is the same 0-1 scale catalogue songs are minted on (professional mean 0.68).
- **No genre term.** The genre-weighted era factor leaves the hook. It stays in craft score, lyric, identity fit
  and the source-mix preference.
- So a genre's hook level now comes only from **how talented its writers are** and **how often it self-writes**.
- Writer craft is genre-neutral (median 0.51-0.59 in every genre, p90 0.77-0.87).
- It applies to both the live builder (`BuildArtistWritten`) and the Release Judgement shadow candidate.
- Pure arithmetic, no draws.
- A flag-off rerun (`awhs-off-1001`) is **byte-identical to `rj2asp10-ctl-1001` (84/84 CSVs)**.

**Hooks (1960-64, five seeds, paired):**

| Genre | Median, control → flag | p99 | Singles ≥ 0.80 |
|---|---|---|---|
| RockAndRoll | 0.43 → 0.54 | 0.79 → 0.94 | 0.9% → 7.3% |
| SurfRock | 0.52 → 0.64 | 0.91 → 0.95 | 4.9% → 14.3% |
| BritishBeat | 0.56 → 0.66 | 0.94 → 0.95 | 11.9% → 18.2% |
| Soul | 0.76 → 0.78 | 0.97 | 36.6% → 43.1% |
| TraditionalPop | 0.74 → 0.75 | 0.97 | 32.6% → 35.9% |

- Top rock writers now reach staff-grade hooks.
- Rock's average stays below Soul's and TradPop's, because those genres still draw more professional material.

**Genres** (flag against control, five seeds, with history in brackets):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| R&R share | 10.0 → **13.4** [13.5] | 7.5 → **11.6** [12.8] | 6.0 → **9.8** [11.5] | 5.1 → **8.6** [10.0] | 3.6 → **7.0** [6.8] |
| R&R slots | 12.6 → 15.0 [14] | 12.0 → 15.2 [12] | 7.8 → 12.0 [12] | 9.4 → 16.0 [11] | 4.2 → 10.8 [8] |
| TeenPop share | 19.1 → 17.0 [13.4] | 16.0 → 13.9 [12.5] | 14.2 → 12.8 [12.3] | 11.9 → 10.0 [10.8] | 10.6 → 8.4 [7.9] |
| TeenPop slots | 32.0 → 32.0 [28] | 26.2 → 24.8 [18] | 27.6 → 27.6 [20] | 15.0 → 14.4 [8] | 13.8 → 10.8 [5] |
| TradPop share | 14.2 → 13.0 [15.0] | 14.2 → 13.1 [15.0] | 13.7 → 12.6 [14.5] | 13.9 → 12.0 [13.2] | 12.6 → 10.6 [11.0] |
| TradPop slots | 23.0 → 18.8 [22] | 18.6 → 18.6 [16] | 18.0 → 15.2 [15] | 25.4 → 23.0 [20] | 18.8 → 16.8 [14] |
| Soul share | 7.1 → 6.4 [7.9] | 8.8 → 8.3 [8.9] | 9.4 → 8.8 [10.2] | 9.8 → 8.9 [11.5] | 9.9 → 10.3 [12.2] |
| Soul slots | 2.8 → 1.6 [6] | 5.4 → 5.0 [9] | 4.4 → 5.4 [7] | 8.4 → 7.2 [15] | 8.8 → 10.4 [14] |
| Sum abs share error | 25.3 → 24.3 | 28.1 → **22.8** (0/5 up) | 28.5 → **22.1** (0/5) | 28.3 → 27.1 | 31.6 → **28.8** (0/5) |
| Sum abs slot error | 28.8 → 32.8 | 35.0 → 36.2 | 40.8 → 38.4 | 45.4 → 49.4 | 47.4 → **37.4** (0/5) |

- **R&R market share lands on history in every year.** It was ×0.53-0.74, and every seed moves up (SE ≈ 0.5).
  - The market table is the benchmark that sets demand (§2 rule), so this is the headline. Genre share error
    falls in every year.
- **R&R now over-charts at a correct share:** +1 to +5 slots in 1960-61 and 1963-64.
  - That is the "market on target, chart over" pattern `RadioAcceptance[RockAndRoll] = 1.10` was left to tolerate.
    That RA, and the 1960 keyframe history in `GenreCatalog.cs`, were set against the low hooks. A chart-side
    retune is owed (RA, not the keyframe).
- **TeenPop and TradPop shares fall by 1-2 points.**
  - TeenPop moves toward history. 1964 slots go 13.8 → 10.8, still about double.
  - TradPop's 1960-62 share is now 1.4-2.0 under.
- **Soul is barely moved**, as predicted (§3 above: its channel is RA 0.55, not hooks).
- **BritishBeat 1964:** slots 21.8 → 19.0 (SE 2.6, 2/5 up) against 24. Share is unchanged at 4.9 against 8.5.
  Within noise, but the wrong side.
- **SurfRock** share goes over in 1963-64 (4.4 / 5.2 against 3.4 / 2.5). Slots 9.4 are on 9.

**Head:**

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Big-5 Top 10 share, control → flag [history] | 33.8 → 28.1 [34.2] | 24.3 → 25.2 [20.6] | 25.7 → 23.3 [21.2] | 26.9 → 24.4 [26.0] | 27.0 → **18.8** [18.7] |
| Top 10 CR8 (1960 band 48-58) | 48.2 → **44.3** (SE 4.5, 2/5 up) | 38.5 → 41.6 | 39.7 | 39.1 | 42.1 |
| Hot 100 debuts / yr | −2.8 | +20.4 | +23.2 | +8.0 | −6.8 |

- **The 1964 Big-5 excess is gone** (−8.2, 0/5 up, landing on history). That confirms it was genre, not push.
- **1960 breaks the CR8 floor** (44.3 against 48). Self-writing R&R independents take 1960 Top 10 weeks from the
  Big-5's TradPop. The SE is wide, but it is a stop-rule hit.

**Verdict.**

- The recalibration is right on its own terms. It is genre-neutral by construction and puts R&R's market on history
  without any genre term. It cuts share error in every year, takes 1964 slot error −10, and removes the 1964 Big-5
  excess.
- **It stays off by default for now.** Two compensations that were tuned against the old low hooks have to come
  out with it:
  1. R&R's chart conversion (RA 1.10), which now over-charts at a correct share;
  2. the 1960 head (CR8 floor), probably together with TradPop's 1960-62 share, which is now 1.4-2.0 under.
     A TradPop 1960-62 keyframe nudge of about sqrt(15/13) ≈ ×1.07 is the transfer-law size.
- Then a two-seed decade A/B, because self-writing grows to 76% of rock by 1969, so the late decade is where this
  matters most.
- Soul's RA 0.55 and TeenPop's rising 1964 keyframe are separate and untouched here.

Evidence: `SimLogs/awhs-on-{1001,...,5005}-260-*` and `awhs-off-1001-260-*`. `awhs_screen.py`,
`awhs_screen5.txt`, `hashcmp.py` and `launch_awhs.py` are in the session notes folder.

### 2026-10-19: Retune screen on the hook-scale base (R&R RadioAcceptance, TradPop 1960-62 keyframes)

**Probes** (`Data/GenreCatalog.cs`, inert unless named):

- `--radio-acceptance=Genre=value,...` replaces a genre's RadioAcceptance multiplier.
- `--genre-keyframe-scale=Genre@year=factor,...` scales one baseline keyframe.
- A rerun of `awhs-on-1001` with these compiled in is **byte-identical (84/84)**.

**Sizing (offline):**

- TradPop's 1960-62 share runs at 0.87 of target on the hook base, so the keyframe scale is √(1/0.87) ≈ ×1.07.
- R&R needs about ×0.83 of its slots at a correct share. Taking airplay as acceptance⁵ and 45% of points sized
  RA at 1.10 → 1.00.

**Arms, five seeds each, all with `--artist-written-hook-scale`:**

- **+TP:** TradPop @1960/@1962 ×1.07 (`awtp-on`).
- **+RA:** R&R RA 1.00 (`awra-on`).
- **+RA+TP:** both (`awratp-on`).

**Results.** Cells read control | hook | +TP | +RA | +RA+TP [history].

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| R&R share | 10.0 \| 13.4 / 13.0 / 13.3 / 14.0 [13.5] | 7.5 \| 11.6 / 11.0 / 11.2 / 11.6 [12.8] | 6.0 \| 9.8 / 10.1 / 9.5 / 9.3 [11.5] | 5.1 \| 8.6 / 8.9 / 8.4 / 8.7 [10.0] | 3.6 \| 7.0 / 7.2 / 6.4 / 6.7 [6.8] |
| R&R slots | 12.6 \| 15.0 / 14.8 / 16.0 / 17.2 [14] | 12.0 \| 15.2 / 16.2 / 16.2 / 15.6 [12] | 7.8 \| 12.0 / 14.6 / 13.6 / 13.8 [12] | 9.4 \| 16.0 / 15.2 / 15.0 / 15.4 [11] | 4.2 \| 10.8 / 10.4 / 10.0 / 10.4 [8] |
| TradPop share | 14.2 \| 13.0 / **14.0** / 12.8 / 14.0 [15.0] | 14.2 \| 13.1 / **14.6** / 13.0 / 14.0 [15.0] | 13.7 \| 12.6 / **13.7** / 12.8 / 13.4 [14.5] | 13.9 \| 12.0 / **12.7** / 12.5 / 12.8 [13.2] | 12.6 \| 10.6 / **11.1** / 10.9 / 11.0 [11.0] |
| TradPop slots | 23.0 \| 18.8 / 21.0 / 18.6 / 20.2 [22] | 18.6 \| 18.6 / 18.0 / 19.2 / 20.4 [16] | 18.0 \| 15.2 / 14.8 / 15.8 / 16.2 [15] | 25.4 \| 23.0 / 23.2 / 24.6 / 23.2 [20] | 18.8 \| 16.8 / 16.4 / 17.2 / 16.0 [14] |
| Soul share | 7.1 \| 6.3 / 6.8 / 6.7 / 6.5 [7.9] | 8.8 \| 8.3 / 8.0 / 7.6 / 7.4 [8.9] | 9.4 \| 8.8 / 7.9 / 7.6 / 8.4 [10.2] | 9.8 \| 8.9 / 9.3 / 8.4 / 8.8 [11.5] | 9.9 \| 10.3 / 10.1 / **8.8** / 9.0 [12.2] |
| Soul slots | 2.8 \| 1.6 / 3.0 / 2.4 / 1.6 [6] | 5.4 \| 5.0 / 5.6 / 4.6 / 3.0 [9] | 4.4 \| 5.4 / 3.4 / 3.6 / 4.4 [7] | 8.4 \| 7.2 / 8.6 / 6.6 / 7.2 [15] | 8.8 \| 10.4 / 9.4 / **5.8** / 7.6 [14] |
| BritishBeat slots | | | | | 21.8 \| 19.0 / 16.2 / 20.0 / 22.4 [24] |
| Sum abs share error | 25.3 \| 24.3 / **22.4** / 22.5 / 21.5 | 28.1 \| 22.8 / **21.8** / 23.9 / 21.9 | 28.5 \| 22.1 / 23.2 / 24.6 / 23.7 | 28.3 \| 27.1 / **23.6** / 24.2 / 25.8 | 31.6 \| 28.8 / **27.8** / 29.4 / 28.7 |
| Sum abs slot error | 28.8 \| 32.8 / 30.8 / 31.2 / 32.0 | 35.0 \| 36.2 / 33.8 / 33.8 / 39.4 | 40.8 \| 38.4 / 38.8 / 38.8 / 37.6 | 45.4 \| 49.4 / 44.6 / 49.4 / 44.6 | 47.4 \| 37.4 / 44.6 / 42.6 / 33.8 |
| Big-5 Top 10 share | 33.8 \| 28.1 / 29.9 / 31.3 / 29.7 [34.2] | 24.3 \| 25.2 / 28.4 / 24.9 / 27.2 [20.6] | 25.6 \| 23.3 / 23.0 / 26.4 / 24.2 [21.2] | 26.9 \| 24.4 / 21.2 / 24.5 / 24.9 [26.0] | 27.0 \| 18.8 / 21.0 / 22.4 / 21.4 [18.7] |
| Top 10 CR8 (1960 floor 48) | 48.1 \| 44.3 / **44.0** / **48.8** / 47.5 | 38.5 \| 41.6 / 44.4 / 38.5 / 42.7 | 38.4 \| 39.6 / 36.8 / 41.9 / 40.8 | 40.4 \| 39.1 / 36.9 / 39.1 / 39.4 | 40.8 \| 42.1 / 36.2 / 38.6 / 38.6 |

**R&R RadioAcceptance 1.00 is rejected.**

- It does reach the records: R&R on-chart radio heat falls 0.435 → 0.410 and no other genre's moves.
- It does not move R&R's chart (slots +1.0 against the hook base, share −0.2). In 1960-64 chart points are
  mostly sales.
- It drains Soul through the shared Urban/R&B segment: 1964 Soul share −1.5 (SE 0.5, 0/5 up), slots −4.6
  (SE 1.1, 0/5). R&B gains +1.7 / +1.0 in 1962-63.
- Its one gain is the 1960 head (CR8 48.8): cutting airplay to indie R&R hands 1960 Top 10 weeks back.
- R&R's over-charge at a correct share is sales-side. It comes from higher hooks concentrating units into more
  hit records, and RA cannot reach it.

**TradPop ×1.07 at 1960/1962 is kept as part of the candidate (+TP).**

- TradPop share sits within 1.0 point of history in every year (14.0 / 14.6 / 13.7 / 12.7 / 11.1).
- Share error is the lowest of the arms in 1960, 1961, 1963 and 1964.
- 1960 TradPop slots recover (21.0 against 22).
- It does **not** restore the 1960 head (CR8 44.0, Big-5 29.9 against 34.2).
- 1964 slot error is worse than hook alone (44.6 against 37.4): TeenPop is 13.4 and BritishBeat 16.2, both a
  seed-noise size away.

**Candidate base: hook scale + TP.** Still open:

1. The 1960 CR8 floor (44 against 48).
2. R&R slots over at a correct share (+1 to +4 in 1960-61 and 1963-64).
3. BritishBeat 1964 is short.
4. Soul stays short (its own RA 0.55 question).

The hook scale stays off by default.

### 2026-10-19: Genre hook audit (author: look for ahistorical balancing), data from `awhs-on-*`

Mean shipped hook on AI singles, 1960-64, under the hook scale:

| | Soul | BossaNova | TradPop | EasyListening | BritishPop | Jazz | Folk | Country | Gospel | BritishBeat | TeenPop | SurfRock | R&B | DooWop | R&R |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Mean hook | .761 | .749 | .735 | .718 | .707 | .676 | .673 | .660 | .660 | .655 | .639 | .631 | .624 | .560 | .533 |

Writer craft is genre-neutral, so this ordering is set by authored per-genre constants on the material path.
Ranked by how ahistorical they look:

1. **No professional office writes rock & roll, doo-wop, surf or beat.**
   - The five publishers (`GenerateProfessionalPool`) focus on TeenPop/GirlGroup/TradPop, Country/Folk, LA pop,
     Tin Pan Alley/Jazz, and Detroit Soul/Motown/R&B.
   - `GetProfessionalForGenre(RockAndRoll)` is therefore empty, and R&R's professional share (already
     `Rock` family .05, availability .28) is redistributed to self-writing and covers.
   - History runs the other way for 1956-63 rock & roll and vocal-group records:
     - Hill & Range and Leiber–Stoller for Elvis, the Coasters and the Drifters;
     - Pomus–Shuman and Otis Blackwell for rock & roll;
     - Aldon (Goffin–King, Mann–Weil, Sedaka–Greenfield) for Bobby Vee, Gene Pitney and the Drifters;
     - the Bryants (Acuff-Rose) for the Everlys.
   - This is the main reason R&R and DooWop sit at the bottom.
   - **Proposed:** a rock & roll / R&B hit-writer office (Hill & Range / Aldon / Trio style) with RockAndRoll,
     DooWop and RnB focus. Then revisit the rock-family 1960 professional anchor (.05) and R&R availability
     (.28), so R&R's pro share starts substantial and declines as self-writing takes over (anchor 1969 AW .76).
   - A hypothesis for the 1960 head: majors get the best professional access (`labelAccess` Major .90), so
     professional R&R material should move R&R hits toward RCA/Decca-style majors in 1960, the way Elvis and
     Holly charted. That is a historically grounded route back to the 1960 CR8 floor, which RA and keyframes
     did not give.
2. **Songbook quality constants are genre-coded.**
   - Standards: Tin Pan Alley .68, Jazz .66, Country .60, Blues .58, R&B Catalog .58, Gospel .56, Folk
     Traditional .50.
   - Pre-game recent hits: R&R .62, R&B / Pop / Teen .60, DooWop / Country .58.
   - These rank the Black-music and roots songbooks below Tin Pan Alley on *commercial* hook. That is a value
     judgement the commercial record does not obviously support: blues and gospel standards were heavily covered
     hit material. These are candidates to equalise, or to justify per family.
3. **The polar execution penalty lands unevenly by genre (mechanism unverified).**
   - Within Independent labels, production splits in two:
     - TeenPop, R&B, R&R and DooWop at 0.645-0.655 (about no execution loss);
     - Soul, Folk, BritishBeat, Blues, Gospel and Jazz at 0.55-0.59 (about 10-16% loss), with Country .61 and
       TradPop .59 between.
   - Execution multiplies hook too, so those genres' hooks are also taxed. Soul would be about 0.90 before it.
   - The split is step-shaped, not graded, which points to some material escaping the capability penalty rather
     than to act skill. Trace it before any per-genre hook tuning.
4. **Neutral, and kept:**
   - In-game hits become cover material at their realised hook. That self-reinforces, but it is genre-blind.
   - The era weight now affects only craft score, lyric, identity fit and source preference.

Evidence: `SimLogs/awtp-on-*`, `awra-on-*`, `awratp-on-*`, `awhs2-on-1001-260-*`. `awra_summary.py`,
`awtp_vs_ctl.txt`, `awra_vs_awhs.txt`, `launch_awra.py` and `launch_awtp.py` are in the session notes folder.

### 2026-10-19: Screen result, `--rock-hit-writers` on the hook-scale + TradPop base (`awrw-on-*` against `awtp-on-*`)

**The change** (`Systems/RockHitWriters.cs`, off unless named):

1. A generic NY-scale "Rock & Roll Hit Office" (90 titles), focused on RockAndRoll, DooWop and RnB, is appended
   after the five existing offices.
2. RockAndRoll gets its own source anchor in place of the rock-family default:
   - 1960: artist-written .30, professional .35, standards .05, hit covers .25, traditional .05;
   - 1969: .62 / .15 / .03 / .18 / .02.
   - Standards and traditional still carry the rock songbook factor .02.
3. R&R professional availability is 0.60 easing to 0.30 by 1968 (was 0.28 → 0.16). In the live path this is
   only an on/off gate.

A flag-off rerun (`awtp2-on-1001`) is **byte-identical to `awtp-on-1001` (84/84)**.

**Hooks (1960-64):**

- R&R 0.533 → **0.595** (5/5) and DooWop 0.559 → **0.612** (5/5). Nothing else moves by more than 0.02.
- The order is now R&R .60 < DooWop .61 < R&B .62 < TeenPop .65 < … < Soul .77. Self-writing still pulls rock's
  average below the staff-led genres.

**Head, the 1960 test** (cells read control | hook | +TP | +TP+RW):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Top 10 CR8 (floor 48 in 1960) | 48.1 \| 44.3 / 44.0 / **44.1** | 38.5 \| 41.6 / 44.4 / 40.2 | 38.4 \| 39.6 / 36.8 / 39.0 | 40.4 \| 39.1 / 36.9 / 42.0 | 40.8 \| 42.1 / 36.2 / 37.6 |
| Big-5 Top 10 share [history] | 33.8 \| 28.1 / 29.9 / 30.4 [34.2] | 24.3 \| 25.2 / 28.4 / 25.6 [20.6] | 25.6 \| 23.3 / 23.0 / 26.5 [21.2] | 26.9 \| 24.4 / 21.2 / 26.9 [26.0] | 27.0 \| 18.8 / 21.0 / 22.1 [18.7] |
| Big-5 % of **R&R** Top 10 weeks | 22.0 \| — / 16.8 / **30.4** | 19.0 \| — / 14.2 / 12.0 | 25.6 \| — / 14.9 / 13.9 | 14.4 \| — / 5.5 / 14.8 | 29.1 \| — / 11.6 / 6.7 |

- **The hypothesis holds for R&R but does not lift the 1960 head.** Professional material moves 1960 R&R Top 10
  weeks to the majors (16.8% → 30.4%), as Elvis on RCA and Holly on Decca did.
- But R&R's own 1960 Top 10 weeks fall (83 → 73 a seed) and TradPop's Big-5 share eases, so CR8 stays at 44.1.
- **Why the majors gain so little:** at equal fit, labels barely differ on professional material. A Major's R&R
  hook is 0.619 against an Independent's 0.596, because label access enters only through `polish × 0.10`.
  - Historically, publishers and song-pluggers pitched their best songs to major-label acts first. That pecking
    order is missing, and it is the remaining historically grounded route to the 1960 floor.
- **Every other lever is spent.** RA and keyframes cannot reach the 1960 head without harm (RA drains Soul; TP
  leaves CR8 at 44).

**Genres:**

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| R&R share | 10.0 \| 13.4 / 13.0 / **16.0** [13.5] | 7.5 \| 11.6 / 11.0 / **13.7** [12.8] | 6.0 \| 9.8 / 10.1 / 11.5 [11.5] | 5.1 \| 8.6 / 8.9 / 9.9 [10.0] | 3.6 \| 7.0 / 7.2 / 7.6 [6.8] |
| R&R slots | 12.6 \| 15.0 / 14.8 / 17.2 [14] | 12.0 \| 15.2 / 16.2 / 14.4 [12] | 7.8 \| 12.0 / 14.6 / 13.8 [12] | 9.4 \| 16.0 / 15.2 / 15.4 [11] | 4.2 \| 10.8 / 10.4 / 9.4 [8] |
| BritishBeat slots | | | | | 21.8 \| 19.0 / 16.2 / **22.4** [24] |
| Sum abs share error | 25.3 \| 24.3 / 22.4 / 25.4 | 28.1 \| 22.8 / 21.8 / 22.6 | 28.5 \| 22.1 / 23.2 / 22.2 | 28.3 \| 27.1 / 23.6 / 23.4 | 31.6 \| 28.8 / 27.8 / 28.9 |
| Sum abs slot error | 28.8 \| 32.8 / 30.8 / 32.0 | 35.0 \| 36.2 / 33.8 / 39.0 | 40.8 \| 38.4 / 38.8 / 38.4 | 45.4 \| 49.4 / 44.6 / 45.4 | 47.4 \| 37.4 / 44.6 / **35.0** |

- R&R's 1962-64 share is now on history (11.5 / 9.9 / 7.6).
- **1960-61 overshoot** (16.0 / 13.7 against 13.5 / 12.8; slots 17.2 against 14 in 1960; 1961 slot error +5.2,
  5/5). This is the old 1960 R&R over-supply that `GenreCatalog`'s keyframe history describes cutting (.48 → .40
  → .35); the keyframe now sits at .45.
  - With real hooks and writers it is a market quantity again. The transfer law sizes the 1960 keyframe at
    √(13.5/16.0) ≈ ×0.92 (.45 → .41).
- **BritishBeat 1964 recovers** (22.4 against 24), 1964 slot error is the best of any arm (35.0), and 1963 Big-5
  lands on history (26.9 against 26.0).

**Verdict.**

- The rock hit-writer office does what the audit asked: realistic R&R and doo-wop material without a genre cap, with
  majors converting the professional share.
- Keep it in the candidate bundle (hook scale + TP + RW), still off by default.
- Owed next, in order:
  1. R&R 1960 keyframe ×0.92, a market fix and the transfer-law size.
  2. A publisher pitch order, where majors' acts see the office's best songs first. That is the 1960-head route.
  3. Then the polar execution trace (audit item 3) and the songbook constants (audit item 2).
  4. Then a two-seed decade A/B of the bundle.

Evidence: `SimLogs/awrw-on-{1001,...,5005}-260-*` and `awtp2-on-1001-260-*`. `rr_big5.py`, `awrw_vs_awtp.txt`,
`awrw_vs_ctl.txt` and `launch_awrw.py` are in the session notes folder.

### 2026-10-19: Screen result, R&R 1960 keyframe ×0.92 (`awkf-on`) and `--publisher-pitch-order` (`awpp-on`)

**Arms**, both on the hook + TP + RW bundle:

- `awkf-on`: adds `RockAndRoll@1960=0.92`.
- `awpp-on`: `awkf-on` plus `--publisher-pitch-order` (`Systems/PublisherPitchOrder.cs`). An AI Major samples the
  top 40% of a genre's staff catalogue by hook, a MidTier the top 70%, and everyone else the bottom 80%.

A pitch-order flag-off rerun (`awrw2-on-1001`) is byte-identical to `awrw-on-1001` (84/84). The pitch-order screen
was **intended as pitch − keyframe**.

**R&R 1960 keyframe ×0.92: rejected.**

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| R&R share, RW → +KF [history] | 16.0 → 14.9 [13.5] | 13.7 → 14.0 [12.8] | 11.5 → 12.8 [11.5] | 9.9 → 11.0 [10.0] | 7.6 → 8.2 [6.8] |
| R&R slots | 17.2 → 16.4 [14] | 14.4 → 17.2 [12] | 13.8 → 17.4 [12] | 15.4 → 17.0 [11] | 9.4 → 12.0 [8] |

- 1960 closes only halfway.
- 1962-64 then surge (+1.1 to +1.3 share, 4-5 of 5 seeds; slots +1.6 to +3.6). This is the relative supply surge the
  `GenreCatalog` R&R comment documents.
- 1960 share error rises +2.9.
- A fix has to be flat across 1960-62, or act on the R&R professional anchor (.35) that produced the overshoot.

**Publisher pitch order at 40 / 70 / 80%: the right channel, far too strong.**

| pitch − keyframe | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Top 10 CR8 | 43.5 → **59.7** (5/5) | 39.9 → 56.2 (5/5) | 41.3 → 52.3 (5/5) | 41.4 → 54.6 (5/5) | 41.3 → 50.6 (4/5) |
| Big-5 Top 10 share [history] | 27.9 → 40.4 [34.2] | 25.7 → 39.3 [20.6] | 25.5 → 33.0 [21.2] | 24.1 → 36.4 [26.0] | 18.4 → 28.4 [18.7] |
| Sum abs slot error | 35.6 → 28.0 | 39.0 → 37.4 | 38.0 → 38.0 | 48.6 → 43.8 | 35.4 → 41.0 |

- Majors' staff material now leads Independents by about +0.11 hook (R&R .684 against .574, Soul .830 against
  .719, TeenPop .738 against .614).
- Big-5 share of each genre's Top 10 rises everywhere: TeenPop 1960 20 → 40%, R&B 1961 18 → 37%.
- Mean genre hooks fall 0.01-0.03, because Independents lose the top fifth.
- **The 1960 head is restored** (CR8 59.7, at the top of the 48-58 band), proving that publisher pecking order is a
  real route to it.
- **But it is a constant-strength lever**, so 1961-64 Big-5 overshoot by +9 to +19 points. Like every label-push
  lever before it, it cannot make history's 1960 → 1961 drop.
- **A grounded variant to try:**
  - much weaker (Majors the top 70% at most; Independents excluded from only the top 5-10%);
  - fading after 1960, because by 1961-63 Brill writers routinely placed hits with independents (Scepter,
    Big Top, Dimension).
  - That time profile is a historical claim and needs the author's sign-off.

Handoff for outside review: `SimTools/GenreHookCalibrationHandoff.md`.

Evidence: `SimLogs/awkf-on-*`, `awpp-on-*`, `awrw2-on-1001-260-*`. `awkf_vs_awrw.txt`, `awpp_vs_awkf.txt` and
`launch_awpp.py` are in the session notes folder.

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

**Capacity follows organization, not cash (added 2026-10-21, from the off-chart Major base work in
DemandAttentionImplementation.md).** The current weekly roll multiplies release chance by a status factor.
Under `EconomicLabelLifecycle`, "Rising" (x1.2) means the label's trailing monthly cash flow exceeds twice
its overhead. That reading suits a founder, whose cash flow is the records it just issued, but not a
corporate division. Columbia under Mitch Miller and RCA Victor under Steve Sholes issued singles on a
schedule their A&R departments could record and promote. A strong quarter of catalog, country or MOR
sales went to CBS or RCA as division profit; it did not double singles sessions. When majors expanded,
they did it deliberately through the organization: a new subsidiary label (Epic), a Nashville office, a
new A&R head, a contract for a proven act. The slate should therefore read **organizational capacity**
(A&R and producer staff, contract roster, session and promotion attention, the release calendar) for
release volume. Cash should be a feasibility floor, and the trigger for an explicit, costed expansion
decision that then raises capacity. It should never be a direct volume multiplier. Founders can keep a
tighter link, since their cash is their recent records. LPs need their own check: the LP was the
majors' main business, and catalog profit plausibly did fund LP expansion. Measured on 2026-10-21, a
.15 off-chart base lifted Majors' Rising share from 51% to 62% of weeks and their single launches by
about 3%. Only ~5% of that cash reached recording, campaigns and advances. The interim probe excludes
the base from the Rising test (`MajorOffChartSingles`); the slate should replace the status multiplier
rather than patch it.

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
