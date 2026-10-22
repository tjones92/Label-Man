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
