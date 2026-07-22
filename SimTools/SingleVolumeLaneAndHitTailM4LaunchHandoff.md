# Single-lane repair: 469-week launch handoff

Date: 2026-07-21. The Album regression exposed by the first M4 attempt is
structurally repaired and bounded validation is complete. The corrected annual
hit-tail adjudication still fails, so M4 is held. No replacement 469-week run has
been launched. Preserve the failed
`d6-single-lane-hit-tail-through-1968-1001` artifacts.

## What failed and what changed

The offline analyzer did not alter the simulation. The first candidate's staged
Single discovery path treated below-neutral chart, momentum, and radio signals as
having no downside. Raw Single demand roughly doubled, shared market capacity
rationed heavily, Albums were crowded out, and the format fork received weak
Album-with-promo feedback.

The corrected enabled path now:

- combines chart, momentum, and radio once as bounded awareness odds, with `1.0`
  neutral and weak signals able to suppress awareness;
- gives correlated strong signals diminishing returns and keeps them out of
  intrinsic conversion;
- records provisional and final Album-with-promo total-project outcomes;
- pools that project memory market-wide, then blends label-local refinement;
- permits negative project evidence to veto another Album-with-promo project,
  while positive promo evidence cannot create extra Album eligibility.

The analyzer now ignores partial calendar years and applies concentration gates by
completed release-cohort year and lane. This is required because week 469 opens
1969 without completing it, and the acceptance rule is lane-year rather than a
whole-run aggregate. No acceptance threshold changed.

## Current validation

- Build passes with only the inherited unused-event warning.
- `git diff --check` passes.
- D5 and D6 fixed probes pass, including neutral, weak, monotonic, bounded, and
  diminishing-return Single discovery cases.
- Disabled replay `d6-single-lane-odds-discovery-disabled-52-1001` is byte-identical
  to `d6-market-clearing-disabled-52-1001` across all 45 CSV streams.
- Enabled checkpoint `d6-single-lane-odds-discovery-through-1961-1001` completed 105
  weeks normally with header-only catastrophic output.

The enabled checkpoint versus the retained control:

| Year | Single units | Album units | Total units | Album projects |
| --- | ---: | ---: | ---: | ---: |
| 1960 | 0.8903 | 1.1244 | 0.8933 | 1.0508 |
| 1961 | 0.9123 | 1.2130 | 0.9212 | 0.9387 |

This removes the prior 1961 failure: Album projects fell from `1780` in the failed
candidate to `1180`, versus control `1257`, while Album units recovered from
`0.7747x` control to `1.2130x`. Lane joins, raw-demand reconstruction, memory keys,
finance posting keys, and market-clearing reconciliation are exact. The focused
105-week analyzer report is
`SimLogs/d6-single-lane-odds-discovery-through-1961-1001-analysis.json`; its only
remaining failures are the separately defined orphan/promo top-10% hit-tail rules:

| Release cohort | Lane | Top 10% share | Top 1% share | Result |
| --- | --- | ---: | ---: | --- |
| 1960 | OrphanSingle | 51.72% | 11.74% | top-10 fail |
| 1960 | PromoSingle | 48.51% | 14.58% | top-10 fail |
| 1961 | OrphanSingle | 52.32% | 11.76% | top-10 fail |
| 1961 | PromoSingle | 49.17% | 11.82% | top-10 fail |

All top-1% checks pass the 35% ceiling. All four eligible lane-years exceed the
40% top-10 ceiling, so this is a real acceptance failure rather than the former
aggregate or partial-year analyzer error. It must not be confused with the repaired
Album regression.

Exact launch-source hashes:

```text
E7E05CC655E83AB8EC4A979D69E64C3335566D3A6395752BF0C16BAF7F7A4E12  Systems/CompetitorManager.cs
999B2B2B3741B706FD5D4D6829666A94FE081A8735AF5E49E7C5C3861F1FA233  Systems/ChartSimulator.cs
C60ED2A35B0290B1E714EC5BC3B202EECCBB837E681326AFC6CF2FE729D423DA  SimTools/GenreMarketV2ProbeSuite.cs
F2986BB5717E95AA5EE578E85A884E7382B829B09A790BAE329E766F85B8B20B  SimTools/ChartAuditRunner.cs
C9A6317F8460B75E1A598794452F9FE50080A9C5C27EE6F4B3C68A8A3339C39E  SimTools/analyze-single-lane-hit-tail.mjs
```

## Held M4 command

M3 remains an in-process checkpoint inside M4. Do not run a separate 313-week
replay. Do not launch M4 under the current 40% top-10 acceptance rule unless the
owner explicitly accepts the known 1960-1961 failure or a subsequent bounded
hit-tail repair passes this checkpoint. The eventual command remains:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=469 --run=d6-single-lane-odds-discovery-through-1968-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d6-transition-envelope-decade-control-1001
```

If the in-process 1965 gate fails, preserve the partial artifacts and stop. If it
passes, allow that same process to finish week 469, then run:

```powershell
& 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' SimTools/analyze-single-lane-hit-tail.mjs SimLogs d6-single-lane-odds-discovery-through-1968-1001 --control-prefix=d6-transition-envelope-decade-control-1001 --json=SimLogs/d6-single-lane-odds-discovery-through-1968-1001-analysis.json
```

Run one exact 469-week repeat only if the candidate and analyzer both pass. Do not
launch M5, more seeds, or a parameter sweep.
