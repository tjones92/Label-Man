# Directive 5 - Genre Taxonomy, Tags, and Emergent Markets

## 0. Status and implementation posture

This directive authorizes the next major market-model revision after Directive 4C/4C-R. It replaces the rough Directive 5 and genre-metadata drafts with one binding implementation contract.

Directive 5 is intentionally staged. Do not attempt the taxonomy, regional segment model, momentum system, tags, soundtrack economics, and calibration in one undifferentiated pass. Each phase must build, produce its required audit evidence, and preserve an exact disabled control before the next phase begins.

The active control is the accepted post-4C Baseline v2 described in `BASELINE-V2.md`. Add a shipping toggle, `genreMarketV2Enabled`, with command-line overrides `--enable-genre-market-v2` and `--disable-genre-market-v2`. Reject both flags together. Resolve the override before population generation and prewarming. Keep the scene default disabled until every checkpoint and the fresh holdout pass.

When disabled, Directive 5 must not alter generated artists, labels, records, formats, RNG calls or order, prewarming, Zeitgeist values, regional acceptance, sales, radio, charts, retirement, costs, revenue, or existing CSV values. The frozen seed-1001 hashes and the accepted 4C configuration must remain byte-exact. New telemetry columns may be written only to new Directive 5 streams in disabled mode; do not alter frozen streams merely to report the feature.

The enabled treatment begins on the first live tick. Unless a phase explicitly says otherwise, prewarm remains on the legacy path. A later phase may authorize a separately validated canonical prewarm, but enabled and disabled comparisons must never mix two undocumented starting-state policies.

## 1. Objective

Build one coherent system in which:

1. every record has a stable musical identity: one primary genre, zero or more secondary genres, and zero or more functional tags;
2. every genre has complete, data-driven historical metadata rather than scattered switches and fallbacks;
3. the unattended market follows a recognizable 1960-1969 trajectory;
4. player and AI successes can bend that trajectory locally and nationally without creating runaway total demand;
5. related genres and overlapping audiences respond more strongly than unrelated markets;
6. regional demographics and media infrastructure route reception through explicit audience segments;
7. genre orientation can distinguish 45-driven Bubblegum from LP-driven Progressive Rock without replacing the accepted decade-wide Single-to-Album transition;
8. seasonal, novelty, instrumental, scene, and production tags have single, auditable application seams;
9. film soundtracks, stage-cast albums, comedy, children's records, and classical records exist at an appropriate level of abstraction; and
10. the system is inspectable enough that a surprising outcome can be explained as baseline, region, segment, Zeitgeist, momentum, tag, format, or product-subtype influence rather than an opaque multiplier pile.

This is an emergence system, not an alternate-history guarantee. The player may release Acid Rock in 1962, but the game must not pretend that the surrounding radio, retail, and audience infrastructure is already ready for it. Early investment can improve later conditions; it cannot bypass every constraint immediately.

## 2. Binding design decisions

### 2.1 Genre, tag, and product are separate axes

- **Genre** describes the record's musical market identity.
- **Tag** describes cross-cutting content, scene, production, mood, origin, or commercial behavior.
- **Release format** remains the physical/commercial format already modeled: `Single`, `EP`, or `Album`. Do not replace `Album` with `LP`, `Compilation`, or `Soundtrack` in `ReleaseFormat`.
- **Album subtype** describes what kind of Album it is. Preserve the existing Album pipeline and evolve `AlbumFormat` rather than creating parallel sales engines.

`FilmSoundtrack` and `StageCast` are Album subtypes, not genres. A soundtrack may contain several genres and derives its musical acceptance from its tracks. `Comedy` and `Childrens` remain genres in the `NonMusic` family because they need distinct audience, decay, and format behavior even when the audio is partly spoken-word.

### 2.2 No `International` family

Do not implement the draft's `International` family. A family is an adjacency default, and placing British Pop, British Blues, Ska, and Reggae in one family would create false spillover. Use:

- `BritishPop` in `Pop`;
- `BritishBeat` and `BritishBlues` in `Rock`;
- `Ska`, `Rocksteady`, and `Reggae` in `Caribbean`;
- origin/scene tags such as `British`, `Merseybeat`, and `Jamaican` for cross-cutting identity.

### 2.3 Motown and Girl Group are tags; Proto-Punk is a genre

`Motown` is a production/scene identity that can qualify Soul, R&B, Pop, or Funk records. `GirlGroup` is a performance/scene identity that can qualify Teen Pop, Soul, R&B, or Doo-Wop. Neither should consume the only primary-genre slot.

`ProtoPunk` remains a genre. It has a distinct late-decade audience route, adjacency, format behavior, and emergence curve; reducing it to a dormant tag would discard useful market behavior.

`Rockabilly` and `Skiffle` are style tags. `SkaRocksteady` is retired and split into three genres.

### 2.4 Genre format orientation is required but subordinate

Keep the accepted decade-wide audience-aging and Album-demand system as the primary era transition. Add a genre `singleOrientation` value because cohort logic alone cannot make Bubblegum strongly 45-oriented while Progressive Rock is strongly LP-oriented in the same year.

`singleOrientation` is a **relative suitability**, not a second independent market-size curve: `0` is strongly Album-oriented, `0.5` is neutral, and `1` is strongly Single-oriented. Convert it into bounded, centered format multipliers at the existing demand and AI-prior seams. Normalize or center the effect so the field reallocates opportunity between formats rather than simply granting high-orientation genres more total demand. Do not delete `GetAlbumDemandEraProgress`, audience aging, price willingness, substitution, compilation logic, or the accepted Album crossover calibration.

### 2.5 Church data models infrastructure, not morality

Add one regional `churchNetworkStrength` value, representing congregational distribution, gospel venues, local radio/programming, community retail, and touring-circuit reach. Use it for the Gospel/Church segment and Gospel discovery.

Do not infer religious belief, race, morality, or likely offense from `churchNetworkStrength`. A future controversy event such as the Lennon remark should combine a separately authored event/content flag with existing `regionalInsularity`, `culturalProgressivism`, audience segment exposure, and perhaps a future explicit values field. Directive 5 does not add denominational simulation or a generic religious-backlash multiplier.

## 3. Canonical identity and migration contract

### 3.1 Stable identity

Every canonical genre and tag must have a stable lowercase string ID used by metadata, telemetry, save migration, and tests. C# enums may remain for type safety and switch exhaustiveness, but enum ordinal is not a durable identity.

Before changing `Genre`, inventory every serialized enum occurrence in `.tscn`, `.tres`, resources, saves, audit fixtures, label templates, artist generation, naming, and telemetry. Assign explicit values to legacy enum members before inserting new ones. Do not reorder a serialized enum and hope Godot maps it by name.

Create one canonical catalog, preferably a validated data asset loaded into an immutable `GenreCatalog`. It is the sole owner of family membership, emergence/death semantics, baseline keyframes, audience lean, format orientation, segment weights, adjacency, decay, and Zeitgeist affinities. Code switches may consume the catalog; they may not duplicate its constants.

### 3.2 Legacy mapping

The compatibility reader must apply these deterministic mappings:

| Legacy value | Canonical result |
|---|---|
| `Psychedelic` | primary `PsychedelicRock` |
| `BritishInvasion` | primary `BritishBeat`, tag `British` |
| `Motown` | primary `Soul`, tag `Motown` |
| `GirlGroup` | primary `Soul` when the old secondary genre is Soul/R&B; otherwise `TeenPop`; add tag `GirlGroup` |
| `Skiffle` | primary `Folk`, tags `Skiffle` and `British` |
| `SkaRocksteady` | `Ska` for release years through 1965, `Rocksteady` for 1966-67, `Reggae` for 1968 onward; use `Ska` if no date exists; add tag `Jamaican` |

Existing canonical values retain their identity. Transitional obsolete enum aliases may exist behind the disabled path for one migration cycle, but enabled generation, metadata validation, and telemetry must emit only canonical IDs.

Migration must be idempotent. Save a schema version and test `legacy -> canonical -> serialize -> reload` without further change. Preserve release dates, chart history, artist identity, label preferences, Album track references, and financial history.

## 4. Canonical taxonomy

Implement these families and genres:

| Family | Genres |
|---|---|
| `Pop` | `TraditionalPop`, `TeenPop`, `BaroquePop`, `SunshinePop`, `Bubblegum`, `EasyListening`, `BritishPop` |
| `Rock` | `RockAndRoll`, `SurfRock`, `GarageRock`, `PsychedelicRock`, `AcidRock`, `HardRock`, `ProtoMetal`, `ProgressiveRock`, `BluesRock`, `ProtoPunk`, `BritishBeat`, `BritishBlues` |
| `RhythmAndSoul` | `RnB`, `Soul`, `Funk`, `DooWop` |
| `Gospel` | `Gospel` |
| `Country` | `Country`, `CountryRock` |
| `Folk` | `Folk`, `FolkRock`, `ContemporaryFolk`, `SingerSongwriter` |
| `Jazz` | `Jazz`, `BossaNova` |
| `Blues` | `Blues` |
| `Classical` | `Classical` |
| `Latin` | `Boogaloo`, `TexMex`, `LatinPop` |
| `Caribbean` | `Ska`, `Rocksteady`, `Reggae` |
| `NonMusic` | `Comedy`, `Childrens` |

`Family` is a default adjacency signal, not a guarantee that every member is equally related. Explicit edges override the family default. Classical, Comedy, and Childrens do not participate in ordinary cross-family trend cascades unless an explicit tag or edge says otherwise.

### 4.1 Initial lifecycle, audience, and format priors

These values complete the rough metadata draft. `Death` is nullable and may fall after the playable decade. The orientation column is the initial 1960-era or emergence-era value; profiles marked as becoming more Album-capable need the small keyframe shift described in section 7.

| Genre | Emerge | Death | Audience lean | Single orientation |
|---|---:|---:|---:|---:|
| TraditionalPop | 1950 | 1971 | .15 | .45 |
| TeenPop | 1957 | 1965 | .90 | .90 |
| BaroquePop | 1966 | 1970 | .60 | .50 |
| SunshinePop | 1965 | 1971 | .65 | .55 |
| Bubblegum | 1967 | 1971 | .95 | .90 |
| EasyListening | 1950 | - | .15 | .35 |
| BritishPop | 1964 | 1968 | .90 | .80 |
| RockAndRoll | 1955 | - | .85 | .85 |
| SurfRock | 1961 | 1966 | .90 | .80 |
| GarageRock | 1963 | 1968 | .90 | .85 |
| PsychedelicRock | 1966 | 1971 | .85 | .45 |
| AcidRock | 1966 | 1971 | .85 | .40 |
| HardRock | 1967 | - | .85 | .40 |
| ProtoMetal | 1968 | - | .85 | .40 |
| ProgressiveRock | 1968 | - | .80 | .25 |
| BluesRock | 1966 | - | .80 | .45 |
| ProtoPunk | 1967 | - | .85 | .40 |
| BritishBeat | 1963 | 1967 | .90 | .75 |
| BritishBlues | 1964 | - | .85 | .65 |
| RnB | 1949 | 1965 | .70 | .80 |
| Soul | 1961 | - | .75 | .70 |
| Funk | 1967 | - | .80 | .70 |
| DooWop | 1954 | 1965 | .80 | .85 |
| Gospel | 1950 | - | .50 | .50 |
| Country | 1950 | - | .40 | .65 |
| CountryRock | 1968 | - | .70 | .40 |
| Folk | 1958 | 1966 | .60 | .50 |
| FolkRock | 1965 | - | .80 | .55 |
| ContemporaryFolk | 1961 | 1969 | .60 | .50 |
| SingerSongwriter | 1967 | - | .65 | .35 |
| Jazz | 1945 | - | .35 | .30 |
| BossaNova | 1962 | 1967 | .40 | .45 |
| Blues | 1945 | - | .45 | .50 |
| Classical | 1945 | - | .20 | .15 |
| Boogaloo | 1966 | 1969 | .70 | .70 |
| TexMex | 1959 | - | .65 | .75 |
| LatinPop | 1958 | - | .55 | .60 |
| Ska | 1964 | 1967 | .60 | .80 |
| Rocksteady | 1966 | 1968 | .60 | .80 |
| Reggae | 1968 | - | .65 | .80 |
| Comedy | 1955 | - | .50 | .15 |
| Childrens | 1950 | - | .50 | .30 |

## 5. Genre profile schema

Each genre profile must contain at least:

```text
id                         stable string
displayName                localized/display string key
family                     GenreFamily
emergenceYear              float
deathYear                  nullable float
preEmergenceFloor          float, normally .01-.02
baselineKeyframes          complete 1960/62/64/66/67/68/69 values
audienceLeanKeyframes      0 adult .. 1 youth
singleOrientationKeyframes 0 Album .. .5 neutral .. 1 Single
segmentWeights             normalized weights
adjacency                  explicit weighted genre edges
regionalAffinities         sparse regional modifiers
momentumHalfLifeWeeks      positive
fatigueSensitivity         nonnegative
zeitgeistAffinities        signed coefficients by field
nicheStableCatalog         bool
chartEligible              bool
nameGenerationGroup        lightweight naming key
```

`deathYear` means culturally exhausted relative to its earlier market, not nonexistent or permanently banned. It must not force acceptance to zero. The state distinctions are `PreEmergent`, `Emerging`, `Established`, `Declining`, and `Legacy`. Preserve them in telemetry because a low pre-emergence baseline and a low legacy baseline have different meaning and future revival potential.

Every profile must specify every required field. Missing profiles, missing keyframes, invalid segment sums, asymmetric required edges, invalid years, non-finite values, or duplicate IDs fail fast at startup in debug/audit builds. There is no global `0.3` fallback and no silent `0.5` acceptance fallback on the enabled path.

## 6. Historical baseline (gravity)

### 6.1 Evaluation

Evaluate historical acceptance continuously using the simulation date, not only an integer year. Linear interpolation between the canonical keyframes is acceptable for Directive 5. Clamp dates before 1960 to the 1960 endpoint and after 1969 to the 1969 endpoint; do not extrapolate an unbounded slope.

For `date < emergenceYear`, evaluate the authored baseline curve. Most genres remain at their `preEmergenceFloor`; some have an intentional seed-scene shoulder before commercial viability, visible in the table below. Never replace an authored pre-emergence value with a global default, and never allow it to exceed the authored curve merely because the genre is selectable. For a genre with a `deathYear`, continue the authored curve through 1969 and classify it as `Legacy` after death; do not replace it with the pre-emergence floor. This distinction fixes both the current phantom-0.3 collapse and the draft's nascent-versus-dead requirement.

The following values are the initial canonical design priors. They are subject only to the calibration authority in section 17; implementation must not casually substitute the current incomplete Zeitgeist rows.

| Genre | 60 | 62 | 64 | 66 | 67 | 68 | 69 |
|---|---:|---:|---:|---:|---:|---:|---:|
| TraditionalPop | .90 | .75 | .50 | .40 | .35 | .32 | .30 |
| TeenPop | .70 | .75 | .50 | .35 | .30 | .28 | .25 |
| BaroquePop | .02 | .02 | .10 | .50 | .65 | .55 | .45 |
| SunshinePop | .02 | .05 | .20 | .55 | .70 | .60 | .50 |
| Bubblegum | .01 | .02 | .05 | .20 | .40 | .65 | .60 |
| EasyListening | .80 | .70 | .55 | .60 | .60 | .55 | .55 |
| BritishPop | .01 | .02 | .80 | .75 | .55 | .40 | .30 |
| RockAndRoll | .60 | .65 | .50 | .40 | .35 | .35 | .35 |
| SurfRock | .05 | .60 | .65 | .40 | .30 | .25 | .20 |
| GarageRock | .10 | .20 | .50 | .65 | .55 | .40 | .30 |
| PsychedelicRock | .02 | .02 | .10 | .50 | .85 | .75 | .65 |
| AcidRock | .02 | .02 | .05 | .30 | .65 | .70 | .65 |
| HardRock | .01 | .02 | .05 | .15 | .30 | .50 | .65 |
| ProtoMetal | .01 | .01 | .02 | .05 | .10 | .20 | .35 |
| ProgressiveRock | .01 | .01 | .02 | .05 | .10 | .25 | .40 |
| BluesRock | .02 | .05 | .15 | .45 | .60 | .70 | .70 |
| ProtoPunk | .01 | .01 | .02 | .05 | .15 | .25 | .30 |
| BritishBeat | .01 | .02 | .95 | .70 | .50 | .40 | .35 |
| BritishBlues | .01 | .02 | .60 | .70 | .65 | .60 | .55 |
| RnB | .40 | .50 | .55 | .45 | .40 | .35 | .30 |
| Soul | .20 | .55 | .75 | .85 | .90 | .90 | .90 |
| Funk | .02 | .05 | .10 | .25 | .40 | .55 | .70 |
| DooWop | .75 | .50 | .20 | .10 | .05 | .03 | .02 |
| Gospel | .35 | .35 | .35 | .40 | .45 | .45 | .50 |
| Country | .65 | .60 | .55 | .55 | .55 | .55 | .60 |
| CountryRock | .01 | .02 | .05 | .10 | .20 | .40 | .55 |
| Folk | .40 | .50 | .60 | .45 | .35 | .30 | .30 |
| FolkRock | .02 | .02 | .10 | .75 | .70 | .60 | .55 |
| ContemporaryFolk | .10 | .40 | .55 | .45 | .40 | .40 | .40 |
| SingerSongwriter | .02 | .05 | .10 | .20 | .30 | .40 | .50 |
| Jazz | .50 | .45 | .40 | .40 | .40 | .40 | .40 |
| BossaNova | .05 | .50 | .55 | .40 | .30 | .25 | .20 |
| Blues | .30 | .30 | .30 | .35 | .40 | .40 | .40 |
| Classical | .30 | .30 | .30 | .30 | .30 | .30 | .30 |
| Boogaloo | .02 | .05 | .10 | .35 | .40 | .35 | .25 |
| TexMex | .15 | .20 | .25 | .30 | .30 | .30 | .30 |
| LatinPop | .20 | .25 | .30 | .35 | .35 | .35 | .35 |
| Ska | .01 | .02 | .05 | .10 | .12 | .10 | .08 |
| Rocksteady | .01 | .01 | .02 | .08 | .12 | .12 | .10 |
| Reggae | .01 | .01 | .02 | .03 | .05 | .10 | .20 |
| Comedy | .40 | .55 | .40 | .35 | .35 | .40 | .40 |
| Childrens | .35 | .35 | .35 | .35 | .35 | .35 | .35 |

The baseline describes national acceptance before regional and segment routing. Country, TexMex, and Boogaloo must therefore receive strong but population-balanced home-region affinities rather than inflated national baselines.

## 7. Audience lean and format orientation

Use the draft's initial audience leans as profile priors: Traditional Pop/Easy Listening `.15`, Classical `.20`, Jazz `.35`, Country/Bossa Nova `.40`, Blues `.45`, Gospel/Comedy/Childrens `.50`, Latin Pop `.55`, Folk/Contemporary Folk/Ska/Rocksteady `.60`, Baroque Pop/TexMex/Reggae/Singer-Songwriter `.60-.65`, Soul `.75`, and the youth Rock/Pop spectrum `.80-.95` as appropriate.

Use the draft's format column as the initial `singleOrientation` prior, including its stated decade shifts. Do not treat an arrow to LP as an instruction to overwrite the general Album-era curve. Store a small keyframe curve where a genre changes materially; otherwise one constant is enough. Required contrasts:

- Bubblegum and Teen Pop remain strongly Single-oriented (`~.90`).
- Doo-Wop, Rock and Roll, Surf Rock, Garage Rock, British Beat/Pop, Ska/Rocksteady/Reggae, and Novelty material lean Single.
- Progressive Rock, Proto-Metal, Hard Rock, Acid Rock, Psychedelic Rock, Blues Rock, Singer-Songwriter, Jazz, Easy Listening, Classical, Comedy, and soundtrack/cast products lean Album by the late decade.
- Country and Traditional Pop begin mixed/Single-capable and become more Album-capable.

At runtime, calculate the accepted era/cohort format demand first, then apply the centered genre tilt. Apply the same convention in actual demand and AI projected net. Never season or tilt only the realized sale while leaving the decision prior neutral. Fixed-input tests must show that changing orientation redistributes Single versus Album opportunity while keeping an approved combined-opportunity probe within `+/-2%`.

## 8. Audience segments and regional routing

### 8.1 Segment set

Create these routing segments inside each of the seven existing regions:

| ID | Function |
|---|---|
| `MainstreamAM` | broad Top-40 reach and ordinary singles radio |
| `Youth` | teen purchasing, youth press, dances, and jukebox exposure |
| `AdultMOR` | adult pop, easy listening, soundtrack, and mature buyers |
| `UrbanRnB` | R&B/Soul market and specialist radio/retail |
| `CountryWestern` | country radio, rural/southern circuits, and western retail |
| `CollegeFolk` | colleges, folk circuit, campus press, and early alternative audiences |
| `UndergroundFM` | album-oriented experimental radio; near-zero before 1967 |
| `JazzHiFiClassical` | jazz, hi-fi, classical, prestige, and specialist Album buyers |
| `GospelChurch` | church/community network and Gospel specialist market |
| `RegionalLatin` | Latin regional audiences and local scenes |
| `FamilyChildrens` | family and children's catalog purchasing |

Segment capacities are deterministic functions of existing region data and media infrastructure plus the new `churchNetworkStrength`. Do not create eleven independent populations and add them together. They are overlapping reception channels over one buying population. Normalize the segment blend at the record-demand seam so a genre with more listed segments does not receive more people merely because its metadata has more rows.

`UndergroundFM` must be effectively unavailable until the region has FM infrastructure and the historical date approaches 1967. `UrbanRnB` crossover into `MainstreamAM` scales with `currentIntegration` and the national `racialIntegration` field. `CollegeFolk` scales with college count per capita, not raw college count alone. `CountryWestern` responds to home-region texture and country-station infrastructure. `GospelChurch` responds to church-network strength and appropriate media/venue infrastructure. `RegionalLatin` is strongest in the Southwest for TexMex and on the East Coast for Boogaloo.

### 8.2 Metadata mapping

Convert the draft five-channel weights as follows:

- `AM` divides between `MainstreamAM` and `Youth` using `audienceLean`;
- `MOR` divides between `AdultMOR`, `JazzHiFiClassical`, and `FamilyChildrens` according to family;
- `RB` maps primarily to `UrbanRnB`, with Gospel share routed to `GospelChurch`;
- `COL` maps to `CollegeFolk`;
- `FM` maps to `UndergroundFM`;
- Country, Latin, Classical, Gospel, and Childrens profiles receive their named specialist segment even where the rough draft omitted it.

Store the final normalized matrix in the catalog and emit it in the audit. Do not leave this conversion as runtime guesswork.

The following five-channel rows are the binding source priors for that conversion. Omitted channels are zero. Rows need not sum to one here; normalize only after splitting them into the eleven canonical segments and adding the specialist channel required by family.

| Genre | Source segment weights |
|---|---|
| TraditionalPop | MOR .60, AM .35 |
| TeenPop | AM .80, COL .05 |
| BaroquePop | AM .50, MOR .30, FM .20 |
| SunshinePop | AM .60, MOR .25, FM .15 |
| Bubblegum | AM .85 |
| EasyListening | MOR .70, AM .25 |
| BritishPop | AM .85 |
| RockAndRoll | AM .75, RB .20 |
| SurfRock | AM .70, COL .10 |
| GarageRock | AM .60, COL .20, FM .10 |
| PsychedelicRock | FM .40, AM .30, COL .25 |
| AcidRock | FM .45, COL .30, AM .20 |
| HardRock | FM .40, AM .30, COL .25 |
| ProtoMetal | FM .50, COL .30, AM .15 |
| ProgressiveRock | FM .55, COL .30, AM .10 |
| BluesRock | FM .40, AM .25, COL .25, RB .10 |
| ProtoPunk | FM .50, COL .40, AM .05 |
| BritishBeat | AM .80, COL .10 |
| BritishBlues | AM .50, FM .25, COL .15, RB .10 |
| RnB | RB .60, AM .30 |
| Soul | RB .50, AM .40, COL .10 |
| Funk | RB .55, AM .30, COL .10, FM .05 |
| DooWop | AM .50, RB .40 |
| Gospel | RB .50, MOR .30, AM .20 |
| Country | AM .40, MOR .40, COL .05 |
| CountryRock | FM .35, COL .30, AM .25 |
| Folk | COL .50, MOR .25, AM .20 |
| FolkRock | AM .40, COL .30, FM .20 |
| ContemporaryFolk | COL .55, MOR .20, AM .20 |
| SingerSongwriter | FM .40, COL .35, MOR .15 |
| Jazz | MOR .50, COL .25, RB .15, FM .10 |
| BossaNova | MOR .55, COL .20, AM .20 |
| Blues | RB .40, COL .25, FM .20, MOR .15 |
| Classical | MOR .70, COL .25 |
| Boogaloo | RB .40, AM .30 |
| TexMex | AM .40, RB .20 |
| LatinPop | AM .40, MOR .30 |
| Ska | RB .40, COL .20 |
| Rocksteady | RB .40, COL .20 |
| Reggae | RB .35, AM .20, COL .20 |
| Comedy | MOR .40, COL .30, AM .20 |
| Childrens | MOR .60 |

For deterministic splitting, use `AM * (0.35 + 0.45 * audienceLean)` for `Youth` and the remaining AM weight for `MainstreamAM`. Route the profile's named specialist share first, then divide residual MOR by family: Pop/Comedy to `AdultMOR`, Jazz/Classical to `JazzHiFiClassical`, Childrens to `FamilyChildrens`, and Gospel to `GospelChurch`. Country receives at least `.35` pre-normalization in `CountryWestern`; Gospel `.40` in `GospelChurch`; Latin `.30` in `RegionalLatin`; Jazz/Classical `.40` in `JazzHiFiClassical`; and Childrens `.60` in `FamilyChildrens`. If the specialist minimum exceeds the source row's residual, reduce the largest general channel before normalization. Materialize the resulting matrix in data so runtime calculations only read validated values.

### 8.3 One regional acceptance owner

The enabled path must consolidate the currently competing national `Zeitgeist.genreAcceptance`, `MarketRegion.currentGenreAcceptance`, `GetYearEvolution`, genre-preference fallback, national momentum, regional momentum, segregation factor, and hard-coded radio genre classifiers behind one pure acceptance service. Consumers pass explicit date, region, segment context, genre mix, and tags.

The service must return an explanation object in audit/debug builds containing baseline, lifecycle state, Zeitgeist factor, regional affinity, segment reach, momentum shock, emergence advance, tag modifiers, and final clamped value. Production code may use a cheaper scalar path if tests prove both paths equivalent.

## 9. Zeitgeist fields

Retain the six national fields: `youthInfluence`, `counterCultureStrength`, `racialIntegration`, `britishInfluence`, `experimentalism`, and `politicalAwareness`. Give each genre and relevant tag a sparse signed affinity to those fields.

Apply them as bounded modifiers to compatible segments, not as a second global genre baseline:

- youth influence affects high-lean material in `Youth` and `MainstreamAM`;
- counterculture affects `CollegeFolk`, `UndergroundFM`, `Protest`, and compatible Rock/Folk genres;
- racial integration affects crossover from `UrbanRnB` into mainstream/adult channels, not demand within the core R&B market;
- British influence affects British genres and the `British` tag;
- experimentalism affects Psychedelic, Acid, Progressive, Proto-Punk, Early Electronic, and `Experimental` material primarily through college/FM routes;
- political awareness affects `Protest`, `Topical`, Folk, Singer-Songwriter, and topical Comedy reception, with controversy still able to create negative regional response.

Historical field curves remain the gravity path. Add an endogenous delta layer only in Phase 3. Successful culturally relevant records may nudge a compatible field, but each delta is bounded, decays toward zero, and is much slower and smaller than genre momentum. No single hit may move a field by more than `0.01`; the total endogenous delta per field is capped initially at `+/-0.15`. Player and AI records use the same rule. Do not let raw release volume move the national culture; require meaningful chart or regional-breakout evidence.

## 10. Emergent market model

### 10.1 State and evaluation

Store market trend state by genre, region, and segment. A single global dictionary is insufficient. The canonical conceptual evaluation is:

```text
baseline = HistoricalBaseline(genre, effectiveDate)
contextual = ApplyZeitgeistRegionAndSegment(baseline, genre, region, segment)
effective = Logistic(Logit(ClampForOdds(contextual)) + preferenceShock[genre,region,segment])
```

Blend segment results using the record's normalized segment reach, then blend primary and secondary genres using explicit weights. Start with primary `0.80`; divide the remaining `0.20` across unique secondary genres. Tags modify routing or the final authorized seam; they do not masquerade as secondary genres.

Use odds/logit or another documented bounded representation so low-baseline genres can be moved without simple addition immediately hitting 0 or 1. Clamp only at defined domain boundaries. No NaN, infinity, or silent fallback is permitted.

### 10.2 Hit evidence

Generate a trend impulse from normalized evidence, not raw sales alone. The impulse should combine:

- regional chart position or national peak translated to a bounded score;
- weeks sustained or bullet/growth evidence;
- regional sales relative to that region and format's market, not absolute national units;
- regional breakout stage where applicable; and
- a small quality/novelty credibility term only after audience response exists.

A release that was merely stocked or heavily marketed does not create a scene. A strong local record may create local momentum before it charts nationally. Albums and Singles both contribute, but use format-appropriate evidence. Prewarm, synthetic probes, manual debug injections, and player-authored direct radio additions do not create impulses unless explicitly run through an audit-only test API.

### 10.3 Decay, saturation, and fatigue

Decay shocks exponentially toward zero using genre/profile half-life in weeks. Novelty and Topical Comedy decay fastest; ordinary Singles are moderate; Album-oriented and scene-building genres decay more slowly. Decay must be calendar/tick based and must not consume RNG.

Apply diminishing returns before adding repeated impulses. Use a saturating function of existing positive shock and recent hit count so the fifth similar hit contributes less than the first. Track short-run trend fatigue separately from long-run baseline. Fatigue may reduce the incremental impulse or temporarily tax the same genre, but must decay and must never rewrite historical metadata.

### 10.4 Adjacency and roughly zero-sum redistribution

Build a symmetric weighted adjacency graph from:

1. a modest same-family default;
2. explicit profile edges; and
3. shared-segment overlap.

Required strong bridges include R&B-Soul-Doo-Wop, Soul-Gospel-Funk, Blues-Blues Rock-British Blues, Folk-Folk Rock-Singer-Songwriter, Country-Country Rock-Folk, Rock and Roll-Surf-Garage-British Beat, Garage-Proto-Punk, Psychedelic-Acid-Progressive, Sunshine-Baroque-Folk Rock, Ska-Rocksteady-Reggae, and Boogaloo-Soul-Funk-Latin Pop. `Rockabilly` adds a temporary Country/Rock bridge for tagged records.

For each impulse in a region/segment:

- allocate the main positive share to the primary genre;
- allocate smaller positive spillover to adjacent genres according to normalized edge and segment overlap;
- fund at least `80%` of the total positive preference impulse by negative pressure on plausible competitors in the same segment;
- choose donors by current baseline share, segment overlap, and inverse adjacency, not uniformly across all genres;
- exclude unrelated niche/stable markets unless they share material segment reach.

The weighted net impulse per region/segment must be approximately zero within float tolerance for the redistributed share. The remaining maximum `20%` represents bounded category expansion and is a calibration ceiling, not an automatic grant. Classical, Comedy, Childrens, and Gospel are not generic donor pools for a Rock trend.

### 10.5 Ahead of its time

Early releases are legal for player and AI, though AI exploration probability should be small and personality/label dependent. A pre-emergent record uses the low baseline and limited available segments, so success is difficult but not impossible.

Sustained positive shock in compatible regions creates `emergenceAdvanceWeeks`. Evaluate the historical curve at `date + emergenceAdvanceWeeks` for that genre only, capped initially at three years and requiring evidence across at least two regions or one region plus a national chart result. Advance grows slowly, decays more slowly than ordinary momentum, and can never push a genre below its pre-emergence floor or past its authored 1969 endpoint.

This makes early scene-building viable without allowing one lucky local week to invent a national infrastructure. Report original emergence date, effective emergence date, cause records, regions, and decay.

## 11. Tag system

### 11.1 Storage and validation

Records and Album tracks may carry any number of unique tags. Store stable IDs in deterministic sorted order for serialization and telemetry. Provide `HasTag`, category query, and catalog lookup APIs. Unknown tags fail validation in authored data and are preserved-but-inactive with a warning when encountered in forward-version saves.

Initial tag catalog:

| Category | Tags | Directive 5 behavior |
|---|---|---|
| Seasonal | `Christmas`, `Halloween`, `Summer`, `Romantic` | active seasonal sales/radio window |
| Commercial | `Novelty`, `Instrumental`, `Topical` | active format/radio/decay/routing behavior |
| Scene/style | `British`, `Merseybeat`, `Motown`, `GirlGroup`, `Rockabilly`, `Skiffle`, `Jamaican`, `EarlyElectronic`, `Experimental` | active adjacency, segment, and Zeitgeist routing where specified |
| Production | `WallOfSound`, `Orchestral`, `HornSection`, `LoFi` | storage/query now; bounded recording-cost modifier where specified below |
| Descriptive/mood | `FemaleVocalist`, `Protest`, `Longing`, `Energetic` | storage/query; `Protest` routes Zeitgeist, others remain dormant |

Do not add a tag solely because a title generator can mention it. Do not allow contradictory seasonal tags on generated content without an explicit authored exception. Secondary genres remain separate from tags.

### 11.2 Seasonal tags and Directive 4C

Directive 5 supersedes only Directive 4C's limitation that there was no holiday-content classification. Preserve 4C's generic national sales and radio opportunity curves unchanged. Add one record-specific seasonal factor after the generic format seasonality factor and before final demand, and one record-specific radio-programming factor at the ordinary radio-opportunity seam. Do not apply the tag again to awareness, chart points, artist heat, or final sales.

Use smooth month windows rather than a single on/off day. Initial design intent:

- Christmas: strong November-December lift, mild October ramp, material January-September penalty;
- Halloween: strong October lift, small late-September ramp, strong off-season penalty;
- Summer: June-August lift, shoulder in May/September, mild winter penalty;
- Romantic: February lift and small late-January ramp, mild rather than punitive off-season behavior.

Normalize tag opportunity against an appropriate annual calendar only for continuously available catalog records. New releases deliberately choose timing and therefore need not receive an ex-ante annual conservation refund. Cap stacked seasonal factors; `Halloween + Novelty` may be powerful but may not multiply without bound.

Seasonal catalog records may re-enter ordinary eligibility during their window. Implement a bounded seasonal-catalog pool rather than keeping every retired record fully active all year. Re-entry requires prior meaningful performance or catalog traction, uses existing chart rules, preserves lifetime history, and does not create a new Record or production expense. A re-entry can refresh tag-specific attention but must not repeatedly grant a full first-hit genre impulse every year.

### 11.3 Commercial tags

- `Novelty`: strong Single orientation, high dependence on radio/virality, sharp early impulse, strong fatigue, and fast decay. Album contribution is weak unless the Album itself is Comedy/Childrens or a coherent novelty collection.
- `Topical`: fast rise and decay; political-awareness and controversy routing; especially useful for Comedy and Folk.
- `Instrumental`: reduce vocal/cultural-friction penalties and broaden regional travel. Route toward Adult MOR/Jazz/Hi-Fi for Easy Listening, Jazz, Classical, and Bossa Nova; route toward Youth/Mainstream for danceable Rock/R&B/Surf material. It is not a universal acceptance bonus.

### 11.4 Production tags

Only apply costs that represent work actually commissioned. `Orchestral`, `WallOfSound`, and `HornSection` may increase recording cost through one centralized production-cost seam; `LoFi` may reduce cost but cap production-quality benefit. Do not alter historical sunk costs, pressing COGS, packaging, or marketing. Exact multipliers are Phase 4 calibration constants and must be logged.

## 12. AI generation

### 12.2 AI behavior

Replace hard-coded genre group switches gradually with catalog queries. AI genre choice should consider label specialties, artist identity, current effective acceptance, regional home strength, format economics, recent fatigue, and a small bounded exploration term. Do not let AI labels instantly chase the highest national genre and erase archetype identity.

AI format choice must use the same centered format orientation and segment demand as realized economics. AI tag generation must be genre-compatible and era-aware, but pre-emergent exploration remains possible for innovative artists/labels. New choices may use new RNG only on the enabled path; record every new draw site and keep ordering deterministic.

### 12.3 Naming

Add only lightweight vocabulary groups required to avoid obviously wrong names for the new genres, Film Soundtracks, Stage Cast albums, Comedy, and Childrens releases. Do not begin the Directive 6 naming/database overhaul. Fallback naming must remain deterministic and nonempty.

## 13. Special products and niche markets

### 13.1 Album subtype model

Evolve `AlbumFormat` to distinguish at least `Standard`, `Compilation`, `Concept`, `Live`, `FilmSoundtrack`, and `StageCast`. Preserve `EP` on `ReleaseFormat`; do not also represent it as an Album subtype. Migrate the current generic `Soundtrack` value to `FilmSoundtrack`.

Film Soundtracks and Stage Cast albums derive musical acceptance from a weighted track-genre mixture. If track data is incomplete, use the Album's declared primary/secondary genre mixture and flag the fallback in telemetry.

### 13.2 Abstract external-media profile

Do not build a film or Broadway industry. When an AI or player-authorized project is commissioned, create one immutable `ExternalMediaProfile` with bounded values for:

```text
sourcePopularity
castOrStarDraw
studioPromotion
distributionReach
criticalPrestige
familyAppeal
catalogLongevity
tieInSingleStrength
```

Film Soundtracks weight source popularity, distribution, promotion, and tie-in strength more heavily. Stage Cast albums weight prestige, adult/urban reach, and catalog longevity more heavily. These values affect launch awareness, relevant segment reach, decay/catalog tail, and tie-in behavior exactly once. They do not directly guarantee chart position.

Generate correlated profiles so eight independent high rolls cannot create routine super-products. Calibrate the whole ecosystem to produce roughly zero to three genuine soundtrack/cast blockbusters per decade run, with many ordinary or failed releases. Report the distribution; do not hard-code two winners.

### 13.3 Comedy, Childrens, and Classical

- Comedy is Album-oriented. `Topical` Comedy spikes and decays quickly; evergreen observational Comedy has a longer tail. Musical Comedy may have a secondary musical genre.
- Childrens is low-volatility, Family-segment, catalog-oriented, and holiday-capable. It is chart eligible but should rarely dominate the general chart.
- Classical is low-volume, Album-oriented, stable catalog, and prestige-adjacent. Full reputation/prestige rewards remain deferred; do not compensate by inflating sales.

These markets participate in revenue, stock, catalog, and chart systems. They are excluded from ordinary trend-donor redistribution unless their segment overlap or tags justify it.

## 14. Implementation phases

### Phase 0 - Inventory, toggle, and observation

1. Add the toggle/CLI contract and Directive 5 telemetry shell with no enabled behavior.
2. Inventory enum serialization, every genre switch, duplicate acceptance path, format switch, and name formatter.
3. Add fixed-input probes for current baseline, regional acceptance, genre momentum, format demand, and tag-free seasonality.
4. Prove disabled seed-1001 byte identity and enabled-no-op determinism.

Stop if the exact-off boundary is not clean.

### Phase 1 - Canonical catalog, taxonomy, and migration

1. Add stable IDs, families, profiles, validation, explicit enum values, and legacy mapping.
2. Replace the incomplete baseline with the full catalog only on the enabled path.
3. Update generation, labels, artists, records, Album tracks, and telemetry to canonical IDs.
4. Do not add segments, format tilt, momentum, tags, or special-product economics yet.

Checkpoint: static catalog tests, migration round trips, no 0.3/0.5 fallback, historical curve probes, disabled hashes, enabled determinism.

### Phase 2 - Segments, regions, and format orientation

1. Add deterministic segment capacities and the new church-network field.
2. Consolidate acceptance ownership and remove enabled-path duplicate evolution/fallback logic.
3. Integrate normalized segment routing with regional demand and radio opportunity.
4. Integrate centered format orientation into realized demand and AI priors.

Checkpoint: regional/segment fixed probes, combined format-opportunity conservation, economic regression, and historical genre-arc report. Do not add endogenous momentum until these foundations are stable.

### Phase 3 - Momentum, adjacency, and endogenous Zeitgeist

1. Replace the global positive-only momentum accumulator on the enabled path.
2. Add region/segment shocks, decay, fatigue, adjacency, donor redistribution, and emergence advance.
3. Add bounded slow Zeitgeist deltas.
4. Add explanation telemetry and causal probe APIs.

Checkpoint: impulse conservation, adjacency ordering, decay half-lives, saturation, early-emergence probes, no runaway monoculture, disabled hashes, and deterministic repeats.

### Phase 4 - Tags and seasonal catalog

1. Add tag storage, validation, queries, and generation.
2. Activate Seasonal, Novelty, Instrumental, Topical, relevant scene tags, Protest, and authorized production costs.
3. Add the bounded seasonal-catalog re-entry pool.
4. Preserve 4C single-application seams.

Checkpoint: tag-free records reproduce Phase 3, tag fixed probes show exactly one application, stacked factors respect caps, and catalog re-entry preserves identity/history.

### Phase 5 - Special products and niche markets

1. Add Film Soundtrack and Stage Cast subtype behavior and migration.
2. Add immutable external-media profiles and tie-in integration.
3. Activate Comedy, Childrens, and Classical niche behavior.
4. Add lightweight naming data only.

Checkpoint: subtype accounting reconciles, correlated profile tests pass, blockbuster frequency is plausible, niche markets remain nonzero without taking over the chart, and no phantom external revenue exists.

### Phase 6 - Full calibration and acceptance

Run the sequence in section 17. Do not update `BASELINE-V2.md` or enable the shipping default until the fresh holdout passes.

## 15. Guardrails

- Do not change the seven-region taxonomy, distance calibration, distribution-deal terms, price, pressing cost, royalties, chart sizes/weights, retirement tolerances, release-capacity growth, Album crossover curve, substitution/cannibalization, or 4C raw seasonality tables merely to make Directive 5 pass.
- Do not create demand by summing overlapping audience segments.
- Do not apply genre acceptance once to radio and again directly as an extra sales factor beyond the existing documented chain.
- Do not multiply primary and secondary genre acceptance together. Use a normalized blend.
- Do not let tag modifiers apply at both launch and weekly demand unless the directive explicitly assigns separate effects.
- Do not use record count, label count, or realized release timing to normalize catalog constants.
- Do not consume RNG for decay, interpolation, adjacency, or deterministic segment calculation.
- Do not make player records more capable of changing culture than equivalent AI records.
- Do not retune historical keyframes to force a specific player's alternate history.
- Do not fabricate touring, film box-office revenue, Broadway grosses, awards ceremonies, prestige rewards, church attendance, or moral-opinion simulation.
- Do not start the Directive 6 naming overhaul.

## 16. Required telemetry and audit deliverables

Add new removable streams without modifying frozen ones. At minimum:

### `genre-catalog.csv`

One row per canonical genre containing identity, family, emergence/death, all keyframes, audience lean, format orientation, half-life, fatigue, segment weights, regional affinities, Zeitgeist affinities, and catalog flags.

### `genre-market-weekly.csv`

```text
seed,enabled,year,month,week,region,segment,genre,
baseline,lifecycleState,zeitgeistFactor,regionalFactor,segmentReach,
preShock,decay,positiveImpulse,adjacentImpulse,donorPressure,postShock,
emergenceAdvanceWeeks,effectiveAcceptance,
eligibleRecords,chartedRecords,units,radioPlay
```

### `record-genre-explanation.csv`

One row per sampled/audited record-region-week with primary/secondary weights, tags, segment blend, format tilt, generic seasonality, record-specific seasonal factor, radio factor, final acceptance, and final demand seam. Sampling rules must be deterministic.

### `genre-events.csv`

Impulse source records, evidence components, recipient genres, donor genres, field nudges, fatigue, emergence advance, and catalog re-entry events.

### `special-products.csv`

Subtype, external profile, correlated-profile bucket, costs, promotion, tie-in, units, chart result, catalog tail, and financial reconciliation.

Write `SimTools/GenreMarketV2Audit.md` with:

1. code-path map and exact toggle/prewarm behavior;
2. enum/resource/save migration inventory and round-trip results;
3. canonical catalog and validation output;
4. historical baseline plots/tables for every genre and Zeitgeist field;
5. segment capacity and genre-weight tables by region/year;
6. format-orientation fixed probes and AI/realized-seam parity;
7. momentum conservation, adjacency, decay, fatigue, and emergence probes;
8. player/AI symmetry proof;
9. tag single-application map and seasonal catalog behavior;
10. special-product and niche-market distributions;
11. disabled hash comparison and enabled deterministic repeat;
12. complete calibration log, including failed probes;
13. all measurement-seed and holdout results, not only pooled means;
14. limitations and deferred systems; and
15. final constants, shipping toggle state, commands, output locations, and hashes.

## 17. Validation, calibration authority, and completion

### 17.1 Static and causal gates

All phases must pass:

- complete catalog; unique stable IDs; valid enum mapping; segment sums `1 +/- 1e-6`; no missing consumers;
- baseline keyframe values match section 6 within `1e-6`; interpolation is continuous; dates outside the decade clamp deliberately;
- pre-emergent genres use their authored floor/seed-scene shoulder, never a global fallback, and legacy genres do not revert to the pre-emergence floor;
- disabled seed-1001 frozen hashes remain exact;
- an enabled seed-1001 repeat in an independent process is byte-identical for every emitted stream;
- no NaN, infinity, invalid probability, negative cost, negative stock, or acceptance outside `[0,1]`;
- a fixed hit impulse ranks primary effect above strong neighbor, strong neighbor above weak bridge, and weak bridge above unrelated genre;
- redistributed shock balances within tolerance and never charges unrelated niche markets by default;
- repeated identical hits show diminishing incremental effect; no-hit state decays to gravity at the authored half-life;
- a qualified early scene advances emergence while an isolated weak record does not;
- tag-free records match the prior phase's calculation; each active tag applies exactly once at its authorized seams;
- format orientation changes Single/Album mix while the fixed-input combined opportunity remains within `+/-2%`;
- soundtrack/cast financial rows reconcile to label and market totals.

### 17.2 Historical unattended-market gates

Run at least three 520-week enabled measurement seeds. Without player intervention, the ensemble must show:

- Doo-Wop declining sharply after 1962 and remaining a small legacy market by 1967;
- British Beat/Pop breaking in 1964, not 1960-62;
- Surf Rock cresting in the early/mid decade and fading thereafter;
- Folk/Folk Rock cresting before or around the psychedelic peak;
- Psychedelic Rock peaking around 1967, followed by stronger late Hard Rock/Blues Rock/Proto-Metal/Progressive activity;
- Soul strong through the mid/late decade and Funk rising late;
- Country, Jazz, Easy Listening, Gospel, Blues, Classical, Childrens, and regional Latin markets remaining nonzero rather than collapsing through omitted rows;
- FM-dependent genres remaining constrained before FM emergence;
- the accepted Album crossover window and the binding 1960 format-mix gates from the current baseline remaining satisfied;
- no single canonical genre exceeding `35%` of annual national units, unless a separately reported data artifact proves the denominator excludes relevant formats; do not waive silently;
- seasonal and special-product records not crowding ordinary repertoire out of the annual chart.

These are shape gates, not a demand for the exact historical artist roster.

### 17.3 Regional and economic gates

- Country share must be higher in Deep South/Great Plains/Southwest than the national mean; TexMex highest in Southwest; Boogaloo strongest on East Coast; Gospel responds to Gospel/Church infrastructure; Urban R&B mainstream crossover rises with integration.
- Segment-derived national buying population must reconcile to the original region buying population rather than multiply it.
- Against same-seed disabled controls, each enabled seed's decade total units, gross, label net, and market net must initially remain in `[0.90,1.10]`.
- Three-seed pooled annual total units and market net must remain in `[0.85,1.15]`; any individual seed-year outside `[0.75,1.25]` is a catastrophic fail requiring diagnosis.
- Each format's decade units must remain in `[0.85,1.15]`; the accepted Album crossover and 1960 mix gates are stricter where they overlap.
- Successful releases and scheduled Album projects per seed must remain in `[0.85,1.15]` of disabled.
- Paired all-decade closed Top-40 median may move by at most `+/-2` weeks.
- Preserve accepted distance, concentration, distribution-deal, finance-reconciliation, and 4C seasonality health checks.

If taxonomy splitting makes a legacy per-genre comparison meaningless, aggregate canonical genres through the migration map and report both views.

### 17.4 Calibration ladder and budget

Fix integration errors before calibration. Authorized calibration groups, in order, are:

1. segment-capacity normalization and genre segment weights;
2. centered format-tilt strength;
3. momentum impulse level, redistributed share, half-lives, saturation, and fatigue;
4. bounded Zeitgeist-affinity strength and emergence-advance rate;
5. active tag factors/caps;
6. special-product frequency and external-profile coefficients.

Historical baseline keyframes are design priors and may be changed only when the historical shape gates fail in the unattended market after routing bugs are excluded. Do not tune unrelated finance, chart, region, distance, release, or 4C constants.

For each phase, allow no more than four autonomous two-seed probes before a three-seed checkpoint. Log every attempted constant set and result. After the full three-seed candidate is frozen, select one seed confirmed absent from committed audits, uncommitted work, and `SimLogs`, then run one enabled/disabled 520-week holdout pair exactly once.

A failed holdout is a reported failure. Do not widen bands, consume another seed, or tune after seeing it without a new directive.

### 17.5 Completion condition

Directive 5 is complete when the canonical taxonomy and migrations are stable; every genre has a full baseline and metadata profile; the 0.3-collapse and competing acceptance paths are eliminated on the enabled path; segments route one conserved regional audience; format orientation differentiates genres without replacing the accepted era transition; momentum is local, decaying, saturating, adjacency-aware, and roughly zero-sum; early scene-building is risky but viable; active tags apply once; soundtracks, cast albums, Comedy, Childrens, and Classical behave plausibly at the approved abstraction; disabled mode remains byte-exact; enabled mode is deterministic; all historical, regional, economic, and inherited gates pass; the fresh holdout passes without post-holdout tuning; and `genreMarketV2Enabled` ships enabled with a complete `GenreMarketV2Audit.md`.
