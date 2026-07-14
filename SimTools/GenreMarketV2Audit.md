# Genre Market V2 Audit

## Current implementation checkpoint

**Phase 0/1 closure status: fail / hard stop.** This is a living phase record, not an acceptance report.

### Phase 0 boundary

- `genreMarketV2Enabled` defaults to `false` in `ChartManager`.
- `--enable-genre-market-v2` and `--disable-genre-market-v2` resolve before label generation, artist generation, or prewarm.
- Passing both flags throws an argument error.
- The enabled catalog is validated at configuration time; disabled runs do not consume catalog logic or RNG.
- Prewarm explicitly retains the legacy acceptance path. The catalog becomes active at the first live weekly tick only.

### Closure evidence (2026-07-11)

| Gate | Result | Evidence |
|---|---|---|
| Current solution build | Inconclusive | `dotnet build "Label Man.sln" --no-restore` compiled this work successfully before the final audit edit. A later repeat could not load `C:\Users\grohl\AppData\Roaming\NuGet\NuGet.Config` and therefore could not resolve `Godot.NET.Sdk/4.7.0`; this is an environment-access failure, not a reported C# compile error. |
| Disabled 520-week seed-1001 frozen-stream hashes | **Fail — hard stop** | Actual headless run completed. Only `label-geography.csv` matches; see the hash table below. No replacement hashes were accepted. |
| Phase-0 enabled no-op double-run | Not run — fail | Requires the same unavailable headless runner. No temporary bypass was committed. |
| Current Phase-1 enabled seed-1001 double-run | Not run — fail | Requires the same unavailable headless runner. |
| Required telemetry output | Implemented, not run | `ChartAuditRunner` creates five new separate streams under `SimLogs` using the run prefix. The catalog stream has Phase-1 rows; market/events/explanation/special-product streams have validated headers only until their authorized phases. |

The executed command was:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-phase0-disabled-1001 --seed=1001 --disable-genre-market-v2 --aggregate-only
```

Output directory: `C:\Project\Label-Man\SimLogs`. The Godot executable used was `C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe`.

| Stream | Expected SHA-256 (`BASELINE-V2.md`) | Actual SHA-256 | Result |
|---|---|---|---|
| `market-revenue.csv` | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` | `AFDB0F32EC8FE81E5F13871A50FE2CB49E8AF12CF7FA8C04FCE58C9836BFF7A1` | **Fail** |
| `release-capacity.csv` | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` | `9956B0BAC3C20A8C15F66FE7BF00C7FC991583BBA014D9299ED9C0848FC3FB6D` | **Fail** |
| `label-geography.csv` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | Pass |
| `geography-metrics.csv` | `AF49EAC9E2843AC1E5B0917E1F23E68340D7C5938F51762DDFFD170CDAA01E4D` | `74D05F65ABFDD9069741866DA5E5EB08A5F9E24FA6666CC838E88B7BDB194EE7` | **Fail** |

Per Directive 5 §0, the mismatch is a hard stop. Enabled treatments, no-op repeats, and Phase 1 checkpoint tests have not been run, and the actual values above are not new baseline candidates.

### Enum-domain repair attempt

Diagnosis: expanding `Genre` changed the disabled denominator in `CompetitorManager.CalculateSingleGenreMarketFactor`; direct enum loops also existed in `Zeitgeist`, `ChartManager` momentum setup/decay/clamping, and `LabelGenerator` random selection. `GenreDomains` now supplies the explicit original ordered values `0..32` in disabled mode and the 42 catalog profiles only when enabled. An initial repair run exposed and then corrected one remaining momentum-clamping enum loop.

The rebuilt authoritative rerun used the same command with `--run=d5-phase0-repair3-disabled-1001` (without `--aggregate-only`). Its hashes remain mismatched:

| Stream | Expected SHA-256 | Repair-rerun SHA-256 | Result |
|---|---|---|---|
| `market-revenue.csv` | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` | `14000CE476EF808D2B53B486055E594D09D0AFA48456C5908F301A6A5E78545A` | **Fail** |
| `release-capacity.csv` | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` | `A5A329A883DC88B820715D68D40E2B69369E851C156F111FA399799A292185D8` | **Fail** |
| `label-geography.csv` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | Pass |
| `geography-metrics.csv` | `AF49EAC9E2843AC1E5B0917E1F23E68340D7C5938F51762DDFFD170CDAA01E4D` | `664030607F5B5469ACA3970929A649185CCF26989EECAD8D1C54A96CFCCA1118` | **Fail** |

The frozen seed-1001 CSV artifacts are not present in `SimLogs`, so a row/column diff against those exact files cannot yet be produced from this worktree. This remains a hard stop; no enabled mode was run.

### Control reclassification and accepted disabled boundary

The prior repair-rerun comparison used the wrong control. `d5-phase0-repair3-disabled-1001` disabled Directive 5 only; market seasonality therefore remained in its accepted shipping state. Its correct control is `4c-releaseonly-enabled-1001`, not `4c-disabled-1001`.

Direct `fc /b` comparison reported no differences for all four streams, and their complete SHA-256 values match:

| Stream | 4C shipping-control SHA-256 | Directive-5-disabled SHA-256 | Byte comparison | Result |
|---|---|---|---|---|
| `market-revenue.csv` | `14000CE476EF808D2B53B486055E594D09D0AFA48456C5908F301A6A5E78545A` | `14000CE476EF808D2B53B486055E594D09D0AFA48456C5908F301A6A5E78545A` | identical | Pass |
| `release-capacity.csv` | `A5A329A883DC88B820715D68D40E2B69369E851C156F111FA399799A292185D8` | `A5A329A883DC88B820715D68D40E2B69369E851C156F111FA399799A292185D8` | identical | Pass |
| `label-geography.csv` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | identical | Pass |
| `geography-metrics.csv` | `664030607F5B5469ACA3970929A649185CCF26989EECAD8D1C54A96CFCCA1118` | `664030607F5B5469ACA3970929A649185CCF26989EECAD8D1C54A96CFCCA1118` | identical | Pass |

The first pre-repair run remains a genuine enum-domain leak. The intervening repair-rerun comparison against `4c-disabled-1001` was a **mis-specified control**, not a shipping-control failure.

A second 520-week run used both `--disable-genre-market-v2` and `--disable-market-seasonality` with run name `d5-phase0-dual-disabled-1001`. Direct `fc /b` comparisons against `4c-disabled-1001` reported no differences in all four streams, and the hashes match the frozen Baseline v2 anchors:

| Stream | Baseline v2 / 4C-disabled SHA-256 | Dual-disabled SHA-256 | Byte comparison | Result |
|---|---|---|---|---|
| `market-revenue.csv` | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` | `7FBB45A28AEF4C9BB5BAD61ACF0D821718916C249AE911BB68BF54467FDDC686` | identical | Pass |
| `release-capacity.csv` | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` | `14B4931B5F83A4D01D86ED447E8F8DC1CA3D39DAD10CBFD83DE009AA216D7C8D` | identical | Pass |
| `label-geography.csv` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | `AFD9D12F0C14C46553CFC87441120E9C3B3E7BE3808B55766870DA866F98CB32` | identical | Pass |
| `geography-metrics.csv` | `AF49EAC9E2843AC1E5B0917E1F23E68340D7C5938F51762DDFFD170CDAA01E4D` | `AF49EAC9E2843AC1E5B0917E1F23E68340D7C5938F51762DDFFD170CDAA01E4D` | identical | Pass |

**Directive 5's disabled exact-off boundary is accepted.** No enabled or Phase 2 work was performed in this verification pass.

### Remaining Phase 0/1 determinism

| Gate | Commands / outputs | Result |
|---|---|---|
| Phase 0 enabled no-op boundary | Isolated disposable copy with catalog validation retained and `Enabled` forced false only in that temporary build; `d5-phase0-noop-a-1001` and `d5-phase0-noop-b-1001`, each `--weeks=520 --seed=1001 --enable-genre-market-v2` | **Pass:** both emitted 40 CSV files; every matching stream has the same SHA-256. The bypass is not present in this worktree and was not committed. |
| Current Phase 1 enabled determinism | `d5-phase1-enabled-a-1001` and `d5-phase1-enabled-b-1001`, each `--weeks=520 --seed=1001 --enable-genre-market-v2` | **Pass:** both emitted the same 40 filenames. SHA-256 comparisons and `fc /b` found no differences in any emitted stream, including Directive 5 telemetry streams. |

### Fixed-input and migration probes

Command: `Godot_v4.7-stable_mono_win64.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase1-probes-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes`.

`GenreMarketV2ProbeSuite` completed without an audit exception. It validates all 42 unique stable IDs and profiles; all seven authored keyframe positions; midpoint interpolation; pre/post-decade clamping; representative pre-emergent, emerging, and legacy lifecycle states; deterministic formatter coverage; and every retired identity migration. Migration probes cover Girl Group's Soul/R&B versus non-Soul/R&B secondary rule, Ska/Rocksteady/Reggae 1965/1966/1968 boundaries and missing date, stable IDs/schema version, tag insertion/sorting/deduplication, and idempotence.

| Remaining Phase 0/1 gate | Result |
|---|---|
| Fixed catalog/curve/lifecycle probe suite | Pass |
| Migration round-trip/idempotence probes | Pass |
| Tag-free 4C seasonality and legacy momentum preservation | Pass by the accepted dual-disabled and 4C-shipping byte-identical controls; Directive 5 does not add a tag consumer or replace the legacy accumulator in Phase 1. |
| Consumer integration | Pass: live-tick normalization resolves labels, artists, records, and Album tracks through `GenreCatalog.MapLegacy`; `Record` persists canonical IDs/tags/schema; formatter resolves catalog IDs; telemetry emits the catalog and Phase-appropriate empty event streams. Retired aliases are mapped before live enabled decisions and never emitted as canonical record IDs. Both `ChartManager.GetEffectiveGenreAcceptance` and `MarketRegion.GetGenreAcceptance` read catalog baselines on the enabled live path. |
| Segment declaration | Pass with limitation: the seven normalized channel weights remain explicitly marked Phase-1 placeholders, not Phase-2 segment completion. |

**Phase 0/1 closure: pass.** No Phase 2 system has been implemented or activated.

### Phase 1 foundations

- Legacy `Genre` ordinals `0` through `32` are explicit and unchanged.
- Canonical Directive 5 genres have stable lowercase IDs in `GenreCatalog`.
- The catalog contains all 42 required profiles with the authored 1960/62/64/66/67/68/69 baseline values, lifecycle years, family, audience lean, orientation, and normalized placeholder segment weights.
- `GenreMigration.Canonicalize` is idempotent and maps the retired identities, including deterministic Ska/Rocksteady/Reggae resolution from release year.
- `Record` persists stable genre/tag IDs and a schema version. No player-facing UI is introduced.
- At the first live tick only, generated labels, artists, records, and Album tracks resolve legacy enum values to canonical identities. Legacy prewarm remains unchanged.
- National and regional live acceptance now both source the complete catalog baseline. The existing momentum accumulator is retained unchanged.
- Existing genre formatting resolves canonical IDs; no new player genre-selection UI is added.

### Phase 2 integration batch (2026-07-11)

- The catalog's authored five-channel priors now materialize into all eleven normalized routing segments, including the required Country, Gospel, Latin, Jazz/Classical, and Childrens specialist floors.
- `GenreAcceptanceService` is the enabled live owner for regional Single and Album demand and ordinary regional radio opportunity. It preserves the existing global legacy momentum accumulator as an input and retains the legacy prewarm/disabled path.
- Segment routing is capacity-centered: the seven-region buying population is partitioned once by `SegmentCapacityModel`, and the weighted segment result preserves a neutral genre baseline rather than summing overlapping audiences.
- The centered `singleOrientation` multiplier is shared by realized Single/Album demand and the corresponding AI format priors. It reallocates equal-format combined opportunity exactly.
- Fixed-input enabled probe command: `Godot_v4.7-stable_mono_win64.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-probes-final --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only`.
- Result: pass (exit code 0). The runner emitted no `CHART_AUDIT_FAILED`; its known pre-existing `MissingSingletonsTemp.cs` autoload diagnostic remained non-fatal.

### Not yet accepted

Phase 2 has its focused fixed-input integration evidence above, but full Phase-2 economic/regression and historical-arc gates remain unaccepted. Phase 3 momentum, Phase 4 tags, Phase 5 special products, and the broader seed audits remain deferred.

### Phase 2 economic repair (2026-07-12)

The completed enabled seed-1001 checkpoint is rejected. Its individual annual unit and market-net ratios against the accepted 4C shipping control were outside the required `[0.90, 1.10]` range from 1960 onward; no Phase 3 work is authorized.

The following integration repairs are implemented but not yet accepted:

- Enabled record-wide radio heat now receives the buying-population-weighted national value of the same resolved regional demand routes. The prior enabled-path `1.0` input removed historical acceptance damping from weekly radio evolution.
- The regional radio-opportunity curve now uses the existing `.60 + .50 * acceptance` demand-scale convention, bounded to `[.35, 1.10]`, instead of the inflating `.55 + .65 * acceptance` curve.
- `genre-market-weekly.csv` now emits a non-overlapping `AllSegments` row for every canonical genre and region every live enabled week. Units can therefore be aggregated into realized annual genre arcs without double-counting overlapping segments.
- `record-genre-explanation.csv` now emits a deterministic, bounded audit sample in every region: Top-40 records plus a stable FNV-1a 1-in-16 launch sample. Rows include normalized segment blend, format tilt, generic seasonality, radio factor, final acceptance, and final demand seam.

These changes are observational except for the radio integration correction; neither telemetry writer consumes RNG or mutates simulation state. Required next evidence is a fresh enabled seed-1001 checkpoint and comparison with `4c-releaseonly-enabled-1001`, followed by the prescribed Phase-2 ladder only if the first repaired run is economically plausible.

### Repaired seed-1001 measurement (2026-07-12)

`d5-phase2-repair-bounded-enabled-1001` completed all 520 weeks and is compared below with the accepted shipping control `4c-releaseonly-enabled-1001`. The repaired telemetry streams are populated: `genre-market-weekly.csv` has 152,881 lines (header plus 42 canonical genres x 7 regions x 520 weeks), and `record-genre-explanation.csv` has 413,414 lines from the bounded deterministic sample.

| Year | Units ratio | Market-net ratio |
|---|---:|---:|
| 1960 | 1.438 | 1.427 |
| 1961 | 1.482 | 1.488 |
| 1962 | 1.455 | 1.471 |
| 1963 | 1.487 | 1.501 |
| 1964 | 1.399 | 1.424 |
| 1965 | 1.464 | 1.501 |
| 1966 | 1.531 | 1.599 |
| 1967 | 1.609 | 1.638 |
| 1968 | 1.588 | 1.656 |
| 1969 | 1.696 | 1.741 |

Decade ratios are `1.514` units and `1.569` market net. This fails the individual-seed `[0.90, 1.10]` decade gate and every annual catastrophic guard from 1960 onward. The end-of-run live pool is 17,244 records (1,519 Singles and 15,725 Albums), versus the 4C control's 14,207 (1,338 Singles and 12,869 Albums).

The radio correction improved the immediate week-one result from 1.087x to 1.066x against the 4C control, but the year-one result remains 1.438x. Therefore the remaining defect is not a late album-retirement effect and must be diagnosed at the Phase-2 regional demand/acceptance integration seam before any authorized calibration probe. Phase 2 remains rejected; do not begin Phase 3.

### Continuation brief: regional demand inflation diagnosis (2026-07-12)

No simulation behavior was changed during this diagnosis. The next implementation remains a Phase-2 regional-demand repair; radio, retirement, and Phase 3 momentum work are out of scope.

#### Findings

1. **The enabled regional acceptance seam applies the legacy momentum input at the wrong strength.** `GenreAcceptanceExplanation` currently computes `baseline * regional * routing + legacyMomentum`. The accepted control applies the same global accumulator through `momentumInfluence = 0.3`. Because prewarm leaves the established chart genres at the accumulator cap of `1.0`, the enabled service adds a full `+1.0`, not the control-equivalent `+0.3`, and clamps the result to `1.0`. This is visible in the repaired telemetry from live week 1: Doo-Wop has baseline `0.747604`, `preShock/postShock = 1`, and `effectiveAcceptance = 1` in all seven regions. Traditional Pop behaves the same way. The annual aggregate shows mean effective acceptance `1.000` in every year for every supplied high-volume genre inspected: Doo-Wop, Traditional Pop, Teen Pop, Rock and Roll, and Soul.

2. **The Single demand transfer then compresses the entire authored acceptance range into a high floor.** `ChartSimulator.CalculateRegionalSales` multiplies conversion by `.60 + .50 * genreAcceptance`. Thus a true zero still retains 60% conversion and every saturated supplied genre receives `1.10`. Even after correcting the missing momentum scale, the `[.60, 1.10]` transfer cannot express the intended collapse of a genre whose catalog baseline approaches zero. The identically shaped `GetRegionalRadioOpportunity` helper is not the remaining economic owner: it affects radio opportunity, while line 156 of `ChartSimulator` is the direct Single sales seam.

3. **Albums amplify the same saturation without the Single floor.** `MarketRegion.GetAlbumMarketSize` consumes `GetRegionalDemandAcceptance` directly. With the erroneous full-strength momentum addition, supplied genres receive a full buying-population acceptance of `1.0`; this replaces the control's regional acceptance and, for R&B/Soul/Doo-Wop/Gospel, also bypasses the legacy `GetSegregationFactor`. This explains why the gap grows across the decade and why market net rises faster than units.

4. **The remembered zero affinities are present, but they are not the cause.** Several future legacy genre preferences in `chart_manager.tscn` default to zero `baseAcceptance` and zero `affinity`. On the old path, `baseAcceptance` owned regional market size; `affinity` only feeds launch sentiment (`GetGenreFit`). On the enabled path, `GenreAcceptanceService` reads neither field. It uses catalog baseline, centered segment routing, and a few hard-coded regional factors. Therefore zero affinity is neither suppressing future enabled demand nor producing the inflation; the authored regional preference data is currently bypassed altogether.

5. **The historical supply arc is independently disconnected from the catalog lifecycle.** `ChartManager.IsGenreAvailableInYear` has no caller. The live label/artist supply continues to originate from legacy preferred genres and is canonicalized once; catalog lifecycle state changes acceptance but does not govern new artist/record supply. In telemetry, active Doo-Wop records rise from a weekly maximum of 254 in 1960 to 1,956 in 1969, so falling acceptance is being asked to offset an eightfold active-catalog expansion. Conversely, British Beat and Psychedelic Rock have zero eligible records in every measured week, hence zero units even while their effective acceptances follow the authored arc. Only 12 of the 42 canonical genres emit any units in this run.

#### Required next repair

Keep the repair at the regional demand transfer boundary and preserve the rejected run as evidence:

- restore the configured legacy momentum influence when the Phase-2 service consumes the legacy accumulator; do not feed raw capped momentum additively into acceptance;
- replace the `.60 + .50 * acceptance` Single-sales floor with a transfer that has a genuine near-zero output for near-zero catalog acceptance, a documented neutral point, and bounded high-end lift; use one named helper so telemetry and realized sales cannot diverge;
- define the Album seam from the same resolved regional acceptance and explicitly adjudicate whether the control segregation effect is represented by segment/regional routing before removing it;
- add fixed-input probes for acceptance `0`, near-zero, neutral, and `1`, plus a probe proving that legacy momentum `1` contributes the configured `0.3` rather than `1.0`;
- rerun seed 1001 for 1 week, 52 weeks, and only then 520 weeks. The first two gates must show both bounded aggregate ratios and unsaturated supplied-genre acceptance before spending a decade run.

The supply/lifecycle disconnection must be recorded for a subsequent authorized Phase-2 supply repair, but it should not be hidden by tuning demand. A passing demand rerun must still reject historical-arc acceptance if Doo-Wop active supply continues to expand or British Beat/Psychedelic Rock remain absent. Phase 2 remains rejected; do not begin Phase 3.

### Regional demand transfer repair attempt (2026-07-12)

Implemented the required boundary-only repair without changing radio evolution, retirement, supply generation, or Phase 3:

- `GenreAcceptanceService` now applies the configured `ChartManager.momentumInfluence` (default `0.3`) to the legacy accumulator. The contribution is a relative lift on the routed catalog acceptance, so raw capped momentum can no longer overwrite the authored baseline with a `+1.0` acceptance-point addition.
- The enabled live Single seam uses the named `GetEnabledSingleDemandMultiplier`: acceptance `0` maps to `0`, `0.50` maps to the documented neutral multiplier `1.00`, and acceptance `1` maps to the bounded `1.10` lift. Disabled and prewarm Single behavior retain the legacy `.60 + .50 * acceptance` transfer.
- The Album buyer pool uses the same resolved regional acceptance and explicitly retains `MarketRegion.GetSegregationFactor`; present segment/regional routing does not represent the established Black-market access factor.
- Fixed-input probes cover Single acceptance `0`, near-zero, neutral, and `1`, plus confirmation that legacy momentum `1` contributes `0.3` rather than `1.0`.

The solution build passed (`dotnet build "Label Man.sln" --no-restore`; only the pre-existing unused `OnGenreMomentumChanged` event warning remained). The one-week fixed-input enabled probe command completed with no `CHART_AUDIT_FAILED`:

```powershell
Godot_v4.7-stable_mono_win64.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-demand-repair-v3-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only
```

The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic still appeared. Against the matching one-week disabled shipping control (`d5-phase2-demand-repair-control-1001`), enabled ratios were **1.056112 units** and **1.059870 market net**. The 12 supplied canonical genres were unsaturated in the aggregate telemetry; Traditional Pop was the maximum at `0.971476`.

The required 52-week gate failed against `d5-phase2-demand-repair-52-control-1001`:

| Year | Units ratio | Market-net ratio | Result |
|---|---:|---:|---|
| 1960 | 1.195260 | 1.184556 | Fail â€” both exceed `1.10` |

All 12 supplied genres remained below `1.0` in the enabled 52-week aggregate telemetry (maximum `0.971476`, zero saturated genre-region-week rows), so the earlier acceptance-cap defect is repaired. However, the economic aggregate remains outside the required `[0.90, 1.10]` band. **Do not run the 520-week checkpoint or begin Phase 3.** The next authorized work is a new diagnosis of the remaining 52-week demand inflation; do not conceal the supply/lifecycle disconnection with further ungrounded demand tuning.

### Era-weighted format conservation repair (2026-07-12)

The remaining 52-week inflation was traced to format orientation, not acceptance saturation. The prior probe proved only that the unweighted Single and Album multipliers summed to `2.0`, which assumes equal format opportunity. The 1960 treatment was actually `98.7%` Singles. Its realized unit-weighted Single tilt was `1.102777`, so the supposedly centered orientation layer was granting aggregate demand to the Single-heavy supplied catalog. Removing that observed tilt estimated a `1.075x` units treatment, inside the economic gate.

The repair keeps the authored orientation strength but normalizes the Single and Album multipliers against the accepted regional Album-demand era progress:

```text
(1 - albumOpportunity) * singleMultiplier + albumOpportunity * albumMultiplier = 1
```

Consequently, a nearly all-Single 1960 market cannot receive a broad Single-demand grant merely because its supplied genres are Single-oriented; the tilt becomes a genuine reallocation as Album opportunity emerges. Realized Single demand, realized Album demand, AI format priors, and explanation telemetry use the same normalization. The format layer is also explicitly neutral during prewarm and activates only on the first live enabled tick.

Fixed probes now cover prewarm neutrality, live activation, early Single-market conservation, era-weighted combined-opportunity conservation, and live AI-prior parity. The build passes with only the existing unused `OnGenreMomentumChanged` event warning. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains.

| Gate | Units ratio | Market-net ratio | Acceptance | Result |
|---|---:|---:|---|---|
| 1 week, seed 1001 | `1.004448` | `1.013632` | 12 supplied genres; max `0.971476`; zero saturated rows | Pass |
| 52 weeks / 1960, seed 1001 | `1.070556` | `1.073377` | 12 supplied genres; max `0.971476`; zero saturated rows | Pass |

The matching 52-week disabled control is byte-identical to the prior demand-repair control for `market-revenue.csv`, `release-capacity.csv`, `label-geography.csv`, and `geography-metrics.csv`.

The prescribed 520-week enabled checkpoint was requested after both short gates passed, but execution authorization was declined, so it was not run. Phase 2 therefore remains unaccepted pending that checkpoint and its historical-arc review. **Do not begin Phase 3.**

### Final 520-week checkpoint and historical-arc review (2026-07-12)

The authorized clean checkpoint completed successfully:

```powershell
Godot_v4.7-stable_mono_win64.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-phase2-final-enabled-1001 --seed=1001 --enable-genre-market-v2
```

It emitted `CHART_AUDIT_COMPLETE run=d5-phase2-final-enabled-1001 weeks=520` and no `CHART_AUDIT_FAILED`. The known `MissingSingletonsTemp.cs` autoload diagnostic remains non-fatal. Economic comparisons below use the accepted matching shipping control `4c-releaseonly-enabled-1001`; the exact disabled boundary was already accepted above.

| Year | Units ratio | Gross ratio | Label-net ratio | Market-net ratio |
|---|---:|---:|---:|---:|
| 1960 | 1.071 | 1.071 | 1.072 | 1.073 |
| 1961 | 1.131 | 1.130 | 1.124 | 1.130 |
| 1962 | 1.095 | 1.097 | 1.091 | 1.098 |
| 1963 | 1.052 | 1.047 | 1.042 | 1.047 |
| 1964 | 1.019 | 1.016 | 1.014 | 1.015 |
| 1965 | 1.090 | 1.075 | 1.067 | 1.068 |
| 1966 | 1.109 | 1.082 | 1.084 | 1.079 |
| 1967 | 1.179 | 1.073 | 1.069 | 1.069 |
| 1968 | 1.221 | 1.106 | 1.092 | 1.106 |
| 1969 | **1.271** | **1.139** | **1.112** | **1.136** |

Decade units are **1.122x** and market net **1.084x**. The individual-seed decade units gate `[0.90, 1.10]` fails; 1969 units are also beyond the `[0.75, 1.25]` annual catastrophic guard. This alone rejects the economic checkpoint.

The independent historical-arc review also fails. Doo-Wop declines from 33.26m regionalized units in 1962 to 3.57m in 1969, and Folk crests in 1965 before declining. Surf Rock crests in 1963 and gradually fades. But the live supply remains disconnected from catalog lifecycle/generation: British Beat, Psychedelic Rock, Hard Rock, Funk, Easy Listening, Blues, Classical, Childrens, TexMex, and Boogaloo have zero units in every year. Soul becomes the largest genre at 37.7% of canonical-genre units in 1967, 46.3% in 1968, and 41.7% in 1969, exceeding the 35% concentration gate in the latter three years.

**Phase 2 conclusion: rejected / not complete.** The regional/format fixed probes and short economic gates remain useful evidence, but the full checkpoint fails both economic and historical unattended-market gates. No Phase 3 momentum, adjacency, or endogenous Zeitgeist work is authorized. The next Phase-2 scope is a supply/lifecycle integration repair: enabled AI artist/record generation must use catalog availability and regional/segment-aware genre choice, while retaining deterministic disabled behavior. It must be followed by fresh short gates, the three-seed 520-week measurement set, and a new historical-arc review.

### Phase 2 supply/lifecycle integration repair (2026-07-12)

The authorized supply repair is implemented and has passed the fixed and 52-week seed-1001 gates. No 520-week run was performed.

- `GenreSupplyService` is the enabled live owner for new AI project genre selection. Pre-emergent and legacy genres are unavailable to new projects; British Pop, British Beat, and British Blues are explicitly deferred pending the dedicated British Invasion mechanic.
- AI artist selection now filters unavailable lifecycle states. Enabled scouting ranks the existing unsigned pool using artist identity, label specialties, regional segment fit, and effective regional acceptance.
- Project choice preserves artist identity 95% of the time in 1960 and 90% thereafter; declining genres retain only 30%. The bounded exploration route uses a stable FNV-1a-derived roll and consumes no global RNG.
- Every available non-British genre receives a deterministic global floor of three AI projects per year. This reallocates the existing release cadence; it does not add labels, artists, release rolls, or projects.
- AI format economics evaluate the chosen project genre, while content generation retains the artist identity and established RNG call pattern. The completed record and Album tracks then receive the chosen canonical project genre. This prevents genre-dependent title/Album construction from perturbing unrelated quality, finance, and lifecycle draws.
- Fixed probes cover established, pre-emergent, emerging, legacy, and deferred-British supply boundaries; required specialist catalog inclusion; established identity retention; and British exclusion during exploration.

Final fixed command:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-supply-probes-final-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only
```

Result: pass, with both `D5_PROBE_PASS` lines and `CHART_AUDIT_COMPLETE`; the known non-fatal `MissingSingletonsTemp.cs` diagnostic remains.

The final 52-week run `d5-phase2-supply-52-final-enabled-1001` completed successfully. Against `d5-phase2-demand-repair-52-control-1001`, 1960 units are `1.066240x` and market net is `1.082929x`, both inside `[0.90, 1.10]`. All non-British genres available for new supply in 1960 produced units, including the previously absent Easy Listening, Blues, Classical, Childrens, TexMex, and Comedy. British genres correctly remain absent.

The solution build passes with only the pre-existing unused `OnGenreMomentumChanged` warning. The next evidence remains the prescribed three-seed 520-week measurement set and historical-arc review. Phase 3 remains out of scope.

### Supply repair seed-1001 520-week checkpoint (2026-07-12)

The first full measurement checkpoint was run with the Downloads Godot Mono executable:

```powershell
Godot_v4.7-stable_mono_win64.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-phase2-supply-520-enabled-1001 --seed=1001 --enable-genre-market-v2 --lean-probe
```

It completed all 520 weeks. `--lean-probe` suppresses only the high-volume per-record/breakout rows; it does not change simulation behavior. Annual output was inspected while the run progressed. No annual economic catastrophe occurred, so it was allowed to complete; the rejection below follows from several independent final hard-gate failures. Economic controls use `4c-releaseonly-enabled-1001`, whose disabled behavior is the accepted matching boundary.

| Year | Units ratio | Gross ratio | Label-net ratio | Market-net ratio |
|---|---:|---:|---:|---:|
| 1960 | 1.037 | 1.046 | 1.052 | 1.052 |
| 1961 | 1.126 | 1.138 | 1.146 | 1.143 |
| 1962 | 1.080 | 1.093 | 1.101 | 1.099 |
| 1963 | 1.060 | 1.060 | 1.065 | 1.062 |
| 1964 | 1.015 | 1.018 | 1.024 | 1.021 |
| 1965 | 1.077 | 1.071 | 1.071 | 1.068 |
| 1966 | 0.928 | 0.959 | 0.967 | 0.963 |
| 1967 | 0.948 | 0.899 | 0.892 | 0.903 |
| 1968 | 0.999 | 0.886 | 0.872 | 0.889 |
| 1969 | 1.062 | 0.926 | 0.909 | 0.929 |

Decade ratios are `1.033` units, `0.991` gross, `0.990` label net, and `0.994` market net; all meet the individual-seed `[0.90, 1.10]` economic gate. Single and Album decade unit ratios are `1.052` and `0.889`, respectively, both within `[0.85, 1.15]`. Every annual ratio remains inside the `[0.75, 1.25]` catastrophic guard.

The checkpoint is nevertheless **rejected / stop the measurement ladder**:

- Successful releases total `0.794x` the matching control, failing the `[0.85, 1.15]` release-count gate. Pipeline album drops are `0.871x` control, but drops are not a substitute for the required successful-release comparison.
- British Beat and British Pop have zero units in every year, including 1964 onward. Their deliberate deferral while the British Invasion mechanic is absent is now a failed unattended-market gate, not merely a known limitation.
- Soul becomes the largest canonical genre at `52.2%` in 1967, `58.1%` in 1968, and `64.3%` in 1969, failing the `35%` annual concentration cap in three consecutive years. Doo-Wop does decline to 56 units in 1967 and zero thereafter; Folk crests in 1965 and Surf Rock fades after 1965, but those passing shapes do not offset the concentration and missing-British failures.

The other specialist markets are nonzero, and late Psychedelic/Hard/Blues/Progressive activity is present, but this seed already has multiple hard failures. Per the requested early-abort rule, do not run seeds 1002/1003 or begin Phase 3. The next authorized scope is a Phase-2 supply/release-capacity diagnosis: explain the release-count loss, introduce the expressly deferred British supply mechanism, and address Soul concentration without tuning unrelated demand, finance, chart, or 4C constants.

### Provisional release-repair checkpoint: demand/prior inflation (2026-07-12)

The release repair was accepted provisionally, but the fresh seed-1001 checkpoint is **failed pending integration reconciliation**. Successful releases are `1.008x` control, so restored supply volume is not itself the failing seam. Units per successful release are approximately `1.177x`; Singles are `1.184x` and Albums `1.404x`, although Albums remain a small share of total units. The prior enabled candidate produced roughly `2.8%` more units per release than this candidate, confirming that the supply repair restored missing volume rather than strengthening records.

Two deterministic integration defects explain the exposed inflation:

- `GetEnabledSingleDemandMultiplier` treated acceptance `0.50` as `1.00x`, while the accepted legacy transfer is `.60 + .50 * acceptance`, or `0.85x` at that anchor. The sampled enabled catalog averages `0.774` acceptance, where that replacement is about `1.066x` the accepted conversion. The reconciliation is the legacy transfer multiplied by `smoothstep(0, .50, acceptance)`: absent and near-absent genres remain near zero, while `0.50` and `1.00` retain `0.85x` and `1.10x` respectively.
- `CalculateSingleGenreMarketFactor` averaged enabled AI comparisons across all 42 canonical genres, including pre-emergent genres that cannot supply a project that year. This drove `4,309 / 4,349` enabled Single decisions (`99.1%`) to the `1.30` cap, versus `3,019 / 4,313` controls; enabled Album decisions fell to `21.84%` compared with `25.27%` in control. The enabled denominator must instead use `GenreSupplyService.GetAvailableGenres(year)`.

The repair is limited to those two seams. Soul, supply routing, British weights, catalog keyframes, and finance constants remain unchanged. Fixed probes now anchor the reconciled Single transfer and verify that the supplied comparison set yields differentiated AI relative-market factors instead of near-universal saturation. A fresh 52-week seed-1001 checkpoint is required next; economics, successful releases, Album projects, and format mix must all pass before any 520-week measurement resumes.

### Demand/prior reconciliation seed-1001 52-week checkpoint (2026-07-12)

Build: `dotnet build "Label Man.sln" --no-restore` passed with only the pre-existing unused `OnGenreMomentumChanged` warning. The revised fixed-input suite also passed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-demand-prior-probes-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only
```

It emitted both `D5_PROBE_PASS` lines and `CHART_AUDIT_COMPLETE`. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains.

The fresh checkpoint completed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d5-phase2-demand-prior-52-enabled-1001 --seed=1001 --enable-genre-market-v2 --aggregate-only
```

Compared with `d5-phase2-demand-repair-52-control-1001`, it is **rejected / do not run 520 weeks**:

| Gate | Enabled/control | Result |
|---|---:|---|
| Units | 1.108 | Fail (`[0.90, 1.10]`) |
| Gross | 1.125 | Fail (`[0.90, 1.10]`) |
| Label net | 1.136 | Fail (`[0.90, 1.10]`) |
| Market net | 1.136 | Fail (`[0.90, 1.10]`) |
| Successful releases | 1.001 | Pass (`[0.85, 1.15]`) |
| Released Album projects | 1.035 | Pass (`[0.85, 1.15]`) |
| Single units | 1.103 | Pass (`[0.85, 1.15]`) |
| Album units | 1.516 | Fail (`[0.85, 1.15]`) |

The release-count format allocation is close (`19.452%` Albums versus `18.944%` control), but realized Album units remain materially inflated. The two requested seam repairs therefore fixed the release-volume and AI-cap path without satisfying the required economics and format-unit gates. No protected calibration surface was changed, and the 520-week ladder remains stopped.

### Album buyer-pool reconciliation seed-1001 52-week checkpoint (2026-07-12)

The Album-only follow-up adds a fixed-input `AlbumDemandExplanation` at the regional buyer-pool seam. It records routed acceptance, accepted legacy acceptance, segregation, Album affinity, purchase willingness, routed pre-tilt pool, accepted pre-tilt pool, normalization, format tilt, and final Album opportunity. The new `album-demand-explanation.csv` is emitted only for bounded Album audit rows and leaves all existing CSV schemas unchanged.

Enabled `GetAlbumMarketSize` now normalizes the routed pre-tilt pool to the accepted legacy regional pre-tilt pool before awareness, quality, stock, record conversion, or format tilt apply. The runtime and AI-prior format seam now centers against the accepted pre-tilt Album opportunity share for the same genre/region (nationally population-weighted for the prior), rather than passing zero from `GetAlbumDemandEraProgress(1960)`.

Build passed with only the pre-existing unused `OnGenreMomentumChanged` warning. The revised fixed-input suite passed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-album-buyer-pool-probes-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only
```

It emitted both `D5_PROBE_PASS` lines and `CHART_AUDIT_COMPLETE`. The Album probe compares Traditional Pop, Jazz, Folk, Country, Gospel, R&B, and Doo-Wop at fixed 1960 inputs; each enabled pre-tilt pool equals its accepted legacy counterpart, and each accepted nonzero Album opportunity centers the matching Single/Album multipliers exactly. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains.

The requested 52-week checkpoint completed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d5-phase2-album-buyer-pool-52-enabled-1001 --seed=1001 --enable-genre-market-v2 --aggregate-only
```

Compared with `d5-phase2-demand-repair-52-control-1001`, it is **rejected / do not run 520 weeks**:

| Gate | Enabled/control | Result |
|---|---:|---|
| Units | 1.137 | Fail (`[0.90, 1.10]`) |
| Gross | 1.134 | Fail (`[0.90, 1.10]`) |
| Label net | 1.122 | Fail (`[0.90, 1.10]`) |
| Market net | 1.139 | Fail (`[0.90, 1.10]`) |
| Successful releases | 0.984 | Pass (`[0.85, 1.15]`) |
| Single units | 1.137 | Fail (`[0.85, 1.15]`) |
| Album units | 1.113 | Pass (`[0.85, 1.15]`) |
| Album releases | 0.851 | Pass (`[0.85, 1.15]`) |

The new explanation rows have mean routed pre-tilt pool `26,853`, accepted/actual pre-tilt pool `24,360`, and fixed-input normalization `0.789`; actual equals accepted to CSV precision. Mean realized raw Album demand per project fell from `3,085` in the preceding rejected enabled checkpoint to `2,675`, but remains `1.302x` the control's `2,054` because the enabled project ecosystem and genre allocation still differ. No Single demand conversion, supply routing, Soul/British behavior, finance, inventory, or chart constants changed in this iteration. The corrected format prior allocation reduced Album releases to `0.851x` control and consequently rerouted volume to Singles; the Album-specific gate now passes, while the resulting Single and aggregate residual requires a separate diagnosis before resuming the measurement ladder.

### Album AI-prior denominator reconciliation seed-1001 52-week checkpoint (2026-07-12)

The next isolated seam was `CalculateAlbumDemandFactor`: it used the accepted/normalized Album numerator but divided it by the enabled routed genre market, while format centering already used accepted Album opportunity. A shared fixed-input helper now defines accepted opportunity as:

```text
sum(region.acceptedPreTiltAlbumPool)
/
sum(region.acceptedLegacyGenrePool)
```

`GetNationalAlbumOpportunity`, `CalculateAlbumDemandFactor`, and the fixed prior explanation all use this same helper. The explanation exposes accepted Album pool, accepted legacy genre pool, untilted demand factor, format tilt, and final Album prior. No Single conversion, supply routing, finance, Soul/British setting, or tilt-strength constant changed.

Build passed with only the pre-existing unused `OnGenreMomentumChanged` warning. The fixed-input suite passed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d5-phase2-album-prior-probes-1001 --seed=1001 --enable-genre-market-v2 --genre-market-v2-probes --aggregate-only
```

For Traditional Pop, Jazz, Folk, Country, Gospel, R&B, and Doo-Wop in a fixed 1960 region, the probe proves that the untilted AI Album demand factor equals the accepted opportunity used for centering, the Album tilt equals the corresponding realized tilt, and `Album prior = untilted factor * tilt`. Both `D5_PROBE_PASS` lines and `CHART_AUDIT_COMPLETE` were emitted; the known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains.

The fresh checkpoint completed:

```powershell
Godot_v4.7-stable_mono_win64_console.exe --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d5-phase2-album-prior-52-enabled-1001 --seed=1001 --enable-genre-market-v2 --aggregate-only
```

Compared with `d5-phase2-demand-repair-52-control-1001`, it is **rejected / do not run 520 weeks**:

| Gate | Enabled/control | Result |
|---|---:|---|
| Units | 1.135 | Fail (`≤1.10`) |
| Gross | 1.148 | Fail (`≤1.10`) |
| Label net | 1.158 | Fail (`≤1.10`) |
| Market net | 1.158 | Fail (`≤1.10`) |
| Successful releases | 0.992 | Pass (`[0.85, 1.15]`) |
| Single units | 1.131 | Fail (`[0.85, 1.15]`) |
| Album units | 1.444 | Fail (`[0.85, 1.15]`) |
| Album decisions | 1.194 | Fail (`[0.85, 1.15]`) |

The denominator mismatch was real and the fixed-input parity is exact, but replacing it raises Album decisions beyond the permitted mix rather than restoring the desired control share. Mean raw Album demand per project is `2,486` versus control `2,054` (`1.210x`), an improvement over the previous `1.302x` but insufficient once the larger Album decision count compounds it. The next investigation must explain this prior-allocation overshoot before any further calibration or measurement-ladder work.

### Fixed-cohort format-decision diagnosis (2026-07-12)

No demand, denominator, buyer-pool, supply, finance, Soul/British, global-scalar, or tilt-strength value changed. The diagnostic adds two observational outputs:

- `format-decision-explanation.csv` records the pre-tilt Single and Album contributions, aggregate Album affinity and accepted opportunity, both tilts, production costs, memory EMA/confidence/blends, sampled noise multipliers, final compared margins, and choice.
- `format-decision-cohorts.csv` aggregates realized-to-checkpoint units and units per decision by decision genre and chosen format, including zero-unit scheduled projects and retired records.

The fixed suite now evaluates Gospel, Folk, Traditional Pop, Jazz, R&B, and Doo-Wop with memory `0` and noise `1`; it also evaluates an orientation-neutral counterfactual while retaining the reconciled accepted opportunity. Build passed with only the pre-existing unused `OnGenreMomentumChanged` warning, and the one-week probe run `d5-phase2-format-decision-probes-1001` emitted both `D5_PROBE_PASS` lines and `CHART_AUDIT_COMPLETE` (with the known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic).

The enabled diagnostic run `d5-phase2-format-decision-52b-enabled-1001` completed 52 weeks. It is observationally equivalent to the preceding denominator checkpoint; no gate is reopened and **no 520-week run is authorized**. Replaying the recorded decision margins with only catalog orientation neutralized, while holding accepted opportunity, production costs, memories, and sampled noise fixed, changes just `2 / 4,277` binary choices (one Jazz and one Traditional Pop Album to Single). Thus the fixed cohort does **not** identify a duplicated orientation factor at the winner-take-all boundary. The cohort output confirms that units-per-decision differ materially by genre and format; that residual, rather than the reconciled denominator or catalog tilt, is the next evidence to explain before any repair is authorized.

### Final short-gate repair: existing-identity retention and Album-prior market reconciliation (2026-07-13)

The authorized supply repair first removed the duplicate regional segment-fit multiplier from `GenreSupplyService.GetSupplyWeight` and made enabled initial artists consume the legacy primary roll plus exactly one existing secondary draw before applying the shared canonical migration. The resulting `d5-supply-canonicalization-52-enabled-1001` checkpoint reduced aggregate units by 4.50M and passed aggregate economics, but Album units remained `1.487x` control with an Album decision share of `29.55%` versus `25.27%` control.

Two remaining integration seams were isolated and repaired without changing catalog keyframes, the required 80/20 genre blend, format-tilt strength, runtime Album demand, finance, stock, radio, or noise:

1. Existing domestic canonical identities may now retain their genre before commercial emergence. New pre-emergent supply remains unavailable, and the British import bridge remains binding. The authored pre-emergence baseline therefore constrains seed-scene records instead of forcibly rerouting existing Soul, Surf Rock, or Blues Rock artists into another genre and changing their format economics. The intermediate `d5-preemergent-retention-52-enabled-1001` checkpoint moved Album units from `1.487x` to `1.316x` while aggregate economics remained inside the required band.
2. The Album AI prior now receives the same enabled-versus-accepted relative-market reconciliation already present on the Single side. The runtime Album buyer pool remains normalized to accepted legacy opportunity; this change only prevents V2 from lowering a genre's Single prior while leaving its independently calibrated Album prior untouched. The factor is the enabled supplied-catalog relative market divided by the accepted legacy-domain relative market. It is exactly neutral on the disabled path and leaves genres whose relative market did not move effectively unchanged.

Fixed probes pass both `D5_PROBE_PASS` groups and explicitly verify enabled seeding/RNG count, single-entry regional supply routing, existing pre-emergent identity retention, new-supply exclusion, British bridge gating, disabled Album-prior neutrality, and the reconciled Album-prior decomposition. Build passes with only the existing unused `OnGenreMomentumChanged` warning.

The final seed-1001 candidate is `d5-album-market-reconciliation-52-enabled-1001`; its current-code matching control is `d5-album-market-reconciliation-52-control-1001`.

| Short gate | Enabled/control | Result |
|---|---:|---|
| Total units | 1.007 | Pass (`[0.90, 1.10]`) |
| Gross | 1.013 | Pass (`[0.90, 1.10]`) |
| Label net | 1.021 | Pass (`[0.90, 1.10]`) |
| Market net | 1.021 | Pass (`[0.90, 1.10]`) |
| Successful releases | 0.943 | Pass (`[0.85, 1.15]`) |
| Single units | 1.006 | Pass (`[0.85, 1.15]`) |
| Album units | 1.123 | Pass (`[0.85, 1.15]`) |
| Album projects | 0.943 | Pass (`[0.85, 1.15]`) |
| Album decision share | 25.2766% vs 25.2724% | Pass / effectively exact |

An independent enabled repeat, `d5-album-market-reconciliation-52-repeat-1001`, is byte-identical in all 45 emitted CSV streams. The current-code disabled run is byte-identical to `d5-supply-canonicalization-52-control-1001` in all 45 streams. The only terminal diagnostic in the Godot runs is the existing non-fatal `MissingSingletonsTemp.cs` autoload warning.

**Readiness decision:** the fixed probes, disabled boundary, enabled determinism, 52-week economics, release volume, format units, Album-project count, and 1960 format allocation all pass. The candidate is ready for the prescribed 520-week seed-1001 measurement checkpoint and historical-arc review. No 520-week simulation has been run from this candidate.

### Decade-run performance repair and recovered partial review (2026-07-13)

The first candidate decade run was stopped by the operator after roughly 20-30 minutes. It reached week 440, but only closed annual rows through 1967 are accepted; the buffered mid-1968 tails are not a checkpoint. Profiling showed that aggregate genre telemetry was not the main cost. In a 52-week lean profile, `CaptureWeek` used `1.54s` while `SimulateWeek` used `26.39s`; Album updates alone used `12.24s`. Each live Album was recalculating the same genre-level substitution propensity during the same simulation week, and Albums also resolved a national acceptance that the Album update never consumed.

The shipped performance repair is observation- and evaluation-order safe:

- Album substitution propensity is calculated once per primary genre in a local dictionary scoped to one `ChartManager.SimulateWeek` call, where genre momentum is fixed, then passed to each Album update. The cache cannot survive into release decisions or another week.
- The unused national acceptance read is skipped for Albums.
- Two identical Album-demand calculations within one release decision reuse their already-resolved value, and the Album-prior decomposition reuses its accepted pools.
- audit writers use a 64 KiB buffer; `--lean-probe` retains `genre-market-weekly.csv` but emits headers only for `record-genre-explanation.csv` and `album-demand-explanation.csv`, whose causal seams were already established by full 52-week telemetry.

A broader week-level market-factor cache was explicitly rejected and reverted after only `13 / 45` streams matched the accepted candidate. It incorrectly crossed release-decision timing while momentum could change. The final scoped optimization matches the accepted pre-performance candidate byte-for-byte in all 45 full-telemetry streams. The final post-routing candidate and its independent lean repeat are also byte-identical in all 45 streams. A current-code disabled replay remains byte-identical to `d5-album-market-reconciliation-52-control-1001` in all 45 streams.

The measured lean 1960 block fell from `32.08s` to `18.35s`; `SimulateWeek` fell from `26.39s` to `12.76s`, and Album updates from `12.24s` to `0.24s`. A two-year lean scaling probe completed 1960 in `17.18s` and 1961 in `24.36s`. This supports a single-digit-minute expectation for the next 520-week lean run rather than the interrupted 20-30+ minute trajectory; the run itself remains the authority.

The recovered closed 1960-1967 economics are favorable. Cumulative enabled/control ratios are `1.0225` total units, `0.9996` gross, `0.9995` label net, and `0.9978` market net. Cumulative Single units are `1.0316`, Album units `0.9141`, and decisions `0.9494`. The 1966 unit ratio `1.124` is not a decade-gate failure: it remains inside the three-seed pooled annual band `[0.85, 1.15]` and far inside the individual seed-year catastrophic band `[0.75, 1.25]`; only the completed decade total is tested against `[0.90, 1.10]` for a single seed.

The closed historical shapes are provisionally strong:

- Doo-Wop falls from `24.54m` units in 1961 to `0.70m` in 1967.
- Surf Rock peaks in 1964 and Folk in 1965, then both decline.
- British Pop and British Beat first carry units in 1964. Psychedelic Rock appears in 1966; Hard Rock, Funk, and Boogaloo carry units by 1967.
- Soul reaches `22.89%` in 1967. The largest observed annual genre share is Traditional Pop at `32.33%` in 1964, still below the `35%` cap.
- Country is stronger by regional share in the Deep South, Great Plains, and Southwest than nationally, and Boogaloo is strongest on the East Coast. The partial data exposed a copied Country/TexMex routing rule that made TexMex strongest in the Deep South. TexMex now retains the existing secondary texture but has a uniquely strongest Southwest factor; a fixed probe locks that ordering.

The first over-broad TexMex correction was rejected because it lowered two regions and moved 1960 Album units to `1.163x` control. The final localized correction restores the accepted short-gate profile: total units `1.007x`, gross `1.013x`, label net `1.021x`, Single units `1.006x`, Album units `1.123x`, decisions `0.943x`, and Album decision share `25.277%`.

**Next-run sign-off:** run seed 1001 for 520 weeks with `--enable-genre-market-v2 --lean-probe --profile-performance`. Do not tune the 1966 annual bump from this one partial seed. Accept or reject only after the complete 1960-1969 decade totals, late Psychedelic-to-Hard/Blues/Proto-Metal/Progressive succession, late Funk rise, final regional gates, and all inherited checks are available. If seed 1001 passes, proceed to the prescribed seeds 1002 and 1003; no holdout is authorized before the three-seed candidate is frozen.

### Three-seed late-Single rejection and supplied-portfolio reconciliation (2026-07-13)

The complete lean runs `d5-album-market-reconciliation-520r3-enabled-1001`, `-1002`, and `-1003` reject the preceding candidate. All three individual decade economic gates, decade format gates, successful-release gates, scheduled-Album-project gates, and catastrophic individual-year guards pass, but pooled annual units are `1.1579x` in 1968 and `1.2238x` in 1969, above the binding `1.15` ceiling. No additional seed or holdout was consumed.

The rejection is a repeatable late Single seam rather than broad economic instability. Against the matching `4c-releaseonly-enabled` controls, individual 1969 unit ratios are `1.2444x`, `1.1871x`, and `1.2404x`. Pooled 1969 units are `663.1m` enabled versus `541.9m` control (`+121.3m`): Singles contribute `+147.5m`, partly offset by Albums at `-26.2m`. In 1969, Single decision-count ratios are `1.167x`, `1.057x`, and `1.262x`, while Single units per decision are independently elevated at `1.210x`, `1.251x`, and `1.146x`. Album units are below control in every late seed. A fixed replay of the recorded format forks from tilt strength `.22` to zero moves the pooled 1969 Album decision share only from `81.38%` to `81.61%`; format tilt does not have enough causal leverage to repair the failure.

The bounded full-telemetry sample from the interrupted predecessor run isolates the growing demand seam. For Single records, the exposure-weighted enabled demand seam divided by the same-observation accepted legacy transfer is approximately `1.134x` in 1960, `1.114x` in 1964, `1.203x` in 1965, `1.329x` in 1966, `1.349x` in 1967, and `1.430x` in the partial 1968 sample. Mean legacy momentum is already approximately saturated in 1960 and remains flat; the drift comes from the routed/catalog baseline rising after 1964 while the accepted comparator declines. `SimTools/analyze-late-single-opportunity.mjs` reproduces the exact annual decision/yield bridge and the optional bounded explanation-sample comparison.

The first pure new-supply average was rejected before simulation because it produced only a `0.9711` 1968 correction and omitted retained seed-scene artists. An opportunity-reweighted variant was also rejected because it was effectively neutral (`0.9978`). Adding the exact expected 1960 primary-identity prior and the existing prospective project-retention probabilities correctly represents the fixed retained cohort without reading realized releases, release timing, units, charts, or annual results. Applying its raw correction immediately was still rejected because it would have reduced 1964 to `0.9166` despite the already-low 1964 annual result.

The retained candidate therefore activates the supplied-portfolio reconciliation across the authored catalog-expansion boundary: neutral through 1964, smooth transition during 1965, fully active from 1966. It applies one national factor and therefore preserves within-year genre and regional rankings. The factor is bounded to `[0.90,1.10]`; fixed probe values for 1960/1964/1966/1968/1969 are `1.0000 / 1.0000 / 0.9000 / 0.9000 / 0.9000`. The final prospective supply prior deliberately excludes the live regional-acceptance path because that path contains mutable momentum; its fixed enabled/accepted drift is `1.0293x` in 1960 and `1.1545x` in 1969. The accepted transfer endpoints, catalog keyframes, genre segment weights, format tilt, Album demand/prior, finance, release growth, stock, chart, seasonality, and RNG order are unchanged. Disabled and pre-expansion execution bypass the reconciliation exactly.

Validation before the long-probe stop point:

- `dotnet build "Label Man.sln" --no-restore` passes with only the existing unused `OnGenreMomentumChanged` warning.
- `d5-single-portfolio-reconciliation-probes-ready-1001` emits both `D5_PROBE_PASS` groups and locks the initial-identity prior, 1960/1964 neutrality, late bounded correction, fixed-input supply weighting, and disabled neutrality.
- `d5-single-portfolio-reconciliation-52-control-1001` is byte-identical to `d5-album-market-reconciliation-52-control-1001` in all 45 CSV streams.
- `d5-single-portfolio-reconciliation-52-enabled-1001` and `d5-single-portfolio-reconciliation-52b-enabled-1001` are byte-identical in all 45 streams; the final current-code replay `d5-single-portfolio-reconciliation-52-ready-enabled-1001` is also byte-identical to that repeat in all 45. The 1960 reconciliation is inactive, so the short gate retains total units `1.0074x`, gross `1.0133x`, label net `1.0212x`, market net `1.0212x`, successful releases `0.9430x`, scheduled Album projects `0.9431x`, Single units `1.0059x`, Album units `1.1226x`, and Album decision share `25.2766%` versus `25.2724%` control.
- The only terminal diagnostic remains the known non-fatal `MissingSingletonsTemp.cs` autoload warning. The sandboxed console runner had one native startup crash before initialization; rerunning the same Downloads runner outside the sandbox completed normally.

**Prepared stop point:** the candidate is ready for the authorized two-seed 520-week probe, but neither long run has been launched. Run exactly:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-single-portfolio-reconciliation-520p1-enabled-1001 --seed=1001 --enable-genre-market-v2 --lean-probe --profile-performance
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-single-portfolio-reconciliation-520p1-enabled-1002 --seed=1002 --enable-genre-market-v2 --lean-probe --profile-performance
```

Do not run seed 1003 or a holdout from this calibration candidate. The two-seed probe must re-evaluate every annual unit ratio, decade economics and formats, release/project gates, historical shapes, concentration, regional gates, chart lifetime, finance reconciliation, and inherited health checks before a formal three-seed checkpoint is authorized.

### Supplied-portfolio reconciliation two-seed probe (2026-07-13)

The authorized completed probes were run directly with the Downloads Godot console executable, with no code or constant changes between seeds:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-single-portfolio-reconciliation-520p1-enabled-1001 --seed=1001 --enable-genre-market-v2 --lean-probe --profile-performance
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=520 --run=d5-single-portfolio-reconciliation-520p1-enabled-1002 --seed=1002 --enable-genre-market-v2 --lean-probe --profile-performance
```

Both completed with `CHART_AUDIT_COMPLETE` at week 520. The accepted artifacts are the complete CSV families rooted at `SimLogs/d5-single-portfolio-reconciliation-520p1-enabled-1001-*` and `-1002-*`, compared only with the matching `SimLogs/4c-releaseonly-enabled-1001-*` and `-1002-*` controls. No `1003` or holdout artifact exists.

One earlier monitored wrapper invocation for seed 1001 stopped at week 104 and is **not** a measurement result. Its captured logs, `d5-single-portfolio-reconciliation-520p1-enabled-1001-console.log` and `.err.log`, remain as runner-failure evidence. The subsequent direct invocation above overwrote that incomplete run's same-stem CSVs; only its completed 520-week CSVs are used below. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains at normal process exit.

#### Economic and format gates

| Seed | Decade units | Gross | Label net | Market net | Single units | Album units | Successful releases | Scheduled Album projects | Result |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 1001 | 0.9867 | 0.9567 | 0.9556 | 0.9521 | 1.0004 | 0.8837 | 0.9569 | 1.0082 | Pass |
| 1002 | 0.9785 | 0.9645 | 0.9675 | 0.9628 | 0.9849 | 0.9296 | 0.9642 | 1.0511 | Pass |

All decade economics are inside `[0.90, 1.10]`; each format, successful-release count, and scheduled-project count is inside `[0.85, 1.15]`. Project terminal accounting also reconciles exactly: seed 1001 is `32,056 = 31,775 released + 2 cancelled + 279 pending`; seed 1002 is `32,290 = 32,044 + 3 + 243`.

| Year | Pooled units | Pooled market net | Enabled units (m) | Control units (m) | Result |
|---:|---:|---:|---:|---:|---|
| 1960 | 0.9898 | 0.9969 | 295.62 | 298.65 | Pass |
| 1961 | 1.0080 | 1.0185 | 342.82 | 340.11 | Pass |
| 1962 | 0.9640 | 0.9760 | 362.59 | 376.13 | Pass |
| 1963 | 0.9536 | 0.9523 | 360.45 | 377.98 | Pass |
| 1964 | 0.8996 | 0.8924 | 373.88 | 415.61 | Pass |
| 1965 | 0.8884 | 0.8830 | 388.47 | 437.27 | Pass |
| 1966 | 1.0113 | 0.9624 | 380.37 | 376.10 | Pass |
| 1967 | 1.0031 | 0.9570 | 368.15 | 367.03 | Pass |
| 1968 | 1.0418 | 0.9574 | 385.16 | 369.71 | Pass |
| 1969 | 1.1021 | 0.9978 | 390.32 | 354.15 | Pass |

The correction removes the rejected pooled upper breaches (`1.1579` in 1968 and `1.2238` in 1969). It creates no 1966-69 pooled lower-bound miss: those unit ratios are `1.0113`, `1.0031`, `1.0418`, and `1.1021`. The lowest individual annual unit ratio is seed-1002 1965 at `0.8687`, before the fully-active 1966 correction and still above the catastrophic `[0.75, 1.25]` guard.

| Year | Seed 1001 units / market net | Seed 1002 units / market net | Catastrophic guard |
|---:|---:|---:|---|
| 1960 | 1.0074 / 1.0212 | 0.9735 / 0.9741 | Pass |
| 1961 | 1.0129 / 1.0261 | 1.0032 / 1.0110 | Pass |
| 1962 | 0.9824 / 0.9893 | 0.9466 / 0.9633 | Pass |
| 1963 | 0.9752 / 0.9699 | 0.9331 / 0.9354 | Pass |
| 1964 | 0.9039 / 0.8821 | 0.8953 / 0.9027 | Pass |
| 1965 | 0.9093 / 0.8882 | 0.8687 / 0.8780 | Pass |
| 1966 | 1.0159 / 0.9565 | 1.0069 / 0.9681 | Pass |
| 1967 | 0.9912 / 0.9280 | 1.0149 / 0.9858 | Pass |
| 1968 | 0.9893 / 0.9231 | 1.0950 / 0.9914 | Pass |
| 1969 | 1.1164 / 0.9953 | 1.0886 / 1.0002 | Pass |

The Album gross crossover remains 1967 for both enabled seeds and both matching controls. The 1960 Album decision shares are `25.3382%` enabled versus `25.2737%` control for seed 1001 and `24.1004%` versus `22.9545%` for seed 1002; the accepted 1960 allocation remains intact. All-decade closed Top-40 medians are `11` versus `11` weeks for seed 1001 and `10` versus `11` for seed 1002, within the `+/-2` week gate.

#### Historical, concentration, and regional gates

| Gate | Evidence | Result |
|---|---|---|
| Doo-Wop decline | 1961/1967 units are `22.71m / 0.71m` in seed 1001; `25.12m / 0.91m` in seed 1002. | Pass |
| British break | British Beat and Pop are zero in 1960-63 and first carry units in 1964 in both seeds. | Pass |
| Surf arc | Surf peaks in 1963 (`3.09m`, `5.28m`) and is `0.07m`, `0.02m` in 1969. | Pass |
| Folk/Folk Rock timing | Seed 1001 crests at `7.45m` in 1968; seed 1002 at `16.98m` in 1966. | Pass relative to the observed psychedelic timing, but see the failed psychedelic gate. |
| Psychedelic-to-late-rock succession | Psychedelic peaks in 1968 (`2.69m`) for seed 1001 and 1969 (`2.09m`) for seed 1002, not around 1967. The late Hard/Blues/Proto-Metal/Progressive aggregate is strong (`14.99m`, `19.00m` in 1969), but seed 1002 has no completed post-psychedelic succession within the decade. | **Fail** |
| Soul and Funk | Soul remains strong late (`56.65m`, `66.27m` in 1969); Funk rises from 1967 to 1969 (`1.63 -> 12.07m`, `3.11 -> 8.39m`). | Pass |
| Specialist survival | Country, Jazz, Easy Listening, Gospel, Blues, Classical, Childrens, and TexMex remain nonzero annually. Boogaloo is appropriately absent before emergence, then nonzero from 1966 through 1969 in both seeds. | Pass |
| FM constraint | Pre-1967 Acid Rock is only `0.38m` / `0.98m` versus `9.41m` / `3.59m` from 1967 onward; Progressive Rock and Proto-Punk are zero before 1967 in both seeds. | Pass |
| Concentration cap | Largest annual canonical share is Soul at `29.49%` (seed 1001, 1969) and `33.43%` (seed 1002, 1969), below 35%. | Pass |
| Seasonality / special products | All 120 raw monthly 4C seasonality multipliers are exactly unchanged from each matching control. No special-product rows were emitted, so there is no special-product crowding signal. | Pass |
| Country regional share | Seed 1001 is above national (`5.97%`) in Deep South / Great Plains / Southwest (`7.49% / 6.62% / 7.72%`). Seed 1002 Southwest is `7.82%`, below its `8.26%` national share. | **Fail** |
| TexMex regional peak | Seed 1001 peaks in Deep South (`0.23%`) rather than Southwest (`0.21%`). Seed 1002 ties Great Plains and Southwest at `0.22%`, rather than making Southwest uniquely highest. | **Fail** |
| Boogaloo regional peak | Seed 1001 peaks in Great Plains (`0.17%`; East Coast `0.16%`). Seed 1002 ties East and West Coast at `0.12%`. | **Fail** |
| Gospel / Urban R&B direction | Gospel is above national in the church-strong southern/southwestern regions in both seeds (for example, `3.57%` vs `2.53%` national in seed 1001 Deep South). Lean telemetry emits only `AllSegments` rows, so this two-seed output cannot independently measure Urban R&B's `MainstreamAM` crossover; the earlier fixed input probe remains the only direct proof. | Gospel directional pass; Urban R&B not independently reverified |

#### Inherited health checks

| Check | Evidence | Result |
|---|---|---|
| Finance reconciliation | For each of 520 weeks in both seeds, market gross equals summed label gross, market label net equals summed label net, and market net equals summed label net plus distribution income. Maximum absolute difference is `0`. | Pass |
| Distance | Each enabled `distance-matrix.csv` SHA-256 is byte-identical to its matching control. | Pass |
| Concentration health | C4/C8 ranges are `45.11-61.98% / 66.32-78.01%` (1001) and `40.90-60.66% / 58.29-74.91%` (1002); firms charting remain `81-151` and `80-140`. | Pass |
| Distribution deals | Seed 1001 records `35 / 14 / 14` generated / accepted / signed deals; seed 1002 `42 / 15 / 15`. Signed counts reconcile to accepted counts and no invalid rate is present. | Pass |

**Decision: no-go for a formal three-seed checkpoint.** The supplied-portfolio correction successfully repairs the binding late economic upper breach without creating a 1966-69 lower-bound miss, and the economic, format, release/project, crossover, chart-life, finance, distance, concentration, and seasonality gates pass. It does not preserve all required unattended historical and regional behavior: seed 1002's psychedelic peak is 1969 rather than around 1967, seed 1002 misses the Country Southwest share requirement, TexMex fails to be uniquely Southwest-highest in both seeds, and Boogaloo fails to be East-Coast-highest in both seeds. Do not tune between these seeds, do not run seed 1003, and do not select or run a holdout. Preserve this failed two-seed evidence; any next change needs a new directive and must keep the protected finance, chart, release-growth, Album, seasonality, and historical-keyframe constants fixed.

### Evidence-only diagnosis: psychedelic timing and specialist routing (2026-07-13)

This investigation used only the completed `d5-single-portfolio-reconciliation-520p1-enabled-1001` and `-1002` artifacts. It launched no simulation and made no behavior, constant, or telemetry change. The supplied-portfolio reconciliation remains the current economic base.

#### 1. Psychedelic Rock, 1966-1969

`supply-selections.csv`, `release-strategy.csv`, `album-projects.csv`, `genre-market-weekly.csv`, `lifecycles.csv`, and `live-records-snapshot.csv` give the following bridge. Counts are per seed; eligible/charted counts are the weekly national sum of the seven `AllSegments` rows, and acceptance is their mean.

| Year | Supply selections (1001 / 1002) | Scheduled projects; mean delay weeks (1001 / 1002) | Mean acceptance (1001 / 1002) | Mean eligible / charted (1001 / 1002) | Annual units (1001 / 1002) | Units per supplied project (1001 / 1002) |
|---:|---:|---:|---:|---:|---:|---:|
| 1966 | 64 / 50 | 52 / 44; 3.50 / 3.57 | .7727 / .7278 | 269.63 / 2.02; 226.02 / 2.15 | 0.638m / 0.546m | 9,970 / 10,922 |
| 1967 | **108 / 116** | 103 / 108; 1.38 / 0.86 | **.9129 / .8744** | 828.69 / 4.71; 756.54 / 3.50 | 1.734m / 0.872m | 16,056 / 7,513 |
| 1968 | 93 / 89 | 91 / 82; 0.44 / 0.93 | .8548 / .7874 | 1,291.90 / 10.90; 1,335.92 / 1.35 | **2.692m** / 0.758m | **28,946** / 8,518 |
| 1969 | 76 / 77 | 74 / 77; 0.77 / 0.69 | .7039 / .7748 | 1,406.87 / 1.35; 1,409.02 / **11.58** | 0.657m / **2.092m** | 8,642 / **27,175** |

Supply and routing do not cause the late peak. Both seeds select the most Psychedelic projects in 1967, exactly when routed acceptance peaks. All 341 / 332 1966-69 selected projects serialize as primary `PsychedelicRock` in `release-strategy.csv`; no other primary project uses Psychedelic as its raw secondary. Every one was selected from a non-Psychedelic artist identity, as expected for the emerging catalog, but project scheduling is not late: after 1967, mean Album delay is below 1.4 weeks and scheduled/drop counts track closely.

Carry-in expands the eligible catalog, but it is not the primary timing failure. First-week eligible totals rise from 518 / 413 in 1967 to 1,463 / 1,519 in 1969. At the 1969 endpoint, 135 / 134 of 207 live Psychedelic records were from pre-1969 cohorts. However, seed 1002's 1969 cohort alone has 1.552m observed units, versus 0.761m from the live pre-1969 cohorts. Thus catalog persistence is background capacity, not the source of the 1969 spike.

The late shape is a record-yield event concentrated in cross-identity, primary-Psychedelic Singles:

- Seed 1001's 1968 high-yield closed Singles include `gen_64191` (1.173m observed lifetime units, raw secondary TeenPop) and `gen_64759` (0.432m, DooWop), while the annual Psychedelic total is 2.692m.
- Seed 1002's 1969 peak is dominated by `gen_78782`: a primary Psychedelic, raw-secondary DooWop, orphan Single released in week 498. It is Q4 quality (`.802883`), has four charted prior Singles and hit score `1.024168`, entered with `0.973856` Single confidence, and has 1.328m units by week 520. That is 63.4% of seed 1002's entire 1969 Psychedelic annual total. Its current chart position is already zero, so this is not a slow late-decade chart-persistence effect.

The common runtime route gives every distinct primary/secondary pair a fixed 80/20 acceptance and format blend. The lean decade runs intentionally suppress per-record `record-genre-explanation.csv`, so they cannot apportion the 20% secondary contribution to those individual outliers. They nevertheless establish the first causal seam: **late Psychedelic timing is a record-level demand/yield issue on non-identity transition projects, not a supply-count, release-delay, or catalog-keyframe issue.** The catalog baseline already reaches its intended high point in 1967; no historical keyframe change is supported.

**Smallest future Psychedelic repair to evaluate, not implement here:** constrain the existing non-retained transition route before record construction so an emerging Psychedelic primary can draw a legacy secondary only from the existing compatible/adjacent transition set. Preserve the selected project count, the 1966-67 floor, the 80/20 blend for compatible pairs, and all release rolls; reallocate an incompatible selection through the existing prospective candidate weighting rather than creating demand. This directly excludes the observed late TeenPop/DooWop-to-Psychedelic outlier path, shifts the supplied transition mix toward the authored 1967 emergence window, and does not require a baseline-keyframe, finance, chart, Album, or seasonality change. A future fixed probe must first prove that the compatibility predicate is static, deterministic, and neutral for retained identities; a bounded existing full-telemetry replay must then confirm the primary/secondary demand decomposition before any long run.

#### 2. Country, TexMex, and Boogaloo regional funnel

The existing aggregate streams resolve the location of the specialist failures. `genre-market-weekly.csv` is pre-fulfillment route evidence; `geography-metrics.csv` contains fulfilled units and explicit backorders. In the lean runs `breakout-funnel.csv` and `records.csv` contain headers only, so they cannot provide per-record raw demand, stock coverage, or launch-level regional attribution. The figures below are therefore deliberately aggregate: fulfilled share uses the required regional total-unit denominator, and backorder percent is `backorders / (fulfilled + backorders)`.

For each target and seed, eligible and charted counts are identical across all seven regions, eliminating region-specific selection, primary/secondary mix, and chart-entry count as the first divergence:

| Genre | Seed 1001 mean eligible / charted | Seed 1002 mean eligible / charted |
|---|---:|---:|
| Country | 578.83 / 14.49 | 672.21 / 19.53 |
| TexMex | 30.12 / 0.21 | 36.61 / 0.11 |
| Boogaloo (1966-69) | 61.92 / 0.46 | 74.87 / 0.57 |

`effectiveAcceptance` retains the authored ordering before fulfillment. Country is .898-.899 in Deep South, Great Plains, and Southwest versus .638 elsewhere; TexMex is .437 / .414 in Southwest versus .352 / .334 in Deep South and Great Plains; Boogaloo is .465 / .440 on East Coast versus .335 / .317 elsewhere. Radio does not reverse TexMex: Southwest radio is 211 / 238, higher than both Deep South (198 / 228) and Great Plains (193 / 220). The regional fulfilled-share funnel is:

| Genre / region | Acceptance 1001 / 1002 | Fulfilled units m 1001 / 1002 | Share % 1001 / 1002 | Backorder % 1001 / 1002 | Home-region share % 1001 / 1002 |
|---|---:|---:|---:|---:|---:|
| Country — Deep South | .898 / .899 | 9.226 / 11.207 | 7.494 / 9.612 | 17.237 / 25.560 | 33.07 / 37.08 |
| Country — East Coast | .638 / .638 | 37.359 / 55.114 | 5.392 / 7.605 | 40.421 / 38.438 | 58.33 / 78.85 |
| Country — Great Lakes | .638 / .638 | 22.094 / 33.891 | 5.001 / 7.625 | 47.450 / 45.306 | 7.73 / 3.78 |
| Country — Great Plains | .899 / .899 | 8.876 / 15.173 | 6.617 / **10.184** | 49.556 / 41.243 | 0.35 / 0.56 |
| Country — Rockies | .638 / .638 | 2.632 / 3.584 | 6.013 / 8.392 | 26.755 / 30.502 | 2.51 / 0.88 |
| Country — Southwest | .898 / .898 | 10.767 / 10.233 | **7.720** / 7.818 | 24.886 / **50.367** | 55.58 / 10.37 |
| Country — West Coast | .638 / .638 | 16.475 / 23.605 | 7.379 / 9.789 | 45.769 / 43.537 | 32.41 / 11.32 |
| TexMex — Deep South | .352 / .334 | 0.279 / 0.215 | **0.226** / 0.184 | 12.003 / 19.184 | 14.70 / 19.63 |
| TexMex — East Coast | .240 / .227 | 1.095 / 1.040 | 0.158 / 0.144 | 35.883 / 37.166 | 54.57 / 60.71 |
| TexMex — Great Lakes | .240 / .227 | 0.751 / 0.665 | 0.170 / 0.150 | 23.595 / 26.908 | 39.05 / 28.27 |
| TexMex — Great Plains | .352 / .334 | 0.302 / 0.325 | 0.225 / **0.218** | 29.246 / 14.639 | 2.46 / 0.17 |
| TexMex — Rockies | .240 / .227 | 0.071 / 0.061 | 0.161 / 0.143 | 8.566 / 3.809 | 2.09 / 0.93 |
| TexMex — Southwest | **.437 / .414** | 0.299 / 0.283 | 0.214 / 0.216 | 28.155 / 14.220 | 15.87 / 17.11 |
| TexMex — West Coast | .240 / .227 | 0.354 / 0.448 | 0.159 / 0.186 | 44.616 / 27.907 | 14.89 / 19.99 |
| Boogaloo — Deep South | .335 / .317 | 0.173 / 0.092 | 0.141 / 0.079 | 7.883 / 17.693 | 16.38 / 22.29 |
| Boogaloo — East Coast | **.465 / .440** | **1.132 / 0.847** | 0.163 / 0.117 | 35.469 / **48.770** | 67.56 / 67.83 |
| Boogaloo — Great Lakes | .335 / .317 | 0.557 / 0.412 | 0.126 / 0.093 | 35.967 / 41.073 | 25.82 / 27.66 |
| Boogaloo — Great Plains | .335 / .317 | 0.223 / 0.142 | **0.166** / 0.096 | 18.678 / 27.763 | 0.89 / 0.00 |
| Boogaloo — Rockies | .335 / .317 | 0.052 / 0.040 | 0.118 / 0.094 | 15.472 / 2.937 | 2.52 / 3.39 |
| Boogaloo — Southwest | .335 / .317 | 0.160 / 0.127 | 0.115 / 0.097 | 25.010 / 15.731 | 5.42 / 16.81 |
| Boogaloo — West Coast | .335 / .317 | 0.342 / 0.287 | 0.153 / **0.119** | **41.498** / 45.883 | 25.89 / 19.88 |

The first causal seam differs slightly by target but is downstream of authored acceptance:

- **Country:** the three preferred regions receive equal high routed acceptance and equal national eligible/charted counts. Seed 1002 Southwest then loses at fulfilled-share accounting, with a 50.367% backorder rate and a larger regional total-unit denominator. This is a fulfillment/denominator failure, not routing or radio.
- **TexMex:** Southwest has the highest acceptance and higher radio, but its relative uplift is too small to survive the different regional total-unit denominators. Fulfillment is not a common explanation: Southwest backorders are high in seed 1001 but lower than Deep South and tied with Great Plains in seed 1002. The first stable failure is therefore the population/denominator-unbalanced specialist acceptance contrast, not coverage, stock, or secondary blending.
- **Boogaloo:** East Coast has the highest acceptance and by far the most absolute fulfilled units, yet its much larger regional denominator produces only a tie/loss in share. High East Coast backorders (35.469% / 48.770%) then further suppress fulfillment. The route is correct; the share failure begins at population/denominator scaling and is worsened by fulfillment, not by selection, charting, or radio.

**Smallest future regional repair to evaluate, not implement here:** replace the three uncentered specialist constants in `GenreAcceptanceService.GetRegionalFactor` with one static, population-centered specialist texture helper. Its inputs must be only fixed regional buying-population/capacity priors and the authored target ordering; its population-weighted national mean must be exactly 1.0 for each specialist genre/year. Country must distinguish Southwest above the other two preferred regions; TexMex must give Southwest sufficient centered relative texture to exceed Deep South and Great Plains after their fixed denominators; Boogaloo must give East Coast sufficient centered texture to exceed the West/Great Plains share competition. Do not use realized units, release timing, charting, live momentum, backorders, or any annual result to normalize it.

This is intentionally an acceptance-route repair first, not a change to distance, finance, release capacity, chart rules, or a stock/coverage rule. A fixed population-conservation probe must demonstrate the national opportunity is unchanged and the regional ordering is correct before a new 52-week check. That check must report raw demand and fulfilled/backordered units through the existing full breakout telemetry; if the centered route raises East Coast Boogaloo demand without improving fulfillment, stop and separately diagnose fixed-stock allocation rather than widening demand again.

**Recommended next implementation directive:** authorize only (1) a deterministic compatibility predicate for non-retained emerging Psychedelic transitions, preserving the existing project budget and 80/20 blend for compatible pairs, and (2) the static population-centered Country/TexMex/Boogaloo route helper described above, with fixed probes for identity neutrality, regional-ordering, and conserved national opportunity. Keep the supplied-portfolio reconciliation, finance, charts, release-capacity growth, Album behavior, seasonality, distance, and historical keyframes unchanged. Require an enabled 52-week full-telemetry checkpoint before any additional 520-week measurement; do not use seed 1003 or a holdout unless a subsequent directive expressly authorizes them.

### Evidence-only diagnosis: specialist fulfillment and late compatible Psychedelic yield (2026-07-13)

This investigation used only the completed frozen-candidate artifacts from commit `67567f5`, principally `d5-specialist-opportunity-normalizer-520p1-enabled-1001` plus the preceding full-telemetry 52-week run. It launched no simulation and changed no behavior, acceptance texture, historical keyframe, finance, chart, Album, release-capacity, seasonality, or distance input. Seed 1002 remains unrun.

#### 1. TexMex fixed-stock allocation and fulfillment

The centered acceptance route succeeds before fulfillment. Across the decade, Southwest mean TexMex acceptance is `.560432`, versus `.374302` in Deep South and `.358706` in Great Plains. Eligible and charted record counts are identical across regions (`36.6538 / .2808` weekly means), and Southwest radio is also highest (`251` versus `248` and `234`). Fulfilled units plus surviving backorders retain a uniquely Southwest-highest demand proxy: Southwest is `622,487 / 137,531,002 = .4526%`, versus Great Plains `581,562 / 139,025,363 = .4183%`. Fulfillment alone reverses that order:

| Region | Fulfilled units | Fulfilled share | Surviving backorders | Backorder percent |
|---|---:|---:|---:|---:|
| Great Plains | 474,980 | .34165% | 106,582 | 18.33% |
| Southwest | 440,599 | .32036% | 181,888 | 29.22% |

The reversal begins only after scale rises. Southwest has the higher intent share in every year, but its fulfilled share falls below Great Plains in 1966-69. The annual Southwest versus Great Plains backorder rates are `33.04% / 19.17%` in 1966, `39.09% / 25.75%` in 1967, `40.81% / 8.48%` in 1968, and `28.13% / 16.12%` in 1969.

MidTier repertoire is the binding cohort. Its Southwest fulfilled/backordered units are `205,682 / 116,921`, versus `286,593 / 31,044` in Great Plains. Removing only MidTier from the fulfilled comparison restores the intended order (`234,917` Southwest versus `188,387` Great Plains). Non-national MidTier rows account for `77,065` Southwest backorders versus `22,718` in Great Plains, while national MidTier rows are also worse (`39,856` versus `8,326`). Major-label TexMex has zero Southwest backorders, so this is not a universal Southwest capacity failure.

The runtime explains the first physical seam. `ChartSimulator.CalculateInitialRegionalStock` receives label, destination, career scale, and perceived quality, but no genre or routed regional opportunity. Initial placement is therefore genre-blind. Uncharted replenishment in `ChartManager.RestockHotRecords` then requires `breakoutScore >= .20` before either stock exhaustion or backorders can trigger restock. The full-telemetry 1960 checkpoint exposes the failure directly for TexMex record `gen_3648`: Southwest stock falls from `532` to zero by age four; backorders then reach `183`, raw demand remains positive, and no restock is requested or applied because breakout score never exceeds `.0611`. Great Plains shows the same mechanism at lower demand, reaching `135` backorders with a maximum breakout score of `.0562`. The lean decade run suppresses those record-level rows, but its tier/year aggregates show the same stock-limited signature once MidTier TexMex scales up.

**Diagnosis:** the first confirmed TexMex failure is genre-blind initial stock followed by a breakout-gated uncharted restock path that cannot serve dispersed niche demand. The centered acceptance texture is already sufficient and must not be widened. A future repair should be evaluated only at fixed-stock placement or uncharted fulfillment, should conserve each record's national initial-stock budget, and should not change destination capacity, distance, finance, charts, release supply, or demand.

#### 2. Late compatible Psychedelic orphan Singles and carry-in

The compatibility predicate works. Every 1966-69 supplied Psychedelic project has a compatible Rock and Roll, Surf Rock, or Blues Rock artist identity/secondary; Teen Pop and Doo-Wop are absent. Routed acceptance peaks in 1967, not 1969, while supply peaks in 1968:

| Year | Supply selections | Mean acceptance | Mean eligible / charted | Units |
|---:|---:|---:|---:|---:|
| 1966 | 14 | .6768 | 13.17 / 0 | 127,639 |
| 1967 | 28 | **.7993** | 31.29 / 0 | 131,578 |
| 1968 | **37** | .7001 | 52.56 / 0 | 180,306 |
| 1969 | 19 | .7210 | 68.43 / .2353 | **281,784** |

Two 1969 compatible orphan Singles supply the binding late increment:

| Record | Week | Secondary | Quality | Prior Single / Album net | Projected Single / Album net | Observed units |
|---|---:|---|---:|---:|---:|---:|
| `gen_76102` | 481 | Surf Rock | .6662 | 27,089 / 121,511 | 12,209 / 10,146 | 49,082 |
| `gen_80893` | 507 | Rock and Roll | .6268 | 26,239 / 129,916 | 10,060 / 9,111 | 73,781 |

Together they contribute `122,863`, or `43.60%`, of all 1969 Psychedelic units. Subtracting only their observed units leaves `158,921`, below the 1968 total; this is an arithmetic attribution, not a replay counterfactual, but it proves the two Singles are sufficient to create the observed 1969 peak. The 19 new 1969 Psychedelic records have `165,243` observed lifetime units by week 520, of which the two Singles supply `74.35%`; the remaining `116,541` annual units are the bounded contribution from pre-1969 carry-in. Without the two Singles, carry-in plus the new Album cohort does not independently create the late peak.

Both format forks have a strong Album prior, but high-confidence label-wide format memories replace most of that genre-specific prior before independent noise is applied. `gen_76102` has Single/Album confidence `.9819 / .9385`; `gen_80893` `.9570 / .9802`. Their final margins differ by only `$2,063` and `$949`. The memory store is keyed only by label and format, not project genre, so unrelated catalog outcomes dominate the emerging-genre prior at this fork.

Both artists serialize as `Dropped`, and the runtime reveals why they remain eligible: `SimulatedArtist.UpdateCareerState` can assign `Dropped`; `AILabel.ShouldDropArtist` does not recognize that terminal state; `GenreSupplyService.IsEligibleExistingArtistForRelease` checks only `artist.isActive`; and `DecideRelease` maps the unexpected state back to the New/Unsigned career band. This is not a narrow two-record anomaly: by 1969, `5,932 / 6,166` release decisions (`96.20%`) use Dropped artists, and all 98 supplied Psychedelic projects in 1966-69 do. A global terminal-artist exclusion would therefore destroy the protected release topology and is not an acceptable repair.

**Diagnosis:** the compatibility and carry-in paths are not the remaining binding defect. The first narrow fork is the combination of prospective emerging-genre projects with genre-agnostic, high-confidence label-format memory; it lets two otherwise compatible late projects reverse overwhelming Album priors and become high-yield orphan Singles. The broader Dropped-roster lifecycle leak is real but cannot be repaired inside this directive without replacing most release capacity. Do not change Album constants, format noise, historical keyframes, release growth, or global roster eligibility from this evidence. Any future repair needs a separately authorized, fixed-input rule at the non-retained emerging-project format-memory seam, preserving the project count and disabled RNG boundary, followed by exact fork probes before another simulation.

**Stop decision:** no seed 1002, seed 1003, or holdout is authorized. The smallest independently supported future surfaces are (1) conserved genre-aware initial stock or uncharted restock service for specialist demand, and (2) the non-retained emerging-project format-memory fork. Both require a new directive; no implementation was made here.

### Specialist fulfillment and emerging-project memory repair (2026-07-13)

The subsequently authorized repair is intentionally limited to the two diagnosed seams. It does not change acceptance texture, historical keyframes, supply weights, charts, finance, distance, release capacity, Album priors, format noise, or roster eligibility.

- At launch, Country/TexMex/Boogaloo now redistribute only a record's already-drawn regional stock across its existing regions using the fixed centered specialist texture. The integer allocation preserves the exact national launch-stock total, makes no additional random draw, and uses no realized demand, units, charts, stock, backorders, or annual result. Uncharted specialist records with a physical backorder may use the existing replenishment calculation without first clearing the broad-market breakout gate; this creates no demand and remains subject to the existing coverage, reach, and physical-capacity limits.
- A non-retained project in a catalog genre introduced in 1966 or later now uses its project-specific Single/Album priors at the format fork instead of a genre-agnostic label-wide format-memory EMA. Retained identities, earlier catalog genres, disabled execution, and prewarm retain the established memory route. The audit telemetry records this as `ProjectPrior` rather than `LabelFormat`.

Fixed probes pass for national-stock conservation, TexMex Southwest-over-Great-Plains allocation, specialist-only uncharted service, emerging-project memory bypass, retained/disabled neutrality, supply-selection boundaries, acceptance conservation, and the post-transfer Single-opportunity normalizer. The final current-code disabled replay, `d6-fulfillment-emerging-memory-52b-control-1001`, is byte-identical to `d5-specialist-opportunity-normalizer-52-control-1001` in all 45 CSV streams.

The enabled full-telemetry checkpoint, `d6-fulfillment-emerging-memory-52b-enabled-1001`, completed 52 weeks with total units `1.0318x`, gross `1.0361x`, label net `1.0320x`, market net `1.0402x`, Single units `1.0305x`, Album units `1.1337x`, and Album decision share `26.1922%` versus `25.2724%` control. TexMex fulfilled share is uniquely highest in Southwest (`.0543%`), ahead of Deep South (`.0327%`) and Great Plains (`.0275%`); Southwest TexMex backorders are zero in this short checkpoint. The known non-fatal `MissingSingletonsTemp.cs` autoload diagnostic remains the only runner diagnostic. No 520-week run, seed 1002, seed 1003, or holdout was launched from this repair.
