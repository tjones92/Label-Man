# Does LP assembly explain the combined profile's extra closures?

2026-11-03. Controlled follow-up to `CombinedLabelTurnoverInvestigation.md`.

The earlier broad comparison found more smaller-label turnover alongside better
professional-song use and repeat-career breadth. The pre-exit investigation
found that 147 opening identities closing only in the combined worlds lose
substantial LP business: 159 → 4 untransferred album releases and 1.60M → .12M
LP units in matched pre-exit windows. Another 80 identities survive only in the
combined worlds and show the reverse pattern. This suggests investigating LP
availability and economics before introducing a general survival subsidy.

## Matched policy contrast

Control: `writing-combined-integration-market-{1001,2002}`.
Treatment: `writing-combined-no-lp-assembly-market-{1001,2002}`.
All worlds run for 104 weeks on assembly
`511E9709FE20879CE4636C7FFE39F9C62E9CE825DC8EB01A110041711D221AC5`.

The treatment omits **only `--lp-assembly`**. The separate
`--album-master-costs` stock-aware costing correction remains enabled, as do
album prior recalibration, all-in economics, release practice, writing supply
and access, media, retail, campaigns, financial risk, forecasts, follow-up
scheduling and the remaining combined settings. Native arguments captured before
startup verify this exact one-flag difference for both seeds.

This isolates the complete assembly policy, including actual controlled/shelved
master selection, opening-catalogue seeding, ordinary/hit-led/greatest-hits
proposals, package spacing and production/appeal implications. It is not an
isolated recording-cost treatment or a comparison against the old flag-free
economy. Disabling assembly restores its legacy album construction path while
retaining the separate costing correction. Changed proposals and trajectories
can change subsequent RNG consumption and catalogue histories.

Physical warehouse inventory remains excluded for the previously documented
channel-accounting blocker. No engine code, fees, writer abilities, cash reserves,
survival thresholds or gameplay defaults change in this experiment.

## Results

Both omission runs completed 104 weeks. Closures rose in both seeds even as
more album projects reached release.

| Seed | Closures, on → omitted | Album releases | Single launches |
|---|---:|---:|---:|
| 1001 | 126 → 135 | 4,033 → 4,388 | 6,738 → 6,519 |
| 2002 | 106 → 107 | 4,076 → 4,400 | 6,790 → 6,513 |
| Pooled | **232 → 242** | **8,109 → 8,788** | **13,528 → 13,032** |

Bankruptcies rose 185 → 189 and dormancies 47 → 53. Independents accounted
for 11 more closures and Boutiques for six more. Small-label closures fell
by six and MidTier closures fell by one, measured at the tier at exit.

Of 1,192 matched opening label identities, 84 closed only with assembly omitted,
78 closed only with assembly enabled, 142 closed in both configurations, and 888
survived in both. Among the 84 closing only with assembly omitted, 67 had lower
unit sales and 75 had lower record receipts in their matched pre-exit windows.
The single switch changed which firms closed, while the pooled opening cohort
recorded six additional closures.

Compilations rose from 79 of 8,109 released albums to 5,139 of 8,788.
Paid production across prepared slate album masters moved from $32.80M to
$32.41M. The additional LP output therefore came with a large shift in album
format and a small reduction in total slate album production spending.

Professional songs held near 12.5% of mature issued singles (12.43% → 12.57%).
The share of the complete hit cohort with one to three further hits moved from
120/341 (35.2%) to 115/342 (33.6%).

These two seeds provide little support for LP assembly as the source of the
broad profile’s 70 additional closures. More albums reached release with assembly
omitted, but aggregate survival declined slightly. The comparison leaves the
separate cost and forecast settings in place. A follow-up on LP profitability
would need to isolate those settings directly.

## Reproduction

```powershell
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Weeks 104 -Tag NEW-FRESH-TAG -WithoutLpAssembly
python SimTools/analyze_lp_assembly_turnover.py
python SimTools/analyze_combined_label_turnover.py --control-tag writing-combined-integration --treatment-tag writing-combined-no-lp-assembly --output SimLogs/writing-calibration/writing-lp-assembly-identity-turnover.json
python -m unittest discover -s SimTools -p test_lp_assembly_turnover.py
```

The analyzer's treatment tag must be updated if using a different fresh tag.
The wrapper refuses existing names and requires a distinct tag for the ablation.
The broad configuration file remains frozen. Generated output belongs under
`SimLogs/writing-calibration/`. Six new fixtures verify one-flag isolation,
retention of the costing correction, rejection of other policy changes, common
binaries, duplicate parameter keys and the distinct scopes of prepared costs
versus released assembly uses.
