# City placement repair — 1960s domestic talent cohort

Continue `codex/local-scene-phase-4` from `1b1b5e4`. Recruitment remains opt-in.
The primary band-member checkout and its unrelated edits are untouched.

The attached directive, atlas and pasted sketch are reference material. The user's
request authorizes city redistribution and a quick research pass, not the full
scene-dynamics, international routing or population recalibration proposals.

## Research and numerical interpretation

No source below measures the percentage of every American performing act residing
in each city during the 1960s. The numerical weights are authored simulation priors
for the game's selected domestic cohort across its 31 playable cities. They are
not chart shares, recording-session shares, modern employment shares or a census.

The quick research supports broad New York/Los Angeles/Chicago opportunity and
specialized Nashville/Detroit/Memphis opportunities:

- [Library of Congress, RCA Studio B history](https://www.loc.gov/pictures/item/tn0437/):
  RCA already operated studios in New York, Chicago and Hollywood before its
  Nashville studio; the Nashville Sound developed in the mid-1950s. This establishes
  recording infrastructure and specialization, not hometown percentages.
- [Museum of the City of New York, Folk City](https://www.mcny.org/exhibition/folk-city):
  Greenwich Village clubs and coffeehouses nurtured the 1950s/1960s folk revival.
- [Library of Congress, Motown songwriting](https://blogs.loc.gov/loc/2022/04/motowns-songwriting-stars-and-reach-out-ill-be-there/):
  Detroit's local musical networks and Motown's early-1960s songwriting activity.
- [Stax Museum history](https://staxmuseum.org/stax-museums-electronic-press-kit/):
  Satellite started in 1957 and became Stax in 1961; supports Memphis soul infrastructure.
- [1960 Census population volume](https://www2.census.gov/library/publications/decennial/1960/population-volume-1/vol-01-01-e.pdf):
  New York and Chicago lead city population, followed by Los Angeles. Municipal
  boundaries are not comparable musical catchments; these figures are a scale
  check, not inputs silently converted from the game's retail-store counts.

Additional genre directions use the companion `LocalSceneHistoricalAtlas.md`.
Small-city and exact relative weights remain provisional calibration choices.

## Implementation

Previously, a uniform seven-region draw followed by almost equal within-region
placement allocated around 5% of the national cohort to Billings and 2% to NYC.
The national opportunity draw removes that region-first multiplication error.
All playable cities have positive weights; neither city saturation nor label
vacancies cause births, deletions, quality bonuses or forced relocation.

`SceneCityPlacement` normalizes city opportunity multiplied by genre fit. Broad
relative weights: NYC 18, LA 14, Chicago 10, Detroit/Nashville 6 each,
Philadelphia/Memphis 5 each, San Francisco 4, Boston/New Orleans 3 each;
other cities range from Atlanta/Dallas/Houston 2.5 to Billings 0.1.
These are relative weights, not literal percentages. Actual national shares depend
on the preserved genre mix. Country strengthens Nashville; blues Chicago; soul/R&B
Detroit and Memphis; jazz NYC/New Orleans; folk NYC/Boston/SF; surf LA; garage
Seattle/Portland/Detroit/Minneapolis; TexMex San Antonio; Latin NYC/Miami. SF psych
receives its additional fit from formation year 1965 and Detroit/NYC proto-punk
from 1966. Existing genre chronology still decides which acts are born.

Placement uses the existing pure seed/ID hash in a version-2 namespace and stable
city order. Initial legacy, initial reserve and runtime formations share the policy.
Typed formation places, credible existing origins and solo spin-out bases still
win over the prior. Foreign/unknown legacy regions remain unresolved. Existing
artist/person geography is never rerolled, including version-1 saved identities;
this repair affects fresh worlds and later unplaced births, not automatic moves
of established save residents. Assignment provenance is versioned.

Commercial `homeRegion` retains its calibrated meaning and RNG draw. It is separate
from working-base geography. No changes to nation-wide counts, genre selection,
quality, contracts, formation budgets or population/band-life random streams.
Recruitment-on economic behavior may change through the intended city-access path.

## Validation

`RunSceneCityPlacementChecks.ps1` runs serial checks at seeds 1001 and 2002,
including actual 3,000-act launch census, synthetic initial/reserve/runtime draws,
order independence, preserved existing geography, genre/era fits, recruitment,
26-week world plus real gzip save/load and recruitment-off room checks.
`SimLogs/city-placement-{seed}.csv` separates launch and reserve denominators.

Build passes with six existing warnings. Results and economic comparison are
recorded below after the completed audit.

Completed short suite (`SimLogs/scene4-city-placement-v1/runs.json`):
27 placement checks per seed, 49 recruitment checks per seed, both 26-week world/
gzip IO roundtrips, and 74 room checks pass. The existing non-Node temporary
autoload error also appears in the prior baseline logs; all required completion
markers and exit codes pass.

| Opening city | Seed 1001 acts / share | Seed 2002 acts / share |
|---|---:|---:|
| New York | 493 / 16.43% | 481 / 16.03% |
| Los Angeles | 345 / 11.50% | 381 / 12.70% |
| Chicago | 337 / 11.23% | 318 / 10.60% |
| Detroit | 223 / 7.43% | 200 / 6.67% |
| Nashville | 170 / 5.67% | 202 / 6.73% |
| Memphis | 192 / 6.40% | 171 / 5.70% |
| Philadelphia | 170 / 5.67% | 151 / 5.03% |
| San Francisco | 117 / 3.90% | 118 / 3.93% |
| Billings | 1 / 0.03% | 0 / 0.00% |

These are the 3,000 launch acts, not all musicians or the full registry. Billings
also receives 7 / 3 of the separate 4,000-act unsigned reserve. Positive weights
do not guarantee a quota in every smaller cohort. Existing scene rooms retain
their finite recurring cast even where the broader resident population is large.

`RunSceneCityPlacementAudit.ps1` deliberately uses a 52-week recruitment-off
control plus one-week recruitment runs for the two opening censuses. This is not
one-year recruitment economic validation. `analyze_scene_city_placement.py` compares
the 84 non-scene economic CSVs to the previous initialization repair, compares
opening allocations and checks duplicate ownership/round turns and exhausted
accessible supply. It keeps long-horizon recruitment acceptance false.

The completed bounded audit (`SimLogs/scene4-city-placement-audit-v1/analysis.json`)
passes: all 84 recruitment-off economic CSVs match the prior initialization repair
exactly over 52 weeks. Launch signings increase 2,487 -> 3,000 on seed 1001 and
2,301 -> 2,817 on seed 2002. Empty opening labels increase 24 -> 30 / 26:
Billings accounts for 6 / 2 empties, and all other empty cases are unresolved HQs,
London, Indianapolis or Milwaukee. Sparse city supply now exposes local label
placement/routing pressure instead of concealing it with a uniform region lottery.

On seed 1001 two domestic majors are under target: NYC `label_0004` has 39/42
and Hollywood `label_0248` has 39/44. Both have zero accessible remaining launch
supply; all 3,000 launch acts are contracted. Seed 2002 has no underfilled domestic
major. The preserved one-pick-per-label rounds, targets and birth budgets cannot
guarantee every desired signing with finite supply. No inaccessible launch supply
bailout, reserve activation or extra births were introduced to mask this limitation.
This is a completed placement repair, not recruitment economic acceptance.

Reproduce after `dotnet build --no-restore`:

```powershell
./SimTools/RunSceneCityPlacementChecks.ps1 -RunTag scene4-city-placement-v2
./SimTools/RunSceneCityPlacementAudit.ps1 -RunTag scene4-city-placement-audit-v2
python ./SimTools/analyze_scene_city_placement.py ./SimLogs/scene4-city-placement-audit-v2/runs.json
```

Use the bundled Python executable if `python` resolves to a Windows Store stub.
