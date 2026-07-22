# Single-volume lane and hit-tail systemic repair - Codex handoff

## Status and authority

Investigation date: 2026-07-21.

This is an implementation directive, not another observational request. The next
Codex is authorized to make a systemic repair across record generation, release
identity, revenue memory, Single opportunity reconciliation, demand feedback,
telemetry, analyzers, and validation. Do not constrain the work to a one-constant
patch merely to move the 1966/1967 aggregate ratio below `1.15`.

The retained candidate remains:

```text
d6-album-bounded-mature-channel08-through-1968-1001
```

with control:

```text
d6-transition-envelope-decade-control-1001
```

`SimTools/AlbumCatalogPersistenceHandoff.md` remains the source of truth for the
accepted Album work and the rejected spillover-origin experiment. This directive
supersedes only that handoff's generic instruction to investigate the remaining
Single failure. Do not reopen the Album-channel share or spillover-origin theory.

This handoff was written from existing artifacts and source inspection. No new
simulation was launched while preparing it.

## 1. Correct diagnosis

The apparent late-decade Single rise is not an absolute-volume rise.

| Year | Enabled Single units | Enabled/control |
|---:|---:|---:|
| 1965 | 155.225M | 0.9425 |
| 1966 | 135.314M | 1.1584 |
| 1967 | 124.516M | 1.1933 |
| 1968 | 112.181M | 1.0620 |

Enabled volume declines smoothly. The failing ratio combines two effects:

1. the retained control loses its upper-tail promo-Single hits unusually quickly;
2. the enabled path has a real nonlinear upper-tail amplification despite having
   fewer active and newly released Singles.

Annual decomposition makes the second point explicit:

| Year | Unit ratio | Single-release ratio | Units/release ratio | Active-Single ratio |
|---:|---:|---:|---:|---:|
| 1965 | 0.9417 | 0.9693 | 0.9715 | 1.0946 |
| 1966 | 1.1574 | 0.7824 | 1.4794 | 0.9109 |
| 1967 | 1.1925 | 0.9221 | 1.2933 | 0.7935 |
| 1968 | 1.0614 | 1.0378 | 1.0227 | 0.8642 |

This is a yield-distribution problem, not a release-count or active-pool problem.

### 1.1 Capacity and aging are ruled out

Late Single clearing is effectively unconstrained:

- 1966 cleared/serviceable is `0.9946`; spillover is only `1.25%` of Single units.
- 1967 cleared/serviceable is `1.0000`; spillover is `0.13%`.
- 1968 cleared/serviceable is `1.0000`; spillover is zero.

More than 97% of enabled Single units in 1966-1968 occur by week 14. Do not repair
this through capacity, spillover, inventory, restock, or long-tail retirement.

### 1.2 The aggregate Single bucket hides two different products

Every `AlbumWithPromo` project creates a promo Single in addition to explicit
`OrphanSingle` decisions. The late release population is therefore two lanes:

| Year | Enabled orphan releases | Enabled promo releases | Control orphan releases | Control promo releases |
|---:|---:|---:|---:|---:|
| 1966 | 697 | 2,087 | 887 | 2,074 |
| 1967 | 627 | 1,329 | 675 | 1,920 |
| 1968 | 674 | 1,017 | 665 | 1,448 |

First-14-week yield shows the mechanism moving between lanes:

| Year/lane | Enabled units/release | Control units/release | Ratio |
|---|---:|---:|---:|
| 1966 orphan | 92,252 | 54,821 | 1.683 |
| 1966 promo | 30,669 | 29,676 | 1.033 |
| 1967 orphan | 110,749 | 94,118 | 1.177 |
| 1967 promo | 36,612 | 18,351 | 1.995 |
| 1968 orphan | 92,559 | 115,067 | 0.804 |
| 1968 promo | 34,101 | 17,800 | 1.916 |

The 1966 excess is primarily orphan-Single yield. The 1967-1968 enabled promo
lane remains much stronger than control even while enabled creates far fewer promo
records.

### 1.3 The excess is a hit tail, not a typical-record uplift

For 1967 promo Singles, enabled median first-14-week yield is `8,648`, slightly
below control's `9,003`. The enabled p90 is `61,299` versus `28,146`, and p99 is
`396,461` versus `160,405`. Enabled top-1% records supply `36.07%` of promo units,
versus `28.29%` in control.

For 1968 promo Singles, enabled median is again lower (`7,929` versus `8,363`),
while p99 is `659,587` versus `130,620`.

The control promo p99 itself falls from approximately `1.12M` in 1965 to `473K`,
`160K`, and `131K` in 1966-1968. That is a control-tail instability, not a broad
collapse in typical promo performance. Validation must expose it instead of
letting an aggregate candidate/control ratio silently treat the denominator as
stable.

### 1.4 Source mechanisms that jointly create the tail

The current source contains four interacting structural problems:

1. `CompetitorManager.CreatePromoSingleFromAlbum` selects the maximum-quality
   non-single album track and copies that one scalar into `hookStrength`,
   `productionQuality`, and `danceability`. This is an order-statistic boost and
   destroys the component covariance of ordinary Singles.
2. `ChartSimulator` raises realized quality to the fourth power, then multiplies
   chart visibility, momentum, radio, acceptance, format opportunity, sentiment,
   distribution, and seasonality. Correlated discovery signals therefore compound
   instead of saturating.
3. Promo outcomes are folded into Album project memory. The market sees a Single,
   but Single decision calibration does not see a stable promo-Single lane.
4. `GenreAcceptanceService.CalculateSuppliedSingleOpportunity` normalizes a fixed
   prospective genre portfolio. It cannot see the actual orphan/promo mix, the
   selected quality distribution, or successor-genre concentration. Its `0.90`
   floor is fully bound from 1966 onward.

The modest input differences are large enough to cross the nonlinear tail. Mean
decision-quality ratios imply about `1.40x` demand from `quality^4` alone for 1966
orphan Singles, about `1.23x` for 1967 promo Singles, and about `1.35x` for 1968
promo Singles before discovery feedback.

## 2. Repair mandate

Implement all four workstreams below as one coherent candidate. The purpose is to
make Single origin explicit, remove synthetic promo quality, prevent correlated
signals from multiplying without bound, and normalize the portfolio actually
released. Do not stop after the first local change happens to pass an annual gate.

### 2.1 Make release lane a first-class invariant

Use the existing `ProjectRecordRole` vocabulary or replace it with one canonical
equivalent; do not create competing booleans. At minimum distinguish:

```text
OrphanSingle
PromoSingle
StandaloneAlbum
LinkedAlbum
ExternalOrLegacy
```

Requirements:

- assign the role when the record/project is created and never infer it later from
  title, format, release timing, or the survival of an `AlbumProject` object;
- preserve the role across scheduling, transfer, cancellation, release, retirement,
  archive snapshots, and audit joins;
- expose it in release, demand-funnel, lifecycle, chart, revenue-memory, and annual
  telemetry;
- reconcile `all Singles == orphan Singles + promo Singles + external/legacy
  Singles` exactly in every weekly and annual output;
- keep disabled save/data compatibility through an explicit legacy/default role.

No aggregate Single metric may be emitted without an adjacent lane decomposition.

### 2.2 Replace synthetic promo-track construction

Extend `AlbumTrack` so a newly generated original track stores the component traits
needed to construct a real `Record`, at minimum:

```text
hookStrength
productionQuality
danceability
quality/composite
```

Generate these component traits once for every original album track when the album
is assembled. Preserve their covariance and retain the existing scalar `quality`
as the album-appeal composite. For older snapshots that contain only `quality`, use
one documented compatibility reconstruction; never apply that fallback to newly
generated tracks.

Promo selection must then:

- score every eligible track with a deterministic lead-Single suitability function
  built from stored traits and current genre/era suitability;
- use a stable ordinal tie-break;
- consume no RNG during selection;
- copy the selected track's stored component traits into the promo `Record`;
- never assign the maximum generic track-quality scalar to all Record traits;
- leave the selected track's contribution to Album pooled appeal exactly
  reconcilable before and after moving it into `trackRefs`.

Generate all candidate-track traits before selection so the winner does not receive
extra draws. Preserve the disabled branch's RNG contract. If the live enabled RNG
contract necessarily changes because tracks now have real components, version that
contract explicitly and require exact repeatability from the new candidate onward.

Do not replace max-quality selection with a percentile cap or post-selection
quality haircut. Model a promo track correctly.

### 2.3 Separate economic memory by decision lane

Refactor format/project memory so these estimators are distinct:

```text
OrphanSingle outcome
PromoSingle outcome
StandaloneAlbum outcome
AlbumWithPromo total-project outcome
```

The promo Single's realized economics must update `PromoSingle` memory even when
the associated Album later drops successfully. The combined Album-plus-promo net
may also update `AlbumWithPromo` project memory, because that estimator answers a
different decision question. This is estimator bookkeeping, not a second finance
posting.

Rules:

- orphan-Single projection reads orphan-Single memory;
- expected promo net reads promo-Single memory;
- Album-with-promo strategy comparison reads total-project memory plus its explicit
  component priors;
- a cancelled or transferred Album must not relabel its already-released promo as
  an orphan Single;
- every memory observation carries lane, release ID, project ID if any, observation
  age, expected net, realized net, normalized residual, weight, and fold state;
- telemetry must prove that every economic outcome is posted to finance once and
  observed by each applicable estimator once.

Do not solve this by feeding promo outcomes into the old undifferentiated Single
EMA. That would contaminate orphan decisions in the opposite direction.

### 2.4 Replace the fixed-portfolio Single normalizer

Retire `CalculateSuppliedSingleOpportunity` as the live owner once a sufficient
actual release cohort exists. Replace it with a deterministic, ex-ante,
lane-aware opportunity ledger built from the records the simulator actually
releases.

For each Single at release, capture its enabled and accepted-legacy opportunity
mass using only information already fixed at release:

- release lane;
- primary/secondary genre and regional routing;
- stored quality components and the intrinsic quality curve;
- initial reach/awareness inputs;
- label tier and distribution inputs;
- format opportunity and era state.

Explicitly exclude realized units, chart position, momentum, later radio heat,
sentiment, awards, inventory clearing, revenue, and annual gate results. The ledger
must not become a closed-loop sales target.

Maintain rolling cohort sums by lane and region. Compute the same enabled-to-
accepted relationship currently anchored in 1960, but weight it with actual cohort
composition rather than fixed `GenreSupplyService` priors. Preserve within-lane
genre and regional differences; reconcile only portfolio drift. Freeze the
normalizer used by a release so later collection order or retirement cannot revise
its past demand.

The current `[0.90, 1.10]` clamp is not an accepted design boundary. Remove it as a
calibration limiter. A broad numerical safety bound may remain, but binding it is a
hard diagnostic failure requiring investigation, not a normal operating state.
Use the old prospective calculation only as a cold-start fallback, report its share,
and stop using it once the minimum deterministic cohort mass is met.

### 2.5 Give correlated discovery signals one owner

Refactor live Single demand into named stages:

```text
potential audience
baseline awareness
earned discovery exposure
aware buyers (bounded by potential audience)
intrinsic conversion
raw demand
serviceable demand
cleared units
```

Chart visibility, breakout visibility, momentum, and radio are correlated discovery
signals. They must be combined once through a bounded/diminishing-return discovery
function and applied to awareness/exposure. They must not also multiply intrinsic
purchase conversion independently.

The exact combiner may reuse existing neutral points and ordering, but it must prove:

- aware buyers never exceed potential audience;
- each input is monotonic when varied alone;
- neutral inputs preserve neutral output;
- adding a second or third strong discovery signal gives diminishing incremental
  lift;
- no discovery input appears in both awareness and conversion;
- quality and genre acceptance still affect conversion;
- chart success can extend reach but cannot manufacture a second copy of the same
  audience.

Extract the stage calculation into a pure function with probe access. Do not merely
lower `QUALITY_EXPONENT`, `TOP_5_VISIBILITY_MULT`, or `HIT_MOMENTUM_BONUS` while
leaving the multiplicative topology intact. Those constants may only change if the
new staged model makes their old meaning obsolete, and the hand-back must explain
the replacement semantics.

## 3. Telemetry and offline analyzer

Extend enabled-only telemetry without changing frozen disabled CSV schemas.

### 3.1 Per-release identity and ex-ante opportunity

Emit one row per release:

```text
week,year,recordId,projectId,releaseLane,labelId,tier,artistId,genre,
careerState,hookStrength,productionQuality,danceability,quality,
enabledOpportunityMass,acceptedOpportunityMass,cohortNormalizer,
normalizerSource,coldStartFallback
```

### 3.2 Weekly Single demand stages

The existing bounded diagnostic may be extended or replaced, but must expose:

```text
recordId,releaseLane,region,age,
potentialAudience,baselineAwareness,earnedDiscoveryExposure,awareBuyers,
intrinsicQualityFactor,acceptanceFactor,formatFactor,
intrinsicConversionRate,rawDemand,serviceableDemand,clearedUnits,
chartSignal,momentumSignal,radioSignal,
inventoryFulfillmentRate,marketFulfillmentRate
```

Sample deterministically and guarantee coverage through week 14. The analyzer must
be able to reconstruct raw demand from the reported stages within float tolerance.

### 3.3 Memory ledger

Emit estimator lane separately from physical release lane and include exact
finance/memory reconciliation fields. Fail on duplicate observation keys, missing
promo observations, double finance posting, or a promo observation routed to orphan
memory.

### 3.4 Analyzer

Add `SimTools/analyze-single-lane-hit-tail.mjs`. It must ingest candidate and control
families and report, by calendar year, release cohort year, lane, genre, quality
band, career band, and label tier:

- release count, active count, successful count, and units;
- units/release and first-3/4-14/15-26/27-52-week yield;
- mean, median, p75, p90, p95, p99, maximum, top-10%, top-1%, and Gini share;
- enabled/control raw ratios with denominator-health warnings;
- direct-standardized yield ratios over common lane/genre/quality/career/tier
  strata;
- ex-ante opportunity mass and normalizer attribution;
- discovery-stage attribution and raw-demand reconstruction residual;
- memory observation/fold and finance-posting reconciliation;
- capacity/spillover shares to preserve the already-established negative evidence;
- a ranked list of records and cohorts responsible for every annual delta.

The analyzer must flag `CONTROL_TAIL_INSTABILITY` when a control lane's p99 changes
by more than 50% year over year while its median changes by less than 25%. A raw
candidate/control aggregate gate remains visible, but it may not be reported as a
complete causal verdict when this flag is active.

Fail closed on missing columns, partial joins, unclassified Singles, or any
reconciliation residual.

## 4. Fixed probes

Extend the existing probe suites. At minimum prove:

1. every generated orphan Single has `OrphanSingle` role;
2. every album promo has `PromoSingle` role and a stable project link;
3. transfer, cancellation, retirement, and archive do not change release role;
4. weekly and annual lane sums exactly reconcile to all Singles;
5. new album tracks store non-degenerate component traits;
6. promo construction copies those exact traits and does not set all components to
   scalar quality;
7. promo selection is deterministic and stable under collection insertion order;
8. promo selection consumes no RNG;
9. moving a selected track from `nonSingleTracks` to `trackRefs` preserves Album
   pooled-appeal inputs;
10. orphan, promo, standalone-Album, and Album-with-promo memories remain separate;
11. a successful promo plus Album produces one promo estimator observation, one
    project estimator observation, and no duplicate finance posting;
12. cancelled/transferred projects preserve correct promo memory identity;
13. cohort opportunity calculation is invariant to collection order;
14. no realized outcome field can influence ex-ante opportunity mass;
15. cold-start fallback retires deterministically at the configured cohort mass;
16. the normalizer preserves within-lane genre ordering;
17. discovery stages are bounded, monotonic, neutral, and diminishing-return;
18. raw demand reconstructs from telemetry within tolerance;
19. chart, momentum, and radio are each consumed by exactly one discovery owner;
20. disabled behavior and schemas remain byte-compatible.

## 5. Validation ladder

Stop at the first hard failure. Preserve source hashes, commands, completion
markers, manifests, and analyzer output. Do not tune after seeing a partial result.

### M0 - implementation integrity

- record `git status --short`, `git diff --check`, and starting/final hashes;
- build with `dotnet build "Label Man.sln" --no-restore`;
- inspect every caller of promo creation, opportunity normalization, responsive
  memory, and live Single demand;
- prove the accepted Album channel, Album replenishment closure, regional clearing,
  prices, finance settlement, label population, and genre catalog were not tuned.

### M1 - fixed probes and disabled replay

Run the exact-source one-week probe harness, then the inherited disabled 52-week
replay. Require all old and new probes, normal completion, the frozen disabled
suffix set and hashes, and no new enabled-only streams in disabled output.

### M2 - bounded enabled checkpoint

Run one seed-1001 aggregate-plus-new-diagnostics checkpoint long enough to cover at
least one complete 14-week cohort. Require:

- exact lane, demand-stage, inventory, allocation, memory, and finance
  reconciliation;
- nonzero orphan and promo coverage;
- no normalizer safety-bound hit;
- no duplicate discovery factor;
- analyzer completion without missing joins.

Run an exact deterministic repeat only after the first checkpoint passes.

### M3 - transition checkpoint

Run through the 1965 hard gate. Require all inherited release, Album, total-unit,
economic, lifecycle, catastrophic, and reconciliation gates. Additionally report
lane-specific yield distributions and control-tail health for every completed year.

Do not alter code or constants after this checkpoint merely because the decade gate
is known to be close.

### M4 - one through-1968 candidate

Run the same 469-week seed-1001 boundary used by the retained candidate. Analyze it
against both the retained control and retained enabled source family. Do not launch
M5, more seeds, or a parameter sweep under this handoff.

## 6. Acceptance rules

All inherited exact invariants and economic/Album gates remain in force. The repair
must also satisfy:

```text
annual Single units/control: [0.85, 1.15]
annual total units/control:  [0.85, 1.15]
```

Those compatibility bands are necessary but no longer sufficient. Require all of
the following:

- enabled absolute Single volume does not reverse into a late 1965-1968 rise;
- every Single is classified and all lane sums reconcile exactly;
- direct-standardized mean yield by lane is within `[0.80, 1.25]` of control for
  strata with adequate common support;
- for lane-years with at least 200 records, top-1% unit share is at most `35%` and
  top-10% share is at most `40%`;
- candidate `p99 / median` is no more than `2x` the corresponding healthy-control
  ratio; when control is flagged unstable, compare against the pooled adjacent-year
  healthy-control ratio and report the substitution explicitly;
- no single cohort of fewer than 10 records explains more than 5% of all annual
  Single units without being listed and adjudicated as a genuine hit cluster;
- typical-record medians are not pushed down simply to compensate for the old hit
  tail;
- the actual-cohort normalizer is active for the mature 1966-1968 surface and its
  numerical safety bound never binds;
- zero missing/duplicate memory observations and zero finance double posts;
- exact deterministic repeat equality at every authorized repeat boundary;
- header-only catastrophic output.

If the aggregate ratio passes only because medians collapse, reject. If medians are
healthy but the same synthetic p99 concentration remains, reject. If the aggregate
ratio remains slightly outside band solely because a control lane is flagged
unstable, do not widen the band or declare success: report the standardized and
absolute evidence to the owner for an explicit baseline decision.

## 7. Prohibited shortcuts

Do not:

- lower the Album channel or alter Album catalog age allocation to suppress Singles;
- revisit spillover export/import shares;
- cap annual units, units per record, chart weeks, or hit counts after calculation;
- add a promo quota, orphan quota, genre quota, or label-tier reservation;
- modify the retained control;
- widen the `1.15` ceiling;
- tune `QUALITY_EXPONENT` or visibility constants in isolation;
- add a special late-decade multiplier or year-keyed correction;
- normalize from realized sales, revenue, chart ranks, or gate failures;
- let the new analyzer silently drop unmatched promo records;
- call a one-seed aggregate pass sufficient without distributional reconciliation.

## 8. Required hand-back

Update this file and `SimTools/AlbumCatalogPersistenceHandoff.md` with:

- exact files changed and hashes;
- structural choices for release role, track traits, memory lanes, opportunity
  ledger, and discovery combiner;
- build/probe/disabled compatibility results;
- every run command and completion marker;
- annual absolute and control-relative Single/Album/total/economic tables;
- orphan/promo counts and full yield distributions;
- standardized yield results and common-support coverage;
- control-tail health flags;
- top contributing records/genres and whether the old rare-hit clusters survived;
- normalizer source/share and safety-bound status;
- memory/finance reconciliation;
- deterministic comparison;
- the exact accept, reject, or owner-adjudication decision.

The implementation is complete only when the model no longer depends on hidden
promo identity or synthetic promo traits, correlated discovery signals have one
bounded owner, actual released supply drives ex-ante reconciliation, and both
aggregate and distributional gates pass without suppressing the typical Single.

## Implementation update — 2026-07-21

Implemented the structural candidate at source revision
`b2004673f26b06f7459451239b88a13fd02370c8`:

- `ProjectRecordRole` is durable on `Record`, copied into runtime data, with an
  explicit `ExternalOrLegacy` compatibility default. New Singles are assigned
  `OrphanSingle` or `PromoSingle` before release; project linkage is durable.
- Original `AlbumTrack` material stores deterministic, covariant component
  traits. Promo selection uses a deterministic suitability score and title
  ordinal tie-break; it copies the winner's traits rather than scalar quality.
- Live revenue observations are separated into orphan, promo, standalone-Album,
  and Album-with-promo estimators. Promo cancellation preserves promo identity.
- `SingleOpportunityLedger` freezes a lane-specific release normalizer using
  completed actual cohorts; prospective reconciliation is retained only for
  cold start. Bound hits fail closed.
- Enabled Single demand now has a pure bounded discovery stage. Chart,
  momentum, and radio are combined once into awareness and are no longer also
  conversion multipliers. Enabled-only lane and demand-stage CSVs are emitted.

Added `SimTools/analyze-single-lane-hit-tail.mjs` and fixed discovery-stage probes
in `GenreMarketV2ProbeSuite`. `dotnet build "Label Man.sln" --no-restore` passed
after the change (one pre-existing unused-event warning). `git diff --check`
passed. No simulation replay was run, and Node.js is unavailable in this
workspace, so the analyzer has not been executed. The inherited disabled replay,
M2 checkpoint, and decade gate remain required before acceptance. The existing
user modification to `AlbumCatalogPersistenceHandoff.md` was intentionally not
edited.
