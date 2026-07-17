# Artist Population Market Clearing and Format-Memory Closure Handoff

Status: **OWNER AUTHORIZED / READY FOR IMPLEMENTATION**

Date: 2026-07-16

Scope owner: the next Codex implementation and validation pass.

This handoff supersedes only the prior freezes on:

- a market-wide purchase-capacity mechanism;
- the live label-format memory blend; and
- correction of the known operating-target audit snapshot defect.

Genre diagnostics, genre catalog/keyframe changes, genre supply/routing changes, and
genre-specific acceptance tuning remain deferred.

## 1. Owner decision and required outcome

The owner has authorized a wide systemic correction after the completed
`d6-historical-regional-scouting-failfast-decade-enabled-1001` run:

1. Implement a shared market purchase budget. Stronger records must compete for a
   bounded pool of weekly purchase opportunities instead of creating additional
   aggregate purchases from independent per-record buyer pools.
2. Treat a 1969 scheduled-Album share from **78% through 85%, inclusive**, as
   acceptable.
3. Correct the remaining acceptance failures without changing genre behavior.
4. Preserve enough attribution and integrity evidence to support one more complete
   seed-1001 decade run after bounded probes and checkpoints pass.

The implementation is not a scalar reduction to Single demand and is not an Album
quota. It is a two-part systemic repair:

- common market clearing governs realized purchases downstream of record demand;
- a bounded label-memory blend prevents stale absolute outcomes from replacing the
  contemporaneous format prior upstream of release choice.

## 2. Frozen starting state

Do not discard or overwrite the existing dirty worktree. At handoff creation:

```text
M  SimTools/ArtistPopulationLifecycleAudit.md
M  Systems/RosterManager.cs
?? SimTools/ArtistPopulationHistoricalRegionalScoutingValidationHandoff.md
```

Those changes predate this handoff and belong to the owner/current work. The latest
validation made no production-source change. `git diff --check` passed.

Starting SHA-256 evidence:

```text
Systems/RosterManager.cs
21B13D09CEB69A7991350210A4B156B1CB0D459A69A912C521B3988F950A5EDB

Systems/ChartSimulator.cs
89CAC815A8E8F460CC3B7D2B393A1EFEC525E1F467326213D06B19238F9C594E

Systems/CompetitorManager.cs
76077FD72E78F1F2BBF3D400878D2521AB0DC225D61B3105FC791C1877C89D1F

Systems/GenreAcceptanceService.cs
D1ECB8700D5409CD8FBF41306EBFB3AFE7E1F3472AF80406F4BB055664725D68

SimTools/ChartAuditRunner.cs
C749DF1703A381EA9A94835B6B705C7EF3A15AF7F9ACE98CDB66C1E2F8E83080
```

Authoritative treatment:

```text
d6-historical-regional-scouting-failfast-decade-enabled-1001
```

Authoritative retained control:

```text
d6-transition-envelope-decade-control-1001
```

No new control is required if the disabled checkpoint below remains byte-identical
and the existing control preflight passes.

## 3. Causal findings

### 3.1 The 49 operating-target overshoots are audit false positives

All 49 reported rows occur on acquisition weeks. The weekly scouting observation
captures `OperatingRosterTarget` before acquisition, while its final roster count is
captured after the acquisition transfer. The acquisition event stream and the live
label target both reconcile correctly, and the live fail-fast invariant remained
clean.

The defect is confined to the snapshot written by
`ChartAuditRunner.WriteLabelScoutingVacancyRows`. Its target column must use the
final live `label.OperatingRosterTarget` corresponding to the already-finalized
roster. Do not change acquisition, roster, or target behavior.

### 3.2 There is no shared consumer market

`ChartManager.SimulateWeek` currently walks every record and region, calls the
Single or Album sales simulator, and immediately commits the result to inventory and
sales. Each record independently receives:

- for Singles, the full regional buying population filtered by that record's
  awareness and conversion;
- for Albums, a genre/era buyer pool filtered by that record's awareness and
  conversion.

Per-record exhaustion prevents one title from reselling indefinitely to its own
audience, but nothing reconciles simultaneous titles against a common weekly
regional purchase pool. Talent, acceptance, awareness, and supply growth therefore
increase aggregate realized purchases instead of reallocating market share.

The late Single evidence is consistent with this:

| Year | Single decisions, enabled/control | Units per Single decision, enabled/control | Single units, enabled/control |
|---|---:|---:|---:|
| 1967 | 1.0459x | 1.2038x | 1.2591x |
| 1968 | 1.1098x | 1.0635x | 1.1802x |
| 1969 | 1.6994x | 0.7950x | 1.3511x |

The same-observation exposure-weighted enabled/legacy Single demand seam was
`1.3185x`, `1.4050x`, and `1.3611x` in 1967-1969. In 1969, quality was not the
explanation: mean enabled quality was `0.5999` versus `0.6142` control. The extra
Single and total units are a market-architecture and format-mix problem, not proof
that the enabled records were simply better.

The total-unit misses are entirely carried by Singles:

```text
1967: Single +27.039M, Album -1.077M, total +25.962M
1969: Single +33.453M, Album -2.701M, total +30.752M
```

### 3.3 Absolute label-format memory is overconfident

`CompetitorManager.DecideRelease` calculates era-aware Single and Album priors, then
blends each toward a label-wide absolute `emaNetPerRelease` using:

```text
confidence = releasesObserved / (releasesObserved + 4)
projected = lerp(currentPrior, historicalAbsoluteEma, confidence)
```

Confidence approaches one as observations accumulate. The current prior therefore
receives less weight precisely when the market is changing most. In 1969, mean
LabelFormat confidence was approximately `0.8820` for Singles and `0.8765` for
Albums. The memory is also label-wide and absolute: it is not normalized against the
opportunity or economics that existed when the outcome was observed.

The decision stages show the effect:

| Year | Decisions | Album at deterministic prior | Album after memory | Final Album |
|---|---:|---:|---:|---:|
| 1963 | 3,590 | 38.72% | 50.53% | 50.36% |
| 1964 | 3,544 | 35.16% | 45.94% | 45.68% |
| 1969 | 3,027 | 78.86% | 73.31% | 71.99% |

Thus the same high-confidence absolute memory amplifies Albums too far in 1963/64
and suppresses them in 1969. The deterministic 1969 prior is already inside the
owner's accepted 78-85% band.

The 1969 population is:

```text
LabelFormat: 2,602 decisions
  deterministic-prior Album share 85.97%
  after-memory Album share         79.52%
  final Album share                77.94%

ProjectPrior: 425 decisions
  final Album share                35.53%
```

`ProjectPrior` is the existing live non-retained/emerging-project bypass. It is a
genre-sensitive policy and remains untouched in this pass.

### 3.4 Precommitted 0.75 confidence ceiling

An offline replay of the already-recorded decision trace changed no simulation
state, RNG draw, genre input, prior, EMA, or noise. It only recomputed:

```text
effectiveConfidence = min(recordedConfidence, 0.75)
projected = lerp(recordedPrior, recordedEma, effectiveConfidence) * recordedNoise
```

Results:

| Year | Replayed Albums | Decisions | Replayed Album share | Replayed scheduled Albums/control |
|---|---:|---:|---:|---:|
| 1963 | 1,672 | 3,590 | 46.574% | 1.156x |
| 1964 | 1,501 | 3,544 | 42.353% | 1.135x |
| 1969 | 2,393 | 3,027 | 79.055% | 0.854x |

All three previously failed scheduled-Album surfaces would be in their existing
bands, and 1969 would be inside the owner-approved share band. This is a
counterfactual over fixed old outcomes, not a claim that the next endogenous run
must reproduce those exact counts.

Use **0.75 exactly** for the first behavioral candidate. Do not tune it after a
partial run.

## 4. Required implementation

### 4.1 Live-only two-pass regional market clearing

Refactor the live sales pass into demand intent followed by common clearing.
Disabled and prewarm behavior must continue through the existing immediate sales
path byte-for-byte.

For every live week:

1. Preserve the current record-major, region-minor evaluation order.
2. Compute one intent for every active record-region pair without committing
   inventory or sales.
3. Preserve all existing demand inputs:
   awareness, quality, acceptance, format tilt, radio, momentum, age, seasonality,
   Album affinity/willingness, substitution, distribution, per-record audience
   exhaustion, stock, and store capacity.
4. Consume each existing sales jitter RNG draw in the same record/region order and
   store it in the intent.
5. For each region, allocate its common weekly purchase capacity across **all**
   serviceable Single and Album intents, including player-owned records.
6. Commit cleared integer units to stock, regional totals, record totals, momentum,
   charts, label finance, and revenue memory.

The common allocator must be deterministic:

- use serviceable intent as the competitive weight;
- if aggregate intent does not exceed capacity, clear every intent;
- otherwise apply the common proportional factor;
- assign integer remainders by descending fractional remainder, then stable
  `recordId` ordinal tie-break;
- never exceed regional capacity, serviceable intent, store capacity, or stock.

Do not create tier reservations, label reservations, genre lanes, format quotas, or
player exceptions. A stronger record wins a larger portion of a fixed market; it
does not create another copy of the market.

### 4.2 Purchase-capacity constant

Use the fixed regional buying-population inputs already present in
`MarketRegion.GetBuyingPopulationPercentage`. Define:

```text
weeklyRegionalPurchaseCapacity =
    round(regionalBuyingPopulation * 1.34)
```

The seven configured regions sum to approximately `2,165,246` addressable buyers.
The initial common capacity is therefore about `2.901M` purchases per week or
`150.874M` per 52 weeks. This is inside the previously accepted `140-175M` annual
market-volume interval.

`1.34` is precommitted for the first candidate. It must not depend on:

- realized demand, releases, units, revenue, chart ranks, labels, or genres;
- the retained control's annual outcomes;
- the current calendar year; or
- a post-run calibration.

Do not add a second seasonal multiplier to the capacity. Existing format demand is
already seasonal; low-demand capacity may remain unused.

This is a purchase-opportunity budget, not a retail-dollar budget. Single and Album
prices continue to determine finance and format economics after units clear.

### 4.3 Demand, backorder, and discovery semantics

Keep these quantities separate:

- `rawDemandThisWeek`: audience intent before stock/store/market constraints;
- `serviceableIntentThisWeek`: jittered demand that stock and store capacity can
  physically serve;
- `unitsSoldThisWeek`: post-clearing realized purchases;
- `physicalBackordersThisWeek`: stock-shortfall demand only;
- `marketDisplacedDemandThisWeek`: serviceable intent lost to the common market
  budget.

Market displacement is substitution/foregone purchase, not a physical backorder.
It must not:

- enter `unitsBackordered`;
- independently request restock;
- count as unmet physical distribution demand; or
- inflate specialist-restock eligibility.

Breakout and restock behavior should continue using raw audience demand, cleared
sales, and physical backorders in their existing roles. This preserves latent demand
and lets realized competition affect the fulfilled-volume component without
inventing inventory demand.

The current jitter is applied after a stock/capacity minimum and can round above that
minimum. The live intent path must clamp the jittered intent back to both available
stock and store capacity. Leave the disabled path untouched.

Album cannibalization remains upstream of common clearing. Per-record audience
exhaustion remains in place; it models title saturation, while the new allocator
models simultaneous competition.

### 4.4 Live label-format memory ceiling

In the live enabled path only, after the existing `ProjectPrior` bypass has resolved
confidence, apply:

```text
effectiveSingleConfidence = min(singleConfidence, 0.75)
effectiveAlbumConfidence  = min(albumConfidence, 0.75)
```

Use the effective confidence in the existing `Lerp(prior, ema, confidence)`.

Do not:

- change `revenueMemoryAlpha`;
- change `revenueMemoryConfidenceK`;
- change the stored absolute EMA in this candidate;
- change project outcome folding;
- change decision noise;
- change Single or Album priors;
- change the emerging-project bypass; or
- impose a release quota.

The ceiling guarantees that the contemporaneous prior retains at least 25% of the
blend. A normalized residual-memory redesign may be considered later, but it is not
required for this closure candidate.

### 4.5 Operating-target telemetry repair

When writing `label-scouting-vacancy-weekly.csv`, write the final live
`label.OperatingRosterTarget` that corresponds to the final roster snapshot. The
column header and all production behavior remain unchanged.

Add a fixed acquisition-week probe proving:

```text
rosterSize <= writtenOperatingRosterTarget <= maxRosterSize
```

and reconciling the written target to the acquisition target-event ledger.

## 5. Telemetry and analyzer requirements

Do not alter any of the 45 frozen disabled CSV schemas. Create enabled/live-only
streams.

### 5.1 `market-clearing-weekly.csv`

One row per week and region:

```text
week,year,regionId,activeIntentCount,
rawSingleDemand,rawAlbumDemand,rawTotalDemand,
serviceableSingleIntent,serviceableAlbumIntent,serviceableTotalIntent,
purchaseCapacity,clearedSingleUnits,clearedAlbumUnits,clearedTotalUnits,
unusedCapacity,rationingFactor,
physicalBackorders,marketDisplacedDemand,
inventoryViolationCount,allocationViolationCount,reconciliationDelta
```

Required exact invariants:

```text
clearedTotalUnits <= purchaseCapacity
clearedTotalUnits <= serviceableTotalIntent
clearedSingleUnits + clearedAlbumUnits == clearedTotalUnits
unusedCapacity == max(0, purchaseCapacity - clearedTotalUnits)
sum(record-region cleared units) == clearedTotalUnits
reconciliationDelta == 0
inventoryViolationCount == 0
allocationViolationCount == 0
```

### 5.2 `format-memory-adjustment.csv`

One row per live format decision:

```text
week,year,recordId,labelId,memoryScope,
rawSingleConfidence,rawAlbumConfidence,
effectiveSingleConfidence,effectiveAlbumConfidence,
singleCapApplied,albumCapApplied
```

For `ProjectPrior`, effective confidence remains zero. For LabelFormat decisions,
effective confidence must never exceed `0.75`.

### 5.3 Analyzer

Add `SimTools/analyze-market-clearing-format-memory.mjs`. It must:

- fail closed on missing/malformed rows;
- reconcile clearing rows to `market-revenue.csv`, `weeks.csv`, and annual rollups;
- report capacity utilization and displacement by year, region, and format;
- report raw versus effective memory confidence by year and scope;
- reproduce the prior/memory/final format-decision stage table;
- report scheduled-Album counts and shares;
- report Single, Album, total-unit, gross, net, and release ratios against the
  retained control;
- enumerate every ordinary and catastrophic gate failure; and
- explicitly report `GENRE_DIAGNOSTICS_DEFERRED` without adjudicating genre peaks,
  genre shares, or genre realism.

## 6. Fixed probes

Extend the existing D6 probe suite. At minimum prove:

1. no clearing during prewarm;
2. no clearing when Genre Market V2 is disabled;
3. below-cap intents clear unchanged;
4. above-cap intents reconcile exactly to capacity;
5. proportional allocation favors greater intent and is invariant to collection
   insertion order;
6. largest-remainder ties use ordinal `recordId`;
7. Single and Album intents compete in the same regional pool;
8. player-owned and AI records compete in the same pool;
9. cleared units never exceed stock or store capacity after jitter;
10. market displacement never enters physical backorders;
11. raw demand remains available to breakout/restock telemetry;
12. cleared units feed momentum, charts, finance, and memory;
13. live LabelFormat confidence is capped at `0.75`;
14. `ProjectPrior` confidence remains zero;
15. disabled/prewarm format confidence remains unchanged;
16. the finalized acquisition-week operating target matches the finalized roster.

## 7. Validation ladder

Stop at the first hard failure. Preserve commands, stdout/stderr, stream manifests,
hashes, and measurements in `ArtistPopulationLifecycleAudit.md`.

### M0 — integrity and implementation review

- Record `git status --short`, `git diff --check`, and starting hashes.
- Confirm only authorized files are changed.
- Confirm no genre data, genre catalog, genre acceptance, genre routing, or supply
  policy changed.
- Confirm disabled sales and format-decision branches remain intact.

### M1 — build and fixed probes

```powershell
dotnet build "Label Man.sln" --no-restore
```

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-market-clearing-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes
```

Require build success, accepted D5 lines, all prior D6 probes, all new probes, and
`CHART_AUDIT_COMPLETE`.

### M2 — disabled compatibility

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-market-clearing-disabled-52-1001 --seed=1001 --aggregate-only
```

Require:

- the same 45-stream suffix set as
  `d6-transition-envelope-disabled-52-1001`;
- 45/45 suffix-matched SHA-256 equality;
- no market-clearing or memory-adjustment stream; and
- no genre/population enabled telemetry.

Then run the existing no-simulation control preflight:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-market-clearing-control-preflight-1001 --seed=1001 --catastrophic-control-preflight --gate-control-run=d6-transition-envelope-decade-control-1001
```

Require all 1960-1969 preflight rows and the completion marker. This command must not
create candidate run CSVs.

### M3 — 104-week enabled checkpoint and repeat

Run twice with different run names and otherwise identical arguments:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-market-clearing-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-market-clearing-enabled-repeat-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Require:

- header-only catastrophic streams;
- exact clearing and inventory invariants;
- nonzero clearing activation and nonzero displacement in at least one region/week;
- no written operating-target overshoot;
- exact enabled-repeat equality for all deterministic comparable streams; and
- release ratios still inside the inherited non-catastrophic envelope.

### M4 — 260-week transition checkpoint

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=260 --run=d6-market-clearing-enabled-260-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Before the decade, require:

- all exact invariants;
- no catastrophic abort;
- 1963 and 1964 scheduled-Album ratios each within `[0.80, 1.20]`;
- first-five-year aggregate releases and economics inside inherited gates;
- no ordinary year below the `0.85` total-unit/economic floor; and
- no source, constant, flag, analyzer, or acceptance change after M4.

If 1963 or 1964 misses, stop. Do not tune the memory cap or buyer budget.

### M5 — one enabled decade

Only after M0-M4 pass:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-market-clearing-failfast-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Run the new analyzer immediately after completion. Do not launch seeds 1002/1003,
a holdout, a replacement control, or a second decade under this handoff.

## 8. Acceptance rules

All inherited reconciliation, invariant, lifecycle, release, finance, and
catastrophic gates remain in force except for the explicit 1969 Album-share band
below.

Required final surfaces:

```text
1963 scheduled Albums/control: [0.80, 1.20]
1964 scheduled Albums/control: [0.80, 1.20]
1969 scheduled Albums/control: [0.80, 1.20]
1969 scheduled-Album share:     [0.78, 0.85]

annual Single units/control:    [0.85, 1.15]
annual Album units/control:     [0.80, 1.20]
annual total units/control:     [0.85, 1.15]
annual total gross/control:     [0.85, 1.15]
annual market net/control:      [0.85, 1.15]
```

Also require:

- all common-market exact invariants;
- zero market clearing before the live boundary;
- zero physical backorder contamination from market displacement;
- no operating-target overshoots after the snapshot repair;
- decade aggregate economics inside inherited `[0.90, 1.10]`;
- release totals inside inherited gates; and
- header-only catastrophic output.

The common market may be under capacity. `unusedCapacity > 0` is not a failure.
Likewise, market displacement is expected when demand exceeds capacity.

Do not pass a candidate merely because aggregate units are capped. Format share,
format-specific units, gross/net, releases, lifecycle, and all exact invariants must
pass independently.

## 9. Stop and hand-back conditions

Stop and preserve evidence if any of these occurs:

- build/probe/disabled compatibility failure;
- any allocation, inventory, stock, backorder, finance, or CSV reconciliation
  mismatch;
- any non-header catastrophic row;
- deterministic repeat mismatch;
- 1963/1964 transition miss;
- 1969 Album share below 78% or above 85%;
- a required annual or decade band miss; or
- evidence that common clearing starves a region/tier through an ordering defect.

Do not react to a miss by changing `1.34`, `0.75`, a demand constant, a genre
constant, or an acceptance band. Diagnose and return a measured handoff.

## 10. Explicit exclusions

This pass must not answer whether Psychedelic Rock peaked in 1967 and must not
adjudicate any genre's historical curve. It must not change:

- genre catalog values or year keyframes;
- genre supply, emergence, retention, routing, specialist, or radio rules;
- Album affinity or willingness curves;
- Single or Album format tilts;
- label/artist population policy;
- release capacity, scouting, acquisition, bankruptcy, or deal behavior;
- prices, royalties, pressing/packaging costs, or production costs;
- chart points, rank, retirement, or restock thresholds; or
- the retained control and inherited acceptance gates, other than the owner-approved
  1969 Album-share band.

The new telemetry may retain ordinary format/region identifiers needed for
reconciliation. It must not add genre acceptance reports or genre pass/fail logic.

## 11. Completion record

Append to `SimTools/ArtistPopulationLifecycleAudit.md`:

- exact files changed and final hashes;
- all commands and exit/completion markers;
- build warnings;
- fixed-probe count;
- disabled manifest/hash comparison;
- control preflight result;
- clearing invariants and capacity utilization;
- memory raw/effective confidence;
- scheduled-Album counts/shares and stage attribution;
- annual Single/Album/total units, gross, net, and release ratios;
- operating-target reconciliation;
- catastrophic stream status;
- the final accept/stop decision; and
- a statement that genre diagnostics remained deferred.

