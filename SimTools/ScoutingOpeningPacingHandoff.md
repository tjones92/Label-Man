# Scouting continuation: opening reachability and pacing

Implemented October 10, 2026 in `C:/Project/Label-Man/tmp/scouting-unsigned`, extending the preserved unsigned-appointment implementation. The user's request authorizes continued implementation; the attached directive supplies design context. The user's clarification is central: logistical help must leave discovery and musical judgment to gameplay and a good ear.

## Baseline and patch

The supplied `ScoutingUnsignedAppointments.patch` adds unsigned rehearsal/demo appointments to the earlier scouting continuation's working contents at HEAD `57bd42ef2594d1bb853a47949cd40c283eb27592`. It is not a patch against the primary `band-member-simulation` checkout. Its changes were already present in `tmp/scouting-unsigned`; it was not reapplied.

This continuation edits that existing snapshot in place. Primary-branch gameplay sources and the other worktrees remain unchanged. Earlier validation manifests describe their original source states, not this extended snapshot.

`SimTools/ScoutingOpeningPacing.patch` is a delta against the unsigned implementation before this continuation, including the earlier uncommitted scouting work. Do not apply it directly to the main branch. It passes `git apply --check` against reconstructed baseline files. Baseline copies and exact before/after source hashes are in `tmp/scouting-unsigned/SimLogs/opening-baseline` and `opening-source-manifest.json`.

## Player behavior

The A&R board shows at most two next affordable local bills, ordered by actual date/time and quoted cost. Each names a posted act and room, date, set hours, wait time on the set day, listening duration and admission. Waiting and listening remain separate actions through the existing owners. A player can switch directly to all room calendars and back; that switch consumes no resources or evidence. The ordinary room board remains available.

When no affordable unheard local set is reachable, the list can show connected-town alternatives that the existing drive action can reach now. Those show drive duration and one-way gas separately from admission; the return trip remains separate. Driving never automatically waits or listens. Quotes use current cash, not a prediction of future settlement. Waiting retains ordinary world progression and expenses.

The list does not rank musical ability, infer potential, favor unsigned performers, grant introductions, reserve an act, activate prospects, spawn talent, change calendars, guarantee contracts, or prescribe a founding act. Signed and uninterested performers remain in the same calendar. The player selects where to hear music and interprets the resulting evidence. Already-heard events are excluded without changing their world history; a current set is explicitly described as partial if entered after its start.

Per-set listening controls disable admission the player cannot cover and appearances ending beyond the hard stop. Waiting rejects a future start before the workday begins before advancing time. The A&R introduction now describes actual set time rather than claiming every set takes two hours.

## Owners and persistence

`LocalSceneRoomService.AnnouncedCalendar` returns detached snapshots of already-posted calendars without ensuring or advancing schedules. The existing clock and `Calendar` retain scheduling ownership. `PlayerDesk.ScoutingItinerary` projects these facts with existing geographic reach, drive quotes, admission and acquired-event identity. Notebook follow-up uses the same local projection.

No save schema or envelope version changes: the existing v13 world/player owners retain calendars, evidence, visits and costs. Planning adds no persisted state. The show-list/browse-board switch is a transient UI preference.

## Validation

- Final build: zero warnings and errors.
- Opening fixtures: 544 checks each at seeds 1001 and 2002; all 31 founding cities and four starting-cash profiles have an affordable actual posted route. The audit changes the fixture's city viewpoint over one generated world rather than founding 124 labels. These are listening-access checks, not claims about signability or quality.
- Planning preserves full serialized world/player state and RNG continuation; save/restore reproduces the choices. Signed performers remain selectable; cash changes affordability without changing the cast. Zero-cash Nashville retains authored free listening.
- Real wait/listen checks preserve explicit choice: waiting acquires no evidence or travel; listening charges admission and saves one impression without signing; repeat clicks add no cost; heard events leave the list; late-day driving and disabled/observer scouting modes are excluded.
- Existing scouting experience: 131 checks. Existing unsigned appointments: 59 checks, seed 1001.
- Final rendered UI fixture passes the list, full-calendar switch, waiting, real listening, and no-signing checks at seed 1001. Screenshots inspected at 1600 by 900.

Headless regressions preceded the final UI-only shortening and introductory-copy changes; the final build and rendered UI fixture include those changes. `opening-validation.json` records final source/assembly/screenshot hashes and accepted logs. Native Godot crashed before engine initialization in the sandbox; fixtures ran successfully outside it.

Reproduce from `C:/Project/Label-Man/tmp/scouting-unsigned` with installed Godot 4.7 Mono, serially:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-opening-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-opening-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-appointment-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-opening-ui-check
```

## Remaining work

Contrasting human opening playthroughs, repeated missed/lost-lead pacing, later calendar windows, credible offers and enforceable promises, private pending offers/outside options, audience attribution, and founding-history consequences remain. This slice changes no recruitment/economic treatment and makes no long-run economic or clean-editor-import claim. Integration of the accumulated scouting work into the main branch remains a separate task.
