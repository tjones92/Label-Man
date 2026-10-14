# Phase 4 recruitment: matched four-year gate

Revision: `2992acbfede85fe8e7be8cb2ab31ea2d71128e41`. Four complete 208-week runs, seeds 1001 and 2002.

Controls and treatments use the current city placement build. Recruitment is the only flag changed within each pair.

The practical screen was set before treatment results: declines over 5% in cumulative singles, albums, market net or final roster headcount; label failure-rate increase over 5 percentage points; affordable vacancy excess over max(25% of control, 208 label-weeks). Integrity and unexplained signing counts must be zero. Annual and tier results require review. Two seeds do not establish statistical significance.

| Measure | Seed 1001 control → recruitment | Seed 2002 control → recruitment |
|---|---:|---:|
| singleUnits | 619,578,608 → 608,239,745 (-1.83%) | 623,750,236 → 620,417,908 (-0.53%) |
| albumUnits | 248,542,047 → 261,023,504 (+5.02%) | 242,082,326 → 243,742,791 (+0.69%) |
| marketNet | 876,317,742 → 900,156,495 (+2.72%) | 868,299,109 → 866,985,604 (-0.15%) |
| endRostered | 3,163 → 3,188 (+0.79%) | 3,064 → 3,080 (+0.52%) |
| defunct | 388 → 324 (-16.49%) | 365 → 322 (-11.78%) |
| vacancyWeeks | 5,070 → 6,612 (+30.41%) | 4,645 → 5,594 (+20.43%) |
| vacancyWeeksExcludingAcceptedLegacyUk | 4,904 → 6,196 (+26.35%) | 4,464 → 5,178 (+15.99%) |

Economic gate passed: **True**. Recruitment pressure screen passed: **False**. Combined development screen passed: **False**.

Seed 1001 failed gates: affordableVacanciesExcludingAcceptedLegacyUk. Integrity maxima: {'ownershipConflicts': 0, 'duplicateRosterEntries': 0, 'duplicatePoolEntries': 0, 'terminalRostered': 0, 'terminalReleaseEligible': 0, 'duplicateSeekingEntries': 0, 'latentUnsignedPoolEntries': 0, 'seekingMissingFromUnsignedPool': 0, 'prospectStatusContractConflicts': 0, 'duplicateBills': 0, 'personConflicts': 0, 'remoteDays': 0, 'capacityErrors': 0, 'unexpectedRuntimeErrors': 0}. Unexplained signings: 0.
Opening major cohort excluding accepted UK exceptions: finance-ledger net change +6.81%. This is a per-label finance metric, distinct from the market settlement net above.
Seed 2002 failed gates: none. Integrity maxima: {'ownershipConflicts': 0, 'duplicateRosterEntries': 0, 'duplicatePoolEntries': 0, 'terminalRostered': 0, 'terminalReleaseEligible': 0, 'duplicateSeekingEntries': 0, 'latentUnsignedPoolEntries': 0, 'seekingMissingFromUnsignedPool': 0, 'prospectStatusContractConflicts': 0, 'duplicateBills': 0, 'personConflicts': 0, 'remoteDays': 0, 'capacityErrors': 0, 'unexpectedRuntimeErrors': 0}. Unexplained signings: 0.
Opening major cohort excluding accepted UK exceptions: finance-ledger net change +2.55%. This is a per-label finance metric, distinct from the market settlement net above.

Scope clarification during testing: the user accepts EMI and Decca UK as legacy exceptions outside US-only recruitment. Their empty rosters and missing sales remain in raw totals and tier outputs. The vacancy screen subtracts their weekly active, affordable operating-target deficits from the aggregate demand count; the weekly vacancy telemetry is a snapshot proxy, not a new replay of the activation owner. Other unknown/foreign HQs remain visible. No economic threshold was changed.

Full totals, annual and tier comparisons, route counts and exact manifests are in `analysis.json` and `runs.json` beside this report. Decade validation remains deferred. No gameplay constants or defaults were changed for this test.

## Decision and default

The user requested a four-year economic gate before Phase 5 and confirmed that
passing it should enable recruitment by default. The economic screen passes on
both seeds. On that basis recruitment now defaults on, with
`--disable-scene-recruitment` retaining a clean control. Explicit local-scene,
persistence, population and genre-market disable modes suppress the default.

This does not erase the pressure diagnostic: seed 1001 narrowly exceeds the
25% vacancy screen after the two accepted UK exceptions are removed. The Phase 5
first slice observes pressure without feeding it into formation or migration.
It is not a claim of complete recruitment/playability balance or decade acceptance.

Annual whole-market changes stay within the 5% economic screen: the largest
single decline is seed 1001's 1960 -4.82%; the largest annual Album decline is
seed 2002's 1963 -0.88%; the largest annual net decline is seed 2002's 1963 -1.30%.
Independent market net rises 6.15% / 8.73%. MidTier rises 28.58% / 4.32%, boutique
2.33% / 14.24%, and small 8.16% / 2.87%. Tier-format reshuffling is descriptive:
boutique Albums fall 7.33% on seed 1001 while that tier's overall net increases.
The major-tier drop includes the two accepted legacy UK exceptions; the other
opening majors' finance-ledger net rises 6.81% / 2.55%.

Exact flags, engine/assembly/source hashes, exit codes, 208-week completion
markers and durations are in `SimLogs/scene4-four-year-v1/runs.json`. All four
runs used the same original Phase 4 assembly, before changing any defaults or
building the Phase 5 observer. The primary band-member checkout is untouched.

Reproduce the four-year experiment using a new run tag:

```powershell
./SimTools/RunSceneFourYearAudit.ps1 -RunTag scene4-four-year-v2
python ./SimTools/analyze_scene_four_year.py scene4-four-year-v2
```

Use the bundled Python executable if `python` resolves to a Windows Store stub.
No decade run was launched; that gate remains deferred to the end, as requested.
