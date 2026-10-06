# Polar song selection and consumer wiring

Implemented October 2, 2026 in the existing working tree. The user's request authorizes this behavior slice; the supplied directive and prior review are design references, not separate instructions to execute later phases or pause for sign-offs. Existing data/shadow work and unrelated changes remain in place.

## Feature boundary

`PolarSongBehavior.UsePolarFitSelection` defaults to `false`. Opt in before population generation with `--use-polar-fit-selection`. The composition save section persists the switch, lagged taste snapshots, and master metadata with realized fit. Missing fields in older saves default to disabled/empty. Loading a save restores its saved switch. There is no UI switch in this slice.

With the switch off, legacy material choices and their numerical consumers remain intact. Compatibility-only craft selection and interpretation helpers are marked obsolete. With the switch on:

- Bounded AI pool sampling uses act-specific Capability/Identity/Moment with proposed weights 0.35/0.45/0.20. Genre fit breaks exact ties. Recent-hit fatigue remains bounded existing pressure. Source-category choice retains the era source prior but replaces its scalar craft/need preference with realized fit. Forced album source slots retain their source constraints and choose a suitable song within them.
- Player commissions choose suitable material for the act. Live sets retain the existing original-count and set-length draws, deduplicate eligible covers, and choose them by suitability. Exhausted pools leave unfilled slots rather than fabricating a standard. Player catalogues group known genres and sort by title/ID; they expose no aggregate fit ordering or new truth-derived fit display. Existing material read widgets remain for the later perception/UI slice.
- Fixed-song player choices carry a heard master ID through repertoire, rehearsal, material choice, session preparation and save DTOs. A `demo:<songId>` subject explicitly preserves a demo choice when other masters exist. Automatic AI references use known completed recording memories in stable year/ID order, without scoring chart outcomes. Missing historical master metadata falls back to marked demo inference. Invalid explicit references also fall back to the demo.
- The resolver's proposal is committed when the recording is applied, with its own taxonomy, parent master and separate fit components. Master reuse retains that metadata. Composition identity, writer credits and the existing rights snapshot continue through the original application path. New cover arrangement originality uses realized Stretch. Refusal and definitive-version/fatigue outcome updates are still deferred.
- Album-only cuts commit the selected proposal and execution effect. A promoted single chooses among actual cuts by suitability and reuses its source composition/master/traits. It does not select a second song or apply execution loss twice. An album market record itself has no single-song polar fit.

## Consumer contract and provisional tuning

All new weights and effect strengths are in `Data/PolarSongTable.json`, version `polar-behavior-v1`. They are proposals, not accepted historical calibration.

| Component | Consumer | Proposal |
|---|---|---|
| Capability | Recording hook/production; album-cut execution quality | Execution factor `1 - (1 - Capability) * 0.35`; recording hook is bounded by raw composition hook |
| Identity | Artistic merit and completed-run critical acclaim | Critical craft factor `1 - (1 - Identity) * 0.20` |
| Moment | Single regional purchase conversion, including staged live demand | `1 + (Moment - 0.5) * 0.10`, neutral without earlier market evidence |

These components are not recombined into a player-facing score. The AI-only aggregate is neither persisted nor added to CSV/display fields. Composition craft does not enter demand axes. Existing generated originality remains an input to critical craft; Stretch is stored as arrangement movement, not automatically declared artistic success.

Moment uses a saved per-genre centroid from completed chart weeks, with the existing 0.20 drift proposal. Material decisions target the next chart week, allowing the latest completed week's evidence while excluding future candidate outcomes. Recording fit is evaluated at preparation/session time and stored on the master; the conversion consumer reads that stored Moment. This is an additional small shape-relevance effect alongside existing genre acceptance, explicitly provisional. It does not change the genre acceptance tables or infer new genre demand from composition craft.

AI sessions use the label's expected support through the existing minimal adapter. Player printed masters supply label producer craft and the kept take's production as session-support proxies. The underlying model still lacks authored vocal nuance/diction and detailed historical reference performances.

## Verification

`dotnet build "Label Man.csproj" --no-restore` passes. It reports three intentional obsolete compatibility-helper warnings and the existing unused `ChartManager.OnGenreMomentumChanged` warning. `git diff --check` passes. Existing headless `MissingSingletonsTemp` autoload diagnostics appear in both baseline and candidate logs; the audits complete successfully.

Development seed 1001 probes:

- `SaveLoadRoundTripRunner --weeks=0 --seed=1001 --polar-song-data-check`: ownership, legacy migration, saves and tags pass.
- `--polar-song-fit-check`: F1–F4, purity, deterministic proposals, lagged evidence and act adapter checks pass.
- `--polar-song-behavior-check`: suitability beats a large global craft advantage; actual player live-set length/deduplication; disabled fixed-song behavior; explicit reference/demo preservation; committed lineage; original rights/credits; execution, critical and market component separation; disabled consumers for saved polar masters; switch/taste/explicit-zero fit persistence and player heard-version persistence all pass.
- `SaveLoadRoundTripRunner --weeks=2 --seed=1001 --use-polar-fit-selection`: full-world capture/apply/re-capture and real gzip I/O pass (`105830814` JSON bytes, `7760675` compressed bytes). This proves serialized state preservation, not full unsaved RNG continuation equivalence.

Final binary audit pairs use 52 weeks, development seeds 1001/1002, ordinary scene defaults and otherwise matching flags. Enabled candidates also use `--polar-song-shadow` for bounded component diagnostics. Controls are the preceding shadow slice's `polar-shadow-control-*` captures. No holdout seeds were used.

| Seed | Disabled legacy CSV streams | Unique single projects off → on | Change | Album projects off → on |
|---|---|---|---|---|
| 1001 | 79 byte-identical | 4983 → 4846 | −2.75% | 1872 → 1846 |
| 1002 | 79 byte-identical | 5044 → 4880 | −3.25% | 1798 → 1745 |

Counts are unique project IDs observed by `artist-project-identity.csv`, including the common initial/prewarm cohort. `song-material.csv` has one material row per observed single project. Both enabled runs have zero invalid/ranged fit rows and valid logging budgets. The shadow CSV remains a bounded diagnostic sample, not an authoritative census of committed session fit or player live sets.

The one-year material-source distribution is mixed. Standards/traditional share for Soul acts falls from 15.63% to 12.16% (1001) and 18.79% to 11.54% (1002). RockAndRoll rises from 29.46% to 32.52% and 31.75% to 32.40%. These are recording assignments, not live-set slots, and do not demonstrate the historical live-set acceptance goal. Folk's share also rises; suitability for folk is a separate question from inappropriate standards for rock acts. Full per-genre numerators/denominators and source→act-genre counts are in the analyzer output.

`node SimTools/analyze-polar-selection.mjs` reproduces the neutrality check, completeness checks, fit/budget validation and source/project comparisons. Logs, CSVs and `polar-selection-summary.json` are under ignored `SimLogs/`.

## Remaining validation boundary

The switch remains default-off. Release-volume changes and the increased RockAndRoll standard/traditional recording share require calibration review; no tolerance or historical acceptance is claimed. The 1960–63 population live-set baseline, decade-scale comparisons, unbiased assignment analysis, deterministic full resume equivalence and holdout checks have not been run in this slice. The direct player-path fixture does not substitute for that population baseline.

Historical taxonomy is still inferred when no known master exists. Automatic reference availability is conservative (completed chart-run memory, year granularity), rather than a full act-specific hearing registry. Persistent perceived axis observations, the comparison UI, refusal consequences and outcome-based version pressure remain later work. The prior missing rights-controller settlement fallback is unchanged; this slice preserves existing composition-based snapshots rather than redesigning writer payments.

`PolarSongSelectionHashes.json` records current SHA-256 values for every source/config/analyzer changed in this slice, including files that already contained the preceding uncommitted work. No commit, push or default-on change was made.
