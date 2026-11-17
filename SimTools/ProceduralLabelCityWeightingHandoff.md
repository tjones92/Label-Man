# Procedural label placement weighted by city — handoff (2026-11-16)

A separate change, split off from the census-population work (LpGenreAllocationFollowup.md §29–30). Goal: put the
procedural labels, both the opening roster and runtime births, in the cities the 1960 record industry was actually in.
Today two hard-coded pickers place them, and neither reads population, industry data or any authored per-region figure.

## What places labels today

**Opening roster** (`AILabelFactory.GenerateAllLabels`, `ChartManager.cs:635`): 43 named historical templates at their
real cities, then about 550 procedural labels from `GenerateProceduralLabel` (`Systems/AILabelFactory.cs:224`). City
choice is `GetRandomCity(archetype)` (`AILabelFactory.cs:402`):

- Archetype overrides: CountrySpecialist → always Nashville; SoulFactory → Detroit 60%, else a Great Lakes city;
  TeenHitMachine → New York 60% / Philadelphia; FolkBoutique and JazzPrestige → New York; GospelPowerhouse → a
  "southern" city; BluesRoots → Chicago 40%, else southern; RockRebel → southern or West Coast, 50/50.
- Everything else: `GetRandomCityAny` rolls a fixed split: northeast 30%, Great Lakes 10, Great Plains 10, "southern" 20,
  Rockies 10, West Coast 20. Then it picks a city **uniformly** within that list.
- The `southernCities` list mixes two regions: Memphis, Nashville, New Orleans, Atlanta and Miami map to `deepsouth`
  (`CityToRegion`, `AILabelFactory.cs:501`), while Houston, Dallas, San Antonio, Phoenix and Albuquerque map to
  `southwest`. The "southern 20%" is therefore half Southwest.

**Runtime births** (`LabelLifecycleManager.SpawnNewLabel` → `LabelGenerator.GenerateSingleLabel`,
`Systems/LabelGenerator.cs:57`): a **uniform** draw over 19 cities (`LabelGenerator.cs:28`). Seattle is as likely as
New York, and there is no West Coast city but LA, San Francisco and Seattle. `RuntimeLabelProfileFactory` then resolves
the home region from the city (`RuntimeLabelProfileFactory.cs:166–179`) and assigns the archetype afterwards, so a
birth's city is independent of its genre.

**`MusicInfrastructure.localLabelCount` is never read.** It is authored per region in `chart_manager.tscn` (East Coast
235, Great Lakes 135, West Coast 95, Great Plains 55, Southwest 42, Deep South 35, Rockies 7). `--census-population`
scales it, without effect. It was authored at one per-capita density of each region's authored population, like
stations and stores (§29), so it encodes population and nothing else. On the authored 15M South it would give the Deep
South 6% of labels. **Do not use it as the weight**: "labels ∝ people" is the wrong model for an industry concentrated
in a few hubs.

## Result: opening labels, seed 2002 (`bsc13-2002-sc-label-geography.csv`, week 1)

| Region | Labels | Share | 1960 census share |
|---|---|---|---|
| East Coast | 218 | 37% | 31% |
| Deep South | 114 | 19% | 17% |
| Great Lakes | 86 | 14% | 22% |
| West Coast | 75 | 13% | 11% |
| Southwest | 74 | 12% | 8% |
| Great Plains | 18 | 3% | 9% |
| Rockies | 11 | 2% | 3% |

Deep South: Nashville 67, Memphis 15, Miami 15, Atlanta 8, New Orleans 8, Jackson 1. Rockies: Salt Lake City 7,
Denver 3, Billings 1. Visible defects:
- Uniform picks inside a region (Salt Lake City has more labels than Denver; Miami as many as Memphis).
- The Great Lakes are undersized for Chicago and Detroit. The Great Plains get 3%, with St. Louis and Kansas City in them.
- The Southwest (12%) is oversized relative to Houston's and Dallas's real activity.
- Runtime births ignore everything above.

## Proposed change

1. **One city-weight table**, shared by both pickers: a weight per canonical city in the `DistanceModel` city catalog,
   sourced from a historical count of active record labels by city around 1958–62. Candidate sources: Billboard's
   annual "Buyer's Guide" / International Talent and Record Buyer's Guide label lists, the Galen Gart "First Pressings"
   label tables, and the 45cat / 45worlds label-by-city indices. **Research first; do not invent weights.** If only
   rankings are recoverable, record that and use a stated rank → weight rule.
2. **Archetype as a tilt, not an override.** Weight = city weight × archetype affinity for that city (CountrySpecialist
   strongly toward Nashville, with some Shreveport-style Southern and West Coast country; SoulFactory toward Detroit and
   Chicago; and so on). This keeps the archetype geography but stops one archetype filling one city.
3. **Runtime births use the same table.** Either draw the city after `RuntimeLabelProfileFactory` assigns the archetype
   (preferred, if the order can move without breaking its stable PRNG), or use the city weights alone.
4. Add missing hub cities to the catalog only if the research shows they matter (a likely candidate is Shreveport;
   Cincinnati and Newark are already present). Each new city needs a `DistanceModel` entry and a `CityToRegion` row.
5. Behind a switch (`--label-city-weights`), off byte-inert, like every other calibration change.

## Caveats

- **RNG reshuffle.** Changing which city a label draws changes every downstream draw in the global stream
  (memory note `one-conversion-reshuffles-the-rng-stream`). Single-seed deltas are not evidence. Screen at least three
  seeds, 104 weeks, then a decade A/B at audit end.
- **Weighted draws must consume the same number of `GameRandom` draws per label** wherever possible, so the
  reshuffle stays confined to city-dependent state. A single `Randf()` against a cumulative weight table replaces the
  current two draws (region roll + index). Document the difference.
- **Interaction with `--census-population`.** The Southern-labels question (§30) is not caused by label count: the
  Deep South already holds about its census share of labels. Expect this change to move regional label mix and
  genre supply (Nashville Country, Detroit/Chicago R&S), so screen it on both the authored and census bases.
- Small-label home territory (`SmallLabelHomeTerritory.Apply`), `GetRegionalStrength` and `GetDistributionRegions`
  key off the city, so their outputs move too. That is intended, but check the small-label survival and defunct counts.

## Screen

Arms on the current candidate (`run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/candidate-request.json
-Arms sc -LpScale 2.65 -Weeks 104`), seeds 2002 / 1001 / 3003: control, `--label-city-weights`, and both again with
`--census-population --lp-purse-adult-weight`. Measure:
- opening and end-of-run label counts by region and city (`label-geography`)
- small-label gross by home region (`label-finance`) and defunct counts
- genre share error and the Pop / R&S / Country year-end shares (`year-end-hot100`)
- regional home-label share of sales (`--regional-home-sales`, §30)
- Top 10 / Top 40 label concentration

Adopt only if genre and economy metrics hold inside the seed band; the gain is fidelity of the label map.

## Done means

- A sourced city-weight table, with the source cited in code.
- Both pickers using it behind the switch; off is byte-inert (compare a 27-week run's streams against a control).
- A check in the retail-purchase-envelope or a new check harness asserting the regional label shares the table implies.
- The three-seed screen written up as a section of this file with an adopt / don't-adopt call.
