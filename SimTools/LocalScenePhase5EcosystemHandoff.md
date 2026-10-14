# Phase 5: funded programs, voluntary relocation and price feedback

Continues the current Phase 5 work, including its uncommitted attention, mobility,
HQ identity and recruitment repairs, in this chat's attached worktree on
`codex/local-scene-phase-5-feedback`. The prior scene worktree and primary
band-member checkout are preserved. The user's request authorizes implementation;
the attached directive, atlas and earlier sketch are design references.

## Behavior and independent gates

Validated scene attention feedback now defaults on when persistent rooms and
geographic recruitment are enabled. Explicit `--disable-scene-attention-feedback`
preserves the control. Dependency-off modes suppress the default, and explicit
contradictions reject before changing configuration.

The following slices are implemented and independently opt-in:

- `--enable-scene-institutions`: a label can fund a fictional rehearsal/showcase
  program from the named-room A&R board. It costs $600, preserves the sponsor's
  operating reserve, starts in 14 days, lasts 91 days and permits one extra set
  per existing performance night. Programs cannot stack in a room. Existing
  announced bills are preserved; newly scheduled bills use the dated extra slot.
  Genre programming, person conflicts, stage hours and receipts remain owned by
  the room service. Expiry returns capacity to two sets. No artist births,
  automatic talent bonus or full-city employment estimate is introduced.
- `--enable-scene-relocation` additionally requires institutions. The Band Room
  offers discussions for the label's funded destination programs. Only the
  sponsor's active roster acts can move. All active members must agree, existing
  bills/residencies/guest or road/session bookings and shared personnel must be
  clear, and a recent move blocks another. A suitable domestic destination needs
  at least 1.5 times the existing genre-opportunity prior and a known road route
  within 350 miles. Consent is keyed to person, source, destination and quarter;
  repeatedly pressing the button cannot reroll it. Ambition, children and fatigue
  affect consent, never musical ability. Travel and temporary housing cost
  $100/person plus $0.50/mile, prepaid separately. Each program supports one
  housing/work introduction, with at least four weeks remaining after arrival.
  These costs and thresholds are gameplay assumptions.
- `--enable-scene-price-feedback`: AI advance calculations and player venue asks
  use a separate bounded multiplier `1 + 0.10 * genreHeat`. The same conserved
  signing ledger, place attribution, one-week lag, eight-week half-life and
  expiry drive the price reader. A single local signing gives about a 0.49%
  increase. Affordability gates, managerial terms and committed advances use the
  same AI calculation once. Player heat applies after baseline rounding so a
  low-dollar ask cannot exceed the 10% limit through rounding. Unknown places,
  unknown recorded genres and initialization contracts give no price heat.
  Price feedback can be enabled with discovery feedback explicitly disabled.

There are matching `--disable-scene-*` controls. Broad
`--enable-scene-dynamics` remains unsupported; it would imply more than these
separate channels. Explicit observer/dependency contradictions remain invalid.

## Timing, history and saves

Relocation is a planned, dated commitment. Acts keep their base until arrival;
room booking rejects their work from departure while travel is pending. New
pre-departure sets cannot create a residency across the move. The clock advances
arrivals independently of visits, before scheduling the day's opportunities.
Contract, career, lineup, sponsor, program, current genre/era opportunity and shared-person conditions are
rechecked at arrival. Failed commitments cancel without changing bases;
prepaid travel/housing remains spent. Moves do not create contracts or rewrite
commercial `homeRegion`, which retains its compatibility role.

Arrival records dated act and person base histories, preserves each origin,
reconciles resident and geographic indices and retains career provenance.
Repeated ticks do not apply an arrival twice. Disabling relocation freezes its
pending commitments; it does not erase population or permit stage work in transit.

Save envelope v9 carries a separately versioned ecosystem ledger with programs,
move commitments, costs, member IDs, dates, statuses and processed-day marker.
It stores references, not artist copies. Older saves have no programs or moves.
Validation precedes manager mutation; restore/rollback follows the existing
world-plus-player relinking boundary. Derived attention/price snapshots reset on
load. Read APIs return detached program/move snapshots.

`--scene-ecosystem-audit` writes weekly enabled modes, active funded programs,
planned/arrived/cancelled moves and maximum price multiplier. Ordinary AI labels
do not automatically fund programs or relocate. Room investment changes actual
supporting opportunities; it does not guarantee work, a record deal or livelihood.

## Validation

The initial 14-job serial suite passes: both seeds' ecosystem assertions, the
all-slice 26-week gzip save/reload, both seeds' attention/dynamics/recruitment
checks, the 74 room checks and the matched eight-week controls. Default, explicit
and observer comparisons preserve all 84 economic CSVs. Evidence:
`SimLogs/scene5-ecosystem-fixed-v1/` and
`SimLogs/scene5-ecosystem-fixed-v1-regression/analysis.json`.

The final player-rounding fix passes 41 focused checks on each seed and the
all-slice 26-week gzip round-trip in `SimLogs/scene5-ecosystem-final-v1/`.
Both seeds also pass 43 checks in `SimLogs/scene5-ecosystem-timeline-v1/`, including
actual third-set scheduling within room hours and no new source engagements
across departure. The room scheduler's internal planned-move predicate avoids
serializing detached move snapshots while iterating booking candidates; its
selection predicate is unchanged.

The final arrival suite passes 45 assertions on each seed in
`SimLogs/scene5-ecosystem-arrival-final-v1/`. It includes cancellation when a
changed genre no longer fits the funded destination, a current-year opportunity
recheck, and distinct opening/middle/closing roles on three-set bills.

The rendered funding-button test passes, creates exactly one program, charges
$600 and displays the dated program. The screenshot and UI text are in
`SimLogs/scene5-ecosystem-ui-v1/`. Two unchanged menu-image import caches were
restored from matching local assets after the first render reported missing
textures. The final UI recheck passes with no missing-resource errors
(`tmp/ecosystem-ui-final-console.log` and `tmp/ecosystem-ui-final-errors.log`). The known
temporary-autoload diagnostic is retained from earlier controls.

All four serial 104-week price runs complete successfully. Both pairs pass the
established economic, vacancy and integrity screens. Manifests and full analysis:
`SimLogs/scene5-price-two-year-v1/runs.json` and `analysis.json`.

| Price-on change versus attention-on, price-off control | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Singles units | +0.0024% | +0.3149% |
| Album units | -0.0086% | -1.2946% |
| Market net | -0.00010% | -0.6285% |
| Final roster headcount | +0.37% | +0.74% |
| Formations | unchanged | unchanged |
| Affordable vacancy burden excluding accepted UK exceptions | -0.57% | unchanged |
| Maximum observed price multiplier | 1.07242 | 1.06899 |

Both price-off controls match the previously accepted attention-enabled runs
across all 84 economic streams (`control-equivalence-1001.json` and
`control-equivalence-2002.json`). All four runs have zero population/booking
integrity violations, unexplained signings or unexpected runtime errors.
Tier/annual outcomes remain available in the analysis; these are development
screens, not statistical significance or decade acceptance.

The final genre-arrival guard and three-set role labels were tightened after the
price audit and verified by the two-seed 45-assertion suite. These affect funded
move/program paths, which were inactive in the matched price runs. The audited
price, attention, recruitment and contract-price readers remain unchanged;
`final-provenance.json` records their hash comparison and the final fixed-suite
assembly. No simulation or UI test remains running.

Reproduction (use fresh run tags):

```powershell
dotnet build --no-restore
./SimTools/RunSceneEcosystemChecks.ps1 -RunTag scene5-ecosystem-fixed-v2
./SimTools/RunSceneEcosystemAudit.ps1 -RunTag scene5-price-two-year-v2 -Weeks 104
python ./SimTools/analyze_scene_ecosystem.py scene5-price-two-year-v2
```

Use the bundled Python executable if `python` resolves to the Windows Store stub.
The matched price audit keeps attention enabled on both sides. Institution and
relocation flags are also enabled, but no programs are funded automatically;
the long comparison isolates price, while controlled fixtures exercise funding,
travel and arrival. This is not a calibration of autonomous AI investment/migration.

Full background employment, sourced dated historical institution openings and
closures, autonomous investment/migration, broader influence/diffusion and scene
news remain follow-on scope. This slice adds fictional funded supporting
programs without claiming new historical facts. Paired-seed decade acceptance
remains deferred under the prior handoff.

## Phase 6 default update

The opt-in descriptions above record Phase 5's original state. Phase 6 now enables institutions, relocation and price feedback by default under their dependencies, with independent explicit off controls. See [LocalScenePhase6Handoff.md](LocalScenePhase6Handoff.md) for current behavior and validation.
