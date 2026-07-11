# Directive 4C-R - Seasonality Ensemble Adjudication and Final Validation

## 1. Disposition

Directive 4C is not rejected. Its current implementation is retained, but Checkpoint B is not accepted under the original validation language.

The terminal two-seed evidence shows that 4C preserves decade-scale market level, format balance, release volume, market net, chart life, deterministic replay, and the intended seasonal shape. The remaining failures are isolated to same-seed, single-calendar-year total-unit ratios after the enabled mechanic changes release timing and therefore downstream RNG topology.

This is the same measurement problem already adjudicated during Directive 4b: a treatment that legitimately changes event timing cannot be required to remain inside a narrow band in every same-seed/year cell when the decade and ensemble levels remain conserved. Directive 4C-R therefore replaces that defective gate with an ensemble-year gate while retaining strict per-seed decade controls and a catastrophic individual-year guard.

This directive authorizes validation resumption only. It does not authorize another seasonality design or calibration pass.

## 2. Frozen Candidate

Freeze the candidate exactly as represented by the final `4c-releaseonly-enabled-1001` and `4c-releaseonly-enabled-1002` runs:

- `EnabledSingleSalesLevel = 1.00`;
- `EnabledAlbumSalesLevel = 0.98`;
- the seven raw monthly tables remain unchanged;
- calendar/legacy normalization remains unchanged;
- Single and Album demand seams remain unchanged;
- regional radio-opportunity seams remain unchanged;
- recording-cost and comparable-prior seams remain unchanged;
- launch/drop marketing-efficiency seams remain unchanged;
- artist availability remains on `CompetitorManager.CalculateWeeklyReleaseChance`;
- artist availability does not modify `RosterManager`'s scouting probability;
- the venue getter remains public and unused;
- shipping `marketSeasonalityEnabled` remains `false` until final acceptance.

Do not change a curve entry, normalization formula, scalar, demand constant, radio constant, marketing formula, recording-cost formula, release-growth value, cooldown, roster rule, chart rule, retirement rule, Album curve, distance setting, or Baseline v2 value during this resumption.

No further calibration probe is authorized. If the frozen candidate cannot pass the revised gates, stop and report the failure.

## 3. Accepted Existing Evidence

The following completed runs remain valid and must not be repeated merely to obtain a more favorable result:

- disabled controls: `4c-disabled-1001`, `4c-disabled-1002`, and `4c-disabled-1003`;
- enabled determinism pair: `4c-enabled-1001-a` and `4c-enabled-1001-b`;
- final frozen-candidate measurement runs: `4c-releaseonly-enabled-1001` and `4c-releaseonly-enabled-1002`.

The earlier scalar-free, scouting-enabled, and `4c-probe98-*` runs remain part of the calibration history but are superseded for final candidate adjudication. Preserve them in the audit log; do not mix them into the final ensemble.

Record the already-verifiable exactness results:

- disabled seed-1001 `market-revenue.csv` must equal frozen Baseline v2 SHA-256 `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686`;
- disabled seed-1001 `release-capacity.csv` must equal frozen Baseline v2 SHA-256 `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D`;
- both enabled seed-1001 determinism streams must match byte-for-byte within each corresponding output.

Any failure to reproduce those facts from the existing artifacts is a hard stop. Do not replace the baseline hashes.

## 4. Reason for Rescope

The final release-only candidate produced:

| Seed | Decade total units | Decade Singles | Decade Albums | Decade market net | Successful releases | Album projects |
|---|---:|---:|---:|---:|---:|---:|
| 1001 enabled / disabled | 0.9972 | 0.9994 | 0.9811 | 0.9958 | 1.0012 | 0.9982 |
| 1002 enabled / disabled | 1.0128 | 1.0138 | 1.0052 | 1.0132 | 0.9965 | 0.9886 |

Closed Top-40 median is 11 weeks in both enabled and disabled runs for both seeds.

When seeds 1001 and 1002 are pooled by calendar year, total-unit ratios range from `0.9795` to `1.0391`, and the combined decade ratios are:

- total units `1.0051`;
- Singles `1.0067`;
- Albums `0.9931`.

The original same-seed/year failures have opposing signs. For seed 1002, 1962 is high while 1967 is low. A global Album scalar cannot repair one without worsening the other, and the two-seed pooled values show no corresponding annual level failure. This supports an RNG/timing-composition diagnosis rather than a missing global level correction.

The seasonal treatment is also observably active rather than being normalized away:

- Singles are lower in January and December and stronger in late spring/summer relative to disabled v2;
- Albums are lower in winter and stronger in Q4, with December approximately `1.08-1.09x` disabled across the two final seeds;
- summer mean radio play rises while December falls;
- release timing follows the retained availability curve;
- the first December with Album gross above Single gross is 1967 in both final seeds.

These observations justify completing validation. They do not themselves constitute acceptance.

## 5. Revised Checkpoint B

### 5.1 Required remaining run

Run exactly one additional measurement simulation using the frozen candidate:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=4c-releaseonly-enabled-1003 --seed=1003 --enable-market-seasonality
```

Use the existing `4c-disabled-1003` as its control. Do not rerun or replace the disabled control unless an artifact is corrupt or incomplete. If a rerun is technically unavoidable, document why and require its frozen-treatment outputs to match the existing disabled artifacts byte-for-byte before using it.

### 5.2 Per-seed decade hard gates

Apply these separately to seeds 1001, 1002, and 1003:

- decade total market units: enabled/disabled in `[0.97, 1.03]`;
- decade Single units: enabled/disabled in `[0.95, 1.05]`;
- decade Album units: enabled/disabled in `[0.95, 1.05]`;
- decade gross: enabled/disabled in `[0.95, 1.05]`;
- decade market net: enabled/disabled in `[0.95, 1.05]`;
- total successful releases: enabled/disabled in `[0.95, 1.05]`;
- scheduled Album projects: enabled/disabled in `[0.95, 1.05]`.

No seed may be rescued by pooling if one of these decade gates fails.

### 5.3 Three-seed pooled calendar-year hard gates

For each calendar year 1960 through 1969, sum the enabled value across seeds 1001-1003 and divide by the corresponding sum of the three disabled controls.

Require:

- pooled total market units: `[0.95, 1.05]` in every year;
- pooled gross: `[0.95, 1.05]` in every year;
- pooled market net: `[0.92, 1.08]` in every year;
- pooled successful releases: `[0.90, 1.10]` in every year.

This three-seed pooled test replaces the original requirement that every same-seed/year total-unit cell lie within `[0.95,1.05]`.

### 5.4 Individual seed-year catastrophic guard

Continue to report every enabled/disabled seed-year ratio. Individual seed-years are no longer ordinary +/-5% hard gates, but any of the following is a hard failure:

- total market units outside `[0.90, 1.10]`;
- gross outside `[0.90, 1.10]`;
- market net outside `[0.85, 1.15]`;
- successful releases outside `[0.85, 1.15]`;
- a repeated one-direction drift across all three seeds in the same year that the pooled calculation obscures through a reporting or weighting error.

Do not describe an individual +/-5% miss as irrelevant. Report its seed, year, magnitude, sign, and nearby-year reversal. The rescope changes its gating role, not its diagnostic value.

### 5.5 Inherited simulation-health gates

Retain all applicable non-volume protections from Directive 4C and the frozen Baseline v2:

- disabled byte exactness;
- enabled deterministic repeat;
- all-decade closed Top-40 median enabled-minus-disabled within `+/-2` weeks per seed;
- accepted Album crossover window and 1960 format-mix gates;
- accepted 4b chart, distance, concentration, home-market, and distribution-deal checks;
- no NaN, infinity, negative cost, invalid probability, or out-of-range awareness/radio state;
- venue-driven revenue, sales, awareness, and costs remain exactly zero;
- all fixed-input single-application assertions for sales, radio, production cost, and marketing remain valid.

If an inherited gate was not measured in the current 4C output, calculate it from the existing streams or add telemetry without changing simulation behavior or RNG, then rerun only the minimum frozen-candidate measurement needed to populate it.

### 5.6 Seasonal-signal adjudication

Report month-of-year enabled/disabled ratios pooled across the three measurement seeds. The following directions must remain visible:

- Single demand/sales: January below late spring/summer, with December below legacy v2;
- Album demand/sales: December above January and above disabled December;
- radio play: summer above winter, December below summer;
- recording cost per event: November above January;
- fixed-budget marketing awareness return: November above January;
- successful release timing: January above December under the retained availability seam.

Observed unit ratios need not equal raw multipliers. If any directional signal reverses after pooling all three seeds, stop and diagnose the relevant application seam; do not enlarge the curve.

The December/full-year Album-gross crossover remains report-only. Record the first December crossover, first full-year crossover, and lead/lag for each seed. Do not tune for a two-to-three-year lead.

## 6. Checkpoint-B Disposition

Checkpoint B passes only if all three per-seed decade gates, all ten pooled-year gates, all catastrophic guards, inherited health gates, and seasonal-direction checks pass.

If Checkpoint B passes:

1. freeze the candidate and all acceptance calculations;
2. update `SimTools/MarketSeasonalityAudit.md` with the complete evidence;
3. proceed to the one fresh holdout in section 7.

If it fails:

- do not alter the Album scalar;
- do not restore scouting seasonality;
- do not remove release seasonality;
- do not change monthly curves or baseline systems;
- do not consume a holdout seed;
- stop and report 4C as not accepted under the frozen candidate.

Any further design reduction or gate revision requires another directive.

## 7. Fresh Holdout

### 7.1 Seed selection

After Checkpoint B passes, select one seed confirmed unused by all prior calibration, validation, ensemble, and holdout work in the repository and available scratch artifacts. Record the search performed and why the seed is fresh before running it.

Run the disabled/enabled pair exactly once at the frozen candidate:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=4c-holdout-disabled-<seed> --seed=<seed> --disable-market-seasonality
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=4c-holdout-enabled-<seed> --seed=<seed> --enable-market-seasonality
```

Do not tune, widen a gate, or select another seed after inspecting the pair.

### 7.2 Holdout hard gates

Apply the per-seed decade gates from section 5.2, the individual-year catastrophic guards from section 5.4, the inherited simulation-health gates from section 5.5, and the seasonal-direction checks from section 5.6.

The three-seed pooled-year gate is a measurement-ensemble calibration gate and is not applied to a single holdout seed. Holdout annual +/-5% misses are report-only unless they breach the catastrophic guard.

A holdout failure is terminal for this directive. Preserve and report it; do not run a replacement.

## 8. Audit Repair and Final Report

Rewrite `SimTools/MarketSeasonalityAudit.md` as one chronological, internally consistent audit. The current statement that no runnable Godot executable was available is obsolete and must be removed or explicitly labeled as an earlier pre-run status.

The final audit must include:

1. final code-path map and frozen constants;
2. raw and effective normalized tables;
3. disabled seed-1001 frozen-hash verification;
4. enabled seed-1001 deterministic-repeat hashes;
5. complete calibration history: scalar-free results, Album `0.98` probe, failed original checkpoint, scouting-seam removal, and terminal original-gate failure;
6. the 4C-R rationale without erasing the original failure;
7. all three final per-seed decade comparisons;
8. all 30 individual seed-year rows and all ten three-seed pooled-year rows;
9. market-net, release-count, Album-project, chart-life, crossover, format-mix, and inherited 4b regression results;
10. pooled month-of-year seasonal-shape tables;
11. fixed-input single-application checks;
12. fresh-seed provenance and the one-shot holdout results;
13. exact commands, output names, final hashes, shipping toggle state, and limitations.

Do not say the original Directive 4C Checkpoint B passed. Say that it failed its original same-seed/year gate, 4C-R prospectively replaced that gate based on already-observed ensemble evidence and the established 4b precedent, and the frozen candidate then passed or failed the revised validation.

## 9. Baseline and Shipping State

Until the holdout passes:

- keep `marketSeasonalityEnabled = false`;
- do not append a 4C acceptance section to `BASELINE-V2.md`;
- do not replace any frozen 4b hash or metric;
- preserve all existing audit artifacts.

After, and only after, every Checkpoint B and holdout hard gate passes:

1. set the shipping scene/default to `marketSeasonalityEnabled = true`;
2. run one final smoke test confirming the no-flag path is enabled and `--disable-market-seasonality` remains exact-off;
3. append a clearly labeled Directive 4C/4C-R acceptance section to `BASELINE-V2.md` while preserving the frozen 4b history;
4. record the accepted constants, holdout seed, new enabled anchors, and legacy-disabled hashes.

## 10. Completion Condition

4C-R is complete when the frozen release-only candidate has passed three per-seed decade comparisons, the three-seed pooled calendar-year gates, catastrophic individual-year guards, inherited health checks, visible seasonal-direction checks, and one fresh one-shot holdout; the audit truthfully preserves the original 4C failure and the prospective rescope; disabled v2 remains byte-exact; and the shipping toggle is enabled only after all acceptance evidence is complete.
