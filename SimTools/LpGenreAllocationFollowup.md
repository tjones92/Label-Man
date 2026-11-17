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

## 7. The handoff's Folk fix on the Major-LP bundle (2026-11-15)

Base: `ma12d-v2` (208 weeks; the candidate + `--lp-launch-standing=30000 --lp-demand-scale=2 --programme-major-albums=12
--album-prior-debut-scale=2.0613`, `GameyBehaviorAudit.md` §2.18). The extra Major LPs leave the genre problem as it was:
Folk is 62 / 64 / 62 / 28 and 62 / 82 / 68 / 48 of the 1960–63 year-end top 50, and Bossa Nova is 24–40 in 1962–63.

Arms, at LP scale 2.2 to restore the volume the Folk cut removes:
- `gb`: `--album-affinity=Folk=0.44,BossaNova=0.25,TraditionalPop=0.41,EasyListening=0.28,Comedy=0.36`. This is the
  §6 `fm2` set with Bossa Nova at the 0.25 the 208-week follow-up asked for, Classical left at 0.45, and the suggested
  Comedy trim.
- `gbb`: `gb` + Blues=0.40, seed 1001 only.

Year-end top 50 %, 1960 / 61 / 62 / 63 (seed 2002; seed 1001):

| | estimate | `ma12d-v2` | `gb` | `gbb` (1001) |
|---|---|---|---|---|
| Folk | 13 / 6 / 18 / 18 | 62 / 64 / 62 / 28; 62 / 82 / 68 / 48 | **20 / 14 / 12 / 6; 30 / 20 / 26 / 16** | |
| TradPop | 30 / 28 / 24 / 36 | 14 / 12 / 6 / 6; 2 / 0 / 0 / 4 | 50 / 62 / 58 / 42; 26 / 38 / 40 / 42 | |
| Bossa Nova | – / – / 2 / 4 | 0 / 0 / 24 / 40; 0 / 0 / 28 / 30 | **0 / 0 / 0 / 4; 0 / 0 / 0 / 0** | |
| Easy Listening | 6 / 12 / 10 / 2 | 16 / 12 / 8 / 6; 20 / 12 / 2 / 2 | 16 / 4 / 2 / 2; 8 / 14 / 2 / 2 | |
| Comedy | 11 / 6 / 6 / 8 | 0 / 2 / 0 / 0; 2 / 0 / 0 / 0 | 2 / 0 / 0 / 0; 6 / 2 / 0 / 0 | |
| Soundtrack | 7–14, ≈25 in 1962–63 | 8 / 4 / 0 / 4; 8 / 2 / 0 / 2 | 4 / 6 / 6 / 16; 14 / 10 / 2 / 6 | |
| Blues | not listed (≈0) | 0 / 4 / 0 / 2; 0 / 2 / 2 / 2 | 4 / 8 / 12 / 8; 4 / 6 / 14 / 2 | **0 / 0 / 0 / 0** |

| | `ma12d-v2` | `gb` | `gbb` (1001) |
|---|---|---|---|
| Stores 1961 / 1962 / 1963 (M) | 65.2 / 72.8 / 69.5; 68.2 / 75.4 / 71.3 | 64.7 / 69.4 / 67.8; 63.0 / 71.8 / 70.4 | 64.0 / 70.3 / 67.0 |
| Gross 1962 ($M) | 507; 514 | 507; 510 | 510 |
| Major share of 1962-cohort LP units % | 66.2; 72.1 | 63.8; 66.1 | 65.0 |
| Bankruptcies (208 wk) | 132; 150 | 134; 146 | **137** |

- **The Folk fix carries over.** Folk lands near the estimate in 1960–62. Seed 2002 undershoots in 1962–63, partly
  because the model files the 1963 revival as ContemporaryFolk (6% on that seed). Bossa Nova at 0.25 is fixed on both
  seeds, slightly under.
- **Volume and costs hold at scale 2.2.** Stores are within 1–4M, gross is flat, and bankruptcies are +2 / −4. The fix
  also undoes most of the Major-share rise the Major LP bundle caused.
- **New absorbers:**
  - **TradPop at 0.41** overshoots on seed 2002 (42–62 against 24–36). Next value: 0.38.
  - **Blues at 0.58**, the highest affinity left, takes 12–14% of the 1962 head on both seeds. At 0.40 it is 0, and
    bankruptcies are 137 against 146.
  - **Comedy is under on this base** (0–6 against 11 / 6 / 6 / 8), so the 0.36 trim, which §6 drew from `lwbr`, is the
    wrong direction here. Next value: 0.50.
- Still open: soundtracks in 1962–63 (2–16 against ≈25) are a supply question for the soundtrack subsystem, not an
  affinity question, and Jazz stays at the author's decision (§4).
- Next: the combined candidate `cb-v1` (`--programme-major-albums=8` + debut prior + `--album-affinity=Folk=0.44,
  BossaNova=0.25,TraditionalPop=0.38,EasyListening=0.28,Comedy=0.50,Blues=0.40`, scale 2.2), 208 weeks, both seeds.

**Combined candidate `cb-v1`** (208 weeks: `--programme-major-albums=8` + debut prior + `--album-affinity=Folk=0.44,
BossaNova=0.25,TraditionalPop=0.38,EasyListening=0.28,Comedy=0.50,Blues=0.40`, scale 2.2), against `ma8d-v2`. Year-end
top 50 %, 1960 / 61 / 62 / 63 (seed 2002; seed 1001):

| | estimate | `ma8d-v2` | `cb-v1` |
|---|---|---|---|
| TradPop | 30 / 28 / 24 / 36 | 14 / 10 / 12 / 12; 2 / 6 / 0 / 6 | 36 / 54 / 36 / 42; **38 / 36 / 24 / 44** |
| Folk (+ ContemporaryFolk in 1963) | 13 / 6 / 18 / 18 | 66 / 68 / 54 / 22; 64 / 82 / 70 / 34 | 22 / 24 / 22 / 12; 22 / 20 / 28 / 6 (+8) |
| Easy Listening | 6 / 12 / 10 / 2 | 18 / 14 / 4 / 6; 20 / 4 / 6 / 2 | 14 / 6 / 12 / 8; 8 / 18 / 12 / 6 |
| Comedy | 11 / 6 / 6 / 8 | 2 / 4 / 0 / 0; 4 / 0 / 0 / 0 | **8 / 4 / 6 / 0; 12 / 8 / 12 / 0** |
| Bossa Nova | – / – / 2 / 4 | 0 / 0 / 26 / 32; 0 / 0 / 22 / 40 | 0 / 0 / 0 / 0; 0 / 0 / 0 / 0 |
| Soundtrack | 10 / 10 / 25 / 25 | 0 / 0 / 0 / 8; 2 / 2 / 2 / 6 | 18 / 6 / 6 / 10; 10 / 4 / 2 / 12 |
| Blues | ≈0 | – | 0; 0–4 |

- **The head is close to both estimates on both seeds for the first time.**
- **Still off:**
  - TradPop in 1961 on seed 2002 (54);
  - Folk in 1961 (20–24 against 6; Kingston Trio's 1961 dip);
  - Comedy and Folk in 1963;
  - Bossa Nova a little under (0 against 2–4, try 0.30);
  - Soundtracks in 1962–63 (a supply question);
  - SurfRock 6–16 in 1963. That is plausible: Beach Boys, Jan & Dean and Dick Dale all charted LPs in 1963.
- **Costs against `ma8d`:** stores −5M a year, mostly made up on racks; gross −$1–16M; bankruptcies +22 / +4.

## 8. Soundtracks, TradPop and Bossa Nova on `cb-v1` (2026-11-15)

Runs: `cbg-*` (104 weeks unless noted; seed 2002 / 1001). A 104-week run is byte-identical to the first 104 weeks of
the 208-week run of the same request (`cbg-ctl-2002` against `cb-v1-2002`), so `cb-v1` is the short control.

- **Soundtracks were short-lived, not scarce.** `cb-v1` mints as many as the October reference (20–40 charting a year)
  but each charts 7–11 weeks (reference 17–24, design 40–60). Racks give a soundtrack a flat 0.9 shopper weight
  (`RackJobberChannel.SoundtrackGenreWeight`), so in 1960–61 it sells 4.1 rack units per store unit (other genres
  0.6–1.6), and under `--rack-shared-exhaustion` those sales spend its store buyers. New probe
  `--rack-soundtrack-weight=F`. At 0.3 with `--rack-shopper-scale=0.033` (restores the rack volume), 1961 soundtracks
  go from 6 / 4 to 18 / 18 of the top 50, chart life to 12–16 weeks, and the 208-week pair (`cbg-v1`) reads
  10 / 24 / 26 / 18; 16 / 24 / 20 / 16 (Billboard 1961–63: 24 / 22 / 18).
- **TradPop:** 0.36 (the authored value) with Comedy 0.50 (`cbg-v2b`) is 36 / 32; 22 / 22 in 1960–61. 0.34 undershoots.
  Reverting Comedy to 0.44 raised TradPop 4–8 points and left Comedy under in 1962–63.
- **Bossa Nova 0.30:** 4 in 1963 on both seeds (estimate 4, Billboard 4).
- **Folk:** a 1961-only `--album-affinity-ramp=` (new probe) hits the dip but its space goes to TradPop and Comedy.
  Not adopted.

Best set so far (`cbg-v2b`): `--rack-soundtrack-weight=0.3 --rack-shopper-scale=0.033
--album-affinity=Folk=0.40,BossaNova=0.30,TraditionalPop=0.36,EasyListening=0.28,Comedy=0.50,Blues=0.40`.
Not yet confirmed at 208 weeks; `cbg-v1` (Comedy 0.44, TradPop 0.38) had bankruptcies 132 / 160 against 139 / 138.

## 9. Researched album-chart targets and why youth LPs miss the head (2026-11-15)

**Billboard year-end LP top 50, classified to model genres** (bestsellingalbums.org: 1961 and 1962 mono charts, 1963
combined; 1960 lists only the top 5). Act soundtracks (Elvis) count under the act; cast and film albums as Soundtrack;
Ray Charles's country LPs as R&B; Peter, Paul & Mary and Baez as Folk.

| % of top 50 | 1961 | 1962 | 1963 |
|---|---|---|---|
| Soundtrack / cast | 24 | 22 | 18 |
| TradPop | 22 | 18 | 28 |
| Youth + R&B (rock, teen, R&B, doo-wop, soul, surf) | 16 | 30 | 20 |
| Folk (incl. contemporary) | 10 | 14 | 16 |
| Comedy | 12 | 4 | 10 |
| Easy Listening | 10 | 8 | 2 |
| Jazz / Bossa Nova | 2 / 0 | 4 / 0 | 0 / 4 |
| Classical | 2 | 0 | 0 |
| Country | 2 | 0 | 0 |

Country was nearly absent from the pop LP chart (one title in three years); the model has 4–12, so it is over.
Half of the youth titles are hit collections (*Encore of Golden Hits*, *Paul Anka Sings His Big 15*, *Bobby Darin
Story*, *Oldies But Goodies*, *Buddy Holly Story*, Orbison and Shirelles *Greatest Hits*); most of the rest are the
1962 twist LPs (four Chubby Checker titles, Joey Dee, Ray Charles). `GenreAnnualMarketShareTable.md` is an old
three-seed model output of total market share, not an album target.

**Why youth LPs miss.** Youth acts launch 44% of LPs and their LPs reach the top 10 as often as adult ones, but chart a
median 3 weeks (adult 7–8). New telemetry `--album-demand-trace` (inert: weeks.csv identical) on `cbg-tr-2002`:

| top-10 LPs, week | 1 | 2 | 3 | 4 |
|---|---|---|---|---|
| youth + country units | 17.1k | 10.6k | 8.2k | 2.2k |
| follower-bought | 92% | 87% | 78% | 0% |
| adult units | 11.6k | 9.0k | 9.8k | 6.6k |
| follower-bought | 73% | 50% | 33% | 0% |

Follower LP intent is spent 50 / 30 / 20 over three clearing weeks (`MediaFollowerDemand.WeekShare`). After it, youth
LPs have 2–3k a week of organic demand (format tilt 0.63 against 1.10, conversion about 2.6x lower, partly the small
label tier) and adult LPs 9–12k. No stock-outs; exhaustion is similar.

Screens (all on `cbg-v2b`, 104 weeks), youth + R&B and country % of the 1960 / 1961 top 50:

| arm | seed 2002 youth; country | seed 1001 youth; country |
|---|---|---|
| `cbg-v2b` | 0 / 0; 2 / 0 | 0 / 4; 0 / 4 |
| `--follower-album-casual=0.8,26` (`fc8x26`) | 2 / 6; 4 / 10 | |
| `--lp-act-genre` (`ag1`; 2002 ran out of memory at week 101) | 0 / 0; 0 / 2 | 0 / 0; 2 / 4 |
| both (`agc1`) | 4 / 4; 4 / 12 | 2 / 12; 6 / 10 |

- `--follower-album-casual=C,N` (new) spreads the casual share of an LP's follower book over N weeks, total unchanged.
  It doubles youth top-10 chart life (3 → 6.5–11.5 weeks) but lowers peaks, helps TradPop acts most, and lifts
  country the wrong way. Hot 100 TeenPop 1961 16 → 21–28 (watch).
- `--lp-act-genre` (new) files an LP under the act's identity genre: off-identity LPs 25% → 3.5%. It moves no youth
  share (youth-act LPs were already 87% filed youth) and raises LPs losing money 6.6 → 8.9% / 6.7 → 11.8%, because the
  format decision and profit plan are still made on the picker's genre. It needs the decision made on the act's genre.
- Bankruptcies are flat in every arm (94–106).

**Open.** The remaining gap is organic demand concentrated on star acts. History says it came through hit
collections and crazes, not through every youth LP.

## 10. Hit-package demand: youth LPs through their hit collections (2026-11-16)

Follows §9's open item. Base `cbg-v2b` (104 weeks; seed 2002 / 1001). Nothing here is a default.

**Why the existing packages miss.** `--lp-assembly` is in the profile, so the old compilation lottery
(`AlbumModel.GetCompilationChance`, tilted to youth by single orientation) is retired. The live path is the LP-assembly
greatest-hits package: it needs 3 or more singles that peaked in the Top 40, 8 tracks and package spacing. The model makes
enough of them (16–28 youth packages in 1960–61). They reach the weekly top 10 (68–82%) but chart a median 3 weeks at
19–26k units, half the adult packages, and none reaches the year-end top 50. The act's hits only enter the label's
profit forecast. At sale time a package is an ordinary LP: genre album pool, track appeal × freshness, the normal-LP
decay, and 3 weeks of follower demand.

**Target: Billboard's 1961 mono year-end top 50**
([mono](https://bestsellingalbums.org/year-end/Billboard_Top_Mono_Albums_1961),
[stereo](https://bestsellingalbums.org/year-end/Billboard_Top_Stereo_Albums_1961)). Six hit collections, 12% of the top 50:

| Rank | Title | Model group |
|---|---|---|
| 12 | Johnny Mathis, *Johnny's Greatest Hits* | adult (TradPop) |
| 14 | The Platters, *Encore of Golden Hits* | youth |
| 19 | Paul Anka, *Sings His Big 15* | youth |
| 35 | Bobby Darin, *The Bobby Darin Story* | youth |
| 38 | *Johnny Horton's Greatest Hits* | country |
| 48 | *Oldies But Goodies Vol. 1* (various artists) | youth |

So youth packages are about 6–8% of the top 50 and adult plus country about 4%. Ranks 51–100 add Buddy Holly, Connie
Francis, Bobby Rydell and *Oldies Vol. 3*. The stereo top 50 has no hit collections (stereo was the adult hi-fi buyer).

**Probe** (`HitPackageDemand`, `Systems/GameyAuditSwitches.cs`; read in `AlbumSimulator.CalculateRegionalSales`):
- `--hit-package-demand=K`: a Compilation's store buyer pool in each region gains K × the buyers of the released
  singles it carries, split by population share. Buyers per single come from its chart peak (1960–61 median lifetime
  units in `cbg-v2b`: 878k at No. 1, 165k at 30, 31k at 85; log-log interpolation), so pre-1960 opening masters count
  like live singles. The added buyers pass through the same awareness, conversion and exhaustion. The labels'
  forecasts do not see it.
- `--hit-package-demand-youth`: K is also scaled by the package genre's Youth segment weight (`GenreSegmentRouting`:
  TeenPop 0.71, SurfRock 0.66, Rock and Roll 0.58, Doo-Wop 0.39, Soul 0.28, R&B 0.22, Country 0.16, TradPop 0.15,
  Folk 0.13, Easy Listening 0.11, Jazz 0). An adult act's single buyers are mostly adults already in its genre's
  album pool, so adding all of them counts them twice.
- `--hit-package-decay=D`: packages' weekly decay after week 8. Not screened: packages chart a median 3 weeks, so
  the decay was not binding.
- Off is inert: `hp-off-2002-sc`, 142 of 142 CSVs byte-identical to `cbg-v2b-2002-sc`. Unit checks are in
  `GameyAuditSwitchChecks`.

Year-end top 50 %, 1961 (seed 2002 / 1001):

| | control | flat 0.3 | flat 0.5 | flat 1.0 (2002) | youth 0.8 | youth 1.0 | Billboard |
|---|---|---|---|---|---|---|---|
| Youth + R&B | 0 / 2 | 6 / 6 | 16 / 14 | 36 | **20 / 10** | 20 / 16 | 16 |
| All hit packages | 4 / 2 | 30 / 22 | 40 / 40 | 56 | **30 / 20** | 40 / 30 | 12 |
| Youth hit packages | 0 / 0 | 6 / 6 | 16 / 14 | 36 | **20 / 10** | 20 / 16 | 6–8 |
| Adult + other packages | 4 / 2 | 24 / 16 | 24 / 26 | 20 | **10 / 10** | 20 / 14 | about 4 |
| Country | 0 / 4 | 4 / 12 | 8 / 16 | 4 | **0 / 8** | 2 / 10 | 2 |
| TradPop | 32 / 22 | 44 / 10 | 30 / 12 | 22 | 36 / 24 | 34 / 10 | 22 |

| Packages, 1960–61 launches | control | flat 0.5 | youth 0.8 |
|---|---|---|---|
| Youth: median chart weeks; units | 3 / 3; 26k / 19k | 11 / 12; 56k / 108k | 10.5 / 8; 85k / 58k |
| Adult + other: median chart weeks; units | 4 / 3; 65k / 47k | 15 / 16.5; 260k / 200k | 5 / 6; 60k / 57k |

| | control | youth 0.8 | youth 1.0 |
|---|---|---|---|
| Store LPs 1960 / 1961 (M) | 39.0 / 61.0; 34.7 / 62.2 | 40.3 / 61.4; 37.0 / 62.6 | 41.8 / 62.3; 37.3 / 65.4 |
| LP gross 1961 ($M) | 268; 265 | 263; 265 | 267; 270 |
| Labels defunct (104 weeks) | 119; 135 | 115; 129 | 112; 125 |

- **The mechanism works.** Hit buyers make youth packages chart 8–12 weeks and reach the year-end top 50. That is
  what the history shows: a youth act's LP presence came through its hit collection.
- **Flat K overshoots on adult packages.** At 0.5, adult and country packages take 24–26 points of the 1961 top 50
  against about 4, at 200–260k units a package. That is the double count.
- **Youth-weighted 0.8 is the best arm.** Youth + R&B averages 15 across the two seeds against 16. Adult packages are
  back near control units (57–60k) and 10 points of the top 50. Country is 0 / 8 (control 0 / 4).
- **It is cheap.** Store LPs +0.4 to +2.3M, gross flat, defunct labels −4 / −6 (inside the reshuffle band).
- **Youth is carried entirely by packages.** Youth packages are 20 / 10 of the top 50 against Billboard's 6–8, and
  youth studio LPs (Elvis, Brenda Lee, Ricky Nelson in 1961; the 1962 twist LPs) are still absent. The probe fills the
  package half of the gap and over-fills it to stand in for the studio half. Fixed below by the studio scope.
- **Seed spread is wide** (youth 20 against 10), so a third seed is needed before adoption.

**Studio LPs: `--hit-package-demand-studio=S`.** Billboard's 1961 youth studio LPs are star acts' LPs that carry a
current hit (*Rick Is 21*, *Dedicated to You*, *Genius + Soul = Jazz*, Elvis). The model's hit-led LPs are those LPs:
27 youth Star/Superstar hit-led LPs per seed in 1960–61, reaching the top 10 79–100% of the time and charting 3 weeks.
With S on, a studio LP (any format but Compilation and Soundtrack) gains S × K of the buyers of the released singles on
it, with the youth weighting. An LP with no released singles on it gains nothing, so the new-signing LPs (about 1,400
per seed) are untouched.

Year-end top 50 %, youth-weighted (seed 2002 / 1001):

| | K 0.8 | K 0.8, S 0.5 | **K 0.6, S 1.0** | Billboard 1961 |
|---|---|---|---|---|
| Youth + R&B 1960 | 6 / 8 | 10 / 12 | **14 / 16** | top 5 only |
| Youth + R&B 1961 | 20 / 10 | 20 / 18 | **24 / 14** | 16 |
| Youth hit packages 1961 | 20 / 10 | 20 / 10 | **14 / 6** | 6–8 |
| Youth studio LPs 1961 | 0 / 0 | 0 / 8 | **10 / 8** | about 8 |
| Adult + other packages 1961 | 10 / 10 | 12 / 8 | **10 / 8** | about 4 |
| Country 1961 | 0 / 8 | 2 / 12 | **2 / 8** | 2 |
| TradPop 1961 | 36 / 24 | 30 / 10 | **30 / 14** | 22 |
| Store LPs 1960 / 1961 (M) | 40.3 / 61.4; 37.0 / 62.6 | 42.2 / 63.1; 38.9 / 64.7 | **42.4 / 64.2; 38.9 / 65.2** | |
| Labels defunct | 115; 129 | 121; 137 | **120; 138** | |

- **K 0.6, S 1.0 splits youth the way Billboard does.** Youth averages 19 against 16 in 1961, with packages at 10
  against 6–8 and studio LPs at 9 against about 8.
- **The studio LPs that reach the head are the right acts.** All are hit-led LPs by Stars and Superstars (plus one
  Superstar ordinary LP and one rock soundtrack); no Rising act's LP gets in.
- **The cost is still small.** Store LPs are +3–4M in 1960 and +3M in 1961, LP gross is +4–10M, and defunct labels are
  +1 / +3 against the control.
- **R&B is still missing from the head.** No R&B or Soul LP reaches the top 50, so there is no Ray Charles analogue.
  R&B's Youth weight is 0.22 because its young buyers route to UrbanRnB, so the youth weighting gives R&B acts little.
- **Placebo.** The routing session (2026-11-16) found that a placebo arm that only reshuffles the RNG moves TeenPop
  slots +5.5 and Big-5 Top 10 share +8 points. The youth lift here (0 → 15–20 points) is well above that, but the
  seed gap (24 against 14) is about that size.

**Third seed (3003).** Control `cbg-v2b-3003-sc` and arm `hps-k06s10-3003-sc`, 104 weeks. 1961 year-end top 50 %, seeds
2002 / 1001 / 3003, with the three-seed mean:

| 1961 | control | mean | K 0.6, S 1.0 | mean | Billboard |
|---|---|---|---|---|---|
| Soundtrack / cast | 20 / 16 / 12 | 16 | 18 / 16 / 8 | 14 | 24 |
| TradPop | 32 / 22 / 42 | 32 | 30 / 14 / 20 | 21 | 22 |
| Youth + R&B | 0 / 2 / 0 | 1 | 24 / 14 / 22 | 20 | 16 |
| of which hit packages | 0 / 0 / 0 | 0 | 14 / 6 / 10 | 10 | 6–8 |
| of which studio LPs | 0 / 2 / 0 | 1 | 10 / 8 / 12 | 10 | about 8 |
| Folk | 16 / 24 / 6 | 15 | 14 / 24 / 6 | 15 | 10 |
| Comedy | 16 / 18 / 26 | 20 | 8 / 14 / 30 | 17 | 12 |
| Easy Listening | 14 / 10 / 8 | 11 | 4 / 10 / 6 | 7 | 10 |
| Country | 0 / 4 / 4 | 3 | 2 / 8 / 8 | 6 | 2 |
| Adult + other hit packages | 4 / 2 / 4 | 3 | 10 / 8 / 8 | 9 | about 4 |
| Sum of gaps to Billboard (nine rows) | | 50 | | 36 | |

- **The youth fix replicates.** Youth + R&B is 14–24 on all three seeds, split about evenly between packages and studio
  LPs. Seed 3003 (store LPs +4.2 / +3.8M, defunct labels 128 → 141) gives the largest closure change so far. That is
  inside the reshuffle band seen on seed 2002 (113–131), but worth watching.
- **Folk is a seed effect, not a miss of the probe.** It is 6 / 14–16 / 24 in both arms. The three-seed mean (15) is
  still above Billboard's 10.
- **Comedy on seed 3003** (26–30 in both arms) is seed-specific.
- **Two consistent misses:**
  - **Country** rises by 2–4 points on every seed. Country's Youth weight is 0.16, as high as TradPop's, so its
    packages and hit-led LPs gain. Johnny Horton's package is the only country title Billboard has.
  - **Soundtracks** are under on every seed (8–18 against 24) and fall slightly when youth LPs enter. The rack
    soundtrack weight (§8) lifted them; this takes some of that back.

**Next.**
1. A 208-week pair of youth K 0.6, S 1.0 with a placebo arm (K 0.001, which only reshuffles), judged on 1962–63
   (youth + R&B 30 / 20; 1962 is mostly twist LPs).
2. Country and soundtracks: the two misses that replicate on all three seeds.
3. If R&B in the head matters, weight R&B acts' buyers by Youth + part of UrbanRnB rather than Youth alone.
4. The routing quirks (TeenPop Youth cap, Latin floor) were screened in their own session: no effect beyond placebo,
   not adopted (`segment-routing-quirks` branch, 5ab2d6c). If the Youth slope is ever adopted, K comes down about 20%.

Reproduction: `SimTools/run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/cbg-v2b-2002-sc-request.json
-Arms sc -LpScale 2.2 -Weeks 104 -Seed <seed> -RunPrefix <p> -Extra --hit-package-demand=K[,--hit-package-demand-youth][,--hit-package-demand-studio=S]`
(`hp-off`, `hp-k03`, `hp-k05`, `hp-k10`, `hpy-k08`, `hpy-k10`, `hps-k08s05`, `hps-k06s10`), then from `SimLogs/`
`python ../SimTools/analyze_hit_package_demand.py <run>-<seed>-sc ...`.

## 11. Country and soundtracks on the hit-package candidate (2026-11-16)

Candidate (the author's choice, 2026-11-16): `cbg-v2b` + `--hit-package-demand=0.6 --hit-package-demand-youth
--hit-package-demand-studio=1.0` (§10). Seeds 2002 / 1001, 104 weeks. Nothing here is a default.

**Country.** The country LPs in the 1961 top 50 are Major-label Star and Superstar LPs (hit-led LPs or packages)
selling 110–220k. They were already there in the control (0 / 2 / 2 titles across three seeds); the candidate adds
one or two each. The album chart ranks on store units (`AlbumSimulator.CalculateChartPoints`), so a country LP that
sells like a pop LP charts like one. Billboard's pop LP chart came from pop dealers' reports. Country LPs sold largely
through country outlets, and Billboard's 1961 mono top 50 has one country title. New switch
`--album-chart-reporting=Genre=w,...`: a genre's album chart points × w. Units and money are untouched, but chart
position still feeds awareness. Soundtracks keep weight 1.

**Soundtracks.**
- **What the shortfall is.** Every soundtrack in the model's 1961 top 50 is a 1961 release. On Billboard's 1961 list,
  about 10 of the 24 points are cast and film albums from 1956–59 (*South Pacific* ×2, *My Fair Lady*, *The Sound of
  Music*, *Ben-Hur*) and about 10 are from 1960. The model's 1960 soundtracks chart a median 10–12 weeks against the
  design's 40–60, and the opening world has no pre-1960 soundtracks at all.
- **What ends the runs.** `cbg-tr-2002` trace, the 20 top-10 soundtracks launched in 1960:

  | Age (weeks) | 1–4 | 9–12 | 21–26 |
  |---|---|---|---|
  | Pool penetration | 0.02 | 0.22 | 0.43 |
  | Exhaustion | 0.92 | 0.53 | 0.37 |
  | Box-office decay | 0.90 | 0.79 | 0.62 |
  | Rack units ÷ store units | 0.4 | 2.1 | 1.7 |

  The national pool is about 440k buyers. Racks spend it as fast as stores do (shared exhaustion), and exhaustion
  binds before box-office decay.
- **The switch.** `--soundtrack-pool-breadth=F`: a soundtrack's buyer pool × F and its per-buyer weekly conversion
  ÷ F. The launch week sells the same and the pool exhausts F times more slowly.

Year-end top 50 %, 1961 (seed 2002 / 1001):

| | candidate | Country 0.5, breadth 3 | Country 0.35, breadth 5 | Billboard |
|---|---|---|---|---|
| Soundtrack / cast | 18 / 16 | **20 / 38** | 50 / 26 | 24 |
| … of it released before 1961 | 0 / 2 | **0 / 8** | 16 / 8 | about 20 |
| Country | 2 / 8 | **0 / 0** | 0 / 0 | 2 |
| TradPop | 30 / 14 | **26 / 14** | 22 / 14 | 22 |
| Youth + R&B | 24 / 14 | **22 / 12** | 18 / 20 | 16 |
| Folk | 14 / 24 | **6 / 10** | 2 / 8 | 10 |
| Comedy | 8 / 14 | **22 / 12** | 4 / 22 | 12 |
| Easy Listening | 4 / 10 | **4 / 10** | 4 / 8 | 10 |
| Sum of gaps to Billboard (nine rows, two-seed means) | 30 | **22** | 36 | |
| 1960 soundtracks: median chart weeks | 6 / 11.5 | **13 / 18** | 15 / 28 | 40–60 (design) |
| Soundtracks in the 1960 top 50 | 10 / 14 | **18 / 18** | 20 / 24 | 7–14 (author) |
| Store LPs 1960 / 1961 (M) | 42.4 / 64.2; 38.9 / 65.2 | **42.6 / 66.7; 38.1 / 65.2** | 44.2 / 67.1; 38.8 / 65.7 | – / 68 |
| Labels defunct | 120; 138 | **107; 131** | 118; 140 | |

- **Country 0.5 removes country from the head on both seeds** (and from 1960). Billboard has one title, so 0.6–0.7
  may leave it there. Either way the Country error falls from 3–6 to 2.
- **Breadth 3 doubles soundtrack chart life and brings the total near Billboard's**: a 29 mean against 24, though
  seed 1001 is over at 38.
- **It does not create the carry-over.** Only 0 / 8 points are pre-1961 releases against about 20. The extra
  presence comes from more 1961 releases charting. Breadth 5 shows the same thing at a larger size and overshoots
  (50 on seed 2002).
- **Folk comes back toward target** in both arms (6 / 10 against 10). The longer-running soundtracks take the head
  room that seed 1001's Folk was holding.
- **Cheap.** Store LPs are within ±2.5M of the candidate, and defunct labels move −13 / −7 (inside the noise).

**Open.** The carry-over structure needs titles that are already old in 1960:
- an opening soundtrack catalogue, i.e. long-running cast and film albums released 1956–59 at world start, like the
  opening singles catalogue;
- and/or a slower tail for hit soundtracks only, so the 1960 hits last into 1961 without more 1961 releases charting.

Breadth then likely comes down from 3. Reproduction: `-Extra` = candidate flags +
`--album-chart-reporting=Country=C,--soundtrack-pool-breadth=F` (`cs-c05b3`, `cs-c035b5`), analysis as §10.

## 12. A slower tail for hit soundtracks (2026-11-16)

Base: the §11 candidate (`cbg-v2b` + hit-package flags + `--album-chart-reporting=Country=0.5`). Seeds 2002 / 1001,
104 weeks. Nothing here is a default.

**The switch.** `--soundtrack-hit-tail=R` (`SoundtrackHitTail`). A soundtrack that has peaked in the album top 10 has
an audience that renews, as a hit film or show kept finding new viewers. After week 12, its exhaustion reads its
penetration ÷ (1 + R × weeks past 12), in both the store and rack paths. The penetration ratchet stays in pool terms.
Weekly demand size, the launch, and soundtracks that never reached the top 10 are untouched. The box-office
trajectory, not a spent pool, then sets how long a hit lasts. The opening world has no soundtracks, so a title's first
chart year stands in for its release year (`analyze_soundtrack_carryover.py`).

Year-end top 50 %, 1961, and the chart life of 1960's soundtracks (seed 2002 / 1001):

| | breadth 3, no tail (§11) | b2, R .1 | b1.5, R .1 | b1, R .1 | **b1, R .25** | Billboard |
|---|---|---|---|---|---|---|
| Soundtrack / cast | 20 / 38 | 34 / 36 | 18 / 16 | 18 / 16 | **28 / 22** | 24 |
| … first charted before 1961 | 0 / 8 | 6 / 20 | 6 / 4 | 2 / 10 | **12 / 6** | ≈20 (≈10 from 1960) |
| … first charted in 1961 | 20 / 30 | 28 / 16 | 12 / 12 | 16 / 6 | **16 / 16** | ≈4 |
| 1960 top-10 soundtracks: median chart weeks | 23 / 35.5 | 46.5 / 55 | 36 / 26 | 18 / 50 | **52 / 43** | 40–60 (design) |
| … still charting in 1961 | 54 / 70 % | 90 / 94 % | 91 / 100 % | 64 / 89 % | **83 / 90 %** | |
| TradPop | 26 / 14 | 16 / 20 | 22 / 16 | 18 / 26 | **16 / 26** | 22 |
| Youth + R&B | 22 / 12 | 20 / 18 | 30 / 24 | 30 / 14 | **24 / 18** | 16 |
| Folk | 6 / 10 | 12 / 10 | 14 / 18 | 10 / 20 | **14 / 14** | 10 |
| Comedy | 22 / 12 | 10 / 2 | 6 / 10 | 6 / 6 | **2 / 6** | 12 |
| Easy Listening | 4 / 10 | 8 / 12 | 10 / 12 | 18 / 14 | **16 / 8** | 10 |
| Store LPs 1960 / 1961 (M) | 42.6 / 66.7; 38.1 / 65.2 | 43.4 / 65.6; 39.7 / 65.8 | 42.6 / 66.2; 38.2 / 64.1 | 42.8 / 64.7; 38.1 / 63.3 | **42.7 / 63.0; 38.2 / 63.4** | – / 68 |
| Labels defunct | 107; 131 | 124; 137 | 120; 135 | 120; 136 | **115; 149** | |

- **The tail does what it was built for.** 1960's top-10 soundtracks now run 43–55 weeks against the design's 40–60
  (23–35 before), and 83–100% are still charting in 1961. Breadth does not need to be 3 to get there.
- **Best arm: breadth 1, R 0.25.** The soundtrack total is 25 against 24 (two-seed mean), and 9 points first charted
  before 1961 against Billboard's ≈10 from 1960 releases, which is the half the tail was meant to fill. The 1956–59 half
  (≈10 points) is the opening catalogue's job.
- **Breadth 2 overshoots** (35 mean) through 1961 releases. Breadth 1.5 and breadth 1 at R 0.1 fall short on both
  carry-over and chart life (seed 2002's hits at b1 R .1 run a median 18 weeks).
- **1961 releases are still over.** 16 points against Billboard's ≈4 on both seeds of the best arm. 1961 hits renew
  too, so they hold their place through the year. The opening catalogue will add ≈10 more old points, and the total
  stays near 24 only if those displace 1961 releases.
- **Comedy is the casualty** (2 / 6 against 12, from 22 / 12). Longer-running soundtracks take the adult head room.
- **Cost.** Store LPs in 1961 are −3.7 / −1.8M against §11. Defunct labels are +8 / +18. The seed 1001 rise is larger
  than §11's moves and needs a third seed before the arm is adopted.

**Next.** The opening soundtrack catalogue (cast and film albums released 1956–59 at world start) on b1 R .25, with
the hit tail keeping them alive. Watch whether old titles push out 1961 releases or add on top of them.
Reproduction: `-Extra` = §11 candidate flags + `--album-chart-reporting=Country=0.5,--soundtrack-hit-tail=R`
[`,--soundtrack-pool-breadth=F`] (`sht-b1r25`, `sht-b2r10`, `sht-b15r10`, `sht-b1r10`), then
`python ../SimTools/analyze_soundtrack_carryover.py <run>-<seed>-sc ...` and `analyze_hit_package_demand.py`.

## 13. An opening soundtrack catalogue (2026-11-16)

Base: the §12 best arm (§11 candidate + `--soundtrack-hit-tail=0.25`, breadth 1). Seeds 2002 / 1001, 104 weeks.
Nothing here is a default.

**The switch.** `--opening-soundtrack-catalogue=N` (`OpeningSoundtrackCatalogue`,
`CompetitorManager.SeedOpeningSoundtrackCatalogue`). The first live week mints N soundtracks released 26–210 weeks
before the start (`ExternalMediaService.GenerateOpeningProfile`):
- 60% cast albums and 40% film scores, with box office 0.7–0.95. The first title is a blockbuster (0.85–1.0), which
  does not count against the run's decade cap.
- Peak 1–10, with a chart history of 60–95% of their age.
- Record and regional awareness at their premiere buzz, and half of each region's soundtrack pool already sold.
- No licence fee (it was paid before 1960). The licensee is picked as for any soundtrack (`SelectLabel`).

The hit tail keeps their audience renewing; without it they would be spent. `analyze_soundtrack_carryover.py` finds
them as the soundtracks that chart by week 4 with 100k+ units already sold (`records.csv` logs no albums). On
`osc-n8-2002` one title first charts in week 10 and is counted as a 1960 release.

Year-end top 50 %, 1961 (seed 2002 / 1001):

| | hit tail only (§12) | **N 5** | N 8 | Billboard |
|---|---|---|---|---|
| Soundtrack / cast | 28 / 22 | **22 / 28** | 28 / 40 | 24 |
| … opening (1956–59) titles | 0 / 0 | **6 / 6** | 8 / 12 | ≈10 |
| … first charted in 1960 | 12 / 6 | **8 / 14** | 12 / 14 | ≈10 |
| … first charted in 1961 | 16 / 16 | **8 / 8** | 8 / 14 | ≈4 |
| Opening titles: best peak 1960 / 1961 | – | **1 / 1** both seeds | 1 / 1 both seeds | |
| Opening titles: chart weeks 1960 / 1961 | – | **232 / 166; 220 / 174** | 290 / 206; 373 / 291 | |
| 1960 top-10 soundtracks: median chart weeks | 52 / 43 | **65.5 / 75** | 83.5 / 68 | 40–60 (design) |
| Soundtracks in the 1960 top 50 | 20 / 14 | **20 / 30** | 26 / 34 | 7–14 (author) |
| TradPop | 16 / 26 | **30 / 16** | 24 / 10 | 22 |
| Youth + R&B | 24 / 18 | **20 / 14** | 14 / 16 | 16 |
| Folk | 14 / 14 | **10 / 6** | 8 / 4 | 10 |
| Comedy | 2 / 6 | **2 / 16** | 16 / 14 | 12 |
| Easy Listening | 16 / 8 | **10 / 12** | 10 / 12 | 10 |
| Country | 0 / 4 | **0 / 0** | 0 / 0 | 2 |
| Sum of gaps to Billboard (these seven rows, two-seed means) | 21 | **11** | 26 | |
| Store LPs 1960 / 1961 (M) | 42.7 / 63.0; 38.2 / 63.4 | **44.5 / 63.3; 42.2 / 65.9** | 46.7 / 63.8; 41.9 / 63.9 | – / 68 |
| Labels defunct | 115; 149 | **123; 140** | 111; 141 | |

- **The opening titles behave like the 1956–59 cast albums.** Each run has a #1 among them in both 1960 and 1961,
  and five titles chart about 45 weeks each in 1960 and 35 in 1961.
- **They displace 1961 releases rather than add on top.** The 1961-release share halves (16 → 8 on both seeds at
  N 5), and the soundtrack total stays at 25 against 24. That is the age mix §11 asked for: 6 / 11 / 8 points for
  1956–59, 1960 and 1961 against Billboard's ≈10 / 10 / 4.
- **N 8 overshoots** (34 mean, 40 on seed 1001). Its opening share (10) is right, but the 1960 releases are still
  over. N 5 is under on opening titles (6 against 10) and right on the total.
- **N 5 halves the gap to Billboard** across the seven rows (21 → 11). Comedy and TradPop swing between seeds, but
  their two-seed means are close.
- **1960 is now over on soundtracks** (20 / 30 against the author's 7–14). The old titles add to 1960's head, and
  1960's own hits run 65–75 weeks, above the design's 40–60. R 0.25 may be too strong once the catalogue carries the
  old share. R 0.15–0.2 with N 5–6 is the next screen.
- **Cost.** Store LPs are +1.8 / +4.0M in 1960 (the old titles sell) and flat-to-up in 1961. Defunct labels are
  123 / 140 against §12's 115 / 149, inside the noise. The opening titles carry no licence fee, and their licensees
  (mostly Majors) get their sales.

**Next.** N 5–6 with the hit tail at R 0.15–0.2, to bring 1960's soundtrack share and its hits' chart life back
toward the design without losing the 1961 carry-over. Then a third seed.
Reproduction: `-Extra` = §12 flags + `--opening-soundtrack-catalogue=N` (`osc-n5`, `osc-n8`), analysis as §12.

**Billboard, 4 April 1960 (p. 34, worldradiohistory.com).** In 1960 Billboard printed two LP charts per configuration:
"Action Albums" (on the chart 39 weeks or less, top 40 mono) and "Essential Inventory" (40+ weeks, top 25 mono).
Cast and film albums on the mono lists:
- Action (3 of 40): *The Sound of Music* cast #1 (16 weeks; released Nov 1959), *Porgy and Bess* film (38 weeks),
  *Gypsy* cast (38 weeks).
- Essential Inventory (7 of 25): *South Pacific* film #2 (106 weeks), *My Fair Lady* cast #4 (209), *Gigi* film #5
  (92), *The Music Man* cast #7 (110), *South Pacific* cast #14 (305), *The King and I* film #18 (180), *Flower Drum
  Song* cast #19 (54).

That is 10 of 65 titles (15%), and four of the top seven long-runners. Stereo adds *Fiorello!*, *Oklahoma!* and *The
Eddy Duchin Story*. **Every soundtrack charting in spring 1960 is a 1949–59 release.** A merged top 50 would hold
about 8–10 of them (15–20%). So the 1960 target is about 15–20%, nearly all of it old titles, not the 7–14% design
value. The model's 1960 excess is in **1960 releases**: N 5 has 5 / 10 of them in the 1960 top 50 against about 0–2
historically, while its 5 opening titles are fewer than Billboard's 8–10 active ones. That points to N about 8 with
a lower tail rate (R 0.15), not N 5–6. One spring week cannot show the late-1960 releases (*Can-Can*, *Bye Bye
Birdie*, *Exodus*, *Camelot*) that carry 1961.

**Billboard's 1960 year-end list** ("Chart Toppers of 1960", 19 Dec 1960 Record Programming issue, p. 23: best-selling
mono LPs, a recap of the Action and Essential Inventory charts, January–November). Cast and film albums, 8 of 50
(16%): *The Sound of Music* cast #1 (Nov 1959), *South Pacific* film #8 (1958), *My Fair Lady* cast #22 (1956),
*Gigi* film #29 (1958), *The Music Man* cast #31 (1957), *Can-Can* film #33 (1960), *South Pacific* cast #36 (1949),
*Ben-Hur* (Rome Symphony / Savina, M-G-M) #43 (1959–60). That is 6–7 titles released before 1960 and 1–2 from 1960.

Classified as §9 (my calls; Mitch Miller sing-alongs, Darin's standards LPs and RCA's *Sixty Years* sampler as
TradPop, Belafonte as Folk, Mancini's *Mr. Lucky* as Easy Listening):

| % of top 50 | 1960 |
|---|---|
| Soundtrack / cast | 16 |
| TradPop | 28 |
| Folk (Kingston Trio ×5, Belafonte) | 14 |
| Youth + R&B | 14 |
| Comedy (Berman ×3, Newhart, Gardner, Woodbury) | 12 |
| Easy Listening | 6 |
| Classical / religious | 6 |
| Country | 2 |
| Children's (Chipmunks) | 2 |

Against the model at N 5 (seed 2002 / 1001):
- **The soundtrack total is over** (20 / 30 against 16).
- **The opening titles are about right in number.** N 8 places 6 / 8 of them in the 1960 top 50, against Billboard's
  6–7.
- **The 1960 releases are the miss** (5 / 10 in the top 50 against 1–2). They are over in every run so far, before
  the tail too (tail only 10 / 7; §11 breadth 3 about 9 / 9). So this excess comes mostly from how hard and how often
  a new soundtrack launches, not from the tail. A lower R trims only part of it. The rest needs a lower origination
  rate or a weaker launch for 1960 releases.

Billboard's 1960 releases carry 1961 (*Can-Can*, *Exodus*, *Camelot*), so they were few and late in 1960 and big in
1961.

## 14. N 8, tail R 0.15 and fewer early originations (2026-11-16)

Base: §13 flags with `--opening-soundtrack-catalogue=8 --soundtrack-hit-tail=0.15`. New switch
`--soundtrack-origination-early=F` (`SoundtrackOriginationEarly`): soundtrack originations in 1960–61 × F, later
years untouched. Seeds 2002 / 1001, 104 weeks. Nothing here is a default.

| | N 5, R .25 (§13) | N 8, R .15 | N 8, R .15, origination × 0.5 | Billboard |
|---|---|---|---|---|
| 1960 top 50: soundtrack % | 20 / 30 | 26 / 36 | 20 / 34 | 16 |
| … opening titles / 1960 releases (titles) | 5 / 5; 5 / 10 | 6 / 7; 8 / 10 | 6 / 4; 8 / 9 | 6–7 / 1–2 |
| 1961 top 50: soundtrack % | 22 / 28 | 30 / 26 | 20 / 28 | 24 |
| … opening / 1960 / 1961 releases | 6 / 8 / 8; 6 / 14 / 8 | 8 / 10 / 12; 10 / 8 / 8 | 8 / 4 / 8; 10 / 10 / 8 | ≈10 / 10 / 4 |
| New 1960 soundtracks charting (reaching top 10)* | 20 (7); 24 (11) | 27 (9); 18 (10) | 12 (5); 17 (9) | 1–2 top 10 |
| Gap to Billboard, seven rows, 1960 / 1961 | 26 / 11 | 35 / 18 | 32 / 19 | |

\* Corrected 2026-11-16. The first version of this row had opening titles leaking into the count (a positional mask
misaligned against a record-indexed series in an ad-hoc script); the shared `analyze_soundtrack_carryover.py` rows
were not affected.
| Store LPs 1960 / 1961 (M) | 44.5 / 63.3; 42.2 / 65.9 | 46.2 / 65.0; 41.1 / 62.9 | 44.7 / 62.2; 41.9 / 63.5 | |
| Labels defunct | 123; 140 | 120; 138 | 118; 143 | |

- **N 8 gets the old titles right.** 6 / 8 make the 1960 top 50 (Billboard 6–7) and 8 / 10 points of 1961's.
- **Halving early originations halves the charting new titles, but less so the hits.** New 1960 soundtracks charting
  fall 27 → 12 and 18 → 17, while those reaching the top 10 fall 9 → 5 and 10 → 9. On seed 1001 the 1960 top 50 still
  holds 9 new 1960 soundtracks.
- **So the 1960 excess is mostly the soundtrack launch, not the count.** Every arm puts 5–11 new soundtracks a year
  into the album top 10, against Billboard's one or two in 1960 (*Can-Can*). A soundtrack launches with awareness at
  its premiere buzz (`sourcePopularity`, 0.3–0.8; `CompetitorManager.MintSoundtrackRecord`), against 0.06–0.20 for an
  ordinary LP (`ChartManager.ReleaseRecord`).
- **Neither arm beats N 5, R .25 on the gap sums.** Seed noise in the soundtrack share is ±5–10 points, so two seeds
  cannot rank arms closer than that.

**Next.** A soundtrack launch-awareness scale (premiere buzz × k), screened with N 8 and the full origination rate.
The target is 1–3 new top-10 soundtracks a year, not 10–18. Then three seeds on the best arm.
Reproduction: `-Extra` = §11 candidate flags + `--album-chart-reporting=Country=0.5,--soundtrack-hit-tail=0.15,
--opening-soundtrack-catalogue=8[,--soundtrack-origination-early=0.5]` (`osc-n8r15`, `osc-n8r15o5`).

## 15. Soundtrack launch awareness (2026-11-16)

Base: §14's N 8, R .15 (full origination). New switch `--soundtrack-launch-awareness=k`
(`SoundtrackLaunchAwareness`): a new soundtrack's launch awareness is premiere buzz × k. Opening titles are untouched.
Seeds 2002 / 1001, 104 weeks. Nothing here is a default.

| | k 1 (§14) | k 0.5 | **k 0.25** | Billboard |
|---|---|---|---|---|
| New soundtracks reaching the top 10, 1960 | 9 / 10 | 9 / 6 | **5 / 7** | 1–2 |
| … 1961 | 5 / 6 | 5 / 9 | **3 / 3** | |
| New soundtracks charting, 1960 / 1961 | 27, 29 / 18, 29 | 27, 26 / 15, 27 | **19, 18 / 13, 17** | |
| 1960 top 50: soundtrack % | 26 / 36 | 28 / 26 | **20 / 28** | 16 |
| … opening titles / 1960 releases (titles) | 6, 7 / 8, 10 | 6, 8 / 8, 5 | **6, 4 / 8, 6** | 6–7 / 1–2 |
| 1961 top 50: soundtrack % | 30 / 26 | 30 / 32 | **22 / 26** | 24 |
| … opening / 1960 / 1961 releases | 8 / 10 / 12; 10 / 8 / 8 | 8 / 12 / 10; 12 / 2 / 18 | **8 / 6 / 8; 10 / 6 / 10** | ≈10 / 10 / 4 |
| Gap to Billboard, seven rows, 1960 / 1961 | 35 / 18 | 28 / 24 | **22 / 14** | |
| Store LPs 1960 / 1961 (M) | 46.2 / 65.0; 41.1 / 62.9 | 46.5 / 63.4; 41.2 / 63.4 | **45.7 / 65.5; 42.1 / 64.8** | – / 68 |
| Labels defunct | 120; 138 | 110; 138 | **127; 145** | |

- **k 0.25 is the best arm so far over both years** (gap 22 / 14; N 5, R .25 in §13 was 26 / 11). The 1961
  soundtrack total is 24 against 24, and its old titles are 9 against ≈10.
- **Fewer new soundtracks chart, and fewer reach the top 10.** In 1960 the top-10 count goes 9 → 5 and 10 → 7, and
  in 1961 5 → 3 and 6 → 3. It is still 3–5 times Billboard's one or two.
- **The age mix in 1961 moves the right way but not far enough.** 1960 releases are under (6 against ≈10) and 1961
  releases still over (8–10 against ≈4).
- **k 0.5 is not between k 1 and k 0.25 on every row.** Seed noise (±5–10 points of soundtrack share) is as large as
  the step.
- **Cost.** Store LPs are flat to +2M. Defunct labels are +7 / +7 against k 1, inside the noise.

**Next.** k 0.1–0.15 to reach 1–2 new top-10 soundtracks in 1960, then three seeds on the best arm.
Reproduction: §14 flags + `--soundtrack-launch-awareness=k` (`sla-k50`, `sla-k25`).

## 16. Launch awareness k 0.15 and 0.10: the switch floors out (2026-11-16)

`sla-k15` and `sla-k10` (§15 base) are **byte-identical** on both seeds (album chart files compared). The launch seed
is `max(ordinary LP launch awareness, premiere buzz × k)`, and at k ≤ 0.15 premiere buzz × k (at most about 0.12)
falls below the ordinary LP launch value, so the soundtrack term never binds. The switch's working range is k ≥ about
0.2. k 0.25 differs from k 0.15 in the titles drawn (one conversion reshuffles the stream), not in a measurable trend.

| | k 0.25 (§15) | k ≤ 0.15 | Billboard |
|---|---|---|---|
| New soundtracks charting / reaching top 10, 1960 | 19 / 5; 13 / 7 | 19 / 6; 15 / 6 | 1–2 top 10 |
| … 1961 | 18 / 3; 17 / 3 | 18 / 10; 24 / 10 | |
| 1960 top 50: soundtrack % (opening, 1960-release titles) | 20 (6, 4); 28 (8, 6) | 18 (6, 3); 24 (8, 4) | 16 (6–7, 1–2) |
| 1961 top 50: soundtrack % | 22 / 26 | 24 / 30 | 24 |
| Gap to Billboard, seven rows, 1960 / 1961 | 22 / 14 | 19 / 14 | |
| Store LPs 1960 / 1961 (M) | 45.7 / 65.5; 42.1 / 64.8 | 45.5 / 64.7; 42.0 / 65.2 | |
| Labels defunct | 127; 145 | 121; 130 | |

- **5–7 new soundtracks still reach the 1960 top 10 with the launch seed at the ordinary LP value.** So what remains
  is not launch buzz but the soundtrack's own demand path: a buyer pool drawn from the adult and family segments
  regardless of genre (`MarketRegion.GetSoundtrackAlbumMarketSize`), appeal 0.5 + 0.35 × box office
  (`ExternalMediaService.PooledAppeal`), and the box-office multiplier. Each of these is a candidate. A weekly trace of
  the 1960 top-10 soundtracks against ordinary top-10 LPs (pool, awareness, conversion) should come before another
  switch.
- **The two arms are tied on the gap sums** (19–22 / 14). The 1960 soundtrack share is within 2–8 points of
  Billboard's 16, and 1961 within 0–6 of 24.
- **Candidate for a three-seed check:** N 8, R .15, k 0.25 (the lowest k at which the switch still acts).

## 17. The soundtrack demand path: a genre format tilt on a bypass pool (2026-11-16)

Trace: the §16 candidate (N 8, R .15, k 0.25) with `--album-demand-trace`, which now also logs appeal, regional
awareness and blended awareness (`slt-k25`, seeds 2002 / 1001; album charts byte-identical to `sla-k25`, so the
trace is inert). `analyze_soundtrack_demand_trace.py` compares the new 1960 soundtracks that reached the top 10
(5 + 7) with the ordinary 1960 LPs that did (110 + 113), by weeks since release.

Soundtrack ÷ ordinary, medians:

| Weeks | Store units | Raw demand | Pool | Blended awareness | Appeal | Conversion | Format tilt | Exhaustion | Decay |
|---|---|---|---|---|---|---|---|---|---|
| 1–4 | 0.84 | 1.57 | 1.10 | 0.62 | 1.01 | 2.61 | **1.82** | 1.20 | 0.92 |
| 5–8 | 2.04 | 1.71 | 1.03 | 1.00 | 0.99 | 2.04 | **1.82** | 1.07 | 0.88 |
| 9–12 | 1.81 | 1.69 | 1.03 | 0.65 | 1.00 | 1.94 | **1.79** | 0.95 | 1.02 |
| 13–20 | 2.07 | 2.53 | 1.01 | 0.71 | 1.00 | 3.55 | **1.79** | 1.14 | 1.38 |

- **Pool and appeal are not the cause.** A top-10 soundtrack's buyer pool is within 10% of a top-10 ordinary LP's,
  and its appeal is the same (0.73–0.74). Its awareness is lower (0.6–0.8×).
- **The format tilt is.** A soundtrack's store conversion carries the format tilt of its *mapped* genre
  (`GenreAcceptanceService.GetLiveFormatMultiplier`, `AlbumSimulator.CalculateRegionalSales`), even though its buyer
  pool bypasses that genre (`GetSoundtrackAlbumMarketSize`). Film scores map 60% to Classical, whose tilt is 1.72.
  Comedy's is 1.42, Easy Listening's 1.22 and TradPop's 1.07. So the 1960 top-10 soundtracks run at a median 1.72
  against 0.94–0.96 for the ordinary top-10 LPs. For an ordinary LP the tilt reshapes a genre pool; for a soundtrack
  it is applied to a demographic pool the genre never shaped, which counts it twice.
- **The rest of the conversion gap** (2.0–3.5× against a 1.8× tilt) is the box-office decay running slower than
  catalogue decay after week 12 (by design: 1.4× at 13–20 weeks, 3× at 21–40) and exhaustion 1.1–1.2× in the
  first month. Rack sales follow store standing (rack conversion has no tilt), so racks amplify the same gap.

**Next.** A switch that gives a soundtrack with a media profile a neutral format tilt (1.0, or a value F), screened
on the candidate. It should cut new top-10 soundtracks toward Billboard's one or two without touching the opening
titles' 1961 carry-over much, since their run is set by box office and the hit tail.

## 18. A neutral format tilt for soundtracks (2026-11-16)

Base: §16 candidate (N 8, R .15). New switch `--soundtrack-format-tilt=F` (`SoundtrackFormatTilt`): a soundtrack with
a media profile takes tilt F in its store conversion, in place of its mapped genre's. Arms at F 1.0, with and without
k 0.25 (`sft-t10k25`, `sft-t10k1`, with `--album-demand-trace`). Seeds 2002 / 1001, 104 weeks. Gap sums from the new
`analyze_album_head_gap.py`.

| | k 0.25, genre tilt (§16) | **tilt 1.0, k 0.25** | tilt 1.0, k 1 | Billboard |
|---|---|---|---|---|
| New soundtracks reaching the top 10, 1960 | 5 / 7 | **4 / 8** | 6 / 11 | 1–2 |
| … 1961 | 3 / 3 | **2 / 6** | 4 / 5 | |
| 1960 soundtrack % (opening, 1960-release titles) | 20 (6, 4); 28 (8, 6) | **14 (5, 2); 30 (8, 7)** | 20 (5, 5); 30 (8, 7) | 16 (6–7, 1–2) |
| 1961 soundtrack % | 22 / 26 | **16 / 28** | 20 / 26 | 24 |
| 1961 TradPop % | 24 / 12 | **40 / 28** | 32 / 28 | 22 |
| Gap to Billboard, seven rows, 1960 / 1961 | 22 / 14 | **26 / 24** | 30 / 24 | |
| Store LPs 1960 / 1961 (M) | 45.7 / 65.5; 42.1 / 64.8 | **45.6 / 64.9; 41.1 / 64.2** | 45.1 / 63.0; 41.8 / 65.2 | |
| Labels defunct | 127; 145 | **105; 143** | 121; 133 | |

- **The tilt is gone from the trace, and with it the soundtrack's edge.** Tilt now reads 1.0–1.06. With it neutral,
  a new top-10 soundtrack's conversion is exactly a Major LP's: the residual after appeal, exhaustion, decay and tilt
  is 0.214 for both. Every new soundtrack is on a Major (`ExternalMediaService.SelectLabel` ranks by tier and
  capital), and charting Major soundtracks reach the top 10 *less* often than charting ordinary Major LPs (33%
  against 48%, appeal 0.69 against 0.72).
- **So what is left is volume.** About 18 new soundtracks a year chart in 1960, all on Majors. At a Major's top-10
  rate that is 4–8 top-10 entries against Billboard's one or two. Origination count is now the lever; §14's
  origination cut ran while the genre tilt still made almost every launch a hit.
- **The gap sums got worse in 1961** (14 → 24). The head room soundtracks gave up went to TradPop (40 / 28 against
  22), and seed 2002's 1960 soundtracks ran short (top-10 median 13.5 weeks), which cost the 1961 carry-over.
- **k matters less once the tilt is neutral** (k 1 against k 0.25: within seed noise).

**Next.** Tilt 1.0 with `--soundtrack-origination-early=0.5`, then three seeds on whichever arm leads. The 1961
TradPop excess on this base needs watching; it may be a seed effect (seed 2002 TradPop swings 24–40 across arms).

## 19. Heavy 53-week probes on the 1960 soundtrack share (2026-11-16)

Base (`pr-base`): §18's tilt 1.0, k 0.25, N 8, R .15. Each probe makes one heavy change; 53 weeks (all of 1960),
seeds 2002 / 1001. `pr-base` reproduces the 1960 of the 104-week `sft-t10k25` exactly (same top-50 split and
top-10 counts), so these probes are prefixes of the long runs. Rack weight is overridden through a copied reference
request (`--rack-soundtrack-weight=0.1`), because the first copy of a flag wins. "Old" = opening titles in the top
50; "new" = 1960 soundtracks in the top 50. One or two late-charting opening titles leak into "new" (on `pr-o0`,
which mints no new soundtracks, 2 still show).

| Probe | 1960 soundtrack % (2002 / 1001; mean) | Old / new in top 50 | New soundtracks reaching top 10 | Gap, seven rows |
|---|---|---|---|---|
| base | 14 / 30; 22 | 5, 2 / 8, 7 | 4 / 8 | 26 |
| origination × 0 | 10 / 16; 13 | 5, 0 / 8, 0 | 1 / 0 | 22 |
| **origination × 0.25** | **10 / 22; 16** | **5, 0 / 8, 3** | **1 / 3** | **21** |
| **format tilt 0.6** | **10 / 14; 12** | **4, 1 / 6, 1** | **2 / 3** | **22** |
| opening catalogue N 4 | 12 / 16; 14 | 4, 2 / 4, 4 | 3 / 4 | 19 |
| hit tail off | 14 / 20; 17 | 2, 5 / 4, 6 | 9 / 5 | 14 |
| rack soundtrack weight 0.1 | 18 / 24; 21 | 5, 4 / 8, 4 | 5 / 5 | 23 |
| Billboard 1960 | 16 | 6–7, 1–2 | 1–2 | |

- **Two levers move the 1960 soundtracks the right way:** fewer originations and a format tilt below 1. Origination
  × 0.25 hits the total (16) and keeps the old titles whole, since they are not originations. Tilt 0.6 cuts deeper (12)
  and also trims the old titles (they are soundtracks too).
- **N 4 and turning the tail off move the wrong part.** They cut the old titles (Billboard's 6–7 become 2–4) and
  leave 4–6 new ones. The tail is what keeps the opening titles alive through 1960. "Tail off" has the best gap
  (14) only through other rows (TradPop, Comedy), not through the soundtrack mix.
- **Rack weight 0.1 does almost nothing** (21 against 22).
- **Caution for 1961.** The origination cut runs through 1961, and Billboard's 1961 needs about 10 points from 1960
  releases. With only 4–5 new soundtracks charting in 1960, that carry-over may thin. A 104-week run on the two leading
  probes answers it.

## 20. Origination 0.25 and tilt 0.6 at 104 weeks: 1960 fixed, 1961 lost (2026-11-16)

The two §19 leaders at 104 weeks (`lg-o25`: tilt 1.0 + origination × 0.25 through 1961; `lg-t06`: tilt 0.6), seeds
2002 / 1001. Their 1960 matches the probes.

| | tilt 1.0 (§18) | origination × 0.25 | tilt 0.6 | Billboard |
|---|---|---|---|---|
| 1960 soundtrack % | 14 / 30 | 10 / 22 | 10 / 14 | 16 |
| 1961 soundtrack % | 16 / 28 | **10 / 14** | **8 / 8** | 24 |
| 1961: opening / 1960 / 1961 releases | 8 / 2 / 6; 10 / 8 / 10 | 8 / 0 / 2; 10 / 4 / 0 | 4 / 2 / 2; 4 / 0 / 4 | ≈10 / 10 / 4 |
| Gap, seven rows, 1960 / 1961 | 26 / 24 | 21 / 34 | 22 / 46 | |
| 1961 TradPop % | 40 / 28 | 38 / 18 | 46 / 26 | 22 |
| Store LPs 1960 / 1961 (M) | 45.6 / 64.9; 41.1 / 64.2 | 43.2 / 62.1; 39.4 / 62.7 | 43.2 / 61.4; 39.7 / 63.2 | – / 68 |
| Labels defunct | 105; 143 | 135; 131 | 123; 146 | |

- **Both fix 1960 and empty 1961.** The 1961 soundtrack share falls to 8–14 against 24. Origination × 0.25 keeps
  the opening titles (8 / 10 points, Billboard ≈10) but leaves no 1960 or 1961 releases. Tilt 0.6 also halves the
  opening titles' 1961 life (183–232 → 103–117 chart weeks), because they are soundtracks too.
- **Volume levers trade one year for the other.** Billboard's pattern is few 1960 releases in the 1960 top 50 (1–2)
  but about 10 points of them in 1961's. The model's soundtracks peak at launch, so a 1960 release is either in
  1960's head or not much of a carry-over. Billboard's 1960 releases in the 1961 list (*Exodus*, *Camelot*,
  *Never on Sunday*) came out in the last quarter of 1960 and peaked in 1961. *The Sound of Music* (Nov 1959) did the
  same for 1960.
- **So the remaining miss is timing.** Fall-season launches (Broadway's season, holiday and roadshow film releases)
  or a soundtrack that builds for some months after launch would put 1960 releases into 1961's head without adding
  to 1960's. Neither exists in the model: originations are uniform through the year (`OriginationsForYear / 52`
  weekly) and the box-office multiplier starts at its peak (`GetSoundtrackBoxOfficeMultiplier`, ramp 0.85 → 1 over
  3 weeks only).
- **The base (§18, tilt 1.0) is still the best two-year arm** on soundtrack share (22 / 22 means against 16 / 24).
  Its miss is the age mix inside each year, not the totals.

## 21. Timing: a fall origination season and a box-office build (2026-11-16)

Base: §18's tilt 1.0, k 0.25, N 8, R .15 (`sft-t10k25`). New switches:
- `--soundtrack-origination-season=S` (`SoundtrackOriginationSeason`): the weekly origination rate is
  × 1 + S·cos(2π(month − 11) / 12), with a November peak, a May trough and a mean of 1.
- `--soundtrack-build-weeks=B` (`SoundtrackBuild`): a soundtrack's box-office multiplier starts at 0.35 and rises
  linearly to 1 by week B.

Seeds 2002 / 1001, 104 weeks (`tm-s09`, `tm-b16`, `tm-s09b16`).

| | base | **season 0.9** | build 16 | season 0.9 + build 16 | Billboard |
|---|---|---|---|---|---|
| 1960 soundtrack % | 14 / 30 | **14 / 24** | 14 / 16 | 12 / 18 | 16 |
| … opening / 1960-release titles | 5, 2 / 8, 7 | **5, 2 / 8, 4** | 5, 2 / 8, 0 | 5, 1 / 8, 1 | 6–7 / 1–2 |
| 1961 soundtrack % | 16 / 28 | **20 / 26** | 10 / 18 | 8 / 12 | 24 |
| 1961: opening / 1960 / 1961 releases | 8, 2, 6 / 10, 8, 10 | **8, 6, 6 / 10, 8, 8** | 8, 0, 2 / 12, 0, 6 | 8, 0, 0 / 10, 2, 0 | ≈10 / 10 / 4 |
| New soundtracks charting / top 10, 1960 | 17/4; 19/8 | **12/5; 21/8** | 9/1; 12/0 | 5/1; 16/2 | 1–2 top 10 |
| … 1961 | 19/2; 22/6 | **25/5; 15/5** | 14/1; 14/1 | 11/1; 11/0 | |
| Gap, seven rows, 1960 / 1961 | 26 / 24 | **20 / 22** | 19 / 36 | 23 / 37 | |
| Store LPs 1960 / 1961 (M) | 45.6 / 64.9; 41.1 / 64.2 | **44.0 / 63.0; 40.7 / 63.8** | 44.5 / 63.9; 40.7 / 64.0 | 43.8 / 64.6; 40.4 / 64.5 | |
| Labels defunct | 105; 143 | **110; 134** | 113; 136 | 123; 132 | |

- **The fall season moves the age mix the right way.** 1960 soundtracks fall from a 22 mean to 19 (Billboard 16).
  1961 rises to 23 (24). Within 1961, 1960 releases carry 6 / 8 points against the base's 2 / 8 (Billboard ≈10), and
  1961 releases fall to 6 / 8 (≈4). It has the lowest gap of any arm on this base (20 / 22).
- **The build removes soundtracks rather than delaying them.** Starting at 35% of demand, a new soundtrack charts low
  and seldom reaches the top 10 (0–1 a year), so the hit tail, which needs a top-10 peak, never renews it. Lost early
  sales are not made up later. 1961 falls to 10–18. With the season it is worse still (8 / 12).
- **New top-10 soundtracks in 1960 are unchanged by the season** (5 / 8). It moves when they launch, not how many
  hit. That excess is the volume question from §18–20.

**Candidate.** Tilt 1.0, k 0.25, N 8, R .15, season 0.9. Next: three seeds (add 3003), 104 weeks.
Reproduction: `-Extra` = §11 candidate flags + `--album-chart-reporting=Country=0.5,--soundtrack-launch-awareness=0.25,
--soundtrack-hit-tail=0.15,--opening-soundtrack-catalogue=8,--soundtrack-format-tilt=1.0,
--soundtrack-origination-season=0.9`.

## 22. Three seeds on the season candidate (2026-11-16)

Seed 3003 added for the candidate (`tm-s09`, §21) and its base without the season (`sft-t10k25`, tilt 1.0); 104
weeks. Seeds 2002 / 1001 / 3003. Code as committed in `ed4b3fc`.

| | base, tilt 1.0 | **season 0.9** | Billboard |
|---|---|---|---|
| 1960 soundtrack % | 14 / 30 / 18 (21) | **14 / 24 / 16 (18)** | 16 |
| 1961 soundtrack % | 16 / 28 / 24 (23) | **20 / 26 / 20 (22)** | 24 |
| 1961: opening titles | 8 / 10 / 10 (9.3) | **8 / 10 / 10 (9.3)** | ≈10 |
| 1961: 1960 releases | 2 / 8 / 4 (4.7) | **6 / 8 / 8 (7.3)** | ≈10 |
| 1961: 1961 releases | 6 / 10 / 10 (8.7) | **6 / 8 / 2 (5.3)** | ≈4 |
| New soundtracks reaching top 10, 1960 | 4 / 8 / 7 | **5 / 8 / 10** | 1–2 |
| Gap, seven rows, 1960 / 1961 | 24 / 24 | **24 / 26** | |
| Store LPs 1960 / 1961 (M) | 45.6 / 64.9; 41.1 / 64.2; 40.3 / 58.0 | **44.0 / 63.0; 40.7 / 63.8; 40.6 / 60.0** | – / 68 |
| Labels defunct | 105; 143; 149 | **110; 134; 134** | |

- **The season improves the 1961 age mix on all three seeds.** 1960 releases carry more (+4, 0, +4) and 1961
  releases hold less (0, −2, −8). The three-seed mix is 9.3 / 7.3 / 5.3 against Billboard's ≈10 / 10 / 4. The
  soundtrack totals are 18 and 22 against 16 and 24.
- **The gap sums do not show it** (24 / 24 against 24 / 26). They are driven by other rows that swing by seed:
  TradPop 40 on seed 2002 in 1961, Youth + R&B 34 on seed 3003, Comedy under on most seeds, Easy Listening under in
  1961. Those are the next misses, not the soundtrack rows.
- **New top-10 soundtracks in 1960 are still 5–10 against 1–2**, the volume question from §18–20. The season does
  not touch it.
- **Cost.** Store LPs are within −1.6 to +2.0M of the base, and defunct labels are +5 / −9 / −15.

**Status.** The soundtrack thread has a candidate: §11 candidate flags + `--album-chart-reporting=Country=0.5
--soundtrack-launch-awareness=0.25 --soundtrack-hit-tail=0.15 --opening-soundtrack-catalogue=8
--soundtrack-format-tilt=1.0 --soundtrack-origination-season=0.9`. It has not been checked past 1961 (a decade A/B
belongs at the audit's end).

**Adopted into the candidate (2026-11-16, author's call).** `SimLogs/writing-calibration/candidate-request.json` now
carries the `tm-s09-2002-sc` flag list in its order (237 flags; `--weeks=208`, `--run=candidate`): the Major-LP bundle,
the affinity set, the rack soundtrack weight, hit-package demand, Country chart reporting 0.5, the soundtrack set and
`--album-chart-history`. Its `--lp-demand-scale` is 2.2. A 104-week run of it reproduces `tm-s09`. The previous file
is `candidate-request-pre-soundtrack-2026-11-16.json`. `SimLogs/working-bundle/plan.json` (the October game-profile
bundle) is unchanged.

## 23. LP chart health against Billboard (2026-11-16)

The author asked why 5–10 new soundtracks a year reach the 1960 album top 10 against Billboard's one or two, and
then for the LP chart's health as a whole, allowing for the chart's growth over the decade. Runs: the candidate
(`tm-s09-{2002,1001,3003}-sc`, 104 weeks) for 1960–61, `cbg-v1-{2002,1001}-sc` (208 weeks, the same base without the
hit-package and soundtrack flags) for 1962–63 (replaced by the candidate's own 208-week runs `lch-cand-{2002,1001}-sc` once they finished), and
`fc8x26-2002-sc` against `cbg-v2b-2002-sc`. New script
`analyze_album_chart_health.py`. Nothing here changes a default.

### The soundtracks are a symptom

The album chart ranks on raw weekly store units (`AlbumSimulator.CalculateChartPoints`). In 1960 the #10 album sells a
median 8.7–9.2k a week, and the #1 sells 23–32k. The new soundtracks that reach the top 10 do it with 6–16k in their
second or third chart week. By peak weekly units they rank 62nd to 227th among 1960's charting albums; none is in the
50 biggest. They get in because the bar is low, and the bar is low because nothing holds the top 10: 84% of top-10
albums reach it in their first two chart weeks and stay a median 3 weeks. Soundtracks are 3–8% of the 119–143
titles that reach the 1960 top 10. Billboard's one or two of roughly 50 is 2–4%. So the soundtrack rate per entrant is
at most about twice history; the count is high mainly because the whole top 10 turns over about three times too fast.
That is why the soundtrack-only levers (§18–20: origination, tilt) fixed 1960 only by emptying 1961.

### Billboard's chart, 1960–69

| Period | Billboard (mono / stereo) | Model (`ChartManager.GetAlbumChartSize`) |
|---|---|---|
| Jan 1960 – Jan 1961 | Action Albums 40 (titles charted ≤39 weeks) + Essential Inventory 25 (40+ weeks); stereo 30 + 20 | 40, one merged chart |
| Jan – about Apr 1961 | Action Albums 25 (24) mono, 15 stereo | 40 |
| About Apr 1961 – Aug 1963 | Top LPs: mono 150, stereo 50 | **50** (1 Apr 1961) |
| 17 Aug 1963 | merged Top LPs 150 | 150 (1 Aug 1963) |
| 1 Apr 1967 / 13 May 1967 | 175 / 200 | 200 (1 May 1967) |

Sources: [Billboard 200](https://en.wikipedia.org/wiki/Billboard_200) (chart history section), Billboard 4 Jan 1960
p. 3 policy note (65 mono = 40 Action + 25 Essential Inventory; local copy `SimLogs/album-promo-research/`). The model
follows the stereo size from 1961 to 1963; the mono chart, which carried most sales, was three times longer.

### Health metrics (means across seeds)

| | 1960 | 1961 | 1962 | 1963 | Billboard |
|---|---|---|---|---|---|
| Chart size | 40 | 50 | 50 | 150 | 65 merged mono (1960); 150 mono (1962) |
| New titles a week | 5.0 | 8.0 | 9.8 | 13.5 | **5** on the 150 mono chart, 2–4 on the 50 stereo (1962) |
| Median debut position, % of chart | 44 | 31 | 32 | 25 | **70–99** (#105–148 of 150; #36–50 of 50 stereo) |
| Debuts inside the top 10, per year | 92 | 155 | 184 | 223 | about 0 (the first No. 1 debut was 1975) |
| Top-10 albums peaking in chart week 1 (median peak week) | 1 | 1 | 1 | 1 | climbed for weeks |
| Distinct titles reaching the top 10 | 130 | 193 | 236 | 286 | about 50 (estimate) |
| Median weeks in the top 10 | 3 | 2.3 | 2 | 2 | |
| Median chart run (debuts, first half-year) | 5.3 | 3.0 | 3.5 | 3.0 | mean ≈ 30 on the mono chart (150 slots ÷ 5 entries) |
| Distinct No. 1 albums | 23 | 32 | 32 | 38 | **10 / 12**; 9 in 1965; 12 in 1968 |
| Longest No. 1 run (weeks) | 7 | 3 | 5 | 3 | **13** (1960), 10+ (1961), 13 (1965) |
| Chart slots held by titles 40+ weeks on the chart | 14% | 11% | 8% | 4.5% | **38%** (25 of 65 mono listings, Jan 1960) |
| Soundtrack share of top-10 titles | 10% | 4% | 3% | 2% | 2–4% (1960) |

1960–61 are the candidate on three seeds (`tm-s09`); 1962–63 are the candidate's 208-week runs `lch-cand-{2002,1001}-sc`,
whose 1960–61 are byte-identical to `tm-s09`. The candidate's long-runners are its opening soundtracks; `cbg-v1`, which
has none, reads 0–1.5% from 1961. New-entry counts are from Billboard's "New on the Top LP's"
boxes of 21 April and 5 May 1962 (`billboard-1962-04-21.txt`, `-05-05.txt`): five mono entries each week, at #105,
111, 128, 133, 138 and #111, 115, 123, 132, 148. The No. 1 counts are from Wikipedia's yearly No. 1 lists (1960
mono 10; 1961 12 across both charts). Two issues are a thin sample for the entry rate, but they agree with each other
and with the 1960 Essential Inventory split.

**Ranking of the misses, worst first:**
1. **Shape.** Model LPs debut at their peak, inside the top half, and fall. Billboard LPs entered at the bottom and
   climbed. 92–223 top-10 debuts a year against essentially none.
2. **Turnover.** Two to three times Billboard's entry rate per slot, so runs are about a tenth as long, and 2.5–3×
   too many No. 1s, none holding more than a few weeks.
3. **Catalogue stock.** Long-runners held 38% of Billboard's 1960 listings. The model's only long-runners are the
   eight opening soundtracks; without them it is 0%.
4. **Chart size.** The model's chart is 50 from April 1961 to August 1963 against Billboard's 150 mono.
5. **Genre head mix** (§9–22) is closest to history of all of these, and soundtracks are a symptom of 1–3.

### Why: the LP chart is a launch chart

Chart points are this week's store units, and LP demand is spent at launch. §9's trace: a top-10 album sells
11.6–17.1k in week 1, 73–92% of it bought by followers, whose LP book is spent 50 / 30 / 20 over three weeks
(`MediaFollowerDemand.WeekShare`); by week 4 youth albums sell 2.2k. With 5–15 new titles a week
charting at their peak, the top 10 each week is that week's launch spikes, and #10 needs only ~9k. Billboard's chart
was compiled from dealer reports; its albums entered low and climbed, and catalogue titles held their place for
months. What built them (airplay of their singles, word of mouth, reorders, the reporting lag itself) is not
separated here.

**Existing evidence that the follower window is a lever.** `--follower-album-casual=0.8,26` (casual followers spread
over 26 weeks, §9) on seed 2002, 1960 / 1961:

| | `cbg-v2b` | `fc8x26` | Billboard |
|---|---|---|---|
| New titles a week | 6.1 / 9.6 | 4.5 / 6.6 | (5 on 150 slots) |
| Top-10 debuts | 103 / 185 | 54 / 72 | ≈0 |
| Median peak week of top-10 titles | 1 / 1 | 3 / 3 | later |
| Median chart run | 5 / 3 | 7 / 7 | ≈30 |
| Distinct No. 1s | 26 / 33 | 20 / 24 | 10 / 12 |
| Soundtrack share of top-10 titles | 9 / 4 | 13 / 7 | 2–4 |

It moves every shape metric the right way, halves top-10 debuts, and still leaves the chart several times too fast.
It also raised country and TradPop at the head (§9), so it is not a fix by itself.

### Next

1. A chart-side probe: LP chart points as a trailing blend of weekly units (points_t = λ·units_t + (1 − λ)·points_{t−1}),
   standing in for Billboard's dealer-report lag. Units and money are untouched; chart position feeds awareness, so it
   is not fully inert. Judge on the table above, then the year-end genre head.
2. A demand-side probe: launch-front-loading of non-follower LP demand (launch awareness, exhaustion), together with
   the follower spread.
3. Chart size: 150 from April 1961, which changes the Top 40/Top 10 career read (`AlbumScaleCareerMilestones`), so
   screen it with that switch in mind.
4. Re-read the soundtrack counts after 1–2. If the top 10 holds long-runners, the 5–10 soundtrack entries should fall
   without the origination cuts that emptied 1961.

Reproduction: from `SimLogs/`, `python ../SimTools/analyze_album_chart_health.py tm-s09-2002-sc tm-s09-1001-sc
tm-s09-3003-sc lch-cand-2002-sc lch-cand-1001-sc` and `... cbg-v2b-2002-sc fc8x26-2002-sc`.

## 24. A trailing-blend album chart (2026-11-16)

Follows §23's first next step. New switch `--album-chart-blend=L` (`AlbumChartBlend`, `Systems/GameyAuditSwitches.cs`,
read in `ChartManager.SimulateWeek`). Each LP's chart points become L × this week's points + (1 − L) × last week's
blend, so a launch spike enters low and a steady seller climbs to its level over about 1/L weeks. An LP first seen at 8
weeks or older (opening catalogue, opening soundtracks) is seeded at its own points; anything younger starts from 0.
Units and money are untouched, but chart position feeds awareness and career reads. The blend is runtime state and is not
saved. Off is inert: `acb-off-2002-sc` (27 weeks) matches `tm-s09-2002-sc` on 94 weekly streams; the five that differ
are close-only files, as in §6. Unit checks are in `GameyAuditSwitchChecks`.

Arms on the candidate, 104 weeks, seeds 2002 / 1001: `acb-l30` (L 0.3), `acb-l15` (L 0.15). Control `tm-s09`.

| Two-seed means, 1960 / 1961 | control | L 0.3 | L 0.15 | Billboard |
|---|---|---|---|---|
| New titles a week (chart 40 / 50) | 5.2 / 8.1 | 3.2 / 4.4 | 2.2 / 2.8 | 5 on the 150 mono chart; 2–4 on the 50 stereo (1962) |
| Median debut, % of chart | 45 / 33 | 80 / 73 | 84 / 81 | 70–99 |
| Debuts inside the top 10 | 93 / 153 | 32 / 41 | 16 / 14 | about 0 |
| Distinct titles reaching the top 10 | 131 / 194 | 71 / 79 | 49 / 55 | about 50 (estimate) |
| Distinct No. 1s | 23 / 33 | **13 / 14** | 7 / 8.5 | **10 / 12** |
| Longest No. 1 run | 8 / 3 | 14 / 11 | 21 / 12 | 13 / 10+ |
| Median weeks in the top 10 | 3 / 2.5 | 6 / 6 | 9.5 / 9 | |
| Median chart run | 5.5 / 3 | 11 / 10 | 15 / 16.5 | |
| Peak chart week of top-10 titles (median) | 1 / 1 | 3 / 3 | 3 / 3 | later |
| Chart slots held by 40+-week titles | 14 / 10.5 | 12.5 / 9 | 14 / 11.5 | 38 (Jan 1960) |

| Year-end top 50 and soundtracks (2002 / 1001) | control | L 0.3 | L 0.15 | Billboard |
|---|---|---|---|---|
| New 1960 soundtracks reaching the top 10 | 5 / 8 | 0 / 3 | **1 / 2** | 1–2 |
| Soundtrack % of top 50, 1960 | 14 / 24 | 10 / 18 | 14 / 18 | 16 |
| … 1961 | 20 / 26 | 10 / 12 | 16 / 18 | 24 |
| Youth + R&B, 1961 | 16 / 22 | 28 / 28 | 20 / 32 | 16 |
| … youth studio LPs in the 1961 top 50 | 8 / 8 | 16 / – | 10 / 28 | about 8 |
| TradPop, 1961 | 40 / 12 | 36 / 28 | 38 / 24 | 22 |
| Comedy, 1961 | 4 / 12 | 10 / 4 | 2 / 0 | 12 |
| Gap to Billboard, seven rows, 1960 / 1961 | 20 / 22 | 17 / 43 | 23 / 40 | |
| Store LPs 1960 / 1961 (M) | 44.0 / 63.0; 40.7 / 63.8 | 42.9 / 60.8; 37.9 / 61.0 | 41.7 / 59.4; 37.8 / 60.4 | – / 68 |
| Labels defunct | 110; 134 | 122; 139 | 115; 130 | |

- **The blend fixes the chart's shape.** LPs debut in the bottom fifth and peak in their third chart week, as on
  Billboard. L 0.3 gives 13–14 No. 1s and 14-week reigns; L 0.15 overshoots (7–8.5 No. 1s, a 21-week reign).
  Interpolating, about L 0.22–0.25 meets 10–12. Top-10 debuts fall to 14–41 a year; the remainder are the largest
  follower launch books.
- **It fixes the 1960 soundtrack count directly.** New 1960 soundtracks reaching the top 10 are 0–3 against
  Billboard's 1–2, with no origination or tilt cut. This confirms §23: the excess was the porous top 10.
- **Two head fixes were tuned on the old chart and now misfire:**
  - **Hit-package demand (§10).** K 0.6, S 1.0 was sized to make 3-week youth LPs reach the year-end head. On a blended
    chart youth packages run a median 16.5 weeks (`acb-l30-2002`; 3 before) and youth studio LPs 8, so youth is 20–32
    of the 1961 top 50 against 16. S (the studio scope) is the larger overshoot.
  - **The soundtrack hit tail (§12)** renews only soundtracks that peaked in the top 10. Few new soundtracks peak there
    now, so 1960 releases no longer carry into 1961: 1961 soundtrack share falls to 10–18 against 24.
- **Comedy falls further under** (0–10 against 12), and TradPop stays over in 1961 on seed 1001.
- **Cost.** Store LPs are 1–3M a year lower: the chart's awareness feedback now favours steady sellers over launch
  spikes. Defunct labels are +12 / +5 (L 0.3) and +5 / −4 (L 0.15), inside the reshuffle band.
- **Not fixed by the blend:** the catalogue stock (40+-week titles still 9–14% against 38%; they are the opening
  soundtracks only) and the 50-slot chart from April 1961 (Billboard mono 150).

**Next.**
1. L 0.22 (between the two arms) as the chart setting.
2. On it, retune the two compensations: hit-package studio scope S about 0.5 (and K if packages stay over), and the
   hit-tail trigger from "peaked in the top 10" to a chart-depth bar (top 20, or 10+ chart weeks).
3. Then the catalogue stock: an opening LP catalogue beyond soundtracks, or a slower normal-LP catalogue decay
   (`AlbumSimulator.CatalogWeeklyDecay` 0.93 after week 8), judged on the 40+-week share.

Reproduction: `SimTools/run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/candidate-request.json -Arms sc
-LpScale 2.2 -Weeks 104 -Seed <seed> -RunPrefix acb-l30 -Extra --album-chart-blend=0.3`, then from `SimLogs/`
`analyze_album_chart_health.py`, `analyze_album_head_gap.py acb-l30`, `analyze_soundtrack_carryover.py`,
`analyze_hit_package_demand.py`.

## 25. Blend 0.22 with the two compensations retuned (2026-11-16)

§24's next steps 1–2. New option `--soundtrack-hit-tail-peak=N` (`SoundtrackHitTail.PeakSwitch`, default 10): the hit
tail renews a soundtrack that peaked at #N or better. Switch checks pass. All arms are the candidate +
`--album-chart-blend=0.22 --soundtrack-hit-tail-peak=20`, 104 weeks, seeds 2002 / 1001:
- `acb-t20`: blend and tail bar only.
- `acb-s5t20`: + hit-package studio scope S 0.5 (K 0.6 kept).
- `acb-k4t20`: + K 0.4, S 0.75 (studio LPs get K × S = 0.3, as in `s5t20`; packages 0.4 instead of 0.6).

The hit-package values are replaced in copied references (`acb-ref-s5-request.json`, `acb-ref-k4s75-request.json`),
because the first copy of a flag wins.

**Chart shape at 0.22** (two-seed means, 1960 / 1961; the arms differ by at most a point or two):

| | control | blend 0.22 | Billboard |
|---|---|---|---|
| Distinct No. 1s | 23 / 33 | 8–9 / 13–14.5 | 10 / 12 |
| Longest No. 1 run | 8 / 3 | 17–18 / 7–13 | 13 / 10+ |
| Median debut, % of chart | 45 / 33 | 81 / 75–79 | 70–99 |
| Debuts inside the top 10 | 93 / 153 | 22–24 / 26–30 | about 0 |
| Distinct titles reaching the top 10 | 131 / 194 | 61 / 62–70 | about 50 (estimate) |
| Median chart run | 5.5 / 3 | 11–12.5 / 10.5–12 | |

**Year-end top 50 %, 1960 / 1961 (seed 2002; seed 1001):**

| | control | `t20` | `s5t20` | **`k4t20`** | Billboard |
|---|---|---|---|---|---|
| Soundtrack | 14 / 20; 24 / 26 | 10 / 14; 26 / 22 | 14 / 20; 24 / 22 | **10 / 18; 22 / 22** | 16 / 24 |
| … 1961: first charted before 1961 / in 1961 | 14 / 6; 18 / 8 | 10 / 4; 18 / 4 | 14 / 6; 18 / 4 | **16 / 2; 16 / 6** | ≈20 / ≈4 |
| New 1960 soundtracks reaching the top 10 | 5; 8 | 0; 6 | 3; 5 | **1; 5** | 1–2 |
| Youth + R&B | 10 / 16; 22 / 22 | 16 / 18; 14 / 28 | 10 / 20; 18 / 30 | **12 / 16; 14 / 26** | 14 / 16 |
| … 1961 youth packages / studio LPs | – | 10 / 8; 10 / 18 | 14 / 6; 10 / 20 | **8 / 8; 2 / 24** | 6–8 / ≈8 |
| TradPop | 34 / 40; 8 / 12 | 46 / 36; 22 / 26 | 42 / 32; 18 / 18 | **44 / 34; 22 / 20** | 28 / 22 |
| Folk | 12 / 16; 20 / 14 | 8 / 6; 10 / 6 | 12 / 6; 18 / 4 | **8 / 8; 16 / 14** | 14 / 10 |
| Comedy | 14 / 4; 12 / 12 | 6 / 4; 10 / 2 | 8 / 2; 10 / 4 | **12 / 6; 10 / 0** | 12 / 12 |
| Easy Listening | 12 / 4; 6 / 10 | 12 / 12; 8 / 8 | 12 / 14; 6 / 10 | **12 / 12; 6 / 8** | 6 / 10 |
| Gap to Billboard, seven rows, 1960 / 1961 | 20 / 22 | 24 / 37 | 14 / 31 | **14 / 24** | |
| Store LPs 1960 / 1961 (M) | 44.0 / 63.0; 40.7 / 63.8 | 41.4 / 59.1; 37.5 / 61.9 | 41.0 / 60.3; 36.2 / 58.2 | 40.3 / 57.7; 35.1 / 57.6 | – / 68 |
| Labels defunct | 110; 134 | 122; 126 | 119; 131 | 121; 130 | |

- **The tail bar at #20 restores the soundtrack carry-over.** 1961 soundtracks are 18–22 against 24, and the age mix is
  now the closest of any arm: 16 points first charted before 1961 against ≈20, and 2–6 from 1961 against ≈4. New 1960
  soundtracks reaching the top 10 are 1 / 5 in `k4t20` (control 5 / 8; Billboard 1–2). The seed 1001 five need a
  third seed before reading anything into them.
- **K 0.4, S 0.75 is the best arm on the gap sums** (14 / 24 against the control's 20 / 22 and `t20`'s 24 / 37): it is
  close to the control in 1961 (24 against 22) and better in 1960, on a chart whose shape is now near Billboard's. Cutting the
  studio scope alone (`s5t20`) fixes 1960 but leaves youth packages over in 1961 (14 on seed 2002).
- **Seed 1001's youth studio LPs do not move** (18–24 of the 1961 top 50 in every arm against ≈8). On seed 2002 they
  are 6–8. This is the largest remaining row error, and it needs a third seed and a look at which acts they are.
- **Comedy is under in 1961** (0–6 against 12) on both seeds in every blend arm; the control was under on seed 2002
  too. TradPop 1960 is over on seed 2002 (44–46 against 28).
- **Cost: store LP volume.** `k4t20` sells 3.7–6.2M fewer store LPs a year than the control: about 2–4M from the blend
  (chart awareness now rewards steady sellers, not launch spikes) and 1–4M from the smaller hit-package demand. 1961
  stores are 57.6–57.7M against Billboard's 68 (control 63–64). Defunct labels are inside the noise (+11 / −4).

**Candidate arm:** `k4t20` (blend 0.22, tail bar 20, K 0.4, S 0.75). Before adopting it:
1. Restore volume with the LP scale (`--lp-demand-scale` 2.2 → about 2.4), since the gap is now almost entirely
   volume.
2. A third seed (3003), to settle seed 1001's youth studio LPs and its new-soundtrack count.
3. Still open from §23: the catalogue stock (40+-week titles 13% against 38%) and the 50-slot chart from April 1961.

Reproduction: `run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/acb-ref-k4s75-request.json -Arms sc -LpScale
2.2 -Weeks 104 -Seed <seed> -RunPrefix acb-k4t20 -Extra "--album-chart-blend=0.22,--soundtrack-hit-tail-peak=20"`;
analysis as §24.

## 26. Store LP volume back to 68M on the blended chart (2026-11-16)

§25's `k4t20` sells 57.6–57.7M store LPs in 1961 against Billboard's 68M (the candidate: 60–64M). Runs on `k4t20`
(blend 0.22, tail bar 20, K 0.4, S 0.75), 104 weeks: `acb-k4s24` (LP scale 2.4, seeds 2002 / 1001 / 3003),
`acb-k4s265` (scale 2.65, 2002 / 1001) and `acb-k4raw` (scale 2.2 + the new `--album-chart-blend-floors-raw`, 2002 /
1001).

**What binds.** Not the retail envelope: it is 131.9M units a year and 10–21M of it is unspent in 1961 in every arm.
Store volume is set by store intent before the envelope, and 11–13M of that intent switches to racks. The blend cut store
intent from 81–84M to 70–72M. The cause is the chart's awareness floors (`AlbumSimulator`: at least 0.48 in the top 10
and 0.30 in the top 40, plus chart discovery). The blend leaves the number of floor-weeks unchanged (the chart still has
10 and 40 slots) but moves them from launch-week LPs, whose buyer pools are fresh, to LPs several weeks old, whose pools
are partly spent. Each floor-week sells less.

**The raw-floor probe.** `--album-chart-blend-floors-raw` (`AlbumChartBlend.FloorsRaw`, read only with the blend on):
the album demand's chart reads (floors, chart discovery, and the A5 promo shadow) use last week's unblended sales rank;
the published chart, rack access and career reads keep the blend. Switch checks pass.

| 1961 (seed 2002 / 1001; 3003) | candidate | `k4t20` (2.2) | raw floors (2.2) | scale 2.4 | **scale 2.65** | Billboard |
|---|---|---|---|---|---|---|
| Store LPs (M) | 63.0 / 63.8; 60.0 | 57.7 / 57.6 | 61.6 / 59.7 | 62.8 / 63.3; 59.3 | **67.4 / 67.1** | 68 |
| Store intent before the envelope (M) | 81.0 / 84.2 | 70.4 / 72.2 | 76.9 / 75.2 | 78.6 / 80.8 | 87.6 / 87.7 | |
| Racks / budget editions (M) | 41.2 / 13.9; 42.1 / 14.2 | 39.8 / 13.8; 39.0 / 14.1 | 39.8 / 13.9; 38.5 / 14.1 | 39.9 / 13.8; 39.7 / 14.2 | 40.8 / 13.8; 38.7 / 14.1 | 60 together |
| Envelope unspent (M) | 13.8 / 11.8 | 20.6 / 21.2 | 16.6 / 19.6 | 15.4 / 14.8 | 10.0 / 12.0 | |
| LP gross ($M) | 278 / 285 | | | 273 / 275 | **288 / 282** | |
| Store LPs 1960 (M) | 44.0 / 40.7 | 40.3 / 35.1 | 42.1 / 36.6 | 42.8 / 39.0 | 46.0 / 41.9 | |
| Distinct No. 1s, 1960 / 1961 | 23 / 33 | 8 / 14.5 | 9 / 13.5 | 12.3 / 14 (3 seeds) | 8 / 12.5 | 10 / 12 |
| New 1960 soundtracks reaching the top 10 | 5 / 8 | 1 / 5 | 1 / 3 | 2 / 3; 2 | 3 / 3 | 1–2 |
| Soundtrack % of 1961 top 50 | 20 / 26 | 18 / 22 | **10 / 8** | 16 / 24; 20 | 18 / 16 | 24 |
| Youth studio LPs in 1961 top 50 | | 8 / 24 | 4 / 22 | 8 / 20; 12 | 10 / 14 | ≈8 |
| Gap, seven rows, 1960 / 1961 (two seeds) | 20 / 22 | 14 / 24 | 16 / 33 | 17 / 35 | 14 / 36 | |
| … three seeds | 24 / 26 | | | **11 / 23** | | |
| Labels defunct | 110; 134 | 121; 130 | 116; 136 | 117; 139; 145 | 120; 128 | |

- **The LP scale restores volume almost one for one.** 2.2 → 2.4 (+9%) raised 1961 stores 9–10%, and 2.65 (+20%)
  raised them 16–17%. Scale 2.65 lands at 67.1–67.4M and brings LP gross back to the candidate. The chart shape and
  the soundtrack count hold. 1960 stores also rise (42–46M; Billboard has no 1960 store figure here).
- **Raw floors are rejected.** They recover only about half of the blend's loss (+2–4M), and they hand the floors back
  to launch spikes, so the soundtrack carry-over collapses again (1961 soundtracks 8–10 against 24). Comedy lands on
  target in that arm (14 / 14), the one row it helps.
- **The head gap is seed-noisy at this size.** Two-seed 1961 gaps of 33–36 against three-seed 23 at the same settings
  (scale 2.4) come mostly from seed 1001 (youth studio LPs 14–24, Folk 0–14, TradPop 18–34). Rows swing ±5–10 points
  between seeds, so the arms from 2.4 to 2.65 cannot be ranked on head gap. Scale 2.4's three-seed gap (11 / 23) is
  the best measured so far, against the candidate's 24 / 26.
- **Racks stay short.** The stores-only scale does not reach racks: racks plus budget editions are 52.8–54.6M against
  Billboard's 60M (the candidate: 55–56M). Fixing that is a separate lever (`--rack-shopper-scale`, now 0.033).
- **Cost.** Defunct labels are inside the reshuffle band in every arm.

**Proposed candidate update:** `k4t20` at scale 2.65, i.e. candidate-request.json with `--hit-package-demand=0.4
--hit-package-demand-studio=0.75 --lp-demand-scale=2.65` and `--album-chart-blend=0.22 --soundtrack-hit-tail-peak=20`
added. Open: racks (above), seed 1001's youth studio LPs (third seed 3003 reads 12, so partly seed), Comedy and Folk
under in 1961 on some seeds, the 40+-week catalogue stock and the 50-slot chart from April 1961 (§23).

Reproduction: `run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/acb-ref-k4s75-request.json -Arms sc -LpScale
2.65 -Weeks 104 -Seed <seed> -RunPrefix acb-k4s265 -Extra "--album-chart-blend=0.22,--soundtrack-hit-tail-peak=20"`; raw
floors add `,--album-chart-blend-floors-raw`. Volume from `decade-annual-rollup`, `rack-finance-weekly`,
`budget-finance-weekly` and `retail-purchase-envelope`.

**Adopted into the candidate (2026-11-16, author's call).** `SimLogs/writing-calibration/candidate-request.json` now
carries `--hit-package-demand=0.4 --hit-package-demand-studio=0.75 --lp-demand-scale=2.65` and appends
`--album-chart-blend=0.22 --soundtrack-hit-tail-peak=20` (239 flags). Apart from `--run`, `--seed` and `--weeks` its
flag list equals the `acb-k4s265` run requests, so those runs are its 104-week control. The previous file is
`candidate-request-pre-blend-2026-11-16.json`. `--album-chart-blend-floors-raw` is not in it.

## 27. Racks to 60M, and seed 1001's youth studio LPs (2026-11-16)

§26 left racks plus budget editions at 52–55M against Billboard's 60M and suggested `--rack-shopper-scale`. Runs on the
adopted candidate (`candidate-request.json`, scale 2.65), 104 weeks: control `acb-k4s265` (seed 3003 added),
`bsc115` (`--budget-shopper-scale=0.115`, 2002 / 1001), `bsc13` (0.13, 2002 / 1001 / 3003) and `bsc13s28` (0.13 with
`--lp-demand-scale=2.8`, 2002 / 1001). No code change.

**The short channel is budget editions, not front-line racks.** In the control, front-line racks sell 38.0–40.0M in
1961, inside the 36.6–41.2M front-line range (`RackJobberChannelDesign.md`; 60M less a 31–39% budget share). Budget
editions sell 13.5–13.8M against 18.8M (31% of units at the model's $1.75 budget price) to 23.4M (39% at $1.25). Rack
shelf is not binding (66–72% used). Clubs are 44.1M against 45M (count `record-club-annual` without the `Licensor` rows,
which repeat the `OperatorLicensed` units), so total 1961 LPs are 160–165M against 173M, and the missing ~6M of budget
editions fits without overshooting. A higher `--rack-shopper-scale` would push front-line racks past their range, and
under `--rack-shared-exhaustion` it spends store buyers. The lever is `--budget-shopper-scale` (default 0.08).

| 1961 (seed 2002 / 1001; 3003) | control | budget 0.115 | **budget 0.13** | 0.13 + LP scale 2.8 | Billboard |
|---|---|---|---|---|---|
| Front-line racks (M) | 40.0 / 38.0; 39.3 | 38.7 / 39.4 | **38.6 / 37.9; 37.2** | 38.8 / 38.5 | 36.6–41.2 |
| Budget editions (M) | 13.5 / 13.8; 13.7 | 18.4 / 18.7 | **20.6 / 20.9; 21.1** | 20.5 / 20.9 | 18.8–23.4 |
| Racks together (M) | 53.5 / 51.8; 53.0 | 57.1 / 58.1 | **59.2 / 58.8; 58.3** | 59.3 / 59.4 | 60 |
| Store LPs (M) | 67.4 / 67.1; 62.9 | 66.1 / 65.8 | **64.8 / 65.0; 61.7** | 66.0 / 65.0 | 68 |
| Total with clubs (M) | 165.0 / 163.0; 160.0 | 167.3 / 168.0 | **168.1 / 167.9; 164.2** | 169.4 / 168.5 | 173 |
| Envelope: store intent / allocated (M) | 87.6→79.5 / 87.7→78.9 | 88.7→78.0 / 90.9→77.8 | 87.5→76.4 / 90.1→76.5 | 91.1→77.7 / 91.2→76.7 | |
| … region-weeks bound / unspent (M) | 29% 10.0 / 26% 12.0 | 35% 7.6 / 43% 6.9 | 39% 6.8 / 45% 6.9 | 45% 5.5 / 48% 6.4 | |
| Budget net to labels ($M) | 1.76 / 1.80 | 2.40 / 2.44 | 2.69 / 2.73; 2.76 | 2.68 / 2.73 | |
| Head gap, seven rows, 1960 / 1961 (2002 + 1001) | 14 / 36 | 18 / 21 | **14 / 19** | 21 / 31 | |
| Distinct No. 1s, 1960 / 1961 (two-seed mean) | 8 / 12.5 | 7.5 / 12.5 | 7.5 / 12.5 | 9 / – | 10 / 12 |
| Labels defunct | 120; 128 | 116; 131 | 117; 132 | 111; 131 | |

- **Budget scale 0.13 puts racks at 58.3–59.4M**, budget editions at 35% of rack units (between the two price
  sensitivities), and leaves the chart untouched: budget units never enter chart points, and the chart-shape and head
  rows move inside the seed band.
- **It costs 1.2–2.6M of store LPs through the retail purchase envelope**, not through switchers (1.4M, unchanged).
  Stores and racks share one purse (`RetailPurchaseEnvelope.Split`); in a short region-week, the purse is split by store
  intent against 1.6 × rack intent, so more budget demand takes store copies. Bound region-weeks rise from 26–29% to
  39–45%. Nationally 6.8M stays unspent: the purse is tight in some regions and weeks and slack in others.
- **More store intent barely buys it back.** LP scale 2.8 (+6% store intent) recovers 0.0–1.2M, because nearly half
  the region-weeks are now envelope-bound. Store volume above ~65M is now an envelope question (its size is the singles
  purse × 133/129, and its regional fit), not a demand-scale one.

**Seed 1001's youth studio LPs are seed luck.** 1961 top-50 youth studio LPs, counted per title: control 7 / 5 / 9
(1001 / 2002 / 3003), `bsc13` 7 / 4 / 6; Billboard about 4. Seed 1001's excess is one Superstar (artist_00007,
label_0004) with three studio LPs in the 1961 top 50, released at weeks 55, 73 and 91, each reaching #1. Youth Stars
and Superstars release a studio LP every 18–19 weeks on all three seeds (median and lower quartile at the 18-week
spacing), about 2.8 a year, the top of what Elvis or Brenda Lee issued in 1960–61. Seed 3003 has more youth Superstar
LPs (16) than 1001 (11) and its own control reads 9. The residual over Billboard is 1–3 titles a seed spread across
Major and MidTier Stars, inside the row noise of §26. No lever proposed.

**Proposed candidate update:** add `--budget-shopper-scale=0.13` (scale 2.65 unchanged). Open: store LPs 62–65M against
68M, now bound by the envelope's regional fit; Comedy under in 1961 (2–8 against 12); the 40+-week catalogue stock
and the 50-slot chart (§23).

Reproduction: `run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/candidate-request.json -Arms sc -LpScale
2.65 -Weeks 104 -Seed <seed> -RunPrefix bsc13 -Extra "--budget-shopper-scale=0.13"` (run with `-ExecutionPolicy
Bypass`). Volume: `rack-region-weekly` (front), `budget-region-weekly`, `record-club-annual` without `Licensor`,
`decade-annual-rollup` (stores), `retail-purchase-envelope`.

**Adopted into the candidate (2026-11-16, author's call).** `candidate-request.json` appends `--budget-shopper-scale=0.13` (240 flags). Apart from `--run`, `--seed` and `--weeks` it equals the `bsc13` run requests, so those are its 104-week control. The previous file is `candidate-request-pre-budget-2026-11-16.json`.

## 28. Store LPs against the purse, and Comedy in 1961 (2026-11-16)

§27 left stores at 61.7–65.0M against Billboard's 68M (bound by the envelope's regional fit) and Comedy at 2–8 against
12 in 1961. Both read from the `bsc13` runs (the candidate's control; seeds 2002 / 1001 / 3003). No code change, no new
runs.

**The purse is Billboard's retail LP total, so stores and racks share what little slack there is.** The envelope
gives LPs the singles purse × 133/129 (the 1966 guide's retail ratio) = 131.9M in 1961. Billboard's own 1961 retail
figures are LPs 68 + 60 = 128M against singles 92 + 40 = 132M, a ratio of 0.97, not 1.03; the model's singles purse
(~128M) is 3% under Billboard and the ratio 6% over, so the purse lands 3% above Billboard's retail LP total. Reaching
68 + 60 needs 97% of it spent. The runs spend 121–125M (92–95%): stores 61.7–65.0M, racks and budget 59.5–60.4M,
unspent 6.8–10.7M.

| 1961, by region (seed 2002) | purse (M) | store intent | stores | racks | unspent | weeks bound | store intent lost |
|---|---|---|---|---|---|---|---|
| East Coast | 44.2 | 34.3 | 23.3 | 20.5 | 0.4 | 87% | 7.0 |
| Great Lakes | 31.4 | 20.8 | 16.1 | 14.1 | 1.2 | 52% | 1.9 |
| West Coast | 19.6 | 13.8 | 10.5 | 8.4 | 0.7 | 62% | 1.4 |
| Deep South, Southwest, Plains, Rockies | 36.8 | 18.6 | 15.0 | 17.4 | 4.4 | 8–44% | 0.9 |

- **The regional fit is a youth-weighting carry-over.** The album purse is the region's singles purse
  (`MarketRegion.GetBuyingPopulationPercentage`: youth share, √income, urbanisation), so youthful, low-income regions
  get the same LP:single ratio as the East Coast. Store intent per purse dollar runs 0.78 (East Coast) to 0.42 (Deep
  South); the South and Southwest leave 3–4M unspent while the East Coast is bound in 77–87% of weeks.
- **Offline re-split** (`SimTools/replay_envelope_region_weights.py`, which applies `RetailPurchaseEnvelope.Split` to
  the logged region-week intents; with production weights it reproduces the logged stores and rack allowance exactly).
  The national purse is held fixed and only its regional shares change. Feedback through the chart is not modelled.

| 1961 stores (M), seed 2002 / 1001 / 3003 | rack weight 1.3 (production) | rack weight 1.0 |
|---|---|---|
| Current purse shares (East Coast 33.5%) | 64.8 / 65.0 / 61.7 | 67.2 / 67.8 / 63.3 |
| Adult × √income × urbanisation (36.5%) | 66.2 / 66.1 / 62.8 | 68.3 / 68.7 / 64.1 |
| Adult × income × urbanisation (37.8%) | 66.5 / 66.4 / 62.8 | 68.7 / 69.0 / 64.2 |

  Adult-weighted purse shares add 1.1–1.7M to stores at no cost to racks (rack allowance ±0.2M). Rack weight 1.0 (the
  priority racks get in a short region-week; production 1.3 in 1960–66, calibrated on the 1966–68 guide) moves another
  ~2.5M from racks to stores, so racks would fall to ~57.5M and the budget scale would have to rise again: a trade, not
  a gain.
- **Seed 3003 is short on demand, not purse**: store intent 80.3M (2002 / 1001: 87.5 / 90.1), bound in 24% of
  region-weeks, 10.7M unspent. No purse change reaches it.

**Comedy is seed noise around the target.** Year-end top-50 Comedy share, three seeds: 1960 10 / 8 / 16 (mean 11.3,
Billboard 12), 1961 8 / 2 / 20 (mean 10, Billboard 12). Release supply is the same on every seed (65–76 Comedy LP drops
in 1961, 55–67 of them new signings). The head depends on whether an opening Comedy act becomes a Star: seed 3003 has
two (artist_00620 with 7 charting LPs in two years, artist_00931 with 5), seed 1001 none (its best act charts 2 LPs,
peak #4). Billboard's 1961 six titles came from about four acts with one or two LPs each. Raising Comedy affinity
would lift the mean only by pushing seed 3003 further over, so no genre lever is proposed. The 7-LPs-in-two-years act
is the same Star release cadence §27 found for youth (every 18–19 weeks).

**Proposals.**
1. Size each region's album purse by adult population × √income × urbanisation (national total unchanged), as an opt-in
   switch with a 104-week three-seed screen. Expected +1–1.5M stores; it is a model correction (LPs were bought by
   adults) rather than a tuning knob.
2. Leave the rack weight at 1.3: stores above ~66M then need either racks below 60M or a purse above Billboard's retail
   LP total.
3. Comedy: none. If the Star release cadence (18–19 weeks) is ever lengthened, recheck Comedy and youth together.

**Population check (author's question).** Model regions total 157.9M against the 1960 census's 178.5M (48 states + DC,
by `Data/Map/UsStates.json` regions). The gap is not uniform: Great Plains, West Coast, Rockies and Southwest match the
census; Great Lakes is 36.1 against 39.3 and the East Coast 52.2 against 54.8; the **Deep South is 15.0 against 29.9**
(no documented reason found). The national level is absorbed by the calibrated multipliers (the singles purse lands at
~128M against Billboard's 132M), so the store gap is not a total-population effect. Replaying the purse with census
populations, intents held as logged: the Deep South's purse share rises from 7.8% to 13.9%, the East Coast's falls from
33.5% to 31.6%, and stores fall to 58.8–61.4M (with adult weighting, 60.2–62.6M). The Deep South's own demand would
also roughly double, but its 1961 store intent (4.0–4.5M) is far below its census-sized purse (~18M), so most of that
purse would go unspent. Correcting the Deep South is a world calibration question (singles, Country / R&B, regional
labels), not a store-LP fix, and the adult-weighted purse would still be needed alongside it.

## 29. Census population with the economic scale, and the adult-weighted LP purse (2026-11-16)

Two new opt-in switches, built after §28 (the author: the Deep South's 15M is a badly sourced figure, not a deliberate
segregation proxy; the segregation factor and Black radio already model that).

- `--census-population` (`Systems/CensusPopulation.cs`, applied in `ChartManager._EnterTree` before anything reads a
  region). Each region takes its 1960 census population (Great Lakes 39.3, Great Plains 15.4, East Coast 54.8, West
  Coast 20.3, Deep South 29.9, Rockies 4.6, Southwest 14.2; 178.5M), and its infrastructure counts (stations, jukeboxes,
  venues, studios, local labels, clubs, theatres, colleges, record and department stores) scale by census / authored.
  **Infrastructure check:** every region was authored at one per-capita density of its authored population (about 24
  stations, 2,800 jukeboxes and 10–12 record stores per million, the South included), so the counts were sized to the
  same wrong 15M. Scaled, the national totals read about 4,380 stations (Broadcasting: 3,507 AM + 938 FM at the end of
  1961; 4,339 AM+FM in November 1961) and about 497,000 jukeboxes (about 500,000 is the usual period figure). No
  state-level station table was found to check regional splits.
  `CensusPopulation.EconomicScale` (0.8975) holds the national record-buying population at its calibrated level. Every
  consumer that turns population into units, points, pools or fans reads `MarketRegion.EconomicPopulation` (singles
  purse, genre and album market sizes, airplay chart points and panel weights, potential audience, rack and budget
  shopper pools, rack counts, fanbase, media fans and TV viewers), and the four store-capacity formulas multiply by the
  scale. Logging, regional taste (cities against the rest) and colleges per million keep the real population.
- `--lp-purse-adult-weight` (`RetailPurchaseEnvelope.AdultWeightedBudgets`, requires `--retail-purchase-envelope=1`).
  The national LP purse (the sum of the regional purses, unchanged) is shared out by economic population × (1 − youth)
  × √income × (0.6 + 0.4 urbanisation) instead of by each region's youth-weighted singles purse.

Checks: `--retail-purchase-envelope-check` passes 4,023 assertions (7 new: purse conservation and direction, census
populations, infrastructure scaling, national buying population held, scale ≈ 0.90, off = authored). **Off is inert:**
`cpoff-2002-sc` (27 weeks, new build, no switches) matches `bsc13-2002-sc` on all 69 comparable weekly streams; the four
that differ do so from week 1 in end-of-run snapshot columns (company-monthly, programme-cash-shortfalls,
release-profit-gates, writing-suppliers), as in §24.

Runs on the candidate, 104 weeks, seeds 2002 / 1001 / 3003: `cen` (census only), `cenad` (census + adult purse);
control `bsc13`. Analysis: `SimTools/analyze_census_population.py bsc13 cen cenad` and `analyze_album_head_gap.py`.

| 1961 (seed 2002 / 1001 / 3003) | control | census | census + adult purse | Billboard |
|---|---|---|---|---|
| Singles (M) | 128.0 / 128.0 / 128.0 | 128.0 / 128.0 / 128.0 | 128.0 / 128.0 / 128.0 | 132 |
| Store LPs (M) | 64.8 / 65.0 / 61.7 | 62.7 / 62.7 / 58.7 | **64.2 / 63.9 / 61.6** | 68 |
| Front-line racks (M) | 38.6 / 37.9 / 37.2 | 39.8 / 37.8 / 39.7 | 40.8 / 38.6 / 40.3 | 36.6–41.2 |
| Budget editions / clubs (M) | 20.6–21.1 / 44.1 | same | same | 18.8–23.4 / 45 |
| Purse unspent (M); region-weeks bound | 6.8 / 6.9 / 10.7; 27–45% | 7.6 / 9.2 / 11.0; 25–40% | 5.2 / 7.2 / 7.5; 38–54% | |
| Deep South share of singles / store LPs | 8% / 5–6% | 14% / 10% | 14% / 9% | (16.8% of people) |
| Labels defunct | 117 / 132 / 124 | 128 / 152 / 128 | 128 / 158 / 134 | |
| Genre share error, 1960 / 1961 (3-seed mean) | 22.9 / 25.4 | 29.9 / 26.6 | 30.0 / 29.7 | |
| … Pop / R&S share 1960 (mean) | 37.4 / 21.2 | 38.0 / 19.0 | 38.2 / 18.9 | 34.9 / 24.3 |
| … Pop / R&S share 1961 (mean) | 34.8 / 22.9 | 34.3 / 21.8 | 38.7 / 19.9 | 34.0 / 24.7 |
| Album top-50 head gap 1960 / 1961 (3 seeds) | 9 / 15 | 11 / 26 | 11 / 18 | |

- **The economy holds at the national level.** Singles, clubs and budget editions are unchanged to the decimal, and
  the purse is the same 131.9M. The Deep South takes 14% of singles (from 8%).
- **The adult purse is needed with the census.** The census alone costs 2.1–3.0M of 1961 store LPs (a bigger South
  thins the East Coast's purse share, as §28's replay predicted). With the adult purse, stores are within 0.1–1.1M of
  the control and racks gain 0.7–3.1M, so retail LPs rise to 123–126M. The purse only binds in 1961, so 1960 is the
  same in both arms.
- **Three costs, none from the national level:**
  1. **Defunct labels +12 (census) and +16 (both); every seed is positive.** Small labels (Independent, Small,
     Boutique) lose 15% of 1961 gross on the East Coast ($51.9M → $44.0M, three-seed mean), more than the 5% cut in
     East Coast buyers, while Southern small labels gain only 4% ($17.8M → $18.6M) from 1.8× the buyers. Majors hold.
     The larger South's buyers go to national product, not to Southern labels.
  2. **R&S falls 1–2 points** (1960: 21.2 → 19.0 against 24.3), so the 1960 genre share error rises 7 points. The
     regions that shrink in economic terms (East Coast, Great Lakes) are the integrated R&S markets; the one that grows
     is the least integrated (`integrationLevel` 0.05), so Southern R&S demand stays mostly with its 33% Black
     population. The genre calibration was fitted against the 15M South.
  3. **The adult purse lifts Pop in 1961 by 4.4 points** (LP money moves to the TradPop-heavy East Coast).
- Album head rows and Comedy move inside the seed band of §26–28.

**Not proposed for adoption yet.** The switches do what they say and hold the economy's level, but they move regional
and genre balance that the old calibration absorbed. Next, in order:
1. Why Southern small labels do not convert a larger home market (distribution difficulty 0.6, inventory depth 0.4,
   breakout bars denominated in buying population, or national hits taking Southern buyers), from `label-sales-weekly`
   and the breakout funnel on `cen` against `bsc13`.
2. R&S under census: whether the Deep South's segregation factor and R&S acceptance were tuned to make a 15M South
   carry its R&S share; then R&S acceptance on the census base.
3. Then a decade A/B of `--census-population --lp-purse-adult-weight` at the end of the audit.

Reproduction: `run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/candidate-request.json -Arms sc -LpScale
2.65 -Weeks 104 -Seed <seed> -RunPrefix cenad -Extra "--census-population,--lp-purse-adult-weight"` (`cen`: census
only), run with `-ExecutionPolicy Bypass`.

## 30. Why Southern labels don't capture the bigger South, and R&S on the census base (2026-11-16)

New telemetry: `--regional-home-sales` (telemetry only, written in every mode including `--calibration`) writes
`{run}-regional-home-sales.csv`: each week, sale region × selling label's home region × tier × format × genre family,
with records in play and awaiting arrival, raw demand, aware buyers, serviceable intent, displaced demand, units
(local and spillover), backorders, stock, summed awareness and radio, and regional breakouts. Inert: `rhs0` (control +
stream) and `rhsc` (census + stream) match `bsc13` and `cen` byte for byte on decade-annual-rollup,
market-clearing-weekly, year-end-hot100, label-finance and retail-purchase-envelope, all three seeds. Analysis:
`SimTools/analyze_regional_home_sales.py` (one run in depth) and `SimTools/summarize_regional_home_sales.py CONTROL ARM`
(three seeds).

### Southern small labels do capture their home market

1961 singles, three-seed means, `rhs0` → `rhsc`:

| | control | census |
|---|---|---|
| Southern small labels' units in the Deep South (M) | 1.50 | 2.83 (+88%) |
| … outside the Deep South (M) | 9.67 | 9.29 (−4%) |
| … home fraction of their singles | 13% | 23% |
| Their share of Deep South singles | 15.2% | 16.0% |
| Small-label share of singles in the five other regions | 54.8% | 48.4% (paired −7.4 / −6.2 / −5.3) |
| Small-label share of Hot 100 record-weeks (2002 / 1001 / 3003) | 56.6 / 49.7 / 60.5 | 46.9 / 43.6 / 57.5 |

- **None of the four candidates binds.** Southern small labels' home units grow with the 1.8× buyers and their share
  of Southern sales holds. Distribution difficulty (0.6, a ×0.82 conversion term) and inventory depth (0.4, store
  capacity) apply to every record in the region, so they scale the South for everyone and cannot move Southern buyers
  from Southern labels to national product. Backorders per Southern-label record-week roughly double (seed 2002:
  1,401 → 3,041) and serviceable/raw falls from 71% to 65%, so inventory trims a few percent at most. Majors and
  mid-tier labels gain 1–3 points of Southern sales (national product), also small.
- **Southern labels are export labels.** In the control 87% of their singles sell outside the South, so a census that
  shrinks every other region's economic population by 2–11% costs them about as much abroad as they gain at home.
  Southern label *supply* does not respond either: the label count (~100 small) and 1961 launches (~550–650) are the
  same in both arms, because label placement does not read population (see `ProceduralLabelCityWeightingHandoff.md`;
  `MusicInfrastructure.localLabelCount` is never read).
- **The real cost is national and hits every small label.** Outside the South, small-label records lose 12–15% of raw
  demand per record-week on every seed (6–10% fewer aware buyers, 6–7% lower conversion), while majors' change is
  +4 / −7 / −1%. Small labels lose 3–10 points of Hot 100 record-weeks to majors on every seed. The split by region,
  genre and label is seed noise (East Coast small-label conversion −19 / −32 / +18%; R&S and non-R&S small records
  both lose), so this probe cannot name the mechanism. It is not R&S taste alone. The likeliest candidate is the
  zero-sum chart signal in conversion amplifying a small first-round loss in the regions small labels concentrate in.
  Testing that needs `demandChartSignal` in the stream. **Open.**

### R&S: the loss is the regional taste normalizer, not segregation or acceptance

- **The segregation factor does not reach singles.** On the live path (`GenreMarketV2`), singles demand is
  `GenreAcceptanceService.GetRegionalDemandAcceptance`: Σ segment capacity × acceptance × routing, with routing centred
  on each region's own capacity-weighted mean, so the UrbanRnB segment's size nearly cancels.
  `MarketRegion.GetSegregationFactor` enters only album pools (and the player desk). The South's integration (0.05)
  reaches singles only through MainstreamAM's 0.6 + 0.4 × integration factor.
- **R&S units barely move under census; the share loss is small and mostly 1960.** Paired R&S unit-share deltas (all
  formats, `genre-decade-shape`): 1960 −1.6 / −4.3 / −0.7 (mean −2.2, SE ≈ 1.1); 1961 −0.4 / +0.8 / −3.8. Year-end
  slots: 26.0 → 25.3 (1960), 33.0 → 32.0 (1961). Southern radio does not disfavour R&S (R&S airplay per record is 1.07×
  the all-genre mean in the South in both arms).
- **The carrier is `RegionalGenreTaste` (K = 1 on the candidate).** It multiplies single conversion by a region's
  Black share against the national mean and normalizes so the **population-weighted** national mean is 1. Under
  census two things go wrong:
  1. The national Black mean rises from 0.108 to 0.126, because the authored Deep South share (0.33, set for 15M)
     now applies to 29.9M. On census populations the authored shares imply 22.2M Black Americans (12.4%) against the
     census's 18.86M (10.6%). From the 1960 census by state (Gibson and Jung, Census Bureau Working Paper 56, Tables
     15–65, summed by `Data/Map/UsStates.json`): Deep South 0.263 (authored 0.33), Great Lakes 0.079 (0.11), East
     Coast 0.090 (0.10), Southwest 0.099 (0.09), West Coast 0.047 (0.05), Great Plains 0.036 (0.01), Rockies 0.014
     (0.01).
  2. The normalizer weights heads, not buyers. The South converts about 0.6 singles per head against 0.74–0.84
     elsewhere, so taste mass moved there is partly lost.
  R&S taste falls 3–13% in every region (Deep South 2.38 → 2.08, East Coast 0.95 → 0.86). A first-order replay on seed
  2002 (control regional R&S shares × taste ratio × census regional units) gives R&S singles 26.9% against the
  control's 28.7% (observed 28.3%; clearing damps it). With census Black shares and a buyer-weighted normalizer the
  same replay gives 28.1%.
- Side note: the Country centring priors (`GenreAcceptanceService.SpecialistBuyingPopulationPriors`) are still the
  authored-population weights, so under census the Country texture no longer centres nationally (it over-weights the
  South's 1.30). Not screened.

Two new opt-in switches, off byte-inert (seed 3003's `rhs0`/`rhsc` ran on the new build and match):
- `--census-black-share` (requires `--census-population`): `CensusPopulation.CensusBlackShare1960` replaces each
  region's `blackPopulation` before regions initialise, so it reaches the UrbanRnB segment, the segregation factor
  (album pools) and the regional taste.
- `--regional-genre-taste-buyer-weight`: `RegionalGenreTaste`'s national means weight each place by population × its
  region's `GetBuyingPopulationPercentage` (the weight `GenreAcceptanceService` uses).

Checks: `--retail-purchase-envelope-check` passes 4,025 assertions (2 new: the Black-share table reproduces 10.6%;
the switch is off by default).

### Screen: census Black share and the buyer-weighted normalizer

Runs on the candidate, 104 weeks, seeds 2002 / 1001 / 3003, all with `--census-population` and `--regional-home-sales`:
`cenb` (+ `--census-black-share`), `cenbw` (+ `--regional-genre-taste-buyer-weight`). Controls `bsc13`, `cen`.

| Three-seed mean | control | census | + Black share | + buyer weight | history |
|---|---|---|---|---|---|
| R&S unit share 1960 (paired vs control) | 21.2 | 19.0 (−1.6 / −4.3 / −0.7) | 18.2 (−1.6 / −4.5 / −2.8) | **20.9** (+0.9 / −1.3 / −0.5) | 24.3 |
| R&S unit share 1961 | 22.9 | 21.8 (−0.4 / +0.8 / −3.8) | 20.2 (+0.4 / −1.8 / −6.6) | 21.4 (−2.8 / +1.7 / −3.6) | 24.7 |
| R&S year-end slots 1960 / 1961 | 26.0 / 33.0 | 25.3 / 32.0 | 26.3 / 29.7 | 30.3 / 31.0 | |
| … RnB / DooWop unit share 1960 | 9.9 / 6.3 | 8.5 / 6.4 | | 8.4 / 7.4 | 11.3 / 5.1 |
| Genre share error 1960 / 1961 | 22.9 / 25.4 | 29.9 / 26.6 | 27.6 / 29.0 | 28.3 / 30.1 | |
| Labels defunct (2002 / 1001 / 3003) | 117 / 132 / 124 | 128 / 152 / 128 | 121 / 116 / 137 | 121 / 129 / 131 | |
| Singles, clubs, budget editions | | identical in every arm | | | |

- **The census Black shares alone make R&S worse**: they lower the Black share of the regions with the most R&S
  buyers (Deep South, Great Lakes), and the head-weighted normalizer still leaks the mass to the low-conversion South.
- **With the buyer-weighted normalizer the R&S family is back to the control in 1960** and inside the seed band in 1961.
  It recovers through DooWop (lower UrbanRnB routing, +1.1), not RnB, which stays 1.5 points under the control.
- **Defunct labels return to the control** (+1 and +3 against +12 for census alone).
- **The genre share error does not recover** (28.3 / 30.1 against 22.9 / 25.4). The census residue is RnB −1.5,
  TeenPop +1.4, Rock and Roll +1.1, DooWop +1.1 in 1960, and Country +1.8 in 1961 (12.5 against a target of 8.3). The
  Country centring priors are still authored-population weights (see above), the likeliest cause of the Country excess.

### R&S acceptance retune on the census base: RnB keyframes ×1.15 (1960) / ×1.08 (1962)

`cenbwr` = `cenbw` + `--genre-keyframe-scale=RnB@1960=1.15,RnB@1962=1.08` (the existing calibration probe; RnB's keyframes
are 1960 and 1962, so 1961 interpolates to about ×1.115). Sized to return RnB to the control on the census base.

| Three-seed mean | control | `cenbw` | `cenbwr` | history |
|---|---|---|---|---|
| R&S unit share 1960 (paired) | 21.2 | 20.9 | **21.7** (+1.7 / +0.4 / −0.6) | 24.3 |
| R&S unit share 1961 (paired) | 22.9 | 21.4 | **21.7** (+0.2 / +0.4 / −4.2) | 24.7 |
| RnB 1960 / 1961 | 9.9 / 9.9 | 8.4 / 8.9 | **10.0 / 9.8** | 11.3 / 12.0 |
| R&S year-end slots 1960 / 1961 | 26.0 / 33.0 | 30.3 / 31.0 | 27.3 / 33.0 | |
| Genre share error 1960 / 1961 | 22.9 / 25.4 | 28.3 / 30.1 | 27.2 / 27.9 | |
| Labels defunct | 117 / 132 / 124 | 121 / 129 / 131 | 122 / 119 / 123 | |

- **R&S is restored on the census base**: family share, RnB and year-end slots are at the control, and defunct labels
  are at or below it. Singles volume is unchanged (128.0M in every arm). Seed 3003's 1961 (−4.2) is the outlier in every
  census arm.
- **The remaining census genre error is not R&S**: TeenPop and Rock and Roll (+1 each, the South's youth share 0.40)
  and Country (+1.8 in 1961, the uncentred specialist priors). Those need their own fixes before the census package can
  match the control's 22.9 / 25.4.

**Census package candidate, not adopted:** `--census-population --census-black-share --regional-genre-taste-buyer-weight
--genre-keyframe-scale=RnB@1960=1.15,RnB@1962=1.08` (plus `--lp-purse-adult-weight` from §29, not in these arms).
The keyframe scale is a probe flag; if the package is adopted, fold the factor into the RnB keyframes behind the
census switch rather than carrying the probe.

Next, in order:
1. Re-centre the Country specialist texture on live regional buyers (`GenreAcceptanceService.SpecialistBuyingPopulationPriors`
   is the authored-population table) under `--census-population`, and re-screen Country.
2. TeenPop / Rock and Roll on the census base (youth-weighted South).
3. The small-label chart-access loss under census (3–10 points of Hot 100 record-weeks to majors): add `demandChartSignal`
   to `--regional-home-sales` and test the chart-feedback hypothesis. Check whether it survives the buyer-weighted taste
   (defunct already returns to the control in `cenbw` / `cenbwr`).
4. Then the decade A/B of the census package at the end of the audit.

Reproduction: `run_lp_earned_screen.ps1 -Reference SimLogs/writing-calibration/candidate-request.json -Arms sc -LpScale
2.65 -Weeks 104 -Seed <seed> -RunPrefix cenbwr -Extra "--census-population,--census-black-share,--regional-genre-taste-buyer-weight,--genre-keyframe-scale=RnB@1960=1.15,RnB@1962=1.08"`
(`cenb`, `cenbw`: drop the trailing switches; `rhs0` / `rhsc`: control / census with `--regional-home-sales`), run with
`-ExecutionPolicy Bypass`. Analysis: `summarize_regional_home_sales.py rhs0 rhsc`, `analyze_regional_home_sales.py`,
`analyze_census_population.py cen cenb cenbw`.

### Adopted (2026-11-16)

The `cenbwr` package is on the candidate (`SimLogs/writing-calibration/candidate-request.json`, backup
`candidate-request-pre-census-2026-11-16.json`): `--census-population --census-black-share
--regional-genre-taste-buyer-weight`. The RnB retune is folded into `GenreCatalog.Add` behind `--census-black-share`
(×1.15 on the 1960 keyframe, ×1.08 on 1962, the same float multiply as the probe), so the probe flag is not carried.
Check: `adcen-2002-sc` (candidate, 27 weeks, no probe flag) matches `cenbwr-2002-sc` byte for byte on all 116 common
weekly streams; the 26 that differ are end-of-run snapshots. `--lp-purse-adult-weight` (§29) is not adopted: it was
not in the screened arms.

## 31. Specialist texture centred on census buyers; Southern labels as exporters (2026-11-16)

### `--census-specialist-priors`

`GenreAcceptanceService.SpecialistBuyingPopulationPriors` (the weights that centre the Country, TexMex and Boogaloo
regional texture to one national opportunity, and the specialist single normalizer) were authored population ×
`GetBuyingPopulationPercentage`. New opt-in switch `--census-specialist-priors` (requires `--census-population`)
rescales each prior by census / authored population; the buying fraction does not change under the census. Check: the
authored priors equal authored population × buying fraction to 1e-5 (`--retail-purchase-envelope-check`, 4,026 pass).

Predicted size: on census buyers the authored centring leaves national acceptance at 1.021 (Country), 1.016 (TexMex),
0.985 (Boogaloo); Deep South Country texture 1.38 → 1.35, East Coast 0.85 → 0.83.

`csp` = candidate (census package adopted) + `--census-specialist-priors` + `--regional-home-sales`, 104 weeks; control
`cenbwr`.

| Three-seed mean | `bsc13` (pre-census) | `cenbwr` | `csp` | target |
|---|---|---|---|---|
| Country 1960 (2002 / 1001 / 3003) | 8.5 / 11.5 / 8.4 | 9.6 / 11.9 / 9.4 | 8.2 / 10.1 / 9.7 | 8.4 |
| Country 1961 | 8.5 / 13.9 / 9.8 | 11.4 / 12.5 / 11.9 | 11.1 / 11.2 / 11.6 | 8.3 |
| TexMex 1960 / 1961 | 0.11 / 0.11 | 0.08 / 0.15 | 0.06 / 0.16 | 0.23 / 0.24 |
| LatinPop 1960 / 1961 (not a specialist) | 0.17 / 0.30 | 0.46 / 0.53 | 0.41 / 0.21 | 0.91 / 0.95 |
| Genre share error 1960 | 22.9 | 27.2 | 28.2 | |
| Genre share error 1961 | 25.4 | 27.9 | **25.5** | |

- Country moves the predicted way on 5 of 6 seed-years (paired −1.4 / −1.8 / +0.3 in 1960, −0.3 / −1.3 / −0.3 in
  1961): 1960 is back to the pre-census control, 1961 sits 0.6 over it. The rest of the 1961 Country excess (10.7
  control, 8.3 target) predates the census.
- TexMex and LatinPop are under a quarter of a point and move inside noise; both are under target by half in every
  arm, a supply question rather than centring. Boogaloo emerges in 1966 and cannot be screened at 104 weeks.
- The 1961 genre error returns to the control; the 1960 rise (+1.0) is seed 3003 and 1001 outside the specialists.
- **Recommendation:** adopt as the census package's correctness fix (it removes an authored-population weight the
  census made stale). **Adopted on the candidate 2026-11-16** (backup `candidate-request-pre-specialist-2026-11-16.json`).

### Why Southern labels are exporters

- **The lease route is on the candidate** (`--lp-retail-v2-profile` expands to `--ai-master-deals`,
  `--ai-master-national-pickups`, `--ai-master-capacity-decisions`), but fires 15 / 11 / 22 times in 104 weeks
  (`cenbwr`), 6 / 1 / 7 for Southern sellers: a seller accepts only when it cannot fund replenishment, and small labels
  reach national distribution on their own first (64% sign a P&D deal in year one; the deal gate is a launch-ramp bar).
- **Southern small labels' singles are 76–81% charted units** (`label-sales-weekly`, 1961, `rhsc`), the same as other
  small labels, so their export share is that of a national chart record in a South with 14% of buyers.
- **Chart coverage:** Deep South labels hold 6–13% of Hot 100 record-weeks in `bsc13` and 9–18% in `cenbwr`.
  An unverified count of the 1960–61 year-end Top 100 on Deep South labels (Hi, Monument, Rita, Minit, Instant,
  Satellite, Hickory) is about 5 and 7 records; regional hits that went national were often leased to Northern labels
  (Satellite → Atlantic "Gee Whiz", Dot's licensing of local masters) and are booked there. Outside their hits,
  Southern labels' catalogues sold through regional channels (WLAC mail order via Ernie's Record Mart, Randy's;
  Excello/Nashboro, Starday, Peacock).
- **Reading:** the model has the South's national hits but not its regional catalogue or the lease hand-off. The lever
  is the P&D deal gate (history suggests 5–15% of small labels a year, against 64%), which would keep non-hit records
  home and route breakouts through the existing lease path. A whole-economy change; needs its own screen and a labelled
  1960–61 Hot 100 count as the target.

## 32. A proven-regional-hit bar for national distribution (2026-11-16)

On the current candidate (census package, specialist priors, label city weights; the regional-hit deal gate and slow
wholesale spread are on by default) 35% of starting Small labels sign a P&D deal in 1960 and 56% by the end of 1961,
against a historical 5–15% a year (national pickups were per-record leases of proven regional hits;
`pd-deal-gate-is-a-launch-ramp-bar`). The evidence is a regional hit (breakout ≥ .40, volume ≥ .50) in the last four
weeks; about a fifth of monthly checks pass it.

New opt-in `--proven-hit-deal-weeks=N` (`RegionalHitDealGate.IsProvenRecent`; counter `RegionalRecordData.dealHitWeeks`,
saved): an AI label's P&D pull offer (and the record a Major's poach covers) and a wholesale house's hop into a
neighbouring market need a record that is a regional hit now and has been one for N weeks in that market. AI master
lease pickups and the player's pitch keep the plain four-week test, so a short regional hit can still be leased out.
Checks: 4,037 pass (3 new); save/load round trip passes. Off is inert (`phoff-2002-sc` matches `lcw-2002-sc` on all
117 common weekly streams).

Runs: candidate + `--regional-home-sales`, 104 weeks, seeds 2002 / 1001 / 3003; control `lcw`. Analysis:
`SimTools/analyze_deal_gate.py lcw ph4 ph8 ph13`.

| Three-seed mean | control | N 4 | N 8 | N 13 | history |
|---|---|---|---|---|---|
| Starting Small with a P&D deal, end 1960 | 35% | 26% | 18% | **5%** | 5–15% a year |
| … end 1961 | 56% | 45% | 33% | **10%** | |
| Starting Small with a wholesale house, end 1960 (placements) | 36% (106) | 31% (95) | 21% (60) | 14% (43) | |
| Starting Independent with a P&D deal, end 1960 | 44% | 39% | 32% | 19% | |
| AI master leases (two years; Southern sellers) | 14 (4) | 12 (4) | 12 (4) | 14 (4) | |
| Southern small labels' home fraction of singles, 1961 | 24% | 21% | 24% | 26% | |
| Deep South Hot 100 record-weeks 1960 / 1961 | 8.9 / 8.6% | 9.0 / 7.2% | 9.0 / 7.0% | 7.7 / 5.8% | ~4% (two stations) |
| Deep South Top 10 record-weeks 1960 / 1961 | 7.4 / 5.5% | 8.4 / 4.1% | 10.1 / 7.2% | 6.6 / 3.3% | 3.1 / 7.1% of slots |
| Small-label Hot 100 record-weeks 1960 / 1961 | 50 / 50% | 51 / 48% | 51 / 50% | 50 / 45% | |
| Labels defunct | 131 | 129 | 129 | 137 (+9 / +11 / +1) | |
| Genre share error 1960 / 1961 | 23.6 / 26.7 | 21.1 / 22.8 | 18.6 / 22.3 | 20.4 / 25.9 | |

- **N 13 puts Small P&D signing in the historical band** (5% a year), and the wholesale route does not refill it: the
  hop reads the same proven bar, so Small house placements fall too (106 → 43), unlike the October gate
  (`wholesale-substitutes-for-pd-deals`). Independents keep more access (19%), as they should.
- **Southern labels' home fraction barely moves** (24% → 26%). About 80% of their singles units are Hot 100 records,
  and a charting record still reaches every region (national Top 40 arrival, neighbour tests, the blind-launch dose for
  hot acts). Over two years (seed 2002), 6,853 held region launches opened through acquired distribution against 186 by
  the chart, so the bar trims non-hit spread, but the units are in the hits. The export share is the share of a
  national hit, which is historical for a label with hits (Minit's "Mother-in-Law" sold nationally).
- **Leases do not respond** (12–14 per seed): `--ai-master-capacity-decisions` only lets a seller lease when it cannot
  fund replenishment, so withholding a P&D deal does not make a label lease. That is the next lever if leases are the
  goal.
- **Costs:** defunct +6 at N 13 (two seeds +9 / +11), from slower distribution growth. Genre error is no worse at any N
  (N 8 is lowest, inside reshuffle noise).
- **Recommendation:** N 13 for the deal band; N 8 if the closures matter more (18% a year, defunct flat). Not adopted.

**Adopted 2026-11-16:** `--proven-hit-deal-weeks=13` is in `SimLogs/writing-calibration/candidate-request.json`
(previous: `candidate-request-pre-provenhit-2026-11-16.json`). The candidate equals the ph13 runs without the
`--regional-home-sales` telemetry.

## 33. Why the Southern home share barely moves (2026-11-16)

`--regional-home-sales` now also splits rows by the record's peak Hot 100 class to date (`peakClass` T40 / H100 / none)
and whether the label holds a P&D deal that week (`pdDeal`). Telemetry only: `hs0` / `hs13` (the pre-adoption control
and the new candidate, 104 weeks, seeds 2002 / 1001) print the same `REGIONAL_ARRIVAL` counters as `lcw` / `ph13`.
Analysis: `SimTools/analyze_home_share_split.py hs0 hs13`.

Southern small labels' 1961 singles (control → N 13):

| | 2002 | 1001 |
|---|---|---|
| Units | 11.64M → 6.56M | 4.63M → 3.57M |
| Home fraction | 21% → 23% | 31% → 29% |
| Top 40 records: share of units, home fraction | 64% → 54%, 17% → 13% | 48% → 37%, 23% → 11% |
| Hot 100 (41–100) records | 20% → 22%, 25% → 28% | 29% → 31%, 35% → 35% |
| Uncharted records | 16% → 24%, 34% → 41% | 23% → 32%, 43% → 44% |
| Units on labels with no P&D deal | 38% → 79% | 17% → 67% |

- **The gate works on volume, not geography.** It halves the Top 40 count, which cuts home units with away units
  (seed 2002 home 2.44M → 1.53M). Each record keeps its own export profile, so the mix shift towards uncharted records
  (+4–7 pts home) is offset by a lower home fraction on the Top 40 records that remain.
- **Charted records are national whatever the label's distribution.** At N 13, 67–79% of these units sit on labels with no
  P&D deal: a Top 40 record arrives everywhere (national Top 40 trigger, and the hot-act launch dose opens every region
  for the act's next single). A Top 40 record's home fraction (11–23%) is about the Deep South's census share of buyers
  (17%), i.e. a national hit, which is historical.
- **Uncharted records were never exported by P&D.** With no deal they are still in play in 30–70% of away
  region-weeks (East Coast / Great Lakes / Southwest 54–70%). The floor is opening owned distribution
  (`AILabelFactory.GetDistributionRegions`): home plus 1–2 regions for Boutique and 1–3 for Independent, and 40% of
  labels get an adjacent strong region (`GetRegionalStrength`); both open at launch. Small labels are already cut to home
  only (`SmallLabelHomeTerritory`, default on), and it shows: 63–71% of Southern Small uncharted units sell at home,
  against 33–44% for Boutique and Independent, whose records are in play in 65–72% of away region-weeks. Independents
  and Boutiques carry ~85% of Southern small-label units.
- **Ceiling:** removing every away unit of uncharted records would lift the home fraction only to 27% (2002) / 35%
  (1001). The aggregate is set by the hit mix, so it should not be a target: a label with a national hit exports most of
  its units, in history too.
- **Better target:** the uncharted home fraction (41–44% now). A non-hit Southern record selling more than half its
  units outside the South is the unhistorical part. The lever is the opening owned-distribution draw for
  procedural Boutique/Independent labels, not the deal gate (screened in §34).

## 34. Home-only opening territory for Boutique / Independent labels (2026-11-16, one-seed probe)

New opt-in `--home-territory-tiers=Boutique[,Independent]` (`SmallLabelHomeTerritory`): opening procedural labels of those
tiers start with distribution and strong region at home only, as Small labels already do. Templates keep their networks;
the cut runs after the factory's draws, so the opening RNG stream is unchanged (check: same draws, 297 labels cut).
Off is the existing code path.

Probe: `htbi` (both tiers) on the candidate + `--regional-home-sales`, seed 2002, 1960 read at week 53 against `hs13`.

| 1960, seed 2002 | hs13 | htbi |
|---|---|---|
| Southern small labels: singles units, home fraction | 6.91M, 28% | 3.59M, 41% |
| … uncharted units: home fraction | 44% | 68% |
| … Boutique / Independent uncharted home | 35% / 34% | 75% / 61% |
| Deep South Hot 100 / Top 10 record-weeks | 7.4% / 5.1% | 8.1% / 5.5% |
| Small-label share of Hot 100 record-weeks (all regions) | 50% | **38%** |
| Small-label share of single units | 48% | **35%** |
| Defunct by week 52 | 58 | 54 |

- **It hits the target:** uncharted Southern records go from 44% to 68% home, and Boutiques reach the Small level.
- **The cost is national:** every region's procedural Independents lose their opening network, so small labels lose
  12–13 points of chart and unit share to Majors. History moves the other way in 1960 (independents rising), so the
  Independent half of the arm is too blunt.
- Next, if pursued: Boutique only (`htb`), with the small-label chart share as the guard.

## 35. Is the national small-label share carried by Southern labels? No (2026-11-16)

`SimTools/analyze_region_small_share.py` splits small-label (Independent / Small / Boutique) chart and unit share by label
home region. 1960, `hs13` seeds 2002 / 1001, then the §34 probe `htbi` seed 2002:

| Label home | labels | small H100 rec-wks | home fraction | uncharted home | region's demand | `htbi`: small H100, uncharted home |
|---|---|---|---|---|---|---|
| East Coast | 44 / 42% | 43 / 48% | 43 / 45% | 60 / 60% | 36 / 38% | 49%, 79% |
| Great Lakes | 17 / 21% | 22 / 18% | 35 / 38% | 55 / 59% | 23 / 20% | 20%, 75% |
| West Coast | 18 / 16% | 18 / 13% | 31 / 33% | 49 / 52% | 10 / 9% | 16%, 70% |
| Deep South | 14 / 12% | 11 / 11% | 28 / 31% | 44 / 53% | 17 / 19% | 10%, 68% |
| Southwest | 4 / 7% | 3 / 9% | 16 / 11% | 27 / 29% | 7 / 6% | 4%, 62% |
| Plains + Rockies | 3 / 2% | 2 / 1% | 10–38% | 13–42% | 8 / 8% | 1% |

- **Southern labels are under-weight, not propping up the total.** They supply 5.5 of the 48–50 points of small-label
  Hot 100 share, less than their share of small labels (12–14%) or of demand (17–19%). East Coast labels supply 21–23
  points, Great Lakes 9–11, West Coast 6–9.
- **Southern export is ordinary.** Every region's small labels sell most units away; home fraction over the region's
  demand share is 1.2 (East Coast), 1.5–1.9 (Great Lakes, Deep South), 3.0+ (West Coast). Uncharted records are 44–60%
  home in the big regions.
- **The §34 loss was national** because the switch hit Independents everywhere: East Coast 21.3 → 18.7 points, Great
  Lakes 11.1 → 7.8, West Coast 9.0 → 6.1, Deep South 5.6 → 4.0.
- **The regional imbalance that exists is placement, by design:** label share follows the sourced city weights (§31,
  New York / Los Angeles heavy) and small-label chart share follows label count, not regional demand. That matches the
  sourced 1960–61 firm table, so it is not evidence of a fault.
- **Implication:** if uncharted spread is too wide, it is too wide in every region (opening networks of procedural
  Independents / Boutiques), and a fix must be judged on the national small-label share, which §34 shows it costs.

## 36. What is wrong with procedural Independents and Boutiques (2026-11-16, read-only)

Read from `hs13` (seeds 2002 / 1001, 104 weeks, current candidate + `--regional-home-sales`); no new runs. Scripts in the
session scratchpad (opening-label census, closure split, overhead bands).

### Census, 1960

| Procedural | labels | singles / label | median units / single | unit share | Hot 100 record-weeks | P&D end 1960 | defunct end 1960 / 1961 |
|---|---|---|---|---|---|---|---|
| Independent | 188 / 177 | 6.7 / 7.2 | 792 / 872 | 28 / 29% | 29 / 28% | 14 / 22% | 20 / 20%, 31 / 29% |
| Boutique | 111 / 119 | 4.1 / 4.4 | 670 / 644 | 10 / 9% | 9 / 9% | 11 / 9% | 5 / 7%, 8 / 18% |
| Small | 231 / 241 | 3.4 / 3.6 | 129 / 141 | 7 / 5% | 8 / 5% | 5 / 5% | 2 / 2%, 22 / 20% |

The chart's firm structure is not the miss: 58 / 59 labels reach the 1960 Top 10 (33 / 39 with one record) against 49
(31) in Billboard's 1960 Top 10 (`historical-top10-1960/concentration_1960.csv`). The tier sizes are not evidence of a
fault on their own.

### Problems found

1. **An overhead cliff decides who closes.** `LabelCorporateModel.IsFounderRun` needs zero paid employees. Once the
   work estimate (`SetRequiredStaff`, roster and release rate) passes one founder, the first quarter-employee also
   switches on the founder's fixed salary and a leased suite, so opening overhead is either ~$25 or $1,100–1,900 a
   month, with nothing between. 76–83% of procedural Independents open above it and 12–18% of Boutiques. At the same
   opening cash (~$14k Independents):

   | Opening overhead | Independent n, defunct by week 104 | Boutique n, defunct |
   |---|---|---|
   | ≤ $100 | 45 / 29, 16 / 14% | 97 / 98, 6 / 11% |
   | $1k–2k | 143 / 147, 36 / 33% | 14 / 21, 21 / 48% |

   Dead and surviving Independents sell the same median single (856 vs 815, 1,170 vs 886 units); the dead ones simply
   never got a hit while carrying $15k a year. This is the MidTier pattern (`midtier-closures-are-tier-payroll-vs-opening-roster`)
   one tier down: unearned, set by the opening roster draw. Candidate fix: a graduated transition (pay the employees
   hired; the founder stays on surplus compensation and home premises until paid staff pass a threshold), not more cash.

2. **Geography is supply-only, so uncharted spread can only be cut by cutting networks.** A record's regional sales
   are the region's buyers times the launch factor (`ChartSimulator.GetRegionalLaunchFactor`: strong/home 1.35, covered
   0.55 + 0.45 × reach ≈ 0.7, uncovered ≈ 0.18) and stock depth; nothing on the demand side favours a local act or a
   record with no action. A Deep South Independent with home plus East Coast and Great Lakes therefore sells about
   .17 × 1.35 against .36 × .7 + .21 × .7 + the rest × .18 elsewhere, i.e. ~35% home, which is what §33 measured. The
   same networks carry the hits, so §34's home-only cut cost 12–13 points of small-label share. The historical shape
   was broad, shallow distribution (independent distributors carried the line) with distributors pushing only records
   that showed local action. Candidate: covered non-home regions launch at the uncovered factor until the record has a
   home breakout, then open at the covered factor. That targets uncharted records and leaves hits their reach; judge
   it on the national small-label share and the uncharted home fraction together.

3. **Opening networks are drawn without geography.** `AILabelFactory.GetDistributionRegions` adds 1–3 (Independent) or
   1–2 (Boutique) regions uniformly from all seven, so a Nashville label can open in the Rockies and West Coast but not
   the East Coast. Strong regions are home plus a 40% adjacent draw. If networks stay, they should follow market size or
   adjacency (where independent distributors were), which would also make problem 2 easier to read.

4. **Runtime births follow different rules.** `RuntimeLabelProfileFactory` (`:180–187`) sets every birth's strong
   regions to home only, after `LabelGenerator.AssignRegions` (`:189–212`) has moved ~30% of a birth's extra regions
   into `strongRegions`; those regions are then dropped (neither strong nor distributed). Opening and runtime
   Independents of the same tier therefore start with different footprints. Unverified in a run.

5. **Tier is drawn independently of archetype.** `GetRandomTier` ignores the archetype, so 9–12 procedural Boutiques
   and 9–15 Independents per seed are `CorporateGiant`, and 20–30 Boutiques are `RegionalHustler`. "Boutique" carries
   no specialist meaning: it is an Independent-like envelope (budget .20–.52, marketing .30–.70) at Small costs, which
   is why it rarely closes (5–7% in year one).

**Order:** 1 is a correctness fix with a clear target (closures earned on records, not roster size) and should be
screened first, three seeds at 104 weeks, guarded by Independent launches and Big-5 share. 2 is the lever for the
uncharted spread and needs its own screen. 3–5 are cleanup that changes the RNG stream, so they belong behind a switch.

## 37. Graduated founder overhead (2026-11-16)

New opt-in `--graduated-founder-overhead[=T]` (`Systems/GraduatedFounderOverhead.cs`; bare = 2 paid FTE; requires
`--founder-surplus-compensation`). A founder-run Small / Boutique / Independent firm stays founder-led while its paid staff
is at most T: it pays the staff it hires (wages plus the $12 desk allowance) but keeps home premises and contingent
founder pay (`LabelCorporateModel.IsFounderLed`, read by the premises quote and `FounderCompensation.Applies`). Above T
it leases an office and salaries the founder as before. Founder dormancy still reads the strict no-staff test. No draws,
no saved state. Checks: `--founder-compensation-check` 38 pass (13 new), `--retail-purchase-envelope-check` 4,037 pass.
Off is inert: `gfooff-2002-sc` matches `ph13-2002-sc` byte for byte on 142 of 143 CSVs; the other
(`regional-home-sales`, two telemetry columns added in §33) matches `hs13-2002-sc`.

Runs: `gfo` = `ph13` flags + `--graduated-founder-overhead`, 104 weeks, seeds 2002 / 1001 / 3003. Analysis:
`SimTools/analyze_founder_overhead.py ph13 gfo`, `SimTools/analyze_label_city_weights.py ph13 gfo`.

| Three-seed mean | `ph13` | `gfo` |
|---|---|---|
| Procedural Independent opening overhead (median) | $1,269 | $353 |
| Procedural Independent defunct end 1960 / 1961 | 20.6 / 29.5% | **2.6 / 8.8%** |
| … opened ≤ $100 / above, defunct end 1961 | 12 / 34% | 3 / 10% |
| Procedural Boutique defunct end 1960 / 1961 | 6.2 / 13.4% | 0.6 / 4.7% |
| Procedural Small defunct end 1961 | 18.2% | 14.4% |
| Procedural MidTier defunct end 1961 (untouched) | 41.8% | 43.3% |
| All labels defunct by week 104 | 137 | **69** |
| Runtime label births by week 104 | 99 | 53 |
| Procedural Independent singles 1960 / 1961 | 1,377 / 1,327 | 1,579 / 1,757 (+15 / +32%) |
| Small-label unit share 1960 / 1961 | 49.5 / 41.8% | 49.7 / 48.9% |
| Small-label Hot 100 record-weeks 1960 / 1961 | 50.1 / 43.3% | 51.5 / 48.5% |
| Big-5 Top 10 record-weeks 1960 / 1961 | 27.5 / 33.8% | 33.3 (38 / 30 / 32) / 33.7% |
| Genre share error 1960 / 1961 | 20.4 / 25.9 | 22.1 / 24.1 |

- **The cliff was the closure cause.** Independent two-year closures fall by two thirds, and the ≤ $100 group falls too
  (12 → 3%): firms that opened below the cliff were crossing it later, when they hired their first part-timer. By week
  104 survivors' overhead is graduated (most $100–1,000; 17–28 firms per seed above $1,000, past the 2-FTE limit).
- **Small labels hold their 1961 share** (+7 points of units, +5 of Hot 100 weeks), because the firms that survive keep
  releasing (Independent singles +32% in 1961). The 1961 small-label fade in the control was partly closures.
- **Births halve** (99 → 53): runtime founding fills vacancies left by closures, so fewer closures mean fewer births.
- **Big-5 1960 Top 10 +5.8** is mostly seed 2002 (+12; 1001 +1, 3003 +5) and 1961 is flat: inside reshuffle noise, but
  the same sign on all three seeds, so watch it. Genre error moves inside noise.
- **Open question: closures may now be too low.** 9% of Independents and 5% of Boutiques close in two years; the design
  band is 20–35% five-year financial closure for Small labels (`small-labels-never-lose-money`). The remaining
  closures are earned (no hit, real cost), but whether the five-year rate lands in the band needs a 260-week run.
- **Recommendation:** adopt as a correctness fix (it removes a cost step set by the opening roster draw), after a
  260-week check of the five-year closure rate. Not adopted.

## 38. Graduated founder overhead: economy, chart health and the Southern question (2026-11-16)

Same runs as §37 (`ph13` vs `gfo`, three seeds, 104 weeks; Southern split on `hs13` vs `gfo` where it needs the §33
columns). Scripts: `SimTools/analyze_founder_overhead_wider.py ph13 gfo`, `analyze_home_share_split.py`,
`analyze_region_small_share.py`, `analyze_label_city_weights.py`.

### Economy

| Three-seed mean | `ph13` 1960 / 1961 | `gfo` 1960 / 1961 |
|---|---|---|
| Single units (M) | 123.3 / 128.0 | 123.3 / 128.0 |
| Album units (M) | 43.6 / 65.3 | 44.4 / 66.7 |
| Single + album gross ($M) | 287 / 369 | 290 / 373 |
| Singles released | 3,542 / 4,297 | 3,788 / 4,773 (+7 / +11%) |
| Singles losing money | 67.2 / 68.7% | 69.0 / 70.0% |
| Gross share Major / MidTier / Indie / Boutique / Small, 1961 | 68.8 / 10.5 / 14.2 / 4.0 / 2.5% | 67.2 / 9.6 / 16.3 / 4.2 / 2.6% |
| Independent pretax earnings ($M) | 16.8 / 14.0 | 18.3 / 16.9 |
| Independent owner salary ($M) | 1.67 / 1.33 | 0.70 / 1.01 |
| Active Independent / Boutique / Small firms, end of run | 242 / 103 / 190 | 263 / 113 / 185 |
| Independent median cash, end of run | $37k | $44k |

- **The singles purse is fixed, so more survivors means more records sharing it.** Units are identical to the
  thousand (`singles-volume-is-a-fixed-purse`); 11% more 1961 singles push the money-losing share up 1.3 points. Album
  units and gross rise about 1–2%.
- **Share moves from Majors and MidTiers to Independents** (1961 gross −1.6 and −0.9, +2.1). Independents' earnings
  rise $1.5–2.9M a year, about half of it the founder salary that is now paid only from surplus.
- **Small labels are flat:** they were never above the cliff, and fewer runtime births offset their lower closures.

### Chart health (Billboard in brackets)

| Three-seed mean | `ph13` 1960 / 1961 | `gfo` 1960 / 1961 | Billboard |
|---|---|---|---|
| Top 10 label CR4 (slots) | 27.9 / 37.2 | 30.9 / 32.6 | 34.6 / 27.9 |
| Top 10 label CR8 | 44.8 / 56.5 | 47.2 / 49.7 | 52.7 / 43.5 |
| Top 10 firms | 60.7 / 52.3 | 62.0 / 55.7 | 49 / 51 |
| … with one Top 10 record | 38.7 / 35.7 | 43.7 / 37.7 | 31 / 27 |
| Top 10 records | 123 / 119 | 117 / 119 | 102 / 115 |
| Hot 100 records | 666 / 637 | 670 / 637 | 683 / 783 |
| Hot 100 weeks per record | 8.0 / 8.2 | 7.9 / 8.2 | 7.6 / 6.6 |
| #1 records | 21.3 / 19.7 | 18.3 / 21.0 | 19 / 22 |

- **Concentration moves toward Billboard in both years:** 1960 CR4 / CR8 up toward 34.6 / 52.7, 1961 down toward
  27.9 / 43.5 (the control's 1961 over-concentration halves).
- **The firm count overshoots more:** 2–3 more Top 10 firms and 2–5 more one-record firms, already above Billboard's
  31 / 27. Survivors get more one-off Top 10 hits.
- Record counts, Hot 100 run length and #1s do not move. The 1961 Hot 100 shortfall (637 vs 783 records, 8.2 vs 6.6
  weeks) predates this switch.

### The Southern question

| | `ph13` / `hs13` | `gfo` |
|---|---|---|
| Southern procedural small labels defunct by week 104 (3 seeds) | 22.7% (elsewhere 21.2%) | 9.5% (elsewhere 10.5%) |
| Southern small-label singles 1961, seeds 2002 / 1001 | 6.56 / 3.57M | 7.66 / 6.56M |
| … home fraction | 23 / 29% | 24 / 23% |
| … uncharted records' home fraction | 41 / 44% | 40 / 40% |
| … units on labels with a P&D deal | 21 / 33% | 45 / 22% |
| Deep South points of small-label Hot 100 share, 1961 | 5.4 / 3.5 | 6.4 / 4.9 |
| Deep South Hot 100 / Top 10 record-weeks, 1961 (3 seeds) | 5.8 / 3.3% | 6.0 / 3.5% |

- **The fix is neutral for the South.** Southern labels' closures were the cliff like everyone else's (22.7 → 9.5%, no
  regional gap), so they gain volume and a point or so of small-label chart share, but each record keeps the same
  export profile: home fractions do not move and uncharted Southern records still sell 60% away.
- The uncharted spread remains §36 problem 2 (supply-only geography, opening networks). This switch removes a
  confound from that screen: closures no longer thin out the labels whose networks are being tested.

**Reading:** the economy and chart side effects are small and mostly in the right direction (concentration toward
Billboard; Independents' share up, Majors' down). Two costs to watch: the one-record Top 10 firm excess grows, and
money-losing singles rise 1–2 points from the fixed purse. The §37 open question (five-year closure rate) still decides
adoption.

## 39. Graduated founder overhead at five years (2026-11-16)

`gfo260` = `gfo` flags at 260 weeks, seeds 2002 / 1001, no control (read against the 20–35% five-year band for Small
labels, `small-labels-never-lose-money`). Reference without the switch: `lch-cand` (an earlier 208-week candidate,
before the census and proven-hit gates), whose procedural Independents close 25 / 28% by week 104 and 30 / 33% by week 208. Analysis:
`SimTools/analyze_closure_horizon.py gfo260 --weeks 52 104 156 208 260`. Runtime 48 minutes per seed.

| Opening procedural cohort, defunct by week (2002 / 1001) | 104 | 208 | 260 |
|---|---|---|---|
| Independent | 8.0 / 11.3% | 10.6 / 13.0% | **11.2 / 13.6%** |
| Boutique | 4.5 / 5.0% | 13.5 / 10.1% | **16.2 / 10.9%** |
| Small | 15.6 / 17.4% | 24.2 / 27.4% | **29.4 / 32.4%** (band 20–35%) |
| MidTier (untouched) | 35 / 54% | 40 / 62% | 40 / 62% |

Week 104 reproduces the §37 screen (8 / 11%). Runtime births: 179 / 186, of which 28 / 25 closed.

- **Small labels land in the band** (29–32%, mostly dormancy). About 55% of surviving starting Smalls have been promoted
  to Independent by 1964.
- **Independents almost stop closing after year two:** +3 points over 1962–64. Boutiques keep closing slowly
  (dormancy), reaching 11–16%.
- **Why survivors do not run down.** Year-five survivors split into charted (two thirds of Independents, a Hot 100 record in
  1963–64) and uncharted. Uncharted Independents in 1964: ~14 singles a year, ~55k units, $80–85k gross, median pretax −$1–2k,
  56–57% losing money, but on $29–30k cash and $450–570 monthly overhead. Losses that small against that buffer take
  decades to bankrupt a firm, and dormancy needs a founder with no releases, which these firms never are. Charted
  Independents earn $18–23k pretax.
- **Reading:** the cliff closures are gone and were unearned. What remains is a different, older gap: a no-hit label sits at
  break-even indefinitely, because ~4k units a release roughly pay for its records. That is the per-release volume
  question in `small-labels-never-lose-money` and `singles-volume-is-a-fixed-purse`, not a reason to keep the cliff.
- **Recommendation:** adopt (it replaces roster-set closures with earned ones and moves chart concentration toward
  Billboard, §38), and treat the low five-year Independent closure rate (11–14%) as a separate question:
  why a no-hit Independent's records break even.

**Adopted 2026-11-16:** `--graduated-founder-overhead` is in `SimLogs/writing-calibration/candidate-request.json` (247 flags;
previous: `candidate-request-pre-gfo-2026-11-16.json`).

## 40. Why a no-hit label breaks even: the RIAA 1963 release survey against the model (2026-11-16)

Source: RIAA / Glover statement, House Judiciary Subcommittee 3, 10 June 1965, Exhibit 10 (p. 799) and pp. 861–865;
details in `SimTools/RIAA-1965-LP-sales-research-notes.md`. Six companies (Mercury, Capitol, Columbia, RCA Victor,
Laurie, Kapp; 32% of 1963 industry releases), records released in 1963 with sales counted through 1964. Break-even is a
release-weighted company average and allocates recording overhead and G&A per selection (p. 862). Singles: 1,189
releases, 53.3% under 2k, break-even 11,200, 74% fail; revenue $.440, contribution $.259 before S,G&A. Popular LPs:
1,603 releases, 35.8% under 2k, break-even 7,800, 61% fail; revenue $1.818, contribution $.902 before S,G&A.

Model: `gfo260` (candidate + graduated overhead, 260 weeks, seeds 2002 / 1001), the same cohort (released weeks
157–208, sales to week 260; retired records from `release-outcomes`, LPs still selling from `live-records-snapshot`).
"Fail" = realized net < 0; "+ overhead" also charges each release its label's 1963–64 overhead per release. Scripts:
`SimTools/analyze_riaa_1963_cohort.py gfo260`, `SimTools/analyze_riaa_1965_breakeven.py gfo260` (retired 1962–64, with
a per-tier fit of realized net = contribution × copies − fixed cost).

### Singles: the model matches the survey

| Releases in 1963, sales to end 1964 | < 2k | 2–10k | 10–50k | 50k+ | fail | fail + overhead |
|---|---|---|---|---|---|---|
| RIAA | 53.3% | 19.7% | 14.9% | 12.2% | 74% | (included) |
| Model, all tiers (n 10,316) | 61.8% | 17.5% | 13.2% | 7.5% | 73% | 79% |
| Model, Major + MidTier (n 2,299) | 39.3% | 17.2% | 26.8% | 16.9% | 64% | 78% |

| Retired singles 1962–64, fit by tier | Major | MidTier | Indie | Boutique | Small | RIAA |
|---|---|---|---|---|---|---|
| Contribution per copy | $0.246 | $0.269 | $0.263 | $0.269 | $0.283 | $0.259 |
| Fixed cost per release | $4,283 | $2,756 | $1,042 | $807 | $509 | $2,900 incl. allocated S,G&A |
| Break-even copies | 17,400 | 10,200 | 4,000 | 3,000 | 1,800 | 11,200 |

- Per-copy margin is right in every tier. Fixed cost is the authored `AILabel.GetProductionCost` table ($4,000 / 2,000 /
  800 / 600 / 350) plus launch marketing; the survey firms' average sits between the model's Major and MidTier.
- The survey firms (mostly majors) had more flops than the model's Majors and MidTiers (53% vs 39% under 2k, and
  fewer 10–50k records), but the loss rate with overhead lands on target (78% vs 74%).
- **No-hit labels' singles already lose money as history says:** surviving Independents with no Hot 100 record in
  1963–64 sell a median 250–460 copies per single, 84–85% fail, and their singles net slightly negative in total.

### LPs: the survival margin, and the miss

| Releases in 1963, sales to end 1964 | < 2k | < 4k | 10–50k | 300k+ | fail | fail + overhead |
|---|---|---|---|---|---|---|
| RIAA | 35.8% | 48.8% | 22.9% | 1.4% | 61% | (included) |
| Model, all tiers (n 7,539) | 11.3% | 25.2% | 43.4% | 0.0% | 20% | 26% |
| Model, Major + MidTier (n 3,155) | 0.5% | 2.0% | 74.9% | 0.0% | 17% | 26% |
| Model Independent / Boutique / Small, median copies | 5,348 / 5,619 / 2,714 | | | | 23 / 17 / 20% | |

- **The no-hit labels live on LPs.** Over 1963–64 the uncharted surviving Independents (~50 per seed) issue ~11 LPs each,
  median 5.4k copies, 24–26% failing, worth $3.6–4.6M; their singles lose $0.1–0.2M. The "4k units pays for its records"
  of §39 was the LP line.
- **Shape:** the model has almost no LP flops (Major + MidTier 0.5% under 2k vs 35.8%) and no LP over 300k (vs 1.4%);
  three quarters of the RIAA-like firms' LPs sit in 10–50k. This is the same miss as `riaa-1965-lp-distribution-check`
  (2026-11-15), still present after `--lp-launch-standing=30000`.
- **Margin:** LP contribution is $1.24–1.37 a copy against $0.90 before S,G&A, and the fitted fixed cost per Independent
  LP is $2,940 (break-even 2.4k) against the survey's 7,800 break-even.

**Reading:** nothing about singles needs to change. The levers for the no-hit label are the LP floor (unknown-act LPs
sell 5k+ on small labels, 20k on Majors) and the LP per-copy margin, which together set a 2–3k LP break-even.
Lowering LP income without moving LP volume raised bankruptcies before (`unknown-act-lp-floor-is-scale-plus-label-push`);
here that is the intended effect, but it must be earned on the LP flop share (target 36% < 2k, 49% < 4k, 61% fail) and
must keep the LP market total (stores + racks + budget + clubs) on Billboard.

## 41. LP flop share: the reception draw on the current candidate (2026-11-16)

`--lp-reception-sigma=S` (GameyBehaviorAudit.md §2.17; `GameyAuditSwitches.LpReception`) gives every LP with a roster
act a mean-preserving reception multiplier on store conversion, exp(S z − S²/2), z keyed on record id and world seed (no
RNG stream, no saved state; forecasts do not see it). On 2026-11-15 σ 1.5 reproduced the RIAA shape but was held back for
+35–45 small-label bankruptcies, then read as imposed. With the graduated overhead adopted and no-hit labels meant to be
able to fail (§39–40), it is re-screened here.

Runs: `gfo` (= candidate) vs `lrs10` / `lrs15` (+ σ 1.0 / 1.5), 104 weeks, seeds 2002 / 1001. Scored on LPs released in
1960 with sales to week 104 (the RIAA window shifted to the run's first two years): `SimTools/analyze_riaa_1963_cohort.py RUN
--launch 1-52`; guards: `SimTools/analyze_lp_flop_screen.py gfo lrs10 lrs15`.

| LPs released 1960, sales to end 1961 | < 2k | < 4k | 4–20k | 20–100k | 300k+ | fail | fail + overhead |
|---|---|---|---|---|---|---|---|
| RIAA (1963 releases to end 1964) | 35.8% | 48.8% | 27.8% | 18.3% | 1.4% | 61% | (included) |
| `gfo` all tiers | 6.2% | 17.7% | 45.1% | 34.2% | 0.2% | 6% | 10% |
| `lrs10` | 18.6% | 31.4% | 39.3% | 25.7% | 0.6% | 22% | 28% |
| `lrs15` | **33.2%** | **46.1%** | 31.3% | 19.1% | 1.0% | **40%** | **45%** |
| `gfo` / `lrs15` Major + MidTier < 2k | 0.1% / 15.1% | | | | | 4% / 39% | 12% / 50% |

Per tier, `lrs15` (median copies, < 4k): Major 12.6k, 25%; MidTier 19.6k, 26%; Independent 3.8k, 52%; Boutique 3.2k,
56%; Small 1.5k, 70%. A third of the cohort is still selling at week 104, so final fail rates will be somewhat lower.
The RIAA bands are probably per edition (mono and stereo; §2.18), so the Major + MidTier per-title shortfall is about
half what it looks.

| Guards, mean of 2002 / 1001 | `gfo` | `lrs10` | `lrs15` |
|---|---|---|---|
| LP market 1961, stores + racks + budget + clubs (M) | 170.5 | 167.9 | 169.6 |
| … stores / racks 1961 (M) | 68.4 / 37.0 | 65.1 / 37.6 | 63.5 / 41.3 |
| Singles 1961 (M) | 128.0 | 128.0 | 128.0 |
| Single + album gross 1961 ($M) | 374.0 | 365.4 | 371.2 |
| Procedural Independent / Boutique / Small defunct by week 104 | 9.6 / 4.8 / 16.5% | 9.9 / 7.4 / 18.9% | **16.8** / 6.6 / 18.6% |
| Procedural MidTier defunct | 44.4% | 53.3% | 53.5% (30 / 77) |
| All labels defunct by week 104 | 75 | 86.5 | **100.5** |
| Small-label share of LP units 1960 / 1961 | 29.6 / 28.1% | 29.3 / 29.2% | 23.0 / 26.7% |
| Surviving Independents with no 1961 Hot 100 record | 47.3% | 52.4% | 49.0% |

**Who closes.** Under σ 1.5 the dead procedural Independents (26 / 35 against 15 / 20) issued 0.7–0.8 LPs each, 85–88%
of which lost money, and only 2 / 7 had a chart record before closing; they fold on a first LP that flops against a
first pressing sized to a forecast that never saw the draw. Surviving Independents' LPs fail 43–44% (control 17–18%), but
their LP net per label rises ($115–120k against $104–116k): the draw is mean-preserving, so the lucky ones gain what the
flops lose.

- **σ 1.5 puts the LP shape on RIAA** (33 / 46% under 2k / 4k against 36 / 49%, the 4–100k bands within 2–6 points, a
  300k+ tail) while the LP market (−0.5%), singles and gross (−0.7%) hold. σ 1.0 gets half way.
- **The closures are earned on flops** in the sense the RIAA meant ("Producing and Selling Records Is a Risky
  Business"), but by a draw: the flop is the title's reception, not its quality or the label's judgement.
- **It does not close the §39–40 gap on its own.** The draw adds variance, not a lower mean: the share of surviving
  Independents without a hit is unchanged (49% vs 47%), and their LP income per label does not fall. A no-hit label
  still breaks even on average; it now risks ruin on its first flops instead. The mean is the per-copy LP margin
  ($1.24–1.37 against $0.90; §40), the second lever.
- **Watch:** MidTier closures (seed 1001 77%, n 13); small-label LP share 1960 −6.6 points (1961 −1.4).
- **Recommendation:** σ 1.5 as the shape fix, checked at 260 weeks on the RIAA 1963 cohort and five-year closures before
  adoption; then the LP margin screen against the §40 per-copy table.

## 42. LP margin per copy: the label's share of list, on σ 1.5 (2026-11-16)

`--lp-label-price-share=S` (`LabelPressingEconomics.AlbumLabelShareOfList`): what an AI label books on an LP it sells,
as a share of list. The default is the dealer share (0.63 / 0.98 = 0.643). The RIAA 1963 survey has the companies
realizing $1.818 an LP, about 0.45 of a $3.98 / $4.98 list mix. They sold to distributors and branches below dealer
price and gave free goods and return credit. Artist royalty and mechanicals stay on list. The release gate prices with the same
`LabelPrice`, so labels see the cut when deciding to issue. Singles, budget editions and clubs are unchanged.

Runs: `lrs15` (σ 1.5) vs `lps55` / `lps50` (σ 1.5 + share 0.55 / 0.50), 104 weeks, seeds 2002 / 1001, built from the
`gfo-2002-sc` request; `gfo` is the σ-off control. Scripts: `analyze_lp_flop_screen.py gfo lrs15 lps55 lps50` (new
rows: LPs issued, LP net per copy, the no-hit surviving Independents' LPs), `analyze_riaa_1965_breakeven.py RUN --years
1960 1961`, `analyze_riaa_1963_cohort.py RUN --launch 1-52`.

| Retired LPs 1960–61, median net per copy | Major | MidTier | Indie | Boutique | Small | all |
|---|---|---|---|---|---|---|
| RIAA contribution before S,G&A | | | | | | $0.902 |
| `lrs15` | $1.230 | $1.265 | $1.328 | $1.352 | $1.537 | $1.328 |
| `lps55` | $0.977 | $1.014 | $1.064 | $1.126 | $1.241 | $1.077 |
| `lps50` | **$0.862** | **$0.884** | **$0.915** | **$0.961** | $1.030 | **$0.932** |

| Mean of 2002 / 1001 | `gfo` | `lrs15` | `lps55` | `lps50` |
|---|---|---|---|---|
| LPs issued in the run (retired + live) | 5,412 | 4,984 | 4,416 | 3,995 |
| … of which released 1960 by Major + MidTier / Indie | | 1,328 / 1,411 | 1,089 / 1,325 | 896 / 1,252 |
| No-hit surviving Indies: LPs per label | 9.3 | 8.6 | 7.6 | 7.0 |
| No-hit surviving Indies: LP realized net per label ($k) | 98 | 115 | 92 (64 / 119) | **74** (73 / 75) |
| No-hit surviving Indies: LP fail % | 18 | 44 | 48 | 49 |
| Surviving proc. Independents with no 1961 Hot 100 record | 47.3% | 49.0% | 47.9% | 43.4% |
| Procedural Independent / Boutique / Small defunct by week 104 | 9.6 / 4.8 / 16.5% | 16.8 / 6.6 / 18.6% | 14.8 / 6.1 / 17.6% | 15.9 / 8.2 / 19.7% |
| Procedural MidTier defunct | 44.4% | 53.5% | 63.5% | 62.1% |
| All labels defunct by week 104 | 75 | 100.5 | 95.5 | 104.5 |
| LP market 1961, stores + racks + budget + clubs (M) | 170.5 | 169.6 | 167.6 | **159.7** |
| … stores / racks 1961 (M) | 68.4 / 37.0 | 63.5 / 41.3 | 60.0 / 42.5 | **54.1** / 40.5 |
| Small-label share of LP units 1960 / 1961 | 29.6 / 28.1% | 23.0 / 26.7% | 27.6 / 28.8% | **32.7 / 33.5%** |
| LP shape, 1960 releases: < 2k / < 4k / fail | 6 / 18 / 6% | 33 / 46 / 40% | 31 / 43 / 41% | 29 / 42 / 41% |

- **0.50 puts the margin on the survey.** The median net per copy is $0.86–1.03 by tier, against $0.902, and the
  no-hit Independents' LP income falls by a third ($115k to $74k per label over two years, both seeds agreeing).
- **The labels answer through the release gate, not by going broke.** LPs issued fall 20% from `lrs15`. Closures do
  not move outside noise (all labels 100.5 to 104.5; Independents 16.8 to 15.9%), and the share of survivors with no hit
  does not fall (49.0 to 43.4%). Two years is too short to see a lower mean turn into closures; at 104 weeks the no-hit
  label still nets $74k from LPs.
- **The cut lands on the big labels.** Major + MidTier LP releases fall a third (1,328 to 896) and Indies' 11%: the
  Major's higher fixed cost per LP (about $10.6k) clears the gate less often on a lower margin. The small-label share
  of LP units rises 7–10 points to 33%, and store LPs fall 15% (63.5M to 54.1M), so the LP market (−6%) leaves the
  Billboard guard. Fewer Major titles shift the size bands up a little: Major + MidTier < 2k falls 15 to 11%.
- **0.55** is in between: margin $1.08, market −1%, Majors −18% LPs, no-hit LP net $92k with the seeds split (64 / 119).

**Reading:** the margin is the right number to change (period-sourced, and it moves the mean). At 0.50, though, the
Majors' release gate turns it into a cut in Major LP volume. That is the opposite of history: the survey firms issued
1,603 popular LPs at this margin. Before adoption the Major LP count has to hold. Two options: (a) check whether the
gate's Major LP forecast is low (`debut-lp-forecast-is-3x-low`), so the lower margin exposes a forecast bias rather than a
true loss; (b) restore store volume with `--lp-demand-scale`, which would not restore the titles. The closure effect
needs the 260-week check (RIAA 1963 cohort, five-year closures) with σ 1.5.

## 43. Why the lower LP price cuts Major LPs: the gate's Major forecast is low (2026-11-17)

§42 found that at `--lp-label-price-share=0.50` Major + MidTier LP releases fall by a third. Here the release gate's LP
forecast is checked against what the released LPs did, on `gfo` (σ off), `lrs15` and `lps50`. Script:
`SimTools/analyze_lp_gate_forecast.py RUN` (gate attempts and forecast vs realized net by tier); the units split below
joins `release-strategy` (projectedAlbumNet, albumMarginPerUnit) with `release-campaign-costs` (productionCost), and
forecast units = (projected net + production) / margin. LPs chosen in weeks 1–52, sales to week 104, seeds 2002 / 1001.

**Gate attempts, Major `AlbumStandalone`:** commit 30% → 11% from `lrs15` to `lps50`; NewSigning (82–85% of attempts)
29% → 10%, median forecast −$3.9k → −$5.2k; Rising 41 → 18%; Star 99 → 58%. Independents 88 → 67%, MidTiers 71 → 61%.

| LPs released 1960: forecast units (mean) vs sold to week 104 | `gfo` NewSigning | `gfo` Rising+ | `lrs15` NewSigning | `lps50` NewSigning | still selling (`gfo`) |
|---|---|---|---|---|---|
| Major | 23.5k → 36.0k (**1.53×**) | 37.5k → 91.6k (**2.44×**) | 2.32× | 1.29× | 86–88% |
| MidTier | 1.54× | **2.89×** | 1.56× | 1.47× | 69–84% |
| Independent | 12.5k → 11.2k (0.90×) | 1.51× | 0.68× | 0.75× | 35–59% |
| Boutique | 1.22× | 1.37× | 0.76× | 1.62× | 40–72% |
| Small | 1.06× | 1.52× | 0.78× | 0.91× | 21–57% |

- **The miss is the tier, not the career.** On `gfo`, where the ratio of medians agrees with the ratio of means, a Major
  debut LP sells 1.5× its forecast and a Major Rising-or-better LP 2.4×. 86–88% of these are still selling at week
  104, so the final ratio is higher. A small label's debut is on forecast (0.9–1.2×). The candidate's
  `--album-prior-debut-scale=2.0613` is one number for every tier, so it lands the small labels and leaves Majors and
  MidTiers short. That is the earlier finding (`debut-lp-forecast-is-3x-low`, `major-lp-count-is-profit-gate-bound`) still
  present after the debut scale.
- **The margin side is not a Major bias.** The gate's margin per copy ($0.99 at the dealer share, $0.61 at 0.50) is
  about $0.45–0.55 below the record ledger's net per copy ($1.54 / $1.12). The difference is the company sale costs
  (fulfilment, trade support, excise), charged at the company level, which run 17–26% of gross in every tier
  (`company-monthly`, 1961). The gate is right to charge them. Two consequences: the §42 per-copy table, which is
  record net, flatters every tier by that much; and the RIAA $0.902 is "before S,G&A", so the comparison stands as made.
- **Why Majors fall hardest:** a Major LP carries $10.3k of production against $2.3k for an Independent. At the gate's
  $0.61 margin the Major breaks even at about 17k units. Its debut forecast median is 19–24k, so most fall near the
  line, while the sales it does not forecast (≥1.5×) would have cleared it.

**Reading:** the LP margin cut at 0.50 is blocked by a Major/MidTier LP forecast that is 1.5–2.9× low, not by Major LPs
losing money. A fair test of the margin needs that forecast fixed first. Two ways:
(a) **earned:** each label scales its LP unit forecast by its own retired LPs' realized/forecast ratio, a geometric
memory like `pressingForecastAlbumLogUnits` (no tier constant; a new label starts at 1);
(b) **calibrated:** a per-tier debut/Rising prior scale in place of the single `--album-prior-debut-scale`.
(a) is preferred under `closures-must-be-earned-not-imposed`, but its memory fills from retirements, which for Major LPs
come late (86% still selling at week 104). It may need the live-record maturity curve (`album-format-memory-provisional-bias`).

## 44. Earned LP forecast: each label learns its own LP forecast error (2026-11-17)

`--earned-lp-forecast` (`Systems/EarnedLpForecast.cs`). When an AI label's LP reaches 52 weeks, or retires sooner, it
adds the copies sold so far and the units the gate forecast at release to two running totals, each weighted 0.1. The
gate then multiplies that label's LP unit forecasts by their ratio (clamped 0.25–6), shrunk toward 1 by n / (n + 4)
while the label has few LPs. Using totals rather than averaging each LP's ratio keeps the σ 1.5 draw from biasing it
downward on average. It adds two floats and a count on `AILabel` and the release forecast on `RecordRuntimeData`. It
uses no random draws, and with the switch off nothing is read or written. The forecast at release is cached per
(label, act) when the gate prices the LP. It is not saved, so an LP released after a load without a fresh forecast is
never observed. Checks: `--label-pressing-check` (inert when off, observed once, ratio of totals, shrinkage).

Runs: `elf` = `lrs15` + switch; `elf50` = `lps50` + switch (both σ 1.5), 104 weeks, seeds 2002 / 1001. Learning starts
at week 53, so 1960 matches the controls (LP market and gross agree to 0.1M) and only 1961 differs. Scripts:
`SimTools/analyze_earned_lp_forecast.py lrs15 elf lps50 elf50` and `analyze_lp_flop_screen.py` on the same four runs.

| 1961, mean of seeds | `lrs15` | `elf` | `lps50` | `elf50` |
|---|---|---|---|---|
| Major LPs chosen (per seed) | 1,252 | 1,253 | 696 | **1,000** |
| Major gate commit | 27% | 33% | 11% | **23%** |
| Major LPs chosen: median gate forecast / median sold | $8.2k / 6.6k | $18.2k / 6.2k | $7.2k / 10.8k | $11.2k / 7.7k |
| MidTier LPs chosen / commit | 226 / 75% | 252 / 81% | 176 / 61% | 185 / 67% |
| Indie LPs chosen / commit | 1,284 / 89% | 1,222 / 80% | 1,078 / 68% | 1,046 / 61% |
| Boutique / Small commit | 88 / 80% | 84 / 71% | 69 / 60% | 61 / 54% |
| LP market (M) | 169.6 | 167.7 | 159.7 | **163.9** |
| Single + album gross ($M) | 371.2 | 364.8 | 287.6 | 298.1 |
| All labels defunct by week 104 | 100.5 | 109.5 (+11 / +7) | 104.5 | 107.5 (+6 / 0) |
| Independent / Small defunct | 16.8 / 18.6% | 18.5 / 21.4% | 15.9 / 19.7% | 18.4 / 19.7% |
| Small-label share of LP units | 26.7% | 28.8% | 33.5% | 30.0% |
| No-hit surviving Indies: LP net per label ($k) | 115 | 121 | 74 | 77 |

- **Majors learn what §43 measured.** At the 0.50 price, Major LPs chosen in 1961 rise from 696 to 1,000 and the gate
  passes 23% instead of 11%. That recovers about 55% of the gap to the dealer price, inside one year of learning. The
  added Major LPs sell less (median 7.7k against 10.8k), as expected for LPs that only just clear the gate. The LP
  market recovers 4.2M of the 9.9M lost, and the small-label share of LP units falls back from 33.5% to 30%.
- **At the dealer price the Major count does not move** (1,252 → 1,253) though the gate passes more (27 → 33%): there,
  something other than the gate caps Major LPs (the programme limit of 10 a month; `major-lp-count-is-profit-gate-bound`).
- **Small labels learn a correction below 1 and pass the gate less** (Indie 89 → 80% at the dealer price, 68 → 61% at
  0.50; Small 80 → 71%, 60 → 54%). Two causes. Under σ 1.5 a label with only a few LPs usually sees a total below its
  expected value, because a few big hits carry the average, and shrinkage toward 1 with 4 prior LPs only partly offsets
  that. And Independent debuts really do sell a little under forecast (0.9× on `gfo`, §43). The `elf` closures
  (+9, both seeds up) are mostly Independent and Small; they are within noise at `elf50` (+3).
- **Not yet measured:** the learned multipliers themselves (no telemetry; inferred here from gate pass rates and
  median forecasts), and anything past one year of learning.

**Reading:** the earned memory removes about half of the Major LP cut at the RIAA margin without any tier constant, and
the remaining gap should shrink as more Major LPs reach 52 weeks. Before adoption: (1) log each label's multiplier and
observation count (a weekly or yearly label telemetry column); (2) settle the small-label downward drift: either
stronger shrinkage (ShrinkRecords 8–10) or learning on log units with the label's observed spread; (3) the 260-week
check of σ 1.5 + 0.50 + earned forecast on the RIAA 1963 cohort, five-year closures and the Major LP count against
the bsnpubs range (143–209 titles a label a year).
