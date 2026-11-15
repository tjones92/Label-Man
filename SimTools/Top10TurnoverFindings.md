# Top 10 turnover on the reference bundle (2026-11-14)

Question (from `FollowUpGapFableHandoff.md` §7): why does the reference bundle turn the Top 10 over too fast, and does that
inflate the Big-5's count of hit acts and its Top 10 share? Analysis only, on existing runs; no simulation was run.

Runs: `fit208-v1-{2002,1001}-base` (control), `-lw0up1`, `fit208-v3-*-lw0up1_ao2`, `fit208-v4-*-lw0up1_ao2_lh1`,
`fit208-v5-*-lw0up1_ao2_lh1_he095`; 208 weeks, two seeds. History: `historical-hot100-1960-64/hot100_weekly.csv`, cut
to the same 209 chart weeks (from 1959-12-28), labels from `records_<y>_grouped.csv`.

Scorers (all in `SimTools/`, outputs in `SimLogs/top10-turnover-*.txt`, `top10-climb-decomposition-v1.txt`):
- `analyze_top10_turnover.py [--owner] [name=fmt…]`: count, length, peak bands, moves at #4-10, short runs by tier, new
  Top 10 acts.
- `analyze_top10_turnover_replay.py [name=fmt…]`: static re-rank of each week from the logged rows, with the inertia cap
  re-implemented. The identity replay reproduces the published Top 10 at 99.6-100% same rank and 100% same membership.
  It then varies one term at a time.
- `analyze_top10_climb_decomposition.py`: splits a climbing week's demand growth into its terms (needs
  `single-demand-stages.csv`, which covers ~75% of Top 10 records).

**Definition.** History's Big-5 here is the *producing label* (Columbia, RCA Victor, Capitol, Decca, Mercury). That
reproduces the author's targets exactly: 20.6 / 21.2 / 26.0 for 1961-63. Owner grouping (`HISTORY_BIG5`, used by the
selection scorer) reads 22.1 / 26.0 / 38.8, because it adds Epic, Smash and the other imprints. The sim's
`label_0001-0005` are the flagships, so producing label is the like-for-like measure.

## Answer in brief

1. **The excess is one quantity.** It shows as 129 Top 10 records a year against 107, or 4.1 weeks each against 4.9.
   - It is spread over every peak band; band shares match history.
   - Weeks both before and after the peak are short.
   - It is not chart supply. The sim has *fewer* Hot 100 and Top 40 records than history, but 45-47% of its Top 40
     records go on to the Top 10, against 36%.
   - Records climb 19-28% faster per week at every level of the chart.
2. **Flags.**
   - `--chart-inertia-base=0.4` is the one chart-side bundle flag that carries a measurable part: 7-9 of the ~22 extra
     records a year (static; it matches October's 260-week dynamic measurement of +7).
   - The ration exponent carries none: it is order-preserving on units, and g = 1 makes turnover slightly *faster*.
   - Neither does airplay convexity.
   - ~~Post-peak decay is not in the bundle.~~ **Wrong — see §10.** `--lp-retail-v2-profile` expands to a profile that
     runs `--post-peak-demand-decay=0.8 --post-peak-demand-grace=1 --post-peak-demand-decay-top=0.9`, and it is the
     largest turnover lever found.
   - The fit shape is not the cause. The control's shape is the fastest (129), LW 0 + upside 1 slows it to 115, and
     the follow-up arms (ao2, he095) add it back to 129.
   - The remaining ~8-10 records a year is trajectory shape: Top 10 visits are too pointed. A 3-week smoothing of units
     and airplay removes 7-10 records a year. With existing runs it can't be pinned to a flag, and it predates the
     November flags (an October 27 run was already fast).
3. **Big-5: turnover does not inflate the share or the hit-act count.**
   - The Big-5 share excess is *count*: 41.9 Big-5 Top 10 records a year against 24.2. Each runs *shorter* than
     history's (4.06 weeks against 5.39).
   - Every turnover counterfactual leaves Big-5 share within ±1 point.
   - Big-5 new Top 10 acts run about 2× history (16.5-18.0 a year against 8.7) in every arm, including the slowest.
   - Short runs do concentrate in Big-5 records under ao2 and he095. So a fix that lengthened runs without cutting
     Big-5 entries would *raise* Big-5 share.

## 1. Count, length, Big-5 (two seeds pooled; per seed-year 1960-63; shares 1961-63)

| | Top 10 records/yr | weeks | Big-5 weeks | field weeks | one-week % | Big-5 records/yr | Big-5 share 61 / 62 / 63 |
|---|---|---|---|---|---|---|---|
| history | 106.8 | 4.91 | 5.39 | 4.77 | 11.1 | 24.2 | 20.6 / 21.2 / 26.0 |
| control | 129.2 | 4.11 | 4.06 | 4.14 | 15.2 | 41.9 | 29.4 / 37.1 / 38.6 |
| lw0up1 | 115.0 | 4.64 | 4.60 | 4.65 | 14.6 | 27.1 | 23.8 / 24.7 / 26.3 |
| ao2 | 121.1 | 4.38 | 3.92 | 4.60 | 14.0 | 39.1 | 34.2 / 26.1 / 34.0 |
| lh1 | 121.2 | 4.39 | 4.10 | 4.49 | 15.3 | 32.6 | 28.2 / 26.4 / 26.2 |
| he095 | 129.0 | 4.11 | 3.87 | 4.21 | 16.9 | 36.6 | 26.3 / 31.6 / 30.0 |

Returns (records with two or more Top 10 spells) are 3.5-6.5% against 3.8%, so they are not part of the miss.

## 2. Peak-band shape (uncensored Top 10 records)

| band | share % hist / ctl / he095 | Top 10 weeks hist / ctl / lw0up1 / he095 | weeks entry→peak hist / ctl / he095 | weeks peak→exit hist / ctl / he095 |
|---|---|---|---|---|
| #1-3 | 39.8 / 41.6 / 37.7 | 7.39 / 6.15 / 7.42 / 6.55 | 2.47 / 1.95 / 2.01 | 4.28 / 3.21 / 3.54 |
| #4-6 | 26.2 / 29.2 / 30.2 | 4.46 / 3.48 / 4.03 / 3.57 | 1.78 / 1.03 / 1.20 | 1.70 / 1.50 / 1.44 |
| #7-10 | 34.0 / 29.2 / 32.1 | 2.36 / 1.84 / 2.02 / 1.76 | 0.60 / 0.36 / 0.31 | 0.87 / 0.64 / 0.54 |

- **Every band is short**, and the shortfall is about equal before and after the peak (~0.4-0.5 weeks each across the
  mix).
- One-week visits by records that peak at #7-10 are 48-49% against 33%.
- Fit shape matters for the #1-3 band only: lw0up1 matches history there (7.42), while the control and he095 lose
  0.9-1.2 weeks. #4-10 is short in every arm.
- On the Hot 100 itself, Top 10 records live as long as history's (14-16 weeks). They climb a week faster (debut to
  peak, median 6 against 7; spread sd 1.8-1.9 against 1.91), then linger longer below the Top 10 (peak to exit, 7
  against 6).

## 3. Moves: climbs are fast; falls in the Top 10 are about right

Mean weekly rank change (rises exclude exits, falls exclude drops of 60 or more ranks):

| | rise #1-10 | rise #11-20 | rise #21-40 | rise #41-100 | fall #1-10 | fall #11-20 | fall #21-40 | fall #41-100 |
|---|---|---|---|---|---|---|---|---|
| history | −2.14 | −4.41 | −6.94 | −11.15 | 4.17 | 8.29 | 12.52 | 20.53 |
| control | −2.55 | −5.36 | −8.89 | −13.77 | 4.45 | 7.21 | 10.73 | 14.19 |
| lw0up1 | −2.29 | −5.01 | −8.19 | −13.69 | 3.98 | 6.49 | 9.52 | 13.42 |
| he095 | −2.41 | −5.41 | −8.54 | −13.87 | 4.35 | 7.19 | 11.23 | 14.91 |

| per year | Hot 100 records | Top 40 | Top 10 | Top 10 / Top 40 |
|---|---|---|---|---|
| history | 678 | 298 | 106.8 | 0.36 |
| control | 604 | 277 | 129.2 | 0.47 |
| lw0up1 | 622 | 256 | 115.0 | 0.45 |
| he095 | 621 | 284 | 129.0 | 0.45 |

Post-peak moves at #4-10 (history / control / he095):
- mean fall 4.48 / 4.64 / 4.29;
- exit next week 55 / 59 / 55%;
- exit from the peak week 31 / 36 / 39%.

Rank spacing at #4-20 matches Hesbacher, so spacing is not the cause. Only #1 sits further out (2.57-2.69 times #10,
against 1.91).

## 4. What carries a climb (`analyze_top10_climb_decomposition.py`)

Median weekly log change, for climbing weeks at #11-40 (control, two seeds, n = 787):

| raw demand | aware buyers | conversion | rank-keyed exposure | clearing | units | airplay |
|---|---|---|---|---|---|---|
| +0.204 | +0.029 | +0.156 | +0.173 | +0.073 | +0.267 | +0.758 |

- **The rank-keyed purchase exposure** (`ChartSimulator.GetChartExposureWeight`, Hesbacher^0.62, flat inside the Top
  10) carries ~85% of a climbing record's demand growth. It reads last week's position, so a climb feeds itself until
  #10.
- Inside the Top 10, conversion falls about 0.21 a week. The age decay (×0.91 a week after week 8) is about half of
  that.
- 70-77% of Top 10 records peak at age 8-10 weeks.
- **But the exposure loop does not set the count.** Closing it in the replay (units × (w′/w)^1.35, read from the
  replay's own positions):
  - a softer curve, E 0.45, gives 129.7 / 130.7 Top 10 records a year (control / he095);
  - E 0.30 gives 132.4 / 133.7;
  - exposure lagged to the slower of two weeks gives 129.3 / 129.0;
  - the mean of two weeks gives 127.9 / 127.0.

  The loop drives every record alike, so it changes how fast records move, not how many reach the Top 10.

## 5. Static counterfactuals (`analyze_top10_turnover_replay.py`; mean of two seeds)

| replay | control: Top 10/yr | weeks | Big-5 share 61-63 | Big-5 new acts/yr | he095: Top 10/yr | weeks | Big-5 share | Big-5 new acts |
|---|---|---|---|---|---|---|---|---|
| published (= identity) | 129.3 | 4.10 | 35.1 | 17.7 | 129.0 | 4.10 | 29.4 | 16.5 |
| inertia base 0.8 (authored) | 122.0 | 4.34 | 35.9 | 16.7 | 120.2 | 4.40 | 29.9 | 16.7 |
| inertia base 0 | 131.7 | 4.03 | 34.9 | 18.0 | 130.5 | 4.04 | 29.3 | 16.8 |
| ration g 1.0 | 133.3 | 3.97 | 33.6 | 17.5 | 132.8 | 3.98 | 28.9 | 17.2 |
| airplay ^3 instead of ^5 | 130.5 | 4.06 | 36.5 | 18.4 | 127.8 | 4.14 | 30.3 | 17.0 |
| airplay linear | 129.0 | 4.10 | 38.0 | 18.7 | 124.2 | 4.24 | 31.9 | 17.3 |
| units, 3-week centred mean | 126.4 | 4.19 | 34.9 | 16.8 | 123.8 | 4.27 | 29.1 | 17.5 |
| units + airplay, 3-week mean | 122.3 | 4.31 | 35.2 | 17.0 | 119.2 | 4.43 | 28.8 | 17.2 |

Static means no feedback through the radio position bonus, awareness floors, momentum or exhaustion. The inertia result
agrees with October's dynamic measurement (B 0.8 → 0.4: +7 records a year over 260 weeks,
`top10-duration-is-rank-spacing`).

## 6. Since October

- Old chart files survive only for two 52-week runs off the reference bundle. Their 1960 numbers:
  - history: 104 Top 10 records at 5.29 weeks, 10.7% one-week;
  - `ref-c566f24` (Oct 14, lifecycle flags only): 97 / 5.53 / 6.9%;
  - `hs52op-v1` (Oct 27, game profile, 5 seeds): 119.6 / 4.42 / 9.9%;
  - fit208 control: 126.5 / 4.41 / 13.6%;
  - the 104-week `fund-v1-*-base` (seeds 3003 and 6006): 133 / 4.25 / 17.7%.
- So run length had already fallen to today's level by October 27. Between then and now, one-week visits rose.
- `--chart-inertia-base=0.4` is in today's default profile (`SimulationArguments.cs`, made default in `bbd7858`,
  2026-11-04). It was adopted in October when runs were too long, and now overshoots.
- None of the other candidate flags is a cause:
  - the ration exponent is order-preserving;
  - the rotation ceiling at 0.85 lengthens stays if anything (the October probe);
  - post-peak decay is not in the bundle;
  - the fit shape moves turnover in both directions.
- The flags of the October 27 run are not recorded, so the October-to-now change in trajectory shape can't be
  attributed further without new runs.

## 7. Big-5

- **Share = count × length.**
  - History's Big-5: 24.2 records a year × 5.39 weeks, a 13% premium over the field (4.77).
  - The control's: 41.9 × 4.06, no premium over the field (4.14).
  - he095's: 36.6 × 3.87, *below* the field (4.21).
  - The share excess is all count. Length works the other way.
- **Short runs concentrate in Big-5 records under ao2 and he095.**
  - Big-5 one-week share: 16.2% / 19.8% (ao2 / he095), against field 13.0% / 15.7%.
  - Big-5 hold 33-37% of one-week Top 10s, against 28-32% of all Top 10 records.
  - History's Big-5 were under-represented among one-week visits: 16% of them, against 23% of records.
  - Sim Big-5 records reach the Top 10 at the margin (entry edge) without the staying power.
- **Turnover does not inflate the Big-5 share or hit-act count beyond its own proportion.**
  - Every slower-turnover counterfactual leaves Big-5 share within −1.5 to +0.9 points.
  - It removes ≤ 1.2 Big-5 new acts a year.
  - Big-5 new Top 10 acts (first Top 10 in the window, 1961-63) are 16.5-18.0 a year in every arm, against history's
    8.7. That includes lw0up1, the slowest arm (115 records a year). Field new acts: 46-50 in the control and he095
    against 51; 65.5 in lw0up1.
- **Implication.** A turnover fix that only lengthens runs would give the Big-5 history's run length on their present
  count, and push Big-5 share *up*. The Big-5 lever remains entries (`big5-entry-edge-is-campaign-impact`).

## 9. Screen result: inertia 0.8 on he095 — rejected (2026-11-14)

Runs: `inr208-v1-{2002,1001}-lw0up1_ao2_lh1_he095`, 208 weeks.
- Reference `SimLogs/writing-calibration/inertia08-bundle-request.json` is `major-scale-bundle-request.json` with only
  `--chart-inertia-base` 0.4 → 0.8. The request diff against `fit208-v5` is that flag alone.
- The DLL was rebuilt after `fit208-v5` ran. A 27-week `he095` run on the new DLL (`inr27-off-2002-…`) matches
  `fit208-v5-2002` byte for byte (3,228 lines), so `fit208-v5` stays the control.
- Outputs: `SimLogs/inr208-v1-{turnover,fitshape,selection,carriers}.txt`.

| | history | he095 (0.4) | inertia 0.8 |
|---|---|---|---|
| Top 10 records/yr | 106.8 | 129.0 | **114.1** |
| weeks each (Big-5 / field) | 4.91 (5.39 / 4.77) | 4.11 (3.87 / 4.21) | **4.68** (4.57 / 4.74) |
| one-week % | 11.1 | 16.9 | **12.5** |
| Big-5 Top 10 share 1961 / 62 / 63 | 20.6 / 21.2 / 26.0 | 26.3 / 31.6 / 30.0 | **35.0 / 35.8 / 38.0** |
| Big-5 / field Top 10 records (two seeds, 1960-63, uncensored) | — | 278 / 699 | 296 / **562** |
| MidTier Top 10 records | — | 261 | **157** |
| #1 records/yr, tenure, returns % | 20.0, 2.61, 3.8 | 21.4, 2.44, 6.4 | 20.6, 2.53, **12.1** |
| #1 median lifetime units (M), 1961 / 62 / 63 | (target 0.80) | 0.97 / 1.01 / 0.90 | **1.26 / 1.17 / 1.17** |
| Top 40 records/yr | 298 | 284 | **267** |
| hit-act follow-ups, Top 40 % (Big-5 / MidTier / Independent) | — | 27.1 / 27.0 / 17.7 | 25.5 / **20.3 / 15.8** |
| field hit acts: chart events per act, Top 10 per debut % | 1.82, 17.5 | 1.39, 23.0 | 1.41, 17.0 |

- **Turnover moved most of the way to history.** Top 10 records fell 15 a year, against the static forecast of 9.
  Peak-band weeks rose in every band: #1-3 7.27, #4-6 3.91, #7-10 1.95 (history 7.39 / 4.46 / 2.36).
- **The static replay was wrong on Big-5 share.** It forecast flat; the run gave +5 to +8 points, up in 5 of 6
  seed-years (per seed 1961 31.0 → 32.9 and 21.7 → 37.1; 1962 28.3 → 32.5 and 35.0 → 39.0; 1963 28.1 → 44.4 and
  31.9 → 31.5).
- **The whole cut in Top 10 records fell on the field** (−20%, MidTiers −40%). Big-5 records rose and ran 0.7 weeks
  longer: Big-5 got history's run length on a count already 1.6× history's, which is the §7 warning.
- **Why the replay missed it** (inference): the replay holds the logged demand fixed. In the run, a field record held
  out of the Top 10 by an inertia-held incumbent also loses what Top 10 entry pays (the 0.7 awareness floor, the radio
  position bonus, plateau exposure). Big-5 records carry more awareness already (0.55 against 0.40 for an
  Independent's hit-act follow-up), so they need that entry pay-off less.
- **The other guards fail too.** #1 returns double (12.1% against history's 3.8%); #1 sales move further from the
  author's 0.80M; field follow-ups convert less (the follow-up gap widens); Top 40 records fall 6%.

**Verdict:** reject. Inertia is the wrong lever for turnover on this bundle, because it lengthens the Big-5's runs.
Any turnover fix has to shorten Big-5 runs less than field runs, or come with a cut in Big-5 entries. The open
remainder is the trajectory shape (§2, §5). Next is a dynamic look at what makes Top 10 visits pointed, keeping the
inertia base at 0.4.

## 8. Proposed screen (run; result in §9)

`--chart-inertia-base=0.8` on top of `he095`: an existing switch, so no code change. Two seeds × 208 weeks, about
15 minutes each, against `fit208-v5-*`. Score with `analyze_top10_turnover.py` and
`analyze_hitact_selection.py` / `analyze_followup_gap_carriers.py`.

- **Expect** (static): Top 10 records 129 → ~120 a year, 4.10 → ~4.40 weeks, one-week 16.9 → ~15%, Big-5 share flat.
- **Guards:**
  - #1 returns: October found 0.4 improved the #1 guards.
  - Top 40 entries: October's 0.4 raised them 9%. The follow-up gap is field Hot 100 entry, so watch field entry and
    events per hit act.
  - Hot 100 life.
- **It closes about a third.** The rest (trajectory shape) needs a dynamic diagnosis. The first candidates are the
  units side of the post-peak fall inside the Top 10, and the release-ramp-to-age-decay window.

## 10. Trajectory shape: dynamic screens (2026-11-14)

All arms are `he095` (`lw0up1_ao2_lh1_he095`) plus one change, 208 weeks, launched with `run_indie_access_screen.ps1`
(`-Extra` for switches). Two seeds (2002, 1001) unless marked; the four-seed rows add 3003 and 6006
(`fit208-v5-{3003,6006}-…` are new he095 controls on the probe-built DLL, which is byte-identical when the probes are off:
a 27-week `tsoff-v1-2002` run matches `fit208-v5-2002` over 3,228 record lines and 7,036 demand-stage lines).

### 10.1 What the bundle actually runs

`--lp-retail-v2-profile` expands `SimulationArguments.PreviousProfile`; an explicit flag overrides a profile flag only
by key. So the reference bundle also runs, among others:
- `--post-peak-demand-decay=0.8 --post-peak-demand-grace=1 --post-peak-demand-decay-top=0.9` (October's tail, top rank 3);
- `--chart-exposure-plateau-rank=20` (rank exposure is flat through #20, not #10; §4's exposure term assumed #10);
- `--radio-position-bonus-scale=0` (a `=0` arm reproduced the control byte for byte);
- `--hit-act-hype=0.3`.
Read the profile expansion before calling any flag off.

### 10.2 The shape miss, on rank alone

Rank-implied log points (Hesbacher of the published position), Top 10 records aligned on their peak:
- Hot 100 timing per peak band matches history: debut → peak 6.3-6.7 weeks against 6.1-7.4.
- The miss is vertical. The last two weeks of the climb run 0.27-0.29 a week against 0.17-0.20; the first four weeks
  after the peak fall 0.16 / 0.19 / 0.29 / 0.34 against 0.12 / 0.18 / 0.24 / 0.27.
- Too few records stall below the Top 10: 64-77 a year peak at #21-40 against 111.
- Sim climbs are *jerkier* than history's, not smoother: 9-15% of pre-peak weeks lose ground against 1.6-3.5%.
  Mid-climb weekly growth varies a lot around its age mean (sd 0.34 in log) but with no persistence (lag-1 correlation
  0.03). 65% of Top 10 records peak at release age 8-10.
- The published chart is far steeper than Hesbacher below #20: #10 / #100 = 29× against 11×.

### 10.3 What carries a climb (observation run `tobs-v1-2002`, `--local-consumption-observe`)

Into a #1-3 peak, weekly intent growth (weeks −3..0: 0.67, 0.51, 0.33, 0.06) is spread across base awareness
(+0.11 to +0.16), radio familiarity (+0.08 to +0.15; the multiplier runs 1.1 → 2.5 as listener airplay climbs 0.03 → 0.5)
and conversion (+0.40 → −0.05). Radio contact and peer word of mouth saturate awareness at ~1 by week −3. No single term
carries the approach; the terms share one radio/age clock and stack, and the singles ration raises the result to 1.35.

### 10.4 Screens

Two seeds:

| arm | Top 10/yr | weeks | one-week % | #21-40 peakers/yr | approach (wk −1, 0) | Top 40/yr | #1 returns % | #10/#100 |
|---|---|---|---|---|---|---|---|---|
| history | 106.8 | 4.89 | 11.1 | 111 | 0.20 / 0.17 | 298 | 3.8 | 11× |
| he095 | 129.0 | 4.05 | 16.8 | 77 | 0.29 / 0.27 | 284 | 6.4 | 29× |
| ration g = 1 | 124.5 | 4.20 | 15.1 | 70 | 0.27 / 0.25 | 276 | 5.1 | 14× |
| airplay convexity 3 | 125.6 | 4.16 | 17.6 | 73 | 0.27 / 0.29 | 281 | 14.1 | 22× |
| appeal drift 0.25 | 131.0 | 3.99 | 16.8 | 78 | 0.30 / 0.27 | 294 | 12.5 | 30× |
| label hold capacity 1 | 127.1 | 4.11 | 15.6 | 75 | 0.29 / 0.28 | 282 | 11.2 | 29× |
| tail 0.9, top 0.95 (all) | 109.0 | 4.79 | 13.2 | 69 | 0.24 / 0.24 | 250 | 11.4 | 31× |
| tail 0.99 (≈ off) | 95.5 | 5.47 | 11.7 | 59 | 0.20 / 0.23 | 215 | 12.1 | 30× |
| tail top 0.95, top rank 10 | 109.0 | 4.80 | 15.8 | 73 | 0.26 / 0.24 | 269 | 10.3 | 31× |
| tail top 0.92, top rank 10 | 115.0 | 4.55 | 15.5 | 79 | 0.26 / 0.24 | 276 | 6.5 | 29× |

New probes (off by default, byte-identical off): `--airplay-convexity=K`, `--demand-age-decay=R`,
`--radio-fatigue-decay=F`, `--survey-noise-scale=X` (`Systems/TrajectoryShapeProbe.cs`) and `--single-appeal-drift=S`
(`Systems/SingleAppealDrift.cs`, a keyed AR(1) per-record fortune; diagnostic only — in concept it double-counts the
breakout, TV and DJ mechanics, so it is not a candidate).

- **Airplay convexity is not the approach.** K = 3 leaves the approach where it was, compresses the top and doubles
  #1 returns. Rejected.
- **Persistent per-record fortune does not create stalls.** Drift 0.25 adds churn (faster falls, #1 returns 12.5%)
  and no #21-40 stalls: rank gates turn any swing into more movement through the thresholds. Rejected.
- **The ration exponent** is most of the steep chart bottom (29× → 14×) and buys −4.5 Top 10 records a year, but 1.35
  was adopted for #1 sales size.
- **The post-peak tail is the main lever.** It also slows the approach: the decay runs on each region's own sales peak
  with a one-week grace, so local sales flicker trips it mid-climb. One rate cannot fit both ends: softening it for all
  records lengthens every record's whole life (Top 40 records −34 a year, Hot 100 life of Top 10 records +2.5 weeks).
  Softening it only for Top 10 peakers (`--post-peak-demand-top-rank=10`, #1 at T interpolated to 0.8 at #11) keeps
  most of the lower chart.

### 10.5 Four seeds: top rank 10, top 0.95 against he095

| | history | he095 | top 0.95, rank 10 |
|---|---|---|---|
| Top 10 records/yr | 106.8 | 126.6 | **110.3** |
| weeks each (Big-5 / field) | 4.89 (5.48 / 4.72) | 4.13 (3.95 / 4.20) | **4.74** (4.30 / 4.93) |
| one-week % | 11.1 | 17.3 | 16.3 |
| approach (wk −1, 0) / fall (wk 1-4) | 0.20, 0.17 / .12 .18 .24 .27 | 0.28, 0.26 / .15 .19 .28 .33 | 0.25, 0.24 / .14 .15 .22 .27 |
| Big-5 / field Top 10 records/yr | 24.2 / 82.5 | 37.4 / 89.1 | 33.2 / 77.1 |
| Big-5 share 1961 / 62 / 63 | 20.6 / 21.2 / 26.0 | 31.4 / 33.0 / 31.1 | 31.4 / 29.2 / 29.5 |
| Big-5 share 1961-63 per seed (2002 / 1001 / 3003 / 6006) | — | 29.1 / 29.6 / 35.4 / 33.3 | 24.1 / 27.2 / 37.4 / 31.5 |
| Top 40 records/yr | 298 | 281 | 267 |
| Hot 100 life of Top 10 records | 14.4 | 14.9 | 16.7 |
| #1 returns % | 3.8 | 7.3 | 12.2 |

- **Turnover is nearly fixed and Big-5 share does not rise** (−2 points on average, 3 of 4 seeds down). This is the
  difference from inertia 0.8 (§9), which raised it on both seeds: here field runs lengthen more than Big-5 runs.
- **Open costs:** #1 returns rise to 12% (history 3.8); Top 40 records fall 5%; Top 10 records live 1.8 weeks longer on
  the Hot 100. At two seeds, top 0.92 kept #1 returns at the control's 6.5% for 115 records a year.
- **Next:** top 0.92-0.93 at four seeds to trade turnover against #1 returns; then a look at what makes former #1s
  retake the top (survey noise at the top, the #1/#2 spacing) as a companion. The approach is still ~1.4× history's
  in its last two weeks; the remaining carriers are the stacked radio/age terms of §10.3.

## 11. #1 returns (2026-11-14)

Question: the Top-10-only tail (§10.5) fixed turnover but raised #1 returns from ~7% to 12% (history 3.8%). Why?
Analysis only, on existing runs. Scorer `analyze_no1_returns.py` (output `SimLogs/no1-returns-v1.txt`):
1. spells and returns;
2. every #1 handover A → B, split by whether B was ahead of A on `unsampledChartPoints` (the true score) in its first
   week at #1;
3. a static replay of the chart top under redrawn survey noise.

The logged published points reproduce the published #1 sequence exactly. The replay is valid for the #1 slot because
nothing on the bundle pays #1 over #2: exposure is flat to #20 and the radio position bonus is 0. Its Top 10 columns
ignore the #10 awareness floor and the inertia cap (the identity gives 127.6 against 129 published on he095, and is far
off under inertia 0.8), so read those as direction only.

### 11.1 Spell length is right; returns are not

| | history | he095 (4 seeds) | top 0.95 r10 (4) | top 0.92 r10 (2) | tail 0.99 (2) | inertia 0.8 (2) |
|---|---|---|---|---|---|---|
| #1 records/yr | 19.9 | 20.5 | 19.0 | 19.3 | 19.8 | 20.5 |
| spell length | 2.52 | 2.37 | 2.41 | 2.53 | 2.35 | 2.26 |
| one-week #1s % | 23.8 | 35.0 | 34.6 | 34.2 | 36.5 | 31.5 |
| returns % (published) | 3.8 | 7.3 | 12.1 | 6.5 | 12.0 | 12.1 |
| returns %, expected (replay, as built) | — | 8.3 | 12.4 | 9.1 | 12.8 | 9.4 |
| return spells/yr | 0.75 | 1.5 | 2.6 | 1.2 | 2.4 | 2.5 |

- **Spell length is on history in every arm** (2.3-2.5 against 2.52), as in July
  (`no1-tenure-is-returns-not-run-length`). The excess is returns and one-week #1s.
- **Two-seed return rates are mostly luck.** Twenty-odd events per arm. The replay's expectation puts top 0.92 at 9.1%
  (published 6.5) and inertia 0.8 at 9.4% (published 12.1). The 4-seed tail arm (12.4 expected) is a real rise over
  he095 (8.3).
- **History's three returners include one revival.** *The Twist* returned in January 1962 after 15 months. History's
  ping-pong rate is 2 of 80 #1s in 1960-63 (2.5%), and the same 2 of 103 in 1960-64.
- 60-90% of returns come back after a single week away, mostly after a one-week interloper.

### 11.2 Two channels: survey-noise handovers and real crossings

| handover | arm | per yr | old #1 retakes next week | retakes/yr | true gap A−B in B's first week | A's slope in → out | B's slope in → out |
|---|---|---|---|---|---|---|---|
| noise | he095 | 4.1 | 19.7% | 0.81 | +0.07 | −0.09 → −0.18 | +0.10 → −0.02 |
| noise | top 0.95 | 5.0 | 24.7% | 1.24 | +0.07 | −0.06 → −0.15 | +0.10 → 0.00 |
| real | he095 | 17.4 | 1.4% | 0.25 | −0.20 | −0.19 → −0.27 | +0.18 → +0.04 |
| real | top 0.95 | 16.0 | 3.9% | 0.62 | −0.21 | −0.17 → −0.22 | +0.17 → +0.07 |

- **One #1 handover in five is survey noise.** The new #1 is behind the old one on true points, by 7% in log, at a
  crossing (A falling, B rising). The survey's sigma at the top is 1/√110 = 0.095, so the two-draw difference
  (0.134) covers a 7% gap easily. The noise handover is usually just a week early. Sometimes B stalls and A retakes.
  That is the larger channel in every arm: 0.8-1.7 retakes a year, against history's whole 0.5.
- **Real crossings back are the tail's own effect.** Without survey noise the replay gives returns of 1.9% on he095 and
  5.2% under top 0.95 (top 0.92: 2.5%). A slow-decaying former #1 is still close when a pointed interloper falls
  back. Top 0.95 also slows A's fall into a noise handover (−0.09 → −0.06), so noise handovers rise too (4.1 → 5.0 a
  year) and A retakes more often.
- **Near-tie exposure and the noise level do not change between arms.** The incumbent spends 11-13 weeks a year
  within 0.10 of its best challenger, and the top-5 noise sd is 0.092-0.098 in every arm. What changes is how close
  the dethroned #1 stays after a handover.

### 11.3 The survey is redrawn independently every week

`ChartSimulator.DrawSurveySample` draws a fresh lognormal for every record every week. A real panel was a fixed set of
outlets. Most of a record's sampling error is the panel's composition: which stores and stations happen to carry it.
That error persists from week to week while the record's footprint stays put. Independent weekly draws make the top
two trade places at crossings and then trade back. That is also why one-week #1s run 35% against 24%.

Static replay, keeping each record's marginal sigma and making its panel error AR(1) across weeks:

| arm, survey model | #1/yr | one-week #1 % | returns % | Top 10/yr (direction) | one-week Top 10 % (direction) |
|---|---|---|---|---|---|
| history | 19.9 | 23.8 | 3.8 (ping-pong 2.5) | 106.8 | 11.1 |
| he095, as built | 21.0 | 37.8 | 8.3 | 127.2 | 18.1 |
| he095, AR ρ 0.8 | 19.5 | 31.5 | 3.2 | 124.1 | 15.5 |
| top 0.95, as built | 19.1 | 34.1 | 12.4 | 111.3 | 17.6 |
| top 0.95, AR ρ 0.8 | 17.8 | 28.4 | 4.8 | 108.4 | 15.0 |
| top 0.95, AR ρ 0.9 | 17.6 | 27.6 | 4.2 | 107.9 | 14.7 |
| top 0.92, as built | 20.3 | 37.4 | 9.1 | 115.9 | 16.2 |
| top 0.92, AR ρ 0.8 | 19.0 | 31.5 | 3.3 | 113.3 | 14.2 |
| top 0.92, no noise | 18.9 | 31.8 | 2.5 | 112.8 | 14.0 |
| top 0.92, iid sigma × 0.5 | 18.9 | 32.0 | 4.3 | 113.3 | 14.1 |

- **A persistent panel removes most of the noise channel** without making the chart noise-free. At ρ 0.8, returns fall
  to 3.2% (he095), 3.3% (top 0.92) and 4.8% (top 0.95), and one-week #1s and one-week Top 10s fall toward history.
- **Cost: distinct #1s fall about 1.3 a year**, because the noise was also minting one-week #1s. On top 0.95 that
  leaves 17.8 against 19.9: its true #1 stays are too long (17.2 a year with no noise at all), which the weekly
  flicker had hidden.
- **Top 0.92 with a persistent panel is the balanced point:** 19.0 #1s a year, 3.3% returns, one-week #1s 31.5%,
  and about 113 Top 10 records a year.
- Halving iid sigma does about the same at the top, but it also halves the noise lower down, where the panel really is
  thin. Persistence keeps the error's size and changes only its timing.

### 11.4 Proposed screen (run; result in §11.5)

New probe `--survey-panel-persistence=ρ` (off by default):
- each record carries its standardised panel error, z ← ρ·z + √(1−ρ²)·ε, with ε drawn exactly where the draw is now;
- the draw count is unchanged, so the probe should be byte-identical when off;
- the multiplier stays exp(σz − σ²/2), with σ from this week's units.

Arms, on he095 + `--post-peak-demand-top-rank=10`, 4 seeds × 208 weeks:
- top 0.92 with ρ 0.8, against top 0.92 (seeds 3003 and 6006 still to run);
- top 0.95 with ρ 0.8, against `tst95r10-v1`.

Guards:
- #1 records a year (history 19.9);
- one-week Top 10 % (history 11.1);
- Big-5 Top 10 share;
- Top 40 records a year;
- Top 10 records a year. A persistent error carries a record's noise above #10 across weeks, and the static replay
  can't see the #10 awareness floor it then collects.

### 11.5 Screen result: persistent panel ρ 0.8 (2026-11-14)

Runs: 208 weeks, seeds 2002, 1001 and 3003, on he095 + `--post-peak-demand-top-rank=10`:
- `psp92-v1-*` and `psp95-v1-*`: top 0.92 and top 0.95, each with `--survey-panel-persistence=0.8`;
- controls `tst92r10-v1-*` (seed 3003 run on the probe-built DLL) and `tst95r10-v1-*`.

Probe (`Systems/SurveyPanelPersistence.cs`, checks `--survey-panel-persistence-check`, 8 pass):
- the error is kept in a weak table, so saves never carry it;
- the draw count is unchanged, so the probe is byte-identical when off: a 27-week `pspoff-v1-2002` run matches
  `fit208-v5-2002` over 3,227 record lines and 7,035 demand-stage lines.

Outputs: `SimLogs/psp-v1-{turnover,no1}.txt`. All columns are three seeds pooled.

| | history | he095 | top 0.92 | **0.92 + ρ 0.8** | top 0.95 | **0.95 + ρ 0.8** |
|---|---|---|---|---|---|---|
| #1 returns % | 3.8 (ping-pong 2.5) | 6.9 | 9.3 | **4.6** | 12.8 | **4.9** |
| return spells/yr | 0.75 | 1.4 | 1.8 | **0.8** | 2.8 | **0.8** |
| one-week #1s % | 23.8 | 33.6 | 35.6 | **25.0** | 34.5 | **27.3** |
| #1 records/yr | 19.9 | 20.5 | 19.6 | **17.9** | 19.5 | **17.0** |
| #1 spell length | 2.52 | 2.38 | 2.43 | **2.77** | 2.33 | **2.92** |
| noise handovers/yr, retaken % | — | 4.2, 16 | 4.8, 17 | **3.5, 5** | 5.4, 29 | **3.1, 11** |
| Top 10 records/yr | 106.8 | 126.3 | 115.3 | 114.2 | 109.1 | 105.8 |
| Top 10 weeks (Big-5 / field) | 4.91 (5.39 / 4.77) | 4.20 (4.05 / 4.27) | 4.61 (4.45 / 4.68) | 4.65 (4.57 / 4.68) | 4.92 (4.38 / 5.16) | 5.06 (5.23 / 4.97) |
| one-week Top 10 % | 11.1 | 16.4 | 15.8 | 13.6 | 15.3 | 11.8 |
| Big-5 share 1961-63 (per seed 1001 / 2002 / 3003) | 22.6 | 31.4 (29.6 / 29.1 / 35.4) | 31.2 (29.3 / 37.7 / 26.5) | 31.7 (36.2 / 26.4 / 32.5) | 29.6 (27.2 / 24.1 / 37.4) | 36.4 (37.3 / 36.3 / 35.5) |
| Top 40 records/yr | 298 | 282 | 274 | 270 | 268 | 265 |
| Hot 100 life of Top 10 records | 14.4 | 14.4 | 15.6 | 15.5 | 16.0 | 16.1 |

- **Returns are fixed.** Both tails fall to 4.6-4.9% (from 9.3 and 12.8), as the replay forecast. Noise handovers fall a
  quarter to a third and are rarely retaken, and one-week #1s reach history (25-27% against 24%).
- **#1 counts fall more than the replay forecast:** −1.7 a year on top 0.92 and −2.5 on top 0.95 (replay −1.3), to
  17.9 and 17.0 against history's 19.9. Spells are now too long (2.77 / 2.92 against 2.52). The weekly flicker had
  hidden #1s that hold too long: the true top is too far ahead of the field (§3: #1 = 2.6× #10 against 1.91×).
- **Top 10 side effects are benign.** One-week Top 10 visits fall toward history (top 0.95: 11.8 against 11.1). Top 10
  count moves little (−1 and −3 a year), and Top 40 records −1%.
- **Big-5 share is unresolved.** Top 0.92: +0.5 points (+6.9, −11.3, +6.0 by seed). Top 0.95: +6.8 (+10.1, +12.2,
  −1.9), with Big-5 Top 10 runs lengthening 4.38 → 5.23 weeks. Seed-to-seed swings between arms are ±10 points, so
  three seeds cannot settle +7. The survey error carries no label, so a real tilt would have to come through what a
  persistent error buys at the Top 10 edge. Treat it as a watch item.

**Verdict:** a persistent panel is the right fix for returns and one-week #1s. **Top 0.92 + ρ 0.8 is the candidate:**
returns 4.6%, one-week #1s 25%, Top 10 114 a year, Big-5 share flat. Top 0.95 + ρ 0.8 matches turnover better (106)
but carries the unresolved Big-5 rise. What remains open at the top is #1 spell length (2.77 against 2.52, so 18 #1s
a year against 20): the true #1 sits too far clear. Candidates:
- a lower top rate at #1 only;
- the top's airplay spacing (`--airplay-convexity` K = 3 compressed the top, §10.4). It was rejected for doubling
  returns under iid noise, and should be re-screened with the persistent panel.


### 11.6 Screen result: airplay convexity K 4 and K 3 under the persistent panel (2026-11-14) — rejected

Runs: `pspk4-v1-*` and `pspk3-v1-*` are `psp92-v1` (top 0.92, top rank 10, ρ 0.8) plus `--airplay-convexity=4` or `3`.
Three seeds × 208 weeks. Outputs: `SimLogs/pspk-v1-{turnover,no1,no1-replay}.txt`.

| | history | top 0.92 | 0.92 + ρ 0.8 (K 5) | + K 4 | + K 3 |
|---|---|---|---|---|---|
| #1 records/yr, published | 19.9 | 19.6 | 17.9 | 18.5 | 18.3 |
| #1 records/yr, expected (replay of the run's own survey model) | — | 20.1 | 18.6 | 18.4 | 18.4 |
| #1 returns %, published / expected | 3.8 | 9.3 / 9.7 | 4.6 / 3.9 | 5.8 / 4.8 | 4.5 / 4.9 |
| one-week #1s % | 23.8 | 35.6 | 25.0 | 30.9 | 27.6 |
| true points #1 / #10, #1 / #2 (median) | (Hesbacher 1.91) | 2.55, 1.21 | 2.48, 1.21 | 2.41, 1.19 | 2.26, 1.15 |
| units #1 / #10 | | 1.91 | 1.98 | 2.03 | 2.05 |
| Top 10 records/yr, one-week % | 106.8, 11.1 | 115.3, 15.8 | 114.2, 13.6 | 113.2, 13.1 | 115.1, 14.3 |
| Big-5 share 1961-63 (seed 1001 / 2002 / 3003) | 22.6 | 31.2 | 31.7 (36.2 / 26.4 / 32.5) | 28.7 (19.9 / 33.6 / 32.6) | 31.6 (30.7 / 31.3 / 32.9) |
| Top 40 records/yr | 298 | 274 | 270 | 275 | 271 |

- **Convexity is not the #1 lever.** K 3 narrows the top on true points (#1 / #10 2.48 → 2.26, #1 / #2 1.21 → 1.15), but
  the expected #1 count stays at 18.4-18.6. Units alone already space #1 / #10 at about Hesbacher's 1.91, so airplay adds
  the excess spacing, and narrowing it doesn't let more records reach #1. Everything else is flat or noise. Rejected.
- **The persistent panel's expected #1 cost is 1.5 a year** (20.1 → 18.6 on top 0.92). The published 17.9 included
  about 0.7 of bad luck. At three seeds the #1 count's sampling sd is about ±1.2 a year, so read published #1 counts
  against the replay's expectation.
- **Where the #1 miss sits** (#1 records a year by weeks at #1; three seeds):

  | weeks at #1 | 1 | 2 | 3 | 4 | 5-6 | 7+ |
  |---|---|---|---|---|---|---|
  | history | 4.7 | 6.2 | 5.2 | 1.2 | 2.0 | 0.5 |
  | he095 | 6.9 | 5.1 | 3.9 | 1.7 | 2.6 | 0.3 |
  | top 0.92 | 7.0 | 4.2 | 3.4 | 1.9 | 2.2 | 1.0 |
  | 0.92 + ρ 0.8 | 4.5 | 4.7 | 2.9 | 2.5 | 2.7 | 0.7 |
  | 0.95 + ρ 0.8 | 4.6 | 3.0 | 3.5 | 1.9 | 3.2 | 0.8 |

  - With the persistent panel, one-week #1s match history (4.5 against 4.7).
  - The miss is too few 2-3 week reigns (7.6 against 11.4 a year) and too many of 4 weeks or more (5.8 against 3.7).
  - Long reigns were already in excess on he095 (4.6). The Top-10-only tail adds about 0.5 and the persistent panel
    about 0.7, because iid flicker used to break long reigns.
  - So the open quantity is long uncontested reigns. Either a #1 decays too slowly while it sits there, or too few
    challengers arrive strong enough to unseat it. It is not top spacing and it is not survey noise.

**Standing candidate:** top 0.92 + ρ 0.8 at today's K 5. #1 returns are fixed (expected 3.9%) and one-week #1s are on
history; the expected #1 count is 18.6 against 19.9; Top 10 is 114 a year against 107; Big-5 share is flat. The next
diagnosis is what ends a #1 reign. Split each reign's last week by whether the #1 fell (own decay) or a challenger rose
past it, and compare the arrival of challengers within 0.1 of the #1 with history's 2-3 week reigns.

## 11.7 What ends a #1 reign (2026-11-14)

Analysis only, on existing runs (he095, top 0.92 and top 0.92 + ρ 0.8; three seeds). Scorer `analyze_no1_reigns.py`,
output `SimLogs/no1-reigns-v1.txt`. Airplay points are `unsampledChartPoints − unitsThisWeek` exactly: sales points are
units (`ChartSimulator.CalculateChartPoints`).

### 11.7.1 History's reigns run to a clock; the sim's are memoryless

Share of reigns alive at reign week j that end after it:

| j | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| history | 28% | 43% | **56%** | 33% | 70% |
| he095 | 40% | 40% | 43% | 37% | 56% |
| top 0.92 + ρ 0.8 | 28% | 38% | **35%** | 39% | 53% |

- History's hazard climbs to 56% by the third week, which is why its reigns bunch at 2-3 weeks.
- The sim's hazard is flat at 35-43% through week 4. Reigns of 2-3 weeks run short (7.6 a year against 11.4) and
  reigns of 4 weeks or more run long (5.8 against 3.7).
- Rank paths around a handover are close to history's. The next #1 sits at #2 the week before 51% of the time
  (history 57%) and comes from #4 or below 29% (30%). The old #1 lands at #2 53% of the time (62%). Who takes over is
  about right; what differs is when the #1 starts to give way.

### 11.7.2 Long reigns: the record reaches #1 on sales, then airplay arrives

First reigns by eventual length (medians; j = weeks from reaching #1; top 0.92 + ρ 0.8; he095 is the same):

| reign | release age at #1 | Big-5 | airplay share at j 0 → 2 | units j 0 → 3 (k) | airplay j 0 → 2 (k) | true slope at #1, reign weeks 1 / 2 / 3 |
|---|---|---|---|---|---|---|
| 1 week | 10 | 31% | 0.36 → 0.29 | 107 → 66 | 54 → 29 | — |
| 2-3 weeks | 9 | 33% | 0.36 → 0.34 | 120 → 99 | 70 → 58 | |
| 4+ weeks | **7** | **43%** | **0.28 → 0.41** | **129 → 131** | **50 → 97** | +0.25 / +0.16 / +0.09 |

- **Long-reign records reach #1 young, on sales.** At release age 7, airplay is only 28% of their points. Their sales then
  hold a flat plateau (129-136k for four weeks) while **airplay doubles** by age 9-10. They pull 0.3-0.4 (log) clear of
  every challenger in reign weeks 2-3, and lose #1 only when their own decline starts (week 4 or later).
- **Airplay peaks at release age 9-10 in every class**, whenever sales peaked. Records that peak on sales at age 9-10
  reign 1-3 weeks; records whose sales arrive two weeks early get the airplay peak stacked on their sales plateau.
- National `radioHeat` barely moves (0.33 throughout). The build is in regional rotation:
  `tail = Lerp(radioPlay × RegionalRadioHold 0.92, target, RegionalRadioLerp 0.15)` (`ChartManager.cs:2265`).
  - That carries 0.78 of last week forward, about a 4.6-week time constant, and settles at 0.69 × target. It comes
    after the age-6 radio build ramp, and the reporter-panel blend is small (0.085).
  - Under convexity 5, rotation still rising 5% a week means airplay points rising about 25% a week.
  - The hold was chosen so airplay outlasts the sales fall after the peak. The same lag makes airplay *arrive* late.
- **Sales hold up too.** Local consumption pays a familiarity multiplier on listener airplay (§10.3), so airplay that is
  still building props up the #1's sales.
- **The Big-5 tilt fits the entry edge.** A Big-5 campaign brings sales forward
  (`big5-entry-edge-is-campaign-impact`), so Big-5 records are over-represented among the young sales-first #1s whose
  reigns the late airplay stretches.

### 11.7.3 Implication and proposed probe (not built)

The miss is the **timing** of airplay against sales, not its level (§11.6: compressing the top changed nothing) and not
the post-peak tail. Airplay should rise about as fast as a record sells, then keep today's slow fade.

Proposed `--regional-radio-rise-lerp=L` (off by default):
- while a region's target exceeds its rotation, `tail = Lerp(radioPlay, 0.69 × target, L)`;
- 0.69 is today's settle ratio, so the plateau level is unchanged and only the approach speeds up;
- falling weeks keep the 0.92 / 0.15 hold, so the post-peak plateau is unchanged;
- no extra random draws, so it should be byte-identical when off.

Screen L 0.3 and 0.5 on top 0.92 + ρ 0.8 (three seeds). Expect:
- fewer 4+ week reigns and more #1s a year;
- airplay peaking nearer the sales peak.

Guards:
- Top 10 records and weeks: a faster rise also speeds the approach, which is already ~1.4× history's;
- one-week Top 10s;
- Big-5 share;
- debut position: the radio build exists to keep debuts near #90.

### 11.8 Screen result: faster regional radio rise, L 0.3 and 0.5 (2026-11-14)

Probe `--regional-radio-rise-lerp=L` (`Systems/RegionalRadioRise.cs`, checks `--regional-radio-rise-check` 7 pass).
- In a rising week, a region's rotation moves toward the authored settle level (0.69 × target) at rate L. The level and
  falling weeks are unchanged.
- It makes no random draws. Byte-identical off: a 27-week `rroff-v1-2002` run matches `fit208-v5-2002` over 3,227
  record lines and 7,035 demand-stage lines.

Runs: `rrl3-v1-*` and `rrl5-v1-*` are `psp92-v1` (top 0.92, top rank 10, ρ 0.8) plus L 0.3 or 0.5; three seeds × 208
weeks. Outputs: `SimLogs/rrl-v1-{turnover,no1,reigns}.txt`.

| | history | top 0.92 | 0.92 + ρ 0.8 | **+ L 0.3** | + L 0.5 |
|---|---|---|---|---|---|
| #1 records/yr, published / expected (replay) | 19.9 | 19.6 / 18.7 | 17.9 / 18.6 | **20.3 / 19.8** | 20.0 / 19.2 |
| #1 spell length, returns % | 2.52, 3.8 | 2.43, 9.3 | 2.77, 4.6 | **2.47, 3.7** | 2.51, 3.7 |
| reigns 1 / 2-3 / 4+ weeks a year | 4.7 / 11.4 / 3.7 | 7.0 / 7.6 / 5.1 | 4.5 / 7.6 / 5.8 | **6.0 / 9.5 / 4.9** | 7.1 / 7.9 / 5.1 |
| chance a reign ends after week 1 / 2 / 3 / 4 | 28 / 43 / 56 / 33 | 43 / 34 / 44 / 35 | 28 / 38 / 35 / 39 | **31 / 43 / 46 / 55** | 38 / 32 / 45 / 43 |
| one-week #1s % | 23.8 | 35.6 | 25.0 | 29.4 | 35.3 |
| 4+ reigns: airplay at j −2 / 0 / +2 (k) | | | 9 / 50 / 97 | 16 / 65 / 100 | 11 / 58 / 91 |
| Top 10 records/yr, weeks, one-week % | 106.8, 4.91, 11.1 | 115.3, 4.61, 15.8 | 114.2, 4.65, 13.6 | 113.8, 4.67, 12.0 | 114.9, 4.66, 13.0 |
| Top 10 approach, true log change at peak −2 / −1 | | | 0.37 / 0.24 | 0.38 / 0.23 | 0.37 / 0.24 |
| debut position median / mean | | 87 / 83.4 | 88 / 84.1 | 88 / 84.3 | 88 / 84.3 |
| Big-5 share 1961-63 (seed 1001 / 2002 / 3003) | 22.6 | 31.2 | 31.7 (36.2 / 26.4 / 32.5) | 33.7 (27.5 / 38.9 / 34.6) | 35.1 (30.5 / 35.1 / 39.6) |
| Top 40 records/yr | 298 | 274 | 270 | 272 | 270 |

- **The #1 count returns to history in both arms.** Spells hit history's 2.5 weeks and returns stay at history's 3.7%.
  The replay's expected count rises 18.6 → 19.8 (L 0.3) and 19.2 (L 0.5).
- **L 0.3 gives history's reign shape most nearly.** 2-3 week reigns rise 7.6 → 9.5 and 4+ fall 5.8 → 4.9. The chance a
  reign ends now rises with its age (31 / 43 / 46 / 55%) as history's does (28 / 43 / 56%).
- **The mechanism moved, but only partly.** Long-reign records reach #1 with more airplay (65k against 50k at L 0.3), and
  airplay grows 1.5-1.6× over the next two weeks instead of 1.9×. But airplay still builds after #1, and L 0.5 does no
  better than L 0.3. So the 0.15 regional lag is one of several stacked clocks:
  - the age-6 radio build ramp;
  - the per-region build clock (`--regional-radio-build-clock`, in the bundle), which starts each region's ramp at its
    own arrival, so later regions ramp later;
  - the station panel.
- **Effect size against noise.** The response is not monotone in L, and three seeds give 4+ reigns a sampling sd of
  about ±0.65 a year. Read the reign-shape gain as direction, not size. The #1 count gain (+1.2 expected) is about two
  sd of the count's own noise.
- **The guards hold.** Approach slopes, debut position, Top 10 count and weeks, and Top 40 are all unchanged; one-week
  Top 10s fall slightly (12.0 against 13.6).
- **Big-5 share is up 2-3.4 points, mixed by seed** (−8.7 / +12.5 / +2.1 at L 0.3). This is not resolved at three seeds.
  It is the third arm in a row to read Big-5 share somewhat up on this base, so keep it on the watch list.

**Candidate:** top 0.92 + ρ 0.8 + L 0.3. #1 count, spell length, returns and reign hazard are near history, and Top 10
turnover is 114 a year against 107. Next steps:
- confirm at more seeds, since the Big-5 read and the reign shape are near the noise;
- or measure how much of the remaining post-#1 airplay build is the per-region build clock (regional arrival plus the
  six-week ramp per region) before adding another lever.

### 11.9 The per-region build clock carries none of the post-#1 airplay build (2026-11-14)

Question: how much of the airplay a #1 still gains after reaching #1 (§11.8: 1.5-1.6× over two weeks at L 0.3) comes from
the per-region build clock (`--regional-radio-build-clock`: a region serviced after launch restarts the six-week radio
build ramp at its own arrival)?

Probe `--regional-radio-build-observe` (`Systems/RegionalRadioBuildObserve.cs`, observation only, no draws) writes
`SimLogs/<run>-regional-radio.csv`: per single, region and week, the arrival age, build age and weight, the tail target,
the rotation at the start of the week, after the regional update and at chart time, the reporter play and that region's
airplay points. Rows are buffered and written at chart time, because breakout discovery adds rotation after the regional
update. Scorer `analyze_radio_build_clock.py` replays each late region's rotation from its arrival with the build weight
read at release age (the clock off). Checks: the regional airplay sums to the chart's airplay points (median error 1e-6),
and the replay reproduces the logged rotation to 1.6e-7. The probe is inert: `rbobs-v1-2002` matches `rrl3-v1-2002`
over 104 weeks (16,299 record lines, 65,373 demand-stage lines).

Run: `rbobs-v1-2002` (candidate top 0.92 + ρ 0.8 + L 0.3, one seed), scored at 104 weeks. 40 first reigns (12 / 20 / 8 of
1 / 2-3 / 4+ weeks). One seed and 8 long reigns, so read sizes as direction; the zero result is not near the noise.

**The build clock: about 0%.**
- Long-reign #1s are launched almost everywhere at once: 0.25 late regions of 7, 2% of radio reach. Their airplay grows
  ×1.77 from reign week 0 to 2 with the clock and ×1.78 without it.
- Shorter reigns carry more late regions (0.9-1.5, 11-14% of reach, arriving at release age 5-6), but switching the clock
  off moves their airplay by under 2% in any week.
- Top 10 records: 29% have any late region; 0.81 of 7 on average.

**What does carry it: the reporter panel, through the rotation hold.** Rotation in the launch regions sits 2.5-3× above
the level the tail target settles at, so it is in the hold's linear branch and splits exactly into its inflows
(reach-weighted means, 4+ reigns):

| reign week | rotation | launch seed carried | tail target | rise rule (L) | reporter panel | breakout / event adds |
|---|---|---|---|---|---|---|
| −2 | 0.198 | 0.038 | 0.032 | 0.000 | 0.111 | 0.017 |
| 0 | 0.272 | 0.019 | 0.050 | 0.000 | 0.180 | 0.022 |
| +2 | 0.304 | 0.010 | 0.059 | 0.000 | 0.211 | 0.024 |

- The reporter panel is 0.085 of each week's blend (`StationRegionalSupport.PanelWeight`), but the blend is written back
  into `radioPlay` and so carried at 0.78 a week. It accumulates to about 3.4× its weekly weight, and the reporters'
  play is six times the tail target. So two thirds of rotation is reporter play, and from #1 to week 2 it supplies
  0.031 of the 0.032 rise.
- Reporter play itself is already up before #1. Reach-weighted medians at weeks −2 / 0 / +2:
  - 4+ reigns: 0.81 / 0.90 / 0.82 (tail target 0.13);
  - 2-3 week reigns: 0.68 / 0.75 / 0.53;
  - one-week reigns: 0.72 / 0.64 / 0.36.

  The build after #1 is the hold still catching up with a reporter level that was reached weeks earlier. What
  separates long reigns is that the reporters keep the record (0.82 at week 2) while shorter reigns' reporters drop it.
- **This is why L 0.5 did no better than L 0.3** (§11.8). The rise rule only acts while rotation is below 0.69 × the tail
  target, and around the reign start rotation is far above it. The rise rule contributes nothing in these weeks.
- §11.7.2 called the reporter blend small. Per week it is; carried through the hold it is the largest term.

**Verdict:** the build clock is not the lever, and no clock-off dynamic arm is needed. The late airplay build is the
reporter panel's play arriving with the station playlists and then carried at the 4.6-week time constant of the hold.
An existing switch targets this: `--station-regional-support-panel-linear=L` pays the panel linearly beside the convex
term instead of inside it (Big-5 handoff lead 2). A cleaner probe would blend the reporter play into the chart's
rotation without writing it back into the carried state. Screen either on the candidate before adding another lever.

### 11.10 Screen: panel paid linearly, L 0.15 (2026-11-14) — confounded, airplay share too low

Run `rbpl15-v1-2002`: the candidate (top 0.92, top rank 10, ρ 0.8, L 0.3) plus
`--station-regional-support-panel-linear=0.15`, which pays this week's reporter play linearly (0.15 × panel) beside a
convex term on the panel-free rotation, so the carried panel stock leaves the chart payout. One seed, 104 weeks,
against `rrl3-v1-2002` over the same weeks. Scorer `analyze_short_screen.py` (records only; one seed, read direction).

| weeks 1-104, seed 2002 | candidate | + panel linear 0.15 |
|---|---|---|
| #1 records/yr | 20.0 | 16.5 |
| one-week #1 % | 27.5 | 15.2 |
| first reigns 1 / 2-3 / 4+ per yr | 5.5 / 10.0 / 4.0 | 2.0 / 9.0 / 5.0 |
| #1 returns % | 2.4 | 2.9 |
| 4+ reigns: #1 airplay j0 → j2 (k) | 86 → 120, ×1.77 | 28 → 25, ×1.03 |
| airplay share of true points, Top 10 slots % | 26.3 | **15.8** |
| Top 10 records/yr, weeks, one-week % | 112, 4.7, 13.1 | 113, 4.6, 12.5 |
| Top 40 records/yr | 274.5 | 287.0 |
| debut position median | 87 | 88 |
| Big-5 Top 10 share 1960 / 1961 % | 20.2 / 36.9 | 18.3 / 30.2 |

- **The post-#1 airplay build is gone** (×1.03 against ×1.77), which confirms §11.9: it was the carried panel.
- **But long reigns did not fall** (5.0 a year against 4.0), and one-week #1s and #1 records fell. L 0.15 cut airplay to
  16% of Top 10 points from 26%, so the chart top is close to a sales chart, and #1 tenure follows the sales plateau.
  This arm cannot separate removing the build from removing airplay.
- **A fair test holds airplay share.** The panel-free tail pays only a few percent, so airplay is about linear in L:
  L ≈ 0.3 should restore ~26%. Big-5 share reads down here, but one seed swings ±10 points, and in October L 0.15 on
  the older base raised it (`linear-panel-payout-zeroes-offset`), so treat that as unknown.

### 11.11 Screen: panel paid linearly, L 0.3 (airplay share held) — rejected (2026-11-14)

Run `rbpl30-v1-2002`: as §11.10 with `--station-regional-support-panel-linear=0.3`. One seed, 104 weeks.

| weeks 1-104, seed 2002 | candidate | linear 0.15 | **linear 0.3** |
|---|---|---|---|
| airplay share of true points, Top 10 slots % | 26.3 | 15.8 | **27.9** |
| 4+ reigns: #1 airplay j0 → j2 (k) | 86 → 120, ×1.77 | 28 → 25, ×1.03 | **58 → 60, ×1.03** |
| #1 records/yr | 20.0 | 16.5 | 18.5 |
| one-week #1 % | 27.5 | 15.2 | 27.0 |
| first reigns 1 / 2-3 / 4+ per yr | 5.5 / 10.0 / 4.0 | 2.0 / 9.0 / 5.0 | 5.5 / 7.0 / 5.5 |
| #1 returns % | 2.4 | 2.9 | 5.1 |
| Top 10 records/yr, weeks, one-week % | 112, 4.7, 13.1 | 113, 4.6, 12.5 | 113.5, 4.6, 12.9 |
| Top 40 records/yr | 274.5 | 287.0 | 284.5 |
| Big-5 Top 10 share 1960 / 1961 % | 20.2 / 36.9 | 18.3 / 30.2 | 32.7 / 39.2 |

- **With airplay share held, the post-#1 build is still gone** (×1.03) and long reigns still do not fall: 5.5 a year
  against 4.0, with 2-3 week reigns down to 7.0 from 10.0. At one seed over two years the 4+ count has an sd of
  roughly ±1.4 a year, so this reads as no help rather than harm. It is not the shortening §11.7 predicted.
- **So the late airplay build is not what makes reigns long.** It is how the build shows up in airplay points under
  the carried panel. Remove it, and the #1 still holds, because the panel reads local sales standing: the record that
  sells most draws the most panel play, linear or carried. That is the same reinforcement October found
  (`linear-panel-payout-zeroes-offset`), and Big-5 share reads up here (+12.5 / +2.3), as it did then.
- **Verdict: reject panel-linear** for reign length. No multi-seed confirmation is needed: the target metric moved the
  wrong way at matched airplay, and the Big-5 guard reads up.
- **What remains open** is why a young sales-first #1 keeps its sales flat for four weeks (§11.7.2: 129 → 136k). The
  candidates are the local-consumption radio familiarity multiplier (§10.3) and the reporter panel's sales ordering.
  Both put more weight on the leader the longer it leads.

### 11.12 What holds a young #1's sales flat: supply, not demand (2026-11-14)

Analysis only, on `rrl3-v1-{2002,1001,3003}` (the candidate, 208 weeks), from `single-demand-stages.csv` (it covers the
lane singles, 187 of the #1s; its cleared units match `records.csv` exactly). Scripts in the session scratchpad, not
kept. First reigns: 56 / 90 / 41 of 1 / 2-3 / 4+ weeks.

**Demand falls on schedule; sales do not.** National sums for 4+ week reigns, medians:

| reign week | release age | aware buyers (M) | conversion | local multiplier | serviceable ÷ raw | units (k) |
|---|---|---|---|---|---|---|
| −1 | 7 | 1.54 | 0.405 | 3.08 | 0.40 | 115 |
| 0 | 8 | 1.68 | 0.385 | 3.17 | 0.42 | 120 |
| +1 | 9 | 1.74 | 0.339 | 3.24 | 0.44 | 127 |
| +2 | 10 | 1.75 | 0.293 | 3.31 | 0.49 | 123 |
| +3 | 11 | 1.74 | 0.251 | 3.33 | 0.56 | 119 |

- Aware buyers saturate at ~1.75M by the reign start, and the local consumption multiplier (§10.3) is flat (3.1-3.3).
  Neither props the #1 up.
- Conversion falls 12-15% a week from #1 on (release ramp done, age decay from week 8), as for every class.
- What absorbs the fall is **inventory fulfilment** (serviceable ÷ raw demand): it rises 0.40 → 0.56. The market ration
  (cleared ÷ serviceable) is flat at 0.16, so units follow serviceable intent.

**Long reigns are the records most over their supply.** Raw demand ÷ serviceable intent, medians, at reign weeks
0 / +2 / +4: 4+ reigns 2.39 / 2.04 / 1.58; 2-3 week reigns 1.84 / 1.46 / 1.18; one-week reigns 1.44 / 1.22 / 1.12. A
long-reign #1's demand starts so far above what its stores can serve that its 12%-a-week fall takes three to four weeks
to reach its sales. Its serviceable intent is flat across that window (745k / 767k / 770k / 736k at weeks 0-3).

**Where the supply binds** (share of a #1's serviceable units, reign week 0; 4+ / 2-3 / 1-week reigns):
- pinned at a fixed per-label stock ceiling (a serviceable value that recurs exactly for that label and region in the
  year): 33 / 38 / 32%, rising to 45% at week +2 for long reigns. The restock ceiling is
  `RegionalPhysicalCapacity × (0.55 + 0.65 × distributionStrength)` (`ChartManager.RestockHotRecords`);
- short of demand but not at the ceiling (restock request, service level, pressing): 59 / 47 / 39%;
- demand-bound: 8 / 15 / 29%.

The shelf sales cap (`storeCapacity`) binds in only ~3% of #1 region-weeks.

**Reading.** §11.7.2's "sales hold a flat plateau" is a supply plateau: young sales-first #1s overshoot their supply
by 2.4×, and the chart sees the supply, not the demand. That also fits §11.10-11: removing the late airplay build left
long reigns alone, because they rest on capped sales. History's 2-3 week reign clock is what a demand fall looks like
when sales follow demand.

**Open.** The split between the stock ceiling and the restock request needs restock telemetry. `breakout-funnel.csv`
logs it for ages 1-14, but `--calibration` runs skip it (lean probe). Next: a short observe run that logs, for Top 20
records, stock before restock, the demand signal, service level, requested amount, ceiling and shipped amount. Then a
lever: the ceiling, or how the restock request reads demand.

### 11.13 The supply plateau is the per-label stock ceiling (2026-11-14)

Probe `--restock-observe` (`Systems/RestockObserve.cs`, observation only, no draws) writes `SimLogs/<run>-restock.csv`:
for each single in last week's Top 20 and each region, this week's raw and servable demand, the shelf cap, stock
before and after restock, the trigger, demand signal, service level, request, the per-label stock ceiling and what
pressing shipped. Inert: a 27-week run matches `fit208-v5-2002`, and `rsobs-v1-2002` (the candidate, 104 weeks) matches
`rrl3-v1-2002` over all 16,299 record lines. Scorer `analyze_restock_binding.py`: a servable week at last week's stock is
put down to last week's restock (no trigger / stock ceiling / pressing / request too small). The ±4% regional sales
jitter comes after the stock clip, so the tests allow 4.5%. 0.5% of Top 20 units stay unclassified.

Share of a #1's servable units by what bound it (seed 2002, 104 weeks; 40 first reigns, 12 / 20 / 8):

| reign week | 4+ reigns: ceiling / demand | raw ÷ servable | 2-3 week: ceiling / demand | raw ÷ servable | 1 week: ceiling / demand | raw ÷ servable |
|---|---|---|---|---|---|---|
| −1 | 0.72 / 0.13 | 2.86 | 0.59 / 0.25 | 1.97 | 0.60 / 0.26 | 1.37 |
| 0 | 0.87 / 0.06 | 2.84 | 0.66 / 0.27 | 1.57 | 0.45 / 0.47 | 1.28 |
| +1 | 0.89 / 0.07 | 2.68 | 0.62 / 0.33 | 1.47 | 0.40 / 0.49 | 1.19 |
| +2 | 0.82 / 0.16 | 2.34 | 0.49 / 0.49 | 1.34 | 0.35 / 0.56 | 1.15 |
| +3 | 0.75 / 0.23 | 2.10 | 0.36 / 0.61 | 1.22 | 0.20 / 0.75 | 1.11 |

- **The restock ceiling is the plateau.** It holds 82-89% of a long-reign #1's servable units through reign weeks 0-2.
  The restock request, pressing, the trigger and the shelf sales cap each bind for under 5%. Across all Top 20
  region-weeks it holds 34% of servable units (demand 52%).
  - §11.12's "pinned" count (exact repeats) undercounted it. Most "short but not pinned" units were at the ceiling too.
- **The ceiling is fixed per label and region:** `(record stores × 100 + department stores × 200) ×
  (0.55 + 0.65 × distributionStrength)` for a covered label (`ChartManager.RestockHotRecords`). The physical part is
  constant by region (e.g. eastcoast 294,900, greatlakes 178,600), and the label factor runs 0.69-1.20. Once a record's
  demand is over it, the region's servable intent is a constant, so its sales points are flat until demand falls back
  under the ceiling. A long-reign #1 starts 2.8× over, which at a 12-15% weekly fall takes most of its reign.
- **It tilts toward strong distributors.** Median label distribution strength at the reign start is 0.88 for 4+ reigns,
  against 0.74 and 0.82 for shorter ones. Big-5 are 50% of long reigns here, against 9-20% (n 8, one seed: direction
  only). A higher ceiling is a higher plateau. That is the Big-5 tilt §11.7.2 saw among long reigns.

- **It is not a physical shortage.** Where the ceiling binds a #1, the region sells 17% of the copies on its shelves that
  week and 83% are left (Top 20: 16% / 84%). The clip is applied to servable *intent*, and the market ration
  (fixed singles purse, ~0.16) turns intent into units only after it. Stock units are compared with an intent quantity
  about six times larger than what sells. So the ceiling caps how much of the purse a hit can bid for, not how many
  copies the shops hold.

**Implication.** Long reigns are not an airplay or demand-shape problem. The chart reads a capped supply, and the
biggest hits are capped hardest and longest. Lever directions, none screened:
- **Raise the ceiling** (a new scale on the stock ceiling), so sales follow demand and fall when demand falls. Risk: #1
  peak units rise (#1 lifetime units already run above the author's 0.80M), and the fixed singles purse shifts toward
  the top records.
- **Flatten its label factor** (0.55 + 0.65 × ds), which would also cut the Big-5 plateau premium.
- **Pull top-record demand nearer supply.** A raw demand 2.8× what stores can stock is itself unhistorical.

### 11.14 Screen: stock clip on units after the ration (2026-11-14) — not a candidate alone

Probe `--post-ration-stock-clip` (`Systems/PostRationStockClip.cs`, off by default, byte-identical off: a 27-week
`prsoff27-v1-2002` run matches `fit208-v5-2002`). A live single's intent is no longer clipped at shelf stock (only the
shelf sales cap applies); once local clearing and spillover are done, its cleared units are clipped at its stock, the
excess going to backorders. Albums are unchanged. The clip runs before the LP steps, because the LP audit checks that
singles are settled by then. Run `prsc-v1-2002`: the candidate plus the probe, one seed, 104 weeks.

| weeks 1-104, seed 2002 | candidate | + post-ration clip |
|---|---|---|
| #1 records/yr | 20.0 | 16.5 |
| first reigns 1 / 2-3 / 4+ per yr | 5.5 / 10.0 / 4.0 | 4.0 / 9.0 / 3.0 |
| one-week #1 %, #1 returns % | 27.5, 2.4 | 18.2, 15.4 |
| **#1 peak week units (k, median)** | 121 | **198** |
| **#1 lifetime units (M, median)** | 1.12 | **1.51** (author's target 0.80) |
| Top 10 records/yr, weeks, one-week % | 112, 4.7, 13.1 | 114, 4.6, 14.7 |
| Top 40 records/yr, debut median | 274.5, 87 | 273.5, 88 |
| airplay share, Top 10 slots % | 26.3 | 23.7 |
| Big-5 Top 10 share 1960 / 1961 % | 20.2 / 36.9 | 29.4 / 28.7 |
| singles cleared ÷ singles purse | 1.002 | 0.956 |

- **The intent clip was doing calibration work.** Without it, the singles ration (exponent 1.35 on servable intent)
  hands the biggest hits far more of the fixed purse: #1 peak weeks +64%, lifetime +35%, both further from the 0.80M
  target. Long reigns fall only a little (4.0 → 3.0 a year), #1 records fall to 16.5 a year, and returns jump to 15%:
  bigger hits trade #1 back and forth.
- **Lost sales: 4.4% of the singles purse.** The post-ration clip still binds on 1.2% of intents (11.5M units over two
  years), mostly records with almost no stock. Those units are not re-rationed to records with stock. A clean version
  would clip inside the ration (water-filling), not after it.
- **Reading.** The stock ceiling was compressing the top of the market as a side effect, and the 1.35 ration exponent
  was adopted (for #1 sales size) on top of that compression. Removing one exposes the other. The ration exponent at
  1.35 is also most of the too-steep chart bottom (§10.4: g 1 takes #10 / #100 from 29× to 14×, history 11×).
- **Next screen:** the post-ration clip together with `--singles-ration-exponent=1.0`, which should pull #1 sales back
  down and flatten the bottom of the chart in one move. Fix the lost sales first if that arm looks promising.

### 11.15 Screen: stock clip after the ration with ration exponent 1.0 (2026-11-14) — calibration yes, reigns no

Run `prscg1-v1-2002`: `prsc-v1-2002` with `--singles-ration-exponent=1.0` (the only flag difference). One seed, 104
weeks. Reference flags in `SimLogs/writing-calibration/prscg1-reference-request.json`, launched through
`run_indie_access_screen.ps1 -Arms lw0up1_ao2_lh1_he095`. #10 / #100 is the median weekly units at those positions
(`curve.py` method; history, Hesbacher, 11×). Purse = singles cleared ÷ `singleFormatBudget` (market-clearing-weekly).

| weeks 1-104, seed 2002 | candidate | + clip | **+ clip, g 1.0** |
|---|---|---|---|
| #1 records/yr | 20.0 | 16.5 | 17.5 |
| first reigns 1 / 2-3 / 4+ per yr | 5.5 / 10.0 / 4.0 | 4.0 / 9.0 / 3.0 | **7.0 / 4.5 / 5.5** |
| one-week #1 %, #1 returns % | 27.5, 2.4 | 18.2, 15.4 | 40.0, 2.8 |
| #1 peak week units (k, median) | 121 | 198 | **116** |
| #1 lifetime units (M, median) | 1.12 | 1.51 | 1.21 (target 0.80) |
| weekly units #1 / #10 / #25 / #50 / #100 (k) | 127 / 66 / 32 / 10.2 / 2.3 | 204 / 61 / 27 / 7.6 / 1.6 | 130 / 53 / 27 / 10.1 / 3.1 |
| **#10 ÷ #100** (history 11×) | 28× | 38× | **17×** |
| Top 10 records/yr, weeks, one-week % | 112, 4.7, 13.1 | 114, 4.6, 14.7 | 111.5, 4.75, 13.1 |
| Top 40 records/yr, debut median | 274.5, 87 | 273.5, 88 | 265.5, 87 |
| airplay share, Top 10 slots % | 26.3 | 23.7 | 30.2 |
| Big-5 Top 10 share 1960 / 1961 % | 20.2 / 36.9 | 29.4 / 28.7 | 22.7 / 27.7 |
| singles cleared ÷ purse | 1.002 | 0.956 | 0.962 |

- **The calibration half works.** Exponent 1.0 undoes the clip's #1 inflation (peak week 116k against the candidate's
  121k) and halves the chart's bottom slope relative to the candidate (17× against 28×). The clip alone had made it steeper (38×). #1 returns
  fall back to 2.8%. #1 lifetime units stay above target (1.21M).
- **Long reigns do not shorten.** 4+ reigns read 5.5 a year against 4.0 (11 against 8; sd about ±1.4 a year at one
  seed, so no help rather than harm), and the middle collapses: 2-3 week reigns 4.5 a year against 10.0, one-week
  #1s 40%. The reign distribution goes bimodal, the opposite of history's 2-3 week clock.
- **A sales plateau survives without the stock ceiling.** National medians for 4+ reign #1s, reign weeks −1 … +3:
  units 102 / 161 / 161 / 156 / 153k; raw demand 2.0 / 2.5 / 2.3 / 1.95 / 1.67M; servable 1.64 / 1.59 / 1.61 / 1.65 /
  1.51M. Raw demand still falls 12-17% a week, but servable intent stays flat. (An earlier draft put ~30% of it at
  the shelf sales cap; that test missed the ±4% jitter applied after the cap. §11.16 has the corrected trace: the shelf
  cap holds 70%.)
- **Long reigns are the biggest records.** Raw demand at reign start: 2.5M for 4+ reigns against 1.1-1.4M for 2-3
  week reigns (both clip runs). Their lead over #2 at reign start is not much larger (chart points #1 ÷ #2: 1.14
  against 1.07; candidate 1.24 / 1.10), so a points gap does not explain them. Reading, not yet traced: demand above the
  caps acts as a reservoir that keeps servable intent flat while raw demand falls.
- **Lost sales unchanged:** 3.8% of the purse (the clip-after-ration leak in §11.14, not re-rationed).

**Verdict.** Not a reign-length lever. As a calibration move (clip after the ration plus g 1.0) it improves #1 peak
size and the chart's bottom slope, but it needs the inside-the-ration water-fill fix for the 3.8% leak and a
multi-seed check of the reign mix before it could be adopted. Long reigns now look like a shelf-cap plateau on the
biggest records. Next: trace the raw-to-servable stages for long-reign #1s in `prscg1` (shelf cap, inventory
fulfilment, coverage) to see which cap holds servable intent flat.

### 11.16 Trace: with the clip after the ration, the shelf sales cap is the plateau (2026-11-14)

Analysis only, on `prscg1-v1-2002` (§11.15) and `prsc-v1-2002`, from `-restock.csv` (Top 20 singles × region). Sums
per record over the first reigns that ended inside week 104 (11 / 8 / 17 records of 4+ / 2-3 / 1 weeks). Scripts in
the session scratchpad, not kept.

With `--post-ration-stock-clip`, a live single's servable intent is `min(raw, shelf sales cap) × jitter(0.96-1.04)`
(clamped at the cap), `ChartSimulator.CalculateRegionalSales`. Nothing else stands between raw demand and servable:
in regions under the cap, servable equals raw to within the jitter. The cap is `(record stores × 250 + department
stores × 500 × rack shelf) × inventoryDepth`, ×1.5 inside the Top 20 (`storeCapacity`). A region counts as capped when
servable ≥ 0.955 × cap.

Long-reign #1s, per-record means (k):

| reign week | raw | servable | units | capped regions | capped raw → servable | uncapped raw = servable |
|---|---|---|---|---|---|---|
| −1 | 2,367 | 1,454 | 157 | 3.4 | 1,756 → 840 | 611 |
| 0 | 2,589 | 1,547 | 162 | 3.9 | 2,124 → 1,078 | 465 |
| +1 | 2,523 | 1,555 | 163 | 3.6 | 1,896 → 929 | 627 |
| +2 | 2,275 | 1,501 | 153 | 3.4 | 1,655 → 878 | 623 |
| +3 | 1,990 | 1,366 | 136 | 2.9 | 1,238 → 614 | 752 |

2-3 week reigns at week 0: 1.5 capped regions, 565 → 346 capped, 636 uncapped; by week +2 only 1.0 region is capped.

- **The shelf sales cap holds 70% of a long-reign #1's servable intent at reign start** (52% of all #1 region-weeks'
  servable; Top 10 26%, Top 20 20%). In capped regions raw demand is 2.0× the cap, so a 12-15% weekly fall takes about
  five weeks to show up in servable intent. This is the same plateau as §11.13, one stage on: removing the stock ceiling
  from intent exposed the next ceiling in the chain.
- **Stock is a minor limit after the ration.** Sold-out region-weeks (stock left after sales ≤ 4.5% of sales; the
  `stockBefore` column is logged after the week's sales) carry 5% of #1 units, 11 / 11 / 9 / 7% of long-reign units in
  reign weeks −1 … +2, and ~1% for 2-3 week reigns. They still occur for labels with small regional ceilings
  (e.g. gen_1665: 18,258 a week in greatplains, 70,183 in eastcoast until its label's ceiling rose to 305,959 at
  week 27).
- **The shelf cap is a units constant applied to intent.** "250 weekly sales per record store" is compared with
  servable intent, which the ration turns into units at about 0.09 here (0.15 at g 1.35). Eastcoast's Top-20 cap of
  987k intent clears as about 70-90k units. This is the same unit mismatch §11.13 found in the stock ceiling. Raw demand
  is an intent scale too, not buyers: gen_1665's conversion per aware buyer reached 7.7 in greatplains, with raw
  demand of 1.5-2.6M against a 192k potential audience.

**Reading.** In both clip arms, long reigns are the records whose demand is furthest over a per-region intent ceiling.
First that ceiling was the restock stock ceiling (§11.13); with that one moved after the ration, it is the shelf cap.
History's 2-3 week clock needs sales to follow demand down. Under this design that means either no per-region intent
ceiling binding at the top (cap in units after the ration, like the stock clip) or top-record raw demand nearer the
caps. Without a cap the fixed purse moves further toward the biggest records, so #1 size would have to come from the
ration exponent (g < 1) or a lower demand scale.

**Next screen, not run:** shelf cap moved after the ration as well (a units cap on cleared units, or the cap scaled by
the ration factor), with g 1.0 and the clip. That shows whether long reigns shorten once no ceiling binds, and how
much #1 size and the chart's bottom slope move. The purse leak fix (water-filling) is still owed for any adopted
version.

### 11.17 Screen: shelf cap after the ration too (2026-11-14) — rejected; reigns follow the demand life-cycle

Probe `--post-ration-shelf-cap` (`Systems/PostRationStockClip.cs`, needs `--post-ration-stock-clip`, off by default). A
live single's intent is no longer clipped at the shelf sales cap either, and its cleared units are clipped at the lower
of stock and the shelf cap after the ration. In units the shelf cap (e.g. 987k in eastcoast) never binds, so this arm
is "no per-region ceiling on intent". Inert: with the switch off, 27-week runs match row for row on `records.csv`
(`prsoff27b` = `prsoff27`, clip off; `prscg127` = `prscg1` weeks 1-27, clip and g 1.0 on). Run `prsshelf-v1-2002`:
`prscg1` (§11.15) plus the switch, one seed, 104 weeks.

| weeks 1-104, seed 2002 | candidate | clip + g 1.0 | **+ shelf cap after ration** |
|---|---|---|---|
| #1 records/yr | 20.0 | 17.5 | 14.0 |
| first reigns 1 / 2-3 / 4+ per yr | 5.5 / 10.0 / 4.0 | 7.0 / 4.5 / 5.5 | **1.5 / 6.0 / 6.0** |
| one-week #1 %, #1 returns % | 27.5, 2.4 | 40.0, 2.8 | 10.7, 0.0 |
| #1 peak week units (k, median) | 121 | 116 | **188** |
| #1 lifetime units (M, median; target 0.80) | 1.12 | 1.21 | **1.63** |
| #10 ÷ #100 (history 11×) | 28× | 17× | 17× |
| Top 10 records/yr, weeks, one-week % | 112, 4.7, 13.1 | 111.5, 4.75, 13.1 | 111.5, 4.71, 16.0 |
| Top 40 records/yr | 274.5 | 265.5 | 270.5 |
| airplay share, Top 10 slots % | 26.3 | 30.2 | 28.8 |
| Big-5 Top 10 share 1960 / 1961 % | 20.2 / 36.9 | 22.7 / 27.7 | 11.9 / 31.2 |
| singles cleared ÷ purse | 1.002 | 0.962 | 0.958 |

- **Sales now follow demand, and reigns still do not shorten.** Servable equals raw (to the jitter), and units of
  long-reign #1s fall 197 / 201 / 188 / 172 / 145k over reign weeks 0-4 instead of holding flat. But 4+ reigns read
  6.0 a year, one-week reigns nearly vanish (1.5), and #1 records fall to 14 a year. With no ceiling the biggest
  records take more of the fixed purse: #1 peak weeks +62%, lifetime 1.63M.
- **Stock after the ration binds a little more** (17% of long-reign units in sold-out region-weeks at reign weeks 0-1,
  against 11%), and lost sales stay at ~4% of the purse.

**Why reigns stay long: when a record reaches #1 in its demand life-cycle.** National demand stages for #1s
(`single-demand-stages.csv`, medians):

| | age at reign start | aware buyers j0 → j+2 (M) | conversion j0 → j+2 → j+4 | raw j0 / j+1 / j+2 / j+4 (k) |
|---|---|---|---|---|
| 4+ reigns (n 12) | **6** | 1.63 → 1.76 | 0.464 → 0.402 → 0.274 | 2,274 / 2,507 / 2,242 / 1,525 |
| 2-3 week reigns (n 12) | **8** | 1.76 → 1.75 | 0.353 → 0.262 → 0.203 | 1,794 / 1,800 / 1,538 / 1,138 |
| one-week reigns (n 3) | **9.5** | 1.75 → 1.72 | 0.236 → 0.164 → 0.115 | 1,292 / 1,102 / 896 / 568 |

- Every single follows the same clock: awareness saturates at ~1.75M around age 8, conversion is flat through the
  release ramp and then falls 12-17% a week from age 8. The local consumption multiplier rises slowly (2.96 → 3.31)
  and offsets part of the fall.
- **A record that reaches #1 at age 6 still has two weeks of rising demand ahead of it**, then a slow decline from a
  higher level. One that reaches #1 at age 9-10 is already falling. Long reigns are fast climbers that peak on the
  chart before they peak in demand. The ceilings in §11.13-16 lengthened this plateau but did not create it.
- Not checked: the release age at which history's long-reign #1s reached the top. That would show whether arriving
  early is itself unhistorical, or whether the post-arrival demand build is.

**Verdict: reject** the shelf-cap move (bigger #1s, fewer #1 records, no shorter reigns). The intent ceilings are not
the reign lever. Reign length is set by the single's demand clock (awareness ramp to ~age 8, then conversion decay)
relative to how fast a record climbs. Levers on that clock: an earlier or sharper demand peak for fast climbers
(e.g. `--demand-age-decay=R`, §10.4, never screened on this base), or an awareness ramp that saturates sooner for
records already at the top. Any of them changes every record's life, so screen them against Top 10 weeks and the
chart's bottom slope as well as reigns.

**Clip + g 1.0 (§11.15) remains a separate calibration candidate** for #1 size and the chart's bottom slope. It is
neutral to harmful for reigns and still needs the water-fill fix for the ~4% purse leak.

### 11.18 Screen: faster conversion age decay, 0.85 and 0.80 (2026-11-14) — rejected

Runs `dad85-v1-2002` and `dad80-v1-2002`: the candidate (`rrl3`, stock ceiling as authored, g 1.35) plus
`--demand-age-decay=0.85` / `0.80` (`TrajectoryShapeProbe`; authored 0.91 a week after demand age 8). One seed, 104
weeks; reference flags `SimLogs/writing-calibration/rrl3-reference-request.json`.

| weeks 1-104, seed 2002 | history | candidate (0.91) | **0.85** | **0.80** |
|---|---|---|---|---|
| #1 records/yr | | 20.0 | 22.5 | 21.5 |
| first reigns 1 / 2-3 / 4+ per yr | | 5.5 / 10.0 / 4.0 | 10.5 / 6.0 / 5.5 | 9.5 / 6.5 / 5.0 |
| one-week #1 %, #1 returns % | | 27.5, 2.4 | 42.2, 4.3 | 32.6, 12.2 |
| #1 peak week units (k, median) | | 121 | 148 | 164 |
| #1 lifetime units (M, median; target 0.80) | | 1.12 | 1.07 | 1.17 |
| Top 10 records/yr, weeks, one-week % | 106.8, 4.89, 11.1 | 112, 4.70, 13.1 | 124, 4.25, 12.2 | 131, 4.00, 15.1 |
| Top 40 records/yr | 298 | 274.5 | 305 | 330 |
| #10 ÷ #100 | 11× | 28× | 28× | 28× |
| airplay share, Top 10 slots % | | 26.3 | 24.2 | 24.1 |
| Big-5 Top 10 share 1960 / 1961 % | | 20.2 / 36.9 | 29.6 / 26.0 | 31.3 / 38.7 |

- **It speeds up the whole chart, not the top.** Top 10 records +12 / +19 a year (further above history's 106.8),
  Top 10 stays shorter (4.25 / 4.00 weeks against 4.70; history 4.89), Top 40 records +31 / +56. Long reigns do not
  fall (5.5 / 5.0 against 4.0; ±1.4 at one seed), and the extra #1 records are mostly one-week reigns.
- **#1 peak weeks grow** (148k / 164k against 121k). Older records lose conversion, so the fixed purse moves to the
  young records at the top.
- **The decay misses the window that matters.** On the candidate, long-reign #1s reach the top at demand age 7,
  against 10 for 2-3 week and one-week #1s (the same split as §11.17's age 6 against 8-9.5). The decay starts after
  age 8, so it does not touch their first reign weeks. At 0.85 a long-reign #1's raw demand is 1.76M at reign week 0
  and 1.72M at week +2, still above the stock ceiling, so its units hold at 153k → 156k. For the later arrivals it
  bites immediately: 2-3 week #1s' conversion falls 0.217 → 0.128 over two weeks.

**Verdict: reject.** A global post-age-8 decay is the wrong shape: it shortens the life of every record that arrives
late and leaves the early-arriving #1s alone. If the demand clock is the lever, it has to act on where a record stands
in its own life rather than on its calendar age: e.g. the decay clock starting at the record's national peak (the
post-peak tail already does this, top 0.92), or awareness/ramp growth ending sooner for a record that is already #1.
Before building either, check the history side (§11.17, open): at what release age history's long-reign #1s reached
the top. If history's long reigns also arrived early, the sim's issue is the 2-3 week middle, not the early arrivals.

## 11.19 How history's long reigns worked, against the candidate (2026-11-14) — no reign-length gap left to close

Analysis only. History: `SimLogs/historical-hot100-1960-64/hot100_weekly.csv` (positions only; a record = title +
artist; release age unknown, so the clock is **chart age**, weeks since Hot 100 debut). Sim: the candidate
`rrl3-v1-{2002,1001,3003}`, 208 weeks each, records already charting in week 1 dropped. First reigns only, debut
observed, reign finished inside the window. Scripts in the session scratchpad, not kept.

**Reign lengths per year** (all completed first reigns; history 101 over 5 years, sim 236 over 12 seed-years):

| reign length (weeks) | 1 | 2 | 3 | 4 | 5 | 6+ | mean | chance of ending after week 1 / 2 / 3 / 4 / 5 |
|---|---|---|---|---|---|---|---|---|
| history | 5.8 | 6.2 | 4.4 | 1.4 | 1.6 | 0.8 | 2.51 | 29 / 43 / **54** / 37 / 67 |
| candidate | 5.8 | 5.8 | 3.7 | 2.4 | 1.2 | 0.9 | 2.53 | 29 / 41 / **45** / 54 / 60 |

- **The distribution is history's except at week 3.** History ends 54% of reigns that reach week 3, the candidate
  45%. That moves about one reign a year from three weeks to four (4-week reigns 2.4 against 1.4 a year; weeks at #1
  in 4+ reigns 21.4 against 19.3). The difference is about 1.4 sd (history has 7 four-week reigns in five years), so
  it is not established.
- By year, history's 4+ reigns run 5 / 3 / 4 / 3 / 4, and the candidate's 2-7 per seed-year (mean 4.6). One seed over
  104 weeks cannot rank arms on this count: §11.14-18 compared arms at ±1.4 a year against a gap of ~1.

**How long reigns form and end** (medians by eventual first-reign length):

| | history 1 / 2-3 / 4+ | candidate 1 / 2-3 / 4+ |
|---|---|---|
| first reigns per yr | 5.0 / 10.0 / 3.6 | 5.6 / 8.9 / 4.4 |
| **chart age at reaching #1 (weeks)** | 7 / 6 / **4** | 7 / 6 / **5** |
| debut position | 79 / 72.5 / 65.5 | 77 / 74 / 65 |
| position 3 / 2 / 1 weeks before #1 | 8-4-2 / 11-5-2 / 17-9-3 | 8-4-2 / 12-4-2 / 15-7-2 |
| position 1 / 2 / 4 weeks after the reign | 2-4-12 / 2-4-13 / 2-5-15 | 2-5-12 / 2-5-14 / 2-4-14 |
| weeks in Top 10 / on the Hot 100 | 7-15 / 8-15 / 10-16 | 7-17 / 8-18 / 10-19 |
| successor: chart age / position the week before | 6-3 / 5-2 / 7-2 | 6-2 / 7-2 / 6-2 |
| release age at #1 (sim only) | | 10 / 9 / 7 |

- **History's long reigns were fast climbers that arrived early**, at chart age 4 (6% at age 1, 34% by age 3),
  against 6-7 for shorter reigns. They debuted higher (#65) and were at #17 three weeks before #1. That is the
  candidate's pattern (chart age 5, debut #65, #15). §11.17's "long reigns are records that arrive early in their
  demand life" is historical, not a sim artefact.
- **They ended the same way:** the old #1 drops to #2, then about #5 and #15 four weeks later, and the successor comes
  up from #2. Top 10 weeks match (10). Only total Hot 100 life is longer in the sim (+3 weeks in every class, the known
  post-peak tail, §10).
- History has no sales, so the sales plateau under the stock ceiling (§11.12-13) cannot be compared directly. In rank
  terms it produces history's reign shape.

**Verdict.** At three seeds the candidate reproduces history's reign lengths, reign hazard to week 2, arrival age,
climb path and exit path. The remaining difference (week-3 hazard 45% against 54%, ~1 four-week reign a year) is
within noise. §11.12-18 chased a mechanism, the plateau under per-region ceilings, without a measurable count gap
behind it. The baseline for those screens (seed 2002, 104 weeks: 4.0 a year) was already at history's 3.6-3.8.
- **Close the reign-length thread.** No further reign lever is warranted unless more seeds show the week-3 hazard gap
  is real. A 6-seed × 208-week candidate pool would put the 4-week reign count at about ±0.3 a year.
- **What remains of §11.14-18 is calibration, not reigns.** Clip + g 1.0 (§11.15) improves #1 peak size and the
  chart's bottom slope (28× → 17×, history 11×). It moved the reign mix the wrong way at one seed (2-3 week reigns
  10 → 4.5), so check it against this table at three seeds before taking it further.
- Rule for this metric: compare reign-length counts only on pooled multi-seed, multi-year runs. Per-seed-year 4+
  counts range 2-7 in the candidate alone.

## 12. Chart bottom target: what the bottom of the curve should be (2026-11-14)

Research only. Question: what should the sim's sales-by-rank curve look like below #25, independent of the "Hesbacher"
curve used so far?

### 12.1 What we had been calling "Hesbacher"

The target in `curve.py` (100 / 52.3 / 27.4 / 13.6 / 7.9 / 4.8 at #1 / 10 / 25 / 50 / 75 / 100), and the 11× quoted
in §10.2, is **Carroll (2015)**: score = −4357 · x/(x+10) + 4139, fitted for 1958-75. Hesbacher et al. (1982) is a
different formula, weight = −30.404 · x/(x+2) + 30.265, for the 1970s. Both are **chart-score weights**, built from
Billboard's scoring system: store rank reports plus radio-playlist points, per Carroll and Haampland. They are not
sales. Carroll says the fat tail of Hesbacher's curve is "driven by the steepness of the radio rating system", i.e.
airplay is more skewed than sales.

### 12.2 Searching for a sales curve

No 1960s US sales-by-rank data is public. Billboard and Cash Box never printed units, the chart was compiled from
dealer and DJ rank reports, and RIAA shipment statistics start in 1973. Anchors found (none gives a curve):
- 1959: operators of ~500,000 jukeboxes bought 47M records, ~92 per machine per year (*Billboard*, 9 May 1960). A
  spotlight review could bring 75,000 operator copies in a record's first week (Seymour Stein, *Billboard* interview).
  Operators bought widely across titles, which would make the 1960s curve flatter than a consumer-only one (not
  measured).
- 1967: a single usually broke even below 20,000 units, ~70% of singles lost money, a fast-rising #1 sold 1M+
  (*Fortune*, "The Motown Sound of Money").
- UK: a 1970s #1 needed ~150k a week; 2010: #1 ~100k, Top 10 ~30k, Top 40 ~6k a week.
- BMRB monthly Top 200 UK singles with units, 1976-82, are posted on UKMIX but sit behind a bot check. Not read.

**The one measured curve: Haampland (2017).** "Power Laws and Market Shares: Cumulative Advantage and the Billboard
Hot 100", *J. New Music Research* 46(4) (text in `SimLogs/chart-bottom-research/`). Twelve weeks of Nielsen
SoundScan **Digital Songs** (pure unit sales, 2012-13) fit share = 0.09 · rank^−0.66 (r² 0.98). Weekly #1 up to
412k, #100 down to 16.9k. It is a 99¢ digital market, not 1960s 45s, but it is a real sales count by rank.

### 12.3 The curves side by side

% of #1 by rank; slope = −d ln(share) / d ln(rank). Sim: candidate `rrl3-v1-{2002,1001,3003}`, weeks 1-208, median
weekly value at each position.

| curve | #5 | #10 | #25 | #50 | #75 | #100 | #1÷#10 | #10÷#100 | slope #10-50 | slope #50-100 |
|---|---|---|---|---|---|---|---|---|---|---|
| Hesbacher 1982, 1970s chart weights | 42.5 | 24.5 | 10.5 | 5.1 | 3.2 | 2.3 | 4.1 | 10.8 | 0.97 | 1.17 |
| Carroll 2015, 1958-75 chart score | 71.8 | 52.4 | 27.4 | 13.6 | 7.9 | 4.8 | 1.9 | 11.0 | 0.84 | 1.51 |
| Haampland 2017, SoundScan units | 34.6 | 21.9 | 11.9 | 7.6 | 5.8 | 4.8 | 4.6 | 4.6 | 0.66 | 0.66 |
| **sim units** | 75.1 | 53.6 | 26.5 | 9.3 | 4.1 | 2.2 | 1.9 | **24.6** | 1.09 | **2.09** |
| sim chart points | 57.2 | 39.5 | 18.3 | 6.1 | 2.6 | 1.3 | 2.5 | 29.8 | 1.16 | 2.21 |

- **The sim's bottom half is too steep against every reference.** From #50 to #100 the sim's units fall with
  exponent 2.1. The chart-score curves fall at 1.2-1.5 and the measured sales curve at 0.66. The middle (#10-50,
  1.09) sits inside the references' range (0.66-0.97 sales/weights, 0.84 Carroll), slightly steep.
- **Sales should be flatter than the chart score, not steeper.** Carroll attributes the chart score's tail to
  airplay. The one measured sales curve is flatter than either score curve, and 1960s jukebox buying plausibly
  flattened the bottom further.
- **The top is not settled by these sources.** The sim's #1÷#10 (1.9) matches Carroll. Hesbacher 1982 and the
  SoundScan sales curve both put it at 4.1-4.6. The reign and turnover work (§11) was tuned against rank behaviour,
  not this ratio; leave the top alone here.

### 12.4 Target

For the sim's **units** below #25:
- slope #50-100 between ~0.7 (measured sales, digital era) and ~1.5 (1958-75 chart score); aim ~1.0-1.2;
- #10 ÷ #100 between ~5× and ~11× (now 24.6×); #100 at ~4-5% of #1 (now 2.2%), #50 at ~10-14% (now 9.3%);
- the #1-#25 curve unchanged (it matches Carroll, and §11 reign shape depends on it).

Chart **points** add airplay on top, so they may stay steeper than units (Carroll), but not 30×.

**Open.** Read §10.4's g 1.0 result (14× on an older base) against this target: it is the right direction. The
remaining question is mechanism: which stage makes the bottom steep (ration exponent 1.35, purchase-exposure weight,
airplay convexity, or the stock ceiling), so a fix flattens #25-100 without touching #1-25. A bottom-only lever is
preferable to a global exponent change, which also flattens the top (§11.15: more one-week #1s at g 1.0).

### 12.5 What makes #25-100 steep (2026-11-14)

Analysis only, on the candidate `rrl3-v1-{2002,1001,3003}`, weeks 1-208. Each charted record-week's national units are
split into additive log stages from `single-demand-stages.csv`:
- aware buyers;
- mean quality factor;
- **purchase exposure**, `GetChartExposureWeight` recomputed from last week's position;
- other conversion: base rate, release ramp, age decay, commercial fit, uncharted exposure;
- the local consumption multiplier: raw ÷ (aware × conversion), with fans;
- supply: servable ÷ raw, the stock ceiling and shelf cap;
- the ration: cleared ÷ servable, the fixed purse at g 1.35.

Means of logs over 5-rank bands, so the stages sum exactly to the units slope. Coverage: the stage file holds lane
singles only, 22-25% of chart rows below #25 (52% in the Top 25). The covered rows' units slope (2.10) matches all
records' (2.09).

| slope contribution | #10 → #25 | #25 → #50 | **#50 → #100** |
|---|---|---|---|
| aware buyers | 0.16 | 0.13 | 0.11 |
| quality | −0.05 | 0.03 | 0.21 |
| **purchase exposure** | 0.26 | **0.68** | **0.82** |
| other conversion | 0.43 | 0.17 | 0.41 |
| local consumption multiplier | −0.06 | 0.29 | 0.36 |
| supply (stock ceiling, shelf cap) | −0.20 | −0.10 | −0.35 |
| **ration, g 1.35** | 0.19 | 0.33 | **0.53** |
| **units slope** | 0.75 | 1.54 | **2.10** (target ~1.0-1.2) |
| chart points ÷ units | 0.12 | 0.05 | 0.03 |

- **Purchase exposure is the largest term**: 39-44% of the slope below #25. It is the Carroll score curve raised to
  0.62 and flat across the Top 10 (`RawChartExposure`), applied by last week's rank. Carroll's tail is, by his own
  account, radio scoring. So the sim multiplies conversion by a chart-score curve whose bottom is airplay-shaped.
- **The ration exponent multiplies everything above it by 1.35.** Its 0.53 is (g − 1) × the servable slope. Through
  it, exposure's full effect on units is ~0.82 × 1.35 ≈ 1.1 of the 2.10.
- **How this probably happened.** The `CHART_EXPOSURE_EXPONENT` note fitted 0.62 so that the published curve met the
  Carroll target at #100 (first-order 6.0%), at g = 1. g 1.35 came later (fanbase part B), for #1 sales size.
  Exposure at 0.62 under g 1.35 overshoots the bottom.
- The local consumption multiplier (0.36) also falls with rank. It pays familiarity on listener airplay (§10.3), and
  low records get little airplay: a second, airplay-driven rank feedback. Other conversion (0.41) is mostly
  selection: #100 mixes very young climbers on the release ramp with old records on the age decay.
- Supply works the other way (−0.35): the stock ceiling holds back records nearer the top more. Moving the stock clip
  after the ration (§11.14) removed that offset, which is why it steepened the bottom (38×).
- Chart points add almost nothing below #25 (0.03); the sim's points curve is steep because its units are.

**Lever.** The steepness below #25 comes from the exposure curve's tail, amplified by the ration exponent. A
bottom-only change to exposure leaves the Top 25, #1 sales and the reign mix alone, which g 1.0 does not:
- **Proposed probe** `--chart-exposure-tail-rank=R`: exposure flat below rank R (R ≈ 40, the Top 40 radio and
  jukebox break). The Top 10 plateau and the #10-R shape are unchanged, and the mean is renormalised as today.
- First-order with R 40: exposure's #50-100 contribution goes to 0, so the units slope falls by about 0.82 × 1.35 →
  ~1.0. #25-50 flattens partly (the #40-50 stretch); #1-25 is unchanged except through the renormalisation.
- Screen at three seeds × 208 weeks. Read the §12.4 targets, then the guards: §11.19 reign table, Top 10 and Top 40
  counts, debut position (the `RELEASE_RAMP` note ties debut to the bottom slope), Big-5 share and #1 units.

### 12.6 Screen: purchase exposure flat below #40 (2026-11-15) — bottom half fixed, rank dynamics broken; not adoptable alone

Probe `--chart-exposure-tail-rank=R` (`ChartEntryFeedbackProbe`, off by default). A record charted below #R gets #R's
purchase exposure. #1-R, bubbling-under (internal 101+) and uncharted records keep their authored weights exactly,
with no renormalisation; the fixed purse conserves units. Inert: with the switch off, a 27-week run matches `rrl3-v1-2002`
row for row (3,162 lines). Runs `cet40-v2-{2002,1001,3003}`, the candidate plus R = 40, 208 weeks.
- `cet40-v1` was a first build where bubbling-under positions also took #40's weight. It reads the same within noise,
  so those positions were not driving anything.
- Pooled scorer in the session scratchpad; reign columns as §11.19.

| 3 seeds × 208 weeks | history / target | candidate | **tail 40** |
|---|---|---|---|
| units % of #1 at #10 / #25 / #50 / #75 / #100 | (§12.4: #100 4-5) | 53.6 / 26.5 / 9.3 / 4.1 / 2.2 | 53.0 / 27.7 / 11.3 / 6.0 / 3.5 |
| #10 ÷ #100 units; slope #50-100 | 5-11×; 1.0-1.2 | 24.6×; 2.09 | **15.3×; 1.70** |
| #10 ÷ #100 chart points | (Carroll score 11×) | 29.8× | 18.8× |
| #1 median weekly units, #1 lifetime | | 118k, 1.11M | 108k, 1.04M |
| reigns of 1 / 2 / 3 / 4+ weeks a year | 5.8 / 6.2 / 4.4 / 3.6-3.8 | 5.8 / 5.8 / 3.7 / 4.5 | 5.8 / **3.8** / 3.5 / 5.2 |
| chance a reign ends after week 1 / 2 / 3 | 29 / 43 / 54 | 29 / 41 / 45 | 32 / **31** / 40 |
| #1 records/yr, returns % | 19.9, 3.8 | 20.2, 3.6 | 18.8, 3.5 |
| Top 10 records/yr, weeks, one-week % | 106.8, 4.89, 11.1 | 112.8, 4.61, 12.5 | 112.8, 4.63, **15.5** |
| Top 40 records/yr; chart entries per year | 298; 642 | 271; 526 | 273; 565 |
| debut median; debuts at #60 or higher | 90; 2.7% | 88; 6.9% | 86; **11.0%** |
| Big-5 Top 10 share (seed mean) | 22.6 (1961-63) | 30.3 | 28.9 |

- **The bottom moves about halfway**: slope 2.09 → 1.70, #10 ÷ #100 24.6× → 15.3×. That is half the first-order
  estimate (§12.5). New entries are half of the rows at #91-100 and still carry the uncharted exposure from the week
  before. Re-running the §12.5 stage split with the tail applied still gives exposure 0.51 of the #50-100 slope, now
  from that entry step. The rest is the ration (0.40), other conversion (0.42) and local consumption (0.29). The Top 25
  and the purse are unchanged; #1 sales fall 8%.
- **But rank dynamics break.** Early chart paths, median position in chart weeks 0-4:

  | | all records | peak #41-100 | peak #11-40 | peak #1-10 |
  |---|---|---|---|---|
  | history | 90/75/63/52/43 | 93/84/76/70/66 | 85/68/51/41/34 | 80/55/35/21/13 |
  | candidate | 88/74/62/50/42 | 91/83/76/70/68 | 85/68/53/42/36 | 80/59/39/25/14 |
  | tail 40 | 86/67/55/48/41 | 90/76/69/68/68 | 80/59/44/37/32 | 73/46/30/19/11 |

  The candidate's early paths are history's almost exactly. Under the tail, records jump further in their first
  week. Debuts at #60 or higher rise to 11% (history 2.7%), minor records stall at #68 instead of climbing steadily,
  one-week Top 10s rise, and 2-week reigns fall (hazard at week 2: 31% against history's 43%).
- **Why: rank movement is weekly growth divided by the curve's local slope.** A record gaining g in log points a week
  climbs about g ÷ slope-per-rank places. The candidate reproduces history's paths with a slope of 2.1. With the
  bottom at 1.7, the same weekly growth buys ~25% more places. History's chart combined a flatter curve (Carroll's
  score falls at 1.5) with these paths, so history's records grew more slowly per week near the bottom than the
  sim's. The `RELEASE_RAMP` note in `ChartSimulator.cs` made the same observation about debuts.
- **So the steep bottom is compensating for early growth that is too fast.** The two are coupled: a bottom fix alone
  trades a wrong sales curve for wrong rank dynamics, and rank dynamics are what history records directly.

**Verdict.** Do not adopt tail 40 alone. Flattening the bottom needs a matching cut in the early weekly growth of
chart entrants. For the chart-points curve to reach Carroll's 1.5 at the bottom, weekly log growth near entry must
fall roughly in proportion (~30% at the bottom). For units to reach ~1.1, nearly half.

Open: which stage carries the sim's fast entry growth: the release ramp (`RELEASE_RAMP_FULL_WEEK` 6, linear),
awareness growth (chart visibility ladder, awareness floors), regional arrival, or the radio build. Measure the week
0 → 1 → 2 log growth of new entrants by stage, as §12.5 did by rank, before building a second lever.

## 13. The steep bottom is the ration exponent; exponent 1.0 fixes it without touching rank paths (2026-11-15)

Question: get #1 returns and the #41-100 share of the chart inside history's bounds together. Scorers in the session
scratchpad (`shape.py`, `stage.py`, `guards.py`), not kept; `shape.py` reproduces §11.19 and §12.6 for history and the
candidate.

**Bounds.** #41-100 share of the chart total: Hesbacher 1982 weights 21.4%, Carroll 2015 score 26.1%, Haampland 2017
sales 32.0%. #1 returns: 3.6% of spells in 1960-63, 2.8% in 1960-64 (within five weeks: 2.4% / 1.9%).

### 13.1 Cause

The servable (pre-ration) curve is already Carroll's. Lane singles, `single-demand-stages.csv`, seed 2002, geometric
means over 5-rank bands:

| | servable #8 ÷ #98 | units #8 ÷ #98 | units slope ÷ servable slope, #48-98 |
|---|---|---|---|
| he095 (g 1.35) | 11.4× | 26.2× | 1.39 |
| candidate (g 1.35) | 12.3× | 28.7× | 1.36 |
| he095, g 1.0 (`tsg1-v1`) | 11.0× | 11.3× | 1.01 |

- `CHART_EXPOSURE_EXPONENT` 0.62 was fitted at g = 1 so that demand meets Carroll at #100. `--singles-ration-exponent=1.35`
  (2026-11-10) then raised every title to 1.35, which is the whole excess: 24.6^(1/1.35) = 10.7.
- The ration is order-preserving, so it does not change which record outranks which. §12.6 read the steep bottom as
  compensating for fast entry growth. That holds for a change to exposure (a feedback term) but not for the ration:
  growth and slope scale together. Early chart paths are the same at g 1.35 and g 1.0 on both bases.
- The reason for 1.35 has lapsed. It was adopted when #1 median lifetime read 715k with a ~120k weekly ceiling. On
  this bundle at g 1.0 the #1 median lifetime is 0.79M (target 0.80M) and the largest week is 279k.

### 13.2 Screen: candidate + `--singles-ration-exponent=1.0`

Runs `rg10-v1-{2002,1001}` (208 weeks; reference `SimLogs/writing-calibration/rg10-reference-request.json` = the `rrl3`
reference with the exponent changed, no stock-clip probe). Seed 3003 was still running when this was written and is
not in the table. Candidate columns are the same two seeds.

| 2 seeds × 208 weeks | history / bound | candidate (g 1.35) | **g 1.0** |
|---|---|---|---|
| units #41-100 share % | 21.4-32.0 | 17.7 | **25.3** |
| true points #41-100 share % | 21.4-26.1 | 15.2 | 20.7 |
| units #10 ÷ #100; slope #50-100 | 11×; 1.5 (Carroll) | 25.3×; 2.14 | **11.5×; 1.55** |
| units % of #1 at #10 / 25 / 50 / 75 / 100 | 52.4 / 27.4 / 13.6 / 7.9 / 4.8 | 53.8 / 26.6 / 9.3 / 4.0 / 2.1 | 62.8 / 36.7 / 16.3 / 8.7 / 5.4 |
| #1 returns %, return spells/yr | 3.6 (2.8), 0.75 | 3.6, 0.75 | **3.1, 0.62** |
| #1 records/yr, one-week #1 % | 19.9, 23.8 | 20.4, 27.6 | 19.5, 27.6 |
| first reigns 1 / 2 / 3 / 4+ per yr | 5.2 / 6.0 / 4.7 / 3.5 | 5.4 / 6.6 / 3.9 / 4.0 | 5.1 / 5.2 / 4.1 / 4.5 |
| #1 median weekly units, lifetime | (0.80M) | 118k, 1.10M | 76k, **0.79M** |
| path weeks 0-4, all records | 90/75/63/52/44 | 88/74/62/50/43 | 88/74/62/51/43 |
| debut median; debuts at #60 or higher % | 90; 2.6 | 88; 6.6 | 88; 6.8 |
| Top 10 records/yr, weeks, one-week % | 106.2, 4.94, 11.3 | 112.2, 4.62, 12.3 | 117.5, 4.45, 14.0 |
| Top 40 records/yr; chart entries/yr | 296; 650 | 271; 551 | 266; 559 |
| airplay share of Top 10 points % | | 28.6 | 37.4 |
| Hot 100 share of all single units % | | 93.3 | 81.1 |
| Big-5 Top 10 share 1961-63 % | 22.6 | 33.2 | 28.4 |
| label exits over four years (per seed) | | 229 / 248 | 163 / 192 |
| singles cleared ÷ purse | | 1.00 | 1.00 |

- **Both targets are inside the bounds**, with the #1 size on its target and rank paths unchanged.
- **Costs to watch:** Top 10 turnover reads 5 records a year faster (two seeds; on he095 the same change read 4.5
  slower, so this may be noise). Points share is just under the lower bound, because airplay is convex and
  concentrated at the top. Label exits fall about a quarter: off-chart singles go from 7% to 19% of single units.
- Open misses the exponent does not touch: debuts at #60 or higher (6.8% against 2.6%), chart entries (559 against
  650 a year), Top 40 records.

### 13.3 Bottom-only alternative: `--singles-ration-knee-rank=K`

Built in `Systems/SinglesRationExponent.cs` (off by default). The exponent applies among a region's K most-wanted
singles; below the K-th every title clears at one rate, continuous at the knee. Checks `--singles-ration-knee-check`
(`SimTools/SinglesRationKneeChecks.cs`), 12 pass. Inert off: `rkoff27-v1-2002` matches `rrl3-v1-2002` row for row
over 27 weeks (3,162 lines).

Run `rk25-v1-2002`: candidate + K 25, one seed, 104 weeks (read direction only):

| seed 2002, weeks 1-104 | candidate | g 1.0 | knee 25 |
|---|---|---|---|
| units #41-100 share % | 16.6 | 24.6 | 20.6 |
| units #10 ÷ #100; slope #50-100 | 28.4×; 2.19 | 12.4×; 1.59 | 15.2×; 1.61 |
| #1 median weekly units, lifetime | 127k, 1.12M | 84k, 0.90M | 110k, 1.15M |
| path weeks 0-4, all records | 87/74/62/50/42 | 87/74/62/52/43 | 86/72/61/49/41 |
| #1 returns % | 2.4 | 2.6 | 2.6 |
| Top 10 records/yr | 112.0 | 116.0 | 115.5 |

- The knee keeps #1 size and rank paths and flattens the bottom about halfway: 20.6% is just under the lower bound.
  The regional 25th title sits lower on the national chart than #25, so a tighter knee (K 12-15) would be needed.
- It keeps #1 lifetime at 1.15M, above the 0.80M target, which g 1.0 fixes for free.

**Recommendation:** adopt `--singles-ration-exponent=1.0` on the candidate (top 0.92, top rank 10, ρ 0.8, L 0.3). The
knee stays as a probe in case a later year needs the priority back at the top.

**Adopted 2026-11-15.** The candidate reference is now `SimLogs/writing-calibration/rg10-reference-request.json`
(the `rrl3` reference with `--singles-ration-exponent=1.0`; that is the only flag difference). New screens on the
candidate use it as their control; `rrl3` runs stay valid as the g 1.35 comparison.

**Owed:** score seed 3003 (`rg10-v1-3003`, finished, 208 weeks); a decade A/B, since 1.35 was adopted on decade evidence (teen-cohort
persistence, late-decade #1 size); a look at whether fewer label exits is wanted.
