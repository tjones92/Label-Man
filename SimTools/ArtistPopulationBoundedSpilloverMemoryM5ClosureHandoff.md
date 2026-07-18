# Bounded Spillover, Settlement, and Responsive-Memory M5 Closure Handoff

Status: **OWNER AUTHORIZED / EXECUTE THROUGH M5 WITHOUT ANOTHER HANDOFF**

Date: 2026-07-18

Scope owner: the next Codex implementation and validation pass.

This document is a closure authorization, not a request for another diagnostic
checkpoint. It supplements
`ArtistPopulationBoundedSpilloverAndResponsiveFormatMemoryHandoff.md` and
supersedes that document where validation sequencing, stopping behavior, or
analyzer completeness differs.

The behavioral contract in the earlier handoff remains binding:

- common local clearing remains stage A;
- one-hop bounded neighbor spillover remains stage B;
- the base regional capacity multiplier remains `1.34`;
- responsive format memory remains a bounded normalized-residual adjustment to
  the current prior;
- disabled and prewarm behavior remain frozen; and
- genre acceptance, momentum, supply, catalog, routing, and keyframes remain
  protected.

The owner explicitly directs Codex to finish the implementation defects described
below and proceed autonomously through M1, M2, M3, M4, and the seed-1001 M5
decade. A successful intermediate checkpoint is not a reason to stop, summarize,
or request another handoff.

## 1. Governing execution instruction

The required terminal outcome is one of:

1. **M5 PASS** with a fully reconciled seed-1001 decade and complete audit record;
   or
2. **FINAL BLOCKED RESULT** proving that the decade cannot be reached without
   crossing a protected surface or exceeding the bounded correction authority in
   section 8.

Do not create or request another handoff after M1, M2, M3, or M4. Continue directly
to the next checkpoint when the current checkpoint passes.

Implementation and analyzer defects are not acceptance results. Fix them, rerun
the earliest invalidated checkpoint, and continue. Preserve failed artifacts under
unique run names; never overwrite evidence.

Long runtime is not a stop condition. Use a sufficiently long foreground timeout
or a hidden background process with redirected stdout/stderr and poll it at
intervals shorter than 60 seconds. Require the explicit completion marker and
process exit status; file presence alone is not completion.

## 2. Frozen starting state and evidence

Starting commit:

```text
00c95849fae1784a2e7bb079543f6ef3d2871e88
```

Starting dirty files:

```text
 M Data/AILabel.cs
 M Data/RecordRuntimeData.cs
 M SimTools/ChartAuditRunner.cs
 M SimTools/analyze-market-clearing-format-memory.mjs
 M Systems/ChartManager.cs
 M Systems/CompetitorManager.cs
```

Do not discard, reset, normalize, or overwrite this worktree. Preserve all
unrelated owner changes.

Starting SHA-256 values:

```text
Data/AILabel.cs
9A51E449BE2891E29F4708DD60693A9CEDE62123A1C06FC632D7651331BE0DA7

Data/RecordRuntimeData.cs
2B17DB2E42CB49D092997CB835D92DD06E5318EFBB183548BAEB6ECBDD639CC3

SimTools/ChartAuditRunner.cs
0ADDF6647828ABE77F86E975C3F51B0DE2B1DE7E1BE81A4DFFF9C1F21D2DD175

SimTools/analyze-market-clearing-format-memory.mjs
AEB772CE3221591C32B8E44C647D39401999530144D6CBB758B72E3A97AB1B97

Systems/ChartManager.cs
990CB14068780A7FAA512902EDE5DEE0E112AE3B6AFEBB51915C967805F36DCB

Systems/CompetitorManager.cs
3598AC36B5EF16BF458BDB7E4CEA9A46CB6E66347170F326F4417FAD5C2E6B04
```

`git diff --check` passes at this boundary.

Retained disabled reference:

```text
d6-market-clearing-disabled-52-1001
```

Retained decade control:

```text
d6-transition-envelope-decade-control-1001
```

Current enabled diagnostic:

```text
d6-bounded-spillover-memory-enabled-104-1001
```

It completed 104 weeks normally and emitted 63 CSV streams. Its catastrophic file
is header-only. Its 1960 and 1961 annual ratios are within the inherited bands:

| Year | Releases | Scheduled Albums | Single units | Album units | Total units | Gross | Label net | Market net |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 0.9874 | 0.9973 | 0.9503 | 0.9746 | 0.9506 | 0.9636 | 0.9597 | 0.9617 |
| 1961 | 0.9175 | 1.1208 | 0.9441 | 1.0408 | 0.9470 | 0.9545 | 0.9566 | 0.9531 |

The current analyzer reports eight false decade failures because it aggregates
these two completed years against the full retained decade. That reporting defect
must be fixed, but the diagnostic is not yet an acceptable M3 artifact for the
independent reasons in section 3.

## 3. Mandatory pre-M3 closure findings

These are established code/data findings. Do not spend another task merely
rediscovering them.

### 3.1 The settlement CSV is not yet an economic settlement ledger

`ChartAuditRunner.OnWeekSettlement` currently:

- writes `gross` as literal `0`;
- writes `distributionIncome` as literal `0`;
- writes `record.lifetimeLabelNet`, a cumulative value, as the week's `labelNet`;
- copies that cumulative value to `marketNet`;
- writes `retiredAfterSettlement` as literal `false`; and
- writes `bookedCount` and `auditedCount` as literal `1`.

Observed evidence:

```text
week 1 settlement gross:          0
week 1 market-revenue gross:      1,322,244.469131

week 2 settlement label net:      1,556,435.882990
week 2 market-revenue label net:    833,721.766153
delta:                              722,714.116837
```

Settlement units do reconcile to clearing for all 104 weeks. The monetary columns
do not. Therefore the current artifact proves the unit/culling repair but not the
required economic settlement contract.

### 3.2 Booking and auditing still depend on subscriber order

`ChartManager` freezes unit references and invokes `OnWeekSettlement`.
`CompetitorManager` and `ChartAuditRunner` subscribe independently. There is no
strict settlement-ID acknowledgement state and no duplicate-booking rejection in
the production path.

Subscriber registration order is not proof. The final design must expose an
explicit deterministic sequence:

```text
units frozen
-> economics booked exactly once
-> immutable settlement finalized
-> audit acknowledged exactly once
-> retirement/culling completed against that settlement identity
```

### 3.3 The analyzer parses new streams but does not adjudicate them

`parseSettlementAndResponsiveMemory()` returns a value assigned to
`settlementAndMemory`, but that result is not joined to clearing, weeks,
market-revenue, label-finance, or annual rollups.

The analyzer also does not yet:

- parse or validate `market-spillover-weekly.csv`;
- prove every positive edge is a configured neighbor;
- reconcile donor exports to recipient imports;
- prove per-record settlement uniqueness;
- prove actual booking/audit acknowledgement counts;
- validate revision replacement as one observation per release;
- prove the 104-week memory horizon or the declared `0.65` confidence ceiling;
- report provisional Album age compliance; or
- produce the required Psych-rock mediation report.

`GENRE_DIAGNOSTICS_DEFERRED` is not an acceptable M5 result.

### 3.4 The required new fixed probes are not present

The inherited combined probe command reportedly passes, but the probe suite has
not been extended to cover the new settlement, bounded-flow, and responsive-memory
contract. M1 is incomplete until those probes exist and pass.

## 4. Required settlement architecture

Implement an explicit settlement state machine. Equivalent architecture is
allowed, but it must prove the following behavior without relying on event
subscriber order.

### 4.1 Settlement identity and acknowledgement

For live enabled weeks:

1. `ChartManager` freezes the week's unit allocation into a new settlement with a
   strictly increasing positive `settlementId`.
2. One explicit booking operation calculates and applies weekly economics.
3. The booking operation rejects an already-booked, skipped, stale, or
   out-of-order settlement ID.
4. The finalized settlement contains immutable weekly economics, not references
   that must be reread after later mutation.
5. The audit consumer acknowledges that exact settlement once.
6. Duplicate, skipped, stale, or out-of-order audit acknowledgement fails closed.
7. Records may be removed only after their final units and economics belong to
   the finalized settlement.

A direct booking call followed by a post-booking audit event is acceptable. A
two-phase pull/acknowledgement API is also acceptable. Two unordered subscribers
to the same pre-booking event are not acceptable.

Legacy disabled and prewarm behavior must retain their existing timing, RNG order,
schemas, and stream set.

### 4.2 Unit and regional detail

The ledger must retain enough immutable detail to prove units by:

```text
settlement, week, year, record, region, format, genre, label, and tier
```

For each record/region allocation retain, directly or in a companion enabled-only
stream:

```text
raw intent
serviceable intent
local cleared
spillover cleared
final cleared
physical backorders
market-displaced demand
inventory movement
```

The existing record-level `completed-week-settlement.csv` may remain as a compact
summary, but `regionalUnits` cannot merely repeat `totalUnits`. Add a
record-region stream if that is clearer and smaller than encoding a regional map
in one field.

Each finalized record summary must expose the actual
`retiredAfterSettlement` result. A deterministic pre-cull decision using the same
predicate or a post-cull finalization step keyed to the already-frozen settlement
is acceptable.

### 4.3 Weekly economics

Capture the actual weekly amounts calculated during booking. At minimum retain:

```text
retail gross
manufacturing/packaging cost
artist royalty
distribution skim
client label net
distribution recipient label ID
distribution income
market net
```

The ledger must support both record economics and label-finance reconciliation.
Where distribution income is paid to a different label, retain the recipient
identity or emit immutable finance legs keyed to `settlementId` and `recordId`.

Required equations include:

```text
record market net == client label net + distribution income
weekly format sums == market-revenue weekly format rows
weekly All sums == market-revenue weekly All/All
weekly label legs == label-finance weekly values
completed-year sums == market-revenue annual rows
completed-year format units/gross/net == decade-annual-rollup
```

Use the existing cent-level tolerance only for floating-point summation order.
Units and counts require exact integer equality.

### 4.4 Required settlement invariants

The production code, probes, telemetry, and analyzer must collectively prove:

```text
one settlement ID per completed live week
IDs are contiguous from 1 through the final completed week
one record summary per settlementId/recordId
one regional row per settlementId/recordId/regionId
every finalized row booked exactly once
every finalized row audited exactly once
no record is booked after retirement
same-week sold-and-retired records remain present
sum regional final units == record total units
sum record total units == clearing final units
all finance legs balance
no duplicate booking or acknowledgement can mutate cash twice
```

## 5. Analyzer closure requirements

Extend `SimTools/analyze-market-clearing-format-memory.mjs` as one fail-closed
adjudicator for M3, M4, and M5. Do not create a permissive side analyzer that can
pass while this analyzer still omits required joins.

### 5.1 Checkpoint-aware annual and decade gates

Derive completed candidate years from exact complete annual evidence. Validate that
they are contiguous from 1960 and that matching control summaries exist.

Apply annual gates to every completed candidate year.

Define a date-complete decade only when all target years 1960 through 1969 have
complete candidate annual rollup and market-revenue rows. Only then:

- aggregate and gate decade ratios;
- apply the 1969 scheduled-Album share gate;
- print the decade control-ratio table; and
- emit M5-only reporting.

For M3 and M4, print an explicit line such as:

```text
Decade gates: NOT_APPLICABLE; completed candidate years: 1960-1961
```

Do not compute two-year/six-year totals against the ten-year control. Replace the
hardcoded `1960-1969` reconciliation sentence with the actual completed-year
range.

Structural, settlement, spillover, memory, lifecycle, ownership, finance, and
catastrophic failures remain active at every checkpoint.

### 5.2 Settlement reconciliation

Fail on:

- missing, duplicate, skipped, stale, or out-of-order settlements;
- missing or duplicate record and regional identities;
- any actual booked/audited count other than one;
- any unit mismatch among settlement, regional detail, clearing, weeks, and
  market-revenue;
- any economic mismatch among settlement, market-revenue, label-finance, and
  annual rollup;
- any non-finite monetary field;
- any invalid finance identity or unbalanced distribution leg; and
- any retired record absent from its final settlement.

Report row counts and exact maximum absolute deltas for every join.

### 5.3 Spillover reconciliation

Parse `market-spillover-weekly.csv`. The accepted undirected graph is:

```text
eastcoast:   greatlakes, deepsouth
greatlakes:  eastcoast, deepsouth, greatplains
greatplains: greatlakes, rockies, southwest
deepsouth:   eastcoast, greatlakes, southwest
southwest:   deepsouth, rockies, westcoast, greatplains
rockies:     greatplains, southwest, westcoast
westcoast:   rockies, southwest
```

Fail on:

- any non-neighbor positive edge;
- duplicate donor/recipient/week rows;
- exported capacity above donor budget or donor unused local capacity;
- imported capacity above recipient limit or residual demand;
- edge transfer totals that do not equal regional exports/imports;
- edge Single plus Album clearing that does not equal transfer clearing;
- local clearing above base capacity;
- final clearing not equal to local plus spillover;
- national final clearing above national base capacity;
- nonzero edge or reconciliation violations; or
- zero spillover across a checkpoint that otherwise contains local rationing.

Report local, spillover, and final units separately by year, region, format, tier,
and genre where the ledger supports those dimensions.

### 5.4 Responsive-memory reconciliation

Extend the decision telemetry if needed so the analyzer can observe:

```text
current Single and Album priors
Single and Album residuals
effective observation weights
oldest contributing observation ages
effective confidences
bounded adjustments
chosen format
memory scope
```

Revision telemetry must identify one stable observation per release and an
unambiguous revision sequence. If a provisional and final revision can share an
age, add an ordinal so their replacement order is provable.

Fail on:

- a missing observation creation for an eligible release;
- more than one live observation identity for a release;
- a first revision incorrectly marked as replacement;
- a later revision that does not replace the prior contribution;
- revision after finalization;
- more than one finalization;
- a provisional Album observation later than age 13;
- an active Album older than 26 weeks without a valid provisional observation;
- any effective observation older than 104 weeks;
- effective label-format confidence above `0.65`;
- nonzero memory confidence or adjustment under `ProjectPrior`;
- non-finite residual, scale, weight, confidence, prior, estimate, or adjustment;
- non-positive opportunity scale;
- residual outside the declared bound;
- partial outcome compared directly with an undiscounted lifetime prior; or
- duplicated effective weight from multiple revisions of one release.

Report revision ages, residual distributions, effective weights, oldest ages, and
confidence by year and format.

### 5.5 Psych-rock mediation

Implement the earlier handoff's Psych-rock mediation report before M5. Add compact
enabled telemetry if existing streams cannot support the joins. Do not change
genre behavior.

For Psychedelic Rock and a documented non-psychedelic comparison cohort, report by
year, label tier, and format:

- deterministic-prior, after-memory, and final decisions;
- decisions whose format changed because of memory;
- successful releases;
- serviceable intent;
- local, spillover, and final clearing;
- units, chart entries, and label net;
- contributing observation age and confidence; and
- label cash/status/roster mediation where evidence supports it.

End the date-complete M5 report with exactly one evidence-supported
classification:

```text
NOT_SUPPORTED
FORMAT_SUPPLY_AMPLIFIER
LABEL_FINANCE_MEDIATOR
DOMINANT_GENRE_SYSTEM_EFFECT
MIXED_OR_UNRESOLVED
```

Do not claim causality from peak movement alone.

## 6. Fixed probes required for M1

Add deterministic probe coverage for every item below. The probes may live in a
new focused suite, but the established combined probe command must invoke them and
print explicit pass markers.

1. Below-cap local demand clears without spillover.
2. An overloaded region borrows from one adjacent donor.
3. A non-neighbor cannot donate.
4. Donor export and recipient import bounds are exact.
5. Multiple donors/recipients are insertion-order independent.
6. Spillover uses residual intent and cannot allocate one unit twice.
7. Singles and Albums compete together for spillover.
8. Spillover cannot exceed stock or store capacity.
9. Donated capacity creates neither physical backorders nor restock demand.
10. National clearing cannot exceed national base capacity.
11. A record sold and retired in the same week remains in the ledger.
12. Its units and economics are booked and audited exactly once.
13. Duplicate booking fails before any second cash mutation.
14. Duplicate audit acknowledgement fails closed.
15. A release creates one responsive-memory observation.
16. Age-13 and age-26 revisions replace rather than duplicate it.
17. An age-52 Album revision replaces rather than duplicates it.
18. Partial outcomes use an age-matched expectation.
19. An observation older than 104 weeks cannot affect a decision.
20. Negative priors, negative realized net, zero raw opportunity, and extreme hits
    produce finite bounded state.
21. A zero normalized residual leaves the current prior at the center.
22. `ProjectPrior` bypasses label-format memory.
23. Decision-noise RNG order is unchanged.
24. Prewarm performs no clearing, spillover, settlement booking, or live-memory
    update.
25. Disabled mode emits none of the enabled streams and remains byte-identical.

Retain all inherited D5 and D6 probes. A test helper that merely returns the
expected constant is not proof; exercise the production allocator, booking guard,
and memory revision logic.

## 7. Required M1-M5 execution ladder

Use new run names. Do not overwrite the current diagnostic or any retained
reference.

Recommended PowerShell setup:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$node = 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
$control = 'd6-transition-envelope-decade-control-1001'
```

If any implementation or bounded behavioral correction is made, use a new
family suffix such as `-r2`; never reuse a prior prefix.

### M1 - build, diff hygiene, and complete fixed probes

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-bounded-spillover-memory-closure-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes --lean-probe
```

Require:

- build exit zero;
- no new compiler warning;
- `git diff --check` exit zero;
- every inherited and new probe marker;
- normal `CHART_AUDIT_COMPLETE`; and
- no non-fatal probe exception hidden in stderr.

When M1 passes, continue immediately to M2.

### M2 - disabled replay and retained-control preflight

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-bounded-spillover-memory-closure-disabled-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --run=d6-bounded-spillover-memory-closure-control-preflight-1001 --seed=1001 --catastrophic-control-preflight --gate-control-run=$control
```

Require:

- normal completion;
- exactly the retained 45-stream suffix set;
- 45/45 suffix-matched length and SHA-256 equality to
  `d6-market-clearing-disabled-52-1001`;
- no enabled settlement, regional-settlement, spillover, or responsive-memory
  stream; and
- retained decade-control preflight exit zero.

When M2 passes, continue immediately to M3.

### M3 - fresh 104-week candidate and deterministic repeat

The existing 104-week run is diagnostic only because its settlement economics are
placeholders. Produce two fresh artifacts from the finalized source:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-bounded-spillover-memory-closure-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-bounded-spillover-memory-closure-enabled-repeat-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Run the analyzer on both:

```powershell
& $node SimTools/analyze-market-clearing-format-memory.mjs SimLogs d6-bounded-spillover-memory-closure-enabled-104-1001 $control
& $node SimTools/analyze-market-clearing-format-memory.mjs SimLogs d6-bounded-spillover-memory-closure-enabled-repeat-104-1001 $control
```

Require:

- both completion markers and exit zero;
- both analyzer exits zero;
- header-only catastrophic output;
- exact settlement/economic reconciliation;
- all spillover and memory invariants;
- nonzero local rationing and nonzero bounded spillover;
- 1960 and 1961 annual gates inside their inherited bands;
- identical suffix sets; and
- byte-identical deterministic comparable streams by suffix, length, and SHA-256.

`performance-profile.csv` timing values are the expected repeat exception. Report
the exclusion explicitly; do not silently omit any other mismatch.

When M3 passes, continue immediately to M4.

### M4 - date-complete through 1965

With the current Friday calendar, the retained control contains 52 ticks for each
of 1960-1964 and 53 for 1965. The smallest established checkpoint containing the
complete 1965 annual row is therefore 313 ticks.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=313 --run=d6-bounded-spillover-memory-closure-through-1965-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
& $node SimTools/analyze-market-clearing-format-memory.mjs SimLogs d6-bounded-spillover-memory-closure-through-1965-1001 $control
```

Verify the artifact actually contains complete annual rows 1960-1965 before
adjudication. Require:

- normal completion and analyzer exit zero;
- zero hard structural or reconciliation failure;
- every completed-year release, scheduled-Album, format-unit, total-unit, gross,
  label-net, and market-net ratio within inherited bands;
- 1964 Album units at least `0.80x`;
- 1965 Single units at least `0.85x`;
- 1965 Album units at least `0.80x`;
- 1965 total units, gross, label net, and market net at least `0.85x`;
- bounded one-hop spillover, never national pooling;
- median first provisional Album observation age no greater than 13 weeks;
- no active Album older than 26 weeks without a valid provisional observation;
- confidence no greater than `0.65`;
- no duplicated effective observation or non-finite state; and
- count-versus-yield reporting by tier and genre.

The analyzer must mark decade gates `NOT_APPLICABLE`; that is expected at M4.

When M4 passes, freeze the source and continue immediately to M5.

### M5 - one fully reconciled seed-1001 decade

Do not run M5 from a source that differs from accepted M4. Record hashes before
launch.

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-bounded-spillover-memory-closure-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
& $node SimTools/analyze-market-clearing-format-memory.mjs SimLogs d6-bounded-spillover-memory-closure-decade-enabled-1001 $control
```

Require:

- normal `CHART_AUDIT_COMPLETE` and process exit zero;
- header-only catastrophic output;
- analyzer exit zero;
- complete annual rows 1960-1969;
- all annual and decade release, scheduled-Album, Single-unit, Album-unit,
  total-unit, gross, label-net, and market-net gates;
- 1966 Album units at least `0.80x`;
- 1969 scheduled-Album share inside inclusive `[0.78,0.85]`;
- 1969 scheduled-Album count at least `0.80x` control;
- no annual Single, Album, or total-unit ratio above its inherited upper band;
- zero settlement, booking, audit, spillover, allocation, inventory, chronology,
  ownership, lifecycle, finance, and non-finite violations; and
- one explicit Psych-rock mediation classification with supporting tables.

Do not launch seeds 1002/1003 or a holdout under this handoff.

## 8. Autonomous correction authority

The purpose of this section is to avoid another permission handoff while
preserving causal discipline.

### 8.1 Implementation and audit defects

Codex is authorized to fix any number of demonstrated implementation, telemetry,
probe, analyzer, settlement-order, idempotency, or reconciliation defects within
the architecture above. These fixes do not consume the behavioral correction
budget.

After a fix, rerun from the earliest checkpoint whose evidence was invalidated.
Examples:

- analyzer-only reporting fix: rerun the analyzer and any checkpoint whose verdict
  depended on it;
- telemetry or settlement-booking fix: rerun M1, M2, and all enabled checkpoints;
- allocator or responsive-memory production fix: rerun the full M1-M4 ladder
  before M5.

### 8.2 One bounded evidence-driven behavioral correction

One structural correction cycle is authorized if the first fully valid M3 or M4
candidate fails an ordinary behavioral gate and the evidence identifies a defect
inside bounded spillover or responsive normalized memory.

Allowed examples:

- correcting a simultaneous-flow defect that strands eligible neighbor capacity;
- correcting an opportunity normalization or maturity-estimation defect;
- correcting revision replacement, recency, or confidence computation; or
- replacing the reference market-wide spillover bounds with one principled
  market-wide bounded rule when evidence proves the reference rule structurally
  defective.

The correction must be derived from causal telemetry, documented before rerun,
and applied uniformly across year, genre, tier, label, and format. Rerun M1-M4
afterward. This is not authority for a grid search or repeated constant sweep.

### 8.3 Protected surfaces

Do not change:

- the base `1.34` regional capacity multiplier merely to fit annual gates;
- genre acceptance, lifecycle, momentum, catalog, supply, routing, or keyframes;
- artist formation, scouting, roster, label growth, or release-rate policy;
- year-, genre-, tier-, label-, or format-specific clearing capacity;
- Album quotas, Single penalties, or guaranteed utilization;
- inherited annual or decade acceptance bands;
- retained control artifacts; or
- disabled/prewarm RNG order, behavior, schemas, or stream set.

If completion demonstrably requires one of these changes, do not author another
handoff. Append a final blocked result to the audit with the exact failed gate,
causal evidence, attempted in-scope correction, and prohibited surface required.

## 9. Evidence preservation and final record

Append the complete execution record to
`SimTools/ArtistPopulationLifecycleAudit.md`. Do not create a new follow-up
handoff.

Record:

- starting and final git status, diff check, and source hashes;
- exact commands, exit codes, completion markers, elapsed times, stdout/stderr
  paths, stream counts, sizes, and hashes;
- fixed-probe names and counts;
- disabled 45/45 comparison;
- M3 deterministic comparison and the exact timing exclusion;
- settlement state-machine and idempotency architecture;
- settlement/clearing/weeks/market/finance/annual reconciliation deltas;
- spillover by edge, region, format, tier, and genre;
- memory revision ages, residuals, effective weights, oldest ages, confidence,
  and replacement proof;
- every annual and decade gate;
- count-versus-yield tables;
- label cash/status/roster/release feedback;
- Psych-rock mediation evidence and classification;
- any correction made under section 8; and
- the final `M5 PASS` or final blocked decision.

The completion criterion is not merely a green decade ratio table. It is a
date-complete seed-1001 decade in which every completed unit and economic leg is
settled exactly once, bounded geography reconciles exactly, responsive memory is
revision-safe and temporally bounded, all inherited acceptance gates pass, and the
analyzer itself exits zero.
