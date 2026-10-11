# Scouting continuation: the funded-session promise

October 10, 2026. Continues the contract-continuity working snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. This is the first Phase 4 (credible offers) slice: one enforceable promise that affects acceptance. Primary gameplay sources and other checkouts are unchanged.

## Delivered behavior

In a negotiated signing or renewal (Firm/Hardball), the label can promise a **funded studio session** within 8 weeks of signing. It is offered as the answer to an objection (**Promise a session**) or added directly on the tabling form; it can be dropped before the next round. Proposing it costs no time, cash or RNG. The form shows the cheapest-room quote at today's home rate (the existing `SessionCost(Budget, MinSessionHours)`), paid when the session is booked; it is not escrowed.

The promise is scored by the existing acceptance check through its `extraValue` input. Its worth rises with the act's artistic ambition (0.02-0.08 of package value). It never overrides the minimum-advance floor or a hard line, and no number is shown. Consequently it matters only where an act's reservation actually binds: in testing, a Svengali-managed act objected only on money, so a promise could not move it, while a Visionary-managed act could be moved. Easy (Pushover) acts keep their one-click form without the promise; they have no stake in the extra terms, matching how publishing and control already work for them.

Affordability includes the promise: the label must be able to cover the advance plus the session quote and keep its two-month reserve. This is checked before a round spends time or patience, and rechecked at commit after the meeting hours pass on the ordinary clock. If cash leaves mid-meeting, nothing commits, no commitment is written, the hours stay spent and the promise stays on the table.

## The commitment record

Signing writes one `ContractCommitment` (agreed day, due day, quote, renewal flag). The same record owns everything that follows:

- **Kept:** a real paid `StartSession` with the act on or before the due day. A late session cannot repair a miss.
- **Missed:** the first dawn after the due day records the miss once. The act's career history notes it and the desk logs it.
- **Void:** the act left the roster before the due day. No broken-promise history is written.
- **Remedy:** the act's next renewal ask raises its advance 25% per unanswered miss and says why in its demand summary. Completing that renewal marks the miss answered, so it is never charged twice. The renewal can carry its own new promise.
- Only one open promise per act; a new one waits until the earlier one is kept.

The roster card shows **Session promised by DATE**, in rust when 14 days or fewer remain.

## Ownership and compatibility

`PlayerDesk.ContractCommitments.cs` owns the record, affordability, fulfillment, dawn processing, remedy and save. The negotiation owner reads only the promise flag and value. `ContractTalk.sessionPromised` is copied by the existing detached-talk path, so open scouting negotiations and renewal discussions keep the promise through save/load. `PlayerSaveData.ContractCommitments` is optional: save envelope stays v13, older saves invent no promise, and malformed records (wrong label, duplicate ID, open-with-resolution, non-finite quote, due before agreement, remedy on a non-miss) are rejected before any player state mutates. Disabled and observer scouting modes keep commitments. New-label founding clears them.

All processing is player-side and runs after the no-label return in `OnDayStarted`; there is no AI reader, world RNG draw, feature flag or economic transfer beyond the player's own studio payment. A missed-promise line in an artist's career history is the only write to world data.

## Verification

Build: zero warnings and errors. `--scouting-session-promise-check` passes **65 checks** at seeds 1001 and 2002. It covers proposing with no RNG/world/time/cash change, add/withdraw from the form, a package that objects without the promise and signs with it (found by search through an internal fixture-only accessor), affordability before time and patience, cash drained by an `OnHourChanged` callback during the closing meeting, real compressed save/load of a tabled promise and a signed commitment, a real paid session that keeps it, a dawn miss recorded once, a late session that cannot repair it, the renewal remedy answered once with a fresh renewal promise, voiding on departure, six malformed saves, legacy absence and feature modes.

The continuity fixture's assertion that promises "remain blocked" was replaced with a check that a renewal promise is carried in the saved discussion. Rendered check `--scouting-session-promise-ui-check` presses the actual Promise, Drop and Add buttons and confirms the quote, no cost, no hidden values, a successful promised signing and the roster due date. `SimLogs/scouting-session-promise-ui-v1/{tabling,roster}.png` were inspected at 1600 by 900.

Regression on the final build, all passing: contract continuity 50 (both seeds), offer planning 27, business 34, experience 131, terms 42, meeting 43, appointment 59, opening 544, commitment planning 19, founding history 40, audience 37, comparison 32, market history 9, listener 40, hired listener 43, scene rooms 83, scene information 43, plus the continuity and offer-planning rendered checks. Exact markers and hashes are in `SimLogs/session-promise-validation.json`. This is deterministic validation, not a human playthrough or long-run economic acceptance.

During packaging, an edit helper had rewritten whole-file line endings. Every touched file's accepted pre-slice bytes were recovered and hash-verified against the continuity (or audience) validation, and the per-line endings were restored; the `.baseline` copies in `SimLogs/session-promise-baseline` are those verified bytes.

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-session-promise-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-session-promise-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-session-promise-ui-check
```

## Portable delta and remaining work

`SimTools/ScoutingSessionPromise.patch` is an eleven-file delta against the contract-continuity snapshot, checked with `git apply --check --whitespace=error` in a reconstructed baseline.

Open design questions for review: the 8-week window, the 0.02-0.08 credit, and the 25% renewal premium are first values, not calibrated. Further Phase 4 work remains: other capability terms (distribution, promotion), outside options and competing offers, relationship credit, and founding-act consequences of a broken promise. Human playthroughs and main-branch integration also remain. Changes are uncommitted.
