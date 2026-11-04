# Handoff: regional lockstep (audit step 3)

Full record: [RegionalSyncDesign.md](RegionalSyncDesign.md) (§2–4 diagnosis, §6b as built, §6c all results).
Branch `media-atmosphere`; code at `0a79082`, plus uncommitted §6c bankruptcy notes and
`analyze_arrival_bankruptcies.py`.

## The problem

The seven market regions sell every single on the same timeline. Regional demand is 99.8% `region gain × common
week shape`. Regions differ in **how much** they sell, never **when**. There are two causes, and each one alone is
enough to hold the lockstep:

1. **Simultaneous launch.** Every single gets launch stock, awareness and rotation in all 7 regions in week 1.
   Regions the label doesn't ship to just get less (launch factor 0.12–0.30).
2. **National awareness blend.** `effectiveAwareness = 0.4·national + 0.6·regional`
   (`ChartSimulator.CalculateRegionalSales`), so 40% of every region's awareness is one shared series.

Targets (settled sales, not raw demand): local share of weekly change ≥ 10%, top-11 overlap 70–85%, settled peak
spread median 3–5 weeks with 5–15% of records ≥ 8 weeks apart. Guards: single units ±3%, Major share ±3 pts,
Hesbacher ratios, closures and bankruptcies. Single-seed noise on Major share over 104 weeks is about ±5 pts.

## What's built (all behind switches, off by default; switches off = byte-identical to control)

- `--local-signals-read-demand` (5d): local readers use raw demand instead of stock-limited cleared units. A
  correctness fix, neutral on the guards.
- `--regional-arrival` (5a–5c): a single is **held** in regions its label doesn't ship to (`RegionalAudience.
  HoldUntilArrival`). Before arrival there is no launch seed, no restock, no rotation target and no reporter-station
  add, and awareness = `0.6·regional + 0.25·0.4·national` (hearsay, all of it unserved demand). `TryArrive`
  triggers, in order: national top 40, neighbour test ≥ 0.08, own breakout > 0.24, rack on regional proof,
  backorders ≥ 100 × region scale (one-stop order).

Run: `--weeks=104 --seed=N --enable-genre-market-v2 --enable-artist-population-lifecycle [switches] --run=NAME`
(the two `--enable` flags are required). Score: `analyze_regional_sync.py`, `analyze_regional_guards.py`,
`analyze_arrival_bankruptcies.py`. Each 104-week run takes about 9 minutes.

## What we tried (104 weeks; deltas against the 5d-only control on the same seed)

| Arm | Held regions that ever get the record | Local share of weekly change | Settled ≥ 8-wk peak spread | Bankruptcies |
|---|---:|---:|---:|---:|
| Control | — | 1.3% | 0.3% | — |
| Arrival, no route of its own (s1001) | 8% | 29.8% | **7.2%** ✓ | +14 |
| + hearsay 0.25 (s1001) | 8.5% | 6.7%* | 3.1% | +17 |
| + one-stop order (s1001 / s2002) | 73% | 20% / 20% | 0.8% / 0.2% ✗ | +17 / +14 |

\* That metric reads raw demand, which hearsay re-couples by construction.

Findings:

- A held region can't trigger its own breakout: with no stock and ~0 awareness it has no demand to show. Hearsay
  made the breakout trigger fire, but it mostly replaced the other triggers instead of adding arrivals.
- The one-stop order delivers the record, but at a near-uniform week ~4. That is a lag shared by all held regions,
  not regional timing, so settled spread falls back and overlap rises to 89%.
- **The whole bankruptcy excess is Small-tier labels that sell singles** (+12 on each seed). Their single units fall
  14–26%, while Independent and Boutique units rise 2–8%. It is lost launch-week revenue from out-of-territory
  regions, not stranded stock (COGS is booked per unit sold). Coverage count doesn't predict the victims; size does.

## The core tension

The thing that breaks the lockstep, a **late** start in uncovered regions, is the out-of-territory launch revenue
Small labels live on. No arrival → timing but bankruptcies; reliable arrival → no timing and still bankruptcies.

## Open ideas, untested

1. Hold the region but **sell the held launch stock to hearsay demand** before arrival: Small labels keep some
   revenue, and timing stays local because that demand is small. (Claude's pick.)
2. Keep launch stock and launch-week sales in held regions; gate only the blend and rotation. Likely protects Small
   labels but re-synchronizes the start (an earlier probe of this shape reached only 6.7% local share of change).
3. Accept Small-tier folding as historical: relax that guard and keep the no-route arm, which met the timing target.
4. Arrival via the label's independent distributor is **already** in place (`AILabel.HasDistributionInRegionForRecord`
   counts `independentDistributionRegions`), so it is not a new lever.
