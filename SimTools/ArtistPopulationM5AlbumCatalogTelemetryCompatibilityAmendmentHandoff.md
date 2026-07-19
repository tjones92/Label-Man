# M5 Album Catalog Telemetry Compatibility Amendment and Resume Handoff

Status: **AMENDMENT IMPLEMENTED / POST-AMENDMENT M1 PASS / RESUME AT FRESH M2**

Date: 2026-07-18

This handoff supersedes
`ArtistPopulationM5AlbumCatalogTelemetryReplayHandoff.md` only where explicitly
stated. Its M3-M5 economic, telemetry, reconciliation, analysis, and stop gates
otherwise remain authoritative.

The owner authorized:

1. the narrow disabled-compatibility amendment below;
2. completion of the already-authorized telemetry-aware cohort analyzer;
3. a fresh M2 replay; and
4. continuation through M3-M5 only when each preceding rung passes.

Do not overwrite, delete, rename, or treat as acceptance evidence the failed
first M2 family:

```text
d6-album-catalog-telemetry-disabled-52-1001
```

## 1. Root cause of the first M2 failure

The failure was not caused by the Album diagnostic stream or cohort analyzer.
M2 performs direct suffix, length, and SHA-256 comparisons and invokes no
analyzer.

The first causal divergence occurred after the week-9 distribution agreement
between `label_0093` and distributor `label_0025`:

```text
week 10 frozen distributor income: 161.5599
week 10 failed-replay distributor income: 34.99345
difference retained by client: 126.56645
```

Album units and gross were still identical at that point, and total market net
was unchanged apart from floating representation. The different client and
distributor cash balances then changed later economic decisions. The first
rounded `weeks.csv.totalMarketUnits` difference appeared at week 30, followed
by weeks 40 and 52, and propagated into 22 streams.

`CompetitorManager.CalculateLabelRevenue` had begun using granted-region deal
scoping for every settlement. That is correct for live enabled settlements,
but the disabled route creates a legacy settlement with identity `-1` and must
retain the frozen formula:

```text
active deal: clamp(marginSkim, 0, 1)
no deal:     0.25 * (1 - ownedReach)
```

The pre-amendment `Systems/CompetitorManager.cs` hash was
`BA6B3039615C0A25481B3FAB79DB029E579DCAAC87901D17F21C235599621349`,
exactly the frozen M5 hash. Therefore this incompatibility predated the Album
telemetry edit and had not been exposed by a disabled replay after the
granted-region correction.

## 2. Implemented amendment

The owner-authorized file-boundary expansion is limited to:

```text
Systems/CompetitorManager.cs
SimTools/GenreMarketV2ProbeSuite.cs
SimTools/analyze-m5-album-catalog-cohorts.mjs
SimTools/ArtistPopulationM5AlbumCatalogTelemetryReplayHandoff.md
SimTools/ArtistPopulationM5AlbumCatalogTelemetryCompatibilityAmendmentHandoff.md
```

`CalculateLabelRevenue` now identifies a live settlement only when both:

```text
GenreMarketV2.Enabled
settlement.SettlementId > 0
```

Live settlements continue to scope a deal margin to granted-region units.
Disabled legacy settlements use the exact frozen full-deal-margin/no-deal
formula. No RNG, demand, inventory, capacity, release, chart, retirement,
memory, format-choice, or enabled settlement order changed.

The fixed D5 probe now proves all four cases:

```text
live deal          -> margin * granted-unit share
disabled deal      -> full frozen deal margin
live no-deal       -> frozen owned-reach formula
disabled no-deal   -> frozen owned-reach formula
```

The cohort analyzer is now prefix-driven and reads
`album-catalog-settlement-diagnostic.csv` as the authoritative immutable age
and causal join. It supports:

```text
--telemetry-validation
```

It performs lock-step key coverage against Album regional settlement rows,
entry-level immutable-field agreement, causal arithmetic identities, integer
serviceable/cleared reconciliation, exact-raw float reconciliation, annual
economic reconciliation, all required cohort distributions/shares, the eight
final questions, and the required terminal classification.

The exact-raw tolerance is documented and enforced per week-region as:

```text
max(0.25 units, 5 ppm of market-clearing rawAlbumDemand)
```

Serviceable and cleared reconciliation remains exact integer equality.

## 3. Completed post-amendment M1 evidence

`git diff --check` passed.

```text
dotnet build "Label Man.sln" --no-restore
```

passed with zero errors and only the inherited unused
`ChartManager.OnGenreMomentumChanged` warning.

The one-week fixed harness:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-album-catalog-telemetry-compat-amendment-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes
```

exited zero with:

```text
D5_PROBE_PASS
D6_PROBE_PASS: D6 fixed probes 1-65 passed
CHART_AUDIT_COMPLETE run=d6-album-catalog-telemetry-compat-amendment-probes-1001 weeks=1
```

The known post-completion `MissingSingletonsTemp.cs` autoload diagnostic and
ObjectDB leak warning remain non-fatal.

The analyzer passes Node syntax validation. A runtime smoke against the
one-week probe family correctly returns `TELEMETRY_VALIDATION_FAIL` because
that harness contains no Album settlement rows or completed annual Album row;
that is an expected negative smoke, not M3 evidence.

## 4. Frozen resume-source manifest

Before M2, verify every value exactly and run `git diff --check`. Stop on any
mismatch; do not recreate source from this document.

```text
05C36AA077580176BB9380D005C9BADC493FBABDA9945F510DE285EA9F853412  Data/AILabel.cs
ACE17C624A8CBA3C1CFEC900B781143E27881576F2D07821E1B2F7155E388ED3  Data/AlbumProject.cs
4954724F386F2C08506F8A86EF2E7E7242CAAEABF0B0A056CC9C2DC55F77DB8A  Data/RegionalRecordData.cs
DF6F5B01494314C3D55A6CADB206777B342725B4C5FA055E37057F3D8D800957  Systems/AlbumModel.cs
B7162551D3958CE04444F90AC6F1FC1B89145207AC3788EE72695CC3DB5E09F8  Systems/AlbumSimulator.cs
B434D8507AF7DE80DCCA76FF8BD12F12D86B3F951EDC70A64AF8C1FB913A5916  Systems/ChartManager.cs
687DA937F02724D13C3F2958E109DE84CE3F213475BE12D134BD22E2AA7160DD  Systems/CompetitorManager.cs
D2BFA31FA5894C48EBA65AB7467B7714B4EC24B317342FB9EF97050BD5BBA70E  Systems/DistanceModel.cs
4243109AD85E57C8896B00D01D0E3682BD5ECA2FAF5254EB0CA41F2AA3C90431  SimTools/ChartAuditRunner.cs
153B9764334951BA97D94152F756D45D801B2402D1A16755DA23AEA1AE7867F8  SimTools/ArtistPopulationLifecycleProbeSuite.cs
E8AFF4842C817E82D0F750DEA4ECF40A57DB1014C24AA95574E7FC19BF370A3E  SimTools/GenreMarketV2ProbeSuite.cs
05E9FDA23863D8EE2291BD25A4883F011D569CA22D59A32EFFEB9543C4AA2A64  SimTools/analyze-m5-album-catalog-cohorts.mjs
```

Preserve all unrelated dirty-tree, `.uid`, handoff, audit, and ignored
`SimLogs` artifacts. Do not restore the deliberately deleted
`SimTools/analyze-market-clearing-format-memory.mjs`.

## 5. M2 - fresh disabled compatibility replay

Use a new prefix because the first M2 family is retained evidence:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-album-catalog-telemetry-disabled-52-1001-r2 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
```

Compare by suffix, byte length, and SHA-256 with:

```text
d6-market-clearing-disabled-52-1001
```

Require all of:

- process exit zero;
- `CHART_AUDIT_COMPLETE ... weeks=52`;
- exactly the same 45 frozen suffixes;
- all 45 suffix-matched files byte-identical;
- no missing or extra frozen stream; and
- no `album-catalog-settlement-diagnostic.csv`.

If any hash still differs, stop and report the first line-level divergence. Do
not run M3 and do not replace the frozen baseline.

## 6. M3 - 104-week telemetry checkpoint

Only after M2 passes:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-album-catalog-telemetry-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Do not add catastrophic fail-fast or a gate-control switch.

Require the complete M3 gate in
`ArtistPopulationM5AlbumCatalogTelemetryReplayHandoff.md`, including:

- exit zero and exactly 104 completed ticks;
- zero booking/audit, clearing, spillover, allocation, inventory, ownership,
  lifecycle, duplicate-key, stale-observation, and non-finite violations;
- one diagnostic row for every Album regional settlement row and no Single
  diagnostic row;
- exact integer serviceable and cleared reconciliation;
- exact-raw reconciliation inside the documented tolerance;
- nonzero `NEW`, `MID`, and `CATALOG` observations;
- retirement-predicate agreement; and
- reproduction of the preserved M5 candidate's established 1960-1961 economic,
  release, and format values.

Run telemetry validation:

```powershell
$node = 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
& $node --max-old-space-size=8192 SimTools/analyze-m5-album-catalog-cohorts.mjs d6-album-catalog-telemetry-enabled-104-1001 --telemetry-validation
```

Require exit zero, `TELEMETRY_VALIDATION_PASS`, and companion CSV/JSON/Markdown
outputs under the M3 prefix.

If the 104-week run prefix already exists before execution, do not overwrite
it. Stop and choose an owner-approved unique replacement prefix consistently
for the simulation and analyzer.

## 7. M4 - replay through completed 1968

Only after every M3 requirement passes:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=469 --run=d6-album-catalog-telemetry-through-1968-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Do not use `--catastrophic-fail-fast` or `--gate-control-run`.

Require:

- exit zero and `CHART_AUDIT_COMPLETE ... weeks=469`;
- exactly 469 `weeks.csv` data rows, ending in 1968;
- no week-470/1969 row in the analysis population;
- continuation of every M3 telemetry and structural invariant;
- exact diagnostic coverage for every 1967-1968 Album regional settlement;
- reproduction of established 1960-1968 economic, release, format, unit, gross,
  label-net, and market-net values from the preserved M5 candidate; and
- no pre-existing gameplay/economic stream change except a documented
  completion-boundary difference caused solely by omitting fail-fast.

Any behavioral difference is a hard stop. Do not tune around it.

## 8. M5 - cohort adjudication

Only after M4 passes:

```powershell
& $node --max-old-space-size=8192 SimTools/analyze-m5-album-catalog-cohorts.mjs d6-album-catalog-telemetry-through-1968-1001 d6-transition-envelope-decade-control-1001
```

Require exit zero and preserve:

```text
d6-album-catalog-telemetry-through-1968-1001-album-catalog-cohort-analysis.csv
d6-album-catalog-telemetry-through-1968-1001-album-catalog-cohort-analysis.json
d6-album-catalog-telemetry-through-1968-1001-album-catalog-cohort-analysis.md
```

The output must pass every immutable-key, causal, settlement, clearing, annual
economic, and age-coverage reconciliation and answer all eight questions. It
must end with exactly one:

```text
EXISTING_DATA_SUFFICIENT_FOR_CORRECTION_SURFACE
EXISTING_DATA_CONFIRMS_CATALOG_EXCESS_BUT_NOT_MECHANISM
EXISTING_DATA_INSUFFICIENT
```

Append a concise result to `ArtistPopulationLifecycleAudit.md` only after every
required reconciliation passes.

## 9. Final stop boundary

Follow the original handoff's decision boundary exactly:

- if sufficient, report the supported narrow correction surface and stop;
- if mechanism remains unresolved, report the ambiguity and stop; or
- if insufficient, report the exact missing evidence and stop.

Do not implement a behavioral correction, run 522 weeks, run another seed,
launch a holdout, replace the control, weaken a gate, or add another telemetry
round under this handoff.
