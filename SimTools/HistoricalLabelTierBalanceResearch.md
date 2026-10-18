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

## 16. The 1961 excess is Top 10 run length, not roster or volume

Logs only: the three `top10-fiveyear-review-v1` runs and the reconstruction. Script:
`SimTools/analyze_top10_1961_lag.py`, output `SimLogs/top10-fiveyear-review-v1/lag-1961.json`. "Opening roster"
means artists with no `RuntimeFormation` or `EnabledInitialReserve` cohort, i.e. on a label at the start of the run.

**Accounting.** CR4 = (top-four firms' Top 10 records) x (their weeks per Top 10 record) / 520, both counted within
the year as in the historical files.

| 1961 | Head Top 10 records | Head weeks per record | Rest weeks per record | CR4 |
|---|---:|---:|---:|---:|
| History | 32 | **4.53** | 4.52 | 27.9 |
| Sim, seeds 1001 / 2002 / 3003 | 27 / 31 / 31 | **6.33 / 6.29 / 6.16** | 4.72 / 4.85 / 4.70 | 32.9 / 37.5 / 36.7 |

The sim's head has about as many Top 10 records as history's (29.7 against 32). With history's 4.53 weeks per
record they would give CR4 25.9. History's 32 records at the sim's 6.26 weeks would give 38.5. **The 1961 excess is
all run length.** The head's Top 10 record count also falls from 1961 to 1962 by about the same amount in both
(sim 29.7 to 23.3, history 32 to 25).

**Who carries the 1961 head.** Opening-roster artists: 59-100% of each head firm's 1961 slots, 3-9 acts per firm.
In seeds 2002 and 3003 the head is the one or two Big-5 firms whose opening rosters were strongest (Mercury and
RCA; Decca and RCA). In seed 1001 it is two opening-roster MidTier labels (Surreal Sound, Glide).

**The opening roster is the population, not the cause.**
- *Artist persistence is not high.* Share of a year's Top 10 slots held by artists with a Top 10 slot the previous
  year is 22.5 / 41.2 / 30.2% in 1961, against 39.2% in history (normalized artist names; 1962: sim 23-32,
  history 41).
- *Opening-roster records do not run longer.* Their 1961-63 Top 10 weeks per record are 5.30 / 5.16 / 5.30, against
  5.28 / 5.22 / 5.13 for later artists.
- *What runs longer:* every Top 10 record does, by about 15% (5.2 weeks against history's 4.5). In two seeds, Big-5
  upper-half-quality records also add more: 6.79 and 7.62 weeks, against 5.19 and 5.31 for other labels' upper-half
  records. Seed 1001 has a smaller gap (6.11 against 5.76), and its head premium comes from MidTier firms.
- *The head premium is steady in the sim and alternates in history.* Head minus rest is +1.2 to +1.6 weeks in the
  sim's 1961. History's is +1.84, +0.01, +1.33, +0.07 and +0.75 in 1960-64. History's 1961 is a no-premium year,
  so part of the gap is history's own single draw. The 1961 bootstrap SD is 3.7 points, and the gap is 7.8.

**The 1962 Major step-down is a one-year lag of a historical dip, and it recovers in 1963.**

| Big-5 Top 10 slot share | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| History (RCA Victor, Columbia, Capitol, Decca, Mercury operating labels) | 34.2 | **20.6** | 21.2 | 26.0 | 18.7 |
| Sim, three-seed mean | 36.1 | 30.3 | **24.6** | 29.9 | 24.8 |

Big-5 Hot 100 entries (three-seed mean) are 154 / 103 / 90 / 107 / 119 for 1960-64. Entries by opening-roster
artists decay 152, 81, 55, 55, 44. Later artists' entries build 2, 23, 35, 52, 75. The 1962 trough is the point
where the opening roster has faded and the replacements are not yet charting. Big-5 Hot 100 to Top 10 conversion is
21-26% for 1960-63 releases (16% in 1964; by release year), so the dip is volume, not conversion. The cause of the
opening roster's decay rate (releases, drops or ageing) was not separated: `release-strategy.csv` does not log every
Big-5 single, and drop events carry no label.

**Implications (nothing changed).**
- The lever for the 1961 flag is Top 10 duration: about 15% too long overall, plus a Big-5 premium on
  upper-half-quality records. That is the same quantity as the section 15 turnover finding, since 520 slots
  divided among about 100 records (history: about 115) is the weeks per record. A fix should target it directly and be checked on the #1 tenure and
  chart-life guards, since those share the duration. The premium is not attributed. The push-spread probe
  (section 10) moved closed chart life by only +0.01 weeks, so weekly label push is not an established cause.
- Roster, signing and opening-career changes are not supported by this evidence. The opening roster carries
  the slots but behaves like everyone else on persistence and run length.
- The Big-5 timing lag is real but secondary. It would matter for a Major-share-by-year guard more than for CR4.

## 17. Top 10 duration: the excess is the shape of the top of the chart, not how long records stay

Logs only, no new runs. Script: `SimTools/analyze_top10_duration.py`, output
`SimLogs/top10-fiveyear-review-v1/top10-duration.json`. It reads the three `top10-fiveyear-review-v1` runs and the
1960-64 Billboard Top 10 reconstruction. It also reads two older paired probes whose logs survive: the five-seed 1960
push-spread probe (section 10) and the seed-1001 no-chart-floors probe from the regional-sync work
(`sync-ctl-1001-52` against `sync-floors-1001-52`, `--probe-sync-no-floors`). Unless a table says "within year",
it counts each record's whole Top 10 stay over 1960-64. It excludes records whose stay touches the first or last
week of the window. Reproduce with
`python SimTools/analyze_top10_duration.py --output SimLogs/top10-fiveyear-review-v1/top10-duration.json`.

### 17a. Decomposition of the extra Top 10 weeks

**Stays by peak position already match history. The sim has too few records that peak at #7-10.**

| 1960-64, uncensored | Top 10 records | Weeks | Entry → peak | Peak → exit | Re-entries | Gone the week after entering at #8-10 |
|---|---:|---:|---:|---:|---:|---:|
| History | 509 | **4.93** | 1.70 | 2.38 | 2.9% | **17.3%** |
| Seeds 1001 / 2002 / 3003 | 440 / 444 / 445 | **5.74 / 5.71 / 5.67** | 1.66-1.71 | 3.00-3.06 | 0.5-1.6% | **7.7-9.5%** |

| Peak band | History: share / weeks | Sim (three seeds): share / weeks |
|---|---:|---:|
| #1 | 18.9% / 8.39 | 22-27% / 7.76-8.38 |
| #2-3 | 20.6% / 6.48 | 28-32% / 6.38-6.41 |
| #4-6 | 27.1% / 4.53 | 25-30% / 4.74-4.86 |
| #7-10 | **33.4%** / 2.36 | **14-22%** / 2.62-2.94 |

Records climb to their peak on history's schedule. Within each peak band they stay about as long as history's
records, up to 0.4 weeks longer in the lower bands. The difference is the mix. History's Top 10 has many records
that reach #7-10 for two or three weeks and leave. The sim has half as many. Its Top 10 records go on to #1-4. If
the sim's per-band stays are combined with history's peak mix, the result is **4.95 / 5.20 / 5.09 weeks**, against
history's 4.93. **The peak mix accounts for about 80% of the excess.** The rest is in the lower bands' tails (see
inertia below).

**The peak mix is set by how far apart the ranks are in chart points.** A record peaks at rank k when its best
week falls between the points of rank k and rank k-1. So peaks per rank should follow the log-points spacing
between adjacent ranks. They do, in both series. Peaks per unit of spacing are roughly constant: 144-232 in the
sim and 532-773 in history.

| Rank | History peaks | Sim peaks (mean of three seeds) | Sim log spacing to rank above | Hesbacher spacing | Sim points / #10 | Hesbacher / #10 |
|---:|---:|---:|---:|---:|---:|---:|
| 1 | 96 | 107.7 | - | - | **6.41** | **1.91** |
| 2 | 50 | 75.0 | 0.49 | 0.092 | 3.92 | 1.74 |
| 3 | 55 | 55.3 | 0.29 | 0.085 | 2.95 | 1.60 |
| 5 | 48 | 38.3 | 0.22 | 0.074 | 1.89 | 1.37 |
| 7 | 51 | 26.7 | 0.13 | 0.066 | 1.39 | 1.20 |
| 9 | 42 | 16.7 | 0.10 | 0.060 | 1.11 | 1.06 |
| 10 | 38 | 15.0 | 0.10 | 0.057 | 1.00 | 1.00 |
| 11 | - | 29.7 | 0.09 | 0.054 | 0.91 | 0.95 |

History's peaks are flat across #2-10, which is what Hesbacher's nearly even spacing produces. The sim's
published #1 runs at **6.4x #10** (median weekly ratio; 5.0-9.1x across seed-years, steeper in no particular
year), against Hesbacher's 1.91x. Its gaps between ranks are five times Hesbacher's at the top and twice
Hesbacher's at #7-10, so peaks pile up at #1-3 and thin out at #7-10. The handoff measured #1/#10 at 2.44x on
`d7-drop-decade-522-1001` (August). Every records log that survives, from 2026-10-15 on, reads 5-9x, so when the
top steepened cannot be dated from these logs.

**The spread is airplay.** Medians, as a ratio to the #10 record in the same week:

| Rank | Points | Units | Airplay points |
|---:|---:|---:|---:|
| 1 | 6.2-6.6 | 1.43-1.77 | **16.8-24.2** |
| 5 | 1.80-1.96 | 1.10-1.30 | 3.5-4.2 |
| 10 | 1.00 | 1.00 | 1.00 |
| 20 | 0.44-0.47 | 0.54-0.64 | 0.10-0.19 |

Units are nearly flat across the Top 10. Ranks inside the Top 10 are set by the fifth-power airplay term
(`AIRPLAY_CONVEXITY`). For records still climbing, airplay is about 56% of the points at #10 and 84% at #1. At Top 10 entry, the stay
a record eventually gets is predicted by airplay and heat, not units. Rank correlations: age at entry −0.58,
airplay points +0.34, radio heat +0.28, quality +0.22, units −0.08, momentum −0.12. The records that stay seven
weeks or more enter in week 5 of their life, against week 8 for records that stay one or two weeks, with 2.4x the
airplay and the same units. Saturation is negligible: 0.003-0.005 at entry. National awareness is 0.60 in every
Top 10 row. Word of mouth correlates negatively, through age.

**Two other mechanisms were measured.**
- *The #10/#11 step is real but not the cause.* Entering the Top 10 raises next-week units growth from 1.15-1.19x
  to 1.35-1.46x. Radio heat growth rises from 0.012 to 0.055 a week, from the radio position bonus (0.25 against
  0.10). The units step is the 0.7 awareness floor plus the chart signal (3.0 against 2.0). In the control's demand
  stages, the aware fraction is 0.78 at #9-10 and 0.56 at #11-12. Removing all floors (`sync-floors`) took the units
  step from 1.215 to 1.029. It left Top 10 records at 96 and weeks per record at 5.42, both unchanged (one seed,
  52 weeks).
- *Inertia (`GetInertiaPositionCap`, `BASE_INERTIA` 0.80) holds 6.9-7.2% of Top 10 slot-weeks* for records whose
  raw rank is 11-13 (median 12). It is label-neutral: Big-5 6.4-7.5%, others 6.9-7.3%. It also denies 26-35 records
  per seed (about 6 a year) any Top 10 week at all. They stall at a published #11, which is why #11 has twice the
  peaks of #10. As a static upper bound it is about 6 of the 15-20 missing records a year. Within-year weeks per
  record would fall from about 5.2 to about 4.8 if held slots went to the raw top ten.

### 17b. The Big-5 premium is label push in the airplay term

Upper-half quality is quality ≥ 0.792, the median of uncensored Top 10 records. Whole stays, 1960-64:

| Weeks per Top 10 record | Big-5, upper half | Others, upper half |
|---|---:|---:|
| Seed 1001 / 2002 / 3003 | 6.64 / 7.36 / 7.61 | 6.23 / 5.87 / 5.84 |
| Pooled | **7.19** | **5.99** |
| Big-5 per-band stays at the others' peak mix | 6.83 | - |
| Others' per-band stays at the Big-5 peak mix | - | 6.34 |

- **About 30% of the 1.20-week premium is higher peaks.** 42.7% of Big-5 upper-half Top 10 records reach #1,
  against 24.1% of others'. **About 70% is longer stays at the same peak:** +1.05 weeks at #1, +1.03 at #2-3,
  +0.75 at #4-6, +0.27 at #7-10.
- With launch controls (quality, log launch stock, launch awareness, launch career state, genre, seed), the Big-5
  premium is **+0.81 weeks**. It is +0.48 with the peak band also held fixed. Launch stock and launch awareness do
  not separate.
- **Units are not the carrier.** For upper-half records peaking at #1-3, aligned on each record's own peak-units
  week, Big-5 units over the Top 10 cutoff run 1.25 / 1.05 / 0.88 / 0.80 at weeks 0 / 2 / 3 / 4. Others' run
  1.12 / 0.98 / 0.85 / 0.74. Big-5 airplay over the cutoff runs 3.38 / 3.05 / 1.65 / 0.63, against 2.63 / 1.66 /
  0.55 / 0.18 for others: **1.3x at the peak, then 1.8x, 3.0x and 3.5x.** Big-5 radio heat is 0.10-0.12 higher
  throughout. That matches the push gap of about 0.15 x acceptance that `UpdateLabelPush` keeps in the heat target
  while a record is in the top 20. The Big-5 records also keep their station panel longer (1.00 against 0.87 at
  week 4). At week 4, 91% of Big-5 records are still in the Top 10, against 70% of others'. At week 5 it is 67%
  against 36%.
- **Causal check: the push-spread probe closes the premium but not the excess** (five seeds, 1960, paired):

| 1960, five seeds | Control | `--radio-push-spread=0.5` |
|---|---:|---:|
| Big-5 upper-half weeks per Top 10 record | 6.47 | 6.04 |
| Others' upper-half | 5.52 | 5.99 |
| Premium | **0.95** | **0.05** |
| All Top 10 records: weeks / distinct records | 5.20 / 100.0 | 5.32 / 97.8 |

Compressing push moves the extra weeks from Big-5 records to other labels' records. It does not shorten the Top 10.
This squares with section 10's +0.01-week closed chart life: push sets *whose* records the convex top holds, and
the convexity sets *how long* it holds them.

### 17c. Proposed lever and guards (nothing changed)

**Superseded by 17d: this lever was built as a probe and rejected.** The guard table remains the checklist for the next lever.

**Lever: saturate rotation at the top of the airplay ladder.** Add an off-by-default probe switch in
`ChartSimulator.CalculateChartPoints`, for example `--airplay-rotation-ceiling=c`. It would soft-cap each
region's `ownRotation` near `c` before the fifth power: `x / (1 + (x/c)^m)^(1/m)` with m ≈ 4, identity when off.
It leaves `AIRPLAY_CONVEXITY` alone, along with radio heat, awareness and demand. Below the ceiling the exponent is
untouched: the body of the chart, debut climb, Top 40 life and genre ordering below the top. The physical claim is
that a Top 40 station's heavy rotation saturated. The records in its top ten were all on the hourly cycle, so
spins could not keep growing 5x per step of heat. One lever reaches both misses:
- *Duration:* it flattens the rank spacing that sets the peak mix. History's mix with the sim's per-band stays gives
  4.95-5.20 uncensored weeks. That is about 4.6 within-year weeks and about 112 distinct Top 10 records a year,
  against history's 4.5 and 111-119.
- *Big-5 premium:* both halves of the premium live where rotation is highest: the extra reach to #1 and 1.8-3.5x
  airplay in the weeks after the peak. A ceiling caps what push buys once a record is already in heavy rotation.
  Unlike push-spread, it does not hand those weeks to other labels' hits, because their airplay is capped the same
  way. Arithmetic only: section 16's 1961 head (29.7 records) at about 4.9 weeks per record gives CR4 about 28,
  against the band of 22-32.

**Calibration path (needs approval, short runs only).** (1) Build and check byte-identity with the switch off,
against one review seed. (2) Run a one-seed 52-week sweep of `c` and read the published #1/#10 ratio, a weekly
aggregate that a 52-week run can resolve. Aim for about 2-3x, not Hesbacher's 1.91x (see the #1 returns guard).
(3) Run three 260-week seeds at the chosen `c`, paired with `top10-fiveyear-review-v1`. That is about 30 minutes in
parallel. No decade run.

**Guards** (current three-seed values; history in brackets):

| Guard | Current | Pass condition |
|---|---|---|
| Target: Top 10 weeks per record, within year | 4.95-5.42 | toward 4.5 (history 4.5; 4.9 in 1960) |
| Target: peaks per rank, #2-10 | 75 falling to 15 | roughly flat (38-55) |
| Target: gone after entering at #8-10 | 7.7-9.5% | toward 17% |
| #1 tenure (whole stay / spell) | 2.17-2.55 / 2.10-2.36 | stays near 2.55 / 2.48 |
| **#1 returns** | 3.3-7.8% | ≤ about 8% (2.9%; decade 4-5%) |
| Closed chart life | 7.31-7.68 weeks | within seed noise (7.48) |
| Hot 100 / Top 40 entries per week | 12.9-13.7 / 4.23-4.32 | within seed noise |
| Genre Top 10 slot shares | TradPop 21-23, RnB 18-21, TeenPop 19-21, R&R 10-12, Soul 6-8% | no genre moves more than seed noise; check `genre-decade-shape` slot error |
| CR4 / CR8 (`analyze_label_tier_historical_benchmarks.py`) | 1960 33.8 / 50.3; 1961 35.7 / 49.1 | 1960 within 30-40 / 48-58; 1961-64 within 22-32 / 38-48 |
| Single units | - | within about 1% (the ceiling changes points, but rank feeds demand) |

**#1 returns is the binding guard.** The sim's median #1/#2 gap is now 1.6x (log 0.49). The survey's two-draw
noise at the top is about 0.13. A top as flat as Hesbacher's (#1/#2 log gap 0.09) would put the gap inside the
noise and bring back ping-pong (`no1-tenure-is-returns-not-run-length`). The ceiling therefore has to stop at a
#1/#10 of about 2-3x. The full Hesbacher flatness at the very top also needs trajectory divergence, which is out of
scope here.

**Secondary lever: inertia.** Lowering `BASE_INERTIA` (0.80) would free about 7% of Top 10 slot-weeks and admit
about 6 more records a year, with no label tilt. It may also hold former #1s close enough to return, so it moves
the returns guard in the same direction. Try it only if the ceiling alone leaves the record count short.

### 17d. Probe result: the rotation ceiling flattens the top but lengthens stays (rejected)

**Switch.** `Systems/AirplayRotationCeiling.cs` adds `--airplay-rotation-ceiling=C`, which is off unless named. In
`CalculateChartPoints` it replaces each region's `ownRotation` with `x / (1 + (x/C)^8)^(1/8)` before the fifth power.
Nothing else reads it. `--airplay-rotation-ceiling-check` (`SimTools/AirplayRotationCeilingChecks.cs`, run through
`SaveLoadRoundTripRunner.tscn`) asserts that it is an identity when off and monotone, capped and nearly untouched
below C/2 when on. It passed, as did `--radio-push-spread-check`. The scale came from logged airplay: the
uniform-equivalent rotation is about 0.40 at #1, 0.29 at #5 and 0.21 at #10.

**Inertness.** Debug build `F97501FB…7322`. A switch-off seed-1001 52-week run (`rotceil-off-check`) is
**byte-identical** to `historical-top10-review-v2-default-1001-52` across all 80 CSVs. All five runs passed, exit 0,
with empty error logs. They were launched four at a time by a scratchpad twin of `RunSmallCostPilot.ps1`, with the
same args (`-Modes default -Compact`) and one `runs.json` per tag under `SimLogs/rotceil-*`. Comparison:
`SimTools/analyze_rotation_ceiling_probe.py`, output `SimLogs/rotceil-sweep/sweep-1001-52.json`.

| Seed 1001, 1960 | Control | C = 0.34 | 0.30 | 0.27 | 0.24 |
|---|---:|---:|---:|---:|---:|
| Published #1 / #10 | 6.63 | 2.71 | 2.40 | 2.15 | 2.25 |
| #1 / #2 | 1.62 | 1.10 | 1.13 | 1.11 | 1.13 |
| Airplay share of #1's points | 0.86 | 0.54 | 0.40 | 0.26 | 0.17 |
| **Distinct Top 10 records** | **101** | 93 | 94 | 100 | 91 |
| **Top 10 weeks per record** | **5.15** | 5.59 | 5.53 | 5.20 | 5.71 |
| Peaks #1-3 / #4-6 / #7-10 / #11-14 | 32 / 19 / 9 / 14 | 32 / 14 / 10 / 20 | 34 / 11 / 9 / 25 | 38 / 11 / 12 / 12 | 31 / 18 / 6 / 20 |
| Gone after entering at #8-10 | 7.3% | 4.1% | 6.7% | 18.5% | 12.5% |
| #1 records / returns | 23 / 4.3% | 25 / 8.0% | 30 / 13.3% | 30 / 3.3% | 22 / 13.6% |
| Closed chart life | 6.22 | 6.44 | 6.14 | 6.06 | 6.09 |
| Hot 100 / Top 40 entries per week | 13.2 / 4.1 | 12.9 / 4.1 | 13.5 / 4.2 | 13.3 / 4.1 | 13.5 / 4.3 |
| Single units vs control | - | +0.6% | +0.6% | +1.0% | +0.3% |
| TraditionalPop / TeenPop / RnB Top 10 slots | 24.6 / 29.8 / 11.3 | 21.5 / 37.7 / 11.9 | 21.0 / 37.1 / 14.8 | 11.3 / 33.5 / 15.4 | 12.9 / 35.6 / 17.1 |
| CR4 / CR8 | 26.9 / 43.1 | 36.3 / 54.2 | 31.5 / 46.3 | 35.0 / 52.9 | 32.7 / 45.6 |

**The spacing target is reached and the duration target moves the wrong way at every C.** Weeks per Top 10 record
rise by 0.05-0.57 and distinct records fall by 1-10. Peaks do not shift toward #7-10. One seed and 52 weeks is
noisy, but the sign is the same at all four settings. The guards also fail where predicted: #1/#2 collapses to
1.10-1.13, which is inside the survey noise, and #1 returns reach 8-14% at three of four settings. TraditionalPop
loses up to half its Top 10 slots once the genre amplifier at the top is cut.

**Why the section 17a counterfactual was wrong.** It held the per-band stays fixed while changing the spacing, but
the two are one mechanism. Aligned on each Top 10 record's peak-units week:

| Weeks after peak units | 0 | 2 | 3 | 4 | 5 |
|---|---:|---:|---:|---:|---:|
| Control: units / #10 cutoff | 0.79 | 0.67 | 0.59 | 0.45 | 0.36 |
| Control: airplay / cutoff | 1.77 | 0.56 | 0.18 | 0.05 | 0.02 |
| Control: still in Top 10 | 96% | 74% | 54% | 32% | 13% |
| C = 0.30: units / cutoff | 1.10 | 0.98 | 0.84 | 0.67 | 0.55 |
| C = 0.30: airplay / cutoff | 0.72 | 0.42 | 0.18 | 0.07 | 0.03 |
| C = 0.30: still in Top 10 | 95% | 78% | 66% | 48% | 27% |

In the control, the convex airplay term is also what ejects a record: rotation falling from 0.40 to 0.25 costs it
90% of its airplay within two or three weeks of the sales peak. The ceiling removes that collapse as well as the
spread. It also lowers the #10 cutoff (the top's airplay shrinks), so units alone now hold records in. Units still
cover 67% of the cutoff four weeks after the peak, against 45% before. **The sim's Top 10 sales tail is the slow
clock; convex airplay was both spreading the top and shortening the stay.** Rank spacing and stay length cannot be
tuned separately through airplay.

**Decision: the ceiling is not a lever.** No 260-week seeds were run. The switch stays in the tree, off and inert,
pending the user's decision on whether to keep it.

**What this leaves.** Stay length is set by how fast a Top 10 record's points fall after its sales peak. Units
fall about 0.8x a week from week 2: 0.79 → 0.45 of the cutoff in four weeks. That is slow beside history's 2.4
weeks from peak to exit. Inertia (section 17a: about 7% of Top 10 slot-weeks, about 6 records a year kept out)
works on the same clock. Next probes, in order:
1. `BASE_INERTIA`. It is label-neutral and needs only an off-by-default switch.
2. The Top 10 sales tail itself: the post-peak decline of a charted record's demand. The 0.7 awareness floor is
   one candidate. Removing it had no effect in the convex regime (17a). Its effect here is untested.

Both need the same #1 tenure and returns guards. Neither touches the Big-5 premium. That premium remains push in the
airplay term (17b): its only tested lever is push spread, which redistributes the weeks rather than removing them.

### 17e. Probe result: lower chart inertia (one seed, 52 weeks)

**Switch.** `Systems/ChartInertiaProbe.cs` adds `--chart-inertia-base=B`, which is off unless named. It replaces
`BASE_INERTIA` (0.80) in `GetInertiaPositionCap`, including the cap on the momentum bonus. B = 0 removes the cap.
`--chart-inertia-base-check` (`SimTools/ChartInertiaProbeChecks.cs`) asserts that it returns the authored base
bit-for-bit when off, that a lower base lets a falling record drop further, that B = 0 returns the raw position, and
that a rising record is never held. It passed, as did the ceiling check (a throwing check exits 3 from
`SaveLoadRoundTripRunner`). The build is Debug `9D6A45FE…a5c0`, and it carries both probe switches. The switch-off run
(`inertia-off-check`) is **byte-identical** to `historical-top10-review-v2-default-1001-52` across all 80 CSVs. All
five runs passed, exit 0, with empty error logs. Output: `SimLogs/inertia-sweep/sweep-1001-52.json`.

| Seed 1001, 1960 | Control (0.80) | B = 0.6 | 0.4 | 0.2 | 0 |
|---|---:|---:|---:|---:|---:|
| Top 10 slot-weeks held by inertia | 7.5% | 2.5% | 1.2% | 0 | 0 |
| Records kept out of the Top 10 | 6 | 3 | 1 | 0 | 0 |
| **Distinct Top 10 records** | **101** | 99 | 106 | 104 | 109 |
| **Top 10 weeks per record** | **5.15** | 5.25 | 4.91 | 5.00 | 4.77 |
| Peaks #1-3 / #4-6 / #7-10 / #11-14 | 32 / 19 / 9 / 14 | 31 / 21 / 6 / 23 | 37 / 16 / 17 / 23 | 40 / 12 / 14 / 16 | 38 / 18 / 13 / 20 |
| Published #1 / #10 | 6.63 | 6.70 | 6.46 | 5.94 | 5.46 |
| #1 records / tenure / returns | 23 / 2.26 / 4.3% | 22 / 2.36 / 0% | 23 / 2.26 / 8.7% | 27 / 1.93 / 0% | 24 / 2.17 / 0% |
| **Closed chart life** | **6.22** | 5.66 | 6.21 | 5.52 | **5.29** |
| Hot 100 / Top 40 entries per week | 13.2 / 4.1 | 13.8 / 4.3 | 13.7 / 4.7 | 14.1 / 4.5 | 14.3 / 4.5 |
| Single units vs control | - | −0.6% | +0.4% | −0.5% | +0.5% |
| CR4 / CR8 | 26.9 / 43.1 | 33.5 / 46.0 | 34.8 / 49.4 | 31.2 / 45.2 | 35.2 / 49.6 |

**Read.**
- *The mechanism works as measured in 17a.* Held slots and kept-out records go to zero. The Top 10 gains up to 8
  records, and weeks per record fall by up to 0.38 (7%), at B ≤ 0.4. It does not touch the top's spacing or the
  peak mix in a systematic way. It is the secondary lever 17a sized, about 6 records a year, not a fix for the whole
  excess.
- *The effect is about the size of the noise.* Unrelated ceiling arms moved Top 10 records over 91-101 on this
  seed. B = 0.6 moved the wrong way. The trend from 0.4 to 0 is consistent but small, and one seed cannot separate
  it.
- *The wrong year for the target.* 1960 is the one year where the sim's Top 10 count already matches history (102).
  The excess is in 1961-64 (111-119 historical against 96-105), which only the 260-week seeds can test.
- *Chart-life guard.* Inertia acts on the whole Hot 100, not only the Top 10. At B = 0, closed chart life falls 15%
  (6.22 → 5.29) and Hot 100 entries rise 8%. B = 0.4 leaves life at 6.21 and entries at 13.7 on this seed, so it is
  the only setting that clears this guard here.
- *CR4 and Big-5.* CR4 rises in every arm (31-35 against 26.9), but seed 1001 is the low 1960 seed (the three-seed
  mean is 33.8), and any change reshuffles the stream (`one-conversion-reshuffles-the-rng-stream`). It cannot be
  read at one seed. Inertia does not act on the Big-5 premium, which is airplay (17b).

**Candidate for the 260-week check: B = 0.4,** paired with `top10-fiveyear-review-v1` (seeds 1001, 2002, 3003).
Run in 17f.

### 17f. Three 260-week seeds at `--chart-inertia-base=0.4` (`inertia040-fiveyear-v1`)

Seeds 1001, 2002 and 3003, 260 weeks, `-Modes default -Compact` arguments plus `--chart-inertia-base=0.4`. Assembly
`9D6A45FE…a5c0` (the 17e build; its switch-off run is byte-identical to the 1960 control). All three passed, exit 0,
with empty error logs, in about 29 minutes in parallel. They were launched by a scratchpad twin of
`RunSmallCostPilot.ps1` that writes one combined `runs.json`. Paired against `top10-fiveyear-review-v1` (assembly
`37E62AA3…`, the same sources without the switches). Outputs in `SimLogs/inertia040-fiveyear-v1/`: `paired-<seed>.json`
and `paired.txt` (`analyze_rotation_ceiling_probe.py`), `review.json` and `review.txt`
(`analyze_label_tier_historical_benchmarks.py`), and `lag.json` (`analyze_top10_1961_lag.py`).

`analyze_label_tier_historical_benchmarks.py` crashed on this run. A runtime-founded Independent ("Chancellor",
`gen_chancellor_6740`) led seed 1001's 1961 Top 10 and had exited before the end-of-run label directory was written.
The script now takes such a label's name from `first-chart-events.csv`. Names are display only, and the control's
re-run `reviews` block is identical to `top10-fiveyear-review-v1/review.json`.

**Target: Top 10 turnover (three-seed means).**

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Distinct Top 10 records: control | 100.3 | 100.0 | 99.0 | 99.0 | 99.7 |
| B = 0.4 | 105.0 | **108.3** | **106.0** | 101.3 | **109.3** |
| History | 102 | 115 | 115 | 119 | 111 |
| Weeks per record, within year: control | 5.18 | 5.20 | 5.26 | 5.25 | 5.22 |
| B = 0.4 | 4.95 | 4.80 | 4.91 | 5.13 | 4.76 |
| History | 5.10 | 4.52 | 4.52 | 4.37 | 4.68 |

Every seed gains Top 10 records in every year except one seed-year (3003's 1963, −1). Over the whole run, records
rise by 30-39 per seed and weeks per uncensored record fall by 0.36-0.44. Held slots fall from 6.9-7.2% to 0.8%, and
kept-out records from 26-35 to 3-6. **It closes about 44% of the 1961-64 record gap (99.4 → 106.2 against 115).**
1960 overshoots history slightly (105 against 102). The Top 10 peak mix does not move consistently: #7-10 peakers
go 58 / 92 / 79 → 88 / 90 / 74. The gain comes from records that inertia was holding or keeping out.

**Guards (three-seed means).**

| Guard | Control | B = 0.4 | History | Read |
|---|---:|---:|---:|---|
| #1 records (five years) | 110.7 | 103.7 | 102 | pass, toward history |
| #1 tenure / spell | 2.36 / 2.24 | 2.51 / 2.41 | 2.55 / 2.48 | pass, toward history |
| #1 returns | 5.2% | 4.2% | 2.9% | pass |
| Closed chart life | 7.50 | **7.20** | 7.48 (decade) | −4%: falls 0.28 below history, outside the 0.37 seed spread in two seeds |
| Hot 100 entries per week | 13.28 | 13.81 | about 13.4 | +4%, now slightly above history |
| Top 40 entries per week | 4.29 | **4.68** | - | +9%, the largest guard move |
| Single units | - | +0.2 / +0.6 / −0.0% | - | pass |
| Genre Top 10 slot shares | - | largest moves RnB +2.9, Soul −2.0, TeenPop +2.0 points | - | pass (within about 3 points) |

**CR4 / CR8 review: one flag traded for two.**

| Year | Control CR4 / CR8 | B = 0.4 CR4 / CR8 | Band | B = 0.4 status |
|---|---|---|---|---|
| 1960 | 33.78 / 50.26 | 31.15 / **46.92** | 30-40 / 48-58 | **REVIEW** (CR8 low) |
| 1961 | **35.71 / 49.10** | 28.27 / 42.37 | 22-32 / 38-48 | WITHIN_BANDS (was REVIEW) |
| 1962 | 27.05 / 42.56 | 28.97 / 42.95 | 22-32 / 38-48 | WITHIN_BANDS |
| 1963 | 29.55 / 45.00 | **35.51 / 50.83** | 22-32 / 38-48 | **REVIEW** |
| 1964 | 27.44 / 39.94 | 27.56 / 45.19 | 22-32 / 38-48 | WITHIN_BANDS |

The 1963 rise is systematic: CR4 is 34.4 / 35.0 / 37.1 in the three seeds, against 25.0-32.3 in the controls.

| 1963 | Head Top 10 records | Head weeks per record | Rest weeks per record |
|---|---:|---:|---:|
| History | 31 | 4.42 | 4.35 |
| Control (1001 / 2002 / 3003) | 28 / 21 / 28 | 6.00 / 6.19 / 5.82 | 5.03 / 5.06 / 4.89 |
| B = 0.4 | 32 / 29 / 30 | 6.03 / 6.17 / 6.07 | 4.67 / 4.67 / 4.83 |

**Inertia shortens the rest of the Top 10 and leaves the head alone.** The head's stays are held by airplay, which
the cap does not touch (17b). The rest's stays include the inertia-held weeks. So lowering inertia widens the
head-rest gap: 1963's head premium goes from about +1.0 to +1.4 weeks. In the years where the sim's head has as many
records as history's or more, that raises CR4. Big-5 upper-half Top 10 stays in 1961-63 stay at 6.36-7.62 weeks,
against 4.99-5.47 for other labels' upper-half records. The premium persists. In 1961 the effect runs the other
way, because there the extra records go to the rest and the head's record count falls (29.7 → 28.0).

**Verdict: a real partial lever, not adoptable alone.** B = 0.4 moves Top 10 turnover about 44% of the way to
history. It brings #1 tenure and #1 records closer to history and keeps returns under control. It costs about 4%
of closed chart life and 9% more Top 40 entries. Because it acts only on non-head records, it shifts the CR4
excess from 1961 to 1963 instead of removing it. Any adoptable fix has to shorten the head's airplay-held stays as
well. The only tested lever on those is push spread (17b, section 10). That probe moved the weeks to other labels,
which is exactly what this lever would then remove. **Next candidate:** B = 0.4 combined with
`--radio-push-spread=0.5`, three 260-week seeds, same guards. Push spread still carries section 10's late-decade
Major-share caveat.

**Not levers, on this evidence:** the push spread (it moves the premium between labels and leaves duration as it
is); the Top 10 awareness floor and chart-signal step (removing them changed nothing); `AIRPLAY_CONVEXITY` itself
(it carries #1 tenure, Top 40 life and the genre ordering throughout the chart, while the defect is only at the
top).

### 17g. Combined inertia / push-spread investigation (2026-10-17)

**Screen before confirmation.** The requested starting point was B = 0.4 with
`--radio-push-spread=0.5`. Three 260-week runs were initially dispatched as
`inertia040-push050-fiveyear-v2`. Their completed 1960 charts already miss the
adopted CR8 band: CR4 **31.41**, CR8 **45.26** (band 30-40 / 48-58), with 104
distinct Top 10 records per seed on average (history 102). Per-seed CR4 / CR8:
30.38 / 43.08, 35.58 / 51.73, 28.27 / 40.96. These are observed completed-year
screens, not a formal review of completed five-year runs.

The user directed that balancing use short runs and five-year runs confirm a
good candidate. All three long runs were stopped at 104-108 observed weeks;
their manifest is marked `STOPPED_AFTER_FAILED_1960_SCREEN`, with `passed=false`.
The unfinished final year and unflushed final audit outputs must not enter a
formal review. The preceding `...-v1` attempt crashed in Godot before startup
under the sandbox and contributes no simulation data.

**Short-run protocol.** Use seeds 1001, 2002 and 3003, 52 weeks, default mode and
compact output, with the same flags and assembly as 17f (`9D6A45FE...a5c0`).
Require both 1960 concentration bands on the three-seed mean, and inspect paired
chart life, entries, sales, turnover and genre changes before promotion. #1
tenure cannot be resolved by a 52-week screen. A setting that clears screening
still needs the 1961-64 concentration and turnover checks in confirmation;
later-decade Major share remains a separate limitation.

`SimTools/RunTop10BalanceProbe.ps1` supplies the same flags as
`RunSmallCostPilot.ps1 -Modes default -Compact`, dispatches at most five engine
processes, defaults to **52 weeks**, records assembly/arguments/completion, and
counts observed weeks from `records.csv` because compact `weeks.csv` is
header-only. `SimTools/analyze_top10_balance_probe.py` compares completed 52- or
260-week configurations, reuses the adopted annual review, reports head/rest
stays and every genre's Top 10 share, and rejects stopped manifests.

**Analysis repair and validation.** The 17f label-name fallback required
`first-chart-events.csv` even for older/minimal logs that did not need it. It now
reads that optional display-name source only when present, otherwise falling
back to the label ID. The existing six review tests pass, including an added
exited-label case; shares are unchanged. The new comparison reproduces the old
control and 17f annual `reviews` blocks exactly, and comparing an arm with
itself produces zero paired changes. Debug build passed with zero warnings or
errors. Both existing probe checks passed with the two switches enabled.

**Completed three-seed 1960 screens (same seeds as 17f).**

| Inertia / push spread | CR4 | CR8 | Distinct Top 10 | Closed chart life | Outcome |
|---|---:|---:|---:|---:|---|
| Defaults 0.8 / 1 | 33.78 | 50.26 | 100.3 | 6.213 | Within concentration bands |
| 0.4 / 0.5 (stopped long runs) | 31.41 | 45.26 | 104.0 | unavailable | Reject: CR8 low |
| 0.4 / 0.75 | 27.24 | 42.82 | 105.0 | 5.817 | Reject: both low; chart life -6.4% |
| 0.6 / 0.75 | 30.71 | 45.06 | 103.7 | 5.853 | Reject: CR8 low; chart life -5.8% |
| 0.6 / 0.9 | 32.18 | 47.44 | 105.7 | 6.075 | CR8 0.56 below band; expand short-run sample |

The 0.4/0.75 runs were dispatched one seed at a time under
`inertia040-push075-screen-<seed>`; `inertia040-push075-screen-v1/runs.json`
combines those three unchanged manifests after verifying their assembly and
flags match. Other short-run tags are `inertia060-push075-screen-v1` and
`inertia060-push090-screen-v1`. Each directory has `comparison.json` and
`comparison.md` from the new analyzer. These chart-life values use the same
52-week window as the control, not the decade's 7.48-week historical reference.

At 0.6/0.9, single units move -0.50% to +0.42% per seed; mean Hot 100 entries
are 13.065 versus 13.052, and Top 40 entries 4.216 versus 4.033. The Big-5
upper-quality stay falls from 6.42 to 5.77 weeks and others from 5.69 to 5.33.
The mean #1 tenure is 1.96 versus 2.23, so multi-year confirmation must guard
that direction. Before further parameter changes, the unchanged setting is
being screened on 4004 and 5005, the two extra seeds used in the established
five-seed 1960 review. Retain the original three-seed miss when reporting the
expanded sample; the extra seeds must not replace or erase it.

**Expanded 1960 screen: 0.6/0.9 clears the five-seed concentration bands.**
All five 52-week runs passed with empty error logs. `inertia060-push090-screen-holdout-v1`
holds 4004 and 5005; `inertia060-push090-screen-five-seed-v1/runs.json` combines
them with the original three, retaining every seed. The paired control is the
established five-seed 1960 sample, merged as `top10-balance-control-five-seed`.

| Five-seed 1960 mean | Defaults | 0.6/0.9 |
|---|---:|---:|
| CR4 / CR8 | 37.85 / 55.00 | **36.69 / 52.08** |
| Distinct Top 10 / within-year weeks per record | 100.0 / 5.20 | 103.6 / 5.03 |
| Closed chart life | 6.081 | 6.118 |
| Hot 100 / Top 40 entries per week | 12.926 / 4.008 | 13.165 / 4.204 |
| #1 records / tenure / spell | 22.8 / 2.30 / 2.29 | 25.4 / 2.06 / 1.99 |
| #1 returns | 0.87% | 3.37% |
| Single units | - | -0.33% aggregate; -1.18% to +0.42% paired |

The larger sample clears concentration and restores the chart-life guard, but
the original three-seed CR8 miss remains 47.44. #1 tenure moves the wrong way
in these short runs. This is a **confirmation candidate, not an adopted default**.
This amends the initial three-seed screening rule for the near-boundary result:
use the established five-seed sample without changing parameters or dropping
seeds, and confirm using those same five seeds. The largest five-seed mean genre
shift is RnB +3.04 points (TraditionalPop -1.96); retain that as a guard too.
`inertia060-push090-fiveyear-confirm-v1` therefore tests the same five seeds for
260 weeks, checking complete annual concentration while running and stopping
on a miss. No new parameter setting was chosen after inspecting the extra seeds.

**Why the initial combination misses CR8.** Against the three-seed control, its
1960 top four lose 2.37 slot-share points and ranks five to eight lose another
2.63. The five points go to firms below the top eight. Narrowing push is not a
transfer from the head into the second rank of hitmakers; on this evidence it
reduces the concentration of both groups. This is a comparison of full-system
outcomes, including the reshuffled random stream, not a fixed-record counterfactual.

The analyzer's explicit `--screen --seeds ...` mode reads frozen CSV bytes and
checks only years with all 52 weeks and 520 unique Top 10 slots. Its output is
marked `formalRunReview=false`; it does not treat a running or stopped process
as a completed audit or supply missing final guard outputs.

**0.6/0.9 confirmation: rejected at the completed 1962 checkpoint.** All five
confirmations reproduce their corresponding 52-week `records.csv` rows
byte-for-byte (`first-year-identity.json`). Complete annual slot screens:

| Year | CR4 | CR8 | Distinct Top 10 | Status |
|---|---:|---:|---:|---|
| 1960 | 36.69 | 52.08 | 103.6 | Within screen bands |
| 1961 | 29.96 | 44.85 | 103.0 | Within screen bands |
| 1962 | **35.12** | **48.04** | 101.2 | Outside screen bands |

The five processes were stopped at 162-171 observed weeks; their manifest is
`STOPPED_AFTER_FAILED_1962_CONFIRMATION`, with `passed=false`. No completed
five-year review or final multi-year guard result is claimed. Evidence:
`inertia060-push090-fiveyear-confirm-v1/annual-screen.json` and
`1962-head-diagnosis.json`.

The original three seeds' 1962 CR4 mean is 30.13, within band. Seeds 4004 and
5005 are 39.42 and 45.77. Their heads have **37 and 41 records**, versus
history's 25, with **5.54 and 5.80 weeks per record**, versus history's 5.56.
Thus these two high-concentration seeds have a head hit-count excess, not the
large stay premium diagnosed in the original three-seed 1961 review. Their
Big-5 slot shares are 37.1% and 48.5%. A stay-only correction cannot address
that particular failure without pushing head stays below history.

Before another confirmation, the existing 0.6/0.75 short screen is being
completed on 4004 and 5005 (`inertia060-push075-screen-holdout-v1`), using the
same five-seed sample for both candidate and control. Its original three-seed
CR8 miss remains recorded above.

**0.6/0.75 expanded screen: reject before confirmation.** The five-seed 1960
mean is CR4 **33.31**, CR8 **47.38**: the low CR8 persists. Turnover is 102.4
records, closed chart life 6.001 versus 6.081, #1 tenure 2.174 versus 2.304, and
#1 returns 1.74% versus 0.87%. Output:
`inertia060-push075-screen-five-seed-v1/comparison.*`. No long run was launched.

**Next bounded interpolation screen:** B = 0.6, F = **0.825**, the midpoint of
0.75 (1960 CR8 low) and 0.9 (1960 passes, 1962 CR4 high). Screen all five
established seeds for 52 weeks as `inertia060-push0825-screen-v1`; require the
same concentration bands and inspect the same guards before any confirmation.

**0.6/0.825 screen: concentration passes; #1 is the confirmation risk.** All five
52-week runs passed, empty error logs. CR4 **33.35**, CR8 **49.77**, distinct
Top 10 records **103.0**, within-year weeks 5.05; closed chart life 6.076 versus
6.081. Single units -0.08% aggregate (-0.75% to +0.29% paired); Hot 100 / Top 40
entries 13.137 / 4.157 versus 12.926 / 4.008. But #1 records rise 22.8 -> 27.2,
tenure falls 2.304 -> **1.913**, spell 2.286 -> **1.796**, and returns rise
0.87% -> **6.70%**. These are warnings, not a claim that #1 tenure is resolved
by one opening year. `inertia060-push0825-fiveyear-confirm-v1` tests the same
five seeds, with the same annual concentration stop rule and #1 behavior a
binding adoption guard. Source screen: `inertia060-push0825-screen-v1/comparison.*`.

Its five confirmation runs reproduce the first-year record logs byte-for-byte.
The completed 1961 screen passes at **28.92 / 42.19**, with 102.8 distinct Top 10
records (history 115). Through both complete years, #1 tenure rises from the
opening year's 1.91 to **2.11**, spell 2.02 and returns 4.92%. That recovery does
not clear the historical #1 guard yet. Evidence: `annual-screen.json`,
`first-year-identity.json`, and the matched-original-three-seed
`no1-checkpoint-1961.json` under the confirmation tag.

**0.6/0.825 confirmation: reject at 1962; no default adopted.** The complete
five-seed 1962 mean is **CR4 34.69 / CR8 50.00**, outside 22-32 / 38-48. Top 10
records are 103.2, versus history's 115. All five runs were stopped at 162-172
observed weeks and marked `STOPPED_AFTER_FAILED_1962_CONFIRMATION`. Their
incomplete 1963 charts and missing final guard outputs are excluded. #1 through
the three complete years remains short at **2.18 tenure / 2.07 spell**, returns
**5.08%**. That is not the #1 improvement seen with inertia 0.4 alone in 17f.

The 1962 head accounting now differs from the original duration diagnosis:

| 1962 | Historical head | 0.6/0.825 five-seed mean |
|---|---:|---:|
| Head records | **25** | **32.6** |
| Head weeks per record | **5.56** | **5.46** |
| Rest weeks per record | 4.23 | 4.81 |
| CR4 | 26.73 | **34.69** |

The original three seeds pass 1962 CR4 at 27.76. Seeds 4004 / 5005 are 43.08 /
47.12, with 40 / 39 head records. One seed-5005 Big-5 label alone contributes
**21 Top 10 records and 143 slot-weeks (27.5%)**. These are full-system outcomes,
not a paired causal estimate for the two additional seeds: they have no matching
five-year default control in the existing archive.

**Accounting counterfactual only:** holding the candidate's mean head record
count at 32.6 and replacing its stay with history's 5.56 gives CR4 **34.86**;
replacing the count with history's 25 at the candidate's mean stay gives
**26.24**. Thus further shortening the head is not the supported repair for
this expanded-sample failure. The head's hit count is high while the rest still
runs long. `1962-head-diagnosis.json` retains the per-seed firms and quantities.

**Verdict for the tested combination family.** The requested 0.4/0.5 pairing
fails its completed 1960 screen. Increasing inertia and weakening compression
can restore the five-seed opening-year concentration, chart-life and sales
guards, but both promoted candidates (0.6/0.9 and 0.6/0.825) fail 1962. The latter
also retains a #1 tenure penalty. No setting tested here has cleared all
historical concentration bands and chart guards; no simulation default was
changed and nothing was committed. All requested probe processes are stopped.

The next investigation should distinguish **hit conversion/volume of the
prolific head firms** from **stay length of the rest**, and preserve the second
rank's share rather than sending head slots to the whole tail. These results
do not establish that every possible constant inertia/spread pairing fails;
they do establish that neither the suggested pair nor the screened/confirmed
alternatives justify adoption. Further interpolation without that diagnosis
would risk selecting a reshuffled random stream instead of a durable repair.

Reproduce a completed short comparison:

```text
python SimTools/analyze_top10_balance_probe.py --candidate SimLogs/inertia060-push0825-screen-v1/runs.json --control SimLogs/top10-balance-control-five-seed/runs.json --output SimLogs/inertia060-push0825-screen-v1/comparison.json
```

Reproduce the explicitly partial confirmation screen (not a completed-run review):

```text
python SimTools/analyze_top10_balance_probe.py --candidate SimLogs/inertia060-push0825-fiveyear-confirm-v1/runs.json --screen --seeds 1001 2002 3003 4004 5005 --output SimLogs/inertia060-push0825-fiveyear-confirm-v1/annual-screen.json
```

Final checks: all six historical-review tests pass; the snapshot reproduces all
five baseline annual concentration reviews exactly and its cumulative #1 values
match the existing completed-run analyzer. All three stopped manifests are
rejected by completed-run analysis. The launcher parses successfully and cleans
up both the console shim and its verified engine child on failure. `git diff
--check` passed; process inspection found zero remaining requested Godot probes.

### 17h. Head hit conversion and volume: matched controls and artist-heat attribution (2026-10-17)

**Question.** The 0.6 / 0.825 midpoint clears the five-seed 1960 and 1961
concentration means, then fails 1962 with 32.6 head records against 25 in the
reconstruction. Does the head release more records, or convert more of its
releases into hits? Short screens precede long confirmation.

**Matched controls change the interpretation.** Two new, completed **168-week**
default runs supply the missing seeds 4004 and 5005. Together with the archived
three 260-week controls, all five now have complete 1960–62 charts. This
supersedes §17g's missing-control caveat for these years, but supplies no new
1963–64 control evidence. Annual figures below are equally weighted seed means;
firms are ranked within each seed/year.

| Setting | 1960 CR4 / CR8 | 1961 CR4 / CR8 | 1962 CR4 / CR8 |
|---|---:|---:|---:|
| Defaults, five seeds | 37.85 / 55.00 | **37.27 / 50.77** | 30.85 / 46.15 |
| Inertia 0.6 / push 0.825, five seeds | 33.35 / 49.77 | 28.92 / 42.19 | **34.69 / 50.00** |

Thus 1962 is not intrinsically outside the bands under every setting. The
midpoint repairs 1961's mean while producing a different 1962 concentration
excess. Its 1962 head stay remains 5.46 weeks against 5.56 historically; defaults
have 27.8 head visitors and a 5.82-week mean stay. Further shortening stays alone
does not address the midpoint's excess hit count.

**Cohort method.** `analyze_head_hit_conversion.py` maps ordinary singles by
`recordId` and album-promoted singles by **`promoSingleId`**, excluding standalone
albums. Cohorts use releases in the first **48 audit weeks of each year**, each
followed for **eight weeks**, with every input observed through week 160. A
release never charting in that window remains in the denominator. Calendar-year
fields on release decisions differ at boundaries, so cohort year is
`1960 + (week - 1) // 52`; the original `releaseCalendarYear` is retained. The
analyzer requires all 100 chart ranks in **every** observation week, unique
record/week and launch identities, and reconciles inferred release ages to
launch weeks. Annual visitors include older releases and carryover, so they
must not be divided by the unrelated release cohort.

| 1962 Big-5 release cohorts, pooled across the same five seeds | Defaults | Midpoint |
|---|---:|---:|
| Actual single launches | 1,060 | 1,063 |
| Hot 100 entrants within eight weeks | 496 | 533 |
| Top 10 hits within eight weeks | 107 | 138 |
| Hot 100 entry rate | 46.79% | 50.14% |
| Top 10 conversion among entrants | 21.57% | 25.89% |
| Launch quality estimate, all cohort releases | 0.5792 | 0.5806 |
| Actual quality, entrants within the window | 0.7025 | 0.7037 |

The **31 extra cohort hits** decompose exactly as `launches × entry rate ×
conditional conversion`, changing factors in that order: **0.30** from launch
volume, **7.68** from chart entry, and **23.02** from conditional Top 10
conversion. This is descriptive accounting, not an isolated causal effect of
one parameter. Quality averages are close, but averages do not establish equal
quality distributions; actual quality in the old logs is observable only for
chart entrants. Big-5 is a stable label set, whereas the annual head is ranked
after observing outcomes and can include newly founded labels.

A descriptive check using actual entrant-quality bins
`[0, .5, .6, .65, .7, .75, .8, .85, 1.01]` covers **all 496 default entrants**.
Reweighting the midpoint's bin-specific conversion rates to the default quality
mix gives **25.81%**, almost unchanged from its observed 25.89%, against
21.57% in defaults. Thus this coarse quality-mix check explains very little of
the conditional-conversion gap. It is not a randomized quality intervention.

**The strongest failure is conversion, not extra Mercury release slots.** In
seed 5005, Mercury (`label_0005`) has **52 launches** in every year for both
defaults and the midpoint. Its annual Top 10 visitors are **12 → 13 → 21** in
the midpoint, against **15 → 6 → 9** in defaults. Its matched eight-week cohorts
give:

| Mercury, seed 5005 | Cohort launches | Hot 100 entrants | Top 10 hits | Entry rate | Conditional Top 10 conversion |
|---|---:|---:|---:|---:|---:|
| Midpoint 1960 | 48 | 33 | 11 | 68.75% | 33.33% |
| Midpoint 1961 | 48 | 31 | 9 | 64.58% | 29.03% |
| Midpoint 1962 | 48 | 38 | 17 | 79.17% | 44.74% |
| Defaults 1962 | 48 | 36 | 7 | 75.00% | 19.44% |

Relative to defaults in 1962, the midpoint's ten extra cohort hits account for
**zero** volume effect, **0.39** entry effect and **9.61** conditional-conversion
effect. Relative to its own 1960, the six-hit rise accounts for zero volume,
1.67 entry and 4.33 conversion. These decompositions depend on factor order.

It is not just an album-promo launch flood. The midpoint's 1962 Mercury cohort
has 25 promos / 23 ordinary singles, with 10 / 7 hits; defaults have **more**
promos, 37 / 11, with 5 / 2 hits. Nor are the 21 annual visitors all from one
opening act: they span 13 artists. Reserve/runtime population contributes
39.58% of the midpoint's 1962 release cohort, against 2.08% in 1960. Population
cohort describes how an artist entered the world, not proof of an opening label
contract. In seed 4004 the 40 head visitors are spread over RCA Victor (17),
Capitol (11), Mercury Music (7) and Phoenix Sound (5), so the head problem is not
identical to a Big-5-only or Mercury-only problem.

**First causal diagnostic: album contributions to singles' artist heat.**
`ChartManager.CalculateArtistHeat` sums current chart standing and retained
peak standing across both singles and albums, plus a hit-count bonus. A Top 10
album alone can contribute 0.45 before the shared 1.0 clamp. Singles receive this
heat in the radio target at a 0.12 coefficient. Album persistence therefore
offers a plausible path from a growing album catalog to later single success;
the old compact logs have no populated album chart with which to measure it.
This is a hypothesis, not a demonstrated explanation for 1962.

The new **off-by-default** `--head-hit-attribution` flag records actual quality
and launch traits for **all** ordinary/promo singles, including chart misses,
and samples each artist's singles/album heat components every four weeks.
The separate **off-by-default** `--single-artist-heat-no-albums` diagnostic uses
only single contributions as the live singles' heat input. Albums keep the
authored input; prewarm is unchanged. It introduces no year-specific rule,
label targeting or release cap.

Instrumentation validation: a 52-week default seed-5005 replay has **all 80
shared CSV files byte-identical** to the archived control, with exactly two
expected additional trace files. The midpoint's new seed-5005 measurement
replay also reproduces all first-52-week records, release decisions and finance
rows exactly. The focused Godot attribution checks exit successfully; they
cover format gating, disabled identity, prewarm, and preserving authored float
addition order for a single-only catalog. The build succeeds with zero warnings
or errors. New runs retain the assembly SHA-256 and flags in their manifests.

**Completed diagnostic, two selected failure seeds.** The 168-week runs finish
cleanly with empty error logs. Seed 5005's new measurement replay matches the
archived midpoint through **all 160 observation weeks** exactly: 16,000 chart
rows, 16,421 release-decision rows and 101,039 finance rows. Seed 4004's comparator
is the archived midpoint. These seeds were selected for their high midpoint
1962 concentration, so this two-seed contrast is provisional and does not
replace a five-seed acceptance mean.

| 1962 diagnostic | Midpoint CR4 / CR8 | Singles-only heat CR4 / CR8 | Head visitors, before → after |
|---|---:|---:|---:|
| Seed 4004 | 43.08 / 57.12 | 36.35 / 48.65 | 40 → 31 |
| Seed 5005 | 47.12 / 60.00 | 41.15 / 52.31 | 39 → 36 |

Mercury in seed 5005 falls from **21 to 14 annual Top 10 visitors**, and from
143 to 80 slot-weeks. Its fixed 48-launch cohort falls from 17 to 12 hits,
with 38 → 35 Hot 100 entrants and 44.74% → 34.29% conditional conversion.
The intervention has a useful full-system effect on this failure, but does not
by itself bring either selected seed into the later-year bands. No individual
seed result constitutes an ensemble acceptance or rejection.

The telemetry supports the proposed feedback path. In Mercury's **sampled
Top 10 slot observations**, album contributions raise artist heat by **0.065 /
0.150 / 0.253** in 1960 / 1961 / 1962. In 1962 the original input averages
0.804, against 0.551 using singles only, and 30 of 35 sampled slots have a
positive album contribution. These are conditional, slot-weighted samples,
not means over all artists or a longitudinally fixed artist cohort. Zero-cache
artists are retained with zero heat rather than dropped from the denominator.

The contrast includes downstream changes in artists, careers and release mix:
Mercury's actual quality across all 48 launches shifts from 0.6635 to 0.6313.
It cannot assign every removed hit to a direct radio-heat effect while holding
everything else constant. Within the two seeds' Big-5 chart entrants, quality
standardization still leaves conditional conversion lower (24.24% versus the
midpoint's 28.70%; observed treatment 22.76%). This is supportive descriptive
evidence, with a small selected sample and a changed simulation path.

The completed 168-week seed-5005 pair also shows a tradeoff: #1 tenure falls
2.333 → 2.127 weeks, spell length 2.182 → 2.074, while returning #1 records fall
6.94% → 2.53%. Closed chart life changes 7.270 → 7.149 weeks and total single
units rise 0.25%. The median #1/#10 points ratio remains steep, 5.169 → 5.133.
These are matched 168-week comparisons, not five-year historical confirmation.

**Fame preservation constraint and narrower follow-up.** The user explicitly
required successful albums to raise hype and single buying (the Beatles case).
Wholesale album-heat removal is therefore retained as a diagnostic only. The
remaining three 52-week ablation runs completed; combined with the first-year
charts of the two 168-week runs, its opening-year mean is **35.46 / 52.08**, with
104.0 distinct Top 10 records. That is a concentration-only screen with mixed
source horizons, not a final lifecycle/economic review. Passing that mean does
not make the full ablation an acceptable fame model.

Code inspection confirms album milestones still feed the enabled persistent
recognition stock. Recognition then raises a new single's launch awareness,
initial retail stock and initial radio exposure. Separately, weekly artist heat
credits both an album's current chart rank and its old peak plus hit-count bonus
for as long as that album remains in the active record set. These are different
mechanisms; the data do not by themselves prove every shared contribution is
erroneous double counting.

The new, also **off-by-default**, `--single-artist-heat-current-albums` diagnostic
preserves **all current-album chart heat**, including 0.30 from a current Top 10
album. It excludes only album peak/hit-count terms from the extra weekly
singles' heat input. Persistent recognition, milestone credit, launch fame
lifts, album simulation and prewarm retain their authored behavior. Both
diagnostic switches cannot be supplied together. Focused checks pass for the
current-album contribution, disabled identity, unchanged album/prewarm inputs
and single-only addition order.

This narrower target follows the attribution: in the failing Mercury run, the
approximately 0.253 album contribution at sampled 1962 Top 10 slots splits into
**0.091 from current album chart heat** and **0.161 from retained peak/hit
credit** after the shared clamp. The latter was only 0.019 in 1960, while the
current-chart part was 0.046. These are descriptive component differences in
the same observed world; their full-system effects still require a new run.
All five 52-week opening screens of the narrower diagnostic complete cleanly.
Their equally weighted mean is **CR4 34.65 / CR8 51.73**, within the 1960 bands,
with **101.6** distinct Top 10 visitors against 102 historically. Mean closed
chart life is 6.184 versus 6.081 default weeks, and mean single units are
156.18m versus 156.69m (−0.33%). Per-seed single-unit changes range −1.35% to
+0.42%. #1 tenure is 2.067 versus 2.304 default weeks, spell length 1.975 versus
2.286, and returns 4.77% versus 0.87%; these short-run #1 quantities require
multi-year confirmation and are a concern, not an acceptance claim.

The narrower candidate is promoted to **five 260-week runs**, checked only at
common complete year checkpoints. No later-year result is inferred from its
1960 screen. Results are recorded in
`SimLogs/head-hit-current-albums-opening-v1/comparison.json` and
`SimLogs/head-hit-current-albums-confirm-v1/runs.json`. Simulation defaults
remain unchanged; all research switches are explicit and nothing is committed.

**Narrower candidate confirmation: closer, still outside in 1962.** Its five
first-year replays match the corresponding completed 52-week runs exactly in
chart records, release decisions, finance and both new attribution streams.
The common completed-year screen gives:

| Year | Current-album hype retained, CR4 / CR8 | Concentration screen |
|---|---:|---|
| 1960 | 34.65 / 51.73 | within |
| 1961 | 28.88 / 44.15 | within |
| 1962 | **33.31 / 46.88** | CR4 high; CR8 within |

In 1962, head records fall 32.6 → **31.0**, head stay moves 5.46 → **5.60**
weeks (history 5.56), and total Top 10 visitors rise 103.2 → **104.0**. The
32% CR4 ceiling is still exceeded by 1.31 points. This confirms progress on the
CR8/second-rank distribution but not a complete historical repair. All five
confirmation engines are stopped after this failed checkpoint (164–173 observed
weeks); their manifests mark them incomplete and they cannot enter a completed
260-week review. No 1963–64 outcome is established.

The remaining failure is no longer chiefly seed 5005's Mercury. Per-seed 1962
CR4 is **44.23 / 29.23 / 26.92 / 31.35 / 34.81** for seeds 1001–5005. Seed
1001 now has 42 head visitors, including RCA Victor's 17 and Mercury's 14.
RCA still has 52 annual launch slots; its fixed 48-launch cohort has 26 Hot 100
entries and 15 hits, a 57.69% conditional conversion. Removing the old album
peak term helps the earlier failure cases, but these results do not establish
it as the sole source of excessive head conversion.

Cumulative three-year #1 tenure/spell are **2.234 / 2.145** weeks and returns
**4.09%**. At the common two-year horizon, the same five seeds give default
2.337 / 2.293, midpoint 2.114 / 2.017, and current-album variant 2.243 / 2.141;
the narrower variant partially recovers the midpoint's #1 penalty. The
historical five-year reference still requires an actual five-year comparison.

One adjacent follow-up retains the same fame-preserving rule and inertia 0.6,
with push-spread **0.8** instead of 0.825. Its five 52-week screens complete
cleanly at **CR4 35.85 / CR8 51.23**, with **102.2** Top 10 visitors. #1 tenure /
spell are 2.451 / 2.378 weeks and returns 2.55%, more promising than the earlier
short screen but not a later-year result. Closed chart life is 6.225 weeks and
mean single units are −0.24% versus defaults. Top 40 entries rise 4.008 → 4.302
per week, requiring attention in long confirmation. Evidence is under
`SimLogs/head-hit-current-albums-push080-opening-v1/`. It is promoted to five
260-week runs in `SimLogs/head-hit-current-albums-push080-confirm-v1/`, with
the same complete-year stopping rule and all fame-preserving behavior retained.

The push-0.8 candidate clears the common **1961 and 1962** checkpoints as well:
**31.42 / 44.85** and **29.42 / 45.00**, respectively. Cumulative three-year
#1 tenure / spell are **2.394 / 2.312** weeks, against **2.269 / 2.191** in the
same five default seeds over exactly the same 156 chart weeks. Returns are
3.30% versus 3.53%. Thus its three-year #1 behavior moves toward the historical
five-year reference rather than repeating the midpoint's #1 penalty.

Matched three-year closed chart life is 7.337 versus 7.464 default weeks, Hot
100 / Top 40 entries per week are 13.430 / 4.382 versus 13.132 / 4.208, and mean
single units are 472.38m versus 473.16m (−0.16%). All five paired single-unit
changes lie within −0.62% to +0.13%. Mean album units rise 0.70%, with per-seed
changes ranging −0.84% to +3.69%; the correction does not disable album demand.
These are common-horizon comparisons using three complete annual unit rollups
and only non-left-censored lifecycle exits observed by week 156. They are
recorded in `common-three-year-guards.json` under the confirmation directory.
Confirmation continues through 1963–64; the later-year result follows.

**Push-0.8 five-year result: four years within, 1963 `REVIEW`.** All five
260-week runs complete (`passed`, exit 0, 260 observed weeks, one assembly
`2C500CBF…`). The formal review (`formal-review.json`) gives:

| Year | CR4 / CR8 | Status | Top 10 records (history) | Head records × stay (history) |
|---|---:|---|---:|---:|
| 1960 | 35.85 / 51.23 | within | 102.2 (102) | 34.4 × 5.42 |
| 1961 | 31.42 / 44.85 | within | 104.6 (115) | 29.8 × 5.51 |
| 1962 | 29.42 / 45.00 | within | 101.8 (115) | 25.4 × 6.02 |
| 1963 | **33.42** / 45.92 | **`REVIEW`** (CR4 +1.42) | 98.2 (119) | 27.6 × 6.37 (31 × 4.42) |
| 1964 | 31.96 / 46.92 | within (0.04 under the ceiling) | 101.4 (111) | 30.0 × 5.57 |

CR8 is inside its band in every year. Five-year guards (five-seed means):
#1 records 106.4 (history 102), tenure / spell **2.448 / 2.347** (2.55 / 2.48),
returns 4.13% (2.9%), closed chart life 7.30 (7.48), Hot 100 entries 13.60 per
week (about 13.4), Top 40 entries 4.47 per week.

Against the three-seed 260-week references (`comparison-seeds1001-3003.json`,
seeds 1001–3003 only, because no five-year defaults exist for 4004/5005), the
candidate is **within the bands in all five years**, and its 1963 (29.04 / 40.71)
is lower than both defaults (29.55 / 45.00) and inertia 0.4 (35.51 / 50.83).
Paired single units move −0.20% to +0.32%, and the largest genre Top 10 slot
change is 2.42 points. #1 tenure is 2.405 against 2.360 (defaults) and returns
are 3.34% against 5.24%. Closed chart life falls 7.497 → 7.363 (−1.8%), Top 40
entries rise 4.288 → 4.472 (+4.3%), and held slots fall 7.0% → 2.7%. The weak
point is turnover: only **12.8%** of the 1961–64 Top 10 record gap closes,
against about 44% for inertia 0.4 alone, and 1963 head stays lengthen
(6.00 → 6.54 weeks against 4.42 historically).

**The remaining excess belongs to seeds 4004 and 5005 under every setting.**
Per-seed five-year mean CR4 is 29.7 / 29.7 / 27.0 / **38.9 / 36.7** for seeds
1001–5005. Pooled over every complete seed-year of the five comparable runs
(defaults, midpoint 0.6 / 0.825, 0.6 / 0.9, current-albums 0.825, and this
candidate), seeds 4004 and 5005 exceed the CR4 ceiling in **24 of 34** seed-years,
while seeds 1001–3003 do so in 6 of 57. Their mean CR4 sits +5.0 / +4.7 points
above the configuration-year mean, against −2.7 to −3.2 for the other three seeds.
The defaults already show the pattern in 1960–62 (4004: 45.4 / 41.0 / 35.4;
5005: 42.5 / 38.3 / 37.7). The tested levers have only moved the excess
from one year to another inside these two worlds
(`seed-concentration-by-variant.csv`).

In each of these two seeds, one Big-5 label dominates.
In seed 4004, RCA Victor (`label_0002`) holds 112 / 127 / 41 / 85 Top 10 slots in
1960–63 (21.5% / 24.4% / 7.9% / 16.3%; RCA held 66 slots in the 1960
reconstruction). In seed 5005, Mercury grows 54 → 62 → 82 → 88. Neither is a
single superstar or the opening cast. RCA's 1960 slots come from 17 records by
13 artists, 102 of 112 from acts without an opening archetype, spread evenly
through the year (35 / 26 / 23 / 28 by quarter). Mercury's 1963 slots come from
12 records by 8 artists. In seed 4004, RCA converts **48.8%** of its 1960 Hot 100
entrants into Top 10 hits, and 58.1% in 1961. The other Big-5 labels convert 26.9%
and 21.9%. Yet RCA's entrants have *lower* launch awareness (0.340 vs 0.381),
initial stock and recognition (0.043 vs 0.104), and similar quality (0.693 vs 0.685).
Their Top 10 records' mean quality is 0.736, against 0.760 for every other label's.

Two observations point at label push rather than music. Across seed × Big-5
label × year (1960–61, 50 points), Top 10 slots correlate with the logged label
marketing (r = 0.47) and spread campaign impact (0.51), but not with quality
(−0.01) or hook (−0.30). These label traits differ by seed. And in RCA's first six
chart weeks in seed 4004, its units are 1.24× and radio heat 1.10× those of the
other Big-5 labels, but its mean chart points are **1.83×**. That is consistent
with the airplay convexity turning a small push edge into a head premium (17b).
Label traits do not fully explain it. Seed 2002's Mercury has similar marketing,
campaign and payola values (0.96 / 0.83 / 0.56 against RCA-4004's 0.92 / 0.81 /
0.55) and holds 61 slots in 1960, not 112. These are descriptive comparisons
from one run per seed, not interventions.

**Reading.** This is the best candidate tested so far: within the bands in 1960–62
and 1964, CR8 within in every year, the #1 guards moving toward history, and units
neutral. The single `REVIEW` is 1.42 CR4 points in 1963, against a five-seed CR4
SD of 6.8 (standard error about 3.0). It comes from two worlds where one major's hit conversion runs
away under every tested setting. A further global push-spread or inertia step
would trade years again, as §17f–g did. If the 1963 flag needs closing, the next
target is the **per-label conversion premium**: the path from label marketing and
campaign strength to radio heat inside the convexity. Year and seed rules are not
a fix. The candidate still leaves Top 10 turnover short of history, and that gap
remains open. Simulation defaults are unchanged and nothing is committed. Codex
started this run; Claude carried out the review because Codex's usage limit ran out.

Evidence: `SimLogs/head-hit-current-albums-push080-confirm-v1/` holds `runs.json`,
`formal-review.json`, `runs-seeds1001-3003.json`, `comparison-seeds1001-3003.json`
/ `.md` and `seed-concentration-by-variant.csv`. The ad hoc diagnostic scripts
that produced the label, launch-trait and radio comparisons are in its
`diagnostics/` folder. Reproduce the paired comparison with
`python SimTools/analyze_top10_balance_probe.py --candidate SimLogs/head-hit-current-albums-push080-confirm-v1/runs-seeds1001-3003.json --output SimLogs/head-hit-current-albums-push080-confirm-v1/comparison-seeds1001-3003.json`.

### 17i. The per-label conversion premium: where push enters radio heat, and a capacity lever (2026-10-17)

**Question.** In seeds 4004 and 5005 one major (RCA, Mercury) converts Hot 100
entrants to Top 10 hits at about twice the rate of the other majors. Its records
have equal or lower quality and recognition (§17h). How does label marketing
reach radio heat, and which lever acts the same way in every seed? All
measurements come from the push-0.8 confirmation runs, 1960–64. Nothing was changed.

**Where the trait comes from.** `ChartSimulator.GetCampaignImpact` is
`marketingPower × (0.45 + 0.55 × budgetLevel)`. `AILabelFactory.ApplyTierStats`
draws both traits per seed, uniformly inside the Major envelope (marketing
0.70–0.98, budget 0.78–1.00). So a major's impact ranges from **0.62 to 0.98**, and
after `--radio-push-spread=0.8` from 0.59 to 0.88. Payola (0.1–0.7) is drawn
separately. The same label is a different-strength promoter in every world. RCA's
marketing is 0.94 / 0.75 / 0.77 / 0.92 / 0.84 in seeds 1001–5005.

**Five routes from campaign impact to the chart** (code trace):

| Route | Where | Size | When |
|---|---|---|---|
| Launch awareness | `ChartManager` release (0.15 × impact), `PromoteRecordAI` broad launch (impact × (0.10 + 0.10 × reach)) | about +0.05 awareness across the Major range | launch only |
| Launch radio seed | release (0.12 × impact), `PromoteRecordAI` (+0.12 × impact), `CompetitorManager.ApplyReleasePromotion` (0.3 × impact + 0.15 × payola) | heat seed, decays at the 0.10–0.28 lerp | launch only |
| Sales ramp floor | `GetReleaseRampWeight(weeks, currentLabelPush)` | floor rises with push | first weeks |
| AI promo awareness | `GetSeasonalMarketingImpact` (budget impact × campaign impact × 0.35) | awareness | launch |
| **Weekly push in the radio target** | `UpdateLabelPush` → `UpdateRadioHeat`: `currentLabelPush × 0.40 × acceptance` | 0.10–0.16 of target heat | **every week; floored at 0.85 while the record is in the top 20** |

The release-age factor drops weekly push to 0.6 / 0.3 / 0.1 after week 3. But
`weekFactor = max(weekFactor, 0.85)` for any record at positions 1–20. A record
that has reached the top 20 therefore keeps 85% of its label's full campaign for
as long as it stays there. That **top-20 hold** is the binding channel. It is
active in **89–99% of every major's Top 10 slot-weeks** in every seed: the head is
almost entirely carried with the hold on. Here push sits inside the fifth-power
airplay convexity. The 1.10× radio-heat edge of RCA-4004's early chart weeks
becomes 1.83× chart points (§17h), as described in
`airplay-convexity-amplifies-label-push`.

**The runaway is concurrency, not one record.** The hold is paid in full to
*every* record a label has in the top 20, however many there are.

| Big-5 label, 1960–64 | Mean records in the top 20 per week | Weeks with 3+ in the top 20 | Top 10 slots |
|---|---:|---:|---:|
| RCA, seed 4004 (lead) | **2.53** | **128** of 260 | 405 |
| Mercury, seed 5005 (lead) | **2.26** | **98** | 335 |
| Leading label, seeds 1001–3003 | 1.30–1.58 | 20–47 | 196–261 |
| All other Big-5 labels | 0.52–1.64 | 0–56 | 38–244 |

Across the 25 seed × label pairs, Top 10 slots correlate 0.63 with campaign
impact and 0.98 with concurrency. Concurrency partly *is* the outcome, so that
correlation is not causal. It does mark where an additional hold pays out: on a
label's third and later records.

**History sets a reference for concurrency.** Matching the weekly Billboard Top 10
(`historical-top10-YEAR/top10_weekly_YEAR.csv`) to the reconstruction's operating
labels (over 98% matched each year):

| 1960–64 | Mean of the top label's concurrent Top 10 records | Weeks one label holds 3+ of the Top 10 | 4+ |
|---|---:|---:|---:|
| History | 1.74 | **8.9%** | 0.8% |
| Seeds 1001 / 2002 / 3003 | 1.66 / 1.55 / 1.56 | 7.3 / 4.6 / 6.2% | 0 |
| Seed 4004 | 2.15 | **31.5%** | 6.2% |
| Seed 5005 | 1.92 | **14.2%** | 1.2% |

History allowed a label two concurrent Top 10 hits routinely and three
occasionally. The three ordinary seeds match that, or run slightly under it. The
two failing seeds exceed it by 1.6–3.5×. It is a sharper discriminator than annual
CR4 and a record-level mechanism rather than a year-specific target.

**Proposed lever (not built): promotion capacity on the top-20 hold.** Add an
off-by-default switch such as `--label-hold-capacity=H`. Each week, before
`UpdateLabelPush`, rank each label's *held* records: positions 1–20 by the
position `UpdateLabelPush` reads, whose release-age factor is below 0.85. The H
best-placed keep the 0.85 floor. Any further held records fall back to their
release-age factor (0.1–0.3 after week 7). Launch awareness, launch heat, the
ramp, the momentum floor (0.7) and the push spread are untouched, and it needs no
RNG draw, so with the switch off a run is byte-identical. The physical claim is
that a label's promotion staff and distributors' promo men were finite: they
worked the label's biggest records and let its third or fourth chart record ride.
This is the same constraint the player's promo mechanic already prices in
copies, hours and miles. It reads no seed, label identity, year or tier. It applies
to every label every week, including any Independent that builds a hot streak.

**Ranked, not split.** A proportional split (`0.85 × H / n` for all held
records) would also cut a label's #1. The static exposure of the two forms at the
observed paths differs sharply:

| Static exposure, 1960–64 | Ranked H = 2 | Split H = 2 | Ranked H = 3 |
|---|---:|---:|---:|
| Top 10 weeks touched, seeds 1001–3003 | 0.5–0.9% | 4.8–6.2% | 0–0.1% |
| Top 10 weeks touched, seeds 4004 / 5005 | **3.8 / 1.6%** | 16.6 / 10.7% | 0.8 / 0.1% |
| Lead label's Top 10 weeks touched, 1001–3003 vs 4004 / 5005 | 2.0–4.2% vs **16.3 / 6.3%** | 13.8–25.7% vs 60.5 / 33.7% | 0–0.5% vs 4.2 / 0.3% |
| #1 weeks touched | **0** | some | 0 |
| Mean heat-target cut where touched | 0.11–0.16 | 0.08–0.11 | 0.07–0.16 |

Ranked H = 2 is the candidate. It barely touches the three ordinary worlds,
concentrates on the runaway label in 4004 and 5005, and never removes push from a
#1. The cut where it bites is about the size of the Big-5 heat premium in §17b. H = 3
would miss seed 5005's Mercury almost completely. The table understates exposure:
the same rule also cuts the label's held records at #11–20 and slows their climb,
and the freed weeks then reshuffle. It is a static count on the observed path, not
a prediction. Only a run can say where the freed weeks go.

**Calibration path (needs approval).** (1) Build the switch and confirm byte
identity with it off against one push-0.8 seed (`probe-run-byte-comparison`).
(2) Run five 260-week seeds with ranked H = 2 on the current candidate stack
(inertia 0.6, push 0.8, current-album heat, about 55 minutes in parallel). (3) If it
clears, run one arm with the push spread restored to 1.0. A capacity that acts only
on overflow might make the global spread unnecessary. That would return the push
spread's turnover cost (§10, §17b), and the late-decade Major-share caveat would
no longer apply.

**Guards** (in addition to §17c's table):

| Guard | Current (push-0.8) | Pass condition |
|---|---|---|
| CR4 / CR8, every year 1960–64 | 1963 CR4 33.42 `REVIEW` | all within bands |
| Seed 4004 / 5005 five-year mean CR4 | 38.9 / 36.7 | toward the 27–30 of the other seeds |
| Weeks one label holds 3+ of the Top 10 | 4.6–31.5% by seed | toward history's 8.9%; seeds 1001–3003 not driven below about 4% |
| #1 tenure / spell / returns | 2.448 / 2.347 / 4.13% | no worse; ranked form should not move #1s |
| Closed chart life, Hot 100 / Top 40 entries | 7.30, 13.60 / 4.47 | within seed noise |
| Major tier slot share; late-decade Major share | 38.1% (1963) | no structural fall; must not block the 1968–69 consolidation goal |
| Single units, genre Top 10 slots | - | within about 1%, about 3 points |

**Open risks.** The freed weeks may go to the same label's next record or to the
second major in the same world. Concentration would then move rather than fall,
and CR8 would be the warning sign. The rule makes a label's later hits weaker
during a streak, and that is visible to the player. It should also bind the
player's label to the same capacity, but that path is not yet checked. The
seed-specific trait draw (impact 0.62–0.98) remains. Narrowing the Major
marketing envelope is a separate option. It is identity-level, flattens label
personality, and would not have separated RCA-4004 from Mercury-2002 (similar
traits, 112 vs 61 slots), so it is not recommended as the first lever.

Evidence and scripts: `SimLogs/head-hit-current-albums-push080-confirm-v1/diagnostics/`
(`push_hold_concurrency.py`, `hold_capacity_exposure.py`, `hold_capacity_ranked.py`,
`concurrent_top10_history.py`).

### 17j. Probe result: ranked hold capacity H = 2 (`label-hold-h2-confirm-v2`, stopped at 1962)

**Build.** `Systems/LabelHoldCapacity.cs` adds the off-by-default
`--label-hold-capacity=H` switch. `ChartManager.SimulateWeek` ranks each label's
held singles before the update loop: positions 1–20, read before the week's chart,
with a release-age factor below 0.85 after the week's increment. Records beyond the
H best-placed skip the top-20 floor in `UpdateLabelPush` and fall through to the
momentum rule (0.7 if momentum > 0.15 before week 14), else their release-age
factor. The release-age taper moved unchanged into
`ChartSimulator.GetReleaseAgePushFactor`. The switch draws no random numbers and
logs a `LABEL_HOLD_CAPACITY` line with yearly held and overflow record-weeks.
`--label-hold-capacity-check` (`SimTools/LabelHoldCapacityChecks.cs`) covers the
taper, ranking, kept and overflow push, the momentum fall-through, other labels,
young records, albums and switching off. It passes, as do the push-spread and
inertia checks. The build has 0 warnings and 0 errors (assembly `984F88DA…`).

**Inertness with the switch off.** A 52-week seed-5005 run on the push-0.8 stack
(`label-hold-off-identity-v1`) is byte-identical to
`head-hit-current-albums-push080-opening-v1` in **all 82 CSV files**.

**Run and stopping rule.** Five 260-week seeds were run on the push-0.8 stack plus
`--label-hold-capacity=2`, with a yearly watch. The run was to stop at the first
complete year whose five-seed mean CR4 or CR8 left its band. The first launch
(`-v1`) died about 60 seconds in, when `RunTop10BalanceProbe.ps1` read an empty
`records.csv` tail; that progress line is now guarded. The `-v2` relaunch was
**stopped after 1962** at weeks 160–168, so its manifest marks every run
incomplete. It cannot enter a five-year review. Only 1960–62 are established.

| Year | push-0.8 CR4 / CR8 | H = 2 CR4 / CR8 | Screen | Top 10 records (push-0.8 → H = 2) | Weeks one label holds 3+ of the Top 10 |
|---|---:|---:|---|---:|---:|
| 1960 | 35.85 / 51.23 | 33.73 / 49.69 | within | 102.2 → 103.8 | 15.0 → 12.7% |
| 1961 | 31.42 / 44.85 | 30.81 / 45.85 | within | 104.6 → 101.8 | 15.4 → **8.8%** |
| 1962 | 29.42 / 45.00 | **32.08** / 46.12 | **breach (+0.08)** | 101.8 → 100.4 | 5.8 → 11.2% |

**It removes the targeted runaway once, then the concentration comes back.** In
1961 the lever does what §17i predicted. Seed 4004's CR4 falls 45.4 → **31.0**, its
label 3+ weeks 48.1 → 5.8%, and the five-seed concurrency lands on history's 8.9%.
But the three-year mean CR4 is unchanged: **32.21 against 32.23**. By seed over
1960–62, CR4 changes −4.8 in 4004 and +1.2 in 5005, +2.1 / +1.6 / −0.1 in 1001 /
2002 / 3003. In 1962, RCA in seed 4004 holds 76 slots (41 under push-0.8) and
Mercury in seed 5005 holds 74. The leading label's share rises 9.85 → 11.88%. The
freed hold weeks did not spread to the field. Labels in the runaway worlds keep
dominating through their two kept holds plus full release-week push, which the cap
does not touch, and the reshuffled RNG stream moves the peak year.

**Other guards (1960–62, 156 weeks).** #1 tenure / spell fall to **2.250 / 2.204**,
from 2.394 / 2.312 (history 2.55 / 2.48), returns fall 3.30 → 2.22%, and there are
69.6 #1 records against 65.4. Chart units change −2.08% to +0.89% by seed. Top 10
turnover does not improve.

**Verdict: not adoptable at H = 2.** The capacity is a real mechanism: it takes out
a label's third-and-later held record, and it pulls 1961 concurrency to history.
But it does not lower the multi-year concentration mean. It costs about 0.14 weeks
of #1 tenure, the opposite of the historical gap. The excess in seeds 4004 and 5005
is broader than overflow holds. It reaches the first two held records and
release-week push, through a seed-drawn campaign impact of 0.62–0.98 inside the
fifth-power convexity. A tighter H = 1 would cut each label's second record too
(static exposure about 21% of Top 10 weeks in split form), with a larger #1 cost.
Simulation defaults are unchanged. The switch stays in the tree, off by default,
and nothing is committed.

Evidence: `SimLogs/label-hold-h2-confirm-v2/` (manifest, console logs) and its
`diagnostics/` folder: `yearly-watch.log`, `yearly_band_watch.py`,
`h2_stopped_diag.py`, `compare_run_csvs.py`. The identity run is
`SimLogs/label-hold-off-identity-v1/`.

### 17k. Why seed 5005 keeps breaking the bands, and why H = 2 did not help (2026-10-17, analysis only)

All figures come from completed logs: push-0.8 confirmation 1960–64, defaults
1960–62, and the stopped H = 2 run 1960–62. No new simulation was run.

**Seed 5005 is not a 4004-style runaway; it is a majors-collectively world.**
Seed 4004's excess is one label's *conversion*: RCA turns 38.8% of its entrants into
Top 10 hits, against 9–36% for the other majors. Seed 5005's majors convert at an
ordinary rate (22.1%), but put **146.6 records a year into the Hot 100**, against
111–119 in the other seeds. Launch volume is not the difference: 5005 has 250 Big-5
singles a year and 1001 has 246. The difference is **Hot 100 entry**, 56.9% of Big-5
launches against 44.9–51.0% elsewhere. In 5005, three majors enter at 61–76%:
Columbia 65.3%, RCA 61.5% and Mercury 75.8%. The majors' collective Top 10 share
*rises* 33.5 → 35.6 → 37.5 → **44.2** → 38.1% over 1960–64. In history it falls
34.2 → 20.6 → 21.2 → 26.0 → 18.7%. Three or four of seed 5005's top four firms are
majors every year.

**What predicts a major's Hot 100 entry rate** (25 seed × Big-5 pairs, push-0.8,
1960–64): launch awareness (r = 0.78), campaign impact (0.64), the share of launches
by Established/Star/Superstar acts (0.61) and launch recognition (0.59). The share of
new signings predicts it negatively (−0.66). Distribution reach does not (−0.03 to
−0.05), and neither does quality (0.35). Record-level entry is 89% for Star and
Superstar launches against 38% for new signings, and 17.5% → 82.9% across
launch-stock quintiles. Mercury-5005 has the highest senior share (0.435), recognition
(0.190) and launch stock (83.6k) of any major. Its senior share snowballs from 0.28
in 1960 to 0.57 in 1964, while its slots run 54 → 62 → 82 → 88 → 52. The pattern is
not universal: RCA-4004 started with the *least* senior roster (0.09) and still took
112 slots. Columbia-3003 had the most senior roster (0.37–0.51) and middling slots.
Fame is one input to entry, not the whole explanation.

A label edge is not wrong in itself. A major's Top 10 slots carry over year to year
at r = **0.67** in the sim (within-seed-year demeaned; 0.47 in defaults), against
**0.66** in history over the same five majors. What is wrong is the level.

**The common defect is the majors' collective share after 1960.** Across the
push-0.8 seed-years, CR4 tracks the Big-5's combined slot share at r = 0.85 (0.72 in
defaults). In 1961–64, CR4 ≈ 10.6 + 0.69 × Big-5 share.

| 1961–64 mean | History | Sim (five seeds) |
|---|---:|---:|
| Big-5 share of Top 10 slots | **21.6%** | **30.8%** (by seed 1960–64: 29.9 / 32.8 / 25.8 / 35.4 / 37.8) |
| Big-5 firms among the top four | **1.75** | **3.05** |
| CR4 implied by the fit | 25.5 | 31.9 |

History's 1961–64 top four were filled by national independents and new majors:
Liberty and Parkway in 1961; ABC-Paramount, Cameo and Vee-Jay in 1962; Philips,
Epic and Motown in 1964. The sim's second rank of non-majors is thinner. The top four
non-major firms hold 55–136 slots a year (history 90–114), and 0–4 non-major firms
reach 20+ slots in a year (history 3–5). Seed 5005's non-major field is not unusually
weak on this measure (69–103). Its distinction is the majors' entry rate. To centre
CR4 near 27 in 1961–64, the fit needs the majors' combined share near 24%, about
7 points below today's mean. This confirms §11's "only five firms have head-scale
access" from the year-by-year side. History's majors lost ground after 1960; the
sim's do not.

**Why H = 2 gave no concentration gain.** The cap is small and points at the wrong
level:

- *Small.* The overflow telemetry shows **14–117 overflow record-weeks a year per
  seed**, out of about 1,020–1,135 held record-weeks (1–10%), a fraction of one
  record per week chart-wide. Over 1960–62 the leading label's share falls only
  11.82 → 10.64% of Top 10 slots.
- *Wrong level.* In seed 4004 the freed weeks went to the field: the lead fell
  17.9 → 14.7%, other majors 20.0 → 17.9%, the rest 62.1 → 67.4%. In seed 5005 they
  went to the *other majors*: Mercury fell 12.7 → 9.6% and the other Big-5 rose
  22.8 → **28.0%**. Pooled over the five seeds, the Big-5 excluding the lead moved
  21.13 → 21.42% and the rest 67.05 → 67.94%. The cap acts on one label's third
  record, while the excess sits in the majors as a group, at launch entry.

**Why #1 tenure fell, and how much of it is real.** No #1 week was ever an overflow
record: ranking keeps the best-placed record, so the direct channel is zero. Over
1960–62, tenure fell 2.394 → 2.250, #1 records rose 65.4 → 69.6, one-week #1s rose
35.2 → 38.7%, and the median #1/#2 points ratio fell 1.415 → 1.378. The per-seed
changes are −0.18 / +0.32 / −0.43 / −0.25 / −0.18. The mean of −0.144 is about
**1.2 standard errors**. Neighbouring configurations already differ by that much: the
current-albums variant at push 0.825 against 0.8 gave three-year tenures of 2.234
against 2.394 (§17h). The flatter #1/#2 gap points in the same direction, with less
separation at the very top when a label's third and fourth records lose push. But
the drop is not established as an effect of the lever. It is within the
realization noise that any small change brings by reshuffling the RNG stream.

**Implications.**

1. Seed 5005 is the clearest case of a defect every seed shows: the Big-5 hold
   about 9 points too much of the 1961–64 Top 10 collectively. Seed 3003, the
   lowest-CR4 seed, is the closest to history (Big-5 18–27%).
2. Levers acting inside the top 20 can move weeks *between majors* (push spread,
   hold capacity) but cannot lower the majors' collective entry. The binding channel
   is launch-side: awareness, stock and radio seed, which all read campaign impact,
   plus the recognition of senior acts. That is the Big-5 0.66 against MidTier 0.28
   Hot 100 entry per launch of §11.
3. The historical mechanism is the rise of national independents after 1960, not a
   decline in the majors' promotion. A fix that only taxes major launches would
   lower CR4, but leave the second rank as thin as it is and cost the late-decade
   Major-share goal. Two candidate directions, neither built: a sourced set of
   head-scale 1960 independent templates (§11's probe: MGM, ABC-Paramount, Dot,
   Liberty, Cameo-Parkway, Vee-Jay), or a performance-earned path to head-scale
   access for non-majors that chart (`midtier-bucket-cannot-grow-by-promotion`).

Scripts (scratch analyses, saved with the evidence):
`SimLogs/label-hold-h2-confirm-v2/diagnostics/` (`seed_world_profile.py`,
`big5_launch_volume.py`, `entry_rate_drivers.py`, `roster_snowball.py`,
`residual_label_effect.py`, `label_persistence.py`, `big5_share_by_year.py`,
`cr4_vs_big5.py`, `nonmajor_heads.py`, `h2_where_weeks_went.py`).

Reproduce the existing-controls conversion analysis:

```text
python SimTools/analyze_head_hit_conversion.py --include-new-controls --output SimLogs/head-hit-conversion-investigation-v1/analysis.json
python -m unittest discover -s SimTools -p test_head_hit_conversion.py -v
```

Evidence: `SimLogs/head-hit-default-controls-v1/runs.json`,
`SimLogs/head-hit-conversion-investigation-v1/analysis.json` and its companion
firm, artist and launch-cohort CSVs; `accounting.json`, `trace-identity.json`
and `combo-replay-through160.json` in the same investigation directory. The
diagnostic analysis is `heat-diagnostic.json` with companion CSVs and
`heat-diagnostic-guards.json`. The new opening screens are recorded in
`SimLogs/head-hit-singleheat-opening-v1/runs.json`.

### 17l. Probe design: head-scale 1960 non-major templates (2026-10-17, proposal, not built)

This is §17k's first direction. It uses the existing Billboard reconstruction, the template roster in
`AILabelFactory`, and the Wikipedia history of each label. No simulation was run and no code was changed.

**Three of the six named labels are already templates, and two of them are below head scale.** Cameo-Parkway is
MidTier `TeenHitMachine`. Liberty is Independent `RockRebel`. Vee-Jay is Independent `RegionalHustler`, which
lowers budget, distribution and reach. MGM, ABC-Paramount and Dot are not in the roster. Warner Bros. is also
absent, although it ranks 6th by operating label in 1960 (§12).

**Top 10 slots by operating label, 1960-64** (`SimLogs/head-template-candidates/label_slots_1960_64.txt`;
the owned-imprint figure is in brackets where it differs):

| Label | 1960 | 1961 | 1962 | 1963 | 1964 | Total | 1960 parent / standing (source) |
|---|---:|---:|---:|---:|---:|---:|---|
| MGM (+ Cub) | 40 (56) | 13 | 22 | 1 (5) | 10 (14) | 86 (110) | Film-studio label from 1946; "self-distributed", with its own pressing plant |
| ABC-Paramount (+ Chancellor, distributed) | 23 | 11 (14) | 32 | 18 | 4 (10) | 88 (97) | American Broadcasting-Paramount Theatres, 1955; self-distributed; distributed Chancellor from 1957 |
| Cameo + Parkway | 28 | 31 | 54 | 26 | 4 | 143 | Lowe and Mann, Philadelphia, Dec 1956; Parkway 1958; peak ends with *Bandstand*'s move in Feb 1964 |
| Warner Bros. | 23 | 8 | 12 | 21 (26) | 5 (21) | 69 (90) | Film studio, March 1958; lost at least $3M by 1960; Everly Brothers and Newhart that year |
| Liberty (+ Dolton) | 7 | 42 | 8 | 13 (15) | 12 (27) | 82 (99) | Waronker, Hollywood, 1955; Bobby Vee, Chipmunks, Cochran; sold to Avnet in 1963 |
| Vee-Jay | 3 | 14 | 28 | 13 | 20 (39) | 78 (97) | Carter and Bracken, 1953, Chicago; R&B and blues until the Four Seasons (1962) and the Beatles (1963-64) |
| Dot | 0 | 19 | 5 | 22 | 0 | 46 | Randy Wood, 1950; sold to Paramount Pictures in 1957; Pat Boone, Billy Vaughn, Welk; strongest on albums |
| *Big-5 operating labels* | *178* | *107* | *110* | *135* | *97* | | *34.2 / 20.6 / 21.2 / 26.0 / 18.7% of slots* |

The first five rows together hold **23.3 / 20.2 / 24.6 / 15.2 / 6.7%** of the Top 10 in 1960-64. In 1960-62
that is about 4.5% per firm, or about 24 slots a year: two-thirds to three-quarters of a Big-5 operating label.
In 1963-64 they fade (*Bandstand* leaves Philadelphia, MGM goes quiet, the British Invasion arrives).

**Proposed template list**

| Firm | Action | Tier | Envelope | Archetype | City | Genres |
|---|---|---|---|---|---|---|
| MGM | **new** | MidTier | head | CorporateGiant | New York (to verify) | TraditionalPop, TeenPop, RockAndRoll |
| ABC-Paramount | **new** | MidTier | head | CorporateGiant | New York | TeenPop, RnB, TraditionalPop |
| Warner Bros. | **new** (not in the original list) | MidTier | head, low cash | CorporateGiant | Los Angeles | RockAndRoll, TeenPop, Comedy, TraditionalPop |
| Cameo-Parkway | upgrade | MidTier (unchanged) | head | TeenHitMachine (unchanged) | Philadelphia | unchanged |
| Liberty | upgrade | Independent → MidTier | head | RockRebel → TeenHitMachine | Los Angeles | TeenPop, RockAndRoll, EasyListening |
| Vee-Jay | upgrade | Independent → MidTier | **standard MidTier** | unchanged | Chicago | unchanged |
| Dot | optional new | MidTier | **standard MidTier** | CorporateGiant | Los Angeles | TraditionalPop, EasyListening, TeenPop |

- **Vee-Jay is not seeded at head scale.** Its 1960 was 3 slots. Its head years came from a 1962 signing and the
  1963-64 Beatles licence. Starting it at head scale would write that history in advance. It was a national R&B
  label in 1960, so standard MidTier is the sourced position. Its rise should be left to the run.
- **Dot is optional and not head scale.** It had no 1960 Top 10 and a lumpy 0-22 after that. It was an album and
  easy-listening house. Warner Bros. is the better fifth head firm on the 1960 evidence.
- **Head firms are MidTier, not Major.** A Major tier makes a label a Major-distribution and consolidation actor and
  adds it to every tier-based Major-share measure. A MidTier can distribute up to 6 clients, which matches the
  "ABC around seven, MGM about five" note in `IsEligibleDistributor`. The Big-5 stay `label_0001`-`0005`, so the
  Big-5 share analyses are unaffected.

**Head envelope** (applied only with the switch on). The Big-5 envelope is used for the launch channels, because
§11's structural hypothesis is about access, and about 70% of Big-5 volume, because that is the historical ratio:

| Trait | MidTier | **Head** | Major |
|---|---|---|---|
| marketingPower | 0.48-0.82 | **0.70-0.98** | 0.70-0.98 |
| budgetLevel | 0.52-0.82 | **0.78-1.00** | 0.78-1.00 |
| distributionStrength / nationalReach | 0.50-0.82 / 0.45-0.78 | **0.75-1.00 / 0.75-1.00** | same as head |
| Extra distribution regions | 2-4 | **5-8 (national)** | 5-8 |
| releasesPerMonth | 1.0-2.5 | **1.6-2.4** | 2.0-4.0 |
| maxRosterSize | 15-35 | **25-40** | 30-60 |
| Starting cash | MidTier band | **Major band** (Warner Bros. at the bottom of it) | Major band |
| Scouting, production, payola, loyalty, risk | tier draw | MidTier draw | - |

Major-band cash is needed. Major-scale release volume on MidTier cash would trip `consecutiveLossMonths > 12`
and demote the template for an economics artifact. Tier-keyed channels stay MidTier: the prewarm quota weight (3
against 5), the album and contract multipliers, `SongMaterialSelectionService` access (0.65), and opening-career
eligibility (both tiers are already in `Big`).

**Switch and inertness.** An off-by-default `--seed-head-templates` flag. With it off, no template is created and no
envelope changes, so a run must be byte-identical to a reference probe (`probe-run-byte-comparison`). New templates
go after the existing 42, so `label_0001`-`0042` keep their ids. With it on, the RNG stream changes, so the
comparison has to be multi-seed. Three of the 600 procedural slots are given up.

**Prediction.** By §17k's fit (CR4 ≈ 10.6 + 0.69 × Big-5 share), centring 1961-64 CR4 near 27 needs the Big-5
near 24%, from 30.8. §11's null model with ten head firms gives CR4 28.6-34.6, CR8 47-59 and firms 5-8 18-25.
The probe works if the head templates take their slots **from the Big-5** (opening careers, launch-week radio and
attention), not from the field.

**Pass / kill (five 260-week seeds, against a paired control on the same assembly):**

| Guard | Pass | Kill |
|---|---|---|
| 1960 CR4 / CR8 | 30-40 / 48-58 | CR4 above 40 (§12's warning that MGM raises CR4) |
| 1961-64 CR4 / CR8, five-seed mean by year | 22-32 / 38-48 every year | any year out of band and worse than the control |
| Big-5 share of the Top 10, 1961-64 | down at least 3 points, toward 21.6 | down less than 3 points |
| Big-5 firms among the top four, 1961-64 | toward 1.75 (control 3.05) | no change |
| Non-major firms with 20+ slots a year | 3-5 | - |
| **Field share**: slots held by labels that are neither Big-5 nor templates | falls by less than the Big-5 share does | falls by more than the Big-5 share does |
| **Labels with a Top 10 hit** a year | 48 or more (history 49-55) | below 45 |
| **First-time Top 10 labels** a year | within seed noise of the control | clearly below it |
| Template outcomes across seeds | spread out: some template outside the top 20 in some seed-years | every template in the top 10 firms in every seed-year |
| Template share, 1963-64 | allowed to fall (history 15.2 → 6.7%) | reported; persistence is a finding, not a kill |
| Templates absorbed / promoted to Major | absorbed 0; promotions logged | any absorption |
| #1 tenure, chart life, single units, genre Top 10 slots, Small closures | within seed noise | outside it |

**Known interactions.** The operating-label review counts Cameo and Parkway, and MGM and Cub, as separate labels. A
single template for each therefore reads slightly high on CR4 against the reconstruction (about +1 point in 1960
from Cameo-Parkway). A head template can earn Major promotion (16 charting records, roster of 25, capability
0.78 for four quarters). That would make it a Major distributor and change the late-decade owner-Major path. It is
logged, not blocked, for this probe. Chancellor stays its own template. Seeding its 1957 ABC-Paramount
distribution deal is a separate, deal-seeding question.

**Railroading.** The templates reserve no slots and script no hits. They are AI labels with seeded names and
starting traits, run by the same rules. Procedural labels and the player keep every channel they have. The player
still starts Small, at 0.02 reach, with distribution earned market by market. What does change is competition: the
Top 10 is fixed at 520 slots a year, and stronger firms take a share. The field-share, label-count and
first-time-label guards are there so the slots come out of the Big-5's excess and not out of everyone else. The
control currently has 50-67 labels with a Top 10 hit against history's 51-55, so there is some room. Head
templates as MidTier distributors also add six-client distribution capacity each, which is another route to
national reach for independents. The template fix covers only the 1960 starting roster. Later history (a
Vee-Jay or Motown rising) stays open only if non-majors can earn head-scale access. That is §17k's second
direction (`midtier-bucket-cannot-grow-by-promotion`), and it should follow this probe.

**Sources.** Wikipedia histories: MGM Records, ABC Records, Warner Records, Dot Records, Liberty Records,
Vee-Jay Records, Cameo-Parkway Records (read 2026-10-17); slots from the §12-13 reconstruction. MGM's 1960 office
city is not stated in its article and still needs a check.

### 17m. Probe result: head-scale templates (`headtpl-on-v1`, stopped after 1961)

**Build.** `Systems/HeadScaleTemplates.cs` adds the off-by-default `--seed-head-templates` switch. With it on,
`AILabelFactory` appends MGM, ABC-Paramount and Warner Bros. as `label_0043`-`0045`. It replaces Cameo-Parkway
(`label_0015`) and Liberty (`label_0014`) with head-envelope MidTier templates, and Vee-Jay (`label_0020`) with a
standard MidTier, all as specified in §17l. `HEAD_SCALE_TEMPLATE` console lines record each seed's drawn traits.
Archetype modifiers apply as for every label, so the two TeenHitMachine templates draw 1.8-2.6 releases a month.
Reputation follows the budget envelope, as the factory already does.
`--seed-head-templates-check` (`SimTools/HeadScaleTemplatesChecks.cs`, run through `SaveLoadRoundTripRunner`)
passes. The build has 0 warnings and 0 errors.

**Inertness with the switch off.** A 52-week seed-5005 run on adopted defaults (`headtpl-off-identity`) is
byte-identical to a reference built before the edit (`headtpl-preedit-ref`) in **all 80 CSV files**. It is also
byte-identical to `historical-top10-review-v2-ext-default-5005-52`, an older assembly. The controls are therefore
reused rather than rerun. Seeds 1001-3003 use `top10-fiveyear-review-v1`, which has the same flags. Seeds 4004 and
5005 use `head-hit-default-controls-v1`, a 168-week run that covers 1960-62.

**Stopping rule.** Five 260-week seeds with a yearly watch
(`SimLogs/headtpl-on-v1/diagnostics/yearly_watch.py`). The run stops when the five-seed mean CR4 or CR8 is more
than 2 points outside its band. It stopped after **1961**, at weeks 106-110, so only 1960-61 are established.

| Five-seed mean | 1960 control | 1960 templates | 1961 control | 1961 templates | History 1960 / 1961 |
|---|---:|---:|---:|---:|---:|
| CR4 | 37.8 | 36.6 | 37.3 | 34.6 | 34.6 / 27.9 |
| CR8 | 55.0 | 54.3 | 50.8 | **52.0** (band 38-48) | 52.7 / 43.5 |
| Big-5 share of the Top 10 | 40.4 | 29.1 | 33.8 | **31.3** | 34.2 / 20.6 |
| Big-5 firms in the top four | 3.2 | 2.0 | 3.0 | 2.2 | - |
| Template firms | 1.9 | 26.4 | 1.8 | 21.4 | 23.3 / 20.2 |
| Field: Independent | 26.8 | 18.8 | 30.3 | **21.1** | - |
| Field: MidTier (procedural) | 21.4 | 17.1 | 22.5 | 16.2 | - |
| Field, total (neither Big-5 nor template) | 57.8 | 44.5 | 64.4 | **47.3** | - |
| Labels with a Top 10 hit | 47.6 | 45.4 | 52.4 | 49.0 | 49 / 51 |

**The templates' share is historical. Where it came from is not.** In 1960 the templates took 26.4 points. They
took 11.3 from the Big-5, but 13.3 from the field. In 1961 they held 21.4 points, almost exactly history's 20.2, but
the Big-5 gave up only **2.4** of them. Independents lost 9.2 and procedural MidTiers 6.3. The field fell 17.1
points. That fails three §17l guards in 1961: CR8 (more than 2 points out of band), the field share (it fell by
more than the Big-5 share), and the Big-5 share (it fell by less than 3 points). It is the railroading outcome the
field guard was there to catch. Labels with a Top 10 hit stay above the kill line (49.0 against 45), so smaller
labels still chart. Their share of slots is what falls.

**By seed** (`stopped_1961_diag.txt`):

- **The head envelope is too strong in some worlds.** Cameo-Parkway holds 73 slots in seed 2002's 1960 (history 28
  with Parkway) and 50-65 in 1961 in seeds 2002 and 5005. Liberty holds 53 in seed 4004. MGM holds 52 in seed 3003.
  At Big-5 launch access, a template can become a rank 1-3 firm, which history's second rank never was (its
  1960 maximum was MGM's 40). That is what drives CR8 up: ranks 5-8 fill with 20-40-slot firms on top of an
  intact Big-5 head.
- **The Big-5 excess is not displaced by competition.** Seed 1001's control Big-5 had already fallen to a
  history-like 20.8% in 1961. With the templates it is **31.0%**: the reshuffled stream gave its majors a stronger
  year. In seeds 4004 and 5005 the Big-5 1961 share is 37.9 and 37.3 with templates, against 36.5 and 41.3 without.

**Verdict: not adoptable as specified.** The test confirms §12's warning and §17k's diagnosis from the other side.
Adding firms with head-scale access does not draw slots out of the majors. Their excess sits in their own launch
entry (§17k: 57% Hot 100 entry in seed 5005, senior-act share, launch awareness). Under a fixed 520 slots,
new head mass is paid for by the field. A weaker envelope would cut the runaway templates and CR8, but it still
cannot make the Big-5 give up share, because nothing here acts on Big-5 entry. Templates are a sensible
historical roster only *together with* a lever on the Big-5's launch-side entry. Alone, they move slots from
independents to named firms. Simulation defaults are unchanged. The switch stays in the tree, off by default, and
nothing is committed. The 4004/5005 260-week controls were not run, because the stop came inside their existing
168-week coverage.

Evidence: `SimLogs/headtpl-on-v1/` (manifest, console logs with `HEAD_SCALE_TEMPLATE` lines) and its `diagnostics/`
folder (`yearly_watch.py`, `yearly-watch.log`, `stopped_1961_diag.py`, `stopped_1961_diag.txt`). The identity pair
is `SimLogs/headtpl-preedit-ref/` and `SimLogs/headtpl-off-identity/`. A paired-review script for a completed run is
`SimTools/analyze_head_templates_probe.py`, written for this probe and not yet exercised.

### 17n. The Big-5 launch-side entry edge, and a lever on it (2026-10-17, proposal, not built)

All figures come from completed logs: the push-0.8 confirmation (`head-hit-current-albums-push080-confirm-v1`, five
seeds, 1960-64, singles from `single-launch-attribution.csv`), with a cross-check on the default seeds
`top10-fiveyear-review-v1`. No simulation was run and no code was changed.

**The edge is per launch, and it is the same in every genre.** Per launch, 1960-64, pooled:

| Group | Launches / firm-year | Hot 100 entry | Top 10 / launch | Campaign impact | Launch awareness | Launch stock | Senior share of launches | Quality |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Big-5 | 46.3 | **49.9%** | 11.8% | 0.726 | 0.469 | 72.8k | 31.5% | 0.635 |
| Procedural Major | 28.3 | 36.1% | 6.0% | 0.689 | 0.397 | 68.8k | 20.0% | 0.587 |
| MidTier | 25.2 | 22.3% | 3.3% | 0.521 | 0.287 | 56.7k | 8.8% | 0.617 |
| Independent | 12.8 | 11.6% | 1.2% | 0.381 | 0.198 | 46.1k | 2.1% | 0.606 |

The gap holds at equal career state. New signings enter at 37.8% (Big-5), 16.9% (MidTier) and 9.9% (Independent).
Established acts enter at 58.3%, 47.6% and 40.6%. It also holds within each genre. Big-5 entry against everyone
else's is 64 / 14% in TraditionalPop, 75 / 25% in TeenPop, 60 / 16% in RnB, 65 / 17% in Soul and 38 / 6% in
RockAndRoll. This is not a genre or A&R mismatch: the Big-5 enter at 2.5-5x the field whatever they release.

**Four launch inputs account for the whole newcomer gap, and campaign impact is the largest.** A logit of entry on
log launch stock, launch awareness, campaign impact, payola, quality and hook (genre, year and seed fixed; pseudo
R² 0.67) predicts Big-5 newcomer entry at 35.1%. Moving one input at a time to the MidTier mean:

| Big-5 newcomers, input set to the MidTier mean | Predicted entry |
|---|---:|
| None | 35.1% |
| Quality | 32.2% |
| Log launch stock | 33.4% |
| Launch awareness | 29.1% |
| **Campaign impact** | **25.9%** |
| All four | **17.2%** (observed MidTier 16.9%) |

Against Independents, all four give 9.1% (observed 9.9%). For Established acts, campaign impact alone accounts
for almost the whole gap: 57.7% predicted, 47.0% with MidTier impact, against 47.6% observed MidTier. The impact
coefficient is label-level, so this logit cannot separate launch push from the weekly push in a record's first
weeks. Treat these figures as an upper bound for a launch-only lever. Launch awareness carries impact as well,
because the marketing term in `ApplyReleasePromotion` is `BudgetToImpact × GetCampaignImpact × 0.35`.

**Senior acts amplify the edge; they are not where it starts.** The Big-5 senior share of launches rises from
19.4% in 1960 to 30.7 / 33.5 / 36.5 / **38.1%** in 1961-64. The senior share of Big-5 Top 10 slots rises from
37.5% to 60 / 61 / 70 / **75%**. The default seeds show the same climb (17 → 52%, 32 → 49%, 47 → 65%). In
1961-64, senior acts hold 64.9% of Big-5 slots, against 22.8% of everyone else's. The Big-5 grow these acts
themselves:

- **They sign no senior acts.** All 1,400 Big-5 signings in 1960-64 (five seeds) entered as NewSigning.
- **Senior launches by origin, 1960 → 1964:** opening careers 120 → 64 (falling); acts on the 1960 roster promoted
  on the ladder, about 190 → 245; acts signed during the run and promoted, 0 → **121**.
- **They rarely fall back.** The ladder's Established → Declining rule needs three flops in a row, and at 58% entry
  that is rare. Big-5 launches by Declining acts number 44, against 1,167 by Established acts (ratio 0.04). The ratio
  is 0.24 for MidTier and 0.55 for Independents.

The snowball is the newcomer edge compounded by the shared ladder. Its timing matches the defect. In the push-0.8
log, the Big-5 hold 35.0% of the launch-attributed slots in 1960, close to history's 34.2%, and 29-35% after that,
where history holds about 21%. A lever on the newcomer edge also slows the snowball, and the effect grows after
1960, as the gap does. A lever on seniority alone would hit every label's stars and the shared ladder, while
leaving the source untouched.

**A historical anchor on per-release entry (approximate, to verify).** The sim's Big-5 release about 46 singles
a firm a year, and half of them enter the Hot 100. RCA Victor's 1960 pop singles run through at least catalog
numbers 47-7705 to 47-7777 (Neil Sedaka's "Stairway to Heaven" at 47-7709, Sam Cooke at 47-7730, Elvis Presley's
"It's Now or Never" at 47-7777, July 1960). That is on the order of 100-200 singles a year for one major. A major's
historical entry rate per single was therefore probably well under 50%. The sim gets the 1960 Big-5 share
roughly right with too few releases at too high an entry rate. This rate is a diagnostic, not a target: Hot 100
entries by label for 1960-64 have not been reconstructed (§12-13 cover the Top 10 only).

**What is not proposed, and why:**

- *A senior-act lever* (ladder decay, a seniority discount): it treats the amplifier. Separately, a cleanup finding:
  `AILabel.GetMarketingBudget` still scales the launch marketing budget by discrete careerState (0.8x for a new
  signing to 2.5x for a Superstar), and that budget feeds launch awareness. This counts seniority twice, alongside
  the recognition awareness lift, which the recognition directive said should replace careerState in launch
  demand. It also sets spending, so changing it is an economics change. It belongs in its own pass.
- *A genre / A&R lever*: the Big-5 edge is 2.5-5x in every genre.
- *Launch stock and distribution*: the smallest carrier (about 2 points), and the majors' distribution advantage
  is sourced.
- *Weekly push and top-20 hold* (§10, §17d, §17j): these move weeks between records already charting. §10's weekly
  push 0.5 also pushed 1960 CR8 below band, because the freed weeks went to the long tail.

**Lever: launch-push saturation.** An off-by-default probe switch, `--launch-push-saturation=F` (F in [0, 1],
identity at 1). Above a fixed knee K = 0.55, just above the MidTier mean impact of 0.52, the campaign impact that a
**launch** reads becomes `K + F × (impact − K)`. Below the knee it is unchanged. It applies to two launch reads:

1. `CompetitorManager.ApplyReleasePromotion`'s `pushRadio`, the launch radio heat of every AI single, including
   promo singles and buyouts.
2. `CompetitorManager.GetSeasonalMarketingImpact`, the marketing term in launch awareness. This covers singles,
   the album snapshot and the format-decision projection (`ProjectLaunchAwareness`), which must see the same
   value as the real launch.

It does not touch weekly `UpdateLabelPush`, launch stock, distribution, `AlbumSimulator`, the
`ChartManager` minor paths or label cash. Labels below the knee get byte-identical launch inputs: Independents,
Small, Boutique, most MidTiers and the player, who starts Small. The lever is one-sided on purpose. Compressing
toward a pivot, as in §10, would also raise roughly 110,000 small-label launches. A saturation only removes the part
of the majors' launch edge above regional-label scale. The freed entries are then shared out by merit across the
whole field.

*Physical claim (to be argued, not yet sourced in this document):* after the 1959-60 payola hearings, Top 40
stations worked from tight playlists and added records on local sales and request evidence. A national campaign
bought trade advertising and distributor attention, but beyond a point it could not buy the station add that put
a new record on the Hot 100. What it could still buy, through distribution, stock and a star's name, is left alone.

*Side benefit:* the §17m head templates (MidTier head envelope, impact 0.70-0.98) sit above the knee too, so the
lever would also cut the runaway templates (Cameo-Parkway 73 slots). §17m concluded that templates can only be
adopted together with a Big-5 entry lever. This is that lever.

**Prediction (offline, upper bound).** At F = 0.5, Big-5 launch impact goes from about 0.79 to about 0.67, roughly
half the distance to MidTier. On the logit, that is about −5 points of newcomer entry from radio push, plus about −2
from marketing awareness. Big-5 entry would fall from 50% to about 43% (−14% entries), the 1961-64 Big-5 share from
30.8% to about 26.5% straight away, and lower as the snowball slows. Centring 1961-64 CR4 near 27 needs about
24% (§17k), so F = 0.25 may be needed. In 1960 the lever bites as well. That is welcome in defaults (Big-5 1960
40.4% against 34.2%), but it puts the 1960 CR8 floor at risk, as in §10.

**Calibration path (needs approval):**

1. Build the switch and a `--launch-push-saturation-check` (identity off; knee arithmetic; a label below the knee
   gets the same launch awareness and radio heat). Run 52 weeks on seed 5005 with the switch off, and require
   byte-identity with `headtpl-off-identity` in all 80 CSVs, so the existing controls stay valid.
2. **Screen:** five seeds at F ∈ {0.5, 0.25}, 156 weeks (1960-62), with a yearly watch and §17m's stopping rule.
   Controls are reused: `top10-fiveyear-review-v1` (1001-3003) and `head-hit-default-controls-v1` (4004/5005,
   168 weeks). That is ten runs, four at a time, about 45 minutes.
3. **Confirm** the chosen F over five seeds × 260 weeks. This needs the 4004/5005 260-week controls (two extra
   runs).
4. Then lever + head templates (§17l) as its own paired probe.
5. Two-seed decade A/B at the end of the audit (`decade-ab-only-at-audit-end`). The lever does not depend on era,
   so it lowers Big-5 own-label entry in 1968-69 too. The late-decade goal is owner-family share through
   absorption (`major-share-late-decade-consolidation-goal`), which is a different measure, but it must be read.

**Pass / kill (five seeds, paired against the control):**

| Guard | Pass | Kill |
|---|---|---|
| Big-5 share of the Top 10, 1961-64 | down ≥ 3 points, toward 21.6 (aim about 24) | down < 3 points |
| Big-5 share, 1960 | 30-38 (history 34.2) | below 28 |
| Big-5 firms among the top four, 1961-64 | toward 1.75 (control 3.05) | no change |
| CR4 / CR8, 1960 | 30-40 / 48-58 | CR8 below 46 (§10's tail failure) |
| CR4 / CR8, 1961-64, five-seed mean by year | 22-32 / 38-48 | any year out of band and worse than the control |
| **Field share** (slots of non-Big-5 labels) | rises by what the Big-5 lose, with no single procedural firm taking more than a third of the freed slots | freed slots concentrate in one or two firms |
| **Labels with a Top 10 hit** a year | ≥ 48 (history 49-55) | < 45 |
| First-time Top 10 labels a year | within seed noise of the control, or higher | clearly below |
| Non-major firms with 20+ slots a year | toward 3-5 | — |
| Big-5 Hot 100 entry per launch; senior share of Big-5 launches and slots, 1964 | reported (diagnostics: 50% → about 40; 38% / 75% expected to fall) | — |
| **Genre Top 10 slot shares**, TraditionalPop first | within seed noise | TraditionalPop falls outside noise. The Big-5 hold 58% of TraditionalPop slots and enter it at 64% against 14% for others, so freed slots may change genre |
| Big-5 economics | no Big-5 demotion; cash and `consecutiveLossMonths` reported | any Big-5 demotion |
| #1 tenure, closed chart life, Hot 100 / Top 40 entries per week, single units, Small closures | within seed noise | outside it |

**Known risks.** (1) The logit's impact coefficient includes the weekly push. If the screen shows less than half
the predicted cut, the next step is to extend the saturation to the weekly push of records **not yet in the top
40** (the entry funnel), never to the top-20 hold. (2) A Major record that misses entry loses launch revenue, which
moves label cash over five years. (3) Changing the outcome shuffles the RNG stream (`one-conversion-reshuffles-the-rng-stream`),
so only multi-seed means count.

Evidence: `SimLogs/big5-launch-entry-v1/diagnostics/` (`big5_entry_decomp.py`, `big5_newsigning_decomp.py`,
`big5_senior_origin.py`, `big5_genre_and_drops.py`, with their `.txt` outputs). Catalog numbers: RPM-archive
listings and *Music Vendor*, 18 July 1960 (worldradiohistory.com), found 2026-10-17. They are not a complete
discography.

### 17o. Screen result: launch-push saturation (`launchsat-050-screen-v1`, `launchsat-025-screen-v1`)

**Build.** `Systems/LaunchPushSaturation.cs` adds `--launch-push-saturation=F`, as specified in §17n: knee 0.55,
one-sided, applied to `pushRadio` in `ApplyReleasePromotion` and to `GetSeasonalMarketingImpact`. It is configured
in `ChartManager` next to the other probes. `--launch-push-saturation-check`
(`SimTools/LaunchPushSaturationChecks.cs`, through `SaveLoadRoundTripRunner`) passes: identity when off, identity at
or below the knee, order kept, factor 0.5 halves the excess over the knee. The build has 0 warnings and 0 errors.

**Inertness.** A 52-week seed-5005 run with the switch off (`launchsat-off-identity`) is **byte-identical** to
`headtpl-off-identity` in all 80 CSVs, so the §17m controls stay valid: `top10-fiveyear-review-v1` (1001-3003)
and `head-hit-default-controls-v1` (4004/5005).

**Screen.** Five seeds, 156 weeks, default flags plus the switch. There was a yearly watch, and the stop rule was
tightened to §17n's form: a band missed by more than 2 points **and** by more than the control misses it. F = 0.5
completed all five seeds with empty error logs. F = 0.25 was **stopped after 1960** (`STOPPED.txt` in its folder).

| Five-seed mean | 1960 ctl | F = 0.5 | F = 0.25 | 1961 ctl | F = 0.5 | 1962 ctl | F = 0.5 | History 1960 / 61 / 62 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| CR4 [band] | 37.85 | 35.46 | 31.62 | 37.27 | **29.27** | 30.85 | 28.58 | 34.6 / 27.9 / - |
| CR8 [band] | 55.00 | 49.92 | **45.54** (below 48) | 50.77 | **43.04** | 46.15 | 43.50 | 52.7 / 43.5 / - |
| Big-5 share of the Top 10 | 40.38 | 34.85 | 29.62 | 33.77 | 28.12 | 29.85 | 28.15 | 34.2 / 20.6 / 21.2 |
| Big-5 firms in the top four | 3.2 | 3.2 | 2.8 | 3.0 | **3.0** | 3.2 | **3.4** | - |
| Labels with a Top 10 hit | 47.6 | 52.6 | 55.8 | 52.4 | 56.8 | 56.4 | 58.2 | 49 / 51 / - |
| First-time Top 10 labels | 47.6 | 52.6 | 55.8 | 27.0 | 31.0 | 26.6 | 25.6 | - |
| Non-major firms with 20+ slots | 2.0 | 2.2 | 1.8 | 2.0 | 1.6 | 2.0 | 2.6 | 3-5 |
| TraditionalPop share of Top 10 | 26.2 | 23.5 | **21.0** | 20.4 | 23.0 | 17.8 | 19.0 | - |
| #1 tenure (weeks) | 2.30 | 2.45 | 2.01 | 2.33 | 2.30 | 2.06 | 2.29 | - |
| Hot 100 new entries a week | 12.68 | 12.63 | 12.60 | 15.02 | 14.76 | 14.98 | 14.93 | - |

Per-seed paired Big-5 deltas at F = 0.5: 1960 **−5.5** (SE 2.0; all five seeds down), 1961 **−5.7** (SE 2.5;
all five down), 1962 **−1.7** (SE 4.7; −1.5 / +8.5 / +3.5 / +0.4 / −19.2). CR4 in 1961 is −8.0 (SE 1.5). No Big-5
label was demoted. Freed slots spread out: Boutique and Independent gain 3-5 points each in 1960-61, the largest
single gaining firm takes 10-36 slots in a seed-year, and the reshuffle is as large as the Big-5 loss. MidTier
*loses* about 2 points in 1960-61, because its strongest firms sit above the knee.

**F = 0.25 is killed.** 1960 CR8 is 45.5, below the floor while the control sits inside the band, and below §17n's
kill line of 46. TraditionalPop loses 5.2 points (SE 1.3, all seeds down), which fails the genre guard.

**F = 0.5 passes the concentration and breadth guards but fails the head-composition guard, and its effect
fades.** CR4 and CR8 are within band in all three years, including 1961, which the control misses and no earlier
probe had fixed. Labels with a hit, first-time labels and field share all rise. The guard on "Big-5 firms in the top
four", however, does not move at all (3.0 against 3.0 in 1961, 3.4 against 3.2 in 1962). §17n made "no change"
there a kill. The 1962 Big-5 cut is inside the noise. TraditionalPop is −2.8 in 1960 (SE 1.2) and then +2.6 and
+1.3. #1 tenure is +0.23 in 1962 (SE 0.09), about the size of the neighbouring-configuration differences in §17k.

**Why it fades: the lever reaches newcomers, not senior acts.** Big-5 Top 10 slots per seed-year by launch career
state (`big5-slots-by-career-050.txt`):

| Big-5 slots | 1960 ctl / F = 0.5 | 1961 | 1962 |
|---|---:|---:|---:|
| NewSigning | 79.4 / **64.0** | 48.4 / **33.2** | 39.0 / **31.2** |
| Rising | 41.6 / 25.0 | 29.0 / 25.0 | 30.0 / 26.2 |
| Established + Star + Superstar | 63.4 / 65.6 | 94.8 / 87.2 | 86.2 / 88.0 |

Newcomer slots fall by 8-16 a seed-year. Senior slots do not move, although senior Hot 100 *entries* fall
somewhat (1962: 49.6 → 42.0 records). Once a senior record is in, it converts to the Top 10 as before. By 1962,
seniors hold most Big-5 slots, so a launch-only lever loses its grip, and the top four stay Big-5. The §17n
prediction that slower promotion would damp the snowball inside three years did not hold. The 1960 roster's
promotions already happened, and senior Top 10 conversion is carried by something the lever does not touch. The
candidates are the top-20 weekly hold push (§17b), the recognition stock and radio lift, and senior-act quality.

**Verdict.** F = 0.25 is rejected. F = 0.5 is the first lever to bring 1961 into band, and it widens the field
without crowding it, unlike §17m. But it misses a pre-registered kill (Big-5 top-four count), and its Big-5 cut is
not established beyond 1961. It is **not adopted and not confirmed**. Simulation defaults are unchanged, and the
switch stays in the tree, off by default. A 260-week confirmation at F = 0.5 alone is not recommended until the
senior-slot channel is identified. The next step is analysis only, on the F = 0.5 logs and controls: which input
converts a Big-5 senior entry into Top 10 weeks (weekly hold push, recognition stock and radio lift, or quality),
and whether that channel has a historical reason to be weaker for the majors' senior acts after 1960.

Evidence: `SimLogs/launchsat-050-screen-v1/`, `SimLogs/launchsat-025-screen-v1/` (manifests, console and error
logs, `STOPPED.txt`), `SimLogs/launchsat-off-identity/`, and `SimLogs/launchsat-screen-diagnostics/`
(`yearly_watch.py`, `screen_review.py`, `review-050.txt`, `review-050-025.txt`, `big5-slots-by-career-050.txt`).
The F = 0.25 Big-5 entry-per-launch figure is not usable, because its `release-outcomes.csv` was not flushed.

### 17p. What carries a Big-5 senior entry into the Top 10, and a momentum half-life (2026-10-17, proposal, not built)

This is analysis only, on the F = 0.5 screen and its controls (weekly chart rows) and on the push-0.8
confirmation, the only runs that log every single launch and sample artist heat. No simulation was run, and no code
was changed beyond the §17o switch.

**Conversion after entry.** Hot 100 entrants that debut in 1961-62, five seeds:

| Entrants | Per seed-year (ctl) | P(Top 10), ctl / F = 0.5 | Top 10 weeks per entrant, ctl | Quality | Launch awareness | Launch recognition |
|---|---:|---:|---:|---:|---:|---:|
| Big-5 senior | 45.7 | **28.7 / 31.1%** | 1.86 | **0.700** | 0.757 | 0.361 |
| Big-5 junior | 63.0 | 19.2 / 17.0% | 1.07 | 0.698 | 0.378 | 0.041 |
| Other senior | 68.7 | 21.1 / 20.6% | 1.19 | 0.724 | 0.582 | 0.290 |
| Other junior | 510.7 | 9.4 / 9.9% | 0.49 | 0.746 | 0.226 | 0.020 |

Big-5 senior records are the *lowest*-quality entrants, yet they convert best. The F = 0.5 lever did not touch them.
Their conversion rose, and their launch awareness moved only 0.757 → 0.745. A logit of Top 10 on entry
(quality, launch recognition, log stock, launch awareness, artist heat, Big-5 and senior dummies, genre, year,
seed, arm; pseudo R² 0.45) predicts 31.6% for Big-5 senior entrants:

| Big-5 senior entrants, one input changed | Predicted P(Top 10) |
|---|---:|
| As predicted | 31.6% |
| Big-5 dummy off (label push, launch and weekly) | 20.6% |
| Launch awareness at the other-senior mean (0.75 → 0.58) | 24.6% |
| Launch awareness at the Big-5 junior mean (0.75 → 0.37) | **17.2%** |
| Quality at the other-senior mean | 38.5% (higher: quality is not a carrier) |
| Singles-only artist heat set to 0 | 31.0% |

Recognition enters only through awareness and stock. Its direct coefficient is negative once awareness is held.
Seniority converts through **launch awareness**, and the Big-5 premium on top of that through **label push**.

**What senior launch awareness is made of** (push-0.8 log; terms computed from the authored formula in
`ApplyReleasePromotion`, with seasonality and perceived-quality multipliers taken as 1):

| 1961-64 singles | Awareness | Marketing (`BudgetToImpact` × impact × 0.35) | Recognition × 0.115 | Residual: momentum × 0.5 + reputation × 0.3 + label reputation × 0.1 |
|---|---:|---:|---:|---:|
| Big-5 NewSigning | 0.331 | 0.154 | 0.001 | 0.096 |
| Big-5 Established | 0.580 | 0.201 | 0.023 | **0.275** |
| Big-5 Star | 0.910 | 0.233 | 0.053 | **0.544** |
| Big-5 Superstar | 0.994 | 0.237 | 0.079 | **0.598** |
| Other Established | 0.446 | 0.090 | 0.022 | 0.254 |

The recognition stock, the bounded fame input of the recognition directive, contributes 0.02-0.08. The legacy
artist term (`momentum × 0.5 + reputation × 0.3`) contributes 0.18-0.50 above the newcomer baseline. That makes
it the largest fame input at launch, and it is uncapped. The discrete careerState multiplier in
`GetMarketingBudget` adds another 0.05-0.08. Among Big-5 senior launches, the residual is 0.583 for opening-career
acts, 0.326 for the pre-1960 roster and reservoir, and 0.316 for acts formed in the run. Opening-career and pre-1960
acts make up **80%** of Big-5 senior launches.

**Artist momentum has no time decay.** `SimulatedArtist.momentum` starts at 0 for a generated act. Chart outcomes
alone move it: +0.02 for a Top 40, +0.10 for a Top 10 and +0.18 for a #1 (credited when the peak is reached), and
−0.05 / −0.10 / −0.15 when a run completes at peak 41-60, 61-100 or uncharted. Nothing decays it with time: no
writer outside the chart-run path, the opening seed, the 1960 roster seed and the player band room. A 1960 seed
value (initial roster 0.1-0.5, opening careers `0.10 + 0.22 × recent`, up to 0.9) persists until the act flops, and
at Big-5 entry rates it rarely does. This contradicts the authored intent in `OpeningCareerService`: "Momentum is
what the last two years did; reputation is the whole record. A former star keeps the name and loses the heat." At
runtime the heat is never lost. Momentum also reads beyond awareness. It feeds release priority
(`AILabel.CalculateReleasePriority` + 0.4 × momentum; the artist pick + 0.5 × momentum), which is one route by
which the Big-5 senior share of launches climbs from 19% to 38%. It also feeds re-signing (`ShouldResignArtist`:
Rising with momentum above 0.2), contract pricing and evolution confidence.

A chart-history reconstruction of momentum covers only acts formed in the run, starting at 0
(`momentum_reconstruction.py`). It gives Big-5 senior launches a mean of 0.10-0.17, and capping it changes
predicted entry by only 1-2 points. The reconstruction is collinear with recognition, and its awareness slope
(0.13) is far below the authored 0.5, so it does not identify the term. The seeded 1960 values, which carry the
80% majority, are not observable in any current log.

**The second senior channel is album heat in the weekly radio target.** In the push-0.8 attribution, Big-5 senior
entrants carry a default artist heat of 0.380, of which 0.296 is album-caused (0.262 retained album peak and hit
credit). Other seniors carry 0.254 (0.136 album), and juniors 0.015-0.045. At 0.12 × heat, that is +0.046 to the
radio target, against +0.030. This is §17h's channel. The current-albums diagnostic already tests it, under the
user's constraint that a successful album must still raise single hype. It is not re-proposed here.

**Proposal: an artist momentum half-life.** An off-by-default switch, `--artist-momentum-half-life=W` (weeks). Each
week, every artist's momentum is multiplied by `0.5^(1/W)`. It runs in `RosterManager.OnWeekEndedCore` next to
`ArtistRecognitionService.DecayRegistryForWeek`, idempotent per artist per week, and applies to every artist,
player-signed ones included. Hits still add the same increments, and reputation (the "whole record") is untouched.
Opening-career momentum is already a two-year-window value at seeding, so it is not decayed again at seed time.
Inertness: a run without the switch must be byte-identical in all 80 CSVs. The decay draws no randomness.

*Why it is the right kind of fix:* it is a realism fix to the author's own definition, label-blind, with no
Big-5 targeting. Its expected Big-5 effect is compositional, and time-profiled like the defect. Per launch it bites
Big-5 and other seniors equally: the median gap since the act's last charting single is 35 weeks for Big-5 seniors
and 30 for others, so a 52-week half-life keeps 56% and 57% of the last credit, and 104 weeks keeps 73% and 74%. It
lands on the Big-5 because seniors hold 65% of their 1961-64 slots against 23% for everyone else, and because 80% of
their senior launches are 1960-seeded acts whose seed momentum otherwise never cools. History's majors lost Top 10
share after 1960 as their 1950s star system faded. This lets the sim's seeded star system fade on a clock, not
only on flops.

*Size: not predictable offline.* The seeded momentum that matters most is unlogged. The build therefore adds two
columns, `artistMomentum` and `artistReputation` at launch, to the off-by-default `--head-hit-attribution`
single-launch log, which §17h showed leaves the default CSVs byte-identical. That decomposes the residual term
directly in the screen.

**Screen (five seeds × 156 weeks, paired with the §17o controls; same watch and stop rule):**

| Arm | Switches | Question |
|---|---|---|
| A | `--artist-momentum-half-life=104 --head-hit-attribution` | Does the decay alone move the Big-5 senior arm, at the author's two-year window? |
| B | A + `--launch-push-saturation=0.5` | Do the newcomer and senior levers together move the top-four count and keep 1962 down? |
| C (only if B's 1962 cut is under 3 points) | `--artist-momentum-half-life=52` + F = 0.5 | A stronger senior lever |

**Guards** (on top of §17n's table, which still applies in full):

| Guard | Pass | Kill |
|---|---|---|
| Big-5 firms in the top four, 1961-62 | below the control (3.0 / 3.2) | no change (§17o's failure) |
| Big-5 share, 1962 | down ≥ 3 points, all-seed mean | inside noise again |
| Big-5 senior slots per seed-year | down | unchanged |
| Other-senior Top 10 slots | fall less, in proportion, than Big-5 senior slots | fall as much or more |
| Senior share of all Top 10 slots | reported (control: Big-5 65%, others 23%) | — |
| TraditionalPop share | within seed noise (79% of Big-5 TraditionalPop slots are senior, so it is exposed) | outside it |
| Drops, performance departures, re-signings | within noise (`turnover-is-90-percent-firing`; Rising re-signing reads momentum > 0.2) | drops or departures up more than 20% |
| Genre conversions | within noise (evolution confidence reads momentum) | clearly up |
| Recognition-concentration screen (top 1% / 10% act slot share, #1 acts) | within noise | top 10% up more than 3 points |

**Not in scope:** removing the retained album heat (§17h, user constraint), reshaping reputation, and the
`GetMarketingBudget` careerState multiplier (an economics pass, because it also sets spending).

Evidence: `SimLogs/launchsat-screen-diagnostics/` (`senior_conversion.py` / `.txt`, `senior_channels.py` /
`.txt`, `momentum_reconstruction.py` / `.txt`, `momentum_decay_sizing.txt`).

### 17q. Screen result: momentum half-life, alone and with launch saturation (arms A-C, 2026-10-17)

**Build.** `Systems/ArtistMomentumHalfLife.cs` adds `--artist-momentum-half-life=W`. It is an idempotent weekly
pass in `RosterManager.OnWeekEndedCore` right after the recognition decay, multiplying every artist's momentum by
`0.5^(1/W)`. Reputation is untouched, and it draws no RNG. `--artist-momentum-half-life-check` passes: off is
bit-for-bit, 104 steps halve, a repeat in the same week is a no-op, reputation is unchanged. The
`--head-hit-attribution` launch log gains `artistMomentum`, `artistReputation`, `labelReputation` and
`launchCampaignImpact`. The build has 0 warnings and 0 errors. A 52-week seed-5005 run with the switch off
(`momhl-off-identity`) is **byte-identical** to `headtpl-off-identity` in all 80 CSVs. All 15 screen runs passed
with empty error logs. Each arm is five seeds × 156 weeks against the §17o controls.

| Five-seed mean | Control | F = 0.5 (§17o) | **A**: W = 104 | **B**: W = 104 + F = 0.5 | **C**: W = 52 + F = 0.5 | History |
|---|---:|---:|---:|---:|---:|---:|
| Big-5 share 1960 / 61 / 62 | 40.4 / 33.8 / 29.9 | 34.9 / 28.1 / 28.2 | 35.4 / 31.2 / 29.5 | 32.3 / 29.1 / 29.4 | **31.2 / 28.2 / 24.2** | 34.2 / 20.6 / 21.2 |
| Paired Big-5 Δ (SE) 1960 / 61 / 62 | — | −5.5 / −5.7 / −1.7 (4.7) | −5.0 / −2.6 / −0.4 (3.9) | −8.0 / −4.7 / −0.4 (4.6) | **−9.2 / −5.5 / −5.7** (2.4 / 3.5 / 3.2) | |
| CR4 1960 / 61 / 62 | 37.9 / 37.3 / 30.9 | 35.5 / 29.3 / 28.6 | 36.3 / 31.9 / 31.1 | 34.3 / 30.2 / 29.5 | 32.3 / 27.2 / 25.0 | 34.6 / 27.9 / - |
| CR8 1960 / 61 / 62 | 55.0 / 50.8 / 46.2 | 49.9 / 43.0 / 43.5 | 52.4 / 45.3 / 45.1 | 49.0 / 43.2 / 42.4 | **47.7** / 40.0 / **38.1** | 52.7 / 43.5 / - |
| Big-5 firms in the top four 1960 / 61 / 62 | 3.2 / 3.0 / 3.2 | 3.2 / 3.0 / 3.4 | 3.0 / 3.2 / 3.2 | 2.8 / 3.0 / 3.2 | 2.6 / **3.0 / 3.0** | 1.75 (1961-64) |
| Big-5 senior Top 10 slots 1960 / 61 / 62 | 63 / 95 / 86 | 66 / 87 / 88 | 62 / 86 / 92 | 53 / 90 / 93 | **53 / 65 / 74** | |
| Other-senior Top 10 slots 1960 / 61 / 62 | 53 / 88 / 87 | 55 / 74 / 86 | 69 / 86 / **108** | 64 / 84 / 76 | 70 / 68 / 79 | |
| Senior share of all slots 1962 | 33.4 | 33.5 | **38.5** | 32.5 | 29.6 | |
| Labels with a Top 10 hit 1960 / 61 / 62 | 47.6 / 52.4 / 56.4 | 52.6 / 56.8 / 58.2 | 50.8 / 54.8 / 54.0 | 50.6 / 55.8 / 57.6 | **51.6 / 59.0 / 60.2** | 49 / 51 / - |
| First-time Top 10 labels 1961 / 62 | 27.0 / 26.6 | 31.0 / 25.6 | 29.0 / 25.4 | 32.0 / 27.0 | 34.4 / 28.6 | |
| Non-major firms with 20+ slots 1961 / 62 | 2.0 / 2.0 | 1.6 / 2.6 | 1.4 / 2.0 | 1.2 / 1.0 | 1.0 / 1.4 | 3-5 |
| #1 tenure 1962 (paired Δ, SE) | 2.06 | +0.23 (0.09) | +0.15 (0.18) | **+0.58 (0.24)** | +0.20 (0.26) | |
| TraditionalPop share 1960 / 61 / 62 | 26.2 / 20.4 / 17.8 | 23.5 / 23.0 / 19.0 | 23.5 / 21.9 / 17.0 | 26.7 / 20.9 / 19.3 | 25.2 / 20.6 / 18.2 | |
| Drops / perf. departures / re-signings 1962 | 1164 / 617 / 145 | 1166 / 623 / 156 | 1149 / 630 / 159 | 1157 / 620 / 152 | 1154 / 618 / 144 | |
| Genre conversions 1960 / 61 / 62 | 6.4 / 21.4 / 19.0 | 7.8 / 18.4 / 23.2 | 7.2 / 25.8 / 20.2 | 6.2 / 26.8 / 27.4 | 4.8 / 22.4 / 23.6 | |
| Top 10% acts' share of Hot 100 slot-weeks 1962 | 31.2 | 30.6 | 31.4 | 31.9 | 31.5 | |

**What the telemetry shows.** Big-5 senior momentum at launch in 1961-62 is 0.31 / 0.29 at W = 104 and **0.21 /
0.17** at W = 52, with launch awareness 0.63 / 0.60 against 0.58 / 0.54. Hits keep refilling momentum, so a two-year
half-life trims it by about a third and a one-year half-life by about half. Opening-career acts keep reputation
0.87-0.98, which by design does not decay. Each Big-5 senior launch then reads 0.26-0.29 awareness from reputation
alone. With W = 52, Big-5 senior launches also fall (1961 / 62: 293 / 332, against 336 / 358 at W = 104), because
release priority reads momentum.

**Arm A (W = 104 alone): near-inert on concentration.** The Big-5 cut is −5.0 in 1960 and then within noise. Big-5
senior slots do not fall. Other-senior slots and the senior share of all slots *rise* in 1962 (108 against 87;
38.5% against 33.4%; about 1 SE). A two-year half-life is a realism fix that does not move the label structure.

**Arm B (W = 104 + F = 0.5): not additive.** It fades in 1962 just like F = 0.5 alone. Its #1 tenure in 1962 is
+0.58 weeks (SE 0.24), with 18.8 distinct #1 acts against 23.2. That is the largest #1 move of any arm, and a
concern.

**Arm C (W = 52 + F = 0.5): the first combination that holds its Big-5 cut through 1962.** The cut is −9.2 / −5.5 /
−5.7 with every SE under 3.6. The 1962 Big-5 share of 24.2% is the closest to history (21.2%) of any run so far.
Big-5 senior slots fall at last (−11 / −30 / −12). Other seniors fall proportionally less (1961: −31% Big-5 against
−23% other; 1962: −14% against −9%), which passes the §17p proportionality guard. Labels with a hit and first-time
labels rise every year. TraditionalPop, #1 tenure, drops, departures, re-signings and act concentration are all
within noise. Genre conversions are 22-24 against 19-21, inside the range of the other arms.

**Where C fails:**

1. **Big-5 firms in the top four stay at 3.0** in 1961-62, which §17n pre-registered as a kill. It cannot pass by
   cutting the Big-5 alone. Non-major firms with 20+ slots *fall* to 1.0-1.4 (history 3-5). The freed slots spread
   across the long tail, so no non-major grows big enough to displace a major from the top four. That is §11's
   structural finding again: the second rank needs head-scale non-majors.
2. **CR8 sits at the floor:** 47.7 in 1960 (band 48-58, inside the 2-point margin) and 38.1 in 1962 (band
   38-48). This is the same thin-second-rank cause seen from the other side.

**Verdict.** A is a realism fix with no concentration effect. B adds nothing over F = 0.5 and moves #1 tenure.
**C is the best candidate so far** on Big-5 share, field breadth and chart health, but it misses the top-four guard
and is at the CR8 floor, because the sim has no second rank to receive the freed slots. Nothing is adopted. All three
switches stay off by default, and nothing is committed. The natural next probe is the one §17m called for:
**C together with the §17l head templates**. The lever supplies the Big-5 cut that templates alone could not
produce. The templates supply the 20-40-slot non-majors whose absence keeps the majors in the top four and pins CR8
to its floor. Because the templates have head-envelope impact above the 0.55 knee, the saturation also damps the
template runaway §17m saw (Cameo-Parkway at 73 slots). A 260-week confirmation of C alone would establish 1963-64,
but it cannot clear the top-four kill.

Evidence: `SimLogs/momhl104-screen-v1/`, `SimLogs/momhl104-launchsat050-screen-v1/`,
`SimLogs/momhl052-launchsat050-screen-v1/` and `SimLogs/momhl-off-identity/` (manifests and logs; each run's
`single-launch-attribution.csv` carries the new columns). In `SimLogs/launchsat-screen-diagnostics/`:
`review-armsABC.txt` (all arms, per-seed deltas), `review-momhl104.txt`, `momhl104-telemetry.txt` and
`fade-1962.txt`.

### 17r. Screen result: arm C with the head templates (`momhl052-launchsat050-headtpl-screen-v1`, 2026-10-17)

**Run.** Five seeds × 156 weeks with default flags plus `--artist-momentum-half-life=52 --launch-push-saturation=0.5
--seed-head-templates --head-hit-attribution`, paired against the §17o controls (`top10-fiveyear-review-v1` for
1001-3003, `head-hit-default-controls-v1` for 4004/5005). It was built from commit `d4fb234` with 0 warnings and 0
errors. All five seeds passed with empty error logs, in about 19 minutes. The yearly watch (§17o stop rule) reported
every year as WITHIN band and never stopped the run. Guards are §17l (templates), §17n (launch saturation) and §17p
(momentum half-life). "Templates" means the six template ids (`label_0014`, `0015`, `0020`, `0043`-`0045`), and
the control's figure for them is the three ordinary templates on those ids. "Field" is every label that is neither
Big-5 nor a template.

| Five-seed mean | 1960 ctl | **C + T** | C alone | T alone | 1961 ctl | **C + T** | C alone | T alone | 1962 ctl | **C + T** | C alone | History 1960 / 61 / 62 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| CR4 [band] | 37.9 | **31.6** | 32.3 | 36.6 | 37.3 | **27.1** | 27.2 | 34.6 | 30.9 | **28.1** | 25.0 | 34.6 / 27.9 / - |
| CR8 [band] | 55.0 | **51.1** | 47.7 | 54.3 | 50.8 | **41.7** | 40.0 | 52.0 | 46.2 | **42.2** | 38.1 | 52.7 / 43.5 / - |
| Big-5 share of the Top 10 | 40.4 | **29.1** | 31.2 | 29.1 | 33.8 | **23.2** | 28.2 | 31.3 | 29.9 | **18.2** | 24.2 | 34.2 / 20.6 / 21.2 |
| Big-5 firms in the top four | 3.2 | 2.6 | 2.6 | 2.0 | 3.0 | **2.4** | 3.0 | 2.2 | 3.2 | **1.8** | 3.0 | 1.75 (1961-64) |
| Template share | 1.9 | 24.1 | 2.4 | 26.4 | 1.9 | 18.8 | 2.2 | 21.4 | 2.1 | 18.8 | 1.5 | 23.3 / 20.2 / 24.6 |
| Field share | 57.8 | 46.8 | 66.5 | 44.5 | 64.4 | 58.0 | 69.6 | 47.3 | 68.0 | 63.1 | 74.4 | - |
| Non-major firms with 20+ slots | 2.0 | **4.4** | 2.8 | 3.8 | 2.0 | **3.4** | 1.0 | 3.2 | 2.0 | **3.0** | 1.4 | 3-5 |
| Labels with a Top 10 hit | 47.6 | 46.4 | 51.6 | 45.4 | 52.4 | 55.0 | 59.0 | 49.0 | 56.4 | 59.0 | 60.2 | 49 / 51 / - |
| First-time Top 10 labels | 47.6 | 46.4 | 51.6 | 45.4 | 27.0 | 29.4 | 34.4 | 24.0 | 26.6 | 24.8 | 28.6 | - |
| Big-5 senior Top 10 slots | 63 | 52 | 53 | 50 | 95 | 56 | 65 | 75 | 86 | 58 | 74 | |
| Other-senior Top 10 slots | 53 | 82 | 70 | 81 | 88 | 96 | 68 | 108 | 87 | 102 | 79 | |
| Big-5 Hot 100 entry per single launch, % | 56.1 | 46.5 | 52.4 | 45.1 | 48.0 | 42.1 | 42.9 | 43.1 | 45.7 | 38.4 | 39.6 | |
| TraditionalPop share | 26.2 | **22.9** | 25.2 | 22.6 | 20.4 | 21.4 | 20.6 | 22.2 | 17.8 | 18.4 | 18.2 | |
| #1 tenure (weeks) | 2.30 | 2.19 | 2.07 | 2.34 | 2.33 | 2.06 | 2.20 | 2.33 | 2.06 | 2.32 | 2.26 | |

The "T alone" columns are §17m's stopped run, which covers only 1960-61.

**Paired deltas, C + T against the control** (mean, SE, per seed 1001-5005; `review.txt`, `extra-deltas.txt`):

| Measure | 1960 | 1961 | 1962 |
|---|---|---|---|
| Big-5 share | −11.3 (2.2; −3.8 −11.7 −10.2 −13.3 −17.5) | −10.6 (5.1; **+9.6** −17.7 −12.9 −17.5 −14.4) | **−11.7 (1.9; all five down)** |
| Big-5 firms in the top four | −0.6 (0.4) | −0.6 (0.4) | **−1.4 (0.24; all five down)** |
| CR4 / CR8 | −6.2 / −3.9 | −10.2 / −9.0 | −2.8 / −3.9 |
| Field share | −10.9 (2.6) | −6.4 (4.2) | −5.0 (4.5) |
| Big-5 senior / other-senior slots | −11 / +29 | −38 / +8 | −28 / +15 |
| TraditionalPop share | **−3.3 (0.87; all five down)** | +1.1 (1.5) | +0.6 (1.3) |
| RnB / RockAndRoll share | +1.2 / +2.0 | +3.4 (1.0) / −0.9 | **+3.9 (1.3) / −3.2 (1.2)** |
| Field entry per single launch (pts) | −0.9 (0.3) | −0.8 (0.2) | −0.7 (0.4) |
| #1 tenure / closed chart life | −0.11 / +0.46 (0.21) | −0.27 / −0.03 | +0.26 / +0.15 |
| Re-signings / performance departures | +0 / +1 | **−29 (3.8)** / −21 | +12 / +32 (16) |
| Small closures, Hot 100 units, genre conversions, top-10% act share | within noise | within noise | within noise |

**This is the first configuration to clear every concentration guard in all three years.**

- **Concentration is inside its band every year,** and the CR8 floor problem from C alone is gone (51.1 / 41.7 /
  42.2, against 47.7 / 40.0 / 38.1).
- **The Big-5 cut holds and stays large:** −11.3 / −10.6 / −11.7. In 1962 every seed is down, with SE 1.9.
- **The top-four count moves at last.** It is 2.6 / 2.4 / 1.8. In 1962 it is down in every seed (−1.4, SE 0.24),
  against a target of 1.75. Every earlier arm failed this kill (§17o, §17q).
- **The second rank appears.** Non-majors with 20+ slots are 4.4 / 3.4 / 3.0, inside the 3-5 band. C alone gave
  1.0-2.8.

The two halves do what §17q predicted. The lever supplies the Big-5 cut, which templates alone could not keep (T
alone was −2.5 in 1961). The templates take the freed head slots, and with C the field gives up less to them: field
−6.4 against −17.1 in 1961. The saturation damps the templates' own entry: template launch entry falls from 42.5% /
40.4% with T alone to 36.2% / 38.2%. The field falls by less than the Big-5 every year, but only just in 1960 (−10.9
against −11.3). The senior guards pass: Big-5 senior slots fall, and other-senior slots rise. Templates are spread
out. Every seed-year has some template outside the top 20, and no seed-year has all six in the top 10. Nothing is
absorbed. No Big-5 label is demoted. #1 tenure, chart life, units, Small closures and act concentration stay within
noise.

**Where it fails or needs a decision:**

1. **1960 is over-corrected at the Big-5 and costs TraditionalPop.** The 1960 Big-5 share is 29.1, below §17n's
   30-38 pass range. Per seed it is 26.0 / 27.3 / 29.4 / 31.9 / 30.8, so two seeds are under the kill line of 28.
   TraditionalPop loses 3.3 points in every seed. That is a genre-guard failure, and it comes from the templates:
   T alone gave −3.65, C alone −1.0. In 1960 labels with a hit are 46.4, between the kill line (45) and the pass
   line (48). Two seeds are at 43-44.
2. **1962 overshoots history.** The Big-5 are at 18.2% against 21.2%. Seeds 1001 and 2002 are at 12.9 and 10.6.
   The 1961 cut also has one inverted seed (1001, +9.6), so the 1961 SE is 5.1.
3. **Genre mix shifts after 1960.** RnB gains 3.4 / 3.9 points (about 3 SE), and RockAndRoll loses 3.2 in 1962.
   C alone already showed most of this (RnB +3.0 and the same RockAndRoll figure in 1962), so it belongs to the
   lever, not the templates. It is outside noise and must be read in the confirmation.
4. **Head templates promote to Major in year one.** Liberty is promoted MidTier → Major at week 48 of 1960 in
   **all five seeds**, with 17-25 recent charting records, a roster of 25-30 and owned reach 0.78-0.94. Warner Bros.
   is promoted in four seeds (week 48 of 1960 in two, 1962 in two). Vee-Jay is demoted to Independent in three.
   §17l logs this rather than blocking it, so it is not a kill. It is ahistorical, though: neither label was a
   major. It also makes them Major distributors and puts them in every tier-keyed Major measure. That moves the
   late-decade owner-Major path, and in this screen it moves the `tier_Major` row as well. The cause is the
   promotion gate's volume test (`major-promotion-gate-unperformance-gated`): head-envelope volume clears it on
   launches alone.
5. **One seed still runs away.** In seed 2002, Cameo-Parkway and Liberty hold 116 / 121 / 127 slots. Cameo-Parkway
   has 86 in 1961 (history 31), and Liberty 63 in 1962 (history 8). CR4 is 38.3 / 33.3 / 34.4. The other four seeds
   are damped relative to §17m.
6. **The field's per-launch entry dips** by about 0.8 points (10.1% → 9.2%) in every year. Procedural MidTiers lose
   3.5-6 points of slots to the templates.

**Verdict.** C + T is the best result of the review so far, and the first to clear the top-four kill. It is **not
adopted**. Defaults are unchanged, all three switches stay off, and it is not yet confirmed past 1962. Before a
260-week confirmation, two things need a decision:

- whether head templates may earn Major promotion in their first year (item 4);
- whether to accept the 1960 TraditionalPop / Big-5 floor cost (item 1). That cost is the templates' 1960 entry,
  so a smaller 1960 envelope, or seeding fewer templates at head scale, is the obvious next variant.

The confirmation itself needs the 4004/5005 260-week controls, the 1963-64 template-share fade (history 15.2 →
6.7%), and the genre-mix check in item 3.

Evidence: `SimLogs/momhl052-launchsat050-headtpl-screen-v1/` (manifest, console logs with `HEAD_SCALE_TEMPLATE`
lines, empty error logs) and `SimLogs/momhl052-launchsat050-headtpl-diagnostics/` (`yearly_watch.py`,
`yearly-watch.log`, `paired_review.py`, `review.txt` with all four arms and per-seed template ranks and events,
`extra_deltas.py`, `extra-deltas.txt`). The T-alone tier rows in `review.txt` fall back to the control's label
directory, because that run never flushed its own. Read them only by label id.

### 17s. Templates v2: Liberty at standard MidTier, a year-one promotion hold, and the genre check (2026-10-17)

**What the promotion gate reads (clarifying §17r item 4).** The MidTier → Major gate has a chart term. Since
`14ef670` (2026-07-31) it requires `MajorPromotionChartingRecords = 16` records charting in the last 52 weeks.
That term is a Hot 100 **entry count**, and release volume clears it: Liberty promoted with 17-25. It reads nothing
of head performance (Top 10 slots, chart share) and nothing beyond one year. The gate needs real
chart-performance evidence.

**Changes, inside `--seed-head-templates` only** (the switch stays off by default):

1. **Year-one Major promotion hold, as a stand-in.** `HeadScaleTemplates.HoldsMajorPromotion` keeps the six probe
   templates from promoting MidTier → Major while `monthsActive < 12`. It is read in
   `LabelLifecycleManager.TryPromoteLabel`. **This is a stand-in, not the fix.** The fix is chart-performance
   evidence in the MidTier → Major gate for every label, and that will be necessary. The code comment says to remove
   the hold once the gate is fixed.
2. **Liberty opens as a standard MidTier.** It keeps the TeenHitMachine archetype and its genres, but not the head
   envelope. Its 1960 was 7 Top 10 slots, and its head year (42) was 1961. Seeded at head scale, it reached 53-63
   slots and promoted in every §17r seed. This is §17l's Vee-Jay argument applied to Liberty: a 1961 rise is left
   for the run to earn.

`--seed-head-templates-check` was extended and passes. It checks Liberty's standard envelope; the hold for a template
at 11 months, none at 12 months, none for a procedural label, and none with the switch off. The build has 0 warnings
and 0 errors. A 52-week seed-5005 run with every switch off (`headtpl-v2-off-identity`) is **byte-identical** to
`headtpl-off-identity` in all 80 CSVs.

**Screen V1** (`momhl052-launchsat050-headtpl2-screen-v1`): arm C with templates v2, five seeds × 156 weeks,
against the §17o controls. All seeds passed with empty error logs, and the watch reported every year WITHIN band.

| Five-seed mean | 1960 ctl | **V1** | §17r v1 | 1961 ctl | **V1** | §17r v1 | 1962 ctl | **V1** | §17r v1 | History 1960 / 61 / 62 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| CR4 [band] | 37.9 | 34.0 | 31.6 | 37.3 | 30.5 | 27.1 | 30.9 | 27.7 | 28.1 | 34.6 / 27.9 / - |
| CR8 [band] | 55.0 | 51.1 | 51.1 | 50.8 | 46.4 | 41.7 | 46.2 | 43.3 | 42.2 | 52.7 / 43.5 / - |
| Big-5 share | 40.4 | **30.6** | 29.1 | 33.8 | 25.9 | 23.2 | 29.9 | **21.3** | 18.2 | 34.2 / 20.6 / 21.2 |
| Paired Big-5 Δ (SE) | | −9.8 (2.7) | −11.3 (2.2) | | −7.9 (2.7) | −10.6 (5.1) | | **−8.6 (0.9)** | −11.7 (1.9) | |
| Big-5 firms in the top four | 3.2 | 2.6 | 2.6 | 3.0 | 2.8 | 2.4 | 3.2 | **2.0** | 1.8 | 1.75 |
| Template share | 1.9 | 18.2 | 24.1 | 1.9 | 15.6 | 18.8 | 2.1 | 16.9 | 18.8 | 23.3 / 20.2 / 24.6 |
| Field share | 57.8 | 51.3 | 46.8 | 64.4 | 58.5 | 58.0 | 68.0 | 61.8 | 63.1 | - |
| Non-major firms with 20+ slots | 2.0 | 3.8 | 4.4 | 2.0 | 3.2 | 3.4 | 2.0 | 3.6 | 3.0 | 3-5 |
| Labels with a Top 10 hit | 47.6 | 47.6 | 46.4 | 52.4 | 52.4 | 55.0 | 56.4 | 57.0 | 59.0 | 49 / 51 / - |
| TraditionalPop share of the Top 10 | 26.2 | 22.4 | 22.9 | 20.4 | 21.4 | 21.4 | 17.8 | 18.4 | 18.4 | |
| #1 tenure (weeks) | 2.30 | 2.09 | 2.19 | 2.33 | 2.12 | 2.06 | 2.06 | **2.39** | 2.32 | 2.55 (1960-64) |

Per seed, V1's Big-5 share is 29.8 / 22.5 / 27.5 / 35.8 / 37.3 in 1960 and 15.6 / 14.6 / 20.8 / 28.1 / 27.3 in 1962.
The 1962 top-four count is 2 in every seed.

**What v2 fixed:**

- **The 1960 over-correction.** The Big-5 are back inside §17n's 30-38 range. Templates fall from 24.1 to 18.2. The
  field gives up 6.5 points, against 9.8 for the Big-5. Labels with a hit equal the control.
- **The 1962 overshoot.** The Big-5 share is 21.3 against history's 21.2, with the tightest paired SE of any arm
  (0.9, all five seeds down).
- **The field guard passes every year with margin:** field −6.5 / −5.9 / −6.2 against Big-5 −9.8 / −7.9 / −8.6.
  The second rank holds at 3.2-3.8 non-majors with 20+ slots.

**What it gave back:** the 1961 top-four count is 2.8 (−0.2, SE 0.37). That is inside noise against the control's
3.0. In 1961 history needs a strong Liberty, and v2 deliberately leaves Liberty to earn it. In V1 it does so in
only one seed (26 slots in 4004).

**The hold only delays promotion, as expected for a stand-in.** Warner Bros. promotes MidTier → Major in 1961 in
three seeds, at weeks 61-74, with 16-17 charting records: the first or second review after the hold expires, at
the bar exactly. Liberty no longer promotes, and in seed 1001 it demotes to Independent in 1962. Vee-Jay demotes in
three seeds. Nothing is absorbed, and no Big-5 label is demoted.

**Genre check, against the year-end benchmark** (`genre_check.py`; `GenreCatalog.ChartSlotBenchmark`, which counts
year-end Hot 100 slots). Summed |sim − benchmark| over the 12 benchmarked 1960-62 genres:

| | 1960 | 1961 | 1962 |
|---|---:|---:|---:|
| Control | 23.6 | 31.6 | 33.0 |
| C alone (§17q) | 26.8 | 30.6 | 40.8 |
| §17r C + T v1 | 29.0 | 35.8 | 43.8 |
| **V1** | **24.8** | 36.0 | **35.4** |

- *TraditionalPop in 1960.* V1's Top 10 share is still −3.8 against the control (SE 1.6). Year-end TraditionalPop
  is 20.4 against a benchmark of 22, and the control's 23.0 sits on the other side. So this is a guard miss against
  the control, not against history. In the Top 10, the Big-5 give up 26 TraditionalPop slots, the templates take 14
  and the field loses 8. The templates are TeenPop-heavy (about 50 slots in 1960), and they take most of those from
  the field.
- *RnB / RockAndRoll.* §17r's 1962 shift (RockAndRoll 7.0 year-end slots against a benchmark of 12; RnB 31.4
  against 28) **does not replicate** in V1 under the same lever: 9.4 and 29.0, against a control at 9.6 and 28.6.
  §17r attributed the shift to the lever. That was wrong. It belongs to those runs' streams, or to head-scale
  Liberty's RockAndRoll genre, not to C.
- *Still worse than the control:* Soul in 1961-62 (3.8 / 4.2 year-end slots against 5.2 and a benchmark of 9 / 7).
  The templates squeeze the field, which holds most Soul.

**Open items:**

1. **#1 tenure in 1962 is +0.33 weeks (SE 0.12), with 3.8 fewer distinct #1 acts (SE 1.9).** That is outside noise
   against the control, which is the guard as pre-registered. It is toward history's 1960-64 tenure of 2.55
   (§17h), but the 1962 control figure (2.06) is the reference, so it is reported as a miss.
2. **One template a seed can still run away.** ABC-Paramount reaches 61-66 slots (seed 3003 1961, seed 5005 1962),
   and Cameo-Parkway 76 in seed 2002's 1960. Seed 5005's 1962 CR8 is 57.9, above band. History's largest
   non-major year was Cameo-Parkway's 54 (1962). Which firm runs away changes from seed to seed, so this is
   spread, not a scripted winner. But the tail is too long.
3. **The real promotion fix** (above).

**Decision on screen V2 (a softer lever): not run.** It was proposed to answer §17r's 1962 overshoot and its
RockAndRoll loss. V1 lands 1962 at history and does not reproduce the RockAndRoll loss. A softer lever would move
1961 (25.9 against 20.6) further from history.

**Verdict.** V1 is the new front-runner: C (`--artist-momentum-half-life=52 --launch-push-saturation=0.5`) with
`--seed-head-templates` v2. It is better than §17r on the 1960 floor, the 1962 level and genre fit, and slightly
weaker on the 1961 top-four count. Nothing is adopted, and defaults are unchanged. The 260-week five-seed
confirmation (`momhl052-launchsat050-headtpl2-confirm-v1`) and the 4004/5005 260-week default controls follow.
They establish 1963-64, including the template fade (history 15.2 → 6.7%).

Evidence: `SimLogs/momhl052-launchsat050-headtpl2-screen-v1/`, `SimLogs/headtpl-v2-off-identity/`, and in
`SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `review-v1.txt`, `extra-deltas-v1.txt`, `genre_check.py`,
`genre-check.txt` (control / C / §17r / T alone; T alone's 1962 column is a partial year and is not usable),
`genre-check-v1.txt` and `yearly-watch-v1.log`.

### 17t. Confirmation: V1 over 1960-64 (`momhl052-launchsat050-headtpl2-confirm-v1`, 2026-10-17)

**Runs.** V1's flags, five seeds × 260 weeks, all passed with empty error logs. The new 4004/5005 default controls
(`top10-default-controls-260-v1`, 260 weeks) also passed. Over weeks 1-156 they reproduce
`head-hit-default-controls-v1` row for row in `records.csv`, so every control is now a full five-seed 260-week set.
The confirmation's 1960-62 equals V1's to the decimal: same seeds and flags, and the runs are deterministic. Its
new information is 1963-64. The yearly watch reported 1963 MARGINAL and 1964 WITHIN, and never stopped the run.

| Five-seed mean | 1962 ctl | V1 | 1963 ctl | **V1** | 1964 ctl | **V1** | History 1962 / 63 / 64 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Big-5 share | 29.9 | 21.3 | 29.2 | **28.9** | 27.9 | **24.7** | 21.2 / 26.0 / 18.7 |
| Paired Big-5 Δ (SE; per seed) | | −8.6 (0.9) | | **−0.3** (3.3; −6.3 −5.8 −4.8 **+6.9 +8.5**) | | −3.1 (2.8; −7.9 +2.5 +3.1 −11.3 −1.9) | |
| CR4 [22-32] | 30.9 | 27.7 | 28.5 | **32.5** | 30.0 | 28.7 | |
| CR8 [38-48] | 46.2 | 43.3 | 44.0 | **49.1** | 44.1 | 46.0 | |
| Big-5 firms in the top four | 3.2 | 2.0 | 3.0 | 2.4 | 2.6 | 2.4 | 1.75 |
| Template share | 2.1 | 16.9 | 3.0 | **17.2** | 2.2 | **14.1** | 24.6 / 15.2 / 6.7 |
| Field share (paired Δ) | 68.0 | 61.8 (−6.2) | 67.8 | **53.9 (−13.9, SE 3.1)** | 70.0 | 61.2 (−8.8, SE 4.9) | |
| Labels with a Top 10 hit | 56.4 | 57.0 | 57.6 | **49.2** (−8.4, SE 2.9) | 56.6 | 54.4 | |
| First-time Top 10 labels | 26.6 | 25.8 | 24.0 | 19.0 (−5.0, SE 3.8) | 22.4 | 19.6 | |
| Non-major firms with 20+ slots | 2.0 | 3.6 | 2.2 | 3.6 | 1.8 | 3.8 | 3-5 |
| Big-5 Hot 100 entry per single launch, % | 45.7 | 41.1 | 48.8 | 39.7 | 48.4 | 41.0 | |
| #1 tenure (paired Δ) | 2.06 | +0.33 (0.12) | 2.38 | +0.13 (0.12) | 2.61 | +0.09 (0.13) | |
| Year-end genre error, 14 genres (control) | | 35.4 (33.0) | | 45.0 (41.4) | | 37.8 (37.6) | |

**1963-64 fail the pre-registered guards:**

- **The Big-5 cut fails in 1963.** The paired change is −0.3 against §17n's ≥3, and 1964 is −3.1 with SE 2.8.
  Seeds 1001-3003 keep a cut of about −5.6 in 1963, but 4004 and 5005 go up (+6.9, +8.5).
- **CR4 and CR8 are out of band in 1963 and worse than the control** (32.5 / 49.1 against 28.5 / 44.0). That is
  §17l's kill condition. It is inside the watch's 2-point stop margin.
- **The field guard fails in 1963 and 1964:** the field loses 13.9 and 8.8 points, while the Big-5 lose 0.3 and
  3.1. Labels with a hit fall 8.4 in 1963 (49.2, still above the 48 pass line). First-time labels fall 5.0.
- **Templates persist.** They hold 17.2 / 14.1% against history's 15.2 / 6.7. §17l made this a finding, not a kill.
  Their slots are about half senior (41.6 of 89.4 a seed-year in 1963). Most of those acts reached senior status
  during the run: only 16 of the 42 were already senior in 1960.
- **More templates promote once the hold has expired:** Warner Bros. in seed 3003 and MGM in seed 4004 (both 1964),
  on top of Warner Bros.' 1961 promotions in three seeds. The stand-in delays promotion; it does not prevent it.

What still holds in 1963-64: the top-four count stays below the control (2.4 / 2.4 against 3.0 / 2.6), the second
rank holds at 3.6-3.8, Big-5 entry per launch is still 7-9 points lower, no Big-5 label is demoted, nothing is
absorbed, and #1 tenure, chart life, units, Small closures and genre fit are within noise. The 1962 #1-tenure miss
(+0.33) does not persist.

**Where the 1963 slots went** (Top 10 slots per seed-year; `senior-origin-1963-64.txt`):

| 1963, ctl → V1 | Slots | Senior-act slots | Of which senior since 1960 |
|---|---:|---:|---:|
| Big-5 | 151.8 → 150.2 | 93.8 → 94.4 | 37.0 → 30.8 |
| Templates | 15.8 → 89.4 | 5.2 → 41.6 | 1.2 → 16.2 |
| Field | 352.4 → 280.4 | 91.6 → 61.8 | 34.8 → 16.0 |

In 1963 the Big-5 hold exactly the control's senior slots, although their 1962 senior slots were −29. The templates'
gain comes out of the field, junior and senior alike.

**Reading.** V1 works for as long as the excess sits in the Big-5's 1950s inheritance: the seeded stars the
half-life cools, and the newcomer edge the saturation trims. By 1963 the excess is in **senior acts grown during
the run, at any head-scale label**. That is the Big-5's own replacement stars and the templates' new ones alike. The
momentum half-life leaves these alone, because hits keep refilling momentum, and nothing in the career ladder makes
a successful act fall back (§17n: Established → Declining needs three flops in a row). So the templates do not fade
the way history's did. Part of history's 1963-64 fade was external: *Bandstand* left Philadelphia, MGM went quiet,
the British Invasion arrived. The sim models none of these, so some persistence is expected. Template persistence,
plus the 1963 Big-5 rebound in seeds 4004 and 5005, is the same mechanism seen in two places: the senior snowball
does not depend on the label, and head-scale access is what feeds it.

**Verdict.** V1 is **not confirmed for 1960-64**. It remains the best 1960-62 configuration of the review: inside
band, at history on the 1962 Big-5 share, the top-four count moving, and a second rank. Its 1963-64 failure points
to the shared career ladder, not to anything Big-5-specific. Defaults are unchanged. All switches stay off by
default, and the year-one hold stays a stand-in for the MidTier → Major gate fix.

**Next (proposal, not built):** analysis only, on these logs. Which rule keeps a run-grown senior act charting at
the head? Candidates: the career ladder's fall-back rule, the `GetMarketingBudget` careerState multiplier (§17n
flagged it as counting seniority twice, an economics pass), and the recognition stock. Is there a label-blind,
historically argued decay of senior status, for example career age or time since the last Top 10? That would let
1963-64 heads turn over without targeting any label.

Evidence: `SimLogs/momhl052-launchsat050-headtpl2-confirm-v1/`, `SimLogs/top10-default-controls-260-v1/`, and in
`SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `yearly_watch_260.py`, `yearly-watch-confirm.log`,
`paired_review_260.py`, `review-confirm.txt`, `extra_deltas_260.py`, `extra-deltas-confirm.txt`,
`genre_check_260.py` (benchmark through 1964, BritishBeat and BritishPop added), `genre-check-confirm.txt`,
`senior_origin.py` and `senior-origin-1963-64.txt`.

### 17u. What keeps senior acts at the head in 1963-64, and a senior cooling clock (2026-10-17, analysis only, proposal not built)

This is analysis only, on the §17t confirmation (V1, five seeds × 260 weeks, which carries the single-launch log) and
its five-seed default controls. It adds a label-blind historical comparison, using the `billboard.com` weekly Top 10
files from §13 for 1960-64. No simulation was run and no code was changed.

**History measures turnover by act, so the sim is measured the same way.** Take each act with a Top 10 record before
year Y (the window starts in 1960, so both sides are left-censored the same way). Does it debut another Top 10
record in Y? Split by how many Top 10 records it already has (n) and by the years since its last one (k). The table
pools 1962-64. Sim figures are acts per seed:

| Act's cell at the start of Y | History acts | History P(return) | Control | V1 |
|---|---:|---:|---:|---:|
| n = 1, k = 1 | 156 | 13.5% | 11.1% | 10.9% |
| n = 1, k = 2 | 127 | 3.9% | 5.1% | 5.0% |
| n = 1, k ≥ 3 | 120 | 2.5% | 2.9% | 2.2% |
| **n ≥ 2, k = 1 (a hot repeater)** | 96 | **38.5%** | **37.5%** | **38.7%** |
| **n ≥ 2, k = 2 (one fallow year)** | 43 | **9.3%** | **22.9%** | **15.4%** |
| **n ≥ 2, k ≥ 3** | 31 | **0.0%** | **7.1%** | **4.6%** |

The rows hold without 1964, before the British Invasion: in 1962-63 history gives 46.8% / 12.0% / 0 of 9.

- **A hot senior act returns at history's rate.** Neither the control nor V1 over-rewards an act that had a Top 10
  last year. This rules out a decay keyed on career age. It would cut the one cell that is already right, and
  history has about twice the sim's count of third-or-later Top 10 records (120 against 61-67 in 1961-64).
- **The excess is the cold return.** Once a multi-hit act misses a year, history's return rate drops by a factor of
  4, and after two missed years nobody returns. In the sim it drops by a factor of 1.6 (control) or 2.5 (V1; the
  momentum half-life already helped), and acts still come back after two years. One-hit acts are on history at every
  k.
- **The repo's own files show the pattern** (Top 10 records per year, 1960-64). Connie Francis 5/4/4/0/0, the Everly
  Brothers 4/2/2/0/0, Neil Sedaka 1/2/3/0/0, Paul Anka 3/1/0/0/0, Brenda Lee 4/4/3/1/0, Chubby Checker 1/4/4/1/0,
  Elvis Presley 3/6/4/3/0. Streaks end abruptly and do not resume inside the window. This is the author's own rule
  in `OpeningCareerService`: "A former star keeps the name and loses the heat."

**Top 10 slots a year, 1962-64, by the act's cell** (sim: five-seed mean; Big-5 + template share in brackets):

| Cell | History | Control | V1 |
|---|---:|---:|---:|
| New (no earlier Top 10) | 344.7 | 326.3 (20%) | 326.4 (29%) |
| n = 1, k = 1 | 59.7 | 57.3 (39%) | 63.7 (44%) |
| n ≥ 2, k = 1 | 98.7 | 86.1 (50%) | 89.8 (70%) |
| **Cold: any n, k ≥ 2** | **17.0** | **50.2 (61-65%)** | **40.1 (63-100%)** |

Cold returns hold 3.3% of history's Top 10, against 9.7% in the control and 7.7% in V1. That is an excess of about
23 slots a year in V1, and 63-70% of it sits at the Big-5 and the templates, against their 38-46% share of all
slots. The rule that would fix it is label-blind, and its incidence lands at the head.

**Which rule carries a cold senior.** These are V1 1963-64 single launches by acts at Established or above with no
in-run Top 10 entry in the last 52 weeks: 128 a seed-year, 9.4% reach the Top 10, and they hold 71 slots, 64% of
them at Big-5 or template labels. The counterfactuals come from a logit of Top 10 on the launch (awareness, quality,
log stock, campaign impact, recognition radio lift, holder group, genre, year, seed; 1961-64; pseudo R² 0.69). The
fit stopped short of full convergence on sparse genre cells, so the predictions are used only as relative
counterfactuals:

| Cold senior launches, one input changed | P(Top 10) |
|---|---:|
| As predicted | 9.2% |
| `GetMarketingBudget` multiplier one rung down (Established 1.5 → Declining 0.5, Star → 1.5, Superstar → 2.0) | 8.8% |
| Recognition → 0 (awareness lift, stock bonus, radio lift) | 7.4% |
| Reputation → NewSigning mean | 8.6% |
| Momentum → 0 | 8.8% |
| All four together | **6.3%** |
| Quality → NewSigning mean (0.662 → 0.604) | 0.3% |
| Observed: Rising / NewSigning launches | 5.2% / 0.8% |

For every 1963-64 run-grown senior launch the ranking is the same: careerState budget multiplier at 1.0 −0.3
points, recognition −2.2, reputation −0.4, momentum −0.7, all four 11.8 → 8.4%. For 1960-seeded seniors, recognition
is −4.7 of 21.2.

1. **`GetMarketingBudget` is the smallest carrier.** Its careerState part adds 0.023 to a run-grown senior's launch
   awareness, and neutralizing it moves P(Top 10) by 0.3 points. §17n's "counts seniority twice" is real but small.
   It stays an economics cleanup, not the lever.
2. **Recognition is the largest measured input.** The stock has a 277-week half-life, and the launch consumes it
   through awareness (up to 0.115), stock (up to 1.3x) and radio (up to 0.075) with no reference to how recently the
   act had a hit.
3. **The ladder carries status, not conversion.** With recognition on, careerState no longer feeds launch awareness
   or stock directly. Its reads are the budget multiplier, release priority, the release cooldown, drop protection
   and re-signing. It is sticky. Of 281 acts that were senior at their first 1963 launch and launched in 1964, 264
   (94%) were still senior at their last 1964 launch. The fall-back counts only flops peaking below 60: a 41-60
   peak is neutral and one Top 40 resets the count. 41 senior launches a seed-year (22%) follow three or more
   straight Top 40 misses. They convert at 6.1% and hold 16 slots. Release volume is a weak channel: launches per
   launching act-year are 1.83 for run-grown seniors, 1.73 for Rising acts and 1.45 for new signings.
4. **Most of a cold senior's edge is the song.** Cold seniors launch at Rising quality (0.662), well above new
   signings (0.604). The fame inputs together are about a third of their conversion (9.2 → 6.3%). History's cold
   stars did not lose talent. Their audience and their sound dated, and the sim has no channel for that. The
   proposal below does not try to supply one. It removes the fame premium that outlives the heat, which is the part
   the author's rule says should go.

**Correction to §17t's reading.** The templates' 1963 slots, split by time since the act's previous Top 10, are:
new 48.0, ≤ 52 weeks 30.4, 53-104 weeks 10.8, > 104 weeks 0.2. Their senior slots are hot repeaters, and hot
repeaters return at history's rate. Template persistence is head-scale access producing new hit acts and hot
streaks, not stale seniority. A senior decay reaches about 11 of their 89 slots. Their 1964 fade in history was
external, as §17t noted. The Big-5's 1963 cold slots are 33.2 of 150 (control 41.6). Those a cooling clock would
reach.

**A second shape mismatch, recorded but not acted on: repeat hits run longer in the sim and shorter in history.**
Top 10 weeks per record, 1961-64:

| | 1st Top 10 record | 2nd | 3rd+ |
|---|---:|---:|---:|
| History | 5.10 | 4.26 | 4.41 |
| Control | 5.46 | 5.73 | 6.12 |
| V1 | 5.44 | 5.85 | 6.63 |

The inversion holds in every holder group, field included. In an OLS of Top 10 weeks on V1 launches, the 3rd+
premium is +1.16 weeks raw and +0.52 (SE 0.24) after launch inputs. The fame block (awareness, recognition,
momentum, carried single heat) accounts for about +0.9 weeks and the label's campaign impact for +0.5, against
quality's −0.5. History's stars had more hits, and shorter ones. In 1960-62 Connie Francis put 4-5 records a year
into the Top 10, against the sim's 1.8 single launches per senior act-year. That is a release-cadence question for
hot acts, and the cooling below does not touch it.

**Proposal: a senior cooling clock** (an off-by-default switch, `--senior-cooling`; label-blind; applies to every
act, player-signed ones included).

- *Clock.* `weeksSinceLastTop40`, reset when any record by the act enters the Top 40, the ladder's own hit
  definition (`RegisterTop40Hit` already resets flops). Top 40 rather than Top 10 keeps an act that is still
  charting a steady #15-#30 hot. On V1 that clock marks 86 cold senior launches a seed-year (47% of senior
  launches, 48.5 slots, 69% at the Big-5 or templates). The Top 10 clock would mark 128 (69%). Opening careers seed
  the clock from their keyed pre-1960 history. Other 1960 seniors start at 0, so they begin cooling only after a
  hitless 1960.
- *Launch reads cool, the stock does not.* Multiply reputation × 0.3 and the three recognition launch reads
  (awareness lift, stock bonus, radio lift; singles and the album stock path) by
  `cool(k) = 1` for k ≤ G, and `F + (1 − F) · 0.5^((k − G)/H)` after it, with **G = 52, H = 26, F = 0.35**. The
  grace G protects the hot-repeater cell that already matches history. The floor F is "keeps the name". The
  recognition stock, cultural standing and every UI read are untouched. Momentum is excluded because V1's half-life
  already decays it, and cooling it too would count it twice.
- *The ladder cools in step.* A weekly, idempotent pass next to `ArtistMomentumHalfLife`: a senior act whose clock
  passes G steps down one rung, and steps again every further H weeks (Superstar → Star → Established → Declining).
  The existing comeback is unchanged (Declining → Established on one Top 40). A hit resets the clock. It does not
  restore rungs, which are re-earned under the usual rules. So the budget multiplier, release priority, drop review
  and contract leverage all read a status that matches the act's heat. The pass draws no RNG.
- *Inertness.* With the switch off, all 80 CSVs must be byte-identical. A check suite covers the clock reset, the
  cool(k) values at G, G + H and the floor, one rung per H, the comeback path, and stock and UI reads unchanged.

*Size: rough only.* The fame block's full removal cuts cold conversion by about 31%. At this cool(k), the cold
launches keep roughly half of it, so conversion drops by about 14%, or about 7 slots. Ladder cooling then halves
an Established act's release priority (0.7 → 0.25 base), and the label's freed launches go mostly to juniors who
convert at 0.8-5%. That could take another 10-15 slots. **Expected effect: 15-25 Top 10 slots a seed-year leave
cold seniors**, against the roughly 23-slot excess the history comparison measures. About half the Big-5's share of
cold slots is reachable, so **the expected Big-5 effect is −1.5 to −2.5 points in 1963-64**. On its own, the clock
will probably not clear §17n's ≥ 3-point Big-5 guard. Its job is to put the cold-return cells on history. It is not
a Big-5 lever.

**Screen (five seeds × 260 weeks; the effect starts in 1961 and matters in 1963-64):**

| Arm | Switches | Question |
|---|---|---|
| A | V1 + `--senior-cooling` with the ladder pass disabled | The launch-read half alone |
| B | V1 + `--senior-cooling` | Both halves: do the cold-return cells reach history? |
| D | defaults + `--senior-cooling` | Paired against the default controls, because a realism fix must stand on defaults too |

**Guards** (on top of §17n's table, which applies to arms B and D in full):

| Guard | Pass | Kill |
|---|---|---|
| n ≥ 2, k = 1 return rate (hot repeaters) | within 4 points of the control (history 38.5%) | down more than 5 points |
| n ≥ 2, k = 2 return rate | ≤ 12% (history 9.3%, 12.0% before 1964) | unchanged from V1 (15.4%) |
| n ≥ 2, k ≥ 3 return rate | ≤ 2% | above V1 |
| Cold-return slots a year | toward history's 17 from V1's 40 | unchanged |
| n = 1 return rates | within noise (already on history) | any cell off by more than 3 points |
| Top 10 weeks per record by nth hit | reported | — |
| Drops, performance departures, re-signings | within +20% (Declining with two flops is dropped half the time) | up more than 20% |
| TraditionalPop Top 10 share | within seed noise (its slots are senior-heavy) | outside it |
| Recognition-concentration screen | within noise | top 10% act slot share up more than 3 points |

**Not in scope:** an appeal decay on the song (the 2/3 of a cold senior's edge that is quality; it would also reach
one-hit acts, which already match history), the hot-act release cadence above, the retained album heat (§17h, user
constraint), and the `GetMarketingBudget` economics pass.

Evidence, all in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `senior_hazard.py` / `senior-hazard.txt`
(return rates and cell slots, with history), `senior_gap_slots.py` / `senior-gap-slots.txt` (slots by gap and by
holder group), `senior_turnover.py` / `senior-turnover.txt` (act-level repeat rates and concentration),
`senior_retention.py` / `senior-retention.txt` (launch classes, awareness decomposition, ladder stickiness,
logit counterfactuals), `senior_clock40.py` / `senior-clock40.txt` (Top 40 against Top 10 clock), and
`senior_duration.py` / `senior-duration.txt` (weeks per record by nth hit).

### 17v. Screen result: the senior cooling clock (arms A, B, D; rejected, 2026-10-18)

**Build.** `Systems/SeniorCooling.cs` adds `--senior-cooling` (both halves) and `--senior-cooling-launch-only` (arm
A), as §17u specified: G = 52, H = 26, F = 0.35. The clock is `SimulatedArtist.seniorCoolingLastTop40Week`, written
in `RegisterTop40Hit` only while the switch is on. Opening careers seed it from their last pre-1960 Top 40, placed
mid-year; every other act starts it at the first read. The launch-side cooling sits in
`ArtistRecognitionService.LaunchRecognition`, which is read by single and album stock, single radio and the
recognition launch audit, and in the reputation and recognition terms of `GetNewReleaseAwarenessBonus`. The ladder
pass runs in `RosterManager.OnWeekEndedCore` after the momentum half-life. `--senior-cooling-check` passes: off is
bit-for-bit, the cool(k) values at G, G + H and the floor, the clock seed and reset, ladder steps at G + 1 and
then every H, the comeback path, a fresh grace year after a hit, and no ladder pass in launch-only mode. The build
has 0 warnings and 0 errors. A 52-week seed-5005 run with the switch off (`senior-cooling-off-identity`) is
**byte-identical** to `headtpl-v2-off-identity` in all 80 CSVs. A mid-run smoke check (seed 1001, 1961-62) confirmed
that the reads cool: cold senior launches read recognition 0.207 → 0.159 and awareness 0.442 → 0.399, while hot
seniors and juniors were unchanged.

**Runs.** Five seeds × 260 weeks per arm: A = V1 + launch-only, B = V1 + both halves, D = defaults + both halves.
All 15 runs passed with empty error logs. The yearly watch (`yearly_watch_cooling.py`, A and B against V1, D
against the control) never stopped a run. It reported A WITHIN every year, B MARGINAL in 1962 (CR8 37.8, below
band) and D MARGINAL in 1961-64.

**The mechanism guards fail in every arm** (1962-64 pooled; the §17u table):

| | History | Control | V1 | A | B | D |
|---|---:|---:|---:|---:|---:|---:|
| n ≥ 2, k = 1 return rate (hot) | 38.5 | 37.5 | 38.7 | 38.1 | 35.4 | 38.7 |
| n ≥ 2, k = 2 return rate (pass ≤ 12) | 9.3 | 22.9 | 15.4 | 14.4 | **12.0** | 18.4 |
| n ≥ 2, k ≥ 3 return rate (kill if above the base) | 0.0 | 7.1 | 4.6 | **11.4** | **5.8** | **13.0** |
| Cold-return slots a year (toward 17) | 17.0 | 50.3 | 40.1 | 44.5 | 39.1 | 48.5 |
| Hot-repeater slots (n ≥ 2, k = 1) | 98.7 | 86.1 | 89.8 | 84.6 | **72.6** | 95.4 |

Cold-return slots do not move in any arm. Arm B reaches the k = 2 pass line, but at the cost of 17 hot-repeater slots
a year, the one cell that already matched history. The k ≥ 3 cell is small (about 20 acts per seed) and noisy, but
it points the wrong way in all three arms. Per the pre-registered table, that is a kill.

**Why cold returns did not fall.** Split each cold-return Top 10 record by the act's clock at launch (slots a year):

| Clock at launch | V1 | A | B | D |
|---|---:|---:|---:|---:|
| Warm: a Top 40 within 52 weeks, so never cooled | 18.6 | 22.9 | 22.3 | 23.6 |
| 53-104 weeks | 14.3 | 14.1 | 12.2 | 15.9 |
| > 104 weeks | 7.1 | 7.5 | 4.7 | 9.0 |

- **About half of the cold returns are out of the clock's reach.** These acts had no Top 10 for two years but kept
  charting #11-#40, so the Top 40 reset kept them hot. §17u sized the cold set on a Top 10 clock and specified a
  Top 40 reset. The two describe different populations.
- **Where the clock did run, the reduction is 0-4 slots,** within noise. §17u estimated −7 from the launch half
  and −10 to −15 from the ladder half. The ladder half mostly relabels: Declining cold-return slots rise 2.2 → 8.1
  (B) and 10.9 (D).

**Head concentration (paired; mean with SE, per seed):**

| | 1962 | 1963 | 1964 |
|---|---|---|---|
| A − V1, Big-5 share | +1.7 (2.0) | **−5.8 (3.3)**; +0.2 +1.7 −3.5 −12.7 −14.6 | −3.9 (2.1) |
| A − V1, CR8 | −2.8 (3.1) | **−6.9 (2.4)**, all five down | −3.1 (3.5) |
| B − V1, Big-5 share | −2.5 (3.2) | −2.4 (1.7) | −0.8 (2.8) |
| B − V1, field share | +5.0 (5.2) | **+3.9 (1.1)**, all five up | +2.1 (3.6) |
| B − V1, labels with a Top 10 hit | +4.0 (2.8) | **+5.8 (1.2)**, all five up | +3.6 (3.4) |
| D − control, Big-5 share | +2.5 (4.0) | +2.4 (2.9) | **+4.7 (2.0)**; +5.2 +9.2 +3.3 −2.1 +7.9 |
| D − control, field share | −2.7 (4.0) | −1.9 (2.3) | **−4.7 (1.9)** |

- **A's 1963 cut is two seeds.** 4004 and 5005 carry it. Those are the two seeds that rebounded in V1 (§17t).
  B, which differs from A only by adding the ladder half, lands the same years at −2.4 and −0.8. Two
  configurations this close should not differ by this much, so at five seeds the Big-5 mean is dominated by the
  stream, not the lever.
- **B's one robust gain is the field:** 1963 field +3.9 and labels with a hit +5.8, every seed. It came with lower
  hot-repeater slots, not lower cold returns, and it does not meet §17n's ≥ 3-point Big-5 guard.
- **On defaults the cooling helps the Big-5.** D's Big-5 senior slots rise +9.0 and +18.4 in 1963-64, while
  other labels' seniors fall in every year from 1961 (−8 to −19). The 1964 Big-5 share is +4.7, four of five seeds.
  A label-blind cut to an artist's own fame is **not label-neutral**. Fame is the part of launch awareness that a
  small label's act carries itself. Label push, the other large input, is untouched. So the cut takes more from
  field seniors in relative terms, and the fixed 520 slots go to push. In V1 the launch saturation trims push and
  the templates compete for the freed slots, which is why A and B do not show this.

**Other guards** (A and B against V1, D against the control). Drops and performance departures are within +5%
(the guard is +20%). #1 tenure, closed chart life, chart units, the top-10%-acts share and genre conversions are
within noise. D's top-10% share is +1.2 in 1964 (SE 0.2, all five seeds), inside the 3-point line. Soul is
−1.2 to −2.7 in D in 1960-62 (SE 0.5-0.7), a further step from a benchmark it already misses. **Year-end genre
error worsens in 1964** in every arm: A 42.4, B 43.8, D 44.2, against control 37.6 and V1 37.8. Weeks per record
by nth hit barely move (3rd+ 6.46-6.51, against 6.63 in V1); §17u expected no change there.

**Verdict: rejected.** The clock does not reach the cell it was built for. It fails the k ≥ 3 kill in every arm and
leaves cold-return slots unchanged. Its ladder half costs hot repeaters. Its head effects are seed-driven in A,
small in B and harmful on defaults. Defaults are unchanged and the switch stays off. Nothing is adopted. V1
remains the best 1960-62 configuration and is still not confirmed for 1963-64.

**What this changes for the next pass.**

1. **The cold-return excess cannot be reached through fame reads.** In §17u the fame block was a third of a cold
   senior's edge, and quality was two-thirds. Cutting the third, even before the Top 40 reset leaked half the
   cells, could not reach a 23-slot excess. The remaining routes are on the song side, which §17u ruled out of
   scope, or on why a cold act gets a release at all.
2. **Any artist-side, label-blind cut must be paired with a push-side check on defaults.** D shows that the slot pool
   re-weights toward label push. This generalizes [[one-conversion-reshuffles-the-rng-stream]]: a five-seed
   Big-5 mean moves ±5 points from stream effects alone (A against B in 1963).
3. **The clock definition matters as much as the decay.** If a cooling probe is ever re-run, the clock and the
   measured cells must use the same hit definition (Top 10 throughout), and the guard on hot repeaters must be on
   slots as well as return rate.

Evidence: `SimLogs/senior-cooling-off-identity/`, `SimLogs/seniorcool-{a,b,d}-v1/`, and in
`SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `identity_compare.py` / `identity-senior-cooling-off.txt`,
`cooling_smoke.py`, `yearly_watch_cooling.py` / `yearly-watch-cooling.log`, `cooling_review.py` /
`cooling-review.txt` (and the earlier `cooling-review-a.txt`, `cooling-review-ab.txt`), `cold_slot_split.py` /
`cold-slot-split.txt`, `senior_origin_ab.py` / `senior-origin-ab.txt`, and `genre-check-cooling.txt`.

### 17w. Why cold hits keep returning: height, not frequency, and hit acts that release too little (2026-10-18, analysis only)

Analysis only, on the V1 confirmation and the five-seed default controls. No simulation was run and no code was
changed. **New data:** the billboard.com pages that `research_top10_fetch.py` cached for §13 hold the whole Hot 100,
although the fetch kept only rows 1-10. `hist_hot100_extract.py` recovers all 260 weekly charts for 1960-64, 100
positions each, to `SimLogs/historical-hot100-1960-64/hot100_weekly.csv`. Positions 11-100 are now available for
history.

**1. The cold-return excess is mostly height, not return frequency.** Decomposing 1962-64 cold-return slots (the
act's last Top 10 debut two or more calendar years back) into pool × return rate × records per returner × slots
per record:

| Per year | Pool (acts) | P(return) | Cold-return records | Slots per record | Slots |
|---|---:|---:|---:|---:|---:|
| History | 107.0 | 3.7% | 4.7 | **3.64** | 17.0 |
| Control | 128.1 | 5.9% | 8.9 | 5.59 | 50.3 |
| V1 | 130.3 | 4.6% | 7.3 | **5.46** | 40.1 |

At history's slots per record, V1 would hold 26.7 cold slots. So about **13 of the 23-slot excess is how long each
cold return stays**. The other 10 come from 1.5x too many cold-return records, split about evenly between a larger
one-hit pool (113 acts against 82, at history's n = 1 rate) and the doubled n ≥ 2 return rate that §17u targeted.
At best, the cooling clock addressed about a fifth of the excess.

**Stay length is set by peak, and cold returns peak too high.** Per peak band, Top 10 weeks match history in every
cell (a #1 runs about 8 weeks, #4-6 about 4.4-5.3, #7-10 about 2-3), as §17a found for all records. History's cold
returns are weak hits: mean peak 5.9, no #1, 36% stall at #8-10, and they peak about one week after entry. V1's
peak at 3.9, 55% reach the top three, and 15% stall at #8-10. History's known acts peak **lower** than its new acts
(new 4.8, hot 5.3, cold 5.9). In V1, hot acts peak **higher** (new 4.1, hot 3.0, 68% top three; cold 3.9). Among
Top 10 records, launch inputs explain little of the peak (R² 0.06; quality the largest per-SD term). Radio heat at
the peak explains much more (R² 0.29). Cold returns carry the highest launch campaign impact of any cell (0.63
against 0.51 for new acts), and 67% of them are at Big-5 or template labels. The §17a steep top (airplay's fifth
power) turns each of them into a climber.

**2. Correction to §17v: the Top 40 "leak" is historical.** Of history's 14 cold Top 10 returns in 1962-64, 10 came
from acts with a Top 40 entry in the previous 52 weeks. In both series, faded acts that return are still charting.
§17v's lesson that a cooling clock and the cells it is measured on must share one hit definition still holds, but
the leak itself is not a sim defect.

**3. In history, a faded act's chart records rarely reach the top; in the sim there is no such ceiling.** Records first
entering the Top 40 in 1962-64, by time since the act's last Top 10:

| Cell at Top 40 entry | History: entries a year / P(Top 10) | V1: entries a year / P(Top 10) | Control |
|---|---:|---:|---:|
| New (no earlier Top 10) | 125.3 / **43.9%** | 159.2 / **33.6%** | 153.2 / 34.9% |
| Hot (last Top 10 ≤ 52 weeks back) | **120.7** / **30.7%** | **31.0** / **59.1%** | 31.3 / 58.4% |
| Warm (53-104 weeks) | 17.3 / 30.8% | 14.0 / 45.2% | 15.9 / 49.6% |
| Cold (> 104 weeks) | 5.3 / 37.5% (16 records) | 6.7 / 41.6% | 8.1 / 43.0% |
| All Top 40 entries a year | 293 | about 211 | about 209 |

Faded acts put about the same number of records into the Top 40 in both series (about 21-23 a year). In history,
those records convert below a new act's rate. In the sim they convert above it: at Big-5 labels the rate is 52-55%,
while field acts' cold records convert at 30%, on history. The ordering is inverted for hot acts too.

**4. The larger mismatch: hit acts release too little.** For acts with a Top 10 debut in year Y − 1, their records in
Y (1962-64 pooled; sim per seed):

| | Hot act-years | Top 40 records per act-year | Top 10 records per act-year | P(Top 10 \| Top 40) | Single launches per act-year |
|---|---:|---:|---:|---:|---:|
| History | 252 | **1.17** | **0.36** | **31%** | — |
| Control | 243 | 0.43 | 0.25 | 58% | — |
| V1 | 238 | **0.44** | **0.24** | **55%** | 1.23 |

History's hit acts put 2.7x as many records into the Top 40 and still had 1.5x as many Top 10s. Each record was a
smaller event: one of three or four sides a year, splitting the act's fans, radio and the label's push. The sim's hit
act releases about one single a year, and each release carries the act's whole fame and the label's whole push. That
explains three findings at once: §17u's longer repeat hits (2nd 5.85 and 3rd+ 6.63 Top 10 weeks against history's
4.26 / 4.41), V1's tall hot-act peaks, and cold returns that hit as hard as new breakouts. It also explains part of
§17a's turnover gap: 211 Top 40 entries a year against 293.

**Reading.** Senior status is not over-held at the act level. Hot repeaters return at history's rate (§17u), and
hot acts produce fewer Top 10 records than history's, not more. The fame and push behind an act are spent on too few
records, so each one runs too tall and too long. This applies whether the act is hot or cold. A fame decay (§17u/v)
cannot fix that. It reduces the stake on each record, not the number of records the stake is spread over.

**Next (proposal, not built): hit-act cadence with a same-act split.** Diagnostic first, on these logs:
(a) what caps a hot act's single launches at 1.23 a year (`UpdateArtistCooldowns`, release priority, label release
capacity, the album cycle); (b) whether two records by one act on the chart at once compete for anything (fans,
radio adds, push) or simply stack; (c) history targets from the recovered Hot 100: Top 40 entries per hot act-year
(1.17), P(Top 10 | Top 40) by cell, and how often an act has two records in the Top 40 at once. A probe would pair a
shorter cooldown for acts with a recent Top 40 with a same-act split of awareness and push across that act's
concurrent records, so that more releases buy more, smaller hits rather than more big ones. Risk: hot acts sit at
head labels, so more of their records could raise concentration unless the split holds. The §17n guards and the
§17u cell table both apply.

**Arms A/B/D stay rejected.** One robust signal is left unexplained: A's 1963 CR8 fell in all five seeds (−6.9, SE
2.4), and B's field rose in all five (+3.9, SE 1.1). Neither came through cold returns, B's cost was 17 hot-repeater
slots a year (a cell already short of history's output), and D reverses the sign on defaults. Without a mechanism,
these are not grounds to adopt.

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `cold_return_decomp.py` /
`cold-return-decomp.txt`, `cold_return_shape.py` / `cold-return-shape.txt`, `cold_return_height.py` /
`cold-return-height.txt`, `cold_top40_ceiling.py` / `cold-top40-ceiling.txt`, `hist_hot100_extract.py`,
`hist_top40_ceiling.py` / `hist-top40-ceiling.txt`, `hot_act_cadence.py` / `hot-act-cadence.txt`.

### 17x. Hit-act cadence diagnostic: what caps a hit act's releases, and the history targets (2026-10-18, analysis only)

Analysis only, on the V1 confirmation (five seeds, 1962-64) and the recovered 1960-64 Hot 100 (§17w). No simulation
was run and no code was changed. A "hit act-year" is an act with a Top 10 debut in Y − 1, observed in Y.

**History's targets.** History lists artist credits as printed, so a duo credited two ways counts as two acts. That
lowers history's per-act figures slightly.

| 1962-64 | History | V1 |
|---|---:|---:|
| Hot 100 debuts per hit act-year | **2.19** | 0.75 |
| Top 40 / Top 10 records per hit act-year | 1.09 / 0.33 | 0.43 / 0.24 |
| Median weeks between a hit act's consecutive Hot 100 debuts (p25-p75) | **13** (9-17) | 27 (17-44) |
| Hit acts' charting weeks with 2+ of their records on the Hot 100 | **9.4%** | 1.5% |
| All acts' charting weeks with 2+ records on the Hot 100 / in the Top 40 | 6.7% / 4.0% | 0.7% / 0.3% |
| Top 40 records whose act's next record debuts while they are still in the Top 40 | 13.6%, at week 7 of the Top 40 run | 3.6%, at week 11 |
| Remaining Top 40 weeks after that follow-up, against all records of the same age | 3.07 against 4.05 (−24%) | 1.47 against 1.84 (−20%) |
| Acts on the Hot 100 in a year | **386** | **645** |
| Hot 100 records / weeks per charting act-year | **2.03 / 13.9** | 1.24 / 8.3 |
| Acts charting in Y − 1 that chart again in Y | **43.9%** | 35.5% |

History's hit acts released a single about every quarter, timed so that it arrived as the previous one peaked. The
sim spreads the Hot 100 across 1.7x as many acts, each charting once and briefly. That matches the user's
observation that few artists stay on the chart. The follow-up effect cannot be read as competition alone. The sim
shows −20% with no same-act coupling in its code (below), so part of history's −24% is selection: follow-ups go out
as a record fades.

**What caps a hit act at 1.23 single launches a year.** In V1, every release roll launches one single: an
`OrphanSingle`, or an `AlbumWithPromo` that launches its promo single. Three limits stack:

1. **The release cooldown does not bind.** `GetMinimumReleaseCooldown` is 8-12 weeks, which would allow 4-6 releases
   a year. A hit act's median gap is 22 weeks (p25 17, p75 32), and only 12% of gaps fall within two weeks of the
   cooldown.
2. **The label's release lottery is nearly flat, and it binds.** `CalculateWeeklyReleaseChance` gives each label one
   roll a week at most (`releasesPerMonth / 4`). `GetArtistForRelease` hands the roll to one eligible roster act,
   weighted by `CalculateReleasePriority`: career priority (Superstar 1.0 … NewSigning 0.4), momentum × 0.4, a
   cooldown bonus up to 0.2, genre heat and 0-0.15 noise. A Major rolled 40 times a year (median 43) across 28-33
   acts it released. A hit act at a Major took 4.0% of its label's rolls, against 3.3% for an even split. At a
   MidTier the figures were 6.5% against 5.3%, and at an Independent 15.9% against 13.9%. A Top 10 act buys about
   1.2x an average act's share, and Major hit acts got 1.7 releases a year.
3. **Albums take more than half of a hit act's rolls.** Per hit act-year: 0.68 `AlbumWithPromo` against 0.54
   `OrphanSingle`. History's acts issued albums alongside a quarterly singles schedule. In the sim, an album uses the
   act's roll and resets its cooldown, so the album cycle replaces singles.

**Hit acts are lost.** 31% of hit act-years have no release roll in Y. The first event after the hit is a contract
expiry without re-sign (9.9% of hit act-years), a performance departure (8.8%), a move to another label (4.3%),
disbandment (2.9%), or none: still rostered, never picked (4.6%). Counting every drop or departure in the year after
the hit, about 83 of 238 hit acts a seed (35%) leave their label:

- **Contract expiry, 30 a seed.** 93% are Rising with no flops in the contract. `ShouldResignArtist` re-signs a
  Rising act only while `momentum > 0.2`. Otherwise it falls to `GD.Randf() < artistLoyalty` (0.28-0.78). Momentum
  fades between releases, and faster under V1's half-life, so a hit act whose option comes up 29 weeks (median)
  after its hit is renewed on a coin flip.
- **Performance departure, 51 a seed.** All are Declining at the event, a median 51 weeks after the hit. The ladder
  falls back on three straight flops peaking below 60, and `ShouldDropArtist` drops a Declining act with two flops
  half the time at each monthly review. History's hit acts stayed with their labels through years of flops; the
  sim's lose their label within about a year.

**Concurrent records by one act do not interact.** Outside the observe-only media layer (§ media atmosphere),
nothing in chart, demand, radio or push reads an act's other records. `ChartManager.GetArtistChartingRecords` has no
callers. The only modeled cannibalization is between an album and its own promo single (`release-strategy`
substitution terms). Two singles by one act on the chart stack at full strength. So raising cadence without a split
would multiply big hits, not spread them.

**Reading.** The sim's hit acts are under-released and under-retained, and both are label-side rules, not demand.
A Major has the release capacity (40 rolls a year) to give each of its hit acts a quarterly single. It spends that
capacity on an almost even lottery across 30 acts. History's labels did the opposite: the hit act came first, and
the rest of the roster got what was left. This supports §17w's reading and widens it. The sim's chart has too many
acts, each with one or two big records. History's had fewer acts, each with a steady run of medium ones.

**Next (proposal, not built): a hit-act cadence probe, off by default, label-blind.**

- *Follow-up lane.* An act with a Top 40 entry in the last 26 weeks becomes due for its next single 13 weeks after
  its previous release, matching history's median. When it is due, it takes the label's next roll ahead of the
  lottery. The label's `releasesPerMonth` is unchanged, so the extra hit-act singles come out of juniors' rolls.
  That is the history shape (fewer acts, more records each), and it adds no volume. An album project for a due act
  does not displace the follow-up single.
- *Same-act split.* While two or more of an act's singles are on the Hot 100, the launch awareness, push and
  recognition radio lift of the newer one are divided with the older, sized so that the predecessor loses about a
  week of Top 40 life. Without the split, the lane multiplies big hits.
- *Retention (separate arm).* `ShouldResignArtist` renews any act with a Top 40 under the current contract. The
  Declining drop does not count flops released within 52 weeks of a Top 10.
- *Targets:* 2.19 Hot 100 debuts and 1.09 Top 40 per hit act-year, a 13-week debut gap, 9.4% concurrent weeks,
  386 acts a year on the Hot 100, 44% year-to-year persistence, P(Top 10 | Top 40) for hot acts toward 31%, and
  §17w's cold-return slots toward 17.
- *Guards:* §17n's concentration table in full (the lane favors acts at head labels unless the split holds), #1
  tenure, Top 10 weeks per record by nth hit (§17u), label release counts unchanged, drops and re-signings, genre
  error, and the off-switch byte-identity check.
- *Screen:* five seeds × 260 weeks. Arm L is V1 + lane + split. Arm R is V1 + retention. Arm D is defaults + lane +
  split, paired against the controls (§17v: an artist-side change must also stand on defaults).

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `hot_act_release_gaps.py` /
`hot-act-release-gaps.txt`, `hot_act_formats.py` / `hot-act-formats.txt`, `hot_act_silence.py` /
`hot-act-silence.txt`, `hot_act_losses.py` / `hot-act-losses.txt`, `hist_cadence_targets.py` /
`hist-cadence-targets.txt`.

### 17y. Build: the hit-act cadence probe (`--hit-act-cadence`, `--hit-act-retention`; 2026-10-18, screen not run)

**Build.** `Systems/HitActCadence.cs` adds two switches. Both are off unless named and label-blind.

- **`--hit-act-cadence`, the follow-up lane with the same-act split.** This is the change to the one-roll-a-week
  lottery. When a label's weekly release roll fires, `AILabel.GetArtistForRelease` first looks for a *due* act:
  release-eligible, a Top 40 entry in the last 52 weeks, and 13 or more weeks since its last release. If one is
  due, it takes the roll (the longest wait wins, ties go to the lower artist id, no RNG drawn). Otherwise the
  lottery runs as before. A lane release is a single: if `DecideRelease` chose an album, `TryReleaseRecord`
  overrides it to an `OrphanSingle`, and the album waits for a later roll. The roll rate itself
  (`releasesPerMonth`, at most one roll a week) is unchanged. So the lane reallocates capacity from the rest of the
  roster to hit acts and adds no volume. §17x showed total Hot 100 entries are already near history's; the
  allocation is what's wrong.
  In `ApplyReleasePromotion`, a single launched while the same act has other singles on the Hot 100 gets
  `share = 1 / (1 + charting predecessors)` of its airplay reads: label push radio and the recognition radio lift.
  Each charting predecessor's radio heat is cut by 10% (`PredecessorRadioCut`). Launch awareness and stock stay
  whole: fans still buy a known act's new single, while programmers ration its airplay. In history, fame bought
  entry, not height (§17w). The 10% cut is a first guess, still to be sized against history's follow-up effect
  (§17x: −24%, part of it selection). The split is market-side and applies to every single, the player's
  included. The lane never touches the player's releases.
- **`--hit-act-retention`.** `ShouldResignArtist` renews any act with a Top 40 under the current contract, still
  subject to `CanAffordToSign`. No act is let go within 52 weeks of its last Top 40, whether by `ShouldDropArtist`
  or by the career ladder's own fall to `Dropped` in `UpdateCareerState`, which `RecordChartRunComplete` turns into
  a performance departure. The window is the lane's own hot window. Both rules are AI-only.
- **Clock.** `SimulatedArtist.hitCadenceLastTop40Week` is written in `RegisterTop40Hit`, only while a switch is
  on. Opening careers seed it from their last pre-1960 Top 40, placed mid-year. Other acts count as never hot until
  their first in-run Top 40.
- **Telemetry.** No CSV schema changes, so the off run can be byte-compared. While a switch is on, the console
  prints a cumulative `HIT_ACT_CADENCE` line each year: lane picks, lane album overrides, split launches, retention
  renewals, protected reviews. Lane releases appear in `release-strategy.csv` as `OrphanSingle` rows.

**Checks.** `--hit-act-cadence-check` (`SimTools/HitActCadenceChecks.cs`) passes. Off: no clock writes, share 1, no
predecessors, no forced single, no renewal or protection, no radio cut, and the ladder's drop untouched. On: clock
seeding from public history (a 1958 Top 40 counts, a 1959 #55 does not); the due edges (52 weeks hot inclusive, 53
not; 12 weeks since release not due, 13 due); the pick order (longest wait, then id, fall-through when none is due);
the forced single only for a lane pick that chose an album; share = 1, 1/2, 1/3; the predecessor filter (the act's
other charting singles only, not its album or other acts); the radio cut; renewal and protection edges (52 weeks
protected, 53 not); and the ladder hold (a Declining act with three flops stays put inside the year and still drops
outside it). Each switch alone leaves the other off. `--senior-cooling-check` still passes. The Debug build has 0
warnings and 0 errors.

**Inertness.** A 52-week seed-5005 run with both switches off (`hit-cadence-off-identity4`) is **byte-identical**
to `senior-cooling-off-identity` in all 80 CSVs, with an empty error log. The identity run was repeated after each
code change below (`-identity` to `-identity4`), and every time all 80 CSVs were identical.

**Smoke runs (one seed, not a screen).** V1's flags plus both switches, seed 1001, 104 weeks, compared on 1961
against V1's own seed 1001 (deterministic over the shared weeks) and history. Four iterations, each fixing what the
previous one showed:

- *v1:* a 26-week hot window, a split of fame and push, retention on a Top 10. Hit acts launched more (1.36 →
  1.80), but follow-ups launched while the act was charting made the Hot 100 49% of the time against 78%. The split
  was cutting entry, not height. Performance departures rose 420 → 503, because the career ladder's own fall to
  `Dropped` (Rising → Declining on two flops, then Dropped on three) bypassed `ShouldDropArtist`. More releases made
  hit acts flop out faster.
- *v2:* the split moved to radio only, and retention also holds the ladder's drop.
- *v3:* the hot window widened to 52 weeks. Only 37 of 85 hit acts had reached three launches, because one
  follow-up that missed the Top 40 ended the chain.
- *v4 (the build as it stands):* retention keyed on a Top 40 within 52 weeks, the lane's own population. v3's extra
  departures were Declining acts with a Top 40 but no Top 10 (150 → 270).

| 1961, seed 1001 | History | V1 | v4 |
|---|---:|---:|---:|
| Single launches per hit act (Top 10 in 1960) | — | 1.36 | **2.49** |
| Median weeks between a hit act's launches | — | 19 | **13** |
| Hit acts with no launch / 3+ launches | — | 18 / 7 of 80 | 11 / 45 of 79 |
| Hot 100 / Top 40 / Top 10 records per hit act | 2.58 / 1.34 / 0.42 | 0.70 / 0.31 / 0.16 | **1.15 / 0.61 / 0.34** |
| Median weeks between a hit act's Hot 100 debuts | 12 | 22 | 15 |
| Hit acts' weeks with 2+ records on the Hot 100 | 14.0% | 1.8% | 3.9% |
| Acts on the Hot 100 | 395 | 647 | **588** |
| Hot 100 records per charting act | 1.98 | 1.21 | 1.32 |
| Hot 100 debuts / release rolls (volume) | 690 / — | 692 / 5,401 | 687 / 5,489 |
| *Guard:* Top 10 records / Top 10 weeks per record | 105 / 4.52 | 98 / 4.86 | **89 / 5.36** |
| *Guard:* Big-5 Top 10 share | — | 17.3% | **26.9%** |
| *Guard:* performance departures / contract-expiry drops | — | 420 / 886 | 514 / 788 |

**Reading.** The lane works as designed. A hit act now gets a quarterly single, almost none go silent, and the
Hot 100 spreads over fewer acts with more records each. Release volume is unchanged (+1.6% rolls, Hot 100 debuts
flat). It closes about a third of the per-hit-act gap to history (Hot 100 debuts 0.70 → 1.15 against 2.58). The
rest is entry: a hit act's single reaches the Hot 100 only about 41% of the time, with or without the lane. At a
quarterly cadence, history's 2.19-2.58 debuts imply roughly 55-75%.

Three guard signals on this one seed point the wrong way, and they are the risk §17x predicted. The Big-5 Top 10
share rose 9.6 points: hit acts sit at head labels, and their Top 10 conversion per launch held at 11-13%. The Top
10 turned over less (89 records, 5.36 weeks each). Performance departures stayed +22%, above the +20% line; the
remaining extras are Declining acts past the hot window. One seed in one year cannot separate these from stream
noise ([[single-seed-decade-ab-noise-floor]]: ±5 Big-5 points on a stream reshuffle). They are what the screen must
decide.

**Screen (not launched; awaiting review):** five seeds × 260 weeks. Arm L = V1 + `--hit-act-cadence`. Arm LR = V1
+ both switches. Arm D = defaults + both, paired against the five-seed controls. Targets and guards as in §17x, with
the §17n Big-5 and Top 10 turnover guards read first. Open sizing questions: the predecessor cut (10%), and whether
entry for a known act (the ~41% Hot 100 rate) is a separate lever.

Evidence: `SimLogs/hit-cadence-off-identity/` (four identity runs), `SimLogs/hit-cadence-smoke/` (v1-v4), and in
`SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `identity-hit-cadence-off.txt`, `cadence_smoke.py` /
`cadence-smoke.txt` / `cadence-smoke3.txt` / `cadence-smoke4.txt`, `cadence_smoke_releases.py` /
`cadence-smoke-releases.txt`.

### 17z. Why the lane raised the Big-5 Top 10 share (2026-10-18, analysis only)

Analysis only, on the v4 smoke and V1's five seeds. No simulation was run and no code was changed. A launch is *hot*
when its act had an in-run Top 40 in the 52 weeks before it, which is the lane's own population.

**Seed 1001, 1961.** Of the +50 Big-5 Top 10 slot-weeks (90 → 140 of 520), +55 come from hot Big-5 acts. Cold Big-5
acts lost 5. Other labels' hot acts gained 17, and other labels' cold acts lost 67.

| 1961, seed 1001 | Launches V1 → v4 | Per act | Top 10 / launch | Top 10 slot-weeks |
|---|---:|---:|---:|---:|
| Big-5, hot | 47 → **104** | 1.38 → 2.67 | 10.6 → 14.4% | 36 → **91** |
| Other, hot | 286 → 495 | 1.59 → 2.29 | 6.6 → 4.9% | 130 → 147 |
| Big-5, cold | 202 → 138 | | 5.0 → 5.1% | 54 → 49 |
| Other, cold | 4,866 → 4,752 | | 1.05 → 0.67% | 300 → 233 |

The lane is label-blind. It gave Big-5 hot acts +93% launches per act and other hot acts +44%. The Big-5 hot acts
were fewer (33 of the 196 acts with a 1960 Top 40, against 45 of 198 in V1), but they got more of the lane. A Major
rolls about 40 times a year across a handful of hot acts, so every due act is served on time. A smaller label's
hot act waits for a roll the label rarely makes. Each extra Big-5 hot launch then carries §17n's per-launch edge.

**Five seeds, projected.** Per seed-year in V1, 1961-64, Top 10 slot-weeks per launch were measured for the four
groups. The lane was applied as seed 1001's launch multipliers (Big-5 hot ×2.21, other hot ×1.73, cold launches
cut to keep total rolls), with slot-weeks renormalized to 520. This shows only the allocation effect: yields are held
fixed, with no crowding and no stream noise.

| Five-seed mean | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|
| V1 Big-5 share | 25.9 | 21.3 | 28.9 | 24.7 |
| Projected with the lane | 31.1 | 25.3 | 34.1 | 28.8 |
| Δ (SE) | **+5.2** (1.7) | **+4.0** (1.9) | **+5.2** (1.3) | **+4.1** (0.6) |

19 of 20 seed-years rise. Pooled, a hot Big-5 launch yields **1.41** Top 10 slot-weeks against **0.55** for a hot
launch elsewhere (2.6x). On seed 1001, allocation accounts for +3.1 of the observed +9.6. The rest is Big-5 hot
conversion rising from 10.6 to 14.4% (15 Top 10 records against about 11 expected, inside Poisson noise on that count)
and longer Top 10 stays.

**Reading.** The jump is mostly mechanism, and about a third of it is seed 1001's draw. The lane does what history
did: hit acts get a quarterly single. History's Big-5 held 20.6 / 21.2 / 26.0 / 18.7% of the Top 10 in 1961-64 with
that cadence. The sim's hot Big-5 launch converts at 2.6x everyone else's. That is §17n's edge, and §17o/§17p found
it is carried by senior acts. The lane multiplies whatever a hot launch is worth, so it exposes that edge. The
same-act split cannot offset it: it binds only while two singles chart together (3.9% of hit-act weeks).

The §17y screen as specified would very likely fail the §17n Big-5 guard by 4-5 points in every year. Arm D, on
defaults (Big-5 near 30%), would start further out. The question to settle before the screen: what makes a hot
Big-5 launch worth 2.6x a hot launch elsewhere at the same hotness? The candidates are launch stock and awareness,
campaign impact, the recognition radio lift, and the weekly hold push. Each is in `single-launch-attribution.csv`
and `records.csv`, and each can be tested on V1's logs. If one channel carries most of the gap and has a historical
reason to be weaker, it pairs with the lane in the screen. If none does, the lane cannot ship alone.

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `cadence_big5_decomp.py` /
`cadence-big5-decomp.txt`, `cadence_big5_projection.py` / `cadence-big5-projection.txt`.

### 17aa. What makes a hot Big-5 launch worth 2.6x: a post-entry premium history does not have (2026-10-18, analysis only)

Analysis only, on V1's five seeds (hot launches 1961-64, as in §17z), the v4 smoke, and the 1960-64 Hot 100 and
labelled Top 10 files. No simulation was run and no code was changed.

**The funnel.** Pooled hot launches, 1961-64:

| Hot launches | Per seed-year | Hot 100 entry | Top 10 / entry | Top 10 weeks / Top 10 record | Yield (Top 10 weeks / launch) |
|---|---:|---:|---:|---:|---:|
| Big-5 | 45.9 | 69.9% | 28.9% | 6.59 | **1.33** |
| Procedural Major | 17.4 | 51.0% | 32.2% | 6.19 | 1.02 |
| MidTier | 101.5 | 53.0% | 24.3% | 6.18 | 0.80 |
| Independent | 116.0 | 30.6% | 14.5% | 5.65 | 0.25 |
| All non-Big-5 | 250.2 | 40.5% | 21.2% | 6.04 | 0.52 |

The 2.56x splits into entry 1.72 × conversion after entry 1.36 × stay 1.09. Per seed it runs from 1.44 (1001) to
3.67 (4004). It is a tier gradient, not a Big-5 effect. Procedural Majors come within 1.3x, and most of the
comparison pool is Independents, whose hot acts yield a fifth as much.

**Which inputs carry it.** Three stage models on hot launches: an entry logit, a Top 10 | entry logit and a stay
OLS, with year and seed fixed (pseudo R² 0.70 / 0.50, R² 0.42). Each row moves one input group of the Big-5 rows to
the comparison pool's mean, in linear-predictor terms. The rows are not additive.

| Big-5 hot launches, one group moved | Gap closed: to MidTier | to all non-Big-5 |
|---|---:|---:|
| Label push (campaign impact, payola) | **73%** | **74%** |
| Record (quality 0.684 against 0.673 / 0.652, hook) | 34% | 45% |
| Artist fame (momentum, reputation, recognition) | 18% | 17% |
| Big-5 / Major dummies (residual label effect) | 13% | 9% |
| Label reputation | 13% | 12% |
| Career state | 9% | 8% |
| Opening career | 6% | 6% |
| Format | 1% | 1% |
| Genre | **−131%** | **−74%** |
| All groups | 57% | 95% |

Campaign impact is the largest term in all three stages (z 12.9 entry, 8.1 conversion, 5.3 stay). Genre runs the
other way: 33% of Big-5 hot launches are TraditionalPop and 14% Country, which convert worse, against 25% TeenPop and
21% RnB elsewhere. The Big-5 edge holds despite its genre mix.

**History has the per-act edge, but not this one.** History labels only its Top 10 records, so a per-launch yield
cannot be measured there. A per-hit-act yield can: a hit act has a record first reaching the Top 10 in Y − 1, and
carries that record's operating label. Its Hot 100 debuts come from the weekly Hot 100 by normalized artist name.
Pooled 1961-64:

| Per hit act in Y | History Big-5 | History other | **Ratio** | V1 Big-5 | V1 other | **Ratio** |
|---|---:|---:|---:|---:|---:|---:|
| Hit acts per year | 16.3 | 62.8 | | 19.3 | 61.0 | |
| Top 10 slot-weeks | 3.43 | 1.89 | **1.82** | 2.74 | 1.65 | **1.66** |
| Hot 100 debuts | 3.28 | 2.18 | **1.51** | 0.92 | 0.72 | 1.29 |
| Top 10 per Hot 100 debut | 15.96% | 15.93% | **1.00** | 37.4% | 27.0% | **1.38** |
| Top 10 weeks per Top 10 record (debuts 1961-63) | 4.97 | 4.83 | 1.03 | 6.13 | 5.77 | 1.06 |

History's Big-5 hit acts out-charted the rest by 1.8x, all of it through cadence. They put 1.5x as many records on
the Hot 100, and each converted at exactly the field's rate. On history's counts (34 of 213 against 87 of 546), the
conversion ratio is 1.00 ± 0.18, so V1's 1.38 is about two SE above it. V1 reaches history's per-act edge (1.66
against 1.82) only by offsetting two errors. Big-5 hit acts are the most under-released (the flat lottery across
30-act rosters, §17x), and each of their entries carries a post-entry premium that history lacks.

The lane removes the first error and exposes the second. On seed 1001 in 1961, the v4 smoke gives Big-5 hit acts
2.12 Hot 100 debuts each (history 2.83, V1 0.58), but other hit acts only 0.90 (history 2.76, V1 0.75).

**Projection.** Applied to §17z's five-seed projection, removing the premium (Big-5 hot yield ÷ 1.38) cancels the
lane's Big-5 rise:

| Five-seed mean, Big-5 share | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|
| V1 | 25.9 | 21.3 | 28.9 | 24.7 |
| Lane | 31.0 | 25.3 | 34.0 | 28.8 |
| Lane, no post-entry premium | **26.9** | **21.8** | **29.5** | **24.9** |
| V1, no post-entry premium | 23.2 | 19.1 | 25.9 | 22.4 |
| History | 20.6 | 21.2 | 26.0 | 18.7 |

**The carrier after entry is radio heat.** For hot Hot 100 entrants, awareness reaches its 0.60 ceiling by week 3
in every tier, so it carries nothing after entry. Radio heat differs from the entry week and stays apart. Big-5
entrants run 0.47-0.55, procedural Majors 0.46-0.54, MidTier 0.42-0.50 and Independents 0.35-0.42 over chart weeks
0-8. At matched quality (0.66-0.74) the gap is the same: 0.47 against 0.35 at entry. Independents enter on *more*
units (3.5k against 2.9k) and less radio, then fall behind as radio compounds. Their station panel also thins
sooner (0.77 against 0.93 at week 8). This is §17i's weekly push in the radio target
(`currentLabelPush × 0.40 × acceptance`, held at 0.85 while a record is in the top 20) inside the airplay
convexity. Per §17b, it sets *whose* records the top holds.

**The second gap is other labels' hit-act entry.** Non-Big-5 hit acts put 0.72 records a year on the Hot 100,
against history's 2.18. The lane takes seed 1001 to 0.90. Hot launches enter at 31% (Independent), 53% (MidTier)
and 70% (Big-5), and campaign impact is the strongest entry term. Label push gates both entry and height. History
says a known act's follow-ups charted wherever it was signed (§17w: fame bought entry, not height).

**Reading.** The lane is historically right, and it cannot ship alone. Its partner must make a hit act's airplay
label-blind after entry. It should do that by bringing the Big-5 down to the field, not the field up: conversion
per Hot 100 debut is already 27-37% against history's 16% at every tier. Entry for other labels' hit acts is a
separate shortfall, and the cadence targets need it. It should not be closed by raising height.

**Candidate partners (not built):**

- *Existing: `--radio-push-spread`.* It compresses campaign impact toward the mean for every record. At 0.5 it closed
  §17b's stay premium (0.95 → 0.05 weeks) but not the excess. It needs no build. It reaches entry and launch as well
  as the post-entry weekly term, and it is not specific to hot acts.
- *New, narrow: a known-act airplay floor.* For a record whose act is hot (the lane's 52-week window), the weekly
  label-push term in the radio target becomes label-blind: the field-mean push for that tier-blind population, in
  place of the label's own. That cuts the Big-5 and Major premium after entry and lifts Independent hot acts a little.
  Cold records keep label push, which is history's channel for breaking new acts. Its sizing target is history's
  1.00 conversion ratio for hit acts, and overall hit-act conversion must not rise.
- *Entry for known acts* (§17y's open question): launch entry for hot acts read more from the act's recognition and
  less from campaign impact. It is sized against non-Big-5 hit acts' 2.18 Hot 100 debuts a year.

**Screen implication.** Arm L as specified in §17y will fail §17n's Big-5 guard. The useful screen pairs the lane
with one post-entry partner. Measured first: Top 10 per Hot 100 debut for hit acts, Big-5 against others (target
ratio 1.0, level falling toward 16%), Hot 100 debuts per hit act by group (targets 3.3 / 2.2), then §17n's table.

Caveats: a history hit act carries the label of its Y − 1 hit, so later label moves are missed. Artist credits are
normalized by name and "with" / "and his" suffixes. History has 65 Big-5 hit act-years in four years. The model
cannot separate the launch-week from the weekly part of campaign impact; the radio-heat profile is what places the
premium after entry.

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `hot_edge_common.py`, `hot_edge_funnel.py` /
`hot-edge-funnel.txt`, `hot_edge_model.py` / `hot-edge-model.txt`, `hot_edge_history.py` / `hot-edge-history.txt`,
`hot_edge_history2.py` / `hot-edge-history2.txt`, `hot_edge_projection2.py` / `hot-edge-projection2.txt`,
`hot-edge-weekly.txt` (an inline weekly-profile query on the same pickles).

### 17ab. Build: known-act airplay (`--known-act-airplay[=P]`; 2026-10-18, screen proposed, not run)

**Build.** `Systems/KnownActAirplay.cs`, off unless named. It applies to a single whose act was hot at launch: a Top
40 within the lane's 52-week window, on the lane's clock, with opening careers seeded from public history. While
that single is on the Hot 100 (positions 1-100), the weekly label push read by `UpdateRadioHeat` uses campaign
impact `P` in place of the label's own. The release-age taper and the top-20 hold still apply, so only the label's
strength is replaced. `P` defaults to **0.60**, the mean raw campaign impact of V1's hot Hot 100 entrants
(1961-64, five seeds; Big-5 0.80, procedural Major 0.75, MidTier 0.62, Independent 0.38). At that value the
population's average push is unchanged and only the label spread goes.

- *Where it is read.* `UpdateLabelPush` writes `RecordRuntimeData.knownActRadioPush`, using the week factor it has
  just computed. `UpdateRadioHeat` reads it through `KnownActAirplay.ForRadio`, which returns `currentLabelPush`
  unless the probe wrote a value. The regional heat inherits the change through `radioHeatBaseTargetThisWeek`.
  The release ramp in sales, awareness, launch stock, the launch radio seed and `totalLabelInvestment` all still
  read the label's own push.
- *Hot at launch* is decided once, on the record's first weekly update with the switch on, and stored in
  `knownActHotAtLaunch`. That update comes before the record can credit its own Top 40, which lands when its
  chart peak reaches 40 (`ArtistManager.OnRecordChartUpdated`). So a cold launch stays cold after its own hit, and
  a hot launch stays hot as the act cools. Before entry the label's push stands: label push breaking a new
  record is history's channel and stays as it was.
- *Clock.* `HitActCadence.ClockEnabled` (the lane, retention or this switch) now gates `OnTop40`, so the switch
  works without the lane. Both fields are runtime-only, like the lane's clock, and are not saved.
- *Scope.* Market-side and label-blind, so the player's records are included. A label-less record keeps its
  0.02 push and clears any known-act value.
- *Telemetry.* No CSV schema change. A cumulative `KNOWN_ACT_AIRPLAY` console line each year gives P, hot and cold
  launch decisions, and record-weeks applied.

**Checks.** `--known-act-airplay-check` (`SimTools/KnownActAirplayChecks.cs`) passes.

- *Off:* no push written, nothing decided, `ForRadio` reads `currentLabelPush`, the clock stays unset, and
  `UpdateLabelPush` leaves both fields at −1.
- *On, the clock and window:* the switch alone writes the clock without turning on the lane or retention. 52
  weeks after a Top 40 is hot and 53 is cold. A 1959 Top 40 is hot at week 0 and a 1958 one is not.
- *On, the push:* `P × week factor`, with P = 0.40 at factor 0.5 giving 0.20. The decision is made once, and an
  unknown act is cold.
- *On, the scope:* the push applies at #100 and not at #101, not before entry (though the launch is already
  decided), and never to an album. A label-less record is reset.
- *Parse:* absent, the bare switch and `=P`. Out-of-range, non-numeric and repeated values throw.

The `--hit-act-cadence-check`, `--senior-cooling-check`, `--radio-push-spread-check` and
`--label-hold-capacity-check` checks still pass. The Debug build has 0 warnings and 0 errors.

**Inertness.** A 52-week seed-5005 run with the switch off (`known-act-off-identity`) is **byte-identical** to
`hit-cadence-off-identity4` in all 80 CSVs, with an empty error log (`identity-known-act-off.txt`). The §17y and
earlier controls stay valid.

**Smoke (one seed, not a screen).** V1's flags + `--hit-act-cadence --hit-act-retention` + the switch at P = 0.60
and P = 0.40. Seed 1001, 104 weeks, read on 1961. Both runs completed with empty error logs. In year one the switch
was applied on 1,879 record-weeks (P = 0.60), from 857 hot and 4,633 cold launch decisions.

| 1961, seed 1001 | History | V1 | Lane (v4) | Lane + P 0.60 | Lane + P 0.40 |
|---|---:|---:|---:|---:|---:|
| **Big-5 Top 10 share** | 20.6 | 17.3 | 26.9 | **26.7** | **14.8** |
| Top 10 records / Top 10 weeks per record | 105 / 4.52 | 98 / 4.86 | 89 / 5.36 | 89 / 5.36 | 91 / 5.25 |
| Hot 100 debuts per hit act | 2.58 | 0.70 | 1.15 | 1.37 | 1.22 |
| Acts on the Hot 100 | 395 | 647 | 588 | **527** | 561 |
| Hot 100 records per charting act | 1.98 | 1.21 | 1.32 | 1.46 | 1.37 |
| Hit acts, Big-5 / other | 18 / 50 | 24 / 56 | 16 / 63 | 20 / 59 | 13 / 73 |
| Hit-act Top 10 per Hot 100 debut, Big-5 / other | 25.5 / 16.7% | 21.4 / 23.8% | 32.4 / 28.1% | 31.7 / 16.4% | 32.0 / 17.5% |
| Performance departures / contract-expiry drops | — | 420 / 886 | 514 / 788 | 505 / 813 | 515 / 831 |

Hot launches, 1960-61 pooled, by tier (`known-act-mechanism.txt`):

| Hot launches | Radio heat, chart wk 0-6: lane → P 0.60 → P 0.40 | Top 10 / entry: lane → 0.60 → 0.40 | Yield: lane → 0.60 → 0.40 |
|---|---:|---:|---:|
| Big-5 | 0.454 → **0.428** → **0.415** | 34.9 → 23.9 → 19.8% | 0.94 → 0.82 → 0.56 |
| Procedural Major | 0.447 → 0.445 → 0.428 | 9.5 → 35.3 → 14.3% (n ≈ 20) | 0.12 → 0.72 → 0.38 |
| MidTier | 0.419 → 0.410 → 0.401 | 24.7 → 16.4 → 19.3% | 0.51 → 0.43 → 0.50 |
| Independent | 0.363 → 0.360 → 0.364 | 8.5 → 12.1 → 14.3% | 0.07 → 0.16 → 0.18 |

**Reading (one seed; ±5 Big-5 points of stream noise, [[single-seed-decade-ab-noise-floor]]).**

- *It does what it was built to.* Hot Big-5 entrants lose radio heat (−0.026 at P 0.60, −0.039 at P 0.40), and
  their Top 10 conversion after entry falls to the MidTier level or below. Independent hot conversion rises. The
  lift in Independents' radio heat is too small to see across runs whose populations differ: at their week
  factors (rarely top 20) it is about +0.02 in the target, against a lerp of 0.28.
- *P 0.60 does not move the 1961 Big-5 share on this seed* (26.9 → 26.7). Big-5 hot entry rose 45.5 → 54.5%, and
  the yield fell only 13%. *P 0.40 overshoots* (14.8 against history's 20.6), with the Big-5 hot yield at 0.56.
  One seed cannot place P, but the two values bracket the target.
- *The cadence side improved with the switch.* At P 0.60, acts on the Hot 100 fell to 527 (history 395, V1 647),
  and Hot 100 records per charting act rose to 1.46 (history 1.98). Top 10 turnover is unchanged from the lane
  (89 records, 5.36 weeks each), so the switch does not fix §17y's turnover guard.
- *Retention's departures guard still fails* in every arm: +20-23% performance departures against V1. That is
  §17y's open item, not this switch's.

**Screen (proposed, not launched): five seeds × 260 weeks, paired.** Every arm runs V1's flags +
`--hit-act-cadence --hit-act-retention`. The partner's effect is read against arm LR, and the whole bundle against
V1's existing five-seed controls (`momhl052-launchsat050-headtpl2-confirm-v1`, reused).

| Arm | Adds | Question |
|---|---|---|
| LR | nothing (§17y's LR arm) | The lane's own Big-5 rise across five seeds; §17z projects +4 to +5 points a year |
| A60 | `--known-act-airplay` | Does an average-preserving label-blind push cancel that rise? |
| A40 | `--known-act-airplay=0.40` | A stronger cut: does it overshoot history's Big-5 share? |

That is 15 runs, about 46 minutes per batch of five in parallel, so about 2.5 hours. Arm D (defaults + lane +
retention + the better P), required by §17v for an artist-side change, waits for this result. It would be one
more batch of five against the default controls.

*Read first, in order:*

1. Hit-act Top 10 per Hot 100 debut, Big-5 against others (history ratio 1.00 ± 0.18; V1 1.38), and its level
   (history 16%).
2. Big-5 Top 10 share by year (history 20.6 / 21.2 / 26.0 / 18.7), paired against LR and V1.
3. Top 10 turnover: records and weeks per record.

*Then:*

- Hot 100 debuts per hit act, Big-5 and others (history 3.3 / 2.2).
- Acts on the Hot 100 (386).
- The §17x cadence targets.
- §17n's concentration table in full.
- #1 tenure.
- Drops and departures (the +20% line).
- Genre error.

*Stop rule:* the §17o yearly watch, unchanged. An arm stops when a band is missed by more than 2 points and by more
than V1 misses it.

*Pass for a partner arm:*

- Big-5 share at or below LR in four of five seeds every year, and within 3 points of history's 1961-64 mean.
- The conversion ratio below 1.2.
- No new guard failures beyond LR's.

*Kill:* the Big-5 share inside LR's noise (paired SE) in 1962-64, or Top 10 turnover worse than LR's.

Evidence, in `SimLogs/`: `known-act-off-identity/`, `known-act-smoke/` (consoles, empty error logs), and in
`momhl052-launchsat050-headtpl-diagnostics/`: `identity_compare.py` / `identity-known-act-off.txt`,
`known_act_smoke.py` / `known-act-smoke.txt`, `known_act_mechanism.py` / `known-act-mechanism.txt`.

### 17ac. Screen result: the lane does not lift the Big-5; known-act airplay rejected (`hitcad-*-screen-v1`, 2026-10-18)

**Runs.** Build `6aaac2e`, five seeds × 260 weeks per arm. Each arm runs V1's flags + `--hit-act-cadence
--hit-act-retention`; A60 adds `--known-act-airplay` and A40 adds `--known-act-airplay=0.40`. The yearly watch
(`yearly_watch_cadence.py`, the §17o stop rule) read LR against V1, and A60 and A40 against LR.

- **LR and A60** completed all 10 runs: 260 weeks each, about 39 minutes, empty error logs. The watch reported LR
  WITHIN band every year, and A60 WITHIN through 1963 with 1964 MARGINAL (CR8 37.2).
- **A40 was stopped after 1960** at week 57-60, with Godot left at 0. Its 1960 CR8 was 45.7 against the 48-58 band,
  while LR stood at 50.3 and V1 at 51.1. Its Big-5 share was 25.0 (LR 31.4, history 34.2). P = 0.40 cuts the
  opening year that history leads with the majors.

**1. Hit-act conversion per Hot 100 debut** (1961-64, five seeds pooled):

| | History | V1 | LR | A60 |
|---|---:|---:|---:|---:|
| Big-5 / other hit acts | 15.96 / 15.93% | 37.4 / 27.0% | 26.1 / 22.0% | 27.4 / 21.9% |
| **Ratio** | **1.00** ± 0.18 | 1.38 | **1.19** | 1.25 |
| Ratio by seed | | 0.67 0.92 2.00 1.31 1.50 | 0.92 1.55 1.50 1.09 1.10 | 1.73 1.02 1.22 1.02 1.41 |
| Hot 100 debuts per hit act, Big-5 / other | 3.28 / 2.18 | 0.92 / 0.72 | 1.81 / 0.94 | 1.74 / 0.98 |

The lane alone took the ratio from 1.38 to 1.19, just inside history's interval. Big-5 hit acts doubled their
Hot 100 debuts (0.92 → 1.81), and each converted less often (37.4 → 26.1%). A60 added nothing (1.25), and the
per-seed ratios run 0.67-2.00, so the ratio is noisy at five seeds.

**2. Big-5 Top 10 share:**

| | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|
| History | 20.6 | 21.2 | 26.0 | 18.7 |
| V1 | 25.9 | 21.3 | 28.9 | 24.7 |
| **LR** | **21.8** | **21.5** | **25.3** | **19.0** |
| A60 | 23.9 | 26.1 | 22.7 | 18.2 |
| LR − V1, paired (SE) | −4.0 (4.4) | +0.2 (2.4) | −3.6 (3.0) | −5.7 (3.4) |
| A60 − LR, paired (SE) | +2.1 (1.8) | **+4.6** (1.8) | −2.6 (3.8) | −0.8 (2.3) |

LR sits within 1.2 points of history in every year 1961-64. A60 raised the share in 1961-62. In 1962 four of five
seeds moved up, by +4.6 at an SE of 1.8, the wrong way. A60 fails both the pass rule (at or below LR in four of
five seeds every year) and the kill rule.

**3. Top 10 turnover** is unchanged in every arm: 97-101 Top 10 records a year (history 105) at 5.1-5.4 weeks each.
§17y's one-seed turnover worry (89 records, 5.36 weeks) did not hold.

**Other guards, five-seed means 1961-64:**

| | History | V1 | LR | A60 |
|---|---:|---:|---:|---:|
| Acts on the Hot 100 a year | 386 | 627-670 | 560-616 | 554-597 |
| Hot 100 records per charting act | 2.03 | 1.21-1.24 | 1.31-1.37 | 1.33-1.39 |
| #1 weeks per #1 record | 2.55 | 2.12-2.69 | 2.13-2.67 | 2.23-2.67 |
| Cold-return slots a year, 1962-64 (watch) | 17 | 24-49 | 22-29 | — |
| Performance departures vs V1 | | | **+7.9%** | +7.4% |
| Contract-expiry drops | | 848-1,796 | 770-1,678 | 774-1,658 |
| CR8, 1961-64 (band 38-48) | | 43.3-49.1 | 38.2-41.3 | 37.2-45.9 |

**Correction to §17z and §17aa.** The lane's projected +4 to +5 Big-5 points did not happen: LR is 0 to 6 points
*below* V1. §17z held each group's Top 10 yield per launch fixed and scaled launch counts. But the lane adds
launches to the *same* acts, and a hit act's extra singles convert less often. That is history's shape (§17w:
more, smaller records), and the fixed-yield projection could not see it. The 9.6-point jump that started §17z was
seed 1001's draw: LR's seed 1001 is +9.6 against V1 in 1961, and the other four seeds fall. §17aa's history finding
stands, since history's hit acts did convert at the field's rate. But the premium it measured was partly an
artefact of V1's under-release, and the lane removes most of it.

**Verdicts.**

- **Known-act airplay: rejected at both P.** A60 does not cut the Big-5 share and does not lower the conversion
  ratio; A40 breaks 1960. The switch stays in the tree, off by default.
- **The lane + retention (LR): the best candidate of this arc, not adopted.** It is the first arm to put the Big-5
  share on history in all four years, with the conversion ratio inside history's interval, cold returns down by
  half, 10% fewer charting acts, and turnover and #1 tenure unchanged. Performance departures are +7.9%, inside
  the +20% line.
- **LR's open items:**
  - CR8 sits at the floor of the band (38.2 in 1964, 40.1 in 1961).
  - Cadence is still half of history's (1.81 / 0.94 Hot 100 debuts per hit act, against 3.28 / 2.18).
  - Other labels' hit acts gained least.
  - §17v requires an artist-side change to stand on defaults as well, so arm D is owed.

**Next.**

- *Arm D:* defaults + lane + retention, five seeds × 260 weeks against the default controls. One batch, about 40
  minutes.
- *Analysis:* split LR's fall in Big-5 conversion between the same-act split and plain dilution, and read genre
  error.
- *The cadence shortfall* is the entry question §17y left open: a non-Big-5 hit act's follow-up still reaches the
  Hot 100 well under half the time.

Evidence: `SimLogs/hitcad-lr-screen-v1/`, `hitcad-a60-screen-v1/`, `hitcad-a40-screen-v1/` (manifests, consoles,
error logs). In `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `run_cadence_screen.ps1` /
`screen-cadence-launch.log`, `yearly_watch_cadence.py` / `yearly-watch-cadence.log`, and `cadence_screen_readout.py`
/ `cadence-screen-readout.txt`.

### 17ad. Arm D: the lane on defaults keeps the head flat but not the conversion ratio (`hitcad-d-screen-v1`, 2026-10-18)

**Runs.** Build `e7a39d2` (Debug rebuilt), defaults + `--hit-act-cadence --hit-act-retention`, five seeds × 260
weeks, paired against the five-seed default controls (`top10-fiveyear-review-v1` 1001-3003,
`top10-default-controls-260-v1` 4004/5005). As in §17v's arm D, no V1 flag is set. All five runs passed in about 42
minutes with empty error logs. The yearly watch (`yearly_watch_cadence.py`, D against the control, LR alongside)
reported WITHIN every year 1960-64.

**§17v's question: does an artist-side change hand slots to label push on defaults?** No. §17v's arm D raised the
Big-5 share +2.5 / +2.4 / +4.7 in 1962-64. The lane does not:

| Big-5 Top 10 share | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|
| History | 20.6 | 21.2 | 26.0 | 18.7 |
| Control | 33.8 | 29.8 | 29.2 | 27.8 |
| **D** | **30.7** | **31.6** | **28.8** | **26.8** |
| D − control, paired (SE) | −3.0 (3.1) | +1.7 (4.9) | −0.4 (3.1) | −1.0 (1.8) |

Every year is inside noise, and the 1962 mean rests on seed 2002 (+18.7). Concentration is unchanged: CR4 / CR8 are
30.5-31.0 / 44.2-45.3 in 1961-64, against the control's 28.5-37.3 / 44.0-50.8, and 1961 is closer to band than the
control. The lane does not move the head on defaults either way. History's lower Big-5 share stays out of reach
without V1's push-side flags.

**The cadence gains carry over, and they are the robust result:**

| 1961-64 | History | Control | D | V1 | LR |
|---|---:|---:|---:|---:|---:|
| Hot 100 debuts per hit act, Big-5 / other | 3.28 / 2.18 | 1.00 / 0.69 | **1.95 / 0.97** | 0.92 / 0.72 | 1.81 / 0.94 |
| Acts on the Hot 100 a year | 386 | 619-654 | **574-604** | 627-670 | 560-616 |
| Hot 100 records per charting act | 2.03 | 1.22-1.26 | **1.33-1.36** | 1.21-1.24 | 1.31-1.37 |
| Cold-return slots a year, 1962-64 | 17 | 38.8 / 60.2 / 51.8 | **18.2 / 24.4 / 32.6** | — | 29.0 / 22.2 / 28.0 |

Acts on the Hot 100 fall by 41-61 a year, in every seed and every year (SE 5-8). Cold-return slots fall by half,
from 50.3 to 25.1 a year, the same cut LR made on V1. That cut is the lane's: it holds on both bases.

**The conversion ratio does not carry over. It gets worse:**

| Hit-act Top 10 per Hot 100 debut, 1961-64 | History | Control | D | V1 | LR |
|---|---:|---:|---:|---:|---:|
| Big-5 / other | 15.96 / 15.93% | 38.5 / 28.0% | **34.9 / 19.7%** | 37.4 / 27.0% | 26.1 / 22.0% |
| **Ratio** | **1.00** ± 0.18 | 1.37 | **1.77** | 1.38 | 1.19 |
| Ratio by seed | | 1.11-1.96 | 1.47-1.99 | 0.67-2.00 | 0.92-1.55 |

On defaults the Big-5's hit acts double their Hot 100 debuts and keep converting (38.5 → 34.9%). Other labels'
hit acts gain fewer debuts, and those convert less (28.0 → 19.7%). On V1 the Big-5's extra singles were the ones
that converted less (37.4 → 26.1%). The difference between the bases is V1's launch-push saturation, which trims a
Big-5 launch's push. So **LR's move to 1.19 came from the lane meeting V1's saturation, not from the lane alone.**
The Big-5 share still holds because the Big-5 have fewer hit acts in D (402 against 496, pooled 1961-64), each more
prolific.

**Other guards, D against the control:**

- *Top 10 turnover:* 98.8-102.2 records a year at 5.10-5.27 weeks, unchanged (paired +0.0 to +4.4, inside noise).
- *#1 tenure:* 2.06-2.40 against 2.06-2.61. The 1961 change is −0.27 (SE 0.11), with all five seeds down, so it
  moves away from history's 2.55. 1962-64 are inside noise.
- *Performance departures:* **+8.1%**, inside the +20% line (LR +7.9%). Contract-expiry drops fall 10-12% every
  year.
- *Year-end genre error worsens every year:*

  | Year-end genre error, 14 genres | 1960 | 1961 | 1962 | 1963 | 1964 |
  |---|---:|---:|---:|---:|---:|
  | Control | 23.6 | 31.6 | 33.0 | 41.4 | 37.6 |
  | D | 26.4 | 37.8 | 43.6 | 46.2 | 44.0 |
  | V1 | 24.8 | 36.0 | 35.4 | 45.0 | 37.8 |
  | LR | 26.2 | 35.4 | 39.6 | 48.8 | 44.0 |

  D is +2.8 to +10.6 above the control in all five years, and LR is above V1 in four. The largest moves are RnB
  over target (1962 32.2 against 28, control 28.6), and Soul, RockAndRoll and Country further under. This was
  LR's open "read genre error" item. It is a cost of the lane on both bases. It is not sized against seed noise yet.

**Verdict.**

- **§17v's defaults test: passed.** The lane does not hand slots to label push on defaults. The Big-5 share,
  CR4/CR8, turnover and departures all hold.
- **The lane's effects split in two.** The cadence effects belong to the lane and hold on both bases: fewer
  charting acts, more records per act, cold returns halved. The fair-conversion effect (ratio → 1.19) needs V1's
  launch-push saturation, and on defaults it reverses (1.77).
- **The lane cannot ship alone on defaults.** Its best form is LR, the lane on top of V1. That ties it to V1's
  open 1963-64 question (§17t), although LR's 1963-64 is better than V1's (Big-5 25.3 / 19.0 against 28.9 / 24.7).
- **New cost:** year-end genre error rises 3-11 points a year on defaults and up to 6 on V1. That has to be
  explained before any adoption.

**Next.**

- *Analysis:* which genres' hit acts the lane favours, and why RnB gains and Soul, RockAndRoll and Country lose.
  Then size the genre error against paired seed noise. The likely channel is labels' genre mix: the lane serves
  the hot acts a label already has.
- *Analysis:* split LR's fall in Big-5 conversion between the same-act split and dilution (still owed from §17ac),
  now with D as the no-saturation contrast.
- *The cadence shortfall* is unchanged: 1.95 / 0.97 Hot 100 debuts per hit act against 3.28 / 2.18.

Evidence: `SimLogs/hitcad-d-screen-v1/` (manifest, consoles, empty error logs). In
`SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `run_cadence_arm_d.ps1` / `screen-cadence-d-launch.log`,
`yearly-watch-cadence-d.log`, `cadence_d_readout.py` / `cadence-d-readout.txt`, and `genre-check-cadence-d.txt`
(`genre_check_260.py`).

### 17ae. The lane's genre error: a genre-blind lane meets genre-unequal follow-ups (2026-10-18, analysis only)

Analysis only, on control, D, V1 and LR, five seeds, 1961-64. No simulation was run and no code was changed. A
launch is *hot* when its act had a record reach the Top 40 in the 52 weeks before it. Single launches are
`release-strategy.csv`'s orphan singles plus album promo singles, which are logged on the album's row
(`promoSingleId`). Without the promo singles, half of all Hot 100 slot-weeks were unattributed. With them, 95% are
attributed.

**1. Size, paired per seed** (14-genre year-end error, computed per seed and then averaged):

| | 1961 | 1962 | 1963 | 1964 | 1961-64 (SE) | Seeds |
|---|---:|---:|---:|---:|---:|---|
| D − control | +3.4 | +6.6 | +1.2 | +4.4 | **+3.9 (1.8)** | +8.2 +6.5 +5.0 +1.0 −1.2 |
| LR − V1 | −1.8 | +1.4 | +4.2 | +4.2 | **+2.0 (2.6)** | +3.2 −0.5 +10.5 +2.5 −5.8 |

**Correction to §17ad.** §17ad reported the error of the five-seed mean slots (D +2.8 to +10.6, LR above V1 in
four years). Paired per seed, the cost on defaults is +3.9, about two SE, with four of five seeds up. On V1, LR's
+2.0 is inside noise, with three of five seeds up. Resolving 2 points would take about four times the seeds. So the
error is a real but modest cost on defaults, and **unresolved for LR, the actual candidate.**

**2. Where it lands** (year-end slots a year, 1961-64; paired change, SE):

| Genre | Bench | Control → D | D − control | V1 → LR | LR − V1 |
|---|---:|---:|---:|---:|---:|
| TeenPop | 12.8 | 21.1 → 22.2 | +1.1 (1.2) | 21.0 → 22.1 | **+1.1 (0.4)** |
| RnB | 19.2 | 19.8 → 22.4 | **+2.7 (0.5)** | 21.6 → 21.5 | −0.1 (1.6) |
| Soul | 11.2 | 7.7 → 6.5 | −1.1 (0.6) | 5.8 → 4.6 | −1.2 (1.0) |
| Country | 6.0 | 3.0 → 2.5 | −0.4 (0.3) | 2.7 → 3.2 | +0.5 (0.8) |
| Folk | 3.5 | 0.6 → 0.1 | **−0.5 (0.1)** | 1.2 → 0.3 | −0.9 (0.6) |

The lane pushes TeenPop further over target (already 1.65× the benchmark), and on defaults RnB past it. It pulls
Soul and Folk further under. On V1, BritishBeat (+0.8, SE 0.3) and DooWop (+0.5, SE 0.2) improve, which is why
LR's net is smaller.

**3. Mechanism: the lane is genre-blind, and the payoff of a follow-up is not.**

The lane roughly doubles hot single launches in **every** genre (×1.6-2.7). They are paid for by cold launches,
which fall 4-8% in nearly every genre. But how much an extra hot launch adds varies about tenfold by genre:

| Hot launches (1961-64) | Extra hot launches a year, D / LR | Extra hot slot-weeks a year, D / LR | Slot-weeks per extra launch, D / LR | Hot entry %, control → D | Big-5 % of hot launches (D) |
|---|---:|---:|---:|---:|---:|
| TeenPop | +59 / +67 | +221 / +254 | **3.7 / 3.8** | 50 → 46 | 14 |
| RnB | +61 / +51 | +186 / +135 | **3.0 / 2.6** | 39 → 37 | 12 |
| RockAndRoll | +15 / +20 | +40 / +49 | 2.7 / 2.4 | 46 → 40 | 17 |
| TraditionalPop | +45 / +48 | +54 / +39 | 1.2 / 0.8 | 56 → 37 | 39 |
| Country | +23 / +32 | +23 / +48 | 1.0 / 1.5 | 42 → 31 | 31 |
| Soul | +20 / +12 | +7 / −21 | **0.3 / < 0** | 52 → 35 | 7 |
| EasyListening, Folk, Jazz | +5 to +13 each | −10 to −2 | ≈ 0 | 41-49 → 19-32 | 30-40 |

- **TeenPop's and RnB's follow-ups keep charting.** Their hot entry rate barely moves (50 → 46, 39 → 37), so each
  extra single adds 3-4 Hot 100 weeks. Soul's, the adult genres' and Country's follow-ups mostly miss: hot entry
  falls 11-21 points, and the extra launches add almost nothing.
- **It is not a label effect.** Soul's hot launches are 7% Big-5, about the same as RnB's (12%). TraditionalPop's are
  39% Big-5, yet still convert badly. The split is by genre.
- **The cold side funds it.** Cold Hot 100 slot-weeks fall most in TeenPop (−176 / −151), Country (−101 on
  defaults) and Soul (−71 / −78, SE 41-48). TeenPop more than refills its loss from hot acts. Soul does not.
- **Fewer Soul acts break through:** Soul hit acts fall 19.7 → 16.9 (D) and 16.7 → 12.5 (LR). The cold Soul
  launches that produced new Soul hit acts are the rolls the lane takes.

**Reading.** The lane reallocates rolls from cold launches to hit acts' follow-ups, in proportion to where hit acts
already are. Where a follow-up converts (TeenPop, RnB, RockAndRoll), the genre gains. Where it does not (Soul,
Country, Folk, EasyListening, Jazz, TraditionalPop), the genre loses the cold launches and gets little back. History's
Soul share rose through 1963-64 (benchmark 7 → 15 → 14) under the quarterly hit-act cadence that §17x measured
across all genres. So a Soul follow-up that fails to enter is most likely a defect the lane exposes, not one it
creates. History's cadence has not been measured by genre, so this is not confirmed.

Two candidate causes, not tested here:
- **Genre single-market size.** Entry is a launch-week units bar ([[top40-is-a-launch-week-bar]]). A follow-up in a
  small single pool (Soul, Folk, Jazz) or an album-leaning one (TraditionalPop, EasyListening) cannot clear it.
- **Same-act cannibalization.** The act's audience splits between its own singles, and that may bind harder in a
  small genre pool.

**Verdict.**

- Genre error is a modest cost of the lane on defaults: +3.9 points a year, SE 1.8. For LR it is unresolved: +2.0,
  SE 2.6. It does not disqualify LR, which **stays the best candidate of the arc.**
- The cost has a clear mechanism. A genre-blind lane meets genre-unequal follow-up entry. Restricting the lane by
  genre would be targeting, and is not proposed. The defensible fix is on the entry side: why a hot Soul or Country
  follow-up misses the Hot 100 at twice TeenPop's rate.

**Next.**

- *Analysis:* for hot follow-ups, compare launch-week units and the Hot 100 cutoff by genre (Soul, Country and
  TraditionalPop against TeenPop and RnB). Then split the miss between genre pool size and same-act
  cannibalization (an act with two singles live against one). This is the same entry question as §17y's cadence
  shortfall, now with a genre address.
- *Analysis (still owed from §17ac):* split LR's fall in Big-5 conversion between the same-act split and dilution,
  with D as the no-saturation contrast.

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `genre_error_cadence.py` /
`genre-error-cadence.txt`.

### 17af. Why weak-genre follow-ups miss: the lane turns album promo singles into orphan singles; no same-act cannibalization (2026-10-18, analysis only)

Analysis only, on V1 and LR, five seeds, launches 1961-64. Only these arms carry `single-launch-attribution.csv`, the
per-launch log that includes chart misses. No simulation was run and no code was changed. *Hot* is as in §17ae.
*Weak genres* are Soul, Country, TraditionalPop, EasyListening, Folk and Jazz, and *good genres* are TeenPop, RnB and
RockAndRoll (§17ae's split). Calibration mode leaves out the weekly genre-market and breakout logs, so a miss's
launch-week units cannot be seen. Lifetime sell-in (`release-outcomes.csv` `pressingBookSold`, about 1.5× units at
the last chart week) stands in for size.

**1. The genre term is real, but it predates the lane.** A linear probability model of hot-launch entry was fitted on
the launch inputs (quality, hook, stock, awareness, recognition, campaign impact) and genre. Each genre's entry gap
to TeenPop splits into what its inputs explain and the genre itself:

| Gap to TeenPop, points | V1: gap = inputs + genre | LR: gap = inputs + genre | LR cold launches: genre |
|---|---|---|---:|
| Soul | +3.6 = +14.1 − 10.4 | −14.1 = −3.7 − 10.4 | −8.5 |
| Country | −2.1 = +6.5 − 8.6 | −11.4 = −4.1 − 7.2 | −6.0 |
| TraditionalPop | +7.1 = +25.9 − 18.9 | −8.8 = +7.8 − 16.5 | −10.2 |
| EasyListening | −5.5 = +20.7 − 26.2 | −21.1 = −0.4 − 20.7 | −14.0 |
| Folk | −2.0 = +20.7 − 22.7 | −19.2 = −2.7 − 16.5 | −10.4 |
| Jazz | −16.5 = +17.1 − 33.5 | −23.9 = +4.0 − 27.9 | −15.9 |

Quality carries the inputs: +32 to +34 entry points per SD, against +6 to +8 for campaign impact and awareness.
The genre term is about the same in V1 and LR (Soul −10.4 in both), and cold launches show the same pattern. So it
is a property of the genre market the lane did not change. Its source is not resolved: smaller single pools, or the
per-genre radio acceptance in the airplay exponent ([[radio-acceptance-split]]). Settling it needs the weekly genre-market
log, which calibration mode does not write. Entrants clear the cutoff the same way in every genre (median 1.19-1.31×
the cutoff, 2.7-4.2k units in the entry week).

**What the lane changed is the inputs.** In V1, weak-genre hot launches carried better material than TeenPop's
(Soul quality 0.70 and hook 0.77, against TeenPop's 0.65 / 0.67), and that offset the genre term. In LR the edge is
gone (Soul 0.64 / 0.61, TeenPop 0.64 / 0.61). For Soul, the whole V1 → LR swing (−17.7 points) is in the inputs.

**2. Where the material went: the album override.** When a lane pick's label would have started an album project,
the lane makes it an orphan single (`HitActCadence.ShouldForceSingle`, `CompetitorManager.cs:1967`). That happened on
**48%** of lane picks: 1,321-1,559 of 2,771-3,079 per seed through 1963 (`HIT_ACT_CADENCE` console lines). An album's
promo single is its best track. An orphan single is whatever the act has.

| Hot launches, 1961-64 | V1 album promo | V1 orphan | LR album promo | LR orphan |
|---|---|---|---|---|
| Weak genres: n, quality / hook, entry | 1,479, 0.717 / 0.809, **59.8%** | 818, 0.639 / 0.594, 27.5% | 193, 0.699 / 0.794, 44.6% | 4,479, 0.638 / 0.594, **29.9%** |
| Good genres | 829, 0.661 / 0.781, 51.6% | 1,796, 0.638 / 0.591, 41.4% | 147, 0.642 / 0.745, 35.4% | 5,248, 0.633 / 0.581, 40.1% |

- In V1, **64% of weak-genre follow-ups were album promo singles**, against 32% in good genres. That matches the
  album-leaning genres. Their follow-up entered on the album's best track.
- The orphan path converts the same in both arms (weak 27.5 → 29.9%, good 41.4 → 40.1%). Only the mix changed.
- **The mix shift accounts for the whole drop.** At V1's entry rate for each path, LR's mix (4% promo) gives weak
  genres 28.8%, and LR observes 30.5% (V1 48.3%). For good genres the same shift costs about 3 points (44.6 → 41.7).
  That is §17ae's genre asymmetry.
- Hot-launch material by weeks since the previous single says the same. In LR, weak-genre follow-ups at the lane's
  13-16 weeks have quality 0.640 and hook 0.596. Those within 12 weeks, which the lane does not schedule, have 0.674 / 0.718.

**3. Same-act cannibalization: none worth the name.**

- *The lane's coded split* divides only the new single's launch radio seed (label push and recognition airplay) by
  1 + the act's singles on the Hot 100. It cuts each predecessor's radio heat by 10%. Stock and awareness are whole.
- *The new single:* hot launches with an act single already charting enter **more** often than those without (LR 39.5
  against 33.3%, V1 45.6 against 45.2%). Strong acts are the ones still charting. The visible cost is height: in V1
  such launches peaked at 27 against 40, and in LR both peak at 45. That is the split working as designed (§17y:
  more, smaller records).
- *The predecessor*, matched on chart position and weeks on chart against records with no act or label launch that
  week:

  | LR, act launched an orphan single that week | Good genres | Weak genres |
  |---|---:|---:|
  | Off the chart within 4 weeks, vs matched | −1.0 pts | +0.9 pts |
  | Next-week places, vs matched | +0.6 | +2.7 |
  | Next-week units, vs matched | +1.4% | +0.6% |

  The weak-genre +2.7 places is the 10% radio cut. Survival and units do not move. A launch by another act on the
  same label does nothing either (−0.5 places, +4% units).
- *V1 is confounded and was not used.* In V1 a sibling launch comes with a +44% units jump for the charting single
  even with no album nearby. Radio heat is flat (−0.005), so this is not airplay. Without the lane, a label tends to
  release for an act whose single is surging, so the surge comes before the release. The lane's fixed 13-week clock
  removes that selection, and LR shows +1.2%. An album by the act within two weeks comes with +61-66% in both arms.
  That is timing, or album synergy.
- *No demand-level link exists:* the market has no per-act audience, so an act's two singles compete only like any
  two records in the genre.

**Corrections to §17ae.** Neither of §17ae's two candidate causes is the cause. Genre pool size, or any genre
term, is present at the same size without the lane. Same-act cannibalization is absent. The lane's genre error comes
from **its album override**. It removes the album-promo follow-up that the album-leaning genres relied on, and
replaces it with an orphan single of average material.

**Proposal (not built): let the lane keep the album.** When a lane pick's label chooses an album, the album goes
ahead and its promo single is the follow-up. Only a single-or-nothing pick is forced. A probe switch is needed,
since the override is part of `--hit-act-cadence`.

- *Prediction:* weak-genre hot entry back toward V1's level (30 → 45-50%), most of LR's genre-error cost gone, and
  §17ae's Soul and Folk losses reversed.
- *Risks:*
  - Album projects take weeks to deliver, so follow-ups arrive later, and the cadence gain (§17ac: 1.81 / 0.94 Hot 100
    debuts per hit act) may shrink.
  - Promo singles carry album synergy, so the Big-5 conversion ratio (1.19) and Top 10 heights need re-reading.
- *Screen:* one arm against LR, five seeds × 260 weeks. Read first: weak-genre hot entry, the genre error per seed
  (paired), Hot 100 debuts per hit act, then §17ac's Big-5 share and ratio. Kill: Hot 100 debuts per hit act below
  1.4 / 0.85, or the Big-5 share above LR by more than its paired SE in two years.

Evidence, in `SimLogs/momhl052-launchsat050-headtpl-diagnostics/`: `followup_entry_genre.py` /
`followup-entry-genre.txt`, and `sibling_units_check.py` / `sibling-units-check.txt`. Lane counts are in
`SimLogs/hitcad-lr-screen-v1/*-console.log`.

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
