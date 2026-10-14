# Scouting expansion: accompanying independent listeners

October 10, 2026. Continues the implementation request in this chat's attached checkout, C:/Users/grohl/.codex/worktrees/scouting-appointments/Label-Man. HEAD remains 57bd42e. All previous listening questions, source questions and public signing reports are retained; all changes remain uncommitted. The primary band checkout and earlier scouting checkouts were not edited. SimLogs/scouting-listener-baseline.json records source hashes before this slice.

## Delivered slice

At an actual playing appearance, the room board offers an optional LISTEN TOGETHER action when a locally available musician on the player's roster can accompany the visit. Ordinary solo listening remains available. This is a live observation, not a retrospective imaginary listener or a rehearsal/demo appointment.

Availability comes from canonical artist/person identity, current roster ownership, active career/member state, local scene presence and the existing relocation availability reader. Performing members and house backers cannot be independent listeners to their own set. The room owner now exposes a read-only person-booking query. Overlapping performance/backing bookings block attendance. The existing conservative one-room-per-day policy also blocks same-day bookings in another room even when hours do not overlap; no travel or new calendar is invented.

The action rechecks source availability, remaining coverage and a captured price/capability quote before spending. It advances the same ordinary listening duration and charges the player and musician's admission through the existing listening cash path. There is no separate consultation fee, trust bonus, reserve on talent, live-work credit, development award or exposure signal. Each visitor pays once per continuous visit; admitted musician IDs survive save/reload and consecutive listening. Existing substantial source conversations still end the visit. If a booked performance cancels during the action, the ordinary visit can consume time/admission but creates no invented performance or opinion.

The room/performance owner resolves the same shared snapshot independently of attendance. The original player interpretation is unchanged. The co-listener interprets that snapshot with person-keyed stable error and per-axis expertise derived from existing musical capabilities: instrumental skill (legacy technical skill fallback), vocalist performance capability, writer creativity and stage presence. Non-vocalists and non-writers receive general impressions for those axes, explicitly labeled in the source role description. No ceiling, reliability, hidden health, private personality or scalar talent rating is consulted or presented. These capabilities are proxies for listening expertise, not a newly simulated professional critic occupation.

ListenerOpinion is appended to the existing evidence enum, preserving prior ordinals. It snapshots the person ID/name/role, same event ID, room, bill, date, acquisition time, people, material, coverage and a separately interpreted assessment. It is stored by the existing dossier/save owner. The ongoing visit's ListenerIds is additive and defaults empty for older saves. No save-envelope version bump or second evidence/relationship store.

The notebook compares the accompanying listener with the player's assessment of the latest heard performance, axis by axis. Agreement is described as broadly similar impressions; disagreement is left as a question for further evidence. Instrumental absence and partial coverage remain explicit. Neither a second observer nor two sets from one date count as another night. The comparison does not overwrite the player read, improve recording readiness or reveal future potential. Provenance includes the accompanying person's name, shared event context and actual coverage without exposing internal event keys or numbers.

## Verification

Final build: zero warnings/errors. Dedicated listener fixtures pass 40 checks at seeds 1001 and 2002. Existing scouting fixtures pass 108 checks at both seeds, and the room fixture passes 83 at seed 1001. These regressions ran before source role-text polish and final test-only screenshot positioning; no performance, price, availability or save-schema change followed. Listener fixtures reran after the role-text polish. Final rendered UI passes without ERROR entries; controls and notebook were visually inspected at 1600x900. git diff --check passes. Final build and acceptance results, source/assembly/log hashes, and screenshot hashes are recorded in SimLogs/scouting-listener-validation.json. The dedicated deterministic fixture runs with --scouting-listener-check. It covers free/canonical availability, performer/backer exclusions, stable cost-free/RNG-free previews, invalid IDs, stale quote/time/capability changes, injury/departure/ownership changes, local booking conflicts, total affordability, paired solo versus accompanied world outcomes, original player-read equality, exact action time, both admissions, provenance, sample independence, repeated action prevention, JSON restore, continuous admission over an authoritative controlled second appearance, same-date comparisons, disagreement, partial/instrumental evidence and observer/off behavior.

The real rendered --scouting-listener-ui-check injects a controlled roster musician in the canonical registry, clicks LISTEN TOGETHER on the actual room board, and verifies a separate opinion and the notebook's same-performance explanation. Artifacts: SimLogs/scouting-listener-ui-v1/board.png and ui.txt. The separate controls.png capture shows the real listener selector and total admission. Final screenshots were inspected before delivery; temporary feedback is hidden in the notebook capture only.

Earlier fixture issues: the first invocation fell through to the existing zero-week full save roundtrip because the new command-line branch had not matched the runner's tab indentation; the branch is now explicitly wired. A later booking-conflict fixture used an unknown room ID and was correctly rejected by restore validation. It now uses a real catalog room. Neither issue required loosening production restore or action checks.

Reproduce serially with Godot 4.7 Mono:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-listener-ui-check

## Remaining expansion

This completes a roster co-listener slice, not all independent expertise. A founding label with no roster has no roster musician to accompany it; sources and ordinary listening remain available. External hired experts, consent-based rehearsals/demo appointments, sourced private courtship/outside options, enforceable capability promises, audience attribution and human opening playthroughs remain. New unsigned-artist studio appointments still require consent and real capacity/accounting ownership. No AI recruitment, recognition, development or contract-economic reader changed in this slice, and no new long-run economic acceptance is claimed. Any later behavioral channel still needs the established integration baseline acceptance before enabling it.
