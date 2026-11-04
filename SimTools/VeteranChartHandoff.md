# Handoff: the chart's top ages too slowly (beyond `--hit-history-signing-recovery`)

Written 2026-11-04 at the end of the media-atmosphere branch, for the next (player-side / celebrity tiers) branch.
Every number here comes from existing decade runs; no new runs were made for this note.

## The finding

Both the model and the recovered Billboard Hot 100s (`SimLogs/historical-hot100-1960-64/hot100_weekly.csv`, 1960-64,
all 100 positions; `SimLogs/historical-top10-1968/records_1968_grouped.csv`) start in 1960, so "acts first seen in
1960" means the same thing in both. Model runs: `lpgate-decade-market-2002` and `branch-end-decade-market-1001`
(the branch-end flag set, Recovery on).

| | History | Model 2002 | Model 1001 |
|---|---|---|---|
| 1964 Hot 100 slot-weeks, acts first seen 1960 | 20% | 32% | 34% |
| 1964 Hot 100 slot-weeks, acts first seen 1963-64 | 56% | 46% | 43% |
| **1964 Top 10 slots, acts first seen 1960** | **8%** | **33%** | **54%** |
| 1968 Top 10 slots, acts on the chart 1960-64 | 28% | 63% | 40% |
| 1960 acts still charting in 1964 | 18% | 21% | 21% |
| 1964 weeks per charting act: 1960 cohort | 18.1 | 20.3 | 20.9 |
| 1964 weeks per charting act: new acts (1963-64) | 12.2 | 8.9 | 7.8 |
| Acts debuting 1961-62 (count) | 450 | 432 | 430 |
| ...of those charting two years later | 18% | 16% | 19% |

## What it means

- **New acts arrive and survive at historical rates.** Debut counts, two-year survival and years charting all match.
- **New acts' hits are too small.** They hold 8-9 weeks each against history's 12, and they rarely reach the Top 10.
- **The Top 10 belongs to the 1960 cohort.** It holds 33-54% of 1964 Top 10 slots against 8% historically. That cohort's
  survival (21% vs 18%) and Hot 100 weeks per act (20 vs 18) are only a little high. The excess is concentrated
  at the top: established acts get the big hits.
- **Recovery is a minor part.** In 1964 its re-signings have barely started. In the 1968 Top 10, only 16% (seed 2002) to
  32% (1001) of veteran slots are on a label other than the act's first. Recovery also closes a real gap (Y+3
  persistence of top-40 acts 22% → 29%, history 31-32%; `MediaTvHeadlinerBookingDesign.md`), so tune it rather than
  remove it.

So it's systemic. Fresh talent builds careers at the right *rate*, but can't win the *big* hits, because whatever
decides who reaches the Top 10 favours an established act.

## Suspects, in the order I'd check them

1. **Launch power keyed to standing.** Label push, launch stock and awareness scale with the act's recognition or
   career state. With airplay convexity (k=5), a small edge becomes a large Top-10 share. Memory notes:
   `top10-cr4-is-major-push-premium`, `airplay-convexity-amplifies-label-push`, `big5-entry-edge-is-campaign-impact`,
   `artist-fame-cut-favors-label-push`.
2. **Incumbent snowball.** `head-scale-senior-snowball`, `senior-excess-is-cold-return`, `artist-momentum-never-decays`,
   `--senior-cooling` and `--artist-momentum-half-life=52` (both in the profile). Check whether cooling acts on chart
   access at all, or only on Hot 100 presence.
3. **Opening careers.** The 75 opening archetypes (`opening-careers-replace-canopy`) and `--historical-independent-stars`
   seed 1960 with strong acts. Compare their Top 10 share with procedural 1960 debutants.
4. **The career ladder's first rung** (`career-ladder-first-rung-is-unreachable`, 0.51% Rising). If a new act can't
   climb, it never gets the launch treatment a hit act gets.
5. **Recovery** (`Systems/RosterManager.cs`, `NominateFromDailySnapshot`, the `HitHistorySigning.RecoveryCompetes`
   block). It ignores how long ago the hit was. Fade the hit history's pull with years since the last hit.

## Method

- **First run:** one decade, seed 2002, Recovery off (~1 hour since the slate index), against
  `lpgate-decade-market-2002`. That sizes Recovery's share directly. One paired seed is the standard.
- **Measures:** the table above, by year. Use `records.csv` chart rows (positions 1-100); `lifecycles.csv` holds only
  closed records. Scripts that produced the table are easy to rebuild: normalise artist names (lower case, drop a
  leading "the", `&` → "and", strip punctuation), clip history's 1959-12-28 week to 1960.
- **Then:** for the Top 10 records in 1963-64, compare the 1960 cohort with new acts on launch push, launch stock,
  launch recognition, label tier, radio heat and quality (all in `records.csv`). Whichever launch input separates
  them is the lever.
- **Celebrity tiers interaction:** if the new tiers gate launch power or bookings on fame, they could make this worse
  (fame is incumbent-held) or better (if the tiers let a breakout act climb fast). Re-run the table after they land.

The 1964 Top 10 measure is the headline: history 8%, model 33-54%.
