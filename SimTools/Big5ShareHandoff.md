# Handoff: bringing the Big-5 Top-10 share back under 5e (2026-10-28)

**Goal.** Make 5e (`standing` station support plus the unsold floor) hold the Big-5 share of 1960 Top-10 slots within
~2 points of the control. Keep the station shape it buys. Background is in `RegionalSyncDesign.md` §9g-9o (commit
`cadb04d`).

## Where it stands

| 8 seeds, 1960 | Big-5 Top-10 | Top 10 by units alone | Airplay offset | Same / cross city | Airplay |
|---|---:|---:|---:|---|---:|
| History | 34.2 | – | – | 69.7 / 40.3 | – |
| Control | 34.7 | 38.7 | −4.0 | 52 / 39 | 25.5 |
| Floor (W .085) | 39.6 | 41.8 | −2.2 | 68.7 / 50.7 | 27.8 |

The +4.9 lift (SE 1.0, paired) has two parts:
- **The airplay offset shrinks** (−4.0 → −2.2). The panel now follows each market's sellers, so airplay adds to the
  sales leaders instead of lifting others.
- **The units-only share rises** (38.7 → 41.8). This part is untested. A likely route is panel airplay feeding
  awareness and then sales (`ChartManager`: `data.awareness += data.radioPlay * 0.12f`).

## Ruled out (do not repeat)
- **Lowering the panel weight alone:** loses as much offset as standing (§9k).
- **Blending support back toward size-blind:** shape degrades before the share returns (§9j).
- **Changing the aggregation** (`ReporterRadioPlay`, tier weights): it passes on whatever composition candidacy
  seated (§9k).
- **Non-sales new-release picks** (`--station-picks`, any tier, `trial` or `ear` score): every one moves the offset
  toward zero (§9m).
- **Plateau K 25:** its offset gain came from the unsold-record bug and vanishes with the floor. Its breadth gain
  survives (§9n).
- **Advocacy and payola:** player-only, neutral headless.

## Leads, in order
1. ~~**Split the units-only rise.**~~ **Done, negative (§9p).** Panel-free awareness *raises* the Big-5 by +2.7 (SE
   1.3) over the floor arm. Adding breakout raises it +3.5. The rise is rank feedback (exposure weight, Top-10/40
   awareness floors, top-20 push hold), not radio → awareness. Also note that the tail carries the blended
   `radioPlay`, so W .085 is ~0.30 of steady-state rotation.
2. ~~**Change how the panel term is paid.**~~ **Done, negative (§9q).** With the panel paid linearly (L 0.15, airplay
   on the guard), the offset goes to −0.5 (Δ +1.7, SE 0.4, vs the floor) and Big-5 to 43.3. Under the floor arm the
   tail alone is ~2% of airplay, so there is no tail × panel compounding to remove. A linear payout of a sales-ordered
   panel is pure reinforcement.
3. **Chart-side split (§9l option (a), already built):** **Screened with the floor (§9r): meets the goal.** The chart
   is the control's byte for byte on 8 seeds (Big-5 34.7). Station shape is the floor's (68.7 / 51.9; rank / absent
   61.4 / 18.0). The shown lists share 19% of slots with the scoring panel, and their High tier is 33% Big-5 against
   the scoring panel's 21%. Adoption is a design decision: which sheet the player-facing reads follow.

## How to measure
- **Big-5 by label ID**, `label_0001`-`label_0005`. Names vary by seed, and the runtime `Major` tier adds promoted
  fictional majors.
- **Primary metric:** the paired Big-5 Top-10 share against the control. **Secondary:** the paired offset, meaning
  the actual share minus the share in a top 10 ranked by units in the same run. Use seeds 1001-1008; one seed cannot
  resolve the offset.
- **Guards:** airplay (pooled top-40 share, within ±3 of the control), same and cross city (survey cities,
  Top40 + FullService), and top-10 artists.
- **Floor-arm flags:** `--station-playlists --station-regional-support=standing --station-regional-support-rank-noise=0.5
  --station-regional-support-unsold-floor --station-regional-support-panel-weight=0.085`. Use them on top of the
  HGRPO profile; the launcher is `run_opta.sh` (scratchpad, mirrors `run_5ecal.sh`). Each run is 52 weeks, ~6-9 min,
  3 in parallel.
- **Before every run:** `dotnet build "Label Man.sln" -c Debug`, and byte-compare any new switch unnamed against its
  predecessor.
