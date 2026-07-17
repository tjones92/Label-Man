# Artist population daily talent market and fail-fast decade handoff

## Mission and authority

Replace enabled-mode label-order scouting with a deterministic rolling daily talent market, then validate the repaired runtime-label path directly through the retained seed-1001 G6 decade comparison.

This is the authoritative next implementation handoff after `ArtistPopulationRuntimeLabelGenerationRepairHandoff.md`. Preserve that repair: runtime-founded labels must still receive deterministic tier-valid operating profiles, empty entry, operating target one, canonical capacity, zero history, and no birth-chart-week signing.

This handoff makes two owner decisions explicit:

1. Labels may scout on any of the seven calendar days. Enabled scouting must not remain one shared weekly event.
2. The proposed standalone 260-week maturity run is waived as redundant. After the focused 104-week proof and retained-control prefix proof pass, proceed directly to one fail-fast 522-week seed-1001 decade run.

Do not run the decade until the daily market, telemetry, probes, disabled replay, 104-week repeat, and retained-control proof all pass. Do not run seeds 1002/1003 or the holdout.

Preserve all failed G6 artifacts and the audit record. Never overwrite or relabel the failed decade as a pass.

## Why another correction is required

The enabled 104-week runtime-profile validation proved that the zero-output shell defect is repaired:

```text
runtime founders        80
runtime first signings  36
runtime re-signings     49
runtime releases        43
```

The 49 re-signings are not evidence that the runtime cohort naturally needed that many experienced artists. They expose market-order starvation.

Production currently processes enabled vacancy-responsive scouting from `RosterManager.OnWeekEnded`. `TimeManager` emits that callback only at Friday day-end, so every eligible label is currently considered at the same weekly boundary. The handler iterates the registered label list in order and mutates artist ownership immediately after each successful signing. `ChartManager.RegisterLabel` appends runtime-founded labels, so launch labels normally evaluate first and live-founded labels normally evaluate last.

The retained 104-week detail confirms the mechanism:

- all 49 runtime re-signings came through `ExperiencedProduction` / `ExperiencedFallback`;
- every one reported `freshLaneCount=0`;
- 47 artists had one prior contract and two had two prior contracts;
- 44 had `InitialLegacy` provenance and five had `RuntimeFormation` provenance;
- runtime first signings did not begin until week 79; and
- five runtime labels signed twice.

In shared scouting weeks, earlier launch labels consumed fresh candidates before appended runtime labels evaluated. The resulting lane was not a frozen view of market supply. Registration chronology became an unhistorical priority rule.

The defect is therefore systemic:

```text
shared weekly trigger
    -> launch-first mutable iteration
    -> later labels observe depleted fresh supply
    -> runtime labels fall back to experienced artists
    -> re-signing concentration and distorted label survival/output
```

Do not fix this by sorting runtime labels earlier, rotating the list, reserving artists for runtime labels, or granting runtime labels a separate pool. Those treatments merely replace one privileged order with another.

## Required market model

### 1. Give every vacancy a rolling daily scouting appointment

Enabled-mode scouting must be serviced from the calendar's daily callback. Labels can be due on Monday through Sunday, including weekends. A label's appointment belongs to its current vacancy, not to a universal weekday and not permanently to the label.

When an enabled active label first acquires a serviceable vacancy, assign a deterministic offset in `[0, 6]` from the first eligible calendar day. Derive it from isolated stable inputs such as:

```text
simulation seed
label identity
vacancy generation / vacancy-open date
fixed DailyTalentMarket namespace
```

Do not consume the global simulation RNG. Persist the resulting `nextScoutingDate`; do not recalculate it from collection order each frame or reload.

If the vacancy remains after an appointment, its next appointment is exactly seven calendar days later. If the vacancy closes, clear the appointment. A later vacancy receives a new vacancy generation and a newly derived appointment.

An appointment is an opportunity to evaluate and make at most one offer. It does not guarantee a signing and does not authorize hiring above `OperatingRosterTarget` or canonical capacity.

### 2. Preserve the no-birth-chart-week boundary

A runtime-founded label cannot sign in the same chart week in which it was born. If its derived initial appointment falls inside that week, move the appointment forward by seven days until the current chart week is strictly later than `birthWeek`.

This remains a structural invariant, not a probability. The daily conversion must not accidentally shorten the boundary from chart week to elapsed days.

### 3. Move only the enabled path to daily service

Subscribe the enabled scouting coordinator to `TimeManager.OnDayStarted`. Unsubscribe symmetrically during teardown.

Keep the disabled path byte-frozen. It may retain its existing weekly callback and RNG order. Ensure the enabled path is not also called from `OnWeekEnded`; no Friday or week-end double scouting is permitted.

Weekly release processing, contract review, finance, lifecycle formation, and other unrelated weekly systems remain weekly unless a separate accepted handoff says otherwise.

### 4. Clear each calendar day as one two-phase market

Spreading the present mutable loop across days is insufficient because multiple labels can be due on the same date. Each due date must be processed as a small simultaneous market.

Phase A — freeze and nominate:

1. Collect every active, serviceable, affordable, vacancy-bearing label whose persisted appointment equals the current date.
2. Freeze the eligible unsigned-artist supply snapshot for that date before any due label signs.
3. From that common snapshot, let every due label run the existing deterministic regional discovery, fresh/experienced lane construction, scoring, affordability, and Recovery policy.
4. Each label may nominate at most one candidate and one proposed contract.
5. Do not mutate artist ownership, roster membership, or unsigned-pool membership during nomination.

Phase B — resolve and commit:

1. Group nominations by artist.
2. A sole valid offer wins if the artist and label still satisfy all hard invariants.
3. When several labels nominate the same artist, resolve the artist's choice with the deterministic offer utility specified below.
4. A collision loser receives no second candidate or second offer that day. Its vacancy persists and its next appointment is seven days later.
5. Commit winners only after all collisions have resolved. Apply commits in a stable identity order and assert that this order cannot affect the winner set.

The following must always hold:

```text
one label       <= one evaluation per appointment
one label       <= one offer per appointment
one label       <= one signing per appointment
one artist      <= one accepted offer per date
one vacancy     <= one service appointment per seven-day window
roster headcount <= operating target and canonical capacity
```

### 5. Preserve discovery realism

Do not create one global omniscient artist auction. Each label must still discover candidates through its existing regional pool, scouting ability, genre preferences, national fallback rules, availability, affordability, and stable-hash slate construction.

This matters historically and mechanically: a small local label can discover a promising club act that a corporate major never saw, did not scout, or did not rate. A major's budget and reach must not entitle it to every artist in the global pool.

Only offers to the same artist collide. Labels that discover different candidates can sign independently on the same date.

### 6. Let the artist choose among colliding offers

Do not resolve collisions by label registration order, label age, raw budget, or tier alone. Use deterministic artist-relative utility over the actual offer and relationship.

The utility must include bounded normalized components for at least:

- preferred/secondary genre fit;
- artist home-region proximity and the label's local presence;
- offered royalty;
- offered advance;
- label reputation;
- distribution/reach;
- roster opportunity, so an artist can prefer a label where they are likely to receive attention; and
- a small isolated artist-label affinity term to represent personal fit.

Design constraints:

```text
sum(genre fit + locality + royalty + roster opportunity)
    >= sum(reputation + reach + advance)

no single component weight > 0.25
artist-label affinity weight <= 0.08
```

The exact normalized formula is an implementation choice, but it must satisfy the fixed probes. A strong major can win a competitive artist through genuinely better fit and terms; it cannot win automatically because it is a major. A well-matched local label with strong royalty and roster opportunity can beat a distant corporate giant.

Use an isolated stable hash for the affinity term and any exact tie-break. Do not consume global RNG. The last tie-break must use stable label identity, never list position.

### 7. Retain the accepted fresh/experienced policy

This handoff changes service timing and contention. It does not reopen the accepted candidate eligibility, fresh-potential scoring, experienced-production scoring, Recovery threshold fallback, comeback exhaustion, contract evidence, or affordability rules.

Every due label must evaluate the same lanes it would have evaluated against the frozen start-of-day supply. The two-phase market must not silently change thresholds or grant collision losers an experienced fallback after losing a fresh candidate.

### 8. Keep runtime profiles coherent

Preserve `RuntimeLabelProfileFactory` and its tier-valid pairings. Runtime style/archetype, budget, scouting, production, marketing, reach, release cadence, risk, loyalty, and financial initialization must remain one deterministic coherent profile. Do not return to independent style and budget rolls.

The daily market must treat launch and runtime labels under the same discovery and offer rules. Runtime labels receive no special priority after the birth-week restriction ends.

## State and integration requirements

Add explicit enabled-only state sufficient to make daily scheduling inspectable and save-safe:

```text
vacancyGeneration
vacancyOpenedDate
nextScoutingDate
lastScoutingDate
lastScoutingOutcome
```

Use existing serialization conventions. Reconstructing missing state for legacy enabled saves may use the same isolated schedule function, but must not touch disabled behavior.

Centralize the authoritative daily clearing operation. Avoid callbacks that let each label independently mutate the market, since callback subscription order would recreate the same defect in another form.

Recheck hard eligibility immediately before commit. If lifecycle activity earlier that day invalidated a label, artist, vacancy, or offer, reject the commit with a structured reason. Do not search for a replacement inside the same appointment.

## Required telemetry

Add an enabled-only daily aggregate stream, or extend an existing enabled stream without catastrophic row multiplication, with at least:

```text
date
chartWeek
eligibleVacancies
dueLabels
supplySnapshotCount
freshSupplySnapshotCount
experiencedSupplySnapshotCount
nominations
uniqueNominatedArtists
collisionArtists
collisionOffers
acceptedOffers
collisionLosers
invalidatedBeforeCommit
```

Extend per-label scouting telemetry sufficiently to prove:

```text
labelId
labelOrigin
labelTier
vacancyGeneration
vacancyOpenedDate
scheduledScoutingDate
actualScoutingDate
appointmentOrdinal
serviceMode
freshLaneCount
experiencedLaneCount
selectedArtistId
selectedLane
offerOutcome
collisionOfferCount
winnerLabelId
artistChoiceUtility
artistChoiceComponent values
nextScoutingDate
```

Structured outcomes must distinguish at least:

- `NoVacancy`;
- `NotYetEligibleBirthWeek`;
- `EstimatedAdvanceUnaffordable`;
- `NoCandidate`;
- `NoQualifyingCandidate`;
- `Nominated`;
- `AcceptedUncontested`;
- `AcceptedArtistChoice`;
- `LostArtistChoice`;
- `InvalidatedBeforeCommit`; and
- `ServiceSatisfied`.

Telemetry must show that daily appointments occur on all seven weekdays over a sufficiently populated run. It must also show appointment spacing, same-day frozen supply, collision decisions, and whether runtime signings are first contracts or re-signings.

Do not add telemetry or global RNG draws to the disabled stream set.

## Fixed probes

Retain all passing D5/D6 probes, including probe 62 for tier-valid runtime profiles. Add deterministic production probes for at least:

1. initial vacancy appointments cover offsets zero through six for controlled identities;
2. appointments can occur on every calendar weekday, including Saturday and Sunday;
3. an unfilled vacancy reschedules exactly seven calendar days later;
4. closing a vacancy clears its appointment;
5. reopening a vacancy creates a new vacancy generation and schedule;
6. a runtime founder cannot sign in its birth chart week even when its derived appointment falls there;
7. a runtime founder becomes eligible on the first later due appointment;
8. disabled mode retains the weekly path and exact RNG/stream behavior;
9. enabled mode is not also evaluated at week end;
10. every label due on one date sees the same frozen unsigned-supply snapshot;
11. nomination cannot mutate ownership or remove candidates;
12. two labels nominating different artists can both sign on the same date;
13. two labels nominating one artist produce exactly one winner;
14. a collision loser receives no second offer that day;
15. commit order cannot change the daily winner set;
16. registration order cannot change nominations, collisions, or winners;
17. a later-created runtime label can beat an earlier launch label on artist-choice utility;
18. a small local specialist beats a distant major when genre, locality, royalty, and roster opportunity dominate;
19. a well-matched strong major beats a weak-fit small label when its complete offer is genuinely better;
20. raw budget or tier alone cannot decide a collision;
21. exact utility ties use stable identity rather than list position;
22. no component exceeds its weight limit and affinity remains at or below `0.08`;
23. no label receives more than one evaluation, offer, or signing per appointment;
24. no artist receives more than one accepted offer per date;
25. operating target and canonical capacity remain hard stops;
26. daily scheduling and collision resolution consume no global RNG;
27. tier-valid runtime profiles remain paired and unchanged; and
28. telemetry reports the actual production branch without independently re-enumerating candidates.

## Validation ladder

### M0 — retained artifact preflight

Before editing, retain and report the accepted profile-repair evidence:

```text
build                         pass (existing unused-event warning only)
D5/D6 probes                  1-62 pass
disabled 52-week replay       45/45 byte-identical
enabled 104-week founders     80
runtime first signings        36
runtime re-signings           49
runtime releases              43
independent 104-week repeat   54/54 byte-identical
```

Reproduce the 49 re-signing attribution above. If retained artifacts no longer support it, stop and record the mismatch before changing policy.

### M1 — implementation, build, and probes

Implement the daily two-phase market and catastrophic fail-fast runner support. Build without new warnings. Run the complete D5/D6 suite and every new probe. Run `git diff --check`.

### M2 — disabled 52-week byte-exact replay

Run the existing disabled seed-1001 52-week comparison. Require every expected stream to be byte-identical, no new enabled-only streams on the disabled path, and no profile, daily-market, or target stream leakage.

Any mismatch stops the ladder.

### M3 — enabled 52-week structural smoke

Require:

- all seven weekdays represented in due appointments;
- no birth-chart-week signing;
- no double weekly/daily service;
- no multi-owner artist;
- no label multi-signing inside one appointment;
- no operating-target or capacity overshoot;
- same-day snapshot and collision reconciliation exact;
- tier-valid runtime profiles; and
- daily telemetry reconciliation exact.

This is a correctness gate, not a volume calibration gate.

### M4 — enabled 104-week proof and exact repeat

Run seed 1001 for 104 weeks, then repeat independently with identical source and arguments. Require all enabled streams byte-identical between repeats.

Report at minimum:

- runtime founders, active founders, first signings, re-signings, and releases;
- signings by artist provenance and prior contract count;
- appointment weekday distribution for launch and runtime labels;
- due labels, nominations, collisions, wins, and losses by label origin/tier;
- days from vacancy open to first appointment and first signing;
- artist-choice wins by utility components;
- fresh and experienced supply/slate counts by origin; and
- all structural reconciliations.

Do not impose a hand-tuned target that re-signings must be zero. They must be explainable and must no longer arise from a systematic `freshLaneCount=0` disadvantage caused by list order.

### M5 — retained G6 control reuse proof

Do not rerun a full disabled decade merely because enabled implementation changed.

Prove that the retained `d6-transition-envelope-decade-control-1001` is still the valid comparison control by establishing all of the following:

1. M2 is byte-exact on every disabled 52-week stream.
2. Disabled source reachability for the daily market, runtime profile factory, enabled telemetry, and fail-fast comparison code is absent or guarded no-op.
3. The current disabled 52-week output prefix matches the corresponding prefix of the retained decade control for every comparable control stream.
4. Seed, configuration, historical inputs, launch population inputs, and runner arguments match the retained control manifest.

If any proof fails, stop and request a new control authorization. If all pass, record that the retained control is reused—not regenerated.

### P5 — 260-week maturity run intentionally waived

Do not perform the previously requested paired 260-week run. It is an intermediate deterministic prefix of the same seed-1001 decade and adds substantial runtime without a distinct policy question once M4 and M5 pass.

Record it as `WAIVED_BY_OWNER_REDUNDANT_WITH_FAIL_FAST_DECADE`, never as `PASS`.

### M6 — one enabled fail-fast G6 decade

Run one enabled seed-1001 522-week decade against the retained G6 control with catastrophic fail-fast enabled. The completed run still uses the inherited annual and decade acceptance bands from the controlling audit/handoff. This document does not loosen those final acceptance criteria.

The purpose of fail-fast is to avoid spending the rest of a decade on a clearly corrupt or catastrophically divergent run. It is not an early rejection mechanism for marginal misses.

## Catastrophic-only fail-fast contract

Add an explicit runner option such as:

```text
--catastrophic-fail-fast
--gate-control-run=d6-transition-envelope-decade-control-1001
```

The option must be off by default. It is valid only when the named control manifest and required comparison rows load successfully.

### Immediate structural aborts

Abort immediately for a correctness failure that invalidates the simulation, including:

- one artist simultaneously owned by multiple labels;
- one artist receiving multiple accepted offers on one date;
- one label receiving multiple signings in one appointment/service window;
- roster above operating target or canonical capacity due to scouting;
- runtime founder signing in its birth chart week;
- inactive/closed label signing or inactive/unavailable artist being signed;
- missing or invalid runtime profile, including incoherent tier pairing;
- NaN, infinity, negative-impossible counts, or invalid finance values;
- date, week, release, contract, or lifecycle chronology reversal;
- daily nomination/collision/commit reconciliation mismatch;
- stream aggregate/detail reconciliation mismatch; or
- missing required control row or incompatible control manifest.

### Completed-year catastrophic divergence aborts

At each completed calendar-year boundary, compare enabled against the same completed control year. Abort only when a core measure ratio is outside the catastrophic band `[0.70, 1.30]`:

```text
successful releases
scheduled Album projects
total units
gross revenue
label net
market net
```

Handle a zero denominator explicitly. A positive enabled value against zero control is not automatically catastrophic; log it and use the controlling invariant or an agreed absolute check. A zero enabled value against materially positive control is catastrophic for releases or scheduled Albums.

`[0.70, 1.30]` is an emergency abort band, not the final acceptance band. Reaching the end still requires the inherited tighter gates.

### Runtime-cohort catastrophic liveness checks

After the end of 1961, abort if runtime founders exist but cumulative runtime signings are still zero. Abort if cumulative runtime releases are still zero after at least one full post-signing release-eligible year and the cohort contains signed active labels with nonzero release capacity.

Do not convert a slow cohort, a small cohort, or a temporary year-to-year dip into a catastrophic rule. Any liveness abort must print the exact denominator, elapsed exposure, and state proving that the expected path was reachable.

### Explicit non-aborts

Do not abort early for:

- a `0.5%`, `1%`, or similarly marginal band miss;
- an ordinary inherited `[0.80, 1.20]` or `[0.85, 1.15]` gate miss that remains inside `[0.70, 1.30]`;
- a single artist, label, genre, format, or week having an unusual but valid result;
- format-mix movement;
- deferred Single-yield weakness;
- one annual ratio barely outside its final acceptance band;
- runtime re-signings merely being nonzero; or
- an incomplete-decade aggregate that cannot yet be compared fairly.

Log non-catastrophic warnings and continue. Final G6 acceptance is decided only after the full 522 weeks complete.

### Graceful abort artifact requirements

Do not rely on an external process kill. On a catastrophic abort, the runner must:

1. record gate name, metric/invariant, enabled value, control value, ratio if applicable, week, date, and explanatory state;
2. write the current endpoint snapshots and flush every open stream;
3. close telemetry cleanly;
4. emit a unique marker such as `CHART_AUDIT_ABORTED_CATASTROPHIC`;
5. return a distinct nonzero exit code; and
6. never emit the normal completion marker.

An ordinary exception remains a failure and must retain its stack/error evidence; do not mislabel it as a measured catastrophic gate.

## Final decade report

If M6 completes, compare the enabled decade to the retained control and report every inherited annual and decade gate, including the original failed late-decade measures:

```text
1968 scheduled Albums
1969 releases
1969 scheduled Albums
```

Also report runtime-cohort founders, first signings, re-signings, releases, Albums, closures, survival, appointment delay, collision outcomes, and label-tier/profile distributions by year. Attribute remaining deficits to the first causal seam supported by telemetry; do not infer from terminal snapshots alone.

Append a dated completion or failure entry to `ArtistPopulationLifecycleAudit.md` only after the ladder stops or completes. Include exact commands, source manifest, artifact paths, checksums/repeat proof, retained-control proof, the P5 waiver, fail-fast status, and the next authorized action.

## Stop conditions and scope boundaries

Stop immediately for:

- any disabled mismatch;
- any global RNG change caused by daily scheduling or collision resolution;
- failure of same-day frozen-supply or collision reconciliation;
- failure of runtime tier/profile pairing;
- failure to prove the retained control reusable;
- any catastrophic fail-fast trigger; or
- any completed-decade inherited acceptance failure.

This handoff does not authorize changing artist supply, formation rate, release cadence, Album decisions, market demand, finance, genre history, format economics, closure policy, operating targets, canonical capacities, or final acceptance bands. If the daily market passes structurally but the decade still fails, diagnose from the new telemetry and write a new bounded handoff before changing another production rule.
