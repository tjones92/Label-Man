# M5 Album Catalog Cohort Analysis Handoff

Status: **PRESERVED M5 PARTIAL / EXISTING-DATA ANALYSIS AUTHORIZED / NO SIMULATION AUTHORIZED**

Date: 2026-07-18

This handoff follows the frozen-source M5 failure recorded in
`SimTools/ArtistPopulationLifecycleAudit.md`. The seed-1001 candidate stopped
after 469 completed ticks because completed-1968 gross revenue was
`297,153,766.647076 / 224,777,772.118624 = 1.321989x`, above the catastrophic
inclusive `[0.70,1.30]` envelope. The run reached the completed-1968 checkpoint,
but it did not complete 1969 or the decade.

The immediate task is retrospective analysis of the preserved data. Determine
whether the late Album excess is concentrated in new releases, the 26-51 week
catalog, or the 52+ week catalog, and whether the concentration appears first in
raw demand, serviceability, market clearing, or catalog retention. Use the
result to decide whether existing evidence is sufficient or whether a new
observational run must be proposed.

This handoff does **not** authorize a simulation, gameplay change, telemetry
change, parameter sweep, replacement control, additional seed, or holdout.

## 1. Frozen inputs and preservation boundary

Candidate prefix:

```text
SimLogs/d6-bounded-spillover-75-decade-enabled-1001
```

Retained control prefix:

```text
SimLogs/d6-transition-envelope-decade-control-1001
```

The earlier partial 104-week family remains archived at:

```text
SimLogs/Archive/d6-bounded-spillover-75-decade-enabled-1001-partial-104w-20260718
```

Do not modify, rename, truncate, normalize, or replace any retained CSV. Do not
restore the deliberately deleted
`SimTools/analyze-market-clearing-format-memory.mjs`.

The current M5 family contains a week-470 / 1969-01-03 settlement boundary
associated with completed-1968 fail-fast adjudication. The cohort analysis must
use calendar years 1967 and 1968 only. Do not include week 470 in either year.

Before analysis, record file sizes and SHA-256 values for every candidate and
control input actually consumed. A full frozen-source rehash or build is not
required because no simulation or functional-source change is authorized.

## 2. Authorized implementation scope

Codex may create a new streaming, read-only analyzer and its generated analysis
outputs. Suggested analyzer name:

```text
SimTools/analyze-m5-album-catalog-cohorts.mjs
```

The analyzer must:

- use a real CSV parser or an equivalently correct streaming parser;
- stream the large files rather than loading the approximately 950 MB regional
  settlement ledger as one in-memory string;
- consume no network resources and add no package dependency unless already
  present in the workspace;
- never rewrite an input artifact;
- use invariant-culture numeric parsing;
- report all join failures and excluded values explicitly; and
- exit nonzero on a failed required reconciliation.

Generated CSV/JSON/Markdown outputs may be placed under `SimLogs` with a unique
suffix based on the candidate run, for example:

```text
d6-bounded-spillover-75-decade-enabled-1001-album-catalog-cohort-analysis.csv
d6-bounded-spillover-75-decade-enabled-1001-album-catalog-cohort-analysis.json
```

Do not append conclusions to `ArtistPopulationLifecycleAudit.md` until the
analysis reconciles. If it reconciles, append a concise evidence record and
stop. Do not turn an analysis finding into authority for a behavioral change.

## 3. Authoritative candidate inputs

Use these candidate streams:

```text
*-weeks.csv
*-records.csv
*-completed-week-settlement.csv
*-completed-week-settlement-regional.csv
*-market-clearing-weekly.csv
*-market-revenue.csv
*-decade-annual-rollup.csv
*-album-chart.csv
*-retirement.csv
*-release-strategy.csv
*-album-projects.csv
*-album-project-weekly.csv
```

Use other existing candidate streams only when necessary for a documented
reconciliation. Do not silently substitute a diagnostic ledger for an
established economic value.

The control does not contain the candidate's completed-week settlement family,
and its `records.csv` is not a comparable weekly record-detail population.
Therefore:

- do not fabricate control record-level age cohorts;
- use control annual economics from `market-revenue.csv`;
- use control age-tail summaries from `decade-annual-rollup.csv`;
- use control release/project streams only for fields they actually contain;
  and
- label every treatment/control comparison with its true aggregation level.

## 4. Cohort assignment

Assign every candidate Album settlement record-week in 1967 and 1968 to exactly
one release-age cohort:

```text
NEW:       0-25 completed weeks since release
MID:       26-51 completed weeks since release
CATALOG:   52+ completed weeks since release
```

The primary age source is `records.csv.weeksSinceRelease`, joined on
`(week, recordId)`.

Because settlement occurs before culling, a record retired after settlement may
be absent from the same-week active-record snapshot. For such rows, use the
same-week `retirement.csv` row as the first fallback. If any remaining age is
derived from an earlier/later record snapshot or a release week, report that
fallback separately and prove the derived age against all rows where both
sources exist.

Never drop an unmatched settlement row. Report unmatched:

- record-week count;
- unique record count;
- units;
- gross;
- label net;
- market net; and
- share of the annual Album total.

The main cohort finding is usable only if age coverage is exact or if every
unmatched row has zero economic and clearing weight. Anything else is an
observability limitation.

## 5. Required candidate cohort table

For each year and age cohort, report:

### Population and flow

- unique active Album records;
- Album record-weeks;
- new Album releases entering the cohort;
- actual Album retirements;
- `retiredAfterSettlement` count;
- minimum, median, p75, p90, and maximum age;
- active-title count at the final completed week of the year; and
- units and gross per active record-week.

### Demand and clearing

From `completed-week-settlement-regional.csv`, report:

- raw intent;
- serviceable intent;
- local cleared;
- spillover cleared;
- final cleared;
- physical backorders;
- market-displaced demand; and
- inventory movement.

Derive and report:

```text
serviceable / raw
final cleared / serviceable
final cleared / raw
spillover / final cleared
physical backorders / raw
market-displaced / serviceable
```

### Established economics

From `completed-week-settlement.csv`, report:

- units;
- gross;
- manufacturing cost;
- artist royalty;
- distribution skim;
- label net;
- distribution income;
- market net; and
- gross per cleared unit.

Also report each cohort's share of annual:

- Album units;
- Album gross;
- Album label net; and
- Album market net.

Do not infer price, gross, or net by allocating annual totals proportionally.
Use the established settlement values and reconcile them to the annual
authoritative streams.

## 6. Retirement analysis

Actual retirement evidence is authoritative. A prospective retirement
eligibility reconstruction may also be produced, but it must be labelled
`RECONSTRUCTED` rather than logged telemetry.

The current Album rules in `Systems/ChartManager.cs` are:

```text
catalog sales floor:            10 units/week
never-charted tolerance:        26 weeks
charted tolerance:              52 weeks
```

Using weekly `records.csv` and `album-chart.csv` history, a reconstructed
eligible Album must satisfy:

- current chart position is zero;
- current weekly units are below 10; and
- either:
  - it never charted and age is at least 26 weeks; or
  - it previously charted, is at least 52 weeks past its last chart week, and is
    at least 52 weeks past its last week at or above 10 units.

Validate the reconstruction against every actual Album retirement for which
the required history is complete. Report false positives, false negatives,
history-left-censoring, and ordering ambiguity. If it does not reproduce actual
retirements exactly, retain actual retirements and mark prospective eligibility
`NOT_RELIABLE`.

For each age cohort/year, report:

- under-10 record-weeks;
- off-chart record-weeks;
- reconstructed eligible record-weeks, if reliable;
- actual retirements;
- titles repeatedly returning to at least 10 units after an under-floor week;
  and
- titles crossing from MID into CATALOG while remaining active.

This distinguishes a large catalog caused by ordinary inflow from one sustained
by repeated sales-floor resets.

## 7. Buyer-pool, penetration, and exhaustion limitation

Do not claim a full-catalog buyer-pool or exhaustion result from
`album-demand-explanation.csv`.

The live Album formula computes these runtime intermediates:

```text
buyerPool
regional cumulative units / buyerPool = penetration
max(0.15, 1 / (1 + 4 * penetration)) = exhaustion
```

Those values are not logged for every Album-region-week.
`album-demand-explanation.csv` contains Top-40 Albums plus a stable 1-in-16
launch sample. It is useful as a sampled diagnostic, but it is not a
population-complete cohort ledger.

The analyzer may report a separate `SAMPLED_TOP40_OR_LAUNCH` table containing:

- sample coverage by age/year;
- buyer-pool explanation values;
- raw and serviceable intent for the sampled record-region-weeks; and
- current-year differences for records observed in more than one year.

Do not weight that sample into a claimed catalog-wide buyer-pool,
penetration, or exhaustion estimate. Do not reconstruct an exact exhaustion
value unless every input and update-order dependency is demonstrated from the
retained data.

## 8. Required reconciliations

The analyzer must reproduce these candidate annual Album totals:

| Year | Album units | Album gross |
|---:|---:|---:|
| 1967 | 37,366,778 | 148,719,777.429836 |
| 1968 | 52,979,345 | 210,857,795.435656 |

It must also prove:

1. cohort settlement units and economics sum to candidate annual Album values;
2. regional `finalCleared` sums to settlement Album units;
3. regional `localCleared + spilloverCleared == finalCleared`;
4. regional cohort sums reconcile to the Album columns in
   `market-clearing-weekly.csv`;
5. settlement booking and audit counts remain exactly one;
6. no 1969/week-470 row enters the 1967-1968 analysis;
7. retirement fallbacks do not double-count a record-week; and
8. every excluded or unmatched row is quantified.

Use small tolerances only for decimal CSV formatting. Unit reconciliations are
integer and must be exact.

## 9. Control context

Use the retained control only at its supported annual level. At minimum, print:

| Year | Candidate active Albums | Control active Albums | Candidate >26-week gross share | Control >26-week gross share | Candidate >52-week gross share | Control >52-week gross share |
|---:|---:|---:|---:|---:|---:|
| 1967 | 8,314 | 6,370 | 0.668847 | 0.488461 | 0.411601 | 0.255310 |
| 1968 | 9,227 | 7,002 | 0.691689 | 0.440042 | 0.431699 | 0.205466 |

Reproduce rather than hard-code the values.

Also calculate from the authoritative annual rows:

- candidate/control Album units and gross;
- candidate/control total units and gross;
- candidate and control year-over-year changes;
- absolute candidate/control Album-gross gap; and
- the amount and share of that gap represented by the reported 26+ and 52+
  annual gross summaries.

The currently observed 1968 context is approximately:

```text
candidate Album gross:                 $210.858M
control Album gross:                   $131.066M
Album gross gap:                       $79.792M
candidate 52+ Album gross:             $91.027M
control 52+ Album gross:               $26.930M
52+ difference:                        $64.097M
52+ difference / Album gross gap:      80.33%
```

The analyzer must reproduce these from source rows before relying on them.

## 10. Questions the report must answer

Answer each with `YES`, `NO`, or `NOT_ADJUDICABLE`, followed by evidence:

1. Did the 52+ cohort account for a majority of the candidate's 1967-to-1968
   Album-unit increase?
2. Did the 52+ cohort account for a majority of the candidate's
   1967-to-1968 Album-gross increase?
3. Did 52+ raw intent grow materially faster than new-release raw intent?
4. Did the 52+ cohort's cleared/serviceable rate rise, remain stable, or fall?
5. Was the 52+ excess already present before clearing, or created primarily by
   allocation/spillover?
6. Did repeated returns above the 10-unit retirement floor materially extend
   the 52+ catalog?
7. Can ordinary current-year Album scheduling explain the excess?
8. Does the evidence support old-catalog demand persistence as the correction
   surface without knowing the exact buyer-pool/exhaustion intermediate?

Do not reduce the finding to one ratio. Show absolute levels, year-over-year
movement, shares, and clearing rates together.

## 11. Existing-data sufficiency decision

End with exactly one of:

```text
EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE
EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM
EXISTING_DATA_INSUFFICIENT
```

Use `EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE` only if:

- all required unit/economic/clearing reconciliations pass;
- age coverage is complete;
- the analysis can distinguish pre-clearing demand growth from clearing
  allocation;
- retirement evidence is usable; and
- the evidence identifies whether the excessive surface is new-release yield,
  mid-catalog yield, or 52+ catalog persistence.

This classification may identify a correction surface, but it does not
authorize a correction.

Use `EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM` if the age cohorts
reconcile and clearly localize the excess, but the missing buyer-pool,
penetration, or exhaustion intermediates prevent choosing among otherwise
plausible internal mechanisms.

Use `EXISTING_DATA_INSUFFICIENT` if material settlement value cannot be assigned
an age, the regional joins do not reconcile, or the retained run cannot
separate demand generation from clearing.

## 12. Rerun decision boundary

No rerun is authorized by this handoff.

If the result is not fully sufficient, propose the smallest observational
telemetry addition needed. The proposal should prefer per
Album-region-week fields attached to the immutable settlement identity:

```text
weeksSinceRelease
buyerPool
regionalCumulativeUnitsBeforeSale
penetration
exhaustion
catalogDecayMultiplier
awareness
conversionBeforeCannibalization
cannibalizationSuppression
lastChartedAge
lastSalesAboveRetirementFloorAge
retirementEligibleAfterSettlement
```

The proposal must state whether causal adjudication needs:

- only a replay through completed 1968; or
- a full 522-week decade replay because a required conclusion depends on 1969
  or decade-complete gates.

Do not assume a full decade is necessary merely because the failed command
requested 522 weeks. A replay through completed 1968 is the default sufficient
scope for diagnosing the observed 1968 breach. A later acceptance run after a
behavioral correction would still require its own separately authorized
complete validation ladder.

Do not edit telemetry, launch the proposed replay, alter capacity, reduce Album
yield, change retirement, or modify buyer-pool/exhaustion behavior without a
new explicit owner handoff.

## 13. Stop condition

Stop after:

1. implementing and running the read-only analyzer;
2. writing its reconciled outputs;
3. recording the existing-data sufficiency classification;
4. appending a concise result to `ArtistPopulationLifecycleAudit.md` only if
   the analysis reconciles; and
5. if necessary, writing a telemetry/rerun proposal without implementing or
   launching it.

No simulation or behavioral follow-on is implicit.
