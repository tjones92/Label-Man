# Label Lifecycle-Calibrated 1965 Acceptance and Album Attribution Handoff

## Purpose

Run exactly one seed-1001 simulation through the completed-1965 acceptance gate using the current label-lifecycle calibration. Determine whether MidTier representation has returned to approximately the retained control share without losing the current lifecycle, promotion, daily-market, and organic-growth features.

If the population and release gates pass, continue the analysis—not the simulation—to determine whether any remaining Album failure is caused by Album count, realized yield, portfolio mix, or within-cohort emerging-genre performance.

Do not alter Album, genre, promotion, or lifecycle policy during this pass. Preserve the run and stop after reporting.

This is the prospective successor to `SimTools/MidTierPopulation1965TestHandoff.md`. Keep that file as the historical record of the earlier test.

## Fixed implementation under test

The run must use the current implementation, including:

- daily genre-market behavior;
- artist population lifecycle;
- runtime-founded labels and organic label growth;
- the existing MidTier promotion evidence gates;
- the enabled-only quarterly competitive label exit mechanism;
- the deterministic label-competition draw isolated from Godot's global random stream;
- competitive-exit base chance `0.03`;
- launch-population minimum operating age of 9 months;
- runtime-founded minimum operating age of 12 months;
- zero recent charting records as an exit prerequisite;
- D6 label-competition calibration probe 69.

Do not weaken promotion evidence gates or compensate for a result by changing Album volume, genre demand, or release-success logic.

## Required preflight

1. Preserve all existing unrelated worktree changes.
2. Run:

   ```powershell
   git diff --check
   dotnet build "Label Man.sln" --no-restore
   ```

3. Confirm the source still contains the fixed calibration above and probe 69 passes.
4. Confirm no Godot process from an earlier test is running.
5. Confirm the new run prefix does not already exist.
6. Confirm the initial label directory is 600 labels total, including 86 MidTier labels.

Godot previously crashed during startup inside the sandbox. Launch the executable outside the sandbox after obtaining the required user approval. Do not substitute a different executable or project checkout.

## The only authorized simulation

Use this fresh run prefix:

`d6-label-competition-midtier-through-1965-1001`

Run:

```powershell
$godotExe = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godotExe --headless --path . SimTools/ChartAuditRunner.tscn -- `
  --weeks=314 `
  --run=d6-label-competition-midtier-through-1965-1001 `
  --seed=1001 `
  --enable-genre-market-v2 `
  --enable-artist-population-lifecycle `
  --genre-market-v2-probes `
  --artist-population-lifecycle-probes `
  --profile-performance `
  --catastrophic-fail-fast `
  --strict-1965-acceptance-gate `
  --gate-control-run=d6-transition-envelope-decade-control-1001
```

Week 314 is intentional: it crosses the completed-1965 strict gate.

Run this command once. Do not rerun it, change the seed, launch a repeat, run a holdout, or start a decade simulation. If it crashes or produces incomplete output, preserve the partial artifacts and report the failure without launching a replacement.

## Evaluation order

Apply the checks in this order. A later success does not override an earlier failure.

### 1. Structural integrity

Require:

- a normal process exit;
- all required probes passing;
- finite metrics;
- no catastrophic fail-fast event;
- complete annual and cohort output through 1965;
- release/project reconciliation without unexplained losses or duplication.

If this fails, stop attribution at the structural fault.

### 2. Overall release and lifecycle envelope

The prior 1965 attempt never reached its intended result because completed-1964 successful releases were `4,367 / 3,330 = 1.311411x`, above the inclusive `1.30x` ceiling. Check that gate before interpreting MidTier or Album results.

For each completed year, report:

- successful releases and candidate/control ratio;
- release decisions;
- successful-release rate;
- mean active labels;
- distinct participating labels;
- releases per mean active label;
- participating labels by launch-population versus runtime-founded origin;
- the same population and participation measures by tier.

Use the exact strict-gate result as authoritative. For directional interpretation, treat `[0.85x, 1.15x]` as the ordinary release band and `[0.70x, 1.30x]` as the inclusive emergency envelope.

If the release gate fails, stop. Attribute the miss to active population, participation, decisions per active label, or success rate. Do not proceed to an Album-policy recommendation.

### 3. MidTier result and promotion integrity

The retained control reference for completed 1965 is:

- mean MidTier active-label share: `16.39%`;
- mean MidTier active-label count: `55.51` of `338.75`.

The primary directional pass range is `14%` through `19%` mean MidTier active-label share. Report both share and count; do not claim alignment from the percentage alone if the entire active-label population remains materially inflated.

Also report the completed-1965 tier footprint for:

- mean active labels;
- release decisions;
- roster;
- release-eligible artists;
- market units;
- market gross;
- selling-label gross concentration.

For every observed MidTier promotion, require:

- reason `PromotionReconciliation`;
- correct destination tier;
- origin separated into `LaunchPopulation` and `RuntimeFounded`;
- at least 2 recent charting records;
- roster size at least 6;
- positive last-month profit;
- zero consecutive loss months;
- at least 6 runway months;
- no promotion inside the first 18 operating months.

More than 12 distinct launch-population promotions through completed 1965 is a directional review condition even if every individual promotion is valid.

Classify the outcome explicitly:

- **MidTier passed and overall population/release envelope passed**: continue to economic and Album attribution.
- **MidTier passed but overall population/release envelope failed**: the next repair remains overall survival/participation; do not tune Albums or MidTier.
- **MidTier failed**: preserve the population, origin, closure, and promotion evidence and stop before Album-policy work.

### 4. Inherited economic acceptance

Only after the preceding gates pass, report the exact strict-gate candidate/control ratios for:

- Single units;
- Album units;
- total units;
- gross;
- label net;
- market net;
- scheduled Album projects;
- completed Album drops, when available.

Do not infer a pass from the earlier 104-week calibration. The completed-1965 output is authoritative.

## Album count-versus-yield review

Perform this review only if MidTier, overall release/lifecycle, and inherited economic prerequisites have passed far enough for Album attribution to be meaningful.

First ask whether Album count failed, Album yield failed, or both. Report:

### Count and pipeline

- scheduled Album projects;
- Album release decisions;
- completed Album drops;
- cancelled, rejected, deferred, and other terminal project states;
- candidate/control count ratios by tier, origin, genre lifecycle state, and release quarter.

Apply the exact strict-gate band printed by the runner. For directional comparison, report scheduled Album projects against `[0.80x, 1.20x]` and completed releases against the inherited release-count band where applicable.

### Yield

Report at least two distinct measures:

1. **Market-year proxy:** annual Album units divided by completed Album drops. Label this a proxy because annual units include catalog carryover from older releases.
2. **Matched-cohort observed yield:** realized units for candidate and control Album cohorts matched by release quarter and exposure age. State the observation cutoff and identify right-censored releases.

Use lifetime yield only for projects whose lifetime outcome is actually complete. Do not describe an incomplete 1965 release cohort as lifetime yield.

For the matched cohorts, also report:

- pooled Album appeal;
- launch awareness;
- initial stock or supply;
- demand cleared versus unfilled/backordered demand;
- catalog reuse and freshness;
- realized units per completed Album.

Join count and yield by tier and genre. A release-count increase is not an acceptable substitute for weak yield.

As an attribution identity, use:

`Album-unit ratio ~= completed-drop ratio * units-per-drop ratio`

Then separate:

- the count contribution;
- portfolio/tier/genre mix contribution;
- within-cohort yield contribution.

Use the existing Album bridge, reconciliation, pipeline, cohort-clearing, and catalog-cohort analyzers where their input artifacts are present. If a required field is unavailable, mark that measure unavailable rather than silently substituting a different quantity.

## Decision after the Album review

### Album count fails, yield is approximately in band

The next pass should investigate the Album project/release pipeline: scheduling, eligibility, deferral, cancellation, and completion. Do not change genre conversion or demand.

### Album count passes, yield fails

Proceed to matched-cohort yield attribution. Determine whether the deficit is systemic or localized before authorizing any policy change.

### Both count and yield fail

Keep the causes separate. Investigate pipeline count first while preserving yield evidence. Do not raise Album volume merely to conceal a yield deficit.

### Count and yield both pass

No Album or genre follow-up is authorized from this run.

## Conditional emerging-genre investigation

Do not jump directly from a total Album-unit failure to emerging-genre conversion.

Classify projects using `GenreCatalog` lifecycle state at the project decision or scheduling year. Use the code's `Emerging` state; do not create a hand-written list of “emergent” genres. Report `PreEmergent` separately as an invalid or exceptional release state. Compare:

- `Emerging`;
- `Established`;
- `Declining`;
- `Legacy`.

Where existing telemetry permits, distinguish retained from non-retained emerging projects. Match cohorts by release quarter/exposure age and further split by tier and label origin.

An emerging-genre Album-ratio investigation is authorized as the next handoff only when all of the following are true:

1. Album project/drop count ratios are in their applicable bands.
2. Overall Album units or matched units-per-drop yield is outside its applicable band.
3. Non-emerging matched-cohort yield is in band or materially closer to control.
4. Emerging cohort mix or within-cohort performance explains a majority of the absolute Album-unit deficit; use more than 50% as a directional authorization threshold.
5. Each compared arm has an adequate finite denominator. Use at least 30 completed Album drops per arm for a quantitative claim; otherwise report the result as insufficient evidence.

Interpret the decomposition as follows:

- **Emerging share/mix is wrong, but within-emerging yield is in band:** investigate portfolio selection and genre mix, not conversion.
- **Within-emerging yield is low with count and exposure matched:** the next handoff may investigate emerging-genre Album conversion, demand, launch stock, and market clearing, one seam at a time.
- **Both emerging and non-emerging yield are low:** investigate a systemic Album realization, stock, clearing, or catalog issue.
- **Only a tier or origin slice is low:** investigate that localized lifecycle/portfolio interaction instead of a global genre rule.

This 1965 run authorizes diagnosis only. It does not authorize changing emerging-genre Album ratios in the same pass.

## Required final report

Write one report containing:

1. command, seed, prefix, weeks, exit status, and runtime;
2. preflight and probe results;
3. the first strict-gate failure, or an explicit statement that the strict gate passed;
4. annual 1964 and 1965 release/lifecycle attribution;
5. completed-1965 tier footprint and MidTier comparison to `16.39%` / `55.51`;
6. promotion-integrity table and launch-promotion count;
7. inherited economic ratios;
8. Album count-versus-yield decomposition, if authorized by the earlier gates;
9. emerging versus non-emerging cohort attribution, only if the Album evidence warrants it;
10. one next-pass classification:
    - no follow-up required;
    - overall label survival/participation;
    - MidTier lifecycle/promotion;
    - Album project pipeline;
    - systemic Album yield;
    - portfolio/genre mix;
    - localized emerging-genre Album yield;
    - insufficient evidence.

End with a firm stop statement confirming that no second simulation, alternative seed, holdout, decade run, or policy change was launched.

## Stop conditions

Stop immediately after the single 314-week run and its analysis. Preserve all artifacts.

Do not:

- rerun a failed or ambiguous result;
- start a second seed;
- start a repeat or holdout;
- start a decade simulation;
- edit lifecycle, MidTier, Album, or genre policy;
- treat a MidTier percentage pass as sufficient when overall label population or releases still fail;
- treat Album-unit failure alone as proof of an emerging-genre conversion problem.
