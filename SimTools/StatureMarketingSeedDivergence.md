# Stature marketing: seed disagreement and next experiments

The completed fixed-cohort trajectory audit, offline following-magnitude screen, prewarm assessment and short half/full experiment are in [StatureMarketingMechanismReview.md](StatureMarketingMechanismReview.md).

The decade results support keeping the reader experimental. Seed 2002 moves toward the historical cohort benchmarks; seed 1001 moves away in 1964 and is essentially unchanged on the broad 1968 cohort measure. This is a result about cohort composition, not proof that every economic or career outcome improves in 2002.

## What the follow-up measurements show

In 1964, the 1960 cohort gains 42 Top 10 slot-weeks in seed 1001 and loses 55 in seed 2002. There are only 520 Top 10 slots per year, so a small number of long-running records can materially change the share.

Two opening acts in seed 1001 account for gross gains of 43 slot-weeks: `artist_01672` has 3 → 25 weeks and `artist_00007` has 2 → 23. Other old acts lose weeks, including `artist_02638` at 23 → 1. Thus a handful of winners and losers can exceed the net cohort movement. In seed 2002, `artist_01538` falls from 27 → 4 weeks; several other old acts lose representation while others gain it. These are descriptive differences between divergent worlds, not isolated causal effects on those acts.

The marketing traces do not show a simple blanket cut to old acts in seed 2002. Among 1964 single-launch budget reads for the retrospectively identified 1960 cohort, the treatment increases 97 and reduces 71 of 368 reads in seed 2002. Seed 1001 increases 94 and reduces 49 of 362 reads. The reader still uplifts more old-cohort launches than it cuts in both worlds. These are pre-cap requested envelopes, not paid campaign dollars. Cohort assignment here uses the whole observed run and is descriptive, not information available to a launch decision.

First-observed chart cohorts also change between arms: acts can first chart in different years or never chart in one world. This is part of the economic outcome, but makes an arm-specific cohort share insufficient to diagnose a treatment on a fixed group of artists. Generated runtime IDs cannot automatically be treated as the same underlying act after worlds diverge. Follow-up trajectories should first use verified common opening acts; runtime acts should be compared by formation cohorts or distributions.

The full decomposition and selected slot-week winners/losers are saved in `SimLogs/writing-calibration/stature-marketing-decade-v2-cohort-explanation.json`. Its same-ID cohort decomposition is accounting, not a claim of matched runtime identities or causal attribution.

## Mechanism consistent with the evidence, not yet established

The intervention substitutes audience-derived standing for the legacy career rung when requesting marketing budgets. The standing implementation supports sales, regional response and following; following contributes to both the record and conversion routes, and the strongest supported route can determine a band. The calibration uses lagged top-20 peer references and permits large rung mismatches. It does not explicitly target young acts or cohort turnover.

A durable incumbent can therefore receive a higher marketing envelope even after the legacy chart-based career rung cools. A later entrant can also gain an uplift. Which acts get the useful exposure depends on the seed's audience distribution, available songs, release dates, label funding and competing releases. Changed decisions then alter later state and RNG-sensitive decisions even with the same initial seed. The strong upward asymmetry (82–84% of changes) and divergence in spending, releases and chart allocation are consistent with amplification of different acts in each world. They do not identify following feedback, campaign spend, or any single route as the cause of the cohort split.

## Recommended order

1. **Audit the existing winners and losers before another decade.** For the leading common opening acts, trace standing components/route, legacy rung, requested and actually paid marketing, release opportunities, following and subsequent response. Include control-only successes and treatment-only successes. Check whether gains follow follower-led support, new observed demand, or an earlier unrelated scheduling divergence. Supplement chart cohorts with fixed opening-act and formation-year cohorts.
2. **Test treatment intensity separately from the evidence model.** A candidate half-strength reader, `legacy + 0.5 * (standingBudget - legacy)`, preserves the legacy value whenever both ranks agree and retains upward and downward changes. Compare it with the current full-strength reader on a frozen, shared starting world in short paired windows. This tests whether large budget substitutions drive instability; it does not guarantee spending neutrality or historical improvement. The factor is a proposed experimental setting, not a selected calibration.
3. **If trajectories implicate following, test that channel narrowly.** First compute an offline shadow sensitivity that discounts attributable follower-origin evidence, clearly labeling any upper-bound proxy. Hold actual follower demand, live draw and programme audience behavior fixed. If warranted, follow with an isolated causal standing-input screen. Turning off the entire follower feature would change several mechanisms and would not answer this question.
4. **Validate the chosen candidate on additional prespecified seeds.** Freeze mapping and evaluation metrics before seeing those results. Track both 1964 and 1968 cohort mix, fixed-lookback repeat hits, entrant counts, LP volume, paid spending and label populations. Avoid selecting a policy because it makes seed 2002 look good.

The cheapest useful next step is item 1 using existing artifacts. A short mid-decade replay starting from identical saved state can then distinguish immediate decision effects more cleanly than comparing worlds that have already diverged for eight years. Verify matching save-state/RNG restoration before relying on that design; do not claim such bookmarks already exist for this batch.
