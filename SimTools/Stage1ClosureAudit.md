# Directive 4b - Stage 1 closure audit

Date: 2026-07-10

Taxonomy was held frozen throughout this measurement. All disabled runs used `distanceModelEnabled = false`.

## Six-v1 versus six-v2 conservation

| Year | V1 mean | V1 CV | V2 mean | V2 CV | V2-V1 shift |
|---:|---:|---:|---:|---:|---:|
| 1960 | 161,155,266 | 2.461% | 156,467,697 | 5.110% | -2.909% |
| 1961 | 194,160,845 | 3.382% | 186,387,049 | 5.658% | -4.004% |
| 1962 | 208,204,704 | 3.723% | 201,425,172 | 4.473% | -3.256% |
| 1963 | 211,900,835 | 4.766% | 205,191,276 | 3.584% | -3.166% |
| 1964 | 213,683,122 | 2.921% | 207,821,630 | 4.041% | -2.743% |
| 1965 | 225,077,495 | 3.216% | 219,057,649 | 3.955% | -2.675% |
| 1966 | 226,592,020 | 3.785% | 219,701,153 | 3.794% | -3.041% |
| 1967 | 232,610,369 | 2.623% | 226,599,739 | 3.213% | -2.584% |
| 1968 | 244,190,429 | 2.295% | 235,798,212 | 3.822% | -3.437% |
| 1969 | 236,455,188 | 3.098% | 231,132,308 | 4.145% | -2.251% |

All 1960 values:

| Seed | V1 units | V2 units |
|---:|---:|---:|
| 1001 | 154,810,982 | 149,968,921 |
| 1002 | 158,812,169 | 152,292,116 |
| 1003 | 165,617,751 | 167,999,980 |
| 1004 | 161,038,169 | 161,683,066 |
| 1005 | 161,990,515 | 159,808,618 |
| 1006 | 164,662,011 | 147,053,483 |

The v1 decade totals are 2,059,247,478; 2,119,042,107; 2,252,083,001; 2,188,657,214; 2,136,034,039; and 2,169,117,806. Their mean is 2,154,030,274 (CV 3.049%).

The v2 decade totals are 2,015,877,813; 2,068,972,945; 2,232,706,134; 2,083,942,847; 2,123,675,886; and 2,012,315,680. Their mean is 2,089,581,884 (CV 3.918%), a -2.992% shift.

Curve shape is preserved: Pearson correlation of annual ensemble means is 0.999071; decade-share RMSE is 0.049 percentage points; and the largest annual decade-share difference is 0.094 points. Annual mean shifts range from -4.004% to -2.251% (1.753 percentage points wide), which is a near-uniform level shift rather than a material change of curve shape. The six-seed evidence therefore accepts Stage 1 conservation without parameter calibration.

## Enabled regression, seeds 1001-1003

| Seed | Crossover | 1960 overall/adult/youth mix | Standalone through 1963 | Standalone 1969 | Paired Pearson delta | Paired all-decade Top-40 median delta |
|---:|---:|---|---:|---:|---:|---:|
| 1001 | 1967 | 25.527% / 54.313% / 12.165% | 0.000% | 42.585% | +0.138744 | +1.0 weeks |
| 1002 | 1967 | 25.033% / 55.095% / 11.853% | 0.000% | 44.398% | +0.015949 | 0.0 weeks |
| 1003 | 1967 | 24.659% / 53.921% / 11.437% | 0.000% | 52.462% | +0.133620 | +1.0 weeks |

All three crossover years are inside the hard 1966-1969 window. All 1960 mix values pass the 18-28% overall, 45-75% adult, and 4-15% youth hard bands. Standalone ordering passes: no standalone decisions before 1964 and positive 1969 share in every seed. Paired Pearson means exceed the disabled pair in every seed. The all-decade closed Top-40 medians are within the +/-1-week paired guard; annual 1960-64 medians also remain within one week.

Youth compilation production is nonzero and rises strongly: annual compilation-album counts move from 764-784 in 1960 to 3,684-4,315 in 1969; `youthCompCompleted` moves from 13-27 to 2,687-3,244.

The runs used lean output mode, which suppresses per-record and breakout CSV rows only. It does not alter simulation behavior; the annual rollups, lifecycle stream, album decisions, album composition, chart, finance, and all other regression data were retained and used for these gates.

## Determinism

The enabled seed-1001 repeat has the same 34 emitted CSV files as the primary run. Every paired file has the same byte count and SHA-256 hash: 34/34 byte-identical.

## Stage 1 disposition

Stage 1 is closed and accepted. Distance may now activate for Stage 2 calibration. The conservation acceptance applies only to the fixed taxonomy measured here; it does not authorize adoption, album, or other parameter tuning.
