# D7 label chart access loop — continuation handoff

Paused July 28, 2026 at the user's request. This continues
`D7LabelChartAccessLoopHandoff.md`.

## 1. Acceptance target and user decisions

The target was clarified after the original handoff:

- **400–600 unique labels charting cumulatively across the entire 1960–1969
  decade.** This is not an annual target.
- An active distributor is supposed to provide **temporary national reach** for
  the duration of the deal.
- A very small number of Small labels charting is acceptable.
- The below-MidTier charting population should be dominated by
  **Independent** labels, not Small labels.

Do not resume work against the old “200–400 firms per year” interpretation.
The annual `firmsCharting` series is useful context, but acceptance is the
decade-cumulative identity count.

## 2. Current worktree

Eight tracked files are modified and uncommitted:

- `Data/AILabel.cs`
- `Systems/CompetitorManager.cs`
- `Systems/ChartSimulator.cs`
- `Systems/ChartManager.cs`
- `Systems/RuntimeLabelProfileFactory.cs`
- `Systems/LabelLifecycleManager.cs`
- `SimTools/ChartAuditRunner.cs`
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs`

`.claude/` is an unrelated untracked user directory. Do not alter it.

At pause:

```text
183 insertions(+), 50 deletions(-)
git diff --check: clean
```

No Godot process was running when checked immediately before the latest
checkpoint.

## 3. Implemented mechanics

### 3.1 Active distribution supplies temporary national reach

`AILabel.effectiveNationalReach` is:

```csharp
Mathf.Clamp(nationalReach + borrowedReach, 0f, 1f)
```

`borrowedReach` still comes from the active deal. Ending a deal therefore
removes the temporary contribution without mutating permanent
`nationalReach`.

The effective value is used by:

- live Single demand scaling;
- regional launch coverage;
- broad launch awareness;
- regional-breakout propagation.

### 3.2 National reach can be earned permanently

`CompetitorManager` now grows permanent `nationalReach` through two bounded
paths:

- qualified self-built monthly growth: `+0.008`, ceiling `0.70`;
- completion of a distribution term: retain `25%` of granted reach, ceiling
  `0.80`.

Both helpers are pure, clamped, and cannot reduce a value that already exceeds
the configured ceiling.

### 3.3 Pull deals grant the distributor’s actual network

The former pull-deal path intersected the distributor's regions with the
client's strong regions. In practice it often granted only the region the
client already served.

`GetGrantedDistributionRegions` now grants every distinct region in the
distributor's network that the client does not already cover. Distributor
eligibility uses the same “has at least one new region” rule. Pre-chart restock
also recognizes regions granted by the active deal, while retaining a real
backorder/raw-demand requirement.

The regional-breakout threshold remains `0.30`. The current candidate raises
`monthlyPullOfferProbability` from `0.12` to `0.40`; eligibility still requires
the existing quality, regional-sales, and breakout evidence.

### 3.4 Live Single demand follows capability instead of frozen tier

The enabled Single path no longer applies the hard-coded tier switch that gave
Independent labels `0.55` forever and Boutique labels `1.20` regardless of
earned reach.

The current pure scale is:

```csharp
Clamp(0.45 + distributionStrength * 0.55 + effectiveNationalReach * 0.35,
      0.55, 1.20)
```

Album demand was not changed.

### 3.5 Runtime founder geography is no longer all East Coast

The old reconciliation searched `MarketRegion.majorCities`, failed for the
generated headquarters names, and fell back to `regions[0]`. In the measured
baseline decade, **674/674 runtime founders** were therefore assigned to
`eastcoast`, including San Francisco and Dallas firms.

Runtime geography now resolves the headquarters through
`DistanceModel.GetCityByName`, uses the canonical city's `parentRegionId`, and
ensures the resulting home region appears in `distributionRegions`.

In the latest checkpoint, the 382 runtime founders were distributed as:

| home region | founders |
|---|---:|
| eastcoast | 112 |
| deepsouth | 96 |
| greatlakes | 74 |
| westcoast | 61 |
| southwest | 21 |
| greatplains | 18 |

`362/382` were direct `hq-match`; 20 used the existing
`domestic-unmapped` home-city fallback.

### 3.6 Runtime entrants are Independent-heavy and get a longer runway

Runtime founding is now 25% Small / 75% Independent through the pure
`SelectRuntimeFoundingTier` helper. The six-year checkpoint produced:

- 277 Independent founders;
- 105 Small founders.

Runtime emergence and competitive-review protection were both extended from
9 to 18 months. This gives a new firm time to sign artists, release records,
and build distribution before the quarterly marginal-label exit review.

## 4. Audit and probe coverage

`ChartAuditRunner` now:

- writes permanent `nationalReach` in `label-finance.csv`;
- verifies that forced exit/renew deal completion raises permanent national
  reach;
- preserves the original concentration columns;
- appends annual firms-charting counts for all five tiers;
- appends cumulative unique firms-charting counts for all five tiers;
- counts each original label ID once and attributes it to its tier on first
  chart appearance.

The fixed D6 suite is now probes 1–73. New probes cover:

- self-built reach boundaries;
- completed-deal retention;
- active distributor temporary reach and its removal;
- full new distributor-region grants;
- continuous live Single demand scaling;
- San Francisco runtime geography/home distribution;
- the 25% Small founding boundary;
- the 18-month runtime runway.

The latest probe run completed:

```text
run=d7-runtime-indie-probes3-52-1001
D5 passed
D6 fixed probes 1-73 passed
CHART_AUDIT_COMPLETE
```

The last build also passed with only the pre-existing
`ChartManager.OnGenreMomentumChanged` unused-member warning.

## 5. Baseline decade result

Before the runtime-geography, entrant-mix, longer-runway, and higher pull-offer
candidate, this full run completed:

```text
d7-decade-firms-522-1001
```

Its cumulative charting-label series was:

| year | annual firms | decade cumulative |
|---|---:|---:|
| 1960 | 147 | 147 |
| 1961 | 143 | 174 |
| 1962 | 139 | 189 |
| 1963 | 137 | 196 |
| 1964 | 132 | 202 |
| 1965 | 128 | 210 |
| 1966 | 133 | 216 |
| 1967 | 134 | 224 |
| 1968 | 137 | 234 |
| 1969 | 139 | **241** |

Final first-chart tier mix:

- Small 23
- Boutique 30
- Independent 100
- MidTier 78
- Major 10

This definitively showed that the 400–600 decade target was not met.

## 6. Latest six-year checkpoint

The current worktree was run for 312 weeks:

```powershell
& 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' `
  --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=312 `
  --run=d7-runtime-indie-312-1001 `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --lean-probe `
  --profile-performance
```

It completed normally in about nine minutes:

```text
CHART_AUDIT_COMPLETE run=d7-runtime-indie-312-1001 weeks=312
```

The usual post-completion `MissingSingletonsTemp.cs does not inherit from
Node` autoload error remains pre-existing.

Results:

| year | annual firms | cumulative | cumulative Small | cumulative Boutique | cumulative Independent | cumulative MidTier | cumulative Major |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 144 | 144 | 5 | 16 | 41 | 73 | 9 |
| 1961 | 155 | 178 | 7 | 23 | 59 | 79 | 10 |
| 1962 | 137 | 194 | 8 | 25 | 71 | 80 | 10 |
| 1963 | 148 | 209 | 12 | 26 | 81 | 80 | 10 |
| 1964 | 150 | 219 | 14 | 27 | 88 | 80 | 10 |
| 1965 | 151 | **239** | 16 | 29 | **104** | 80 | 10 |

This is a real improvement over the old configuration's 210 through 1965,
and Independent already dominates the below-MidTier cumulative mix. It is
still not an acceptance result: the observed trajectory is unlikely to reach
400 by 1969 without another conversion improvement.

Annual Single volume stayed stable at approximately 145–153 million units.

## 7. Entrant conversion is the remaining constraint

The checkpoint produced 382 runtime founders. A join of
`runtime-label-profiles.csv`, `release-outcomes.csv`, and `lifecycles.csv`
showed:

- 316 runtime labels with at least one completed release;
- 3,404 completed runtime-label releases;
- 23 runtime labels with a retired record that had charted;
- all 23 of those observed charting founders were Independent.

The charting calculation excludes still-active, unretired records, so it is a
lower bound. Its scale agrees with the cumulative gain, however: geography
and survival now work, but only a small fraction of runtime Independents
convert releases into a first chart appearance.

Distribution results:

- 195 offers generated;
- 101 accepted/signed (`51.8%`);
- 39 distinct runtime-founded clients signed a deal;
- every signed deal was `LabelSought`;
- no `DistributorCourted` deal appeared in the six-year run.

Among runtime Independents, founders with an observed chart hit had somewhat
higher mean production quality (`0.707` vs `0.680`) and owned reach (`0.493`
vs `0.440`), but there was no single obvious profile-field discontinuity.

Do not simply increase runtime births: 82.7% of founders already reached a
completed release, so formation volume is not the primary missing link.
The next pass should isolate the post-release regional-breakout → deal →
national-chart conversion.

## 8. Recommended resume sequence

1. **Do not run the full decade yet.** The 239-through-1965 checkpoint is
   below a credible 400-label trajectory.
2. Add a compact first-chart event/funnel audit keyed by label ID, including:
   runtime/launch origin, birth tier, current tier, best regional-breakout
   strength, active/completed deal state, permanent/borrowed reach, and
   release age. The cumulative counter currently records identity and first
   tier internally but does not emit per-label first-chart rows.
3. Measure runtime founders at these boundaries:
   released → regional breakout ≥0.30 → offered → signed → first national
   chart appearance. Compare charting and non-charting Independents without
   relying only on retired records.
4. Repair or tune the binding boundary, preserving:
   - evidence-gated distribution;
   - temporary active-deal national reach;
   - Independent-heavy below-MidTier contribution;
   - stable annual market volume;
   - the cumulative, not annual, target.
5. Re-run a 312- or 366-week checkpoint. A plausible 400-label decade
   trajectory should be materially above 239 by the end of 1965; roughly
   280–320 is a useful checkpoint range, depending on later-year turnover.
6. Only then run 522 weeks and accept only if 1969 cumulative unique charting
   labels fall in **400–600**.
7. Re-run forced deal exit and renew integrations, the D5/D6 probes, build,
   and `git diff --check`.

## 9. Commands

Headless Godot executable explicitly approved by the user:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
```

Probe run:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=52 `
  --run=<name> `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --lean-probe
```

Checkpoint:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=312 `
  --run=<name> `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --lean-probe `
  --profile-performance
```

Full acceptance run:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=522 `
  --run=<name> `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --lean-probe `
  --profile-performance
```

Build:

```powershell
dotnet build "Label Man.sln" --no-restore
```

The console tool may time out while Godot continues in the background. Before
launching a replacement run, check:

```powershell
Get-Process | Where-Object { $_.ProcessName -like 'Godot*' }
```

Audit artifacts are written to `SimLogs/<run>-*.csv`.
