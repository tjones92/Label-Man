# Format-decision causal trace

## Scope

This is an offline causal trace of the existing 52-week seed-1001 telemetry. It does not fit, tune, or alter the decision-noise or final fork formula.

Run the reproducible analyzer with:

```powershell
& $node SimTools/analyze-format-decision-causal-trace.mjs `
  d5-phase2-format-causal-52r3-enabled-1001 `
  d5-phase2-format-causal-52r2-control-1001 `
  --output SimLogs/d5-phase2-format-causal-52r3-enabled-1001-causal-trace-v1.json
```

The analyzer reports three ordered stages for every cohort:

1. deterministic prior;
2. recorded label-format EMA/confidence blend;
3. recorded noise-applied final margin and selected format.

For rows with complete inputs, it exactly reconciles `priorAlbumNet - priorSingleNet` as:

```text
affinity/demand + accepted opportunity + format tilts + hit inventory + production costs
```

The `priorDecomposition.meanResidual` is at float-rounding scale for the reconciled cohorts.

## Surf Rock: telemetry routing defect

The earlier apparent enabled Surf Rock split was not a Surf Rock market regime. Album decisions were evaluated using the selected V2 project genre, but album telemetry was emitted after the artist identity had been restored. Thus a Surf artist whose selected project was Traditional Pop, Easy Listening, or another genre was written as a Surf Rock decision.

The repair makes album telemetry use `album.primaryGenre` and `album.secondaryGenre`; calibration telemetry now also uses the album genre. In the corrected enabled trace the actual Surf Rock cohort is four pre-live decisions, all deterministic Singles (`0 / 4` Album priors and final Albums). The four rows have zero accepted Album opportunity, which is appropriate for the 1960 pre-emergent catalog state. There is no enabled Surf Rock Album preference to tune or retain.

## Gospel: retained V2 economic input

Corrected 52-week results:

| Cohort | N | Prior Album share | Mean Album - Single prior |
|---|---:|---:|---:|
| Enabled Gospel | 328 | 71.0% | +$838 |
| Control Gospel | 268 | 36.9% | -$4,415 |

The accepted Album opportunity is identical (`0.090249`) and both format tilts are neutral (`1.0`) in the two runs. The movement is instead in the intended V2 Single relative-market input: mean `genreSinglesMarketFactor` is `0.795` enabled versus `1.300` control. The enabled Gospel specialist routing therefore lowers the Single prior while leaving the accepted Album opportunity seam intact. This is an economic-input difference to document and retain; it is not a taxonomy/telemetry defect and no noise or final-fork change is indicated.

## Native Soul memory: no valid feedback finding

The apparent native-Soul memory loop was caused by the same project-versus-artist telemetry error. After routing repair, the true native Soul cohort has four decisions, no positive memory confidence, no prior-to-memory flips, and no changed choice in the recorded-noise no-memory replay. The previous evidence therefore does not support changing memory scope or confidence.

The analyzer retains the offline no-memory replay and the label/week trace so a later cohort with actual native-Soul volume can be checked without changing the simulator.

## Targeted validation and stop decision

The corrected telemetry-enabled run is `d5-phase2-format-causal-52r3-enabled-1001`; the matching unchanged control is `d5-phase2-format-causal-52r2-control-1001`. Both completed 52 weeks. The only terminal diagnostic was the pre-existing non-fatal `MissingSingletonsTemp.cs` autoload warning.

The telemetry repair reconciles the affected Surf Rock and Native Soul cohorts. It does not reopen the existing broad economic failure: enabled/control ratios are `1.135` units, `1.148` gross, `1.158` label net, and `1.158` market net. Successful releases remain `0.992x` control, while Album units remain `1.444x` control. Therefore this trace does **not** authorize resuming the 520-week ladder.
