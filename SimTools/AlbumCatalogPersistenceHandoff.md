# Album catalog-persistence handoff

## Status

Investigation date: 2026-07-21.

The retained work-in-progress source fixes the 1962 Album upper-band failure,
passes the 1965 hard gate, and brings 1967-1968 Album units and economics inside
their inherited bands. It is **not globally accepted** because the first
remaining decade failure is now Single units in 1966. M5 was not launched.

Final candidate family:

```text
d6-album-bounded-mature-channel08-final-probes-1001-*
d6-album-bounded-mature-channel08-through-1968-1001-*
```

The 469-week process exited normally with
`CHART_AUDIT_COMPLETE ... weeks=469`. All market-clearing inventory,
allocation, reconciliation, and settlement deltas are zero.

The 2026-07-21 spillover-origin exclusion follow-up was run and **falsified**.
Its gameplay and telemetry edits were reverted after validation; the retained
source hashes at the end of this handoff are restored. The rejected run
artifacts remain under:

```text
d6-album-spillover-origin-probes-1001-*
d6-album-spillover-origin-disabled-52-1001-*
d6-album-spillover-origin-through-1968-1001-*
```

## Retained implementation

The implementation treats early realization and mature catalog persistence as
separate surfaces.

### Early shared market

Before the existing retail-maturity transition, aggregate Album title intent
continues to use the accepted overlap correction:

```text
effectiveAlbumIntent = A * regionalBaseCapacity
                     / (regionalBaseCapacity + 2 * A)
```

This leaves the accepted 1962 correction unchanged.

### Mature Album purchase channel

When `AlbumModel.GetRetailFulfillmentMaturity(year)` becomes established, the
Album format receives a bounded regional purchase channel rather than either
sharing the old Singles-only capacity forever or cloning the entire Singles
channel. Its share of regional base capacity is:

```text
0.08 + 0.35 * region.GetAlbumDemandEraProgress(year)
```

This is 8% in 1964, 12.375% in 1965, 21.125% in 1967, and 25.5% in 1968. It
contains no control lookup and does not react to live intent share.

### Catalog replenishment closure

Automatic uncharted Album backorder service is no longer perpetual. It remains
available while a title is younger than 156 weeks, or for 26 weeks after a
later chart appearance. After that boundary the title must sell through
existing shelf stock before ordinary retirement can close it.

The market-clearing stream now reports `baseCapacity`,
`albumChannelCapacity`, and `albumOverlapPressure` separately.

## Final annual adjudication

Ratios are candidate/control.

| Year | Single units | Album units | Album gross | Total units | Gross | Label net | Market net |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 0.9761 | 1.0057 | 1.0577 | 0.9765 | 0.9902 | 1.0150 | 1.0197 |
| 1961 | 0.9828 | 1.0628 | 1.0740 | 0.9851 | 0.9925 | 1.0183 | 1.0116 |
| 1962 | 0.9991 | 1.1270 | 1.1311 | 1.0038 | 1.0189 | 1.0292 | 1.0264 |
| 1963 | 1.0416 | 1.0615 | 1.0648 | 1.0425 | 1.0463 | 1.0583 | 1.0564 |
| 1964 | 0.9748 | 0.9838 | 0.9852 | 0.9752 | 0.9775 | 0.9812 | 0.9797 |
| 1965 | 0.9425 | 0.8199 | 0.8276 | 0.9347 | 0.9160 | 0.9268 | 0.9268 |
| 1966 | **1.1584** | 0.9265 | 0.9363 | 1.1255 | 1.0600 | 1.0662 | 1.0663 |
| 1967 | **1.1933** | 1.0108 | 1.0183 | **1.1554** | 1.0974 | 1.1217 | 1.1124 |
| 1968 | 1.0620 | 1.0886 | 1.0951 | 1.0683 | 1.0827 | 1.1163 | 1.0963 |

The inherited Single-unit envelope is `[0.85,1.15]`. The first required
failure is therefore 1966 Single units at `1.1584x`; 1967 Single units and
total units also exceed their annual ceilings. Do not relabel this M4 as a
pass, and do not launch M5.

Successful-release ratios remain within `[0.85,1.15]` through 1968. Annual
Album-release ratios are also inside `[0.80,1.20]`; the largest is 1966 at
`1.1833x`.

## What changed on the catalog surface

| 1968 metric | Old failed M4 | Current candidate | Control |
|---|---:|---:|---:|
| Album units ratio | 1.6237 | 1.0886 | 1.0000 |
| Album gross ratio | 1.3332 | 1.0951 | 1.0000 |
| Active Albums | 9,138 | 7,363 | 7,002 |
| Albums ever released | 15,782 | 15,760 | 15,624 |
| Albums retired | 6,644 | 8,397 | 8,622 |
| Median active age | 97 | 80 | 76 |
| P90 active age | 212 | 150 | 208 |
| 52+ Album gross share | 0.4319 | 0.4223 | 0.2055 |

The replenishment closure materially fixes surviving stock and the very-old
tail. It does **not** yet fix sales composition inside the bounded Album
channel: proportional allocation still lets 52+ titles consume 42.23% of
Album gross. The channel cap hides that composition problem at the aggregate
unit/economic level; it does not resolve it.

## Rejected experiments

1. A live Album-intent-share transition protecting recent titles was unstable.
   Moving its threshold from 5-6% to 5.2-6.2% raised 1962 Album units from
   `1.1989x` to `1.2486x` through economic feedback. It was fully reverted.
2. A 26-week automatic-restock window overcorrected: 1963 Album units fell to
   `0.7371x` and 1965 to `0.4943x`.
3. A 104-week window still failed 1964/1965 at `0.7584x` / `0.6221x` Album
   units.
4. A full mature Album channel cleared every serviceable Album intent and
   worsened 1968 Album units to `1.9373x`.
5. A bounded mature baseline of 4.5% solved late Albums but failed 1965 Album
   units (`0.7807x`) and economics. A 6% baseline missed by less than one point:
   Album units `0.7920x`, Album gross `0.7995x`, total gross `0.8989x`.
6. Excluding mature unused Album-channel capacity from regional spillover did
   not repair the late Single surface. It brought 1966 Single units into band,
   but failed the 1965 Album hard gate and made 1967-1968 Singles materially
   worse through downstream economic feedback. The candidate was reverted and
   must not be used as evidence for export/import share tuning.

These runs are retained as negative evidence. Do not return to an endogenous
live-share threshold or a full cloned Album channel.

## Spillover-origin negative evidence

The narrow candidate split regional slack into base and Album-channel origins,
excluded the mature Album-channel origin from `ExportBudget`, and appended
enabled-only origin telemetry. It did not change demand, acceptance, yield,
the Album channel formula, early overlap correction, or catalog replenishment.

Validation completed as specified:

- `dotnet build "Label Man.sln" --no-restore` passed with the inherited unused
  `ChartManager.OnGenreMomentumChanged` warning; `git diff --check` passed.
- Both D5 suites and temporary D6 probes 1-68 passed.
- The disabled 52-week replay completed normally. Its frozen
  `market-revenue.csv` and `release-capacity.csv` hashes were
  `06FF1BD3815C816718C380F921360DEBF75754A0F5B4A2CA24AF3B8023BCFE03`
  and `75516019E251C4EC76B6E90295B3A3F241E5AF12A7668F828D52507FBC86683C`,
  byte-identical to the retained disabled families.
- The single combined 469-week seed-1001 run completed normally with a
  header-only catastrophic stream. Inventory, allocation, reconciliation,
  settlement, edge, and origin-reconciliation deltas were all zero.
- No deterministic repeat was run because the annual gates failed.

Annual effects below compare the rejected candidate with the retained 8%
channel run. `Single change` is candidate units minus retained units. The old
Album-origin Single-import estimate is reconstructed from the retained run's
exact donor slack and edge-format output, proportionally attributing each
donor's transferred Singles by its unused capacity origins.

| Year | Retained Single ratio | Candidate Single ratio | Single change | Old Album-origin Single imports |
|---:|---:|---:|---:|---:|
| 1964 | 0.9748 | 0.9638 | -1,686,874 | 2,546,521 |
| 1965 | 0.9425 | 0.9034 | -6,439,192 | 4,603,123 |
| 1966 | 1.1584 | 1.1376 | -2,427,273 | 879,695 |
| 1967 | 1.1933 | **1.2272** | +3,540,007 | 70,111 |
| 1968 | 1.0620 | **1.1746** | +11,895,200 | 0 |

The hypothesis therefore fails its own falsification check. The direct
Album-origin import estimate is small relative to the 1966 movement and is
negligible or zero precisely where 1967-1968 Singles worsen. The rejected run
also produced 1965 Album units at `0.7946x`, 1967 total units at `1.1822x`, and
1968 total units at `1.1534x`. This is negative evidence against treating the
reported late Single ceiling breach as a mechanical spillover-capacity leak.

## Required next pass

### 1. Keep Album channel capacity fixed while fixing its age composition

The next Album change should be allocation-neutral in total. Split the mature
Album budget by deterministic release age, or apply a deterministic age weight
inside `AllocateProportionalLocal`, so recent/current titles receive displaced
capacity before 52+/156+ catalog. Preserve the exact regional Album budget and
do not use live aggregate intent share as a transition trigger.

Population-complete bridge output must show whether 52+ gross share moves
toward control without changing annual Album units materially. The current
candidate's 1968 Album total is already in the center of the accepted band.

### 2. Treat the Single failure as a separate surface

The first remaining M4 failure is 1966 Single units. Attribute it through
Single record count, raw/serviceable intent, regional capacity, and per-title
yield before modifying the Album baseline. The 4.5%, 6%, and 8% runs show that
the Single response is nonlinear and is not safely repaired by lowering the
Album channel again.

That attribution is now complete. The authorized systemic repair and validation
contract are in `SimTools/SingleVolumeLaneAndHitTailRepairHandoff.md`. It replaces
this paragraph as the operative Single directive. In particular, the next pass
must treat orphan and promo Singles as separate lanes, repair synthetic promo-track
construction and hidden promo memory, replace the fixed-portfolio opportunity
normalizer, and bound correlated discovery feedback. Do not substitute another
Album-channel or spillover experiment.

### 3. Validation

After a causal correction:

1. build, `git diff --check`, and exact-source D5/D6 probes;
2. disabled frozen replay, because it has not been rerun after this pass;
3. one 469-week seed-1001 run, with 1965 adjudicated as a hard gate inside the
   full run per owner direction;
4. deterministic repeat only after every annual gate passes;
5. do not launch M5 until the repeated M4 family passes all gates.

## Exact retained source

```text
47ACAAC316887560A59A1C626D0C9AC2BE231039BC31836A470ED38CA1B39B91  Systems/ChartManager.cs
4D1A6EDF8584232CA6E62CE9C720B797AD0C750940A777DB92D89364EDA14EAD  SimTools/ChartAuditRunner.cs
BBF409183668D3B0A382BBB19EB4B6FC912DEF74CAB14D948E0CC6D5F9B02FF9  SimTools/ArtistPopulationLifecycleProbeSuite.cs
677951736D08025939266D4E8BCF512D620A679EAAD6C699DB7B13FE5F355C8B  SimTools/analyze-album-cohort-clearing.mjs
```

The final exact-source build passed with only the inherited unused
`ChartManager.OnGenreMomentumChanged` warning. The final one-week harness
reported both D5 passes, D6 fixed probes 1-67 passed, and
`CHART_AUDIT_COMPLETE`.
