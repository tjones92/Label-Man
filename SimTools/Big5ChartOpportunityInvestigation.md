# Why Big-5 singles reach the Top 10 more often

The four reference worlds reproduce two separate effects. The Big-5 release far more singles by acts past the newcomer career stage. Within the newcomer stage, their singles are also 1.91 times as likely to enter the Hot 100 and 1.61 times as likely to reach the Top 10 once in. The most consequential modeling assumption I found is that the label's launch reach helps decide whether buyers who are already aware of a record can respond to its commercial appeal. A second rule prevents stations from auditioning some records even after stores in their region are selling them.

## How large is the excess?

I joined each live AI single launched in weeks 1–92 to its own chart history, within seed. Every title gets the same observation window, from launch through launch +12. A hit counts once. Opening catalog titles contribute to the calendar charts and to acts' chart histories, but they have no observed live launch. Big-5 means Columbia, RCA Victor, Capitol, Decca and Mercury. Field includes every other label, including MidTiers.

| Launch cohort | Big-5 launches → Hot 100 → Top 40 → Top 10 | Field launches → Hot 100 → Top 40 → Top 10 | Big-5 / field Top 10 per launch |
|---|---:|---:|---:|
| All live singles | 2,780 → 1,009 → 476 → 229 | 27,674 → 2,984 → 1,335 → 551 | 8.24% / 1.99% = **4.14×** |
| NewSigning or Unsigned at launch | 1,204 → 142 → 88 → 46 | 23,492 → 1,448 → 713 → 291 | 3.82% / 1.24% = **3.08×** |
| Other careers, including Rising | 1,576 → 867 → 388 → 183 | 4,182 → 1,536 → 622 → 260 | 11.61% / 6.22% = **1.87×** |

For newcomers, the successive relative rates are **1.91×** at entry, **1.26×** from Hot 100 to Top 40, and **1.28×** from Top 40 to Top 10. The advantage exists before a title can benefit from its own Top 40 position and keeps growing afterward. The fixed-window newcomer Top 10 premiums are 2.91, 4.33, 3.27 and 1.58 across seeds 1001, 2002, 3003 and 4004. The previous score of 2.86× uses the same launch cutoff and follows each record through the end of the run. Late field hits account for the difference.

Career mix matters as much as the within-career advantage under one simple standardization. Other-career acts supply 56.7% of Big-5 launches and 15.1% of field launches. At the Big-5 career mix and the field's two conversion rates, 4.06% of singles would hit. Moving from 1.99% to 4.06% is a 2.04× mix effect. Moving from 4.06% to the observed 8.24% leaves another 2.03×. The split depends on the reference rates and on combining several career stages. It measures composition, rather than the causal effect of changing an act's label.

## What history can establish

The reconstructed 1961 chart gives the five operating labels **107 of 520 Top 10 slots** (20.6%) and **20 of 105 titles first reaching the Top 10** (19.0%). Assuming an 18% issue share, the implied relative rates are **1.18× for slot-weeks** and **1.07× for distinct hits**. At a 16–19% issue share the distinct-hit proxy ranges from 1.00× to 1.24×. The calculation is the odds of belonging to the Big-5 among hits divided by the odds among issues. For 1960 the Big-5 held 178 of 520 slots (34.2%), implying 2.37× slot-weeks per issue at the same assumed 18% share. History itself changes markedly between the two years. Release-year carryover, two charting sides on one disc, and uncertainty over the issue denominator limit the comparison.

Billboard's contemporary table identifies the often-cited 5,797 (1960), 6,036 (1961), and 6,700 (1962) as singles **reviewed**. The project's roughly 1,080 Big-5 issues come from 1960 catalog-number spans with gaps and dating uncertainty. Applying 18% to 1961 is therefore an assumption. Neither source supplies issues by newcomer status. The historic 1.2× is a useful scale for all releases, with no measured 1.2× newcomer target. [Billboard's 1964–65 International Buyer's Guide](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1964/Billboard%201964-08-15-II-International-Buyer%27s-Guide-1964-65.pdf) · [Catalog reconstruction](LabelReleaseProgrammeImplementation.md)

**The 1961 newcomer discrepancy survives a common definition.** Take records whose Hot 100 debut and first Top 10 both fall in 1961. Call a record a newcomer hit if its act had no earlier Hot 100 week in the available history. In the historical series, the Big-5 supplied **3 of 44** such hits (6.8%). The sim gave **8/44, 9/38, 9/35 and 7/47**, or **33/164** (20.1%). The sim averages 41 newcomer hits across the whole industry against 44 historical. Its excess chiefly concerns which labels break those acts. Among *all* titles that first reached the Top 10 in 1961, including earlier Hot 100 debuts and previously charted acts, the Big-5 supplied 24–41 per sim seed, versus 20 historically.

The historical window starts December 28, 1959. Its three Big-5 acts are Buzz Clifford, Jimmy Dean and Leroy Van Dyke. Dean had a 1953 country hit and two 1957 pop chart records. Van Dyke had already made the pop Top 20 with “The Auctioneer.” Two of these three were demonstrably experienced hitmakers. The simulation likewise has career information that a short chart history cannot recover. Of its 33 Big-5 first-chart newcomer hits, eight were outside NewSigning/Unsigned at launch. A true career-debut *rate* needs a historical release census that includes misses. [Country Music Hall of Fame on Dean](https://www.countrymusichalloffame.org/hall-of-fame/jimmy-dean) · [Van Dyke on his two hits](https://leroyvandyke.com/leroy-van-dyke-on-his-two-career-defining-hits-the-auctioneer-and-walk-on-by/)

The historical 4.5 per year describes 18 Big-5 first-observed-chart hits across 1961–64, including records crossing New Year within that window. A consistent same-calendar-year debut and hit gives 3, 0, 7 and 7. That variation and the short lookback matter when comparing one simulated year. Historical credits here use normalized primary artist names and title text, while simulation joins use IDs. Each historical title first reaching the Top 10 in the period matched its annual label table.

## What the label's reach does to a buyer

At launch a keyed draw determines fit exposure for each region, with probability 0.3 + 0.7 × label reach and exceptions for home regions. The purchase calculation already has separate measures of regional arrival, awareness, station listening and physical stock. It also caps the record's national commercial-fit multiplier in an unexposed region. For a newcomer with no prior hit and a draw exactly at the model's 85th-percentile hit threshold, toggling only this exposure state changes the fit component of purchase conversion **13.34×**. That is an exact comparison at fixed inputs. Inventory, competing titles and the singles budget still determine realized sales. [RegionalFitExposure.cs](../Systems/RegionalFitExposure.cs) · [SingleCommercialFit.cs](../Systems/SingleCommercialFit.cs) · [ChartSimulator.cs](../Systems/ChartSimulator.cs)

The two archived regional traces show the rule operating after arrival. They cover newcomers passing the runtime's above-median fit test, a broader set than the upper 15% hit tail. In seed 2002, 3,736 field record-regions had arrived in week one but were fit-capped. Their median modeled aware audience was 32,329 and median cleared sales were two. Seed 3003 had 3,714 such observations, 33,235 aware and two sales. Among arrived markets the cap applied to **31.7% of field observations and 7.2% of Big-5 observations** across these two traces.

I also compared each record's arrived, exposed markets to its own arrived, capped markets. The median exposed-to-capped ratio in raw demand per thousand modeled aware buyers was **5.09× and 5.60× for field records** (1,898 and 1,910 mixed-exposure titles) and **5.42× and 5.07× for Big-5 records** (79 and 62). This comparison holds the song, act, label and national draw fixed. Regional taste, home-market selection, radio and buyer composition can still differ. The exact 13.34× component comparison isolates the code path. The observed ratios describe its setting and cannot be translated directly into chart positions.

The national Top 40 does open cities and can release regional arrival and fit holds. The automatic 0.4 awareness floor and rack shelf are less uniform than the handoff suggests. The reference mixes the local and national ranks for retail, scales the floor by city reach, and suppresses it during an arrival hold. For field newcomers in the two traces whose first observed week was at most 92, only **273 of 17,062** initially capped regions first realized fit through the national-chart trigger by age 12. **3,700** opened through a station or a neighboring station, 254 through neighboring sales, and 12,835 had no observed opening. This counts the first effective opening and cannot measure the chart's indirect effects on earlier local signals. It does put station decisions ahead of the national threshold as a direct source of early expansion. [CityReach.cs](../Systems/CityReach.cs) · [RegionalAudience.cs](../Systems/RegionalAudience.cs) · [LocalRetailExposure.cs](../Systems/LocalRetailExposure.cs)

## Where a field record with evidence loses a hearing

The station system already creates cross-market audition offers. A source region must have two consecutive observations with ordinary station support, measurable listening and at least 20 units sold. Up to two format-admissible destinations receive the report. Before the destination compares offers for its one research slot, a new candidate can be rejected because it is awaiting regional arrival or because its city has not been reached. A rejected candidate stays outside the trial competition even when the source report is strong. An arrival hold also zeros the local-unit signal used by the regional discovery system. [ResponseAuditionProbe.cs](../Systems/Radio/ResponseAuditionProbe.cs) · [StationNetwork.Playlist.cs](../Systems/Radio/StationNetwork.Playlist.cs) · [PlaylistMeeting.cs](../Systems/Radio/PlaylistMeeting.cs)

Across the four controls I counted only actual offers to NewSigning/Unsigned titles, up to launch +12 and strictly before the record's Hot 100 debut. Multiple stations or meetings can offer the same record.

| What happened before national entry | Field | Big-5 |
|---|---:|---:|
| Offers / distinct titles offered | 45,070 / 4,164 | 1,876 / 277 |
| Rejected on regional arrival | 18,586 | 0 |
| Rejected on city reach after passing arrival | 8,857 | 0 |
| Offers rejected for either reason | **60.9%** | **0%** |
| Titles for which every observed offer hit one of those rules | 1,077 | 0 |
| Titles starting at least one research trial from these offers | 688 | 71 |

The field's geographic rejection share was 60.0–62.3% across seeds. Even where the source response score reached 100, the score used for one part of local retention, 12,567 of 19,363 field offers hit a geographic veto. A narrower test found **197 distinct station-weeks** with a vacant research seat, an unseated city-rejected newcomer, an already-arrived region and a source score of at least 100. Those cases covered 178 seed-specific titles. The candidate could still have faced cooldown, another offer or weak listener response in a live trial.

For example, seed 2002's Nancy Perry single “Pretty Baby I Love You” sold 44 units in the Deep South region. A station there received a Southwest report scoring 1,388 in week 35. Its research seat was empty. City reach rejected the offer. The record entered the Hot 100 at No. 79 in week 40 and never reached the Top 10. Billboard's contemporary coverage distinguishes records showing substantial regional activity before national chart entry. That supports a test of how stations act on reports, while leaving the probability of a successful trial an empirical question. [Billboard, February 29, 1960](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1960/Billboard%201960-02-29.pdf)

## Other explanations and the volume decision

| Explanation | Evidence and implication |
|---|---|
| Release selection | Career mix alone doubles the field rate when standardized to the Big-5 slate. The archived cap on the number of candidates reviewed actually raised the newcomer premium on three seeds. Who receives a slot matters, but reducing shortlist size is a poor substitute for modeling what gets issued. |
| Song selection, production, promotion | Earlier keyed-draw work found similar upper-tail odds for newcomers. Hook standardization reduced the pooled premium from 2.86× to 2.32×. The production cap split 2–2 by seed. Launch-package and weekly-push cuts failed to remove newcomer conversion in the archived screens. The older quality-to-sales elasticity was measured among selected hits and combines several mechanisms. It is not an independent purchase exponent. |
| Launch depth | Re-scoring the existing four-seed newcomer footprint experiment reproduces **2.86× → 1.84×**, down on three seeds. It jointly changes exposure, regional launch intensity and stock. Big-5 1961 slot share fell from 31.9% to 27.2%, but 1960 also fell from 26.8% to 23.3% against 34.2% historically. This is evidence for a causal contribution from the package. The individual contribution of the fit cap remains unmeasured in a live world. |
| Purse and stock | All four controls explicitly select a singles ration exponent of **1.0**. Across all 104 weeks, 11.5–12.1% of serviceable intent cleared by units. The purse is tight, but the suggested 1.35 power cannot have generated these results. A separate four-seed quarter-stock experiment reduced three-week sales on Big-5 advance books of at least 50,000 from 23,876 to 15,364 and entry from 89.6% to 73.4%. Across all Big-5 singles in that experiment the pooled Top 10 rate actually increased from 6.74% to 8.75%. The experiment identifies a stock effect on heavily preordered releases, with no demonstrated general cure for newcomer hits. |
| Missing issues | The reference reserves part of the live singles purse for major-label catalog and specialty business and books **30.1–30.2 million units per 104-week run** without creating individual records. These units earn revenue but contribute neither a launch nor a chart entry. Historic catalogs include kinds of issues represented only in aggregate here. Sales of old catalog and sales of newly issued titles cannot be separated in this allowance. Converting units to hypothetical releases would invent a denominator. |

The completed three-seed volume series illustrates the distinction. The baseline had 12.5% of 1961 issues and 33.8% of Top 10 slots. More slots plus a field trim moved those to 15.1% and 31.7%. Exempting Major releases from gates moved them to 16.4% and 39.8%. Enlarging opening rosters took them to **18.8% and 48.8%**. Volume alone does not dictate chart share. Which records fill the marginal slots does. [Volume experiments](Big5VolumeAndPremiumHandoff.md) · [Off-chart accounting](../Systems/MajorOffChartSingles.cs)

**The first test I would run is to distinguish national servicing from listener response.** Keep the major's ability to deliver a copy anywhere. In one arm, let already-reached listeners respond to the same record fit wherever they hear it, limiting the number reached through measured contact and keeping the other launch terms fixed. In a separate arm, let stations in arrived regions consider an audition copy on a qualifying outside report even if the city flag is closed. Keep the single trial seat, local retention test, physical retail orders and ordinary rotation rules. The first isolates purchase conversion. The second isolates the DJ's opportunity to hear the record. The 197 vacant-seat cases give the second test a measurable first outcome before looking for chart movement.

Use the same four seeds and report both stages of newcomer conversion, all-release conversion, first-chart-act title counts, both years' slot shares, and LA–Minneapolis lead and gap. The archived blanket footprint experiment shows why a better 1961 share alone is insufficient. When testing volume, distinguish new pop campaigns, lower-priority issues and catalog business so an increase in the release count has an identifiable audience and cost.

## Results of the two tests (2026-11-17)

Both tests ran on the four seeds against the `zfg-*-sc` controls. A rebuilt plain control (`ctl-2002-sc`) matched `zfg-2002-sc` byte for byte, apart from the audition log, so code changes since the controls ran do not affect the comparison.

| Launches wk 1–92, window +12 | Control | `--fit-for-aware-buyers` | `--dj-word-of-mouth` |
|---|---:|---:|---:|
| Newcomer Top 10 premium | 3.08× | 3.26× | 4.19× |
| Field newcomer Hot 100 / Top 10 | 1,448 / 291 | 1,556 / 278 | 1,433 / 278 |
| 1961 Big-5 Top 10 slots | 31.9% | 32.8% | 34.2% |
| 1961 Big-5 first-act hits | 33/164 | 28/157 | 49/174 |

Removing the purchase cap raised top-tier field entry from 36.5% to 39.8%, but the field's Top 10 rate stayed near 8%. DJ tip trials in regions still awaiting arrival had no local sales, so only 15 of 956 graduated. Tips also travel from stations that already play a record, which favors Big-5 releases: their top-tier newcomer Top 10 rate rose from 23.5% to 33.9%. The serviced-veto control was not run.

**Arrival footprint.** In the `nctrace` runs, every top-tier Big-5 newcomer had arrived in all 7 regions at age 1. Top-tier field newcomers averaged 3.45 regions at age 1 and 5.44 at age 12, and only 10% were national at launch. The field's Top 10 rate rises with footprint: 2.8%, 8.9%, 13.2% and 12.0% for 0–2, 3–4, 5–6 and 7 regions at age 1. At full footprint, Big-5 releases still convert at 25.2% against 12.0%, despite lower fit draws and launch awareness and equal week-one stock. Their raw demand per thousand aware buyers is 1.26× higher. That matches their runtime-quality edge raised to the fourth power, (0.713 / 0.671)<sup>4</sup> = 1.27, with production at 0.752 against 0.665. MidTier newcomers with similar quality convert at Big-5 rates. Roughly, footprint takes the premium from 3.2× to 2.1×, and quality through `QUALITY_EXPONENT = 4` accounts for the rest.

**Which quality term carries the slope.** In a logit with footprint and log quality, the Big-5 Top 10 odds ratio falls to 1.13 (standard error 0.30). The log-quality slope is about 12. `Systems/QualityResponseProbe.cs` adds two diagnostic switches. Each leaves a record at quality 0.68 unchanged, and each was tested on one 52-week seed (2002) against a control from the same build:

| Seed 2002, 52 weeks | Control | `--wom-quality-once` | `--purchase-quality-exponent=2` |
|---|---:|---:|---:|
| Newcomer Hot 100 log-quality slope | 7.2 | 8.1 | 4.4 |
| Newcomer Top 10 log-quality slope | 10.7 | 11.1 | 6.0 |
| 1960 Big-5 Top 10 slot share (history 34.2%) | 29.1% | 25.7% | 18.5% |
| All-record units | 109.2M | 109.0M | 104.7M |

Counting quality once in word of mouth leaves the slope where it was. Halving the purchase exponent halves the slope, but it also pulls the 1960 Big-5 share far below history. One seed's newcomer premiums rest on three to six Big-5 Top 10s, so they are not reported. Score with `python SimTools/score_quality_response.py <control> <arm>...`.

The three-seed, 104-week screen (`qpk2-*` and `qpk3-*` for seeds 1001, 2002 and 3003) compares against `zfg`. With both switches off, the current build matches `zfg-2002-sc` exactly through week 52.

| Pooled, launches wk 1–92, window +12 | Control | Exponent 3 | Exponent 2 |
|---|---:|---:|---:|
| Newcomer Top 10 premium | 3.49× | 2.62× | 3.43× |
| Big-5 newcomer Top 10s / launches | 41 / 953 | 31 / 917 | 43 / 873 |
| Established-act Top 10 premium | 2.00× | 1.66× | 1.46× |
| Established-act climb ratio (Hot 100 → Top 10) | 1.32× | 1.10× | 0.98× |
| Big-5 Top 10 slots, 1960 (history 34.2%) | 30.2% | 21.9% | 21.1% |
| Big-5 Top 10 slots, 1961 (history 20.6%) | 33.8% | 32.9% | 26.5% |
| 1961 Big-5 hits by acts new to the chart (history 3/44) | 26/117 | 28/128 | 29/145 |

The newcomer premium does not fall steadily as the exponent falls. The exponent-3 drop rests on ten fewer Big-5 Top 10s and does not repeat at exponent 2, so it is treated as noise. The established-act premium falls at each step. The steepness of the purchase response therefore carries how far charting Big-5 acts climb. It does not carry the newcomer premium. The cross-sectional result, where quality absorbs the Big-5 coefficient, reflects correlated advantages rather than the cause. Both exponents pull 1960 far below history, so neither is adoptable.

**Campaign, stock and exposure depth.** The earlier Major-newcomer launch-depth reset (`mnfoot`) lowered three launch reads at once. Each of those reads was then run alone (`--label-blind-launch=factor|stock|expose --label-blind-launch-scope=major-newcomer`) on seeds 1001, 2002 and 3003. Score with `python SimTools/score_newcomer_depth.py 1001,2002,3003 zfg mnfoot mnexpose mnfactor mnstock`.

| Pooled, launches wk 1–92, window +12 | Control | Full reset | Exposure only | Factor only | Stock only |
|---|---:|---:|---:|---:|---:|
| Newcomer Top 10 premium | 3.49× | 1.68× | **2.19×** | 4.02× | 3.76× |
| Big-5 newcomer Top 10s / launches | 41 / 953 | 23 / 1,041 | 25 / 1,027 | 40 / 928 | 38 / 939 |
| Established-act premium | 2.00× | 1.64× | 2.09× | 1.79× | 2.24× |
| Big-5 Top 10 share 1960 / 1961 | 30.2 / 33.8% | 24.7 / 27.3% | 30.2 / 31.7% | 28.3 / 35.1% | 30.1 / 39.0% |
| 1961 Big-5 hits by acts new to the chart | 26/117 | 24/131 | 17/103 | 34/122 | 26/111 |

The launch factor sets regional launch awareness and radio. Resetting it lowered Big-5 newcomers' week-one awareness from 0.142 to 0.103 and radio from 0.065 to 0.046, but their Top 10 rate stayed at about 25%. The stock arm did not lower week-one stock, because first-run stocking follows fit exposure. Stock never binds in any case: about a tenth of demand clears in every group. Resetting only the launch fit-exposure chance (about 0.95 for a Major, 0.57 at the reference) left awareness and radio unchanged. It cut week-one demand from 1,922 to 1,196 and the Big-5 newcomer Top 10 rate from 25.2% to 14.9%. The premium fell on all three seeds, and the 1960 Big-5 share held. Exposure carries about two-thirds of the reset's effect without its 1960 cost.

**Adoption guards for exposure only.** Adding seed 4004 gives a newcomer premium of 3.08× → 2.10×, down on three of four seeds (seed 4004 rose from 1.58× to 1.88×). Big-5 newcomer Top 10s fall from 46 to 32. Big-5 newcomer Top 10 records in 1961 fall from 7.2 to 5.0 per seed; history has about 4.5 a year.
- **Units and money:** single units are identical because the purse is fixed. Big-5 gross and net are flat or slightly up, and defunct labels fall from 96.2 to 94.8.
- **Shares:** the Big-5 Top 10 share moves from 26.8% to 25.7% in 1960 and from 31.9% to 31.1% in 1961.
- **Regional stagger (fails):** the share of Big-5 Top 10s that reach Los Angeles or Minneapolis two or more weeks before the other city rises from 9.4% to 15.7%. Those with a city gap of six weeks or more rise from 3.8% to 10.3%. History has neither among the twenty 1960 Big-5 Top 10s. Independent records move toward history: lead from 26.9% to 22.7%, gap from 24.7% to 21.7%, against 18.5% and 14.6% historically.

Because the draw is made region by region, a Big-5 newcomer connects in some regions and not others, and breaks city by city. A draw shared by all of a record's regions would decide national connection once. That would keep the majors' simultaneous national breaks.

**Shared national draw.** `--label-blind-launch-expose-shared` gives each Major-newcomer record one keyed exposure draw, read by every region; the act's home region stays exposed. The runs are `mnshared-*` for seeds 1001–4004. On seed 1001, 92 Big-5 newcomers were exposed in all seven regions and 68 only at home, against a chance of 0.57.

| Four seeds | Control | Per-region exposure | Shared exposure |
|---|---:|---:|---:|
| Newcomer Top 10 premium | 3.08× | 2.10× | 2.65× |
| By seed | 2.91 / 4.33 / 3.27 / 1.58 | 2.71 / 2.50 / 1.44 / 1.88 | 2.69 / 2.49 / 2.88 / 2.50 |
| Big-5 newcomer Top 10 records per seed, 1960 / 1961 | 4.5 / 7.2 | 4.2 / 5.0 | 6.0 / 5.5 |
| Big-5 1960 Top 10s with a city lead ≥ 2 weeks | 9.4% | 15.7% | 6.9% |
| Big-5 1960 Top 10s with a city gap ≥ 6 weeks | 3.8% | 10.3% | 8.4% |

The shared draw removes the extra city leads. Most of the remaining six-week gaps belong to Big-5 records whose acts were not newcomers, which the switch never reaches: three in the control, six with per-region exposure and five with the shared draw. Newcomer gaps are 0, 2 and 2. With about 85 records, these counts move by about three records between reshuffled worlds. A connected record keeps its full national strength, so the shared draw cuts the premium less than the per-region draw does. The connection chance is the remaining setting.

**Connection chance.** `--label-blind-launch-expose-shared=P` sets the shared chance. The runs are `mnsh40-*` and `mnsh30-*` on seeds 1001–4004.

| Four seeds | Control | 0.57 | **0.4** | 0.3 |
|---|---:|---:|---:|---:|
| Newcomer Top 10 premium | 3.08× | 2.65× | **1.78×** | 1.88× |
| By seed | 2.91 / 4.33 / 3.27 / 1.58 | 2.69 / 2.49 / 2.88 / 2.50 | 2.01 / 2.70 / 1.42 / 1.08 | 1.34 / 2.74 / 1.43 / 2.28 |
| Big-5 newcomer Top 10s | 46 | 38 | 26 | 32 |
| Big-5 newcomer Top 10 records per seed, 1961 (history about 4.5) | 7.2 | 5.5 | **4.5** | 5.8 |
| Established-act premium | 1.87× | 1.76× | 1.85× | 2.10× |
| Big-5 Top 10 share 1960 / 1961 | 26.8 / 31.9% | 25.7 / 30.2% | 25.2 / 30.0% | 25.5 / 31.7% |
| Big-5 1960 Top 10s, city lead ≥ 2 / gap ≥ 6 weeks | 9.4 / 3.8% | 6.9 / 8.4% | 7.1 / 7.5% | 8.5 / 9.0% |
| Defunct labels by week 104 | 96.2 | 95.5 | 94.5 | 99.2 |

The effect floors at about 1.8×. Below a chance of 0.4, stations, neighbouring regions and the national chart open most unconnected records anyway. At 0.4 the premium falls on every seed, and 1961 Big-5 newcomer hits match history. Established acts, money and closures are unchanged. The remaining 1961 Big-5 share of 30% is carried by established acts and release volume, not by newcomer launches.

**Adopted (2026-11-17).** The author adopted the shared draw at 0.4. `SimLogs/writing-calibration/candidate-request.json` now ends with `--label-blind-launch=expose --label-blind-launch-scope=major-newcomer --label-blind-launch-expose-shared=0.4`; the previous file is `candidate-request-pre-newcomer-exposure-2026-11-17.json`. The screens ran on the `zfg` flags, which are the candidate plus `--historical-majors`, `--zero-fee-material-gate` and the telemetry switch `--regional-home-sales`. `mnsh40-{1001,2002,3003,4004}-sc` are the controls for the next screens. Later the same day the author also adopted `--historical-majors` and `--zero-fee-material-gate` (256 flags; previous file `candidate-request-pre-historical-majors-2026-11-17.json`). Apart from `--run`, `--seed`, `--weeks` and telemetry switches, the candidate now equals the `mnsh40` run requests, flag for flag and in order.

## Where the 1961 Big-5 share still comes from

On the adopted controls (`mnsh40-*`, four seeds), the Big-5 still hold about 30% of 1961 Top 10 slots, against 20.6% historically. `SimTools/analyze_big5_1961_excess.py` splits 1961 slot points by whether the act had a 1960 Top 10:

| 1961 Top 10 slot points | History | Sim mean |
|---|---:|---:|
| Big-5 acts with a 1960 Top 10 | 16.4 | 12.3 |
| Big-5 acts without a 1960 Top 10 | 4.2 | **17.6** |
| Other labels' 1960 hit acts | 22.9 | 21.9 |
| Other labels' acts without a 1960 Top 10 | 56.5 | 48.1 |

The whole excess is Big-5 acts without a 1960 Top 10: about 19 records a seed, against history's five (Clifford, Dean, Cline, Van Dyke and the Tokens). Before the adopted lever this group held 17.2 points, so its total did not fall. Big-5 NewSigning slots fell from 8.0 to 5.3 points, while Big-5 acts launched as Established rose from 1.6 to 5.7. Most of that rise is small-sample movement among opening-roster acts (7 of 22 launches against 1 of 14).

For 1961 launches (weeks 53–92), Big-5 acts signed during the run still convert at three times the field rate:

| Acts signed during the run | Big-5 Top 10 / launches | Field Top 10 / launches |
|---|---:|---:|
| NewSigning at launch | 17 / 683 (2.5%) | 100 / 9,130 (1.1%) |
| Rising at launch | 13 / 111 (11.7%) | 29 / 577 (5.0%) |

These home-grown acts give the Big-5 about 7.5 Top 10 records a seed in 1961, against roughly two in history. Big-5 acts with a 1960 Top 10 convert at 1.41× the field. Opening-roster acts without a 1960 Top 10 supply about two records a seed, close to history's three veterans.

Under the adopted draw, top-tier Big-5 newcomers' unconnected regions open through sustained local station support 18.5% of the time, against 3.2% for the field. Only 32% stay closed through week 12, against 52%. Big-5 regional radio starts higher because of the launch factor, so with exposure capped the factor may now be what reopens Big-5 records. `mnsf40-*` tests the adopted draw plus the factor reset.

**The factor reset is rejected.** With the adopted draw plus the factor reset, the newcomer premium is 1.91× against 1.78×. The 1961 Big-5 share rises from 30.0% to 33.3%, and defunct labels rise from 94.5 to 100 on all four seeds. Big-5 city leads and gaps fall to 3.9% and 4.0%, but the station path does not carry the residual.

**Follow-ups and volume.** Home-grown Rising follow-ups split by the act's best earlier chart position:

| Prior best before the 1961 follow-up | Big-5 Top 10 / launches | Field Top 10 / launches |
|---|---:|---:|
| Top 10 | 2 / 37 | 15 / 258 |
| 11–40 | 10 / 51 | 20 / 405 |
| Hot 100 only, or never charted | 1 / 34 | 0 / 61 |

After a Top 10 the two convert alike. After a Top 40 that peaked between 11 and 40, Big-5 follow-ups reach the Top 10 four times as often as the field's. Acts the Big-5 signed during the run account for about 60% of Big-5 singles in 1961. At field conversion rates they would still give about five Top 10 records a seed, against roughly two in history. The remaining 1961 excess therefore combines how many newcomers the Big-5 sign and release with the post-Top 40 follow-up premium.

**Veteran depth and a signing quota.** In the adopted control, about two-thirds of Big-5 singles in 1961 come from newly signed acts (324 per seed, from about 158 acts releasing two singles each). Only about eight established acts per major release at all. Two switches in `Systems/MajorVolumeSwitches.cs` test the opposite policy:
- `--major-catalog-veterans=N` gives N acts on each AI major's opening roster a working career since 1946–55 with only occasional placings (`CatalogVeteran`, Established).
- `--major-signing-quota=K` caps an AI major's in-run market signings at K a year.

The runs `vetq-*` use N = 20 and K = 10 on top of `mnsh40`.

| Four seeds | Control (`mnsh40`) | Veterans 20, quota 10 |
|---|---:|---:|
| Big-5 signings per seed, 1961 | 183 | 50 |
| Big-5 share of issues, 1960 / 1961 | 7.2 / 11.7% | 10.5 / 13.1% |
| Big-5 Top 10 share, 1960 (history 34.2%) | 25.2% | 30.1% |
| Big-5 Top 10 share, 1961 (history 20.6%) | 30.0% | 31.6% |
| 1961 slot points, Big-5 acts without a 1960 Top 10 (history 4.2) | 17.6 | 17.4 |
| Established-act premium | 1.85× | 1.30× |
| Defunct labels by week 104 | 94.5 | 104.8 |

The veterans release about 3.5 singles a year each, but 6.7–7.4% of their singles reach the Top 10, two and a half times the rate for signed newcomers. That moves 1960 toward history. The Established opening history ends in a recent Top 40 placing, which raises their recognition. The career ladder then drops most of them on flops: releasing veterans fall from 91 to 37 per seed in 1961. Newcomer singles from the 1960 cohort keep 1961 where it was, and closures rise by ten.

**Second version: quiet, retained, deeper veterans.** Two more switches in `Systems/MajorVolumeSwitches.cs`, both needing `--major-catalog-veterans=N`:
- `--major-catalog-veterans-quiet`: no forced recent Top 40. The last three sides missed or placed 61–100, and the two-flop cap is skipped, so recognition starts low (median 0.07 at launch, about the level of other Established acts) and the ladder reads the real streak.
- `--major-catalog-veterans-retained`: the opening contract runs at least through 1961 (expiry week ≥ 105). Until then the act cannot be dropped on performance and its contract does not mature early. Demotion to Declining still happens.

The runs `vq2-*` use N = 28 (140 veterans a seed, nearly the whole unstamped opening roster), quiet, retained and quota 10 on top of `mnsh40`.

| Four seeds | Control (`mnsh40`) | `vetq` (20, quota 10) | `vq2` (28, quiet, retained, quota 10) |
|---|---:|---:|---:|
| Big-5 share of issues, 1960 / 1961 | 7.2 / 11.7% | 10.5 / 13.1% | 9.7 / 13.3% |
| Big-5 Top 10 share, 1960 (history 34.2%) | 25.2% | 30.1% | 29.1% |
| Big-5 Top 10 share, 1961 (history 20.6%) | 30.0% | 31.6% | **33.0%** |
| 1961 slot points, Big-5 1960 hit acts (history 16.4) | 12.3 | 14.2 | 18.1 |
| 1961 slot points, Big-5 acts without a 1960 Top 10 (history 4.2) | 17.6 | 17.4 | 14.9 |
| Veterans releasing in 1961, per seed | – | 37 | 93 |
| Veteran singles reaching the Top 10, 1960 / 1961 | – | 6.7 / 7.4% | 7.1 / 3.9% |
| Big-5 newcomer launches, 1961 (to week 92) | 700 | 637 | 500 |
| Defunct labels by week 104 | 94.5 | 104.8 | 102.0 |

Retention works: the bench lasts through 1961. But quiet veterans with low recognition still put 7.1% of their 1960 singles in the Top 10, about 22 records a seed, against about 1.2–1.5% for field newcomers. Those 1960 hits turn them into "1960 hit acts", so 1961 points move from the newcomer bucket to the hit-act bucket instead of leaving the Big-5. The 1961 share rises to 33.0%.

The ratio of issue share to slot share is the measure. History has the Big-5 at about 18% of issues and 20.6% of slots, so the average Big-5 issue converted only slightly better than the average issue. In `vq2` the Big-5 hold 13.3% of issues and 33% of slots. Even a major's least-known act converts at about 2.5 times the average issue. Recognition does not carry that rate, and nor does who the act is. Making the roster older, quieter or more stable changes whose name is on a Big-5 hit, not how many there are. Not adopted. The remaining lever is per-issue conversion of a Big-5 single regardless of career, which the fit-exposure work (§ "Connection chance") addressed only for newcomers.

## How well a Big-5 single converts (all careers)

Every live single launched in weeks 1–92 on the adopted controls (`mnsh40-*`, four seeds), outcomes within launch +12, annualized per seed:

| Tier | Singles a year | Top 10 titles a year | Per issue | History (1961) |
|---|---:|---:|---:|---:|
| Big-5 | 387 | 30.2 | **7.8%** | ~20 of ~1,080, about 1.9% |
| MidTier | 368 | 25.0 | 6.8% | |
| Independent | 1,917 | 37.2 | 1.9% | |
| Small/Boutique | 1,635 | 17.4 | 1.1% | |
| All non-Big-5 | 3,920 | 79.6 | 2.0% | ~85 of ~4,950, about 1.7% |

The field matches history per issue, and the Big-5 do not. They issue a third as many singles as history and score half as many Top 10 titles again. The comparison inherits the issue-denominator caveats above, but a factor of four is well outside them.

**Mix and within-career.** Against the field, a Big-5 single is 3.85 times as likely to reach the Top 10. Career mix accounts for 2.35×: 56% of Big-5 issues come from acts past the newcomer stage, against 15% of field issues. The remaining 1.64× is within career, and nearly equal for newcomers (1.78), mid-career acts (1.60) and stars (1.66). Run length adds nothing: a Big-5 Top 10 record spends 0.94 times as many weeks there as a field one.

**Big-5 and MidTier are one group.** Within career, Big-5 Top 10 odds are 2.04 times the Independents' and MidTier odds are 2.01 times. A logit with career fixed effects (clustered by seed) moves the Big-5 odds ratio from 2.04 to 1.56 with log runtime quality, 1.43 with hook and production, 1.46 with launch campaign, 1.64 with launch stock and awareness, and 1.28 with all of them. These inputs move together by tier, so the logit shows where the premium travels, not what causes it.

**No triage.** The candidate's `--launch-push-saturation=0 --launch-caps-major-only` clips every Big-5 launch campaign to exactly 0.55, from the 10th to the 90th percentile (raw 0.69–0.90). Every Big-5 single therefore launches with the full package: the 0.55 campaign, production 0.75 against 0.67 at Independents, about twice the Independent stock, and the Major fit-exposure chance for non-newcomers. Top 10 rate barely varies across the Big-5 slate. A period major issued about 20 singles a week and plugged a few. The sim's majors issue about 7.5 a week and plug all of them.

**Reading.** The 1961 excess comes from too many fully launched Big-5 singles, not too few or too strong field ones. About 390 plugged issues at 7.8% give 30 Top 10 titles. History is consistent with roughly 200–250 plugged issues at a similar rate, plus a long tail of routine issues that rarely charted. Adding volume alone fails (`vetq`, `vq2`, the enlarged rosters) because each added issue arrives plugged. Lowering the strength of every Big-5 issue would lower 1960 too, and 1960 is already below history (25–29% of slots against 34.2%).

**Proposed test: Major plug slots.** Each AI Major gives its full launch package to at most K singles a month, chosen by its own forecast (existing hit acts first). Its other singles launch as routine issues: campaign at the Independent median, routine first-run stock, and the shared reference exposure draw already adopted for newcomers. Run it alone, then with `--major-gate-exemption` so the routine tail can lift the issue share toward 18%. Guards: Big-5 Top 10 titles about 20 a year, 1961 slot share toward 20.6% without 1960 falling further, city lead and gap, Big-5 net and closures.

## Promotion capacity (2026-11-18)

**Where volume stalled.** The volume package was parked rather than failed. In §57, `--programme-major-singles=16 --slate-work-budget=0.85 --slate-work-budget-midtier=1 --major-gate-exemption` (plus a 58-act opening roster in `mvx`) took the 1961 Big-5 share of issues to 16.4–18.8%. But each added single launched with the full Major package, so the Top 10 share went to 39.8–48.8%, and the package was set aside until the per-issue premium was fixed. The candidate still runs the default 10 singles a month cap, the single capital-risk test and the material hold. Those cancel 200–250 Big-5 slots a year and leave issues at 7.2% (1960) and 11.7% (1961). At the sim's ~4,100 singles a year, history's ~18% share is about 750 Big-5 singles, not 1,080.

**Mechanism.** `--major-plug-slots=K` (`Systems/PromotionCapacity.cs`; `--midtier-plug-slots=K` for MidTiers): an AI Major gives its full launch package to at most K singles a month.
- Hit acts (Star, Superstar, or last run a Top 40) take slots first. Once the month's slots are gone the label may buy one more at its standard marketing budget, if cash covers four.
- Other singles need a slot beyond the label's hit-act reserve (one slot per three hit acts on the roster) and a quality read (keyed error ±0.05) at or above the median of the label's last 24 reads.
- Everything else launches as a routine issue with national distribution kept: reference campaign impact (launch and weekly push), reputation and payola; reference launch factor and stock; one shared exposure draw at 0.4. City reach and the arrival hold stay the label's own. Spend is unchanged in this version.

Checks: `--major-volume-check` now also runs `PromotionCapacityChecks`. A 27-week off run (`plugoff27-2002-sc`) matches `mnsh40-2002-sc` on records, launches and label finance. Score with `python SimTools/score_plug_slots.py ARM`.

**Screen** (`plug4-*` = K 4; `plug4v-*` = K 4 plus the volume package on a `--slate-work-budget=0.85` copy of the `mnsh40` request; four seeds, 104 weeks):

| Big-5 singles launched weeks 1–92, per seed | Singles | Hot 100 | Top 10 |
|---|---:|---:|---:|
| `plug4` hit act, plugged | 207 | 64.1% | 15.6% |
| `plug4` hit act, bought plug | 20 | 61.7% | 14.8% |
| `plug4` other, plugged | 92 | 28.3% | 5.2% |
| `plug4` routine | 345 | 12.5% | 2.4% |
| `plug4v` routine | 584 | 12.3% | 2.4% |

| Four seeds | Control | `plug4` | `plug4v` | History |
|---|---:|---:|---:|---:|
| Big-5 Top 10 per issue (a year) | 7.8% (30.2) | 7.1% (28.3) | 6.0% (33.1) | ~1.9% (~20) |
| Big-5 share of issues 1960 / 1961 | 7.2 / 11.7% | 7.4 / 12.2% | 11.2 / 16.9% | ~18% |
| Big-5 Top 10 share 1960 | 25.2% | 26.8% | 27.6% | 34.2% |
| Big-5 Top 10 share 1961 | 30.0% | 24.9% | 36.0% | 20.6% |
| By seed, 1961 | 40.6 / 26.2 / 30.0 / 23.1 | 26.3 / 14.0 / 30.8 / 28.5 | | |
| 1961 points, Big-5 acts without a 1960 Top 10 | 17.6 | 12.0 | 18.2 | 4.2 |
| 1961 points, Big-5 1960 hit acts | 12.3 | 12.9 | 17.8 | 16.4 |
| Big-5 1960 Top 10s, city lead ≥ 2 / gap ≥ 6 | 7.1 / 7.5% | 6.0 / 3.8% | 8.8 / 8.0% | 0 / 0 |
| Defunct labels by week 104 | 94.5 | 98.8 | 85.5 | |

Routine issues convert at 2.4%, about the field's 2.1%, and the shared draw keeps Big-5 hits national. In `plug4` the group behind the 1961 excess falls by a third and 1960 rises slightly. The 1961 share falls strongly on two seeds and is flat or up on the other two, which is within the ±10-point reshuffle noise, so it is not yet a clean result. Closures rise on three of four seeds.

**What capacity does not touch.** About 70% of Big-5 Top 10s now come from hit-act singles, and capacity never binds them. Follow-ups by the act's previous in-run single, eventual Top 10:

| Previous single | Big-5 | MidTier | Independent | Small |
|---|---:|---:|---:|---:|
| Top 10 | 20.3% | 11.7% | 9.7% | 19.2% (25 a seed) |
| 11–40 | 17.2% | 8.1% | 4.7% | 4.4% |
| 41–100 | 10.8% | 8.8% | 5.1% | 2.3% |
| Missed | 3.8% | 4.0% | 1.9% | 0.9% |

With the volume package, 240 more routine issues at 2.4% add about six Top 10s a seed. A routine Big-5 issue is still an Independent single with national distribution, which history's marginal major issue was not. Two pieces remain: how far a Major's full package lifts a proven act's follow-up (twice to three times an Independent's), and what a routine national issue should convert at.

## Hit-act follow-ups (2026-11-18)

History can be compared through the Hot 100 alone, so misses drop out on both sides. An anchor is a record's first week in the Top 10. Its act's other titles debuting in the next 52 weeks are the follow-ups, and the anchor's label decides the group. Score with `python SimTools/analyze_hit_followups.py ARM`.

| Per Top 10 anchor, next 52 weeks | Anchors | Hot 100 entries | Top 40 | Top 10 | Top 10 per entry |
|---|---:|---:|---:|---:|---:|
| History 1960–63, Big-5 | 95 | 4.35 | 2.93 | 1.13 | 26% |
| History 1960–63, other | 325 | 3.30 | 1.89 | 0.66 | 20% |
| History 1960, Big-5 | 26 | 3.81 | 2.96 | 1.31 | 34% |
| History 1960, other | 68 | 4.01 | 2.49 | 0.85 | 21% |
| `mnsh40` 1960 anchors, Big-5 (4 seeds) | 128 | 4.34 | 2.12 | 0.98 | 23% |
| `mnsh40` 1960 anchors, other | 364 | 1.93 | 0.86 | 0.39 | 20% |
| `plug4` 1960 anchors, Big-5 | 126 | 4.61 | 2.10 | 1.04 | 23% |
| `plug4` 1960 anchors, other | 344 | 1.99 | 0.83 | 0.35 | 18% |

**Big-5 hit-act follow-ups are historical.** After a Top 10, a Big-5 act gets 4.3–4.6 more Hot 100 entries and about one more Top 10 in a year, as in history. The 2–3× tier gap in follow-up conversion is the field falling short: other labels' hit acts get 1.9–2.0 entries against history's 3.3–4.0, and 0.35–0.39 Top 10s against 0.66–0.85. Per entry, both groups convert like history. The same split shows in 1961 slot points: Big-5 1960 hit acts hold 12.3–12.9 (history 16.4), and other labels' 1960 hit acts hold 18.9–21.9 (history 22.9).

The field's shortfall has two parts: fewer releases and fewer that chart. In the year after a 1960 Top 10, a field hit act releases 3.7 singles against the Big-5's 6.1–6.4, and 52% of them reach the Hot 100 against 71%. About 6–10% of field hit acts change label in that year. For entry, recognition carries the tier gap. Big-5 follow-ups launch with recognition 0.40 against 0.19 at Independents, and adding it to a logit takes the Big-5 entry odds from 2.14 to 1.09. Launch stock and awareness take them to 1.30, campaign impact to 1.52 and quality to 1.84. Big-5 hit acts are mostly established names; a field hit act is usually on its first hit, and one Top 10 adds about 0.07 recognition.

Earlier work on this (memories `hit-followups-lack-chart-access`, `field-hit-cadence-is-lane-clock`, `hit-act-hype-probe`): the hype lift (0.3, in the candidate) added about 11% to follow-up entries. Follow-up finance (`hff-v3`) raised field releases per hit act by about one but crowded out new acts and was not adopted. Neither touched the Big-5 side, which this table says needs no change.

**Consequence for the 1961 Big-5 share.** Under `plug4` the Big-5 hold 24.9% of 1961 slots against 20.6%. Their 1960 hit acts are 3.5 points below history and their acts without a 1960 Top 10 are 7.8 points above. The remaining excess is the non-hit side (routine issues at 2.4% and plugged non-hit singles at 5.2%), plus the field's missing repeat hits.

## Field hit-act follow-ups (2026-11-18)

**Count chart events, not entries.** History's Hot 100 lists both sides of a two-sided hit, so raw entries overstate history's follow-ups. `analyze_hit_followups.py` now also counts chart events: an act's debuts under four weeks apart count once (as in `FollowUpGapFableHandoff.md` §6). Per Top 10 anchor, next 52 weeks:

| | Events | Top 40 events | Top 10 events | Top 40 per event |
|---|---:|---:|---:|---:|
| History 1960–63 / 1960, Big-5 | 3.19 / 2.88 | 2.33 / 2.19 | 1.08 / 1.23 | 73 / 76% |
| History 1960–63 / 1960, other | 2.69 / 3.06 | 1.72 / 2.12 | 0.64 / 0.79 | 64 / 69% |
| `mnsh40`, Big-5 | 4.19 | 2.07 | 0.95 | 49% |
| `mnsh40`, other | 1.87 | 0.82 | 0.37 | 44% |

This corrects the previous section. Big-5 hit acts chart *more* follow-ups than history and climb less, and the two cancel at the Top 40 and Top 10. Field hit acts chart fewer follow-ups *and* climb less, so they get 0.4–0.5 of history's Top 40 follow-ups.

**The sim has no climb premium for proven acts.** Of all chart events, the share reaching the Top 40, for acts with a Top 10 in the 52 weeks before the debut against everyone else (history debuts 1961–63; sim debuts weeks 40–92):

| | Proven acts' events | Proven T40 % | Other T40 % | Proven T10 % | Other T10 % |
|---|---:|---:|---:|---:|---:|
| History | 583 | **60.5** | 38.1 | 21.6 | 15.3 |
| `mnsh40` | 751 | 42.3 | 46.8 | 20.0 | 20.1 |
| `kaa55` | 738 | 41.7 | 47.4 | 20.2 | 20.4 |

History's charting record by a proven act reached the Top 40 1.6 times as often as anyone else's. In the sim it does slightly worse. The sim's other records also reach the Top 10 too often (20% against 15%), which bears on the Big-5 non-hit side.

**Funnel (`mnsh40`, per 1960 anchor, 52 weeks).** Field hit acts launch 3.6 singles against 5.7 for the Big-5, and 0.40 of them chart against 0.70. Field follow-ups have similar quality (0.706 against 0.730) and arrive in all seven regions (`he095` is in the candidate), but launch with lower awareness (0.43 against 0.60), stock (32k against 68k) and campaign (0.36 against 0.55). Among follow-ups that never chart, the median commercial fit is 0.62 at the field and 1.65 at the Big-5. Launch awareness and push move the national connection kink (`SingleCommercialFit.National`).

**Known-act airplay screen (`kaa55`, rejected).** The candidate carries `--known-act-airplay=0.1`: while a proven act's single is on the Hot 100, its weekly radio push is 0.1 whatever the label (Independent ~0.36, Big-5 0.55). No screen of 0.1 is documented; §17ab–ac rejected 0.6 and 0.4, and 0.1 entered with the 2026-11-03 combined-integration flag set. `kaa55-*` replaces it with 0.55 on the `mnsh40` requests, four seeds:

| | `mnsh40` | `kaa55` | History |
|---|---:|---:|---:|
| Proven-act climb premium (T40 % proven / other) | 42.3 / 46.8 | 41.7 / 47.4 | 60.5 / 38.1 |
| Follow-up events, Big-5 / other | 4.19 / 1.87 | 4.97 / 2.14 | ~3.0 / ~2.9 |
| Follow-up Top 10 events, Big-5 / other | 0.95 / 0.37 | 1.40 / 0.45 | ~1.15 / ~0.7 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 27.0 / 32.5% | 34.2 / 20.6% |
| Defunct labels by week 104 | 94.5 | 99.5 | |

Radio push does not carry the climb. Restoring it mostly lifts Big-5 hit acts past history, so the 0.1 value works as a cut on Big-5 hit-act airplay. Not adopted.

**Reading.** Proven acts get their advantage at entry (awareness, stock, connection odds) rather than in the climb, so each boost to their entry adds marginal entrants that stall below 40. In history the advantage showed on the chart as well. A historical mechanism would act after entry, independent of the label: programmers and dealers adding a proven act's charting record on its name (Top 40 playlist adds, one-stop reorders). That would apply to both tiers and narrow the Big-5's extra entries while the field gains.

**Proven-act chart add (`pca`, rejected).** `--proven-act-chart-add=V` (`Systems/ProvenActChartAdd.cs`): a single whose act had a Top 10 in the 52 weeks before launch (the hit-act fit floor's saved launch capture), while it charts at 41–100, earns chart visibility V in the weekly awareness growth instead of the low-chart value (about 0.0014 a week at No. 60; Top 40 0.025, Top 5 0.12). It applies whatever the label and only after entry. It is off by default and draws nothing; `--proven-act-chart-add-check` passes; the 27-week off run `pcaoff27-2002-sc` matches `mnsh40-2002-sc` row for row in records and label finance.

| Four seeds | `mnsh40` | `pca025` | `pca12` | History |
|---|---:|---:|---:|---:|
| Proven-act climb (T40 % proven / other) | 42.3 / 46.8 | 43.0 / 47.1 | 44.8 / 47.0 | 60.5 / 38.1 |
| Follow-up Top 40 events, Big-5 / other | 2.07 / 0.82 | 2.01 / 0.75 | 2.09 / 0.78 | ~2.25 / ~1.9 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 23.5 / 29.0% | 26.0 / 36.1% | 34.2 / 20.6% |
| Defunct labels | 94.5 | 96.3 | 101.3 | |

Even at Top 5 visibility (applied on 689 record-weeks a seed in 1960 at 0.025), the climb premium does not appear. With `kaa55`, two different levers acting after entry fail, so the climb is settled at launch ([[top40-is-a-launch-week-bar]] in memory).

**Where history's premium sits.** Top 40 % by debut position, chart events:

| Debut | History proven | History other | `mnsh40` proven | `mnsh40` other |
|---|---:|---:|---:|---:|
| 41–60 | 100 (33) | 89 (9) | 57 (106) | 90 (58) |
| 61–80 | 84 (198) | 70 (194) | 52 (197) | 72 (297) |
| 81–100 | 43 (350) | 32 (1,075) | 33 (433) | 37 (1,031) |
| Median debut | No. 84 | No. 92 | No. 85 | No. 89 |

In history a proven act's record debuts higher *and* climbs more from the same debut. In the sim it debuts higher and then climbs less: launch awareness, stock and hype carry it onto the chart, but its demand underneath is no stronger, so it stalls. Other sim records debuting at 41–80 got there on their own demand and keep climbing. The hit-act fit floor explains little of this: in the follow-up trace only 68 of about 930 charting proven-act singles were floor-supported, and those that connected on their own still climb 57% against other records' 72% at 61–80.

**Reading.** Proven acts' advantage enters the sim as launch awareness, a front-loaded push. History's looks like stronger demand: the record is better received, so it climbs. The candidate lever is to move part of the proven-act advantage from launch awareness (`--hit-act-hype=0.3`) into the size of a connected draw. That is the `ar1` direction that `FollowUpGapFableHandoff.md` rejected for Big-5 share (45–53%), so it would need a cap or a field-only scope.

**Proven-act fit size (`asz`, not adopted yet).** `--single-commercial-fit-act-record-size=S` (`Systems/SingleCommercialFit.cs`, needs `--single-commercial-fit-act-record-odds-only`): a connected draw's national log rises by S × the act record (recency of the act's last Top 40, captured and saved at launch), so a proven act's record that connects sells more instead of only connecting more often. Misses and cold acts are unchanged. Off is the odds-only shape exactly; `--single-commercial-fit-shape-check` passes with six new cases; the 27-week off run `aszoff27-2002-sc` matches `mnsh40-2002-sc` row for row. `asz05` = S 0.5; `asz05h0` adds `--hit-act-hype=0`, swapping the launch awareness lift for size. Four seeds:

| | `mnsh40` | `asz05` | `asz05h0` | History |
|---|---:|---:|---:|---:|
| Climb, T40 % proven / other | 42.3 / 46.8 | **55.5 / 42.4** | 53.6 / 45.2 | 60.5 / 38.1 |
| T10 % proven / other | 20.0 / 20.1 | 31.5 / 15.9 | 29.5 / 17.8 | 21.6 / 15.3 |
| Field follow-ups: events / T40 / T10 | 1.87 / 0.82 / 0.37 | 2.46 / 1.29 / 0.71 | 2.44 / 1.32 / 0.68 | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 |
| Big-5 follow-ups: events / T40 / T10 | 4.19 / 2.07 / 0.95 | 5.12 / 3.09 / 1.84 | 5.42 / 2.99 / 2.02 | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 31.2 / 43.9% | 31.1 / 44.2% | 34.2 / 20.6% |
| Defunct labels | 94.5 | 114.5 | 110.3 | |

By debut band (`asz05`), proven acts now climb more than others from the same debut at 81–100 (45% against 35%), and other records' Top 10 rate lands on history's. The field's Top 10 follow-ups reach history's level and its Top 40 follow-ups close about half the gap.

The cost falls on the Big-5. Their hit acts already chart about 1.4 times history's follow-ups (more than four events against about three), and the size term now makes each one climb, so their Top 10 follow-ups reach about 1.7 times history and the 1961 share goes to 44%. Removing hype does not cut those follow-ups (5.4 events). Proven records also reach the Top 10 too often (30% against 22%), so S 0.5 is too large at the top.

**Reading.** The size term is the first lever to reproduce history's climb premium, and the field comes close to history. It can't be adopted while the Big-5's excess follow-up cadence stands: the size term multiplies it. The two ways forward are a field-only scope (non-Major labels), or the size term together with a cut in the Big-5's hit-act cadence (the roster programme gives Big-5 hit acts about 4 slots a year on top of the follow-up lane), with S lowered to about 0.3 for the Top 10 rate. The Big-5 hit acts' excess entries show up in history's frame only on chart events, the measure added in this section.

**Size 0.3 with a Major hit-act gap (`asz03g12`, `g12`).** `--slate-major-hit-act-gap=W` (`Systems/LabelReleaseProgramme.cs`, read in `CanProgrammeArtist` and `HasConflictingIntention`): at an AI Major, an act with a Top 40 in the last 26 weeks waits W weeks between programme singles instead of the 6-week floor. Other labels, the player and cold acts are unchanged. `--major-hit-act-gap-check` passes; the 27-week off run `gapoff27-2002-sc` matches `mnsh40-2002-sc` row for row in records, label finance and the release slate. `g12` = W 12 alone; `asz03g12` = W 12 plus size 0.3. Four seeds:

| | `mnsh40` | `g12` | `asz03g12` | History |
|---|---:|---:|---:|---:|
| Climb, T40 % proven / other | 42.3 / 46.8 | 42.0 / 46.2 | 48.6 / 45.3 | 60.5 / 38.1 |
| T10 % proven / other | 20.0 / 20.1 | 20.5 / 20.2 | 24.0 / 19.6 | 21.6 / 15.3 |
| Big-5 follow-ups: events / T40 / T10 | 4.19 / 2.07 / 0.95 | 2.51 / 1.31 / 0.72 | **2.99 / 1.77 / 1.07** | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 |
| Field follow-ups: events / T40 / T10 | 1.87 / 0.82 / 0.37 | 2.14 / 0.90 / 0.41 | 2.48 / 1.21 / 0.60 | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 21.6 / **20.9%** | 25.5 / 28.8% | 34.2 / 20.6% |
| Established-act premium | 1.85× | 1.65× | 1.80× | |
| Defunct labels | 94.5 | 93.8 | 101.8 | |

- **The gap does what it should for the Big-5, and does it alone:** follow-up events fall from 4.2 to 2.5–3.0 and the 1961 share lands on history in `g12`. But in `g12` the 1960 share also falls, from 25.2% to 21.6% against history's 34.2%, failing the guard that 1960 should not fall further. Big-5 follow-ups then sit below history (0.72 Top 10s against 1.1–1.2).
- **The size term puts the Big-5's follow-ups back on history** (2.99 events, 1.07 Top 10) and holds 1960 at 25.5%. It lifts field follow-ups by about 40%, and freed Big-5 slots go partly to field repeat hits (field events 1.87 → 2.48).
- **At 0.3 the climb premium is weak** (48.6 against 45.3), though the Top 10 rate of proven records is about right (24% against 21.6%). Other records still reach the Top 10 too often (19.6% against 15.3%). 0.5 gave the premium but too many Top 10s.
- **Closures:** +7 with size (seeds 107 / 96 / 92 / 112 against control 89 / 101 / 89 / 99); the gap alone is flat. Control seeds span 12, so this needs a second look before adoption.
- **The 1961 share in `asz03g12` (28.8%)** falls only 1.2 points: the size term gives back most of what the gap took. Big-5 hit-act follow-ups are now historical, so the rest of the 1961 excess sits on the non-hit side, as found under plug slots.

**Reading.** `asz03g12` is the first arm where both tiers' hit-act follow-ups are near history and 1960 holds. It is a candidate, not adopted: it needs a closure check and probably S 0.4 for the climb premium. The remaining 1961 excess is the Big-5's non-hit singles; `plug4` (routine issues for non-hit singles) is the natural pair.

**Package screen (`asz04g12p4`): size 0.4, Major hit-act gap 12, Major plug slots 4.** Four seeds:

| | `mnsh40` | `plug4` | `asz03g12` | `asz04g12p4` | History |
|---|---:|---:|---:|---:|---:|
| Climb, T40 % proven / other | 42.3 / 46.8 | 42.7 / 45.5 | 48.6 / 45.3 | 51.9 / 46.8 | 60.5 / 38.1 |
| T10 % proven / other | 20.0 / 20.1 | 20.6 / 20.6 | 24.0 / 19.6 | 26.3 / 17.4 | 21.6 / 15.3 |
| Big-5 follow-ups: events / T40 / T10 | 4.19 / 2.07 / 0.95 | 4.53 / 2.06 / 1.01 | 2.99 / 1.77 / 1.07 | 2.75 / 1.57 / 0.90 | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 |
| Field follow-ups: events / T40 / T10 | 1.87 / 0.82 / 0.37 | 1.92 / 0.80 / 0.33 | 2.48 / 1.21 / 0.60 | 2.45 / 1.30 / **0.66** | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 26.7 / 24.9% | 25.5 / 28.8% | 24.8 / **25.0%** | 34.2 / 20.6% |
| Big-5 Top 10 per issue (field) | 7.8% (2.0%) | 7.1% | | 6.7% (2.13%) | ~1.9% (~1.7%) |
| 1961 Big-5 hits by acts new to the chart | 25/137 | 32/173 | | 23/115 | 3/44 |
| Defunct labels | 94.5 | 98.8 | 101.8 | 103.5 | |

Plug outcomes (Big-5 singles launched weeks 1–92, per seed): hit act plugged 154.5 at 21.0% Top 10, other plugged 96.5 at 5.4%, routine 384.5 at 1.5%, against the field's 2.1%.

- **Field hit acts now match history at the Top 10** (0.66 follow-up Top 10s per hit against 0.64–0.79) and reach about 70% of history's Top 40 follow-ups, up from 45%.
- **Big-5 hit-act follow-ups now sit a little under history** (0.90 Top 10s against 1.1–1.2). The gap and the size term together overshoot the cut; W 10 would land closer.
- **The 1961 Big-5 share is 25.0%**, the same as `plug4` alone: plug slots carry the share and the follow-up switches move whose hits they are. 1960 is 24.8%, 1.9 points below `plug4`.
- **The climb premium is about half-way** (51.9 against 46.8; history 60.5 against 38.1). Proven records reach the Top 10 too often at S 0.4 (26.3% against 21.6%), so the premium cannot be raised through S alone.
- **Closures rise by 9** (seeds 102 / 104 / 107 / 101, against control 89 / 101 / 89 / 99). All four seeds are above the control's mean, and the gain builds with each switch (`plug4` +4, `asz03g12` +7). This is the main obstacle to adoption.

**Reading.** The package makes hit-act follow-ups historical at both tiers and takes the 1961 share from 30% to 25%, but costs about nine more label closures, and 1960 stays about nine points below history. Before adoption, the closures need attributing: which labels close (tier, and whether they had hits) and why. Lowering W to 10 should restore the Big-5's follow-ups.

**Closure attribution.** Package against control, per seed: the extra closures are opening labels (+9.5; labels founded during the run −0.5) at Small, Independent and Boutique (+3 to +5 each, MidTier −1) that never had a Hot 100 record (+10.3; labels with Hot 100 records −1.3), closing in 1961 (+11.5; 1960 −2.5). By exit: Dormancy +7, Bankruptcy +2. Acts leaving those labels are not higher (34.8 in the second half of 1961 against 37.8).

- **About 3 of the 10 are composition.** Field Top 10s now go to repeat hit acts, so fewer distinct labels chart: 309 never-charting opening field labels a seed against 300. Their dormancy rate rises only from 25% to 28%.
- **Dormancy is mostly the control drawing low.** Dormancy needs a founder-run label with 12 months without a release, non-positive cash flow and less free cash than a production. The labels that closed this way stopped releasing in the first half of 1960. Across every arm on these seeds:

| Arm | Size S | Dormancy | Bankruptcy | Total | Small/Indie/Boutique gross, H1 1960 |
|---|---:|---:|---:|---:|---:|
| `mnsh40` | 0 | **27.5** | 67.0 | 94.5 | 36.7M |
| `g12` | 0 | 33.3 | 60.5 | 93.8 | 36.9M |
| `plug4` | 0 | 33.5 | 65.3 | 98.8 | 37.0M |
| `kaa55` | 0 | 29.5 | 70.0 | 99.5 | 35.3M |
| `pca025` / `pca12` | 0 | 30.0 / 31.3 | 66.3 / 70.0 | 96.3 / 101.3 | 37.1 / 36.8M |
| `asz03g12` | 0.3 | 30.0 | 71.8 | 101.8 | 35.6M |
| `asz04g12p4` | 0.4 | 34.5 | 69.0 | 103.5 | 35.4M |
| `asz05h0` / `asz05` | 0.5 | 34.8 / 36.5 | 75.5 / 78.0 | 110.3 / 114.5 | 36.0 / 34.2M |

  The control has the fewest dormancies of any arm, and arms that never touch field labels (`g12`, `plug4`) have 33. Dormancy moves with the reshuffle, not the size term.
- **The size term's real cost is bankruptcies, and it bites at 0.5.** Arms without it run 60.5–70 bankruptcies; 0.3–0.4 sit at the top of that range (69–72); 0.5 reaches 75.5–78. Small labels' gross in early 1960 falls with S (36.7M → 34.2M at 0.5): the singles purse is fixed, and opening hit acts count as proven (pre-1960 Top 40s via the hit-act clock), so their larger 1960 hits take units from small labels' records.

**Reading.** At S 0.4 the package's closure excess is mostly noise against a low control: dormancy is in the S 0 range and bankruptcies are at its top. The closure guard limits S to about 0.4. Size 0.5 is excluded on closures as well as Top 10 rate.

**Gap 10 (`asz04g10p4`).** Same package with W 10. Four seeds:

| | `asz04g12p4` | `asz04g10p4` | History |
|---|---:|---:|---:|
| Big-5 follow-ups: events / T40 / T10 | 2.75 / 1.57 / 0.90 | **3.46 / 1.99 / 1.21** | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 |
| Field follow-ups: events / T40 / T10 | 2.45 / 1.30 / 0.66 | 2.50 / 1.25 / 0.68 | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 |
| Climb, T40 % proven / other | 51.9 / 46.8 | 48.7 / 47.6 | 60.5 / 38.1 |
| T10 % proven / other | 26.3 / 17.4 | 26.5 / 20.1 | 21.6 / 15.3 |
| Big-5 Top 10 share 1960 / 1961 | 24.8 / **25.0%** | 31.4 / 32.5% | 34.2 / 20.6% |
| Big-5 / field Top 10 per issue | | 4.14× | ~1.1× |
| Dormancy / bankruptcy / total closures | 34.5 / 69.0 / 103.5 | 35.0 / 75.0 / 110.0 | |

W 10 puts Big-5 hit-act follow-ups on history but returns the 1961 share to 32.5% and raises bankruptcies to 75. W 12 is kept: it gives up about a quarter of the Big-5's follow-up Top 10s for a 7.5-point lower 1961 share. A two-week change moving the share this far is larger than the follow-up change implies, so part of it may be reshuffle; 1960 moves with it (24.8 → 31.4%).

**Two untried levers for the climb premium.** Half of the premium gap is on the non-proven side: their records reach the Top 40 at 46.8% against history's 38.1%. From the same debut band they climb like history's (61–80: 72 against 70%; 81–100: 37 against 32%), but fewer of them debut at the bottom: 74% of non-proven events debut at 81–100 against 84% in history, and the sim has about 350 non-proven events a year against 426. The missing ~80 short bottom-chart runs a year look like 1960's regional records, which the store-and-station-report Hot 100 charted low nationally. A chart lever giving regional action weight at the bottom would add them; it needs a check against the earlier regional-blend and chart-bottom work first. On the proven side, a size term that tapers to zero for strong draws would lift marginal proven records into the Top 40 without enlarging big hits, which addresses the Top 10 overshoot (26% against 21.6%).

**Size taper (`t06g12p4`, rejected).** `--single-commercial-fit-act-record-size-taper=T`: the lift is full up to the size kink and falls linearly to zero T standard deviations above it. It is measured from the size kink because draws between the act's kink and the size kink sell at the just-connected level, so a lift fading from the act's kink would make a better draw sell less. Configuration requires S ≤ T × upside, and the shape check (39 cases) tests monotonicity at S = T. The 27-week off run matches `mnsh40` row for row. S 0.6, T 1.0, with gap 12 and plug 4, four seeds against `asz04g12p4`: climb 55.1 / 46.3 (from 51.9 / 46.8), but proven records reach the Top 10 *more* often, 30.6% against 26.3%. Field follow-ups move to 1.48 Top 40 / 0.82 Top 10, Big-5 to 1.86 / 1.25, the 1961 share to 26.8% and 1960 to 23.2%, and bankruptcies rise to 77 (total closures 111.8). Proven Top 10s come from modest draws carried by a strong launch, so tapering the large draws cannot hold the Top 10 rate down.

**Regional chart action (`rca02`, rejected in this form).** The material exists: on each seed, about 150 never-charting newcomer singles had a week in which one region alone sold at least 40% of the national No. 100's units, with about 90% of their sales there (`newcomer-connect-trace`, ages 1–12). `--chart-regional-action=L` (`Systems/RegionalChartAction.cs`, read in `CalculateChartPoints`): a single's sales points gain L × (best region's units ÷ its population share − national units), capped at its own units. `--chart-regional-action-check` passes; the 27-week off run matches `mnsh40` row for row. L 0.2 on `mnsh40`, four seeds:

| | `mnsh40` | `rca02` | History |
|---|---:|---:|---:|
| Hot 100 debuts, 1961 per seed | 534 | 518 | |
| Non-proven events debuting 81–100 (Top 40 %) | 1,031 (37.1) | 1,027 (38.5) | 1,075 / 3 yr (31.9) |
| Climb, T40 % proven / other | 42.3 / 46.8 | 47.7 / 49.3 | 60.5 / 38.1 |
| Follow-ups, Big-5 / field events | 4.19 / 1.87 | 4.17 / 1.56 | ~3.0 / 2.7–3.1 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 24.3 / 31.3% | 34.2 / 20.6% |
| Closures (dormancy / bankruptcy) | 27.5 / 67.0 | 28.8 / 69.0 | |

The bottom does not widen: debuts fall and non-proven records climb more, not less. The bonus is not selective. With the regional fit draw (σ 0.5), almost every record has one region selling above its share, so ordinary records gain points too and the bottom cutoff rises with them. Concentrated regional sellers gain little relative to the rest. A selective version would pay only skew well beyond the regional draw's spread (for example, the excess of the best region's equivalent over K × national units with K about 3), or would rank-score each region's top list as the store reports did.

**Selective regional action (`rcs3`, rejected).** `--chart-regional-action-skew=K` pays only the excess of the best region's equivalent over K × national units; the check adds two skew cases and passes. L 1.0, K 3 on `mnsh40`, four seeds:

| | `mnsh40` | `rca02` | `rcs3` | History |
|---|---:|---:|---:|---:|
| Hot 100 debuts, 1961 per seed | 534 | 518 | 528 | |
| Non-proven events debuting 81–100 (Top 40 %) | 1,031 (37.1) | 1,027 (38.5) | 1,015 (35.5) | 1,075 / 3 yr (31.9) |
| Climb, T40 % proven / other | 42.3 / 46.8 | 47.7 / 49.3 | 45.5 / 49.1 | 60.5 / 38.1 |
| Follow-ups, Big-5 / field events | 4.19 / 1.87 | 4.17 / 1.56 | 4.62 / 1.67 | ~3.0 / 2.7–3.1 |
| Big-5 follow-up Top 10s per anchor | 0.95 | 1.03 | 1.36 | 1.08 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 24.3 / 31.3% | 24.6 / 31.8% | 34.2 / 20.6% |
| Closures (dormancy / bankruptcy) | 27.5 / 67.0 | 28.8 / 69.0 | 31.0 / 64.5 | |

Skew does not single out the regional sellers. Using each region's share of single units in place of population, the `mnsh40` newcomer trace (ages 1–12, record-weeks with at least 20 units) gives a best-region ratio, the best region's equivalent ÷ national, with a median of 3.0 on chart weeks and 3.16 for never-charted records selling at least 0.4 × the No. 100's units. The ratio exceeds 3 on half of chart weeks and 61% of the near-misses, and exceeds 5 on 18% and 22%. Newcomers launch in a few covered regions, so their charting records are as concentrated as the ones that miss. The bonus scales with a record's own units, so records with similar ratios keep their order. The bonus mostly reached proven and Big-5 records: Big-5 follow-up Top 10s rose to 1.36 per anchor and the 1961 share to 31.8%. No threshold on skew relative to the record's own sales can widen the bottom. The remaining form is an additive one, points for a record's rank in each region's list regardless of its national size, as the 1960 store reports gave. That is a change to how the chart is built and needs to be checked against the chart-bottom target first.

**Store-report rank scoring (`rrk5`, rejected).** `--chart-regional-rank=B` (`Systems/RegionalChartRank.cs`): each region ranks its singles by the week's units. Rank r up to the list depth D (`--chart-regional-rank-depth`, default 50) earns the Carroll chart-score weight, tapered to zero at D + 1. A record's store score sums those weights over regions, each weighted by population share raised to A (`--chart-regional-rank-market-weight`, default 1, renormalized). The score is converted to national units so that the week's top 100 by units keep their total, and sales points become (1 − B) × units + B × score. Each week's score is computed once before ranking and cached on the record (absent from saves when off), so the audit recomputation stays exact. `--chart-regional-rank-check` passes, and the 27-week off run matches `mnsh40` row for row. B 0.5, D 50, A 0.5 (small markets' lists count more than their buyers), four seeds:

| | `mnsh40` | `rcs3` | `rrk5` | History |
|---|---:|---:|---:|---:|
| Hot 100 debuts, 1961 per seed | 534 | 528 | 516 | |
| Non-proven events debuting 81–100 (Top 40 %) | 1,031 (37.1) | 1,015 (35.5) | 1,055 (36.7) | 1,075 / 3 yr (31.9) |
| Climb, T40 % proven / other | 42.3 / 46.8 | 45.5 / 49.1 | 49.3 / 47.5 | 60.5 / 38.1 |
| Follow-ups, Big-5 / field events | 4.19 / 1.87 | 4.62 / 1.67 | 4.45 / 1.78 | ~3.0 / 2.7–3.1 |
| Big-5 follow-up Top 10s per anchor | 0.95 | 1.36 | 1.36 | 1.08 |
| Big-5 Top 10 share 1960 / 1961 | 25.2 / 30.0% | 24.6 / 31.8% | 23.3 / 33.5% | 34.2 / 20.6% |
| #10 ÷ #100 units, 1961 (seed medians) | 10.5–11.9 | | 9.9–11.4 | |
| Closures (dormancy / bankruptcy) | 27.5 / 67.0 | 31.0 / 64.5 | 32.3 / 67.3 | |

Rank scoring adds 24 bottom debuts over four seeds, but total debuts fall by 18 a seed and non-proven records still climb at 47.5%. The gain again goes to Big-5 follow-ups (1.36 Top 10s per anchor) and the 1961 share (33.5%). All three forms (proportional, skew-gated and rank-scored) give the same result: any credit for regional strength lengthens the runs of records already charting and favours the Big-5's proven acts, whose follow-ups lead in their strong regions. None adds the short bottom runs. **Reading.** The missing ~80 non-proven bottom events a year are not a chart-construction miss the sales score can fix. They need records that sell briefly near the cutoff, which is a supply question (how many marginal newcomer singles exist and how fast they fade), not a ranking question. The regional chart switches stay off. (The next section corrects this reading: supply is there, and the slots are held by falling records.)

## Where the chart bottom's slots go (2026-11-18)

The Hot 100 has 5,200 slot-weeks a year, so events × mean chart life is fixed. Fewer short bottom runs must mean longer runs somewhere. History is the Hot 100 of 1961–63; the sim is 1961 on `mnsh40`, four seeds, per seed-year. Score with `python SimTools/score_chart_faller_depth.py ARM`.

| | History | `mnsh40` |
|---|---:|---:|
| Debuts / chart events a year | 674 / 620 | 534 / 531 |
| Mean chart life, weeks | 7.7 | 8.9 |
| Share of slot-weeks held by records past their chart peak | 30.4% | 45.7% |
| Top 40 peakers: weeks after the peak | 4.1 | 6.9 |
| Top 40 peakers: median last chart position | **No. 50** | **No. 81** |
| Record past its peak at 41–60: off the chart the next week | **58.8%** | **2.7%** |
| Non-proven events a year (debuting 81–100) | 426 (360) | 337 (251) |

**History's records left the chart from about No. 50, whatever their peak.** The median last position of a completed run is 48, 49, 49 and 52 for records peaking at 1–3, 4–10, 11–20 and 21–40, and 79% of Top 40 peakers last appear at No. 60 or higher (for example "Hit the Road Jack": … 6, 7, 22, 47, gone). In the sim the same bands leave at 82, 83, 78 and 81, and only 5.5% of Top 40 peakers leave from No. 60 or higher. Weeks before the peak match history in every band above No. 40 (6.8–7.2 against 6.1–7.0). Weeks after it run about three longer (10.3 / 7.6 / 6.1 / 5.1 against 7.2 / 5.0 / 3.7 / 2.7). In history, records past their peak hold 125 slot-weeks a year at 81–100; in the sim they hold 435. Everything else about the bottom is close: bottom-only records last 2.1 weeks against 2.3.

**What holds a falling record.** At 61–100, 97.5% of a falling record's points are units: about 6,000 a week, four weeks past its sales peak. Airplay is about 36% of a No. 1's points by design, and the fifth-power rotation makes it negligible at the bottom. The 1960 Hot 100 was a survey of about 110 outlets, 63 of them radio stations (`ChartSimulator.cs`, survey comment), and outlets reported only what was on their own lists. Wikipedia's Hot 100 article says that before 1991 stations and stores dropped a song from their manual reports once its label stopped promoting it, so songs fell quickly after peaking. The sim ranks on residual national units, so a falling record slides on them to about No. 80. [Billboard Hot 100](https://en.wikipedia.org/wiki/Billboard_Hot_100)

**Supply is there.** Static replay of the logged `mnsh40` weeks (scratch `replay.py`; no feedback): below No. D, list only singles fewer than G weeks past their own sales peak, refilling from the logged pool. In these calibration runs `records.csv` logs off-chart singles only for the follow-up trace (hit acts' next singles), so the pool adds every connected NewSigning/Unsigned single to age 12 from the newcomer trace, scored on national units. The pool fills the freed slots (11–25 slot-weeks a year unfilled of 5,200).

| Replay | Events / yr | Non-proven / proven events | T40 % non-proven / proven | Last pos, T40 peakers | Past-peak share |
|---|---:|---:|---:|---:|---:|
| History | 620 | 426 / 195 | 38.1 / 60.6 | 50 | 30.4% |
| As run | 531 | 337 / 188 | 46.6 / 42.8 | 81 | 45.7% |
| D 50, G 2 | 698 | **425** / 265 | **37.6** / 30.8 | 43 | 31.9% |
| D 50, G 3 | 649 | 395 / 246 | 40.3 / 33.4 | 43 | 35.2% |
| D 55, G 2 | 680 | 415 / 257 | 38.6 / 31.7 | 47 | 33.2% |

At D 50, G 2 the non-proven side lands on history: 425 events a year reaching the Top 40 at 37.6%. The Top 40 itself is unchanged by construction (225 records a year against history's 293). The proven side moves the other way. Hit acts' near-miss follow-ups now chart at the bottom, so proven events rise from 188 to 265 a year while their Top 40 events stay at about 80 (history 118). History's proven act charted fewer records and most of them climbed; the sim's issues more weak follow-ups that miss the chart only because falling records hold the bottom.

**Reading.** The missing short bottom runs are crowded out by falling records, not missing from supply. The three regional forms could not add them, because each one paid records already charting. A chart that lists falling records only above about No. 50 gives back history's non-proven bottom in a static replay.

**Probe.** `--chart-faller-report-depth=D` (`Systems/ChartFallerReportDepth.cs`, applied to the points ranking in `ChartManager`): a single ranked below No. D on points and at least G weeks past its own weekly sales peak (`--chart-faller-report-grace=G`, default 2) goes unreported. It is dropped from the week's ranking, so it is off the Hot 100 and Bubbling Under, and records below it move up. Points are unchanged and nothing is drawn. `--chart-faller-report-depth-check` passes. In a live run, a dropped faller also loses chart exposure; under the candidate's `--uncharted-single-exposure=0.5`, a stalled uncharted single falls toward half exposure, as dealers returned records with no chart or regional support. The 27-week off run (`frdoff27-2002-sc`) matches `mnsh40-2002-sc` row for row in records, label finance and the release slate.

**Live screen (`frd50` = D 50, G 2 on `mnsh40`, four seeds).** The neutral range is the four arms that never touch the field's chart (`g12`, `plug4`, `kaa55`, `pca025`) in the same scorer.

| | History | `mnsh40` | `frd50` | Neutral arms |
|---|---:|---:|---:|---:|
| Chart events a year | 620 | 531 | 730 | 523–532 |
| Top 40 peakers: weeks after the peak / median last position | 4.1 / No. 50 | 6.9 / No. 81 | 4.8 / No. 41 | |
| Record past its peak at 41–60: off next week | 58.8% | 2.7% | 71.9% | |
| Slot-weeks past the peak | 30.4% | 45.7% | 32.1% | |
| Non-proven events a year (debuting 81–100) | 426 (360) | 337 (251) | 470 (376) | 339–360 |
| Non-proven: T40 % / T10 % | **38.1 / 15.3** | 46.6 / 20.1 | **35.8 / 15.2** | 45.5–47.6 |
| Proven events a year; T40 % / T10 % | 195; 60.6 / 21.6 | 188; 42.8 / 20.0 | 262; 28.1 / 14.5 | 164–193; 41.6–43.2 |
| Follow-ups per 1960 anchor, field: events / T40 / T10 | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 | 1.87 / 0.82 / 0.37 | **2.66** / 0.78 / 0.36 | |
| Follow-ups per 1960 anchor, Big-5: events / T40 / T10 | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 | 4.19 / 2.07 / 0.95 | 5.29 / 1.67 / 1.03 | |
| Big-5 Top 10 share 1960 / 1961 | 34.2 / 20.6% | 25.2 / 30.0% | 21.5 / 30.9% | 21.6–27.0 / 20.9–32.5% |
| #10 ÷ #100 units, 1961; units share of 41–100 | | 11.4; 25.4% | **18.3**; 23.0% | 11.5–12.0; 25.3–25.4% |
| Single units; Big-5 share of them | | 251.5M; 23.9% | 251.6M; 22.9% | |
| Opening labels closed (dormancy / bankruptcy) | | 91.0 (27.5 / 63.5) | 99.2 (32.8 / 66.5) | 89.0–96.2 (29.2–33.2 / 56.0–67.0) |

- **The descent is now history's.** Weeks after the peak by peak band are 8.3 / 5.4 / 4.0 / 2.9 against history's 7.2 / 5.0 / 3.7 / 2.7 (No. 1–3, 4–10, 11–20, 21–40). The rule is slightly too sharp: records leave from No. 41 against No. 50, and a faller at 41–60 leaves 72% of weeks against 59%. D 55–60 would land closer.
- **The non-proven half of the climb premium is closed.** Non-proven records reach the Top 40 35.8% of the time and the Top 10 15.2% (history 38.1 and 15.3). Their excess climb in the control came from fewer, larger records charting, not from records climbing too readily.
- **The proven half is now the whole gap.** Proven acts chart 262 events a year against history's 195, but their Top 40 events stay at about 74 a year (history 118) and their Top 10 events at about 38 (history 42). The extra events are weak follow-ups that used to miss the chart. Field hit acts' follow-up events reach history (2.66 per anchor) while their Top 40 follow-ups stay at 0.78 against 1.7–2.1. Big-5 hit acts chart 5.3 follow-ups against about 3.
- **The bottom now turns over a little too fast.** 730 events a year against 620. Records peaking at 61–80 rise for 1.9 weeks against history's 3.6, and there are about 35% more of them. History's marginal records crept up the bottom; the sim's peak almost at once (the climbs-are-pointed result in `Top10TurnoverFindings.md`).
- **Guards.** The 1961 Big-5 share is unchanged. 1960 falls on three of four seeds to 21.5%, the bottom of the neutral range. Units are fixed by the purse; the Big-5 lose a point of unit share as their fallers' tails lose chart exposure. Closures rise on all four seeds to 99.2, three above the neutral range. Labels with a Top 40 record close as before (7.2 → 6.5 a seed). The rule moves 44 labels a seed from never charting to charting only at 41–100. The labels left never charting close at 28.7% against 24.0%, since they are now the weaker group, and the bottom-only group closes at 11.6% against 9.7%. **#10 ÷ #100 units go from 11.4 to 18.3**, because the No. 100 is now a small rising record instead of a large falling one. The chart-bottom target (memory `chart-bottom-target`) set about 11 from the Carroll and Hesbacher score curves. On a survey chart those curves describe reported score, not units, so this guard needs the author's reading.

**Reading.** The 1960 chart's lower half was a list of records still rising, and falling records left it from about No. 50. Reproducing that closes the non-proven half of the proven-act climb premium and puts field hit acts' follow-up count on history. Not adoptable alone: closures sit just above the neutral range, 1960 sits at its bottom, and #10 ÷ #100 needs a decision. The natural pair is the follow-up package (`asz04g12p4`): its size term lifts proven records' climb, and its gap cuts the Big-5 hit acts' extra follow-ups, which the rule now shows as stalled bottom events. Use D 55–60 for the exit point.

**Rule plus follow-up package (`frd60pk` = D 60, G 2, size 0.4, Major hit-act gap 12, plug slots 4 on `mnsh40`, four seeds).**

| | History | `mnsh40` | `asz04g12p4` | `frd50` | `frd60pk` |
|---|---:|---:|---:|---:|---:|
| Chart events a year; mean life | 620; 7.7 | 531; 8.9 | 501; 9.6 | 730; 6.5 | **656; 7.2** |
| Top 40 peakers: median last position; faller at 41–60 off next week | 50; 58.8% | 81; 2.7% | 83; 1.7% | 41; 71.9% | **48; 61.7%** |
| Slot-weeks past the peak | 30.4% | 45.7% | 45.2% | 32.1% | 33.8% |
| Non-proven events; T40 % / T10 % | 426; 38.1 / 15.3 | 337; 46.6 / 20.1 | 332; 46.9 / 17.2 | 470; 35.8 / 15.2 | **435; 35.3 / 14.0** |
| Proven events; T40 % / T10 % | 195; 60.6 / 21.6 | 188; 42.8 / 20.0 | 174; 51.7 / 26.4 | 262; 28.1 / 14.5 | 220; 39.3 / **22.3** |
| Field follow-ups per 1960 anchor: events / T40 / T10 | 2.7–3.1 / 1.7–2.1 / 0.64–0.79 | 1.87 / 0.82 / 0.37 | 2.45 / 1.30 / 0.66 | 2.66 / 0.78 / 0.36 | **3.10** / 1.30 / **0.74** |
| Big-5 follow-ups per 1960 anchor: events / T40 / T10 | 2.9–3.2 / 2.2–2.3 / 1.1–1.2 | 4.19 / 2.07 / 0.95 | 2.75 / 1.57 / 0.90 | 5.29 / 1.67 / 1.03 | **3.19** / 1.38 / 0.81 |
| Top 40 records a year | 293 | 226 | 230 | 223 | 221 |
| Big-5 Top 10 share 1960 / 1961 | 34.2 / 20.6% | 25.2 / 30.0% | 24.8 / 25.0% | 21.5 / 30.9% | 22.1 / **21.9%** |
| #10 ÷ #100 units; units share of 41–100 | | 11.4; 25.4% | 12.4; 25.4% | 18.3; 23.0% | 19.5; 23.5% |
| Opening labels closed (dormancy / bankruptcy) | | 91.0 (27.5 / 63.5) | 100.5 (34.2 / 66.2) | 99.2 (32.8 / 66.5) | **107.8 (34.2 / 73.5)** |

- **On history together for the first time:** chart turnover and exit point, the non-proven climb, both tiers' follow-up counts, field follow-up Top 10s, proven records' Top 10 rate, and the 1961 Big-5 share (19.4 / 28.1 / 21.7 / 18.3 by seed).
- **The climb premium is positive but small:** proven 39.0% against other 35.5% (history 60.5 against 38.1). Proven records reach the Top 10 at history's rate but stall between 41 and 100 more often. Proven Top 40 events are about 86 a year against history's 118, and Top 40 records overall stay at 221 against 293 in every arm. The size term lifts how far a connected proven record goes, not how many reach the Top 40.
- **Big-5 follow-ups now sit under history at the Top 40 and Top 10** (1.38 / 0.81 against 2.2–2.3 / 1.1–1.2), as in `asz04g12p4`. 1960 stays at 22.1%, the bottom of the neutral range.
- **Closures are the blocker.** All four seeds close 107–109 opening labels, against 89–96 for the neutral arms; bankruptcies are 73.5 against 56–67. The two parts roughly add (package +9.5, rule +8). By tier, against the control: Independents +8.7 (6.7 bankruptcies), Small +4.7, Boutique +3.6, MidTier none. By chart history, labels with a Top 40 record close as before (7.3 → 6.3 a seed). There are fewer of them (179 → 160), because the package gives field Top 10s to repeat hit acts. About 50 labels move from never charting to charting only at 41–100. The labels left never charting close at 31% against 24%. Their singles sell 1.4M units in 1961 against 2.7M in the control (Independent, Small and Boutique). The purse is fixed, and more of it now goes to records with chart exposure, including the extra bottom entrants.

**Reading.** The faller rule and the follow-up package fit together: the rule fixes the chart's construction and the non-proven side, and the package fixes hit acts' cadence and Top 10 conversion. What is left is the proven Top 40 rate, the 1960 Big-5 share, and closures. The closure cost runs through the fixed purse to labels that never chart. Before tuning further, it needs splitting into how much comes from the rule's chart exposure (fallers lose it, bottom entrants gain it) and how much from the size term's larger proven hits.

**Closure split (2026-11-18).** Opening labels are identical across the arms of a seed, so each can be paired across worlds. In every arm, including the neutral ones, 31–54 opening Independent, Small and Boutique labels a seed close that thrived in the control. In the control those labels sold about 340,000 units, earned about $62,000, and 57% charted. Another 31–44 a seed close in the control but survive in the arm. Which labels close is mostly a lottery on whether a label's first singles land. The treatment effect is the difference between the two pools (closed in the arm only, minus closed in the control only):

| Pair | Arm only | Control only | Net a seed |
|---|---:|---:|---:|
| `mnsh40` → `g12`, `plug4`, `kaa55`, `pca025` (neutral) | 33–43 | 31–44 | −1.3 to +5.0 (mean +1.9) |
| `mnsh40` → `frd60` (rule alone, D 60) | 42.0 | 34.5 | **+7.5** |
| `mnsh40` → `frd50` (rule alone, D 50) | 45.5 | 36.8 | +8.8 |
| `mnsh40` → `asz04g12p4` (package alone) | 46.3 | 35.8 | **+10.5** |
| `asz04g12p4` → `frd60pk` (rule added to the package) | 45.5 | 39.0 | +6.5 |
| `mnsh40` → `frd60pk` (both) | 53.5 | 36.5 | +17.0 |

The two costs add: rule 6.5–7.5 plus package 10.5 against 17 together. Net of the neutral arms' mean, about 5–6 a seed come from the rule at D 60 and about 8.5 from the package. Two-year closure by starting tier, control → `frd60pk` (neutral range): Independent 15.1 → 19.3% (14.8–16.9), Small 17.0 → 19.0% (15.7–17.2), Boutique 6.1 → 9.1% (5.9–7.4). The closures are micro-labels: those that close gross about $8,000 over two years and lose about $6,000. Total Independent, Small and Boutique gross and units move less between arms than they do among the neutral arms.

- **The rule's channel is the minor chart record's tail.** Connected newcomer singles that peaked at 41–100 (newcomer trace, ages 1–12) sell, relative to their chart-peak week:

  | Weeks after the chart peak | +2 | +3 | +4 | +5 |
  |---|---:|---:|---:|---:|
  | Peak 41–60, `mnsh40` / `frd50` | 0.95 / 0.86 | 0.85 / 0.65 | 0.70 / 0.30 | 0.60 / 0.18 |
  | Peak 61–80, `mnsh40` / `frd50` | 0.81 / 0.88 | 0.63 / 0.60 | 0.46 / 0.28 | 0.40 / 0.20 |

  Units in the six weeks after the peak fall 16% (peak 41–60), 22% (61–80) and 34% (81–100, partly the smaller entrants). Off the chart, a record loses chart exposure and, once stalled, drops to the candidate's 0.5 uncharted floor. A label living on one minor chart record loses that tail. In 1960 a record leaving the chart had also lost its reporting stations and dealers, so the faster tail is plausible history. These labels are the sheltered small labels already flagged (memory `small-labels-never-lose-money`; the design band is 20–35% of starting-Small labels closed in five years).
- **The package's channel is the size term**, as found before: bankruptcies through the fixed purse. Field Top 10s also go to repeat hit acts, so fewer labels ever get a Top 40 record (179 a seed → 165 → 160). Labels with a Top 40 record close at about 4%.

**The rule alone at D 60 (`frd60`).** Chart structure: 690 events a year, last position No. 48, a faller at 41–60 leaves 62.3% of weeks, 34.4% of slot-weeks past the peak. Non-proven T40 / T10 36.5 / 16.9% (history 38.1 / 15.3). Proven 239 events at 33.3% T40. Guards: Big-5 Top 10 share 28.2% in 1960 and 32.4% in 1961 (control 25.2 / 30.0; by seed 41.0 / 28.5 / 21.3 / 38.7), so the 1960 fall under `frd50` was reshuffle. Opening labels closed 97.8 (dormancy 31.8, bankruptcy 66.0; neutral 89.0–96.2, 29.2–33.2, 56.0–67.0). #10 ÷ #100 units 16.6; units share of 41–100 24.2%.

**Reading.** At D 60 the rule alone costs about 5–6 closures a seed beyond noise. Its total sits 1.6 above the neutral range, with dormancy and bankruptcy each inside theirs. That cost is minor chart records losing a tail that 1960 records also lost. The package costs more, through the size term. `frd60` is the candidate form of the rule. Its open guard is #10 ÷ #100: the published ratio is 16.6, and the sales-ranked ratio (all singles by units) still needs logging.

**Adoption check for `frd60` (2026-11-18): not adoptable alone.**

*Sales-ranked bottom.* `--sales-rank-telemetry` (audit runner, telemetry only) writes each week's 1st, 10th, 25th, 40th, 50th, 60th, 75th, 100th, 150th and 200th best-selling single by units, with its published position and weeks past its sales peak. Control and `frd60` were rerun with it on seeds 1001 and 2002 (`srkc-*`, `srkf60-*`). Each rerun matches its original row for row in records, label finance and the release slate. Score with `python SimTools/score_sales_rank_guard.py RUN ...`. 1961 medians over weeks:

| | Control 1001 / 2002 | `frd60` 1001 / 2002 |
|---|---:|---:|
| Sales-ranked #10 ÷ #100 (target ~5–11×) | 11.5 / 11.0 | 12.9 / 12.7 |
| Published #10 ÷ #100 | 11.8 / 10.5 | 17.7 / 15.4 |
| Sales-ranked No. 100 units; published No. 100 units | 4,235 / 4,405; 4,188 / 4,290 | 3,951 / 3,980; 2,558 / 2,932 |
| Sales-ranked No. 60 not on the published chart | 0% | 21–23% (all 2+ weeks past their peak) |

The sales curve barely moves. Fallers losing their tail lower the 100th-best seller by about 8%, which puts the sales-ranked ratio about 1.5 above the control, just over the target's 11× top. The published ratio steepens mostly because No. 100 is now a smaller rising record. A fifth of the sales chart's No. 60s are falling records the survey no longer lists. On this guard the rule is marginal, not failing.

*Two guards fail.* Pooled 1960–61 launches to week 92, four seeds (`analyze_footprint_scope.py`), with the neutral range from `g12`, `plug4`, `kaa55` and `pca025`:

| | History | Control | Neutral arms | `frd50` | `frd60` | `frd60pk` |
|---|---:|---:|---:|---:|---:|---:|
| Big-5 newcomer Top 10 records, 1961, per seed | ~4.5 | 4.5 | 4.5–5.8 | 7.8 | **7.8** | 4.2 |
| Newcomer Top 10 premium (Big-5 ÷ field) | | 2.04 | 1.66–2.54 | 2.47 | **2.65** | 2.09 |
| Top 10 records, 1961, per seed | 110.7 | 115.3 | 116.3–119.5 | 118.5 | **124.8** (129 / 124 / 123 / 123) | 117.5 |
| One-week Top 10s / No. 1 records a year | 11.5% / 20.7 | 12.6% / 20.3 | 14.1–16.3% / 19.3–22.0 | 15.3% / 20.8 | 16.5% / 18.3 | 14.9% / 18.3 |

Clearing fallers off the bottom gives early-charting records a higher position and more chart exposure. Big-5 newcomers ship nationally with full stock, so they turn that into climbs, and the newcomer premium that the shared fit-exposure draw removed partly comes back (1961 Big-5 newcomer Top 10s 4.5 → 7.8 at both depths). Top 10 turnover rises on every seed at D 60. With the follow-up package, both return to the control: the plug slots launch most Big-5 newcomer singles as routine issues, so they cannot use the freed bottom. The size term touches only proven acts and the gap only hit acts. The plug slots and the gap were neutral on closures alone (`plug4` 94.8, `g12` 89.0 against 91.0). The package's closure cost is the size term's.

**Reading.** The rule needs the plug slots, and the 1961 share needs them too. The size term buys the proven side (Top 40 39.3% against 33.3% without it; Top 10 22.3% against 16.7%, history 60.6 / 21.6) for about 8.5 closures a seed. The next screen is `frd60gp` (D 60, gap 12, plug slots 4, no size) to price the rule with the parts of the package that cost nothing. A size 0.3 arm on top of it shows what the proven side is worth per closure.

**Adopted (2026-11-18): `frd60pk`.** The author adopted the full arm, accepting its two costs: more opening-label closures (91 → 108 a seed), and a lower 1960 Big-5 share (Top 10 25.2 → 22.1%, Hot 100 20.3 → 15.9%; history 34.2% at the Top 10). The 1960 dip was then treated as an opening-roster question; "The 1960 dip" below finds it is Top 10 run length instead. Closures were judged low to begin with. `SimLogs/writing-calibration/candidate-request.json` now ends with `--chart-faller-report-depth=60 --single-commercial-fit-act-record-size=0.4 --slate-major-hit-act-gap=12 --major-plug-slots=4` (260 flags; previous file `candidate-request-pre-faller-depth-2026-11-18.json`). Apart from `--run`, `--seed`, `--weeks` and the telemetry switches `--regional-home-sales`, `--gzip-telemetry` and `--newcomer-connect-trace`, it equals the `frd60pk-*-sc` run requests, which are the 104-week controls for the next screens. In 1961 the adopted candidate gives: Big-5 Top 10 share 21.9% (history 20.6%); CR4 26.2% (27.9%); 656 chart events (620), with records leaving at No. 48 (No. 50); non-proven Top 40 rate 35.3% (38.1%); follow-ups per hit 3.10 field and 3.19 Big-5 (2.7–3.1 and 2.9–3.2); Big-5 net $104M (control $103M). Open items: the proven Top 40 rate (39% against 61%), Top 40 records a year (221 against 293), 1960, and the sales-ranked #10 ÷ #100 (12.8 for the rule alone, not yet measured with the size term).

## The 1960 dip (2026-11-18)

On the adopted controls (`frd60pk-*`, four seeds) the Big-5 hold 22.0% of 1960 Top 10 slots (27.9 / 16.5 / 23.7 / 19.8 by seed) against history's 34.2%: 114 slot-weeks a seed against 178. The working guess was the opening roster. The gap is not there. Score with `python SimTools/analyze_big5_1960_dip.py frd60pk`. History's year is the 52 charts from 1959-12-28; the sim's is weeks 0–51.

| 1960 Top 10 | History Big-5 | `frd60pk` Big-5 | History field | `frd60pk` field |
|---|---:|---:|---:|---:|
| Records | 28 | 27.3 | 74 | 91.5 |
| Acts | 20 | 18.5 | 54 | 65.5 |
| Records per act | 1.40 | 1.47 | 1.37 | 1.40 |
| Weeks per record | **6.36** | **4.23** | 4.62 | 4.47 |
| Records with 7+ / 10+ weeks | 12 / 6 | 4.0 / 1.3 | 19 / 3 | 18.0 / 5.3 |

**The count is right; the run length is not.** The Big-5 place history's number of records and acts in the Top 10. Their records stay 4.2 weeks against history's 6.4, which accounts for about 58 of the 64 missing slot-weeks (27.3 × 2.1). The gap opens mid-year: Big-5 share by quarter is 28.1 / 17.5 / 15.8 / 26.5% against history's 31.5 / 47.7 / 30.0 / 26.2%. History's second quarter is where several long runs overlap (Summer Place, He'll Have to Go, Stuck On You, Greenfields, Sink the Bismark, Cradle of Love).

**History's premium belongs to 1960 alone.** Big-5 weeks per Top 10 record were 6.36, 4.46, 4.58, 4.22 and 5.11 in 1960–64, and the field's were 4.43–4.62 every year. Big-5 records with 7+ weeks numbered 12 in 1960 and 5–7 in each later year. The sim gives the Big-5 4.2–4.6 weeks in every arm checked (`mnsh40` 4.16, `plug4` 4.49, `frd60` 4.55, `frd60pk` 4.23) and 4.29 in 1961. It reproduces history's 1961–63, not its 1960.

**It is not the stars.** History's 1960 Top 10 split by the act's pre-1960 record (hand-coded: a pop Top 10 before 1960, a Top 40 only, or neither; approximate):

| 1960 Top 10, history | Records | Slots | Weeks per record |
|---|---:|---:|---:|
| Big-5, new acts | 11 | 72 | 6.55 |
| Big-5, prior Top 10 acts | 15 | 89 | 5.93 |
| Big-5, prior Top 40 only (Reeves, Peterson) | 2 | 17 | 8.50 |
| Field, new acts | 32 | 157 | 4.91 |
| Field, prior Top 10 acts | 35 | 160 | 4.57 |

Brenda Lee, Johnny Preston, the Brothers Four, Floyd Cramer and Jeanne Black ran as long as Elvis, Percy Faith and Johnny Horton. Across all labels, records by prior hitmakers ran no longer than records by new acts (5.0 against 5.3 weeks).

**Nor the genre.** Six of the twelve 7+-week Big-5 records were Nashville crossovers (Reeves, Robbins, Horton, Cramer, Brenda Lee twice), and in the sim Country, EasyListening and Folk Top 10 records run shortest (3.0–3.5 weeks). But the Big-5's 1961 Top 10 is the same acts in the same genres (Jimmy Dean, Robbins, Cramer three times, Lee four times, Van Dyke, Cline, Elvis six times), and those records averaged 4.5 weeks. Lee went from 29 slots on four records in 1960 to 19 on four in 1961. The long runs were a 1960 condition on Big-5 records, not a property of the acts or their genres. The next subsection identifies it.

**The opening roster is thin at the Big-5, but that cannot close this.** The Big-5 receive 9.25 of the 30 hitmaker opening careers a seed (4.5 adult standard-bearers, 2.75 current hitmakers, 2.0 established hitmakers), and the 27–35 MidTiers receive 18.75. `OpeningCareerService` ranks Major and MidTier rosters together by quality. Those nine acts supply about 30 Big-5 slots, and prior-hit acts supply about 30% of Big-5 Top 10 records against history's 61% (17 of 28, by the table above). Moving hitmakers to the Big-5 would change the names on a Big-5 record count that already matches history, as `vq2` found for 1961. It could also add records beyond history's 28 at 4.2 weeks each. Either way it would not reproduce history's 1960 run length. The allocation does show in 1961's mix: Big-5 acts with a 1960 Top 10 hold 10.9 points of 1961 slots (history 16.4) and Big-5 acts without one hold 11.0 (history 4.2), though the 1961 total (21.9%) is on history.

**Reading.** The 1960 dip is a Big-5 run-length premium (+1.7 weeks over the field) that history shows only in 1960. The roster already supplies history's count of Big-5 hits. Either guard 1960 on Big-5 Top 10 record count (28) and acts (20) instead of slot share, or treat 1960's premium as a period condition and research it before modelling. The 1961 hit-act mix is a separate, smaller roster question.

**What made 1960 different.** Complete runs avoid calendar truncation. Each record that entered the Top 10 is followed through its whole run on the 1960–64 weekly Hot 100 (runs cut off at either end are dropped) and grouped by the year it entered. The same scorer prints both tables.

| Entered the Top 10 in | Records, Big-5 / field | Top 10 weeks | Top 10 weeks after the peak | Weeks at No. 1 per record | Records with 10+ weeks |
|---|---:|---:|---:|---:|---:|
| 1960 | 23 / 60 | **6.87** / 5.08 | 4.22 / 2.48 | 1.35 / 0.33 | **7** / 3 |
| 1961 | 19 / 86 | 5.26 / 4.78 | 2.47 / 2.03 | 0.53 / 0.49 | 1 / 2 |
| 1962 | 20 / 86 | 5.25 / 4.62 | 2.25 / 2.14 | 0.30 / 0.50 | 1 / 5 |
| 1963 | 30 / 81 | 4.30 / 4.86 | 1.77 / 2.19 | 0.27 / 0.56 | 0 / 1 |
| 1964 | 15 / 76 | 5.53 / 4.70 | 2.93 / 1.86 | 1.13 / 0.29 | 1 / 2 |
| `frd60pk` 1960, per seed | 21.0 / 81.0 | 4.75 / 4.92 | 2.39 / 2.45 | 0.40 / 0.53 | 1.3 / 6.0 |
| `frd60pk` 1961 (entries to week 90) | 18.5 / 64.3 | 4.50 / 4.70 | 2.23 / 2.23 | 0.64 / 0.42 | 1.0 / 4.0 |

- **A cluster of blockbusters.** The premium sits after the peak and at No. 1. Before their peak, Big-5 records spent as long in the Top 10 as the field's (1.65 against 1.60 weeks). Seven Big-5 entrants stayed 10 weeks or more: Elvis Presley's three post-Army singles, "Theme from A Summer Place" (nine weeks at No. 1), "I'm Sorry", and the instrumentals "Last Date" and "Wonderland by Night". Two more ("He'll Have to Go", "Running Bear") carried in from 1959. Dropping the six longest Big-5 entrants takes the premium from 1.79 to 0.39 weeks, near 1961–64's pooled 0.23. Drawing 23 records from the 1961–64 Big-5 records reaches a mean of 6.87 weeks in about 1 in 10,000 bootstrap draws, so the year is not sampling noise.
- **The trade record agrees.** Deacon Street's label tallies give the five majors 37 of 150 top-twenty records in 1960, no better than 1959's 24%, while their share of popularity points rose from 23% to 34%. In 1961 it fell to 20.6% of records and 21.5% of points. The tallies credit RCA's lead to Elvis's three No. 1s, Sam Cooke and Jim Reeves, and Columbia's to "El Paso" and "A Summer Place". Variety in January 1961 called RCA, with Presley, the strongest singles company in a year when most hits failed to reach 200,000 copies. RCA shipped about 1.4 million copies of "Stuck on You", Elvis's first record after two years in the Army, without waiting for orders. [Deacon Street 1960](https://www.deaconstreet.com/ElvisEra/1960/1960recordlabels.html) · [Deacon Street 1961](https://www.deaconstreet.com/ElvisEra/1961/1961recordlabels.html) · [Variety, January 1961](https://lantern.mediahist.org/catalog/variety221-1961-01_0208) · ["Stuck on You"](https://en.wikipedia.org/wiki/Stuck_on_You_(Elvis_Presley_song)) · ["Theme from A Summer Place"](https://en.wikipedia.org/wiki/Theme_from_A_Summer_Place)
- **Not payola.** The premium runs through both halves of 1960 (+1.65 and +1.96 weeks) and stops in January 1961 (+0.19). That follows the blockbusters' release dates, not the hearings (early 1960) or the law (September 1960), whose effect should have lasted. The majors' share of hit records did not rise. A small contribution cannot be ruled out.
- **A slightly slower year for everyone.** All Top 10 entrants ran 5.58 weeks in 1960 against 4.71–4.87 afterwards, and the field alone 5.08 against 4.62–4.86. Billboard reviewed fewer singles (5,797 in 1960, 6,036 in 1961, 6,700 in 1962), and Variety describes a shrinking singles market. The sim's field already runs 4.92 weeks in 1960.
- **Without the cluster, 1960 is within the sim's range.** Counting the six calendar-1960 Big-5 records with 10+ weeks as average-length records leaves a Big-5 share of 26.9%. Removing Elvis and Percy Faith outright leaves 26.7%. `frd60pk` gives 22.0% (seeds 16.5–27.9), and earlier arms gave 25–28%.
- **The sim makes blockbusters, but at the field.** It gives 7.3 records a seed with 10+ Top 10 weeks among 1960 entrants (history 10), but only 1.3 at the Big-5 against history's 7. That is history's 1961–64 pattern (Big-5 0–1 a year). Its Elvis analogue does not drive the Big-5 share: a CurrentHitmaker Superstar is seeded at a Big-5 label (RCA) in two of four seeds, and those seeds' 1960 shares are 23.7% and 19.8%, against 27.9% and 16.5% for the other two.

**Reading.** History's 1960 was the majors' blockbuster year. Elvis's return from the Army and a film theme with a nine-week No. 1 account for most of the excess, and a few more long runs make up the rest. These were one-off events, not a market condition worth a mechanism. Net of the cluster, the 1960 target is a Big-5 share of about 27%, with 28 Big-5 Top 10 records and 20 acts. The sim matches the count and the acts, and `frd60pk` sits about five points under the share, inside its own seed spread. One smaller, general gap remains. History's Big-5 records ran about 0.2 weeks longer than the field's in 1961–64 (half-years swing from −1.7 to +2.2), and the sim's run about 0.2 shorter. That is worth about two points of share and belongs with the proven-act climb premium, not with 1960.

**Adopted (2026-11-18).** The author accepted 1960 as an anomaly. The 1960 guard is now a Big-5 Top 10 share of **27%**, plus 28 Big-5 Top 10 records and 20 acts. The raw 34.2% stays as the historical fact. `score_chart_faller_depth.py` now prints 27.0 as its 1960 history column. On this target `frd60pk` sits 5 points under, inside its seed spread (16.5–27.9%).

## Reproduction

Run these from the repository root with a Python interpreter that has pandas installed. The historical tables, national charts, radio decisions and regional traces live under the local ignored `SimLogs` directory. The analysis uses existing runs. It makes no gameplay changes.

```powershell
python SimTools/analyze_big5_chart_opportunities.py
python SimTools/analyze_footprint_scope.py 1001,2002,3003,4004 'zfg-{s}-sc' 'mnfoot-{s}-sc' 'mjfoot-{s}-sc'
node SimLogs/report-follower-stock.cjs SimLogs/shelf-final.json
python SimTools/score_chart_faller_depth.py mnsh40 frd50 g12 plug4 kaa55 pca025
python SimTools/analyze_hit_followups.py mnsh40 frd50
python SimTools/score_sales_rank_guard.py srkc-1001-sc srkc-2002-sc srkf60-1001-sc srkf60-2002-sc
python SimTools/analyze_footprint_scope.py 1001,2002,3003,4004 'mnsh40-{s}-sc' 'frd60-{s}-sc' 'frd60pk-{s}-sc'
python SimTools/analyze_big5_1960_dip.py frd60pk
```

The faller screen's run requests are `SimLogs/writing-calibration/frd50-{1001,2002,3003,4004}-sc-request.json` (the `mnsh40` requests plus `--chart-faller-report-depth=50`), launched with `SimTools/run_request_arm.ps1`.
