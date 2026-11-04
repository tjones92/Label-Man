# Exploratory fixed-chart sensitivity replay

This is a deliberately aggressive offline thought experiment on the three completed
104-week `plan-pilot-control-market` runs (seeds 1001, 2002, 3003). It changes no
simulation or gameplay setting. The source runs use the combined planned-assignment
and delivery-offer workflow, without reminders. This screen does not estimate the
causal effect of any implementable policy.

## Experiment

Freeze the actual launches, artist histories, weekly Top 100 membership and all
other world decisions. A follow-up is eligible if it launches within 52 weeks after
its act's first observed non-opening Top 40 entry and has one to three lifetime
Top 40 hits recorded at launch. Apply a 1.5x or 2x score to those titles on every
observed chart week. A separate arm halves the score of titles whose acts have at
least four lifetime Top 40 hits at launch. The combined arm applies both. These
are stress tests, not proposed literal bonuses or penalties.

The `records.csv` chart points do **not** reproduce the actual ordering (9,360,
9,340 and 9,338 of 10,400 rank positions differ). To make a no-change replay
reproduce the chart, sort each week's logged point values descending and assign
them to the observed rank order before applying multipliers. The resulting
rank-calibrated scores are artificial: their gaps are not verified chart margins.
Only actual Top 100 records compete. An off-chart title cannot enter, and a title
cannot remain after leaving the observed chart. Rank, act history, releases,
awareness, radio, market clearing and finances do not feed back into later weeks.
Consequently these numbers are sensitivity illustrations, not predicted charts,
conversion rates, economic outcomes, or evidence for a quality/demand parameter.

The cohort is frozen from the baseline: acts whose first observed non-opening
Top 40 entry has 52 weeks remaining. The table counts acts with 1–3 subsequent
new Top 40 titles in that window, using the original hit date even if a replay
would change the origin hit. Top-30 share is the top 30 acts' share of **Top 40
slot-weeks**, reranked separately per scenario; it is not the pilot report's Top
100 slot measure. The baseline cohort counts reproduce the pilot report.

| Seed | Cohort | 1–3 further hits: baseline → 1.5x → 2x | Top-30 Top 40 slot share: baseline → 1.5x → 2x |
|---|---:|---:|---:|
| 1001 | 113 | 44 → 57 → 60 | 53.3% → 53.1% → 52.5% |
| 2002 | 110 | 33 → 45 → 54 | 56.4% → 55.7% → 54.9% |
| 3003 | 111 | 40 → 50 → 63 | 53.7% → 52.5% → 51.5% |

The 1.5x arm replaces 187, 149 and 179 of 4,160 Top 40 slot-weeks. The
half-score established-act arm replaces 548, 595 and 597 slots and moves the
same middle counts to 51, 40 and 50; its top-30 share becomes 46.3%, 46.5%
and 44.8%. These larger concentration shifts are mechanically driven by the
strong penalty and assume that existing Top 100 competitors fill all vacated
slots. The combined arm moves the middle counts to 55, 51 and 64; overlap and
competition mean combined outcomes need not be the sum of the separate arms.

## Interpretation and next experiment

There is room in the observed chart population for a strong follow-up advantage
to move the complete-cohort measure in each seed, while a much larger direct
restriction on established acts is needed to move this particular concentration
measure substantially. This is a property of the frozen charts under artificial
scores, not a finding that follow-up support or incumbent restriction would work
in the live model. It also says nothing about follow-ups that never reached the
observed Top 100, a potentially important blind spot.

A productive live test would isolate a plausible follow-up mechanism, such as
continuity of professional material or a separately measured post-hit audience
response, with a deliberately large opt-in treatment and matched controls. Track
first-hit cohort conversion, fixed-N Top 100 and Top 40 shares, release volume,
label economics and the lower-chart population; retain ordinary selection and
compare multiple seeds. The exact replay multipliers should not be transplanted
into gameplay. Full-world reruns and later-era holdouts are necessary to learn
whether any real mechanism survives feedback and has acceptable side effects.

## Reproduction

```
python SimTools/replay_writing_chart_sensitivity.py plan-pilot-control-market-1001 plan-pilot-control-market-2002 plan-pilot-control-market-3003
```

The script checks 104 complete chart weeks, 100 unique chart positions per week
by count, and zero baseline Top 40 replacements. Results are saved as
`SimLogs/writing-calibration/chart-sensitivity-replay.json`. The baseline
one-to-three cohort counts were compared with the pilot report. The raw scoring
mismatch is recorded in the JSON; no game build or full-world replay was run.
