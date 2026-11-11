# Veteran chart excess: where the demand advantage comes from

Status, 2026-11-10. Static reads of completed runs only. No simulation was run and no gameplay source changed.
Continues `VeteranChartInvestigation.md`. Written as a handoff: each action lists its evidence, the proposed change,
and the test that would confirm or kill it.

## Summary

The early cohort's advantage is carried mostly by its **albums**, and only secondarily by follower purchases of
singles in launch week.

1. Followers buy LPs in volume. Those LPs chart, and an act's charted LPs keep heating every later single for as
   long as the LPs stay active.
2. With follower purchase intent off, older acts hold 40% fewer active album hits and their singles read 40% less
   artist heat. That arm is the one that cut the 1960 cohort's 1964 Top 10 share from 43.3% to 18.8%.
3. The launch-week singles bump is real but is not what makes the hits. Removing advance stock took week-one
   Hot 100 entries by older hit acts from 61% to 0% and left their Top 10 rate unchanged (16% to 14%).
4. Followers also recruit themselves, and the adult following barely decays, so the stock that drives all of this
   stays with whoever built it first.

Runs read: the two follower-on decade controls (`stature-marketing-decade-v2-{1001,2002}-control`) and the three
261-week channel arms (`veteran-follower-channels-261w-v2-2002-{full,no-stock,no-intent}`). The arms are one seed.

## Findings

### 1. A single's artist heat is 85–90% album-caused for older acts

`ChartManager.CalculateArtistHeat` sums, over every active record by the act, its current chart standing
(0.30 / 0.15 / 0.05) plus a retained credit for its peak (0.10 Top 10, 0.05 Top 40) plus 0.05 per hit up to 0.30.
Albums and singles count alike, on raw album-chart positions. The retained terms last as long as the album stays
in the active set. Singles read this heat in the weekly radio target (`+ 0.12 × heat`) and albums read it in
their own discovery rate.

Heat read by 1964 single launches, by the act's first Hot 100 year:

| Cohort | Seed 2002 heat | of which albums | of which retained album peaks | Active album hits per launch | Seed 1001 heat / albums |
|---|---:|---:|---:|---:|---:|
| 1960 | 0.556 | 0.496 | 0.467 | 4.17 | 0.610 / 0.539 |
| 1961–62 | 0.422 | 0.391 | 0.367 | 3.31 | 0.413 / 0.375 |
| 1963–64 | 0.147 | 0.112 | 0.094 | 0.86 | 0.160 / 0.129 |

The singles' own contribution is 0.03–0.07 in every cohort. The whole gap between cohorts is album catalogue.
By 1968 the 1960 cohort still reads 0.53–0.56 and the 1963–64 cohort has climbed to 0.38–0.43, so the ladder is
ordered by how long an act has been accumulating LPs.

### 2. Follower purchase intent is what keeps those albums charted

Same build, same seed, 1964 single launches by the 1960 cohort:

| Arm | Launches | Acts releasing | Heat at launch | Active album hits | Launch awareness | Momentum | Top 10 singles | Top 10 weeks |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Both channels | 380 | 159 | 0.556 | 4.17 | 0.53 | 0.20 | 41 | 197 |
| Advance stock off | 419 | 158 | 0.588 | 4.32 | 0.53 | 0.18 | 40 | 173 |
| Purchase intent off | 254 | 121 | 0.336 | 2.48 | 0.39 | 0.10 | 13 | 74 |

The 1963–64 cohort in the same arms goes from 50 to 76 Top 10 singles and 158 to 253 Top 10 weeks with intent off.
Its own heat barely moves (0.147 to 0.141).

Follower LP buying is large enough to do this. Singles intent clears at about 10% (0.154 in 1960, 0.095 in 1969)
because the singles purse is fixed, so a follower "grant" on a single becomes about a tenth as many sales. LP intent
clears at 84% in 1960 falling to 23% in 1969. Estimated follower units for the 1960 cohort in 1964:

| | Singles | LPs |
|---|---:|---:|
| Seed 2002 | 2.22M (1.7% of single units) | 7.90M (about 12% of store LP units) |
| Seed 1001 | 2.25M | 8.22M |

These are grants scaled by the yearly clearing ratio, so they are estimates. The earlier "grant ÷ lifetime units"
reading overstated singles by roughly tenfold.

### 3. The launch-week singles bump does not make the hits

Singles launched within 52 weeks of the act's previous Top 40 entry, quality 0.70–0.80, 1964, seed 2002:

| Group and arm | n | Follower book | Week-one Hot 100 | Top 40 | Top 10 |
|---|---:|---:|---:|---:|---:|
| Older acts, both channels | 93 | 83k | 61% | 30% | 13% |
| Older acts, advance stock off | 114 | 28k | 0% | 27% | 18% |
| Older acts, purchase intent off | 52 | 0 | 10% | 21% | 15% |
| Newer acts, both channels | 110 | 12k | 5% | 16% | 6% |
| Newer acts, purchase intent off | 139 | 0 | 1% | 18% | 12% |

Older acts do sell more in week one, and about 70% of those week-one units are follower purchases. But a hot older
act's single reaches the Top 10 at about the same rate with or without them. What the intent-off arm changes is how
many older acts are still hot (93 to 52 such launches) and how much room newer acts get (Top 10 rate 6% to 12%).
Across all 1960-cohort launches the Top 10 rate does halve (10.8% to 5.1%), which is that composition effect.

### 4. At equal quality and recency, newer hit acts launch far colder

Same group as above, both seeds, follower-on decade controls, 1964:

| | Launch awareness | Artist momentum | Recognition | Follower book |
|---|---:|---:|---:|---:|
| Older acts (seed 2002 / 1001) | 0.73 / 0.77 | 0.37 / 0.44 | 0.74 / 0.79 | 87k / 107k |
| Newer acts | 0.40 / 0.38 | 0.08 / 0.07 | 0.19 / 0.19 | 13k / 17k |

Both groups had a Top 40 entry in the last year. Momentum is meant to be "hot now", yet the act that just broke
through reads a fifth of the veteran's. The rule pays +0.02 for a Top 40, +0.10 for a Top 10, +0.18 for a No. 1,
and charges −0.05 to −0.15 for each record peaking below 40. Album runs pay into the same account through
`ArtistManager.OnRecordChartUpdated`. Older acts hold about sixteen active album hits for every single hit, so
album milestones are the likely source of their momentum, reputation and recognition. That split is not logged
and is not measured here.

### 5. Followers recruit themselves, and the adult following does not decay

`FollowingWeek` counts every unit sold as a potential joiner at 0.30 × heat, including the units the act's own
followers just bought. The only brake is `1 − following / fitPopulation`, and the fit population is tens of
millions. For the 1960 cohort in 1964, buyer joiners were 4.8M (seed 2002) and 5.4M (seed 1001) against about
10.1M and 10.5M follower-purchased units. At the rule's heat range that is 0.6M–3.1M joiners, or 12–60% of the
cohort's buyer joiners, coming from people who already follow. Heat per act-week is not logged, so only the range
is known.

Share of an act's peak following still held later (acts peaking above 50k, both seeds agree within 0.03):

| Months after peak | Total | Young | Adult |
|---|---:|---:|---:|
| 12 | 0.85 | 0.77 | 0.96 |
| 36 | 0.58 | 0.39 | 0.88 |
| 60 | 0.39 | 0.12 | 0.84 |

The young side matches its 2–3 year design half-life. The adult side holds 84–87% after five years, where a 5–7
year half-life would leave 55–60%. Adults pay 0.30 intent per LP, which is the format that clears. The 1960
cohort's adult following rises from 11M to 20M by 1966 and never falls.

## Actions

Ordered by how directly the evidence points at them. Each needs a same-build control; the existing arms were built
on `BB1AE1C2…` and current source has moved.

| # | Change | Why | Test |
|---|---|---|---|
| 1 | Add `--follower-album-scale=` beside the existing `--follower-single-scale=`, and run LP intent off with singles intent on. | Finding 2 predicts the LP channel alone reproduces most of the intent-off result. No current switch separates the formats. | 261 weeks, seed 2002, against a same-build full arm. Expect 1960-cohort active album hits near 2.5 and 1964 Top 10 share near the intent-off 18.8%. If the share stays near 43%, the LP path is wrong and singles intent matters after all. |
| 2 | Stop retained album peaks heating singles: `--single-artist-heat-current-albums` (already built, off). | Finding 1: 0.47 of the 1960 cohort's 0.56 heat is retained album peak credit. Under that rule the logged inputs give about 0.09 for the 1960 cohort and 0.05 for 1963–64. A current Top 10 album still heats singles, which keeps the author's Beatles requirement. | Same horizon and seed. Report cohort Top 10 shares, repeat Top 40 entries by cohort, album units. The earlier five-seed screen of this switch predates follower demand and only scored label concentration. |
| 3 | Exclude follower-purchased units from joiners for both formats. `--follower-singles-recruit-from-new-buyers` covers singles only; LPs are where the volume is. | Finding 5. | Same run shape. Expect 1960-cohort buyer joiners down 12–60% and its stock falling after 1962 instead of plateauing. |
| 4 | Log career milestone credits by format (momentum, reputation, recognition deposits from single runs versus album runs), observe-only. | Finding 4 infers but does not measure that album runs carry the veterans' momentum. | Byte-identical economy outputs; then read the split before changing any deposit. |
| 5 | Revisit adult drift only after 3. | Part of the adult persistence is self-recruitment and the age-out transfer; raising drift first would fit the wrong cause. | Re-run the persistence table on the action 3 arm. |

Repeat entries are the guardrail throughout. The intent-off arm dropped repeat Top 40 share from 54.2% to 43.0%,
and history's repeats in 1964 come mostly from the newest cohort (87 of 196), which the model under-produces
(29–36). Actions 1–3 remove an advantage from older acts; none of them yet gives a newer hit act its follow-up.
Finding 4 is where that would come from.

## What this does not show

- Every table is a read of worlds that already ran. Nothing here re-ranks a chart under a changed rule.
- The channel arms are one seed. The decade controls agree across two seeds on every stock and heat figure.
- Follower unit counts are grants times a yearly clearing ratio. Per-record follower sales are not exported.
- The link from album milestones to momentum and recognition is read from code and from hit counts.

## Reproduce

```powershell
$py = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe'
& $py SimTools/analyze_veteran_album_heat.py              # channel arms
& $py SimTools/analyze_veteran_album_heat.py 2002 1001    # decade controls
& $py SimTools/analyze_veteran_channel_arms.py
& $py SimTools/analyze_veteran_launch_week.py 2002
& $py SimTools/analyze_veteran_following_flows.py 2002
& $py SimTools/analyze_veteran_demand_stack.py 2002
```

Saved outputs are in `SimLogs/veteran-demand/`. `analyze_veteran_album_heat.py` rewrites
`album-heat-v1.json` on each run; the kept copies are `album-heat-arms-v1.json` and `album-heat-decade-v1.json`.
