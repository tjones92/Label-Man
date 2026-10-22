# Demand attention: founder costs and dormancy

October 21, 2026. Experimental continuation after the master-deal repair. The
commercial-fit bundle retains both pickup switches, drops push weighting, and keeps
`--major-offchart-singles=0.12 --major-offchart-singles-hold-units` throughout.

## Implementation

`--home-operated-micro-labels --home-label-running-cost=25` retains the existing
home-operation rule for Small, Boutique and Independent founders without paid
employees. It removes standing retainers and the office suite, with existing leases
honored until review. A $25/month allowance covers routine home business running
costs, booked through the ordinary premises account and indexed by the existing
yearly cost index. It is a **scenario allowance**, not a measured historical mean.
Paid staff still trigger the ordinary office/staff structure. Private-corporation
founder payroll remains under the existing corporate ownership rules.

`--founder-label-dormancy` requires home operation and economic exits. At monthly
review, an active founder operation may close voluntarily when all of these hold:

- A full twelve months have passed without a genuine new release. A bought or
  reverting master is not a new release; a new master shopped to another label
  counts for its originating founder as well as its releasing buyer.
- Protected free cash is below the label's reference session budget. This uses
  `GetProductionCost()`, not a random draw or a chart-hit quota. Twelve quiet months
  and this session-budget bar are disclosed design assumptions.
- Trailing monthly cash flow is not positive. A quiet, profitable catalog is a
  continuing business.
- Cash covers outstanding plant liabilities and recorded tax payables. Negative
  cash and unpaid bills remain on the existing bankruptcy path.
- There is no pending album project, master lease as buyer or seller, or unpaid
  backer principal/interest. Dormancy cannot erase those commitments.

Players, subsidiaries and paid-staff firms are excluded. Exit still uses the common
roster-release and inactive-label cleanup, with persisted `exitReason=Dormancy`.
Bankruptcy is persisted separately. Dormancy retains publishing ownership; only
bankruptcy invokes the existing catalog-succession rule.
This probe treats dormancy as a permanent end to active label operations.

The release clock, last monthly review, quiet-month count and exit cause survive
both full-world and legacy-label saves. An older save without clock state gets a
full observation grace, and loading a new save preserves the elapsed grace. Both
new probes are off by default. The prior home-only zero-cost experiment remains
available when the new running-cost argument is omitted.

## Verification and run design

Build passed with zero warnings/errors. Twenty native founder checks cover costs,
grace, monthly idempotence, funded and catalog businesses, commitments, liability
protection, player/staff/subsidiary boundaries, save migration and RNG neutrality.
The existing economic lifecycle checks pass. A 13-week full-world round trip with
the new flags passed byte identity and compressed file IO. Four attribution tests
also pass. Final regression checks pass: 54 corporate-accounting checks and 47
demand-stocking/master/base checks.

`SimTools/RunFounderSurvivalAudit.ps1` records the matrix in
`SimLogs/dfs-v1/plan.json`; each group also records exact arguments, build hash and
completion in its own `runs.json`. Every run is 265 weeks:

| Group | Seeds | Change from repaired bundle |
|---|---|---|
| dfs-v1-current1 | 1001 | None |
| dfs-v1-home | 1001 | Home operation plus $25 running allowance |
| dfs-v1-founder | 1001, 2002, 3003 | Home/$25 plus dormancy |
| dfs-v1-current-rest | 2002, 3003 | None |

This provides three matched five-year seed pairs and one overhead-only diagnostic.
The 20–35% five-year closure band is a **design judgment**, not a sourced historical
failure rate. Results must separate bankruptcy from dormancy and distinguish
starting firms from runtime births. Major singles sales are measured under the
weekly selling label, with their off-chart units added separately. Annual base
reconstruction is reported only if it reconciles exactly to recorded cumulative
copies and every recipient has initial Major tier.

Choose an unused run tag for another execution; `dfs-v1` below identifies the
completed matrix and its analysis files.

```powershell
./SimTools/RunFounderSurvivalAudit.ps1 -RunTag dfs-v1 -Weeks 265 -Seeds 1001,2002,3003
python SimTools/analyze_founder_survival.py "current=dfs-v1-current1:1001,dfs-v1-current-rest:2002,dfs-v1-current-rest:3003" "home=dfs-v1-home:1001" "founder=dfs-v1-founder:1001,dfs-v1-founder:2002,dfs-v1-founder:3003" --output SimLogs/dfs-v1/results.json
```

## Full bundle, 265 weeks

Rates below concern the labels present in week 1, grouped by their initial tier.
Means are unweighted across seeds. The first five full calendar years are 1960–64;
the run also includes five weeks of 1965. Sales shares cover all 265 weeks and use
the selling label at weekly settlement, with the actual recorded off-chart copies.

| Metric | 1001 | 2002 | 3003 | Mean |
|---|---:|---:|---:|---:|
| Starting Small labels | 231 | 230 | 232 | |
| Small closed | **24.2%** | **22.2%** | **19.8%** | **22.1%** |
| Small bankruptcy | 19.9% | 16.5% | 17.7% | 18.0% |
| Small dormancy | 4.3% | 5.7% | 2.2% | 4.0% |
| Boutique closed | 37.5% | 32.9% | 36.8% | 35.7% |
| Independent closed | 29.5% | 25.4% | 32.7% | 29.2% |
| Major singles share, live + base | **33.5%** | **28.1%** | **30.3%** | **30.6%** |
| Major live-record singles share | 25.7% | 19.4% | 21.9% | 22.3% |
| Small singles share, incl. base in denominator | 7.6% | 8.4% | 9.5% | 8.5% |
| Boutique singles share | 9.9% | 13.8% | 11.1% | 11.6% |
| Major Top 10 share, 1961 | 42.5% | 34.2% | 32.3% | 36.3% |
| Small survivor median cash | $5.9k | $7.3k | $6.1k | |
| Small survivor median monthly overhead | $52.6 | $54.0 | $51.5 | |
| Tracked quiet-year Small survivors | 14 | 22 | 17 | |

The Small mean meets the 20–35% design band. Seed 3003 is 0.2 points below its
lower edge; no coefficient was adjusted to move this individual seed across 20%.
Rounded cause percentages need not sum exactly to the rounded total.

There are 22/21/9 dormancy events across the entire worlds, including runtime
births; 10/13/5 of those are starting-Small founders. Every observed dormant exit
has nonnegative cash and at least twelve quiet months. Quiet survivors remain
when funding, income or commitments protect them; the rule does not impose a
release quota on funded businesses. The quiet-month diagnostic is unavailable
(`null`) in runs with the dormancy clock disabled.

The Major sales result remains near the ~32% residual, but it is not a final
calibration. In particular, the 1961 Top 10 share remains 36.3%, versus the historical
22.1%: the film/broadcast second-rank work is still separate and unresolved.

For seed 1001, the overhead-only run has 19.0% Small closures (all bankruptcies),
38.3% Boutique and 25.6% Independent closures, with 28.4% Major singles share.
The matched current-cost run has 44.6/42.2/25.6% closures and 33.8% Major singles
share. These whole-world contrasts include subsequent release, recruitment and
RNG-stream changes; the dormancy count is not a static addition to the overhead-only
result.

Annual off-chart totals are reconstructed exactly when possible. A promoted
MidTier label receives part of the base in the seed-1001 founder/home worlds, so
the analyzer suppresses their annual *initial-Major* total shares instead of
assigning that label's copies to the original Majors. The whole-run shares above
use each recipient's recorded cumulative copies and remain exact.

## Matched assessment

All seven audits completed successfully. The two remaining controls have 35.2%
and 45.7% Small closures. Their Major singles shares are 31.5% and 34.4%.

| Three-seed means, 265 weeks | Current costs | Founder costs + dormancy | Paired change (SE) |
|---|---:|---:|---:|
| Starting Small closed | 41.8% | **22.1%** | **-19.7 points (3.7)** |
| Small bankruptcy | 41.8% | 18.0% | -23.8 points (2.7) |
| Small dormancy | 0% | 4.0% | +4.0 points (1.0) |
| Boutique closed | 43.4% | 35.7% | -7.6 points (3.7) |
| Independent closed | 27.9% | 29.2% | +1.3 points (3.5) |
| Major singles share, live + base | **33.2%** | **30.6%** | **-2.6 points (1.2)** |
| Major live-record singles share | 25.2% | 22.3% | -2.8 points (1.4) |
| Small singles share | 7.8% | 8.5% | +0.7 points (1.1) |
| Boutique singles share | 10.7% | 11.6% | +0.9 points (.9) |
| Live + off-chart single units | 866.3M | 865.8M | -0.05% |
| Major Top 10 share, 1961 | 34.9% | 36.3% | +1.4 points (3.4) |

Major sales decline in every pair: approximately -0.3/-3.4/-4.1 points. Lower
founder costs are not neutral to the larger labels' market. The resulting 30.6%
mean is 1.4 points below the ~32% residual, with a 28.1–33.5% seed range. Leave the
.12 base unchanged for now; recheck it after the film/broadcast second-rank work
rather than compensating before that market structure is repaired.

**Decision:** carry the $25 founder-cost and twelve-month dormancy probes into the
experimental candidate. They substantially reduce Small bankruptcies, expose a
separate solvent exit, and meet the Small closure band on the mean. This is not a
claim that every seed or every tier is inside the band: Small seed 3003 is 19.8%,
and Boutique closures still average 35.7%. Independent closures are effectively
unchanged. No shipping defaults are changed.

The cost comparison holds commercial fit, launch stocking, repaired master deals,
backers and the .12 held base fixed. It does not measure the fit's excess mortality
against a separately re-costed no-fit control. Subsequent release/recruitment and
global RNG-stream effects are included in the paired whole-world differences.

The current-cost seed-1001 run reproduces all first-105-week sales rows from the
prior `dma-capacity` run exactly. Final output is saved in
`SimLogs/dfs-v1/results.json`, `screen.txt` and `paired.txt`. Each completed audit
passes the existing completion/error checks, weekly unit reconciliation and the
new exit-cause/solvency checks. Chart-week sales share and #1 units remain diagnostics;
the ~88% and ~150k targets remain withdrawn.
