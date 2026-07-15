# Artist population supply-policy redesign handoff

## Mission and authority

Replace the enabled lifecycle's permanent 4,000-artist searchable reserve with an explicit stock/flow labor market that preserves the 7,000-artist mature population, the original 3,000-artist launch allocation, and annual runtime formation while preventing the reserve from acting as a frictionless shelf of indefinitely available replacement quality.

This is the authoritative next-pass handoff for Codex. It follows `ArtistPopulationEconomicYieldInvestigationHandoff.md` and the completed economic-yield checkpoint in `ArtistPopulationLifecycleAudit.md`. Preserve the current uncommitted implementation, diagnostic seam, analyzer, probes 1-54, audit edits, and systemic release-capacity repair. Do not reset or discard them.

This handoff authorizes one specified behavior candidate, its telemetry and fixed probes, paired short validation, and the later validation ladder only when every preceding gate passes. It does not authorize a pool-size sweep, a finance or market-demand offset, weaker artist generation, a release/Album rule change, or silent removal of the reserve.

## Decision in one sentence

Keep the reserve as population, but make **participation in active first-contract search** a bounded, rotating state driven by actual weekly hiring opportunities rather than making every reserve artist permanently searchable from week zero.

## Evidence that governs the redesign

### The boundary result

The current default enabled path materializes 4,000 post-allocation reserve artists, reaches a 7,000-artist registry, and exposes the entire reserve through `unsignedArtists`. In the 1961 baseline, that cohort supplies 2,104 current-year Single records and the treatment reaches approximately `1.16x` units and revenue despite release and Album capacity remaining in band.

The opt-in no-reserve diagnostic preserves the 3,000 launch allocation and 300 annual formations. It finishes 1961 at `1.0163x` units, `1.0195x` gross, `1.0245x` label net, and `1.0188x` market net, with release and Album capacity also in band. This identifies reserve-mediated replacement supply as the binding mechanism for seed 1001. It does not justify deleting the reserve.

Label-lifecycle freeze leaves 1961 units at `1.1526x`, so label survival is an amplifier but not the first-order correction. Dropped control rows are frozen compatibility behavior, not an enabled-path defect. Those findings close comparator, label-lifecycle, release-cadence, format, finance, and market-penalty routes for this pass.

### Source-level cause

The current implementation has four properties that together make the reserve unlike a labor market:

1. `ChartManager._Ready` preserves the 3,000 launch allocation and then calls `ArtistManager.MaterializeEnabledInitialUnsignedReserve`.
2. `MaterializeEnabledInitialUnsignedReserve` generates all 4,000 reserve artists into both the registry and `unsignedArtists` before live scouting.
3. `RosterManager.GetEnabledSupplyCandidates` treats every eligible `unsignedArtists` entry as searchable and refreshes deterministic discovery slates every four weeks.
4. `ArtistManager.ApplyLifecycleExits` applies the 78-week unowned horizon only to artists with a prior contract. A never-signed reserve prospect therefore never leaves or pauses active search.

The reserve is isolated from the global RNG, but not from selection. It is a permanent searchable stock. The new policy must correct participation, not talent quality.

### Research basis

This is a simulation design, not an attempt to calibrate a 1960s music labor market from modern data. The external research supplies structural principles only:

- Blanchard and Diamond model hires as flows produced jointly by stocks of job seekers and vacancies, rather than as an unlimited draw from the whole labor force: [The Aggregate Matching Function](https://www.nber.org/papers/w3175).
- Federal Reserve search-and-matching work makes the same distinction explicit: matches are bounded by job seekers and vacancies, and market tightness changes each side's matching probability: [Labor Market Search in Emerging Economies](https://www.federalreserve.gov/pubs/ifdp/2009/989/ifdp989.htm).
- Matching efficiency depends on composition and geographic or sector dispersion, supporting retention of the existing genre/region discovery frictions instead of globally ranking the reserve: [What Drives Matching Efficiency?](https://www.federalreserve.gov/pubs/feds/2011/201110/).
- Musicians commonly experience intermittent work and periods outside active music employment, so population membership should not imply continuous active search: [U.S. Bureau of Labor Statistics, Musicians and Singers](https://www.bls.gov/ooh/Entertainment-and-Sports/Musicians-and-singers.htm).

These sources support a population/participation split, stock/flow accounting, and retained mismatch. They do **not** supply a numeric tuning target. The candidate below deliberately derives its weekly exposure target from simulated hiring opportunities and reuses the already accepted 78-week horizon.

## Authorized candidate: rotating prospect participation

### 1. Preserve population and identify the reserve explicitly

Keep all current generation behavior:

- generate the original 3,000 artists on the frozen launch path;
- populate initial label rosters from those 3,000 with unchanged ordering and global RNG behavior;
- generate the additional 4,000 reserve artists after roster allocation on the isolated population RNG;
- retain annual runtime formation at exactly 300 per complete calendar year; and
- keep attributes, quality distributions, genres, regions, members, names, ages, and formed-year rules unchanged.

Add `EnabledInitialReserve` to `ArtistCohort`. Assign it only while `generatingInitialReserve` is true. The first 3,000 remain `InitialLegacy`; runtime artists remain `RuntimeFormation`. Do not rely on artist ID ranges once this explicit cohort exists, although the analyzer may retain ID-boundary reconciliation as an invariant.

The default enabled registry must still contain exactly 7,000 artists immediately after reserve materialization. `--suppress-enabled-initial-reserve` remains diagnostic-only and must retain its current semantics and RNG neutrality.

### 2. Separate prospect participation from lifecycle and ownership

Add a narrow state model for **never-signed active prospects**. Do not overload `ArtistLifecycleStatus`, `CareerState`, ownership, or performance cooldown state.

Recommended representation:

```text
ProspectMarketStatus = NotProspect | Latent | Seeking
prospectSeekingWeeks
prospectMarketSpellCount
```

Semantics:

- `Latent`: active in the population and registry, but absent from `unsignedArtists` and ineligible for discovery or signing.
- `Seeking`: active, never signed, present exactly once in `unsignedArtists`, and eligible for the existing fresh-potential discovery lane.
- `NotProspect`: signed artists and all artists with prior contract history. Existing roster, free-agent, cooldown, inactivity, exhaustion, and terminal rules remain authoritative for them.

At launch:

- every `EnabledInitialReserve` artist starts `Latent`;
- any never-signed residual from the original 3,000 starts `Seeking`; and
- every `RuntimeFormation` artist starts `Seeking` when formed.

A first signing atomically changes `Seeking -> NotProspect` as the artist leaves `unsignedArtists`. A later departure uses the existing experienced-free-agent path; it must never re-enter the prospect participation policy.

### 3. Derive weekly exposure from real hiring opportunities

At the enabled weekly population boundary, calculate:

```text
V_t = count of active labels that:
      - have CurrentRosterSize < OperatingRosterTarget; and
      - can afford the existing tier estimated advance

S_t = count of active, never-signed Seeking prospects
L_t = count of active, never-signed Latent prospects

A_t = min(L_t, max(0, V_t - S_t))
```

`V_t` counts label-weeks, not empty slots. A label can perform at most one candidate evaluation and one signing attempt per week, so a five-slot deficit is still one weekly hiring opportunity. Do not use `maxRosterSize`, release-lane deficit, release eligibility, expected releases, units, revenue, artist quality, or an economic ratio in this calculation.

Activate exactly `A_t` latent prospects into `Seeking`. This is an exposure budget, not an automatic match or signing. The existing service mode, scouting gate, discovery window, region/genre candidate construction, fresh-potential score, `0.30` ordinary threshold, recovery fallback, affordability check, operating target, and one-attempt rule still decide whether a contract occurs.

Calculate affordability through the existing pure estimated-advance preview. Do not consume a scouting roll or enumerate a label's discovery slate while measuring `V_t`.

Runtime formations enter `Seeking` before `A_t` is calculated, so organic inflow reduces reserve activation instead of being added on top of a fully exposed reserve. Existing seekers are not forcibly withdrawn merely because `V_t` later falls; they complete their current search spell.

### 4. Rotate unmatched prospects without deleting them

Reuse the existing 78-week unowned horizon as the active-search spell length. For a never-signed `Seeking` prospect:

- increment `prospectSeekingWeeks` once per live week;
- if signed, transition to `NotProspect` normally;
- if still unsigned after 78 weeks, transition to `Latent`, remove the artist from `unsignedArtists`, reset `prospectSeekingWeeks`, and increment `prospectMarketSpellCount`.

This transition is **not** inactivity, retirement, disbandment, a performance failure, or a career-state change. The artist remains active in the registry and may be exposed again later.

When choosing latent artists for `A_t`, order by:

1. lowest `prospectMarketSpellCount`;
2. a deterministic stable key from artist ID and a fixed policy namespace; and
3. artist ID as the final tie-break.

Do not inspect or rank quality, reputation, momentum, genre weight, label fit, prior slate score, or economic output during activation. This ensures broad circulation through the reserve before repeat exposure. It also consumes no behavior-producing RNG.

Region and genre mismatch remain inside the existing discovery process. Do not pre-match activated prospects to labels or guarantee that every vacancy finds a suitable candidate.

### 5. Authoritative weekly order

Keep `ChartManager.OnWeekEnded` as the live owner. Within `ArtistManager.AdvancePopulationLifecycle`, use one documented sequence:

```text
reconcile ownership and terminal state
apply existing experienced-artist lifecycle exits
materialize this week's runtime formations as Seeking
expire completed 78-week prospect search spells to Latent
measure V_t, S_t, and L_t
activate A_t latent prospects
reconcile unsigned-pool uniqueness and ownership
```

Roster scouting then observes the finalized weekly searchable supply through its existing callback order. If actual subscriber order does not permit this sequence, fix ownership/order explicitly and cover it with a probe; do not approximate it with a second scouting pass.

## Required telemetry

Add one enabled-only, aggregate, one-row-per-week stream such as:

```text
artist-labor-market-weekly.csv
```

Required fields:

```text
seed,week,date
registryPopulation
initialLegacyPopulation
enabledInitialReservePopulation
runtimeFormationPopulation
activeRostered
experiencedFreeAgents
freshSeeking
freshLatent
affordableHiringOpportunityLabels
requestedProspectActivations
actualProspectActivations
prospectSearchSpellExpirations
firstTimeSignings
repeatSignings
meanSeekingQuality
meanLatentQuality
activationMeanQuality
activationQ1,activationQ2,activationQ3,activationQ4
maxProspectMarketSpellCount
duplicateSeekingEntries
latentUnsignedPoolEntries
seekingMissingFromUnsignedPool
prospectStatusContractConflicts
```

Extend `artist-population-events.csv` for aggregate-relevant individual transitions only:

- `prospect-activated`;
- `prospect-search-expired`; and
- the existing first signing event with pre-sign prospect status.

If emitting one event per activated/expired prospect would materially inflate decade telemetry, write cohort/genre/region aggregate transition rows instead and keep deterministic fixed probes for individual semantics. Do not emit 4,000 launch-latency rows.

Extend annual cohort output to report Seeking and Latent separately. The existing `activeUnsigned` field currently conflates active population with searchable supply and must no longer be used as the sole labor-market measure.

## Fixed probes

Retain accepted D5 probes and D6 probes 1-54. Add deterministic production-helper coverage for at least:

1. the original 3,000 remain `InitialLegacy` and preserve launch roster allocation;
2. the post-allocation 4,000 are explicitly `EnabledInitialReserve` and the registry reaches 7,000;
3. reserve generation consumes only the isolated population RNG as before;
4. reserve prospects begin Latent and are absent from `unsignedArtists`;
5. original residual and runtime formations begin Seeking and appear exactly once in `unsignedArtists`;
6. `V_t` counts active, affordable labels below operating target once each, regardless of slot depth;
7. `A_t = min(L_t, max(0, V_t - S_t))` at zero, under-supplied, exactly supplied, over-supplied, and latent-exhausted boundaries;
8. activation ordering is independent of quality and consumes no RNG;
9. runtime formation reduces reserve activation in the same weekly boundary;
10. a Seeking prospect remains searchable through week 77 and becomes Latent at week 78;
11. search-spell expiry does not change lifecycle status, career state, formed year, cohort, or history;
12. least-exposed latent prospects activate before any repeat-spell prospect;
13. first signing leaves the prospect policy atomically;
14. a dropped prior-contract artist remains on the existing experienced/cooldown path and is never capped as a prospect;
15. Latent artists cannot enter discovery, signing, roster, release, or pending-project selection;
16. suppression still leaves the original 3,000 intact and does not create reserve participation state;
17. disabled configuration performs no participation work, emits no labor-market stream, consumes no new RNG, and retains all 45 frozen hashes; and
18. population, participation, ownership, pool uniqueness, and lifecycle counts reconcile after repeated activation, signing, expiry, and reactivation.

## Validation ladder

### S0 - freeze and reproduce the causal boundary

Before changing behavior, reproduce the baseline and no-reserve 1960/1961 capacity and economic ratios with `analyze-economic-yield-attribution.mjs`. Record:

- reserve, original, and runtime first-time signings and releases;
- end-of-year registry, roster, unsigned, and release-eligible counts;
- reserve quality quartiles at generation, signing, and release;
- label hiring-opportunity counts derivable from current vacancy telemetry; and
- the fact that never-signed reserve prospects have no search exit.

Resolve any discrepancy with `ArtistPopulationLifecycleAudit.md` before implementation.

### S1 - build and probes

- `dotnet build "Label Man.sln" --no-restore` passes with no new warning.
- `git diff --check` passes.
- Accepted D5 probes and the complete expanded D6 suite pass.
- Record the functional-source manifest.
- Prove that activation and expiry are deterministic and RNG-neutral.

### S2 - paired 52-week seed-1001 checkpoint

Run a fresh control and treatment from the same source:

```powershell
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-supply-policy-control-52-1001 --seed=1001 --disable-genre-market-v2 --disable-artist-population-lifecycle --aggregate-only
& '<Godot-console.exe>' --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d6-supply-policy-enabled-52-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
```

Require:

- successful releases and scheduled Albums each inside `[0.85,1.15]`;
- units, gross, label net, and market net each inside `[0.90,1.10]`;
- individual-format units inside inherited `[0.85,1.15]` bands;
- exactly 300 runtime formations for the complete 1960 year;
- a 7,000-artist default launch registry and explicit 4,000-artist reserve cohort;
- nonzero reserve activation and nonzero first-time signing;
- zero ownership, duplicate, latent-searchable, terminal, chronology, closed-label, hard-cap, one-attempt, and release-selection violations; and
- telemetry reconciliation.

Stop if S2 fails. Do not adjust a participation multiplier, quality threshold, formation rate, or reserve size. One source-correctness repair is allowed only for a demonstrated implementation defect in the specified state machine.

### S3 - paired 104-week checkpoint and deterministic repeat

Run a fresh paired 104-week control/treatment and an independent treatment repeat with no source change:

```text
d6-supply-policy-control-104-1001
d6-supply-policy-enabled-104-1001
d6-supply-policy-enabled-repeat-104-1001
```

Require every annual S2 capacity, format, and economic gate. The two treatment runs must have the same stream set and every suffix-matched deterministic CSV must be byte-identical.

Also require:

- both reserve activation and runtime-formation signing occur;
- at least one prospect search spell expires at the 78-week boundary;
- latent and seeking stocks reconcile by cohort every week;
- activation quality is statistically an unranked slice of the eligible latent stock, reported by quartile rather than enforced by a narrow mean threshold;
- no repeat-spell activation occurs while a never-exposed eligible latent prospect remains; and
- the default reserve, not the suppression diagnostic, is active.

Interpret failures by surface:

- **Economics above band with capacity in band:** stop and recommend the already identified market-clearing amendment; do not further ration supply to compensate for independent buyer pools.
- **Capacity below band with economics in band:** stop and report the labor-market service deficit; request authority for a separately justified participation/matching amendment, not a scalar sweep.
- **Both above band:** first audit whether Latent artists leaked into discovery or activation was quality-ranked. If not, stop; the specified policy is rejected.
- **Both inside band:** freeze the candidate and continue.

### S4 - 260-week market and chronology checkpoint

Only after S3 passes, run a fresh paired 260-week seed-1001 control/treatment from the frozen source. Do not reuse a control produced before the systemic release-capacity repair.

Apply the inherited per-block capacity, format, economics, finance, genre, label, and lifecycle gates. Additionally require:

- formations `300 / 300 / 300 / 300 / 294` across the five 52-week blocks;
- nonzero first-time signings in every block;
- reserve participation declines naturally as runtime cohorts enter, without forcing reserve exhaustion;
- every complete-year runtime cohort receives active-search exposure;
- p90 first-exposure age for runtime formations is reported and no formed cohort is permanently Latent;
- experienced free-agent behavior remains independent of the fresh-prospect exposure budget;
- maximum spell count, activations, expirations, signings, and release share are reported by cohort, genre, region, and year; and
- all structural invariants remain zero.

Stop on the first hard failure. One implementation-defect repair within the specified model invalidates prior candidate runs and requires restarting at S1.

### S5 - disabled replay

After S4 passes, run the frozen 52-week disabled aggregate replay and require all 45 compatibility streams to match `d6-fulfillment-emerging-memory-52b-control-1001` by suffix and SHA-256. No population or labor-market stream may be emitted.

### S6 - date-complete decade and later seeds

Only after S5 passes, run a fresh paired 522-Friday seed-1001 decade. Apply the inherited per-year and aggregate gates plus the S4 labor-market and chronology requirements. If seed 1001 passes, the handoff authorizes sequential paired seeds 1002 and 1003 with no source change, followed by the already defined unused holdout procedure. Stop on the first failed seed and do not tune between seeds.

## Closed surfaces

Do not change or tune:

- the 3,000 original launch pool, 7,000 enabled launch registry, or 300 annual formations;
- artist attribute distributions, quality calculation, ages, types, genres, regions, or generation RNG;
- prospect/experienced scoring, the ordinary `0.30` threshold, recovery fallback, discovery slate sizes, or four-week discovery refresh;
- scouting probability, service-mode thresholds, operating targets, affordability, advances, or one-attempt semantics;
- performance probation, cooldown, exhaustion, inactivity, retirement, or disbandment rules, except reusing 78 weeks solely as the prospect search-spell horizon described above;
- release cadence, release probability, release priority, project timing, Single/Album choice, hit inventory, or format rules;
- demand, buyer pools, quality exponents, awareness, saturation, inventory, sales, price, finance, royalties, overhead, label lifecycle, or distribution deals;
- genre availability, keyframes, supply weights, regional routing, distance, or historical inputs;
- acceptance bands; or
- frozen disabled behavior, stream set, headers, values, and RNG order.

Do not add a participation multiplier such as `0.5x`, `1.5x`, or `2x` vacancies in this candidate. The absence of that scalar is intentional. If the one-seeker-per-hiring-opportunity stock target fails, report the failure and seek a new amendment rather than calibrating it against economics.

## Completion record

Append the completed pass to `ArtistPopulationLifecycleAudit.md` with:

- before/after source manifests and exact commands;
- external research links and the limited principle taken from each;
- baseline/no-reserve causal-boundary reproduction;
- weekly stock/flow reconciliation for `V_t`, `S_t`, `L_t`, and `A_t`;
- cohort, genre, region, quality-quartile, spell, activation, signing, release, and expiry tables;
- capacity, format, economics, finance, label, chronology, and invariant results at every executed gate;
- deterministic-repeat and disabled-replay hashes;
- whether the reserve remains materially used without dominating replacement output;
- the exact stop/pass decision; and
- the next recommendation selected from supply service, market clearing, direct defect repair, or validation continuation.

The intended result is a real mature artist population with a bounded active labor force—not a smaller hidden population, a weakened talent distribution, or an economic penalty that cancels better artists after they are signed.
