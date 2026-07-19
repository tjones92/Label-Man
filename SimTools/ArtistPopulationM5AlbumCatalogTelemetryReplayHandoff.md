# M5 Album Catalog Telemetry Replay Handoff

Status: **EXISTING DATA INSUFFICIENT / TELEMETRY-ONLY AMENDMENT AUTHORIZED / STOP BEFORE BEHAVIOR**

Date: 2026-07-18

> **2026-07-18 compatibility amendment:** The first M2 attempt correctly
> stopped after 22/45 frozen hashes differed. The cause was an older
> granted-region distribution-skim correction leaking into the disabled legacy
> revenue path, not this Album telemetry or its analyzer. The owner authorized
> the narrow compatibility branch and a fresh M2-M5 continuation. Resume only
> from `ArtistPopulationM5AlbumCatalogTelemetryCompatibilityAmendmentHandoff.md`,
> which supersedes this document where the two differ. Preserve the failed
> `d6-album-catalog-telemetry-disabled-52-1001` family.

This handoff follows:

```text
SimTools/ArtistPopulationM5TrendAdjudicationHandoff.md
SimTools/ArtistPopulationM5AlbumCatalogCohortAnalysisHandoff.md
SimLogs/d6-bounded-spillover-75-decade-enabled-1001-album-catalog-cohort-analysis.md
SimLogs/d6-bounded-spillover-75-decade-enabled-1001-album-catalog-cohort-analysis.json
```

The preserved seed-1001 M5 candidate stopped after 469 completed ticks because
completed-1968 gross revenue was
`297,153,766.647076 / 224,777,772.118624 = 1.321989x`, above the catastrophic
inclusive `[0.70,1.30]` envelope.

The authorized read-only cohort analysis correctly ended:

```text
EXISTING_DATA_INSUFFICIENT
```

All `832,167` Album settlement record-weeks representing `11,573` titles,
`90,346,123` units, and `$359,577,569.58` gross lacked an authorized release-age
assignment. `records.csv` intentionally contains the weekly Single population,
not Albums, and the same-week retirement fallback assigned no rows. The
regional settlement still reconciled local plus spillover to final clearing,
final clearing to settlement units, and serviceable/cleared Album totals to
`market-clearing-weekly.csv`.

The existing `106,098.087` raw-intent difference is not presently evidence of
a market defect. `MarketClearingRegionalSummary` accumulates floating-point
`rawDemandThisWeek`, while the frozen settlement stores
`Mathf.RoundToInt(rawDemandThisWeek)` per record-region before aggregation. The
telemetry amendment below records the exact pre-rounding value so this
representation difference can be adjudicated.

This handoff authorizes only:

1. an enabled-only observational telemetry amendment;
2. fixed probes, build/diff verification, and disabled compatibility replay;
3. one 104-week seed-1001 telemetry checkpoint;
4. one seed-1001 replay through the 469 completed weeks ending in 1968; and
5. rerunning the cohort analyzer against that new artifact.

It does **not** authorize a behavioral correction, demand/retirement/capacity
change, full decade, later seed, holdout, replacement control, or parameter
sweep.

## 1. Preserve existing artifacts and source

Do not modify, rename, truncate, normalize, or replace:

```text
SimLogs/d6-bounded-spillover-75-decade-enabled-1001-*
SimLogs/d6-transition-envelope-decade-control-1001-*
SimLogs/Archive/d6-bounded-spillover-75-decade-enabled-1001-partial-104w-20260718/*
SimLogs/d6-bounded-spillover-75-decade-enabled-1001-album-catalog-cohort-analysis.*
```

Do not restore the deliberately deleted:

```text
SimTools/analyze-market-clearing-format-memory.mjs
```

Before editing, verify that the nine frozen-source hashes still match
`ArtistPopulationM5TrendAdjudicationHandoff.md`. Also record SHA-256 for the
additional files allowed below that were not part of that nine-file manifest.
If a required pre-edit hash differs from the recorded frozen M5 source, stop
and report the mismatch instead of recreating source by guesswork.

Preserve unrelated `.uid`, handoff, analysis, and working-tree changes.

## 2. Allowed files

Functional or telemetry source changes are limited to:

```text
Data/RegionalRecordData.cs
Systems/AlbumSimulator.cs
Systems/ChartManager.cs
SimTools/ChartAuditRunner.cs
SimTools/ArtistPopulationLifecycleProbeSuite.cs
SimTools/analyze-m5-album-catalog-cohorts.mjs
```

This handoff document and the eventual audit entry may also be changed.

Do not modify:

- Album demand, affinity, willingness, pricing, conversion, decay, retirement,
  chart, release, project, finance, format-choice, memory, capacity, spillover,
  inventory, or RNG behavior;
- constants or exported values;
- method call order;
- random draw count or order;
- record enumeration/order;
- settlement, booking, audit, or culling order; or
- disabled-mode output schemas.

If the required telemetry cannot be captured within this file boundary without
changing behavior, stop and report the seam.

## 3. Telemetry architecture

Add a new enabled-only stream rather than changing the existing settlement CSV
schemas:

```text
*-album-catalog-settlement-diagnostic.csv
```

It must contain only Album record-region rows. Its immutable key is:

```text
settlementId, recordId, regionId
```

The diagnostic must be frozen as part of the existing
`CompletedWeekSettlement` before booking, audit acknowledgement, or culling.
The writer must serialize primitive snapshot values from the settlement. It
must not derive age or causal values later by dereferencing a mutable
`RecordRuntimeData` or `RegionalRecordData`.

The production order remains:

```text
simulate
freeze immutable settlement and Album diagnostic
book exactly once
acknowledge/write exactly once
cull
```

The new stream must not exist when Genre Market V2 is disabled.

## 4. Required fields

Emit this schema, or a documented field-for-field equivalent:

```text
week
year
settlementId
recordId
labelId
labelTier
genre
regionId
weeksSinceRelease
weeksOnChart
currentPosition
lastChartedAge
lastSalesAboveRetirementFloorAge
weeksSinceLastCharted
weeksSinceSalesAboveRetirementFloor
retirementEligibleAfterSettlement
rawIntentExact
rawIntentRounded
serviceableIntent
localCleared
spilloverCleared
finalCleared
physicalBackorders
marketDisplacedDemand
inventoryMovement
buyerPool
regionalCumulativeUnitsBeforeSale
penetration
exhaustion
catalogDecayMultiplier
effectiveAwareness
conversionBeforeCannibalization
cannibalizationSuppression
rawDemandBeforeCannibalization
rawDemandAfterCannibalization
```

Definitions:

- `week` and `settlementId` use the existing settlement identity.
- `year` uses the existing settlement year convention.
- `weeksSinceRelease`, chart state, relevance ages, and retirement eligibility
  are captured at settlement freeze time.
- `retirementEligibleAfterSettlement` is the result of the existing Album
  retirement predicate at that same immutable boundary. It is observational;
  do not call a second behavior-changing cull path.
- `rawIntentExact` is the live floating-point `rawDemandThisWeek` value used by
  market-clearing summaries before the existing settlement rounding.
- `rawIntentRounded` reproduces the existing
  `Mathf.RoundToInt(rawDemandThisWeek)` value.
- `regionalCumulativeUnitsBeforeSale` is the regional lifetime-unit value read
  when penetration is computed, before current-week units are added.
- `penetration` and `exhaustion` are the exact live values used by
  `AlbumSimulator.CalculateRegionalSales`.
- `catalogDecayMultiplier` is `1` at or before the existing decay threshold and
  the exact existing power term afterward.
- `effectiveAwareness` is the value after chart-position minimums and before it
  multiplies buyer pool and conversion.
- `conversionBeforeCannibalization` is the fully composed conversion after
  appeal, exhaustion, word of mouth, sentiment, packaging, age, catalog decay,
  seasonality, format routing, distribution, and label-tier terms, immediately
  before raw pre-cannibalization demand is calculated.
- `cannibalizationSuppression` is the existing week-local suppression.
- `rawDemandBeforeCannibalization` and `rawDemandAfterCannibalization` are the
  exact values before physical inventory, store-capacity, jitter, and common
  clearing.

Use round-trip invariant-culture formatting for causal floating-point fields.
Do not reduce them to six decimal places merely to match older diagnostic
formatting.

## 5. Week-local capture requirements

`AlbumSimulator.CalculateRegionalSales` already computes the required causal
intermediates. Copy them into telemetry-only week-local fields on
`RegionalRecordData` without re-evaluating formulas and without adding any
random call.

Every new week-local field must be overwritten on every Album-region live sales
calculation. If any path can skip calculation while still appearing in the
settlement, reset the field deterministically before the weekly intent pass and
emit an explicit observation-valid flag. Do not allow a prior week's value to
leak into the current settlement.

The diagnostic must capture the jittered/serviceable and clearing outputs from
their existing state, but it must not introduce a second jitter calculation.

For non-finite causal telemetry, fail the run with the settlement key and field
name. Do not coerce non-finite values to zero.

## 6. Fixed probes

Extend the existing D6 fixed probe suite after probe 65. At minimum cover:

1. `0-25`, `26-51`, and `52+` cohort boundaries using captured ages 25, 26, 51,
   and 52;
2. buyer pool, regional cumulative units, penetration, and exhaustion capture
   equality to the already-calculated live values;
3. catalog decay multiplier equality at ages 26, 27, and 52;
4. exact before/after-cannibalization demand identity;
5. `rawIntentRounded == Mathf.RoundToInt(rawIntentExact)`;
6. immutable snapshot behavior after the backing runtime objects change;
7. retirement eligibility capture for never-charted and charted Albums at
   their existing boundaries;
8. one diagnostic row per Album settlement entry-region and no Single rows;
9. no duplicate `(settlementId, recordId, regionId)` identity;
10. no stale week-local diagnostic value;
11. zero added RNG draws; and
12. disabled-mode absence of the diagnostic stream/state path.

Retain and pass all accepted D5 and D6 probes.

## 7. Analyzer amendment

Extend:

```text
SimTools/analyze-m5-album-catalog-cohorts.mjs
```

Do not overwrite or reinterpret the existing
`EXISTING_DATA_INSUFFICIENT` report. The analyzer must accept a new candidate
prefix and use the new Album diagnostic stream as the authoritative age and
causal join.

For the new prefix:

- assign `NEW = 0-25`, `MID = 26-51`, and `CATALOG = 52+` directly from captured
  `weeksSinceRelease`;
- require one diagnostic row for every Album settlement region row;
- require every diagnostic key to join exactly once to the immutable regional
  settlement;
- require entry-level age/chart/retirement values to agree across all regions
  of the same settlement record;
- aggregate exact raw intent by cohort and reconcile it to
  `market-clearing-weekly.rawAlbumDemand` within a documented float-accumulation
  tolerance established by the 104-week checkpoint;
- retain exact integer reconciliations for serviceable and cleared units;
- calculate buyer-pool, penetration, exhaustion, decay, awareness, conversion,
  cannibalization, and retirement-eligibility distributions by year/cohort;
- report p10, p25, median, p75, p90, p95, p99, mean, minimum, and maximum for
  causal continuous values;
- separate title counts, record-weeks, and record-region-weeks;
- report raw, serviceable, cleared, units, gross, and net shares by cohort; and
- preserve the eight required `YES`/`NO`/`NOT_ADJUDICABLE` questions and the
  three existing-data classifications.

Add a telemetry-validation mode usable on the 104-week checkpoint. It must
validate schema, identities, causal arithmetic, and age coverage even though
the final 1967-1968 questions are not yet adjudicable.

## 8. M1 - source verification, build, diff, and probes

After implementation:

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-album-catalog-telemetry-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes
```

Require:

- build success with no new warning;
- `git diff --check` success;
- accepted D5 probes pass;
- all existing D6 probes pass;
- all new telemetry probes pass; and
- no simulation beyond the one-week probe harness.

Stop on any failure.

## 9. M2 - disabled compatibility boundary

Run:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-album-catalog-telemetry-disabled-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
```

Compare by suffix, length, and SHA-256 with:

```text
d6-market-clearing-disabled-52-1001
```

Require:

- normal exit zero and `CHART_AUDIT_COMPLETE ... weeks=52`;
- the same 45 frozen suffixes;
- all 45 hashes byte-identical;
- no missing or extra frozen stream; and
- no Album catalog settlement diagnostic stream.

Stop on any failure. Do not run M3.

## 10. M3 - 104-week telemetry checkpoint

Use the unique prefix:

```text
d6-album-catalog-telemetry-enabled-104-1001
```

Run:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-album-catalog-telemetry-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Do not add catastrophic fail-fast or a gate-control switch.

Require:

- normal exit zero and `CHART_AUDIT_COMPLETE ... weeks=104`;
- all settlement booking/audit, clearing, spillover, allocation, inventory,
  ownership, lifecycle, and non-finite invariants remain zero;
- the new diagnostic stream contains Album rows only;
- exact key coverage against all Album regional settlement rows;
- zero duplicate diagnostic keys;
- zero stale or invalid causal observations;
- exact integer serviceable/cleared reconciliations;
- documented, small, representation-only exact-raw reconciliation tolerance;
- nonzero observations in all three age cohorts, including `52+`;
- captured retirement eligibility agrees with the existing frozen predicate;
  and
- established 1960-1961 economic, release, and format values reproduce the
  corresponding years of the preserved M5 candidate.

Run the analyzer's telemetry-validation mode and save its output under the M3
prefix. If it cannot assign every economically weighted Album row an age, or if
any pre-existing economic value changes, stop. Do not run M4.

No deterministic repeat is required at this rung because the change is
observational and M2 protects the disabled schedule. Any unexplained
nondeterminism or economic drift is nevertheless a hard stop.

## 11. M4 - one replay through completed 1968

Only after M1-M3 pass, use:

```text
d6-album-catalog-telemetry-through-1968-1001
```

Run exactly 469 completed ticks:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=469 --run=d6-album-catalog-telemetry-through-1968-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Do not use:

```text
--catastrophic-fail-fast
--gate-control-run
```

This is a diagnostic reproduction of the already-observed completed-1968
surface. It is not an acceptance M5 and must end normally at 469 ticks rather
than deliberately aborting at the next completed-year fail-fast check.

Require:

- process exit zero;
- `CHART_AUDIT_COMPLETE ... weeks=469`;
- `weeks.csv` contains exactly 469 data rows ending in 1968;
- no 1969/week-470 row enters the cohort analysis;
- all M3 telemetry and structural invariants continue to pass;
- every 1967-1968 Album regional settlement row has exactly one diagnostic row;
- all established candidate annual release, format, unit, gross, label-net, and
  market-net values for 1960-1968 reproduce the preserved M5 observations; and
- no pre-existing gameplay/economic stream changes except a documented
  completion-boundary difference caused solely by running without fail-fast.

If the run differs behaviorally from the preserved candidate, stop and classify
the telemetry implementation as non-observational. Do not tune around the
difference.

## 12. M5 - cohort adjudication

Run the amended analyzer against:

```text
candidate: d6-album-catalog-telemetry-through-1968-1001
control:   d6-transition-envelope-decade-control-1001
```

The report must reproduce all required tables and questions from
`ArtistPopulationM5AlbumCatalogCohortAnalysisHandoff.md`, now using captured
immutable age and causal fields.

It must specifically determine:

1. how much of the 1967-to-1968 Album-unit and Album-gross increases came from
   `NEW`, `MID`, and `CATALOG`;
2. whether `CATALOG` excess exists in raw demand before inventory and clearing;
3. whether serviceability or common clearing materially amplifies or suppresses
   it;
4. whether expanding buyer pool reduces penetration and raises exhaustion
   headroom for old Albums;
5. whether weak catalog decay, awareness, conversion, or cannibalization
   materially mediates the excess;
6. whether repeated sales-floor resets prevent retirement;
7. whether current-year Album scheduling can explain the excess; and
8. which narrow correction surface, if any, is supported.

End with exactly one:

```text
EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE
EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM
EXISTING_DATA_INSUFFICIENT
```

Append a concise result to `ArtistPopulationLifecycleAudit.md` only after all
required telemetry, economic, and clearing reconciliations pass. Preserve the
full CSV/JSON/Markdown analyzer outputs under the new run prefix.

## 13. Decision and stop boundary

If the result is:

```text
EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE
```

report the supported correction surface and stop. Do not implement it. A new
owner handoff is required for any behavioral change.

If the result is:

```text
EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM
```

report the remaining ambiguity and stop. Do not add another telemetry round or
run another simulation without a new handoff.

If the result is:

```text
EXISTING_DATA_INSUFFICIENT
```

report the exact missing evidence and stop. Do not escalate automatically to a
full decade or later seed.

A 522-week decade run is not authorized here. It is unnecessary for diagnosing
the already-completed 1968 breach. A later full decade is required only after a
separately authorized behavioral correction reaches its final acceptance rung.

Do not alter Album yield, buyer-pool behavior, exhaustion, catalog decay,
retirement, scheduling, prices, physical capacity, common-market capacity,
spillover limits, or responsive memory under this handoff.
