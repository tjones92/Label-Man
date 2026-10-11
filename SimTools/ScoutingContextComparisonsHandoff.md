# Scouting continuation: comparing the nights actually heard

October 10, 2026. Continues the stated-terms handoff in the isolated managed checkout `C:/Users/grohl/.codex/worktrees/scouting-continuation/Label-Man`, branch `codex/scouting-context-comparisons`. The earlier worktree belongs to another chat, so its working sources were copied without modifying that checkout. The primary band checkout is preserved. All inherited scouting work and this slice remain uncommitted for review.

The attached expansion directive is design input; its earlier document-only status does not replace the user's request to continue implementation. This slice strengthens purposeful listening comparisons. It does not complete the remaining expansion roadmap.

## Baseline and change

`SimLogs/scouting-comparisons-baseline.json` records the exact inherited commit and SHA-256 hashes of the copied C# sources before this slice. The earlier stated-terms implementation was inspected rather than inferred from its completion language. A baseline build passed with zero warnings and errors.

Previously the notebook compared the maximum and minimum execution impression over all retained sets. Two different sets on one night could therefore produce a claim about variation across dates. The notebook and pinned listening question also selected and described their evidence differently.

Both now use `ScoutingComparison`, a read-only projection of retained acquired observations. It:

- Orders performances by occurrence date and set time, deduplicates known event identities, and excludes source claims and other listeners' opinions from independent dates.
- Uses the latest dated performance as its subject and one representative (latest) set per earlier date. Earlier same-night sets cannot be substituted to bypass that night's later context.
- Requires complete coverage and an acquired interpretation on the relevant axis; partial or missing evidence leaves the question open.
- Prefers the most recent complete earlier date with comparable recorded performing people, backing people, material and order, and room. Otherwise it names which context differed or was unknown and states that consistency under the same conditions is unresolved.
- States both historical dates and limits the conclusion to those two samples. Current lineup changes still mark affected notebook judgments as needing fresh evidence.

The notebook's performance count and latest-set description use the same ordered, deduplicated evidence. A pinned ensemble question keeps its question visible but does not repeat the consistency paragraph verbatim. Missing retained interpretations no longer crash a multi-date consistency summary. No new evidence, appointments, population, relationship balances, RNG, cash, action time, or performance outcomes are created by these reads. No hidden talent, ceiling, motive or causal field is consulted. The save representation and v11 envelope are unchanged; existing evidence is retained in off/observer modes.

## Verification

Final build passes with zero warnings/errors. Comparison checks pass 32 checks each at seeds 1001 and 2002, including real compressed save/load. Scouting regressions pass 131 checks each, terms checks pass 42, and room compatibility passes 83. The rendered comparison fixture passes and its 1600x900 notebook screenshot was visually inspected. Final results and hashes are recorded in `SimLogs/scouting-comparisons-validation.json`; individual probe logs are beside it. New comparison fixtures cover same-night spread, comparable older dates, changed performers/backing/material/rooms, missing context and interpretations, incomplete coverage, per-axis questions, repeated event IDs, correlated opinions, current lineup staleness, read-only world/RNG/time/cash ownership, and serialized plus real compressed save/load. They begin with actual listening to a canonical act and use controlled acquired snapshots to isolate comparisons. These are evidence ownership fixtures, not human opening or economic acceptance.

All game probes ran serially. Two-seed comparisons and scouting regressions, plus terms and room compatibility, passed before the final removal of duplicate ensemble prose. The final comparison/save, scouting regression, and rendered probes were rerun at seed 1001 after that presentation change. The rendered fixture `--scouting-comparison-ui-check` uses the actual notebook after real listening with controlled acquired comparison snapshots; it checks the date comparison and changed-backing limitation. Its screenshot and text are in `SimLogs/scouting-comparison-ui-v1/`.

Reproduce with Godot 4.7 Mono, serially:

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-comparison-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-comparison-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-terms-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
--path . SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-comparison-ui-check
```

On this host the fresh editor import crashed while importing existing data CSVs. Existing imported assets and UID cache were copied from the preserved source checkout; the continuation's C# assembly was built independently. Runtime checks and rendered validation use that assembly. This does not establish a clean editor import from scratch.

## Remaining work

Consent-based unsigned rehearsal/demo appointments still need a scheduling, capacity, and commitment owner. Pending private offers/deadlines, enforceable capability promises, acceptance outside options, audience attribution, founding-history consequences, all-city opening validation, and human playthroughs remain. This observation projection introduces no behavioral feedback, so it makes no claim of long-run economic acceptance.
