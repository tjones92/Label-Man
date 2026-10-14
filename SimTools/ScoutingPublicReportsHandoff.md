# Scouting expansion: saved public signing reports

October 10, 2026. Continues the implementation request using the supplied source-question handoff and expansion directive as design context. The directive's historical document-only restriction is not the current user's request.

## Checkout and baseline

This chat owns C:/Users/grohl/.codex/worktrees/scouting-appointments/Label-Man, created at 57bd42e. The source-question checkout belongs to an earlier chat. Its six modified files and five untracked continuation files were copied here before implementing this slice, along with its reusable Godot cache. Neither the primary band checkout nor the earlier scouting checkouts were edited, switched, committed or stashed. All changes here remain uncommitted.

SimLogs/scouting-competition-baseline.json records the inherited commit, source checkout and SHA-256 hashes of each inherited modified/untracked file. The cumulative diff includes earlier listening-question, source-question and UI work; those changes are not newly attributed to this slice.

## Delivered slice

The existing local scene recap already projects committed contract records from SceneRecruitmentRecord, with a full event-week publication delay, local geography, bounded history and stable event keys. This slice reuses that owner instead of adding a second market or inventing public inquiries.

SAVE LOCAL REPORTS & MARK READ explicitly snapshots displayed signing reports into existing dossiers for acts already known to the label, including archived dossiers. Reading spends no time or money, grants no hold, produces no performance evidence and creates no dossier for an unfamiliar act. Previewing, refreshing and reopening do not acquire evidence. Only the existing displayed batch of at most eight reports is marked read.

PublicReport is appended to ScoutingEvidenceKind, preserving earlier enum ordinals. Optional observation fields store place, event week and publication day. Exact occurrence day is left unknown because the authoritative contract ledger has week precision. Source/contact, claim, event identity and actual acquisition date/minute persist through the existing v11 player save owner. No save-envelope bump or parallel knowledge store was introduced. Repeated reads and save/reload do not duplicate evidence. A saved report survives source pruning or subsequent changes to artist ownership/name and label display names because its claim text is retained.

The notebook shows the last acquired public signing report and distinguishes it from unknown private inquiries, offers or deadlines. If an act is currently signed elsewhere, the notebook lists up to three other unsigned acts already heard locally and retained on the active notebook, ordered by pursuit priority and stable identity. If no such lead exists it points to actual announced bills and booker tips. These are remembered leads, not guaranteed willing or currently signable candidates. Signed acts' future performances remain useful listening opportunities.

The daily talent-market callback still feeds actual signings into the existing morning paper. With scouting expansion enabled, it no longer converts private Nominated/LostArtistChoice callbacks into an unsourced 'A&R heard' notebook warning. The legacy callback remains when the expansion is explicitly disabled. Existing crowding, handshakes, market choices, actual offers, AI budgets, recruitment, recognition, development and contract economics were not changed. This is a limited public-information slice, not completion of all rival courtship instrumentation.

## Validation

The final compiled scouting fixture includes ScoutingCompetitionChecks: publication delay, preview invariance, world/RNG conservation, local report geography, archived acquisition, honest week precision, acquisition time, retained source/identity, no costs/holds/population changes, deduplication, JSON reload, source removal, loss next steps, an actual watched-entry private nomination probe and fresh off/observer publication probes.

Final build: zero warnings/errors. Final scouting fixtures pass 108 checks at each of seeds 1001 and 2002. Existing information and room fixtures pass 43 and 83 checks at seed 1001; these ran before the final test-only watched-entry/UI-loss refinements, with unchanged production code. Final UI passes with no ERROR entries. git diff --check passes. Final verification results and source/log/assembly hashes are recorded in SimLogs/scouting-competition-validation.json. Rendered --scouting-competition-ui-check clicks the real listen action and explicit recap button, then verifies a public report, no resource spending and other-lead guidance for a signed-away act. Its screenshot and text dump are in SimLogs/scouting-competition-ui-v1. The image is visually inspected before delivery.

Earlier fixture setup failure: the test captured world state before changing its RNG seed and jumped the clock before priming the shared calendar. The corrected fixture snapshots after setup, separates world and RNG checks, and then measures the public information reads. No production behavior was changed to satisfy that assertion.

Run with Godot 4.7 Mono, serially:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-information-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-competition-ui-check

## Remaining work

Consent-based rehearsals/demo appointments, independent listener expertise, sourced private courtship and outside options, enforceable capabilities/promises, audience attribution and human opening playthroughs remain. Unsigned-artist appointment consent and studio/personnel reservation are not supported by the inspected player recording actions; do not substitute a generated gig or cost-free recording sample. The current contract ledger supports public completed signings, not a history of meetings, inquiries or rejected offers. Any future behavioral channel or economic change still needs the integration baseline's established paired-seed long-run acceptance.
