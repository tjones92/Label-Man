# Unsigned rehearsal and demo appointments

Implemented October 10, 2026 in `C:/Project/Label-Man/tmp/scouting-unsigned` using the complete working contents of `C:/Users/grohl/.codex/worktrees/scouting-continuation/Label-Man`, verified HEAD `57bd42ef2594d1bb853a47949cd40c283eb27592`. The inherited working tree contains uncommitted scouting changes. This is an isolated continuation snapshot, not a merge into `band-member-simulation`. The primary checkout only receives its necessary nested-source build exclusion, this handoff, and the portable patch. The original continuation is unchanged.

The user's request authorizes implementation. The expansion directive's earlier document-only status is historical design context, not the instruction for this continuation.

## Player behavior

After actually hearing a dated set with identified material, the player can ask an available observed bandleader (or the solo act) to arrange a rehearsal or unsigned demo. Previewing is pure; asking takes fifteen minutes and creates an actual acceptance or refusal statement. An eligible representative agrees for the named active lineup through existing band leadership; this is not a new individual voting or relationship simulation. The same encounter cannot manufacture multiple consent records. Signing remains separate, and an appointment creates no hold or signing modifier.

A rehearsal takes one hour with no hire fee in this slice. It reserves the rehearsal floor of an existing local room and every active participant. It searches existing rooms and posted dates/hours for genuine gaps between actual performance slots. Conversation tables, rehearsal floors, and the player's home studio console have distinct capacity identities; player and person conflicts still apply.

A demo takes the existing minimum studio duration, three hours, in the home budget studio workflow. It uses the existing budget hourly rate, quoted at invitation and frozen in the commitment. The fixture's Nashville quote is $60. The console must be free, and normal roster sessions cannot overlap a reserved demo. Demo appointments use the ordinary workday rather than the performance room's opening hours; their source room remains geographic and encounter provenance. This implements the existing player studio console's capacity, not a new census or shared AI studio calendar.

Cancellation before attendance costs no time or cash and releases all reservations, including while scouting is disabled. Payment happens exactly once when attendance begins, through player cash and monthly expenses. It creates no debt against an unsigned artist. Blocked attendance consumes nothing; after starting, consumed time and session costs are not refunded. Missed starts, rival signing, changed lineup or presence, and conflicting bookings prevent stale completion. Waiting retains ordinary world progression and does not travel.

Successful completion acquires exactly one controlled sample of one agreed song, with IDs, lineup, material, time, cost and context. Vocals where applicable and execution use canonical musician capabilities with stable bounded event variation and observer interpretation. No stage or material assessment is inferred from this sample. Rehearsal and demo evidence are separate kinds, excluded from live-night consistency and audience counts. No master, release, song development, additional performance, concert fee, relationship credit or potential score is created. The UI shows the exact proposed material and current participant names, an agenda even for archived notes, and the acquired sample in the notebook.

## Ownership and saves

`LocalSceneRoomService.Conversations.cs` extends the existing authoritative commitment collection with activity, full participant IDs, agreed material, fee/payment state and completed sample. Existing person-booking queries include every reserved participant. Existing performance bookings, the player console and accounting remain their owners. Quotes, snapshots and UI refresh do not consume world RNG.

`PlayerDesk.ScoutingMeetings.cs` owns invitation, waiting, attendance, payment and acquisition. `PlayerDesk.ScoutingKnowledge.cs` projects controlled evidence without turning it into another live set. `PlayerDeskPanel.ScoutingMeetings.cs` displays proposals and handles the same appointment actions; conversation attendance still opens the existing contract form.

The save envelope is v13. Missing activity fields in older appointment records default to the existing conversation kind; missing participant collections infer no additional consent. Saves without commitments remain empty. Older executables reject the newer envelope. Restore validates duration, IDs, duplicate participants, agreed material, payment/sample states and pending capacity conflicts before mutation.

## Validation

Builds pass with zero warnings and errors in both the isolated implementation and the primary checkout. The appointment fixture passes 59 checks at each of seeds 1001 and 2002. Existing return-meeting checks pass 43, scouting checks pass 131, room checks pass 83, and the zero-week full-world compressed round trip is byte-identical at seed 1001. The rendered fixture exercises actual invitation, archived agenda, cancellation, waiting, attendance, payment and notebook evidence. Its screenshots were inspected at 1600 by 900.

The new fixture chooses real reachable scheduled appearances with identified material, then controls unsigned/active/seeking eligibility to isolate consent. It checks pure previews/RNG, stale requests, named participant reservations, duplicate requests, compressed save/load, malformed participant data, cancellation/release, exact costs and duration, one bounded sample, repeated attendance, participant departure, external ownership, in-hour signing, consumed-cost retention, and disable-mode cancellation. A performance with no identified material cannot supply a fabricated appointment song. Human openings and long-run economic acceptance are not claimed. Imported assets/UID cache were reused from the preserved continuation; this does not establish clean editor import from scratch.

The final fixture-only change scrolls the completion screenshot toward the evidence notebook; gameplay sources are unchanged by that capture adjustment. Additional regression runs preceded later UI/fixture-only edits. Exact current source and assembly hashes, logs and screenshot hashes are in `tmp/scouting-unsigned/SimLogs/unsigned-validation.json`; changed-source baseline hashes are in `unsigned-source-manifest.json` beside it.

Reproduce serially with Godot 4.7 Mono from the isolated directory:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-appointment-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-appointment-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-meeting-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0
--path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-appointment-ui-check
```

`SimTools/ScoutingUnsignedAppointments.patch` is a delta against the original continuation's working contents, not against its committed HEAD and not against the primary branch. `git apply --check` passes in that original continuation. Do not apply it to a different baseline that lacks the inherited scouting changes.

Remaining seams include credible offer capabilities, private pending offers/deadlines and outside options, audience attribution, founding-history consequences, all-city openings, and human playthroughs. Rehearsal hire fees and broader studio/personnel options remain deliberately narrow in this slice.
