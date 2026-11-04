# Broad combined feature screen

2026-11-03. The user requested two seeds with the past month's experimental
systems enabled together, clarifying that most are pending eventual main-game
integration rather than intended to remain disabled.

## Configuration

Seeds 1001 and 2002, 104 weeks, fresh worlds. This uses the ordinary profile,
finite writing market, combined writing workflow and causal fanbase bundle,
plus the 133 explicit entries in `writing_combined_integration_flags.txt`.
The settings include already-default features explicitly so the intended
integration configuration is reviewable. Flags with numerical arguments are
scenario choices, not newly adopted gameplay defaults or historical estimates.
The original assembly is frozen; no engine code is changed.

This is a broad **many-feature scenario**, not a factorial experiment or an
isolated estimate of writing supply. It includes writing access/reminders,
rock supply, album assembly/release practice/session production, prior/cost
corrections, campaign spending, financial risk/forecast/roster candidates,
hitmaker renewal/signing/cooling, follow-up allocation, media adapters, regional
exposure/retail/radio, local consumption and the keyed sales RNG pilot. Current
scene/default-enabled band, scene, player studio and contact systems remain on.

Recent feature provenance was checked against the October 3–November 3 git
history, the current runtime flag inventory and the uncommitted writing work.
The current branch is `media-atmosphere`. No branch switch or default rollout
was requested or performed.

## Why literal all-on does not form one runnable configuration

The startup attempts preserve their separate logs under
`SimLogs/writing-calibration/`:

| Attempt | Outcome |
|---|---|
| `writing-combined-preflight-market-1001` | Rejected: response auditions require reserved station-pick slots; `--station-picks-extra` conflicts |
| `writing-combined-preflight-v2-market-1001` | Rejected: `--seed-head-templates` and the profile's `--studio-parent-labels` seed the same firms |
| `writing-combined-preflight-v3-market-1001` | Real runtime failure: week-one `PhysicalPressingInventory.BookSales` reconciliation on `gen_1` |
| `writing-combined-preflight-v4-market-1001` | Completed one week, exit zero, no reported engine errors |

The final screen keeps studio-parent labels and reserved picks with response
auditions. It uses the existing pressing ledger. The physical-inventory attempt
fails on an LP with autonomous rack sales; those units reach revenue settlement
while the physical ledger only reconciles its warehouse, regional shelves and
held stock. This is a combined accounting integration blocker, not evidence
that physical inventory is unwanted. The exact first discrepancy still needs
instrumentation; the observed failure and unintegrated channel boundaries must
not be called a repaired ledger. Follower advance stocking also directly adds
shelf copies and needs physical-ledger integration before enabling both safely.
The four physical-inventory flags are excluded from the successful configuration
and remain on the integration-blocker list. No accounting guard was bypassed.

Other exclusions are required to choose one coherent scenario:

- Observation-only, disable, historical replay, permutation and partial-mechanism
  ablations are alternatives to enabled behavior, not additional main-game systems.
- The old hit-follow-up finance arm conflicts with the retained breakout-planning
  policy; preparation-headroom variants replace the active work-budget policy.
- Senior cooling uses its complete form instead of the launch-only ablation;
  studio labels use the full form instead of roster-only; singles retain the
  baseline launch-awareness convention rather than its alternate awareness split.
- Bounded-audience and local-shelf consumer replacements conflict with local
  retail exposure. The screen retains local retail exposure and the compatible
  contact/familiarity/peer/listener/radio-signal probes. Random audition and
  response audition cannot both be selected; response is used.
- The existing LP purchase-envelope/capacity policy is retained instead of the
  competing album-volume retrenchment. Parameter sweeps (office shares, prices,
  demand factors, chart weights, etc.) cannot all have different values at once.
  Local retail uses the adopted .3 weight across its terms, rather than the
  optional split trade-channel-weight variant.
- Gospel songbook v2 is an unimplemented gated research request: its `=on`
  setting explicitly throws. A bare tag does not activate it. It is excluded.

Some enabled tags are **superseded by another enabled owner**. In particular,
`PublisherPitchOrder.Visible` and `RockHitOfficePlacement` defer their legacy
placement rules while the finite writing market is enabled. Their tags do not
stack an extra unrestricted pitching market on top of finite rights/capacity.
This is important when interpreting what “enabled” actually exercises.

The successful one-week smoke does not establish save/resume support for this
combination: several radio/retail/consumer probes explicitly support fresh,
unbroken runs only. The 104-week runs are unbroken and do not use saved resumes.

## Results

Both worlds completed 104 weeks, exited zero and passed the analyzer. Requests
were written before startup; completion manifests were written after success.
Both full requests match the successful smoke apart from run/seed/week arguments.
The analyzer verifies the exact flag set, common assembly, matching seeds,
104-week endpoints/completion markers, clean engine-error logs, writing ledgers,
material provenance and delivery order. Final console, engine and error logs
for these worlds and the three supply worlds contain no `ERROR:`, `Exception`
or `SCRIPT ERROR` matches. The frozen assembly hash was rechecked after all runs.

### Professional material and release volume

Mature intentions are singles proposed in weeks 1–78 and issued within 26 weeks.
Counts and provenance follow the earlier investigation; covers exclude
traditional/public-domain material.

| Seed | Mature issued intentions, control → combined | Professional issues | Covers | Covers per professional |
|---|---:|---:|---:|---:|
| 1001 | 5,823 → 5,396 | 310 → 718 | 3,409 → 2,726 | 11.00 → 3.80 |
| 2002 | 5,787 → 5,465 | 344 → 632 | 3,505 → 2,810 | 10.19 → 4.45 |
| Pooled | 11,610 → 10,861 | 654 → 1,350 | 6,914 → 5,536 | **10.57 → 4.10** |

Professional share rises **5.63% → 12.43%**. The separate complete-conversion
launch denominator agrees: 699 → 1,458 professional launches among
12,387 → 11,780 singles, **5.64% → 12.38%**, with covers/professional
10.64 → 4.20. Professional counts rise in both seeds despite fewer issues; the
share improvement is not solely a shrinking denominator. The 696 additional
professional issues are not individually identified cover substitutions.

| Launch genre, same two seeds | Professional issues | Covers | Covers per professional |
|---|---:|---:|---:|
| RockAndRoll | 24 → 225 | 959 → 759 | 39.96 → 3.37 |
| DooWop | 43 → 127 | 276 → 270 | 6.42 → 2.13 |
| RnB | 47 → 135 | 993 → 717 | 21.13 → 5.31 |
| TraditionalPop | 57 → 73 | 1,236 → 934 | 21.68 → 12.79 |
| Country | 80 → 136 | 829 → 564 | 10.36 → 4.15 |
| Soul | 56 → 90 | 262 → 287 | 4.68 → 3.19 |

These are changed genre populations, not matched individual recordings.
TraditionalPop remains particularly imbalanced. The aggregate still falls
outside the earlier working 1–2:1 sensitivity range, which is a calibration
hypothesis rather than a measured historical release-wide target.

### Career breadth and smaller-label turnover both increase

| Seed | Hit acts | Complete cohort with 1–3 further hits | Fixed top-30 Top 100 slot share | Failed/observed labels | All writing fees | Unused licensed stock |
|---|---:|---:|---:|---:|---:|---:|
| 1001 | 257 → 290 | 41/155 → 60/175 | 30.0% → 27.6% | 89/667 → 126/692 | $373,894 → $507,791 | 14 → 56 |
| 2002 | 258 → 283 | 41/161 → 60/166 | 28.9% → 29.6% | 73/662 → 106/677 | $389,506 → $480,531 | 19 → 66 |

Hit-act counts and the complete repeat-career middle improve in both seeds:
pooled hit acts **515 → 573**, and 1–3-further-hit cohort members
**82/316 (25.9%) → 120/341 (35.2%)**. Cohort membership can change. Fixed-headcount
slot concentration is mixed; it is not uniformly improved. Silence after the
origin hit is also mixed (2.6% → 4.0% and 6.8% → 1.2%).

Failed-label counts rise in both seeds, **162 → 232 pooled**, among
1,329 → 1,369 observed label-worlds (12.2% → 16.9%). The worlds form different
new labels, so these are not paired individual failure probabilities. All
writing fees rise **29.5%**, $763,400 → $988,322, while endpoint unused licensed
stock rises **33 → 122**. This is an endpoint inventory, not permanent waste.

#### Exit composition: turnover is not automatically a balance failure

The user raised the possibility that stronger competition naturally closes more
small labels. A follow-up decomposition supports the concentration of exits in
smaller firms, but does not establish competition as their cause:

| Exit classification, both seeds | Control | Combined | Net change |
|---|---:|---:|---:|
| Bankruptcy | 137 | 185 | +48 |
| Solvent dormancy | 25 | 47 | +22 |
| Small at exit | 33 | 69 | +36 |
| Independent at exit | 77 | 103 | +26 |
| Boutique at exit | 29 | 40 | +11 |
| MidTier at exit | 23 | 20 | −3 |
| Major at exit | 0 | 0 | 0 |

Reasons and tiers are two classifications of the same exits, not additive rows.
`EconomicLabelLifecycle.ReviewMonth` sets bankruptcy after two consecutive
negative cash month-ends. `FounderLabelLifecycle.ReviewMonth` identifies a solvent
founder's dormancy after twelve quiet months with no pending project or protected
obligations blocking retirement, nonpositive recent cash flow and insufficient
available funds for the reference session. Neither reason directly records
“outcompeted by a stronger label.” The dormancy period/session budget are existing
scenario assumptions, not measured historical exit rates.

Initial-tier cohorts avoid classifying a firm by its post-treatment demotion:
opening Small exits rise 32 → 69, Independent 69 → 91 and Boutique 29 → 40;
opening MidTier exits fall 23 → 20. The net increase is 67 among opening firms and
3 among later-observed firms. Both runs in each seed contain 596 opening label
identities. Matching name/archetype/historical status/initial tier finds 147
identities active at the control endpoint but closed in the combined world,
and 80 with the reverse outcome. The worlds change initial conditions and
subsequent trajectories; these identity comparisons are not a clean firm-level
causal estimate. Across all observed firms, historical exits fall 17 → 16 while
procedural exits rise 145 → 216.

Therefore the raw closure increase should be described as **more small-label
turnover**, not automatically as worse economic realism. Competition-driven
selection remains plausible. A useful next diagnosis would track the opening
cohorts' release opportunities, sales/receipts, cash commitments and liabilities
before exit, then compare a few controlled bundles separating changed competition
and repertoire from forecast/risk/campaign/cost rules. This can distinguish
commercial displacement from avoidable accounting or commitment starvation
without assuming that every small firm ought to survive.

Across all 104 weeks, including launches outside the mature intention cohort:

| Output | 1001 | 2002 | Pooled change |
|---|---:|---:|---:|
| Single launches | 7,155 → 6,738 | 7,070 → 6,790 | 14,225 → 13,528 (−4.9%) |
| Released album projects | 5,106 → 4,033 | 5,033 → 4,076 | 10,139 → 8,109 (−20.0%) |

Store clearing nevertheless rises from 495.1M to 499.4M single units (+0.9%) and
182.9M to 211.4M LP units (+15.6%). These are the store-clearing observer's units,
not an all-channel total incorporating autonomous racks/clubs/budget products.
More units with fewer releases does not identify which production, repertoire,
audience or market mechanism caused the change.

### Album allocation remains a live constraint

For these same two seeds, newly recorded demos with released album-only use move
**2,213/3,201 (69.1%) → 2,119/3,535 (59.9%)**. Single-only destinations rise
267 → 677, both-route destinations move 454 → 417, and no observed released use
moves 267 → 322. Use the two-seed denominator here, not the original three-seed
68.2% figure.

The preparation-time diagnostic still finds **79 demo/first-client observations
covering 70 distinct subsequent singles** where a then-unprepared planned single
ultimately uses a composition with hook at least .10 below an album-only song.
The same two controls had 134 such observations covering 102 singles. The
composition-only comparison cannot establish act fit, perceived preference,
ownership at the later decision, or the alternative project's profit. This
combined configuration does not change the previously identified prepared-slate
extraction bypass in engine code.

### What this screen establishes

The broad integration configuration is runnable for two years after documenting
the mutually exclusive settings and excluding the real physical-inventory
blocker. It improves professional use and repeat-career breadth in both seeds,
while raising writing costs, unused licenses and failed-label counts and reducing
release volume. It is therefore a more informative future-profile candidate
than a claim that the omitted switches were irrelevant, but it is not an
isolated causal diagnosis or a successful whole-game balance verdict.

All store-clearing inventory/allocation/reconciliation/settlement checks remain
zero. Monthly company cash reconciliation errors are at most $2.33e-10 in the
combined runs. These checks do not repair or validate the excluded warehouse
ledger. All 27 writing-analysis tests and the two material-funnel fixtures pass;
the new Python scripts compile, and both PowerShell runners parse cleanly.

The full generated comparison is
`SimLogs/writing-calibration/writing-combined-integration-comparison.json`;
`writing-combined-integration-plan.json` records the smoke, matched full requests,
flag-file hash, recent source provenance and reconstructed failed configurations.

The isolated three-seed rock-supply comparison completed separately on the same
assembly. A broad scenario difference cannot be attributed to the office alone,
and neither 104-week result replaces the intended branch-end decade evaluation.

## Reproduction

```powershell
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Weeks 104 -Tag NEW-FRESH-TAG
python SimTools/analyze_writing_combined_integration.py --tag NEW-FRESH-TAG
```

The wrapper refuses existing output names. Keep the flag file frozen while runs
execute; requests/manifests preserve each run's explicit arguments. Generated
artifacts belong under `SimLogs/writing-calibration/`.
