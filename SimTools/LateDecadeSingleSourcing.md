# How 1965-69 hit singles were sourced, and a late-decade advance prior

2026-10-20. Follows [LpGenreAllocationFollowup.md](LpGenreAllocationFollowup.md) §5.1 and the era-neutral priors in
[AlbumReleasePracticeResearch.md](AlbumReleasePracticeResearch.md). Part of the release-judgement work; no decade run
here (the author is saving those for the release-judgement branch). One opt-in flag was added,
`--album-practice-late-advance=K`. It is inert unless named and changes nothing before 1965, even when named
(seed-6006 52-week replay with the flag on: 87/87 CSVs byte-identical to `apr8-opening-6006-52`).

## Summary

- **The historical shift is real but moderate.** In a dated census of the Billboard year-end top 20, singles issued
  with or ahead of an LP that has its own title are 10% of 1963 hits, 17.5% of 1965-66 and 22% of 1967-69. Singles
  pulled from an LP already on sale are 15%, 7.5% and 18%. Single-led hits (the single first, then a rush LP titled after
  it, or no LP at all) fall from 75% to 57%.
- **The model's late miss is the advance route, not extraction.** In the best decade arm (`dec-lpa4-youth`, 1965-69)
  advances are 7% of Top 10 weeks against about 22% in the census, while extractions are 16% against 18%.
  The ~30 extractions a year look too few against 5,000 LPs, but each one is a large hit (2.5-2.7 Top 10 weeks), so
  their share of hits is already historical.
- **The late Big-5 deficit is a volume problem, and a route fix will not close it.** Late Big-5 single launches are
  about 228 a year in the pre-bundle reference (195 of them LP promos) against about 128 in the bundle. The census does
  not support restoring a promo on every LP. The Big-5 hits are 57% LP-sourced against 46% for independents, about one
  SE apart. Big-5 labels make only 3.6% of late LP commitments, so a tier-neutral advance prior mostly reaches
  independents. The census Big-5 share of the year-end top 20 does rise late (20 / 25 / 20 / 40 / 35%, 1965-69),
  which fits the late-consolidation goal. But it is not on the Top 10 benchmark's scale: for 1963 it reads 50%
  against that benchmark's 26.0. So it shows direction only, and the late Big-5 level stays with the consolidation
  mechanism (§5), not with release practice.
- **Change:** an opt-in era term on the advance commissioning prior. From 1965 it ramps over three years to K times
  the institutional value and drops the dependence on singles orientation. The 1965-67 resume screen puts K=2 slightly high (27% of 1967 Top 10 weeks against ~22%), with share and slot
  error flat. The decade arms are K=1.5 and K=2.

## 1. Census method

Sample: positions 1-20 of the Billboard Year-End Hot 100 for 1965-69 (100 records), plus 1963 as an early baseline.
Year-end lists were taken from Wikipedia. Each record was classified from its single date, its parent LP and that
LP's date, and the LP's title. Dates come from discography knowledge, checked against Wikipedia song and album pages
for the uncertain cases. Six 1965-69 rows are marked low confidence because no source gave the LP date. Data:
[`data/hot100-1965-69-single-sourcing.csv`](data/hot100-1965-69-single-sourcing.csv) (1963 is in §2 only).

| Code | Route | Model route |
|---|---|---|
| A-plan | single issued with or up to ~8 weeks before an LP that has its own title | advance single |
| A-title | single followed within ~8 weeks by an LP titled after it | orphan single, then a hit-led LP |
| H | single first; LP more than ~8 weeks later or built around it | orphan single, then a hit-led LP |
| E | single pulled from an LP already on sale | post-LP extraction |
| N / S | non-album single / film or cast cut | orphan / soundtrack |

The split between A-plan and A-title matters. Classified loosely ("single just before an LP"), 1963 looks the same as
1967-69 (40% against 42%), because rush LPs titled after a hit were already routine in 1963. The title test separates
a planned LP programme from an LP assembled after a hit. It is still a proxy for intent; the research doc's
definition (a cut issued to promote an already-planned LP) cannot be read from dates alone.

Limits: these are hits, not a sample of all singles; n=20 per year (one record = 5 points); the 1963 baseline is one
year; Big-5 membership follows the simulation's five (Columbia/Epic, RCA Victor incl. RCA-distributed Colgems,
Capitol incl. Apple, Decca/Uni, Mercury/Philips/Smash/Fontana), so MGM, Warner/Reprise and London count as
independents.

## 2. Census results

| Top-20 hits | 1963 | 1965 | 1966 | 1967 | 1968 | 1969 | 1965-66 | 1967-69 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| A-plan | 2 | 3 | 4 | 4 | 4 | 5 | 17.5% | 22% |
| A-title | 6 | 1 | 2 | 4 | 6 | 2 | 7.5% | 20% |
| E (from a released LP) | 3 | 2 | 1 | 3 | 4 | 4 | 7.5% | 18% |
| H | 9 | 12 | 13 | 8 | 5 | 7 | 62.5% | 33% |
| N / S | 0 | 2 | 0 | 1 | 1 | 2 | 5% | 7% |
| Big-5 share | 50% | 20% | 25% | 20% | 40% | 35% | 22.5% | 32% |

1963 classification: A-plan *Can't Get Used to Losing You* (Columbia; LP *Days of Wine and Roses*) and *Walk Like a
Man* (Vee-Jay); E *Blue Velvet* (from *Blue on Blue*), *Fingertips* (from *The 12 Year Old Genius*; Billboard's own
1963 report, cited in the earlier research), *Puff* (from *Moving*); the rest A-title or H.

- **Extraction (E) cases are the classic late-decade examples**: *Light My Fire* (edited from the debut LP),
  *Respect* (second single from *I Never Loved a Man*), *Never My Love* (after *Windy* from *Insight Out*), *Sunshine of
  Your Love*, *One* (third single from *Three Dog Night*), *Crystal Blue Persuasion*, *Love Is Blue*. DJ response to an
  LP track is the documented trigger in several (*Mrs. Brown* 1965, *Love Is Blue*, *Get Together*).
- **Adult pop goes LP-led late.** Of the 1967-69 MOR/adult hits, *Love Is Blue*, *The Good, the Bad and the Ugly*,
  *Little Green Apples*, *I'll Never Fall in Love Again* (E) and *This Guy's in Love*, *Romeo and Juliet* (A-plan) are
  LP-sourced; *Somethin' Stupid* is the single-led exception. This is the basis for dropping the orientation term
  late: TraditionalPop and Easy Listening have the lowest singles orientation (.45, .35), which was right for adult
  LP programmes in 1960 but not by 1968.
- **Tier.** Big-5 hits 1965-69 are A 11, E 5, H 7, N 3, S 2: 57% LP-sourced (A+E) against 46% for the 72 independent
  hits. With n=28 that is about one SE, so the prior stays tier-neutral, as in the original research.

## 3. What the model does late (existing decade runs, seeds 1001/2002)

Share of 1965-69 Top 10 chart-weeks by release route (two-seed means):

| Route | `dec-awrw-on` (reference) | `dec-apr8-bundle` | `dec-lpa4-youth` | census 1967-69 (hits) |
|---|---:|---:|---:|---:|
| Advance (promo) | 80.6 | 8.3 | 7.3 | 22 (A-plan) |
| Extraction | – | 10.8 | 15.6 | 18 |
| Orphan / independent | 19.4 | 80.9 | 77.1 | 60 (A-title + H + N/S) |
| Big-5 Top 10 share | 22.0 | 16.5 | 19.9 | not comparable (year-end top 20 reads ~2x; see §5) |

Per seed-year, 1965-69, `dec-lpa4-youth`:

| Route | Big-5 releases | Top 10 weeks each | Independent releases | Top 10 weeks each |
|---|---:|---:|---:|---:|
| Advance | 4.7 | 1.06 | 167 | 0.19 |
| Extraction | 11.3 | 2.50 | 19.0 | 2.68 |
| Orphan / independent | 112 | 0.61 | 4,094 | 0.08 |

- Advance decisions are commission-limited: in 1969 (seed 1001) the LP-only reasons are `no-advance-commission` 3,385 and
  `format-or-market` 2,368 (Compilation/Live are excluded by design), against 245 approved and 8 rejected on
  incremental economics. The mean eligible prior is .067 (TraditionalPop .068, Easy Listening .052).
- An approved advance replaces the act's independent-singles slot for that LP (that slot only fires for a standalone
  LP), so more advances re-route singles rather than add them.
- Extraction is held to ~30 a year by its gate (8-52 weeks old, album top 40, one cut, `.35` opportunity roll on a
  singles decision), but its hit share already matches the census. It is left unchanged.
- The reference's Big-5 lead comes from attaching a promo to all ~195 Big-5 LPs a year (0.51 Top 10 weeks each).

## 4. Change: `--album-practice-late-advance=K`

`AlbumReleasePractice.AdvanceCommissionProbability`:

```text
w(year)     = clamp((year − 1964) / 3, 0, 1)            0 through 1964, 1/3 in 1965, 2/3 in 1966, 1 from 1967
orientation = lerp(SingleOrientation, 1, w)
p           = clamp(.15 × orientation × institution × campaignScale × lerp(1, K, w), 0, .45)
```

- Unchanged before 1965 at any K, and when the flag is absent (checked per genre in `AlbumReleasePracticeChecks`, and by
  the byte-identical 52-week replay with the flag on).
- The institution factor stays (Classical/NonMusic 0, Jazz/Gospel .25, Blues/Folk .5). No tier term.
- Sizing: the target is advances ≈ 22% of late Top 10 weeks, about 3x the current 7%. At K=2 the commercial pop prior
  is .30 from 1967 (TraditionalPop 4.4x, TeenPop 2.2x, Soul 3.0x), a mean eligible prior of roughly .24. Yield per
  advance was expected to fall as weaker LPs get one. It fell less than expected: K=2 gave 3.7x the approvals and 3.5x
  the advance Top 10 share in 1967 (§6), so K≈1.5-1.7 is the sizing. K=3 hits the .45 clamp and was not run.
- The ramp shape follows the census (about half the 1963 → 1967-69 change by 1965-66).

## 5. Validation so far, and what this does not fix

Checks: `AlbumReleasePracticeChecks` 63/63 (four new ones on the late prior). Inertness: above.

1965-67 resume screen: §6. K=2 overshoots the 1967 target (27% against ~22%), so the decade arms are K=1.5 and K=2.

Not fixed here:

- **Late Big-5 share.** The census reads 22.5% for 1965-66 and 32% for 1967-69, but year-end top-20 share
  overweights the largest hits. For 1963 it reads 50% against the project's Top 10 benchmark of 26.0, so the census
  supports the late rise, not a level. The rise is the consolidation goal (distribution-deal absorptions,
  owner-family attribution), already scoped as its own mechanism. The reference sat above the bundle only because it
  gave every LP a promo.
- **Extraction volume.** Matches the census by hit share; revisit only if the advance change crowds it out.
- **1960-64.** The 1963 sample has A-plan 10% against the model's 3.5% of Top 10 weeks, but n=2 records and the early
  priors were set in the original research against CR8 and genre balance; left alone.

## 6. 1965-67 resume screen

Not a decade run. `dec-lpa4-youth` flags were run to the first chart week of 1965 and saved (`lpl-w65-{1001,2002}`),
then both arms were resumed from the same world for 156 weeks (1965-67). The control is a resume of the same world, as
the world-resume rules require. All runs passed the runner's checks. Two-seed means:

| | 1965 ctl | 1965 K=2 | 1966 ctl | 1966 K=2 | 1967 ctl | 1967 K=2 |
|---|---:|---:|---:|---:|---:|---:|
| Advance % of Top 10 weeks (census ~17.5 / ~22) | 4.2 | 6.6 | 7.2 | 13.5 | 7.7 | **27.3** |
| Extraction % of Top 10 weeks (census 7.5 / 18) | 11.9 | 13.2 | 13.2 | 12.1 | 10.5 | 9.3 |
| Advances approved per seed | 110 | 180 | 134 | 343 | 163 | 610 |
| Independent-programme singles | 2,243 | 2,251 | 2,392 | 2,225 | 2,453 | 2,215 |
| Big-5 Top 10 share | 14.3 | 14.2 | 13.5 | 13.1 | 12.6 | 14.5 |
| CR8 | 44.2 | 42.4 | 36.6 | 42.8 | 36.6 | 41.7 |
| Share error | 27.1 | 27.7 | 29.0 | 28.2 | 29.7 | 29.7 |
| Slot error | 78 | 80 | 73 | 70 | 70 | 69 |
| TraditionalPop share | 9.5 | 9.3 | 7.4 | 6.4 | 7.6 | 7.2 |
| Soul share | 10.8 | 10.3 | 13.9 | 14.3 | 18.0 | 18.6 |

- The route moves as designed and replicates (1967 advance share 27.5 and 27.1 by seed): 3.7x the approvals, mostly
  replacing independent-programme singles. At full weight K=2 overshoots ~22%, so K≈1.5-1.7 is the sizing.
- Share and slot error are flat within noise. The CR8 rise is mostly seed 1001 (+10 in 1966-67; seed 2002 +2.5 and
  +0.4). Big-5 1967 is up on both seeds (+2.5, +1.4), but 1965-66 are mixed, so no Big-5 claim.
- **Cost:** TraditionalPop share is 0.4-0.9 points lower on both seeds in 1966-67, and it is already under target. A
  TradPop advance replaces an independently recorded single and diverts some LP demand. Watch it in the decade run.
- Extraction share falls slightly as advances take Top 10 room. Watch it too.

Reproduce: `SimLogs/lpl1-plan.json` (inertness + saves), `SimLogs/lpl2-plan.json` (resumes), each with its
`-runs.json`; analyses `SimLogs/lpl2-s{1001,2002}-*` and `SimLogs/lpl2-routes-seeds.csv`.

## 7. Decade run, when the release-judgement branch resumes

Ready in `SimLogs/lpl-decade-plan.json`: seeds 1001/2002 (pairs only), the common flags, and the `dec-lpa4-youth`
arm flags plus:

| Arm | Adds to `dec-lpa4-youth` |
|---|---|
| `dec-lpl-k15` | `--album-practice-late-advance=1.5` |
| `dec-lpl-k2` | `--album-practice-late-advance=2` |

```text
python SimTools/run_genre_hook_experiments.py SimLogs/lpl-decade-plan.json --parallel 2
python SimTools/analyze_genre_hook_calibration.py --tags dec-lpa4-youth dec-lpl-k15 dec-lpl-k2 --seeds 1001 2002 --weeks 522 --output SimLogs/lpl-decade
```

Judge on: advance share of late Top 10 weeks (target ~22%), extraction share (~18%), late share and slot error, Soul
and TradPop late shares, and Big-5 Top 10 1965-69 (should not fall; the consolidation gap is separate). Controls are
the existing `dec-lpa4-youth` runs.

## Sources

- Billboard Year-End Hot 100 lists via Wikipedia, e.g.
  [1965](https://en.wikipedia.org/wiki/Billboard_Year-End_Hot_100_singles_of_1965),
  [1967](https://en.wikipedia.org/wiki/Billboard_Year-End_Hot_100_singles_of_1967),
  [1969](https://en.wikipedia.org/wiki/Billboard_Year-End_Hot_100_singles_of_1969),
  [1963](https://en.wikipedia.org/wiki/Billboard_Year-End_Hot_100_singles_of_1963).
- Spot-checks: [Mrs. Brown](https://en.wikipedia.org/wiki/Mrs._Brown,_You%27ve_Got_a_Lovely_Daughter) (LP Feb 1965,
  single Apr 1965 after DJ interest), [One](https://en.wikipedia.org/wiki/One_(Harry_Nilsson_song)) (third single from
  the 1968 LP), [Satisfaction](https://en.wikipedia.org/wiki/(I_Can%27t_Get_No)_Satisfaction),
  [Crying in the Chapel](https://en.wikipedia.org/wiki/Crying_in_the_Chapel) (1960 recording, 1965 single),
  [My Girl](https://en.wikipedia.org/wiki/My_Girl_(The_Temptations_song)),
  [The In Crowd LP](https://en.wikipedia.org/wiki/The_In_Crowd_(Ramsey_Lewis_album)).
- UK context only: Billboard, 5 April 1969, reports UK album output passing singles in 1968 for the first time
  ([PDF](https://www.worldradiohistory.com/Archive-Billboard/60s/1969/Billboard%201969-04-05.pdf)). No primary US unit
  comparison was found.
