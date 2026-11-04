# LP programme and purchase capacity: first factorial screen

25 October 2026. Implements the user's requested LP programme-only,
purchase-capacity-only and joint comparison, keeping authored single purchase
capacity fixed. Exploratory, opt-in; no working-bundle adoption.

Base: judged v7 (`label-project-v7-judged` flags), without earned roster
capacity, per-act singles cadence, forecast-realization or capital-risk probes.
Seed 1001, 106 weeks, all four arms on one assembly. Calendar-year release
counts use the census year (release ticks 1-53 / 54-105); sales use settlement
years (ticks 1-52 / 53-104). LP titles and units are separate measurements.

| Arm | Programme | Purchasing |
|---|---|---|
| control | unchanged | unchanged |
| programme | `--lp-programme-scale=2` | unchanged |
| purchase | unchanged | `--lp-purchase-share=0.48` |
| both | scale 2 | share .48 |

Programme scale multiplies the AI LP work/issue rate, including Majors and the
other tiers' LP envelope. Nominal singles rates are unchanged. Player capacity,
rosters, artist spacing, annual artist LP limits, material gates and company
promotion/cash limits are unchanged. More LP intentions need not become more
issues, and sharing company resources can affect actual singles releases.
Scale 2 is a sensitivity scenario, not a sourced programme for every company.

The purchase setting specifies an authored capacity share, not an enforced
sales share. Desired LP channel capacity is base single capacity times
.48 / .52, bounded below by the existing LP capacity. Only the increment is
added. It is allocated to serviceable LP residual demand after the unchanged
local and mixed regional-spillover passes. Spare extra LP capacity cannot buy
singles or be exported. Existing single allocation, single local-budget
formula, base capacity, mixed export budget and mixed import limit remain the
legacy pass's values for a given state. The LP addition is asserted to leave
that state's single units exactly unchanged. Other arms can still change
single supply, demand and income over time; this is not a forced sales-unit
equality between divergent worlds. Legacy mixed spillover remains shared.

No album demand, forecast, price or royalty multiplier is added. Extra purchases
still require demand, stock and distribution. The observed LP share can remain
below 48%. Existing channel capacity is never withdrawn if it exceeds the
scenario target in a later year. No new RNG draws or world save fields.

The separate `lp-capacity.csv` records legacy and incremental LP capacity,
extra units sold, authored single capacity and before/after single sales, while
preserving legacy CSV schemas. `RunLpCapacityScreen.ps1` performs native checks
and a full-world combined-arm save/load round-trip before running the factorial.
It may run two audit worlds concurrently, with distinct CSV, console and engine
paths and capped managed heaps. The audit scene has no UI/autosaves; checkpoint
and world-save flags are not supplied to the concurrent runs. Save checks run
alone. All input flags and the assembly hash are saved.

`analyze_lp_capacity_screen.py` checks same-state single isolation, common base
single and legacy LP capacity per region/week, no mixed-spillover budget leak,
inventory/settlement/company-cash reconciliation and a full-year control replay.
It reports calendar-year counts and units, the programme x purchase interaction,
LP intentions/cancellations, starting-cohort liquidity and profit, mature retired
release losses, and LP chart genre shares. Repeated gate encounters are not
independent lost releases. Conditional loss samples omit active outcomes.

Reference sizes from the supplied Billboard scans: 3,112 / 2,927 LP releases in
1960 / 1961; in 1961 stores plus racks buy 128M LPs and 132M singles. LP retail
unit share stays approximately 47-49% through 1965. Jukebox-operator and record-
club purchases are outside the game's current purchase channels. Catalogue
version/reissue counting still needs scope confirmation before release totals
become hard title-count acceptance gates. The four arms diagnose capacity;
neither a historical loss probability nor a historical risk preference is fitted.

The capital-risk gate stays off in this factorial so it cannot be mistaken for
an LP-capacity effect. A one-seed result is a mechanism screen, not multi-seed
adoption evidence.

## Completed screen: releases can catch up before LP sales do

All four 106-week arms completed on seed 1001. Calendar 1961:

| Measurement | Control | Programme only | Purchase only | Both | Billboard reference |
|---|---:|---:|---:|---:|---:|
| LP releases | 1,739 | 2,994 | 1,720 | 3,032 | 2,927 |
| LP units, M | 53.15 | 63.73 | 54.46 | 74.05 | 128 |
| LP share of single + LP units | 26.81% | 31.72% | 27.33% | 35.05% | 49.23% |
| Raw LP demand, M | 60.23 | 83.94 | 60.69 | 82.38 | |
| Serviceable LP demand, M | 54.31 | 78.48 | 54.46 | 74.05 | |
| Authored LP channel capacity, M | 67.02 | 67.02 | 139.27 | 139.27 | |
| Single releases | 3,660 | 3,668 | 3,682 | 3,675 | 6,036 |
| Single units, M | 145.11 | 137.18 | 144.83 | 137.23 | 132 |
| Cumulative closed labels at year end | 87 | 79 | 88 | 74 | |

Programme capacity is material for LP titles: doubling it takes 1961 issues to
102.3% of the historical count; the joint arm reaches 103.6%. The first year
still ramps up: joint 1960 LP issues are 2,203 against 3,112 (70.8%), with 41.95M
LP units against the control's 31.57M. This is not evidence for doubling every
firm's permanent programme or matching historical catalogue scope.

Purchase capacity alone adds only 1.31M LP units in 1961. With more LP work
present, the same purchase treatment adds 10.32M versus programme-only. The
factorial interaction is +9.01M LP units: the two capacities complement each
other. The joint arm gains 20.90M units over control, but reaches only 57.9% of
the historical LP unit target. Its incremental LP pass directly clears 10.39M
units; that is a different measurement from the between-world treatment effect.

The joint arm fulfills **all 74.05M serviceable LP demand**. Raw demand is only
82.38M, so even perfect stock/distribution cannot produce the missing 53.95M
units from current demand. Increasing the purchase-capacity parameter again
cannot solve this result. All arms start with just 144 LPs; the joint arm has
4,432 active LPs at the end of 1961. Opening catalogue depth, retention and
age-dependent demand therefore need diagnosis alongside new-issue demand.
The annual units/new-issue quotient (joint 24.4k versus historical 43.7k)
includes catalogue sales; it is not a cohort's lifetime yield. Historical
release totals alone do not establish how many old titles should remain in
print. A larger catalogue or demand multiplier is not adopted here.

## Fixed single capacity, but lower realized single spillover

Authored single capacity is exactly 150,874,412 units in each sales year in
every arm. The LP increment changes zero same-state single allocations across
all 2,968 region/weeks. It also leaves the legacy mixed export/import limits
unchanged. The requested direct capacity isolation passes.

Realized single sales nevertheless fall 5.47% with programme-only and 5.43%
with both in 1961. Each arm clears the same 134.71M local single units. What
disappears is legacy mixed spillover: control sells 10.40M singles through it,
programme-only 2.47M and joint 2.52M. More LP demand uses spare legacy LP budget
that previously spilled into singles. The new LP-only increment is applied
after that pass and cannot restore its single transfers. Thus the earlier
plan's +/-3% realized-single-unit guard does **not** pass, although authored
capacity remains fixed. Changing this legacy cross-format channel rule would
be a separate intervention.

Single issue counts remain about 61% of history. More LP cash by itself does
not provide the head firms with more single programme slots, roster capacity
or material; those interventions remain off in this screen.

## Head-company liquidity with LP income present

1961 company statements, control versus joint (millions of model dollars):

| Fixed opening cohort | Pretax earnings, control -> joint | Closing cash, control -> joint | Cumulative closures, control -> joint |
|---|---:|---:|---:|
| Big Five | 37.96 -> 44.63 | 27.54 -> 32.60 | 0 -> 0 |
| Six studio labels | 1.14 -> 2.37 | 1.95 -> 2.66 | 0 -> 0 |
| Other opening MidTier | 2.44 -> 3.75 | 3.51 -> 4.08 | 12 -> 14 |
| Opening Independent | 12.32 -> 17.59 | 13.39 -> 18.06 | 43 -> 35 |
| Opening Small | 5.56 -> 8.45 | 3.25 -> 4.43 | 19 -> 15 |
| Opening Boutique | 6.95 -> 8.60 | 7.70 -> 9.48 | 10 -> 7 |

LP income improves aggregate profits and cash. The Big Five were already
well funded; their single issues do not rise (598 -> 592). Individual smaller
head firms still differ substantially. December closing balances, $k:

| Firm | Cash, control -> joint | Cash less corporate hold, control -> joint |
|---|---:|---:|
| King | 226.7 -> 130.6 | 86.9 -> 7.4 |
| MGM | 294.4 -> 702.5 | 67.8 -> 78.7 |
| ABC-Paramount | 202.7 -> 347.7 | 71.2 -> 104.4 |
| Warner Bros. | 525.0 -> 660.4 | 53.7 -> 144.8 |
| United Artists | 346.6 -> 405.6 | 82.9 -> -159.5 |
| Dot | 380.4 -> 421.3 | 68.0 -> -158.6 |

These are paired named firms, not a claim that LP income lowers any firm's
cash mechanically: the whole programme and subsequent trading path change.
The six-studio median outstanding receivables rise from $101.2k to $153.4k and
plant payables from $24.6k to $52.6k. More accrual earnings need not mean more
cash available for a signing. Cash less the corporate protected hold is a
diagnostic, **not** the programme/A&R budget or an exact signing-affordability
calculation. Negative values mean the firm is below its hold, not negative
bank cash. Group medians also have changing survivor composition.

The earlier earned-capacity/W17 signing refusals are not re-created here,
because those switches are off for the factorial. This result establishes the
LP-funded financial baseline for a subsequent matched signing-liquidity test;
it does not establish that the former advance gate now clears.

## Tail risk with LP income present

For singles launched in 1961 with at least 26 weeks of follow-up and already
retired by that year's close:

| Launch tier | Retired sample, control -> joint | Losing share, control -> joint | Median record net, control -> joint | Mean record net, control -> joint |
|---|---:|---:|---:|---:|
| Independent | 459 -> 435 | 45.53% -> 46.21% | $273 -> $237 | $9,598 -> $9,427 |
| Small | 309 -> 292 | 60.19% -> 61.30% | -$173 -> -$388 | $2,736 -> $3,435 |
| Boutique | 226 -> 220 | 45.13% -> 46.36% | $203 -> $197 | $9,279 -> $6,513 |

Small labels still face a majority-losing sample with a positive mean. Added
LP income increases their median closing cash from $11.60k to $14.24k, while
median cash less corporate hold rises only from $5.97k to $6.40k. Their cohort's
net owner withdrawals also rise from $3.83M to $5.69M. Thus neither higher
mean earnings nor higher company cash alone identifies the chance of surviving
another loss. The LP treatment makes many tail companies richer without
removing single losses or supplying a calibrated risk preference.

These outcomes are conditional on retirement, omit still-active records and
use the legacy record net before marketing. They are not population loss
probabilities, lifetime seller profits after master transfers, or a historical
80-90% loss-rate comparison. LP loss samples are especially immature: the joint
arm has only 14 qualifying retired LPs across these three tiers, all positive.
They cannot establish safe LP returns. The consistent capital-risk distribution
remains available as a separate opt-in probe; this factorial neither fits its
loss shape nor changes risk tolerance. A future risk-policy comparison must use
the same LP baseline in each arm and include cash timing, corporate commitments
and still-active outcome censoring.

## Validation and disposition

- Build: zero warnings/errors; 24 native LP checks pass, including parser,
  format isolation, no forced sales and fixed mixed-transfer limits.
- Full-world joint-treatment save/load round-trip passes over 13 weeks,
  including programme resumption.
- All four 106-week runs finish with exit 0 on one assembly. The control
  reproduces all 73 legacy weekly CSV streams through week 105 exactly.
- No inventory or allocation violations; format sales reconcile to settlement.
  Maximum company cash reconciliation error is below $0.000000001.
- One seed only. No causal inference from survivor-only medians, statistical
  significance claim, calibrated historical risk claim or default adoption.

Results: `SimLogs/lpc-v1/review/` (`annual.csv`, `effects.csv`, `finance.csv`,
`head-companies.csv`, `losses.csv`, `pipeline.csv`, `lp-chart-genres.csv`,
`validation.csv`, `metadata.json`). Check logs and plan:
`SimLogs/lpc-v1/`; arm manifests: `SimLogs/lpc-v1-{control,programme,purchase,both}/`.
Assembly SHA256: `F620933FAD6AAB87052E84C1882A48CA9A0005FDFE7B3BD81ABABF2F85810C33`.
The runner and analyser preserve the inputs and can repeat the screen under a
new run tag. No calibration defaults or saved working-bundle flags change.

The next mechanism to resolve is LP catalogue/demand depth and the legacy
cross-format spillover rule. Then a matched liquidity/risk comparison can use
that LP income consistently, rather than tuning head advances or tail risk
tolerance against the underfunded LP economy.
