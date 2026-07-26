# Economic/lifecycle genre-and-label holdout handoff

Status: **READY / HOLDOUT PRESPECIFIED / DO NOT RUN IN THIS TASK**

## Purpose

Run exactly one untouched-seed decade holdout from the accepted
founder-emergence implementation, while evaluating three independent surfaces:

1. the economic, format, release, and reconciliation gates already applied to
   seeds 1001 and 1002;
2. annual genre emergence, commercial peaks, regional fulfillment, and
   historical succession, with an explicit Psychedelic Rock review;
3. runtime-founded label survival, operating-target growth, release
   participation, tier mobility, promotion, demotion, and competitive exit.

This handoff authorizes the future run. It does not authorize tuning before or
after the holdout, replacing an unfavorable seed, or rerunning seed 2007.

## Frozen implementation and completed evidence

The runtime implementation is anchored at:

```text
fb137d7bc7a24b508d07b35e46ff1ff220e46fbd
```

That commit contains the founder-emergence repair and the evidence-gated
MidTier-promotion / competitive-exit probes. At handoff creation:

- the worktree was clean before this handoff file was added;
- `dotnet build "Label Man.sln" --no-restore` had already passed with zero
  errors and the one pre-existing unused-event warning;
- seed 1001 and seed 1002 had completed all 522 requested weeks;
- both runs emitted header-only `catastrophic-fail-fast.csv` files;
- the captured seed-1002 console log contained the required D5 and D6 probe
  passes;
- 1969 scheduled-Album ratios were `0.743398` and `0.734118`;
- 1969 Album decision shares were `0.749283` and `0.749546`;
- no further runtime source repair was required.

Accepted measurement artifacts:

```text
SimLogs/d6-economic-lifecycle-founder-emergence-decade-1001-*
SimLogs/d6-economic-lifecycle-founder-emergence-decade-1002-*
```

Fixed economic gate control:

```text
d6-transition-envelope-decade-control-1001
```

The seed-1001 control is intentionally reused for all catastrophic and strict
acceptance checks. Do not substitute a newly generated or same-seed control in
this holdout.

## Prespecified holdout seed and prefix

Use seed **2007** exactly once:

```text
d6-economic-lifecycle-genre-label-holdout-2007
```

Seed 2007 and this prefix were selected before inspecting any seed-2007 result.
At selection time, the repository had no seed-2007 simulation artifact and no
prior written seed-2007 result. Seeds 2001-2006 had already appeared in earlier
project phases, so they were not selected.

If any file with the prescribed prefix exists before launch, stop. Do not
delete it, overwrite it, choose another prefix, or replace the seed. Report the
collision for adjudication.

## Important known genre result

The founder-emergence implementation has **not** already established that the
Psychedelic Rock timing problem is fixed. Evidence-only aggregation of the
accepted 1001/1002 artifacts gives:

| Seed | Year | Supply selections | Mean routed acceptance | Mean eligible | Mean charted | Fulfilled units | Backorders |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1966 | 17 | 0.678645 | 12.19 | 0.00 | 121,963 | 75,529 |
| 1001 | 1967 | 31 | 0.816886 | 31.04 | 0.06 | 126,343 | 29,315 |
| 1001 | 1968 | 53 | 0.831780 | 62.44 | 0.19 | 283,639 | 115,324 |
| 1001 | 1969 | 53 | 0.832586 | 95.02 | 0.27 | **414,896** | 99,242 |
| 1002 | 1966 | 9 | 0.734648 | 6.77 | 0.12 | 168,550 | 66,933 |
| 1002 | 1967 | 38 | **0.895437** | 23.12 | 0.23 | 241,844 | 81,057 |
| 1002 | 1968 | 48 | 0.869699 | 59.60 | 0.46 | 186,794 | 31,246 |
| 1002 | 1969 | 52 | 0.832510 | 94.17 | 0.12 | **338,244** | 48,054 |

These fulfilled-unit totals come from `geography-metrics.csv`; they are not
pre-fulfillment demand and do not double-count segment rows. The partial 1970
tail created by the 522-week runner is excluded.

Both accepted measurement seeds therefore still have a 1969 commercial peak.
Seed 1002's routed acceptance peaks in the intended 1967 window, showing that
late fulfilled yield/catalog accumulation can still move the commercial peak
away from the authored acceptance peak. Seed 1001 is more concerning because
its routed acceptance also continues upward through 1969.

The holdout is consequently a generalization and causal-shape check:

- another 1969 peak confirms a persistent cross-seed genre defect;
- a 1967 or 1968 peak shows seed variance, but cannot retroactively make the
  already observed 1001/1002 defect fixed;
- no holdout result authorizes post-holdout tuning.

## Preflight

Use the Downloads Godot executable outside the sandbox:

```powershell
$control = 'd6-transition-envelope-decade-control-1001'
$run = 'd6-economic-lifecycle-genre-label-holdout-2007'
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$node = 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
```

Before launch:

```powershell
git diff --check
dotnet build "Label Man.sln" --no-restore
git status --short
git rev-parse HEAD
$sourceState = git diff --binary | git hash-object --stdin
Test-Path $godot
Get-ChildItem SimLogs -File -Filter "$run-*"
```

Requirements:

- the build has zero errors;
- the Godot executable exists;
- the only expected worktree difference is this handoff if it has not yet been
  committed;
- no runtime source, scene, data, analyzer, or configuration file differs from
  the accepted implementation;
- no file with the holdout prefix exists;
- at least 8 GiB of free disk is available for the lean decade telemetry and
  captured console logs.

Record `$sourceState` before launch. Require the exact same value after the
process exits. The handoff file may be committed first; the controlling
requirement is that the recorded state cannot change during the holdout.

Do not run an extra probe, short replay, control, or seed-2007 preflight
simulation. The fixed D5 and D6 probes are intentionally part of the one
authorized command and its accepted RNG path.

## The one authorized simulation

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=$run `
  --seed=2007 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --lean-probe `
  --profile-performance `
  --catastrophic-fail-fast `
  --strict-1965-acceptance-gate `
  --gate-control-run=$control
```

For nonblocking monitoring, the same arguments may be launched with
`Start-Process -WindowStyle Hidden -PassThru` and explicit stdout/stderr files:

```powershell
$stdout = Join-Path (Get-Location) "SimLogs\$run-console.log"
$stderr = Join-Path (Get-Location) "SimLogs\$run-console.err"
$arguments = @(
  '--headless', '--path', (Get-Location).Path,
  'SimTools/ChartAuditRunner.tscn', '--',
  '--weeks=522', "--run=$run", '--seed=2007',
  '--enable-genre-market-v2',
  '--enable-artist-population-lifecycle',
  '--genre-market-v2-probes',
  '--artist-population-lifecycle-probes',
  '--lean-probe',
  '--profile-performance',
  '--catastrophic-fail-fast',
  '--strict-1965-acceptance-gate',
  "--gate-control-run=$control"
)
$process = Start-Process -FilePath $godot -ArgumentList $arguments `
  -RedirectStandardOutput $stdout -RedirectStandardError $stderr `
  -WindowStyle Hidden -PassThru
$process.Id
```

Poll without altering any artifact. Useful read-only checks are:

```powershell
Get-Process -Id $process.Id -ErrorAction SilentlyContinue
Get-Content $stdout -Tail 30
Get-Content $stderr -Tail 30
(Get-Content "SimLogs\$run-weeks.csv" | Measure-Object -Line).Lines
Get-Content "SimLogs\$run-catastrophic-fail-fast.csv"
```

Do not terminate a healthy slow run. Stop only on a genuine simulator
exception, a fail-fast rejection, loss of disk capacity, user interruption, or
another condition that makes continued output unsafe. Do not rerun after any
failure.

## Completion and economic hard gates

Require all of the following:

- process exit code zero;
- `CHART_AUDIT_COMPLETE` in captured stdout;
- all 522 requested week rows, with completed annual rows for 1960-1969;
- a header-only `catastrophic-fail-fast.csv`;
- both D5 probe groups and D6 fixed probes 1-69 pass;
- the completed-1965 strict acceptance gate passes;
- every completed-year catastrophic control ratio is inside inclusive
  `[0.70, 1.30]`;
- all annual values are finite;
- exact format-unit and market-net reconciliation;
- the single-lane/hit-tail analyzer passes;
- `$sourceState` is unchanged.

The 1969 scheduled-Album ratio should remain in the measurement neighborhood
(`0.734118-0.743398`) and the Album decision share near
`0.749283-0.749546`. Those narrow measurement envelopes are diagnostic, not
new fail-fast bands. Apply the existing hard bands unchanged and investigate a
material holdout departure without retuning.

After a normally completed run:

```powershell
& $node SimTools/analyze-single-lane-hit-tail.mjs SimLogs $run `
  --control-prefix=$control `
  --json="SimLogs/$run-single-yield.json"
```

Report annually and for the full decade:

- successful releases and participating labels;
- scheduled Album projects, completed Album drops, and control ratios;
- Album decision share;
- Single, Album, and total units;
- gross, label net, distribution income, and market net;
- Single units per Single release;
- Album units per completed drop, explicitly labelled as a
  market-year/catalog-carryover proxy;
- tier and label-origin results;
- minimum annual scheduled-Album ratio, with 1963, 1968, and 1969 explicit.

An economic pass does not override a genre-shape or label-evolution failure.
Report the three surfaces separately before giving an overall disposition.

## Genre-evolution analysis contract

Use immutable output only. Do not change telemetry or rerun to obtain a more
favorable decomposition.

Primary sources:

- `genre-market-weekly.csv`: pre-fulfillment routed acceptance, eligible and
  charted counts, and radio;
- `geography-metrics.csv`: fulfilled units and surviving backorders by genre,
  region, label tier, and destination tier;
- `supply-selections.csv`: selected project genre and artist identity;
- `release-strategy.csv`: realized release decisions, format, primary/secondary
  identity, orphan/promo/standalone strategy, and label tier;
- `album-projects.csv`: scheduled/drop timing, terminal state, and transfer;
- `format-decision-cohort-details.csv`: run-end realized units by decision-year
  cohort; label this as cohort/lifetime evidence, not annual commercial units;
- `genre-catalog.csv` and `genre-events.csv`: authored emergence and event
  timing.

For every canonical genre and year 1960-1969, calculate:

- supply selections;
- release decisions split by Single/Album and strategy;
- mean routed acceptance;
- mean eligible and charted records;
- fulfilled units, backorders, and backorder rate;
- share of annual fulfilled market units;
- first fulfilled year and commercial peak year;
- results by label tier and by region where material.

Do not sum overlapping segment rows from `genre-market-weekly.csv`.
`AllSegments` is the non-overlapping row. For national weekly means, average
the seven regional `AllSegments` rows; for fulfilled commercial units, sum
`geography-metrics.csv`.

Historical-shape checks:

- no commercial units before a genre's authored availability window unless
  explicitly retained catalog is allowed;
- Doo-Wop declines materially after its early-decade strength;
- Surf Rock peaks around 1963-1964 and then declines;
- Folk peaks near the middle of the decade and then yields share;
- British Pop/British Beat onset is visible in 1964;
- Psychedelic Rock begins in 1966 and should peak commercially around
  1967-1968, not uniquely in 1969;
- Hard Rock, Blues Rock, Funk, Acid Rock, Proto-Metal, Progressive Rock, and
  Singer-Songwriter emerge and strengthen in a plausible staggered sequence;
- no genre exceeds the existing 35% annual fulfilled-unit share cap;
- no one-year cliff or surge is accepted solely because the decade economy
  reconciles.

For Psychedelic Rock, produce the exact 1966-1969 table used above:

- supply selections;
- mean routed acceptance;
- mean eligible and charted records;
- fulfilled units and backorders;
- units per supplied project;
- Single/Album and orphan/promo/standalone split;
- annual cohort/lifetime units;
- pre-year carry-in catalog count and contribution where recoverable;
- highest-yield release cohorts and their raw primary/secondary identities.

Classify the first divergence:

1. authored acceptance/keyframe;
2. supply selection or compatibility;
3. format-memory fork / Single-Album choice;
4. scheduling or completed-drop delay;
5. catalog carry-in;
6. fulfillment/backorders;
7. isolated record-yield outlier.

Do not infer causality merely from the peak year. If lean telemetry cannot
resolve a record-level seam, state the limit and stop at the narrowest supported
aggregate seam.

## Emergent-label evolution analysis contract

The accepted measurement reference is:

| 1969 metric | Seed 1001 | Seed 1002 |
| --- | ---: | ---: |
| Mean active labels | 261.06 | 268.23 |
| End active labels | 251 | 265 |
| Mean active runtime founders | 141.58 | 156.60 |
| End active runtime founders | 138 | 157 |
| All participating labels | 265 | 276 |
| Runtime-founded participants | 141 | 162 |
| Runtime-founded decisions | 554 | 582 |
| Decisions per runtime participant | 3.93 | 3.59 |
| Successful releases | 2,777 | 2,748 |
| Capacity success rate | 99.32% | 99.21% |
| Runtime organic-growth events, decade | 564 | 560 |
| Runtime founders ever promoted above birth tier | 1 | 0 |

The promotion count is deliberately rare. Zero promotions in the holdout is
not by itself a hard failure because the fixed probes establish the mechanic;
promotion must be evidence-gated rather than forced. A runaway promotion wave,
unearned MidTier population, or tier oscillation is a failure.

Run the existing lifecycle helper, clearly labelling its reference comparison
as cross-seed:

```powershell
& $node SimTools/analyze-label-survival-participation.mjs `
  d6-economic-lifecycle-founder-emergence-decade-1001 `
  $run --year 1969 `
  > "SimLogs/$run-label-survival-participation.md"
```

Also report annual 1960-1969:

- launch-population and runtime-founded births, closures, mean/start/end active
  labels, and participating labels;
- closure status and age at closure;
- operating-target distribution for active runtime founders at each year end;
- organic-growth event counts and transitions `1->2`, `2->3`, and `3+`;
- months from birth to first growth, first release, first chart, first
  profitable month, `Dying`, closure, and promotion where observable;
- release decisions and successful releases by label origin and tier;
- decisions per mean active label and per participant;
- tier at birth, highest tier, ending tier, promotion/demotion counts, and
  oscillations;
- promotion evidence: age, profitability, recent charting, recent releases,
  cash runway, and roster/operating target at transition;
- competitive exits by tier, origin, status, profitability, distress duration,
  and recent-chart safe harbor;
- finite finance, distribution-deal reconciliation, and no resurrection from
  `Bankrupt`, `Defunct`, or `Acquired`.

Structural expectations:

- founders bootstrap at operating target one;
- growth occurs one slot per eligible quarterly review;
- the bounded emergence path reaches targets two and three in nonzero numbers;
- growth through the three-lane floor may use recent releases and adequate
  runway, while growth beyond three retains the stricter established-label
  evidence;
- runtime births remain 72 per full mature year (1961-1969);
- organic growth is distributed across the decade rather than confined to the
  final year;
- runtime founders have nonzero 1969 survival and release participation;
- release participation is not concentrated entirely in launch labels;
- added labels do not cause economic upper-band, Album-share, or throughput
  failure;
- closure remains possible and competitive exits do not erase healthy,
  recently charting labels;
- promotion is rare, evidence-backed, and never a mechanical quota.

The numerical 1001/1002 envelope is a diagnostic reference, not a new
same-seed hard band. A holdout outside it requires explanation and causal
breakdown. It must not be tuned back into the envelope.

## Final disposition vocabulary

Use one of these outcomes for each surface:

- **PASS**: all binding checks pass and the behavior is consistent with the
  accepted intent;
- **PASS WITH WATCH**: binding checks pass, but a report-only measurement is
  outside the 1001/1002 neighborhood without a demonstrated inconsistency;
- **FAIL**: a binding gate, reconciliation, chronology, status, historical
  shape, or lifecycle invariant fails;
- **INCONCLUSIVE**: the run completes, but lean telemetry cannot resolve the
  requested causal seam.

The overall result is the worst of economy, genre evolution, and label
evolution. In particular, another 1969 Psychedelic commercial peak is a genre
failure even when the economy passes.

Preserve every artifact after the one run. Do not patch, tune, change a gate,
replace the seed, or rerun. Write the final holdout audit from the immutable
seed-2007 output and the accepted 1001/1002 references.
