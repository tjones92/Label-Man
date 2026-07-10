# Directive 4b - Stage 1 ensemble adjudication

Date: 2026-07-10

Scope: gate adjudication only. Task 0 remains closed; taxonomy values were held fixed; `distanceModelEnabled` remains `false`. Stage 2, seed 2004, and the v2 freeze were not entered.

## Method

The former same-seed/per-year +/-3% comparison is suspended by sign-off as a defective conservation test because a seventh live region changes the RNG topology. This audit compares the fixed three-seed v2 taxonomy ensemble (1001-1003) with six independently generated v1 disabled runs (1001-1006), all using 520 weeks and albums disabled.

For v1 internal calibration, all ten unique partitions of six seeds into two groups of three were enumerated. Each partition reports the percentage difference between its two group means. This gives an empirical three-seed-versus-three-seed variability envelope without mixing v1 and v2 implementations.

## Complete seed-year matrix

Annual disabled Single units:

| Year | V1-1001 | V1-1002 | V1-1003 | V1-1004 | V1-1005 | V1-1006 | V2-1001 | V2-1002 | V2-1003 |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 154,810,982 | 158,812,169 | 165,617,751 | 161,038,169 | 161,990,515 | 164,662,011 | 149,968,921 | 152,292,116 | 167,999,980 |
| 1961 | 184,816,416 | 191,481,726 | 204,741,885 | 194,614,773 | 192,597,473 | 196,712,799 | 176,228,711 | 181,324,170 | 204,109,659 |
| 1962 | 193,942,990 | 207,650,554 | 216,780,875 | 211,245,523 | 207,702,218 | 211,906,062 | 190,619,017 | 199,602,818 | 215,829,478 |
| 1963 | 197,359,432 | 204,214,989 | 223,962,982 | 220,136,193 | 209,382,724 | 216,348,689 | 198,038,617 | 199,972,919 | 215,867,728 |
| 1964 | 204,277,839 | 210,759,388 | 221,832,809 | 216,854,707 | 210,957,827 | 217,416,164 | 197,123,370 | 208,538,321 | 222,302,188 |
| 1965 | 212,735,370 | 222,691,660 | 233,007,693 | 231,356,927 | 226,202,388 | 224,470,934 | 210,557,190 | 219,756,147 | 234,317,532 |
| 1966 | 217,992,043 | 218,291,507 | 239,959,051 | 232,629,841 | 223,528,133 | 227,151,548 | 216,679,132 | 218,286,484 | 235,744,147 |
| 1967 | 227,324,481 | 228,800,558 | 243,940,045 | 234,766,803 | 229,677,422 | 231,152,905 | 219,972,119 | 224,370,747 | 236,516,842 |
| 1968 | 236,389,006 | 243,371,598 | 253,815,443 | 244,817,778 | 244,125,763 | 242,622,985 | 230,877,144 | 234,112,966 | 251,873,569 |
| 1969 | 229,598,919 | 232,967,958 | 248,424,467 | 241,196,500 | 229,869,576 | 236,673,709 | 225,813,592 | 230,716,257 | 248,145,011 |

## Annual ensemble level and dispersion

| Year | V1 mean | V1 SD | V1 CV | V2 mean | V2 SD | V2 CV | V2-V1 shift |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 161,155,266 | 3,966,420 | 2.461% | 156,753,672 | 9,808,613 | 6.257% | -2.731% |
| 1961 | 194,160,845 | 6,566,842 | 3.382% | 187,220,847 | 14,846,377 | 7.930% | -3.574% |
| 1962 | 208,204,704 | 7,751,818 | 3.723% | 202,017,104 | 12,777,457 | 6.325% | -2.972% |
| 1963 | 211,900,835 | 10,099,163 | 4.766% | 204,626,421 | 9,783,180 | 4.781% | -3.433% |
| 1964 | 213,683,122 | 6,242,544 | 2.921% | 209,321,293 | 12,607,657 | 6.023% | -2.041% |
| 1965 | 225,077,495 | 7,238,410 | 3.216% | 221,543,623 | 11,980,600 | 5.408% | -1.570% |
| 1966 | 226,592,020 | 8,575,446 | 3.785% | 223,569,921 | 10,573,776 | 4.730% | -1.334% |
| 1967 | 232,610,369 | 6,102,351 | 2.623% | 226,953,236 | 8,569,358 | 3.776% | -2.432% |
| 1968 | 244,190,429 | 5,604,074 | 2.295% | 238,954,560 | 11,304,567 | 4.731% | -2.144% |
| 1969 | 236,455,188 | 7,325,721 | 3.098% | 234,891,620 | 11,736,623 | 4.997% | -0.661% |

The v2 means are below v1 in every year. Two annual means exceed the proposed automatic +/-3% uniform-shift condition: 1961 (-3.574%) and 1963 (-3.433%). The range of annual mean shifts is 2.913 percentage points (-3.574% to -0.661%), so this cannot be certified as an automatic uniform <=3% shift.

V2 dispersion is above the maximum observed v1 three-seed subset coefficient of variation in seven of ten years: 1960, 1961, 1962, 1964, 1965, 1968, and 1969. This is the remaining diagnostic concern.

| Year | V1 3-seed CV min | V1 3-seed CV max | V2 CV | V2 within range |
|---:|---:|---:|---:|---:|
| 1960 | 1.016% | 3.700% | 6.257% | no |
| 1961 | 0.823% | 5.237% | 7.930% | no |
| 1962 | 0.987% | 5.795% | 6.325% | no |
| 1963 | 1.729% | 6.727% | 4.781% | yes |
| 1964 | 1.247% | 4.257% | 6.023% | no |
| 1965 | 0.782% | 4.988% | 5.408% | no |
| 1966 | 1.416% | 5.588% | 4.730% | yes |
| 1967 | 0.517% | 3.941% | 3.776% | yes |
| 1968 | 0.296% | 3.615% | 4.731% | no |
| 1969 | 0.811% | 4.573% | 4.997% | no |

## V1 internal three-versus-three calibration

All v2 annual mean shifts are inside the empirical v1 three-seed-versus-three-seed split range. That result supports the sign-off's conclusion that the original same-seed test cannot identify a taxonomy failure.

| Year | V1 split minimum | V1 split maximum | V1 split SD | V2 shift inside range |
|---:|---:|---:|---:|---:|
| 1960 | -3.577% | +0.674% | 1.296% | yes |
| 1961 | -4.559% | +1.309% | 2.053% | yes |
| 1962 | -4.787% | -0.633% | 1.352% | yes |
| 1963 | -7.494% | +1.827% | 2.828% | yes |
| 1964 | -4.589% | +0.776% | 1.668% | yes |
| 1965 | -4.441% | +0.555% | 1.490% | yes |
| 1966 | -5.706% | +3.230% | 2.798% | yes |
| 1967 | -3.389% | +2.378% | 2.032% | yes |
| 1968 | -2.743% | +0.671% | 1.430% | yes |
| 1969 | -4.662% | +2.817% | 2.346% | yes |

## Decade level and curve shape

| Ensemble | Per-seed decade totals | Mean | Sample SD |
|---|---|---:|---:|
| V1 | 2,059,247,478; 2,119,042,107; 2,252,083,001; 2,188,657,214; 2,136,034,039; 2,169,117,806 | 2,154,030,274 | 65,685,327 |
| V2 | 2,015,877,813; 2,068,972,945; 2,232,706,134 | 2,105,852,297 | 113,020,773 |

The v2 decade mean is -2.237% versus v1. It is inside the v1 three-versus-three decade split range of -4.471% to +1.180% (split SD 1.827%).

Curve shape is close despite the lower level:

- Pearson correlation of annual ensemble means: 0.997579.
- RMSE of each year's share of decade units: 0.091 percentage points.
- Largest share difference: 0.177 percentage points (1969).
- The 1960-indexed mean curve differs by at most 0.0312 (1969).

## Adjudication

The evidence clears the taxonomy of the suspended same-seed conservation failure: level shifts lie within v1's own three-seed split envelope and the decade/shape comparisons do not show a structural year-dependent distortion.

It does **not** satisfy the pre-authorized automatic acceptance condition. Two annual ensemble means are more than 3% below v1 and v2 dispersion exceeds the observed v1 three-seed envelope in seven years. This requires the separate calibration sign-off specified in the Stage 1 adjudication authorization before distance may activate. No taxonomy tuning is proposed here.
