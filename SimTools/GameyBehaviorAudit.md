# Game-y behaviour audit: economy, fanbase and label (2026-11-12)

Response to `SimTools/GameyBehaviorAuditFableHandoff.md`.

## Method

- **Control:** `parity-v1-{2002,1001}-dp`, the adopted reference bundle (`major-scale-bundle-request.json`, 187 flags, `--label-demand-parity=1`), 104 weeks, full output. No control was rerun.
- **Scale assumed:** the model's 1960 market is about $360M at dealer prices (`company-monthly`). I took history as roughly $600M at retail, about $370M at dealer prices; that figure is mine, not from the repo. Findings below still use shares, rates and ratios.
- **Screens:** three new switches, default off, 104 weeks, seeds 2002 and 1001, `--lean-probe`. Runs are `gamey-{lp,sc,ro}-{seed}-x`.
- **Inertness:** with all three switches off, the 27-week `records.csv` (`gamey-off-2002-base`) is byte-identical to `cpw-off2-2002-base`.
- **Scripts:** analysis scripts were scratch files and are not in the repo. Each finding names the CSVs it reads.
- **Not done:** the decade A/B on one paired seed. It is owed once the author picks which fixes to adopt.

## 1. Ranked findings

Patterns are numbered as in the handoff (1 ratchet, 2 free money or stock, 3 fictional credit, 4 flat ration, 5 cliff or gate, 6 accounting timing, 7 dead field, 8 self-reinforcing loop, 9 tier-blind or code-path rule).

| # | Finding | System | Pattern | Evidence | Historical anchor | Distortion | Proposed fix |
|---|---|---|---|---|---|---|---|
| 1 | **LP sales are act-blind and never lose money.** An LP by an act whose best single sold under 1,000 copies sells a median 20–23k copies; an act with a 100k+ single sells 39–41k. No class of LP loses money (0.0–0.2%). | Economy (LP demand) | 1, 4 | `AlbumSimulator.cs:60` adds 0.004 + 0.012 × appeal to awareness every week for every LP, with no decay and no act term. Control: LPs launched weeks 1–52, both seeds (`release-outcomes`, `live-records-snapshot`, `album-projects`). | LPs followed hits; 80–90% of releases failed to recoup (Denisoff, already cited in memory). A Small-label debut LP selling 9k at 7.9× its production cost has no period counterpart. | **High.** 40–42% of LP units go to Independent, Boutique and Small labels, and these labels issue one LP per two singles. | Mechanism: discovery must be earned by the act (`--lp-earned-discovery`, screened below). |
| 2 | **LP units are spread flat; singles are winner-take-all.** Top 10% of LPs hold 32–33% of LP units and the best LP sells 204–207k in a year. Top 10% of singles hold 96% of single units and the median single sells 69–90 copies. | Economy (demand) | 4, 5 | Same files. LPs: per-title exhaustion `1 / (1 + 4 × penetration)` caps hits (`AlbumSimulator.cs:238`) while finding 1 lifts flops. Singles: intent is 6.8× the fixed purse (1,700M asked, 251M cleared, `market-clearing-weekly`) and `--singles-ration-exponent=1.35` gives the surplus to the top. | No. 1 LPs of 1960–61 sold 0.5–2M. No distribution anchor in the repo for flop singles; my estimate is 500–5,000 copies for a stiff (see open questions). | **High.** 59–65% of Independent, Boutique and Small singles sell under 100 copies, and so do 29–31% of Major and MidTier singles. A quality ≥ 0.80 single sells under 100 copies 26% of the time. | LP side: finding 1's fix plus a wider exhaustion pool for known acts (calibration). Singles side: needs a floor channel (jukebox operators, one-stops) outside the chart purse; mechanism, not screened. |
| 3 | **Solvent labels are liquidated on a hit.** 16% of bankruptcies (20 of 124, 19 of 141) happen with wholesale receivables at or above the overdraft. `label_0495` folded in week 35 with a Top 10 single, $503k gross in 26 weeks, cash −$49k and $211k owed to it. | Label (lifecycle) | 6 | `EconomicLabelLifecycle.cs:28-37`: two month-ends below zero is bankruptcy and "receivables are never treated as spendable cash". Excise and selling costs are paid in cash on billed gross (`CompetitorManager.cs:1221`) while the billing is deferred 12–18 weeks. `label-finance`, `label-sales-weekly`. | The squeeze is real (Vee-Jay), but a label with a hit and receivables 4× its debt got plant and distributor credit or factoring. | **High** for closures-must-be-earned. 11 of 19 Small bankruptcies had charting records in their last 26 weeks. This is the common root of two known items (tax on receivables, chart-proven wholesale). | Mechanism: receivable-backed overdraft at a carrying charge (`--receivable-backed-overdraft`, screened below). |
| 4 | **Opening contracts expire on 1 January.** 637 contract-expiry drops land on 1/1/1961: 40–41% of all drops in two years, on one day. 97% of the maturing deals are dropped, 89 of them without one release. | Label (roster) | 5 | `RosterManager.cs:438-439`: expiry is `max(1961, signed year + term)` and the week is a multiple of 52, so every seeded deal matures on a year boundary and every lapsed one is clamped to week 52. `artist-population-events`. | Options fell due on signing anniversaries through the year. | **Medium.** About 22% of signed acts reach the free-agent pool in one week; the same cliff recurs at weeks 104–260 for longer seeded terms. | Mechanism: anniversary weeks (`--staggered-opening-contracts`, screened below). |
| 5 | **Band splits fire once a year.** 85 of 91 disbandments and retirements in two years happen in the week of 6 January 1961; 22 of those acts had Top 40 hits and 32 were under contract. | Fanbase / artists | 5 | `BandLifeService.cs:51`: "the pass runs once per year at the population lifecycle's year rollover". `artist-population-events`. This is the root of the handoff's "stars disband at the first year-end". | Splits were spread through the year. | **Medium.** It removes the field's largest non-Big-5 acts in one week (known side effect), and it means "lineup churn never fires" is no longer true under `--enable-lineup-churn=world`. | Mechanism: date each act's yearly pass to its own anniversary week. Not screened (touches the band-life RNG keys). |
| 6 | **Industry margins are 2–4× history and fixed costs are near zero.** Pretax earnings are 24–35% of gross in every tier. Campaign spend is 0.1–0.5% of gross, recording 2–5%, payroll 1.3% at the Majors. | Economy (costs) | 2 | `company-monthly` by tier and year, seed 2002: Major 23.6 / 25.1%, MidTier 26.5 / 23.6%, Independent 28.2 / 28.2%, Boutique 33.0 / 30.7%, Small 32.8 / 35.1% (1960 / 1961). | Capitol reported about 7% after tax (about 14% pretax) in 1960–61; my figure, not in the repo. Promotion and selling ran well above 1% of sales. | **High as a root.** With costs this small every cash gate is slack for a label that sells anything (see 7 and 8). Singles lose money at a historical rate (71–86% all-in), so the margin is LP income (finding 1) plus hits. | Calibration after finding 1 is fixed: re-measure before touching cost constants. |
| 7 | **"Rising" is the normal state, not a signal.** 41–51% of non-Major labels and 88–100% of Majors are Rising, which gives 1.2× release capacity and 0.65× competitive-exit chance. | Label | 5, 8 | `EconomicLabelLifecycle.cs:43`: Rising is recent cash flow above 2× overhead; a home-run label's overhead is about $25–50 a month. Read at `LabelReleaseProgramme.cs:175` and `LabelLifecycleManager.cs:487`. `label-finance`. | None needed: a gate half the field passes is not a gate. | **Low–medium.** A flat 20% release bonus for half the field, compounding with finding 6. | Mechanism: measure against the label's own trend or its tier's median, not its overhead. |
| 8 | **Major cash has no sink.** Median Major cash goes $0.24M → $10.8M in 104 weeks (6 → 142 months of overhead). | Label | 1 | `label-finance`. Opening cash is $50–200k (`LabelStartingFinances.cs`) against $4–5M of monthly gross; `LabelCorporateModel.PlanDistribution` pays out 35–65% of earnings. | Record divisions remitted surplus to the parent. | **Low** for charts (no Major gate binds either way), but every Major affordability test is inert. | Calibration: raise the corporate-division payout share, or sweep cash above protected cash to the parent. |
| 9 | **The LP purchase rate is a tier table.** Major 1.36, MidTier 0.95, Boutique 0.63, Independent 0.57, Small 0.50. | Economy (LP demand) | 9 | `AlbumSimulator.cs:241`, applied at line 109. `--label-demand-parity` removed the same double count for singles only (`ChartSimulator.cs:733`); the LP path was not touched. | Same argument as the adopted singles parity: distribution already bounds stock and shelf. | **Medium.** A 2.7× Major-to-Small edge from a constant. It currently hides part of finding 1. | Mechanism: route the LP scale through `LabelFootprintParity.Demand`. Screen only together with finding 1's fix. |
| 10 | **Follower stock is far from equilibrium.** Followings reach 27.9M by end-1960 with 20.8M joins against 5.1M losses. | Fanbase | 1 | `MEDIA_FOLLOWING year=1960` in both control logs. At the measured ~18% yearly loss rate the stock settles near 110M memberships unless the fit-population cap binds first. | Design target in `CelebrityFanbaseDesign.md` §9. | **Unknown; decade-only.** Follower singles intent was already 17% of single units in 1960. | Check in the decade A/B before any change. Known item, restated with the inflow-to-loss ratio. |
| 11 | **Idle acts.** 35–38% of MidTier acts signed for 26+ weeks release nothing in a year; singles per signed act run 0.9–1.9 a year. | Label (roster) | 8 | `act-release-years`, both seeds. | Contracts called for 4–8 sides a year (`AILabel.CalculateContractSinglesObligation`). | **Medium.** Known (earned-roster ratchet, hit acts release too little); still true under `--roster-delivery-bound=2`. | Already owned by the roster work. |
| 12 | **Label production quality only rises.** +0.01 a quarter after 1963 up to 0.95, kept after the asset is written off. | Label | 1 | `LabelCorporateModel.cs:326-345`. Not measurable in 104 weeks (starts 1964). | — | **Low**, decade-only. | Mechanism: tie the increment to the asset's book value. |
| 13 | **Artist loyalty does not affect drops.** | Label | 7 | `AILabel.cs:824-825`: the `artistLoyalty > 0.7` test sits after every drop rule and both branches return false. Loyalty still sets the renewal roll and the term bias. | — | **Low.** | Delete the dead test, or move it above the flop rules if it was meant to protect acts. |

## 2. Screened fixes

All three are in `Systems/GameyAuditSwitches.cs`. Check: `--gamey-audit-switches-check` on `SimTools/SaveLoadRoundTripRunner.tscn` passes.

Each cell is seed 2002 / seed 1001. Big-5 Top 10 share moves by ±3–9 points between arms on single seeds, which is reshuffle noise; it is shown only to confirm nothing broke.

### 2.1 `--lp-earned-discovery=0.25` (finding 1): right direction, too weak alone

An LP's unconditional weekly discovery term (0.004 + 0.012 × appeal) is scaled by `F + (1 − F) × known`, where `known` is the act's chart heat or its release-awareness bonus over 0.30. Hook: `AlbumSimulator.UpdateAlbum`.

| Metric (LPs launched weeks 1–52) | Control | Arm |
|---|---|---|
| Median LP units, act's best single under 1,000 | 20.3k / 23.1k | 16.9k / 17.3k |
| Median LP units, act's best single 100k+ | 39.0k / 40.6k | 34.2k / 38.6k |
| Top 10% share of LP units | 33.1 / 31.7 | 35.1 / 35.0 |
| Small-label share of LP units | 41.9 / 40.0 | 38.6 / 37.7 |
| LPs losing money | 0.0% / 0.2% | 0.0% / 0.4% |
| Store LP units, two years | 95.2M / 97.4M | 85.7M / 86.5M |
| Bankruptcies | 124 / 141 | 134 / 146 |

- Same sign on both seeds for every LP metric, so the term is a real part of the floor: cutting it to a quarter takes 17–25% off an unknown act's LP.
- It is not the whole floor. An unknown act's LP still sells 17k and still never loses money. The rest is launch awareness (0.04 base plus label reputation and marketing), the label push term (`currentLabelPush × 0.03`, also act-blind) and the purchase rate itself.
- Store LP units fall 10%, because the LP market is demand-led (budget equals intent in every region-week) and nothing moves the lost units to known acts. Stores were calibrated to Billboard, so adopting this needs the hit side raised in the same change (finding 2).
- **Verdict: mechanism confirmed, not adoptable alone.** Next screen: F = 0 together with an act-scaled launch base and the LP purchase-rate parity of finding 9.

### 2.2 `--staggered-opening-contracts` (finding 4): removes the cliff

A seeded deal keeps its expiry year but matures in its own week inside the 52 before that 1 January, keyed on the act's id. Hook: `RosterManager.InitialSignArtist`. No random draw.

| Metric | Control | Arm |
|---|---|---|
| Drops on the busiest day | 650 / 659 | 152 / 152 |
| Busiest day's share of all drops | 40.3% / 41.3% | 7.4% / 7.1% |
| Drops by quarter, 1960 | 0, 89, 247, 211 / 1, 64, 251, 235 | 102, 247, 415, 360 / 115, 258, 434, 344 |
| Drops by quarter, 1961 | 694, 80, 117, 173 / 707, 82, 106, 149 | 244, 212, 228, 242 / 272, 250, 244, 220 |
| Contract-expiry drops | 818 / 782 | 1,408 / 1,340 |
| Signings | 1,286 / 1,286 | 1,563 / 1,471 |
| Singles launched 1960 | 4,260 / 4,080 | 4,092 / 3,760 |
| Bankruptcies | 124 / 141 | 123 / 153 |

- The 1 January spike is gone and 1961 turnover is flat by quarter.
- The totals are not like for like. The arm pulls the second seeded cohort (due 1 January 1962, one week after the control window closes) into 1961, so expiry drops rise by about 580. It also ends lapsed deals an average of 26 weeks earlier, which costs 4–8% of 1960 singles.
- The remaining 152-drop day is the monthly review: expiries are only tested at month change (`RosterManager.OnMonthChanged`).
- **Verdict: adoptable as a mechanism fix.** Author's call whether lapsed deals should run to an anniversary in 1960 (as built) or in 1961 (keeps 1960 volume, moves the cohort a year later).

### 2.3 `--receivable-backed-overdraft=0.75` (finding 3): stops the unearned closures

A label whose overdraft is within 75% of its outstanding wholesale receivables is not counted insolvent that month. It is marked Struggling and pays 1.5% of the overdraft. Hook: `EconomicLabelLifecycle.ReviewMonth`.

| Metric | Control | Arm |
|---|---|---|
| Bankruptcies | 124 / 141 | 112 / 121 |
| Bankruptcies with receivables ≥ overdraft | 20 / 19 | 0 / 0 |
| Of the control's receivable-covered bankrupts: alive at week 104 | — | 17 of 20 / 14 of 19 |
| Their median cash at week 104 | — | $15k / $21k |
| Singles launched 1961 | 3,986 / 3,845 | 4,069 / 3,969 |
| Dormancy exits | 36 / 39 | 39 / 39 |

- Bankruptcies fall 10–14%, and all of the fall is the receivable-covered class. 31 of the 39 labels that folded on a hit in the control are alive and cash-positive two years in; 6 still go bankrupt later, on the ordinary rule.
- No other closure class moves, so this is not a blanket rescue.
- **Verdict: adoptable as a mechanism fix.** The share (0.75) and the charge (1.5% a month) are scenario values (open question 4). It should be re-screened with `--chart-proven-wholesale`, whose failure was this same squeeze.

## 3. Checked and found sound

- **P&D advances are repaid under the bundle.** `--distribution-advance-recoupment` withheld $438k of $515k advanced (seed 2002) and $408k of $475k (seed 1001) inside two years; 4–6% was forgiven. The "never repaid" item is closed for the reference bundle.
- **Artist momentum decays.** `--artist-momentum-half-life=52` is in the bundle.
- **Singles lose money at a historical rate.** 71–86% of retired singles lose money all-in by tier (`release-all-in-outcomes`), against 80–90% for history. The old 8% figure no longer holds.
- **The first career rung is reachable.** 346 promotions to Rising and 208 to Established in 104 weeks (`career-ladder`), under `--album-scale-career-milestones`.
- **Distribution cost is charged once.** No-deal labels pay `0.25 × (1 − owned reach)` as skim and the corporate fulfilment cost on the owned share; deal labels pay the skim on granted regions only and fulfilment on the rest. Both come to about 25–26% of the gross they apply to (`CompetitorManager.cs:1215-1222, 1522-1533`).
- **Unsold pressings cost money.** 8–10% of pressed singles go unsold and are billed (`release-outcomes`).
- **Rack jobbers are not a floor.** Racks carry 25 hit and 50 catalogue titles a region, ranked by store sales (`RackJobberChannel.cs`).
- **Studio parent capital and AI backer loans** are capped, sourced and repaid or reviewed (`StudioParentLabels.cs`, `AiBackers.cs`). Neither is in the reference bundle.
- **Record excise and trade support** are charged once per sale, and the excise stops on its 1965 repeal date (`LabelCorporateModel.cs:77-90`).
- **Most bankruptcies are earned.** 84% of bankruptcies have receivables under half the overdraft, and Independent bankrupts sold a median 1,202 copies in their last 26 weeks.

## 4. Known items re-verified

| Item | Status under the reference bundle |
|---|---|
| P&D advances never repaid | Fixed (section 3). |
| Artist momentum never decays | Fixed (section 3). |
| First career rung unreachable | Fixed (section 3). |
| Tax paid on uncollected receivables | Still true: `LabelCorporateModel.UnprovidedTax` adds the change in receivables. Same root as finding 3. |
| Chart-proven wholesale bankrupting indies | Same root as finding 3. |
| Follower advance stock free | Still true by design; all shelf stock is unbilled (`LabelPressingEconomics` header). Not re-measured. |
| Singles a fixed purse | Still true: 250.8M of budget on both seeds, 6.8× oversubscribed. Root of finding 2's singles half. |
| Earned-roster ratchet | Still visible as idle acts (finding 11). |
| Lineup churn never fires | No longer true: it fires as one annual batch (finding 5). |
| Controversy cosmetic | Still true: `controversy=0.00` in both control logs. |
| Promo-single credit, Big-5 premium | Not re-verified. |

## 5. Open questions for the author

1. **What did a flop single sell?** The model's median is 69–90 copies. I would expect 500–5,000 from jukebox operators and one-stops, but I have no source in the repo. A sourced figure decides whether finding 2's singles half needs a floor channel.
2. **What share of LP units did the smaller labels hold in 1960–61?** The model gives Independent, Boutique and Small labels 40–42% of in-run LP units. If the answer is near 15–25%, finding 1 is the main lever on Major LP share as well.
3. **Should an unknown act's LP sell at all without a hit?** `--lp-earned-discovery=F` leaves a share F. Jazz, classical, folk and comedy LPs sold without singles, so F may need to vary by genre.
4. **Receivable-backed overdraft: who carries it and at what price?** The screen uses 1.5% a month and 75% of receivables. A sourced factoring or plant-credit term would replace both.
5. **Is the yearly band-life pass a deliberate simplification?** Moving it to anniversaries changes keyed RNG ids, so it needs its own inertness plan.
6. **Major cash:** should a corporate division sweep surplus to its parent, and should anything a Major does be cash-limited?
