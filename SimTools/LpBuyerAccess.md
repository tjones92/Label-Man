# Known-buyer rack access

The subsequent purchase-intention calibration is in
[LpDemandCalibration.md](LpDemandCalibration.md).

26 October 2026. Continues [LpChannelFactorial.md](LpChannelFactorial.md).
The user authorized testing access for existing store buyers before changing
autonomous shopper demand. This is an opt-in experiment, not an adopted fit.

## Mechanism

`--rack-buyer-access` requires a positive `--rack-store-choice=` together with
the existing budget/rack prerequisites. For an already-selected front-line title:

`planned preferences = floor(cleared title store purchases × rack reach × choice share)`.

The previous rule additionally multiplies by that title's discovery/display
exposure. Access removes only this final factor for known buyers. It does not
change autonomous shoppers' exposure, conversion, buyer pools, rack reach,
title eligibility, assortment selection, prices or budget substitution. An
unstocked title cannot receive preferences. Rack reach remains the existing
authored curve; the coefficient and the access interpretation are hypotheses.

The existing disjoint buyer cohorts, one shared regional shelf, fulfilled-only
store removals and saved store-origin exhaustion remain in use. Failed rack
preferences remain store purchases. They cannot also choose a budget copy that
week. Changes in store charting, retirement, label cash and later repertoire
can still affect annual outcomes.

Saves using access write v34 and persist `RackBuyerAccess` in the budget ledger.
Resume requires the same access setting, choice share, assortment and throughput.
Display-rule saves continue to write v33; the new false field is omitted, and
older budget saves remain v32 when the prior treatments are off.

## Matched screen

All eight cells retain wider assortment (25 front-line hits, up to 50 front-line
catalogue titles and 25 budget offers), seed 1001, front-line shopper scale .026,
budget scale .08, budget price $1.75 and budget substitution .25.

Cross display rule/access with choice share .5/1 and turnover 30/45. Turnover 45
is the existing capacity sensitivity; it is not an empirical throughput estimate.
Each arm starts a fresh world. The 106-week screen covers complete 1960/1961
settlement years and a partial 1962 tail. Do not annualize the tail.
Choose unused run tags when repeating the screen.

```powershell
dotnet build --no-restore
$accessArms = @('budgetmixchoice','budgetmixchoice100','budgetmixaccess',
  'budgetmixaccess100','budgetmixchoice45','budgetmixchoice100t45',
  'budgetmixaccess45','budgetmixaccess100t45')
./SimTools/RunRackJobberProbe.ps1 -RunTag lpa-v1-verified -Compact -Weeks 13 `
  -Arms ($accessArms + 'budgetaccesspure') -Seeds 1001
python SimTools/analyze_budget_lp_products.py --tag lpa-v1-verified
./SimTools/RunRackJobberProbe.ps1 -RunTag lpa-v1-final -Compact -Weeks 106 `
  -Arms $accessArms -Seeds 1001 -SkipChecks -ValidationTag lpa-v1-verified
python SimTools/analyze_lp_buyer_access.py --tag lpa-v1-final --plot-year 1961
```

The runner requires complete native/save/load/smoke evidence from the same
assembly before reusing checks. The access round trip also exercises choice one
and turnover 45. `budgetaccesspure` uses near-zero autonomous front-line
shoppers, zero incremental budget shoppers and substitution/choice one to stress
fulfilled transfers and cohort conservation under the access rule.

The analyzer validates frozen settings and the existing capacity, product,
finance, depletion, disjoint-cohort and cash identities. It reports matched
access effects at each share/throughput, share effects at each rule/throughput,
and throughput effects at each rule/share. Report stores and all racks separately,
alongside planned/fulfilled transfers, raw demand, autonomous sales, retirement
and singles. Raw rack intentions include failed transfers whose buyers remain
counted in stores; they are not independent additional national purchases.

Generated evidence stays in `SimLogs/`. A completed-only review is explicitly
provisional and cannot produce the final plot. No shopper scale or default is
changed by this experiment.

## Validation

Build passed with zero warnings/errors. `lpa-v1-verified` completed all eight
ordinary 13-week cells plus `budgetaccesspure` on assembly
`F097D6624DCC862AB3783266997AE255839374AB91703D84F36286102CDAB79D`.
Its 50 rack, 34 budget and 38 channel native checks pass. Rack, budget,
display-choice v33 and access v34 full-world/gzip save/load/resume checks pass.
Every short world passes shared capacity, product/finance/depletion, buyer
conservation and company cash reconciliation. The access stress transfers
912,238 front-line and 69,062 budget purchases; all budget units are transfers.
Its tiny autonomous front-line pool sells a further 261 copies, reported
separately rather than classified as transfers.

The flag-off replay matches 75 archived weekly streams through week 12. The
existing display-choice arm matches 79 archived weekly streams through week 12.
Evidence and a frozen assembly copy are under `SimLogs/lpa-v1-verified/`.
The sandbox-only attempt `lpa-v1-checks` crashed in native Godot before project
initialization; no simulation results from that attempt are used.

## Completed 1961 results

All eight cells completed 106 weeks on seed 1001 and the same validated assembly.
The comparison uses the complete 52-week 1961 settlement year. Units are millions:

| Rule | Choice share | Turnover | Stores | All racks | Raw rack intentions | Fulfilled front-line transfers |
|---|---:|---:|---:|---:|---:|---:|
| Display | .5 | 30 | 72.800 | 50.400 | 55.100 | 2.624 |
| Display | 1 | 30 | 72.812 | 51.309 | 57.194 | 5.437 |
| Access | .5 | 30 | 74.929 | 51.655 | 59.383 | 3.622 |
| Access | 1 | 30 | 71.195 | 51.604 | 58.882 | 7.182 |
| Display | .5 | 45 | 74.478 | 57.188 | 57.217 | 3.102 |
| Display | 1 | 45 | 71.645 | 54.617 | 54.628 | 6.022 |
| Access | .5 | 45 | 73.262 | 56.845 | 56.907 | 4.173 |
| Access | 1 | 45 | **70.478** | **62.314** | **62.808** | **8.285** |
| Historical outlet reference | — | — | 68 | 60 | — | — |

At turnover 30, access at choice one lowers stores 1.605M and increases racks 1.203M relative
to the original display/.5 joint case. It remains 3.195M above stores and
8.396M below racks. Its raw rack intentions exceed sales by 7.278M, while
1.506M of the national shelf remains unused across different region/weeks.
Its 1.066M unfulfilled front-line preferences stay counted in stores; do not
add those preferences again as independent new national purchases.

At turnover 30, the effects depend on the coefficient. At choice .5, access increases stores
2.129M despite an additional 1.003M fulfilled transfers across both products:
pre-switch store intentions increase 3.132M. At choice one, access lowers
stores 1.617M relative to display/one. A larger direct transfer cohort is not
an equal net reduction in annual stores.

The original display/.5 baseline's eleven complete sales, company, rack,
budget and choice streams match `lpc-v4-final-budgetmixchoice-default-1001-106`
byte for byte across all 106 weeks (`baseline-full-replay.json`). All eight
long worlds pass the analyzer's capacity, product/finance/depletion, buyer
conservation and company cash checks. Maximum cash reconciliation error is
below 2.4e-10. Final evidence is in `SimLogs/lpa-v1-final/`: `plan.json`,
`buyer-access-review.json`, `buyer-access-annual.csv`, `buyer-access-effects.csv`
and `lp-buyer-access-1961.png`. Earlier completed-only files remain provisional.

## Interpretation

**Access at choice one plus turnover 45 removes the rack shortfall without
increasing either shopper scale.** Relative to the original joint display/.5/
turnover-30 case, stores fall 2.323M and racks rise 11.914M. The remaining errors
are +2.478M stores and +2.314M racks. Stores plus racks total 132.792M against
128M: both channels now overshoot. This is the closest joint channel result
among these eight cells, but it is still a trial rather than an accepted fit.

The channel mix is especially close: racks are 46.926% of store-plus-rack LPs,
against 46.875% in the historical reference. Combined volume is 3.744% high;
stores are 3.644% high and racks 3.857% high. If this pattern repeats across
seeds, the remaining issue resembles a modest LP volume-level excess more
than a large channel-share error. Clubs are excluded from this share calculation.

The earlier 55.100M raw rack intentions were conditional on the previous rule
and world history. The access/one/45 world records 62.808M. It sells 62.314M,
leaving .494M of unfulfilled intentions and 17.350M of national shelf unused
across region/weeks. Only .072M front-line preferences fail to clear. Extra
capacity alone therefore has little remaining direct store-switching potential
in this world and would increase the rack overshoot.

The matched effects are not interchangeable:

| Effect of access versus display, at the same share and turnover | Store change | Rack change |
|---|---:|---:|
| Choice .5, turnover 30 | +2.129 | +1.255 |
| Choice 1, turnover 30 | -1.617 | +.294 |
| Choice .5, turnover 45 | -1.216 | -.344 |
| Choice 1, turnover 45 | -1.167 | +7.698 |

At choice one/turnover 45, access adds 2.263M direct front-line transfers,
5.790M autonomous front-line rack sales and loses .355M budget rack sales
relative to display/one/45. Much of its rack gain reflects later demand and
repertoire feedback, not simply reassigning existing purchases. Relative to
the original joint case, pre-switch store intentions rise 3.321M while total
fulfilled transfers rise 5.643M, leaving the observed 2.323M net store reduction.

Mean live LP counts remain approximately 3,630–3,735 across the eight cells;
LP retirements are 915–957 and singles 136.194–136.589M. Clubs remain 44.135M
on the settlement calendar. There is no large availability or singles collapse,
but composition and subsequent history still materially affect the results.

**Do not increase aggregate demand on the basis of the old rack shortfall.**
This experiment demonstrates sufficient rack demand with unchanged shopper
scales. The next step is to repeat the promising access/one case and its matched
controls across seeds before choosing a throughput or demand correction. A
modest downward LP demand-level screen would then be a candidate if the close
channel mix repeats. The rack-count/throughput assumptions still need an
independent historical review; turnover 45 is only a sensitivity. Neither linear interpolation of
these independent worlds nor multiplying the final sales by a fitted scalar
would establish a historical calibration. All new behavior remains opt-in;
game defaults and shopper scales are unchanged.
