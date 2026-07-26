# D7 genre calibration pass 1

## Post-fallback remeasurement review

Source run: `d7-fallback-remeasure-1001`, 522 weeks through January 1970.

- All D5/D6 fixed probes passed in the source run.
- The largest individual annual share was Traditional Pop at 29.16%, below the
  35% concentration cap.
- Seed 1001 recorded zero Album-capacity reroutes.
- Traditional Pop fallback telemetry fell from 1,999 to 1,141 events, but every
  remaining event still came from `ArtistManager.GetRelatedGenre`.
- The remaining requests were exclusively post-1960 canonical identities, led
  by British Beat (132), British Pop (109), British Blues (95), Contemporary
  Folk (95), Garage Rock (65), Folk Rock (61), Psychedelic Rock (61), and Bossa
  Nova (61).

The regenerated mixed three-seed comparison is in
`D7RAdjustedHistoricalGenreCalibrationSummary.md`. It has overall cell MAE
1.270 percentage points. Under the provisional target-dependent tolerances,
118 of 420 cells across 30 genres remain outside the envelope. The largest
errors remain Traditional Pop high, Soul/R&B low, and late Rock and Roll high.
Seeds 1002 and 2007 are older measurements, so the mixed mean is diagnostic and
must not be treated as an all-post-fix acceptance result.

## Evidence-backed candidate

This pass completes the remaining related-genre mappings and starts the first
catalog/format calibration:

- all 42 canonical identities now have explicit related-genre choices;
- R&B no longer becomes a terminal catalog identity after 1965; its authored
  baseline now retains a declining late-decade tail;
- Traditional Pop, Rock and Roll, Jazz, Baroque Pop, Sunshine Pop, and Blues
  Rock receive lower or more strongly declining catalog curves;
- British Beat's 1963 seed year is bounded separately from its 1964 break;
- British Blues changes from an early spike/decline to a restrained 1964 onset
  and growth through 1969;
- Gospel and Classical receive stronger authored specialist curves;
- centered format orientation strength changes from 0.22 to 0.60 after the
  remeasurement confirmed zero capacity reroutes;
- Psychedelic Rock and Classical receive historically appropriate Album
  affinity, while Traditional Pop and Jazz lose excess Album affinity and
  Soul/Funk become more Single-oriented at the format-economics seam.

The format multiplier remains exactly centered against accepted Album
opportunity. A new fixed sweep proves bounded, monotonic response across the
complete catalog and equal-input endpoint ordering. Another fixed probe covers
all canonical related-genre mappings and fails on any enabled fallback event.

## Verification and stop

- `dotnet build "Label Man.sln" --no-restore`: pass, zero errors and the existing
  unused-event warning.
- The first fixed-probe attempt reached managed probes and stopped on one stale
  hard-coded Jazz supply expectation. The expectation was updated from the old
  0.520 value to the newly authored 0.424 value.
- The console-subsystem Godot executable then twice crashed at engine startup,
  including on a scene-free `--quit` check.
- The sibling Godot executable completed
  `d7-genre-calibration-pass1-probes-r4-1001` for one week with exit code 0.
  Its Traditional Pop fallback file contains only the header, and its completed
  week proves the pre-week D5/D6 suites passed.

No long simulation was started. The next evidence step is one 522-week seed-1001
measurement of this candidate before any second magnitude pass.
