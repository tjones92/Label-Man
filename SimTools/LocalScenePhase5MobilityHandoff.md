# Phase 5 continuation: counterfactual movement and opportunity coverage

Continues `codex/local-scene-phase-5` at `55ba2cc` in the existing scene
checkout. The primary band-member checkout and its existing changes are untouched.
The attached documents are design references; the current user request authorizes
this continuation and a quick vacancy investigation.

See `LocalSceneVacancyInvestigation.md`: the quoted 26% / 16% measures cumulative
affordable vacancy-slot burden against controls. It is not an empty-label rate.
The opening year and unresolved/disconnected headquarters dominate snapshot
differences. No broad hiring or population tuning is justified by this pass.

## Implemented

The existing `--observe-scene-dynamics` now writes bounded, immutable movement
leads in `scene-move-observations.csv`. They are counterfactual observations,
not events, approvals, funded relocations or guaranteed livelihood.

Only active, unsigned, contract-seeking, chronology-eligible acts with an active
performer are considered. Outstanding bills/residencies, guest commitments,
shared active personnel and recent recorded moves block consideration. Candidates
need a known domestic game-map road estimate within 350 miles and at least 1.5x
the existing genre opportunity prior. These limits and 250 miles/day are declared
simulation assumptions, not historical measurements or a new travel simulation.
Destinations are chosen by opportunity ratio, distance, then stable place ID.
The hash namespace and 13-week window bound attention to one lead per source
city per week. Repeated observations are not independent migration impulses.

Every row retains origin, source, destination, genre, time window, road estimate,
travel days, opportunity ratio, reason and unresolved consent/funding/housing/
employment/access/member-agreement requirements. Neither vacancies nor the named
room slot ratio chooses destinations. Live movement remains disabled.

The pressure CSV explicitly labels `opportunityCoverage` as
`NamedSupportingRoomsOnly` and leaves `fullEmploymentSlots` unknown. The 18
supporting weekly appearances cannot become a full-city carrying cap. This pass
does not fabricate missing background employment data.

No RNG stream, advancing saved state, birth owner, contract, residence, person,
capacity, institutional opening or feedback multiplier changes. Existing dated
genre priors are reused; no new historical institution/date claim is shipped.

## Verification

Build: zero errors, six existing warnings. Expanded dynamics checks pass 27
assertions per seed, covering world/RNG conservation, repeated observations,
bounded attention, travel time, commitment rejection, honest foreign/unknown
routes, preserved origin and save/reload reproduction. The existing nine-job
serial suite and matched economic hashes are recorded in
`SimLogs/scene5-mobility-v1/runs.json` and `analysis.json`.

Reproduce after building:

```powershell
./SimTools/RunPhase5ObservationChecks.ps1 -RunTag scene5-mobility-v2
python ./SimTools/analyze_scene_dynamics_observation.py scene5-mobility-v2
python ./SimTools/analyze_scene_vacancy_pressure.py
```

Use bundled Python if the system command is a Store stub. Full dynamic ecosystems
remain unfinished: sourced full employment/institution opportunities, funded and
consented relocation commit, bounded influence/feedback channels and their longer
matched calibration remain future work. Decade validation stays deferred under
the established handoff. This slice does not claim the vacancy screen repaired.


Completed validation: all nine serial jobs pass. Both seeds pass 27 dynamics
and 49 recruitment checks; 74 room checks and the 26-week gzip save/load pass.
Default/explicit/observe matched eight-week comparisons preserve all 84 existing
economic CSVs byte-for-byte. Observation emits 248 city/week rows, 1,581 prior
rows and 92 movement-lead rows. Known temporary-autoload diagnostics also occur
in the established controls. No decade simulation was launched.
