# LP channels and LP genre balance: handoff to Fable (2026-11-15)

## Goal

Two jobs, in this order:

1. **Investigate LP genre balance**: why Folk is about twice its historical share, and where Classical stands. This
   is an investigation first. Find the mechanism, rank the causes, and screen one fix behind its own flag.
2. **Pick up the open LP issues** listed below. Most of them are calibration on top of mechanisms already built.

The aim is historical accuracy of the LP market as a whole: who sells LPs, through which channel, and in what
genre. Don't optimise a single metric.

## Where things stand

Full record: `SimTools/GameyBehaviorAudit.md` §2.4–2.9c (finding 1, 2, 6, 9 and the LP research pass §2.8). Short
version:

**The problem we were fixing.** LP sales were act-blind. Every LP gained awareness every week whoever made it, so an
unknown act's LP sold 15–18k and almost no LP lost money. That act-blind LP income is what kept small labels alive
(finding 6: 24–35% pretax margins). Separately, every hit LP was reissued at budget price 51 weeks after launch, and
those editions outsold the full-price LP by up to 4×.

**Switches built** (all off unless named, all inert when off, byte-checked):

| Switch | What it does | Status |
|---|---|---|
| `--lp-earned-discovery=0` | Weekly discovery scales with how known the act is | Screened, part of package |
| `--lp-earned-launch` | Launch awareness scales with how known the act is | Screened, part of package |
| `--lp-buyer-word-of-mouth=10000` | Word of mouth comes from buyers, not from appeal | Screened, part of package |
| `--budget-noncurrent-weeks=156` | Budget edition only after the LP retires and is 3+ years old | Recommended for adoption |
| `--budget-appeal-saturation=25000` | Budget appeal = `units × K / (units + K)` | Recommended; K open (issue 3) |
| `--budget-rank-by-weight` | Budget slots go to the highest demand weight, not the highest appeal | Screened; overshoots (issue 3) |
| `--lp-demand-scale=1.6` (range now 0.9–2) | Global LP purchase scale that restores volume | Screened |
| `--lp-demand-scale-stores-only` | Scale applies to store intent only | Screened, needed with the scale |
| `--lp-label-demand-parity` | Moves the LP tier constant toward 1 | **Rejected** (§2.4) |

**Best package so far: `lwbr`.** It is all of the "part of package" and "screened" switches above except parity,
on top of `SimLogs/writing-calibration/candidate-request.json`. Runner: `SimTools/run_lp_earned_screen.ps1 -Arms lwbr
-LpScale 1.6 -Reference SimLogs/writing-calibration/candidate-request.json`. Runs `bud-v2-{2002,1001}-lwbr`
(208 weeks) against control `cand-v3-{2002,1001}-off`.
- **Matches the control:**
  - store LPs 65–73M in 1961–62 (Billboard 1961: 68M), with racks also at the control;
  - 1961 gross $468M / $469M;
  - bankruptcies 119 / 139 against 124 / 124 (seed 1001's +15 is read as reshuffle; recheck in the decade A/B).
- **What it changes:**
  - an unknown act's LP sells 12.5–13.8k (control 15.7–17.9k);
  - the hit-to-unknown gap is 3.1–3.4× (control 2.0–2.6×);
  - 1.2–1.4% of LPs lose money (control 0.1%).
- **Not adopted.** It is waiting on issue 3, then the decade A/B on one paired seed.

Nothing in the package is in `candidate-request.json` yet. That is the author's call once issue 3 is settled.

## Issue 1 (main task): Folk over-share

**Numbers.** `SimTools/analyze_lp_genre_channels.py`, seeds 2002 / 1001. "Store" means lifetime store units of LPs
launched that year; rack and budget figures are calendar-year units. Control `cand-v3-*-off`, package `bud-v2-*-lwbr`.

| Folk share | Control | `lwbr` |
|---|---|---|
| Store, 1960 launches | 28.5% / 29.4% | 29.3% / 30.2% |
| Store, 1961 launches | 23.0% / 27.6% | 22.8% / 26.1% |
| Racks 1961 | 27.4% / 37.7% | 25.1% / 32.7% |
| Racks 1963 | 10.7% / 19.6% | 13.7% / 17.3% |
| Budget 1961 | 55.9% / 61.4% | 20.8% / 18.9% |

The package fixes Folk's budget share, which was hit reissues, and leaves its store and rack shares where they were.

**Anchors.**
- Author's year-end benchmark: Folk is about **13% of the album top 50** (memory `folk-bossa-untouched-album-affinity`).
- Kingston Trio, the boom's biggest act, was about 15–22% of Capitol's sales at its 1959–62 peak. Sources conflict
  ([folksongindex](https://folksongindex.fandom.com/wiki/The_Kingston_Trio)).
- So Folk sits at roughly 2× history in stores and higher on racks in 1961.

**What is already known.**
- Folk's album affinity (`MarketRegion.GetAlbumAffinityBaseline`, Folk 0.78, BossaNova 0.72) was never re-tuned when
  the August album-chart pass cut every other album genre. Per-title LP units track `affinity × acceptance` almost
  exactly, so the share that pass freed up piles into whichever affinity was left high.
- Probe `--album-affinity=Folk=0.48,BossaNova=0.40,Jazz=0.30` lowered the share error by 5–9 points every year
  1960–64 on five seeds. It was never adopted. Side effect: the Big 5 earn about 30% of their LP net from Folk, so the
  cut lowers 1961–63 Big-5 share by about 1 SE through label cash. Write-up: `SimTools/LpGenreAllocationFollowup.md`.
- 1960 LP units are demand-bound, so a pure cut lowers total LP units. Move demand between genres; don't remove it.

**What to investigate.**
- Re-measure on the current candidate. The affinity probe predates the retail envelope, racks, budget and the
  earned-demand package.
- Is Folk's over-share in **supply** (Folk LP releases per year: 16% of 1960 titles earlier) or in **demand per
  title** (2× units per title earlier)? Check both on `cand-v3` and `lwbr`.
- Folk racks 1961 run 27–38%. Racks rank catalogue titles by store sales, so a store over-share is amplified on racks.
  Confirm before treating racks as a separate cause.
- The album chart is **empty** in these runs (`*-album-chart.csv` has only a header). The 13%-of-top-50 benchmark
  therefore can't be scored without re-enabling that telemetry. Find why it is empty, perhaps a skip-telemetry or
  lean-probe path. Until then, score on units.
- Re-screen the affinity probe on `lwbr`, and check that the volume freed by cutting Folk goes to the genres history
  had (TradPop, EasyListening, Soundtrack, Classical, Comedy), not to the next-highest affinity. Memory
  `vacuum-filling-vs-sampling-variance` describes that trap.

## Issue 2: Classical is two problems

**Correction to audit §2.8B.** §2.8B said launched Classical LPs sell 4–10× their store units on racks. That is
true only of **artistless LPs**: the soundtrack and cast-album pipeline (`CompetitorManager.ProcessWeeklySoundtrackOrigination`,
`ExternalMediaService.MapGenre`). Those records have no roster act; `album-projects.csv` does not list them, so use
`demand-release-census` for LP universes. They are about 1.5% of LP launches but take:

| Seed 2002 / 1001 | Control | `lwbr` |
|---|---|---|
| Artistless share of launched-LP store units | 4.5% / 3.8% | 5.8% / 5.2% |
| Artistless share of launched-LP rack units | 13.4% / 9.7% | 18.5% / 12.6% |
| Top rack title (rack / store) | Classical 3.49M / 349k; Classical 1.85M / 300k | Classical 1.80M / 92k; TradPop 1.27M / 447k |

- Every run's biggest rack titles are artistless, at 3–20× their store sales.
- The earned-demand switches skip records with no act (`LpEarnedDiscovery`, by design), so the package makes them
  relatively bigger.
- Mechanism (§2.8B): rack demand = shopper pool × genre weight × exposure × quality^2.5, with a per-title exhaustion
  ledger. The catalogue tier keeps steady past-peak sellers, and soundtracks are built to last 200–350 weeks (memory
  `blockbuster-soundtrack-longevity`). A long-lived, high-quality artistless title therefore keeps a rack slot and
  sells to a pool its store sales never touched.
- Anchor: Cliburn's 1958 Tchaikovsky, a No. 1 LP and a national event, took until December 1961 to reach 1M, and
  about 2.5M by 1969. No Classical title should sell 1.8–3.5M on racks in two to three years.

**Second problem: Classical may be *under*-shared overall.**
- Classical is 4–6% of store LP units and 4–12% of rack units.
- Schickel (1960) implies about 20% of record dollars. That figure is unverified. If it holds, Classical as a genre
  is short while a few artistless Classical titles are hugely over-sold on racks.
- Research ask: a sourced figure for Classical's share of 1960–62 LP units or dollars, by channel if possible.
  Budget classics were a real rack category ($1.98, Billboard 1965 via §2.8).

**Fix direction to screen.** Bound a title's rack sales by its store standing: rack units as a share of store units,
or a shared exhaustion ledger across store and rack for artistless titles. Check what `--rack-shared-exhaustion`, in
the candidate, already does for them, because the anomaly survives it.

## Issue 3: budget per-title volume (blocks the package)

Under `--budget-rank-by-weight` with saturation K = 25k, appeal is nearly flat, so `quality^2.5` picks the titles:
- 93% of budget units come from titles with quality above 0.7 (the median issued master is 0.67);
- each edition sells 450–670k in about a year;
- a few sell 10–16× their full-price LP, for example a Soul LP with 40k store units sells 638k at budget.

Two candidate fixes, one arm each:
- (a) bound a title's budget units by a multiple of its full-price store units;
- (b) raise K to about 100k so sales history still counts.

Score: editions that sell, share of budget units from sources with 100k+ store sales (control 55%, `lwbs` 96–99%,
`lwbr` 63%), top-20 median edition ÷ source LP (control 3.8–4.2×), and the maximum ratio.

## Issue 4: retail envelope and budget timing

- `RetailPurchaseEnvelope` gives stores and racks a joint 131.9M LPs a year: the single purse × the period LP:single
  ratio, held before 1966. It binds from 1962 in every arm.
- Under `--budget-noncurrent-weeks` the budget line is opening baskets only until 1963. Budget is then 13% of rack LP
  dollars in 1962, against the NARM 1962 figure of 21%. In 1963 three years of retired masters arrive at once
  (27M) and push stores 6–7M under the control.
- Look at:
  - whether opening baskets should hold more 1960–62 volume, since their appeal is the opening live catalogue
    and they exhaust as one title;
  - whether a fixed envelope that doesn't grow before 1966 is right for 1960–65 (LP dollars grew in that period).

## Smaller LP items (known, not yet worked)

- **LP volume is spread too flat** (finding 2). The top 10% of LPs had 44% of units in the control and have 50–53%
  under `lwbr`. What keeps an unknown act's LP above zero is label-side and act-blind: label reputation × 0.1 at
  launch, label push × 0.03 a week, and the purchase rate (§2.4).
- **LP tier constant** (finding 9). `AlbumSimulator.cs:241` gives Major 1.36 down to Small 0.50. Removing it (`lep`) moved units
  to small labels, so it is offsetting the act-blind terms. Revisit only after the package is adopted.
- **1960 is a warm-up.** Store LPs are 36–40M in 1960 against 61–67M in 1961, because the opening catalogue is
  empty (§2.8D).
- **Soul LP launches in 1960.** Soul has the most LP launches of any genre (memories `soul-album-is-segment-pool-bound`,
  `soul-over-share-predates-rotation`).
- **Clubs** are logged per label only, so no title total includes club units.

## Open research questions

Cite a source and give a range for each.
1. Classical's share of 1960–62 LP units or dollars, and by channel.
2. Billboard's 1960 or 1961 year-end LP list by genre, to set a second Folk anchor beside the author's 13%.
3. Budget LP unit share in 1960–63 (we have NARM 1962: 21% of rack LP dollars).
4. Rack sales of soundtracks and cast albums against store sales (*South Pacific*, *The Sound of Music*, *West Side
   Story*), to bound issue 2's rack-to-store ratio.

## Method notes

- **Scripts** (run from `SimLogs/`, Python 3.14 at `%LOCALAPPDATA%\Programs\Python\Python314\python.exe`; bare
  `python` is the Store stub):
  - `../SimTools/analyze_lp_bundle.py <runs…>`: package metrics.
  - `../SimTools/analyze_lp_genre_channels.py <runs…>`: genre by channel, and artistless titles.
- **Data sources:**
  - Map records to acts through `demand-release-census` (`artist-project-identity` is empty, `record-acts` is
    partial).
  - Per-title store units: `release-outcomes` `pressingBookSold` for retired titles, `live-records-snapshot`
    `totalUnitsSold` for live ones.
  - The rollup's `albumUnits` is stores only.
- **Runs:**
  - Short runs first. 104 weeks is the default; budget timing needs 208.
  - This machine has about 7 GB RAM, so run two worlds at a time. A 208-week pair takes about 70 minutes.
  - `SimTools/run_lp_earned_screen.ps1` takes `-Arms`, `-Seed`, `-Weeks`, `-LpScale` and `-RunPrefix`. Add new arms
    there.
- **Checks:**
  - Prove a switch is inert by byte-comparing a 27-week off run against `cand-v3-2002-off` (68 weekly streams).
  - Native checks: `SimTools/SaveLoadRoundTripRunner.tscn` with `--budget-lp-check` and
    `--lp-demand-calibration-check`.
- **Reading results:**
  - A change of 3–9 points in Big-5 share between single-seed arms is reshuffle noise; so are bankruptcy moves of
    about ±15.
  - Hold the decade A/B (one paired seed) until the end of the investigation.
- Outputs go to `SimLogs/` and are never committed. Record results in `GameyBehaviorAudit.md` (LP mechanics) or a
  new section of `LpGenreAllocationFollowup.md` (genre).

## Deliverable

- A ranked account of why Folk is over-shared on the current candidate, with the mechanism, numbers, and one
  screened fix behind a flag.
- Issue 2's two Classical problems, each with evidence and a proposed fix.
- Issues 3–4 if time allows, each with a screened arm.
- An updated table of switches and verdicts, so the author can decide what goes into `candidate-request.json`.
