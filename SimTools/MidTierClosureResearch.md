# MidTier closures in the 1960 control world

2026-10-22. In the switches-off control of the label release slate screen
(`slate-screen-v9-off-control-default-1001-53`), 14 of the 26 starting MidTiers go bankrupt during 1960, Chess, Atlantic,
Cameo-Parkway, Parlophone and Pye among them. This note finds the cause, adds two opt-in switches, and reports a 3-seed,
53-week A/B on the game profile with the working bundle from `SimLogs/slate-screen-v9/plan.json`.

Tools: `SimTools/analyze_midtier_closures.py` (per-label table, `--screen` one-line run summary),
`SimTools/summarize_midtier_ab.py` (pooled 3-seed A/B), `--midtier-opening-realism-check` (SaveLoadRoundTripRunner).

## 1. Root cause

**The death spiral is a tier-fixed payroll against about 2.5 months of opening cash.** These are not thin rosters on their own.

- `LabelCorporateModel.SetRequiredStaff` adds a fixed tier term (MidTier scale 1.8) to every department. A MidTier
  employs 13–23 FTE and pays $8.8–16.3k a month (mean $13.8k, seed 1001) whatever its roster. Parlophone and Pye pay
  $9k a month with no acts and no catalogue.
- MidTier opening cash is $15–60k (`LabelStartingFinances`). Failures opened with $36.9k against $15.3k monthly overhead
  (2.4 months); survivors with $44.3k against $16.9k (2.6 months).
- `EconomicLabelLifecycle.ReviewMonth` marks a firm `Struggling` below 3 months' overhead (release chance ×0.5) and
  `Dying` below 1 month (×0.3). Nearly every MidTier therefore opens at or below the Struggling line.
- Weeks 1–13: failures spent 120 of 182 label-weeks Struggling or Dying, survivors 17 of 156. Failures' recording spend
  fell from $5.9k in month 1 to $0.3–0.4k a month; survivors kept $3.8–7.5k. Failures grossed $19k a month against
  $13k payroll; survivors $116–162k. The survivors were the ones whose first-month records hit.
- **The closures are not earned.** In the control, failing MidTiers launched records as good as survivors' did
  (`runtimeQualityAtLaunch` 0.599 vs 0.612 pooled over 3 seeds; seed 1001: 0.622 vs 0.619).

### Q1: why the opening rosters are thin

Launch allocation (`RosterManager.SceneInitialization`) fills MidTiers to 64% of their drawn target (255/397, seed 1001),
against 79% for Majors (100% for the US Majors), 81% for Independents and 93% for Small labels.

- Scene recruitment gives a MidTier a 650-road-mile radius. A Major with scouting and national reach ≥ .5 adds a
  nationwide `NationalAr` route (`SceneRecruitmentService.ExplainBeyondGuest`).
- Every label picks one act per round. A city's pool runs dry once the small-target labels are full, so the large-target
  MidTiers stop short. New York: 3 Majors 117/117, 5 MidTiers 41/91, 49 Independents 268/351. Chicago: Mercury 30/30,
  Chess 13/23. Cincinnati: King 12/21.
- Roster size alone does not separate outcomes. Failures and survivors both opened with 8–14 acts. The payroll above is
  what turns a short roster into a spiral.
- History supports deeper rosters. In 1960 Chess (with Checker and Argo), Atlantic (with Atco) and King (with Federal)
  each carried tens of acts, and Chess and Atlantic signed far beyond their home city. The fix belongs in the scene
  access rule, not in a bigger fill draw. Proposed follow-up: a national A&R route for MidTiers with national reach.
  Not done here.

Opening careers and head templates are not the cause. `OpeningCareerService` restamps 75 existing acts on Major and
MidTier rosters and adds no headcount; `--seed-head-templates` is not in the working bundle.

### Q3: Parlophone and Pye

Neither was a US operating label in 1960. EMI sold in the US through Capitol, and Decca UK through London Records;
Parlophone and Pye licensed masters to American firms. Based in London, scene recruitment finds no international
connection, so the four London templates open with Parlophone 0/21, Pye 0/10, EMI 5/34 and Decca UK 1/33 acts. They
still trade: under workload staffing, Pye grossed $58k a week from distribution with no records. They belong to the
British Invasion mechanic, which should bring them in as licensors.

## 2. Switches (both opt-in, byte-identical when off)

- **`--workload-scaled-staffing`** (Q2: yes). The MidTier tier term is scaled by the roster against the tier's normal
  opening roster: `1.8 × clamp(acts / 15, .35, 1)`. Fifteen acts is lifecycle capacity 25 at the midpoint of the launch
  fill range. The act- and project-driven terms are unchanged, so a growing label hires as before. The term is never
  raised; Majors and the founder-run tiers are untouched. Opening organisations are sized after rosters are seeded, so
  the switch acts from week 1. An 8-act MidTier pays about $8.5k instead of $12.5k. The switch sets costs only; no cash,
  release, chart or survival rule changes.
- **`--defer-british-labels`**. The four London templates are drawn as before and then dropped, so all 596 other labels
  keep their ids and traits; label directories match the control exactly. Nothing replaces them. This needed one engine
  fix: `ChartManager.GenerateAILabelsIfNeeded` regenerated the whole label set whenever it held fewer than 600 labels.

Verification: `--midtier-opening-realism-check` passes 607 checks. `--seed-head-templates-check` still passes. A 13-week
save/load round trip passes with both switches and the bundle.

## 3. A/B: 3 seeds (1001/2002/3003), 53 weeks, game profile, working bundle

Arms: `mts-ab1-control`, `mts-ab1-staffing`, `mts-ab2-british`, `mts-ab2-both`. Closures are by initial tier among the
starting firms. MidTier counts exclude the London labels, so every arm compares the same 59 US firms. The
"Historical alive" column counts Atlantic, Chess, King and Cameo-Parkway across 3 seeds.

| arm | US MidTier closed | Δ per seed (pp) | Independent | Boutique | Small | Historical alive |
|---|---|---|---|---|---|---|
| control | 25/59 = 42.4% | | 30/620 = 4.8% | 10/399 = 2.5% | 4/687 = 0.6% | 7/12 |
| staffing | 16/59 = 27.1% | −21, −6, −18 vs control | 35/620 = 5.6% | 8/399 = 2.0% | 4/687 | 9/12 |
| british | 22/59 = 37.3% | −4, +6, −18 vs control | 32/620 = 5.2% | 15/399 = 3.8% | 3/687 | 8/12 |
| both | 20/59 = 33.9% | −4, −6, 0 vs british | 29/620 = 4.7% | 13/399 = 3.3% | 5/687 | 7/12 |

| arm | 1960 Top 10 CR4 (per seed) | Major Top 10 share | MidTier Top 10 share | Q1 distress weeks | MidTier launches | MidTier payroll |
|---|---|---|---|---|---|---|
| control | .313 (.262 .331 .346) | .389 | .162 | .297 | 825 | $14.3k |
| staffing | .390 (.381 .377 .413) | .443 | .221 | .191 | 979 | $11.8k |
| british | .296 (.290 .298 .298) | .343 | .148 | .290 | 791 | $14.4k |
| both | .333 (.317 .302 .381) | .396 | .162 | .224 | 863 | $12.0k |

CR4 is the four largest operating labels' share of 1960 Top 10 slot-weeks. The history and band are 34.6 and 30–40
(see `HistoricalLabelTierBalanceResearch.md` §14).

Reading:

- **The mechanism is clean.** Staffing cuts US MidTier payroll by 17%. Q1 distress weeks fall 36% against control and
  23% against british. MidTier launches rise 19% and 9%.
- **MidTier closures fall in 5 of 6 paired seeds, one tied.** The pooled effect is −14.7 pp against control but only
  −3.4 pp against british. Seed 3003 moves −18 pp even under british alone, which barely touches US firms, so the
  per-seed reshuffle is about ±15 pp: roughly 3 firms out of ~20. Three seeds bound the effect between a few points and
  about 15 points; they do not pin it down.
- **The remaining closures look earned.** Under staffing, failing MidTiers launched weaker records than survivors in all
  3 seeds (0.589 vs 0.614 pooled). In the control the two groups were level.
- **Independent, Boutique and Small closures do not move beyond noise.** Seed 3003's staffing arm has 23 Independent
  bankruptcies against 10, 7 of them at the week-35 month-end. There were no distributor failures; the two other seeds
  moved −1 and −7. The mechanism behind any real effect is market share: MidTier unit share rose 2.6–5.5 pp, mostly from
  Independents (−0.5 to −7.3 pp).
- **Top 10 concentration rises but stays within the band.** CR4 goes .313 → .390 under staffing (all 3 seeds up) and
  .296 → .333 with both switches. Both stay inside the 30–40 band, and the both-arm value is near the 34.6 history.
- Deferring the London labels moves MidTier closures within noise for US firms, so it is a historical-correctness change,
  not a survival lever.

## 4. Recommendation

1. Adopt `--workload-scaled-staffing` as a candidate. Run it together with `--defer-british-labels`, and confirm it on a
   five-year screen before making it a default. Watch Top 10 CR4 against the 1961 REVIEW year.
2. Adopt `--defer-british-labels` on historical grounds. The British Invasion mechanic should re-introduce the four
   London labels as licensors in 1963–64.
3. Follow-up, the remaining root cause: give a national MidTier (national reach and scouting ≥ .5) a launch-only national
   A&R route in scene recruitment, so Chess, Atlantic and King open near their drawn targets. This is the fix for the
   thin rosters (Q1). It touches the local-scene allocation, so it needs its own screen.
4. Not recommended: raising MidTier opening cash, or exempting MidTiers from `Struggling`. Both impose survival instead of
   letting it be earned.

## 5. National A&R for MidTiers (`--midtier-national-ar`, follow-up 3)

**Switch.** `--midtier-national-ar` gives a MidTier with `scoutingAbility` >= .5 and `nationalReach` >= .5 the `NationalAr`
route (weight .12) that capable Majors already have in `SceneRecruitmentService.ExplainBeyondGuest`. The route applies
only while `RosterManager.InitializeSceneRosters` runs. `--midtier-national-ar-runtime` also leaves it open for weekly
recruitment, to answer whether it should apply after launch. Road-circuit and catchment acts keep their nearer, heavier
routes. Independents and the founder tiers are untouched. Off, nothing changes: `--midtier-opening-realism-check` adds 9
route checks (616 pass), and seed 1001 of the control arm was rerun on this build. 95 of its 96 CSVs are byte-identical to `mts-ab2-both`; the other is the wall-clock `performance-profile`.

### Launch fill (1-week census, `--scene-recruitment-audit`, 3 seeds, bundle + staffing + British deferral)

| tier | control fill | national fill | control mean quality | national mean quality |
|---|---|---|---|---|
| Major | 570/570 = 100% | 483/570 = 84.7% (61%, 100%, 100%) | .541 | .537 |
| MidTier | 685/966 = 70.9% | 961/966 = 99.5% | .538 | .544 |
| Independent | 3709/4778 = 77.6% | 3753/4778 = 78.5% | .542 | .543 |
| Boutique | 1811/2193 = 82.6% | 1823/2193 = 83.1% | .545 | .542 |
| Small | 1848/2007 = 92.1% | 1848/2007 = 92.1% | .550 | .549 |

- **MidTiers fill their targets.** In the national arm, 611 of 961 MidTier signings use `NationalAr`. New York's MidTiers
  fill 258/260 slots against 138/260, Chess 23/23 and 21/21 (was 12/23 and 17/21), King 21/21 and 24/24 (11/21 and
  17/24), and Cameo-Parkway 17/17, 16/16 and 23/23. Mean act quality does not fall.
- **Independents do not lose acts at launch; they gain.** Independents gain 21, 17 and 6 acts per seed. A national
  MidTier draws on distant pools and leaves acts near its home that local Independents then sign. Their mean quality and
  strong-act counts are level (136 → 142 strong acts).
- **Seed 1001 exhausts the US launch pool.** Its Majors carry a 225-act target (149–196 in the other seeds). With about
  20 national MidTiers drawing from the same pool in the round-robin, every accessible US act is signed by round 23. All
  six Majors stop at 23 acts with no accessible act left: 138/225, 87 fewer acts than control. Total launch signings rise
  in every seed (2970 → 3000, 2827 → 2944, 2826 → 2924). The route redistributes a finite pool once it binds; it is not
  free supply.

### A/B: 53 weeks, 3 seeds, game profile, slate-screen-v9 bundle + `--workload-scaled-staffing --defer-british-labels`

The control is `mts-ab2-both` (section 3, same bundle). Arms: `mtn-ab-national` (launch only) and `mtn-ab-runtime`.

| arm | US MidTier closed | per seed (pp) | Independent | Boutique | Small | Historical alive |
|---|---|---|---|---|---|---|
| control | 20/59 = 33.9% | | 29/620 = 4.7% | 13/399 = 3.3% | 5/687 = 0.7% | 7/12 |
| national | 24/59 = 40.7% | +8, +6, +6 | 45/620 = 7.3% (+1, +3, +4 pp) | 14/399 = 3.5% | 2/687 | 4/12 |
| runtime | 24/59 = 40.7% | +4, +6, +12 | 43/620 = 6.9% (+2, +2, +3 pp) | 17/399 = 4.3% | 1/687 | 4/12 |

| arm | 1960 Top 10 CR4 (per seed) | Major Top 10 | MidTier Top 10 | unit share Major / MidTier / Indep | Q1 distress | MidTier launches | MidTier payroll (Jan) |
|---|---|---|---|---|---|---|---|
| control | .333 (.317 .302 .381) | .396 | .162 | .326 / .126 / .352 | .224 | 863 | $12.0k |
| national | .356 (.352 .388 .327) | .379 | .196 | .316 / .147 / .367 | .253 | 920 | $15.0k |
| runtime | .338 (.262 .487 .267) | .374 | .183 | .325 / .145 / .356 | .253 | 881 | $15.0k |

Reading:

- **Fuller rosters raise MidTier closures in all 3 seeds.** The switch works as intended: rosters fill and MidTier Top 10
  share (.162 → .196) and unit share (.126 → .147) rise. But under `--workload-scaled-staffing` a fuller roster brings
  back the payroll that switch removed. January MidTier payroll rises from $12.0k to $15.0k (+25%) because launch rosters
  are near the 15-act reference. Year-one gross per MidTier rises only ~10% ($1.12M → $1.24M per firm-year), because
  new acts have not released yet. Q1 distress weeks rise from .224 to .253. Chess and King close in both arms in every
  seed, and with full rosters Atlantic and Cameo-Parkway join them. Historical MidTiers alive at week 53 fall from 7/12
  to 4/12.
- **The two switches pull against each other.** Workload staffing cut closures by making a thin roster cheap. National
  A&R removes the thin roster, and with it the saving. Section 1 showed that a tier-fixed payroll against ~2.5 months of
  opening cash puts MidTiers on the Struggling line. A full roster is the historical case, so the opening cash or the
  payroll per act, not the roster, has to carry a full-roster MidTier through 1960. That needs its own screen; this one
  cannot settle it.
- **Independent closures rise +2.6 pp (29 → 45), with the same sign in every seed.** Seed 3003's 23-vs-10 swing in
  section 3 means three seeds cannot separate this from reshuffle. It is not lost supply: Independents gain acts at
  launch, and after launch no Independent appointment fails for lack of a candidate (1 in 3 seeds). Runtime signings dip
  344 → 324 because more appointments end "advance unaffordable" (97 → 151), a cash signal. It is not lost share
  either: Independent unit share is .352 → .367. It is not distributor contagion: no closed Independent's distributor
  failed before it, and nearly all were self-distributed. Their bankruptcies cluster at the week-31–40 month-ends, as in
  control.
- **Top 10 CR4 stays in the 30–40 band under launch-only national A&R.** It moves .333 → .356, with per-seed values .327–.388 and
  the Major share down slightly (.396 → .379). The runtime arm spreads CR4 to .262–.487, and seed 2002 falls outside the
  band.

### Should it apply at runtime? No.

After launch, MidTier recruitment is not access-bound. Across 3 seeds and all three arms, no MidTier appointment fails for
lack of a candidate, while about 270 a year end "advance unaffordable". The runtime route adds no signings (104 against 102
for launch-only and 111 in control). It adds a national draw on talent that Independents also bid for, and it widens the
CR4 spread. Keep the route launch-only.

### Recommendation

1. Keep `--midtier-national-ar` opt-in, launch-only. It fixes the cause found in Q1: MidTiers open at 99.5% of their
   drawn targets instead of 71%, Chess, King and Cameo-Parkway open full, and Independents do not lose launch acts. Do not
   add it to the working bundle yet. It raises MidTier closures (+6.6 pp, all seeds) because it cancels the workload-staffing
   saving.
2. Drop `--midtier-national-ar-runtime`, or keep it only as a probe. Runtime MidTier recruitment is bound by cash, not access.
3. Before adopting it, fix the launch pool. In seed 1001 the round-robin lets national MidTiers exhaust the US pool and
   leaves the Majors 87 acts short. Options: give national MidTiers the route only after the Majors are full, or treat
   the binding pool as a supply-sizing miss (launch candidates against summed targets).
4. Then screen a full-roster MidTier's first year: what carries Chess or King through 1960 with 20 acts? Test this as a
   cash or cost question, without imposing survival.

## 6. Majors first, then what carries a full-roster MidTier through 1960

### 6.1 The national route opens after the Majors (changes `--midtier-national-ar`)

At launch the `NationalAr` route for MidTiers now stays shut until every Major has filled its target or run dry.
Until then a capable MidTier recruits locally as before. If its local pool runs dry, it waits for the route instead of
leaving the allocation. When the route opens, those MidTiers rejoin the round-robin and their cached access lists are
rebuilt. `--midtier-opening-realism-check` adds 2 checks (618, then 623 with 6.3).

| tier | control fill | ungated (section 5) | gated |
|---|---|---|---|
| Major | 570/570 = 100% | 483/570 = 84.7% | 570/570 = 100% (every seed) |
| MidTier | 685/966 = 70.9% | 961/966 = 99.5% | 882/966 = 91.3% (82%, 97%, 98%) |
| Independent | 3709/4778 | 3753/4778 | 3709/4778 (identical to control) |
| Boutique / Small | 1811 / 1848 | 1823 / 1848 | 1811 / 1848 (identical to control) |

The gated route takes only acts that no other label signed: 197 `NationalAr` MidTier signings on top of control's 685
local ones. Independent, Boutique and Small launch rosters match control act for act. Seed 1001 still binds: its
national MidTiers stop at 82% of target because the leftover US pool is about 30 acts.

### 6.2 Anatomy: what closes a full-roster MidTier (`analyze_midtier_first_year_cash.py`)

Ungated national arm (section 5), US MidTiers, medians:

| | n | launch acts | week-1 cash | monthly overhead | cover (months) | below 3 months | H1 receipts/mo | H1 spend/mo | Q1 launch quality | Q1 distress weeks | Q1 launches |
|---|---|---|---|---|---|---|---|---|---|---|---|
| closed | 24 | 18 | $34k | $18.2k | 2.03 | 20/24 | $20.9k | $28.1k | .616 | 7.7/13 | 3.8 |
| survived | 35 | 16 | $55k | $18.3k | 3.20 | 11/35 | $143k | $104k | .620 | 0.5/13 | 7.1 |

- **Cost per act is not what separates them.** Both groups carry the same overhead and about $860–990 of payroll per act.
- **Opening cover sorts the outcomes.** `EconomicLabelLifecycle.ReviewMonth` marks a firm `Struggling` below three months'
  overhead, and `CommitmentReserve` then holds back new releases. Of the closers, 20/24 open below that line, against
  11/35 survivors. They spend 7.7 of 13 Q1 weeks Struggling or Dying and launch half as many records. Their records are
  as good as survivors' (.616 vs .620), so these closures are not earned.
- **It is not a collection lag.** Opening receivables are zero, and receipts run at ~90% of gross in the same month.
  Plant credit (30-day terms) is already on by default.
- **Opening cash ignores the roster.** It is drawn from `LabelStartingFinances` before the organisation is sized. With
  full rosters, overhead rises from $15.6k to $18.2k and the median cover falls from 3.34 to 2.99 months.

**The answer, in both currencies:**

- **Cash.** Opening with three months of the opening organisation's overhead (~$55k at $18k a month) puts every MidTier
  on the right side of the Struggling line. Closers need a median $17k more (max $34k). The extra H1 burn of a firm
  without hits (receipts $21k against $28k spend; static shortfall median $22k, max $40k) should not be covered: that
  part is earned.
- **Per-act cost.** At the closers' $34k, three months' cover needs overhead of about $11.3k, so payroll per act would
  have to fall about 38% (~$860 → ~$530). That would re-price every MidTier, not only the cash-poor ones. Nothing in
  sections 1–5 says the per-act wage schedule is wrong, so I tested cash.

### 6.3 `--midtier-going-concern-cash` (opt-in)

When the 1960 world opens (`CompetitorManager` → `LabelCompanyAccounts.Open(label, openingWorld: true)`), a MidTier's
cash is raised to at least `3 × GetMonthlyOverhead()` of the organisation it actually opens with. Cash above that is
untouched. The floor is booked on the opening balance sheet as contributed capital, before any week runs. Runtime-born
labels, other tiers and the player are untouched, and no later cash, release, status or survival rule changes. This
reads a MidTier as a going concern: a firm that carried this roster through 1959 had the working capital to staff it.
It raises 45 of 59 MidTiers (gated arm), a median $16k (max $39k), about $244k per seed in all. That is against
$1.2–1.5M gross per MidTier-year.

### 6.4 A/B: 53 weeks, 3 seeds, game profile, slate-screen-v9 bundle + staffing + British deferral

| arm | US MidTier closed | per seed (pp) | Independent | Boutique | Small | Historical alive |
|---|---|---|---|---|---|---|
| control (`mts-ab2-both`) | 20/59 = 33.9% | | 29/620 = 4.7% | 13/399 | 5/687 | 7/12 |
| national, ungated (§5) | 24/59 = 40.7% | +8, +6, +6 | 45/620 = 7.3% | 14/399 | 2/687 | 4/12 |
| gated | 14/59 = 23.7% | −21, −6, 0 | 29/620 = 4.7% | 10/399 | 3/687 | 8/12 |
| cash only | 13/59 = 22.0% | −17, −11, −6 | 26/620 = 4.2% | 15/399 | 5/687 | 10/12 |
| gated + cash | 10/59 = 16.9% | −29, −28, +12 | 27/620 = 4.4% | 12/399 | 2/687 | 9/12 |

| arm | CR4 (per seed) | Major / MidTier Top 10 | unit share Major / MidTier / Indep | Q1 distress | MidTier launches | closed vs survived Q1 launch quality | closed Q1 launches (survivors) |
|---|---|---|---|---|---|---|---|
| control | .333 (.317 .302 .381) | .396 / .162 | .326 / .126 / .352 | .224 | 863 | .591 vs .625 | 4.0 (7.2) |
| national, ungated | .356 (.352 .388 .327) | .379 / .196 | .316 / .147 / .367 | .253 | 920 | .616 vs .620 | 3.8 (7.1) |
| gated | .396 (.388 .425 .375) | .383 / .238 | .324 / .166 / .330 | .215 | 885 | .616 vs .622 | 3.5 (7.0) |
| cash only | .331 (.302 .348 .344) | .331 / .190 | .305 / .141 / .352 | .176 | 1048 | .591 vs .624 | 4.6 (7.6) |
| gated + cash | .359 (.325 .352 .400) | .366 / .231 | .313 / .175 / .335 | .141 | 1124 | .598 vs .628 | 5.7 (7.1) |

Reading:

- **Gating fixes the Major shortfall and costs nobody acts.** The ungated-vs-gated closure gap (24 vs 14) has the same
  opening cover (31 vs 33 of 59 below three months), so most of it is the ±15 pp reshuffle, not mechanism. The Majors
  keep their launch rosters, and Independent closures return to control (29).
- **Gated national A&R alone pushes CR4 to the top of the band** (.396, with seed 2002 at .425). Full MidTiers in the
  Struggling spiral still close as before, and their records are as good as survivors' (.616 vs .622).
- **Going-concern cash makes the remaining closures earned.** In the gated + cash arm, closing MidTiers launch at nearly
  normal volume (5.7 against 7.1) and their records are clearly weaker (.598 vs .628, the widest gap of any arm). Q1
  distress falls from .224 to .141, and MidTier launches rise 30%. CR4 returns to .359, inside the band, because the
  surviving MidTiers' Top 10 share comes out of the Majors' (.396 → .366) rather than adding to the head.
- **Cash alone does less.** On thin rosters the three-month floor is about $36k. A bad first month still drops a firm
  below the line, so closers still spend 7.1 of 13 Q1 weeks in distress. Its closures fall about as much (13), but
  MidTiers stay at 71% of their launch targets and their unit share stays at .141.
- **Independents are unaffected by every arm except the ungated route** (26–29 closures against 29). After launch their
  appointments never fail for lack of a candidate.
- Seed 3003 in the gated + cash arm rose from 3 to 5 closures (+12 pp): control's 3003 is already the low-closure
  seed, and the per-seed reshuffle is about ±15 pp.

### 6.5 Recommendation

1. Make `--midtier-national-ar` (now gated on the Majors) and `--midtier-going-concern-cash` a pair, as a candidate,
   alongside `--workload-scaled-staffing --defer-british-labels`. Together they open MidTiers near their drawn rosters,
   leave Majors and Independents their launch acts, keep CR4 in band (.359), and make the remaining closures follow the
   records (16.9% of US MidTiers in 1960).
2. Confirm on a five-year screen before making it a default. Watch CR4 in the 1961 REVIEW year, since the gated route
   alone reaches .40. Also watch whether the late-decade Major consolidation goal still holds once MidTiers survive 1960.
3. Keep `--midtier-national-ar-runtime` as a probe only. Runtime MidTier recruitment is cash-bound, not access-bound.
4. Seed 1001's launch pool binds: leftover US supply (~30 acts) is below the national MidTiers' deficit. That is a
   supply-sizing question (launch candidates against summed targets), separate from this switch.

## 7. Five-year screen: `--midtier-national-ar` + `--midtier-going-concern-cash`

3 seeds (1001/2002/3003), 265 weeks (1960–64), game profile, slate-screen-v9 bundle + `--workload-scaled-staffing
--defer-british-labels`. Arms: `mtn5-control` and `mtn5-pair`. Summary: `SimTools/summarize_midtier_five_year.py`.
Both switches are byte-identical when off, so `mtn5-control` can be reused as the five-year control for this bundle.

### Top 10 concentration and Major family share (means over 3 seeds)

| year | control CR4 (per seed) | pair CR4 (per seed) | Major T10 c / p | MidTier T10 c / p | majorFamilyChartShare c / p | owner-Major-family entries c / p |
|---|---|---|---|---|---|---|
| 1960 | .333 (.317 .302 .381) | .359 (.325 .352 .400) | .396 / .366 | .162 / .231 | .473 / .528 | .393 / .409 |
| **1961** | **.265** (.283 .242 .269) | **.309** (.360 .260 .308) | .290 / .295 | .128 / .192 | .401 / .397 | .340 / .333 |
| 1962 | .294 (.308 .277 .296) | .345 (.354 .263 .417) | .308 / .325 | .087 / .201 | .350 / .426 | .311 / .342 |
| 1963 | .289 (.350 .200 .317) | .331 (.277 .410 .306) | .269 / .290 | .181 / .213 | .402 / .427 | .372 / .357 |
| 1964 | .261 (.313 .123 .346) | .278 (.331 .260 .242) | .255 / .274 | .110 / .157 | .368 / .448 | .359 / .406 |

- **1961 CR4 moves into the band.** The control falls to .265 in 1961, below the 30–40 band, and stays at or under
  .294 through 1964. The pair holds .309 in 1961 and .28–.35 afterwards. Two of 15 per-seed pair values exceed .40
  (seed 3003 in 1962 at .417, seed 2002 in 1963 at .410). Two control values fall below .21 (seed 2002 in 1963 and
  1964). The pair's head comes from MidTiers that survive (MidTier Top 10 share .16–.23 against .09–.18). The Majors'
  own Top 10 share is level or slightly higher.
- **Major consolidation cannot be read in a five-year screen.** The goal is a Major share that rises in 1968–69
  (`major-share-late-decade-consolidation-goal`). Through 1964 the pair does not weaken the Majors' position:
  majorFamilyChartShare goes .397 (1961) → .448 (1964) against the control's .401 → .368. Owner-Major-family entries end
  at .406 against .359. The rise is distribution, not ownership. Majors' own entries are level (.207 vs .196 in 1964),
  and Major-distributed entries are .199 against .163. Neither arm has a single Major-owned subsidiary in 1960–64, so
  absorption never fires. Consolidation still needs its own post-1966 mechanism and a decade run. The pair leaves more
  surviving MidTiers that such a mechanism could absorb.

### Closures of starting firms (cumulative, 3 seeds)

| by end of | arm | MidTier | Independent | Boutique | Small |
|---|---|---|---|---|---|
| 1960 | control | 20/59 | 27/621 | 13/399 | 3/693 |
| 1960 | pair | 8/59 | 26/621 | 12/399 | 1/693 |
| 1962 | control | 28/59 | 86/621 | 41/399 | 58/693 |
| 1962 | pair | 23/59 | 88/621 | 40/399 | 70/693 |
| 1964 | control | 30/59 | 111/621 | 58/399 | 131/693 |
| 1964 | pair | 24/59 | 109/621 | 65/399 | 137/693 |

(This 1960 pair count, 8, differs from the 10 in section 6 only by calendar. The first 53 weeks are identical to
`mtn-ab-gated-cash`, but two seed-3003 closures fall at week 53, the first week of 1961.)

- **The MidTier saving shrinks from 12 firms in 1960 to 6 by 1964.** The pair closes 15 MidTiers in 1961–62 against the
  control's 8. Those closures are earned. The pair's 1961–62 closers launched weaker records than MidTiers alive past
  1962 (`runtimeQualityAtLaunch` .600 vs .613 over their first three years). The control's 1961–62 closers launched
  better ones (.619 vs .603). The cash floor buys a year of runway, not survival.
- **Other tiers move within noise.** Independent closures are 109 vs 111. Boutique (+7 of 399) and Small (+6 of 693)
  are inside the per-seed reshuffle. Tier transitions are similar: Independent→MidTier promotions 105 vs 111,
  MidTier→Independent demotions 55 vs 66, and one MidTier→Major promotion in the pair.

### Recommendation

1. Promote `--midtier-national-ar` + `--midtier-going-concern-cash` into the working bundle, alongside
   `--workload-scaled-staffing --defer-british-labels`. Over five years the pair keeps 1961 CR4 in band where the
   control falls below it, closes fewer MidTiers, makes the MidTier closures follow the records, and leaves the other
   tiers and Major family share intact or higher.
2. The late-decade consolidation check needs a decade run plus the absorption mechanism. No absorptions fire in either
   arm through 1964. Re-check Major family share in 1968–69 when that lands.
3. Watch per-seed CR4 excursions above .40 (2 of 15 seed-years) in the next screens. They come from surviving MidTiers
   charting, not from the Majors.

## 8. Adopted into the working bundle (2026-10-22)

`--workload-scaled-staffing`, `--defer-british-labels`, `--midtier-national-ar` and `--midtier-going-concern-cash`
join the working bundle (evidence: sections 3, 6 and 7). The bundle now lives in `SimLogs/working-bundle/plan.json`.
`SimLogs/slate-screen-v9/plan.json` stays as that screen's record. Run it on the game profile:

```
--single-commercial-fit=2.5 --single-commercial-fit-hit-share=0.15 --single-commercial-fit-regional=0.5
--single-commercial-fit-scale=0.88 --single-commercial-fit-launch-weight=1.25 --single-commercial-fit-floor=0.12
--launch-stock-share=0.1 --sell-through-reorders --ai-master-deals --ai-backers --ai-master-national-pickups
--ai-master-capacity-decisions --major-offchart-singles=0.12 --major-offchart-singles-hold-units
--home-operated-micro-labels --home-label-running-cost=25 --founder-label-dormancy --founder-surplus-compensation
--distress-master-sales --prior-year-corporate-tax --hit-act-cadence --hit-act-retention --profile-performance
--workload-scaled-staffing --defer-british-labels --midtier-national-ar --midtier-going-concern-cash
```

The four switches stay opt-in in code, so default and player games are unchanged, and they exist only on this branch
until it merges. A build without them ignores the flags silently, so check that the branch is in before trusting a
run. Reusable controls for the new bundle are `mtn5-pair` (265 weeks) and `mtn-ab-gated-cash` (53 weeks), 3 seeds each.
