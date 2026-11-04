# Record-club catalogue channel: design note

25 October 2026. Follow-up to [LpDemandDiagnosis.md](LpDemandDiagnosis.md). Implemented
behind `--record-clubs` (v1); results from `rcl-v1` are at the end.

## Why a separate channel

Billboard's 6 August 1966 International Buyers Guide splits 1961 LP units into stores
68M, racks 60M, and **clubs and mail order 45M** (1962-65: 45, 41, 38, 46M). The model
has no club or mail-order channel. Its one per-region retail capacity (record stores plus
department-store shelf) is what lpd-v2 measured at 74.1M in 1961.

lpd-v2 showed that the catalogue tail cannot be added through slower ordinary-LP decay in that
shared market. At .985, back catalogue fills the album channel. New LPs then lose 24% of their
launch-window sales, the album chart clogs, and the run carries 25% more live records. The
decay curve exists to keep LPs from piling up, so a club channel has to sell catalogue **without
keeping records alive**.

## What the historical club market was

From the FTC's 1967 Columbia Record Club decision
([72 F.T.C. 320-411](https://www.ftc.gov/sites/default/files/documents/commission_decision_volumes/volume-72/ftcd-vol72july-december1967pages320-411.pdf);
page numbers are the volume's):

- **Who ran clubs.** Columbia entered in August 1955 (p. 323). By the early 1960s the "big three"
  integrated manufacturers, Columbia, RCA Victor and Capitol, each had a club. The RCA club was
  administered by Reader's Digest (p. 348 n. 44).
- **Size.**
  - The Columbia Record Club sold $41.5M in 1961 and $53M in 1962 (p. 324).
  - In 1962 it shipped 22.3M LPs, so $2.38 per LP (p. 348 n. 42).
  - All club sales were about $100M in 1962, against a $620M all-record market (p. 348).
  - Columbia had 56.1% of club sales in 1960 and at least 53% in 1962 (p. 347). Columbia itself
    put its 1961 share at 41.5% of club dollars (p. 321).
  - The RCA club sold about $14M in 1961 and $22.7M in 1962 (p. 348 n. 44).
- **Licensing.**
  - From May 1958 Columbia licensed the masters of nine medium-sized and smaller labels
    exclusively. It paid a "royalty" of about 17.8¢ per record. Its total cost per licensed record
    was about 87.5¢, against $1.60 or more paid by retailers (p. 323).
  - The contracts also fixed (depressed) the royalty licensors paid their artists on club sales
    (p. 323).
  - Licensed labels were 30.9% of Columbia club dollars in 1961, and 36.3% of its units in 1962
    (pp. 347-348).
- **Price and cost.**
  - A $3.98 popular LP cost club members about $2.37 including postage. Dealers charged about
    $2.98 on average (pp. 351, 354).
  - The Columbia club's cost per licensed record shipped was $2.13 (p. 354).
  - In 1961 Columbia's average price to its own wholesalers was $1.93 per LP: 16.6M LPs for
    $30.3M (p. 354 n.).
- **Who joined.**
  - Members were mostly young adults who had just bought a phonograph, were unsure of their
    taste, and were used to buying by mail (finding 19, p. 351).
  - There were three to four million members, typically committed to 12 records a year (p. 352).
  - The long-run "shrinkage rate" was 54.13%: of each year's new members, 54 in 100 resigned
    within the year (finding 20, p. 352).
- **What clubs sold.** Name artists and "hits", not repertoire (p. 349). A club offered 200-400
  records a month, under 2% of the roughly 25,000 in stores (p. 352 n. 65).
- **Packages are separate.** Reader's Digest sold multi-LP mail-order packages of standard
  repertoire by little-known performers: about $12M a year in 1960-62, at $1.33-2.33 per LP
  (pp. 322, 348).

**Cross-check against 45M.** In 1961 Columbia sold about 17.5M club LPs ($41.5M at $2.37). At a
41.5-56% share, the clubs sold about 31-42M. Packages and other mail order add roughly 5-9M.
That gives about 36-51M, consistent with Billboard's 45M.

## Proposed mechanism (flag `--record-clubs`, off by default)

The model already has a precedent in `MajorOffChartSingles` ([Systems/MajorOffChartSingles.cs](../Systems/MajorOffChartSingles.cs)).
It books per-label units every week, belonging to no record, through the label's ordinary
accounts. The club channel follows that pattern with three differences: its volume is LP-only,
it is split by **catalogue state**, and that state persists and is saved.

### 1. Catalogue ledger (aggregate, per label and artist)

- **Entry.** `ClubCatalogueEntry { labelId, artistId, titles, appeal }`. Per-label totals are
  cached.
- **Folding in.** Retired LPs fold in through `ChartManager.OnRecordRetired`.
  - `appeal += lifetimeUnits × nameWeight(artist career state at retirement)`.
  - Soundtracks and cast albums fold in the same way.
  - The record itself is then discarded as now; the ledger keeps no record IDs.
- **Live LPs.** These are eligible after a club holdback of H weeks: appeal is units to date,
  recomputed monthly from live records. That's a cheap pass over about 5k LPs. They leave the
  live term when they retire and enter the ledger.
- **Decay.** Monthly, `appeal ×= d^(1/12)`. Entries below a floor are pruned, so the ledger is
  bounded by the number of acts with a selling LP, not by titles.
- **Masters move with ownership.**
  - An act changing label leaves its old catalogue with the old label.
  - When a label is absorbed (`LabelLifecycleManager.MarkLabelAcquired`), its ledger merges
    into the acquirer's.
  - At closure, the ledger is dropped.
- **Opening seed.** The 1960 world has no retired LPs, but Columbia's club sold the 1950s
  catalogue. Historical labels get a synthetic opening appeal with no artist rows, sized so the
  1960 split is plausible (Columbia about 56%). It decays like the rest. This is a calibration
  assumption and must be reported apart from earned catalogue.

### 2. Operators and licences

- **Operators.**
  - Columbia operates from the start. RCA Victor and Capitol were running clubs by the
    early 1960s, so all three are open at the 1960 start. Their exact founding dates are not
    sourced here.
  - In an emergent world, an operator is an active AI Major with no distribution deal and opening
    or earned appeal above a threshold.
- **Offer list.** Each month an operator offers its top N entries by appeal (N about 300), from
  its own catalogue plus licensed ones. Concentrating on the top N is the "name artists, 200-400
  records" rule. It also keeps allocation cheap.
- **Licences.**
  - Each year, an operator offers exclusive licences, in a deterministic order (appeal rank,
    then label ID), to non-operator labels. Up to about nine per operator.
  - A label holds at most one licence. Its term is several years. Accepting is a rule: a label
    accepts when the licence income beats nothing, which it always does at zero cost. There's no
    negotiation in v1.
  - Mercury, a model Major, can be a licensor. The FTC lists it among Columbia's.

### 3. Volume and allocation

- **v1 volume is authored** from two Billboard directories:

  | Year | 1961 | 1962 | 1963 | 1964 | 1965 | 1966 | 1967 | 1968 |
  |---|---|---|---|---|---|---|---|---|
  | Clubs and mail order, M units | 45 | 45 | 41 | 38 | 46 | 47 | 49 | 49 |

  - 1961-65 are from the 6 August 1966 International Buyers Guide.
  - 1966-68 are from the
    [1969-70 International Music-Record Directory](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1969/Billboard%201969-08-30-Buyer.pdf)
    (30 August 1969), pp. 10-11.
    - Every club unit is an LP: the all-format and LP club rows are identical.
    - Clubs were 25.0% of 1968 LP units and 20.5% of all-format list dollars ($231M).
  - 1960 is unsourced: use 1961's 45M. 1969 is unsourced: hold 1968's 49M.
  - **The two directories disagree on everything except the clubs.** The 1969 edition says all
    figures from 1950 on were revised. Its 1966 LP total is 180M (stores 56, racks 77, clubs 47),
    against 228M for 1965 in the 1966 guide. Its 1966 all-format total is 363M, against 477M for
    1965. Only the club row is consistent across both editions. The 1961 stores-plus-racks figure
    of 128M is therefore a soft benchmark: the 1969 edition does not restate 1961 units.
  - Context:
    - [Music Business, 16 May 1964](https://worldradiohistory.com/Archive-All-Music/Music-Business/Music-Business-1964-05-16.pdf)
      put clubs at roughly a fifth to a quarter of U.S. LP sales.
    - ABC-Paramount joined the Capitol club in 1965.
    - Capitol sold its club to Longines-Wittnauer in December 1968
      ([Record club](https://en.wikipedia.org/wiki/Record_club)).
  - Membership in 1959, as a sanity check for v2
    ([Billboard, 5 October 1959](https://www.americanradiohistory.com/Archive-All-Music/Billboard/50s/1959/Billboard%201959-10-05.pdf)):
    Columbia about 1.3M, RCA Victor's three clubs close to 600k, Capitol about 250k.
  - Weekly units are annual/52. This is an exogenous channel volume, like the off-chart
    singles share. It adds units without the model having earned them, and the doc and
    telemetry must say so.
- **v2 volume is a member stock** (open question): enrolment from phonograph adoption among
  young adults, 54% annual churn, about 12 records per member a year, and volume scaled by how
  strong the operators' offers are. Only v2 can show a world whose majors have weak catalogues
  selling fewer club records.
- **Package slice.** Mail-order packages are in the channel, at 15% of annual volume (about
  7M of 45M in 1961, the midpoint of the 5-9M estimate). They sold standard repertoire by
  little-known performers, so the slice is allocated across **all** labels' ledgers, not just
  the operators' offer lists:
  - weight = appeal × the genre's adult-segment share (AdultMOR + JazzHiFiClassical +
    FamilyChildrens), with no name weight;
  - the label books it as a wholesale sale to an outside packager. The packager (Reader's
    Digest, Life, Book-of-the-Month Club) is not a model label.
- **Allocation.** The club slice goes to operators in proportion to offer-list appeal, then to entries within
  each offer list in proportion to appeal. Rounding is largest-remainder, in stable ID order, as in
  `MajorOffChartSingles.Allocate`. There are no random draws.
- **Additive to retail.** The FTC treats clubs as a separate submarket, and Billboard counts them
  separately; see decision 1 for what the store pool contains. A test flag `--record-club-hold` takes club units out of the album channel
  capacity instead. Finding 21 says a member buys the specific club hits from the club rather
  than a store, so some overlap is real.

### 4. Economics (period dollars; scaled by the model's price table)

| Per club LP | Own catalogue (operator) | Licensed catalogue |
|---|---|---|
| Club gross to operator | $2.37 | $2.37 |
| Club operating cost (mailing, advertising, bonus records, bad debt) | $1.255 | $1.255 |
| Pressing, mechanical | model LP costs | inside 87.5¢ |
| Paid to licensor | — | 17.8¢ |
| Artist royalty | own rate × club factor | licensor pays, × club factor |

- **Package LPs.** Reader's Digest packages sold for $1.33-2.33 per LP. The label's price to
  the packager is unsourced. v1 assumes the licensed-club economics: the label receives
  17.8¢ per LP plus pressing at cost, and pays its artists at the club factor.
- $1.255 is the FTC's $2.13 total cost less the 87.5¢ acquisition cost. Licensed records then
  net the operator about 24¢.
- The **club factor** on artist royalties is an assumption (for example 0.5). The FTC confirms
  the rate was depressed but this extract gives no figure.
- **Booking path.**
  - Operator and licensor book through the `CompetitorManager` weekly settlement, beside
    `SettleOffChartSingles`, using `LabelCompanyAccounts.RecordSale`.
  - Club receipts are never deferred.
  - Artist royalties go to the entry's artist.
- **What club sales stay out of in v1:**
  - Chart points, release outcomes and record priors. Billboard's LP chart was a retail survey.
  - LP format memory and release-margin forecasts. Clubs offered only the big releases, so this
    is historically defensible as well as cheaper (decision 3).
  - The "Rising" momentum test, like the off-chart singles base. Solvency does count them.
- **Player.** Out of scope for v1. A club licence offer to the player's label is an obvious later
  feature.

### 5. State, determinism, performance

- **Saved.** Ledger entries, opening-seed appeal, operators, and licences (licensor, operator,
  expiry). Cumulative telemetry is not saved.
- **Determinism.** No random draws. When off, the run must byte-compare to the control.
- **Cost.**
  - O(entries) monthly decay.
  - O(operators × N) weekly allocation.
  - One monthly scan of live LPs.
  - No retained records. Expect a few thousand entries by 1969.

## Telemetry and diagnostics (no acceptance gate)

- **`record-club-weekly.csv`:** operator, own and licensed units, gross, net, and artist
  royalties.
- **`record-club-catalogue-annual.csv`:**
  - entries and appeal by label, split into opening seed and earned catalogue;
  - licences in force;
  - share of appeal from LPs over two years old.
- **Analyzer comparisons with the FTC and Billboard figures:**
  - annual units against 45M;
  - operator shares against 53-56% for Columbia;
  - Columbia licensed share against 31-36%;
  - gross per LP against about $2.38;
  - Big-5 LP revenue share, closures and Major share against control.

The opening seed's contribution must be shown separately.

## Native checks

- Off: allocation is empty, and nothing is booked.
- Allocation sums exactly and is stable under label-ID order.
- A retired LP folds once; decay and pruning are monotone.
- Absorption merges the ledger; closure drops it.
- Licences are exclusive and expire on time.
- Booking reconciles to label cash.
- Full-world save/load round-trip with the flag on.

## Run plan

1. Run `rcl-v1`: the lpc-v1-both base, observe against `--record-clubs`, 106 weeks, seed 1001.
   Reuse lpd-v2 observe as the control if the assembly hash allows.
2. Then two more seeds, then a decade A/B at the end of the audit.
3. One hold-flag arm to size cannibalization.

## Author decisions (25 October 2026)

1. **Additive to retail, with one condition on how retail is calibrated.** The author was unsure
   whether the store pool counts club buyers, so the code was traced (25 October 2026):
   - **The pool is defined by population, not channel.**
     - `MarketRegion.GetBuyingPopulationPercentage` is youth × income × urbanization × 0.032. It
       has been unchanged since the first upload, and no channel enters it.
     - The LP buyer pool (`GetAlbumMarketSize`) multiplies it by genre acceptance, segregation,
       album affinity and LP purchase willingness.
     - Nothing in the construction distinguishes a store buyer from a club member.
   - **Its scale was tuned to a singles-era market and an LP share curve, not to an outlet total.**
     - The weekly regional purchase capacity is buyers × 1.34 (`ChartManager`). It was sized in
       July 2026 to 150.9M units a year, inside an accepted 140-175M band, when LPs were 1-2% of
       model units. That is roughly the stores-and-racks singles market (132M in 1961).
     - The album channel sits on top as a share of that capacity, fitted to the author's LP unit
       share curve: 30% in 1960 to 55% in 1969.
     - That curve is below Billboard's LP share however the channels are cut. In 1961 it was 49%
       of all units, and 49% of stores and racks alone. So the curve does not encode "clubs
       excluded".
   - **In units, the club channel does not double-count today.** The model's 74.1M LPs in 1961 sit
     below stores plus racks alone (128M, soft). Adding 45M of club units cannot overshoot.
   - **Condition.** Any future recalibration of the retail LP pool must target stores plus racks,
     not an all-channel LP total or share. Otherwise this channel double-counts.
   - `--record-club-hold` stays only as a test arm. Finding 21 says a member buys the specific
     club hits from the club instead of a store, so some overlap is real.
2. **Packages are in.** See the package slice above.
3. **Club income stays out of LP release decisions.** Clubs offered only the big releases, so
   club income was not a serious consideration in deciding whether to issue an LP. The
   top-N offer list already confines club income to the hits.
4. **1966-68 are now sourced** (47, 49, 49M); 1969 holds 49M (see Volume above). Whether v2's
   member stock replaces the authored table is still open.

## Implementation (v1)

- **Code.**
  - [Systems/RecordClubChannel.cs](../Systems/RecordClubChannel.cs): ledger, operators, licences, allocation,
    telemetry and saved state.
  - [Systems/CompetitorManager.RecordClubs.cs](../Systems/CompetitorManager.RecordClubs.cs): weekly booking.
- **Hooks.**
  - Allocation and booking run in the weekly settlement, next to the off-chart singles base.
  - Retired LPs fold into the ledger at `CompetitorManager.RecordRetired`.
  - Absorption merges ledgers at `LabelLifecycleManager.MarkLabelAcquired`.
  - Club net leaves the "Rising" test in `EconomicLabelLifecycle`.
  - The hold arm reduces each region's album channel in `ChartManager`.
- **Save.** State is saved as `CompetitorSaveData.RecordClubs`, omitted when null, so flag-off saves are unchanged. A
  club world refuses to resume without the flag.
- **Simplifications beyond the design:**
  - Name weight is 1: appeal is lifetime units.
  - Live LPs count after a 13-week holdback.
  - Operators are matched by historical label name (Columbia, RCA, Capitol).
  - Club royalties credit acts' earnings without recouping advances, since the act may since have moved labels.
- **Checks.** `--record-club-check` runs 42 native checks.
- **Runner and analyzer.** [RunRecordClubProbe.ps1](RunRecordClubProbe.ps1) and
  [analyze_record_club_probe.py](analyze_record_club_probe.py). The runner passes the arms' GC settings by
  inheritance, because Windows PowerShell 5.1 has no `Start-Process -Environment`.

## Results: rcl-v1, seed 1001, 106 weeks

Control: lpd-v2 observe, same flags without the clubs.

**Checks passed.**
- 42 native checks.
- Flag-off 13-week run: 75 streams byte-identical to the control through week 12.
- 13-week save round-trip with clubs on.
- In both arms: annual club volume exact; licensed units balance; company cash reconciles.

| 1961 | Control | Clubs | Hold |
|---|---|---|---|
| Store LP units | 74.1M | 74.5M | 74.6M |
| Club + package LP units | — | 45.0M | 45.0M |
| **Total LP units** | 74.1M | **119.5M** | 119.6M |
| Single units | 137.2M | 136.9M | 134.7M |
| Major record gross | $144.9M | $236.4M | $238.6M |
| Club split: Columbia / Capitol / RCA | — | 44.8 / 32.2 / 23.0% | 43.7 / 31.9 / 24.3% |
| Columbia's licensed share of its club units | — | 44.8% | 45.5% |
| C4 chart share | .338 | .356 | .322 |
| Closures, run end (all tiers) | 74 | 79 | 84 |
| Run time | 1,581 s | 1,331 s | 1,311 s |

- **LP units rise by exactly the authored volume.** Store LP sales and singles are unchanged within
  noise, as intended for an additive channel. Total LPs reach 119.5M: 0.69x the old guide's 173M. In the
  1969 directory's revised series, the 1966 total is 180M.
- **The hold arm is inert on store LPs.** The store LP market is demand-bound: in lpd-v2 it cleared all
  serviceable intent. Taking 45M of album capacity away therefore costs no LP sales. It did cut 1960
  singles by 8%, 139.6M to 128.1M, because singles share the regional capacity. Additive is the right
  default.
- **Columbia's club gross matches the FTC almost exactly.** 44.8% of 38.25M club LPs at $2.37 is
  about $40.6M, against the FTC's $41.5M for 1961. Club net across all labels is about $15M a year.
- **Two split misses against the FTC:**
  - Capitol is high: 32% of club units against about 8% of direct-mail sales in 1962 (FTC p. 320). RCA is
    near its share (23% against 21%).
  - Columbia's licensed share is high: 45% against 31-36%. Columbia's own catalogue is weaker relative to
    its licensors than history.
  - Both follow the model's catalogue strengths, which came mostly from live LPs by 1962. Not tuned.
- **What the catalogue is made of.** The opening backlist is all of the appeal in 1960, 40% in 1961 and
  16% in 1962. Retired LPs are 7% in 1962. Most of the club's appeal is live LPs past the 13-week holdback
  (recent sellers), which fits the clubs' "hits".
- **Decca is a licensor.** In the model, Decca and Mercury both license their catalogues to a club.
  The FTC lists Mercury among Columbia's licensors; whether Decca licensed historically is unverified.
- **Label economics.**
  - Majors' record gross rises 63% in 1961.
  - MidTier closures fall: 10 against 14 by end-1961 in the clubs arm.
  - Small closures rise: 22 against 14 by end-1961, and 79 against 74 at run end.
  - One seed cannot separate these from the reshuffle.
- **No runtime cost.** The club arms ran faster than the control, though the control ran in a different
  session.

### Three seeds (rcl-v1 seed 1001; rcl-v2 seeds 1002-1003)

rcl-v2 runs its own control arm on the same assembly. Flag-off replay: 75 identical streams through week 12.
Reproduce with `./SimTools/RunRecordClubProbe.ps1 -RunTag rcl-v2 -Seeds 1002,1003 -Arms control,clubs`, then
`python SimTools/analyze_record_club_probe.py --tags rcl-v1 rcl-v2 --out rcl-v2`.

The table gives paired clubs-minus-control deltas, mean ± standard error, n = 3.

| | 1960 | 1961 | Run end (106 wk) |
|---|---|---|---|
| Closures, all tiers | −1.0 ± 2.0 | 0.0 ± 3.2 | −2.0 ± 3.6 |
| Closures, MidTier | −1.7 ± 0.7 | −2.3 ± 1.3 | −2.3 ± 1.3 |
| Closures, Independent | −0.3 ± 1.2 | +0.3 ± 1.5 | −1.7 ± 1.2 |
| Closures, Boutique | −1.3 ± 2.0 | −1.0 ± 2.0 | −1.0 ± 2.0 |
| Closures, Small | +2.3 ± 1.2 | +3.0 ± 4.0 | +3.0 ± 4.0 |
| Closures, Major | 0 | 0 | 0 |
| Hot 100 C4 share | +.028 ± .008 | −.019 ± .023 | |
| Hot 100 Major-family share | +.012 ± .003 | −.018 ± .038 | |
| LP chart C4 (charted LP units) | +.013 ± .016 | −.036 ± .021 | |
| LP chart Major share (charted LP units) | −.002 ± .004 | −.011 ± .002 | |
| Major record gross | +$95.9M ± 2.5 | +$89.6M ± 1.5 | |
| Store LP units | +0.2M ± 0.5 | −0.2M ± 0.4 | |

Control levels, seed range across 1001-1003:

| | Range |
|---|---|
| Closures by run end | 74-89 |
| 1961 Hot 100 C4 | .322-.373 |
| 1961 Major-family share | .565-.643 |
| 1961 LP chart Major share | .878-.929 |

LP chart measures use charted LP units, because the album-chart stream is empty in calibration runs.

- **Closures do not move beyond noise.** The total is within one standard error of zero at every point.
  Majors never close.
  - MidTier is the most consistent: fewer closures in all three seeds, −2.3 ± 1.3 by run end. That's
    1.8 SE, below the bar for a three-seed result.
  - Small closures lean up, with a wide error.
  - Three-seed standard errors understate reshuffle noise (a flag known to be inert once moved Boutique
    closures by 8 points on one seed).
- **Concentration is neutral by 1961.**
  - The one clear move is 1960 Hot 100 concentration: C4 +2.8 points, Major family +1.2 points, both
    above 3 SE. It's gone in 1961.
  - LP chart concentration does not rise. Its Major share falls about one point in 1961, because the
    club buys no chart position.
  - The extra club money ($90M a year of record gross to the Majors) does not turn into chart share in
    two years.
- **The club split is seed-sensitive.**

  | Seed | Columbia | Capitol | RCA | Columbia licensed |
  |---|---|---|---|---|
  | 1001 | 44.8% | 32.2% | 23.0% | 45% |
  | 1002 | 47.4% | 26.1% | 26.5% | 53% |
  | 1003 | 29.9% | 37.8% | 32.3% | 64% |

  Columbia stays below the FTC's 53-56% throughout. That's because the club split follows each world's
  opening LP strength and current sellers. A Columbia-specific opening backlist would fix the FTC split
  directly, but that is calibration, not mechanism, and is left for the author.
- **Runtime.** In the same session, clubs arms ran 1,621 s and 1,641 s against 1,771 s and 1,791 s for the
  controls. There is no runtime cost.

Next: the decade A/B at audit end. Club volume after 1962 and licence turnover only show up over the
decade.
Capitol's share and Columbia's licensed share are diagnostics, not gates.
