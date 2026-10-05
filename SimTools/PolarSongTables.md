# Polar shipped tables and constants

October 3, 2026. Exact values from the current JSON files; no retuning in the default-on/final-audit pass. The configuration provenance remains provisional. Unit/algorithm values (six demand axes, four identity axes, FNV hash constants) are schema/algorithm definitions, not gameplay calibration. The historical numbered sign-offs in the unsigned draft are not independently verified signatures; release activation follows the current user's request.

## Numbers (polar-recording-calibration-v3)

General sources: Spec A §§3–7; the current derivation, fit, resolver and behavior implementations; rock/recording calibration reports. Capability/headroom/identity/Moment numbers implement fit; actor/ensemble/session/history/reach/rigidity numbers implement the minimal adapter; candidate/resolver/lyric/reinterpretation numbers govern arrangements; refusal/standing/empty-songbook numbers govern resistance; selection numbers govern AI-only ranking; execution/hook/quality/promotion/critic/conversion numbers govern calibrated consumers; rock factors govern rare songbook exceptions. maxShadowRowsPerWeek is a telemetry budget. Exact original section-level author comments are available in the spec and implementation; provisional keys have no fabricated individual signed gate.

| Key | Value | Source / rationale |
|---|---:|---|
| capabilityPenalty | 2.2 | Spec A §5 fit |
| headroomBonus | 0.12 | Spec A §5 fit |
| identityBasePenalty | 0.7 | Spec A §5 fit |
| rigidityPenalty | 0.6 | Spec A §5 fit |
| momentPenalty | 1.3 | Spec A §5 fit |
| neutralMoment | 0.5 | Spec A §5 fit |
| wordDensityBlend | 0.45 | Spec A §4 derivation; Spec B/C taxonomy |
| productionDemandStep | 0.12 | Spec A §4 derivation; Spec B/C taxonomy |
| instrumentationDemandStep | 0.04 | Spec A §4 derivation; Spec B/C taxonomy |
| techniqueDemandStep | 0.06 | Spec A §4 derivation; Spec B/C taxonomy |
| formDemandStep | 0.12 | Spec A §4 derivation; Spec B/C taxonomy |
| plasticityDensityPenalty | 0.15 | Spec A §4 derivation; Spec B/C taxonomy |
| plasticityFormPenalty | 0.1 | Spec A §4 derivation; Spec B/C taxonomy |
| actorNuanceTechnical | 0.55 | Spec A §3 minimal act adapter; explicit provisional mapping |
| actorNuanceVersatility | 0.3 | Spec A §3 minimal act adapter; explicit provisional mapping |
| actorNuanceReliability | 0.15 | Spec A §3 minimal act adapter; explicit provisional mapping |
| ensembleCohesion | 0.6 | Spec A §3 minimal act adapter; explicit provisional mapping |
| ensembleSkill | 0.4 | Spec A §3 minimal act adapter; explicit provisional mapping |
| deliveryTechnical | 0.6 | Spec A §3 minimal act adapter; explicit provisional mapping |
| deliveryVersatility | 0.4 | Spec A §3 minimal act adapter; explicit provisional mapping |
| sessionProducerWeight | 0.5 | Spec A §3 minimal act adapter; explicit provisional mapping |
| missingSessionCraft | 0.4 | Spec A §3 minimal act adapter; explicit provisional mapping |
| backupVocalFactor | 0.65 | Spec A §3 minimal act adapter; explicit provisional mapping |
| traitIdentityAdjustment | 0.08 | Spec A §3 minimal act adapter; explicit provisional mapping |
| historyIdentityBlend | 0.1 | Spec A §3 minimal act adapter; explicit provisional mapping |
| neutralTrait | 0.5 | Spec A §3 minimal act adapter; explicit provisional mapping |
| reachCraft | 0.35 | Spec A §3 minimal act adapter; explicit provisional mapping |
| reachExperimental | 0.35 | Spec A §3 minimal act adapter; explicit provisional mapping |
| reachCohesion | 0.3 | Spec A §3 minimal act adapter; explicit provisional mapping |
| missingDisposition | 0.3 | Spec A §3 minimal act adapter; explicit provisional mapping |
| rigidityBase | 0.25 | Spec A §3 minimal act adapter; explicit provisional mapping |
| rigidityAmbition | 0.55 | Spec A §3 minimal act adapter; explicit provisional mapping |
| rigidityRoots | 0.2 | Spec A §3 minimal act adapter; explicit provisional mapping |
| candidateDemandReach | 0.5 | Directive Phase 4 deterministic resolver; provisional mapping |
| resolverIdentityWeight | 2 | Directive Phase 4 deterministic resolver; provisional mapping |
| resolverExecutionWeight | 1 | Directive Phase 4 deterministic resolver; provisional mapping |
| nearTie | 0.001 | Directive Phase 4 deterministic resolver; provisional mapping |
| lyricFlipStretch | 0.06 | Directive Phase 4 deterministic resolver; provisional mapping |
| lyricFlipReach | 0.65 | Directive Phase 4 deterministic resolver; provisional mapping |
| reinterpretationThreshold | 0.05 | Spec A §6 reinterpretation diagnostic |
| reinterpretationLandedCenter | 0.5 | Spec A §6 reinterpretation diagnostic |
| reinterpretationMultiplier | 2 | Spec A §6 reinterpretation diagnostic |
| refusalBase | 0.3 | Spec A §7 resistance |
| refusalAmbition | 0.35 | Spec A §7 resistance |
| refusalStretchCapability | 0.9 | Spec A §7 resistance |
| newSigningStanding | 0.15 | Spec A §7 resistance |
| emptySongbookSoftening | 0.5 | Spec A §7 resistance |
| tasteDrift | 0.2 | Directive C10 lagged market-centroid proposal |
| maxShadowRowsPerWeek | 2048 | Shadow instrumentation budget; implementation limit |
| selectionCapability | 0.35 | Spec A §10 AI-only selection |
| selectionIdentity | 0.45 | Spec A §10 AI-only selection |
| selectionMoment | 0.2 | Spec A §10 AI-only selection |
| executionPenalty | 0.35 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| executionFullCapability | 0.9 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| recordingHookBlend | 0.9 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| hookCeilingStrength | 0.15 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| recordingQualityHookWeight | 0.5 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| recordingQualityProductionWeight | 0.3 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| promotionRealizationWeight | 0.65 | Spec A §5 consumer separation; PolarRecordingRealizationReport calibration |
| criticIdentityPenalty | 0.2 | Spec A §5 component consumers; behavior/calibration report |
| momentConversionStrength | 0.1 | Spec A §5 component consumers; behavior/calibration report |
| rockStandardFactor | 0.02 | PolarRockCalibrationReport authorized rare-exception proposal |
| rockTraditionalFactor | 0.02 | PolarRockCalibrationReport authorized rare-exception proposal |

Identity distance/Stretch weights, in Toughness/Sophistication/Sincerity/Maturity order: 1.25, 1, 1.15, 0.85.

## Archetypes

Axis order: VocalPower, VocalNuance, Musicianship, Ensemble, LyricDelivery, StudioCraft, Toughness, Sophistication, Sincerity, Maturity. Data rows below are the shipped resolver mappings (archetype, era/family eligibility, pace/mood and axis base), not a separate hidden switch table.

| Archetype | Ten axes in declared order | Plasticity | From year | Families | Pace | Mood |
|---|---|---:|---:|---|---|---|
| StomperRocker | 0.65, 0.2, 0.4, 0.7, 0.2, 0.2, 0.8, 0.2, 0.6, 0.35 | 0.6 | 1950 | Rock | fast | driving |
| MidTempoRocker | 0.55, 0.3, 0.45, 0.6, 0.4, 0.3, 0.7, 0.3, 0.7, 0.45 | 0.55 | 1950 | Rock | mid-tempo | assertive |
| GrooveRiffVamp | 0.45, 0.35, 0.45, 0.65, 0.25, 0.3, 0.65, 0.25, 0.65, 0.55 | 0.65 | 1950 | Rock, RhythmAndSoul, Blues | mid-tempo | languid |
| ShuffleTwelveBar | 0.45, 0.4, 0.4, 0.5, 0.35, 0.2, 0.6, 0.25, 0.75, 0.6 | 0.65 | 1900 | Rock, Blues, RhythmAndSoul | shuffle | easy |
| SlowBlues | 0.5, 0.7, 0.45, 0.35, 0.45, 0.2, 0.6, 0.3, 0.9, 0.75 | 0.45 | 1900 | Blues, RhythmAndSoul, Rock | slow | desolate |
| JamExtendedWorkout | 0.4, 0.35, 0.85, 0.75, 0.2, 0.3, 0.7, 0.4, 0.7, 0.65 | 0.4 | 1965 | Rock, Jazz, Blues | extended | exploratory |
| LightAndShadeEpic | 0.7, 0.65, 0.8, 0.8, 0.5, 0.65, 0.7, 0.65, 0.85, 0.75 | 0.25 | 1967 | Rock | shifting | dramatic |
| CountryTwoBeat | 0.45, 0.35, 0.4, 0.5, 0.4, 0.2, 0.55, 0.25, 0.7, 0.45 | 0.65 | 1900 | Country, Rock | two-beat | jaunty |
| BrightPopNumber | 0.4, 0.35, 0.3, 0.6, 0.25, 0.45, 0.25, 0.4, 0.55, 0.25 | 0.7 | 1900 | Pop | bright | hopeful |
| MidTempoPopSong | 0.4, 0.5, 0.35, 0.55, 0.45, 0.4, 0.3, 0.5, 0.7, 0.45 | 0.65 | 1900 | Pop | mid-tempo | warm |
| JauntyMusicHallRomp | 0.4, 0.45, 0.35, 0.6, 0.6, 0.3, 0.2, 0.55, 0.35, 0.6 | 0.5 | 1900 | Pop, NonMusic | jaunty | playful |
| SingAlongChant | 0.5, 0.2, 0.2, 0.7, 0.2, 0.2, 0.4, 0.2, 0.65, 0.35 | 0.75 | 1900 | Pop, Rock, Gospel | mid-tempo | communal |
| DanceNumber | 0.4, 0.3, 0.4, 0.75, 0.2, 0.4, 0.45, 0.35, 0.5, 0.35 | 0.7 | 1900 | Pop, RhythmAndSoul, Latin, Caribbean | fast | celebratory |
| ChamberBaroquePiece | 0.4, 0.75, 0.6, 0.85, 0.45, 0.7, 0.15, 0.9, 0.8, 0.65 | 0.35 | 1964 | Pop, Classical | measured | wistful |
| DramaticBalladBigBuild | 0.85, 0.75, 0.45, 0.75, 0.45, 0.55, 0.5, 0.55, 0.95, 0.75 | 0.5 | 1900 | Pop, RhythmAndSoul, Gospel | dramatic-build | resolute |
| SlowBallad | 0.45, 0.75, 0.3, 0.45, 0.45, 0.3, 0.2, 0.55, 0.85, 0.65 | 0.55 | 1900 | Pop, Country, Folk, RhythmAndSoul | slow | yearning |
| ReverieMoodPiece | 0.3, 0.7, 0.45, 0.55, 0.25, 0.6, 0.15, 0.75, 0.75, 0.65 | 0.4 | 1900 | Pop, Jazz, Classical | slow | dreamlike |
| LushStandard | 0.55, 0.9, 0.25, 0.7, 0.55, 0.5, 0.15, 0.9, 0.75, 0.9 | 0.55 | 1900 | Pop, Jazz | measured | elegant |
| SaloonBallad | 0.35, 0.9, 0.3, 0.4, 0.7, 0.25, 0.25, 0.8, 0.95, 0.95 | 0.4 | 1900 | Pop, Jazz | slow | reflective |
| CharmSong | 0.35, 0.65, 0.3, 0.5, 0.4, 0.3, 0.15, 0.65, 0.65, 0.65 | 0.6 | 1900 | Pop, Jazz | lilting | hopeful |
| Swinger | 0.5, 0.7, 0.65, 0.8, 0.45, 0.3, 0.35, 0.8, 0.6, 0.8 | 0.55 | 1900 | Pop, Jazz | swing | confident |
| HornDrivenSoulNumber | 0.8, 0.55, 0.4, 0.8, 0.35, 0.4, 0.7, 0.35, 0.85, 0.6 | 0.55 | 1950 | RhythmAndSoul | mid-tempo | commanding |
| DeepSoulPleader | 0.85, 0.8, 0.35, 0.55, 0.3, 0.3, 0.55, 0.35, 0.95, 0.7 | 0.35 | 1950 | RhythmAndSoul, Gospel | dramatic-build | yearning |
| FunkWorkout | 0.75, 0.35, 0.65, 0.85, 0.25, 0.4, 0.85, 0.25, 0.7, 0.6 | 0.55 | 1965 | RhythmAndSoul | fast | insistent |
| SpiritualShout | 0.9, 0.65, 0.35, 0.8, 0.3, 0.2, 0.65, 0.25, 0.95, 0.7 | 0.5 | 1900 | Gospel, RhythmAndSoul | dramatic-build | exultant |
| VerseDrivenSong | 0.3, 0.55, 0.25, 0.3, 0.95, 0.2, 0.4, 0.75, 0.85, 0.7 | 0.25 | 1900 | Folk, Country | measured | reflective |
| ProtestMessageSong | 0.4, 0.5, 0.3, 0.4, 0.9, 0.2, 0.65, 0.65, 0.95, 0.7 | 0.3 | 1900 | Folk, Country, Rock | measured | resolute |
| RagaModalDrone | 0.35, 0.55, 0.8, 0.7, 0.3, 0.65, 0.35, 0.8, 0.8, 0.7 | 0.2 | 1965 | Rock, Pop, Classical | sustained | meditative |
| SuiteMultiPart | 0.5, 0.55, 0.85, 0.9, 0.55, 0.95, 0.45, 0.9, 0.7, 0.75 | 0.15 | 1966 | Rock, Pop, Classical | shifting | dramatic |
| Collage | 0.2, 0.2, 0.65, 0.6, 0.45, 0.95, 0.4, 0.85, 0.35, 0.7 | 0.2 | 1966 | Rock, Pop, NonMusic | shifting | surreal |
| Novelty | 0.35, 0.3, 0.25, 0.5, 0.55, 0.35, 0.25, 0.3, 0.15, 0.3 | 0.65 | 1900 | Pop, NonMusic | jaunty | comic |
| SpokenWord | 0.1, 0.35, 0.1, 0.15, 0.95, 0.2, 0.45, 0.6, 0.7, 0.8 | 0.25 | 1900 | NonMusic, Folk | spoken | reflective |
| Medley | 0.55, 0.6, 0.65, 0.8, 0.55, 0.45, 0.4, 0.6, 0.65, 0.65 | 0.35 | 1900 | Pop, Jazz, Rock | shifting | varied |
| LivePartyRecord | 0.65, 0.3, 0.4, 0.8, 0.4, 0.2, 0.6, 0.2, 0.6, 0.4 | 0.65 | 1900 | Rock, RhythmAndSoul, Pop | fast | communal |

## Family/genre priors

Four identity axes in the same identity order. The resolver permits era/family rows plus the reference shape; plasticity × reach bounds movement; session support does not invent vocalist skill. Zero reach/plasticity or an empty eligible candidate pool preserves the reference reading.

Shipped resolver procedure: copy the reference/demo taxonomy; replace archetype, pace and mood with the selected row; set primary genre to the project genre and retain the prior primary as secondary when it changes. Known lead vocal approach can replace delivery. RomanticAddress flips to DemandBoast (plus CallAndResponse for RhythmAndSoul) when **projected** Stretch and reach meet lyricFlipStretch/lyricFlipReach; the final discrete arrangement's committed Stretch may be smaller. Meter/form, vocal presence and content/tag data are inherited. Candidate demand movement is bounded by reach × plasticity × candidateDemandReach, then rows are ranked by mean squared demand mismatch, weighted identity mismatch and execution shortfall; near ties use the planned master ID's stable FNV hash. This is the actual general procedure in PolarCoverResolver.Propose, not a per-title mapping.

| Scope | Identity | Fallback |
|---|---|---|
| Family Pop | 0.25, 0.6, 0.65, 0.55 | MidTempoPopSong |
| Family Rock | 0.75, 0.3, 0.7, 0.45 | MidTempoRocker |
| Family RhythmAndSoul | 0.7, 0.35, 0.9, 0.65 | HornDrivenSoulNumber |
| Family Gospel | 0.5, 0.35, 0.95, 0.7 | SpiritualShout |
| Family Country | 0.45, 0.4, 0.9, 0.7 | CountryTwoBeat |
| Family Folk | 0.4, 0.65, 0.9, 0.7 | VerseDrivenSong |
| Family Jazz | 0.35, 0.85, 0.7, 0.85 | Swinger |
| Family Blues | 0.65, 0.3, 0.9, 0.75 | SlowBlues |
| Family Classical | 0.2, 0.95, 0.8, 0.85 | ReverieMoodPiece |
| Family Latin | 0.45, 0.5, 0.7, 0.6 | DanceNumber |
| Family Caribbean | 0.5, 0.35, 0.75, 0.55 | DanceNumber |
| Family NonMusic | 0.3, 0.45, 0.35, 0.55 | SpokenWord |
| Genre TraditionalPop | 0.2, 0.85, 0.75, 0.85 | LushStandard |
| Genre EasyListening | 0.15, 0.8, 0.7, 0.8 | ReverieMoodPiece |
| Genre GarageRock | 0.85, 0.15, 0.7, 0.35 | StomperRocker |
| Genre TeenPop | 0.2, 0.4, 0.55, 0.25 | BrightPopNumber |
| Genre BaroquePop | 0.2, 0.85, 0.75, 0.6 | ChamberBaroquePiece |

Additional modifier tables and exact perception settings are included verbatim below. Spec B governs taxonomy; typed/legacy tag mappings follow the compatibility adapter and Spec C (no lifecycle/economy). Spec A perception plus the authorized UI report supplies the uncertainty/gate/standing proposal provenance.

```json
{
  "lyricModifiers": {
    "RomanticAddress": [
      0,
      0,
      0,
      0,
      0,
      0,
      -0.05,
      0,
      0.05,
      0
    ],
    "DemandBoast": [
      0.05,
      0,
      0,
      0,
      0,
      0,
      0.15,
      0,
      0.05,
      0.05
    ],
    "InvectiveAccusation": [
      0,
      0,
      0,
      0,
      0.05,
      0,
      0.15,
      0,
      0,
      0
    ],
    "Narrative": [
      0,
      0,
      0,
      0,
      0.1,
      0,
      0,
      0.05,
      0,
      0.05
    ],
    "Confessional": [
      0,
      0.05,
      0,
      0,
      0,
      0,
      0,
      0,
      0.1,
      0.05
    ],
    "TopicalTestimony": [
      0,
      0,
      0,
      0,
      0.1,
      0,
      0,
      0.05,
      0.05,
      0.05
    ],
    "SurrealCatalogue": [
      0,
      0,
      0,
      0,
      0.1,
      0,
      0,
      0.05,
      -0.1,
      0
    ],
    "Nonsense": [
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      -0.2,
      0
    ],
    "CallAndResponse": [
      0,
      0,
      0,
      0.1,
      0,
      0,
      0,
      0,
      0,
      0
    ]
  },
  "vocalModifiers": {
    "Crooned": [
      -0.1,
      0.1,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
    ],
    "Conversational": [
      -0.05,
      0.05,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
    ],
    "Belted": [
      0.12,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
    ],
    "Testifying": [
      0.1,
      0.1,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
    ],
    "Shouted": [
      0.1,
      -0.1,
      0,
      0,
      0,
      0,
      0.05,
      0,
      0,
      0
    ],
    "HarmonyBlend": [
      0,
      0,
      0,
      0.15,
      0,
      0,
      0,
      0,
      0,
      0
    ],
    "Spoken": [
      -0.15,
      -0.1,
      0,
      0,
      0.1,
      0,
      0,
      0,
      0,
      0
    ],
    "Deadpan": [
      -0.1,
      -0.1,
      0,
      0,
      0,
      0,
      0,
      0,
      -0.1,
      0
    ]
  }
}
```

```json
{
  "version": "polar-perception-v1",
  "provenance": "Spec A perception proposal, expanded to persist observer/event/version reads. Visual uncertainty proposals under the authorized UI slice; no recording/economy tuning.",
  "demandErrorWeak": 0.42,
  "demandErrorStrong": 0.07,
  "identityErrorWeak": 0.3,
  "identityErrorStrong": 0.04,
  "marketErrorWeak": 0.36,
  "marketErrorStrong": 0.14,
  "bandWidth": 0.8,
  "arrangementDescriptionThreshold": 0.04,
  "gateScale": {
    "FirstListen": 1,
    "FollowUp": 0.7,
    "Demo": 0.8,
    "Rehearsal": 0.5,
    "Playback": 0.16
  },
  "standing": {
    "Unsigned": 0.15,
    "NewSigning": 0.15,
    "Rising": 0.3,
    "Established": 0.55,
    "Star": 0.8,
    "Superstar": 1,
    "Declining": 0.55,
    "Dropped": 0.15,
    "Disbanded": 0.15,
    "Retired": 0.55
  },
  "maximumObservations": 4096
}
```
