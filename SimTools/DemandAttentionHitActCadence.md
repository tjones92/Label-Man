# Hit-act release cadence on the adopted tax bundle

October 22, 2026. This is the next base-model screen after `ttx-v1-tax`.
The studio-parent-label probe is **off in every arm**. Its earlier `spl-v1`
results remain evidence about the previous base; adoption waits for cadence to
land and a new studio screen on the resulting base. Opening studio rosters are
not part of this change.

## Implementation and scope

The existing `--hit-act-cadence` gives a release-eligible act with a Top 40 in
the preceding 52 weeks the next label release roll once it has waited 13 weeks.
Longest wait wins, with artist ID breaking ties. The roll becomes a single if
the format decision chose an album. Label release capacity is unchanged.

The previous implementation had evolved into a radio-only launch split
(`HistoricalLabelTierBalanceResearch.md`, §17y). This screen restores the
awareness component requested for this work: with N other same-act singles on
the Hot 100, the new single receives 1/(N+1) of its artist, marketing and label
awareness lift, plus its ordinary .08 discovery floor. Its label-push and
recognition radio lift receive the same share; charting predecessors receive
the existing 10% radio-heat cut. Albums, other acts, and physical stock retain
their existing rules. The 10% predecessor cut remains a calibration assumption.

This is a **launch allocation**, not a persistent division of accumulated
awareness, a reduction in charged campaign expenditure, or a new weekly
label-wide promotion budget. It runs at the existing promotion seam used by
orphan, promo, extracted and acquired singles. No new random draw, state field,
or save schema is introduced. With no predecessor, or with the switch off, the
original floating-point expression is used exactly.

`--hit-act-retention` remains a separate arm: renew on a contract Top 40,
subject to affordability, and protect against performance departure for 52 weeks
after a Top 40, including the career ladder's own drop. Its clock is already
part of the serialized artist. Neither switch changes shipping defaults.

## Matched screen

`RunHitActCadenceAudit.ps1` fixes the exact adopted `ttx-v1-tax` flags and runs:

- Off replay: seed 1001, 265 weeks, against the archived control.
- Lane: cadence plus same-act split, seeds 1001/2002/3003, 265 weeks.
- Retention: retention alone, same three seeds and horizon.
- Both: lane, split and retention, same three seeds and horizon.

Regression checks cover cadence, tax timing, distressed master sales, founder
compensation, founder lifecycle, company accounting, economic lifecycle and
stocking. A 13-week full-world save/load check uses the combined arm.
The manifests retain the flags, assembly SHA256, exit status and completion
markers. A changed assembly stops the screen. The analyzer requires completed
runs, rejects the studio flag, and reconciles weekly seller sales with clearing.

The main outcome is distinct Top 10 records per act per calendar year, including
carryovers, with 2+ and 3+ counts. Historical 1960 counts reproduce **19 / 6**;
the three control seeds reproduce **7/15/7 and 0/2/4** (means 9.7 and 2.0).
An act moving labels is assigned once, by the seller of its first Top 10 record
observed that year. Group shares use the actual seller in each chart week.
Control studios consist only of the named labels actually present there;
the screen does not create their missing real-world peers.

The analyzer also reports prior-year Top 10 cohorts, singles per signed year,
release gaps, silence, Hot 100/Top 40/Top 10 debuts, chart overlap, Top 10 tenure,
number-one streaks, concentration, label breadth, unit shares, operating exits,
and the current GenreCatalog year-end slot benchmark error. Debuts exclude
opening catalog and use each record's first observed threshold crossing in the
run. Calendar-year tenure/streaks are truncated at year boundaries. Conversion
follows debuts to run end, so late cohorts have less follow-up. Printed historical
artist credits and simulated artist IDs are different identity systems; no
discography-based merging of two-sided records is imputed.

Three seeds supply a matched first screen, not a tight estimate of reshuffle
variance. Report per-seed changes alongside means and paired standard errors.
A cadence gain alone does not establish acceptance if concentration, tenure,
genre mix or operating survival deteriorate.

## Reproduction

```powershell
dotnet build 'Label Man.sln' --no-restore
./SimTools/RunHitActCadenceAudit.ps1 -RunTag hac-v2
python -m unittest discover -s SimTools -p test_hit_act_cadence_screen.py
python SimTools/analyze_hit_act_cadence.py --tag hac-v2
```

Use the installed Python interpreter (the Windows Store alias may be unavailable)
and an unused run tag. Godot crashed at startup inside this session's sandbox;
the successful native execution uses the approved external process route.
The failed sandbox attempt is retained separately as `SimLogs/hac-v1/`.

Results and the user's direction are recorded below.

## User direction during the screen

The user directed that cadence and retention should both be retained while the
next work examines planned release schedules and allocation among acts, backed
by 1960-64 practice. The old one-attempt-per-week ceiling is explicitly identified
as gamification and may be changed. This changes the next workstream; it does
not make a still-incomplete repeat-hit calibration successful. Both mechanisms
remain the foundation for the scheduling comparison, with shipping defaults
unchanged. Research and the concrete calendar design are in
[LabelReleaseSchedulingResearch.md](LabelReleaseSchedulingResearch.md).


## Completed screen: hac-v2

All nine treatment audits and the one off replay passed (three seeds per arm,
265 weeks, game profile). Every run uses the adopted tax bundle with studio off.
The off replay is byte-identical to `ttx-v1-tax` in **all 95 CSV streams**.
The eight mechanism/accounting check suites and the 13-week combined-arm
full-world save/load check pass. Build: zero warnings/errors. Three Python
fixture tests cover repeat identities and moving acts, lagged cohorts and opening
catalog, and deduplicated/censored 52-week departure follow-up.

| Three-seed mean | Control | Lane + split | Retention | Both |
|---|---:|---:|---:|---:|
| 1960 acts with 2+ Top 10 records | 9.67 | 11.00 | 8.67 | **12.00** |
| 1960 acts with 3+ Top 10 records | 2.00 | 1.67 | 1.33 | **2.00** |
| 1960-64 acts with 2+, per year | 7.20 | 9.07 | 7.00 | **8.87** |
| 1960-64 acts with 3+, per year | .87 | 1.40 | .87 | **1.47** |
| Singles / 52 signed weeks, lagged lifetime Top 10 acts | 1.52 | 2.17 | 1.50 | **2.22** |
| LPs scheduled / same denominator | .63 | .30 | .64 | **.28** |
| Hit acts with a recorded departure within 52 weeks | 32.44% | 38.40% | 13.03% | **16.27%** |
| Mean of annual Top 10 weeks / record | 5.75 | 5.77 | 5.68 | **5.79** |
| Major single-unit share, including off-chart | 29.91% | 29.24% | 30.15% | **30.51%** |
| Starting Small closed | 19.61% | 20.20% | 20.05% | **19.90%** |
| Starting Boutique closed | 13.68% | 12.53% | 15.04% | **13.29%** |
| Starting Independent closed | 20.38% | 19.03% | 17.01% | **19.01%** |

History has 19/6 repeaters in 1960 and averages 17.8/7.2 over 1960-64. The
combined arm improves two-hit acts by 1.67 per year (paired SE 1.12) and
three-hit acts by .60 (.31), leaving most of the gap open. Retention cuts the
52-week departure share strongly: combined minus control is **-16.17 points
(SE 3.32)**, down in every seed. The completed-cohort metric includes recorded
roster departures, including performance exhaustion; it is not an estimate of
historical contract survival. The pure retention arm improves retention most,
without materially increasing hit-act release cadence.

Combined-arm prior-year hit cohorts have median single-launch gaps of 14-15
weeks in all twelve 1961-64 seed-years, versus 20-27 weeks in control. Cadence
and the separate lifetime-at-year-start signed-week cohort are different
populations and should not be combined into one statistic. Both arm's annual
repeat counts are 12.0/2.0, 9.7/2.0, 10.0/2.0, 7.7/.7, 5.0/.7: gains do not
persist at historical scale through 1964.

### Effects to carry into the scheduling work

- Single launches are essentially flat (20,877.7 -> 20,786.3 over the full
  265-week window), while actual LP launches fall 17.3% (9,194.3 -> 7,601.7).
  The single-priority override is suppressing LP output; independent timing must
  retain real shared resources rather than making album activity free.
- Total single units, including the off-chart base, rise **858.7M -> 908.8M**.
  The mean seed-relative gain is **5.84% (SE .69 points)**. Seller/clearing
  identities reconcile; this is an economic effect, not evidence of fixed sales.
- Big-5 Top 10 share in 1961 rises **26.15% -> 36.54%**, +10.38 points
  (SE 2.91), up in all three seeds. Slot HHI rises .0197 (SE .0010). Other years
  differ: Big-5 share moves 38.8 -> 33.9% in 1960, 28.5 -> 31.4% in 1962,
  28.3 -> 27.1% in 1963 and 28.8 -> 22.5% in 1964. A pooled share would hide
  this 1961 concentration problem.
- Top 10 life increases only .034 weeks on the mean, but it remains too long
  against history. Genre absolute slot error improves in 1961/62/64 and worsens
  in 1960/63; the full yearly values are retained in the screen output.
- The tier closure changes are small relative to three-seed reshuffle uncertainty.
  There is no new historical closure acceptance claim. The combined Major unit
  share changes +.60 points (SE .95), rather than the large 1961 Top 10 change.

**Disposition:** retain cadence and retention for the next scheduling work,
following the user's explicit direction. They improve distinct mechanisms;
the combined parameters are not a completed historical calibration. The next
implementation is the research-backed label programme and calendar described
in `LabelReleaseSchedulingResearch.md`, including replacement of the artificial
weekly cap. Shipping defaults remain unchanged. Studio adoption stays deferred;
re-screen studio after that base lands.

Evidence: `SimLogs/hac-v2/plan.json`, the four group `runs.json` manifests,
`identity.json`, `screen.md`, `screen.csv`, and `review.json`. Annual chart
outcomes cover 1960-64; economic guards use the complete matched 265-week window.
The compact release-capacity and seasonality streams contain headers only, so
recorded strategy decisions are **not fired release rolls**. No actual fired-roll
count is inferred from those rows. Original failed sandbox startup is retained
under `SimLogs/hac-v1/`; all completed runs are in `hac-v2`.
