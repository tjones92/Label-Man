# Enabled Polar player playtest

October 3, 2026. The user requested this playtest; the supplied directive and review were design references. This continues the calibrated recording slice in `PolarRecordingRealizationReport.md`. Polar remains default-off; each playtest explicitly enables it before population generation. No tuning, default-on change, holdout consumption, commit, push, or decade audit was performed.

The current player loop passes on development seeds 1001 and 1002 after repairing a reproducible save defect. This is a scripted walkthrough of the real Godot desk, public player actions, and autoload chain, with rendered screen inspection. It does not constitute a human usability sign-off or completion of the proposed Polar comparison UI.

## Defect found and repaired

Saving after booking a studio session retained the spent cash but discarded the console on load. In `polar-player-1001-c`, The Harmonies paid $144 for two takes each of the named cover and commission; loading removed the pending session. `CaptureState` omitted it, and `RestoreState` explicitly set it to null. A comparison of only the old save DTO therefore missed the loss; the subsequent live-session assertion detected it.

`PlayerSaveData.Session` now captures the booked artist, studio tier, hours, cost, date, material choices (including heard master/demo IDs), takes, and kept-take indices. Plain DTO lists rebuild the readonly runtime collections. Written-song choices relink to the restored songbook entry so printing marks that actual entry recorded. Restore does not charge money or roll takes again. Missing session fields in older saves clear the console, preserving their existing behavior. This is an additive save field under the existing v3 envelope.

The only runtime changes in this pass are session capture/restore in `Systems/PlayerDesk.cs` and the session DTOs in `Systems/SaveGameService.cs`. These files already contained uncommitted Polar work, which was preserved. New runner, scene, and PowerShell launcher files provide reproducible player-path coverage.

## Walkthrough and results

Both scenarios found an Ex-Musician label in Memphis with the normal $800 capital. They scouted the clubs at opening time, heard and followed up real prospects, signed an R&B act at its requested terms, taught a named cover, waited for rehearsal, commissioned and received a named song, selected those two compositions in the studio, kept the second take of the first cut, printed two masters, coupled a two-sided single, ordered 500 discs with 120 promos, dated release for the plant's arrival, shipped, and mailed two promo copies.

Actual desk button signals exercised roster/manage navigation, cover teaching, commissioning, song checkboxes, room booking, take selection, and printing. Founding/scouting/signing, time advancement, pressing/scheduling, and mailing called the same public verbs the UI uses. Morning-paper modals were dismissed through their real button before UI interaction or capture. Screens use the normal `UIManager.OnClick_Desk` entry.

| Development seed | Signed act | A-side / B-side | Release date | Ending cash | Live prospects checked | Autonomous cuts / promos checked |
|---|---|---|---|---:|---:|---:|
| 1001 | The Harmonies | Achin' Concrete / Down the Track | Feb 8, 1960 | $262.72 | 3 | 3016 / 274 |
| 1002 | Lincoln Webb & the Rangers | Aching Bridge / Born Under a Hard-luck Reckoning | Feb 4, 1960 | $262.72 | 2 | 2398 / 220 |

Three real gzip save/load checkpoints pass per scenario: a cover in rehearsal, a paid pending session, and the released/promoted two-sided single. The enabled flag is deliberately perturbed to false before each load and restored by the save. Player-state comparisons account only for the expected added load notification; heard references, explicit song identities, takes and kept indices have separate live-state assertions. The final seed-1002 verification freezes the serialized comparison before saving to avoid aliasing mutable objects.

Both printed masters retain separate durable metadata and finite realized fit. The selected cover's explicit demo reference survives rehearsal/session reload and remains a demo at printing. Composition writer credits remain intact. The released disc retains both composition IDs, both master IDs, and B-side fit after the shelf master is removed and after reload. The plant delivers exactly 380 sellable copies and 120 promos; mailing consumes two promos. Zero marketing budget and normal cash/time gates were retained.

The live-set samples have supported lengths and distinct cover IDs; they include Blues and R&B acts. They are not a Rock-family census. Seed 1001's repeated walkthrough produces identical printed-master identities, taxonomy, components and traits. This is repeat-start evidence, not unsaved RNG continuation equivalence.

Album checks inspect actual autonomous projects created while the player advances days. Across the two scenarios, 494 projects supply 5414 fitted cuts and 494 promo singles. Each fitted cut links its correct composition/master; every checked promo reuses its on-album master, song and component traits without another execution penalty. The desk currently has no player album-creation action. Consequently this is autonomous album/promotion runtime coverage, not a player-created album test.

Build passes with the four previously documented compatibility/unused-event warnings. Existing `PolarSongDataChecks` and `PolarSongBehaviorChecks` pass, including disabled behavior and old-save ownership/migration. Focused session checks cover an absent legacy session, explicit-zero takes, kept-take index, paid cost/date, and written-song object relinking. `git diff --check` passes.

## Player-facing observations and remaining boundary

- Material catalogs, repertoire and takes still use hook/production stars. No comparative Polar radar, perceived Capability/Identity/Moment bands, refusal explanation, or arrangement preview is available. This limits the player's ability to understand why a choice suits an act, although no AI aggregate fit score appears on the tested screens.
- Long scouting/distribution rows can require horizontal scrolling in the current desk layout. The initial HUD overlap was a harness artifact caused by directly opening the panel; using the normal desk entry removes it. Early screenshots also contained the normal morning-paper modal; final captures dismiss it.
- Blues has no professional commission catalog in the first scouted sample. Its commission attempt correctly reports no available writer. The successful scenarios choose real R&B prospects with available commissions. The report does not call that supported empty-pool response a regression.
- These two R&B recordings are almost fully capable under the inferred adapter. The walkthrough does not adjudicate the experience of a severe mismatch, forced failure, or transformative historical cover. Rock live-set population balance, persistent fog-of-war observations, and longer economic cohorts remain unvalidated here.

The gameplay/save path is ready for continued hands-on play with the enabled save. Completion of the directive's player-facing comparison experience remains separate work.

## Artifacts and reproduction

Final visible capture: `SimLogs/polar-player-screens-1001-*` (eight inspected PNGs, UI text snapshots, invocation/assembly/table fingerprints, console and summary). Second-seed verification: `SimLogs/polar-player-verify-1002-*`; its preceding `polar-player-final-1002-*` run has the same scenario results. Existing regression logs are `SimLogs/polar-player-regression-*`. Earlier attempts remain available for the defect and harness corrections. These artifacts are ignored by Git.

The reusable launcher builds first, refuses an existing run-log family, records flags/fingerprints, and requires a success marker. Its default interactive run captures screens; `-Headless` performs the same player-path assertions without images.

```powershell
./SimTools/run-polar-player-playtest.ps1 -Seed 1001
./SimTools/run-polar-player-playtest.ps1 -Seed 1002 -Headless
# Open the completed enabled save for hands-on play; the window stays open.
./SimTools/run-polar-player-playtest.ps1 -OpenSave polar-player-screens-1001
```

Saved slots are separate from quicksave. Loading a different save restores that save's own Polar flag. `PolarPlayerPlaytestHashes.json` fingerprints the two touched runtime files, runner/scene/launcher, and this report.
