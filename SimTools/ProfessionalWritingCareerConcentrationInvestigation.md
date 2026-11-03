# Professional writing career concentration investigation — 2026-11-02

The strongest observed break is between **paying for a commission and using it
for the intended act**. Lower fees or more available supplier slots do not repair
that path. Most mature commissions eventually reach a release, but usually for
someone other than the client who ordered them.

This investigation changes telemetry and analysis only. It changes no fees,
abilities, capacity, contracts, licensing rights or AI decisions.

## Evidence and verification

The original 104-week stage-one and calibrated-market runs supply the two-seed
concentration decomposition. New `--writing-concentration-audit` runs repeat the
market for seeds 1001 and 2002 and stage one for seed 1001, capturing every AI
single's actual material source and weekly demo/hold/shortlist states. Every launch
with a complete 13-week conversion window has material provenance; none are
missing. A further seed-1001 control explicitly enables team writing.

All four runs completed. Their existing chart, launch-quality, label-finance and
act-year CSVs are **byte-identical** to their corresponding controls across all
104 weeks. Both market runs also exactly reproduce supplier, order and partnership
CSVs. These observer runs therefore reproduce the concentration problem rather
than creating a different economy. Evidence is in
`SimLogs/writing-calibration/provenance-noninterference.json` and
`concentration-provenance.json`; run manifests record flags and assembly hashes.

The build passes without warnings or errors. Eight analysis tests pass, including
complete follow-up windows, cumulative-ledger deduplication, preparation versus
release, opening holds, recycled licenses and deduplicated album destinations.

**Correction to the earlier report:** the shipped `chart_manager.tscn` enables
cowriting and team writing already. Stage one and the market both had standing
teams. Explicitly enabling teams produces identical control results. The earlier
claim that only the market arm had teams was incorrect; team formation is not an
explanation for this gap.

## 1. Leaders get a larger share of charting titles

Leaders are the retrospective leading 10% of charting acts, rounded up. This is a
description of outcomes, not a prospective or causal grouping.

| Seed | Arm | Leader slot share | Leader share of charting titles | Relative observed title duration | Leader slots with overlapping titles |
| --- | --- | ---: | ---: | ---: | ---: |
| 1001 | Stage one | 48.4% | 36.0% | 1.343x | 46.0% |
| 1001 | Market | 53.2% | 39.7% | 1.339x | 54.4% |
| 2002 | Stage one | 51.3% | 37.7% | 1.360x | 51.0% |
| 2002 | Market | 55.2% | 40.4% | 1.367x | 57.9% |

The exact identity is slot share = title share × relative observed duration.
Almost all the increase is the larger title share; the relative duration hardly
changes. A fixed top-30-act count confirms this is not just a changing percentile
denominator: their shares rise from 32.0% to 37.5% in seed 1001 and from 35.1% to
39.9% in seed 2002.

For nonleaders' launches with a complete 13-week follow-up, Hot 100 conversion
falls from 14.1% to 13.4% in seed 1001 and 14.6% to 12.9% in seed 2002. Top 40
conversion falls from 3.05% to 2.83% and 3.27% to 2.61%. The mechanism is therefore
not simply longer-lived leader records displacing otherwise unchanged challengers.

## 2. Professional singles become much scarcer, especially outside the leaders

Seed 1001, launches in weeks 1–91, each followed through week 104:

| Actual source | Stage-one singles | Market singles | Stage-one mean hook | Market mean hook |
| --- | ---: | ---: | ---: | ---: |
| External professional | 2,168 | 432 | .762 | .683 |
| Artist-written | 820 | 1,106 | .457 | .449 |
| Recent-hit cover | 1,262 | 1,954 | .612 | .602 |
| Standard cover | 918 | 1,329 | .692 | .669 |
| Traditional/public domain | 289 | 525 | .572 | .557 |

Professional share falls from 39.7% to 8.1%. In seed 2002 the market share is
395/5,263, or 7.5%; its stage-one source provenance was not rerun, so no equivalent
source-specific control claim is made for that seed. Both original stage-one
runs do have complete chart/launch-quality observations.

The professional singles that remain are more concentrated: in seed 1001,
retrospective leaders account for 320/2,168 (14.8%) of professional singles in
stage one but 147/432 (34.0%) in the market. Nonleaders lose 84.6% of their
professional single releases, versus 54.1% for leaders. Market seed 2002 has
149/395 (37.7%) going to its retrospective leaders.

This explains why the professional singles' observed Top 40 conversion can rise
from 12.6% to 25.9% while the broader career middle fails to improve: the surviving
professional releases are a much more selected population. That is not evidence
that commissioned songs became better.

Outside the leaders, mean released hooks fall from .652 to .581 and .659 to .585
across the two seeds. Production stays approximately .660→.658 and .652→.652.
Source substitution and within-source selection are therefore more relevant
initial targets than a general recording-production increase. These are
descriptive comparisons; genre, act standing and label histories also change.

## 3. Most commissioned work misses its intended career

Only delivered orders with at least 26 weeks remaining in the audit enter the
following table. Orders are counted once, not once per weekly snapshot.

| Outcome | Seed 1001 | Seed 2002 |
| --- | ---: | ---: |
| Mature deliveries | 1,665 | 1,550 |
| Entered intended act's shortlist within 26 weeks | 504 (30.3%) | 436 (28.1%) |
| Released by original client within 26 weeks, any format | 433 (26.0%) | 381 (24.6%) |
| Released by original act by audit end | 433 (26.0%) | 382 (24.6%) |
| Released by another act by audit end | 1,120 (67.3%) | 1,088 (70.2%) |
| Prepared for original client but still unreleased at end | 33 (2.0%) | 30 (1.9%) |

The last three rows use the endpoint, not a common 26-week outcome window.
Original-act attribution tolerates label changes; original-client attribution
requires the commissioned label too. Release includes singles and albums.
Preparation is explicitly not treated as release.

About 81% of mature orders in each seed went to acts that were NewSigning at the
first delivery snapshot. Only 249/1,355 and 219/1,257 of those deliveries reached
release for their original client by audit end. Retrospective leaders converted
137/173 and 137/201 of their orders, while nonleaders converted 296/1,492 and
245/1,349. These are selection-sensitive descriptions, but the operational gap is
large and repeated across seeds.

Complete first-recording holds tell the same story:

| Paid hold route | Seed 1001 released before hold expiry | Seed 2002 released before hold expiry |
| --- | ---: | ---: |
| Existing demo selected and licensed | 654/813 (80.4%) | 729/900 (81.0%) |
| Original commission reservation | 413/1,908 (21.6%) | 336/1,772 (19.0%) |
| Expired commission relicensed to another/new hold | 1,120/1,307 (85.7%) | 1,084/1,306 (83.0%) |

Opening holds and endpoint-incomplete holds are excluded. A commission's original
reservation is distinguished from later licenses of that same composition.
Relicensing after expiry is legitimate under the existing rights model. The
failure is the purchasing client's workflow, not duplicate sales or a license
expiry bug. High supplier recording totals were masking this distinction.

## 4. Code path consistent with that failure

`ProfessionalWritingMarket.Deliver` creates the composition and its dated license
but does not offer it directly to the act's AI repertoire. `AiRepertoireService`
can pick that paid delivery when a later probabilistic **professional arrival**
occurs. There is no guaranteed delivery-to-shortlist transition. Legacy sessions
and programme searches can also use it, so lack of shortlist entry does not prove
that a song was never heard or used.

Meanwhile, an empty professional pool with fewer than two active candidates can
purchase a commission during repertoire initialization or later arrivals. That
purchase does not require an actual planned single or a nearby studio/release
slot. The current cash, interval and unused-delivery guards constrain spending
but do not establish a production intention for that particular act. Twelve weeks
later an unused reservation expires and the material becomes general supply.

These code paths and the repeated trace support a **planning and delivery-routing
hypothesis**. They do not prove that changing the routing alone will reduce final
chart concentration; that requires a causal intervention screen.

## 5. Album check and limits

A separate 13-week seed-1001 run adds a final album-track manifest and effective
profile metadata. Of 370 delivered commissions, 10 had appeared in singles and 2
in released albums for the intended act; 358 had neither observed destination.
Those orders are young and this is not a mature failure rate. The short run rules
out treating early unused work as an established album-placement success.

The two long traces have reliable any-format release flags but were collected
before the album-track manifest was added. They do not fully classify later
album-only destinations, especially after a commission is relicensed. The new
manifest supports that classification in subsequent screens. No long-run
album-share estimate is inferred from the short case.

## Next controlled test

This test is now completed in
[ProfessionalWritingWorkflowExperiment.md](ProfessionalWritingWorkflowExperiment.md).
It repairs the intended-client workflow but does not meet the concentration
target; the combined policy remains opt-in.

Test **planned assignment plus explicit delivery offering**, holding fees, writer
abilities, contracts, capacity and first-recording rights fixed:

1. Require a real impending release/preparation need for autonomous new work,
   rather than buying solely to fill an initial repertoire gap.
2. Offer a completed commission once to its intended act's perceived shortlist
   or linked material search. Preserve imperfect judgment and the ability to
   decline a disappointing song; do not force recording or award quality bonuses.
3. Follow original-client use, expired/recycled work and later hit conversion,
   then repeat the complete-cohort and concentration comparison.

Run delivery routing alone and planned-demand gating separately before combining
them, so their effects remain distinguishable. Keep the market experimental until
this improves careers across seeds without undoing the economic gains. Further
price cuts, larger writer populations or hit/ability bonuses would obscure the
identified workflow failure before testing it.

## Reproduction

`SimTools/RunWritingMarketCalibration.ps1 -Arm market -Seeds 1001,2002 -Tag writing-trace -ConcentrationAudit`
captures long source and demo traces. Use `-Arm stage1 -Seeds 1001` for the material
control. `-Teams` explicitly enables the already-shipped team default.
`analyze_writing_concentration.py RUN... --fixed-leaders 30 --output PATH` writes
the title-duration decomposition, fully followed conversion tables, original
commission outcomes and completed-hold routes. The current observer also writes
effective settings and final album-track manifests. Scratch evidence stays under
`SimLogs/writing-calibration`, `SimLogs/writing-trace-*`,
`SimLogs/writing-teams-trace-*` and `SimLogs/writing-album-trace-*`.
