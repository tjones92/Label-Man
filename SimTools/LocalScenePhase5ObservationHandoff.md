# Local scene Phase 5: observation baseline

Branch: `codex/local-scene-phase-5`, continuing from Phase 4 `2992acb` in the
existing scene worktree. The separate primary band-member checkout remains
untouched. The user authorized a four-year economic gate, recruitment default-on
if that gate passed, then starting Phase 5. Decade validation remains deferred.

## Phase 4 decision

See `LocalScenePhase4FourYearHandoff.md` and the exact manifest/analysis in
`SimLogs/scene4-four-year-v1/`. Both matched 208-week economic comparisons pass.
Whole-market singles change -1.83% / -0.53%, Albums +5.02% / +0.69%, and net
+2.72% / -0.15%. Integrity and unexplained signings are zero. Label failures
fall. The user explicitly accepts EMI and Decca UK as legacy exceptions outside
the intended US recruitment scope.

Recruitment now defaults on with persistence and the genre market. Explicit
`--disable-scene-recruitment` remains available. Identity-only, all-off, population
and genre dependency-off modes suppress that default. Existing saved contracts
and locations are preserved; loading does not rerun opening allocation.

The vacancy pressure diagnostic remains open: after separating the two accepted
UK exceptions, affordable vacancy-slot weeks rise 26.35% / 15.99%. Seed 1001
narrowly fails the preregistered 25% pressure screen. Economic acceptance under
the user's condition is distinct from claiming every recruitment/playability
measure or decade balance has passed. No advances, births, talent quality,
roster targets or geographic weights were retuned to secure this decision.

## Implemented first slice

`--observe-scene-dynamics` on the standard headless `ChartAuditRunner` emits:

- `scene-dynamics-weekly.csv`: all 31 domestic playable places, active residents,
  working residents, signed/seeking/latent contract state, potentially bookable
  residents, dated performance slots, existing booked appearances and unique acts,
  recurring engagements, active HQ labels and operating-slot deficits.
- `scene-opportunity-priors.csv`: each city's existing genre-placement weight,
  its 1960 reference and ratio, once per observed calendar year. These are the
  previously authored simulation priors, not new historical census measurements
  or claims that a genre exists before its chronology admits formation.

The observation reads the next complete calendar week already owned by the room
service. It never calls `EnsureCalendar`, visits a room, activates a prospect,
reserves a set or advances a simulation tick. Stable place IDs and scalar-only
immutable observations prevent extra artist copies or mutable calendar aliases.
There is no new saved simulation state, advancing RNG, heat impulse, institution
opening, formation owner, relocation decision or feedback reader.

Important denominators: the resident cohort is selected simulation population;
the five named performance rooms are supporting institutions, not a census of
all employment. Trade listening appointments provide no stage slots. The current
fictional profiles expose 18 weekly appearances per city. Potential bookability
means existing genre/presence/person eligibility on at least one dated night;
it does not promise a free slot, no shared-person conflict, payment or a contract.
The ratio must not become a city cap or relocation rule without a separate full
opportunity model and calibration. Operating deficits are headcount targets,
not affordability-weighted demand or proof of accessible supply.

`--observe-local-scenes` remains the earlier identity-only control. It is distinct
from `--observe-scene-dynamics`, which requires persistence and rooms. Invalid
observer dependencies reject before configuration changes. Live
`--enable-scene-dynamics` remains unsupported.

## Verification

`dotnet build --no-restore` passes with zero errors and the six existing warnings.
`SimLogs/scene5-observation-v1/runs.json` records nine completed serial checks,
exact arguments and assembly/source hashes; no simulation remains running.

- Both seeds pass 19 observer/configuration checks: default on, explicit off and
  dependency controls, early contradiction rejection, unchanged complete world
  state and global RNG, repeat-read identity, all 31 cities, denominators, dated
  slots, no trade-stage credit, save/reload identity and missing-room rejection.
- Both seeds pass all 49 existing recruitment checks using the new default,
  without the explicit enable flag.
- The 26-week world/player gzip save/load test passes with default recruitment.
- All 74 existing room checks pass with the new default.
- Three matched eight-week seed-1001 runs compare default, explicit recruitment
  enablement and default plus observation. All 84 existing economic CSVs are
  byte-identical in both comparisons. The observer emits 248 unique place/week
  rows, reconciles booking/resident denominators and emits 1,581 opportunity rows.

The known temporary-autoload diagnostic also occurs in earlier controls and did
not introduce a new runtime failure. This is a validated read-only Phase 5
baseline, not completion of dynamic ecosystems.

Reproduce using a new run tag:

```powershell
./SimTools/RunPhase5ObservationChecks.ps1 -RunTag scene5-observation-v2
python ./SimTools/analyze_scene_dynamics_observation.py scene5-observation-v2
```

Use the bundled Python executable if the `python` command is a Windows Store stub.
Next Phase 5 work: distinguish full opportunity from named-room attention budgets;
author sourced institutional/era changes; observe travel-feasible move proposals
and bounded influence before enabling separate live feedback channels. Keep one
birth owner, immutable origin, causal move provenance and save/replay conservation.
The final paired-seed decade acceptance stays deferred until the end.
