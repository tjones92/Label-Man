# Economy and release-judgement audit

**October 20, 2026 — original report.** Branch `media-atmosphere`, HEAD `a58b509`. For that report, no game code was changed and no new
simulation was run. The uncommitted edits to `Data/GenreCatalog.cs`, `Data/MarketRegion.cs` and the album-practice files
belong to the parallel "1965-69 singles research and Jazz allocation" thread and were not reviewed.

The question: which ahistorical balancing and which defects in the economic code hold back (1) AI label release
judgement and (2) an ecosystem in which celebrity can form? This complements
[CelebrityEconomyAudit-2026-10-15.md](CelebrityEconomyAudit-2026-10-15.md). Its findings (opening careers, regional sync,
media, the genre chart guard, the album tier multiplier, the two cash identities) are not repeated here.

**Integration status.** A1 (`claude/zealous-moore-7e4d72`, through `a147251`) and B1/B2
(`claude/album-scale-career-milestones`, through `ee324f0`, in the `eager-wiles-1cfa05` worktree) are now merged
into `media-atmosphere` as `e8abf7e` and `41f6f02`. Both switches remain off by default. Their measurements and
limitations are retained in the follow-ups below. A2 has an opt-in recalibrated prior (`87a9ed9`). A3 now has an
opt-in career-banded memory experiment; its album-prior calibration and debut-LP economics remain open.
B1's missing-promotion investigation is deferred as recorded below; its exact quarterly gate tracing is ready.

**Deferred follow-up — missing MidTier promotions (user decision, October 20).** Return to B1's unexplained
promotion loss later; it does not block continuing A2 and the remaining audit items. Resume the same archived
1963 worlds in matched off/B1 arms with `--mid-tier-promotion-audit`, then run
`analyze_mid_tier_promotions.py`. Inspect owned reach, dependency and the four-quarter capability streak at the
actual decision. Preserve the placebo/noise caveat and the correction that the added readers did not cause the
MidTier drop. Keep memory available for normal computer use; use at most two concurrent early-decade seeds and
reduce concurrency when free memory is low. Do not substitute incomplete runs for completed matched evidence.

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

---

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

### The four raw-peak readers moved onto the album scale (October 20, 2026)

Four more readers now take `AlbumScaleCareerMilestones.CareerPeak` when the switch is on. Before, each treated an
LP past #100 as uncharted.

- **Label hit credit** (`CompetitorManager.OnRecordChartUpdated`): label Top 40s and #1s, and the charted-label set
  the consolidation gate reads. Label Top 40s feed the health score, label momentum (distribution-deal triggers and
  signing buzz) and the player's contract outlook.
- **Landmark recognition** (`AlbumLegitimacyService`): the commercial term of the landmark album read.
- **Critical acclaim** (`ArtistCriticalAcclaimService`): the commercial score. Acclaim feeds the landmark read and
  evolution pressure.
- **Evolution era tally** (`ArtistEvolutionService`): discography text only.

`CulturalMemoryService` is singles-only and was left alone. The check suite adds a #120-of-150 LP earning commercial
recognition and acclaim on (0 off).

**Off stays inert.** Resumes of the same 1963 worlds: 88 of 88 CSVs byte-identical to the earlier off arm on both
seeds.

**1963-65, same worlds, off / on (readers included).** Values are at the end of 1965 unless a range is given.

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Star | 21 / 27 | 22 / 23 |
| Superstar | 14 / 15 | 12 / 8 |
| Established | 70 / 93 | 96 / 105 |
| Label tiers: MidTier / Major | 44 / 36, 8 / 9 | 43 / 32, 7 / 9 |
| Promotions to Major, 1963-65 | 0 / 1 | 0 / 2 |
| CR4 chart share, 1965 | .175 / .206 | .221 / .256 |
| Major-family chart share, 1965 | .586 / .572 | .558 / .538 |
| Ratified genre conversions | 88 / 74 | 63 / 57 |

The career rows differ from the earlier on arm because the label and acclaim readers change label decisions, and the
run diverges from there. These are full-switch on/off differences, not isolated reader effects. In particular, the
ladder-only arm already lowered MidTier to 36 and 35: attributing that drop to the added readers was incorrect.
The later ablation below withdraws that attribution. Its near-zero perturbation also moves Star and Established
by amounts comparable to the apparent gains here, so these two seeds do not establish a career-population benefit.

**Landmarks are unmeasured.** One seed-1001 pair with `--enable-album-legitimacy --enable-cultural-memory` published
no landmark album in 1963-65 in either arm (legitimacy 0.0). Breakthrough-hit events were 253 off and 259 on. The
recognition read is not the binding gate in this window. Earlier decade runs placed landmarks mostly in 1965-69 under
the canonisation cap. Measuring this reader needs a decade run with those flags.

### What lowers the MidTier count (October 20, 2026)

Same 1963 worlds, 156 weeks, two seeds. A diagnostic sub-switch, `--album-scale-career-milestones-part=b1|b2|placebo`
(main switch required), runs one half of the change. The placebo runs the B2 code path at −0.001 instead of −0.15:
the run diverges the same way, but the change is too small to matter.

| End of 1965 (seed 1001 / 2002) | off | placebo | B1 only | B2 only | both |
|---|---|---|---|---|---|
| MidTier labels | 44 / 43 | 41 / 39 | 31 / 31 | 35 / 37 | 36 / 32 |
| Promoted into MidTier | 33 / 32 | 29 / 29 | 18 / 26 | 30 / 28 | 29 / 31 |
| Demoted, started 1963 as MidTier | 11 / 9 | 12 / 9 | 10 / 11 | 8 / 8 | 9 / 10 |
| Demoted, joined during the window | 5 / 4 | 3 / 5 | 4 / 7 | 13 / 7 | 10 / 11 |
| Star | 21 / 22 | 17 / 22 | 20 / 21 | 14 / 24 | 27 / 23 |
| Established | 70 / 96 | 65 / 81 | 98 / 102 | 69 / 71 | 93 / 105 |

**Findings.**

1. **Not the readers.** The ladder-only arm (before the label/landmark/acclaim/era readers) already ended at 36 and 35
   MidTier labels. The earlier attribution of the tier shift to the label-credit reader is withdrawn.
2. **About a third is divergence noise.** The placebo alone drops MidTier by 3-4. Both halves still sit 4-10 below the
   placebo on both seeds. B1 alone is −10 and −8, B2 alone −6 and −2.
3. **Every MidTier demotion, in every arm, is the charting exit** (under 4 charting records in 52 weeks, two quarters
   running). None is capability or losses. The demotion count includes LPs.
4. **The labels lost are recent joiners, not the established MidTier.** Demotions among labels that were MidTier in
   January 1963 do not move (8-12 in every arm). That fixed cohort charts about the same singles and more LPs with
   the switch on (+33 and +19 LPs). The extra demotions are Independents promoted during the window and demoted again
   39-78 weeks later. They enter at 4-6 charting records against a promotion bar of 5 (4 + 1 on the dependent route)
   and an exit below 4. In the ladder-only arm, the joiners demoted only with the switch on charted as many singles
   as they did in off (86 vs 87, 81 vs 84) but fewer LPs (50 vs 62, 45 vs 66). That is the record that drops them.
5. **B2's route is joiner demotion** (13 vs 3 placebo on seed 1001; 7 vs 5 on seed 2002). **B1's route is not
   consistent**: fewer promotions in on seed 1001 (18 vs 29), more demotions on seed 2002 (18 vs 14). For B1 on seed
   1001, the promotion gate's charting, cash, profit, runway, status and capability conditions all pass as often as
   off (88 vs 90 qualifying label-quarters), and Independent rosters are the same size. The binding condition behind
   the 18 promotions is not found in the logged fields. Owned reach, dependency and the four-quarter capability streak
   are not logged per label.

**Reading.** MidTier is a thin band between a promote-at-5 and an exit-under-4 charting bar, and its new members
regress to their base rate within a year. Small redistributions of chart entries between labels change its
headcount, including the redistribution that a random-stream divergence alone causes. The switch's own effect is
probably real (all six treated arms sit under the placebo) but carries one placebo draw per seed. The ladder counts
carry the same caution: the placebo moves Star by −4 and Established by −5 and −15, about as much as the earlier
reported Star gains. Those gains are not distinguishable from divergence at two seeds.

### Continuation: exact quarterly promotion evidence (October 20, 2026)

`--mid-tier-promotion-audit`, with artist population lifecycle enabled, now writes
`<run>-mid-tier-promotion-gates.csv`. Every Independent review is recorded at the actual quarterly decision,
including failed reviews. The row contains operating months, recent charting records, roster size, owned reach,
distribution dependency, capability, the sustained-capability streak, status, consecutive loss months, last
monthly profit, cash and overhead, together with the unchanged gate's actual eligibility result.

The observation occurs after the quarterly capability-streak update and before promotion resets the streak or
changes the tier, and before attribute drift and later daily activity. This matters because a week-end finance
snapshot cannot establish the inputs the quarterly test saw. No gate, threshold or random draw changed.

[analyze_mid_tier_promotions.py](analyze_mid_tier_promotions.py) streams those rows using the Python standard
library. It reports first blockers and all overlapping failures, also isolating reviews with at least five
charting records. It verifies reconstructed eligibility against the recorded decision and checks a one-to-one
match with actual Independent-to-MidTier transitions. Floating-point fields are read back as C# float32.
For example:

```powershell
& "$env:LOCALAPPDATA\Programs\Python\Python314\python.exe" -I SimTools\analyze_mid_tier_promotions.py `
  SimLogs\promotion-off-1001 SimLogs\promotion-b1-1001 --output SimLogs\promotion-gate-review.json
```

**Validation.** The merged solution builds with zero warnings and errors. A1's eight maturity checks and the
album-scale, hit-cadence and senior-cooling suites pass. The new `--mid-tier-promotion-audit-check` through
`SaveLoadRoundTripRunner.tscn` verifies both a blocked review and a successful promotion: the decision observer
sees the four-quarter streak while the label is still Independent, and the transition observer sees it reset.
Analyzer fixtures accept matched decisions and reject missing or unexplained promotions.

**Run limits.** To leave memory for ordinary computer use, subsequent checks used a 256 MB managed heap. A 104-week early
seed-1001 trace attempt was stopped by its free-memory guard during 1961. A separate attempt to load the archived
1963 B1 world was stopped by the same guard before any resumed chart week. Both are incomplete attempts, excluded
from outcome comparisons. Two planned 52-week smoke runs were not started after the available-RAM preflight
reported 859 MB free. Logs and invocation manifests are under `SimLogs/audit-integration-20261020/`.
The fixed checks validate the new trace, not the missing-promotion explanation. That still needs completed,
matched off/B1 windows with the trace enabled; no new causal attribution or default adoption is made here.

## Follow-up: A2 current-economy single prior (October 20, 2026)

**Switch:** `--single-prior-recalibration`, off by default
([SinglePriorRecalibration.cs](../Systems/SinglePriorRecalibration.cs)). It applies only to live genre-market
decisions, requires dealer pricing and period single costs, and leaves the prewarm/disabled calculation intact.
The same prior feeds the orphan and promo paths, with their existing separate memory lanes. The memory ceiling
remains 0.65. Marketing and the pass/wait decision remain A4 work.

**Fit.** [calibrate_single_prior.py](calibrate_single_prior.py) joins `calibration-decisions`,
`a3-economic-decisions` and `release-outcomes`. Training uses the reference `dec-awrw-on-1001-522`, standalone
single launches through week 418: **19,897 launches, all finalized**. The 104-week lag avoids a cohort that has
observed quick failures but not its long-lived hits. The 317 Declining launches are excluded from fitting;
the game's existing New/Unsigned fallback is retained for that state. Promo outcomes are not mixed into the
standalone fit. The script refuses an incomplete training or holdout cohort.

The target is `(realizedNet + release-time production cost) / (reach × genre market factor)`, at the booked-price
margin. Twelve quality bins concentrate resolution around 0.60-0.70. Weighted isotonic regression constrains
contribution to be nondecreasing in quality and career; empty cells receive the pooled quality-bin mean at
weight one. Linear interpolation between the bins' mean qualities removes the old Q3/Q4 cliff. Low-quality
Star cells still have sparse or no direct evidence; monotonic constraints prevent them penalizing Stars relative
to Established acts, but do not manufacture observations.

The table is already priced at the current margin ($0.4055 under the prior's existing 5% royalty approximation).
It is repriced relative to that reference, without applying the old counter-to-dealer discount a second time.
Production cost is subtracted once, after repricing.

**A correction in interpretation.** An expected-money prior must be fitted to arithmetic means, not medians.
Hits have a long earnings tail. The original A2 median comparisons demonstrate stale scale and the career
inversion, but their 2-13x ratios overstate the error in the expected value. On the mature reference training
cohort the old band-level mean projections are about 2.0-3.9x realized, rather than 2-13x.

**Separate-seed holdout:** `dec-awrw-on-2002-522`, same mature cohort rule, 18,413 completed launches.
Errors compare total projected net with total realized net within each band; these are descriptive point
estimates, not confidence intervals.

| Career band | n | Mean realized | Old mean prior | New mean prior | Old error | New error |
|---|---:|---:|---:|---:|---:|---:|
| New/Unsigned | 16,446 | 7,498 | 18,824 | 6,910 | +151% | −8% |
| Rising | 1,234 | 14,820 | 58,402 | 15,152 | +294% | +2% |
| Established | 564 | 20,567 | 92,396 | 24,034 | +349% | +17% |
| Star/Superstar | 169 | 59,202 | 103,363 | 48,929 | +75% | −17% |

**Policy transfer remains imperfect.** `dec-lpa4-youth-{1001,2002}-522` are additional evaluations, not training.
The new prior underpredicts proven acts there: Rising by 36-39%, Established by 29-37%, and Star by 31-47%.
On experimental seed 2002, the old Star mean happened to be close (+2%), while the new one is low (−47%).
Mean absolute error improves in every career band in all three holdouts, but that does not establish unbiased
expectations across policies. A2 therefore remains an opt-in candidate. The inputs and outcomes still need the
conditioning work in A3, and the shared prior is not an independently validated promo-single forecast.

**Short live check.** Matched 26-week off/on runs on seeds 1001 and 2002; A1's maturity curve is enabled in
both arms, B1/B2 is off, and all runs use the same compiled assembly and canonical population/genre flags.
These are smoke tests, not completed-year or decade acceptance tests. Cells are off / on.

| Metric | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Orphan single launches | 1,944 / 1,532 | 1,774 / 1,441 |
| Promo single launches / LP projects scheduled | 813 / 1,138 | 741 / 999 |
| LP share of format decisions | 29.5% / 42.6% | 29.5% / 40.9% |
| Total single launches | 2,757 / 2,670 | 2,515 / 2,440 |
| Distinct Hot 100 acts | 368 / 365 | 357 / 348 |
| Distinct Top 40 acts | 131 / 134 | 128 / 125 |
| Distinct album-chart acts | 102 / 102 | 93 / 95 |

Lower single expectations move the fork toward LPs; single volume falls about 3%, because most extra LP projects
still launch a promo. The short window does not establish a long-run improvement. In particular, the underlying
advantage of unknown acts' LPs and pooled career memory remain A3 problems.

**Validation and memory.** Build: zero warnings/errors. Three Python calibration tests pass (weighted pooling,
supported-surface recovery, mature-cohort accounting and duplicate rejection). Nine in-engine checks pass,
including continuity, career ordering, prewarm/off identity and single subtraction of production cost. The
authored C# table matches the generated fit. All four live smoke runs completed. They ran serially with a
192 MB managed-heap cap; peak process working set was 347-372 MB and the lowest observed free RAM was 638 MB.

Reproduce the fitting/evaluation evidence without a new simulation:

```powershell
& "$env:LOCALAPPDATA\Programs\Python\Python314\python.exe" -I SimTools\calibrate_single_prior.py `
  SimLogs\dec-awrw-on-1001-522 --holdout SimLogs\dec-awrw-on-2002-522 `
  --holdout SimLogs\dec-lpa4-youth-1001-522 --holdout SimLogs\dec-lpa4-youth-2002-522 `
  --output SimLogs\single-prior-a2-20261020\fit.json --csharp SimLogs\single-prior-a2-20261020\table.txt
```

Evidence, input hashes and run manifests live under `SimLogs/single-prior-a2-20261020/` (untracked run artifacts).

## Follow-up: A3 memory conditioned on career (October 20, 2026)

**Switch:** `--career-banded-format-memory`, off by default
([CareerBandedFormatMemory.cs](../Systems/CareerBandedFormatMemory.cs)). It requires the live genre market.
It fixes the pooled-career memory portion of A3. The album prior's stature calibration and the market advantage
of unknown acts' LPs are not changed by this experiment.

Each observation can now carry its original decision's career cohort. A decision reads residuals and confidence
from that cohort within the existing label/estimator lane. Cohorts 0-3 are New/Unsigned, Rising, Established and
Star/Superstar. Cohort 4 keeps legacy fallback states such as Declining from teaching the debutant cohort;
their prior's existing fallback is unchanged. There is no pooled-residual fallback when a cohort has no evidence:
confidence is zero and its prior remains in charge. Recency, maturity, residual limits and the 0.65 ceiling stay
as before. Cohort caches are separate, including in the global project lane, so alternating career groups do
not repeatedly scan the whole project history.

**Ex-ante context.** Standalone singles and later LP extractions use their own launch career. A delayed album
and its promo use `careerStateAtSchedule`, the context that priced the project, even if the act rises before
the LP drops. A later revision does not classify an old release from the artist's current state. Old untagged
observations are excluded from cohort reads; a fresh revision can tag active records from their stored launch
or project context. Fully retired old observations without that context are not assigned a guessed cohort.

**Persistence.** `FormatMemoryObservation.careerBand` is nullable and omitted when unknown, preserving the
untagged off-path schema. Worlds carrying tagged history require save v24, including after the switch is turned
off, so an older executable cannot silently discard that context. The normal untagged route retains its existing
writing version. Switching off restores pooled arithmetic while retaining the optional tags.

**Evidence.** Enabled runs add `*-format-memory-career-revisions.csv`, including in calibration mode.
[analyze_career_format_memory.py](analyze_career_format_memory.py) checks valid, stable career tags and release
weeks and summarizes the observed release/lane groups. Its residual medians describe observations, not
expected profits or the label-specific live correction.

**Validation.** Build: zero warnings/errors. Eleven in-engine checks pass: isolated cohort residuals and
confidence, cache separation, unsupported-cohort behavior, scheduled-project versus extraction context,
legacy fallback isolation, nullable-tag serialization, required save version and retained-history detection.
With the switch off, **all 84 pre-existing CSVs match the preceding A2 control byte-for-byte on both seeds**.

**Short live check.** Matched 26-week windows on seeds 1001 and 2002, A1 and A2 enabled in both arms, B1/B2 off.
All four runs completed on one assembly. These are smoke tests, not completed-year or decade acceptance tests.
Cells are off / on.

| Metric | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Orphan single launches | 1,532 / 1,528 | 1,441 / 1,424 |
| Promo single launches / LP projects scheduled | 1,138 / 1,109 | 999 / 998 |
| LP share of format decisions | 42.6% / 42.1% | 40.9% / 41.2% |
| Total single launches | 2,670 / 2,637 | 2,440 / 2,422 |
| Distinct Hot 100 acts | 365 / 366 | 348 / 336 |
| Distinct Top 40 acts | 134 / 132 | 125 / 124 |
| Distinct album-chart acts | 102 / 103 | 95 / 98 |

The on streams contain 3,280 and 3,049 valid, stable revisions. AlbumComponent evidence is very sparse for proven
acts: Established has 6 and 9 observed LPs, Star/Superstar 7 and 4, against 407 and 355 New/Unsigned LPs.
Every one of those LP observations is still provisional. The model now separates that evidence correctly;
this short window cannot establish a correction to the historical new-versus-proven LP ordering or justify a
new album stature multiplier. Sparse-cohort confidence also makes the remaining prior calibration important.

The runs were serial with a 192 MB managed-heap cap. Peak process working sets were 370-376 MB; the lowest
observed free RAM was 960 MB. Run manifests, identity hashes, smoke summaries and observed-cohort reports are
under `SimLogs/career-memory-a3-20261020/`.

**Next audit work.** Finish A3's album-prior calibration using mature LP cohorts and assess the existing
hit-led album programme against unknown acts' LP advantage, before moving to A4's marketing and pass/wait
economics. Use the mature archived outcomes before adding larger simulation windows. B1's missing-promotion
cause stays on the deferred list; this memory experiment does not reopen or resolve it.
