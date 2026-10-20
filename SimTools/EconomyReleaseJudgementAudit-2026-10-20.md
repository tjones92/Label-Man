# Economy and release-judgement audit

**October 20, 2026 — report only.** Branch `media-atmosphere`, HEAD `a58b509`. No game code was changed and no new
simulation was run. The uncommitted edits to `Data/GenreCatalog.cs`, `Data/MarketRegion.cs` and the album-practice files
belong to the parallel "1965-69 singles research and Jazz allocation" thread and were not reviewed.

The question: which ahistorical balancing and which defects in the economic code hold back (1) AI label release
judgement and (2) an ecosystem in which celebrity can form? This complements
[CelebrityEconomyAudit-2026-10-15.md](CelebrityEconomyAudit-2026-10-15.md). Its findings (opening careers, regional sync,
media, the genre chart guard, the album tier multiplier, the two cash identities) are not repeated here.

## Evidence

Code read in full: `CompetitorManager` (decisions, settlement, deals), `ChartSimulator`, `ChartManager` (clearing,
chart assignment, retirement, discovery), `AlbumSimulator`/`AlbumModel`, `AILabel`, `SimulatedArtist`, career and contract
paths in `ArtistManager`/`RosterManager`, `LabelPressingEconomics`, `EconomicLabelLifecycle`, `LabelLifecycleManager`,
`ContractPricing`, `AILabelFactory`, `ReleaseJudgement`/`AiRepertoireService`, `ArtistRecognitionService`, `HitActCadence`.

Measurements come from existing logs only (read-only). [analyze_economy_audit.py](analyze_economy_audit.py) reproduces
every table:

- **`dec-awrw-on-1001-522`**: the reference decade run, quoted below unless stated otherwise.
- **`dec-lpa4-youth-1001-522`**: the experimental best arm of the parallel thread. Every conclusion below holds there too.
- **`lpafull-ctl-1001-260-album-chart.csv`**: a five-year control, used for album chart peaks.

These are single-seed decade numbers. The effects below are large multiples, not noise-floor deltas.

## Summary

| # | Finding | Kind | Bears on |
|---|---|---|---|
| A1 | Album format memory learns from extrapolations that run 2.3x high; late in the decade it reads the wrong sign | Defect, measured | Release judgement |
| A2 | The single prior is 2-13x too optimistic, steepest for proven acts, with no resolution where charting is decided | Stale calibration, measured | Release judgement |
| A3 | Format choice gives new acts more LPs than proven acts; memory is pooled across a roster's career bands | Model gap, measured | Both |
| A4 | Releases almost never lose money, and nothing lets a label pass | Economic design, measured | Release judgement |
| A5 | The promo-single synergy credit is never tested by any feedback | Model gap | Release judgement |
| B1 | Album chart runs drive the career ladder on singles thresholds | Defect, measured | Celebrity |
| B2 | A never-charting record costs less momentum than a charting one | Defect | Celebrity |
| B3 | Distribution advances are never repaid ($1.95M in the reference decade) | Defect, measured | Economy, player |
| B4 | Royalty rates run backwards by tier | Ahistorical | Celebrity, signing |
| B5 | Marketing money buys almost nothing; push is a fixed label trait | Ahistorical | Both |
| B6 | AI-to-AI publishing on covers pays about 5x the period mechanical | Ahistorical | Economy |
| B7 | Re-signing forgives the unrecouped balance | Ahistorical | Celebrity |
| C | Smaller items: stale comments, a reserve inconsistency, price constants, a double studio modifier, self-referential heat | Mixed | — |

The order is a suggestion. Fixing A1 and A2 changes what the format memory feeds back, and that changes the baseline
against which the album and promo constants were tuned. Each one wants a short-run catastrophe check, then the
end-of-audit decade A/B ([[decade-ab-only-at-audit-end]]).

---

## A. Release judgement

### A1. Album format memory learns from extrapolations that run 2.3x high

**Defect, measured.** At ages 13, 26 and 52 (and at retirement), `UpdateResponsiveMemoryObservation` estimates a
release's final net as `lifetimeLabelNet / maturity − sunkProductionCost`. It uses a *linear* maturity:
`(age + 1) / 52` for albums and `/ 20` for singles ([CompetitorManager.cs:1724](../Systems/CompetitorManager.cs:1724),
[:1761](../Systems/CompetitorManager.cs:1761)). Album demand is front-loaded: a launch ramp, then catalog decay of 0.93 a
week from week 8 ([AlbumSimulator.cs:26](../Systems/AlbumSimulator.cs:26)). So at age 13 an album has earned far more than
27% of its lifetime net, and dividing by 0.27 overstates it.

| Lane | Age | n | Median estimate | Median final | Estimate > final |
|---|---:|---:|---:|---:|---:|
| AlbumComponent | 13 | 30,368 | 40,934 | 17,703 | 100% |
| AlbumComponent | 26 | 30,368 | 30,584 | 17,703 | 100% |
| AlbumComponent | 52 | 29,631 | 17,489 | 18,173 | 1% |
| OrphanSingle | 13 | 21,634 | 4,023 | 3,761 | 76% |
| OrphanSingle | 26 | 18,188 | 4,463 | 4,682 | 3% |

Singles are fine (+7% at age 13). Albums are not. The bias matters because albums stay provisional for a long time. If
the recency-times-maturity weights the live estimator uses (`GetResponsiveMemory`, 104-week window, 52-week half-life) are
rebuilt at each year boundary, about 70-78% of the AlbumComponent lane's decision weight sits on provisional rows:

| Week | Live residual | Same releases' final residuals | Provisional weight |
|---:|---:|---:|---:|
| 260 (1965) | +0.81 | +0.40 | 73% |
| 364 (1967) | +0.38 | +0.08 | 71% |
| 416 (1968) | +0.33 | −0.03 | 69% |
| 468 (1969) | +0.33 | −0.19 | 71% |
| 520 (1970) | +0.38 | −0.33 | 74% |

(Pooled across labels. Each label reads its own lane, but every lane takes the same bias.)

From 1968 the finished albums say albums are *underperforming* their prior, while the memory tells labels they are
*beating* it. At the 0.65 confidence ceiling, the album projection moves about +22-25% above the prior when the evidence
supports roughly −12% to −21%. The StandaloneAlbum lane carries the identical bias. This lands in the late-decade years
the parallel thread is calibrating, and it pushes toward LPs.

**Suggestion.** Replace linear maturity with an empirical cumulative-earnings curve per format, fitted from finalized
records (the revisions CSV already holds both ends). Alternatives: let album lanes learn only from Final rows, or weight
provisional rows by maturity squared. `GetAgeMatchedExpectedNet` exists but is telemetry-only and carries the same
linear assumption.

### A2. The single prior is 2-13x too optimistic, steepest for proven acts

**Stale calibration, measured.** `SinglePriorNormalizedContribution` is a 4x4 table (quality quartile x career band) fitted
on an older economy ([CompetitorManager.cs:2917-2938](../Systems/CompetitorManager.cs:2917)). The rescale for dealer price
and pressing (`SinglePriorNetAtBookedPrice`) shifts it by about −22%. Realized outcomes fell much further:

| Career band | Q3 prior | Q3 realized | Q4 prior | Q4 realized |
|---|---:|---:|---:|---:|
| New/Unsigned | 14,114 | 3,579 | 36,030 | 4,208 |
| Rising | 34,791 | 5,748 | 76,315 | 7,049 |
| Established | 102,117 | 9,006 | 99,175 | 7,711 |
| Star/Superstar | 26,850 (n=8) | 7,958 | 102,942 | 19,584 |

Three structural problems sit on top of the level error:

1. **The career column is not monotone.** The Star column has zero samples, so its Q1-Q3 cells borrow from Rising, while
   Established Q3 borrows from Established Q4. A Star with a Q3 record therefore gets a quarter of an Established act's Q3
   prior (26.5k vs 95.2k mean). In the experimental arm, where Star Q3 has n=54, realized nets run the other way
   (27k vs 10.5k).
2. **There is no resolution where outcomes are decided.** Q4 starts at a quality estimate of 0.624, so every record that
   can realistically chart (the ~0.64 entry cliff in [[release-judgement-directive]]) shares one cell and one value.
   Meanwhile the prior's Q3-to-Q4 jump (2.6x for new acts) is far steeper than realized (1.2x).
3. **Memory can correct only 65% of a persistent error** (`ResponsiveMemoryMaximumConfidence`,
   [:47](../Systems/CompetitorManager.cs:47)). The OrphanSingle lane's final residual has a median of −0.80 (PromoSingle
   −0.72). A persistent −0.80 residual, corrected at most 65%, leaves the projection at 0.48x the prior, so Established
   singles are still projected at about 6x realized.

**Suggestion.** Refit the single prior from current `release-outcomes` x `a3-economic-decisions`: career band x quality
decile (or a fitted function), monotone in career. Revisit the confidence ceiling once the priors are close; it exists to
stop memory overriding a good prior, not a bad one.

### A3. Format choice gives new acts more LPs than proven acts

**Model gap, measured.** Album share of format decisions in the reference run:

| Career band | 1960-62 | 1963-65 | 1966-69 |
|---|---:|---:|---:|
| New/Unsigned | 0.36 | 0.65 | **0.82** |
| Rising | 0.38 | 0.53 | 0.70 |
| Established | 0.37 | 0.59 | **0.68** |
| Star/Superstar | 0.38 | 0.81 | 0.78 |

Realized album net against the album prior: New 0.97x, Rising 1.29x, Established **2.22x**, Star **2.39x**. The album
prior's stature multiplier (1.5x for Established, 2.0x for Star; [:2979](../Systems/CompetitorManager.cs:2979))
under-projects proven acts' LPs, while A2 over-projects their singles. Their formats are mis-ranked from both sides.

The memory cannot learn the interaction because the lanes are keyed by label x format only
([:2464-2473](../Systems/CompetitorManager.cs:2464)). A label's single album residual averages a roster that is mostly new
acts and is then applied equally to its debutants and its stars.

The market side contributes too. A new act's LP nets 16.9k at the median, against 3.5k for its single, so in-sim LP
economics barely require fame. Historically a debut LP from an unknown pop act was marginal, and LPs followed a hit
single. The parallel thread's `HitLedAlbums` practice addresses that order on the decision side.

**Suggestion.** Key the format priors and memory by career band (at least new vs proven), or condition them on
recognition. Lift the album stature multiplier toward the realized ratios. For celebrity, the LP is where fame gets
monetized: a model where proven acts get LPs and unknowns get singles produces stars who sell albums.

### A4. Releases almost never lose money, and nothing lets a label pass

**Economic design, measured.** Share of memory-eligible releases whose realized net is negative. That net includes
production and pressing but excludes marketing, which no prior or outcome counts.

| | 1960-62 | 1963-65 | 1966-69 |
|---|---:|---:|---:|
| Albums | 0.1% | 2.0% | 2.4% |
| Singles | 8.8% | 10.9% | 17.3% |

This holds with dealer pricing and the pressing book (both on by default now). A fired weekly roll releases whenever an
artist is eligible and cash clears the reserve ([:1433](../Systems/CompetitorManager.cs:1433),
[:1606](../Systems/CompetitorManager.cs:1606)). No path compares an orphan single's projected net to zero or to waiting.
The only gate that weighs marketing is the album-practice singles programme. So economics never disciplines release
volume, and "judgement" reduces to the format fork. Historically most singles lost money.

Two smaller accounting points widen the gap. Marketing is charged but left out of both the prior and the realized net
(`realizedNet = lifetimeLabelNet − sunkProductionCost`, [:1638](../Systems/CompetitorManager.cs:1638)), so an
AlbumWithPromo project's two marketing budgets are invisible to the choice. And the promo math uses a hard-coded
`singleNetMarginPerUnit = 0.40` ([:128](../Systems/CompetitorManager.cs:128)) that the dealer-price switch did not
re-price.

**Suggestion.** This is the economic half of the release-judgement directive's "wait/commission" phase. Count marketing
in both the projection and the outcome. Once A2 is refit, a pass threshold (expected net minus marketing, or an
opportunity cost per roll) becomes meaningful. Without a downside, a better ear changes which song ships but not
whether anything ships. Returns (only ~4% of pressed copies go unsold) are the other lever already named in
[[small-labels-never-lose-money]].

### A5. The promo-single synergy credit is never tested by feedback

**Model gap.** Whether an album gets a promo single depends on `componentProjectedAlbumWithPromo`, which carries
`expectedPromoLift + promoSynergyGain` ([:2590](../Systems/CompetitorManager.cs:2590)). `PromoAlbumConversionK = 1.5` is
documented as set to hold late-decade Major entry share off its band floor ([:72-88](../Systems/CompetitorManager.cs:72)).

No memory lane can contradict it. The component lanes observe the promo and the album separately. The total-project
lane measures only component deltas against a projection that already contains the credit. Its estimates stay 2.3x
high even at age 54-56 (49k vs 21k), and `ResolveAlbumDecision` never reads its value: the
`totalProjectMemoryProjection` parameter is unused, although the comment calls it a viability guard
([:2714-2739](../Systems/CompetitorManager.cs:2714)). So an authored constant tuned to a chart-share band permanently
steers the promo-vs-standalone choice.

**Suggestion.** Observe the album's realized lift directly (its demand with an active linked promo vs without, which
`AlbumSimulator` already accumulates) and let that residual scale the synergy credit. Or delete the dead parameter and
comment, so nobody assumes a guard exists.

---

## B. Celebrity and the wider economy

### B1. Album chart runs drive the career ladder on singles thresholds

**Defect, measured.** `AssignChartPositions` fires `OnRecordChartUpdated` and `OnRecordLeftChart` for the album chart as
for the Hot 100 ([ChartManager.cs:1214](../Systems/ChartManager.cs:1214)). `ArtistManager` credits those positions as
chart entries, Top 40 hits, Top 10s and number ones, and `CompleteChartRun` applies the singles flop rules (peak 0 or over
60) and momentum penalties ([SimulatedArtist.cs:320-347](../Data/SimulatedArtist.cs:320)). Recognition gains follow the
same events. But the album chart is 40 slots until April 1961, 50 until August 1963, then 150, then 200
([ChartManager.cs:1572](../Systems/ChartManager.cs:1572)).

Album chart peaks in `lpafull-ctl-1001-260`, scored by the ladder:

| Year | Top 40 hit | 41-60 neutral | 61-100 flop (−0.10) | 101-200 flop (−0.15) | Flop share |
|---|---:|---:|---:|---:|---:|
| 1960 | 212 | 0 | 0 | 0 | 0% |
| 1962 | 168 | 33 | 0 | 0 | 0% |
| 1963 | 166 | 50 | 91 | 98 | 47% |
| 1964 | 176 | 85 | 130 | 162 | 53% |

Through 1960, every charting LP is a "Top 40 hit". From August 1963, half of charting LPs are flops that reset
`consecutiveHits`. That breaks the Established-to-Star rule (three consecutive hits plus a #1,
[SimulatedArtist.cs:360](../Data/SimulatedArtist.cs:360)) for any act with an album programme, and two in a row demote a
Star. The change in 1963 reflects chart size, not performance.

Since proven acts make 68-78% of their releases LPs late in the decade (A3), this rule likely blocks the stars the game
wants. An album-chart #1 also counts the same as a Hot 100 #1 for Superstar.

**Suggestion.** Score album runs against the album chart's own scale (percentile of chart size, or separate album
milestones), and keep Hot 100 milestones for singles. Decide explicitly how much an album #1 counts toward stardom.

### B2. A never-charting record costs less momentum than a charting one

**Defect.** `CompleteChartRun` applies the momentum penalty only when `peakPosition > 40`, and a never-charted run has peak
0 ([SimulatedArtist.cs:341](../Data/SimulatedArtist.cs:341)). A single that peaks at #75 costs −0.10 momentum; one that
never charts costs nothing. An LP peaking at #120 costs −0.15. Momentum feeds launch awareness at 0.5x, so the penalty
schedule is inverted for the most common outcome.

**Suggestion.** Treat peak 0 as the worst bucket. Momentum is also the only "hot now" stock and never decays
([[artist-momentum-never-decays]]), so the two fixes belong together.

### B3. Distribution advances are never repaid

**Defect, measured.** A signing moves `advance` from distributor cash to client cash, and `unrecoupedAdvance` is decremented
only out of the distributor's own skim ([CompetitorManager.cs:1422](../Systems/CompetitorManager.cs:1422)). The skim is
charged at the same rate whether or not the advance is recouped, and nothing claws back the remainder at exit. The advance
is therefore a gift.

Reference decade: 1,151 advances totalling $1.95M. Of that, $1.66M came from distributor courting, mostly by Majors
(median $4.8k, concentrated in 1967-69). A single courted advance is about the top of a Small label's entire opening-cash
range ($1k-8k). The player can take the same gift through
`PursueDistributionDeal`, and the Rolodex tells them "You still owe him $X of the advance"
([RolodexDirectory.cs:211](../Systems/RolodexDirectory.cs:211)), which is never collected.

**Suggestion.** Recoup from the client's share: withhold the client's net until `unrecoupedAdvance` reaches zero, as the
artist recoupment path already does. Measure the courting route afterwards; dependent labels' survival was tuned with the
gift in place.

### B4. Royalty rates run backwards by tier

**Ahistorical.** Base royalty rates are Major 3%, MidTier 5%, Independent 8%, Small 10%, plus career leverage up to 8 points
([AILabel.cs:370](../Data/AILabel.cs:370)). In period practice, majors paid established acts the most and small
independents the least, often nothing beyond a flat fee. Because artist choice scores royalty at weight 0.15
([RosterManager.cs:872](../Systems/RosterManager.cs:872)), small labels win contested acts on royalty, and artists'
earnings (member wealth, lasting fame) run highest at the weakest labels.

**Suggestion.** Flatten or invert the tier base (indies 2-5%, majors 3-5% rising with leverage), and let the big label's
pull come from promotion and distribution, as `ContractPricing` already argues for advances.

### B5. Marketing money buys almost nothing; push is a fixed label trait

**Ahistorical.** `BudgetToImpact` normalizes each label's spend by its own tier baseline before a log
([CompetitorManager.cs:3971](../Systems/CompetitorManager.cs:3971)). On the spend term, a Major's $3,000 campaign is
worth about 1.24x a Small label's $150; the rest of the launch gap comes from fixed label attributes. Weekly radio push is `GetCampaignImpact(label)`, a fixed function of
`marketingPower` and `budgetLevel` applied identically to every record the label releases
([ChartSimulator.cs:971](../Systems/ChartSimulator.cs:971), [:1106](../Systems/ChartSimulator.cs:1106)). The only
record-level lever is the top-20/momentum hold.

So a label cannot "work" a breaking record harder, a star's 2.5x budget mostly disappears into the log, and cash, which
established labels pile up, has almost no use. That removes the main way money should turn into fame.

**Suggestion.** Make campaign impact a function of absolute spend relative to market size, with a per-record budget the
AI can raise for a record that is breaking (or for a proven act) and cut for a weak one. That gives release judgement a
second decision and gives celebrity a cost.

### B6. AI-to-AI publishing on covers pays about 5x the period mechanical

**Ahistorical.** With routing on, which is the default despite the "OFF by default" comment
([PublishingRoutingService.cs:17](../Systems/PublishingRoutingService.cs:17) vs [:27](../Systems/PublishingRoutingService.cs:27)),
a cover pays the controlling label 11% of list (about 9.8c a copy,
[CompetitorManager.cs:13](../Systems/CompetitorManager.cs:13)). The AI also expenses a 4c two-sides mechanical on every
sold single, paid to nobody ([:1098](../Systems/CompetitorManager.cs:1098)). Player-controlled songs are already
corrected to 2c. AI catalogs are therefore worth several times the period figure, and a cover's composition is paid
twice.

**Suggestion.** Route the period mechanical (2c a side) between AI labels and drop the separate 4c expense on routed
songs. The "goldmine" comment already flags 0.11 for re-derivation.

### B7. Re-signing forgives the unrecouped balance

**Ahistorical.** A renewal sets `unrecoupedAdvance = newAdvance` ([RosterManager.cs:1388](../Systems/RosterManager.cs:1388)),
wiping production charges carried from the old deal. Period contracts cross-collateralized. This makes
`HasRecoupedCurrentContract`, the drop exemption, easier to reach for acts on their second deal, and it inflates their
royalty earnings.

---

## C. Smaller items

- **Album launch reserve.** `ProcessDueAlbumProjects` caps album marketing against monthly overhead only. Every other launch
  site uses the commitment reserve plus the initial-run reserve ([CompetitorManager.cs:3834](../Systems/CompetitorManager.cs:3834)).
- **Price constants.** AI single list is $0.89 while the player's is $0.98, so the AI's dealer share is 71% on singles and
  64% on LPs. Stereo LPs, flagged and ramped by year, carry no price premium.
- **Double studio modifier.** `GenerateRecordFromArtist` applies `studioMod` twice, as a blend and then a multiplier
  ([:3441](../Systems/CompetitorManager.cs:3441)), keyed to the label's HQ region. Across the authored regions (0.83-1.15)
  an act of base quality 0.60 records at 0.53 or 0.80 depending on where its label is headquartered.
  `AiRepertoireService` mirrors the formula, so it is at least consistent. Confirm it is intended.
- **Self-referential heat.** `CalculateArtistHeat` counts a record's own current position into its act's heat, which feeds
  its radio target. That overlaps the radio position bonus ([ChartManager.cs:600](../Systems/ChartManager.cs:600)).
- **Release capacity (known).** Majors get 2-4 releases a month for 30-60 acts ([AILabelFactory.cs:241](../Systems/AILabelFactory.cs:241)),
  under one release per act-year, against history's stars at 4.7 singles plus 2.5 LPs. Covered by
  [[hit-acts-release-too-little]] and the `--hit-act-cadence` probe.
- **National awareness clamp (deferred).** The 0.60 national single-awareness clamp
  ([ChartManager.cs:2228](../Systems/ChartManager.cs:2228)) discards a known act's launch awareness after week one, the
  lowest week of the release ramp. Already deferred in [CelebrityAuditProgress.md](CelebrityAuditProgress.md).
- **Stale memory note.** [[small-labels-never-lose-money]] says `--dealer-price-pressing` is off by default; it is on
  ([LabelPressingEconomics.cs:30](../Systems/LabelPressingEconomics.cs:30)).

## Not covered

Genre acceptance and demand sizing (`GenreAcceptanceService`, `MarketRegion`), the station network, regional audience,
media, the player desk's own economics, and the live/scene economy. The parallel thread's uncommitted genre and album
files were deliberately skipped.

## Reproduction

```powershell
& "$env:LOCALAPPDATA\Programs\Python\Python314\python.exe" -I SimTools\analyze_economy_audit.py `
  SimLogs\dec-awrw-on-1001-522 SimLogs\lpafull-ctl-1001-260-album-chart.csv
```

It needs a full-telemetry run (format-memory-revisions, release-outcomes, a3-economic-decisions, release-strategy, deal-ledger).

## Follow-up: B1 and B2 behind `--album-scale-career-milestones` (October 20, 2026)

Branch `claude/album-scale-career-milestones`, based on `media-atmosphere` `cdf05fd`. The switch is off by default
([AlbumScaleCareerMilestones.cs](../Systems/AlbumScaleCareerMilestones.cs)) and configured in `ChartManager._EnterTree`.

**B1 rule.** An album chart position is converted to its singles equivalent by its percentile of that week's album
chart: `ceil(position x 100 / size)`, clamped to 2..100. #1 stays #1. The run's best equivalent is stored on the
record (`albumCareerPeak`, saved) and replaces the raw peak in every career read: chart entry, Top 40, Top 10, #1,
the flop streak, the momentum penalty, contract evidence, recognition milestones and run sustain. The hit-cadence
and senior-cooling clocks follow too, because they hang off `RegisterTop40Hit`. Album thresholds become top 40%
for Top 40 (#16/40, #20/50, #60/150, #80/200), top 10% for Top 10 (#4, #5, #15, #20) and bottom 40% for a flop (#25+,
#31+, #91+, #121+). Albums and singles still share one ladder. Keeping them apart would freeze the late-decade acts
whose releases are 68-78% LPs (A3).

**Album #1 decision.** An album #1 counts as a full #1 toward Star (one) and Superstar (four). The album chart has
exactly one #1 a week at any depth, the same scarcity as the Hot 100. The conversion removes the depth bias below #1,
not the top slot.

**B2 rule.** A never-charted single takes −0.15 momentum, the schedule's worst bucket. A #75 single keeps −0.10. A
never-charted album takes no momentum penalty: at 40-50 slots most LPs never chart. It still counts as a flop for the
streak, as before.

**Checks.** `--album-scale-career-milestones-check` (`SimTools/AlbumScaleCareerMilestonesChecks.cs`, through
`SaveLoadRoundTripRunner`) passes. It covers off identity, the conversion table, the run's best peak across a size
change, an album #1 plus two upper-half LPs making a Star (off: no Star), 1960 #30-of-40 as a flop, the cadence clock,
and B2's buckets. `--hit-act-cadence-check` and `--senior-cooling-check` still pass.

**Off is inert.** 156 weeks, seed 1001, `--enable-genre-market-v2 --enable-artist-population-lifecycle`, full
telemetry: 86 of 86 CSVs are byte-identical to `cdf05fd`. The branch adds two observe-only CSVs,
`<run>-career-ladder.csv` (weekly career-state census and rung moves) and `<run>-chart-breadth.csv` (distinct acts
per year).

**Short run, off vs on** (`asc-off-1001-156`, `asc-on-1001-156`). The census is taken at the last week of each year.
Moves are counted over the year.

| Year | Rising | Established | Star | Superstar | → Star | → Superstar | Star → Established | Superstar → Star |
|---|---|---|---|---|---|---|---|---|
| 1960 | 252 / 213 | 107 / 87 | 24 / 23 | 8 / 6 | 5 / 4 | 2 / 0 | 4 / 6 | 0 / 0 |
| 1961 | 202 / 182 | 111 / 78 | 23 / 17 | 6 / 8 | 9 / 9 | 0 / 3 | 7 / 11 | 1 / 1 |
| 1962 | 165 / 171 | 107 / 75 | 23 / 14 | 9 / 8 | 9 / 7 | 4 / 1 | 6 / 7 | 1 / 0 |

| Year | Hot 100 acts | Top 40 acts | Top 10 acts | Album chart acts |
|---|---|---|---|---|
| 1960 | 588 / 563 | 210 / 213 | 78 / 79 | 130 / 140 |
| 1961 | 637 / 620 | 224 / 219 | 86 / 88 | 153 / 147 |
| 1962 | 611 / 617 | 221 / 230 | 85 / 86 | 139 / 155 |

Album runs in the off arm (by first chart year): Top 10 / 11-40 / 41-60 / flop on singles thresholds against the album
scale:

| Year | Singles thresholds | Album scale |
|---|---|---|
| 1960 | 77 / 121 / 0 / 0 | 40 / 65 / 31 / 62 |
| 1961 | 75 / 93 / 29 / 0 | 46 / 60 / 35 / 56 |
| 1962 | 64 / 97 / 28 / 0 | 42 / 57 / 35 / 55 |

**Reading.** A 156-week run ends in December 1962, so it never reaches the 150-slot chart of August 1963. It
exercises only the half of B1 in which every early LP was a hit. Removing that half takes about 30% of 1960-62 LP runs
from "hit" to "flop". By the end of 1962, Established falls from 107 to 75 and Star from 23 to 14, and Star demotions
rise from 17 to 24 over three years. Superstar is flat (9 vs 8). Chart breadth barely moves (Top 40 acts within ±9 a
year, album chart acts +16 in 1962). The half of B1 the switch was written for (half of LPs scored as flops from
August 1963) is unmeasured here. A 1963-65 window, a resumed world or a five-year run, is needed to see it. B1 and B2
are not separated in this run: one on/off pair cannot attribute the Established loss between the percentile flops and
the −0.15 on never-charted singles.

**Open items.** `CompetitorManager.OnRecordChartUpdated` still credits label `top40Hits`/`numberOneHits` from raw
album peaks (label success rate, not the artist ladder). `ArtistEvolutionService` and `AlbumLegitimacyService` read the
raw album peak in the cultural reads. Neither was in scope.

### 1963-65 window, two seeds (October 20, 2026)

Each seed ran to a January 1963 world with the switch off (`asc-<seed>-1963`). That world was then resumed twice for 156
weeks, off and on, so both arms start from the same act population. The on arm applies the rules from 1963 only.
Seeds 1001 and 2002 use the canonical flags and full telemetry. The census is taken at the last week of each year;
moves are summed over 1963-65. Cells are off / on.

| Seed | Year | Rising | Established | Star | Superstar | Top 40 acts | Album chart acts |
|---|---|---|---|---|---|---|---|
| 1001 | 1963 | 159 / 176 | 99 / 102 | 21 / 22 | 9 / 8 | 226 / 222 | 324 / 345 |
| 1001 | 1964 | 133 / 177 | 86 / 87 | 18 / 27 | 13 / 11 | 234 / 226 | 447 / 478 |
| 1001 | 1965 | 132 / 184 | 70 / 84 | 21 / 28 | 14 / 13 | 224 / 238 | 475 / 470 |
| 2002 | 1963 | 163 / 161 | 108 / 105 | 18 / 22 | 10 / 8 | 223 / 221 | 318 / 324 |
| 2002 | 1964 | 148 / 178 | 100 / 96 | 18 / 23 | 11 / 8 | 234 / 229 | 512 / 507 |
| 2002 | 1965 | 158 / 192 | 96 / 92 | 22 / 27 | 12 / 9 | 245 / 236 | 479 / 494 |

| Seed | → Star | → Superstar | Star → Established | Superstar → Star |
|---|---|---|---|---|
| 1001 | 24 / 32 | 8 / 6 | 16 / 20 | 2 / 1 |
| 2002 | 22 / 26 | 5 / 3 | 16 / 17 | 0 / 1 |

Album runs in the off arm, flops (peak over 60) on singles thresholds against the album scale: 1964 is 281 vs 166 of 540
(seed 1001) and 309 vs 186 of 571 (seed 2002). 1965 is 321 vs 204 and 308 vs 192.

**Reading.** In 1963-65 the switch does what B1 asked, on both seeds. About 40% of charting LPs stop being scored as
flops. Star rises from 21 to 28 and from 22 to 27 by the end of 1965. Promotions to Star rise (+8, +4) by more than
Star demotions (+4, +1). The largest move is Rising, +52 and +34 by 1965: fewer flop streaks means fewer Rising acts
fall to Declining. Superstar is slightly lower on both seeds (−1 and −3 at 1965; −2 and −2 promotions). That is a
handful of acts, but the sign repeats. A candidate cause is B2's −0.15 on never-charted singles, which cuts the
momentum of the acts that chain #1s. The pair cannot separate B1 from B2. Chart breadth does not move beyond noise
(Top 40 acts +14 and −9 in 1965).

Taken with the 1960-62 run: before August 1963 the switch removes free hits and lowers the ladder. After it, the
switch removes size-driven flops and raises it. The decade A/B at audit end decides the net effect.
