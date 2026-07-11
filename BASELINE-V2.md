# Baseline v2 — Directive 4b

Status: **Frozen 2026-07-10. Stage 1 closed; Stage 2 calibration checkpoint passed; and the fresh Stage 3b holdout passed.**

Directive 4b is strictly staged. The seven-region taxonomy is implemented and loads. Six-v1 versus six-v2 ensemble evidence accepted Stage 1 conservation without parameter calibration; see `SimTools/Stage1ClosureAudit.md`. Distance passed its three-seed calibration checkpoint. The original one-shot seed-2004 Stage 3 holdout remains a historical fail under its then-binding +/-1-week paired chart-lifetime guard; the separately authorized fresh seed-2005 Stage 3b holdout passed under the now-binding +/-2-week guard. The enabled seed-1001 decade anchor below is frozen; see `SimTools/Stage2CalibrationAudit.md` and `SimTools/Stage3bHoldoutAudit.md`.

## Task 0 — 4a finance-path disposition

The frozen parent commit (`550793c`) was rebuilt with Godot 4.7 Mono and rerun for 52 weeks at seed 1001 with albums disabled. Its regenerated `market-revenue.csv` has the exact frozen SHA-256:

`765897841BCB1C62225EF2A7861CF46ACC282FE206A10BADCF895330612A3866`

The 4a disabled file has SHA-256:

`12B2D0EED2EAD1B788DE65006F87788CE6CCE268693071ED0EC9BA46615BF2EC`

Task 5b's account is confirmed, with one clarification. The primary approved source of drift is the change from one aggregate `weeklyUnits * pressingCost` operation to a sum of per-region float multiply-adds. The refactor also regrouped the equivalent net expression from `weeklyUnits * grossPerUnit - skimAmount - artistPayment` to `grossAfterCogs - skimAmount - artistPayment`. Float addition/multiplication is not associative, so those equivalent forms are not byte-transparent.

The two files have identical 636-row keys. Numeric comparison:

| Column | Cells | Different | Max absolute delta | Max relative delta | Positive / negative |
|---|---:|---:|---:|---:|---:|
| `totalMarketUnits` | 636 | 0 | 0 | 0 | 0 / 0 |
| `gross` | 636 | 0 | 0 | 0 | 0 / 0 |
| `labelNet` | 636 | 632 | 0.153624 | 8.3052e-8 | 326 / 306 |
| `distributionIncome` | 636 | 44 | 0.000168 | 1.4878e-7 | 12 / 32 |
| `marketNet` | 636 | 632 | 0.153791 | 8.3052e-8 | 326 / 306 |

Annual 1960 Single units and gross are identical (`154,810,982` and `136,105,309.658338`). Annual Single net changes from `75,235,207.373569` to `75,235,207.317945`, a `-$0.055624` delta (about `7.39e-10` relative). The signs of weekly deltas are mixed rather than systematically biased. `release-capacity.csv` remains byte-identical at SHA-256 `8B84BBFDBA5F8B38729F29A1FDF14F8F7598A7AF18DCBAD053BC6C8238A9A461`. Task 0 therefore passes as float-noise-only with zero material economic drift.

## Stage 1 — seven-region taxonomy

### Static conservation

| Field | Old Midwest target | Great Lakes | Great Plains | Implemented combined/weighted value |
|---|---:|---:|---:|---:|
| Population (M) | 51.600 | 36.100 | 15.500 | 51.600 |
| Urbanization | 0.700 | 0.760 | 0.560 | 0.699922 |
| Income | 1.050 | 1.080 | 0.980 | 1.049961 |
| Youth | 0.350 | 0.350 | 0.350 | 0.350000 |
| Black population | 0.080 | 0.110 | 0.010 | 0.079961 |
| Colleges | 420 | 260 | 160 | 420 |
| Integration | 0.400 | 0.420 | 0.350 | 0.398973 |
| Progressivism | 0.450 | 0.450 | 0.450 | 0.450000 |
| Insularity | 0.450 | 0.400 | 0.570 | 0.451066 |
| Adoption | 1.200 | 1.300 | 0.970 | 1.200872 |

All 33 population-weighted genre acceptances are within the required ±0.05 of old Midwest. The differentiated rows are:

| Genre | Old | Great Lakes | Great Plains | Weighted | Delta |
|---|---:|---:|---:|---:|---:|
| Rock & Roll | 0.550 | 0.550 | 0.680 | 0.589050 | +0.039050 |
| Doo Wop | 0.500 | 0.620 | 0.380 | 0.547907 | +0.047907 |
| R&B | 0.550 | 0.700 | 0.350 | 0.594864 | +0.044864 |
| Soul | 0.150 | 0.220 | 0.100 | 0.183953 | +0.033953 |
| Country | 0.550 | 0.400 | 0.734 | 0.500329 | −0.049671 |

Infrastructure count conservation is exact for radio stations (1270), jukeboxes (144,000), concert venues (720), studios (150), local labels (190), clubs (3120), record stores (512), and department stores (1020). Theaters total 5250 versus 5253 old (−3).

### Taxonomy and sanity checks

- The scene loads seven live regions: East Coast, Great Lakes, Great Plains, Deep South, Southwest, Rockies, and West Coast.
- Great Lakes resolves to Chicago and Great Plains resolves to Minneapolis. Runtime assertions reject any non-East-Coast live region that silently falls back to New York.
- The full symmetric ten-edge breakout graph and both curated adjacency rows match Directive 4b exactly.
- The Plains, Southwest, and Rockies pools include the resolved cities, including Minneapolis and Billings. Procedural aliases resolve to the directed nearest roster cities.
- `parentRegionId` is live; `futureRegionId` is retired while its CSV column remains blank for header compatibility.
- No quoted `"midwest"` identifier remains in live C#, data, the chart scene, or the audit runner. The artist-profile region picker also uses the seven-region taxonomy; its seven entries preserve the prior one-draw RNG shape.

Observed generated-label home regions in the final two-seed probe:

| Seed | East Coast | Great Lakes | Great Plains | Deep South | Southwest | Rockies | West Coast |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1001 | 205 | 102 | 18 | 129 | 61 | 19 | 66 |
| 1002 | 226 | 101 | 14 | 98 | 64 | 18 | 79 |

The configured generic-city bands are Northeast 30%, Great Lakes 10%, Great Plains 10%, South 20%, Rockies 10%, and West Coast 20%. The observed full-label distribution also includes archetype-specific and historical-city rules, which is why it does not equal those generic bands.

Great Lakes versus Great Plains charted-unit texture is visible in the final probes. For seed 1001, R&B charted units split 76.7%/23.3% and Soul 76.1%/23.9%; for seed 1002, R&B split 80.3%/19.7% and Soul 75.8%/24.2%. These exceed the regions' 70.0%/30.0% population split in the intended Great Lakes direction.

### Superseded same-seed gate record

The initial authored table was run for all three disabled seeds. Eight of 30 same-seed year cells failed the ±3% gate; the worst was seed 1002 in 1968 at −5.610%. It also exposed two out-of-spec static genre rows (Doo Wop and Country), which were corrected.

The permitted two-seed probe of the corrected, fully compliant table still fails eight of 20 cells:

| Seed | Year | Delta vs same-seed v1 |
|---:|---:|---:|
| 1001 | 1960 | −3.128% |
| 1001 | 1961 | −4.647% |
| 1001 | 1964 | −3.502% |
| 1001 | 1967 | −3.234% |
| 1002 | 1960 | −4.106% |
| 1002 | 1961 | −5.305% |
| 1002 | 1962 | −3.876% |
| 1002 | 1968 | −3.804% |

The failure is not a uniform level error: the initial table put 1965 at +4.489% for seed 1001 and −3.419% for seed 1002. Correcting the genre rows moved individual years in both directions. This is consistent with the directive's expected seventh-region RNG-topology divergence, but it does not satisfy the stated per-seed/per-year hard band. Forcing it would require further taxonomy overfitting or changes to chart/RNG mechanics, both outside the approved guardrails.

This historical same-seed result is superseded for gate purposes by the six-v1/six-v2 closure audit in `SimTools/Stage1ClosureAudit.md`. The enabled three-seed regression and seed-1001 determinism repeat both pass; Stage 1 is closed.

## Stage 2 — calibrated checkpoint

`distanceModelEnabled = true` in `chart_manager.tscn`. The checkpoint configuration is `reachHalfDistance = 10000` and `costPerDistance = 0.0001`; `difficultyWeight` remains frozen at `0.35`. The three disabled calibration seeds keep every national annual-unit delta within +/-5%, preserve the required tiered home-market concentration, and keep distribution deals active. The enabled regression and its seed-1001 repeat pass. Full evidence is in `SimTools/Stage2CalibrationAudit.md`.

## Stage 3 — historical holdout and recalibrated fresh validation

The terminal seed-2004 pair was run exactly once. Its headline mix, timing, and Pearson checks pass, but its closed Top-40 median is 11 weeks enabled versus 9 disabled (+2 weeks); 1964 also reaches +2. This violated the then-binding +/-1 paired-median guard. Per the no-post-holdout-tuning rule, no calibration was attempted and that holdout remains consumed as a historical fail.

Alice subsequently approved a prospective widening of the paired closed Top-40 median guard to +/-2 weeks. Seed 2003 was unavailable because it was already used in the earlier `DecadeRunValidationAudit.md` holdout history, so confirmed-fresh seed 2005 was used instead. Its disabled/enabled pair ran once at the frozen Stage 2 configuration and passed every inherited guard: its all-decade closed Top-40 medians are 11 weeks disabled and 11 weeks enabled (delta 0). See `SimTools/Stage3bHoldoutAudit.md` for the full result. The historical v1 figure `154,810,982` remains historical rather than a v2 target.

## Frozen Baseline v2 anchor

Frozen 2026-07-10 from an enabled 520-week seed-1001 run using the shipping `chart_manager.tscn` configuration: `distanceModelEnabled = true`, `reachHalfDistance = 10000`, `costPerDistance = 0.0001`, and `difficultyWeight = 0.35`. The same command was repeated in an independent process; all emitted streams were byte-identical. The SHA-256 anchors are:

| Stream | SHA-256 |
|---|---|
| `market-revenue.csv` | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` |
| `release-capacity.csv` | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` |
| `label-geography.csv` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` |
| `geography-metrics.csv` | `AF49EAC9E2843AC1E5B0917E1F23E68340D7C5938F51762DDFFD170CDAA01E4D` |

## Baseline v1 retirement

Alice retired Baseline v1 on 2026-07-10. Its artifacts and figures remain preserved as historical evidence, including the historical 1960 figure `154,810,982`, but v1 is no longer an active parallel reference or a target for future comparisons. Baseline v2 is the sole active frozen reference.

## Directive 4C / 4C-R acceptance

Accepted 2026-07-10. Directive 4C's original same-seed/year +/-5% unit gate
failed under the frozen release-timing treatment; Directive 4C-R prospectively
replaced that defective cell gate with a three-seed pooled calendar-year gate
while retaining per-seed decade and catastrophic individual-year protections.
The frozen candidate uses the published raw tables, calendar/legacy
normalization, `EnabledSingleSalesLevel = 1.00`, `EnabledAlbumSalesLevel =
0.98`, release-only artist availability, and no venue consumer. Disabled
seed-1001 remains byte-exact to the frozen 4b anchors above; enabled seed-1001
repeated byte-identically. Three final measurement seeds (1001-1003) and fresh
holdout seed 2006 passed their applicable gates. `marketSeasonalityEnabled` now
ships enabled; `--disable-market-seasonality` remains the exact-off control.
Full evidence, commands, calibration history, and output hashes are in
`SimTools/MarketSeasonalityAudit.md`.
