# Big-5 per-launch premium: handoff for Astra (2026-11-17)

Branch `stature-fanbase`, nothing committed. Full detail: `SimTools/Big5VolumeAndPremiumHandoff.md` (running log and plan),
`SimTools/LpGenreAllocationFollowup.md` §49-59, `SimTools/MajorShareRootCauseHandoff.md` (2026-11-13 decomposition),
`SimTools/GameyBehaviorAudit.md` (2026-11-12 audit).

## The problem

In 1960-61 a Big-5 single converts to the Top 10 at 2-4x a field single's rate. History shows about 1.2x. Because of this, every
lever that adds Big-5 volume (history: ~18% of issues; sim: 7-13%) pushes Big-5 chart share further from history (1961 Top 10:
sim ~32%, history 20.6%). The premium has to come down before the volume can go up.

**The author's question for you:** is there a game-y model somewhere (a cliff, a gate, a double count, an all-or-nothing read)
that manufactures this premium? The aim is a fix where the premium falls out of behavior (labels, DJs, distributors, dealers,
buyers acting on what they can see), not a tax on the Majors and not a hand-tuned handicap.

## Standing decisions (the author's)

- Five historical US majors only (`--historical-majors`). Majors keep national distribution breadth (§56, sourced).
- No Major tax. Closures must be earned. `--zero-fee-material-gate` stays on.
- Score levers on the **per-launch** premium (Big-5 ÷ field Top 10 per launch), on 4 seeds, with the same sign on 3 of 4.
  Single-seed Top 10 share moves ±10 points on reshuffle alone.

## What is measured (don't redo)

| Suspect | Finding | Where |
|---|---|---|
| Hit lottery (commercial fit draw) | label-blind; Big-5 and field connect alike | MajorShareRootCause 2026-11-13 |
| Act selection depth (slate pool) | capping it raised the premium; picks no better | handoff, candidate 0 |
| Label production | craft cap moved premium within noise | handoff, candidate 2 |
| Hooks / song choice | +0.03 from A&R picking (scouting-scaled shortlists and reads) + better self-writers; ≤20% of premium | §58 |
| Genre / taste fit | not the premium | handoff, taste fit |
| Launch promotion (impact, push, stock) | inert for newcomers | §51, §55 |
| Label purchase-rate scale | double count; removed (`--label-demand-parity=1`, adopted) | MajorTop10ShareFableHandoff |
| Faster held-region arrival | raises Big-5 share, erases indie city lead | §9x, `ca` |
| Lift non-Major footprint | closes Big-5 ÷ Small, Big-5 share flat (slots are zero-sum) | `dose-v1` |
| **Thin launch depth for Major newcomers** | premium 2.86 → 1.84 (3/4 seeds), passes stagger lead guard; **but** probably carried by fit exposure, which the code defines as "servicing" a region, and §56 says majors serviced DJ copies nationally. Parked as possibly ahistoric. | §59 |

Connected-newcomer trace (`--newcomer-connect-trace`): Hot 100 entry is a weeks 1-3 sales bar and label-blind at matched
units. A field newcomer covers 3.4 of 7 regions; uncovered regions stay held until weeks 5-6, after the entry window.
Big-5 newcomers are exposed in 6.5 regions vs 4.1 and sell ~1.5x in weeks 1-3.

## Suspects worth a fresh look (my list, not checked against history yet)

1. **Chart success opens everything in one week.** The national Top 40 lifts the arrival hold, fit exposure, city reach, a 0.4
   awareness floor and rack shelf at once (`CityReach.Spread`, `RegionalFitExposure.UpdateWeek`, `RegionalAudience.TryArrive`).
   Of ~26,000 held region-launches a run, ~400 open by chart and ~16,000 never open. History: indie hits broke city by city
   and still charted; local distributors and DJs picked up records with action. Is access a cliff where history had a slope?
2. **Fit exposure is a launch-time coin flip per region** (`RegionalFitExposure.Launch`: 0.3 + 0.7 × label reach). An unexposed
   region can't respond to the record's fit until the spread rules or the chart open it. Is that a label-reach gate standing in
   for what was really DJ choice?
3. **Steep quality response.** For a hit, units scale about quality^14.6, against a designed q⁴. The terms stack: purchase q⁴,
   audience 0.3 + 0.7q, radio q^1.8, word of mouth q^2.2 × (q − .45), organic growth × q, the top-40 gates, then the singles
   ration at exponent 1.35. Small label-driven edges become large chart edges.
4. **The singles purse.** Intent is 7-8x a fixed purse; ~13% clears; the ration keeps the order of titles. Median single sells
   69-90 copies; flops sell almost nothing (the fit comment promises "a few hundred"). Winner-take-all by construction?
5. **Everything keyed to label scouting.** Free professional-song arrivals (royalty licences: fee 0), read bands, demo reads,
   material-search reads all scale with `scoutingAbility` (Major envelope .68-.96 vs Small .34-.84) (§58).
6. **Who gets released.** Majors score 34-42 candidates per slot (Small 2.8) with a momentum priority; hot acts are 38% of Big-5
   launches vs 8%. Opening stars hold 57% of Top 10 weeks. Cadence: Big-5 hit acts 3.76 Hot 100 records a year vs 1.86
   (history 3.28 vs 2.18).
7. **The unexplained ~17%:** weekly distribution after launch (restock in uncovered regions, rack shelf, one-stop terms).

## How to work

- Reference runs: `SimLogs/zfg-{2002,1001,3003,4004}-sc` (104 weeks). Traces: `nctrace-{2002,3003}-sc`. Stage-1 arms:
  `mnfoot-*`, `mjfoot-*`. All in `SimLogs/` (never commit).
- Arm: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File SimTools/run_request_arm.ps1 -Reference
  SimLogs/writing-calibration/zfg-<seed>-sc-request.json -Name <arm>-<seed>-sc -Seed <seed> [-Weeks 27] -Extra "--a,--b"`;
  four at once, ~20 min each, ~450 MB each (~38 GB free at handoff).
- New switches: off by default, no new draws, a check, a 27-week off run byte-compared to the reference.
- Scorers: `analyze_footprint_scope.py SEEDS FMT...` (pooled premiums, shares, finance), `analyze_major_volume.py RUN...`,
  `analyze_newcomer_exposure.py RUN...`, `analyze_newcomer_hook_source.py RUN...`, `analyze_regional_stagger.py SEEDS FMT`
  (guard: Big-5 Top 10 city lead ≥ 2 / gap ≥ 6, control 9.4 / 3.8, history 0 / 0; indie 26.9 / 24.7, history 18.5 / 14.6).
- Build `dotnet build "Label Man.csproj" -c Debug` (never while runs hold the DLL). Checks: Godot console exe `--headless --path .
  SimTools/SaveLoadRoundTripRunner.tscn -- --<name>-check`. Python: `py`.

## Uncommitted work at handoff

`Systems/LabelBlindLaunch.cs` (scope switch) + `SimTools/LabelBlindLaunchChecks.cs`; `Systems/MajorVolumeSwitches.cs` and its
check and hooks (§57); scorers listed above; doc §56-59; this file and `Big5VolumeAndPremiumHandoff.md`.
