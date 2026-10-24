# Label release programme implementation

October 22, 2026. Experimental implementation of the model described in
[LabelReleaseSchedulingResearch.md](LabelReleaseSchedulingResearch.md). The
research is design context; its historical examples are not universal quotas.
The working bundle now includes the slate, revised capacity and studio labels,
following the user's decision to continue implementation with all three enabled
and reassess the economy and statistics afterward. This is the active development
base. Calibration results remain observations for that later reassessment.
The prior `mtn5-pair` bundle is preserved in
`SimLogs/working-bundle/mtn5-pair-plan.json`; the legacy release path remains
available by using that bundle. Cadence and retention stay enabled in both.

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

An LP that launches an advance single now enters `AwaitingDelivery`. Its actual
issue date stays empty until the existing album project delivers the LP. The
project owns the recorded master, outstanding finance and any ownership
transfer; the slate holds no duplicate master or cash reservations while it
waits. A cancelled project cancels the intention without refunding paid
production. Earlier v28 advance-single entries that were prematurely marked
issued are reconciled with their existing project's delivery state on resume.
Delivery completes the original intention, including after an ownership change.

Delayed LP campaigns now book the label's promotion pool at actual delivery,
just as immediate releases do. The existing project makes the cash payment;
the campaign booking only records capacity. Snapshot label awareness and
ongoing LP push read the resulting share. Initial campaign allocation also
subtracts any campaign already booked earlier in the first resumed week.

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

Rushes and postponements now share one rescheduling path. Moving a date frees
all unspent production, promotion and pressing holds before readmission at the
revised date. The prepared master, paid recording cost and original intended
date remain intact. A prepared master rebooks marketing and initial pressing,
without reserving production again; the quote excludes its own existing holds
from cash and promotion availability. Insufficient pressing cash postpones the
launch even when marketing alone would fit. A prepared item's revised holds
can be acquired during the preparation lead, before its issue week.

The actual session week is saved separately from planned readiness. Overdue
masters wait until at least the following week to issue, including when the
LP's first event is an advance single. Only work recorded on its original
opening-week slot can issue that week; postponed opening work has the ordinary
lead. The same boundary applies when using an idle issue slot early. Rescheduling
retains the session date and paid master. This is an optional additive v28 field;
older saves without it retain their existing readiness boundary because the
actual past recording date cannot be recovered. New audit rows expose the
session and proposal weeks; older audit streams remain readable.

Idle work observes the same review boundary as ordinary due work. A failed or
postponed intention waits until a later week, while another feasible intention
can use the unused slot. Failed early preparation and early issue release all
unspent holds without shifting the original future date or discarding a paid
master. Every slate issue requotes live cash, initial pressing and promotion for its
actual launch week, excluding its own old holds. A future promotion reservation
cannot finance an early launch against a congested current pool. New audit rows
declare this weekly review policy so the analyzer can reject same-week retries
and retained holds without imposing the rule retrospectively on older streams.

The studio parent's monthly support explicitly requests the reserve **without
programme holds**. Planning a new launch cannot increase parent support. A
native probe exercises an actual support review at the non-programme reserve.
Studio adoption and its known historical guards remain separate decisions.

Subsidiary absorption retains the operating label's tier, roster contracts,
slate, paid masters, pending LP projects, format clocks and semantic identities.
Its cash and promotion books remain with that imprint rather than being copied
to the acquiring Major. Ownership rolls up for corporate reporting and payouts;
it does not manufacture a larger promotion pool or restart the release horizon.
The corporate discretionary-spending buffer now adds unspent programme holds
once, protecting them from shareholder distributions and capital purchases.
Paid production is not a hold and is not reimbursed by the ownership change.
When a hold is released, the underlying cash can become available to a later
eligible payout or investment, without a refund or new financing.

Distribution financing uses a shared funding floor while the programme is
enabled. It preserves the existing three-month operating buffer, protects the
larger signing reserve for taxes, plant bills and recent losses when necessary,
then adds unspent slate holds once. Receivables and paid masters are not cash.
New advances are quoted from cash above that floor. Poaching quotes first allow
for the incumbent's unrecouped balance, then cap the new cash advance; commitment
rechecks both payments together before dropping another client, retiring the old
claim or moving any cash. The bought-out claim is carried once in the replacement
deal's unrecouped balance, separately from its new cash advance. Voluntary exit
settlements use the same protected-cash boundary and retain the existing writeoff
rule for the unpaid remainder. With the programme off, the prior funding and
settlement policy remains available.

Player offer acceptance also rechecks the distributor's current funding. An
unfunded or unavailable offer leaves the pending offer, current contracts and
cash unchanged and returns a failure message. A successful acceptance clears
the pending offer only after the financed contract is committed.

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
`promotionCommittedPeak` also reports the maximum overlap of paid campaigns
and unspent future promotion reservations. Campaign intervals end before a new
one starts in the same week. The analyzer checks that future peak against the
same label pool when capacity is enabled, and records how many label-weeks
carry this audit; older streams retain their actual-bookings-only check.
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
default is now one seed, 53 weeks, for the first screening stage. One seed or
short runs are mechanism/smoke evidence, not adoption evidence. Review those
eight arms first, then select candidates and matched controls for longer,
multiple-seed validation; do not automatically escalate to 24 five-year runs.
`-ReuseChecksFrom PATH/plan.json` can reuse all four passed native checks only
when the assembly hash and working bundle are identical; the new manifest
records their source.

Compatibility: plain `--hit-act-cadence` retains the radio-only split used by
`mtn5-pair`. The later awareness-sharing probe now requires the separate
`--hit-act-awareness-split` flag. Neither scheduler switch enables it. The
cadence-screen runner explicitly supplies it to reproduce its own awareness
experiment; changing allocation or capacity must not silently change that
mechanism in the working bundle's control.

The runner now reads `SimLogs/working-bundle/plan.json` directly (override with
`-WorkingPlan PATH`), rather than copying the older flag list into the script.
It saves the working-plan snapshot and source hash beside the screen manifest.
When the active bundle includes slate/capacity/studio switches, the factorial
runner explicitly excludes those three from its untreated base, then reapplies
each arm's settings. The manifest names these exclusions and retains the full
active-bundle snapshot; the analyzer validates that derivation. This keeps the
legacy comparison available after adoption without discarding the working base.
The current base includes workload-scaled staffing, deferred British labels,
MidTier national A&R and MidTier going-concern opening cash. The source branch
named in that plan has been merged into this checkout. `mtn5-pair` is the
five-year reference; neither the old slate smoke controls nor `ttx-v1-tax`
substitute for it.

```powershell
./SimTools/RunLabelReleaseProgrammeAudit.ps1 -RunTag slate-short
# Full factorial only when explicitly chosen after the short screen:
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
Annual output also separates ordinary programme singles from LP promo/advance
singles, reports acts with 2+/3+ distinct Top 10 records (including carryovers,
matching the cadence screen), and reports studio Top 10 share and best rank.
Pass `--control-reference PATH/TO/mtn5-pair/runs.json` with `--plan` to compare
every untreated-control CSV with the original baseline by seed. The reference
must match the working flags and duration; missing, added and changed streams
are listed explicitly. Its older assembly hash is retained by its own manifest,
not silently treated as the current assembly.

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

### slate-working-v13 (discarded baseline) / v14 (screen in progress)

The v13 four native checks passed with the MidTier switches: mechanisms and actual
world save/load/resume, each with studio off and on. The analyzer's six tests
also passed, including the baseline replay check's detection of changed,
missing and additional streams. The expanded analyzer validated all eight
existing v11 smoke arms; these remain evidence on the older base only.

The first untreated five-year v13 run differed from `mtn5-pair` in 62 of 96
streams. Investigation found the awareness split had been changed implicitly
in the cadence switch between the reference and this checkout. The run was
stopped; it is not valid comparison evidence. Awareness sharing is now a named
probe, leaving the working bundle's original radio split intact.

The replacement v14 24-run screen uses seeds 1001/2002/3003 for 265 weeks, all eight
allocation × capacity × studio arms, and the saved working-plan snapshot.
All four corrected native programme checks and the cadence regression check
passed. The report job checks each untreated seed as it finishes, stopping the
screen if any simulation stream differs from `mtn5-pair`; wall-clock performance
measurements are listed separately from simulation identity.
`SimLogs/slate-working-v14/status.json` tracks completion. After all native
arms finish, `finish-screen.ps1` writes `screen-review.json` and compares the
untreated controls against the original `mtn5-pair` CSVs. The running job has
not established calibration or studio adoption. Repeat-hit calibration and
Major absorption/consolidation remain open.

The user subsequently chose staged screening. The v14 five-year batch was
stopped after all three untreated controls completed: each reproduced all 95
simulation streams in `mtn5-pair` exactly (only runtime measurements differed).
The interrupted allocation run is incomplete and excluded from evidence.
The replacement `slate-short-v15` runs all eight configurations for 53 weeks
with seed 1001, reusing v14's checks on the same assembly and bundle. Its report
job performs analysis only; it does not start longer runs. Status and results
live in `SimLogs/slate-short-v15/`.

### Active implementation decision after slate-short-v15

All eight one-year runs and their resource/readiness analysis completed. The
user chose to continue with the complete scheduler and studio labels enabled,
allowing the economy to change before reassessing statistics. Accordingly,
`SimLogs/working-bundle/plan.json` now adds `--label-release-slate`,
`--label-release-capacity` and `--studio-parent-labels` to the prior 27 flags.
Its source records the user decision, the completed short-screen evidence and
the archived prior bundle. The existing awareness-split experiment remains a
separate named mechanism. No additional long batch is started by this change.

The active 30 flags exactly match the completed `slate-short-v15-on-both`
invocation. Its same-assembly native checks cover both studio settings,
prepared masters, reservations and save/load/resume. Capacity parameters and
finance boundaries remain as documented above. Further implementation and
economic calibration should use this complete working bundle; repeat-hit
calibration and Major absorption/consolidation remain open work.

### slate-delivery-v16 (delayed LP lifecycle)

Completed the delivery path behind an advance single: explicit awaiting state,
actual LP issue dates, cancellation/transfer reconciliation, and promotion
booking on the LP's delivery week. The analyzer now checks actual issue weeks
against the release census, rejects issue dates or duplicate reservations on
awaiting entries, and separates cancelled advance-led LPs from pending ones.
It continues to read earlier manifests with their explicitly identified
advance-single timing convention.

Validation: build passed with no warnings/errors; eight analyzer tests passed;
24 native mechanism assertions passed in each studio setting; actual world
save/load/resume passed at 13 weeks in both settings and at 19 weeks with the
complete working bundle. One fully enabled 53-week run passed accounting,
promotion conservation and calendar/census checks: 21 advance singles, 19 LPs
delivered and two pending at run end. One switches-off 53-week replay matched
all 95 simulation CSVs from the prior short control; only runtime measurements
differed. Logs and reports: `SimLogs/slate-delivery-v16/`. This was focused
implementation validation, with no new factorial or five-year batch.

### slate-reservations-v17 (rescheduling and future commitments)

Unified rush/postponement resource handling and restored pressing holds when
a prepared master is readmitted. The native checks now exercise overlapping
future campaigns, exact end/start boundaries, preservation of paid masters,
own-hold exclusion during requotes, and insufficient pressing cash.

Validation: build passed without warnings/errors; nine analyzer tests passed;
33 mechanism assertions passed in each studio setting; actual world
save/load/resume passed at 13 weeks in both settings. A single fully enabled
53-week run passed the new future-promotion audit across all 30,527 observed
label-weeks, alongside existing accounting and delivery checks. The separate
53-week legacy run again matched all 95 simulation streams byte-for-byte;
only runtime measurements differed. Evidence lives in
`SimLogs/slate-reservations-v17/`. No long calibration batch was started.

### slate-preparation-v18 (actual session dates and issue lead)

Closed a preparation timing gap: the ordinary due-work loop could record and
issue a master in the same week when preparation ran late. The v17 enabled
run included 63 such overdue issues, besides work recorded on its due date.
The central issue path now enforces the saved actual session week plus one,
with the existing original opening-week exception. An LP's advance single
also passes this gate. A delayed opening intention cannot reuse the exception.
Waiting work retains its paid master and rebooks unspent holds normally.

Validation: build passed without warnings/errors; ten analyzer tests passed;
39 native mechanism assertions passed in each studio setting, including old-save
fallback, early preparation, delayed opening work and session-date persistence.
Actual world save/load/resume passed at 13 weeks with studio off and on. One
fully enabled 53-week run passed accounting, delivery, promotion and calendar
checks over 30,498 label-weeks; all 4,097 issue/advance-launch transitions carried
actual recording dates, and 449 reviews postponed work for the issue lead.
The separate 53-week legacy replay again matched all 95 simulation streams;
only runtime measurements differed. Evidence: `SimLogs/slate-preparation-v18/`.
This is focused implementation validation, not a new calibration screen.

### slate-idle-v19 (idle fallback and actual-week resource checks)

Closed two gaps in the idle-work pass. Work postponed by the ordinary pass
cannot retry later in the same week; unused slots remain available to other
feasible intentions. Failed early preparation and issue now release unspent
holds while keeping the future due date and any paid master. Each slate issue
requotes its actual launch window and live funding, so a future reservation
cannot authorize an early launch against today's congested promotion pool.
Failed funding reviews remain included in release-attempt telemetry.

Validation: build passed without warnings/errors; eleven analyzer tests passed;
46 native assertions passed in each studio setting. The native cases include
future promotion fitting while the current pool is full, rejected early
readmission, preservation of the future date and paid master, and next-week
eligibility after a failed review. Both 13-week world save/load/resume checks
passed. One fully enabled 53-week run passed accounting, delivery, promotion
and calendar checks over 30,528 label-weeks. Its 42,462 review rows contain
13,222 deferred item-weeks with no same-week retry or retained unspent holds;
358 failed early issues exercised that cleanup path. No early-preparation
failure occurred in this run. The prior v18 enabled stream remains readable
under its earlier review convention. The separate 53-week legacy replay
matched all 95 simulation CSVs, with only runtime measurements differing.
Evidence: `SimLogs/slate-idle-v19/`. No long calibration batch was started.

### slate-ownership-v20 (subsidiary continuity and protected programme cash)

Verified the existing absorption path with the complete scheduler. Absorbed
subsidiaries retain their operating imprint, capacity tier, contracts, dated
slate, prepared masters and paid costs, pending LPs, format clocks, campaigns
and planning identities. No scheduler transfer or duplicate resource book was
needed. A finance gap was fixed: `LabelCorporateModel.ProtectedCash` now adds
unspent programme holds to its ordinary liquidity buffer. Shareholder payouts
and discretionary capital spending therefore cannot consume that reserved
cash, including a CorporateDivision's larger distribution to its new parent.
The addition is gated off with the programme; no save-schema change is needed.

Validation: build passed without warnings/errors; eleven analyzer tests passed;
46 existing programme assertions and 16 new ownership/finance assertions passed
in each studio setting. The new assertions cover cash conservation, one-time
hold protection, retained identity and capacity, payout replay, release of holds
without refunds, and equipment-purchase funding boundaries.

Two live scenarios force the real absorption method after finding an otherwise
funded independent with both a prepared slate master and a pending advance-led
LP. They verify unchanged programme/project state, a byte-identical full-world
capture/apply/capture, gzip file IO, and resumed delivery under the subsidiary's
imprint with separately conserved parent and subsidiary promotion books:

* Studio off: `label_0098` acquired in week 25; LP delivered in week 29.
* Studio on: `label_0108` acquired in week 20; LP delivered in week 24.

One ordinary fully enabled 53-week run passed the existing accounting,
delivery, review and promotion audits over 30,514 label-weeks and 42,257 slate
review rows. The separate 53-week legacy replay matched all 95 simulation CSVs;
only runtime measurements differed. The existing corporate regression passed
all 54 assertions with the scheduler enabled under its original tax/founder-pay
settings. Its first invocation on the full working bundle stopped at its
current-year quarterly-tax expectation because that bundle adopts prior-year
tax timing; the incompatible invocation is preserved in the logs.

Evidence: `SimLogs/slate-ownership-v20/`. These forced integration scenarios
establish continuity, not acquisition frequency or historical consolidation
calibration. Distribution-deal advance and buyout funding remain a separate
finance follow-up before reassessing consolidation. No long batch was started.

### slate-funding-v21 (distribution advances, buyouts and stale offers)

Completed the distribution-financing follow-up. New advance quotes protect the
programme's existing cash commitments. A courting Major budgets the incumbent's
unrecouped claim before quoting additional cash, and the replacement path checks
the entire payment before dropping a weaker client, retiring the existing claim
or moving cash. A refused replacement leaves both contracts and cash unchanged.
Funded buyouts carry the purchased claim once on the new deal, separately from
the new advance. Voluntary exits retain the same protected reserve before
settling, with the existing writeoff rule for the remainder. These boundaries
are enabled with the programme; the legacy comparison policy is preserved.

The player acceptance path now handles a distributor whose funding changed
since its offer: acceptance returns failure, retains the pending offer and
creates neither a contract nor cash. Successful acceptance clears the offer
after commitment. The existing shipping-flow integration fixture now uses a
real distributor when testing the stricter programme path.

Validation: build passed without warnings/errors; eleven analyzer tests passed;
21 new funding assertions, 46 scheduler assertions and 16 ownership assertions
passed in each studio setting. Funding checks include exact cash boundaries,
advance quotation net of buyout, failed-replacement preservation of a third
client's contract, financing-only accounts, cash conservation, replay rejection,
exit settlement, legacy-floor compatibility and actual player-desk acceptance.
Two live 13-week worlds staged an $800 incumbent advance followed by a $500
new advance and $800 buyout. Both retained their prepared masters and restored
the resulting $1,300 claim through byte-identical full-world and gzip file
roundtrips, then resumed recoupment successfully.

The existing B/C regression passed all 49 assertions in each studio setting
under its original non-scheduler configuration. A full-bundle invocation
stopped at that harness's exact-v27 assertion, which is incompatible with a
programme world requiring v28; its log is retained. Earlier fixture attempts
are also retained: the quotation fixture needed a nonzero operating bill under
home-founder compensation, and cash setup needed financing journal entries
before checking month-end reconciliation. These were test-setup corrections.

One fully enabled 53-week run passed accounting, delivery, promotion and review
checks over 30,476 label-weeks and 41,853 review rows. The separate 53-week legacy
replay matched all 95 simulation CSVs; only runtime measurements differed.
Completed evidence: `SimLogs/slate-funding-v21-checked/`. This closes the funding
implementation pass; the forced buyouts do not establish natural late-decade
poaching frequency, consolidation calibration or historical acceptance. No long
batch was started.

### slate-ownership-guard-v22 (corporate paths and stale client eligibility)

The distribution cycle check now follows both active distribution contracts and
corporate ownership while the programme is enabled. A label cannot sign with or
be absorbed by its own subsidiary, including nested and mixed ownership/deal
paths. Offer selection and the final signing, replacement and absorption checks
share this live graph. Converging paths can revisit a firm without inventing a
cycle back to an unrelated client; traversal terminates on existing loops.

Signing and replacement also recheck whether the client is still independent,
eligible by tier and outside the studio-parent model. An offer made before an
absorption or promotion cannot bypass those boundaries when accepted later.
Rejection occurs before cash, claims, contracts, client drops or prepared work
change. Player offers remain pending on failure. No new save fields, random
draws, capacity settings or consolidation probabilities are introduced. The
switches-off signing and cycle policy remains available for legacy comparisons.

Validation: build passed with zero warnings/errors; eleven analyzer tests passed.
In both studio settings, twenty new ownership/dependency assertions, twenty-three
funding assertions, forty-six scheduler assertions and sixteen existing ownership
assertions passed. Both live 13-week funding worlds retained their prepared
masters and $1,300 replacement claims across byte-identical full-world and gzip
roundtrips, then resumed recoupment. The new cases include rejected direct and
nested absorptions, unchanged replacement claims and dropped clients, valid
unrelated subsidiary distributors, and stale player-offer retention after an
ownership change. The first sandboxed Godot startup crashed before the checks;
the completed native checks ran outside the sandbox, with that log retained.

The fully enabled 53-week seed-1001 run passed accounting, delivery, promotion
and schedule review checks over 30,476 label-weeks and 41,853 review rows. All
98 simulation streams match the preceding v21 enabled run byte for byte. The
separate 53-week legacy replay matches all 95 preceding simulation streams;
only wall-clock performance profiles differ in either comparison. These normal
worlds therefore did not exercise a newly rejected ownership loop; the native
fixtures establish that behaviour. Completed evidence, exact arguments, assembly
hash, save/load results and stream hashes: `SimLogs/slate-ownership-guard-v22/`.
No long calibration batch was started.

### slate-material-v23 (anticipatory material search)

`--anticipatory-material-search` (requires the slate) lets an open single
intention arrange one specific song ahead of its session: a professional song
($150 supply fee, 7-day delivery, one job per supplier) or a cover rehearsal
(no fee, 3–14 days by act ability, existing rights route). Each intention reads a
fixed, keyed sample of four professional and four standard songs once; the best
positive net offer starts if it can arrive before the planned session and the
fee fits above the commitment reserve. One pending job per act and two per label.
Delivery is heard against the saved baseline and may disappoint; failure,
expiry, cancellation and contract changes are terminal, with no refund or
redraw. The session builds the saved song; save v29 carries the work.
Code: `Systems/CompetitorManager.MaterialSearch.cs`; telemetry:
`*-programme-material-search.csv`.

Validation: build passed with no warnings/errors. In each studio setting, 30
material assertions and the 46 existing scheduler assertions passed, and a live
13-week world roundtrip resumed pending cover work through byte-identical
full-world and gzip saves and consumed the saved song in its session.

53-week seed-1001 smoke on the working bundle, switch off vs on. The switch-off
run matched all 98 simulation streams of `slate-ownership-guard-v22-on`; only
the runtime profile differed.

* **Uptake is high.** 66% of the 3,956 reviewed single intentions started work
  (1,297 professional, 1,308 cover); 34% found no worthwhile offer. Of started
  work, 1,724 reached the session, 402 disappointed on hearing, 180 expired
  and 3 were cancelled; 296 were still pending or ready at run end. Fees
  totalled $195k. Uptake is similar across tiers (Majors 73%, Small 62%).
* **The fee hardly binds.** Median offer read exceeds the baseline by .23 (p10
  .09); median expected benefit is $449 against the $150 fee, and covers are
  free. Only ~17% of heard deliveries disappoint, so the gap sits in the
  expectation itself, not in best-of-eight noise. The baseline was the act's
  raw talent (`qualityEstimate`), not on `ExpectedRecording`'s scale; v24
  below corrects it.
* Singles cut from searched material charted more (Hot 100 16.4% vs 13.4%,
  Top 10 2.9% vs 1.5% of other slate singles), but selection is not random.
* Market totals barely move: singles −1.2%, LPs +1.4%, single units +0.4%,
  charting records 600 vs 596, Hot 100 acts 410 vs 383. Big-5 Top 10 share
  −2.3 points, Major conversion 6.3% vs 7.9%, studio Top 10 share 11% vs 4%:
  all within the single-seed reshuffle range seen earlier.

Evidence: `SimLogs/slate-material-v23/` (checks, reviews, `uptake.txt`). No
default or working-bundle change; no long batch was started.

### slate-material-v24 (same-scale no-search baseline)

v23 compared offer reads (`ExpectedRecording`: shipped hook and production,
including the label's production quality and studio terms) with
`CalculateBaseQuality`, the act's raw talent. The baseline is now the existing
session's expected cut on the offer's scale: the act's own original at mean
craft (`AiRepertoireService.MeanOwnOriginal`) and the same sampled songs, each
valued by `ExpectedRecording` and weighted as `ChooseMaterial` weights sources
(source mix × polar selection score). Recent-hit and traditional sources are not
sampled, so they are absent from the baseline. No draws or new state.

Validation: build clean; 31 material assertions (one new: the baseline lies
between the act's own-song and the offer's expectation), 46 scheduler
assertions and the live roundtrip passed in each studio setting. The
switch-off 53-week run again matched every simulation stream of v22-on.

The scale error was real but small on average. The baseline mean moved from
.559 to .575, and its spread narrowed from .119 to .084. The old baseline
followed raw talent instead of the label's studio, so it was too low at
well-equipped labels and too high at weak ones. Uptake did not fall: 67.6% of
3,907 reviews started work (1,256 professional, 1,384 cover), 1,680 reached
the session and 487 disappointed (~22% of heard, from ~17%). The median gap is
.18 (from .23). The remaining gap is genuine search value: choosing the best of
eight read songs against a weighted draw from the same pools. The $150 fee and
free covers remain far below the median expected benefit ($362). Uptake is now
a design question (search breadth, cost of a cover rehearsal), not a scale bug.

Market totals stay within single-seed noise: singles 3,021 vs 3,023, single
units −0.01%, Big-5 Top 10 share 39.4% vs 37.9%, Major conversion 7.5% vs 7.9%,
single loss share .68 vs .70. Studio Top 10 share is again higher (11.5% vs
4.0%), the one effect repeated across v23 and v24; it is one seed.

Evidence: `SimLogs/slate-material-v24/`. No default or working-bundle change.

### slate-lp-assembly-v25 (Release Judgement 5A: LP assembly)

`--lp-assembly` (requires the slate) replaces the compilation lottery on the
slate's LP path. Before, `GetCompilationChance` (up to .97 × single
orientation) drew a "compilation" whose reused masters were simply the act's
last four singles, at the 0.6-session compilation cost even with nothing to
reuse. At preparation, an LP intention now prices three propositions on their
actual track lists with the existing album margin model (`BuildLpAssembly`,
`Systems/CompetitorManager.LpAssembly.cs`) and takes the best feasible one:

* **Ordinary studio LP:** new sessions, plus up to three of the act's
  strongest shelved masters.
* **Hit-led studio LP:** up to three recent (≤52 weeks) Top 40 singles the
  label controls and not already on a studio LP, up to two other recent
  singles, then shelved masters. Reuse is capped at half the tracks.
* **Greatest-hits package:** needs `--lp-assembly-gh-hits` (default 3)
  relevant hits, meaning controlled Top 40 singles not already on a hits
  package. Hits lead, then the rest of the controlled catalogue, then shelved
  masters ("previously unissued"). No new sessions; at least 8 tracks.

Statement and live LPs remain ordinary-LP outcomes of the existing draws.

Supporting state (save v30, all null when unused):

* A **master-control ledger** filled in `TrackRelease`, so it follows master
  sales and leases. A single from a former label is not this label's
  catalogue.
* A **shelf** of paid slate singles whose window expired while the act stayed
  signed: six per act, cleared when the act leaves, each master used once.
* Each item's chosen `LpAssemblyPlan`.

Pricing:

* New sessions cost 2.4 sessions each and reused masters 0.6, pro rata.
* Only included singles enter the hit inventory, at full weight.
* A hits package registers comp uses, which lower later freshness. A studio LP
  marks its singles as used for later studio LPs.

The track length (9–12) is keyed to the intention. The planning and slot
choice still use the legacy prior, so the format opportunity policy is
unchanged. Telemetry: `*-lp-assembly.csv`.

Validation: build clean. In each studio setting, 20 LP-assembly assertions,
the 46 scheduler and 31 material assertions (both with assembly on), and a
26-week live roundtrip passed. The roundtrip checked every assembled master
against its plan as it was made: references, track counts, compilation only
for packages, and shelved masters consumed. It then checked byte-identical
full-world and gzip saves, unchanged ledger after load, and 13 resumed weeks
under the live-state audit (studio off: 705 ordinary and 100 hit-led LPs,
306 reused masters; studio on: 703/88/258).

53-week seed-1001 smoke, working bundle with and without the switch. The
switch-off run matched all 98 simulation streams of v24's base.

| 1960 | LPs pressed | Compilation | LP production paid | LP units | LP unit share | Singles | Single units |
| --- | --- | --- | --- | --- | --- | --- | --- |
| control | 1,078 | 61.5% | $2.98M | 29.2M | 16.9% | 3,023 | 144.0M |
| assembly | 1,061 | 0.1% | $4.64M | 32.4M | 18.6% | 3,029 | 141.6M |

* Of 1,197 assembled LPs: 1,077 ordinary, 115 hit-led, 5 greatest hits.
  938 ordinary LPs belong to new signings with no controlled catalogue. Hit-led
  LPs reuse 2.5 released masters on average (multiplier 1.95). All 5 packages
  are Major Star/Established acts. 11 LPs had 3+ relevant hits; the other 6
  chose differently or were too short (the ledger starts empty in 1960, so
  opening-career discographies are not reusable masters).
* Retiring the lottery is the main economic change, not the new kinds. The
  control's 663 "compilations" were mostly filler LPs at the 0.6-session cost
  and 0.68 track-quality scale. They are now studio LPs at full cost and the
  0.88 scale, so LP production rises 56% and LP units 11%. LP unit share moves
  toward the ~30% 1960 reference, and no starting Small label closed (control:
  1). Shelved masters were rarely used (0.09 per ordinary LP), because most LPs
  belong to new acts.
* Hot 100 acts 383 vs 383; Big-5 Top 10 share 36.2% vs 37.9%; Major conversion
  6.9% vs 7.9%; single loss share .70 vs .70. All within single-seed noise.

Open: the three-hit gate against 2 and 4 (`--lp-assembly-gh-hits`), multi-seed
and five-year runs (LP losses are unfinalized at 53 weeks), and an opening
discography for proven 1960 acts. Evidence: `SimLogs/slate-lp-assembly-v25/`.
No default or working-bundle change.

### slate-lp-opening-v26 (opening catalogue, appeal pricing and package spacing)

Three additions, still under `--lp-assembly`:

* **Opening catalogue.** Once per world, each opening act's pre-1960 singles
  (the `OpeningCareerService` history, drawn on its current label) become
  masters that label controls (`SeedOpeningCatalogue`). The history has no
  recording, so quality is provisional: the act's ordinary LP-track level plus
  up to +.10 for the chart peak. Release months are keyed. They are kept apart
  from `releasedSingleIds`, so legacy album and hit-inventory code never sees
  them.
* **Appeal-aware pricing.** Each proposition's affinity units are scaled by
  its expected pooled appeal against an all-new LP. Reused tracks are
  freshness-weighted, exactly as `GenerateAlbum` sells them. A package
  otherwise won on its 0.6-session cost while selling on stale hits.
* **Package spacing.** At least `--lp-assembly-package-gap` (default 104)
  weeks between an act's packages: the directive's sample is about one per two
  years for stars. Opening acts with a pre-1960 LP and three or more hits get a
  last-package week keyed uniformly within the gap before the start, so their
  eligibility spreads out instead of arriving in week one.

Validation: build clean; 26 LP-assembly assertions plus the 46 scheduler and 31
material assertions with assembly on, in each studio setting. The live
roundtrip ran 7 and 5 packages through plan, save/load and resume checks. The
switch-off run again matched all 98 simulation streams.

Seed 1001, 53 weeks:

* 11 packages (v25: 5), spread over months 3–13 with no opening wave; 6 from
  opening acts. Ten are at Majors. Spacing blocked 40 package evaluations.
* Opening acts issued 75 LPs: 37 hit-led (late-1959 hits now lead early-1960
  LPs), 6 packages, 11 ordinary LPs held by spacing and 21 other ordinary.
  That is about 1.9 LPs per act-year, between the sample's 2.45 (stars) and
  1.67 (mid acts).
* Packages price at 0.97 of all-new appeal, hit-led LPs at 1.10.
* Against v25: LP units 31.2M vs 32.4M (control 29.2M), and Hot 100 acts 371
  vs 383. Big-5 Top 10 share, Major conversion and single units all stay
  within single-seed noise.

Opening-act coverage: on a like-for-like proxy, acts already on the first 1960
chart number 87 (26%) historically and 93 (24%) in the sim. But they hold 44.7%
of 1960 Hot 100 slot-weeks historically against 30.5% in the sim (Top 10:
43.4% vs 35.4%). The proxy misses veterans absent from the first chart, so the
historical share is a floor. Headcount is close; veterans' continued chart
share is the miss, and more seeded histories would not supply it by
themselves. Evidence: `SimLogs/slate-lp-opening-v26/`.

### Repeat hits on the working bundle (`rhb-v1`, 2026-10-23)

The 30-flag working bundle, seeds 1001/2002/3003, 265 weeks, against the
pre-slate bundle (`slate-working-v14-off-control`, same seeds). The two differ
in the slate, capacity and studio switches together, so this is not a
single-switch attribution. `analyze_hit_act_cadence.py --runs` now accepts
studio-on runs (`SimLogs/rhb-v1/compare/`).

Acts per year with 2+ / 3+ Top 10 records, 1960-64 mean, by label group at the
act's first Top 10 that year:

| | History | Pre-slate | Slate |
|---|---:|---:|---:|
| All | 17.8 / 7.2 | 10.7 / 2.0 | 11.3 / 4.0 |
| Big-5 | 5.6 / 3.6 | 5.5 / 1.5 | 7.8 / 3.3 |
| Studio labels | 3.4 / 1.4 | 0 / 0 | 1.1 / 0.1 |
| Field | 8.8 / 2.2 | 5.2 / 0.5 | 2.3 / 0.3 |

Three-hit acts +2.0 (paired SE .8); two-hit acts +0.6 (SE 1.0); field two-hit
acts -2.9 (SE .7), down in every seed. The Big-5 now meet or exceed history;
almost the whole remaining gap is field and studio acts. Side effects: 1961
Big-5 Top 10 share 28 -> 43% (history 22%), Independent closures 18 -> 31%,
single launches -23%.

**Follow-up funnel.** Each act's first non-opening Top 10, followed for 52 weeks
(pooled seeds):

| | Pre-slate Big-5 | Pre-slate field | Slate Big-5 | Slate field |
|---|---:|---:|---:|---:|
| Hit acts per seed | 49 | 186 | 49 | 147 |
| Singles in the year | 3.3 | 2.3 | **6.4** | **2.1** |
| Median weeks to next single | 10 | 13 | 4 | 16 |
| Silent all year | 3% | 15% | 3% | 18% |
| Left the roster | 6% | 20% | 2% | 22% |
| Another Top 10 | 39% | 14% | 43% | **10%** |

No field hit act moved to a Big-5 label within the year; pickups are not the
route. Departures are about one in five field hit acts in both arms (expired
contracts, label closures, performance drops). They are not the change.

Three mechanisms, in order of size:

1. **Programme volume goes to the Big-5.** Hit follow-up slots are similar
   (1.98 per Big-5 hit act, 1.83 field). But the roster programme adds 4.27 more
   for a Big-5 hit act against 0.49 at a field label. In 1961 an Independent
   plans 0.16 singles a week (Major 2.6) with $322 of programme cash reserved.
   A hit does not raise that throughput.
2. **Field slots fail on funding and expiry.** 20-34% of field slots end
   cancelled (Big-5 3-6%). Per field hit act, 0.57 slot windows expire and 0.51
   slots meet insufficient unreserved cash. The Big-5 see 0.14 and 0.07. A label
   holding a hit is cash-short exactly when it should follow up, which is
   consistent with hit income arriving as receivables (WorkingCapitalResearch.md).
3. **Field follow-ups chart less at equal quality.** Mean runtime quality is
   the same (.63-.65 in every group). In the .7-.8 band a slate Big-5 follow-up
   reaches the Top 10 27% of the time, a field one 14% (pre-slate 35% / 18%).
   Launch awareness is the gap: .56 Big-5 against .35 field, and both fell
   under the slate (.63 and .43 before). With 939 Big-5 follow-ups over three
   worlds against 478 before, the Big-5's extra volume also competes for the
   same slots. The field also produces fewer first hits (147 vs 186 acts per seed).

The Big-5 cadence (6.4 singles a year, one every four weeks) is likely above
period practice for hit acts, which ran nearer three to five a year. Raising
field cadence alone would not close the gap while mechanism 3 stands. At 2.1
singles and a 10% Top 10 rate, field repeats are bounded near what is observed.
Candidate fixes: hit-scaled programme throughput and funding for the act that
charted (financed against the hit's receivables), a cap on per-act cadence near
period practice, and a review of launch awareness for follow-ups to a fresh
hit. None is implemented; no default or bundle change.

### Hit follow-up finance (`hff-v2`, 2026-10-23)

`--hit-follow-up-finance` (requires the slate and capacity;
`Systems/CompetitorManager.HitFollowUpFinance.cs`) applies to non-Major AI
labels and acts with a Top 40 within 52 weeks:

* **Throughput.** Each such act adds `--hit-follow-up-finance-singles`
  (default 2) single slots a year to dated planning, preparation and issue
  work. Planned extra slots are open only to hot acts and use their own keyed
  roll domain, so base slots are unchanged.
* **Funding.** A hot act's slot that is short of unreserved cash factors
  wholesale receivables, latest due first, at the player's factoring terms
  (70-85 cents by house reliability). The discount is a write-off. Nothing is
  sold unless the sale covers the shortfall. Receivables are billed by house and
  week, not record: the hit gates the sale but is not traced to its invoices.
  No saved state. Telemetry: `HIT_FOLLOW_UP_FINANCE` console lines.

Validation: build clean; 17 native checks (`--hit-follow-up-finance-check`),
the 46 programme checks and a 13-week save/load round trip with the switch on
(`SimLogs/hff-v1/checks/`). The switch-off 265-week replay (`hff-v2-off`, seed
1001) is byte-identical to `rhb-v1` in all 98 simulation streams; only the
runtime profile differs.

Screen: working bundle plus the switch, seeds 1001/2002/3003, 265 weeks, paired
against `rhb-v1` (`SimLogs/hff-v2/compare/`). Acts per year with 2+ / 3+ Top 10
records, 1960-64 mean:

| | History | Slate | + finance | Paired difference (2+) |
|---|---:|---:|---:|---:|
| All | 17.8 / 7.2 | 11.3 / 4.0 | 10.3 / 2.7 | -1.0 ± 0.9 |
| Big-5 | 5.6 / 3.6 | 7.8 / 3.3 | 5.4 / 2.1 | -2.4 ± 0.8 |
| Field | 8.8 / 2.2 | 2.3 / 0.3 | 3.9 / 0.4 | **+1.6 ± 0.3** |

Field hit acts' 52-week follow-up: 3.0 singles (from 2.1), median 9 weeks to the
next (from 16), 11% silent (from 18%), and 17% reach the Top 10 again (from 10%).
Field two-hit acts rise in every seed. Big-5 two-hit acts fall in every seed, to
about history's level. The total does not rise: three-hit acts fall 1.3 ± 1.0.

Other effects: single launches +5.1% (SE .8 points); Major single-unit share
-4.2 points (SE 1.2); 1961 Big-5 Top 10 share -3.3 (SE 2.8); hit-act 52-week
departures +3.6 points (SE 1.0); Independent closures +3.4 (SE 2.8), Small and
Boutique -2.4 / -2.3, all within noise. Factoring: 488-550 sales per world, $12-14M
of invoices for $9.5-10.9M, a $2.3-2.7M discount per world, at 128-152 labels.
The median sale ($8-10k) exceeds a single's cost. The cash test includes the
commitment reserve, so most sales restore a depleted reserve rather than fund
the release; closures did not fall.

**Why the total cannot rise here.** History has 112 distinct Top 10 records a
year from 83 acts (66 with one). The model has 89 from 72-75 acts (60-65 with one).
One-hit acts are close to history; the miss is about 23 Top 10 records a year.
With 520 slot-weeks, a model record holds the Top 10 for 5.8 weeks against
history's 4.6. Repeats are capped by Top 10 turnover, so a follow-up fix can only
move them between groups: this switch moves them from the Big-5 to the field.

Not adopted. The throughput half does what it was built for. The funding half
should be limited to the release's own costs, not the reserve. Next: Top 10
turnover (record tenure), which bounds total repeats.

### Top 10 turnover on the working bundle (2026-10-23)

Total repeats are capped by distinct Top 10 records (previous section). Records
whose first Top 10 week falls in Feb 1960 - Sep 1964, history (recovered
Hot 100) against `rhb-v1` (three seeds), per year:

| Peak | History records | Model records | History Top 10 weeks | Model | History weeks after peak | Model |
|---|---:|---:|---:|---:|---:|---:|
| #1 | 20.0 | 22.6 | 8.4 | 8.9 | 4.9 | 6.1 |
| #2-3 | 21.9 | 26.1 | 6.5 | 6.9 | 3.1 | 4.3 |
| #4-6 | 28.8 | 21.9 | 4.5 | 5.1 | 1.7 | 2.8 |
| #7-10 | **34.8** | **11.0** | 2.4 | 2.8 | 0.8 | 1.1 |
| All | 105.5 | 81.6 | 5.0 | 6.4 | 2.3 | 3.9 |

The missing records are brief visitors: records that touch #7-10 and leave make
up the whole count gap. One- and two-week Top 10 stays are 20% of history's
records against 8% of the model's. Two mechanisms produce this.

1. **Entering the Top 10 carries a record higher.** A quarter of history's
   #9-10 entrants peak there; 9% of the model's do. The model's #7-8 entrants
   peak at #3.3 on average, history's at #4.8. Median units rise 50% the week
   after entry (68k to 103k) and points 42%, and the record climbs two more weeks
   (#8 to #4). Records arrive on a steep ramp, with units roughly doubling each
   week before entry. The rank exposure weight plateaus across the Top 10
   (`CHART_EXPOSURE_PLATEAU_RANK`), so #10 gets #1's exposure. The radio position
   bonus (+.25 heat in the Top 10) adds to the same feedback.
2. **Records fall in small steps.** Model records in the Top 10 fall more often
   than history's (60% vs 49% of weeks) but by about half as much: mean weekly
   move -0.8 places at #4-10 and -1.6 at #11-20, against -1.5 and -2.4. At
   #41-100, history's records move +4-7 places a week, mostly fresh records
   climbing. The model's lower chart is mostly old records sliding down slowly
   (+0.3 to +0.5). Top 10 records' Hot 100 life is 19.6 weeks against 14.1.
   Weeks after the peak run long in every band (the 2026-10-17 note that stays
   per band matched history does not hold on this bundle).

**Inertia probe** (one year, seed 1001; control `slate-lp-opening-v26-base`,
whose records reproduce `rhb-v1`'s first year exactly):

| 1960 | History | Inertia .8 (control) | .4 | 0 |
|---|---:|---:|---:|---:|
| Top 10 records | 103 | 93 | 99 | 101 |
| Top 10 weeks / record | 5.05 | 5.59 | 5.25 | 5.15 |
| Peaks #7-10 / #1-3 | 37 / 40 | 14 / 55 | 14 / 55 | 18 / 57 |
| #9-10 entrants peaking there | 26% | 13% | 13% | 11% |
| Hot 100 records | 678 | 590 | 586 | 604 |
| Mean move at #4-10 / #41-100 | -1.4 / +4.1 | -0.9 / +0.3 | -0.9 / +0.8 | -1.0 / +1.0 |

Removing inertia recovers most of 1960's count and stay length, but not the
shape. Brief #7-10 visitors stay rare, entrants keep climbing, Top 10 falls stay
small and the lower chart stays slow. Inertia is part of mechanism 2 and none of
mechanism 1. One seed and one year is within the noise noted for the earlier
probe; this locates mechanisms, it does not calibrate.

Candidates, untested: the Top 10 exposure plateau and radio position bonus
(entry feedback), and the post-peak units tail that holds records in the lower
chart. No code or default change.

### Entry-feedback probe (`cef-v1`, `cef-v2`, 2026-10-23)

`Systems/ChartEntryFeedbackProbe.cs`, off unless named:
`--chart-exposure-plateau-rank=R` replaces the exposure plateau rank (10). R above
10 lowers and widens the plateau, so entering the Top 10 buys no exposure beyond
#R; the curve is renormalised to mean 1, which lifts ranks below R slightly.
`--radio-position-bonus-scale=S` scales the Top 10 / Top 40 radio heat bonus.
11 native checks (`--chart-entry-feedback-check`) cover the off path bit-for-bit,
the renormalised plateau and the bonus scale. The switch-off one-year replay
(`cef-v1-off`) matches `slate-lp-opening-v26-base` in all 98 simulation streams.

The plateau is a deliberate Hesbacher fit (see `CHART_EXPOSURE_PLATEAU_RANK`).
Units grow 76-87% a week before Top 10 entry and 50% after, so part of the
"entry jump" is the release ramp, not feedback.

One year, seed 1001, 1960 (`cef-v1-*`, `inertia-rb-*`): plateau 15 matched
history's Top 10 count (103) and weeks per record (5.05); plateau 20 + radio 0
moved the most toward history on #7-10 peakers (26 vs 14; history 37), #9-10
entrants peaking there (27%; history 26%) and repeats (16 / 5; history 19 / 6).

Five years, seeds 1001/2002/3003, paired against `rhb-v1`:

| 1960-64 per year | History | Slate | Plateau 15 | Plateau 20 + radio 0 |
|---|---:|---:|---:|---:|
| Top 10 records | 105.5 | 89.7 | 93.5 (+3.8 ± 0.6) | 93.8 (+4.1 ± 0.8) |
| Records peaking #7-10 | 34.8 | 11.0 | 15.3 | 16.7 |
| Weeks after peak, #1 / #2-3 / #4-6 | 4.9 / 3.1 / 1.7 | 6.1 / 4.3 / 2.8 | 5.9 / 4.2 / 2.8 | 5.8 / 4.3 / 2.8 |
| Acts with 2+ Top 10s | 17.8 | 11.3 | 10.5 (-0.8 ± 0.2) | **12.0 (+0.7 ± 0.3)** |
| of which field | 8.8 | 2.3 | 2.8 (+0.5 ± 0.1) | 3.2 (+0.9 ± 0.1) |
| Acts with 3+ | 7.2 | 4.0 | 3.3 | 4.3 (+0.3 ± 0.8) |
| Big-5 Top 10 share, 1961 | 22% | 42.8% | 44.0% | 41.7% |

Plateau 20 + radio 0 raises two-hit acts in every seed and field two-hit acts in
every seed. Other effects are small: hit-act departures 18 -> 15%, single units
-0.5%, Major unit share and Big-5 share flat, Boutique closures -5.4 (SE 2.7),
Small and Independent flat. Plateau 15 adds the same turnover, but the extra
slots go to one-hit acts and repeats fall in every seed.

Over five years both arms recover about a quarter of the missing #7-10 peakers
and a quarter of the record gap; the one-year 1960 gain overstated it. Weeks
after the peak are unchanged in every band: mechanism 2 (small falls, long tails)
is untouched by either switch, as expected. Not adopted. Plateau 20 + radio 0 is
the candidate entry-feedback setting; the post-peak tail is the remaining
turnover mechanism.

### Post-peak tail probe (`ctp-v1`, `ctp-v2`, 2026-10-23)

`Systems/ChartTailProbe.cs`, off unless named: `--post-peak-demand-decay=D`
multiplies a charted single's regional conversion by D^(weeksSincePeakUnits - G)
once it is more than G weeks past its own sales peak; `--post-peak-demand-grace=G`
sets G (default 3). It is 1 while a record is still setting weekly highs and never
touches a record that has not charted, so slow regional breakouts keep their
clock. Six native checks (`--chart-tail-check`). All arms run on top of plateau 20
+ radio 0. The switch-off one-year replay (`ctp-v1-off`) matches
`cef-v1-p20r0b` in all 98 simulation streams (only the timing profile differs).

**Diagnosis (control `cef-v1-p20r0b`, 1960).** Median position k weeks after a Top
10 record's chart peak:

| Peak band | k = 0 | 2 | 4 | 6 | 8 | Hot 100 weeks after peak |
|---|---:|---:|---:|---:|---:|---:|
| #4-6 history | 5 | 8.5 | 16.5 | 40.5 | off | 7.4 |
| #4-6 model | 5 | 8 | 15 | 22 | 30 | 11.1 |
| #7-10 history | 8 | 13 | 23 | 51 | off | 7.2 |
| #7-10 model | 8 | 12 | 19 | 29 | 37 | 10.6 |

The first four weeks match. History then accelerates: Hesbacher-weighted points
fall ~0.87x a week to week four and ~0.6x after. The model's points fall a steady
~0.8x, so eight weeks out a #4-10 peaker still has 3-5x the #100 cutoff. Radio heat
is still 60% of peak at week eight. The same slow slide fills the lower chart:
51% of #41-100 slot-weeks are held by records past their peak (history 34%).

**One year, seed 1001:**

| 1960 | History | Control | D .85 G 3 | D .75 G 3 | D .80 G 1 |
|---|---:|---:|---:|---:|---:|
| Top 10 records | 103 | 99 | 96 | 101 | **112** |
| Top 10 weeks / record | 5.05 | 5.25 | 5.42 | 5.15 | 4.64 |
| Peaks #7-10 / #1-3 | 37 / 40 | 26 / 56 | 20 / 51 | 26 / 51 | 20 / 55 |
| #1 records | 20 | 21 | 26 | 24 | 25 |
| Acts with 2+ / 3+ Top 10s | 19 / 6 | 16 / 5 | 15 / 5 | 17 / 5 | **19 / 6** |
| Hot 100 records | 678 | 583 | 625 | 642 | 663 |
| Mean move #41-100 | +4.1 | +1.0 | +1.4 | +2.3 | +2.0 |
| Past-peak share of #41-100 | 34% | 51% | 45% | 43% | 42% |
| Big-5 Top 10 share | | 38.5% | 37.1% | 37.9% | 32.3% |

A grace of three works below the Top 10 (Hot 100 records, lower-chart movement)
but leaves Top 10 count and stay almost unchanged, because a Top 10 record has left
the Top 10 by the time the decay bites. Grace one shortens Top 10 stays. With it,
#4-6 peakers follow history's post-peak path almost week for week (5.5 / 11 / 21.5
/ 40.5 / off at k = 0/2/4/6/8). #1-3 peakers now fall too fast (#50 eight weeks out,
history #30; Hot 100 weeks after peak 8.5 vs 10.5), and #7-10 peakers fell rather
than rose. The added Top 10 records are #4-6 peakers, not history's brief #7-10
visitors. The one-year overstatement found for the entry-feedback probe (about
3x) applies here too.

**What else holds back repeat hits (`cef-v2-p20r0`, three seeds, 1960-63 hit acts).**
After an act's first Top 10, records by the act that debut on the Hot 100 within
52 weeks:

| | History | Model | Big-5 | Field | Studio |
|---|---:|---:|---:|---:|---:|
| Hot 100 entries per hit act | 2.43 | 1.07 | 2.57 | 0.56 | 0.97 |
| Top 10 entries per hit act | 0.46 | 0.29 | 0.79 | 0.14 | 0.16 |
| P(Top 10 \| Hot 100 entry) | 0.19 | 0.27 | 0.31 | 0.24 | 0.16 |
| Hit acts with no entry | 22% | 48% | 16% | 61% | 37% |
| Baseline P(Top 10 \| entry), no prior Top 10 | 0.15 | 0.15 | | | |

The gap is chart *entries*, not conversion: once a follow-up charts it reaches
the Top 10 more often than history's. History's follow-ups are ordinary records
(conversion near baseline) that chart anyway. Flip sides explain little of
history's count (2.10 distinct singles per act once same-act entries within three
weeks are merged).

Two causes in the model:

1. **Hits are hook luck.** 23% of single-quality variance is between acts (hook
   21%, production 69%). A hit act's hit averages quality .764 (hook .80); its
   singles before the hit and in the 52 weeks after both average .632 (hook .58),
   against a population mean of .593. 39% of field follow-ups are at or below .60,
   which charts 10% of the time and never reaches the Top 10.
2. **One hit buys little access.** A Top 10 adds ~0.07 public recognition, about
   +0.01 launch awareness. Follow-ups launch mostly as Rising (43%) or Established
   (35%). At quality .65-.70 they chart 32% / 36% of the time, against 19% for an
   unknown act's record and 63% for a Star's. Launch awareness: NewSigning .19,
   Rising .24, Established .39, Star .79.

Candidate, untested: a recency-keyed launch lift (awareness and radio) for acts
with a recent Top 40 / Top 10, fading over about a year, so it does not feed the
cold-return excess. Hook persistence through material access (hot acts get pitched
better songs; hit writers return) would mostly raise conversion, which already
runs high. It needs a full-telemetry run first: `song-material.csv` is empty under
`--calibration`.

**Five years, seeds 1001/2002/3003 (`ctp-v2-d80g1`: D .80, G 1), paired against
`cef-v2-p20r0`.** Top 10 records by first Top 10 week, Feb 1960 - Sep 1964:

| Per year | History | Plateau 20 + radio 0 | + tail |
|---|---:|---:|---:|
| Top 10 records | 105.5 | 86.7 | **102.8** |
| Peaking #1 / #2-3 / #4-6 / #7-10 | 20.0 / 21.9 / 28.8 / 34.8 | 21.7 / 27.5 / 20.8 / 16.7 | 24.4 / 32.2 / 28.1 / 18.2 |
| Top 10 weeks / record | 4.95 | 6.05 | 5.09 |
| Weeks after peak, #1 / #2-3 / #4-6 / #7-10 | 4.9 / 3.1 / 1.7 / 0.8 | 5.8 / 4.3 / 2.8 / 0.9 | 4.1 / 3.1 / 2.1 / 1.0 |
| Weeks at #1 per #1 record | 2.62 | 2.38 | 2.12 |
| Hot 100 weeks of Top 10 records | 14.1 | 19.0 | 14.5 |
| Top 10 stays of 1-2 / 9+ weeks | 20% / 10% | 11% / 19% | 9% / 2.5% |

Annual counts including carryovers, 1960-64 mean (paired delta ± SE, seeds up):

| | History | Plateau 20 + radio 0 | + tail |
|---|---:|---:|---:|
| Acts with 2+ Top 10s | 17.8 | 12.0 | 15.4 (+3.4 ± 1.1, 3/3) |
| Acts with 3+ | 7.2 | 4.3 | 5.5 (+1.2 ± 0.5, 3/3) |
| 2+ acts at Big-5 / studio / field | 5.6 / 3.4 / 8.8 | 7.9 / 0.9 / 3.2 | 10.0 / 1.5 / 3.9 |
| Hit acts per year (first Top 10) | 61 | 58 | 64 |
| Hot 100 follow-ups per hit act, 52 wk | 2.43 | 1.07 | 1.21 |
| Hit-act departure within 52 weeks | | 14.8% | 17.7% (+2.9 ± 0.8) |
| Big-5 Top 10 share, 1961 | 22% | 41.7% | 40.2% |
| #1 weekly units | ~150k | 116k | 153k |

Single units -0.3% (-2.6M ± 1.6M), Major unit share flat, closures within noise
(Boutique +1.5 ± 0.8). Debuts by acts without a prior Top 10 rise 369 -> 456 a
year (history 405).

The tail closes about 85% of the Top 10 record gap over five years, unlike the
entry-feedback switches (a quarter). The one-year result did not overstate it
(1960: +13 on seed 1001 alone, +14 as the five-year seed mean). Weeks after the peak are on history at #2-3 and Hot 100 life
is on history. Remaining misses:

- The added records peak too high. #1 and #2-3 peakers now exceed history (+4 and
  +10 a year), and brief #7-10 visitors are still half of history's. Stay lengths
  are compressed into four to seven weeks: too few one- and two-week stays and
  too few nine-plus-week runs.
- #1 records run short after the peak (4.1 vs 4.9 weeks) and hold #1 for 2.1 weeks
  against 2.6. A peak-size-blind decay hits the biggest records as hard as the
  smallest; history's #1-3 peakers decayed more slowly than its #4-10 peakers.
- Most repeat gains go to the Big-5, already above history. Field 2+ acts stay at
  3.9 against 8.8, and follow-up chart entries barely move (1.07 -> 1.21 vs 2.43).
  The tail lifts the cap; filling the slots with hit-act follow-ups needs the
  access lift above.

Not adopted. D .80 / G 1 on plateau 20 + radio 0 is the candidate turnover setting.

### Hit-act hype probe (`hyp-v1`, `hyp-v2`, 2026-10-23)

`Systems/HitActHype.cs`, off unless named. An act's hype is
`max(0.5^(t10 / H), 0.5 * 0.5^(t40 / H))`, with t10 / t40 the weeks since its last
Top 10 / Top 40 and H `--hit-act-hype-half-life` (default 26). `--hit-act-hype=A`
adds A × hype to an AI single's launch awareness; `--hit-act-hype-radio=R` adds
R × hype to its launch radio heat. It is recency, not stock, so a fallow act loses
it within about a year. Concurrent singles by the act split it through the existing
launch share. Albums, label push and the format decision's projected awareness are
untouched. Clocks are written only while the switch is on (run-only, not saved);
an opening career's last pre-1960 hit seeds them mid-1959. Ten native checks
(`--hit-act-hype-check`). The switch-off one-year replay (`hyp-v1-off`) matches
`ctp-v1-d80g1` in all 98 simulation streams. All arms run on plateau 20 + radio 0
+ tail .80 / 1.

**One year, seed 1001:**

| 1960 | History | Tail base | A .15 | A .30 | A .15, R .05 |
|---|---:|---:|---:|---:|---:|
| Top 10 records | 103 | 112 | 117 | 118 | 111 |
| Peaks #7-10 / #1-3 | 37 / 40 | 20 / 55 | 24 / 58 | 34 / 53 | 18 / 55 |
| #9-10 entrants peaking there | 26% | 17% | 20% | 25% | 21% |
| Acts with 2+ / 3+ Top 10s | 19 / 6 | 19 / 6 | 20 / 10 | 25 / 3 | 16 / 3 |
| Big-5 Top 10 share | | 32.3% | 39.8% | 40.2% | 37.3% |

Follow-ups of acts whose first Top 10 falls in weeks 5-26, releases in the next 26
weeks:

| | Tail base | A .15 | A .30 |
|---|---:|---:|---:|
| Big-5: releases per hit act / launch awareness / P(Hot 100) | 3.64 / .63 / .51 | 3.47 / .74 / .69 | 3.53 / .76 / .57 |
| Field: releases per hit act / launch awareness / P(Hot 100) | 1.65 / .55 / .46 | 1.33 / .72 / .50 | 1.00 / .71 / .75 |
| Hot 100 follow-ups per hit act, all (history 1.71) | 1.13 | 1.24 | 1.25 |

The lift reaches field follow-ups (awareness +.16) but the gain goes to the Big-5,
whose hit acts release twice as often. Field releases per hit act fell in both
arms, which cancels their higher chart rate; with 20-24 field hit acts this is
within one-seed noise. The radio arm is worse on every row and is dropped. Both
awareness doses go to five years.

**Two years, seeds 1001/2002/3003 (`hyp-v3-a15`, `hyp-v3-a30`, 106 weeks).** At
the user's suggestion the five-year screen was stopped after its first seed
started (`hyp-v2-a15` is an abandoned partial run) and replaced by a two-year gate.
The control is the tail base's five-year runs truncated to week 106: on seed 1001,
the one-year and five-year d80g1 records match row for row through week 53.
Paired against it (delta ± SE):

| | Tail base | A .15 | A .30 |
|---|---:|---:|---:|
| Top 10 records, 1960 / 1961 | 112.3 / 112.0 | +1.0 ± 3.5 / +0.3 ± 1.5 | +3.3 ± 1.5 / +0.3 ± 1.7 |
| Acts with 2+ Top 10s, 1960 / 1961 (history 19 / 20) | 20.0 / 14.3 | -1.3 ± 1.9 / +2.0 ± 2.3 | +0.7 ± 2.7 / **+7.0 ± 2.0** |
| Field 2+ acts, 1961 | 3.0 | +0.3 ± 0.3 | **+4.7 ± 1.3** |
| Big-5 2+ acts, 1961 | 9.0 | +3.0 ± 2.0 | +3.7 ± 0.3 |
| Big-5 Top 10 share, 1960 / 1961 | 38.6 / 40.2% | +0.3 ± 4.6 / +6.9 ± 4.6 | +5.8 ± 2.9 / +0.6 ± 2.1 |
| Releases per hit act, 52 wk: Big-5 / field | 6.86 / 2.37 | +0.13 / +0.02 | +0.10 / -0.04 |
| Hit-act Hot 100 follow-ups, 52 wk (pooled): all / Big-5 / field | 408 / 249 / 124 | 412 / 268 / 117 | 453 / 290 / 133 |
| P(Top 10 \| follow-up entry) | 0.29 | 0.30 | 0.29 |

The one-year Big-5 rise was mostly seed 1001: over three seeds the Big-5 share
moves inconsistently by year and dose, within about 1.5 SE. The one-year field
release drop was noise too (releases per hit act unchanged). A .30 lifts 1961
repeat acts to history in all three seeds (13 -> 22, 18 -> 21, 12 -> 21), and the
field's share of that is the largest yet (+4.7). It works through entries, not
conversion: hit-act follow-ups on the Hot 100 rise 11% at unchanged Top 10
conversion. 3+ acts do not rise (1961: 6.0 -> 5.0), and total Top 10 records are
flat, so the added repeats displace one-hit acts. A .15 is within noise.
Follow-up volume remains far below history (field 1.0 Hot 100 follow-ups per hit
act against 2.43 overall): field releases per hit act (2.4 vs Big-5 6.9) are the
remaining limit.

### Why field hit acts release 2.4 follow-ups a year (`hyp-v3-a30`, 2026-10-23)

Two-year runs, three seeds, acts whose first non-opening Top 10 falls in weeks
5-54 (70 Big-5, 130 field, 21 studio), followed for 52 weeks through the slate log.

**The label's slot rate is not the cap.** A field hit label holds a median 9.9
single slots a year (Big-5 119) and issues 6. The hit act gets about 2 whatever the
label's rate: 2.0 singles at labels with up to 4, 4-8 or 8-16 slots a year, 3.0
above 16. At small labels the hit act already takes 60-65% of the label's singles.

**Slots per hit act in the 52 weeks, by source and outcome:**

| | Issued | Cancelled | Open at window end |
|---|---:|---:|---:|
| Big-5: hit follow-up lane / roster programme / contract delivery | 0.77 / 6.04 / 0.13 | 0.04 / 0.41 / 0 | 0.17 / 0.97 / 0.01 |
| Field: hit follow-up lane / roster programme / contract delivery | 1.33 / 0.84 / 0.15 | 0.41 / 0.57 / 0.04 | 0.33 / 0.16 / 0.08 |

Field hit acts get more lane slots than Big-5 ones (2.07 against 0.98). The Big-5
advantage is the roster programme, whose score picks a hit act again every
six weeks (the single floor) out of 2-3 slots a week. A field label's roster
programme has one slot every five weeks, so its hit act depends on the lane.

**The lane's clock sets field cadence.** An act becomes due 13 weeks after its
last single (`HitActCadence.DueWeeks`). The 13 weeks count from the hit's own
release, which is usually 5-8 weeks before its first Top 10. Slots are planned
at the eight-week horizon:

| Weeks | Big-5 | Field | History |
|---|---:|---:|---:|
| First Top 10 -> next single (median, IQR) | 4 (3-6) | 15 (8-25) | |
| First Top 10 -> next Hot 100 debut (median) | 10 | 24 | 10 |
| Between issued singles (median, IQR) | 6 (6-7) | 13 (8-18) | 13 (debut gap) |

Once running, field hit acts already release at history's spacing. The loss is
the slow first follow-up: a hit act that issued at 0, 15, 28 and 41 weeks could
fit three in the year. Slot failures cut it to 2.3. Field slots spend 7.7 weeks
per hit act postponed for preparation capacity, 1.7 for unreserved cash and 1.0
for promotion congestion, and 0.87 slots per hit act expire (113 of 130 acts'
worth, against 15 for 70 Big-5 hit acts). A field label's weekly preparation
capacity is a roll on a rate near 0.2 a week, so a queued item can wait several
weeks for one slot. About one hit act in six also leaves its label within the year.

History's hit acts had a next Hot 100 debut a median 10 weeks after their first
Top 10 and then about one every 13 weeks. That implies roughly four singles a year
at around 0.6 charting, an estimate, since the chart records only the sides that
charted. The field gap is about 1.5 singles a year: about half from the late
first follow-up, the rest from expired slots and departures.

`--hit-follow-up-finance` already targets two of the three causes: dedicated
slots for hot acts at non-Major labels, and cash factoring. On `rhb-v1` it moved
field follow-ups from 2.1 to 3.0 a year and the first follow-up from 16 to 9
weeks. It was set aside only because turnover capped total repeats, and it took
them from the Big-5. The tail now removes that cap (+16 Top 10 records a year), so
re-screening it on plateau 20 + radio 0 + tail + hype .30 with the two-year gate is
the cheapest next test. A hit-triggered first follow-up (plan a slot at the act's
earliest feasible week when it first enters the Top 40, ahead of the preparation
queue) would address the late first follow-up directly, if the finance switch
leaves it.

### Hit follow-up finance on the tail + hype base (`hff-v3`, 2026-10-23)

`--hit-follow-up-finance` (default two extra slots a year) on plateau 20 + radio
0 + tail .80 / 1 + hype .30, seeds 1001/2002/3003, 106 weeks, paired against
`hyp-v3-a30` (same assembly). The 17 finance checks and 46 programme checks pass
on this bundle.

| | History | Hype base | + finance | Paired delta |
|---|---:|---:|---:|---:|
| Field releases per hit act, 52 wk | | 2.33 | 3.31 | +0.98 ± 0.17 |
| Field Hot 100 follow-ups per hit act | | 1.00 | 1.43 | +0.43 ± 0.08 |
| Big-5 releases / Hot 100 follow-ups per hit act | | 6.96 / 4.14 | 7.31 / 4.13 | +0.36 / -0.01 |
| Field hit acts (first Top 10, weeks 5-54, per seed) | | 43.3 | 37.3 | -6.0 ± 5.2 |
| Acts with 2+ Top 10s, 1960 / 1961 | 19 / 20 | 20.7 / 21.3 | 22.0 / 19.0 | +1.3 ± 1.7 / -2.3 ± 1.3 |
| Acts with 3+, 1960 / 1961 | 6 / 9 | 6.7 / 5.0 | 8.7 / 7.7 | +2.0 ± 2.1 / +2.7 ± 1.9 |
| Field 2+ acts, 1961 | | 7.7 | 6.0 | -1.7 ± 1.2 |
| Big-5 Top 10 share, 1960 / 1961 | 1961: 22% | 44.5 / 40.8% | 41.0 / 40.1% | -3.4 ± 1.8 / -0.7 ± 3.1 |
| Top 10 records, 1960 / 1961 | | 115.7 / 112.3 | 114.3 / 113.3 | within 1.3 |
| Major single-unit share | | 39.9% | 36.7% | -3.2 ± 1.3 (0/3 up) |
| Single launches | | 6,010 | 6,387 | +6.3% |
| Single units incl. off-chart | | 334.1M | 334.6M | +0.1% |
| Closures, Small / Boutique / Independent | | 6.7 / 11.3 / 22.8% | 7.0 / 10.1 / 21.4% | within noise |

Factoring: 159-249 sales a world at 54-75 labels, $3.7-4.6M of invoices for
$3.0-3.7M over two years.

The switch does what it was built for: field hit acts release a single more a
year and put 43% more follow-ups on the Hot 100. It does not raise repeat acts.
The extra follow-ups turn existing repeaters into three-hit acts (3+ toward
history in both years), and fewer field acts reach a first Top 10, since the
same small labels' slots now go to their hit act. Total two-hit acts in 1961 fall
within noise. Together, tail + hype + finance put 1961 at 19.0 / 7.7 against
history's 20 / 9; 1960 runs over (22.0 / 8.7 against 19 / 6). The Major unit share
falls 3.2 points in every seed, as on `rhb-v1` (-4.2). Not adopted. Its funding
half still restores the commitment reserve rather than funding the release alone.

### Peak-aware tail (`tpk-v1`, 2026-10-23)

`--post-peak-demand-decay-top=T` gives a record that peaked at #1 the weekly rate
T in place of D, interpolated to D at `--post-peak-demand-top-rank=R` + 1 (default
R = 3). Without it every rank decays at D exactly. Ten native tail checks; the
46 programme checks pass. The switch-off one-year replay (`tpk-v1-off`) matches
`hyp-v1-a30` in all 98 simulation streams. Arms run on plateau 20 + radio 0 +
tail .80 / 1 + hype .30, three seeds, 106 weeks, paired against `hyp-v3-a30`.

**Release-programme fix.** The first T .90 run (`tpk-v1-t90`) failed on seed 2002
in week 74: `Programme promotion overbooked for label_0115: 474.5 + 0`, from a $0
LP booking. A label's promotion pool is its tier's base budget × marketing power ×
releases a month, so a MidTier -> Independent demotion cuts it to a third under
bookings made at the old size. `BookProgrammeCampaign` now checks only positive
spends. Callers cap spend at the available pool, which is 0 after a shrink, so
real overbooking is still refused. Only a run that would have thrown changes:
`tpk-v1-off`, on the fixed build, matches the pre-fix `hyp-v1-a30` byte for byte.
T .90 seeds 2002/3003 were rerun as `tpk-v1-t90b`; seed 1001 had finished cleanly.
The queued T .95 arm was replaced by T .85 (T .95 decays more slowly than .90).

Top 10 records with a first Top 10 in weeks 5-95, per year:

| | History | Hype base | T .85 | T .90 |
|---|---:|---:|---:|---:|
| Top 10 records | 105.5 | 107.1 | 106.1 | 99.2 |
| Peaking #1 / #2-3 / #4-6 / #7-10 | 20 / 22 / 29 / 35 | 23 / 32 / 29 / 23 | 26 / 27 / 30 / 23 | 23 / 28 / 28 / 20 |
| #1 records: Top 10 weeks / weeks after peak | 8.4 / 4.9 | 6.8 / 4.1 | 7.0 / 4.1 | 7.8 / 4.9 |
| Weeks at #1 per #1 record | 2.62 | 2.22 | 1.99 | 2.19 |
| Weeks after peak, #4-6 / #7-10 | 1.7 / 0.8 | 2.2 / 0.9 | 2.2 / 0.8 | 2.2 / 1.3 |
| Top 10 stays 1-2 / 9+ weeks | 20% / 10% | 13% / 2.9% | 14% / 3.4% | 11% / 8.5% |

Annual counts (paired delta ± SE against the hype base):

| | History | Hype base | T .85 | T .90 |
|---|---:|---:|---:|---:|
| Top 10 records, 1960 | 103 | 115.7 | -3.3 ± 2.4 | -9.0 ± 1.2 |
| Top 10 records, 1961 | | 112.3 | +3.7 ± 2.7 | -4.3 ± 4.6 |
| Acts with 2+ / 3+, 1960 | 19 / 6 | 20.7 / 6.7 | 18.0 / 8.0 | 17.3 / 4.3 |
| Acts with 2+ / 3+, 1961 | 20 / 9 | 21.3 / 5.0 | 19.3 / **10.0** | 19.3 / 8.0 |
| Field 2+ acts, 1961 | | 7.7 | -3.0 ± 1.2 | -3.3 ± 1.8 |
| Big-5 2+ acts, 1961 | | 12.7 | 0.0 ± 1.7 | +1.3 ± 0.7 |
| Big-5 Top 10 share, 1960 / 1961 | 1961: 22% | 44.5 / 40.8% | -5.6 ± 3.7 / +2.2 ± 3.8 | -4.9 ± 2.4 / +5.1 ± 3.0 |
| Field follow-ups per hit act: releases / Hot 100 | | 2.33 / 1.00 | 2.28 / 1.04 | 2.29 / 0.98 |

T .90 puts the top of the chart near history: nine-week-plus stays from 2.9% to
8.5%, #1 records' Top 10 runs and post-peak weeks on history, and 1960's Top 10
count from 116 to 107 (history 103). T .85 keeps turnover (106 a year) but
recovers almost none of that shape. Neither recovers brief #7-10 visitors or
weeks at #1: #1 records hold the Top 10 longer but still swap the top spot
quickly, so #1 tenure is set elsewhere (airplay). Both raise three-hit acts in
1961 in every seed (T .85: 10, 9, 11 against 5, 7, 3; history 9), and both cost
the field about three two-hit acts in 1961. Total two-hit acts hold near
history. Big-5 share moves in opposite directions in the two years at both
settings, within about 2 SE. Follow-up cadence is unchanged, as expected.

#### Why T .90 costs field two-hit acts

Read from the `tpk-v1` runs, no new runs. Paired against `hyp-v3-a30`, three seeds, records debuting after week 4:

| | Hype base | T .90 | Paired delta |
|---|---:|---:|---:|
| Top 10 slot-weeks held by records 6+ weeks in, 1960 / 1961 | 73 / 67 | 90 / 89 | +16.7 ± 4.9 / +22.3 ± 10.9 |
| Field Top 10 entries, 1960 / 1961 | 57.3 / 53.3 | 53.3 / 45.0 | -4.0 ± 2.5 / -8.3 ± 3.7 |
| Big-5 Top 10 entries, 1960 / 1961 | 49.0 / 42.3 | 42.7 / 47.0 | -6.3 ± 1.5 / +4.7 ± 1.2 |
| Field records peaking #7-10, 1960 / 1961 | 17.7 / 18.7 | 13.0 / 13.0 | -4.7 ± 2.7 / -5.7 ± 1.8 |
| Big-5 records peaking #7-10, 1960 / 1961 | 15.3 / 17.7 | 15.7 / 20.7 | +0.3 / +3.0 |
| Field / Big-5 records peaking #1-10 a year | 51.3 / 41.5 | 45.3 / 41.0 | -6.0 ± 2.8 / -0.5 ± 1.5 |
| Field / Big-5 records peaking #11-20 a year | 35.8 / 27.3 | 40.0 / 29.3 | +4.2 / +2.0 |
| Field climbers at #11-15 (at their sales peak), record-weeks | 127 | 108 | -15% |
| Their weekly Top 10 entry odds, field / Big-5 | .386 / .435 | .338 / .409 | -12% / -6% |

It is not a follow-up mechanism. Field follow-ups within 26 weeks of an act's first hit chart at the
same rate and, once charted, reach the Top 10 as often (.166 -> .173); their launch stock rises; whether
the act's first hit is still on the Hot 100 at release makes no measurable difference. No competitive
channel moved either. Aggregate single rationing is unchanged (cleared / serviceable 0.556 in 1961 in
both arms). Inertia-held blocks fall (108 -> 91 record-weeks). The radio position bonus is zero on this
bundle, so local sales rank does not reach rotation. Crossing into the Top 10 pays field and Big-5 climbers
the same (units x1.34 / x1.33 the next week).

The chain is slot-level. T .90 keeps about 20 more Top 10 slot-weeks a year with long runners, so there
are fewer openings. The marginal entrant loses, and the marginal entrant is a field record. Field records
reach #11-15 later (7.6 weeks after release against 6.3) with half the launch awareness (.34 against .69),
and they peak nearest the bar. They stall at #11-20 and miss the Top 10's sales step, so their peak
points fall too: field records reaching 60k points drop 148 -> 131 in every seed, while Big-5 stays at
113 -> 112. Two-hit acts are the convex tail of entries. Randomly thinning the base's field Top 10 records
by T .90's loss predicts 1961 field 2+ at 5.5 (observed 4.3, base 7.7) and 1960 at 6.2 (observed 4.7,
base 7.0). The rest, about 1-1.5 acts a year, is within three-seed noise.

So the tail is group-neutral by construction, but the slots it takes come out of the field's margin. That
widens a gap that already ran the wrong way: history's field 2+ is 8.8 a year against Big-5 5.6. Fixing it
inside the tail would mean making decay depend on label, which has no historical basis. The lever is the
Big-5 entry edge (campaign impact at launch) or field climb speed, not decay.

#### Five-year T .90 (`tpk-v2`, 2026-10-23)

`tpk-v2-t90` against its own control `tpk-v2-base` (plateau 20 + radio 0 + tail .80 / 1 + hype .30), seeds
1001/2002/3003, 265 weeks, all six passed. Their 1960-61 figures equal the two-year screens exactly
(`hyp-v3-a30`, `tpk-v1-t90`), as expected. Annual counts include carryovers; an act's group is the owner of
its first Top 10 record that year. Shape is for records with a first Top 10 in weeks 5-247.

| | History | Base | T .90 | Paired delta (seeds up) |
|---|---:|---:|---:|---:|
| Acts with 2+ / 3+ Top 10s, 1960-64 mean | 17.8 / 7.2 | 18.3 / 6.0 | 17.0 / 6.7 | -1.3 ± 1.3 (0/3) / +0.7 ± 0.4 (2/3) |
| Field / Big-5 / studio 2+ acts | 8.8 / 5.8 / 3.2 | 4.8 / 11.9 / 1.6 | 4.1 / 11.8 / 1.1 | **-0.73 ± 0.07 (0/3)** / -0.1 / -0.5 |
| Field 2+ acts, 1960 / 61 / 62 / 63 / 64 | 6 / 11 / 11 / 7 / 9 | 7.0 / 7.7 / 4.3 / 3.3 / 1.7 | 4.7 / 4.3 / 3.3 / 5.7 / 2.3 | -2.3 / -3.3 / -1.0 / +2.3 / +0.7 |
| Top 10 records a year | 105.5 | 103.9 | 99.6 | -4.4 ± 0.7 (0/3) |
| #1 records: Top 10 weeks / weeks after peak | 8.4 / 4.9 | 7.0 / 4.2 | 7.7 / 4.8 | +0.7 ± 0.2 / +0.7 ± 0.2 (3/3) |
| Weeks at #1 per #1 record | 2.62 | 2.26 | 2.23 | -0.0 ± 0.1 |
| Top 10 stays 1-2 / 9+ weeks | 20% / 10% | 11.6% / 3.7% | 12.1% / 8.3% | +0.5 / **+4.5 ± 1.5 (3/3)** |
| #7-10 peakers a year | 34.8 | 20.5 | 20.4 | -0.1 ± 1.0 |
| Big-5 Top 10 share, 1960-64 | 31.4% | 43.6% | 45.0% | +1.4 ± 1.4 (3/3) |
| Single units incl. off-chart | | 876.6M | 877.4M | +0.1% |
| Major unit share incl. off-chart | | 46.7% | 48.5% | +1.8 ± 1.1 |
| Small / Boutique unit share | | 8.9 / 8.2% | 7.8 / 7.6% | -1.1 ± 0.5 / -0.6 ± 0.6 |
| Closures, Small / Boutique / Independent | | 20.5 / 21.0 / 31.8% | 21.0 / 21.9 / 31.3% | within 5 points, SE 3-5 |

The shape holds over five years: nine-week-plus stays 3.7% -> 8.3% (history 10%) and #1 records' Top 10
runs on history, in every seed. Turnover settles 4 records a year below the control (99.6 against
history's 105.5); the first year carries most of it (-9.0 in 1960, -1 to -4.4 after). #1 tenure and the
#7-10 shortfall are untouched, as in the screen.

Field two-hit acts cost less than the two-year screen implied. The 1960-61 loss (-2.3, -3.3) is followed by
-1.0, +2.3 and +0.7, for -0.73 ± 0.07 a year over five years, down in every seed. The control's own field
repeats collapse anyway, from 7.0 in 1960 to 1.7 in 1964 against history's 6-11. That collapse, not T .90,
is the field gap (4.8 against 8.8). T .90 moves about 2 points of unit share from Small and Boutique labels
to the Majors (1 seed flat), consistent with the slot reading above: the Top 10 margin that long runners
take is the field's. Units and closures do not move.

T .90 passes the five-year check as the tail shape for the next base. The open items are the field's repeat
collapse after 1961, #1 tenure (airplay) and the missing brief #7-10 visitors.

#### Why the control's field repeat acts collapse after 1961

Read from `tpk-v2-base` (three seeds, 265 weeks) and the earlier slate and MidTier runs; no new runs. Per seed:

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Field acts with a Top 10 / their Top 10 records | 48.7 / 57.3 | 49.3 / 56.7 | 47.7 / 51.7 | 47.7 / 51.0 | 45.0 / 46.7 |
| Field 2+ acts | 7.0 | 7.7 | 4.3 | 3.3 | 1.7 |
| Field share of Top 10 slot-weeks | 48% | 51% | 48% | 46% | 42% |
| Field MidTier labels alive at 1 January | 20.0 | 11.3 | 5.0 | 4.3 | 4.7 |
| Field Independents alive at 1 January | 201 | 191 | 271 | 323 | 374 |
| Field Top 10 records at MidTier / Boutique / Independent labels | 11.7 / 11.0 / 29.3 | 4.3 / 9.7 / 34.0 | 3.0 / 10.0 / 37.0 | 4.7 / 7.0 / 37.3 | 4.0 / 6.7 / 33.0 |
| Field 2+ acts at MidTier / Boutique / Independent labels | 1.7 / 1.3 / 3.3 | 0.7 / 1.0 / 3.7 | 0.3 / 1.0 / 2.7 | 0.3 / 0 / 2.7 | 0 / 0.3 / 1.3 |

Field 2+ is convex in records per act, which fall from 1.18 to 1.04. Two things drive it:

1. **The field's high-capacity labels die.** A field label's single slots a year at a Top 10 entry are about 16 at a
   MidTier, 10 at an Independent and 5 at a Boutique. Of 60 opening field MidTiers, 26 are bankrupt by the end of 1960
   and 40 by the end of 1961: median 9 weeks to Struggling, 40 to Defunct. They took 1.7 of 1960's 7 repeat acts.
   Their slots and Top 10 share go mostly to the Big-5 (field slot share 48% -> 42%).
2. **Hits move to small Independents.** Field Independent hit acts rise from 25 to 34 a year as Small labels go
   national (201 -> 374 Independents), but each has a ~9-10 slot envelope shared by about five acts. A hit act wins
   the same 11-14% of its label's non-lane slots in every year. So its follow-ups fall with the envelope: issued
   singles in the 52 weeks after a first hit go from 2.4 to 1.85, and acts with no follow-up from 10% to 23%. Only
   4-8% of field hit acts change label, and none go to the Big-5. Top 10 conversion per charting follow-up is
   flat (.19-.27).

The slate's source labels shift too: roster-programme issues fall from 2,624 to 50 a year while contract-delivery
issues rise from 48 to 2,381. That is only the classification (an act that still owes contract singles is
labelled contract-delivery), not a second pool. Field issue volume is flat at ~2,500-2,900 a year.

**The MidTier die-off came in with the release programme, not with the MidTier fixes.** Opening MidTier closures
(Big-5 excluded; studio subsidiaries included):

| Run | Flags | Closed by end 1960 | by end 1961 | by end 1964 |
|---|---|---:|---:|---:|
| `mtn5-pair` | national A&R + going-concern cash | 8 / 59 | 19 | 24 |
| `slate-working-v14-off-control` | same, no slate / capacity / studio parents | 8 / 59 | | 24 |
| `rhb-v1`, `cef-v2-p20r0`, `ctp-v2-d80g1` | + slate + capacity + studio parents | 23-26 / 75 | 37-39 | 41-43 |
| `tpk-v2-base` | + tail + hype .30 | 26 / 75 | 40 | 47 |

The 53-week `slate-short-v15` split (seed 1001, the 2026-10-22 build) separates the flags. Opening MidTier
closures are 3 / 24 without the slate flags, 7 with `--label-release-slate` alone, 8 with `--label-release-capacity`
alone, 8 with both and 14 / 30 once studio parents add their six. Under the slate, MidTiers issue 2.7 singles per
label in weeks 1-13 against the control's 5.8 (2.6 against 4.2 in weeks 14-26), gross falls a third, and Struggling
labels at week 13 rise from 3 to 8. On the current build MidTiers issue 4.4 of 8.5 planned singles in the first 26
weeks. The main block is `preparation-capacity-unavailable`, about 11 postponements per label, ahead of
insufficient cash (5.4) and issue capacity (3.5). Planning, preparation and issue are each drawn at the same weekly
rate, `releasesPerMonth` x .67 / 4.33. Work that misses a preparation draw queues behind the next planned slot at
utilization one, and items expire at the eight-week window. Struggling halves all three rates, so a slow first
quarter feeds on itself. Under the MidTier standard these closures are unearned: the label's records are not
the cause.

Fixing field repeats starts with MidTier throughput under the slate. The probe would give non-Major preparation
capacity headroom over the planning rate, or have preparation not consume the dated slot. It should be screened at
53 weeks on MidTier first-quarter issues and closures, then at five years on field 2+.

### Preparation headroom (`phr-v1`, 2026-10-23)

`--slate-preparation-headroom=H` (1-4, needs `--label-release-slate`) runs a non-Major label's weekly preparation
draw at H x its planning rate; Majors and issue capacity are unchanged. Off, the rate passes through unchanged.
Eleven native checks (`--slate-preparation-headroom-check`) cover validation, the Major exclusion, scaling, and
that headroom never removes a slot under the same keyed roll. The 46 programme checks pass with it off and at 2.
`phr-v1-off` (seed 1001, 53 weeks) matches `tpk-v2-t90` row for row through week 53. Arms run on the T .90
bundle, three seeds, 53 weeks, paired against `tpk-v2-t90` read through week 53. Opening MidTiers exclude the Big-5
(25 a seed).

| Opening MidTiers, per label | T .90 base | H 1.5 | H 2 |
|---|---:|---:|---:|
| Preparation-capacity postponements, weeks 1-26 | 10.9 | 5.5 | 3.8 (-7.1 ± 0.8) |
| Issue-capacity postponements | 3.3 | 6.2 | 5.4 (+2.1 ± 0.3, 3/3) |
| Insufficient-cash postponements | 4.2 | 6.5 | 8.6 (+4.4 ± 1.6, 3/3) |
| Singles issued, weeks 1-13 / 14-26 / 27-52 | 2.71 / 1.51 / 2.00 | 2.63 / 1.49 / 2.01 | 2.61 / 1.38 / 1.79 |
| Struggling at week 13 / 26 (count) | 7.3 / 3.0 | 8.7 / 4.7 | 8.3 / 3.0 |
| Closed by week 52 (count) | 9.3 | 10.3 | 9.0 |
| Independent closures by week 52 | 11.1% | 12.3% | 12.5% |
| Top 10 records, 1960 | 106.7 | 110.0 | 110.3 (+3.7 ± 1.5, 3/3) |

Headroom does what it says: the preparation queue clears. Nothing more is issued. The same work now waits at
the issue draw, which runs at the old rate, and at cash, because more prepared items hold reservations at once. MidTier
issues, Struggling and closures stay within noise, and Independents close slightly more often. Not adopted.

The preparation queue is a symptom. MidTiers lose output in two layers (seed 1001, weeks 1-13, per label):

| | Before the slate (`slate-short-v15-off-control`) | Slate (`-off-allocation`) | Current bundle (`tpk-v2-t90`) |
|---|---:|---:|---:|
| Singles / LPs launched | 5.79 / 1.12 | 2.71 / 0.33 | 2.80 / 0.30 |
| Planned single capacity (status factor included) | | | 4.07 |

1. **Envelope.** The slate's planned capacity is `releasesPerMonth` x .67 / 4.33 a week for singles (x .33 for
   LPs) x status. That gives about 4 singles and 2 LPs a quarter, against the 5.8 + 1.1 the control's
   weekly release chance produced.
2. **Conversion.** Only ~70% of planned singles and ~15% of planned LPs issue. Preparation, issue and cash
   are separate gates, each at or near the planning rate, and Struggling (22-31% of MidTier weeks in the
   first quarter) halves every gate.

Relieving one gate moves the queue to the next. A MidTier probe has to open the issue rate and preparation
together, or replace the stacked draws with a single per-label work budget. Its target is the pre-slate
first-quarter output, about 6 singles and 1 LP. The cash gate should be watched as the next binding one, since
going-concern opening cash is sized to the overhead reserve.

### Singles issued per label, 1960 (research for the per-label work budget, 2026-10-23)

Counts are catalog-number spans between dated releases. Each label's numbers ran in issue order, so the span
from its first to its last 1960 release approximates its singles that year. Exact counts come from complete
singles lists (Cadence and Bobby Robinson's labels, from Callahan/Edwards on bsnpubs). The others are anchored
on dated artist releases (Wikipedia discographies, musicvf chart-entry months, which lag release by a few weeks),
so they carry ±10-15%. Unissued numbers inflate spans slightly: Cadence skipped 4 of 22 numbers in 1961.

| Label (company) | Model tier | 1960 anchors | Singles, 1960 | Model plan / issued, 1960 |
|---|---|---|---:|---:|
| Columbia | Major | 41511 (Nov 59) ... 41922 (Jan 61) | ~360 | 141 / 82 |
| RCA Victor (47- series) | Major | 7740 (Apr) ... 7850 (Feb 61) | ~160 | 137 / 88 |
| King (main series) | MidTier | 5312 (Mar) ... 5430 (Dec) | ~150, + ~18 Federal | 18 / 11 |
| Dot | MidTier | 16040 (4 Jan 60) ... 16229 (12 Jun 61) | ~130 | 12 / 4 |
| Imperial | Independent | 1960 Domino 5645-5704, 1961 5723-5796 | ~80 (1961 ~85) | 11 / 6 |
| Liberty | Independent | 55234 ... 55287; 55296 (1961) | ~70 (1961 ~115) | 6 / 4 |
| Vee-Jay | Independent | 354 (Aug) ... 375 (early 61) | ~45 | 12 / 8 |
| Atlantic (main series; Atco adds ~25-30) | MidTier | 2050 (28 Jan) ... 2087 (early Dec) | ~40 | 12 / 7 |
| Laurie | (not modelled) | 3044 ... 3070; 3081 (1961) | ~37 | |
| Chess (main series; Checker, Argo extra) | MidTier | 1747 (Feb) ... 1767 (Nov) | ~25 | 16 / 1 |
| Swan | (not modelled) | 4050 ... 4066; 4071 (1961) | ~25 | |
| Monument | Independent | 421 (May 60) ... 438 (Mar 61) | ~20 | 6 / 2 |
| Cameo (Parkway extra) | MidTier | 171 (Feb) ... 182 (Nov); 186 (Jan 61) | ~15 | 11 / 2 |
| Cadence | (not modelled) | complete list 1376-1390 | 15 (1959 16, 1961 18) | |
| Fire / Fury (Robinson) | Independent | complete list | 25 / 13 (all 7 imprints 79) | 9 / 9 |
| Revelation, VIM, Vest, Fling, Everlast (Robinson) | | complete list | 21, 11, 4, 3, 2 | |

The model's 1960 total is 2,962 singles from about 600 labels: Majors 446, MidTiers 184, Independents
1,084, Smalls 764, Boutiques 484 (release census; `single-launch-quality` agrees). The 16 historical companies
above alone issued about 1,300. No sourced industry total for 1960 turned up.

What this says for the budget:

1. **Output was skewed by firm, not set by tier.** One tier label held 15-singles firms (Cadence, Cameo) and
   150-singles firms (King, Dot). Cadence's 15 singles carried a top hit-making label, so volume and hits are
   separate. The model's tier envelopes are narrow: MidTier `releasesPerMonth` is 1-2.5 and Independent 0.5-1.5.
2. **Every tier issues several times fewer singles than history.** Majors do 70-90 against 160-360. Historical
   MidTiers do 1-11 against 15-150 per imprint. Named Independents do 2-11 against 13-80. That fits the earlier
   finding that only ~8% of model singles lose money against history's 80-90%: history's flops came from volume.
3. **For MidTiers, the pre-slate output is the closer anchor.** About 23 singles a year (5.8 a quarter) sits in the
   second rank (Atlantic, Vee-Jay, Laurie, Swan, Chess main); the slate's ~11 does not. King- or Dot-scale
   output (70-150) is a head-firm scale. It needs the demand pool rebalanced first, because units per release
   would fall.

Proposed sizing for the first budget probe: non-Major singles a year by firm, set by the label's own
`releasesPerMonth` and scaled to the pre-slate realized output (MidTier ~20-40, Independent ~8-25, Small and
Boutique ~3-12). A Struggling label's budget shrinks its spend, not its slot count. The historical head scale
(King, Dot, Imperial, Liberty at 70-150) and the Majors' 160-360 are deferred to a separate volume-and-demand
calibration.

### Head-firm and Major volume against the singles purse (design sketch, 2026-10-23)

This sketch covers what the deferred calibration would do to demand: head firms at 70-150 singles a year, Majors
at 160-360. It is first-order arithmetic on `tpk-v2-t90` (seed 1001; 1960 unless noted; seed 2002 agrees on
clearing). Nothing has been run.

**The singles market is a fixed purse.** `ChartSimulator.CalculateRegionalSales` computes each record's
intent on its own. The cohort normalizer (`SingleOpportunityLedger`) is an enabled/accepted ratio and does
not depend on how many records there are. Records compete only at clearing. A region's single budget is
buying population x 1.34 a week, less the LP budget, less the Major off-chart hold. `AllocateProportionalLocal`
then scales every single's intent by the same factor r = budget / intent.

| Year | Region-weeks where intent > single capacity | Serviceable single intent (M) | Cleared singles (M) | Intent / capacity |
|---|---:|---:|---:|---:|
| 1960 | 72% | 237 | 142 | 1.27 |
| 1961 | 92% | 267 | 149 | 1.57 |
| 1962 | 96% | 269 | 151 | 1.56 |
| 1964 | 96% | 315 | 163 | 1.65 |

Extra volume therefore adds intent, not units. Each added single lowers r for every record, hits included, and
the cuts are proportional. The 100 chart slots are fixed too, so the charting rate falls on its own as volume
rises.

**Where the volume sits now (1960, label-sales units, which exclude the off-chart base):**

| Tier | Singles | Charted | Units (M) | Units / single (k) | Cleared per never-charted single (k) | Median net, never-charted 1960-61 single | Lose money |
|---|---:|---:|---:|---:|---:|---:|---:|
| Major (6) | 449 | 47% | 53.9 | 120 | 20.6 | -$2,980 (production ~$5,600) | 81% |
| MidTier | 186 | 38% | 26.2 | 141 | 18.8 | -$930 | 67% |
| Independent | 1,098 | 18% | 40.7 | 37 | 10.0 | -$145 | 53% |
| Boutique | 489 | 15% | 13.7 | 28 | 9.0 | +$100 | 48% |
| Small | 784 | 7% | 7.2 | 9 | 5.8 | +$64 | 48% |

On top of this, the Major off-chart base books 18.6M units a year, held out of the same purse (`offchart-singles.csv`
93.4M over five years). It nets about $0.20 a unit (Columbia: $3.2M net on 16.2M units), about $640k a Major a
year. Rosters are thin as well as slow. Majors carry 33-48 acts at 1.9 singles an act and MidTiers 14 acts at 0.54.
King has 21 acts and 11 singles.

**Scenarios.** Each added single carries its tier's present never-charted intent (cleared / r) unless scaled. All
other intents are held static.

| Scenario | Singles / yr | r | Hit units | Major unit share (base incl.) | MidTier / Indep / Small units (M) |
|---|---:|---:|---:|---:|---|
| S0 current | 3,006 | .60 | x1.00 | 45.2% | 26.2 / 40.7 / 7.2 |
| S1 first budget probe (MidTier +400, Indep +250; pre-slate ~3,650) | 3,656 | .56 | x0.93 | 43.0% | 31.5 / 40.3 / 6.7 |
| S2 + head firms (King, Dot, Imperial, Liberty +420) | 4,076 | .54 | x0.89 | 41.7% | 34.9 / 39.9 / 6.4 |
| S3 + Majors to ~1,100 (+650), base kept | 4,726 | .49 | x0.82 | 46.2% | 32.2 / 36.8 / 5.9 |
| S4 = S3 with the base retired | 4,726 | .56 | x0.93 | 39.2% | 36.4 / 41.6 / 6.7 |
| S5 = S4, added singles at 1/2 intent | 4,726 | .61 | x1.02 | 38.7% | 33.3 / 43.6 / 7.4 |
| S6 Majors +650 only, base retired | 3,656 | .62 | x1.03 | 43.4% | 27.1 / 42.1 / 7.4 |

In S4 an added Major single clears about 19k units. In S5 it clears about 10k. These numbers ignore chart
feedback, spillover routing and genre mix. They are for sizing, not prediction.

What this means for the budget:

1. **For the Majors, volume swaps out the off-chart base.** The base already stands in for their uncharted
   catalogue, country, MOR and specialty 45s, held out of the same purse. Real volume plus the base would
   count that output twice and cut everyone else's r (S3: hits -18%, Small units -18%). Taper the base by each
   Major's progress toward its volume target and keep the hold. Then S6 is close to neutral for other labels.
2. **The marginal release is a different object, a line single.** At today's Major costs, a never-charted single
   loses $3k at the median. Each Major would add about 130 of them. At about 19k units, $0.20 a unit and $5.6k
   production, the swap moves a Major's net by about -$850k a year, against about $1.3M annual profit (Columbia). At
   about $1.5k production (one side of a four-side session) the swing is about -$315k. The budget needs a cheap
   release class: session sides or reissues of retired masters (`retired-track-availability`, which LP assembly
   already draws on). It would take no company promotion pool, so the slots stay out of the promo push and the
   airplay channel. A line single is priced by the slot, not forecast for margin.
3. **Marginal intent sets the hit tax.** If line singles keep today's never-charted intent, the full stack costs
   hits 7% even after the base is retired (S4). At half that intent the stack is neutral (S5). This is the same
   lever the loss-rate and stocking work already need: median non-hit demand in the low thousands, with the
   hit tail kept. Plan for line singles to carry less discovery than a standard single. They should not get
   #100's uncharted purchase exposure. `UnchartedSingleExposure` is the existing hook, scoped by release
   class instead of applied to every stalled record (which was rejected).
4. **Slot counts should come from the organisation, not from forecast margin.** Revenue memory reads realized
   units per release, which lag r. A label sizing slots from expected margin overestimates every added
   release until r catches up, and then cuts back. That is a cycle with no historical counterpart. Size slots
   from roster, A&R and session capacity, keep the marginal cost low and fixed, and test the economics after the
   fact.
5. **Volume is roster x cadence.** Columbia at 360 needs about 100 acts at 3-4 singles, or line singles that need
   no roster act (reissues, house and session acts). King at 150 needs its 21 acts at about 7 each, or a
   bigger roster. Present per-act cadence (0.54 for MidTiers) is the larger gap. Bigger rosters draw on
   payroll and the talent pool, and those need their own check.
6. **The chart moves through airplay.** A lower r cuts units but leaves airplay alone, so airplay takes a larger
   share of chart points. Airplay convexity (k = 5) amplifies label push. The Majors are already over in the Top 10
   (36-39% against 22%). Keep r within about ±5% of the current .60, and read the airplay share of Top 10 points.
7. **Flop-income labels pay the dilution.** Smalls live on never-charted units: 59% of their units in 1960, 78%
   in 1964. Under S3 they lose 18% and under S4 7%. Read Small and Boutique closures alongside r.

Proposed order: (a) the first budget probe as planned (S1, r .60 to .56); (b) Major line singles with the base
taper (S6, aiming for no change in other labels' units); (c) head-firm volume once line-single intent is settled
(S2 to S5). Every arm reports yearly r (cleared over serviceable single intent from `market-clearing-weekly.csv`,
which `--calibration` also writes), hits' cleared units, the chart-week unit share, the airplay share of Top 10
points, Major unit share including the base, loss share by tier, and Small, Boutique and MidTier closures.
Runtime should be measured too: singles rise by up to 57%, and each one is a per-region record every week.

### Per-label work budget (`wbg-v1`, 2026-10-24)

`--slate-work-budget=S` (0.25-3; needs `--label-release-slate` and `--label-release-capacity`; refuses
`--slate-preparation-headroom`) makes a non-Major label's planned intentions its whole work budget. Planning runs
at S singles a month per unit of `releasesPerMonth` (LPs keep .33) through the existing keyed slot draw. Preparation
and issue take no second dated draw, and a budget label has no idle-slot pass. Planned work is lost only to cash,
material, economics or the eight-week window. A Struggling non-Major keeps its full slot rate and halves its campaign
reserve instead (`SlotStatusFactor`, `CampaignStatusFactor`). Majors, the player and every other status are unchanged.
The Majors' MonthlyCapacity is untouched. Off, no path changes: `wbg-v1-off` (seed 1001, 53 weeks) matches
`tpk-v2-t90` row for row in all 62 week-keyed CSVs through week 52. The new build passes 27 native checks
(`--slate-work-budget-check`), the 46 programme checks, the live programme check at S 1 and a 13-week save/load round
trip at S 1. That round trip's separate gzip-I/O step ran out of memory under the 2.5 GB heap cap; the save/load
itself passed.

Arms run on the T .90 bundle, three seeds, 53 weeks. They are paired against `tpk-v2-t90` read through week 52
(`analyze_work_budget.py`; table in `SimLogs/wbg-v1-readout/paired.csv`). S .67 keeps the slate's envelope and only
removes the stacked draws. S 1 also widens it. Opening firms are counted by initial tier (25 MidTiers a seed). Deltas
are mean ± SE over seeds.

| | Control | S .67 | S 1 |
|---|---:|---:|---:|
| MidTier singles / label, weeks 1-13 / 14-26 / 27-52 | 2.71 / 1.51 / 2.00 | 2.98 / 2.02 / 3.91 | 4.26 / 2.93 / 5.33 |
| MidTier LPs / label, weeks 1-52 | 2.12 | 3.19 | 3.10 |
| MidTier capacity postponements (preparation + issue), weeks 1-26 | 18.1 | 0 | 0 |
| MidTier cash postponements, weeks 1-26 | 5.7 | 16.0 | 23.3 (+17.6 ± 1.2) |
| MidTier Struggling at week 13 / closed by week 52 (count) | 7.3 / 9.3 | 7.7 / 7.7 | 6.0 / 7.7 (-1.7 ± 1.5, 2/3) |
| Independent / Small / Boutique closed by week 52 | 22.3 / 0.7 / 7.0 | 24.7 / 0.7 / 6.0 | 25.0 / 1.3 / 7.3 |
| Singles issued, 1960 | 2,940 | 3,264 | 4,405 (+1,466 ± 51) |
| Of which MidTier / Independent / Small / Boutique | 155 / 1,154 / 775 / 467 | 221 / 1,297 / 829 / 530 | 310 / 1,805 / 1,145 / 772 |
| r (cleared / serviceable single intent) | .602 | .588 | .497 (-.106 ± .014, 3/3) |
| Cleared single units (M) | 142.9 | 142.3 | 143.6 (+0.8 ± 1.1) |
| Top 10 units (M) | 49.4 | 48.1 | 44.1 (-5.3 ± 0.8, 3/3) |
| Share of units sold while on the Hot 100 | .841 | .825 | .774 |
| Major unit share, off-chart base included | 45.7% | 44.6% | 38.9% (-6.8 ± 2.1, 3/3) |
| Top 10 / Hot 100 records, 1960 | 106.7 / 670 | 107.7 / 694 | 111.3 / 709 |
| Loss share, singles issued weeks 1-26 and retired by week 52: Major / MidTier / Indep / Small / Boutique | .40 / .32 / .48 / .66 / .47 | .45 / .34 / .47 / .62 / .48 | .46 / .43 / .52 / .71 / .54 |

The budget removes the stacked gates, and cash becomes the binding gate as predicted. At S .67 every capacity
postponement disappears, but MidTier output rises only from 6.2 to 8.9 singles a year, because insufficient cash
replaces the queue. S 1 reaches 12.5 a year. That is double the control, but still short of the pre-slate world's
~16.6 and its 5.8 in the first quarter. MidTier closures and Struggling move down within noise. Independent closures
rise slightly (+2.7 ± 1.5, 2/3).

The purse behaves as the sketch says. At S 1 cleared single units do not move (+0.8M on 143M). Every added single
lowers r, and r falls to .497. That is where the pre-slate control sat (`slate-short-v15-off-control`, .488, with
4,434 singles), so the slate's r of .60 was itself a by-product of its lower volume. The purse cut lands on hits, at
-11% of Top 10 units. Because the Majors' slots are fixed, their unit share falls by 6.8 points. Loss shares rise
5-10 points in every tier, which is the historical direction. Most of the added volume is not MidTier (+155) but
Independent, Small and Boutique (+1,326), which scale with S on the same `releasesPerMonth` rule. That is about
twice the sketch's S1 assumption (+650). It overshoots the ±5% r guard, and the first-order arithmetic (S2, r .54
for +1,070) is consistent with what happened.

Not adopted at either value. The next probe should separate the two effects. Hold the small tiers near S .67, which
leaves them near their slate output and r near .59. Give MidTiers their own S, and treat MidTier cash as the
binding gate. Going-concern opening cash is sized to the overhead reserve, not to a 20-40 single programme.

### MidTier work budget and the going-concern reserve (`wbg-v2`, `wbg-v3`, 2026-10-24)

Two switches on top of `--slate-work-budget=S`:

- `--slate-work-budget-midtier=SM` (0.25-3) gives MidTiers their own budget, and the smaller tiers keep S.
- `--slate-going-concern-reserve` (needs the budget; work-budget MidTiers only) tests slate work against a
  going-concern reserve. Overhead, the negative-flow hold, plant payables and programme holds stay. The
  tax-and-service hold (`LabelCorporateModel.TaxAndServiceReserve`: tax payable, unprovided tax and two months of
  commercial spend) is replaced by the tax that leaves within two month-ends (`LabelCompanyTax.DueWithin`, the
  `PayQuarter` schedule on the current books). The same reserve applies wherever a slate item is funded or
  issued: the reservation, the session (`PrepareSlateMaster`), the single issue test in `TryReleaseRecord` and the
  LP first-event test (`TryClampFirstEventCost`). A budget run also writes `*-programme-cash-shortfalls.csv`, one
  row per refused reservation, with the reserve split into its parts. The telemetry is inert: the cash-shortfall
  rows and the finance, launch and clearing CSVs match `wbg-v2-split15` with and without the tax split.

Native checks: 49 budget checks (switch validation, tier scope, the reserve identity, and `DueWithin` month by month on
fixed books for both tax rules), the 46 programme checks with both switches on, and a 13-week save/load round trip.
Off is inert after every change: `wbg-v2-off`, `wbg-v3-off`, `-offb` and `-offc` match `tpk-v2-t90` row for row in all
62 week-keyed CSVs through week 52.

**Why MidTiers were refused cash** (`wbg-v2-split15`, S .67 / SM 1.5, cash shortfall telemetry, opening MidTiers,
weeks 1-52). There are two populations. The 27 MidTiers that close account for 3,136 refusals. They hold a median
$16k and are short of overhead and a negative-flow hold, so those refusals are earned. Removing the whole
tax-and-service hold would clear only 31% of them. The 42 that survive account for 4,739 refusals. They are
refused a median $2.5k single while holding a median $162k. Of the $187k reserve, 73% is the tax-and-service
hold. On seed 1001 it is half tax payable, accrued since January but due in March and June 1961 under
`--prior-year-corporate-tax`, and half the two-month commercial hold. That is distribution, trade support and excise,
which are paid out of the receipts that create them. Unprovided tax is negligible. Dropping the commercial hold
alone would clear 83% of the survivors' refusals; dropping both would clear 95%.

**A second gate.** The first version applied the going-concern reserve only to the reservation and the session. Cash
refusals halved, but singles barely moved, because prepared masters were refused at issue against the ordinary
reserve until they expired. Of 30 planned MidTier singles a year, 7 expired, mostly after production had been paid.
Extending the reserve to the two issue tests fixed it: expiries fell to 2.9 a year, and issues rose to 22.4 of 31.
The tables below are the corrected arms (`-gc15c`, `-gc10c`).

| | Control | Split, SM 1.5 | GC, SM 1.5 | GC, SM 1 |
|---|---:|---:|---:|---:|
| MidTier singles / label, weeks 1-13 / 14-26 / 27-52 | 2.71 / 1.51 / 2.00 | 6.29 / 3.79 / 6.61 | 6.39 / 6.15 / 9.99 | 4.34 / 4.14 / 7.00 |
| MidTier singles / label, 1960 (pre-slate ~16.6) | 6.2 | 16.7 | 22.5 | 15.5 |
| MidTier LPs / label, 1960 | 2.12 | 2.97 | 4.20 | 4.20 |
| MidTier cash postponements, weeks 1-26 | 5.7 | 35.0 | 12.2 | 9.4 |
| MidTier closed by week 52 (count of 25) | 9.3 | 8.7 | 6.7 (-2.7 ± 1.5) | 6.3 (-3.0 ± 1.7, 2/3) |
| MidTier median cash at week 52, survivors ($k) | 240 | 342 | 385 | 372 |
| Independent / Small / Boutique closed by week 52 | 22.3 / 0.7 / 7.0 | 24.0 / 0.3 / 8.3 | 23.7 / 0.7 / 6.7 | 24.3 / 1.0 / 7.3 |
| Singles issued, 1960 (MidTier) | 2,940 (155) | 3,459 (412) | 3,589 (561) | 3,427 (384) |
| r | .602 | .560 | .515 | .525 (-.078 ± .008) |
| Top 10 units (M) | 49.4 | 45.7 | 43.7 | 44.7 (-4.7 ± 0.7) |
| Major unit share, base included | 45.7% | 42.4% | 38.7% | 39.9% (-5.8 ± 1.9) |
| Top 10 / Hot 100 records | 106.7 / 670 | 111.3 / 698 | 112.7 / 700 | 110.0 / 692 |
| MidTier loss share (singles retired by week 52) | .32 | .44 | .49 | .41 |

**What the arms show.**

- The cash gate was holding back profitable work. With the going-concern reserve, MidTiers issue more singles and
  twice the control's LPs. They end the year with more cash than under the stacked reserve, and fewer of them close
  (6.3-6.7 of 25 against 9.3). Firms that go on to close hold 80-89% of the remaining refusals.
- **GC with SM 1 is the closest arm to the target.** MidTier output is back at the pre-slate level: 15.5 singles a
  year, and 4.3 in the first quarter against the earlier 5.8. Closures are down 3 ± 1.7.
- **The purse pays for it.** The added work is single intent with larger campaigns, because funded campaigns are
  no longer clamped by cash. r falls to .525 and Top 10 units to -9.5%. The Majors, whose slot count is fixed, lose
  5.8 points of unit share. Small-tier volume at S .67 is unchanged across arms (about +270 singles), so the r cost
  beyond .588 is MidTier.

Not adopted yet. `wbg-v3-gc10c` (S .67, SM 1, going-concern reserve) is the candidate for the five-year screen. The
open question is the purse: at r .525 hits lose about 10% of units. That belongs to the line-single and Major-swap
design in "Head-firm and Major volume against the singles purse", not to the MidTier budget.

### Next calibration: volume and repeat hits together (2026-10-24)

This is a proposed experiment order, not an adopted configuration or new historical evidence. The figures below
come from the screens above. Company release-count anchors are approximate; no sourced industry-wide 1960
singles total has been established. Do not turn the model's total issue count into a historical target.

**The repeat target must be split by owner group.** On the five-year T .90 base, annual acts with 2+ Top 10s
are already 17.0 against history's 17.8, and 3+ are 6.7 against 7.2. But field / Big-5 / studio 2+ acts are
4.1 / 11.8 / 1.1 against 8.8 / 5.8 / 3.2. Matching the pooled count can conceal the entire remaining problem.
History's Big-5 Top 10 share is 31.4% over 1960-64, against 45.0%; the 22% reference elsewhere is 1961 alone.

1. **Check that the MidTier repair lasts.** Compare the candidate `wbg-v3-gc10c` (S .67, SM 1, going-concern
   reserve) with T .90 through five complete years, seeds 1001/2002/3003. First-year output (15.5 singles per
   opening MidTier) and closures (6.3 of 25) establish a promising mechanism, not a five-year fix. Read each
   year's surviving field MidTiers, their output and repeat acts, and 52-week follow-ups of fully observed hit
   cohorts. A one-year repeat count cannot answer the post-1961 collapse. Keep the measured purse cost visible:
   r .602 -> .525 and Top 10 units 49.4M -> 44.7M in year one.
2. **Then isolate first-follow-up timing.** If field follow-ups remain scarce after the budget repair, test a
   recent-Top-40-triggered intention at the earliest feasible single week, within the existing work budget.
   Reuse an open intention for the act rather than duplicating it; retain the single floor, material checks,
   paid-master accounting and campaign budget. Compare to the repaired budget alone. Diagnose first Top 10 to
   next release, first Top 10 to next Hot 100 debut (history median 10 weeks), subsequent debut spacing (13
   weeks), expiries and silence. Roughly four releases per field hit act per year is an inferred working aim,
   not an observed release quota. The old field rate was 2.3-2.4. Do not enable the whole factoring-plus-extra-
   slots arm as a substitute: `hff-v3` raised follow-ups but did not raise two-hit acts overall.
3. **Build the marginal single before widening head-company slots.** A cheap session-side or controlled-master
   reissue needs explicit production cost, master eligibility and demand treatment. It must not consume a full
   promoted-single campaign or automatically receive a chart-tail level of discovery. Then increase Major
   issues while tapering each Major's synthetic off-chart allowance against its added output. Track actual
   units replacing the allowance, so volume plus the old allowance cannot book the same business twice.
   Neither a cheaper cost nor weaker demand is an accepted parameter yet; the half-intent sketch is a sizing
   scenario. Pilot the Major swap before adding the King/Dot/Imperial/Liberty head-company expansion.
4. **Size programmes by company capacity.** The release-count anchors span Columbia ~360, RCA ~160, King ~150,
   Dot ~130, Imperial ~80, Liberty ~70 and ordinary second-rank firms ~15-45 a year. One tier multiplier cannot
   reproduce that spread. Check roster x feasible cadence, A&R/session capacity and catalogue supply for each
   programme; a 100-act roster at 3-4 singles is a different cost structure from reissues or session acts.
   The smaller tiers should not inherit a multiplier intended to restore the head firms.

Every comparison should report annual issues by company and source, 2+/3+ acts by field/Big-5/studio, first-hit
and follow-up entry/conversion rates, r, hit units, Major share including the remaining allowance, Top 10 airplay
share, completed-cohort losses, closures and runtime. Preserve opening-catalogue and carryover definitions from
the cadence analysis. The existing +/-5% r guard is a review diagnostic against the low-volume base, not an
independent historical fact: retain it while testing the replacement mechanism, then assess the joint volume,
units and hit distribution rather than preserving .60 at any cost. Do not raise aggregate consumer demand to
make extra releases profitable without separate evidence for a larger market.
