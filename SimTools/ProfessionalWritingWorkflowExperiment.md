# Planned assignments and delivery offers — 2026-11-02

This controlled test follows the
[career concentration investigation](ProfessionalWritingCareerConcentrationInvestigation.md).
It adds two independent, opt-in policies to `--professional-writing-market`.
The plain market setting retains its previous behavior.

## Policies

`--writing-market-planned-assignments` restricts autonomous commissions to a live,
unprepared single-release intention for the same label, act and artist contract.
The supplier's existing two- or three-week delivery must fit before the plan's
ready week, within the existing eight-week planning horizon. Issued/cancelled
plans, existing masters and already pending/ready material searches do not qualify.
The order saves the actual intention ID. Preparation of that intention waits
while its linked commission is pending; delivery or cancellation ends the wait.
The plan still uses normal preparation, issue, funding and selection rules.

An intention currently being prepared is not eligible for a new commission.
Preparation is tracked across the whole preparation call, including repertoire
initialization and project review, so a nested material lookup cannot order work
for the master already being cut. A stored preparation date also disqualifies an
already-worked intention.

`--writing-market-delivery-offers` offers each completed commission to the intended
AI act's repertoire without waiting for a random professional arrival. It is
recordable when the writing is complete and its dated license is available. The
existing perceived recording-quality read, fit refusal and shortlist cap decide
whether to retain it. New delivery candidates have an order-specific ID and do
not rekey the ordinary candidate sequence; existing candidates are not duplicated.
An order marker prevents repeated offers after
archive, pruning or reload; random professional arrivals do not give that same
act another shortlist read of its already-offered delivery.

A delivery is not forced into a recording or release. Existing session and
programme material selection remain available. The native fixture demonstrates
that a weak paid delivery is archived behind stronger perceived alternatives.

Both policies require the finite market and causal repertoire selection. Planned
assignments also require release-slate allocation. Player commissions retain
their existing controls. Neither policy changes fees, retainer terms, writer
abilities, generation distributions, capacity, delivery time, licensing duration,
copyright or publishing rights. Offering a delivery charges nothing and extends
no reservation. An unused first-recording hold can still expire and be relicensed.

## Persistence

The market save block records both policy settings. Loading with different
workflow switches is rejected before world application. Start a fresh world to
change the experiment. The order stores its plan ID and delivery-offer marker.
New fields are omitted when default, preserving the old control save shape and
allowing older market saves to resume with both new policies off. The existing
world save version and market-state version remain valid.

## Matrix and observation

Fresh worlds, seeds 1001 and 2002, 104 weeks in every arm:

| Arm | Planned assignments | Delivery offers |
| --- | --- | --- |
| Control | Off | Off |
| Planned | On | Off |
| Delivery | Off | On |
| Combined | On | On |

Every arm uses perceived repertoire selection, release slate/capacity,
anticipatory material search, chart history and material provenance. The scene's
existing team-writing settings are shared. All runs use one frozen assembly;
the matrix analyzer rejects mixed assembly hashes or incomplete windows.

The planning intervention includes both the eligibility gate and waiting for
linked writing before preparation. It is not a test of the eligibility gate
alone. The delivery intervention changes shortlist ingestion and prevents
reoffering that delivery, without changing the existing selection threshold.

Compare original-client releases within a complete 26-week delivery window,
later recycling, and complete 52-week observed-hit career cohorts. Shortlist
exposure includes offers immediately archived; observed retention is reported
separately. A higher offer count is a mechanism check, not evidence of a healthier
career. Leader grouping is retrospective, and cohort membership can change
between arms. These remain two-seed simulation screens, not general balance proof.

## Checks

Both native seeds pass the workflow fixture: feasible/infeasible plans, stale
artist contracts, cancelled intentions, existing masters, planless refusal,
delayed paid work, linked preparation waits, one-time offers, perceived rejection,
no extra delivery fee, exact field serialization and saved-policy mismatch refusal.
An active retainer/pending-work world passes full-world gzip save/load and resumed
processing after three weeks. A separate 13-week combined-policy world passes
byte-identical save/load, gzip IO and resumed programme processing.

With both switches off, the first 13 weeks reproduce the previous market control's
chart, launch-quality, label-finance and supplier rows exactly. Eleven analysis
tests pass. Both the three-week staged world and 13-week world also pass on the
corrected build.

## Rejected pilot and corrected matrix

The first pilot exposed a reentrant timing edge: an empty repertoire lookup could
order a commission from inside preparation of that very intention, after the
pending-work check had run. The pilot's encouraging delivery-use rates are not
accepted as final results. The corrected implementation excludes the active
preparation scope. The matrix analyzer now rejects early preparation of linked
work, and a regression test reproduces the same-week booking/preparation failure.

Final evidence uses `writing-workflow-v2-*` runs on the corrected frozen assembly.
The original `writing-workflow-*` pilot artifacts are retained as scratch evidence
of the failed acceptance check; they must not be mixed into the final comparison.

## Corrected results: workflow repaired, concentration target not met

All eight final runs completed 104 weeks with no audit errors, using assembly
`DE9AAF012F49D97B1866B04B10F885915443BE011BA0CCD14E02346C6024E128`.
Every planned/combined run has **zero linked plans prepared before delivery**;
the validator also checks that later cancellation cannot erase an earlier timing
violation. Every planned order has an intention link. Every completed assignment
in the delivery/combined arms has its one-time offer marker.

The corrected control reproduces the previous plain-market chart, launch-quality,
label-finance, act-year, supplier and partnership CSVs byte for byte on both seeds.
All original order columns also match. The preparation-scope correction likewise
leaves the delivery-only arm's full 104-week results unchanged. Evidence is in
`workflow-v2-noninterference.json` under the calibration directory.

Only deliveries with at least 26 weeks remaining enter the following rates.
"Linked single used song" requires the linked intention to issue that actual
commissioned composition within the window; issuing different material does not
count. Original-client release requires the commissioning label and act.

| Seed | Arm | Mature deliveries | Client release, any format | Client single release | Linked single used song | All writing fees |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | Control | 1,665 | 26.0% | 14.1% | — | $1,066,962 |
| 1001 | Planned | 396 | 46.7% | 30.6% | 17.7% | $472,773 |
| 1001 | Delivery | 1,579 | 33.5% | 25.8% | — | $1,008,959 |
| 1001 | Combined | 414 | 77.5% | 70.3% | 61.8% | $474,965 |
| 2002 | Control | 1,550 | 24.6% | 12.0% | — | $1,046,931 |
| 2002 | Planned | 420 | 48.3% | 28.3% | 15.7% | $485,250 |
| 2002 | Delivery | 1,526 | 33.4% | 25.7% | — | $988,696 |
| 2002 | Combined | 461 | 72.0% | 66.8% | 56.2% | $487,932 |

Fee totals include commissions, separate licenses and retainers, rounded here to
dollars; the JSON keeps exact totals. All commission ledgers reconcile to unique
paid orders. Fee parameters are unchanged; realised quotes still respond to the
changed demand and reputation histories.

The pooled combined arm releases 653/875 (74.6%) mature deliveries for the original
client within 26 weeks, versus 814/3,215 (25.3%) in control. Client single use rises
from 420/3,215 (13.1%) to 599/875 (68.5%). The linked single itself uses 515/875
(58.9%) of the mature combined orders, versus 136/816 (16.7%) under planning alone.
Thus this is an actual intended-single improvement, not just extra album exposure
or a higher shortlist counter.

The combined arm buys far fewer commissions: 574/629 orders versus 2,200/2,060 in
control. Commission spending falls 74.3%/69.9%, and all writing fees fall
55.5%/53.4%. Pending-commission slot occupancy falls from 57.8%/52.9% to
15.1%/16.3%; this is not total writer utilisation because speculative demos also
use writing opportunities. The rate improvement does not mean more total work:
the pooled number of original-client releases is lower, 653 versus 814, alongside
much lower purchasing volume.

Career cohorts require a complete 52-week follow-up after the first observed
non-opening Top 40 title. "Middle" means one to three further Top 40 titles.
Leaders are selected retrospectively within each run. Owner C4 is the 1961
chart-unit share, not the act slot-share measure.

| Seed | Arm | Middle / cohort | Middle share | Leading 10% act slot share | Fixed top-30 share | Owner C4 |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | Control | 42/121 | 34.7% | 53.2% | 37.5% | 52.6% |
| 1001 | Planned | 34/120 | 28.3% | 54.6% | 39.1% | 53.5% |
| 1001 | Delivery | 46/124 | 37.1% | 53.1% | 38.2% | 51.6% |
| 1001 | Combined | 44/113 | 38.9% | 55.8% | 39.4% | 55.4% |
| 2002 | Control | 40/113 | 35.4% | 55.2% | 39.9% | 49.5% |
| 2002 | Planned | 31/105 | 29.5% | 55.5% | 41.0% | 54.4% |
| 2002 | Delivery | 39/119 | 32.8% | 57.5% | 41.6% | 59.6% |
| 2002 | Combined | 33/110 | 30.0% | 57.5% | 42.9% | 60.1% |

The combined arm increases leading-act concentration by 2.6 and 2.3 percentage
points. The fixed top-30 measure also rises in both seeds, so this is not simply
percentile rounding or a different number of charting acts. Its career middle is
mixed across seeds and pooled approximately flat: 77/223 (34.5%) versus 82/234
(35.0%). Post-hit silence worsens to 11.5%/10.9%, from 8.3%/7.1% in control.

Planning alone worsens the middle in both seeds. Delivery alone improves work use
and roughly doubles client single conversion, but its career/concentration result
also fails to repeat across seeds. No arm establishes a reliable deconcentration
improvement.

## Decision

Keep both switches opt-in. **Do not adopt the combined workflow as a career-balance
fix or change the plain market defaults.** The experiment supports the planning
and delivery-routing hypothesis for wasted work, but fixing that workflow is
insufficient to broaden careers and can further favour the leading acts.

The next investigation should measure who obtains release intentions and studio/
issue opportunities, alongside the professional-material supply lost when
unplanned commissions stop funding later recycled demos. Those are hypotheses
for the remaining concentration, not conclusions established by this matrix.
Evaluate any further intervention against the fixed-leader concentration measure,
complete hit cohorts and supplier/label economics; do not infer career success
from commission fulfilment percentages alone.

Final machine-readable results are in
`SimLogs/writing-calibration/workflow-v2-results.json`. The matrix keeps one
assembly, complete 26-/52-week windows, actual material provenance, exact fee
reconciliation and the preparation-timing invariant. Broader seeds and later-era
holdouts remain necessary before any ordinary-game adoption.

## Reproduction

Run `SimTools/RunWritingMarketCalibration.ps1 -Arm market -Workflow ARM -Seeds 1001,2002 -Tag writing-workflow-v2-ARM -ConcentrationAudit`,
where ARM is `control`, `planned`, `delivery` or `combined`. Each run has isolated
user data. Manifests, logs and summaries are under `SimLogs/writing-calibration`;
native CSVs use the corresponding run prefix under `SimLogs`.

Native fixture: `SaveLoadRoundTripRunner.tscn` with the market, causal repertoire
selection, release-slate switches, both policy switches and `--writing-workflow-check`.
`analyze_writing_workflow.py RUN... --output PATH` joins the economic, provenance
and complete-cohort results and validates the shared assembly and 104-week window.
