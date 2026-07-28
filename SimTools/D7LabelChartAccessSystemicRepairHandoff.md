# D7 label chart access systemic repair — live handoff

Last maintained: July 28, 2026.

This handoff is intentionally maintained while the work is in progress so a
replacement model can continue even if the current Codex run ends abruptly.
It supersedes the resume sequence in
`D7LabelChartAccessLoopContinuationHandoff.md`, while retaining that file as
the detailed history of the preceding pass.

## 1. User goal and non-negotiable interpretation

The acceptance target is:

- **400–600 unique label identities charting cumulatively over the entire
  1960–1969 decade.**
- This is not a per-year target.
- An active distributor should provide temporary national reach during the
  deal.
- A very small Small-label chart tail is acceptable.
- The below-MidTier charting population should be dominated by Independents,
  not Small labels.
- Use the most recent run data to inform changes before spending another run.
- Look broadly for systemic chart-access causes, bugs, and glaring historical
  inaccuracies.

Do not try to reach the target by merely increasing label births. The current
data shows that formation and release throughput are not the binding
constraint.

## 2. Repository state at this checkpoint

Workspace:

```text
C:\Project\Label-Man
```

Branch:

```text
d7-artist-population-plateau
```

Current HEAD:

```text
8ae0986 Track earned national reach and expand chart audit outputs
```

The worktree has six tracked files modified by this pass:

- `Data/DistributionDeal.cs`
- `Data/RecordRuntimeData.cs`
- `Systems/ChartSimulator.cs`
- `Systems/CompetitorManager.cs`
- `SimTools/ChartAuditRunner.cs`
- `SimTools/ArtistPopulationLifecycleProbeSuite.cs`

`.claude/` is unrelated untracked user content. Do not modify it.

At the time this live handoff was first written:

- `git diff --check` was clean.
- The main code changes had built once successfully.
- The fixed probes added afterward had **not yet been rebuilt or run**.
- No new 312-week or decade simulation had been launched.
- No Godot process was running.

Treat every result below labeled “existing run” as artifact analysis, and
every code change labeled “candidate” as unaccepted until the validation
ladder completes.

## 3. Existing acceptance baseline

The full prior decade run is:

```text
d7-decade-firms-522-1001
```

Its cumulative unique label-ID charting series was:

| year | annual labels charting | decade cumulative |
|---|---:|---:|
| 1960 | 147 | 147 |
| 1961 | 143 | 174 |
| 1962 | 139 | 189 |
| 1963 | 137 | 196 |
| 1964 | 132 | 202 |
| 1965 | 128 | 210 |
| 1966 | 133 | 216 |
| 1967 | 134 | 224 |
| 1968 | 137 | 234 |
| 1969 | 139 | **241** |

Final first-chart tier mix:

- Small: 23
- Boutique: 30
- Independent: 100
- MidTier: 78
- Major: 10

The latest existing six-year checkpoint is:

```text
d7-runtime-indie-312-1001
```

It reached **239 cumulative unique label IDs through 1965**, with:

- Small: 16
- Boutique: 29
- Independent: 104
- MidTier: 80
- Major: 10

The Independent-heavy entrant mix improved the old configuration from 210 to
239 through 1965, but that trajectory still does not credibly reach 400 by
1969.

## 4. Artifact analysis performed before another run

### 4.1 Release supply and chart turnover are not binding

For the prior full decade:

- 674 runtime founders were created.
- 498 completed at least one release.
- Runtime labels completed 5,595 releases.
- There were 10,273 retired charting Singles.
- Chart runs averaged 4.91 weeks; median 3, P75 8, P90 11, P95 13, maximum 26.

The chart turns over quickly enough. The problem is access by new label
identities, not a small number of records occupying all positions forever.

Chart records are nevertheless highly concentrated within labels:

- 258 release-lane label IDs had chart evidence before censor correction.
- Mean charting records per such ID: 39.82.
- Median: 8.
- Top-ten labels held 27.7% of those charting records.

The apparent 258-versus-241 discrepancy is prewarm censoring: 17 label IDs
only have left-censored chart peaks that predate the audit window. Excluding
`leftCensoredAtRunStart=true && debutPosition=0` reproduces the official 241.

### 4.2 Exact current runtime-founder funnel

For `d7-runtime-indie-312-1001`:

- 382 runtime founders.
- 339 launched a Single: 88.7%.
- 316 completed a Single: 82.7% of founders and 93.2% of launchers.
- 3,505 runtime Singles were launched.
- 3,069 runtime Singles and 335 runtime albums completed.
- 153/316 completing labels released at least one Single with intrinsic
  quality above 0.70.
- 39 runtime-founded labels signed a distribution deal.
- 30 runtime-founded labels ever charted, all Independent.

The exact 30-label hit count combines:

- 104 retired charting Singles in `lifecycles.csv`;
- 27 active, currently off-chart prior hits in `retirement.csv`;
- 6 currently charting Singles in `live-records-snapshot.csv`.

Those are 137 hit Singles across 30 runtime label identities.

Deal association:

- 25/39 signed runtime labels charted: 64.1%.
- 5/277 completed but unsigned runtime labels charted: 1.8%.
- The association is approximately 35.5×.

This does not alone prove causality because the old audit did not timestamp
first-chart events versus deal state, but it places the binding seam squarely
at regional evidence → offer/signing → national chart access.

### 4.3 The previous pull-deal eligibility was brittle and circular

The old `TryGenerateDistributionOffer` pull route required all of:

- an active Small, Boutique, or Independent with no deal;
- permanent `nationalReach < 0.40`;
- a live record whose running regional breakout peak was at least 0.30;
- hidden intrinsic record quality above 0.70;
- nonzero sales in a strong region in the exact current processing week;
- then a 40% monthly chance, a distributor with capacity/new regions, and
  client acceptance.

Measured consequences in the latest checkpoint:

- 70/277 runtime Independents were born at `nationalReach >= 0.40`.
- 64 of those completed a Single.
- None signed a deal and only one was observed to chart.
- Another 23 completed unsigned labels crossed 0.40 later.
- 87 completed unsigned labels therefore entered the permanently closed
  national-reach state at some point.
- All 101 signed deals in the run were `LabelSought`.
- There were zero `DistributorCourted` signings.

The scalar `nationalReach` threshold duplicated the actual physical boundary:
`SelectDistributor` already requires a distributor to offer at least one
region the client does not cover. Current-week sales also made proven regional
evidence disappear during a stockout, while intrinsic quality duplicated
evidence already represented by the observed breakout score.

### 4.4 Tier conversion reinforces the same diagnosis

Prior full-decade release funnel:

| tier | Singles | releasing labels | charted labels | charted records | label conversion |
|---|---:|---:|---:|---:|---:|
| Boutique | 2,478 | 100 | 42 | 217 | 42.0% |
| Independent | 10,043 | 358 | 135 | 933 | 37.7% |
| Major | 4,239 | 19 | 19 | 3,012 | 100.0% |
| MidTier | 19,257 | 118 | 111 | 6,084 | 94.1% |
| Small | 3,869 | 509 | 23 | 27 | 4.5% |

Latest checkpoint through week 312:

| tier | Singles | releasing labels | charted labels | charted records | label conversion |
|---|---:|---:|---:|---:|---:|
| Boutique | 1,894 | 106 | 42 | 156 | 39.6% |
| Independent | 9,416 | 427 | 112 | 755 | 26.2% |
| Major | 2,706 | 15 | 15 | 1,849 | 100.0% |
| MidTier | 11,945 | 105 | 93 | 3,475 | 88.6% |
| Small | 2,133 | 266 | 15 | 18 | 5.6% |

The entrant-mix change raised the absolute number of charting Independents,
but conversion was diluted. The next improvement should therefore improve
evidence-based access, not raw founder volume.

## 5. Candidate systemic fixes currently in the diff

### 5.1 Persistent, geographically correct pull-deal evidence

`CompetitorManager.TryGenerateDistributionOffer` now:

- evaluates `RegionalRecordData.peakBreakoutScore` in the label’s actual
  strong regions;
- treats that observed peak as persistent evidence;
- removes the arbitrary `nationalReach < 0.40` pull gate;
- removes hidden `quality > 0.70` and exact-current-week sales requirements;
- retains the 0.30 regional breakout threshold;
- retains the monthly offer probability;
- retains distributor capacity and the requirement that the distributor add
  at least one genuinely new physical region;
- retains client offer acceptance.

This is intended to make the real observed signal persistent without turning
distribution into an unconditional subsidy.

Compact attempt telemetry records:

- persistent evidence;
- the old quality/current-sales gate for counterfactual comparison;
- the old national-reach gate;
- push and pull chance outcomes;
- no-distributor, rejected, and signed outcomes.

Output:

```text
SimLogs/<run>-distribution-offer-attempts.csv
```

### 5.2 Retired records remain visible to nominal 52-week label history

`GetRecentChartingRecordCount`, `GetRecentReleasedRecordCount`, and
`HasRecentTop40Record` formerly queried only `ChartManager.GetAllRecords()`.
Retirement removes records from that live collection, so a label’s supposed
52-week evidence vanished as soon as the record retired.

`CompetitorManager` now stores compact retired-record evidence and combines it
with active records for inclusive lookback queries.

### 5.3 Live label track-record counters now advance

`AILabel.totalReleases`, `top40Hits`, and `numberOneHits` were not incremented
for live releases and chart outcomes, despite being used by health, momentum,
and public-profile logic.

The candidate:

- increments `totalReleases` once when a record is tracked;
- increments Top 40 and number-one counters once per record using dedupe sets.

### 5.4 Awareness aging applies one weekly decay step

The old mutable awareness calculation did:

```text
awareness *= 0.95^(weeksSinceRelease - 8)
```

every week. Since the prior week’s already-decayed awareness was the input,
the exponent accumulated triangularly. At record age 18 it had effectively
received `0.95^55`, not `0.95^10`.

The candidate applies one `0.95` step per elapsed week after week 8. This
repairs a direct implementation bug that disproportionately erased slow
regional-to-national breakouts.

### 5.5 Preserve release-imprint identity across acquisition

Acquisition mutates `record.baseRecord.labelId` to the new operating owner.
That is appropriate for current economics, but it retroactively erased the
release label from cumulative chart-breadth audits.

`RecordRuntimeData.releaseLabelId` now preserves the original release imprint.
Annual concentration still assigns economics to the current owner; cumulative
first-chart identity uses the immutable release imprint.

### 5.6 First-chart event telemetry

The audit now emits one row at each label identity’s first chart appearance:

```text
SimLogs/<run>-first-chart-events.csv
```

Fields include:

- release imprint and current owner;
- launch/runtime origin and birth tier;
- first-chart tier and label state;
- record age, chart position, points, and published cutoff;
- quality and regional-breakout evidence;
- signed/completed deal counts and active deal state;
- permanent, borrowed, effective, and owned reach;
- permanent and granted region counts;
- initial awareness and stock.

This closes the causality/attribution gap in the previous artifacts.

### 5.7 Fixed-probe additions

The D6 fixed suite is expanded from 73 to 77 probes:

- persistent home-region breakout evidence survives a zero-sales week;
- an arbitrary national-reach scalar does not close a physically useful deal;
- a peak outside the strong region cannot masquerade as home evidence;
- retired evidence remains visible through the inclusive 52-week boundary;
- awareness receives one decay step per week, not a triangular exponent;
- acquisition does not rewrite the immutable release imprint.

These probes are written but not yet rebuilt or executed.

## 6. High-confidence bugs and historical issues still under adjudication

These were found during the broad audit. They are not all fixed yet.

### 6.1 Active launch-factory profiles are incompletely initialized

`AILabelFactory.ApplyTierStats` currently:

- never initializes `riskTolerance`, leaving all launch labels at 0;
- computes `artistLoyalty` from default zero plus `Rand(-0.1, 0.1)`, leaving
  roughly half exactly 0 and the rest no higher than 0.1;
- never initializes `monthsActive` despite assigning a historical
  `foundedYear`;
- starts historical track-record counters at zero;
- receives `archetype` but explicitly omits the advertised archetype tweaks.

Risk tolerance directly suppresses unknown-artist evaluation; loyalty affects
contract renewal. This is a real active-factory implementation defect and a
candidate for correction before the next simulation.

### 6.2 Duplicate launch names inflate “unique label” IDs

The latest checkpoint’s launch directory had 600 IDs but only 410 exact
display names:

- 106 duplicate-name groups;
- 190 excess IDs;
- one name appeared ten times.

Across reconstructed chart evidence, 253 charted IDs corresponded to only 203
exact display names, although that reconstruction includes prewarm evidence
and is not the official 239 metric.

The target currently means release-imprint IDs, matching the preceding
handoff and audit. However, reporting both ID breadth and normalized brand
breadth is advisable before calling the result historically credible.

### 6.3 Named templates are activated at historically impossible dates

All named templates are active in January 1960 while `foundedYear` is
randomized. Examples include:

- Dimension, historically founded in 1962;
- Red Bird, historically founded in 1964;
- Stax seeded under the Stax name before the 1961 Satellite-to-Stax rename.

The ontology also mixes parent companies, imprints, and distribution vehicles
as peer `AILabel`s, such as EMI, Parlophone, and Capitol. For this simulation,
the acceptance metric should be documented explicitly as release imprints,
not corporate firms.

### 6.4 Major distribution networks are randomly incomplete

`AILabelFactory.GetDistributionRegions` samples 5–7 regions with replacement
and drops duplicates. As a result, nominal Majors usually own only about four
or five of seven regions. Since a deal grants the distributor’s actual
regions, a Major distributor is often not national.

A low-RNG-disturbance repair is possible: consume the existing random draws,
then return all seven canonical regions for Majors.

### 6.5 Pittsburgh falls through the distance model

Pittsburgh is generated as a valid headquarters city and maps to the East
Coast region, but it is absent from `DistanceModel`. Runtime Pittsburgh firms
therefore use the `domestic-unmapped` New York hub fallback. There were 20
such founders in the latest checkpoint.

### 6.6 Artist release history is added twice

`RosterManager.RecordReleased` adds to `artist.releaseHistory`, and both
`CompetitorManager.TryReleaseRecord` and `ReleasePreparedRecord` add it again.
The known consumer saturates at three prior projects, so the second release
can behave like the third. This is a genuine lifecycle bug but could alter
genre-supply behavior broadly; inspect and probe before changing it.

### 6.7 Prewarm age and inventory are internally inconsistent

Prewarm records receive release dates 1–20 weeks in the past, but runtime age
starts at zero and the prewarm loop advances only eight ticks. They also
receive substantial stock in every region regardless of distribution. This
can distort 1960 and explains why prewarm/left-censor semantics need special
handling in cumulative audits.

## 7. Immediate resume sequence

1. Finish adjudicating the active-factory initialization, Major-network, name
   duplication, Pittsburgh, and duplicate artist-history findings.
2. Keep changes scoped to direct bugs and evidence-based access. Do not add
   more founder volume.
3. Rebuild:

   ```powershell
   dotnet build "Label Man.sln" --no-restore
   ```

4. Run the cheap 52-week D5/D6 probe suite:

   ```powershell
   $godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
   & $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
     --weeks=52 `
     --run=d7-chart-access-systemic-probes-52-1001 `
     --seed=1001 `
     --enable-genre-market-v2 `
     --enable-artist-population-lifecycle `
     --genre-market-v2-probes `
     --artist-population-lifecycle-probes `
     --lean-probe
   ```

5. Only after probes pass, run a 312-week checkpoint:

   ```powershell
   & $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
     --weeks=312 `
     --run=d7-chart-access-systemic-312-1001 `
     --seed=1001 `
     --enable-genre-market-v2 `
     --enable-artist-population-lifecycle `
     --lean-probe `
     --profile-performance
   ```

6. Analyze before spending the decade run:

   - cumulative charting IDs by 1965;
   - first-chart tier/origin/deal-state mix;
   - offer outcomes and old-versus-new gate counterfactuals;
   - runtime founder released → evidence → offered → signed → charted funnel;
   - annual Single units and chart run lengths;
   - exact duplicate display-name breadth;
   - Major/MidTier dominance and Small-label tail.

7. A useful checkpoint trajectory is roughly 280–320 cumulative labels
   through 1965, subject to later-year turnover. If the checkpoint remains
   near 239, inspect the emitted funnel instead of blindly running 522 weeks.
8. If the checkpoint is credible, run the 522-week acceptance:

   ```powershell
   & $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
     --weeks=522 `
     --run=d7-chart-access-systemic-522-1001 `
     --seed=1001 `
     --enable-genre-market-v2 `
     --enable-artist-population-lifecycle `
     --lean-probe `
     --profile-performance
   ```

9. Accept only if the 1969 cumulative release-imprint identity count is
   400–600 and the mix/volume guardrails remain credible.
10. Run forced deal exit and renew integrations, build, fixed probes,
    `git diff --check`, and preferably at least one holdout seed before final
    acceptance.

Before launching another Godot command, check for a still-running process:

```powershell
Get-Process | Where-Object { $_.ProcessName -like 'Godot*' }
```

Godot sometimes outlives the shell tool timeout. Do not accidentally launch a
second run against the same output or working tree.

## 8. Current judgment

The evidence supports a systemic repair, not a numerical chart-count knob:

```text
founders/releases abundant
        ↓
regional proof exists
        ↓
old eligibility discards proof or permanently closes at reach 0.40
        ↓
few distribution deals
        ↓
very few new Independent identities reach the national chart
```

The current candidate makes regional proof persistent and physically
grounded, preserves offer/acceptance friction, repairs disappearing historical
evidence, fixes awareness aging, and adds the telemetry needed to determine
whether a new first chart happened before or after distribution.

It is promising but **not yet an accepted result**. The next expensive action
must be the 312-week checkpoint, not a full decade.

## 9. Live validation update

The candidate was rebuilt after the telemetry and probe changes:

```text
dotnet build "Label Man.sln" --no-restore
Build succeeded.
0 errors; one pre-existing unused-event warning.
```

The first two sandboxed Godot launches crashed in native startup because the
sandbox denied the engine access to its AppData runtime directories. They
produced no simulation output and are not model failures. The user explicitly
authorized the Downloads executable to run outside the sandbox.

The out-of-sandbox probe run completed:

```text
run=d7-chart-access-systemic-probes3-52-1001
D5 passed
D6 fixed probes 1-77 passed
CHART_AUDIT_COMPLETE
```

The usual post-completion `MissingSingletonsTemp.cs does not inherit from
Node` error and ObjectDB leak warning remain pre-existing.

The probe-year audit also verified that the new compact files are populated:

- first-chart events: 155 identities;
- run-start/left-censored first observations: 48;
- live first observations: 107;
- exact display names among those 155 IDs: 130;
- distribution-attempt rows: 5,022;
- rows with persistent strong-region evidence: 254;
- signed offers: 53;
- rejected offers: 50;
- no-distributor outcomes: 0;
- chance misses: 463.

The 1960 concentration row was:

```text
annual IDs charting=151
cumulative imprint IDs=155
cumulative exact display names=130
tier mix Small/Boutique/Independent/MidTier/Major=4/25/46/70/10
```

This confirms both the expected access effect and material display-name
duplication. It is only one year and must not be extrapolated as acceptance.

Two audit correctness repairs were added before this probe:

- chart-hit bookkeeping now subscribes during `CompetitorManager.Initialize`,
  because autoload order can leave `ChartManager.Instance` unavailable during
  `CompetitorManager._Ready`;
- the initial live chart is seeded into cumulative breadth at week zero and
  first-chart rows explicitly distinguish `RunStartChart` /
  `leftCensoredAtRunStart=true` from live first observations.

The concentration CSV now appends
`cumulativeExactLabelNamesCharting` alongside immutable imprint-ID breadth.
This does not redefine the current acceptance metric, but makes name-based
identity inflation visible.

An accidental sandboxed `--editor --quit` diagnostic generated 391 untracked
Godot `.import`/`.translation` sidecars and one `.uid`. They were immediately
removed with path-checked workspace-local cleanup. `.claude/` was untouched.

Next action: run `d7-chart-access-systemic-312-1001`, inspect its funnel and
trajectory, and only then decide whether a 522-week run is justified.

## Independent historical/systemic audit

This section is an independent read-only audit of the active 1960-1969 label,
release, distribution, geography, and chart paths. The just-completed
`d7-chart-access-systemic-312-1001` checkpoint was run **before** the deferred
factory, prewarm, identity-history, and chronology corrections below. Treat
that checkpoint as evidence for the systemic chart-access candidate, not as a
measurement of those later corrections.

### Fix-now defects

1. **The active launch-label factory creates incomplete operating profiles.**
   `ChartManager.GenerateAILabelsIfNeeded` uses `AILabelFactory`, whose
   `ApplyTierStats` never initializes `riskTolerance`, derives
   `artistLoyalty` from its zero default, and explicitly omits archetype
   modifiers (`Systems/ChartManager.cs:277-286`;
   `Systems/AILabelFactory.cs:162-205`). All launch labels therefore begin
   with zero risk tolerance and loyalty in the 0.00-0.10 range, directly
   distorting unknown-artist scouting and re-signing. The same factory leaves
   historical `monthsActive` and pre-1960 track record at defaults despite
   assigning a founding year.

2. **Release-imprint IDs and distinct label identities are not equivalent.**
   The factory has no name registry, while `NameGenerator` deliberately
   re-emits real label names (`Systems/AILabelFactory.cs:88-108,134-159`;
   `Systems/Naming/NameGenerator.cs:807-879`). The earlier 312-week artifact
   contained 600 launch IDs but only 410 exact display names; reconstructed
   chart history contained 253 IDs but 203 exact names. Keep both immutable
   imprint-ID breadth and exact/normalized-name breadth visible, and either
   deduplicate generated firms or explicitly define the 400-600 target as
   release-imprint IDs.

3. **Prewarm age is disconnected from release date.** Initial records are
   dated one to twenty weeks before game start, but every runtime age starts
   at zero and all records participate in eight synthetic ticks
   (`Systems/CompetitorManager.cs:280-305`;
   `Data/RecordRuntimeData.cs:130`;
   `Systems/ChartManager.cs:409-434`). Stage records by their dated release or
   initialize their synthetic age consistently.

4. **Prewarm bypasses the live physical-distribution model.**
   `BootstrapPrewarmRecord` seeds 5,000-20,000 units in every region regardless
   of tier or covered regions (`Systems/CompetitorManager.cs:338-370`). This
   gives the installed cohort national stock that a live Small or Independent
   entrant cannot obtain and can create incumbent chart crowding.

5. **Seeded Major networks are not reliably national.**
   `GetDistributionRegions` samples regions with replacement and discards
   duplicate draws (`Systems/AILabelFactory.cs:308-320`), so a Major commonly
   covers only four or five of seven regions. Deal terms then inherit only the
   distributor's stored regions. Generate Major networks without replacement
   or explicitly grant all canonical regions for a qualifying national deal.

6. **Founding dates do not control activation.** Procedural launch labels can
   draw a 1961 founding year while operating in January 1960, and every named
   template is activated immediately with a randomized founding year
   (`Systems/AILabelFactory.cs:111-159`). Dimension (1962) and Red Bird (1964)
   therefore operate early, while Stax is present under its later name and at
   MidTier before the 1961 Satellite-to-Stax change. Gate activation by an
   authored founding date or remove future labels from the launch population.

7. **Pittsburgh is missing from the canonical distance substrate.**
   `LabelGenerator` can found labels there, but `DistanceModel` has no
   Pittsburgh city or alias (`Systems/LabelGenerator.cs:23-26,130-143`;
   `Systems/DistanceModel.cs:26-43,108-117,216-253`). All 20 Pittsburgh
   founders in the earlier runtime profile fell back to New York as
   `domestic-unmapped`.

### Already repaired in the systemic candidate

- Mutable awareness stock no longer receives an increasing age exponent every
  week. The old path produced a triangular `.95^55` factor by age 18 instead
  of ten weekly `.95` applications
  (`Systems/ChartSimulator.cs:543-554`).
- Retired releases now retain recent chart/Top-40 evidence, and "recent
  Top 40" uses historical peak rather than current position
  (`Systems/CompetitorManager.cs:709-724,3148-3187`).
- Label release, Top-40, and number-one totals are updated from live events
  (`Systems/CompetitorManager.cs:2668-2683`).
- Acquisition no longer retroactively erases the release imprint used for
  breadth telemetry (`Data/RecordRuntimeData.cs:13,118`;
  `SimTools/ChartAuditRunner.cs:2297-2305`).

### Document/defer historical and modeling issues

- Runtime headquarters is selected before `RuntimeLabelProfileFactory`
  replaces the archetype, so scene and specialization are independent
  (`Systems/LabelGenerator.cs:130-143`;
  `Systems/RuntimeLabelProfileFactory.cs:39-47`). In the earlier 382-founder
  sample, none of 37 SoulFactories was in Detroit and only three of 67
  CountrySpecialists were in Nashville.
- Runtime founders specialize in only seven preferred genres
  (`Systems/RuntimeLabelProfileFactory.cs:29-100`). They cannot be founded as
  dedicated folk-rock, psychedelic, jazz, funk, British, Latin/Tex-Mex,
  boogaloo, ska, or reggae labels despite those genres' authored decade
  emergence.
- Runtime geography repair fixes the home region but preserves the random
  legacy distribution regions drawn while `MarketRegion.majorCities` is
  empty (`Systems/LabelGenerator.cs:255-280`;
  `Systems/RuntimeLabelProfileFactory.cs:141-163`). This can help access, but
  it produces geographically incoherent networks.
- The historical identity model mixes operating companies, parents, imprints,
  and distributors as peer labels: for example EMI/Parlophone/Capitol,
  Duke-Peacock/Peacock, and Decca UK as an East Coast competitor. Decide
  whether the target represents release imprints or operating firms.
- Motown and Stax start at MidTier in 1960 even though the tier definition
  describes the mature mid-1960s large-independent state
  (`Systems/AILabelFactory.cs:15-16,93`;
  `Data/ContactEnums.cs:38-43`).
- The 18-week never-charted retirement backstop is historically aggressive
  (`Systems/ChartManager.cs:48-50,1774-1789`). Reassess it only after the
  direct factory/prewarm defects are corrected, rather than tuning around
  those defects.

## Independent patch-safety review

An independent review of the uncommitted systemic candidate found no compile
or whitespace failures: `dotnet build "Label Man.sln" --no-restore` and
`git diff --check` both passed.

The following review findings were fixed immediately afterward:

- **Chart-hit subscription order:** `CompetitorManager` autoloads before
  `ChartManager`, so subscribing in `CompetitorManager._Ready` could silently
  leave live Top-40 and number-one counters disconnected. Subscription now
  occurs during `CompetitorManager.Initialize`, before prewarm, with duplicate
  subscription protection.
- **Left-censored chart breadth:** run-start chart records are now seeded into
  cumulative breadth before the first simulated tick, and first-observation
  telemetry distinguishes `RunStartChart` /
  `leftCensoredAtRunStart=true` from live first charts.
- **Probe-number label:** the earned-reach demand assertion is now labeled
  probe 73 rather than probe 74.

Remaining concerns to preserve for follow-up:

1. **Recent-chart evidence is still aged from release, not chart occurrence.**
   Active queries compare `weeksSinceRelease`, while compact retired history
   stores a release week plus chart/Top-40 booleans. A record released more
   than 52 weeks ago but charting now can therefore be excluded, and a late
   chart can expire based on release age. A fully correct lookback needs the
   last chart and last Top-40 weeks.

2. **Persistent regional deal evidence exists only while the record remains
   active.** Pull evaluation scans `ChartManager.GetAllRecords()`, but retired
   history stores no strong-region breakout peak. A qualifying record retired
   before the next monthly offer check loses its proof. Consider a bounded
   retired regional-evidence lookback if checkpoint telemetry shows this seam
   remains material.

3. **Annual concentration can retroactively move pre-acquisition sales.**
   Weekly units are already captured under the owner at that week, then
   `ResolveCurrentOwner` rolls those IDs to the year-end acquirer. This can
   distort annual firm counts and C4/C8 even though cumulative release-imprint
   breadth is now correct.

4. **Acquisition rows mix attribution domains.** First-chart deal counts are
   keyed to the immutable release imprint, while active capability and deal
   fields describe the current owner. Acquired-record rows should be treated
   carefully or expose both firms' deal histories explicitly.

5. **The new pull route needs an overshoot check, not another blind tuning
   change.** Once an active record has persistent qualifying evidence, repeated
   monthly 40% rolls make an offer increasingly likely. Use the 312-week
   attempt/outcome funnel to verify that offer frequency, rejection, physical
   distributor availability, chart conversion, and tier mix remain credible.

The awareness-decay correction and immutable release-imprint field were found
sound. The strong-region evidence route is materially better grounded than
the former hidden quality/current-sales/national-reach gates, subject to the
bounded-retirement and repeated-offer concerns above.
