# Rock songbook calibration and release-volume diagnosis

October 2, 2026. Continues the default-off selection/consumer slice in `PolarSongSelectionReport.md`. The user authorized investigation of the roughly 3% single-volume reduction and downward calibration of rock standard/traditional sourcing, preserving rare exceptions. This changes rock sourcing under the polar flag; it does not enable the flag or retune recording realization, critic effects, genre demand, release cadence, or rights.

## Why single volume fell

The previous change altered recorded execution as well as material selection. `SongMaterialApplicationService.Apply` now bounds the recording hook by the composition hook and applies the capability execution factor to hook and production. It replaces the old source-authority blend toward expected material hook and professional-polish boost. Album cuts also receive execution loss; promoted singles now reuse those cuts instead of selecting/blending another song.

In the prior 52-week pairs, the mean quality of new single release lanes fell from 0.6105 to 0.5169 (1001) and 0.6081 to 0.5136 (1002). Market units fell from 195.80m to 176.76m and 195.50m to 181.05m. Summed weekly label net fell from $156.90m to $140.04m and $158.95m to $144.21m.

This feeds the existing economy. `CalculateWeeklyReleaseChance` uses label status and release-eligible roster capacity: Rising labels receive a 1.2 status multiplier, Stable 1.0, Struggling 0.5, and Dying 0.3. Lower returns also affect roster growth, financing and format/project choices. Release rolls fell from 4718 to 4582 and 4755 to 4600. Failed rolls increased only 37→47 and 29→41; cooldown mismatch counts remained zero. Most of the aggregate successful-release reduction is therefore fewer opportunities, not an inability to find songs. Polar refusal is not wired.

Two diagnostic ablations retain polar selection and the old rock source policy, using the same development seeds and 52-week window:

| Configuration | Singles, seed 1001 | Singles, seed 1002 |
|---|---:|---:|
| Original flag-off | 4983 | 5044 |
| Prior full polar behavior | 4846 | 4880 |
| Polar selection with legacy realization/critic/market group | 5081 | 4997 |
| Polar behavior with legacy promo selection/application | 4945 | 4858 |

Restoring the realization/critic/market group consistently increases singles (+235/+117 versus full polar), while restoring legacy promo behavior alone is mixed (+99/−22). That identifies the downstream realization group as the largest consistent contributor measured here. It does not allocate an exact percentage to the hook ceiling versus capability, critic and Moment effects individually. These are evolving-world interventions: changes in statuses, rosters, projects and RNG traversal interact, so ablation deltas cannot be added as an exact causal decomposition.

`--polar-fit-audit-legacy-realization` and `--polar-fit-audit-legacy-promotion` are headless diagnostic controls, default false and not persisted as gameplay settings. Normal gameplay uses neither.

## Why rock songbook sourcing was excessive

The 1960 Rock-family anchor carries raw weights `{original .32, professional .05, standard .18, recent hit .27, traditional .18}`. Standards receive the existing 0.40 suppression, leaving .072. Traditional material receives no suppression. Their combined normalized share is `.252 / .892 = 28.25%` before suitability or pool availability. This is already far above the requested rare-exception role.

The polar source choice preserves that prior and replaces the old source-specific preference factors with realized fit. Easy-to-execute traditional/blues arrangements can compete well with newly written material after resolution. The source remains standard/traditional even if its arrangement becomes rock-like. The excess is present in ordinary singles, not just album promos:

| Prior polar Rock-family lane | Seed 1001 | Seed 1002 |
|---|---|---|
| Orphan singles | 191/596 = 32.05% | 210/665 = 31.58% |
| Promo singles | 28/75 = 37.33% | 34/90 = 37.78% |

Promos retain a material plan whose rock album slots already include these songbooks, then choose the strongest-fit cut. Some rock acts also make projects in other genres. A policy keyed only to the project genre would leave that route open. Pool candidate count is not itself the source-share multiplier: source finalists compete by their weights. Larger pools mainly improve candidate availability and sampled suitability.

## Calibration

`Data/PolarSongTable.json` is now version `polar-rock-calibration-v1`:

- `rockStandardFactor = 0.02` replaces the legacy 0.40 standard factor for a Rock-family act or Rock-family project.
- `rockTraditionalFactor = 0.02` multiplies the otherwise unsuppressed traditional prior for the same contexts.

The resulting 1960 Rock prior is 1.1125% standard/traditional combined, with both categories nonzero; the 1969 endpoint is about 0.1503%. These are implementation calibration values under the user's “very rarely” direction, not a claimed historical estimate. Recent-hit cover weights are untouched; R&B/blues hits remain normal cover sources. Folk, gospel, jazz, blues and traditional-pop acts retain their existing priors unless the project itself is Rock-family. FolkRock remains in its canonical Folk family.

The policy reaches all autonomous rock paths:

1. Source-category choice uses both act identity and project genre. A rock act cannot regain a large traditional allocation by selecting a traditional-pop or other project genre.
2. Rock album plans use the smaller shares. Stable hash-based systematic apportionment replaces fixed largest-remainder rounding only for enabled rock plans with actor/album keys. It preserves the exact track count and expected shares while allowing a tiny fractional allocation to occasionally become an actual cut. Ordinary 9–13-track LPs would otherwise round these categories to zero every time.
3. Promo selection applies the same songbook exception factor to a songbook cut's suitability. A rare album cut does not automatically become the default promotional single. Master reuse and lineage remain intact. A pool consisting only of such cuts still has an explicit usable choice.
4. Rock live sets draw from the existing adjacent-family cover catalogues, choose source buckets by prior times fit, and sample suitable songs within each bucket. Source weighting is independent of the number of songs in a bucket. Existing original-count/set-length draws are preserved; source draws use stable hashes, not new GD randomness. Recent-hit covers fill almost all autonomous cover slots.

No title, real artist, or specific historical recording is whitelisted. A Maggie Mae-like traditional album exception remains possible through the nonzero allocation; explicit player song choices and forced-song APIs are not banned.

## Results and verification

Final calibrated 52-week runs, development seeds 1001/1002, otherwise matching the preceding audits:

| Act identity | Prior polar share | Calibrated share |
|---|---|---|
| RockAndRoll, 1001 | 239/735 = 32.52% | 8/758 = **1.06%** (5 standards, 3 traditional) |
| RockAndRoll, 1002 | 267/824 = 32.40% | 6/756 = **0.79%** (4 standards, 2 traditional) |
| All canonical Rock-family acts, 1001 | 240/737 = 32.56% | 8/765 = **1.05%** |
| All canonical Rock-family acts, 1002 | 267/826 = 32.32% | 6/757 = **0.79%** |

The calibrated orphan-single shares are 8/621 = 1.29% and 6/602 = 1.00%. None of the 79/88 observed rock promos uses a songbook cut. Recent-hit cover recordings remain common: 356/765 and 318/757 of all Rock-family material rows. Counts use unique observed single projects and include the initial/prewarm cohort, as in the previous report.

Single project counts after rock calibration are 4943 and 4865; album project counts are 1881 and 1785. Compared with the original disabled counts, singles are −0.80% and −3.55%. The source calibration does not claim to resolve broader execution/economy calibration. Its effect on whole-world counts varies by seed.

`PolarRockSongbookChecks` uses 512 stable fixture identities with the real adjacent-family catalogues. It verifies nonzero rare priors, cross-project protection, unchanged Folk priors, unsuppressed recent-hit covers, exact album/live slot counts, distinct live songs, deterministic apportionment, and disabled allocator compatibility. It produces 40/1536 = 2.60% songbook **live-cover** slots and 49/6144 = 0.80% songbook **album-plan** slots; 27/512 LP plans contain an exception. These are dedicated fixtures, not a historical population census of live sets or released album tracks.

Build passes with the previously documented compatibility/unused-event warnings. Existing ownership, fit and player behavior fixtures pass. The enabled two-week full-world round-trip passes repeat serialization and gzip I/O (`107087042` JSON bytes; `7834155` compressed bytes). Both final disabled 52-week runs remain byte-identical across all 79 legacy CSV streams. Both calibrated enabled runs have finite/ranged fit components, valid lag checks and valid bounded telemetry budgets. `git diff --check` passes.

Reproduction:

- `SaveLoadRoundTripRunner --weeks=0 --seed=1001 --polar-rock-songbook-check`
- `ChartAuditRunner --weeks=52 --seed=1001 --run=polar-rock-calibrated-1001 --use-polar-fit-selection --polar-song-shadow` (repeat with 1002)
- Disabled controls use `--run=polar-rock-off-1001` / `1002` and omit the two polar switches.
- `node SimTools/analyze-polar-calibration.mjs` checks matched-window completion, disabled neutrality, fit/budget validity, rare Rock-family shares, release lanes, financial outcomes and source→act/project assignments. Full output and logs are under ignored `SimLogs/`.

The flag remains default-off. No holdout seed, four-year population live-set baseline or decade behavior run was consumed. Historical taxonomy inference, full hearing history, independent market evidence and the previously reported rights-controller fallback remain unchanged. `PolarRockCalibrationHashes.json` records current source/config/analyzer fingerprints for this slice; earlier manifests describe earlier snapshots.
