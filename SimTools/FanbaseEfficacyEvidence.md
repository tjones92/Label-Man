# Fanbase model: what a following actually does, and where it is gamified

Status, 2026-11-10. Static reads of completed runs and of current source. No simulation was run and no gameplay
source changed. Continues `VeteranDemandStackEvidence.md`. Written as a handoff.

Runs read: the two follower-on decade controls, `stature-marketing-decade-v2-{2002,1001}-control`. Figures are given
as seed 2002 / seed 1001 unless one seed is named. History is the recovered 1960–64 Billboard Hot 100 in
`SimLogs/historical-hot100-1960-64/`.

## Answers to the three questions

**Is the fanbase model ineffectual?** No. It is strong in the wrong place. On singles a follower is nearly inert:
the largest following in the run (2.4M people) bought about 46k copies of a new single in its first week. On LPs
the same followers buy in volume, the LPs chart, and the charted LPs heat the act's later singles for two to three
years. The earlier document measured that route. So the following works as a slow album-catalogue engine, where the
1960s fan it was designed to represent was a teenager buying a 45 in release week.

**Can the model rush 200k pressings for a hot single?** It presses them and cannot sell them. Followers ask for up to
909k copies of one single and the dealers are handed every copy, free. Then 80–90% of those copies go unsold in the
launch window. After 1960 no single in either seed sold more than 121k in any week of its life, and the largest
launch week was 46k / 43k.

**Does a 1960 doo-wop group sell in 1969 because its LP backlog is hot?** Not the cold ones, but very few go cold.
A 1960–61 act with no Top 40 single in three years and no active album hit charts about like an unknown. The
problem is the other 60%: acts that first charted in 1960–61 as doo-wop, rock and roll or teen pop still place
22–24% of their singles in the Top 40 in 1968–69, the same rate as in 1964–65. Their heat is album memory: in about
70% of those launches the act has no LP on the album chart at all.

## Findings

### 1. A follower's single purchase is paid at ten cents on the dollar

`MediaFollowing.AdvancePerFollower` gives a young follower 0.35 copies of a new single and an adult 0.10. Those
copies are added to the record's serviceable intent (`ChartManager.cs:1429–1438`) and then cleared with everything
else through the regional singles purse, which pays out 10–15% of intent. The rates were written as real copies and
are spent as intent.

Effective purchase per follower, 1964 clearing (singles 0.106, LPs 0.593):

| | New single | New LP |
|---|---:|---:|
| Young follower | 3.7% | 7.1% |
| Adult follower | 1.1% | 17.8% |

A teenage follower is twice as likely to end up with the act's LP as with its 45. The period ran the other way: the
45 was the teenage purchase and the LP the adult one. By 1969 LP clearing has fallen to 0.233 and the two formats are
level for the young (3.3% single, 2.8% LP).

The largest single books of the decade, seed 2002 (two per act at most):

| Year | Act | Followers | Book | Advance copies stocked | Week-one units (entry) | Three-week units | Peak | Lifetime |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| 1969 | The Fresh Rockers | 2.36M | 909k | 909k | 46.1k (18) | 154k | 4 | 844k |
| 1969 | Chester Rogers | 2.04M | 659k | 659k | 29.8k (33) | 62k | 33 | 61k |
| 1967 | Del Toliver | 1.87M | 608k | 608k | 34.0k (34) | 79k | 34 | 109k |
| 1963 | Lulu Powell | 1.82M | 577k | 577k | 36.6k (26) | 89k | 26 | 135k |
| 1967 | Melanie Fowler | 1.54M | 500k | 500k | 35.4k (24) | 96k | 2 | 722k |

Week-one units are the book's 50% first-week share times the clearing ratio: 909k × 0.5 × 0.10 ≈ 46k. Seed 1001 is
the same shape (largest book 670k, week one 37k). An act with two million followers whose single is not a hit sells
53–61k copies in total, about 3% of its following.

### 2. No single has a big week, at launch or later

| | Seed 2002 | Seed 1001 |
|---|---:|---:|
| Largest launch week, 1961–69 | 46.1k | 42.6k |
| Launch-week units of singles charting in week one, median | 2.4k–5.2k by year | 3.9k–5.1k |
| No. 1 single, mean weekly units by year | 41k–64k | 34k–70k |
| Largest week by any single, 1961–69 | 121k | 110k |
| Record-weeks above 200k, 1961–69 | 0 | 0 |

The two record-weeks above 200k in 1960 are opening-state records. From memory and not checked this session,
"I Want to Hold Your Hand" sold about 250k in three days and a million inside three weeks, and "Can't Buy Me Love"
had about two million US advance orders. The model's ceiling is an order of magnitude under that at every level:
the fixed purse spreads about three million singles a week across every title in proportion to intent, and a
committed buyer is rationed exactly like a browser.

### 3. Advance stock is free, and most of it is never bought

`StockAdvanceOrders` adds the whole three-week book to `unitsInStores` in every region the record reached, for AI
labels only. The pressing-inventory switch is not in these runs' flags, so the copies cost nothing and are never
returned. The player gets the intent and no copies.

Seed 2002, 1969: 67.3M advance copies stocked. Followers bought about 3.3M singles (35.0M granted × 0.095) and 7.3M
LPs. About 84% of the advance stock was never sold to the people it was stocked for. The stock does one thing: it
raises the title's physical limit so the book can be granted in full (asked ≈ granted in every year). The earlier
no-stock arm showed what that buys, which is a week-one chart entry and no change in Top 10 rate.

### 4. The fan pulse lands the record at 18–40 for a week and then lets go

Single books by size, launches 1962–69:

| Book | n | Median followers | Charted in launch week | Top 40 in launch week | Reached Top 40 | Reached Top 10 | Median weeks to peak (Top 40 records) |
|---|---:|---:|---:|---:|---:|---:|---:|
| 10k–50k | 4,180 / 3,904 | 97k / 100k | 6% / 5% | 0% | 14% / 15% | 7% | 9 |
| 50k–100k | 712 / 848 | 310k / 284k | 75% / 66% | 0% | 27% / 25% | 17% / 15% | 8 |
| 100k–250k | 397 / 626 | 540k / 655k | 99% / 100% | 2% / 1% | 45% / 46% | 32% / 31% | 8 |
| 250k+ | 173 / 109 | 1.44M | 100% | 61% / 63% | 77% / 75% | 41% / 33% | 6 / 5 |

No book of any size put a single in the Top 10 in launch week. Of the fifteen largest books in each seed, nine
(2002) and eleven (1001) peaked in their launch week and sold little beyond their first three weeks. The book is one
50/30/20 pulse; after week three the followers have no further effect on the record.

Debut to peak for Top 10 singles, 1960–64, measured the same way on both sides:

| | n | Median weeks | Peaked within 4 weeks | Debuted in Top 40 |
|---|---:|---:|---:|---:|
| Billboard, all | 504 | 7 | 9.3% | 1.6% |
| Billboard, acts with 5+ Top 10s | 115 | 6 | 18.3% | 5.2% |
| Model, all | 603 / 612 | 7 | 4.8% / 3.1% | 6.3% / 4.6% |
| Model, acts with 5+ Top 10s | 197 / 216 | 7 | 0.5% / 0.0% | 18.3% / 11.6% |

The overall pace matches history. The star shape is inverted. Historical stars debuted modestly and climbed fast;
the model's stars debut high on fan copies and then climb at the same seven-week pace as everyone else. Fan sales
buy a chart position and nothing that follows from one.

### 5. Cold veterans are not carried; veterans simply do not go cold

1968–69 single launches by acts that first charted in 1960–61:

| Group | Launches | Acts | Quality | Recognition | Heat at launch | Book | Top 40 | Top 10 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Top 40 single in the last year | 274 / 255 | 87 / 69 | 0.76 / 0.77 | 0.75 / 0.81 | 0.34 / 0.37 | 91k / 106k | 30.7% / 36.1% | 16.8% / 22.7% |
| Top 40 single 1–3 years ago | 155 / 152 | 78 / 79 | 0.74 | 0.64 | 0.25 | 43k / 48k | 26.5% / 20.4% | 12.9% / 10.5% |
| None in 3+ years | 239 / 264 | 108 / 116 | 0.71 | 0.31 / 0.34 | 0.12 / 0.13 | 12k / 13k | 10.5% / 6.4% | 5.4% / 2.3% |
| of which no active album hit | 163 / 184 | 90 / 106 | 0.70 | 0.26 / 0.28 | 0.00 | 11k | 5.5% / 6.5% | 3.1% / 1.6% |
| of which 2+ active album hits | 67 / 74 | 46 / 43 | 0.73 / 0.71 | 0.43 / 0.51 | 0.40 / 0.45 | 14k / 17k | 22.4% / 6.8% | 11.9% / 4.1% |
| Acts first charted 1966+, Top 40 in the last year | 376 / 333 | 164 / 141 | 0.74 | 0.25 / 0.28 | 0.12 / 0.15 | 16k / 25k | 14.1% / 15.0% | 7.4% / 8.7% |
| No chart history | 4,874 / 5,165 | | 0.68 | 0.02 | 0.02 | 0.5k | 3.6% / 3.2% | 1.6% / 1.4% |

- A cold 1960–61 act with no album hit is close to an unknown. The backlog alone does not sell its 1969 single.
- Whether album hits rescue a cold act is unresolved. The seeds disagree (22% and 7% Top 40) on about 70 launches each.
- An act that had a Top 40 single 1–3 years ago charts better than a newer act that had one in the last year
  (26.5% / 20.4% against 14.1% / 15.0%). Recognition is 0.64 against 0.25–0.28 and heat 0.25 against 0.12–0.15.
- In every group that reads album heat, 68–86% of launches come from acts with no LP on the album chart that week.
  The heat is retained peak credit. LPs stay in the active set for up to about three years: the week-522 snapshot
  holds 6,528 records, of which 5,410 are LPs (2,942 under a year old, 1,735 aged 1–2, 682 aged 2–3, 51 older) for a
  200-place chart.

The teen cohort of 1960 does not age off. Acts whose first charted record was doo-wop, girl group, rock and roll,
rockabilly or teen pop, and that first charted in 1960–61:

| Launch years | Launches | Acts | Recognition | Book | Top 40 | Top 10 |
|---|---:|---:|---:|---:|---:|---:|
| 1964–65 | 395 / 368 | 94 / 94 | 0.49 / 0.54 | 71k / 78k | 23.5% / 26.9% | 14.9% / 16.3% |
| 1966–67 | 267 / 265 | 66 / 77 | 0.58 / 0.61 | 85k / 82k | 24.7% / 23.0% | 16.1% / 12.8% |
| 1968–69 | 220 / 195 | 63 / 59 | 0.60 / 0.62 | 79k / 93k | 24.1% / 22.1% | 13.2% / 11.3% |

About sixty such acts are still releasing a hundred singles a year in 1968–69, with larger follower books and higher
recognition than they had in 1964, and an unchanged hit rate. From memory, the real cohort was nearly gone from the
Top 40 by then, with a handful of comebacks. This table is the clearest single statement of the veteran excess.
These acts may have changed genre since 1960; that is not tracked here.

## Where the model departs from the period's economics

| # | What the model does | What the period did |
|---|---|---|
| 1 | Follower copies are intent, rationed at 10% with all other singles intent. | A fan's purchase was the committed one. It came out of the same pocket money but came first. |
| 2 | AI dealers receive the whole book in free copies; 84% are not bought by followers; nothing is returned or charged. | Advance orders were pressed, shipped and paid for, and over-shipping came back as returns. |
| 3 | A young follower is likelier to get the LP than the 45. | Teenagers bought singles; LPs were the adult format until late in the decade. |
| 4 | One three-week pulse per release. Followers do nothing afterwards. | Fan sales in week one drove dealer reorders and station adds, which is how a star record climbed in three weeks. |
| 5 | Stars debut higher than history and climb no faster than anyone. | Stars debuted modestly and climbed fast. |
| 6 | Heat is a memory of album peaks that lasts as long as the LP stays active, about three years, on or off the chart. | A current hit album sold its singles. A three-year-old one did not. |
| 7 | Recognition and following for the 1960 teen cohort rise through the decade. | Teen audiences aged out and took their idols with them. |
| 8 | Followers' own purchases count as new joiners (earlier document, finding 5). | |

Rows 1, 3 and 4 make the following weak on singles. Rows 6, 7 and 8 make it strong and permanent on albums and
career stature. Fixing only the first group would hand the 1960 cohort more of the singles chart: followers were
granted 33–39M single copies a year against a purse of roughly 150M, so paying them in full would give followers
about a quarter of all singles sold, most of it to the acts with the oldest stock.

## Actions

Order matters. Shrink and age the stock before making it buy.

| # | Change | Why | Test |
|---|---|---|---|
| 1 | Decide what a follower is. Either a die-hard (buys about one release in three; the top act holds a few hundred thousand) or a casual (millions; buys 3%). The stock is sized like the second and rated like the first. | Finding 1. Every later calibration depends on this. | Author decision; no run. |
| 2 | Take the earlier document's actions 2 and 3: singles read current albums only (`--single-artist-heat-current-albums`), and follower purchases stop recruiting followers in both formats. | Finding 5 here: 68–86% of album heat is read with no LP on the chart. | As specified there. Add the teen-cohort table above; expect the 1968–69 Top 40 rate to fall below the 1964–65 rate. |
| 3 | Log, observe-only, the following and recognition of the 1960–61 teen cohort by year, split young, adult and aged-out transfers. | Finding 5: books and recognition rise through 1969. Half of each aged-out young cohort becomes adult followers, who drift at 8% a year. That transfer is the suspect and is not measured. | Byte-identical economy outputs. Read the split before changing a rate. |
| 4 | Clear follower single copies ahead of the proportional ration, inside the same regional purse, stock and shelf limits. | Findings 1 and 2. This is what makes a 200k week possible. | Only after 2. 104 weeks, seed 2002, same-build control. Expect week-one units near half the book for the largest books; total single units unchanged; report Top 10 share by cohort and repeat Top 40 share. Kill it if the 1960 cohort's Top 10 share rises. |
| 5 | Set the young rates so the 45 leads in effective terms after clearing, by year. | Finding 1 table. | With 4. Report follower units by format and audience against the table. |
| 6 | Make advance stock a dealer order: charged to the label's pressing book, sized to expected sell-through, unsold copies returned. | Finding 3. Also removes an AI-only advantage over the player. | Observe-only first: unsold advance copies per label-year. Then a same-build arm with the pressing-inventory switch on. |
| 7 | Find out whether week-one fan sales reach station adds at all. | Finding 4. If they do not, action 4 produces a tall first week and the same seven-week climb. | Observe-only: radio heat by week for 250k+ books against book-less singles of equal quality and label tier. |

## What this does not show

- Every table is a read of worlds that already ran. Nothing here re-ranks a chart under a changed rule.
- Release hype (`--event-hype`) was on in both runs and is not isolated. The finding that launch does not make hits
  concerns advance stock and the fan pulse. Launch awareness does matter: older hot acts launch at 0.73 awareness
  and reach the Top 40 at twice the rate of newer hot acts.
- The historical unit and comeback figures in findings 2 and 5 are from memory. The Hot 100 pace comparison is measured.
- History on disk ends in 1964, so the 1968–69 cohort tables have no measured historical counterpart.
- `records.csv` holds charted and bubbling-under records only. Launch weeks of singles that did not chart are missing,
  so launch-week medians are for charting records.
- The album chart and weekly summary exports are empty in these runs. Album-chart status comes from the heat
  attribution log, and LP ages from the final snapshot.
- Source holds a second fan system, `ArtistFanbase` (`--artist-fanbase=`). It was not in these runs' flags and is
  not evaluated.

## Reproduce

```powershell
$py = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe'
& $py SimTools/analyze_fanbase_efficacy.py 2002
& $py SimTools/analyze_fanbase_efficacy.py 1001
& $py SimTools/analyze_fanbase_climb_speed.py 2002 1001
```

Saved outputs are in `SimLogs/fanbase-efficacy/`.
