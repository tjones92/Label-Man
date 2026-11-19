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

## 45. Earned LP forecast: telemetry and the small-label fix (2026-11-17)

**Telemetry.** With `--earned-lp-forecast` on, the runner writes `RUN-earned-lp-forecast.csv`, one row per LP the
memory learns from (week, label, tier, record, age, forecast units, copies sold, observations, totals ratio, log
mean and variance, learned ratio, multiplier). `SimTools/analyze_earned_lp_telemetry.py RUN` reports, per tier, the
**pooled** ratio (all observed LPs' copies sold over forecast units, the unbiased value a label should learn) against
each label's final learned ratio and multiplier. Observations begin at week 36: LPs that retire before 52 weeks,
mostly flops, are observed first.

**Two fixes screened**, on σ 1.5 + `--lp-label-price-share=0.50`, 104 weeks, seeds 2002 / 1001:
- `elk10`: the totals estimator with `--earned-lp-forecast-shrink=10` (default 4).
- `ell2`: `--earned-lp-forecast-log`, the label's log-ratio mean m and variance v, learned ratio exp(m + v/2), the
  lognormal mean. LPs weigh equally until a label has 10, then exponentially (0.1). v is corrected by n / (n − 1),
  and only the learned ratio is clamped. A first version (`ell`) seeded the mean with the label's first LP, weighted
  later LPs 0.1, and clamped each LP's ratio to 0.25–6. So a label with a few LPs learned almost only from its first
  (often an early-retiring flop), and the clamp cut off the hits the variance needs: Majors learned 1.31 against a
  pooled 2.37 and the Major gain halved (814 LPs). Fixed in `ell2`.

`elt` is `elf50` (§44) rerun with telemetry; it is byte-identical to `elf50` (release-strategy compared on both seeds).

| End of 1961, median over labels: learned ratio (pooled) / multiplier / share of labels < 1 | `elt` totals, shrink 4 | `elk10` totals, shrink 10 | `ell2` log, shrink 4 |
|---|---|---|---|
| Major (17 labels, median 43 LPs) | 1.58 (2.33) / 1.52 / 12% | 1.56 (2.27) / 1.45 / 18% | **2.24 (2.29)** / 2.06 / 12% |
| MidTier (30, 8) | 2.51 (3.06) / 1.79 / 27% | 2.59 (3.04) / 1.53 / 26% | 3.43 (3.06) / 2.29 / 20% |
| Independent (~420, 4) | 0.62 (1.10) / 0.81 / 69% | 0.62 (1.05) / 0.88 / 69% | **0.98 (1.09)** / 0.99 / 51% |
| Boutique (~193, 3) | 0.54 (1.87) / 0.79 / 71% | 0.53 (1.83) / 0.88 / 73% | 0.75 (1.78) / 0.89 / 58% |
| Small (~300, 2) | 0.37 (0.94) / 0.76 / 75% | 0.37 (0.91) / 0.88 / 76% | 0.48 (0.93) / 0.81 / 66% |

| 1961, mean of seeds | `lps50` (no memory) | `elt` | `elk10` | `ell2` |
|---|---|---|---|---|
| Major LPs chosen per seed / gate commit | 696 / 11% | 1,000 / 23% | 968 / 21% | **1,027 / 28%** |
| MidTier commit | 61% | 67% | 67% | 72% |
| Indie / Boutique / Small commit | 68 / 69 / 60% | 61 / 61 / 54% | 64 / 63 / 56% | **66 / 64 / 57%** |
| LP market (M) | 159.7 | 163.9 | 164.0 | 164.3 |
| All labels defunct by week 104 | 104.5 | 107.5 | 110.0 | 108.5 |
| Small-label share of LP units | 33.5% | 30.0% | 31.3% | 31.1% |

- **The totals estimator is biased for small labels, as suspected.** The pooled Independent ratio is 1.10, but the
  median Independent learns 0.62, and 69% of small labels end below 1. Shrinkage 10 only damps the multiplier
  (0.81 → 0.88); the learned ratio is still 0.62. Majors learn below their pooled ratio too (1.58 against 2.33), so
  the skew costs them as well.
- **The log mode learns the pooled ratio** for the tiers with enough LPs: Major 2.24 against 2.29 and Independent 0.98
  against 1.09. Major LPs reach 1,027 and the gate passes 28% (dealer price 27%). Small-label gate rates come back to
  within 2–3 points of the no-memory control.
- **What remains:** Small (2 LPs) and Boutique (3) labels still learn low (0.48 / 0.75): two or three LPs give a noisy
  variance. Boutique's pooled 1.78 also rests on a few big sellers. The closures are within noise of each other
  (104.5–110). The LP market recovers 4.6M of the 9.9M lost at 0.50.

**Reading:** adopt the log mode as the estimator (`--earned-lp-forecast --earned-lp-forecast-log`), still behind the
0.50 price. Before adoption: the 260-week check of σ 1.5 + 0.50 + log-mode earned forecast (RIAA 1963 cohort,
five-year closures, Major LPs a label a year against bsnpubs 143–209), which also shows the memory past its first
year. If labels with 2–3 LPs still drift low there, give them a prior variance (σ²₀ ≈ 2, the pooled log spread) blended
with their own until they have about 5 LPs.

## 46. 260-week check of the LP package; adopted (2026-11-17)

Package: `--lp-reception-sigma=1.5 --lp-label-price-share=0.50 --earned-lp-forecast --earned-lp-forecast-log`
(§41, §42, §44–45). Runs: `gfo260` (control: the candidate plus `--regional-home-sales`, from §40), `lrs260` (+ σ 1.5
only), `pkg260` (+ the package). All 260 weeks, seeds 2002 / 1001. Scripts: `SimTools/analyze_lp_package_260.py`
(yearly market, Major LPs per Major label, small-label LP share, cumulative closures), `analyze_riaa_1963_cohort.py`,
`analyze_closure_horizon.py`, `analyze_earned_lp_telemetry.py`.

**RIAA 1963 cohort** (LPs released in 1963, sales to the end of 1964):

| | < 2k | < 4k | 10–50k | 300k+ | fail | fail + overhead |
|---|---|---|---|---|---|---|
| RIAA | 35.8% | 48.8% | 22.9% | 1.4% | 61% | (included) |
| `gfo260` | 11.3% | 25.2% | 43.4% | 0.0% | 20% | 26% |
| `lrs260` | 36.2% | 49.4% | 24.8% | 0.4% | 51% | 56% |
| `pkg260` | **34.0%** | **47.3%** | **26.9%** | 0.4% | **55%** | **61%** |
| `pkg260` Major + MidTier | 21.6% | 33.2% | 34.7% | 0.7% | 57% | 66% |

| Mean of 2002 / 1001 | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| LP market (M), `gfo260` / `lrs260` / `pkg260` | 144.3 / 144.9 / 136.6 | 172.6 / 171.8 / 166.5 | 176.3 / 175.0 / 175.2 | 173.1 / 172.8 / 172.0 | 167.7 / 167.7 / 167.4 |
| Major LPs per Major label (bsnpubs 143–209) | 65 / 64 / 38 | 125 / 112 / 95 | 170 / 156 / **150** | 182 / 181 / **157** | 186 / 182 / **164** |
| Small-label share of LP units | 29.6 / 23.0 / 32.8% | 28.1 / 26.7 / 31.1% | 23.6 / 22.2 / 24.9% | 21.9 / 18.9 / 24.0% | 21.1 / 20.1 / 22.6% |
| Labels defunct, cumulative | 14.5 / 19.5 / 19.5 | 75 / 100.5 / 108.5 | 107.5 / 163 / 180 | 124 / 208.5 / 227.5 | **152.5 / 261 / 287.5** |
| Record gross ($M) | 291.5 / 291.8 / 230.4 | 374.0 / 371.2 / 299.1 | 387.6 / 382.9 / 317.9 | 384.2 / 382.5 / 317.5 | 379.7 / 381.0 / 317.8 |

Opening procedural labels defunct by week 260, seeds 2002 / 1001: Independent 11 / 14% (`gfo260`), 21 / 30%
(`lrs260`), 30 / 31% (`pkg260`); Boutique 16 / 11%, 30 / 28%, 25 / 31%; Small 29 / 32%, 46 / 45%, 49 / 50%. Runtime births
rise with the closures (179 / 186 → 255 / 286 → 288 / 292): the field churns and refills rather than thinning out.

- **The LP shape is on the survey.** Under 2k / under 4k / fail are 34 / 47 / 55% against 36 / 49 / 61%, and 61% with
  overhead. A 300k+ tail now exists (0.4%; RIAA 1.4%).
- **The LP market holds from 1962** (−0.6% to −0.2%). The first two years are down 5.3% / 3.5% because the memory first
  learns at week 53, so in 1960 Majors face the lower price on the raw prior (38 LPs a label against 65).
- **Major LP counts are inside the sourced range** in 1962–64 (150–164 a label a year). That is about 10% below the
  control, which ran near the top of the range.
- **The closures are σ's.** σ 1.5 alone takes cumulative closures from 152.5 to 261. The margin and the earned forecast
  add 26 more. They are earned on LP flops in the sense of §40: no-hit labels can now fail, the flop share is on
  RIAA, and the market holds.
- **Record gross is −17% at the label.** That is the intended effect: labels book about 0.45–0.50 of list rather than
  the dealer share. The `gross` column is label receipts, not consumer spending.
- **Watch:** on seed 1001 one procedural opening Major (`label_0088`, $130k opening cash) never builds cash at the
  lower margin. It is demoted to MidTier and goes bankrupt in week 83; on the control it holds $2.5M by week 79. Other
  watch items: procedural MidTier closures (seed 1001, 77%, n 13, ~equal to σ-only 85%) and the 1960 Major LP dip.
- **Memory at 260 weeks** (`pkg260` telemetry): Majors learn 2.56 (pooled 2.13; median 497 LPs observed), Independents
  0.79 (0.91, 8 LPs), Boutiques 0.95 (1.10, 11), Small 0.39 (0.59, 3). Small labels with 2–3 LPs still learn low.

**Adopted.** The four flags are added to `SimLogs/writing-calibration/candidate-request.json` (251 flags; previous:
`candidate-request-pre-lp-margin-2026-11-17.json`). Open items: the opening-Major cash fragility at the lower margin
(`realistic-opening-cash-needs-credit`), a prior spread for labels with fewer than ~5 LPs, and the 1960–61 Major LP dip
before the memory learns.

## 47. The §46 open items: who closes, the seed 1001 Major, few-LP labels, the 1960 Major LP dip (2026-11-17, read-only)

All on the §46 runs (`gfo260`, `lrs260`, `pkg260`; seeds 2002 / 1001). New script: `SimTools/analyze_closure_economy.py RUN ...`
(closed labels by tier at closure: ever charted, LPs and singles issued, net by format, exit reasons; per year Hot 100
entries by tier family, firms charting, C4, distinct acts releasing, singles).

### Are the doubled closures flops, and do they touch the economy?

Small labels (Independent / Boutique / Small at birth, opening and runtime), closed by week 260, split by whether the
label ever issued an LP that lost money (`release-outcomes` realized net < 0):

| Closed / labels, seeds 2002 · 1001 | had a losing LP | LPs, none lost | never issued an LP |
|---|---|---|---|
| `gfo260` | 29 / 459 · 18 / 442 (**4–6%**) | 19 / 166 · 27 / 194 (11–14%) | 92 / 111 · 99 / 114 (83–87%) |
| `lrs260` (σ 1.5) | 110 / 592 · 125 / 605 (19–21%) | 13 / 69 · 15 / 73 | 114 / 151 · 123 / 172 |
| `pkg260` (package) | 139 / 612 · 127 / 599 (**21–23%**) | 13 / 74 · 19 / 84 | 116 / 159 · 133 / 173 |

- **The extra closures are the labels with losing LPs.** That group goes from about 24 closures a seed to 133. Labels
  whose LPs all made money close no more often (23 → 16 a seed), and labels that never issued an LP close at a steady 73–87%.
  Half the exits are bankruptcies and half are `Dormancy`: 12 quiet months, solvent, and too little free cash for a
  production (`FounderLabelLifecycle`). That is the same cash run-out, ending one step before bankruptcy.
- **Of the closed small labels, 98% never held $50k.** The mean closer issued 1.6 LPs and 7 singles, and 10% ever charted.
  No Major closed in any run. MidTier closures went 9.5 → 12.5 a seed.
- **The market holds.** Singles are unchanged (a fixed purse), the LP market is −0.2% by 1964 (§46), and C4 stays at 0.27–0.31.
  The Major + MidTier share of Hot 100 entries in 1964 is 55.5% control and 57.2% package, with the seeds split (2002 +7, 1001 −4).
- **What does move is breadth.** Firms charting in 1964: 196.5 → 179.5 (−9%). Distinct acts with a release in 1964:
  3,433 → 2,864 (−17%), of which acts on small labels 2,380 → 1,939. σ alone does half of each (179.0, 3,134). Births refill
  the field, but each new label carries a short roster. This is the cost of the package, and it is earned: fewer labels
  survive their flops, so fewer acts get a record out. No period target exists for it yet.

### Seed 1001, `label_0088` (Twister Records, procedural opening Major)

- **Five of six procedural opening Majors grow under the package** ($1.3–4.7M by week 104), including `label_0312`, which
  opens with less cash ($94k). Opening cash alone does not explain the failure.
- **It is the weakest opening Major on either seed.** First-month revenue is $182k under the package; the next lowest of
  16 Majors is $262k. It earns on 4 career acts (3 Rising, 1 Star). Its 12 NewSignings fail the gate at both prices (0
  commits in 1960 even on the control).
- **It starves; it does not flop.** Its records made money (LPs +$66k on $7k production, singles +$51k). From week 6 the
  commitment reserve refuses releases: overhead, plant payables, tax reserve and programme slots, all its own
  obligations. That includes a Rising act's LP in week 11. With fewer releases revenue falls ($93k → $20k a month by
  week 32) and the label turns `Struggling` (fewer release attempts), then is demoted and goes bankrupt in week 83. It
  never signs after week 13 (control: P&D clients from week 35).
- **The failure falls inside the 1960 dip window** (below): its Rising LP forecasts are on the unlearned prior
  ($3.3k album profit against $5.6k on the control). A fix for the dip should save it. No separate change is proposed.

### Small labels with 2–3 LPs learn low: correct, not bias

Replaying the log estimator on the `pkg260` telemetry, small labels grouped by how many LPs the memory saw by week 260:

| Small-tier labels, seeds 2002 · 1001 | closed: pooled sold / forecast | alive: pooled (born after wk 104) | learned ratio (median) |
|---|---|---|---|
| 1–3 LPs | 0.28 · 0.28 | 0.60 · 0.38 (81% · 78%) | 0.25–0.35 |
| 4+ LPs | 0.46 · 0.50 | 0.99 · 0.91 | 0.80–1.12 |

- **Labels with few LPs learn about what their LPs sold.** Half of them closed after those LPs, with pooled sales 0.28 of
  forecast. Most of the others were born after week 104 and are on their first LPs (pooled 0.38–0.60).
- The §45 idea of a prior variance (σ²₀ 2–2.25, worth 3–5 LPs) moves the median learned ratio by 0.1 or less in every tier.
  The within-label log spread is about 3 (σ² of the draw is 2.25), so a prior variance has little to add.
- The release-time forecast the memory compares against is the raw prior, cached before the multiplier
  (`CompetitorManager.CalculateAlbumPriorNet`), so there is no feedback.
- **Closed: no change.** The tier pooled ratio (Small 0.56–0.63) is not a target for a single label. It mixes mature labels
  near 1 with failing ones near 0.3.

### The 1960–61 Major LP dip

Major LPs chosen per quarter, mean of seeds (Major `AlbumStandalone` gate commit %):

| Quarter | Q1 | Q2 | Q3 | Q4 | Q5 | Q6 | Q7 | Q8 |
|---|---|---|---|---|---|---|---|---|
| `gfo260` | 26 (68) | 188 (34) | 159 (35) | 150 (29) | 242 (31) | 232 (33) | 231 (25) | 294 (30) |
| `pkg260` | 14 (35) | 112 (11) | 106 (13) | 72 (9) | 100 (9) | **213 (33)** | 230 (43) | 260 (45) |

The dip is five quarters (weeks 1–65) at about 55% of control, and it ends in Q6. It is the §46 cause: the memory
first observes at week 36 (early retirements) and 53, shrunk by n / (n + 4), so through 1960 the Major gate prices the RIAA
margin on a prior that is 1.5–2.9× low (§43). Options:

1. **Opening experience (warm start).** Opening labels start with k pseudo-LPs at the ratio the sim's own Majors /
   MidTiers / small labels learn by 1962 (about 2.2 / 2–3 / 1). These labels sold LPs for years before 1960, so this is
   memory earned before the run starts. One measured constant per tier, opening labels only; runtime births still
   start at 1.
2. **Earlier provisional observation.** Observe at 13 or 26 weeks, with copies scaled by a measured share of 52-week
   sales. This is earned and needs no tier constant, but it needs per-LP weekly sales telemetry to calibrate (none of the
   current files has it). Q1–Q2 would still be on the raw prior.
3. **Root cause.** The Major prior leaves out what Majors sell beyond it (footprint, push;
   `realization-premium-is-footprint-quality-heat`). Fixing that would bring the learned Major ratio to about 1. This is the
   largest change and the most correct.

**Recommendation:** (1) now, behind a switch, screened at 104 weeks on the Q1–Q5 Major LP count, the 1960 LP market,
and `label_0088`; (3) stays the long-run fix.

## 48. Is the thinner field still historical? And how many Majors did 1960 have? (2026-11-17, read-only)

### Breadth against history

Sim: `records.csv` weekly positions, mean of seeds 2002 / 1001, operating label. History: the Hot 100 counts come from
`SimLogs/historical-hot100-1960-64/hot100_weekly.csv` (distinct artist strings; a duet counts as its own artist, so this
runs slightly high). Top 10 labels come from the operating-label reconstruction (HistoricalLabelTierBalanceResearch.md
§12–13). Top 20 labels in 1960 come from a fan tally of Billboard's 1960 charts (deaconstreet.com "Record Labels 1960"),
which gives 55 in its text and 45 in its heading, so treat it as weak.

| Per year 1960 / 61 / 62 / 63 / 64 | History | `gfo260` (control) | `pkg260` (package) |
|---|---|---|---|
| Labels with a Top 10 record | 49 / 51 / 53 / 54 / 55 | 62 / 53 / 54 / 60 / 57 | 61 / 61 / 49 / 58 / 56 |
| Labels with a Top 20 record | 55 (1960 only) | 94 / 86 / 82 / 90 / 84 | 90 / 85 / 80 / 82 / 80 |
| Distinct Hot 100 artists | 329 / 395 / 384 / 389 / 376 | 382 / 350 / 370 / 373 / 370 | 374 / 353 / 376 / 369 / 352 |
| Distinct Hot 100 records | 683 / 783 / 768 / 754 / 811 | 669 / 630 / 648 / 655 / 660 | 668 / 636 / 641 / 642 / 653 |
| Labels with a Hot 100 record | not found | 222 / 203 / 197 / 199 / 197 | 208 / 198 / 188 / 190 / 180 |

- **Where history exists, the package stays in bounds.** Top 10 labels stay at 49–61, against 49–55, and the package is no
  further off than the control. Hot 100 artists are 352–376, against 329–395. Top 20 labels are already too broad in both
  arms (80–94 against 55), and the package moves them slightly toward history.
- **Hot 100 records are about 15% short after 1960 in both arms.** The package doesn't cause this (it is −0.4% against
  the control); the shortfall is chart turnover.
- **No sourced figure** was found for labels on the Hot 100, labels in business, or acts with any release a year, the
  measures that fall 9% / 17% under the package (§47). The chart-visible field is unchanged. The thinning is below the
  chart: fewer flop-making labels survive to issue records nobody charts.

### Majors in 1960

The trade sense of the 1950s "majors" is six: Columbia, RCA Victor, Decca, Capitol, Mercury and MGM (history-of-rock.com,
"The Major Record Companies"). Some accounts drop MGM and give a Big Five. The film-company labels (ABC-Paramount, Dot,
Warner Bros., United Artists, 20th Fox) were large, corporately owned firms, but not majors in that sense.

The sim opens with the five Big Five templates (`AILabelFactory.majorTemplates`; Decca UK and EMI deferred) plus the
procedural draw `GetRandomTier` (roll < 0.002 → Major, about 1.1 expected from 553 procedural labels). §46's seeds 2002
and 1001 each drew 3, for 8 Majors; seed 3003 drew 0. The procedural Majors' archetypes are SoulFactory ×3,
CountrySpecialist, RockRebel and BluesRoots. **No 1960 firm like that ran at Major scale.** The large specialists of
1960 (Vee-Jay, Chess, Atlantic, King, Starday) were independents without national branch distribution. So "the weakest
of 8 opening Majors" (§47 said 16 by adding both seeds) means a world with 3 Majors too many, and `label_0088` should
not have been a Major at all.

**Proposed:** route the 0.002 Major roll to MidTier, or to Independent for specialist archetypes. One draw is still
consumed, so the RNG stream keeps its shape, but every label after the first former Major reshuffles its later
draws (memory `one-conversion-reshuffles-the-rng-stream`). That leaves a fixed 5 Majors. MGM as a sixth template is grounded
historically, but `head-templates-crowd-the-field` warns that head capacity is paid for by small labels. Screen it on
CR4 / CR8, Top 10 labels and the small-label Hot 100 share.

## 49. `--historical-majors`: the five US majors, 1960 repertoire, no procedural Majors (2026-11-17)

`Systems/HistoricalMajors.cs`, check `--historical-majors-check` (`SimTools/HistoricalMajorsChecks.cs`; passes, as do
`--seed-head-templates-check` and `--midtier-opening-realism-check`). With the switch on:
- **No procedural Majors.** The procedural tier draw's Major band (roll < 0.002) becomes MidTier. The same draw is
  consumed, but the world reshuffles from the first label that drew it.
- **The legacy London templates are dropped**, as `--defer-british-labels` does. Profile runs already drop them (596 labels).
- **MGM stays the studio MidTier** (`--studio-parent-labels`). It was one of the 1950s "six majors" by the trade
  definition of its own plants and distribution, but it sold through independent distributors (FilmBroadcastLabelResearch.md).
  Here the Major tier means branch distribution and Big-5 launch reach.
- **The five templates carry their 1960 repertoire**, TraditionalPop first for all five (`HistoricalMajors.GenresFor`):

| Label | Before | 1960 repertoire (sources: the 1960 Top 10 reconstruction; label histories) |
|---|---|---|
| Columbia | Pop, Jazz, Folk | Pop, EasyListening (Percy Faith, Conniff), Country (Robbins, Horton, Cash), Folk (Brothers Four), Jazz |
| RCA Victor | Pop, Country, Rock | Pop, Country (Reeves, Locklin, the Browns, Cramer), Rock (Elvis), TeenPop (Sedaka, Peterson), EasyListening (Mancini) |
| Capitol | Pop, Jazz, Country | Pop (Sinatra, Cole, Martin), Folk (Kingston Trio), Country (Owens, Husky, Ford), EasyListening (Gleason), Jazz |
| Decca | Pop, Country, Jazz | Pop (Crosby, McGuire Sisters), Country (Bradley's Nashville), TeenPop (Brenda Lee), EasyListening (Kaempfert) |
| Mercury | Pop, R&B, Country | Pop (Patti Page), R&B (Benton, Washington), DooWop (Platters), TeenPop (Johnny Preston), Country (George Jones), Jazz (EmArcy) |

Runs: `hm104` = the `pkg260` request (candidate + `--regional-home-sales`) + `--historical-majors`, 104 weeks, seeds 2002 / 1001;
control `pkg260` cut at week 104 (same source revision; the switch is inert when off). Launcher
`SimTools/run_request_arm.ps1`; script `SimTools/analyze_historical_majors.py pkg260 hm104`.

| Mean of 2002 / 1001 | History | `pkg260` | `hm104` |
|---|---|---|---|
| Opening Majors | 5 (6 with MGM) | 8 | **5** |
| 1960 Big-5 share of Top 10 slots | 34.2% (178 / 520) | 22.8% | **32.3%** |
| 1960 CR4, Top 10 slots, operating label | 34.6% | 25.8% | **33.4%** |
| 1961 Big-5 share of Top 10 slots | ~22% (1961–64 mean 21.6%) | 31.5% | **37.2%** |
| 1961 Major-tier share of Top 10 slots | | 41.8% | 37.2% |
| 1961 CR4 | 27.9% | 33.8% | **40.1%** (37.5 / 42.7) |
| Labels with a Top 10 record 1960 / 1961 | 49 / 51 | 60.5 / 60.5 | 61.0 / 58.5 |
| Labels with a Hot 100 record 1960 / 1961 | | 210.5 / 197.5 | 224.0 / 206.0 |
| Major Hot 100 entries, 1960: Country / Teen+Rock / R&B+DooWop / EasyListening | Top 10 records: 27 / 42 / 12 / 4% | 4 / 32 / 32 / 7% | **11 / 42 / 11 / 13%** |
| LP market 1960 / 1961, stores + racks (M) | | 70.6 / 100.4 | 75.5 / 99.9 |
| Major LPs chosen per Major label, 1960 / 1961 | | 38.2 / 94.3 | 43.9 / 114.1 |
| Labels defunct by week 52 / 104 | | 19.5 / 108.5 | 16.5 / 96.5 |

- **1960 lands on history.** With three fewer Majors, the Big-5 recover their 1960 Top 10 share (32.3% against 34.2%) and CR4
  goes from 25.8 to 33.4% (history 34.6%). The low control values were the LP package's 1960 Major dip (§47) with the
  procedural Majors taking the rest.
- **The Majors' chart repertoire moves toward history.** Before, a third of Major Hot 100 entries were R&B, carried by the
  SoulFactory procedural Majors. Now it is 11–14%, and Country, EasyListening and TraditionalPop rise. Country is still below the
  1960 Top 10 mix (11% of Major Hot 100 entries against 7 of 26 Top 10 records). The Top 10 is a small and different base.
- **1961 is the known Big-5 problem, now unmasked.** The Major-tier share falls (41.8 → 37.2%), but none of it is held by a
  non-template Major any more, so the Big-5's share rises to 37.2% and CR4 to 40.1% (history 27.9%; both seeds above).
  The procedural Majors were diluting the Big-5's post-1960 excess (`big5-collective-share-drives-cr4`: history's majors fell
  from 34% to 19–26% after 1960 as Liberty, Cameo-Parkway, ABC and Vee-Jay took top-4 spots). It does not recreate them.
- **Closures fall** (−12 by week 104), and the 1960 LP market and Major LP count rise. Labels with a hit hold.

**Reading:** the switch is structurally right, and 1960 now reproduces. The cost is that 1961 concentration is out of band.
That is the open Big-5 launch-entry edge (§17k of HistoricalLabelTierBalanceResearch.md), not something this switch should
paper over. Before adoption: a 260-week check (CR4 / Big-5 share 1962–64, closures, and whether the second rank of
national independents forms by earned promotion).

## 50. Where the 1961 Big-5 excess comes from under `--historical-majors` (2026-11-17, read-only)

Runs `hm104-{2002,1001}-sc` (with `pkg260` for contrast). History is the operating-label Top 10 reconstruction
(`SimLogs/historical-1960-top10`, `SimLogs/historical-top10-1961`; HistoricalLabelTierBalanceResearch.md §13). Script:
`SimTools/analyze_big5_1961_excess.py RUN [RUN …]`. A "1960 hit act" is an act with any Top 10 slot in 1960.

### It is not run length any more, and not conversion after entry

- **Weeks per Top 10 record are historical.** Big-5 1961 Top 10 records run 4.70 / 4.85 weeks (history ~4.5), and
  the rest of the field 4.26 / 4.28. §16's run-length explanation no longer applies. The excess is the **count**: the Big-5
  place 40 / 41 records in the 1961 Top 10, against history's 24, out of 118 / 116 in total (history ~115).
- **Hot 100 → Top 10 conversion is close to fair.** For 1961 Hot 100 debuts, 20.0 / 21.0% of Big-5 debuts reach the
  Top 10 against 16.8 / 16.6% for other labels, a ratio of 1.2–1.26 (history 1.00 ± 0.18, §17aa). That is too small to
  carry the gap.
- **Volume goes the other way.** The Big-5 launch 11.0 / 11.6% of 1961 singles (history ~18%) and take 36–38% of Top 10
  slots: about 5x the field's Top 10 weeks per single, against history's ~1.2x. 1960 lands on history only because both
  errors are present: 7.4–8.0% of launches at 5.1–5.6x.

### The excess is new Big-5 hit acts

1961 Top 10 slots (% of 520) by who holds them:

| 1961 | History | 2002 | 1001 | Sim mean | Gap |
|---|---:|---:|---:|---:|---:|
| Big-5, act with a 1960 Top 10 | 16.3 | 17.7 | 24.2 | 21.0 | +4.7 |
| **Big-5, act with no 1960 Top 10** | **4.2** | 18.5 | 14.0 | **16.3** | **+12.0** |
| Other labels, 1960 hit act | 22.9 | 18.7 | 12.7 | 15.7 | −7.2 |
| Other labels, new hit act | 56.5 | 45.2 | 49.0 | 47.1 | −9.4 |
| Big-5 / other 1960 hit acts | 20 / 53 | 24 / 62 | 23 / 69 | | |
| 1961 slot-weeks per 1960 hit act, Big-5 / other | 4.45 / 2.17 | 3.83 / 1.56 | 5.48 / 0.96 | 4.66 / 1.26 | |

- **Three quarters of the +16.7 is new hit acts at the Big-5.** In history, only five Big-5 records in 1961 came from acts
  without a 1960 Top 10: Buzz Clifford, Jimmy Dean, Patsy Cline, Leroy Van Dyke and the Tokens (22 slots, two of them
  veterans crossing over). The sim has 14–16 such acts with 73–96 slots. They are mostly NewSigning-state acts that the Big-5
  signed or first released in 1960 (15.2 / 7.7 points; 2.3 / 5.2 points from 1961 debuts; opening-roster acts ~1). Half of
  them had no 1960 Hot 100 entry at all. History's new hit acts of 1961 were at Liberty, Parkway, Scepter and the other
  national independents (Bobby Vee, Chubby Checker, the Shirelles).
- **The Big-5's carry-over is only slightly high** (4.66 slot-weeks per 1960 hit act against 4.45; seed 2002 is under). Big-5 hit
  acts launch 5.0–5.25 singles in 1961 and put 4.1 records on the Hot 100 each (history 3.28 raw, ~2.6 chart events).
- **The field's 1960 hit acts underproduce** (1.26 slot-weeks against 2.17). They launch 2.1–2.5 singles in 1961
  (history 3.6–4.7), and 56–62% of Independent and 48–54% of MidTier hit acts take a performance departure by week 104,
  against 17–26% at the Big-5. Every one of those departures is from Declining, a median 55–61 weeks after the hit. That is
  just past `HitActCadence.RetentionWeeks` = 52, once flops (peak > 60) have run them down from Rising. The drops follow from low
  follow-up entry; they do not cause it (`followup-gap-is-hot100-entry-via-footprint-gates`).

### Why Big-5 newcomers break through

Newcomer singles (NewSigning / Rising), 1960–61 pooled, Top 10 weeks per launch:

| Quality | Big-5 | MidTier | Independent | Small |
|---|---:|---:|---:|---:|
| < 0.66 | 0.063 | 0.027 | 0.010 | 0.008 |
| 0.66–0.70 | 0.051 | 0.247 | 0.057 | 0.018 |
| 0.70–0.74 | 0.249 | 0.174 | 0.096 | 0.054 |
| 0.74–0.78 | 0.190 | 0.313 | 0.139 | 0.062 |
| 0.78+ | **0.962** | 0.675 | 0.329 | 0.107 |
| All | **0.293** | 0.214 | 0.078 | — |

- **At equal quality, a Big-5 newcomer converts like a MidTier one** (MidTier rates at the Big-5 quality mix: 0.274 against
  0.293). Against an Independent the Big-5 gap is 0.215. About 20% of it is quality: Big-5 newcomers average 0.713
  (production 0.748, hook 0.726) against 0.672 (0.666 / 0.693). Independent rates at the Big-5 mix give 0.121. The other
  ~80% is the head-firm launch package that MidTiers share: campaign impact 0.55 / 0.53 against 0.36, a median first run of
  5.5k / 5.9k against 3.3k, and all seven regions covered by peak (Big-5 newcomer entrants 7.0, Independents 5.4).
  After entry, Big-5 newcomers peak at 26k a week against 14k.
- **Volume turns that edge into share.** The Big-5 launch 211 newcomer singles in 1960 and 266 in 1961 (58–62% of their
  launches), against 211–313 for all MidTiers together. With five head firms and few MidTiers, the head package mostly sits
  on Big-5 newcomers.
- **The genre spread is broad.** Big-5 1961 newcomer Top 10 weeks: Teen/Rock 19, EasyListening 11 (one hit), TradPop 9,
  R&B/DooWop 8.5, Country 5. Repertoire is not the carrier: Big-5 Teen/Rock launches are the same count with and without the
  switch (154 / 162), and the doubled Teen/Rock Top 10 share under `hm104` comes with a higher senior share at the same quality.

### One firm carries it

1961 Top 10 slots per Big-5 firm (2002 / 1001; history): Columbia 12 / 13 (23), **RCA Victor 74 / 111 (44)**, Capitol
0 / 0 (0), Decca 55 / 43 (28), Mercury 47 / 32 (12). Capitol's blank 1961 is historical. Columbia is short; RCA is 1.7–2.5x
history, and Decca and Mercury are about 2–3x. RCA's 1961 slots in seed 1001 come from 13 acts. Most are Rising or
Established youth acts first released in 1960. Under `pkg260`, the leading firm was Columbia (seed 2002) or Mercury (seed
1001), so which firm leads is seed noise. A runaway leader with no national independent answering it is the shape that
recurs. The best non-Big-5 firm holds 19 / 36 slots (history: Liberty 42, Parkway 31).

### Reading

The 1961 excess is a **Big-5 farm system that history did not have**. The majors sign newcomers at volume. At equal quality
each one launches with the same package as a MidTier newcomer, but with ~0.04 more production quality, so they mint
three to four times history's new Big-5 hit acts. History's 1961 new hit acts came from the national independents, which
the sim lacks at head scale (§17k). The field's 1960 hit acts then fade and are dropped about a year later. That is a
second, smaller part (−7 points), and it is the known follow-up entry gap.

Levers tried before (§17n–17o launch saturation, which cut newcomer slots and put 1961 CR4 in band; stock spread, demand
parity, push spread, earned impact) have all acted on this same newcomer package. The adopted caps already equalize Big-5
and MidTier impact (0.55). What remains unscreened is the **allocation**. History's majors put promotion and singles slots
behind roster acts and gave unknowns a release without a campaign. **Proposed probe, not built:** a Major-tier newcomer
package (`--major-newcomer-package=F`) that moves campaign impact and first-run stock for NewSigning launches at Major-tier
labels toward the Independent level, with established acts untouched.

- **Pre-registered for 1961:** Big-5 new-hit-act slots fall toward ~4–8 points, Big-5 share toward 21–28, the 1960 hit-act
  carry-over stays.
- **Kill:** the 1960 Big-5 share falls below ~28 (1960 is on history now; 14 points of it are Big-5 newcomers), or the
  freed slots go to MidTiers rather than spreading over the field.
- **Screen:** `hm104` + switch, 104 weeks, seeds 2002 / 1001, then 3003 / 4004 if the sign holds.

## 51. Screen: `--major-newcomer-package` is inert on newcomer breakthroughs (2026-11-17; rejected)

`Systems/MajorNewcomerPackage.cs`, check `--major-newcomer-package-check` (`SimTools/MajorNewcomerPackageChecks.cs`;
passes, as do the stock-spread and historical-majors checks). For a live AI single by a NewSigning act at a Major-tier
label: launch campaign impact `0.36 + F × (impact − 0.36)` (read by the launch radio push and the launch marketing
awareness), and launch stock scale × `0.60 + 0.40 F`. Coverage, weekly push and every other launch are untouched, and no draw
is added. **Off is identical:** `mnpoff27-2002-sc` matches `hm104-2002-sc` row for row through week 26 (records, launch
attribution, label finance). Arms `mnp0` (F = 0) and `mnp05` (F = 0.5) = the `hm104` request + switch, 104 weeks, seeds 2002 / 1001.

| 2002 / 1001 | `hm104` | `mnp05` | `mnp0` | History |
|---|---:|---:|---:|---:|
| Big-5 newcomer first run, median 1961 | 5.5k / 5.2k | 4.3k / 4.2k | 3.3k / 3.1k | |
| Big-5 newcomer Hot 100 entry 1961 | 7.4 / 10.3% | 12.4 / 15.8% | 12.6 / 10.3% | |
| Big-5 newcomer Top 10 weeks per launch 1961 | .226 / .188 | .253 / .292 | .162 / .269 | |
| Big-5 newcomer entrants: radio heat at entry (2002 / 1001) | .255 / .249 | | .244 / .260 | |
| Big-5, no-1960-Top-10 acts, % of 1961 slots | 18.5 / 14.0 | 19.4 / 31.0 | 10.0 / 20.0 | 4.2 |
| 1960 Big-5 Top 10 share | 35.7 / 28.9 | 33.6 / 24.9 | 24.7 / 31.5 | 34.2 |
| **1961 Big-5 Top 10 share** | **36.2 / 38.3** | **37.1 / 40.2** | **22.1 / 39.6** | ~21 |
| 1961 CR4 | 37.5 / 42.7 | 38.7 / 39.2 | 22.3 / 40.6 | 27.9 |

- **The lever reached its target and the target was not the carrier.** Big-5 newcomers ship 40% less, but their Hot 100
  entry and Top 10 weeks per launch do not fall at either dose; in three of the four seed-arms they rise. Newcomer entrants
  arrive about 6 weeks after release (age 5.5–6.0), when launch radio heat has been replaced by the weekly dynamics: entry
  radio heat is unchanged (.24–.26). Launch awareness falls only .160 → .137 (seed 2002) and .163 → .160 (1001).
- **No dose response.** F = 0.5 raises the 1961 Big-5 share in both seeds. F = 0 cuts seed 2002 by 14 points but raises seed
  1001. The seed 2002 cut sits in *senior* Big-5 slots (Established 56 → 25, Declining 24 → 15), which the switch never touches:
  that is world reshuffle from the first changed launch on. The 1961 Big-5 share moves ±1–14 points per seed on reshuffle alone,
  so a two-seed screen cannot resolve anything under ~10 points here.
- **§50's attribution was wrong.** "About 80% is the launch package" came from Big-5 ≈ MidTier at equal quality, and MidTiers
  share the launch package. They also share national coverage and the weekly regional build, and those are what the
  newcomers' entry six weeks after release depends on. Matched-quality tier comparisons do not identify the launch half.

**Rejected; not added to any bundle.** The switch stays in code for reference, off and identical. The remaining carrier
of Big-5 newcomer breakthroughs is post-launch: the per-week regional factor for covered regions (`GetRegionalLaunchFactor`:
0.55 + 0.45 × reach covered, 0.12 + 0.18 × reach uncovered), together with coverage count (7.0 against 5.4 regions by
peak). That is national branch distribution, which is historically real. The structural reading in §50 stands: history's
1961 new hit acts sat at national independents that the sim does not have at head scale. That points at the second rank
(§17k: earned national access for non-Majors that chart) rather than at a further Major tax.

## 52. Second-rank access: reach is already there, volume is not; earned capacity adds entries, not Top 10 slots (2026-11-17; not adopted)

### Where the second rank stands under `hm104`

The historical second rank is present and already has near-Major reach. MGM, Dot, UA, ABC-Paramount, Warner Bros.,
Liberty, Cameo-Parkway, Chess and Atlantic run as MidTiers covering 6–7 regions, with owned reach ~0.75 by week 52,
launch campaign impact 0.36–0.53 (Cameo-Parkway 0.80–0.98) and label production 0.55–0.90. What they lack is output. They issue
**6–37 singles a year** (MidTier median 16–17) against the Big-5's 56–127. History's studio and head firms issued 65–130 a
year: MGM ~110, ABC ~100, UA ~80, WB ~65, Dot ~130, Liberty ~70 (1961 ~115). Source: LabelReleaseProgrammeImplementation.md,
"Singles issued per label, 1960". The binding constraint is slate capacity: `releasesPerMonth` × work budget is about 2 a
month, and they use 80–100% of it from rosters of 16–30 release-eligible acts. In 1961 only 19–25 MidTiers are alive,
9 of them historical. That is about the size of history's second rank.

Market volume for reference (seeds 2002 / 1001): 4,447 / 4,380 singles in 1960 and 4,099 / 4,040 in 1961 (closures), on 123 → 128M
cleared single units. In 1961 the Big-5 issue 452–468 (history ~1,080) and MidTiers 291–326 (history ~600+ for the studio
and head firms). Independents, Small and Boutique labels issue ~3,300.

### `--earned-head-capacity=R`

`Systems/EarnedHeadCapacity.cs`, check `--earned-head-capacity-check` (`SimTools/EarnedHeadCapacityChecks.cs`, passes). A
non-Major's monthly singles capacity becomes `max(envelope, min(Major envelope, R × Hot 100 records in the last 52 weeks / 12))`.
The count is `GetRecentChartingRecordCount`, the promotion gate's own, cached per label per week. R = 4 follows history's
head independents at about 4–5 singles per Hot 100 record. Capacity is earned, falls back when the records stop, and still
passes through cash, the going-concern reserve, roster and material. Albums, Majors and the player are untouched.
**Off is identical:** `ehcoff27-2002-sc` matches `hm104-2002-sc` row for row through week 26 (records, launches, label
finance, programme resources, release slate). Runs: `ehc4-{2002,1001,3003,4004}-sc` against `hm104` on the same four seeds
(`hm104-3003` / `-4004` are new controls).

| 1961, seeds 2002 / 1001 / 3003 / 4004 | `hm104` | `ehc4` (R = 4) | History |
|---|---|---|---:|
| MidTier singles | 326 / 291 / 236 / 311 | 466 / 520 / 464 / 588 (+60%) | ~600+ (studio + head) |
| MidTier Hot 100 records | 105 / 92 / 73 / 103 | 134 / 173 / 113 / 144 (+50%) | |
| Major + MidTier share of Hot 100 entries | 43.9 mean | 49.5 | |
| MidTier share of Top 10 slots | 19.8 / 20.2 / 13.5 / 21.2 | 15.8 / 33.5 / 10.2 / 20.6 | |
| **Big-5 share of Top 10 slots** | 36.2 / 38.3 / 34.2 / 30.2 (**34.7**) | 37.7 / 27.5 / 37.5 / 29.2 (**33.0**) | 20.6 |
| CR4 | 35.5 mean | 33.6 | 27.9 |
| Non-Big-5 firms with 20+ Top 10 slots | 0 / 2 / 2 / 0 | 1 / 2 / 0 / 1 | 4 (Liberty 42, Parkway 31, Scepter 20, Swan-Cameo group) |
| Labels defunct by week 104 | 96.5 | **105.5** | |
| Labels with a Hot 100 record | 208 | 195 | |
| Single units per release | 31.3k | 29.5k (−6%) | |
| LP market stores + racks (M) | 96.7 | 92.4 | |
| 1960 Big-5 share | 30.3 | 29.5 | 34.2 |

- **The capacity reaches the right firms.** In seed 1001, Cameo-Parkway earns 9.8 singles a month and Chess 8.7. Their launches go
  37 → 97 and 25 → 73, and their Top 10 slots 36 → 41 and 7 → 43. In seed 2002, Dot, MGM and Pioneer double their output.
- **It buys Hot 100 entries, not Top 10 slots.** MidTier Hot 100 records rise in every seed (+50%), but the MidTier Top 10
  share moves only in seed 1001 (+13); the other three fall 0.6–4 points. The Big-5 drop 1.7 points on the mean, with two
  seeds up and two down, inside the ±10-point reshuffle range (§51). The count of second-rank firms with 20+ slots does not move.
- **It costs the field.** Volume is a fixed singles purse (`singles-volume-is-a-fixed-purse`). Units per release fall 6%,
  9 more labels close by week 104, and 13 fewer labels place a Hot 100 record.
- Same shape as `--programme-major-singles=16` for the Majors (`major-share-is-career-mix-adult60-is-volume`). Extra singles
  enter the Hot 100, but the Top 10 is held by hit acts, and volume does not make them.

**Not adopted.** The switch stays in code, off and identical. Second-rank *volume* is now historical-ish under R = 4 and
second-rank *reach* already was, yet the second rank still does not take Top 10 slots. What history's Liberty and Parkway
had that the sim's MidTiers lack is hit acts: Bobby Vee, Chubby Checker, Gene McDaniels, Dee Dee Sharp. The next question is
why a second-rank label's newcomer converts at 0.214 Top 10 weeks per launch against the Big-5's 0.293 (§50 table), and
why its 1960 hit acts carry only 1.26 slot-weeks into 1961 against 4.66. That is the field hit-act follow-up gap again
(§50, `field-hit-act-gap-is-not-selection`).

Four-seed `hm104` control for reference: 1961 Big-5 share 34.7 and CR4 35.5. Seeds 3003 / 4004 (34.2 / 30.2) run below the
first two.

## 53. Why the second rank makes few hit acts (2026-11-17, read-only)

Runs: `hm104-{2002,1001,3003,4004}-sc` (control) and `ehc4-*` (§52). Scripts, run from `SimLogs/`:
`SimTools/analyze_second_rank_funnel.py`, `analyze_second_rank_followups.py` and `analyze_second_rank_matched_entry.py`, each `RUN [RUN …]`.
The tier comes from `single-launch-quality.csv`. Big-5 = label_0001–0005. "Newcomer" = NewSigning, Rising or Unsigned at launch.
"New hit act" = an act whose first in-run Top 10 record was launched as a newcomer.

### Minting: a per-launch tie, so the gap is newcomer volume, and volume does not buy more

1961, four-seed mean per run:

| | Big-5 | MidTier | Independent |
|---|---:|---:|---:|
| Launching firms | 5 | 20.8 | 256 |
| Newcomer launches (per firm) | 287 (57) | 203 (9.8) | 1,662 (6.5) |
| Newcomer Hot 100 entry / Top 10 per launch | .151 / .035 | .122 / .031 | .082 / .015 |
| Newcomer Top 10 weeks per Top 10 record | 5.6 | 3.5 | 4.5 |
| Hit acts minted 1961 (per firm) | 15.2 (3.05) | 9.8 (0.47) | 29.8 (0.12) |
| Entrants: Top 10 per Hot 100 entry, newcomer / established | .231 / .190 | .253 / .211 | .180 / .174 |

- **A MidTier newcomer reaches the Top 10 about as often as a Big-5 one** (.031 against .035 per launch), and a MidTier entrant
  converts to the Top 10 slightly better. Per firm, the Big-5 mint 6.5x as many hit acts because they launch 5.8x as many
  newcomers. Their newcomer Top 10 records also run longer (5.6 weeks against 3.5, on ~25 MidTier records, so treat this as soft).
- **`ehc4` shows that more volume does not add hit acts.** MidTier newcomer launches go 203 → 375, but entry falls .122 → .099 and Top 10 per launch
  falls .031 → .019, so new MidTier hit acts go only 9.8 → 11.5. Established MidTier conversion also falls (Top 10 per entry .211 → .149).
  The extra records enter the Hot 100 and then compete with each other for the same ~115 Top 10 records a year
  (`singles-volume-is-a-fixed-purse`). The second rank cannot mint its way past the Big-5 while the Big-5 newcomer pool is 1.4x
  the whole MidTier pool, which is §50's excess.

### Carry-over: MidTier hit acts follow up at half the Big-5 rate

New hit acts with a first hit by week 78 (52-week window inside 104 weeks). Control, four-seed pool:

| Per new hit act, 52 wk after the hit | Big-5 | MidTier | Independent |
|---|---:|---:|---:|
| Acts per run | 15.8 | 19.8 | 44.0 |
| Quality of the hit record | .768 | .756 | .753 |
| Follow-up singles | 4.56 | 3.00 | 3.09 |
| Hot 100 entries (share) | 2.86 (63%) | 1.28 (43%) | 1.13 (37%) |
| Top 10 records / Top 10 weeks | .54 / 3.13 | .29 / 1.43 | .20 / .93 |
| Follow-up quality (hook / production) | .728 (.760 / .742) | .690 (.709 / .702) | .678 (.695 / .690) |
| Label production at follow-up | .827 | .746 | .728 |
| Professional songs / artist-written | 54 / 16% | 40 / 32% | 44 / 21% |
| Median first run; launch awareness | 37k; .44 | 27k; .50 | 25k; .42 |
| Left the label within 52 wk | 0% | 4% | 5% |

Log split of the 3.13 → 1.43 Top 10 weeks gap: **follow-up count 53%, Hot 100 entry 49%, weeks per Top 10 record 21%**, and
conversion after entry −23% (in the MidTier's favour). Departures, lanes (96–99% OrphanSingle in every tier) and awareness are not carriers.

**Entry, quality-matched** (1961 launches by acts with a prior chart record, four seeds): MidTier .429 against Big-5 .552.
At the Big-5 quality mix the MidTier rate is .483, so **quality is ~45% of the entry gap** (.704 against .727 mean quality) and
**~55% is an equal-quality residual** (8–11 points lower in each bin below .78, equal above it). `he095` (`--label-blind-launch-dose-exposure=0.95`)
and `--label-demand-parity=1` are already in `hm104`. The residual is the post-launch national footprint (6.4–6.5 regions at entry against 7.0, and the
covered-region weekly factor), which §51 named as the Big-5's remaining carrier and which is a historical branch-distribution edge.
For Independents the same split is .270 → .358 (quality ~30%).

The quality gap also has both halves. MidTier hit acts drop .066 from their hit to their follow-ups and Big-5 acts drop .040.
Label production is .746 against .827, and the MidTier relies on artist-written songs (32% against 16%), with a lower hook in every source.

### Reading

A second-rank firm mints hit acts at the Big-5's per-launch rate. It mints fewer because it launches a sixth as many newcomers,
and giving it the volume (`ehc4`) only dilutes its own conversion inside a fixed Top 10. Once a MidTier label has a hit act, it
gets half the Top 10 weeks the Big-5 get, evenly from fewer follow-ups (3.0 against 4.6) and lower entry (43 against 63%). Lower entry is part quality
(label craft + professional song access) and part national footprint. Each piece has already been screened in some form:
cadence (`--hit-act-due-weeks-field=8`, §9 of the follow-up handoff: launches up, entry flat), craft (`--hit-act-craft-floor`: quality
+.02, entry flat), capacity (`ehc4`: hit-act follow-ups 3.49 → 4.41 for all MidTier hit acts, Top 10 .52 → .46), footprint (Major
distribution edge, kept by author choice). No single label-side lever is left for the second rank. History's second-rank
hit acts (Bobby Vee, Chubby Checker) had both cadence and entry, so the untested combination is the joint
package that gives a **MidTier hit act** professional material and cadence together. Whatever the second rank gains still has to come out of a Top 10 that the
Big-5 newcomer pool over-fills (§50: +12 points). A second-rank fix cannot pass the 1961 Big-5 share gate on its own.

**`--earned-head-capacity`:** keep it in code, off and identical, and do not add it to the bundle. It is the only lever that brought
MidTier release volume (and MidTier hit-act cadence) toward history, but in a fixed-purse market it costs 9 closures, 13 charting labels
and 6% of units per release while lowering MidTier conversion. Re-screen it, possibly at R = 2, only as the cadence half of a hit-act package.

## 54. Screen: `--midtier-hit-act-package` raises MidTier follow-ups but not their Top 10 weeks (2026-11-17; not adopted)

### What §53 left, and the material finding

§53 split the MidTier hit-act follow-up gap into cadence and Hot 100 entry, and entry into quality and footprint. Before building,
the song search was traced (`material-funnel.csv`, four `hm104` seeds, acts on a hit rung). MidTier hit acts' searches stop on
**cash 46%** of the time. The Big-5's stop on the two-pending cap (39%) and almost never on cash (1%). Under `--writing-market-royalty-licences`
a professional licence costs 0 and a cover's rehearsal costs 0, so the test `cash − going-concern reserve < fee` reads `cash < reserve`:
a label under its reserve cannot take a free song. Those follow-ups fall back to the act's own song or a cover. Within every route,
Big-5 hit acts' follow-ups still carry a hook 0.035–0.045 higher (act endowment and wider pools: 59 against 23 professional songs in
the act's genre pool), which the package does not touch.

### The switch

`Systems/MidTierHitActPackage.cs`, check `--midtier-hit-act-package-check` (`SimTools/MidTierHitActPackageChecks.cs`; passes, as do the
hit-act cadence, footprint-craft and earned-capacity checks). `--midtier-hit-act-package=PARTS` (`cadence`, `craft`, `material` or `all`)
applies only to a hot act (Top 40 within 52 weeks, the hit-act clock) at a MidTier AI label:

- **cadence:** due 8 weeks after the last single instead of 13, through `HitActCadence.DueWeeksFor` (the shorter field interval is kept if one is set).
- **craft:** the single's label-craft term reads the Major reference 0.85 (`FootprintCraftSoftening.Craft`).
- **material:** a zero-fee licence or cover is not refused for a label sitting under its reserve (`ProfessionalWritingMarket.CanLicense`,
  `TryStartProgrammeMaterial`, the audit mirror). A fee above 0 still needs cash.

No draw is added. **Off is identical:** `mhpoff27-2002-sc` matches `hm104-2002-sc` row for row through week 26 (records, launch
attribution, label finance, programme resources, release slate, material funnel, material search, writing launches).
Arm `mhpa` = the `hm104` request + `--midtier-hit-act-package=all`, 104 weeks, seeds 2002 / 1001 / 3003 / 4004.

### Result

| Four seeds | `hm104` | `mhpa` | History |
|---|---:|---:|---:|
| MidTier hit-rung searches started / stopped on cash | .23 / .46 | .43 / .01 | |
| MidTier hit-act follow-ups: professional catalogue / commission | 27.8 / 7.3% | 35.1 / 9.9% | |
| **MidTier new hit act, 52 wk after the hit:** follow-ups | 3.00 | 3.83 | 3.6–4.7 a year (field) |
| Hot 100 entries (share) | 1.28 (43%) | 1.93 (50%) | |
| Follow-up quality (production / hook) | .690 (.702 / .709) | .706 (.739 / .721) | |
| Top 10 records / **Top 10 weeks** | .29 / **1.43** | .37 / **1.59** | |
| Big-5 new hit act: Top 10 weeks | 3.13 | 1.76 | |
| MidTier new hit acts minted 1961 | 9.8 | 10.2 | |
| MidTier Top 10 slot share 1961 | 19.8 / 20.2 / 13.5 / 21.2 (18.7) | 28.5 / 16.2 / 11.5 / 30.4 (21.7) | |
| Non-Big-5 firms with 20+ Top 10 slots (sum) | 4 | 6 | 4 per year |
| **1961 Big-5 Top 10 share** | 36.2 / 38.3 / 34.2 / 30.2 (**34.7**) | 26.7 / 38.5 / 39.0 / 31.5 (**33.9**) | 20.6 |
| 1961 CR4 | 35.5 | 33.9 | 27.9 |
| 1960 Big-5 share | 30.3 | 27.1 | 34.2 |
| Labels defunct by week 104 / with a Hot 100 record 1961 | 96.5 / 208 | 101.5 / 203 | |

- **Every part reached its target.** Cash blocks vanish, MidTier hit acts cut more professional songs and get the Major craft read,
  and they issue 0.8 more follow-ups. Hot 100 entry per follow-up rises 43 → 50% and Hot 100 entries per hit act rise half.
- **Top 10 weeks barely follow:** +11% per MidTier hit act (1.43 → 1.59). The extra entries peak below the Top 10. Conversion per
  established MidTier entry falls (.211 → .181), the same dilution as `ehc4`.
- **The Big-5 do not give ground outside noise.** 1961 share −0.8 points (seed 2002 −9.5, seed 3003 +4.8). The MidTier Top 10 share is up 3 points
  on the mean, from two seeds (+9) against two (−2 to −4). Big-5 new hit acts' follow-up Top 10 weeks fall 3.13 → 1.76, which the
  package does not touch: that is reshuffle, or crowding of the Top 10, and four seeds cannot tell which.
- Cost: 5 more closures and 5 fewer labels with a Hot 100 record, inside the `ehc4` range.

**Not adopted.** The switch stays in code, off and identical. MidTier hit-act cadence and entry are now closer to history,
but the Top 10 does not follow. After `ehc4` (§52) this is the second second-rank lever that buys Hot 100 entries without Top 10 slots.
Neither lever changes how a Top 10 slot is decided. The Top 10 is about 115 records a year. A second-rank record wins a slot only
by out-selling a Big-5 newcomer at the peak, and §50–51 found that edge in the post-launch national footprint, which is kept by
choice. The 1961 Big-5 excess is not a second-rank deficit that a label-side package can fill. The remaining options are:
(a) accept ~34% as the model's 1961 Big-5 share under kept distribution; or (b) act on the Big-5 newcomer pool itself (287 newcomer singles a year; history's
majors gave unknowns a release without national weekly push), the post-launch half §51 left. The material finding is separate from this verdict:
a zero-fee song refused for want of cash is a gate defect for every non-Major under its reserve (Independents stop on cash 47% of the time).
It is worth its own screen as a general fix, not as part of a tier package.

## 55. Zero-fee material gate; did the Big-5 promote newcomers? Promotion-follows-action is inert (2026-11-17; one seed)

### `--zero-fee-material-gate` (gate fix)

`Systems/ZeroFeeMaterialGate.cs`, check `--zero-fee-material-gate-check` (passes). For every AI label, a song that costs the label nothing
(royalty licence, a cover's rehearsal) clears the material cash test whatever the going-concern reserve. Same three sites as §54's
material part (`ProfessionalWritingMarket.CanLicense`, `TryStartProgrammeMaterial`, the audit mirror). A fee above 0 still needs the cash.
**Off is identical** (`zfgoff27-2002-sc` against `hm104-2002-sc`, eight decision files through week 26; `napoff27-2002-sc` repeats it on the build with §55's second switch).

Seed 2002, 104 weeks (`zfg-2002-sc` against `hm104-2002-sc`), all launches:

| | Search started (hm104 → zfg) | Stopped on cash | Professional + commission songs | Mean record hook |
|---|---|---|---|---|
| Big-5 | .245 → .265 | .012 → 0 | 47.0 → 45.0% | .752 → .752 |
| MidTier | .178 → .437 | .520 → 0 | 29.9 → 38.7% | .709 → .724 |
| Independent | .342 → .544 | .343 → 0 | 32.0 → 36.0% | .695 → .701 |
| Small / Boutique | .483 / .399 → .601 / .556 | .196 / .241 → 0 | ~flat | ~flat |

The defect is fixed where it bit. Labels closed by week 104 rose 95 → 106, but the both-switch arm gives 98, so one seed cannot separate that from reshuffle.
**Status: a defect fix, not a calibration lever. Kept (the user's decision, 2026-11-17): it goes on every arm from here; the four-seed closure and LP check rides on the next multi-seed run.**

### Research: did the Big-5 give newcomers little national promotion?

**Sources (web, 2026-11-17).** No source found states a majors' policy for unknown acts directly. What the trade press and histories show:
- Major-label promotion was regional and staffed: Mercury named regional sales-promotion managers in December 1960 (Billboard 1960-12-05),
  Capitol ran a regional promotion man from its Detroit branch (Tom Takayoshi, early 1960s), and MGM's field men worked single stations
  market by market (Billboard 1965). Billboard's own charts carried a "national breakout" tier and "Regional Breakouts" for records showing
  dealer action in a market before the Hot 100 (Billboard 1960-01-18; 1966-08-20). Promotion followed reported action market by market.
- The majors' A&R heads favoured roster stars and covers. Mitch Miller (Columbia) disliked rock and passed on new rock acts; independents
  broke new acts, largely by DJ contact and payola until the 1959-60 hearings (Gillett, *The Sound of the City*; Time 1958; payola-hearing histories).
- RCA's 1961 year-end statement names its new pop acts (Ann-Margret, Al Hirt, the Tokens and others) as the year's push (Billboard Music Week 1961-12-25).
  Majors did promote chosen newcomers. That was selection, not a campaign behind every newcomer single.

**Repo data.** The Big-5 issued about 1,080 singles in 1960 (Columbia ~360, Mercury ~195, Decca ~185, Capitol ~178, RCA ~160;
LabelReleaseProgrammeImplementation.md), which is 4 a week per firm. Finite promotion staff could not work them all. Yet history shows about
**4.5 Big-5 newcomer Top 10 records a year (1961-64)**, against the sim's ~12 from ~460 singles.

**Climb shape** (`SimTools/analyze_newcomer_climb.py`, Top 10 records; history: labelled Top 10 1961-64 on the recovered Hot 100):

| Top 10 records | History n / debut / weeks to Top 10 | Sim (hm104, 4 seeds, 1960-61) n / debut / weeks |
|---|---|---|
| Big-5 newcomer | 18 / 83 / 4.9 | 96 / 77 / 3.9 |
| Other newcomer | 146 / 85.5 / 5.5 | 420 / 83 / 5.4 |
| Big-5 established | 67 / 73 / 4.4 | 169 / 65 / 5.7 |

History's Big-5 newcomer hits debuted low and climbed at the field's pace (−0.5 weeks against other newcomers, 95% CI −1.75 to +0.75).
The sim's climb 1.5 weeks faster (CI −1.84 to −1.17). History's interval does not rule out the sim's gap.

**Verdict on the assumption: partly confirmed.** Majors released far more newcomer singles than they could promote, and promotion followed
regional action. The strong form, that national promotion played no part for the newcomers that hit, is not shown: the few that did hit
are too few (18) to tell their climb from the field's or from the sim's.

### `--major-newcomer-action-push=F` (promotion follows action)

`Systems/MajorNewcomerActionPush.cs`, check `--major-newcomer-action-push-check` (passes). The weekly label push on a Major's single by an act
NewSigning at launch reads `0.36 + F × (push − 0.36)` until the record shows action (any Hot 100 week or a regional breakout), then full.
§51 cut the launch package; this cuts the weekly push, the half §51 left. Off is identical (`napoff27`). Arms on seed 2002, 104 weeks:
`nap0` (F = 0), `zfgnap0` (gate + F = 0), with `zfg` as the gate-only reference.

| Seed 2002 | `hm104` | `zfg` | `nap0` | `zfgnap0` | History |
|---|---:|---:|---:|---:|---:|
| Big-5 newcomer launches 1961 | 244 | 351 | 318 | 307 | |
| Big-5 newcomer Hot 100 entry / Top 10 per launch | .119 / .033 | .162 / .046 | .126 / .035 | .163 / .039 | |
| Big-5, no-1960-Top-10 acts, % of 1961 slots | 18.5 | 23.5 | 15.2 | 15.8 | 4.2 |
| 1960 Big-5 share | 35.7 | 29.1 | 29.1 | 30.2 | 34.2 |
| **1961 Big-5 share** | **36.2** | **37.9** | **31.3** | **32.9** | 20.6 |
| 1961 CR4 | 37.5 | 33.7 | 28.1 | 30.8 | 27.9 |
| Labels defunct by week 104 | 95 | 106 | 98 | 98 | |

- **The action push does not touch Big-5 newcomer conversion.** Entry .119 → .126 and Top 10 per launch .033 → .035. The newcomers that become hits
  show action at a median of 3 weeks after release (max 6), on stock and national coverage alone, so the cut holds for about three weeks of
  a 0.55 → 0.36 push. Promotion following action is what the sim already does in effect: its distribution makes hits show action early.
- **The share moves are reshuffle.** The gate fix never touches a Big-5 newcomer, yet it moves 1960 Big-5 share −6.6 and the 1961 newcomer slots +5.
  Every arm's 1961 change (−4.9 to +1.7) is inside the ±10-point single-seed range (§51).
- With §51 (launch package) and §54 (second-rank package), this is the third promotion-side lever with no effect on Big-5 newcomers. The carrier is
  the distribution footprint (stock, seven-region coverage, the covered-region launch factor), as §51 concluded. Under that footprint a Big-5
  newcomer shows action before promotion could matter.

**Not adopted; the switch stays in code, off and identical.** The research supports selective promotion, but promotion is not what makes a Big-5 newcomer
hit in the sim. The historical lever left is how widely and deeply a major shipped an unknown's single, which history does not
document (the national-breakout tier implies records broke in one market first, not seven). That would be a launch-coverage probe for Major
newcomers (ship to home and strong regions only until action), and the earlier guards say to check the regional stagger before adopting any footprint change.

## 56. Did a major ship every single nationally, or only its big acts? (2026-11-17, research and history data, no runs)

§55 left one historical lever: ship a Major newcomer's single only to its home and strong regions until it shows action.
Before building it, the question was whether majors gave every single full distribution or only their hits and big acts.

**Breadth (which markets got the record): national, newcomers included.**
- 1960 city surveys (`SimTools/analyze_regional_break_history.py`, §9s; Los Angeles KFWB+KRLA against Minneapolis KDWB): the 20 Big-5
  Top 10 records had **no** city lead of 2+ weeks over the Hot 100 and **no** city gap of 6+ weeks (indie Top 10: 18.5% and 14.6%).
  The newcomers among them reached both cities within about two weeks, mostly *after* their Hot 100 debut: The Brothers Four
  "Greenfields" (Columbia; city lead −3.6 wk, gap 2.1), Jeanne Black "He'll Have to Stay" (Capitol; +0.4, 1.1), Bert Kaempfert
  "Wonderland by Night" (Decca; −0.7, 0.9), Hank Locklin "Please Help Me, I'm Falling" (RCA, a pop newcomer; −4.7, 1.9).
  A home-first launch would create exactly the city leads and gaps that history shows only for indie records.
- Billboard's charts of the period define "Bubbling Under" as records with "considerable regional action" that lack strength
  "thruout the country" (1960-11-14). That is demand showing up market by market, not shipping held back.
- Collector and trade recollections (soul-source.co.uk threads on 1960s US distribution and on rare major-label 45s) agree that
  majors serviced DJ copies nationally by station format, and that Columbia and RCA were "much more liberal with their promos".

**Depth (how many copies stood in shops): thin for unknowns, set by orders.**
- Stock followed demand: stations reported plays, and stock was "pushed to stores through the distributor network" where a city
  showed demand; flops often had more DJ copies than store stock, some only DJ copies (the same threads; anecdotal, no counts).
- Big acts sold on a different scale: Billboard (1961-03-20) reports every Presley RCA single since 1955 sold a million, and
  Herman's Hermits had 600,000 advance orders (MGM, 1966). A small label's known act moved 3,500 singles as an initial order
  (Limelight, Billboard 1965-12-11).
- RCA cut its 1960 singles release to 2-3 a week, "fewer, more intensively promoted", and led the industry in singles sales
  (Billboard Music Week 1962-01-13). The repo's ~160 RCA singles for 1960 agrees.

**What this means for the sim.**
- The home-first launch lever is **not built**. History says Big-5 records, newcomers included, went out nationally. A held
  launch would fail the stagger guard (Big-5 lead ≥ 2 and gap ≥ 6 both 0 in 1960). §51's conclusion stands: national branch
  coverage is historically real.
- The historical difference for unknowns is depth: national promo copies and thin, order-led store stock. §51 already cut a Big-5
  newcomer's first run 40% (median 5.5k → 3.3k) with no effect on entry or Top 10 weeks. The sim's newcomers enter around week 6,
  after reorders replace the launch stock, so first-run depth does not bind.
- So distribution is not what makes the sim's Big-5 newcomers hit 2-3x too often (§55: ~12 newcomer Top 10 records a year
  against history's ~4.5). The candidates left are what each launch carries: quality and slate selection (MajorShareRootCauseHandoff.md, 2026-11-13
  decomposition: quality mix ~35% of the Big-5 realization premium) and the number of newcomer singles the Big-5 issue.

## 57. Big-5 single volume: share of issues, the field trim, the gate exemption and opening rosters (2026-11-17; four seeds; not adopted yet)

**The gap.** The five majors issued about 1,080 singles in 1960 (LabelReleaseProgrammeImplementation.md), 16-19% of Billboard's
5,800-6,700. The sim's Big-5 issue 313 (1960) and 543 (1961), 7-13% of ~4,200. Field labels issue fewer singles than history in
absolute terms; their share is too large only because the Big-5's is too small. The sim's total is ~2/3 of history's and singles
demand is a fixed purse, so the target (the user's call) is history's **share**, ~18%, about 750 Big-5 singles at the sim's
scale, taken from Independent, Small and Boutique output. MidTiers, already below history, are held.

**Knobs.** `--programme-major-singles=N` (cap raised 16 → 30 in `LabelReleaseProgramme.cs`) and the non-Major work budget
`--slate-work-budget=S` with `--slate-work-budget-midtier=1`. Two new switches in `Systems/MajorVolumeSwitches.cs`, check
`--major-volume-check` (passes): `--major-gate-exemption` (a Major's single skips the single capital-risk test, and its project
review records the act's own session instead of holding for stronger material; it still waits for a song in delivery and never
records after the window closes) and `--major-opening-roster=N` (a Major opens with at least N acts; the fill draw is still
consumed). **Off is identical:** `mvoff27-2002-sc` matches `zfg-2002-sc` row for row through week 26 (records, launches, label
finance, release slate, programme choices). Scorer `SimTools/analyze_major_volume.py`. All arms carry `--zero-fee-material-gate`.

**One-seed calibration (seed 2002).** Majors at 16 and 24 a month issue 662 and 681 singles in 1961: above ~16 a month the extra
slots expire. Field trim 0.85 removes ~380-440 Independent and Small/Boutique singles a year; units per single +6-10%, closures flat.

**Why Major slots expire.** At 16 a month the extra cancelled Big-5 slots were held, until their window closed, by the
**single capital-risk** test (6 → 97 on seed 2002, 19 → 204 on 1001) and **hold-for-stronger-material** (46 → 118, 37 → 185).
On seed 1001 Columbia's 1961 singles fell 109 → 38 with $5M in cash. History's majors issued exactly the marginal singles these
gates refuse: most singles of the period lost money.

**Rosters.** Majors open with `maxRosterSize × fill` (30-60 × 0.6-0.85), about 39 acts a week considered for a slot in 1960 H1,
growing to 58-67 by 1961 H2 under `--earned-roster-capacity`. History has no direct roster count; from 1,080 singles at ~2-4.6
singles per act (repo discography sample), ~350-450 releasing acts, ~70-90 per major, ~55-60 at the sim's scale. The sim is short
in 1960 and on target by late 1961.

**Screen** (104 weeks; arms on the `zfg` reference; Big-5 = label_0001-0005):

| Seeds 2002 / 1001 / 3003 (4004) | `zfg` | `mj16wb85` (16 + field 0.85) | `mvg` (+ exemption) | `mvx` (+ exemption + roster 58) | History |
|---|---|---|---|---|---:|
| 1961 Big-5 singles | 543 / 534 / 491 (431) | 641 / 536 / 587 (729) | 672 / 588 / 717 | 729 / 701 / 803 (653) | |
| 1961 Big-5 share of issues | 12.9 / 13.0 / 11.7 (10.5) | 16.6 / 14.3 / 14.3 (18.0) | 16.8 / 15.1 / 17.2 | 18.7 / 18.0 / 19.7 (16.8) | 16-19 |
| 1960 Big-5 share of issues | 7.1 / 8.1 / 7.9 (5.8) | 9.7 / 10.1 / 8.3 (9.1) | 11.4 / 11.3 / 10.2 | 11.7 / 13.2 / 13.7 (12.3) | 16-19 |
| **1961 Big-5 Top 10 share** | 37.9 / 33.8 / 29.8 (26.2) | 29.2 / 40.0 / 26.0 (34.6) | 38.7 / 38.1 / 42.7 | **46.5 / 45.6 / 54.2 (35.4)** | 20.6 |
| **1960 Big-5 Top 10 share** | 29.1 / 27.9 / 33.6 (16.6) | 31.1 / 23.6 / 24.0 (23.2) | 37.9 / 17.0 / 25.7 | **39.6 / 45.7 / 52.5 (28.1)** | 34.2 |
| 1961 CR4 | 33.7 / 37.5 / 27.9 (27.1) | 29.2 / 41.0 / 34.2 (31.0) | 38.3 / 45.6 / 39.6 | 43.7 / 45.6 / 54.2 (37.9) | 27.9 |
| 1961 Big-5 newcomer Top 10 records | 8 / 9 / 7 (5) | 8 / 14 / 8 (5) | 7 / 11 / 14 | 7 / 16 / 12 (9) | ~4.5 |
| 1961 single units per single issued (k) | 30.3 / 31.1 / 30.5 | 33.1 / 34.3 / 31.1 | 32.0 / 32.9 / 30.6 | 32.8 / 32.8 / 31.4 | |
| Labels defunct by week 104 | 106 / 101 / 84 (94) | 106 / 95 / 82 (97) | 97 / 100 / 75 | 102 / 94 / 86 (94) | |

- **Volume reaches history.** The exemption makes the Big-5 issue 590-720 singles in 1961 on every seed, 15-17% of issues; with
  the opening roster, 18-20%. 1960 stays at 10-14%.
- **Extra slots alone leave chart share flat** (`mj16wb85` 31.7 against 33.8, mean of three seeds), as §52 and the earlier
  `--programme-major-singles=16` found.
- **The exemption adds about 6 points of 1961 Big-5 Top 10 share** (33.8 → 39.8; +0.8 / +4.3 / +12.9 by seed), none in 1960.
- **The opening roster adds 19 points in 1960 and 9 in 1961, on every seed** (4 of 4 above reference in both years). The extra
  opening acts are newcomers: opening Big-5 rosters 149-196 → 290 acts, Stars and Established nearly unchanged (seed 2002: 7 → 8
  Stars; 3003: 11 → 15). Most 1960 Big-5 Top 10 slots come from opening acts (205 of 210 on seed 2002). Each added newcomer launches with
  the Big-5's per-release advantage, so moving acts from field labels to Majors converts them at Big-5 rates. On seed 2002 the
  roster also drained the launch pool for MidTiers (1961 MidTier singles 298 → 171).
- **Reading.** Volume that repeats the same acts buys entries, not Top 10 slots; volume that adds *distinct* acts multiplies the
  per-launch Big-5 premium (2-4x the field per launch, against history's ~1.2x; `big5-premium-constant-1961-is-volume`). History's
  Big-5 issued far more and charted at only ~1.2x per release, so the sim's volume is not wrong: the premium is. Raising volume
  before the premium comes down moves the Big-5 further from history.

**Status: not adopted (the user decides); both switches stay in code, off and identical, with the cap raised to 30.** My recommendation: reject `--major-opening-roster`
while the per-launch premium stands. The volume package (`--programme-major-singles=16`, field trim 0.85, `--major-gate-exemption`)
is historically right on output but costs ~6 points of 1961 Big-5 share; it should be paired with a per-launch premium fix
(quality mix and slate selection, footprint; MajorShareRootCauseHandoff.md 2026-11-13) and screened again.

## 58. Where Big-5 newcomers' stronger hooks come from (2026-11-17; `zfg` x4, no new runs)

**Question.** The connected-newcomer trace put ~35% of the Big-5 newcomer Hot 100 premium on record quality, mostly hook
(.74 vs .69). Do the Big-5 get better songs or better writers? If so, would a writing-talent bank that labels spend be the lever?
Scorer `SimTools/analyze_newcomer_hook_source.py RUN...`. Newcomer (NewSigning / Unsigned) singles launched to week 92:
1,204 Big-5, 23,492 field, seeds 2002 / 1001 / 3003 / 4004 of `zfg`.

**How a hook is made.** Record hook = `PolarSongBehavior.RealizeHook`: lerp(performance, composition hook, 0.9), a 15% ceiling
toward the composition hook, times execution. The record's hook is ~90% the song's (`commercialHook`). Performance (act
songwriting, act quality, studio) matters only for artist-written songs, through `ArtistWrittenHookScale` (0.85 writer craft +
0.15 performance).

**The gap.** Record hook .730 vs .698 (+.032); composition hook .755 vs .726 (+.029; by seed +.036 / +.028 / +.025 / +.029);
realization (record − composition) adds only +.003. So the edge is which songs get cut.

| Split of the +.029 composition-hook gap | Size | How measured |
|---|---:|---|
| Source × genre mix (Big-5 do more TeenPop / Country / TradPop) | +.005 | shift-share over source × genre cells |
| Act writer craft (Majors sign better self-writers) | ~+.007 | OLS with cell FE: Big-5 +.025 → +.018 with act craft and songwriting |
| Path mix (Big-5 use the material search less: 23% vs 34% of launches) | ~−.004 | Big-5 +.018 → +.022 with path; label cap of 2 pending searches blocks 64% of unstarted Big-5 searches (field 1%) |
| Label-side picking: deeper shortlists, sharper reads | ~+.020 | remainder; explained below |

- **Within every source, Big-5 newcomers cut better songs** (pooled composition hook, Big-5 vs field): professional .775 vs .755,
  standards .804 vs .741, own songs .717 vs .680, recent-hit covers .744 vs .733. Source mix explains +.002.
- **Own songs are all act craft:** Big-5 newcomer writer craft .726 vs .685; with craft in the regression the Big-5 term is −.002
  (se .002).
- **Professional songs and standards are label-side:** Big-5 +.028 (se .006) and +.051 (se .011) after act controls.
- **Not writers or offices.** Within professional songs (+.020): publisher-office mix +.002, within the same office +.018; writer
  quartile mix +.008, within the same writer quartile +.012. Writer recognition .002 for both groups. The R&R hit office
  (`--rock-hit-office-placement=2`, pitched only to Majors and recent Top 40 labels) is 5.5% of Big-5 newcomer professional songs vs
  6.1% of the field's. The Big-5 draw better songs from the same writers' catalogues and from the same (non-exclusive) standards.

**The picking mechanism, by path** (`release-judgements-shadow.csv` outcome; OLS with source × genre × seed FE):

| Path | Share Big-5 / field | Hook Big-5 / field | Big-5 term → with controls |
|---|---|---|---|
| Shortlist pick (`pool`: best-read recordable song) | 65 / 60% | .726 / .706 | +.014 → +.009 (act craft) → +.003 (+ depth) → −.005 (+ read band) |
| Material search (`material-search`) | 23 / 34% | .817 / .760 | +.052 → +.049 (act craft) → +.021 (+ baseline read) |

- **Shortlist depth.** Big-5 newcomers hold 2.39 recordable songs at release vs 1.93. `AiRepertoireService.ArrivalRate` brings
  professional songs at base × pro share × (0.5 + 0.63 × scouting). At equal depth (0-2 songs) Big-5 and field hooks match
  (.666/.663, .707/.698, .717/.714).
- **Read accuracy.** Shortlist read band .099 vs .118 (`ReadBand` = lerp(.20, .08, scouting)); demo reads .18 − .12 × scouting
  (`ProfessionalWritingMarket.DemoRead`); material-search reads lerp(.20, .08, scouting). Founding scouting envelopes: Major
  .68-.96, MidTier .54-.90, Independent .44-.91, Boutique .48-.93, Small .34-.84.
- **Search bar.** A search starts only for a song that reads above the baseline (best-read shortlist song): Big-5 .715 vs .618.
  A deeper shortlist raises the bar, so the songs searched for are stronger. Baseline absorbs 60% of the search-path edge, read
  band most of the rest.

**What the hook gap is worth.** Logit over newcomer launches (genre × seed FE, hook, production, Big-5 dummy): at field hooks,
Big-5 newcomer Top 10 per launch falls ×0.77 / 0.83 / 0.84 / 0.81 by seed (pooled premium 2.86 → 2.32), Hot 100 entry ×0.85-0.90
(1.89 → 1.66). That is ~20% of the Top 10 premium in log terms, an upper bound because it is cross-sectional. The label-side picking
part (~.020 of .029) is worth ~12%. The production gap is larger (.747 vs .656) and the same logit gives field hook + production
1.29x, but `--major-newcomer-craft=0.65` cut Big-5 newcomer production .06-.07 causally and moved the premium within seed noise
(candidate 2 diagnostic), so production's cross-sectional weight is the label's footprint, not its studio.

**Reading.**
- The Big-5 do not have disproportionately better writers. Their hook edge is ordinary A&R: they sign better self-writers,
  get more professional songs pitched, read songs more accurately and set a higher bar. That is historical (staff A&R
  producers, publishers pitching majors first) and worth at most ~12% of the premium.
- **A writing-talent bank is not a premium lever.** Rationing writer access acts on a channel worth ~.003 of hook. Charging for
  each pitched song would thin Big-5 shortlists (arrivals are free under royalty licences), but the Majors are the labels best
  able to pay, and the whole picking channel is ~12%. Not built.
- With the slate-pool cap rejected, the newcomer-craft cap inside seed noise (both in `Big5VolumeAndPremiumHandoff.md`) and hook
  ≤20%, record quality is not where the ~3x per-launch premium lives. What remains is launch footprint (covered regions ~45% of connected Hot 100 entry odds) and campaign
  impact (~20%): candidate 3, footprint jointly, which the author holds as the last resort.

## 59. Major launch depth: the footprint ablation scoped to Majors (2026-11-17; stage 1 of candidate 3; four seeds)

**Switch.** `--label-blind-launch-scope=major|major-newcomer` (`Systems/LabelBlindLaunch.cs`) restricts the existing `foot`
ablation to live singles at Major-tier labels (`major`), or to those whose act is NewSigning / Unsigned at launch
(`major-newcomer`). A Major already covers every region, holds none and reaches every city, so for a Major `foot` lowers depth
only: the regional launch factor (strong 1.35 / covered ~.95 → .72), the fit-exposure launch chance (~.95 → .57, label home no
longer free) and launch stock (reference reach). National breadth is unchanged. The scope needs foot parts and refuses promo.
Check cases added to `--label-blind-launch-check` (passes; so do `--major-volume-check` and `--footprint-craft-softening-check`).
**Off is identical:** `lbsoff27-2002-sc` matches `zfg-2002-sc` row for row through week 26 (records, launch attribution, label
finance, release slate, programme choices, material funnel, release judgements, writing launches).

**Arms** (reference `zfg-{seed}-sc-request.json`, 104 weeks, seeds 2002 / 1001 / 3003 / 4004, with `--newcomer-connect-trace`):
`mnfoot-*` = `--label-blind-launch=foot --label-blind-launch-scope=major-newcomer`; `mjfoot-*` = scope `major` (the bound).
Scorers: `SimTools/analyze_footprint_scope.py SEEDS FMT...` (pooled premiums, shares, finance), `analyze_newcomer_exposure.py`
(connected newcomers at launch), `analyze_regional_stagger.py SEEDS FMT` (now takes a run format).

| Pooled 1960-61 launches to week 92 | `zfg` | `mnfoot` | `mjfoot` | History |
|---|---:|---:|---:|---:|
| Newcomer Top 10 premium (Big-5 / field) | 2.86 | **1.84** | 1.85 | ~1.2 |
| Newcomer Hot 100 premium | 1.89 | 1.66 | 1.61 | |
| Big-5 newcomer Top 10 per launch % | 3.82 | 2.54 | 2.55 | |
| Established Top 10 premium | 1.86 | 1.65 | 1.39 | |
| Established Hot 100 premium | 1.50 | 1.54 | 1.38 | |

Per-seed newcomer Top 10 premium: `zfg` 4.0 / 2.8 / 3.0 / 1.4 → `mnfoot` 2.5 / 1.4 / 1.3 / 2.4 (down 3 of 4; 4004 rises).

| Per-seed mean (seeds below `zfg`) | `zfg` | `mnfoot` | `mjfoot` | History |
|---|---:|---:|---:|---:|
| 1960 Big-5 Top 10 share | 26.8 | 23.3 (3/4) | 20.6 (3/4) | 34.2 |
| 1961 Big-5 Top 10 share | 31.9 | 27.2 (3/4) | 26.6 (4/4) | 20.6 |
| 1961 Big-5 Top 40 / Hot 100 share | 28.1 / 27.9 | 26.0 / 27.6 | 23.9 / 24.9 | |
| 1961 CR4 | 31.5 | 28.1 (4/4) | 27.5 (4/4) | 27.9 |
| Big-5 newcomer Top 10 records 1960 / 1961 | 4.5 / 7.2 | 3.0 / 6.2 | 2.5 / 6.2 | ~4.5 |
| 1961 Big-5 gross / net ($M) | 244 / 104 | 248 / 107 | 234 / 99 (4/4 down) | |
| Labels defunct by week 104 | 96.2 | 96.0 | 101.5 | |
| 1960 Big-5 Top 10 city lead ≥ 2 / gap ≥ 6 (%) | 9.4 / 3.8 | 9.1 / 6.9 | **25.8 / 15.3** | 0 / 0 |
| 1960 indie Top 10 city lead ≥ 2 / gap ≥ 6 (%) | 26.9 / 24.7 | 24.5 / 20.3 | 24.2 / 18.6 | 18.5 / 14.6 |

**Mechanism** (connected newcomers, seeds 2002 / 3003; control from `nctrace-*`): Big-5 fit-exposed regions at launch 6.4-6.6 →
4.5 (field 4.1); launch stock 5.4k → 3.3k (field 2.9k); regional awareness at week 1 .155 → .11 (field .09); weeks 1-3 cleared
units 361 / 494 → 273 / 259 (field ~255). Hot 100 entry .213 / .199 → .216 / .143; Top 10 .086 / .056 → .063 / .034.

**Reading.**
- **Go on stage 1.** Thin launch depth for Major newcomers cuts their Top 10 premium by 42% in log terms (2.86 → 1.84), the first
  lever in this chase to clear seed noise on the per-launch premium. It keeps national breadth by construction and passes the
  Big-5 stagger guard on the lead (9.4 → 9.1); the 6-week gap rises 3.8 → 6.9 (about two records of ~75).
- **Established acts must keep their depth.** `mjfoot` adds no newcomer effect over `mnfoot` and takes the established premium
  to 1.39, but it triples the Big-5 city lead (25.8%, history 0) and costs the Big-5 ~4% gross. That is §56's history from the
  other side: the majors' established records broke everywhere at once; thin depth was for unknowns only.
- 1960 Big-5 share falls further below history (26.8 → 23.3). That is the 1960 volume gap (Big-5 issue 6-8% of 1960 singles
  against ~18%), which stage 4's volume package is meant to fill.
- Not yet known: which read carries it (exposure vs launch factor; stock is inert by §51). The ablation sets the reference
  everywhere; a period form (a keyed share of regions at reference) is still to build.

**Status:** stage 1 done; the switch stays in code, off and identical. Next: stage 2 (scoped `expose` and `factor` arms, then a
dose), per `Big5VolumeAndPremiumHandoff.md`.

## 60. LP chart against Billboard under frd60pk, and the opening-label warm start (2026-11-18; four seeds; not adopted)

### Where the LP chart stands (frd60pk, read-only)

`analyze_album_chart_health.py` and `analyze_album_head_gap.py` on `frd60pk-{1001..4004}` (both read `.csv`; the `.gz`
files need a `read_csv` shim). The chart has moved since §25:

| 1960 / 1961 | frd60pk | Billboard | §25 (k4t20) |
|---|---|---|---|
| Distinct No. 1 titles | 5.8 / 7.5 | 10 / 12 | 8-9 / 13-14.5 |
| Longest No. 1 run | 21 / 20 (seeds to 29-32) | 13 / 10+ | |
| Top-10 titles there by chart week 2 | 61 / 42% | rare | |
| Debuts inside the top 10 a year | 11 / 9 | rare | |
| Median debut, % of chart depth | 82 | 70-99 | 73-80 |
| Slots held by 40+ week titles | 19 / 14% | 38% (Jan 1960) | |
| Year-end top-50 gap (7 rows) | 38 / 37 | 0 | 24 / 26 |
| Store LPs (M) | 33.1 / 52.6 | – / 68 | 42-46 / 67-69 (gfo260) |

Head, mean of four seeds (% of the top 50, 1960 / 1961; Billboard in brackets): Soundtrack 19 / 14 (16 / 24), TradPop
24 / 26.5 (28 / 22), Folk 5.5 / 10.5 (14 / 10), Youth 17.5 / 13.5 (14 / 16), Comedy 4 / 5 (12 / 12), Easy Listening 14.5 / 21
(6 / 10), Country 0 / 0.5 (2 / 2); Jazz, Classical, Blues, Soul and others 15 / 9 (6 / 4). Soundtracks hold 2 of 48 No. 1s.

**Where store volume went** (seed 2002 for gfo260, which lacks seed 1001's market file; 1961 store / rack, M): gfo260
67.6 / 38.3, `lrs260` (σ 1.5) 63.6 / 42.7, `pkg260` (+ price 0.50 + earned log) 57.7 / 43.5, `hm104` 54.0 / 43.7, frd60pk
49.6-55.4 / 40.6-43.9. Each LP package step and the historical majors take a share; racks gain ~5M. Major LPs a quarter
under frd60pk run ~40% of gfo260 from Q1 to Q5 and ~60% after (fewer Majors). The five Majors learn 3.1-4.4 by the end of
1961 (pooled 2.3-3.6), against 2.0-2.2 for pkg260's procedural Majors; MidTiers 1.8-5.1. In 1960 every tier's first
observations are early-retired flops (pooled 0.01-0.2), so a label's first learned ratio is the 0.25 clamp.

### `--earned-lp-forecast-warm-start=K`

§47 option 1, `Systems/EarnedLpForecast.cs`. Before an opening (LaunchPopulation) Major or MidTier label's first gate use or
observation, its log memory is seeded with K pseudo-LPs whose moments give the tier ratio
(`--earned-lp-forecast-warm-ratios=Major,MidTier`, default 3.5, 2.5) and a log variance (`--earned-lp-forecast-warm-variance`,
default 3.5). Runtime births and small tiers start empty. Log mode only; no draws; off is an early return. Checks in
`LabelPressingEconomicsChecks` (five new). Arm `ews20` = frd60pk + K 20 (first-LP multiplier 3.5^(20/24) = 2.84).

| Mean of 4 seeds | frd60pk | ews20 | History |
|---|---|---|---|
| Major LPs, Q1-Q5 (sum) | 254 | 400 | |
| Major LPs, Q6-Q8 (sum) | 479 | 486 | |
| Store LPs 1960 / 1961 (M) | 33.1 / 52.6 | **38.5 / 56.1** | – / 68 |
| Head gap 1960 / 1961 | 38 / 37 | 31 / 36 | |
| No. 1 titles; longest run | 5.8 / 7.5; 21 / 20 | 6.5 / 6.8; 23 / 21.5 | 10 / 12; 13 / 10+ |
| Opening labels closed by wk 104 | 107.8 | **96.2** | neutral 89-96 |
| **Big-5 Top 10 share 1961** | 21.9 | **29.7** (26.3-33.5) | 20.6 |
| Big-5 Top 10 share 1960 | 22.1 | 23.9 | 27.0 |
| Big-5 Top 10 records 1961 | 26.5 | 36.0 | |
| of which LP promo singles | 0.2 | 1.8 | |
| Big-5 single issues 1961 | 422 | 458 | |

The chart-bottom metrics (events, life, faller exit, proven/other climb) are unchanged.

- **Volume:** the warm start restores the Q1-Q5 Major LP count (~1.6x) and 1960 store LPs by 5.4M, but 1961 by only 3.5M:
  after Q5 Major counts match the control, so the rest of the 1961 gap is the smaller Major field and the lower margin,
  not the memory.
- **It breaks the 1961 Big-5 singles share** on every seed (+8, history 20.6). The extra Big-5 Top 10 records are not LP
  promo singles (+1.6 of +9.5). They are Established (+4.7), NewSigning (+3) and Star (+2) acts; Big-5 single issues
  rise 8.5%. **The adopted frd60pk 1961 Big-5 share rests partly on the Majors being LP-starved through Q5.** The channel
  (LP-fed follower books and recognition, or Big-5 cash into promotion and singles) is not yet measured.
- LP chart shape is unchanged: the No. 1 stickiness is not a volume effect.

**Status:** not adopted; the switch stays in code, off. Next: find the channel from LP volume to Big-5 single hits before
any LP volume fix; any fix that restores Major LPs will meet the same guard.

## 61. How Major LP volume reaches Big-5 single hits: album chart runs climb the singles career ladder (2026-11-18; four seeds)

§60's warm start (`ews20`) raised the 1961 Big-5 Top 10 share 21.9 → 29.7%. Traced on `frd60pk` vs `ews20`, seeds 1001-4004.

### Ruled out

| 1961 Big-5 singles | frd60pk | ews20 |
|---|---|---|
| Top 10 records / of them LP promo singles | 26.5 / 0.2 | 36.0 / 1.8 |
| Single issues; Top 40 per issue; Top 10 per issue | 440; 11.1%; 4.8% | 478; 11.2%; 6.3% |
| Hit-act plugs (`score_plug_slots.py`), Top 10 rate | 153; 17.6% | 156; 21.6% |
| Big-5 cash wk 52 / wk 104 ($M); marketing per single | 32.6 / 43.0; $284 | 34.3 / 43.7; $282 |
| Mean follower book on a launch | 13.6k | 14.0k |

Top 10 per issue rises in every follower-book bucket (no book 1.3 → 1.9%, 50k+ 19.6 → 25.0%) and as much for acts with no
album heat (Top 10 / Top 40 0.42 → 0.52) as with it. So it is not promo singles, plug capacity, cash, follower books or album
heat on singles.

### The channel

- **Act-level, paired.** Opening acts keep their IDs across arms. The ~136 Big-5 acts a seed that got more 1960 LPs under
  `ews20` carry the excess: 1961 Top 10 records 7.75 → 16.0 (+8.25 of +9.5); acts with unchanged LP counts −0.25. Their
  1961 Big-5 singles 116 → 176 and LPs 165 → 265; drops are rare in both arms.
- **They climb.** In 1960 these acts earn more album Top 40-equivalent runs (10.5 → 17; under `--album-scale-career-milestones`
  that is the top 40% of the 40-slot album chart, #16) and more single Top 40s (18 → 26). Their 1961 release decisions are
  Established 33 → 65, Rising 33 → 66, Star 0.5 → 11.
- **Climbing pays three ways.** Renewal: Big-5 acts with a 1960 LP renew at 49-57% at expiry, without one 3-8%
  (`ShouldResignArtist` reads career state and contract chart records). Volume: the single prior's career band lifts the
  projected single net (median $1.8k → $2.0k), so the label makes 441 release decisions a seed for these acts against 281.
  Launch: their Top 40 singles launch with ~50% more stock and higher recognition.

### Probe: `--album-career-credit-off`

`Systems/AlbumCareerCreditProbe.cs`, read in `ArtistManager.OnRecordChartUpdated` / `OnRecordLeftChart`: an LP's chart run
credits nothing to the act (entry/Top 40/Top 10/#1 latches, run completion, recognition gains). Singles and the other
album-chart readers (label hit credit, landmark, acclaim, genre-market run log) are unchanged. 2×2, four seeds:

| Mean of 4 seeds | frd60pk | ews20 | acc0 (probe) | ews20acc0 (probe + K 20) |
|---|---|---|---|---|
| Big-5 T10 share 1961 (hist 20.6) | 21.9 | 29.7 | 24.5 | 25.6 |
| by seed | 19.4 / 28.1 / 21.7 / 18.3 | 26.3 / 33.5 / 32.5 / 26.3 | 24.8 / 19.0 / 22.3 / 31.9 | 26.5 / 29.2 / 22.7 / 23.8 |
| Big-5 T10 share 1960 (hist 27.0) | 22.1 | 23.9 | 25.5 | 25.3 |
| Big-5 T10 records 1961 | 26.5 | 36.0 | 27.5 | 27.8 |
| Big-5 LPs 1960 | 197 | 362 | 200 | 363 |
| Big-5 singles 1961 (Rising / Established / Star) | 422 (47 / 64 / 17) | 458 (60 / 70 / 21) | 398 (31 / 40 / 15) | 390 (32 / 45 / 13) |
| Store LPs 1960 / 1961 (M) | 33.1 / 52.6 | 38.4 / 56.1 | 36.3 / 53.0 | 37.1 / 55.7 |

- **Confirmed.** With the credit on, the warm start adds +7.8 points (paired by seed +6.9 / +5.4 / +10.8 / +8.0); with it
  off, +1.1 (+1.7 / +10.2 / +0.4 / −8.1, noise). About 90% of the excess runs through album runs crediting the act's career.
- **The LP volume survives without it:** store LPs 1961 55.7 under `ews20acc0`.
- **The probe moves the base too:** with no album credit, Big-5 share rises 1960 22.1 → 25.5 and 1961 21.9 → 24.5 (one seed
  at 31.9); every label's album acts lose rungs, and the field (more LP-reliant late in 1961?) loses more. Not measured here.
- **Reading.** The adopted 1961 Big-5 fit leans on the Big-5 being LP-starved in 1960: with fewer LPs, fewer Big-5 acts
  climb through album runs. Any LP-volume fix will lift Big-5 singles until the ladder reads album runs differently.

**Status:** probe only, off by default and not for adoption (removing album credit entirely is not a period rule: an album
#1 was a star-making event, AlbumScaleCareerMilestones). Open design question for the author: how much an album run should
count on the singles ladder (e.g. album Top 10-equivalents only, album credit to renewal but not to the single prior's
career band, or the album chart's real 1961 depth of 150 mono for the percentile scale). Not yet run.

## 62. `--album-career-importance`: album runs weigh less in an act's career early in the decade (2026-11-18; four seeds, 104 weeks)

The author's rule (2026-11-18): the album chart must score differently from the Hot 100 because (1) its size changed over
the decade and (2) its weight in stardom grew; a 1960 #1 LP meant much less than a 1969 one. (1) is
`--album-scale-career-milestones` (percentile of that week's album chart size, already in the candidate). (2) is new.

**Rule** (`Systems/AlbumScaleCareerMilestones.cs`). The act's career reads an album's singles equivalent e as
ceil(e / w(year)) while that is still a Top 40 hit, otherwise max(e, 41), so losing the hit to the weight costs at most the
mildest non-hit bucket. Weighted readers: the milestone latches (`ArtistManager.OnRecordChartUpdated`), run completion
(`ArtistManager.OnRecordLeftChart`, `RosterManager` `UpdateAfterChartRun`) and the recognition run credit (peak, plus the
sustain/breadth gain × w). Label hit credit, landmark, acclaim and evolution keep e. Keyframes `YEAR:W`, linear, flat
outside; default = the author's LP unit-share target over 1969's (0.55 / 0.64 / 0.73 / 0.91 / 1.0 in 1960/63/65/67/69).
At 0.55 an LP needs #8 of 40 for a Top 40 credit and an album #1 reads #2 (no #1 toward Star). Checks in
`AlbumScaleCareerMilestonesChecks`.

Arms on the §60 warm start (frd60pk + `--earned-lp-forecast-warm-start=20`): `aciu` (default schedule) and `acis`
(`=1960:0.3,1964:0.5,1967:0.85,1969:1`; 0.3 means #4 of 40 for a Top 40 credit and an album #1 reads #4).

| Mean of 4 seeds | frd60pk | ews20 | aciu | acis | History |
|---|---|---|---|---|---|
| Big-5 T10 share 1960 | 22.1 | 23.9 | 25.7 | **27.5** | 27.0 |
| Big-5 T10 share 1961 | 21.9 | 29.7 | 28.1 | **24.6** | 20.6 |
| by seed (1001/2002/3003/4004) | 19.4 / 28.1 / 21.7 / 18.3 | 26.3 / 33.5 / 32.5 / 26.3 | 29.6 / 21.9 / 26.2 / 34.8 | 31.2 / 16.7 / 25.4 / 25.0 | |
| Big-5 T10 records 1961 | 26.5 | 36.0 | 29.8 | 27.2 | |
| Big-5 1961 singles (Rising / Established / Star) | 422 (47 / 64 / 17) | 458 (60 / 70 / 21) | 404 (51 / 56 / 12) | 431 (48 / 51 / 10) | |
| Big-5 LPs 1960 | 197 | 362 | 362 | 365 | |
| Store LPs 1960 / 1961 (M) | 33.1 / 52.6 | 38.4 / 56.1 | 35.7 / 57.3 | 36.8 / 55.0 | – / 68 |
| Opening labels closed by wk 104 | 107.8 | 96.2 | 98.2 | 99.2 | neutral 89-96 |

Chart structure (`score_chart_faller_depth.py`: events, life, faller exit, proven/other climb, #10/#100) and the LP chart
(No. 1s 7.2 / 6.8, head gap 38 / 37 for `acis`) are unchanged in every arm.

- **The default schedule barely binds in 1960-61** (28.1 vs 29.7): w 0.55-0.58 still lets a #8 of 40 LP make an act.
- **The steep schedule takes most of the warm start's excess out** (29.7 → 24.6, 4 points over history) and puts 1960 on
  history (27.5 vs 27.0, the open 1960 dip): album credit had been helping field acts climb in 1960 more than Big-5 acts
  (the §61 probe showed the same, 22.1 → 25.5). It keeps most of the LP volume (+3.7M 1960, +2.4M 1961 over frd60pk) and
  the lower closures.
- **Seed spread is wide** (acis 1961 16.7-31.2; paired against ews20 +4.9 / −16.8 / −7.1 / −1.3). The 4-point residual
  is inside it.
- Big-5 Star decisions fall below the control (17 → 10): fewer 1960 Stars made by album #1s. No period count to check.
- **Untested:** the late-decade end of the schedule. At 0.5-0.7 in 1964-66, album-led acts may climb too slowly; that needs
  a decade A/B (memory: decade A/B at audit end).

**Status:** not adopted. `acis` is the candidate shape for 1960-61.

## 63. `acis` on six seeds: the 1961 residual was seed noise (2026-11-18; 104 weeks)

§62 left `acis` (warm start K 20 + `--album-career-importance=1960:0.3,1964:0.5,1967:0.85,1969:1`) 4 points over the 1961
history on four seeds. Seeds 5005 and 6006 added to both `frd60pk` and `acis` (same requests, `run_request_arm.ps1`).

**Why not steepen the schedule first.** In §61 the probe that removes album credit entirely (`ews20acc0`) left 1961 at
25.6; `acis` was already at 24.6. The importance weight had nothing left to take out. And the four-seed residual was run
length, not records: `acis` Big-5 Top 10 records 27.25 (control 26.5), weeks per record 4.71 (control 4.30; field 4.4;
history 1961 4.46 Big-5, ~4.5 field).

| Mean of 6 seeds | frd60pk | acis | History |
|---|---|---|---|
| Big-5 T10 share 1960 | 23.0 | **25.0** | 27.0 |
| Big-5 T10 share 1961 | 23.0 | **22.2** | 20.6 |
| 1961 by seed (1001-6006) | 19.4 / 28.1 / 21.7 / 18.3 / 24.4 / 26.3 | 31.2 / 16.7 / 25.4 / 25.0 / 17.1 / 17.9 | |
| Big-5 T10 records 1961; weeks each | 26.2; 4.59 | 25.3; 4.56 | ~24; 4.46 |
| Field weeks per T10 record 1961 | 4.41 | 4.36 | ~4.5 |
| Store LPs 1960 / 1961 (M) | 33.5 / 51.6 | **37.7 / 54.9** | – / 68 |
| Opening labels closed by wk 104 (bankrupt) | 104.7 (71.8) | **98.8 (66.2)** | neutral 89-96 |
| Big-5 share of single units | 19.7 | 18.8 | |

Chart structure (`score_chart_faller_depth.py`, six seeds) is unchanged: events 657 / 653, life 7.2, last position 47 / 49,
faller exit 62 / 62%, T40 % proven 39.6 / 40.3, T10 % proven 22.5 / 20.4, #10/#100 19.0 / 19.3.

- **1961 is on the control's fit.** Paired by seed `acis` − `frd60pk` = +11.8 / −11.4 / +3.7 / +6.7 / −7.3 / −8.4, mean
  −0.8. The four-seed +2.7 and the Big-5 run-length premium (4.71) were seeds 1001-4004; on six seeds weeks per record match.
- **1960 moves 2 points toward the adopted 27% guard**, and the warm start keeps +4.2 / +3.3M store LPs and 6 fewer closures.
- Further tuning of the 1960-61 weights is below the noise: the per-seed paired SD is ~9 points (SE ~3.7 on six seeds).

**Status:** `acis` is the 1960-61 candidate: it passes every 104-week guard and improves LP volume and closures. Not
adopted: the schedule's 1964-69 end is still untested (1960-64 runs `acis260-{1001..4004}` in progress, §64).

## 64. Why the 1960-61 LP chart is out of shape (2026-11-18; read-only, `acis` six seeds and earlier arms)

`analyze_album_chart_health.py` (now reads `.csv.gz`) down the adoption sequence, seeds 1001/2002, 1960 / 1961:

| Arm | No. 1s | Longest No. 1 | #1/#10 units | Top-10 by chart wk 2 | Top-10 debuts | 40+ wk slots | ST % of top-10 titles |
|---|---|---|---|---|---|---|---|
| `gfo260` (before the LP package) | 10 / 11 | 16 / 12 | 2.4 / 2.8 | 66 / 72% | 21.5 / 20.5 | 14.5 / 12.5 | 15 / 10 |
| `lrs10` (σ 1.0) | 9.5 / 9.5 | 9.5 / 13 | 2.9 / 2.6 | 65 / 48% | 15 / 11.5 | 14.5 / 10.5 | |
| `lrs260` (σ 1.5) | 4.5 / 5 | 22 / 27.5 | 7.0 / 4.8 | 51 / 27% | 10 / 4.5 | 18 / 12 | 12 / 2 |
| `pkg260`, `hm104`, `frd60pk` | 5-6 / 3-9 | 18-31 / 20-25 | 4-10 | 57-64 / 44-48% | 10-13 / 9-12 | 16-19 / 13-14 | 12-14 / 2-5 |
| `acis` (six seeds) | 6.7 / 6.8 | 21.5 / 18.7 | 5.1 / 4.1 | 56 / 34% | 10 / 6.2 | 16.7 / 14.3 | 11 / 3 |
| Billboard | 10 / 12 | 13 / 10+ | | rare | rare | 38% (Jan 1960, 65 listings) | |

### 1. Too few No. 1s, reigns too long: the reception draw's upper tail

`--lp-reception-sigma=1.5` (§41, adopted for the flop share) is a mean-preserving lognormal on store conversion:
one LP in 44 sells 6.5× the mean every week, one in 740 29×. σ alone moves the No. 1 count from 9.5 to 4.5-5 and the
longest reign from 10-13 to 22-28 (table). The long `acis` reigns are these titles: *Monster Dracula* (Comedy, 35 weeks
at No. 1, 5.0M copies in two years), *Nicky and the Martians* (31 weeks, 3.0M), and 1.0-2.0M TradPop/Blues/compilation
titles at 20-25 weeks. In reigns of 8+ weeks No. 1 outsells No. 2 by 1.45× (1.16× in short reigns).

The RIAA survey supports a thinner top (`analyze_lp_riaa_1965.py`, 1960 releases to week 104, store + rack, seeds
1001-3003): 300-600k 1.2-2.0% (RIAA 0.7), 600k-1M 0.4-0.6% (0.1), 1M+ 0.1-0.4% (0.4); overall log-sd 2.0 (RIAA ~2.0),
<4k matches (§41). The bottom fits; the 300k-1M band is 2-4× RIAA. One σ cannot fit both ends. *Correction (§66): the
300k+ bands do not move when the draw's top is cut; they are set by act, label and racks, so RIAA does not discriminate
here. The case for a thinner top rests on the weekly chart.*

### 2. Soundtracks fade in 1961: the same tail, through the hit-tail bar

Year-end 1961 top 50, seeds 1001/2002 (Billboard 24 = 10 pre-1960 / 10 from 1960 / 4 from 1961):

| Arm | 1961 ST points | pre-1960 (opening) | 1960 releases | 1961 releases | new ST reaching top 10 / 20, 1960 / 1961 |
|---|---|---|---|---|---|
| `gfo260` | 20 | 7 | 9 | 4 | 3 / 5; 1.5 / 4.5 |
| `pkg260` | 15 | 8 | 3 | 4 | 0 / 3; 0 / 1.5 |
| `acis` | 15 | 10 | 3 | 2 | 0.5 / 4.5; 0 / 1 |

Soundtracks charting is not short (median 103 chart weeks, 16.5 in the top 10, six seeds). They carry no roster act, so
they get no reception draw (factor 1): new soundtracks still peak at 5-8k copies a week, but σ 1.5 raised the #10 bar
from 5-7k (`gfo260`) to 8-11k and the #20 bar by 1-2.5k. They miss the top 20, `--soundtrack-hit-tail-peak=20` never
fires, and 1960 releases carry 3 points into 1961 instead of 9-10. The opening catalogue is on target (10 vs 10).

### 3. Too few long-runners: hits decay like flops

Titles 40+ weeks on the chart hold 14-17% of slots, and **96% of those slot-weeks are soundtracks**; non-soundtrack LPs
hold 0-1.2%. Billboard's Essential Inventory of 4 April 1960 (top 25 of titles 40+ weeks old, §13) had 7 cast/film
albums; the other 18 were non-soundtrack long-runners (not classified here). Normal LPs decay by a fixed `0.93^(weeks − 8)`
(`AlbumSimulator.cs:26`, `LpDemandProbe.LegacyDecay`; D7's 12-18 week median life target), whatever their peak. Top-10
LPs follow it: units 0.74 of peak at chart week 3, 0.32 at week 10, 0.11 at week 20; median chart life 20 weeks
(Standard), 18 (Compilation). Only soundtracks have a success-dependent tail (box office + hit tail). The 38% figure
is for 65 listings (Action 40 + EI 25); a like-for-like target is ~18 evergreen titles in the top 65, which the model's
40-slot 1960 chart cannot show directly.

### 4. Fast top-10 entries: launch-loaded demand, not σ

Top-10 titles reaching the top 10 by chart week 2 were 66-72% before σ (`gfo260`) and 34-56% after. By format (`acis`):
Compilation 71%, TeenPop 64%, Classical 54%, Soundtrack 55%, Standard 36%. Hit packages and youth LPs spend their
follower books in three weeks (memory: youth LP runs are the follower window), and the 0.22 blend still lets a launch
of 4.5× the #10's level into the top 10 in week 1. This one fades as the chart deepens (`pkg260` 1962-64: 17-28%).

### Reading

Items 1 and 2 are one cause: the reception draw's upper tail, adopted for the flop share. An asymmetric draw (σ 1.5
below the median, ~1.0 above, renormalized to mean 1 while keeping the <4k share) would test both at once. Item 3 needs a
success-dependent catalogue tail for non-soundtrack LPs (the soundtrack hit tail generalized, e.g. by peak), which runs
against D7's 12-18 week median unless it binds only on hits. Item 4 is the follower window and is minor once the chart
deepens. None built yet.

## 65. `acis` 1960-64: the LP chart and the Big-5 share past 1961 (2026-11-18; four seeds, 260 weeks)

Runs `acis260-{1001..4004}` (the §62-63 candidate at 260 weeks; `run_request_arm.ps1 -Weeks 260`). No `frd60pk` 260-week
control exists, so 1962-64 is scored against history only.

### LP chart shape (`analyze_album_chart_health.py`)

| Mean of 4 seeds | 1960 | 1961 | 1962 | 1963 | 1964 | Billboard |
|---|---|---|---|---|---|---|
| Chart size (model) | 40 | 50 | 50 | 150 (Aug) | 150 | mono 150 from ~Apr 1961 |
| Distinct No. 1s | 7.2 | 6.8 | 10.8 | 7.8 | 8.8 | 10 / 12 (1960/61), 9 (1965) |
| Longest No. 1 run | 19.2 | 20.2 | 11.5 | 17.0 | 17.0 | 13 / 10+ / 13 (1965) |
| #1 / #10 units | 4.7 | 4.2 | 3.0 | 4.2 | 3.2 | |
| Top-10 titles there by chart week 2 | 55% | 35% | 25% | 23% | 27% | rare |
| Slots held by 40+ week titles | 17.5 | 14.5 | 8.8 | 4.8 | 6.5 | 38% (Jan 1960, 65 listings) |
| Soundtrack share of top-10 titles | 11.2 | 3.2 | 0.5 | 1.0 | 1.2 | |
| Median debut, % of depth | 84 | 83 | 84 | 74 | 60 | 70-99 (1962) |

- **No. 1s recover in 1962** (10.8, longest 11.5), but 1963-64 reigns run 17 weeks again. The §64 reading holds: the
  reception tail dominates most where volume is low and the chart short.
- **Long-runners get worse as the chart deepens:** 4.8-8.8% of slots from 1962. Billboard's 150-deep mono chart carried
  multi-year titles (memory: blockbuster soundtrack longevity, *West Side Story* 341 weeks). §64's flat 0.93 decay is the
  cause for normal LPs, and soundtracks no longer reach the top.
- **Launch spikes fade** to 23-27% from 1962, and debuts sit lower as the chart deepens (60% of depth in 1964).

### Year-end top 50 by genre (Billboard 1960-63, memory: Billboard 1961-63 album genre targets)

| % of top 50, mean of 4 seeds (by seed) | 1962 | Billboard | 1963 | Billboard |
|---|---|---|---|---|
| Soundtrack/cast | **7** (8/4/6/10) | 22 | 13 (12/14/10/16) | 18 |
| TradPop | 26 (34/30/20/20) | 18 | **17** (24/18/12/14) | 28 |
| Youth + R&B | **18.5** (20/18/18/18) | 30 | 22 (20/28/20/20) | 20 |
| Folk (+ ContemporaryFolk) | 10 + 2 | 14 | 9.5 + 6 | 16 |
| Comedy | 5.5 | 4 | **4** | 10 |
| Easy Listening | 13.5 | 8 | **11.5** | 2 |
| Jazz / Blues / Gospel / Classical / Childrens / Latin | 4 / 3.5 / 0.5 / 2.5 / 2 / 0.5 | Jazz 4 | 4 / 4 / 2 / 2 / 1 / 0 | 0 |
| BossaNova | 1.5 | 0 | 3 | 4 |
| Gap (7 rows) | 48 (≈46 with ContemporaryFolk as Folk) | | 41 (≈35) | |

1960 / 1961 gaps are 38 / 37 (§60's 38 / 37 on `frd60pk`).
- **Soundtracks are the largest row in 1962** (7 vs 22, the *West Side Story* year). This is §64's mechanism: no
  reception draw, below the hit-tail bar, so no carry-over.
- **Youth 1962** (18.5 vs 30) is the twist LP year (memory: half the youth titles were hit collections, most of the
  rest 1962 twist LPs).
- **Blues, Gospel, Childrens and Latin** hold 5.5-9 points every year (1960-64) where Billboard has about 0 (Ray Charles counted as
  R&B). Easy Listening is over in every year (means 15 / 19.5 / 13.5 / 11.5 in 1960-63 vs 6 / 10 / 8 / 2).
- Folk is near target (12 vs 14, 15.5 vs 16) once ContemporaryFolk (PP&M, Baez in Billboard's classification) is counted as Folk.
- TradPop swings by seed (12-34), so its 1962/1963 misses go opposite ways and need more seeds before a lever.

### Big-5 Top 10 share past 1961 (`records.csv`; history from the weekly Top 10 records files)

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| `acis` share (by seed 1001/2002/3003/4004) | 27.6 | 24.6 | 24.8 (26.2/26.3/26.5/20.2) | 28.2 (42.3/31.5/26.2/12.7) | 25.9 (28.3/29.4/24.2/21.5) |
| History | 34.2 (guard 27) | 20.6 | 21.2 | 26.0 | 18.7 |
| `acis` Big-5 Top 10 records; weeks each | 30.8; 4.81 | 27.3; 4.71 | 32.8; 4.00 | 33.8; 4.28 | 32.0; 4.23 |
| History records; weeks each | 28; 6.36 | 24; 4.46 | 24; 4.58 | 32; 4.22 | 19; 5.11 |

- 1962-64 run +3.6 / +2.2 / +7.2 over history, all in **record count** (33 vs 24 / 32 / 19); weeks per record are at or
  under history. Seed spread is wide (1963: 12.7-42.3), and four-seed 1961 read 24.6 where six seeds read 22.2 (§63).
- Without a 260-week control it is unknown whether `acis` causes any of this (album weights 0.35-0.5 in 1961-64) or it was
  already in `frd60pk`. 1964 history (19 records, the British Invasion year) is the lowest of the five.

### LP volume

Store LPs 36.8 / 55.0 / 57.3 / 53.7 / 49.5M fall after 1962 while racks rise 31.9 → 50.3M; store + rack is flat at
~100M in 1962-64. The same store decline is in `gfo260` (67.4 → 57.4) and `pkg260` (62.0 → 54.0): a channel shift that
predates the LP package, not new here. Clubs and budget lines are not counted (memory: LP units need all four channels).

**Status:** the 1960-61 shape problems persist through 1964 except No. 1 count (1962) and launch spikes (fade with depth).
Soundtracks (1962 worst) and long-runners (worse with depth) are the largest misses; both trace to §64. Owed: a
`frd60pk` 260-week control to attribute the 1962-64 Big-5 excess.

## 66. Screen: `--lp-reception-upper=K:U`, a thinner top on the reception draw (2026-11-18; seeds 1001/2002, 104 weeks)

**Switch** (`Systems/GameyAuditSwitches.cs`, `LpReception`). Above z = K the log multiplier rises at slope U instead of
σ: σz up to the knee, σK + U(z − K) beyond, divided by its own mean (quadrature at configure), so the mean stays 1.
Requires `--lp-reception-sigma`; off never read and the symmetric formula unchanged. Checks in `GameyAuditSwitchChecks`
(mean 1 on 200k keyed draws, slopes below/above the knee, symmetric unchanged) pass. Because a mean-1 lognormal takes most
of its mean from its top, cutting the top raises the median: σ 1.5 knee 0 / U 1.0 has median 0.63 (symmetric 0.32) and
27% of draws under 0.25× (43%); σ 2.0 knee 1 / U 0.5 restores 42.5% under 0.25× with the top 0.13% at 7.3× (29×).

Arms on `acis` (seeds 1001/2002 only, per the author; σ 2.0 via reference copy `ref-acis-sigma20-request.json`, since the
first copy of a flag wins): `rup0u10` (σ 1.5, 0:1.0), `rup1u05` (σ 1.5, 1:0.5), `rs20u05` (σ 2.0, 1:0.5).

| Mean of seeds 1001/2002 | `acis` | `rup0u10` | `rup1u05` | `rs20u05` | Target |
|---|---|---|---|---|---|
| No. 1s 1960 / 1961 | 7 / 7 | 9.5 / 11 | 10 / 17 | **10 / 12.5** | 10 / 12 |
| Longest No. 1 run | 22 / 22.5 | 20 / 11 | 16.5 / 7.5 | **16.5 / 11** | 13 / 10+ |
| #1 / #10 units | 4.8 / 4.7 | 3.0 / 3.0 | 2.6 / 2.6 | 2.6 / 2.9 | |
| Top-10 by chart wk 2, 1960 / 1961 | 59 / 38% | 63 / 41% | 57 / 45% | 51 / 40% | rare |
| 40+ wk slots 1960 / 1961 | 18.5 / 16 | 18 / 14 | 18.5 / 16 | 19 / 15.5 | 38% |
| ST in 1961 top 50 (1960 releases) | 15 (3) | 15 (5) | 19 (7) | 17 (5) | 24 (10) |
| New ST reaching top 10, 1960 | 0.5 | 2.5 | 2.0 | 1.5 | 1-2 |
| Head gap 1960 / 1961 | 36 / 39 | 35 / 46 | 43 / 44 | 49 / 43 | 0 |
| LPs <4k (1960 releases, store + rack) | 43% | 32.5% | 34% | **39.5%** | RIAA 49% |
| Losing LPs | 43.6% | 32.3% | 34.3% | **41.2%** | RIAA 61% |
| 300k+ / 1M+ (store + rack) | 2.45 / 0.15% | 2.4 / 0.1% | | 2.6 / 0.1% | 1.2 / 0.4% |
| Store LPs 1960 / 1961 (M) | 35.2 / 53.4 | 37.9 / 59.7 | 37.4 / 58.3 | 36.5 / 57.0 | – / 68 |
| Bankruptcies (opening labels) | 69 | 61 | 66.5 | 72 | |
| Big-5 T10 1961 (1001 / 2002) | 23.9 (31.2 / 16.7) | 28.1 (32.3 / 23.8) | 29.0 (36.3 / 21.7) | **23.2 (24.0 / 22.3)** | 20.6 |
| Big-5 T10 1960 | 28.8 | 24.9 | 26.5 | 25.4 | 27 |
| Big-5 LPs charting 1960; top 4 (1001 / 2002) | 72 / 72; 18 / 17 | 73 / 91; 16 / 25 | 76 / 87; 20 / 23 | 77 / 76; 22 / 27 | |

- **No. 1s and reigns are the draw's top.** Every arm cuts #1/#10 from 4.7 to 2.6-3.0; `rs20u05` puts 1961 on Billboard
  (12.5 No. 1s, longest 11) and 1960 at 10 No. 1s; 1960's longest reign is still 16.5 (13).
- **The median rise is the cost.** With σ 1.5 the bottom thins (<4k 43 → 33%, losing 44 → 33%), store LPs gain 5-6M and
  more Big-5 LPs chart (seed 2002 72 → 87-91), which lifts the 1961 Big-5 singles share on both seeds (+1 to +7; §61's
  album-career channel). σ 2.0 below the knee holds the bottom (39.5% <4k, losing 41%), the Big-5 share (23.2) and
  bankruptcies (+3), with +1.3 / +3.6M store LPs.
- **Soundtracks improve only a little** (1961 15 → 17-19, 1960 releases carrying 5-7 against 10); new 1961 soundtracks
  still miss the top 20 (0.5-2.5). The #10 bar falls but not to their 5-8k peaks: the carry-over needs its own lever
  (hit-tail bar, or a draw for soundtracks).
- **The 300k+ bands do not move** in any arm (§64's RIAA reading corrected above), and long-runners and launch spikes are
  unchanged, as expected.
- **Head gap is worse on `rs20u05` in 1960** (49 vs 36): TradPop 37 vs 28 (acis 28) and Easy Listening 18 vs 15 on both
  seeds; Youth 10 vs 19. Two seeds swing rows ±5-10, so this is a flag, not a finding.

**Status:** `rs20u05` (`--lp-reception-sigma=2.0 --lp-reception-upper=1:0.5`) is the candidate shape: it fixes the
1961 No. 1 count and reign length and holds flops, closures and the Big-5 guard on two seeds. Not adopted; more seeds,
the 1960 TradPop flag and 1962-64 wait on the Big-5 attribution (§65).

## 67. Soundtracks and the 300k+ / 1M+ LPs (2026-11-18; read-only on `rs20u05`, seeds 1001/2002)

Trace runs `rs20tr-{1001,2002}` = `rs20u05` + `--album-demand-trace --album-demand-trace-weeks=104` (new: trace rows to
release week N; new columns `reception`, `stSource`, `stBoxOffice`, `stBlockbuster`, `stRenewal`; `ChartAuditRunner.cs`).
Inert: album chart and records byte-identical to `rs20u05`.

### Scorer fix: the RIAA cohort had no soundtracks

`analyze_lp_riaa_1965.py` inner-joined the census on album projects, and soundtracks are minted outside the project
pipeline, so **every soundtrack was dropped** from §41-46 and §64-66 RIAA bands. Fixed: soundtracks (no project, no
roster act) join with their census launch week, opening catalogue excluded; `--no-soundtracks` reproduces the old cohort
(checked: `acis` 1001 300k+ 2.0%, <4k 44%), and the compact output adds `1M+ %` and `soundtracks: n / 300k+ / 1M+`.

| 1960 releases to week 104, store + rack | `rs20u05` 1001 / 2002 | `acis` 1001 / 2002 | RIAA 1963 |
|---|---|---|---|
| All LPs 300k+ | 3.2 / 3.7% | 2.9 / 3.3% | 1.2% |
| All LPs 1M+ | 0.66 / 0.55% | 0.72 / 0.63% | 0.44% (7 of 1,603) |
| Artist LPs 300k+ / 1M+ | 2.3 / 2.9%; 0.06 / 0.13% | 2.0 / 2.8%; 0.06 / 0.19% | |
| New soundtracks: n / 300k+ / 1M+ | 31 / 15 / 10; 28 / 14 / 7 | 24 / 16 / 11; 24 / 9 / 7 | |

**Nothing stops 300k+ LPs**: the model has 2.5-3× RIAA's share. The composition is off: artist LPs crowd 300k-1M and
almost never pass 1M (1-3 a seed), while a third of new soundtracks pass 1M within two years. (RIAA counts manufacturer
sales including clubs; the model's title counts exclude clubs and budget editions, so its totals are if anything low.)

### Why artist LPs stop near 1M: genre pool exhaustion plus the 0.93 decay

Top 20 artist LPs (0.73-1.11M store + rack; TradPop / Easy Listening Majors, reception 1.7-6.7), median by release week:

| Week | 2 | 4 | 8 | 12 | 16 | 20 | 26 | 40 |
|---|---|---|---|---|---|---|---|---|
| Store / rack units (k) | 32 / 31 | 21 / 22 | 18 / 18 | 12 / 14 | 8 / 11 | 4.7 / 8.0 | 2.9 / 7.6 | 1.7 / 0 |
| Penetration (units ÷ genre pool ~470k) | 0.24 | 0.43 | 0.82 | 1.13 | 1.32 | 1.43 | 1.58 | 1.78 |
| Exhaustion `max(0.15, 1/(1+4p))` | 0.52 | 0.37 | 0.23 | 0.18 | 0.16 | 0.15 | 0.15 | 0.15 |
| Catalogue decay (0.93 from wk 8) | 1 | 1 | 1 | 0.75 | 0.56 | 0.42 | 0.27 | 0.10 |

A title's pool is its genre's national LP buyer pool (~460-500k for TradPop and Easy Listening), counted separately per
title; store, rack (`--rack-shared-exhaustion`) and budget units all count against it. Exhaustion reaches its floor by
week 16-20 and the time decay finishes the run, so a hit's lifetime is about 1.8× its genre pool: 0.7-1.1M. The same two
terms are §64's missing long-runners. Only soundtracks escape them.

### Why soundtracks sell millions but chart low

Top 8 new soundtracks, median by release week: penetration stays 0.31-0.37 from week 8 to 78 while they sell 4.4-7.4k
store + 9-19k rack a week. `--soundtrack-hit-tail=0.15` (peak top 20) divides penetration by 1 + 0.15 × (weeks − 12)
(×10.9 by week 78), and box-office decay is 0.70-0.84 at weeks 12-40. So a top-20 soundtrack never exhausts and sells
for years, two-thirds through racks (rack/store 1.88 vs 1.08 for artist LPs, at `--rack-soundtrack-weight=0.3`).

Its store side is weak: blended awareness 0.24-0.42 vs ~0.6 for top artist LPs (premiere buzz × 0.25,
`--soundtrack-launch-awareness`), no reception draw, and rack units spend its store buyers. **The album chart counts
store units only** (`AlbumSimulator.CalculateChartPoints`; `RackJobberChannel`: rack units "never enter chart points").
Billboard counted racks from 4 January 1960, announcing the "inclusion of rack sales data in the total chart
computations" in place of its separate rack charts (the issue that split off Essential Inventory; `SimLogs/album-promo-research/
billboard-1960-01-04.txt`). Budget ("Low Price") and classical LPs got their own charts.

Offline re-rank of the traced titles by store + rack units (unblended weekly ranks, so only differences count):
soundtrack share of the weekly top 10 rises 18 → 27% (1960) and 3.8 → 14% (1961); top-40 share is unchanged (15-17%);
1961 year-end top-50 soundtrack points 17 → 19 (Billboard 24); the share of slots held by titles 26+ weeks old is
unchanged (~15%). Racks in the chart lift soundtracks into the top 10 but do not make long-runners.

### Reading

- The 1961 soundtrack gap is store visibility, not sales: the model's soundtracks outsell history, through racks the
  chart ignores. Counting racks is sourced and moves the top 10; the remaining year-end gap needs store awareness.
- A third of new soundtracks passing 1M in two years is too strong per title: the hit-tail renewal is uncapped while
  racks carry the volume. Counting racks in the chart would also let the tail fire more often (it keys on chart peak).
- Artist LPs need what soundtracks have: a hit's audience reaching past its genre pool and lasting past week 20. A
  peak-gated pool renewal for all LPs (the soundtrack hit tail generalized) would address the 1M+ artist LPs and §64's
  long-runners together, but it lengthens reigns too, so it should sit on the `rs20u05` draw.

## 68. Screen: `--album-chart-racks=W`, rack sales in the album chart (2026-11-18; seeds 1001/2002, 104 weeks)

**Switch** (`AlbumChartRacks` in `Systems/GameyAuditSwitches.cs`, read in `AlbumSimulator.CalculateChartPoints`): an LP's
chart points count store units + W × its rack units this week (racks clear before the album chart ranks). Source: Billboard
4 January 1960 integrated rack sales into the Top LP charts (§67). Budget editions untouched. Checks in
`GameyAuditSwitchChecks` pass. Arm `rcr1` = `rs20u05` + W 1 (+ the inert demand trace).

| Seeds 1001/2002 | `rs20u05` | `rcr1` | Billboard |
|---|---|---|---|
| No. 1s 1960 / 1961; longest run | 10 / 12.5; 16.5 / 11 | 9.5 / 11.5; **11 / 11** | 10 / 12; 13 / 10+ |
| Top-10 debuts 1960 / 1961 | 11 / 10.5 | 8.5 / **4** | rare |
| 40+ week slots 1960 / 1961 | 19 / 15.5 | **20 / 21.5** | 38 (Jan 1960) |
| ST share of top-10 titles 1960 / 1961 | 14 / 4.5 | **21.5 / 13** | |
| ST points, 1961 top 50 (pre-1960 / 1960 / 1961 releases) | 17 (10 / 5 / 2) | **22** (16 / 4 / 2) | 24 (10 / 10 / 4) |
| **Head gap 1960 / 1961** | 49 / 43 | **55 / 80** | 0 |
| Easy Listening / TradPop / Youth, 1961 top 50 | 19 / 33 / 10 | **35 / 39 / 2** | 10 / 22 / 16 |
| Folk / Comedy, 1961 | 6 / 8 | 1 / 1 | 10 / 12 |
| Big-5 share of LP chart slots 1961 (1001 / 2002) | 72.9 / 69.4 | 83.6 / 75.4 | |
| **Big-5 Top 10 singles 1961** (1001 / 2002) | 23.2 (24.0 / 22.3) | **30.5** (24.4 / 36.5) | 20.6 |
| LPs <4k; losing; bankruptcies | 39.5%; 41%; 72 | 40.5%; 42.6%; 74.5 | RIAA 49%; 61% |

- **Shape improves:** shorter 1960 reigns, fewer launch spikes, more long-runners, and soundtracks reach the top 10
  (1961 22 points vs 24). The soundtrack carry-over goes to the opening catalogue (16 vs 10), not to 1960 releases (4 vs 10).
- **The genre head breaks.** Racks sell to household adults (segment tilt: AdultMOR/Family 1.0, Youth 0.3, CollegeFolk
  0.4), so W 1 turns the chart into Easy Listening and TradPop (1961: 74 of 100 points) and youth, folk and comedy
  vanish. Billboard's chart with racks did not look like this, so either W 1 overweights racks (later Billboard
  booklets weight each rack reporter; the 1960 weighting is unknown) or the model's rack mix is too adult.
- **Big-5 rises again on seed 2002** (+14), through the LP chart (Big-5 LP slot share 69 → 75%): the §61 album-career
  channel.

**Status:** not adopted. The source supports racks in the chart; W 1 with the current rack mix does not fit. Next: a
lower W (0.3-0.5), or check the rack genre mix against NARM before weighting it.

## 69. Can the model make a Beatles-scale LP? (2026-11-18; read-only, `acis260` 1962-64 + code)

Targets: *Meet the Beatles!* ~3.65M in about ten weeks (Billboard of 28 March 1964, via Beatles Bible), 4.0M by the end of
1964, 11 weeks at No. 1; *Sgt. Pepper* ~2.5M in its first three months (secondary sources). That is ~250-365k copies a
week sustained for ten weeks or more.

**The model's best LPs (`acis260`, store + rack):** peak weeks 240-323k (1962-64), which is Beatles-scale, but only 1-6
weeks at 100k+ and 9-17 at 50k+; 0.9-1.2M in the first ten weeks (a third of *Meet the Beatles!*) and 1.8-2.4M in the
first year (about half). The biggest 1964 youth LP (BritishPop, seed 2002) sold 0.69M in ten weeks, 1.25M in a year.

What limits it, in order:

1. **The buyer pool is the genre's, not the act's.** Each LP sells against its genre's national LP pool (1961: Comedy
   604k, Folk 538k, TradPop 462k, RockAndRoll 419k, TeenPop 370k; `MarketRegion.GetAlbumMarketSize`). Exhaustion
   (`max(0.15, 1/(1+4p))`) reaches its floor near 1.4× the pool and lifetime sales end near 1.8× (§67). No act can
   have a larger LP audience than its genre; there is no artist analogue of the soundtrack hit-tail renewal.
2. **Fans move LP buyers but don't add them.** Follower launch intent is 0.12 LP per young follower and 0.30 per adult
   over three weeks, and `MediaFollowerDemand.ConserveAlbumIntent` holds each region's LP intent at its pre-fan total,
   taking the fans' grants from every other title. The largest following in the 1960-64 runs is ~1.0-1.2M people, so a
   phenomenon act adds at most ~150-350k LPs and takes all of them from other LPs. Beatlemania could not grow the LP market.
3. **Artist LPs decay by time** (0.93 a week from week 8, `AlbumSimulator`), whatever their success, so a ten-week
   plateau is impossible even with buyers left.
4. **Not binding:** a title's weekly store cap summed over regions is ~880k ((record stores × 130 + department stores ×
   300) × depth × economic scale) and its stock ceiling ~1.05M; national store LP sales run ~1.0M a week in 1962-64.
   Pressing and shelves would allow a Beatles LP.

**Answer: not with the current demand model.** The weekly peak is reachable, the plateau and the total are not. A
Beatles-scale release needs an act-level audience that can exceed its genre pool and add market (new LP buyers), plus a
success-gated slowdown of decay and exhaustion. That is the same mechanism §67 proposes for 1M+ artist LPs and §64's
long-runners. Unmeasured: the 1965-69 pools (the saved 1965 world will not resume under the current flags:
`--local-retail-exposure` is a fresh-world switch); a decade run with `--album-demand-trace` would give them.

## 70. Where the youth LPs went under `rcr1`, and what racks in the chart need (2026-11-18; seeds 1001/2002, 104 weeks)

`rs20dt` = `rs20u05` + `--album-demand-trace` (inert: head gap identical to `rs20u05`). `analyze_album_chart_rack_mix.py`
re-ranks the traced LPs offline by store + W × rack each week. At W 0 it reproduces the real chart (gap 45 / 43 vs 49 / 43),
so the static re-rank is usable. `analyze_album_head_gap.py` now reads `.csv.gz`.

**rcr1's 1961 gap of 80 has two parts.**

| 1961 top 50 (two-seed mean) | Youth | TradPop | Easy L. | ST | Gap |
|---|---|---|---|---|---|
| `rs20dt`, store only (W 0) | 10 | 33 | 19 | 14 | 43 |
| `rs20dt` world, re-ranked at W 0.25 / 0.5 / 1 | 7 / 6 / 4 | 30-32 | 23-26 | 16-18 | 47 / 51 / 54 |
| `rcr1` (W 1 live) | 2 | 39 | 35 | 22 | 80 |
| Billboard | 16 | 22 | 10 | 24 | 0 |

1. **Ranking (43 → 54):** racks are adult. Rack/store units in 1961 (`rs20dt`): Easy Listening 1.70, TradPop 1.24,
   Comedy 1.00, Folk 0.89, Youth 0.73 (soundtracks 2.08), from `RackJobberChannel.SegmentTilt` (AdultMOR/Family 1.0,
   Youth 0.3). Any W > 0 moves youth down: even W 0.25 costs 3 points, so a lower W does not fix youth.
2. **Feedback (54 → 80):** in the live run the chart moves store sales. Youth's share of traced store units falls from
   17.8% to 12.8% in 1961, TradPop rises 31 → 41% and Easy Listening 17 → 26%. The album chart sets a store awareness
   floor (top 10: 0.48, top 40: 0.30, `AlbumSimulator.CalculateRegionalSales`), so adult LPs lifted by racks keep store
   awareness that youth LPs lose.

**What the rack mix would need** (offline, `rs20dt` world; a = how far each group's rack/store ratio moves to the mean of
artist LPs; soundtracks keep theirs):

| W 1 | 1960 gap; Youth | 1961 gap; Youth |
|---|---|---|
| model mix (a 0) | 66; 6 | 54; 4 |
| halfway (a 0.5) | 61; 7 | 48; 6 |
| racks proportional to store (a 1) | 54; 10 | **38; 11** |
| a 1, hit tier only (catalogue keeps the tilt) | 57; 9 | 50; 6 |

- With a genre-neutral rack the 1961 chart gains soundtracks and loses nothing (gap 38 < 43). In 1960 adult LPs still
  gain (TradPop 36 → 40) because racks add weeks to adult LPs through the **catalogue tier**; correcting only the hit
  tier does not help. Youth LPs are launch-loaded and leave both tiers fast.
- So the youth loss under racks is the rack genre tilt plus the chart feedback that amplifies it. W only scales it.
- **The store-only chart is already off** (W 0: Youth 12 / 10 vs 14 / 16; TradPop +8/+11, Easy Listening +12/+9, Folk and
  Comedy −8 to −10). Head titles (`rcr1` 1960 releases reaching the top 50) show why youth is fragile: median LP format
  tilt 0.57 (TradPop 1.10, Easy Listening 1.18) and pool 299k (462-476k), about 0.37 of a TradPop LP's demand, across
  800 youth LPs a year (TradPop 350). Youth reaches the head only through the reception draw's top and its followers,
  which is why youth swings ±5-8 between seeds and reception arms (`acis` 19 / 15, `rs20u05` 10 / 10).

**Evidence still missing:** the rack genre mix itself. Billboard ran separate rack sales charts until the end of
1959 (announced in the 4 January 1960 issue). A late-1959 issue would give the rack top LPs directly: if they look like the store
chart, the tilt is too adult for best sellers and a genre-neutral (or much flatter) rack mix is sourced; if they are
mood music and cast albums, Billboard's integrated chart weighted racks lightly and W should be fitted to the 1961 head.

**Next:** read the late-1959 rack chart; then one live arm `rcr1` + the sourced rack tilt (switch to build), against
`rs20u05`, two seeds, 104 weeks. The store-only TradPop/Easy Listening excess is a separate, older problem (gap 14 / 19 at
§27's `bsc13`, 38 / 37 by `frd60pk`).

## 71. The last Billboard rack charts (30 November and 28 December 1959) against the model's racks (2026-11-18; read-only, `rs20dt`)

**Source.** *Billboard*, 30 November 1959, p. 26, and 28 December 1959, p. 18 (worldradiohistory.com;
`SimLogs/album-promo-research/bb-1959-11-30/`, `bb-1959-12-28/`):
"Best Selling LP's on the Racks", top 10, "tabulated from the sales made by the nation's leading rack service
merchandisers and jobbers", one of four rack charts on a four-week cycle (LPs, low-price LPs, EPs, kiddie records).
The December chart is the last before racks went into the main charts. The two share 8 of 10 titles, so the
November chart confirms the December one; it is not an independent sample. Classified as in §13 (December):

| Rank | Racks | Store mono chart, same issue |
|---|---|---|
| 1-5 | Chipmunks (Children's); Mathis *Heavenly*; *Christmas Sing Along With Mitch*; Darin *That's All*; Sinatra *No One Cares* | Kingston Trio; Berman; Mathis *Heavenly*; *Sixty Years* sampler; *South Pacific* film |
| 6-10 | *More Johnny's Greatest Hits*; **Ricky Nelson *Songs by Ricky*** (store #45); *Party Sing Along*; *Como Swings*; *Kingston Trio at Large* | Berman; Mormon Tabernacle; Mitch (Christmas); Chipmunks; Kingston Trio |

| Titles in a 4-week top 10 | Racks, Billboard (Dec / Nov) | Racks, model | Store, Billboard (that week; Dec / Nov) | Store, model |
|---|---|---|---|---|
| TradPop | **7 / 6** | 3.0 | 3 / 3 | 3.8 |
| Soundtrack | **0 / 0** | 2.9 | 1 / 1 | 0.6 |
| Easy Listening | **0 / 1** | 2.8 | 0 / 0 | 2.0 |
| Youth | **1 / 1** | 0.35 | 0 / 0 | 1.6 |
| Comedy / Folk | 0 / 1 both | 0.3 / 0.2 | 2 / 2; 1 / 4 | 0.6 / 0.5 |
| Children's, other | 1 / 1 | 0.2 | 2 / 1 | 0.7 |

Model: `rs20dt`, seeds 1001/2002, national rack and store units summed over every four-week window from week 12 to 104;
the rack top 10 sits at median store rank 7.

- **The model's racks favour soundtracks and mood music. Billboard's favoured the big pop singers.** Every title on the
  Billboard list is a famous name: Mathis, Sinatra, Como, Mitch Miller, Darin, Nelson, the Kingston Trio, the Chipmunks.
  The store chart's comedy (Berman, #2 and #6), its soundtrack (*South Pacific*, #5) and the orchestras (Mantovani,
  Conniff, Denny, all in the store top 50) are missing from the rack top 10.
- November's rack list: Chipmunks; *Heavenly*; *No One Cares*; **Songs by Ricky**; Mancini *More Music From Peter Gunn*
  (Easy Listening, as §13 files *Mr. Lucky*); *Party Sing Along*; *Como Swings*; *More Johnny's Greatest Hits*;
  *Kingston Trio at Large*; *That's All*. The store top 10 that week holds four Kingston Trio LPs, Berman and *South Pacific*;
  the rack list has one Kingston Trio LP and neither of the others.
- **Youth isn't penalized on racks.** *Songs by Ricky* is #4 on racks and #39 in stores (November), #7 and #45 (December);
  the Chipmunks are #1 on racks both times (#47 and #9 in stores). The model's rack
  top 10 has a fifth of the youth titles its store top 10 has (Youth tilt 0.3).
- **Caveats:** two overlapping charts of ten titles, no unit figures. The direction is clear on soundtracks (model 2.9,
  Billboard 0 on both) and TradPop (3 vs 6-7). On youth the evidence is one teen star, twice, ranked far above his store
  position. Only one Christmas title is seasonal (December's *Christmas Sing Along*; November has none).

**Reading against §70.** The genre-neutral rack (a = 1), which restored youth offline, has this direction. The source
points further: racks sold *names*. In the model, rack demand is quality × word of mouth × genre weight × shelf
exposure, with no term for the act's recognition, and soundtracks get a near-maximal weight (0.9). Two candidate
designs:

1. **Flatter tilt** (calibration only): Youth and UrbanRnB near the adult segments, Easy Listening no higher than
   TradPop, a lower `--rack-soundtrack-weight`; refit `--rack-shopper-scale` to front-line racks 36-42M (1961).
2. **Name-led rack demand** (mechanism): rack conversion scales with the act's recognition (the fame ladder), so star
   LPs of any genre sell on impulse and actless soundtracks and mood records rely on the shelf. This fits the list
   better. It also touches the Big-5 channel (§61): stars are mostly on Majors.

Either way, soundtracks lose rack-borne chart points, so §68's soundtrack gain under `--album-chart-racks` partly goes away. §67's
store-awareness route for soundtracks remains the sourced one.

**Next:** build option 1 or 2 as a switch and run it with `--album-chart-racks=1` against `rs20u05` (two seeds, 104
weeks). Older rack charts (1958-59) would not add much: these lists move slowly.

## 72. Screen: `--rack-genre-flatten=F` with racks in the chart (2026-11-18; seeds 1001/2002, 104 weeks)

**Switch** (`RackJobberChannel`, `--rack-genre-flatten=F`, needs `--rack-jobbers`): each non-soundtrack rack genre weight
(baseline × segment tilt) moves geometrically toward 0.37, the 1961 rack-unit-weighted mean (`rs20dt`): w^(1−F) × 0.37^F.
Soundtracks keep `--rack-soundtrack-weight` (0.3 in the candidate). Their rack excess comes from the hit-tail renewal,
not the weight. 1961 weights before: Easy Listening 0.53, Comedy 0.36, TradPop 0.35, DooWop 0.31, Folk 0.24,
RockAndRoll/TeenPop 0.17, R&B/Soul 0.13. Off: unchanged. Checks in `RackJobberChecks` (62) pass.

Arms on `rs20u05` + `--album-chart-racks=1` + demand trace: `rgf1` (F 1) and `rgf1s30` (F 1 + `--rack-shopper-scale=0.030`
instead of 0.033, via `ref-rs20u05-rackscale030-request.json`, because the first copy of a flag wins). Scored by
`analyze_album_head_gap.py` rows, `analyze_big5_1961_excess.py` and `analyze_lp_riaa_1965.py` (now reads `.csv.gz`).

| Seeds 1001/2002 | `rs20u05` (store chart) | `rcr1` (racks, tilt) | `rgf1` (racks, flat) | **`rgf1s30`** | Target |
|---|---|---|---|---|---|
| Head gap 1960 / 1961 | 49 / 43 | 55 / 80 | 41 / 37 | **37 / 35** | 0 |
| Youth 1960 / 1961 | 10 / 10 | 12 / 2 | 13 / 13 | **15 / 10** | 14 / 16 |
| Soundtrack 1960 / 1961 | 19 / 17 | 20 / 22 | 18 / 26 | **18 / 22** | 16 / 24 |
| TradPop / Easy L. 1961 | 33 / 19 | 39 / 35 | 28 / 20 | 27 / 19 | 22 / 10 |
| Folk / Comedy 1961 | 6 / 8 | 1 / 1 | 4 / 4 | 6 / 5 | 10 / 12 |
| Youth share of store / rack units 1961 | 18 / 10 | 13 / 6 | 22 / 20 | 20 / 19 | |
| Rack top 10 (4-wk): ST / TradPop / EL / Youth | 2.9 / 3.0 / 2.8 / 0.4 | 2.8 / 2.8 / 3.6 / 0.3 | 2.5 / 2.6 / 1.7 / 1.9 | 2.2 / 2.3 / 1.9 / 1.7 | 0 / 6-7 / 0-1 / 1 |
| Front-line racks 1960 / 1961 (M) | 31 / 41.5 | 31.5 / 43.5 | 36.5 / 45.5 | **33 / 42.4** | – / 36-42 |
| Store LPs 1961 (M) | 57.0 | 54.5 | 52.9 | 54.7 | 68 |
| Big-5 LP chart slots 1961 (1001 / 2002) | 72.9 / 69.4 | 83.6 / 75.4 | 81.5 / 73.5 | 71.5 / 77.0 | |
| **Big-5 Top 10 singles 1961** (1001 / 2002) | 23.2 (24.0 / 22.3) | 30.5 (24.4 / 36.5) | 26.2 (28.1 / 24.2) | **19.4** (22.3 / 16.5) | 20.6 |
| LPs <4k; losing; bankruptcies | 39%; 40.5%; 72 | 40.5%; 42.6%; 74.5 | 41%; 42%; 63 | 40.5%; 40.4%; 67 | RIAA 49%; 61% |

- **Flat racks fix the chart that racks broke and beat the store-only chart.** Youth comes back (15 / 10 against 2 in
  `rcr1`), soundtracks reach their 1961 target through the rack counting §67 sourced, and Easy Listening falls from
  35 to 19. The 1961 gap is 35, against 43 for the store-only control.
- **Rack volume needs the refit.** Flattening raises youth rack sales more than it lowers Easy Listening's, so racks gain
  2-5M at scale 0.033. At 0.030 they are back at 42.4M, the top of the fitted range.
- **Big-5 is a guard pass on two seeds, but noisy.** Racks in the chart lift Big-5 LP chart slots (§61's album-career
  channel) and `rcr1` reached 30.5. `rgf1s30` is 19.4, with seed 2002 swinging from 36.5 (`rcr1`) to 16.5. Two seeds
  cannot rank this.
- **What stays off is the store side:** TradPop +5, Easy Listening +9, Folk −4, Comedy −7 (1961), the same rows as
  before racks (§60). On racks, soundtracks (2.2 of the top 10 against Billboard's 0) and youth (1.7 against 1) are still
  high and TradPop low (2.3 against 6-7). The hit-tail renewal still feeds soundtracks on racks, and the model has no
  name-led rack demand (§71 option 2).

**Status:** `rgf1s30` (`--album-chart-racks=1 --rack-genre-flatten=1 --rack-shopper-scale=0.030` on `rs20u05`) is the
candidate rack setting. Not adopted. Next: seeds 3003/4004 (the Big-5 swing) and a 1962-64 look before adoption. The
store-side TradPop/Easy Listening excess and Folk/Comedy shortfall are a separate question.

## 73. The Beatles question, part 2: what a phenomenon LP needs (2026-11-18; read-only, `acis260` 1962-64 four seeds + code)

§69 found three LP-side blockers: the genre pool, follower intent that only displaces (`ChartManager.ApplyFollowerAlbumIntent`
→ `MediaFollowerDemand.ConserveAlbumIntent`), and fixed time decay (`AlbumSimulator.GetCatalogDecayMultiplier`). Two
upstream facts sit in front of them.

**1. The model's LP hits do not come from its singles stars.** The three biggest non-soundtrack LPs of 1962-64 per
seed are a jazz LP, an orchestra, folk and surf acts, a TradPop singer: acts with 0-0.5M singles in the 26 weeks before
release. The acts with the most singles sell little on LP:

| `acis260`, 1963-64 | Singles (2 yr) | Share of singles | Peak following | Their LPs (each) |
|---|---|---|---|---|
| Top 4 singles acts per seed (16 acts) | 2.6-5.0M | 1.2-2.2% | 0.56-1.28M | 5-62k peak week; 16-200k typical; best 496k (hits package) and 551k |
| Their LP chart peaks | | | | #1-#42, mostly #8-#20; TeenPop acts #13-#42 |

Historically the youth acts in the LP head were the singles stars (§13, §26), and *Meet the Beatles!* (3.65M in ten
weeks) sold on the back of the same act's singles run (the ratio to its singles is unsourced here). The live link is `HitPackageDemand` (candidate: K 0.4, youth-weighted,
studio 0.75): a studio LP carrying released singles adds about 0.2 × those singles' buyers to its pool. Those buyers come
from a peak→units table (a No. 1 counts 878k, the 1960-61 median) rather than from the singles' real sales, and the
act's other current hits count for nothing unless they are on the LP.

**2. The model makes no singles phenomenon.** Biggest single 1960-64: 1.4-1.7M lifetime, 100-205k in its best week
(four seeds); no act takes more than 5-6% of a week's singles market (median 2.1M a week in 1964). Beatlemania was a
singles event first (the Beatles held the top five of the Hot 100 on 4 April 1964). The singles purse is fixed and rationed
(memory: singles ceiling is the flat ration), so an act cannot take a large share of it.

**So a Beatles-scale LP needs a chain, and the LP side alone cannot produce it.** Proposed stages, each its own switch:

| Stage | Mechanism | What it buys | Risk |
|---|---|---|---|
| A. Act audience on LP | An LP's pool adds K × the act's *actual* singles buyers in the region over the last N weeks (all its singles, not just those on the LP), replacing the peak table. Optionally a share n of follower LP grants is new market (not conserved). | Singles stars' LPs reach the head: the youth LP head (§70-72) is Billboard's star LPs (Elvis, Nelson, Brenda Lee, Checker). Scale: a 1M-in-26-weeks act at K 0.3 → +300k pool, LPs in the 150-300k band. | Duplicates `HitPackageDemand` for compilations; must replace, not stack. Big-5 channel (§61) via star LPs. |
| B. Success-gated LP life | Time decay pauses (or slows) while the LP holds the top N or sells ≥ X% of its peak, generalizing the soundtrack hit tail (§64 item 3, §67). | Long-runners and ten-week plateaus. | Lengthens No. 1 reigns, which §66 just shortened. Gate tight. |
| C. Singles phenomenon | Act-level singles demand above the genre pool and the ration (an act's followers add singles market, or the ration lets one act's records take a larger share). | The precondition for a 1964-style act; nothing in A or B makes one. | Touches the fixed-purse singles calibration, Top 10 turnover and the Big-5 guard. Largest project. |

**Recommendation:** A first. It is sourced on the 1960-61 chart (star LPs), plugs into the youth head work, and can be
screened on 104 weeks. B second, gated on A's hits. C is a separate singles project; without it the model's ceiling is a
~0.5-1M star LP, not 3-4M. Unmeasured: real units of the model's top singles acts' LP-carried tracks (needs
`album-track-links` + records), and the 1965-69 pools (§69).

## 74. Screen: `--act-lp-audience=K`, an act's singles buyers on its LPs (Beatles stage A; 2026-11-18; seeds 1001/2002, 104 weeks)

**Switch** (`ActLpAudience` in `Systems/GameyAuditSwitches.cs`; hooks in `ChartManager.CalculateLiveRegionalSalesWithMarketClearing`
and `AlbumSimulator.CalculateRegionalSales`): each act keeps a regional ledger of its cleared single and EP units, halved
every 13 weeks (`--act-lp-audience-half-life`). An act's LP (studio or compilation, not a soundtrack) adds K × the genre's
Youth weight (with `--hit-package-demand-youth`) × the act's ledger to its buyer pool, taking the larger of this and the
hit-package term so the same buyers count once. Fresh-world only (the ledger is not saved); no draws; off is
byte-identical in code (the hit-package line is unchanged when off). Checks in `GameyAuditSwitchChecks` pass.
Scorer: `SimTools/score_act_lp_audience.py`.

Control `rgf1s30` (§72). Arms `aud05` (K 0.5) and `aud10` (K 1.0), with the demand trace. Judged on the distribution,
not the peak: a Beatles-scale LP should be possible but very rare.

| Seeds 1001/2002 | `rgf1s30` | **`aud05`** | `aud10` | Target |
|---|---|---|---|---|
| Top 10 singles acts of 1960-61: LPs; median store + rack (k) | 30; 157 | 34.5; 179 | 37.5; 204 | |
| … share of their LPs reaching the album top 10 | 35% | 43% | 48% | |
| 1961 top 50: LPs by acts with a Top 10 single in the prior year (youth) | 14.5 (4.5) | 16.5 (7.5) | 19 (9) | |
| Youth 1960 / 1961 | 15 / 10 | **15 / 16** | 20 / 19 | 14 / 16 |
| Head gap 1960 / 1961 | 37 / 35 | **33 / 31** | 41 / 30 | 0 |
| LPs 300k+ / 1M+ (RIAA scorer, all 1960 releases) | 2.75 / 0.45% | 2.85 / 0.48% | 3.1 / 0.42% | 1.2 / 0.4% |
| LPs <4k; losing; bankruptcies | 40.5%; 40.4%; 67 | 41%; 42%; 60 | 41%; 42%; 61 | |
| Biggest LP in the run (k, store + rack) | 1,290 | 1,371 | 1,586 | |
| **Big-5 Top 10 singles 1961** (1001 / 2002) | 19.4 (22.3 / 16.5) | **30.0** (30.0 / 30.0) | 29.8 (26.9 / 32.7) | 20.6 |

- **The LP side does what stage A is for.** Singles stars' LPs reach the head and the youth head lands on Billboard at
  K 0.5 (15 / 16). The 1961 gap falls to 31. The top of the distribution barely moves: 1M+ stays at the RIAA rate and
  the biggest LP is 1.4M. A Beatles-scale LP is still out of reach without a singles phenomenon (§73 stage C), as it should be.
- **Big-5 rises by ~10 at both K, and the rows say why.** `analyze_big5_1961_excess.py`, 1961 Top 10 slots:

| 1961 Top 10 slots (%) | history | `rgf1s30` 1001 / 2002 | `aud05` 1001 / 2002 |
|---|---|---|---|
| Big-5 acts with a 1960 Top 10 | 16.4 | 7.3 / 6.4 | **12.7 / 15.2** |
| … slot-weeks per such act | 4.45 | 1.9 / 1.8 | **3.9 / 4.2** |
| Big-5 acts without one (newcomers) | 4.2 | 15.0 / 10.2 | 17.3 / 14.8 |
| Other labels' 1960 hit acts | 22.9 | 26.7 / 26.7 | 25.0 / 18.1 |

  The act audience brings Big-5 hit acts' 1961 follow-through up to history (the follow-up shortfall in memory: Big-5
  follow-ups are historical, the field's are short). The total overshoots because Big-5 newcomers were already 2.5-3.5×
  history in the control and stay there. On seed 2002 the label split also moves toward history (Columbia 5 → 27 slots
  against 23, RCA 11 → 35 against 44, Decca 11 → 28 against 28, Mercury 59 → 42 against 12).
- **Channel, partly measured.** With `--album-career-credit-off` (an LP's chart run credits nothing to the act) on both
  control and arm, the act audience moves Big-5 by −1.3 (1001) and +8.6 (2002), against +7.7 and +13.5 with credit on.
  Roughly two-thirds plausibly goes through §61's album-career ladder; two reshuffled seeds cannot pin it down.

**Status:** `aud05` (K 0.5) is the stage A candidate for the LP and youth rows. Not adoptable on its own: it breaks the
1961 Big-5 total, though through a historically correct row. Two ways forward: (1) pair it with the Big-5 newcomer fix
(the 2.5-3.5× newcomer excess is the pre-existing gap; memory: Big-5 premium hub), or (2) stop double credit: an LP's career
credit should not count sales to the act's own singles buyers, which its singles already credited. Seeds 3003/4004 before
either.

## 75. The Big-5 newcomer excess under stage A: per entry it is on history; earned indie coverage rejected (2026-11-18; seeds 1001-4004, 104 weeks)

The author picked option (1) of §74: fix the Big-5 newcomer excess. Scorers `SimTools/analyze_big5_1961_newcomers.py`
(1961 Big-5 Top 10s by career, prior chart best and plug outcome) and `SimTools/score_climb_by_prior.py` (Top 10 per Hot 100
chart event by the act's prior best; history label-blind, since labels exist only for Top 10s).
`score_chart_faller_depth.py` now reads `SEEDS=` (it divided pairs by four).

**Per Hot 100 entry, Big-5 newcomers climb at history's rate; field first entries climb at half of it.** Chart events
debuting weeks 40-92 (`aud05`, four seeds), Top 10 % per event:

| Prior best of the act | History 1961-63 (label-blind) | Big-5 | MidTier | Field |
|---|---|---|---|---|
| none (first entry) | **20.8** (206/yr) | **23.2** (35/yr) | 18.6 | **10.1** (194/yr) |
| 41-100 | 9.0 | 21.7 | 18.2 | 19.7 |
| 11-40 | 10.2 | 27.6 | 17.6 | 14.4 |
| Top 10 | 17.5 | 24.1 | 15.4 | 20.0 |

History climbs best on a first entry. The sim climbs worst there, for the field, and repeat entries after a 11-100 best
climb about twice history's rate in every group. Routine Big-5 newcomer singles (plug slots) enter the Hot 100 at the
Independents' rate within each quality bin (19.3 vs 19.1% at q > .76). Their premium is after entry (Top 10 per entry
21 vs 13%), with Plugged picks (q .77) on top. The Big-5 newcomer excess is therefore entry volume (35 first entries a year from
~260 NewSigning issues, quality .71-.77) at a historical climb, set against a field whose first entries under-climb.

**Stage A feeds the Big-5 newcomer row.** 1961 Top 10 slot points, Big-5 acts without a 1960 Top 10 (history 4.2):

| | control | `aud05` |
|---|---|---|
| 1001 / 2002 (control `rgf1s30`) | 15.0 / 10.2 | 17.3 / 14.8 |
| 3003 / 4004 (control `frd60pk`, no §60-72 LP flags) | 11.7 / 7.3 | 14.8 / 21.5 |
| Big-5 1960 hit acts, same seeds (history 16.4) | 7.3 / 6.4; 10.0 / 11.0 | 12.7 / 15.2; 11.4 / 12.9 |

On four seeds the 1961 Big-5 total is 30.0 / 30.0 / 26.2 / 34.4 (history 20.6). 1960 is 25.1 on 1001/2002 and 30.5 on
3003/4004 (frd60pk 21.8; target 27). On 1001/2002 the added newcomer Top 10s are youth NewSigning singles. Big-5 youth
NewSigning singles launched in weeks 40-92 reach the Top 10 at 0.4% in `rgf1s30` and 5.9% in `aud05`, from the same
~128 issues a seed, with no singles-side code path. The act audience touches only LP pools. The rise comes through what
an act's LPs feed back: §74's `--album-career-credit-off` probe puts about two-thirds of the Big-5 rise through the album
career ladder.

**Screen: `--earned-indie-coverage=1` on `aud05` (`aud05ec`, 1001/2002). Rejected.** The switch already exists
(2026-11-11): it gives a charted non-Major record's uncovered regions covered-grade restock and shelf capacity. Field first
entries stay at 10.2 → 11.7% Top 10. 1961 Big-5 is 30.0 → 29.3, and the newcomer row rises 16.1 → 17.9. 1960 Big-5 falls
25.1 → 20.8 and closures rise 91 → 108.5. Shelf access does not hold field first entries back.

**Reading.** With stage A, the newcomer excess has two parts. One is stage A's own feedback (LP chart runs → the
career ladder → Big-5 youth newcomers), which is §74's option (2): a career credit for an LP's sales to buyers the act's
singles already credited. The other is the pre-existing volume of Big-5 newcomer first entries. Per entry it is
historical, so it is not a launch or climb lever. Of the two, the double credit is the one stage A created and has a
measured channel. Next: build `--album-career-credit-net-of-act-audience` (career credit from an LP's sales above the act-audience
term), 4 seeds paired with `aud05` on seeds 3003/4004 with an `rgf1s30` control on those seeds.

## 76. Screen: `--act-lp-audience-net-credit`, an LP's career credit net of the act's own singles buyers (2026-11-18; seeds 1001-4004, 104 weeks)

**Switch** (`ActLpAudience.NetCreditSwitch` in `Systems/GameyAuditSwitches.cs`; `ChartManager.AssignActNetAlbumPositions`,
`AlbumScaleCareerMilestones.ObserveNetCareerPeak`; needs `--act-lp-audience=K` and `--album-scale-career-milestones`).
`AlbumSimulator` records the share of each region's LP pool that stage A adds over the hit-package term. Each week, every
ranked LP gets a net album position: its chart points (or trailing blend) times (1 − act-audience units / chart units),
ranked against the other entries' real points. The act's career (milestone latches, run completion, recognition, contract
evidence: every `ObserveActCareerPeak` / `ActCareerPeak` read) takes the best net position's singles equivalent. An LP that
charts only on its act audience never charts for the act. The chart, LP demand, label hit credit, landmark, acclaim and
evolution keep the real position. Checks pass. Off is byte-identical: `aud05id-1001-sc` on the new build matches
`aud05-1001-sc` in records, weeks, label finance, album chart, label sales and launch attribution.

Arms: control `rgf1s30` (3003/4004 run for this screen), `aud05`, and `audnet` (`aud05` + the switch).

| Seeds 1001-4004 | `rgf1s30` | `aud05` | **`audnet`** | Target |
|---|---|---|---|---|
| **Big-5 Top 10 share 1961** (by seed) | 23.9 (22.3 / 16.5 / 27.9 / 29.0) | 30.1 (30.0 / 30.0 / 26.2 / 34.4) | **21.2** (18.5 / 22.5 / 20.4 / 23.3) | 20.6 |
| Big-5 Top 10 share 1960 | 26.2 | 27.8 | 25.6 | 27 |
| 1961 slot points: Big-5 acts with no 1960 Top 10 | 13.9 | 17.1 | 10.6 | 4.2 |
| … Big-5 1960 hit acts | 10.1 | 13.0 | 10.6 | 16.4 |
| … field 1960 hit acts / field new | 23.7 / 52.4 | 21.0 / 48.9 | 26.9 / 51.9 | 22.9 / 56.5 |
| Proven / other events: T10 % | 21.6 / 15.4 | 19.6 / 15.2 | 20.8 / 14.0 | 21.6 / 15.3 |
| Labels closed by week 104 | 98.2 | 93.0 | 96.8 | |
| Top 10 singles acts' LPs reaching the album top 10 | 34.5% | 35.0% | 35.0% | |
| … median LP (k, store + rack) | 170 | 177 | 175 | |
| 1961 top 50: singles-star LPs (youth) | 15.0 (6.0) | 17.2 (7.2) | 13.8 (6.5) | |
| Youth in the year-end top 50, 1960 / 1961 | 15.5 / 12.5 | 15 / 16 | 17.5 / 14.5 | 14 / 16 |
| Album head gap 1960 / 1961 | 38 / 29 | 32 / 26 | 40 / 30 | 0 |

- **The switch removes stage A's Big-5 rise.** 1961 Big-5 falls 30.1 → 21.2, down on 4/4 seeds against `aud05`. Against the control
  it is 2.7 lower (2/4 seeds down), inside seed noise. Both rows that stage A raised come back: Big-5 newcomers 17.1 → 10.6
  (control 13.9) and Big-5 1960 hit acts 13.0 → 10.6 (control 10.1). So §74's "historically correct" hit-act gain also
  ran through the double credit. Closures and 1960 stay in their ranges.
- **On four seeds stage A's LP gain is small.** §74's two-seed rows overstated it. The share of top singles acts' LPs
  reaching the album top 10 is 34.5 / 35.0 / 35.0%; their median LP is 170 / 177 / 175k; the head gap is 38/29 against
  32/26. What holds is the 1961 youth row: 12.5 → 16 under stage A and 14.5 with the net credit (target 16). Beatles
  stage A buys a youth LP head through the act's own buyers, and with the double credit removed it keeps about half of that.
- **The Big-5 mix is unchanged from the candidate.** Newcomers are 2.5× history and hit acts 0.65×, as before stage A
  (§75: the newcomer surplus is first-entry volume at a historical climb).

**Status:** `aud05` + `--act-lp-audience-net-credit` is safe on the singles side and adds a modest youth LP gain.
It does not close the album head gap. Not adopted; the author decides whether the youth row justifies the pair, or
whether stage A waits until §73's stage B (success-gated LP life) gives it something to carry.

**ADOPTED 2026-11-18 by the author, with the LP chain underneath it.** `SimLogs/writing-calibration/candidate-request.json`
(267 flags; previous `candidate-request-pre-lp-chain-audnet-2026-11-18.json`) now carries `--earned-lp-forecast-warm-start=20`,
`--album-career-importance=1960:0.3,1964:0.5,1967:0.85,1969:1` (§62-63), `--lp-reception-sigma=2.0 --lp-reception-upper=1:0.5`
(§66, replacing σ 1.5), `--album-chart-racks=1 --rack-genre-flatten=1 --rack-shopper-scale=0.030` (§68-72, replacing
0.033), and `--act-lp-audience=0.5 --act-lp-audience-net-credit`. It matches the `audnet` requests except four telemetry
flags (`--album-demand-trace[-weeks]`, `--newcomer-connect-trace`, `--regional-home-sales`). Controls for later screens:
`audnet-{1001..4004}-sc`. Owed: the 1962-64 look at the rack setting (§72) and the decade check of the importance
schedule (§63). Next: §73 stage B, success-gated LP life.

## 77. Screen: success-gated LP life (§73 stage B): hold gates fail, a past-peak tail works (2026-11-18; seeds 1001/2002, 104 weeks)

**Switch** (`LpHitLife` in `Systems/GameyAuditSwitches.cs`; `AlbumSimulator.UpdateAlbum` advances it, `CalculateRegionalSales`
reads it). Each LP keeps a count of decay weeks taken off its 0.93 clock (`albumHitLifePausedWeeks`, runtime only). Two modes:

- **Hold** (`--lp-hit-life-top=N`, `--lp-hit-life-share=X`): §73's literal spec. In a week past 8 where the LP stood in the
  top N last week, or sold ≥ X × its best week, the clock stops (`--lp-hit-life-rate=S` keeps S of the week;
  `--lp-hit-life-max=M` caps the weeks removed, default 104).
- **Past-peak tail** (`--lp-hit-life-tail-top=N --lp-hit-life-tail=D`): an LP that peaked in the top N decays at D a week
  (vs 0.93, or the hit-package decay for compilations) in each week past 8 that it stands outside the top N. Its top-N weeks
  keep the normal clock, so reigns are not extended.

Exhaustion is untouched in both modes. Profiled soundtracks keep their box-office path. No draws. Checks in
`GameyAuditSwitchChecks` pass. Off is byte-identical: `hloff-1001-sc` matches `audnet-1001-sc` in records, weeks, label
finance and the album chart. Scorer: `SimTools/score_lp_hit_life.py` (artist LP = any format but Soundtrack).

Control `audnet` (the adopted candidate, §76). Arms: `hlt10` (top 10), `hls50` (X 0.5), `hlt10m26` (top 10, cap 26),
`hlq96` / `hlq98` (tail, top 10, D 0.96 / 0.98). Mean of seeds 1001/2002:

| | `audnet` | `hlt10` | `hls50` | `hlt10m26` | `hlq96` | **`hlq98`** | Billboard |
|---|---|---|---|---|---|---|---|
| No. 1s 1960 / 1961 | 7.5 / 6.5 | 5.5 / 6.5 | 7 / 10.5 | 6.5 / 9.5 | 6 / 11 | **9 / 10.5** | 10 / 12 |
| Longest No. 1 run 1960 / 1961 | 12.5 / 13 | **32 / 32** | 23 / 9.5 | 27.5 / 15.5 | 17.5 / 12.5 | **13 / 9.5** | 13 / 10+ |
| Slots held by 40+ week artist LPs 1960 / 1961 (%) | 0.3 / 0.0 | 1.4 / 2.7 | 0.7 / 0.1 | 1.4 / 1.8 | 0.7 / 0.8 | **1.5 / 5.4** | ~28 of 65 (Jan 1960) |
| … all titles 1961 (%) | 18.9 | 21.2 | 18.7 | 19.6 | 19.7 | 24.5 | 38 |
| Slots held by 20+ week artist LPs 1961 (%) | 7.6 | 15.2 | 11.0 | 14.3 | 10.8 | 20.4 | |
| Artist LPs reaching 40 chart weeks | 0.5 | 6.5 | 0 | 5.5 | 3.5 | **10.5** | |
| Median run: artist LPs debuting wk 2-40 / their top-10 titles | 12 / 20.8 | 12.5 / 32 | 13 / 24.5 | 12.5 / 32 | 12.5 / 25.5 | 13 / 28.5 | D7 12-18 |
| Charted artist LPs 300k+ / 1M+ (demand trace, launched wk 2-52; ~85-90 a seed) | 33.5 / 0 | 28 / 3 | 35 / 1.5 | 30 / 3 | 30.5 / 1.5 | 25 / 5 | |
| All 1960 releases 300k+ / 1M+ % (RIAA scorer, store + rack) | 2.95 / 0.45 | | | | | 2.9 / **0.73** | 1.2 / 0.44 |
| Biggest artist LP (k) | 1,294 | **5,275** (9,041 on 1001) | 2,012 | 2,633 | 1,446 | 1,906 | |
| Big-5 Top 10 singles 1961 (1960 hit acts + newcomers) | 20.5 | 18.5 | 24.7 | 19.8 | 25.1 | **19.8** | 20.6 |
| Store LPs 1960 / 1961 (M) | 35.2 / 54.2 | | | | 36.7 / 56.4 | 37.4 / 59.1 | – / 68 |
| Head gap 1960 / 1961 | 34 / 39 | | | | 36 / 35 | 37 / **50** | 0 |
| Labels defunct (1001; 2002) | 96; 104 | | | | 106; 96 | 104; 101 | |

- **The hold gates feed the monsters, not the long-runners.** Holding the top 10 is self-reinforcing: an LP that does
  not decay stays at No. 1. On seed 1001 *Nicky and the Martians* (Comedy, the §64 monster draw) holds No. 1 for 103 weeks
  and sells 9.0M. A 26-week cap still gives it a 42-week reign. The share gate is self-limiting, because exhaustion
  pulls every other LP below half its best week by about chart week 10, so it extends only titles still selling at peak:
  the monster again (23-week reign) and almost no long-runners (0.1%). §73 flagged this risk. The data show that
  "success" read as *holding the top* selects the reception draw's upper tail.
- **Long-runners live below the head, after the peak.** Under every hold arm, a top-10 artist LP keeps 0-1% of its best
  week by chart week 30 and falls below the chart bottom (2.2-2.5k a week) around week 27. Billboard's evergreens
  (Mathis, the Kingston Trio, Mitch Miller) sat at #20-50 for a year. The past-peak tail gives them that zone and leaves
  reigns alone: `hlq98` puts No. 1 count and reign length on Billboard (9 / 10.5; 13 / 9.5). Artist long-runners rise from 0
  to 10.5 titles and 5.4% of 1961 slots. On the RIAA scorer the 300k+ share is unchanged (2.95 → 2.9%) and 1M+ rises
  0.45 → 0.73%, past RIAA's 0.44%, which the control matched: the tail turns a few 600k-1M hits into million-sellers. The Big-5 singles share is flat (19.8 against 20.5), so the §61 career channel does
  not pick this up. Store LPs gain 4.9M.
- **Cost: the tail amplifies whatever leads the LP head.** The 1961 head gap rises 39 → 50. Seed 2002's long-runners are 11
  TradPop titles of 15 (TradPop 46 points in its 1961 top 50 against 22), because the year-end top 50 sums points and the
  tail adds points to old hits. TradPop already swings 12-34 by seed (§65). Seed 1001's six long-runners are mixed: two
  TradPop, two Easy Listening, a doo-wop hits package and a rock and roll act. `hlq96` holds the gap (35) but buys only a
  seventh of the long-runner slots (0.8%).
- **Still far from Billboard's long-runner share.** 5.4% against ~28% of a 65-title listing. Part of that is the window:
  a 104-week run has no artist LPs older than 1960, while Billboard's 1960 evergreens dated to 1956-59 (the opening
  catalogue seeds soundtracks only, §13).

**Status:** the hold mode (§73 as written) is rejected. It extends monster reigns, and a cap does not prevent that.
`hlq98` (`--lp-hit-life-tail-top=10 --lp-hit-life-tail=0.98`) is the stage B candidate: No. 1s, reigns and long-runners move
toward history (1M+ overshoots RIAA), and the Big-5 share and closures hold. Not adopted on two seeds, because the 1961
head gap worsens through seed 2002's TradPop. Next: seeds 3003/4004 (`audnet` controls exist), then decide whether the
TradPop concentration is seed noise or the tail. If it is the tail, the lever is an opening artist-LP catalogue or a
genre-neutral gate, not a lower D. Stage A + B together still cannot make a Beatles-scale LP without stage C.

## 78. Long-runner artist LPs: the rack catalogue, renewal and catalogue heat (2026-11-18; seeds 1001/2002, 104 weeks)

### Target: Billboard's 1963 merged Top LPs, top 50

§13's 38% (25 of 65 listings, January 1960) counts every Essential Inventory title as if all 25 would rank in a merged
top 65; the model's chart is 40-50 deep. The August and November 1963 issues (`SimLogs/album-promo-research/`) print the
merged 150 with weeks on chart. In their top 50:

| Top 50 slots held by 40+ week titles | 24 Aug 1963 | 9 Nov 1963 |
|---|---|---|
| Artist LPs | **8 (16%)**: Mathis *Johnny's Greatest Hits* (277 wk), Baez ×2, PP&M, Andy Williams, Bennett, Allan Sherman, *Jazz Samba* | **10 (20%)**: PP&M ×2, Baez ×3, Bennett, Williams, Orbison and Kingston Trio hits packages, Nat Cole |
| Cast / film | 5 (10%) | 4 (8%) |
| All | 13 (26%) | 14 (28%) |

Target: **artist LPs of 40+ weeks hold about 16-20% of top-50 slots**, sitting at #19-50; greatest-hits packages, folk and
vocal standards. Measured over the run's last 26 weeks (`score_lp_hit_life.py` now prints it), because a 1960 release
needs most of a year to reach 40 weeks.

### Why the model's hits leave: the rack catalogue tier churns

With racks in the chart (`--album-chart-racks=1`), an LP's chart points are store + rack units, and the #50 bar is about
6k (the album chart CSV's `unitsThisWeek` is store only; the §77 "chart bottom 2.2-2.5k" was store units and is wrong).
Top-10 artist LPs in `audnet` sell 5.5k a week on racks at chart week 20 and 0 by week 30. Racks have no time decay. The
racks drop the title: from week ~16 it moves from the hit tier to the catalogue tier (`RackJobberChannel.AssignTiers`), and
the catalogue tier (50 slots a region) refills every week from any past-peak LP by this week's store units. It takes 7.5
new titles a region-week, median tenure 7 weeks, median age 13; half its entrants were never rack hits.

### Switches (checks pass)

- `--rack-proven-catalogue=K:N` (`RackJobberChannel`): the first K catalogue slots go to past-peak titles that peaked at N
  or better on the album chart, same store-unit rank and hysteresis; the rest fill as before. Source: Billboard printed
  Essential Inventory in 1960 as a stocking guide to proven catalogue.
- `--lp-hit-life-renewal=R` (+ `-peak=N`, default 10; `-racks-only`) (`LpHitLife`): `--soundtrack-hit-tail`'s renewal for
  artist LPs. After week 12, an LP that peaked at N or better reads its store and rack penetration divided by 1 + R per week.
- `--album-heat-action-weeks=W` (`ArtistHeatAttribution`): under `--single-artist-heat-current-albums`, an LP with more
  than W chart weeks adds no current-position heat to its act's singles. The authored heat and peak credits are unchanged.
  Source: Billboard's 1960 split of Action Albums (≤39 weeks) from Essential Inventory.

### Screen (control `audnet`; seeds 1001/2002)

| | `audnet` | `hlq98` (§77) | `rpc50` | `rpcr05` | **`rpcr05h`** | `rpcr03h` | `rpck05` | Target |
|---|---|---|---|---|---|---|---|---|
| Proven catalogue / renewal / heat cutoff | – | tail 0.98 | 50:10 | 50:10, R .05 | 50:10, R .05, W 39 | 50:10, R .03, W 39 | 50:10, R .05 racks, W 39 | |
| **Artist 40+ wk share, last 26 wk** | 0.1 | 5.9 | 3.7 | 18.2 | **22.0** | 11.5 | 19.6 | 16-20 |
| All 40+ wk share, last 26 wk | 18.7 | 24.0 | 23.1 | 37.7 | 40.2 | 32.9 | 39.3 | 26-28 |
| Artist LPs reaching 40 chart weeks | 0.5 | 10.5 | 10.5 | 22 | 22.5 | 19 | 19.5 | |
| No. 1s 1960 / 1961 | 7.5 / 6.5 | 9 / 10.5 | 7.5 / 13 | 8 / 10.5 | 8 / 9.5 | 8 / 9.5 | 10.5 / 9.5 | 10 / 12 |
| Longest No. 1 1960 / 1961 | 12.5 / 13 | 13 / 9.5 | 12.5 / 9 | 14 / 11 | 14 / 12 | 13.5 / 13 | 12 / 15.5 | 13 / 10+ |
| **Big-5 Top 10 singles 1961** (1001 / 2002) | 20.5 (18.5 / 22.5) | 19.8 | 29.7 (32.9 / 26.5) | **35.4** (39.8 / 31.0) | **20.2** (25.0 / 15.4) | 28.3 (29.0 / 27.5) | 25.0 (24.4 / 25.6) | 20.6 |
| 1M+ % of 1960 releases (RIAA scorer) | 0.45 | 0.73 | 0.51 | 1.04 | **1.13** | 0.93 | 1.18 | 0.44 |
| 300k+ % | 2.95 | 2.9 | 2.95 | 2.8 | 2.95 | 2.9 | 2.85 | 1.2 |
| Head gap 1960 / 1961 | 34 / 39 | 37 / 50 | 45 / 44 | 39 / 44 | 37 / 41 | 42 / 38 | 39 / 44 | 0 |
| Store LPs 1961 (M) | 54.2 | 59.1 | 57.4 | 54.8 | 56.8 | 58.2 | 56.4 | 68 |
| Labels defunct (1001; 2002) | 96; 104 | 104; 101 | 116; 108 | 100; 94 | 94; 89 | 101; 88 | 110; 90 | |

(`rpcq` = `rpc50` + `hlq98`: 14.7% long-runners, Big-5 23.3, 1M+ 0.79%. `rpck10` = racks-only R 0.10: 25.2%, Big-5 24.9,
1M+ 1.36%. `rpc25` (25:10) moved nothing, because half the catalogue was already ever-hit titles.)

- **The proven catalogue keeps hits on the rack, but it is not enough on its own.** Under `rpc50`, top-10 artist LPs sell
  4.1-4.8k a week on racks from chart week 26 to week 52. About 85 proven titles then compete for the lower half of a
  50-slot chart, and rack exhaustion ranks the newest first, so few pass 40 weeks (3.7%).
- **Renewal makes the biggest hits the evergreens.** With R 0.05 a proven title's audience renews, and the best-selling
  hits hold the lower chart: 18-22% of slots over the last 26 weeks. They sit at median #32-36, as in 1963. In `rpcr05h`
  they are 22-25 titles a seed, 59-64% Big-5: TradPop 8-16, Easy Listening 4-6, few folk (Folk is already short at the
  head, §65), and greatest-hits and live LPs 4-6.
- **Long LP runs leak into singles through current-album heat.** `rpcr05` lifts the 1961 Big-5 share to 35.4 on both seeds
  (newcomers 10 → 20 slot points). The candidate's `--single-artist-heat-current-albums` lets a 45-week LP at #35 heat its
  act's singles like a new one. The 39-week cutoff (`rpcr05h`) returns Big-5 to 20.2 and keeps the long-runners (22.0%).
  Two seeds are noisy here: `rpcr03h` and `rpck05` carry the same cutoff and read 25-28, so the guard needs seeds 3003/4004.
- **Cost: million-sellers.** Every renewal arm puts 1M+ at 0.9-1.4% of 1960 releases (RIAA 0.44%, control on target). The
  excess comes from the racks: racks-only renewal does not lower it, and the long-runners sell ~5k a week on racks for a
  year. The RIAA window (12-24 months, manufacturer sales including clubs) may undercount multi-year evergreens, but the
  model is 2.5× over within two years. 300k+ is unchanged.

**Status:** `rpcr05h` (`--rack-proven-catalogue=50:10 --lp-hit-life-renewal=0.05 --album-heat-action-weeks=39`) is the
long-runner candidate. On two seeds it puts artist long-runners on 1963's share, holds No. 1s, reigns and the Big-5 share,
and slightly lowers closures; it overshoots 1M+ 2.5×. Not adopted. Next: seeds 3003/4004 (`audnet` controls exist) for the
Big-5 guard and the 1M+ count. `hlq98` (§77) is superseded: it buys a quarter of the long-runners for a larger head-gap cost.

## 79. The 1M+ overshoot is catalogue racks, and the scorer counted the opening soundtracks (2026-11-18; seeds 1001/2002, 104 weeks)

Question: is `rpcr05h`'s 1M+ excess (§78) rack sales? No new runs; the §77-78 runs re-read.

**Scorer bug, fixed.** `--opening-soundtrack-catalogue=8` mints its 1956-59 titles in the first live week, which the
census logs as week 2. The week-1 filter that `analyze_lp_riaa_1965.py` uses to drop the opening catalogue (§67) let them
in as 1960 releases: 6 a seed, every one 1M+ (store + rack 1.9-3.3M). They were the control's whole 1M+ count. The
scorer now drops a soundtrack whose age at first sighting exceeds its weeks since launch by more than 8.
`score_lp_hit_life.py` counts artist LPs only and is unaffected.

| Mean of seeds 1001/2002 | `audnet` | `hlq98` | `rpc50` | `rpcr05` | `rpcr05h` | `rpck05` | RIAA 1963 |
|---|---|---|---|---|---|---|---|
| 1M+ % as scored in §77-78 | 0.45 | 0.73 | 0.51 | 1.04 | 1.13 | 1.18 | 0.44 |
| **1M+ %, opening catalogue out** | **0.09** | **0.37** | 0.15 | 0.69 | **0.77** | 0.82 | 0.44 |
| 1M+ artist / new soundtrack (titles, both seeds) | 0 / 3 | 10 / 2 | 2 / 3 | 20 / 3 | 24 / 2 | 24 / 3 | |
| 300k+ % | 2.61 | 2.56 | 2.61 | 2.48 | 2.59 | 2.49 | 1.31 |
| 1M+ as a share of 300k+ | 3% | 14% | 6% | 28% | 30% | 33% | 33% |
| 1M+ % on store + hit-tier racks only | 0.06 | 0.25 | 0.09 | 0.12 | 0.09 | 0.12 | |
| 300k+ % on store + hit-tier racks only | 1.57 | 1.59 | 1.74 | 1.64 | 1.91 | 1.88 | |
| Artist 300k+ units: all racks / catalogue tier (%) | 51 / 16 | 50 / 21 | 65 / 39 | 69 / 47 | 69 / 46 | 69 / 46 | |

Long-runners and the rack catalogue tier (`audnet` → `rpcr05h`, seeds 1001; 2002):

| | `audnet` | `rpcr05h` |
|---|---|---|
| #35, weeks 79-104: store / rack k a week | 3.5 / 7.3; 3.2 / 6.4 | 0.9 / 11.3; 0.1 / 12.1 |
| #50: store / rack k a week | 2.3 / 6.4; 2.6 / 6.5 | 0.9 / 9.1; 0.2 / 8.5 |
| Artist LPs past 40 chart weeks, weeks 79-104: median position; store / rack k a week; rack share | – | #36; 0.2 / 11.4; 95% · #33; 0.1 / 12.5; 99% |
| Titles under 20 chart weeks at #25-45: store / rack k a week | 3.2 / 7.7; 3.5 / 7.2 | 4.7 / 7.2; 4.6 / 8.3 |
| 1961 rack units, hit / catalogue tier (M) | 21.7 / 19.2; 21.7 / 20.2 | 21.4 / 25.1; 20.6 / 26.9 |
| 1961 catalogue-tier titles; top-20 share of its units | 522, 33%; 532, 32% | 222, 45%; 227, 48% |

- **Yes, the excess is rack sales: the catalogue tier.** In no arm does an artist LP reach 1M on store and hit-tier rack
  units (at most 0.25%). Under `rpcr05h` the catalogue tier carries 46% of artist 300k+ units (control 16%). The
  proven catalogue narrows that tier from ~525 to ~225 titles and grows it 6-7M in 1961 (+31-33%), so each proven title
  sells ~12k a week on racks for a year.
- **The overshoot is 1.75×, not 2.5×, and the control was never on target.** Without the opening catalogue the control
  makes 0.09% 1M+ (no artist LP at all), a fifth of RIAA. `hlq98` (§77) lands on RIAA's 0.44 (0.37).
- **The 1M+ excess is the 300k+ excess carried up.** 300k+ is 2× RIAA in every arm, the control included, and catalogue
  racks cause that too: on store and hit-tier racks it is 1.6-1.9%. Under the renewal arms 28-33% of 300k+ titles pass
  1M, which is RIAA's 33% (7 of 21). With 300k+ on RIAA, `rpcr05h`'s ratio would give 0.39% 1M+.
- **A rack-only long-runner is not historical.** At #25-45 a young title sells ~38% in stores; a 40-week artist LP sells
  1-5%. Store demand dies on the 0.93 clock (decay multiplier 0.017-0.025 by chart week 40), while rack demand has no time
  term (pool × exposure × conversion × exhaustion) and the renewal holds rack exhaustion near 0.3. At the 1962 NARM
  convention RCA's George Marek said "a national best seller becomes generally a rack best seller". The racks' own best
  sellers were economy-priced lines (101 Strings, Living Strings), and Capitol's Glenn Wallichs pressed the rack
  jobbers on the manufacturers' stake in catalogue
  (`SimLogs/album-promo-research/billboard-1962-04-21.txt`). Marek's 1961 national best sellers include long-runners
  (*Sing Along With Mitch*, *Portrait of Johnny*). Racks followed store best sellers; no source gives a per-title
  channel split.
- **Fixing the split alone does not move 1M+.** Chart points are store + rack, so a title that holds #33-36 sells
  ~12k a week either way, and 60 weeks there is ~700k. Units fall only if racks stop carrying titles that stores have
  dropped: `--rack-store-standing-bound=K` already does this (GameyBehaviorAudit §2.10: K 2 works at 104 weeks; at 208
  it fails on soundtracks because it reads store intent). With the bound, a long-runner needs a store tail to stay on
  the chart, which is `hlq98`'s past-peak tail.

**Status:** scorer fixed. `rpcr05h` overshoots 1M+ 1.75×. The cause is the proven catalogue rack tier, which has no store
standing behind it. Next screen: `rpcr05h` + `--lp-hit-life-tail-top=10 --lp-hit-life-tail=0.98` +
`--rack-store-standing-bound=K` (K 2 and 3). Read the long-runner share, the store share of long-runners, 300k+ / 1M+
(fixed scorer), the Big-5 share and soundtrack racks, which the bound also cuts.

## 80. Screen: store-led long-runners (tail + rack store-standing bound) fail; 1M+ is set by long-runner share (2026-11-18; seeds 1001/2002, 104 weeks)

**Arms** (on `rpcr05h`, §78): `rptb2` / `rptb3` add `--lp-hit-life-tail-top=10 --lp-hit-life-tail=0.98` (§77's store tail)
and `--rack-store-standing-bound=2` / `=3`, so racks follow store sales (§79). Built on the current assembly (3FBE…; `rpcr05h`
ran on 4398…, which lacks only the racks-only renewal switch). `rpcq` (§78 footnote: `rpc50` + `hlq98`, no renewal, no heat
cutoff) is scored in full alongside them. RIAA rows use the §79-fixed scorer.

| Mean of seeds 1001/2002 | `audnet` | `rpcr05h` | `rptb2` | `rptb3` | **`rpcq`** | Target |
|---|---|---|---|---|---|---|
| Artist 40+ wk share, last 26 wk | 0.1 | 22.0 | 27.4 | 32.9 | **14.7** | 16-20 |
| … of it 1960 debuts (opening / 1960 / 1961 debuts) | – | 2.4 / 16.4 / 3.3 | | | 0 / **11.0** / 3.8 | 11-15 from 1960 (below) |
| Long-runner store / rack k a week; rack share | – | 0.2 / 12.0; 97% | 3.6 / 9.4; 71% | 3.0 / 10.7; 76% | 2.0 / 7.0; 77% | store-led |
| Long-runner median position | – | 33-36 | 33-34 | 33 | 42 | #19-50 |
| No. 1s 1960 / 1961 | 7.5 / 6.5 | 8 / 9.5 | 7.5 / 8 | 10 / 11 | 7.5 / 9.5 | 10 / 12 |
| Longest No. 1 1960 / 1961 | 12.5 / 13 | 14 / 12 | 15 / 13.5 | 12.5 / 13 | 12.5 / 11 | 13 / 10+ |
| **1M+ %** (fixed scorer) | 0.09 | 0.77 | 0.97 | 1.05 | **0.42** | 0.44 |
| 300k+ % | 2.61 | 2.59 | 2.09 | 2.09 | 2.28 | 1.31 |
| 1M+ / 300k+ % on store + hit-tier racks only | 0.06 / 1.57 | 0.09 / 1.91 | 0.35 / 1.37 | 0.44 / 1.63 | 0.15 / 1.65 | |
| 1961 rack catalogue tier (M) | 19.7 | 26.0 | 18.3 | 22.2 | 18.3 | |
| 1961 stores / racks / soundtrack racks (M) | 54.2 / 41.4 / 12.3 | 56.8 / 47.0 / 9.1 | 62.0 / 38.7 / 6.9 | 60.2 / 42.5 / 8.1 | 59.8 / 39.2 / 9.5 | 68 / 36-42 / |
| **Big-5 Top 10 singles 1961** (1001 / 2002) | 20.5 (18.5 / 22.5) | 20.2 (25.0 / 15.4) | **32.8** (38.3 / 27.3) | **34.4** (32.3 / 36.5) | 23.3 (27.5 / 19.0) | 20.6 |
| … 1960 hit acts / no 1960 Top 10 | | 10.3 / 9.9 | 16.9 / 15.9 | 13.0 / 21.4 | | 16.4 / 4.2 |
| Head gap 1960 / 1961 | 34 / 39 | 37 / 41 | 30 / 45 | 42 / 49 | 43 / 35 | 0 |
| Labels defunct (1001; 2002) | 96; 104 | 94; 89 | 111; 102 | 97; 114 | 94; 102 | |

- **The bound and tail make long-runners store-led and sell more.** The store tail lifts their store decay multiplier from
  0.02 to 0.23, so their rack share drops 97 → 71-76%. The bound barely touches them: it caps only the racks' own demand,
  store buyers who switch to the rack are added after it (`ClearWeek`), and their racks stay at 9-11k a week. It cuts the
  rest of the catalogue tier (26 → 18-22M in 1961). Long-runners rise to 27-33% of
  slots and 1M+ to 0.97-1.05%. 300k+ falls to 2.09%, because borderline hits lose catalogue rack sales.
- **The Big-5 guard breaks on all four seed-runs** (27-38), mostly through newcomers (9.9 → 16-21 slot points; history
  4.2). Longer LP runs heat singles before the 39-week cutoff, the §78 channel.
- **1M+ is set by the long-runner share, whatever the mechanism.** Over all 13 arms of §77-80 (tail, hold, renewal,
  racks-only, proven catalogue, bound): 1M+ % ≈ 0.12 + 0.031 × artist 40+ share, r = 0.975. RIAA's 0.44 falls at a 10.4%
  share. A long-runner holds ~#33-42 for most of a year at 7-14k a week, and that is the million.
- **Opening catalogue, corrected.** §77 said the run has no artist LPs older than 1960. The prewarm (`CompetitorManager`,
  "PREWARM SEEDING") does seed artist LPs, released 1-20 weeks before the start, so the week-1 chart is 40 artist LPs with a
  median 9-10 chart weeks and at most 14-23. Six a seed still chart in the last 26 weeks of `rpcr05h`. Nothing older is seeded: there is no Essential
  Inventory tier (Billboard January 1960: ~25 of 65 listings).
- **The 16-20% target needs a cohort split.** By release year, Billboard's artist 40+ week titles in the top 50 were:
  24 Aug 1963: 6 of 8 from 1962, one 1961, Mathis's 1958 *Greatest Hits*. 9 Nov 1963: 7 of 10 from 1962, one each from
  1960, 1961 and 1963 (debut weeks from the weeks-on-chart column, `billboard-1963-*.txt`). So 70-75% of those slots come
  from the previous year's releases and 20-25% from older ones. The run's last 26 weeks correspond to the second half of
  1961, so the 1960 releases are the previous year and should hold **11-15%** of top-50 slots. `rpcq`'s 1960 releases hold
  11.0% on both seeds and its 1M+ is 0.42%: both on target. `rpcr05h`'s 16.4% asks one release year to fill the older
  catalogue's slots too, and the million-sellers follow from that. The rest of the 16-20% gap is the missing pre-1959 tier,
  not the 1960 releases.

**Status:** `rptb2` / `rptb3` rejected (Big-5 33-34, 1M+ up). `rpcq` (`--rack-proven-catalogue=50:10
--lp-hit-life-tail-top=10 --lp-hit-life-tail=0.98`) replaces `rpcr05h` as the long-runner candidate: 1960 releases on the
1963 share for one cohort, 1M+ on RIAA, long-runners at median #42, No. 1s and reigns on history, closures at the
control. Costs: Big-5 23.3 (27.5 / 19.0) without the heat cutoff, head gap 43 / 35, long-runners still 77% rack. Next:
`rpcq` + `--album-heat-action-weeks=39` on seeds 1001-4004 (`audnet` controls exist) for the Big-5 guard. Separately,
the author decides whether to seed a pre-1960 evergreen artist-LP tier for the older 20-25% of long-runner slots.

## 81. An opening evergreen artist-LP tier: built, but it cannot chart without audience renewal (2026-11-19; seeds 1001/2002, 104 weeks)

**Source.** Billboard 4 Jan 1960, Essential Inventory (mono top 25, titles 40+ weeks on the chart;
`SimLogs/album-promo-research/billboard-1960-01-04.txt`): 8 cast/film albums, Van Cliburn, and **16 artist LPs from
1956-59**: TradPop 7 (Mathis ×3 incl. *Johnny's Greatest Hits*, Mitch Miller ×2, Sinatra ×2), Easy Listening 3
(Mantovani ×2, Mancini's *Peter Gunn*), Jazz 2 (Dakota Staton, Ahmad Jamal), rock and roll 2 (*Elvis' Golden Records*,
Duane Eddy), Folk 1 (Kingston Trio, *From the Hungry i*), Country 1 (Tennessee Ernie Ford, *Hymns*), 40-130 weeks on
the chart (median ~51). 14 of the 16 are on Major or Major-distributed labels.

**Switch** `--opening-evergreen-lps=N` (`OpeningEvergreenLps`, `CompetitorManager.SeedOpeningEvergreenLps`). After the
prewarm, it mints the first N of those 16 in rank order. Each goes to the strongest unused roster act of the slot's genre
(reputation × base quality × tier weight: Major 1, MidTier 0.3, other 0.1). Each gets:
- a keyed top-10 peak and its printed chart weeks; release 2 weeks before the chart debut; sales peak at week 6;
- 1.5× each region's buyer pool already sold, so exhaustion sits at its 0.15 floor;
- the decay clock the configured `--lp-hit-life-tail` would have given it: 6 weeks held in the top 10, then D a week.
  Without the tail its store sales die at once, as the opening soundtracks do without `--soundtrack-hit-tail`;
- under `--lp-reception-sigma`, a reception z drawn from the standard normal above 0.75 (median 1.2, keyed). `rpcq`'s own
  40+ week artist LPs have a median z of 1.0 / 1.4 (multiplier 2.6 / 3.3; all LPs 0.7, an ordinary draw's median
  multiplier 0.37).

Checks pass. Off is inert: `evoff-1001-sc` (27 weeks, new build) matches `rptb2-1001-sc` in 74 weekly streams, and only
close-of-run rows differ (format-mix's 2 summary rows, retirement's week-27 status snapshot). Evergreens log one
`OPENING_EVERGREEN_LP` line each. They are census week 0, so the RIAA cohort excludes them.

**Screen** (on `rpcq`): `rpcqe8` / `rpcqe16` (first build: ordinary reception, tier weights 1 / 0.6 / 0.3) and
`rpcqf8` / `rpcqf16` (hit reception, Major-weighted).

| Mean of seeds 1001/2002 | `rpcq` | `rpcqe16` | `rpcqf8` | `rpcqf16` | Target |
|---|---|---|---|---|---|
| Evergreen chart weeks (all titles, both seeds) | – | 1 | 5 | 14 (4 titles, seed 2002, best #30, gone by week 19) | most chart in 1960 |
| Evergreen weekly units, 1960 (store / rack, per title) | – | 0.4 / 2.3k | | 1.2-1.9 / 2.5-3.4k | above the #40 bar (~8k) |
| Artist 40+ share, last 26 wk (evergreen / prewarm / 1960 / 1961) | 14.7 (0 / 0 / 11.0 / 3.8) | 15.2 | 14.7 | 18.7 (0 / 1.2 / 15.9 / 1.6) | 16-20, older 3-5 |
| 1M+ % | 0.42 | 0.58 | 0.49 | 0.40 | 0.44 |
| Big-5 Top 10 singles 1961 (1001 / 2002) | 23.3 (27.5 / 19.0) | 24.1 | 29.8 (38.5 / 21.2) | 26.8 (35.2 / 18.5) | 20.6 |
| Store LPs 1960 (M) | 36.4 | 40.3 | 38.8 | 41.7 | |
| Labels defunct (1001; 2002) | 94; 102 | 104; 103 | 108; 114 | 112; 102 | |

- **Evergreens sell 4-6k a week and miss the chart.** With hit-level reception the best sell 2.8k in stores and 3.4k
  on racks (week 3), most 1.5 + 3k. The 1960 bar (40 deep, racks counted) is about 8k. Racks place them: they sit in
  the proven catalogue tier from week 1.
- **The binding term is the spent pool.** At the exhaustion floor and the tail's decay clock (0.06-0.37 at the start),
  a 1956-59 hit sells like the model's own 1961 long-runners at #40-45 (2 / 7k), and 1960 racks are 15% smaller. Billboard's
  Essential Inventory titles still sold like hits in 1960: their audience kept renewing as new households bought their
  first LPs. The model gives that renewal only to soundtracks (`--soundtrack-hit-tail`). That is why the 8 opening
  soundtracks chart and the evergreens do not.
- **The other rows move within seed noise.** Seeding reshuffles the world's random stream. Big-5 on seed 1001 spans 25-38
  across arms that differ only in uncharted evergreens. Store LPs in 1960 gain 2-5M (the evergreens' own sales, about 3-4M
  a year at N 16). Closures rise 0-18 a seed, which is unresolved.

**Status:** built, inert when off, not adopted. Under `rpcq`'s rules (tail, no renewal) a pre-1960 hit cannot hold a
1960 chart slot. The evergreens need the audience renewal that `--lp-hit-life-renewal` gives (§78). Renewal overshot 1M+
in §78-80 because the 1960 releases then filled every long-runner slot. With an older tier taking 20-25% of those slots,
a lower R may fit both: R 0.03 left the 1960 releases at 8.4% (§80, `rpcr03h`). Next screen: `rpcq` +
`--lp-hit-life-renewal=0.03` + `--album-heat-action-weeks=39` + `--opening-evergreen-lps=16`, with and without the
evergreens. Read: evergreen slots in 1960 and late 1961, the 1960 releases' share (11-15), 1M+, Big-5.

## 82. Screen: renewal brings the evergreens onto the chart, but tail + renewal is too sticky (2026-11-19; seeds 1001/2002, 104 weeks)

**Arms** (current build, after commit `0efbad6`): `rpqrh` = `rpcq` + `--lp-hit-life-renewal=0.03
--album-heat-action-weeks=39`; `rpqrhe` = `rpqrh` + `--opening-evergreen-lps=16`. Both ran on the same build, so `rpqrh`
is the evergreens' control. `rpcq` ran on an older build (59CA…), so read that column as approximate.

| Mean of seeds 1001/2002 | `rpcq` | `rpqrh` | **`rpqrhe`** | Target |
|---|---|---|---|---|
| Evergreen share of top-50 slots: 1960 Q1 / 1960 H2 / 1961 H1 / last 26 wk | – | – | **10.1 / 12.2 / 11.0 / 7.7** | charting; older tier 3-5 late 1961 |
| Evergreens charting (of 16; 1001 / 2002); 40+ run weeks | – | – | 11 / 14; 5 / 7 | |
| Artist 40+ share, whole of 1960 | 1.9 | 7.6 | 19.9 | ~15-20 (Jan 1960 Essential Inventory) |
| Artist 40+ share, last 26 wk | 14.7 | 26.9 | 35.0 | 16-20 |
| … evergreen / prewarm / 1960 / 1961 releases | 0 / 0 / 11.0 / 3.8 | 0 / 1.4 / 23.3 / 2.3 | 7.7 / 4.9 / 19.9 / 2.6 | older 3-5, 1960 11-15 |
| Long-runner median position; store / rack k a week; rack share | 42; 2.0 / 7.0; 77% | 34; 2.4 / 10.0; 79% | 31.5; 1.8 / 10.5; 84% | #19-50 |
| No. 1s 1960 / 1961 | 7.5 / 9.5 | 8 / 11.5 | 6 / 9.5 | 10 / 12 |
| Longest No. 1 1960 / 1961 | 12.5 / 11 | 12.5 / 9.5 | 16 / 11 | 13 / 10+ |
| 1M+ % (fixed scorer) | 0.42 | 0.86 | 0.91 | 0.44 |
| 300k+ % | 2.28 | 2.53 | 2.10 | 1.31 |
| Big-5 Top 10 singles 1961 (1001 / 2002) | 23.3 (27.5 / 19.0) | 25.7 (26.0 / 25.4) | 22.2 (24.8 / 19.6) | 20.6 |
| Head gap 1960 / 1961 | 43 / 35 | 36 / 49 | 39 / 46 | 0 |
| 1961 stores / racks (M) | 59.8 / 39.2 | 59.1 / 44.4 | 57.2 / 45.7 | 68 / 36-42 |
| Labels defunct (1001; 2002) | 94; 102 | 99; 102 | 107; 106 | |

- **Renewal is what the evergreens needed.** They now chart from week 1 and hold 10-12% of slots through 1960 and early
  1961, falling to 7.7% by late 1961. Twelve of the 32 seeded titles stay 40+ weeks in the run; three on seed 2002 chart all
  104 weeks, like Mathis's *Johnny's Greatest Hits*. Artist long-runners hold 19.9% of 1960's slots against 1.9-7.6%
  without the evergreens.
- **Tail and renewal compound.** R 0.03 without the tail (`rpcr03h`, §78) left the 1960 releases 8.4% of late-1961
  slots. With the tail they take 23.3%, and 1M+ doubles (0.86%). The tail keeps the store decay up (0.3 against 0.02) and
  renewal keeps exhaustion off its floor, so a hit stays well above the #50 bar.
- **The evergreens displace a little of the 1960 releases, not enough.** The 1960 releases' share falls 23.3 → 19.9 and
  1M+ is flat (0.86 → 0.91, inside noise). The older tier (evergreen + prewarm) is 36% of the late-1961 long-runner
  slots, above 1963's 20-25%, and the total is 35% against 16-20.
- **1M+ follows the long-runner share as in §80.** The fit (0.12 + 0.031 × share) gives 0.95 for `rpqrh`'s 26.9;
  it measures 0.86.
- **Big-5 holds with the heat cutoff** (22.2-25.7, inside the seed range). Store LPs ease 1-2M as racks gain 5-6M.

**Status:** `rpqrhe` shows the evergreen tier works once artist LPs renew. Tail + renewal at R 0.03 is about twice as
sticky as the targets: the 1960 releases need 11-15% (now 20-23) and the total 16-20% (now 35). Not adopted. Next: halve
the strength with evergreens on. Arms: R 0.015 with the tail at 0.98, and R 0.03 with the tail at 0.96. Read the same rows;
1M+ should land near 0.44 when the 1960 releases hold about 11%.

## 83. Screen: half-strength tail + renewal with evergreens; the shape fits, 1M+ does not (2026-11-19; seeds 1001/2002, 104 weeks)

**Arms**, all `rpcq` + `--album-heat-action-weeks=39 --opening-evergreen-lps=16`: `rq015e` (R 0.015, tail 0.98) and
`rq96e` (R 0.03, tail 0.96). Each was built from a reference request with the values replaced in place: the argument
parser reads the first copy of a flag, so a value appended through `-Extra` is ignored when the reference already
carries the flag. The §77-82 screens appended only flags their references lacked, so they stand.

| Mean of seeds 1001/2002 | `rpqrhe` (§82) | `rq015e` | `rq96e` | Target |
|---|---|---|---|---|
| Evergreen share of slots: 1960 Q1 / 1960 H2 / 1961 H1 / last 26 wk | 10.1 / 12.2 / 11.0 / 7.7 | 3.6 / 3.1 / 3.3 / 0.2 | 2.4 / 5.0 / 11.0 / 3.7 | charting; older 3-5 late 1961 |
| Artist 40+ share, last 26 wk | 35.0 | 21.2 | 21.4 | 16-20 |
| … evergreen / prewarm / 1960 / 1961 releases | 7.7 / 4.9 / 19.9 / 2.6 | 0.2 / 1.6 / 17.0 / 2.5 | 3.7 / 2.9 / **13.4** / 1.5 | older 3-5, 1960 11-15 |
| Long-runner median position; store / rack k a week; rack share | 31.5; 1.8 / 10.5; 84% | 37; 2.0 / 8.8; 80% | 35.5; 0.6 / 11.1; 93% | #19-50, store-led |
| No. 1s 1960 / 1961; longest | 6 / 9.5; 16 / 11 | 7 / 8; 14.5 / 13.5 | 6.5 / 10; 15.5 / 9.5 | 10 / 12; 13 / 10+ |
| 1M+ % | 0.91 | 0.80 | 0.74 | 0.44 |
| 300k+ % | 2.10 | 2.68 | 2.36 | 1.31 |
| Big-5 Top 10 singles 1961 (1001 / 2002) | 22.2 (24.8 / 19.6) | 27.3 (26.5 / 28.1) | 29.3 (36.7 / 21.9) | 20.6 |
| Head gap 1960 / 1961 | 39 / 46 | 43 / 48 | 42 / 45 | 0 |
| Labels defunct (1001; 2002) | 107; 106 | 117; 106 | 119; 103 | |

- **Halving brings the total to 21%**, near 1963's 16-20. `rq96e` also puts the 1960 releases inside 11-15 (13.4) and
  keeps an older tier of 6.6% (evergreen + prewarm, 31% of the long-runner slots against 1963's 20-25%).
- **1M+ stays 1.7× RIAA** (0.74-0.80). At a 21% share the §80 fit predicts 0.78. In `rq96e` the 1960 releases hold the
  §80 candidate's share (13.4 against `rpcq`'s 11.0) yet sell 1.8× its million-sellers: renewal lifts every top-10 LP's
  whole run, not just the long-runners.
- **`rq96e`'s long-runners are rack-only again** (93%, store 0.6k): the 0.96 tail lets store demand die, which §79
  found unhistorical. `rq015e` keeps them store-led (80%) but loses the evergreens by late 1961 (0.2%).
- **Renewal's divisor grows without limit** (1 + R × (age − 12)): an evergreen 200 weeks old renews 7×, a 1960 hit 2-3×.
  That is why `rq96e`'s evergreens rise again in 1961 (11%) as the tail decays them.
- **Big-5 and closures drift up on the evergreen arms.** Big-5 is 27-29 (seed 1001 spans 25-38 across arms). Closures
  on seed 1001 are 107-119 with evergreens against 94-99 without, which is unresolved and may be the evergreens'
  3-4M-a-year purse.

**Status:** neither arm is adopted. Tuning the tail and renewal can now fit the chart shape (`rq96e`), but not the shape,
1M+, a store-led long-runner and the Big-5 guard at once. The constant across §77-83 is 300k+ at about 2× RIAA in every
arm, which catalogue racks drive (§79). Each long-runner lever turns part of that excess into million-sellers.

## 84. The 2× 300k+ excess is mostly the 1960 cohort: Majors' ramp year, not a heavy tail (2026-11-19; read-only)

Question: why is 300k+ about 2× RIAA (1.31%) in every arm of §77-83, control included? No new runs. Scripts in the
session scratchpad wrap `analyze_lp_riaa_1965.py`'s cohort. "Scale" below is the model's sales at RIAA's exceedance share
divided by RIAA's threshold. For example, scale@20k is the model's 76.6th percentile over 20k, and 1.0 is on RIAA.

**1. In the 104-week arms the whole distribution is shifted, not the tail.** For 1960 releases to week 104 (`audnet`,
`rpcq`, seeds 1001/2002), the scale is 1.4-1.8 at every band from 2k to 100k. 300k+ is 1.3-1.4 (`audnet`) and 2.3-2.4
(`rpcq`, whose tail lever fattens the top). 300k+ titles carry 33-43% of cohort units against about 35% in RIAA (band
midpoints). The 20k+ / 50k+ / 100k+ shares are 30 / 16-17 / 8-10%; RIAA's are 23.4 / 11.2 / 5.1. The model releases
too few LPs for the units it sells:

| 1960 releases to wk 104, seeds 1001; 2002 | scale@4k | scale@20k | scale@100k | scale@300k | 300k+ % |
|---|---|---|---|---|---|
| `audnet` all tiers | 1.71; 1.72 | 1.52; 1.46 | 1.60; 1.79 | 1.26; 1.42 | 2.54; 2.68 |
| … Major + MidTier | 5.28; 4.76 | 4.84; 4.88 | 3.53; 3.78 | 1.77; 2.10 | 7.1; 7.4 |
| … Independent + Small + Boutique | 1.10; 1.03 | 0.92; 0.78 | 0.68; 0.65 | 0.44; 0.49 | 0.3; 0.2 |

The RIAA sample is four majors plus Laurie and Kapp, but the model's majors sit 4-5× above it at every band below
100k. Its small labels match RIAA below 20k, as found in §40.

**2. 1960 is the Majors' ramp year.** Majors launch 84-86 LPs a label in 1960 and 139-141 in 1961 (`audnet`). The
sourced range is 145-210 (`GameyBehaviorAudit.md` §2.18). The 104-week arms score a cohort that has about 60% of a mature
year's Major releases. The market is near full size by then.

**3. In RIAA's own year and window the excess roughly halves.** `acis260` (§65, four seeds, 260 weeks), each cohort
scored over the RIAA window (a year of releases plus the following year):

| `acis260`, mean of 4 seeds | n | Major n | units M | mean k | scale@20k | scale@100k | scale@300k | <4k % | 300k+ n / % | 1M+ n / % | Major 300k+ / <4k % |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1960 releases to wk 104 | 1,630 | 361 | 85.6 | 52.6 | 1.23 | 2.37 | 2.54 | 42.7 | 68.5 / 4.20 | 11.5 / 0.71 | 11.5 / 23.0 |
| 1961 releases to wk 156 | 2,303 | 646 | 113.7 | 49.4 | 1.21 | 2.34 | 2.45 | 41.6 | 91.8 / 3.99 | 14.3 / 0.62 | 8.9 / 24.0 |
| 1962 releases to wk 208 | 2,601 | 862 | 99.5 | 38.3 | 1.15 | 1.94 | 1.73 | 43.9 | 77.8 / 2.99 | 6.0 / 0.23 | 6.9 / 28.5 |
| **1963 releases to wk 260** | **2,751** | **939** | 92.8 | **33.7** | **1.09** | **1.52** | **1.51** | 45.6 | 65.8 / **2.39** | 7.0 / **0.26** | 4.9 / 32.4 |
| RIAA 1963 | 1,603 (six firms) | | | ~31 (midpoints) | 1.0 | 1.0 | 1.0 | 48.8 | 21 / 1.31 | 7 / 0.44 | |

- **The count of 300k+ titles hardly moves; the denominator grows.** Across cohorts 300k+ titles stay at 66-92 a seed
  while releases grow 1.7× (Majors 2.6×). Cohort units stay at 93-114M. The mean per release falls from 53k to 34k,
  close to RIAA's ~31k, and the scale at 4-20k reaches 1.1.
- **The 1963 cohort is still 1.5× at 100k+ and 1.8× at 300k+** (2.17-2.67%). 1M+ is under RIAA (0.18-0.38%).
  This residual is in the upper tail, unlike the 1960 shift. `acis` predates the LP chain (§76) and the long-runner
  arms, which in 1960 have 300k+ of 2.1-2.7% against `acis`'s 4.2. If that ratio carries over, `audnet` / `rpcq`
  would give about 1.3-1.6% on the 1963 cohort. That needs a 260-week run to check.
- **Two basis questions, of opposite sign, are open.** RIAA may count mono and stereo editions separately (§2.18, the
  1,603 releases ≈ four majors × 150-200 titles × 2). If so, a title's sales split across two rows, so per title
  RIAA's 300k+ share is higher. RIAA counts manufacturer sales, which may include club pressings (26% of 1961 LP
  units, weighted to hits). The model's title counts exclude clubs, so on that basis RIAA's per-title store + rack
  share would be lower. Neither is resolved by the source (p. 862 only says mono and stereo are covered).

**Reading.** The 1960 cohort is the wrong yardstick for RIAA's 1963 survey. It is the Majors' ramp year, and its 2×
300k+ is mostly too few releases selling a full-size market. Scored in RIAA's year, `acis` is 1.5-1.8× on 100k+ and
300k+ and on or under RIAA for 1M+. §80's 1M+ fit (0.12 + 0.031 × long-runner share) and §79's "300k+ 2× in every
arm" are both 1960-cohort measurements; read them as ramp-year numbers. Long-runner levers should not be tuned to pull
the 1960 cohort's 300k+ down to 1.31.

**Next:** score `audnet` and `rpcq` at 260 weeks (seeds 1001/2002) on the 1963 cohort (`--launch=157-208 --last=260`)
and check that the residual 300k+ excess is under 1.5×. Until then, report 300k+ / 1M+ on the 1960 cohort as relative
to the control, not to RIAA.

## 85. How labels start 1960 cold: an audit of the opening state (2026-11-19; read-only, `audnet` and `acis260` seeds 1001/2002)

Question (author): the 1960 Major LP shortfall (§84) is a weakness of the opening environment. What does the prewarm
set up, what starts empty, and can labels start with forecasts?

**What the opening does** (`ChartManager` init, `CompetitorManager.PopulateInitialRecords`):
1. Rosters from the frozen 3,000-artist launch allocation; opening careers (public histories) for the canopy acts.
2. About 500 opening records (`targetActiveRecords`), each 1-20 weeks old, spread by tier weight × roster size; the album
   share by genre seed affinity. `--opening-soundtrack-catalogue` and `--opening-evergreen-lps` add older LPs.
3. `PrewarmSimulation`: 8 weeks of chart simulation (`prewarmWeeks`) with the live genre market off. **No label
   decision runs in the prewarm.** No release is planned, no gate is used, no forecast observes, no staff review
   runs, no money moves through the company accounts. Records with no chart run and under 500 units are culled.
4. Opening follower stock from the opening careers' last five years of hits (`SeedOpeningFollowing`).
5. Learned state: the earned LP forecast is warm-started for opening Major and MidTier labels
   (`--earned-lp-forecast-warm-start=20`, in the control). Format memory and the single forecast start at
   their fitted priors with zero evidence.

**Where labels limp, by measured effect:**

| # | Cold start | Evidence (`audnet` 1001 unless noted) | Effect on 1960 |
|---|---|---|---|
| 1 | **Empty release pipeline.** The slate holds nothing at week 1, and every slot takes 8 weeks from first sighting to issue (median, every quarter). | Weeks 1-13: Major LPs 28-30 against ~140 the next quarter; Independent 12-16, Small 0, Boutique 1-2 (`acis260`, both seeds). Major singles 37-41 against 80-100. | About a quarter of a year's LPs lost for every tier; singles at Majors halved for a quarter. |
| 2 | **Shallow opening Major rosters.** Majors open with ~39 acts and fill through scouting and earned capacity: 39 → 48 (end-1960) → 62 (end-1961) → 104-110 (1964, still rising; `acis260`). History: 70-90 releasing acts per major (Big5ChartOpportunityInvestigation; Columbia ~90-100). | Major LPs per roster act per quarter are flat at 0.63 (1960 Q2-4: 0.77 / 0.52 / 0.62; 1961: 0.66 / 0.60 / 0.64 / 0.62). Major LPs rise from ~130 a quarter to ~235 by 1963 with the roster; singles from ~90 to ~150. | After the first quarter, the **whole** 1960 → 1961 Major LP ramp is roster growth. |
| 3 | **Staff sized to the opening roster.** Required staff rises with clients and projects; hiring adds at most 25% of a department a quarter (`LabelCorporateModel.ReviewOrganization`). | Major organisation coverage 0.93 → 0.74-0.77 in H2 1960 → 0.99 by 1962 (`acis260`); programme capacity is multiplied by it. | A consequence of #2: up to a quarter of Major programme capacity off in H2 1960. |
| 4 | **Thin opening catalogue.** Only ~500 records, none older than 20 weeks apart from the switches in step 2; no back catalogue earning. | Median monthly gross H1 1960 against 1961: Major $3.4M vs ~$5.0M, MidTier $106k vs $150-180k, Independent $4.4k vs $7.5-8.5k, Small $14 vs $500-700 (`acis260` 1001). Major cash 1.8M → 9.3M by end-1961. | Low early earnings feed the earned roster capacity (whose week-9 review annualizes January-February, ReleaseVolumeCalibrationPlan.md), MidTier cash against payroll, and no budget editions or rack catalogue from noncurrent titles in 1960. |
| 5 | **LP forecast warm start lands low.** The seed's ratio is 3.5, but shrinkage (n / (n + 4)) applies to the pseudo-LPs too, so an opening Major's multiplier is 3.5^(20/24) = 2.84. Learned Major multipliers in 1961 have a median of 3.7-4.4. | Major gate pass 49-57% in 1960 against 69% in mid-1961; median gate album profit lower. | About 30% low on the Major LP forecast in 1960. Volume barely moves (#2: per-act rate flat). The gate picks from the acts it has, so this changes which LPs pass, not how many. |
| 6 | **Follower stock filling for years.** Seeded only from opening careers' public histories. | Year-end stock 27.4M (1960) → 40.5 → 51.3 → 60.2M (1963), still rising (`acis260` 1001 log). Equilibrium at the ~18% loss rate is ~110M (GameyBehaviorAudit #10). | Unknown size. Follower demand feeds veteran LPs (`veteran-excess` memory), so 1960 follower LP demand is below steady state. No target yet. |
| 7 | **Learned memories at priors.** Format memory (career-banded) and the single forecast realization start with no evidence. | Neutral by design, at fitted priors (dec-awrw reference). | No cold bias; only as good as the priors. Not a limp. |

**Seeding forecasts alone will not fix 1960.** The LP forecast is already seeded (#5), and its error changes which
LPs pass, not how many: with the roster held, Majors issue the same 0.63 LPs per act a quarter in 1960 and 1961.
The volume is set by the empty pipeline (#1) and the roster (#2, with #3 following).

**Options** (none built):
- **A. Warm pipeline** (#1). At week 1 each label's programme plans weeks 1-8 as work already under way since
  November-December 1959: slots through the normal choice and gate, prepared on their intended week without the
  8-week lead. Small and isolated; it changes only the first quarter.
- **B. Historical Major roster depth** (#2, #3). Majors open with ~70-90 acts, mostly contracted veterans with public
  histories (opening careers), operating target and staff sized to match. This is the largest lever and the author's
  design call, because it changes the frozen launch allocation. It meets the Big-5 singles guard (§60-61: the adopted
  1961 Big-5 fit rests on LP-starved Majors, and the fill is newcomers, two-thirds of Big-5 singles).
- **C. Warm-start ratio without shrink** (#5). Exempt the pseudo-LPs from shrinkage, or seed the ratio at the 1961
  learned median (~4). One-line change.
- **D. Deeper opening catalogue** (#4). Older titles with a selling tail and noncurrent catalogue for budget editions
  and racks. It overlaps the evergreen tier (§81) and needs the renewal question settled.
- **E. A full 1959 spin-up.** Run every system from January 1959 and reset the clock, ledgers and telemetry at
  January 1960. This is the general fix for #1-#6 at once. Costs: 1959 chart history and careers become
  model-made, which collides with the historical seeding (opening careers, historical majors). The RNG stream
  reshuffles, and the runtime rises about 20%.

Suggested order: A, then C (both small, A/B-able at 104 weeks), then B as a design decision with the Big-5 guard
read, then D. E only if several of A-D are adopted and the opening still drifts.

## 86. Screen: warm opening (A + C), historical Major roster depth (B), opening evergreens (D) (2026-11-19; seeds 1001-4004, 104 weeks)

**The cold pipeline was a clock bug, not a missing seed.** The release programme spaces an act's work from its last
single and last LP (`ProgrammeClockFor`). An act with no catalogue row of a format is dated from `contractStartWeek`, and
the opening roster is signed through the ordinary signing seam at chart week 0. So every opening act read as signed this
week: in `audnet` no Major act was eligible for an LP slot before week 18 (programme-choices: all 768-849 Major album
candidates `artist-or-spacing-ineligible` for intended weeks 1-10, then the album gap is exactly 18 for 1,120 of 1,121
scored candidates at week 10), and singles only from week 6 outside the ~500 prewarm acts. The programme already plans an
opening horizon (`PlanProgramme(label, date, pipelineWeek)`); it had nobody to put in it.

**A. `--opening-programme-clocks`** (`Systems/OpeningProgrammeClocks.cs`; check in `--gamey-audit-switches-check`, passes).
The opening roster is snapshotted before the first live week. An opening act with no row of the format is dated from its
pre-1960 history: singles from `weeksSinceLastRelease` (seeded 0-52 at roster seeding); LPs from the year of its last
pre-1960 LP in its public history (opening careers) at a keyed week, else a keyed uniform age in [0, 36). In the control's
steady state (acis260, week 130) acts about to issue an LP had last issued one 8 / 16 / 29 weeks before (quartiles), half
never; the uniform gives 9 / 18 / 27. Off is inert: `oc0` (current build, flag off) matches `audnet` on every scored row.

**C. Warm-start ratios without the shrink loss.** No code: `--earned-lp-forecast-warm-ratios=4.5,4.1`. The 20 pseudo-LPs
pass through the n / (n + 4) shrink, so a seeded ratio R applies as R^(20/24). The control's labels end 1961 at applied
Major multipliers 3.1-3.9 and MidTier 3.1-3.4; 4.5 and 4.1 apply as ~3.5 and ~3.25 (the old 3.5 / 2.5 applied 2.84 / 2.15).

**B. `--major-opening-roster=58`** (existing switch, §57): Majors open with 58 acts (§57: ~70-90 releasing acts per major
historically, ~55-60 at the sim's issue scale). **D. `--opening-evergreen-lps=16`** (existing, §81) on this base.

| Mean of seeds | control `audnet` (4) | A (2: 1001/2002) | **A + C** (4) | B = A + C + roster 58 (4) | D = A + C + evergreens (2) | Target |
|---|---|---|---|---|---|---|
| LPs launched weeks 1-13: Major / all tiers | 28 / 53 | 151 / 680 | **148 / 684** | 190 / 724 | 157 / 680 | ~a steady quarter (Major ~140) |
| Major LPs per label 1960 / 1961 | 80 / 139 | 108 / 148 | **107 / 146** | 142 / 167 | 112 / 145 | 145-210 titles (Capitol, Columbia main lines) |
| All LPs launched 1960 / 1961 | 1,776 / 2,437 | 2,476 / 2,594 | **2,543 / 2,578** | 2,665 / 2,636 | 2,544 / 2,545 | Billboard 3,112 / 2,927 (incl. classical) |
| Store LPs 1960 / 1961 (M) | 35.6 / 53.9 | 47.4 / 57.5 | **48.0 / 56.6** | 51.1 / 59.2 | 48.0 / 56.6 | 1961 Billboard 68 |
| Racks 1960 / 1961 (M) | 34.0 / 41.0 | 35.0 / 40.5 | 34.7 / 40.0 | 35.0 / 40.2 | 35.4 / 40.5 | 36-42 |
| 1960 releases 300k+ / 1M+ % (to wk 104) | 2.70 / 0.08 | 2.55 / 0.09 | **2.22 / 0.09** | 2.16 / 0.08 | 2.27 / 0.06 | RIAA 1963 1.31 / 0.44 (§84: score 1963) |
| Major median copies (k) / <4k % | 20.6 / 24.6 | 18.8 / 25.4 | 17.5 / 25.0 | 15.3 / 27.8 | 17.8 / 25.6 | RIAA majors-heavy: <4k 49% |
| Major roster week 1 / 52 / 104 | 37 / 47 / 73 | 39 / 53 / 76 | 37 / 48 / 75 | 58 / 65 / 91 | 39 / 47 / 76 | 55-60 at sim scale |
| Big-5 signings 1960 / 1961 | 69 / 178 | 79 / 190 | 74 / 191 | 72 / 203 | 53 / 203 | |
| Major / MidTier singles 1961 | 408 / 319 | 438 / 334 | 460 / 344 | 480 / 287 | 422 / 301 | |
| **Big-5 Top 10 share 1960** (per seed) | 25.6 (28 / 22 / 27 / 25) | 20.1 | **24.8** (24 / 23 / 29 / 23) | **36.6** (40 / 32 / 39 / 35) | 22.4 | 27 (adopted target) |
| **Big-5 Top 10 share 1961** (per seed) | 21.2 (18 / 22 / 20 / 23) | 21.6 (28 / 15) | **25.6** (28 / 27 / 29 / 19) | **29.9** (34 / 28 / 27 / 30) | 25.6 | 20.6 |
| Bankruptcies by week 104 | 71.5 | 64.5 | **60.2** | 66.2 | 59.0 | |

- **A removes the cold quarter and most of the 1960 ramp.** First-quarter LPs rise from 53 to ~680 across tiers (Major 28 →
  ~150, a steady quarter). 1960 LP releases rise 43% to 2,543 (82% of Billboard's all-format count) and 1960 store LPs from
  35.6 to 48.0M; 1961 moves only +2.7M, so the 1960 / 1961 store ratio goes from 0.66 to 0.85. Bankruptcies fall 71.5 → 60.2.
  The first quarter carries a small catch-up for the smaller tiers (MidTier ~85 against ~60-80 later): the keyed LP ages are
  tier-blind, and small-label acts issue LPs less often than the Major-led steady state the uniform was sized on.
- **C is a small refinement.** On the paired seeds A + C against A: Major LP gate pass 1960 57-68% against 46-54%; Major 1960
  LPs a label +0-7% (107-115 against 107-109). Its job is the forecast level, not volume (§85 #5).
- **300k+ falls a fifth** (2.70 → 2.22%) as the denominator grows (§84); 1M+ unchanged.
- **The Big-5 guard.** A + C adds 4.4 points to 1961 (3 of 4 seeds up; 1960 flat). The channel is roster growth, not LP
  credit: Big-5 signings rise (178 → 191 in 1961) because stronger 1960 LP earnings raise the earned roster capacity, and
  the extra 1961 Big-5 singles are newcomers on contract-delivery slots (seed 1001: 143 → 259). This is §60's warm-start
  result again, smaller (§60: +7.8): the adopted 1961 fit rests on LP-starved Majors.
- **B puts Major LP volume on history (142 / 167 a label) and fails the guard on every seed**: 1960 Big-5 share 36.6
  (target 27), 1961 29.9 (history 20.6). As in §57, the extra opening acts are newcomers from the launch pool, and each
  launches with the Big-5 per-release premium; plug slots (`--major-plug-slots=4`, in the control) cut routine conversion
  but not enough. MidTier 1961 singles fall 344 → 287 (the launch pool drains). Staff coverage stays 0.72-0.75 in H2 1960:
  the opening staff is not sized to the larger roster.
- **D adds 0.7-3.9M store LPs in 1960** (paired with A + C: 47.3 → 51.2, 44.2 → 44.9), about the 3-4M back-catalogue gap
  measured from the steady state (§85 #4: titles over 26 weeks earn 22% of LP gross in steady state, 15% in 1960 under A + C).
  The guard and closures move within noise. On this base (no past-peak tail) the evergreens sell without charting (§81).

**Status.** A + C is a candidate: it fixes the opening clock bug, puts 1960 LP volume and the first quarter near steady
state, cuts closures, and lowers 300k+. Its cost is 1961 Big-5 share 21.2 → 25.6. B is the author's decision: volume on
history against a Big-5 share far over both years; it needs the per-launch premium closed first, as §57 concluded, now with
plug slots in. D is a small positive on top of A + C and belongs with the long-runner decision (§83), since the evergreens
chart only with renewal. Not adopted; no candidate request changed.

## 87. The Big-5 newcomer premium after launch depth, and where B's Big-5 excess comes from (2026-11-19; seeds 1001-4004, 104 weeks)

The question (§86): A + C and B both fail the Big-5 guard; is the Big-5 newcomer per-launch premium the shared cause, and
can launch depth close it? Launch depth's stage 2 (§59, expose / factor / stock arms) was run on 2026-11-17, and its shared
national draw at 0.4 is in the control (`--label-blind-launch=expose --label-blind-launch-scope=major-newcomer
--label-blind-launch-expose-shared=0.4`). Routine plug-slot issues already read the reference launch factor, stock and one
shared draw (`PromotionCapacity.InRoutine`). Scorers: `score_newcomer_depth.py`, `score_newcomer_quality_split.py`,
`analyze_opening_star_owners.py`, `analyze_big5_1961_newcomers.py`.

**At matched quality the newcomer launch premium is near history's per-release ratio.** NewSigning/Unsigned launches
weeks 1-92 (+12), Big-5 against the field excluding MidTiers, field rates re-weighted to the Big-5 quality mix:

| | `audnet` | `ocAC` | `ocB` | `mnfs` |
|---|---|---|---|---|
| Raw Top 10 premium (vs field ex-MidTier) | 2.14 | 2.29 | 2.76 | 2.98 |
| Hot 100 entry at matched quality | 1.17 | **1.01** | 0.97 | 1.20 |
| Top 10 at matched quality | 1.32 | **1.26** | 1.53 | 1.68 |
| Big-5 newcomer quality / field | .72 / .67 | .72 / .66 | .72 / .66 | .72 / .66 |
| MidTier newcomer Top 10 % (Big-5) | 2.80 (1.75) | 2.06 (1.92) | 3.26 (2.01) | 1.91 (2.33) |

The raw 2.1-2.3× is quality mix. Routine Big-5 newcomer singles enter at 0.9-1.15× the field at the same quality. MidTier
newcomers convert as well as the Big-5's or better.

**History does not ask for less newcomer quality.** History's ~1.2× (§57) is all-release (Big-5 Top 10 share over issue
share, all careers), not a newcomer target. The newcomer target is ~4.5 Big-5 newcomer Top 10 records a year (1961-64).
Per seed, by year of first Top 10: `audnet` 4.0 / 3.75 (1960 / 1961), `ocAC` 2.0 / 6.5, `ocB` 3.0 / 7.25. Two-year totals
are 7.75 / 8.5 / 10.25 against ~9. A + C's 1961 newcomer rise (slot points 3.8 → 7.4) moves 1960 newcomer hits (4.2 → 2.2
points) into 1961: the warm pipeline fills 1960 with opening acts.

**Screen `mnfs` (`ocAC` + `--label-blind-launch=factor,stock`: Major newcomers' Plugged picks also read the reference launch
factor and stock). Inert, rejected.** The newcomer premium went 2.08 → 2.70 (2.73 / 2.37 / 2.35 / 3.45 against
2.31 / 2.93 / 1.17 / 2.15), Big-5 newcomer Top 10 30 → 37, mostly Plugged picks (2.75 → 4.5 a seed). Big-5 Top 10 share
1960 24.8 → 23.8, 1961 25.6 → 24.0. As on `zfg` (§59 stage 2), factor and stock do not bind. Launch depth is used up.

**B's excess is established careers, not newcomers.** Big-5 slot points by career at launch, `ocAC` → `ocB`: 1960
established 20.1 → 28.4, newcomer 2.2 → 3.7, prewarm 2.4 → 4.4; 1961 established 18.2 → 24.0, newcomer 7.4 → 5.8. Big-5
NewSigning launches stay 391 → 398 a seed, with Top 10s 7.5 → 8.0. What moves is star careers: Big-5 Star / Superstar
launches 50.5 → 75.5 a seed while the field's fall 94 → 69 (total 144.5 in both arms), plus Rising +23 and Established +13.

**Mechanism: the opening star stamp follows the draft.** The opening allocation (`RosterManager.InitializeSceneRostersCore`)
drafts in rounds, Majors first in each round, by perceived quality and genre fit. Afterwards `OpeningCareerService.Seed`
stamps Star / Superstar careers on the best-quality acts on Major and MidTier rosters (caps: 4 Stars, 2 Superstars, 6
opening acts a label). `--major-opening-roster=58` keeps the Majors drafting after MidTiers reach their targets, so they hold
more of the top-quality acts and the stamp lands on them. Opening star-archetype acts by tier at first launch: `ocAC`
Big-5 9.2 / MidTier 18.8 / Independent 2.0 a seed; `ocB` 13.2 / 14.2 / 2.2. The Big-5's share of stars goes 31 → 45%;
history's Big-5 share of 1960 acts with two or more Top 10s is ~37% (studio-parent screen, 2026-10-22).

**In-run retention is not the problem.** Opening stars whose label changes over weeks 1-104 (4 seeds): MidTier → Big-5 is
4-7 (about one a seed in two years), the rest is MidTier → Independent / Boutique / Small. That is history's pace: Sam
Cooke Keen → RCA (1960), Bobby Darin Atco → Capitol (1962), Fats Domino Imperial → ABC (1963). Historically the stars
stayed because the label that found them held them: rock'n'roll, R&B and teen acts the majors' A&R passed over,
option-year contracts, producer ties (Bleyer at Cadence, Ertegun and Wexler at Atlantic, Bartholomew at Imperial). When a
star moved, the money was often a MidTier's with a film or broadcast parent (Ray Charles to ABC-Paramount 1959, the Everly
Brothers to Warner Bros. 1960).

**Status.** No change adopted. The per-launch newcomer premium is not the lever for either arm: A + C's 1961 cost is a
timing shift of newcomer hits, and B's is the opening star allocation. Candidate fix for B (not built): draft a Major's
opening slots above its drawn target after every other label has reached its target, so roster depth adds working acts
and the star stamp keeps A + C's split. `--studio-parent-labels` (not in the control) is the existing switch that pins the
studio MidTiers' historical stars.

## 88. Screen: opening tier quotas on B (stars and the opening catalogue by tier) (2026-11-19; seeds 1001-4004, 104 weeks)

The author's direction after §87: fix the January 1960 state by tier, then leave it alone. §87 traced B's excess to the
opening draft. Majors pick first in every round, so a deeper Major roster takes more of the best acts, the star stamp lands on
them, and the opening catalogue (CompetitorManager.CalculateInitialQuotas: tier weight × roster size) gives the Majors more of
the opening chart. A further channel stays open: each drafted act has a label-blind 12% / 4.5% chance of a Rising /
Established career at signing (`RosterManager.InitialSignArtist`), so roster depth still adds career acts.

**Switches** (`Systems/OpeningTierQuotas.cs`; hooks in `RosterManager.InitializeSceneRostersCore`,
`OpeningCareerService.ReserveStars` / `Seed`, `CompetitorManager.CalculateInitialQuotas`; checks in
`--gamey-audit-switches-check`, pass):
- `--opening-star-tier-share=S`: the opening Superstars and Stars are chosen from the launch pool before the draft, by the
  stamp's own ranking (spec genres, base quality, id). They are signed to a Major (share S of each rung, interleaved down the
  quality order) or a MidTier label: one that can reach the act and has room, preferred genre first, then a keyed order. The
  stamp then gives exactly those acts their careers. The studio rows (`--studio-parent-labels`, on through the profile) and
  Imperial's rows are reserved first, out of the same budget, and count on the field side.
- `--opening-catalogue-major-share=S`: the Majors' weight in the opening-catalogue split is scaled so the tier holds S of it,
  whatever its roster size.
- Off is identical: `otqoff27-2002-sc` (27 weeks, current build) matches `ocAC-2002-sc` in records, launches and label sales.
  company-monthly differs only in column dtypes from the 27-week cut.

**Settings.** S(stars) = 0.37: the Big-5's share of 1960 acts with two or more Top 10s (studio-parent screen, 2026-10-22).
S(catalogue) = 0.16: A+C's own roster-weighted Major share (1-week probes `otqp-*`: 0.161 / 0.161 / 0.127 / 0.161; B reads
0.227). Under A+C the opening records already chart at history's level (below).

Arm `otqB2` = `ocB` (A + C + `--major-opening-roster=58`) + both switches. The first launch (`otqB-*`) refused the studio switch
at startup and ran no weeks. The reservation: 30 acts a seed, Majors 2-3 of 6 Superstars and 9 of 23 Stars (11.25 star
acts against B's 13.25 and A+C's 9.25). One Superstar on seed 1001 fell to a Major because no MidTier could reach it.

| Mean of 4 seeds | A+C `ocAC` | B `ocB` | **B + quotas `otqB2`** | Target |
|---|---|---|---|---|
| **Big-5 Top 10 share 1960** (per seed) | 24.8 (24/23/29/23) | 36.6 (40/32/39/35) | **23.9** (18/26/24/27) | 27 (adopted) |
| **Big-5 Top 10 share 1961** (per seed) | 25.6 (28/27/29/19) | 29.9 (34/28/27/30) | **26.4** (20/29/34/23) | 20.6 |
| 1960 points: newcomer / other careers / prewarm records | 2.2 / 20.1 / 2.4 | 3.7 / 28.4 / 4.4 | 3.7 / 17.6 / 2.6 | |
| 1961 points: newcomer / other | 7.4 / 18.2 | 5.8 / 24.0 | 7.6 / 18.8 | |
| Opening chart: Big-5 % of week-1 Hot 100 / weeks 1-4 Top 10 | 24.8 / 36.5 | 38.5 / 64.5 | 30.8 / **33.5** | ~24% of top-20 records 1959-60; Q1 1960 Top 10 32% |
| Opening star acts: Big-5 / MidTier | 9.25 / 18.75 | 13.25 / 14.25 | 11.25 / 16.5 | ~37% Big-5 |
| Big-5 Top 10s from wk 1-92 launches: NewSigning / Rising+Established / Star+Superstar | 7.5 / 19.25 / 15.0 | 8.0 / 29.5 / 22.25 | 8.5 / 21.25 / 18.0 | |
| Big-5 newcomer Top 10 records 1960 / 1961 | 2.0 / 6.5 | 3.0 / 7.25 | 3.5 / 6.5 | ~4.5 / yr |
| Big-5 LPs a label 1960 / 1961 (album projects) | 98 / 143 | 133 / 164 | **126 / 165** | 145-210 titles |
| Major singles a label 1961 | 92 | 96 | 94 | |
| Labels inactive at week 104 (company-monthly) | 89.0 | 92.0 | 91.25 | |

- **The quotas remove B's guard failure.** The Big-5 1960 share falls 36.6 → 23.9 (4 of 4 seeds down) and 1961 29.9 → 26.4,
  A+C's level in both years. The "other careers" points that carried B's excess go back to A+C's (1960 28.4 → 17.6; 1961
  24.0 → 18.8). The opening Top 10 goes 64.5 → 33.5%, on history's Q1 32%. The Hot 100 share (30.8%) is above A+C's 24.8: the
  catalogue is fixed by count, and the Majors' deeper rosters still issue their share across more acts.
- **B's volume survives.** Big-5 LPs a label stay 126 / 165 (B 133 / 164; A+C 98 / 143). Major singles and closures move
  within noise.
- **Rising/Established Top 10s come back to A+C's level (29.5 → 21.25).** So the signing-time career draw on the extra acts
  is small next to the catalogue. Those careers come from charting opening records.
- **What stays is A+C's 1961 cost.** 1961 Big-5 is 26.4 against history's 20.6, the same as A+C (§87: newcomer hits move from
  1960 into 1961). Per seed it spans 20-34, so four seeds put the mean within ±3.

**Status.** B + quotas gives history's Major LP volume and opening chart at A+C's Big-5 share. Not adopted; no candidate
request changed. Owed before adoption: the 1961 Big-5 excess shared with A+C, and a 260-week look, since the opening quotas
only set the start.

## 89. Opening staff catch-up and archetype star shares on B + quotas (2026-11-19; seeds 1001-4004, 104 weeks)

**Why 1960 newcomer hits land in 1961.** Big-5 newcomer Top 10s in 1960 come almost only from opening-roster acts; acts
signed during the run barely launch before 1961 (signings begin week 5). Majors issue 58-62 singles a label in 1960 against
92-94 in 1961 (history ~216 a label in 1960): 9-13 a quarter in Q1-Q3 1960 against 23-25 from Q5. Major programme telemetry
(`programme-resources`, `programme-issue-refusals`) shows two throttles.
- **Staff coverage.** Majors run 0.85 in Q1, 0.73 in Q2-Q3 and 0.78 in Q4 1960, and 0.93 by late 1961. Every other tier holds
  0.9 or above. The planned single rate falls with coverage (2.44 a week at 0.85, 1.98 at 0.73). The opening organization
  (`LabelCorporateModel.InitializeOpeningOrganization`) is sized with no distribution clients and projects capped at the
  label's release rate. The monthly review then sees the real programme and the P&D clients arriving, and hiring is capped
  at 25% a department a quarter.
- **The profit gate in Q1-Q2 1960:** 6.8 and 3.4 refusals a label a quarter. Not addressed here.

**Switches.**
- `--opening-staff-catch-up=M` (`Systems/OpeningStaffCatchUp.cs`, hook in `LabelCorporateModel.ReviewOrganization`): for the
  first M months, each monthly review hires each department's whole gap under the same cash test. Contraction stays
  quarterly. After the window the 25% cap applies. Screened at M = 12 (1960).
- `--opening-star-tier-share=AdultStandardBearer:S,CurrentHitmaker:S,EstablishedHitmaker:S`: a Major share per archetype, split
  within each archetype over both rungs (the single-share form is unchanged and reproduces `otqB2`'s reservation and stamps
  on seed 1001). Shares 0.85 / 0.25 / 0.6 come from the January 1960 holders (author agreed with the tally):
  - Adult standard-bearers, ~7 of 8 on majors: Sinatra, Como, Cole, Mathis, Bennett, Day, Page, Martin.
  - Current hitmakers, ~2-3 of 10: Elvis and Brenda Lee on majors; Nelson, Anka, Darin, Francis, the Everlys, Avalon elsewhere.
  - Established hitmakers, ~7 of 12: Robbins, Cash, Horton, Reeves, the Platters, Benton, the Kingston Trio on majors;
    Domino, Ray Charles, the Drifters, Berry elsewhere.
- Checks in `--gamey-audit-switches-check` pass. No off-identity run was made for this build; the off paths are guarded
  conditionals, as in §88.

Arms on `otqB2` (§88): `stB` = + `--opening-staff-catch-up=12`. `stArB` = `ocB` + catalogue 0.16 + catch-up 12 + archetype
shares (Majors hold 4 of 6 Superstars and 13 of 23 Stars; 7/8, 3/10, 7/11 by archetype).

| Mean of 4 seeds | `otqB2` | **`stB`** | `stArB` | History |
|---|---|---|---|---|
| Big-5 Top 10 share 1960 (per seed) | 23.9 (18/26/24/27) | **26.2** (29/30/25/20) | 32.7 (39/36/33/23) | 27 adopted (34.2 raw) |
| Big-5 Top 10 share 1961 (per seed) | 26.4 (20/29/34/23) | 29.5 (36/27/34/22) | 33.3 (28/39/33/33) | 20.6 |
| 1961: Big-5 1960 hit acts / other Big-5 acts | 13.4 / 12.9 | **17.8** / 11.7 | 16.9 / 16.4 | 16.4 / 4.2 |
| Big-5 newcomer Top 10 records 1960 / 1961 | 3.5 / 6.5 | **6.0 / 5.0** | 5.25 / 8.0 | ~4.5 / ~4.5 |
| … opening-roster acts 1960 / 1961 | 3.25 / 4.0 | 5.25 / 1.75 | 4.75 / 3.75 | |
| Major singles a label 1960 / 1961 | 62 / 94 | 72 / 101 | 78 / 109 | ~216 (1960) |
| Major staff coverage 1960 / 1961 | 0.77 / 0.88 | 0.90 / 0.96 | 0.91 / 0.96 | |
| Opening star acts Big-5 / MidTier | 11.25 / 16.5 | 11.25 / 16.75 | 17.0 / 10.75 | ~16-17 Big-5 |
| Opening chart: Big-5 % week-1 Hot 100 / weeks 1-4 Top 10 | 30.8 / 33.5 | 30.8 / 33.5 | 37.5 / 40.0 | ~24 / 32 |
| Big-5 LPs a label 1960 / 1961 | 126 / 165 | 128 / 166 | 137 / 175 | 145-210 |
| Labels inactive at week 104 (Small / Indep. / MidTier) | 91.3 (37 / 35 / 11) | 94.0 (41 / 34 / 9) | 106.3 (46.5 / 37 / 13) | |

- **Staff catch-up moves newcomer hits into 1960.** Coverage reaches 0.90 in 1960, and Major singles rise 62 → 72 a label.
  Big-5 newcomer Top 10 records go 3.5 → 6.0 in 1960 and 6.5 → 5.0 in 1961; opening-roster newcomers' 1961 hits fall
  4.0 → 1.75. 1960 Big-5 share is 26.2 (target 27).
- **The 1961 total rises (26.4 → 29.5) through the 1960 hit acts' follow-ups.** They now hold 17.8 points, on history's 16.4.
  The 1961 excess is the other Big-5 acts: 11.7 points against 4.2. Newcomers are ~4.8 of them (on history's count); the rest
  is Rising acts (3.8) and Established acts (~2.3) with Top 11-100 placings but no 1960 Top 10.
- **Those acts over-climb on repeat entries** (`score_climb_by_prior.py`, debuts weeks 40-92, `stB`). Top 10 per Hot 100
  event, prior best 11-40: Big-5 28.2%, MidTier 21.9, field 13.4, history 10.2 (label-blind). Prior best 41-100: Big-5 18.4,
  history 9.0. Prior Top 10: Big-5 28.3, history 17.5. First entries: Big-5 17.7, field 9.9, history 20.8. This is §75's
  pattern; it is the main 1961 residual once the opening is fixed.
- **History's star ownership raises the Big-5 in both years** (`stArB`: 32.7 / 33.3). 1961 Big-5 1960 hit acts stay on history
  (16.9); other Big-5 acts rise to 16.4, newcomers 9.0 points among them (more stars → more Major LP earnings → earned roster
  capacity → more signings, §86's channel; Major singles 109 a label in 1961). Closures rise 94 → 106 (Small +5.5, MidTier
  +3.75): the MidTiers lose the stars.

**Status.** `stB` (B + both quotas + staff catch-up 12) is the candidate shape: 1960 on target, 1961's Big-5 1960 hit acts on
history, newcomer hits by year near history, LP volume kept. Archetype shares are the historical ownership but cannot be
afforded until the Big-5 repeat-entry over-climb (prior #11-100: 2-3× history) is fixed. Not adopted; no candidate request
changed.

## 90. The Big-5 repeat-entry over-climb is the plug; a Top 10 hit-act rung moves plugs to the wrong acts (2026-11-19; seeds 1001-4004, 104 weeks)

The question (§89): on `stB`, Big-5 chart events by acts whose best was #11-40 reach the Top 10 28.2% of the time, against
history's 10.2% (label-blind; field 13.4). Why? Scratch scorers (per launch, weeks 36-88, joined to `PROMO_PLUG` lines in the
engine logs).

**Where it comes from: plugged follow-ups.** Big-5 singles by acts whose prior best was #11-40, `stB`, 4 seeds:

| | launches | Hot 100 | Top 10 per launch | Top 10 per entry | launch awareness | launch stock |
|---|---|---|---|---|---|---|
| Big-5 plugged | 90 | 64% | **15.6%** | 24% | 0.40 | 35k |
| Big-5 routine | 75 | 27% | 4.0% | 15% | 0.28 | 26k |
| MidTier (no plug switch; full package) | 146 | 50% | 11.6% | 23% | 0.38 | 29k |
| field | 732 | 37% | 4.5% | 12% | 0.25 | 12k |

- A Big-5 routine issue converts at the field's rate. The premium is the full package, and under the plug rule
  (`PromotionCapacity.HitAct`: Star/Superstar or `consecutiveHits > 0`, a Top 40 streak broken only by a peak worse than 60)
  every act whose last run was #11-40 counts as a hit act. Hit acts always get a plug and may buy one more.
- The edge is at launch, not after entry. Plugged records start at the 0.6 awareness cap in week 1 (the field reaches it in
  week 6), and units in weeks 1-5 run about 2× the field's. Radio heat, panel share and word of mouth match the field.
  Follow-up campaigns are rare (5 records). Fit draws can't be compared: `single-demand-stages` traces only 19% of these records.
- Matched quality doesn't close it. Top quality bin (0.8+): Big-5 22% Top 10 per launch, field 8%.

**Screen `pt10B` = `stB` + `--plug-hit-act-top10`.** `Systems/PromotionCapacity.cs`: the hit act is a Star/Superstar or an act
whose last completed run reached the Top 10 (new `SimulatedArtist.lastRunPeak`, written by `CompleteChartRun` and by the
opening `DeriveCounters`, read only by the switch). The hit-act reserve counts the same acts. Checks in `--major-volume-check`
pass. No off-identity run was made: `lastRunPeak` is written but read only under the switch.

| Mean of 4 seeds | `stB` | `pt10B` | History |
|---|---|---|---|
| Big-5 Top 10 share 1960 (per seed) | 26.2 (29/30/26/20) | 21.8 (20/26/22/19) | 27 |
| Big-5 Top 10 share 1961 (per seed) | 29.5 (36/27/34/22) | 23.5 (29/24/23/19) | 20.6 |
| 1961: Big-5 1960 hit acts / other Big-5 acts | 17.8 / 11.6 | **8.7 / 14.8** | 16.4 / 4.2 |
| Big-5 Top 10 per event, prior #11-40 / #41-100 / Top 10 | 28.2 / 18.4 / 28.3 | 15.7 / 15.3 / 21.8 | 10.2 / 9.0 / 17.5 |
| Big-5 events a year, prior #41-100 | 9.3 | 14.5 | |
| Big-5 newcomer Top 10 records 1960 / 1961 | 6.0 / 5.0 | 3.0 / 7.5 | ~4.5 / ~4.5 |
| Plugs (plugged / bought / routine, 4 seeds) | 1,175 / 21 / 1,897 | 1,103 / 3 / 1,781 | |
| Major singles a label 1961 | 101 | 92 | |
| Labels inactive at week 104 | 94.0 | 95.5 | |

- The per-event rate falls as aimed (#11-40: 28.2 → 15.7). But the 1961 drop is in the wrong row: Big-5 1960 hit acts fall
  17.8 → 8.7 (history 16.4), and other Big-5 acts *rise*, 11.6 → 14.8 (history 4.2).
- Why: a real hit act whose latest single stalled at #11-40 loses its plug. The freed reserve goes through the read bar,
  where higher-quality #11-100 repeat acts beat the opening-roster newcomers to the free slots. Newcomer 1960 Top 10s
  fall 6.0 → 3.0 and slide into 1961, and #41-100 repeat events rise 9.3 → 14.5 a year at 15% Top 10.
- 1960 falls to 21.8 (target 27).

**Status.** `--plug-hit-act-top10` rejected; it stays in the code, off. Who counts as a hit act only moves plugs between
Big-5 acts. A plug turns any Big-5 act's single into a hit-act launch (15.6% a launch against 4% routine), and the
plug count (4 a month plus bought plugs) decides how many non-proven acts get one. The over-climb is plug strength on
non-proven acts, not the hit-act rung. Next candidates: (a) a plug's strength by the act's standing (the full
package only for a Top 10 last run or a star, a partial package for #11-40, routine below); (b) fewer slots for non-hit
reads with the Top 40 rung kept.

## 91. Screen: graded plug strength (held plugs for #11-60 hit acts) does not fix it either (2026-11-19; seeds 1001/2002, 104 weeks)

**Switch** `--plug-graded=G` (`Systems/PromotionCapacity.cs`; checks in `--major-volume-check` pass). The Top 40 hit-act rule
still decides slots and the reserve. A Star, a Superstar or an act whose last run reached the Top 10 gets the full package.
Any other hit act (last run #11-60 on an unbroken Top 40 streak) takes its slot as before and launches with the full package
at keyed chance G, else as a routine issue (a "held plug"). It buys no extra plug. Unlike `pt10B` (§90), no slot is freed
for other acts. Arms on `stB`: `pg0B` (G = 0) and `pg5B` (G = 0.5). The first 4-seed launch ran 8 worlds at once, and
`pg5B-2002` died with OutOfMemoryException. Seeds 3003/4004 were stopped, and `pg5B-2002` was rerun alone.

| Seeds 1001/2002 | `stB` | `pt10B` | `pg0B` | `pg5B` | History |
|---|---|---|---|---|---|
| Big-5 Top 10 share 1960 | 29.6 | 23.3 | 24.9 | 25.5 | 27 |
| Big-5 Top 10 share 1961 (per seed) | 31.2 (36/27) | 26.4 (29/24) | **34.4** (39/30) | 26.2 (31/22) | 20.6 |
| 1961: Big-5 1960 hit acts / other Big-5 acts | 18.4 / 12.8 | 10.5 / 16.0 | 13.0 / **21.2** | 12.8 / 13.4 | 16.4 / 4.2 |
| Big-5 Top 10 per event, prior #11-40 | 26.9 | – | 20.9 | 20.5 | 10.2 |
| Big-5 newcomer Top 10 records 1960 / 1961 | 8.0 / 6.0 | 2.5 / 9.5 | 5.0 / 10.0 | 4.5 / 7.5 | ~4.5 / ~4.5 |
| Plugs: full / held / bought / routine | 589 / – / 11 / 926 | | 427 / 148 / 6 / 919 | 529 / 73 / 3 / 1,056 | |
| Major singles a label 1961 | 99 | 94 | 104 | 106 | |
| Labels inactive at week 104 | 93.5 | 91.5 | 90.0 | 93.0 | |

- The held plugs did replace full plugs (`pg0B`: 148 held, full plugs 589 → 427). Yet the "other Big-5 acts" row did not
  fall: 12.8 → 21.2 at G = 0 and 13.4 at G = 0.5. The 1960 hit acts fall again (18.4 → 13), as in §90, because a real hit
  act whose latest single stalled loses its package.
- Even when every #11-60 act's single is routine (G = 0), the Big-5 #11-40 Top 10 rate per event stays at 21% (history 10,
  field 12). So the plug is part of the over-climb, not all of it. A routine Big-5 issue still has the label's national
  distribution, city reach, arrival hold and roster programme, and the act's recognition and act-record fit terms.
- At two seeds a 1961 point is noisy (per-seed spreads of 6-9 points), but no arm moves the target row in the right
  direction without cutting the 1960 hit acts.

**Status.** `--plug-graded` rejected; it stays in the code, off. Launch-package levers on #11-60 acts are exhausted (§90-91):
they trade the 1960 hit acts' follow-ups for other Big-5 acts. Before another lever, the "other Big-5 acts" row needs
attributing: by launch career, plug outcome and prior band per record, on `stB` and `pg0B` (where the newcomer 1961 points
rose 6.3 → 11.4).

## 92. Who holds the 1961 "other Big-5 acts" slots: acts that charted below the Top 10 in 1960, which history has none of (2026-11-19; read-only)

Scorer `SimTools/attribute_big5_1961_other.py SEEDS ARM`. Points = share of 1961 Top 10 slot-weeks. History: the operating-
label 1961 Top 10 (`historical-top10-1961`); the act's best 1960 Hot 100 peak from `hot100_weekly`.

**History's 4.2 points are five records, all by acts with no 1960 Hot 100 entry**: Jimmy Dean "Big Bad John" (10 slots),
Leroy Van Dyke, Buzz Clifford, the Tokens, Patsy Cline. No Big-5 act with a 1960 #11-100 best had a 1961 Top 10.

`stB` (4 seeds), 11.7 points:

| By the act's 1960 best | points | of which plugged / routine / not routed |
|---|---|---|
| #11-40 | 1.8 | 1.5 / 0.3 / 0 |
| #41-100 | 2.2 | 1.9 / 0 / 0.2 |
| no 1960 Hot 100 entry | 7.6 (history 4.2) | 4.0 / 2.0 / 1.7 |

- By launch career: NewSigning 4.8, Rising 3.8, Established 2.3, Declining 0.7. Opening careers hold only 1.5. Nearly all
  (11.2) are singles launched in 1961.
- The no-1960-entry 7.6 is newcomers 4.3, Rising 2.5 (signing-time Rising draws and acts that charted only in 1961) and
  Established 0.8.

**Label-blind: acts that charted below the Top 10 convert too often, and fresh acts too rarely.** 1961 Top 10 points by the act's
1960 best, and the share of 1960 acts in each band with a 1961 Top 10:

| | History | `stB` | `pg0B` | `pg5B` |
|---|---|---|---|---|
| Points: no 1960 entry / 1960 Top 10 / #11-40 / #41-100 | 46.0 / 39.2 / 11.5 / 3.3 | 32.6 / 47.1 / 11.3 / 8.9 | 32.1 / 38.3 / 19.6 / 10.0 | 35.2 / 41.2 / 13.4 / 10.2 |
| 1960 acts with best #41-100 (count; % with a 1961 Top 10) | 148; 1.4% | Big-5 28; 7.1% · other 255; 3.0% | | |
| 1960 acts with best #11-40 | 96; 7.3% | Big-5 13.5; 18.5% · other 76; 11.8% | Big-5 26.1% | Big-5 16.7% |
| 1960 acts with a Top 10 | 74; 33.8% | Big-5 23; 44.0% · other 65; 31.5% | | |

- The sim has twice history's #41-100 acts (283 a seed against 148), and they reach a 1961 Top 10 at 2× (field) to 5× (Big-5)
  history's 1.4%. #11-40 acts convert at 1.6× (field) and 2.5× (Big-5) history's 7.3%.
- Fresh acts are short across the chart: 32.6 points against 46. The Big-5's fresh row is over (7.6 against 4.2), so the
  field's fresh acts carry about 25 against history's ~42.
- Within an act, consecutive singles' quality correlates at 0.34 (hook 0.32); the within-act SD is 0.062 against 0.086
  overall. That is modest, and history has no comparable measure.

**Reading.** The "other Big-5 acts" excess is about 4.0 points from acts that charted #11-100 in 1960 (history 0) and
about 3.4 points of extra fresh Big-5 acts. It belongs to a chart-wide pattern: a modest chart record carries forward to the
next records far more than in history, most strongly at the Big-5, while fresh acts break through too rarely. The plug is
one carrier (§90-91), but cutting it moves slots between the same Big-5 acts. Candidate carriers to screen next are the
act-level terms a #41-100 act gains: recognition at launch (`ArtistRecognitionService` awareness and stock lifts), follower
demand (`--follower-demand`), the Rising career at signing and its stock scale, and the act-record fit (Top 40 acts only).

## 93. Entries per act match history; the climb per entry is flat in the act's past. An act-level fit share does not order it (2026-11-19; seeds 1001/2002, 104 weeks)

**Decomposition** (per 1960 band, 1961 debuts; history's Hot 100 1960-61, label-blind; sim `stB`):

| Act's 1960 best | History: acts / entries per act / Top 10 per entry | `stB` Big-5 | `stB` field |
|---|---|---|---|
| Top 10 | 74 / 2.72 / 19.4% | 2.66 / 23.8% | 2.06 / 16.8% |
| #11-40 | 96 / 1.04 / **8.0%** | 1.25 / 14.7% | 0.83 / 13.3% |
| #41-100 | 148 / 0.28 / **7.1%** | 0.23 / 27.2% | 0.24 / 12.9% |
| no 1960 entry (fresh) | 242 / 1.43 / **16.7%** | 1.43 / 17.7% | 1.26 / 11.7% |

(Seeds 1001/2002; the 4-seed figures in §92 agree.) The number of chart entries per act is on history in every band. What is
off is the climb: in history a modest act's next record usually stalls (7-8% Top 10 an entry), while a fresh act's first
records climb best (17%). In the sim the climb hardly depends on the act's past (field 12-17% in every band). Modest acts
climb too often everywhere, most at the Big-5, and the field's fresh acts climb too rarely.

**Screen `--single-commercial-fit-act-share=R`** (`Systems/SingleCommercialFit.cs` `NationalZ`): the national fit draw becomes
sqrt(R) × an act-keyed draw + sqrt(1 - R) × the record's draw. Every record keeps a standard normal draw, and an act's
records correlate at R. `--single-commercial-fit-shape-check` passes (42 checks; off identical, same-act correlation 0.5 at
R = 0.5). Arms on `stB`: `as3B` (R 0.3) and `as6B` (R 0.6).

| Seeds 1001/2002 | `stB` | `as3B` | `as6B` | History |
|---|---|---|---|---|
| Top 10 per entry, field: Top 10 / #11-40 / #41-100 / fresh | 16.8 / 13.3 / 12.9 / 11.7 | 15.5 / 14.6 / 11.5 / 10.6 | 25.3 / 13.9 / 11.1 / **8.0** | 19.4 / 8.0 / 7.1 / 16.7 |
| Top 10 per entry, Big-5: #11-40 / #41-100 | 14.7 / 27.2 | 22.0 / 40.4 | 18.6 / 14.7 | |
| Big-5 Top 10 share 1960 / 1961 | 29.6 / 31.2 | 24.9 / 32.9 | 26.9 / 27.6 | 27 / 20.6 |
| 1961: 1960 hit acts / other Big-5 acts | 18.4 / 12.8 | 16.6 / 16.2 | 16.0 / 11.6 | 16.4 / 4.2 |
| Big-5 newcomer Top 10 records 1960 / 1961 | 8.0 / 6.0 | 3.5 / 6.0 | 3.5 / **2.0** | ~4.5 / ~4.5 |
| Labels inactive at week 104 | 93.5 | 96.0 | **104.0** | |

- **The shared draw makes hit acts persistent but does not mark modest acts as modest.** At R 0.6 the field's Top 10 acts
  climb more (16.8 → 25.3), the #11-100 rows barely move, and fresh acts climb less (11.7 → 8.0). With a fixed purse, hit acts'
  extra share comes out of fresh acts. A #41-100 run does not reveal a low act draw, because such records chart mainly on
  launch strength (awareness, stock, plug), not on the draw.
- At R 0.6 the 1961 Big-5 share falls to 27.6 only because newcomers lose (6.0 → 2.0 Top 10 records). The other-Big-5 row
  stays at 11.6 (history 4.2), and closures rise by 10.5.

**Status.** `--single-commercial-fit-act-share` rejected; it stays in the code, off. With §90-91, three levers have now
moved Big-5 slots between groups without restoring the history pattern. The pattern to fix is chart-wide: modest acts'
records climb at 12-27% an entry (history 7-8%), and the field's fresh acts' at 12% (history 17%). The fresh-act deficit has
stood since §75 (field first entries 10% against 21%). Raising it would take slots from the modest repeat acts on a fixed
chart, which may be the more direct lever.

## 94. Fresh field acts: opening the Top 40 gates to the whole Hot 100 lifts their climb to history's (2026-11-19; seed 1001, 52 weeks)

**What holds a fresh field act** (`stB`, an act's first chart entry, weeks 40-92, 4 seeds). Field first entries debut with
4.1 covered regions and reach only 4.4 at most. Their Top 10 rate climbs with coverage: 2.6% at 1 region, 8-11% at 2-5, 16.5% at
6-7, where it equals the Big-5 newcomers' rate (7 regions, 18.9%). Small labels sit at 1.6 regions and 3.2%. Three gates
open for an uncovered region only inside the national top 40: fit exposure (`RegionalFitExposure.NationalRank`), held-region
arrival (`--regional-arrival`, depth 40) and restock service (uncovered regions are served at 0.18 + 0.25 × strength). So a
fresh record debuting at #90 must climb about 50 places on 3-4 regions before the rest of the map can buy it. In 1960, one-stops and
independent distributors ordered off the whole chart.

**Screen** (existing switches in `Systems/LabelFootprintParity.cs` and `EarnedIndieCoverage.cs`, one seed, 52 weeks, a 52-week
control `fc52` on `stB-1001`'s flags):
- `fn52`: `--regional-fit-exposure-national-rank=100 --regional-arrival-chart-depth=100`.
- `fnec52`: `fn52` + `--earned-indie-coverage=1`.

| Seed 1001, 1960 | `fc52` | `fn52` | `fnec52` | History |
|---|---|---|---|---|
| Field first entries (debut wk 12-48): Top 10 / Top 40 per entry | 10.1 / 31.5 | 16.3 / 30.4 | **19.0 / 35.8** | 16.7-20.8 / ~46 |
| … Independent / Boutique Top 10 per entry | 11.1 / 5.6 | 15.4 / 26.3 | 17.4 / 34.4 | |
| Fresh acts' Top 10 records (all labels) | 46 | 54 | 59 | |
| Big-5 Top 10 / Top 40 share | 29.2 / 24.0 | **18.3 / 17.9** | **19.8 / 18.3** | 27 (adopted) |
| Top 10 records in the year | 120 | 133 | 135 | ~111 (1961) |
| Hot 100 units (M) | 110.4 | 110.7 | 113.0 | |
| Labels inactive at week 52 | 14 | 14 | 12 | |

- **The effect takes.** Opening exposure and arrival to the whole chart moves field first entries' Top 10 rate from 10% to
  16%, and restock lifts it to 19%, on history's 17-21%. Chart units are flat, and no more labels close in the first year.
- **Cost: 1960 Big-5 share drops about 10 points** (29.2 → 18-20 against the 27 target). Fresh field hits take Big-5 slots
  already in 1960, and Top 10 turnover rises (120 → 135 records against ~111).
- One seed and one year. The 1961 target rows (§92-93: modest repeat acts, the other-Big-5 row) need 104 weeks.

**Status.** Not adopted. The lever works on the fresh-act deficit. Next: dose the depth (60-80) to keep 1960 Big-5 near 27,
then 104 weeks on 2 seeds scored on §93's band table and closures.

**Dose (2026-11-19; seed 1001, 52 weeks).** The arms below set exposure, arrival and earned coverage to depth N
(`--earned-indie-coverage-position=N`):

| Seed 1001, 1960 | control | N = 60 | N = 80 | N = 100 (`fnec52`) | History |
|---|---|---|---|---|---|
| Field first entries: Top 10 per entry | 10.1 | 12.1 | 13.4 | **19.0** | 16.7-20.8 |
| Fresh acts' Top 10 records | 46 | 52 | 51 | 59 | |
| Big-5 Top 10 share (H1 / H2) | 29.2 (26.7 / 31.9) | 23.2 (18.5 / 28.1) | 22.6 (20.4 / 25.0) | 19.8 (23.3 / 16.2) | 27 |
| Top 10 records | 120 | 126 | 123 | 135 | |
| Labels inactive at week 52 | 14 | 16 | 15 | 12 | |

- The fresh-act gain is nearly all at depth 100. Fresh records debut around #90, so a gate at 60 or 80 still sits above most
  of them.
- The Big-5 share does not dose with it: N 60/80 cost 6-7 points for a fraction of the gain. Single-seed 1960 Top 10 share
  moves ±4-5 points with any perturbation (the H1 halves differ before most of the run diverges). Below 100 there is no
  depth that keeps the Big-5 at 27 and fixes fresh acts.
- Reading: if fresh field hits are to come up to history, Big-5 share has to be held by Big-5-side levers that are now
  affordable. §89's historical star ownership (`stArB`, 32.7 / 33.3 alone) is the natural pair. Next: 104 weeks × 2
  seeds, `fnec` depth 100 on `stB` and on `stArB`, scored on 1960/1961 share, §93's band table and closures.

## 95. Screen: depth 100 (exposure, arrival, earned coverage) at 104 weeks, alone and with historical star ownership (2026-11-19; seeds 1001/2002)

Arms: `fnB` = `stB` + `--regional-fit-exposure-national-rank=100 --regional-arrival-chart-depth=100 --earned-indie-coverage=1`;
`fnArB` = `stArB` (§89 archetype star shares) + the same three.

| Seeds 1001/2002 | `stB` | `fnB` | `stArB` | `fnArB` | History |
|---|---|---|---|---|---|
| Big-5 Top 10 share 1960 | 29.6 | 16.2 | 37.4 | **27.2** | 27 |
| Big-5 Top 10 share 1961 | 31.2 | 30.4 | 33.2 | 28.0 | 20.6 |
| 1961: 1960 hit acts / other Big-5 acts | 18.4 / 12.8 | 11.5 / 19.0 | 18.2 / 15.1 | 11.9 / 16.2 | 16.4 / 4.2 |
| 1961 field Top 10 per entry: fresh / #41-100 / #11-40 / Top 10 | 11.7 / 12.9 / 13.3 / 16.8 | 11.3 / 15.2 / 15.8 / 17.9 | 12.8 / 19.7 / 15.6 / 11.6 | 14.7 / 22.5 / 21.3 / 17.2 | 16.7 / 7.1 / 8.0 / 19.4 |
| Big-5 newcomer Top 10 records 1960 / 1961 | 8.0 / 6.0 | 3.0 / 10.5 | 7.0 / 4.0 | 5.0 / 8.0 | ~4.5 / ~4.5 |
| Major singles a label 1961 | 99 | 101 | 110 | 119 | |
| Labels inactive at week 104 | 93.5 | **102.0** | 108.0 | **111.5** | |

- **1960 lands.** Star ownership plus depth 100 puts 1960 at 27.2. Depth 100 alone overshoots to 16.2.
- **1961 does not move.** It stays at 28-30. The fresh field acts' 1961 gain mostly disappears (11.7 → 11.3 in `fnB`, 12.8 → 14.7
  with stars). Opening the bottom of the chart lifts every field record there, and modest repeat acts gain more than fresh ones
  (#41-100 22.5% against history's 7.1). So the flat-in-the-past pattern of §93 gets worse.
- **The 1960 hit acts' 1961 follow-ups fall** (18 → 12, history 16.4), and the other-Big-5 row stays high. In `fnArB` it
  holds 16.2: #11-40 3.3, #41-100 4.1, fresh 8.8 (newcomers 5.4), plugged 10.0.
- **Closures rise by 8.5 to 18** (`fnB` 102, `fnArB` 111.5 against `stB` 93.5).

**Status.** Not adopted. Depth 100 fixes the first-year fresh climb and, paired with star ownership, the 1960 share.
It doesn't fix 1961 or §93's ordering: the open bottom helps modest acts too, and closures rise. The 1961 Big-5 excess
has stood at 28-31 through every lever in §88-95.

## 96. Audit of the 1961 Big-5 target: 20.6 is one noisy year; the sim's 29.5 is about 1.4 SE away (2026-11-19; read-only)

Sources: `SimLogs/historical-top10-{1960..1964}/records_YYYY_final.csv` (52 weekly Top 10s each, operating labels). Sim:
`stB`, 4 seeds.

**1. Scope: marquee labels against corporate families.** 20.6 counts only the five marquee labels. The sim has no Epic, Okeh,
Coral, Brunswick, Smash or Philips (HistoricalLabelTierBalanceResearch.md §11), so its five Majors stand for the whole groups.
Family share (Columbia + Epic/Okeh; RCA + Groove/Vik/Camden; Capitol; Decca + Coral/Brunswick; Mercury + Smash/Philips):

| | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---|---|---|---|
| Marquee share (current targets) | 34.2 | 20.6 | 21.2 | 26.0 | 18.7 |
| Family share | 35.6 | **22.1** | 26.0 | 38.8 | 34.4 |
| Bootstrap SD (resampling records) | 5.6 | **4.3** | 4.7 | 5.1 | 5.0 |

In 1961 the subsidiaries add only 1.5 points (Smash 7 slots, Brunswick 1). From 1962 they matter (Epic, Smash, Philips), and
1963-64 marquee targets understate the groups by 13-16 points.

**2. Noise.** A year's Top 10 is about 115 records, and a few long runs move the share. 1961's bootstrap SD is 4.3 points, so
the 95% range on the family share is about 13.5-30.7. `stB`'s seeds run 36.0 / 26.5 / 33.7 / 21.7 (SD 6.4). Mean against
history: 29.5 against 22.1, a gap of 7.4 against a combined SE of about 5.3 (history 4.3, sim mean 3.2), i.e. about 1.4 SE.
Pooled 1961-62 family history is 24.0 (SD about 3.2), so the gap is 5.5 at about 1.2 SE.

**3. Per firm, 1961** (`stB` mean / history family): Columbia 4.7 / 4.4, RCA 6.6 / 8.5, **Capitol 2.9 / 0.0**,
**Decca 9.2 / 5.6**, **Mercury 6.1 / 3.7**. Capitol's zero is a real slump: no Capitol single reached the 1961 Top 10. The sim's
excess sits in Decca, Mercury and Capitol, while RCA (Elvis) is under. Per firm, the sim swings 1-14 points between seeds.

**4. Two-year view.** Family history 1960 + 1961 averages 28.9, and `stB` 29.6 / 29.5 averages 29.6. Over the two years the sim
is on history. What it lacks is history's swing (35.6 → 22.1), which the 1960-dip screen (memory big5-1960-dip-is-run-length) traced to one 1960 event
cluster (Elvis's return, the "Summer Place" blockbusters). That cluster is why 1960 was adopted at 27 and not 34.2. Taking that
down-weighting for 1960 and not for 1961 sets the two years' targets on different bases.

**Reading.** The 20.6 target treats one year's marquee share as a point. Corrected for scope, it is 22.1 ± 4.3, and the sim's
1961 excess is inside about 1.5 SE of it. §88-95's levers were judged against 20.6 and kept failing on 1961. The 1960-61 mean is on
history. Proposed target, for the author: judge Big-5 share on corporate families, as a band (1961: 22 ± 4.3; pooled 1961-62: 24
± 3.2) or on the 1960-61 mean (history 28.9), with at least 4 seeds. Under that rule `stB` (29.6 / 29.5) passes 1960 and sits at
the edge of the 1961 band.

**ADOPTED 2026-11-19 (author).** The Big-5 Top 10 share is scored on corporate families, as a band, with at least 4 seeds:
1960 27 (unchanged), 1961 22.1 ± 4.3 (pooled 1961-62 24.0 ± 3.2). `score_chart_faller_depth.py` and
`score_opening_tier_quotas.py` now print 22.1 for 1961. Under the band, `stB` (§89) passes 1960 (26.2); its 1961 (29.5) sits 3.1 above
the band top of 26.4, within about one SE of the four-seed mean. The §90-95 switches stay off. The 1962-64 targets (marquee
21.2 / 26.0 / 18.7, families 26.0 / 38.8 / 34.4) need re-basing before any late-decade Major work.
