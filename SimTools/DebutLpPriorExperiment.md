# Does correcting the debut-LP forecast reduce smaller-label closures?

2026-11-03. Matched follow-up to `LpProfitGateFunnelInvestigation.md`, which
found that the all-in profit gate refuses about 45% of debut LPs although
finalized debut LPs realize about 2.9× their forecast contribution.

## Treatment

New opt-in flag `--album-prior-debut-scale=<x>` (requires
`--album-prior-recalibration`). It multiplies the NewSigning career affinity
of the album prior, and nothing else. Declining, Unsigned and every other
career keep the fitted coefficients. The scale is capped at Rising's affinity
(2.0613×), so the career order cannot invert. The run uses that cap, which is
deliberately below the measured 2.9× because the measured ratio comes only from
LPs that passed the gate.

Control `writing-combined-debut-off-market-{1001,2002}` and treatment
`writing-combined-debut-lp-prior-market-{1001,2002}` share assembly
`284698FF268370DA9049A9C1C1FEB0FE409DF805C0ED641C12112A0E7E002560` and every
explicit flag except the added scale. The flag-off control reproduces the
original `writing-combined-integration` worlds byte for byte (label finance,
slate, profit gates, records and company-monthly) in both seeds. The new build is
therefore inert with the flag off, and the earlier investigations still apply.

The prior feeds both the format choice in `DecideRelease` and the preparation
gate, so the treatment changes which projects are proposed as well as which pass.

## Results (two seeds pooled)

| Measure | Control | Treatment |
|---|---:|---:|
| Smaller-label debut LP gate pass | 10,509/19,230 (55%) | **12,271/13,951 (88%)** |
| Released album projects | 8,109 | **9,415** |
| Smaller-label LP slots issued | 5,690 | 6,502 |
| Smaller-label LP slots cancelled | 2,459 | 1,765 |
| Finalized smaller debut LPs (released ≤ wk 52) | 663, all profitable | 1,071, 1,064 profitable |
| Their median realized net | ~$9.9k | ~$6.9k |
| Their total realized net | $7.81M | $9.76M |
| Paid slate LP production | $32.80M | $38.97M |
| Single launches | 13,528 | 13,302 |
| **Closures** | **232** | **229** |
| Bankruptcy / dormancy | 185 / 47 | 177 / 52 |
| Smaller labels, 1–10k units wk 1–13: closed | 138/538 | 132/543 |
| Surviving smaller labels, endpoint cash | $67.4M (926) | $65.5M (926) |
| Mature professional singles | 1,350 | **1,207** |
| Covers per professional single | 4.10 | **4.48** |
| New professional demos only on released LPs | 2,119/3,535 | 2,235/3,537 |

By seed, closures went 126 → 129 (1001) and 106 → 100 (2002), which is within
the run-to-run noise documented for these comparisons.

## Interpretation

1. **The forecast was wrong, and correcting it releases profitable LPs.** The
   newly admitted debut LPs remain profitable (99.3% of finalized ones), though
   at a lower median as the marginal projects come in. Debut gate pass rises
   from 55% to 88%, and 1,306 more album projects reach release.
2. **It does not address smaller-label turnover.** Closures are unchanged
   (232 → 229), including among weak early sellers, and surviving smaller labels
   end with slightly less cash. The extra LPs cost $6.2M more in production,
   and much of their revenue is still uncollected at week 104. This supports
   the funnel note: closures are driven by missed early hits feeding the 2×-burn
   commitment reserve, not by the profit gate.
3. **It works against the professional-singles goal.** Labels choose LPs for
   new signings more often, and professional material goes with them. Mature
   professional singles fall by 143 (−10.6%), and the cover ratio rises from
   4.10 to 4.48. The share of professional demos that appear only on LPs rises
   from 60% to 63%.

The flag is therefore not recommended for the combined baseline as a turnover
or song-balance remedy. If the LP forecast is corrected later on accuracy
grounds, the prepared-single path should also consider an eligible existing LP
master (the handoff's separate allocation experiment). Without that, more LPs
move professional songs further away from singles.

Not changed here: the assembly path's portfolio-credit subtraction (immaterial,
see the funnel note) and the 2×-burn reserve.

## Reproduction

```powershell
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-combined-debut-off -ExpectedAssemblySha256 284698FF268370DA9049A9C1C1FEB0FE409DF805C0ED641C12112A0E7E002560
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-combined-debut-lp-prior -DebutLpPriorScale 2.0613 -ExpectedAssemblySha256 284698FF268370DA9049A9C1C1FEB0FE409DF805C0ED641C12112A0E7E002560
py SimTools/analyze_debut_lp_prior.py
```

Repeat for seed 2002, running one task per seed in parallel.
`--album-prior-recalibration-check` (SaveLoadRoundTripRunner) covers the
NewSigning-only reach, the Rising cap and the unit-scale identity. Output:
`SimLogs/writing-calibration/writing-debut-lp-prior-comparison.json`.
