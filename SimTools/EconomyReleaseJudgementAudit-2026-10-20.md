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

---

## Follow-up note: A1 maturity curve (October 20, 2026)

Branch `claude/zealous-moore-7e4d72`, commit on top of `b48427b`. **Switch:** `--format-memory-maturity-curve`, off by
default ([FormatMemoryMaturityCurve.cs](../Systems/FormatMemoryMaturityCurve.cs)). When on, `UpdateResponsiveMemoryObservation`
and `GetProjectComponentOutcomeDelta` replace linear maturity with an empirical cumulative-label-net curve. It drives the
divisor (the projected outcome), the observation's `maturityWeight` and the age-matched expectation, since all three mean
"share of the lifetime earned so far".

**Curve.** Medians of lifetimeLabelNet(age) / lifetimeLabelNet(final) for finalized, memory-eligible releases of
`dec-awrw-on-1001-522`, where lifetimeLabelNet(age) = realizedNetToDate + sunkProductionCost (revisions joined to
release-outcomes). Interpolation is linear between knots, from (−1, 0), and closes at 1.0 near the median final age.

| Format | Age 13 | Age 26 | Age 52 | Closes at 1.0 | Linear at 13 / 26 / 52 |
|---|---:|---:|---:|---:|---|
| Album (n 30,368) | 0.534 (IQR 0.50-0.57) | 0.807 | 0.972 | 78 | 0.269 / 0.519 / 1.0 |
| Single (n 58,120 pooled) | 0.750 | 0.973 | — | 34 | 0.70 / 1.0 |

Soundtracks and cast albums decay more slowly and are under-projected by this curve; they are a small share of albums.

**Off is inert.** All 84 output CSVs of a 52-week seed-1001 off run are byte-identical to the same run on `b48427b`.
**Check:** `SimTools/FormatMemoryMaturityChecks.tscn` (8 checks, pass). A synthetic album (AlbumSimulator's flat-to-week-8,
then 0.93 a week shape, 0.75 awareness ramp, 23% sunk) projects within −1.9% / +1.8% / +0.7% of its final at ages 13 / 26 / 52.
Linear projects 2.24x. With no awareness ramp the error is +13%.

**Short run.** 156 weeks, seed 1001, `--enable-genre-market-v2 --enable-artist-population-lifecycle`, full telemetry.
Runs `SimLogs/fmc-{off,on}-1001-156`.

Provisional estimate against the release's Final (1a; finalized releases only):

| Lane | Age | Off: median est / final | On | Off: share over | On |
|---|---:|---:|---:|---:|---:|
| AlbumComponent | 13 | 1.85 | 0.91 | 100% | 20% |
| AlbumComponent | 26 | 1.52 | 0.96 | 100% | 20% |
| AlbumComponent | 52 | 0.97 | 1.00 | 0% | 34% |
| AlbumWithPromo (vs its Final estimate) | 12 | 1.26 | 0.96 | 99% | 20% |
| AlbumWithPromo (vs its Final estimate) | 25 | 1.20 | 0.99 | 99% | 37% |
| OrphanSingle | 13 | 1.06 | 0.98 | 81% | 42% |
| OrphanSingle | 26 | 0.97 | 1.00 | 0% | 51% |

AlbumComponent decision-time residual, live blend against final-only (1b, pooled, recency × maturity weights):

| Week | Off live | Off final-only | Off gap | On live | On final-only | On gap | Provisional weight off / on |
|---:|---:|---:|---:|---:|---:|---:|---|
| 52 | +2.25 | +1.55 | +0.70 | +1.58 | +1.53 | +0.05 | 99% / 99% |
| 78 | +1.91 | +1.40 | +0.50 | +1.45 | +1.40 | +0.06 | 92% / 94% |
| 104 | +1.74 | +1.32 | +0.42 | +1.45 | +1.32 | +0.13 | 76% / 81% |
| 130 | +1.65 | +1.22 | +0.43 | +1.34 | +1.20 | +0.14 | 72% / 77% |
| 156 | +1.58 | +1.14 | +0.45 | +1.22 | +1.15 | +0.07 | 74% / 79% |

The AlbumWithPromo lane's gap at week 156 falls from +0.17 to +0.01. The OrphanSingle lane already tracked its finals and
is unchanged.

Album share of format decisions (a3-economic-decisions): 1960 38.5% → 38.5%, 1961 42.0% → 41.1%, 1962 49.1% → 50.7%;
all three years 43.2% → 43.5% (n 15,626 / 15,962). Early-decade albums genuinely beat their prior (final-only +1.1 to
+1.5), so removing the inflation barely moves the early mix. Provisional rows now also carry more weight (album age 13
0.53 instead of 0.27), which raises confidence. The test that matters is 1966-69, where the finals are negative and the
off residual is positive. That is for the end-of-audit decade A/B.

**Separate finding (not fixed).** The AlbumWithPromo lane's estimate, Final rows included, sits about $40k (median)
above the project's realized net: estimate = projectedProjectNet + Σ(component actual − component projection). So
whatever projectedProjectNet carries beyond its two components (the promo synergy credit, A5) is never tested against
outcomes. Compared with its own Final estimate, the lane is now unbiased. Compared with realized net, it still runs 2.1x.
