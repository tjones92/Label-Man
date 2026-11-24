# Hit-act follow-up gap: handoff (2026-11-14)

Background and the full chase: `SimTools/MajorShareRootCauseHandoff.md` (read its last five sections, from "third pass" on).

## The question

A hit act's next single reaches the Top 40 about twice as often at a Big-5 label as at an Independent or Small label. History says hit acts converted equally (1961–64 Top 10 per Hot 100 debut: Big-5 15.96%, field 15.93%; `HistoricalLabelTierBalanceResearch.md` §17aa). **What carries the gap?** The author wants stars kept strong and Big-5 1961 Top 10 share near history (20.6 / 21.2 / 26.0 for 1961–63).

## Where things stand

The commercial-fit shape, launch weight 0 and upside 1 (`lw0up1`), brings Big-5 share near history but weakens stars. Stars recover with `--single-commercial-fit-act-record-weight=2 --single-commercial-fit-act-record-odds-only` (`ao2`). In that mode an act's last Top 40 raises its odds of connecting, not the size of a hit. With it, Big-5 share drifts back toward the control.

Here a hit act is one whose last Top 40 week fell 26 weeks or less before the launch. Top 40 % of hit acts' singles, 1961–63, seeds 2002 + 1001, scored with `SimTools/analyze_hitact_followups_by_tier.py`:

| arm | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| control | 24.2 | 25.7 | 9.2 | 9.2 |
| `ao2` | 27.3 | 26.7 | 14.3 | 12.6 |
| `ao2` + footprint opened for hit acts (`lh1`) | 23.8 | 26.8 | 14.5 | 9.7 |

Hit-act singles are 29% of Big-5 launches, against 8% at Independents and 5% at Small labels. That is a separate cadence and endowment effect.

## Ruled out for this gap

- ~~**Launch footprint.**~~ **Not ruled out; see the finding below.** `--label-blind-launch-dose=1 --label-blind-launch-dose-hot-acts` (`lh1`) releases the arrival hold but lifts the fit-exposure chance only to the reference 0.57, which is about what an Independent already has. Fit-exposed regions went 4.3 → 4.6 of 7 (Big-5 6.5), so the screen never opened the larger half of the footprint.
- **Moving the whole kink on the act's record** (`ar1`, no odds-only). Stars recover, but Big-5 share goes to 45–53%: with upside 1, a kink shift multiplies every connected draw.
- **Upside cap** (`--single-commercial-fit-upside-cap`). Little effect on share, and #1 sales stay up.
- Earlier work already removed launch promotion terms, airplay at entry, the hit-act fit floor and the ration exponent as carriers of the overall premium.

## Hypotheses to check (telemetry first)

1. **Record quality.** Quality = 0.5 hook + 0.3 production + 0.2 dance; production is 0.4 × the label's `productionQuality`. Units respond to quality at about quality^14.6, so a 0.04 edge is worth about 2×. Compare hook, production and quality of hit acts' singles by tier, and their Top 40 rate by quality band within tier. `--label-craft-floor` exists and was worth about 3 points of share on top of `lw0`.
2. **The label's share of launch awareness.** With LW 0, awareness still sets the number of aware buyers linearly. Regress hit acts' launch awareness on label and artist terms (`single-launch-attribution.csv` has campaign impact, label reputation, recognition and momentum). Is a field hit act's awareness low because of its label?
3. **What happens after launch.** Weekly distribution (restock, rack shelf, one-stop terms) was about 17% of the overall gap and was never measured. Do field hit acts' singles that start well stall in weeks 2–6?
4. **Selection.** The Big-5 slate scores 34–42 candidates per slot, against 3–4 in the field. Do Big-5 hit acts release their best material while field hit acts release whatever is ready? `programme-choices.csv` has `qualityEstimate` for picks and non-picks.

## Finding (2026-11-14): the gap is Hot 100 entry, and the launch footprint carries the largest part

Sections 1–3 use existing runs only: `fit208-v1-*-base`, `fit208-v3-*-lw0up1_ao2`, `fit208-v4-*-lw0up1_ao2_lh1`, seeds 2002 + 1001, launches 1961–63. Scorer: `SimTools/analyze_followup_gap_carriers.py name=fmt…`. It replays each single's keyed fit draw and reads `single-demand-stages.csv`, the per-region weekly demand trace. The bundle's `--followup-chart-trace` already writes that file for every single an act releases in the 52 weeks after its first in-run Top 10, misses included ("the trace sample" below).

### 1. Where the gap sits: entry, not conversion

Hit acts' singles under `ao2` (Top 40 in the 26 weeks before launch):

| | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| draw connects % | 72.0 | 69.9 | 68.5 | 64.0 |
| reaches the Hot 100 % | 60.4 | 60.2 | 32.6 | 29.7 |
| Top 40 % | 27.3 | 26.7 | 14.3 | 12.6 |
| Top 40 per Hot 100 entry % | 45.2 | 44.4 | 43.9 | 42.4 |
| Top 10 per Hot 100 entry % | 21.5 | 20.2 | 19.9 | 20.2 |

Once a hit act's single is on the Hot 100 it converts the same at every tier. The whole 2× is whether it gets there.

History's figure is conversion per Hot 100 debut, so the sim has to be read the same way. Hit act = first Top 10 in Y − 1 (§17aa), `ao2`:

| | launches per act-year | reach the Hot 100 % | Hot 100 debuts per act-year | Top 10 per debut % |
|---|---|---|---|---|
| Big-5 | 4.40 | 67.6 | 2.98 | 20.9 |
| MidTier | 3.45 | 66.2 | 2.36 | 20.3 |
| Independent | 2.97 | 34.5 | 1.07 | 19.2 |
| Small | 2.81 | 29.9 | 0.91 | 21.8 |
| all non-Big-5 | 3.09 | 45.3 | 1.46 | 20.2 |
| history, Big-5 / other | | | 3.28 / 2.18 | 15.96 / 15.93 |

- **Top 10 per debut, Big-5 ÷ other: 1.03** (history 1.00 ± 0.18; the control is 1.16). The seeds give 1.07 and 0.98. On history's own measure `ao2` already converts equally.
- **Hot 100 debuts per hit act, Big-5 ÷ other: 2.04** (history 1.51). Launches per hit act are already at 1.42. The miss is the field's entry rate, 33% against 67%. At the Big-5 rate, non-Big-5 hit acts would debut about 2.1 records a year (history 2.18).
- So the target is field hit acts' Hot 100 entries: about 1.0 a year, against about 2 in history.

### 2. What carries it

Accounting on the trace sample under `ao2`: a logit of Hot 100 entry and of Top 40 on the keyed draw, quality, launch awareness, launch stock and the two regional gates. Independent singles are moved onto the Big-5 distribution one input at a time. Points of the gap closed by each step (the steps overshoot the entry gap by 4 points, which lands on the tier term):

| input moved to the Big-5's | Hot 100 entry (gap 22.3) | Top 40 (gap 13.6) |
|---|---|---|
| regions fit-exposed at launch | 6.4 | 3.9 |
| regions not held at launch | 3.5 | 2.5 |
| record quality | 5.3 | 3.7 |
| launch awareness, act terms | 4.3 | 0.8 |
| launch awareness, label terms | 1.2 | 0.2 |
| launch stock | 3.1 | 0.3 |
| connect bar (recency of the last Top 40) | 1.8 | 0.6 |
| left on the tier term | −4.4 | 0.7 |

**The launch footprint is the largest carrier: about 40% of the entry gap and 45% of the Top 40 gap.** It is two gates, both read off the label's distribution:

- `RegionalAudience.HoldUntilArrival`: awareness is zero in a region the label does not distribute to, until the record arrives there.
- `RegionalFitExposure.Launch`: each region is exposed on a keyed chance of 0.3 + 0.7 × reach. In an unexposed region a connected record's fit is capped at the flop level. Under upside 1 that is about a twelfth of a just-connected record (0.18 against 2.46).

Connected follow-ups at launch, of 7 regions:

| | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| regions not held | 7.0 | 6.7 | 4.4 | 3.5 |
| regions fit-exposed | 6.5 | 5.9 | 4.3 | 4.3 |
| share of the audience both open and exposed | 95% | 84% | 49% | 46% |

- With the two gates in the model the field's tier term disappears (Independent Top 40 log-odds −0.92, z −3.3, becomes −0.09). Without them it survives every other control tried: the act's chart record, fame terms, label push, label reputation.
- Exposure is a keyed draw, so within a tier it is a natural experiment. Its coefficient is positive on each seed, inside Big-5 + MidTier alone and inside the field alone (z 2.0–4.7 on entry).
- Matched cells (connected, quality ≥ 0.74, awareness 0.30–0.55): Big-5 87% enter and 65% reach the Top 40; the field 68 and 41; field singles open and exposed to at least 80% of the audience 92 and 62 (n = 13).
- The fit gate matters more under `ao2` than in the control (6.4 points of entry against 0.5). Upside 1 makes a connected draw worth more, and the act record connects 64–72% of hit acts' singles against 21–31%.

**Why `lh1` showed nothing.** The dose lifts an opened region's fit-exposure chance to the reference 0.57 (reach 0.38). An Independent's own chance is about 0.52. Under `ao2_lh1` regions not held went 4.4 → 6.7, but fit-exposed regions only 4.3 → 4.6, and the audience live 49% → 66%. The hold is the smaller half; opening it moved Independent entry by the 3 points the model gives it (30.8 → 33.8 in the trace).

### 3. The four hypotheses

1. **Record quality: a carrier, about a quarter.** Hit acts' singles, Big-5 / Independent: quality 0.736 / 0.703, hook 0.765 / 0.739, production 0.754 / 0.688. Production is 0.4 × label craft (0.84 / 0.71) + 0.3 × the act's studio performance + 0.2 × studio + noise. Label craft alone is about half the quality gap; the rest is hook. Split on all hit acts' singles (Independent): label craft 3.5 points of entry and 3.1 of Top 40; hook 3.0 and 2.8; the rest of production 1.0 and 0.9.
2. **The label's share of launch awareness: no.** Launch awareness = 0.08 + act terms (0.5 momentum + 0.3 reputation + 0.115 recognition) + label terms (campaign, 0.1 × reputation). Big-5 / MidTier / Independent / Small: label terms 0.23 / 0.26 / 0.21 / 0.20; act terms 0.25 / 0.28 / 0.09 / 0.08. The field's hit acts are smaller acts:
   - 56% of Big-5 hit-act launches come from acts already warm at their first in-run single (opening stars), against 20% in the field.
   - A fresh hit buys little. A Top 40 adds 0.02 momentum and 0.01 reputation (+0.013 awareness); a Top 10 adds +0.07. A home-grown hit act whose best recent peak was 11–40 launches with act terms of 0.03–0.05.
   - Home-grown hit acts alone still differ (entry 47.7 vs 31.8, Top 40 24.5 vs 14.0), so the endowment is not the whole gap.
   - Awareness moves entry (4.3 points) but hardly Top 40 (0.8). Launch stock (follower advance orders, median 35k vs 19k) is the same: 3.1 points of entry, none of Top 40.
3. **After launch: no separate carrier.** Connected follow-ups in weeks 1–3: purchase conversion (1.55% / 1.56%), the share of intent the singles purse clears (6% / 5%) and inventory fulfilment are the same at every tier. The Big-5 single has 1.65× the aware buyers (48% against 29% of the audience) and so 1.7× the units (median 1,290 against 745 a week). The Hot 100 bar is about 2,500 a week and the median entry comes in week 5. The median Big-5 follow-up grows from week 4; the median field one never does. That is the launch state compounding through the Top 40 gates, not weekly distribution. Top 40 per entry is equal.
4. **Selection: no.** `release-judgements-shadow.csv`: the best candidate beats the mean recordable candidate by 0.044 / 0.037 / 0.040 / 0.039, so every tier picks equally well, from 4.3 / 4.1 / 3.4 / 3.0 candidates. The pool's level differs (mean recordable 0.651 against 0.620), which is craft and hook again. Song sources are the same mix (artist-written 22%, professional 46–51%).

### 4. Confirmation run — `fit208-v5-{2002,1001}-lw0up1_ao2_lh1_he095`

New switch `--label-blind-launch-dose-exposure=P` (`Systems/LabelBlindLaunch.cs`, read in `RegionalFitExposure.Launch`): the fit-exposure chance an opened region reads at least, in place of the reference 0.57. Script arm `heN` (`he095` = 0.95, about a Major's own chance). Off by default and draws nothing; `--label-blind-launch-check` passes with new cases; the 27-week off run `hexp-off-2002-base` is byte-identical to the first 3,191 lines of `fit208-v1-2002-base-records.csv`.

The arm is `ao2` plus `lh1` plus `he095`: a non-Major hit act's single is held nowhere and fit-exposed at the Big-5 rate. Connected follow-ups at launch now read 6.8 regions not held and 6.5 exposed at Independents, 6.5 and 6.3 at Small labels (Big-5 7.0 and 6.5).

Hit acts' singles 1961–63, reach the Hot 100 % / Top 40 % / Top 10 %:

| arm | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| `ao2` | 60.4 / 27.3 / 13.0 | 60.2 / 26.7 / 12.2 | 32.6 / 14.3 / 6.5 | 29.7 / 12.6 / 6.0 |
| `ao2_lh1` (holds only) | 56.3 / 23.8 / 9.9 | 57.9 / 26.8 / 12.4 | 35.1 / 14.5 / 6.8 | 27.4 / 9.7 / 4.1 |
| `ao2_lh1_he095` (holds + exposure) | 59.0 / 27.1 / 12.4 | 59.8 / 27.0 / 14.6 | **40.3 / 17.7 / 8.9** | 31.4 / 13.9 / 6.0 |

- **Independents rise on both seeds at every rung**: entry 30.6 → 36.5 and 34.7 → 43.8; Top 40 12.0 → 14.7 and 16.7 → 20.6; Top 10 5.9 → 7.4 and 7.1 → 10.3. The Big-5 do not fall. About a third of the Independent entry gap closes (27.8 → 18.7 points) and a quarter of the Top 40 gap (13.0 → 9.4).
- **The unexplained field term is gone.** Without any gate term in the model, the Independent tier log-odds are +0.02 on entry (z 0.1) and −0.19 on Top 40 (z −0.8); under `ao2` they were −0.43 and −0.92.
- Small labels gain little and the seeds disagree (entry 35.9 → 28.4 and 25.0 → 33.5; about 180 singles a seed).
- History's frame: Independent Hot 100 debuts per hit act-year 1.07 → 1.43. All non-Big-5 1.46 → 1.51, because MidTiers fell 2.36 → 1.99. Big-5 ÷ other 2.04 → 1.96 (history 1.51); Top 10 per debut 0.98.
- **What is left of the Independent gap** in this arm (trace sample; entry gap 12.9, Top 40 gap 8.3): quality 7.7 and 5.7 points; act awareness 2.2 and 0.5; connect bar 2.3 and 1.3; launch stock 1.5 and 0.1. Record quality is now the main carrier.
- Share and stars, two seeds (cannot resolve under about 8 points):

| arm | Big-5 T10 1961 / 62 / 63 | Independent T10 1963 | stars 1961 / 62 / 63 | #1 median |
|---|---|---|---|---|
| control | 29.4 / 37.1 / 38.6 | 19.6 | 1.82 / 1.83 / 1.37 | 0.97–1.07M |
| `ao2` | 34.2 / 26.1 / 34.0 | 28.1 | 1.88 / 1.57 / 1.37 | 1.07–1.20M |
| `ao2_lh1` | 28.2 / 26.4 / 26.2 | 37.1 | 1.69 / 1.62 / 1.11 | 1.02–1.16M |
| `ao2_lh1_he095` | 26.3 / 31.6 / 30.0 | 37.8 | 1.70 / 1.71 / 1.06 | 0.90–1.01M |

  Big-5 share is not separable from `ao2` or `ao2_lh1` at two seeds (1962 reads 28.3 and 35.0). Stars sit with `ao2_lh1`. MidTier closures after 1960: 2 (both 1961).
- The switch is an ablation, like the dose it extends. The design it points to: a hit act's next single is serviced nationally whatever the size of its label. Non-Majors only and lift only, so the Majors' own footprint is untouched.

### Next

1. Four seeds for share and stars. Seeds 3003 and 6006 need the 208-week control, `ao2` and this arm: six runs, two batches.
2. Record quality on top of this arm. Label craft is half the quality gap; `--label-craft-floor` exists but lifts every single. A hit-act-only craft lift, or a look at why field hit acts' hooks fall when hot (0.750 → 0.738) while Big-5 hit acts' rise (0.735 → 0.765), are the two halves.
3. Act awareness: a fresh Top 40 buys +0.013 launch awareness. `--hit-act-hype` (a recency lift, screened 2026-10-23 before the fit change) is the existing lever.
4. Exposure dose-response (`he075`), and the regional stagger guard on this arm; `fx` failed it when every single was exposed everywhere.

### 5. Fix attempt on top of `he095` (2026-11-14): craft and awareness levers do not move field entry

Runs `fit208-v6-{2002,1001}-lw0up1_ao2_lh1_he095_{hc1,hy06}`, against `fit208-v5-*-…he095`.

- **New switch `--hit-act-craft-floor=F`** (`Systems/FootprintCraftSoftening.cs`, read where `CompetitorManager` cuts a single; arm `hcN`). A hot act's live non-Major AI single (Top 40 in the last 26 weeks when the record is cut, HitActCadence clock) reads label craft F of the way to the Major reference 0.85. With `--label-craft-floor` the larger lift applies. Off by default and draws nothing; `--footprint-craft-softening-check` and `--label-blind-launch-check` pass; the 27-week off run `hcf-off-2002-base` is byte-identical to the first 3,191 lines of `fit208-v1-2002-base-records.csv`.
- **`--hit-act-hype=0.3` is already in every bundle run.** `--lp-retail-v2-profile` expands `SimulationArguments.PreviousProfile`, which carries it. An `hy03` arm is byte-identical to its base. Arm `hyN` added to the script for other doses.

Hit acts' singles 1961–63, two seeds:

| arm | Indep. quality | Indep. awareness | Indep. reach H100 % | Big-5 reach H100 % | debuts/hit act, Big-5 ÷ other (hist 1.51) | T10/debut ratio |
|---|---|---|---|---|---|---|
| `he095` | 0.70 | 0.40 | 40.3 | 59.0 | 1.96 | 0.98 |
| `he095_hc1` | 0.72 | 0.41 | 40.3 | 58.5 | 2.12 | 0.92 |
| `he095_hy06` | 0.71 | 0.57 | 41.8 | 57.8 | 1.76 | 0.96 |

- **hc1:** Independent hit acts' quality rises by 0.02, as designed, but entry does not move. The ratio reads worse, inside noise.
- **hy06:** Independent awareness 0.40 → 0.57 buys 1.5 points of entry. The ratio's move to 1.76 comes mostly from Big-5 hit acts launching less (4.63 → 4.24 per act-year) and entering less, not from the field. Top 40 per entry falls (Independent 44 → 40). Big-5 T10 share 1961 is 29.2 (he095 26.3; history 20.6). The star metric (awareness ≥ 0.7) is not comparable under hy06, since hype puts twice as many singles over the bar.
- **What is left is the act, not the label.** Under `hc1` the Independent entry gap (19 points on the trace) splits as: quality 5.2 (now mostly hook), act awareness 3.4, launch stock 3.3, connect bar 2.5, regional gates 0.3, residual 4.4. The hook gap is composition hook *within* song source (artist-written: Big-5 0.819, Independent 0.780). Big-5 hit acts are 65% Established, 20% Star or Superstar; Independent hit acts are 38% Rising and 46% Established; Small 55% Rising. Hooks rise when an act is hot at every tier (Big-5 +0.057, Independent +0.036), so field hooks do not fall when hot.
- **Reading.** Once national servicing is in (`he095`), no label-side lever tried closes the remaining field entry gap. Field hit acts are weaker acts whose hit came partly on the fit draw, and they regress. History's equal conversion per debut is already matched. The miss sits in how many debuts a field hit act makes, and that now traces to who the field's hit acts are.

### Next (revised)

1. Adopt-or-not on `he095` alone: four seeds (3003 / 6006 need control, `ao2` and `he095`). `hc1` and `hy06` are not worth carrying.
2. Selection test: at the week of first in-run Top 40, compare acts' songwriting ability, career state and recognition by tier, sim against what history implies. If the field's hit acts are weaker than history's, the fix is upstream (who gets a hit: the fit draw's weight on luck against act craft), not in follow-up launch.

### 6. Selection test (2026-11-14): it is not who the field's hit acts are

Scorer: `SimTools/analyze_hitact_selection.py name=fmt…`. It reads history (`hot100_weekly.csv` plus the labelled Top 10 files) and the sim on the same frame. A hit act in Y has its first Top 10 in Y − 1 and takes that record's label. "Other" is every non-Big-5 label, as in §17aa. Both charts start in January 1960, so the new / experienced split uses Y = 1962–63. "New" means no Hot 100 record before the hit. **Chart events** count an act's debuts under 4 weeks apart once, because history's Hot 100 lists both sides of a two-sided hit and the sim's singles chart once.

**Chart events per hit act in Y, 1961–63:**

| | Big-5 | other | ratio |
|---|---|---|---|
| history | 2.56 | 1.80 | 1.42 |
| `he095` | 2.64 | 1.39 | 1.90 |
| control | 2.44 | 1.34 | 1.82 |

- **On events, Big-5 hit acts match history; the whole miss is the field, at 0.77× history.** Raw debuts overstate history's level: Big-5 3.30 debuts against 2.56 events. The A/B count is the like-for-like measure.
- **History's field hit acts were newer, not older.** New: history 60%, `he095` 48%, control 52%. Earlier Top 10: history 20%, sim 19–21%. Composition by chart record works in the sim's favour.
- **Within each cell (events, Y = 1962–63):**

| | history | `he095` | sim ÷ history |
|---|---|---|---|
| Big-5, experienced | 3.00 | 2.92 | 0.97 |
| Big-5, new | 1.78 | 1.70 | 0.96 |
| other, experienced | 2.59 | 1.83 | 0.71 |
| other, new | 1.18 | 0.76 | 0.64 |

  A Big-5 hit act behaves like history's, new or experienced. A field hit act of either kind makes about two-thirds of history's chart events.
- **Where the field cells lose (`he095`, Y = 1962–63):**
  - Experienced field hit acts launch 3.25 singles a year (Big-5 4.58) and enter 59% (71%).
  - New field hit acts launch 2.94 (Big-5 4.04) and enter 28% (45%).
  - So both cadence and entry are short. To reach history's 2.59 and 1.18 at the sim's own entry rates, the field would need about 4.4 and 4.2 launches a year.
- **Luck is real but small.** Field hits ride larger fit draws (keyed z: Independent 1.70, Small 1.99, Big-5 1.33) and sit lower in their year's quality ranking (0.80 against 0.84). The act's follow-ups lose more quality from the hit (−0.065 / −0.077 against −0.053). History's newer field hit acts still out-debuted the sim's, so this regression cannot be most of the gap.
- **Year pattern.** History's event ratio is 1.20 / 1.70 / 1.41 for 1961 / 62 / 63; `he095` reads 2.19 / 1.90 / 1.78. The 1961 miss is the largest. The sim's level also falls across years (Big-5 3.20 → 2.03 events) where history's holds.
- **Side observation.** The sim makes more hit acts: Big-5 27 a year against history's 17, other 74 against 59. Their hits are shorter (Big-5 3.8 Top 10 weeks against 6.2). That is the known Top 10 spacing issue, not this one.

**Reading.** The follow-up gap is not selection: the sim's field hit acts are no newer or weaker on chart record than history's. Once national servicing is in (`he095`), it is the field's follow-up path. A field hit act releases about 30% fewer singles than a Big-5 hit act, and each enters less often. A Big-5 hit act already matches history in every cell, so the fix should leave the Big-5 untouched and lift field hit acts toward Big-5 cadence. Earlier work: `hit-acts-release-too-little`, `repeat-hit-gap-is-independents` and the slate's 0.49 slots per field hit act against 4.27 per Big-5 hit act.

### Next (revised again)

1. Field hit-act cadence: why does a field hit act launch 3.0–3.3 singles a year against the Big-5's 4.0–4.6? Read `programme-choices.csv` and `release-slate.csv` for field hit acts (slots offered, financed, cancelled, reasons). A lever there, plus `he095`, is the candidate fix.
2. New field hit acts' entry (28% against 45%): the carriers in §5 (awareness, stock, connect bar) apply here most.
3. Four seeds before adopting anything.

### 7. Too many hit acts? Not the follow-up gap (2026-11-14)

The sim makes more hit acts than history: Big-5 27 a year against 17, other 74 against 59. It also turns the Top 10 over too fast (`he095`, per seed):

| | Top 10 records a year | weeks each | one-week Top 10 % |
|---|---|---|---|
| history 1960–63 | 94–111 | 4.7–5.7 | 7–13 |
| `he095` 1960–63 | 127–131 | 3.9–4.3 | 17–19 |

This is the reverse of the October bundles, where runs were too long (`top10-duration-is-rank-spacing`).

- **Dilution test: no.** Keep only history's count of hit acts per tier and year, the strongest by Top 10 weeks. Field chart events per hit act are then 1.41 (all 1.39; history 1.80).
- **By the hit's length:** field acts whose hit held the Top 10 for 7+ weeks make 1.59 events, against history's 2.04. The field shortfall holds in every band.
- **It may still matter for share.** The excess hit acts are mostly Big-5 (1.6× history's count against 1.25× for the field). That is a turnover question, separate from the follow-up gap.

### 8. Field hit-act cadence: the slate funnel (2026-11-14)

Scorer: `SimTools/analyze_hitact_cadence_funnel.py name=fmt…`. It covers the 52 weeks after a hit act's first Top 10 and reads `release-slate.csv` and `single-launch-attribution.csv`. `he095`, two seeds:

| per hit act | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| singles launched | 5.25 | 3.92 | 3.79 | 3.01 |
| slots | 6.85 | 4.96 | 4.82 | 3.72 |
| from the hit-follow-up lane | 2.03 | 3.71 | 3.51 | 2.88 |
| from the roster programme | 4.19 | 0.99 | 0.93 | 0.57 |
| cancelled | 0.57 | 0.36 | 0.50 | 0.31 |
| weeks from hit to first launch (median) | 5 | 5 | 7 | 8 |
| left the hit's label % | 7 | 11 | 12 | 8 |

- **Field slots are not lost; there are fewer of them.** Almost every field slot issues. Expiry, cash waits and departures are small, so the earlier expiry finding (`field-hit-cadence-is-lane-clock`) no longer binds on this bundle.
- **The field's cadence is the hit-follow-up lane's clock.** `HitActCadence.DueWeeks` = 13, a constant, gives about four slots a year. Big-5 hit acts get the roster programme's slots on top.
- **History in the same window** (an act's first Top 10 in 1960–62, chart events in the next 52 weeks):

| | history Big-5 | history other | `he095` Big-5 | `he095` other | control other |
|---|---|---|---|---|---|
| events | 2.65 | 2.08 | 2.75 | 1.72 | 1.47 |
| 3+ events % | 57 | 40 | 46 | 26 | 22 |
| no event % | 5 | 23 | 15 | 31 | 35 |
| median weeks between events | 14 | 13 | 12 | 12.5 | 13 |

  The spacing between events matches history. The field's shortfall is fewer runs that keep going.
- **History's release side** (`SimLogs/historical-hot100-1960-64/release-cadence-compare.txt`, a Wikipedia-discography sample of 61 act-years, not split by tier):
  - acts with 3–7 charting records released 3.64 singles a year; acts with 8+ released 4.68;
  - that implies about 53% and 63% of singles charted.
  - The sim's field hit acts launch 3.8 a year, about 45% charting. Big-5 hit acts launch 5.25, about 52% charting.
- **Reading.** Against history, the field is somewhat short on cadence (about 3.8 a year against roughly 4–4.7) and shorter on entry (about 45% against 53–63%). The Big-5 already over-release slightly and enter less often than history, and still match its event count. Both levers are candidates; on this evidence, entry is the larger.

### Next (revised again)

1. Cadence screen: put `HitActCadence.DueWeeks` behind a switch (non-Major hit acts only, for example 10 weeks), on top of `he095`. Watch field events per hit act, the event gap (history 13 weeks; it should not fall much) and the count of new field hit acts, which suffered in `hff-v3` (`field-hit-cadence-is-lane-clock`).
2. Entry on field follow-ups: the carriers left in section 5 (hook, act awareness, launch stock, the connect bar).
3. Top 10 turnover (section 7) as its own investigation.
4. Four seeds before adopting anything.

### 9. Cadence screen: `--hit-act-due-weeks-field` (2026-11-14)

**New switch `--hit-act-due-weeks-field=N`** (`Systems/HitActCadence.cs`, arm `fdN`). A hot act at a non-Major AI label is due its follow-up after N weeks instead of 13. Every read of the due clock goes through `HitActCadence.DueWeeksFor(label)`:
- the slate's due pick and waiting priority (`CompetitorManager.ReleaseProgramme.cs`);
- the delivery work queue and the routine follow-up review (`ProgrammeFollowUpScheduling.cs`, `CompetitorManager.ProjectJudgement.cs`);
- the legacy lottery lane (`AILabel.GetArtistForRelease` via `PickDueFor`);
- telemetry.

Majors and the player keep 13. It is off by default and draws nothing. `--hit-act-cadence-check` passes with new cases; the 27-week off run `fdue-off-2002-base` is byte-identical to the first 3,191 lines of `fit208-v1-2002-base-records.csv`.

Runs `fit208-v7-{2002,1001}-lw0up1_ao2_lh1_he095_{fd10,fd8}`, against `fit208-v5 he095`, two seeds:

| | `he095` | `fd10` | `fd8` | history |
|---|---|---|---|---|
| field launches per hit act, 52 weeks after the hit (Independent) | 3.79 | 3.81 | 4.03 | |
| field launches in Y, experienced / new hit acts | 3.25 / 2.94 | 3.35 / 3.03 | 4.09 / 3.19 | discographies 3.6–4.7 |
| field entry %, experienced / new | 59 / 28 | 60 / 32 | 59 / 29 | |
| chart events per hit act in Y, Big-5 / other | 2.64 / 1.39 | 2.65 / 1.41 | 2.64 / 1.64 | 2.56 / 1.80 |
| chart events in the 52 weeks after the first Top 10, other | 1.72 | 1.70 | 1.76 | 2.08 |
| median weeks between events, other | 12.5 | 13 | 12.25 | 13 |
| hit acts per seed-year, other | 74 | 70 | 76 | 59 |
| Big-5 T10 share 1961 / 62 / 63 | 26.3 / 31.6 / 30.0 | 37.4 / 32.1 / 31.6 | 25.2 / 26.8 / 31.1 | 20.6 / 21.2 / 26.0 |
| star Top 40 per act-year 1961 / 62 / 63 | 1.70 / 1.71 / 1.06 | 1.87 / 1.51 / 1.17 | 1.90 / 1.49 / 1.06 | |

- **fd8 lifts experienced field hit acts' cadence into history's range (3.25 → 4.09 a year) at unchanged entry, and their debuts rise 1.84 → 2.29.** New field hit acts barely move (2.94 → 3.19 launches, entry still about 28%).
- **The event gain depends on the measure.** By calendar year, other rises 1.39 → 1.64 and the event ratio falls 1.90 → 1.61 (history 1.42). In the 52-week window after an act's first Top 10, it is 1.72 → 1.76. Two seeds do not resolve it.
- **fd10 does nothing.** The slate's slot supply, not the clock, binds until the clock is well below the slot spacing.
- **No visible costs.** Event spacing holds at history's 13 weeks. There is no crowd-out of new field hit acts (74 → 76 a year; `hff-v3` lost them). Big-5 share and stars are not separable from `he095`. Independent departures within a year of the hit read 12% → 17%, against 8% under `fd10`: probably noise, but watch it.
- **Reading.** Cadence is a real but small part of the gap. Even at fd8, field chart events per hit act sit at 0.79–0.91 of history. The rest is entry, worst for new field hit acts (28% against 40–49% for new Big-5 hit acts). The section 5 carriers (act hook, act awareness, launch stock, the connect bar) are where that sits.

### Next (revised)

1. ~~Entry for new field hit acts~~: done in section 10. The single-arm figure (28% against 40–49%) overstated the gap.
2. If cadence is kept: four seeds of `he095` against `he095_fd8` (seeds 3003 and 6006 need their control and `ao2` runs first).
3. Top 10 turnover runs in its own session, launched 2026-11-14.

### 10. Entry for new hit acts (2026-11-14)

**History's Big-5, corrected.** The selection scorer now groups history by *producing label* (Columbia, RCA Victor, Capitol, Decca, Mercury) by default, like the sim's flagship `label_0001–0005` and the share targets (`SimTools/Top10TurnoverFindings.md`); `--owner` keeps §17aa's owner grouping. The per-hit-act baseline barely moves:
- chart events per hit act in Y, Big-5 / other: 2.57 / 1.82 (owner grouping 2.56 / 1.80);
- history has 14.7 Big-5 hit acts a year against the sim's 27.

Sections 6–9 stand.

**New switch in the carrier scorer:** `analyze_followup_gap_carriers.py --subset=new|experienced`. A new hit act's only charted record before the launch is its hit, and it has no opening career.

**New hit acts' follow-ups, Hot 100 entry %, 1961–63, two seeds.** All the `ao2` arms that leave the Big-5 untouched:

| arm | Big-5 | MidTier | Independent | Small |
|---|---|---|---|---|
| control | 26.0 | 22.0 | 13.8 | 15.4 |
| `ao2` | 38.7 | 30.6 | 24.2 | 29.2 |
| `he095` | 42.4 | 31.6 | 27.0 | 23.0 |
| `he095_hc1` | 32.9 | 35.5 | 29.4 | n/a |
| `he095_fd10` | 35.7 | 35.2 | 27.0 | 26.0 |
| `he095_fd8` | 30.1 | 29.8 | 22.3 | 30.8 |
| **pooled `ao2` family** | **36** | **33** | **26** | **27** |

- **The new-act tier gap is about 10 points, not 15–20.** New Big-5 hit acts' entry swings 30–42 across arms that do not touch the Big-5 (about 160–200 singles a cell), so single-arm cells mislead.
- **Experienced hit acts carry the larger gap:** `he095` Big-5 62%, Independent 48%, Small 41%.
- **New hit acts are a common low level at every tier:** 26–36% entry. History's new field hit acts made 1.19 chart events a year, against the sim's 0.76–0.93. At the sim's 3.0–3.2 launches a year, that needs 37–40% entry.

**What carries the new-act gap** (`he095`, trace sample; the Independent entry gap is 19 points there, inflated by that arm's high Big-5 cell):
- quality 12.5 points;
- connect bar 3.7;
- regions held at launch 2.3 (Small 7.0: Small labels' new hit acts still launch with 90% of the audience open);
- awareness about 2.5;
- residual 1.5.

Quality of new hit acts' follow-ups:

| | Big-5 | Independent | Small |
|---|---|---|---|
| quality | 0.714 | 0.678 | 0.673 |
| hook | 0.723 | 0.699 | 0.691 |
| production | 0.744 | 0.683 | 0.667 |
| label craft | 0.839 | 0.700 | 0.657 |
| career Rising % | 50 | 76 | 72 |
| career Established % | 44 | 6 | 12 |

The Independent gap of 0.036 is label craft 0.017, hook 0.012 and dance 0.006 (dance is a random draw). The hook gap is the act's own songwriting, as for hit acts generally: artist-written composition hook 0.708 against 0.747. Field new hit acts lean on outside material: 46% professional and 23% recent-hit covers, against 34% and 16%.

**The craft half was already screened.** Under hc1 (`--hit-act-craft-floor=1`), new Independent hit acts' quality rose 0.68 → 0.70 and entry 27.0 → 29.4. That is +2.4, inside the ±5 noise of these cells. The accounting's quality weight (12.5 points for 0.036) overstates what a causal craft lift buys. Quality in the cross-section carries the act (hook, career) as well as craft.

**Reading.**
- **New hit acts:** the tier gap is about 10 points, about half quality, of which half is label craft. hc1 buys a few points at most. The rest is that new field hit acts are mostly Rising acts with weaker writing, which history also had (60% of history's field hit acts were new).
- **Experienced hit acts:** the gap (62 against 48) is diffuse: quality 5.3, stock 2.2, act awareness 1.7, connect bar 1.6 points.
- **No single label-side lever is left.** National servicing (`he095`) was the large one. Cadence (`fd8`) and craft (`hc1`) each add a little, inside two-seed noise.

### Next (revised)

1. **Package test on four seeds:** `he095`, `he095_fd8` and `he095_hc1_fd8` against the control and `ao2`. Seeds 3003 and 6006 need all five; 2002 and 1001 need only `he095_hc1_fd8`. Score:
   - chart events per hit act, Big-5 and field, against history 2.57 / 1.82;
   - Big-5 Top 10 share against 20.6 / 21.2 / 26.0;
   - stars, #1 sales and closures.
   A package of small effects can pass where each part is inside noise.
2. **Small labels' holds for new hit acts** (90% of the audience open; regions held worth 7 points). Check why `lh1` leaves Small labels' hit acts partly held: the hot-act test, or reach-0 regions the dose skips.
3. **Top 10 turnover:** `SimTools/Top10TurnoverFindings.md`. `--chart-inertia-base=0.4` carries about a third of the extra Top 10 records; turnover does not inflate Big-5 share.

## Method notes

- **Runs:** `SimTools/run_indie_access_screen.ps1 -Seed N -Weeks 208 -Arms … -RunPrefix X -Extra --lean-probe`.
  - Arm tokens: `lwAupB`, `arN`, `aoN`, `capN`, `lhN`, `heN`, `hcN`, `hyN`, `fdN`; a leading 0 means a decimal point (`lw025` = 0.25).
  - This machine has 7 GB, so run two or three at a time, one seed per batch. A 208-week run takes about 15 minutes, measured from each request file's start time to its last CSV write (27 weeks: under 2 minutes; 104 weeks: 7).
- **Existing runs** (all on the reference bundle):
  - control `fit208-v1-{2002,1001}-base`;
  - `fit208-v1-*-lw0up1`;
  - `fit208-v3-*-lw0up1_ao2`;
  - `fit208-v4-*-lw0up1_ao2_lh1`;
  - `fit208-v5-*-lw0up1_ao2_lh1_he095`;
  - `fit208-v6-*-lw0up1_ao2_lh1_he095_{hc1,hy06}`;
  - `fit208-v7-*-lw0up1_ao2_lh1_he095_{fd10,fd8}`.
- **Noise.** Two seeds can't resolve effects under about 8 points at the Top 10. Single seeds swing 8–15 points; for example `ao2_lh1` 1963 read 17.7 on one seed and 34.6 on the other. Confirm anything on four seeds (2002 / 1001 / 3003 / 6006; skip 4004 / 5005). The 208-week control does not exist yet for 3003 or 6006.
- **Scorers:**
  - `analyze_fit_shape_long.py SEEDS name=fmt…` for share, stars, closures and #1 sales;
  - `analyze_hitact_followups_by_tier.py name=fmt…`;
  - `analyze_hitact_cadence_funnel.py name=fmt…` for the slate funnel of hit acts' follow-ups;
  - `analyze_hitact_selection.py [--owner] name=fmt…` for the selection test (history against sim, chart events, new / experienced cells);
  - `analyze_followup_gap_carriers.py [--subset=new|experienced] name=fmt…` for the entry funnel, history's frame, the regional gates and the carrier accounting (needs `single-demand-stages.csv`, which the bundle's `--followup-chart-trace` writes);
  - `analyze_softening_guardrails.py`, which reads each run's fit flags from its request file.
- **New switches:** off by default, no global RNG draws, a check in `SaveLoadRoundTripRunner`, and a byte-identical 27-week off run.
  - Compare against the first N lines of `fit208-v1-2002-base-records.csv`.
  - `lbl-off`, `dose-off` and `skiptel2-off` predate the funding flags in the bundle and are no longer valid off references.
- **Build and Python:**
  - Build with `dotnet build "Label Man.csproj" -c Debug`, never while runs hold the DLL.
  - Python is `$LOCALAPPDATA/Programs/Python/Python314/python.exe`.
  - Write multi-line scripts with a file tool, not a bash heredoc that contains apostrophes.
- **Commits:** none. Everything is uncommitted on `stature-fanbase`. Run outputs go to `SimLogs/`.

## Separate open item (not this task)

The median #1 sells about 1.0M in the control and 1.1–1.3M in the fit arms, against the author's 0.80M target. The fit tail is not the main driver.
