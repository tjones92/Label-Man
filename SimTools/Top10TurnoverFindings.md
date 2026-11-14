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
