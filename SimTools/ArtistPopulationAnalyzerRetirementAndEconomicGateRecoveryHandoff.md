# Analyzer Retirement and Album Economic-Gate Recovery Handoff

Status: **IMPLEMENTED / VALIDATE FROM RAW TELEMETRY**

Date: 2026-07-18

This handoff retires `analyze-market-clearing-format-memory.mjs`. It supersedes
earlier handoffs wherever they require that analyzer or treat one of its passes
as acceptance evidence. Historical handoffs and their run records remain
evidence of what was attempted; they are not active instructions to restore or
repair the script.

## Decision

The monolithic analyzer is removed. It combined authoritative economic data with
new diagnostic ledgers, had an incomplete responsive-memory lifecycle model, and
did not validate settlement calendar years against the established weekly
calendar. Maintaining it has obscured the simulation problem instead of making
the gate easier to trust.

The established telemetry is authoritative again:

- `weeks.csv`: completed tick identity, calendar year, and total market units;
- `market-revenue.csv`: annual units, gross, label net, distribution income, and
  market net by format;
- `decade-annual-rollup.csv`: annual population/release/economic checkpoint;
- `release-capacity.csv`: rolls, successes, and failure mix;
- `fork-ratios.csv`: pre/post-memory format decision economics;
- `album-projects.csv`: scheduled projects, drops, terminal state, and project
  economics.

`completed-week-settlement*.csv`, `market-clearing-weekly.csv`,
`market-spillover-weekly.csv`, and `format-memory-*.csv` remain useful diagnostic
ledgers. They may explain a result, but they do not override the established
annual telemetry or independently declare a gate pass.

## Established failure

The M4 candidate
`d6-bounded-spillover-memory-closure-through-1965-1001-r3` genuinely failed. The
1965 Album result was `6,945,634 / 11,129,114 = 0.624096`, below the `0.80`
floor. Re-keying settlement rows to the authoritative `weeks.csv` calendar
reproduced the same total, so this was not merely an analyzer read error.

The causal chain was:

1. Album serviceable intent was only `7,949,081`, or `0.7143x` control. Perfect
   clearing could not have met the gate.
2. Physical Album backorders accumulated while uncharted Albums were denied
   ordinary replenishment unless they first passed a broad-market breakout score.
3. Every prepared Album and promo release was recorded with
   `releaseTimeExpectedNet = -productionCost`, even though a deterministic
   release-time prior had already been calculated.
4. `lastRevisionAge` defaulted to zero, so every first responsive-memory
   observation was falsely classified as replacing an earlier revision.

## Implemented recovery

The recovery is deliberately structural rather than a gate-floor adjustment.

- Live uncharted Albums with positive raw demand and physical backorders may use
  the existing regional restock path without first winning a Singles-oriented
  breakout score. Distribution coverage, distance reach, service level, store
  capacity, and inventory accounting still bound every replenishment.
- Standalone and linked Albums now carry the deterministic pre-memory Album prior
  into `releaseTimeExpectedNet`. Promo Singles carry their deterministic Single
  prior. Opportunity scale is the maximum of prior magnitude and production
  cost.
- Responsive-memory observations now start at age `-1`, have explicit ordinals,
  reject duplicate/backward/post-final revisions, and permit a final revision to
  replace a provisional revision at the same age.
- Completed settlements are dated to the completed audit checkpoint rather than
  the preceding Friday callback, aligning settlement year with `weeks.csv`.
- Fixed probes cover Album replenishment neutrality outside the live Album path
  and the first/replacement/final memory-revision lifecycle.

No demand keyframe, genre acceptance, common-market capacity, gate threshold, or
control artifact was changed.

## Validation contract

Use unique run names and evaluate gates directly from the authoritative CSVs.
Do not recreate a replacement all-in-one analyzer during this recovery.

1. M1: build, `git diff --check`, and the combined D5/D6 fixed probes.
2. M2: 52-week disabled seed-1001 replay. All comparable established streams
   must be byte-identical to `d6-market-clearing-disabled-52-1001`.
3. M3: enabled 104-week seed-1001 candidate and deterministic repeat. Compare
   like-named CSVs byte-for-byte, excluding only performance timing.
4. M4: run through the date-complete 1965 checkpoint. Compare the authoritative
   1964 and 1965 annual rows with
   `d6-transition-envelope-decade-control-1001`. The inherited format and
   economic floors remain unchanged.
5. M5: launch the full decade only if M4 passes.

For each completed year and format, sum the `Annual` rows in
`market-revenue.csv`; compare candidate/control values for `totalMarketUnits`,
`gross`, `labelNet`, and `marketNet`. Cross-check total annual units and release
counts against `weeks.csv`, `decade-annual-rollup.csv`, and
`release-capacity.csv`. A diagnostic stream disagreement is reported and
investigated, but it never silently replaces the established value.

Stop only for a genuine failed inherited gate, a non-reconciled authoritative
stream, a nondeterministic repeat, or a build/probe failure. M5 is not authorized
by an analyzer pass; it is authorized by an M4 pass calculated from the raw
telemetry above.
