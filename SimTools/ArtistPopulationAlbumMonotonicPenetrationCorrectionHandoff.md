# Album Catalog Monotonic-Penetration Correction Handoff

Status: **TELEMETRY PATH RETIRED / ONE CAUSAL BEHAVIORAL CANDIDATE AUTHORIZED / SEQUENTIAL HARD STOPS**

Date: 2026-07-18

**Controlling resume amendment:** read
`SimTools/ArtistPopulationAlbumMonotonicPenetrationResumeAmendmentHandoff.md`
first. Its authorized current-state manifest and explicit instruction to
proceed supersede every obsolete pre-telemetry whole-file hash gate. Do not
execute this handoff from a cached copy that lacks that amendment.

This handoff supersedes the execution instructions in:

```text
SimTools/ArtistPopulationM5AlbumCatalogTelemetryReplayHandoff.md
SimTools/ArtistPopulationM5AlbumCatalogTelemetryCompatibilityAmendmentHandoff.md
```

Preserve those documents and all telemetry artifacts as historical evidence,
but do not continue their telemetry runs or analyzer ladder.

The owner has directed work to proceed from the evidence already available.
This handoff authorizes one narrow behavioral hypothesis and its validation
ladder. It does not authorize a constant sweep, a second behavioral candidate,
later seeds, or a holdout.

## 1. Evidence and decision

The preserved seed-1001 M5 candidate stopped after 469 completed ticks because
completed-1968 gross revenue was:

```text
candidate: 297,153,766.647076
control:   224,777,772.118624
ratio:     1.321989
```

The excess was Album-led:

```text
1968 candidate Album units/control:    1.5992x
1968 candidate Single units/control:   0.9179x
1968 candidate total units/control:    1.0806x
```

From 1967 to 1968, candidate total units rose only about `0.9%`, while Album
units rose about `41.8%` and Single units fell about `12.8%`. The physical
market was already at `99.38%` of base capacity. Raising capacity would clear
more excessive demand and cannot reduce gross.

The retained annual catalog summaries also show a materially larger and older
candidate Album catalog:

| Year | Candidate active Albums | Control active Albums | Candidate >52-week gross share | Control >52-week gross share |
|---:|---:|---:|---:|---:|
| 1967 | 8,314 | 6,370 | 0.411601 | 0.255310 |
| 1968 | 9,227 | 7,002 | 0.431699 | 0.205466 |

Current Album exhaustion uses:

```text
penetration = regional cumulative units / current-year buyer pool
exhaustion  = max(0.15, 1 / (1 + 4 * penetration))
```

The buyer pool grows through the Album-era transition. For an old title whose
cumulative units are unchanged, a larger current-year denominator can lower
calculated penetration and raise exhaustion headroom. The same catalog title
then benefits twice from market growth:

1. the larger buyer pool directly multiplies demand; and
2. the lower penetration raises its exhaustion multiplier.

This handoff treats that second effect as the most plausible correctable
catalog-rejuvenation seam.

## 2. Chosen correction

Implement **monotonic effective regional Album penetration** on the live
Genre-Market-V2 path.

For each Album and region, retain the greatest effective penetration previously
used:

```text
observedPenetration =
    regionalCumulativeUnitsBeforeSale / max(1, currentBuyerPool)

effectivePenetration =
    max(previousPeakEffectivePenetration, observedPenetration)

previousPeakEffectivePenetration =
    effectivePenetration

exhaustion =
    max(0.15, 1 / (1 + 4 * effectivePenetration))
```

The persistent state may be named:

```text
albumPeakEffectivePenetration
```

or a clearly equivalent name on `RegionalRecordData`.

This correction has no tunable scalar. It prevents cumulative market
penetration from moving backward merely because the era-level buyer pool
expanded. It does **not** stop current buyer-pool growth from directly
increasing Album opportunity. It therefore preserves the historical format
transition while removing the suspected double benefit.

## 3. Exact behavioral boundary

Apply the monotonic state only when both are true:

```text
GenreMarketV2.Enabled
ChartManager.Instance?.IsGenreMarketV2Live == true
```

When that live boundary is false:

- calculate penetration with the existing formula;
- do not read or update the new peak state;
- preserve the exact disabled/prewarm arithmetic and RNG schedule; and
- preserve every frozen disabled CSV byte.

The state begins at zero. The first live observation therefore uses the
existing observed penetration. It persists for the lifetime of that
Album-region runtime state, including ordinary label/project ownership changes.

Do not:

- freeze the buyer pool at release;
- add an age, year, genre, label, tier, or career-state branch;
- change the Album-era curve;
- change buyer-pool affinity or willingness;
- change base purchase rate;
- change the `0.15` exhaustion floor or the factor `4`;
- change catalog decay start or weekly decay;
- change retirement floors or tolerance;
- change Album scheduling or format choice;
- change price, cost, royalty, or finance formulas;
- change physical/common-market capacity or spillover limits;
- add a format reservation;
- add, remove, or reorder an RNG draw; or
- add diagnostic telemetry.

## 4. Retire the failed telemetry experiment before implementation

Preserve all telemetry handoffs, analyzer outputs, failed run families, and
reports. They remain historical evidence. Do not delete them.

The source now contains mixed-purpose post-M5 changes. Therefore **do not use
the old whole-file M5 hashes as a cleanup gate**. Those hashes predate both the
failed Album diagnostic and later valid fail-fast/annual-row corrections.
Requiring the old hashes would incorrectly delete valid work and is expressly
superseded by this section.

The previously reported mismatch is already classified:

```text
Systems/ChartManager.cs B434...A5916
    known Album diagnostic implementation

SimTools/ChartAuditRunner.cs 2E73...0F4C
    Album diagnostic implementation plus valid completed-year,
    annual-revenue-flush, revision-ordinal, and immutable-settlement fixes

SimTools/ArtistPopulationLifecycleProbeSuite.cs 25A3...2C9
    valid completed-year probe 64d plus telemetry-only formatting probe 65g
```

These known hashes are evidence of the mixed state, not an unknown source
divergence and not a reason to stop before semantic cleanup.

Remove the unaccepted Album catalog settlement diagnostic implementation
semantically before adding the behavioral candidate:

```text
Data/RegionalRecordData.cs
Systems/AlbumSimulator.cs
Systems/ChartManager.cs
SimTools/ChartAuditRunner.cs
```

Remove only:

- `album-catalog-settlement-diagnostic.csv` writer/open/header/write/flush/
  dispose state;
- `AlbumCatalogSettlementDiagnosticRegion`, `AlbumDiagnosticRegions`, and the
  causal Album diagnostic validation path on the immutable settlement;
- telemetry-only week-local `album*ThisWeek` fields and reset method on
  `RegionalRecordData`; and
- telemetry-only causal capture/refactoring in `AlbumSimulator`;
- `DiagnosticF` / `FormatDiagnosticCausalFloatForProbe` when they have no
  remaining non-diagnostic consumer; and
- telemetry-only probe `65g` for causal round-trip formatting.

Restore the pre-telemetry live arithmetic exactly before applying monotonic
penetration. Do not remove or rewrite unrelated M5 behavior.

Explicitly preserve these valid post-M5 changes:

```text
completed-year identity in CatastrophicAbortException and catastrophic CSV
FormatCompletedYearRatioState / completedYear fail-fast state
AdvanceMarketRevenueYear before a new-year fail-fast abort
format-memory revisionOrdinal header and row value
immutable settlement label-tier and genre snapshots
probe 64d completed-year fail-fast attribution
probe 64e birth-week protection
```

These changes are not part of the abandoned Album cohort diagnostic and must
not be reverted merely to reach an obsolete hash.

Retain the independently documented disabled-legacy settlement compatibility
amendment:

```text
687DA937F02724D13C3F2958E109DE84CE3F213475BE12D134BD22E2AA7160DD  Systems/CompetitorManager.cs
E8AFF4842C817E82D0F750DEA4ECF40A57DB1014C24AA95574E7FC19BF370A3E  SimTools/GenreMarketV2ProbeSuite.cs
```

That amendment preserves the live enabled formula and restores the frozen
disabled deal calculation. Do not undo it.

After cleanup, require a focused source search over
`Data/RegionalRecordData.cs`, `Systems/AlbumSimulator.cs`,
`Systems/ChartManager.cs`, `SimTools/ChartAuditRunner.cs`, and
`SimTools/ArtistPopulationLifecycleProbeSuite.cs` to find no remaining:

```text
albumCatalogSettlementDiagnostic
AlbumCatalogSettlementDiagnostic
AlbumDiagnosticRegions
albumSettlementObservation
albumBuyerPoolThisWeek
albumPenetrationThisWeek
albumExhaustionThisWeek
FormatDiagnosticCausalFloat
```

Equivalent abandoned diagnostic-only names must also be absent. Do not search
historical handoffs, analyzers, or preserved `SimLogs` artifacts for this gate.

Record a **new** SHA-256 manifest after semantic cleanup and again after the
monotonic-penetration implementation. The new manifest documents the actual
preserved source; it is not required to equal an obsolete pre-fix manifest.

Stop only if:

- an abandoned diagnostic source path remains;
- Album live arithmetic cannot be separated from telemetry capture;
- an unclassified change exists outside the explicit preserved list,
  disabled-compatibility amendment, or monotonic candidate; or
- cleanup requires guessing about behavior.

Known mixed-purpose changes listed above are not a reason to stop.

## 5. Allowed implementation files

After semantic telemetry retirement, behavioral changes are limited to:

```text
Data/RegionalRecordData.cs
Systems/AlbumSimulator.cs
SimTools/ArtistPopulationLifecycleProbeSuite.cs
```

`ArtistPopulationLifecycleAudit.md` may be updated only with completed rung
evidence. This handoff document may be amended only to correct an objective
command or manifest error.

No analyzer, telemetry writer, market-clearing, settlement, retirement,
release, finance, format-memory, or capacity source may change.

## 6. Fixed probes

Add one new D6 probe after the currently accepted probe 65. It must prove:

1. the first live observation uses observed penetration unchanged;
2. increasing current buyer pool with unchanged cumulative units cannot reduce
   effective penetration;
3. increasing the buyer pool cannot increase exhaustion through the
   penetration term;
4. increasing cumulative units can increase effective penetration;
5. effective penetration and its stored peak are monotonic nondecreasing;
6. exhaustion remains bounded below by the existing `0.15` floor;
7. the disabled/prewarm calculation remains the exact existing stateless
   formula;
8. disabled/prewarm evaluation does not mutate the peak state;
9. current buyer pool still directly multiplies Album raw demand;
10. no RNG draw is added, removed, or reordered; and
11. no Album settlement diagnostic stream or telemetry state remains.

Prefer a small internal deterministic helper that production and the probe call
identically. Do not duplicate a probe-only approximation of the formula.

Retain and pass all accepted D5 and D6 probes.

## 7. Candidate identity and run prefixes

Use this candidate name consistently:

```text
album-monotonic-penetration
```

Reserved prefixes:

```text
d6-album-monotonic-penetration-probes-1001
d6-album-monotonic-penetration-disabled-52-1001
d6-album-monotonic-penetration-enabled-104-1001
d6-album-monotonic-penetration-enabled-repeat-104-1001
d6-album-monotonic-penetration-through-1968-1001
d6-album-monotonic-penetration-decade-enabled-1001
```

Before each launch, require that its prefix has never been used. Never
overwrite or merge a run family.

Use the retained control:

```text
d6-transition-envelope-decade-control-1001
```

Use the frozen disabled baseline:

```text
d6-market-clearing-disabled-52-1001
```

## 8. M1 - build, diff, and fixed probes

Run:

```powershell
dotnet build "Label Man.sln" --no-restore
git diff --check

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=1 --run=d6-album-monotonic-penetration-probes-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --genre-market-v2-probes --artist-population-lifecycle-probes
```

Require:

- build success with no new warning;
- `git diff --check` success;
- the abandoned Album diagnostic symbol search in section 4 returns no source
  matches;
- the focused diff contains only the explicitly preserved post-M5 fixes, the
  disabled-legacy compatibility amendment, telemetry removal, and the
  monotonic-penetration candidate;
- a new post-candidate SHA-256 manifest is recorded for every modified
  functional/probe file;
- accepted D5 probes pass;
- D6 probes 1-65 pass;
- the new monotonic-penetration probe passes;
- no diagnostic telemetry stream is emitted; and
- no simulation beyond the one-week probe harness.

Stop on any failure.

## 9. M2 - disabled compatibility

Run:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-album-monotonic-penetration-disabled-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
```

Compare against `d6-market-clearing-disabled-52-1001` by suffix, length, and
SHA-256.

Require:

- exit zero and `CHART_AUDIT_COMPLETE ... weeks=52`;
- the same 45 frozen suffixes;
- all 45 files byte-identical;
- no missing or extra frozen stream; and
- no Album catalog diagnostic stream.

Stop on any failure. Do not run M3.

## 10. M3 - enabled 104-week candidate and repeat

Run two independent processes:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-album-monotonic-penetration-enabled-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle

& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=104 --run=d6-album-monotonic-penetration-enabled-repeat-104-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

Require:

- both exit zero with exactly 104 completed ticks;
- identical suffix sets;
- every suffix-matched CSV byte-identical between candidate and repeat;
- zero ownership, duplicate-membership, terminal-eligibility,
  premature-probation, booking, settlement, allocation, inventory,
  reconciliation, non-finite, and hard-capacity violations;
- annual successful releases in `[0.85,1.15]`;
- annual scheduled Albums in `[0.80,1.20]`;
- annual Single units in `[0.85,1.15]`;
- annual Album units in `[0.80,1.20]`;
- annual total units, gross, label net, and market net in `[0.85,1.15]`; and
- no inherited format, chart-duration, or lifecycle gate failure.

Use authoritative `market-revenue.csv`, `release-capacity.csv`,
`album-projects.csv`, and `decade-annual-rollup.csv`. Do not infer economics
from final active-record snapshots.

Stop on the first failed gate. Do not run M4.

## 11. M4 - completed-1968 target checkpoint

Only after M1-M3 pass, run:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=469 --run=d6-album-monotonic-penetration-through-1968-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Do not add catastrophic fail-fast or a gate-control switch. This rung must
complete normally so every annual row through 1968 is available.

Require:

- exit zero and `CHART_AUDIT_COMPLETE ... weeks=469`;
- exactly 469 `weeks.csv` data rows ending in 1968;
- complete annual rows for 1960-1968;
- annual successful releases in `[0.85,1.15]`;
- annual scheduled Albums in `[0.80,1.20]`;
- annual Single units in `[0.85,1.15]`;
- annual Album units in `[0.80,1.20]`;
- annual total units, gross, label net, and market net in `[0.85,1.15]`;
- 1966 Album units at least `0.80x` control;
- specifically, 1968 Album units no greater than `1.20x` control;
- specifically, 1968 gross, label net, and market net each no greater than
  `1.15x` control;
- no annual Single, Album, or total-unit ratio above its inherited upper band;
- zero weekly, annual, settlement, booking, spillover, allocation, inventory,
  ownership, lifecycle, finance, chronology, and non-finite violations;
- only configured one-hop spillover with the existing 15% import and 75% donor
  export caps;
- the `1.34` common-market capacity multiplier unchanged; and
- no operating-target or hard-capacity overshoot.

Report for every year:

- candidate/control Single, Album, total-unit, gross, label-net, and market-net
  ratios;
- successful releases and scheduled Albums;
- active Albums;
- Album gross over 26 and over 52 weeks;
- never-retired Album share;
- serviceable intent, cleared units, cleared/serviceable, cleared/base
  capacity, unused capacity, backorders, and residual displacement; and
- candidate year-over-year movement.

Causal confirmation requires the 1967-1968 Album/economic upper-band failures
to clear while the 1964-1966 Album floors remain intact. The existing annual
catalog summaries should move in the predicted direction: lower late-catalog
gross pressure or fewer active old Albums. Do not invent a new hard numeric
catalog-share target after seeing the output.

If any required annual gate fails, append the exact evidence to
`ArtistPopulationLifecycleAudit.md` and stop. Do not tune the formula, add an
age threshold, weaken another gate, or launch M5.

## 12. M5 - final seed-1001 decade acceptance

Only after M4 passes every requirement, run:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-album-monotonic-penetration-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Require all inherited M5 acceptance conditions:

- normal exit zero and `CHART_AUDIT_COMPLETE ... weeks=522`;
- complete annual rows for 1960-1969;
- header-only catastrophic stream;
- annual successful releases in `[0.85,1.15]`;
- annual scheduled Albums in `[0.80,1.20]`;
- annual Single units in `[0.85,1.15]`;
- annual Album units in `[0.80,1.20]`;
- annual total units, gross, label net, and market net in `[0.85,1.15]`;
- decade Single and Album units in `[0.85,1.15]`;
- decade total units, gross, label net, and market net in `[0.90,1.10]`;
- 1966 Album units at least `0.80x` control;
- 1969 scheduled-Album share inside inclusive `[0.78,0.85]`;
- 1969 scheduled-Album count at least `0.80x` control;
- no annual Single, Album, or total-unit ratio above its inherited upper band;
- paired all-decade closed Top-40 median movement within `+/-2` weeks;
- zero weekly, annual, settlement, booking, spillover, allocation, inventory,
  chronology, ownership, lifecycle, finance, operating-target, hard-capacity,
  and non-finite violations;
- the configured one-hop spillover and capacity constants unchanged; and
- no reintroduction of the retired Album telemetry stream.

Produce the full trend report required by
`ArtistPopulationM5TrendAdjudicationHandoff.md`. Passing aggregate decade
economics cannot cure an annual miss.

End with exactly one evidence-supported classification:

```text
HEALTHIER_BOUNDED_TRANSITION
CONTROL_RELATIVE_TROUGH_ONLY
CAPACITY_SATURATION
FORMAT_TRANSITION_LAG
MIXED_OR_UNRESOLVED
```

## 13. Terminal instruction

This handoff authorizes only the monotonic-penetration candidate at seed 1001.

If M1, M2, M3, M4, or M5 fails:

- preserve the complete or partial artifact;
- append the exact first failed gate and supporting annual evidence to
  `ArtistPopulationLifecycleAudit.md`;
- report whether the failure is an early Album-floor regression, unchanged
  late-catalog excess, another economic/format miss, structural violation, or
  execution failure; and
- stop.

Do not automatically:

- change the exhaustion floor or coefficient;
- add an age threshold;
- strengthen decay or retirement;
- change buyer-pool timing;
- raise capacity;
- reduce Album price or yield globally;
- run another scalar or behavioral candidate;
- run seeds 1002/1003;
- select a holdout; or
- widen an acceptance band.

If M5 passes, append the full implementation, manifest, M1-M5 evidence, annual
and decade tables, and final classification to
`ArtistPopulationLifecycleAudit.md`, then stop. Later-seed confirmation requires
a new owner handoff.
