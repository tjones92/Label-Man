# Supply allocation to realized economics bridge

## Scope and reproduction

This checkpoint is offline analysis of the corrected 52-week seed-1001 telemetry. It does not alter the simulator, demand, finance, noise, or the format fork.

```powershell
& $node SimTools/analyze-supply-economic-bridge.mjs `
  d5-phase2-format-causal-52r3-enabled-1001 `
  d5-phase2-format-causal-52r2-control-1001 `
  --output SimLogs/d5-phase2-format-causal-52r3-enabled-1001-supply-economic-bridge-v1.json
```

The analyzer joins supply selections, format decisions/release strategy, retired-or-live realized units, release outcome/live net telemetry, and annual format market net. All 4,277 decisions join to release strategy and realized-unit telemetry; 97.6% have record-level observed label-net telemetry. Per-transition market net is an explicit format-level allocation of annual market net by observed units, while `observedLabelNet` remains the record-level realized/lower-bound measure.

Count/allocation and yield effects reconcile exactly:

```text
count/allocation = (enabled projects - control projects) * control units/project
realized yield   = enabled projects * (enabled units/project - control units/project)
```

The standardized comparison uses common format-by-career-band-by-quality-quartile-by-reach-bucket strata.

## Aggregate result

| Format | Unit delta | Count/allocation | Realized yield |
|---|---:|---:|---:|
| Singles | +7.320M | +8.734M | −1.415M |
| Albums | +0.827M | +0.349M | +0.477M |
| Total | +8.147M | +9.084M | −0.937M |

The reconciliation residual is below `0.000004` units. Singles are the allocation problem; Albums are only about 10% of absolute excess units.

## Allocation modes and transitions

Enabled Singles comprise 2,437 retained projects (95.933M units), 428 weighted transitions (14.136M), 26 annual-floor projects (1.861M), and 78 pre-live/unmatched projects (2.744M). The matched supply-selection destination equals the corrected project decision genre in every matched row.

The router sends Soul identities into rather than retaining Soul projects. The largest resulting Single transitions are:

| Transition | Projects | Units | Allocated market net |
|---|---:|---:|---:|
| Soul -> Doo-Wop | 66 | 3.113M | $1.491M |
| Soul -> Teen Pop | 32 | 1.172M | $0.561M |
| Soul -> Traditional Pop | 12 | 1.043M | $0.499M |
| Soul -> Jazz | 5 | 0.909M | $0.435M |
| Soul -> R&B | 59 | 0.750M | $0.359M |
| Soul -> Easy Listening | 10 | 0.551M | $0.264M |

This is a real allocation pattern, not the prior project-versus-artist telemetry defect. It follows the authored V2 lifecycle rule: `Soul` is pre-emergent in 1960, so existing Soul identities cannot retain a Soul project. Whether that historical allocation is desired needs an explicit product decision; this trace alone does not justify changing lifecycle or routing inputs.

## Main supported genre-format cohorts

| Cohort | Unit delta | Count/allocation | Yield |
|---|---:|---:|---:|
| Teen Pop Singles | +12.291M | +5.678M | +6.613M |
| Traditional Pop Singles | +8.096M | +5.540M | +2.556M |
| Soul Singles | −11.445M | −11.405M | −0.041M |
| Country Singles | −2.923M | +1.150M | −4.073M |
| Doo-Wop Singles | −2.299M | +7.028M | −9.327M |

There are 21 supported genre-format cohorts, 14 enabled-only cohorts, and 3 control-only cohorts. The largest enabled-only Single is Easy Listening (`+1.498M`); the largest control-only Single is Blues Rock (`−2.441M`). Canonical migration is applied before support and effect accounting, so Motown and Girl Group are not mistaken for disappearance artifacts.

Standardization confirms that excess yield is not only composition: Teen Pop Singles retain a +11.029K standardized units/project difference and Traditional Pop Singles retain +28.068K. Country and Doo-Wop retain negative standardized differences. This directs the next offline diagnosis to the regional demand conversion for Teen Pop and Traditional Pop, after confirming whether their increased prospective allocation is historically intended.

## Decision gate

- No fork, finance, noise, or demand-keyframe change is authorized from this trace.
- The Soul transition behavior is explicitly tied to authored pre-emergence lifecycle policy, not an accidental routing mismatch; retain it pending a historical allocation decision.
- Teen Pop and Traditional Pop have both count-allocation and standardized yield excess. Before altering supply balance, trace their regional demand conversion and verify whether their allocation is intended.
- The 520-week ladder remains stopped. No new simulation was run for this checkpoint.
