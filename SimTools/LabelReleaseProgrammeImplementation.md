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
