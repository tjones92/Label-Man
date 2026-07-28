# D7 label population — everything below MidTier is pinned by chart access

Continues from `D7LabelPopulationBirthRateHandoff.md`. That document asked for a staged rise
in `maxMonthlyBirths` (6 → 8 → 10) to arrest a decline in live label count. **Stage one
breaches the gate, the decline it was correcting does not exist, and the birth cap is not the
lever.** This document records those corrections and the constraint that actually binds.

The pattern of the last two handoffs repeats. The previous document corrected a measurement
artifact (retained defunct records) and then reasoned about the corrected series using a
decade-average death rate. That average is itself an artifact of the launch cohort's
first-year die-off, and it is what produced the phantom decline.

## 1. The population is not declining

`D7LabelPopulationBirthRateHandoff` computed equilibrium as `births ÷ death rate` =
`72 ÷ 0.285 ≈ 253`, and read the observed 319 as "still drifting down toward it". The 0.285
is the whole-decade average, dominated by 1960-62 when the 600-label launch cohort was culled.
Measured against the standing population in each year, the back-half rate is ~22%:

| year | live @ year end | deaths in year | death rate | births |
|---|---|---|---|---|
| 1965 | 298 | 83 | 27.9% | 72 |
| 1966 | 302 | 68 | 22.8% | 72 |
| 1967 | 301 | 73 | 24.2% | 72 |
| 1968 | 312 | 61 | 20.3% | 72 |
| 1969 | 317 | 67 | 21.5% | 72 |

Back-half mean 22.2%, so equilibrium is `72 ÷ 0.222 ≈ 324`. The observed 317 is just below it
and **rising** — 298 → 302 → 301 → 312 → 317 across 1965-69, with deaths (mean 67.25) running
below births (72). The series is at rest, not in decline. Stage one's stated purpose, "so the
decline reverses instead of continuing", had nothing to reverse.

## 2. Stage one breaches the gate

`maxMonthlyBirths` 6 → 8, gated at 522 weeks against the frozen control, aborted:

```
CompletedYearCatastrophicDivergence,scheduledAlbumProjects,3055,2324,1966,week=366,ratio=1.314544,band=[0.70,1.30]
```

The prior handoff read `scheduledAlbumProjects` as tight only at 1960 ("a thin-denominator
first-year effect; the back half runs 1.10-1.24, so there is perhaps 5-15% of roster growth
available there"). The binding year is **1966**, and it is a fat-denominator year. Head ran
1.2431 there — 0.057 of headroom, not 5-15%. Two extra births a month consumed it.

| year | births 6 (head) | births 8 | ceiling |
|---|---|---|---|
| 1960 | 1.2728 | 1.2695 | 1.30 |
| 1965 | 1.1605 | 1.2183 | 1.30 |
| 1966 | **1.2431** | **1.3145** | 1.30 |

## 3. What the extra births bought

Measured at 1966, the last completed year both runs share:

| metric | births 6 | births 8 | change |
|---|---|---|---|
| live labels | 302 | 340 | **+12.6%** |
| aggregate operating target (live only) | 2,715 | 2,789 | +2.7% |
| rostered (live only) | 2,658 | 2,729 | +2.7% |
| live-only fill | 0.979 | 0.979 | — |
| deaths in year | 68 | 97 | **+42.6%** |
| `scheduledAlbumProjects` ratio | 1.2431 | 1.3145 | +5.7% |

A third more founding bought 2.7% more roster capacity while costing 5.7% of album-project
volume — it consumes gate headroom at roughly twice the rate it buys the capacity the work was
chasing. And the level barely moved: equilibrium went ~324 → ~338 (+4.3%) for +33% births,
because the death rate rose from 22.2% to 28.4% to absorb them.

The added album projects come from **churn, not standing population**. Births went 72 → 96/yr
and deaths 68 → 97/yr. Each new label bootstraps a roster and starts releasing; most die within
a year or two, but they schedule album projects while alive.

## 4. The constraint: chart access, not appetite

The homeostasis is not incidental. `TryApplyCompetitiveExit` grants safe harbour at
`chartingLastYear >= CompetitiveExitSafeHarborChartingRecords` (2). Survival is gated on
charting — and charting capacity is fixed by chart width, not by how many labels exist.

`firmsCharting` from `concentration.csv` is flat whatever the population:

| year | births 6: live / charting / share | births 8: live / charting / share |
|---|---|---|
| 1960 | 477 / 160 / 33.5% | 482 / 161 / 33.4% |
| 1962 | 356 / 119 / 33.4% | 391 / 117 / 29.9% |
| 1964 | 309 / 108 / 35.0% | 348 / 99 / 28.4% |
| 1965 | 298 / 108 / 36.2% | 341 / 105 / 30.8% |

Adding 39 live labels produced **fewer** charting firms (108 → 99). More labels means a smaller
charting share, which means more labels exposed to full exit hazard, which raises the death
rate until the population returns to where it was.

And chart access is not merely scarce — below MidTier it is **absent**. Live labels holding a
recent hit at week 365 (`label-scouting-vacancy-weekly.csv`, head run):

| tier | live | has recent hit | share |
|---|---|---|---|
| Small | 111 | **0** | 0% |
| Independent | 79 | **0** | 0% |
| Boutique | 37 | 4 | 11% |
| MidTier | 65 | 49 | 75% |
| Major | 10 | 10 | 100% |

**Zero of 190 live Small and Independent labels chart.** The sim runs a two-class industry:
~75 labels that chart, and ~300 that never do and exist only to die. That single fact
disables three separate systems at once, because all three are gated on charting evidence:

- **Promotion.** `Small -> Independent` and `Independent -> MidTier` both require charting
  records. At 0% neither can fire, whatever the threshold is set to.
- **Survival.** Exit safe harbour requires two charting records, so every Small and
  Independent label sits permanently on maximum exit hazard.
- **Growth.** `GetOrganicGrowthBlockingReason` requires `chartingCount >= 1` once the target
  reaches `RuntimeEmergenceReleaseLaneTarget` (3), so runtime founders cannot grow past three
  slots.

This is also where the sim departs from history most clearly. Roughly 200-400 distinct labels
reached the Hot 100 in a mid-60s year against the sim's ~104, and the era's defining story is
small independents charting constantly — Motown, Stax, Sun, Chess, Vee-Jay, Philles, Scepter
all did it from tiny rosters. A model in which no label under MidTier ever charts cannot
reproduce that.

## 5. Seven is the largest gate-clean birth rate — but it is not what landed

**This section documents a measurement, not the committed state.** `maxMonthlyBirths` stays at
6; see section 6 for why the ladder repairs were preferred over the population gain.

`maxMonthlyBirths = 7` (84/yr) completes 522 weeks clean. Its peaks are not worse than head —
the global maximum across all six metrics *improves*, because the 1960 album-project spike
comes down further than the 1966 one rises:

| metric | head (births 6) | births 7 | ceiling |
|---|---|---|---|
| `successfulReleases` | 1.1006 (1962) | 1.1371 (1964) | 1.30 |
| `scheduledAlbumProjects` | **1.2728 (1960)** | **1.2642 (1966)** | 1.30 |
| `totalUnits` | 1.0888 (1969) | 1.0835 (1969) | 1.30 |
| `grossRevenue` | 1.1181 (1969) | 1.1151 (1969) | 1.30 |
| `labelNet` | 1.2073 (1969) | 1.2000 (1969) | 1.30 |
| `marketNet` | 1.1654 (1964) | 1.1601 (1964) | 1.30 |

Read the year, not just the peak: at **1966** the album-project ratio goes 1.2431 → 1.2642, so
headroom in the year that binds future roster growth falls from 0.057 to 0.036.

What it delivers at week 521:

| metric | head (births 6) | births 7 |
|---|---|---|
| live labels | 317 | **344** (+8.5%) |
| deaths / births in 1969 | 67 / 72 | 76 / 84 |
| aggregate operating target (live only) | 2,788 | 2,753 (−1.3%) |
| rostered (live only) | 2,765 | 2,714 (−1.8%) |
| live-only fill | 0.992 | 0.986 |
| `firmsCharting` | 103 | 105 |
| latent prospects | 1,544 | 1,580 |

The population rises 8.5% and is still climbing at 1969 (328 → 336 → 344), settling near
`84 ÷ 0.24 ≈ 350`. **Roster capacity does not follow it** — appetite per live label falls from
8.79 to 8.00 and the absolute total drops slightly. Fill stays ~0.99 and the reservoir stays at
~1,580 latent against ~68 affordable vacancies, which answers section 4 of the prior handoff:
appetite is still not the constraint and talent supply is nowhere near binding.

Seven buys label count and nothing else.

## 6. The tier ladder: three defects, one affordable

Head runs 11 promotions against 12 demotions over a decade of 300-480 live labels, which is
too static to be historical. Three separate defects were found and fixed; a fourth change was
measured and reverted.

**Defect A — the bottom rung collides with the exit rule.** `Small -> Independent` required
`chartingLastYear >= 2`, which is exactly `CompetitiveExitSafeHarborChartingRecords`. The only
Small labels that qualified for promotion were the ones already immune from exit. Now
`IndependentPromotionMinimumRecentChartingRecords` (1) — the same signal the exit rule already
credits through `CompetitiveExitOneChartMultiplier`. **This fix is currently inert**: 0% of
Small labels chart at all, so it fired once. It is correct and it becomes live the moment
charting breadth is addressed.

**Defect B — an off-by-one against the tier's own capacity.** `Boutique -> Independent`
required `CurrentRosterSize > BoutiqueAuteurRosterThreshold`, and that threshold is 8, which is
also `GetRosterCapacityForTier(Boutique)`. Zero of 37 live Boutiques could ever satisfy it;
the rung had only ever fired for launch labels seeded above their own cap. Now `>=`.

**Defect C — the rung promoted on seeded state.** Fixing B alone promoted **21 labels in the
first six months of 1960**, because the launch population is generated with full boutique
rosters and this rung, alone among the four, had no operating-time gate. Added
`monthsActive > 18`, matching `Small` and `Independent`. Promotions in the first five months
went 21 → 0.

**Reverted — promotion still does not grant appetite.** `ReconcileCapacityForTierChange` hands
a promoted label a larger `maxRosterSize` while preserving its prior operating target, and
`IsOrganicGrowthEligibleOrigin` admits launch labels only at Major/MidTier. A promoted launch
Small therefore receives a cap it can never grow into: promotion moves the tier without moving
appetite. Admitting promoted labels (via `AILabel.hasEarnedTierPromotion`, which is retained
for exactly this purpose) was measured at 522 weeks and **breaches**: thirteen promoted
Boutiques growing from eight slots toward twelve carried `scheduledAlbumProjects` to **1.3219**
at 1966, worse than births 8. It is available once album-project economics have slack.

The honest summary is that the ladder's rungs were genuinely broken and are now genuinely
fixed, but fixing them cannot produce historic promotion rates while no label below MidTier
ever charts. Promotion is downstream of the same constraint as population and growth.

### The 1966 ceiling affords the population gain or the ladder, not both

Every configuration measured, at `scheduledAlbumProjects` for 1966 (control 2,324):

| configuration | 1966 value | ratio | verdict |
|---|---|---|---|
| head — births 6 | 2,889 | 1.2431 | clean |
| births 7 | 2,938 | 1.2642 | **clean at 522** |
| births 6 + ladder rungs | — | — | **committed, uncertified** |
| births 7 + ladder rungs | 3,024 | **1.3012** | abort |
| births 8 | 3,055 | 1.3145 | abort |
| births 7 + ladder + growth eligibility | 3,072 | 1.3219 | abort |

Births 7 costs 0.021 of headroom and the ladder rungs cost 0.037. Together they are 0.058
against the 0.057 that head leaves, so the pair misses by 0.0012 — the `births 7 + ladder
rungs` abort is that close. **The two deliverables are mutually exclusive at present.** The
ladder was taken because the static tier structure is the more ahistorical of the two
symptoms, so `maxMonthlyBirths` stays at 6 and the population stays at its ~324 equilibrium.

### What is committed, and what is unverified

Committed: `maxMonthlyBirths = 6` (unchanged from head) plus ladder defects A, B and C.
`AILabel.hasEarnedTierPromotion` is maintained but unread, documented as the input to the
reverted growth-eligibility change.

**This configuration is not gate-certified.** Its run (`d7-ladderonly-gated-522-1001`) was
stopped by hand at May 1964 having cleared 1960-63 with no breach. The binding year is 1966,
so the decisive result does not exist yet. The arithmetic above predicts ~1.280, which fits,
but that is interpolation from the births-7 delta and not a measurement.

Partial evidence through May 1964 is encouraging on the directive's actual aim — 18 promotions
(15 `Boutique -> Independent`, 3 `Small -> Independent`) against head's 11 for the entire
decade, with `Small -> Independent` firing at all for the first time.

### Resume here

1. Re-run `d7-ladderonly-gated-522-1001` to completion. It must clear 1966 (week 366); the
   full 522 is still needed because `labelNet` peaks at 1969.
2. If it breaches, the cheapest thing to drop is defect A (`Small -> Independent` at one
   charting record), which is inert anyway at 0% Small charting, then defect B/C.
3. If it is clean, re-measure promotions and demotions with `tiers.sh`-style transition counts
   from `label-finance.csv`, and confirm live-only fill has not moved.

## 7. Recommendation

**Charting breadth is the prerequisite for all of it.** One change unblocks population level,
promotion mobility, roster growth and historical fidelity together, because all four read the
same signal. The question is how chart slots are allocated across labels — specifically why
no Small or Independent label ever takes one — not how any lifecycle rule is tuned.

Expect to need album-project economics alongside it. `scheduledAlbumProjects` at 1966 sits at
1.2642 with 0.036 of headroom, and every change measured here that raised release volume
breached there. Until that has slack, nothing that broadens charting can land either.

Do not stage `maxMonthlyBirths` further; 8 is measured to breach. Do not damp
`TryApplyCompetitiveExit` to buy the level — the prior handoff was right that high founding
against high failure is the correct sixties texture. What this measurement adds is that the
exit rule's *safe-harbour threshold* is the coupling to chart capacity, so that rule should be
revisited as part of the charting work rather than by lowering its base chance.

## 8. Secondary findings

**The launch cohort dies in synchronised waves.** Every label death in the first 120 weeks
lands on a quarterly review, and the culls are extremely uneven:

```
week 35  deaths 76      week 61  deaths 47      week 100 deaths 26
week 48  deaths 53      week 74  deaths 25      week 113 deaths 23
```

All 600 launch labels start at `monthsActive = 0`, so they cross
`LaunchCompetitionMinimumOperatingMonths` (6) together and meet `TryApplyCompetitiveExit` for
the first time at the same quarterly review — week 35 is month 9 of 1960, and 12.9% of the
population closes that day. This is the same class of defect as Defect C above, on the death
side. Dispersing launch `monthsActive` would smooth it; it perturbs the RNG stream and needs
its own gated run.

**Dead public surface on `LabelLifecycleManager`.** `GetLabelsByTier`, `GetLabelsByGenre`,
`GetLabelsInRegion`, `GetRandomLabelForSigning`, `MajorLabels`, `DefunctThisYear` and
`FoundedThisYear` have zero callers outside the class. `GetRandomLabelForSigning` is a trap if
wired up: it admits candidates on `CurrentRosterSize < maxRosterSize`, while
`ValidateCatastrophicStructural` aborts the run when `CurrentRosterSize > OperatingRosterTarget`.

## 9. Reproduction

Unchanged, and the control is still the frozen disabled route (commit 53eac45) — do not
re-baseline it.

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d7-portfolio-gated-decade-control-1001
```

**Iterate at 366 weeks, not 260.** The prior handoff recommended 260 as a cheap filter. For any
change touching release or roster volume that is now wrong: the binding metric is
`scheduledAlbumProjects` at **1966**, which completes at week 366. A 260-week run passes
changes that breach at 1966 — which is exactly how stage one looked clean.

Gate ratios are reconstructable from CSVs without re-running: `successfulReleases` and
`albumProjectsScheduled` sum from `seasonality-monthly.csv` (columns 18 and 20), and
`totalUnits`/`gross`/`labelNet`/`marketNet` come from the `annual`/`All`/`All` rows of
`market-revenue.csv`. These reproduce the runner's own accumulator exactly.

Runs recorded here, all seed 1001 against control `d7-portfolio-gated-decade-control-1001`:

| run | configuration | result |
|---|---|---|
| `d7-rotation-gated-522-1001` | births 6 | head, clean |
| `d7-births7-gated-522-1001` | births 7 | **clean at 522** |
| `d7-births8-gated-522-1001` | births 8 | abort, `scheduledAlbumProjects` 1.3145 at 1966 |
| `d7-ladder-gated-522-1001` | births 7 + ladder + growth eligibility | abort, 1.3219 at 1966 |
| `d7-rungs-gated-522-1001` | births 7 + ladder rungs | abort, 1.3012 at 1966 |
| `d7-ladderonly-gated-522-1001` | births 6 + ladder rungs (**committed**) | **incomplete** — stopped by hand at 1964-05, clean through 1963 |

Probe suites pass (`d7-ladder-probes-52-1001`): D5, D6 fixed probes 1-71, and genre-market-v2.
Run them separately from any gated comparison — both probe flags perturb the RNG stream.

Unrelated pre-existing noise on every run: `MissingSingletonsTemp.cs` is registered as an
autoload in `project.godot` but does not inherit from `Node`, so Godot logs a failed-autoload
ERROR at startup. It does not affect the simulation.
