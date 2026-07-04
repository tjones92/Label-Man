# Directive 3B: Prior Repair and Album Project Pipeline

## Objective

Implement this directive in two gated checkpoints.

- **Checkpoint A - prior repair:** correct the analytic release prior so the 1960 singles chart regains meaningful adult-genre representation, youth compilations can be economically rational, and projected contribution net is materially better calibrated.
- **Checkpoint B - album projects:** replace isolated AI album drops with explicit album projects. A project may release immediately as a standalone album or launch a promo single and schedule the album three to five weeks later. Add deterministic scheduling, promo-to-album launch effects, linked-release cannibalization, project-level memory attribution, and audit telemetry.

Do not begin Checkpoint B until Checkpoint A's hard gates pass. If a hard gate cannot pass within Checkpoint A's authorized changes, stop and report the evidence instead of changing demand, chart, retirement, or generation constants.

## Verified starting point

Treat these as repository facts, not assumptions to rediscover by changing behavior.

- `CompetitorManager.OnWeekEnded` currently calls `ProcessWeeklyRevenue()` and then `ProcessWeeklyReleases(date)`.
- `ProcessWeeklyReleases` owns the existing roll/success/failure counters and resets them at its start.
- `TryReleaseRecord` chooses an artist, calls `DecideRelease`, generates the chosen record, charges production plus clamped marketing, releases the record, applies promotion, and resets the artist cooldown.
- `DecideRelease` is already an economic Single-versus-Album comparison. It uses deterministic priors, label-local EMA memory, and exactly two projection-noise draws in Single/Album order when albums are enabled.
- The final 3A scalar settings are `priorUnitScalarSingle = 12000`, `priorUnitScalarAlbum = 100000`, and `priorAssumedAlbumPackaging = 0.50`.
- 3A's realized outcome excludes marketing, overhead, advances, and other-label distribution income. Preserve that definition unless this directive explicitly says otherwise.
- `ChartManager` retires records before `CompetitorManager.ProcessWeeklyRevenue` runs. Retirement-week revenue is therefore absent from `lifetimeLabelNet`; do not add a synthetic settlement.
- The current album assembler may choose `AlbumFormat.Compilation` without four reusable singles. It then attempts to resolve up to four recent IDs from `artist.releasedSingleIds` and fills the remaining track count with generated material. There is no existing four-single compilation eligibility gate.
- `ApplyReleasePromotion` consumes per-region RNG draws. A zero-draw album drop therefore requires promotion randomness to be sampled and stored when the project is scheduled, then applied deterministically at drop.
- A dropped artist remains in `ArtistManager`'s registry, but `AILabel.DropArtist` removes the artist from the roster, clears `labelId`, and changes career state. A pending project must not depend on current roster membership or current artist state.
- `AbsorbLabel` transfers the roster and live records to the distributor and rewrites live record label IDs. It does not know about not-yet-released album records or scheduled projects.
- Lower record IDs are not guaranteed by collection order. Any new project collection that affects execution must preserve explicit creation order.

## Definitions

Use these terms consistently.

- **OrphanSingle:** the current standalone single strategy.
- **AlbumStandalone:** an album project whose album releases immediately in the successful weekly release attempt; it has no promo single and never enters the pending-drop queue.
- **AlbumWithPromo:** an album project whose promo single releases in the successful weekly release attempt and whose already-generated album is scheduled to drop later.
- **Project scheduling week:** the week in which `TryReleaseRecord` successfully creates the project and releases its first public record.
- **Terminal project state:** `Released`, `Cancelled`, or `PendingAtAuditEnd`. Transfer is ownership history, not a terminal state.
- **Project realized net:** the sum of the eligible booked contribution nets for the records that belong to the project, each already reduced by its own sunk production cost. Marketing remains excluded, matching 3A memory.

## Non-goals

- No withheld-strategy calibration. `AlbumStandalone` must exist and compete honestly, but its decade-scale emergence is a later phase.
- No compilation-use count, comped-single tagging, or staleness penalty.
- No EP strategy.
- No changes to base demand curves, chart scoring, chart capacity, retirement constants, release-generation distributions, or the single simulator.
- The only new weekly demand term is the linked-album cannibalization multiplier defined in B4. It must evaluate to an exact multiplier of `1.0` for every unlinked or no-longer-linked album.
- No multi-year calibration in this phase.
- No direct CSV file I/O in gameplay classes. Gameplay exposes events and read-only snapshots; `ChartAuditRunner` owns writers.

---

# Checkpoint A: Prior repair

## A1. Add a final-week censoring snapshot

At the end of the requested audit run, before writers close, emit `live-records-snapshot.csv` with one row for every still-active runtime record:

```text
week,year,recordId,labelId,artistId,format,ageWeeks,lifetimeLabelNet,sunkProductionCost,observedNetLowerBound,currentPosition,totalUnitsSold
```

Define:

```text
observedNetLowerBound = lifetimeLabelNet - sunkProductionCost
```

This is a lower bound on terminal booked contribution net, not a completed outcome and not a point estimate. Do not merge it into `release-outcomes.csv`, do not train memory from it, and do not claim that an album error band passed by treating the lower bound as final realized net.

For prior calibration, report all three views by format and, where sample size permits, career-state band:

1. exact signed error over retired eligible outcomes;
2. strategy coverage: retired, live, and unmatched counts;
3. the all-cohort **signed-error ceiling** obtained by substituting each live record's lower bound for final net.

Because future booked net can only reduce `projected - realized` relative to that substituted value, the third statistic is an error ceiling. It is not a two-sided censoring correction. The `+/- $5,000` goal below is a calibration target; it is a hard pass only for exact completed outcomes. Censored results may prove overprojection but cannot prove a two-sided pass without a defined terminal-net estimator.

## A2. Make charged album production cost format-aware

Add an exported setting to `CompetitorManager`:

```csharp
[Export(PropertyHint.Range, "0,2,0.05")]
private float compilationProductionMultiplier = 0.60f;
```

Centralize production-cost calculation in one deterministic helper and use it both when charging a generated record and when assigning `sunkProductionCost`:

```text
Single = label.GetProductionCost()

Compilation Album = label.GetProductionCost() * compilationProductionMultiplier
                  + albumPackagingFixedCost * actualPackaging

Other Album = label.GetProductionCost() * 2.4
            + albumPackagingFixedCost * actualPackaging
```

Use the generated album's actual `albumFormat` and `packaging` at charge time. Do not infer compilation cost from genre.

The lower compilation multiplier is a calibration rule, not a claim that the current assembler generates no new material. The current assembler may still create several non-single tracks for a compilation; leave that generation behavior unchanged.

For the analytic prior, add a deterministic, side-effect-free `HasFourResolvableSingles` helper that mirrors the assembler's reverse traversal and live/archive resolution rules but stops after four successful resolutions. Do not call the telemetry-mutating `TryResolveTrackSnapshot` from the prior; expose a pure lookup or equivalent read-only helper. This four-single threshold is a new **prior proxy**, not an existing generation gate. If true, price the Album prior with `compilationProductionMultiplier`; otherwise use `2.4`. Continue to use `priorAssumedAlbumPackaging` in the prior.

Record both `assumedCompilationCost` and the actual generated `albumFormat` in audit analysis so approximation error is visible.

## A3. Repair the prior shape

Scalar-only probes failed in 3A, so deterministic shape changes inside `CalculatePriorNet` are authorized. Demand-side constants remain frozen.

Known defects:

- The 3A Single prior materially underprojects a heavy-tailed outcome distribution: approximately `$1K` projected mean versus `$13K-$14K` realized mean in the measured seeds.
- The Album prior did not anticipate lower compilation production cost.
- The Single prior uses a genre-neutral demand factor, while the Album prior uses a genre/year market factor. This can route nearly every adult act to Album and remove adult records from the singles population.

Implement the smallest deterministic correction that addresses those defects. Preferred order:

1. Add a risk-neutral expected-tail term to the Single expected-units calculation. The term must be a deterministic function of existing artist quality, career state, label reach, and genre/year market information. It must not call `CalculateRecordQuality`, inspect future RNG, or consume a draw.
2. Use A2's compilation-cost proxy in the Album prior.
3. Add a bounded Single genre/year market factor only if the first two changes do not restore adult singles. Derive it from existing regional market data; do not introduce a hand-authored adult/youth bonus table.

Do not tune against aggregate means alone. Report projected and realized net by at least these career-state groups when counts permit: New/Unsigned, Rising, Established, and Star/Superstar. A single blockbuster correction that fixes the global mean while making every ordinary act implausibly profitable is not acceptable.

Preserve the enabled decision's RNG contract: compute Single then Album deterministically, blend memory, then draw exactly one Single projection noise and one Album projection noise in that order. The album-disabled path must still return Single before inspecting memory or drawing noise.

## A4. Checkpoint A validation

Run 52 weeks for seeds `1001`, `1002`, and `1003`. Run enabled seed `1001` twice in independent processes.

### Hard gates

| Check | Required result |
|---|---:|
| Album-disabled seed-1001 units | `154,810,982` exactly |
| `market-revenue.csv` SHA-256 | `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866` |
| `release-capacity.csv` SHA-256 | `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461` |
| Enabled determinism | all emitted CSVs byte-identical across the two seed-1001 runs |
| Album choice share | `18%-28%` of successful economic decisions |
| Adult-genre Album choice share | `45%-75%` of successful adult decisions |
| Youth-genre Album choice share | `2%-15%` of successful youth decisions; generated youth albums overwhelmingly Compilation |
| Adult share of album-chart rows | at least `95%` |
| Closed Top-40 median life | `11.0-11.5` weeks per seed |
| Quality/outcome Pearson | at least `0.50` per seed |

Use the same adult and youth genre definitions as `RevenueMemoryROIAudit.md`.

### Required diagnostics and calibration targets

- Adult-genre share of singles-chart rows, per seed, compared with the 3A result. It must rise materially; report the exact delta rather than inventing a threshold after the run.
- Exact completed Single and Album mean signed error, plus sample count.
- Live count and signed-error ceiling from A1 for each format.
- Target: exact completed mean signed error within `+/- $5,000`. Report separately when the Pearson result remains below the earlier `0.535-0.595` reference band.

The adult-singles explanation is a hypothesis. If adult singles return and Pearson does not recover, stop and report that the selection mechanism is insufficient; do not describe it as confirmed and do not alter demand or chart constants.

If any hard gate fails, stop after Checkpoint A. Do not proceed to B.

---

# Checkpoint B: Album project pipeline

## B1. Add explicit project state and strategy types

Add a runtime-only `AlbumProject` model. Keep it out of Godot resources unless serialization is genuinely required.

Minimum state:

```text
projectId
creationSequence
originalLabelId
currentLabelId
artistId
scheduledWeek
scheduledDate
dropWeek
dropDate
strategy                 // AlbumStandalone or AlbumWithPromo
albumRecord              // complete, generated at scheduling
promoSingleRecord         // null for standalone
promoSingleId             // null for standalone
terminalState             // Released, Cancelled, PendingAtAuditEnd
transferCount
wasTransferred
albumProductionCost
promoProductionCost
albumPromotionSnapshot
albumMarketingBudgetPlanned
heldPromoOutcome
promoOutcomeState
albumOutcomeState
```

Add a three-way strategy enum used by `ReleasePlan`:

```text
OrphanSingle
AlbumStandalone
AlbumWithPromo
```

Project IDs and record IDs must come from monotonic counters. Maintain pending projects in explicit `creationSequence` order.

### Release timing

- `OrphanSingle`: generate and release immediately, unchanged except for strategy telemetry.
- `AlbumStandalone`: generate the complete album project and release the album immediately in the successful `TryReleaseRecord` call. Set `dropWeek = scheduledWeek`. Do not enqueue it.
- `AlbumWithPromo`: generate the complete album and promo single, release the promo single immediately, and enqueue the album for `albumDropGapWeeks`.

Add an exported inclusive gap range with defaults `3` and `5`. Draw the gap once at scheduling.

### RNG contract

All project-specific random generation must occur during the successful scheduling attempt in a documented stable order:

1. evaluate the three strategy expectations and their specified decision noise;
2. generate the complete album record;
3. if needed, derive the promo record from the chosen album track without a second song-generation roll;
4. draw the drop gap for `AlbumWithPromo`;
5. draw and store every random component needed for album promotion at its future drop.

The drop-week path for an already-scheduled project must consume zero RNG draws. Do not call the current randomizing `ApplyReleasePromotion` directly at drop. Split promotion into:

- a scheduling-time snapshot builder that captures per-region awareness/sentiment random factors and perceived-quality inputs; and
- a deterministic applicator that combines that snapshot with the actual drop marketing budget and current owner infrastructure.

For a dropped artist, use the scheduling-time artist promotion snapshot. Do not use the artist's later `Dropped` career state to recalculate launch scale.

### Promo track construction

Choose the highest-quality track from `album.nonSingleTracks`; break ties by original array order. Do not select an already-released `trackRef` from a compilation.

Create the promo `Record` deterministically from that track:

- copy title and genre from the track;
- copy album artist and label identity;
- assign a new record ID;
- set hook strength, production quality, and danceability so `RecordRuntimeData.GetQuality()` equals the track's stored quality;
- copy only non-quality attributes that do not require RNG.

Replace the chosen entry in the album composition with a released-single snapshot referencing the promo record ID, keep total track count unchanged, set `isReleasedSingle = true`, and append the ID to `album.leadSingleIds`. Recalculate `pooledAppeal` only if the data move changes the current `GetAllTracks()` result; it must not receive new random input.

### Cost and bookkeeping semantics

- Charge album production at project scheduling using A2's actual generated-format helper.
- For `AlbumWithPromo`, also charge promo production and clamped promo marketing at scheduling.
- For `AlbumStandalone`, charge clamped album marketing immediately and release normally.
- For a scheduled album, charge its clamped album marketing at drop. Do not charge album production twice.
- Store the prepaid production cost on the appropriate runtime object when that record is actually released.
- Continue to exclude marketing from `realizedNet` and revenue memory.
- A scheduling attempt that cannot preserve the existing minimum reserve after required production and first-event marketing fails and creates no project. Generation draws already consumed by the failed attempt remain part of the enabled RNG stream, matching current `TryReleaseRecord` behavior.

### Cooldown and counters

- Reset `artist.weeksSinceLastRelease` when the first public record releases.
- For `AlbumWithPromo`, reset it again when the album drops if the artist still exists; do not require roster membership.
- The album drop bypasses the weekly release roll and artist-selection gate.
- Preserve the exact meanings of `WeeklyReleaseRollsFired`, `WeeklySuccessfulReleases`, `WeeklyFailedReleaseRolls`, and `WeeklyCooldownMismatchRolls`: one successful weekly roll means one successful strategy/project initiation, not one count per physical record.
- Add `WeeklyPipelineAlbumDrops`, reset alongside the existing weekly counters before pipeline processing.
- In `OnWeekEnded`, use this order: `ProcessWeeklyRevenue`; reset weekly counters; process due pipeline drops in creation order; process weekly release rolls without resetting the counters again.
- Keep `release-capacity.csv` unchanged. Put the new drop count in project telemetry, not that existing schema.

## B2. Project survival, cancellation, and transfer

For each due pending project:

- If the current owner is active, release the album even if the artist has left the roster or is marked Dropped.
- If the current owner is inactive for a reason other than an absorption already handled by `AbsorbLabel`, cancel the album. Production remains sunk and no album runtime record is created.
- Projects scheduled beyond the audit horizon remain `PendingAtAuditEnd`; do not force-drop them for validation.

Extend `AbsorbLabel` to transfer pending projects in creation order alongside roster and live-record migration:

- rewrite `currentLabelId` and the not-yet-released album record's `labelId` to the distributor;
- preserve `originalLabelId`;
- increment `transferCount` and set `wasTransferred = true`;
- keep the original project ID, record IDs, drop date, generation snapshot, and creation sequence;
- do not mark transfer as terminal and do not count it as a released/cancelled/pending project.

If ownership transfers more than once, repeat those rules. A terminal project's final state remains Released or Cancelled, with transfer history reported separately.

## B3. Promo synergy at album launch

At drop, read the linked promo single's best peak known at that moment. A still-climbing single uses its current best peak. A retired single uses its archived peak stored by the project when the retirement event arrives.

Define a normalized `promoPeakScore` where higher is better:

```text
never charted or peak position > promoFlopThreshold => 0
peak position == 1                                => 1
otherwise linearly map [promoFlopThreshold..1] to [0..1]
```

Add exported defaults:

```text
promoFlopThreshold = 80
promoAwarenessBonusMax = 0.25
promoStockBonusMax = 0.80
promoStockFlopFloor = 0.85
```

Apply:

```text
awarenessBonus = promoAwarenessBonusMax * promoPeakScore

stockMultiplier = promoPeakScore == 0
    ? promoStockFlopFloor
    : 1 + promoStockBonusMax * promoPeakScore
```

Apply the awareness bonus after deterministic base launch awareness is calculated and clamp to `[0,1]`. Apply the stock multiplier to each region's deterministic initial stock before integer rounding. Standalone albums receive awareness bonus `0` and stock multiplier `1`.

Telemetry and validation must correlate `promoPeakScore`, not raw `promoPeakAtDrop`, with launch awareness and stock. Raw chart position has the opposite sign because position `1` is best.

## B4. Linked-release cannibalization

Add the minimum runtime linkage needed for an album to resolve its promo single. Use the single's live `radioHeat` as the normalized activity measure; it already lies in `[0,1]` and avoids a new units normalization constant.

Add:

```csharp
[Export(PropertyHint.Range, "0,1,0.01")]
private float cannibalizationStrength = 0.15f;
```

At the start of each album update, compute and store:

```text
singleHeat = linked promo runtime exists and is not retired
    ? clamp(linkedPromo.radioHeat, 0, 1)
    : 0

cannibalizationSuppression = clamp(cannibalizationStrength, 0, 1) * singleHeat
```

In `AlbumSimulator.CalculateRegionalSales`, apply one unconditional multiplier after the existing conversion factors and before inventory/capacity clipping:

```text
rawDemandBeforeCannibalization = buyerPool * awareness * conversion
rawSales = rawDemandBeforeCannibalization * (1 - cannibalizationSuppression)
```

For standalone albums, missing links, and retired promo singles, suppression must be exactly `0` and the multiplier exactly `1`. Do not branch to a separate demand formula.

Accumulate demand-weighted telemetry:

```text
suppressedDemand / rawDemandBeforeCannibalization
```

Report it for linked-live album weeks and standalone album weeks separately. Linked-live must be greater than zero in aggregate; standalone must be exactly zero.

## B5. Make the three-way economic decision

When albums are enabled, calculate these deterministic pre-noise expectations:

```text
expectedOrphanSingle = repaired A3 Single prior
expectedAlbumStandalone = repaired A3 Album prior

expectedAlbumWithPromo = expectedAlbumStandalone
                       + expectedPromoLift
                       - expectedCannibalizationLoss
                       + expectedPromoSingleNet
```

`expectedPromoSingleNet` is the repaired A3 Single prior and therefore already includes Single production cost. Do not deduct production twice. Marketing remains excluded from all three expectations, consistent with 3A realized memory. Do not subtract marketing from only the promo strategy.

Derive expected promo lift from awareness headroom:

```text
projectedLaunchAwareness = deterministic projection using the same artist
                           momentum/reputation/career-state and label reputation/
                           campaign inputs used by promotion

awarenessHeadroom = 1 - clamp(projectedLaunchAwareness, 0, 1)
expectedPromoLift = awarenessHeadroom * exportedPromoLiftScalar
```

Estimate cannibalization from the same `cannibalizationStrength` and a deterministic expected promo heat; do not inspect a future runtime record. Estimate promo-single net with the repaired Single prior inputs for this artist/label, without updating memory or drawing extra generation quality.

Do not special-case career state. High-stature acts should naturally have less awareness headroom.

Blend expectations with memory at the same comparison level:

- OrphanSingle uses Single memory.
- Both album strategies use Album project memory.

Apply decision noise in a documented fixed three-draw order: OrphanSingle, AlbumStandalone, AlbumWithPromo. This intentionally replaces 3A's two-draw enabled contract only after Checkpoint A has passed. The album-disabled branch still returns OrphanSingle before all prior, memory, and RNG work.

Choose the strictly greatest projected value; resolve ties in this stable order:

```text
OrphanSingle, AlbumStandalone, AlbumWithPromo
```

## B6. Route memory at project level

Add runtime-only project-role metadata sufficient for `RecordRetired` to distinguish:

- orphan single;
- standalone album;
- promo single for a project;
- album for a promo project.

Routing rules:

- Eligible OrphanSingle retirement updates Single memory once.
- Eligible AlbumStandalone retirement updates Album memory once.
- Eligible promo-single retirement emits normal outcome telemetry but does not immediately update either memory. Store its realized net on the project.
- Eligible linked-album retirement likewise stores its realized net. As soon as both outcomes exist, regardless of retirement order, sum them and update Album memory once.
- If the album is cancelled before drop, redirect the promo outcome to Single memory. If the promo has already retired, apply its held outcome immediately; otherwise mark the route and apply it when the promo later retires.
- No physical record may update more than one memory, and no project may update Album memory more than once.

If a project transfers before its combined outcome is folded, credit the completed project observation to `currentLabelId` at fold time. Preserve each physical outcome's emitted label ID as observed; do not rewrite historical CSV rows.

Do not use the draft invariant `single observations + project observations = eligible retirements`; a promo project has two eligible record retirements but one Album-memory observation.

Validate with record-equivalent accounting:

```text
eligible retired records
    = orphan singles applied to Single memory
    + standalone albums applied to Album memory
    + 2 * completed promo projects applied to Album memory
    + retired promo outcomes currently held
    + retired promo outcomes redirected/applied after cancellation
    + any eligible project record explicitly reported as unresolved
```

Also report the simpler observation accounting separately:

```text
Single-memory observations
+ Album-memory observations
+ held/unresolved memory observations
```

These two totals answer different questions and must not be compared as if they were identical.

## B7. Telemetry

### `album-projects.csv`

Emit one final snapshot row per project at audit termination, ordered by `creationSequence`:

```text
projectId,creationSequence,originalLabelId,currentLabelId,tierAtSchedule,genre,careerStateAtSchedule,scheduledWeek,dropWeek,strategy,albumRecordId,promoSingleId,promoPeakAtDrop,promoPeakScore,synergyAwarenessApplied,synergyStockMultiplier,terminalState,wasTransferred,transferCount,albumRetired,promoRetired,projectRealizedNet
```

`projectRealizedNet` is populated only when all released project records required by the strategy have retired. Cancelled and pending projects may leave it blank; report their sunk cost separately in the audit.

Reconciliation uses terminal state only:

```text
scheduled projects = Released + Cancelled + PendingAtAuditEnd
```

Transfer is checked independently through `wasTransferred` and `transferCount`.

### `release-strategy.csv`

This is the one existing schema intentionally extended in Checkpoint B. Preserve the existing columns in their current order, then append:

```text
projectId,strategy,projectedOrphanSingleNet,projectedAlbumStandaloneNet,projectedAlbumWithPromoNet,promoSingleId
```

For an album strategy, keep the existing `recordId` field equal to the album record ID, including when its drop is pending. For OrphanSingle it remains the single record ID. Emit the row only after affordability succeeds and the release/project initiation succeeds.

All other existing CSV schemas, including `release-capacity.csv`, remain unchanged. New fields needed for analysis belong in `album-projects.csv`, `live-records-snapshot.csv`, or another new project-specific CSV approved by this directive.

## B8. Checkpoint B validation

Run the same three 52-week seeds and the same independent enabled determinism repeat.

### Baseline and determinism

- The album-disabled seed-1001 units and both checksum guards remain exact.
- The two enabled seed-1001 runs emit byte-identical CSVs.
- The album-disabled path emits no album projects and consumes no new strategy/project RNG.

### Project reconciliation

- `scheduled = Released + Cancelled + PendingAtAuditEnd` exactly per seed.
- Transfer counts reconcile independently and transferred projects can later be Released or Cancelled.
- No pending project is overdue beyond the inclusive maximum gap while its current owner remains active.
- Every released promo single and album has exactly one project/role link; every pending album has no runtime record yet.
- Drop-week processing consumes zero RNG draws in an isolated deterministic instrumentation check.

### Chart and launch behavior

- Promo singles use the normal single chart, revenue, retirement, and archive paths.
- Existing entries/exits, zombie, age-14 tier-observability, closed Top-40 life, and quality/outcome guards continue to pass.
- Among released AlbumWithPromo projects, report Pearson correlations of `promoPeakScore` with `initialLaunchAwareness` and `initialLaunchStock`. Both must be positive when variance and sample size are sufficient; report `N` and `N/A` instead of fabricating a coefficient when either variable is constant.
- Report mean demand-weighted cannibalization for linked-live album weeks; it must be greater than zero. Standalone suppression must be exactly zero.

### Mix and memory

- Re-run all Checkpoint A format-choice and chart-composition hard gates using successful strategy decisions for choice shares. Also report physical album drops separately so the three-to-five-week end-of-run censoring is visible.
- Report the 1960 AlbumStandalone/AlbumWithPromo decision split with no acceptance band.
- Prove the B6 record-equivalent accounting and observation accounting independently, with no duplicate memory contribution.
- Join every released project's album outcome and, where applicable, promo outcome by IDs. Report pending/cancelled/unretired records rather than dropping them from the denominator.

## Stop conditions and guardrails

- If Checkpoint A cannot pass with prior-shape changes and compilation costing alone, stop with the A1 exact and censored diagnostics. Do not proceed to B.
- If adult singles return but Pearson remains below `0.50`, stop and report the failed causal hypothesis.
- If B breaks an A hard gate, isolate and report whether the movement comes from the extra promo population, launch synergy, cannibalization, delayed album drops, or release-capacity/cooldown interaction. Do not retune A's prior merely to hide a B-side regression.
- Do not change demand, chart, retirement, or generation constants to meet a validation band.
- Do not silently change an existing CSV schema except for the explicitly appended `release-strategy.csv` fields.
- Do not describe a lower-bound censoring statistic as a completed or censoring-corrected realized outcome.

## Required audit handoff

Write `SimTools/AlbumProjectPipelineAudit.md` with:

1. exact code and data-model changes;
2. all new exported settings, starting values, probes, and final values;
3. RNG order and proof that due drops consume zero project draws;
4. baseline checksums and enabled determinism hashes;
5. every Checkpoint A and B validation table by seed;
6. exact versus censored projection-error results, clearly labeled;
7. project reconciliation, transfer history, and memory accounting;
8. known limitations, including retirement-week revenue truncation and any end-of-run pending projects;
9. build result and any pre-existing warnings.

If a gate fails, leave the implementation at the last coherent checkpoint and state plainly that the phase did not pass. Do not soften a failed hard gate into an informal success.
