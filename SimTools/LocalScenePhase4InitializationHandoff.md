# Phase 4 opening-roster allocation repair

This continuation stays on `codex/local-scene-phase-4`, starting from `6f4630b`.
The primary band-member checkout and its uncommitted edits remain untouched.
Recruitment stays opt-in behind `--enable-scene-recruitment`.

## Allocation policy

Opening allocation gives each eligible label one signing opportunity per round.
Tier priority is Major, MidTier, Independent, Boutique, Small; within-tier order is
seeded by world seed, label identity and round. Preparation uses canonical label-ID
order. This replaces list-order priority without giving a major its entire roster
before smaller competitors get a first opportunity.

Existing capacities and tier fill ranges remain. Majors retain their larger
headcount targets, native scouting capability and earned geographic/national A&R
access. There is no guarantee that they obtain every strongest artist.

Talent access is evaluated before genre fallback. The allocator tries accessible
preferred styles, then accessible secondary styles, then accessible broader styles.
An act may match either its primary or secondary genre, preserving the original
initial genre lookup. Style fallback never broadens geographic access.

Each opportunity uses the existing 4-12-act geographic slate and scouting fog.
The stable prelaunch window is -1; repeated rounds replace contracted slate members
from the remaining real pool without rerolling the same scout/artist read. Opening
scores no longer layer random true-quality jitter over those reads. The existing
weighted choice among up to ten scored candidates remains. Strong acts are not
reserved by quota for any tier.

The live unsigned query cannot be used before prospect-search bootstrap: the
3,000 launch acts do not have their live Seeking status yet. A launch-only query
therefore reads the canonical unsigned registry with existing unsigned/active/
ownership eligibility and restricts it to InitialLegacy. It does not activate
reserves or alter live signing eligibility. The old launch path already used a
genre query before search bootstrap. No births, placements, advances, moves,
foreign routes or runtime-recovery rules are changed.

## Measurement

New initialization CSVs retain desired target versus actual fill before operating
targets are reset to the actual roster. They also record residual accessible supply,
true roster quality, per-round appointments and true versus perceived signing reads.
`strongActs` means base quality >= 0.75 solely for diagnostics; it is not a selection
threshold. Initialization observations are transient diagnostics, not save fields
or additional economic state.

`analyze_scene_initialization.py` compares opening census and tier outcomes to the
previous Phase 4 candidate. Its checks include unique ownership, one turn per label
per round, tier order and whether underfilled labels still have accessible supply.
Economic analysis continues against the frozen Phase 3 matched runs.

## Verification

`scene4-initialization-checks-v2/runs.json` records 49 recruitment checks per seed
(1001 and 2002), 26-week world/real gzip IO roundtrips on both seeds, and all 74
recruitment-off room checks. The first exploratory check caught the live-query
bootstrap mismatch before the launch-only seam was added; that failed run is kept
as `scene4-initialization-checks-v1`.

Opening explained contracts increased from 1,930 to 2,487 on seed 1001 and from
1,747 to 2,301 on seed 2002. Final economic and tier results follow below.

## Completed 52-week comparison

`SimLogs/scene4-initialization-audit-v1/runs.json` records three complete serial
52-week runs. `analysis.json` confirms all 84 economic files remain exactly equal
with recruitment off, 9,200 final registry acts and 2,200 formations in each run,
zero maxima across all nine population/labor integrity fields, and zero unexplained
signings. End room censuses also have zero duplicate bills, person conflicts,
remote days and capacity errors.

| Measure | Seed 1001 previous Phase 4 -> repair | Seed 2002 previous Phase 4 -> repair |
|---|---:|---:|
| Opening explained contracts | 1,930 -> 2,487 | 1,747 -> 2,301 |
| Opening empty labels | 290 -> 24 | 305 -> 24 |
| Opening major contracts | 228 -> 226 | 192 -> 189 |
| Opening MidTier contracts | 287 -> 267 | 227 -> 193 |
| Opening boutique contracts | 261 -> 469 | 272 -> 455 |
| Opening independent contracts | 841 -> 985 | 768 -> 997 |
| Opening small-label contracts | 313 -> 540 | 288 -> 467 |
| Week-52 rostered acts | 2,120 -> 2,387 | 1,980 -> 2,247 |
| Affordable vacancy label-weeks | 2,746 -> 1,194 | 3,101 -> 1,210 |
| End affordable vacancies | 12 -> 12 | 14 -> 15 |
| Defunct labels in final finance census | 154 -> 100 | 136 -> 88 |
| Single units | 157,816,508 -> 151,963,255 | 157,564,323 -> 152,545,911 |
| Album units | 36,511,332 -> 43,214,295 | 31,594,571 -> 34,907,569 |
| Market net | 158,416,506 -> 171,756,278 | 146,240,000 -> 151,921,960 |

All domestic majors meet their requested opening targets. The two empty majors on
each seed are in London; this repair does not invent international connections.
Actual desired targets differ slightly because capacity/target/contract random
calls now occur in rounds rather than whole-label blocks. The range constants are
unchanged. MidTier headcount falls moderately while smaller-label access improves;
this is not a claim that every tier is numerically unchanged.

The new initialization analysis verifies no duplicate artist assignments, no
multiple picks per label per round, no tier-order violations and no underfilled
opening label with accessible unsigned launch supply still present. All 24 empty
opening labels are unresolved HQs, London, Indianapolis or Milwaukee. Those routes
remain an explicit backlog. Desired aggregate targets are 3,598 / 3,577, while
513 / 699 of the existing launch acts remain unsigned in other places. Round
fairness cannot fix that geographic mismatch.

## City placement and stranded launch supply

The user asked whether Billings still outnumbers NYC. It does. The analysis checks
that every one of the 3,000 launch acts retains the same working base as in the
previous candidate. Counts are acts, not musicians, venues or bookings.

| City | Seed 1001 launch count (share) | Signed at launch | Seed 2002 launch count (share) | Signed at launch |
|---|---:|---:|---:|---:|
| New York | 61 (2.03%) | 61 | 68 (2.27%) | 68 |
| Billings | 154 (5.13%) | 67 | 146 (4.87%) | 28 |
| Denver | 140 (4.67%) | 64 | 141 (4.70%) | 47 |
| Nashville | 82 (2.73%) | 82 | 89 (2.97%) | 89 |

The full registry at week 52, including unsigned and latent cohorts, still assigns
Billings 443 / 452 acts (4.82% / 4.91%) and NYC 207 / 229 (2.25% / 2.49%). This is a
separate denominator from the 3,000-act launch cohort. The existing region-first,
near-even city prior gives small-region cities excessive supply relative to label
demand. Full city tables are in `city-allocation-1001.csv` and
`city-allocation-2002.csv` alongside `initialization-analysis.json`.

## Acceptance boundary and next work

The allocation repair resolves the reachable-genre fallback and whole-roster
priority defects. It substantially reduces emptiness and vacancy duration while
preserving domestic majors' requested headcounts, with imperfect talent reads at
all tiers. It does not correct city population/placement or missing connection
routes. Observed true-quality averages span roughly 0.537-0.552 across tiers;
majors are not guaranteed the strongest hidden talent.

Recruitment remains off by default. Relative to Phase 3, year-end headcount is
still lower, vacancies remain elevated, singles decline, and album/net effects
vary by seed. The one-year evidence supports this bounded initialization repair,
not whole Phase 4 economic acceptance or a decade balance claim.

Next work should revise the initial/runtime placement prior against city
opportunity and genre-specific recruiting demand while preserving national counts
and genre mix, and resolve literal HQ/catchment/foreign routes with explicit
provenance. Billings should retain local life rather than absorbing a disproportionate
national cohort through near-uniform regional splitting. No demand-driven extra
births or unlogged national bailouts were added here.
