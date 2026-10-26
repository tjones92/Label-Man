# LP assortment × buyer channel choice

26 October 2026. Continues [BudgetLpProducts.md](BudgetLpProducts.md). The user
requested crossing assortment changes with broader buyer channel choice. This
experiment keeps the front-line .026 shopper scale, budget .08 shopper scale,
$1.75 budget price, .25 budget substitution and 30-unit rack turnover fixed.
Neither treatment is an adopted game default.

## Two independent treatments

**Assortment:** `--rack-mixed-assortment` retains the existing 25 front-line hit
and 50 front-line catalogue candidates alongside at most 25 budget offers. The
comparison permits up to 100 titles instead of forcing budget offers to eject
25 front-line catalogue titles from the previous 75-title list. Every fulfilled
copy still uses the **same single regional shelf capacity**. There is no new
rack count, throughput, shopper pool, conversion multiplier or per-title exposure
boost. This tests wider available repertoire against the existing throughput
proxy; it does not model physical facings, permanent stock depth or replenishment
lead times. The 75/100 title counts and retained exposure remain assumptions.

**Channel choice:** `--rack-store-choice=0.5` makes an existing AI store buyer
eligible to choose the same front-line LP at a rack. Planned integer preferences
are `floor(cleared title store purchases × coefficient × rack reach × display)`.
The .5 coefficient is an exploratory midpoint, not an estimate or a value solved
from the 6.44M store excess. Coverage follows the existing reach curve; display
uses the selected title's existing exposure. Unavailable titles and players' LPs
are excluded. Front-line prices and the existing budget price incentive stay fixed.

Preferences reserve a cohort before budget substitutions are calculated from the
remaining same-genre store buyers. Incremental budget demand is unchanged. Rack
allocation then jointly clears autonomous front-line demand, front-line store
preferences and budget demand against the existing shelf. Only fulfilled store
preferences remove store sales. The fulfilled front-line cohort is rounded once
per region and apportioned by largest remainders, avoiding one autonomous copy
per title from repeatedly flooring a near-pure switching share. Unfulfilled
preferences stay in stores and are not offered a second budget purchase that
week. Budget transfers are apportioned
only among buyers outside the reserved front-line cohort. This is a hierarchical
anonymous weekly choice model, not a fitted multinomial utility model or a
longitudinal ledger of unique people.

Each region/week verifies:

`store before = store after + fulfilled front-line switches + fulfilled budget switches`.

Front-line switches cannot exceed sold front-line rack copies, and the two switch
cohorts cannot overlap. Freed store capacity is not reassigned to singles or a
second spillover pass. Actual remaining store purchases consume store inventory;
routed purchases use ordinary rack economics and cumulative rack ledgers. The
saved regional ledger also retains routed buyers' store origin: they continue
to exhaust the store audience and are excluded from autonomous rack exhaustion.
The shared-exhaustion sensitivity counts all channel purchases once. Sales
accounting remains in the actual channel; no store sale or chart unit is added
for this saved buyer history. Native checks cover both ledgers and save/resume.
The existing store-only chart/retirement rules are retained, so changed channel
allocation can also change subsequent charting, retirement and catalogue supply.
The report includes LP availability, launch entrants, retirements and singles to
expose those effects.

## Saved settings and diagnostics

Active treatment worlds write v33 and persist both settings in the budget ledger.
Resume rejects different settings. With both treatments off, their optional
fields are omitted and the budget writer stays at v32. Week-local cohorts are
reconstructed after load. No random draws are introduced.

`lp-channel-choice-weekly.csv` contains original store purchases, reserved
front-line preferences, fulfilled front-line switches, fulfilled budget switches
and final stores. Existing rack title/region streams remain front-line only;
budget streams remain separate. All-rack capacity and total title breadth are
checked together, with a 75- or 100-title bound according to the treatment.

The extra `budgetchoicepure` smoke arm uses no incremental budget shoppers,
near-zero autonomous front-line shoppers (.000001), choice coefficient one and
budget substitution one. It must exercise both transfer paths, every budget copy
must match a store switch, and the full regional identity must hold. It is a
conservation stress, not a historical volume fit.

## Reproduction

Use fresh run tags. Generated evidence stays under `SimLogs/`:

```powershell
dotnet build --no-restore
./SimTools/RunRackJobberProbe.ps1 -RunTag lpc-v4-verified -Compact -Weeks 13 `
  -Arms budget,budgetmix,budgetchoice,budgetmixchoice,budgetchoicepure -Seeds 1001
python SimTools/analyze_lp_channel_factorial.py --tag lpc-v4-verified
./SimTools/RunRackJobberProbe.ps1 -RunTag lpc-v4-final -Compact -Weeks 106 `
  -Arms budget,budgetmix,budgetchoice,budgetmixchoice -Seeds 1001 `
  -SkipChecks -ValidationTag lpc-v4-verified
python SimTools/analyze_lp_channel_factorial.py --tag lpc-v4-final
python SimTools/plot_lp_channel_factorial.py --tag lpc-v4-final --year 1961
```

The long screen reuses completed native/save/load checks only when the source
manifest is complete and its assembly hash matches exactly. The analyzer checks
capacity, products, financial units, cohort conservation, saved depletion and
company cash before reporting effects. It separates assortment effects with and
without channel choice, channel-choice effects with and without wider assortment,
and their interaction. Partial calendar years remain marked and are not annualized.

The held 1961 shelf ceiling is approximately 53.11M units, below the historical
60M all-rack target. The factorial can diagnose demand, substitution and binding;
it cannot meet that target while the ceiling stays fixed. A capacity follow-up
must be reported separately from the primary factorial.

## Results

All four `lpc-v4-final` cells completed 106 weeks on seed 1001 and assembly
`08E2886AC879A1CDED4ABC2D744F835065E06B04C0A3D487E5F67A3642995183`.
The 1961 settlement year has 52 weeks; the two-week 1962 tail is partial and is
excluded from the historical comparison. Units are millions:

| Cell | Stores | Front-line racks | Budget racks | All racks | Front-line store switches | Budget store switches |
|---|---:|---:|---:|---:|---:|---:|
| Current budget model | 74.441 | 26.561 | 16.928 | 43.489 | 0 | 1.800 |
| Wider assortment | 74.274 | 33.893 | 16.616 | 50.510 | 0 | 1.741 |
| Broader channel choice | 74.101 | 29.296 | 16.787 | 46.084 | 2.553 | 1.764 |
| Both treatments | 72.800 | 34.468 | 15.932 | 50.400 | 2.624 | 1.564 |
| Historical outlet reference | 68 | — | — | 60 | — | — |

Wider assortment is the strongest rack-volume result: +7.021M racks with stores
almost unchanged (-.167M). Broader choice alone adds 2.595M racks but lowers
stores only .341M after subsequent world effects. With both treatments, stores
fall 1.641M and racks rise 6.911M from the current budget model. The remaining
errors are +4.800M stores and -9.600M racks. These are improvements, not a fit.

The effects are not additive. Adding choice to wider assortment lowers stores
another 1.474M but changes racks by -.110M. Relative to adding the two standalone
effects, the interaction is -1.134M stores and -2.704M racks. Both treatments
fulfill 4.188M direct store transfers, but counterfactual store intentions before
those transfers are 76.988M versus 76.241M in the control. Greater direct
switching therefore does not become an equal net change in annual store sales.

### Capacity and demand decomposition

With both treatments, 2.884M front-line preferences are planned and 2.624M
fulfilled. Only .260M of this route is lost at fulfillment. The current .5 ×
coverage × display rule produces a relatively small eligible cohort; increasing
capacity alone does not generate the extra 4.800M store switches needed to reach
68M in this state. This merits a separate test of known buyers' channel access
versus discovery/display exposure, rather than increasing autonomous shoppers.

Recorded pre-capacity rack demand with both treatments is 55.100M, versus
50.400M sold. Removing every unit cap from those recorded intentions could
recover at most about 4.700M, leaving a 4.900M all-rack demand shortfall even
before allowing for changed future behavior. The ceiling remains 53.110M with
2.710M unused nationally across different regions/weeks. With wider assortment
alone, raw demand is 54.108M, sold volume 50.510M and unused capacity 2.600M.

The loss of other sales when routing is enabled is not solely physical crowding.
Compared with wider assortment alone, the joint world's raw autonomous front-line
demand falls 1.459M and raw budget demand falls .433M; subsequent catalogue,
chart/retirement and business outcomes also differ. Sold autonomous front-line
racks fall 2.049M and budget racks .684M, offsetting the added 2.624M fulfilled
front-line store transfers. A higher-capacity world must be run independently;
adding these annual differences as independent quantities would be misleading.

Mean live LP counts are 3,635 / 3,608 / 3,624 / 3,650 in control / assortment /
choice / both; annual LP retirements are 929 / 925 / 925 / 934. Singles remain
136.742 / 136.726 / 136.404 / 136.589M. Thus there is no large collapse in LP
availability or singles in this screen, but title composition and feedback still
affect the annual channel result. Clubs remain at 44.135M on the shared
settlement calendar in every cell.

### Validation and disposition

- Build: zero warnings/errors. 28 channel-choice checks, 34 budget checks and
  50 rack checks pass, including saved buyer-origin depletion, pure cohorts,
  disjoint preferences, integer allocation and settings validation.
- Existing rack and budget world/gzip round trips and the new v33 joint-treatment
  round trip all pass and resume. Five 13-week smoke worlds pass their accounting
  and conservation checks, including the near-zero autonomous shopper stress.
- All four long cells pass regional buyer identities, fulfilled sales, shared
  capacity, assortment breadth, saved product depletion and financial-unit checks.
  Company cash reconciliation error stays below 2.4e-10.
- The corrected baseline matches the previous 106-week budget world byte for byte
  in all six sales/company/cash/finance/rack streams. The short baseline replays
  78 weekly streams exactly; the off replay matches 75 archived streams through
  week 12. Evidence is under `SimLogs/lpc-v4-verified/` and `SimLogs/lpc-v4-final/`.
- Preliminary runs before the saved origin ledger were marked superseded and are
  excluded. The accepted short and long matrices use the same assembly hash.

Wider assortment is the leading candidate for recovering the rack catalogue
loss under these assumptions. The tested channel-choice rule moves purchases in
the intended direction but is insufficient to close the store excess. The next
screen should distinguish known-buyer access/preferences from discovery exposure
and review the rack-count/throughput assumptions separately. A one-seed 1961
screen does not establish the correct coefficient, title breadth or 1962–65 path.
All new behavior remains opt-in; game defaults and shopper scales are unchanged.
