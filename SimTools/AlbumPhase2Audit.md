# Album entity, pooling, demand, and chart audit

Measured 2026-07-03 on Godot 4.7 Mono. Validation used 52-week runs with seeds 1001, 1002, and 1003 against `RevenueReleaseFormatSubstitutionAudit`.

## Phase 0 findings

- Singles flow from static `Record` identity into `RecordRuntimeData`, then through national update, regional demand/stock fulfillment, weekly finalization, restocking/discovery, sales-plus-airplay scoring, inertia, and Top-100 assignment. Album regional inventory and finance can share this state, but album awareness, demand, age decay, and ranking require a separate path because the single simulator is radio-led and decays sharply after week eight.
- Single retirement has four independent constants: a five-week under-floor and 18-week maximum-age pair for never-charted records, an eight-week relevance horizon for charted records, and the universal 50-unit gate. A monthly radio-decay check is an additional charted-record exit. Because `weeksOnChart` never resets, any record that charts stays on the charted path. Albums now branch before every single retirement test.
- `releaseHistory` IDs alone were unsafe because retirement removed the only live lookup. Albums use lightweight immutable track snapshots and a retired-single archive. A dedicated single-ID history prevents album IDs from being mistaken for missing singles.
- The album chart is new construction. It borrows ordering and position bookkeeping patterns, but has its own pool, bubbling-under state, sales-led score, and historical capacity curve. No time-varying chart mechanism existed previously.
- `Album` mirrors the static `Record` resource role. `AlbumRuntimeData` is serializable runtime-only state, matching the existing static/runtime split.

## Implementation

- Added `Album`, `AlbumTrack`, and `AlbumRuntimeData`, plus `AlbumFormat` and `ReleaseFormat.Album`/`EP`.
- Added tunable 1960-1968 era weighting, peak/whole-work appeal pooling, thematic-cohesion ceilings, pre-1965 statement-album fluke gating, packaging, stereo flavor, runtime, and generated non-single tracks.
- Added demographic album affinity, youth/income price willingness, and parallel regional album market sizing.
- Added a slower album awareness/sales path with a 26-week demand-decay threshold and long catalog tail.
- Added a separate combined mono/stereo album chart sized 40 before April 1961, 50 before August 1963, 150 before May 1967, and 200 thereafter.
- Album catalog retirement is independently exported: 10-unit floor, 26-week never-charted tolerance, and 52-week charted tolerance. Albums bypass all four single constants and the radio check.
- Converted pressing cost to a format-keyed exported dictionary: Single $0.30, Album $0.95, EP $0.55. Album packaging adds exported per-unit and fixed costs. Existing gross/COGS/skim/royalty/net routing is reused.
- The release fork uses a deliberately simple genre/era affinity roll. An album consumes the same artist slot and cooldown reset as a single.
- Added `album-chart.csv`, `album-composition.csv`, `format-mix.csv`, and `retired-track-availability.csv`. Existing singles record/chart audit streams remain singles-only.

## 1960 validation

| Seed | Album release share | Single units | Album units | Combined units | Album gross | Adult share of album-chart rows |
|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 22.9% | 132,130,753 | 2,091,825 | 134,222,578 | $7,991,040 | 97.9% |
| 1002 | 22.4% | 141,623,022 | 2,142,919 | 143,765,941 | $8,174,578 | 97.2% |
| 1003 | 23.1% | 146,088,750 | 2,368,490 | 148,457,240 | $9,037,935 | 97.0% |

Across the seeds, albums average 10.96-11.01 tracks and thematic cohesion is 0.08. Concept and high-cohesion albums are both zero. Every youth-genre album generated in 1960 is a compilation. Adult catalogs also contain Standard, Live, and Soundtrack albums, so the chart dominance is not produced by a compilation-only rule.

Compilation assembly made 402-459 single-resolution attempts per seed. The archive supplied 231-247 retired singles and there were zero unarchived misses, confirming that archival is load-bearing rather than theoretical.

## Singles guards and substitution

The album-disabled seed-1001 run exactly reproduces the accepted annual 154,810,982 units. Its published baseline checksum anchors are also exact:

- `market-revenue.csv`: `765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866`
- `release-capacity.csv`: `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461`

With albums enabled, the singles chart retains 19.31-20.06 entries/exits per week, closed Top-40 median life of 11-11.5 weeks, quality/outcome Pearson of 0.535-0.595, zero week-52 charted zombies, and nonzero age-14 Independent/Boutique charting in every seed. Single units fall by 10.8-14.6% because 22-23% of successful releases consume their slot as albums; combined units are 9.5-13.3% below the prior single-only totals. This is reported substitution, not tuned away.

Two independent seed-1001 processes were byte-identical across all 16 emitted CSVs. `dotnet build "Label Man.sln" --no-restore` succeeds with no errors and the pre-existing unused-event warning only. Headless runs also continue to print the pre-existing `MissingSingletonsTemp.cs` autoload warning after successful audit completion.
