# Handoff: genre hook calibration and the 1960 chart head (2026-10-19)

Full record: `SimTools/ReleaseJudgementDirective.md`, from "Why 1964 runs TraditionalPop/TeenPop long" to the end
of the implementation log. This note is the short version.

## The problem

The Label Man sim's 1960-64 genre chart was off in a patterned way:

- RockAndRoll (R&R) was far short. Market share was ×0.53-0.74 of target and year-end Hot 100 slots ran 4.2
  against 8 in 1964.
- TeenPop and TraditionalPop ran long.
- The Big-5 labels held too much of the 1964 Top 10.

**Root cause: hook strength was genre-biased by the material path, not by talent.**

1. **The artist-written hook formula was low.** It was `0.75*(0.8*craft + 0.1*era) + 0.15*performance`. A median
   writer landed near 0.45, while professional catalogue songs are minted at N(0.68, 0.15).
2. **Rock genres self-write most.** Rock-family singles are about 65% artist-written by the source-mix prior.
3. **No professional publishing office covered RockAndRoll, DooWop, SurfRock or BritishBeat.** Their staff pool
   was empty. That is ahistorical: Hill & Range, Leiber–Stoller, Pomus–Shuman and Aldon all wrote rock & roll.
4. **Result:** R&R's mean hook was 0.44 against Soul's 0.74, even though writer craft is genre-neutral (median
   0.51-0.59 everywhere).
5. **At matched hook, R&R charts as well as TeenPop.** The gap was purely the hooks.

**Author's rule:** a genre's hook level must come from **writer talent × the genre's preference for self-penned
songs**, never a blanket genre cap. Top rock writers must be able to reach staff-grade hooks, but rock's *average*
should not outrank, for example, Soul's.

## What is built (all off by default, all byte-identical when off)

| Flag | What it does | File |
|---|---|---|
| `--artist-written-hook-scale` | Artist-original hook = `0.85*(craft+noise) + 0.15*performance + team`; no genre term | `Systems/ArtistWrittenHookScale.cs`, `SongMaterialSelectionService.BuildArtistWritten` |
| `--rock-hit-writers` | R&R/DooWop/RnB staff office; R&R source anchor 1960 pro .35 → 1969 .15; R&R pro availability 0.60 → 0.30 | `Systems/RockHitWriters.cs`, `CompositionCatalogService`, `SongMaterialSelectionService` |
| `--genre-keyframe-scale=G@year=f,...` | Scales one demand baseline keyframe | `Data/GenreCatalog.cs` |
| `--radio-acceptance=G=v,...` | Replaces a genre's RadioAcceptance multiplier | `Data/GenreCatalog.cs` |
| `--publisher-pitch-order` | Majors see the top 40% of a genre's staff catalogue by hook, MidTier the top 70%, others the bottom 80% | `Systems/PublisherPitchOrder.cs` (screen pending) |

**Current candidate bundle:** hook scale + rock hit writers + `TraditionalPop@1960=1.07,TraditionalPop@1962=1.07`.
Against the old control it:

- puts R&R's 1962-64 market share on history (11.5 / 9.9 / 7.6 against 11.5 / 10.0 / 6.8);
- puts TradPop's share within about 1 point every year;
- cuts genre share error in most years and 1964 slot error from 47 to 35;
- brings R&R's hook to 0.60 and DooWop's to 0.61 (Soul 0.77, TradPop 0.73).

## What still needs fixing

1. **The 1960 Top 10 head is below its floor.**
   - Top 10 CR8 (top-8 labels' share of Top 10 slot-weeks) is 44 against a historical band of 48-58. The old
     control was 48.1.
   - Big-5 Top 10 share is 30 against history's 34.2.
   - Cause: better R&R material lets independent R&R acts take 1960 Top 10 weeks.
   - The staff office does move 1960 R&R Top 10 weeks to majors (17% → 30%). But labels barely differ in the
     professional songs they get (Major R&R hook 0.619 against Independent 0.596), so the overall head does not
     recover.
   - `--publisher-pitch-order` is the attempt at this. Expected risk: it may push the 1961-63 Big-5 share, already
     about 3-5 over history, further up.
2. **R&R overshoots in 1960-61 and over-charts at a correct share.**
   - 1960-61 share is 16.0 / 13.7 against 13.5 / 12.8.
   - Slots run +1 to +5 over history across 1960-64, even where share is right.
3. **Soul is short throughout** (1964: 10.2 share against 12.2; 8 slots against 14).
   - The likely cause is Soul's flat RadioAcceptance 0.55, sized for a late-decade over-charge. It is untouched here.
4. **TeenPop is long** (1964: 9.1 share against 7.9; 11.6 slots against 5).
   - Its 1964 keyframe rises (.42 → .46) while history collapses.
   - Hypothesis: the hand count may file girl groups as Soul, while the model maps GirlGroup → TeenPop.
5. **Audit items not yet acted on:**
   - Songbook quality constants are genre-coded: Tin Pan Alley .68 against Blues .58, R&B Catalog .58, Gospel
     .56 and Folk Traditional .50.
   - The polar execution penalty seems to tax Soul, Folk, BritishBeat, Blues and Gospel by about 10-16% on hook
     and production, but not TeenPop, R&B, R&R or DooWop. It is step-shaped, the mechanism is untraced, and it is
     visible as a production gap within Independent labels.

## Tried and rejected

- **R&R RadioAcceptance 1.10 → 1.00.**
  - It did reach the records (R&R radio heat −6%) but did not move R&R's chart: 1960-64 chart points are mostly
    sales.
  - It drained Soul through the shared Urban/R&B segment (1964 Soul −1.5 share, −4.6 slots, 0/5 seeds up).
- **R&R 1960 keyframe ×0.92.**
  - 1960 share fell only halfway (16.0 → 14.9).
  - It caused a relative supply surge in 1962-64 (+1.1 to +1.3 share, more slots), which the `GenreCatalog`
    comment history had already warned about. Any fix has to be flatter (1960 and 1962 together) or act on the
    professional anchor itself.
- **Weekly label-push levers (radio push spread 0.5 to 0.0).** They do not move 1961-64 once launch saturation is
  on, and a spread of 0 breaks 1960.

## Rules and method

- **Benchmarks:**
  - Market share targets: `SimTools/AdjustedHistoricalGenreShareTargets.csv`. High confidence, and it wins on
    demand baselines.
  - Year-end Hot 100 slot hand count: `ChartSlotBenchmark` in `Data/GenreCatalog.cs`. A guide, ±1-2 slots of noise.
  - Big-5 Top 10 share history 1960-64: 34.2 / 20.6 / 21.2 / 26.0 / 18.7.
- **A genre that charts wrongly at a correct market share is a chart-side defect.** Do not fix it with a keyframe.
- **Share goes as baseline²,** so size a keyframe change by √(target/current).
- **RadioAcceptance is weak in 1960-64**, where airplay is a small share of chart points, and it spills across
  genres that share a segment.
- **Screens:** five paired seeds (1001-5005), 260 weeks (1960-64), about 9 minutes a run, four in parallel.
  - Single-seed deltas under about 3-5 points are noise.
  - Any code change reshuffles the RNG stream, so read paired deltas with their SE.
  - Base flags are in `launch_awpp.py` in the session notes folder: the F = 0.75 launch saturation + radio push
    spread 0.1 control.
  - The control runs are `SimLogs/rj2asp10-ctl-*`; the current bundle is `SimLogs/awrw-on-*`.
- **New flags must be byte-identical when off** (hash every CSV against a reference run). They must also not
  change GD RNG draw order when off.
- **Decade runs take about 2.5 hours.** Do one two-seed decade A/B at the end, not per fix. Self-writing reaches
  76% of rock by 1969, so the late decade is where the hook scale matters most.

## The ask

Find a historically grounded way to restore the 1960 head (CR8 ≥ 48, Big-5 near 34) and trim R&R's 1960-61
overshoot, without:

- breaking the hook-scale principle (no genre caps);
- giving back the 1962-64 R&R and TradPop share gains;
- pushing the 1961-64 Big-5 share further above history.
