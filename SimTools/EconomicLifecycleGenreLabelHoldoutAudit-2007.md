# Economic/lifecycle genre-and-label holdout audit

## Scope and disposition

- Candidate: `d6-economic-lifecycle-genre-label-holdout-2007`
- Seed: `2007` (one authorized 522-week run)
- Fixed economic control: `d6-transition-envelope-decade-control-1001`
- Accepted implementation commit: `fb137d7bc7a24b508d07b35e46ff1ff220e46fbd`
- Overall disposition: **FAIL**

| Surface | Result | Basis |
| --- | --- | --- |
| Economy | **PASS** | Completion, reconciliation, control bands, and single-lane analyzer passed. |
| Genre evolution | **FAIL** | Psychedelic Rock emerged in 1966 but fulfilled commercial units peaked in 1969. |
| Label evolution | **PASS WITH WATCH** | Lifecycle invariants passed; annual participation was modestly below the cross-seed reference. |

The genre result controls the overall disposition. No post-holdout tuning,
seed replacement, or rerun was performed.

## Run integrity and hard gates

- Godot completed with `CHART_AUDIT_COMPLETE run=d6-economic-lifecycle-genre-label-holdout-2007 weeks=522`.
- `weeks.csv` contains 522 data rows plus its header; completed annual rows cover 1960-1969.
- `catastrophic-fail-fast.csv` is header-only.
- Both D5 probe groups passed; D6 fixed probes 1-69 passed.
- The strict 1965 acceptance period completed without a fail-fast rejection.
- The single-lane/hit-tail analyzer returned `PASS`, with no structural failures, jackpot failures, market-clearing violations, or non-finite memory rows.
- Pre/post source-state hash: `e69de29bb2d1d6434b8b29ae775ad8c2e48c5391`.
- The only source-tree difference is the pre-existing untracked handoff file; the audit and generated `SimLogs` are measurement artifacts.

The autoload diagnostic in `console.err` was non-fatal: the simulation continued
through all 522 weeks and emitted the completion marker.

## Economy

Annual rows below use `geography-metrics.csv` for fulfilled units and
`market-revenue.csv` for financial reconciliation. Scheduled-Album ratio is
scheduled Album projects divided by annual release decisions. Album units are
annual market units, not cohort/lifetime units.

| Year | Successful releases | Participants | Scheduled Albums | Sched. ratio | Album decision share | Single units | Album units | Gross | Label net | Distribution | Market net |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1960 | 4,485 | 516 | 1,273 | 0.279412 | 0.283143 | 147,029,316 | 1,595,067 | 274,408,912 | 152,383,100 | 31,670 | 152,414,771 |
| 1961 | 3,881 | 414 | 1,231 | 0.319077 | 0.319596 | 146,975,296 | 3,839,452 | 292,178,060 | 164,582,333 | 84,249 | 164,666,582 |
| 1962 | 3,547 | 339 | 1,203 | 0.338301 | 0.338583 | 145,033,067 | 5,693,741 | 303,481,033 | 172,955,922 | 97,618 | 173,053,540 |
| 1963 | 3,409 | 315 | 1,101 | 0.323443 | 0.322268 | 143,860,501 | 6,619,394 | 308,762,064 | 176,091,665 | 72,587 | 176,164,252 |
| 1964 | 3,229 | 302 | 948 | 0.292142 | 0.294915 | 152,633,664 | 7,222,426 | 329,178,430 | 186,542,567 | 73,276 | 186,615,843 |
| 1965 | 3,292 | 299 | 1,883 | 0.572689 | 0.575122 | 160,084,206 | 9,655,628 | 361,808,682 | 204,313,556 | 86,687 | 204,400,242 |
| 1966 | 2,944 | 275 | 2,218 | 0.757255 | 0.756231 | 125,716,039 | 19,212,371 | 376,705,023 | 213,010,607 | 86,953 | 213,097,560 |
| 1967 | 2,697 | 258 | 2,015 | 0.746296 | 0.750000 | 92,170,125 | 28,013,009 | 387,046,371 | 222,490,540 | 247,251 | 222,737,791 |
| 1968 | 2,840 | 253 | 2,132 | 0.754423 | 0.749469 | 95,689,473 | 36,231,480 | 458,729,842 | 264,365,002 | 274,098 | 264,639,101 |
| 1969 | 2,672 | 261 | 1,999 | **0.743399** | 0.749349 | 94,823,759 | 44,008,186 | 519,091,451 | 300,026,186 | 236,441 | 300,262,628 |

Decade totals are 1,304,015,446 Single units, 162,090,754 Album units,
33,051 decisions, 3,611,389,868 gross, 2,058,052,310 market net, and
1,290,830 distribution income. The minimum annual scheduled-Album ratio is
0.279412 in 1960; explicit checkpoints are 0.323443 in 1963, 0.754423 in
1968, and 0.743399 in 1969. The 1969 scheduled ratio and 0.749349 Album
decision share remain in the accepted measurement neighborhood.

## Genre evolution

The complete canonical aggregation used non-overlapping `AllSegments` rows
for market means and summed `geography-metrics.csv` for fulfilled units. No
overlapping segment rows were added together.

Historical checks supported by the immutable output:

- Doo-Wop peaked in 1960 at 23,584,028 units and declined to 150,003 in 1969.
- Surf Rock peaked in 1963 at 4,580,822 units and was down to 16,473 by 1969.
- Folk peaked near the middle of the decade, 1965 at 9,788,182 units, then fell to 992,976 in 1969.
- British Beat and British Pop are present from 1964 and peak in 1965.
- Acid Rock peaks in 1967; Hard Rock, Funk, Blues Rock, Proto-Metal, Progressive Rock, and Singer-Songwriter strengthen in the later staggered windows.
- The largest annual genre share is 23.24% in 1967, below the 35% cap.

### Psychedelic Rock

| Year | Supply selections | Mean routed acceptance | Mean eligible | Mean charted | Fulfilled units | Backorders | Backorder rate | Units / supplied project | Singles | Albums | Orphan | Promo | Standalone | Cohort/lifetime units* |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1966 | 15 | 0.759106 | 9.87 | 0.37 | 321,076 | 159,863 | 33.24% | 21,405.07 | 2 | 13 | 2 | 0 | 3 | 444,906 |
| 1967 | 32 | 0.914241 | 28.79 | 0.00 | 83,679 | 30,096 | 26.45% | 2,614.97 | 0 | 31 | 0 | 0 | 30 | 409,761 |
| 1968 | 39 | 0.775372 | 56.71 | 0.00 | 139,828 | 31,029 | 18.16% | 3,585.33 | 1 | 38 | 1 | 0 | 35 | 563,742 |
| 1969 | 57 | 0.815227 | 86.00 | 0.35 | **392,265** | 54,967 | 12.29% | 6,881.84 | 5 | 51 | 5 | 0 | 51 | 637,369 |

\* Cohort/lifetime values are summed from `format-decision-cohort-details.csv`
for rows whose raw primary or secondary identity is Psychedelic Rock. They are
not annual commercial units.

The highest-yield raw identity rows include Acid Rock -> Psychedelic Rock
(1966, 296,699 units), Garage Rock -> Psychedelic Rock (1968, 198,946), Acid
Rock -> Psychedelic Rock (1967, 151,871), and Psychedelic Rock -> Blues Rock
(1969, 107,045). The narrowest supported first divergence is an aggregate
supply/catalog seam: supply grows from 15 to 57 projects while fulfilled units
and lower backorder rates culminate in 1969. Lean telemetry cannot separate
completed-drop delay from pre-year catalog carry-in at record level, so it does
not support a stronger scheduling-versus-carry-in causal claim.

Genre evolution is therefore **FAIL**: the authored 1966 emergence is present,
but the required commercial peak around 1967-1968 is not met.

## Emergent-label evolution

The prescribed helper compared 1969 against the accepted 1001 reference:

| Metric | Control 1001 | Candidate 2007 |
| --- | ---: | ---: |
| Mean active labels | 261.06 | 261.15 |
| End active labels | 251 | 264 |
| Mean active runtime founders | 141.58 | 143.04 |
| End active runtime founders | 138 | 149 |
| Distinct participating labels | 265 | 261 |
| Runtime-founded participants | 141 | 142 |
| Runtime-founded decisions | 554 | 482 |
| Successful releases | 2,777 | 2,672 |
| Capacity success rate | 99.32% | 98.93% |

Runtime-founded births are 72 in each full mature year 1961-1969. The run
recorded 677 runtime bootstrap events, 525 organic-growth events, 307 target
transitions `1->2`, 180 `2->3`, and nonzero bounded growth beyond three slots
(`3->4`: 20; `4->5`: 10; `5->6`: 5; `6->7`: 2; `7->8`: 1). Organic growth
was distributed across 1961-1969 as 52, 53, 77, 54, 58, 47, 44, 76, and 64
events. There were 12 promotion reconciliations and 17 demotion
reconciliations; the console shows rare promotions and competitive exits,
rather than a mechanical promotion wave. Runtime founders had nonzero 1969
survival and release participation.

The label surface is **PASS WITH WATCH** because the candidate's active
population and lifecycle structure are consistent with intent, while release
decisions and runtime-founded decisions are below the cross-seed reference.
This is diagnostic only and was not tuned.

## Immutable artifacts

Primary completion log: `SimLogs/d6-economic-lifecycle-genre-label-holdout-2007-console.log`

Prescribed analyzer outputs:

- `SimLogs/d6-economic-lifecycle-genre-label-holdout-2007-single-yield.json`
- `SimLogs/d6-economic-lifecycle-genre-label-holdout-2007-label-survival-participation.md`

All seed-2007 output files were preserved after the one authorized run.
