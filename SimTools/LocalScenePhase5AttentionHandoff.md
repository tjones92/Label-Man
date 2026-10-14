# Phase 5 continuation: bounded signing attention

Continues `codex/local-scene-phase-5` after the user-authorized HQ access repair.
The primary band-member checkout remains untouched. The earlier directive and
atlas are reference documents; the current request authorizes implementation.

## Implemented channel

The existing `--observe-scene-dynamics` projects committed contract events into
bounded signing attention. It emits `scene-attention-weekly.csv` for all 31
playable cities and `scene-attention-evidence.csv` with source event IDs, artist,
label, signing week, location, event-time genre, attribution and decayed impulse.
The evidence is the persisted recruitment history, not hypothetical offers.

Each signing has an authored impulse budget of 0.05: working base 60%, origin
20%, label HQ 20%. Catchments merge location claims, so NYC/Newark or SF/Oakland
do not count the same contract twice. Unknown claims retain no budget and are
not redistributed. Nonplayable/foreign claims retain evidence but are not
invented as additional playable scenes. Initialization and test phases are
excluded. DailyMarket, WeeklyMarket and PlayerRival are supported commit phases.

Attention decays with an eight-week half-life, a 104-week routine horizon and
`1-exp(-rawImpulse)` saturation. The constants are declared simulation priors,
not historical measurements or calibrated live price/population multipliers.
Repeated observations are not events. Projection is sorted and duplicate event
keys are suppressed. Ledger identity uses artist/label/week/commit phase; the
existing ledger cannot distinguish repeated same-pair same-week contracts.

`SceneRecruitmentRecord.SigningGenre` captures genre at the signing. This nullable
field is backward compatible; missing legacy values remain unknown. The observer
never assigns a historical genre from the act's current style.

No advancing save sequence or mutable heat stock is added. Save/reload derives
the same attention from the same authoritative ledger. No RNG, bids, prices,
formation, talent, scene placement, contract choice, relocation or development
reader consumes this channel. Live dynamics remain gated pending calibration.

## Validation

Build has zero errors and six existing warnings. The exact nine-job serial suite
is in `SimLogs/scene5-attention-v1/runs.json`; the economic comparison and row
reconciliation are in its `analysis.json`.

Fixed probes cover repeated reads, all cities, state/RNG preservation, attribution
conservation, duplicate suppression, opening/future event exclusion, half-life,
bounded horizon and saturation, ordering, legacy genre uncertainty and save/reload.
The suite also retains both recruitment seeds, 74 room checks and 26-week gzip IO.
Default, explicit recruitment and observed eight-week runs require all 84
economic CSVs to match byte-for-byte. No decade run is launched.

Reproduce with a new tag:

```powershell
./SimTools/RunPhase5ObservationChecks.ps1 -RunTag scene5-attention-v2
python ./SimTools/analyze_scene_dynamics_observation.py scene5-attention-v2
```

Full Phase 5 remains open: employment/institution opportunity, consent and funding
for actual relocation, dated institution/era authoring, diffusion and independently
gated live feedback need implementation and longer calibration. Full four-year
vacancy revalidation and final paired-seed decade acceptance remain outstanding.


Completed final validation: both seeds pass 38 dynamics and 56 recruitment
assertions; 74 room checks and the 26-week gzip save/load pass. All nine suite
jobs finish successfully. Both matched eight-week observer comparisons preserve
all 84 economic CSVs exactly. The eight-week window contains no runtime contracts,
so its zero attention confirms that opening allocation is not rewarded.

The additional `scene5-attention-integration-v1` 26-week run exercises positive
events: 806 city/week rows and 1,019 event-attribution observations reconcile to
all 60 committed runtime signings, with known event-time genres, budget <= 1,
exact decay and saturation, and population integrity maxima zero. Maximum observed
signing heat is 0.346296381. Booking census has zero duplicate bills, person
conflicts, remote days and capacity errors. Existing temporary-autoload messages
are the known control diagnostic; every required marker/exit code passes.

Reproduce this nonempty integration separately with a fresh tag:

```powershell
./SimTools/RunSceneAttentionIntegration.ps1 -RunTag scene5-attention-integration-v2
python ./SimTools/analyze_scene_attention_integration.py scene5-attention-integration-v2
```

The HQ repair's first-year vacancy reduction is 68.60% / 83.00% versus the
previous recruitment build. It does not replace the outstanding four-year
pressure screen or deferred decade acceptance. All simulation processes have
finished. Current changes remain in the existing scene checkout for review.

## October 9 pressure revalidation update

The user replaced the outstanding four-year pressure revalidation with matched two-year runs. All four 104-week runs (seeds 1001 and 2002, recruitment off/on) pass the economic and pressure screens with zero checked integrity errors or unexplained signings. See `LocalScenePhase5TwoYearPressureHandoff.md` and `SimLogs/scene5-two-year-pressure-v1/report.md`. This supersedes the earlier outstanding four-year requirement for this continuation. Live Phase 5 feedback implementation and final decade acceptance remain separate unfinished work.

## October 9 live feedback continuation

The first live consumer is implemented behind `--enable-scene-attention-feedback`: bounded, delayed, genre-specific signing attention changes AI discovery weights within existing access and slate budgets. Both matched 104-week seed comparisons and the expanded fixed/save suite pass. See `LocalScenePhase5LiveFeedbackHandoff.md` and `SimLogs/scene5-live-feedback-two-year-v1/report.md`. Remaining live channels and final decade acceptance remain separate work.
