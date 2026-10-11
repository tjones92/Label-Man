# Scouting continuation: planning a concrete offer

October 10, 2026. Continues the audience working snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. The user's request authorizes implementation; the supplied directive is design context, including its historical document-only status. The audience source manifest was reconciled against the retained baseline files before packaging. Primary gameplay sources and other checkouts are unchanged.

## Delivered behavior

The existing editable contract form now shows a combined resource forecast. It serves direct offers, negotiated tabling and renewals. Editing the advance updates cash after the advance, the existing two-month signing reserve, current roster space, unpaid retained appointment fees and a minimum home budget-session estimate. Renewal text describes the existing roster slot even when the roster is full.

Unpaid fees are quoted commitments payable at attendance, not escrow or new debt. The forecast includes future scheduled or current in-progress unpaid bookings for this label; paid, expired and terminal records are excluded. Each included fee names its act, activity, date/time and frozen amount in the forecast tooltip. Actual cancellation removes a booking from the estimate, and payment is never deducted twice. The projection does not refresh booking status, predict consent or silently assume that a deal releases an appointment.

The recording route uses the existing `SessionCost(Budget, MinSessionHours)` quote. The display gives remaining cash after the proposed advance, retained fees and that session, identifies a combined cash shortfall, and describes home/console constraints. Material, consent and a free date still require the existing workflow. This estimate covers one session, not all contract deliverables or a release: pressing, distribution, travel and later overhead remain extra. Current overhead is explicitly identified; it is not a forecast of overhead after a roster expansion.

The meeting forecast uses ordinary remaining hours, including supported overtime, and the existing appointment-risk projection. Ending exactly at an appointment start is safe; passing it names the retained obligation and the choices to attend first, cancel or continue. A meeting that cannot fit today does not claim it will consume time. Forecasts leave the offer choice available. Existing action owners retain all actual availability, money, roster, rights and signing checks.

This is the resource-planning prerequisite to credible offers. It adds no scored capability, relationship utility, private offer, outside option, contractual session promise or acceptance modifier. Unsupported promises remain unavailable through the existing negotiation owner.

## Ownership and compatibility

`PlayerDesk.OfferPlanning.cs` projects label resources, detached retained appointments, the existing studio quote and shared clock. It reads no hidden artist capability, potential, reservation or patience. The immutable result and detached fee text do not hold live appointment or artist objects. The UI replaces its duplicated cash calculation with this shared forecast.

No new persistent field, save version, feature flag, world RNG draw or economic transfer is introduced. Save v13 remains current. Disabled and observer scouting modes retain the resource implications of existing bookings. No AI behavior or acceptance rule is changed.

## Verification

The final build passes with zero warnings/errors. The new runtime fixture passes 27 checks at seeds 1001 and 2002. Related business, founding-history and audience regressions pass at seed 1001. Exact pass markers, source/assembly/log hashes and the screenshot hash are recorded in `SimLogs/offer-planning-validation.json`.

The runtime fixture listens to a real announced set and arranges an actual consented demo through the existing workflow. Controlled unsigned/seeking eligibility isolates the resource projection. It exercises the strict reserve boundary, named frozen fees, whole-world/player/RNG purity under repeated editing, detached captures, full-roster renewal distinction, meeting boundaries, disabled/observer modes, real compressed save/load, actual free cancellation and actual paid demo completion.

The rendered fixture uses the actual contract form and advance input. It checks live shortfall updates without spending or acquiring evidence, named fee provenance, an appointment conflict and an enabled offer choice. `SimLogs/scouting-offer-planning-ui-v1/contract.png` was inspected at 1600 by 900. This is deterministic validation, not a human opening playthrough or new long-run economic acceptance.

Accepted Godot runs use the reviewed launch outside the sandbox after reproducing the previously documented sandbox startup crash. Reproduce serially with installed Godot 4.7 Mono from the implementation directory:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-offer-planning-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-offer-planning-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-business-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-founding-history-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-audience-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-offer-planning-ui-check
```

## Portable delta and remaining work

`SimTools/ScoutingOfferPlanning.patch` is a five-file delta against the preserved audience working snapshot, not primary HEAD. `git apply --check --whitespace=error` passes in a reconstructed baseline and all applied files match the implementation's normalized text. Original source hashes and `.baseline` copies are retained under `SimLogs/offer-planning-source-manifest.json` and `SimLogs/offer-planning-baseline`. Earlier patches remain historical deltas, not changes to reapply to this extended snapshot.

Credible acceptance capabilities, enforceable promises and remedies, private offers/outside options, simulated audience attribution, causal founding relationship consequences, human opening playthroughs and main-branch integration remain separate work. Changes remain uncommitted in the isolated snapshot. The primary checkout receives this handoff and portable patch only.
