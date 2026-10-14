# Scouting continuation: audience evidence with context

October 10, 2026. Continues the working contents of `C:/Project/Label-Man/tmp/scouting-unsigned` after founding history. The user's request authorizes this implementation; the expansion directive is design context. No primary gameplay sources or other checkout are changed.

## Delivered behavior

The notebook now summarizes acquired whole-bill audience history, with an expandable **Audience evidence** clipboard. It counts distinct known bill IDs, recorded rooms and recorded towns. Hearing more sets on one bill or asking its booker does not create another crowd. Copied event/source reports deduplicate; listener judgments, demos and rehearsals are excluded. Reports without bill IDs remain reviewable but cannot establish independent crowds.

The clipboard preserves occurrence date, receipt date/time, source and credibility. Direct listening presents broad occupancy against capacity and the set's actual listening coverage. Booker records present their stated attendance against capacity. The comparison never displays exact attendance from the player's visual impression. Exact source counts inside the same visual band do not become contradictions; incompatible occupancy, capacities, recorded rooms/dates or distinct precise source counts remain disputed instead of being averaged.

New listening observations snapshot place, listed-act count and posted admission before advancing time. Posted admission describes the bill's quote, not another payment or the player's already-paid visit cost. Booker audience answers retain place and the completed record's listed-act count. They do not reconstruct historical admission. Missing fields in earlier notes remain unknown. The latest calendar does not fill earlier observations automatically.

Room and bill context remains separate from individual draw. A crowded bill cannot establish fans of its opener, who arrived or stayed for a set, cross-town portability, recording quality or future potential. Quiet-room evidence can coexist with strong musical evidence. This slice makes existing audience evidence reviewable; it adds no simulated act-attribution signal or recognition feedback.

## Ownership and compatibility

`ScoutingAudience` projects only retained acquired observations. It reads no artist capability, current calendar or audience model. `PlayerDesk.SceneRooms` and the existing booker conversation acquire the optional context through their existing actions. The scene room owner still resolves attendance, live work and fees. Knowledge still owns observation persistence and identity. No new population, appointment, contract evaluator, balance adjustment, following deposit or world RNG reader is introduced.

The save envelope remains v13. New fields are optional; older notes show unknown bill size/admission and do not infer a historical town from today's geography. Existing room IDs still provide recorded room identity. Archived, disabled and observer modes retain the history. An older executable can ignore optional context; this does not guarantee preservation when re-saved by old code.

## Verification

Accepted build, checks, exact source/assembly/patch hashes and screenshot hash are recorded in `SimLogs/audience-validation.json`. Original source hashes are in `SimLogs/audience-source-manifest.json`; baseline copies have `.baseline` extensions under `SimLogs/audience-baseline`.

The new fixture hears a real scheduled set, ends the day through the ordinary clock so its bill resolves, then asks the actual room's booker for a completed-bill audience answer. Additional comparisons use explicitly controlled retained observations, including a crowded multi-act bill and a quiet larger room in a different town. Those comparison observations do not create world performances, fans or source events. Checks cover denominator validity, source/visual precision, copied reports, conflicting counts, missing identities/context, pure world/player/RNG projections, calendar pruning, archiving, feature modes and real compressed save/load.

The rendered fixture hears a real set and supplies the same controlled retained comparison notes to exercise the actual notebook button, provenance, bounded scrolling and dismissal. Its screenshot was inspected at 1600 by 900. This is deterministic validation, not a human multi-town playthrough or new long-run economic acceptance. The initial fixture incorrectly required a reachable set end to coincide with the bill's end plus an available conversation hour; it now uses ordinary end-of-day resolution before consulting the booker.

Reproduce serially with installed Godot 4.7 Mono from the implementation directory:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-audience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-audience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-comparison-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-founding-history-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-audience-ui-check
```

Accepted engine runs use the reviewed unsandboxed launch because sandboxed Godot startup crashed during the preceding slice.

## Portable delta and remaining work

`SimTools/ScoutingAudience.patch` is against the preserved founding-history working snapshot, not primary gameplay sources or committed HEAD. Patch application is checked against reconstructed baseline files and the resulting text compared with the implementation. Earlier patches are historical deltas, not changes to reapply to the extended snapshot.

Credible offer capabilities and enforceable promises, private offers/outside options, simulated audience attribution and recognition effects, causal founding relationship consequences, human playthroughs and main-branch integration remain separate work. All changes remain uncommitted in the existing isolated snapshot.
