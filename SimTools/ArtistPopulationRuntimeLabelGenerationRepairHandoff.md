# Artist population runtime-label generation repair handoff

## Mission and authority

Repair the enabled runtime-founded label generation path so a live-founded label is a coherent operating business rather than a zero-capability shell. Preserve empty entry, target one, no birth-week signing, ordinary weekly labor-market matching, canonical tier capacity, quarterly one-slot organic growth, and the accepted runtime-birth RNG alignment.

This is the authoritative next-pass handoff after the seed-1001 G6 decade stop recorded in `ArtistPopulationLifecycleAudit.md`. It supplements `ArtistPopulationRuntimeLabelBootstrapRepairHandoff.md`, `ArtistPopulationRuntimeLabelOrganicGrowthHandoff.md`, and `ArtistPopulationRuntimeLabelOrganicGrowthRngAlignmentHandoff.md`. It supersedes only the G6 attribution that all runtime labels failed because the initial bootstrap vacancy was never filled.

The G6 result remains a historical failure. Do not rewrite, relabel, delete, or overwrite its artifacts. The decade still failed 1968 scheduled Albums at `0.7787x`, 1969 releases at `0.8395x`, and 1969 scheduled Albums at `0.7088x`. Later seeds and the holdout remain stopped.

This handoff authorizes:

- one enabled-only runtime-label operating-profile repair;
- coherent runtime tier/archetype selection;
- correct runtime founding, geography, and zero-history initialization;
- focused production helpers, probes, and enabled-only profile telemetry;
- a fresh disabled no-op proof; and
- a restarted seed-1001 validation ladder through the decade only if every earlier rung passes.

It does not authorize:

- changing the original 600-label launch population or its initialization behavior;
- changing target one, birth-week no-signing, Recovery, signing thresholds, candidate discovery, artist supply, quarterly growth eligibility, or canonical capacities;
- changing release cadence after generation, Album decisions, demand, sales, finance rules, format economics, the deferred Single-yield surface, or any acceptance band;
- adding a shipping toggle, seed-specific behavior, or output-targeted calibration; or
- running seeds 1002/1003 or a holdout.

Preserve the current worktree and all retained artifacts. Do not reset or reconstruct prior repairs.

## Corrected decade attribution

The G6 decade proves zero runtime-label output, but it does not prove universal failure to fill target one.

Retained telemetry establishes:

- 662 `RuntimeBootstrap` birth events;
- 656 runtime labels visible in weekly scouting telemetry, with the remaining six born at the final boundary after their first weekly scouting opportunity;
- 263 successful runtime-label signings belonging to 263 distinct labels;
- 252 first contracts and 11 repeat contracts;
- 12,529 runtime-label rows observed at roster full;
- 20,391 active runtime-label rows with a nonempty eligible candidate slate;
- signed runtime labels retaining a roster for 4 to 73 weeks, with a median of 51 weeks;
- zero runtime-label release outcomes; and
- zero runtime-label Album projects.

All 77 runtime labels still active at week 522 were empty and had never signed, but they were a late survivor cohort born between weeks 461 and 518. Every runtime label that had ever signed was inactive by week 522. The final snapshot is survivor-biased and cannot be used to claim that no runtime bootstrap vacancy ever filled.

The target-one initializer and weekly Recovery boundary are functioning. The hard zero is downstream of signing and upstream of release scheduling.

## Confirmed source defect

`LabelLifecycleManager.SpawnNewLabel` creates every runtime label through `LabelGenerator.GenerateSingleLabel`.

`LabelGenerator.GenerateSingleLabel` calls `ApplyArchetypeStats`, but that method is an unfinished stub. It calculates `tierBudget`, never uses it, and assigns no operating fields. The comments claiming a full implementation was omitted "for brevity" have been present since the initial repository commit.

As a result, every generated runtime label enters with serialized/default zero values for:

```text
budgetLevel
scoutingAbility
productionQuality
marketingPower
ownedReach / distributionStrength
nationalReach
riskTolerance
artistLoyalty
payolaWillingness
releasesPerMonth
```

`InitializeFinancials` assigns cash, reputation, share, and debt, but it does not repair the missing operating profile. `InitializeRuntimeRosterForLabel` correctly assigns the canonical capacity and target one; it is not responsible for initializing the business fields.

The release hard stop is exact:

```text
weeklyCapacity = max(0, releasesPerMonth) / 4
releaseChance  = weeklyCapacity * status * artistAvailability * seasonality
```

Because `releasesPerMonth` remains zero and no production path later raises it, every runtime label has release chance zero forever. A signed runtime artist can therefore never create a production event, marketing event, release, charting record, or Album project.

Monthly overhead and signing advances continue normally. With no possible revenue, runtime labels accumulate losses, deteriorate to Dying/Bankrupt/Defunct, and eventually lose their artists. Quarterly organic growth is then blocked by `UnhealthyStatus` or `ConsecutiveLosses`; even a surviving healthy label could not satisfy `NoRecentCharting` while releases are impossible.

Do not repair this by weakening the organic-growth checks. They are correctly rejecting businesses with no demonstrated output.

## Additional generation defects in scope

### Incoherent tier/archetype pairs

Runtime tier and style are not selected as a coherent business profile. Birth tier is currently Small or Independent, while `GenerateArchetype` gives every archetype a nonzero chance at either tier.

The retained 662 births include:

- 18 Small and four Independent `CorporateGiant` labels;
- 65 `FolkBoutique` labels, none born at Boutique tier; and
- 50 `JazzPrestige` labels, none born at Boutique tier.

The enabled runtime path must no longer generate impossible pairs such as a Small `CorporateGiant`. Style and scale may not be independent decorative rolls followed by an unrelated budget roll.

The launch factory has a broader version of this issue: procedural launch labels roll archetype and tier independently, and `AILabelFactory.ApplyTierStats` does not use its archetype argument. That is a real separate defect, but changing the frozen 600-label launch population would invalidate the retained control and disabled surfaces. Record it as deferred work. Do not change launch label identities, pairs, stats, RNG, rosters, or prewarm behavior in this pass.

### Missing home geography

`LabelGenerator.AssignRegions` populates strong and distribution regions but never assigns `homeRegion` or the canonical home-city fields. Enabled candidate discovery then fails the regional lookup and silently uses the national fallback. Runtime labels must have a valid canonical home region and city assignment before their first scouting week.

### False pre-birth history

`GenerateSingleLabel` assigns a random `foundedYear` from the preceding five years, preloads at least 12 months of activity, and fabricates prior release totals. A label founded during the measured simulation must not enter with a fictional pre-birth operating history.

For an enabled `RuntimeFounded` label require:

```text
foundedYear       = runtimeBirthYear
monthsActive      = 0 at birth
totalReleases     = 0
top40Hits         = 0
numberOneHits     = 0
momentumScore     = 0
consecutiveLossMonths = 0
sustainedCapabilityQuarters = 0
sustainedLowCapabilityQuarters = 0
```

The existing exact `runtimeBirthWeek` and date fields remain authoritative.

### Probe blind spot

The current runtime bootstrap probes construct a synthetic label with `scoutingAbility = 0.6` and ample cash. They validate roster initialization and service state but never call the production runtime generator. This allowed the unfinished generator to pass every fixed probe.

## Required runtime profile model

### Preserve the birth-tier policy

Retain the existing runtime birth-tier roll:

```text
70% Small
30% Independent
```

Do not add Boutique, MidTier, or Major births in this pass. Tier advancement remains the responsibility of the existing lifecycle.

### Select only tier-valid archetypes

Choose the runtime archetype conditionally from the already selected tier. The pair is one business-profile decision, not two independent identities.

Use these allowed sets:

| Birth tier | Allowed runtime archetypes |
|---|---|
| Small | `RegionalHustler`, `RockRebel`, `BluesRoots`, `CountrySpecialist`, `GospelPowerhouse` |
| Independent | `SoulFactory`, `RockRebel`, `BluesRoots`, `CountrySpecialist`, `TeenHitMachine`, `GospelPowerhouse`, `RegionalHustler` |

The following are invalid at the currently reachable runtime birth tiers:

- `CorporateGiant`: requires Major scale and a corporate operating profile;
- `FolkBoutique`: requires Boutique scale; and
- `JazzPrestige`: requires Boutique scale.

Do not relabel one of those styles as Small or Independent merely to retain its old draw. If a future pass authorizes those birth tiers, add the corresponding coherent profiles then.

Within each allowed set, retain the existing era-sensitive intent from `GenerateArchetype` where applicable: Soul becomes more available from 1962, Rock from 1964, Folk remains excluded because Boutique is not a current birth tier, and Blues may decline after 1965. Renormalize only across the tier-valid set. Do not use realized sales, charts, demand, label deaths, or acceptance outcomes to choose a profile.

### Initialize a complete operating profile

Use the existing `AILabelFactory.ApplyTierStats` Small and Independent ranges as the canonical scale envelopes, without calling the launch factory or changing its behavior:

| Field | Small envelope | Independent envelope |
|---|---:|---:|
| `budgetLevel` | `0.10-0.40` | `0.28-0.62` |
| `marketingPower` | `0.18-0.56` | `0.30-0.72` |
| `distributionStrength` / owned reach | `0.12-0.42` | `0.28-0.62` |
| `nationalReach` | `0.07-0.30` | `0.18-0.50` |
| `scoutingAbility` | `0.34-0.84` | `0.44-0.91` |
| `productionQuality` | `0.28-0.80` | `0.40-0.91` |
| `releasesPerMonth` | `0.20-0.80` | `0.50-1.50` |

Initialize `riskTolerance`, `artistLoyalty`, and `payolaWillingness` from explicit bounded profile ranges as well. No operating field above may remain at its serialized zero merely because the label was runtime-founded.

Archetype must have a real, bounded effect within the tier envelope. At minimum preserve these directional identities:

- `SoulFactory`: higher production, marketing, loyalty, and cadence; lower risk than a Rock label;
- `RockRebel`: higher risk and scouting, lower polish and loyalty;
- `TeenHitMachine`: higher marketing, production, scouting, and cadence; lower loyalty;
- `BluesRoots`: steadier loyalty and production, lower marketing and risk;
- `CountrySpecialist`: stronger loyalty and owned distribution, lower risk and national reach;
- `GospelPowerhouse`: high loyalty and focused production, restrained marketing and risk;
- `RegionalHustler`: shoestring budget and reach, relatively strong scouting/risk, and no corporate-scale capability.

Choose and document one explicit modifier/range table before running behavioral validation. Keep every final value inside its birth-tier envelope. Do not repeatedly tune the table against annual release or Album ratios. One correction is allowed only for an implementation error or an incoherent/zero profile exposed by probes.

Cash reserves, reputation, market share, and debt must remain coherent with the same tier/profile. The existing tier financial ranges may be retained if probes show they are internally consistent. Do not grant startup subsidies or special loss forgiveness.

### Runtime founding and registration order

At enabled runtime birth, require this order:

1. generate the legacy identity shell at the existing call boundary;
2. record immutable runtime origin and exact birth week/date;
3. apply the coherent enabled runtime operating profile without shared-RNG consumption;
4. reconcile exact founding year, zero prior history, and canonical home geography;
5. initialize the empty runtime roster, consume the one accepted legacy capacity-alignment draw, assign canonical hard capacity, and set target one;
6. emit profile and target initialization telemetry;
7. register with label, chart, and competitor owners; and
8. perform no signing until the next ordinary weekly scouting boundary.

Do not call `PopulateInitialRoster`, `InitialSignArtist`, or any launch-population factory from this path.

## RNG and compatibility contract

The accepted G6 source restored one discarded tier-specific legacy capacity draw at every enabled runtime birth. Preserve that draw exactly once and continue to discard its value.

The profile repair must not add shared `GD.Rand*` calls after runtime birth and phase-shift the rest of the simulation. Use an isolated deterministic runtime-profile RNG or a pure stable-hash mapping for:

- coherent archetype selection;
- profile-range values; and
- any profile-specific genre choice that is new to this repair.

Seed the isolated decision from stable recorded inputs such as requested simulation seed, label ID, runtime birth week, and a versioned domain tag. Do not use process-randomized `GetHashCode`, wall time, object identity, collection iteration order, or realized simulation outcomes.

The legacy `LabelGenerator.GenerateSingleLabel` call currently consumes the shared identity/archetype/genre/region/finance draws in both disabled and enabled runs. Preserve its shared-RNG call schedule for compatibility, then overwrite only the enabled runtime operating profile at the explicit seam. Treat any discarded legacy style value as a compatibility token; it may not control the final tier/archetype pair or operating stats.

The disabled route must remain byte-identical. Do not globally complete `LabelGenerator.ApplyArchetypeStats`, because that would add shared draws and alter disabled runtime-label behavior. Put the repair behind the existing enabled lifecycle boundary or an equivalent production seam that is provably dormant when the lifecycle is disabled.

Telemetry must consume no gameplay RNG and may not participate in decisions.

## Implementation boundary

Prefer one production component with a name such as `RuntimeLabelProfileFactory` or `InitializeRuntimeFoundedOperatingProfile`. It should own:

- tier-valid archetype selection;
- profile stat initialization;
- archetype-consistent genres;
- home geography assignment;
- exact founding/zero-history reconciliation; and
- a structured immutable result suitable for telemetry and probes.

Do not copy another partial generator or add a third independent set of unexplained random rolls. Keep the runtime profile table centralized and auditable.

The profile helper must not:

- initialize or sign a roster;
- set operating target or hard capacity;
- inspect candidate supply, charts, releases, sales, finance outcomes, or future dates;
- change status after birth based on output;
- consume the global RNG; or
- mutate launch-population labels.

Retain `LabelGenerator` only as the legacy identity/disabled compatibility shell needed by this pass. Add a clear comment and audit note that its unfinished stat method is not authoritative for enabled runtime-founded labels. Do not silently claim the broader generator duplication is resolved.

## Required telemetry

Add one compact enabled-only runtime-profile birth stream, or an equivalent event stream that makes every initialized field queryable. Prefer a new stream over changing a high-volume existing schema.

At minimum record:

```text
seed
birthWeek
birthDate
labelId
labelName
birthTier
archetype
headquartersCity
homeRegion
homeCityId
homeCityAssignmentSource
preferredGenres
secondaryGenres
budgetLevel
scoutingAbility
productionQuality
marketingPower
ownedReach
nationalReach
riskTolerance
artistLoyalty
payolaWillingness
releasesPerMonth
cashReserves
reputation
marketShare
debtLevel
foundedYear
monthsActive
totalReleases
top40Hits
numberOneHits
maxRosterSize
operatingRosterTarget
profileVersion
```

Require exactly one profile row per enabled runtime birth, no row for launch labels, and no profile stream in a dual-disabled run. Dispose and flush the writer correctly at completion.

Retain existing target-event and weekly scouting telemetry. Add derived audit checks rather than behavior-producing counters for:

- invalid tier/archetype pair count;
- zero required operating field count;
- missing/invalid home geography count;
- false pre-birth history count;
- runtime labels with a signed, release-eligible artist but zero calculated release chance;
- runtime signings, releases, charting records, and Album projects by birth cohort;
- time from birth to first signing and first release; and
- target-one fill, later organic target increase, and subsequent vacancy fill.

## Fixed probes

Retain accepted D5 and D6 probes 1-61. Add production-helper coverage proving:

1. the real enabled runtime birth path calls the profile helper exactly once;
2. Small births can produce only the five allowed Small archetypes;
3. Independent births can produce only the seven allowed Independent archetypes;
4. `CorporateGiant`, `FolkBoutique`, and `JazzPrestige` cannot be selected at current runtime birth tiers;
5. every required operating field is inside its tier envelope and `releasesPerMonth > 0`;
6. archetype modifiers preserve the documented directional identities without escaping the tier envelope;
7. the same seed, label ID, birth week, and profile version produce the same profile independent of enumeration order;
8. changing the stable identity input can produce a different profile without consuming global RNG;
9. runtime founded year equals birth year and all prior-history counters are zero;
10. headquarters city resolves to valid `homeRegion`, `homeCityId`, and assignment source;
11. runtime roster remains empty with target one and canonical capacity after profile initialization;
12. no birth-week signing occurs;
13. one successful ordinary signing closes target one and leaves positive release capacity;
14. a signed release-eligible runtime label has a strictly positive calculated weekly release chance;
15. disabled initialization receives no profile mutation or telemetry and retains its existing RNG state;
16. launch-population construction and initialization remain untouched; and
17. the accepted legacy capacity-alignment draw still occurs exactly once and cannot affect profile, capacity, target, roster, or signing.

Do not satisfy these probes with a synthetic pre-populated healthy label. At least one probe must call the production runtime generator/profile seam used by `SpawnNewLabel`.

## Validation ladder

Use seed 1001 only. Preserve all prior artifacts and use new run names.

Suggested family:

```text
d6-runtime-label-profile-probes-1001
d6-runtime-label-profile-disabled-52-1001
d6-runtime-label-profile-control-52-1001
d6-runtime-label-profile-enabled-52-1001
d6-runtime-label-profile-control-104-1001
d6-runtime-label-profile-enabled-104-1001
d6-runtime-label-profile-enabled-repeat-104-1001
d6-runtime-label-profile-maturity-control-260-1001
d6-runtime-label-profile-maturity-enabled-260-1001
d6-runtime-label-profile-decade-control-1001
d6-runtime-label-profile-decade-enabled-1001
```

### P0 - retained-artifact preflight

Before editing, reproduce from the retained G6 artifacts:

- 662 birth events and 656 weekly-observable runtime IDs;
- 263 successful signings across 263 runtime labels;
- 252 first and 11 repeat signings;
- 12,529 roster-full runtime rows;
- zero runtime release outcomes and zero runtime Album projects;
- all 77 final active runtime labels empty and never signed;
- the retained invalid-pair counts, including 22 runtime `CorporateGiant` births;
- `LabelGenerator.ApplyArchetypeStats` assigning no fields;
- `releasesPerMonth == 0` at runtime birth; and
- the exact G6 annual ratios and stop decision.

Record current functional-source hashes before changing source.

### P1 - implementation, build, and probes

Implement only the authorized enabled runtime-profile repair, telemetry, and production probes. Run:

```text
git diff --check
dotnet build "Label Man.sln" --no-restore
the complete accepted D5/D6 probe command
```

Require every old and new probe to pass. Record the profile version, pair table, stat envelopes/modifiers, isolated-seed recipe, shared-RNG proof, and corrected functional-source manifest.

### P2 - disabled no-op proof

Run a fresh 52-week dual-disabled aggregate replay. Require:

- exactly the retained 45 CSV suffixes;
- 45/45 byte equality with `d6-transition-envelope-disabled-52-1001` and the accepted R2 family;
- no runtime-profile stream;
- no target-event stream; and
- no missing or extra stream.

Any disabled difference is an implementation defect. Correct only that defect, rebuild, rerun all probes, and restart P2.

### P3 - fresh 52-week boundary

Run a fresh same-source control and enabled treatment with the exact G6 feature switches and seed. Do not overwrite prior runs.

Require:

- fresh control equivalence to the retained control on every required common stream;
- one valid profile row for every enabled runtime birth;
- zero invalid pair, zero required-field, zero geography, and zero false-history violations;
- target one, empty birth, no birth-week signing, and canonical capacities 5 or 12;
- no signed release-eligible runtime label with zero calculated release chance;
- no shared-RNG phase difference before the first intentional profile-caused behavior event;
- all population, ownership, chronology, finance, release, project, and target invariants; and
- every inherited 1960 economic gate.

Do not require a particular number of runtime signings, releases, Albums, or organic increases in 1960. Reconcile whatever occurs to actual opportunities and profile state. If a runtime output occurs, prove the chain birth -> weekly signing -> positive release chance -> ordinary release decision.

Stop on the first inherited gate failure. Do not tune the profile table to rescue an Album or release ratio.

### P4 - 104-week maturity and determinism

Only after P3 passes, run fresh 104-week control/treatment and an independent enabled repeat.

Require:

- the two enabled families byte-identical by suffix, length, and SHA-256;
- every inherited 1960/1961 gate;
- all profile and lifecycle invariants;
- nonzero successfully signed runtime labels;
- nonzero runtime-label successful releases by the end of 1961;
- no runtime business with an eligible artist and permanently zero release capacity; and
- cohort reconciliation for births, signings, releases, closures, and surviving active labels.

An Album project is observational at this rung; do not require or suppress one. Organic growth may still be zero if no label has satisfied all unchanged evidence requirements.

### P5 - 260-week maturity

Only after P4 passes, run fresh paired 260-week seed-1001 processes. Require every original G5 annual, catastrophic-economic, structural, population, finance, release, Album, and target gate.

Additionally require:

- nonzero cumulative runtime-label signings;
- nonzero cumulative runtime-label releases;
- nonzero runtime-label charting records;
- at least one runtime-founded label that fills target one and reaches a quarterly review where the blocker has advanced beyond `OperatingTargetUnfilled`;
- full reconciliation of every organic increase, if any, to eligibility evidence and the later weekly vacancy/signing chain; and
- no invalid pair or zero-profile regression.

Do not force a nonzero organic increase if the unchanged profit, runway, status, and chart evidence genuinely do not qualify a label. If growth remains zero despite nonzero runtime output, stop and attribute the first remaining legitimate blocker before changing policy.

### P6 - restarted seed-1001 decade

Only after P5 passes, run fresh paired 522-Friday seed-1001 control/treatment from unchanged source. Do not reuse the failed enabled G6 process as treatment.

Require:

- every inherited annual release, scheduled-Album, Album-unit/gross, total-unit/gross, label-net, market-net, population, finance, chronology, ownership, and structural gate;
- deterministic completion and complete stream families;
- zero invalid runtime profile rows;
- nonzero mature runtime contribution to signings, releases, charting, and projects where ordinary format decisions select Albums;
- organic increases reconciled to unchanged eligibility evidence;
- no target jump at birth, promotion, demotion, or acquisition beyond the existing reconciliation contract; and
- no market-clearing attempt at or above operating target.

The deferred per-release Single-yield excess remains separate. Report it but do not change it in this pass.

Seeds 1002/1003 and the holdout remain stopped even if P6 passes. Record the result and request the next authorization.

## Stop conditions

Stop and preserve artifacts at the first occurrence of any of the following:

- a disabled-path byte difference;
- a launch-population generation or initialization difference;
- any new shared RNG draw or unexplained phase movement at runtime profile creation;
- loss, duplication, or use of the accepted one-draw capacity compatibility token;
- an invalid runtime tier/archetype pair;
- a required operating field left zero or outside its tier envelope;
- missing home geography or fabricated pre-birth history;
- birth-week signing, target above one at birth, bulk signing, or launch-roster population;
- a signed release-eligible runtime label whose calculated release chance is zero because of generation state;
- an organic target increase outside the unchanged quarterly contract;
- nondeterministic profile or repeat-run output;
- any inherited release, Album, economic, population, finance, chronology, ownership, or structural gate failure; or
- any attempt to rescue a result through release, Album, demand, sales, finance, Single-yield, growth-cadence, threshold, or acceptance-band tuning.

One implementation correction is allowed only when a probe or first-divergence trace proves this handoff was implemented incorrectly. Otherwise stop and write the result.

## Required audit record

Append the completed result to `ArtistPopulationLifecycleAudit.md`. Include:

- the historical G6 failure without revision;
- the corrected 263-signing attribution and final-cohort survivor bias;
- exact source lines and history for the unfinished generator;
- old zero fields and new coherent profile contract;
- allowed pair table and actual birth-pair distribution;
- profile version, isolated RNG recipe, and shared-RNG proof;
- founding/history/geography reconciliation;
- source hashes, commands, completion markers, build and probe results;
- disabled 45-stream comparison;
- control/treatment totals and annual ratios at each completed rung;
- runtime births, signings, releases, charting records, projects, closures, survivors, and organic increases;
- determinism comparison; and
- the exact stop or next-authorization decision.

Do not describe this repair as release or Album calibration. It is a production initialization correction: enabled runtime labels must receive coherent business identities and nonzero operating capabilities before the unchanged labor market, release system, finance system, and organic-growth lifecycle can evaluate them normally.
