# Handoff: release-week DJ picks and the local break (2026-10-29)

**For:** an agent picking this up cold. Everything below is opt-in, on branch `media-atmosphere`, and **uncommitted**.
Do not commit SimLogs output (audit output stays in `SimLogs/`). Background: `SimTools/RegionalBreakHandoff.md` (item 1)
and `SimTools/RegionalSyncDesign.md` §9v-§9x.

## The goal

The author wants regional radio to drive the simulation as it did in 1960: an indie record breaks in one town, spreads
to its neighbours, then goes national. The benchmark is the **city lead**: the share of records on a Los Angeles or
Minneapolis Top 40 / FullService station list 2+ weeks before their national top-100 debut. 1960 history:

| Group | History lead ≥ 2 |
|---|---:|
| Indie top-10 hits | 18.5% |
| Big-5 top-10 hits | 0% (0 of 20) |
| Records peaking 41-100 | 16% |
| All national debuts (Feb-Nov) | 14.5% |

Before this work the model gave indie top-10 hits a lead of 1-2%.

## What was found before the build (do not repeat)

- **A breadth-aware national chart cannot do it** (§9v). Coverage weights, hard local-list gating and reporter
  thresholds all give lead 0-1.5% in replay. The seven regional top 40s hold only ~75 distinct records a week, fewer
  than the 100 national slots, so every local seller already charts. Hits chart at age 2, before they are local
  sellers anywhere.
- **Stations that read local sales sooner cap at ~9%** (§9w), even as an oracle. The sales that would trigger an add
  are the same sales that chart the record nationally. 18.5% needs stations to add ~40% of eventual indie hits at
  age 1, before sales.
- **The author chose "the DJ hears the fit"** over "the pick earns the hit." The commercial-fit draw stays fixed at
  launch, and programmers forecast it with noise. Do not make picks raise a record's fit without asking the author.
  That is a different meaning of commercial fit (handoff item 2).

## What is built

All in the §9m station-pick block (`Systems/Radio/StationPicks.cs`, `Systems/Radio/StationNetwork.Playlist.cs`,
`FitPickScore`). Picks are seated after the sales ranking, from records it did not seat, in the regional (5e) meeting.

| Flag | Effect |
|---|---|
| `--station-picks=N` | N pick slots per station (Light tier by default). |
| `--station-picks-score=fit` | Ranks records up to `--station-picks-age` weeks old by exp(national log fit draw + S × keyed normal per station and record), only among records the label serviced to that region at release (the §9t launch footprint). |
| `--station-picks-fit-noise=S` | The noise S, in log units (default 1). |
| `--station-picks-fit-bar=B` | Only records heard B log units above the connection line (`SingleCommercialFit.UnexposedCapLog`). A hit sits ~2.6 above it. |
| `--station-picks-fit-unserviced` | Only records held in some region at launch, i.e. not serviced everywhere. |
| `--station-picks-extra` | Pick slots are added on top of the tier instead of reserved from it. |

Supporting code: `SingleCommercialFit.NationalLog` (public read of the draw), `RegionalFitExposure.LaunchExposed` and
`HeldAtLaunch`. No new RNG draws: the noise is `BandLife.Normal("dj-ear|station|record")`. A fit-pick run prints
`STATION_PICKS fit slots=… noise=… bar=… unserviced=… extra=…` on startup.

**Current candidate `djb24u`:** the `ufxal025` flags (below) plus
`--station-picks=2 --station-picks-score=fit --station-picks-age=2 --station-picks-fit-noise=0.5
--station-picks-fit-bar=2.4 --station-picks-fit-unserviced`, at launch weight 0.25.

## Results so far (seeds 1001-1004, 52 weeks of 1960, paired against `ufxal025`)

| Arm | Indie lead ≥ 2 | Big-5 lead ≥ 2 | Peak 41-100 lead ≥ 2 | All debuts | LA new records/wk, never chart | Big-5 Top 10 (SE) | Top-10 artists (SE) |
|---|---:|---:|---:|---:|---|---|---|
| History | 18.5 | 0 | 16 | 14.5 | 15.8, ~10% | | |
| `ufxal025` (no picks) | 1.6 | 0 | 12.9 | 8.7 | 10.1, 13% | | |
| `djf2n05` (2, S .5, no bar) | 19.8 | 11.9 | 32.4 | 28.9 | 12.1, 20% | +4.9 (2.2) | −3.8 (2.5) |
| `djb2` (+ bar 2) | 16.9 | 2.8 | 29.4 | 26.8 | 12.1, 22% | +3.4 (3.4) | −1.0 (3.5) |
| **`djb24u`** (bar 2.4, unserviced) | **18.9** | **3.0** | 31.4 | 27.5 | 12.2, 22% | +2.5 (2.8) | −3.5 (5.0) |
| `djb24ux` (+ extra slots) | 13.7 | 3.6 | 31.0 | 25.6 | 12.5, 24% | −0.5 (1.4) | −2.5 (2.5) |
| `djb24ux1` (1 extra slot) | 10.0 | 3.4 | 22.8 | 18.6 | 11.2, 19% | +5.5 (1.8) | −4.2 (3.1) |

The full table, with the first arms and gap metrics, is in `RegionalSyncDesign.md` §9x. Bankruptcies are flat or
down in every arm.

## Open problems: what to investigate

### 1. Records below the top 10 lead about twice as often as history (main problem)
In every arm, records peaking 41-100 get the lead 1.7-2.3× as often as indie top-10 hits. History's ratio is 0.86.
The noise, the bar, the unserviced switch, extra slots and the slot count do not change the ratio. Fewer slots lower
both leads together.

What is known:
- Picks mostly put records on city lists *early* that the sales ranking would have listed later anyway. LA takes only
  ~2 more new records a week. Those extra records almost all never chart, so the never-chart share of new city records
  goes 13% → 22% (history ~10%; that history match is crude title matching).
- Working hypothesis, untested: a hit and a 41-100 record are both connected draws (above the connection line), and
  what separates them is mostly the label's launch strength and push. A DJ hearing only the draw cannot tell them
  apart. At launch weight 0.25 with no push weight in the profile, launch strength moves the draw by only ~0.1σ.

Suggested first steps:
- **Measure before changing anything.** For picked records (early station adds at age ≤ 2), split by eventual peak
  (top 10 / 11-40 / 41-100 / never) and by label tier and launch awareness. Also break down where the 41-100 lead
  comes from. Is it picks of connected records from small labels? Records the ranking would have listed anyway? Check
  whether launch awareness or label tier predicts peak among connected records.
- If launch strength separates them, one option is to let the DJ read some launch strength (the promo man's call, the
  label's reputation) alongside the draw. **Risk:** that is the Big-5 edge coming back. Watch the Big-5 lead (history
  0) and the Big-5 Top-10 cost.
- Check that the history numbers are comparable. History counts a record "on a survey" by title matching
  (`analyze_regional_break_history.py`). The model's peak 41-100 lead was already 12.9 without picks.

### 2. The Big-5 Top-10 cost is unresolved
Across pick arms it runs from −0.5 to +5.5 at four seeds. The units-only Big-5 Top 10 moves the same way, so the swing
is in sales, not chart weighting. One extra slot, which displaces nothing, gave the largest cost. Treat this as
reshuffle noise (one changed decision reshuffles the world) until eight seeds say otherwise. The four-seed paired SE
understates it. **The next concrete run:** `djb24u` on seeds 1005-1008, then score all eight.

### 3. Smaller items
- Indie top-10 gap ≥ 6 between the two cities overshoots: 19-23 vs history 15.
- The picks set a Big-5 lead of ~3% (history 0 of 20; a rate that low is still consistent with 0 of 20).

## How to run

- **Build first:** `dotnet build` (0 errors). Headless Godot loads `bin/Debug`.
- **Screen runner:** `bash SimTools/run_regional_fit_screen.sh LW "seed tag flags..." "seed tag flags..." ...`
  (52 weeks, 3 runs in parallel, ~5-7 minutes each). The profile is in `SimTools/regional_fit_profile_flags.txt`; the
  runner replaces its launch weight with LW. Use **0.25**. Output goes to
  `SimLogs/hs52op<seed>-SMP-HGRPO-<tag>-*.csv`, and the console log to
  `SimLogs/regprobe/hs52op<seed>-SMP-HGRPO-<tag>-console.log`. The runner skips a run whose console log already has
  `CHART_AUDIT_COMPLETE`.
- **`ufxal025` flags** (every arm builds on them): `--station-regional-support=standing
  --station-regional-support-rank-noise=0.5 --station-regional-support-unsold-floor
  --station-regional-support-panel-weight=0.085 --regional-fit-exposure --regional-fit-exposure-relive
  --regional-fit-exposure-relive-awareness`.
- **Example (the candidate on the next four seeds):**
  ```bash
  F="--station-regional-support=standing --station-regional-support-rank-noise=0.5 --station-regional-support-unsold-floor --station-regional-support-panel-weight=0.085 --regional-fit-exposure --regional-fit-exposure-relive --regional-fit-exposure-relive-awareness --station-picks=2 --station-picks-score=fit --station-picks-age=2 --station-picks-fit-noise=0.5 --station-picks-fit-bar=2.4 --station-picks-fit-unserviced"
  A=(); for s in 1005 1006 1007 1008; do A+=("$s djb24u $F"); done
  bash SimTools/run_regional_fit_screen.sh 0.25 "${A[@]}"
  ```
  `ufxal025` and `ufw085` already exist for seeds 1001-1008, so they need no rerun.
- **After the first run finishes:** grep its console log for `xception` and check the `STATION_PICKS` and
  `REGIONAL_FIT_EXPOSURE on` lines.

## How to measure

Python 3.14 with pandas is at `%LOCALAPPDATA%\Programs\Python\Python314\python.exe`. The bare `python` command is the
Windows Store stub.

- **City lead and stagger:** `SimTools/analyze_regional_stagger.py SEEDS TAG`. Read column `leadGE2` (and `gapGE6`) of
  the rows "top 10, indie", "top 10, Big-5", "peak 41-100" and "all Hot 100 debuts". The history counterpart is
  `analyze_regional_break_history.py`.
- **List precision:** `SimTools/score_city_list_precision.py SEEDS TAG...` (new records per city-week and the share
  that never chart). The history counterpart is `analyze_city_list_vs_hot100_history.py`.
- **Station adds around the debut:** `SimTools/analyze_hit_station_adds.py TAG`.
- **Big-5 and costs:** `SimTools/score_big5_handoff.py SEEDS ufw085 ufxal025 TAG...` (paired, against the first two
  tags). It crashes on an arm with empty output (division by zero), so list only finished arms.
  **Bankruptcies:** `SimTools/score_bankruptcies.py SEEDS TAG...`.
- **Replay (forecast before running seeds):** `SimTools/replay_dj_ear_picks.py`, `replay_onestop_station_adds.py` and
  `replay_regional_relive.py` (now with `--breadth/--bthresh/--unexp`). Replay picks do not move demand, so they can
  only forecast the lead, not costs.
- **Noise:** about 45 indie top-10 hits per seed, so one seed swings ±8 points. Four seeds are enough to see a shape;
  costs need eight.

## Pitfalls hit in this session

- **Stopping a screen:** stopping the background task leaves the runner's bash children alive, and they start the next
  runs. Kill the `bash.exe` processes whose command line contains `run_regional_fit_screen.sh`, then the `Godot*`
  processes whose command line contains `--run=hs52op…-<tag>`. Interrupted runs leave partial logs; the runner reruns
  them.
- **Config order:** `StationPicks.Configure` runs before `RegionalFitExposure.Configure` (`ChartManager.cs` ~448).
  Validate by reading the argument list, not the other class's `Enabled`. This bug produced two empty-world arms.
- **CRLF:** some `.cs` files have CRLF line endings. Preserve them when editing by script.
- **Decade A/B:** keep it for the end of the audit. Use 52-week screens per fix.
