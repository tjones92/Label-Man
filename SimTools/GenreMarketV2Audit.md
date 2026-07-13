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
