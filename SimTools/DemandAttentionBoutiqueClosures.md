# Demand attention: Boutique closure investigation

October 21, 2026. Reanalysis of the completed `dfs-v1` matrix, without a new
simulation or a change to economic behavior. Boutique closures are investigated
before film/broadcast competitors, as requested by the user.

## What 35.7% measures

It is the unweighted three-seed mean share of **starting Boutique labels** that
become Bankrupt or Defunct within 265 weeks (just over five years), classified
by their initial tier even if they subsequently promote or demote. Runtime births
are excluded. It is not an annual failure rate or a release loss rate.

| Seed | Starting Boutique | Bankruptcies | Solvent dormancy | Total closures |
|---|---:|---:|---:|---:|
| 1001 | 128 | 44 | 4 | 37.5% |
| 2002 | 146 | 47 | 1 | 32.9% |
| 3003 | 125 | 45 | 1 | 36.8% |

The pooled count is 142/399 (35.6%); the documented 35.7% averages the three
percentages instead. The mean comprises 34.2% bankruptcies and 1.5% dormancy.
Current-cost controls average 43.4% closures, so the founder bundle reduces this
by 7.6 percentage points. Small and Independent means are 22.1% and 29.2%.

The 20–35% five-year design band is specifically for starting Small labels.
There is no verified historical Boutique closure target. Calling 35.7% “high”
means high relative to the other simulated tiers, not a demonstrated historical
excess. No new historical claim or target follows from this analysis.

## Founder compensation is the largest fixed cost

`LabelCorporateModel.Ensure` assigns Small labels an OwnerOperated structure and
Boutique labels a PrivateCorporation structure. `Quote` pays a private company's
working founder at the departmental wage proxies, with the employment allowance
and yearly wage index. That debit is `ownerSalary`, separate from hired payroll.
`HomeOperated` removes retainers/office costs for an eligible no-employee firm;
it does **not** remove founder salary. This was an explicit existing policy,
documented in `LabelCorporateModelImplementation.md`, not a new regression.

Among the **136 bankrupt Boutiques** in the founder worlds:

- Founder salary is 80.0% of their summed cumulative fixed costs. Fixed costs
  here mean hired payroll, owner salary, premises, retainers and legacy overhead.
- 95 have zero median paid employees in their final six observed calendar months.
  A zero median does not assert they never hired anyone.
- 84 never launched an LP during the observation before exit. Their short lives
  and depleted funding also limit launch opportunities; this is not proof that
  forcing an LP would have saved them.

| Median among bankrupt Boutiques | 1001 | 2002 | 3003 |
|---|---:|---:|---:|
| Bankruptcy audit week | 74 | 87 | 96 |
| Last six months' record receipts | $2,934 | $2,816 | $3,064 |
| Last six months' fixed costs | $4,410 | $4,544 | $4,291 |
| Of those, founder salary | $4,251 | $4,303 | $4,053 |
| Last six months' new project costs | $750 | $746 | $685 |
| Last six months' operating cash change | -$4,091 | -$4,310 | -$3,868 |
| Actual single / LP launches before exit | 4.5 / 0 | 6 / 0 | 4 / 0 |

Medians do not add together. Operating cash includes collected record/service
income and ordinary cash expenses; it excludes income tax, capital spending,
financing and owner distributions. Receipts are after the existing record-level
settlement treatment; plant payments and commercial expenses remain separate.
Accrual cost of sales is not subtracted again.

Adding back owner salary makes this last-six-month operating cash measure
nonnegative for 51/136 bankrupt firms. This is **arithmetic sensitivity only**:
it is not a survival prediction, does not replay decisions, and supplies no
household living income. The remaining firms would still burn operating cash.

For example, seed 3003's Elektra closes in week 48 after one single and no LPs.
Its observed opening company cash is $4,818; cumulative owner salary is $6,250,
against $3,032 in record receipts. Prestige in that seed closes in week 31 after
two singles and no LPs. These are simulated firms, not historical outcomes of
their namesakes.

## A second pattern is collection and tax timing

`EconomicLabelLifecycle.ReviewMonth` declares bankruptcy after two consecutive
negative month-end cash balances. Outstanding wholesale receivables do not count
as cash. That correctly prevents spending an uncollected invoice, but the model
does not distinguish a bridgeable collection gap from a business with no income.

At first bankruptcy, **46/136** Boutiques have receivables exceeding their cash
deficit **plus** outstanding plant payables and the latest recorded tax payable:
14/44, 17/47 and 15/45 by seed. Only ten of those 46 never launched an LP.
This is a liquidity-exposure screen, not proof of solvency or available credit:
collection timing/risk, other commitments, later costs and finance terms matter.
It is not an instruction to add receivables to spendable cash.

Seed 2002's Riverside is a clear example:

- At week 104 it has $138,244 cash and $243,509 wholesale receivables.
- The December close (week 105) pays $168,004 income tax. Cash becomes -$34,521,
  marking the first negative month end.
- At week 109 cash is -$13,101, receivables $222,825, plant payables $16,460 and
  recorded tax payable $5,449. The second negative month end closes the label.
- It has launched six singles and three LPs and collected about $998k over its
  observed life. Quiet-business founder cost relief alone would miss this case.

There are 56 bankrupt Boutiques with positive cash by the end of the audit, after
post-exit collections/payments. This does not reverse their recorded operating
exit. Diagnostics use the **first exit's** cash and liabilities, not that later
balance; monthly spending summaries stop at the first exit as well.

## Quality and the meaning of Boutique

Boutique is a size tier spanning all archetypes. Across the three founder worlds,
FolkBoutique archetypes close at 7/38 (18.4%) and JazzPrestige at 7/45 (15.6%),
while RockRebel closes at 22/39 (56.4%) and BluesRoots at 18/34 (52.9%). These
small, heterogeneous cohorts are descriptive; they are not genre balance targets.
The aggregate 35.7% does not describe a uniform collapse of folk/jazz specialists.

The analyzer also selects actual singles launched in weeks 1–13 by starting
Boutiques and observes their Hot 100 entry through week 52. It uses the launch
origin even when a master transfers, keeping the follow-up window common:

| Runtime single quality | Launches | Entered Hot 100 by week 52 |
|---|---:|---:|
| At most .5 | 87 | 0 |
| (.5,.6] | 214 | 5 (2.3%) |
| (.6,.7] | 266 | 27 (10.2%) |
| Above .7 | 139 | 44 (31.7%) |

Seven of the 28 high-quality early singles originating at firms that eventually
bankrupt do enter the Hot 100. Good material has an observed route to the chart,
but chart entry does not guarantee company survival. These are observational
quality strata, not controlled estimates of quality's effect. LP chart entry is
not inferred from an empty compact album-chart stream. LP quality is kept separate
from single quality in label summaries. No unfinished release is declared profitable.

## Next implementation decision

Do not tune a Boutique closure quota or weaken commercial fit to force this mean
inside the Small-label band. The evidence supports two separate experimental
questions before another balance change:

1. **Founder compensation:** distinguish paid employment from discretionary
   owner compensation, with explicit household/side-income and any deferred-pay
   obligations. The current legal-form-by-tier assignment is a scenario profile.
   Simply deleting owner salary would subsidize incorporated founders and ignore
   the policy that originally introduced it. Research a defensible founder-income
   treatment before selecting a probe.
2. **Working capital:** test whether firms with collectible trade receivables
   can obtain a priced, limited bridge, or meet tax/plant payments on defensible
   terms. Keep liabilities, credit losses and repayment explicit; receivables
   must remain unavailable for ordinary release budgets until collected or funded.

These may overlap. The counts above are not disjoint causes and cannot be added
to forecast a corrected closure rate. A subsequent screen must retain the
quality strata, starting-tier cohorts and all accepted economy/chart guards.
No economic switches, defaults, coefficients or mortality rules change here.

When film/broadcast competitors are reached, first research what those companies
actually operated and how that maps to Label Man. Include the existing soundtrack
and Broadway cast-album systems in that assessment. A blanket promotion to a
stronger label tier is not established by this investigation; the existing
`--seed-head-templates` probe has prior adverse field-share results and should not
be treated as an untested fix.

## Reproduction and validation

`analyze_boutique_closures.py` writes one starting-label CSV per run plus grouped
JSON to `SimLogs/boutique-closure-investigation/`. It validates monthly cash
bridges, repeated identities, finite cash data, completed unrestarted observation
windows and missing records. All six original windows completed with zero missing
records. Every monthly cash bridge reconciles to about $1.2e-10 or better.

```powershell
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/analyze_boutique_closures.py 'current=dfs-v1-current1:1001,dfs-v1-current-rest:2002,dfs-v1-current-rest:3003' 'founder=dfs-v1-founder:1001,dfs-v1-founder:2002,dfs-v1-founder:3003' --output SimLogs/boutique-closure-investigation
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' -m unittest discover -s SimTools -p test_boutique_closures.py
```

Native build/run evidence remains `SimLogs/dfs-v1/plan.json` and its group
`runs.json` files. This reanalysis does not replace that provenance or assert a
new native replay. Six new tests and the four existing weekly-owner attribution
tests pass. The latter use a workspace-local temporary directory because the
sandbox's default temporary directory rejected file writes. Tests cover no
double-counting of accrual COGS, starting-tier
retention through promotion, exclusion of post-exit statements, duplicate/nonfinite
or broken cash data, exit cash versus later collections, and launch-origin
attribution with a fixed chart window.
