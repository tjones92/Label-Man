# Scouting expansion: purposeful repeat listening

October 10, 2026. Continues the user's authorized implementation request from committed scouting baseline 57bd42e (Consolidate scouting evidence safeguards and provenance details).

## Checkout and authority

The supplied migration handoff and expansion directive were read as design/context documents. Their old document-only restriction and completion language were not treated as the current user's request or as proof of implementation. The existing scouting checkout was inspected read-only; it belongs to another chat.

This chat owns the managed checkout at C:/Users/grohl/.codex/worktrees/scouting-followup/Label-Man, branch codex/scouting-purposeful-followup, created from codex/scouting-experience-expansion at 57bd42e. Changes are uncommitted for review. The original scouting branch and primary band checkout were not changed.

C:/Project/Label-Man remains on band-member-simulation at 51adfc7 with its original export preset modification and untracked directive/UID files. No stash, merge, push, or band commit was made.

## Delivered slice

Phase 2 purposeful repeat listening now has a player-selected question: vocals, ensemble, material, or stage communication. A question is an optional dossier field, separate from observations and priorities. Choosing or refreshing it costs no action time, creates no evidence, and changes no performance, trust, negotiation, or economic state.

The notebook compares the latest acquired performance on each of the latest two occurrence dates. Received claims are excluded. Same-night sets cannot establish consistency. Partial coverage and absent vocal evidence leave the question open. Comparisons qualify changes in people, backing players, repertoire, or room and describe impressions without assigning hidden causes or predicting development.

The notebook now exposes the next unheard local appearance within the player's hard stop, with actual event identity, date, interval, remaining listening minutes, and current admission quote. It excludes cancelled bills, cancelled slots, already observed performances, expired slots, and feature-disabled/observer encounters.

Explicit WAIT FOR NEXT SET and HEAR NEXT SET buttons route through the existing clock/listening APIs. Waiting runs ordinary expenses and decisions; it does not automatically listen or charge admission. Actions recheck their captured event identity, so a stale preview cannot bypass cancellation. Continuous visits use the existing admission policy, including staying for a later set.

## Ownership and saves

ScoutingQuestion belongs to the existing player dossier. Missing fields in existing v11 saves default to None; no save-envelope bump or parallel appointment/relationship store was added. Questions survive serialized restore and archiving. Observation IDs, historical evidence, canonical artist ownership and performance snapshots retain their existing owners.

The opportunity is an ephemeral projection of the authoritative room calendar, not a reservation or an invented appointment. It makes no rival, fan, recording, consent, or market claim.

## Validation

Final dotnet build: zero warnings and errors. Scouting fixtures pass 67 checks each at seeds 1001 and 2002. These include actual listening, detached captures, save/reopening identity, question persistence, independent-date comparisons, partial/instrumental evidence, occurrence versus receipt ordering, cost-free previews, cancellation rechecks, and hard-stop filtering.

Existing serial compatibility fixtures pass: room seed 1001, 83 checks; information seed 1001, 43 checks. They ran before the final preview admission expression was changed to the existing AdmissionForVisit helper; final scouting fixtures and the rendered fixture ran after that change.

Actual rendered UI with --scouting-followup-ui-check passes. It exercises the real listening button and question selector, then asserts the stored ensemble question and visible missing-date explanation. The final 1600x900 screenshot was visually inspected: wrapped notes, question selector and follow-up actions fit the scrollable paper. The final renderer log contains no ERROR entries.

The initial broad editor import stopped while importing large SimTools CSVs, leaving some texture/font cache entries missing. Unchanged texture/font caches from the exact scouting baseline were reused in this isolated checkout. The final rendered rerun succeeds with no missing-resource errors. Earlier diagnostic logs are retained.

Artifacts under SimLogs:
- scouting-followup-final-1001.log
- scouting-followup-final-2002.log
- scouting-followup-scene-room-check.log
- scouting-followup-scene-information-check.log
- scouting-followup-ui-final.log
- scouting-followup-ui-v1/board.png and ui.txt
- scouting-followup-validation.json (source/assembly/log hashes)

git diff --check passes. This slice adds no AI recruitment, recognition, development, audience attribution, contract economics or new feedback channel; no new long-run economic acceptance is claimed.

Reproduce serially with Godot 4.7 Mono:
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-information-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-followup-ui-check

## Remaining roadmap

This completes a repeat-listening follow-up slice, not the entire expansion. Consent-based rehearsal/demo appointments, independent listener expertise, sourced market courtship and outside options, enforceable capability promises, audience attribution, and human opening playthroughs remain as described in the original implementation handoff. New behavioral feedback still requires the baseline's established long-run acceptance before enabling it.
