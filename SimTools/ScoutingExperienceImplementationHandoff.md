# Scouting experience: first playable implementation

The user authorized code implementation on October 9, 2026, explicitly overriding the attached directive's earlier document-only status. The directive is preserved verbatim as design input. Its roadmap is not a claim that all phases are complete.

## Exact integration baseline

New branch: `codex/scouting-experience-expansion`. Base: `band-member-simulation` at `51adfc7a6e9990ebe430c7029541b4a7445df967`. Isolated managed checkout: `C:/Users/grohl/.codex/worktrees/scouting-experience-expansion/Label-Man`. The primary `C:/Project/Label-Man` checkout remains on `band-member-simulation`; its export preset modification and untracked directive/UID files were preserved. No switch, stash, merge, push or commit was performed in that checkout.

The base includes the committed Phase 7 scene source implementation and subsequent band/contact work. It does not include the primary checkout's uncommitted files. Generated missing band-source UID metadata in this worktree is excluded from the scouting change.

Source SHA-256 hashes below are of exact Git blob bytes at the base commit, not normalized working copies. Directive SHA-256: `3558593e3cdacd3e1e3d66db4abb113ba2d1320265687e5be32afd21d82be3d3`.

| Audited owner/caller | Base blob SHA-256 |
|---|---|
| `Systems/PlayerDesk.SceneRooms.cs` | `a63553cfa6610825a3e3f7c3996776e31ecdc08f85ef440a05452f50a960a060` |
| `Systems/LocalSceneRoomService.cs` | `80e49ceb398d77721edc0d40a3a8fc13445f686bb10e16cce5f03fb0892ff236` |
| `Data/LocalSceneRooms.cs` | `a8aa37908e3c8ef381d65dff8a3095c3012e6735482df617cb0da6e288ae5b1d` |
| `Systems/PlayerDesk.LocalScenes.cs` | `a877dfd8fc78f59c0f7ec4df971f19100affe3fefb8f7b88a8c8300569480013` |
| `Systems/PlayerDesk.SceneInformation.cs` | `612225ef226074179516bfb8b7d8773810ed7e943f0e4e8b6154c22d04aed6d0` |
| `Systems/PlayerDesk.cs` | `a13a959d6f0ac6acc296fcd50784e62172ff9ff1edeb489be1bbc23fcadaa0ef` |
| `Systems/PlayerDesk.ContractNegotiation.cs` | `289ffa61b34ac1d4f031c98d5f79d6a453d010b31ca8daee3a1ae9525ca5319c` |
| `Systems/SaveGameService.cs` | `57bf318c6582a1a2ae0d0cd496a09feaa051161e2501305b8f1fb38a11f1f0ee` |
| `Systems/WorldSaveData.cs` | `9a1a9bab246cccb8075c6ab747b1e41ab4034782c21b19216a781f7d2f06fd09` |
| `Systems/ScoutingPerception.cs` | `badf9da882052c1e608f1b93adba3d80bf0da96c3de68d06c4eb9a68a2f9939c` |
| `Systems/LocalScenes.cs` | `c8712da39e0da01aed14a8cfe2437fa1d6b9dcc0dc0452a9790b9c25f0e968aa` |
| `UI/PlayerDeskPanel.SceneRooms.cs` | `a0f5f66c2a805ba30a27f3ae8f0ec7790a7b97219b81fc9b0c463d5dcc3292ca` |
| `UI/PlayerDeskPanel.cs` | `30f573ab40208bda8109e026ac98a687195bf5593eb8ddb6c055b1917e53b528` |
| `Data/ContractTermSheet.cs` | `02522b9948a42b35abb3de7d88d02dac16a9813c8447d3b98f4db4979fc04925` |

## Reconciled defaults and owners

Code wins over historical handoff wording: persistent rooms, recruitment, lagged attention, price feedback, institutions, relocation, source circuits, live economics, house bands and AI scene investment are enabled under their existing dependencies. Older attention handoffs describe an earlier opt-in boundary; no feedback default was changed here. Phase 7's inherited 52-week paired-seed acceptance and deferred decade confirmation remain inherited claims, not newly rerun acceptance for band work.

The room service owns bookings, appearance completion, work, fees, attendance, and the new compact shared live snapshot. The snapshot is generated on clock resolution for attended and unvisited appearances. It uses existing member axes, present execution/stage abilities and actual scheduled repertoire; keyed descriptive variance does not affect development, fatigue, fees, recruitment or world RNG. There is no ceiling read.

Player knowledge owns label/act dossiers, acquired observations, interpretations, priorities and the most recent continuous visit. People/artist/source/event keys are IDs; it owns no artist object graph or parallel trust balances. Contacts and recruitment retain their existing owners. Contract acceptance still uses the existing evaluator and atomic commit checks. Pending scouting negotiations have ID-keyed saved state with detached talks; reopening does not regenerate asks, patience, counters or cooldowns. Immutable term sheets now have an explicit JSON constructor, required to preserve those terms.

## Delivered loop

- Named-room itinerary shows cash, hours, actual set times/status and explicit waits. Waiting runs ordinary day/hour processing and rechecks the booking on arrival.
- A player can listen to a particular scheduled appearance, stay for later sets, or hear the next two hours of actual appearances. Coverage is based on minutes actually present. Cancelled appearances generate no invented evidence or replacement act.
- Consecutive listening/staying in one room shares admission. A later return after another substantive action is another visit. Costs and time persist through reload. Same-event replay is rejected without cost or progress.
- Every heard act, including signed and latent performers, receives a durable automatic dossier outside the six-slot legacy watch list. Watch/pursue/archive affect player priorities; historical evidence survives removal and signing.
- The notebook gives contextual vocal, ensemble, material and stage impressions; independent dates and contexts qualify consistency. Recording readiness is unknown after live listening. No scalar act verdict, potential rating or hidden causal diagnosis is presented.
- Whole-bill attendance is included only if the bill completed while the player was present, with its capacity and scope. It is never converted into individual following. No signing creates fans.
- Booker questions report actual room booking history; repeating the same answer adds no evidence or cost. Cold listening tips follow posted availability, without a two-bill familiarity checklist. Source claims are distinct from direct listening.
- A single supported listening can justify discussing terms. Canonical ownership, market activation/cooldown, roster capacity, cash and source access remain authoritative. Contract UI preserves asks and actionable objections while hiding reservation/package calculations and patience. The unsupported promotion-promise credit is removed from the surface and rejected by the API.
- The first actual signature records act/label/date, terms and encounter context as historical memory; it grants no loyalty or ownership bonus.

## Save/feature contract

Save envelope v11 adds shared appearance snapshots and `ScoutingKnowledge`. Scene room schema/content remains v1 because the new appearance field is optional. Old notebook entries migrate as legacy impressions with unknown source/coverage, never invented visits. Already-completed old appearances have no retroactively manufactured performance result. Acquired observations survive bounded calendar pruning.

`--disable-scouting-experience` preserves existing dossiers and completed snapshots, blocks new encounters and suppresses new shared snapshots. `--observe-scouting-experience` resolves shared descriptive snapshots but blocks player acquisition. `--enable-scouting-experience` explicitly enables the default. Both require persistent rooms and reject contradictory disable/dependency modes. No new AI feedback channel exists. v10 executables reject v11 saves; downgrade is not supported.

## Acceptance and remaining roadmap

The first implementation covers the Phase 0 reconciliation and the core Phase 1 room/notebook/approach loop, plus selected Phase 2 repeat-listening and source questions. It does not claim the entire directive is complete. Dedicated consent-based rehearsal/demo appointments, independent listener expertise, full pending rival-event instrumentation, richer sourced courtship/outside options, capability-backed enforceable promises, audience attribution, all-city human opening playthroughs and later feedback/polish phases remain follow-on work. Existing signing news continues through its dated local projection; no rival offers or fake deadlines were added.

Before testing, required checks were defined as deterministic event deduplication, visit cost/time conservation, visited/unvisited shared-state equality, save identity, legacy migration, canonical signing eligibility, negotiation continuity and existing room/information compatibility. No economic tolerance was invented. This change adds no AI recruitment/recognition/development reader; the existing long-run acceptance is required before any later behavioral channel is enabled.

Validation results and commands are appended after checks finish. Generated logs, baseline/source manifests and screenshots belong in `SimLogs`, not committed source directories.

## Completed validation

- Final `dotnet build "Label Man.csproj" --no-restore --nologo -v quiet`: zero warnings and errors.
- Actual autoloaded game fixtures, seeds 1001 and 2002: **43 checks pass each**. Event IDs, source correlation, complete coverage, detached snapshots, independent appearances, admission across reload, canonical signed/latent gates, lineup staleness, archive retention, legacy migration, observer/off acquisition, claim deduplication and negotiation terms/counters/cooldown roundtrip are covered. One real listening permits an approach in a controlled canonical consent fixture.
- Existing named-room suite: **74 checks pass**, including unique temporary real player/world gzip save/load, source relinking, attendance/work/fee conservation and deterministic replay. The previous assertion that a rewound same-event scouting action succeeds was updated to assert rejection without new evidence/cost. Follow-up now begins at the act's actual appearance, and preserves the caller plus legacy watch-note provenance.
- Existing local information suite: **43 checks pass**. Its former two-bill tip prerequisite was updated to check cold access to a real scheduled appearance.
- Full-world **26-week seed-1001 roundtrip passes**: 380,638,340 serialized bytes, 30,526,654 gzip bytes; byte identity and real gzip IO, 600 labels, 8,100 acts, 1,078 unsigned acts at chart week 26. This is save/replay validation, not decade economic acceptance.
- Real 1600x900 rendered UI: **SCOUTING_EXPERIENCE_UI_PASS**. The actual room listening button produces the automatic evidence notebook; calendar selection, provenance controls and hidden-threshold assertions pass. `SimLogs/scouting-ui-v1/board.png` was visually inspected: readable notes, fitting action buttons and retained paper styling. Final onboarding/grammar wording was polished afterward without changing geometry.
- Source `git diff --check` passes. The verbatim supplied directive retains its intentional Markdown hard-break spaces. The primary band checkout's branch, HEAD and pre-existing status remain unchanged.

Generated validation/source hash manifest: `SimLogs/scouting-validation.json`. Successful logs: `scouting-accepted-{1001,2002}.log`, `scouting-rooms-repaired-1001.log`, `scouting-information-1001.log`, `scouting-roundtrip-1001.log`, `scouting-ui-console-repaired.log`. Earlier failed probes remain diagnostic artifacts: immutable term sheets needed a JSON constructor, and the UI fixture needed to match the handbill's uppercase title. Those repairs pass the recorded reruns. No new acceptance threshold or AI feedback was enabled to obtain these results.

Reproduce fixed checks with the repository's Godot 4.7 Mono console executable:

```powershell
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-experience-check
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-experience-check
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-room-check
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scene-information-check
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=26
```

Run game probes serially. The rendered probe uses `SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-experience-ui-check` with a real renderer (not headless). Temporary test save slots are uniquely named and deleted by the existing room fixture.
