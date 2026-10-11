# Scouting expansion: notebook source questions

October 10, 2026. Continues the user's implementation request and the purposeful repeat-listening slice. The attached documents were used as design/context; their historical document-only restriction was not treated as the user's current instruction.

## Checkout and preserved work

This chat owns the managed checkout at C:/Users/grohl/.codex/worktrees/scouting-source-questions/Label-Man, created from codex/scouting-purposeful-followup at 57bd42e. The prior checkout belongs to another chat and could not be attached here. Its five modified tracked files and three untracked follow-up files were copied into this checkout before implementation. Its reusable Godot cache was copied for build/render verification. The previous checkout and primary band checkout were not edited, switched, stashed or committed. All scouting changes remain uncommitted in this new checkout.

The complete diff here includes the prior session's questions, comparisons and next-set controls. This session adds Systems/PlayerDesk.ScoutingSources.cs, a retained-completed-bill projection in LocalSceneRoomService, notebook source controls, source acceptance cases and the rendered source-control fixture.

## Delivered slice

The notebook now offers a room/booker selector and four specific questions: completed booking history, the next announced date, material announced for the next appearance, and attendance at the latest retained completed bill. Sources come from rooms referenced by acquired evidence in the player's current city; signed acts remain useful subjects. Selecting questions and previewing availability creates no evidence and spends no action time.

The explicit ASK BOOKER button previews one hour and no conversation fee. It explains posted-hour, hard-stop and already-recorded restrictions. The action snapshots the answer before advancing the ordinary clock, ends a continuous listening visit and saves a BookerClaim in the existing dossier. It creates no independent performance, interpretation, relationship bonus, hold, population or rival pressure.

Source scope is explicit. Announced titles are a programme, not material the player heard or proof of readiness. Attendance is the completed whole bill's count and capacity, not act-specific or portable fans. Missing records remain unknown. Booking counts say nothing about punctuality. Upcoming dates and material use genuine uncancelled appearances whose end has not passed, sorted by occurrence rather than incidental calendar order.

The existing forward itinerary omits past dates. LastCompletedBill therefore consults retained authoritative room bills and returns a detached snapshot, without extending retention or storing another calendar. Acquired source claims survive pruning. The legacy AskScoutingBooker signature delegates to the new action and retains history/date evidence namespaces, preserving deduplication for saved answers.

An ephemeral quote captures both evidence identity and answer content. Button actions recheck it before spending: cancellation, changed programme or a revised time on the same bill cannot silently consume an hour. Repeating the same stored answer, including after save/reload, cannot buy more support.

## Ownership and saves

No save-envelope bump or parallel knowledge/relationship store. Existing v11 observation fields retain source/contact, room, bill reference, occurrence/announced date, receipt date/time, claim text and optional whole-bill count/capacity. Existing dossiers, listening questions and negotiations retain their owners. Off/observer modes block actionable new source previews and acquisition; historical notes survive.

## Validation

Final dotnet build: zero warnings and errors. Final scouting fixtures pass 85 checks each at seeds 1001 and 2002. New scenarios cover cost-free previews, invalid sources/questions, retained past bills, source scope, exactly one conversation hour, no fee, visit interruption, stale quotes, changed time on the same bill, cancellation, save/reload deduplication, retained acquired claims after pruning, missing-count uncertainty, programme versus listening evidence, subhour closing, hard stop and observer mode.

Existing serial room fixture passes 83 checks at seed 1001; information fixture passes 43. They ran before the final source quote was tightened to include answer content; the affected final scouting fixtures and rendered UI ran afterward. No world behavior, recruitment, recognition, development or contract-economic reader was added, and no new long-run economic acceptance is claimed.

Rendered --scouting-source-ui-check exercises the real listen action, question selection and ASK BOOKER button, then checks the saved source claim, one-hour action, no fee, visible source scope and disabled repeat explanation. The 1600x900 screenshot was visually inspected: wrapped notes and selectors/actions fit the scrollable paper. Final UI log has no ERROR entries. git diff --check passes.

Artifacts in SimLogs:
- scouting-sources-build.log
- scouting-sources-final-1001.log and scouting-sources-final-2002.log
- scouting-sources-rooms.log and scouting-sources-information.log
- scouting-sources-ui-final.log
- scouting-source-ui-v1/board.png and ui.txt
- scouting-sources-validation.json (source, assembly and log hashes)

Earlier fixture runs exposed two test setup errors: a negative pre-world date and an existing completed bill sharing the fixture's date/end time. The controlled fixture now uses a valid later date and isolates its source record before asserting selection. Final reruns pass.

Reproduce serially with Godot 4.7 Mono:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-information-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-source-ui-check

## Remaining expansion

This completes a source-question slice of Phase 2. Consent-based rehearsal/demo appointments, independent listener expertise, sourced courtship/outside options, enforceable capability promises, audience attribution and human opening playthroughs remain. Any later behavioral feedback still needs the baseline's established long-run acceptance before enabling it.
