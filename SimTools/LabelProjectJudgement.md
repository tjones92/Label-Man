# Connecting repertoire to label projects

24 October 2026. First implementation following [the label AI audit](LabelAISimulationAudit.md).

`--label-project-judgement` enables the existing persistent repertoire, perceived
song selection and anticipatory material search together. It requires
`--label-release-slate`; the continuing balance scenario also supplies capacity,
work budgets, breakout planning and the existing audience-demand experiment.
This is an explicit development configuration. Existing game defaults and the
previous blend remain available as the comparison control.

## Shared decision rule

For singles, the old +10 due-hit ranking bonus becomes +.75 attention, with a
bounded material-read contribution between -1 and +1. Career, momentum, prior
returns, obligations and spacing still participate. These are initial policy
parameters, not historical estimates or per-company targets.

At preparation, the label compares its best heard shortlist song with its
imperfect expectation of this act's routine original session. Both use the
existing perception model and the same recorded-quality scale. The routine
reference has a stable identity and evidence version; repeated reviews do not
average away the read error. No audit oracle, future demand, realized commercial
fit or named-label identity enters the decision.

A pending order waits for delivery. A ready searched song competes with existing
shortlist material; the label uses the better heard read. An insufficient or
empty shortlist produces `hold-for-stronger-material`, without a session or its
production charge. A cleared project records concrete material; an empty or
refused selection cannot silently manufacture a legacy fallback. Existing
cash, staff, promotion, spacing and issue-window constraints continue to apply.
The original expiry window remains fixed, so a hold cannot refresh itself forever.

Searches now value an offer against the stronger of the routine session and the
best existing shortlist read. They reuse the existing bounded sample, supplier
limits, lead times, fees, disappointment and rights checks. There is no fresh
offer lottery on each weekly review. Material can also arrive through the
existing repertoire's dated writing and supply events.

The same material standard applies to stars and prospects. A negative generic
prior does not automatically veto a project: the existing forecast is not yet a
song-specific investment valuation. The optional all-in positive-profit gate
is not added to this scenario.

## Prepared-master integration

Previously the selector's transient record-to-song pick was cleared each week,
although a slate master could be prepared before its issue week. A paid slate
session now makes its candidate `Recorded`, removes it from available material,
and persists the exact record-to-song pick. Issue consumes that saved pick.
An outside-material session or player replacement does not consume an unrelated
shortlist song. Cancellation clears the pending issue identity and keeps the
recorded song unavailable; already paid work is not refunded.

New project/pick state requires save version 31. Absent nullable fields keep
older configurations' save shape. The shortlist's existing arrival/read stream
is unchanged; no new global random draws are introduced by reviews.

`*-programme-projects.csv` records each preparation review's act, intention,
candidate, perceived read, routine read, action and choice of searched material.
The ordinary slate and material-search ledgers retain scheduling, paid work and
delivery outcomes.

## Reproduction

Run an unused tag from the repository root:

```powershell
./SimTools/RunLabelProjectAudit.ps1 -RunTag label-project-v1 -Seed 1001 -Weeks 106
```

The runner reuses the completed balance reference's economic flags, replays an
unchanged control, connects existing material systems in a second arm, and enables
the reviewed-project bundle in a third. It preserves one assembly hash across
arms. `analyze_label_projects.py` requires declared configuration differences,
completed native runs, old-stream control identity, fixed consumer capacities,
market reconciliation, and material authorization invariants. Reports distinguish
open intentions from issued and cancelled projects. Holds are evidence, not a
target cancellation percentage.

Native verification includes the repertoire suite, existing material/scheduler/
breakout suites, and `--programme-project-roundtrip --label-project-judgement`
through `SaveLoadRoundTripRunner.tscn` with the balance flags. The last check finds
a prepared single in a live world, round-trips the entire world and gzip file,
then verifies its saved master, paid production and exact song at resolution.

This first step connects material decisions. Continuous A&R observation while
the roster is full, audience-aware opportunity/renewal valuation, explicit funded
development portfolios and campaign learning remain later work. Their presence
elsewhere in the model is not evidence that this implementation completes them.

## Verification recorded so far

Build: zero warnings/errors. Python regressions: 25 passing tests. Native
repertoire checks: 46 assertions, including a star held for weak heard material,
a prospect authorized for strong material, stable/costless reviews, exact prepared
song identity through serialization, and later issue. Existing native material
checks pass 31 assertions; scheduler 46; breakout planning 20.

The full-world project roundtrip passes exact capture/apply/capture identity and
gzip I/O, then resumes a prepared project through cancellation with its master and
production payment unchanged. The separate repertoire test exercises later issue.

The 106-week `label-project-v1-control` replay matches all 66 checked week-keyed
streams of the archived control through week 105, before the terminal snapshot
boundary.

## First screen: `label-project-v1`

Seed 1001, 106 weeks, one assembly across arms. One seed is a behavioral screen,
not a calibration estimate. Generated by `analyze_label_projects.py` under
`SimLogs/label-project-v1/review/`.

| Annual mean | Control | Connected | Judged |
|---|---:|---:|---:|
| Big Five repeat Top 10 acts | 12 | 9 | 9 |
| Studio repeat Top 10 acts | 2 | 4.5 | 3.5 |
| Field repeat Top 10 acts | 7 | 8.5 | 7 |
| Big Five Top 10 share | 34.9% | 34.1% | 29.2% |
| Studio Top 10 share | 10.3% | 12.4% | 10.8% |
| Field Top 10 share | 54.8% | 53.5% | 60.0% |

| Singles | Control | Connected | Judged |
|---|---:|---:|---:|
| Big Five issued / cancelled | 825 / 408 | 819 / 394 | 719 / 523 |
| Studio issued / cancelled | 179 / 19 | 193 / 7 | 176 / 26 |
| Field issued / cancelled | 6,072 / 975 | 5,926 / 1,018 | 5,520 / 1,399 |
| Big Five unit share | 30.4% | 30.1% | 24.4% |
| Opening field MidTier closures (of 25) | 13 | 15 | 14 |

Total single units are flat across arms (294.5M, 296.3M, 295.2M), so the
market is unchanged; the judged arm moves share away from the Big Five by
issuing fewer of their records.

**How held projects resolved (judged arm).** 656 of 7,907 reviewed single
projects (8.3%) were held at least once. Only 141 (21%) were later approved and
issued; 21 were approved but not yet issued, 435 (66%) were cancelled (406 for
`window-expired`) and 59 were still open at week 106. Hit follow-ups: of 143
held, 45 issued and 80 cancelled; for the Big Five, 31 issued and 33 cancelled
out of 77. Recovered holds waited a median of 4 weeks (interquartile 2-7).

Most holds were marginal. Excluding the 14% with an empty shortlist, the best
heard read fell short of the routine-session read by a median of .028
(interquartile .012-.053), likely within the label's own read error. The rule was
therefore mostly waiting out its window over small differences and then losing
the project, rather than finding stronger material. Too cautious.

## Revision: a bounded hold with the act's own session

The shared rule now holds only while waiting can still pay off. Once the fixed
window is closing (from `IntendedWeek + Horizon - PreparationWeeks`, the ordinary
preparation lead before the last issue week), a project without stronger heard
material records the act's own session (`record-own-session`) instead of
expiring. A pending material order no longer holds a project past that point.
In the window's final week nothing is recorded, because no later week can issue
the master.

The own session reuses the ordinary artist-written source
(`ChooseMaterial(..., ArtistWritten, forcedOnly: true)`): the existing stable
craft draw, with no new random draw. If the act refuses its own original, the
project gets no material and expires as before; it never falls back to another
source or to the unreviewed legacy choice. The routine read was already the
benchmark, so this records the expectation the label held rather than a new
judgement. Hit follow-ups get no special case: one deadline applies to every
project. A 2-week cap was considered and rejected, because recovered holds
waited a median of 4 weeks.

Verification: build clean; Python 3 tests pass (own-session rows must name the
routine session); native repertoire checks pass 49 assertions (3 new: the own
session at the closing review, an undelivered order does not extend the hold, and
the closing-review weeks, including no session once nothing can issue); the
full-world programme-project roundtrip passes.

### Screens of the revision

Both runs use seed 1001, 106 weeks and the balance flags plus
`--label-project-judgement`, built from the revised assembly. They are compared
with the v1 arms above. The revision is gated by the project flag, so the control's
behavior is unaffected, but the assemblies differ.

`label-project-v2-judged` triggered the fallback at `Horizon - 1`. Expired holds
fell from 406 to 51, but 68 own-session masters were cancelled after their
production was paid ($137k): 17 recorded in the final week, when no issue was
possible, and 51 that lost their single issue week to promotion congestion or cash.
`label-project-v3-judged` is the rule described above.

| Annual mean / count | v1 connected | v1 judged | v2 judged | v3 judged |
|---|---:|---:|---:|---:|
| Big Five repeat Top 10 acts | 9 | 9 | 7.5 | 7.5 |
| Studio repeat Top 10 acts | 4.5 | 3.5 | 4.5 | 2 |
| Field repeat Top 10 acts | 8.5 | 7 | 6 | 7.5 |
| Big Five Top 10 share | 34.1% | 29.2% | 30.8% | 33.5% |
| Studio Top 10 share | 12.4% | 10.8% | 12.7% | 10.6% |
| Big Five unit share | 30.1% | 24.4% | 24.5% | 26.8% |
| Big Five singles issued / cancelled | 819 / 394 | 719 / 523 | 746 / 490 | 782 / 462 |
| Field singles issued / cancelled | 5,926 / 1,018 | 5,520 / 1,399 | 5,934 / 1,151 | 5,956 / 1,094 |
| Opening field MidTier closures | 15 | 14 | 11 | 13 |
| Paid single masters later cancelled | 726 | 810 | 879 | 780 |

Total single units stay between 294M and 296M in every arm.

In v3, 628 projects were held. 317 were issued as own sessions and 105 with
stronger material that arrived in time; 98 were cancelled and 47 were open at the
end. Of 122 held hit follow-ups, 83 were issued and 17 cancelled. Of 373 own
sessions, 320 issued and 51 were cancelled: 46 for `window-expired` after
recording with two issue weeks left, most of them blocked by the label's own
promotion congestion. That is a capacity conflict, not the timing bug.

Costs that remain:

- A held project now usually waits out its window: recovered holds waited a
  median of 7 weeks, against 4 in v1. A hitmaker without stronger material
  releases about seven weeks late, not never.
- Holds are still mostly marginal (median shortfall about .03). The next revision
  addresses this.
- Big Five unit share remains below the connected arm (26.8% against 30.1%),
  with 68 more Big Five cancellations. Repeat-act differences across these arms
  are within one seed's noise and are not yet evidence either way.

## Revision: hold only beyond the label's read uncertainty

A shortfall the label cannot tell apart from its own read error is not evidence
of weaker material. The review now records heard material when
`perceived >= routineRead - uncertainty`, and holds only beyond that. The
uncertainty comes from the existing read-error model, with no new parameter. Each
read errs uniformly within its half-width, and the tolerance is one standard
deviation of the difference of the two independent errors,
`sqrt((a^2 + b^2) / 3)`:

- Two shortlist reads of one act share the label's act bias (`BiasShare` = .25),
  which cancels, so each half-width is `.75 * band`.
- A delivered searched song has no shared bias, so both full bands apply.
- An empty shortlist still holds.

Sharper scouts have narrower bands and are therefore stricter. Each review logs
its tolerance (`uncertainty` in `*-programme-projects.csv`, and in the saved
review; an older v31 save reads it as zero). The session's shortlist selection
uses the same threshold through `SessionMinimumRead`. The material-search
baseline and the planner's material-attention score still compare against the
plain routine read.

`label-project-v4-judged` is invalid. Its review applied the tolerance but its
session still filtered shortlist songs at the exact routine read. Approved
projects failed at recording and were re-approved weekly (up to 15 times) until
their windows closed, which also bypassed the own-session fallback. 302 such
projects were cancelled. `label-project-v5-judged` is the corrected rule. A native
check now asserts that a session accepts the song its review approved, at the
review's own tolerance.

| Annual mean / count | v1 connected | v3 judged | v5 judged |
|---|---:|---:|---:|
| Big Five repeat Top 10 acts | 9 | 7.5 | 6.5 |
| Studio repeat Top 10 acts | 4.5 | 2 | 1.5 |
| Field repeat Top 10 acts | 8.5 | 7.5 | 7.5 |
| Big Five Top 10 share | 34.1% | 33.5% | 31.8% |
| Studio Top 10 share | 12.4% | 10.6% | 7.2% |
| Big Five unit share | 30.1% | 26.8% | 26.9% |
| Big Five singles issued / cancelled | 819 / 394 | 782 / 462 | 760 / 468 |
| Field singles issued / cancelled | 5,926 / 1,018 | 5,956 / 1,094 | 5,913 / 1,142 |
| Opening field MidTier closures | 15 | 13 | 12 |
| Paid single masters later cancelled | 726 | 780 | 813 |

The tolerance averaged .073 (range .036-.13). Holds fell from 628 projects
(7.9%) in v3 to 273 (3.4%), and 38% of the remaining holds have an empty
shortlist. 294 approvals fell inside the tolerance. Of the 273 held projects, 118
were issued as own sessions and 58 with stronger material, and 60 were cancelled.
Of 65 held hit follow-ups, 41 were issued and 11 cancelled.

The change does what it should to holds, but its market effect is within one
seed's noise. Studio share (12 labels) is the most volatile row. The hold is no
longer the main difference from the connected arm. 445 of the Big Five's 468
cancelled single projects in v5 had a recorded, paid master that then failed at
issue (`material-economics-funding-or-availability` in `IssueSlate`); the
connected arm shows the same pattern at 370 of 394. The remaining Big Five gap
(+74 cancellations, unit share 26.9% against 30.1%) lies in issue-time economics,
or in the planner's revised ranking (+.75 attention and the material read
replacing the +10 due-hit bonus) changing which acts reach issue. It is not
explained by material holds.

Verification: build clean; 4 Python tests pass; 52 native repertoire assertions
pass, including tolerance arithmetic, logging and review/session consistency; the
full-world programme-project roundtrip passes.

## Why prepared Big Five masters fail at issue

Traced from `label-project-v5-judged` against `label-project-v1-connected`.
Observer-only telemetry (`OnProgrammeIssueRefused`, written to
`*-programme-issue-refusals.csv`) names the gate that refused each prepared
master at issue. The existing slate reason strings are unchanged. A rerun with the
telemetry (`label-issue-trace-judged`) reproduces v5's slate, project, record and
cash ledgers byte for byte.

**The gate.** `IssueSlate` calls `TryReleaseRecord` with `singlesOnly`. With
all-in economics off, that path refuses a single when
`plan.projectedOrphanSingleNet <= label.GetMarketingBudget(artist)`.
`projectedOrphanSingleNet` is already net of the single's production cost
(`SinglePriorNetAtBookedPrice` subtracts `decision.singleProductionCost`). For a
prepared master that production has already been paid, so the gate charges a
sunk cost a second time. Preparation never applies the check, so the label pays
for a session it then refuses to issue, and the unchanged evidence refuses it
every week (about 10 times) until the window expires.

In the judged trace, 437 of the 442 cancelled Big Five masters with refusals
were refused mainly by this gate (4,440 of 4,489 refusal weeks). Cash,
promotion headroom and artist spacing are minor: the Big Five never had a cash
shortfall at issue, and spacing accounts for about 0.5% of the refusal weeks on
cancelled masters.

**Why the judged arms feed it more.** The revised planner adds
`4 * (best shortlist read - routine read)`, clamped to ±1. This is a read
relative to the act's own writing, used to rank different acts. New signings
have the lowest routine read (mean .49) and fresh shortlists (.69), so they
average +.70 from this term, against +.22 for Stars. Big Five single intentions
for new signings rise from 539 (connected) to 681 (v5), while Established fall
from 252 to 203 and Declining from 63 to 34. New signings mostly have
nonpositive prior forecasts: about 60% of their prepared Big Five masters are
cancelled, against 1-4% for Stars and Superstars. Prepared masters with a
nonpositive forecast are cancelled 41% (connected) to 48% (v5) of the time,
against 8-12% for a positive one.

The gate predates this work. A telemetry rerun of the connected arm
(`label-issue-trace-connected`, byte-identical to v1-connected) shows the same
gate behind 347 of its 354 cancelled Big Five masters with refusals (3,689 Big
Five refusal weeks, against 4,987 in judged v5). The connected arm already loses
29.4% of its prepared Big Five masters, and the judged planner raises that to
34.5%.

Candidate fixes, not implemented:

1. At issue, judge a prepared master on its remaining costs (add back paid
   production), or apply the profitability check before paying for the session.
2. Score material attention on an absolute scale across acts, keeping the
   act-relative comparison for the hold decision within one act's project.

## Revision: judge a prepared master on its remaining costs

With `--label-project-judgement`, the issue gate now compares
`projectedOrphanSingleNet + singleProductionCost` against the marketing budget
for a prepared master (`IssueGateNet`). The forecast deducted that production
estimate, and the session has already been paid. Adding back exactly what was
deducted leaves the costs still to come; adding the actual bill would mix it into
a forecast built on the estimate. An unrecorded single still carries its
production, and a negative estimate earns no credit. The change is gated by the
project flag, so control and connected replays are unchanged.

`label-project-v6-judged`, seed 1001, 106 weeks:

| | v1 connected | v5 judged | v6 judged |
|---|---:|---:|---:|
| Big Five singles issued / cancelled | 819 / 394 | 760 / 468 | 1,112 / 100 |
| Big Five paid masters later cancelled | 370 (29.4%) | 445 (34.5%) | 72 (5.8%) |
| Big Five new-signing paid masters cancelled | 278 / 493 | 399 / 645 | 53 / 523 |
| Field paid masters later cancelled | 351 (5.5%) | 361 (5.7%) | 160 (2.6%) |
| Big Five Top 10 share | 34.1% | 31.8% | 33.7% |
| Studio Top 10 share | 12.4% | 7.2% | 8.8% |
| Field Top 10 share | 53.5% | 61.0% | 57.5% |
| Big Five unit share | 30.1% | 26.9% | 28.0% |
| Total single units | 296.3M | 294.2M | 296.3M |
| Top 10 units | 76.4M | 79.5M | 71.9M |
| Opening field MidTier closures | 15 | 12 | 14 |
| Big Five shelved paid production | $1.48M | $1.76M | $0.29M |
| Big Five end cash | $23.55M | $23.29M | $24.51M |
| Studio end cash | $2.95M | $1.89M | $2.57M |
| Field end cash | $27.93M | $30.51M | $27.75M |

The gate fix removes most of the paid-but-shelved waste. Big Five end cash rises
$1.22M over v5 (about 5%): record receipts rise $2.0M, and shelved production
falls $1.48M. Singles still behave as a fixed purse (total units are flat), so
the additional Big Five releases mostly take sales from field labels: field end
cash falls $2.76M and two more opening field MidTiers close. Top 10 units fall
from 79.5M to 71.9M as more records compete for the same demand.

Among retired programme singles, Big Five realized net is unchanged in total
($12.57M against $12.56M) across 822 retired singles, against 566. The marginal
releases are new signings: 349 retired, a mean realized net of $8.8k (against
$17.7k in v5) and 69% losing money. Those figures include the production that
shelving would have lost anyway, so releasing still beats shelving for the label.
The underlying planner bias remains: the act-relative material term still sends
the Big Five toward new signings, now 523 prepared masters against 493 in
connected.

Verification: build clean; 53 native repertoire assertions pass (one new for
`IssueGateNet`); the full-world programme-project roundtrip passes. One seed:
the Big Five, studio and field shares are within plausible seed noise; the paid-
master and cash rows are large, mechanical effects.

## Revision: material attention on one scale across acts

The planner's single ranking no longer compares an act's best heard song with its
own routine session. For each slot, it reads every eligible act's best heard
song and scores `4 * (best read - slot mean)`, clamped to ±1
(`MaterialAttention`, `SlotMaterialMean`). The mean counts only acts with heard
material, and an act with none scores -1. Hit urgency stays at +.75. The
act-relative routine comparison remains only in the hold review, where it asks
whether this act's project should wait for stronger material. Without the
project flag, the planner keeps its original lazy candidate evaluation.

`label-project-v7-judged`, seed 1001, 106 weeks:

| | v1 connected | v6 judged | v7 judged |
|---|---:|---:|---:|
| Big Five single intentions: new signing / Rising / Established | 539 / 331 / 252 | 568 / 335 / 252 | 505 / 350 / 272 |
| Big Five single intentions: Star / Superstar / Declining | 127 / 36 / 63 | 115 / 22 / 36 | 129 / 37 / 32 |
| Big Five singles issued / cancelled | 819 / 394 | 1,112 / 100 | 1,109 / 98 |
| Big Five paid masters later cancelled | 29.4% | 5.8% | 5.5% |
| Big Five realized net, retired programme singles | $14.56M | $12.57M | $14.55M |
| Big Five Top 10 share | 34.1% | 33.7% | 34.4% |
| Studio Top 10 share | 12.4% | 8.8% | 7.0% |
| Field Top 10 share | 53.5% | 57.5% | 58.6% |
| Big Five unit share | 30.1% | 28.0% | 30.7% |
| Big Five / field repeat Top 10 acts | 9 / 8.5 | 7 / 8 | 8 / 10 |
| Opening field MidTier closures | 15 | 14 | 12 |
| End cash: Big Five / studio / field | $23.55M / $2.95M / $27.93M | $24.51M / $2.57M / $27.75M | $24.20M / $1.82M / $29.06M |

The new-signing tilt is gone: Big Five new-signing intentions fall below the
connected arm's, and Stars and Superstars regain their slots. With the issue-gate
fix keeping waste low, Big Five realized net returns to the connected level
($14.55M across 817 retired programme singles), and Big Five unit share
slightly exceeds it. Field labels recover part of what v6 cost them: $1.3M more
end cash and two fewer opening MidTier closures. Big Five end cash is $0.3M below
v6 but still $0.65M above connected.

Studio Top 10 share and cash fall (12 labels, the noisiest group). Studio
repeat acts are 1.5 in every judged arm against 4.5 in connected. The studio row
is the next thing to check across seeds before attributing it to the project
rules.

Verification: build clean; 55 native repertoire assertions (two new for the
slot mean and material attention); scheduler 46, breakout planning 20 and
material 31 suites pass; the full-world programme-project roundtrip passes.
One seed.
