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

### 2.4 `--lp-earned-discovery=0 --lp-earned-launch` (`le`) and `+ --lp-label-demand-parity` (`lep`) on the rg10 candidate (2026-11-15)

New switches in `Systems/GameyAuditSwitches.cs`: `--lp-earned-launch` scales the LP launch base (0.04, both LP launch paths
in `CompetitorManager`) by how known the act is; `--lp-label-demand-parity` moves the LP tier constant toward 1 by the
`--label-demand-parity` share. Runner `SimTools/run_lp_earned_screen.ps1`; control `rg10-v1-{2002,1001}` (208 weeks; the
off arm `lpe-v1-2002-off` matches it byte for byte over 104 weeks). Lifetime units at week 208 of LPs launched weeks
1–52 (release-outcomes `pressingBookSold` for retired titles, `live-records-snapshot` for live ones). Seed 2002 / 1001.

| Metric | Control | `le` | `lep` |
|---|---|---|---|
| Median LP units, act's best single under 1k | 17.6k / 18.9k | **9.8k / 10.9k** | 15.0k / 18.3k |
| Median LP units, act's best single 100k+ | 37.8k / 36.8k | 31.6k / 33.5k | 37.6k / 37.1k |
| Median LP units, Major / Small | 96k / 10.0k; 91k / 9.4k | 64k / 4.8k; 59k / 4.2k | 38k / 9.2k; 37k / 8.6k |
| Top 10% share of LP units | 44.3 / 43.6 | 52.8 / 50.2 | 45.7 / 44.1 |
| Small-label share of LP units | 32.4 / 32.9 | 27.6 / 29.1 | 44.4 / 46.8 |
| LPs losing money | 0.0% / 0.1% | 1.5% / 2.6% | 1.4% / 0.8% |
| LP units, cohort total | 69.2M / 69.0M | 53.6M / 54.4M | 59.9M / 60.1M |
| Record gross, 1960–63 ($M) | 364/472/499/464; 369/468/498/463 | 340/419/458/426; 343/414/467/438 | 349/429/463/437; 355/431/472/441 |
| Bankruptcies (log) | 116 / 126 | 129 / 143 | 112 / 117 |

- **`le` works in the right direction on both seeds.** It cuts an unknown act's LP by 42–44% and a hit act's by
  9–16%, so the gap between them widens from 2.1× to 3.1–3.2×, and LP units become more concentrated (top 10% share +7–9 points).
- **It is still not enough.** An unknown act's LP still sells about 10k and only 1.5–2.6% of LPs lose money. What is
  left is label-side and act-blind: label reputation × 0.1 at launch, label push × 0.03 a week, regional word of mouth
  from appeal alone (`AlbumSimulator.UpdateRegionalState`), and the purchase rate.
- **It costs 22% of LP units and 7–11% of record gross** (the LP market is demand-led, see §2.1), and 10–14% more
  bankruptcies. Adopting it needs the hit side raised in the same change.
- **`lep` is rejected.** Removing the LP tier constant moves units to small labels: Major median LP −60%, small-label
  share 44–47%, and an unknown act's LP back to 15–18k. On the LP side the tier constant is offsetting the act-blind
  terms, not double-counting distribution.
- **Next:** take word of mouth from buyers, not appeal, plus a decade-scale check of hit LPs against the 0.5–2M
  No. 1 LP anchor before recalibrating volume.

### 2.5 Contract expiry: telemetry and `--anniversary-opening-contracts` (2026-11-15)

Observe-only `*-contract-expiry.csv` (`Systems/ContractExpiryDecision.cs`): one row per matured AI contract with the
evidence the renewal rule read and its outcome. Byte-inert (`lpe-v1-2002-off`). Note: `re-signing` rows in
`artist-population-events` are signings to a new label after a prior contract, not renewals, so this audit's earlier
"97% of maturing deals are dropped" was wrong.

Control `lpe-v1-2002-off`, 104 weeks, 1,358 maturities:
- 443 renewed (33%), 822 not wanted, 87 unaffordable, 6 studio-parent bids. Renewal follows units under the deal:
  89% of acts with 200k+ renewed, 50% at 50–200k, 16% at 10–50k.
- **The renewal rule reads stale evidence.** 560 of the 822 non-renewals are the first-contract flop rule
  (`RosterManager.ShouldResignArtist`: two or more sides, no Top 40, no charted record, no breakout, not recouped).
  A record counts as charted only when its chart run ends (`SimulatedArtist.CompleteChartRun`), so a single still climbing
  counts as nothing: acts with 430–550k-unit singles released in the last 12 weeks are dropped as flops.
- **Delivery maturity decides too early.** A deal also matures the week its singles obligation is delivered
  (`RosterManager.IsContractMatured`): 245 maturities, every one with a release under 12 weeks old. Example: a Major's
  1960 signing shipped its 4th side in week 104 and was dropped in week 105, and the single peaked at #2 in week 113
  with 864k.
- 87 unaffordable renewals include a Star with 1.48M units under the deal.
- **Fix screened: `--renewal-live-evidence`.** A record under the deal that is charting now, has charted, or has broken out
  counts as evidence against the flop rule, and an AI deal matures on its date only. Runs `lpe-v1-{2002,1001}-re` against
  `lpe-v1-{2002,1001}-off` (104 weeks; `lpe-v1-1001-off` matches `rg10-v1-1001` byte for byte, so the telemetry is inert).

  | Seed 2002 / 1001 | Control | `re` |
  |---|---|---|
  | Maturities (deals decided) | 1,358 / 1,261 | 1,123 / 1,056 |
  | Delivery-only maturities | 244 / 210 | 0 / 0 |
  | Renewed | 32.6% / 31.7% | 36.2% / 31.6% |
  | Renewed, 10–50k units under the deal | 16% / 14% | 22% / 22% |
  | Renewed, 200k+ | 89% / 89% | 93% / 82% |
  | Flop-rule non-renewals | 560 / – | 381 / 283 |
  | Drops (all reasons) | 1,432 / 1,456 | 1,234 / 1,328 |
  | Singles launched 1961 | 4,382 / 4,104 | 4,492 / 4,119 |
  | Bankruptcies | 85 / 101 | 84 / 97 |

  - Both mechanisms do what they should: no deal is decided the week its last side ships, and no act with a record on
    the chart is dropped as a flop. Drops fall 9–14% and 1961 singles rise 0.4–2.5%; bankruptcies do not move.
  - The 200k+ renewal rate moves −7 points on seed 1001 on about 150 acts; read as noise until a second look.
  - **Adopted 2026-11-15** (in `candidate-request.json`). The effect is modest because two other paths remain: the loyalty
    roll (249 / 319 non-renewals; an act that is not a flop is renewed with probability `artistLoyalty`, 0.28–0.78,
    whatever it sold) and the cash gate (81 / 115 unaffordable, almost all first-contract acts at Independent,
    Boutique and Small labels). The roll is the next candidate: renewal should follow recoupment or sales under
    the deal, not a coin.

`--anniversary-opening-contracts` (author's design): a seeded deal gets a signing week from the act's id and matures on
its anniversary after its term; a deal past its term is on its yearly option and comes due on its 1960 anniversary.
Run `lpe-v1-2002-an`, 104 weeks:

| | Control | Anniversary |
|---|---|---|
| Busiest drop day | 746 (1 Jan 1961) | 147 (1 Aug 1960, monthly review) |
| Maturities by quarter | almost all in one quarter | 120–240 every quarter |
| Renewal rate | 32.6% | 29.9% |
| Singles launched 1960 / 1961 | 4,398 / 4,382 | 4,311 / 4,297 |

It costs 2% of 1960 singles, against 4–8% for `--staggered-opening-contracts`, because live deals are no longer pulled a
year early. **Adopted 2026-11-15** (in `candidate-request.json`); supersedes 2.2.

### 2.6 `--renewal-earned-option`: renewal on what the deal earned, not a loyalty roll (2026-11-15)

The last line of `RosterManager.ShouldResignArtist` renewed any act that got past the career and flop rules with
probability `artistLoyalty`. With the switch on:
- The label renews when the deal paid: its realized net on records released under the deal (retired records are banked
  in the new `SimulatedArtist.contractRealizedNet`, live ones count at net to date), plus a loyalty allowance of
  `artistLoyalty × next advance`, must be at least 0. An act never released under the deal lapses. No random draw.
- A deal with positive net is exempt from the first-contract flop rule. Without this exemption (first arm, `cand-v1-*-eo`),
  95 / 82 deals that made the label $10–150k were dropped for never charting and not having recouped the act's own
  advance. One Major act made $124k.

Control `cand-v1-{2002,1001}-off` = the adopted candidate (`candidate-request.json`); arm `cand-v2-{2002,1001}-eo`.
104 weeks; contract-expiry telemetry now carries `dealNet` and `nextAdvance`.

| Seed 2002 / 1001 | Control | `eo` |
|---|---|---|
| Renewed, deal lost more than $1k | 29% / 29% | 3% / 0% |
| Renewed, deal lost up to $1k | 30% / 34% | 2% / 3% |
| Renewed, deal made $0–10k | 21% / 23% | **81% / 79%** |
| Renewed, deal made $10k+ | 44% / 45% | **89% / 83%** |
| Renewed overall | 31.5% / 34.6% | 35.5% / 32.1% |
| Net of profitable deals walked away from | $7.8M / $6.9M | $3.3M / $3.2M |
| Unaffordable renewals | 140 / 140 | 78 / 84 |
| Drops (all reasons) | 1,409 / 1,481 | 1,354 / 1,543 |
| Singles launched 1961 | 4,343 / 4,212 | 4,369 / 4,240 |
| Bankruptcies | 87 / 98 | 96 / 104 |

- In the control the roll ignored money: a deal that made $10k+ was renewed less often than one that lost money did on a
  good roll. Under the switch renewal follows the deal on both seeds, at about the same overall rate.
- What is still walked away from ($3.2–3.3M) is the cash gate and studio-parent bids.
- **Bankruptcies +9 / +6, traced (2026-11-15).** These are closures that losses earned, not new ones, and total exits barely move.
  - Total exits are 111 → 112 and 130 → 138. On seed 2002 dormancy exits fall from 24 to 16, so part of the change is the
    same labels closing as bankrupt instead of dormant.
  - Label identities churn: 31 / 38 labels go bankrupt only under the switch and 22 / 32 only in the control. The
    net change is small inside that reshuffle.
  - The labels that fold only under the switch end with empty rosters. Before closing they renewed 0.03 acts each,
    against 0.23–0.26 for the same labels in the control, and 0.6–0.7 of their maturing acts per label had never been
    released. They then released 0.0–0.1 records in their last 13 weeks (control 1.4–2.5) and signed 0.13–0.16
    replacements after their last maturity (control 0.42–0.45), with about $1.0–1.3k cash at that point.
  - So in the control a cash-starved label whose acts all lost money was kept trading by the loyalty roll. Under the
    switch it lets those acts go, cannot afford to sign anyone, and folds. That fits *closures must be earned*.
  - It is not renewal advances: labels that later went bankrupt renewed 3 / 2 acts under the switch against 15 / 25
    in the control.
- **Adopted 2026-11-15** (in `candidate-request.json`). Recheck exits in the decade A/B.
- Major-family chart share moves −3 / +6 points in 1961, which is reshuffle noise.

### 2.6b `--lp-buyer-word-of-mouth=10000` on top of `le` (2026-11-15)

LP word of mouth's appeal term (`appeal² × 0.62`) is scaled by `units / (units + K)`, K = 10,000. Runs
`cand-v3-{2002,1001}-{off,le,lw}`, the adopted candidate, 208 weeks, LPs launched weeks 1–52 (lifetime at week 208).

| Seed 2002 / 1001 | Control | `le` | `lw` (`le` + word of mouth) |
|---|---|---|---|
| Median LP units, act's best single under 1k | 17.4k / 18.5k | 9.8k / 10.8k | **7.3k / 8.6k** |
| Median LP units, act's best single 100k+ | 41.2k / 36.2k | 35.4k / 32.5k | 36.9k / 35.7k |
| LPs losing money | 0.1% / 0.1% | 1.8% / 1.8% | 7.1% / 5.6% |
| Top 10% share of LP units | 43.8 / 43.5 | 52.7 / 50.6 | 54.8 / 52.8 |
| Store LP units 1961 (Billboard 68M) | 65M / 66M | 45M / 49M | 41M / 42M |
| Record gross 1961 ($M) | 468 / 461 | 415 / 428 | 396 / 397 |
| Bankruptcies | 124 / 124 | 133 / 138 | **159 / 177** |

- Word of mouth from buyers works as intended. It takes a further 13–26% off an unknown act's LP, leaves hit acts' LPs
  where `le` had them (slightly up), and the hit-to-unknown gap reaches 4.2–5.1× (control 2.0–2.4×). LPs start to
  lose money.
- **It is not adoptable alone.** Store LPs fall to 41–42M against Billboard's 68M, gross falls 14–15%, and
  bankruptcies rise 28–43%. Under the old model labels lived on act-blind LP income (finding 6); taking it away
  without putting the volume back where history had it closes labels for the wrong reason.
- **Next:** restore store LP volume with a global LP purchase scale (hit LPs have room under the 0.5–2M anchor:
  0.7–0.8M full price now). Because demand is now concentrated on known acts, a global scale returns the volume to
  them. Re-screen with that scale at 104 weeks and check unknown-act LPs, the anchor and bankruptcies together.

### 2.7 Hit LPs against the 0.5–2M No. 1 LP anchor (2026-11-15) — the full-price hit side is inside it

Analysis only, on `rg10-v1-{2002,1001}` (208 weeks), LPs launched in the run (opening catalogue excluded). Per title:
store units (`release-outcomes` / `live-records-snapshot`), rack units (`rack-title-weekly` `units`) and budget
reissues of the title's own master (`budget-product-catalogue`, `sourceId`). Record clubs are logged by label only, so
are not in a title's total.

- **Full-price sales are inside the anchor.** The biggest launched LPs sell 450–550k in stores and 230–280k on racks,
  so 0.7–0.8M at full price, inside 0.5–2M. The "top LP 204–207k a year" in finding 2 counted stores only, so the
  hit side does not need raising for the anchor. (Finding 2's flat top-10% share still stands.)
- **Budget reissues of current hits (new finding).** `BudgetLpChannel` offers a budget edition of any own master at
  `HoldbackWeeks` = 52, ranked by `Appeal = record.totalUnitsSold`. Every hit master (store ≥ 100k, 390 / 407 titles)
  is reissued exactly 51 weeks after launch. The biggest budget editions sell 1.6–2.0M, up to 4× their full-price
  LP, and budget products total 58–61M units over four years. Budget lines (Camden, Harmony, Pickwick) reissued
  catalogue several years old and kept current hits at full price, so these are not period-faithful.
  Pattern 4/5 (a gate at 52 weeks with a hit-proportional draw).
- **Racks sell Classical at 4–10× store sales.** The top rack titles on both seeds are Classical (one at 2.85M rack
  against 337k in stores, one at 3.1M against 577k). Racks carry 25 hits and 50 catalogue titles a region ranked by store
  sales (`RackJobberChannel`), so a long-lived catalogue title keeps a rack slot for years. Traced in 2.8.
- **Consequence for the LP screens.** Any recalibration of LP volume should hold stores plus racks on the full-price
  product and leave the budget and rack anomalies to their own fixes; otherwise lost store units would be made up from
  budget reissues of hits.

### 2.8 LP model against period sources (2026-11-15)

Research pass on the LP channels. Model figures are `rg10-v1-{2002,1001}` unless named.

**Channel totals are close.** 1961: stores 66M, front-line racks 34M, budget 17M, clubs 45M = 162M, against
Billboard's 173M (stores 68, racks 60, clubs and mail order 45; memory `billboard-1966-buyers-guide-market-totals`).
Clubs are exact. Racks plus budget are 51M against 60M.

**A. Budget reissues of current hits — not period practice (high).**
- Sources:
  - [Budget album (Wikipedia)](https://en.wikipedia.org/wiki/Budget_album): Harmony "seldom issued material that had not
    been previously released". RCA Camden re-released "older recordings by currently popular artists" and "vintage
    material from previous decades". Pickwick licensed Capitol's "secondary and noncurrent titles". Crown, Tops and
    Somerset used pseudonymous studio groups and sound-alikes.
  - A 1958 Harmony LP reissued a 1954 Columbia choir LP (ARSC list, via the
    [Harmony Records](https://en.wikipedia.org/wiki/Harmony_Records) search).
  - The Capitol Pickwick series of 1966–70 ([bsnpubs](https://mail.bsnpubs.com/capitol/previews/pickwick-capitol.pdf))
    repackaged Sinatra's 1953–61 Capitol material and Wayne Newton's 1963–65 sides "in sliced and diced form", not the
    hit LP as issued. The Sinatra and Newton dates are the author's knowledge, not the PDF's.
  - Budget LPs were 21% of NARM members' LP dollars in 1962 (`SimTools/RackJobberChannelDesign.md`).
- No source found states a holdback rule. The pattern is noncurrent, secondary or several-years-old material,
  repackaged, priced at $1.49–1.98 against $3.98.
- Model: `BudgetLpChannel.HoldbackWeeks` = 52 and product appeal = the source LP's lifetime units, both labelled scenario
  assumptions in `SimTools/BudgetLpProducts.md`. Every hit master is reissued 51 weeks after launch, and the biggest
  budget editions outsell their full-price LP up to 4×. Total budget volume (about 15–20M a year) is about right; the
  selection is wrong.
- Proposed fix: budget products draw on noncurrent catalogue (a multi-year holdback, and no title still selling at
  full price), weighted toward secondary titles rather than the best sellers. This needs an author decision on the
  holdback (3 years is the shortest age the sources support).

**B. Racks sell Classical at 4–10× store sales — implausible (high).** *Correction (2026-11-15): the
over-selling titles are artistless soundtrack and cast-album LPs (`ProcessWeeklySoundtrackOrigination`). They are
about 1.5% of LP launches and take 10–19% of launched-LP rack units. Classical by roster acts is not over-sold on
racks. See `SimTools/LpChannelFableHandoff.md` issue 2.*
- Sources:
  - [Cliburn's Tchaikovsky concerto](https://en.wikipedia.org/wiki/Tchaikovsky_Piano_Concerto_No._1_(Van_Cliburn_1958_recording)),
    a 1958 No. 1 LP and a national event, was the first classical LP to reach 1M, in December 1961
    ([Time](https://time.com/archive/6812043/records-hot-classic), [Real Time 1960s](https://realtime1960s.com/post/van-cliburn-record-goes-gold)),
    and about 2.5M by the end of the decade.
  - Billboard 1965 (via search) has racks carrying classical, mostly budget classics at $1.98. Rack space was about a
    third best sellers.
- Model: single launched Classical LPs sell 2.85M and 3.1M through racks in about two years, against 337k and 577k in
  stores. Classical is 10.5% of rack LP units in 1962 on 2.2% of the same titles' store units.
- Mechanism: rack demand per title is `ShopperPool × GenreWeight × exposure × conversion`, with conversion ∝ quality^2.5,
  and each racked title sells to its own rack-shopper exhaustion ledger. The catalogue tier takes past-peak titles
  ranked by this week's store units, so a steady high-quality title gets a slot and then sells to an unexhausted pool,
  bounded by neither its store popularity nor a shared pool. Both are act-blind.
- Proposed fix: bound a title's rack sales by its store standing (rack units as a share of its store units, or a
  shared rack exhaustion ledger, as `--rack-shared-exhaustion` already offers for the budget pool), and check which
  Classical titles racks carried (budget classics, per the 1965 source).

**C. Folk over-share (known, high).** Folk is 30–42% of store LP units and 25–31% of rack units in 1960–62. The
Kingston Trio, the boom's biggest act, was about 15–22% of Capitol's sales at its 1959–62 peak
([folksongindex](https://folksongindex.fandom.com/wiki/The_Kingston_Trio), sources conflict). The author's year-end
benchmark puts Folk at about 13% of the album top 50 (memory `folk-bossa-untouched-album-affinity`). The probe
`--album-affinity=Folk=0.48,BossaNova=0.40,Jazz=0.30` cut share error on five seeds and was never adopted.

**D. 1960 store LPs are a warm-up (medium).** Store LP units are 37M in 1960 against 66M in 1961; Billboard's 1961 is
68M, and LP dollars grew modestly 1960–61. The 1960 shortfall is the empty opening catalogue.

**E. Soul LP releases in 1960 (known, medium).** Soul has the most LP launches of any genre in 1960 (240), with RnB 46
and Blues 78. Memory `soul-album-is-segment-pool-bound` and `soul-over-share-predates-rotation` cover it.

**Checked, not a finding:** the LP-to-single release ratio. Independents issue 0.35–0.5 LPs per single in the model.
Vee-Jay ran about 70 main-series LPs 1957–63 plus jazz and gospel series against about 40–50 singles a year
([bsnpubs](https://bsnpubs.com/veejay/veejay100-1070.html)), so the ratio is plausible for an established independent.

Not found and still open: classical's share of 1960 LP sales (Schickel 1960 implies about 20% of record dollars,
unverified), Billboard's 1960 year-end LP list by genre, and budget LP unit share.

### 2.9 Budget editions from noncurrent catalogue, bundled with earned LP demand and a global LP scale (2026-11-15)

New switches:
- `--budget-noncurrent-weeks=N` (`BudgetLpChannel`, fix for 2.8A). A live record is still selling at full price, so it is
  never reissued; a master becomes a budget product only when it retires, and its edition waits until N weeks after its
  original release. N = 156 (3 years, the shortest age the sources support). 0 keeps the 52-week rule.
- `--budget-appeal-saturation=K` (built, not yet screened): budget appeal is `units × K / (units + K)`.
- `--lp-demand-scale` now accepts 0.9–2 (was 0.9–1). It scales store, rack and budget-autonomous LP demand.

Arm `lwb` = `le` + `--lp-buyer-word-of-mouth=10000` + `--budget-noncurrent-weeks=156` + `--lp-demand-scale=1.6` (the
profile's 0.98 is replaced, so about 1.63× control demand). Runs `bud-v1-{2002,1001}-lwb`, 208 weeks, against
`cand-v3-{2002,1001}-{off,lw}`. Off check: `bud-v1-2002-off` (27 weeks) matches `cand-v3-2002-off` byte for byte in
all 68 weekly streams. Native budget checks: 38 pass (`--budget-lp-check`).
LP cohort rows are LPs launched weeks 1–52, lifetime units at week 208. In this analysis "best single" maps acts through
`demand-release-census`, so the control's unknown-act median reads 15.7k / 17.9k, against 17.4k / 18.5k in 2.6b.

| Seed 2002 / 1001 | Control | `lw` | `lwb` |
|---|---|---|---|
| Median LP units, act's best single under 1k | 15.7k / 17.9k | 6.9k / 7.5k | 12.1k / 12.7k |
| Median LP units, act's best single 100k+ | 41.2k / 36.2k | 36.9k / 35.7k | 45.9k / 44.2k |
| Hit-to-unknown gap | 2.6× / 2.0× | 5.4× / 4.8× | 3.8× / 3.5× |
| LPs losing money | 0.1% / 0.1% | 7.2% / 5.7% | 0.9% / 2.4% |
| Top 10% share of LP units | 44.6 / 44.0 | 56.2 / 53.7 | 54.0 / 52.1 |
| Best full-price LP, stores | 438k / 490k | 506k / 491k | 631k / 619k |
| 1961 stores / racks / budget (M) (Billboard 68 / 60 racks incl. budget) | 65.1/34.7/16.5; 65.6/31.7/17.1 | 40.6/31.9/16.0; 41.9/31.0/16.3 | 60.9/45.6/15.6; 63.7/41.6/14.9 |
| 1963 stores / racks / budget (M) | 67.8/39.9/21.7; 72.3/36.3/21.1 | 48.3/38.2/21.6; 54.2/40.0/23.5 | 55.8/47.5/27.1; 58.1/45.5/26.8 |
| Record gross 1961 ($M) | 468 / 461 | 396 / 397 | 486 / 483 |
| Own-master budget editions issued in 208 weeks | 6,799 / 6,560 | 6,128 / 6,036 | 1,729 / 1,679 |
| Their median age at reissue (weeks) | 51 / 51 | 51 / 51 | 155 / 155 |
| Biggest budget edition / its full-price store LP | 2.04M / 349k; 1.98M / 490k | 2.25M / 410k; 1.82M / 487k | 1.22M / 254k; 1.22M / 316k |
| Bankruptcies (log, 208 weeks) | 124 / 124 | 159 / 177 | 118 / 139 |

**Budget fix: works as built.**
- No current title is reissued: own-master editions arrive at a median 155 weeks, all after retirement, and none
  before 1963. 1960–62 budget volume is the opening synthetic baskets (assumed older own repertoire), and they hold it:
  1961 budget is 15–16M, against 16.5–17.1M in the control.
- Two things are still wrong. First, 1963 budget jumps to 27M, against the control's 21–22M and the 15–20M a year
  history suggests, because three years of retired masters reach the shelf at once. Second, the biggest editions still
  sell 1.0–1.2M in their first year, 4–5× their full-price store LP. That is selection by appeal, the
  title's lifetime units, which is what the unscreened `--budget-appeal-saturation` is for.

**Global scale: restores volume where the earned-demand fixes concentrate it.**
- 1961 stores + racks + budget come to 122M / 120M against Billboard's 128M without clubs. The control has 116M / 114M
  and `lw` 89M. Gross is 3–5% above the control.
- The mix leans to racks. Stores are 61–64M against 68M; front-line racks are 42–46M, and racks plus budget come to 57–61M
  against 60M. Racks are no longer short. Stores fall to 56–58M in 1963 while launches match the control (2,718 /
  2,821 LPs that year), so per-title store demand thins late. That is not resolved here.
- An unknown act's LP comes back to 12k when the scale lifts it as well. The hit-to-unknown gap stays wider than in
  the control (3.5–3.8× against 2.0–2.6×), and 1–2% of LPs lose money.
- Hit LPs stay inside the anchor: the best full-price LP sells 0.62–0.63M in stores.
- Bankruptcies are −6 / +15 against the control and −41 / −38 against `lw`, so the `lw` closures came from lost
  volume.

**Verdict:** the budget mechanism is adoptable. The bundle is the first LP-demand package that keeps volume, gross and
closures near the control while making LP sales follow the act. Before adopting it:
- screen `--budget-appeal-saturation`, which should cut the 4–5× budget editions and the 1963 overshoot;
- check whether the 1963 store thinning is the scale (exhaustion) or racks taking store buyers, and possibly split the
  scale so racks are not lifted with stores;
- the decade A/B.

### 2.9b `--budget-appeal-saturation=25000` on `lwb`, and why stores thin in 1963 (2026-11-15)

Arm `lwbs` = `lwb` + `--budget-appeal-saturation=25000`. Runs `bud-v1-{2002,1001}-lwbs`, 208 weeks. Seed 2002 / 1001.

| | `lwb` | `lwbs` |
|---|---|---|
| Own-master editions that sell at all (of about 1,700 issued) | 70 / 70 | 103 / 121 |
| Top 20 editions: median units | 865k / 903k | 575k / 567k |
| Top 20 editions: median units ÷ source full-price store LP | 3.4× / 3.4× | 1.6× / 1.6× |
| Biggest edition / its store LP | 1.22M / 254k; 1.22M / 316k | 0.89M / 640k; 0.80M / 281k |
| Budget units from sources with 100k+ store sales | 95% / 98% | 96% / 99% |
| Budget units 1963 (M) | 27.1 / 26.8 | 27.5 / 28.8 |
| 1961 stores / racks / budget (M) | 60.9/45.6/15.6; 63.7/41.6/14.9 | 61.0/43.3/15.5; 67.0/40.3/15.1 |
| Record gross 1961 ($M) | 486 / 483 | 480 / 489 |
| Bankruptcies (208 weeks) | 118 / 139 | 117 / 121 |

- **Saturation halves the outsized editions.** The top budget editions now sell about 1.6× their full-price LP, not
  3.4×, and units spread over half as many titles again. Nothing outside the budget line moves beyond noise.
- **It cannot bring in secondary titles.** Saturation is monotonic, so the 25 offer slots a region has still go to the
  highest-appeal masters. 95–99% of budget units still come from former hits, and only 70–120 of about 1,700 issued
  editions ever sell. To reach secondary titles, the slots would have to be ranked by the same weight that splits demand
  (`sqrt(appeal) × quality^2.5`, with saturation), so a strong secondary title can win a slot.
- **Correction to 2.9: the 1963 budget volume is not an overshoot against history.** Valued at $3.98 front-line and
  $1.75 budget, budget is 20% of rack LP dollars in 1963 under `lwbs` and 19% in the control. The NARM 1962 figure is 21%.
  The real gap is 1962: 13% under `lwbs` against 20% in the control, because no in-run master is old enough yet and the
  opening baskets are exhausted.

**Why stores thin in 1963: the retail purchase envelope binds.**
- `RetailPurchaseEnvelope` gives stores and racks a joint 131.9M LPs a year: the single purse × the period LP:single
  ratio, held before 1966. In both arms unspent capacity is 3M in 1962 and 0.0–0.2M in 1963, so from 1962 the scale
  cannot add volume.
- `--lp-demand-scale` lifts rack and budget-autonomous demand along with store intent, so racks take the envelope. In
  1963, racks plus budget-autonomous units are 63M against the control's 49M, and stores get what is left.
- In 1963 store intent is about the same as the control's (111M vs 109M), but stores are allocated only 69M against 82M.
- **Next arm:** apply the scale to store intent only, leaving rack and budget-autonomous demand as calibrated, so the
  envelope split stays where the control has it. Pair it with slot ranking by demand weight.

**Verdict:** adopt `--budget-noncurrent-weeks=156` with `--budget-appeal-saturation=25000` as the budget mechanism.
Hold `--lp-demand-scale=1.6` until the stores-only arm is screened.

### 2.9c Stores-only LP scale and budget slots ranked by demand weight (2026-11-15)

New switches:
- `--lp-demand-scale-stores-only` (`LpDemandCalibration`): the scale applies to store intent only, so rack and
  budget-autonomous demand stay as calibrated. It is saved with the world and checked on resume.
- `--budget-rank-by-weight` (`BudgetLpChannel`): a region's 25 offer slots go to the highest demand weight,
  `sqrt(appeal) × quality^2.5 × that region's exhaustion × age decay`, instead of the highest appeal. A bought-out
  title gives its slot up.

Checks: 39 budget and 25 LP-calibration checks pass. The calibration check had still expected 1.01 to be rejected after
2.9 widened the range; it now tests 2.01. `bud-v2-2002-off` (27 weeks) matches `cand-v3-2002-off` byte for byte in 68
weekly streams. Arm `lwbr` = `lwbs` + both switches. Runs `bud-v2-{2002,1001}-lwbr`, 208 weeks. Seed 2002 / 1001.

| | Control | `lwbs` | `lwbr` |
|---|---|---|---|
| Median LP units, act's best single under 1k | 15.7k / 17.9k | 12.4k / 13.5k | 12.5k / 13.8k |
| Median LP units, act's best single 100k+ | 41.2k / 36.2k | 37.5k / 44.1k | 42.1k / 42.5k |
| LPs losing money | 0.1% / 0.1% | 1.2% / 1.5% | 1.2% / 1.4% |
| Top 10% share of LP units | 44.6 / 44.0 | 54.8 / 51.4 | 52.9 / 49.7 |
| Best full-price LP, stores | 438k / 490k | 640k / 595k | 558k / 574k |
| 1961 stores / racks / budget (M) | 65.1/34.7/16.5; 65.6/31.7/17.1 | 61.0/43.3/15.5; 67.0/40.3/15.1 | 65.2/35.8/14.0; 66.7/33.3/14.3 |
| 1962 stores / racks / budget (M) | 68.6/37.9/21.4; 73.6/34.3/20.4 | 61.9/50.4/17.4; 68.4/44.7/17.0 | 68.8/39.3/16.8; 73.0/36.3/17.2 |
| 1963 stores / racks / budget (M) | 67.8/39.9/21.7; 72.3/36.3/21.1 | 55.2/47.6/27.5; 57.7/43.9/28.8 | 61.1/41.6/27.2; 65.0/37.4/27.6 |
| Record gross 1961 ($M) | 468 / 461 | 480 / 489 | 468 / 469 |
| Own-master editions that sell | 93 / 121 | 103 / 121 | 212 / 198 |
| Budget units from sources with 100k+ store sales | 56% / 55% | 96% / 99% | 63% / 63% |
| Top 20 editions: median units ÷ source store LP | 3.8× / 4.2× | 1.6× / 1.6× | 2.6× / 2.9× |
| Bankruptcies (208 weeks) | 124 / 124 | 117 / 121 | 119 / 139 |

**Stores-only scale: the envelope split is back where the control has it.**
- 1961–62 stores (65–73M) and racks match the control within 1–2M. Gross is unchanged (468 / 469). Earned LP demand
  keeps its effect: the unknown act's LP sells 12.5–13.8k, the hit-to-unknown gap is 3.1–3.4× (control 2.0–2.6×), and
  1.2–1.4% of LPs lose money.
- 1963 stores are still 6–7M under the control. That is the budget line: 27M against 21–22M, as three years of
  retired masters arrive, and inside a full envelope those units come out of stores. Racks are at control
  (37–42M). By rack-dollar share (about 20%, NARM 1962 21%) the budget volume is period-plausible, so this is a
  consequence of a fixed envelope, not a channel fault.

**Rank by weight: budget reaches secondary titles, but quality now decides too much.**
- Twice as many editions sell (about 200). Sources with 100k+ store sales give 63% of budget units, against 96–99%
  under `lwbs`.
- With saturation at 25k, appeal is nearly flat, so `quality^2.5` picks the titles. 93% of budget units are titles
  with quality above 0.7 (the median issued master is 0.67).
- Each edition sells 450–670k in about a year, and a few sell 10–16× their full-price LP. Example: a Soul LP that sold
  40k in stores sells 638k at budget. That is the same anomaly as before, moved from former hits to
  high-quality flops.
- Next: bound a title's budget sales by its full-price record, or raise K (for example 100k) so sales history still
  counts. Either is calibration on top of the mechanism.

**Bankruptcies:** seed 2002 is flat (119 against 124), and seed 1001 is +15 (139). The same seed gave 121 under
`lwbs`, a difference of 18 from switches that touch only LP demand and the budget shelf. Read it as reshuffle noise
for now and recheck in the decade A/B.

**Verdict:** the `lwbr` package (earned launch and discovery, buyer word of mouth, noncurrent budget, saturation,
store-only scale 1.6, rank by weight) is the first that holds stores, racks, gross and closures at the control while
LP sales follow the act. It is a candidate for adoption once a budget per-title bound or a higher K is screened.

### 2.10 Rack bound by store standing, and the budget per-title bound (2026-11-15, LP channel handoff issues 2–3)

New switches, all off by default:
- `--rack-store-standing-bound=K` (`RackJobberChannel`): a racked title's own weekly rack demand in a region is at most
  K times its store units there that week. No saved state.
- `--budget-source-bound=M` (`BudgetLpChannel`): an own-master edition sells, in each region, at most M times its
  full-price LP's store units there. The source's regional store units are kept on the product at retirement, the
  edition's demand weight tapers linearly to zero at the bound, and a bought-out edition gives its slot up. Opening
  baskets have no source and are not bound. Saved with the world and checked on resume.
- `--album-chart-history` (`ChartAuditRunner`): writes the weekly album chart under `--calibration`. Telemetry only.

Checks: 45 budget, 53 rack and 25 LP-calibration checks pass. `fh-v1-2002-off` (27 weeks) matches `cand-v3-2002-off`
byte for byte in 94 weekly streams (`compare_weekly_streams.py`); five more differ only in rows each run writes at
close. Arms are on `lwbr`: `lref` (`lwbr` + chart telemetry), `rk` (K = 2), `bb` (M = 1), `bk` (saturation 100k).

**Issue 2. Why artistless LPs over-sell on racks.** An artistless soundtrack or cast album sells 4–14× its same-week
store units on racks. Act titles sell 0.8–2.5× (Easy Listening 1.8–2.5, Country 1.4–1.6, TradPop 1.3–1.5, Folk
0.8–1.05). Three terms, all in `RackJobberChannel`:
- `SoundtrackGenreWeight` is 0.9. An act title's weight is `baseline × segment tilt`, about 0.13 for Classical and
  0.4–0.6 for TradPop and Easy Listening.
- Exposure is `max(display, awareness)`. A soundtrack lives for years, its awareness climbs to 1.0, and its
  catalogue-tier exposure goes from 0.35 to 0.7–1.0 (act titles average 0.49).
- Rack conversion has no age decay, and exhaustion is `1 / (1 + 4 × penetration)` with a floor of 0.15. A title holding
  a slot sells about 0.9% of the region's rack-shopper pool every week without end. The control's top title sold about
  20k a week for 150 weeks against 4k a week in stores. `--rack-shared-exhaustion` only adds store units to the
  penetration, which is already past the floor.

K = 2 puts a soundtrack at the top of the act-title range (Easy Listening). Runs `fh-v1-{2002,1001}-{lref,rk}`,
104 weeks, seed 2002 / 1001:

| | `lref` | `rk` |
|---|---|---|
| Artistless share of launched-LP rack units | 20.0% / 18.4% | 13.5% / 15.5% |
| Artistless rack units (M) | 11.2 / 9.7 | 6.8 / 7.8 |
| Top rack title, rack / store (k) | 975 / 237; 1,172 / 242 | 401 / 146; 769 / 263 |
| Classical rack ÷ store (all titles 0.5) | 1.05 / 0.50 | 0.48 / 0.48 |
| Front-line racks 1961 (M) | 35.0 / 32.6 | 31.2 / 30.9 |
| Stores 1961 (M) | 65.2 / 66.7 | 63.5 / 68.3 |

- The bound works in the intended direction on both seeds. The biggest rack titles fall by a third to a half, and no
  title sells more than about 3× its realized store units. The bound is taken against store intent before the retail
  envelope trims it, so realized ratios run above K.
- Front-line racks lose 2–4M in 1961, because `--rack-shopper-scale` was fitted with the anomaly inside it. A lift from
  0.026 to about 0.029 would return that volume to act titles. Not screened.
- Artistless titles are still rack-heavy (13.5–15.5% of rack units on 6% of store units). That is the 0.9 genre weight,
  and it is at the level of Easy Listening act titles.

**Second Classical problem: the overall share.** Roster-act Classical is 4–7% of store LP units and its rack-to-store
ratio is at the all-genre level. The only figure found for the period is Schickel (1960): $85M of $425M spent on
recordings, 20% of dollars, year and source unstated
([Sandow](https://artsjournal.com/sandow/2006/12/past_popularity.html)); a Time piece also says about 20%. Nothing by
channel was found. Raising the Classical album affinity is the wrong lever: at 0.60 it gives 7–11% of units but 24–26%
of the album top 50 on one seed (`LpGenreAllocationFollowup.md` §6). The route that fits is more Classical titles at
low per-title demand, as for Jazz. Not built.

**Issue 3. Budget per-title volume.** Runs `fh-v1-2002-{bb,bk}`, 208 weeks, against `bud-v2-2002-lwbr`. Everything is
identical through 1961; own-master editions start in 1962.

| Seed 2002 | Control | `lwbr` | `bb` (M = 1) | `bk` (K = 100k) |
|---|---|---|---|---|
| Own-master editions that sell | 93 | 212 | 1,003 | 165 |
| Budget units from sources with 100k+ store sales | 56% | 63% | 47% | 83% |
| Top 20 editions: median units | | 462k | 126k | 479k |
| Top 20 editions: median ÷ source store LP | 3.8× | 2.6× | 0.55× | 1.9× |
| Largest edition ÷ source (editions over 20k) | | 12.0× | 1.6× | 9.7× |
| Median selling edition | | 103k | 17.5k | 143k |
| Budget 1962 / 1963 (M) | 21.4 / 21.7 | 16.8 / 27.2 | 15.7 / 20.4 | 16.1 / 26.3 |
| Stores 1963 (M) | 67.8 | 61.1 | 66.0 | 63.0 |
| Bankruptcies (208 weeks) | 124 | 119 | 118 | 117 |

- **The source bound removes the anomaly; K = 100k does not.** Under `bb` no edition outsells its full-price LP by a
  multiple and five times as many editions sell. The ratios under 1 are by construction. The 1.6× maximum is the gap
  between the regional store ledger the bound reads and the pressing-book figure scored here. Under `bk` sales history
  counts again, so the shelf returns to former hits (83%) and a flop can still sell 9.7× its LP.
- **It also answers most of issue 4's 1963 thinning.** Budget returns to 20.4M (control 21.7M), and inside the full
  envelope stores recover 5M of the 6–7M they had lost.
- **Cost.** 1962 budget volume is 15.7M against the control's 21.4M, so the gap to NARM's 21% of 1962 rack dollars is
  not closed. M = 1 is a scenario value; no source gives a budget-to-original sales ratio.

**Verdict:** `--budget-source-bound=1` is the per-title bound the `lwbr` package was waiting for. Recommend it with the
package. `--rack-store-standing-bound=2` works at 104 weeks but does not hold at 208 (2.11), so it is not recommended
as built. `--budget-appeal-saturation=100000` is rejected as the fix.

Research still open (handoff questions 3–4): budget LP unit share 1960–63, and rack against store sales of soundtracks.
Found only: RCA Camden, the largest low-price line, at $8–9M a year in 1963 and Pickwick at over $5M
([Billboard, 24 August 1963](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1963/Billboard%201963-08-24.pdf),
seen through a search excerpt); the *West Side Story* soundtrack at about 2.5M in 1962 across all channels
([Wikipedia](https://en.wikipedia.org/wiki/West_Side_Story_(1961_soundtrack)), figures conflict).

### 2.11 Combined arm `all`: `lwbr` + genre affinities + rack bound + budget source bound (2026-11-15)

`all` = `lwbr` with `--lp-demand-scale=1.8`, `--album-affinity=Folk=0.44,BossaNova=0.40,TraditionalPop=0.41,
EasyListening=0.28,Classical=0.60` (`LpGenreAllocationFollowup.md` §6), `--rack-store-standing-bound=2` and
`--budget-source-bound=1`. Runs `fh-v1-{2002,1001}-all`, 208 weeks, against `cand-v3-*-off` and `bud-v2-*-lwbr`.
Seed 2002 / 1001.

| | Control | `lwbr` | `all` |
|---|---|---|---|
| 1961 stores / racks / budget (M) | 65.1/34.7/16.5; 65.6/31.7/17.1 | 65.2/35.8/14.0; 66.7/33.3/14.3 | 63.6/32.7/13.5; 62.2/31.2/14.4 |
| 1962 stores / racks / budget (M) | 68.6/37.9/21.4; 73.6/34.3/20.4 | 68.8/39.3/16.8; 73.0/36.3/17.2 | 70.1/38.1/15.5; 71.7/36.3/16.1 |
| 1963 stores / racks / budget (M) | 67.8/39.9/21.7; 72.3/36.3/21.1 | 61.1/41.6/27.2; 65.0/37.4/27.6 | 67.6/39.4/21.2; 71.3/36.7/20.5 |
| Record gross 1961 / 1963 ($M) | 468 / 461; 461 / 461 | 468 / 455; 469 / 453 | 456 / 459; 451 / 460 |
| Median LP units, act's best single under 1k | 15.7k / 17.9k | 12.5k / 13.8k | 12.4k / 13.5k |
| Median LP units, act's best single 100k+ | 41.2k / 36.2k | 42.1k / 42.5k | 46.2k / 38.6k |
| LPs losing money | 0.1% / 0.1% | 1.2% / 1.4% | 1.0% / 1.3% |
| Top 10% share of LP units | 44.6 / 44.0 | 52.9 / 49.7 | 49.4 / 47.7 |
| Own-master editions that sell | 93 / 121 | 212 / 198 | 1,039 / 1,005 |
| Budget units from sources with 100k+ store sales | 56% / 55% | 63% / 63% | 45% / 44% |
| Top 20 editions: median ÷ source store LP | 3.8× / 4.2× | 2.6× / 2.9× | 0.3× / 0.6× |
| Artistless share of launched-LP rack units | 13.4% / 9.7% | 18.5% / 12.6% | 14.5% / 14.1% |
| Top rack title, rack / store (k) | 3,490 / 349; 1,850 / 300 | 1,799 / 92; 1,270 / 447 | 1,581 / 328; 2,188 / 492 |
| Bankruptcies (208 weeks) | 124 / 124 | 119 / 139 | 128 / 121 |

Year-end album top 50 (`analyze_album_chart_head.py`), 1960 / 1961 / 1962 / 1963:

| Top 50 % | Author's estimate | `all`, seed 2002 | `all`, seed 1001 |
|---|---|---|---|
| Folk | 13 / 6 / 18 / 18 | 12 / 22 / 24 / 2 | 10 / 14 / 14 / 6 |
| TradPop | 30 / 28 / 24 / 36 | 34 / 46 / 38 / 32 | 20 / 18 / 20 / 26 |
| Easy Listening | 6 / 12 / 10 / 2 | 18 / 6 / 4 / 6 | 8 / 10 / 0 / 4 |
| Comedy | 11 / 6 / 6 / 8 | 16 / 14 / 0 / 0 | 12 / 26 / 6 / 0 |
| Soundtrack | 7–14, about 25 in 1962–63 | 12 / 8 / 8 / 16 | 12 / 4 / 2 / 6 |
| Bossa Nova | – / – / 2 / 4 | – / – / 8 / 18 | – / – / 22 / 18 |
| Classical | not estimated | 8 / 4 / 2 / 2 | 28 / 22 / 12 / 4 |

Folk is 8.7–9.3% of the store units of 1960–61 launches (control 23–29%), 6.9–9.6% of 1961 rack units (27–38%) and
9–11% of budget units (56–61%).

- **Channel totals are back at the control in all four years**, within 1–3M a channel, including 1963. The 1963 store
  thinning is gone (67.6M / 71.3M against the control's 67.8M / 72.3M). 1961 stores are 2–3M under and 1961 gross is
  2–3% under; 1962–63 gross matches.
- **Earned LP demand keeps its effect**: an unknown act's LP sells 12.4–13.5k, 1.0–1.3% of LPs lose money.
- **Folk is fixed for 1960–62 in every channel, and now under in 1963** (2–6% of the top 50 against 18). The
  over-share was a 1960–62 problem. Folk's baseline falls after 1962 and the control's 1963 Folk head was already near
  the estimate, so a flat affinity cut takes too much from 1963. That needs a year-keyed value or a 1963–64 baseline
  lift.
- **Bossa Nova at 0.40 is still over at the head** (18–22% in 1962–63 against 2–4) while its units are 3–5%. Its
  shaped affinity clamps from 1962; it needs a lower value than Folk's.
- **Classical 0.60 overshoots on seed 1001** (28 / 22 / 12). Drop it from the set.
- **Comedy is over in 1960–61 on both seeds** and absent from 1962.
- **The rack bound does not hold at 208 weeks.** One artistless title per seed still sells 1.6–2.2M on racks at 4–5×
  its store units, and the artistless share of rack units is at the control's level, not below it. The bound is taken
  against store intent, and a long-lived title's store intent stays high while the retail envelope trims what it
  sells. The cause to fix is the title's rack pool (the 0.9 genre weight and awareness-driven exposure), or the bound
  should use realized store units.
- **Bankruptcies** are +4 / −3 against the control.

**Switch table and verdicts** (all off by default, all inert when off):

| Switch | Status after this pass |
|---|---|
| `--lp-earned-discovery=0`, `--lp-earned-launch`, `--lp-buyer-word-of-mouth=10000` | Recommended (package) |
| `--budget-noncurrent-weeks=156` | Recommended (package) |
| `--budget-appeal-saturation=25000` | Recommended (package); 100000 rejected |
| `--budget-rank-by-weight` | Recommended with the source bound |
| `--budget-source-bound=1` | **Adopted** with the package; closes issue 3 and the 1963 store thinning. M is a scenario value |
| `--lp-demand-scale` + `--lp-demand-scale-stores-only` | Recommended; 1.6 without the genre set, 1.8 with it |
| `--album-affinity=Folk=0.44,TraditionalPop=0.41,EasyListening=0.28` | Screened on two seeds; fixes 1960–62. Open: Folk 1963, Comedy |
| `--album-affinity` BossaNova=0.40 | Too high; rescreen at about 0.25 |
| `--album-affinity` Classical=0.60 | Rejected (head overshoot on one seed) |
| `--rack-store-standing-bound=2` | Works at 104 weeks, not at 208. Not recommended as built |
| `--album-chart-history` | Telemetry; add to candidate runs that score the album head |
| `--lp-label-demand-parity` | Rejected (2.4) |
| `--lp-demand-scale-known` | Rejected (2.13): Known is weekly heat, so hit acts lose the scale too |
| `--lp-floor-probe` | Diagnostic only (2.14) |
| `--lp-launch-standing=30000` + scale 2.0 | Candidate (2.15); needs a 208-week pair |

**Adopted 2026-11-15** (in `candidate-request.json`): the `lwbr` package plus `--budget-source-bound=1`, that is
`--lp-earned-discovery=0 --lp-earned-launch --lp-buyer-word-of-mouth=10000 --budget-noncurrent-weeks=156
--lp-demand-scale=1.6 --lp-demand-scale-stores-only --budget-appeal-saturation=25000 --budget-rank-by-weight
--budget-source-bound=1`. On seed 2002 (`fh-v1-2002-bb`, 2.10) that set holds stores, racks and budget near the control
through 1963 and needs no genre decision; it has not been run alone on seed 1001. The genre set is the next step, after the Folk
1963, Bossa Nova and Comedy values are settled on a 208-week pair, then the decade A/B on one paired seed.

### 2.12 Why unknown acts still sell LPs on the adopted candidate (2026-11-15, analysis only)

Runs `fh-v1-2002-bb` (= the adopted candidate), `cand-v3-*-{off,lw}`, `bud-v1-*-lwb`, `bud-v2-2002-lwbr`. LPs launched
weeks 1–52, lifetime units at week 208; "unknown" = the act's best single sold under 1k.

**Who they are.** 372 LPs, median 12.4k. 356 are standalone LPs by acts with no single at all, 369 are NewSignings,
and 365 never reach the album chart, so this is off-chart demand that never stops. They sell most in LP-native genres
(Comedy 33k, Folk 28k, Children's 24k, Blues 20k, Easy Listening 19k; Soul 7.6k, Country 5.6k, Rock and Roll 3.2k).

**1. The volume scale gave most of the earned cut back to unknowns.** Medians by the act's best single, seed 2002 / 1001:

| | `lw` (earned demand, no scale) | `lwb` (+ scale 1.6) | Gain |
|---|---|---|---|
| Under 1k | 6.9k / 7.5k | 12.1k / 12.7k | 1.77× / 1.69× |
| 1k–100k | 9.0k / 9.5k | 14.0k / 15.4k | 1.56× / 1.62× |
| 100k+ | 36.9k / 35.7k | 45.9k / 44.2k | 1.24× / 1.24× |

`--lp-demand-scale` multiplies conversion, and an unknown act's LP never gets near its buyer pool, so it takes the whole
1.63×. A hit LP is held back by exhaustion (`1 / (1 + 4 × penetration)`, floor 0.15). A faster sale uses up its pool
sooner, so it gains only 1.24×. In 2.6b the plan was for the scale to give the volume "back to known acts". It gave it
back mostly to unknown acts instead.

**2. What the earned switches leave is the label, and it does not depend on the act.** For an act with Known = 0, the
earned switches remove the 0.04 launch base and the unconditional discovery term. What is left:
- launch awareness = marketing impact + reputation × 0.1 (`ApplyPromotionSnapshot` / the AI launch path);
- awareness growth of `push × 0.03` a week (`AlbumSimulator.UpdateAlbum`) and regional growth of `push × 0.018`
  (`UpdateRegionalState`). *Correction (2.14): under the candidate the push ends with the programme campaign window
  (`ProgrammePushFactor` is 0 after `CampaignWeeks`), so the 0.12 tail never runs;*
- the tier purchase constant, Major 1.36 down to Small 0.50 (finding 9).

Unknown-act median by label tier, candidate seed 2002: Major 41k, MidTier 37k, Independent 11k, Small 5k. Under `lw`,
before the scale: Major 23k / 29k, Small 3.2k / 3.1k. An unknown act on a Major sells more than the median LP of the
whole cohort (16k).

**3. Conversion does not depend on the act.** `0.095 × quality^2.5`, × (0.72 + word of mouth × 0.45), × (0.85 + sentiment
× 0.25). With buyer word of mouth an unknown act's LP loses at most the word-of-mouth part of the 0.72–1.17 range.

**Budget editions sell unknown acts too.** On `fh-v1-2002-bb`, 849 own-master editions of 1960 LPs sell 17.3M:

| Source act's best single | Editions selling | Units (M) | Share | Median edition | Median edition ÷ source |
|---|---|---|---|---|---|
| Under 1k | 144 | 1.78 | 10% | 14.3k | 0.58 |
| 1k–100k | 407 | 7.09 | 41% | 15.5k | 0.62 |
| 100k+ | 298 | 8.46 | 49% | 21.9k | 0.41 |

The budget weight is `sqrt(saturated appeal) × quality^2.5 × taper × exhaustion × age decay`. Nothing in it depends on
the act. With saturation at 25k, a 12k-unit LP has appeal 8.1k and a 100k-unit LP has 20k, so in `sqrt` terms the hit
is only 1.6× ahead and quality chooses the titles. The source bound limits each edition to its own LP's sales, but it
does not change the choice. In the sources (2.8A), Camden reissued "older recordings by currently popular artists":
budget lines sold on the name. Before the bound (`lwbr`), 77% of these units came from hit acts.

**Fix directions (not built):**
- A volume restore that goes through act standing, not flat conversion. For example, apply the scale × Known so it
  falls on acts people know, or let a hit's buyer pool reach outside its genre so hits can take the volume.
- An LP's push scaled by Known, like the launch base. (The 0.12 tail already ends under the candidate; see 2.14.)
- Act standing (current recognition or fame at reissue) in the budget weight, alongside or in place of `quality^2.5`.

Constraint: under `lw`, taking act-blind LP income away without moving it raised bankruptcies by 28–43%. Hits are
held back by exhaustion, so any cut to unknown acts needs a place for the volume to go.

### 2.13 `--lp-demand-scale-known`: the store scale through Known (2026-11-15) — rejected

New switch (`LpDemandCalibration`, off by default): an act's LP takes store scale `1 + (S − 1) × Known`, where Known is
`LpEarnedDiscovery.Known` on the act's chart heat and release-awareness bonus that week. An LP with no roster act takes S.
Needs the scale. Saved with the world and checked on resume. The scale's upper bound is now 3 (was 2). Checks: 32 LP
calibration and 45 budget pass. `kn-v1-2002-off` (the new candidate, 27 weeks) matches `fh-v1-2002-lref` byte for byte
in 94 weekly streams; five more differ only in rows written at close, as in 2.10.

Runs `kn16-v1-{2002,1001}-kn` (S = 1.6) and `kn25-v1-{2002,1001}-kn` (S = 2.5), 104 weeks, on `candidate-request.json`
plus `--album-chart-history`. Control: `fh-v1-*-lref` (`lwbr` at 104 weeks; the source bound does nothing before 1962).
LPs launched weeks 1–52, lifetime at week 104 (`analyze_lp_known_scale.py`). Seed 2002 / 1001:

| | Control | S = 1.6 | S = 2.5 |
|---|---|---|---|
| LP median, act's best single under 1k (k) | 13.2 / 13.8 | 8.3 / 9.3 | 10.2 / 10.5 |
| LP median, best single 1k–100k (k) | 13.9 / 16.2 | 10.0 / 10.1 | 10.5 / 11.1 |
| LP median, best single 100k+ (k) | 36.6 / 36.5 | 28.8 / 29.1 | 27.9 / 32.9 |
| Hit-to-unknown gap | 2.77 / 2.64 | 3.47 / 3.13 | 2.72 / 3.14 |
| Unknown act on a Major / Small (k) | 41.8 / 5.4; 41.6 / 5.1 | 32.0 / 4.3; 31.8 / 3.9 | 33.8 / 4.0; 32.8 / 3.6 |
| LPs losing money | 1.2% / 1.4% | 3.0% / 3.9% | 2.8% / 3.9% |
| Top 10% share of LP units | 41.2 / 39.5 | 43.8 / 43.7 | 46.6 / 45.8 |
| 1961 stores / racks (M) | 65.2/35.0; 66.7/32.6 | 48.6/35.4; 52.4/33.5 | 54.1/37.4; 59.2/34.0 |
| Record gross 1961 ($M) | 419 / 420 | 379 / 386 | 398 / 402 |
| Bankruptcies (104 weeks) | 86 / 96 | 98 / 117 | 91 / 119 |

- **Hit acts lose the scale too.** At S = 1.6 the 100k+ median falls 20–21%, about what removing the scale entirely
  would cost a hit, so their Known averages well under 1 over an LP's sales life. Chart heat counts only while a record
  is on the chart (0.3 for a Top 10 position, 0.1 per Top 10 peak), and the release bonus is
  `momentum × 0.5 + reputation × 0.3 + recognition`, against a full-known mark of 0.30. Raising S to 2.5 lifts the few
  acts charting that week and the artistless titles (best LP 477k on seed 1001), not the hit band (median 28–33k).
- **The gap barely moves** (2.7–3.5× against 2.6–2.8×) and **volume is lost**: 1961 stores fall 7–17M, gross 4–10%,
  bankruptcies rise 5–23.
- **The unknown floor is the label.** Without any scale, an unknown act on a Major still sells 32k and on a Small 4k.
- The same Known drives `--lp-earned-discovery` and `--lp-earned-launch`, so those switches also treat most hit acts as
  partly unknown. That fits 2.6b, where `le` cut hit acts' LPs by 14%.

**Verdict:** rejected as built. Two things follow. Act standing for LP demand needs a durable measure (the act's sales
or chart record, or its recognition stock), not this week's heat. And the unknown-act floor sits on the label side,
which a scale cannot reach; 2.14 finds it is mostly the label's launch awareness.

### 2.14 Strong one-cause probes on the unknown-act LP floor (2026-11-15)

Diagnostic switch `--lp-floor-probe=a,b,...` (`LpFloorProbe`, not for a candidate). Each named cause is switched off
for every LP with a roster act; artistless LPs are untouched. `launch` = no label launch awareness (marketing +
reputation × 0.1, both launch paths). `push` = label push grows no awareness (record × 0.03, regional × 0.018). `tail` =
no push after week 12. `tier` = LP tier constant 1. `wom` = no 0.72 floor on the word-of-mouth multiplier. `label` =
launch + push + tier. Runs `fp-{ctl,launch,push,tail,tier,wom,label}-2002-off`, 52 weeks, seed 2002, on
`candidate-request.json` + `--album-chart-history`. `fp-ctl` matches `kn-v1-2002-off` in 94 streams over 27 weeks. LPs
launched weeks 1–26, lifetime at week 52 (`analyze_lp_known_scale.py --launch-weeks=26`).

| | Control | launch | push | tail | tier | wom | label |
|---|---|---|---|---|---|---|---|
| LP median, best single under 1k (k) | 12.6 | **5.8** | 9.6 | 12.6 | 17.8 | 0.0 | 6.9 |
| LP median, best single 1k–100k (k) | 15.0 | 8.7 | 14.2 | 15.0 | 25.0 | 0.0 | 9.2 |
| LP median, best single 100k+ (k) | 34.6 | 26.5 | 29.8 | 34.6 | 35.2 | 1.8 | 25.7 |
| Unknown-act change / hit change | | **−54% / −23%** | −24% / −14% | 0 / 0 | +41% / +2% | −100% / −95% | −45% / −26% |
| Hit-to-unknown gap | 2.74 | **4.54** | 3.10 | 2.74 | 1.98 | – | 3.73 |
| Unknown act on a Major / Small (k) | 38.0 / 6.9 | 17.3 / 2.7 | 34.1 / 4.2 | 38.0 / 6.9 | 33.1 / 11.4 | 0 / 0 | 11.1 / 3.4 |
| LPs losing money | 1.0% | 5.9% | 2.0% | 1.0% | 0.2% | 73% | 12.6% |
| 1960 stores (M) | 40.3 | 29.2 | 36.9 | 40.3 | 44.9 | 2.1 | 30.0 |
| Bankruptcies (52 weeks) | 51 | 53 | 58 | 51 | 55 | 107 | 59 |

- **Launch awareness is the cause.** Removing the label's launch awareness halves an unknown act's LP (−54%; on a
  Major 38k to 17k) and costs hit acts −23%, so the gap goes from 2.7× to 4.5×. Nothing else is that large or that
  selective. Why the launch matters so much: an LP's awareness never decays. For an unknown act nothing after launch adds
  much (no discovery term under the earned switches, push only in the campaign window, little word of mouth), so the
  launch stock sets the LP's lifetime demand, and catalogue decay is the only clock.
- **Push is second** (−24% / −14%), all of it inside the campaign window.
- **The tail does not exist under the candidate.** `fp-tail` matches `fp-ctl` byte for byte in 99 streams over 52
  weeks. `ProgrammePushFactor` returns 0 after the record's programme campaign (`CampaignWeeks`), so the 0.12 tail
  is multiplied by 0. (2.12 is corrected.)
- **The tier constant does not hold unknown acts up; it holds them down.** Most unknown acts are on Independent and
  Small labels, so a constant of 1 raises their median by 41% (Small 6.9k to 11.4k) and lowers Major unknowns by 13%.
- **Word of mouth hardly tells LPs apart.** Without the 0.72 floor, every LP collapses, hits included (−95%). Under buyer
  word of mouth an LP's multiplier is about 0.72 plus a few points, so it is a common constant, not a source of the
  unknown floor.
- **Hit acts lean on launch awareness too** (−23%), because their own release bonus is small (2.13). A fix that cuts
  only the label's launch for unknown acts needs a durable standing measure, or hit acts pay as well.

One seed at 52 weeks; the launch effect (−54%) is well above seed noise. Next: scale the label's launch awareness on an
LP by a durable act-standing measure, then screen at 104 weeks on two seeds.

### 2.15 `--lp-launch-standing=K`: the label's LP launch awareness by what the act has sold (2026-11-15)

New switch (`LpLaunchStanding`, off by default). On an act's LP, the label's launch awareness (marketing impact +
reputation × 0.1, both launch paths) is multiplied by `units / (units + K)`. Here `units` is everything the act has
sold: its credited career total (`SimulatedArtist.totalUnitsSold`, credited when a record leaves the chart) plus its
live records not yet credited (`ChartManager.UncreditedLiveUnits`, which includes prewarmed opening records). A debut LP
by an act with nothing sold gets no label launch awareness. An LP with no roster act is untouched. Read once at launch,
no saved state. The labels' forecast (`ProjectLaunchAwareness`) is not changed, as with `--lp-earned-launch`.
Checks: gamey switches pass; `ls-v1-2002-off` (27 weeks) matches `fh-v1-2002-lref` in 94 streams, with the usual five
rows written at close.

At K = 30k an act with 10k units keeps 25% of the label's launch, 100k keeps 77%, 1M keeps 97%. Runs
`ls30-v1-{2002,1001}-off` (store scale 1.6, as in the candidate) and `ls30s20-v1-{2002,1001}-sc` (scale 2.0), 104
weeks, on `candidate-request.json` + `--album-chart-history`, against `fh-v1-*-lref`. LPs launched weeks 1–52, lifetime
at week 104. Seed 2002 / 1001:

| | Control | K = 30k, scale 1.6 | K = 30k, scale 2.0 |
|---|---|---|---|
| LP median, act's best single under 1k (k) | 13.2 / 13.8 | 7.4 / 8.3 | 8.4 / 10.8 |
| LP median, best single 1k–100k (k) | 13.9 / 16.2 | 9.4 / 10.2 | 11.6 / 12.1 |
| LP median, best single 100k+ (k) | 36.6 / 36.5 | 32.0 / 31.7 | **41.5 / 37.7** |
| Hit-to-unknown gap | 2.77 / 2.64 | 4.34 / 3.83 | **4.92 / 3.49** |
| Unknown act on a Major / Small (k) | 41.8 / 5.4; 41.6 / 5.1 | 26.2 / 3.7; 26.5 / 2.7 | 33.3 / 4.5; 40.7 / 5.0 |
| LPs losing money | 1.2% / 1.4% | 4.7% / 6.1% | 3.4% / 2.4% |
| Top 10% share of LP units | 41.2 / 39.5 | 46.0 / 43.9 | 45.2 / 42.8 |
| 1961 stores / racks (M) | 65.2/35.0; 66.7/32.6 | 51.5/36.2; 54.2/34.4 | **62.6/35.3; 67.1/32.1** |
| Record gross 1961 ($M) | 419 / 420 | 389 / 390 | **416 / 419** |
| Bankruptcies (104 weeks) | 86 / 96 | 92 / 102 | 95 / 98 |

- **It does what the Known-weighted scale (2.13) could not.** At scale 1.6, unknown-act LPs fall 40–44% while hit acts
  lose 13%. Hit acts have sold enough to keep most of the launch, so a volume restore now lands on them.
- **With scale 2.0 the package holds volume.** 1961 stores are within 3M of the control, gross is flat, and acts with a
  100k+ single gain 3–13% while unknown acts lose 22–36%. The gap goes from 2.6–2.8× to 3.5–4.9×.
- **Bankruptcies:** +9 / +2 at scale 2.0, +6 / +6 at 1.6, in 104 weeks.
- **What is left of the unknown floor** is on the label side but outside the launch: push in the campaign window
  (2.14: −24%) and the Major tier constant. A brand-new act's first LP on a Major still sells 31–35k
  (first LPs without pre-1960 history or opening records), against 3.7–7.4k on an Independent. Medians over 30–45 Major
  LPs move a lot between arms, so read the Major row as rough.
- **LP-only acts gain standing from their own records.** An act's second LP in the run outsells its first (seed 1001:
  25k against 9k), because the first LP's units count. That is intended.

**Verdict:** `--lp-launch-standing=30000` with `--lp-demand-scale=2.0` is a candidate. It is the first change that
lowers unknown-act LPs while holding store volume and gross and raising hit-act LPs. Before adoption it needs a 208-week
pair: the envelope binds from 1962, budget editions start in 1963, and bankruptcies need the full window. Open: the
label forecast still assumes the full launch, so labels may over-plan unknown-act LPs; push by standing is the next
lever if the floor should fall further.

### 2.16 LP sales distribution against the RIAA 1965 release survey (2026-11-15, analysis only)

Source: `SimTools/RIAA-1965-LP-sales-research-notes.md` (Glover / Cambridge Research Institute for RIAA, House
Judiciary hearings, June 1965, pp. 797–799 and 861–865). 1,603 non-classical LPs released in 1963 by Capitol,
Columbia, RCA Victor, Mercury, Laurie and Kapp, with sales counted through 1964. Model analogue
(`analyze_lp_riaa_1965.py`): non-classical LPs launched weeks 1–52, units at week 104, store + rack (clubs are logged by
label only and are left out). Runs `fh-v1-{2002,1001}-lref` (control) and `ls30s20-v1-{2002,1001}-sc` (2.15 candidate).

| | RIAA 1963 | Model, all tiers | Model, Major | Model, Major debut |
|---|---|---|---|---|
| Under 4k | **48.8%** | 9–16% | **0%** | 0% |
| Under 8k | 61.7% | 23–34% | 0–2% | 0–3% |
| Median | 4–8k band | 13–18k | **65–83k** | 70–73k (control), 37–38k (2.15) |
| 600k or more | 0.6% | 0% | 0% | – |
| Losing money | 61% (break-even estimate, avg 7.8k) | 1–5% | 0–2% | 0–6% |

"Debut" = the act's first LP in the run, scheduled as NewSigning (two thirds of launched LPs).

- **The RIAA sample is mostly majors.** If as many as 300 of the 1,603 releases were Laurie or Kapp and all sold under 4k,
  at least 37% of the four majors' LPs still did (a bound, not a source figure). The model's Major tier has none. Its
  Small tier (median 5k, 28–42% under 4k) is the one that looks like the RIAA sample.
- **Mean volume is about right; the shape is wrong.** The grouped RIAA mean lies between about 19k and 38k (band floors /
  ceilings, 1M+ at 1M). The model's is 41–44k counting racks, 28–32k stores only. The model has too few flops, no title
  above 600k, and a Major floor about 10× too high. A fix should move Major volume from flops to hits, not remove it.
- **2.15 halves Major debuts** (about 72k to 37k), still 5–9× the RIAA median band and still 0–3% under 8k. What remains
  is campaign-window push and the tier constant (2.14).
- **Unit costs match p. 864:** mechanicals 2¢ × tracks (≈ $0.22 against $0.24), pressing $0.40 + packaging (against
  $0.45), royalty 4–5% of list (against $0.15), net ≈ $1.92 a mono copy (against $1.818; stereo nets more). **The
  per-copy remainder does not:** retired LPs realise $1.13–1.32 a copy after unit costs against RIAA's $0.45 after
  SG&A, so Independent / Small LPs break even at 1–2k copies. Major break-even is about 8.3k, close to RIAA's 7.8k;
  Majors almost never lose because they almost never sell under it. (Retired LPs only: biased to low sellers, 12–13
  Major titles.) Probably no per-copy selling and overhead charge on records; not verified.
- Caveats: the source has no debut split. The model cohort is its 1960 warm-up year against RIAA's 1963.

**Scoring for Major LP fixes:** Major-tier share under 4k and under 8k, median band, share at 300k+, holding mean units
per release and 1961 store / rack totals.

### 2.17 Bringing Major LPs toward the RIAA bands: label push by standing, and a reception draw (2026-11-15)

**Why Majors sit high (on `ls30s20-v1-2002-sc`).** Within a tier, log units have sd about 1.0 against RIAA's about
2.0 (a lognormal fit to the p. 863 bands). Once act, genre and label are fixed, an LP's demand barely varies: quality
correlates only 0.21 with Major log units, and the lowest-selling Major Folk LP still sells 19k. Most of the spread
between Major LPs comes from career state and genre (Superstar 444k, Star 245k, Established 134k, NewSigning debut
37k; Easy Listening 188k, Soul 13k). Majors issue 36–66 LPs each in 1960 and 80–100 in 1961, and their mean is about
130k per LP, roughly 4× RIAA's sample mean.

New switches (`GameyAuditSwitches.cs`; both off by default; checks pass):
- `--lp-label-standing=push,tier` (needs `--lp-launch-standing=K`). The act's launch standing, units / (units + K), is
  stored on the LP (`RecordRuntimeData.lpLabelStanding`, saved). It scales the label's push growth (record and
  regional) and the part of the tier constant above 1 (Major 1.36 → 1 + 0.36 × standing). Constants below 1 are untouched.
- `--lp-reception-sigma=S`. Each LP with a roster act gets a mean-preserving reception multiplier on store conversion,
  `exp(S z − S²/2)`. The draw z is keyed on record id and world seed (`PhiloxCounterRandom`), so it consumes no RNG
  stream and needs no saved state. Forecasts do not see it. The basis is Glover's own heading for the data, "Producing
  and Selling Records Is a Risky Business".

Runs at 104 weeks on `candidate-request.json` + `--album-chart-history --lp-launch-standing=30000 --lp-demand-scale=2`
(the 2.15 candidate, `ls30s20`). Metrics are from `analyze_lp_riaa_1965.py --compact`: non-classical LPs launched weeks
1–52, store + rack units at week 104. `lps` = + push,tier (seeds 2002 / 1001). `rc15` = + reception σ 1.5, and
`lrc15` = both (seed 2002 only).

| | RIAA | 2.15 control | `lps` push,tier | `rc15` σ 1.5 | `lrc15` both |
|---|---|---|---|---|---|
| All: under 4k / 8k % | 49 / 62 | 16 / 34; 13 / 32 | 24 / 43; 26 / 43 | **41 / 56** | 49 / 61 |
| All: median (k) | 4–8 | 12.7; 15.2 | 10.3; 10.8 | **5.8** | 4.3 |
| All: log-sd | ≈ 2.0 | 1.35; 1.36 | 1.58; 1.66 | **2.04** | 2.40 |
| All: 300k+ % | 1.3 | 2.6; 2.4 | 2.6; 2.2 | 3.3 | 3.0 |
| Major: under 4k / 8k % | ≥ 37 (bound) / – | 0 / 1; 0 / 2 | 1 / 3; 2 / 7 | 11 / 21 | 16 / 26 |
| Major: median / mean (k) | (sample 4–8 / ≈ 31) | 74 / 131; 65 / 119 | 58 / 124; 46 / 108 | 35 / 137 | 30 / 111 |
| Major debut: median (k) | – | 37; 38 | 26; 23 | 15 | 13 |
| Major non-debut: median (k) | – | 98; 98 | 99; 96 | 53 | 44 |
| LPs losing money % | 61 | 3.3; 2.5 | 10.4; 13.0 | 30.6 | 39.8 |
| 1961 stores / racks (M) | – | 62.6/35.3; 67.1/32.1 | 54.6/33.8; 55.8/34.1 | 60.3/37.5 | 48.4/39.7 |
| Record gross 1961 ($M) | – | 416; 419 | 388; 395 | 415 | 388 |
| Bankruptcies (104 wk) | – | 95; 98 | 100; 110 | **140** | 141 |

- **Push and tier by standing only reach debuts.** The Major debut median falls by about a third, but non-debut Major
  LPs (median about 97k, none under 8k) do not move: their acts have standing near 1. The switch also costs 8–12M 1961
  store units and 25–30M dollars of gross. Not recommended alone.
- **Reception σ 1.5 reproduces the RIAA shape across the industry** (median, log-sd, under 4k within 8 points) and
  holds 1961 stores, racks and gross. It is the first lever that moves non-debut Major LPs (98k → 53k). The losing
  share rises from 3% toward RIAA's 61%, reaching 31%.
- **Its cost is +45 bankruptcies,** all at Independent / Boutique / Small (Independent 62 → 72, Boutique 12 → 29,
  Small 9 → 22); 98 of 140 come in a label's first year. A flop LP leaves pressings against a forecast that never sees
  the draw. That is the period's risk, but closures must be earned (memory `closures-must-be-earned-not-imposed`),
  so the cash side needs a look before adoption.
- **Majors are still about 5× too high per title.** The Major mean per LP (111–137k) is about 4× the RIAA sample mean,
  so no mean-preserving reshaping can bring the Major median into 4–8k while Major LP volume holds. The rest is volume
  per title: the model's Majors issue 40–100 LPs a year, against roughly 1,500 sample releases from four majors. That
  figure may count mono and stereo editions separately (p. 862 covers both), so it is perhaps 190–375 titles each. An
  author decision is needed: more Major LP titles, or less Major LP volume.

### 2.18 More Major LP titles: the sourced release rate, the real block, and the screen (2026-11-15)

**Historical rate (sourced).** These are counts of titled entries per release year in the main pop / jazz / cast series:
- **Capitol T/ST** (bsnpubs `capitol/previews/600-2999.pdf`, parsed): 163 titles (1959), 168 (1960), 149 (1961),
  156 (1962), 143 (1963), 160 (1964).
- **Columbia CL/CS** (bsnpubs `columbia/columbia12/columbia1400`–`1900.html`): about 145 in 1961 (CL-1400s 3, 1500s 35,
  1600s 93, 1700s 14) and about 209 in 1962 (1600s 6, 1700s 78, 1800s 90, 1900s 35). 1960 is about 150+ (1400s 91,
  1500s 61, plus early-1960 CL-1300s not counted).
- Both labels pair mono and stereo on one title (CL-1500/CS-8291; ST/T 1412), so these are titles, not editions. They
  exclude subsidiaries (Epic, Harmony, Okeh; Tower, Angel) and classical series.
- So each of the head majors issued about **145–210 titles a year in its main series alone.**
- **Consequence for 2.16:** four majors at about 150–200 titles each, plus Kapp and Laurie, comes to about 700–800
  titles. Doubled for mono and stereo editions, that matches the RIAA sample's 1,603 "releases". So RIAA's bands are
  most likely **per edition**, and per-title sales run up to about 2× the band values (less where stereo sold
  little). An inference, not stated on p. 862.

**What caps the model's Majors.** The Major programme envelope (`--programme-major-albums=5`, "a sensitivity
assumption, not an RCA LP policy"; ×2 by the candidate's `--lp-programme-scale=2`, so 10 a month) is not the cap.
Majors issued 52 a label in 1960 and 82 in 1961 against 120 of capacity. On `ls30s20-v1-2002-sc`, 1,128 Major LP
slots were planned and 620 issued. The rest expired after repeated `preparation-material-economics-or-funding`
postponements, which are the **all-in profit gate** (`release-profit-gates.csv`). Only 17% of Major LP attempts pass,
10% for NewSignings (forecast median −$5.3k), against 61–67% for Rising / Established and 95–100% for Star /
Superstar. Yet the Major debut LPs that are released sell a median 37k and almost never lose money. This is
`debut-lp-forecast-is-3x-low` on the Major side.

**Screen** (104 weeks, seed 2002, all on `ls30s20` = candidate + `--lp-launch-standing=30000 --lp-demand-scale=2`):
`ma12` = + `--programme-major-albums=12`; `ma12d` = `ma12` + `--album-prior-debut-scale=2.0613` (the 2026-11-03
debut prior fix); `ma12r` / `ma12rd` = the same + `--lp-reception-sigma=1.5`.

| | 2.15 control | `ma12` | **`ma12d`** | `ma12r` | `ma12rd` |
|---|---|---|---|---|---|
| Major LPs per label, 1960 / 1961 | 52 / 82 | 54 / 97 | **80 / 154** | 54 / 89 | 80 / 148 |
| Major LP gate pass % (all / NewSigning) | 17 / 10 | 13 / 8 | 36 / 33 | 12 / 7 | 39 / 37 |
| Major: under 4k / 8k % | 0 / 1 | 0 / 2 | 1 / 4 | 14 / 24 | **23 / 35** |
| Major: median / mean (k) | 74 / 131 | 52 / 111 | **37 / 79** | 27 / 111 | **18 / 86** |
| Major non-debut median (k) | 98 | 75 | 48 | 36 | 27 |
| Major LP units (M, cohort) | 27.8 | 25.3 | 28.6 | 25.4 | 30.3 |
| Major share of cohort units % | 46.6 | 44.1 | 47.5 | 46.4 | 51.0 |
| All: under 4k % / median (k) | 16 / 12.7 | 15 / 13.3 | 21 / 11.1 | 41 / 6.0 | 46 / 4.8 |
| All: log-sd | 1.35 | 1.32 | 1.38 | 2.00 | 2.19 |
| LPs losing money % | 3.3 | 3.1 | 7.1 | 31.0 | 38.2 |
| 1961 stores / racks (M) | 62.6 / 35.3 | 59.5 / 35.9 | **65.2 / 37.4** | 54.0 / 38.9 | 59.0 / 37.0 |
| Record gross 1961 ($M) | 416 | 408 | **427** | 403 | 407 |
| Bankruptcies (104 wk) | 95 | 86 | **94** | 124 | 130 |

- **The envelope alone does little** (`ma12`): the gate refuses the extra slots. It does lower the Major median, because
  the fixed campaign pool is split across more releases.
- **Envelope + debut prior (`ma12d`) reaches the historical rate at no cost.** Majors issue 154 LPs a label in 1961,
  inside the sourced 145–210. Each Major title sells about half as much (median 74k → 37k, mean 131k → 79k), Major
  LP volume and share hold, 1961 stores and gross rise slightly, and bankruptcies are flat.
- **Adding reception (`ma12rd`) brings the Major distribution near RIAA per title.** The median is 18k, 23% under
  4k and 35% under 8k; against per-edition bands roughly ×2, that is close. The all-label shape matches RIAA (46%
  under 4k, median 4.8k). The cost is +35 bankruptcies, at small labels (2.17).
- **Seed 1001** (`ls30s20` → `ma12d` → `ma12rd`):
  - Major LPs per label 1960 / 1961: 49 / 68 → 71 / 125 → 73 / 123.
  - Gate pass, all / NewSigning: 15 / 10% → 33 / 30% → 36 / 33%.
  - Major median / mean: 65k / 119k → 36k / 84k → **11k / 68k**. Under 4k / 8k: 0 / 2% → 1 / 5% → 28 / 43%.
  - Major LP units: 28.3M → 31.1M → 25.8M. Major share of cohort units: 45.8% → 51.7% → 50.7%.
  - 1961 stores: 67.1M → 68.2M → 59.4M. Gross: $419M → $434M → $413M.
  - Bankruptcies: 98 → **109** → 140.
  - `ma12d` repeats seed 2002, except for the bankruptcies (+11 here, −1 on 2002). Its Major share of LP units rises
    about 6 points on this seed.
- **Verdict:** `ma12d` (`--programme-major-albums=12 --album-prior-debut-scale=2.0613` on `ls30s20`) is a candidate.
  It needs a 208-week pair before adoption: the envelope binds from 1962, and both bankruptcies and Major share need
  the full window. `ma12rd` is closest to RIAA but carries the 2.17 small-label closures.
- **208-week pair** (`ls30s20-v2` against `ma12d-v2`, seeds 2002 / 1001; scored on 1962 releases with sales to week
  208, the RIAA window; `analyze_lp_riaa_1965.py --compact --launch=105-156 --last=208`):

  | | `ls30s20-v2` | `ma12d-v2` |
  |---|---|---|
  | Major LPs per label, 1960 / 61 / 62 / 63 | 52 / 84 / 115 / 133; 49 / 70 / 109 / 121 | 80 / 157 / **245 / 284**; 71 / 127 / **207 / 230** |
  | Major LP gate pass % (all / NewSigning) | 28 / 19; 25 / 19 | 45 / 44; 41 / 41 |
  | Major median / mean, 1962 releases (k) | 70 / 117; 48 / 95 | **26 / 58; 25 / 54** |
  | Major under 4k / 8k % | 0 / 1; 0 / 2 | 2 / 8; 1 / 7 |
  | Major share of cohort LP units % | 59.6; 64.5 | **66.2; 72.1** |
  | LPs losing money % | 9.0; 9.3 | 15.8; 16.1 |
  | Stores 1962 / 1963 (M) | 69.7 / 66.2; 71.6 / 70.3 | 72.8 / 69.5; 75.4 / 71.3 |
  | Gross 1962 / 1963 ($M) | 500 / 461; 506 / 466 | 507 / 468; 514 / 466 |
  | Bankruptcies (208 wk) | 124; 132 | **132; 150** |

  - Per-title Major sales halve again at 208 weeks, and volume and gross hold.
  - **Overshoot:** once the gate passes 41–45% of Major LPs, the 24-a-month envelope (12 × `--lp-programme-scale=2`)
    binds. Majors issue 207–284 a label in 1962–63, against the sourced 145–210. The control reaches 109–133 by
    then, so the 1960–61 gap is larger than the 1962–63 gap.
  - **Costs that replicate:** the Major share of LP units rises 7–8 points, and bankruptcies rise 8–18. From 1962 the
    extra Major titles take envelope from small labels.
  - Next: `--programme-major-albums=8` (16 a month effective, about 190 a year) with the debut prior, to keep the
    1960–61 lift and cap 1962–63 inside the range.
- **Envelope 8 a month (`ma8d-v2`) and the combined candidate (`cb-v1`)**, 208 weeks:
  - `cb` = `ma8d` + `--album-affinity=Folk=0.44,BossaNova=0.25,TraditionalPop=0.38,EasyListening=0.28,Comedy=0.50,
    Blues=0.40` at LP scale 2.2 (`LpGenreAllocationFollowup.md` §7).
  - Seed 2002 / 1001; 1962 releases, sales to week 208.

  | | `ls30s20-v2` | `ma12d-v2` | **`ma8d-v2`** | **`cb-v1`** |
  |---|---|---|---|---|
  | Major LPs per label 1960 / 61 / 62 / 63 | 52 / 84 / 115 / 133; 49 / 70 / 109 / 121 | 80 / 157 / 245 / 284; 71 / 127 / 207 / 230 | 80 / 150 / 204 / 208; 70 / 143 / 188 / 202 | 83 / 148 / 201 / 206; 69 / 142 / 182 / 188 |
  | Major median / mean (k) | 70 / 117; 48 / 95 | 26 / 58; 25 / 54 | 31 / 68; 32 / 70 | 29 / 62; 31 / 62 |
  | Major share of cohort LP units % | 59.6; 64.5 | 66.2; 72.1 | 64.9; 71.6 | 62.7; 65.1 |
  | LPs losing money % | 9.0; 9.3 | 15.8; 16.1 | 14.4; 15.5 | 10.5; 12.1 |
  | Stores / racks 1961 (M) | 62.6/36.0; 67.1/32.8 | 65.2/38.2; 68.2/37.4 | 65.3/35.5; 68.2/34.9 | 59.6/39.4; 63.2/36.7 |
  | Stores / racks 1963 (M) | 66.2/42.4; 70.3/39.4 | 69.5/41.0; 71.3/39.6 | 65.9/43.7; 70.7/39.2 | 62.2/46.5; 66.0/44.2 |
  | Gross 1961 / 1963 ($M) | 465 / 461; 469 / 466 | 476 / 468; 483 / 466 | 470 / 465; 477 / 465 | 464 / 462; 468 / 465 |
  | Bankruptcies (208 wk) | 124; 132 | 132; 150 | **117; 134** | 139; 138 |

  - **`ma8d` is the Major-LP fix.** Release counts sit inside the sourced 145–210 from 1961, per-title Major sales
    fall by a third to a half, gross holds, and bankruptcies match the control (−7 / +2). The Major share of LP units
    still rises 5–7 points.
  - **`cb` adds the genre fix** (the year-end head is in the follow-up doc §7) and takes back most of that share rise
    (62.7 / 65.1). It costs about 5M store LPs a year, mostly made up on racks, gross −$1–16M, and bankruptcies
    +22 / +4 against `ma8d`.
- **Open:** the non-debut Major median (27–48k) is still high. The Major mean (79–86k) is still about 1.5–2.5× the
  RIAA per-title mean, which is roughly 31k per edition, so maybe 45–60k per title.

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
   - *Sourced (2026-11-15)* from Saulnier & Jacoby, *Accounts Receivable Financing* (NBER, 1943), read from the chapter
     PDFs' text layer (nber.org/system/files/chapters/c4591 and c4593). Figures are for 1940–41.
   - **Advance share.** Non-notification lenders (the receivable is pledged, the customer is not told) advanced "from 70
     to 90 percent" of face value; only 42 of 357 clients in a mid-1941 survey got less than 70%. The worked example is
     80%. The margin must absorb returns and allowances, and the advance is "generally set so as to advance no more than
     the client's costs of goods sold" (ch. 5, pp. 84–86). Delinquent accounts are taken back 30–60 days past due.
   - **Charge.** Commercial finance companies charged 1/50 to 1/25 of 1% a day on the gross receivables held, most at
     1/40 of 1%. On money actually in use this is 9.7–19.5% a year at a 75% advance; 1/40 of 1% at 75% is 12.2% a
     year. Their estimate of the real range is about 9% a year for the best clients to 20% for the riskiest (ch. 7,
     pp. 132–133).
   - **Mapping to the switch.** The overdraft is the money in use, so the charge is **about 1.0% a month** (12.2% a year)
     and 0.75–1.7% across the range. The screen's 1.5% sits at the risky end. A record label's receivables carry heavy
     returns, which argues for the low end of the advance range: **0.70–0.75** is supported, 0.75 as screened is
     inside it. The cost-of-goods cap is not modelled.
   - Rates are 1940 rates. The prime rate was about 1.5% then and 4.5–5% in 1960, so a 1960 charge is likely higher than
     the 1940 one, which also points to the 1.0–1.5% a month end.
   - **Adopted 2026-11-15** at 0.75 and **1.25% a month**: `--receivable-overdraft-charge=0.0125` (the flag defaults to
     the screened 1.5% so older runs reproduce), in `SimLogs/writing-calibration/candidate-request.json`. Re-screen
     `lpe-v1-2002-rc` against `lpe-v1-2002-off` (104 weeks): bankruptcies 85 → 81, other operating cost (where the
     charge is booked) +5% / +20% in 1960 / 1961. Record gross −0.7% / −2.7% and Major-family chart share 0.50 / 0.59 →
     0.50 / 0.54 are within single-seed reshuffle noise (a cash change reshuffles later decisions).
   - No source on record-label distributor terms or pressing-plant credit turned up; Vee-Jay's "could not pay to press
     *Sherry*" (NPR, 2007) confirms the squeeze, not the terms.
5. **Is the yearly band-life pass a deliberate simplification?** Moving it to anniversaries changes keyed RNG ids, so it needs its own inertness plan.
6. **Major cash:** should a corporate division sweep surplus to its parent, and should anything a Major does be cash-limited?
