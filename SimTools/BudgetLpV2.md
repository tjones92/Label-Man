# Budget LP v2: calibration review and first implementation

The next own-master product/substitution/shared-shelf implementation is in
[BudgetLpProducts.md](BudgetLpProducts.md). This file preserves the earlier
supply-only review and its evidence.

25 October 2026. Continues [LpDemandDiagnosis.md](LpDemandDiagnosis.md) and
[RackJobberChannelDesign.md](RackJobberChannelDesign.md). The documents describe
prior experiments; this review uses the completed `rkj-v1` results. Existing
working-tree rack implementation is preserved. No calibration defaults change.

Follow-up validation is tracked in [RackJobberValidation.md](RackJobberValidation.md).
The matched extra seeds, shared-exhaustion arm, live return-cost scenarios and
1961–65 growth screen are complete. The table below retains the original
booking-year club convention. The follow-up aligns all channels to the store
settlement calendar (1961 clubs 44.135M, total LPs 157.234M for seed 1001).

## What remains before historical accuracy

The 1961 totals are close, but opposing channel errors cancel. Units in millions:

| Channel | rkj-v1, seed 1001 | Historical reference | Model minus reference |
|---|---:|---:|---:|
| Stores | 76.87 | 68 | +8.87 |
| Front-line racks (all currently modelled racks) | 36.24 | about 36.6–41.2 | within the exploratory range, near its bottom |
| All racks, including budget | 36.24 | 60 | -23.76 |
| Clubs/mail | 45 | 45 | 0, authored by construction |
| Total LP | 158.11 | 173 | -14.89 |
| Singles, stores plus racks | 136.64 | 132 | +4.64 |

The net 14.89M shortfall is **not** an independently observed budget market size.
Using the 1962 NARM members' budget share of LP dollars (21%), a $3 front-line
retail price and assumed $1.25/$1.75 budget prices yields 38.95%/31.30% of units.
Applied to 1961's 60M racks, that is 23.37M/18.78M budget LPs. These are price
sensitivities: member versus national coverage and 1962 versus 1961 remain
qualifications. Adding them unchanged would overshoot total LPs by 8.48M/3.89M.
Do not choose budget conversion merely to make 173M appear.

The existing rack shelf is another constraint: 53.32M annual units, with 17.08M
unused after front-line sales. It cannot carry 60M even with unlimited demand.
Budget capacity must be accounted for beside front-line use. Test shared shelf
first; independently justify any change to rack count or turnover. Do not give
each line a full duplicate rack shelf. National annual headroom also overstates
usable capacity when demand and spare space differ by region or week.

Remaining validation, in priority order:

1. Frozen-scale matched seeds 1002–1003: completed. Front-line racks reach
   34.8–36.2M across all three seeds. Concentration and survival effects vary;
   see the follow-up report for paired results.
2. Budget product supply, conversion and economics. A reissue is a separate cheap
   product, not a new high-price sale on the original front-line record.
3. Buyer overlap and returns. Shared-exhaustion and return-cost arms are now
   implemented; the completed overlap stress materially reduces volume.
   Chart-points sensitivity remains unimplemented.
4. Channel-specific later-year trajectories and the decade A/B. The runner now
   accepts up to 530 weeks and the analyzer reports all observed years, marking
   partial years. The completed 318-week pair shows a store excess in 1963/64,
   a 1965 front-line rack plateau and stronger Major assortment concentration;
   it is not decade validation. Do not splice the revised 1966–68 levels onto the
   older 1961–65 series as a continuous target.
5. Opening catalogue composition and record coverage. Pre-1960 title ages are
   inadequate; review counts are not a count of newly produced domestic titles.
   Singles already include racks: avoid adding a second rack-single market.

## V2 first slice: catalogue supply audit (implemented)

`--budget-lp-audit`, on a genre-market-v2 audit with `--record-clubs --rack-jobbers`,
streams `*-budget-lp-supply.csv` at each booked, pre-retirement settlement.
[BudgetLpCatalogue.cs](../Systems/BudgetLpCatalogue.cs) makes detached scalar
snapshots, with separate `(label, artist, source kind)` rows:

- `Retired`: the existing club ledger's title count and decaying appeal.
- `LiveAged52`: live AI LPs aged at least 52 weeks with positive store lifetime
  sales. The threshold is a supply sensitivity, not a historical eligibility rule.
- `OpeningSynthetic`: the club opening seed. Title count stays blank, artist stays
  blank. Its appeal is synthetic, not observed pre-1960 unit sales.

Only active AI labels are included. Appeal is a weighting proxy, not unique
buyers, pressing stock or predicted demand. Live appeal uses store lifetime
sales, consistent with the retired club ledger; rack totals are not silently
added to one side. The synthetic seed can overlap the represented opening live
catalogue economically; keeping it separate makes that assumption visible.

There are no budget sales, bookings, chart points, release issues, RNG draws or
new saved fields. The observer does not initialize, decay, prune or allocate the
club ledger. The existing acquisition path already moves its aggregate owner.
Source kinds distinguish retiring records from entries folded in earlier weeks.
The first 13-week check cannot establish the longer-run `LiveAged52` supply.

Reproduction (use a free run tag; build first):

```powershell
dotnet build --no-restore
./SimTools/RunRackJobberProbe.ps1 -RunTag blp-v2-supply -BudgetLpAudit -Arms racks -Weeks 13
python SimTools/analyze_budget_lp_supply.py --tag rkj-v1 --supply-run blp-v2-supply-racks-default-1001-13
```

`--budget-lp-catalogue-check` on `SaveLoadRoundTripRunner.tscn` exercises source
boundaries, unknown opening counts, exclusions, deterministic ordering, no
mutation and retirement migration. The analysis writes generated JSON under
`SimLogs/`, including price assumptions and separate channel errors.

Validation completed for this slice:

- `dotnet build --no-restore`: zero warnings or errors.
- Eight native catalogue checks passed.
- `blp-v2-supply`, seed 1001, 13 weeks: completed without engine errors.
- Existing weekly CSV streams matched `rkj-v1-racks-default-1001-106` through
  week 12, including rack region/title rows, sales, cash, charts and retirement.
  Replay evidence: `SimLogs/blp-v2-supply/replay.json`.
- Supply output: 13 weekly snapshots, 104 synthetic opening-label sources each
  week, no retired or 52-week-old live supply yet. This is a startup integration
  check, not validation of a mature budget market. No new save fields are added;
  the full-world save/load check was not rerun for this observer-only slice.

Godot crashed before project initialization inside the execution sandbox. The
same headless native and world checks passed outside it. Generated evidence
stays under `SimLogs/budget-lp-v2-checks/` and `SimLogs/blp-v2-supply/`.

## Next implementation slice

The completed validation supports keeping the front-line scale frozen while
addressing buyer substitution, shared capacity and annual channel trajectories.
It does not support sizing budget sales from one net 1961 gap. The shared-
exhaustion stress changes total LPs by 17.3M; live return-cost scenarios change
them by less than 1M on seed 1001. Current shelf capacity remains below the
historical all-rack total even in 1965. Detailed results and limits are in
[RackJobberValidation.md](RackJobberValidation.md).

The aggregate club ledger cannot identify an individual master, recording date,
genre, contract, budget licence or prior reissue. Do not treat club licences as
budget licences. Before booking budget sales, add a compact saved product ledger
with owner, source provenance, release/holdback, repertoire weights and budget
exhaustion. Choose an explicit aggregate-product abstraction if title identities
cannot be recovered; do not invent title identities from aggregate title counts.

Budget offerings then need a bargain shopper pool, bounded assortment and shared
rack shelf allocation. Own-label and licensed product need separate receipts,
pressing/packaging/mechanical costs, artist royalties and returns. The club's
17.8c licensing royalty describes club contracts and is not evidence for budget
terms. Retired products must remain outside front-line chart points, original
record retirement and new-record production counts. Save/load and ownership
transfer checks precede any volume fit. Test cannibalization explicitly instead
of assuming every budget buyer is an additional buyer.

## Sources and limits

- [Billboard, 16 March 1963](https://retrocdn.net/images/0/0f/Billboard_US_1963-03-16.pdf):
  1962 NARM survey. Search-index text rechecked the 21% figure; full scan fetching
  was blocked during this review. Prior design contains the detailed extraction.
- [Billboard, 13 March 1965](https://www.worldradiohistory.com/Archive-Billboard/60s/1965/Billboard%201965-03-13.pdf):
  budget symposium distinguishes bargain customers from full-price customers.
  Search-index excerpt rechecked; no new claim about conversion is inferred.
- Outlet unit series, returns and other period details: sources and qualifications
  in [RackJobberChannelDesign.md](RackJobberChannelDesign.md).

No new empirical price, conversion or contract estimate is established here.
