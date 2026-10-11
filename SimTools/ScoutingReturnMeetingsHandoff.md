# Scouting return meetings

Continuation in `C:/Users/grohl/.codex/worktrees/scouting-continuation/Label-Man`, branch `codex/scouting-context-comparisons`, inherited HEAD `57bd42ef2594d1bb853a47949cd40c283eb27592`. The user's continuation request authorizes implementation. The attached expansion directive's original document-only status remains historical design context. The primary checkout and original scouting checkout were preserved. This worktree includes earlier uncommitted scouting work; this is a continuation slice, not a claim that all inherited changes are committed or integrated.

## Player behavior

After hearing an actual set, the player can ask an available observed member to return for a record discussion. A pure preview proposes a later slot at that room. Asking consumes fifteen minutes with no fee; willingness is learned only through the response. Acceptance reserves the named speaker and one conversation slot. A refusal records the actual words without reserving anything. The same encounter cannot manufacture repeated consent.

The A&R notebook shows agreed meetings even when the related notes are archived. The player can wait through ordinary time, attend at the agreed start, or cancel without spending time or money. Waiting advances day events, ordinary expenses, and competing decisions; it stops if the appointment becomes unavailable. It does not travel to another city. Attendance consumes the full hour, ends the earlier listening visit, checks availability again, and opens the existing contract form. Signing and subsequent offers retain the existing contract owner's costs and eligibility, identity, roster, and cooldown checks. Scheduling or attending creates no concurrent listening evidence.

Booking reserves time, not signing access. Rival ownership, a departed speaker, a move or changed availability, conflicting performance work, or a missed start can end the appointment. A speaker is never silently substituted. An availability change during the consumed hour prevents stale completion or terms. Cancellation releases the real booking. No relationship score, contract discount, acceptance modifier, act hold, private offer, or enforceable capability promise is introduced.

## Ownership and persistence

`LocalSceneRoomService.Conversations.cs` extends the existing room owner. `SceneRoomSaveData` carries authoritative conversation identities, consent responses, statuses, time slots, and an ordinal. Existing person-booking queries now include pending conversations. Search uses already scheduled weeks, posted room dates/hours, the player's hard stop, actual scene presence, and existing performance bookings. It creates no bill, performance, room, session, or economic ledger. Conversation capacity is one slot per room at a time; this is a conversation table, not an exclusive performance-room booking.

`PlayerDesk.ScoutingMeetings.cs` requires acquired listening and an actual available member, owns the player actions, and retains sourced appointment statements in acquired knowledge. Quotes, summaries, and previews are read-only. Repeated action notices are deduplicated. Existing acquired evidence remains separate from mutable appointment status. Off/observer modes preserve commitments/history and suppress new actions/acquisition; cancellation can still release a commitment while scouting is off.

`PlayerDeskPanel.ScoutingMeetings.cs` renders invitation/agenda controls and hands successful attendance to the same contract UI used by DISCUSS TERMS. `PlayerDesk.ScoutingKnowledge.cs` projects the latest appointment into the dossier summary; source details retain earlier consent and notices.

The save envelope is v12 because appointments are authoritative commitments. Older supported saves default to an empty appointment collection and infer no consent. Older v11 executables reject the newer envelope rather than silently discard commitments. Restore validates structural identities, ordinal/count, room dates/hours, repeated encounters, and conflicting pending commitments before mutating room state.

## Verification

Validation results, exact source/assembly hashes, baseline comparison, logs, and screenshot hashes are recorded in `SimLogs/scouting-meetings-validation.json`. `SimLogs/scouting-meetings-baseline.json` records the source before this slice.

The build passes with zero warnings/errors. Appointment checks pass 43 checks at each of seeds 1001 and 2002, and scouting regressions pass 131 at each seed. Terms checks pass 42, room checks pass 83, and comparison checks pass 32 at seed 1001. The zero-week full-world round trip is byte-identical. The rendered meeting fixture passes, and both its agenda and contract-form screenshots were visually inspected at 1600×900. The final fixture-only adjustment captures the accepted agenda before cancellation to avoid displaying an old cancellation message after restoring the test branch; gameplay sources are unchanged by that adjustment.

Reproduction commands for Godot 4.7 Mono, run serially:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-meeting-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-meeting-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-terms-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-comparison-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0
--path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-meeting-ui-check
```

The appointment fixture begins with real listening to a canonical artist, then controls unsigned/active/seeking eligibility to isolate consent. It includes real compressed save/load, pure preview/RNG/time checks, stale quotes, speaker/room conflicts, cancellation, missed starts, flag retention, external signing, an in-hour availability change, roster capacity, and legacy defaults. The rendered fixture clicks the actual invitation, cancellation, waiting, and attendance controls and verifies that archived notes leave the agenda visible. It restores the accepted branch after cancellation and uses a controlled clock fifteen minutes before the meeting to isolate successful arrival from the ordinary rival signing market. The headless fixture separately verifies multi-day waiting through ordinary day events and external signing. These are controlled ownership/UI fixtures, not human opening or economic acceptance.

On this host fresh editor import previously crashed while importing existing data CSVs. Imported assets and the UID cache were reused from the preserved source checkout; the C# assembly is independently built here. Runtime/render validation does not establish a clean editor import from scratch.

## Next implementation seam

Unsigned rehearsal and demo appointments still need activity-specific capacity, participant consent, session cost/cancellation rules, and bounded recording evidence. This return-conversation commitment owner provides a foundation; it does not itself authorize or create an unsigned recording session. Credible offer capabilities, private pending offers/deadlines, acceptance outside options, audience attribution, founding-history consequences, all-city openings, and human playthroughs remain. No long-run economic acceptance is claimed.
