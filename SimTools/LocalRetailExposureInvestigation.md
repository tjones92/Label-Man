# Local lists, national measure: local retail exposure

Started 2026-10-30, following `BoundedAudienceInvestigation.md`. Research switch only
(`--local-retail-exposure`); fresh uninterrupted worlds; no production default changes.

## The question

How can a record break in a region and then go national without disturbing the tuned
economy? This note first states what "without disturbing the economy" requires
mechanically, then reads the bounded-audience result against that, then measures where the
present model forecloses a regional break, and finally screens one structural change.

## 1. What economic neutrality means in this market

Single units do not come from demand. Each region-week clears singles against a budget:

```
singleBudget = 0.95 * min(single serviceable intent, capacity - LP share) / (1 + off-chart share)
```

and every single is rationed proportionally to its serviceable intent (`AllocateProportionalLocal`).
Two properties follow, and every regional-breakout experiment so far has been judged by them
whether or not it said so:

1. **Within a region-week, demand only sets shares.** Adding demand to some records takes units from
   the others in the same region-week.
2. **Total single intent still moves the purse, in two ways.** Before LPs have their own mature channel,
   capacity is split between singles and LPs in proportion to intent
   (`CalculateFormatClearingBudget`). And capacity binds in only about half of region-weeks
   (48% in the 12-week control), so moving intent between capacity-bound and slack region-weeks
   changes how much clears.

So a mechanism is economy-neutral to first order only if it **preserves each region's single intent
and changes only who holds it, or when**. Two constructions qualify:

- **A permutation within a region-week.** The rank exposure weights are an example: every region
  has exactly one #1, ten Top-10 floors and one hundred charted shelf weights. Reassigning them
  leaves the region's multiset of weights intact.
- **A time shift of a record's lifetime demand.** With a stationary release stream, delaying a class
  of records' regional demand without destroying it leaves that class's steady-state share
  unchanged. Only the transient and the labels' cash timing move.

The earlier arms fail this test in one direction or the other:

| Arm | Raw single demand | What it did to the economy |
|---|---:|---|
| Direct contacts c=3 | +21% to +38% | created demand; reallocated to treated titles; closures +9.3 |
| Buyer WOM b=8 | +75% | created demand; flattened the top; million sellers 2.0 to 0.67 |
| Familiarity f=6 | +155% | created demand on existing winners |
| Home-only launch (replay) | indie -28% | destroyed indie demand that the spread never recovered |
| Bounded audience (radio) | -31% | destroyed demand (section 2) |

None of these is a regional-breakout mechanism with an economic side effect. The side effect is
what each one does.

## 2. Why the bounded audience cut sales

`ba12-v1-audience_random-1001-bounded-audience.csv`, 30,737 record-region-weeks:

- **The ownership bound never binds.** At the end of the run, owners/capacity has a median of 0.00026,
  a 90th percentile of 0.004 and a 99th of 0.057. Capacity (median 192k per region) is the region's
  whole potential buying audience, and the model's "awareness" is a demand index, not a head count.
  An interested person's weekly purchase hazard (`rate`) has a median of 0.00076. Conservation of
  owners is true but inert for 99% of titles.
- **What actually changed was the awareness engine.** Interest starts at capacity times launch aware
  share (376M person-units in week 1) and loses 10% a week. It gains only 1-5% a week, because radio
  contacts have a median of 0 (90th percentile 0.0037) and external contacts about 0.0035. The old
  model's awareness kept growing from national radio heat, word of mouth and chart floors. The
  bounded model's intent/legacy ratio falls from 1.01 at age 1 to 0.26 by age 9.
- **Who it hurt.** Connected records lost 51% of legacy intent; unconnected ones lost 23%. Every
  record's demand was pulled toward launch week, where label push and artist heat set awareness.

So the experiment tested "replace awareness growth with a contact process that has almost no
contacts", not "conserve buyers". Fewer trials survived because destination regions saw almost no
interest growth. Conservation of buyers can only shape a curve if capacity is the record's own
reachable audience (thousands to hundreds of thousands per region), not the region's population.
That is a calibration project, and nothing in the breakout question needs it.

## 3. Where the present model forecloses a regional break

Measured on `lrc52-v1-ctl-1001` (52 weeks, the current regional-fit control profile):

**Scale.** National #100 sells a median 3,784 units a week. A region's #10 sells 15,673 (East
Coast), 10,604 (Great Lakes), 6,740 (West Coast) and 3,372 to 3,819 (Deep South, Southwest,
Great Plains). Even the East Coast's #40 (4,045) clears #100. Any big-region top-10 record is a
national chart record the same week, by arithmetic. **100% of first regional top-10 entries are on
the Hot 100 that same week** (history: 85-88% of city survey top-10 entries ever chart). Only ~77
distinct records fill the seven regional top 40s each week.

**Diffuse launches.** Indie Top-10 records debut nationally at median age 2.5, when their best
region has only 44% of their units (population max 34%; per-capita max/mean 2.3). Two weeks
before debut, their best regional rank is #100. They climb everywhere at once and chart before they
top any regional list.

**National amplifiers act everywhere.** National chart position sets, in every region, the rank
exposure weight, the top-10/40 awareness floors, the visibility discovery signal, the top-20 shelf
bonus and rack access. A record that is national #15 gets #15's shelf in a region where nobody buys
it. A region's best seller gets nothing for that until the national total catches up. This common
factor is why a late region had to replay a logged national rise (§9u relive) instead of climbing
its own list. Measured from week 10: national #11-20 records are below that region's #40 in 14.5%
of record-region-weeks. National #21-40 records are below #40 in 35.6% and below #100 in 9.3%, yet
they hold Top-40 floors and shelf there. 99.2% of local top-10 records are already national Top 40.

**The post-peak decay runs on the national clock.** `--post-peak-demand-decay=0.8` (in the default
profile) multiplies a charted single's conversion in every region by `0.8^(weeks since its national
sales peak - 1)`. A region reached after the national peak is decayed while it is still rising.
Among 484 late-exposed record-regions, the demand-weighted multiplier is 0.917, against 0.984 on
each region's own clock (+11.5% of their demand). At local ages 8-12 it is 0.55-0.75 against
0.65-0.98.

## 4. The change: local retail exposure

`--local-retail-exposure` (`Systems/LocalRetailExposure.cs`) keys every rank-dependent *retail* term
in live single demand to the region's own sales list (`localSalesRank`, last week's settled units,
the list stations already read). `--local-retail-exposure=λ` keys it to a λ-weighted blend of the
national and local positions instead (section 5). That covers the exposure weight, the awareness floors, the
visibility signal, the top-20 shelf bonus, rack access, and the restock triggers for one-stops and
rack jobbers. Rack jobbers, one-stops and jukebox operators stocked what turned over in their own
territory. The post-peak decay reads the region's own sales peak for live releases. The national
chart keeps its trade-attention roles: campaign hold, radio heat, national awareness and word of
mouth, and the national-chart exposure spread rule.

A late region now relives the rise naturally: it climbs its own list from unranked, so it gets the
exposure it earns there.

Two pieces keep it economy-neutral:

- **Mass normalizer.** Re-keying puts the best weights on the records each region already buys most,
  which raises its single intent about 7% (rearrangement; static estimate on `lrc52-v1-ctl-1001`).
  Each single's intent is also computed with national keying, in the same pass. Next week, each
  region's singles are scaled by the ratio of national-keyed to local-keyed intent (bounded 0.5-1.5),
  so the switch moves who holds the region's demand, not how much there is.
- **Seed.** The list is written after each week's sales. Without a seed, the first live week saw every
  single unranked everywhere and cleared half the purse (617k against 1,226k singles). The seed reads
  the last prewarm week.

Static first-order estimate (exposure weight only, no feedback) on `lrc52-v1-ctl-1001`: Major unit
share -1.1 pp, Independent +0.5, Small +0.4, Boutique +0.3. The ten largest weekly sellers' share
falls 28.6 to 26.9%. Regional top-11 overlap falls 56% to 47% (history 40-45% city to city; 45-65%
is defensible for macro-regions).

## 5. Results

### Verification

- Switch off: the 12- and 52-week controls are byte-identical to `lrc12-v2-ctl-1001` and
  `lrc52-v1-ctl-1001` (records, label finance, market clearing, station playlists).
- `--local-retail-exposure=1` is byte-identical to the bare flag across the two builds.
- Every receipt has exit 0 and an unchanged assembly; the market integrity counters are zero
  (`analyze_local_consumption.py` raises otherwise).
- Normalizer scales settle at 0.83-1.00 by region. The largest corrections are in the small regions,
  where national hits sell least.

### Fully local lists (λ = 1): 4 seeds, 52 weeks, `SimLogs/lre52-v2`

The table shows seed means, with seed-paired deltas (SE). `nb` sets
`--regional-fit-exposure-launch-base=0.1` (narrow launch footprints).

| Metric | ctl | lre | lre - ctl | nb - ctl | lre_nb - lre |
|---|---:|---:|---:|---:|---:|
| Single units, M | 120.94 | 121.37 | +0.42 (0.05) | -0.39 (0.11) | -0.30 (0.07) |
| LP units, M | 38.68 | 38.74 | +0.06 (0.15) | -0.00 (0.16) | -0.06 (0.18) |
| Major unit share, % | 35.74 | 31.75 | -4.00 (0.89) | +2.72 (1.16) | +1.52 (1.08) |
| Indie-family unit share, % | 33.91 | 37.52 | +3.61 (1.19) | -1.68 (1.48) | -2.00 (1.08) |
| Big-5 Top-10 slots, % (history 34.2) | 39.38 | 34.86 | -4.52 (1.87) | +3.80 (3.67) | +0.77 (0.68) |
| Ten largest sellers' unit share, % | 28.46 | 24.58 | -3.88 (0.96) | +0.12 (0.90) | +0.32 (0.16) |
| Distinct Top-10 artists | 79.5 | 96.3 | +16.8 (4.7) | -3.0 (2.2) | -2.5 (4.4) |
| Closures | 34.75 | 39.25 | +4.5 (4.5) | +3.0 (2.4) | +2.25 (2.6) |
| Live singles over 1M (in window) | 5.75 | 2.00 | -3.75 (0.75) | +1.5 (1.0) | +1.0 (0.4) |
| Indie national Top-40 records (with a regional top 10) | 112.5 | 180.0 | +67.5 (3.4) | -6.8 (1.2) | -5.5 (6.2) |
| ...regional top 10 >= 2 wk before national Top 40, % | 2.2 | 13.7 | +11.4 (1.4) | +3.5 (0.7) | +2.2 (1.1) |
| ...first-to-last region span >= 6 wk, % | 14.9 | 21.4 | +6.5 (1.9) | +9.3 (2.4) | +8.5 (1.6) |
| Station panel: indie Top-10 lead >= 2, % (history 18.5) | 20.5 | 21.0 | +0.5 (3.6) | -1.3 (1.6) | +5.5 (3.2) |
| Station panel: peak 41-100 lead >= 2, % (history 16) | 30.9 | 37.7 | +6.8 (1.4) | -0.1 (1.1) | +1.4 (0.9) |
| Station panel: Big-5 Top-10 lead >= 2, % (history 0) | 3.7 | 5.1 | +1.4 (2.5) | +7.8 (5.7) | +17.5 (8.9) |
| Regional top-11 overlap, % | 54.7 | 29.0 | -25.8 (1.5) | -2.5 (1.5) | -1.9 (0.3) |
| Regional top-10 entries never on the Hot 100, % (history 12-15 city) | 0.00 | 0.25 | +0.25 (0.07) | 0 | -0.16 (0.09) |
| Late-exposed regions' share of live-single units, % | 6.7 | 11.2 | +4.5 (0.3) | +6.3 (0.3) | +8.1 (0.8) |

Station sheets, Top 40 format, 1960, seeds 1001-1002 (history: same city 69.7, cross city 40.3,
cross-city absent 17.8):

| | Same-city overlap | Cross-city overlap | Cross-city absent |
|---|---:|---:|---:|
| ctl | 69.1 / 68.9 | 48.3 / 49.2 | 15.3 / 16.3 |
| lre (λ = 1) | 69.1 / 68.6 | 31.0 / 33.3 | 35.3 / 31.8 |

Read-out:

- **Volume is neutral, the mechanism works, and the regions over-diverge.** Local lists create the
  regional-to-national path the model lacked: a regional top-10 before national Top 40 (2% to 14% of
  indie Top-40 records), a wider regional stagger, and 60% more indie records reaching the Top 40.
  Seven wholly local feedback loops behave like seven independent MusicLab worlds, however. Regional
  overlap halves, cross-city station overlap falls well under history, and national hits lose shelf
  in the regions that locked onto other records. The top flattens (million sellers 5.75 to 2.0),
  which is the wrong direction for a model whose #1s already sell under history.
- **Majors lose 4 points of units.** The static estimate was -1.1; feedback quadruples it. Their broad
  launches leaned on national-rank shelf everywhere. The Big-5 Top-10 share moves from the profile's
  +5 overshoot to history (34.9 vs 34.2). That is welcome on the chart, but it is not "untouched".
- **The station-panel indie lead does not move** (20.5 to 21.0, already at history through the DJ picks).
  The low-peak (41-100) lead worsens (31 to 38; history 16). Local lists add early local hits that
  never get past #41.
- **Narrow launches remain a cost, not a breakout mechanism.** Base 0.1 moves Major units +2.7 with or
  without local lists. With them it gives Big-5 records city leads in 22.5% of cases (history 0),
  because Majors are now held at launch in some regions. The hypothesis that local lists make narrow
  launches cheap is rejected.
- **Local-only hits stay near zero (0.25%),** as section 3's arithmetic requires.

### Blended lists (λ = 0.5, 0.3): 4 seeds, 52 weeks, `SimLogs/lre52-v3`, paired against `lre52-v2` ctl

Rack jobbers and the one-stops that supplied jukebox operators bought from the national trade charts;
record shops followed the local station survey. `--local-retail-exposure=λ` sets each term's
position to the geometric mean of its national position and the region's list position, weighted
λ to the local one. An unlisted record counts as #101; a blend beyond #100 is uncharted. At λ = 1 the
switch is byte-identical to the bare flag (checked).

| Metric | ctl | λ = 0.3 | λ = 0.5 | λ = 1 | λ = 0.3 - ctl | λ = 0.5 - ctl |
|---|---:|---:|---:|---:|---:|---:|
| Single units, M | 120.94 | 121.03 | 121.07 | 121.37 | +0.08 (0.03) | +0.13 (0.06) |
| LP units, M | 38.68 | 38.70 | 38.74 | 38.74 | +0.02 (0.30) | +0.06 (0.11) |
| Major unit share, % | 35.74 | 33.95 | 32.90 | 31.75 | -1.79 (1.40) | -2.85 (1.21) |
| Indie-family unit share, % | 33.91 | 35.00 | 37.49 | 37.52 | +1.09 (1.08) | +3.57 (2.33) |
| Big-5 Top-10 slots, % (history 34.2) | 39.38 | 37.50 | 35.91 | 34.86 | -1.88 (1.66) | -3.46 (3.40) |
| Ten largest sellers' unit share, % | 28.46 | 27.01 | 26.74 | 24.58 | -1.45 (0.94) | -1.72 (1.01) |
| Live singles over 1M | 5.75 | 4.50 | 3.75 | 2.00 | -1.25 (0.95) | -2.00 (0.71) |
| Closures | 34.75 | 37.25 | 39.50 | 39.25 | +2.5 (2.7) | +4.75 (3.5) |
| ...of opening Independents | 20.50 | 24.00 | 25.50 | 23.50 | | |
| Release median units (12-wk cohort) | 3,006 | 2,953 | 2,958 | 2,858 | -53 (108) | -48 (138) |
| Indie national Top-40 records | 112.5 | 143.5 | 158.8 | 180.0 | +31.0 (4.7) | +46.3 (2.1) |
| ...regional top 10 >= 2 wk before national Top 40, % | 2.2 | 5.3 | 7.4 | 13.7 | +3.0 (1.2) | +5.2 (0.8) |
| ...region span >= 6 wk, % | 14.9 | 26.3 | 25.3 | 21.4 | +11.4 (1.0) | +10.4 (2.6) |
| Station panel: indie Top-10 lead, % (history 18.5) | 20.5 | 25.4 | 22.4 | 21.0 | +5.0 (5.5) | +1.9 (4.5) |
| ...fixed 2+1 panel | 9.6 | 16.3 | 15.5 | 11.6 | +6.7 (3.7) | +6.0 (3.5) |
| Station panel: peak 41-100 lead, % (history 16) | 30.9 | 33.2 | 35.8 | 37.7 | +2.3 (0.7) | +4.9 (1.0) |
| Station panel: Big-5 Top-10 lead, % (history 0) | 3.7 | 6.0 | 7.0 | 5.1 | +2.3 (1.8) | +3.3 (4.0) |
| Regional top-11 overlap, % | 54.7 | 45.0 | 40.7 | 29.0 | -9.7 (1.1) | -14.0 (1.6) |

Station sheets, Top 40, 1960 (seeds 1001 / 1002; history same 69.7, cross 40.3, cross absent 17.8):

| | Same-city overlap | Cross-city overlap | Cross-city absent |
|---|---:|---:|---:|
| ctl | 69.1 / 68.9 | 48.3 / 49.2 | 15.3 / 16.3 |
| λ = 0.3 | 70.7 / 68.4 | 39.3 / 44.0 | 19.2 / 15.6 |
| λ = 0.5 | 67.3 / 70.0 | 37.7 / 40.2 | 21.5 / 18.8 |
| λ = 1 | 69.1 / 68.6 | 31.0 / 33.3 | 35.3 / 31.8 |

`lre05_nb` (λ = 0.5 with launch base 0.1) repeats the λ = 1 finding: narrow launches put Major units
back up (+2.7, SE 1.2 against λ = 0.5) and cost indie Top-10 records (-6.8).

## 7. Conclusions

1. **The economy-safe design space is narrow and now explicit:** within-region permutations and time
   shifts. Mechanisms that add or remove demand mass will always move label economics, however
   historical their story. Astra's bounded audience removed mass by accident: its bound never bound,
   and it replaced awareness growth with a near-zero contact process.
2. **The national chart was doing a job that belongs to local retail.** Keying shelf, floors,
   discovery visibility, rack access, restocks and the post-peak clock to a blend of national and
   local lists is volume-neutral (singles +0.1%, LPs +0.2% at λ = 0.5). It opens a regional-to-national
   path the model did not have. At λ = 0.3-0.5 it puts both station-sheet overlaps on the 1960
   surveys, which the control misses by 8 points cross-city. It also removes most of the Big-5 Top-10
   overshoot of this research profile.
3. **It is not free.** Majors lose 1.8-2.9 points of units, and the top flattens (fewer million
   sellers, on a model whose #1s already sell under history). Closures among opening Independents
   rise 3-5 per year, because units move from median releases to local winners. The low-peak lead
   excess, already about 2x history, grows. Four seeds give SEs of 1-3.5 points on these; adoption
   needs 8.
4. **Leads and local-only hits need a city layer, not more demand.** The macro-region arithmetic
   (section 3) makes every big-region top-10 a Hot 100 record. Section 6's city decomposition of
   settled units, which changes station lists and not regional totals, is the next build.

Recommended next screen: λ = 0.3 and 0.4 on eight seeds. Report the paired airplay offset, units-only
Big-5 share, closures by opening tier and million sellers, alongside the regional metrics here.
Then the city decomposition on top of the chosen λ.

## Reproduction

```powershell
dotnet build 'Label Man.csproj' --no-restore
$py = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe'
& $py SimTools/run_local_consumption_screen.py --tag NEW --weeks 52 --seeds 1001,1002,1003,1004 --arms ctl,lre,lre05,lre03 --jobs 3 --observe
& $py SimTools/analyze_local_consumption.py NEW
& $py SimTools/analyze_local_retail_exposure.py NEW
& $py SimTools/analyze_station_playlist_overlap.py NEW-ctl-1001 NEW-lre05-1001
```

Excluded batches: `lre12-v1` and the aborted `lre52-v1` ran before the first-week seed (week 1 cleared
half the purse). `lre12-v2` ran before the mass normalizer. Generated logs stay in `SimLogs/` and are
not committed.

## 6. What local retail exposure cannot do, and the next layer

Local lists change who sells where. They cannot make a region's top-10 record miss the national
chart: at macro-region scale the arithmetic in section 3 forbids it. History's leads and local-only
hits are **city** phenomena. A Cleveland #1 sells perhaps 3,000 copies, under the national #100,
while the Great Lakes macro-region's #10 sells 10,600. The previous replay found a breadth-weighted
national chart could not open a lead (`breadth-chart-cannot-open-local-lead`), and the same
arithmetic says why. With seven regions, the East Coast alone holds 37 of a 110-outlet panel, so an
East Coast-only #1 scores like a national #20.

The next layer is a **city decomposition of each region's settled units for the station lists only**:

```
cityShare(i, c, a) = pop(c) + (1[c = origin(i)] - pop(c)) * exp(-a / tau)
```

Here `origin` is the label's or act's home city in that region, or the city of the first station to
add the record; `a` is the region's local age; `tau` is about 3-4 weeks. Station sheets rank by
`regionalUnits * cityShare`. Regional totals, and therefore the economy, are unchanged by
construction (a permutation of the same units across a region's cities). The only feedback is
through panel airplay (weight 0.085). This is the measurement half of the historical process: a
Cleveland record is on Cleveland surveys first because Cleveland bought it first. It also gives
same-city station pairs a common city signal while cross-city pairs diverge.

The demand half (a city that has not heard the record does not buy it) is a time shift. It is only
economy-neutral if the reached share's lost weeks are relived later. CityReach (§9d) cut indie
demand without reliving it, which is why it handed regional top-11 slots to Majors.
