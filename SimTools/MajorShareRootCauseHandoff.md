# Major chart share: root-cause handoff (2026-11-13)

## The question

Why do large labels convert their singles into chart hits 2–6× better than small ones, when history says they converted about the same?

The answer should be found in the economy and label-AI systems: what a label's size buys at each step from signing to chart. Earlier work (`SimTools/MajorTop10ShareFableHandoff.md`) removed the obvious footprint terms one by one. None of them closed the gap on its own, and several traded slots between tiers without lowering the total.

## Targets (history)

- Big-5 (Columbia, RCA Victor, Capitol, Decca, Mercury) issued about **18%** of singles and held **20.6%** of 1961 Top 10 slot-weeks (21.2 / 26.0 / 18.7 for 1962–64). That is a per-single premium of about **1.2×**.
- Hit acts 1961–64: Top 10 per Hot 100 debut was **15.96%** at the Big-5 and **15.93%** elsewhere. The Big-5's edge was **cadence** (3.28 vs 2.18 debuts a year), not conversion (§17aa, `HistoricalLabelTierBalanceResearch.md`).
- Repeat hits came mostly from the field: acts per year with 2+ / 3+ Top 10 records, Big-5 5.6 / 3.6, field 8.8 / 2.2 (`repeat-hit-gap-is-independents` memory).
- 1960 is not a calibrated year (opening rosters and warm-up). Judge on 1961 onward.

**Scoring rule:** Big-5 = exactly `label_0001`–`label_0005`. Never compare label IDs as strings (procedural `gen_…` IDs sort below `label_`).

## Starting point

**Reference bundle:** `SimLogs/writing-calibration/major-scale-bundle-request.json`, 191 flags. The last five are:
- `--label-demand-parity=1`: the flat label purchase-rate scale.
- `--uncovered-sales-terms=10` and `--receivable-backed-overdraft=0.75`: one-stop terms for uncovered markets, and the overdraft.
- `--slate-going-concern-reserve-all-tiers` and `--programme-overdraft-funding`: the release-funding fix.

Earlier versions are kept beside it as `.pre-*.json`.

**Control:** `fund-v1-{2002,1001,3003,6006}-gc_of`. These are 104 weeks and lean, with flags identical to the bundle. Its 1961 numbers, seed 2002 / 1001 / 3003 / 6006:

| | 2002 | 1001 | 3003 | 6006 | Mean | History |
|---|---|---|---|---|---|---|
| Big-5 Top 10 share | 30.6 | 28.3 | 53.7 | 39.2 | 37.9 | 20.6 |
| Big-5 Top 40 share | 25.9 | 23.5 | 37.2 | 32.5 | 29.8 | |
| Big-5 Hot 100 share | 24.9 | 21.9 | 32.1 | 29.5 | 27.1 | |
| MidTier Top 10 share | 34.2 | 27.9 | 20.4 | 20.4 | 25.7 | |
| Big-5 share of launches | 14.8 | 13.0 | 15.7 | 14.4 | 14.5 | ~18 |
| Per-single Top 10 premium | 2.2× | 3.4× | 6.5× | 4.2× | | ~1.2× |

**The gap is realization, and it belongs to label size, not to the Major tier.** Singles launched weeks 1–92, `analyze_fit_lottery_by_tier.py`:

| Tier | Share of launches | Chance a single "connects" (`SingleCommercialFit`) | Top 40 per expected connect | Top 10 weeks per expected connect |
|---|---|---|---|---|
| Big-5 | 10–12% | 0.21–0.23 | 0.69–0.84 | 1.3–2.4 |
| MidTier | 8–10% | 0.22–0.24 | 0.67–0.83 | 1.5–2.2 |
| Independent | 37–41% | 0.15 | 0.34–0.37 | 0.44–0.74 |
| Small + Boutique | 40–41% | 0.14 | 0.21–0.24 | 0.25–0.33 |

- The hit lottery is close to tier-fair; the awareness tilt on it is small.
- MidTiers realize exactly like the Big-5. Big-5 plus MidTier hold about 60% of Top 10 slot-weeks on about 23% of launches.
- When a lever moves slots, check where they come from. Chart-proven wholesale lines (`cw`) took Hot 100 slots from MidTiers, and the Big-5 picked them up: Big-5 share rose on all four seeds.

## Already ruled out or used up

Do not re-screen these unless the code under them changes. Numbers are 1961 Big-5 Top 10 share, mean of seeds, against the control of the day.

| Lever | Result |
|---|---|
| Record production parity (`cp`) | Production is about 20% of the gap. It removes a real 1960 Major edge. Held. |
| Follower advance stock (`nas`) | Stars chart without it; restock covers demand. |
| Never-charted momentum penalty (`ncp`) | A symptom; 1961 unchanged. |
| Act-earned campaign impact (`aci`) | Seeds disagree. Small-label awareness is **budget-bound** (they spend ~$109 vs $334 a single), not impact-bound. |
| Label purchase-rate scale (`dp`) | **Adopted.** About 10–19 points, the largest single carrier found. |
| Earned coverage + arrival depth (`ec` + `ad`) | −8 at the Top 10 and same sign at every rung on two seeds (dp bundle). `ec` bypasses distribution. **Not rescreened on the current bundle.** |
| Open fit exposure at launch (`fx`) | Fails the regional stagger guard. Rejected. |
| Chart-proven wholesale lines (`cw`) | Crowds MidTiers; Big-5 up on 4/4 seeds. Rejected. |
| One-stop terms, overdraft, release-funding fix | **Adopted** as economics. Big-5 share flat. |
| Launch push saturation, senior cooling, hit-act hype, known-act airplay | Earlier sessions (§17n–§17v). Newcomer-only cuts fade by 1962. Artist-side cuts shift slots to label push. |

## Hypotheses to dig into (economy and label AI)

Ranked by how directly each could produce a size premium that survives `dp`. Each names where to look and what would confirm or kill it.

### 1. Selection: the slate picks the cream at large labels and anything at small ones

- Under `--slate-work-budget` a non-Major slot had about 4.7 scored candidates, so any act past the six-week floor gets one. The Majors stay slot-bound at about 122 candidates per slot (`MajorSinglesPerMonth` 10). That's from `major-share-is-career-mix-adult60-is-volume`, measured 1966–69; re-measure for 1960–61.
- If the slate's scoring can see anything correlated with the hidden hit draw (hook, commercial fit, act fame, material quality from the song pool), then large labels release the right tail of their supply and small labels release their median. Matched-quality comparisons would then understate it, because quality is only one of the inputs.
- **Look at:** `CompetitorManager.ReleaseProgramme.cs` (planning and scoring), `programme-choices.csv`, `release-slate.csv`, `release-judgements-shadow.csv`.
- **Measure:** P(selected | scored) by tier, and the hook / fit / awareness of selected versus unselected candidates by tier. Then the connect rate and realization of the selected singles at matched candidate pool size.
- **Kill test:** cap the Big-5 candidate pool at the field's size (or widen the field's), and check whether realization converges.

### 2. Material: who gets the best songs

- Professional songwriting offices and publishers pitch to labels. Hook persistence is the strongest per-act hit predictor (between-act variance share 23%, hook 21%; `hit-followups-lack-chart-access`). If the song market routes high-hook songs to large labels (by advance, reputation, office location or commission rules), the premium sits in the material, not the label.
- **Look at:** `ProfessionalWritingMarket`, office placement (`rock-hit-office-placement-trace` memory), material search (`programme-material-search.csv`), `material-mix-and-commission`.
- **Measure:** hook and track traits of released singles by tier and by source (self-penned / office / cover), and Top 40 rate by hook band within tier.
- **Note:** `song-material.csv` is empty under `--calibration`, so a traced run may be needed.

### 3. Signing: who gets the best acts

- Major new signings' first launches reach the Top 40 at 23.7%, against 15.3% for MidTiers and 6.3% for Independents. The Big-5 sign no senior acts. Their seniors are home-grown, climbing the shared career ladder.
- Check whether scouting and contract acceptance route latent talent (hook ability, stage fame, act identity) to the richest bidder. Major cash grows from $0.24M to $10.8M in 104 weeks (`gamey-behavior-audit-2026-11-12`), so money is never the constraint at the top.
- **Look at:** `RosterManager`, scouting (`scouting-mechanic-directive`), offer and acceptance code, `daily-talent-market.csv`, `artist-labor-market-weekly.csv`, `deal-ledger.csv`.
- **Measure:** latent traits of acts at signing by signing tier. Artist choice when two labels compete: is the acceptance probability tilted by advance or reputation, and how steeply?

### 4. Label-scaled launch terms still live after `dp`

Every place `tier`, `distributionStrength`, `nationalReach`, `marketingPower`, `reputation` or a tier budget enters a single's awareness, radio or stock is a candidate. Known live ones:
- `BudgetToImpact(budget, tier)` × campaign impact in launch awareness. Small labels' budgets are a third of the Big-5's.
- Label reputation × 0.1 in launch awareness.
- Recognition: 0.29 vs 0.09 at matched act momentum.
- `GetRegionalLaunchFactor` covered term 0.55 + 0.45·reach.
- Launch stock: 36k vs 12k / 4k at matched momentum.
- Weekly label push: compressed by `--radio-push-spread=0.1`.
- The LP tier purchase table (1.36 Major / 0.50 Small), untouched by `dp`.

**Do:** produce a complete inventory with `grep` (a table: term, file:line, Major vs Independent value, adopted switch if any). Then run one ablation arm that neutralises every remaining launch-side label term at once. If realization converges, split it. If it doesn't, the premium is upstream (hypotheses 1–3).

### 5. Career ladder and cadence at large labels

- Home-grown Big-5 seniors carry most Big-5 slots from 1962. Established → Declining needs three flops in a row and almost never fires at a 58% entry rate. `GetMarketingBudget` has a careerState multiplier.
- History's Big-5 edge was cadence at equal conversion. The model has higher conversion at equal or lower cadence.
- **Measure:** Top 10 per Hot 100 debut for hit acts by tier (history equal at 16%). Debuts per hit-act year by tier (history 3.28 vs 2.18). The marketing budget by careerState × tier.

### 6. Opening MidTiers and promotions

- MidTiers realize like Majors but are few (18–29 alive), freeze as a bucket, and close early under slate and capacity (`slate-starves-midtiers-field-repeats`, `midtier-bucket-cannot-grow-by-promotion`).
- Imperial, Liberty and Vee-Jay out-issued the model's MidTiers in 1960 but are typed Independent (`singles-per-label-1960-research`).
- If tier typing puts the historical head independents in the low-realization class, part of the Big-5 excess is the model's missing head firms.
- **Measure:** 1960 singles and Top 10 by named label against history.

## Method

- **Runs:** `SimTools/run_indie_access_screen.ps1 -Seed N -Arms base[,arm…] -RunPrefix X -Extra --lean-probe [-Extra raw,flags]`. Arms join with `_`. The script refuses existing run names. A lean 104-week run is about 0.12 GB and 15–25 minutes; four run at once.
- **Seeds:** use four (2002, 1001, 3003, 6006). Skip 4004 / 5005 (concentrated worlds). Single-seed Top 10 swings are ±5–8 points between arms that differ only in small finance details. Judge on Top 40 / Hot 100 and realization, and require the same sign on at least 3 of 4 seeds.
- **Scorers:**
  - `analyze_big5_share_ladder.py <run>…`: Top 10 / 40 / Hot 100 share, launch share, premium, entry rates.
  - `analyze_fit_lottery_by_tier.py <run>…`: connect chance and realization by tier.
  - `analyze_big5_premium_decomposition.py`: mix vs in-cell.
  - `analyze_hit_followups_by_tier.py`.
  - `analyze_regional_guards.py`.
  - Stagger: copy `analyze_regional_stagger.py` and change its prefix line (it hard-codes `hs52op…`).
- **Prefer telemetry over new runs.** The decision CSVs (`programme-choices`, `programme-cash-shortfalls`, `release-slate`, `deal-ledger`, `daily-talent-market`) answer most of hypotheses 1–3 from the control runs alone.
- **New switches:** off by default, no global RNG draws, a check in `SimTools/SaveLoadRoundTripRunner` (`--<name>-check`), and a byte-identical 27-week off run (compare `records.csv` bodies with `cpw-off-2002-base`).
- **Build:** `dotnet build "Label Man.csproj" -c Debug`, never while runs hold the DLL. Python: `$LOCALAPPDATA/Programs/Python/Python314/python.exe`.
- **Commits:** none. All work from 2026-11-11 to 2026-11-13 is uncommitted on `stature-fanbase`, including another session's `GameyAuditSwitches.cs`. Run outputs go to `SimLogs/` and are never committed.

## Switches built in the earlier chase (all in the working tree)

- **Adopted (in the bundle):**
  - `--major-scale-gate=0.75`
  - `--label-demand-parity=1`
  - `--uncovered-sales-terms=10` (`UncoveredSalesTerms.cs`)
  - `--receivable-backed-overdraft=0.75` (`GameyAuditSwitches.cs`)
  - `--slate-going-concern-reserve-all-tiers` and `--programme-overdraft-funding` (`ProgrammeFundingRelief.cs`)
- **Built, not adopted:**
  - `--earned-indie-coverage` and `--regional-arrival-chart-depth` (`ec`, `ad`)
  - `--major-craft-parity`
  - `--covered-launch-parity`
  - `--regional-fit-exposure-national-rank`
  - `--chart-proven-wholesale[-weekly|-markets]` (`ChartProvenWholesale.cs`)
  - `--hit-follow-up-factoring`
  - `--indie-distributor-access`
  - `--act-earned-campaign-impact`
  - `--launch-stock-trace` (observe-only)

## Finding (2026-11-13): what carries the realization premium

Control `fund-v1-{2002,1001,3003,6006}-gc_of`, singles launched weeks 1–92 of 1960–61. Scorer: `SimTools/analyze_realization_root_cause.py <run>…`. It recomputes every single's commercial-fit draw offline with `SimTools/keyed_fit_draws.py`, a Python replica of the keyed Philox draw, `SingleCommercialFit.National` and the `RegionalFitExposure` launch draw. The replica correlates r = 0.97 with the captured fit, and no single it calls a flop is connected in the capture. "Connected" below is the sim's own `Connected` test on the record's actual draw, not an expectation from awareness. Act heat at launch: cold = momentum and recognition both < 0.03; hot = either ≥ 0.15; warm otherwise.

### No hidden selection
The fit draw is label-blind. Its mean z is −0.04 to +0.06 at every tier on every seed. The connect rate is 0.57–0.63 at the Big-5 and MidTiers and 0.48–0.50 at the field; awareness moves the kink, and nothing else does.

The gap is **realization of connected singles**, and it sits mostly before the chart. H100 | connected is 0.42–0.52 for the Big-5 and MidTiers, against 0.15–0.23 for Independent and Small labels. Top 40 | Hot 100 is only 0.51–0.56 vs 0.41–0.50.

Top 40 per connected single (%):

| seed | Big-5 | MidTier | Indep. | Small+Bout | Big-5 ÷ Small | Big-5 ÷ Indep. |
|---|---|---|---|---|---|---|
| 2002 | 25.2 | 27.6 | 10.8 | 6.2 | 4.06 | 2.34 |
| 1001 | 25.6 | 30.7 | 10.5 | 6.2 | 4.15 | 2.44 |
| 3003 | 30.1 | 29.9 | 11.1 | 7.1 | 4.26 | 2.71 |
| 6006 | 27.8 | 25.1 | 11.4 | 7.0 | 3.97 | 2.43 |

### Decomposition of Big-5 ÷ Small (4.11×, mean of seeds)
Small labels' in-cell rates are reweighted to the Big-5 mix. The order is quality, then act heat, then the launch ablation. Every step has the same sign on all four seeds.

| step | 2002 | 1001 | 3003 | 6006 | mean | share of log gap |
|---|---|---|---|---|---|---|
| raw | 4.06 | 4.15 | 4.26 | 3.97 | 4.11 | |
| at Big-5 quality mix | 2.50 | 2.65 | 2.37 | 2.54 | 2.52 | **quality mix ~35%** |
| at Big-5 quality × heat mix | 2.20 | 1.80 | 1.77 | 1.78 | 1.89 | **act-heat mix ~20%** |
| same, under `--label-blind-launch=all` | 1.15 | 1.41 | 1.29 | 1.26 | 1.28 | **launch footprint ~28%** |
| residual | | | | | 1.28 | ~17% (post-launch, unmeasured) |

### Carrier 1: the launch footprint (live, label-scaled, not removed by `dp`)
`--label-blind-launch=foot` (new; `Systems/LabelBlindLaunch.cs`) launches every live AI single as if its label covered every region at reach 0.38, with no strong or home region. That changes:
- the regional launch factor (`ChartSimulator.GetRegionalLaunchFactor`: strong 1.35 / covered 0.55 + 0.45·reach / uncovered 0.12 + 0.18·reach, feeding the 0.6-weighted regional awareness and launch radio);
- launch stock (`CalculateInitialRegionalStock`: uncovered access 0.18, home/strong depth, distance-model reach);
- the arrival hold (`RegionalAudience.HoldUntilArrival`);
- city reach (`CityReach.Launch`);
- the fit-exposure launch chance (`RegionalFitExposure.Launch`, 0.3 + 0.7·reach plus the label home). The Big-5 are exposed in 6.4–6.7 of 7 regions, Small labels in 3.6–3.7.

Results:
- `foot` alone gives the whole ablation's effect. Big-5 ÷ Small goes 4.11 → **2.64** (2.66 / 2.39 / 2.52 / 3.00); with `promo` added it is 2.62.
- **The launch promotion terms carry nothing measurable once the footprint is open.** Those terms are campaign impact in launch awareness and radio, the weekly label push, reputation × 0.1 and payola × 0.15.
- The fit-exposure draw is a keyed natural experiment: exposure varies at random within a label. It is worth OR 1.14 per exposed region on connected singles (z = 5.1 pooled; 2.1–2.8 on each seed).

Under the full ablation, cold acts converge once quality is matched (pooled, Top 40 % of connected singles):

| quality | Big-5 | MidTier | Indep. | Small |
|---|---|---|---|---|
| 0.70–0.74 | 12.7 | 13.9 | 13.2 | 10.3 |
| 0.74–0.78 | 18.5 | 29.2 | 25.7 | 16.1 |
| 0.78+ | 33.8 | 51.1 | 30.5 | 35.2 |

The control's cold-act gaps in the same bands were 22.9 vs 6.2, 32.9 vs 13.1 and 41.3 vs 26.3.

1961 Big-5 chart share, control → `foot` (2002 / 1001 / 3003 / 6006):

| | control | `foot` | mean |
|---|---|---|---|
| Top 10 | 30.6 / 28.3 / 53.7 / 39.2 | 39.8 / 21.2 / 36.5 / 30.8 | 37.9 → 32.1 |
| Top 40 | 25.9 / 23.5 / 37.2 / 32.5 | 27.9 / 18.3 / 29.4 / 27.2 | 29.8 → 25.7 |
| Hot 100 | 24.9 / 21.9 / 32.1 / 29.5 | 25.1 / 18.2 / 25.3 / 24.3 | 27.1 → 23.2 |

- Down on 3 of 4 seeds at every rung; seed 2002 rises at the Top 10 and Top 40.
- `all` gives Top 10 39.6 / 21.3 / 37.3 / 38.8; its Hot 100 share is down on 4 of 4.
- The footprint mostly closes **Small vs Big-5**. Big-5 ÷ Independent moves only 2.48 → 2.11 under `foot`, and MidTiers keep a premium.

### Carrier 2: the quality mix (label craft plus slate selection), ~35%
Connected Big-5 singles average quality 0.707 against 0.669 at Small labels. The Top 40 response to quality is steep: log-quality coefficient about 17–20 in the logit, from `QUALITY_EXPONENT` 4 plus radio taste and word of mouth. A 0.04 quality edge is therefore worth about 2× in odds.

Quality = 0.5 hook + 0.3 production + 0.2 dance (`RecordRuntimeData.GetQuality`). Production differs most: 0.73–0.74 vs 0.64–0.65. Production is 0.4 × the label's `productionQuality` (`CompetitorManager.cs:3966`), so about 60% of the edge is label craft. The rest is hook, which comes from slate selection.

**Hypothesis 1 measured, 1960–61** (`programme-choices.csv`):

| | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| scored candidates per slot | 34–42 | 15 | 4.1–4.5 | 2.8 |
| P(selected) | 0.024–0.029 | | 0.22–0.24 | 0.35 |

- Picks' `qualityEstimate`: Big-5 0.60 vs 0.53 unselected; Independent 0.57, Small 0.56.
- Picks' act momentum: Big-5 0.095–0.118 vs 0.014 unselected; non-Majors pick at their pool mean.
- Selection cannot see the fit draw (equal z), so it acts only through quality and act heat.
- `cp` (Major craft parity) only reset the Majors' founding draws. MidTiers (production 0.705) still carry the edge.

### Carrier 3: act-heat mix, ~20%
- Hot acts make up 35–40% of Big-5 launches and 30–42% of MidTier launches, against 9–12% at Independents and 7–8% at Small labels.
- Hot acts take 74% of Big-5 Top 10 weeks and 70% of MidTiers', against 30% at Independents and 23% at Small labels.
- Launch awareness is mostly artist, not label. Regressing it on label and artist terms (R² 0.92), the label part is 0.069 at the Big-5 vs 0.024 at Small labels. The artist part is 0.31 vs 0.15 (momentum 0.107 vs 0.020, recognition 0.19 vs 0.01).
- Launch stock is likewise dominated by recognition and follower advance orders. Under `all` the Big-5 still stock 22k a single vs 4.3k.
- 1961 hit acts: the Big-5 put 3.76 Hot 100 records a year per hit act, against 1.86 at Independents and 1.55 at Small labels (history 3.28 vs 2.18). The cadence edge exists and is wider than history's: hypotheses 3 and 5 (roster and career ladder), amplified by the slate's momentum priority.

### What this means
- The premium is not one term. Of the log gap between the Big-5 and Small labels:
  - about 28% is the **launch footprint**: arrival hold, fit exposure, uncovered stock and launch factor, all read off distribution coverage and reach;
  - about 35% is **record quality**: label production quality plus a slate picking from ten times the pool;
  - about 20% is **who is released**: hot acts;
  - about 17% is unexplained, probably weekly distribution (restock in uncovered regions, rack shelf, one-stop terms).
- **Launch promotion is not a carrier** once the footprint is open.
- A Big-5 footprint edge is historical; the sim's version is too steep for Small labels, which get about 3.7 exposed regions and arrival-held markets even for a connected record.
- Candidate next screens:
  - **(a)** soften `foot` instead of equalising it. For example, the fit-exposure launch base 0.3 → 0.6 alone, or the arrival hold released for connected records. Re-check the regional stagger guard: `fx` failed it at base 1.0.
  - **(b)** cap the production-quality spread by label, the `cp` idea extended to MidTiers;
  - **(c)** cap the Big-5 slate's candidate pool (hypothesis 1's kill test), to split selection from supply in the quality and heat mix.

### Switch
`--label-blind-launch=promo|foot|all`:
- off by default; draws nothing;
- live AI singles only (albums, player records and opening catalog untouched);
- check: `--label-blind-launch-check` in `SaveLoadRoundTripRunner` passes;
- 27-week off run `lbl-off-2002-base` `records.csv` is byte-identical to `cpw-off-2002-base`;
- runs: `lbl-all-*-base`, `lbl-foot-*-base` (104 weeks, lean, reference bundle).

It is an ablation, not a design: every label gets one reference footprint.

## Screen (2026-11-13): softening without removing the Majors' edge — `soft-v1-*`

Switches (`Systems/FootprintCraftSoftening.cs`, check `--footprint-craft-softening-check` passes; 27-week off run `fcs-off-2002-base` byte-identical to `lbl-off-2002-base`):
- `fb` = existing `--regional-fit-exposure-launch-base=0.6` (0.3 → 0.6).
- `ca` = `--connected-arrival=1`: a connected single arrives in a held region 1 week after that region is fit-exposed (trigger 5 in `RegionalAudience.TryArrive`; fired 7.9k times on seed 2002).
- `cf` = `--label-craft-floor=0.5`: in a single's production, label craft below 0.85 moves half way up to it (order kept, Majors ≥ 0.85 untouched).

Scorer: `SimTools/analyze_softening_guardrails.py SEEDS FMT…` (G1 share/premium, G2/G7 T40 per connected single by quality band, G3 stock, G5 hit-act cadence, G6 finance). Stagger: scratch copy of `analyze_regional_stagger.py` taking a run format.

1961 Big-5 share, mean of 2002/1001/3003/6006 (T10 / T40 / H100, premium = T10 share ÷ launch share):

| arm | T10 | T40 | H100 | premium | indie T10 lead≥2 / gap≥6 (hist 18.5 / 15) |
|---|---|---|---|---|---|
| control | 37.9 | 29.8 | 27.1 | 2.59 | 20.7 / 23.0 |
| fb | 38.3 | 31.2 | 29.1 | 2.62 | 14.4 / 16.2 |
| fb_ca | 40.0 | 33.3 | 29.8 | 2.64 | 13.5 / 13.8 |
| cf | 33.9 | 28.2 | 26.0 | 2.34 | 17.0 / 23.2 |
| fb_ca_cf | 37.4 | 30.5 | 28.0 | 2.47 | 15.6 / 17.4 |

- **Every guardrail holds in every arm** (Big-5 ≥ Indie in every quality band, stock 2–4×, hit-act debuts 3.3–4.0 vs 1.7–1.9, Big-5 gross +1–4%). The trouble is the reverse: none narrows the premium.
- `fb` / `fb_ca` **raise** Big-5 T40/H100 share (fb_ca H100 up on 3 of 4 seeds) and weaken the stagger. Field in-band realization barely moves (Small .70–.74: 7.6 → 8.8). So `foot`'s effect is not in fit exposure or the arrival hold; it is in the parts not softened here: the regional launch factor (uncovered 0.12 + 0.18·reach), launch stock and city reach.
- `cf` lowers the mean, but only 2 of 4 seeds at every rung (3003 carries it). It lifts field quality (Indep .78+ singles +7%) yet field realization per band falls, because chart slots are zero-sum: better field records compete with each other. Not adoptable on this evidence.
- Next: split `foot` into launch factor / launch stock / city reach arms to find which piece carries its 28%, and soften that piece. Rejected: `fb`, `ca`.

## Screen (2026-11-13): `foot` split into its parts — `split-v1-*`

`--label-blind-launch` now takes the parts of `foot` separately: `factor` (regional launch factor), `stock` (launch stock), `city` (city reach), `hold` (arrival hold), `expose` (fit-exposure launch chance); `foot` = all five. Script arms `lf`, `ls`, `lc`, `lh`, `lx`, `lfoot`. Check passes with new per-part cases; 27-week off run byte-identical to `lbl-off-2002-base`; 27-week `foot` run byte-identical to the first 27 weeks of `lbl-foot-2002-base`.

Share of `foot`'s log drop in Big-5 ÷ Small Top 40 per connected single (control 4.11 → `foot` 2.64):

| arm | 1001 | 2002 | 3003 | 6006 | mean | Big-5 ÷ Small |
|---|---|---|---|---|---|---|
| `lc` city reach | 49 | 56 | 45 | −22 | 32 | 3.49 |
| `lf` launch factor | −5 | 28 | 23 | 29 | 19 | 3.83 |
| `lh_lx` hold + expose | −5 | 17 | 40 | −24 | 7 | 3.94 |
| `ls` launch stock | 1 | −5 | 3 | −3 | −1 | 4.12 |

- **No single carrier.** City reach is largest on 3 of 4 seeds but reverses on 6006; the launch factor is steadier (3 of 4, ~25%); stock is nothing. The parts sum to ~57% of `foot`: the rest is interaction, so the footprint works only when its reads move together (a city reached in a region the launch factor still treats as uncovered gains little).
- **Every part alone raises Big-5 chart share** (mean T40 29.8 → 29.9–32.5, H100 up on most seeds) **and Big-5 ÷ Independent** (2.48 → 2.61–2.69). Only the full `foot` lowers both (2.11). One read at a time moves slots from Independents to Small labels and the Big-5 gain.
- Two seeds would have misled: on 2002 + 1001 the parts looked additive and city reach looked like half the effect.
- Next: a joint softening — move factor, city, hold and expose together part of the way toward the reference for non-Major singles (a dose on `foot`, stock left alone), Majors untouched; screen at about half strength on four seeds against the guardrails.

Telemetry: `--skip-telemetry=NAME` (one flag per file; `ChartAuditRunner.CreateWriter` discards rows of `*-NAME.csv`/`.jsonl`). `run_indie_access_screen.ps1 -SkipTelemetry` now defaults to stature-shadow, writing-demos, writing-partnerships, artist-fanbase-weekly, media-demand, single-capital-risk (pass `none` to keep them). 27-week run `skiptel2-off-2002-base`: records byte-identical, 128 → 83 MB. A "lean" 104-week run was ~840 MB, not 0.12 GB.

## Screen (2026-11-13): joint footprint softening — `dose-v1-*`

`--label-blind-launch-dose=D` (`LabelBlindLaunch.cs`; check cases pass; 27-week off run byte-identical): for a live non-Major AI single, one keyed draw per region opens a share D of regions to the reference footprint on all four reads at once (launch factor ≥ 0.72, all cities in territory, no arrival hold, fit-exposure chance ≥ 0.57). Lift only; stock, Majors, albums untouched. Arms `ldh` (0.5), `ldf` (1).

| arm | B5 ÷ Small | B5 ÷ Indep. | T10 | T40 | H100 | premium | indie T10 lead≥2 / gap≥6 (hist 18.5 / 15) |
|---|---|---|---|---|---|---|---|
| control | 4.11 | 2.48 | 37.9 | 29.8 | 27.1 | 2.59 | 20.7 / 23.0 |
| `ldh` 0.5 | 3.50 | 2.51 | 40.1 | 31.0 | 27.5 | 2.65 | 26.9 / 27.3 |
| `ldf` 1 | 3.02 | 2.49 | 34.9 | 29.1 | 26.2 | 2.55 | 27.6 / 27.9 |
| full `foot` | 2.64 | 2.11 | 32.1 | 25.7 | 23.2 | 2.34 | 33.6 / 31.2 |

- **Closes the Small-label gap, not the share.** B5 ÷ Small falls on 4 of 4 seeds at both doses (to 3.02 at full dose, ~70% of `foot`'s drop). B5 ÷ Independent does not move (Independents cover more regions, so little of theirs opens). Big-5 share is flat: ldf H100 down on 2 of 4 seeds, T10 down 2 of 4. Small labels' 1961 T10 share 2.7 → 4.2, Independents 23.8 → 26.9, MidTiers 25.7 → 24.7.
- Guardrails all hold (Big-5 ≥ Indep in every band, cadence 3.4 vs 1.8, Big-5 gross +1–3%). The stagger widens (lead ≥ 2 from 20.7 to 27.6): further from history, the opposite way from `fx`.
- **So `foot`'s share effect came from pulling the Majors' own footprint down** (strong 1.35 and covered 0.98 to the 0.72 reference) and from the Independent gap, not from lifting Small labels. A lift-only footprint softening cannot lower Big-5 share; lowering it through distribution means reducing the Majors' footprint edge, which the author wants kept.
- Remaining carriers that do not tax Major distribution: the quality mix (~35%: label craft + slate picking from 34–42 candidates per slot) and act heat (~20%: the slate's momentum priority). Next: hypothesis 1's kill test — cap the Big-5 slate's scored-candidate pool near the field's size and see whether quality and heat mix converge.

## Finding (2026-11-13, third pass): the commercial fit's shape carries most of the excess — `lw0-v1`, `up1-v1`, `lw0up1-v1`, `lw05up05-v1`, `lw0cf1-v1`

No code was changed. Every arm uses existing `SingleCommercialFit` flags (the profile supplies 1.25 / 0.15 through `--lp-retail-v2-profile`; an explicit flag replaces it). 104 weeks, lean, reference bundle, seeds 2002 / 1001 / 3003 / 6006, about 10 minutes for four seeds.

### Result

1961, mean of four seeds. Seeds column is Top 10 in the order 2002 / 1001 / 3003 / 6006. "Down" counts seeds below the control.

| arm | flags | Big-5 T10 | T40 | H100 | MidTier T10 | Indep. T10 | Small T10 | T10 seeds | down T10 / T40 / H100 |
|---|---|---|---|---|---|---|---|---|---|
| control | LW 1.25, upside 0.15 | 37.9 | 29.8 | 27.1 | 25.7 | 23.8 | 10.0 | 30.6 / 28.3 / 53.7 / 39.2 | |
| `lw0` | `--single-commercial-fit-launch-weight=0` | 32.6 | 23.5 | 22.0 | 20.2 | 29.6 | 17.3 | 32.1 / 24.6 / 38.3 / 35.4 | 3 / 4 / 4 |
| `up1` | `--single-commercial-fit-upside=1` | 34.0 | 27.3 | 25.0 | 30.7 | 25.1 | 9.5 | 39.8 / 20.0 / 31.0 / 45.4 | 2 / 3 / 3 |
| `lw05up05` | LW 0.5, upside 0.5 | 32.0 | 25.0 | 23.5 | 22.8 | 34.2 | 10.3 | 30.8 / 22.5 / 37.1 / 37.7 | 3 / 4 / 4 |
| **`lw0up1`** | LW 0, upside 1 | **25.7** | **17.6** | **17.6** | 15.2 | 40.2 | 17.8 | 21.9 / 25.6 / 30.0 / 25.2 | **4 / 4 / 4** |
| `lw0cf1` | LW 0, `--label-craft-floor=1` | 29.3 | 20.3 | 18.2 | 16.6 | 34.7 | 19.4 | 26.3 / 34.8 / 21.9 / 34.2 | 3 / 3 / 4 |
| history | | 20.6 | | | | | | | |

- `lw0up1` is the first arm near history, and the first with the same sign on four seeds at every rung. Big-5 plus MidTier Top 10 share goes 63.6 → 40.9. Distribution is untouched.
- The two fit terms interact. `up1` alone moves nothing to the field (MidTiers gain); `lw0` alone is worth about 5 points; together about 12. The half dose gives about half at the Top 10.
- Label craft is a smaller, additive carrier: about 3 points at each rung on top of `lw0`.
- Chart units stay at 120–124M a year in every arm.

### Why: two fit parameters were tuned to hold a Major share that is now known to be too high

`SingleCommercialFit.National`: `k = kink − LaunchWeight × (launchAwareness − 0.2)`; below `k` the log factor is `2.5 × (z − k)`, above it `Upside × (z − k)`.

1. **Launch weight 1.25 counts an act's heat twice.** Launch awareness already sets aware buyers linearly. Through the kink it also multiplies every sub-kink record's purchase rate by `exp(3.125 × Δawareness)` and raises the share of draws that are hits.
   - Big-5 and MidTier singles (mean awareness 0.37–0.43) draw a true hit 19–27% of the time; the field 14–15%. That 1.3–1.8× tilt is larger than the whole historical per-single premium (1.2×). The earlier handoff called it small.
   - At awareness 0.9 (a star) the hit chance is 44% and a sub-kink record's factor is 9× the same draw on a reference launch.
   - Singles launched at awareness ≥ 0.7 are about 103 a seed at the Big-5, 116 at MidTiers and 17 in the whole field. They take **31% of all Top 10 weeks** in the control, 14% under `lw0`, 11% under `lw0up1`.
   - Half of Big-5 Top 10 weeks (52%; MidTier 46%) come from records whose label-blind draw was below the reference kink. The field's figure is 16–21%.
2. **Upside 0.15 makes the draw irrelevant among records that pass.** The code comment says it: "so luck cannot outsell a campaign". Among singles above their kink, log units regress on the draw `z` with R² 0.009; on log quality alone with R² 0.55 (slope 14.6); on tier dummies 0.14. So the lottery picks who may chart, and deterministic label, act and quality terms rank them. History's near-equal conversion needs the record's own appeal to dominate that ranking.
3. **Both were calibrated to the old target.** `DemandAttentionImplementation.md` (2026-10-21): a lognormal and a per-record-normalized kink were rejected because Major chart-unit share fell to .18–.30 and Major-family share to .33–.51; weights near 1.25 were kept because they held Major-family share "within a few points of control" (.60–.62). `RegionalSyncDesign.md` §9s later rebalanced the launch weight against a Big-5 Top 10 benchmark of 34.2. The present target is 20.6.

### What the arms change at a matched label-blind draw

Top 40 % of singles, pooled seeds, launched weeks 1–92 (`SimTools/analyze_realization_by_draw.py LW UPSIDE FMT`):

| draw | arm | Big-5 | MidTier | Indep. | Small |
|---|---|---|---|---|---|
| hit (z > 1.036) | control | 54.3 | 50.9 | 27.8 | 17.4 |
| | `lw0` | 58.6 | 58.6 | 31.1 | 22.0 |
| | `lw0up1` | 59.0 | 57.3 | 36.4 | 27.0 |
| 0.5 < z < 1.036 | control | 31.8 | 31.6 | 7.1 | 3.4 |
| | `lw0` | 23.4 | 22.0 | 8.3 | 5.5 |
| | `lw0up1` | 12.7 | 9.4 | 3.0 | 1.8 |
| 0 < z < 0.5 | control | 10.4 | 13.7 | 0.7 | 0.2 |
| | `lw0` | 1.3 | 1.9 | 0.2 | 0.0 |

- Big-5 ÷ Small on hit draws: 3.11 → 2.68 → 2.19 (`lw0cf1` 2.28). Big-5 ÷ Independent: 1.95 → 1.88 → 1.63. The Majors keep a 2× edge on a hit draw; it comes from footprint and quality, as measured before.
- Big-5 ÷ Small Top 40 per connected single (the earlier headline, 4.11): 3.13 under `lw0`, 4 of 4 seeds.

### Guardrails on `lw0up1` (control → arm)

- **Majors' edge kept.** Top 40 per connected single by quality band, Big-5 vs Independent: 10.0 / 21.8 / 28.5 / 33.1 vs 5.9 / 17.9 / 18.5 / 27.7.
- **Finance.** Big-5 record gross $241.7M → $231.6M (−4%), pretax $60.1M → $57.4M. MidTier gross −17%, MidTiers alive 21.0 → 18.5. Field gross +16%; Independents alive 249.5 → 273.8, Small 167.2 → 152.2.
- **Stagger** (indie Top 10, lead ≥ 2 / gap ≥ 6; history 18.5 / 15): control 20.7 / 23.0, `lw0` 26.9 / 26.2, `lw05up05` 23.7 / 24.0, **`lw0up1` 21.7 / 18.0**.
- **Chart shape, 1961** (`analyze_chart_shape_1961.py`): number ones 22.5 → 20.5, mean weeks at number one 2.3 → 2.6, Top 10 records 137 → 119, Top 10 acts 101 → 111, records over 1M units 14 → 28, biggest record 1.68M → 2.30M, units at #100 2,492 → 1,682.
- **Hit acts.** Big-5 debuts per hit act 2.93, field 1.45–1.74 (history 3.28 vs 2.18). Follow-ups after a first Top 10 enter the Hot 100 at the same rate (Big-5 54–67%); their Top 10 weeks per single halve at the Big-5 and MidTiers. Field hit acts rise by half or more.
- **Watch:** a star's single (awareness ≥ 0.7, Big-5) reaches the Top 40 23% of the time, against 44% in the control. With six singles a year that is still about 1.4 Top 40 records a year per star; history's stars made two or three from fewer singles. 1960 Big-5 Top 10 falls to 20.6 / 16.0 / 25.7 / 17.5.

### The second analysis' five points, checked on the control

1. **Feedback loop (heat as a consequence).** Present but slow, and not the 1960–61 carrier.
   - Cold acts whose first single had a hit draw reach the Top 40 at 55 / 45 / 27 / 16% (Big-5 / MidTier / Indep. / Small): a label effect on a first record.
   - After a first-single Top 40 the tiers converge: singles through week 92 are 7.1 / 4.6 / 5.2 / 4.7, Top 10 weeks in all 3.3 / 4.5 / 2.0 / 2.0, and only 4–12% of second singles launch hot.
   - **The heat mix is an opening endowment.** Acts already hot or warm at their first in-run single are 23% of 1961 launches and take 57% of 1961 Top 10 weeks. The Big-5 issue 32% of those launches and take 51% of those weeks. Among cold-start acts the Big-5 issue 9% of launches and take 22% of Top 10 weeks.
   - Stars and Superstars at the open, per seed: Big-5 9.3, MidTier 17.2, Independent 1.5, Small 0.
2. **Chart success gates access.** Confirmed in code: the national top 40 opens the arrival hold, fit exposure, city reach, the 0.4 awareness floor and rack shelf in the same week. Of 26,000 held region-launches in a run (`lw0` log), about 400 arrive by the chart and 16,000 never arrive. This is the `foot` carrier; lifting it for non-Majors closes Big-5 ÷ Small but not the share (`dose-v1`).
3. **Response to quality.** Confirmed, and it is the size of the response, not of the gap. Conditional on a hit, units scale as quality^14.6 (within-tier 11.6–14.6), against a designed purchase exponent of 4. Median units of hit singles run from 6k (quality < 0.62) to 680k (≥ 0.78) at the Big-5 and 1.5k to 371k at Small labels.
   - Sources: purchase q⁴, potential audience 0.3 + 0.7q, radio heat q^1.8, word of mouth q^2.2 × (q − 0.45), organic awareness growth × q, the top-40 gates, then the singles purse ration at exponent 1.35 (intent is 7–8× the purse, so only 13% clears).
   - A 0.04 quality edge is worth about 2.2× units. Label craft alone (0.4 × label production quality, 0.83 vs 0.63) is about 1.6×; among cold acts' hit draws production carries a Top 40 odds ratio near 2.2. `lw0cf1` puts that at about 3 points of share.
4. **A blind draw is not blind potential.** Confirmed; this is the launch-weight finding above.
5. **Three kinds of evidence.** For each carrier:

| carrier | explains an advantage | historically excessive | lowers Big-5 share |
|---|---|---|---|
| fit launch weight | yes (stars' sub-kink records) | yes: tilt 1.3–1.8× vs a 1.2× total premium; tuned to the old target | yes, 4 of 4 at T40 / H100 |
| flat fit upside | yes (label terms rank the hits) | yes by the code's own intent | only with `lw0` |
| label craft in production | yes (odds ratio ~2.2) | probably; conversion was equal in history | ~3 points on top of `lw0` |
| launch footprint | yes (28%) | too steep for Small labels | only by lowering the Majors' own |
| opening endowment and star cadence | yes (57% of Top 10 weeks) | partly (below) | not screened |

### Ruled out in this pass

- **Airplay points at entry.** Airplay is 4–6% of chart points in a record's first chart week at every tier. Hot 100 entry is a sales bar of about 2,500 cleared units.
- **Hit-act fit floor (0.6).** It binds only for z < −0.17 on a hot act under LW 1.25; those singles hold 2–4% of Big-5 and MidTier Top 10 weeks, and 1–2% under `lw0`.
- **Singles ration exponent 1.35.** It keeps the order of titles within a region, so it widens unit gaps without moving chart slots.
- **Fat upside alone** (`up1`).

### Other things seen that look wrong, not screened

- **Flops sell almost nothing.** Median lifetime units of a floor-draw single: Big-5 57, MidTier 38, Independent 13, Small 10. The fit comment promises "a few hundred copies, not a few dozen"; the purse ration clears 2–4% of small intents.
- **Adult acts in the singles Top 10.** `AdultStandardBearer` acts hold 9.0% of 1961 Top 10 weeks (Big-5 6.5); acts formed before 1950 hold 14.4% (Big-5 9.1). Known (`major-share-is-career-mix-adult60-is-volume`).
- **Star cadence.** Big-5 opening archetype acts issue 10.6–11.5 singles in 92 weeks, about six a year.

### Next

1. Take `lw0up1` to 156–208 weeks on two seeds (1962–64 share, bankruptcies, hit-act cadence), then a decade A/B. The upside value is one point on a line; screen 0.6–0.8 with LW 0–0.25 if the stars' hit rate or the #100 units matter.
2. Decide whether a star's heat should move the kink at all. A small weight on the act's own recent chart record, instead of total launch awareness, would keep "a star's follow-up is a safer bet" without counting label and campaign awareness twice.
3. Label craft: `--label-craft-floor` at 0.5–1 on top of the fit change.
4. The slate pool cap (hypothesis 1's kill test) is still open, and is now a smaller question.

Scorers added: `analyze_fit_shape_arms.py name=fmt…` (arm table, `{s}` = seed), `analyze_realization_by_draw.py LW UPSIDE fmt` (overrides `keyed_fit_draws` so "connected" matches the arm), `analyze_top10_by_launch_awareness.py fmt`, `analyze_chart_shape_1961.py name=fmt…`. `analyze_softening_guardrails.py` still assumes LW 1.25 for "connected".

## Screen (2026-11-13): 208 weeks — `fit208-v1-{2002,1001}-{base,lw0up1,lw025up07}`

No code change in the sim. `run_indie_access_screen.ps1` takes `lwAupB` arms (leading 0 = decimal: `lw025up07` = LW 0.25 / upside 0.7). `base` is a new 208-week control (no current-bundle control that long existed). Scorer: `SimTools/analyze_fit_shape_long.py SEEDS name=fmt…`. Two seeds only.

**Scorer fix.** `analyze_softening_guardrails.py` now reads each run's LW / upside from its request file (`keyed_fit_draws.params_for_run`, `configure` recomputes `LOG_NORM` and `UNEXPOSED_CAP`, which depend on upside). The `lw0up1` G2 figures above were computed at LW 1.25; correct ones (Big-5 vs Independent): 11.2 / 25.6 / 33.2 / 39.6 vs 5.9 / 18.1 / 18.6 / 28.2. Guardrail holds.

Big-5 Top 10 share (mean of 2 seeds; history 20.6 / 21.2 / 26.0):

| arm | 1961 | 1962 | 1963 | T40 1963 | H100 1963 |
|---|---|---|---|---|---|
| control | 29.4 | 37.1 | 38.6 | 33.7 | 29.8 |
| `lw0up1` | 23.8 | 24.7 | 26.3 | 20.6 | 20.1 |
| `lw025up07` | 31.5 | 25.2 | 28.5 | 22.1 | 20.8 |

- The control's excess grows after 1961; both arms hold Big-5 share near history through 1963.
- **Stars do not recover at LW 0.25.** Big-5 + MidTier singles launched at awareness ≥ 0.7, Top 40 records per star act-year 1961 / 1962 / 1963: control 1.82 / 1.83 / 1.37, `lw025up07` 1.10 / 0.88 / 0.67, `lw0up1` 0.70 / 0.57 / 0.56. Star singles reach the Top 40 ~40% (control), 17–20%, 12–17%.
- **No MidTier closures after 1960 in any arm**; MidTiers alive at end of 1963: 20 / 26 / 23. Independent closures fall (1962–63: 57 → 31 / 39). Small labels alive fall (214 → 181 / 190) while Independents rise (366 → 396 / 390): field labels grow a tier, they do not close.
- **#1 lifetime units rise in both arms.** Median 0.97–1.08M (control) → 1.21–1.33M (`lw0up1`), 1.13–1.29M (`lw025up07`); ≥1M share 49–64% → 72–91%. The author's target is a 0.80M median: the fatter upside makes the top bigger, which needs its own lever.
- Next: keep stars strong without the double count — move the kink on the act's own recent chart record instead of launch awareness; and a cap on how far above the kink a lucky draw lifts sales (luck should decide who hits, not how big).

## Screen (2026-11-14): act-record kink and upside cap — `fit208-v2-{2002,1001}-lw0up1_{ar1,cap1,ar1_cap1}`

New switches in `Systems/SingleCommercialFit.cs` (off by default, no global draws; check `--single-commercial-fit-shape-check` passes 24 cases; 27-week off run `fsh-off-2002-base` `records.csv` byte-identical to the first 27 weeks of `fit208-v1-2002-base`, same flags. Note `lbl-off`/`dose-off`/`skiptel2-off` predate the funding flags in the bundle and are no longer valid off references.)
- `--single-commercial-fit-act-record-weight=W`: kink moves by W × recency of the act's last Top 40 (1 that week, half-life 26 weeks, 0 past 104; pre-1960 Top 40s count from mid-year via the `HitActCadence` clock, which `--known-act-airplay` keeps on). Captured at launch on live AI singles into `RecordRuntimeData.actChartRecordAtLaunch`, saved; written to `single-launch-attribution.csv` only when on.
- `--single-commercial-fit-upside-cap=C`: the log lift above the kink is at most C; the normalizer integrates the cap.
- Script arms `arN`, `capN` (leading 0 = decimal). `keyed_fit_draws.configure(*params_for_run(run))` replicates both.

Big-5 Top 10 share, mean of 2 seeds (history 20.6 / 21.2 / 26.0); stars = Top 40 records per star act-year (Big-5 + MidTier singles at launch awareness ≥ 0.7); #1 = median lifetime units of #1s:

| arm | T10 1961 | 1962 | 1963 | stars 1961 | 1962 | 1963 | #1 median 1961–63 |
|---|---|---|---|---|---|---|---|
| control | 29.4 | 37.1 | 38.6 | 1.82 | 1.83 | 1.37 | 0.97–1.07M |
| `lw0up1` | 23.8 | 24.7 | 26.3 | 0.70 | 0.57 | 0.56 | 1.21–1.33M |
| `ar1` | 49.1 | 50.1 | 45.0 | 1.73 | 1.40 | 0.99 | 1.06–1.13M |
| `cap1` | 22.1 | 27.2 | 32.4 | 0.50 | 0.67 | 0.59 | 1.14–1.22M |
| `ar1_cap1` | 53.3 | 40.9 | 44.1 | 1.79 | 1.53 | 1.10 | 1.03–1.07M |

- **The act-record kink brings stars back and sends Big-5 share above the control** (seed 2002 1961: 64%). Hit acts are concentrated at the Big-5, and the term feeds itself: a Top 40 lowers the next single's kink.
- **Why it overshoots: under upside 1, moving the kink also multiplies every connected draw.** Above the kink the log is U·(z − k), so lowering k by W adds U·W to every hit's sales (e^1 = 2.7× at W = 1). Under the old upside 0.15 a kink shift was mostly a connect-odds change; under upside 1 it is a size change too.
- **The cap alone does not lower #1 sales** (median 1.14–1.22M against `lw0up1` 1.21–1.33M) and lets Big-5 share drift back up by 1963 (32.4). #1 size is mostly not the fit tail: the control is already at ~1.0M. The 0.80M median needs another lever (purse ration, quality response).
- No MidTier closures after 1960 in any arm.
- Next: the act record should raise the odds of connecting, not the size of a hit. Compute the upside from the reference kink (a draw between the act-shifted kink and the reference kink sells at the just-connected level), then screen W 0.5 and 1.

## Screen (2026-11-14): act record moves connect odds only — `fit208-v3-{2002,1001}-lw0up1_{ao05,ao1,ao2}`

`--single-commercial-fit-act-record-odds-only` (with the act-record weight): the act term lowers the kink for connecting, but a connected draw's size is measured from the kink without it (a draw between the two kinks sells at the just-connected level). Check passes 31 cases; 27-week off run `fsh2-off-2002-base` byte-identical to the first 27 weeks of `fit208-v1-2002-base`. Script arm `aoN`.

Big-5 Top 10 share 1961 / 62 / 63, mean of 2 seeds (history 20.6 / 21.2 / 26.0); stars = Top 40 records per star act-year (launch awareness ≥ 0.7, Big-5 + MidTier); #1 median lifetime:

| arm | B5 T10 | B5 H100 1963 | stars | #1 median |
|---|---|---|---|---|
| control | 29.4 / 37.1 / 38.6 | 29.8 | 1.82 / 1.83 / 1.37 | 0.97–1.07M |
| `lw0up1` | 23.8 / 24.7 / 26.3 | 20.1 | 0.70 / 0.57 / 0.56 | 1.21–1.33M |
| `ar1` (full kink) | 49.1 / 50.1 / 45.0 | 37.2 | 1.73 / 1.40 / 0.99 | 1.06–1.13M |
| `ao05` | 28.7 / 33.2 / 32.1 | 24.5 | 1.17 / 1.07 / 0.63 | 1.15–1.33M |
| `ao1` | 40.4 / 29.3 / 33.5 | 29.0 | 1.32 / 1.22 / 0.72 | 1.25–1.29M |
| `ao2` | 34.2 / 26.1 / 34.0 | 32.4 | 1.88 / 1.57 / 1.37 | 1.07–1.20M |

- Odds-only removes `ar1`'s overshoot. `ao2` restores stars to control level; Big-5 Top 10 is ~4 points under the control (pooled 1961–63 Top 10 weeks 35.5% → 31.6%), Top 40 / Hot 100 about at the control. Seeds disagree by up to 15 points at the Top 10.
- **Star strength and Big-5 share are largely the same thing in this model.** Launch-awareness stars are 91% Big-5 / MidTier (awareness itself carries label terms).
- Label-blind view, hit acts (an act whose last Top 40 week is ≤ 26 weeks before the launch), 1961–63 launches pooled:

| arm | hot share of launches B5 / Mid / Indep / Small | Top 40 % of hot launches B5 / Mid / Indep / Small |
|---|---|---|
| control | 29.0 / 24.5 / 8.3 / 4.8 | 24.2 / 25.7 / 9.2 / 9.2 |
| `lw0up1` | 22.7 / 20.3 / 10.8 / 6.9 | 9.4 / 10.7 / 6.1 / 5.8 |
| `ao2` | 29.9 / 25.2 / 8.1 / 4.6 | 27.3 / 26.7 / 14.3 / 12.6 |

  - `ao2` lifts field hit acts' follow-ups the most (Independent 9.2 → 14.3, +55%; Big-5 +13%), but a Big-5 hit act's follow-up still converts ~2× a field hit act's (history: equal). That gap is the footprint and quality carriers measured earlier, not the fit.
  - The Big-5 issue 3–6× the field's share of hot-act launches (endowment and cadence, hypotheses 3, 5, 6).
- #1 median stays 1.07–1.20M in every LW 0 / upside 1 arm.
- Next candidates: (a) four-seed `ao2` to firm the share effect; (b) attack the follow-up gap where it lives — field hit acts' chart access (`hit-followups-lack-chart-access`), e.g. a footprint dose (`--label-blind-launch-dose`) only for hot-act singles; (c) the hot-act endowment by tier against history (who held the 1960 hit acts).

## Screen (2026-11-14): footprint dose for hit acts only — `fit208-v4-{2002,1001}-{lw0up1_ao2_lh05,lw0up1_ao2_lh1,lw0up1_lh1}`

`--label-blind-launch-dose-hot-acts` (with `--label-blind-launch-dose=D`): the dose opens only a non-Major single whose act had a Top 40 in the last 26 weeks (`HitActCadence` clock; checked at launch because the clock is configured after `LabelBlindLaunch`). `--label-blind-launch-check` passes with new cases; 27-week off run `lhd-off-2002-base` byte-identical to the first 27 weeks of `fit208-v1-2002-base`. Script arm `lhN`. Hit-act scorer now takes `name=fmt` args: `SimTools/analyze_hitact_followups_by_tier.py`.

Top 40 % of hit acts' singles (Top 40 ≤ 26 weeks before launch), 1961–63 pooled, Big-5 / MidTier / Independent / Small:

| arm | hit-act T40 % | B5 T10 1961 / 62 / 63 | stars | 
|---|---|---|---|
| control | 24.2 / 25.7 / 9.2 / 9.2 | 29.4 / 37.1 / 38.6 | 1.82 / 1.83 / 1.37 |
| `ao2` | 27.3 / 26.7 / 14.3 / 12.6 | 34.2 / 26.1 / 34.0 | 1.88 / 1.57 / 1.37 |
| `ao2_lh05` | 28.9 / 22.4 / 11.1 / 9.2 | 32.5 / 37.4 / 35.2 | 1.77 / 1.59 / 1.28 |
| `ao2_lh1` | 23.8 / 26.8 / 14.5 / 9.7 | 28.2 / 26.4 / 26.2 | 1.69 / 1.62 / 1.11 |
| `lh1` (no act record) | 14.4 / 10.2 / 8.3 / 4.1 | 32.1 / 24.9 / 29.5 | 0.86 / 0.69 / 0.59 |

- **The launch footprint does not carry the follow-up gap.** Opening every region for field hit acts leaves their follow-ups where they were (Independent 14.3 → 14.5 under `ao2_lh1`; Small 12.6 → 9.7; `lh1` vs `lw0up1` 6.1 → 8.3). Where a gap closes, it closes by the Big-5 falling (27.3 → 23.8), i.e. through chart-slot competition.
- `ao2_lh1` has the lowest Big-5 share of the act-record arms (mean 26.9 vs `ao2` 31.4, history ~22.6) with stars near control in 1961–62, but `ao2_lh05` moves the other way (35.0). Two seeds cannot separate 4-point effects: per-seed Top 10 swings are ±8–15 points (`ao2_lh1` 1963: 17.7 / 34.6).
- No MidTier closures after 1961 in any arm; #1 median 1.02–1.16M.
- Remaining follow-up carriers, from the decomposition: record quality (label craft, slate picks) and launch awareness's label part. Next: four seeds for the candidates before more design, since the act-record arms differ by less than the seed noise.
