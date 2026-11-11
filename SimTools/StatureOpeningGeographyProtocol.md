# Release-level geography shadow — 2026-11-06

This specifies the comparison before inspecting the new release census or shadow results.

Scope: the fixed 20 RegionalName acts per seed, in seeds 1001 and 2002. Observe qualifying release contributions for all seeded histories, including count-only histories, but change no gameplay inputs. The live seeder, source ageing, losses, joins, RNG, peer references, sales, TV, national following scores and requested budgets remain observed. Shadows run offline with independent hysteresis from the calibration effective date.

Rules to compare:

- `neutral`: original regional source counts, validating the reconstruction against every observed C# band/status and regional input.
- `home_only`: each named-origin release contributes to its origin only. This deliberately ignores extra market counts and is a concentration stress scenario.
- `home_count_average`: enumerate every footprint of the stored market count containing the named origin. Allocate by population within each footprint, then average the allocations uniformly. This is an explicit averaged input scenario; it is neither observed territory nor an expectation of nonlinear standing outcomes.
- `crossover_national`: same as `home_count_average`, except positive Top 40 peaks are treated as national crossovers. The history generator describes crossover, but its geographic metadata remains regional. This rule measures that ambiguity rather than declaring it resolved.

Missing or invalid named origins/counts must fail rather than silently choose a territory. Raw origin names are resolved against region IDs and region display names. Count-only histories outside the targeted cohort stay observed.

For surviving young source stock, use opening release-weighted allocations separately by joining year. For the adult source pool, use the opening adult release mixture, including for later ageing transfers; the observed provenance does not retain adult source by release year. Preserve each observed national young cohort and the observed adult source total exactly. This adult mixing convention and relocation of survivors are frozen-world assumptions, not a rerun of regional audience survival.

Rebuild each regional follower count by removing observed source and adding the conserved relocated source. Recompute density, maximum draw, record reach (maximum of observed smoothed sales response and new density) and conversion reach before band evaluation. Do not impose audience capacity caps on diagnostic relocated counts or alter national following scores. Flag missing evidence rather than turning it into zero.

Validation: source-accounting fixtures, new allocation/conservation/missing-evidence fixtures, build, exact completion markers and empty error logs. Run a 39-week seed-1001 control/treatment observation pair and compare all original 150 CSVs against `stature-origin-smoke-v1`, permitting only two added geography CSVs. If historical baseline parity fails, investigate rather than claim neutrality. Then run 104-week pairs in both seeds and check paired release/seed censuses and current run provenance. Retain hashes and exact requests.

Report release market/peak distributions and crossover ambiguity, national stock conservation, neutral reconstruction, higher/lower bands, changed draw/reach and launch-envelope increases/decreases relative to the observed standing proposal, by year/arm/rule. Include distinct acts and inspect changed launches. No source-removal result substitutes for the geography comparison. No causal economic result or gameplay correction is claimed.
