# National standing for Star and above — 2026-11-07

**Decision:** Star and Superstar should require national standing. Regional
evidence can establish Rising/Established standing, but should not independently
grant or retain the higher celebrity tiers.

The offline candidate implements that distinction by allowing only the existing
national record/conversion routes to support Star/Superstar. Regional evidence
remains eligible for Rising/Established. This is a tested candidate interpretation
of national standing, not yet a gameplay change or a claim that these routes
prove sales across named territories.

## What the replay shows

Reuse the same four completed 104-week geography worlds and the same 20
RegionalName acts per seed. Replay three policies for each of the four geography
rules: full policy, complete regional-route ablation, and national routes for
Star and above. All twelve trajectories per act evolve independent hysteresis
from July 1, 1960. Observed national scores, sales/TV, peer references and the
geography rule's reach inputs stay fixed.

Each row below covers 1,020 selected act-week observations in 1961. Counts are
Star-or-higher observations, not distinct acts.

| World | Full: observed geography | Full: home only | Full: home + count | Full: crossovers national | National-high-tier candidate, each geography rule |
|---|---:|---:|---:|---:|---:|
| 1001 control | 1 | 218 | 41 | 1 | 0 |
| 1001 treatment | 21 | 256 | 60 | 21 | 12 |
| 2002 control | 8 | 255 | 110 | 8 | 8 |
| 2002 treatment | 86 | 332 | 188 | 86 | 83 |

The candidate gives the same high-tier counts across all geography rules in
these worlds: **103** observations, versus **1,061** for full-policy home-only
allocation. On observed geography it lowers 13 high-tier observations (116 to
103). The seed-2002 treatment candidate includes 45 Star and 38 Superstar
observations; the other retained high tiers are Star.

Completely ablating the regional route eliminates every geography-driven
higher-band and higher-launch-proposal count in 1961. That establishes route
dependence of the measured increases under this diagnostic. It removes genuine
regional sales response as well as follower draw, so it does not identify which
of those inputs deserves a different calibration.

## Launch proposals still respond below Star

Counts below compare each geography rule with **observed geography under the
same policy**. Entries show full policy → national-high-tier candidate. They are
higher requested single-launch proposals, before caps or payment.

| World | Available reads | Home only | Home + count | Crossovers national |
|---|---:|---:|---:|---:|
| 1001 control | 13 | 13 → 11 | 11 → 11 | 2 → 2 |
| 1001 treatment | 25 | 19 → 18 | 13 → 13 | 5 → 5 |
| 2002 control | 34 | 20 → 17 | 18 → 15 | 9 → 9 |
| 2002 treatment | 40 | 22 → 22 | 21 → 21 | 8 → 8 |

The candidate lowers 3/6/9/6 home-only launch proposals relative to the full
home-only policy in these worlds. Those are different comparisons from the
table: a smaller proposal can still exceed the candidate's neutral proposal.
Regional support can still make an act Established, which is intentional under
the declared tier distinction. This is not evidence that the remaining
Established support or its budget multiplier is correctly calibrated.

These effects are diagnostic requested envelopes in completed worlds. Control
proposals are unpaid, and this replay changes no paid exposure, subsequent
following, releases or charts. Neither the 1961 counts nor the selected cohort
estimate a decade-wide economic effect.

## What counts as national evidence

The retained routes use nationally peer-normalized sales/TV/following magnitude
and population-weighted reach. They do not require a stored list of reached
territories. Their following magnitude can qualify without current sales/TV
magnitude; this check keeps observed reach and the candidate's current
hysteresis threshold, so it is not a replay with all historical sales removed.

In 1961 that following-magnitude-only check supports 11 candidate high-tier
observations, all Jim Pierce (`artist_00672`) in seed-2002 treatment, October 13
through December 22. There are none in the other three worlds. This does not
mean seeded following caused those observations: actual following mixes opening
survivors and later joins. For context, the existing source export reports about
15,039 original survivors out of 463,795 followers on October 13, and 14,346 out
of 593,455 on December 22. Its full-policy zero-opening-source shadow remains
Superstar on these dates; it is a different policy/history comparison.

At unit regional magnitude the current Star support is `0.5 + 0.5 × reach`.
Entry needs only 3.1808% reach; exit needs none because 0.5 exceeds the 0.412723
exit boundary. For Superstar the corresponding minimum reach is 68.9130% on
entry and 48.4637% on exit. The national-high-tier candidate blocks independent
regional qualification at both entry and exit rather than adjusting these
weights to approximate a national requirement.

## Validation and next step

All 24,960 full-policy geography observations across the four worlds reproduce
the saved replay's bands/statuses. Neutral full-policy observations and all
available selected launch proposals also reconstruct C#. The report retains
74,880 weekly policy observations with per-band support bounds and thresholds.

The consumed CSVs match their original manifests, requests/manifests match the
geography replay hashes, paired opening/release censuses match, and all four
worlds have exact completion markers, empty error logs and matching assembly,
engine, calibration and source provenance. Calibration remains
`80546B52E1CE0902FF44692FF642B45FCC6DA5A63D48663B9CDB295DB6450F19`.
Ten new mechanism fixtures and thirteen existing geography replay fixtures pass.
No C# gameplay source or calibration was changed, and no new simulation was run.

## Candidate implementation

The C# and Python policy kernels now accept `EligibleRoutes` per band in
regional, record and conversion order. Excluded routes contribute zero support
at both entry and exit. Older calibrations use all three routes when the field
is omitted. The opt-in candidate keeps the existing thresholds and restricts
Star and Superstar to the national record and conversion routes.
[Candidate calibration](StatureShadowNationalHighTiers.json).

Both kernels reproduced all 74,880 evaluations across 960 histories from the
retained replay, including band, status, hysteresis threshold and support
bounds. The build passed with zero warnings or errors. The 42 focused Python
tests passed. Headless C# checks passed with empty error logs, including 14
game-reader checks that exercise startup and restoration of the embedded
candidate calibration. [Python results](../SimLogs/writing-calibration/stature-national-policy-v1-python.json),
[C# replay log](../SimLogs/writing-calibration/stature-national-policy-v2-csharp.log),
[game-reader log](../SimLogs/writing-calibration/stature-national-policy-v1-reader.log).

The verifier uses the recorded geography and sensitivity reports as references
and records current code and calibration hashes separately. It emits cases for
the headless scene `SimTools/SaveLoadRoundTripRunner.tscn` with
`--stature-shadow-policy-check --stature-policy-replay=<cases.jsonl>`.
[Verifier](verify_national_stature_policy.py).

## Full roster replay

The candidate replay uses the observed geography and audience evidence for all
logged AI acts in the four retained worlds. It evolves separate histories for
both policies. The replay reconstructs C# status, band, prior band and support
for 3,035,962 observations from July 1, 1960 through December 22, 1961. The
launch comparison uses reads with an available original standing proposal.

| 1961 world | Star or above, original → candidate | Acts falling below Star | Lower launch proposals / available reads |
|---|---:|---:|---:|
| 1001 control | 3,364 → 3,067 | 61 | 25 / 2,120 |
| 1001 treatment | 3,373 → 3,075 | 61 | 21 / 2,152 |
| 2002 control | 3,368 → 3,146 | 57 | 11 / 2,191 |
| 2002 treatment | 3,133 → 2,828 | 50 | 20 / 2,186 |

All 1,122 observations that fall below Star move from Star to Established.
Of those, 991 are outside the opening following census and 13 are from the
RegionalName cohort. The result extends beyond the opening geography sample.
All 77 changed launch proposals are reductions, representing 0.89% of
available 1961 launch reads. These are proposals under fixed observations.
[Full results by year and cohort](../SimLogs/writing-calibration/stature-national-roster-v1.json),
[replay script](analyze_national_stature_roster.py).

## Short marketing comparison

The 39-week trials use seeds 1001 and 2002 with the full marketing reader
enabled in both arms. The two calibrations differ in route eligibility and
version identifiers. The runs cover 13 published weeks after the policy takes
effect on July 1, 1960. Each candidate arm logs 108,594 observations over that
period. Every observed candidate Star or Superstar clears its threshold through
the record or conversion route.

| Seed | Star observations, original → candidate | Superstar observations, original → candidate | Lower aligned launch requests | Launch marketing cost in each arm | Follow-up spend in each arm |
|---|---:|---:|---:|---:|---:|
| 1001 | 516 → 457 | 231 → 231 | 5 of 2,574 | $687,384.13 | $57,410.34 |
| 2002 | 623 → 567 | 208 → 207 | 1 of 2,533 | $629,779.05 | $39,758.78 |

In both seeds the first different requested budget appears in a July 8
programme reserve. The candidate moves those acts from Star to Established and
cuts the requests from $95.12 to $71.34 in seed 1001 and from $528.09 to $396.07
in seed 2002. The first differences follow 45,494 and 43,297 matching reads.

All six lower launch requests join to the same issued single and paid marketing
cost in their paired worlds. Each paid amount matches its programme reservation
within float precision. For example, the request for `gen_4223` in seed 1001
falls from $4,412.21 to $3,309.16, with $336.92 paid in both arms. Single and
album issue counts match within each seed. Album units rise by 902 and 581 in
the candidate arms, so the small simulation differences extend beyond the
standing and budget traces. [Seed 1001 summary](../SimLogs/writing-calibration/stature-national-calibration-39w-v1-1001-summary.json),
[seed 2002 summary](../SimLogs/writing-calibration/stature-national-calibration-39w-v1-2002-summary.json).

### Equal payments can change campaign push

In each short pair, a standalone LP near the start of the comparison had a
lower `album-launch` request and the same reservation and paid marketing. The
same lower request also became the denominator of its booked campaign share.
`BookProgrammeCampaign` divides paid spend by the current envelope and clamps
the result to [0,1]. The share scales the label's contribution to initial
awareness and the ongoing campaign push in the album simulator.
[Campaign booking](../Systems/CompetitorManager.ReleaseProgramme.cs),
[launch promotion](../Systems/CompetitorManager.cs),
[album response](../Systems/AlbumSimulator.cs).

| Seed and LP | Date | Requested envelope, original → candidate | Paid in both | Implied booked share | Cumulative raw LP demand through week 39 |
|---|---|---:|---:|---:|---:|
| 1001 `gen_3108` | July 8, 1960 | $354.83 → $266.12 | $123.86 | 34.91% → 46.54% | 14,874 → 15,381 |
| 2002 `gen_3242` | July 22, 1960 | $528.09 → $396.07 | $198.53 | 37.59% → 50.13% | 7,412 → 7,727 |

The booked shares are reconstructed from the same-day
`programme-campaign-share` reads and logged payments. Raw LP demand is the
pre-cannibalization project total. These aligned LPs show a channel for demand
to move with spending held equal. The difference in total album units is a
paired-world result that also includes downstream effects.

Across the short comparison, implied share changes at equal payment on 5 of
2,536 comparable singles and 3 of 1,017 comparable LPs in seed 1001, and 1 of
2,494 singles and 5 of 1,058 LPs in seed 2002. The join uses issue week, format,
slot, label and act across arms. Within each arm, the issue must have a same-week
payment and a unique campaign-share read for that act and label on the issue
date. [Analysis script](analyze_national_stature_marketing.py),
[short-run payment analysis](../SimLogs/writing-calibration/stature-national-calibration-39w-v1-payments-v3.json).

All four runs reached their exact completion markers with empty error logs.
Each archive contains 147 verified CSVs. The summary checks calibration hashes,
paired settings, manifests and reader envelopes. Seven summary fixtures pass.
To reproduce with a fresh prefix, run from the repository root.

```powershell
./SimTools/run_stature_marketing_trial.ps1 -RunPrefix stature-national-calibration-39w-v2 -Weeks 39 -Seeds @(1001,2002) -CausalOnly -CompressOutputs -Calibration SimTools/StatureShadowNationalHighTiers.json -ControlCalibration SimTools/StatureShadowExperimental.json
```

Protocol: [regional sensitivity](StatureRegionalSensitivityProtocol.md).
Analysis: [script](analyze_regional_standing_sensitivity.py),
[fixtures](test_analyze_regional_standing_sensitivity.py),
[machine-readable results](../SimLogs/writing-calibration/stature-regional-sensitivity-v1.json).
Previous findings: [geography replay](StatureOpeningGeographyReplayReview.md).

Reproduce from the repository root with the existing audit Python interpreter:

```powershell
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/analyze_regional_standing_sensitivity.py --replay SimLogs/writing-calibration/stature-geography-2y-v1-analysis.json --prefix stature-geography-2y-v1 --output SimLogs/writing-calibration/stature-regional-sensitivity-v2.json
```

Output files must be new; existing reports are protected from overwrite.

## Two-year marketing comparison

Both seeds ran for 104 weeks with the full marketing reader in both arms.
The run requests match the 39-week comparison on assembly, engine, C# sources,
calibrations, GC settings and resolved flags apart from run name and horizon.
All four runs reached their exact completion markers with empty error logs
and 147 verified CSVs per run. Every candidate Star and Superstar observation
clears its threshold through record or conversion support.

| Seed / year | Star observations | Superstar observations | Single / LP slate issues | Paid launch marketing | Paid follow-up | LP units |
|---|---:|---:|---:|---:|---:|---:|
| 1001 / 1960 | 1,019 → 916 | 553 → 548 | 3,451 / 1,658 → 3,470 / 1,656 | $973,444 → $970,930 | $64,506 → $67,743 | 39,572,492 → 39,454,688 |
| 1001 / 1961 | 2,075 → 1,650 | 1,298 → 1,349 | 3,344 / 2,272 → 3,327 / 2,296 | $1,128,756 → $1,118,603 | $21,299 → $12,931 | 70,109,886 → 69,803,078 |
| 2002 / 1960 | 1,259 → 1,146 | 451 → 444 | 3,432 / 1,717 → 3,429 / 1,728 | $880,983 → $879,586 | $47,800 → $47,837 | 38,532,559 → 38,554,025 |
| 2002 / 1961 | 1,843 → 1,942 | 1,290 → 1,133 | 3,351 / 2,328 → 3,336 / 2,250 | $957,931 → $944,979 | $20,874 → $9,516 | 65,424,185 → 64,792,549 |

Values are original → candidate. Standing counts are act-weeks. Spending and
slate issues use the calendar year of the audit week's start. Market units
use the engine's annual rollup. Single units match within each paired year:
122,580,979 and 121,667,949 in 1960 for seeds 1001 and 2002 respectively, and
127,973,716 in 1961 for both seeds. By 1961 the candidate reduces total
Star-or-higher observations in both seeds, with a different Star/Superstar
mix in each. Paid launch marketing and LP units are lower in both 1961 pairs.

The first requested differences reproduce the July 8 reserves from the short
comparison. Weekly paid launch marketing first differs on October 7 in seed
1001 ($27,471.82 → $27,472.48) and October 21 in seed 2002
($23,959.86 → $23,969.82). On each of those dates the first changed payment
on a matched slot is for an LP whose campaign envelope matches across arms.
The first paid differences therefore appear after the change in campaign
push documented above.

| 1961 matched issues | Seed 1001 | Seed 2002 |
|---|---:|---:|
| Matched single slots | 854 | 960 |
| Lower / higher single requests | 46 / 43 | 26 / 61 |
| Single slots with both request and payment changes | 38 | 37 |
| Single shares changed at equal payment / comparable shares | 49 / 838 | 48 / 932 |
| LP shares changed at equal payment / comparable shares | 51 / 791 | 56 / 850 |

On January 13 in seed 2002, one matched single slot for `artist_00183` moves
from a $56.27 request and $45.86 reservation and payment to a $37.51 request,
reservation and payment. Its implied campaign share rises from 81.51% to
100%. The worlds issue different masters on this slot. By 1961 only about a
quarter of single issues match on week, slot, label and act. The matched-slot
counts describe that subset, and the totals include changes to the slate and
later audience response. The results cover the two-year comparison with the
calibration still opt-in.

[Seed 1001 summary](../SimLogs/writing-calibration/stature-national-calibration-104w-v2-1001-summary.json),
[seed 2002 summary](../SimLogs/writing-calibration/stature-national-calibration-104w-v2-2002-summary.json),
[matched payments and campaign shares](../SimLogs/writing-calibration/stature-national-calibration-104w-v2-payments-v2.json).

```powershell
./SimTools/run_stature_marketing_trial.ps1 -RunPrefix stature-national-calibration-104w-v3 -Weeks 104 -Seeds @(1001,2002) -CausalOnly -CompressOutputs -Calibration SimTools/StatureShadowNationalHighTiers.json -ControlCalibration SimTools/StatureShadowExperimental.json
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/analyze_national_stature_marketing.py --prefix stature-national-calibration-104w-v3 --output SimLogs/writing-calibration/stature-national-calibration-104w-v3-payments.json
```

## Campaign-share isolation

Two same-build 39-week calibration pairs per seed apply the full marketing
reader. One pair uses the standing envelope in campaign booking. The other
pair uses the existing career budget for that denominator through
`--stature-marketing-legacy-campaign-share`. Forecasts, funding requests and
paid-spend decisions still use the configured stature reader. The opt-in
persists with the experimental reader settings.

Using the career budget for campaign share eliminates the measured difference
in album units between calibrations in both seeds. Weekly market-clearing
exports are byte-identical within each career-denominator pair. Single units,
slate issues, launch payments and follow-up spend also match within each of
those pairs. Five matched singles in seed 1001 and two in seed 2002 still
request lower budgets under the candidate. Those slots have equal
reservations and payments in both arms.

| Seed | Album units with standing denominator, original → candidate | Album units with career denominator, original → candidate |
|---|---:|---:|
| 1001 | 21,957,394 → 21,958,296 (+902) | 21,972,744 → 21,972,744 |
| 2002 | 22,043,347 → 22,043,928 (+581) | 22,057,541 → 22,057,541 |

The two early LPs keep the same launch requests and paid amounts as before.
Within each career-denominator pair the share and raw LP demand now match.

| LP | Paid in all four runs | Standing share, original → candidate | Career share, both calibrations | Raw LP demand with career share, both calibrations |
|---|---:|---:|---:|---:|
| `gen_3108`, seed 1001 | $123.86 | 34.91% → 46.54% | 58.18% | 15,882 |
| `gen_3242`, seed 2002 | $198.53 | 37.59% → 50.13% | 62.66% | 8,037 |

The career budget raises the share for these LPs under both calibrations.
The change also raises the original calibration's market-wide LP units by
15,350 and 14,194 across the two seeds. Its absolute effect on the economy is
separate from its effect on the gap between calibrations.

Within each career-denominator pair, 136 of 147 exports match byte for byte.
The remaining files include standing traces, forecasts and release strategy
diagnostics. In the launch campaign ledger, the only differing field is the
expected net at release, on eight rows in seed 1001 and five in seed 2002.
This establishes the campaign-share effect through week 39. The two-year
results above used the standing denominator throughout.

All eight runs reached their exact completion markers with empty error logs
and 147 verified CSVs per run. All 588 standing-denominator exports match the
retained 39-week runs byte for byte. The build passed with zero warnings or
errors, the Godot marketing fixture passed 39 checks including campaign
booking and save/load, and 21 focused Python tests passed.
[Comparison results](../SimLogs/writing-calibration/stature-campaign-share-39w-v1-comparison.json),
[analysis script](compare_stature_campaign_share.py).

```powershell
$prefix = 'stature-campaign-share-39w-v2'
$run = @{
    Weeks = 39
    Seeds = @(1001,2002)
    CausalOnly = $true
    CompressOutputs = $true
    Calibration = 'SimTools/StatureShadowNationalHighTiers.json'
    ControlCalibration = 'SimTools/StatureShadowExperimental.json'
}
./SimTools/run_stature_marketing_trial.ps1 @run -RunPrefix "$prefix-standing"
./SimTools/run_stature_marketing_trial.ps1 @run -RunPrefix "$prefix-legacy" -LegacyCampaignShare
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/compare_stature_campaign_share.py --prefix $prefix --output "SimLogs/writing-calibration/$prefix-comparison.json"
```

## Two-year campaign-share isolation

The 104-week comparison extends both 39-week denominator pairs using the
same build, calibrations, marketing reader and reference profile. The
career-denominator pairs delay the first differences in market clearing
relative to the retained standing-denominator pairs. Both seeds still
diverge within the two-year window.

### First decisions and market differences

| Seed and denominator | First market difference | First weekly launch spend difference | First slate issue difference |
|---|---:|---:|---:|
| 1001 standing | Week 28 | Week 41 | Week 42 |
| 1001 career | Week 43 | Week 43 | Week 43 |
| 2002 standing | Week 30 | Week 43 | Week 43 |
| 2002 career | Week 54 | Week 55 | Week 56 |

In the career-denominator pair for seed 1001, Capitol reserves $329.91 in
marketing for Biff Allen's prepared single (`artist_00493`, master
`gen_4952`) in both arms. The October 21 decision is the first aligned
release-profit gate to change its result. The original calibration reads
Star and forecasts $5,102.89 in campaign cost. The candidate reads
Established and forecasts $3,827.16. The all-in profit estimate crosses
from −$801.51 to +$215.01, so the original arm postpones the single and the
candidate issues it for $329.91 in paid marketing. The amount reserved for
the campaign and the cost used in the forecast describe different values.
This trace identifies a release decision that changes even when campaign
share uses the career budget.

For seed 2002, the first career-denominator difference in weekly market
clearing appears in week 54, before launch payments or issued slots differ.
Bo Pugh (`artist_00144`) receives two November 18 media envelope quotes of
$7,220.65 under the original calibration and $5,776.52 under the candidate.
He has the largest TV score difference in the December 23 stature snapshot.
The 1960 media ledger records two fewer local acceptances and $237 less
reserved TV budget in the candidate, and the week-53 demand trace records
different regional transfers. The first aligned profit gate change comes
in week 55.

### Seed 2002 title-level media trace

The first logged media-demand difference in the career-denominator pair
occurs in week 53. All 3,096 logged title-region keys match across arms, with
the same pre-transfer intents, physical limits and frenzy flags. The only
changed requests are for two singles in the Southwest. The pair enables
direct television requests, and both singles were neutral donors with zero
Southwest requests in week 52. The new requests therefore reflect fresh
airing contributions during the next demand interval.

| Single | TV demand request, original → candidate | Signed transfer | Serviceable intent after transfer | Southwest units cleared |
|---|---:|---:|---:|---:|
| Bo Pugh, “Dancing of Sadness” (`gen_5036`) | 0.00021 → 0 | +2 → −83 | 10,674 → 10,589 | 1,286 → 1,276 |
| Patricia Davis, “The Ballad of Henry Shaw” (`gen_5808`) | 0.00019 → 0.00006 | +7 → +2 | 39,872 → 39,867 | 4,803 → 4,803 |

Pugh's single switches from recipient to neutral donor. Across the
Southwest, 49 logged titles change their grants and total transfers fall
from 6,676 to 6,669 units. The transfers sum to zero in each arm, and the
week 53 regional market totals match. Eight Southwest titles change
cleared units in the sampled demand-stage export. Pugh's national weekly
sales fall from 22,763 to 22,753 and his chart position stays at 31.
The first regional clearing difference appears in the Deep South in
week 54, where raw single demand increases by five units.

The November 18 media-envelope reads identify Pugh and his label. The
week 53 TV requests identify the singles and region. The retained exports
carry no shared booking or airing ID between those two observations.
Patricia Davis's logged media envelopes through this week match across
arms. The instrumented replay below joins the bookings and airings.

The trace verifies the six consumed CSVs per arm against their manifests
and checks the requests, manifests and logs against the retained trial
summary. [Machine-readable trace](../SimLogs/writing-calibration/stature-campaign-share-104w-v1-legacy-2002-media-trace.json),
[trace script](trace_stature_media_divergence.py).

```powershell
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/trace_stature_media_divergence.py --prefix stature-campaign-share-104w-v1-legacy --seed 2002 --output SimLogs/writing-calibration/stature-campaign-share-104w-v1-legacy-2002-media-trace-v2.json
```

### Seed 2002 booking and airing replay

An audit-only 55-week replay records the TV allocation for each record,
the remaining balance before each AI booking decision, and the regional
demand contribution of each airing. Both arms reproduce the archived
market clearing, media demand, demand stages and record rows through
week 54. They also reproduce all stature marketing reads through the
December 23 tally, 94,878 per arm. Both finish with empty error logs
and 150 verified CSVs each.

Pugh's November 18 quote created the allocation for “Dancing of Sadness”
when *American Bandstand* booked the November 24 episode. Its TV line
fell from $1,299.71 to $1,039.77. The December 7 Buddy Deane booking
still went ahead in the candidate, with minimal preparation at $290.52
instead of extra preparation at $330.52. That left $17.06 for later
dates in the candidate and $237.00 in the original.

| Pugh appearance | Original balance before decision and reservation | Candidate balance and minimum cost | Original Southwest demand contribution |
|---|---:|---:|---:|
| Albuquerque, December 17 | $237.00 and $134.84 | $17.06 and $94.84, declined | 0.00006284882 |
| Phoenix, December 20 | $102.16 and $98.60 | $17.06 and $58.60, declined | 0.00015158289 |

The two original airings reconstruct the week 53 request of 0.00021
at the demand export's precision. The candidate has zero Southwest
contributions for Pugh's single in that tally.

Davis began with the same $953.55 TV allocation in both arms. In the
candidate, Albuquerque accepted her after declining Pugh and reserved
$134.84. Her Cincinnati appearance reserved $280.11 in both arms.
By the December 22 Phoenix booking the candidate had $43.10 left,
below the $58.60 minimum, while the original reserved $98.60 from its
$177.94 balance. The original Phoenix airing contributed 0.00018656357
to Southwest demand for her single. The candidate Albuquerque airing
contributed 0.000056919875. Those airings reproduce her two logged
week 53 requests. The same-sized TV allocation led to a different
schedule and a different amount left for Phoenix.

The trace matches the two Southwest requests to specific booking and
airing records in this pair. It follows the launch of the divergence
through week 54, where the regional market totals first differ.
[Booking replay results](../SimLogs/writing-calibration/stature-campaign-share-55w-bookings-v2-2002-analysis.json),
[analysis script](analyze_stature_media_bookings.py).

```powershell
$prefix = 'stature-campaign-share-55w-bookings-v3'
./SimTools/run_stature_marketing_trial.ps1 -RunPrefix $prefix -Weeks 55 -Seeds @(2002) -CausalOnly -CompressOutputs -Calibration SimTools/StatureShadowNationalHighTiers.json -ControlCalibration SimTools/StatureShadowExperimental.json -LegacyCampaignShare -ObserveMediaDemandBookings
$logs = 'SimLogs/writing-calibration'
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/analyze_stature_media_bookings.py --reference "$logs/stature-campaign-share-104w-v1-legacy-2002-media-trace.json" --replay-prefix $prefix --output "$logs/$prefix-2002-analysis.json"
```

### Seed 2002 single-allocation restoration

Two same-build candidate runs extend the comparison to 60 weeks. The
intervention moves $259.94 from reserve to TV when the November 18 quote
creates the allocation for “Dancing of Sadness.” The candidate's TV line
rises from $1,039.77 to the original $1,299.71, leaving $317.72 in reserve.
The quoted campaign envelope and its other allocations are held fixed.
The audit requires the exact date, episode, record, label, artist and
starting TV balance. It fails the run if the override fails to match.

The intervention restores both of Pugh's original Southwest airings and
Davis's original Phoenix airing. All three December 23 deposits match the
original booking replay field for field. That removes the week 53 media
demand difference and the subsequent week 54 regional market difference.

| First difference from the original calibration | Unmodified candidate | Candidate with TV allocation restored |
|---|---:|---:|
| Media-demand export | Week 53 | Week 57 |
| Regional market clearing | Week 54 | Week 58 |
| Weekly paid launch marketing | Week 55 | Matches through week 60 |
| Release-profit gate decision | Week 55 | Matches through week 60 |
| Issued slate slots | Week 56 | Matches through week 60 |

With the allocation restored, Zion Gospel's week 55 standalone LP gate
again commits at an estimated profit of $9,883.77. The unmodified candidate
declines it at −$1,028.54. The restored run's first changed clearing row
has one extra unit of raw single demand in the Deep South in week 58.
This isolates the early seed 2002 chain. The 60-week experiment cannot
estimate what the change does to later-decade chart cohorts.

Both runs reached their exact completion markers with empty error logs
and 150 verified CSVs per run. The unmodified candidate reproduces all
archived rows through week 60 in seven checked exports, including the
release gate and slate ledgers. [Restoration results](../SimLogs/writing-calibration/stature-campaign-share-60w-tv-restore-v1-2002-analysis.json),
[analysis script](analyze_stature_tv_restore.py).

```powershell
$trial = @{
    Weeks = 60
    Seeds = @(2002)
    CausalOnly = $true
    CompressOutputs = $true
    Calibration = 'SimTools/StatureShadowNationalHighTiers.json'
    ControlCalibration = 'SimTools/StatureShadowExperimental.json'
    LegacyCampaignShare = $true
    ObserveMediaDemandBookings = $true
    CalibrationArm = 'treatment'
}
$baseline = 'stature-campaign-share-60w-tv-baseline-v2'
$restore = 'stature-campaign-share-60w-tv-restore-v2'
./SimTools/run_stature_marketing_trial.ps1 @trial -RunPrefix $baseline
./SimTools/run_stature_marketing_trial.ps1 @trial -RunPrefix $restore -TvAllocationOverride '1960-11-18,american_bandstand|1960-11-24,gen_5036,label_0005,artist_00144,103977,129971'
$logs = 'SimLogs/writing-calibration'
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/analyze_stature_tv_restore.py --booking-reference "$logs/stature-campaign-share-55w-bookings-v2-2002-analysis.json" --baseline "$baseline-2002-treatment" --restored "$restore-2002-treatment" --output "$logs/$restore-2002-analysis.json"
```

### 1961 outcomes

Values are candidate minus original within each seed and denominator rule.

| 1961 measure | Seed 1001 standing | Seed 1001 career | Seed 2002 standing | Seed 2002 career |
|---|---:|---:|---:|---:|
| Star-or-higher act-weeks | −374 | −330 | −58 | −182 |
| Single slate issues | −17 | −24 | −15 | +6 |
| LP slate issues | +24 | −23 | −78 | −4 |
| Paid launch marketing | −$10,154 | +$1,768 | −$12,952 | +$12,246 |
| Paid follow-up marketing | −$8,368 | +$74 | −$11,358 | −$3,494 |
| LP units | −306,808 | +332,325 | −631,636 | −331,504 |

All four 1961 pairs clear 127,973,716 single units in each arm. Both
career-denominator pairs have higher candidate launch spend, reversing the
direction under the standing denominator. Their LP volume changes have
opposite signs across seeds. These annual differences include shifts in the
slates and subsequent audience response. The first-decision traces above
describe specific mechanisms near the start of divergence.

In the 1961 career-denominator runs, 34 of 886 matched single slots in
seed 1001 and 64 of 1,786 in seed 2002 have changes in both request and
payment. Another comparison finds different campaign shares at equal
payment in 24 of 867 and 19 of 1,741 comparable single slots. The
denominator in this arm still reads each world's current career budget.
As the worlds separate, the same slot can have different act or label
inputs to that budget.

All eight runs reached their exact 104-week completion markers with empty
error logs and 147 verified exports per run. All 588 standing-denominator
exports reproduce the retained two-year runs byte for byte. The extended
analyzer also reproduces the earlier 39-week comparison. Twenty-one focused
Python tests pass. The results use the existing opt-in reader and cover two
seeds through the end of 1961.
[Two-year comparison](../SimLogs/writing-calibration/stature-campaign-share-104w-v1-comparison.json),
[career-denominator payment joins](../SimLogs/writing-calibration/stature-campaign-share-104w-v1-legacy-payments.json),
[analysis script](compare_stature_campaign_share.py).

```powershell
$prefix = 'stature-campaign-share-104w-v2'
$run = @{
    Weeks = 104
    Seeds = @(1001,2002)
    CausalOnly = $true
    CompressOutputs = $true
    Calibration = 'SimTools/StatureShadowNationalHighTiers.json'
    ControlCalibration = 'SimTools/StatureShadowExperimental.json'
}
./SimTools/run_stature_marketing_trial.ps1 @run -RunPrefix "$prefix-standing"
./SimTools/run_stature_marketing_trial.ps1 @run -RunPrefix "$prefix-legacy" -LegacyCampaignShare
& 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe' SimTools/compare_stature_campaign_share.py --prefix $prefix --weeks 104 --output "SimLogs/writing-calibration/$prefix-comparison.json"
```
