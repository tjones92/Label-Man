# Label organization costs and uses of cash

## Assessment and scope

AI labels pay an aggregate overhead charge today. They do not have explicit
staff, department budgets, income-tax settlements or shareholder distributions.
The organizational abstraction is too thin to establish that accumulated cash
is realistic, particularly for large firms. Its precise replacement cost still
needs calibration: missing detail alone does not establish a missing dollar amount.

This review implements a read-only diagnostic and proposes the next economic
model. It changes no gameplay charges or defaults. The historical review supplied
with the request is evidence and prior reasoning, rather than authority to adopt
its recommendations or earlier experimental constraints automatically.

The objective is to account for the resources and claims a firm actually carries.
Different payments solve different problems: payroll affects operating margins,
tax affects after-tax earnings, investment converts cash into productive assets,
and dividends distribute already-earned profits. They need separate accounting.

## What the code charges

`Data/AILabel.cs:GetMonthlyOverhead()` gives AI firms these monthly dollar costs,
where N is their current roster count:

| Tier | Existing monthly overhead |
|---|---:|
| Small | 150 + 30 N |
| Boutique | 250 + 30 N |
| Independent | 400 + 30 N |
| MidTier | 1,200 + 80 N |
| Major | 3,000 + 200 N |

`Systems/CompetitorManager.cs:ProcessLabelMonth()` deducts this once per month.
There is no allocation identifying salaries, rent, administration or department
capacity. A larger roster raises cost; releases, distributed client labels and
territorial operations do not directly raise this overhead schedule.

Costs already outside that overhead include recording/master production,
launch marketing, continuing campaigns when enabled, advances, licensing,
manufacturing, freight/composition costs and distribution fees. Pressing orders
consume cash or create plant payables; wholesale channels can defer collection
and incur write-offs. These are existing costs and commitments to reconcile,
not reasons to charge them again through a new overhead percentage.

Additional code findings:

- `Systems/LabelOperatorPay.cs` is an opt-in, profit-funded owner-manager payment,
  with a default cap of zero. Its protected-surplus rule is not an employee payroll.
  Staff cannot all become unpaid whenever the business makes a loss.
- Deal-backed distribution reinvestment uses 2% of positive monthly surplus.
  Self-built distribution can use 10%, subject to profitability/reach gates and
  ceilings. These are cash uses with capability gains, not ordinary payroll.
- `Systems/LabelLifecycleManager.cs:DriftAttributes()` can raise production
  quality after 1963 when cash clears six months of overhead. It checks cash
  but deducts no purchase price for that upgrade.
- `debtLevel` is declared on the label and initialized by the generator. It is
  not an amortizing bank loan with interest and payment obligations. Actual
  plant credit and distribution advances are separate existing mechanisms.
- `monthlyOverheadRate = 0.02` in `CompetitorManager` is an unused declaration;
  it does not impose an additional 2% administrative charge.
- Overhead is also used by signing reserves, status, capability, promotion and
  distribution decisions. Changing it has behavioral effects beyond cash burn.

The player's overhead has its own city/office calculation. A future general
staff model should share economic definitions, while preserving the deliberately
small founder operation and avoiding charges for departments it does not employ.

## Measured scale of current overhead

The new `audit_label_organization_costs.py` reads the existing pressing-candidate
finance streams, seeds 1001/2002. It checks complete weeks 1-260, duplicate
label/week observations, chart-year alignment and finite input values.

| Endpoint tier | Monthly overhead, median | Annualized endpoint overhead / own 1964 record gross, median | Record contribution / own gross, median |
|---|---:|---:|---:|
| Small | $240 | 3.13% | 58.96% |
| Boutique | $370 | 1.76% | 61.03% |
| Independent | $535 | 1.79% | 62.40% |
| MidTier | $2,540 | 1.56% | 63.59% |
| Major | $12,300 | 1.12% | 60.32% |

Each entry is the equal-weight mean of two run-level medians. Firms must be
active for all 52 chart weeks of 1964 and at the endpoint, with positive annual
gross. Counts by tier are 124/90/349/33/9 for seed 1001 and 115/119/339/34/7
for seed 2002. This deliberately describes established survivors, excludes late
births/closures and groups promotions by endpoint tier. It is not an industry
census or a within-firm treatment comparison.

The overhead ratio annualizes the endpoint charge; it does not reconstruct the
actual twelve monthly debits. Ratios are calculated per firm before aggregation.
The contribution field includes the existing settlement adjustments and excludes
recording, campaigns and overhead. It is not company operating or net income.
Distribution service income is retained separately in the diagnostic output.

For a concrete scale check, seeded Capitol (`label_0003`) on seed 2002 has
$22.20m of 1964 booked record gross and $12,800 monthly endpoint overhead:
$153,600 annualized, or 0.69% of record gross. That overhead supposedly supports
the organization behind a substantial national business. This is evidence for
auditing organizational cost, not a sourced estimate of Capitol's missing payroll.

Reproduce from the repository root:

```powershell
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' -I `
  SimTools/audit_label_organization_costs.py `
  SimLogs/economy-retune-p2-larger-1001-260 `
  SimLogs/economy-retune-p2-larger-2002-260 `
  --output SimLogs/label-organization-cost-context.json
```

## Recommended minimum organization model

Use a few persistent department capacities instead of individual employees.
Headcount equivalents can be fractional for part-time workers and retainers.
One founder can cover several functions; a larger label purchases separate teams.
The detailed staff mechanic can later replace these aggregates while keeping
the same payroll and capacity interfaces.

| Function | Capacity/workload driver | What the recurring budget buys | Exclude from this line |
|---|---|---|---|
| Management and administration | Organization size, contracts, accounts | Management, bookkeeping, royalty accounting, legal retainer, clerical support | Artist advances, income tax, dividends |
| A&R and production coordination | Supported acts and concurrent projects | Scouting staff, producers on salary, scheduling | Studio hire, session players, master/project costs already charged |
| Promotion and sales | Concurrent campaigns and territories directly served | Salaried promotion/sales staff and routine office activity | Release advertising, campaign purchases, independent promotion already charged |
| Distribution operations | Own-served territories, volume handled, client imprints | Orders, collections, warehouse administration | Existing distributor fees, plant invoices, freight already charged |
| Premises and equipment upkeep | Offices, departments and owned facilities | Rent, utilities, insurance, maintenance | Facility acquisition/upgrade capital spending |

At Small scale, management/A&R can be founder labor, with shared clerical help
and external legal/accounting. Boutique and Independent firms can use shared
roles and contractors. MidTiers need dedicated core departments. Majors need
department teams and regional operations. These are organizational scenarios,
not historical headcount estimates; tier is a starting profile, not the bill.

Monthly recurring cost should have the form:

```text
premises + sum(paid role equivalents * period role wage * employment-cost factor)
         + external retainers + nonduplicated recurring operating costs
```

Capacity should follow the work the business plans and performs. Use supported
acts, rolling project/campaign load, direct geographic footprint and distributed
client workload. Volume can drive handling costs, but avoid a blanket percentage
of all revenue that pretends every receipt requires identical labor. A catalog
seller and a launch-heavy label should have different needs at equal turnover.

Persist purchased capacity and adjust it at periodic business reviews. Expansion
needs cash and a credible workload; contraction takes time and can carry a
one-time restructuring cost. Existing payroll continues in a bad month. This
creates operating leverage: strong years absorb fixed staff costs comfortably,
while weak years force real retrenchment. Cutting staff should reduce supported
work/capability rather than leave the same output and advantages for free.

The new breakdown REPLACES the existing aggregate overhead. Initially preserve
the old charge as an explicitly allocated baseline, then test the additional
capacity required by actual workload. Do not add a full payroll on top of an
unexplained legacy charge. Salaried producer work also needs allocation against
project costs so the same work is not billed twice.

Period wages should come from occupational wage sources, with label headcounts
and specialist premiums kept as separate assumptions. The BLS 1960-61 survey
covers relevant clerical, accounting and professional roles; it is a broad
urban private-industry proxy, not a record-label salary survey, and excludes
supplementary benefits from its reported salary amounts. See
[BLS Bulletin 1310](https://fraser.stlouisfed.org/title/national-survey-professional-administrative-technical-clerical-pay-4570/national-survey-professional-administrative-technical-clerical-pay-winter-1960-61-497663/fulltext).
No new wage, headcount or employment-cost multiplier is adopted here.

## Other justified uses of funds

**Income tax:** accrue an annual liability from defined taxable earnings, allow
loss carryforwards and settle installments with an annual reconciliation.
Unpaid tax is a liability, not a canceled expense because cash is low. A first
version can use a documented simplified tax base, but must label its accounting
departures. Corporate and owner-operated businesses need explicit tax treatment;
a sole proprietor's owner draw is not automatically corporate payroll or a
second corporate profit tax. Consolidated subsidiaries should not pay tax twice
on the same economic earnings.

This is potentially material. For ordinary US corporations the standard federal
schedule was 30% on the first $25,000 of taxable income plus a combined marginal
rate of 52% above that threshold in 1960-63. In 1964 those rates became 22% and
50%; the upper rate became 48% in 1965. These are statutory brackets, not
effective rates or a rule for every business form. Credits, deductions, losses
and consolidated reporting matter. See the
[IRS historical corporate-rate table](https://www.irs.gov/statistics/soi-tax-stats-historical-table-24)
and the Treasury report's discussion of the 1964 changes,
[1964 Treasury annual report, printed pp. 243-244](https://fraser.stlouisfed.org/title/annual-report-secretary-treasury-state-finances-194/annual-report-secretary-treasury-state-finances-fiscal-year-ended-june-30-1964-5456/fulltext).
Tax parameters outside this reviewed period require separate research.

**Dividends/owner distributions:** pay an ownership-dependent share of cumulative,
undistributed after-tax earnings, subject to available cash after commitments.
Review quarterly, skip distributions in distress, and retain more for growth
firms than mature cash-generating firms. Owner salary belongs in operating cost;
dividends and proprietor draws belong below earnings. An outside shareholder
payment leaves the simulated label sector. A payment to another simulated label
is a transfer; it must reach that owner and cannot be counted as an external sink.
A parent cash sweep similarly requires an owner and funding policy, not deletion.

An initial payout rule can be:

```text
payment = min(policy share * positive undistributed after-tax earnings,
              positive cash available after protected commitments)
```

Deduct distributions from the undistributed-earnings balance, and apply later
losses to that balance. Do not repeatedly pay on the same old profits. Starting
cash needs an explicit contributed-capital/retained-earnings split before any
opening reserve can qualify for payout. No historically calibrated payout share
or cash buffer is established by the available evidence.

**Capital spending and upkeep:** turn the existing free production-quality drift
into a purchased upgrade or access contract. Charge for a useful capability and
its subsequent upkeep, with diminishing returns and limits. An outsourced-studio
label should not be forced to buy a plant. Existing distribution investment
needs a recorded asset/capacity benefit and must stop buying nonexistent capacity
at a ceiling. Acquisition spending is a transfer when the seller is simulated.
Depreciation affects earnings; the purchase affects cash. Do not deduct both
as cash expenditure. Purchase of an asset is not disappearance of business wealth.

**Credit costs:** introduce interest only against an actual loan/factoring
balance. Principal repayment affects cash and debt, not operating earnings.
Factor fees require an optional financing service; they are not an automatic
second discount on every wholesale receivable. This is lower priority than
organization cost, earnings measurement and tax.

## Accounting needed before calibration

Current `lastMonthlyProfit` is a management cash-surplus proxy: weekly banked
contributions, collections and service income less paid operating/project bills.
Plant payment timing, receivable timing and advances prevent interpreting it as
historical company net income. Expansion cash spending is added separately in
the lifecycle's cash-flow review. Do not silently redefine this field and thereby
change survival/promotion behavior while adding financial reporting.

Add a parallel monthly company statement and cash reconciliation:

1. Earned record revenue and distribution/publishing services, with transfers
   identified. Existing record gross alone omits some company revenue.
2. Cost of sold inventory, royalties, freight and genuine write-offs.
3. Recording/development, campaigns and organization costs under an explicit
   expense/capitalization policy; then depreciation and interest where applicable.
4. Pretax earnings, tax provision, and after-tax earnings.
5. Cash from operations, asset purchases, borrowing/principal, dividends and
   owner transfers; opening cash plus these flows must equal closing cash.

Inventory purchases create stock/payables; selling stock recognizes its cost.
Unsold stock has already consumed cash when paid: an impairment changes
earnings/assets, not a second cash charge. Recoupable artist advances need a
consistent asset, recovery and impairment policy, or an explicitly simpler
management-expense treatment; a tax model must identify that simplification.
Receivable collection is not new earned revenue. Avoid a full statutory-accounting
project, but these distinctions are necessary to compare historical net margins.

For payout planning, reserve only future cash needs: unpaid liabilities, expected
net operating cash shortfalls, and approved future projects/investments within
a defined planning horizon. Deduplicate plant obligations from the forecast.
Include a liquidity buffer as a disclosed policy assumption. Do not subtract
receivables or already-paid inventory from the bank account, and do not describe
cash divided by fixed overhead as full operating runway.

## Implementation and calibration order

1. Add the observational monthly statement, overhead categories and cash bridge.
   Persist period accumulators and settlement identifiers so save/load cannot
   omit charges or settle them twice. Preserve current decision inputs initially.
2. Replace aggregate overhead with department-capacity costs in an opt-in arm.
   Validate replacement/no double count, unavoidable payroll in loss months,
   contraction delay and purchased-capacity consequences.
3. Add tax accrual/payments and protected quarterly distributions as separate
   switches and ledger entries. Check losses, brackets, save/load and ownership
   routing. Keep the experimental owner-pay switch off in these arms.
4. Replace free production upgrades with priced, useful investment. Audit how
   changed overhead alters existing reserve and promotion gates explicitly.
5. Compare each mechanism separately, then together, on matched completed runs.
   Use at least three seeds for the next calibration decision and a later
   decade-wide holdout. Do not claim results from runs that have not occurred.

Judge annual whole-firm earnings and cash reconciliation first; then liquidity
after commitments, concentration of losses, distress among established firms,
release/signing cuts, financing uptake, organizational growth and tier changes.
Retain entrants and firms that exit in the industry results alongside survivor
tables. The existing 20-35% Small closure band is a design constraint from prior
work, not historical validation. Report sensitivity rather than retune costs
solely to pass that band or return cash to an old median.

The historical profit examples in
`EconomyCashHistoricalReview-2026-10-21.md` are useful company-level comparisons
after scope/tax adjustments; their 3.4-13.6% reported income/sales range is not a
mandatory margin band for every firm. Successful labels can retain large
reserves. The goal is that reserves result from earning, financing and ownership
choices after plausible organizational obligations, and remain exposed to real
business risks.
