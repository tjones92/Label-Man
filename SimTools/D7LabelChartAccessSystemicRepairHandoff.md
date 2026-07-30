# D7 label chart access systemic repair — live handoff

Last maintained: July 29, 2026.

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

## 10. Root cause found: region-blind absolute breakout thresholds

This section supersedes the prior judgment that the binding seam was deal
eligibility. It was not. Deal access had already been opened: in the
`d7-chart-access-systemic-312-1001` funnel, 173 of the 195 clients that ever
produced qualifying evidence signed a deal — 89%. The gate had moved upstream
to **producing regional evidence at all**, and that gate was structurally shut
for most of the map.

### 10.1 The defect

`ChartManager.UpdateRegionalBreakoutState` scored regional breakout evidence
against fixed absolute weekly unit counts:

```text
rawVolume      = clamp((rawDemand - 150) / 3500)
fulfilledVolume= clamp(unitsSold / 3000)
velocity gate  = previousDemand >= 150
unmet          = backordered / max(750, rawDemand)
collapse       = rawDemand < 1500
```

Authored region populations span 11.3x (East Coast 52.2M, Rockies 4.6M), and
`rawDemand` scales directly with `region.population`. A record performing
identically *per capita* therefore scored an order of magnitude less evidence
outside the two largest regions. `volumeInput` enters the score twice — once
weighted 0.34 and again as the multiplier `0.55 + volumeInput * 0.45` — so the
penalty compounds.

Measured from `geography-metrics.csv`, per-record weekly units for
Independent/Boutique labels:

| region | pop | mean | P90 | P99 | max |
|---|---:|---:|---:|---:|---:|
| eastcoast | 52.2 | 168 | 333 | 792 | 5216 |
| greatlakes | 36.1 | 111 | 216 | 507 | 3273 |
| westcoast | 20.3 | 75 | 149 | 359 | 1846 |
| greatplains | 15.5 | 46 | 88 | 213 | 1265 |
| deepsouth | 15.0 | 41 | 78 | 180 | 1073 |
| southwest | 14.2 | 43 | 80 | 191 | 877 |
| rockies | 4.6 | 14 | 27 | 57 | 354 |

Against a flat 150-unit floor, **99% of Rockies and 90% of Deep South
record-weeks produced zero volume and zero velocity evidence.** The all-time
best Rockies record reached rawVolume 0.058.

### 10.2 Consequence

Charting was near-binary on regional breakout peak. Bucketing the 879 distinct
offer-attempt clients by their best observed peak:

| best peak | labels | charted | rate |
|---|---:|---:|---:|
| 0.00 (no signal) | 127 | 0 | 0% |
| 0.00-0.10 | 52 | 0 | 0% |
| 0.10-0.18 | 217 | 0 | 0% |
| 0.18-0.24 | 127 | 0 | 0% |
| 0.24-0.30 | 76 | 2 | 2.6% |
| 0.30-0.40 | 143 | 77 | 53.8% |
| 0.40+ | 137 | 90 | 65.7% |

Zero of 523 labels below 0.24 ever charted. Runtime founder charting by home
region made the mechanism unambiguous:

| region | pop | founders | charted | rate |
|---|---:|---:|---:|---:|
| eastcoast | 52.2 | 76 | 19 | 25.0% |
| greatlakes | 36.1 | 62 | 18 | 29.0% |
| westcoast | 20.3 | 43 | 3 | 7.0% |
| greatplains | 15.5 | 16 | 0 | 0% |
| deepsouth | 15.0 | 80 | 0 | **0%** |
| southwest | 14.2 | 18 | 0 | 0% |

Confounds were ruled out: Deep South founders had the **highest** mean owned
reach (0.425) and comparable production quality, scouting, marketing, and
budget. Region population was the only differentiator. Zero Deep South labels
charting across a decade — Memphis, Muscle Shoals, Nashville, New Orleans — is
simultaneously the systemic chart-access cause and the single largest
historical inaccuracy in the model.

### 10.3 Repair

`MarketRegion.GetRecordBuyingPopulation()` exposes the weekly record-buying
population. `ChartManager.GetRegionalDemandScale(region)` returns that as a
share of the largest authored region's, and every threshold above is multiplied
by it. Anchoring on the largest market leaves that market's long-standing
calibration untouched and only relieves the smaller ones.

The authored buying-population model matches observed demand closely
(deepsouth model 0.2315 vs observed 0.244; rockies 0.0815 vs 0.0833), and
normalizing equalizes mean evidence across all seven regions to 0.005-0.008.
No arbitrary floor is applied — a floor would reintroduce the handicap.

Probe 78 asserts the largest market keeps its calibration, that equal
per-capita performance yields equal evidence, that a smaller-market hit is no
longer scored against the largest market's thresholds, and that a degenerate
region falls back to unscaled behavior.

### 10.4 Measured effect (region scaling alone)

`d7-region-scaled-breakout-312-1001`, seed 1001:

| year | prior | new | new IDs/yr | prior IDs/yr |
|---|---:|---:|---:|---:|
| 1960 | 162 | 177 | 177 | 162 |
| 1961 | 188 | 221 | 44 | 26 |
| 1962 | 209 | 250 | 29 | 21 |
| 1963 | 229 | 282 | 32 | 20 |
| 1964 | 246 | 307 | 25 | 17 |
| 1965 | 258 | **331** | 24 | 12 |

331 through 1965 clears the 280-320 credibility band. The per-year decay that
capped the old trajectory is largely flat. Regional charting rates became
eastcoast 31.8 / greatlakes 32.2 / westcoast 27.6 / deepsouth 22.9 /
southwest 13.3 / greatplains 12.5 percent. Runtime founder conversion doubled
from 11.0% to 21.7%. 1965 cumulative mix: Small 25, Boutique 43, Independent
175, MidTier 78, Major 10 — below-MidTier is 73% of the population and 72% of
that is Independent, with a 7.6% Small tail. Indie family chart share rose
0.159 to 0.254 while C4 fell 0.378 to 0.261.

## 11. Distribution deals are now per-song and coverage-derived

Per user direction. Two defects were found in the deal model:

1. **The grant was label-wide.** `activeDeal` is one field on `AILabel`, and
   `borrowedReach` / `distributionStrength` / `effectiveNationalReach` feed
   `GetLiveLabelDemandScale`, which multiplies weekly demand for every live
   record. One breakout single put the label's entire back catalog into the
   distributor's network.
2. **`reachGranted` was unrelated to the distributor's network.** It was an
   independent `RandRange(0.50,0.80)` push / `RandRange(0.30,0.50)` pull draw,
   so a three-region distributor could grant more national reach than a
   seven-region one.

The region grant itself was already correct: `GetGrantedDistributionRegions`
takes the distributor's owned `distributionRegions` minus what the client has.

Repair: `DistributionDeal.coveredRecordIds` plus per-record accessors on
`AILabel` (`HasDistributionInRegionForRecord`, `BorrowedReachForRecord`,
`DistributionStrengthForRecord`, `EffectiveNationalReachForRecord`). Coverage
is the record whose breakout earned the deal, bound at signing from
`RegionalDealEvidence.EarningRecordId`, plus everything released during the
term via `TrackRelease`. `reachGranted` is the negotiated range scaled by
`GetNationalMarketShareForRegions(distributor.distributionRegions)`. Probe 79
covers all five behaviors including withdrawal at termination.

## 12. Calibration trap: mechanism fixes that inflate incumbents

**Read this before fixing any other sampler or initializer in this repo.**

Three genuine defects were fixed alongside the above:

- `AILabelFactory.ApplyTierStats` never initialized `riskTolerance` (0, which
  halves the evaluation score of any artist under 0.1 reputation), derived
  `artistLoyalty` from its zero default, and left the archetype modifiers as an
  unimplemented placeholder comment. `RuntimeLabelProfileFactory` is the
  complete reference implementation, and its own `HasCompleteOperatingProfile`
  contract requires `riskTolerance > 0` — which no launch label could satisfy.
- `GetDistributionRegions` sampled with replacement and discarded duplicates.
- Pittsburgh had no `DistanceModel` node, so every Pittsburgh firm resolved as
  `domestic-unmapped` and was charged distance from the New York hub.

The distribution-region fix was the trap. The authored counts encoded
*observed* coverage under the bug, so reading them literally inflated coverage
**regressively**:

| tier | authored | old effective | literal | inflation |
|---|---|---:|---:|---:|
| Major | 5-8 | 4.76 | 7.00 | +59% |
| MidTier | 3-6 | 3.96 | 5.50 | +52% |
| Independent | 1-4 | 2.86 | 3.50 | +35% |
| Boutique | 1-3 | 2.56 | 3.00 | +28% |
| Small | 0-2 | 1.82 | 2.00 | +22% |

Combined with the launch-factory initialization, this strengthened the 600
incumbents enough to erase the entire region-scaling gain:
`d7-per-song-deal-312-1001` fell to 150/187/211 for 1960-62 against 177/221/250,
with indie chart share dropping to 0.057 — below the original baseline. That
run was stopped at 1962 once the trajectory was unambiguous.

Correction: non-Major counts restated to preserve former expected coverage
(MidTier 2-4, Independent 1-3, Boutique 1-2, Small unchanged) while sampling
stays without replacement. Majors keep the literal reading and are granted all
seven regions, because a Major is a national distributor by definition and that
is what makes a signed deal worth anything.

**Rule for the next pass:** compute the old effective value of any constant
whose sampler or initializer you repair, and restate the constant to preserve
it, so the mechanism fix is calibration-neutral. Ship mechanism and calibration
changes separately or the result is unattributable.

### 12.1 Rejected: seeding `monthsActive` from `foundedYear`

Section 6.1 and fix-now item 1 of the earlier audit list this as a defect. It is
not, and it was implemented and then reverted after measurement.

Despite its name, `monthsActive` is an **in-simulation observation counter**,
not a historical attribute:

- `LabelLifecycleManager.UpdateLabelHealth` increments it once per month
  (`Systems/LabelLifecycleManager.cs:153`);
- `RuntimeLabelProfileFactory.ReconcileFoundingAndGeography` resets it to zero
  alongside `totalReleases`, `top40Hits`, `numberOneHits`, and `momentumScore`
  — in-run accumulators, not authored history
  (`Systems/RuntimeLabelProfileFactory.cs:142`);
- every gate reading it pairs it with in-run evidence:
  `MidTierPromotionMinimumOperatingMonths` sits beside a sustained-quarters and
  recent-charting-records requirement, and `GetCompetitiveExitChance` beside
  `chartingLastYear` (`Systems/LabelLifecycleManager.cs:283-289`).

Seeding it with 144-180 months of pre-1960 history made every seeded incumbent
immediately eligible for MidTier promotion and competitive exit without having
earned either in-run, which is not what those gates are asking.

The revert rests on that code reading, not on a measured effect. An earlier
draft of this section attributed the elevated Major-tier count in
`d7-recalibrated-coverage-312-1001` to this seeding; that attribution was wrong
and is retracted. The same elevated count (12 cumulative Majors charting in
1960, rising to 13) persists in `d7-systemic-consolidated-312-1001` *after* the
revert, so it is RNG-composition drift from the two extra `GD.RandRange` draws
`ApplyTierStats` now consumes and from the reworked region-sampling loop, not a
promotion-gate effect. Any run comparison across these passes is a different
random realization, not a controlled A/B.

`foundedYear` remains the authored historical fact. Anything that wants "years
since founding" should derive it from `foundedYear` rather than overloading the
in-run counter.

## 13. The tier-mix guardrail was unsound, and the seeded market was too big

The user challenged the tier guardrail directly. It does not survive scrutiny,
though the failure is the opposite of the one suspected.

### 13.1 The guardrail measured headcount and was quoted against chart share

`cumulative*FirmsCharting` counts **distinct label identities that ever
charted**. `majorFamilyChartShare` measures **share of annual chart units**, and
`IsIndieFamily` (`SimTools/ChartAuditRunner.cs:2435`) is
Independent+Boutique+Small, so its complement is Major **plus MidTier**.

These are not comparable. A long tail of 175 Independents each charting one or
two records is 73% of the *firms* and 12% of the *chart*. Reporting "below-MidTier
is 73% of the charting population" beside a chart-share target was an error in an
earlier version of this document.

The guardrail as originally written is also narrower than it was later quoted as
being: it says the below-MidTier charting population should be dominated by
Independents *rather than Small labels* — a composition rule **within** that
group. It never constrained the overall split, and therefore **could be fully
satisfied while Major+MidTier took 85% of chart entries**, which is what was
happening.

### 13.2 Measured entry-level mix, 1960-65

From `d7-systemic-consolidated-312-1001`, joining `lifecycles.csv` to
`single-release-lanes.csv` on release tier (use a real CSV parser — record titles
contain commas and a naive split silently corrupts every column after `title`):

| tier | chart entries | chart-weeks | Top 40 entries |
|---|---:|---:|---:|
| Major | 40.9% | 48.5% | 65.3% |
| MidTier | 44.5% | 37.0% | 28.9% |
| Independent | 12.2% | 12.2% | 5.2% |
| Boutique | 2.1% | 1.9% | 0.6% |
| Small | 0.3% | 0.3% | 0.0% |

Major share was defensible. **MidTier at 44.5% of entries was the anomaly**, and
the true independent tail at 14.6% was far too thin.

### 13.3 Historical grounding, with its limits stated

The best quantitative source located is Peter Tschmuck's Billboard Hot 100
analysis of the 1960s, measured in **weeks at number one** across all 518 weeks
of the decade:

- decade: majors 57.3%, independents 42.7%;
- 1960: 80.0% / 20.0%; 1963: 46.2% / 53.8%; 1964: 62.0% / 38.0%;
  1965: 62.7% / 37.3%; 1967: 73.1% / 26.9%; 1969: 53.8% / 46.2%;
- 57 distinct labels reached number one during the decade;
- majors defined as EMI/Capitol, CBS-Columbia, RCA Victor, Warner Bros.
  (with Reprise, and Atlantic after 1967), Decca, ABC, MGM, and the Hollywood
  studio labels.

**Three caveats that matter for using these numbers:**

1. **Number-one weeks are the most concentrated metric available.** Entry-level
   major share across all 100 positions is necessarily *lower* than the #1-week
   share, because independents carried a long low-charting tail. Do not compare
   a #1-week percentage directly against `chartEntries*` telemetry.
2. **The period split is binary and does not map to this model's tiers.**
   Industry usage was major (owns its distribution) versus independent (does
   not). Motown, Atlantic before 1967, Vee-Jay, Chess and Stax are all
   *independents* in that source. The model's MidTier/Independent boundary is a
   modeling convenience with **no historical counterpart**, which is exactly why
   it felt blurry. Any guardrail should therefore be stated primarily on the
   binary Major-versus-everything-else split, with the MidTier/Independent
   division as a secondary internal check only.
3. **No published entry-level major/independent split for the 1960s Hot 100 was
   found.** The 35-40% major and 20-25% MidTier figures in the directive are a
   reasonable reading but are not sourced, and the MidTier figure in particular
   has no period definition behind it.

### 13.4 The seeded market carried four times too many large firms

`GetRandomTier` drew Major at 1% and MidTier at 14%, which with the named
templates produced **98 MidTier and 13 Major firms out of 600**. The 1960 market
had roughly eight corporate majors and on the order of twenty to twenty-five
national independents.

Repairs:

- `GetRandomTier` now draws Major 0.2% and MidTier 3.3%, with the freed mass
  going mostly to Independent (25% to 33%).
- `LabelTemplate` carries an optional per-template 1960 tier, because named firms
  were previously tiered by which array they happened to sit in. Motown and Stax
  now start Independent (Motown was months old; Stax was still Satellite) and
  must earn MidTier through the promotion ladder. Chancellor, Colpix, Dimension
  and Red Bird drop to Independent. EMI and Decca UK become Major.
- Probe 81 pins majors to 4-14 and MidTier to 10-40, requires regional
  independents to outnumber national ones by 3x, requires Motown and Stax to
  start below MidTier, and requires every seeded label to satisfy the
  operating-profile contract.

Measured at 52 weeks (`d7-tier-population-probes-52-1001`): seeded large firms
fell from 111 to 42 (Major 8, MidTier 34). The 1960 entry mix moved to Major
40.4%, MidTier 31.2%, Independent 21.8%, Boutique 5.8%, Small 0.9% — indie family
28.5%, up from 14.6%. Indie chart-unit share rose from 0.082 to 0.151.

### 13.5 New telemetry

`concentration.csv` gained twelve columns:
`chartEntries`, `chartEntries{Small,Boutique,Independent,MidTier,Major}`,
`top40Entries`, `top40{Small,Boutique,Independent,MidTier,Major}`. These count
**distinct charting records per year by release-imprint tier**, which is the
analogue of a Billboard chart entry and the figure historical splits are quoted
against. The tier guardrail should be restated on these columns; the firm-count
columns answer a different question and cannot detect over-representation.

## 14. Open risk and required next step

**Do not treat section 13 as accepted.** Two unresolved signals:

1. **1960 cumulative charting identities fell to 142**, against roughly 173-177
   in the runs before the re-tiering. Fewer large firms means fewer firms capable
   of charting early. The thesis is that the promotion ladder lets Independents
   climb across the decade and more than repay this, but **that is unmeasured** —
   no run longer than one year has been executed against the re-tiered
   population, per the user's instruction to hand off before any run longer than
   two years.
2. **Top 40 major share rose to 70.1% in 1960** from 65.3%, because fewer MidTier
   firms compete at the top. Historically 1960 was the most major-dominated year
   of the decade (80% of #1 weeks), so 70% of Top 40 entries in 1960 is not
   obviously wrong, but it must be checked across later years where the
   historical share falls to roughly 46-63%.

### 14.1 Resume sequence

1. Run the 312-week checkpoint:

   ```powershell
   & $godot --headless --path . SimTools/ChartAuditRunner.tscn -- `
     --weeks=312 --run=d7-tier-population-312-1001 --seed=1001 `
     --enable-genre-market-v2 --enable-artist-population-lifecycle `
     --lean-probe --profile-performance
   ```

2. Read `chartEntries*` and `top40*` per year and compare against: Major 35-50%
   of entries (higher in 1960-61, falling mid-decade), non-major 50-65%, and a
   Major Top-40 share that declines from roughly 70% in 1960 toward 45-60% by
   1965. Confirm the MidTier entry share sits well below its former 44.5%.
3. Read `cumulativeFirmsCharting`. The prior best was 331 through 1965 with
   region scaling alone; the consolidated configuration reached 291 and the
   prewarm-corrected one 279. If the re-tiered run does not clear roughly 300
   through 1965, the large-firm reduction is costing more breadth than the
   promotion ladder returns, and the MidTier draw should be raised toward 5-6%
   rather than the access model being retuned.
4. Only then consider the 522-week acceptance run.

### 14.2 Trajectory ledger for seed 1001

| configuration | 1965 cumulative | note |
|---|---:|---|
| pre-existing baseline | 258 | before this pass |
| region scaling only | **331** | best measured |
| + per-song deals, factory init, region-count restatement | 291 | |
| + prewarm physical distribution | 279 | |
| + large-firm re-tiering | 293 | `d7-tier-population-312-1001`, see section 15 |

**These are not controlled comparisons.** Several changes altered the number of
`GD.RandRange` draws consumed during label generation, so each configuration is a
different random realization of the same seed. Differences of ten to twenty
identities should not be read as causal. A holdout seed is required before any
acceptance claim.

## 15. Re-tiered checkpoint measured, and the promotion thesis is refuted

`d7-tier-population-312-1001` (seed 1001) completed all 312 weeks, exit code 0.
No code changed between section 13 and this run; the only worktree action was
restoring the deleted `SimLogs/.gdignore` marker. The 81 fixed probes therefore
still stand from `d7-tier-population-probes-52-1001`.

### 15.1 Trajectory

| year | cumulative | new/yr | Small | Boutique | Independent | MidTier | Major |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 138 | 138 | 4 | 23 | 75 | 28 | 8 |
| 1961 | 177 | 39 | 12 | 29 | 98 | 30 | 8 |
| 1962 | 207 | 30 | 16 | 36 | 117 | 30 | 8 |
| 1963 | 235 | 28 | 20 | 41 | 136 | 30 | 8 |
| 1964 | 264 | 29 | 25 | 43 | 158 | 30 | 8 |
| 1965 | **293** | 29 | 26 | 44 | 185 | 30 | 8 |

293 narrowly misses the 300 credibility bar in section 14.1, but **the annual
accretion is flat at 28-30 from 1962 onward**, where the pre-existing baseline
decayed to +6..+8 and the consolidated configuration to +18. Linear extension of
+29/yr gives roughly **409 by 1969**, at the bottom edge of the 400-600 target.
That extrapolation is the open question a 522-week run has to settle; it is not
a result.

### 15.2 The promotion ladder contributes nothing, and structurally cannot

Section 14 rested on the thesis that promoted Independents would repay the lost
seeded MidTier identities. **That is false, and it is false by construction.**

Of 293 first-chart identities, only 6 differ between `birthTier` and
`firstChartTier`: 2 Small→Independent, 2 Boutique→Independent, and 2
Independent→Small *demotions*. **Zero promotions into MidTier.** All 30 MidTier
and all 8 Major first-chart identities were born at that tier. The same holds in
`d7-systemic-consolidated-312-1001`: 7 of 291 moved, zero into MidTier.

The mechanism:

- `firstChartTierByLabel[releaseLabelId]` is assigned only inside the
  `cumulativeChartingLabelIds.Add(...)` branch (`SimTools/ChartAuditRunner.cs:2337-2341`)
  and is never revised, so the tier bucket is frozen at a label's first chart.
- `IsIndependentReadyForMidTier` requires `chartingLastYear >= 2`
  (`Systems/LabelLifecycleManager.cs:467-477`). A label cannot meet that without
  having already charted — by which point it is permanently bucketed Independent.

Therefore `cumulativeMidTierFirmsCharting` can never exceed *seeded MidTier firms
that ever chart*. It is pinned at 28-30 here against 34 seeded (88.2%), and
saturates at 76-78 in every pre-re-tiering configuration against ~86-98 seeded.

There is also a live deadlock worth recording: charting effectively requires an
active deal (169 of 293 first charts, and 58 of 70 runtime-founded ones), while
promotion requires `ownedReach >= 0.50` **and** dependency `< 0.35`, which an
active deal defeats. `GrowSelfBuiltDistributionReach` additionally early-returns
whenever `activeDeal != null`, so a label on a deal cannot even accrue owned
reach. The two conditions compete inside the same 52-week window.

### 15.3 The prescribed remedy is quantitatively inadequate

Section 14.1 step 3 directs raising the MidTier draw toward 5-6% if the run misses
300. Measured seeded conversion in this run:

| seeded tier | seeded | charted | conversion |
|---|---:|---:|---:|
| Major | 8 | 8 | 100.0% |
| MidTier | 34 | 30 | 88.2% |
| Independent | 209 | 120 | 57.4% |
| Boutique | 114 | 46 | 40.4% |
| Small | 235 | 19 | 8.1% |

Raising MidTier from 3.3% to 5.5% adds roughly 13 seeded MidTier firms, drawn
from Independent. The expected identity gain is
`13 x (0.882 - 0.574) ~= +4` — moving 293 to about 297, while re-inflating the
MidTier entry share section 13 just corrected. **The remedy does not close a
38-identity gap. Do not spend a run on it.**

The whole 331→293 gap decomposes as MidTier −48 and Major −2, offset by
Independent +10, Boutique +1, Small +1. Independent *seeded* count rose 160→209,
but Independent *charted* rose only 175→185, because runtime-founded charting
identities fell 83→70.

### 15.4 Distributor capacity is not the constraint

Cutting large firms from 96 to 42 did not starve the deal market. Across the full
312 weeks, `NoDistributor` outcomes are **0** in both runs, and the re-tiered run
signs *more* deals than the region-scaled one:

| run | attempts | persistent evidence | signed | to runtime founders | rejected | no distributor |
|---|---:|---:|---:|---:|---:|---:|
| region-scaled | 13,300 | 1,744 | 373 | 145 | 359 | 0 |
| re-tiered | 14,457 | 2,535 | 517 | 185 | 518 | 0 |

So the lost breadth is not a distribution-access effect. Runtime founders get
*more* deals and still convert to fewer charting identities, because chart units
concentrate into the smaller surviving large-firm set (c4 rose 0.261 → 0.347).

### 15.5 Entry mix against section 14.1 step 2

| year | Major entries | MidTier | Independent | Major Top-40 | MidTier Top-40 | Ind Top-40 |
|---|---:|---:|---:|---:|---:|---:|
| 1960 | 41.2% | 31.5% | 21.8% | 71.9% | 17.9% | 8.6% |
| 1961 | 39.5% | 32.9% | 22.9% | 68.7% | 21.7% | 8.8% |
| 1962 | 40.6% | 34.6% | 21.3% | 67.2% | 23.7% | 8.3% |
| 1963 | 42.6% | 32.5% | 21.6% | 66.8% | 24.8% | 8.1% |
| 1964 | 42.2% | 30.9% | 23.5% | 64.7% | 22.2% | 12.0% |
| 1965 | 41.5% | 28.7% | 26.6% | 63.4% | 20.3% | 14.8% |

Read against the targets in section 14.1 step 2:

- **Major entry share passes the band** (35-50%) every year, but is *flat* at
  ~41% rather than falling mid-decade.
- **MidTier entry share passes decisively**: 28.7% at 1965 against its former
  44.5%, and trending down.
- **Independent entry share rises** 21.8% → 26.6%; indie-family entry share
  reaches 29.8% and `indieFamilyChartShare` 0.253.
- **Major Top-40 share misses**: it declines 71.9% → 63.4%, the right direction,
  but does not reach the 45-60% band by 1965.

The tier-mix objective of section 13 is therefore substantially met; the breadth
objective is 7 identities short of its checkpoint bar.

### 15.6 Decision taken

The choice was between accepting a marginal checkpoint and spending a run on it.
The options weighed were:

1. **Run the 522-week acceptance on the current configuration.** Justified by the
   flat +29/yr accretion, which is the first configuration in this pass that does
   not decay, and which extrapolates to ~409.
2. **Recover breadth before spending it.** The only levers with enough mass are
   Independent (57.4%) and Boutique (40.4%) conversion — *not* the seeded tier
   mix, and not distribution access. Note section 1 forbids buying the target with
   more label births.
3. Raising the MidTier draw to 5-6% was rejected on the arithmetic in section 15.3.

**Option 1 was chosen by the user.** `d7-tier-population-522-1001` was launched at
seed 1001 against the *unmodified* section 13 configuration — no code changed
between the 312-week checkpoint and this run, so the 81 fixed probes from
`d7-tier-population-probes-52-1001` still stand and the two runs are the same
configuration at different horizons.

Acceptance criteria for that run:

- 1969 cumulative release-imprint identities in **400-600**;
- Major entry share staying inside 35-50% and preferably falling after mid-decade;
- MidTier entry share staying well below its former 44.5%;
- Major Top-40 share continuing down from 63.4% at 1965 toward 45-60%;
- a small Small-label tail with the below-MidTier population Independent-dominated.

A holdout seed is still required before acceptance, and the `monthsActive`/
RNG-realization caveats in sections 12.1 and 14.2 still apply — the 331 and 293
figures are different random realizations, not a controlled A/B.

## 16. Decade run: acceptance fails, and a new late-decade defect is exposed

`d7-tier-population-522-1001` completed all 522 weeks, exit code 0, on the
unmodified section 13 configuration.

### 16.1 Result: 391 at 1969

| year | cumulative | new/yr | Small | Boutique | Independent | MidTier | Major |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 138 | 138 | 4 | 23 | 75 | 28 | 8 |
| 1961 | 177 | 39 | 12 | 29 | 98 | 30 | 8 |
| 1962 | 207 | 30 | 16 | 36 | 117 | 30 | 8 |
| 1963 | 235 | 28 | 20 | 41 | 136 | 30 | 8 |
| 1964 | 264 | 29 | 25 | 43 | 158 | 30 | 8 |
| 1965 | 293 | 29 | 26 | 44 | 185 | 30 | 8 |
| 1966 | 319 | 26 | 27 | 46 | 208 | 30 | 8 |
| 1967 | 348 | 29 | 31 | 46 | 233 | 30 | 8 |
| 1968 | 373 | 25 | 39 | 47 | 249 | 30 | 8 |
| 1969 | **391** | 18 | 44 | 47 | 262 | 30 | 8 |

**391 misses the 400-600 acceptance band by 9.** The 312-week extrapolation of
~409 was close; actual accretion held at 25-29/yr through 1968 and then fell to
18 in 1969.

MidTier is pinned at **30 for the entire decade** and Major at **8**, exactly as
section 15.2 predicts. All growth is Independent (75→262), Small (4→44) and
Boutique (23→47). The Small tail reaches 44 identities, 11.3% of the total, which
is larger than the "very small" tail section 1 asks for.

### 16.2 The late-decade tier mix goes historically wrong

This could not be seen at 312 weeks, because the break is in 1966-67.

| year | Major entries | MidTier | Independent | Major Top-40 |
|---|---:|---:|---:|---:|
| 1965 | 41.3% | 28.8% | 26.9% | 63.5% |
| 1966 | 35.3% | 29.8% | 31.1% | 56.0% |
| 1967 | 22.5% | 30.8% | 41.9% | 36.0% |
| 1968 | 20.8% | 30.5% | 43.9% | 31.9% |
| 1969 | **17.5%** | 28.2% | 49.7% | **25.4%** |

Major entry share falls out of the 35-50% band after 1966 and ends at 17.5%.
Major Top-40 share ends at 25.4% against a period in which majors held **53.8% of
number-one weeks in 1969** (section 13.3). Early-decade calibration is good; the
model inverts in the second half. `c4` ends at 0.174.

### 16.3 Cause: majors stop releasing singles, crowded out by the LP boom

This is a supply collapse, not a competitive one. Total chart entries barely move
(989 → 920), so it is not a denominator effect.

Single releases per year:

| year | Major | MidTier | Independent |
|---|---:|---:|---:|
| 1965 | 479 | 1,064 | 2,568 |
| 1966 | 418 | 1,110 | 2,620 |
| 1967 | **256** | 1,143 | 2,684 |
| 1969 | **216** | 1,251 | 2,867 |

Major single output falls 55% from 1965, tracking the chart-entry collapse
(439 → 161) almost exactly, while every other tier grows. Album projects explain
where the capacity went:

| year | Major albums | MidTier | Independent |
|---|---:|---:|---:|
| 1960 | 56 | 186 | 428 |
| 1965 | 282 | 693 | 1,430 |
| 1969 | 445 | 1,273 | 2,666 |

Majors go from 479 singles + 282 albums in 1965 to 216 singles + 445 albums in
1969. Every tier shifts to albums, but only the Major tier *loses* singles output,
because only the Major tier is capacity-bound at a small firm count.

**This is the section 12 calibration rule violated again.** Section 13 cut the
large-firm population from 111 to 42 — a 62% reduction in aggregate large-firm
capacity — without restating per-firm capacity to compensate. `releasesPerMonth`
is Major 2-4 against Independent 0.5-1.5, so one Major is only ~3x one
Independent; 8 Majors are ~24 Independent-equivalents against 209 seeded
Independents plus runtime founders. That is survivable while singles dominate and
Major records enjoy a large per-record quality and reach advantage, but once the
LP boom consumes their fixed capacity after 1966 the Major singles presence
collapses.

The firm *count* of roughly eight corporate majors is historically right. The
error is treating firm count and capacity as the same knob: a real 1960s major
had many times the release capacity of one independent, not three.

### 16.4 Revised judgment on the section 14.1 remedy

Section 15.3 rejected raising the MidTier draw because it buys only ~4 identities.
That arithmetic stands for **breadth**. But section 16.3 is a separate and stronger
argument about **chart mix**, and it points at per-firm capacity rather than firm
count. The two should not be conflated:

- to fix **breadth** (391 → 400+), the levers are Independent (57.4%) and Boutique
  (40.4%) conversion, per section 15.3;
- to fix the **late-decade mix**, raise per-Major and per-MidTier release capacity
  so the LP transition does not evacuate the singles chart — and restate it to
  preserve the pre-section-13 aggregate, per the section 12 rule.

Ship those separately, or the result is unattributable.

## 17. Breakout diagnosis from the instrumented prefix run

`d7-tier-population-diag-156-1001` was run at seed 1001 **without** `--lean-probe`.
Because `leanProbe` has only three call sites and all are telemetry writes
(`SimTools/ChartAuditRunner.cs:1787,1827`), it is an exact prefix of the decade
run. Verified: 1960-62 `cumulativeFirmsCharting`, `chartEntries` and
`totalChartUnits` match `d7-tier-population-522-1001` to the unit.

That lets the decade-wide charting outcome be joined to per-record/per-region
breakout inputs, so labels that chart *later* are not misread as failures.
1,272,537 breakout rows were attributed across 666 labels.

### 17.1 Peak breakout inputs, eventually-charted vs never-charted

| input | weight | Indep charted | Indep never | ratio |
|---|---:|---:|---:|---:|
| volume | 0.34 | 0.978 | 0.612 | 1.60x |
| velocity | 0.15 | 0.871 | 0.602 | 1.45x |
| audience | 0.12 | 0.509 | 0.366 | 1.39x |
| media | 0.10 | 0.165 | 0.136 | 1.21x |
| sustained | 0.09 | — | — | — |
| genreFit | 0.08 | 0.919 | 0.762 | 1.21x |
| quality | 0.08 | 0.787 | 0.644 | 1.22x |
| unmetDemand | 0.04 | 0.593 | 0.202 | 2.94x |
| — maxScore | | 0.543 | 0.263 | 2.06x |
| — coverRate | | 0.744 | 0.508 | 1.47x |
| — capRate | | 0.000 | 0.000 | — |

The same ordering holds for Boutique (49 charted / 48 never) and Small (26 / 161).

### 17.2 Readings

1. **Failing labels are not making worse records.** quality differs by only 1.22x
   and genreFit by 1.21x, and both carry a weight of just 0.08. The product side
   is close to parity; the gap is demand- and distribution-side.
2. **The binding input is volume, and it counts twice.** `evidence` weights
   `volumeInput` at 0.34 and then multiplies the whole score by
   `0.55 + volumeInput * 0.45` (`Systems/ChartManager.cs:1524-1527`). This is the
   same double-counting section 10.1 identified; region scaling fixed the
   *threshold*, not the double weighting.
3. **`unmetDemand`'s 2.94x is partly an artefact.** It is defined as
   `volumeInput * clamp(backordered / max(750*scale, rawDemand))`
   (`Systems/ChartManager.cs:1521`), so it re-encodes volume rather than
   contributing independent evidence, and it carries the smallest weight (0.04).
4. **Capacity never binds.** `capacityCapped` is 0.000 for every group, so the
   restock/max-capacity path is not a constraint anywhere and can be excluded from
   further investigation.
5. **Coverage is the upstream gate.** Never-charting Independents have
   distribution coverage in 50.8% of their record-region-weeks against 74.4% for
   those that chart. Evidence requires volume, volume requires stock in region,
   in-region stock requires coverage — and coverage is what a distribution deal
   grants, which itself requires evidence. That circularity is the remaining seam,
   and it is consistent with the flat ~75-83% `NoEvidence` rate across the decade.
6. **`mediaInput` is close to inert.** It carries a 0.10 weight but peaks at 0.165
   even for labels that chart, contributing under 0.02 of evidence, because source
   region `radioPlay` is capped at 0.45 (`Systems/ChartManager.cs:1608`) and sits
   near 0.2 in practice. For a decade in which regional radio airplay was the
   breakout mechanism, a 10% weight delivering ~1.7% is worth revisiting — but it
   is an authored ceiling, not a demonstrated defect, so it needs a probe before
   any change.

### 17.3 Candidate levers, in order of evidence

Not yet implemented. Section 12's rule applies to all of them: restate constants
to preserve prior effective values and ship mechanism separately from calibration.

1. **Break the coverage/evidence circularity** — the largest measured gap that is
   not a product-quality gap. Let a record accumulate breakout evidence in the
   label's home/strong region without requiring national coverage first.
2. **Reduce the double weighting of volume**, moving some weight to velocity,
   audience or sustained growth, so a smaller-market record with genuine traction
   is not scored almost entirely on absolute units.
3. **Reconsider the media ceiling**, with a probe.
4. **Per-Major and per-MidTier release capacity**, per section 16.4 — this
   addresses the late-decade mix, not breadth.

## 18. Two measured repairs, and a correction to section 17

### 18.1 Correction: coverage is mostly an effect, not the cause

Section 17.2 item 5 named the coverage/evidence circularity as the remaining seam.
**That was wrong and is retracted.** `GetDistributionRegions` always seeds the
label's home region (`Systems/AILabelFactory.cs:376`), so no label is ever locked
out of generating evidence at home.

Seeded Independents draw home plus 1-3 regions, i.e. an expected coverRate of
0.29-0.57. Measured in the diagnostic prefix run, split by whether the label ever
signed a deal:

| group | coverRate (all weeks) | coverRate (record age <= 8wk) | mean peak breakout |
|---|---:|---:|---:|
| Independent, ever signed | 0.747 | 0.745 | 0.523 |
| Independent, never signed | 0.429 | 0.421 | 0.225 |
| Boutique, ever signed | 0.705 | 0.701 | 0.493 |
| Boutique, never signed | 0.345 | 0.332 | 0.172 |

The never-signed group sits at **0.429, exactly the seeded expectation**. The
signed group's 0.747 is ~5.2 of 7 regions, which the seeded draw cannot produce
(its maximum is 4). That extra coverage therefore comes from deals. Coverage is
downstream of success.

The real gate is the one thing both groups differ on beforehand: **peak breakout
score 0.225 against the 0.30 pull threshold.**

### 18.2 `capacityCapped` is inert, and activating it would be wrong

Measured across 1,272,537 record-region-weeks:

| | covered | uncovered |
|---|---:|---:|
| mean weekStartStock | 7,126.6 | 579.7 |
| mean maxCapacity | 131,342 | 18,833 |
| capacityCapped | 0.00% | 0.00% |
| weeks with backorders | 1.88% | 31.05% |
| mean volumeInput | 0.3319 | 0.1679 |

`maxCapacity` runs 18-32x the stock actually held, because
`recordStoreCount * 100 + departmentStoreCount * 200` is a whole-region shelf
figure applied per record. It is a latent modeling error with **zero current
effect**: the service-level multiplier does all the physical work. Recalibrating
it so it binds would only remove stock from small labels, which is the opposite of
the goal, so the correct treatment is to leave behavior unchanged and record the
inertness here.

### 18.3 Repair A: breakout evidence no longer cancels constrained demand

Two defects in `UpdateRegionalBreakoutState`:

- `unmetInput` was multiplied by `volumeInput`, counting volume a third time and
  cancelling the signal for exactly the labels it describes — a record selling out
  where its label cannot restock has low fulfilled volume by construction. Uncovered
  regions carry backorders in 31.05% of weeks against 1.88% covered.
- the `0.55 + 0.45 * volumeInput` envelope multiplied the whole score by volume a
  second time, on top of its 0.34 weight. This is the section 10.1 double count;
  section 10.3 fixed the thresholds feeding volume but not the envelope.

Now: `unmetInput` is independent at weight 0.08, `volumeInput` weight 0.30, and the
envelope is `0.70 + 0.30 * volumeInput`. Weights remain a partition of unity.
Extracted as `ChartManager.CalculateBreakoutEvidence`; probe 82 covers the
constrained-demand credit, incumbent neutrality, continued volume dominance, and
the partition.

### 18.4 Repair B: artist project history is counted once

`RosterManager.RecordReleased` already appends to `releaseHistory`, and both live
release paths in `CompetitorManager` appended again. `GenreSupplyService` caps
project history at three (`Systems/GenreSupplyService.cs:211-212`), so an artist
reached the cap after two releases instead of three and carried up to 0.06 of
unearned project-identity retention. The prewarm path was already correct — it
never calls `RecordReleased`.

Both sites now use `CompetitorManager.RecordArtistRelease`, which falls back to the
manual bookkeeping when the `RosterManager` singleton is absent — the reason the
redundant append existed. Probe 83 covers it.

### 18.5 Measured, separately attributable

Each repair measured at 156 weeks, seed 1001, against the run before it:

| configuration | 1960 | 1961 | 1962 | Major entries | Major Top-40 |
|---|---:|---:|---:|---:|---:|
| baseline (`diag-156`) | 138 | 177 | 207 | 40.6% | 67.2% |
| + repair A | 142 | 182 | 220 | 41.4% | 67.1% |
| + repair B | 142 | 196 | **232** | 39.8% | 66.6% |

**+25 cumulative identities at 1962, +12.1%**, with the tier mix stable — Major
entry share moves 40.6% → 39.8% and Major Top-40 67.2% → 66.6%, so neither repair
is an incumbent subsidy. The section 12 trap is avoided.

Repair A's mechanism is confirmed end to end in the offer funnel: `NoEvidence`
81.0% → 75.3%, qualifying persistent evidence +21.9%, signed deals +24.7%.

All 83 fixed probes pass; `dotnet build` clean.

### 18.6 Decade confirmation: 412, inside the target band

`d7-evidence-repairs-522-1001`, seed 1001, both repairs, all 522 weeks, exit 0.

| year | baseline | repaired | delta |
|---|---:|---:|---:|
| 1960 | 138 | 142 | +4 |
| 1961 | 177 | 196 | +19 |
| 1962 | 207 | 232 | +25 |
| 1963 | 235 | 256 | +21 |
| 1964 | 264 | 274 | +10 |
| 1965 | 293 | 298 | +5 |
| 1966 | 319 | 318 | -1 |
| 1967 | 348 | 357 | +9 |
| 1968 | 373 | 388 | +15 |
| 1969 | **391** | **412** | **+21** |

**412 clears the 400-600 acceptance band.** Note the delta is not monotonic — it
peaks at +25 in 1962, decays to -1 by 1966, then recovers to +21. The repairs both
pull identities forward and add new ones, and a mid-decade reading alone would have
been misleading in either direction. Final mix: Small 40, Boutique 48, Independent
284, MidTier 32, Major 8. The Small tail is 9.7%, down from 11.3%.

MidTier reaches 32 against 30, the first movement in that bucket across this whole
pass; it remains capped by the structural limit in section 15.2.

### 18.7 What remains

The section 16 late-decade Major collapse is **not addressed by either repair**, and
the decade run confirms it is untouched:

| year | baseline Major entries | repaired | baseline Major Top-40 | repaired |
|---|---:|---:|---:|---:|
| 1965 | 41.3% | 40.5% | 63.5% | 64.3% |
| 1967 | 22.5% | 22.2% | 36.0% | 36.7% |
| 1969 | 17.5% | **17.9%** | 25.4% | **25.3%** |

Major entry share still falls out of the 35-50% band after 1966 and ends near 18%,
against a period in which majors held 53.8% of number-one weeks in 1969.

Its mechanism is now pinned: `CalculateLabelReleaseCapacityChance` gates on
`availabilityMod = clamp(availableArtists / 3)`
(`Systems/CompetitorManager.cs:730-741`), and album projects consume the artists
that would otherwise be eligible for singles. With 8 Majors on rosters frozen near
25, the LP boom evacuates their singles output. The lever is Major and MidTier
**roster capacity**, not `releasesPerMonth`.

This is the one remaining acceptance failure. It cannot be measured below ~1966, so
any attempt at it needs a decade run to validate, and per section 12 it must ship
separately from the breadth repairs above.

Also still open, unchanged by this pass: the section 6.2 duplicate display-name
inflation (412 imprint IDs correspond to 368 exact names), the section 6.3
chronology of named templates, and the section 6.7 prewarm age/inventory
inconsistency.

## 19. The Major collapse: mechanism corrected, first repair attempt reverted

### 19.1 Two wrong mechanisms, then the real one

Section 16.3 and 18.7 attributed the collapse to Major roster capacity via
`availabilityMod = clamp(availableArtists / 3)`. **That is wrong and is retracted.**
Measured in `d7-evidence-repairs-522-1001`:

- Majors are not roster-starved. `releaseEligibleCount` averages 22-33 against the
  3 needed to saturate `availabilityMod`, and rosters grow 32.4 (1960) to 43.4
  (1968) against a hard cap near 52. That gate is pinned at 1.0 throughout.
- Majors are not status-degraded. Every Major label-week is `Rising`
  (`statusMod` 1.2); zero weeks Struggling, Dying, or Bankrupt.

The real mechanism, from `calibration-decisions.csv` joined to seeded tier:

| year | Major album share | MidTier | Independent | Major decisions |
|---|---:|---:|---:|---:|
| 1960 | 23% | 24% | 19% | 360 |
| 1965 | 64% | 67% | 66% | 398 |
| 1969 | 90% | 91% | 92% | 408 |

Two facts combine:

1. **Every label gets exactly one Bernoulli release trial per week**
   (`ProcessWeeklyReleases`), so output is capped at one release per label-week and
   the `Mathf.Clamp(..., 0f, 1f)` in `CalculateLabelReleaseCapacityChance` silently
   discards any `releasesPerMonth` above four. Major decisions sit flat at ~360-408
   per year for 8 firms — about one per firm per week, exactly the cap.
2. **Album share rises to ~90% in every tier alike**, and album and single compete
   for that one slot.

Market-wide singles output is therefore flat (4,570 in 1960 to 5,072 in 1969)
because new Independent firms keep being founded and add slots. **Majors are the
only tier whose firm count is fixed at 8**, so nothing offsets their conversion and
their singles output halves. The collapse is not Major-specific behaviour; it is
the only tier where the fixed firm count makes the universal conversion visible.

### 19.2 Attempt and revert

Tried: uncap the weekly opportunity so capacity above one release per week yields
multiple attempts (bounded by a roster ceiling of one release per three artists),
plus restate Major `releasesPerMonth` 2-4 to 5-9 and MidTier 1-2.5 to 1.5-3.5 as
the section 12 restatement for section 13's 62% cut in large-firm count.

All 84 probes passed. The decade run overshot badly and was stopped at 1965:

| metric | verified (412 config) | attempt | target |
|---|---:|---:|---:|
| 1965 cumulative identities | 298 | **203** | — |
| 1960 Major entry share | 41.4% | **70.9%** | 35-50% |
| 1960 Major Top-40 share | 71.2% | **93.5%** | ~70% |
| 1965 indie family chart share | 0.199 | **0.074** | — |
| 1965 c4 | 0.355 | **0.597** | — |

Raising mean Major capacity ~2.3x flooded the chart from 1960 onward and destroyed
95 cumulative identities by 1965. **Reverted in full**; the tree reproduces the
verified configuration byte-identically (`d7-revert-verify-52-1001` matches
`d7-release-history-probes-52-1001` on 1960 cumulative, entries, units, Major
entries and Major Top-40), and probes are back to 1-83.

### 19.3 Why it failed, and what the next attempt must do differently

A flat capacity multiplier is the wrong shape. The Major deficit **only exists after
1966** — 1960-65 Major entry share was already correctly calibrated at ~41%, inside
the band. Scaling capacity uniformly across the decade inflates precisely the years
that were already right, and the early-decade damage dwarfs the late-decade gain.

Two additional traps found the hard way:

- `GetStatusReleaseModifier` at 1.2 for `Rising` means any uncapping immediately
  grants every Major a 20% boost even at unchanged `releasesPerMonth`, because the
  old code clamped that 1.2 back to 1.0. An uncapping change is therefore **not**
  behaviour-neutral at existing constants.
- Probe-enabled runs consume RNG draws, so a 52-week probe run cannot be compared
  against a non-probe run to verify a revert. Compare probe-run to probe-run.

The next attempt should make the relief **time- or album-share-dependent** rather
than flat: preserve the existing singles throughput while the album pipeline grows,
so albums add rather than displace, leaving 1960-65 untouched by construction. Size
it against the 1969 gap only, and validate on a decade run — a short run cannot see
this at all.

## 20. The Major collapse is the promo Single being abandoned, not capacity

### 20.1 Correction: capacity is not the channel, and section 19.1 is retracted

Section 19.1 attributed the collapse to the one-Bernoulli-trial-per-week structure:
albums and singles competing for a single weekly slot while album share rises to
~90%. **That is wrong.** Measured over all 48,155 format decisions of
`d7-evidence-repairs-522-1001` (`release-strategy.csv`, `tier` × `year`):

| year | Major decisions | album share | AlbumStandalone | Singles emitted |
|---|---:|---:|---:|---:|
| 1960 | 360 | 22.5% | 0 | 360 |
| 1963 | 477 | 39.2% | 0 | 477 |
| 1965 | 475 | 63.4% | 7 | 468 |
| 1967 | 472 | 89.8% | 219 | 253 |
| 1969 | 495 | 90.3% | 274 | 221 |

Major decisions are **flat at 360-495 for the whole decade**, so no capacity is
being lost. Album share rises to ~90% in every tier alike, so the LP conversion is
not Major-specific either. What is Major-specific is the *strategy*:

| year | AlbumStandalone share of album decisions | Major | MidTier | Independent |
|---|---|---:|---:|---:|
| 1965 | | 0.2% | 0.0% | 0.0% |
| 1967 | | 5.0% | 3.6% | 0.3% |
| 1969 | | 54.5% | 6.6% | 1.6% |

An `AlbumWithPromo` project emits an album **and** a promo single; an
`AlbumStandalone` emits only the album. Majors abandoned the promo single for 274 of
495 decisions in 1969 against 0 of 360 in 1960. Major singles fall 468 → 221 across
1965-69, a loss of 247 against 274 standalone projects. **The abandonment of the
promo single is the entire late-decade Major singles collapse**, and the section 16.3
"LP boom consumes fixed capacity" reading is retracted with it.

This also explains why the section 19.2 capacity attempt failed so badly: it acted on
a channel that was never constrained, from 1960 onward, in a year range that was
already calibrated.

### 20.2 Cause: the promo Single's diversion is charged twice

`promoPreferred` compares `projectedAlbum + promoAdvantage` against
`projectedStandaloneAlbum`, where

```text
promoAdvantage = expectedPromoLift + promoSynergyGain + expectedPromoSingleNet
                 - cannibalizationLoss
```

`projectedAlbum` is the **Album-component projection**, already moved off its prior by
the `AlbumComponent` lane residual at weight `confidenceAlbum`
(`Systems/CompetitorManager.cs:1533-1535`). That lane observes realized albums that
were themselves released alongside a promo single
(`Systems/CompetitorManager.cs:828,831`), so whatever diversion the promo actually
caused is already inside the projection being adjusted. Subtracting the full modelled
`cannibalizationLoss` on top charges it a second time, at exactly weight
`confidenceAlbum`.

The duplicate share scales with album unit economics — `cannibalizationLoss` is
`substitutionPropensity × expectedOverlapFraction × expectedSingleUnits ×
albumMarginPerUnit` — while the terms opposing it do not: `expectedPromoLift` is a
fixed 10,000 scalar on awareness headroom, and `expectedPromoSingleNet` is one
single's net. So the strategy is guaranteed to decay to non-viable as the LP market
matures, and it decays **first for whoever carries the largest
`expectedSingleUnits`** — the Majors, at roughly 2× MidTier's.

Measured, mean over album decisions, with the last column being `promoAdvantage`
recomputed with the duplicated share removed:

| year | confidenceAlbum | cannibalizationLoss | promoAdvantage | charged once |
|---|---:|---:|---:|---:|
| Major 1960 | 0.054 | 23,794 | 46,070 | 47,344 |
| Major 1963 | 0.407 | 19,762 | 48,990 | 57,033 |
| Major 1965 | 0.472 | 31,095 | 45,279 | 59,956 |
| Major 1967 | 0.569 | 83,610 | 13,880 | 61,448 |
| Major 1969 | 0.510 | 112,959 | **-2,378** | **55,267** |

Charged twice, the Major promo proposition decays to negative by 1969. Charged once,
it is **flat across the whole decade** at 47,344-61,448 — which is what "preserve
singles throughput as the album pipeline grows" means mechanically. MidTier
(20,470 → 36,467) and Independent (17,523 → 31,091) are corrected in the same
direction but were never close to the sign change.

There is a second, weaker structural problem left in place deliberately:
`PromoAlbumConversionK` (0.50) against `substitutionK × expectedOverlapFraction`
(1.00 × 0.60) makes the promo single's album-unit effect **negative-definite** —
recruitment is at most `0.50·D` and as little as `0.125·D` against diversion `0.60·D`,
so no label at any awareness in any year can find the promo's audience effect
favourable. The comment at `Systems/CompetitorManager.cs:1791-1797` claims recruitment
is "on the same terms as diversion"; it is 21-83% of it. **Not changed in this pass**
— it is a constant restatement, and section 12 requires it ship separately from the
mechanism repair above.

### 20.3 Repair: charge only the share the component projection has not absorbed

```csharp
internal static float CalculateChargedPromoCannibalization(
    float cannibalizationLoss, float albumMemoryConfidence) =>
    Mathf.Max(0f, cannibalizationLoss) * (1f - Mathf.Clamp(albumMemoryConfidence, 0f, 1f));
```

Applied on **both** routes, not gated to live, for the same reason the promo synergy
gain was not gated: it corrects a shared accounting error, not a defect in the live
lane split.

The relief is confidence-weighted, so it is **time-dependent by construction** rather
than by fitting: a label with no album evidence still carries the whole modelled
diversion, and the relief arrives only as the lane accumulates the observations that
already price it in. `release-strategy.csv` gained a `cannibalizationCharged` column
beside the unchanged gross `cannibalizationLoss` so the two accountings stay
separable in later runs.

Probe 84 covers the no-evidence and full-confidence endpoints, monotonicity, clamping
of out-of-range confidence and negative loss, and the measured 1969 Major inputs
flipping from non-viable to viable.

### 20.4 1960 is byte-identical, which is the section 19.3 constraint met exactly

`d7-promo-cannibalization-probes-52-1001` versus the verified reference probe run
`d7-release-history-probes-52-1001` — probe-to-probe, per the section 19.3 trap:

- `concentration.csv` is **byte-identical** (SHA-256 `024AFAB7…9DCD3D`);
- same 4,692 format decisions, same 1,069 album decisions, **0 standalone in both**;
- mean modelled cannibalization unchanged at 6,181, of which 5,911 is still charged —
  `confidenceAlbum` is near zero in 1960, so 4.4% is absorbed;
- `promoAdvantage` moves 19,179 → 19,449, and since promo already won 100% of 1960
  album decisions, no outcome moves at all.

All 84 fixed probes pass; `dotnet build` clean apart from the pre-existing unused-event
warning; `git diff --check` clean.

### 20.5 Sizing, stated before the run so it can be falsified

Major chart entries track Major singles closely (1965: 439 entries on 468 singles;
1969: 161 on 221). Restoring the promo single to ~all Major album decisions returns
1969 Major singles to roughly 495, near their 1965 level of 468. Total chart entries
are roughly fixed by turnover (989 → 920 across the decade), so Major entries should
land near **33-36%** against the 35-50% band — at the band edge, not comfortably
inside it — with Major Top-40 share rising from 25.3% toward the 45-60% target.

Aggregate singles supply should rise only from ~5,096 to ~5,507 (+8%), unlike the
section 19.2 attempt's ~2.3× capacity multiplier. Breadth is expected to be roughly
neutral: this repair moves a strategy split inside the album lane and adds no founders
and no release opportunities.

`d7-promo-cannibalization-522-1001` is running at seed 1001 on the same flags as
`d7-evidence-repairs-522-1001`. 1960-63 must reproduce it exactly (the first standalone
decisions are in 1964); divergence after that is a different random realization, per
section 14.2.

## 21. Decade result: the mix objective is met, breadth is unresolved

`d7-promo-cannibalization-522-1001` completed all 522 weeks, exit code 0, no band
violations. 1960-63 reproduce `d7-evidence-repairs-522-1001` **exactly**, row for row,
as section 20.5 required; divergence begins in 1964 with the first strategy flip.

### 21.1 The mechanism fired, and it is the only thing that moved

Total format decisions 48,155 → 47,863 — no supply flood, unlike section 19.2's 2.3×
capacity multiplier. `AlbumStandalone` projects, by tier, 1969:

| tier | baseline | candidate | Singles emitted, baseline → candidate |
|---|---:|---:|---|
| Major | 274 | **119** | 221 → 355 |
| MidTier | 92 | 44 | 1,425 → 1,433 |
| Independent | 45 | 3 | 3,014 → 2,980 |

Mean Major 1969 cannibalization is unchanged as modelled (96,417) with 44,164 still
charged — 54% absorbed, matching `confidenceAlbum` ≈ 0.51. The repair is doing exactly
and only what section 20.3 describes.

### 21.2 Chart mix: the section 18.7 acceptance failure is fixed

| year | Major entries base → cand | Major Top-40 base → cand | MidTier entries | indieFamilyChartShare |
|---|---|---|---|---|
| 1965 | 40.5% → 41.2% | 64.3% → 65.2% | 29.8% → 32.0% | 0.211 → 0.212 |
| 1966 | 34.8% → 40.2% | 56.2% → 59.8% | 32.7% → 28.1% | 0.253 → 0.271 |
| 1967 | 22.2% → **35.4%** | 36.7% → **55.8%** | 32.3% → 29.4% | 0.444 → 0.305 |
| 1968 | 18.8% → 31.6% | 31.6% → 49.1% | 31.7% → 26.5% | 0.534 → 0.367 |
| 1969 | 17.9% → **30.0%** | 25.3% → **47.2%** | 30.8% → 26.7% | 0.531 → 0.426 |

- **Major Top-40 share reaches 47.2% at 1969, inside the 45-60% acceptance band**,
  against 25.3% before and against majors holding 53.8% of number-one weeks in 1969
  (section 13.3). This was the sharpest remaining acceptance failure and it is met.
- **Major entry share stops collapsing**: 41.4% (1960) → 41.2% (1965) → 30.0% (1969), a
  gentle decline of the right shape, against a baseline that fell to 17.9%. 1967 at
  35.4% is inside the 35-50% band; 1968-69 sit below it.
- MidTier entry share stays well under its former 44.5%, at 26.7%.
- Guardrails hold: Small tail 9.4% of identities, below-MidTier population 76.6%
  Independent.

Section 20.5 predicted 33-36% Major entries. **The measured 30.0% is below that** — the
repair recovered 155 of the 274 abandoned Major promo singles, not all of them, because
the remaining 119 fail the comparison on the residual `promoAdvantage` shortfall rather
than the double count. The section 20.2 `PromoAlbumConversionK` restatement is the lever
for the rest, and it remains deliberately unshipped.

### 21.3 Breadth: 394, and the -18 is not attributable

| year | 1964 | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|
| baseline | 274 | 298 | 318 | 357 | 388 | **412** |
| candidate | 273 | 301 | 323 | 343 | 367 | **394** |

394 misses the 400-600 band by 6. Section 20.5 predicted roughly neutral breadth; that
was wrong by 18.

**But it is not a measured cost of the repair.** Joining `first-chart-events.csv` on
`releaseLabelId`, the two runs share only 303 identities: **109 chart in the baseline
only and 92 in the candidate only.** The charting population is 26% volatile between the
two realizations, and the net decomposes by year as:

| year | 1964 | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---:|---:|---:|---:|---:|---:|
| baseline-only | 2 | 10 | 15 | 33 | 26 | 23 |
| candidate-only | 5 | 10 | 14 | 16 | 22 | 24 |
| net | +3 | 0 | -1 | **-17** | -4 | +1 |

**The entire net loss is 1967.** If the mechanism were Major entries crowding marginal
independents off a fixed chart, the loss would grow with Major entry share — which peaks
in 1969, where the net is **+1**. All churn is inside Independent/Small/Boutique (89/17/3
out, 76/15/0 in); MidTier and Major are frozen seeded sets and do not move at all.

The change alters `GD.RandRange` draw counts from 1964 onward (an `AlbumWithPromo`
project draws a gap week and a second perceived-quality multiplier that an
`AlbumStandalone` does not), so per section 14.2 these are different random realizations
and a difference of ten to twenty identities must not be read as causal. That rule was
written for exactly this situation and it applies here.

### 21.4 Required next step

**A holdout seed, which section 15.6 has required before acceptance since the 412 run.**
Breadth cannot be resolved at seed 1001: the candidate and the baseline differ by less
than the seed-to-seed churn already measured within a single seed. Running both
configurations at one holdout seed separates a 6-identity band miss from a 4% realization
swing, and it is the evidence the acceptance claim needs regardless of which way it lands.

Do **not** reach for a breadth lever before that. The section 15.3 arithmetic still holds
— seeded Independent (57.4%) and Boutique (40.4%) conversion are the only buckets with
enough mass — and section 1 still forbids buying the target with more label births.

### 21.5 Holdout run in flight, and what it can and cannot settle

`d7-promo-cannibalization-522-2029` was launched at **seed 2029** — unused; prior runs in
`SimLogs/` cover only 1001, 1002 and 2007 — on the candidate configuration, same flags as
above. **The user chose the candidate-only variant** of the section 21.4 step over running
both configurations at the holdout seed.

State this plainly when reading the result:

- It **can** answer whether the candidate reaches 400 on a second realization. If 2029
  lands comfortably inside 400-600, the seed-1001 miss by 6 is realization noise and the
  configuration is acceptable on breadth. If it lands near or below 394 again, the
  shortfall is more likely real.
- It **cannot** isolate whether the repair costs breadth, because there is no baseline run
  at 2029 to compare against. A cross-seed comparison against `d7-evidence-repairs-522-1001`
  is not an A/B — the whole point of section 14.2.
- The mix result does **not** need this run. Major Top-40 share moving 25.3% → 47.2% is far
  outside the churn measured in section 21.3, and the strategy-split mechanism in section
  21.1 is directly observed rather than inferred.

If the holdout also misses 400, the ordered candidates are: the section 20.2
`PromoAlbumConversionK` restatement (helps mix, expected to cost breadth if anything);
then seeded Independent and Boutique conversion per section 15.3. Neither should be
attempted in the same run as the other, per section 12.

### 21.6 Holdout result: 411, and the mix reproduces

`d7-promo-cannibalization-522-2029` completed all 522 weeks, exit code 0, no band
violations.

| year | 1960 | 1963 | 1965 | 1967 | 1969 |
|---|---:|---:|---:|---:|---:|
| cumulative identities | 157 | 264 | 328 | 374 | **411** |

**411 is inside the 400-600 band.** Read against seed 1001's 394, the section 21.3
reading holds: the 6-identity miss at 1001 was realization noise, and the two seeds
bracket the band edge rather than clearing it comfortably. Final mix Small 43, Boutique
46, Independent 292, MidTier 21, Major 9; Small tail 10.5%, below-MidTier population
76.6% Independent.

The repaired quantities reproduce across two independent realizations, which is the
part that matters:

| 1969 metric | seed 1001 | seed 2029 | baseline (1001) | target |
|---|---:|---:|---:|---|
| cumulative identities | 394 | **411** | 412 | 400-600 |
| Major chart entries | 30.0% | 30.5% | 17.9% | 35-50% |
| Major Top-40 entries | **47.2%** | **48.7%** | 25.3% | 45-60% |
| MidTier chart entries | 26.7% | 20.8% | 30.8% | « 44.5% |
| Major `AlbumStandalone` | 119 | 91 | 274 | — |

Against the section 15.6 acceptance criteria, **four of five are met on the holdout**:
1969 breadth in band, Major Top-40 in band, MidTier entry share far below its former
44.5%, and a small Independent-dominated tail below MidTier.

**The one reproducible failure is Major entry share, ending near 30% on both seeds
against the 35-50% band.** Both seeds agreeing to within 0.5 points makes this a real
shortfall, not noise — and it is exactly the residue section 21.2 predicted, the 91-119
Major album projects per year that still abandon the promo single on the
`promoAdvantage` shortfall rather than the double count. The section 20.2
`PromoAlbumConversionK` restatement (0.50 against `substitutionK × expectedOverlapFraction`
= 0.60, which makes the promo single's album-unit effect negative-definite at any
awareness in any year) is the lever for it, and per section 12 it must ship as its own
calibration change with its own decade run.

Caveat carried forward: 2029 is a holdout for the *candidate* only. No baseline run
exists at that seed, so nothing here isolates whether the repair costs breadth; it
establishes only that the candidate reaches the band on a second realization.

## 22. Breadth and late-decade Major share: both fixed and accepted across two seeds

Per user direction, this pass took the two remaining acceptance gaps together — breadth
sitting on the band edge and Major entry share collapsing after 1966 — diagnosed each
from the candidate run's own telemetry, shipped a fix for each, and validated both on a
decade run at seed 1001 and a holdout at seed 2029. The user chose to run both fixes in
one decade run rather than two, accepting the reduced per-fix attribution.

### 22.1 Breadth: the 0.18-0.24 breakout limbo band

The candidate's `distribution-offer-attempts.csv` shows the below-MidTier sign rate is not
a soft ramp but a **cliff at 0.18 breakout score**, with a second inflection at 0.24:

| best strong-region peak | Independent labels | sign rate |
|---|---:|---:|
| < 0.10 | 63 | 0% |
| 0.10-0.18 | 81 | 1% |
| 0.18-0.24 | 99 | 36% |
| 0.24-0.30 | 135 | 84% |
| 0.30-0.40 | 136 | 89% |
| 0.40+ | 194 | 94% |

`ChartManager.UpdateRegionalBreakoutState` explains it exactly. Three zones exist around
the breakout score: **below 0.18** a record collapses (`collapseWeeks` accrues, stage
decays to None); **at or above 0.24** it enters `LocalTraction` and `ApplyBreakoutDiscovery`
feeds it self-reinforcing awareness and radio that raise future evidence; the **0.18-0.24
band is limbo** — not collapsing, but `breakoutStage < LocalTraction` so discovery skips it
(`ChartManager.cs` line ~1631), and it never climbs to the 0.30-plus region where deals and
charts happen. Roughly 190 Independent/Boutique/Small labels were stranded there. This is
where the decade's charting breadth was being lost, and it is exactly the seeded-Independent
(57.4%) and Boutique (40.4%) conversion the section 15.3 arithmetic pointed at.

**Fix:** `LocalTractionActivationScore` (new named constant) lowers the LocalTraction /
traction-accrual / discovery-activation anchor from 0.24 to **0.20**, admitting the upper
part of the limbo band to the discovery ramp while leaving the 0.18 collapse floor intact,
so genuinely dead records still die. The discovery ramp was extracted to
`CalculateBreakoutDiscoveryStrength`. The coupled deal gate
`CompetitorManager.regionalBreakoutDealThreshold` — which the code comment ties to the same
LocalTraction boundary, and which is the actual sign gate producing the 0.24 cliff — moves
to 0.20 with it. It is incumbent-neutral by construction: incumbents sit at the 0.40
RegionalBreakout stage with their discovery gains already capped. Probe 86 pins it.

The 1960 checkpoint confirmed the shape before the decade run: breadth +9 with all growth
in Independent (MidTier and Major frozen), c4 unchanged at 0.48, Major entry still ~40%.

### 22.2 Major share: the promo-recruitment negative-definite defect

Section 20 charged promo cannibalization once, recovering 155 of 274 abandoned Major promo
singles, but 1969 Major entry share still ended at 30.0%. The residual — 119 Major Album
decisions a year still dropping the promo single — traces to `PromoAlbumConversionK`. The
promo single's Album-unit **recruitment** (`CalculatePromoAlbumSynergyGain`) is
`K * albumDemand * awarenessHeadroom`; its **diversion** is `substitutionK * albumDemand *
overlap`. With `K = 0.50` against `substitutionK = 1.00` and overlap `0.60`, recruitment ran
only 0.13-0.50x diversion — net-dilutive at *every* awareness level, so as the LP market
matured the promo proposition decayed to non-viable for the highest-`expectedSingleUnits`
acts first, which are the Majors.

**Fix:** raise `PromoAlbumConversionK` so recruitment is on the same base terms as diversion
(`= substitutionK`), making a hit single for an unknown act a net Album driver while
remaining mildly dilutive for a well-known act (the awareness-gated crossover, preserved
below K = 2.4). Sized from `release-strategy.csv`: K = 1.0 flips the majority of the residual
standalone decisions; the user then directed a tune to **K = 1.5** for margin off the band
floor. K only moves post-1966 decisions — 1960-65 promo already wins every Album decision —
so there is no early-decade risk. Probe 85 pins the crossover; the D5 probe that had encoded
the old "mildly dilutive per unit" invariant was updated. `release-strategy.csv` gained
`cannibalizationCharged` (section 20) and the standalone-decision count is the direct
mechanism read.

### 22.3 Measured, two seeds, all five acceptance criteria met

| metric (1969) | seed 1001 (K=1.0) | seed 2029 (K=1.5) | candidate 1001 | candidate 2029 | target |
|---|---:|---:|---:|---:|---|
| cumulative identities | 424 | 423 | 394 | 411 | 400-600 |
| Major chart entries | 35.3% | 36.4% | 30.0% | 30.5% | 35-50% |
| Major Top-40 entries | 52.1% | 55.4% | 47.2% | 48.7% | 45-60% |
| MidTier chart entries | 22.8% | 17.4% | 26.7% | 20.8% | « 44.5% |
| c4 | 0.286 | 0.284 | 0.227 | 0.236 | not cratered |
| Small tail | 9.7% | 12.1% | 9.4% | 10.5% | small, Ind-dominated |

Breadth is essentially seed-invariant here (~423-424) because it is driven by the
LocalTraction fix, not K, so the two seeds validate it as one config despite the K
difference. Both fixes are incumbent-neutral: MidTier and Major first-chart buckets stay
frozen (section 15.2). The late-decade Major collapse is arrested — Major entry holds inside
the band every year (`42.7 -> 41.4 -> 38.4 -> 36.4` on 2029 across 1966-69) instead of
falling out to 30%. Runs: `d7-breadth-major-decade-522-1001`, `d7-major-tune-holdout-522-2029`;
probes `d7-breadth-major-probes-52-1001`, `d7-major-tune-probes-52-1001`. All 86 D6 probes
plus D5 pass; `dotnet build` clean apart from the pre-existing unused-event warning.

Committed and pushed as `3b4a696` (an auto-generated message from a Codex run; the content is
this breadth + Major-share pass on top of the section 18/20 evidence and promo repairs).

### 22.4 The promo lever has a ~36% ceiling — this is why section 23 exists

The K = 1.5 tune is the decisive measurement. It flipped Major `AlbumStandalone` decisions
from 91 (candidate 2029) to **5** in 1969 — near-total promo restoration, the lever
essentially maxed. Yet Major entry share moved only 35.3% -> 36.4%. **With Majors frozen at
8 seeded firms, even full promo retention caps their chart-entry share near 36%**, because
each restored single also grows the total-chart denominator. Pushing K higher is inert —
there is almost nothing left to flip. `majorFamilyChartShare` tells the same story: 0.78 ->
0.56 across the decade, falling throughout, with no late-decade rise.

So the promo lever cannot deliver the historical late-1960s **consolidation** — majors'
share *rising* into 1968-69 as they absorbed independents. That is a distinct mechanism,
scoped in section 23. Also still open and unchanged: the section 6.2 duplicate display-name
inflation, section 6.3 named-template chronology, and section 6.7 prewarm age/inventory.

## 23. Next work: the consolidation lever (scoping, not yet implemented)

**Goal (user):** Major chart-entry share should *rise* into 1968-69 to a **45-52%**
late-decade consolidation level, reflecting the real late-1960s wave of majors absorbing and
distributing independents (WB-Atlantic 1967, MCA forming from Decca/Kapp/Uni, ABC absorbing
imprints, etc.). Trigger absorptions through the existing distribution-deal mechanic, within
historical bounds; deeper consolidation mechanics can wait for future implementation.

### 23.1 Resolve the metric first — this is the crux

`chartEntries*Major` is keyed to the **immutable release-imprint tier** (section 5.5, 13.5).
Acquisition already mutates `record.baseRecord.labelId` to the operating owner but
deliberately preserves `releaseLabelId`, so an absorbed independent's records still count as
**Independent** entries. Therefore consolidation-by-acquisition **cannot move the
imprint-tier `chartEntriesMajor`** at all; only majors releasing more under their own names
could, and that runs straight back into the section 22.4 firm-count ceiling.

But the imprint-tier metric is also the *wrong* historical target. The section 13.3 source
counts Atlantic's hits as major **only after WB acquired it in 1967** — i.e. industry "major
share" is **major-distributed / owner-family share**, crediting the acquiring firm, not the
imprint on the label. And `majorFamilyChartShare` (units) is already owner-based and already
reads 0.56 at 1969. So:

- **Recommendation:** measure the 45-52% target on a new **major-distributed chart-*entry*
  share** — distinct charting records per year bucketed by the record's *current owner*
  family — not on `chartEntriesMajor` (imprint). This is the metric consolidation actually
  moves, and it is the one the historical numbers are quoted against. It will sit between the
  imprint-entry 36% and the owner-*unit* 56%, so 45-52% is a reachable, well-posed target.
- Keep the imprint-tier entries and the cumulative imprint IDs exactly as they are: breadth
  (cumulative release-imprint identities) is **orthogonal** to consolidation and must not
  fall when majors absorb indies. An absorbed imprint still counts once for breadth forever.
  This orthogonality is the elegant part — consolidation raises owner-share while leaving the
  breadth acceptance untouched.

### 23.2 First, measure what acquisition already does

Before adding anything, the next session must establish the current baseline, because the
lever may already be partly wired:

1. Is acquisition firing at all in the accepted decade runs? Check `deal-ledger.csv` and any
   acquisition/absorption telemetry; count acquisitions by year and acquirer tier. The
   suspicion is few or none, since `majorFamilyChartShare` *falls* 0.78 -> 0.56 rather than
   rising late-decade.
2. Compute the proposed **major-distributed entry share** by year from existing data — join
   annual chart entries to current-owner family (the concentration audit already resolves
   current owner for units; extend it to distinct-record entries). Report 1960-69. This is
   the number the 45-52% target attaches to; know where it stands before moving it.
3. Confirm how an acquired imprint's *ongoing* and *post-acquisition new* chart records are
   attributed today (owner for units, imprint for entries) so the change is a deliberate
   redefinition, not an accident.

### 23.3 Implementation sketch (distribution-deal-triggered absorption)

1. **Trigger off the existing deal relationship.** A `DistributionDeal` from a Major (or
   national MidTier) distributor to a successful independent is the historical on-ramp to
   absorption. After a deal of sufficient duration on an independent that has *charted*
   (proven winner — majors bought success, not failure), roll a low-probability acquisition
   that transfers the independent's roster/catalog ownership to the distributor, reusing the
   existing `record.baseRecord.labelId` owner-mutation path and preserving `releaseLabelId`.
2. **Gate to the historical window and cap the count.** Enable absorptions only from ~1966,
   and bound the number to a handful across 1966-69 (order of the real wave — a few majors
   absorbing a few dozen imprints, not wholesale). Over-consolidation would crush the indie
   imprint tail that breadth and the section 1 guardrail require.
3. **Attribution:** the absorbed imprint's records reattribute to the major *family* for the
   new owner-based entry/unit share, while their **imprint identity still counts for breadth**.
   Decide whether post-absorption *new* releases chart under the imprint name (historically
   yes — Atlantic kept charting as Atlantic) but under major-family ownership.
4. **Calibrate against the target.** Size the absorption rate so major-distributed entry
   share rises from its mid-decade level toward **45-52% by 1969**, a gentle late-decade
   *rise* — the shape section 22.4 could not produce. Cross-check it stays below the ~54%
   #1-week share (section 13.3), discounted for entry level.

### 23.4 Discipline and validation

Per section 12, ship this as its own change with its own decade run — consolidation is a
post-1966 effect, invisible on any short run, so a 52-week probe validates only mechanics and
a full decade is required to see the trend. Restate any constant whose sampler you touch.
Guard rails to watch on the decade run: cumulative breadth must **not** fall (imprint IDs are
preserved), the Small tail must stay small and Independent-dominated, and the major-distributed
entry share must *rise* into 1968-69 rather than merely flattening. New telemetry required: an
acquisitions ledger (acquirer, target, week, target's charting history) and the owner-family
entry/unit share alongside the retained imprint-tier entry share.

### 23.5 Open questions to settle before coding

- Which exact metric carries the 45-52% target — confirm major-distributed **entry** share
  (recommended) versus unit share versus a redefined imprint attribution.
- Absorption trigger conditions: minimum deal duration, the independent's success threshold
  (charted once? sustained? Top 40?), and the per-year/whole-decade acquisition cap.
- Whether MidTier national distributors also absorb, or only Majors, and whether an absorbed
  MidTier counts toward the major family.
- Whether this interacts with the frozen first-chart tier buckets (section 15.2): absorption
  changes *owner*, not *first-chart imprint tier*, so it should be orthogonal, but verify the
  audit does not double-count or re-bucket on ownership change.

## 24. Consolidation lever: metric shipped, absorption being redesigned to a subsidiary model

This section continues section 23. The metric is resolved and shipped; the lever is gated and
validated for mechanics; two decade runs then exposed that absorption **as historically
implemented cannot raise major chart share at all**, and the user has approved the redesign
that fixes it. The subsidiary redesign itself is **not yet implemented** — this is the live
handoff point.

All work is uncommitted on branch `d7-artist-population-plateau` (clean at session start). Three
files are modified: `SimTools/ArtistPopulationLifecycleProbeSuite.cs`,
`SimTools/ChartAuditRunner.cs`, `Systems/CompetitorManager.cs`. `Data/AILabel.cs` is **not yet
touched** — the subsidiary field lands there. Build is clean (one pre-existing unused-event
warning); all 87 D6 probes + D5 pass.

### 24.1 The metric is resolved and shipped

Per section 23.1 and user confirmation, the 45-52% target attaches to **owner-Major chart-ENTRY
share**: distinct charting records per year whose *current owner*, resolved through the
acquisition chain (`ResolveCurrentOwner` over `acquiredBy`), is a **Major**. "Owner family" means
the corporate family headed by a Major — an absorbed independent joins that family via
`acquiredBy`, so its records count as Major-owned while its release imprint is unchanged.

Major+MidTier ("family") was rejected as the *target*: imprint Major+MidTier entries are already
~58% at 1969, above the band, so only Major-tier owner share is well-posed (~36-40% baseline,
must rise). Both are emitted so nothing is lost.

Shipped as four **additive** columns in `concentration.csv`
(`ChartAuditRunner.WriteConcentrationYear` / `CountOwnerFamilyEntries`):
`ownerMajorEntries`, `ownerMajorFamilyEntries` (Major+MidTier), `ownerMajorTop40Entries`,
`ownerMajorFamilyTop40Entries`. They consume no RNG and change no existing value. Invariant with
zero absorptions (verified in 1960): `ownerMajorEntries == chartEntriesMajor` and
`ownerMajorFamilyEntries == chartEntriesMidTier + chartEntriesMajor`.

### 24.2 The lever was already partly wired, and wrong-shaped

`DealResolution.Absorb` and `CompetitorManager.AbsorbLabel` already existed and the audit already
consumed them. But absorption was ungated: it fired on any deal expiry with
`dependency >= 0.56` **and** `deal.ownsMasters`, producing ~10-12 absorptions clustered in
**1961-62** with random-tier acquirers (independent-on-independent, even a small label "absorbing"
RCA, a self-absorb). That wrong shape is why `majorFamilyChartShare` *falls* 0.85 -> 0.64 across
the decade instead of rising.

### 24.3 Gated lever built and validated for mechanics (current tree)

`ResolveDistributionDeal`'s high-dependency branch now gates absorption through
`ShouldConsolidate` (private) = `IsConsolidationEligible` (internal static, pure, probeable)
`&& GD.Randf() < consolidationAbsorbChance`. `IsConsolidationEligible` requires:
`year >= consolidationStartYear` (1966), acquirer is Major (or, behind an off-by-default flag, a
national MidTier), client is independent-family, client has charted, and the decade cap is not
reached. Supporting state: `chartedLabelIds` (any Top-100 chart, filled in
`OnRecordChartUpdated`), `consolidationAbsorptionsThisDecade` counter, cap
`maxDecadeConsolidationAbsorptions`. `AbsorbLabel` now returns `bool` and increments the counter.
`ForceConsolidationForTest` is a scoped test hook for the forced-deal harness. Probe 87
(`ProbeConsolidationGate`) pins the gate seven ways. Constants:
`consolidationStartYear=1966, consolidationAbsorbChance=0.50, maxDecadeConsolidationAbsorptions=40,
consolidationRequireCharted=true, consolidationAllowNationalMidTier=false`.

Validated on 52-week probe runs: build clean, all 87 D6 + D5 pass, and 1960 `concentration.csv`
existing columns are **byte-identical** to reference `d7-major-tune-probes-52-1001` (the lever has
no deal expiries inside 52 weeks, so it is provably inert pre-window; the metric is purely
additive).

### 24.4 Two decade runs, and the decisive finding: absorption cannot move the metric

**v1 `d7-consolidation-lever-522-1001` (high-dependency gate):** only **2 absorptions** fired
(1966, 1968). owner-Major entry share stayed flat at ~40% (39.8% imprint / 40.0% owner at 1969).

Diagnosis: **high dependency is a transient early-decade state.** In 1966-69 there are 733 deal
term-expiries but only **24** sit at `dependency >= 0.56`; 583 are in the mid 0.35-0.56 renewal
band. Dependency erodes over renewals (the renew-worse branch shrinks `reachGranted` 0.85x each
time). There are ~230 *major-distributor* expiries late-decade, but almost all at mid-dependency,
invisible to the high-dep absorb branch.

**v2 `d7-consolidation-lever-v2-522-1001` (BLANKET — absorption decoupled from dependency, fired
on any major-distributor expiry, chance 0.35/cap 60):** fired **60 absorptions**, yet owner-Major
entry share bumped to 42.6% (1966) then **decayed to 37.8% (1969)**. The smoking gun:

| metric (1969) | v1 (2 absorptions) | v2 (60 absorptions) |
|---|---:|---:|
| owner-Major entries | 377 | **362** |
| owner-Major entry share | 40.0% | 37.8% |
| Major imprint entries (output) | 375 | **358** |

**60 absorptions produced *fewer* Major-owned chart entries than 2.** Root cause: `AbsorbLabel`
(shutdown-merge) deactivates the label and dumps its roster onto a **capacity-bound Major** (the
section 16-22 result: 8 firms, one Bernoulli release slot per week each). The absorbed artists
bottleneck — the Major's output cannot grow — while the independent's own output is destroyed.
Absorption only briefly reattributes the *existing* records, which retire in ~5 weeks, then the
share falls back. **Consolidation-by-absorption, as implemented, structurally cannot raise major
chart share; it slightly lowers it.**

### 24.5 User design decisions (all three confirmed this session)

1. **Absorption stays gated to HIGH dependency** (Stax leaned on Atlantic's distribution and was
   absorbed); low dependency lets a label leverage the deal while building its own infrastructure
   and staying independent (early Motown). It is **never a blanket effect across all deals**. The
   blanket v2 change was **reverted in full** — absorption is back in the high-dependency branch,
   constants back to 0.50/40. (Memory: `absorption-tied-to-high-dependency`.)
2. **Historically most small labels stayed dependent on majors and were absorbed by decade end;
   Motown was the uncommon breakout-via-leverage exception.** So the approved direction is
   **path (A): realistically grow/retain the dependent population** so most small labels reach
   high dependency and are absorbed late-decade under the existing gate — **not** lowering the gate.
3. **Absorption is redesigned to the SUBSIDIARY model (approved).** An absorbed label continues
   operating as a **major-owned subsidiary imprint** (Atlantic kept charting as Atlantic under WB):
   it keeps its own roster, release capacity and imprint — so it keeps charting, with no capacity
   bottleneck — while **ownership** rolls up to the major. This is both historically accurate and
   the only thing that moves the metric. **User requirement: the label must be clearly marked as a
   subsidiary after absorption.**

### 24.6 Subsidiary redesign plan (NOT yet implemented — the resume point)

Reach model confirmed: `AILabel.borrowedReach` is a computed property = `activeDeal.reachGranted`,
so nulling the deal drops borrowed reach to 0. A subsidiary must therefore *retain* that reach or
it stops charting. Concrete plan:

- **`Data/AILabel.cs`:** add `public string ownerLabelId;` (empty = independent) and
  `public bool IsSubsidiary => !string.IsNullOrEmpty(ownerLabelId);`. Keep it **orthogonal to
  `status`/`IsActive`** — a subsidiary stays operationally Rising/Stable and `IsActive` true (do
  NOT reuse `LabelStatus.Acquired`, which is a dead state excluded from `IsActive`).
- **`CompetitorManager.AbsorbLabel` (rewrite):** keep the guards (add `if (client.IsSubsidiary)
  return false;`), then do NOT deactivate, NOT move the roster, NOT mutate records'
  `baseRecord.labelId`, NOT transfer marketShare/hits/records/album-projects. Instead:
  - capture reach before nulling the deal: `float borrowed = client.borrowedReach;`
  - `client.ownedReach = Mathf.Clamp(client.ownedReach + borrowed, 0f, 1f);` (borrowed reach
    becomes permanent — the subsidiary is now part of the parent's national network);
  - grant the parent's regions permanently:
    `client.distributionRegions = client.distributionRegions.Union(distributor.distributionRegions
    ?? Array.Empty<string>(), StringComparer.Ordinal).ToArray();` (so `HasDistributionInRegion`
    persists and it keeps charting nationally — verify this is the field driving per-region
    coverage);
  - `client.ownerLabelId = distributor.labelId;`
  - `consolidationAbsorptionsThisDecade++; EmitDealEvent(..., DealResolution.Absorb, ...);
    client.activeDeal = null;` (the deal is now ownership; no expiry cycle);
  - `LabelLifecycleManager.Instance?.MarkLabelSubsidiary(client, distributor);` and `return true;`.
- **`LabelLifecycleManager.MarkLabelSubsidiary` (new, replacing `MarkLabelAcquired` for this
  path):** keep `status` operational, do NOT add to `defunctLabels` or increment `DefunctThisYear`,
  log "now a subsidiary of <parent>", optionally fire a distinct `OnLabelSubsidiary` event. (Leave
  `MarkLabelAcquired`/`LabelStatus.Acquired` for any genuine shut-down path.)
- **Exclusions:** in `TryGenerateDistributionOffer` add `&& !client.IsSubsidiary` (a subsidiary
  uses the family network, does not sign new deals); AbsorbLabel already guards re-absorption.
- **Audit (`ChartAuditRunner`):** already correct via `acquiredBy` (set from the Absorb event) +
  `releaseLabelId` for breadth + owner rollup. Because `baseRecord.labelId` stays the subsidiary,
  `ResolveCurrentOwner` walks `acquiredBy` to the Major; breadth keeps the subsidiary imprint;
  firm counts use the owner rollup so the subsidiary is not double-counted. Verify these on the run.
- **Forced-deal harness (`ChartAuditRunner` ~line 884):** the `forcedDealResolution == "absorb"`
  assertion currently checks the shutdown-merge outcome (`status == Acquired`, not in
  `GetOperatingLabels`, roster == 0). **Update it** to the subsidiary outcome: `IsSubsidiary`,
  `ownerLabelId == distributor`, still in `GetOperatingLabels`, roster retained.
- **Probes:** add a subsidiary-invariant probe (absorbed label stays active, `ownerLabelId` set,
  roster and imprint retained). Probe 87 (gate) is unaffected.

### 24.7 Resume sequence

1. Implement section 24.6. Build.
2. 52-week probe run **and** `--forced-deal-resolution=absorb` to exercise the subsidiary path;
   confirm 87+ probes pass and 1960 is byte-identical (existing columns) to
   `d7-major-tune-probes-52-1001`.
3. Decade run at seed 1001. The key check: with subsidiaries retained, does owner-Major entry
   share now **rise** with absorptions (each subsidiary keeps producing as Major-owned) rather than
   decay? Even the current high-dep trickle (~2 absorptions) should now *sustain* a gain.
4. If the metric moves but the count is still a trickle (high-dependency scarcity, section 24.4),
   implement **path (A)**: grow/retain the dependent population so most small labels stay dependent
   and are absorbed late-decade — likely via the existing major-courting/push path (already ramps
   post-1966 via `annualPost1966PushRamp`) and/or gentler dependency erosion. Ship as its own
   calibration change (section 12 rule) and tune to 45-52% by 1969.
5. Guardrails every decade run: cumulative imprint **breadth must not fall**; Small tail small and
   Independent-dominated; owner-Major entry share **rises into 1968-69 to 45-52%**; cross-check
   owner-unit `majorFamilyChartShare`.
6. Holdout seed (2029) before acceptance; forced exit/renew/absorb integrations; all probes;
   `git diff --check`.

### 24.8 Runs, references, environment

- **Reference for 1960 byte comparison** (existing columns): `d7-major-tune-probes-52-1001`
  (matches HEAD; `PromoAlbumConversionK = 1.50`; K does not affect 1960).
- **Decade runs this session:** `d7-consolidation-lever-522-1001` (high-dep gate, 2 absorptions,
  owner-Major ~40%); `d7-consolidation-lever-v2-522-1001` (BLANKET/rejected, 60 absorptions,
  owner-Major 37.8% — retained only as sensitivity evidence that proved the capacity bottleneck).
  Probe runs: `d7-consolidation-lever-probes-52-1001`, `d7-consolidation-lever-v2-probes-52-1001`.
- **Godot:** `C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`,
  run **outside the sandbox** (needs AppData). ~1 min for 52 weeks, ~15 min for 522. The
  `MissingSingletonsTemp does not inherit from Node` error at startup is pre-existing and benign.
- **Do not** raise Major release capacity to fix this (section 19.2 showed it floods the chart);
  the subsidiary model is the capacity-neutral fix.

## 25. Subsidiary redesign implemented (section 24.6)

The section 24.6 plan is now implemented and building clean (one pre-existing unused-event
warning). Six files modified: `Data/AILabel.cs`, `Systems/CompetitorManager.cs`,
`Systems/LabelLifecycleManager.cs`, `SimTools/ChartAuditRunner.cs`,
`SimTools/ArtistPopulationLifecycleProbeSuite.cs` (and this handoff).

### 25.1 What changed

- **`Data/AILabel.cs`:** added `public string ownerLabelId;` and
  `public bool IsSubsidiary => !string.IsNullOrEmpty(ownerLabelId);`, orthogonal to
  `status`/`IsActive` (a subsidiary stays operationally Rising/Stable and `IsActive` true).
- **`CompetitorManager.AbsorbLabel` (rewrite):** no longer shuts the client down. It guards
  (`+ if (client.IsSubsidiary) return false;`), emits the `Absorb` deal event, then calls the
  new pure `ApplySubsidiaryAbsorption(client, distributor)` and
  `LabelLifecycleManager.MarkLabelSubsidiary`. The old roster transfer, record `labelId`
  mutation, marketShare/hits transfer, album-project reattribution and `LabelStatus.Acquired`
  are all removed.
- **`CompetitorManager.ApplySubsidiaryAbsorption` (new, `internal static`, probeable):** folds
  `borrowedReach` into permanent `ownedReach`, unions the parent's `distributionRegions` into
  the client's (so per-region coverage persists once the deal is nulled), sets
  `ownerLabelId = distributor.labelId`, and nulls `activeDeal`. Roster/records/imprint untouched.
- **`LabelLifecycleManager.MarkLabelSubsidiary` (new):** keeps `status` operational, does NOT
  add to `defunctLabels` or increment `DefunctThisYear`, logs "now a subsidiary of <parent>".
  `MarkLabelAcquired`/`LabelStatus.Acquired` are left intact for any genuine shut-down path.
- **`TryGenerateDistributionOffer`:** early `if (client.IsSubsidiary) return;` — a subsidiary
  uses the family network and does not sign new deals.
- **Forced-deal harness (`ChartAuditRunner`):** the `absorb` assertion now checks the subsidiary
  outcome (`IsSubsidiary`, `ownerLabelId == distributor`, still in `GetOperatingLabels`,
  `activeDeal == null`, roster retained > 0) instead of the old shutdown outcome.
- **Probe 88 `ProbeSubsidiaryAbsorptionRetainsLabel`:** pins the four subsidiary invariants on
  `ApplySubsidiaryAbsorption`. Probe 87 (gate) is unaffected. Suite is now 1-88.

### 25.2 Harness fix required by the subsidiary model

The per-week forced-deal skim invariant (`ChartAuditRunner`, "Forced deal skim was not credited
to its distributor") equated `forcedDealClient.weeklyDistributionSkim` to
`forcedDealDistributor.weeklyDistributionIncome` **every week**. A retained subsidiary keeps
selling records with no active deal, and `GetSettlementDistributionSkimFraction` returns a
non-routed self-distribution fraction `0.25*(1-ownedReach)` for a deal-less label
(`CompetitorManager.cs:619`), so the client accrues skim that `RouteDistributionSkim` never
credits to any distributor (it early-returns when `activeDeal == null`). The check is now gated
`&& forcedDealClient.activeDeal != null` — the equality is only well-defined while skim is
actually routed. This is unchanged for the renew and deal-stays-active paths (deal always
active) and correctly skips the post-resolution weeks for exit and absorb.

### 25.3 Validation completed

- Build clean; `git diff --check` clean.
- 52-week probe run `d7-subsidiary-absorption-probes-52-1001`: D5 + all 88 D6 probes pass;
  1960 `concentration.csv` existing columns **byte-identical** to `d7-major-tune-probes-52-1001`;
  additive owner columns satisfy the zero-absorption invariant
  (`ownerMajorEntries == chartEntriesMajor`, `ownerMajorFamilyEntries == MidTier + Major`).
- Forced paths (`--force-deal-resolution=`): `absorb` now converts the client to a retained
  subsidiary (`Motown Records is now a subsidiary of Columbia`) and passes; `exit` and `renew`
  pass. Note the flag is spelled **`--force-deal-resolution`** (no "d"), not
  `--forced-deal-resolution` as section 24.7 step 2 wrote — the misspelled form is silently
  ignored.

### 25.4 Pre-existing failure discovered (NOT a regression)

The bare `--force-distribution-deal` run (null resolution, deal held active for the whole run,
recoup validated at end) fails with the same "Forced deal skim was not credited" error **on
clean HEAD**, before any of this session's changes (verified by stashing). It is unrelated to the
subsidiary work and pre-dates it; document and fix separately. The exit/renew/absorb forced
integrations all pass.

### 25.5 Decade run: mechanism validated, count is the remaining lever

`d7-subsidiary-absorption-522-1001`, seed 1001, completed clean (exit 0).

**The subsidiary mechanism works.** Only **1 absorption** fired (Moon -> Mercury Records, 1966,
dependency 0.561, just over the 0.56 high gate), yet from 1967 on `ownerMajorEntries` sustains a
small surplus over imprint `chartEntriesMajor` (1967 385 vs 383, 1968 373 vs 372, 1969 367 vs
366). A single retained subsidiary keeps producing charting records attributed to its Major owner
across multiple years — precisely what the shutdown-merge model structurally could NOT do (v2's
60 absorptions *decayed* owner share to 37.8%). Here owner-Major share holds flat and net-positive
instead of decaying. That is the capacity-bottleneck fix, confirmed.

**But the target is not reached, because the count is a trickle.** Owner-Major entry share stays
~0.38 across 1966-69 (0.378 / 0.387 / 0.376 / 0.379), far from the 45-52% band. Deal-ledger
resolutions over the decade: **Renew 1358, Signed 978, ClientClosed 357, Exit 271,
DistributorCollapsed 54, Absorb 1.** This is exactly the section 24.4 high-dependency scarcity:
dependency erodes over renewals (renew-worse shrinks `reachGranted` 0.85x each cycle), so almost
no late-decade major-distributor expiry sits at `dependency >= 0.56`.

**Guardrails held:** cumulative imprint breadth **397** (353 exact names) — did not fall. 1969
cumulative tier mix Small/Boutique/Independent/MidTier/Major = **40 / 50 / 268 / 31 / 8**;
below-MidTier is 358/397 = 90% of firms and 75% of that is Independent, Small tail 10%.

**Next (section 24.7 step 4): path (A).** The subsidiary redesign is complete and correct; the
remaining work is raising the absorption count so most small labels reach and hold high dependency
and are absorbed late-decade — via the existing major-courting/push path (`annualPost1966PushRamp`)
and/or gentler dependency erosion — **not** by lowering the gate. Ship that as its own calibration
change (section 12 rule) and tune owner-Major share to 45-52% by 1969. Run reference:
`d7-subsidiary-absorption-522-1001` (subsidiary mechanism, 1 absorption, owner-Major ~0.38).

## 26. Path A: reviving the push route to feed absorption

Diagnosis from the `d7-subsidiary-absorption-522-1001` telemetry (deal-ledger + offer-attempts),
before spending another run:

- **The push route is dead.** Of 978 signed deals, only **4** were `DistributorCourted` (push) and
  **zero** in 1965-69; 974 were `LabelSought` (pull). Yet push deals grant far more reach (mean
  `reachGranted` 0.483 vs pull 0.273) — they are the natural high-dependency generators, and
  `SelectDistributor` weights Majors 6x so they are Major-distributed and absorb-eligible.
- **Why push never fires:** `pushEvidence` (momentum > 0.60 or recent Top-40) fires ~45x/yr in
  1967-69, but base `monthlyPushOfferProbability` was 0.04, so the chance roll passed only 38 times
  all decade and only 4 reached signing (the rest lost to rejection — a proven built-reach indie
  rejects a deal it does not need, the correct Motown behaviour, but it starves the pool).
- **High-dependency expiries are front-loaded and erode away.** Distinct clients ever reaching
  `dependency >= 0.56`: 62. But high-dep expiries run 20/27/19 in 1961-63 then collapse to 7/6/6/5/8/4
  in 1964-69. The high-band renew-worse branch shrank `reachGranted` 0.85x/cycle, eroding
  high-dependency labels into the mid band **before** the 1966 window opened. In 1966-69 only ~10 of
  the ~24 high-dep expiries are Major-distributed, so the eligible (Major + charted + high-dep +
  in-window) pool is ~1-2 — hence the single absorption.
- **Timing constraint:** push terms are 78-104 wks, so only deals signed by ~1967 expire (and can
  absorb) before the 522-week run ends. The base, not just the post-1966 ramp, therefore matters.

### 26.1 The change (calibration, all in `CompetitorManager`)

Per the section 12 rule this ships as its own calibration change, separate from the section 25
mechanism. The consolidation **gate is untouched** (still `dependency >= 0.56`, Major acquirer,
charted, 1966+, cap 40) — per section 24.5 the gate is not lowered.

- `monthlyPushOfferProbability` **0.04 -> 0.10** — revive push from ~1965 so Major high-reach deals
  form and their 78-156wk terms expire inside the window.
- `annualPost1966PushRamp` **0.05 -> 0.10** — concentrate courting late (1967 0.20 / 1968 0.30 /
  1969 0.40).
- `consolidationAbsorbChance` **0.50 -> 0.75** — convert the eligible high-dep expiries the above
  now produce more reliably (within the existing gate, not a gate relaxation).
- High-band renew-worse reach erosion **0.85x -> 0.93x** — gentler dependency erosion so a genuinely
  dependent label stays dependent and is still absorbable when the window opens.

Cap stays 40 (the subsidiary model preserves imprint breadth, so a bounded wave cannot crush the
indie tail; if 40 binds, that is informative for the next step). This is not byte-identical to 1960
by design — it is a calibration change. Build clean; all probes still logic-valid (the four
constants are not probe-pinned; probe 87 tests `IsConsolidationEligible` with explicit args).

### 26.2 Measured effect — two iterations, and the real bottleneck

**v1 `d7-pathA-pushramp-522-1001`** (the 26.1 change): absorptions 1 -> **4**; high-dep in-window
expiries 24 -> 32 (the erosion softening working); owner-Major 1969 **0.385** (barely moved);
breadth 407. Push still under-fired: 10 signings, and the offer-attempt telemetry showed 59
`pushChancePassed` but 49 **rejected** by `ShouldAcceptDeal` — the "successful indie stays
independent" penalty (-0.35 at ownedReach>=0.45) fired on exactly the proven labels a Major courts.

**v2 `d7-pathA-accept-522-1001`** (added push-acceptance fix: `DistributorCourted` gets +0.35 and is
exempt from the independence penalty): absorptions **7**; high-dep in-window expiries **40**; but
owner-Major 1969 **0.380** — still flat. The acceptance fix multiplied push *signings* (10 -> 25) but
they landed in the wrong years: **15 in 1960, 3 in 1961** (expire pre-window), the rest 1968-69
(expire post-run); only **1** in the productive 1965-67 signing window.

**The real bottleneck, from the v2 telemetry:**

- **Push courtable pool is structurally tiny.** `pushEvidence` needs a proven (momentum>0.60 or
  recent Top-40) **and deal-free** label, but by mid-decade proven labels already hold pull deals.
  Distinct courtable labels in 1965-67: **4**. So no push-chance increase can supply volume there.
- **Deal routing is the true constraint.** Of ~1088 signings, only **199 (18%) went to Majors**;
  498 to MidTier, 191 to Independent distributors — all absorb-**ineligible** (gate needs a Major
  acquirer). The 8 Majors (96 slots) were nowhere near capacity. Of v2's 40 in-window high-dep
  expiries only **18 were Major-distributed** (20 MidTier, 2 Independent); after the charted filter
  and 0.75 roll that 18 yields exactly the 7 absorptions observed.
- **Magnitude:** reaching owner-Major 0.45 (~+90 entries over the ~365 imprint-Major baseline at
  1969) needs ~35 in-window absorptions of chart-active labels, i.e. an eligible pool of ~45.

### 26.3 v3 change: route the dependent population to Majors + shift courting to mid-decade

User-approved (routing + ramp-shift; gate still not lowered). All in `CompetitorManager`:

- **`SelectDistributor` Major weight 6x -> 12x** — the primary lever. Routes the dependent population
  toward the 8 (uncapped) Majors so more high-dependency deals are Major-distributed and therefore
  absorb-eligible. The high-dependency gate is untouched.
- **Courting ramp start 1966 -> 1964, base 0.10 -> 0.05** (new fields `consolidationCourtingRampStartYear`
  = 1964, `annualCourtingRampPerYear` = 0.12, replacing `annualPost1966PushRamp`). Concentrates
  courting in the productive 1965-67 signing window (chance 0.05 pre-1964, 0.17/0.29/0.41 in
  1965/66/67) so push deals expire inside the absorption window rather than in 1960 or post-run.
  Push-term length (78-104wk) is left realistic — 1960s major-indie distribution deals were multi-year;
  timing is fixed by *when* deals sign, not by truncating terms.

Kept from v2: acceptance fix, `consolidationAbsorbChance` 0.75, erosion 0.93x, cap 40.

**v3 `d7-pathA-routing-522-1001` result:** absorptions **8**, owner-Major 1969 **0.384** — still flat.
Two hard findings:

- **The routing weight did nothing.** Major share of signings stayed **18%** despite 6x->12x, because
  Majors are **capacity-pinned**: all 8 sit at the 12-client cap essentially every week (~95 of 96
  slots), so weight is irrelevant — they are almost never *eligible*. Capacity (12), not weight, is
  the binding knob, and 12 is unrealistically low (a national Major distributed dozens of imprints).
- **The metric is entry-volume-bound, not just count-bound.** The 8 absorbed subsidiaries added only
  **+4 entries** at 1969 (`ownerMajorEntries` surplus over imprint Major). Absorbable labels are, by
  the gate, high-dependency = small = **low chart volume** (~0.5-1 entry/yr, decaying). Reaching 0.45
  (~+90 entries) would need ~90 such absorptions — impossible under cap/breadth/§24.5. The chart-entry
  tier trend explains the gap: **Independents surge 22%->36.5%** across the decade while Major (41->38%)
  and MidTier (30->23%) decline. The model has the mid-60s indie boom but not the historical
  late-60s consolidation counter-wave, and only *high-volume* indie absorption can supply it.

## 27. Capacity fix + the "Stax" dependent-hitmaker archetype (v4)

User direction: fix capacity **and** create a minority of genuinely hit-making labels that are
financially fragile and stay dependent (a "Stax"), so absorbing them moves real chart volume — while
keeping the existing weak one-or-two-hit dependents and leaving Motown the build-reach-and-exit
exception. Bundled with the capacity fix (both separable in telemetry). Gate still not lowered.

- **Major distribution capacity 12 -> 24** (`IsEligibleDistributor`). The confirmed binding
  constraint; opens the Major-distributed pool absorption feeds on. Realism-supported.
- **Dependent-hitmaker archetype** (`AILabel.distributionDependentHitmaker`; generated in
  `RuntimeLabelProfileFactory.ApplyOperatingProfile` for `DependentHitmakerShare` = 12% of runtime
  Independents). Flagged labels get strong production/scouting (they chart), **low owned/national
  reach** (must lean on a distributor for national access), and a fragile balance sheet at founding
  (sign out of necessity). `GrowSelfBuiltDistributionReach` skips them, so they never graduate to
  independence — they chart through a major's network, stay high-dependency, and are absorbed
  late-decade with real volume. Roll uses the label's own PRNG (other labels unperturbed; non-flagged
  Independents consume one extra draw with no downstream effect; Small tier never flagged). Probe 89
  pins the archetype (minority, high production + low reach, deterministic, Independent-only). Suite
  now 1-89.

**v4 `d7-pathA-capacity-stax-522-1001` result:** capacity worked cleanly — Major share of signings
**18% -> 47%**, absorptions **8 -> 13**. But owner-Major 1969 was still **0.374**: the 13 absorptions
added only **+14 entries** (~1 each). The Stax archetype as built boosted creative *quality* but not
*output* — an Independent releases few records regardless of quality, so it charts ~1-3 entries and
absorbing it barely moves an entry-volume metric. Confirms a third time: the entry metric is
volume-bound and cannot be moved by absorbing individually low-volume small labels.

### 27.1 Candidate "other lever" (not yet actioned): master ownership

Owner-Major share is entry-volume-bound and the late-decade gap is the Independent surge. A lever
that does not depend on dozens of formal absorptions: **count Major-distributed records whose deal
has `ownsMasters` = true as Major-controlled** in the owner rollup. Push deals already own masters
80% of the time, so this is historically legitimate (owning the masters is owning the record) and
could supply much of the 45-52% rise directly. It is a metric-*definition* change (§24.1 defined
owner-Major via the acquisition chain only), so it is a user design call — flagged here with numbers
to follow from v4.

**User approved control-based ownership.** v4 sizing: 188 Major-distributed deals active at 1969 but
only **33 own masters** (the flat 0.15 pull `ownsMasters` rate) — meaningful but alone only ~0.42-0.43.
This motivated raising the Major `ownsMasters` rate (section 28).

## 28. Section 28 bundle: control-based ownership, MidTier absorption, the studio-era barrier

An outside review (Gemini, at the user's request) independently reached the same master-ownership
conclusion and surfaced one lever this work had missed plus a strong baseline lever. Every code
citation in that review was verified against the source before acting; all were accurate. The user
agreed with the findings. This section is the resulting bundle. **Not yet run at decade length** —
the user asked to stop after the probe pass and hand off. Build clean; 52-week probe run passes D5 +
all D6 1-89 (`d7-sec28-probes-52-1001`).

### 28.1 The missed lever and the correction to my own analysis

Absorbing individually low-volume Small/Independent labels cannot bridge a 10-15% entry-share gap
(v1-v4 confirm: ~1 entry per absorption). The historically dominant late-60s consolidation was
majors absorbing **high-volume MidTier** labels — WB->Atlantic 1967, Transamerica->Liberty/UA 1968,
MCA merging Decca/Kapp/Uni. Our gate (`IsConsolidationEligible`, and probe 87e) explicitly forbade
MidTier clients. That exclusion was deliberate (protect the frozen first-chart tier buckets) but it
locked Majors out of the only targets with enough chart volume to move the metric.

I had earlier dismissed "MidTier" by conflating the distributor tier (who absorbs — a MidTier
*distributor* absorbing yields a MidTier owner, no help) with the **client** tier (who is absorbed —
a Major absorbing a MidTier *client* yields a Major owner, the WB->Atlantic case). The latter is the
real lever.

**Mechanism nuance (correction to the outside review):** MidTiers are not distribution-*dependent*
(they hold their own reach), so merely opening the gate will not fire — a MidTier is not a
high-dependency client at a Major deal's expiry. It only fires paired with the promotion-deadlock fix
below, which lets a dependent hitmaker (the Stax archetype) *grow into* MidTier scale while still on
a major's P&D deal, and then be absorbed. The "WB->Atlantic" event thus emerges from the simulation
rather than being hardcoded. Magnitude caveat: our MidTiers average ~10-12 chart entries (~1% each),
not the review's "4-7% each," so expect several MidTier absorptions + master-control to reach the
band, not one giant event.

### 28.2 The six changes (all bundled; separable in telemetry)

1. **Control-based owner metric** (`ChartAuditRunner.CountOwnerFamilyEntries` +
   `IsMajorMasterControlled`): a charting record whose operating label holds an **active Major deal
   with `ownsMasters` = true covering that record** counts as Major-owned, alongside the acquisition
   chain. Models the P&D-era corporate/distributor share. Additive to the owner columns only; the
   existing chart columns are unchanged. *Attribution:* `ownerMajorEntries` jump vs imprint
   `chartEntriesMajor`.
2. **Higher Major `ownsMasters` rate** (`CompetitorManager.GenerateDealTerms`;
   `majorDistributorMastersOwnershipRate` = 0.55): a Major distributor takes masters on >=55% of its
   deals (was a flat 0.15 on pull), so its distributed records fold into the corporate share.
   *Attribution:* deal-ledger `ownsMasters` fraction on Major-distributed deals.
3. **MidTier clients absorbable** (`CompetitorManager.IsConsolidationEligible`): `absorbableClient`
   is now `clientTier != Major` (Small/Boutique/Independent/**MidTier**; a Major peer is never a
   target). Probe 87e updated. *Attribution:* Absorb events with a MidTier client.
4. **MidTier promotion deadlock fixed** (`LabelLifecycleManager.IsIndependentReadyForMidTier`): two
   routes now — the pre-existing organic owned-reach route, OR a **dependent-footprint route**
   (`chartingLastYear >= 4` and roster `>= 8`) that does not require owned national reach. Lets a
   Stax/A&M-style dependent hitmaker reach MidTier on a major's P&D deal. All other prerequisites
   (18+ months, 4 sustained quarters, profitable, runway) unchanged. *Attribution:* Independent->MidTier
   promotions of low-owned-reach labels.
5. **Studio-era production barrier** (`LabelLifecycleManager.DriftAttributes`): the post-1963
   +0.01/quarter production buff (up to +0.24 by 1969, previously granted to **every** active label
   free) is now gated to labels with `cashReserves >= GetMonthlyOverhead() * StudioUpgradeRunwayMonths`
   (6). Cash-starved small labels stagnate, as they historically did; Majors and capitalized labels
   pull acoustically ahead. This is the **baseline** lever against the ahistorical Independent
   chart surge (22%->36.5%). *Attribution:* production-quality distribution by tier over the decade;
   watch breadth. **Expected to lower breadth** — the user accepts this: propped-up breadth from a
   free buff is not real breadth, and healthier breadth levers come next.
6. **Stax dependent-hitmaker archetype** (section 27; unchanged): the source of high-volume dependent
   labels that ride the promotion->MidTier->absorption chain.

### 28.3 Test/probe updates

- Probe 87e: now asserts a Major **can** absorb a MidTier client but never another Major.
- Probe 68h-k (in `ProbeMidTierPromotionBoundary`): the dependent-footprint promotion route — a
  low-reach, high-dependency label with charting `>= 4` and roster `>= 8` promotes; charting 3 or
  roster 7 does not. Existing 68a-g unaffected (base gates fire first; organic case keeps reach `>=`
  0.50).
- Suite remains 1-89 (no new numbered probe; behaviors folded into existing gate/promotion probes).

### 28.4 Resume sequence

1. Decade run `--weeks=522 --seed=1001` (suggested name `d7-sec28-522-1001`). Watch, in order:
   - owner-Major entry share 1966-69 (target **0.45-0.52**), and the master-control surplus
     (`ownerMajorEntries` - imprint `chartEntriesMajor`);
   - MidTier `ownsMasters` fraction (should be ~0.55+) and Absorb events by **client tier** (expect
     some MidTier absorptions now);
   - Independent->MidTier promotions of dependent labels (the Stax chain firing);
   - imprint chart-entry tier mix (Independent surge should recede from 36.5%; Major/MidTier recover);
   - **breadth** (cumulative imprint IDs — expected to fall from ~407; judge whether the drop is only
     the removed free-buff inflation);
   - Small/Independent tail composition.
2. Attribute any surprise via the per-lever telemetry in 28.2 before changing constants.
3. If owner-Major overshoots >0.52, dial back `majorDistributorMastersOwnershipRate` and/or the
   MidTier-absorption rate before touching the gate. If short, revisit `consolidationAbsorbChance`,
   the dependent-footprint thresholds, or Stax share.
4. Holdout seed (2029), forced exit/renew/absorb integrations, `git diff --check`, all probes before
   acceptance.

### 28.5 Deferred (validated but out of this bundle's scope)

From the same review, valid and noted for later, not actioned:

- **Talent signing ignores momentum** (`GetRandomLabelForSigning` weights only scouting+budget):
  add `momentumScore`/`reputation` so artists gravitate to hot labels.
- **Boutique auto-promotes at roster 8** (`BoutiqueAuteurRosterThreshold`): Boutique is a business
  model, not a stepping stone; promotion should be a strategic pivot, not a roster cap.
- **`maxMonthlyBirths` = 6 duct-tape cap**: acknowledged in-code as an album-economy stability hack
  that flattens the mid-60s micro-label explosion; revisit once the album-project capacity is fixed.
- **Healthier breadth levers** to offset the production-barrier breadth drop (per user): to be
  designed after measuring section 28's breadth effect.

## 29. Section 28 decade run + owner-Major masters-rate ramp

The §28 bundle was run at decade length: `d7-sec28-522-1001`, seed 1001, clean.

**§28 result:** primary target holds — cumulative imprint breadth **423** (400-600 band), below-MidTier
Independent-dominated (291/385 = 76%), Small tail 8.7%, and the ahistorical Independent chart-entry
surge is contained (~20-23%, down from 36.5%). Absorptions rose to **9** including MidTier clients
(Capitol, Volt, Galactic) — the MidTier-absorption + Stax->promotion chain fires (Era-Parkway,
Crown Way promoted then absorbed). 82 Independent->MidTier promotions. Masters 37% signed / 44% renew.

**§28 problem:** owner-Major entry share **overshot and was flat** — 51.8% in 1960 rising only to
55.8% in 1969, ~52-56% every year (target 45-52). The +190 entry surplus over imprint-Major is almost
entirely the control-based master-ownership metric, not the 9 acquisitions.

**User correction on the target shape:** majors were NOT dominant in 1960 — they sat out rock and roll
in the mid/late 50s and the indies carried it, so **1960 major share must sit BELOW 1968-69**. The
target is a RISE from a fragmented 1960 into 45-52% by 1968-69 (late-60s P&D consolidation), not a flat
high line. §28's flat 51.8% at 1960 is backwards.

### 29.1 The masters-rate ramp (calibration)

`majorDistributorMastersOwnershipRate` (flat 0.55) was replaced with a year ramp
(`GetMajorMastersOwnershipRate`, linear between `majorMastersRampStartYear` and `...FullYear`,
`...Early`/`...Late` endpoints) in `CompetitorManager`. Rationale: early-60s indie deals were mostly
distribution-only (indie keeps masters); the late-60s P&D consolidation is when majors took the masters.
Isolated to owner columns — breadth/tier byte-identical to §28 (the §12 discipline held).

Iterations, all seed 1001:
- **`d7-sec29-522-1001`** (early 0.28 / late 0.52, ramp 1962-1969): 1960 **47.9** -> 1969 **50.3**. All
  years in-band but the rise is only +2.4 — too shallow.
- **`d7-sec29b-522-1001`** (early 0.15 / late 0.55, ramp 1962-1969): 1960 45.8 -> 1969 45.7 — **flat, no
  rise, WORSE**. Steepening backfired.

### 29.2 Root cause: renewals freeze the masters flag

`ResolveDistributionDeal` renews by keeping the same deal object; `GenerateDealTerms` (and its masters
roll) runs only at original signing. So a deal's `ownsMasters` is locked at signing and re-logged
unchanged across all 1599 renewals. The active-deal masters composition is therefore sticky/lagged
(blended rate only crawled 19%->27% for a 0.15->0.55 ramp), and lowering early/mid rates dragged the
LATE years down (1969's charting deals were signed years earlier). **A flat OR a monotonic-ramp rate on
signing-only cannot produce a rise.** `ownsMasters` is a **metric-only** flag — its sole consumers are
`IsMajorMasterControlled` (owner-Major chart metric) and ledger logging; it does NOT touch deal
economics, skim, reach, dependency, or the absorption gate. Verified by grep.

## 30. Section 30 bundle: masters re-roll at renewal + Boutique archetype/breakout pivot (ACCEPTED)

Two changes, user-approved as a bundle (user accepted the §12 attribution risk; they remain separable in
telemetry — Boutique = promotion counts, owner-Major = masters columns). Run `d7-sec30-522-1001`, seed
1001, clean. Probes: D5 + D6 1-89 pass (`d7-sec30-probes-52-1001`).

### 30.1 The two changes

1. **Masters re-roll at renewal** (`CompetitorManager`): both renew branches in `ResolveDistributionDeal`
   now call `RerollMastersOnRenewal`, which re-rolls `deal.ownsMasters` at the CURRENT year's rate
   (`CurrentDealMastersRate` = push/pull base, raised to `GetMajorMastersOwnershipRate(year)` for a Major
   distributor). Models majors taking masters at renewal during consolidation. The roll uses a
   seed-stable FNV hash (`GetDeterministicMastersRenewalRoll`), NOT the global RNG stream, so breadth and
   tier composition are unperturbed. This makes the ramp finally propagate through the renewing pool.
   Ramp retuned for the stronger propagation: **early 0.15 / late 0.45, ramp 1962-1968**.
2. **Boutique archetype + breakout pivot** (`LabelLifecycleManager.TryPromoteLabel`): the roster-at-cap
   trigger is replaced. A Boutique promotes to Independent only if `IsBoutiqueGrowthArchetype(archetype)`
   (auteur/niche archetypes JazzPrestige, BluesRoots, FolkBoutique, GospelPowerhouse, CountrySpecialist
   never promote) AND `chartingLastYear >= BoutiquePivotMinimumRecentChartingRecords` (=3, a genuine
   breakout, above the Small->Independent bar of 1). 18-month + sustained-capability gates unchanged.

### 30.2 Result — ACCEPTED by user

- **Owner-Major: the rise landed.** 1960 **45.8** -> 1963 46.2 -> 1966 49.8 -> 1968 51.3 -> 1969 **53.7**
  — a genuine +7.9 climb from a fragmented 1960 into late-60s consolidation, 1960 clearly below the late
  years. Minor: 1969 (53.7) tips ~1.7 over the 52 ceiling (1968 51.3 in-band). User: "53%~ is fine for
  Majors at the moment."
- **Boutique fix verified:** 17 promotions (vs 9), ALL growth archetypes (SoulFactory, TeenHitMachine,
  RegionalHustler, CorporateGiant), zero auteur — confirmed by cross-ref (an apparent GospelPowerhouse
  was a duplicate-display-name collision: the real promoted "Thirsty Records" is `label_0565`, SoulFactory).
- **Cost (attributable entirely to the Boutique gate — masters re-roll is metric-only):** cumulative
  breadth **423 -> 402** (still in band, thin cushion). Mechanism: promoting genuine-breakout Boutiques
  into stronger Independents crowds the late chart, so fewer brand-new IDs break through. Small tail grew
  8.7% -> 11.2%; below-MidTier still 72% Independent. 1969 mix S/B/I/M/Maj = 45/57/262/30/8.

### 30.3 Minor follow-up spotted (non-blocking)

5 of the 17 Boutique promotions were `CorporateGiant` — possibly launch Boutiques with an uninitialized
archetype defaulting to enum 0 (= CorporateGiant), i.e. the §6.1 launch-factory incomplete-init issue.
CorporateGiant is a legitimate growth archetype so the promotions are not wrong, but the default-0
suspicion is worth confirming in the launch factory.

### 30.4 Resume sequence (holdout validation — next session)

1. **Holdout seed run** `--seed=2029 --weeks=522` (suggested `d7-sec30-522-2029`), same flags. The key
   checks: does breadth hold >=~400 on a second seed (402 on 1001 is thin), and does the owner-Major arc
   still rise from a low-40s/mid-40s 1960 into ~50-53 by 1969? If breadth dips below 400, tune before
   accepting (see below).
2. **Breadth to ~500 must come from elsewhere, not Boutique** (explicit user goal). The Boutique gate
   cost breadth; do NOT loosen it to buy breadth back. Candidate healthier breadth levers (§28.5, and the
   deferred list): the `maxMonthlyBirths` = 6 duct-tape cap that flattens the mid-60s micro-label
   explosion; the studio-era production barrier's breadth effect; talent-signing-ignores-momentum. Design
   a dedicated breadth lever and ship it as its own calibration change.
3. If tuning is wanted: late masters rate 0.45 -> 0.42 pulls 1969 from 53.7 to ~51; Boutique bar 3 -> 4
   promotes fewer and recovers some breadth cushion. Both are one-line constant changes.
4. Before final acceptance: forced exit/renew/absorb integrations, all probes, `git diff --check`.

### 30.5 Genre shape (release-level, sec30)

Quick look at `single-release-lanes.csv` (chart-level genre dump is suppressed by `--lean-probe`; this is
what labels RELEASE by genre, a proxy — not chart outcomes, and not A/B-attributable to the lifecycle
changes without a comparison run). The shape is historically faithful: RockAndRoll declines 14%->2%,
TeenPop/DooWop/TraditionalPop fade, the British invasion (BritishPop/BritishBeat) appears exactly at
1964-65, Soul stays strong (11-14%), Gospel rises 2%->6%, and the late-60s rock diversification
(Psychedelic, FolkRock, GarageRock, HardRock, Funk, BluesRock, Progressive, AcidRock, ProtoPunk/Metal)
emerges 1966-69; Reggae/Ska/Rocksteady appear late. ~40 genres with credible emergence/decline arcs.
Note the genre diversity is realized per-record from market demand even though runtime label
specialization is limited to 7 preferred genres (the §"Document/defer" concern) — worth a proper
chart-level genre audit on a non-lean run next session.
