# Phase 5 continuation: two-year pressure revalidation

Completed October 9, 2026 in the existing `codex/local-scene-phase-5` checkout. The user explicitly replaced the outstanding four-year pressure request with two-year runs and accepted a serious-regression screen as sufficient to continue. The attached directive and atlas remain design references, not independent instructions or authorization.

Four matched 104-week runs completed January 1, 1960 through December 29, 1961: recruitment off/on for seeds 1001 and 2002. All use the same current assembly, HQ access repair, persistent rooms and established population/band settings. Only the recruitment flag changes within each pair. The existing 5% economic/headcount and 5-point label-failure screens are retained; the vacancy floor scales to 104 weeks, with the 25% relative screen unchanged.

| Change versus matched control | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Singles units | -2.85% | -1.17% |
| Album units | +11.38% | +1.51% |
| Market net | +6.34% | -0.36% |
| Final roster headcount | +1.97% | -4.76% |
| Affordable vacancy-slot weeks, excluding accepted UK exceptions | +12.51% | +1.25% |

**Both economic and pressure screens pass.** All four runs have zero population/ownership/pool and booking integrity violations, zero participation census errors and zero unexpected runtime errors. Recruitment signings have zero unexplained routes. The known temporary-autoload diagnostic is present in controls and treatments.

Seed 2002 final roster headcount is -4.76%, near the existing 5% screen. Annual whole-market results stay within the economic screen. Raw major-tier losses retain the accepted EMI/Decca UK scope exceptions; those exceptions are not erased from economic totals. Opening major-cohort finance net excluding these exceptions changes +12.88% / -0.93%. Smaller tiers do not show a general net collapse; seed 2002 Small net is -0.81%, while its Album units are -9.46%. These are review observations, not reasons to retune at this checkpoint.

This satisfies the user-requested two-year pressure revalidation. The older four-year evidence is retained; another four-year run is not an outstanding requirement for this continuation. No gameplay constants or defaults changed. This is a two-seed early-decade screen, not a decade result or a test of live feedback.

## What live Phase 5 feedback consists of

Current Phase 5 observes committed signing attention (bounded, attributed, decaying) and travel-feasible counterfactual move leads. No simulation reader consumes them to change asking prices, bids, recruitment, births, placement, residence or development. `--enable-scene-dynamics` explicitly rejects use because live dynamics are not implemented.

Live feedback means implementing separately gated consumers and actual state transitions:

- Full employment/institution opportunities, beyond the named supporting-room slot sample.
- Funded and consented relocation commits with housing, work, access, member agreement, commitments and cooldowns; moves preserve origin and never teleport people.
- Sourced dated institution and era changes that affect available opportunities.
- Bounded causal influence/diffusion and attention effects on opportunities, discovery/competition and potentially asks, with conservation, decay, saturation, delays and replay checks.

These are remaining implementation and calibration work, not another name for playing the UI or running the pressure audit. The design must not turn scene attention into innate talent or an independent birth factory. Final paired-seed decade acceptance remains deferred under the existing scope.

## Reproduction and artifacts

```powershell
./SimTools/RunSceneFourYearAudit.ps1 -RunTag scene5-two-year-pressure-v2 -Weeks 104
python ./SimTools/analyze_scene_two_year.py scene5-two-year-pressure-v2
```

Use bundled Python if the system command is a Store stub. Full results: `SimLogs/scene5-two-year-pressure-v1/report.md`, `analysis.json`, and `runs.json`. The manifest records engine/assembly/source hashes, flags, exits, completion and durations. Build passed with zero errors. All simulation processes finished. The primary band-member checkout was not modified by this continuation.
