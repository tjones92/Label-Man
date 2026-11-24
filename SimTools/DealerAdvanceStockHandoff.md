# Dealer advance-stock experiment handoff, 2026-11-17

Branch `stature-fanbase`. The experiment asks whether reducing physical copies stocked per unit of follower intent changes first-week stockouts, three-week sales, and Hot 100 entry. The preceding investigation is in `SimTools/Big5VolumeAndPremiumHandoff.md` and the local `SimLogs/audit-booked-return.cjs` results. No balance change was adopted or committed.

## Experiment and current evidence

The two arms replay each seed's 104-week `zfg` request. Both add `--follower-stock-audit`. Only treatment adds `--follower-single-stock-share=0.25`. The control stocks 100% of each regional follower book. Treatment rounds 25% of regional intent to physical copies for AI singles. Both retain the ordinary shelf, LP stocking, regional availability rules, and follower intent calculation. Following, release mix, and chart positions can diverge as the simulated world responds.

| Completed treatment seed | Archived control Hot 100 entries / Big Five launches | Quarter-stock entries / launches | Quarter-stock mean three-week sales, all Big Five | Quarter-stock mean sales, Big Five books ≥50,000 |
|---|---:|---:|---:|---:|
| 2002 | 129/416 (31.0%) | 109/340 (32.1%) | 4,580 | 14,084 (62 launches) |
| 4004 | 104/338 (30.8%) | 107/391 (27.4%) | 4,450 | 18,907 (66 launches) |

Completed two-pair results are in `SimLogs/shelf-two-pairs.json`. Both full controls reproduced the archived CSVs. Among Big Five books ≥50,000, mean three-week sales fell from 22,944 to 16,571 (−27.8%) and entry from 88.5% to 77.3%, both down in each seed. First-week pre-ration stock pressure rose from 3.85% to 100%; no title in either large-book group exhausted a positive-sale shelf in week one. Across all Big Five launches, mean three-week units fell from 5,127 to 4,511 (−12.0%).

The opposite all-release entry directions make a pooled chart conclusion premature. Among these 128 large-book treatment launches, every release had a region where first-week pre-ration intent exceeded on-hand copies. None sold its last copy in a region after selling at least one there that week. Shelf exhaustion and pre-ration inventory pressure answer different questions. The preliminary score file is `SimLogs/shelf-preliminary.json`; rerun the current scorer for its additional empty-shelf and grant metrics.

A matched opening-week example verifies the treatment mechanism before release slates diverge. Seed 2002, The Sunny Teens, `gen_457`, had identical 61,082-copy follower books, launch traits and a 5,044-copy baseline shelf. Advance copies fell from 61,082 to 15,271; the three-week book granted 60,458 versus 29,723 units before market clearing. Actual sales fell from 84,566 to 38,421. Neither arm sold out a region after a positive first-week sale. This one title illustrates how stock limits demand admitted to clearing even when copies remain after rationing.

## Runs in flight at handoff

Two lower-memory workers were launched with `LABELMAN_WORKSTATION_GC=1`. Worker A runs seed 2002 then 1001. Worker B runs 4004 then 3003. Each runs `shelf-q25-SEED-sc` then `shelf-base-SEED-sc`. At the last check both first treatments had reached `CHART_AUDIT_COMPLETE` at week 104; both control reruns also completed week 104. The 1001/3003 treatments are now running, with their controls queued. Workers were left running; check the logs before restarting. Session IDs, if still available, are 67634 and 90646. Files persist independently of those handles.

Earlier attempts named `dealer-*` failed Godot initialization in the sandbox. Some `dealersim-*` baselines ran partially; 1001 and 3003 hit memory exhaustion with four simultaneous server-GC workers. Exclude those runs from the final screen. The ordinary solution build passed with zero warnings and errors. Partial control prefixes for all four seeds matched the archived records, launch, finance, slate and programme-choice CSV bytes. Full control validation subsequently passed for 2002 and 4004; 1001 and 3003 remain pending.

## How to finish

1. Inspect the tails of `SimLogs/writing-calibration/shelf-*-sc.log` and the corresponding errors logs. Require the explicit `CHART_AUDIT_COMPLETE` marker for each run. The wrapper sometimes prints a blank exit code. Check existing workers before retrying. If a run is incomplete and its worker is gone, use a fresh run name because the runner protects existing CSVs. Use at most two concurrent workers with workstation GC.
2. These commands reproduce a single arm from the repository root. The installed Godot runtime required running outside the sandbox here.

```powershell
$env:LABELMAN_WORKSTATION_GC = '1'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File SimTools/run_request_arm.ps1 -Reference SimLogs/writing-calibration/zfg-1001-sc-request.json -Name shelf-q25-1001-sc -Seed 1001 -Extra '--follower-stock-audit,--follower-single-stock-share=0.25'
```

   For the control, use `shelf-base-1001-sc` and only `--follower-stock-audit`. Substitute the same seed in the reference, name and seed parameter.

3. Once all eight runs complete, run the following from the root. The audit helpers and CSVs live under the local ignored `SimLogs` directory; they are present in this workspace but will not accompany a branch checkout elsewhere.

```powershell
node SimLogs/verify-dealer-controls.cjs 1001 2002 3003 4004
$runs = foreach ($seed in 1001,2002,3003,4004) { "shelf-base-$seed-sc"; "shelf-q25-$seed-sc" }
node SimLogs/score-follower-stock.cjs @runs | Set-Content -Encoding utf8 SimLogs/shelf-final.json
node SimLogs/report-follower-stock.cjs SimLogs/shelf-final.json
```

4. The verifier compares each complete control to archived `zfg` across five outputs and checks assembly identity and paired flags. The scorer joins weeks 53–92 launches, counts entry through launch week +12, and sums regional cleared units over launch week and the next two weeks. It cross-checks three-week units against chart records wherever present. Check `withoutSettlement` before interpreting zeros. Report paired seed effects, all Big Five vs field, and books ≥50,000 (defined by intended orders, independent of the stocking fraction). Compare `soldOut` (a positive sale exhausted a region), `emptyWithDemand` (any region empty with demand), `shortStock` (intent exceeded stock before rationing), and `granted / asked`. Groups are dynamic; only the seed and starting state are matched.

## Working-tree changes

`Systems/Media/MediaFollowerDemand.cs` adds the optional scale, default 1. `Systems/Media/MediaAtmosphereService.FollowerDemand.cs` applies it when stocking singles. `SimTools/ChartAuditRunner.cs` adds the optional post-clearing observer, writing regional shelf balance, cleared copies and demand before reorders. Other pre-existing branch changes belong to previous work.

