# Keyed regional-sales RNG pilot — 2026-10-18

Opt-in only. `--regional-sales-rng=keyed-v1` replaces the two weekly regional sales
jitter draws (`ChartSimulator` ±4%, `AlbumSimulator` ±3%) with a Philox4x32-10 value
keyed on world seed, region ID, record ID, chart week and single/album. Those draws no
longer consume the global `GD` stream. Everything else, including prewarm (chart week 0),
keeps its existing stream. Without the flag, or with `=legacy`, behavior is unchanged.

## Save boundary

- `WorldSaveData` carries `RegionalSalesRngVersion`/`Seed`, omitted when zero, so legacy
  saves keep the exact v21 JSON. Keyed worlds write save version 22, so an older
  executable refuses them instead of silently changing their dice.
- A keyed save reloads keyed with its saved seed. A command-line choice overrides it.
  Unknown versions and conflicting flags are rejected.

## Validation

| Check | Result |
| --- | --- |
| Philox reference vectors, order/insertion invariance, uniformity smoke (`SimTools/CounterRngTests`) | 251,034 checks pass |
| Legacy stream, save metadata, version rejection (`--regional-sales-rng-check`) | 11 checks pass |
| Legacy mode vs room/material build, LR 1001 | 82/82 CSVs + world identical |
| Keyed reproducibility, LR 1001, three runs | 82/82 CSVs + world identical |
| Runtime, sequential alternating LR 1001 | keyed 36.0 / 34.5 s vs legacy 39.3 / 34.1 s: neutral |

Keyed vs legacy aggregates, 26-week LR, four seeds:

| Seed | Single units | Album units | Active albums |
| --- | ---: | ---: | ---: |
| 1001 | +0.1% | −5.8% | +2.8% |
| 2002 | −0.4% | −5.3% | −8.0% |
| 3003 | +0.2% | +2.9% | +4.3% |
| 4004 | −0.1% | +0.6% | +1.5% |

Legacy album units alone span 10.2M–12.4M across these seeds. Mean −1.9% with mixed
signs is within noise. Keyed mode is effectively a different seed, as expected.

## What the pilot cannot show yet

The A/B variance benefit is unmeasured, and this pilot is unlikely to show much of it:

- Record IDs are creation counters (`gen_N`; artists `artist_NNNNN`). Once two arms
  create records in a different order, the same ID names different records, so the
  keyed noise stops lining up.
- Sales draws are unconditional (one per record × region × week). Most global-stream
  reshuffling comes from conditional draws, of which there are about 307 `GD.Rand*`
  sites, mostly in `CompetitorManager`, `AILabelFactory` and `RosterManager`.

Before expanding: key on causal identity, or key the conditional decision sites. Then
measure |A−B| on a matched perturbation in legacy vs keyed mode.
