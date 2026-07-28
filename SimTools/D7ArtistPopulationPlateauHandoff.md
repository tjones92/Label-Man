# D7 artist population — the roster plateau

Continues from `D7AlbumPortfolioCommitmentHandoff.md`, which diagnosed the population
collapse and landed demand-responsive formation. That fix arrested the collapse and put
`successfulReleases` back in band for the whole decade. It did **not** get rosters back to
the size labels say they want, and this document is about that remaining gap.

## What the plateau is

With responsive formation (`d7-responsive-formation-522-1001`):

| year | active | rostered | aggregate operating target | fill |
|---|---|---|---|---|
| 1960 | 7300 | 2739 | ~1126 | 0.96 |
| 1964 | 4397 | 1986 | ~3280 | 0.62 |
| 1969 | 3427 | **1841** | **~3935** | **~0.47** |

Rostered stabilizes near 1841 against an aggregate target near 3935. Labels sit at roughly
half their own stated appetite for the back half of the decade, and the shortfall is stable
rather than closing.

Formation is no longer the constraint. It is answering demand — the servo runs at 681-816
formations a year from 1964 — but disbandment *rose* with it, 4287 -> 4816, because more
acts formed means more acts dropped and then destroyed on the same clock. **Outflow is now
the binding constraint, not inflow.**

## Three candidate defects, in the order they should be settled

### 1. Is the target real? (settle this first)

Everything below is about closing a gap to ~3935 roster slots. If that number is inflated,
part of the gap is not a gap and closing it would be fixing toward a bad target. Two facts
bear on it.

**Aggregate demand growth is entirely label count.** 293 -> 1160 labels across the decade,
a 4x rise. Mean appetite per label is flat and slightly falling, 3.84 -> 3.39. Talent
formation is therefore chasing label-population growth, and the formation servo's input
signal (`affordableHiringOpportunityLabels`) is a count of labels. If label founding is not
itself governed, formation is being fitted against a free-running variable.

**Per-tier appetite is pinned at a ceiling, not growing.** The falling mean is not small
entrants diluting the average — every tier individually saturates early and then stops:

| tier | 1960 | 1962 | 1964 | 1969 |
|---|---|---|---|---|
| Major | 18.51 | 23.62 | 25.10 | **25.10** |
| MidTier | 8.42 | 10.67 | 10.67 | **10.64** |
| Independent | 3.49 | 4.80 | 4.30 | 3.71 |
| Boutique | 2.65 | 4.09 | 4.09 | **4.09** |
| Small | 1.25 | 1.91 | 1.92 | 1.91 |

Majors hold *exactly* 25.10 for six consecutive years.

**Decided — do not re-litigate.** Upper-tier appetite should grow across the decade and
currently cannot. The LP takeover is precisely when majors scaled up: Columbia under Clive
Davis added Joplin, Santana, Chicago, Blood Sweat & Tears and Laura Nyro across 1967-69, and
Warner/Reprise and Atlantic expanded comparably. Freezing majors at 25 artists from 1964
removes the demand-side counterpart of the album shift this directive models.

The scope of that decision, precisely:

- **Major and MidTier grow** across the decade. These are the tiers that historically
  expanded through the LP takeover, and both are pinned at a ceiling today (25.10 and 10.67
  from 1964 and 1961 respectively).
- **Small and Boutique stay flat.** ~1.9 and ~4.1 is historically right; most 60s
  independents carried a handful of acts or died. Do not grow these to close the gap.
- **Independent is left alone.** Its 4.96 -> 3.71 slide is plausibly a real economic
  squeeze, and turning it into growth would be fitting rather than modelling.

So the aggregate target is *too low* at the top and about right at the bottom. The plateau
gap is real, and it will widen once appetite is corrected — the outflow work in section 3 is
sized against a target that is going to grow, not shrink.

Unresolved within the decision: whether growth should be a time curve, a function of label
success or catalogue size, or a lift in the ceiling that labels grow into under existing
rules. The third is the smallest change and keeps growth earned rather than authored, which
is the pattern the portfolio-commitment work settled on for a closely analogous problem
(see `GetAlbumPortfolioCommitmentMultiplier` — commitment moved from a tier lookup to
capacity derived from distribution reach and roster depth, precisely so it would be earned
rather than conferred). That is a starting suggestion, not part of the decision.

### 2. The demand signal counts labels, not vacancies

`ActivateProspectsForHiringOpportunities` computes:

```csharp
int opportunities = labels.Count(label => label.IsActive &&
    label.CurrentRosterSize < label.OperatingRosterTarget &&
    label.CanAffordToSign(...));
```

A major with eight unfilled slots and a one-artist label with one unfilled slot each
contribute exactly **1**. Since `CalculateProspectActivationCount` caps activations at
`opportunities - seeking`, the market activates far fewer prospects than there are slots to
fill, and the same under-count feeds the formation servo.

Counting vacancies instead of labels is tier-neutral and strictly more accurate: it does
not edge out small labels, it counts their demand at its true size of one rather than
inflating it to a major's or discounting it to zero. It is already affordability-gated, so
labels that cannot pay are still excluded.

Expect this to raise both prospect activation and formation. It is a supply-side lever as
much as a signal correction.

### 3. The terminal inactivity clock is opportunity-blind

This is the mechanism actually destroying the roster.

`ApplyLifecycleExits` sends any artist with `contractSequence > 0` who is unsigned for
`InactivityHorizonWeeks` (78) to `Inactive`, then after `TerminalInactivityWeeks` (52) more
to `Disbanded` or `Retired`. **There is no return path** — `IsEligibleUnsignedCandidate`
requires `isActive`, and nothing restores `lifecycleStatus` to `Active` except a drop, which
requires being signed first. Over the decade: 7145 inactivity events, 4287 disbandments,
1015 retirements.

The clock runs regardless of whether the industry had a vacancy. Roster slots are a small
and shrinking fraction of the registry, so most artists are unsigned at any moment through
no fault of their own, and the clock removes them for it.

The model already knows how to hold surplus talent without destroying it. That is exactly
what `ProspectMarketStatus.Latent` and the `hiringOpportunities` cap in
`CalculateProspectActivationCount` are for. But `IsSeekingProspect` and `IsLatentProspect`
both require `contractSequence == 0`, so the reservoir is available **only to artists who
have never had a contract**. Sign once and you lose access to it and are put on a death
clock instead — which is backwards, since a proven act with a chart history is *more*
likely to get another deal than an unknown.

Two secondary asymmetries in the same method:

- **Groups disband unconditionally; solos get an age guard.** `if (!group && (lead == null
  || lead.GetAge(year) < MinimumSoloRetirementAge)) continue;` spares a young solo but
  destroys a group of any age. This is why disbandment (4287) is four times retirement
  (1015). A 22-year-old band 2.5 years without a deal is not more permanently finished than
  a 22-year-old solo act.
- **Two performance drops is immediate permanent removal.** `performanceDropCount >= 2`
  sets `CareerState.Retired` and `lifecycleStatus = Inactive` on the spot, with no clock and
  no appeal.

**Design note before implementing:** `BuildLaborMarketSnapshot` counts a prospect status on
an experienced artist as an integrity violation
(`prospectStatusContractConflicts`), and it is telemetered. Extending the reservoir to
experienced free agents means revisiting that invariant deliberately, not working around it.

## What to expect when this is fixed

Rostered moving from ~1841 toward ~3935 is roughly a 2x rise in signed artists, so:

**Head is a clean gated decade, so every one of these is a regression risk, not slack.**
`d7-portfolio-gated-decade-522-1001` completes 522 weeks with no catastrophic rows and all
six metrics in band. The margins available:

| metric | current range 1960-1969 | headroom to the 1.30 ceiling |
|---|---|---|
| `successfulReleases` | 0.915 - 1.075 | 0.225 at the worst year |
| `scheduledAlbumProjects` | 0.757 - 1.160 | 0.140 |
| `totalUnits` | 0.952 - 1.084 | 0.216 |
| `labelNet` | 1.021 - **1.193** | **0.107** |
| `marketNet` | 1.000 - 1.154 | 0.146 |

- **Release volume rises.** `successfulReleases` runs 0.915-1.075 and will go past 1.0.
  There is room, but `scheduledAlbumProjects` already peaks at 1.160 and has the least of it.
- **Units should barely move.** Demand is pool-limited: the formation change raised Single
  releases 75% while Single units rose only 9%. More releases divide the pool rather than
  adding to it. Do not expect `totalUnits` to be the constraint.
- **Costs are the real exposure, and `labelNet` is the tightest metric on the board** at
  1.193. More releases and more signings against unchanged gross means more production,
  advances and marketing, which pushes label net *down* — toward parity, so the first-order
  effect is favourable. Watch for overshoot into label bankruptcies, which would feed back
  into label count and therefore into the demand signal and the formation servo.

Re-run the gated decade after any change here rather than comparing offline. Head passing
cleanly means the gate is now a real regression detector for the first time in this
directive, and it should be used as one.

## Reproduction

Enabled decade, ungated so every year can be read:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance
```

Gated, against the current control:

```powershell
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d7-portfolio-gated-decade-control-1001
```

Do not add `--genre-market-v2-probes` to a run being compared against a control; it perturbs
the RNG stream. Probe runs and comparison runs stay separate.

Read `artist-population-weekly.csv` with `labelTier == 'All'` only. It carries an `All` row
*and* one row per tier, and summing across them doubles `registryTotal`, `activeTotal`,
`rostered` and `neverSignedUnsigned`. `inactive`, `retired` and `disbanded` are written on
the `All` row alone.
