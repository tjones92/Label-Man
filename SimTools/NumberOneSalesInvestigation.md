# #1 sales against history

Started 2026-10-30, following `LocalRetailExposureInvestigation.md`. Measurement only: no code changes and no new
simulations. Model data: `lrt52-v1` control seeds 1001-1006 (52 weeks, the regional-fit research profile), with the
λ = 0.3 arm (6 seeds) and the λ = 0.4 arm (5 seeds) from the same batch.

## The question

The retail exposure note twice called the flatter top "the wrong direction for a model whose #1s already sell under
history". No history target backed that claim. `DemandAttentionResearch.md` §1 had already found that the code's
150,000 weekly copies at #1 has no source. This note sets out what history supports, what the model does, and which
metric can tell the two apart.

## 1. What history supports

| Source | What it measures | Evidence for 1960-64 #1s | Confidence |
|---|---|---|---|
| Murrells, *Million Selling Records* (year pages at ratherrarerecords.com) | Manufacturers' claims to the trade and general press, unaudited, often worldwide | Million sellers: 14 of 15 #1s (1960), 20 of 20 (1961), 20 of 20 (1962) | Upper bound |
| RIAA gold at the time (1M units, audited, at the label's request) | Audited US units, for labels that asked | Cash Box, 22 Feb 1964: two gold singles in 1963 (*Hey Paula*, *Sugar Shack*). Of the 1961 #1s: *Big Bad John* (14 Dec 1961) and *The Lion Sleeps Tonight* (9 Jan 1962) | A floor: most labels, RCA included, did not certify |
| RIAA catch-up audits of Elvis Presley's RCA singles | Cumulative US units, about 30 years on | 27 Mar 1992: *It's Now or Never* and *Surrender* certified platinum (1M). *Stuck On You* and *Good Luck Charm* certified gold (500k), with no platinum noted that day. All four are platinum today | Audited, but the era's biggest act, with three decades of catalogue sales |
| Billboard International Buyers Guide | US single units, 1961 | 182M: stores 92M, racks 40M, jukeboxes 50M. The 1969 directory revised 1966 down by about a third | Soft |

Sources: [1960](https://www.ratherrarerecords.com/pop-charts-1960/), [1961](https://www.ratherrarerecords.com/pop-charts-1961/),
[1962](https://www.ratherrarerecords.com/pop-charts-1962/) #1 pages (Murrells counts and RIAA dates);
[Cash Box 1964-02-22](https://worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1964/CB-1964-02-22.pdf);
[Elvis Presley singles discography](https://en.wikipedia.org/wiki/Elvis_Presley_singles_discography) (current
certifications); market totals as in `DemandAttentionResearch.md` §2.

**Read:** a typical early-1960s US #1 sold something like 0.5-1.5M copies. Labels claimed a million for nearly every
one. The audits that exist put two of the era's top act's #1s between 500k and 1M after 30 years. No source gives a
distribution of US sales across #1s.

## 2. What the model does

| | Model control, 6 seeds | History |
|---|---:|---|
| #1 records per year | 22.8 (19-28) | 19, 22, 21, 21, 23 in 1960-64 (Hot 100 file; a record #1 across New Year counts in both years) |
| #1 lifetime units, records that finished selling: median (IQR) | 0.80M (0.54-1.03M), n = 71 | ~0.5-1.5M; no distribution |
| ...share at 1M or more | 27% | 93-100% by claim; few audited |
| ...share at 500k or more | 77% | |
| #1 peak-week units, median | 98k | 150k in code, unsourced |
| Single units in the year | 120M | 182M (1961 estimate, soft) |

The model's single market is about two thirds of the 1961 trade estimate. If the million bar is scaled to market
size, it sits at about 0.66M, and 65% of model #1s clear it (48% at 0.81M).

**Read:** the model's #1s sit inside the historical bracket. In absolute units they fall short of the label claims
(27% against nearly all at a million). The claims are unaudited and include foreign sales, and the model's whole
singles market is smaller than the trade estimate. History does not establish a deficit at #1. If a level gap exists,
it is at most about 1.25x on the median, and most of it is market size. "Total single units" is a calibration that
must not move, so a gap of that kind is not a concentration fix.

## 3. The million-seller count measures the warm-up

**Runway.** Of the closed #1 records launched live (n = 52), the mean record has sold 85% of its lifetime units by
age 12, 96% by 16 and 99% by 20. A 1M bar at 12 weeks is about 1.18M of lifetime, roughly the top 15% of #1s. That
is why the 12-week cohort count is 1.8 per seed.

**Warm-up.** The first live cohort has a much bigger top than the rest of the year:

| Weeks | #1 units, weekly median | Market units a week, incl. LPs |
|---|---:|---:|
| 1-4 | 40k | 1.54M |
| 5-8 | 121k | 2.00M |
| 9-13 | 162k | 2.67M |
| 14-26 | 105k | 2.97M |
| 27-39 | 85k | 3.37M |
| 40-52 | 90k | 3.71M |

Of the control's 7.5 lifetime million sellers per seed, 5.5 were released by week 10 (prewarm releases included), 2.0
in weeks 11-32, and none later. So the `liveOver1M` count and the 12-week cohort count mostly measure the warm-up
transient plus a thin tail. Their bar sits at the 73rd percentile of #1 lifetimes. The likely cause, not tested
here, is that the first live releases meet a field of weak prewarm records. The game's first year starts the same way.

## 4. λ on steady-state metrics

Seed-paired deltas against the control, with SE. Steady state means records released in weeks 11-32 (at least 20
weeks of follow-up), and chart weeks 14-52.

| Metric | Control | λ = 0.3 (n = 6) | λ = 0.4 (n = 5) |
|---|---:|---:|---:|
| #1 lifetime, releases in weeks 11-32, median | 781k | -6k (69k) | -36k (41k) |
| #1 weekly units, weeks 14-52, median | 93k | -0.5k (8.0k) | +0.0k (4.8k) |
| #1 records released in weeks 11-32 | 9.7 | -0.17 (1.94) | -0.80 (0.97) |
| Releases in weeks 11-32 at 750k or more: #1s | 5.50 | -1.33 (1.12) | -1.60 (0.93) |
| ...not #1 | 2.17 | -1.33 (0.42) | -0.80 (0.37) |
| Million sellers released by week 10 | 5.50 | -0.50 (0.62) | -0.60 (0.98) |
| Million sellers released in weeks 11-32 | 2.00 | -0.50 (0.43) | -0.60 (0.24) |
| Top 1% of live singles, unit share, % | 27.77 | -1.84 (0.90) | -2.02 (0.67) |

**Read:** λ does not make the typical #1 sell less. The median #1 and the #1's weekly units do not move. λ trims the
upper tail: fewer records at 750k or more, about half of them #1s, and 1.8-2.0 points off the top 1%'s unit share
(2-3 SE). The flatter top is real, but it sits on a #1 level that history does not show to be low.

**Eight seeds (`lrt52-8s`, 2026-10-30) confirm it.** At λ = 0.3 and 0.4 the steady #1 median moves -29k (SE 53k) and
-26k (25k), and #1 weekly units from week 14 move -0.4k and +1.0k. Steady releases at 750k or more fall by 2.75 (1.01)
and 2.00 (1.04) of 7.5. The top 1%'s share falls by 1.60 (0.67) and 1.77 (0.43) points. See
`LocalRetailExposureInvestigation.md` §5.

## 5. Metrics to use from here

- **#1 level:** lifetime units of the records that reach #1 (units by age 20 capture 99%). In 52-week runs, use
  releases in weeks 11-32, about ten #1s per seed. In multi-year runs, use every year after the first. Add #1 weekly
  units from week 14.
- **Concentration:** the top 1%'s unit share of live singles (already in `analyze_local_retail_exposure.py`), and
  the count at 750k or more among steady-state releases.
- **Drop** fixed-1M counts over windows that include weeks 1-13. They are small, threshold-bound and dominated by
  the warm-up.

## 6. What would change this

- **A history distribution.** Dated contemporary trade reports of US sales for a sample of 1960-64 #1s (Billboard
  and Cash Box items saying a record has passed the million, label advertisements, RIAA dates) would separate
  audited from claimed. Without them the target stays a bracket.
- **Market scale.** If 1960 single units were nearer 180M than 120M, the absolute gap is market size. That is an
  accepted calibration, not this question.
- **Multi-year runs.** A 265-week run would give the steady-state #1 level year by year, against 1960-64 history.

## Conclusions

1. **The premise is not supported.** No sourced target said the model's #1s undersell. Against the evidence there
   is, a median of 0.80M with 77% over 500k sits inside the bracket. It is short of the label claims only in absolute
   units, and only about as far as the market is.
2. **Million-seller counts in 52-week screens mostly measure the warm-up.** 73% of the control's million sellers
   come from releases in the first ten weeks, when #1 weekly units are 1.5x their later level.
3. **λ trims the upper tail, not the typical #1.** On steady-state metrics, the cost to keep in view is the top 1%'s
   share (-1.8 to -2.0 points) and the 750k-plus tier. It is not the #1 level.
4. **No #1 fix is indicated.** The λ eight-seed screen no longer needs to wait for one. Run it with the section 5
   metrics.
