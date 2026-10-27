# Modest downward LP purchase-intention calibration

**Current disposition (27 October 2026): adopted as the ordinary-game baseline
at the user's explicit direction.** The earlier candidate/rejection notes below
record the sequence of decisions and are superseded by the adoption section.

26 October 2026. Continues [LpBuyerAccess.md](LpBuyerAccess.md). Before starting,
the completed buyer-access work was committed as `b7ae489` and pushed to
`https://github.com/tjones92/Label-Man.git`, branch `media-atmosphere`, as requested.

## Mechanism

`--lp-demand-scale=` is an opt-in common purchase-propensity factor, restricted
to [.9, 1]. The initial screen uses 1, .98 and .96. It applies before inventory,
capacity and integer sales allocation to:

- Store LP raw intentions, including soundtrack and promotional demand shadows.
- Autonomous front-line rack intentions, before known-buyer preferences join them.
- Autonomous budget rack intentions, before store substitutions join them.

The buyer-pool denominators stay fixed, preserving their exhaustion meaning.
Known-buyer and budget transfer rules use the already-reduced cleared store
cohort and are not multiplied a second time. No sales are rescaled after clearing.
Store purchasing capacity (`--lp-purchase-share=.48`), rack throughput, assortment,
prices and all shopper-pool scales stay fixed. `--lp-purchase-share` is a capacity
setting and is not used as a demand correction. Singles and authored club volumes
receive no direct factor; later world feedback can still affect composition,
singles, charts, retirement and label economics.

The store conversion diagnostic includes the factor so its demand explanation
and full-awareness shadow remain consistent. Factor one has an exact off path
and omits new save fields. Active saves write v35 and retain `LpDemandScale`
on the world snapshot. Resume validates the saved factor before mutating the
world; older saves imply one. The flag works for store-only worlds as well as
the combined rack/budget world.

## Prespecified experiment

The fixed reference is wider assortment, buyer access on, choice coefficient
one, turnover 45, front-line shopper scale .026, budget shopper scale .08,
budget price $1.75 and substitution .25. Each arm runs a fresh 106-week world;
complete 1961 settlement years are compared, and the 1962 tail stays partial.

1. Seed 1001: unchanged / 2% / 4% reductions (`budgetlp100`, `budgetlp098`,
   `budgetlp096`). The unchanged case must reproduce the archived buyer-access
   reference under the new assembly.
2. Select the reduced arm with the lowest mean absolute percentage error for
   the two separate 1961 channels: `(abs(stores/68-1) + abs(racks/60-1))/2`.
   Ties prefer the smaller reduction. Selection is based only on seed 1001;
   even an unimproved candidate can be replicated to test the negative result.
3. Replicate unchanged versus that candidate on held-out seeds 1002 and 1003.
   Report each channel and each paired result rather than fitting their sum.

A one-seed apparent fit does not change defaults. Independent world feedback
can make the response non-monotonic; interpolation or multiplying final totals
by the selected factor is not evidence of a historical fit.

## Reproduction and checks

Use unused tags when repeating this screen. Point `$lpPython` at a Python
installation with pandas and matplotlib; this is the path on this workstation:

```powershell
dotnet build --no-restore
$lpPython = Join-Path $env:LOCALAPPDATA 'Programs/Python/Python314/python.exe'
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-v2-verified -Compact -Weeks 13 `
  -Arms budgetlp100,budgetlp098,budgetlp096,budgetlp096pure -Seeds 1001
& $lpPython SimTools/analyze_budget_lp_products.py --tag lpdc-v2-verified
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-v2-screen -Compact -Weeks 106 `
  -Arms budgetlp100,budgetlp098,budgetlp096 -Seeds 1001 `
  -SkipChecks -ValidationTag lpdc-v2-verified
& $lpPython SimTools/analyze_lp_demand_calibration.py --tags lpdc-v2-screen
# Read candidate from lp-demand-calibration-review.json; replace <candidate> below.
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-v2-replication -Compact -Weeks 106 `
  -Arms budgetlp100,<candidate> -Seeds 1002,1003 `
  -SkipChecks -ValidationTag lpdc-v2-verified
& $lpPython SimTools/analyze_lp_demand_calibration.py `
  --tags lpdc-v2-screen lpdc-v2-replication --output-tag lpdc-v2-review --plot
# Exploratory follow-up performed after the 4% replication failed:
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-v2-secondary -Compact -Weeks 106 `
  -Arms budgetlp098 -Seeds 1002,1003 -SkipChecks -ValidationTag lpdc-v2-verified
& $lpPython SimTools/analyze_lp_demand_calibration.py `
  --tags lpdc-v2-screen lpdc-v2-replication lpdc-v2-secondary `
  --output-tag lpdc-v2-review --plot
```

Native checks cover factor validation, identity, upstream store intentions,
unscaled transfer components, serialization, and rejection of mismatched or
corrupt settings before world mutation. New v35 joint and store-only world/gzip
round trips exercise actual save/load/resume. Existing rack, budget and channel
checks remain required on the same assembly. A .96 pure-transfer stress has
zero incremental budget shoppers and near-zero autonomous front-line shoppers.
The regular short worlds and long worlds must pass capacity, buyer conservation,
product/finance/depletion and company cash checks.

Generated evidence stays in `SimLogs/`. Completed-only results are provisional.
The early `lpdc-v1-verified` attempt was stopped while correcting the conversion
audit field, before freezing the final assembly; its partial evidence is excluded.

## Results

Build passed with zero warnings/errors. `lpdc-v2-verified` completed all 11
checks and four 13-week worlds on assembly
`E1BDA3D425318265C4BD31FFF686EC4F82C1A04D68037629259654F9D356FAD7`.
The native suites passed 50 rack, 34 budget, 38 channel and 20 calibration
checks. Rack, budget, channel v33, access v34, calibration v35 joint, and
calibration v35 store-only world/gzip save/load/resume checks all passed.

All four short worlds pass the capacity, product, finance, depletion, buyer
conservation and company cash audits. The .96 pure-transfer stress records
876,307 front-line and 66,663 budget transfers; its tiny autonomous front-line
pool supplies 251 additional copies, and every budget copy matches a transfer.
Flag-off replay matches 75 archived weekly streams through week 12. The
unchanged joint baseline matches 79 archived weekly streams through week 12.
The checked assembly and evidence are under `SimLogs/lpdc-v2-verified/`.

All three seed-1001 worlds completed 106 weeks and pass the accounting audits.
The unchanged baseline matches eleven archived sales, company, rack, budget and
choice streams byte for byte across all 106 weeks. These are complete 1961
settlement-year results, in millions:

| LP intention factor | Stores | Racks | Combined | Mean channel error |
|---|---:|---:|---:|---:|
| 1.00 | 70.478 | 62.314 | 132.792 | 3.750% |
| 0.98 | 69.357 | 59.483 | 128.840 | 1.429% |
| 0.96 | 69.061 | 59.951 | 129.012 | 0.821% |

The .98 case is 1.357M above the store reference and .517M below the rack
reference. Its 59.549M raw rack intentions exceed its 59.483M sold by .066M;
20.182M shelf capacity is unused across different region/weeks. Full raw rack
intentions include failed transfer preferences and are not independent buyers.
The prespecified rule therefore selects .96 for replication. It is 1.061M
above the store reference and .049M below the rack reference on seed 1001.
Selection on this one seed is not an accepted historical fit.

The held-out replication also completed and passed the same audits:

| Seed | Factor | Stores | Racks | Mean channel error |
|---|---:|---:|---:|---:|
| 1002 | 1.00 | 68.421 | 61.031 | 1.168% |
| 1002 | 0.98 | 68.159 | 56.502 | 3.031% |
| 1002 | 0.96 | 65.728 | 57.340 | 3.887% |
| 1003 | 1.00 | 66.785 | 59.247 | 1.521% |
| 1003 | 0.98 | 67.648 | 57.660 | 2.209% |
| 1003 | 0.96 | 66.552 | 55.838 | 4.533% |

**The selected 4% reduction does not replicate.** It worsens both channel
errors on each held-out seed. Held-out mean channel error rises from 1.345% to
4.210%. Seed 1003 was already below both references before any reduction.
Unfulfilled rack intentions in the .96 worlds are only .038M and .011M, so
extra rack capacity would not close their 2.660M and 4.162M rack shortfalls
without changing the recorded world state. No demand factor is adopted.

After seeing this result, the .98 case was also run on the same two seeds as a
secondary sensitivity. Because this follow-up was chosen after the .96 result,
it is exploratory and is not an independent held-out test of a newly selected
factor. Both .98 worlds pass the same audits. They improve store errors on both
seeds, but lower rack sales enough to worsen mean channel error on each seed.
Their held-out average error is 2.620%, compared with 1.345% unchanged.

Across the three tested seeds, the unchanged worlds average 68.561M stores and
60.864M racks, with 2.147% mean absolute channel error. The .98 worlds average
68.388M and 57.882M, with 2.223% error; .96 averages 67.114M and 57.710M, with
3.080% error. These are summaries of three individual worlds, not confidence
intervals or a national historical estimate.

The relationship is not monotonic across independent histories. On seed 1002,
the .98 case sells 56.502M racks versus 57.340M under .96 and 61.031M unchanged.
It has 56.762M raw rack intentions and .260M unfulfilled; removing every cap
from those recorded intentions would still leave about 3.238M of the 60M rack
target unmet, before future behavior changes. Some unfulfilled preferences are
already counted in stores, so that upper bound is not a count of extra buyers.

Across all nine 106-week worlds, shared shelf, buyer conservation, product and
finance units, saved depletion and cash checks pass; maximum cash reconciliation
error is below 5e-10. Clubs total 44.135M in every 1961 world; singles range
from 136.194M to 137.024M. Full evidence and the reviewed chart are in
`SimLogs/lpdc-v2-review/`.

**Disposition:** retain factor 1 as the current default and keep the scale
experimental. The screened common reductions improve seed 1001, but neither
improves the combined channel error in both other seeds. A next investigation
could distinguish store-side and autonomous rack intentions across seeds, while
accounting for the store buyers routed to racks; these are not independently
additive. The two-seed .98 follow-up is exploratory, and the 1962–65 path and
the historical throughput assumption remain unvalidated.

## Five-percent rack shopper pool smoke test

The user requested a small one-seed test of a 5% increase in both rack shopper
pools, retaining the .98 purchase-intention factor. `budgetlp098pool105` changes
front-line shopper scale from .026 to .0273 and budget shopper scale from .08
to .084. Prices, substitution, buyer access, choice one, wider assortment and
turnover 45 remain fixed. This uses existing runtime settings; no game defaults
or production assembly were changed.

```powershell
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-pool105-smoke-v2 -Compact -Weeks 13 `
  -Arms budgetlp098,budgetlp098pool105 -Seeds 1001 `
  -SkipChecks -ValidationTag lpdc-v2-verified
# Choose an unused tag for a repeat. The general budget analyzer validates it:
# & $lpPython SimTools/analyze_budget_lp_products.py --tag <new-tag>
```

Both 13-week worlds completed on the same previously validated assembly
`E1BDA3D425318265C4BD31FFF686EC4F82C1A04D68037629259654F9D356FAD7`.
These are partial 1960 settlement totals, in millions; they are not annualized:

| Channel | .98 reference | .98 plus 5% rack pools | Change |
|---|---:|---:|---:|
| Stores | 2.954139 | 2.958246 | +0.139% |
| Front-line racks | 5.837765 | 6.152860 | +5.398% |
| Budget racks | 2.353487 | 2.467580 | +4.848% |
| All racks | 8.191252 | 8.620440 | +5.240% |

The increase adds 429,188 rack copies over these 13 weeks. Shelf capacity stays
15.878629M, clubs stay 11.25M, and singles change by -0.00694%. Both worlds pass
shared capacity, buyer conservation, product/finance, saved depletion and cash
checks; maximum cash reconciliation error is below 3e-11. Eleven reference
sales, company, rack, budget and choice CSV streams match the archived .98
13-week world byte for byte. The review also verifies that only the two pool
settings differ between arms.

Evidence is under `SimLogs/lpdc-pool105-smoke-v2/`, including `plan.json`,
`review_smoke.py` and `rack-pool-smoke-review.json`. The first sandbox-only
attempt, `lpdc-pool105-smoke`, failed during native Godot initialization before
any simulation output and is excluded. The completed retry ran outside the
sandbox.

This confirms a rack-sales response with little early store movement on one
seed. It does not establish the complete 1961 fit or replication across seeds.
The experiment stops at the requested small test; no default is adopted.

## Three-seed rack-pool follow-up

The user then authorized the 5% pool treatment on all three seeds through the
complete 1961 settlement year. The same production assembly and checked runtime
settings are reused. Archived .98 worlds provide the controls: seed 1001 from
`lpdc-v2-screen`, and seeds 1002/1003 from `lpdc-v2-secondary`. The analyzer
requires matching assembly hashes and verifies that only the two rack shopper
pool settings differ in each pair. It reruns the channel, buyer, product,
depletion and cash audits on all six worlds.

```powershell
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-pool105-three-seed -Compact -Weeks 106 `
  -Arms budgetlp098pool105 -Seeds 1001,1002,1003 `
  -SkipChecks -ValidationTag lpdc-v2-verified
& $lpPython SimTools/analyze_lp_rack_pool.py --tag lpdc-pool105-three-seed --plot
```

Use an unused tag when repeating. This is an exploratory comparison on seeds
already inspected during calibration; it is not an independent held-out test.
The partial 1962 tail is reported separately and never annualized. No default
is changed by running the experiment.

All three 106-week treatment worlds completed successfully. Complete 1961
settlement-year results, in millions:

| Seed | .98 stores | +5% pool stores | .98 racks | +5% pool racks | Rack lift |
|---|---:|---:|---:|---:|---:|
| 1001 | 69.357073 | 70.158644 | 59.482948 | 61.582228 | +3.529% |
| 1002 | 68.158514 | 67.945383 | 56.502191 | 63.487058 | +12.362% |
| 1003 | 67.647738 | 68.640260 | 57.659627 | 63.458327 | +10.057% |
| Three-seed mean | 68.387775 | 68.914762 | 57.881589 | 62.842538 | — |

The treatment raises racks on every seed but overshoots the 60M reference in
all three. The mean rack increase is 4.960949M (+8.57% relative to the control
mean); mean stores increase .526987M. The mean absolute channel error across
seeds rises from 2.223% to 3.068%. It worsens on seeds 1001 (1.429% to 2.906%)
and 1003 (2.209% to 3.353%), with a small improvement on seed 1002 (3.031% to
2.946%). These are means of per-world absolute errors, not errors calculated
after averaging away opposite-signed deviations.

The response includes independent world feedback and is not a constant 5%
sales multiplication. On average, front-line rack sales rise 3.598650M and
budget sales 1.362299M. Fulfilled transfers increase only .206892M across the
two product lines; pre-transfer store purchases also change. Singles average
136.565287M versus 136.588500M in controls, and authored clubs remain 44.134615M.
Shared shelf capacity stays 79.664635M in each 1961 world.

All six worlds pass the shared-capacity, buyer-conservation, product/finance,
saved-depletion and cash audits on the same assembly. Maximum cash
reconciliation error is below 5e-10. The completed review is in
`SimLogs/lpdc-pool105-three-seed/`: `comparison-plan.json`, `rack-pool-review.json`,
`rack-pool-annual.csv`, `rack-pool-paired-1961.csv` and `rack-pool-1961.png`.
The earlier two-completed-seed validation JSON is provisional and is superseded
by this complete review.

**Disposition:** the rack shopper controls have the intended direction, but
this 5% setting is too strong across the tested worlds and does not improve
the overall two-channel error. Retain the current defaults. A smaller increase
would require its own fresh-world comparison; no additional variants were run.

## Two-percent rack-pool follow-up

27 October 2026. After reviewing the 5% overshoot, the user authorized a 2%
increase in both rack shopper pools on the same three seeds. The
`budgetlp098pool102` arm sets front-line scale to .02652 and budget scale to
.0816, retaining the .98 LP intention factor, buyer access, choice one, wider
assortment, turnover 45, prices and substitution. This uses the same production
assembly and archived .98 controls as the 5% experiment; no runtime defaults
are changed. The rack-pool analyzer supports both increases and verifies the
exact declared scales against each control.

```powershell
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-pool102-three-seed -Compact -Weeks 106 `
  -Arms budgetlp098pool102 -Seeds 1001,1002,1003 `
  -SkipChecks -ValidationTag lpdc-v2-verified
& $lpPython SimTools/analyze_lp_rack_pool.py --tag lpdc-pool102-three-seed --plot
```

Use an unused tag when repeating. The proportional estimate used to choose
2% was only a starting hypothesis; the test compares fresh simulated worlds,
not interpolated sales. These seeds have already informed calibration, so this
remains exploratory. Only complete 1961 years enter the target-error comparison.

All three 106-week worlds completed successfully on the unchanged validated
assembly. Complete 1961 settlement-year totals, in millions:

| Seed | .98 stores | +2% pool stores | .98 racks | +2% pool racks | Rack change |
|---|---:|---:|---:|---:|---:|
| 1001 | 69.357073 | 68.722880 | 59.482948 | 58.799120 | -1.150% |
| 1002 | 68.158514 | 67.232319 | 56.502191 | 61.094058 | +8.127% |
| 1003 | 67.647738 | 66.705759 | 57.659627 | 60.058171 | +4.160% |
| Three-seed mean | 68.387775 | 67.553653 | 57.881589 | 59.983783 | — |

Mean racks increase 2.102194M (+3.632% relative to the control mean) and finish
.016217M below the 60M reference. Mean stores fall .834122M and finish .446347M
below the 68M reference. Combined stores/racks average 127.537436M against 128M.
These close means do not remove the individual-world deviations shown above.

The mean absolute two-channel error across worlds improves from 2.223% to
1.336%, versus 3.068% under the 5% pool increase and 2.147% for the earlier
unchanged LP-intention worlds. The paired error slightly worsens on seed 1001
(1.429% to 1.532%), but improves on seeds 1002 (3.031% to 1.476%) and 1003
(2.209% to 1.000%). The metric averages absolute errors within each world,
then across seeds; it does not cancel opposite-signed deviations in the means.

The lower rack total on seed 1001 is an observed fresh-world outcome, not a
failed application of the flags: the analyzer verifies both 2% increases and
all unchanged settings. The response includes later history and repertoire
feedback. On average front-line rack sales rise 1.811514M and budget sales rise
.290680M. Net fulfilled transfers rise only .072595M, so the .834122M store
decline is not explained by a direct equal-sized transfer. Mean live LPs fall
from 3,666.115 to 3,628.308. Singles average 136.768451M versus 136.588500M,
clubs remain 44.134615M, and shelf capacity remains 79.664635M per world.

All six paired worlds pass shared-capacity, disjoint-buyer conservation,
product/finance, saved-depletion and cash checks. Maximum cash reconciliation
error is below 5e-10. Evidence is in `SimLogs/lpdc-pool102-three-seed/`, including
`plan.json`, `comparison-plan.json`, `rack-pool-review.json`,
`rack-pool-annual.csv`, `rack-pool-paired-1961.csv` and `rack-pool-1961.png`.
The completed review supersedes the earlier provisional validation output.

**Disposition:** .98 LP intentions plus 2% larger rack shopper pools is the
closest tested candidate by three-seed mean absolute channel error. Keep it as
an experimental candidate; the user requested a comparison, so no game default
is changed. The three seeds informed its selection, and performance on fresh
seeds and the complete 1962-65 path remains untested. No additional variants
were run.

## Fresh-seed decade validation

27 October 2026. The user authorized two parallel decade runs on new seeds,
with conditional promotion to the default followed by commit/push if the
results support adoption. The fixed candidate is unchanged: .98 LP intentions,
front-line rack pool .02652, budget pool .0816, wider assortment, known-buyer
access/choice one, turnover 45 and the same price/substitution settings.

Seeds 27102701 and 27102702 were selected after checking the existing plan and
run-name inventory for prior use. Both started fresh with a requested 530 weeks
on the validated assembly. That horizon was a harness setup error: the game
ends on 31 December 1969. No calibration parameter was retuned during the
holdout. The original command is preserved here as failed-run provenance, not
as the command to repeat:

```powershell
./SimTools/RunRackJobberProbe.ps1 -RunTag lpdc-pool102-decade-holdout -Compact `
  -Weeks 530 -Arms budgetlp098pool102 -Seeds 27102701,27102702 -MaxParallel 2 `
  -SkipChecks -ValidationTag lpdc-v2-verified
```

The review verifies the exact candidate flags and assembly, both successful
530-week completions, ten complete years per seed, finite positive retail and
repertoire series, shared shelf capacity, buyer conservation, product/finance
and saved-depletion identities, front-line financial units and company cash.
Historical comparisons use the existing separate store/rack references for
1961-1965. There are no claimed historical channel targets for 1966-1969;
those years test the simulation's path and stability. Technical accounting
checks alone are not grounds for historical adoption. Review the individual
seed/year errors and the later path before deciding whether the runs look good.

At the user's interim request, the same completed worlds will also provide an
observational repeat-hit readout using the existing annual identity, carryover
and owner-group rules. This adds no treatment or randomness. The historical
repeat comparison is restricted to the existing 1960-1964 reconstructions.

Generated evidence belongs in `SimLogs/lpdc-pool102-decade-holdout/`.

### End-date error and the user's retained-seed decision

Seed 27102702 completed 522 recorded chart/settlement observations: ten full
1960-1969 calendar years (1965 has 53 weeks) and one partial 1970 observation.
On the next requested iteration, the harness raised `The game ended before
another chart week could be simulated`. It flushed its sales, rack, buyer,
budget, financial and census ledgers, then exited 1. The original plan retains
`passed: false`; its observation-window ledger retains `completed: false`.
This is not a successful native 530-week run.

The runner's failure cleanup stopped seed 27102701 near October 1969, losing
its buffered rack ledgers. A corrected 521-week replacement was started under
`lpdc-pool102-decade-recovery`. The user then explicitly directed that the
replacement be stopped and only the retained seed used. The replacement was
terminated and verified absent; both interrupted versions of 27102701 are
excluded. There are no active simulation runs from this experiment.

`RunRackJobberProbe.ps1` now rejects requests above 521 weeks before launching
a process. A future fresh two-seed decade uses an unused tag and `-Weeks 521`.
To review this retained evidence instead:

```powershell
& $lpPython SimTools/analyze_lp_decade_holdout.py --retained-seed 27102702 --plot
```

The explicit exception accepts only this declared retained seed, verifies that
the sole native error is the known end-date exception, and requires all ten
full settlement years. The ordinary analyzer still rejects the failed matrix
without the exception. Missing/unrecorded peers cannot use it. The two rejection
checks are in `endpoint-adjudication-checks.json`. The final review keeps both
`nativeMatrixComplete` and `matrixComplete` false, records the endpoint exception
and single reviewed seed, and separately reports the accounting checks. No
original manifest, log or CSV is relabeled or rewritten.

### Retained-decade results and disposition

All retained sales pass shared shelf, buyer-conservation, product/finance,
saved-depletion, front-line financial-unit and finite-series audits. Maximum
cash reconciliation error is 4.657e-10. The census reports zero missing records.
These accounting results do not cure the native harness failure or establish
historical fit.

| Complete year | Stores M | Store reference M | Racks M | Rack reference M | Mean absolute channel error |
|---|---:|---:|---:|---:|---:|
| 1961 | 67.161 | 68 | 62.470 | 60 | 2.675% |
| 1962 | 74.643 | 70 | 72.877 | 76 | 5.371% |
| 1963 | 76.817 | 66 | 80.284 | 79 | 9.007% |
| 1964 | 78.746 | 68 | 83.234 | 86 | 9.510% |
| 1965 | 77.836 | 76 | 93.115 | 106 | 7.286% |

The 1961-1965 mean absolute channel error is 6.770%. Stores are 16.389% high in
1963 and 15.803% high in 1964; racks are 12.156% low in 1965. The 1961 fit is
reasonably close, but this single retained decade does not support calling the
later channel path calibrated. It is not a paired comparison with untreated
decade worlds, so these misses do not establish that the candidate caused the
drift or is worse than the previous default.

There is no retail collapse in the later observations: 1969 stores/racks total
141.662M/166.403M, singles 136.907M, and mean live LPs about 7,000. These are
stability observations, not matches to unprovided 1966-1969 outlet references.

The observational repeat-hit readout gives 18.2 annual acts with 2+ Top 10s
over 1960-1964, against 17.8 in the recovered history; 3+ acts are 8.0 versus
7.2. Distribution is still uneven: Big Five/studio/field repeaters average
10.4/2.0/5.8. This reinforces the earlier distinction between overall repeat
frequency and the label-opportunity imbalance. It is one world, not a new
policy experiment. Definitions retain carryovers and first-Top-10 seller
assignment; historical artist-credit/record-side caveats still apply.

The user clarified that conditional default promotion meant ordinary gameplay
with no flags, not only an audit profile. A proposed coherent profile was
staged under ignored `SimLogs/` but never activated. **Disposition: retain the
LP settings as a candidate, do not promote the ordinary-game default on this
evidence.** The requested commit/push was conditional on successful validation;
no default change, commit or push was performed. No further simulations are
authorized by this report or running.

Final outputs: `lp-decade-review.json`, `lp-decade-annual.csv`,
`lp-decade-hit-acts.csv` and `lp-decade-holdout.png`. The provisional endpoint
review is superseded by the explicit single-seed review, not by a completed
two-seed validation.

### Follow-up: late LP growth and the mid-decade singles hump

The user asked whether late LP growth could simply be the intended market
transition, and why singles peak in 1966. Read-only decomposition of the retained
world is in `format-transition-and-singles-spillover.csv` and `.json` beside the
decade review. No simulation or gameplay change was made for this diagnosis.

The upward LP direction is expected from existing authored mechanisms:
`MarketRegion` raises album affinity and purchase willingness along its era
curve, the legacy store LP capacity expands, rack reach increases, and the live
catalogue grows. The additional LP purchase-capacity treatment contributes zero
extra units in 1968 and 1969 because the legacy LP channel already exceeds its
floor. Late growth is therefore not by itself grounds for rejection. The
earlier disposition concerned measured channel misses, not the mere existence
of an upward late-decade line.

In the retained world, LP share of store-plus-rack record units rises from
48.680% in 1961 to 54.553% in 1966, 65.855% in 1968 and 69.233% in 1969. These
shares exclude both record clubs and jukebox purchases. Including clubs yields
69.519% in 1968; do not compare the two scopes interchangeably.

The later [Billboard 1969-1970 directory](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1969/Billboard%201969-08-30-Buyer.pdf),
U.S. Record Survey pp. 9-11, distinguishes strong dollar dominance from a much
smaller unit majority. For 1968 it gives LPs 84.4% of record dollars and 51.7%
of total industry units. Its retail-only tables give 54M non-rack LPs and 93M
rack LPs, versus 42M/84M singles: a derived retail LP unit share of 53.846%,
against this world's 65.855%. This edition explicitly revises earlier estimates:
its 1966 LP total is 180M, while the 1967-1968 edition reports 262M for 1966.
Reconcile editions, methods and counting scope before appending these values
to the existing 1961-1965 calibration series. Later references do exist; they
were not included in the original five-year analyzer. Their discovery is not
permission to overwrite historical evidence or refit the model silently.

The singles hump has an exact recorded decomposition, in millions:

| Year | Local singles | Singles from regional spillover | Total singles |
|---|---:|---:|---:|
| 1964 | 134.709 | 3.867 | 138.577 |
| 1965 (53 weeks) | 137.300 | 12.481 | 149.780 |
| 1966 | 134.709 | 16.105 | 150.814 |
| 1967 | 134.709 | 9.428 | 144.137 |
| 1968 | 134.709 | 4.489 | 139.199 |
| 1969 | 134.709 | 2.197 | 136.907 |

Every region/week exhausts its local Single format budget. The entire 12.237861M
increase in singles between equal-length 1964 and 1966 is additional spillover,
not a larger local Single budget. The LP-only addition's before/after Single
units are identical in every row. The pre-existing mixed spillover pass exports
unused legacy purchase capacity and can allocate the imports to residual Single
intent even when the recipient's local Single budget is full. With all local
Single budgets exhausted, the unused legacy capacity comes from the LP side.
Later, stronger LP absorption leaves less exportable capacity, and the hump
fades. The separate LP-only capacity addition cannot be exported and is not the
direct source of these Single units. The 53-week 1965 calendar contributes one
extra week, but does not explain the equal-length 1964/1966 difference.

This identifies a capacity-allocation mechanism worth examining; it does not
establish that the spillover behavior reproduces historical consumer behavior.
No cross-format spillover policy was changed, no further native run was started,
and the conditional default promotion remains unperformed.

### Adoption as the ordinary-game baseline (27 October 2026)

The user explicitly selected this bundle as the best available economic baseline
despite its known issues and requested default promotion, commit and push. This
supersedes the earlier conditional hold; it does not turn the retained endpoint
exception into a successful two-seed native validation.

`Systems/SimulationArguments.cs` defines `lp-retail-v1`: all 57 simulation flags
from retained seed 27102702, in the same order, excluding seed, horizon, output
paths and telemetry-only flags. In particular, it retains LP demand .98,
front-line rack shoppers .02652, budget shoppers .0816, turnover 45, mixed
assortment, known-buyer access, store choice 1, LP programme scale 2 and purchase
share .48 together with the tested release/audience systems. Production flag
readers share these effective arguments, including static initialization and
the pre-population seed bootstrap. Ordinary gameplay needs no flags.

Headless audits keep explicit arguments for reproducibility. They can select
the gameplay baseline with `--default-simulation-profile`; explicit parameter
values override corresponding defaults. `--legacy-simulation-profile` bypasses
the bundle for earlier comparisons and saves requiring the old configuration.
The two profile selectors cannot be combined. Saved active LP factors remain
validated before world mutation; this adoption does not silently convert old
saves into a different economy.

Remaining work: investigate cross-format regional spillover behind the singles
hump, excessive similarity of regional charts, the late LP channel trajectory,
and the studio-label share of repeat hits. Regional chart overlap concerns the
same titles winning across markets; it has not been established as the cause of
the capacity-allocation hump. No mechanism was altered to flatten that hump.

The user's additional Billboard table reports **dollar sales**, not unit counts:
LP dollars 783/874/949 million and singles 176/177/175 million for 1966-1968
(the last column's year is clarified by its 1968 headings). Keep those separate
from the guide's LP outlet-unit table and record-release counts. Earlier and
later Billboard estimate vintages still require reconciliation.

Adoption verification: `dotnet build 'Label Man.sln' --no-restore` passed with
zero warnings/errors; the native profile checks passed all 13 assertions; LP
calibration checks passed all 20 assertions. A seed-1001 two-week native run
using only `--default-simulation-profile` plus its seed/horizon passed byte-exact
world save/load, gzip file IO and an additional resumed release-programme week.
Python analyzers compiled, the PowerShell probe parsed and `git diff --check`
passed. These are startup/mechanical smoke checks, not another decade holdout.
