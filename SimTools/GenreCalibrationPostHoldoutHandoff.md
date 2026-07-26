# Genre calibration post-holdout handoff

Status: **READY FOR TARGETED GENRE CALIBRATION / FORMAT INTEGRATION REOPENED / NO SIMULATION AUTHORIZED**

## Decision

Return to genre calibration now.

The seed-2007 holdout is useful precisely because it isolates the remaining
genre problem:

- economy, reconciliation, release throughput, and the Single-lane analyzer
  passed;
- label lifecycle passed with only a participation watch;
- the accepted runtime source and source-state hash did not change;
- the same Psychedelic Rock commercial timing defect appears in seeds 1001,
  1002, and 2007.

This is no longer a reason to keep adjusting the economy or label population.
It is also not evidence for lowering the Psychedelic Rock 1967 demand
keyframe. The cross-seed evidence instead reopens the realized genre-format
allocation and its interaction with supply retention, per-artist Album
capacity, catalog accumulation, and momentum.

This handoff records analysis and calibration scope. It does not authorize a
new Godot run, a replacement holdout, or tuning against a new seed.

## Reproducible evidence

The full decomposition uses the three immutable 522-week runs:

```text
d6-economic-lifecycle-founder-emergence-decade-1001
d6-economic-lifecycle-founder-emergence-decade-1002
d6-economic-lifecycle-genre-label-holdout-2007
```

New evidence artifacts:

```text
SimTools/analyze-genre-historical-bounds.mjs
SimTools/GenreHistoricalBoundsAnnual.csv
SimTools/GenreHistoricalBoundsSummary.md
```

The annual CSV has 1,260 rows: 42 canonical genres x 10 completed years x
three seeds. Each row contains:

- authored emergence, death, baseline, family, and Single orientation;
- supply selections split into retained, weighted-transition, and annual-floor
  routes;
- release decisions split into Single, Album, orphan, standalone, and promo;
- fulfilled units, annual share, backorders, and units per supplied project;
- independently reconciled settlement units split by physical format;
- mean routed acceptance, eligible records, charted records, and radio.

Aggregation rules:

- `geography-metrics.csv` owns fulfilled commercial units;
- `completed-week-settlement.csv` owns the independent physical-format unit
  split;
- only non-overlapping `AllSegments` rows from `genre-market-weekly.csv` are
  averaged;
- no cohort/lifetime value is substituted for annual commercial units.

Reproduce the report with:

```powershell
& 'C:\Users\grohl\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe' `
  SimTools/analyze-genre-historical-bounds.mjs SimLogs `
  SimTools/GenreHistoricalBoundsAnnual.csv `
  SimTools/GenreHistoricalBoundsSummary.md
```

## What the market makeup says beyond Psychedelic Rock

The broad historical succession is substantially healthier than the overall
FAIL might suggest:

| Surface | Cross-seed result | Reading |
| --- | --- | --- |
| Concentration | **PASS** | Largest annual canonical share is Traditional Pop at 28.08% in seed 1001/1967, below 35%. |
| Doo-Wop | **PASS** | Peaks in 1960 in all seeds and is a small legacy market by 1967. |
| British break | **PASS** | British Beat and British Pop first carry units in 1964 in all seeds. |
| Surf Rock | **PASS** | Peaks in 1963-64 and fades sharply. |
| Folk | **PASS** | Peaks in 1965 in all seeds; Folk Rock takes over later. |
| Soul/Funk | **PASS** | Soul remains material; Funk rises strongly from 1967 to a 1969 peak in all seeds. |
| Late Rock | **PASS on aggregate arc** | Hard Rock, Blues Rock, Proto-Metal, and Progressive Rock total 12.85m-16.07m units in 1969. |
| Specialist survival | **PASS** | Country, Jazz, Easy Listening, Gospel, Blues, Classical, Childrens, TexMex, and Latin Pop remain nonzero. |
| Specialist regions | **PASS** | TexMex is Southwest-highest, Boogaloo East-Coast-highest, and all three preferred Country regions exceed national share in every seed. |
| Psychedelic timing | **FAIL** | Commercial peak is 1969 in all three seeds. |
| Realized format ordering | **FAIL / REOPEN** | Authored orientation does not produce the intended cross-genre decision ordering. |

The late market is diversified rather than captured by one successor. In the
holdout's 1969 market, the leading shares are Traditional Pop 20.57%, Rock and
Roll 10.40%, Country 8.74%, Jazz 6.08%, Funk 5.91%, Blues Rock 4.95%, Soul
4.71%, Bubblegum 4.70%, Sunshine Pop 4.49%, and Baroque Pop 3.81%.
Psychedelic Rock is only 0.28%.

The Psychedelic failure is therefore a chronology and format-conversion
failure, not a concentration failure. The market has plausible early decline,
mid-decade transition, specialist continuity, and late diversification, while
the physical format fork assigns several individual genres implausibly.

## Full genre disposition

These are calibration judgments, not retroactively invented hard gates.
`Arc` compares onset, commercial peak, decline, and succession with the
authored catalog and Directive 5. `Format` compares realized release decisions
with the authored relative orientation. Physical unit mix is supporting
evidence but is not expected to equal decision mix because Singles and Albums
have different prices, lives, and yields.

| Genre | Arc | Format | Cross-seed finding |
| --- | --- | --- | --- |
| Acid Rock | **PASS** | **WATCH** | Emerges 1966 and peaks 1967 in all seeds; 32-39% of decisions are Singles, but 92-95% of units come through Singles. |
| Baroque Pop | **PASS** | **PASS WITH WATCH** | Emerges 1966 and peaks 1967; its neutral orientation realizes as a strongly Single-led portfolio. |
| Blues | **PASS** | **WATCH** | Stable nonzero specialist market, peaking 1968-69; seed 2007 has 45 decisions but zero Singles in 1969. |
| Blues Rock | **PASS** | **FAIL** | Strong 1968-69 successor arc, but 82-86% of decisions are Singles despite Album-leaning metadata. |
| Boogaloo | **PASS** | **FAIL** | Correct 1966 onset/1966-67 crest and East Coast lead; only 7-9% of decisions are Singles despite 0.70 Single orientation. |
| Bossa Nova | **WATCH** | **FAIL** | Acceptance peaks 1963, but commerce peaks 1965-66; about 99.6% of decisions and effectively all units are Singles despite a near-neutral 0.45 orientation. |
| British Beat | **PASS** | **WATCH** | Correct 1964 break and 1965 commercial peak; only 26-29% of decisions are Singles despite 0.75 orientation, although Singles dominate units. |
| British Blues | **PASS** | **WATCH** | Correct bridge and 1965 peak; only 7-14% of decisions are Singles at 0.65 orientation. |
| British Pop | **PASS** | **WATCH** | Correct 1964 break and 1965 peak; only 21-24% of decisions are Singles at 0.80 orientation. |
| Bubblegum | **PASS** | **PASS** | Correct 1967 onset and 1968-69 crest; 76-77% Single decisions. |
| Childrens | **PASS** | **PASS WITH WATCH** | Remains nonzero and low-share; Album-led decisions are consistent with the profile, while physical units remain Single-heavy. |
| Classical | **PASS** | **PASS WITH WATCH** | Stable nonzero niche with Album-led decisions; physical units remain Single-heavy and should be monitored, not tuned by volume alone. |
| Comedy | **PASS WITH WATCH** | **PASS** | Stable niche with a 1964-65 commercial crest later than its authored acceptance crest; format decisions are Album-led as intended. |
| Contemporary Folk | **PASS** | **PASS WITH WATCH** | Emerges 1961 and peaks 1964-65; seed 2007 has a zero-Single 1969 tail. |
| Country | **PASS** | **PASS** | Stable market, all regional requirements pass, and mixed/Single-capable decisions are plausible. |
| Country Rock | **PASS** | **FAIL** | Correct 1968 onset and 1968-69 rise; 75-84% of decisions are Singles despite 0.40 Album-leaning orientation. |
| Doo-Wop | **PASS** | **PASS** | Correct early peak and sharp decline; strongly Single-led. |
| Easy Listening | **PASS** | **PASS** | Nonzero throughout with Album-led decisions and an LP-era commercial rise. |
| Folk | **PASS** | **PASS** | Peaks in 1965 in all seeds and declines thereafter; mixed-to-Album decisions are plausible. |
| Folk Rock | **PASS WITH WATCH** | **WATCH** | Peaks 1967-68, one to two years after its acceptance crest; 83-90% Single decisions are high for a 0.55 profile. |
| Funk | **PASS** | **PASS** | Correct late rise and 1969 peak; strongly Single-led portfolio fits its 0.70 orientation. |
| Garage Rock | **PASS** | **WATCH** | Correct 1963 onset, 1965 crest, and decline; decision mix is much more Album-heavy than its 0.85 orientation, while units remain Single-led. |
| Gospel | **PASS** | **PASS** | Stable nonzero specialist market with the required southern/southwestern response. |
| Hard Rock | **PASS** | **FAIL** | Correct 1967 onset and 1968-69 rise; 77-81% Single decisions invert its 0.40 Album orientation. |
| Jazz | **PASS** | **PASS WITH WATCH** | Stable substantial niche with Album-led decisions; commercial peak shifts into 1964-66 with the broader Album economy. |
| Latin Pop | **PASS** | **WATCH** | Stable nonzero market with plausible mid-decade strength; decision mix is more Album-heavy than its 0.60 orientation. |
| Progressive Rock | **PASS** | **FAIL** | Correct 1968 onset and 1969 peak; 67-82% Single decisions invert the strongest Album orientation in the late-Rock set. |
| Proto-Metal | **PASS** | **FAIL** | Correct 1968 onset and 1969 peak; 65-75% Single decisions invert its 0.40 orientation. |
| Proto-Punk | **PASS** | **FAIL** | Correct late onset and 1968-69 rise; 77-81% Single decisions invert its 0.40 orientation. |
| Psychedelic Rock | **FAIL** | **FAIL** | Peaks commercially in 1969 in all seeds; 1967 Single supply is zero or near-zero, then cap-forced late Singles dominate commercial yield. |
| Reggae | **PASS** | **FAIL** | Correct 1968 onset and 1969 rise; 0-7% Single decisions invert its 0.80 orientation. |
| R&B | **PASS** | **PASS** | Correct early/mid decline path and strongly Single-led mix. |
| Rock and Roll | **PASS** | **PASS** | Correct early peak and persistent but lower late base; strongly Single-led. |
| Rocksteady | **WATCH** | **FAIL** | Onset is correct, but commercial peaks vary 1966-68; only 3-6% Single decisions invert 0.80 orientation. |
| Singer-Songwriter | **PASS** | **PASS WITH WATCH** | Correct 1967 onset and 1969 rise; Album-led decisions fit 0.35 orientation, but a small Single minority supplies 41-65% of units. |
| Ska | **WATCH** | **FAIL** | Correct 1964 onset, but commerce peaks 1964-65 before its 1967 acceptance crest; only 9-17% Single decisions at 0.80 orientation. |
| Soul | **PASS** | **WATCH** | Strong mid-decade peak and material late share; decision mix is Album-heavy for 0.70 orientation even though Singles dominate units. |
| Sunshine Pop | **PASS** | **PASS WITH WATCH** | Correct 1965 onset and 1966-68 crest; very Single-led for a 0.55 profile but historically plausible. |
| Surf Rock | **PASS** | **PASS** | Correct early/mid crest and collapse; strongly Single-led. |
| Teen Pop | **PASS** | **PASS** | Correct early peak and decline; strongly Single-led. |
| TexMex | **PASS** | **WATCH** | Nonzero and Southwest-highest in all seeds; only 26-38% Single decisions at 0.75 orientation, though Singles dominate units. |
| Traditional Pop | **WATCH** | **PASS WITH WATCH** | Seed 2007 peaks in 1961, but seeds 1001/1002 peak in 1965/66 despite a declining authored baseline; it remains the largest late genre. |

## Systemic format finding

The realized format problem is not subtle:

- across the 42 pooled active-genre portfolios, authored Single orientation
  has only `0.119` Pearson correlation with actual Single decision share;
- using only 1969 decisions removes era timing as an explanation, but the
  correlation is `0.033`, `-0.110`, and `-0.111` in seeds 1001, 1002, and
  2007;
- the decision-weighted 1969 ordering is reversed in every seed:

| Seed | Album-lean `<=.40` | Middle `.45-.65` | Single-lean `>=.70` |
| ---: | ---: | ---: | ---: |
| 1001 | 29.5% Singles | 26.9% | **20.0%** |
| 1002 | 27.4% Singles | 26.0% | **21.9%** |
| 2007 | 27.9% Singles | 27.6% | **19.7%** |

The centered orientation multiplier is only `+/-22%` around neutral, while
shared era economics, artist history, project memory, and per-artist Album
capacity dominate the fork. A fixed-input conservation probe can pass while
the unattended portfolio still reverses the authored cross-genre ordering.

Do not respond by merely increasing the tilt strength. The strongest
inversions have different causes and need the structural capacity seam fixed
first.

## Psychedelic Rock decomposition

| Seed | Year | Supply / retained | Single / Album decisions | Mean acceptance | Mean charted | Fulfilled units |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1966 | 17 / 6 | 0 / 17 | .679 | 0.00 | 121,963 |
| 1001 | 1967 | 31 / 17 | 4 / 27 | .817 | .06 | 126,343 |
| 1001 | 1968 | 53 / 33 | 3 / 50 | .832 | .19 | 283,639 |
| 1001 | 1969 | 53 / 37 | 3 / 50 | .833 | .27 | **414,896** |
| 1002 | 1966 | 9 / 5 | 0 / 9 | .735 | .12 | 168,550 |
| 1002 | 1967 | 38 / 24 | 3 / 32 | **.895** | .23 | 241,844 |
| 1002 | 1968 | 48 / 27 | 2 / 41 | .870 | .46 | 186,794 |
| 1002 | 1969 | 52 / 38 | 3 / 49 | .833 | .12 | **338,244** |
| 2007 | 1966 | 15 / 7 | 2 / 13 | .759 | .37 | 321,076 |
| 2007 | 1967 | 32 / 11 | **0 / 31** | **.914** | **0.00** | 83,679 |
| 2007 | 1968 | 39 / 26 | 1 / 38 | .775 | 0.00 | 139,828 |
| 2007 | 1969 | 57 / 37 | 5 / 51 | .815 | .35 | **392,265** |

This makes the 1967 problem concrete. Psychedelic Rock is Album-leaning, not
Album-exclusive. A year with 31 decisions, zero Singles, zero mean charted
records, and the highest routed acceptance in the run cannot represent the
1967 crossover market that produced both major LPs and major Singles.

### First supported causes

1. **Coarse identity retention keeps supply growing after the authored
   crest.** `GetProjectIdentityRetention` gives an established identity a
   fixed `.78` retention. Psychedelic Rock remains `Established` until after
   1970 because its death year is 1971, even though its authored baseline falls
   from `.85` in 1967 to `.75` in 1968 and `.65` in 1969. Retained selections
   consequently grow to 37-38 in 1969 as the native catalog expands. The
   retained route bypasses prospective supply weighting before the declining
   baseline can diversify the project.

2. **The two-Album artist cap converts late Album-favored projects into
   Singles.** Under global Album-share pressure, an artist may schedule at most
   two Album projects per year. Every one of the 11 primary-Psychedelic Singles
   released in 1969 across the three seeds had:

   - an Album gate projection above its Single projection; and
   - exactly two earlier Album decisions by the same artist that year.

   The format was therefore not chosen because the Psychedelic project became
   a better Single. It was the fallback after the generic capacity rule denied
   a third Album.

3. **Those forced Singles materially create the late peak.**

| Seed | 1969 cap-forced new Psychedelic Singles | Their 1969 units | Share of 1969 Psychedelic settlement units |
| ---: | ---: | ---: | ---: |
| 1001 | 3 | 160,390 | 38.6% |
| 1002 | 3 | 70,251 | 20.8% |
| 2007 | 5 | 218,759 | **55.7%** |

   In seed 2007, `gen_43605` and `gen_42723` alone supply 188,609 completed-1969
   units, about 48% of the Psychedelic settlement total. This is not one random
   jackpot; it is a repeatable capacity route with seed-dependent yield.

4. **Momentum amplifies the late catalog.** In seed 2007, the authored mean
   baseline falls from `.700` in 1968 to `.650` in 1969, but mean routed
   acceptance rises from `.775` to `.815` because mean pre-shock momentum rises
   from `.365` to `.856`. The state reaches its `1.0` ceiling for long stretches.
   Lean telemetry suppresses `genre-events.csv`, so the immutable run cannot
   separate direct Psychedelic evidence from the strong `.80` Acid
   Rock/Psychedelic adjacency. Treat momentum as a supported amplifier but not
   the first resolved cause.

5. **Fulfillment makes the late supply efficient rather than creating it.**
   Backorder rate falls from 26.45% in 1967 to 12.29% in 1969. This helps late
   projects convert, but the divergence exists earlier at supply and format
   allocation.

### Causes not supported as first repairs

- Do not lower the 1967 Psychedelic baseline; it already produces the intended
  acceptance crest in seeds 1002 and 2007.
- Do not widen transition compatibility; the prior compatibility repair is
  active and the remaining primary identities are adjacent.
- Do not change Album drop scheduling; delays are not the binding late seam.
- Do not globally shorten catalog life; carry-in is background capacity, while
  the new cap-forced 1969 Singles provide the direct late increment.
- Do not change finance, charts, release growth, label count, distance,
  seasonality, or the accepted economy to repair genre timing.

## Required calibration order

### 1. Repair prospective project allocation before tuning demand

Replace the silent `Album wins -> artist cap rejects -> emit Single` behavior
for live enabled projects.

The repair must be prospective and capacity-conserving. The preferred shape is
to apply a deterministic per-artist/year same-genre project pressure before
record construction:

- after an artist has already consumed the bounded project budget for one
  Album-leaning genre, reduce retention and return the same project opportunity
  to the existing weighted transition set;
- preserve the release roll, total project opportunity, candidate chronology,
  disabled RNG boundary, and global Album-share protection;
- do not simply allow unlimited third Albums;
- do not emit a Single whose Album gate still wins solely because the artist
  reached the generic Album cap.

Add explicit telemetry/probes for `albumWinsBeforeCapacity`,
`artistAlbumProjectsBeforeDecision`, `capacityRejectedAlbum`, and the
prospective reroute result.

### 2. Make identity retention respond to the authored curve

The lifecycle enum is too coarse to own project retention by itself. For
finite-wave genres, retention should taper prospectively after the catalog
baseline crest or negative slope becomes material.

Requirements:

- inputs are immutable catalog values, year, artist identity, and prospective
  same-artist project history only;
- no realized units, chart result, annual peak, backorder, or seed result may
  enter the rule;
- Psychedelic retained supply must not continue rising mechanically through
  1969 after the authored 1967 crest;
- late successor genres must receive the conserved opportunity rather than
  reducing total release capacity;
- stable/no-death genres and disabled/prewarm behavior remain unchanged unless
  separately justified.

### 3. Recalibrate centered format influence across the complete catalog

Only after steps 1-2 pass fixed probes, increase or reshape the centered
format influence enough that unattended decisions preserve the authored
ordering:

```text
Single-lean group > middle group > Album-lean group
```

The fixed test must sweep all 42 profiles at the same artist, label, era,
quality, market, and memory inputs. It must prove:

- monotonic response to `singleOrientation`;
- centered combined opportunity remains within the existing `+/-2%` bound;
- AI priors and realized demand use the same tilt;
- no genre becomes format-exclusive merely from a `.45` versus `.55`
  orientation;
- the 1960 global mix and accepted Album gross crossover remain protected.

Do not calibrate from physical unit share alone. Use decision share, projected
margin decomposition, release counts, units, and gross together.

### 4. Diagnose momentum only if the late peak survives

If the supply/capacity repair restores mixed 1967 formats but Psychedelic
commerce still peaks in 1969, run a separately authorized full-telemetry
checkpoint that preserves `genre-events.csv`.

Decompose the 1968-69 Psychedelic shock into:

- direct Psychedelic primary impulses;
- secondary-identity contribution;
- Acid Rock adjacency;
- other adjacency;
- decay and saturation;
- donor pressure and Zeitgeist delta.

Only then consider the `.80` Acid adjacency, 24-week half-life, saturation,
fatigue, or impulse level. Do not tune momentum from the lean aggregate.

## Acceptance targets for the next candidate

These are prospective calibration targets for adjudication:

1. Every seed with at least 20 Psychedelic decisions in 1967 has nonzero
   Singles; pooled 1967 Psychedelic Single decision share is nontrivial rather
   than zero/near-zero.
2. Psychedelic commercial peak is 1967 or 1968, not 1969.
3. Psychedelic supply and retained supply crest no later than 1968 without a
   hard release quota.
4. No 1969 Psychedelic Single is created solely because an Album-favored
   decision hit the two-Album artist cap.
5. In each measurement seed, the 1969 Single-decision ordering is
   Single-lean > middle > Album-lean.
6. No genre-year with at least 20 decisions silently collapses one format lane
   without a documented historical/product exception.
7. Doo-Wop decline, the 1964 British break, Surf/Folk timing, late Rock/Funk
   succession, specialist survival, the 35% cap, and all three regional
   specialist gates remain passing.
8. Economy, reconciliation, scheduled-Album ratio, Album decision share,
   successful releases, label lifecycle, and the Single-lane analyzer retain
   their accepted gates.

## Validation ladder and stop conditions

1. Add fixed probes for the pre-cap Album winner, prospective reroute,
   trend-aware retention, all-profile monotonic format ordering, combined
   opportunity conservation, and disabled neutrality.
2. Build with zero errors and preserve the disabled exact boundary.
3. Run no simulation until separately authorized.
4. With authorization, begin with one full-telemetry seed-1001 checkpoint and
   run the analyzer against the existing seed-1001 baseline.
5. Stop on any economic, reconciliation, release-capacity, global Album-mix,
   historical-onset, regional, or format-ordering regression.
6. Only after seed 1001 passes, request authorization for seed 1002.
7. Preselect a new untouched holdout seed before inspecting its output. Seed
   2007 has already been consumed and must never be reused as a fresh holdout.

The next pass should be judged as a catalog-wide format and supply calibration,
with Psychedelic Rock as the clearest sentinel, not as a one-genre keyframe
patch.
