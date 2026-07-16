# Artist population runtime-label organic-growth RNG-alignment handoff

## Mission and authority

Restore common-random-number comparability at runtime-label birth without changing the implemented organic-growth policy, canonical tier capacities, disabled behavior, or inherited economic gates.

This is the authoritative next-pass handoff after the runtime-label organic-growth G3 seed-1001 stop recorded in `ArtistPopulationLifecycleAudit.md`. It supplements `ArtistPopulationRuntimeLabelOrganicGrowthHandoff.md` and supersedes that handoff only where it required the enabled runtime-birth path to consume no RNG at all. The organic-growth decision itself remains deterministic and RNG-free.

The G3 result remains a historical failure. Do not rewrite, relabel, delete, or overwrite its artifacts. This handoff authorizes one narrow RNG-alignment correction, its probes and telemetry proof, one fresh disabled replay, and a restarted seed-1001 validation ladder. It does not authorize an Album rule change, an acceptance-band change, demand or finance tuning, a Single-yield correction, a new shipping toggle, seeds 1002/1003, or a holdout.

Preserve the current uncommitted organic-growth implementation, including runtime/launch origin, exact birth metadata, target reasons, canonical capacities, quarterly one-slot growth, tier and acquisition reconciliation, enabled-only telemetry, and D6 probe 61. Do not reset or reconstruct the worktree.

## Retained historical results

Keep all prior decisions and failures intact:

- G2 passed: the fresh dual-disabled family matched all 45 retained streams byte-for-byte and emitted no target-event stream.
- G3 failed the inherited scheduled-Album gate at 1,310 enabled versus 1,083 control, or `1.2096x`, above `[0.80,1.20]`.
- G3 had zero organic target increases in 1960.
- The nine runtime-founded labels scheduled and released zero projects in both the preceding bootstrap-enabled run and G3.
- G4-G6, seeds 1002/1003, and the holdout remain unrun.
- The deferred Single-yield problem remains separate and unresolved.

The G3 stop was procedurally correct under the prior handoff. The result must not be represented as a pass merely because its cause is now attributed.

## Confirmed attribution

The G3 Album ratio is a format-mix result:

```text
(4,318 enabled decisions / 4,298 control decisions)
    * (0.303381 enabled Album share / 0.251978 control Album share)
    = 1.2096006
```

Decision volume is only `1.0047x` control. Album decision share is `1.2040x` control. The excess is therefore not runtime-label release volume.

The preceding frozen bootstrap-enabled run had 4,292 decisions, Album share `0.296831`, and 1,274 scheduled Albums. Against the same 1,083-project control this is `1.1764x`, inside the inherited Album band. The organic-growth source changed the 1960 numerator by 36 projects:

| Family | Weeks 1-18 | Weeks 19-52 | Total scheduled Albums |
|---|---:|---:|---:|
| Bootstrap enabled r3 | 465 | 809 | 1,274 |
| Organic-growth G3 enabled | 465 | 845 | 1,310 |

The first runtime label is born in week 18, and the first release/project-count divergence appears in week 19. In the preceding enabled source, `InitializeRuntimeRoster` called `AILabel.InitializeRoster`. Because a generated runtime label enters with `maxRosterSize == 0`, that helper consumed one tier-specific `GD.RandRange` draw and assigned a random hard capacity. The first Small runtime label received capacity 3. The organic-growth source instead assigns canonical Small capacity 5 and returns without consuming that draw.

The canonical capacity did not directly create output: the label remained empty and scheduled no project. Omitting the legacy draw displaced the shared global RNG stream for all subsequent stochastic decisions. The 36-project change is downstream random-stream phase movement among launch-population labels.

The G3 control is not stale. Its 45 common CSVs are byte-identical to `d6-runtime-label-bootstrap-control-r2-52-1001`. It remains the required broad disabled-lifecycle guardrail. The defect is narrower: after the first runtime birth, same-seed control and treatment no longer retain the historical random-call alignment needed for a sensitive one-seed boundary.

## Owner decision

Retain deterministic canonical hard-capacity values while restoring the one fixed legacy RNG consumption at each enabled runtime-label birth.

At enabled runtime birth only:

1. consume exactly one tier-specific legacy hard-capacity draw using the same Godot RNG API, argument types, and bounds previously reached through `AILabel.InitializeRoster`;
2. discard the returned value;
3. assign the canonical capacity from `LabelLifecycleManager.GetRosterCapacityForTier`;
4. set target one and preserve all current origin, birth, and target metadata; and
5. perform no signing or other birth-time work.

For the currently reachable runtime tiers, the compatibility calls are equivalent to:

```text
Small       -> (int)GD.RandRange(3, 10)
Independent -> (int)GD.RandRange(8, 18)
```

If the production spawn tier set later includes another tier, use the exact corresponding legacy `AILabel.InitializeRoster` range. Do not invent a new range.

The draw is an explicit compatibility token, not a capacity decision. The assigned hard capacity remains exactly `5 / 8 / 12 / 25 / 50`. Do not store, expose, branch on, or otherwise use the discarded result.

This owner decision supersedes only the prior statement that runtime birth itself consumes no RNG. It does not change these requirements:

- organic authorization consumes no RNG;
- promotions, demotions, acquisitions, and target reconciliation consume no new RNG;
- target growth adds at most one slot at a qualifying quarterly review;
- the weekly labor market remains the only signing path after birth; and
- no gameplay constant or gate changes.

## Implementation boundary

Make the smallest production change that expresses the compatibility contract clearly. Prefer one named helper at the runtime-initialization seam, such as `ConsumeLegacyRuntimeCapacityAlignmentDraw`, rather than an unexplained inline random call.

The helper must:

- execute exactly once for every enabled `RuntimeFounded` label;
- execute at the same relative initialization point as the removed legacy capacity draw;
- execute before any later callback that can consume shared RNG;
- consume no draw for launch-population labels;
- consume no draw at promotion, demotion, acquisition, or organic growth;
- leave the disabled route exactly unchanged; and
- never call `InitializeRoster` or any launch-population roster helper merely to obtain the draw.

Do not add a general RNG refactor, per-subsystem RNG architecture, feature flag, debug suppression switch, Album-specific fork, or seed-specific branch in this pass. Those are outside scope.

## Fixed-probe amendment

Retain D5 and D6 probes 1-61. Amend probe 61 only as needed to distinguish deterministic value selection from compatibility consumption, and add focused production-helper coverage proving:

1. enabled Small runtime birth consumes exactly the legacy Small draw and still assigns hard capacity 5;
2. enabled Independent runtime birth consumes exactly the legacy Independent draw and still assigns hard capacity 12;
3. the post-initialization RNG state matches the legacy one-draw path, not the current zero-draw path;
4. the discarded value cannot affect capacity, target, metadata, roster, or signing;
5. target remains one and roster remains empty at birth;
6. organic growth, tier reconciliation, and acquisition reconciliation add no compatibility draws; and
7. disabled initialization retains its existing behavior and random-call order.

Use a production helper or injectable draw seam for the probe. Do not duplicate the policy as probe-only logic, and do not reseed or perturb the live simulation from telemetry.

## Validation ladder

Use seed 1001 only until the original G6 ladder explicitly unlocks later work. Preserve every prior artifact and use new run names.

Suggested family:

```text
d6-runtime-label-growth-rngalign-probes-1001
d6-runtime-label-growth-rngalign-disabled-52-1001
d6-runtime-label-growth-rngalign-control-52-1001
d6-runtime-label-growth-rngalign-enabled-52-1001
d6-runtime-label-growth-rngalign-control-104-1001
d6-runtime-label-growth-rngalign-enabled-104-1001
d6-runtime-label-growth-rngalign-enabled-repeat-104-1001
d6-runtime-label-growth-rngalign-maturity-control-260-1001
d6-runtime-label-growth-rngalign-maturity-enabled-260-1001
d6-runtime-label-growth-rngalign-decade-control-1001
d6-runtime-label-growth-rngalign-decade-enabled-1001
```

### R0 - retained-artifact preflight

Before editing source, record and verify:

- current functional-source hashes;
- G3 totals `1,310 / 1,083 = 1.2096x`;
- prior bootstrap-enabled r3 total `1,274` and ratio `1.1764x`;
- exact equality through week 18 and first count divergence in week 19;
- zero runtime-origin projects and zero organic increases in G3; and
- 45/45 equality between the G3 control and bootstrap control r2.

### R1 - implementation, build, and probes

Implement only the fixed compatibility draw and its production-helper probe. Run `git diff --check`, build with `dotnet build "Label Man.sln" --no-restore`, and run the complete accepted D5/D6 probe command. Stop on any failure.

Record the corrected functional-source manifest and the exact helper call used for each reachable runtime tier.

### R2 - disabled no-op proof

Run a fresh 52-week dual-disabled aggregate replay. Require:

- exactly the retained 45 CSV suffixes;
- 45/45 byte equality with `d6-transition-envelope-disabled-52-1001` and the prior G2 family;
- no target-event stream; and
- no missing or extra stream.

Any disabled difference is an implementation defect. Correct only that defect, rebuild, rerun probes, and restart R2.

### R3 - restarted 52-week boundary

Run a fresh control first and then enabled treatment using the exact G3 feature switches and seed. Do not reuse a stale process and do not overwrite G3.

Require the fresh control's 45 common streams to remain byte-identical to the retained G3 control. In the enabled run require:

- runtime labels retain canonical hard capacities 5 or 12 and target one at birth;
- no birth-week signing;
- zero organic increases if the same 1960 qualification history is restored;
- zero runtime-origin projects if the same 1960 history is restored;
- all target, population, ownership, chronology, project, release, and finance invariants reconcile; and
- every inherited 1960 gate passes.

The primary RNG-alignment proof is that all schema-stable gameplay/economic streams match the prior bootstrap-enabled r3 family byte-for-byte through the point where an actual organic increase or another intentionally changed production event first occurs. Telemetry streams with new columns and the new target-event stream are compared semantically, not byte-for-byte.

If no organic increase occurs in 1960, the expected boundary is the prior enabled result: 1,274 scheduled Albums against 1,083 control, or `1.1764x`. Treat this as an expectation and attribution check, not permission to hard-code a total. If the aligned run differs, stop at the first differing week and identify the next state or RNG-consumption difference. Do not tune Albums, cadence, growth eligibility, or acceptance bands.

If R3 passes, record the historical G3 failure and the new R3 pass as separate results. Never rewrite G3.

### R4-R6 - resume the original growth ladder

Only after R3 passes, resume G4-G6 from `ArtistPopulationRuntimeLabelOrganicGrowthHandoff.md` using the new `rngalign` family and unchanged source:

- R4: fresh 104-week control/treatment plus an independent enabled repeat; require repeat byte equality and all inherited annual gates.
- R5: fresh 260-week control/treatment; require the original annual release, Album, catastrophic-economic, reconciliation, and structural gates.
- R6: fresh paired 522-Friday seed-1001 decade; require the original annual gates and nonzero, reconciled mature runtime-label contribution.

At R4-R6, actual target increases are intended causal events. Reconcile every increase to its eligibility evidence, the subsequent weekly vacancy, and any later signing and project output. Do not require byte equality with pre-growth enabled artifacts after the first authorized organic increase.

Seeds 1002/1003 and a holdout remain prohibited. The separate Single-yield surface remains deferred exactly as specified by the original handoff.

## Stop conditions

Stop and preserve artifacts at the first occurrence of any of the following:

- more or fewer than one compatibility draw at an enabled runtime birth;
- a discarded draw influencing canonical capacity or target;
- any disabled-path byte difference;
- birth-week signing or target above one;
- organic growth outside the existing quarterly eligibility contract;
- a schema-stable pre-growth stream diverging from the expected aligned history without a documented causal event;
- any inherited release, Album, economic, population, finance, chronology, ownership, or structural gate failure; or
- any attempt to repair the result through Album, demand, finance, release, Single-yield, growth-cadence, or acceptance-band tuning.

One implementation correction is allowed only when probes or first-divergence evidence show that this handoff's fixed-draw contract was implemented incorrectly. Otherwise stop and write the result.

## Required audit record

Append the completed result to `ArtistPopulationLifecycleAudit.md`. Include:

- the historical G3 result without revision;
- the attribution equation for decision volume and Album share;
- the exact first runtime-birth and first-divergence weeks;
- old and new capacity/draw behavior;
- source hashes, commands, completion markers, and probe result;
- disabled 45-stream comparison;
- control and enabled totals and ratios;
- byte/semantic alignment against bootstrap-enabled r3;
- target-event and runtime-origin project counts; and
- the exact stop or authorization decision for the next rung.

Do not describe RNG alignment as an Album calibration. It is a same-seed experimental-control repair that preserves deterministic capacity values and restores the legacy random-call schedule until an intended organic-growth event changes simulation state.
