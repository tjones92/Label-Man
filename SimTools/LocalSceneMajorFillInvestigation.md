# Domestic major opening-fill investigation

Continue `codex/local-scene-phase-4` from city-placement commit `2bf3ab0`.
The user asked whether the two short majors need a fix or are seed variation.

## Finding and decision

No allocation defect found. Retain production behavior: this is observed
seed-dependent finite-pool scarcity, not evidence for extra births, reserve
activation, city reweighting or guaranteed major rosters.

Across ten seeds, only seed 1001 has a short known-domestic major. Of 61 domestic
major opening rosters, 59 meet their targets; 1,984 of 1,992 requested major slots
are filled (99.60%). Missing headcount is eight, all in seed 1001. This is a small
seed sweep, not an estimate proving a universal failure probability or economic
acceptance. Foreign and unresolved HQ routes remain outside this diagnosis.

| Seed | All requested slots | Launch signings | Remaining launch acts | Domestic majors filled / requested |
|---|---:|---:|---:|---:|
| 1001 | 3,598 | 3,000 | 0 | 218 / 226 |
| 2002 | 3,577 | 2,817 | 183 | 189 / 189 |
| 3003 | 3,628 | 2,825 | 175 | 142 / 142 |
| 4004 | 3,506 | 2,948 | 52 | 225 / 225 |
| 5005 | 3,519 | 2,816 | 184 | 136 / 136 |
| 6006 | 3,334 | 2,838 | 162 | 159 / 159 |
| 7007 | 3,650 | 2,942 | 58 | 191 / 191 |
| 8008 | 3,648 | 2,889 | 111 | 207 / 207 |
| 9009 | 3,753 | 2,955 | 45 | 318 / 318 |
| 10010 | 3,509 | 2,915 | 85 | 199 / 199 |

All requested slots include labels with unresolved/foreign geography and do not
represent a feasible domestic allocation. Launch supply is always 3,000 acts;
the separate 4,000-act reserve remains excluded from opening recruitment.
Remaining acts on other seeds are geographically unavailable to the labels that
still request slots. Their domestic majors nevertheless meet every target.

## Seed-1001 causal trace

- NYC `label_0004`: target 42, filled 39, short 3 (92.86% filled).
- Hollywood `label_0248`: target 44, filled 39, short 5 (88.64% filled).
- Both receive their last pick in zero-based round 38. The remaining third major
  receives the national pool's final act in round 39.
- There are no unsigned launch acts left and neither short major has an accessible
  remaining candidate. All 3,000 act IDs appear once in the signing appointments.
- Neither a genre-filter dead end nor an undersized scouting slate strands talent.
  The allocator retains one signing opportunity per label per round and major-first
  tier order. Smaller targets finish earlier; the last large rosters compete over
  the finite remainder. Higher native A&R reach provides access, not an inexhaustible
  supply or a guarantee of filling every desired slot.

The exact seed reproduces deterministically; 'seed variation' describes differences
between worlds, not nondeterminism within one world. The largest individual deficit
is 5/44 (11.36%), although aggregate major deficit across this sweep is only 0.40%.

A future requirement to guarantee major targets would need explicit allocation
priority/reservation or a launch-demand budget. Such a policy would redistribute
signings away from other labels unless supply were increased. This investigation
does not introduce that policy merely to erase a rare measured shortage.

## Verification and reproduction

The two previously completed one-week recruitment opening audits provide seeds
1001/2002. Eight new serial one-week audits provide seeds 3003 through 10010.
All eight exited successfully and produced complete opening census/appointment
and week data. The analyzer verifies the 3,000 launch cohort, unique signings,
one pick per label per round, headcount reconciliation, and zero remaining
accessible supply for any short domestic major. It distinguishes global depletion
from geographic shortage. No production C# or game configuration was changed;
no additional build or economic retest is needed for these audit-only additions.

Artifacts:

- `SimLogs/scene4-major-fill-seeds-v1/runs.json`: eight-run manifest with binary/source
  hashes, exact flags, duration and exit codes; maximum parallelism one.
- `SimLogs/scene4-major-fill-seeds-v1/analysis.json`: completed ten-seed comparison.
- `SimTools/RunSceneMajorFillSeeds.ps1`: repeatable seed sweep; override `-Seeds` for
  more independent worlds and use a new run tag.
- `SimTools/analyze_scene_major_fill.py`: repeatable diagnosis including every
  missing major's city, target, filled count and last signing round.

```powershell
./SimTools/RunSceneMajorFillSeeds.ps1 -RunTag scene4-major-fill-seeds-v2
python ./SimTools/analyze_scene_major_fill.py ./SimLogs/scene4-city-placement-audit-v1/runs.json ./SimLogs/scene4-major-fill-seeds-v2/runs.json --output ./SimLogs/scene4-major-fill-seeds-v2/analysis.json
```

Use the bundled Python executable if the `python` command is a Windows Store stub.
Do not interpret these one-week runs as long-horizon recruitment balance approval.
Recruitment remains opt-in, as before.
