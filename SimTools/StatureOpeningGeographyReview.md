# Opening-following geography investigation — 2026-11-06

Latest follow-up: [national standing for Star and above](StatureRegionalSensitivityReview.md)
isolates the regional route and tests a high-tier national-evidence candidate.

Follow-up completed: [release-level two-year geography replay](StatureOpeningGeographyReplayReview.md) measures three allocation rules with conserved stock and independent hysteresis. It generally raises standing and launch proposals, with a documented one-week band exception. All 600 original two-year CSVs match their baselines.

The regional-history mismatch is confirmed, but it accounts for a small part of the previously measured opening-source budget sensitivity. Localizing a fixed number of people can strengthen the regional standing route. A geography correction therefore needs a clearly specified interpretation of release territories before a policy experiment.

## Source trace and unresolved semantics

`OpeningCareerService.Entry` sets `regionId = artist.homeRegion` for every RegionalName release. `Finish` assigns these releases one to three markets when charted, and one or two otherwise. `SeedOpeningFollowing` filters to the last five years and positive peaks, aggregates young cohorts and adults across releases, and then spreads the aggregates over all seven regions by population. It never reads either geographic field.

This loss happens before distribution: fixing it requires allocating release contributions before combining them. The peak-to-people mapping, age split, decay and total people are separate choices and should be held fixed in a geography comparison.

The stored fields do not uniquely determine an allocation. `regionId` is documented as the market a regional seller **came out of**, not its complete territory; `markets` counts territories without listing them. The history generator's `ShapeTail` also describes a RegionalName act as having “crossed over once,” yet `Finish` still assigns regional market counts to that release. A blanket home-only allocation would choose an interpretation of that crossover and discard the other-market count. Conversely, a count below seven also occurs outside RegionalName histories without a named region. Those cases need an explicit rule rather than an inferred list of territories.

## Targeted measurement in the existing two-year worlds

The new analysis selects the fixed RegionalName IDs from each opening census and reuses the existing national-only, regional-only and combined source-removal replay. Each alternative has independent hysteresis and observed sales, TV and peer references. Combined replay reproduces every selected C# zero-source band/status. Opening censuses match exactly between arms, and all 20 acts in each seed have the seven-region population distribution.

| Seed | RegionalName opening followers | Share of all opening-source followers |
|---|---:|---:|
| 1001 | 727,075 | 5.84% |
| 2002 | 819,018 | 6.58% |

The following denominators contain only these 20 acts. Reads are available single launch requests in 1961, and uplifts are full standing-table proposals above the legacy envelope, before caps or spending.

| Seed / arm | Available reads | Actual uplifts | Eliminated: national components only | Eliminated: regional draw/reach only | Eliminated: both |
|---|---:|---:|---:|---:|---:|
| 1001 control | 13 | 1 | 0 | 0 | 0 |
| 1001 treatment | 25 | 7 | 1 | 1 | 2 |
| 2002 control | 34 | 10 | 0 | 0 | 1 |
| 2002 treatment | 40 | 19 | 0 | 0 | 2 |

Combined removal reduces no additional uplifts beyond those eliminated. None of these acts' 1960 uplifts is reduced by combined removal. The input partitions overlap and are not additive causal shares. These are within-world reader sensitivities, not a geographic reallocation experiment or a measurement of paid exposure. The corresponding all-75-act 1961 combined-removal counts were 28/25/29/44; the explicitly regional cohort contributes only 0/2/1/2 of those counts.

## Fixed-total geography screen

For source stock `N`, total population `P`, covered population `S`, and the existing density reference `K`, population allocation within a footprint gives:

- Strongest regional follower density: `min(1, N / (S K))`.
- Population-weighted follower conversion reach: `(S / P) min(1, N / (S K))`.

Before clipping, conversion reach is `N / (P K)`, independent of footprint. Concentration raises regional draw, while weighted reach falls only once a destination's density clips. Geographic coverage and this density-weighted reach measure are different quantities.

The artifact enumerates all 7 one-region, 21 two-region and 35 three-region footprints for each act. These are broad scenario ranges, without home-market constraints or reconstructed release territories. They use only opening source stock, not later audiences. Record reach also includes sales response and is not calculated in this screen. An act's history can span more territories than any individual release, so the scenarios are not an inferred bound on its true career footprint.

For Washington Fairley (`artist_00283`, seed 1001), 29,136 opening followers produce national draw and conversion reach of 0.0503. In a hypothetical two-region footprint, regional draw becomes 0.0900–0.4227 while conversion reach stays 0.0503. In a one-region footprint, draw becomes 0.1522–1.0000 and conversion reach 0.0291–0.0503. These are input values, not predicted bands. They demonstrate why preserving total people while localizing them need not reduce standing.

## Next experiment specification

Keep live audience stock, buying, draw, media registration and RNG untouched while testing standing's interpretation of the known regional source. First record release-level qualifying contributions, years, formats, peaks, market counts and named origins. Resolve the crossover semantics and distinguish named-origin histories from count-only histories. Compare declared rules for missing destination identities; do not manufacture observed territories from the market count.

A standing-only geography shadow should preserve young/adult national totals, change only the surviving source's regional allocation, rebuild density and both reach inputs before clipping, and evolve independent hysteresis. Check unchanged national component scores, conserved source totals, neutral reconstruction, input changes and requested budgets before any causal reader trial. Later surviving source relocation is itself a frozen-world assumption: it does not reproduce the region-dependent losses and joins of a world initialized differently.

No gameplay seeder, calibration, audience stock or budget reader was changed in this investigation.

## Reproduction and validation

Run `SimTools/analyze_opening_geography.py --output SimLogs/writing-calibration/stature-opening-geography-v1.json` from the repository root, with the Python interpreter used by the existing audit scripts. Defaults use all four `stature-origin-2y-v1` worlds. The report stores analysis, route-replay, scene and calibration hashes; original world provenance and output manifests remain under `SimLogs/writing-calibration`.

Five new geography tests and two existing route-partition tests pass. Runtime analysis verifies paired censuses, population-weighted distributions and every selected combined C# reconstruction. No new simulation or instrumentation-neutrality claim is made.

Artifacts: [analysis](analyze_opening_geography.py), [tests](test_analyze_opening_geography.py), [machine-readable results](../SimLogs/writing-calibration/stature-opening-geography-v1.json). Previous context: [opening-origin review](StatureOpeningOriginReview.md).
