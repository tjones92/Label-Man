# Release routes balance follow-up, 1960-64

2026-10-20. Continues [AlbumReleasePracticeClaudeHandoff.md](AlbumReleasePracticeClaudeHandoff.md). Nothing here is a
gameplay default or committed. One new opt-in probe was added (`--radio-acceptance-ramp`); it is inert unless named
(seed-6006 52-week replay: 87/87 CSVs byte-identical to `apr6-opening-6006-52`).

## Summary

The handoff's lead, that ordinary single A&R picks weaker material than the LP route, is real but is **not** what
holds Soul down. Within each song source, Soul singles get the same composition hooks as every other genre. Soul's
deficit is on the radio side: its flat RadioAcceptance of 0.55, sized for a late-decade over-charge, also applies in
1960-64. A ramp that holds Soul at the neutral 1.0 through 1964 and returns to 0.55 by 1966 recovers Soul's chart.

The shared studio formula that failed on the old pipeline is ownership-neutral on the repaired release routes.
Bundled with the Soul ramp, it recovers most of what `apr6` lost in concentration, Big-5 share and slot error, and part
of the share error. The bundle is the best release-practice candidate so far, but it still trails the old universal-promo
reference on whole-catalog share error. The standing misses (Jazz, Folk, TeenPop) predate this work and appear in every arm.

## 1. Material trace: single A&R versus LP cuts

Code path (`Systems/SongMaterialSelectionService.cs`, `CompetitorManager.cs`):

- An orphan single samples six songs per source and keeps the best by `SelectionScore(fit)`: capability, identity
  and moment. It then picks a source by `SourceMix × SelectionScore`. **Commercial hook is never scored.**
- An LP records 9-13 tracks through the same per-track `ChooseMaterial`. The single cut from it is chosen by
  `PromoSuitability = SelectionScore(fit) × Lerp(1, realized quality, .65)`, which does weigh realized hook. The old
  universal promo was therefore a best-of-about-ten commercial pick for every LP act.
- Realized hook is `0.9 × composition hook + 0.1 × performance`. So the performance formula's songwriting term
  barely matters, and the composition source dominates.

`apr6-main`, five seeds, 1960-64 orphan singles, mean composition hook by source:

| Source | Soul | R&B | TeenPop | R&R | TradPop |
|---|---:|---:|---:|---:|---:|
| ExternalProfessional | .694 | .698 | .705 | .670 | .692 |
| ArtistWritten | .529 | .524 | .508 | .514 | .535 |
| CoverRecentHit | .521 | .523 | .542 | .510 | .557 |

Hooks are genre-neutral within source, but Top 10 conversion of professional-song orphans is not: Soul 1.5%,
R&B 5.0%, TeenPop 5.3%, TradPop 6.7%. In 1960-61, at matched hook (~.68) and similar tier mix, Soul singles peak at
radio heat .31, against R&B .40, TradPop .47 and DooWop .48. That holds even at Majors (.45 vs .52). Mean units are about 30% lower.

A hook-aware single A&R (scoring commercial hook within source) is historically plausible, but it would mostly lift
pro-heavy genres. TeenPop is 56% professional and is already over. It is not a Soul or balance lever, so it was not run.

Side observation, not pursued: recent-hit covers average a .52 composition hook. The coverable pool admits any song
that charted, and its sampler scores fit × cover fatigue, never the song's proven appeal.

## 2. Experiments

Common flags: the handoff's reference bundle plus `--genre-hook-audit --album-release-practice`, i.e. `apr6-main`.
Five seeds (1001-5005), 260 weeks, all runs passed the runner's completion checks.

| Arm | Adds |
|---|---|
| `apr7-studio` | `--album-session-production` |
| `apr7-soulra80` | `--radio-acceptance-ramp=Soul@1965=0.80,Soul@1966=0.55` |
| `apr7-soulra100` | `--radio-acceptance-ramp=Soul@1965=1.0,Soul@1966=0.55` |
| `apr8-bundle` | studio + the 1.0 ramp |

The ramp is piecewise-linear in continuous year and flat outside its keyframes. Soul is therefore 1.0 through 1964,
ramps during 1965 and is back at 0.55 from 1966. Late-decade parameters are unchanged; late-decade *outcomes* are not
yet checked.

### Studio formula on the repaired routes

Against `apr6-main`: Big-5 +2.6 / +1.7 / −0.2 / +0.6 / +1.1 and CR8 +0.8 / +1.4 / +2.2 / +2.8 / −2.0 across 1960-64.
All are inside one paired SE. Share error is −0.6 to −0.9 and slot error −3.2 (1960) and −6.8 (1963). LP units are
+17% in 1960, giving an LP unit share of 24.4%, toward the author's ~30% target. The old pipeline's +8 to +12 point
Big-5 blowup came from attaching an advance single to every LP, not from the studio formula itself.

### Soul radio-acceptance ramp

`apr7-soulra100` minus `apr6-main`, paired over seeds (SE):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Soul share | +0.27 (.09) | +0.19 (.28) | +0.66 (.44) | +1.07 (.58) | +1.77 (.38) |
| Soul slots | +1.6 (.2) | +1.0 (.8) | +1.8 (1.0) | +4.4 (1.3) | +5.8 (.4) |
| Slot error | −1.2 (2.1) | −2.8 (2.1) | +0.4 (2.0) | −9.6 (1.7) | −9.6 (1.9) |
| Share error | +0.58 (.66) | −0.11 (.70) | −0.80 (.71) | −1.53 (.94) | −3.48 (1.17) |

R&B and R&R shares dip slightly, toward target. The 0.80 level was weaker and noisier, so it is not a usable
intermediate.

### Bundle

Five-seed means (Big-5 and CR8 are Top 10 slot-week shares; review bands are 48-58 in 1960 and 38-48 after):

| Measure | Arm | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---:|---:|---:|---:|---:|
| CR8 | reference `awrw-on` | 44.12 | 40.23 | 39.04 | 42.00 | 37.62 |
| | `apr6-main` | 42.73 | 39.88 | 34.81 | 36.31 | 39.46 |
| | `apr8-bundle` | 45.77 | 39.50 | 37.77 | 40.35 | 38.15 |
| Big-5 | reference | 30.35 | 25.58 | 26.54 | 26.85 | 22.08 |
| | `apr6-main` | 25.19 | 22.92 | 19.85 | 22.19 | 18.19 |
| | `apr8-bundle` | 29.27 | 24.19 | 24.58 | 25.77 | 20.88 |
| | history | 34.20 | 20.60 | 21.20 | 26.00 | 18.70 |
| Share error | reference | 25.42 | 22.62 | 22.16 | 23.42 | 28.86 |
| | `apr6-main` | 30.86 | 28.72 | 29.57 | 27.06 | 33.50 |
| | `apr8-bundle` | 28.66 | 26.94 | 29.03 | 26.14 | 30.60 |
| Slot error | reference | 32.0 | 39.0 | 38.4 | 45.4 | 35.0 |
| | `apr6-main` | 36.8 | 37.8 | 43.6 | 56.2 | 43.0 |
| | `apr8-bundle` | 37.6 | 35.4 | 40.8 | 45.0 | 37.0 |
| Soul share | `apr8-bundle` | 5.41 | 6.78 | 7.33 | 8.65 | 9.47 |
| | target | 7.92 | 8.86 | 10.24 | 11.53 | 12.20 |

Bundle minus `apr6-main`, paired: Big-5 +4.1 (2.9), +1.3 (2.7), +4.7 (1.3), +3.6 (2.4), +2.7 (2.5); share error
−2.2 (1.6), −1.8 (0.8), −0.5 (1.3), −0.9 (0.9), −2.9 (0.7); slot error −11.2 (2.5) in 1963 and −6.0 (1.9) in 1964.
Summed absolute Big-5 error against history is 14.3 for the bundle, 17.0 for `apr6` and 18.4 for the reference.

Fresh 1960 holdouts, seeds 6006/7007 (`apr8-opening` against `apr6-opening` and reference `ghc2-base`): CR8 55.2 / 50.8
(apr6 49.8 / 45.2, reference 55.6 / 37.3), both inside the 1960 band; Big-5 37.9 / 37.5. Share error 31.3 / 27.4 (apr6
31.6 / 29.7, reference 27.9 / 21.1). Soul is *lower* than apr6 on both holdouts (4.0 / 4.5 against 4.5 / 5.2). The ramp
has little 1960 leverage, so the opening Soul gap remains. Pooling all seven 1960 observations gives CR8 47.8.

All five bundle runs pass the release-route validator (chronology, economics, master identity). Advances remain
about 2% of LP commitments and later extractions 142-158 per run, as in `apr6`.

## 3. What remains

Per-genre share residuals for the bundle, present in **every** arm including the old reference:

- Jazz: 2.0-2.5 against 5.3-6.5, the largest single miss every year (about −4).
- Folk: 8.7-10.4 against 4.5-6.6 (+3 to +5). Folk is ~31% of 1960 LP units in `apr6`.
- TeenPop: +2 to +4 points share, +5 to +8 year-end slots (1962-64).
- R&R: +1.7 to +2.2 share. TradPop: −2.3 to −3.2.
- Soul: still −2.5 to −2.9 share and short of slots in 1960-62.

Jazz and Folk together are 8-9 points of a 26-30 point share error. Both are LP-weighted genres, so the LP-side genre
allocation is the most valuable next lead. It is independent of today's work and was not caused by it.

## 4. Before any adoption

1. Late-decade check: one two-seed decade run of the bundle. The ramp returns to 0.55, but Soul's careers and catalogue
   carry forward. The 0.55 was itself sized against a late over-charge that this would revisit.
2. The handoff's open release-practice limitations still apply: the extra singles slot is not two persistent clocks,
   the prior-single hurdle reads resolved masters, and extraction thresholds are priors.
3. TraditionalPop x1.07 keyframes were sized on the old pipeline. Re-check them on the bundle before stacking more keyframes.

## Reproduction

Plans and manifests: `SimLogs/apr7-studio-plan{,-runs}.json`, `apr7-soulra-plan{,-runs}.json`,
`apr8-bundle-plan{,-runs}.json`. Analyses: `SimLogs/apr8-fiveyear-*`, `apr8-opening-*`, `apr7-fiveyear-*`,
`apr7-identity-check.json`, `apr8-bundle-routes-checks.json`.

```text
python SimTools/analyze_genre_hook_calibration.py --tags awrw-on apr6-main apr7-studio apr7-soulra100 apr8-bundle --seeds 1001 2002 3003 4004 5005 --weeks 260 --output SimLogs/apr8-fiveyear
python SimTools/genre_share_breakdown.py --tags awrw-on apr6-main apr8-bundle --seeds 1001 2002 3003 4004 5005
python SimTools/genre_share_breakdown.py --tags awrw-on apr6-main apr8-bundle --seeds 1001 2002 3003 4004 5005 --metric Slots
```

`genre_share_breakdown.py` is new. It reads each run's `genre-decade-shape` and `year-end-hot100` directly, so genres
that did not chart still count (the calibration analyzer's per-genre rows exist only for charting genres).
