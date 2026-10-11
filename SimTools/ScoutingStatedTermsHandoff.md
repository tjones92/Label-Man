# Scouting expansion: what they said at the table

October 10, 2026. Continues the user's implementation request in the existing attached checkout, `C:/Users/grohl/.codex/worktrees/scouting-loss-followthrough/Label-Man`, branch `codex/scouting-loss-followthrough`. The attached directive remains design input; its older document-only status does not replace the user's authorization. The preceding lost-lead slice and all inherited scouting changes remain uncommitted for review.

## Baseline and chosen scope

`SimLogs/scouting-terms-baseline.json` records exact touched-source hashes before this slice, with the preceding validated state recorded in `SimLogs/scouting-loss-validation.json`. The primary band checkout and earlier scouting source checkout are preserved.

The existing session owner requires a roster act and label studio; unsigned rehearsal/demo scheduling and consent are still unsupported. This slice advances the notebook's business evidence through the existing contract owner instead. It adds no appointment, private offer market, acceptance modifier, promise, relationship balance or economic feedback.

## Delivered behavior

For acts with acquired scene performance evidence, opening an actual contract discussion records its stated starting terms and source at the current day/minute. Easy counters preserve the actual counter package, managed objections preserve only the words actually returned, and declined or voluntary withdrawals retain the actual response with the correct speaker. A completed canonical signing records the agreed public terms. A failed commit never produces an agreement.

The existing owner still controls approach eligibility, baseline generation, negotiation state, cooldown, action costs, acceptance and signing. Recording does not generate another ask, spend additional time, consume RNG, infer private motives, grant access or override consent. Merely editing a player offer, refreshing, revisiting the pad, or failing a preflight check acquires no statement. Repeating a withdrawal is deduplicated.

Each genuine new discussion gets an ID with a persistent label/act ordinal, separate from its statement occurrences. Reopening and save/load keep the same ID. A new discussion begun while acquisition is disabled clears the earlier identity; acquisition after re-enabling starts with its next real response and does not backfill an opening. Statements quote the act's side or named manager from the actual opening sheet; voluntary withdrawal is attributed to the player's label. No member is invented as a speaker and no original listening room is misrepresented as the location of the terms meeting.

The concise notebook keeps the latest acquired opening and two recent responses, or three responses where the opening is unknown. Full acquired statement history remains in Sources & Coverage. Every summary identifies its historical date/source and says current availability and terms must be checked again. Listening alone leaves business wishes unknown. Musical observations and public rival history remain distinct.

## Persistence

`BusinessStatement` is appended to the evidence enum. Optional statement metadata and a six-field public `ScoutingStatedTerms` payload serialize through the existing player knowledge owner. This payload excludes manager archetype, negotiation difficulty, reservation, utility, posture, patience and hidden thresholds. The active discussion ID serializes with the existing negotiation state; its ordinal belongs to the label/act dossier. v11 remains the save envelope because these are additive optional fields. Earlier saves retain their existing negotiations and gain no fabricated conversations.

Disable/observer modes preserve historical statements and the existing contract behavior while suppressing new statement acquisition. There is no new AI, recruitment, recognition, development, contact or contract-economic reader and no long-run economic acceptance claim.

## Verification

Final build passes with zero warnings/errors. Business-statement fixtures pass 44 checks at seed 1001 including a real compressed save/load, and 42 at seed 2002. Coverage includes actual opening/counter/objection/withdrawal/signing paths, blocked unaffordable offers, historical snapshot retention, discussion identity, free refresh/reopening, source/time, legacy live negotiations, and off/observer acquisition. Fixtures use canonical artists, real listening and actual contract action APIs, with controlled eligibility, clock, posture and funds to isolate responses. These are deterministic ownership/save checks, not human opening or balance acceptance.

Scouting regressions pass 131 checks each at seeds 1001 and 2002; room compatibility passes 83 at seed 1001. These compatibility checks ran before the final counter-provenance text expansion; final statement and rendered checks ran afterward. All game probes ran serially.

The final real rendered fixture passes `SCOUTING_TERMS_UI_PASS`: it clicks DISCUSS TERMS and NOT NOW and verifies the dated public ask and player withdrawal in the evidence surface. `SimLogs/scouting-terms-ui-v1/board.png` was visually inspected at 1600x900: notes wrap within the scrollable paper with no overflow. The final UI log has no error entries. `git diff --check` passes. Final source, assembly, log and screenshot SHA-256 hashes are in `SimLogs/scouting-terms-validation.json`.

Reproduce serially with Godot 4.7 Mono:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-terms-check --scouting-terms-save-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-terms-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-terms-ui-check

## Remaining roadmap

Consent-based unsigned sessions and their scheduling/capacity/commitment owner remain, as do pending private offers/deadlines, enforceable capability promises, acceptance outside options, audience attribution, founding-history consequences and human playthroughs. These sourced contract statements provide a foundation for later work; no acceptance or behavioral channel is enabled here.
