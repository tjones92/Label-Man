# Scouting expansion migration and evidence detail follow-up

October 9, 2026. Continues `codex/scouting-experience-expansion` from `300ab7eb`, using the existing checkout at `C:/Users/grohl/.codex/worktrees/scouting-experience-expansion/Label-Man`. The supplied implementation handoff was checked against the branch's actual files. Its instructions and completion claims are context; the current user request authorizes consolidation and continued work.

## Overlap decisions

| Work from the duplicate slice | Integration decision |
|---|---|
| Automatic evidence outside the six-act watch list, deduplication, legacy honesty and historical retention | Already implemented more fully by `ScoutingKnowledge` in `300ab7eb`. Keep that owner; do not add a second `SceneDiscovery.ListeningEvidence` store. |
| Standalone room notebook UI | Superseded by the branch's contextual notebook, priorities and provenance controls. Retain the existing surface. |
| Filtering listening to the visit and rechecking after time advances | The branch's minute-based listener already performs both. Retain it; port the optional upper bound and empty-interval guard in the existing `Hear` helper, plus its regression probes. |
| Hidden package/floor/patience presentation | Already removed by `300ab7eb`. Port deletion of the now-unused threshold-disclosure helpers and simplify an identical advance-objection branch. Acceptance math is unchanged. |
| Detached discovery projections, save captures and restore inputs | Port all three boundaries. The branch already detaches its richer dossier store; the older discovery references now follow the same rule. |
| Additional persistence fixtures | Adapt them to the existing dossier schema. Add real gzip evidence identity, replay identity, nested capture isolation, independent later dates and history retention after watch-list removal/calendar pruning. |
| Save envelope v11 | Already used by the scouting branch. Retain it; no second save-version change or new feature flag. |

The duplicate primary patch and original evidence handoff are archived under `C:/Project/Label-Man/SimLogs/scouting-migration-20261009`. Their earlier validation describes the superseded implementation, not acceptance of the combined branch.

## Continued work

Acquired observations now retain their room name and actual acquisition minute, separately from the observed performance interval. Booker reports record acquisition after the conversation's real time cost. These are optional fields: existing v11 observations use the catalog name when available and display an unknown acquisition time rather than inventing one. Existing event/source IDs remain the deduplication and ownership keys.

The evidence detail view presents room, source, occurrence/acquisition time, listening interval, coverage and repertoire. Full-set notes identify heard material; partial-set notes explicitly describe announced material and leave which titles were heard uncertain. Raw event keys stay in saves and diagnostics. The evidence clipboard scrolls within a bounded area to retain an accessible dismissal button as history grows.

No new rival, trust, consent, contract-economic, audience-attribution or AI feedback system is introduced. The remaining roadmap in `ScoutingExperienceImplementationHandoff.md` still applies, including consent-based recording/rehearsal appointments and sourced market courtship.

## Band branch preservation

`C:/Project/Label-Man` remains on `band-member-simulation` at `51adfc7a6e9990ebe430c7029541b4a7445df967`. Only this chat's duplicate scouting source edits and its newly-created handoff were removed after archiving. The final porcelain status exactly matches the recorded pre-task status: the export preset change, untracked original directive and all pre-existing UID files remain. The export preset SHA-256 is unchanged. No band commit, branch move, stash, merge or push was made. Its local assembly was rebuilt to match its restored sources.

## Validation

Both checkout builds pass with zero warnings/errors. The integrated gameplay source passes serial autoloaded fixtures in `SimLogs/scouting-migration-v1/validation.json`: scouting seeds 1001/2002 pass 45 checks each, room seeds 1001/2002 pass 83 checks each, and information seed 1001 passes 43 checks. The room fixture includes real temporary gzip saves, both feature modes, detached captures, pruning, work/fee replay and the existing ownership invariants. Test save slots are uniquely named and deleted.

The final evidence UI and its rendered fixture were rebuilt afterward; core gameplay source did not change between those runs. UI results and final source/assembly hashes are recorded under `SimLogs/scouting-migration-v1`. The dedicated `--scouting-evidence-ui-check` exercises the actual listen and provenance buttons, checks bounded scrolling and visible dismissal, and writes `SimLogs/scouting-migration-ui-v1/board.png` and `ui.txt` without overwriting the prior session's screenshots.

The prior 26-week acceptance remains inherited validation of `300ab7eb`; no new decade/economic acceptance is claimed for this knowledge/presentation change. `git diff --check` passes.

Final rendered result: SCOUTING_EVIDENCE_UI_PASS. The 1600x900 evidence clipboard screenshot was visually inspected: wrapped text fits the bounded paper, source/time/material are legible, and DONE remains visible. Final build reports zero warnings/errors.
