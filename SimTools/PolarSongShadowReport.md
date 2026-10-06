# Polar song shadow slice

Implemented on `codex/polar-song-data-model`, following the data ownership slice. The user authorized implementation; the attached directive and review supply design evidence. Their staged sign-off language is not treated as a separate user instruction.

## Delivered behavior

`Data/PolarSongTable.json` contains all 34 archetypes, ten axes, plasticity, era/family eligibility, style priors, modifiers and numerical tuning under version `polar-shadow-v1`. Six base rows reproduce the supplied Spec A examples. Remaining rows, priors, modifiers, eligibility and tuning are explicitly provisional proposals with provenance.

Pure song derivation distinguishes performance demands from composition craft. Instrumentals remove vocal demands; word density, form, production and typed tags affect relevant axes. Unknown legacy taxonomy uses a marked genre prior. Committed first-master plasticity freezes once when shadow derivation is enabled; later masters cannot reset it. Cached profiles persist with a fingerprint of taxonomy, relevant composition inputs and the full table contents, so changed inputs invalidate the cache.

The act adapter uses active performers, lead/backing support, technical skill, versatility, reliability, ensemble cohesion, session support and genre history. Vocal nuance and lyric delivery are estimates because the actor model lacks direct control/diction fields. Session support affects StudioCraft, not vocal ability. Age and ambition do not become sophistication or maturity. Inferred capability and missing session evidence are marked.

Capability, Identity and Moment remain separate. Capability penalizes unmet demand; WorstAxis identifies the largest deficit. Identity compares the realized arrangement with the act. Stretch is the weighted mean movement from the reference; ReferenceIdentityDistance separately records the initial mismatch. Moment uses only completed earlier-week observations, with neutral 0.5 when evidence is unavailable.

The cover resolver accepts an explicit reference or demo, rejects future/mismatched references, and proposes a deterministic arrangement without mutating composition, reference, rights, IDs or RNG. It searches era/family-eligible archetypes, weighs shape and execution deficits, preserves form/content, and bounds movement by plasticity times interpretive reach. Zero reach or plasticity returns the reference arrangement unchanged. Near ties use the supplied planned master ID. An empty eligible pool produces a marked fallback. Proposal creation does not commit a recording.

Normal gameplay keeps the existing selection and scalar consumers. `ChartAuditRunner --polar-song-shadow` enables read-only candidate observation, profile warming and two extra CSVs. Refusal and reinterpretation helpers are diagnostics only. No selection weights, player perceptions, UI, royalties or market outcomes consume these values yet.

## Formula decisions and provisional tuning

The dominant-axis headroom subtracts demand once, resolving the supplied C6 prose/helper ambiguity. The Beatles fixture therefore produces Capability 1.0 under the written clamp, rather than the example's 0.94. This discrepancy is preserved rather than hidden in a special case.

The reinterpretation threshold is provisionally 0.05 for normalized weighted-mean Stretch, rather than the directive example's 0.15. Respect's realized Stretch is about 0.0541; its outcome helper is positive with the supplied high familiarity. The resolver includes a general execution-shortfall cost so an identity-near arrangement with unsupported ensemble demands does not displace a playable one. These are reviewable tuning proposals, not accepted population calibration.

## Validation

`dotnet build "Label Man.csproj" --no-restore` succeeds with the existing unused `ChartManager.OnGenreMomentumChanged` warning.

`SaveLoadRoundTripRunner --weeks=0 --seed=1001 --polar-song-fit-check` exercises four fixtures plus purity, determinism, zero reach, craft neutrality, instrumentals, frozen plasticity, future-reference rejection, lagged market evidence, inactive performers and session support:

| Fixture | Capability | Identity | Stretch | Result |
|---|---:|---:|---:|---|
| Beatles / bright pop | 1.0000 | 0.9733 | 0.2253 | No deficit; reference mismatch improves after pull |
| Respect | 0.9951 | 0.9224 | 0.0541 | Horn-driven soul, changed lyric/vocal mode, positive diagnostic reinterpretation |
| Garage act / lush standard | 0.2331 | 0.2938 | 0.0550 | VocalNuance deficit 0.75; ensemble and identity mismatch remain |
| Tenderness | 0.9980 | 0.9537 | 0.3900 | Deep Soul Pleader, dramatic build, yearning, changed genre; same composition/reference lineage |

The data probe also checks cache reuse, invalidation and persistence. A two-week full-world save/load run with `--polar-song-shadow` passes repeat serialization and gzip I/O (`106492786` JSON bytes; `7783111` gzip bytes). The save check exercises the warmed cache and frozen plasticity, not player perception or exact unsaved RNG continuation.

Two 52-week development runs (seeds 1001/1002), through December 30, 1960, compare shadow mode with controls captured before this slice. `node SimTools/analyze-polar-shadow.mjs` requires completed runs, byte equality for all 79 existing CSV streams, finite/ranged shadow values, strictly earlier market snapshots and the per-week row bound. Both final runs pass.

| Seed | Existing streams | Shadow rows | Omitted observations | Invalid rows/budgets | Market evidence share |
|---|---|---:|---:|---|---:|
| 1001 | 79 byte-identical | 106496 | 206554 | 0 / 0 | 4.57% |
| 1002 | 79 byte-identical | 106496 | 200200 | 0 / 0 | 2.73% |

All observed taxonomy is inferred in both runs. Sample means for Capability/Identity/Moment/Stretch are respectively `0.783584/0.961521/0.517302/0.103866` (1001) and `0.794877/0.957918/0.509874/0.108102` (1002). These describe the bounded sample only. Full resolver phases total 20702 rows for 1001 and 20802 for 1002; remaining rows are pool observations. Raw logs and the analyzer summary preserve the measurement details.

## Limits and next boundary

Shadow sampling takes at most 2048 observations per week in existing traversal order. It is a bounded prefix, not an unbiased population sample. Omitted rows are counted in `*-polar-fit-budget.csv`; pool rows use cheap fit while source finalists and selected rows use the full resolver. Current generated taxonomy is inferred, so these runs establish neutrality and instrumentation, not historical calibration. Market centroids are a provisional chart-derived diagnostic, not an independent taste system.

This slice does not establish the requested 1960–63 live-set baseline or a decade/holdout result. Promotion still uses legacy material reselection, and the previously identified missing rights-controller fallback remains unchanged. Historical reference taxonomy, reliable heard-version choice, behavior consumers, live-set suitability and persistent perceived player reads remain next-stage work.

The data model report and its hash manifest describe the preceding slice. This slice extends several of those files; `PolarSongShadowHashes.json` records the current combined source/config fingerprints. Logs, CSVs and analysis JSON remain in ignored `SimLogs`.
