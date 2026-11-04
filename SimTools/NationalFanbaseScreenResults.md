# National fanbase: review candidate, 24 October 2026

The `.6` recent-hit support arm plus an earned returning audience is the current
review candidate. Keep it as an experiment while reviewing the careers and label
balance. The audience-only arm does not yet replace the `.6` support proxy. No
game defaults have changed.

## What was tested

Four arms used the same repair/planning bundle, seeds 1001, 2002 and 3003, and
106-week worlds: support off; `.6` support; audience alone; audience plus `.6`.
The audience settings were `.25` weekly trial ceiling, `.35` recruitment and a
104-week attachment half-life. These are explicit model assumptions, not measured
historical loyalty rates. This screen tests one audience setting; it does not
establish an optimal dose or a credible decade of careers.

Actual cleared AI single purchases can build a saved artist audience in regional
consumer segments. Later titles draw on an immutable snapshot of that audience,
with material response, finite title purchases, declining attention and competition
between concurrent releases. Returning buyers still need stock, shelf capacity and
money. A title cannot refill its own audience from its own success. Recognition
continues to represent broader familiarity.

The trace now retains unsuccessful follow-ups and their weekly demand stages.
Complete first-hit cohorts have a full 52 weeks of follow-up, including silent,
declining and unowned acts. Each entry-rate record has a complete 13-week window.
The table reports means of seed metrics; cohort membership can change between
worlds. Individual artists are not paired causal treatment effects.

## Completed three-seed results

| Field hit-act outcome | Support off | .6 support | Audience alone | .6 + audience |
|---|---:|---:|---:|---:|
| Later chart debuts per act, next 52 weeks | 1.48 | 1.61 | 1.58 | **2.01** |
| Eligible follow-ups entering within 13 weeks | 42.1% | 45.6% | 47.1% | **55.0%** |
| Acts with no later chart debut | 33.1% | 31.6% | 31.9% | **22.5%** |
| Hit milestone to next chart debut, weeks | 16.7 | 14.0 | 12.8 | **10.3** |
| Acts with a later Top 40 | 49.4% | 48.7% | 40.3% | **53.2%** |
| Top 10s per subsequent chart entry | 27.4% | 31.2% | 21.8% | 23.1% |
| Below Established at hit, Established+ at endpoint | 19.4% | 22.0% | 25.4% | **27.2%** |
| Recognition gain | .071 | .080 | .071 | **.089** |

The timing row is a mean of within-seed medians, measured from the hit milestone.
It is not the artist's single-to-single release interval. Likewise, the original
six-week next-release result was a milestone-to-release measure across hit acts,
not a six-week release schedule for one artist.

Entry improves in each seed versus `.6`: 42.9% to 46.9%, 46.4% to 59.5%, and
47.6% to 58.6%. Next-debut timing also improves in each. Promotion is less
consistent: the Established+ endpoint rate falls in seed 1001 while improving in
the other two. Three seeds support review of the mechanism, not a precise effect
estimate.

The blend fills more of the middle of the chart. Among eligible field follow-ups,
misses fall from 54.4% under `.6` to 45.0%; peaks 11–40 rise from 10.6% to 14.0%.
Top 10 peaks fall slightly, from 15.2% to 14.3%. This is a useful distinction:
more continuing chart careers do not require a higher Top 10 conversion rate.

Returning buyers account for 8.6% of first-13-week cleared units across the blend's
355 eligible field follow-ups (pooled). An act's estimated attached audience grows
from about 45,600 at its first observed hit to 109,500 after 52 weeks, on average.
These are overlapping audience equivalents, not identified unique people.
Twenty-five of 83 first-career-hit proxy acts lose at least 20% of their peak
audience stock. The model permits attachment to persist while chart success fades.

## A career worth looking at

The casebook selects the median starting-audience artist within stated outcome
buckets, including failures. The blend's successful example is seed 2002,
`artist_02593`: Rising at week 53, Established at 62, Star at 66, Established at 95.
Its subsequent singles peak **1, 1, 14, miss, 79, 30, miss** by the career endpoint.
The first two have material quality .785/.809; the later miss at .525 still sells
20,712 units in its first 13 weeks, including 14,319 attributed to returning buyers.
That is the useful behavior: a failed follow-up can retain a small audience without
automatically becoming a hit. Later records can recover.

This is one within-world sequence, not evidence that the same artist would have
failed under another setting. See the generated casebook for declining acts,
audience losses and active contract exits as well as success.

## Costs and remaining discrepancies

The total singles market barely moves: 296.0 million units under `.6` versus
296.8 million under the blend. Top 10 units fall from 88.8 to 86.5 million.
Opening field MidTier closures average 10.0 versus 9.3 out of 25. There is no
evidence here that the new audience is funded by a large expansion of spending.

Annual field acts with two or more Top 10s move from 9.5 to 8.2, near the recovered
historical 8.5. However, Big Five repeat acts rise from 8.0 to 11.0 against a
historical 6.0; studio repeat acts remain 2.5 against 5.0. Number-one turnover is
also too fast. The blend is promising for career continuity, not a finished chart
calibration. Historical charted sides and reconstructed owners differ from the
model's represented singles and firms; comparisons retain that caveat.

Careers still need more world interaction. This first writer covers AI singles;
player purchases, live/media recruitment, audience-aware bookings and programmer
loyalty are outside it. Longer runs, audience-strength/retention sensitivity and
label-specific opportunity readers remain worthwhile before adopting defaults.
An active artist losing a contract is kept separate from a band disbanding; neither
an improved title nor an unowned endpoint alone identifies the finance mechanism.

## Separate renewal-finance diagnosis

In the audience-only seed 1001 world, `artist_00602` reaches Established at week
48, has a Top 10 at week 51, then loses its contract at week 53. The exact
read-only decision audit confirms that the label wanted renewal. Cash was
$266,061.53 and the advance $3,604.25, but the full signing reserve was
$266,786.84. The going-concern tax horizon already used for its slate would leave
a $96,406.53 reserve requirement, with no tax due in the next three months at
that decision. The artist had about 102,188 attached audience equivalents.

This supports a specific finance experiment. The optional
`--hit-act-going-concern-renewal` gate permits only active AI MidTier labels
already using going-concern slate reserves, with nonnegative recent monthly
cash flow, a hit under the current contract and a Top 40 in the last 52 weeks.
It keeps operating, plant and slate holds, pays the actual advance and leaves
normal tax payments in place. The desire to renew is unchanged. There is no new
audience-to-status promotion rule. The diagnostic replay preserves all 66
checked economic/chart streams before activating the gate.

The active seed-1001 pilot completed 106 weeks with three additional renewals,
$10,105 in actual advances and all receipt/reserve checks passing. One affected
act, `artist_00810`, has the same observed pre-renewal release sequence in the
two blend worlds: miss, 12, 1, 33, 4. With the finance gate it subsequently
releases singles peaking 55 and 45 within its career window and ends Established;
the original blend ends that window Dropped with 13 active unowned weeks.
This illustrates a working opportunity channel, without guaranteeing another hit.

The aggregate pilot is mixed. Versus the original blend in this seed, entry rises
46.9% to 50.8%, new Established acts rise 5 to 7 and active unowned weeks per act
fall 3.26 to 2.41. But the share of field acts with a follow-up Top 40 falls
54.8% to 42.9%, the Established+ endpoint share falls 40.5% to 35.7%, and closures
remain 13 of 25. These are one-seed world outcomes, not isolated artist treatment
effects. The earlier exact diagnostic case belongs to the audience-only world;
the active policy is tested on the blend, where careers already differ.

**Decision:** stop for user review of the three-seed `.6 + audience` candidate.
Retain the optional finance gate as a diagnosed, functioning but unselected
pilot. Do not combine its single-seed figures with the three-seed table or treat
more promotions alone as proof of better careers. Finance evidence is under
`SimLogs/afb-v3-renew/review/`, including `renewals/renewal-review.md` and actual
decision/payment ledgers. No simulation processes remain running after this
screen; no defaults or commits were changed.

## Verification and reproducibility

The native checks pass: 231 audience/renewal assertions and 47 stocking assertions,
plus full-world JSON, real gzip and resumed play with live audiences and title
depletion. Twelve Python analysis tests pass. The zero-trial audience observation
matches the earlier `.6` world in 65 economic/chart streams through week 105.
The read-only renewal diagnostic matches the earlier audience-only world in 66
streams. These prove observational identity separately from audience accounting.

Generated evidence is under `SimLogs/afb-v2/review/`: `review.md`,
`chart-discrepancies.md`, `fanbase-review.md`, `fanbase-career-cases.md` and the
corresponding CSV/JSON ledgers. Source runs are `hfs-v2-off,hfs-v4-off`,
`hfs-v2-f0p6,hfs-v5-f0p6`, `afb-v1-fan,afb-v2-fan` and
`afb-v1-blend,afb-v2-blend`. Plans record arguments, seeds, assembly hashes and
native checks. The four analysis scripts can regenerate the review.

Implementation and assumptions: [NationalFanbaseInvestigation.md](NationalFanbaseInvestigation.md).
Runner and analysis commands: [README.md](README.md).

Continued label-balance investigation: [LabelBalanceInvestigation.md](LabelBalanceInvestigation.md).
It separates release opportunities, temporary finance refusals, firm-specific
outcomes and historical measurement sensitivities, then tests an isolated
spacing change on the existing blend.
