# Historical label-tier balance: targets and the TeenPop explanation

Research date: October 16, 2026. Scope: the U.S. record market, principally 1960-64, with a later-decade reference.
This continues [MajorFamilyShareInvestigation.md](MajorFamilyShareInvestigation.md). The original research below
uses historical sources and existing logs. The subsequently authorized three-seed trial is recorded in section 8.
Neither phase changes generation rules or economic policies. Earlier directives and proposals are background evidence.

## Findings that change the balancing question

**Successful national independents should be an important part of the teen market. Their success is not, by itself,
evidence of excessive major ownership. But the adopted run's TeenPop increase is not carried by newly signed acts.**
The strongest numerical historical comparison available here is the distribution of weekly Top 10 positions among
companies. The game's combined Major + MidTier chart-unit share does not have a directly corresponding historical
series. It combines corporate majors with independent hitmakers and uses a different denominator.

There is also no uniformly excessive concentration in the existing runs. Adopted seed 2002 has high concentration
in 1960-61 and a more dispersed set of hit-producing businesses in 1962-64. The second seed has a different opening
year. A blanket reduction in MidTier strength could improve one observation while worsening another.

## 1. The signing theory, checked against the adopted run

The comparator here is **`ab5ctl-2002-260`**, the later bundle control. Its 1960 chart data match the original noon
control used in the investigation. Its later years must not be represented as the original noon counterfactual.

| 1960, seed 2002 | Later control | Adopted period costs + home territory |
|---|---:|---:|
| MidTier first-time signings, all genres | 68 | 68 |
| MidTier first-time signings with current primary genre TeenPop | 6 | 3 |
| All-tier TeenPop chart units | 20.087M | 20.623M |
| MidTier TeenPop chart units | 3.733M | 8.767M |
| MidTier share of TeenPop chart units | 18.58% | 42.51% |
| MidTier TeenPop units from artists with any signing/re-signing event that year | 284,788 | 66,277 |
| MidTier TeenPop units from artists without such an event | 3,447,826 | 8,700,237 |
| Share of MidTier TeenPop units from that no-event cohort | 92.37% | **99.24%** |

First-time signings are `eventType == signing`; every selected event has `priorContractCount == 0`.
The last two rows use **all** signing and re-signing events at all labels, regardless of the artist's genre at the event.
That avoids overlooking an act signed in an adjacent genre that subsequently releases TeenPop. It is a retrospective
event-defined cohort, not a reconstructed January 1 roster, and does not establish a counterfactual causal effect.

TeenPop's total chart units rise only 2.7%; the large movement is between tiers inside that genre. Most of the adopted
MidTier units belong to artists without a recorded contract move during 1960. This weighs strongly against winning
additional TeenPop signings as the proximate explanation. Competition between the **records of existing rosters**,
distribution, format choice, feedback and changed record-generation draws remain plausible.

The increase is also concentrated among firms, even though the earlier investigation showed it extends beyond a few
individual records:

| MidTier label, seed 2002 | Control TeenPop units | Adopted TeenPop units |
|---|---:|---:|
| Watts Brothers (`label_0358`) | 689,730 | 3,318,493 |
| Imperials Records (`label_0212`) | 254,207 | 1,791,441 |
| United Records III (`label_0363`) | 985,400 | 1,446,524 |
| King (`label_0010`) | 10,389 | 1,018,913 |
| Cameo-Parkway Records (`label_0015`) | 1,464,433 | 819,339 |

The first three procedural businesses supply **74.79%** of adopted MidTier TeenPop chart units. Their combined
increase is 4.627M units, or **91.92%** of the tier's 5.034M TeenPop increase. Historical Cameo-Parkway moves in the
opposite direction in this seed. These are useful firms to inspect in the next record-level diagnostic; the issue is
not simply that the historical teen specialist was made too strong.

## 2. Direct historical targets: concentration among hit-producing companies

Peterson and Berger's original study provides the following series. CR4 and CR8 are the leading four/eight
companies' shares of annual **weekly Top 10 slots**, not unit sales. Their firm grouping consolidates imprints and
affiliations. Carryover hits are excluded from the second year's new-hit counts.

| Year | Firms with hits | CR4 | CR8 |
|---|---:|---:|---:|
| 1960 | 39 | 28% | 52% |
| 1961 | 39 | 27% | 48% |
| 1962 | 41 | 25% | 46% |
| 1963 | 36 | 26% | 55% |
| 1964 | 37 | 34% | 51% |
| 1965 | 35 | 37% | 61% |
| 1966 | 31 | 38% | 61% |
| 1967 | 35 | 40% | 60% |
| 1968 | 30 | 42% | 61% |
| 1969 | 31 | 42% | 64% |

Source: Richard A. Peterson and David G. Berger, *Cycles in Symbol Production: The Case of Popular Music*,
*American Sociological Review* 40(2), 1975, Table 1, p. 160; method pp. 170-171.
[Original paper scan](https://people.uncw.edu/ricej/art/cycles%20in%20symbol%20production.pdf).
The table and method were visually verified against the scan.

### Suggested review bands

Use the annual historical points above as centers. An initial **plus/minus 5 percentage point review band** for
multi-seed mean CR4 and CR8 is a practical design allowance, not a historical confidence interval or an estimated
simulation noise distribution. Flag departures for diagnosis; do not make every single seed reproduce history.

That means, for example:

- **1960:** CR4 23-33%; CR8 47-57%.
- **1961:** CR4 22-32%; CR8 43-53%.
- **1964:** CR4 29-39%; CR8 46-56%.
- Across 1960-63, roughly **35-45 parent companies** producing new Top 10 hits is a useful broad review range.
  This is a design range around the observed firm counts, not a target for all active labels in the economy.

Keep the two concentration measures together: the largest few firms can dominate while the rest of the hit market
is very fragmented. Also preserve the historical increase in concentration after the early-1960s trough rather
than prescribing the same concentration for the entire decade.

## 3. Where the existing simulation lands on that measure

These calculations count each observed rank 1-10 record-week once. Each annual sample below has 52 weeks and
520 Top 10 slots. All three runs have zero `Absorb` events, so operating-label IDs provide a consistent initial
business proxy. They do **not** repair the factory's parent/imprint ontology.

| Year | Historical CR4 / CR8 | Later control, seed 2002 | Adopted, seed 2002 |
|---|---:|---:|---:|
| 1960 | 28 / 52% | 35.00 / 51.73% | **37.12 / 56.73%** |
| 1961 | 27 / 48% | 27.88 / 49.23% | **37.50 / 53.08%** |
| 1962 | 25 / 46% | 35.19 / 50.38% | 25.96 / 42.50% |
| 1963 | 26 / 55% | 26.35 / 44.42% | **25.00 / 40.19%** |
| 1964 | 34 / 51% | 31.73 / 49.81% | **28.85 / 41.15%** |

The adopted seed-1001 opening year is **26.92 / 43.08%**. The simple two-seed 1960 mean is therefore
**32.02 / 49.90%**, within the proposed review bands, although two seeds do not establish robustness.

The seed-2002 opening CR4 excess against history is 9.12 points, but the control already has a 7-point excess.
The adopted-versus-control difference is only **2.12 points** in that year. In 1961 the within-seed change is larger;
in 1962 it reverses. These descriptive comparisons do not identify how much any individual policy caused.

The adopted run has **45 / 48 / 55 / 62 / 57** operating labels with newly observed Top 10 hits in 1960-64. The later
years' low CR8 accompanies a broad spread of hitmakers. Historical company counts and simulation operating labels
are not fully interchangeable: the former consolidate affiliates, and the simulation separately seeds EMI,
Parlophone and Capitol. The 1960 new-hit count is also left-censored because these logs do not include 1959.
Treat this as evidence to audit the commercial hierarchy and ownership mapping, not a reason to close businesses
until the active population equals forty.

The existing `c4ChartShare` and `c8ChartShare` fields measure **all chart-week units**. For example, adopted 1960
reports 29.37 / 45.68% there, versus 37.12 / 56.73% in Top 10 slots. Comparing those existing fields directly with
the historical table would misdiagnose the result.

## 4. Financial anchors for the major companies, kept separate from chart units

The FTC's historical evidence reports these 1960 dollar shares:

| Company grouping | All-record sales | LP sales |
|---|---:|---:|
| Columbia + RCA + Capitol | 48.3% | 56.5% |
| Those three + Decca + Mercury | **54.4%** | **61.9%** |
| Those five + MGM | 56.8% | 63.7% |

With its approximate 75% LP dollar mix, the implied non-LP shares are respectively **23.7%, 31.9%, 36.1%**:
`nonLpShare = (allRecordShare - lpDollarMix * lpShare) / (1 - lpDollarMix)`.
These are reconstructed residuals, not independently measured singles shares. For the five-company group,
assuming a 70-80% LP mix instead gives 36.9-24.4%. There is no justified direct conversion to chart-only unit shares.

Source: FTC, *Columbia Broadcasting System, Inc.*, 72 F.T.C., findings p. 345 and format note p. 324.
[Official indexed decision text](https://www.ftc.gov/sites/default/files/documents/commission_decision_volumes/volume-72/ftcd-vol72july-december1967pages320-411.pdf).

The useful calibration implication is format separation: major firms can have a much stronger LP revenue position
than singles position. The five-company anchors match the factory's five named U.S. major templates, not every
business currently assigned `Major`. The ownership group must remain fixed when making that comparison.
Dealer-level game gross also needs a consistent accounting basis before comparison with consumer expenditure.

For a future comparable **all-market dollar** report, I would initially review the named five-company group around
**50-60% of all-format revenue** and **55-65% of LP revenue**. These are deliberately rounded working bands around
the sourced estimates. The reconstructed singles residual is too sensitive to the format assumption to justify a
hard acceptance band. It suggests a substantially more open singles market, rather than a 60% singles quota for
the corporate majors.

The FTC table was checked in indexed official text; direct PDF downloads returned 404 during this research.
It is a weaker verification path than the visually checked paper and period issue used elsewhere here.

## 5. Why a strong teen-oriented independent is historically appropriate

Billboard's account of Cameo-Parkway's SEC registration reports **six singles selling at least 100,000 copies in
1960**, with **99% of net sales attributable to Bobby Rydell and Chubby Checker**. It sold through approximately
**35 distributors**. This is a concrete example of a successful independent with concentrated star income and
national commercial access. It is not a census of the TeenPop genre.
[Billboard, January 13, 1962, pp. 8 and 19](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1962/Billboard%201962-01-13.pdf).
Both pages were visually checked. The article summarizes a company filing, rather than independently auditing
the company's claims.

Columbia's testimony also explains its club arrangement with Cameo-Parkway as meeting younger customers' demand
for teen-oriented material its existing suppliers had struggled to provide.
[FTC decision, p. 358, footnote 79](https://www.ftc.gov/sites/default/files/documents/commission_decision_volumes/volume-72/ftcd-vol72july-december1967pages320-411.pdf).

That supports an economic role for a strong, focused independent even alongside better-capitalized corporations.
It does not establish a historical percentage for the simulator's canonical `TeenPop` bucket, nor show that stronger
labels always acquired the best acts. Dance records, teen idols, girl groups and rock and roll overlap in period
accounts but are separate game genres.

## 6. What to aim for by tier

There is **no defensible measured five-tier percentage split** in the sources inspected. These tiers encode operating
scale and specialization; the historical sources principally describe corporations, imprints and independents.
Inventing five precise historical quotas would disguise a design choice as a measurement.

| Game tier | Historically grounded balancing objective |
|---|---|
| Major | Preserve strong LP/catalogue economics; compare a fixed, correctly grouped set of corporate owners against format-specific dollar evidence. Do not equate the seven current Major labels with the five-company historical group. |
| MidTier | Let a limited group of proven national independents earn substantial hits, including teen hits. Judge their contribution within company concentration and durable operating scale; there is no sourced 20%, 25% or 30% tier-share quota here. |
| Independent | Preserve a broad field of challengers and breakout firms, while checking whether too many become recurrent national hitmakers later in the run. A promotion must not be mistaken for a corporate acquisition. |
| Boutique | Evaluate specialist catalogues and format/genre outcomes. National pop-chart share alone is a poor measure of the role of jazz, folk or other specialists. No sourced national chart-share floor is established. |
| Small | Preserve local entry and occasional breakthroughs; do not require every local business to chart nationally. Active-label count and local sales need separate denominators from national hit participation. |

The existing 1965 **14-19% MidTier active-label-share** band in the earlier promotion handoff is a retained
simulation-control calibration, not a historical census established by this research. It should not be presented as
external validation of the tier distribution, or substituted for a sales-share target.

My recommendation is to adopt the sourced **Top 10 company-concentration targets first**, retain separate Major,
MidTier and remaining-tier reporting, and leave the combined-family percentage descriptive. A 62.55% combined
share does not establish a historical failure, and 50% is not a historically demonstrated optimum for that metric.

## 7. Next diagnostic implied by the evidence

The next causal investigation should follow the existing artists at Watts Brothers, Imperials and United Records
III across paired arms. Compare realized hook/production/originality, format, launch stock, awareness, regional
conversion, radio and chart feedback; include off-chart competition. Stabilize record-generation draws across arms
if practical, then inspect several seeds. This is a proposed diagnostic, not an implemented engine change.

Before turning the historical review bands into acceptance gates, reconcile the U.S. market and parent/imprint
mapping, and obtain more seeds through 1964. Different initial tier populations and genre specializations can be
tested after the commercial outcome being corrected is explicit. Neither a teen-signing nerf nor a global MidTier
sales penalty follows from the evidence here.

## 8. Adopted 1960 review

The user accepted **23-33% CR4** and **47-57% CR8** as initial 1960 multi-seed review bands. They are now available
through `--review-manifest` in the analysis script. The review averages each seed's separately ranked Top 10 slot
shares with equal weight. It does not pool sales or label identities across worlds.

The first trial uses seeds **1001, 2002 and 3003**, 52 weeks each, on one freshly built assembly and the adopted
default cost/territory configuration with the established regional/economic flag stack. At least three distinct
seeds are required for a completed review; that sample-size rule is a practical initial choice, not a power estimate.
An outlying individual seed does not fail the seed mean. Out-of-band means produce `REVIEW` and explanatory
`historicalWarnings`; fewer than three seeds produce `INSUFFICIENT_SEEDS`. These are advisory statuses, not new
engine gates. The analysis rejects failed native runs, partial opening years, duplicate seeds and mixed flags.

The initial sandboxed Godot launch (`historical-top10-review-v1`) crashed before startup and contributes no data.
The retry uses the fresh prefix `historical-top10-review-v2`. Native results and build provenance are retained in
`SimLogs/historical-top10-review-v2/runs.json`. The completed trial summary will accompany that manifest.

Trial reproduction, using the existing launcher after a successful build:

```powershell
& '.\SimTools\RunSmallCostPilot.ps1' -RunTag 'historical-top10-review-v2' -Weeks 52 -Seeds @(1001,2002,3003) -Modes @('default') -Compact
```

Choose a new run tag for a repeat; the launcher refuses to overwrite previous output. To review the completed trial:

```text
python SimTools/analyze_label_tier_historical_benchmarks.py SimLogs/historical-top10-review-v2-default-1001-52 SimLogs/historical-top10-review-v2-default-2002-52 SimLogs/historical-top10-review-v2-default-3003-52 --review-manifest SimLogs/historical-top10-review-v2/runs.json --output SimLogs/historical-top10-review-v2/review.json
```

## 9. Trial result and diagnosis (`historical-top10-review-v2`)

All three runs passed: exit 0, empty error logs, one assembly hash. Seeds 1001 and 2002 reproduce the section 3
opening-year numbers exactly. The CSVs are at `SimLogs/historical-top10-review-v2-default-<seed>-52-*`. The
manifest, console logs and `review.json` are under `SimLogs/historical-top10-review-v2/`.

| 1960 | Seed 1001 | Seed 2002 | Seed 3003 | Mean (SD) | Band | History |
|---|---:|---:|---:|---:|---:|---:|
| CR4 Top 10 slots | 26.92% | 37.12% | 37.31% | **33.78%** (5.95) | 23-33 | 28 |
| CR8 Top 10 slots | 43.08% | 56.73% | 50.96% | 50.26% (6.85) | 47-57 | 52 |
| Firms 5-8 (CR8 - CR4) | 16.16 | 19.61 | 13.65 | **16.47** | - | 24 |
| Labels with any Top 10 | 57 | 45 | 49 | 50.3 | ~35-45 | 39 |

Status: **`REVIEW`**. The CR4 mean is 0.78 points above the band, but the standard error of a three-seed mean is
about 3.4 points, so the flag alone is marginal. The more consistent signal is the **shape**. In every seed, firms 5-8
hold less than the historical 24 points. The top four average 67% of CR8, against 54% in history. CR8 lands in band
only because a top four that is too strong cancels out a 5-8 group that is too thin.

**What does not explain it** (all three seeds):
- *Run length:* mean Top 10 weeks per record is 4.5-5.7 for the top-four, 5-8 and remaining groups alike.
- *Superstars:* each leading firm spreads its Top 10 records across 5-11 artists. No single act carries a firm.
- *Opening carryover:* records already charting in week 1 take 13-15% of slots. Excluding them, or using only
  weeks 14-52, moves CR4 by 1-3 points at most.
- *Parent mapping:* consolidating EMI into Capitol, its 1955 parent, only *raises* concentration
  (seed 2002: 38.85 / 60.00%). The operating-label proxy understates the excess; it does not create it.

**What does:** the number of Top 10 records. The five CorporateGiant Majors (`label_0001`-`0005`) are 17-23% of Hot 100
entries but 34-40% of Top 10 records. Hot 100 to Top 10 conversion, pooled across seeds, is Major 23.4%, MidTier
14.6%, Independent 8.9%, Boutique 8.9% and Small 5.2%. This happens even though the Majors' median record quality is
*lower* (0.68 against 0.74 for the rest). The gap persists after controlling for launch conditions. This is for
fresh 1960 releases, comparing Big-5 to everyone else:

| Quality quartile | All launch stock | Launch stock ≥ 67k only | Upper-half quality, launch awareness 0.25-0.50 |
|---|---:|---:|---:|
| q2 | 23% vs 4% | 25% vs 14% | - |
| q3 | 48% vs 9% | 46% vs 9% | 48-67% vs 9-23% |
| q4 | 72% vs 24% | 72% vs 24% | (same rows) |

Big-5 radio heat is higher even among records that fail to reach the Top 10 (median 0.52 against 0.36). This is the
channel described in `airplay-convexity-amplifies-label-push`. `UpdateRadioHeat` adds
`currentLabelPush * RADIO_LABEL_WEIGHT (0.4)`, with push from `GetCampaignImpact` =
`marketingPower * (0.45 + 0.55 * budgetLevel)`. The `ApplyTierStats` midpoints give a push of about 0.79 for a Major,
0.53 for MidTier and 0.36 for an Independent. For a q = 0.75 record, that is 1.31x the radio-heat target. The
fifth-power airplay exponent turns it into about **3.8x** the airplay points. This matches the measured 3-5x
conversion gap. Launch stock and launch awareness are not the separating terms.

The seed-1001 low outlier is volume, not conversion: its Big-5 had only 16.9% of Hot 100 entries, against
20.7-22.8% in the other seeds.

**Implication.** The excess is a Major push premium inside the airplay exponent. It is not an A&R advantage or a
signing advantage. Earlier notes advise against tuning `AIRPLAY_CONVEXITY` against a label table, since it carries
number-one tenure, Top 40 life and the genre ordering. The narrower lever is the push term's tier spread (the
Major-to-Independent push ratio). No engine change has been made.

### 9a. Five-seed extension (`historical-top10-review-v2-ext`)

To settle whether the marginal flag was real, seeds 4004 and 5005 were run on the **same assembly**
(`4F68F505…2D62`, not rebuilt; no newer sources) with the same flag stack. Both passed with empty error logs. The
two launcher manifests were merged after checking that the hash, flags and duration were identical:
`SimLogs/historical-top10-review-v2-ext/runs-five-seed.json`, reviewed into `review-five-seed.json`.

| 1960 | 1001 | 2002 | 3003 | 4004 | 5005 | Mean (SD) | Band |
|---|---:|---:|---:|---:|---:|---:|---:|
| CR4 | 26.92 | 37.12 | 37.31 | 45.38 | 42.50 | **37.85** (7.05) | 23-33 |
| CR8 | 43.08 | 56.73 | 50.96 | 62.12 | 62.12 | 55.00 (8.10) | 47-57 |
| Big-5 share of Hot 100 entries | 16.9 | 20.7 | 22.8 | 19.9 | 20.3 | 20.1 | - |
| Big-5 share of Top 10 records | 33.7 | 39.6 | 38.4 | 45.4 | 43.1 | 40.0 | - |
| Big-5 Hot 100 → Top 10 | 26% | 25% | 22% | 30% | 29% | - | - |
| Others Hot 100 → Top 10 | 10% | 10% | 10% | 9% | 10% | - | - |

**The CR4 flag is real.** The mean is 4.85 points above the ceiling and 9.85 above the historical 28, about
3.1 standard errors (SE ≈ 3.15). Four of five seeds exceed 33%. CR8 stays in band, but two seeds now reach 62%.
The new seeds have the same mechanism. Upper-half-quality conversion is 60-66% for the Big-5 against 15-16% for
everyone else. Weeks per Top 10 record are unchanged (5.1-5.4). The seed-4004 extreme, RCA at 24.6%, is breadth
rather than a single act: 41 Hot 100 entries, 21 Top 10 records, 14 artists, and a top artist with 37 slots.

Side finding, separate from concentration: procedural labels can receive historical names. Seed 4004 has a
MidTier TeenHitMachine "Columbia Records" (`label_0090`), and seed 5005 has a MidTier "Capitol Records"
(`label_0063`), alongside duplicates of Decca, Mercury, Atlantic, Sun, Chess, Monument and others.
`NameGenerator.GenerateLabelName` does not check against existing or template names. These are separate operating
businesses, so the label-ID-based CR4 is unaffected, but name-keyed reports and the player-facing directory would
conflate them.

## 10. Push-spread probe (`--radio-push-spread=0.5`)

**Switch.** `Systems/RadioPushSpread.cs` adds `--radio-push-spread=F`, which is off unless named. It compresses only
the weekly radio push in `ChartSimulator.UpdateLabelPush`:
`push' = 0.48 + F * (push - 0.48)`. The pivot 0.48 is the Hot 100 entry-weighted mean push from the
`ApplyTierStats` midpoints. At F = 0.5 a Major goes from about 0.79 to 0.64, and an Independent from about 0.36
to 0.42. Every other reader of `GetCampaignImpact` is untouched: launch awareness, launch stock, album push and the
competitor promo budget. `--radio-push-spread-check` (`SimTools/RadioPushSpreadChecks.cs`) asserts that the probe is
an identity when off and halves the Major-Independent gap at 0.5. The launcher gained `-ExtraArgs`, which are
recorded in the manifest's flags.

**Inertness.** Built assembly `21A2517C…E8CC`. A switch-off seed-1001 run (`push-spread-off-check`) is
**byte-identical** to `historical-top10-review-v2-default-1001-52` across all 80 CSVs. The five review runs are
therefore valid paired controls. All five probe runs passed with empty error logs. The paired output is in
`SimLogs/push-spread-050/paired-five-seed.json` (`SimTools/analyze_push_spread_probe.py`), and the formal review is
in `review.json`.

| 1960, five seeds | Control | Probe | Δ (min..max) | History / band |
|---|---:|---:|---:|---:|
| CR4 | 37.85 (SD 7.05) | **31.50** (SD 2.82) | −6.35 (−12.50..−0.19), all seeds | 28 / 23-33 |
| CR8 | 55.00 (SD 8.10) | **47.12** (SD 3.97) | −7.88 (−13.27..−1.92), all seeds | 52 / 47-57 |
| Firms 5-8 | 17.15 | 15.62 | −1.54 (−3.27..+2.31) | 24 |
| Labels with a Top 10 | 47.6 | 51.2 | +3.6 | ~35-45 firms |
| Big-5 Top 10 slots | 40.38% | 31.42% | −8.96, all seeds | - |
| Big-5 upper-half-quality → Top 10 | 60.8% | 51.5% | −9.3, all seeds | - |
| Others, same | 15.9% | 16.8% | +0.9 | - |

Review status: **`WITHIN_BANDS`**. Every seed's CR4 falls. The seed-4004 RCA outlier drops from 24.6% to 9.2%, and
the seed spread shrinks from 26.9-45.4% to 26.7-33.7%. Major Top 10 slots fall 8.0 points, spread across MidTier
(+1.6), Independent (+2.1), Boutique (+2.6) and Small (+1.7).

**Chart-health guards** (mean Δ): single units +0.43M (+0.3%); new Hot 100 entries per week +0.15 (12.93 → 13.08);
new Top 40 entries per week +0.03; closed chart life +0.01 weeks; distinct #1s −1.4 (range −5..+2); #1 tenure
+0.15 weeks; #1 spell +0.09 weeks; #1 returns +2.0 points (0.9 → 2.9%, still under the historical 4-5%); debut
−0.37 positions. The quality-to-outcome correlations are unchanged (Spearman 0.41 → 0.42, Pearson 0.50 → 0.50).
Nothing moves outside seed noise.

**What it does not fix.** The probe cuts Big-5 conversion by only about 9 of the 45-point gap. The rest of the
premium sits elsewhere: launch stock, launch awareness or volume. More importantly, firms 5-8 still hold
**15.6 points against a historical 24**. The slots the Big-5 lose go to the long tail rather than to a second rank
of hitmakers. So CR8 lands at the bottom of its band (seed 1001 is at 40.2%). A single push factor cannot hit both
centers: a factor high enough to center CR8 leaves CR4 over 33. A thicker second rank of national independents is a
separate structural question.

**Before adopting it as a default:** this is a 1960-only result. The late-decade goal is rising Major share in
1968-69 (`major-share-late-decade-consolidation-goal`), and a smaller push premium works against that. Push also
feeds awareness through radio heat, so its demand effect compounds over a decade. A two-seed decade A/B is owed
before any default change.

## 11. Why firms 5-8 are thin: only five firms have head-scale chart access

Historical 1960 has a **flat head**: CR4 28 and CR8 52 mean firms 5-8 average 6.0% each, against 7.0% for the top
four. That is about 31 slots, or roughly six Top 10 records, per firm. The five control seeds average 9.5% for the
top four and only 4.3% for firms 5-8. Analysis: `SimTools/analyze_second_rank_hitmakers.py` →
`SimLogs/second-rank-investigation/control.json` and `control.firms.csv`.

**Who is at ranks 5-8.** Across the 20 firm-places (5 seeds × ranks 5-8): 8 are Big-5 CorporateGiants that missed the
top four, 2 are procedural Majors, 7 are MidTier and 3 are Independents. Rank 5 is usually the weakest of the Big-5,
and ranks 6-8 are the best of the tier below.

| Rank group (per firm, pooled) | Launches | Hot 100 | Hot 100 / launch | Top 10 / Hot 100 | Top 10 records | Slots |
|---|---:|---:|---:|---:|---:|---:|
| 1-4 | 43.0 | 28.9 | 0.67 | 0.31 | 9.1 | 49.2 |
| 5-8 | 34.3 | 18.8 | 0.55 | 0.21 | 3.9 | 22.3 |
| 9-20 | 20.1 | 6.5 | 0.32 | 0.26 | 1.7 | 9.5 |

| Tier (per firm, pooled) | Firms | Launches | Hot 100 / launch | Top 10 / Hot 100 | Top 10 yield per launch |
|---|---:|---:|---:|---:|---:|
| Big-5 | 25 | ~45 (max 52 = one a week) | 0.657 | 0.262 | **17.2%** |
| MidTier | 103 | 24.6 (max 39) | 0.28 | 0.155 | **4.3%** |
| Independent | 1,018 | 12.0 (max 30) | 0.13 | 0.087 | 1.1% |

The cliff after the Big-5 is about 4x per launch, and MidTier releases about half as many records. Only 5.8 non-Big-5
firms per seed launch 30 or more records in 1960. A MidTier firm would need about 140 launches to reach a historical
5-8 firm's six Top 10 records. Launch awareness, launch radio heat, launch stock and weekly push all read the same
`GetCampaignImpact` and reach (`ChartManager.ReleaseRecord` / `PromoteRecordAI`), so the tier gap compounds on every
launch channel.

**Structure, not luck.** An offline null model keeps each firm's observed Hot 100 entries, gives every firm its tier's
pooled conversion, and redraws Top 10 records and slots from the empirical distributions. It reproduces the control:
CR4 36.0, CR8 51.2, firms 5-8 at 15.3 (observed 37.9 / 55.0 / 17.2). Equalizing the Big-5's volume does not help
(15.7). The thin second rank is built into who has scale.

**What fixes the shape.** These counterfactuals use the same null model, with the chart size held fixed:

| Head firms at Big-5 conversion | Hot 100 per head firm | CR4 | CR8 | Firms 5-8 |
|---|---:|---:|---:|---:|
| 5 (today's structure) | 20 / 30 / 40 | 28.7 / 35.0 / 40.8 | 44.1 / 51.0 / 56.5 | **15.4 / 16.0 / 15.7** |
| 8 | 20 / 30 / 40 | 29.3 / 33.9 / 37.6 | 46.4 / 55.0 / 61.5 | 17.1 / **21.0** / 23.9 |
| 10 | 20 / 30 / 40 | 28.6 / 32.5 / 34.6 | 47.0 / 54.4 / 59.1 | 18.4 / **21.9** / 24.5 |
| Top-3 non-Big-5 promoted to Big-5 access + conversion | - | 34.1 | 53.0 | 18.9 |
| Three extra Big-5-scale firms | Big-5 mean | 33.0 | 52.5 | 19.5 |

With five head-scale firms, firms 5-8 stay at about 15-16 points **whatever** those five firms' volume. This is also
why the push probe (section 10) lowered CR4 and CR8 together. A shape near history (about 30 / 52 / 21) needs
**eight to ten** firms with near-Major chart access at about 25-30 Hot 100 entries each. Record-level sampling noise
keeps even ten identical firms from reaching history's flatness. That residual is consistent with Peterson and
Berger's consolidated parent firms, which are larger and steadier than single operating labels.

**The roster gap.** The template roster has exactly five U.S. head-scale firms. None of the large 1960 pop firms that
could plausibly fill ranks 5-8 is a template: MGM (the sixth company in the FTC grouping, section 4), ABC-Paramount,
Dot, London, Warner Bros., Cadence, Kapp, Laurie, United Artists and Roulette are all absent. Procedural labels roll
Major at 0.2% and MidTier at 3.3% (`GetRandomTier`). Columbia's Epic and Decca's Coral/Brunswick are also absent,
which matters for parent grouping. **Which** firms held 1960 ranks 5-8, and their hit counts, still needs a sourced
check: the names above are candidates, not verified ranks.

**Implication.** This is not a parameter problem. A probe that would test it is to seed three to five national-scale
1960 firms as templates (the sixth major plus large national pop independents). Each would sit near the Major
launch-access and volume bands without being a Big-5 clone. Then check that CR4 stays at or below 33 and firms 5-8
rise toward about 21. Costs: it changes the opening roster and the RNG stream, takes procedural slots from the fixed
`targetCount`, and interacts with the Small-label closure band, the push probe and the late-decade Major-share goal.
Parent grouping (Epic under Columbia, Coral/Brunswick under Decca, EMI-Capitol) should be decided first, so the review
compares like with like.

## 12. 1960 Top 10 reconstruction: who held ranks 5-8, and what the band should be

**Data.** All 52 weekly 1960 Top 10s (issues dated January 4 to December 26) were taken from billboard.com:
520 slots and 102 records. The #1 sequence matches the known 1960 history. Labels came from two sources. **59**
records match the label printed in the Hot 100 of eight 1960 *Billboard* issues (January 4, February 22, April 18,
June 20, August 15, October 10, November 28 and December 26; worldradiohistory.com scans). **41** use the
Wikipedia infobox for that artist's single; where both sources existed, the infobox matched print in every case
except two wrong-version infoboxes, which print corrected (Bill Black's "White Silver Sands" is Hi, not Jubilee;
Joe Jones's "You Talk Too Much" is printed as Roulette, not Ric). **2** one-slot records (Paul Evans on
Guaranteed, Ray Peterson's "Corinna, Corinna" on Dunes) are unverified. Per-row provenance is in
`SimLogs/historical-1960-top10/records_1960_final.csv`. The scripts have since been generalized to take a year
(section 13); rerun for 1960 into `SimLogs/historical-top10-1960/`, they reproduce the same weekly charts, the same
label on all 102 records and every figure below except the any-affiliation all-slots row (section 13, Swan).

**Grouping.** Peterson and Berger (p. 171) assigned labels to corporate owners, and "judged a label as dependent
rather than independent if there was any indication of affiliation". Their 1960 hit-record list excludes records
carried over from the previous year. The 1960 links below come from the Wikipedia histories of each label:

| Label | 1960 parent / affiliation | Kind |
|---|---|---|
| Cub | MGM ("Subsidiary Cub Records was launched in the late 1950s") | owned |
| Brunswick, Coral | Decca (Brunswick revived by Decca in 1943; Coral started by Decca in 1949) | owned |
| Parkway | Cameo ("Until 1962, Cameo was also the parent company name for both labels") | owned |
| Leader | Kapp (listed among Kapp's subsidiaries) | owned |
| Atco | Atlantic ("an abbreviation of Atlantic Corporation") | owned |
| Epic | Columbia/CBS (launched by Columbia's CBS unit in 1953) | owned |
| Capitol | EMI (acquired 1955) | owned |
| Chancellor | ABC-Paramount ("distributed as an imprint of ABC-Paramount") | distributed |
| Dolton | Liberty ("Liberty had distributed Dolton releases" from 1959) | distributed |
| Hi | London, the U.S. arm of British Decca ("distributed nationally by London Records") | distributed |
| Top Rank (U.S.) | Rank Organisation's U.S. branch, 1959-61; EMI took over the **UK** label in 1960 | unresolved |

EMI-Capitol makes no difference in 1960: Capitol has 6 slots, and no other EMI label reached the Top 10. Epic,
Coral and London have no 1960 Top 10 slots of their own.

**Who held ranks 5-8:**

| Rank | Operating label | Slots (records) | Peterson-Berger grouping | Slots (records) |
|---:|---|---:|---|---:|
| 1 | RCA Victor | 66 (10) | RCA Victor | 66 (10) |
| 2 | Columbia | 42 (6) | MGM + Cub | 56 (9) |
| 3 | MGM | 40 (7) | Columbia | 42 (6) |
| 4 | Decca | 32 (5) | Decca + Brunswick | 39 (7) |
| **5** | Mercury | 32 (6) | **Mercury** | 32 (6) |
| **6** | Warner Bros. | 23 (4) | **ABC-Paramount + Chancellor** | 29 (7) |
| **7** | ABC-Paramount | 23 (6) | **Cameo-Parkway** | 28 (4) |
| **8** | Cameo / Cub (tie) | 16 (3) / 16 (2) | **Warner Bros.** | 23 (4) |
| 9-12 | Cadence 15, Top Rank 13, Parkway 12, Era 11 | | Liberty + Dolton 16, Cadence 15, Atlantic + Atco 13, Top Rank 13 | |

The 1960 second rank is **Mercury, ABC-Paramount, Cameo-Parkway and Warner Bros.**, with MGM in the top four once
Cub is included. Of the section 11 candidates, MGM, ABC-Paramount and Warner Bros. are confirmed. Dot and London had
no 1960 Top 10 slot, and Cadence, Kapp, Liberty, Laurie, United Artists and Roulette are ranks 9-30. Cameo-Parkway
(already a template) is a genuine 5-8 firm. Mercury is already a Big-5 template.

**Peterson and Berger's 1960 row does not reproduce.** These are the possible readings of their method:

| 1960 basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **28** | **52** | **24** | 39 (45 labels) | 20 |
| Operating label, slots | 34.6 | 52.7 | 18.1 | 49 | 31 |
| Operating label, records | 28.4 | 47.1 | 18.6 | 49 | 31 |
| Owned imprints, slots | 39.0 | 59.4 | 20.4 | 44 | 26 |
| Any affiliation, slots | 39.0 | 60.6 | 21.5 | 42 | 24 |
| Any affiliation, slots, excl. 1959 carryover | 39.4 | 59.8 | 20.4 | 40 | 22 |
| Any affiliation, records, excl. 1959 carryover | 33.0 | 50.0 | 17.0 | 40 | 22 |

The firm counts match their rule well (40 firms and 22 one-hit firms against 39 and 20), but on slots their own
grouping gives CR4 **39**, not 28. No basis gives 28 and 52 together. Their 1960 CR4 is about 10 points below what
their stated method gives on the Billboard data, so it should not be used alone as a target. Possible reasons, none
tested: a different chart or week set, a different allocation of affiliations, or a transcription error.

**What this changes for the simulation.** The review measures operating labels (section 3). Compared like for like:

| 1960 | CR4 | CR8 | Firms 5-8 |
|---|---:|---:|---:|
| Billboard reconstruction, operating label, slots | **34.6** | **52.7** | **18.1** |
| Simulation control, five seeds | 37.85 (SD 7.05) | 55.00 (SD 8.10) | 17.15 |
| Push-spread probe 0.5 (section 10) | 31.50 | 47.12 | 15.62 |

- **The CR4 flag was mostly a target artifact.** The control is +3.3 CR4 and +2.3 CR8 above the comparable
  reconstruction, about one standard error of a five-seed mean. The 23-33 band was centered on an unreproducible 28.
- **The second rank is not thin on a like-for-like basis:** 17.2 against 18.1. The 24-point target in section 11
  compared operating labels with a consolidated-firm figure. The section 11 mechanism is still accurate: only five
  firms have head-scale access, and the null model's 15-16 ceiling is real. But history's operating-label second
  rank is also only about 18, so that ceiling is close to correct for this measure.
- **The push probe moves away from history** on CR8 (47.1 against 52.7) and on firms 5-8 (15.6 against 18.1).
- **Seeding more head-scale templates is not supported by 1960 data** on the operating-label measure. Adding MGM
  as a sixth head firm would be historically grounded on its own (rank 3 by operating label, 2 with Cub), but it
  would raise CR4 rather than thicken ranks 5-8.

**Recommended re-baseline (proposal, not adopted):** center the 1960 operating-label review on the reconstruction:
CR4 about **30-40** and CR8 about **48-58** (±5 points around 34.6 and 52.7). Treat the Peterson-Berger series as a
consolidated-firm *trend* reference (low concentration in 1962, higher after 1965), not as point targets. Before
adopting this, extend the reconstruction to at least 1961-64 and one late year (1968 or 1969), using the same
scripts, since one year cannot set a decade band.

## 13. Top 10 reconstruction, 1961-64 and 1968-69: decade review bands

Section 12 reconstructed one year, and one year cannot set a decade band. This section applies the same method to
1961, 1962, 1963, 1964, 1968 and 1969, and re-runs 1960 through the same scripts.

**Charts.** For each year, `research_top10_fetch.py` walks billboard.com from the last chart of the previous
year, one week at a time, reading each page's own "Week of" date. That handles the January 1962 move from Monday
to Saturday issue dates (the 1961 charts run to December 25; the 1962 charts start on January 6). Every year has
**52 consecutive weekly charts** with 7-day gaps, which is 520 Top 10 slots. The #1 runs match the known history:
for example, "Tossin' and Turnin'" holds 7 weeks in 1961, "I Want to Hold Your Hand" 7 weeks in 1964, "Hey Jude"
9 weeks in 1968, and "Aquarius/Let the Sunshine In" 6 weeks in 1969. The 1960 weekly file is byte-identical to the
section 12 file.

**Labels.** Each row is assigned in the same order as 1960: printed *Billboard* first, then the Wikipedia infobox
for that artist's single, then a recorded manual decision.

- *Print:* 10 issues per year (every sixth chart week plus the last), from worldradiohistory.com; 1961 has 9,
  because no February 1961 issue is in the archive. The matcher reads Hot 100, R&B chart, review and
  regional-breakout lines of the form "Title, Artist, Label 1234". From 1964 the Hot 100 prints title and artist
  on separate lines, so a cross-line pattern is tried when no same-line match exists. The 1961 scans have **no
  text layer** (`pdftotext` returns 1-2 KB per issue), so their page images were read with the Windows OCR
  engine. That OCR text has no legible Hot 100 page, and only 38 of the 1961 rows are matched in print. The
  quality of the text layer also varies within a year: the October 26, 1968 Hot 100 is unreadable, for example.
- *Infobox:* the per-year "List of Billboard Hot 100 top-ten singles" links, then direct page lookups for records
  the lists do not link. A label such as "Parlophone (UK) / Capitol (US)" resolves to the U.S. label.
- *Manual decisions* are in `SimTools/research_top10_label_overrides.csv`, one row per record with the quoted
  print line or the reason. Every row where print and infobox disagree was read in context, and every
  1961-69 infobox-only row was checked by eye. Print corrected wrong-version infoboxes, for example:
  - "You Are My Sunshine" is Ray Charles on ABC-Paramount (the infobox gives a Decca version).
  - "Light My Fire" is Jose Feliciano on RCA Victor (the infobox gives the Doors' Elektra).
  - "Ruby Baby" is Dion on Columbia (the infobox gives Laurie).
  - "San Antonio Rose" is Floyd Cramer on RCA Victor (the infobox gives Bob Wills's Vocalion).

  Other infoboxes list a local first issue (Jerden for "Louie Louie", Downey for "Pipeline", Lama for "The
  Mountain's High"), and print gives the national label. Four times the matcher took a neighbouring line's label,
  and each was reversed by hand. For example, Three Dog Night's "Easy to Be Hard" is printed Dunhill 4203, and
  Johnny Tillotson's "Talk Back Trembling Lips" is MGM, not the Mercury LP on the next line.
- *Unverified:* labels from domain knowledge alone, marked `UNVERIFIED` in `labelSource`. Most are U.S. issues
  of British records where print and infobox give only the UK label.

| Year | Records | Print | Infobox | Unverified (slots) | Carried over from the previous year |
|---:|---:|---:|---:|---|---:|
| 1960 | 102 | 68 | 32 | 2 (2) | 8 |
| 1961 | 115 | 38 | 74 | 3 (5) | 9 |
| 1962 | 115 | 97 | 18 | 0 | 8 |
| 1963 | 119 | 113 | 6 | 0 | 8 |
| 1964 | 111 | 57 | 44 | 10 (38) | 7 |
| 1968 | 105 | 71 | 30 | 4 (12) | 10 |
| 1969 | 108 | 89 | 18 | 1 (1) | 9 |

The unverified rows hold 58 of the 3,640 slots (1.6%). 38 of them are in 1964, and every 1964 unverified label is
a large firm (Capitol, Smash, Imperial, MGM, Mercury, Decca, Liberty) or one of its owned labels (Amy is Bell's),
apart from Montel (2 slots). An error there would move slots between large firms rather than create a new one.

**Ownership and affiliation, 1960-69.** `SimTools/research_top10_label_affiliations.csv` holds 91 links. Each has
a kind (owned, distributed, or a firm rename), a from/to year or month, and a quoted Wikipedia source. A link
applies to a record whose first Top 10 week in the year falls inside the link's dates. That splits mid-year
changes such as Stax, which moved from Atlantic distribution to Paramount ownership in May 1968. Links also
resolve transitively, so Reprise counts under Warner Bros., and from July 1967 under Warner-Seven Arts. The
changes that move a top-8 firm are:

| Year | Change | Source (Wikipedia) |
|---|---|---|
| 1953-69 | Epic, Okeh (and Date from 1966) owned by Columbia/CBS | Epic, Okeh, Date Records |
| 1961 | Smash founded as a Mercury subsidiary; Philips (Conelco) buys Mercury | Smash Records, Mercury Records |
| 1962 | Philips label issued in the U.S. by Mercury from February | Mercury Records |
| 1962 | MCA completes its acquisition of Decca (Brunswick, Coral; Uni from 1966, Kapp from 1967) | Decca, Uni, Kapp Records |
| 1963 | Liberty (Avnet) buys Imperial, Minit and Dolton; Dolton and Minit had been distributed (1959/1960-62) | Liberty, Imperial, Minit, Dolton Records |
| Aug 1963 | Warner Bros. buys Reprise | Warner Bros. Records |
| 1963 | Columbia Pictures buys Aldon Music and with it Dimension (Colpix since 1958; Colgems 1966; Bell 1969) | Aldon Music, Colpix, Colgems, Bell Records |
| 1964 | Tower (Capitol) founded; Tollie and Interphon (Vee-Jay) founded; Parrot (London) founded | Tower, Tollie, Interphon, Parrot Records |
| 1965-68 | Kama Sutra distributed by MGM; from 1969 by Buddah | Kama Sutra Records |
| 1966 | Golden World bought by Motown | Golden World Records |
| mid-1967 (coded July) | Dunhill sold to ABC (distributed by ABC from 1965) | Dunhill Records |
| Jul 1967 | Seven Arts completes its merger with Warner Bros. | Warner Bros.-Seven Arts |
| 1967 | Transamerica buys United Artists; Liberty follows in 1968 | United Artists, Liberty Records |
| 1967 (coded November) | Atlantic (Atco, then Cotillion 1968) becomes a Warner-Seven Arts subsidiary | Atlantic Records |
| 1960-Apr 1968 | Stax, Volt and Satellite distributed by Atlantic; sold to Paramount (Gulf+Western) May 1968 | Stax Records |
| 1968-69 | Apple distributed by Capitol; Calendar by RCA (1969) | Apple, Calendar Records |

Distribution links that are uncertain are recorded with the source text. Swan to Cameo-Parkway is quoted as "at
the time" of the dance crazes, with no years, and is set to 1960-64. Chancellor to ABC-Paramount has no end year
in its source and is set to end in 1962. Philles to Jamie/Guyden is described only as "initially" and is set to
1961-62. Monument had London distribution until about
1960 and is left unlinked, as in section 12. Two new links touch 1960. Swan to Cameo-Parkway changes the
any-affiliation all-slots row: CR8 is 61.5, not 60.6, and the firm count is 41, not 42 (one-hit firms 23, not
24). Dunes to Big Top changes nothing, since Big Top had no 1960 Top 10 slot. The excluding-carryover figures are
unchanged, because Swan's 1960 hit was a 1959 carryover.

### Per-year results

All figures are shares of the year's 520 weekly Top 10 slots. "Excl. carryover" drops records already in the
previous year's last Top 10, as Peterson and Berger (P-B) did. The operating-label bootstrap SD resamples the year's
records with replacement (2,000 draws). It measures the sampling noise of one year's chart. CSVs are in
`SimLogs/historical-top10-<year>/`, with the cross-year table in `SimLogs/historical-top10-summary.csv`.

#### 1960

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **28** | **52** | **24** | 39 | 20 |
| Operating label | 34.6 | 52.7 | 18.1 | 49 | 31 |
| Owned imprints | 39.0 | 59.4 | 20.4 | 44 | 26 |
| Any affiliation | 39.0 | 61.5 | 22.5 | 41 | 23 |
| Any affiliation, excl. carryover (P-B basis) | 39.4 | 59.8 | 20.4 | 40 | 22 |

Operating-label bootstrap SD: CR4 4.6, CR8 4.6. Leading operating labels: RCA Victor 66, Columbia 42, MGM 40,
Decca 32. Ranks 5-8: Mercury 32, Warner Bros. 23, ABC-Paramount 23, Cameo 16.

#### 1961

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **27** | **48** | **21** | 39 | |
| Operating label | 27.9 | 43.5 | 15.6 | 51 | 27 |
| Owned imprints | 28.1 | 43.7 | 15.6 | 47 | 23 |
| Any affiliation | 28.3 | 44.6 | 16.3 | 43 | 20 |
| Any affiliation, excl. carryover (P-B basis) | 26.7 | 43.0 | 16.3 | 42 | 19 |

Bootstrap SD: CR4 3.7, CR8 4.1. Leaders: RCA Victor 44, Liberty 42, Parkway 31, Decca 28. Ranks 5-8: Columbia 23,
Scepter 20, United Artists 19, Dot 19. Liberty's Bobby Vee, Gene McDaniels and Timi Yuro make it the second
firm. Imperial (Ricky Nelson) is still independent until 1963.

#### 1962

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **25** | **46** | **21** | 41 | |
| Operating label | 26.7 | 43.3 | 16.5 | 53 | 29 |
| Owned imprints | 31.7 | 50.0 | 18.3 | 47 | 24 |
| Any affiliation | 33.7 | 52.9 | 19.2 | 41 | 17 |
| Any affiliation, excl. carryover (P-B basis) | 31.3 | 51.6 | 20.3 | 41 | 18 |

Bootstrap SD: CR4 3.9, CR8 4.2. Leaders: RCA Victor 50, ABC-Paramount 32, Cameo 29, Vee-Jay 28. Ranks 5-8: Parkway
25, MGM 22, Decca 20, Atco 19. With Cameo, Parkway and Swan together, Cameo-Parkway is the largest firm (61 slots).

#### 1963

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **26** | **55** | **29** | 36 | |
| Operating label | 26.3 | 41.9 | 15.6 | 54 | 30 |
| Owned imprints | 36.5 | 55.2 | 18.7 | 40 | 19 |
| Any affiliation | 36.5 | 55.8 | 19.2 | 38 | 17 |
| Any affiliation, excl. carryover (P-B basis) | 36.1 | 55.4 | 19.3 | 36 | 15 |

Bootstrap SD: CR4 3.7, CR8 4.0. Leaders: Capitol 40, Columbia 36, RCA Victor 33, Epic 28. Ranks 5-8: Dot 22, Warner
Bros. 21, Mercury 19, Philips 19. Columbia with Epic and Okeh (66 slots), and Mercury with Smash and Philips (51), are
the two largest owned groups.

#### 1964

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **34** | **51** | **17** | 37 | |
| Operating label | 29.2 | 43.7 | 14.4 | 55 | 32 |
| Owned imprints | 37.7 | 57.9 | 20.2 | 38 | 19 |
| Any affiliation | 37.7 | 58.7 | 21.0 | 36 | 17 |
| Any affiliation, excl. carryover (P-B basis) | 37.9 | 59.9 | 22.1 | 34 | 16 |

Bootstrap SD: CR4 4.1, CR8 4.3. Leaders: Capitol 58, Philips 33, Epic 32, Motown 29. Ranks 5-8: Red Bird 20, Vee-Jay
20, RCA Victor 19, Reprise 16. The Beatles' 1964 singles are split across Capitol, Vee-Jay, Tollie and Swan. With
Tollie and Interphon, the Vee-Jay group is the fourth any-affiliation firm (39 slots).

#### 1968

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **42** | **61** | **19** | 30 | |
| Operating label | 28.1 | 44.0 | 16.0 | 49 | 26 |
| Owned imprints | 42.9 | 60.2 | 17.3 | 35 | 15 |
| Any affiliation | 49.2 | 68.1 | 18.8 | 29 | 10 |
| Any affiliation, excl. carryover (P-B basis) | 48.4 | 69.0 | 20.5 | 28 | 12 |

Bootstrap SD: CR4 4.2, CR8 4.3. Leaders: Atlantic 59, Columbia 39, Capitol 25, Dunhill 23. Ranks 5-8: Buddah 23,
Apple 21, Tamla 20, A&M 19. Warner-Seven Arts (Atlantic, Atco, Warner Bros., Reprise, and Stax/Volt until April)
holds 110 slots, 21%.

#### 1969

| Basis | CR4 | CR8 | Firms 5-8 | Firms | One-hit firms |
|---|---:|---:|---:|---:|---:|
| Peterson & Berger, Table 1 | **42** | **64** | **22** | 31 | |
| Operating label | 26.0 | 43.3 | 17.3 | 48 | 27 |
| Owned imprints | 40.2 | 59.0 | 18.8 | 33 | 14 |
| Any affiliation | 43.8 | 65.8 | 21.9 | 26 | 9 |
| Any affiliation, excl. carryover (P-B basis) | 41.6 | 64.1 | 22.5 | 26 | 9 |

Bootstrap SD: CR4 3.7, CR8 4.2. Leaders: RCA Victor 46, Columbia 35, Tamla 28, Fantasy 26. Ranks 5-8: Apple 25,
Roulette 23, Dunhill 21, Epic 21. Motown's four labels together are the largest firm (71 slots).

### What the seven years show

**Operating-label concentration is flat after 1960.** Over 1961-69 the operating-label CR4 is 26.0-29.2 (mean 27.4,
year-to-year SD 1.2), CR8 is 41.9-44.0 (mean 43.3, SD 0.7), firms 5-8 hold 14.4-17.3, and 48-55 labels chart. The
year-to-year spread is far inside each year's own bootstrap noise of about 4 points. 1960 is the high year
(34.6 / 52.7), 1.6 (CR4) to 2.0 (CR8) bootstrap SDs above the later mean.

**The decade's rise in concentration is ownership, not operating labels.** With imprints folded into owners, CR4
goes from 28 in 1961 to 43 in 1968. With distribution links added, it goes from 28 to 49. Peterson and Berger's
rise from about 26 to 42 is the same movement: Columbia/Epic, Mercury/Smash/Philips, MCA/Decca/Uni/Kapp,
Warner-Seven Arts/Atlantic and Transamerica/Liberty/UA are more and more one company each. The number of operating
labels with a Top 10 hit hardly changes (49 in 1960, 48 in 1969). The number of corporate firms falls from 41 to 26.

**Where the reconstruction and Peterson-Berger disagree by more than 5 points** (P-B basis: any affiliation, slots,
excluding carryover):

| Year | CR4 recon. / P-B | Diff. | CR8 recon. / P-B | Diff. | Firms recon. / P-B |
|---:|---|---:|---|---:|---|
| 1960 | 39.4 / 28 | **+11.4** | 59.8 / 52 | **+7.8** | 40 / 39 |
| 1961 | 26.7 / 27 | -0.3 | 43.0 / 48 | -5.0 (at the threshold) | 42 / 39 |
| 1962 | 31.3 / 25 | **+6.3** | 51.6 / 46 | **+5.6** | 41 / 41 |
| 1963 | 36.1 / 26 | **+10.1** | 55.4 / 55 | +0.4 | 36 / 36 |
| 1964 | 37.9 / 34 | +3.9 | 59.9 / 51 | **+8.9** | 34 / 37 |
| 1968 | 48.4 / 42 | **+6.4** | 69.0 / 61 | **+8.0** | 28 / 30 |
| 1969 | 41.6 / 42 | -0.4 | 64.1 / 64 | +0.1 | 26 / 31 |

- Five of the seven years disagree by more than 5 points on CR4 or CR8. 1969 matches to within half a point, and
  1961 is at the threshold on CR8.
- The firm counts match well (within 3 every year except 1969, 26 against 31). P-B's grouping of labels into
  firms is close to this one; the concentration figures are the part that does not reproduce.
- Their 1961-63 CR4 values (27, 25, 26) are within 1.7 points of the **operating-label** CR4 (27.9, 26.7, 26.3).
  Their 1963 CR8 (55) is the consolidated value (55.4). A mix of bases would give that pattern, but it cannot be
  tested from the paper.
- The links that are not certain do not close the gaps. Dropping Philips-Mercury lowers the 1963-64 P-B-basis
  CR4 by 2-4 points. Dropping Atlantic-Warner-Seven Arts lowers the 1968 CR4 by 3.7, and dropping Stax's Atlantic
  distribution lowers it by 2.7. Dropping Swan-Cameo-Parkway lowers the 1962 CR4 by 1.4 and the 1963 CR8 by
  0.6.

The P-B series is therefore usable as a **direction** (consolidated-firm concentration rising across the decade)
but not as yearly point targets, which extends the conclusion section 12 drew from 1960. The timing also differs:
in the reconstruction the consolidated CR4 rises steadily from 1961 (26.7, 31.3, 36.1, 37.9 on the P-B basis),
while P-B's stays at 25-27 until 1963 and then rises.

**Where the existing simulation lands (reference only).** Two five-year seed-2002 runs (`ab5ctl-2002-260` and
`smallcost-fiveyear-v1-paired-home-2002-260`, in `SimLogs/major-family-investigation/historical-benchmarks.json`)
give operating-label CR4 of 25.0-37.5 and CR8 of 40.2-53.1 over 1961-64. Their 1961-64 means are 30.3 / 48.5 and
29.3 / 44.3, against the reconstruction's 27.5 / 43.1. That is one seed on older code, with 47-64 charting labels
against 51-55 in history.

### Proposed operating-label review bands (proposal, not adopted)

Center each band on the reconstruction and use ±5 points, about 1.2 times one year's bootstrap SD. That allows
for the noise in history's single draw, not only for the simulation's seed noise. 1965-67 were not reconstructed;
their provisional band uses the 1961-69 mean, since the measured years are flat.

| Year | CR4 center | CR4 band | CR8 center | CR8 band | Firms 5-8 (ref.) | Labels with a Top 10 hit (ref.) |
|---:|---:|---|---:|---|---:|---:|
| 1960 | 34.6 | 30-40 | 52.7 | 48-58 | 18.1 | 49 |
| 1961 | 27.9 | 23-33 | 43.5 | 39-49 | 15.6 | 51 |
| 1962 | 26.7 | 22-32 | 43.3 | 38-48 | 16.5 | 53 |
| 1963 | 26.3 | 21-31 | 41.9 | 37-47 | 15.6 | 54 |
| 1964 | 29.2 | 24-34 | 43.7 | 39-49 | 14.4 | 55 |
| 1965-67 (provisional) | 27.4 | 22-32 | 43.3 | 38-48 | 15.9 | about 52 |
| 1968 | 28.1 | 23-33 | 44.0 | 39-49 | 16.0 | 49 |
| 1969 | 26.0 | 21-31 | 43.3 | 38-48 | 17.3 | 48 |

- Because the measured years differ by less than their own noise, one pooled 1961-69 band (CR4 22-32, CR8
  38-48) is statistically equivalent to the per-year rows. Use per-year rows only for 1960 against the rest.
- **Do not aim the operating-label measure at the late-decade rise.** History's operating-label CR4 does not rise
  from 1964 to 1969. If the review wants the consolidation trend (for example, a Major share rising in 1968-69),
  measure it on owned or affiliated firms. There the reconstructed CR4 is 28.1 / 31.7 / 36.5 / 37.7 / 42.9 / 40.2
  (owned) and 28.3 / 33.7 / 36.5 / 37.7 / 49.2 / 43.8 (any affiliation) for 1961, 1962, 1963, 1964, 1968 and 1969,
  with the same ±5. Those figures depend on the uncertain links listed above by up to 4 points.
- These bands replace the Peterson-Berger-centered bands in section 2 only for the operating-label measure. The
  section 2 bands remain a description of the published series.

**Reproduction.**

```text
python -I SimTools/research_top10_fetch.py YEAR SimLogs/historical-top10-YEAR [CACHE]
python -I SimTools/research_top10_fetch_issues.py YEAR SimLogs/historical-top10-YEAR ISSUE_DIR
python -I SimTools/research_top10_wiki_labels.py YEAR SimLogs/historical-top10-YEAR [CACHE]
python -I SimTools/research_top10_print_labels.py YEAR SimLogs/historical-top10-YEAR ISSUE_DIR
python -I SimTools/research_top10_assign_labels.py YEAR SimLogs/historical-top10-YEAR
python -I SimTools/research_top10_concentration.py YEAR SimLogs/historical-top10-YEAR
python -I SimTools/research_top10_summary.py SimLogs/historical-top10-summary.csv 1960 1961 1962 1963 1964 1968 1969
```

`fetch_issues` keeps only each issue's text (`bb-DATE.txt`, plus `.src` with the source URL) and deletes the PDF.
For image-only scans it calls `SimTools/research_top10_winocr.ps1` (Windows.Media.Ocr). The 1960 print step reads
the eight section 12 issues. Each year's directory holds `top10_weekly_YEAR.csv`, `records_YEAR.csv`,
`records_YEAR_labels_raw.csv`, `records_YEAR_print.csv`, `records_YEAR_final.csv` (label and `labelSource`),
`records_YEAR_grouped.csv` (operating label, owned and affiliated firm), `concentration_YEAR.csv` and, except
1960, `print_issues_YEAR.csv`.

## 14. Adopted: reconstruction-centered operating-label bands (2026-10-16)

The user adopted the section 13 bands. `analyze_label_tier_historical_benchmarks.py` now reviews every complete
1960-69 year that all runs share: **1960 CR4 30-40, CR8 48-58**; **1961-69 (pooled) CR4 22-32, CR8 38-48**. The
Peterson-Berger series stays in the output as a description only. Each year's review also carries the reconstructed
reference (CR4, CR8, firms 5-8, labels with a Top 10), and each run's annual line marks out-of-band values with `*`.
The section 8 bands (23-33 / 47-57) are retired. `--radio-push-spread` stays off: it was the fix for a flag that
these bands remove, and it moves CR8 and firms 5-8 away from the reconstruction (section 12).

**1960, five seeds (re-review, no new runs):** `WITHIN_BANDS`, CR4 37.85 and CR8 55.00. Seeds 1001 (low) and
4004 and 5005 (high) are individually out of band; the review judges the seed mean, as before.
Output: `SimLogs/historical-top10-review-v3/review-five-seed-1960.json`.

**1961-64, descriptive only (one seed, no manifest review possible):**

| Seed 2002 | 1961 | 1962 | 1963 | 1964 | History 1961-64 |
|---|---|---|---|---|---|
| Adopted defaults (`smallcost-fiveyear-v1-paired-home`), CR4 / CR8 | **37.5 / 53.1** | 26.0 / 42.5 | 25.0 / 40.2 | 28.9 / 41.2 | 27.9-29.2 / 41.9-43.7 |
| Firms 5-8 | 15.6 | 16.5 | 15.2 | 12.3 | 14.4-16.5 |
| Labels with a Top 10 | 50 | 59 | 64 | 60 | 51-55 |
| Older control (`ab5ctl-2002-260`), CR4 / CR8 | 27.9 / **49.2** | **35.2 / 50.4** | 26.4 / 44.4 | 31.7 / **49.8** | |

On the adopted defaults, 1962-64 sit within 2.6 points of history on both measures. 1961 is out of band on both,
but it is one seed in one year, and the 1960 seed SD is about 7 points. The adopted run also has more labels with
a Top 10 hit in 1962-64 (59-64 against 53-55). `ab5ctl-1001-260` is an incomplete run (empty label directory,
truncated deal ledger) and was excluded. Output: `SimLogs/historical-top10-review-v3/five-year-descriptive.*`.

A formal 1961-64 review needs three five-year seeds on one assembly. The seed-2002 run was built on assembly
`E682FC70…54A7`; the current build is `21A2517C…E8CC`, so it cannot be merged into a new manifest. About 30 minutes
per five-year run.

## 15. Formal 1960-64 review, three five-year seeds (`top10-fiveyear-review-v1`)

Seeds 1001, 2002 and 3003, 260 weeks, adopted defaults, `-Modes default -Compact` arguments, one assembly
(`37E62AA3…`, a rebuild of the committed tree with no source changes since `21A2517C…`). The three were dispatched
in parallel with a scratchpad variant of `RunSmallCostPilot.ps1` that passes identical args and writes the same manifest
shape. The sequential equivalent (slower, same args) is
`& '.\SimTools\RunSmallCostPilot.ps1' -RunTag '<new tag>' -Weeks 260 -Seeds @(1001,2002,3003) -Modes @('default') -Compact`.
All passed, exit 0, empty error logs. Seed 2002 reproduces `smallcost-fiveyear-v1-paired-home-2002-260` year
for year, and the 1960 values of all three seeds match `historical-top10-review-v2`. Output:
`SimLogs/top10-fiveyear-review-v1/` (`runs.json`, `review.json`, `review.txt`).

| Year | CR4 mean (seeds) | CR4 band / hist | CR8 mean (seeds) | CR8 band / hist | Status |
|---|---|---|---|---|---|
| 1960 | 33.78 (26.9 / 37.1 / 37.3) | 30-40 / 34.6 | 50.26 (43.1 / 56.7 / 51.0) | 48-58 / 52.7 | WITHIN_BANDS |
| 1961 | **35.71** (32.9 / 37.5 / 36.7) | 22-32 / 27.9 | **49.10** (44.2 / 53.1 / 50.0) | 38-48 / 43.5 | **REVIEW** |
| 1962 | 27.05 | 22-32 / 26.7 | 42.56 | 38-48 / 43.3 | WITHIN_BANDS |
| 1963 | 29.55 | 22-32 / 26.3 | 45.00 | 38-48 / 41.9 | WITHIN_BANDS |
| 1964 | 27.44 | 22-32 / 29.2 | 39.94 | 38-48 / 43.7 | WITHIN_BANDS |

**1961 is a lag, not a level problem.** History falls from 34.6 to 27.9 in 1961; the sim holds its 1960
concentration for one more year and only reaches history in 1962. Every seed is above 32 in 1961. In seeds 2002 and
3003 the Major tier keeps 36-40% of 1961 Top 10 slots and drops to about 29% in 1962. Seed 1001 differs: its Majors
already fall to 24% in 1961, and a MidTier label (Surreal Sound, 10.6%) leads instead. So the 1961 head has two
carriers across seeds. The cause of the 1961-62 step was not investigated. One candidate is the opening roster and
its pre-1960 careers carrying over a second year.

**Side finding: Top 10 turnover is low in every year.** The sim has 96-105 distinct Top 10 records a year, against
102 in 1960 and 111-119 in 1961-64 historically. This is separate from the 1961 excess, because it holds in the
years that pass.

Firms 5-8 run 11-17.5 against history's 14.4-16.5, and labels with a Top 10 hit run 50-67 against 51-55. Both are
descriptive, not banded.

## Reproduction and evidence

New analysis: `SimTools/analyze_label_tier_historical_benchmarks.py`.
Output: `SimLogs/major-family-investigation/historical-benchmarks.json`.

```text
python SimTools/analyze_label_tier_historical_benchmarks.py SimLogs/ab5ctl-2002-260 SimLogs/smallcost-fiveyear-v1-paired-home-2002-260 SimLogs/smallcost-paired-year1-holdout-v1-paired-home-1001-52 --output SimLogs/major-family-investigation/historical-benchmarks.json
```

Validation: all **11 annual samples** have 52 observed chart weeks and exactly 520 distinct rank-1-10 slots;
all annual chart-unit totals exactly match the existing `concentration.csv` files; record-week rows are unique;
all selected first-time signing events have zero prior contracts; and all three acquisition ledgers contain zero
absorptions. The JSON records annual tier slot shares, leading firms, genre shares, signing cohorts and limitations.
No simulation assembly was built or executed for that original reanalysis; section 8 records the separate trial.

Downloaded source PDFs and page images are temporary reading material under `tmp/pdfs/label-share-research/`.
They are not new game assets. The report's public links identify the original sources.
