# Polar repertoire, archetype, and tie diagnosis

October 3, 2026. Investigation of the current working tree and the retained 1960–63 censuses. No gameplay, table, perception, source-share, or save-schema changes. No new simulation seed or long run. The attached report and quoted analysis were treated as evidence to check, not instructions to implement remedies.

## Main finding

There are several interacting model shortcomings. Persistent per-song variance addresses duplicated truth profiles, but cannot by itself fix saturated fit scores, missing musical forms, absent repertoire supply, the removal of standards from candidate pools, or genre-independent original-slot allocation. Country's composition collapse is confirmed; attributing all of it to insufficient archetype count is too narrow. Easy Listening has confirmed pool and ordering problems. Several apparently plausible genre percentages conceal almost complete exclusion of contemporary repertoire and severe composition concentration.

## What the user's percentages measure

All eight percentages reproduce, rounded to two decimals, from **seed 1001, enabled polar selection, unsigned acts, all filled live-set slots, January 1960–December 1963**. They are not recording-source percentages, chart shares, or samples of persistent repertoires. The attached report's principal Country/Easy Listening table includes signed rosters too; its figures consequently differ.

| Genre | Standard/traditional share of all filled slots | Songbook share of cover slots | Exact song-ID tie share of cover slots | Top-ten share of cover slots |
|---|---:|---:|---:|---:|
| BossaNova | 77.21% | 100.00% | 81.15% | 71.64% |
| ContemporaryFolk | 78.00% | 99.98% | 94.84% | 95.11% |
| Country | 76.60% | 99.93% | 100.00%* | 92.83% |
| Folk | 75.80% | 100.00% | 94.07% | 86.60% |
| Gospel | 77.88% | 100.00% | 93.34% | 93.87% |
| Jazz | 76.31% | 100.00% | 87.63% | 63.43% |
| RnB | 45.14% | 60.13% | 97.91% | 68.86% |
| SurfRock | 2.31% | 3.00% | not applicable | 6.23% |

*Country's unrounded unsigned tie share is 99.9962%. Surf uses another selection algorithm, so its zero recorded tie count is not evidence of no ties.

Country selects 78,968 songbook slots, 42 recent hits and 17 ordinary covers. Contemporary Folk selects 9,184 traditional slots and **two** ordinary covers. Folk, Gospel, Jazz and Bossa select no non-songbook covers in this seed/window. Seed 1002 shows the same near-total songbook selection in these genres, with somewhat different concentrations. Thus this is a broad selection issue, not a Country-only anomaly.

`PlayerDesk.BuildLiveSet` allocates 0/1/2 placeholder originals at songwriting ability cutoffs .3/.6, regardless of genre, then fills a target of 3–5 songs with covers. An act with one original in a four-song set necessarily has a 75% cover share. When every chosen cover is songbook material, songbook share is the complement of original share. There is no shared 78% cap. This also imposes a maximum of two originals: even an exceptionally strong writer cannot show a wholly original set. A weak writer cannot show newly supplied professional material unless it has entered the cover pool.

Sources: [live construction](../Systems/PlayerDesk.cs), [live selection](../Systems/SongMaterialSelectionService.cs), [retained fixed-world original-slot intervention](PolarFollowupReport.md), and [new extracted measurements](../SimLogs/polar-model-diagnosis.json).

## Country: four failures cooperate

1. **Generated songs do not use the taxonomy that exists.** Standards and recent hits vary craft, hook and adaptability, but supply no explicit archetype, word density, lyric/vocal modes, form, or vocal presence. `EnsureComposition` copies genre/tags. `SongProfileDeriver` therefore clones the primary genre fallback. Both Country standards and recent hits start as `CountryTwoBeat`. Legacy adaptability does not determine polar plasticity: plasticity comes from the archetype and density/form adjustments, then can freeze at the first committed master.
2. **Fit deliberately has broad plateaus.** Capability penalizes unmet demands, adds dominant-axis headroom and clamps to 1. Identity is measured after a continuous pull toward the act. When reach × plasticity covers the identity difference, the song's realized identity equals the act's and Identity becomes 1. Moment then also depends on that same pulled identity, giving identical Moment for those songs in a given act/market. This can erase differences even when truth profiles differ. Small unmet demands can also be hidden by headroom before the clamp.
3. **Live selection has no diversity decision after suitability.** Non-rock ranks the entire eligible pool by `.35 Capability + .45 Identity + .20 Moment`, then exact primary genre, then ascending song ID. Hook, familiarity, age, repertoire history, set balance and act-specific song preference do not choose between suitable songs. The first covers are reused whenever the top group remains tied. Country's IDs 1401–1850 are the seeded standards block; its recent-hit block is generated later. The final ID rule consequently contains a catalogue-generation-order preference. Reversing IDs proves composition dependence; retained counterfactuals do not measure the resulting historical category shares.
4. **Country's musical vocabulary is limited.** The 1960–63 Country-family resolver has four rows: `CountryTwoBeat`, `SlowBallad`, `VerseDrivenSong`, `ProtestMessageSong`. It lacks an explicit country shuffle/honky-tonk shape, country waltz, Western swing arrangement, and Nashville orchestral ballad. A reference row from another family can also be retained, so four is the family-row count rather than an absolute limit on every proposal. Assigning existing useful rows is required even if new rows are added.

The authored Country default demands are `.45/.35/.40/.50/.40/.20`. As an arithmetic illustration, an act with six capabilities .65 and interpretive reach .5 can execute both that shape and a shape with **every demand increased .04**. With unchanged plasticity .65, both pull completely to the act's Country identity. Both yield Capability 1, Identity 1 and, without market evidence, score .9. This is illustrative arithmetic rather than a replay of a measured artist. It establishes why demand variance need not break a tie.

Country's seeded exact pool already has 450 standard compositions plus 120 recent hits. Uniform selection would still favor the standard bucket at about 79% of covers, or roughly 60% of a set with 25% originals—consistent with the disabled census. A per-act deterministic tie order can fix shared low-ID sets, but it cannot establish historically appropriate repertoire shares or diversify musical shapes.

The musical coverage concern is historical as well as numerical: the Museum describes [Buck Owens's Bakersfield road-band sound](https://www.countrymusichalloffame.org/hall-of-fame/buck-owens), while the PBS production describes [Nashville's strings, piano and subdued backing voices](https://www.pbs.org/kenburns/country-music/nashville-sound-branches-of-country-music). These merit distinct combinations of archetypes and modifiers. They need not each become a new gameplay genre.

Source: [derivation](../Systems/SongProfileDeriver.cs), [fit](../Systems/PolarMaterialFit.cs), [act adapter](../Systems/PolarActProfileDeriver.cs), [ranking](../Systems/PolarSongBehavior.cs), [resolver](../Systems/PolarCoverResolver.cs), [catalogue](../Systems/CompositionCatalogService.cs).

## Archetype coverage beyond Country

Static audit: 34 archetypes, 45 canonical genres, but only five explicit genre-prior overrides. A genre need not have its own archetype; sharing a well-chosen musical form is valid. The problem is blanket fallback, insufficient forms/modifiers, and lack of actual assignment.

| Context | Primary fallback | Family rows eligible in 1960–63 | Modeling gap |
|---|---|---:|---|
| EasyListening | ReverieMoodPiece | 15 Pop rows | Already has several standard/ballad/charm shapes; pool and ranking are the first issues. Instrumental and vocal cohorts still need metadata. |
| Country | CountryTwoBeat | 4 | Missing distinctions above; all generated Country songs initially use the same row. |
| Folk and ContemporaryFolk | VerseDrivenSong | 4 | Identical identity prior and forms; no live distinction between tradition-bearers and contemporary writers/interpreters. |
| Jazz and BossaNova | Swinger | 6 | Same prior, predominantly vocal/pop/swing rows; no bossa-specific or early modern-jazz instrumental vocabulary. JamExtendedWorkout is unavailable until 1965. |
| LatinPop and TexMex | DanceNumber | 1 | One dance shape cannot represent both repertoires; no initial Latin songbook supply. |
| Classical | ReverieMoodPiece | 1 | Only a mood piece in this period; no orchestral, chamber or solo instrumental distinction. ChamberBaroquePiece starts 1964. |
| Gospel | SpiritualShout | 4 | Quiet hymns, spirituals and quartet/choir differences need more than a universal shout fallback. |
| SurfRock | MidTempoRocker | 10 Rock rows | No specific surf instrumental fallback; genre does not automatically clear vocal demands. |
| Comedy and Childrens | SpokenWord | 3 NonMusic rows | A nursery song and a spoken routine inherit the same prior unless explicitly classified. |

Instrumental vocal demands become zero only with explicit `Instrumental` vocal presence or lyric mode. Unknown metadata does not make Jazz, Classical or Surf material instrumental. Tags add demands by count, with limited special cases; they do not fully describe characteristic orchestration or rhythmic function. These are structural representational gaps; this audit does not attribute a measured percentage of failures to each missing shape.

## Easy Listening: inclusion and arrangement evaluation

The standard-led preference fails through two independent rules:

- **The candidate pool switches discontinuously.** Add exact-genre standards/hits, then add same-family standards/hits only if raw count is under four. Tin Pan Alley and Christmas standards are primary TraditionalPop, secondary EasyListening; indexes use primary genre. The fourth EasyListening ordinary cover removes the entire family songbook. The gate precedes deduplication and date eligibility and is unrelated to the actual requested cover count. This can also crowd out family repertoire for other small genres.
- **Selection evaluates a different arrangement from the refusal gate.** `Fit` scores reference/demo demands plus a continuous identity pull. `SuitableSongs` orders on that preliminary result. `Prepare` subsequently resolves a discrete, act-appropriate arrangement for refusal. The ranking does not reconsider that resolved capability. TraditionalPop's `LushStandard` has nuance demand .90, versus .35 for TeenPop's `BrightPopNumber`; EasyListening's own fallback is .70. An arrangement or instrumental reading that makes an older song playable does not earn that capability advantage during preliminary ranking.

The census's .64-vs-.87 candidate capability comparison therefore measures **preliminary** fits, not resolved standards-versus-ordinary arrangements. The report/quoted analysis occasionally blurs that distinction. Over the full window, 96–97% of enabled EasyListening covers are ordinary covers, about 2% are seeded recent hits. The long-run failure is not primarily chasing current hits.

There is ample Pop vocabulary already, so adding an EasyListening archetype alone is unlikely to repair this. The first diagnostic interventions should keep standards eligible and compare ordering by reference fit with ordering by the same proposed arrangement used for the gate. The magnitude of their independent effects remains unmeasured.

## Supply: repertoire is overly dependent on chart success

The initial standard catalogue contains only Tin Pan Alley, Jazz, Country, Blues, Gospel, Folk, R&B catalogue and Christmas groups. The initial recent-hit catalogue contains only RockAndRoll, RnB, TraditionalPop, TeenPop, DooWop and Country. There is no native Latin/TexMex, Classical, Comedy, Childrens, Bossa or ContemporaryFolk historical songbook.

For a new ordinary composition, `OnRecordChartRunComplete` promotes it into the indexed cover pool only after a **completed top-40 run**. A song's `isCoverable` flag alone does not put it into that pool. Professional catalogues are separate. Genre-exact live selection consequently lacks a route for circulating unpublished songs, album repertoire, locally familiar material or fresh professional songs independent of successful chart completion. A Classical act can lack repertoire because its world has not yet produced a qualifying chart composition, despite a historical tradition built around inherited works.

The family fallback makes Bossa inherit Jazz's seeded repertoire and ContemporaryFolk inherit Folk's traditions. For Latin/TexMex and Classical, their own family has no seeded supply. The recording cross-family helper also defaults unhandled families—including Latin/Classical/NonMusic—to Pop/Jazz pools; that fallback is not a historically specific source ecology.

Reanalysis narrows the shortage divergence without claiming a fixed-cohort causal effect:

| Seed/mode | LatinPop/TexMex first nonempty monthly pool | Eligible songs per act in December 1963 |
|---|---|---:|
| 1001 off | October 1960 | 3 |
| 1001 on | April 1962 | 1 |
| 1002 off | October 1963 | 1 |
| 1002 on | never in these 48 months | 0 |

Both genres have the same transitions because they share the Latin-family supply while their exact pools remain small. Classical's first nonempty month is May 1960 / March 1960 in seed 1001 off/on, and January 1961 / July 1962 in seed 1002 off/on. This supports dependence on rare qualifying supply events. The retained aggregate data does not identify the particular compositions and completed chart runs that caused every transition, or isolate which polar consumer changed those outcomes.

## Category semantics need repair before historical calibration

`GenerateStandardFamily` sets `isStandard = true` for every generated member, including all 350 R&B catalogue songs dated 1945–59. It also sets public domain independently with an 18% random draw unless the whole family is traditional. The census classifies public-domain songs as traditional even when `isTraditional` is false. Consequently its standard/traditional statistic combines repertoire status, generation bucket and rights status.

This materially changes interpretation: seed 1001 RnB's 45.14% consists of **5.85% standard and 39.29% traditional/public-domain slots**. There is no authored RnB-primary traditional family: those RnB-primary public-domain compositions originate from the synthetic R&B standard catalogue. The full selected category counts do not by themselves establish the origins of every cross-family candidate. Gospel is similarly 4.88% standard and 73.00% traditional/public-domain despite its seed family being authored as Gospel Standard, not Gospel Traditional. Exact ranking and recording/reference feedback can concentrate the randomly designated subset; that feedback's individual causal contribution was not isolated.

A song can be a recent cover now and an evergreen later. The current model raises familiarity and remembers performances but has no demonstrated dynamic promotion from nonstandard to `isStandard`. Modern classics can therefore remain ordinary covers forever while all seeded catalogue songs are treated as established standards from startup. Source attribution should distinguish:

1. A newly authored song, whether by the artist or a supplied professional writer.
2. A cover of a recent or ordinary existing composition.
3. A composition established as a standard **by the observation year**.
4. Traditional lineage, tracked separately from rights/public-domain status.

Melodic borrowing also does not automatically make a newly written topical song a traditional cover. These distinctions determine what historical share we should even estimate.

## Historical estimates: first-pass working ranges, not measured genre rates

No source examined provides a representative 1960–63 US live-set census for all nine genres. Album examples, archival descriptions and artist histories support direction and heterogeneity, but cannot justify percentages to two decimal places. The following are **judgment-based starting ranges for investigation**, informed by those sources; they are not measured historical estimates, confidence intervals, acceptance thresholds or implemented targets. The intended denominator is performed song slots and the numerator is traditional lineage plus standards already established at the time; fresh outside-written material and recent-hit covers are excluded. Live and studio cohorts must subsequently be estimated separately.

| Genre/cohort | Provisional share to investigate | Interpretation of current share |
|---|---:|---|
| Bossa Nova | 15–40% older songbook; contemporary Brazilian compositions measured separately | 77% cannot be justified by inheriting the American Jazz pool. Many songs we now call bossa standards were new in this period. |
| Contemporary Folk | 10–35% for the contemporary writer/interpreter scene after 1962 | 78% and almost no contemporary covers defeat the intended distinction. Earlier traditional-revival entrants may legitimately be much higher. |
| Country | 20–50% for mixed contemporary acts; heritage/dance-hall acts can be higher | Near-total old songbook among covers is suspect, but a high total cover share is historically defensible. Do not equate covers with standards. |
| Folk, traditional revival | 50–90% | 76% is plausible for this cohort; ten compositions taking 87–92% of covers is the clearer problem. |
| Gospel | 50–90% for inherited hymn/spiritual/established gospel repertoire | 78% can be plausible; almost no new supplied songs and synthetic public-domain dominance need review. Split quartet, choir and songwriter-led acts. |
| Jazz | 60–90% for standards interpreters; 10–50% for composer-led modern groups | 76% is plausible for the first cohort, not a universal description of jazz. |
| RnB | 5–25% older standards/traditional; older R&B catalogue covers tracked separately | 45% is poorly comparable because the numerator includes all seeded R&B catalogue and arbitrary public-domain status. Some crossover interpreters can be much higher. |
| Surf Rock | 0–10% vocal surf; 5–25% instrumental revival/adaptation repertoire | 2.3% is plausible in aggregate, but generic Rock suppression does not establish correct material or instrumental metadata. Recent covers can be common without being standards. |
| Easy Listening | 50–85% for the requested standards-led adult cohort; contemporary instrumental/MOR repertoire separately | Sub-2% clearly fails that design. A universal old-song quota would misrepresent the contemporary instrumental portion of the genre. |

These bands express useful hypotheses to test, not newly discovered historical statistics. Representative setlists/tracklists stratified by year, scene, vocalist/instrumentalist and act role are needed to narrow them.

### Concrete historical anchors

- **Contemporary folk changes quickly.** Dylan's [1962 debut tracklist and original liner notes](https://www.bobdylan.com/albums/bob-dylan/) identify the older songs and the two Dylan originals. Eleven of thirteen are outside/older repertoire, **84.6% nonoriginal**, which is not automatically 84.6% standards. His [1963 Freewheelin' notes](https://www.bobdylan.com/albums/freewheelin-bob-dylan/) explicitly describe its new songwriting and two direct older-song adaptations, Corrina and Honey; a narrow direct-adaptation count is 2/13, **15.4%**, while melody-derived new songs require separate classification. [The Times They Are A-Changin' (1964)](https://www.bobdylan.com/albums/the-times-they-are-a-changin/) illustrates an entirely newly authored song programme. These examples establish variation, not Dylan's representativeness. Smithsonian documents the [new topical-writing movement](https://folkways.si.edu/a-complete-unknown-a-listening-companion-from-smithsonian-folkways). By contrast the Library's [Joan Baez essay](https://lcweb2.loc.gov/static/programs/national-recording-preservation-board/documents/JoanBaez.pdf) describes a traditional-revival debut, and [Baez's own chronology](https://www.joanbaez.com/bio/) documents subsequent contemporary repertoire.
- **Bossa is not American standards in a different outfit.** The archival [Getz/Gilberto eight-track listing and credits](https://immub.org/album/getz-gilberto-featuring-antonio-carlos-jobim-stan-getz-e-joao-gilberto?page=2) contain six Jobim compositions and two older sambas. The archive documents [Doralice in 1945](https://immub.org/compositor/dorival-caymmi?order=asc&order_by=SANO2&page=4) and [Pra Machucar Meu Coracao in 1943](https://immub.org/artista/deo). That is **2/8 = 25% pre-bossa repertoire in this particular 1964 LP**, not a measured bossa-wide standard share. The Library documents [Ipanema's 1963 recording and bossa's emergence](https://blogs.loc.gov/nls-music-notes/2019/11/the-blog-from-ipanema/). Calling all six Jobim pieces longstanding standards at release would import later fame into an earlier simulation year.
- **Country covers can be extensive without a wholly traditional identity.** Cash's official [Now, There Was A Song! tracklist and 1960 liner notes](https://www.johnnycash.com/music/now-there-was-song/) describe twelve interpretations of other singers' material, while contrasting his preceding original-led records. The Museum documents [Bill Anderson's contemporary songwriting and repeated versions of Tip of My Fingers](https://www.countrymusichalloffame.org/hall-of-fame/bill-anderson). Country needs supplied new songs, recent catalogue and heritage repertoire alongside originals.
- **Jazz contains contrasting repertoire roles.** [Kind of Blue's official five-track album account](https://www.milesdavis.com/albums/kind-of-blue/) and the [Library's historical essay](https://www.loc.gov/static/programs/national-recording-preservation-board/documents/KindOfBlue.pdf) document the late-1950s modern-jazz compositional turn. The label's [Ella in Berlin tracklist](https://www.universalmusic.it/musica-jazz/album/the-complete-ella-in-berlin-mack-the-knife_32788281703/) represents a standards interpreter. A single Swinger/default original-count policy cannot represent both.
- **Gospel is a writing tradition as well as inherited worship material.** The Library's [African American Gospel history](https://www.loc.gov/collections/songs-of-america/articles-and-essays/musical-styles/ritual-and-worship/african-american-gospel) discusses Dorsey's composed repertoire, spiritual arrangements, quartet development and the old-hymn basis of Oh Happy Day. Smithsonian's [gospel panorama](https://folkways.si.edu/classic-african-american-gospel-from-folkways/music-sacred/music/album/smithsonian) also distinguishes spirituals, quartets, choirs and guitar evangelists. The retrospective compilation spans several decades and must not be treated as a 1960s frequency sample.
- **RnB has contemporary creators and catalogue interpreters.** Stax's [Otis Redding history](https://staxrecords.com/artist/otis-redding/) discusses contemporary hits and covers; the rights-holder's [Ray Charles country albums](https://concord.com/concord-albums/modern-sounds-in-country-and-western-music-volumes-1-2/) document a major interpreter/crossover exception. Low old-standard prevalence should not be converted into a low outside-written-material rule for every act.
- **Surf instrumentals have identifiable exceptions.** The artist's [Surfin' USA tracklist](https://thebeachboys.com/pages/music) includes Misirlou, Honky Tonk and Let's Go Trippin'. The Library's [Florida folklife sampler](https://wwws.loc.gov/folklife/sampler/FLaudio.html) documents Misirlou's older lineage and its surf adaptations. Misirlou alone is **1/12 = 8.3% of this LP's original programme**; recent instrumental covers are a separate category. This is an example rather than an estimate for the whole genre.
- **Easy Listening includes contemporary instrumental production.** The reissue label's [Whipped Cream & Other Delights press release and composer credits](https://www.onamrecords.com/sites/default/files/2022-05/Whipped_Cream_2005_PR.pdf) describe the 1965 programme and include Sol Lake compositions alongside Tangerine and other outside material. The production cannot be understood solely as singers choosing either lush old standards or bright teen hits. The user's standards-led adult cohort remains a coherent design direction within the broader category.

## What should be investigated before tuning

1. Give catalogue compositions meaningful taxonomy at creation, with persistent variation around their musical form. Audit generated **and actually selected** archetype distributions; adding unused table rows accomplishes little. Preserve instrumental constraints and composition lineage through arrangements/save/load.
2. Separate suitability from repertoire choice. Keep a plateau for material an act can readily perform if that is intended, then use stable act-specific preferences and set context to choose among suitable numbers. Decide whether order persists per act/song, per season, or per booking; month-salted randomness alone would produce repertoire churn.
3. Make pool inclusion express repertoire access instead of a four-entry rescue. Explicitly model secondary/cross-scene access and a persistent eligible songbook. A stable tie-break cannot recover excluded compositions.
4. Compare proposed-arrangement suitability with reference suitability on the same actors/pools. This is the clean test of Easy Listening's ranking problem. Reconsider act/session inference separately if resolved arrangements still systematically disadvantage standards.
5. Seed the missing repertoires and add circulation routes appropriate to genre/scene. Trace every actual pool admission to song origin, release/recording, chart completion and primary/secondary tags. Use the same retained worlds to explain the Latin/Classical transitions before claiming a numerical treatment effect.
6. Correct historical categories and estimate per-year/cohort shares with numerator and denominator disclosed. Keep live composition selection, recording source choice and chart success as separate quantities. Originals, new professional songs, recent covers and old standards all need room in a live set.

Scouting uncertainty should influence **how differences become observable**, not force truthful song variance to exceed first-listen bands. The configured full un-clipped band width is 1.6 × observation error; strong-staff demand bands are .112 at first listen and .01792 at playback. Modest persistent differences can be visible after stronger evidence while appropriately overlapping early on. Metadata, truth variance and deterministic observation variation are different mechanisms.

## Validation and remaining limits

Reproduce this extraction with `node SimTools/analyze-polar-model-diagnosis.mjs`. It writes ignored [polar-model-diagnosis.json](../SimLogs/polar-model-diagnosis.json), with input SHA-256 hashes, all genre/family row coverage, both populations in both seeds, shortage timelines and the plateau arithmetic. The eight user percentages were checked to their reported rounding; 48 monthly shortage rows per genre/run were checked. Existing source edits were preserved. No gameplay tests are claimed because no gameplay changed.

The census demonstrates tie dependence and supply shortages. Static source inspection proves the pool/ranking/category mechanisms. Historical examples establish important distinctions and challenge blanket assumptions. Numerical remedy effect sizes, representative historical live-set percentages, and the composition-level provenance of every admission remain unmeasured. The first-pass ranges above should narrow through archival sampling, not acquire false precision through simulation calibration.
