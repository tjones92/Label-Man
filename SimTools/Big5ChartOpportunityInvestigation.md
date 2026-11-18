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

## Reproduction

Run these from the repository root with a Python interpreter that has pandas installed. The historical tables, national charts, radio decisions and regional traces live under the local ignored `SimLogs` directory. The analysis uses existing runs. It makes no gameplay changes.

```powershell
python SimTools/analyze_big5_chart_opportunities.py
python SimTools/analyze_footprint_scope.py 1001,2002,3003,4004 'zfg-{s}-sc' 'mnfoot-{s}-sc' 'mjfoot-{s}-sc'
node SimLogs/report-follower-stock.cjs SimLogs/shelf-final.json
```
