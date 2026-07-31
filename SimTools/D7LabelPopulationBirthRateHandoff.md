# D7 label population — the birth cap, and the plateau that wasn't

Continues from `D7ArtistPopulationPlateauHandoff.md`. That document asked for three fixes
against a roster "plateau" — labels sitting at roughly half their stated appetite. All
three landed and are gate-clean. **The plateau itself was a measurement artifact**, and
this document records the correction, the real defect underneath it, and what to do next.

## 1. The plateau was defunct labels

`ChartManager` retains closed labels for lookup and audit history, and their
`OperatingRosterTarget` is retained with them. The scouting telemetry aggregated every
retained record, so every closed label contributed its target as unmet demand.

At week 520 of the gated decade there are 1,189 label records and **317 live** labels
(949 Defunct, 2 Acquired). Of 883 apparent vacancies, **872 were held by dead labels.**

Measured over live labels only:

| run | live labels | aggregate target | rostered | fill |
|---|---|---|---|---|
| head (`d7-portfolio-gated-decade-522-1001`) | 319 | 1,865 | 1,838 | **0.99** |
| after this directive (`d7-rotation-gated-522-1001`) | 317 | 2,788 | 2,764 | **0.99** |

Labels fill 99% of their appetite, and did so in head too. The prior handoff's
"~3,935 target, fill 0.47" and its "293 → 1160 labels, a 4x rise" are the same artifact:
retained records accumulating. Live labels **fell** across the decade.

| week | records | live |
|---|---|---|
| 52 | 621 | 491 |
| 260 | 909 | 318 |
| 520 | 1,269 | 319 |

The telemetry is fixed — `LabelScoutingVacancyObservation.IsActiveLabel` now exists, closed
labels report zero unused slots, and the audit CSV carries an `isActiveLabel` column.
**Any future appetite or vacancy aggregate must filter on it.**

What the directive's work actually did was raise real capacity: rostered 1,838 → 2,764
(+50%), because Major/MidTier appetite genuinely grew and outflow stopped being
opportunity-blind. It was not closing a gap.

## 2. The real defect: births are capped below the death rate

`CheckForBirths` wants to reach `GetTargetLabelCount(year)` — 600 in 1960, rising to 675
at 1965-66, 625 from 1969. It never gets close, and the arithmetic is exact:

```csharp
float spawnModifier = (targetCount - currentCount) / 20f;              // (625-319)/20 = 15.3
float adjustedChance = Mathf.Clamp(monthlyBirthChance + spawnModifier, 0f, 1f);  // clamps to 1.0
int attempts = Mathf.Min(maxMonthlyBirths, Mathf.Max(1, Mathf.CeilToInt((targetCount - currentCount) / 12f)));
                                                                       // min(6, 26) = 6
```

`adjustedChance` saturates at 1.0, so every attempt spawns and `attempts` is pinned at
`maxMonthlyBirths`. Births are therefore **exactly 6/month = 72/yr**, independent of how
far below target the population is. Measured: records grew 621 → 1,269 = 648 over nine
years = 72/yr, to the unit.

Deaths run **~91/yr** (dead records grew 130 → 950 = 820 over nine years), a ~28.5% annual
rate against ~319 live labels.

**Equilibrium live labels = births ÷ death rate = 72 ÷ 0.285 ≈ 253.** The observed 319 is
still drifting down toward it. This is a rate problem, not a level problem: raising the cap
alone does not hold a level, because deaths scale with the population created.

## 3. What a healthy level is

### Historical

The sim models labels that can sign and release, not every imprint that ever pressed a
record. Roughly 200-400 distinct labels reached the Hot 100 in a given mid-60s year, while
the total number of operating US labels ran into the low thousands across the decade with
very high turnover. The authored 600-675 sits sensibly between those, and the authored
*shape* — 600 rising to 675 by 1965-66, easing to 625 by 1969 — correctly models the
independent boom and its late-decade consolidation.

Sixties label populations churned hard: most independents died within a few years and were
replaced by more. **Fix this on the birth side, not by suppressing deaths.** High founding
against high failure is the historically right texture; damping `TryApplyCompetitiveExit`
or `CheckForDeath` would buy the level by making the industry unrealistically stable.

### Economic — this is the binding constraint

Fill is 0.99, so rostered scales with live label count almost one-for-one. Going 319 → 625
is +96% and would take rostered from ~2,764 toward ~5,400, with release volume following.
Current margins against the 1.30 catastrophic ceiling:

| metric | peak (gated, this directive) | headroom |
|---|---|---|
| `successfulReleases` | 1.10 | 0.20 |
| `scheduledAlbumProjects` | **1.273** (1960) | **0.027** |
| `totalUnits` | 1.089 | 0.21 |
| `labelNet` | **1.207** | 0.093 |
| `marketNet` | 1.159 | 0.14 |

**Doubling the label population will breach the gate.** `scheduledAlbumProjects` has
essentially no headroom at 1960 and `labelNet` is the next tightest. Note the 1960 spike is
a thin-denominator first-year effect; the back half runs 1.10-1.24, so there is perhaps
5-15% of roster growth available there but almost none at 1960.

**Do not jump to the authored target.** The honest reading is that the authored 600-675
cannot be reached without rebalancing the release/album-project economics that the ceiling
measures. That is a larger piece of work than the birth cap.

### Recommendation

Stage it, and re-run the gate at each step:

1. **`maxMonthlyBirths` 6 → 8** (96/yr). Just above the ~91/yr death rate, so the decline
   reverses instead of continuing. Equilibrium ≈ 96 ÷ 0.285 ≈ 337. Small demand shock,
   directly measurable.
2. If clean, **8 → 10** (120/yr, equilibrium ≈ 420). Expect `scheduledAlbumProjects` at
   1960 and `labelNet` to be the first to move.
3. Beyond ~420 live labels, expect to need economic work before the count can rise further.
   Treat that as a separate directive, not a continuation of this one.

Watch for the second-order effect: more labels means each takes a smaller market share,
which raises `CheckForDeath` pressure through `cashReserves` and `consecutiveLossMonths`.
The death *rate* may climb with the population, so each step's equilibrium will land below
the naive `births ÷ 0.285` estimate. Measure it rather than assuming it.

## 4. Then re-test the roster question

Once live label count is raised, re-measure what this directive was originally chasing:

- rostered and **live-only** fill (`isActiveLabel == true`),
- whether fill stays near 0.99 or whether labels start failing to fill (which would mean
  talent supply, not appetite, has become binding for the first time),
- `latentProspects` and `latentRotations` — the reservoir should drain faster with more
  demand, and terminal exits should rise with it,
- tier promotions, which are gated on roster size and charting and so should increase
  without any change to the promotion rules.

If fill stays at 0.99 through step 2, appetite is still not the constraint and the roster
simply tracks label count — which would make label population the whole story.

## 5. Reproduction

Gated, against the frozen control. **Run gated by default**; the gate is observational
(the fail-fast accumulator only reads and sums, `ValidateCatastrophicStructural` builds a
local dictionary and throws), so a gated run simulates identically to an ungated one and
writes the same CSVs plus a verdict.

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d7-portfolio-gated-decade-control-1001
```

Fall back to an ungated run only to diagnose an abort — `--catastrophic-fail-fast` throws on
the first breached year, costing every later year's diagnostics.

### Iterate at 260 weeks, certify at 522

A decade is not needed for every regression check. Measured peaks for this directive's
final state, by year:

| metric | peak year | peak | headroom to 1.30 |
|---|---|---|---|
| `scheduledAlbumProjects` | **1960** | 1.273 | **0.027** |
| `successfulReleases` | 1962 | 1.101 | 0.20 |
| `marketNet` | 1964 | 1.165 | 0.14 |
| `labelNet` | 1969 | 1.207 | 0.093 |
| `totalUnits` | 1969 | 1.089 | 0.21 |

The tightest metric peaks in **year one**, so `--weeks=104` already exercises the binding
constraint. By `--weeks=260` three of five metrics have hit their exact decade maximum and
`labelNet` is within 0.027 of its own. That makes 260 a real filter at ~40% of the cost.

It is not a certificate: `labelNet` and `totalUnits` climb to 1969, so a change that
inflates late-decade economics can pass at 260 and fail at 522. Use 260 while iterating,
then 522 before landing. For the birth-rate work the decade is required regardless — the
effect is a cumulative population drift, so the answer only exists at 522.

The control is the **disabled route** and is deliberately frozen (see commit 53eac45). It is
not a previous enabled run and must not be re-baselined per change.

Do not add `--genre-market-v2-probes` **or `--artist-population-lifecycle-probes`** to a run
being compared against a control. Both perturb the RNG stream. The population suite's own
header claimed RNG neutrality and was wrong: several probes reach helpers that draw from the
global stream, and a 52-week run with the flag diverges from one without it in 1960 (album
units 2,271,329 against 2,426,185 on seed 1001). This cost one wasted decade run here — a
gated decade with the flag breached `scheduledAlbumProjects` at 1966 (1.3115) purely from
stream displacement, and passed without it. Run probes separately.

Read `artist-population-weekly.csv` with `labelTier == 'All'` only. It carries an `All` row
*and* one row per tier, and summing across them doubles `registryTotal`, `activeTotal`,
`rostered` and `neverSignedUnsigned`. `inactive`, `retired` and `disbanded` are written on
the `All` row alone.

For label counts, `label-finance.csv` carries `status` per label per week; treat
`Defunct`/`Acquired`/`Bankrupt` as dead. In `label-scouting-vacancy-weekly.csv`, filter on
`isActiveLabel`.
