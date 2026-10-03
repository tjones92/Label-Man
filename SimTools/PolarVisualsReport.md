# Enabled Polar comparison visuals

October 3, 2026. Implemented the user's request for comparative radar, perceived Capability/Identity/Moment bands, refusal explanation and arrangement previews. The supplied directive and review are design context; the user authorized this visual slice and clarified that possible resistance should warn, while strong pushback should open a dialogue. Earlier reports and hashes remain historical snapshots.

## Player experience

- **A&R:** “COMPARE HEARD MATERIAL” opens a comparison of the songs actually heard in the live set. A dropdown switches among heard songs; hidden songs remain hidden.
- **Cover catalog:** “COMPARE / PREVIEW” compares a named composition and its explicit heard master or demo. It does not teach or register material.
- **Studio:** the song picker previews material against the selected act, including expected room support. Checking a song immediately selects its preview. Changing the room or selected cut order recomputes the prospective arrangement using the corresponding planned master ID without allocating it. The act picker compares the same material across the known roster; alternate acts receive a demo-level estimate rather than inheriting another act's rehearsal evidence.
- **Console:** selecting a kept take shows its playback estimate. The recorded act remains fixed. Playback replaces hook/production stars in enabled take buttons.
- **Shelf:** “COMPARE PLAYBACK” reads the printed master taxonomy, rather than proposing another recording. Enabled master cards omit hook/production stars.

The six demand spokes compare the act's observed capability range, heard material, and estimated resolved arrangement. Rust spokes appear only where the observed arrangement's lower demand exceeds the act's observed upper capability. The identity compass projects toughness and sophistication; sincerity and maturity still contribute to the separate Identity band. Ghost movement shows the perceived difference between the reference and prospective/printed version. Capability, Identity and Moment remain separate bands with Low/High labels, no numerical tooltip, area score, or aggregate fit rating.

Warnings and qualified explanations precede the graph. Comparison dialogs use a bounded scroll area so Godot's initial wrapping measurements cannot expand them beyond the viewport. Rendered screenshots were inspected at 1600×900.

## Evidence and persistence

`PolarPlayerPerception` is the boundary that reads model profiles. `PolarComparisonWidget` accepts only `PolarComparisonRead`: estimated observations, fit intervals and player-known labels. It has no recording/composition/profile model input. Fit intervals, deficit highlights, identity positions, arrows and explanatory text are calculated from estimated bands. Raw realized fit and AI selection scores never enter the renderer.

Observation keys include observer, subject, evidence kind, event, subject fingerprint and perception version. Deterministic hash bias leaves the Godot RNG stream untouched. Reads persist in the player save. Reopening the same evidence does not reroll it; a stronger evidence gate or better staff can refine an existing read, and a weaker view does not erase earned evidence. Older saves without observations start empty.

Production skill controls demand uncertainty, scouting skill controls identity uncertainty, and marketing skill controls the lagged market estimate. Follow-up/rehearsal/playback reduce local uncertainty; playback does not tighten the market observation. Missing completed-week market evidence produces a broad Moment band and an explicit notice. Display parameters and public career-to-standing mapping are proposed values in `Data/PolarPerceptionTable.json`, separate from the unchanged recording calibration table.

Uncommitted player originals use explicitly provisional, unregistered demos. Previewing them does not freeze plasticity, consume a master ID or create composition rights. The pure resolver supplies prospective arrangements; printed playback uses the committed master taxonomy. Arrangement previews have no editable axis/intent controls in this slice.

## Resistance and booking

The comparison always presents a resistance notice. Its stronger warning depends on the estimated identity/stretch/capability bands and observed ambition. It is a qualitative risk read, not a calibrated probability.

At booking, actual strong resistance uses the existing `PolarMaterialFit.WouldRefuse` predicate and table thresholds, with ambition, public career standing and empty-songbook softening. The act communicates its response directly; the dialogue adds a qualified staff explanation without exposing hidden fit numbers. Unknown provisional originals are not vetoed from invented composition evidence.

The dialogue offers **INSIST — BOOK THE ROOM**, **USE THEIR OWN MATERIAL**, and **SET THIS ASIDE**. Own material selects available originals for review; it is disabled if none are worked up. Setting material aside deselects it. Both paths leave booking unpaid. Insisting passes an explicit override through the ordinary cash/time checks and charges the ordinary session once. Closing the dialogue leaves the current selection for reconsideration. No new morale, conviction, lineup or monetary penalties were invented.

Polar remains default-off. Disabled selection, booking and star displays retain the existing path. The recording/selection table SHA-256 remains `880B00DA30EFCAD28FB48486CAD42D72E30022CAD751C2073D4E2F38874D42A2`.

## Verification

| Check | Result |
|---|---|
| Build | Pass; four existing compatibility/unused-event warnings on compilation |
| Focused perception fixture, development seed 1001 | Stable reopen; nested follow-up/playback bands; staff refinement; market evidence unchanged by playback; player save restoration; old-save absence; 3,600 independently sampled fit realizations inside predicted intervals; RNG unchanged; song/master/ID mutation absent; cross-act difference; actual refusal and explicit override; disabled refusal path |
| Enabled player workflow, seed 1001 | `polar-visual-handoff-1001`; scout/sign, named cover and commission, room booking, kept takes, printed-master playback, pressing/release/promotion and three exact player-state save/load checkpoints. `polar-visual-reviewed-1001` supplies 12 inspected screenshots of the final chart/dialog layout |
| Enabled player workflow, seed 1002 | `polar-visual-reviewed-1002`; same full flow and save/load checkpoints |
| Refusal UI fixture | `polar-visual-handoff-refusal`; checked song immediately becomes the preview; actual dialogue buttons exercise shelve, own material and insist; no cash/time spent before booking; normal cost charged once |
| Existing data and behavior regressions | `POLAR_SONG_DATA_PASS` and `POLAR_SONG_BEHAVIOR_PASS`, including ownership, lineage, enabled/disabled consumers and persistence |
| Whitespace | `git diff --check` passes |

Seed 1001 still releases “Achin' Concrete” / “Down the Track” on February 8, 1960 with $262.72. Seed 1002 still releases “Aching Bridge” / “Born Under a Hard-luck Reckoning” on February 4 with $262.72. Both deliver 380 sellable copies and 120 promos, then mail two promos. This establishes no incidental economy/RNG drift along these accepting workflows; it is not a broader economic calibration.

The scripted refusal fixture is an intentionally mismatched, ambitious act with its own songbook. It tests the actual resolver and booking predicate, rather than mocking a response. It is not population prevalence evidence. No holdout seed, decade audit, default-on change, commit or push was used. Inferred performer/style mappings and the proposed uncertainty/standing values still need gameplay evaluation. The existing `MissingSingletonsTemp.cs` autoload error remains unrelated to this slice; each successful process also emitted its explicit pass marker and exited zero.

## Artifacts and reproduction

Logs, UI text dumps, PNGs, summaries and invocation fingerprints are under the ignored `SimLogs/` directory. The source/assembly handoff fingerprint is `SimTools/PolarVisualsHashes.json`.

- [Heard-material comparison](C:/Project/Label-Man/SimLogs/polar-visual-reviewed-1001-02b-heard-comparison.png)
- [Studio preview and resistance notice](C:/Project/Label-Man/SimLogs/polar-visual-reviewed-1001-04c-studio-preview.png)
- [Printed master playback](C:/Project/Label-Man/SimLogs/polar-visual-reviewed-1001-06b-master-playback.png)
- [Refusal dialogue](C:/Project/Label-Man/SimLogs/polar-visual-handoff-refusal-09-refusal-dialog.png)

From the repository root in PowerShell:

```powershell
& .\SimTools\run-polar-player-playtest.ps1 -Run polar-visual-1001-new
& .\SimTools\run-polar-player-playtest.ps1 -Seed 1002 -Headless -Run polar-visual-1002-new
& .\SimTools\run-polar-player-playtest.ps1 -RefusalUiFixture -Run polar-refusal-new
```

Use a fresh run name: the harness protects existing saves/logs. The focused headless check uses `SimTools/SaveLoadRoundTripRunner.tscn` with `--polar-player-perception-check --use-polar-fit-selection`, seed 1001, genre-market-v2 and artist-population-lifecycle enabled. `-OpenSave polar-visual-reviewed-1001` opens the completed enabled save for hands-on continuation.
