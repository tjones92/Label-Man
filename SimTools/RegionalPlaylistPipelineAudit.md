# Regional radio → national success: static audit, 2026-10-29

The current candidate matches an early-appearance statistic on an oversized city panel. It does **not** yet demonstrate a durable station-led route to national success. The largest newly identified problem is that a DJ trial often expires before the record becomes a local seller, and the playlist machinery then effectively prevents that station from taking it back.

This investigation used the **eight existing, completed runs**: `ufxal025` and `djb24u`, seeds 1001–1004. No simulator seeds were launched, no economic forecasts were inferred from fixed-demand replays, and no production behavior was changed. The handoffs were treated as evidence and hypotheses, including their suggested runs, rather than as instructions to execute those runs.

## 1. An early pick can destroy its own route into sustained rotation

The candidate's two pick slots apply only while the **record** is at most two weeks old. This is not a two-week trial measured from the station's first add. An age-two add has only one eligible week. At age three it must immediately win a sales-ranked seat.

When it fails, `CommitPlaylist` marks it dropped. Four weeks of lockout follow. After that it is still excluded from the normal ranked pool: re-adds can take **leftover Light capacity only**, and cannot displace an incumbent. With a full candidate field, this is effectively a permanent exclusion, however strong the later candidacy becomes. The baseline sheets are full in 100% of observed station-weeks; candidate sheets are full in 96.8%. The candidate averages only 0.054 empty seats per sheet, so unfilled reservations are not the main capacity problem.

Code: `Systems/Radio/StationNetwork.Playlist.cs`, age gate at line 383, re-add routing at 387–394, leftover-only seating at 454–469, drop bookkeeping in `CommitPlaylist`.

Measured across all Top40/FullService stations, live singles launched in weeks 5–44:

| Eventual national peak | Early adds still on that station at age 3 | Identifiable trailing pick-seat adds still there at age 3 |
|---|---:|---:|
| Top 10 | 60.4% | 22.5% |
| 11–40 | 30.3% | 6.6% |
| 41–100 | 17.1% | 2.1% |
| Not charted during the run | 4.3% | 0.3% |

The second column includes early adds through the normal ranking. The third isolates first station appearances in the trailing reserved seats: rank >33 on Top40 or >34 on FullService. All **10,158** such early adds satisfy age ≤2 and the reconstructed heard-fit bar ≥2.4. This validates the seat attribution without pretending the CSV exports an explicit pick reason.

Tracing first spells through drops, for launches in weeks 5–38 and a 12-week post-launch sales horizon:

- **710** station/record pairs for eventual top-10 records dropped by age three. **708** subsequently reached that region's sales top 40 after the four-week lockout. **None returned to that station.**
- The corresponding 11–40 counts are **1,522 early drops**, **1,363 later local top-40 cases**, and **zero returns**.
- For 41–100: **2,999**, **790**, and **zero**.
- Across the entire early-add drop cohort, neither arm contains a same-station return. A separate full-playlist check of seed 1001 also found zero returns among 17,335 baseline and 19,563 candidate station/record histories.

Local sales rank ≤40 does not prove a record should beat every station's genre-adjusted cutoff. It does prove that meaningful later demand often arrives after the station has locked itself out of ordinary reconsideration. The control flow confirms that even an arbitrarily strong re-add cannot challenge a full sheet.

This is the first mechanism to repair or explicitly redesign. The anti-oscillation policy may be intentional; its interaction with short DJ trials undermines the intended pipeline.

## 2. The headline city comparison uses twice as many model stations

The historical sample is **KFWB + KRLA in LA, KDWB in Minneapolis**. These model runs pool **four LA stations and two Minneapolis stations**. All six model stations are FullService in these runs. More stations mean more chances to be heard early, to appear in both cities, and to include a record that never charts.

I replayed **all 12 combinations** of two LA and one Minneapolis model station per seed, averaging combinations within each seed. No favorable subset was selected.

| Lead ≥2 weeks | Historical benchmark | Candidate, original 4+2 panel | Candidate, average 2+1 panel | Baseline, average 2+1 panel |
|---|---:|---:|---:|---:|
| Indie top 10 | 18.5% | 18.9% | **11.3%** | 1.0% |
| Big-5 top 10 | 0/20 | 3.0% | 1.9% | 0.0% |
| Peak 41–100 | 16.0% | 31.4% | **20.8%** | 8.1% |
| All national debuts | 14.5% | 27.5% | **18.0%** | 5.5% |

The conclusion that “the indie lead is solved” does not survive equal panel counts. The minor-hit overshoot is smaller under this comparison, but still present.

This is a sensitivity correction, not full historical equivalence: the historical lists have 40 positions; model FullService sheets have 36 and different programming semantics. Preserve both the full-world city metric and a fixed comparable observer panel rather than silently replacing one with the other.

## 3. Much of the inverted lead ratio is a debut-speed effect

In the candidate's national debut window, weeks 7–46:

| Cohort | Records | Median national debut age | Eligible to lead ≥2 even if added at age 1 | Actually added in either city by age 2 | Actual lead ≥2 |
|---|---:|---:|---:|---:|---:|
| Indie top 10 | 180 | 2 | 47.2% | 43.3% | 18.9% |
| Big-5 top 10 | 100 | 2 | 22.0% | 32.0% | 3.0% |
| Peak 41–100 | 1,149 | 6 | 93.6% | 29.0% | 31.4% |

The city lists actually find indie hits early **more often** than minor hits. Yet a record debuting nationally at age one or two cannot have a two-week local lead from any post-release add. The 41–100 cohort has almost twice the timing opportunity.

Among records with enough time to qualify, the observed lead rates are about **40.0% for indie top-10 hits versus 33.6% for 41–100 records**. That does not eliminate the excess minor-hit lead, but changes its diagnosis: the raw ratio is not a pure measure of DJ discrimination.

Of the candidate's 361 minor hits with a two-week lead, **82.3%** first appear in a city by age two. The extra lead is largely the early trial lane, not a gradual sales-driven station chain.

Track debut age, first audition, first sustained rotation, and first local sales break separately. Fitting one difference of dates hides which event moved.

## 4. The DJ score distinguishes connection better than eventual hit scale

I reconstructed national commercial fit from the production Philox algorithm, world seed, record ID and exported initial launch awareness, using this profile's settings. The reconstructed cap agrees with the console's `-1.335`; all identifiable early pick seats clear their configured heard-fit threshold.

For live records launched in weeks 5–44 that appear in a city by age two:

| Eventual peak | Records | Median fit above cap, log units | Median initial awareness |
|---|---:|---:|---:|
| Top 10 | 111 | 2.641 | 0.425 |
| 11–40 | 192 | 2.629 | 0.270 |
| 41–100 | 345 | 2.631 | 0.193 |
| Not charted during run | 394 | 2.634 | 0.162 |

The fit medians are nearly identical, including the uncharted cohort. This follows the kinked distribution: above its kink, the slope is **0.15**, versus **2.5** below it. The candidate's hearing noise is **0.5** log units. The score has little information about national scale once it has found plausible connections.

Among these early records, fit distinguishes top-10 from 41–100 outcomes with an AUC of **0.562**, close to chance; launch awareness improves that to **0.669**. These are descriptive, within-run associations, not a validated forecasting model. Also distinguish `Connected` in code (above the reference median cap) from being above the distribution's hit-share kink; they are not the same threshold.

But a launch-strength filter is not the answer to the lead target. In fixed-outcome replays, rejecting early observations below awareness 0.2 reduces indie/minor leads to **8.3% / 18.9%**. At 0.3 they become **5.6% / 15.8%**. Stronger launches disproportionately identify records that chart nationally too soon to provide the desired lead.

The original hypothesis was partly right about launch strength predicting peak, but missed this interaction with debut speed.

## 5. Static alternatives do not yet produce a satisfactory candidate

All figures below retain the original 4+2 city observer panel and the recorded national outcomes. They do **not** predict new sales or Big-5 costs.

| Static intervention on candidate | Indie top-10 lead ≥2 | Peak 41–100 lead ≥2 |
|---|---:|---:|
| Recorded lists | 18.9% | 31.4% |
| Exclude identifiable trailing pick seats | 1.7% | 11.8% |
| Require two consecutive weeks, date from first week | 15.0% | 24.8% |
| Require two consecutive weeks, date from confirmation | 7.2% | 20.8% |
| Early-list awareness threshold 0.3 | 5.6% | 15.8% |
| Two pick seats, retain each admitted trial for 3 weeks | 12.2% | 23.2% |
| Two pick seats, retain each admitted trial for 4 weeks | 10.0% | 22.1% |

The retention replay freezes the sales-ranked prefix and the original pick proposals. Trials graduate if the recorded ranked prefix takes them; otherwise they occupy a slot for the stated duration. Refused/expired proposals are not restarted. There are no unseen replacement candidates, no demand feedback, and no reconstruction of the full candidacy meeting. It tests the **capacity tradeoff**, not the final retention design.

Longer trials reduce discovery throughput when all trial weeks compete for the same two seats. Simply lengthening the age gate or raising launch-strength requirements is therefore not a screened solution. Separate admissions, probation and promotion; give successful trials a legitimate route into ordinary rotation.

## 6. Sales timing and radio causality remain inconsistent with the intended story

**Mixed-week sales support.** `ChartManager` settles sales and updates local peaks before the playlist meeting (line 1235), but refreshes `localSalesRank` afterward (line 1240). `StationRegionalSupport.Support` therefore combines a current sales/peak ratio with the previous week's rank. The station class comment says last-week support; the meeting comment says current settled sales. Neither describes the combined signal accurately. Decide on a reporting lag and apply it consistently. The first-week rank floor is especially consequential for short trials.

**Station adds do not carry fit exposure.** `RegionalFitExposure.UpdateWeek` spreads exposure from local sales rank and national chart position. It does not read a station add, advocacy commitment, or neighboring station's action. Fit picks are restricted to the *launch* footprint, so that lane cannot itself introduce a record to a newly reached region. `awaitingArrival` and `fitExposureAge` are different gates: ordinary playlist eligibility checks the former, not the latter.

**A Light pick has a small direct demand channel.** Reporter radio is blended at 0.085; Light spin weight is 0.2; regional awareness gains radio ×0.12. Even if the entire regional reporter panel carries a record at Light, its direct current-week panel contribution is at most **0.00204 regional awareness**, or **0.204 percentage points**, before the 60% regional share in effective awareness. One station contributes less. This is only the immediate term, not an estimate of accumulated tail carry or chart feedback. A trial can affect the chart immediately while its awareness benefit reaches subsequent demand.

The model can retain a fixed latent commercial fit, as the handoff's prior design choice specifies, and still make local radio consequential. It needs an actual sustained local exposure path, rather than counting a brief early appearance as evidence that the path already exists.

## 7. The weak stagger of minor hits is mostly not a capped-record problem

Among baseline 41–100 records reaching both cities, **159 of 166 (95.8%)** have national fit above the cap. They still show only **9.4%** gap ≥6 weeks. In the candidate, **233 of 248 (94.0%)** are above cap, with **17.2%** gap ≥6.

Thus capped/flop records cannot account for most of the missing stagger in this cohort. The remaining distinction—launch-exposed in both markets versus exposed late—is **not directly exported**, so this audit does not claim to resolve it. Demand-derived conversion jumps are an indirect proxy affected by other multipliers. Export `fitExposureAge` and exposure cause before treating that split as measured fact.

## 8. Measurement and replay repairs needed before more screening

The old new-record scorer filters to weeks 5–44 **before** finding each city's first appearance. It therefore counts pre-window carryovers as new. Corrected full-panel figures are:

| City | Baseline genuine first listings/week | Candidate genuine first listings/week | Baseline / candidate uncharted within run |
|---|---:|---:|---:|
| LA | 8.28 | 10.34 | 13.29% / 23.14% |
| Minneapolis | 6.93 | 8.34 | 10.10% / 18.35% |

The candidate's old LA value was 12.21/week. Under equal 2+1 panels, candidate uncharted shares fall to **17.41% LA / 13.40% Minneapolis**, versus baseline **9.42% / 7.05%**. Both panel size and the window boundary matter. Historical new-listing throughput also needs a consistently deduplicated, full-year first-date calculation; do not treat its rough 15.8/week as directly calibrated against these corrected counts.

The legacy DJ replay is useful for broad intuition, not for selecting the present implementation. It uses region-level picks and a different hash/normal draw, omits the candidate's fit bar and unserviced gate, does not model per-station incumbency/drop/re-add rules, and treats a regional sales top 40 as the ordinary city list. Its fit proxy is inferred from realized demand, rather than reading the launch draw. The broader relive replay also hard-codes a rank-10 exposure plateau while this profile overrides it to rank 20; its `relive` branch changes exposure/ramp/decay but does not explicitly replay the built awareness-history rule. Existing calibrated offsets do not establish fidelity for a new station policy.

Historical matching remains approximate: title tokens plus artist fragments, unequal publication dates, and national observations extending into 1961 versus model outcomes ending at week 52. “Never” in these outputs means **not charted during the saved run**, not never in the record's lifetime. No new historical claims or external research were needed for this audit.

## Recommended next work

1. **Fix the comparison contract first:** fixed observer panels; genuine first listings; trial versus sustained rotation; matched follow-up windows. Keep raw lead and timing-opportunity-adjusted lead together.
2. **Repair trial progression and re-entry together:** measure probation from the station's add; let a convincing trial graduate; after cooldown, allow a re-add to challenge the Light cutoff with hysteresis. Preserve the anti-oscillation objective without making every drop permanent.
3. **Choose a consistent sales timestamp.** Refresh ranks before the meeting if it is meant to see settled current sales, or snapshot all sales evidence if a reporting delay is intended.
4. **Export decision evidence and make a faithful static meeting replay:** candidate scores; add source; prior tier; rejection/drop reason; rank week; trial start; re-add cutoff; launch footprint; exposure age and cause. Replay the captured meeting unchanged first, then trial/re-entry alternatives. Current logs cannot reconstruct every rejected candidate exactly.
5. **Connect qualifying station support to regional exposure**, preserving the fixed fit interpretation. Test distinct local audition, sustained response, neighbor pickup and national arrival events. Do not tune chart breadth or launch weight again merely to compensate for broken station progression.
6. Only after those static checks justify a candidate, run a narrow paired seed screen for demand, chart feedback, label shares, artists and bankruptcies. The existing four-seed Big-5 economic uncertainty remains unresolved; these static replays cannot settle it.

## Reproduce and inspect

Run from the repository root with `C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe`:

```text
SimTools/audit_regional_playlist_pipeline.py
SimTools/audit_playlist_retention.py
SimTools/replay_city_survey_panel.py
SimTools/replay_playlist_trial_slots.py
```

Each line is a Python script argument, not a simulator command. Outputs are in `SimLogs/regprobe/playlist-pipeline-audit/`: per-record cohorts, early station adds/drops, per-seed and pooled replay metrics, occupancy, all observer-panel combinations, precision counts, and trial-slot replays. Simulation output remains under `SimLogs`.

Validation: all eight source console logs contain `CHART_AUDIT_COMPLETE`; the audit reproduces the original pooled lead/stagger figures; identified early pick seats satisfy age/bar constraints; the one-week trial-subset replay reproduces the original first-appearance metrics; all four Python files pass compilation. No game build was needed because no C# behavior changed.
