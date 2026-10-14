# Scouting continuation: the first signature

October 10, 2026. Continues the actual working contents of `C:/Project/Label-Man/tmp/scouting-unsigned` after commitment planning. The user's continuation request authorizes implementation; the attached expansion directive remains design context. No primary gameplay source or other checkout is modified.

## Delivered behavior

The first completed signing now has a durable history boundary rather than being inferred from the current roster. Ordinary easy offers, managed negotiations and the existing leaving-member option use the same knowledge owner. Failed offers do not consume the first signature. Later deals cannot replace it after the roster empties.

New memories retain the completion minute, original terms, acquired performance observation ID and the people actually heard in that observation. The notebook presents the recorded room, date and coverage, plus all six stated contract terms. Raw event IDs no longer appear in newly acquired founding prose. A contract without acquired performance evidence records unknown listening provenance, with no invented visit or people. This is historical context, not a relationship bonus or a new contract owner.

Archiving, departure, renaming and later deals retain the original name and terms. The history appears above notebook entries, including when there are no dossiers. Departure does not itself create a reputation penalty or imply broken promises. Distinct departure consequences and enforceable offer promises remain separate work.

## Persistence and compatibility

Optional fields extend the existing v13 scouting knowledge payload; the save envelope stays v13. Freshly founded labels explicitly begin before their first signature. For older saves with no history boundary, preserve any existing memory and treat missing first-signature history as unknown. Do not reconstruct a founder from the present roster or nominate the next deal. Older memories acquire no inferred minute, observation ID or people. An older executable can ignore these optional fields and will use its earlier history behavior; no downgrade guarantee is made.

The public memory, save captures and restore inputs remain detached. The summary is a read-only projection; no clock, cost, world randomness, musical truth, recruitment decisions or acceptance weights change. Existing accounting and contract checks remain their owners.

## Verification

Final accepted checks, exact source/assembly hashes and screenshot hash are recorded in `SimLogs/founding-validation.json`. Baseline source copies use `.baseline` extensions under `SimLogs/founding-baseline` so they are excluded from compilation. `SimLogs/founding-source-manifest.json` records the initial source hashes.

The new fixture hears a real scheduled set, then controls eligibility, funds, posture and clock to isolate contract/history boundaries. It exercises real easy and managed signing, blocked offers, the existing private leaving-member signing helper, actual departure, re-signing on an empty roster, archiving, renaming, full compressed save/load, detached snapshots, summary/RNG purity, and legacy missing history. Calling the private option helper tests its completed-signature seam, not a human band-departure playthrough. The rendered fixture checks original terms after an archived act departs. These are deterministic fixtures, not a human opening or long-run economic acceptance claim.

Reproduce serially with the installed Godot 4.7 Mono engine from the implementation directory:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-founding-history-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-founding-history-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-terms-check --scouting-terms-save-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-commitment-planning-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-founding-history-ui-check
```

The sandboxed engine crashed before initialization; accepted engine runs used the reviewed unsandboxed launch. The first build accidentally included baseline `.cs` copies; renaming them to `.baseline` resolved that diagnostic-only problem.

## Portable delta and remaining work

`SimTools/ScoutingFoundingHistory.patch` is against the preserved commitment-planning working snapshot, not committed HEAD or the primary branch. Do not reapply older slices to this extended snapshot. The patch is checked against a reconstructed baseline before delivery.

Credible capabilities and enforceable offer promises, private competing offers/outside options, audience attribution, causal founding relationship consequences, contrasting human playthroughs and main-branch integration remain separate work. Changes remain uncommitted in the existing isolated snapshot.
