# Artist population runtime-label organic-growth handoff

## Mission and authority

Repair the missing post-bootstrap lifecycle for labels founded during live simulation. A runtime-founded label must still enter the market empty with a one-artist operating target, but that target must no longer remain one forever. Allow a viable label to add operating roster capacity gradually through the existing label lifecycle, using the established tier capacity as its available roster range and retaining demonstrated output, financial health, affordability, and one-at-a-time labor-market rules.

This is the authoritative next-pass handoff for Codex after the failed transition-envelope C4 seed-1001 decade. It supplements the retained C3 compatibility write-off and supersedes prior instructions only where they described the one-artist bootstrap as a permanent operating ceiling.

This handoff authorizes:

1. explicit runtime-label origin and target-growth state;
2. one deterministic organic-growth decision at the quarterly label-lifecycle boundary;
3. acquisition and tier-change reconciliation needed to keep hard and operating capacities coherent;
4. compact target-change telemetry and fixed production-helper probes;
5. a source-change no-op replay on the disabled path;
6. paired seed-1001 validation through 52, 104, 260, and 522 Fridays; and
7. one implementation correction only if probes or telemetry demonstrate that the specified state machine was implemented incorrectly.

It does not authorize a Single-yield correction, demand or sales tuning, an Album rule change, immediate target-three initialization, release-cadence expansion, performance-exhaustion changes, another acceptance-band amendment, seeds 1002/1003, or a holdout.

## Retained historical results

Do not rewrite or relabel the existing records:

- C3 remains a 14/45 historical disabled-compatibility failure covered by the explicit owner write-off. The old compatibility baseline is not replaced.
- C4 remains a historical failure. The first failed gate is 1966 Single units at `1.2135x` control. Later 1969 also fails releases at `0.8450x`, scheduled Albums at `0.7409x`, and Single units at `1.2846x`.
- C5 seeds 1002/1003 and C6 holdout remain unused.
- The separate Single-yield excess is deferred, not accepted or waived. No eventual Directive 6 completion may omit it.

## Confirmed defect

`LabelLifecycleManager.SpawnNewLabel` creates every live runtime label as Small or Independent and calls `RosterManager.InitializeRuntimeRosterForLabel`. Initialization correctly creates an empty roster, assigns operating target one, consumes no candidate/signing RNG, and performs no birth-week signing.

The defect is that initialization is also the final target decision:

- `AILabel.SetOperatingRosterTargetFromCurrent()` is the only production writer and maps an empty roster to one.
- Every enabled scouting, Watch, Recovery, activation, and market-clearing availability check uses `OperatingRosterTarget` as the hiring ceiling.
- `PromoteLabel` and `DemoteLabel` change `tier` and `maxRosterSize` but never change the operating target.
- No monthly, quarterly, annual, financial, capability, chart-success, investment, or promotion mechanic can raise the target.
- Acquisitions can transfer artists above a target without creating a coherent new operating plan; that is not organic growth.

The result is a disconnected capacity model. Tier progression may raise hard capacity from Small to Independent, MidTier, or Major while the labor market still treats the label as a permanent one-artist operation.

### C4 evidence

Existing C4 telemetry confirms material decade-scale impact:

| Year | Runtime-bootstrap share of active label-weeks | Share of roster | Share of release-eligible artists | Empty active runtime labels |
|---:|---:|---:|---:|---:|
| 1966 | 30.8% | 1.25% | 1.08% | 77.8% |
| 1969 | 30.3% | 1.14% | 0.87% | 81.2% |

Runtime-bootstrap labels produced only 66 of 4,643 physical projects in 1966 and 56 of 3,771 in 1969. This is a structural label-maturation and release-capacity defect.

It is not sufficient attribution for the Single-unit gate. Enabled Single releases are fewer than control in both failed years while units per released Single are approximately `1.414x` control in 1966 and `1.348x` in 1969. Preserve that as a separate later investigation.

## Required state model

### 1. Preserve the one-artist birth bootstrap

Every label created by `SpawnNewLabel` must:

- be marked with immutable origin `RuntimeFounded`;
- record the exact simulation birth week and date;
- initialize an empty roster;
- set `OperatingRosterTarget = min(1, maxRosterSize)`;
- record target reason `RuntimeBootstrap`;
- perform no candidate enumeration, evaluation, signing, advance, or career mutation during birth; and
- enter the ordinary weekly Recovery process on the next authoritative scouting boundary.

Do not use `monthsActive` or the generator's backstory `foundedYear` as runtime chronology. Record a separate exact runtime birth week for attribution and age reporting.

Launch labels retain immutable origin `LaunchPopulation`. Their initialized operating targets remain their populated launch-roster headcounts, with the existing target-one fallback for a genuinely empty launch label. Passive annual organic growth of launch labels is outside this repair.

Use structured state rather than inferring origin from `labelId` prefixes or mutable target-reason strings.

### 2. Let the established tier system govern organic capacity

Evaluate runtime-label organic growth once from each existing quarterly lifecycle pass, after health, tier changes, and current capability are available. A target increase is allowed only when every condition below is true:

```text
ArtistPopulationLifecycle.Enabled
label origin == RuntimeFounded
label is active
CurrentRosterSize >= OperatingRosterTarget
OperatingRosterTarget < maxRosterSize
status is Stable or Rising
consecutiveLossMonths == 0
lastMonthlyProfit > 0
cashReserves >= 6 * current monthly overhead
at least 1 charting record in the preceding 52 weeks
```

The existing lifecycle tier capacity is the operating range unlocked by label progression:

| Current tier | Established lifecycle hard capacity |
|---|---:|
| Small | 5 |
| Boutique | 8 |
| Independent | 12 |
| MidTier | 25 |
| Major | 50 |

Create or expose one pure canonical lifecycle helper for these values and use it for runtime-label birth, promotion, demotion, and organic-growth capacity checks. Do not retain divergent copies in `LabelGenerator`, `AILabel`, and `LabelLifecycleManager` for the runtime path. A runtime-founded Small label therefore starts with hard capacity 5 and target 1; a runtime-founded Independent starts with hard capacity 12 and target 1. Promotion changes the same hard-cap field to 12, 25, or 50 before later target growth can use that range.

Do not recalculate or reroll the frozen launch population's initial hard capacities. Existing launch labels retain their initialized values until an actual lifecycle tier transition, at which point the canonical promoted/demoted tier capacity applies.

When all conditions pass:

```text
OperatingRosterTarget = min(OperatingRosterTarget + 1, maxRosterSize)
```

The decision is deterministic and consumes no RNG. It raises planned capacity only; it must not sign an artist immediately. The existing weekly talent-service path subsequently fills the one-slot deficit through Watch/Recovery, ordinary discovery, scoring, actual affordability, and at most one evaluation and one actual signing attempt per label-week.

Each eligible quarterly decision adds exactly one slot, so no label can authorize more than four organic slots in one complete year. Do not add catch-up growth, tier-sized jumps, automatic target-three initialization, or accumulated credits. A label that remains Small is capped by the Small range. A label promoted to Independent, MidTier, or Major gains access to that tier's larger range and can continue one-slot quarterly growth until it reaches the new hard ceiling. A continuously viable 1960 startup is therefore not limited to a decade target near ten; promotion and the unlocked tier capacity can support materially larger growth while every individual vacancy is still filled through ordinary weekly hiring.

### 3. Make tier progression capacity-coherent

Promotion continues to update `tier` and `maxRosterSize`. That larger `maxRosterSize` is the newly available roster range. Promotion does not set the operating target to the new maximum and does not bulk-sign artists, but the same quarterly lifecycle pass may authorize one ordinary target slot when all growth evidence passes. Later qualifying quarters may continue one slot at a time through the promoted range.

Demotion must preserve invariants without forcibly dropping artists merely to fit a smaller nominal tier size. Reconcile `maxRosterSize` and current roster safely, ensure `OperatingRosterTarget <= maxRosterSize`, and prevent new hiring while roster size is at or above the reconciled operating target. Record the exact reconciliation decision.

The existing MidTier-to-Major roster predicate must read real roster headcount. Do not weaken promotion requirements to make a one-artist label promote.

### 4. Reconcile acquisitions as realized growth

When a distributor absorbs another label and artists are transferred, reconcile the distributor's operating target to at least its post-transfer roster size, clamped to the reconciled hard capacity. Record target reason `Acquisition`. The transfer already happened and must not manufacture a target deficit or immediate extra signing authority.

This is distinct from quarterly organic growth and does not create a reusable growth credit.

### 5. Preserve weekly labor-market boundaries

Retain unchanged:

- `OperatingRosterTarget` as the enabled hiring ceiling;
- `maxRosterSize` as the hard ceiling;
- headcount-only service deficit;
- release-lane deficit as telemetry only;
- Normal, Watch, and Recovery timing;
- one evaluation and one actual signing attempt per active label-week;
- fresh-potential and experienced-production lanes;
- deterministic discovery ordering and bounded national Recovery widening;
- score thresholds, Recovery fallback, and actual affordability;
- contract classification, probation, cooldown, and performance exhaustion; and
- closed-label, ownership, pool, terminal, chronology, project, and release-selection safeguards.

Organic growth creates one ordinary vacancy. It does not guarantee a contract, inject an artist, grant cash, or override affordability.

## Historical and healthy bounds

The implementation must prove all of the following:

- target one at birth remains exact;
- no growth event adds more than one slot;
- no runtime label receives more than one organic target decision in one quarterly lifecycle pass or more than four in one complete year;
- no target exceeds hard capacity;
- no financially distressed, loss-making, unfilled, inactive, defunct, bankrupt, or acquired label receives organic growth;
- every organic increase has preceding chart, profit, runway, and filled-target evidence;
- promotion never causes a bulk roster or target jump;
- growth never signs in the same callback;
- the next weekly service boundary still performs at most one evaluation and attempt; and
- disabled behavior constructs no runtime-growth state and remains byte-identical to the current post-write-off disabled artifact.

The prior target-three experiment is negative evidence: a broad immediate `+2` demand shock caused 1960 releases at `1.1776x` and scheduled Albums at `1.2697x`. Do not recreate it indirectly.

## Telemetry and attribution

Retain current label-week telemetry and add compact structured target state. At minimum expose:

- immutable label origin;
- exact runtime birth week/date;
- current operating target and hard capacity;
- target reason;
- prior and new target;
- target-change week/date;
- organic-growth count;
- weeks since prior organic increase;
- eligibility result and a single structured blocking reason;
- status, tier, roster size, release-eligible count, recent charting count;
- last monthly profit, consecutive loss months, cash, overhead, and runway; and
- whether the next weekly deficit produced an evaluation, attempt, and signing.

Prefer a compact enabled-only `label-operating-target-events.csv` with one row for initialization, organic increase, promotion/demotion reconciliation, and acquisition reconciliation. Do not emit one row per candidate or reserve artist. Existing `label-scouting-vacancy-weekly.csv` should continue to carry current target/origin fields needed for annual stock reconciliation.

Use the retained C4 artifacts to report, by year and label origin:

- active label-weeks and unique active labels;
- target distribution and age since birth;
- empty, occupied, at-target, and below-target shares;
- roster and release-eligible contribution;
- target increases and their evidence;
- first/repeat signings;
- successful releases and physical Single/Album projects; and
- label closures, acquisitions, promotions, and demotions.

The analyzer may be added or extended for this attribution, but it must not alter simulation behavior or acceptance rules.

## Fixed probes

Retain all accepted D5 and D6 probes. Add production-helper coverage for at least:

1. runtime birth records `RuntimeFounded`, exact birth time, target one, and no birth-week signing;
2. launch labels retain `LaunchPopulation` and their initialized targets;
3. a runtime label cannot grow when its current target is unfilled;
4. Struggling, Dying, loss-making, under-runway, no-chart, inactive, and hard-full labels cannot grow;
5. a filled, profitable, six-month-runway, recently charting runtime label can gain exactly one slot at a quarterly review;
6. a label cannot receive a second growth decision in the same quarterly pass;
7. the increase consumes no RNG and signs nobody in the quarterly callback;
8. another increase is possible at the next qualifying quarterly review with fresh evidence;
9. the shared tier-capacity helper returns exactly `5 / 8 / 12 / 25 / 50`, runtime birth and tier transitions use it without RNG, and organic growth stops at that live hard capacity;
10. promotion unlocks the promoted tier range but never bulk-raises the operating target;
11. demotion preserves roster/target/hard-cap invariants without forced arbitrary artist deletion;
12. acquisition reconciliation recognizes the transferred roster without creating extra vacancy;
13. the weekly service path fills at most one newly authorized slot per week through ordinary rules;
14. no market-clearing attempt occurs at or above operating target;
15. closed labels never grow or scout;
16. target events and weekly snapshots reconcile exactly; and
17. the disabled route retains current post-write-off RNG order, stream set, headers, and values.

Do not satisfy probes with parallel probe-only policy logic.

## Validation ladder

Use seed 1001 only. Preserve every prior family and never overwrite C1-C4 artifacts.

Suggested run family:

```text
d6-runtime-label-growth-probes-1001
d6-runtime-label-growth-disabled-52-1001
d6-runtime-label-growth-control-52-1001
d6-runtime-label-growth-enabled-52-1001
d6-runtime-label-growth-control-104-1001
d6-runtime-label-growth-enabled-104-1001
d6-runtime-label-growth-enabled-repeat-104-1001
d6-runtime-label-growth-maturity-control-260-1001
d6-runtime-label-growth-maturity-enabled-260-1001
d6-runtime-label-growth-decade-control-1001
d6-runtime-label-growth-decade-enabled-1001
```

### G0 - retained-artifact preflight

Before source edits, reproduce from existing C4 files:

- the exact 1966 and 1969 gate values recorded above;
- 1966/1969 runtime-label active, roster, eligibility, empty, and project shares;
- target source and target-one persistence by label age and tier;
- enabled/control Single release counts, units, and units per release; and
- the current frozen functional-source manifest.

Record all files that can write target, hard capacity, label tier, label origin, roster membership, or acquisition transfer state.

### G1 - implementation, build, and probes

Implement only this handoff. Run `git diff --check`, build, and the full accepted D5/D6 probe command. Record a before/after functional-source manifest and exact target-growth constants.

### G2 - disabled no-op proof

Run a fresh 52-week dual-disabled aggregate process. Require all 45 suffix-matched streams to be byte-identical to `d6-transition-envelope-disabled-52-1001`, with no missing, changed, or extra stream and no target-growth telemetry.

This is source-change no-op proof against the current frozen candidate. It is not a replacement for, or relabelling of, the older failed compatibility baseline.

### G3 - 52-week boundary

Run a fresh paired 52-week control/treatment. Quarterly growth may operate during 1960, but only for individually qualified runtime labels and only one slot per quarterly pass. Require all inherited 1960 release, Album, economic, population, finance, and structural gates and prove that the repair did not recreate the target-three launch shock.

### G4 - 104-week deterministic boundary

Run fresh paired 104-week control/treatment plus an independent enabled repeat. Require the enabled families to be byte-identical by suffix, length, and SHA-256. Apply all inherited annual gates and reconcile every target increase to its evidence and subsequent weekly labor-market activity.

### G5 - 260-week maturity

Run a fresh paired 260-week control/treatment without source changes. Require every complete year:

- successful releases in `[0.85,1.15]`;
- scheduled Albums, Album units, and Album gross in `[0.80,1.20]`;
- no annual total-unit or market-net ratio outside `[0.75,1.25]`;
- exact project, finance, labor-market, target-event, and population reconciliation; and
- all structural invariants at zero.

Report Single units and the existing transition-envelope economics, but do not tune or stop the growth diagnosis solely to repair the already-deferred per-release Single-yield surface. A new catastrophic economic failure still stops the pass.

### G6 - date-complete seed-1001 decade

Only after G5 passes, run the paired 522-Friday seed-1001 decade. The runtime-label growth repair passes its own scope only if:

- every annual successful-release ratio is in `[0.85,1.15]`;
- every annual scheduled-Album ratio is in `[0.80,1.20]`;
- target growth obeys every historical/healthy bound above;
- runtime labels make a nonzero, reconciled roster, release-eligible, and project contribution after maturation;
- no broad launch or promotion shock appears;
- decade finance, population, chronology, and structural invariants pass; and
- no new catastrophic unit or market-net failure is introduced.

Report the Single-unit ratios exactly. If label growth passes while the retained Single-yield gate still fails, stop after G6 and create a separate yield-attribution handoff from the unchanged growth source. Do not call C4 or Directive 6 complete, and do not run seeds 1002/1003 or a holdout.

If releases or scheduled Albums still fail, preserve the artifacts and stop. Do not sweep the quarterly cadence, runway requirement, chart requirement, or per-event increment after seeing the result.

## Closed surfaces

Do not change:

- birth target one, birth-week no-signing behavior, or launch-population allocation;
- reserve size, formation rate, participation, search horizon, or activation order;
- discovery slate, fresh/experienced scoring, threshold, Recovery fallback, or affordability;
- contract probation, comeback, cooldown, exhaustion, inactivity, retirement, or disbandment;
- release cadence, release helper, release priority, cooldown, selection, or eligibility;
- Album choice, affinity, priors, format tilt, project timing, promo strategy, hit inventory, reuse, freshness, substitution, or cannibalization;
- Single or Album demand, buyer pools, quality exponents, awareness, prices, sales, finance, royalties, or distribution rules;
- genre, geography, distance, seasonality, specialist, or historical inputs;
- existing acceptance bands or the C3 write-off terms; or
- C5/C6 seed and holdout state.

## Immediate Codex instruction

Append this handoff decision to `ArtistPopulationLifecycleAudit.md`. Reproduce G0 from retained artifacts, implement the specified deterministic runtime-label growth state, and proceed sequentially through G1-G6. Stop on the first implementation, growth-capacity, catastrophic economic, or structural failure. Do not touch the deferred Single-yield surface during this pass.
