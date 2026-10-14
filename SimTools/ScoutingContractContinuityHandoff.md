# Scouting continuation: contract discussion continuity

October 10, 2026. Continues the offer-planning working snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. Codex implemented the source and runtime fixture; Claude Code ran the validation, added the rendered check and packaged the slice. Primary gameplay sources and other checkouts are unchanged.

## Delivered behavior

An open renewal discussion now survives leaving the page and a real compressed save/load. Reopening a renewal returns to the same discussion: the ask, draft, posture, patience, rounds, objection and log are retained. Reopening never regenerates the ask, draws RNG or spends time. Only one renewal discussion is open at a time. A different matured act must wait until the current one is finished or dismissed.

The roster announces a retained discussion with **Resume renewal discussion** and **Step back from renewal**. Stepping back is voluntary dismissal. It never drops an act or changes its contract, ownership or cash, even when the discussion is stale.

Each renewal records the identity of the contract under discussion: sequence, start, expiry, term, obligation, royalty, publishing and control. Ordinary cash, debt and release progress may change. If that contract identity changes, the act leaves the roster or label, or the contract is no longer matured, the old discussion cannot spend time, patience or money, or commit. It explains why and can still be dismissed.

Every new-signing and renewal commit path rechecks current ownership/eligibility and affordable cash *after* the meeting hours are spent. Those hours are spent through the ordinary clock, so hourly callbacks can change the world mid-meeting. A failed close keeps consumed time, pays no advance and leaves the old contract unchanged. Negotiated terms stay editable. Holding firm with an unaffordable package is blocked before any time or patience is spent. Non-finite advance and royalty values are rejected. A successful renewal pays once and consumes the discussion; replaying a completed talk is rejected.

## Ownership and compatibility

`PlayerDesk.ContractContinuity.cs` owns currency checks and `RenewalDiscussionSaveData`, which holds IDs and a detached talk only and never another live artist graph. Restore binds the canonical act after the roster restores. Capture and restore detach every mutable collection. Acceptance still uses the existing evaluator and package values. Unsupported promises remain rejected.

`PlayerSaveData.RenewalDiscussion` is optional; save envelope stays v13. Older saves have no field, and no ask or rounds are invented. Malformed state, such as another label, invalid patience or terms, empty weights, a missing contract identity or a serialized live artist, is rejected before any player state mutates. Disabled and observer scouting modes retain an existing discussion. No AI behavior, world RNG reader, economic transfer or feature flag is added. New-label founding clears any prior renewal.

## Verification

The build passes with zero warnings and errors. `--scouting-contract-continuity-check` passes **49 checks** at seeds 1001 and 2002. Codex's fixture covers a real managed renewal, which follows a real listened set and an actual signing. It then matures the contract by recorded deliveries, not a clock jump, and plays a real hard-line objection. It covers RNG-free reopen, detached capture/restore, actual compressed save/load, stale talk objects after reload, detached/rival acts, five malformed saves, feature modes and legacy absence. It also covers lost ownership, a changed contract and an unaffordable hold. Cash consumed by an `OnHourChanged` callback during an accepted-package meeting is checked for both negotiated and easy renewals. Successful negotiated and easy renewals pay exactly once.

Regression, seed 1001, all passing: offer planning 27, business 34, experience 131 (+ loss 23), terms 42, meeting 43, appointment 59, opening 544, commitment planning 19, founding history 40, audience 37, comparison 32, market history 9, listener 40, hired listener 43, scene rooms 83, scene information 43.

Rendered check `--scouting-contract-continuity-ui-check` (added this session) does the following:
1. Saves and loads the objection through real compressed IO.
2. Confirms the roster banner, then presses the actual Resume button.
3. Asserts the same objection, rounds and patience at no time or cash cost.
4. Presses Step back and asserts the contract, roster and cash are unchanged.

`SimLogs/scouting-contract-continuity-ui-v1/renewal.png` was inspected at 1600 by 900. This is deterministic validation, not a human playthrough or long-run economic acceptance. Hashes of every source, log, the assembly and the screenshot are in `SimLogs/contract-continuity-validation.json`.

Reproduce serially with Godot 4.7 Mono from the implementation directory. Engine runs use the reviewed unsandboxed launch:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-contract-continuity-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-contract-continuity-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-contract-continuity-ui-check
```

## Portable delta and remaining work

`SimTools/ScoutingContractContinuity.patch` is a nine-file delta against the preserved offer-planning snapshot, not primary HEAD. `git apply --check --whitespace=error` passes in a reconstructed baseline, and every applied file matches the implementation's normalized text. Baseline copies and hashes are in `SimLogs/contract-continuity-baseline` and `SimLogs/contract-continuity-source-manifest.json`; the overlapping baselines match the accepted offer-planning source hashes.

Credible acceptance capabilities, enforceable promises, private offers/outside options, simulated audience attribution, causal founding relationships, human playthroughs and main-branch integration remain separate work. Changes remain uncommitted in the isolated snapshot.
