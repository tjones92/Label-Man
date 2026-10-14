# Phase 5: first live feedback channel

Completed October 9, 2026 in the existing `codex/local-scene-phase-5` checkout. The user authorized implementing live feedback after accepting matched two-year pressure revalidation. The directive and historical atlas are design references, not independent task instructions.

## Implemented behavior

`--enable-scene-attention-feedback` opts into a real recruitment reader. Committed runtime signings produce the existing conserved, attributed, genre-specific signing attention. Acts based in an attributed scene receive a bounded increase in their weight for an AI label’s existing accessible scouting slate. The artist’s current genre must match the recorded signing genre; unknown legacy signing genres give no boost. This affects discovery, not guaranteed offers or artist acceptance.

```text
input week = recruitment week - 1
genre heat = 1 - exp(-sum(attributed, decayed signing impulses))
discovery weight = existing geographic access weight * (1 + 0.25 * genre heat)
```

The 25% ceiling is a declared gameplay prior. Existing 0.05 signing budgets, base/origin/HQ attribution, catchment merging, duplicate suppression, eight-week half-life and 104-week horizon remain authoritative. One fully local signing initially adds about 1.2% to discovery weight. The one-week lag prevents same-week signing cascades and label iteration order from amplifying immediate feedback.

Access is established before weighting. Slate size, discovery key namespace, quality perception, genre eligibility, signing commit checks and ordinary/recovery budgets are preserved. The reader creates no births, relocations, prices, skill changes, referrals or geographic access. It consumes no advancing RNG stream.

The once-per-recruitment-week cache is derived from the persisted contract ledger. Configure and full-world Apply clear it, including same-week rollback. No new serialized heat stock or save schema is necessary. The same mode, saved ledger, seed, week and candidates reproduce selection after reload. Terminal scene residents retain ledger history through existing registry rules.

`--disable-scene-attention-feedback` retains the exact disabled path. Default remains off for this independently gated slice. Feedback requires persistence, rooms and geographic recruitment; contradictory off/observer dependency combinations reject before mutating configuration. The broad `--enable-scene-dynamics` remains unsupported because it would imply other unfinished channels.

## Validation

Build passes with zero errors and six existing warnings. Final fixed suite: `SimLogs/scene5-live-feedback-fixed-v3/runs.json` and `analysis.json`; all 11 jobs pass. Both seeds pass 93 new feedback checks, 38 dynamics checks and 56 recruitment checks. The 74 room checks and live-enabled 26-week gzip round-trip pass. The default/explicit/observer short comparisons preserve all 84 economic streams exactly.

The controlled shock changes actual slate membership in 22/64 and 29/64 fixed windows, without extending access or slate size. Fixtures cover dependency rejection, one-week lag, attribution conservation, duplicate suppression, decay, saturation, unknown genre/place neutrality, catchment identity, stable input ordering, RNG/world conservation, full save/reload selection, rollback cache invalidation and neutral initialization.

Four matched 104-week runs cover January 1960 through December 29, 1961. Geographic recruitment is enabled on both sides; only feedback differs. Both disabled replays match the accepted pre-feedback recruitment runs byte-for-byte across 84 economic CSVs per seed. Both live comparisons pass the established economic and vacancy screens. All four participation censuses and population/booking integrity checks have zero violations; there are no unexplained signings or unexpected runtime errors. The established temporary-autoload diagnostic is retained in controls and treatments.

| Live change versus matched feedback-off control | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Singles units | +0.03% | -0.56% |
| Album units | -0.09% | +2.39% |
| Market net | -0.08% | +1.11% |
| Final roster headcount | -0.31% | +1.27% |
| Affordable vacancy-slot burden excluding accepted UK exceptions | +1.35% | -0.32% |
| Changed slate memberships / query calls | 316 / 8,504 | 305 / 7,549 |
| Maximum discovery weight increase | 18.11% | 17.25% |

Query counts are repeated scouting observations, not unique acts or independent events. Weekly telemetry in `scene-feedback-weekly.csv` reports calls, boosted candidate/selection observations, counterfactual membership changes and maximum multiplier. `inputWeek` identifies the prior-week snapshot available at the capture week; counters cover the interval since the previous capture and can span adjacent recruitment weeks. Each actual reader uses its own recruitment week minus one. Audit counters never affect simulation decisions.

Whole-market annual changes remain within the economic screen. Tier reshuffling is visible: MidTier market net is -6.44% on seed 1001 and +5.92% on seed 2002. That is not a universal tier improvement or evidence of statistical significance. Raw UK exceptions and all tier data remain in the report; no thresholds or gameplay constants were tuned to the treatment results.

## Reproduction

```powershell
./SimTools/RunSceneFeedbackChecks.ps1 -RunTag scene5-live-feedback-fixed-v4
python ./SimTools/analyze_scene_feedback_checks.py scene5-live-feedback-fixed-v4
./SimTools/RunSceneFeedbackAudit.ps1 -RunTag scene5-live-feedback-two-year-v2 -Weeks 104
python ./SimTools/analyze_scene_feedback.py scene5-live-feedback-two-year-v2
```

Use bundled Python if the system command is a Store stub. The audit manifests record flags, engine/assembly/source hashes, completion, exit codes and serial execution. `analysis-provenance.json` records the analyzer hash and the post-validation whitespace-only cleanup; the original tested source hashes remain intact. Main implementation: `Systems/SceneAttentionFeedback.cs`, `SceneRecruitmentService.cs`, `LocalScenes.cs`, and the restore invalidation in `WorldSaveData.cs`.

## Remaining Phase 5 scope

This completes the first live discovery channel, not all dynamic ecosystems. Full employment/institution opportunities, funded and consented relocation, sourced dated institution/era transitions, broader influence/diffusion and any price feedback remain separate work. Rumors/news presentation remains its own information slice. Final paired-seed decade acceptance stays deferred; this is the user-requested two-seed early-decade development screen. No other channel or new default is implied by enabling this flag.

The primary band-member checkout and its existing changes were preserved. All audit processes have exited.
