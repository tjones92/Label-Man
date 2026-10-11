# Scouting continuation: planning around agreed appointments

October 10, 2026. Implemented in `C:/Project/Label-Man/tmp/scouting-unsigned`, extending the opening reachability/pacing slice. The user's request authorizes the next slice. The expansion directive remains design context; discovering and judging an act must remain gameplay, without a prescribed first signing.

## Delivered behavior

The existing agreed-meeting agenda now appears on the city board, above listening choices, including appointments for archived acts. It appears once rather than repeating in the notebook on the same board. The notebook retains the agenda when room calendars are disabled, including the existing free cancellation action.

Scouting choices show which retained agreed starts their time would pass: planner waiting/listening, connected-town driving from the planner, individual room sets, the whole-bill action, notebook follow-up and alternative leads, and companion/hired listening. Waiting for a later appointment also shows earlier scheduled starts it would pass. The notice names the act, activity, date/time and location and appears beside the action, in its caption and tooltip. Listener notices also appear with the quoted costs.

Reaching a start exactly is safe. Attending an appointment excludes that appointment from its own notice. Cancelled, missed, completed and declined appointments are not treated as scheduled obligations. Past starts are not presented as future conflicts. The notices are forecasts based on retained scheduled commitments, not new consent checks or guarantees that a booking survives world progression.

The player can attend first, cancel, choose another bill, or continue without attending. No forced route, confirmation dialog, automatic cancellation, automatic attendance, replacement act, relationship penalty, talent ranking or signing guarantee is added. The existing clock and commitment owner still determine actual misses/cancellations. This slice covers the scouting surfaces, not every generic time-spending action elsewhere in the game.

## Ownership and saves

`PlayerDesk.ScoutingItineraryRisks.cs` projects detached existing commitment records over the interval from the current clock through the proposed action endpoint. It does not refresh status, mutate bookings, change knowledge, spend resources or draw randomness. The existing appointment owner retains reservation, consent, cancellation, fees and completion. The existing wait/listen/drive actions are unchanged.

`PlayerDeskPanel.ScoutingItineraryRisks.cs` supplies shared notice presentation. No persisted data or save version changes; v13 remains current. Archives do not release commitments or hide them from this projection.

## Verification

Final build passes with zero warnings/errors. New checks pass 19 at each of seeds 1001 and 2002, including real compressed save/load. Existing meeting checks pass 43 and opening checks pass 544 at seed 1001. The final rendered UI fixture passes; its two screenshots were inspected at 1600 by 900. The fixture verifies one agenda for an archived act, a specific conflict notice, an enabled conflicting choice, self-exclusion and cancellation removing the notice.

The new fixture books a real conversation after an actually heard set, with controlled unsigned/seeking eligibility. It verifies interval boundaries, self-exclusion, names/locations, archival, full serialized world/player and RNG purity, player restore, actual compressed save/load, free cancellation, an actual later-set wait that leaves a missed appointment, and absence of fabricated attendance/evidence. Controlled clock placement isolates time conflicts from unrelated daily market decisions; this is not a human opening playthrough.

Accepted results, exact source/assembly/patch hashes, log hashes and screenshot hashes are recorded in `tmp/scouting-unsigned/SimLogs/commitment-validation.json`. Baseline copies and source hashes are in `commitment-baseline` and `commitment-source-manifest.json`. The save fixture's temporary name was shortened to comply with the existing 48-character save-name limit.

Reproduce serially with installed Godot 4.7 Mono, from the implementation directory:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-commitment-planning-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-commitment-planning-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-meeting-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-opening-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-commitment-planning-ui-check
```

## Portable delta and remaining seams

`SimTools/ScoutingCommitmentPlanning.patch` is against the preserved scouting snapshot after the previous opening-pacing slice, not against the primary branch or committed HEAD. Patch application was checked against reconstructed baseline files and its reconstructed text compared with the implementation. Earlier unsigned/opening patches remain historical deltas, not patches to reapply to the now-extended snapshot.

Primary gameplay sources and other worktrees remain unchanged. Changes are uncommitted. Credible offers/enforceable promises, private offers/outside options, audience attribution, founding-history consequences, contrasting human playthroughs and main-branch integration remain separate work. No recruitment/economic treatment or long-run acceptance claim is introduced here.
