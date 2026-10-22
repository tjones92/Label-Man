# Demand attention: master-deal attribution and pickup screen

October 21, 2026. Continues the candidate in DemandAttentionImplementation.md,
"Rising decoupled, base .12, six seeds". No defaults are adopted here.

## Measurement repair

`ChartAuditRunner` now writes `label-sales-weekly.csv` from the completed, booked
settlement, before the runtime owner can change. Each label/format/week row includes
units, charted units and Top 10 units. This covers uncharted and retiring records and
remains enabled under `--calibration` and `--lean-probe`. It measures the observation
window, not an eventual owner's lifetime catalog. Off-chart copies remain in their
separate per-label stream.

`analyze_offchart_screen.py` uses this stream, with full weekly settlement files as
a fallback. It checks week-by-week reconciliation to regional cleared singles,
completion, duplicate rows, and count bounds. Missing weekly data fails explicitly;
`--legacy-final-owner` is an explicit reproduction of the old, biased lifetime-owner
method. Both methods now classify runtime-founded labels by `birthTier` from
`runtime-label-profiles.csv`; the opening directory alone omitted those firms.

105 weeks, seeds 1001 / 2002, original candidate (`dma-owner`). Chart-record CSVs
are byte-identical to `doc-np-oc12r` for both seeds. Therefore the attribution
comparison below holds simulation behavior fixed. Shares include the .12 base.

| Tier | Final-owner lifetime units | Weekly selling label |
|---|---:|---:|
| Boutique | 3.0 / 3.5% | 3.6 / 3.8% |
| Small | 8.2 / 9.6% | 4.1 / 4.7% |
| Independent | 51.0 / 47.6% | 54.6 / 52.3% |
| Major | 29.2 / 26.9% | 29.2 / 26.9% |

Attribution is consequential, but does **not** explain the Boutique collapse.
Lease reversion also creates error in the opposite direction: the returning owner
inherits sales made by the lessee under final-owner accounting. The old assumption
that Small shares were necessarily understated is withdrawn.

## Why pickup counts were misleading

Counts below exclude week-0 initialization deals, which are reported separately by
`analyze_ai_master_deals.py`.

| Original route, 105 weeks | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Pickup transactions | 444 | 411 |
| Distinct picked-up titles | 345 | 316 |
| Onward transfers of outstanding leases | 99 | 95 |
| Boutique to Small | 185 | 181 |
| Small to Independent | 254 | 229 |
| Initialization pickups, excluded above | 4 | 12 |

The third baseline seed (3003, `dma-owner3`) reproduces the original chart-record
file byte for byte too: 452 pickups, 335 titles, 117 onward leases, 178 Boutique-to-Small
transactions. Its additional funding telemetry shows 443 of 452 transactions with
the next replenishment covered by usable stock/protected cash under the scenario
test below. Thus most acceptances did not arise from an inability to finance that run.

For scale, seeds 1001/2002 launched 7,980/7,350 singles; 341/314 of those launch titles
were picked up (about 4.3%). The rest of the distinct pickups were opening records.
This denominator is part of the model screen, not a historical frequency benchmark.

Three problems:

1. `LabelTier.Boutique` follows `Small` in the enum, but means a specialty business.
   Comparing enum integers made any fitting Small label a "bigger" Boutique buyer.
2. Only the current seller's lease list was checked. A lessee could hand the same
   master onward, despite its original owner's outstanding exclusive lease. This
   inflated transaction counts and could prevent orderly return to the licensor.
3. `WouldLetGo` unconditionally accepted for Small and Boutique firms. It could
   surrender their breaking masters even when they had ample cash and distribution.

The original master route also bypassed the volume-qualified regional-hit gate used
by P&D deals. However, 444/444 and 408/411 observed pickups in these two baseline runs
already had recent qualifying hit evidence. Tightening evidence alone is not a
demonstrated explanation for this volume.

## Historical plausibility

The mechanism is supported; an annual national count is **not established** by the
sources below. The raw 345–450 transactions must not be compared with a handful of
famous deals as if that were a representative historical census. Nor should those
transactions be described as that many distinct records.

- [Classic Motown, Marv Johnson's "Come To Me"](https://classic.motown.com/story/marv-johnson-come/)
  describes Detroit success, insufficient resources to support national release,
  the UA re-release and a $3,000 advance against royalties. It also involved Johnson
  signing with UA; the game's master-only transaction is an abstraction.
- [Berry Gordy's retrospective account, quoted by Motown's label group](https://www.udiscovermusic.com/stories/marv-johnson-come-to-me-motown/)
  explains that master licensing brought low royalties and delayed statements.
  This supports a seller weighing its alternatives, not automatically surrendering
  every regional success. It does not supply a population frequency.
- [Rick Hall's memoir excerpt in the UCI course reader](https://music.arts.uci.edu/abauer/6.4/notes/Music_9_Reader_2023.pdf)
  connects the opportunity to lease Jimmy Hughes' "Steal Away" to financing pressing
  as orders increased, and the low share of receipts that leasing would leave him.
  This is participant evidence for a working-capital decision, not a national rate.
  The indexed excerpt was available; the PDF's redirected full text could not be fetched.
- [Boo Frazier's participant account, Record World, January 29, 1972](https://www.worldradiohistory.com/Archive-All-Music/Record-World/70s/RW-1972-01-29.pdf)
  (PDF page 30) describes Vee-Jay's arrangement with FAME as a label distribution deal. It must not
  be counted as a separate master lease for every release without contract evidence.

These cases favor gating acceptance on resources and keeping pure distribution,
master licensing, artist-contract sales, and catalog acquisitions distinct. They do
not justify tuning a weekly random chance to a selected national deal count.

## Opt-in screens

`--ai-master-national-pickups` requires `--ai-master-deals`. Regional pickups require
recent `RegionalHitDealGate` evidence, an Independent-or-larger buyer with greater
effective national reach, and no outstanding lease on the title anywhere in the
label registry. This restriction describes a national pickup; it is not a complete
model of trade among specialty labels. It leaves unreleased master shopping unchanged.

`--ai-master-capacity-decisions` additionally requires the national-pickup probe.
The seller accepts only when protected free cash cannot fund its next replenishment:
four weeks of observed sales (the existing plant-credit interval), or current dealer
backorders if larger, less usable shelves/warehouse stock, priced by the ordinary
repress bill. Existing commitment reserves protect liabilities. No hidden commercial
fit, imposed historical transaction target, or extra random draw is used. The
four-week requirement is a **scenario assumption**, not a historical estimate of
how many weeks founders financed.

These probes apply to fresh screens. They do not rewrite invalid lease chains
already present in an older saved world.

## Results and decision

The eligibility-only arm (`dma-national`, seeds 1001/2002) is rejected as a bundle
candidate: 319/354 pickups, no chained leases, but Boutique unit shares of 2.7/2.8%.
It redirects the automatic giveaways to Independents rather than curing them.

The full capacity arm (`dma-capacity`) adds both switches. 105 weeks, seeds
1001/2002/3003, versus `dma-owner` (1001/2002) and `dma-owner3` (3003). All unit shares
below use the weekly selling label and include the .12 off-chart base.

| Metric | Original candidate | + national pickups + capacity decisions |
|---|---:|---:|
| Pickup transactions, per seed | 444 / 411 / 452 | **35 / 50 / 47** |
| Distinct picked-up titles | 345 / 316 / 335 | 35 / 50 / 47 |
| Onward leases | 99 / 95 / 117 | **0 / 0 / 0** |
| Boutique unit share | 3.6 / 3.8 / 4.0% | **9.3 / 12.2 / 13.5%** |
| Mean Boutique unit share | 3.8% | **11.7%** |
| Mean Small unit share | 4.4% | 7.2% |
| Mean Independent unit share | 52.9% | 36.5% |
| Mean Major unit share | 29.6% | 34.2% |
| Mean live + off-chart singles units | 338.9M | 338.5M |
| Mean Major Top 10 share, 1960 | 37.1% | 40.6% |
| Mean Major Top 10 share, 1961 | 30.0% | 34.9% |
| Starting Small closed, per seed | 21.6 / 13.5 / 19.0% | 22.9 / 16.5 / 23.3% |
| Mean starting Small / Boutique / Independent closed | 18.0 / 25.1 / 18.9% | **20.9 / 26.5 / 16.3%** |

The paired Boutique gain is +7.9 points (SE 1.1); the live-unit change is -0.33M
(SE 2.67M). Each capacity pickup has recent hit evidence and a replenishment funding
shortfall. Median weekly sales at pickup are 6,774 / 6,868 / 7,639, versus
2,389 / 2,516 / 2,578 in the original arm. No numerical volume target was fitted.

**Reading:** automatic acceptance, not retrospective attribution alone, was the
main error behind the Boutique collapse. The capacity screen restores its share
without eliminating the master route or materially changing total singles volume.
The new count is a mechanism-based scenario result; it is **not** a measured
historical pickup rate. A nationwide count remains unverified.

**Remaining costs:** Small closures rise +2.9 points (SE .9), Boutique +1.4 (SE .8),
Independent falls -2.6 (SE 1.3). Keeping a master is not itself an exit from fixed
overhead. The Major sales share also rises +4.6 points (SE 1.5); the previous six-seed
31.9% finding cannot simply be carried over to this repaired bundle. The 1961 Top 10
change is +4.9 points (SE 5.1), almost entirely seed 1001. Deal changes alter later
decisions and the global RNG stream; these are paired whole-world screens, not
isolated record counterfactuals.

Carry both new switches into the next **experimental** closure screen. No game
defaults or bundle adoption are changed. Next in the requested order: founder-scale
overhead plus a separately measured dormancy exit; then a 265-week screen against
the 20–35% five-year design band; then the Major/film-broadcast chart split; then
label attribution of the recovered Hot 100. The ~88% chart-week share and ~150k #1
remain withdrawn targets.

## Reproduction and verification

Run manifests and console logs are under `SimLogs/dma-owner`, `dma-owner3`,
`dma-national`, and `dma-capacity`. Each manifest records exact arguments, assembly
hash, seeds, duration, exit status and completion. The original candidate flags
are unchanged; `dma-national` adds `--ai-master-national-pickups`, and `dma-capacity`
also adds `--ai-master-capacity-decisions`.

```powershell
python SimTools/analyze_offchart_screen.py "original=dma-owner:1001,dma-owner:2002,dma-owner3:3003" "capacity=dma-capacity:1001,dma-capacity:2002,dma-capacity:3003" --paired original:capacity
python SimTools/analyze_offchart_screen.py "original=dma-owner:1001,dma-owner:2002,dma-owner3:3003" --legacy-final-owner
python SimTools/analyze_ai_master_deals.py SimLogs/dma-capacity/dma-capacity-default-1001-105-console.log SimLogs/dma-capacity/dma-capacity-default-2002-105-console.log SimLogs/dma-capacity/dma-capacity-default-3003-105-console.log
```

Final screen output is saved in `SimLogs/dma-capacity/screen.txt` and
`pickups.json`. Build: zero warnings/errors. Native demand-stocking checks: 47
passed. Python attribution regressions: 4 passed. All eight 105-week audits passed;
the three original-candidate replays match their prior `records.csv` byte for byte.
