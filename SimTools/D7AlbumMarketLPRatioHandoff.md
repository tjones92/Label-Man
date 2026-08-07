# D7 album-market LP:45 recalibration — session handoff

Opened August 5, 2026. Branch `d7-genre-decade-calibration`. Parent:
`D7RadioAcceptanceLeverHandoff.md` (radio chart-efficiency work; its step-1 corrective is folded
into the same commit as this work).

## 0. One-line brief

The album market was structurally broken — LP unit share ~1.4% in 1960 (target ~30%) and pinned near
23% at 1969 (target 55%) in **every** run back to the old d6 builds. Root cause found and fixed: the
album **retail-fulfillment channel capacity** at market clearing (not demand, not the format
decision) was the binding constraint, gated to zero before ~1964. **The fix is committed and pushed
(`5a00eef`).** Two follow-ups remain, to be done **together in one decade run** (author accepts the
provenance ambiguity of combining them): a **demand pullback** (runtime) and **prewarm album seeding**
(1960 cold-start).

## 1. What is already done (committed `5a00eef`, verified, probes green)

The album unit share is now driven by the album channel capacity curve, which the demand lift
saturates. Verified over one decade (seed 1001):

| year | LP unit% (model) | target | LP rev% | target |
|---|---:|---:|---:|---:|
| 1960 | 19.6 (cold-start) | 30 | 52.1 | 60 |
| 1963 | 40.8 | 35 | 75.5 | 65 |
| 1965 | 46.3 | 40 | 79.4 | 75 |
| 1967 | **50.8** | 50 | **82.2** | 82 |
| 1969 | **54.7** | 55 | 84.4 | 90 |

Late years nail the targets; early-mid runs ~6 pts hot; 1960 is a first-year cold-start (task B).
**Singles-chart payoff:** year-end slot error vs the hand count (RnR excluded) **301 → 230** — Jazz
and Classical leave the Hot 100 as their demand flows to albums; Soul control holds (173 vs hand
179); TraditionalPop rises, which the hand count *wants* (it is the dominant early singles genre).

### Levers as committed (know these before touching anything)
- **Master lever — album channel capacity** (`Systems/ChartManager.cs` ~line 75):
  `MatureAlbumChannelBaselineShare = 0.17f`, `MatureAlbumChannelEraExpansionShare = 1.30f`. Channel =
  `baseCapacity × maturity × (baseline + expansion × GetAlbumDemandEraProgress)`. Because album intent
  saturates the channel from 1961 on, **album unit share ≈ channel share ≈ this curve**. Read against
  `GetAlbumDemandEraProgress` (rise 1957→1972 ⇒ era ~0.20 at 1960, ~0.80 at 1969).
- **Retail maturity** (`Systems/AlbumModel.cs`): `GetRetailFulfillmentMaturity => 1f` (was a step that
  zeroed the channel before ~1964). The pre-mature clearing branch in ChartManager
  (`AlbumIntentOverlapPressure = 2`) is now dead code (maturity never < 1).
- **Demand lift** (so intent saturates the channel), `Data/MarketRegion.cs`:
  `GetAlbumPurchaseWillingness` base `.45f`, youth penalty `Lerp(.55f,.30f,aging)`;
  `ShapeAlbumAffinity` uses a **multiplicative** `eraBoost = 1 + 1.5×era` (was an additive decadeLift
  that compressed genre spread) with youth penalty `Lerp(.40f,.12f,era)`; `albumDemandRiseStartYear =
  1957f`. `Systems/AlbumSimulator.cs` `BasePurchaseRate = 0.045f` (single side is `0.07f`).
- **Genre album affinity table** `GetAlbumAffinityBaseline` (`MarketRegion.cs`): EasyListening .88,
  Classical .82, PsychedelicRock/Folk .78, Jazz/BossaNova .72, etc.

## 2. TASK A — demand pullback (runtime). Do NOT test on a 1960-only run.

**Problem:** the demand lift over-produces album *titles* — 91% of late release decisions, ~18.6k
active albums by 1969 → **~90-minute decade runs** (vs ~34 min pre-album-work). Author's priority is
to cut this; accuracy comes first, but runtime should improve.

**Why demand, not the decision levers, is the tool:** the channel caps album *units*, so cutting
album *titles* does not cost unit share — fewer albums each take a bigger slice of the same channel,
as long as remaining intent still saturates it. Album intent was ~62M against a channel of ~30M in
1960 (measured, task-A sizing below), i.e. **~2× more demand than the channel needs**. Pull the demand
lift back toward just-saturating the channel. Candidate direction: `BasePurchaseRate` .045→~.030,
willingness base .45→~.36, `eraBoost` 1.5→~1.1 — but **size it against the channel, then verify**.

**CRITICAL GOTCHA that wasted a cut this session:** a 52-week run only simulates **1960**, which is a
cold-start year where the album catalog is still filling and **units follow demand, not the channel**.
A 1960-only pullback test therefore shows unit share falling (it did: 19.6→15.8) and reads as a
failure, when 1961+ (channel saturated) would hold. **Task A must be verified over a full decade**, or
on 1961+ only. The earlier `cut3` attempt (willingness .45→.38, BasePurchaseRate .045→.035) was both
too timid and cold-start-tested; ignore its result.

**Decision-only levers exist but are traps here** (`Systems/CompetitorManager.cs`):
`AlbumPortfolioCommitmentCeiling = 1.50f` exists specifically to stop labels ABANDONING albums as the
LP market matures (see the `DecideRelease` comment ~line 1956) — cutting it risks the opposite
failure, and a probe asserts `GetAlbumPortfolioCommitmentMultiplierForProbe(1,24,1965) > 2f`.
`AlbumPriorEarlyEraDiscount = .78f` only bites 1961-63 (guarded by
`AlbumPriorCalibrationBootstrapYear=1960`/`RetiredYear=1964`). Both were tried and reverted this
session. Prefer the demand pullback.

## 3. TASK B — prewarm album seeding (1960 cold-start)

**Problem:** `CompetitorManager.PopulateInitialRecords` (~line 622-654) seeds the opening catalog as
**100% singles** — `GenerateRecordFromArtist(label, artist, year)` defaults to
`ReleaseFormat.Single` and every seeded record is added to `artist.releasedSingleIds`. So the album
catalog is empty at week 1 and takes ~a year to fill (active albums double 2,629→5,396 into 1961),
which is why 1960 reads 19.6% instead of the ~30% steady-state the channel would give.

**Fix:** seed a fraction of the initial quota as albums — the pre-existing 1960 LP catalog (jazz,
classical, mood/MOR, Broadway) — via `GenerateRecordFromArtist(label, artist, year,
ReleaseFormat.Album)`. Care needed: albums must get album-chart placement + initial stock, must NOT be
added to `releasedSingleIds`, and `BootstrapPrewarmRecord` (~line 680) is single-oriented (sets
radioHeat etc.) — verify it does something sane for albums or branch it. Do NOT chase 1960 by raising
baselines/channel share — that would push the already-hot 1961-65 higher.

## 4. Also-worth-fixing while in there (secondary, verify against the hand count)
- **EasyListening over-corrected:** hand count wants EL *on* the singles chart (decade sum 52), model
  now 28 — its 0.88 album affinity over-routes it. Unlike Jazz (hand ~0), EL kept a real singles
  presence. Lower `GetAlbumAffinityBaseline(EasyListening)` .88 → ~.65 so it keeps singles.
- **Early-mid LP ~6 pts hot** (1963 40.8 vs 35): small `EraExpansionShare` trim, but the target curve
  is S-shaped (gentle 60-65, steep 65-67, gentle 67-69) and the era curve is ~linear, so do not
  over-fit — a few points is fine.
- **1969 rev share 84.4 vs 90:** minor; the ~4.5× album price ratio makes revenue follow units, so
  hitting the unit curve gets rev close. Leave unless it drifts.

## 5. How to run and score (no re-derivation needed)

**Environment (parent §1a):** Godot 4.7-mono needs the x64 .NET 8 runtime; if `godot --headless
--quit` segfaults at .NET init after any runtime change, **reboot first**. Paths (bash):
- Godot: `/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`
- Python: `/c/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe` (bash `python` is not on PATH; Import-Csv dies >50MB, use pandas)
- Build: `dotnet build "Label Man.sln" -v minimal`

**Probes (both must pass), 52 weeks, ~1 min:**
```
"$GODOT" --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=<name> --seed=1001 \
  --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe \
  --genre-market-v2-probes --artist-population-lifecycle-probes
```
**Decade run (522 weeks):** drop the two `--probes` flags, keep `--lean-probe --profile-performance`,
redirect to an explicit log path (`$TMPDIR` is empty in the Bash tool; a bare redirect silently
fails). Runtime is currently ~90 min — task A should cut it.

**Album-clearing diagnosis (the instrument that found the bug):** a **non-lean** run (drop
`--lean-probe`) emits `<run>-album-realization-bridge.csv` (~97 MB for 52 weeks) with the full
per-record-region-week chain buyerPool→awareness→conversion→rawDemand→serviceableIntent→**finalCleared**
and **marketDisplacedDemand**. Sum finalCleared/serviceable to see the clearing loss.

**Scoring (Python on existing CSVs, no run):**
- LP share: `<run>-decade-annual-rollup.csv` cols `singleUnits, albumUnits, singleGross, albumGross,
  albumDecisionShare, activeSingles, activeAlbums`. `albumUnits/(single+album)` = LP unit share.
- Singles chart: `<run>-year-end-hot100.csv` col `yearEndSlots`, scored decade-summed vs the hand
  count in §6, `sum |model − hand|`, **RockAndRoll excluded** (misclassification caveat). Best so far
  230; beat it.

## 6. Reference tables

**Author LP:45 targets** (rough shape). LP UNIT share 1960/63/65/67/69 = **30/35/40/50/55%**; LP REV
share = **60/65/75/82/90%**. Interpolate between. NARM rack-jobber rev share 1963-66 was ~78-83%
albums. Album units are ~1.4% of the market in 1960 in the OLD (broken) model.

**Hand-counted year-end Hot 100 slots** (from `D7GenreChartDivergenceHandoff.md` §3; slots out of
~100/yr, 1960→1969):

| genre | 60 61 62 63 64 65 66 67 68 69 |
|---|---|
| Soul | 6 9 7 15 14 22 22 28 28 28 |
| TraditionalPop | 22 16 15 20 14 11 8 9 12 7 |
| TeenPop | 28 18 20 8 5 7 6 9 2 2 |
| RnB | 12 18 28 21 10 2 3 0 0 0 |
| RockAndRoll (excluded) | 14 12 12 11 8 3 6 6 5 2 |
| Country | 9 7 7 8 2 1 2 2 1 3 |
| DooWop | 7 10 6 3 0 0 0 1 0 0 |
| EasyListening | 5 8 8 5 3 4 5 0 6 8 |
| SurfRock | 3 2 1 6 9 2 3 0 0 1 |
| Comedy | 3 3 2 3 1 0 0 2 0 1 |
| Folk | 1 1 3 7 3 2 1 2 2 0 |
| BritishBeat | 0 0 0 0 24 15 8 3 3 0 |
| BritishPop | 0 0 0 0 2 12 6 7 3 4 |
| FolkRock | 0 0 0 0 0 12 14 6 5 3 |
| GarageRock | 0 0 0 0 3 5 12 4 1 1 |
| BritishBlues | 0 0 0 0 0 5 3 2 0 0 |
| SunshinePop | 0 0 0 0 0 0 4 10 4 12 |
| PsychedelicRock | 0 0 0 0 0 0 1 9 10 6 |
| Bubblegum | 0 0 0 0 0 0 0 0 7 9 |
| Funk | 0 0 0 0 0 1 0 1 4 6 |
| HardRock | 0 0 0 0 0 0 0 1 5 3 |
| Jazz | 0 1 1 0 1 1 0 0 1 2 |
| CountryRock | 0 0 0 0 0 0 0 0 0 6 |
| Classical | 0 0 0 0 0 0 0 0 0 0 |

## 7. Do not redo
- Do not tune album UNIT share with demand knobs — the channel-share curve (§1) is the master lever.
- Do not test a demand pullback on a 1960-only (52wk) run — cold-start confounds it (§2).
- Do not cut `AlbumPortfolioCommitmentCeiling` to reduce titles — it guards against late album
  abandonment and trips a probe (§2). Use the demand pullback.
- Do not raise baselines/channel to fix 1960 — seed the prewarm catalog instead (§3).
- Do not remove `AIRPLAY_CONVEXITY` or the radio-acceptance split (parent doc).
