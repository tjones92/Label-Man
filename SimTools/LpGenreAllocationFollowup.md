# LP-side genre allocation and decade check

2026-10-20. Continues [ReleaseRoutesBalanceFollowup.md](ReleaseRoutesBalanceFollowup.md) §3. Base: `apr8-bundle`.
Nothing here is a gameplay default. One opt-in probe was added (`--album-affinity=Genre=value,...`), inert unless
named (seed-6006 52-week replay: 87/87 CSVs byte-identical to `apr8-opening-6006-52`).

## Summary

The Jazz deficit and the Folk surplus have one origin: the per-genre album affinity table
(`MarketRegion.GetAlbumAffinityBaseline`). The August album-chart pass cut the album-heavy genres against the author's
year-end album estimates (Jazz 0.90 → 0.20, Easy Listening 0.88 → 0.34, TradPop 0.72 → 0.36, Classical, Country,
Comedy). **Folk (0.78) and Bossa Nova (0.72) were never re-tuned.** They are still at their July 2026 values, which
now makes them the two highest early album affinities. They absorb the LP share the other cuts freed. The V3 handoff
noticed it in passing ("Folk (~22%, untargeted) eats a lot").

- **Folk:** both of the author's sources agree it is about 2x over on LPs. The model gives it 31% of 1960 LP units
  and 60% of the year-end album top 50; the share table implies about 11% and `albumchart.odt` counts 13%. Cutting
  its affinity is supported by both benchmarks.
- **Bossa Nova:** the same untouched-absorber pattern from 1963 (+1.2 to +1.9 points of share; 11-16% of the album
  head against 2-4%).
- **Jazz:** the two sources disagree by roughly 8x. The share table needs Jazz at 13-19% of LP units, while the album
  estimate puts Jazz at 0-4% of the year-end top 50. Affinity moves units and album-chart presence together, so it can
  satisfy only one of them. **This is the one decision that needs the author.**

Best candidate so far: `apr8-bundle` + `--album-affinity=Folk=0.48,BossaNova=0.40,Jazz=0.30` (share error −5 to −9
every year 1960-64 on five seeds; −49 over the decade on each of two seeds). Adding TeenPop and Doo-Wop keyframe
trims (`lpa4-youth`, §3) gives the best arm found: decade share error 260 against the bundle's 332 and the pre-bundle
reference's 305, slot error 500 against 512, and CR8 back to the reference.

The decade check also found a problem with the bundle itself. Release practice's era-neutral priors leave late-decade
LP acts almost no route to a single (about 30 extractions per seed per year), and Big-5 Top 10 share in 1965-69 falls
4-7 points below the pre-bundle reference on both seeds. That is the next thing to fix before the bundle can be
adopted (§5).

## 1. Decomposition (`apr8-bundle`, five seeds)

LP titles are format decisions that chose Album; units are from `genre-format-units`.

| 1960 | LP titles % | LP units % | units / LP title (× mean) | album choice p | shaped affinity |
|---|---:|---:|---:|---:|---:|
| Folk | 15.7 | 31.2 | 2.0 | 0.79 | 0.89 |
| TraditionalPop | 17.6 | 21.0 | 1.2 | 0.56 | 0.41 |
| Easy Listening | 10.3 | 14.3 | 1.4 | 0.62 | 0.39 |
| Soul | 13.9 | 4.6 | 0.3 | 0.76 | 0.63 |
| Jazz | 5.9 | 3.4 | 0.57 | 0.36 | 0.23 |

- Per-title units track `affinity × routed acceptance`: the album buyer pool is
  `buyingPop × segregation × acceptance × affinity × willingness`. The Folk/Jazz per-title ratio (3.5x) is almost
  exactly their affinity ratio (3.9x).
- Affinity also sets the format decision through the album prior. Jazz's album margin (4,575) is below its single
  margin (5,385), so 64% of jazz releases are singles. That is backwards for 1960 jazz. Folk's album margin is 2.9x
  its single margin.
- Folk owns the head: 28% of Folk LPs sell above the pooled 90th percentile, and Folk is about three quarters of all
  albums above the 99th. Jazz has none above the 99th.
- Supply is not the binding term. Jazz is 4.9% of all releases and 4.5% of artist identities (target share 6.2%); it
  converts at 0.50x per release. Folk is 7.1% of releases and converts at 1.31x.

## 2. The author's two benchmarks side by side

"Implied LP %" = (share target − the model's own singles points) / LP unit share, i.e. the LP share each genre needs
to hit the share table without changing its singles. The chart column is `albumchart.odt` (year-end top 50).

| | implied LP % 1960-64 | model LP % (`apr8-bundle`) | album top-50 est % |
|---|---|---|---|
| TraditionalPop | 30 / 30 / 30 / 24 / 19 | 21 / 21 / 20 / 18 / 16 | 30 / 28 / 24 / 36 / 28 |
| Folk | 11 / 10 / 12 / 14 / 10 | 31 / 29 / 26 / 20 / 14 | 13 / 6 / 18 / 18 / 13 |
| Jazz | 19 / 17 / 16 / 15 / 13 | 3.4 / 3.6 / 3.1 / 3.0 / 2.3 | 0 / 4 / 2 / 2 / 2 |
| Easy Listening | 17 / 15 / 16 / 13 / 13 | 14 / 14 / 12 / 11 / 9 | 6 / 12 / 10 / 2 / 9 |
| Comedy | 2.9 / 2.5 / 2.4 / 1.9 / 1.7 | 4.2 / 4.8 / 4.1 / 3.7 / 2.9 | 11 / 6 / 6 / 8 / 3 |
| Bossa Nova | – / 0.4 / 3.0 / 3.2 / 3.1 | – / – / 2.6 / 6.6 / 8.1 | – / – / 2 / 4 / 2 |

TradPop and Folk read the same on both benchmarks (chart share ≈ LP unit share). Jazz's chart share is about a tenth
of its implied unit share; Comedy's is about three times. The share workbook itself calls its values "reasoned
estimates", built from chart behaviour and scaled to 100%, and leaves open whether "share" means units or revenue.

The model has no album chart-efficiency term: album chart presence tracks album units directly (no airplay channel).
So the Jazz conflict is the album-side instance of the missing chart-efficiency dimension already recorded for
singles (a genre's chart presence per point of unit share is fixed by its units, not authored).

## 3. Experiments

Common flags: `apr8-bundle`. Five seeds (1001-5005), 260 weeks; all runs passed the runner's completion checks.

| Arm | `--album-affinity=` |
|---|---|
| `lpa-folk40` | Folk=0.40 |
| `lpa-fj40` | Folk=0.40,Jazz=0.40 |
| `lpa2-fb` | Folk=0.48,BossaNova=0.40 |
| `lpa2-fbj30` | Folk=0.48,BossaNova=0.40,Jazz=0.30 |
| `lpa2-fbj40` | Folk=0.48,BossaNova=0.40,Jazz=0.40 |

Authored values: Folk 0.78, Bossa Nova 0.72, Jazz 0.20. Shaped affinity is `baseline × (1 − youth penalty) × era
boost`, clamped to 1, so Folk and Bossa Nova were clamped from 1962. Folk 0.40 was a first sizing; it overshot both
the album head (Folk 7% of the 1960 top 50) and the late units, so round 2 used 0.48.

### Share error and its components (five-seed means; paired delta vs `apr8-bundle`, SE)

| | 1960 | 1961 | 1962 | 1963 | 1964 | sum |
|---|---:|---:|---:|---:|---:|---:|
| `apr8-bundle` | 28.66 | 26.94 | 29.03 | 26.14 | 30.60 | 141.4 |
| `lpa-folk40` | 26.17 | 21.02 | 23.70 | 22.73 | 29.82 | 123.4 |
| `lpa-fj40` | 20.41 | 18.15 | 20.86 | 20.43 | 30.39 | 110.2 |
| `lpa2-fb` | 24.94 | 20.75 | 23.16 | 22.54 | 28.27 | 119.7 |
| `lpa2-fbj30` | 23.57 | 19.60 | 20.03 | 20.40 | 25.70 | **109.3** |
| `lpa2-fbj40` | 24.58 | 19.47 | 21.43 | 19.20 | 26.89 | 111.6 |
| `fbj30` delta | −5.1 (1.3) | −7.4 (1.1) | −9.0 (1.1) | −5.7 (1.1) | −4.9 (1.4) | |

| Share % | target | `apr8-bundle` | `lpa2-fb` | `lpa2-fbj30` | `lpa2-fbj40` |
|---|---|---|---|---|---|
| Folk | 4.5 / 5.0 / 5.7 / 6.6 / 5.2 | 9.4 / 10.4 / 10.3 / 8.7 / 6.6 | 5.6 / 6.1 / 6.6 / 5.8 / 4.6 | 5.4 / 5.8 / 5.9 / 5.7 / 4.4 | 5.8 / 5.8 / 6.3 / 5.4 / 4.2 |
| Jazz | 6.2 / 6.5 / 6.3 / 6.1 / 5.3 | 2.5 / 2.5 / 2.0 / 1.8 / 1.4 | 2.7 / 2.6 / 2.3 / 2.0 / 1.4 | 3.9 / 3.9 / 3.6 / 3.2 / 2.3 | 5.2 / 6.0 / 5.2 / 4.4 / 3.2 |
| Bossa Nova ('62-64) | 1.1 / 1.5 / 1.7 | 1.2 / 2.8 / 3.6 | 0.5 / 1.8 / 2.2 | 0.9 / 1.9 / 2.2 | 0.7 / 1.8 / 2.5 |
| TradPop | 15.0 / 15.0 / 14.5 / 13.2 / 11.0 | 12.7 / 12.2 / 11.4 / 11.3 / 9.9 | 13.6 / 13.9 / 13.0 / 12.7 / 10.9 | 13.8 / 13.0 / 12.9 / 12.5 / 10.8 | 13.2 / 12.9 / 12.1 / 12.1 / 10.3 |
| LP unit share | ~30 at 1960 | 24.2 / 29.7 / 32.2 / 34.8 / 37.8 | 21.0 / 28.2 / 31.6 / 34.5 / 37.5 | 21.9 / 28.7 / 31.9 / 34.7 / 37.7 | 23.2 / 29.4 / 32.1 / 34.8 / 37.7 |

Folk's surplus goes mostly to TradPop, which is under on both benchmarks, and to Easy Listening and Soul. The 1960 LP
market is still demand-bound, not channel-bound: removing Folk's excess LP demand lowers 1960 LP unit share
(−3.2 with Jazz unchanged), and Jazz demand restores it (−2.3 at 0.30, −1.0 at 0.40). Swapping Folk's LP demand for
Jazz's leaves the LP total about where it was.

### Head of the album chart

Proxy: per release year, the 50 albums with the most lifetime units (five seeds). It reads Folk at 61% of 1960 in
`apr8-bundle`; an older full-telemetry run's real year-end album chart (`ab5ctl-2002`) reads 60%.

| Top-50 % | est | `apr8-bundle` | `lpa2-fb` | `lpa2-fbj30` | `lpa2-fbj40` |
|---|---|---|---|---|---|
| Folk | 13 / 6 / 18 / 18 / 13 | 61 / 46 / 46 / 36 / 26 | 18 / 12 / 9 / 8 / 7 | 16 / 15 / 16 / 13 / 8 | 15 / 12 / 12 / 13 / 8 |
| Jazz | 0 / 4 / 2 / 2 / 2 | 0 everywhere | 0 everywhere | 9 / 4 / 3 / 0 / 1 | 27 / 19 / 13 / 11 / 10 |
| TradPop | 30 / 28 / 24 / 36 / 28 | 13 / 11 / 8 / 12 / 17 | 40 / 21 / 22 / 28 / 30 | 32 / 20 / 24 / 22 / 30 | 24 / 16 / 21 / 24 / 28 |
| Bossa Nova | – / – / 2 / 4 / 2 | – / – / 4 / 10 / 12 | – / – / 0 / 4 / 3 | – / – / 3 / 4 / 2 | – / – / 2 / 3 / 5 |
| Easy Listening | 6 / 12 / 10 / 2 / 9 | 16 / 10 / 9 / 10 / 10 | 24 / 24 / 22 / 16 / 16 | 24 / 21 / 16 / 16 / 11 | 21 / 16 / 10 / 12 / 10 |

Real year-end album chart, seed 1001, full-telemetry reruns (`lpafull-*`; the control's economy CSVs are
byte-identical to `apr8-bundle-1001`):

| Top-50 % | est | control | Folk .48 + BN .40 | + Jazz .30 | + Jazz .40 |
|---|---|---|---|---|---|
| Folk | 13 / 6 / 18 / 18 / 13 | 56 / 46 / 60 / 30 / 18 | 18 / 22 / 28 / 18 / 12 | 12 / 22 / 18 / 14 / 8 | 16 / 12 / 22 / 14 / 10 |
| Jazz | 0 / 4 / 2 / 2 / 2 | 0 | 0 | 16 / 14 / 12 / 8 / 0 | 22 / 32 / 20 / 20 / 6 |
| TradPop | 30 / 28 / 24 / 36 / 28 | 12 / 16 / 8 / 16 / 4 | 20 / 24 / 20 / 34 / 38 | 14 / 18 / 28 / 16 / 18 | 10 / 18 / 18 / 22 / 6 |
| Bossa Nova | – / – / 2 / 4 / 2 | 0 / 0 / 10 / 12 / 18 | 0 / 0 / 4 / 2 / 4 | 0 / 0 / 0 / 2 / 4 | 0 / 0 / 4 / 10 / 8 |
| Easy Listening | 6 / 12 / 10 / 2 / 9 | 12 / 24 / 12 / 10 / 4 | 30 / 30 / 18 / 10 / 6 | 26 / 24 / 28 / 20 / 16 | 20 / 20 / 12 / 6 / 10 |

The real chart reads Jazz somewhat above the proxy, so at 0.30 Jazz is already over the estimate in 1960-62. Jazz's
chart presence is strongly convex in affinity (0.20: none; 0.30: ~10%; 0.40: ~25%), while its unit share is roughly
linear (+1.4 points, then +1.3 more).

### Concentration and slots

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| CR8 `apr8-bundle` | 45.8 | 39.5 | 37.8 | 40.4 | 38.2 |
| CR8 `lpa2-fbj30` | 45.7 | 37.0 | 35.4 | 35.8 | 38.8 |
| delta (SE) | −0.1 (2.6) | −2.5 (1.3) | −2.4 (1.9) | −4.6 (2.6) | +0.7 (1.7) |
| Big-5 `apr8-bundle` | 29.3 | 24.2 | 24.6 | 25.8 | 20.9 |
| Big-5 `lpa2-fbj30` | 28.5 | 21.9 | 19.6 | 22.0 | 20.4 |
| Big-5 history | 34.2 | 20.6 | 21.2 | 26.0 | 18.7 |
| Slot error `apr8-bundle` | 37.6 | 35.4 | 40.8 | 45.0 | 37.0 |
| Slot error `lpa2-fbj30` | 31.6 | 35.4 | 40.4 | 47.0 | 36.6 |

Every arm with the Folk cut lowers 1961-63 CR8 and Big-5 by about one SE (`lpa2-fb`: Big-5 −3.7 and −5.4 in
1962-63). That puts 1961-63 CR8 at or just under the 38 floor. The likely channel is Big-5 LP profit. In
`apr8-bundle` the Big-5 schedule only 6% of Folk LP titles (76 of 1,193 per seed, 1960-63) but earn 14.2M of their
47.0M LP net from Folk, about 30%. `lpa2-fb` cuts Big-5 LP net 11% (its Folk net halves); Jazz 0.30 returns part of
it (−6%). That is historically plausible (the Kingston Trio, Belafonte and the Brothers Four were major-label acts),
but it means an LP-side genre move reaches the singles concentration benchmarks through label cash. The cash-to-push
link itself was not traced. Slot error is unchanged within noise; Jazz and Folk hold almost no singles slots in any
arm.

### Decade check (seeds 1001/2002, 522 weeks)

`dec-lpa-fbj30` (Folk .48, Bossa Nova .40, Jazz .30) against `dec-apr8-bundle`; two-seed means and paired deltas.
`analyze_genre_hook_calibration.py` now accepts 53-week sim years and skips a run's trailing partial year (it rejected
1965 before).

| | 1960 | 1961 | 1962 | 1963 | 1964 | 1965 | 1966 | 1967 | 1968 | 1969 | sum |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Share error, bundle | 28.5 | 25.3 | 27.7 | 25.5 | 30.6 | 31.7 | 35.4 | 38.6 | 43.6 | 45.5 | 332.4 |
| Share error, candidate | 24.8 | 18.7 | 19.7 | 19.8 | 27.0 | 27.7 | 32.7 | 35.2 | 37.9 | 40.3 | 283.8 |
| Slot error delta | −5 | −1 | +4 | +7 | +6 | +4 | +4 | +4 | −6 | −4 | +13 |
| CR8 delta | −3.3 | −4.0 | −2.1 | −5.8 | +3.7 | −2.5 | +3.6 | +3.1 | −1.2 | +1.4 | |
| Big-5 delta | −6.2 | 0.0 | −3.5 | −7.0 | +3.4 | −1.5 | +6.2 | −2.1 | +0.3 | −2.1 | |
| Jazz share delta | +1.8 | +1.3 | +1.5 | +1.5 | +0.8 | +0.7 | +0.7 | +0.7 | +0.7 | +0.4 | |
| Bossa Nova delta | | | | −0.9 | −1.6 | −2.0 | −1.8 | −0.6 | −0.5 | | |
| Soul share delta | +0.4 | +0.8 | +1.3 | +0.9 | +0.7 | +1.0 | +0.6 | +2.5 | +2.2 | +3.2 | |
| Soul slots delta | +1 | −1 | +1 | +1 | −0.5 | +2 | +1.5 | +6 | +4 | +2 | |

- Share error falls by 49 points on **each** seed (359 → 310, 306 → 257); that replicates.
- Slot error does not replicate in direction (−4 on 1001, +30 on 2002) and is inside the known decade slot noise.
- The 1961-63 CR8/Big-5 dip seen on five seeds appears on both decade seeds; late concentration is mixed.
- Late Soul moves toward its share target (17.2 against 17.5 at 1969) and up to 30-33 year-end slots against 28.
  The freed Folk and Bossa Nova LP share reaches Soul's high (.55, clamped late) album affinity.
- Jazz stays 2-3 points under all decade; Folk and Bossa Nova are fixed and fade on schedule.

The bundle itself (the earlier follow-up's late-decade question): Soul year-end slots are on benchmark
(20 / 21.5 / 26.5 / 27.5 / 28.5 against 22 / 22 / 28 / 28 / 28 for 1965-69). The Soul ramp's return to 0.55 does not
create a late over-charge. Its late share is under (14.0-14.8 against 16.0-18.5). The largest late misses in both arms
are TeenPop (about 2.5x its share target and +10 slots in 1965-66), Easy Listening share (+4 to +5, 1967-69), Folk
Rock (0 slots against 12-14 in 1965-66) and Jazz.

### Reference decade: what the bundle itself does late

`dec-awrw-on` is the pre-bundle reference (`awrw-on` flags only), run on the same two seeds.

| Decade sum, two seeds | `dec-awrw-on` | `dec-apr8-bundle` | `dec-lpa-fbj30` |
|---|---:|---:|---:|
| Share error | 304.8 | 332.4 | 283.8 |
| Slot error | 492 | 512 | 525 |
| Big-5 Top 10 share, 1965-69 mean | 22.0 | 16.5 | 16.6 |
| Advance-single share of launches, 1969 | 84.7% | 4.8% | 4.6% |

- The bundle is worse than the reference on decade share error on both seeds (+42, +13). The late part is mostly
  Soul share (14.0-14.8 against the reference's 16.8-17.9). The candidate restores it, ending better than the
  reference.
- **The bundle lowers late Big-5 Top 10 share on both seeds** (1965-69 mean −6.8 and −4.3). The losses are in
  TradPop (−2.1 points of Top 10), TeenPop (−1.6), Bubblegum (−1.2) and Soul (−0.6), the majors' LP-act singles. The
  candidate does not recover them. This runs against the author's late-decade consolidation goal.
- The cause is release practice's era-neutral priors. The research doc says the 1960-64 rules continue after 1964 "as
  an extrapolation needing separate validation". This decade check is that validation. Per seed and year, release
  practice issues about 30 post-LP extractions all decade (the LP must have peaked in the album top 40) against 4,000-5,200 album-only LPs;
  advance singles rise only from 38 to 206. The reference's universal promo grew to 85% of launches, which was
  wrong the other way. The late-1960s LP-led single (cuts pulled from or ahead of albums) has no route.

### Youth singles keyframes on top of the candidate (`lpa4-youth`)

The candidate plus TeenPop@1960 ×.87, @1962 ×.96, @1964 ×.85 and DooWop@1960 ×.79, @1962 ×.88, sized by
sqrt(target/current). TeenPop@1966-69 ×.64-.74 rides along for the decade run and is inert in a 260-week run.
Five seeds, paired against `lpa2-fbj30`:

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Share error | 19.1 (−4.5, 1.2) | 17.2 (−2.4, 0.9) | 20.1 (0.0) | 20.5 (0.0) | 26.8 (+1.1, 2.0) |
| CR8 | 44.5 (−1.2) | 41.8 (+4.7, 1.2) | 36.1 (+0.8) | 38.1 (+2.3) | 42.5 (+3.7, 1.5) |
| Big-5 | 32.3 (+3.8) | 28.2 (+6.2, 1.9) | 19.5 (0.0) | 23.1 (+1.1) | 19.7 (−0.7) |
| DooWop share (target) | 6.6 (5.1) | 4.7 (3.8) | 2.2 (2.2) | | |
| TeenPop share (target) | 16.5 (13.4) | 14.8 (12.5) | 14.4 (12.3) | 11.3 (10.8) | 9.3 (7.9) |

- Doo-Wop responds as the quadratic rule predicts. **TeenPop does not.** A ×.87 1960 keyframe should take its
  share from 17.6 to about 13; it moved 1.1. The binding constraint for TeenPop is not its baseline, and it needs
  diagnosing before any further keyframe trims. Its routing is not obviously clamped (youth weight ~.71 against a
  .44 baseline).
- The trims recover the concentration the Folk cut cost. TeenPop and Doo-Wop singles are 96-97% non-major, so
  their freed Top 10 room goes partly to major genres. The combination (`lpa4-youth`) is the best 1960-64 arm overall:
  share error sum 103.6, CR8 back inside the 38-48 band except 1962, Big-5 near or above history.

Decade pair `dec-lpa4-youth` (seeds 1001/2002), sums over 1960-69:

| | `dec-awrw-on` | `dec-apr8-bundle` | `dec-lpa-fbj30` | `dec-lpa4-youth` |
|---|---:|---:|---:|---:|
| Share error | 304.8 | 332.4 | 283.8 | **260.4** |
| Share error by seed | 316.8 / 292.7 | 359.1 / 305.7 | 310.2 / 257.4 | 272.7 / 248.1 |
| Slot error | 492 | 512 | 525 | 500 |
| Slot error by seed | 503 / 481 | 539 / 485 | 535 / 515 | 505 / 495 |
| CR8 | 395.0 | 384.8 | 377.6 | 395.4 |
| Big-5 | 231.4 | 194.9 | 182.3 | 211.5 |

| TeenPop | 1960 | 1962 | 1964 | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| target share | 13.4 | 12.3 | 7.9 | 5.6 | 4.0 | 2.7 | 1.7 | 1.2 |
| `dec-lpa-fbj30` | 18.0 | 13.9 | 11.3 | 11.0 | 9.9 | 6.6 | 3.4 | 2.0 |
| `dec-lpa4-youth` | 16.9 | 14.2 | 8.2 | 6.5 | 4.5 | 2.3 | 1.2 | 0.7 |
| slots, `dec-lpa4-youth` (benchmark) | 33 (28) | 28 (20) | 11 (5) | 8.5 (7) | 8 (6) | 6 (9) | 2 (2) | 0 (2) |

- Best arm on share error on both seeds and better than the candidate on slot error on both seeds. CR8 is back to the
  reference; Big-5 recovers about half of what the bundle lost.
- **TeenPop's late keyframes transmit as the quadratic rule predicts** (1966 ×.64 → share ×.45); only 1960-62 resists.
  That points at the opening roster/catalogue (inherited 1960 supply), not at a clamp. The 1968-69 trims slightly
  overshoot (1.2 and 0.7 against 1.7 and 1.2), so ×.80/×.85 there is the obvious refinement.

## 4. The Jazz decision

The share table and the album-chart estimate cannot both be met by album affinity. Affinity raises every jazz LP's
buyer pool together, and the best of them reach the album head. The options:

1. **Accept the compromise (Jazz 0.30):** Jazz units 3.9 against 6.2; Jazz ~10% of the early album head against 0-4.
2. **Let the share table govern (0.40):** Jazz units on target 1960-62, but Jazz at 20-30% of the album head, the
   problem that the August pass cut it to fix.
3. **Let the album estimate govern (0.20-0.25):** Jazz units stay at 2.5-3.
4. **Build the missing album chart-efficiency term.** A jazz LP that sells for years at a low weekly rate adds units
   without chart weeks. The model has one catalogue decay for all normal albums (`AlbumSimulator.CatalogWeeklyDecay`
   0.93 after week 8). A genre-scoped catalogue tail for jazz (and possibly classical) is the historically grounded
   version (jazz LPs were steady catalogue sellers rather than chart hits; not yet sourced here). It needs
   full-telemetry runs to judge, because the proxy above counts lifetime units.

The decade check uses option 1 as the interim candidate.

### Research: can jazz sell without charting? (2026-10-20, later)

The author asked whether large jazz LP sales with few chart hits is possible. Historically it is plausible. The
structure is many small titles selling slowly through specialist channels. The evidence is thin and mostly
secondhand:

- **Per-title sales were small.** Collector-forum accounts (secondhand, partly unsourced) put Blue Note's break-even
  around 2,500 copies and a normal first year around 7,000. A Mercury executive reportedly defined a jazz record as
  "anything that sells under 5,000". Five figures was a hit
  ([organissimo thread](https://www.organissimo.org/forum/topic/86959-sales-and-distribution-of-jazz-lps-circa-1948-1964/)).
- **Few titles charted.** The same thread's chart counts: Blue Note had about ten pop- or R&B-chart albums before its
  1966 sale, Argo/Cadet 14 by the end of 1966 (eight of them Ramsey Lewis), across hundreds of releases.
- **Distribution under-sampled by the charts.** Independent jazz sold mainly through a few dozen specialty stores and
  by mail order (Prestige). Billboard's jazz list surveyed only 25 outlets and did not exist yet in 1965; its pop LP
  chart ranked titles from store reports and did not measure volume
  ([Crépon, *Pharoah Sanders in Billboard*](https://www.crj-online.org/v14/CRJ-PharoahSandersBillboard.php)).
  Steady sellers could miss the chart entirely: *Kind of Blue* never made Billboard's album chart on release.
- **The big exceptions are the chart record**: *Time Out*, *Jazz Samba*, *Getz/Gilberto*, *The In Crowd* (No. 2,
  47 weeks). That matches `albumchart.odt`'s 0-4%.
- **The share table's jazz level is unconfirmed and looks high.** No 1960-64 source for jazz's share of record sales
  was found. A Billboard item of 5 December 1960 reportedly put pop at about 75% of all sales (seen only in a search
  summary; the archive refused the download). A forum commenter recalls an analysis putting jazz below classical.
  Jazz at 13-19% of LP units would use up most of the non-pop remainder.

In the model, "units without chart presence" already exists: Soul LPs reach the album top 50 at 0.05-0.19× their unit
share and Country at about 0.45×. Both have many titles each drawing a small, suppressed buyer pool. Jazz under affinity
does the opposite, because affinity raises every title's demand (seed 1001 full telemetry, 1960-64):

| Jazz affinity | jazz LPs / jazz releases | % of LP titles | % of LP units | units per title (× mean) | top-50 weeks / unit share |
|---|---:|---:|---:|---:|---:|
| .20 | .48 | 5.3 | 3.0 | 0.56 | 0.38 |
| .30 | .61 | 7.6 | 8.3 | 1.09 | 1.12 |
| .40 | .67 | 8.6 | 12.7 | 1.48 | 1.43 |

The route that fits the history is **more jazz LP titles at low per-title demand**, not higher affinity. Chart
presence is convex in per-title demand but roughly linear in title count. Projection, not a run: at .20 with every
jazz release an LP (2.1× the titles), jazz LP units reach about 6.3% and the head about 2.4%, inside the chart
estimate but only half the 12-13% of LP units the share table implies. Meeting the full table value needs jazz at
roughly a quarter of all LP titles, or a slow jazz catalogue tail, and the table value itself is the weakest number
here.

**Recommendation:** let the album estimate govern chart presence and treat the share table's jazz figure as an upper
bound until it is sourced. Next jazz step (five 260-week seeds, no decade run): an opt-in probe that makes jazz
LP-led in the format decision (1960 jazz was an LP market) while keeping realized per-title demand at .20-.25, judged
on jazz LP units, album head share, and the Folk/TradPop/Easy Listening shares it would displace.

## 5. Next

1. **Late-decade release practice** (blocks adoption of the bundle). Extraction needs an LP already in the album top 40
   and allows one cut, so it stays at about 30 per seed per year while LPs grow to 5,000. Wanted: a dated source census of how
   1965-69 LP-era singles were sourced (pulled ahead of or from LPs, by genre family and major/independent), then
   era-aware extraction/advance priors judged on late Big-5 share, the consolidation goal and late slots.
   **Done in [LateDecadeSingleSourcing.md](LateDecadeSingleSourcing.md):** the census puts the miss on advances (7% of
   late Top 10 weeks against ~22%), not extraction. It adds opt-in `--album-practice-late-advance=K`, and the
   decade arms are ready there. The late Big-5 gap is a volume/consolidation question, not a route question.
2. **TeenPop 1960-62.** Late keyframes work; the opening years resist a baseline trim. Check the opening
   roster/catalogue's TeenPop weight before trimming further. Ease the 1968-69 trims to about ×.80/×.85.
3. **Jazz**, per §4, after the author's decision.
4. Remaining late misses outside this work: Easy Listening share +4 to +5 (1967-69), Folk Rock (0 slots 1965-66),
   Soul late slots slightly over in the candidate (30-33 against 28).

## Reproduction

Plans and manifests in `SimLogs/`: `lpa1-plan`, `lpa2-plan`, `lpa4-plan` (260 weeks), `lpa3-decade-plan`,
`lpa3b-decade-plan`, `lpa4d-decade-plan` (522 weeks), each with its `-runs.json`. Analyses: `lpa1-fiveyear-*`,
`lpa2-fiveyear-*`, `lpa4-fiveyear-*`, `lpa3-decade-*`, `lpa3b-decade-*`. Full-telemetry album charts:
`lpafull-{ctl,fb,fbj30,fbj40}-1001-260` (run directly with the common flags minus `--calibration`).
`run_genre_hook_experiments.py` now accepts 522-week plans.

```text
python SimTools/run_genre_hook_experiments.py SimLogs/lpa2-plan.json --parallel 4
python SimTools/analyze_genre_hook_calibration.py --tags apr8-bundle lpa2-fb lpa2-fbj30 lpa2-fbj40 lpa-fj40 --output SimLogs/lpa2-fiveyear
python SimTools/analyze_genre_hook_calibration.py --tags dec-awrw-on dec-apr8-bundle dec-lpa-fbj30 --seeds 1001 2002 --weeks 522 --output SimLogs/lpa3b-decade
python SimTools/genre_share_breakdown.py --tags dec-awrw-on dec-apr8-bundle dec-lpa-fbj30 --seeds 1001 2002 --weeks 522 --years 1965 1969
```

## 6. Folk on the current candidate and the `lwbr` package (2026-11-15)

Answers `SimTools/LpChannelFableHandoff.md` issue 1. Runs: control `cand-v3-{2002,1001}-off` and package
`bud-v2-{2002,1001}-lwbr` (208 weeks); new 104-week arms `fh-v1-{2002,1001}-{lref,fa,fm,fm2}` on `lwbr`
(`run_lp_earned_screen.ps1`). Seed 2002 / 1001 throughout. Nothing here is a default.

### Why Folk is over-shared, ranked

`analyze_lp_genre_supply_demand.py`, LPs launched in 1960 (lifetime store units):

| | Control | `lwbr` |
|---|---|---|
| Folk share of LP launches | 13.2% / 12.1% | 12.4% / 11.9% |
| Folk share of their store units | 28.5% / 29.4% | 29.3% / 30.2% |
| Folk units per title (× the year's mean) | 2.16 / 2.44 | 2.36 / 2.54 |
| Folk share of the year's 50 biggest LPs | 42% / 58% | 56% / 60% |

1. **Demand per title, through album affinity (about 1.9×).** A title's buyer pool is
   `buying population × segregation × acceptance × affinity × willingness`, and each title sells against its own
   exhaustion, so a genre's units are titles × pool. Folk's shaped affinity is 0.89 in 1960, 0.97 in 1961 and clamps at
   1.0 from 1962; the title-weighted mean is about 0.41. Baseline × affinity puts Folk's pool at 1.83–1.93× the mean.
2. **Label mix (about 1.2×).** 30–47% of Folk LPs are on Majors (TradPop 16–25%), and the live LP tier constant gives
   Major 1.36 against Small 0.50. Majors take 57–69% of Folk's store units.
3. **Quality is not a cause** (0.9× the mean), and the residual after 1–2 is 1.0–1.2×.
4. **Supply is not a cause.** Folk is 12–13% of LP launches, on the 13% anchor.
5. **Racks only pass it on.** Folk act titles sell 0.8–1.05× their same-week store units on racks, below Easy
   Listening (1.8–2.5×), Country and TradPop. Folk's 25–38% of 1961 rack units is its store share carried over by the
   store-sales ranking, not a separate rack cause.

**The head is worse than the units.** The album chart was empty because the candidate runs under `--calibration`,
which skips `WriteAlbumRows`. New telemetry switch `--album-chart-history` keeps the weekly chart (27-week off run:
94 weekly streams byte-identical to `cand-v3-2002-off`; the other five differ only in rows written at close).
`analyze_album_chart_head.py` on `fh-v1-*-lref`: Folk is 80% / 76% of the 1960 year-end top 50 and 78% / 92% of 1961,
against the author's 13 / 6. It holds 50% / 60% of 1960 No. 1 weeks and 27% / 21% of 1961.

**Second anchor.** Billboard's No. 1 LPs: the Kingston Trio held the top for about 16 of 53 weeks in 1960 (*Here We
Go Again!*, *Sold Out*, *String Along*) and no Folk title did in 1961
([1960](https://en.wikipedia.org/wiki/List_of_Billboard_number-one_albums_of_1960),
[1961](https://en.wikipedia.org/wiki/List_of_Billboard_number-one_albums_of_1961); the tables mix the mono and stereo
charts, so the counts are approximate). Billboard's 1960 year-end top five has one Folk title (*The Sound of Music*
cast, Shelley Berman, Bob Newhart, an RCA compilation, the Kingston Trio;
[third-party listing](https://bestsellingalbums.org/year-end/Billboard_Top_Albums_1960)). Both agree with the author's
13 → 6: a large 1960 presence from one act, and a dip in 1961.

### Screened fix: `--album-affinity` on `lwbr`

| Arm | `--album-affinity=` | LP scale |
|---|---|---|
| `lref` | none (`lwbr` as is) | 1.6 |
| `fa` | Folk=0.44, BossaNova=0.40 | 1.6 |
| `fm` | `fa` + TraditionalPop=0.46, Classical=0.60, Comedy=0.50 | 1.6 |
| `fm2` | `fa` + TraditionalPop=0.41, EasyListening=0.28, Classical=0.60 | 1.8 |

Jazz is left at 0.20 (§4: the Jazz level is the author's decision). Year-end top 50, 1960 / 1961, each seed:

| Top 50 % | est | `lref` | `fa` | `fm` (2002) | `fm2` |
|---|---|---|---|---|---|
| Folk | 13 / 6 | 80 / 78; 76 / 92 | 16 / 14; 18 / 16 | 8 / 12 | 12 / 8; 12 / 18 |
| TradPop | 30 / 28 | 2 / 8; 0 / 6 | 20 / 24; 14 / 14 | 48 / 58 | 36 / 48; 24 / 34 |
| Easy Listening | 6 / 12 | 12 / 8; 16 / 0 | 30 / 38; 32 / 40 | 20 / 12 | 14 / 4; 10 / 4 |
| Comedy | 11 / 6 | 0 / 2; 0 / 0 | 20 / 10; 10 / 18 | 18 / 10 | 22 / 20; 8 / 4 |
| Soundtrack | 7–14 | 6 / 4; 2 / 0 | 10 / 10; 0 / 4 | 6 / 6 | 10 / 4; 12 / 2 |
| Classical | not estimated | 0 / 0; 6 / 2 | 2 / 0; 12 / 0 | 0 / 0 | 6 / 6; 26 / 24 |

| Store units of the year's launches % | implied (§2) | `lref` | `fa` | `fm2` |
|---|---|---|---|---|
| Folk | 11 / 10 | 27.5 / 24.5; 28.0 / 27.3 | 9.1 / 10.5; 10.2 / 8.7 | 9.4 / 9.9; 10.7 / 11.3 |
| TradPop | 30 / 30 | 16.6 / 17.3; 13.9 / 13.9 | 23.5 / 19.9; 16.7 / 15.2 | 27.0 / 25.6; 22.0 / 19.2 |
| Easy Listening | 17 / 15 | 14.7 / 14.0; 12.2 / 11.2 | 18.5 / 18.0; 17.2 / 17.0 | 13.7 / 8.0; 9.3 / 8.5 |
| Classical | | 5.4 / 3.9; 6.9 / 4.5 | 7.8 / 5.6; 6.7 / 6.2 | 8.3 / 7.2; 11.5 / 10.0 |
| Comedy | 2.9 / 2.5 | 5.5 / 5.5; 5.4 / 5.9 | 8.0 / 6.4; 8.0 / 7.8 | 8.9 / 7.1; 8.0 / 6.2 |

| | `lref` | `fa` | `fm2` |
|---|---|---|---|
| Folk share of LP launches, 1960 | 12.4% / 11.9% | 8.6% / 8.3% | 8.8% / 8.5% |
| Store LPs 1960 (M) | 40.3 / 40.9 | 34.3 / 31.1 | 39.3 / 35.5 |
| Store LPs 1961 (M; Billboard 68) | 65.2 / 66.7 | 54.1 / 53.7 | 60.4 / 59.2 |
| Gross 1961 ($M) | 419 / 420 | not read | 406 / 406 |
| Median LP units, act's best single under 1k | 13.2k / 13.8k | not read | 13.1k / 14.8k |
| Bankruptcies (104 weeks) | 86 / 96 | not read | 100 / 94 |

- **The Folk cut works and replicates.** At 0.44 Folk is 12–18% of the 1960 top 50 and 9–11% of store units, on both
  benchmarks. Bossa Nova is not yet testable (it starts in 1962).
- **Affinity also cuts supply.** It feeds the album prior and the opening-catalogue seed, so Folk launches fall by
  a third (12% → 8.5%). The unit share lands a little under for that reason; 0.46–0.48 would bring it to 11–12%.
- **A pure cut removes volume.** `fa` loses 6–10M store LPs in 1960 and 11–13M in 1961. 1961 is demand-bound (17–25M of
  the retail envelope is unspent), so nothing fills the gap.
- **The vacuum goes to the wrong genre.** In `fa`, Easy Listening takes 30–40% of the top 50 against an estimate of
  6–12, and Comedy is over in places. This is the trap in memory `vacuum-filling-vs-sampling-variance`.
- **Affinity lifts are convex at the head.** TradPop 0.36 → 0.46 took its units from 17% to 31% and its top-50 share
  to 48 / 58 (`fm`). At 0.41 with Easy Listening trimmed to 0.28 (`fm2`), TradPop is 24–36% of the 1960 top 50 and
  22–27% of units, near both benchmarks; 1961 is still over on seed 2002 (48).
- **Classical 0.60 is seed-dependent.** Units rise to 7–11%, but on seed 1001 Classical takes 24–26% of the top 50.
  That is the Jazz problem again (§4): affinity raises every title's pool, and the best titles reach the head.
  More Classical units without head presence needs more titles at low per-title demand, not affinity.
- **Comedy is the next untouched absorber** (0.44): 20–22% of the top 50 on seed 2002 against 11 / 6. Seed 1001 reads
  8 / 4, so this is not yet a replicated miss.
- **Volume is mostly, not fully, restored.** With the stores-only scale at 1.8, 1961 stores are 59–60M against 65–67M.
  The scale's range ends at 2.0.
- **The package's effect survives.** An unknown act's LP still sells 13–15k and 1.3% of LPs lose money. Bankruptcies
  are +14 / −2, inside the reshuffle band.

**Verdict.** The mechanism is the untouched Folk affinity, and `--album-affinity=Folk=0.44,BossaNova=0.40` fixes Folk
at the head and in units on two seeds. It cannot be adopted alone: it removes 11–13M store LPs in 1961 and hands the
head to Easy Listening. `fm2` (with TraditionalPop=0.41 and EasyListening=0.28, and the stores-only scale at 1.8) is the
best screened set. Open before adoption: the Classical value (leave 0.45 until the title-count route is tried), a
Comedy trim to about 0.36, the last 5–7M of 1961 store volume (scale 1.9–2.0 or TradPop 0.43), Bossa Nova and 1962–63
in a 208-week run, and the Big-5 cash side effect (§3) in the decade A/B.

**208-week follow-up** (`fh-v1-{2002,1001}-all`, `GameyBehaviorAudit.md` §2.11): with this set, Folk is 12 / 22 / 24 / 2
and 10 / 14 / 14 / 6 of the 1960–63 top 50 (estimate 13 / 6 / 18 / 18). The cut fixes 1960–62 and undershoots 1963, where
the control was already near the estimate; it needs a year-keyed value or a 1963–64 baseline lift. Bossa Nova at 0.40 is
still 18–22% of the 1962–63 head against 2–4, so it needs about 0.25. Classical 0.60 overshoots on seed 1001 at 208
weeks too and should be dropped.
