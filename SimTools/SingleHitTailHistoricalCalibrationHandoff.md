# Single hit-tail historical calibration and jackpot guard - Codex handoff

## Status and authority

Date: 2026-07-22.

This handoff supersedes the proposed cashflow-neutral redistribution mandate. The
current evidence does **not** justify changing simulation behavior merely to force
every annual Single lane below a `40%` top-decile share.

The next Codex is authorized to amend the focused analyzer, re-adjudicate the
preserved successful 469-week run, and run one exact current-code repeat after the
amended analyzer passes. This handoff does not authorize demand redistribution, a
conversion change, a sales cap, a finance correction, or any other gameplay
change. If a future run demonstrates a real jackpot pathology under the rules
below, preserve it and return a measured repair handoff before changing behavior.

Retained candidate:

```text
d6-single-lane-pressure-workload-through-1968-1001
```

Decade control:

```text
d6-transition-envelope-decade-control-1001
```

The retained candidate completed all 469 weeks with exit code `0`, passed the
strict 1965 gate, emitted a header-only catastrophic stream, passed all D5 and D6
probes, and retained a byte-identical disabled replay. Its historical focused
report is:

```text
SimLogs/d6-single-lane-pressure-workload-through-1968-1001-analysis.json
```

Its `FAIL` status is caused only by eleven applications of the unsupported
`top10Share <= .40` rule. Do not overwrite or relabel that report.

## 1. Historical interpretation

The metric means “the highest-yielding 10% of releases captured X% of first-14-week
units.” It does not mean that 40% of releases are in the top 10%.

The available evidence establishes a highly selective 1960s Singles market but
does not provide directly comparable first-14-week sales microdata:

- Billboard reviewed `5,797` Singles in 1960, `6,036` in 1961, and `6,690` in
  1962. See the 1963-64 *Billboard International Music-Record Directory*, page 18:
  <https://device.report/m/2c5f2b887d781390d09827e54f62cf75e67e290d1e060cb96fe613af84e3fa38>.
- The contemporary rule of thumb was that nine out of ten Singles failed to make
  the Pop Fifty. Richard Osborne traces the “one in ten” claim here:
  <https://repository.mdx.ac.uk/download/b61e93c61442e786c576d18db55c458e05562221b8712b6e95506ff465bc2209/176623/Richard%20Osborne-I_Am_a_One_in_Ten.pdf>.
- Historical Billboard trajectories had shorter-lived leading hits than the
  modern chart, but conspicuous hit dominance still existed:
  <https://arxiv.org/abs/2405.07574>.

Inference, not measured fact: if the successful decile averaged ten to twenty
times a miss's units, the all-release top decile would capture about `53-69%` of
units. Use **about 60%, with a broad 50-70% plausible range**, as a working
historical expectation for all Singles. Do not encode it as a hard acceptance
band without a direct sales dataset.

At `40%`, an average top-decile release sells six times an average bottom-90%
release. At `48%`, that ratio is about `8.3x`. Forcing `48%` down to `40%` makes
the market materially more egalitarian and likely moves away from the available
historical inference.

OrphanSingle and PromoSingle are simulator subpopulations, not historical market
categories. PromoSingles are deliberately selected album tracks. Historical
all-release evidence must therefore be compared first with a combined nonlegacy
Single distribution, not imposed independently on each lane.

## 2. Current result: no jackpot pathology

The retained run's late PromoSingle tail is spread across an upper cohort rather
than dominated by one absurd release:

| Year | Top 10% | Top 1% | Largest release | p99 / median |
| ---: | ---: | ---: | ---: | ---: |
| 1966 | 43.57% | 12.95% | 1.24% | 13.23x |
| 1967 | 47.55% | 14.30% | 2.27% | 16.64x |
| 1968 | 47.83% | 12.53% | 2.80% | 19.01x |

Across every completed year and both lanes, the maximum top-10% share is `47.83%`,
maximum top-1% share is `14.30%`, maximum single-release lane-year share is
`2.80%`, and maximum p99/median ratio is `19.01x`. Every existing
`top1Share <= .35` rule passes comfortably.

This is not evidence that synthetic jackpots are overtaking the market. Direct
tail-shape guards remain useful protection against future regressions, but general
upper-decile flattening is not justified now.

## 3. Analyzer amendment

Amend `SimTools/analyze-single-lane-hit-tail.mjs`. Do not change any simulation
source, telemetry producer, CSV, RNG path, flag, or gameplay constant.

### 3.1 Retire only the unsupported failure

Remove the annual lane failure `top10Share > .40`. Continue calculating and
reporting top-10% share exactly as today. Do not replace `.40` with `.50`, `.60`,
or `.70` as another hard lane ceiling.

### 3.2 Add combined-Single distributions

Keep annual OrphanSingle and PromoSingle distributions. Add a combined completed
cohort-year distribution containing all mature nonlegacy Singles:

```text
OrphanSingle + PromoSingle
```

Report these fields for each lane and combined distribution:

```text
count, units, mean, median, p90, p99, maximum,
p99Median, top10Share, top1Share, gini,
largestReleaseShare, maximumToP99
```

Use combined Singles for historical comparison. Lane distributions remain
mechanism diagnostics.

### 3.3 Direct jackpot acceptance

For each completed lane-year and combined-Single year with at least 200 releases,
apply these simulator-safety guards:

```text
largestReleaseShare <= .10
top1Share <= .35
```

The top-1% ceiling is retained. The new largest-release guard directly answers
whether one synthetic jackpot owns a material annual market share. They are broad
catastrophic limits, not estimates of normal historical concentration.

Emit non-failing historical warnings when:

```text
top10Share < .40 or top10Share > .70
p99Median > 30
maximumToP99 > 5
```

Warnings must not change status. They mark a surface for review without inventing
a historical calibration fact.

Fail closed on every existing join, lane, memory, settlement, market-clearing,
normalizer, nonfinite, finance-posting, annual compatibility, and raw-demand
reconstruction violation. No structural invariant may be weakened.

The JSON must explicitly separate `structuralFailures`, `jackpotFailures`, and
`historicalWarnings`. Overall status is `FAIL` if either failure collection is
nonempty; warnings alone do not fail.

## 4. Analyzer checks

Add deterministic fixtures or unit-level checks proving:

1. a normal heavy tail with top10 `0.48`, top1 below `.35`, and largest release
   below `.10` passes;
2. one release above `.10` fails even if top1 remains below `.35`;
3. top1 above `.35` fails;
4. top10 above `.40` alone does not fail;
5. top10 outside `.40-.70` warns;
6. p99/median above `30` and maximum/p99 above `5` warn;
7. combined annual values exactly equal the union of Orphan and Promo yields;
8. ExternalOrLegacy, immature releases, and partial years remain excluded;
9. fewer than 200 releases emits metrics without jackpot adjudication; and
10. every inherited structural failure remains a failure.

Do not alter preserved artifacts to construct fixtures.

## 5. Re-adjudicate the preserved run

Write a new report:

```text
SimLogs/d6-single-lane-pressure-workload-through-1968-1001-historical-analysis.json
```

Command:

```powershell
& 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' SimTools/analyze-single-lane-hit-tail.mjs SimLogs d6-single-lane-pressure-workload-through-1968-1001 --control-prefix=d6-transition-envelope-decade-control-1001 --json=SimLogs/d6-single-lane-pressure-workload-through-1968-1001-historical-analysis.json
```

Expected result, subject to exact analyzer calculation: no structural failures,
no jackpot failures, overall `PASS`, unchanged lane-year metrics, combined-Single
metrics added, and any broad historical warnings reported without failure.

If reconstructed values differ from existing telemetry, fix the analyzer. Do not
change the simulation to fit an analyzer defect.

## 6. Static validation

Run:

```powershell
dotnet build 'Label Man.sln' --no-restore
git diff --check
```

Run all D5 and D6 probes plus the new analyzer checks. Because this handoff
authorizes no simulation change, a disabled replay is unnecessary unless a
simulation source or telemetry producer changes unexpectedly. If one does, stop
and explain why.

## 7. Exact 469-week repeat

Only after the preserved run passes the amended analyzer, run one exact repeat
using the Downloads console executable outside the sandbox:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=469 --run=d6-single-hit-tail-historical-repeat-through-1968-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d6-transition-envelope-decade-control-1001
```

If the in-process gate stops, preserve partial artifacts and stop. If it completes,
require exit code `0`, all 469 weeks, header-only catastrophic output, every
inherited operational and annual rule, byte-identical common outputs to the
retained candidate, no jackpot failure, and identical hit-tail metrics/warnings.

Write repeat analysis to:

```text
SimLogs/d6-single-hit-tail-historical-repeat-through-1968-1001-analysis.json
```

Do not launch another repeat, seeds 1002/1003, M5, a holdout, or a sweep.

## 8. Future jackpot stop rule

A future run requires a behavioral diagnosis only if it has a structural failure
or crosses a direct jackpot guard:

```text
largestReleaseShare > .10
top1Share > .35
```

Preserve such artifacts and locate the first divergence among track construction,
discovery, chart/radio feedback, inventory, clearing, and settlement. Do not
automatically apply the retired redistribution design.

If a later repair is justified, it should conserve demand and finance at its
intervention seam, consume no RNG, avoid outcome/year-specific switches, and be
the weakest change that removes the measured pathology. Those are future design
principles, not current implementation authority.

## 9. Prohibited changes

Do not:

- implement `SingleHitTailRedistributor` or an equivalent allocator;
- force top-decile share toward `.40`;
- add a hard historical target without direct sales microdata;
- change demand, conversion, awareness, discovery, quality, charts, momentum,
  radio, saturation, supply, acceptance, format tilt, or normalizers;
- change Albums, projects, memory, workload pressure, capacity, inventory,
  restock, spillover, market clearing, finance, release counts, label survival,
  artist lifecycle, or RNG order;
- weaken inherited annual or catastrophic bands; or
- delete, overwrite, or relabel the old analyzer report.

## 10. Required closure report

Report source hashes and exact commands; old and amended analyzer results; fixture,
build, diff, D5, D6, strict-gate, catastrophic, and repeat results; annual lane and
combined count, units, top10, top1, largest-release share, p99/median, maximum/p99,
and Gini; every failure and warning; proof that no simulation behavior changed;
and confirmation that late PromoSingle concentration is an upper-cohort tail, not
one jackpot release.

State explicitly that the broad 50-70% top-decile expectation is an inference,
not a verified acceptance band. The goal is to preserve a plausible hit-driven
1960s market, retain direct protection against an absurd synthetic jackpot, and
avoid flattening a distribution that is already stable and historically
defensible.
