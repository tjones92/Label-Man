# D7 emergent-genre formation — session handoff

Opened August 4, 2026. Updated August 5, 2026. Branch `d7-genre-decade-calibration`.

Sibling of `D7GenreChartDivergenceHandoff.md` (the chart-divergence arc) and
`D7GenreDecadeCalibrationHandoff.md` (the chart mechanism).

## 0. The one-line brief

**This arc is closed.** The formation regression is repaid and over-repaid, and the investigation
it was blocking has resolved: the remaining Mode A deficit is **not** supply, **not** signing, and
**not** release conversion. It is a missing chart-efficiency dimension. §8 is the handoff for that.

## 1. State

| run | what it is |
|---|---|
| `d7-genretune2-decade-522-1001` | Pre-ladder reference. The original "−42%" figures are against this. |
| `d7-career-ladder-decade-522-1001` | §17. Emergent signings −63.0%. |
| `d7-contractterm-decade-522-1001` | §18. Emergent signings −42.4%. |
| `d7-formationbase-decade-522-1001` | **CURRENT HEAD.** This session. Formation base raised, cliff fixed, `Emerging` supply penalty removed. |

Build clean, both probe suites pass, **nothing committed.** Godot/Python invocations and the
"never pipe a long run through a PowerShell pipeline" rule are unchanged from the sibling doc §1.

```
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance --emergent-signing-floor=350
```

## 2. RETRACTION: §4 of the previous revision was wrong

The previous revision declared `CalculateResponsiveAnnualFormationTarget` inert — "`unmetShare` is
zero in all 1,566 measured run-weeks" — and sent the next session hunting for an unidentified
second driver. **There is no second driver. The servo was always the driver.**

The error was reading the wrong telemetry columns. The servo is fed
`seeking.Count(IsFirstContractProspect)` (`ArtistManager.cs:742-743`), published as
**`freshSeeking` / `freshLatent`**. The retracted analysis used `seekingProspects` /
`latentProspects`, which count everyone. Recomputed correctly, mean annual target x 10 years
reproduces every run's formation total to within 1%:

| run | mean target | x10 | measured |
|---|---:|---:|---:|
| genretune2 | 667 | 6,670 | 6,657 |
| career-ladder | 329 | 3,290 | 3,232 |
| contract-term | 483 | 4,830 | 4,420 |

**Lesson worth keeping:** when a mechanism looks inert, check that the telemetry column matches the
variable passed at the call site, not the one with the matching parameter name.

## 3. What actually drove the regression

The seeded first-contract reserve drains and never refills — `freshSeeking + freshLatent` goes
**4,052 (1960) -> ~15 (1965 onward)**, and `neverSignedUnsigned` hits 0.0 from 1965 in every
pre-HEAD run. From 1965 the servo's supply term is effectively zero, so formation volume became a
pure function of `affordableHiringVacancies`.

§17-18 correctly *reduced* standing vacancies (clean exits, immediate re-signing, fuller rosters:
1969 rostered 1,187 -> 1,219). Formation fell with them. Emergent genres, which emerge after 1964
and have no seeded population by construction, paid the entire tax.

**The baseline was never a restoration target.** genretune2's 6,657 formations came from vacancies
standing open because performance-dropped acts were cooldown-blocked from re-signing. Chasing that
number would have restored the bug. The previous revision's §4.1 step 4 ("back to the ~2,552
baseline") should not be followed.

## 4. The change at HEAD

- **`BaseAnnualRuntimeFormationCount` 300 -> 2200**, **`MaximumAnnualRuntimeFormationCount`
  1200 -> 3000.** The base is the knob, not the ceiling and not the gain:
  - the old ceiling was exactly `base * (1 + gain)` = 300 x 4 = 1200, **unreachable by
    construction** — it clipped 0 of 522 weeks. Raising it alone would have been inert.
  - the servo is a negative-feedback loop whose setpoint is label vacancies. Raising `gain` only
    reaches that same setpoint faster; it can never build a surplus.
  - 2,200 is sized to measured demand: from 1965 `firstTimeSignings` equals formations exactly
    every week, ~100 roster seats sit permanently unfilled, and total slot-fills run ~2,020/yr
    (~1,030 first-time + ~990 re-signings). 2,200 is one absorption cycle — the first value that
    leaves a real surplus.
  - Applies to **all ten years**, by author's preference. Acts form because scenes exist, not
    because labels have open chairs, and there is no reason 1960 should produce a seventh as many
    new acts as 1966. The 1960 seeded reserve now reads as the pre-existing industry.
- **Mid-year formation cliff fixed.** The annual ceiling now measures against the year's peak
  target rather than the current week's. The old form let a vacancy dip retroactively declare the
  year over-supplied *and* discard the fractional carry — 1968 at the previous head asked for a
  mean 569 and delivered 473 across 35 live weeks of 52.
- **`GenreSupplyService` `Emerging => .65f` removed** (`Declining => .35f` kept). It taxed a genre
  35% during its one-year launch window; measured formation share against authored baseline share
  was 0.44-0.45 for genres in that window versus ~1.0 for established ones.
- **Formation accumulator is now `double`.** In float32 the rounding error over 52 additions scales
  with the increment and overran the 1e-5 epsilon at the raised quota, losing the last act of the
  year. The old 300/1200 quotas stayed inside it by luck.
- **`--emergent-signing-floor=N`** — standalone early abort, no control run required, fires once at
  the end of 1965. Genres are derived from the catalog (`EmergenceYear > 1960`) rather than listed,
  so retiming a genre cannot stale the gate. HEAD scored `signed=1213 formed=2700` against 350.
- **Quadratic removed from the population layer** (see §7).

## 5. Result at HEAD

**The unsigned reservoir now exists.** Emergent signed/formed **100% -> 60.1%**. The previous
revision's §3 headline — "there is no unsigned reservoir of emergent-genre artists at all, in every
run" — is no longer true. Labels can finally select instead of taking everyone who forms.

| | genretune2 | contractterm | **HEAD** | band |
|---|---:|---:|---:|---|
| all formations | 6,657 | 4,420 | **22,000** | — |
| emergent formed | 1,178 | 674 | **2,934** | — |
| emergent signed/formed | 100% | 99.9% | **60.1%** | — |
| distinct #1s | 170 | 176 | 176 | ~203 |
| mean #1 tenure | 2.76 | 2.69 | 2.69 | 2.57 |
| mean chart life | 8.20 | 7.94 | 7.99 | 7.48 |
| breadth | 454 | 470 | **479** | 400-600 PASS |
| MidTier 1969 | 32 | 27 | **37** | 25-40 PASS |
| owner-Major 1968 | 44.3 | 40.4 | 42.2 | 45-52 FAIL |
| owner-Major 1969 | 45.8 | 45.3 | **42.9** | 45-52 FAIL, lost |

`MidTier 1969` is off the floor. **owner-Major 1969 left a band it had been passing** — the
volume-denominator effect: a larger unsigned pool lets more indies fill rosters and chart, diluting
owner-Major entry share. Author's call: deprioritized, it may return with other adjustments.

Metric definitions that reproduce the sibling doc exactly, recorded because two were rediscovered
the hard way: owner-Major is `ownerMajorEntries / chartEntries` (**not** `ownerMajorFamilyEntries`);
breadth is `cumulativeFirmsCharting`; MidTier is the annual `midTierFirmsCharting`; chart life is
the mean of `weeksOnChart` over records that **charted at all**, from the last row per `recordId`.

## 6. The finding this unblocked, and it is the whole point

Restoring supply was necessary and is **not sufficient** — the previous revision's §2 was right.
Sunshine Pop went 2 -> 3 year-end slots, inside the noise floor.

But the diagnosis is now measured rather than assumed, and **the original hypothesis of this whole
arc is false**. Emergent genres do not fail to build chart-worthy credibility. They convert
releases into charting records **better** than established ones:

- emergent **9.95%** of releases reach the chart vs established **5.13%**
- FolkRock (15.47%) and PsychedelicRock (14.96%) are 1st and 3rd in the entire simulation

Nor is throughput the answer: total releases moved +2.4% while the artist pool grew 5x, so release
volume is label-bound. Author's call: **this is correct and intended** — the economy is balanced
around it, and a large surplus of unsigned talent is historically accurate.

The real defect is in §8.

## 7. Population-layer quadratic (fixed)

The raised formation rate exposed cost that the 300/yr rate had hidden. `ArtistManager` ran a
`List.RemoveAll` over the unsigned pool **per artist**, inside loops over the whole registry — five
sites — and `ReconcileLifecycleAndOwnership` additionally swept **every label's roster per inactive
artist**, twice a week. `seekingMissingFromUnsignedPool` did a `List.Contains` per seeking artist.
All are quadratic in exactly the pool a raised formation rate grows. Measured cost before the fix:
1.47x wall clock, `populationLifecycleSeconds` 3.26x.

Replaced with mark-and-flush batching over a reused `HashSet`. Safe because every marked artist has
just become ineligible and the existing `ReconcileEnabledUnsignedPool` integrity sweep would drop it
regardless — the flush is an optimization on top of a guarantee that already held, so a missed flush
costs a pool entry the next sweep removes, never a stale entry that survives. `RemoveAll` is stable
and the batch is a set of identities, so survivor order is unchanged.

**Verified inert, not assumed:** both probe suites pass and all 8 compared telemetry files are
byte-identical to the pre-fix probe run (`d7-formation-probe52` vs `d7-quadfix-probe52`).

## 8. NEXT SESSION: the missing chart-efficiency dimension

**A genre's chart presence per point of market share is an independent historical dimension, and
the model does not have it.** Slots per point of unit-share, 1969:

| | SunshinePop | CountryRock | EasyListening | Soul | FolkRock | Jazz | Country |
|---|---:|---:|---:|---:|---:|---:|---:|
| history | **16.44** | 4.58 | 2.35 | 1.60 | 0.68 | 0.49 | **0.26** |
| model | **0.00** | 1.24 | 0.51 | 1.68 | 1.31 | 0.70 | **1.78** |

History spreads this **63x**. The model spreads it **3x**, and `corr(history, model) = -0.32` —
not merely compressed but **inverted**. Country, which should be the least chart-efficient genre in
the decade, is the model's most; Sunshine Pop, which should be the most, is zero.

This is one defect with two signs, and it is the two largest year-end misses at once: Country
**+47 slots** while *under* on market share, Sunshine Pop **-27 slots** while its market share is
one of the best fits in the sim (MAE 0.27; 1969 actual 0.70% vs 0.73% target).

**Why the model cannot express it:** chart conversion is a pure function of the authored demand
baseline — `chart% of releases = 27.80 x baseline²`, corr 0.841, consistent with the quadratic
transfer law. There is no per-genre term for hit concentration, so a genre cannot turn a small unit
share into heavy chart presence the way singles-driven pop did.

### 8.1 Author's correction, recorded so it is not re-litigated

An earlier reading of this session treated Sunshine Pop's 16.44 as the two benchmarks
*contradicting* each other, and proposed resolving it by moving the market-share target. **That is
wrong. Both benchmarks are authoritative.** Sunshine Pop really was a genre that took a small
market share and charted heavily with it — it wants to do exactly what Country is erroneously doing
in this model. The share table's note "Real but never a trade category" means the *tag* was absent
from 1960s trade literature, not that the genre was commercially small.

The sibling doc §2 rule still stands and is not in tension with this: a baseline keyframe is a
demand quantity, the market table wins on baselines, and a genre that charts wrongly at a correct
market share is a chart-side defect. Sunshine Pop is precisely that case.

### 8.2 Order of work

1. **Add a chart-efficiency term independent of the demand baseline.** This is the mechanism fix and
   it is what makes both Country and Sunshine Pop right. Size against the history row above; the
   target is a ~63x spread, so the term needs real range, not a nudge.
2. **Retime Sunshine Pop's late keyframes** to strengthen 1969 — authorized by the author. It is
   wanted independently, but note it cannot produce a 63x spread on its own and must not be used as
   a substitute for (1). Size by `sqrt(target/current)` for the quadratic.
3. **Re-audit all genres on both benchmarks.** Current totals: year-end absolute slot error **642**,
   market-share absolute error **173.6 pts**. Worst year-end offenders after Country: RnB and
   TraditionalPop have correct decade totals but wrong shapes (RnB +35 late, TraditionalPop -12
   late) — pure timing errors; Jazz +37 and Folk +35 are large over-charts on small genres;
   BritishBeat -30, GarageRock -23, BritishPop -15 under-chart. RockAndRoll's -31 is **expected and
   correct** per the sibling doc's misclassification caveat.

## 9. Things not to redo

- **Do not re-derive the career ladder, the probation window, or the contract term.** Sibling §17
  and §18 are settled and probe-covered (`2d-2r`).
- **Do not look for a scouting, lane-competition, artist-choice or roster-occupancy explanation for
  the emergent deficit.** Dead before, and now doubly so: emergent acts are no longer signed at
  100%, and they out-convert established genres.
- **Do not treat formation, signing, or release conversion as the Mode A lever.** All three are
  measured and closed by §5-6.
- **Do not raise `MaximumAnnualRuntimeFormationCount` or `FormationDemandGain` to move formation
  volume.** §4: the ceiling was unreachable by construction and the gain cannot beat the servo's
  own setpoint. The base is the knob.
- **Do not trust the previous revision's §4.** Retracted in full by §2.
- **Do not resolve Sunshine Pop by moving its market-share target.** §8.1.
- **Do not read single-seed genre deltas under ~50 points as signal** (sibling §10). Sunshine Pop
  2 -> 3 is noise; PsychedelicRock 32 -> 53 and FolkRock 25 -> 45 are not.

## 10. Still open, unchanged and out of scope

- **owner-Major 1968/1969** below band. Deprioritized by the author this session.
- **Label buzz in `CalculateArtistChoiceUtility`** — sibling §17.4. Real channel (18.7% of
  signings); `momentumScore` is a dud measure that would make it inert.
- **Wire the momentum engine** — sibling §14.
- **`SingleOrientation` and the unlocated 1.182x residual** — sibling §18.4.
