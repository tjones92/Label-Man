# Professional writing market calibration — 2026-11-02

This pass adjusts autonomous demand and access under `--professional-writing-market`.
It does not change writing ability, fees, delivery times, contract rights, or
player commission controls.

An empty professional-demo search used to buy another assignment even when the
act already had a healthy shortlist. AI clients now:

- Buy from that search only with fewer than two active shortlist candidates.
- Use an unrecorded, unexpired paid delivery before ordering another commission.
- Wait eight weeks between commissions across all suppliers, including after a
  delivery has been recorded. Pending orders also block new commissions.
- Require eight assignment fees of uncommitted operating cash before ordering.
  This is an affordability threshold, not an additional cash charge.

Observed-hit partnerships still generate repeat business; the delivery and
interval guards also apply to those clients. Players retain the original booking
rules. Existing catalogue licensing and publishing rights remain separate.

Weekly AI repertoire processing also rotates acts across labels with a keyed
artist/week order. Fixed label and roster IDs no longer give the same clients
first access to scarce capacity and first-recording demos every week. The rotation
does not read ability, fees, tier or future song quality, and consumes no shared
random draws. Outside the market, traversal retains its original order.

## Experiment

Fresh worlds with seeds 1001 and 2002, 104 weeks per arm:

- `stage1`: `--professional-writing`, without the finite market. There is no
  explicit team flag, but `chart_manager.tscn` already enables cowriting and team
  writing in both arms. The concentration investigation corrected the previous
  report's mistaken claim that only the market arm had standing teams.
- `baseline`: the finite market before this calibration.
- `writing-cal-market`: the first demand-only screen.
- `writing-fair-market`: demand guards plus weekly access rotation.

All arms use AI repertoire shadow and perceived selection, release slate and
capacity, and anticipatory material search. No unrelated experimental profile is
added. The market-versus-baseline comparison measures this demand/access adjustment;
stage one is a broader bundle comparison. Results are seeded simulation screens,
not estimates of historical music-industry prices or causal effects on individual
acts.

Run controls or the current market with
`SimTools/RunWritingMarketCalibration.ps1 -Arm stage1|market -Seeds 1001,2002`.
The script records arguments and assembly hashes in
`SimLogs/writing-calibration/*-manifest.json`. The archived baseline assembly hash
is `D5B923F1265C4B567D1F29891B6CA586666A6A31BFEF4D2C63BFE78848D18CE1`;
`-Arm baseline` requires `-ExpectedAssemblySha256` and that assembly. The arm name
does not restore old economics. Seed-1001 baseline was invoked directly with the
same profile and assembly, before the runner was added.

`analyze_writing_market.py` reads the existing CSVs. Orders are deduplicated by ID,
supplier fee ledgers use their latest snapshots, and commission totals must
reconcile. Supplier utilisation means pending assignments divided by supplier
slot-weeks. Unused licensed songs include commissions and separately licensed
demos; they are not a commission-wastage percentage.

Hit entries count each title once at its first observed Top 40 week. Opening-chart
titles seed observation but are excluded from new entries. Each act's first
non-opening hit establishes its observed cohort; only cohorts with a complete
52-week follow-up enter repeat-career comparisons. The middle has one to three
further Top 40 titles in those 52 weeks. This is an observed-hit cohort, not proof
of the act's first-ever career hit. Leading-act slot concentration uses the leading
10% of charting acts, rounded up, across the complete 104 weeks.

Annual silent-act counts require at least 26 signed weeks in that year. Shortlist
source counts are available, including explicit unspecified/fallback rows. The
fast calibration profile does not populate the all-release song-material CSV;
full release source mix must not be inferred from the shortlist subset. Contract
counts distinguish endpoint active contracts from suppliers retained at any time.

## Demand-only screen: economics improved, career result rejected

| Seed | Measure | Baseline | Demand only |
| --- | --- | ---: | ---: |
| 1001 | Mean booked slots | 96.0% | 56.8% |
| 1001 | Commission fees | $973,347 | $579,032 |
| 1001 | Unused licensed songs at end | 414 | 215 |
| 1001 | Leading 10% act slot share | 52.3% | 53.7% |
| 1001 | Middle / complete hit cohort | 42 / 115 | 36 / 120 |
| 2002 | Mean booked slots | 95.4% | 53.2% |
| 2002 | Commission fees | $1,025,237 | $572,149 |
| 2002 | Unused licensed songs at end | 470 | 221 |
| 2002 | Leading 10% act slot share | 55.1% | 55.8% |
| 2002 | Middle / complete hit cohort | 33 / 103 | 33 / 107 |

Lower spending and fewer unused holds did not establish healthier careers.
Demand-only was therefore not accepted as the career calibration. In particular,
the middle shrank in seed 1001 and leading-act concentration rose in both seeds.
The second screen adds access rotation to address the fixed traversal's persistent
queue advantage. These screens do not establish that traversal alone explains the
original concentration problem.

## Demand plus rotating access: provisional economic pass

Eight completed 104-week runs cover two seeds in each of stage one, original
market, demand-only and demand-plus-rotation. The current implementation is the
last of these and remains behind the existing experimental market switch.

| Seed | Measure | Original market | Demand + rotation |
| --- | --- | ---: | ---: |
| 1001 | Mean booked slots | 96.0% | 57.8% |
| 1001 | Commission fees | $973,347 | $590,247 |
| 1001 | Unused licensed songs at end | 414 | 211 |
| 1001 | Repeat-recording partnerships | 235 | 258 |
| 1001 | Labels failed / observed | 130 / 699 | 117 / 689 |
| 2002 | Mean booked slots | 95.4% | 52.9% |
| 2002 | Commission fees | $1,025,237 | $566,513 |
| 2002 | Unused licensed songs at end | 470 | 216 |
| 2002 | Repeat-recording partnerships | 235 | 239 |
| 2002 | Labels failed / observed | 111 / 682 | 109 / 678 |

Commission spending falls 39.4% and 44.7%, and unused holds approximately halve.
Both runs still exercise autonomous retainers: 18 contracts remain active in each,
with 36 and 32 suppliers retained at some point. Retainer fees are $76,605 and
$62,292. Separate license fees are $400,110 and $418,125. All commission ledgers
reconcile to unique paid orders. Lower failed-label counts are descriptive;
formation, roster and market histories also change between arms.

| Seed | Arm | Complete hit cohort | No further hit | Middle | Four+ further hits | No later single | Leading 10% slot share | 1961 owner C4 |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | Stage one | 120 | 60 | 47 | 13 | 10 | 48.4% | 41.4% |
| 1001 | Original market | 115 | 59 | 42 | 14 | 15 | 52.3% | 50.6% |
| 1001 | Demand + rotation | 121 | 67 | 42 | 12 | 10 | 53.2% | 52.6% |
| 2002 | Stage one | 117 | 60 | 45 | 12 | 7 | 51.3% | 46.1% |
| 2002 | Original market | 103 | 55 | 33 | 15 | 12 | 55.1% | 57.8% |
| 2002 | Demand + rotation | 113 | 56 | 40 | 17 | 8 | 55.2% | 49.5% |

The pooled middle is 82/234 (35.0%), versus 75/218 (34.4%) in the original market
and 92/237 (38.8%) in stage one. Follow-up silence falls from 27/218 (12.4%) to
18/234 (7.7%). Thus the main career improvement is follow-up activity, not a
demonstrated broad increase in repeat hits. Different cohort sizes preclude
treating the extra seven middle acts as a causal effect on seven matched careers.

Leading-act concentration is essentially unchanged to slightly higher. Owner C4
improves in seed 2002 and worsens in seed 1001. The concentration target is **not
closed**, and this bundle should not replace stage one as an established balanced
profile. Annual silent-act shares also do not improve consistently: in 1961 they
are 825/2576 and 857/2560 under rotation, versus 820/2560 and 831/2501 originally.

The next useful calibration target is conversion of paid/held material into
recordings and later hits, split by act career tier and owner group. Add demo-level
hold/expiry and commission-to-shortlist-to-record provenance before adjusting
retainer prices, supplier abilities or career bonuses. The existing fast audit
cannot identify that causal chain. Later-era holdouts and more seeds remain
necessary before promoting the experimental market.

The subsequent [career concentration investigation](ProfessionalWritingCareerConcentrationInvestigation.md)
traces original-client delivery use and recycled commissions, corrects the team
control description, and identifies a planning/delivery-routing hypothesis for
the next controlled intervention.

## Verification

The calibrated build passes with zero warnings and errors. Both native seeds
pass checks for the new ordering guards as well as existing capacity, fees,
rights, partnerships and persistence invariants. A staged active retainer and
pending commission pass full-world save/load, gzip IO and resumed processing.
A separate 13-week world also passes save/load/resume. Stage-one craft,
specialisation, distributions and team regression checks pass. Three analysis
tests cover deduplicated cumulative ledgers, ledger mismatch rejection, opening
titles and complete follow-up windows.

After adding rotation, a further 13-week world with the full AI profile passes
byte-identical save/load, gzip IO and resumed programme processing. A fresh
stage-one 13-week prefix matches the original control's chart, launch-quality and
label-finance rows exactly. Both rotation runs complete all 104 weeks without
audit errors.

Logs and CSVs remain scratch evidence under `SimLogs/writing-calibration` and
`SimLogs/writing-cal-*` and `SimLogs/writing-fair-*`; they are not committed source artifacts.
