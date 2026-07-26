# D7 genre calibration pass 2

## Source diagnosis

The pass-1 seed-1001 decade measurement showed that Traditional Pop fallback
events and Album-capacity reroutes were both zero. The remaining genre errors
therefore did not come from either defensive branch.

The Album-demand explanation stream exposed the dominant cancellation. Live V2
calculated an enabled genre buyer pool and then multiplied it by an opportunity
normalization that restored the accepted legacy pool exactly. In 1969 the
enabled/accepted Album-pool ratios included:

- Traditional Pop: 0.20
- Jazz: 0.51
- Soul: 3.13
- Psychedelic Rock: 4.98
- British Blues: 6.13
- Classical: 3.05
- Gospel: 2.00

Consequently, the Album path erased most of the authored Traditional Pop/Jazz
decline and most of the emerging/specialist growth before record quality,
awareness, inventory, market clearing, or format tilt acted.

The pass-1 1960 result also proved that a literal target-share artist prior is
not a neutral commercial prior. Major-label Adult/Album channels converted
Traditional Pop, Jazz, Easy Listening, and Doo-Wop identities more efficiently
than Soul, R&B, Blues, and Classical identities.

## Implemented correction

- Live Album demand now retains `EnabledPreTiltBuyerPool`; the accepted legacy
  pool remains available as diagnostic and AI-prior decomposition, and the
  disabled path is unchanged.
- The enabled-only 1960 identity prior is conversion-aware while preserving one
  genre draw per artist, full normalization, and the Surf Rock/Blues Rock
  pre-emergence exclusions.
- Prospective transition demand now uses a 0.05 discovery floor rather than
  0.20, preserving materially more of the authored acceptance range.
- Catalog magnitude and format corrections are limited to the measured
  outliers: Acid Rock, Bubblegum, Easy Listening, Psychedelic Rock, British
  Blues, Soul, Gospel, and Country.
- Runtime target tables remain analysis-only. No realized sales, chart,
  backorder, or target-share data enters a release decision.

## Verification

- `dotnet build "Label Man.sln" --no-restore`: passed with zero errors and the
  inherited unused `ChartManager.OnGenreMomentumChanged` warning.
- Final D5/D6 fixed-probe run:
  `d7-genre-calibration-pass2-final-probes-r2-1001`; exit code 0.
- `git diff --check`: passed before handoff generation.
- No 522-week run was started, per user instruction.

The 104-week safety checkpoint
`d7-genre-calibration-pass2-checkpoint-104-1001` completed without a
catastrophic fail-fast abort. It recorded zero Traditional Pop fallback events,
zero Album-capacity reroutes, and a largest annual genre share of 17.94%.

Early checkpoint movement versus the pass-1 seed-1001 run:

| Genre | Pass 1 1960 | Checkpoint 1960 | Target 1960 |
| --- | ---: | ---: | ---: |
| Traditional Pop | 23.43 | 17.94 | 15.00 |
| Jazz | 8.90 | 6.35 | 6.22 |
| Doo-Wop | 7.80 | 4.98 | 5.09 |
| Rock and Roll | 8.98 | 12.80 | 13.54 |
| R&B | 6.81 | 9.37 | 11.32 |
| Classical | 1.10 | 1.54 | 2.04 |
| Soul | 3.51 | 4.61 | 7.92 |

Traditional Pop also moved from 22.43% to 14.80% in 1961, against a 14.98%
target.

After that checkpoint, one final prior-only transfer moved three percentage
points from Teen Pop and one point from Country to Soul. That final adjustment
is build- and fixed-probe-verified but deliberately not simulation-measured.
The checkpoint artifacts must therefore not be represented as final-candidate
acceptance evidence.

