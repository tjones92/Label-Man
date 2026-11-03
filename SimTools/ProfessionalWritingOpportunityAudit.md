# Writing workflow: opportunity and material audit

This observational follow-up uses the corrected `writing-workflow-v2` runs, not the
rejected pilot. `analyze_writing_opportunity.py` counts each single intention once,
requires 26 complete weeks after proposal, and uses actual issued-record provenance.
The report groups plans by their recorded career evidence. These are plan-level
observations, not a census of all eligible artists and not a causal decomposition.

| Seed | Workflow | Mature single plans | Prepared within 26 weeks | Issued within 26 weeks | Issued with professional song |
| --- | --- | ---: | ---: | ---: | ---: |
| 1001 | Control | 6,485 | 5,621 | 4,994 | 399 |
| 1001 | Combined | 6,489 | 5,617 | 5,015 | 472 |
| 2002 | Control | 6,378 | 5,571 | 4,945 | 363 |
| 2002 | Combined | 6,420 | 5,604 | 4,941 | 476 |

Mature new-signing plans issue 3,917 / 5,117 and 3,921 / 5,108 in the controls;
the combined arms issue 3,952 / 5,130 and 3,921 / 5,134. These observations do
not show a large fall in proposed or issued singles for that career category.
They do not count signed artists who never receive a proposal, and a plan's career
classification is not a fixed cohort across arms.

In the combined arms, 249 / 3,952 and 247 / 3,921 issued new-signing plans use
professional songs, versus 90 / 394 and 70 / 312 issued established-act plans.
That is a descriptive gap, not evidence that either group was denied material:
access, genre, demand, willingness to pay and selection all differ.

Completed recycled-commission holds fall from 1,307 to 70 and 1,306 to 106.
Completed existing-demo holds rise from 813 to 1,297 and 900 to 1,251. Hold
episodes are not unique songs or a count of available catalogue stock; a hold
must expire before the end to enter this comparison. The drop establishes a
large change in route, not an overall shortage: professional songs on issued
plans actually increase in both seeds. Albums and release routes outside these
single plans are not included in that plan-level count. Supplier income and the
career and concentration outcomes are in `ProfessionalWritingWorkflowExperiment.md`.

The weekly stock trace gives further evidence against a world-wide shortage.
Mean available demos increase from 183 to 236 in seed 1001 and from 194 to 269 in
seed 2002; endpoint counts increase from 282 to 374 and from 295 to 397. The mean
available-song hook is .689 versus .683 and .704 versus .707, respectively. These
are song-week stock averages over weeks 0–104, not the material visible, affordable
or suitable to any particular act. Holds and priority restrictions still affect
access. The trace includes only the finite writing-market demo inventory.

## Allocator observer replay

Four fresh 104-week runs (`opportunity-{control,combined}-market-{1001,2002}`)
enable the candidate audit on the original frozen assembly. For each run, nine
files match the corresponding workflow-v2 run byte for byte: chart records,
single launch quality, label finance, act release years, writing suppliers,
orders, partnerships, release slate and writing launch provenance. All four
runs report normal completion and no logged errors. This establishes that the
observer reproduced those outcomes, not that every possible state was compared.

| Seed | Workflow | Eligible new-signing acts | Selected at least once as new signing | Eligible new-signing candidate-slot observations | Selected observations |
| --- | --- | ---: | ---: | ---: | ---: |
| 1001 | Control | 3,675 | 3,015 | 66,119 | 6,763 |
| 1001 | Combined | 3,642 | 2,995 | 65,584 | 6,774 |
| 2002 | Control | 3,525 | 2,939 | 61,491 | 6,812 |
| 2002 | Combined | 3,498 | 2,938 | 61,772 | 6,861 |

These are whole-window observations. An act can be eligible in many reviews,
change career state, join or leave the roster, or be selected at a different
career state. Candidate-slot observations are not independent trials; the
unique-act percentage is not an exposure-adjusted probability or a complete
signed-roster census. The new-signing selection counts are similar between arms,
while hundreds of acts observed as eligible at that career state were never
selected in that state. That is a reason to investigate opportunity access, not
proof that a priority bonus would improve chart conversion or concentration.
The separate `ProgrammeFollowupConversionInvestigation.md` found that most
observed first-hit acts already obtain a prompt follow-up and cautioned against
another blanket scheduling trial.

The market already produces speculative demos on a bounded supplier cadence,
subject to capacity, a stock cap, an imperfect self-read and contract priority.
It would be premature to add more supply based solely on lost recycling. The
candidate-level allocator observations and supply availability need investigation
before choosing and isolating an intervention. Do not change game defaults based
on this audit.

## Reproduction and limitations

Run `analyze_writing_opportunity.py RUN... --output PATH` on runs sharing a frozen
assembly. It requires the plan, launch, material search, order and demo traces,
checks the 104-week chart window and rejects missing provenance for issued plans.
`SimLogs/writing-calibration/opportunity-audit.json` contains the four-run result.
Search and delay reasons are plan counts; multiple reasons may apply to one plan.

New runs may add `-OpportunityAudit` to `RunWritingMarketCalibration.ps1` to
capture the existing `--label-decision-audit` candidate observations. The analyzer
then reports eligible and selected unique acts and candidate-slot observations;
an eligible act may appear many times and under more than one career state. These
observations cover allocator reviews, not every signed act, and do not identify
why an act received no review. Verify observer noninterference against the matched
old run before interpreting it. The pre-existing workflow runs did not enable this
trace, so their `choices` field is null.
