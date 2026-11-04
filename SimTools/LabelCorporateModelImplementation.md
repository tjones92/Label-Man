# 1960s label company model: implementation and calibration record

## Status

The calibrated AI company model is enabled by default: department payroll,
premises/retainers, commercial channel operations, record excise, income tax,
ownership distributions and paid capital improvements share a persisted journal
and monthly statement. The full bundle includes the existing corrected advance
recovery, royalty tiers, mechanical publishing and period prices. The model is
AI-only while the player retains the existing founder office and manually
purchased services. `--no-label-corporate-model` retains the legacy economy.

The final adoption results and limitations are recorded at the end of this
document. Earlier pilot results below are historical experiments. The user
authorized default adoption after checks pass, with the 20–35% starting-Small
closure band applied to the equal-weight seed average and outliers disclosed.

Before implementation, checkpoint `ca49f27` was pushed to
`tjones92/Label-Man`, branch `media-atmosphere`, at the user's request.

## Historical interpretation

A record company is more than an imprint and a roster. It combines creative
selection, project administration, contract and royalty obligations, promotion,
sales, and some combination of owned distribution and outside wholesalers.
Those functions can be performed by a founder, shared employees, outside
professionals, regional branches, or departments of a parent company.

Contemporary reporting supports this functional distinction. Cash Box's July 18,
1964 Capitol report names international merchandising, import A&R, import sales,
and international administration positions. This supports differentiated
functions; it does not provide a representative headcount or payroll budget.
Source: [Cash Box, July 18, 1964](https://retrocdn.net/images/5/54/CashBox_US_1964-07-18.pdf),
[searchable transcription](https://device.report/m/cda1d374b3cc640aa09678d88123608240213d979aa424bc0dbc1183822dba6e).

The operating imprint remains distinct from the company owning it. The existing
simulation already has `AILabel.ownerLabelId` for continuing subsidiaries, which
this implementation reuses. Signing a distribution deal does not set equity
ownership. External corporate parents and public shareholders are represented
as outside owners, without inventing bank accounts for unsimulated conglomerates.

Broad occupational wage evidence supplies scale checks. The 1960-61 BLS survey
reported accountant monthly averages from $478 to $879 by level, including $600
for level III. Its professional and clerical categories are proxies for related
work, not record-industry salary quotes. Specialist compensation, staffing
levels, and employment burden below are model assumptions. Source:
[BLS Bulletin 1310](https://fraser.stlouisfed.org/title/national-survey-professional-administrative-technical-clerical-pay-4570/national-survey-professional-administrative-technical-clerical-pay-winter-1960-61-497663/fulltext).

The existing historical cash review supplies company profit and dividend
examples, with important scope differences. No source in this work establishes
a universal record-label cash cap, target margin, staffing ratio, or payout rate.

## Corporate structure and department responsibilities

`LabelCompanyState` persists nine functional budgets. Employee equivalents can
be fractional, representing shared roles and part-time help. These are aggregate
capacity allocations, not nine automatically hired department heads.

| Function | Operational responsibility | Cost boundary | Monthly base wage proxy |
|---|---|---|---:|
| Executive | General management, commercial planning, supervision | Salaried management; outside-owner dividends separate | $1,250 |
| Clerical | Correspondence, calls, filing, office assistance | Office labor; postage/utilities in premises allowance | $325 |
| Finance/royalties | Books, statements, artist and distributor accounting | Administration; royalties themselves remain record costs | $600 |
| Business affairs | Contract administration and legal coordination | Employee/retainer; artist advances separate | $1,000 |
| A&R | Finding/reviewing talent and managing the roster | Staff time; signing money and recording sessions separate | $800 |
| Production coordination | Scheduling sessions and release preparation | Coordinators; hired studios, musicians and masters separate | $550 |
| Promotion/publicity | Campaign planning, press/radio coordination | Salaries; bought campaign tranches and advertising separate | $650 |
| Sales/distribution | Orders, house/client relationships and collections | Internal administration; existing outside distribution fees and freight separate | $500 |
| International | Licensing/import coordination and foreign business administration | Support function; no new foreign revenue is invented | $750 |

All values in the last column are scenario wage proxies. They are not asserted
as measured pay for the corresponding historical record-company jobs. Employment
cost is wage times 1.12; the 12% allowance combines employer costs and benefits
without pretending to be a specific payroll-tax rate. Wages grow 3% annually
from 1960 through 1974, then hold the 1974 wage index. That extrapolation is a
simulation assumption; later-period operation needs its own research.

### Workload, headcount and founder labor

Requirements use roster count, planned/observed monthly releases, direct
distribution reach and regions, distributed client imprints, and operating
scale. Corporate scale parameters are Major 4, MidTier 1.8, Independent .65,
Boutique .4, Small .12. These are departmental workload coefficients, not FTE
totals, cash multipliers or historical estimates.

For corporate organizations, required equivalents before quarter-role rounding
are defined in `LabelCorporateModel.SetRequiredStaff`. In that calculation:

```text
A = supported acts
P = max(observed monthly release rate, min(A / 3, planned releases per month))
S = organization scale coefficient
C = distributed client labels
R = direct distribution reach
G = directly supported region count, with a minimum of one

Executive             max(.25, S + A/40)
Clerical              max(.10, 1.5*S + A/12 + C/4)
Finance/royalties     .7*S + A/16 + C/3
Business affairs      max(0, .4*S + A/40 - .2)
A&R                   A/6 + P/2
Production            P/2 + .4*S
Promotion/publicity   .8*P + S
Sales/distribution    R*(.3*G + 2*S) + .6*C
International         .5*S for MidTier/Major; otherwise zero
```

Owner-managed Small, Boutique and Independent businesses instead start from a
smaller hours-based work plan, including private corporations:
management .12 equivalents, clerical .025+A/80+P/20, finance .025+A/60+P/30,
A&R A/24+P/10, production P/8, promotion P/5, and sales
R*(.08*G+.1*P)+.15*C. Legal help is retained externally. A single founder's
one equivalent/160-hour budget is allocated across these functions, once in
total. Only the remaining work requires hired employees. Private corporations
pay the working founder at the same wage proxies; sole operators draw earnings.
MidTier/Major firms use corporate requirements regardless of owner management.
The allocation is saved by department and visible in the department CSV.

This distinction matters: the first smoke profile implicitly hired multiple
employees at tiny founder firms and closed 52 of 231 starting Small labels
within thirteen weeks on seed 1001. That profile was rejected for its implausible
organizational assumption. The revision supplies founder labor rather than
tuning a global cost multiplier to recover a desired cash median.

Paid requirements are rounded up to quarter-role increments. Initial firms have
the staff required by their initial operating profile. Later hiring occurs at
quarterly reviews, capped at 25% of the existing department or .25 equivalents
per review, and requires cash beyond operating commitments plus three additional
months of the existing payroll quote. Staff are not waived in losing months.

Contraction requires excess capacity at two consecutive quarterly reviews.
Each cut is limited to 25% or .25 equivalents and costs one month's base wage
for the removed capacity. These timing and severance rules are assumptions.

Coverage includes both paid staff and supplied founder time. If an organization
cannot staff its work, the existing weekly release-capacity calculation receives
that coverage factor, bounded at .25 to preserve retrenchment activity. Individual
employee skill, morale, hiring markets and department-specific bottlenecks remain
future staff mechanics; no employee is simulated twice in anticipation of them.

### Premises, professional retainers and maintenance

Premises reuse the existing city office cost ladder, which is itself a game
assumption rather than a corporate real-estate survey. A suite supports five
working equivalents, including the founder. Larger firms maintain regional-office
commitments based on their tier and direct reach. These offices represent aggregate branch support,
not researched historical addresses or literal company branch counts.

Leases renew annually. Expansion can acquire more space before renewal;
headcount cuts do not instantly cancel existing suites or branches. Utilities,
insurance and routine administration add $12 per paid equivalent per month,
including the salaried founder of a private corporation.
Production/distribution assets add monthly maintenance equal to .25% of original
cost while their modeled book value remains positive. The wage index also
indexes the office operating allowance, excluding asset maintenance.

External legal support costs $35 monthly when business-affairs staffing is below
one equivalent; outside accounting support costs $20 below one finance
equivalent. Both use the wage index. These are disclosed retainer assumptions.

Combined staff/premises mode replaces the whole legacy overhead charge. Separate
research arms replace a 70% staff / 30% premises allocation of legacy overhead;
that allocation exists for ablation, not as a historical assertion. The journal
allocates the one actual monthly overhead debit between payroll, retainers and
premises. Existing master production, campaign, manufacturing, royalty, freight
and distribution charges retain their separate cost boundaries.

## Company accounts

`LabelCompanyAccounts.ChangeCash` is the common AI cash-mutation boundary.
It records nominal transactions and float-account rounding separately. The
disabled path performs the existing float addition/subtraction and attaches no
company state. Ledger-only mode is observational and preserves existing
survival/promotion decision inputs.

The monthly statement reports earned record gross, service/publishing receipts,
cost of sales and write-offs, recording, campaigns, advances, licensing, other
operating spending, payroll, premises, retainers, restructuring, depreciation,
pretax earnings, tax provision, after-tax earnings and cash-flow categories.

Accounting policy is deliberately explicit:

- Wholesale receivables and plant payables accrue; collection and payment do
  not create a second revenue or expense.
- Pressed copies enter weighted-cost inventory. Sold copies consume that cost.
  Existing setup/shipping charges are expensed rather than capitalized here.
- Unsold inventory is impaired when the existing runtime record retires.
  Retirement does not debit cash again.
- Opening catalog stock is introduced as opening contributed assets as the
  existing pressing book discovers it. It is excluded from new profit and payouts.
- Recording and recoupable artist advances are expensed when paid for these
  management accounts. Existing recoupment reduces later cash royalty expense.
  This conservative simplification differs from a full master/advance asset
  and impairment accounting system and is also used by the tax proxy.
- Distribution advances, recoveries and balance buyouts are financing cash
  flows. Forgiven debt creates a borrower adjustment and a lender write-off;
  neither creates or removes cash at forgiveness.
- Capital purchases create depreciable assets. Depreciation has no second
  cash debit. Existing distribution investment also receives an asset entry.
- Owner transfers are excluded from operating/taxable earnings. A parent can
  pass received subsidiary distributions on through its distribution pool.

Every month must satisfy both a categorized cash bridge and an independently
expressed company P&L identity. An unexplained cash residual over one cent raises
an error; no balancing income or expense is invented. Accounting uses double
precision around the existing float bank account and exposes its rounding.

Company state includes the open journal, opening working-capital balances,
inventory lots, assets, tax-loss vintages, capacities, lease dates, earnings
pool, last closed period and last paid quarter. The full-world save serializes
it with the label. Old saves open accounts prospectively with zero distributable
opening profit; they do not backfill taxes or declare starting cash to be earnings.

`lastMonthlyProfit` retains its previous management cash-surplus meaning. In
economic model arms, liquidity review additionally uses the reconciled monthly
cash movement excluding financing and owner transfers. The ledger-only arm
does not change that decision calculation.

## Income tax

The ordinary federal corporate schedule uses a $25,000 annual threshold:

| Calendar years | First $25,000 | Marginal rate above $25,000 |
|---|---:|---:|
| 1960-63 | 30% | 52% |
| 1964 | 22% | 50% |
| 1965-67 | 22% | 48% |
| 1968-69 | 24.2% | 52.8% |
| 1970 | 22.55% | 49.2% |
| 1971-74 | 22% | 48% |

The late-decade rows use an annual calendar-year surcharge approximation,
without fiscal-year proration. Primary historical table:
[IRS Table 24](https://www.irs.gov/statistics/soi-tax-stats-historical-table-24).
The 1964 changes are also described in the
[Treasury's 1964 annual report](https://fraser.stlouisfed.org/title/annual-report-secretary-treasury-state-finances-194/annual-report-secretary-treasury-state-finances-fiscal-year-ended-june-30-1964-5456/fulltext).
The code's base 22%/48% fallback outside the supported later period is an
extrapolation, not a claim about subsequent US law.

The base is year-to-date management pretax earnings less eligible forward
losses. Losses carry forward for five years, oldest first; there is no carryback.
Provisions can reverse after later losses. Positive liabilities settle quarterly;
overpaid current-year installments refund at the December true-up. Unpaid tax
is not forgiven because a firm lacks cash. New investment decisions protect
an estimate of the current month's unprovided tax as well as recorded liabilities.

Corporate forms are private corporations, public corporations and corporate
divisions; Small founders initially use an owner-operated pass-through form.
These assignments are scenario profiles, not company-specific researched legal
forms. Forms persist as businesses grow; an actual subsidiary absorption sets
the corporate-division form. Owner-operated income has no additional corporate
tax here; owners' household tax lies outside the simulated company sector.

State/local income taxes, investment credits, statutory master amortization,
personal income tax, carrybacks, consolidated group loss relief, deferred tax
and detailed corporate return rules are omitted. Subsidiaries pay on their own
operating earnings; parent receipts are excluded from the parent's taxable
operating earnings, avoiding a second charge on those same distributions.

## Distributions and liquidity policy

At quarterly closes, eligible firms distribute a fraction of their positive
undistributed after-tax pool: owner-operated .50, private .20, public .35,
corporate division .65. These are policy scenarios, not measured historical
payout ratios. A positive recent cash flow and a positive latest after-tax
statement are required. Losses reduce the pool; a negative balance must be
earned back. Opening cash never creates eligibility.

Payments cannot consume protected cash: three months of organizational overhead,
two months of the greater of recent project spending or planned production,
one recent month of future pressing needs, outstanding plant bills, tax
liabilities/unprovided tax, and two months of recent operating cash shortfall.
Project spending uses a three-month smoothing estimate. These buffers are
budget assumptions and remain visible in the statement.

Receivables and paid inventory are not subtracted from cash. Existing plant
payables are included once as unpaid obligations; future pressing allowance
finances future activity. No total-cash clamp or old-median target is used.

All companies close before payouts run. Transfers occur on the first day of
the next period, removing list-order dependence from recipient accounting.
Every company's proposed payout is calculated before any transfers execute;
incoming subsidiary cash cannot enlarge a parent's payout merely because its
child appeared earlier in the list.
Missing, inactive, player-owned or cyclic simulated owners cause the payout to
be skipped rather than silently treated as outside shareholders. An external
payment is a sector cash outflow; a simulated parent receives the same nominal
amount and the sector transaction nets to zero, aside from reported float rounding.

## Capital spending

The existing post-1963 free production-quality drift is replaced in the capital
arm. A funded upgrade can add .01 quality once per quarter, capped at .95.
Baseline 1960-dollar upgrade costs are $1,500 Small, $4,000 Boutique, $6,000
Independent, $25,000 MidTier and $100,000 Major, indexed with wages. Assets
depreciate over 84 months. The small-firm asset represents equipment/access
improvement rather than mandatory ownership of an entire plant or studio.
These tariffs, lifetimes and capability increments are explicit assumptions.

Distribution investments retain the established reach rules, create 120-month
assets, and add maintenance. Deal-backed investment is limited to remaining
owned-reach capacity and available cash after commitments. Self-built growth
keeps its existing .75 owned-reach ceiling and other gates, with a liquidity
check in the capital arm. Existing investment has no new cash charge from the
ledger; it gains an accounting asset entry only.
Asset upkeep is carried in the premises budget, so the capital-only ablation
tests purchase/depreciation while the combined model also pays that upkeep.

Initial production quality and networks remain opening operating capabilities;
no fictional historical purchase price is charged to their opening bank balance.
This first version does not simulate facility resale, collateralized lending,
full subsidiary consolidation, plant ownership/manufacturing margins, or a
liquidating bankruptcy estate. Those omissions remain visible instead of being
replaced by a generic revenue haircut.

## Running and reviewing

The ordinary game enables the full bundle automatically. Dealer-price pressing
and plant credit are required; corrected distribution advance recovery is
selected automatically by the full/default bundle. Isolated company research
arms require `--distribution-advance-recoupment` explicitly. The experimental
owner-pay parameter must remain zero because this model accounts for management
compensation separately.

Use `--label-corporate-model` explicitly for the full bundle,
`--no-label-corporate-model` for the legacy economy, or select independently:

```text
--label-company-ledger
--label-staff-costs
--label-premises-costs
--label-income-tax
--label-distributions
--label-capital-investment
--label-commercial-costs
--label-record-excise
```

`--label-payroll-scale=` provides an explicit .1-3.0 research sensitivity
multiplier, default 1.0. No fitted multiplier has been adopted.

`RunCorporateEconomyAudit.ps1` records assembly hash, seed, duration and complete
arguments, refuses existing run tags, and checks process exit/completion/error
logs. New manifests record the planned seed/mode matrix; the analyzer rejects
unfinished matrices. Its default `audit` profile retains the broader research stack and the old
1.5-forecast/twelve-week pressing scenario for matched research comparisons.
For the ordinary game (.5 forecast/three-week repress), use `-Profile game` and
`-Modes @('default')`; this supplies no corporate-enable or research economy
flags. A complete five-year adoption check is:

```powershell
dotnet build 'Label Man.sln' --no-restore
./SimTools/RunCorporateEconomyAudit.ps1 -RunTag corp-default `
  -Seeds @(1001,2002,3003,4004,5005) -Modes @('default') -Profile game -Weeks 265
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' -I `
  SimTools/analyze_corporate_economy.py --manifest SimLogs/corp-default/runs.json `
  --output SimLogs/corp-default/review.json
```

Monthly output is `*-company-monthly.csv`, staffing is
`*-company-departments.csv`, and actual cash transactions are
`*-company-cash.csv`. December closes at the January boundary: run 53 weeks
for a full first accounting year and 265 weeks to observe all five closes of
1960-64 plus the following January's activity. Do not call week-260 cash a
post-December-tax bank balance.

The analyzer rejects duplicated periods and failed/incomplete runs, checks
cash reconciliation and the direct P&L identity, and reports completed firm
years separately from partial years. Full-year rows include firms that later
close, avoiding a survivor-only earnings table. Current-tier endpoint cash and
starting-Small closures remain separate diagnostics.

## Original pilot verification record (before margin calibration)

- Final build passed. Native company checks passed 40 financial,
  organization, ownership, investment, persistence and RNG probes.
- The model-off 13-week seed-1001 run matched all 92 existing non-timing CSVs
  from the pre-implementation reference byte for byte.
- The observational ledger's 13-week run matched those same 92 economy CSVs.
- The first combined smoke run completed with every monthly cash journal
  reconciling. Its micro-label staffing profile was rejected and revised as
  described above; its economic results are not adopted calibration evidence.
- The all-mechanism full-world round trip passed at week eight, preserving
  600 labels and the open company accounts exactly. The serialized world was
  240,422,997 bytes before compression.
- All six 53-week runs completed: ledger and combined model on seeds
  1001/2002/3003. Their 44,433 monthly statements passed cash and P&L checks.
- Three 265-week combined runs completed with 152,405 monthly statements.
  Every journal reconciled; the largest absolute residual was below
  $0.000000001. All five accounting years, 1960-64, were observed.
- A final guard against replaying an already-closed monthly handler was
  verified by the native suite after the long runs. It does not change normal
  once-per-month processing. All eight final thirteen-week arms passed: off,
  ledger, staff, premises, tax, distributions, capital and combined.
  The final off/ledger builds preserve the 92-file compatibility controls.
  The final combined run matches the long run's first thirteen weeks exactly
  for finance (7,807 rows), company statements (1,800), department observations
  (16,200), and cash transactions (21,789). Evidence is recorded in
  `SimLogs/corp-final-arms/runs.json` and `SimLogs/corp-final-compatibility.json`.

### Original annual and five-year pilot results

The 53-week comparison uses equal-weight means of run-level active-tier cash
medians, at the January boundary after the December tax close:

| Current tier | Ledger-only control | Combined model |
|---|---:|---:|
| Small | $11,130 | $6,240 |
| Boutique | $71,218 | $36,155 |
| Independent | $114,496 | $27,519 |
| MidTier | $910,234 | $214,132 |
| Major | $5,679,330 | $956,271 |

The combined model's 265-week endpoints are January 29, 1965. These cash
values must not be compared as if they were the old review's week-260
pre-December-settlement balances:

| Seed | Starting-Small closed, week 260 | Starting-Small closed, week 265 | Studio capital paid through week 265 |
|---|---:|---:|---:|
| 1001 | 33.33% | 34.20% | $8,789,098 |
| 2002 | 30.00% | 31.74% | $7,735,059 |
| 3003 | 32.76% | 34.05% | $7,720,428 |

All three week-260 closure results fall within the pre-existing 20-35% design
band. That is a gameplay constraint, not evidence that mortality or balances
are historically validated. The initial founder revision was based on the
resource assumption, not tuning against this band.

Mean active-tier cash medians at week 265 are $2,227 Small, $65,027 Boutique,
$20,050 Independent, $289,908 MidTier and $1,753,985 Major. These medians are
composition-sensitive: promotions, closures and entrants change the tiers.

The 1964 company after-tax margin medians, averaged across seeds, remain
approximately 10.4% Boutique, 21.0% Independent, 19.0% MidTier and 28.1% Major.
The Small value is 24.9%, but mixes persistent owner-operated businesses whose
household tax is outside the company accounts; it is not directly comparable
to a corporation's after-tax shareholder return. Annual margin calculations
require positive revenue. Firm-year summaries exclude late/partial histories
and entirely inactive zero-activity years, while retaining firms that close
during an observed operating year.

The first year's model had many Boutique/Independent losses; by 1964 those
tiers' reported earnings had recovered substantially, with changing survivor
composition. Major margins remain high against the supplied company examples.
The next calibration question is the scale and scope of major-company costs,
development/release risk, and record turnover—not another arbitrary dividend
increase, cash haircut, or uniform demand reduction. At that checkpoint the
corporate model remained opt-in pending margin calibration. The follow-up below
supersedes that rollout decision; these figures are retained as experimental
history.

Reproducible evidence:

```text
SimLogs/corp-pilot-v2/runs.json
SimLogs/corp-pilot-v2/review.json
SimLogs/corp-fiveyear-v3/runs.json
SimLogs/corp-fiveyear-v3/review.json
SimLogs/company-native-check-complete.log
SimLogs/company-world-roundtrip.log
SimLogs/corp-off-parity.json
SimLogs/corp-final-arms/runs.json
SimLogs/corp-final-arms/review.json
SimLogs/corp-final-compatibility.json
```


## Margin calibration follow-up (October 21, 2026)

The earlier 28.1% Major margin was whole-company after-tax earnings, not a
record contribution mislabeled as net income. The reconciliation check passed;
the missing boundary was the cost of operating commercial channels at volume.
The original department model priced office staff and network reach but not
ongoing warehouse/order/field-selling services or trade merchandising on the
copies moving through that network. An outside distributor earned its service
fee with no corresponding volume service bill. Federal record excise was also
absent.

The revised accounts expose three additional cash/P&L categories:

- `distributionOperations`: on self-distributed dealer gross, 85% of the existing
  25% channel tariff. A simulated distributor also spends 85% of fees earned
  servicing another label. Copies carried by that house do not incur the
  client's owned-network service bill. For a live distribution deal, the
  existing granted-region carried share determines which copies are outsourced;
  otherwise the existing owned-reach proxy divides direct and outside service.
- `tradeSupport`: 5% of self-distributed dealer gross for continuing dealer
  merchandising and catalog support, separate from bought launch/radio campaign
  tranches. On carried copies, the outside house funds that support from its
  service-fee operating budget; the client does not pay that same support again.
- `recordExcise`: 10% of an assumed factory value equal to 75% of dealer gross,
  before June 22, 1965; zero on or after that date. It applies to owner-operated
  sellers as well as corporations and to both singles and albums. It is paid
  with the sale settlement and expensed before the income-tax calculation.

The **85%, 5% and 75% coefficients are explicit simulation assumptions**. They
are not measured industry expense ratios or a statutory constructive-price
ruling. Services are represented as outside operating purchases; their workers
are not added again to the company's office payroll. These allowances exclude
pressing, packaging, existing freight, royalties, record-specific campaigns and
leased office costs. They do not change demand, royalties, plant tariffs or
opening bank balances. No profit or cash cap is applied.

The historical evidence is more specific about excise. Public Law 89-44,
sections 204 and 701(a)(1), repealed the phonograph/record manufacturers' tax for
articles sold beginning the day after enactment (June 22, 1965):
[statute, pp. 140 and 155](https://www.govinfo.gov/content/pkg/STATUTE-79/pdf/STATUTE-79-Pg136.pdf).
The Copyright Royalty Tribunal's later review records a 10% levy and a roughly
$1.90 wholesale value on a $3.98 LP, and discusses the 1965 repeal. That factory
value is about three quarters of this game's $2.56 dealer receipt; the game
uses this as a proxy rather than asserting identical channels for every sale.
[Federal Register, February 3, 1981, pp. 10481 and 10484](https://archives.federalregister.gov/issue_slice/1981/2/3/10465-10487.pdf).

Cash is debited once through the company journal. Record receipts and gross are
not rewritten as balancing entries. The new categories reduce pretax earnings,
and income tax then adjusts to the resulting profit. Record lifetime memory
and both single/LP priors include the new direct costs. Forecasts use owned
reach as the prospective direct-channel share; actual deal settlements use the
observed granted-region share. The monthly statement carries each expense
separately, and the analyzer independently validates the expanded P&L identity.

The liquidity reserve now includes two months of smoothed commercial spending,
so dividends and production/distribution capital cannot consume funds committed
to the newly modeled recurring bills. This is a reserve, not another cash debit.

### Rejected first candidate

The first 265-week seed-1001 candidate combined these costs with the old research
pressing commitment of 1.5 times forecast lifetime sales and twelve-week
represses. It reduced the 1964 Major margin to 11.43%, but starting-Small closures
rose to 68.40% at week 260. That fails the 20-35% design guard and is rejected.
The remaining seeds were stopped; their partial data are not validation evidence.
`SimLogs/corp-margin-fiveyear-v1/runs.json` retains the failed experiment.

The next comparison uses the ordinary game's existing .5 forecast / three-week
inventory commitments, and includes the commercial cash reserve. The previous
1.5/12 pressing scenario was a research candidate, not the normal game's native
inventory policy. The actual no-corporate-flag game profile is validated
separately from the historical audit's extra release/royalty/pricing switches.


### Default-rollout corrections and later candidates

The ordinary game originally did not select the audit's previously corrected
royalty tiers, mechanical-publishing settlement or period price ladder. A
corporate-only default would therefore have used different underlying unit
accounts from the reviewed economy. The full/default corporate bundle now also
enables the existing B3 loan-recovery correction, B4 royalty tiers, B6 mechanical
publishing correction and C2 period prices. These are existing financial rules,
not a new sales-volume multiplier. They can still be selected independently in
legacy/research arms. Individual company mechanism flags select only their own
arm and require the shared settlement prerequisites explicitly.

The production-capital decision also requires a positive recent cash flow and
profitable latest company statement, with the purchase limited to twelve months
of recent monthly surplus. This is a disclosed affordability policy, not an
estimated investment return. Cash-rich firms running cash losses no longer buy
an automatic discretionary quality increment. The native suite tests this rule
separately from the existing protected-cash check.

The .5/3 audit-stack candidate (`corp-margin-native-v2`) produced a 13.84% Major
margin on seed 1001, but 38.96% starting-Small closures; it was not accepted as the
rollout configuration. The subsequent actual-default candidate
(`corp-margin-game-v3`) completed all three seeds, but seed 2002's 35.22% closures
missed the 35% guard by one firm. Its all-channel trade-support charge was
rejected: outside-house servicing already has an operating bill funded from
that house's fee. The final candidate limits the client's trade-support bill to
its self-distributed share, in both forecasts and actual cash charges.

Native financial verification for this final boundary is recorded in
`SimLogs/corp-margin-v4-native-console.log` (49 company checks) and
`SimLogs/corp-margin-v4-settlement-console.log` (49 existing B/C settlement
checks). Full-world byte identity and compressed Godot file I/O passed at week
eight with 600 labels and the open accounts. The roundtrip harness holds several
full world copies: an initial 2.5 GiB managed-heap cap exhausted memory during
its extra compression verification; the successful check used 3.5 GiB. This was
a test-memory limit, not an account reconciliation or serialization mismatch.
Final save evidence is `SimLogs/corp-margin-v4-roundtrip-console.log`.

Ownership-form caveat: owner-operated firms retain their form after promotion.
Consequently, household-tax omissions can affect tier summaries outside Small
as well. The Major benchmark here should not be generalized into a universal
after-tax target for every tier or legal form.


### Organization and commitment corrections before adoption

The all-tier cohort review exposed a pre-existing organization error. In the old
seed-1001 five-year pilot, 109 of 128 starting Boutiques closed. The later
margin-only candidate closed 122. Those firms had been assigned departmental
staffing because they were incorporated, even when they operated at the same
small scale as an unincorporated founder label.

Organization now follows operating scale separately from tax form. Owner-managed
Small, Boutique and Independent businesses use the existing hours-based work
plan. A single founder/director has at most one equivalent across all functions;
additional work still requires paid employees. A private corporation **pays**
for its founder/director's allocated work at the same departmental wage proxies,
employment allowance and wage index. That cash expense is `ownerSalary`, split
from the one overhead debit; hired staff remain `payroll`. Sole operators keep
their existing earnings-draw treatment. Corporate income tax remains applicable
to private corporations. MidTier/Major organizations use the corporate workload
plan even when owner-managed; a founder does not replace an entire large team.
Office space includes the working founder's occupancy. These are organization
scenario profiles, not claims about the legal form or staffing of named firms.

The native suite tests incorporated founder compensation, one-time overhead
allocation, retained corporate tax, and large-company staffing. The monthly
statement now exposes the founder equivalent, and the analyzer rejects budgets
over one. Existing staff contractions still require two quarterly reviews and
severance; existing leases do not disappear when requirements fall.

The liquidity review also found that signing and release commitments used the
legacy reserve while dividends and investments protected tax/service cash.
`TaxAndServiceReserve` now shares the same current tax liability, estimated
unprovided tax and two months of smoothed service spending across those
boundaries. Album-drop and follow-up campaign spending preserve those funds as
well. Plant payables remain at their original boundary and are not added twice.
This holds cash for bills; it creates no new expense and forgives no liability.

The final organization build passed 54 company checks, 49 existing settlement
checks, and a byte-identical full-world roundtrip including compressed file I/O
at week eight (600 labels). Logs are `SimLogs/corp-margin-v6-*-console.log`.
The final five-year actual-default manifest is
`SimLogs/corp-margin-game-v6/runs.json`. The analyzer reports all starting-tier
closures as well as current-tier earnings, so a healthy survivor median cannot
hide the disappearance of a starting cohort.

### Accepted default calibration: five completed seeds

The same frozen assembly completed 265 uninterrupted weeks on seeds 1001,
2002, 3003, 4004 and 5005. The last two were holdouts after the three-seed
review; no behavior changed between them. Each run used the ordinary game
profile, with no corporate-enable flag and no extra research economy flags.
Its only observer flags were `--calibration --small-label-finance`. Native
pressing commitments remained .5 forecast / three-week repress. The assembly
SHA-256 was
`413686B78C8AB0A92FA2B93453AA1AE02453E64C7497BB994DC44B41DE619CCE`.

| Seed | Starting-Small closed, week 260 | Starting-Small closed, week 265 | 1964 Major median company after-tax margin |
|---|---:|---:|---:|
| 1001 | 31.17% | 31.17% | 14.27% |
| 2002 | 28.26% | 29.13% | 14.00% |
| 3003 | **38.36%** | **39.22%** | 14.17% |
| 4004 (holdout) | 31.29% | 31.29% | 13.51% |
| 5005 (holdout) | 28.22% | 28.22% | 14.77% |
| Equal-weight seed mean | **31.46%** | **31.81%** | **14.14%** |

The user explicitly selected the multi-seed average for the 20–35% Small-closure
design band, with outliers disclosed. All five seeds are included. The
week-260 mean passes; seed 3003 is individually above the band and is retained
in the result. The corporate model is therefore enabled by default, as
authorized after checks pass. The old pilot's roughly 28.1% Major margin and
the new 14.14% describe different complete configurations: the old pilot used
the broader research stack and 1.5/12 pressing. They are not a paired estimate
of the isolated effect of commercial costs.

The all-tier diagnostics remain visible:

| Tier | Mean starting-cohort closed at week 260 | Mean 1964 company margin median, December tier |
|---|---:|---:|
| Small | 31.46% | 16.73% |
| Boutique | 31.23% | 12.32% |
| Independent | 19.12% | 13.09% |
| MidTier | 57.66% | 10.12% |
| Major | 10.36% | 14.14% |

Starting-MidTier mortality remains high. Promotions replenish current tiers,
so current-tier earnings do not imply survival of their original members.
This is a remaining balance limitation, not hidden by the accepted Small
average. Neither mortality nor the channel-cost coefficients are historically
validated by this check. Household tax and unpaid sole-operator labor remain
outside company earnings, and legal forms persist through promotion. The
calibration covers 1960–64; later periods, including the excise repeal, need
separate balance review. The repeal boundary itself has a native check.

All **209,179 monthly statements** passed cash and independent P&L identities;
the maximum absolute residual for either was $1.17e-10 (rounded upward).
Founder labor never exceeded one equivalent apart from floating-point epsilon.
The final build had zero warnings/errors. All **54 company and 49 existing
settlement checks** passed. The eight-week full-world save/load preserved
249,512,982 bytes exactly, with compressed file I/O passing for 600 labels.

Compatibility controls passed at thirteen weeks: explicit opt-out matched all
92 prior economy CSVs byte for byte; ledger-only matched the same 92 streams;
implicit default matched explicit full across all 88 game-profile streams.
The short default run also matched the long run's first thirteen weeks for
finance (7,800 rows), statements (1,800), departments (16,200) and cash
transactions (76,874). The analyzer additionally rejects an unfinished planned
seed/mode matrix; the deliberate incomplete five-seed probe was rejected.

### Release-judgement flags added to the default (October 21, 2026)

The full/default bundle now also enables the release-judgement audit items that do not
depend on margin levels: A1 `--format-memory-maturity-curve`, A3 `--career-banded-format-memory`,
A5 `--promo-synergy-feedback` (these two only with the live genre market), B1/B2
`--album-scale-career-milestones`, B7 `--cross-collateralized-renewal`, C1 `--album-launch-reserve`,
C3 `--single-studio-modifier` and C4 `--self-excluded-artist-heat`. B5 `--record-campaign-spend`
remains opt-in: on two 105-week seeds it raised 1960 Major-family chart share by 2 and 12 points.
A2, A3's prior/master-cost repairs and A4 remain opt-in pending recalibration on this economy.

Against `corp-margin-game-v6` truncated to 105 weeks (same assembly), the eight flags plus B5
(`SimLogs/corp-safe-v1`) left starting-Small closures at 12.1%/9.6% (control 12.6%/7.0%) and
1961 tier margins within about a point. A 13-week implicit default matched the same flags
given explicitly on all 91 CSVs (`SimLogs/corp-safedef-parity-{a,b}`). Default games now write
save v26 because promo evidence is retained. Version checks that assumed an exact older
writing version now assert the minimum, or isolate their own switch.

Final evidence (generated, gitignored):

```text
SimLogs/corp-margin-game-v6/runs.json
SimLogs/corp-margin-holdout-v6/runs.json
SimLogs/corp-margin-adoption-v6/runs.json
SimLogs/corp-margin-adoption-v6/review.json
SimLogs/corp-margin-adoption-v6/adoption.json
SimLogs/corp-margin-compatibility-v6.json
SimLogs/corp-margin-v8-build.log
SimLogs/corp-margin-v6-native-console.log
SimLogs/corp-margin-v6-settlement-console.log
SimLogs/corp-margin-v6-roundtrip-console.log
```
