# Labels as continuing businesses

24 October 2026. User direction and code/behavior audit following
[LabelBalanceInvestigation.md](LabelBalanceInvestigation.md).

The user wants every AI label to behave as a small label simulation: cultivate
hitmakers, find promising acts, obtain suitable material, choose projects and
work releases within its means. Firm differences must emerge from shared rules
and evolving circumstances. Individual historical firms are diagnostic cases,
not separate success targets, templates or corrective bonuses.

The existing game contains substantial parts of this model. The central
question is whether they form a coherent decision loop in the active experiment.
Changing release frequency alone does not answer it.

This audit connects the existing [Release Judgement design](ReleaseJudgementDirective.md)
with the later [release programme implementation](LabelReleaseProgrammeImplementation.md).
It does not call for a second repertoire, forecast-memory or project system.

## Existing behavior and the connections still missing

| Function | Existing implementation | Limit in the current blend |
|---|---|---|
| Find artists | `RosterManager.ProcessDailyTalentMarket` schedules appointments, uses regional/geographic access and imperfect `ScoutingPerception`, compares fresh potential with experienced acts, checks advances and resolves competing offers through artist choice | The ordinary unsigned-market path requires an operating-roster vacancy. It is primarily replenishment; a continuing watchlist/search while fully staffed is not established by that path. Other acquisition and contract routes still exist |
| Maintain a roster | Contract-time renewal, probation, recent-hit retention, drop reviews and operating-roster targets persist | Retention evidence is mostly career/chart/contract state. The new attached audience is not yet a label opportunity or renewal-value reader |
| Prioritize acts | `PlanProgramme` compares eligible roster acts using career, momentum, obligations, age, recent regional breakouts and prior expected net; a due recent hit gets +10 | Capacity slots are generated first, then the highest score fills each. This does not begin with a portfolio of specific material-backed projects or an explicit choice to invest in development |
| Follow up success | Separate single/LP clocks, 13-week hit urgency, earlier breakout planning and saved dated intentions | Career priority can schedule a successful act at the six-week feasibility floor. Urgency is coarse; the planner has no explicit earned-audience estimate, particular-song readiness or campaign opportunity cost in its ranking |
| Find material | `SongMaterialSelectionService` and polar fit/refusal affect sessions; slate preparation creates a controlled master before issue | The blend leaves `--anticipatory-material-search` off. The optional saved `AiRepertoireService` song shortlist/selection is also off. Code presence does not establish persistent repertoire-led planning in this world |
| Fund and work a project | Operating/slate reserves, recording costs, physical stock, promotion bookings, distribution, receivables and competing campaigns constrain execution | Some constraints are applied after the slot/artist choice, so an optimistic or unsuitable plan can be repeated or postponed rather than reconsidered as a business proposition |
| Judge returns | Prior returns and format revenue memory exist; label/company accounts retain actual receipts and expenses | Profit enters the allocator as `clamp(expectedNet / productionCost, -1, 1)`. Its ability to distinguish candidates depends on saturation. The optional `--release-all-in-economics` positive-profit gate is not active in this blend; a profitable-release gate alone would not supply a development strategy either |

These are code-path findings, not claims that every observed choice is wrong.
An act with the highest immediate forecast profit should not automatically win:
contract delivery, preserving a hit career and deliberately financing a prospect
can be sound reasons to choose another project. Those reasons should be explicit
and supported by resources and evidence.

The active source is the completed `.6 + audience` blend manifest, not an audit
profile that enables a larger research stack. `ReleaseJudgement.Configure`,
`LabelReleaseProgramme.Configure` and `ReleaseAllInEconomics.Configure` read
their explicit switches. The matrix distinguishes those optional paths from
systems already active by the game/corporate defaults.

## Shared decision model to build toward

This is the behavioral specification for subsequent changes, not a declaration
that all of it is already implemented.

1. **Review the business and the roster.** Use observed audience, chart and sales
   evidence, receipts, current contracts, available staff, cash and campaign
   commitments. Recognize an established seller, a breakout needing a follow-up,
   a promising prospect and an act needing better material from their state.
   These are changing assessments, not permanent template roles.
2. **Keep A&R observing opportunities.** Reuse the existing access, perception and
   competing-offer systems. A full roster can constrain an offer without making
   the label oblivious to new acts. Record knowledge, uncertainty and the next
   action; a watchlist must not create free scouting knowledge or bypass signing
   costs, local access or the hard roster cap.
3. **Develop concrete projects.** A worthwhile act may need a song search,
   rehearsal, an audition, another session, or a held master. Reuse prepared
   masters, material work and the existing shortlist implementation where
   compatible. A calendar opening should not itself establish a good project.
4. **Choose a portfolio with real opportunity costs.** Compare serving known
   demand, testing a new act, buying/licensing material or waiting. Use the
   label's circumstances, risk tolerance and finite investment capacity. A
   development loss must be a deliberate funded commitment, not an unlimited
   profitability credit. Do not introduce named-label quotas or a required
   cancellation percentage. Waiting or seeking better material must remain a
   valid choice even when an artist is technically release-eligible.
5. **Schedule and work the chosen projects.** Allocate sessions, issue dates,
   cash and promotion together. Frequent hitmaker releases should follow from
   demand, prepared material and the capacity to work them. Delays should carry
   reasons and next actions. Preserve the project's identity and paid work when
   it is reconsidered.
6. **Learn from the result.** Compare forecast with actual cleared purchases,
   service failures, campaign response and financial returns. Distinguish weak
   demand from unavailable stock or distribution. Revise the artist/project
   assessment, then feed it back to A&R, material choices, campaigns and renewal.
   Preserve useful existing format memory rather than adding a competing ledger.

Randomness belongs in imperfect information, uncertain responses and competition.
It should not substitute for a reason to sign an act or issue a particular record.

## Decision audit added in this pass

`--label-decision-audit` writes `*-programme-choices.csv` from the existing
allocator. It retains every scored alternative, eligibility/spacing exclusions,
conflicting intentions and empty opportunities. Each row shows the rank,
selection, hot/due state, prospective gap, forecast net, capped profit
contribution, career priority, momentum and obligations. Attached audience is
logged as an **observation**, explicitly not a new input the label has learned
to use.

The observer does not change scores, thresholds, project state, capacity or RNG.
An excluded candidate receives no new forecast or clock read. The score formula
is retained, and the audit exists only while a subscriber is attached. Nothing
new is written to a save. The existing daily A&R appointment ledger supplies
candidate-lane and offer/competition outcomes.

`analyze_label_decisions.py` verifies that the selected artist matches the
existing score ordering, all ranks and alternatives are retained, empty slots
remain in denominators, and each initial intention matches its selected artist
and planning week. A later breakout reassignment is kept separate. It requires
an observational replay against a completed control before reporting behavior.

Reproduce with an unused tag:

```powershell
./SimTools/RunLabelDecisionAudit.ps1 -RunTag label-ai-v1
```

The default reference is `label-balance-v1-pilot-local-control`, seed 1001,
106 weeks. Arguments, assembly hash and native completion are retained by the
existing runner. Reports, decision and intention ledgers, A&R summaries and the
stream identity proof are generated under `SimLogs/TAG/review/`.

## Completed first decision audit

`label-ai-v1`, seed 1001, completed 106 weeks. All 66 checked week-keyed
economic/chart streams match the control through week 105, before the terminal
snapshot boundary. Every initial intention reconciles with its selected artist
and planning week. The trace therefore describes the candidate world rather
than a new AI treatment. It is one seed, not a calibration estimate.

The ordinary A&R market is finding new acts. Accepted offers include 107 fresh
potential / 2 experienced acts for the Big Five, 43 / 3 for studios, and 1,257 /
237 for the field. There are 114, 113 and 3,190 appointments respectively;
appointments are not distinct candidate discoveries. The main A&R gap identified
in code is how ongoing observation and roster strategy connect to this market,
not an absence of fresh-prospect signings.

| Initial single planning choices | Big Five | Studios | Field |
|---|---:|---:|---:|
| Selected act has a recent Top 40 | 56.6% | 52.8% | 12.3% |
| Select a due hit act when one is eligible | 100% | 100% | 100% |
| Selected act's prior expected net is nonpositive | 62.3% | .5% | 3.6% |
| Every eligible act's prior expected net is nonpositive | 32.0% | 0% | 2.4% |
| Selected profit contribution is capped at +1 | 0% | 48.6% | 81.6% |

The Big Five create 1,370 initial single intentions from 1,398 dated
opportunities. Of those intentions, 854 have a nonpositive prior expected net;
415 choose a nonpositive forecast despite an eligible positive-forecast
alternative. These are generic prior forecasts, not the returns of the specific
song/master eventually issued. They may be miscalibrated. A higher prior alone
does not establish the better project, and later preparation/cash checks can
still stop an intention. Nevertheless, a completed calendar is plainly not
evidence of adequate project judgment: in 32% of these choices, the planner sees
no positive prior among eligible acts and still assigns one to the slot.

Hit urgency contributes +10 while profit contributes at most -1 to +1. Due
status can dominate a weak forecast, but it does not explain the whole problem:
nonpositive forecasts are also selected for 61.9% of non-due Big Five choices.
The general slot-filling rule, forecast quality and absence of a project-level
investment/hold decision all need examination. This is not grounds to turn on
the optional positive-profit gate blindly: it would read the same priors and
could suppress sensible development or known demand that they undervalue.

An inspectable example is the week-41 single intention for `label_0003`, dated
2 December 1960. Rising act `artist_01212` is due at a prospective 13-week gap;
its prior net is -$851.75 and score 10.819. Established act `artist_01245` has
a +$87.52 prior but score 1.285 at a six-week gap. The former is selected,
issues as `gen_5082` in week 48, misses the chart and retires in week 70 with
reported realized net -$6,565.70. That outcome is not evidence that the other
act would have succeeded. It connects a specific urgency-led choice to what
actually happened and makes a material/forecast/campaign review possible.
All alternatives are retained in the raw choice ledger; the example's rows
are also under `SimLogs/label-ai-v1/review/example-choice.csv`.

**Conclusion for the next implementation:** a hitmaker should earn attention
and an appropriate follow-up project. Attention may mean seeking material,
developing an arrangement or holding a master for a workable campaign. It
should not automatically mean another dated release. Integrate the existing
material/project systems with shared portfolio judgment, and connect A&R and
audience evidence to that judgment. This pass makes the weakness observable;
it does not claim the complete mini-label decision model is implemented.

## Behavioral acceptance

Implementation has begun in [LabelProjectJudgement.md](LabelProjectJudgement.md):
the first step connects existing repertoire and material work, persists prepared
song choices, and adds a shared material review before recording. The audit above
describes the unchanged comparison control.

Evaluate whole label histories as well as owner-group statistics. A credible
trace should explain a hit follow-up, a prospect investment, a refusal or delay
for weak material, a capacity conflict, an unsuccessful campaign and a later
change of mind. It must show the alternatives and the evidence available at the
time, without accessing hidden commercial-fit draws or future outcomes.

Tests should establish that the same shared rule responds to different business
states: success earns attention, weak material can delay a hitmaker, a strong
prospect can attract investment, cash and staff bind, and disappointing evidence
can change the next decision. Confirm that prospects are not permanently starved
by hitmaker priority and that known sellers are not neglected by recruitment.
Validate persisted intentions and knowledge through save/load when causal state
is added. Check market accounting, careers and company survival alongside chart
balance. Matching historical firm shares is not a behavioral acceptance test.

Verification: build passed without warnings/errors; 23 Python regression tests
passed, including four choice-audit fixtures; the existing native scheduler
checks passed 46 assertions and breakout planning passed 20. The completed
replay establishes observational identity separately from the audit's ranking
and intention checks. No simulation policy or game default was changed.
