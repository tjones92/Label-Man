# Studio expansion — implementation progress

Branch: `codex/studio-experience-expansion`, based on `scouting-experience-expansion` at `f00635a`. Existing local changes in the project file, artist manager and export presets are preserved and are not part of this slice.

The October 11 studio directive is implementation guidance. Its statement that the original task produced a directive only does not supersede the current request to implement it.

## Delivered

- Esc opens a separate game menu over the current office view. Resume restores that view and input state. Save/load is removed from office chrome; founding still offers Continue and the folded-label recovery button opens the game menu.
- The game menu includes confirmed exit to the desktop, including before founding a label or creating a save. Nested paper confirmations retain Esc priority.
- New studio dates begin with no selected take. Printing validates all selections before doing work. Invalid indices do not select a performance.
- Take cards show frozen engineer, performer and producer impressions, with bounded uncertainty. No raw take bars, hidden-quality recommendation, selected-side quality summary or take-derived radar remains on the console.
- Explicit producer selection uses those same listening impressions, with stable first-take tie-breaking, and preserves selections the player has already made. This is selection delegation, not full date delegation.
- Save v14 preserves unselected dates and their notes. Legacy paid sessions keep their existing performances, cost and selected index; notes are added without rerolling takes. Earlier executables reject v14 rather than silently selecting an unselected take.
- `--profile-startup` reports elapsed and per-stage timings without consuming randomness or changing world initialization.

## Startup investigation

Seed 1001, real autoloads and normal defaults, headless Godot 4.7/.NET Debug build. Two initial runs reached the interface at 66.647 and 31.965 seconds. This is bootstrap-to-interface timing, not editor launch, engine process creation or time to the first displayed frame. Host load, JIT and filesystem warming are not controlled; the second run is not evidence of an optimization.

| Stage | Run 1 seconds | Run 2 seconds |
|---|---:|---:|
| Autoload setup plus initial artist pool | 13.747 | 3.316 |
| Rosters plus 4,000 unsigned reserve artists | 21.736 | 12.411 |
| Competitor / composition initialization | 10.190 | 5.807 |
| Regions and radio | 0.244 | 0.059 |
| Chart prewarm | 0.531 | 0.274 |
| 52 weeks of room history | 14.623 | 7.822 |
| Live room calendar | 2.470 | 1.552 |
| Player/UI initialization | 3.106 | 0.724 |

The final timing markers additionally separate naming-data/autoload initialization, initial artist creation, roster assignment and unsigned reserve creation. The slow work runs synchronously before the player interface is ready. Population construction and historical room scheduling deserve the next focused performance pass; chart prewarming is a small part of the delay. Do not shorten room history or cut the unsigned population merely to improve this timing. A loading surface would make progress visible but would not reduce this work.

A refined seed-1002 run reached interface readiness at 33.165 seconds: naming data 0.281s, remaining autoload setup 0.455s, initial artist pool 2.338s, roster assignment 7.986s, unsigned reserve 4.618s, competitor/catalog initialization 6.154s, room history 8.425s and live room calendar 1.709s. These measurements narrow the follow-up investigation to roster assignment, population creation and room scheduling rather than naming-data loading or chart prewarming.

Raw runs and checks are in ignored `SimLogs/studio-*.log`. A sandboxed Godot invocation crashed before engine initialization; the same executable ran successfully with approved execution outside the sandbox.

## Validation

`dotnet build --no-restore -v quiet`

```powershell
& $godot --headless --path . SimTools/StudioFoundationRunner.tscn -- --seed=1001 --profile-startup
& $godot --headless --path . SimTools/StudioFoundationRunner.tscn -- --seed=1002 --profile-startup
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --scouting-session-promise-check
```

The foundation runner loads the real UI and tests menu restoration, nested confirmation priority, unselected print refusal, observation stability and gameplay-RNG isolation, JSON and compressed save/load continuation, legacy migration, evidence-based selection, two-side master creation and exactly-once printing/time/cash. Headless checks do not replace a human visual/input playthrough.

Final results: build succeeded with zero warnings/errors; foundation checks passed 23 assertions on each of seeds 1001 and 1002, including the actual studio scene's disabled print control and absence of raw take bars; the scouting funded-session regression passed 65 assertions. `git diff --check` passed for this slice.

## Original slice boundary

This is a selection/evidence foundation, not the complete early date milestone. Booking still uses tiers and batch-generated takes, spends the booked time upfront and keeps the established realization/economic paths. Observation generation has a private keyed stream; performance generation and final material realization still use their existing random owners. The listening notes are coarse impressions of the current Hook/Production model, not a detailed diagnosis of actual vocal/ensemble/capture components.

Next: map all realization consumers; introduce stable named local room/capability snapshots; persist an incremental action clock, protected source takes, arrangement revisions and cross-song workload; then preparation/direction, finishing and full date delegation. Preserve existing funded-session promise semantics, session-player/payment owners and calibrated composition application. Broader economic or autonomous behavior changes need explicit off/observe/enabled states and separate validation. Human playthroughs through pressing remain required for the complete milestone.

## Incremental working date (second slice)

New player dates use stable fictional local room identities and a saved mono-layout/rate/craft snapshot. Local quality limits the available room selection; sparse cities receive a modest recording service rather than three universal choices. Rates and the existing room-cost recoupment owner are retained. The quote separates room and booked musician cost, includes stock/playback/house finishing, and checks that setup, a pass per song and finishing fit before accepting payment.

Only 20 minutes of setup pass at booking. Rehearsal takes 15 minutes, a pass 12, direction and a break 10 each, and finishing 10 per side. Work is incremental and protects finishing time. Other desk jobs and calendar skips are blocked during the paid date. The current own lineup is checked against overlapping shows/appointments and reserved for the quoted window. An actual paid start keeps the existing funded-session promise; abandoning does not refund incurred costs.

Passes now have distinct vocal, ensemble, feel and capture components. Rehearsal improves coordination within bounds; cross-song workload causes fatigue, and breaks partially recover it. Directions trade one aspect against another, respect basic lineup applicability and can be declined under creative control. Earlier performances and kept choices survive every subsequent action. Engineer, performer, band and producer notes are frozen attributed impressions, with no raw quality cards or truth-based tie-breakers.

The player can delegate remaining work under a capture, tighten or explore brief. Delegation uses the same actions and visible listening impressions, preserves manual choices, limits retries, and reserves time for the flip and finishing. It leaves final review and printing to the player. Masters retain room, selected performance, direction, observations, participants and work history.

Save v15 persists empty/unselected sides, preparation, room snapshot, action number, workload, private take identities and master history. Legacy paid batch dates remain batch dates with their original takes and payment. New take generation and observation use private keyed draws; saving/reloading and consuming unrelated gameplay RNG cannot reroll the next identical action. Final material realization retains its existing RNG owner and is not covered by that private-take guarantee.

Player rollout is `--studio-experience=off|observe|enabled`, default enabled. Off and observe retain the previous batch behavior; observe currently has no additional telemetry. No autonomous production or shared economic defaults were changed.

### Realization owners

`PlayerDesk.PrintMaster` still prepares and applies selected material once. `SongMaterialApplicationService` remains the sole capability/composition blending owner. The new performance calculation does not apply a second capability penalty. Existing competitor singles/promoted masters call `Apply`; album tracks and seeded catalog tracks use `ApplyIdentityToAlbumTrack`. `SongMaterialSelectionService` prepares selection candidates through the existing polar behavior. Those shared consumers are unchanged.

### Validation and remaining scope

`StudioDateRunner.tscn` passed 39 assertions on seeds 1001 and 1002: actual UI controls/no raw bars, setup/payment ownership, availability/reservations, empty-side compressed saves, rehearsal/fatigue comparisons, direction/no-op costs, protected takes, private next-action continuation after reload, bounded delegation, finishing, master histories, idempotent printing and abandonment. `StudioFoundationRunner.tscn` with `--studio-experience=off` passed 23 assertions. The headless foundation fixture reports Godot resource leaks on exit; its assertions pass, but this is not a clean visual playthrough.

The funded scouting-session regression passed 65 assertions with incremental dates enabled. After closing the calendar-skip bypass and correcting the overhead warning's musician fee, the build succeeded with zero warnings/errors and the studio runner passed all 39 assertions again on seed 1001. `git diff --check` passed.

Startup cache comparisons and timing limitations are recorded in [StartupCacheResults.md](StartupCacheResults.md).

This is a playable incremental-date foundation, not the full directive. Persistent engineer relationships, detailed arrangements/material-member interactions, actual hired-musician identity/credits, backing crew discovery, room travel and independent recording layouts remain. The existing session-player booking/payment path is reused, including its anonymous-player fallback. Room identities are fictional and derived from the retained tier calibration, rather than a researched facility catalog. Human sessions through pressing/release, economic calibration and the broader first-three-dates onboarding remain to be evaluated.
