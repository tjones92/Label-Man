# HQ-preserving recruitment access repair

User-authorized continuation of `codex/local-scene-phase-5`. No label is moved;
the playable city list, birth budget, opportunity prior, roster targets, advances,
scouting ability and distribution projection are preserved.

## Change

- Add `newark` as a nonplayable location in the NYC recruitment catchment. Labels
  retain Newark HQ/origin IDs and text. No player-start option, market, room,
  formation weight or standalone employment model is added.
- Match the ambiguous literal `Jackson` only with the `deepsouth` context to the
  existing `jackson_ms` location. `Jackson, Mississippi` remains independently
  addressable. Other regions and unknown context do not acquire a Mississippi HQ.
- Give Jackson, Indianapolis and Milwaukee geographic anchors. For routes
  involving independent nonplayable places, use the existing approximate
  domestic road formula. Existing playable/catchment road paths retain their
  original `DistanceModel` path and all tier distance/attention limits.
- Catalog content version becomes 2. Old saves may resolve only projections
  owned by the previous `unresolved-literal-hq; distribution-proxy-not-origin`
  migration. Existing explicit bases/origins, other provenance and move histories
  win. A correction records no move and never reruns opening roster allocation.

## Coordinate provenance and interpretation

The Census 2024 Places Gazetteer provides representative map points:

| Place | Latitude | Longitude | Source |
|---|---:|---:|---|
| Indianapolis city (balance), IN | 39.776664 | -86.145935 | [Indiana gazetteer](https://www2.census.gov/geo/docs/maps-data/data/gazetteer/2024_Gazetteer/2024_gaz_place_18.txt) |
| Milwaukee, WI | 43.063348 | -87.966695 | [Wisconsin gazetteer](https://www2.census.gov/geo/docs/maps-data/data/gazetteer/2024_Gazetteer/2024_gaz_place_55.txt) |
| Jackson, MS | 32.315834 | -90.212850 | [Mississippi gazetteer](https://www2.census.gov/geo/docs/maps-data/data/gazetteer/2024_Gazetteer/2024_gaz_place_28.txt) |

These are modern representative coordinates, not exact historical office addresses
or 1960 municipal boundaries. The game-map route applies 69 miles/degree, mean
latitude longitude scaling, and 1.17 circuity. Newark's NYC catchment is an explicit
simulation access decision, not a claim that Newark residents lived in NYC.

## Validation record

Build and fixed suite: `SimLogs/scene-hq-repair-fixed-v1/runs.json`.
Matched 52-week off plus two treatment runs:
`SimLogs/scene-hq-repair-audit-v1/runs.json` and `repair-analysis.json`.
The treatment compares the first 52 weeks of the existing same-seed recruitment
candidate; it does not claim the full 208-week vacancy screen repaired.
The recruitment-off comparator uses the accepted 52-week city-placement control
and requires all 84 existing economic streams to remain byte-identical.

Focused probes require a fixed 31-city list, literal HQ retention, bounded routes
in both directions, no foreign/unknown-road fallback, region-aware Jackson
resolution, idempotent old-projection repair, no movement and preservation of
credible saved identity. Existing room, ownership, recruitment and save/load
checks remain required. Decade acceptance stays deferred.


Completed first-year treatment results, compared to the previous same-seed
recruitment candidate's first 52 weeks:

| Measure | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Affordable vacancy-slot weeks excluding accepted UK exceptions | 1,290 -> 405 (-68.60%) | 1,106 -> 188 (-83.00%) |
| Singles units | +0.67% | -0.30% |
| Album units | +1.60% | -0.18% |
| Market net | +0.63% | -0.96% |
| Final roster headcount | -1.31% | +0.72% |
| Defunct labels | 80 -> 79 | 73 -> 65 |

Both economic screens pass. Integrity maxima are zero. All affected opening HQs
resolve to actual place IDs. Seed 1001 Ace is 9/9 in Jackson and Savoy 6/6 in
Newark. The recruitment-off 52-week replay is byte-identical across all 84
economic streams. The complete nine-job fixed suite passes; both seeds have 56
recruitment and 27 dynamics assertions, plus 74 room checks and 26-week gzip IO.
The separate literal-HQ migration/identity probe passes 33 assertions, including
idempotence and preservation of credible saved locations. No new playable city
or room is added. This is 52-week evidence; the original 208-week pressure screen
is still outstanding.
