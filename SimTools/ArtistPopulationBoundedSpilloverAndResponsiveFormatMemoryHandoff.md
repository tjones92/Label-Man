# Artist Population Bounded Spillover and Responsive Format-Memory Handoff

Status: **OWNER AUTHORIZED / READY FOR IMPLEMENTATION**

Date: 2026-07-17

Scope owner: the next Codex implementation and validation pass.

This handoff supersedes the behavioral prescription in
`ArtistPopulationMarketClearingAndFormatMemoryHandoff.md`. Preserve the accepted
common-clearing implementation and its disabled boundary, but replace:

- isolated regional clearing with bounded cross-region spillover;
- the live `0.75` ceiling over an absolute lifetime-net EMA with a responsive,
  opportunity-normalized format-memory model; and
- post-cull aggregate auditing with a completed-week settlement ledger that
  reconciles before record retirement.

The owner explicitly authorizes a structural implementation rather than a narrow
constant patch. Equivalent designs are allowed where they satisfy the invariants,
causal contract, and validation gates below. Do not use that freedom for
genre-specific tuning, format quotas, control-fitting, or an unbounded national
pool.

## 1. Required outcome

Implement a stable live market in which:

1. unused purchase capacity can move a bounded distance across the existing region
   graph instead of remaining stranded while adjacent markets ration demand;
2. format memory measures recent performance relative to the opportunity and
   forecast that existed when a release was chosen;
3. Albums begin informing format memory while they are still active, rather than
   only after a 45-142 week retirement tail;
4. every completed week's units and economics reconcile before culling;
5. the disabled and prewarm routes remain byte-frozen; and
6. one fully reconciled seed-1001 decade can adjudicate the inherited release,
   format, unit, and economic gates.

The intended behavior is neither “sell more Albums” nor “raise the market cap.”
Spillover should rescue geographically stranded purchasing opportunities, while
memory should express whether a label and format over- or under-performed their
contemporaneous expectation.

## 2. Frozen starting state

Do not discard, overwrite, or normalize the existing dirty worktree. It contains
the owner's accepted historical-scouting work and the completed common-clearing
candidate.

Starting commit:

```text
00c95849fae1784a2e7bb079543f6ef3d2871e88
```

Starting `git status --short`:

```text
 M Data/RegionalRecordData.cs
 M SimTools/ArtistPopulationLifecycleAudit.md
 M SimTools/ChartAuditRunner.cs
 M Systems/AlbumSimulator.cs
 M Systems/ChartManager.cs
 M Systems/ChartSimulator.cs
 M Systems/CompetitorManager.cs
 M Systems/RosterManager.cs
?? SimTools/ArtistPopulationHistoricalRegionalScoutingValidationHandoff.md
?? SimTools/ArtistPopulationMarketClearingAndFormatMemoryHandoff.md
?? SimTools/analyze-market-clearing-format-memory.mjs
```

`git diff --check` passed when this handoff was written.

Starting SHA-256 evidence:

```text
Systems/ChartManager.cs
4A9F66E13FCCD9FC316B5A43E2D04E3E3300DE2176B6C7F1AC2FCDFB441AA411

Systems/CompetitorManager.cs
E6A0B29F21AFB8E3A1CB73F605142FCCFA10171F7E27AF084AD29D0F3BC3E14F

Systems/AlbumSimulator.cs
CEE932F6620C4D5AAC60BB4DB75236613B04D87CD92DEB8C5256DC2A552B0A5C

Systems/ChartSimulator.cs
D93A042EC949A59809484F463D894F37BAE487AABB81D2B5A4081459E4013417

SimTools/ChartAuditRunner.cs
8CD06EE69C46997A09F1D1037BD8F81D74F2C95646C036AC242FE875EC3A911D

SimTools/analyze-market-clearing-format-memory.mjs
32DF645E16243762ED06F23919A009C3D2CA956B47AC018131395F55A9B0B2DA
```

Authoritative candidate evidence:

```text
d6-market-clearing-failfast-decade-enabled-1001
```

Authoritative immediate predecessor:

```text
d6-historical-regional-scouting-failfast-decade-enabled-1001
```

Retained control:

```text
d6-transition-envelope-decade-control-1001
```

Do not generate a replacement control if the disabled replay and retained-control
preflight remain valid.

## 3. Established diagnosis

### 3.1 Common clearing is internally correct but geographically too rigid

Every per-region allocation and inventory invariant remained clean. The failing
mid-decade formats were above their floors before clearing and fell below them
afterward:

| Year/format | Serviceable intent/control | Cleared units/control |
|---|---:|---:|
| 1964 Album | 0.9278x | 0.7766x |
| 1965 Single | 0.9395x | 0.8319x |
| 1965 Album | 0.8144x | 0.7092x |
| 1966 Album | 0.8224x | 0.7853x |

In 1965, approximately 18.89M units of serviceable demand were displaced in
overloaded regions while 8.87M purchase opportunities remained unused elsewhere.
Only 4.54M additional annual units were needed to clear the inherited total-unit
floor. East Coast was full and Great Lakes was nearly full; the remaining capacity
was geographically stranded.

A fixed-state national-pool counterfactual repaired most aggregate mid-decade
floors, but left 1965 Albums near `0.755x` and pushed 1969 Singles near `1.168x`.
Therefore the correct intervention is bounded spillover, not national pooling.

No static capacity multiplier solves the decade. Fixing 1965 Albums by capacity
alone required approximately `1.706`, which pushed 1969 Singles above `1.21x`.
Keep the base regional capacity multiplier at `1.34`.

### 3.2 Absolute retirement-time memory is stale

The current decision path blends the era-aware prior toward an absolute historical
net EMA. Album outcomes reach that EMA much later than Single outcomes:

```text
Singles: approximately 18-21 weeks from release to retirement
Albums:  45-142 week annual medians during 1963-1969
```

The live `0.75` confidence ceiling helped the 1969 format mix, but it did not change
the observation model. In 1965, effective Album confidence still averaged about
`0.688`, so an old absolute dollar outcome supplied most of the projection while
the current prior supplied the minority.

The same stale signal changes sign across the era: reducing its weight decreases
Albums in 1963/64 and increases them from 1965 onward. This is phase lag, not a
stable label preference.

### 3.3 Album yield, not only Album count, remains weak

The 1965 treatment scheduled 1,831 Albums versus 1,834 in control, but that release
cohort realized approximately 4.16M fewer lifetime units. The largest deficits were
Traditional Pop, Jazz, Country, Rock & Roll, Doo Wop, and R&B. Mean pooled appeal
and launch awareness were not materially worse overall; the enabled portfolio
substituted many emerging projects with lower Album demand per project and less
launch stock.

The previous absolute-memory behavior had masked this weakness with excess Album
volume. The new memory model must not recreate that compensation.

### 3.4 The late release-count decline is endogenous

Relative to the immediate predecessor, the common-clearing candidate ended 1969
with:

```text
label cash:          -13.5%
total roster:        1,590 versus 1,680
release rolls:       2,913 versus 3,035
additional defunct labels: 22
```

The scheduled-Album ratio in 1969 is approximately the product of `0.874x` total
decisions and a `0.908x` Album-share ratio. Memory, cash, label status, roster
throughput, and release opportunity therefore form one feedback system.

### 3.5 Psych-rock hypothesis

Stale format memory is a plausible **amplifier**, but is not yet demonstrated as
the primary cause of the 1969 psychedelic-rock peak.

Format memory is label-wide, not genre-specific. It cannot directly select
Psychedelic Rock. It can, however:

1. preserve or suppress an Album-heavy release posture based on economics from one
   or more years earlier;
2. change the number of Album projects available when Psychedelic Rock acceptance
   and momentum are peaking; and
3. change label cash, roster survival, restocking, and chart opportunity for the
   labels carrying that genre.

The genre system still determines which genre captures the available Album supply.
The new telemetry must measure this mediation. Do not change genre acceptance,
momentum, catalog, keyframes, supply, or routing in this pass.

### 3.6 The decade's hard failure is an audit/settlement seam

`ChartManager.OnWeekEnded` simulates sales and then culls dead records before
`ChartAuditRunner` captures its aggregate record snapshot. Clearing includes the
final units of records removed that week; `weeks.csv` and `market-revenue.csv` do
not.

The first mismatch is week 5 of 1960:

```text
clearing:              1,920,707 units
weeks/market-revenue:  1,920,691 units
delta:                         16 units
```

Across 1960-1969 the omitted amount is 1,110,797 units, about `0.0796%` of clearing
volume. This does not explain the behavioral failures, but it correctly caused the
analyzer to fail closed. The next candidate must not rely on post-cull active-record
enumeration for completed-week economics.

## 4. Required implementation

### 4.1 Establish a completed-week settlement ledger first

Create one immutable completed-week ledger after all regional sales are committed
and before any record is culled. It must be the authoritative source for:

- units by record, region, format, label, and tier;
- serviceable intent, local clearing, spillover clearing, and displacement;
- gross, label net, distribution income, and market net;
- physical backorders and inventory movement; and
- the record IDs retired after that settlement.

`weeks.csv`, `market-revenue.csv`, annual rollups, clearing telemetry, label finance,
and the analyzer must reconcile to this ledger. Do not reconstruct a completed week
from the remaining `allRecords` collection.

Resolve the existing event-order seam explicitly. `CompetitorManager` currently
processes prior-week revenue from its `OnWeekEnded` callback before
`ChartManager` simulates the new week. A record retired after its final sales can
therefore disappear before those final sales are booked on the next tick.

Preferred contract:

1. `ChartManager` completes the week and freezes the settlement ledger.
2. A deterministic, non-RNG settlement event books that ledger exactly once.
3. Audit writers capture the same ledger.
4. Records may then be culled.
5. The next week's release decisions see the completed prior-week settlement, as
   they do conceptually today.

An equivalent explicit pull/acknowledgement contract is allowed. Subscriber order
alone is not sufficient proof. Add a settlement ID or week ID and reject duplicate
booking.

### 4.2 Preserve local common clearing as stage A

Keep the accepted live intent pass:

- demand intent is computed in the existing record-major/region-minor order;
- existing sales-jitter RNG draws remain in that order;
- serviceable intent remains clamped to stock and store capacity;
- Singles, Albums, player records, and AI records compete in the same regional
  pool;
- local allocation remains proportional with largest-remainder integer assignment
  and ordinal `recordId` tie-breaking; and
- the base regional capacity remains
  `round(regionalBuyingPopulation * 1.34)`.

Disabled and prewarm paths must remain unchanged.

### 4.3 Add bounded neighbor spillover as stage B

After local clearing, compute for each region:

```text
unusedLocalCapacity
residualServiceableDemand
exportBudget
importLimit
```

Use the existing undirected neighbor graph returned by
`ChartManager.GetNeighborRegionIds`. Spillover is one-hop only. A region may donate
unused purchase opportunities to an adjacent overloaded region, but there is no
global pool and no multi-hop forwarding in the same week.

Reference initial bounds:

```text
maximum exported share of a donor's unused local capacity: 0.50
maximum imported capacity as a share of recipient base capacity: 0.15
```

These are market-wide constants, not year-, format-, tier-, label-, or
genre-specific values. Keep `1.34` unchanged.

Compute the donor-to-recipient transfer matrix simultaneously, not through a
region-order greedy loop. A deterministic max-flow or equivalent progressive
water-filling solution over the sorted neighbor graph is preferred:

- donor node capacity is its export budget;
- recipient node capacity is
  `min(residualServiceableDemand, importLimit)`;
- only existing neighbor edges are eligible;
- graph nodes and edges use ordinal `regionId` ordering; and
- the solution maximizes eligible transfer without exceeding any bound.

Within each recipient, allocate imported capacity over its remaining
record-region intents using the same proportional/largest-remainder rule and
ordinal `recordId` tie-break as local clearing.

The purchase remains attributed to the recipient demand region and its existing
inventory. The donor supplies a portable purchase opportunity, not record stock.
No record may receive spillover above its remaining serviceable intent or physical
inventory.

Required invariants:

```text
localCleared <= baseRegionalCapacity
exported <= exportBudget <= unusedLocalCapacity
imported <= importLimit
imported <= residualServiceableDemand
sum(exported across donors) == sum(imported across recipients)
finalCleared == localCleared + spilloverCleared
finalCleared <= serviceableIntent
nationalFinalCleared <= sum(baseRegionalCapacity)
every positive transfer follows one configured neighbor edge
no unit is allocated twice
```

Do not add format reservations, genre lanes, tier protections, player exceptions,
or a guaranteed utilization floor.

The implementing Codex may replace the reference `0.50/0.15` bounds with a more
principled market-wide bounded rule if fixed probes or the first checkpoint
demonstrate a structural defect. It must document the reason and prove that the
replacement cannot collapse into national pooling. Do not grid-search constants
against annual gates.

### 4.4 Replace absolute memory with normalized residual memory

The stored live memory signal must be dimensionless and centered on zero:

```text
0  = the release performed as expected for its contemporaneous opportunity
>0 = it outperformed that expectation
<0 = it underperformed that expectation
```

At format decision time, capture enough immutable context for both candidate
formats to reconstruct the expectation that informed the decision. At minimum:

- decision week/year and label;
- candidate format and genre/project identity;
- deterministic current prior before memory;
- expected production, marketing, and distribution economics;
- release-time market/opportunity scale;
- label reach/tier context used by the prior; and
- whether `ProjectPrior` bypassed label-format memory.

For the chosen release, create one mutable outcome observation keyed by stable
release/project ID. A reference normalized residual is:

```text
normalizedResidual =
    clamp((estimatedOutcomeNet - releaseTimeExpectedNet) / opportunityScale,
          -residualLimit, +residualLimit)
```

`opportunityScale` must be strictly positive and derived from release-time
economics/opportunity, not from a retained-control outcome. An equivalent signed
ROI, log-ratio, or robust residual is allowed if it:

- remains valid when expected or realized net is negative;
- is comparable across eras, formats, and label tiers;
- does not encode absolute historical dollars as the future projection; and
- has bounded influence from one extreme release.

Apply memory to the current prior as a residual adjustment, not as a replacement:

```text
projectedCurrentNet =
    currentPriorNet +
    effectiveConfidence * recentResidual * currentOpportunityScale
```

The exact robust estimator may differ, but it must preserve the current prior as
the center of the decision.

### 4.5 Make Album observations timely and revision-safe

Do not wait for retirement before an Album contributes any evidence.

Minimum lifecycle:

- create the observation at release;
- publish a provisional, maturity-weighted residual no later than age 13 weeks;
- revise it at age 26 and, for still-active Albums, age 52;
- finalize it at retirement or project closure; and
- replace the prior contribution for that release rather than counting the same
  release as a new independent observation at every checkpoint.

A weekly revision model is allowed if it is deterministic and bounded. Provisional
outcomes must compare realized-to-date economics with an age-matched expectation or
use a documented terminal estimate; do not compare partial Album net directly with
an undiscounted lifetime prior.

Maintain a bounded recent observation window or explicit recency decay. Reference
behavior:

```text
recency half-life:             no more than 52 weeks
maximum effective history:    no more than 104 weeks
maximum memory confidence:    0.65
minimum current-prior weight:  0.35
```

Confidence must be based on effective weighted observations, not raw lifetime
release count. An equivalent robust scheme is allowed if:

- an observation several years old cannot dominate a current decision;
- one release cannot be duplicated by provisional revisions;
- `ProjectPrior` continues to bypass label-format memory;
- decision noise and its RNG order remain unchanged; and
- the adjustment is finite and bounded under losses, zero opportunity, sparse
  labels, and extreme hits.

Do not retain the old absolute EMA as an additional blended input. It may be kept
temporarily for diagnostic comparison only.

### 4.6 Preserve causal separation

Spillover and memory solve different problems:

- spillover changes which serviceable units can clear;
- memory changes future format choices from normalized historical evidence.

Do not feed market-displaced units into physical backorders or restocking. Do not
use spillover utilization directly as a format preference. If clearing pressure is
part of the release-time opportunity baseline, include it symmetrically in the
forecast and outcome context rather than applying an Album correction.

## 5. Telemetry and analysis

Enabled-only telemetry may evolve. The 45 disabled schemas and stream set remain
frozen.

### 5.1 Completed-week settlement

Add a compact settlement stream or extend an enabled-only stream with:

```text
week,year,settlementId,recordId,labelId,labelTier,format,genre,
regionalUnits,totalUnits,gross,labelNet,distributionIncome,marketNet,
retiredAfterSettlement,bookedCount,auditedCount
```

If one row per record would be excessive, emit exact weekly aggregates plus a
retirement-detail stream. The analyzer must still prove that every settled record,
including records culled that week, was booked and audited exactly once.

### 5.2 Spillover telemetry

Extend `market-clearing-weekly.csv` with:

```text
baseCapacity,localCleared,unusedAfterLocal,exportBudget,exportedCapacity,
importLimit,importedCapacity,spilloverCleared,finalCleared,
residualDisplacedDemand,settlementDelta
```

Add `market-spillover-weekly.csv`, one row per positive edge transfer:

```text
week,year,donorRegionId,recipientRegionId,
donorUnusedLocal,donorExportBudget,recipientResidualDemand,recipientImportLimit,
transferredCapacity,clearedSingleUnits,clearedAlbumUnits,
edgeViolationCount,reconciliationDelta
```

### 5.3 Responsive-memory telemetry

Replace or extend `format-memory-adjustment.csv` so each decision reports:

```text
week,year,recordId,labelId,genre,memoryScope,
currentSinglePrior,currentAlbumPrior,
singleResidual,albumResidual,
singleEffectiveSampleWeight,albumEffectiveSampleWeight,
singleOldestObservationAge,albumOldestObservationAge,
singleEffectiveConfidence,albumEffectiveConfidence,
singleAdjustment,albumAdjustment,chosenFormat
```

Add a revision stream:

```text
week,year,releaseId,labelId,format,genre,releaseAge,
revisionKind,releaseTimeExpectedNet,ageMatchedExpectedNet,
realizedNetToDate,estimatedOutcomeNet,opportunityScale,
normalizedResidual,maturityWeight,recencyWeight,
replacedPriorRevision,finalized,nonFiniteViolation
```

### 5.4 Psych-rock mediation report

The analyzer must report, without changing genre behavior:

- Psychedelic Rock decisions and releases by year, label tier, and format;
- deterministic-prior, after-memory, and final Album shares for Psychedelic Rock
  versus all other genres;
- how many Psychedelic Rock Album choices were changed by memory relative to the
  deterministic prior;
- release counts, serviceable intent, local clearing, spillover clearing, units,
  chart entries, and label net;
- the age distribution of memory observations affecting those choices; and
- the same measures for a reasonable non-psychedelic comparison cohort.

Classify the hypothesis as:

```text
NOT_SUPPORTED
FORMAT_SUPPLY_AMPLIFIER
LABEL_FINANCE_MEDIATOR
DOMINANT_GENRE_SYSTEM_EFFECT
MIXED_OR_UNRESOLVED
```

Do not infer that a changed peak proves causality. Use the decision-stage and
settlement joins.

### 5.5 Analyzer fail-closed requirements

Replace or extend `analyze-market-clearing-format-memory.mjs`. It must fail on:

- missing or malformed enabled streams;
- duplicate or skipped settlement IDs;
- any settled record booked or audited other than exactly once;
- any mismatch among settlement, clearing, weeks, market revenue, label finance,
  and annual rollups;
- any local, edge, import/export, inventory, or allocation invariant;
- non-finite residuals, scales, weights, confidences, or adjustments;
- duplicate counting of one release's memory revisions; and
- any inherited catastrophic gate.

Header-only catastrophic output is not sufficient when the analyzer cannot
reconcile.

## 6. Fixed probes

Retain all accepted probes and add deterministic coverage for at least:

1. below-cap local demand clears without spillover;
2. an overloaded region borrows from one adjacent donor;
3. a non-neighbor with unused capacity cannot donate;
4. donor export and recipient import bounds are exact;
5. multiple donors and recipients resolve independently of collection insertion
   order;
6. spillover uses residual intent and cannot allocate one unit twice;
7. Singles and Albums compete together for spillover;
8. spillover never exceeds stock or store capacity;
9. donated capacity does not become a physical backorder or restock request;
10. the national sum never exceeds the sum of base capacities;
11. a record sold and retired in the same week remains in the settlement ledger;
12. its units and economics are booked and audited exactly once;
13. duplicate settlement acknowledgement fails closed;
14. a release creates one memory observation;
15. age-13 and age-26 revisions replace rather than duplicate that observation;
16. partial outcomes use age-matched expectations;
17. an old Album observation decays and cannot dominate after the maximum history
    horizon;
18. negative priors, negative realized net, zero opportunity, and extreme hits
    produce finite bounded residuals;
19. current conditions remain the center when normalized residual is zero;
20. `ProjectPrior` still bypasses label-format memory;
21. decision-noise RNG order is unchanged;
22. prewarm performs no clearing, spillover, or live-memory update; and
23. the disabled route emits no enabled streams and remains byte-identical.

## 7. Validation ladder

Stop at the first hard failure. Preserve exact commands, source manifests,
completion markers, stdout/stderr, stream counts, file sizes, and hashes in
`ArtistPopulationLifecycleAudit.md`.

### M0 - source and evidence review

- Record `git status --short`, `git diff --check`, starting hashes, and run
  availability.
- Reproduce the week-5 16-unit mismatch and the decade reconciliation delta before
  changing capture/settlement.
- Reproduce the annual serviceable-to-cleared ratios, regional unused/displaced
  capacity, Album observation ages, and 1969 decision decomposition stated above.
- Confirm that no genre source or data changes are required.

### M1 - build and fixed probes

```powershell
dotnet build "Label Man.sln" --no-restore
```

Run the existing combined probe command with the new settlement, spillover, and
responsive-memory probes. Require all prior probes, all new probes,
`git diff --check`, and `CHART_AUDIT_COMPLETE`.

### M2 - disabled compatibility and control preflight

Run the established 52-week disabled seed-1001 replay. Require:

- the same 45-stream suffix set as the retained disabled control;
- 45/45 suffix-matched SHA-256 equality;
- no settlement, spillover, or responsive-memory stream; and
- the retained decade-control preflight to pass without simulation.

### M3 - 104-week enabled checkpoint and deterministic repeat

Run two independent 104-week seed-1001 treatments with different run names and
otherwise identical arguments.

Require:

- all settlement, clearing, edge, inventory, and memory invariants;
- zero completed-week reconciliation delta;
- nonzero local rationing and nonzero bounded spillover;
- exact equality of every deterministic comparable stream;
- no disabled/prewarm contamination; and
- inherited release, Album, format-unit, and economic ratios inside their ordinary
  bands for both completed years.

Performance timing remains the expected repeat exception.

### M4 - date-complete 1960-1965 checkpoint

Run a date-complete seed-1001 candidate through the end of calendar 1965. Prefer an
explicit end-date option; otherwise use the smallest established Friday-tick count
that includes the complete 1965 annual row.

This checkpoint is intentionally longer than the previous 260-week gate because
the strongest spillover and Album-yield collision occurs in 1965.

Require:

- zero hard reconciliation or structural failure;
- every completed-year successful-release and scheduled-Album ratio inside
  `[0.85,1.15]`;
- every completed-year Single, Album, and total-unit ratio inside inherited bands;
- every completed-year gross, label-net, and market-net ratio inside inherited
  bands;
- 1964 Album ratio at least `0.80`;
- 1965 Single ratio at least `0.85`;
- 1965 Album ratio at least `0.80`;
- 1965 total units and economic floors at least `0.85`;
- bounded spillover, never national-pool behavior;
- median first provisional Album observation age no greater than 13 weeks;
- no active Album older than 26 weeks without at least one valid provisional
  observation;
- effective memory confidence never above the declared bound; and
- zero duplicated revisions or non-finite memory state.

Report pre-clearing, local-clearing, spillover, and final ratios separately. A pass
caused by release-count inflation while units per Album continue to collapse is not
accepted; join count and yield by tier and genre.

### M5 - one reconciled seed-1001 decade

Only after M4 passes, run one 522-Friday enabled seed-1001 decade against the
retained control.

Require:

- normal completion and header-only catastrophic output;
- the analyzer itself passes every hard reconciliation;
- all inherited annual and decade release, scheduled-Album, format-unit, total,
  gross, label-net, and market-net gates;
- 1966 Album ratio at least `0.80`;
- 1969 scheduled-Album share inside the owner-approved inclusive `[0.78,0.85]`;
- 1969 scheduled-Album/control ratio at least `0.80`;
- no annual Single, Album, or total ratio above its inherited upper band;
- zero settlement, spillover, allocation, inventory, chronology, ownership,
  lifecycle, and non-finite violations; and
- explicit Psych-rock mediation classification with evidence.

Do not proceed to seeds 1002/1003 or a holdout under this handoff. Return the
completed M5 evidence to the owner.

## 8. Authorized implementation latitude

The implementing Codex may make significant changes when needed to satisfy the
model contract, including:

- extracting a reusable deterministic proportional allocator;
- adding a deterministic bounded-flow solver for the region graph;
- introducing immutable weekly settlement and per-release memory observation
  types;
- changing finance handoff from implicit callback timing to explicit settlement;
- replacing the old EMA state and migrating or discarding live-only transient
  memory at simulation initialization;
- revising enabled telemetry schemas and the enabled analyzer; and
- adding compact diagnostic streams needed to prove causality.

The implementing Codex may perform one evidence-driven structural correction after
the first M4 result. This is not authority for scalar sweeps. If the reference
memory residual or spillover bounds reveal a genuine design defect, revise the
model, rerun M1-M4, freeze the resulting source, and only then run M5.

The following remain prohibited:

- genre-specific, year-specific, label-specific, or format-specific clearing
  capacity;
- Album quotas or Single penalties;
- fitting against individual retained-control annual values;
- changing genre acceptance, supply, momentum, catalog, or keyframes;
- changing artist formation, scouting, roster, or release-rate policy to repair a
  clearing/memory failure;
- changing inherited acceptance bands; and
- treating a header-only catastrophic CSV as success when reconciliation fails.

## 9. Completion record

Append to `ArtistPopulationLifecycleAudit.md`:

- source hashes before and after;
- a concise architecture note for settlement, bounded flow, and normalized memory;
- fixed-probe results;
- disabled equality and repeat hashes;
- annual pre-clearing/local/spillover/final tables;
- spillover by donor/recipient and format;
- memory age, residual, confidence, and revision distributions;
- count-versus-yield tables by tier and genre;
- finance, label-status, roster, and release-roll feedback;
- the Psych-rock mediation report;
- every inherited gate; and
- the exact stop or acceptance decision.

Suggested run family:

```text
d6-bounded-spillover-memory-probes-1001
d6-bounded-spillover-memory-disabled-52-1001
d6-bounded-spillover-memory-enabled-104-1001
d6-bounded-spillover-memory-enabled-repeat-104-1001
d6-bounded-spillover-memory-through-1965-1001
d6-bounded-spillover-memory-decade-enabled-1001
```

The completion criterion is a reconciled market whose geography can flex without
becoming national, and whose labels learn promptly from relative performance
without carrying obsolete absolute economics across the decade.
