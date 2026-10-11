# Scouting expansion: past label decisions and act conversations

October 10, 2026. Continues the hired-listener slice in `C:/Users/grohl/.codex/worktrees/scouting-appointments/Label-Man`, HEAD `57bd42e`. The supplied directive is a design input; the user's request authorizes continued implementation. Earlier uncommitted scouting work is preserved. The primary band checkout's gameplay files were not edited.

## Delivered slice

The existing daily talent market now records completed decisions on the canonical act after clearing: a completed signing, a competing proposal not selected, or a selected signing attempt that did not complete. Each record retains the exact decision day, label identities/names, selected label and whether that selected deal actually completed. Failed commits do not imply contracts. Search nominations, no-candidate passes and internal scoring are not presented as approaches or offers. Neither visits nor introductions are inferred. This additive history has no recruitment, acceptance or economic reader and consumes no RNG. It is bounded to 32 records per act and deduplicated by genuine appointment identity.

After hearing a set today, the player can spend one hour speaking with a free active member who actually performed in the heard lineup, at that room within posted hours. The action rechecks the act's lifecycle, local presence, relocation availability and the person's existing room bookings; it prefers the band leader or lead vocalist when free. This is a limited post-set availability policy, not a general private appointment or consent owner. The notebook shows the speaker and cost before the action; the account itself is acquired only by having the conversation. A missing account explicitly means no retained decision, not guaranteed absence of other approaches.

The account names the last eight retained decisions and their dates, with an explicit distinction between historical decisions and a current offer/deadline. It does not explain unrecorded motives or reveal utility, talent ceilings or hidden contract thresholds. `ActBusinessClaim` is appended to the existing evidence enum and retains the member, room, channel, statement date, acquisition time, exact claim and credibility. The source statement is captured before time advances. Refreshing creates no knowledge, and the same underlying account cannot be acquired again through a different speaker. Stale quotes, conflicting bookings and repeated accounts consume no time or money. A completed conversation ends continuous room admission and grants no second performance sample, trust, contact, hold or signing access. Ordinary world activity still advances with the hour.

Competition summaries show the acquired act account alongside any separately acquired public report. Historical notes remain intact if source history is pruned or current ownership changes. Existing public reports retain their week-level dating and publication rules; this slice does not silently upgrade them to exact-day knowledge.

The optional act history and appended evidence kind serialize through the existing world/player owners with save envelope v11. Older acts have no fabricated decisions. Observer scouting records world history but acquires no conversations. Scouting/room disable modes prevent acquisition and preserve old evidence. `--disable-scouting-market-history` prevents new owner records while leaving retained history and normal market behavior intact.

## Validation

Build passes with zero warnings/errors. Conversation fixtures pass 36 checks at seed 1001 (including a real save/load) and 34 at seed 2002. Actual daily-market fixtures pass 9 checks at each seed with four retained decisions each. Scouting regressions pass 108 checks at both seeds, hired listeners pass 43 at seed 1001 and room checks pass 83 at seed 1001. The rendered conversation exercises the final build.

Final counts, source/log/assembly/screenshot hashes and build results are recorded in `SimLogs/scouting-business-validation.json`. Starting hashes are in `SimLogs/scouting-business-baseline.json`. Logs use `SimLogs/business-v1-*`; the real rendered controls and notebook are in `SimLogs/scouting-business-ui-v1`. The rendered fixture injects a synthetic completed decision through the canonical recorder, then exercises the real button. The separate market fixture requires actual daily clearing to create events.

The paired daily fixture gives four existing labels controlled vacancies, budget and access, enables the established live market at chart week 1, and advances a Saturday to Sunday through the ordinary clock. Both treatments restore the same saved world/player state and RNG seed. Every pre-existing world field is compared, excluding only the new history, along with player state and RNG continuation. This validates daily clearing; it is not long-run economic acceptance. The first fixture crossed Friday's weekly economy and diverged in release IDs, advances and release histories after in-process restore. The corrected fixture isolates the daily owner without weakening its full-state comparison or changing production economics. General weekly replay equivalence was not established by this slice.

Run with the installed Godot 4.7 Mono executable, serially:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-business-check --scouting-business-save-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-business-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-market-history-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-market-history-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-hired-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-business-ui-check

## Remaining expansion

This advances the directive's visible-competition phase with historical outcomes and a sourced conversation. It does not introduce pending private offers, actual deadlines, private appointments, negotiated consulting consent, withdrawal remedies or active outside-option acceptance. Consent-based unsigned rehearsal/demo sessions still need a supported scheduling, capacity and commitment owner; existing `StartSession` requires a roster act and owned studio. Capability promises, audience attribution, founding-history consequences and contrasting human opening playthroughs remain. Later behavioral channels still require the established integration-baseline acceptance. Changes remain uncommitted for review.
