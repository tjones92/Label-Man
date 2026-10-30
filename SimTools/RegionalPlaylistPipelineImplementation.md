# Regional playlist pipeline implementation

Implemented from `RegionalPlaylistPipelineAudit.md`, 2026-10-29. The audit's
existing scripts and findings are preserved. This change supplies the mechanism
repairs and measurement/replay tools; it does not establish a calibrated policy
or resolve the Big-5 economic uncertainty.

## Behavior

- Regional sales ranks now refresh before the station meeting, alongside the
  already settled current-week units and peaks. Regional support alone also
  enables the rank refresh when the live genre-market model is running.
- Dropped records retain the four-week cooldown. Afterwards they can challenge
  ordinary Light seats, including on a full sheet, at 1.2 times the weakest
  retained ordinary Light score (minimum 0.05). High, Mid, advocacy and reserved
  trial seats are protected. Re-adds enter at Light and compete normally later.
- `--station-picks-trial-weeks=3` enables a three-meeting probation measured from
  admission at each station. The existing `--station-picks-age` controls new
  admissions only. Continuing trials precede new admissions for reserved seats;
  ordinary ranking runs first, letting a successful trial graduate and freeing
  its reserved seat. Expired/graduated trials cannot restart. Capacity still
  limits throughput; the audit did not establish three weeks as optimal.
- Omitting the trial option keeps the legacy release-age pick gate. The option
  requires station picks and regional station support. Existing extra/tier
  options still apply. Explicit player advocacy keeps its existing override.
- `--regional-fit-exposure-stations` adds an experimental exposure path. Two
  consecutive **ordinary** weeks at a station qualify; protected trial weeks do
  not count. Qualifying support exposes its own region next week, and an already
  exposed source can expose its neighbours two weeks later. Existing national
  chart and local-sales spread remain active; the earliest scheduled arrival
  wins, with the first cause winning ties. Commercial fit remains fixed, and
  unconnected records do not gain a new lifecycle. Shadow playlists cannot feed
  this path. The option requires regional fit exposure and is off by default.
- Exposure causes (`launch`, `unexposed`, `national-chart`, `neighbour-sales`,
  `station-sustained`, `neighbour-station`) accompany exposure ages and survive
  regional save/load. Older saves have unknown cause. As before, station playlist
  history itself is session runtime state, not a saved historical audit trail.

## Decision capture and exact static replay

`--station-decisions` writes `<run>-station-decisions.jsonl`. Every exported
meeting includes capacities, policy settings, advocacy reservations, scored
candidates, eligibility exclusions, prior tiers, probation state, current rank
week, release age, launch footprint, exposure age/cause, resulting seats with
source, rejections and initial re-add cutoff. Rejections on incumbents identify
drops. Eligibility exclusions have no score because production does not draw
their candidacy noise. Captures describe evidence **at the meeting**, before
end-of-week exposure scheduling. They can be large; capture is opt-in.

The live meeting and replay share `Systems/Radio/PlaylistMeeting.cs`. Replay
checks every resulting seat, ordering, source, rejection and cutoff:

```powershell
dotnet run --project SimTools/PlaylistMeetingTests -- path/to/station-decisions.jsonl
```

Optional arguments write a one-meeting policy sensitivity **only after** exact
replay passes: output path, trial duration, re-add margin.

```powershell
dotnet run --project SimTools/PlaylistMeetingTests -- input.jsonl alternative.jsonl 4 1.3
```

This holds captured scores, eligibility and incoming state fixed. It does not
propagate altered history, regenerate candidates, relax captured cooldowns,
forecast demand, or estimate label finances. Old playlist CSVs cannot supply
missing candidacy evidence and cannot be used as exact meeting captures.

## Observer contract

`regional_observer_panel.json` fixes the first two LA FullService station IDs
and first Minneapolis FullService ID in station-ID order, without selecting on
outcomes. Reuse it across paired runs. The scorer validates the IDs and cities,
and reports both that fixed panel and all eligible city stations. The existing
all-combinations sensitivity script remains available.

```powershell
python SimTools/score_playlist_pipeline.py --records <run>-records.csv --playlists <run>-station-playlists.csv --singles <run>-single-launch-quality.csv --panel SimTools/regional_observer_panel.json --out SimLogs/regprobe/pipeline-contract
```

Optional `--decisions <run>-station-decisions.jsonl` adds explicit trial,
graduation, sustained ordinary rotation, local sales break and exposure events.
The ordinary playlist-only metrics use two consecutive listings at the **same**
station as a persistence measure, reporting start and confirmation separately;
this alone does not establish graduation. `firstAudition` in those metrics is
the first observed listing, not inferred DJ-pick provenance.

Defaults are releases in weeks 5–38, with national outcomes measured over each
release's first 12 weeks. Releases without complete observed follow-up are
excluded from outcome cohorts. Genuine first-listing throughput finds first
dates across the whole input history before applying the window. Its uncharted
count is restricted to the matched release/follow-up subset, reported separately.
Raw lead and lead conditional on enough national-debut timing opportunity are
reported together. Output includes the exact panel/window contract. “Uncharted”
means within that horizon, not never in the record's lifetime.

## Validation and next screen

```powershell
dotnet build "Label Man.csproj" --no-restore
dotnet run --project SimTools/PlaylistMeetingTests
python -m unittest discover -s SimTools -p test_playlist_pipeline.py
```

Checks cover full-sheet returns, hysteresis, cooldown exclusion, tier protection,
trial expiry/graduation, capacity, serialized replay, exposure lags, fixed-fit
gating, cross-station false confirmation, window carryovers and equal outcome
horizons. The scorer was also exercised on saved candidate seed 1001.

## Eight-seed observer screen (2026-10-29)

`python SimTools/report_playlist_eight_seed.py` reads the paired `ufxal025` /
`djb24u-pt3` 1960 runs (seeds 1001-1008, all complete) and writes to
`SimLogs/regprobe/playlist-eight-seed`. Only seed 1001 has decision capture;
its 4,004 meetings replay exactly. The other seeds give playlist histories only.

Follow-up rule check: about 10% of weeks 5-38 releases leave the export
before age 12 (2,416 of 23,686 trial-arm releases). All of them exit at age 4-10,
never chart and are never listed at any station, so the run-coverage rule keeps
them as uncharted outcomes without hiding trial losses. Of the 1,172 seed-1001
trials, 142 graduate and 1,030 end with a recorded drop; none is left open.

Paired 8-seed deltas, release 5-38 / peak in 12 weeks, full-world panel
(fixed 2+1 panel in brackets), mean ± SE:

| cohort | lead ≥2 wk | opportunity-adjusted | gap ≥6 | listed by age 2 |
|---|---|---|---|---|
| indie Top 10 | +10.0 ± 1.4 [+6.2] | +19.3 ± 2.3 [+11.6] | +5.3 ± 2.4 [+5.5] | +26.3 ± 1.8 |
| Big-5 Top 10 | +3.5 ± 1.1 [+3.0] | +12.1 ± 4.2 [+10.0] | −2.0 ± 1.0 [−2.0] | +32.6 ± 6.9 |
| 41-100 | +14.0 ± 1.1 [+8.6] | +15.1 ± 1.1 [+9.3] | +4.8 ± 1.1 [−0.4] | +14.7 ± 1.2 |

The indie lead rises (from 1.7% of 298 pooled), but the trial seats also list
Big-5 hits earlier (30% to 62% listed by age 2), and the Big-5 lead cohort has only
50 opportunity records. Capacity binds: a median meeting has 11 eligible
admissions, leaves 10 unseated and opens at most 2 trials. Graduation takes
12% of trials. The baseline runs have no returns, so return rates are not paired.
These are observer results: they do not test demand, shares or finances.

## Same-build economic screen (2026-10-29)

`ufxal025` predates this build, and re-adds plus the rank refresh are not
flag-gated, so the paired figures above mix the trial policy with the build
change. The same-build control `djb24u-pp` (DJ-ear picks, no trials) now exists
for seeds 1001-1008, as does `djb24u-pt3s` (`pt3` plus
`--regional-fit-exposure-stations`). Outputs are in `SimLogs/regprobe/playlist-econ-screen`.
Scorers: `score_big5_handoff.py`, `score_bankruptcies.py`, `score_playlist_economics.py`,
`analyze_regional_stagger.py`.

Paired deltas against `djb24u-pp`, 8 seeds, mean (SE):

| metric | `pp` level | `pt3` | `pt3s` | `ufxal025` |
|---|---:|---:|---:|---:|
| Big-5 Top 10 share | 44.8 | −5.4 (2.5) | −7.1 (2.7) | −4.0 (2.4) |
| Big-5 units-only Top 10 | 46.4 | −5.4 (2.2) | −5.8 (2.2) | −4.2 (2.5) |
| Major unit share | 36.7 | −2.4 (0.9) | −3.0 (0.9) | −1.5 (1.1) |
| Independent unit share | 23.4 | −0.1 (0.5) | +2.0 (0.5) | −1.0 (0.8) |
| Total units (M) | 120.3 | −0.0 (0.0) | −0.0 (0.0) | −0.1 (0.2) |
| Chart top-100 units (M) | 86.8 | −0.3 (0.4) | −1.3 (0.5) | +0.4 (0.4) |
| Airplay share of Top 40 | 28.9 | +0.3 (0.3) | +0.7 (0.3) | −1.4 (0.4) |
| Top-10 artists | 80.5 | −1.1 (1.3) | +1.1 (1.8) | −0.5 (2.7) |
| Bankruptcies | 37.5 | +0.3 (0.8) | +0.1 (1.0) | +1.1 (1.8) |
| Boutique bankruptcies | 7.6 | −2.0 (0.4) | −0.8 (0.7) | −0.3 (0.8) |

Stagger metric (`analyze_regional_stagger.py`, lead ≥2 weeks; history in brackets):

| arm | indie Top 10 [18.5] | Big-5 Top 10 [0] | peak 41-100 [16] | 41-100 / indie [0.86] | indie gap ≥6 [15] | LA new/wk, never chart [15.8, ~10%] |
|---|---:|---:|---:|---:|---:|---|
| `ufxal025` | 1.7 | 0.0 | 12.9 | — | 14.4 | 8.2, 13.0% |
| `djb24u-pp` | 26.0 | 6.2 | 30.2 | 1.16 | 23.8 | 10.1, 21.4% |
| `djb24u-pt3` | 14.8 | 3.6 | 26.0 | 1.76 | 19.4 | 9.4, 17.5% |
| `djb24u-pt3s` | 14.9 | 6.3 | 25.7 | 1.72 | 19.0 | 9.5, 16.9% |

Reading:

- Picks without trials sit 4.0 (2.4) above the old baseline on the Big-5 Top 10, and
  units-only moves too. This is **not** a same-build comparison: `ufxal025` predates
  the rank refresh and re-adds, so the +4 mixes picks with the build. Trials sit 5.4
  (2.5) below `pp` on the same build. Total demand does not move, so this is a
  redistribution, not lost sales.
- Trials buy list precision (LA never-chart 21% → 17.5%) and fewer Boutique
  closures, at the cost of most of the indie lead. They cut the indie lead more than
  the low-peak lead, so the low-peak ratio gets worse (1.16 → 1.76).
- Station-led exposure fires about 1,500 times per seed and pre-empts most
  neighbour and national spreads, yet it leaves every lead and stagger figure
  unchanged. It shifts about 2 points of unit share from Majors to Independents and
  lowers the Big-5 chart offset by 1.3. On these figures it should not be enabled.
- `score_bankruptcies.py` used to map closures to tiers by label name. Names repeat
  (an Independent "Decca Records" sits beside the Major label_0004), so per-tier
  splits from it were misattributed, and same-name closures were counted once. It
  now keys by labelId from `exitReason`. No Major goes bankrupt in any of these arms.

## No-trial picks: where `pp`'s Big-5 gain comes from (seed 1001)

Static checks on the seed-1001 `pt3` capture (2,600 Top40/FullService meetings):

- The reserved block is filled in 97% of meetings. The two seats it holds would
  otherwise go to settled ranked records: 72% non-Big-5, mostly aged 5+ and peaking
  11-100. The picks themselves are 4% Big-5, and 41% never chart.
- The pre-meeting rank refresh lets launch-week sales rank a record at age 1. Big-5
  records are 13% of age-1/2 candidates but take 43-45% of ranked seats at those ages.
- `PlaylistMeetingTests` now takes an optional fifth argument, `extra`. Replaying the
  capture with picks added on top leaves pick seats unchanged and gives the ranking
  back 1.0k weighted spins (+2.6% airplay). Only 28% of that goes to Big-5, about
  their 30% share of all spins, so it is neutral between label groups at meeting level.

A same-build no-picks control (`ufxal025-nb`, `ufxal025` flags on this build) and
`djb24u-ppx` (`pp` + `--station-picks-extra`) were then run on seed 1001 only:

| seed 1001 | `ufxal025` (old) | `ufxal025-nb` | `pp` | `ppx` | `pt3` |
|---|---:|---:|---:|---:|---:|
| Big-5 Top 10 | 36.0 | 32.5 | 41.5 | 36.9 | 34.8 |
| Big-5 units-only Top 10 | 37.7 | 33.3 | 43.8 | 40.6 | 34.4 |
| Non-Big-5 spins, age 5+ (k) | 25.87 | 26.01 | 24.59 | 25.93 | 25.53 |
| Big-5 spins, age 1-4 (k) | 1.08 | 1.89 | 1.57 | 1.75 | 1.49 |
| Indie lead ≥2 [18.5] | — | 7.8 | 25.6 | 26.7 | 14.6 |
| Peak 41-100 lead ≥2 [16] | — | 14.4 | 29.3 | 38.5 | 26.5 |
| Indie gap ≥6 [15] | — | 25.0 | 18.4 | 35.6 | 14.9 |
| MidTier unit share | — | 33.1 | 34.0 | 30.5 | 32.7 |

On this seed the rank refresh raises young Big-5 spins without raising the Big-5
Top 10 (32.5 against 36.0 on the old build). The gain in `pp` goes with the block:
non-Big-5 records lose 1.4k spins at ages 5+, and Big-5 hits take the sales. Extra
slots restore those spins in full (more than the 0.63k the static replay predicted,
so they propagate). They also remove about half of `pp`'s Big-5 gain and keep the
indie lead. But they raise the low-peak lead and overshoot the indie gap ≥6.
This is one seed: Top 10 share and lead each swing about ±7 per seed, so none of
these differences is resolved. `ppx` was not extended. Even at best it keeps `pp`'s
pick selection, which is the real problem (below).

## Eight-seed same-build control (2026-10-29)

`ufxal025-nb` now covers seeds 1001-1008. Paired against it (SE):

| arm | Big-5 Top 10 | units-only | Major unit share | MidTier unit share |
|---|---:|---:|---:|---:|
| `djb24u-pp` | +7.6 (2.3) | +5.7 (2.5) | +2.2 (1.3) | −2.3 (1.0) |
| `djb24u-pt3` | +2.2 (1.0) | +0.4 (1.0) | −0.2 (0.9) | −0.7 (1.1) |
| `djb24u-pt3s` | +0.5 (2.0) | −0.0 (2.3) | −0.9 (1.0) | −2.1 (0.8) |
| `ufxal025` (old build) | +3.6 (0.9) | +1.6 (1.6) | +0.7 (0.5) | −0.2 (0.9) |

Control level: Big-5 Top 10 37.2, bankruptcies 36.0. Its stagger metric, lead ≥2
(history in brackets): indie Top 10 6.1 [18.5], Big-5 4.3 [0], peak 41-100 16.1 [16],
all debuts 12.8 [14.5].

- No-trial picks raise the Big-5, mostly through sales. The build change (rank
  refresh, re-adds) moves the other way by 3.6, which hid part of the picks effect
  in every comparison against `ufxal025`.
- Without picks, low-peak records lead at the historical rate. The 41-100 excess in
  every pick arm is therefore pick selection, not the base model.

## Pick selection study (seed 1001)

`--station-pick-signals` writes `<run>-station-pick-signals.csv`. It has one row per
candidate aged ≤4 at each regional meeting. Each row records the seat it got, and
everything a fit pick could read at that meeting: launch footprint,
held-at-launch, national fit log, latent draw, launch awareness, push, the keyed DJ
noise `djZ` (heard = nationalLog + S·djZ), and the connection line. It fires with
picks on or off and draws nothing. `ufxal025-nbs` (control + export, seed 1001)
is byte-identical to `ufxal025-nb` in records, playlists and label finance.

`study_pick_selection.py RUN_PREFIX OUT_DIR` uses that world, where no outcome
was shaped by a pick. Output is in `SimLogs/regprobe/pick-selection`.

AUC, Top 10 against peak 41-100, among live singles at age ≤2 (55 indie and 22
Big-5 hits; 276 and 96 low records):

| signal | indie | Big-5 |
|---|---:|---:|
| best local sales rank at age 1-2 | 0.91 | 0.90 |
| best ordinary candidacy score | 0.89 | 0.88 |
| national fit log | 0.66 | 0.83 |
| latent draw | 0.65 | 0.80 |
| push | 0.63 | 0.51 |
| launch awareness | 0.55 | 0.55 |
| not held at launch | 0.53 | 0.52 |

Launch awareness does not separate a hit from a 41-100 record; the working hypothesis
in `DjEarPickHandoff.md` §1 fails here. Early local sales do. For indie records the
fit draw is weak because launch strength barely moves it at weight 0.25.

Rule replay at the LA and Minneapolis Top40/FullService meetings: 2 picks among
unseated, launch-exposed live singles at age ≤2, counting records listed ≥2 weeks
before national debut. This adds to the control's own lead.

| rule | indie Top 10 | Big-5 Top 10 | 41-100 | ratio | Big-5 share of picks |
|---|---:|---:|---:|---:|---:|
| current (heard, bar 2.4, unserviced) | 12.7% (7) | 4.5% (1) | 25.3% (70) | 1.99 | 8% |
| noise-free fit | 7.3% (4) | 0 | 8.3% (23) | 1.15 | 2% |
| current, all launch-exposed | 10.9% (6) | 9.1% (2) | 28.8% (79) | 2.64 | 17% |
| heard + 2 × launch awareness | 10.9% (6) | 9.1% (2) | 21.8% (60) | 2.00 | 11% |
| heard − 0.5 log local rank | 21.8% (12) | 9.1% (2) | 24.2% (67) | 1.11 | 12% |
| heard − 1 log local rank | 20.0% (11) | 9.1% (2) | 22.3% (62) | 1.12 | 14% |

A programmer reading early store reports alongside his ear halves the ratio. Launch
awareness does not. Caveats: one seed (7 against 12 indie hits). The replay ranks by
the heard score alone, without the meeting's common factors. It also cannot model
picks turning records into hits, which is why the live `pp` increments (indie +20,
41-100 +14 over control) differ from the current rule's here. Those three caveats
limit it to ranking rules against each other; it cannot forecast leads or costs.

### Eight seeds (2026-10-29)

`ufxal025-nbs` now covers seeds 1001-1008. On every seed, records, playlists and label
finance are byte-identical to `ufxal025-nb`. `study_pick_selection.py` takes several
prefixes (`PREFIX... OUT_DIR`), pools records, and pairs each rule against the current
one seed by seed. Output is in `SimLogs/regprobe/pick-selection-8seed`.

Pooled AUC, Top 10 against peak 41-100 (418 indie and 224 Big-5 hits; 2,241 and 673 low
records). Per-seed range in brackets:

| signal | indie | Big-5 |
|---|---:|---:|
| best local sales rank at age 1-2 | 0.90 [0.88-0.91] | 0.91 [0.86-0.96] |
| best ordinary candidacy score | 0.88 | 0.89 |
| national fit log | 0.68 | 0.83 |
| latent draw | 0.66 | 0.81 |
| push | 0.62 | 0.52 |
| launch awareness | 0.61 [0.55-0.70] | 0.52 [0.41-0.61] |
| not held at launch | 0.54 | 0.56 |

Rule replay, pooled lead rates, then the paired difference from the current rule
(mean (SE) over 8 seeds, points):

| rule | indie Top 10 | Big-5 Top 10 | 41-100 | ratio | Big-5 share of picks |
|---|---:|---:|---:|---:|---:|
| current (heard, bar 2.4, unserviced) | 17.0 | 4.0 | 23.7 | 1.40 | 7.8 |
| noise-free fit | −8.0 (1.5) | −2.9 (1.0) | −14.8 (0.9) | 0.95 | −2.2 (0.8) |
| current, all launch-exposed | −0.9 (1.1) | +5.8 (1.4) | +0.8 (0.6) | 1.53 | +10.0 (0.8) |
| heard + 1 × launch awareness | −3.3 (0.9) | +0.6 (0.6) | −1.7 (0.3) | 1.59 | +4.8 (0.7) |
| heard + 2 × launch awareness | −6.1 (1.0) | +0.1 (0.8) | −4.0 (0.7) | 1.79 | +8.4 (1.4) |
| heard − 0.5 log local rank | +4.4 (1.2) | +0.9 (1.0) | −0.4 (0.5) | 1.08 | +8.5 (1.0) |
| heard − 1 log local rank | +2.6 (1.3) | +0.9 (1.0) | −2.4 (0.7) | 1.07 | +10.7 (1.3) |

- Seed 1001 overstated the current rule's problem. Pooled, its ratio is 1.40, not 1.99,
  and the local-rank gain is +4.4 indie points, not +9.
- The ordering holds. Early local sales rank separates hits on every seed, and
  −0.5 log local rank is the only rule here that raises the indie lead. It does so
  without raising the 41-100 lead, which brings the ratio to 1.08 (history 0.86).
  −1 log rank lowers the low-peak lead more but gives back indie hits.
- Launch awareness lowers both leads and worsens the ratio at every weight. Drop it.
- The cost: local rank doubles the Big-5 share of picks (7.8% → 16.3%) while adding
  only 0.9 (1.0) to Big-5 hits led. Big-5 records sell well locally in launch week
  whether or not they become hits. The live screen must watch the Big-5 Top 10. `pp`'s
  +7.6 came from displacement, so more Big-5 picks may add to that.
- The noise-free rule has the best ratio (0.95), but only because it leads half as
  many hits. It is not a candidate.
- The caveats above still apply: no common factors, no pick feedback. Use the table
  to rank rules against each other, not to forecast live leads.

### Indie tastemakers in the replay

The pick-signal export now has a `djArchetype` column (`ufxal025-nbs2`, byte-identical to
`nb` on all 8 seeds). Tastemaker jocks hold about 28% of Top40/FullService stations
(11-16 per seed) and 0-3 of the 6 LA/Minneapolis panel stations. In the replay, a
Tastemaker who skips Major-label records cuts the Big-5 share of local-rank picks
from 16.3% to 12.7%. The indie lead (+4.4) and Big-5 hits led stay the same: other
stations still pick the Big-5 records that become hits. Skipping only the Big-5 gives
the same figures.

## Live local-rank screen (2026-10-29)

New opt-in flags, both requiring `--station-picks-score=fit`:

- `--station-picks-fit-local-rank=W`: the pick order is heard − W·ln(local sales rank).
  The bar still reads the heard log alone, and records with no local rank are not picked.
- `--station-picks-tastemaker-indie`: a station whose lead DJ is a Tastemaker picks no
  Major-label record.

Arms: `djb24u-lr05` (`pp` + local rank 0.5) and `djb24u-lr05t` (+ tastemaker-indie),
seeds 1001-1008. `djb24u-ppck` (`pp` rerun on this build, seed 1001) is byte-identical
to `pp`, so the code is inert with both flags off. Output is in
`SimLogs/regprobe/local-rank-screen`.

Paired against `djb24u-pp`, mean (SE) over 8 seeds:

| metric | `pp` level | `lr05` | `lr05t` |
|---|---:|---:|---:|
| Big-5 Top 10 share | 44.8 | −2.1 (1.9) | −3.0 (1.7) |
| Big-5 units-only Top 10 | 46.4 | −1.2 (1.7) | −2.2 (1.7) |
| Major unit share (all) | 31.9 | −0.7 (0.8) | −0.9 (0.6) |
| indie Top 10 lead ≥2 | 26.0 | −1.8 (1.3) | −5.0 (2.5) |
| peak 41-100 lead ≥2 | 30.2 | +0.4 (1.4) | +0.7 (1.3) |
| Big-5 Top 10 lead ≥2 | 6.2 | −2.5 (1.9) | +4.5 (1.8) |
| bankruptcies | 37.5 | +0.6 (2.0) | +2.1 (1.2) |

Against `ufxal025-nb`: Big-5 Top 10 `pp` +7.6 (2.3), `lr05` +5.5 (1.3), `lr05t` +4.6 (2.2).
The low-peak ratio (41-100 lead / indie lead, history 0.86) is 1.16 for `pp`, 1.27 for
`lr05` and 1.48 for `lr05t`.

Reading:

- The replay's gain does not survive live. Local rank leaves the 41-100 lead where `pp`
  has it, and the indie lead falls slightly, so the ratio gets worse. The Big-5 bump the
  replay predicted did not happen. The Big-5 Top 10 eases by 2.1, which is not resolved.
- A likely reason is that the replay cannot see feedback. A record already selling in
  its city is near the national threshold, so a pick moves its debut forward and uses
  up the lead the replay credited. Indie Top 10 hits debut at age 3.53 under `lr05`
  against 3.64 under `pp`. That is consistent, but too small to prove the explanation.
- The tastemaker exclusion lowers the Big-5 Top 10 most (−3.0), but it costs indie lead
  (−5.0) and adds closures (+2.1 (1.2)). The Big-5 lead rises by 4.5 (1.8), against
  history 0. Do not enable it.
- Neither arm beats `pp`. The pick-selection problem stays open. Static replays cannot
  rank pick rules while feedback is unmodelled; screen candidates live.

Live captured-meeting equality now exists for seed 1001 only. Before selecting a production trial duration or
enabling station-led exposure, inspect that capture and the capacity/progression
results, then run the audit's narrow paired economic screen. Static results
cannot validate demand feedback, artist outcomes, shares or bankruptcies.
