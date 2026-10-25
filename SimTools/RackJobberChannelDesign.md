# Rack-jobber LP channel: design note

25 October 2026. Follow-up to [RecordClubChannelDesign.md](RecordClubChannelDesign.md) and
[LpDemandDiagnosis.md](LpDemandDiagnosis.md). Design only; no code yet. Author decisions are
recorded at the end. Period sources were read from
seven Billboard issues (1962-67), listed at the end.

## Why a separate channel

With clubs on (rcl-v1/v2, three seeds), 1961 LP units are 119M against Billboard's 173M. The club
row is 45M by construction, so the whole remaining miss sits in stores plus racks: about 74M against
128M (0.58).

Billboard splits the 128M into **stores 68M and racks 60M**. The model's 74M already matches
stores alone. The model has no rack buyers:

- Its one per-region retail capacity is record stores plus department-store shelf
  (`ChartSimulator`, `ChartManager.RegionalPhysicalCapacity`).
- Rack jobbers appear only as `GetRackJobberShelfMultiplier`. That is extra department-store shelf
  for a proven record in a region its label cannot ship to. It adds shelf, not buyers. Department
  stores were also the smallest rack outlet (see below).
- Store LP demand is used up. In lpd-v2 the store market clears all serviceable intent (74.1M of
  74.1M), so more capacity, stock or purchase share cannot add units.
- Slower ordinary-LP decay is ruled out. At .985 the catalogue fills the shared channel, new LPs lose
  24% of launch sales, and the album chart clogs.
- Awareness is the largest in-scope demand shadow. Full awareness raises 1961 raw demand 3.8x,
  against 1.3x for no buyer exhaustion. A rack puts a record in front of people who never see it in
  a record store, so it works on exactly that term.

So the missing piece is a second set of buyers who meet LPs on a rack, mostly for titles that are
already selling. The rack must not keep the whole catalogue alive.

## What the historical rack market was

### Size (Billboard market tables)

From the 6 August 1966 International Buyers Guide (1961-65) and the revised 1969-70 International
Music-Record Directory (30 August 1969, pp. 9-11, 1966-68). Units in millions:

| | 1961 | 1962 | 1963 | 1964 | 1965 | 1966r | 1967r | 1968r |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| LP units, racks | 60 | 76 | 79 | 86 | 106 | 77 | 87 | 93 |
| LP units, stores (non-racks) | 68 | 70 | 66 | 68 | 76 | 56 | 56 | 54 |
| Single units, racks | 40 | 60 | 64 | 71 | 84 | | | |
| Single units, stores | 92 | 101 | 98 | 101 | 108 | | | |
| Racks' share of stores + racks LPs | 47% | 52% | 54% | 56% | 58% | 58% | 61% | 63% |

r = revised series. The 1969 edition revised all figures from 1950 and does not restate 1961-65.
The two series disagree on levels: the 1966 revised LP total is 180M, against 228M for 1965 in the
older guide. Treat levels as soft and the shape as firmer. Both agree that racks grow and stores
stay flat or decline.

- **Racks were LP-led.** In 1961 LPs were 60% of rack units (60 of 100M) and 42% of store units
  (68 of 160M).
- **Racks grew while stores did not.** Rack LPs rose 1.77x from 1961 to 1965.
- **Dollars agree.** Billboard Music Week's research director put record sales at 56% retail
  outlets, 25% racks, 12% clubs and 7% jukebox operators (Billboard, 24 February 1962, p. 14 area).

### The 1962 NARM member survey (Billboard, 16 March 1963, pp. 4 and 79)

NARM, the rack jobbers' trade association, surveyed its members at its fifth convention:

| | 1961 | 1962 |
|---|---:|---:|
| NARM members' retail gross | about $80M | $102.6M |
| All rack business, NARM and non-NARM | | "over $200M" (estimate) |
| Racks serviced by NARM members | about 19,000 | about 24,000 |
| in supermarkets | 7,900 | 9,000 |
| in variety stores | 4,800 | 8,000 |
| in drugstores | | 5,000 |
| in department stores | | 1,500 |
| in service PXs | | 300 |

- **LPs were 74% of NARM gross dollars.** Singles were 19%, children's records (non-LP) 5%, and
  EPs under 2%.
- **Budget LPs were 21% of NARM members' LP business** in 1962 (dollars). 64.6% of members sold
  LPs listing at 99¢ or less.
- **Stereo was 33% of rack LPs** in 1962, against 31% in 1961.
- **Racks discounted.** 93% of members discounted in some locations in 1961.
- **Rack count tracked rack units.** NARM racks rose 26% from 1961 to 1962, mainly in variety
  stores. Billboard's rack LP units rose 27% (60 to 76M).
- **Department stores were the smallest outlet** (6% of NARM racks). Supermarkets, variety stores
  and drugstores held 92%.

### Price, returns and stock

- **Price.** In 1963 Columbia's distributors sold $3.98-list LPs to retailers at $2.25. Rack jobbers
  bought at the same 10% functional discount as large department and general-merchandise stores, so
  some retailers paid what the jobbers paid (Billboard, 16 March 1963; 10 August 1963). Manufacturers
  refused jobbers full-distributor status in 1963, but a label still sold every rack LP through its
  ordinary distribution price.
- **Returns were liberal and high.** Capitol's president Alan Gortikov told the 1965 NARM convention
  that jobbers had "far too high returns" under liberal privileges, and that slow sellers and
  shopworn stock stayed exposed too long (Billboard, 13 March 1965, p. 1). No rate is given. One 1963
  label programme cites a "regular 10 per cent return privilege" for dealers.
- **What the racks carried.** In 1965 a jobber (Schlang, Albany) said about one third of rack
  departments was devoted to best-selling LPs and two thirds to other product. Jobbers drop a
  record "as soon as [it has] passed [its] peak" if the label will not discount it. Members had
  cut the catalogue they carried in depth by 400 titles from four manufacturers after price
  increases (Billboard, 13 March 1965). A jazz label head in 1964 said almost 60% of the business
  went "through racks which only handle the hot chart items" (Billboard, 15 February 1964).
- **Budget product.** A 1965 NARM symposium (Pickwick, MGM, RCA, Miller International, rack jobbers)
  described two kinds of customer, one for full-priced records and one for bargains. Budget growth
  had not kept pace with the industry (Billboard, 13 March 1965).

### Who bought at racks

- **The household shopper.** Capitol's merchandising vice-president Brown Meggs told the 1966 NARM
  convention that the rack customer was the housewife: an impulse buyer, "enticed into making disk
  purchases by a tasteful but arresting album display", cost-conscious but putting quality first
  (Billboard, 19 March 1966, pp. 8 and 10).
- **A separate population.** Billboard research reportedly found most music buyers did not know some
  supermarkets sold records (secondary, via the cut-out blog below).

### What is still unknown

1. **Titles per rack and rotation.** The sources give space shares (one third hits) but no count.
2. **Budget share in units.** 21% is dollars. At assumed prices of about $1.25 per budget LP and
   $3 per discounted front-line LP, it is about 39% of rack LP units; at $1.75 about 31%.
3. **The return rate.** Only "far too high".
4. **The chart panel.** Billboard's LP chart captions say "Compiled from national retail sales"
   (1965, 1967). None of the issues read says whether jobbers reported.
5. **National rack count.** NARM had 24,000 racks for about half of all rack dollars. If non-members
   ran similar racks, the national count was about 45-50k in 1962. That would be about 1,600 LPs
   per rack per year, or 30 a week. This is an inference, not a source.

## Proposed mechanism (flag `--rack-jobbers`, off by default)

A second, additive LP sales pass per region, after store clearing. It runs beside the LP capacity
increment (`ChartManager.ApplyAdditionalLpCapacity`). Unlike the club channel, the rack sells
**specific live records** to its **own buyer pool**, so units are emergent, not authored.

### 1. Rack assortment (per region, weekly)

The sources split rack space into one third best sellers and two thirds other product. The rack
has two tiers:

- **Eligible.** Live LPs that are either shipped to the region by their label or proven there:
  `GetRackJobberAccess(position, peakBreakoutScore) > 0` (national Top 100, or a regional breakout).
  Soundtracks and cast albums are eligible.
- **Hit tier.** The top `N_hit(year)` eligible titles by trailing four-week regional sales (store plus
  rack), ties broken by record ID. High exposure.
- **Catalogue tier.** The next `N_cat(year)` titles that have charted or broken out, still have
  positive trailing sales, and are past their peak. Lower exposure. Live LPs only: retired LPs never
  return to the rack in v1.
- **Hysteresis.** A title stays in its tier while it ranks inside 1.25 times the tier size, so the
  rack does not churn weekly. When its velocity fades it drops out, as jobbers dropped records past
  their peak.
- **Sizes.** Titles per rack are unsourced. Start with about 25 hit titles and 50 catalogue titles
  in 1960, growing with rack reach. The space shares give the ratio, not the level.
- **Effect.** Neither tier extends LP lives in the store channel or the album chart. The catalogue
  tier sells only live, still-selling titles, so the live record count should not rise.

### 2. Rack buyer pool (per region)

Rack shoppers are household shoppers, mostly women, buying on impulse. They are not the youth-weighted
record-shop population.

- `rackPool = population × incomeFactor × rackReach(year) × RackShopperScale`.
- `rackReach(year)` is the rack outlet curve. It reuses `GetRackJobberEraWeight` (0.30 in 1960 to 1.0
  by 1969). That gives +21% from 1961 to 1962 against +26-27% in the NARM rack count and rack units,
  and 1.62x from 1961 to 1965 against Billboard's 1.77x. Close enough for v1; the analyzer reports
  the gap.
- **Genre mix** comes from the region's audience-segment shares, weighted toward AdultMOR,
  FamilyChildrens and Country, with a smaller youth slice. This uses the soundtrack pool
  (`GetSoundtrackAlbumMarketSize`) as precedent. It does not use `GetBuyingPopulationPercentage`,
  which is youth × income × urbanization.
- **`RackShopperScale` is the channel's one calibration scale.** Fit it once on seed 1001 to
  front-line rack LPs in 1961 (see decision 5), then freeze it. 1962-69 follow from reach,
  assortment and supply. They are reported against Billboard, not tuned.

### 3. Rack demand (per racked title, region, week)

`rackDemand = rackPool × genreWeight(title) × exposure(tier, rank) × conversion × rackExhaustion`

- **Exposure** replaces record-shop awareness. Being on the rack is the awareness, as in Meggs's
  "arresting album display":
  `exposure = max(rackExposure(tier, rank), record awareness)`. The hit tier's top ranks face out.
- **Conversion** reuses the album terms that describe the record: appeal^2.5, word of mouth and
  seasonality. It drops the store-only terms: the label-tier demand scale, the distribution
  difficulty and the launch-week ramp. A rack only takes proven titles, so a launch ramp would count
  twice.
- **Exhaustion** uses its own ledger: rack units to date ÷ (rack pool × genre weight), per title and
  region, through `CalculateAlbumExhaustion`. Store penetration is untouched, and rack sales do not
  exhaust store buyers. The hold arm tests that assumption.
- **Shelf** limits weekly rack units per region: `racks(region, year) × unitsPerRackWeek`, with racks
  proportional to population × `rackReach(year)`. It does **not** use `departmentStoreCount`:
  department stores held 6% of NARM racks. The inferred ~30 LPs per rack per week sizes it.
  Report how often it binds. It should be slack: demand, not shelf, should set volume.

### 4. Supply and economics

- **Stock.** Rack units ship from the label's ordinary pressings. With the physical inventory arm
  off, there is no extra constraint, as for stores today. With it on, rack units draw on the same
  regional stock.
- **Price.** Jobbers bought at the 10% functional discount off the distributor-to-retailer price,
  the same as large retailers. The label sold through its ordinary distribution either way. In v1
  the label books a rack unit at its ordinary LP wholesale net through
  `LabelCompanyAccounts.RecordSale`, with the ordinary artist royalty. Jobber margins, retailer
  discounts and shelf discounting are outside the label's books.
- **Returns.** Real and high, but the rate is unsourced. v1 books no returns charge. Telemetry
  reports rack units so that a rate can be applied later. A returns arm at 10% and 20% sizes the
  cost to labels.

### 5. What rack sales count toward

| | v1 default | Reason |
|---|---|---|
| Record units, label income, artist earnings | Yes | They are sales of that record. |
| Solvency and the "Rising" test | Yes | Ordinary sales. |
| LP format memory and release forecasts | Yes (decision 3) | Racks were about half of retail LPs, and labels planned for them. |
| Album chart points | **No** (decision 2) | Panel unverified ("national retail sales"). Including them creates a loop: racked because charting, charting because racked. That is the lpd-v2 clog. A test arm includes them. |
| Store exhaustion, word of mouth | No | Separate buyers in v1. |

### 6. State, determinism, performance

- **Saved, only when on.** Per title and region: rack units to date. Per region: tier membership.
  The fields are omitted when null, so flag-off saves stay byte-identical. A rack world refuses to
  resume without the flag.
- **No random draws.** Rounding is largest-remainder in stable ID order. When off, the run must
  byte-compare to the control.
- **Cost.** Ranking uses only titles with sales in the last four weeks, about 1-2k per region. The
  demand loop runs over at most `N_hit + N_cat` titles per region. No records are kept alive by the
  rack. lpd-v2's +25% live records and +12% runtime are the failure to watch for.

## Out of scope for v1

- **Rack singles.** Rack singles were 40M in 1961. The model's single units (137M) already sit at
  stores plus racks (132M), because the regional single capacity was sized to that total. Adding rack
  singles would overshoot. They stay implicit in the single calibration (decision 1).
- **Budget lines.** Budget LPs were 21% of rack LP dollars and perhaps 30-40% of rack LP units. The
  model has no budget lines, so v1 cannot sell them. A v2 budget reissue line could draw on the club
  catalogue ledger, which already holds retired LP appeal by label and act. Pickwick-style
  independents and the Majors' own lines (Camden, Harmony, Vocalion, Wing) both existed.
- **Player.** A rack placement or jobber relationship for the player's label is a later feature.

## Telemetry and diagnostics (no acceptance gate)

- **`rack-region-weekly.csv`:** region, rack pool, racks and shelf, titles per tier, rack units by
  tier, and whether demand or shelf bound.
- **`rack-title-weekly.csv`:** record, region, tier and rank, exposure, rack demand, units, age, label
  tier, chart position, and whether its label ships there.
- **Analyzer comparisons:**
  - annual rack LP units against 60/76/79/86/106M and the revised 77/87/93M, with the budget share
    taken out (decision 5);
  - growth of rack units against the rack-count growth (+26% 1961-62);
  - store LP units and single units against the clubs control (they should be unchanged);
  - racks' share of stores-plus-racks LPs against 47-63%;
  - the hit tier's share of rack units against "one third of space";
  - rack units by label tier, and the share going to labels with no distribution in the region (the
    regional-hit route);
  - album chart: distinct entrants and first-spell length against control (the clog check);
  - live record count, runtime, closures, and Major share.

## Native checks

- Off: no rack pass, no saved fields, nothing booked.
- Tiers hold only eligible titles, are deterministic, and hysteresis keeps a title until it drops
  outside 1.25 times its tier.
- A title outside the rack sells zero rack units; retired titles never appear.
- Rack exhaustion is monotone, and store penetration is unchanged by rack sales.
- Shelf caps units exactly.
- Booking reconciles to label cash and artist earnings.
- Rack units never add album chart points with the default flags.
- Full-world save/load round-trip with the flag on.

## Run plan

1. Build on the rcl base: lpc-v1-both plus `--record-clubs`. Reuse rcl-v1/v2 clubs arms as controls
   if the assembly hash allows.
2. **rkj-v1:** seed 1001, 106 weeks. Fit `RackShopperScale` once and freeze it.
3. **rkj-v2:** seeds 1002-1003 on the frozen scale.
4. Test arms, one seed each:
   - `--rack-chart-points`, to size the chart loop;
   - `--rack-shared-exhaustion`, where rack and store sales share one penetration ledger, to size
     overlap between the pools;
   - `--rack-returns=.1|.2`, to size the returns cost.
5. Decade A/B at audit end. The rack trajectory to 1969 only shows over the decade.

## Author decisions (25 October 2026)

The author accepted all seven recommendations.

1. **LP only.** Rack singles stay inside the single calibration. The model's singles already sit at
   stores plus racks, so adding them would overshoot.
2. **Chart points off by default.** Rack units add no album chart points. `--rack-chart-points` stays
   as a test arm. The captions do not settle whether jobbers reported.
3. **Rack income feeds release decisions.** Rack sales enter LP format memory and release
   forecasts, unlike club income. Racks were about half of retail LPs, and labels planned for them.
4. **Emergent with one scale.** Volume comes from the rack buyer pool, assortment and supply, with
   `RackShopperScale` fitted once and frozen. There is no authored volume table.
5. **Front-line calibration target.** Fit 1961 to front-line rack LPs only, about 36-42M (60M less a
   30-40% budget share). The budget gap stays visible in the analyzer. Fitting to the full 60M would
   make front-line titles sell budget buyers' records.
6. **Budget lines in v2.** A budget reissue line draws on the club catalogue ledger. It is not part
   of v1.
7. **Store capacity unchanged.** The existing store capacity and `GetRackJobberShelfMultiplier` stay
   as they are, for comparability with every earlier run. The rack shelf uses its own
   population-scaled rack count.

## Sources

Billboard issues (worldradiohistory.com archive; text extracted from the scans, so page numbers
are approximate):

- [24 February 1962](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1962/Billboard%201962-02-24.pdf):
  sales by outlet in dollars (56/25/12/7%).
- [16 March 1963](https://worldradiohistory.com/Archive-All-Music/Billboard/60s/1963/Billboard%201963-03-16.pdf):
  1962 NARM survey; jobbers at the 10% functional discount; full-distributor dispute.
- [10 August 1963](https://worldradiohistory.com/Archive-All-Music/Billboard/60s/1963/Billboard%201963-08-10.pdf):
  Columbia's $2.25 distributor price for $3.98 LPs; rack margin dispute.
- [15 February 1964](https://worldradiohistory.com/Archive-All-Music/Billboard/60s/1964/Billboard%201964-02-15.pdf):
  racks "only handle the hot chart items".
- [13 March 1965](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1965/Billboard%201965-03-13.pdf):
  Gortikov on returns; one-third best sellers; budget symposium; LP chart caption.
- [19 March 1966](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1966/BB-1966-03-19.pdf):
  Meggs on the housewife shopper; computerized rack inventory.
- [18 March 1967](https://worldradiohistory.com/Archive-All-Music/Billboard/60s/1967/Billboard%201967-03-18.pdf):
  LP chart caption; NARM moving into tape cartridges.
- 6 August 1966 International Buyers Guide and 30 August 1969 International Music-Record Directory:
  unit tables (see [RecordClubChannelDesign.md](RecordClubChannelDesign.md)).

Secondary:

- [Rack jobber (Wikipedia)](https://en.wikipedia.org/wiki/Rack_jobber)
- [Budget album (Wikipedia)](https://en.wikipedia.org/wiki/Budget_album)
- [Sutton Records (Wikipedia)](https://en.wikipedia.org/wiki/Sutton_Records)
- [Music Weird: the cut-out controversy](https://musicweird.blogspot.com/2014/09/the-controversy-over-cut-out-records-in.html?m=1)
