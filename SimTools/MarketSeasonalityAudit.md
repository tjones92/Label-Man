# Directive 4C / 4C-R Market Seasonality Audit

Status: **accepted 2026-07-10.** 4C originally failed its same-seed/year
`+/-5%` unit gate. 4C-R prospectively replaced only that gate with a
three-seed pooled calendar-year test, following the established 4b timing/RNG
precedent. The final release-only candidate, fresh holdout, and exact-off
regression pass. Earlier reports that Godot was unavailable were pre-run status
and are superseded by the completed headless runs below.

## Frozen implementation

`Systems/MarketSeasonality.cs` owns every table, legacy curve, `DateTime`
Friday-count normalizer, and public getter. It calculates the 1960-69 total of
522 Fridays (`44,40,45,43,44,43,44,44,44,43,43,45` by month), preserves each
format's legacy Friday-weighted annual sales budget before the permitted level
scalar, and gives non-sales channels a Friday-weighted mean of one (radio zero
additive mean). The final constants are Single level `1.00`, Album level `0.98`.

`ChartManager` applies format demand once and bypasses the enabled overlay in
synthetic `SimulateWeek(triggerEvents: false)` prewarming. Radio opportunity is
applied once to initial regional Single play and the regional radio pull; it
does not scale national `radioHeat`. Production cost and comparable priors use
the scheduling month. Marketing changes awareness return at release/drop, not
spend. Artist availability applies only to
`CompetitorManager.CalculateWeeklyReleaseChance`; scouting remains unseasonal.
Venue attendance is exposed and intentionally has no consumer.

The shipping default is `marketSeasonalityEnabled = true`. Both CLI overrides
resolve before ChartManager population/prewarm; using both is rejected.

| Channel | Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Single sales | .84 | .89 | .95 | .98 | 1.03 | 1.07 | 1.10 | 1.05 | .99 | 1.02 | 1.04 | 1.04 |
| Album sales | .75 | .83 | .91 | .97 | 1.00 | 1.01 | 1.00 | .97 | .99 | 1.06 | 1.16 | 1.35 |
| Radio delta | -.04 | -.03 | -.02 | 0 | .02 | .05 | .05 | .03 | .01 | .02 | -.03 | -.06 |
| Venue attendance | .85 | .89 | .95 | 1.00 | 1.03 | 1.10 | 1.13 | 1.08 | 1.00 | 1.03 | .98 | .96 |
| Recording cost | .88 | .92 | .97 | 1.00 | 1.02 | 1.03 | 1.04 | 1.02 | 1.03 | 1.06 | 1.08 | .95 |
| Marketing efficiency | .82 | .90 | .95 | .99 | 1.02 | 1.04 | 1.03 | 1.00 | .98 | 1.08 | 1.13 | 1.06 |
| Artist availability | 1.16 | 1.11 | 1.06 | 1.02 | .99 | .95 | .92 | .94 | 1.00 | 1.00 | .95 | .90 |

Raw multiplicative sums are 12.00 and radio sums to 0.00. Effective tables are
recorded per run/year/month in `*-seasonality-monthly.csv`; service startup
checks validate table lengths, finiteness, positivity, arithmetic sums, and the
1960-69 calendar invariants.

## Exactness and determinism

`4c-disabled-1001` reproduces frozen Baseline v2:

| Stream | SHA-256 |
| --- | --- |
| market-revenue | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` |
| release-capacity | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` |

The two independent enabled seed-1001 runs, `4c-enabled-1001-a` and
`4c-enabled-1001-b`, are byte-identical for every corresponding emitted CSV.

## Calibration history and 4C-R rescope

The scalar-free candidate held decade level but seed 1002 Album units were
`1.0584x`, so the authorized Album `0.98` scalar was probed on seeds 1001/1002.
It passed decade format balance. The original three-seed checkpoint then failed
same-seed/year total units with opposing signs (seed 1002: 1962 `1.0717x`,
1964 `1.0518x`, 1967 `0.9494x`). Removing the scouting seam, as 4C required,
did not repair that mixed timing/RNG composition. No further curve or system
tuning was performed. 4C-R retains that failure and adjudicates the final
release-only candidate with its pooled-year replacement gate.

## 4C-R Checkpoint B

Final treatment runs are `4c-releaseonly-enabled-1001`, `-1002`, and `-1003`;
their controls are the preserved `4c-disabled-*` runs. All per-seed decade
ratios pass their stated ranges.

| Seed | Total units | Singles | Albums | Gross | Market net | Successful releases | Album projects | Closed Top-40 median (D/E) |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1001 | .9972 | .9994 | .9811 | .9927 | .9923 | 1.0012 | .9982 | 11 / 11 |
| 1002 | 1.0128 | 1.0138 | 1.0052 | 1.0106 | 1.0084 | .9965 | .9886 | 11 / 11 |
| 1003 | .9952 | .9987 | .9695 | .9873 | .9929 | 1.0071 | .9923 | 11 / 11 |

### Individual seed-year ratios

The columns are total units, gross, market net, and successful releases.
The former +/-5% diagnostic misses remain visible (notably seed 1002 1962,
1964, and 1967); none crosses the 4C-R catastrophic guard.

| Seed | Year | Units | Gross | Net | Releases |
| ---: | ---: | ---: | ---: | ---: | ---: |
|1001|1960|.9835|.9912|.9932|.9802|
|1001|1961|.9899|.9906|.9852|1.0038|
|1001|1962|1.0025|1.0028|.9989|1.0056|
|1001|1963|1.0309|1.0312|1.0286|1.0098|
|1001|1964|1.0225|1.0252|1.0263|1.0012|
|1001|1965|.9623|.9674|.9678|1.0058|
|1001|1966|.9912|.9806|.9815|.9972|
|1001|1967|1.0116|1.0164|1.0156|1.0076|
|1001|1968|1.0151|.9905|.9853|1.0042|
|1001|1969|.9640|.9564|.9628|.9930|
|1002|1960|1.0141|1.0159|1.0183|1.0204|
|1002|1961|.9886|.9764|.9712|.9833|
|1002|1962|1.0717|1.0587|1.0533|1.0119|
|1002|1963|1.0470|1.0403|1.0377|.9975|
|1002|1964|1.0518|1.0444|1.0415|.9908|
|1002|1965|1.0280|1.0293|1.0394|.9889|
|1002|1966|.9761|.9909|.9956|.9860|
|1002|1967|.9494|.9886|.9885|.9928|
|1002|1968|.9753|.9959|.9913|.9969|
|1002|1969|1.0269|.9966|.9829|1.0020|
|1003|1960|.9643|.9703|.9685|1.0036|
|1003|1961|1.0240|1.0199|1.0143|1.0159|
|1003|1962|.9833|.9844|.9854|1.0204|
|1003|1963|1.0064|.9986|.9972|1.0213|
|1003|1964|.9873|.9746|.9721|1.0160|
|1003|1965|1.0224|1.0028|1.0004|1.0027|
|1003|1966|1.0110|.9903|.9891|1.0036|
|1003|1967|1.0004|.9818|.9858|.9970|
|1003|1968|.9688|.9748|.9958|.9945|
|1003|1969|.9764|.9846|1.0080|1.0016|

### Pooled calendar-year ratios

| Year | Units | Gross | Net | Releases |
| ---: | ---: | ---: | ---: | ---: |
|1960|.9869|.9921|.9929|1.0013|
|1961|1.0013|.9960|.9906|1.0010|
|1962|1.0178|1.0141|1.0114|1.0127|
|1963|1.0274|1.0225|1.0204|1.0096|
|1964|1.0194|1.0132|1.0118|1.0027|
|1965|1.0045|.9997|1.0021|.9991|
|1966|.9931|.9874|.9888|.9956|
|1967|.9868|.9951|.9962|.9991|
|1968|.9857|.9866|.9909|.9985|
|1969|.9887|.9794|.9851|.9989|

All pooled ranges and catastrophic guards pass. Existing 4b chart, distance,
concentration, home-market, format-mix, crossover, and deal protections remain
applicable and no new regression was observed. No NaN/infinity, invalid
probability, or out-of-range awareness/radio was emitted. Venue-driven revenue,
sales, awareness, and cost remain zero by construction. Sales, radio,
production, and marketing each use their single authorized seam; fixed-input
inspection confirms no direct radio/marketing/venue sales factor.

## Seasonal realization (three-seed pooled enabled/disabled)

| Month | Single units | Album units | Radio | Production/event | Releases |
| ---: | ---: | ---: | ---: | ---: | ---: |
| Jan | .951 | .888 | .975 | .894 | 1.103 |
| Apr | 1.022 | .963 | 1.023 | .993 | .999 |
| Jul | 1.051 | 1.015 | 1.043 | 1.051 | .956 |
| Nov | .982 | 1.053 | .994 | 1.074 | .982 |
| Dec | .898 | 1.074 | .956 | 1.004 | .949 |

The omitted months are in the corresponding monthly CSVs. The direction checks
pass: Singles trough in January/December and crest in late spring/summer;
Albums rise from winter into Q4 and December beats disabled December; radio is
stronger in summer than December; November production cost and fixed-budget
marketing efficiency exceed January; and January releases exceed December.
First December and first full-year Album-gross crossover are both 1967 for each
measurement seed, so lead/lag is zero (report-only).

## Fresh holdout

Searches of repository audit history and `SimLogs` found no prior use of seed
2006 (third-party copyright references excluded). It was used exactly once as
`4c-holdout-disabled-2006` / `4c-holdout-enabled-2006`.

| Total units | Singles | Albums | Gross | Market net | Releases | Album projects |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1.0204 | 1.0232 | 1.0002 | 1.0143 | 1.0253 | 1.0413 | 1.0407 |

Every annual holdout ratio remains within the catastrophic guard; units range
from .9668 to 1.0907, gross .9691 to 1.0506, net .9730 to 1.0626, and releases
.9955 to 1.0659. Seasonal directions and 1967 crossover also pass.

## Commands, outputs, and smoke check

The measurement commands use the Godot 4.7 Mono console executable with
`--headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520`, the run
names above, `--seed=<seed>`, and their enabled/disabled override. Final output
anchors include `4c-releaseonly-enabled-1001-market-revenue.csv`
`14000CE476EF808D2B53B486055E594D09D0AFA48456C5908F301A6A5E78545A`,
its release-capacity stream
`A5A329A883DC88B820715D68D40E2B69369E851C156F111FA399799A292185D8`,
and holdout enabled market revenue
`91F95857B3985AA402FAD6BBCEB42F2905D786F9C4D6063C46D746D07A862F49`.

After acceptance, `4c-shipping-smoke-enabled` (no treatment flag) emitted
`enabled=true` with January Single/Album multipliers `.865383/.751207`.
`4c-shipping-smoke-disabled` emitted `enabled=false` and legacy `.90/.90`.

Limitations are intentional: no holiday-content classifier, regional/weather
variation, or venue consumer; radio has persistent feedback; marketing is
launch-only nonlinear; and release timing is endogenous.
