# Genre Market V2 Phase 2 Repair Handoff

## Status and stop condition

Phase 2 is rejected. Do not begin Phase 3 and do not run seeds 1002/1003 until the seed-1001 failures below have been repaired and have passed the short validation ladder.

The completed seed-1001 520-week checkpoint is `d5-phase2-supply-520-enabled-1001`. Its matching economic control is `4c-releaseonly-enabled-1001`. The checkpoint passed decade economics but failed three independent hard gates:

- successful releases were `0.794x` control, below the `[0.85,1.15]` gate;
- British Beat and British Pop produced zero units throughout, including 1964 onward;
- Soul reached `52.2%`, `58.1%`, and `64.3%` of canonical national units in 1967-69, above the `35%` cap.

No simulation was run while preparing this handoff. The complete checkpoint table and rejection are recorded in `SimTools/GenreMarketV2Audit.md`.

## Primary diagnosis: lifecycle filtering destroys release capacity

This is primarily a supply-pipeline integration defect, not evidence that the late Soul demand keyframes should be reduced.

The release-capacity streams show:

| Measurement | Enabled | Control |
|---|---:|---:|
| Release rolls fired | 62,165 | 57,981 |
| Successful releases | 45,873 | 57,751 |
| Failed release rolls | 16,292 | 230 |
| Artist-selection/cooldown mismatches | 16,239 | 147 |
| Success rate | 73.79% | 99.60% |

Thus `99.67%` of enabled failures are attributed to artist selection, even though enabled mode fires more release rolls than control. The annual failure cliff begins exactly when several early genres cross their authored death dates:

| Year | Successful releases vs control | Enabled failed-roll rate |
|---|---:|---:|
| 1960 | 0.887x | 14.3% |
| 1961 | 1.003x | 3.0% |
| 1962 | 0.993x | 3.5% |
| 1963 | 0.976x | 4.1% |
| 1964 | 0.970x | 3.9% |
| 1965 | 0.974x | 2.6% |
| 1966 | 0.627x | 49.8% |
| 1967 | 0.560x | 55.3% |
| 1968 | 0.548x | 51.3% |
| 1969 | 0.548x | 44.2% |

`CompetitorManager.CalculateWeeklyReleaseChance` counts every roster artist at or beyond the broad cooldown threshold. Once a roll fires, `AILabel.GetArtistForRelease` applies `GenreSupplyService.IsAvailableForNewSupply` and removes every legacy-genre artist, plus every deferred British artist, from the actual candidate set. A label can therefore appear to have release capacity at the probability seam and then fail selection because the two seams disagree.

This also prevents the project evolution route from doing its job. `CompetitorManager.TryReleaseRecord` calls `ChooseEnabledGenreSupply` only after an artist has been selected. A legacy artist is filtered out before that call and never gets the opportunity to make either a diminished legacy project or a project in an available successor genre.

Relevant code:

- `Systems/CompetitorManager.cs`: `CalculateWeeklyReleaseChance`, `TryReleaseRecord`, and `ChooseEnabledGenreSupply`;
- `Data/AILabel.cs`: `GetArtistForRelease`;
- `Systems/GenreSupplyService.cs`: `IsAvailableForNewSupply`, `ChooseGenre`, and `GetSupplyWeight`.

## Why Soul fills the vacuum

The user's vacuum hypothesis is supported, but the mechanism is supply-side:

1. Teen Pop, R&B, Doo-Wop, Folk, and other early cohorts become ineligible for releases rather than gradually surrendering capacity.
2. Soul has no death date, retains a `0.90` late-decade catalog baseline, and preserves established artist identity on `90%` of projects.
3. The 1960 artist pool is permanently seeded from a hard-coded early distribution. It gives substantial weight to Soul, Motown, Girl Group, and R&B; Motown maps to Soul, and some Girl Group records map to Soul.
4. Historical labels and generated Soul Factory labels reinforce Soul/R&B specialties.
5. No comparable native artist population is generated for most later genres. Scouting only ranks the existing unsigned pool; it does not create an era-aware replacement pool.
6. The deterministic floor of three projects per available genre per year prevents omitted rows, but it is much too small to create a competitive late-decade ecosystem.
7. The current concentration brake is per-label, mild, capped after eight recent projects, and does not respond to global annual genre share.

The economic pass should not be interpreted as pipeline health. Units were `1.033x` control while successful releases were `0.794x`, implying about `1.30x` control units per successful release. Aggregate demand is masking missing supply and excessive concentration.

## Required Phase 2 repair scope

Implement the smallest enabled-only supply repair that addresses all three hard failures while preserving disabled exactness.

### 1. Separate supply lifecycle predicates

Do not use one predicate for all of these decisions:

- whether a new artist may be created or signed in a genre;
- whether a new project may retain a genre;
- whether an existing artist may be selected for a release;
- whether an existing artist may transition to another project genre.

`IsAvailableForNewSupply` is appropriate for new identity creation/signing, but it should not silently delete an existing artist from label release capacity. Make the release-chance population and actual release-selection population agree.

Existing legacy artists should reach project selection. The enabled path may then choose between a deliberately diminished legacy project and a transition toward an available genre. Do not restore unchecked growth of dead genres.

### 2. Restore conserved release opportunity

The repair must remove the late-decade artist-selection failure cliff without changing the accepted global release-growth constants. Do not compensate by increasing release chance, label count, roster capacity, finance, or chart constants.

Add fixed probes demonstrating that:

- every artist counted as available by the release-probability seam can be considered by the selection seam;
- a roster containing only legacy artists does not generate phantom rolls that always fail;
- legacy identity does not make a label permanently unable to release;
- pre-emergent genres remain unavailable for new supply;
- disabled behavior and RNG remain untouched.

### 3. Provide real late-decade supply

The static 1960 unsigned pool cannot be the sole identity source for a ten-year evolving market. Add a bounded, deterministic enabled-only route for later genres to receive meaningful supply. Acceptable shapes include era-aware unsigned-artist replenishment, deliberate artist evolution, or a combination, provided they do not add arbitrary release capacity or begin Phase 3 adjacency/momentum work.

Supply should follow catalog lifecycle, label fit, and regional/segment fit. It must create enough competition for Psychedelic Rock, Hard Rock, Blues Rock, Funk, Progressive Rock, Singer-Songwriter, Country Rock, and other late genres to fill part of the capacity surrendered by early genres.

The three-project annual floor may remain as omitted-row protection, but it must not be treated as the ecosystem mechanism.

### 4. Add a minimal British supply onset

British Beat, British Pop, and British Blues are required for the Phase 2 unattended-market shape even though the full British Invasion event mechanic does not yet exist.

Implement a minimal exogenous supply route with these boundaries:

- no British Beat/Pop break in 1960-62;
- meaningful British Beat/Pop supply begins in 1964;
- British Blues participates from the appropriate mid-decade window;
- the route reallocates or introduces bounded artist/project supply without inflating total release capacity;
- it remains deterministic and enabled-only.

Do not merely remove `DeferredBritishGenres`: British Beat's catalog emergence year is 1963, so unqualified removal can produce the wrong onset, and the static artist pool still lacks a meaningful British cohort. A full import/national-origin/cultural-event simulation can remain deferred; Phase 2 only needs an explicit, auditable 1964 supply bridge.

### 5. Address Soul concentration structurally first

Do not lower Soul demand, its late catalog keyframes, unrelated segment capacities, finance constants, or chart constants as the first response.

First repair conserved release capacity, later-genre replenishment, British supply, and global supply diversification. Reassess Soul only after those routes work. If Soul still exceeds `35%`, the next authorized calibration target is the supply-selection balance: identity retention, global recent-supply/share braking, and label/artist fit strength. Historical keyframes may be reconsidered only after routing defects are excluded, per Directive 5 section 17.4.

Any concentration control should be smooth and prospective. Do not normalize directly from realized unit share or guarantee an outcome by force; release count and realized timing are explicitly forbidden as catalog-constant normalizers.

## Explicit non-goals and guardrails

- Do not begin Phase 3 momentum, adjacency, fatigue, shock redistribution, emergence advance, or endogenous Zeitgeist work.
- Do not change the seven-region taxonomy, release-capacity growth, Album crossover, prices, costs, royalties, finance rules, chart sizes/weights, retirement tolerances, distance calibration, or 4C seasonality constants.
- Do not tune historical baseline keyframes before the supply bugs are excluded.
- Do not add demand by summing overlapping audience segments.
- Do not consume global RNG for deterministic routing or disturb the disabled stream.
- Do not run seeds 1002/1003 while seed 1001 still fails a short gate.
- Do not run a 520-week simulation unless it is expressly authorized after the short gates pass.

## Recommended implementation order

1. Introduce distinct new-supply, existing-release, and project-transition eligibility APIs.
2. Make release probability and artist selection use a consistent eligible roster definition.
3. Add fixed probes for legacy-roster release conservation and disabled isolation.
4. Add the bounded late-genre replenishment/evolution route.
5. Add the explicit 1964 British supply bridge.
6. Add or revise prospective global supply diversification only if the structural routes alone do not adequately spread projects.
7. Run the existing fixed probe suite and build.
8. With authorization, run a short seed-1001 checkpoint and compare release rolls, selection failures, successful releases, genre supply, British onset, Soul share, units, and market net.
9. Only after the short gates pass, request authorization for the prescribed longer measurement ladder.

## Short-checkpoint acceptance targets

Before any new 520-week measurement, require:

- no material artist-selection mismatch caused by disagreement between lifecycle and cooldown seams;
- successful releases plausibly tracking control and trending toward `[0.85,1.15]`;
- no regression in scheduled Album projects or format mix;
- nonzero late-genre supply when catalog lifecycle allows it;
- British Beat/Pop absent in 1960-62 and supplied from 1964;
- declining early genres not continuing unchecked, but also not destroying label release capacity;
- no sign that Soul is inheriting nearly all surrendered supply;
- short-run units and market net inside the applicable economic bands;
- fixed probes, build, disabled boundary, and determinism intact.

## Remaining Phase 2 completion evidence

After a candidate passes the authorized short ladder, Phase 2 still requires the Directive 5 three-seed measurement and historical review. The final evidence must cover:

- successful releases and scheduled Album projects in `[0.85,1.15]` of control per seed;
- decade units, gross, label net, and market net in `[0.90,1.10]` per seed;
- pooled annual and catastrophic economic guards;
- each format's decade-unit gate and inherited Album crossover/1960 mix gates;
- all required historical genre shapes, including the 1964 British break and late Rock/Funk activity;
- the `35%` annual canonical-genre concentration cap;
- nonzero specialist and regional markets;
- inherited chart-health, finance, regional, distance, concentration, and seasonality checks;
- disabled byte-exact behavior and enabled deterministic repeat.

Only after that evidence passes is Phase 2 complete and Phase 3 authorized.
