# Album systemic sweep handoff

## Status

Investigation date: 2026-07-18.

The retained simulator behavior remains the completed M4
`d6-album-monotonic-penetration-through-1968-1001` source state. Two experimental
buyer-pool corrections were measured and rejected, then fully reverted. The only
retained implementation artifact from this pass is:

```text
SimTools/analyze-album-reconciliation.mjs
```

No M5, additional seed, holdout, or replacement 469-week M4 was launched.

## Starting evidence

The monotonic-penetration candidate passed its one-week probes, disabled replay,
and deterministic 104-week candidate/repeat. Its 469-week M4 completed normally
but failed the first annual gate:

```text
1962 Album units:
candidate  6,625,476
control    5,439,136
ratio      1.218112
ceiling    1.20
```

The later failure is much larger:

| Year | Candidate Album units | Control Album units | Ratio |
|---:|---:|---:|---:|
| 1967 | 37,741,051 | 27,352,902 | 1.3798 |
| 1968 | 53,791,247 | 33,128,749 | 1.6237 |

In 1968, gross, label net, and market net are also above their inherited upper
bands. M5 was correctly not launched.

## Adjudication of the attached external findings

### 1. The source asymmetry is real, but the proposed reuse is a category error

`CompetitorManager.GetAlbumPriorExplanation` applies
`CalculateAlbumPriorMarketReconciliation` to the Album AI prior.
`AlbumSimulator.CalculateRegionalSales` does not apply that factor to realized
Album sales.

This is intentional in the documented D5 design, not proof of a missing
realization multiplier. The factor was added so the winner-take-all Single versus
Album decision compares two priors after V2 changed the Single relative-market
input. `GenreMarketV2Audit.md` explicitly says the runtime Album buyer pool
remains normalized to accepted legacy opportunity.

The fixed-input sign check also rejects blindly multiplying realized Album sales
by this factor:

| Year | Traditional Pop | Jazz | Folk | Easy Listening | Rock and Roll | Country | Soul |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1962 | 1.2330 | 0.9022 | 1.0024 | 1.2330 | 1.2330 | 1.0209 | 1.5459 |
| 1967 | 0.7517 | 0.8591 | 0.7517 | 1.2330 | 0.7517 | 1.0025 | 1.8571 |
| 1968 | 0.7028 | 0.8785 | 0.6639 | 1.2080 | 0.7687 | 1.0252 | 1.8571 |

The 1962 candidate is dominated by Traditional Pop and Jazz. Applying the
factor would materially raise Traditional Pop and several other early genres,
making the first gate worse. Inverting it would reverse the late-year direction
for the largest catalog genres. Do not add either form to realized sales without
a separate, population-complete derivation.

### 2. The realized format tilt does not explain the late excess

The new analyzer streams the retained M4 files without loading the
multi-hundred-megabyte settlement ledgers into memory. Its sampled
Top-40/launch format-tilt diagnostic covers about 97% of settlement Album units
through genre joins in 1967-1968:

| Year | Sampled-genre unit coverage | Sampled unit-weighted mean Album tilt |
|---:|---:|---:|
| 1962 | 0.9994 | 1.0376 |
| 1967 | 0.9717 | 0.9825 |
| 1968 | 0.9690 | 0.9751 |

This is a directional diagnostic, not a population-complete causal estimate:
`album-demand-explanation.csv` contains Top-40 Albums plus the stable launch
sample. It is nevertheless enough to reject the claim that the raw Album format
tilt is broadly inflating late realized sales by 1.2x-1.6x. The sampled late
factor is below one.

### 3. Emerging-project memory remains under-isolated, but is not the direct
late gross carrier

The 1966+ project-memory bypass was only accepted at a 52-week checkpoint before
D6 work began. A decade-scale D5-only replay of that exact final repair was not
performed.

That is a real validation gap. However, the retained M4 settlement ledger shows
that 1968 Album units are dominated by Traditional Pop, Rock and Roll, Jazz,
Easy Listening, Country, and Soul. Newly emerging Psychedelic/Hard/Progressive
genres carry comparatively little realized Album volume in this seed. The
memory repair may alter scheduling and downstream topology, but it is not a
direct 53.8M-unit emerging-genre sales story.

### 4. The control confound is confirmed

The retained decade control disables both Genre Market V2 and artist population
lifecycle. M4 enables both. The ratio is therefore a combined-feature result,
not the marginal effect of D6.

The next pass should first compare:

```text
Genre Market V2 enabled, lifecycle disabled
Genre Market V2 enabled, lifecycle enabled
```

with identical seed, instrumentation, and current source. This is the smallest
available isolation of the lifecycle marginal effect because lifecycle currently
requires Genre Market V2.

## Strongest retained finding: two different Album failures

### Early 1962 failure: realization/yield, not excess Album decisions

| 1962 metric | Candidate | Control |
|---|---:|---:|
| Album units | 6,625,476 | 5,439,136 |
| Album decision share | 0.362852 | 0.374274 |
| Active Albums | 2,544 | 2,372 |
| Retired Albums | 1,118 | 1,222 |
| >52-week Album gross share | 0.432914 | 0.231656 |

The candidate makes a lower share of Album decisions yet realizes 21.8% more
Album units. Scheduling/format choice alone cannot explain the first gate.
Catalog stock and per-title realization are already divergent by 1962.

The analyzer's approximate settlement-age split for candidate 1962 is:

| Age | Album units |
|---|---:|
| 0-51 | 3,755,471 |
| 52-103 | 2,307,098 |
| 104+ | 562,907 |

These age rows infer release age from week-one records and first settlement.
Use the existing authoritative cohort analyzer for formal reconciliation.

### Late 1967-1968 failure: catalog persistence

By 1968, release inflow is nearly identical but retirement and surviving stock
are not:

| 1968 metric | Candidate | Control |
|---|---:|---:|
| Albums ever released | 15,782 | 15,624 |
| Albums retired | 6,644 | 8,622 |
| Active Albums | 9,138 | 7,002 |
| Median active Album age | 97 | 76 |
| >52-week Album gross share | 0.431924 | 0.205466 |
| Albums at/above 10-unit floor | 9,026 | 6,764 |

The previously reconciled cohort analysis attributes about 80% of the 1968
Album-gross gap to the difference between candidate and control 52+ cohorts.
Ordinary current-year scheduling is not sufficient to explain this stock.

The structural persistence seam is in the Album realization/retirement system:

- every title receives a full regional buyer-pool calculation;
- catalog decay is per title;
- exhaustion is per title and region;
- global market clearing allocates scarce capacity after those intents exist;
- a flat national 10-unit floor resets catalog relevance; and
- a charted Album needs 52 weeks below both chart and sales relevance clocks.

The monotonic-penetration candidate removed only one rejuvenation path. It did
not materially reduce the surviving stock or the direct current-year buyer-pool
opportunity.

## Rejected experiments from this pass

### A. Release-vintage Album-era opportunity

The experiment kept current genre acceptance but evaluated Album affinity and
purchase willingness at the title's release year. It aligned realized format
centering to the same vintage opportunity.

The 104-week output was byte-identical to the monotonic-penetration candidate
because `albumDemandRiseStartYear` is 1964. The idea therefore cannot change the
known 1962 first gate. It was not retained or run to M4.

### B. Non-increasing per-title regional buyer pool

The second experiment stored each Album-region's first live buyer pool and used:

```text
effective buyer pool = min(first live buyer pool, current vintage buyer pool)
```

This had no new scalar and allowed opportunity to decline but not grow. It
passed the fixed probes and completed 104 and 157 weeks normally. It failed
causally:

| Year | Prior Album ratio | Experimental Album ratio |
|---:|---:|---:|
| 1960 | 1.038821 | 1.038821 |
| 1961 | 1.149424 | 1.197831 |
| 1962 | 1.218112 | 1.232518 |

The change altered market clearing, label finances, closures, and later release
decisions. Reducing some catalog intents rerouted capacity and produced more,
not fewer, Album units at the annual level. The experiment was rejected and
fully reverted.

Artifacts are retained for negative evidence:

```text
d6-album-catalog-opportunity-104-enabled-1001-*
d6-album-catalog-opportunity-through-1962-1001-*
```

## Analyzer

Run:

```powershell
& 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' `
  SimTools/analyze-album-reconciliation.mjs `
  SimLogs/d6-album-monotonic-penetration-through-1968-1001 `
  --annual-only
```

It reports:

- authoritative settlement Album units and gross by year/genre;
- sampled format-tilt and buyer-pool-normalization diagnostics with coverage;
- approximate settlement age cohorts; and
- explicit limitations before the tables.

Do not promote its sampled tilt or inferred age rows to formal causal
reconciliation. Use it to choose where population-complete telemetry is needed.

## Required next pass

### 1. Add population-complete Album-region-week telemetry

The existing Album explanation stream is sampled and cannot resolve the exact
realization bridge. Add an enabled-only stream, leaving every disabled stream
and header unchanged, with at least:

```text
week, year, recordId, regionId, releaseYear, ageWeeks,
buyerPool, formatTilt, effectivePenetration, exhaustion,
rawDemandBeforeCannibalization, serviceableIntent, finalCleared,
currentPosition, weeksSinceLastCharted, weeksSinceSalesAboveFloor,
retirementFloor, retiredAfterSettlement
```

If `MarketReconciliation` is logged, label it as the AI-prior counterfactual;
do not apply it to sales.

### 2. Run the feature-isolation matrix only through 1962 first

Use current source and seed 1001:

1. V2 enabled, lifecycle disabled, 104 weeks.
2. V2 enabled, lifecycle enabled, 104 weeks.
3. Deterministic repeat of each if the first comparison is structurally clean.
4. Extend only the clean pair through 157 weeks / completed 1962.

Compare population-complete Album age/yield/retirement cohorts. This locates
whether the 1962 excess first enters with D5 realization or with D6's marginal
roster/release topology.

### 3. Treat early yield and late persistence as separate correction surfaces

Do not require one multiplier to fix both:

- 1962 needs a realization/yield explanation despite a lower Album decision
  share.
- 1967-1968 needs a catalog-stock/retirement explanation despite nearly equal
  cumulative Album release inflow.

Candidate correction work should not begin until the full realization bridge
shows whether the 1962 gap is already present in raw intent, introduced by
serviceability/clearing, or caused by cohort survival.

### 4. Shortened validation ladder

After a causal candidate exists:

1. build, diff check, D5/D6 fixed probes;
2. 52-week disabled frozen replay;
3. 104-week enabled candidate and independent repeat;
4. 157 weeks through completed 1962 -- hard stop on the known first gate;
5. only if 1962 passes, extend through 1965;
6. only then rerun 469-week M4;
7. do not launch M5 until M4 passes every inherited annual gate.

## Stop decision

The external finding successfully redirected attention from lifecycle-only
files to the shared Album economy, but its proposed realized-sales
`MarketReconciliation` is not supported and has the wrong sign in 1962.

The retained evidence supports:

```text
EARLY_ALBUM_REALIZATION_EXCESS
AND
LATE_CATALOG_PERSISTENCE
```

It does not yet support one safe behavioral correction. The next owner should
instrument the complete Album realization bridge and isolate V2-without-D6 from
V2-with-D6 before changing another scalar or launching another long run.

## Implementation update: 1962 systemic correction accepted (2026-07-18)

The required bridge and isolation matrix are complete. The first annual excess
does not originate in release admission: the lifecycle candidate schedules
fewer 1962 Albums than control (`1,295` versus `1,349`). It originates when D6
restores enough population/release topology for D5's per-title Album
opportunities to accumulate. V2+lifecycle produced 23.6% more Album
record-weeks and 16.7% more raw Album demand than V2-only in 1962, even though
common capacity compressed the cleared difference to 5.1%.

The retained source adds an enabled-only, market-wide Album format budget in
`Systems/ChartManager.cs`. Aggregate serviceable Album intent `A` is collapsed
before common clearing:

```text
effectiveAlbumIntent = A * capacity / (capacity + 2 * A)
```

The format budget is bounded by regional capacity, allocated proportionally
among Album titles, and enforced through spillover. It is not a year or genre
scalar and does not read the control. Disabled behavior remains exact.

Final accepted run family:

```text
d6-album-format-clearing-overlap2-probes-1001-*
d6-album-format-clearing-overlap2-disabled-52-1001-*
d6-album-format-clearing-overlap2-104-1001-*
d6-album-format-clearing-overlap2-104-repeat-1001-*
d6-album-format-clearing-overlap2-through-1962-1001-*
```

Validation:

- build, D5 probes, and D6 fixed probes 1-67 pass;
- disabled replay is 45/45 byte-identical;
- candidate/repeat are 63/63 byte-identical;
- clearing has zero allocation, inventory, reconciliation, or Album-budget
  violations across 1,099 region-weeks;
- 1960 Album units are `1.151010x` control;
- 1961 Album units are `1.062844x` control;
- 1962 Album units are `6,130,092`, or `1.127034x` control, down from
  `6,625,476` / `1.218112x`;
- 1962 Album gross, label net, and market net are `1.131075x`, `1.115768x`,
  and `1.105313x` control; Single and total units remain `0.999135x` and
  `1.003816x`.

Rejected/reverted evidence remains important:

- buyer-pool freeze: 1962 `1.232518x`;
- catalog decay `.984`: 1961 `1.208x`;
- Album retirement floor 15: 1962 `1.227x`;
- overlap pressure 1.0: 1962 `1.2101x`;
- overlap pressure 1.25: 1962 `1.2085x`.

The small format-budget variants were mostly absorbed by economic feedback.
Do not weaken pressure 2.0 based on a static one-week replay.

### Next pass

Start from this exact source and keep the same stop discipline:

1. build and run fixed probes;
2. extend seed 1001 only through completed 1965;
3. require all inherited annual unit/gross/label-net/market-net envelopes through
   1965 and zero structural violations;
4. if clean, run the 469-week M4 candidate/repeat required by the governing
   audit;
5. use `album-realization-bridge.csv` to compare 1967-1968 age bands and active
   stock against control;
6. treat any remaining 52+ excess as catalog persistence, not as permission to
   relax the market-wide overlap correction;
7. do not launch M5 unless every M4 annual gate passes.

No 1965 extension, M4 replacement, additional seed, holdout, or M5 was run in
this implementation pass.
