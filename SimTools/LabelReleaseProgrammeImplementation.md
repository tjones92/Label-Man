# Label release programme implementation

October 22, 2026. Experimental implementation of the model described in
[LabelReleaseSchedulingResearch.md](LabelReleaseSchedulingResearch.md). The
research is design context; its historical examples are not universal quotas.
The existing working bundle is unchanged when both new switches are absent.

## Controls

| Switch | Behaviour |
| --- | --- |
| `--label-release-slate` | Forward allocation, independent single/LP clocks, prepared masters, dated issue intentions and cash holds under the comparison envelope. |
| `--label-release-capacity` | Dated work/issue capacity with multiple ordinary launches per week and a finite company promotion pool. Without the slate, roster/format choice retains the existing release lane/lottery. |
| Both | The slate uses the revised capacity and conserved promotion. |

Either switch retains hit-act cadence, its same-act launch split, and retention.
The slate replaces the lane's artist selection with explicit follow-up intentions.
The separate `AlbumReleasePractice` bundle is not enabled by these switches.
Slate LPs use the existing LP project and finance path; the single programme
has its own intentions, rather than automatically minting a promo. At issue, a
prepared slate LP takes the researched advance-single decision
(`ResolveRecordedAlbumCampaign`, the `AlbumReleasePractice` commission prior
and incremental test) without enabling the rest of that bundle. An approved
advance single becomes the existing `AlbumWithPromo` project. The single uses
the LP's dated slot, and the LP drops after the existing gap. The single
competes for production cash, the promotion pool and the act's single clock.
Promo-led LPs were uncommon in 1960–64 ([AlbumReleasePracticeResearch.md](AlbumReleasePracticeResearch.md)),
so this is deliberately rare, unlike the control's near-automatic promo.

Parameters are modelling probes, not calibrated shipping defaults:

* Eight-week rolling issue horizon, at least two weeks from proposal to issue;
  sessions normally complete one week before the intended issue. Preparation
  and issue each have dated work slots, so filling a horizon cannot produce
  eight weeks of sessions at once. Work left idle in a week (an empty, failed
  or cash-short slot) prepares the next intention early, or issues one prepared
  in an earlier week, so that one failure does not push back every later
  intention. A master is never recorded and issued in the same week, except for
  the label's first horizon. That horizon starts in the current week, because an
  opening or newly founded label already has work in hand.
* An act may hold several open single intentions spaced by the single floor,
  and at most one open LP intention.
* `--slate-single-floor=6` (integer 1–13): an explicit single feasibility floor.
  The 13-week hot-act reference supplies follow-up urgency. LP feasibility has
  an independent 18-week floor and retains existing annual LP project checks.
* `--programme-major-singles=10` (0–16/month) and
  `--programme-major-albums=5` (0–12/month): a head capacity scenario. The LP
  value is a sensitivity assumption, not an RCA LP policy extracted from the
  sources. Other tiers, including studio MidTiers, retain their own existing
  total monthly envelope, divided .67 singles / .33 LPs. No national-total
  feedback controller or uniform tier multiplier is present.
* Four-week promotion commitments. A label's simultaneous campaign pool is its
  standard campaign budget times `max(1, min(4, releasesPerMonth))`. Increasing
  the experimental Major programme does not enlarge that pool. Planned campaigns
  share it and competing campaigns are rechecked at issue. Purchased masters,
  delayed LPs, and optional campaign follow-ups also respect remaining capacity.
  Launch label awareness, label radio push and payola, ongoing single push and
  ongoing LP push carry the resulting share. The act's own split remains
  distinct. Opening campaigns represent previously paid work, not fresh cash;
  their remaining capacity is allocated once without changing opening inventory.

The original Bernoulli release path remains the control. In the allocation-only
arm, its nominal opportunity rate (including status/staff/season adjustments) is
divided between dated format slots. Both slate arms drop eligible-act/3 and the
old any-format cooldown from the probability calculation: eligibility and the
independent clocks are already explicit feasibility checks. Realized volume can
differ because feasibility, preparation and LP coupling differ. Status and staff scale available work
slots, and seasonal availability changes dated slots. Recording-cost seasonality
continues through the existing production-cost calculation. There is no saved
capacity bank. Failure can make room for another ready intention in the same
dated slot; postponed work still needs a later week's capacity.

## State and finance

`CompetitorManager.ReleaseProgramme.cs` owns the programme. Each intention has
an immutable dated slot identity, act, format, original/current issue week,
intended/actual date, source, observed decision evidence, score, readiness,
reservations, actual master ID, paid production, review reason and missed-window
marker. Proposed work becomes in preparation, ready, scheduled, postponed,
issued or cancelled. A recent observed breakout can advance a review inside the
horizon while allowing preparation time. Contract changes, terminal artists,
label closure and player ownership cancel outstanding AI intentions.

Ranking uses publicly observed career standing, momentum, regional results,
delivery obligations, waiting time, a contribution prior and scouting uncertainty.
It reads neither an unmade record's realized quality nor future chart outcomes.
Suitable material and sessions use the existing record/repertoire/crew services.
At preparation, the existing project accounts pay actual production once, and
the act's advance balance receives that cost. The prepared master is saved in
the intention and reused at issue. It is not rerolled or charged a second time.
Cancellation preserves the paid cost and frees unspent holds; it creates no
refund. Prepared-but-unissued costs are reported separately from issued-record
losses, which necessarily exclude those projects.

Holds start at the preparation lead (two weeks before issue), not at proposal.
If unreserved cash or promotion is short, the work is postponed, not cancelled.
Only the window cancels it. Soft production, promotion and (after recording)
initial-pressing holds enter
the existing `EconomicLabelLifecycle.CommitmentReserve`; they are not expenses,
cash credits or a second finance ledger. The currently issuing/recording item
is excluded from its own hold while other intentions remain protected. Actual
pressing, rights, artist availability, profitability and cash are rechecked by
the existing launch/project path. Actual recording costs can differ from the
estimate; the resulting paid cost and liquidity shortfall remain visible.

The studio parent's monthly support explicitly requests the reserve **without
programme holds**. Planning a new launch cannot increase parent support. A
native probe exercises an actual support review at the non-programme reserve.
Studio adoption and its known historical guards remain separate decisions.

Independent format clocks update on actual issue, including external-master
routes. LP issue cannot reset the single clock. For old worlds without these
clocks, surviving format-specific catalogue supplies the initial date; otherwise
contract age or the opening elapsed interval supplies an explicit fallback.
This cannot reconstruct a retired pre-adoption issue date that was never saved.

Terminal intentions retain 52 weeks of history. Open work expires eight weeks
after its original date. Departed actors' clocks and retired campaigns are pruned;
active-record campaign scalars remain until retirement. Save v28 carries the
programme, prepared records, clocks, semantic identity keys and planning frontier.
Switches-off worlds omit the entire optional field and retain their previous
save version/schema. A world that has programme state continues to require v28
even if its switches are subsequently turned off.

New schedule decisions use Philox keys from saved label name/founder/founding
place, act name/origin/founding members, dated slot, week and decision domain.
Generated `label_N` / `artist_N` / `gen_N` counters are lookup keys, not the new
randomness identities. Identity descriptors are frozen on first use so renaming
or a later lineup change does not change existing reservations' dice. Existing
record/session generation still uses the existing simulation streams; an enabled
programme deliberately reshuffles the world. Neither switch off consumes new
draws.

## Measurement

Enabled runs append `*-release-slate.csv` and `*-programme-resources.csv`.
The former is a transition ledger, including reasons, paid costs and reservations.
The latter reports label/week attempts, successes, idle work, multi-issue weeks,
promotion pool/bookings, cash holds, format rates and due/unplanned follow-ups.
Ownership transfers end the selling firm's campaign; they cannot transfer its
promotion department to the buyer for free. Any new paid campaign uses the
buyer's remaining pool. The cap is reapplied after backer funding, whose minimum
loan tranche can otherwise raise the legacy fallback's launch marketing spend.
Actual single/LP issues continue to come from `demand-release-census`, never from
strategy decisions or LP scheduling counts.

`RunLabelReleaseProgrammeAudit.ps1` runs native mechanisms and real-world
save/load checks in both studio settings, then the allocation × capacity × studio
eight-arm screen. Every arm retains the accepted working flags, cadence and
retention; manifests pin arguments, assembly hash, seeds and completion. Its
default is three seeds, 265 weeks. One seed or short runs are mechanism/smoke
evidence, not adoption evidence.

```powershell
./SimTools/RunLabelReleaseProgrammeAudit.ps1 -RunTag slate-v1 -Seeds @(1001,2002,3003) -Weeks 265
```

`analyze_label_release_programme.py PREFIX ... --output SimLogs/REPORT.json`
validates duplicate identities, promotion conservation and issue readiness, and
reports calendar-year issues and tier shares, LP:single ratios, Major conversion,
Big-5 share and Top 10 CR4, distinct Hot 100 acts, observed single gaps, starting
Small closures, finalized losses, prepared/cancelled costs, idle resources and
missed follow-ups. It marks partial years and censored conversion/loss cohorts.
Directory estimates are provisional centres with the `/0.90` sensitivity shown
separately; no hard historical gate is asserted without scope verification.
Operating labels remain the firm proxy: the corporate-family/imprint mapping
and studio-specific capacity evidence still require research before calibration
acceptance. The report always leaves `calibrationAccepted` false.

Use `--plan SimLogs/TAG/plan.json` instead of prefixes to require all eight arms,
matching flags/assembly/duration and complete, distinct seed cells. This adds
per-seed allocation, capacity, combined and interaction effects with equal-weight
means, plus the realized allocation/control issue ratio. A matched expected
envelope does not establish matched actual volume: preparation, failed material,
cash holds and removal of automatic LP promo singles can change throughput. The
report explicitly flags that limitation before interpreting allocation effects.

The current demand allocator limits local units by regional market and format
budgets, then allocates remaining eligible demand through the existing clearing
and spillover paths (`ChartManager`); a larger slate does not add a target-seeking
demand budget. Read total market units and the existing demand/clearing ledger
alongside per-issue results, since record availability can change realized sales.

## Validation record

Generated manifests and comparisons live under `SimLogs/`, not in this source
directory. Multi-year calibration remains required before changing the game's
default programme or adopting the studio arm.

### slate-screen-v6 (seed 1001, 53 weeks; smoke evidence only)

All four native checks (mechanism and save/load, studio off and on) passed. The
switches-off control replays v4 byte-identically across 95 CSVs. Report:
`SimLogs/slate-screen-v6/screen-review.json` (`--plan`, 8 arms).

| 1960 | Singles | LPs | Major singles | Hot 100 acts | Big-5 Top 10 | Major conversion | Single units (M) | LP units (M) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| off control | 4346 | 1083 | 286 | 500 | 29.4% | 10.5% | 150.7 | 26.1 |
| off allocation | 3051 | 1045 | 159 | 405 | 28.5% | 12.6% | 140.1 | 25.9 |
| off capacity | 5257 | 1512 | 879 | 501 | 23.8% | 3.0% | 139.0 | 42.9 |
| off both | 2933 | 1039 | 367 | 433 | 32.7% | 7.6% | 141.0 | 27.9 |
| on control | 4325 | 998 | 293 | 469 | 33.1% | 13.3% | 150.8 | 24.4 |
| on allocation | 2865 | 1021 | 169 | 429 | 19.0% | 9.5% | 144.1 | 25.7 |
| on capacity | 5133 | 1417 | 893 | 504 | 30.4% | 2.8% | 141.4 | 40.2 |
| on both | 2749 | 994 | 370 | 409 | 24.8% | 5.7% | 144.5 | 26.2 |

Neither switch is adoptable. The pattern is the same with studio off and on:

* **The slate issues ~30% fewer singles** (allocation/control issue ratio .75
  off, .73 on); the provisional directory ratio falls from .75 to .51. Most of
  this is not lost programme work. The control's singles include **automatic LP
  promo singles** (1,132 of 4,346 studio off; 1,050 of 4,325 on). The slate does
  not mint those by design. Like for like, programme singles are 3,051 against
  the control's 3,214 (−5%) with studio off, and 2,865 against 3,275 (−12.5%)
  with studio on. The smaller programme losses were cash cancellations at
  planning (363), expired windows (238), slots with no eligible act (~380) and
  the empty two-week opening lead. MidTier converted 40% of its proposals
  (87 of 217), against 73–90% for other tiers. v9 below repairs these losses.
* **Capacity triples Major singles but not their hits.** Major conversion falls
  from 10–13% to 3%, the single loss share rises from .67 to .75, and LP units
  rise 65%. A fixed promotion pool spread over three times the issues yields
  flops, as the conserved-pool design predicts.
* **Big-5 Top 10 share is noise in this screen.** It ranges from 19% to 33% with
  no consistent sign across the matching arms: the combined arm is +3.3 points
  with studio off and −8.3 with studio on. The 32.7% studio-off combined result
  is not a finding by itself.
* Single market units fall 4–8% in every treatment arm, despite the treatment
  arms' different volumes. Starting Small closures are 0–3 in every arm.

### slate-screen-v9 (throughput repairs; seed 1001, 53 weeks; smoke only)

Changes: holds start at the preparation lead and short cash postpones instead of
cancelling. Acts may hold several open singles spaced by the floor. Idle dated
work prepares or issues the next intentions early. The first horizon starts in
the current week. Work queued behind preparation capacity releases its holds.
All native checks passed, and controls are unchanged from v6. Report:
`SimLogs/slate-screen-v9/screen-review.json`. v8 (the same changes without the
queued-hold release) gave the same result within reshuffle noise.

"Programme singles" excludes automatic LP promo singles.

| 1960 | Singles | Programme singles | MidTier programme | LPs | Big-5 Top 10 | Major conversion |
| --- | --- | --- | --- | --- | --- | --- |
| off control | 4346 | 3214 | 179 | 1083 | 29.4% | 10.5% |
| off allocation (v6) | 3051 | 3051 | 88 | 1045 | 28.5% | 12.6% |
| off allocation | 3238 | 3238 | 114 | 1132 | 27.5% | 12.2% |
| off both | 3201 | 3201 | 113 | 1161 | 25.0% | 5.3% |
| on control | 4325 | 3275 | 329 | 998 | 33.1% | 13.3% |
| on allocation (v6) | 2865 | 2865 | 155 | 1021 | 19.0% | 9.5% |
| on allocation | 3111 | 3111 | 223 | 1076 | 23.1% | 11.7% |
| on both | 3080 | 3080 | 216 | 1171 | 20.0% | 3.7% |

* Cash cancellations at planning fell from 363 to 0. MidTier cash postponements
  fell ~70% and its mean holds halved. Programme singles now match the control
  with studio off (+0.7%) and trail by 5% with studio on.
* MidTier remains at 64–68% of the control's programme singles. Its binding
  reason moved from cash to preparation capacity, because its dated rate falls
  by about half over 1960. That fall comes from label closures: ~16 of 26
  MidTiers are defunct by December in the slate arm, ~14 in the control. The
  slate plans eight weeks ahead for labels that do not survive, so those
  intentions expire. This is not a throughput defect, and one seed cannot say
  whether the slate's extra closures are real.
* **Why MidTiers close (control, pre-existing).** In the control, 14 of the 26
  starting MidTiers go bankrupt in 1960, including Chess, Atlantic, King,
  Cameo-Parkway, Parlophone and Pye. Staffing is set by a fixed tier term
  (`LabelCorporateModel.SetRequiredStaff`, scale 1.8), so every MidTier pays
  about $10.5–17.5k a month, even with no acts. The failures had about 4 acts
  and 2.6 launches in their first 13 weeks; survivors had about 10 acts and 6.1
  launches. Their opening gross of $0–8k a week cannot carry that payroll.
  `Struggling` status then halves the release chance. Parlophone and Pye have
  no acts and no catalogue, yet employ ~14 FTE. Survivors end 1960 with
  $70k–$590k. The outcome is decided by the size of the opening roster, which
  is a world-calibration issue outside the slate.
* The remaining headline gap (~1,100 singles) is the automatic LP promo single.
  v11 below adds the researched advance single instead of restoring the
  control's near-automatic promo.
* Capacity still multiplies Major issues without hits (Major conversion 3.7–5.3%
  in the combined arms).

### slate-screen-v11 (advance singles; seed 1001, 53 weeks; smoke only)

All native checks passed, and controls are unchanged. The analyzer accepts an
advance-single LP still pending at run end and reports `slateAdvanceSingles`.
Slate arms now write `*-album-practice.csv` decision rows. Report:
`SimLogs/slate-screen-v11/screen-review.json`.

| 1960 | Singles | LP promo/advance | Promo per LP | Programme singles | LPs | Big-5 Top 10 | Major conversion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| off control | 4346 | 1132 | 1.05 | 3214 | 1083 | 29.4% | 10.5% |
| off allocation | 3286 | 25 | .022 | 3261 | 1153 | 23.1% | 13.9% |
| off both | 3204 | 19 | .016 | 3185 | 1163 | 24.8% | 5.4% |
| on control | 4325 | 1050 | 1.05 | 3275 | 998 | 33.1% | 13.3% |
| on allocation | 3093 | 13 | .012 | 3080 | 1065 | 29.0% | 11.2% |
| on both | 3089 | 24 | .021 | 3065 | 1170 | 16.5% | 3.8% |

* Advance singles go out with 1.2–2.2% of slate LPs. The earlier album-practice
  candidate reported 1.99%. Decision reasons (studio off, allocation):
  format-or-market 794, no commission 323, approved 27, economics 1. Format
  exclusion (non-Standard/Concept LPs, Classical) and the commission prior do
  the selecting; the incremental test almost never binds.
* A first version required a free single slot in the same week. That blocked
  992 of 1,109 LP decisions by calendar accident, giving 0.1–0.7%. It was
  replaced by using the LP's own dated slot.
* The control's ~1.05 promos per LP is the outlier against the research. The
  slate's lower total single count is therefore a deliberate difference, not a
  throughput loss. Compare arms on programme singles plus advance singles.
* Big-5 Top 10 share still swings ±8 points between arms with no consistent
  sign; capacity still lowers Major conversion. Both need the 3-seed, 265-week
  screen before any adoption reading.
