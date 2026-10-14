# Scouting expansion: hired local listeners

October 10, 2026. Continues the independent-listener handoff in `C:/Users/grohl/.codex/worktrees/scouting-appointments/Label-Man`, HEAD `57bd42e`. The attached directive is a design input; the user's current request authorizes continued implementation. Existing listening questions, source questions, public reports and roster listeners are retained. Changes remain uncommitted. The primary band checkout's gameplay files were not edited.

## Delivered slice

A founding label can now hire a locally available session-qualified musician to hear an actual scheduled set alongside the player. The room board retains ordinary solo listening and roster companions. It shows the listener's identity and role, each visitor's admission, the consultation charge and the total before the action. Hired prices are displayed to the cent, including partial-set listening.

The existing session-employment owner exposes a read-only supply using its established instrumental/reading qualifications, locally based active act members and canonical pooled people. Current presence and relocation availability are checked for act members; pooled people use their current base. Existing room-booking checks exclude performers/backers, overlapping work and same-day bookings in another room. Hired consulting supply respects the session-employment disable flag as well as scouting/room feature controls. No people, population, bookings, studio sessions or critic career are generated. Members of other labels may supply one-off work as they already do through session employment; the consultation grants no recruitment access, ownership, hold or consent to a later studio appointment.

The consultation price is a game-design setting of $25 per remaining set-hour in 1960 price-level units, scaled by `SceneLiveEconomics.PriceLevel`. It is not a researched historical tariff. Each genuine set has its own charge; listener and player admissions remain once per continuous visit. The action rechecks availability, expertise/role, affiliation, coverage, price and total affordability. A former free roster companion cannot silently become a paid hire under an old quote. Costs use the existing synchronous label cash/expense fee path, like staff-writer fees. They are expense sinks; no musician wealth/session-pay credit is claimed or fabricated. If the set cancels after attendance starts, agreed time/costs are consumed and no performance/opinion is invented; the control tooltip states this policy.

Both listeners hear the existing shared performance snapshot. The player's original assessment and ordinary world outcomes remain unchanged under paired listening. Listener interpretations retain person-keyed error and per-axis musical capability proxies. Non-vocalists and non-writers still have explicitly general impressions. Hired fees grant no accuracy bonus, recording evidence, future-potential inference, trust, contacts, regular status, development or exposure credit.

The existing `ListenerOpinion` observation retains the hired source channel and an additive `ConsultationFee` field. Older observations default to zero; save envelope v11 and knowledge owner stay unchanged. Source details display the hired channel and amount paid. Shared event identity, coverage, acquisition time, material and listener admission survive reload. A paid opinion is another judgment of one performance, not another performance or night. Already-heard events cannot be repurchased after refresh/reload.

## Validation

Final build passes with zero warnings/errors. Hired-listener fixtures pass 46 checks at seed 1001 (including real compressed save/load) and 43 at seed 2002; roster-listener fixtures pass 40 and scouting fixtures pass 108 at both seeds; room checks pass 83 at seed 1001. Final hired fixtures and rendered UI use the final build. The broader regressions ran before final missing-ID filtering and price-label polish. Final acceptance counts, exact source/log/assembly/screenshot hashes and build results are in `SimLogs/scouting-hired-listener-validation.json`. Starting hashes are in `SimLogs/scouting-hired-listener-baseline.json`. Headless logs use the `SimLogs/hired-v1-*` prefix; the real rendered control/notebook fixture writes to `SimLogs/scouting-hired-listener-ui-v1` and leaves previous screenshots intact.

Run serially with the installed Godot 4.7 Mono executable:

    dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-hired-listener-check --scouting-hired-listener-save-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-hired-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-listener-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
    --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
    --path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-hired-listener-ui-check

The first new fixture incorrectly compared contact state before and after a normally advancing live set. House-band performances legitimately update contacts. Its corrected paired comparison restores the contact owner alongside the room owner between solo/hired runs and requires equal ordinary outcomes. Production contact logic was not changed to satisfy the fixture. The first real-save probe used a name beyond the 48-character limit; the fixture now uses a shorter unique name without loosening production validation. The old roster fixture now requires rejection of the old free quote after ownership changes, since qualifying non-roster musicians may now be hired at a new paid quote.

## Remaining expansion

This slice supplies hired musical opinions where existing local workers are available; it does not guarantee an expert in every city or at every hour, add a separate professional critic occupation, or simulate negotiated consulting consent. Consent-based rehearsal/demo appointments, sourced private courtship/outside options, enforceable capability promises, audience attribution and contrasting human opening playthroughs remain. No AI recruitment, recognition, development or contract-acceptance reader changed. No new long-run economic acceptance is claimed. Later systemic behavioral channels still need the established integration-baseline acceptance before enabling them.
