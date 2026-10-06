# Genre repertoire repair — implementation and matched census

October 5, 2026. Run tag v3; development seeds 1001 and 1002. No hold-out seeds, commits or pushes.

The user authorized implementation of the attached repair plan. Its provisional percentages remain diagnostic comparisons; they were not installed as hard caps or tuned after measurement. Country remains unset. Existing Gospel, Folk and Easy Listening numeric calibration is unchanged.

## Historical interpretation

All three genres used film songs. TraditionalPop draws substantially on Broadway and Hollywood standards ([Songbook Foundation](https://thesongbook.org/about/hotel-carmichael-experience-curators/)); Country has singing-cowboy film repertoire ([Country Music Hall of Fame](https://www.countrymusichalloffame.org/hall-of-fame/sons-of-the-pioneers)); teen-oriented pop had movie songs, including Connie Francis’s Where the Boys Are ([Universal Music](https://www.udiscovermusic.com/stories/best-connie-francis-songs-feature/)) and Elvis’s 1961 Blue Hawaii soundtrack ([official catalogue](https://www.elvisthemusic.com/music/blue-hawaii/)). These sources establish repertoire connections, not numerical live-set shares. The 5%/5%/10% comparisons concern ExternalMediaTheme/Composition in this catalogue, not every song historically originating in a film. Older seeded standards have no complete film-origin provenance. Vocal stage/film songs remain eligible.

## Phase 0 — code map and retained diagnostics

Before-edit hashes are in GenreRepertoireRepairBeforeHashes.json; original source copies are retained under tmp/genre-repair-before with .snapshot extensions. Final code locations follow.

| Path | Final location |
|---|---|
| OriginalCount and WritingPropensity | [Systems/LiveRepertoire.cs:117–142](C:/Project/Label-Man/Systems/LiveRepertoire.cs:117) |
| Access tiers, NonMusic isolation, drift | [Systems/LiveRepertoire.cs:61–78](C:/Project/Label-Man/Systems/LiveRepertoire.cs:61) |
| NonMusic membership | [Data/GenreCatalog.cs:279–280](C:/Project/Label-Man/Data/GenreCatalog.cs:279) |
| Origin + 15 and Comedy exclusion | [Systems/RepertoireProvenance.cs:11–15](C:/Project/Label-Man/Systems/RepertoireProvenance.cs:11) |
| Seeded establishment overrides | [Systems/CompositionCatalogService.cs:757–776](C:/Project/Label-Man/Systems/CompositionCatalogService.cs:757) |
| Album-composition backfill | [Systems/CompositionCatalogService.cs:715–732](C:/Project/Label-Man/Systems/CompositionCatalogService.cs:715) |
| Album cuts enter live pools | [Systems/CompositionCatalogService.cs:687–693](C:/Project/Label-Man/Systems/CompositionCatalogService.cs:687) |
| Fit bands and keyed personal preference | [Systems/PolarSongBehavior.cs:97–122](C:/Project/Label-Man/Systems/PolarSongBehavior.cs:97) |
| Affinity and liveSetMixes | [Systems/LiveRepertoire.cs:25–27](C:/Project/Label-Man/Systems/LiveRepertoire.cs:25) |
| Gospel sacred source preference | [Data/PolarRepertoireTable.json:1168–1185](C:/Project/Label-Man/Data/PolarRepertoireTable.json:1168) |

Original allocation and stochastic rounding: [Systems/LiveRepertoire.cs:122–142](C:/Project/Label-Man/Systems/LiveRepertoire.cs:122). The generic writing floor/exceptional writer values remain .15/.85, and access tiers remain .22/.08/.035/.015. Country’s propensity remains .70, generic default .65. Comedy/Children remain NonMusic in taxonomy; live access is separated at the selector. Gospel retains .77 inherited affinity, ±.10 act spread and sacred eligibility. No distinct PolarGospelRepairDirective4.md or matching named check was present; the landed GospelLiveRepairReport and its preference/Directive 3 checks are the available regression specification.

**Q1.** The retained monthly catalogue and actor-access decompositions for all six baseline seeds are in GenreRepertoireRepairPhase0.json (questions.Q1). There is no weekly pool census in those retained inputs; monthly counts are reported without inventing intervening weeks. External cuts were admitted individually, and each got its own keyed access draw. More cuts increased live candidate opportunity. There was no vocal-act screen-instrumental eligibility filter; the cover resolver’s instrumental arrangement logic did not exclude them. Primary EasyListening reaches them at exact .22, TraditionalPop at secondary .08, other Pop identities at family .035 and Country via Pop at cross-scene .015. Explicit secondary tags and identity drift can change an act’s tier.

| Seed | Date | Genre | Mean accessed screen cues | Mean accessed stage/film songs |
|---:|---|---|---:|---:|
| 1001 | 1/4/1963 | TeenPop | 14.17 | 28.85 |
| 1001 | 1/4/1963 | TraditionalPop | 32.81 | 64.53 |
| 1001 | 1/4/1963 | Country | 5.88 | 13.28 |
| 1001 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 1001 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 1001 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 1001 | 2/1/1963 | TeenPop | 14.69 | 29.45 |
| 1001 | 2/1/1963 | TraditionalPop | 33.65 | 66.38 |
| 1001 | 2/1/1963 | Country | 6.16 | 13.47 |
| 1001 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 1001 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 1001 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 1002 | 1/4/1963 | TraditionalPop | 32.49 | 65.00 |
| 1002 | 1/4/1963 | TeenPop | 14.14 | 27.99 |
| 1002 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 1002 | 1/4/1963 | Country | 7.30 | 11.46 |
| 1002 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 1002 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 1002 | 2/1/1963 | TraditionalPop | 34.66 | 66.61 |
| 1002 | 2/1/1963 | TeenPop | 15.31 | 28.40 |
| 1002 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 1002 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 1002 | 2/1/1963 | Country | 7.43 | 11.85 |
| 1002 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 42001 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 42001 | 1/4/1963 | TraditionalPop | 37.12 | 58.93 |
| 42001 | 1/4/1963 | TeenPop | 17.74 | 26.02 |
| 42001 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 42001 | 1/4/1963 | Country | 7.49 | 10.70 |
| 42001 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 42001 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 42001 | 2/1/1963 | TraditionalPop | 38.92 | 61.00 |
| 42001 | 2/1/1963 | TeenPop | 18.50 | 26.89 |
| 42001 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 42001 | 2/1/1963 | Country | 7.67 | 11.34 |
| 42001 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 42002 | 1/4/1963 | TeenPop | 12.03 | 24.59 |
| 42002 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 42002 | 1/4/1963 | TraditionalPop | 26.87 | 60.87 |
| 42002 | 1/4/1963 | Country | 4.38 | 13.18 |
| 42002 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 42002 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 42002 | 2/1/1963 | TeenPop | 11.69 | 26.29 |
| 42002 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 42002 | 2/1/1963 | TraditionalPop | 27.20 | 62.84 |
| 42002 | 2/1/1963 | Country | 4.59 | 13.58 |
| 42002 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 42002 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 42003 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 42003 | 1/4/1963 | Country | 6.91 | 13.60 |
| 42003 | 1/4/1963 | TraditionalPop | 35.38 | 70.91 |
| 42003 | 1/4/1963 | TeenPop | 16.63 | 29.97 |
| 42003 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 42003 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 42003 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 42003 | 2/1/1963 | Country | 7.15 | 13.34 |
| 42003 | 2/1/1963 | TraditionalPop | 38.25 | 72.25 |
| 42003 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 42003 | 2/1/1963 | TeenPop | 17.64 | 29.91 |
| 42003 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 42004 | 1/4/1963 | Childrens | 0.00 | 0.00 |
| 42004 | 1/4/1963 | TeenPop | 15.19 | 30.78 |
| 42004 | 1/4/1963 | TraditionalPop | 33.77 | 67.51 |
| 42004 | 1/4/1963 | Classical | 0.00 | 0.00 |
| 42004 | 1/4/1963 | Comedy | 0.00 | 0.00 |
| 42004 | 1/4/1963 | Country | 6.62 | 11.58 |
| 42004 | 2/1/1963 | Childrens | 0.00 | 0.00 |
| 42004 | 2/1/1963 | TeenPop | 15.58 | 30.40 |
| 42004 | 2/1/1963 | TraditionalPop | 34.69 | 70.44 |
| 42004 | 2/1/1963 | Classical | 0.00 | 0.00 |
| 42004 | 2/1/1963 | Comedy | 0.00 | 0.00 |
| 42004 | 2/1/1963 | Country | 6.65 | 12.23 |

Means above are weighted per unsigned sampled act, including acts with zero access. The JSON retains the complete genre-tag catalogue counts and all actor-access rows.

**Q2.** All six retained matched seeds are decomposed below. “Charted” means a positive runtime source-record peak or retained completed-source history at the end checkpoint; it is not a real historical chart assertion. Album-track sources use their parent album peak, and are explicitly marked parentAlbumRuntime in the row evidence; this does not claim each cut charted independently. Pre-game standards have no retained source-record chart evidence. End-checkpoint evidence may be later than the January observation. Importantly, 1948 reaches age 15 in 1963; 1949 onward has not. The directive’s inclusive 1948–1955 wording is off by one.

| Genre / existingCover bucket | Year bin | Observed slots | Weighted slots | Share of bucket | Charted weighted slots | No chart weighted slots | Evidence unavailable |
|---|---|---:|---:|---:|---:|---:|---:|
| Country / ArtistOriginal | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / ArtistOriginal | 1940–49 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / ArtistOriginal | 1950–54 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / ArtistOriginal | 1955–59 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / ArtistOriginal | 1960+ | 523 | 9059.75 | 100.00% | 1036.83 | 5671.67 | 2351.25 |
| Country / PreGameStandard | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / PreGameStandard | 1940–49 | 4 | 90.42 | 16.16% | 0.00 | 0.00 | 90.42 |
| Country / PreGameStandard | 1950–54 | 17 | 309.17 | 55.27% | 0.00 | 0.00 | 309.17 |
| Country / PreGameStandard | 1955–59 | 8 | 159.83 | 28.57% | 0.00 | 0.00 | 159.83 |
| Country / PreGameStandard | 1960+ | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / allExistingCover | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| Country / allExistingCover | 1940–49 | 4 | 90.42 | 0.49% | 0.00 | 0.00 | 90.42 |
| Country / allExistingCover | 1950–54 | 21 | 381.00 | 2.05% | 0.00 | 0.00 | 381.00 |
| Country / allExistingCover | 1955–59 | 105 | 1870.83 | 10.08% | 0.00 | 0.00 | 1870.83 |
| Country / allExistingCover | 1960+ | 922 | 16211.75 | 87.38% | 1335.50 | 6929.67 | 7946.58 |
| TraditionalPop / ArtistOriginal | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / ArtistOriginal | 1940–49 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / ArtistOriginal | 1950–54 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / ArtistOriginal | 1955–59 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / ArtistOriginal | 1960+ | 60 | 540.42 | 100.00% | 91.67 | 272.17 | 176.58 |
| TraditionalPop / PreGameStandard | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / PreGameStandard | 1940–49 | 9 | 85.33 | 15.88% | 0.00 | 0.00 | 85.33 |
| TraditionalPop / PreGameStandard | 1950–54 | 26 | 275.67 | 51.29% | 0.00 | 0.00 | 275.67 |
| TraditionalPop / PreGameStandard | 1955–59 | 19 | 176.50 | 32.84% | 0.00 | 0.00 | 176.50 |
| TraditionalPop / PreGameStandard | 1960+ | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / allExistingCover | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TraditionalPop / allExistingCover | 1940–49 | 9 | 85.33 | 0.95% | 0.00 | 0.00 | 85.33 |
| TraditionalPop / allExistingCover | 1950–54 | 26 | 275.67 | 3.06% | 0.00 | 0.00 | 275.67 |
| TraditionalPop / allExistingCover | 1955–59 | 84 | 842.00 | 9.34% | 0.00 | 0.00 | 842.00 |
| TraditionalPop / allExistingCover | 1960+ | 807 | 7808.50 | 86.65% | 169.83 | 470.83 | 7167.83 |
| TeenPop / ArtistOriginal | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / ArtistOriginal | 1940–49 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / ArtistOriginal | 1950–54 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / ArtistOriginal | 1955–59 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / ArtistOriginal | 1960+ | 153 | 1584.42 | 100.00% | 113.92 | 896.42 | 574.08 |
| TeenPop / PreGameStandard | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / PreGameStandard | 1940–49 | 1 | 8.42 | 3.56% | 0.00 | 0.00 | 8.42 |
| TeenPop / PreGameStandard | 1950–54 | 11 | 129.50 | 54.81% | 0.00 | 0.00 | 129.50 |
| TeenPop / PreGameStandard | 1955–59 | 11 | 98.33 | 41.62% | 0.00 | 0.00 | 98.33 |
| TeenPop / PreGameStandard | 1960+ | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / allExistingCover | pre-1940 | 0 | 0.00 | 0.00% | 0.00 | 0.00 | 0.00 |
| TeenPop / allExistingCover | 1940–49 | 1 | 8.42 | 0.07% | 0.00 | 0.00 | 8.42 |
| TeenPop / allExistingCover | 1950–54 | 12 | 135.83 | 1.09% | 0.00 | 0.00 | 135.83 |
| TeenPop / allExistingCover | 1955–59 | 288 | 3089.50 | 24.84% | 0.00 | 0.00 | 3089.50 |
| TeenPop / allExistingCover | 1960+ | 848 | 9202.75 | 74.00% | 397.58 | 2799.50 | 6005.67 |

ArtistOriginal buckets above require the composition’s primary genre to match the act’s genre. Country PreGameStandard rows are specifically Country Standard. The full row identities, titles, originators and chart evidence are retained in the phase-0 JSON. Country’s ArtistOriginal covers are entirely 1960+, so the old classics hypothesis does not explain that bucket. The younger pre-game standards are a separate small contribution.

**Q3.** 1879 sampled Comedy-routine slots; 7731.25 weighted. Originator matches: 0.00. 7731.25 weighted slots lack author/originator evidence. Anonymous seeded routines cannot be asserted authored by any sampled comedian; proven non-author versus unknown is not conflated. Every row is retained in questions.Q3.rows. The new selector excludes all routines without performer authorship evidence.

**Q4.** Each retained seed contains the same 120 keyed Classical works: 114 pre-1940, 1 from 1940–49, 4 from 1950–54 and 1 from 1955–59. This is already 95% pre-1940 and needs no expansion. Children’s has 80 traditional songs and supplied the measured sets without expansion. The ArtistOriginal/Classical spillover comprises recent generated compositions, including album-track IDs song_orig_gen_*_t*, with non-Classical secondary tags. They enter through secondary access, not the old Classical works family. Originators, member-writer IDs and source-record IDs for each selected row are in questions.Q4.classicalOriginalRows. The Classical→Comedy path remains existing secondary-tag access; the Classical family was not opened to Comedy.

## Changes by phase

**A.** [Systems/LiveRepertoire.cs:33](C:/Project/Label-Man/Systems/LiveRepertoire.cs:33) reuses the known-performer/no-vocalist instrumental profile as the screen-cue compatibility rule. [Systems/SongMaterialSelectionService.cs:597](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:597) chooses external material once per cover slot using the existing genre access tier, then screen versus stage/film family without counting cuts. Ordinary material has separate ranking. Calibrated mixes choose their existing source first, then apply the media family opportunity inside it; Easy’s numeric mix is unchanged. No new film-share number was installed.

**B.** [Systems/LiveRepertoire.cs:34](C:/Project/Label-Man/Systems/LiveRepertoire.cs:34) excludes Comedy↔Children borrowing and restricts Comedy routines to originator/member-authored material. Existing secondary Classical access is retained, with its musical exception limited by the existing small familyAccess probability (.035). [Systems/PlayerDesk.cs:1467](C:/Project/Label-Man/Systems/PlayerDesk.cs:1467) fills the rest of a comedian’s programme with keyed new routines, without extra global RNG draws. Own catalogued routines are ownAuthored, outside the inherited numerator and distinct from newly authored placeholders. [Systems/RepertoireProvenance.cs:10](C:/Project/Label-Man/Systems/RepertoireProvenance.cs:10) prevents age establishment. [Systems/CompositionCatalogService.cs:832](C:/Project/Label-Man/Systems/CompositionCatalogService.cs:832) clears obsolete Comedy standard fields on old-save load; schema 2 migration is idempotent and preserves songs, taxonomy, rights and pool identities. Children’s uses its existing songbook and generic original allocation. These are live-repertoire rules; recording-source calibration and manual recording APIs were not redesigned.

**C.** [Data/PolarRepertoireTable.json:1178](C:/Project/Label-Man/Data/PolarRepertoireTable.json:1178) adds the dedicated Classical works preference and a .10 writing propensity as the low original-allocation implementation choice. Its exceptional all-original override is suppressed. Existing book count, year distribution, rights and broad keyed rotation are retained. No catalogue work was added or relabelled.

**D.** TeenPop alone gains the new .06/±.02 inherited setting. [Systems/SongMaterialSelectionService.cs:697](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:697) applies the existing .025 global top-fit band to inherited candidates before the source draw; if no fitting inherited candidate remains, it uses contemporary material. The new global fit fallback is enabled for TeenPop only; Gospel retains its landed source-specific ranking. Its source exhaustion is observed separately. Unconfigured genres retain their inherited selector, subject to A/B/C’s explicitly requested changes. [SimTools/ChartAuditRunner.GenreFollowUp.cs:25](C:/Project/Label-Man/SimTools/ChartAuditRunner.GenreFollowUp.cs:25) records requested source and fallback in scoped observer output.

**E.** Complete January/February observations use the existing follow-up census, a fresh run tag, 12 acts per stratum and unchanged population weights. The observer adds a same-world reference for all active genres, because retained matched follow-up baselines covered only the six focal genres. This isolates live-selection changes while retaining the original six-genre trajectory comparison. The reference consumes a private census RNG and resets the lazy repertoire state. It does not undo any trajectory changes or migration. Earlier diagnostic runs are superseded, with partial data excluded; they exposed the Easy mix interaction and a Gospel global-fit regression (seed 1001 full-window inherited 59.61%). Both were corrected by preserving the existing source-mix ordering and confining the new fit rule to TeenPop. Runs were stopped through the supported checkpoint mechanism. No extra seeds were required solely to measure catalogue count invariance.

## Matched unsigned shares

All values below are percentages of filled, weighted unsigned slots. The “reference” column uses identical repaired-world acts, not a substitute for the retained six-genre baseline.

| Genre | Retained inherited | Same-world reference inherited | After inherited | Traditional lineage | Established | Newly authored | Own catalogued | Existing cover | Screen | Stage/film | Pre-1940 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Country | 10.29 | 10.05 | 14.32 | 1.78 | 12.54 | 28.83 | 0.00 | 56.85 | 0.00 | 1.86 | 11.11 |
| TeenPop | 7.69 | 7.25 | 3.69 | 0.53 | 3.16 | 27.20 | 0.00 | 69.11 | 0.00 | 3.90 | 3.69 |
| TraditionalPop | 10.94 | 12.52 | 33.99 | 0.00 | 33.99 | 30.89 | 0.00 | 35.12 | 0.00 | 5.01 | 25.00 |
| Comedy | 40.57 | 41.04 | 0.00 | 0.00 | 0.00 | 84.56 | 13.20 | 2.24 | 0.00 | 0.00 | 0.00 |
| Childrens | 67.72 | 67.26 | 74.23 | 74.23 | 0.00 | 25.77 | 0.00 | 0.00 | 0.00 | 0.00 | 36.65 |
| Classical | 48.54 | 47.79 | 96.32 | 0.00 | 96.32 | 0.08 | 0.00 | 3.60 | 0.00 | 0.00 | 96.32 |
| Gospel | absent | 72.25 | 72.25 | 0.00 | 72.25 | 10.60 | 0.00 | 17.15 | 0.00 | 0.00 | 58.55 |
| Folk | absent | 50.95 | 50.95 | 33.50 | 17.46 | 20.53 | 0.00 | 28.52 | 0.00 | 0.00 | 36.38 |
| EasyListening | absent | 67.47 | 67.74 | 1.01 | 66.73 | 2.75 | 0.00 | 29.51 | 1.92 | 6.05 | 51.47 |
| BossaNova | absent | 19.89 | 31.78 | 0.53 | 31.25 | 27.08 | 0.00 | 41.14 | 0.00 | 1.12 | 21.06 |
| DooWop | absent | 6.65 | 6.69 | 0.84 | 5.85 | 26.68 | 0.00 | 66.63 | 0.00 | 2.25 | 6.12 |
| Jazz | absent | 35.56 | 43.16 | 0.00 | 43.16 | 14.93 | 0.00 | 41.91 | 0.00 | 1.58 | 30.29 |

Full per-seed and signed/unsigned category, family and pre-1940 measurements are in GenreRepertoireRepairValidation.json (measures). Additional genres enter the main table whenever external-media share moves more than 2 percentage points.

### Complete before/after category and family comparison

For the six focal genres, “before” means retained matched baseline; for regression/additional genres it means the same-world reference. All rows are unsigned.

| Genre | Measure | Before | After |
|---|---|---:|---:|
| Country | Inherited | 10.29 | 14.32 |
| Country | Pre-1940 | 7.91 | 11.11 |
| Country | Screen | 14.91 | 0.00 |
| Country | Stage/film | 0.80 | 1.86 |
| Country | traditionalLineage | 1.96 | 1.78 |
| Country | establishedStandard | 8.34 | 12.54 |
| Country | newlyAuthored | 29.35 | 28.83 |
| Country | existingCover | 60.36 | 56.85 |
| Country | ownAuthored | 0.00 | 0.00 |
| Country | existingCover / ArtistOriginal / Country | 30.03 | 35.97 |
| Country | existingCover / ArtistOriginal / EasyListening | 0.41 | 0.55 |
| Country | existingCover / ArtistOriginal / Folk | 5.96 | 6.43 |
| Country | existingCover / ArtistOriginal / Gospel | 0.00 | 0.39 |
| Country | existingCover / ArtistOriginal / TeenPop | 0.40 | 0.94 |
| Country | existingCover / ArtistOriginal / TraditionalPop | 1.16 | 2.25 |
| Country | existingCover / Country Standard | 1.72 | 2.23 |
| Country | existingCover / ProfessionalOffice / Country | 1.64 | 2.06 |
| Country | existingCover / ProfessionalOffice / EasyListening | 0.11 | 0.00 |
| Country | existingCover / ProfessionalOffice / Folk | 0.12 | 0.12 |
| Country | existingCover / ProfessionalOffice / TeenPop | 0.29 | 0.28 |
| Country | existingCover / ProfessionalOffice / TraditionalPop | 0.29 | 0.28 |
| Country | existingCover / Recent Country Hit | 1.43 | 2.13 |
| Country | existingCover / Recent Pop Hit | 0.85 | 1.10 |
| Country | existingCover / Recent Teen Hit | 0.26 | 0.26 |
| Country | existingCover / Screen instrumental | 14.91 | 0.00 |
| Country | existingCover / Stage and film songs | 0.80 | 1.86 |
| TeenPop | Inherited | 7.69 | 3.69 |
| TeenPop | Pre-1940 | 6.09 | 3.69 |
| TeenPop | Screen | 13.11 | 0.00 |
| TeenPop | Stage/film | 4.86 | 3.90 |
| TeenPop | traditionalLineage | 0.81 | 0.53 |
| TeenPop | establishedStandard | 6.88 | 3.16 |
| TeenPop | newlyAuthored | 27.41 | 27.20 |
| TeenPop | existingCover | 64.90 | 69.11 |
| TeenPop | ownAuthored | 0.00 | 0.00 |
| TeenPop | existingCover / ArtistOriginal / Classical | 0.28 | 0.00 |
| TeenPop | existingCover / ArtistOriginal / DooWop | 2.44 | 4.66 |
| TeenPop | existingCover / ArtistOriginal / EasyListening | 1.56 | 3.22 |
| TeenPop | existingCover / ArtistOriginal / Jazz | 1.82 | 2.84 |
| TeenPop | existingCover / ArtistOriginal / RockAndRoll | 3.91 | 7.50 |
| TeenPop | existingCover / ArtistOriginal / Soul | 6.13 | 9.02 |
| TeenPop | existingCover / ArtistOriginal / TeenPop | 9.24 | 13.62 |
| TeenPop | existingCover / ArtistOriginal / TraditionalPop | 3.00 | 3.39 |
| TeenPop | existingCover / Brazilian songbook | 0.25 | 0.75 |
| TeenPop | existingCover / Christmas Standard | 0.52 | 0.76 |
| TeenPop | existingCover / Jazz Standard | 0.15 | 0.37 |
| TeenPop | existingCover / ProfessionalOffice / Bubblegum | 0.31 | 0.00 |
| TeenPop | existingCover / ProfessionalOffice / GirlGroup | 0.13 | 0.27 |
| TeenPop | existingCover / ProfessionalOffice / TeenPop | 4.20 | 6.02 |
| TeenPop | existingCover / ProfessionalOffice / TraditionalPop | 0.27 | 0.27 |
| TeenPop | existingCover / Recent Pop Hit | 5.33 | 4.15 |
| TeenPop | existingCover / Recent Teen Hit | 7.40 | 7.91 |
| TeenPop | existingCover / Screen instrumental | 13.11 | 0.00 |
| TeenPop | existingCover / Stage and film songs | 4.86 | 3.90 |
| TeenPop | existingCover / Tin Pan Alley | 0.00 | 0.48 |
| TraditionalPop | Inherited | 10.94 | 33.99 |
| TraditionalPop | Pre-1940 | 9.64 | 25.00 |
| TraditionalPop | Screen | 42.91 | 0.00 |
| TraditionalPop | Stage/film | 3.84 | 5.01 |
| TraditionalPop | traditionalLineage | 0.00 | 0.00 |
| TraditionalPop | establishedStandard | 10.94 | 33.99 |
| TraditionalPop | newlyAuthored | 31.45 | 30.89 |
| TraditionalPop | existingCover | 57.62 | 35.12 |
| TraditionalPop | ownAuthored | 0.00 | 0.00 |
| TraditionalPop | existingCover / ArtistOriginal / BossaNova | 0.00 | 0.10 |
| TraditionalPop | existingCover / ArtistOriginal / Classical | 0.23 | 0.65 |
| TraditionalPop | existingCover / ArtistOriginal / EasyListening | 0.12 | 0.58 |
| TraditionalPop | existingCover / ArtistOriginal / Folk | 0.11 | 0.00 |
| TraditionalPop | existingCover / ArtistOriginal / Jazz | 0.90 | 1.37 |
| TraditionalPop | existingCover / ArtistOriginal / TeenPop | 0.66 | 0.99 |
| TraditionalPop | existingCover / ArtistOriginal / TraditionalPop | 1.99 | 10.67 |
| TraditionalPop | existingCover / Brazilian songbook | 0.23 | 0.23 |
| TraditionalPop | existingCover / Christmas Standard | 0.00 | 0.58 |
| TraditionalPop | existingCover / Jazz Standard | 3.12 | 7.29 |
| TraditionalPop | existingCover / ProfessionalOffice / TraditionalPop | 1.73 | 2.75 |
| TraditionalPop | existingCover / Recent Pop Hit | 1.32 | 2.75 |
| TraditionalPop | existingCover / Recent Teen Hit | 0.34 | 0.33 |
| TraditionalPop | existingCover / Screen instrumental | 42.91 | 0.00 |
| TraditionalPop | existingCover / Stage and film songs | 3.84 | 5.01 |
| TraditionalPop | existingCover / Tin Pan Alley | 0.12 | 1.81 |
| Comedy | Inherited | 40.57 | 0.00 |
| Comedy | Pre-1940 | 7.33 | 0.00 |
| Comedy | Screen | 0.00 | 0.00 |
| Comedy | Stage/film | 0.00 | 0.00 |
| Comedy | traditionalLineage | 14.59 | 0.00 |
| Comedy | establishedStandard | 25.98 | 0.00 |
| Comedy | newlyAuthored | 25.87 | 84.56 |
| Comedy | existingCover | 33.56 | 2.24 |
| Comedy | ownAuthored | 0.00 | 13.20 |
| Comedy | existingCover / ArtistOriginal / Classical | 2.39 | 2.24 |
| Comedy | existingCover / Comedy routines | 31.17 | 0.00 |
| Childrens | Inherited | 67.72 | 74.23 |
| Childrens | Pre-1940 | 33.55 | 36.65 |
| Childrens | Screen | 0.00 | 0.00 |
| Childrens | Stage/film | 0.00 | 0.00 |
| Childrens | traditionalLineage | 66.28 | 74.23 |
| Childrens | establishedStandard | 1.44 | 0.00 |
| Childrens | newlyAuthored | 25.72 | 25.77 |
| Childrens | existingCover | 6.56 | 0.00 |
| Childrens | ownAuthored | 0.00 | 0.00 |
| Childrens | existingCover / ArtistOriginal / Classical | 1.50 | 0.00 |
| Childrens | existingCover / Comedy routines | 5.06 | 0.00 |
| Classical | Inherited | 48.54 | 96.32 |
| Classical | Pre-1940 | 48.54 | 96.32 |
| Classical | Screen | 0.00 | 0.00 |
| Classical | Stage/film | 0.00 | 0.00 |
| Classical | traditionalLineage | 0.00 | 0.00 |
| Classical | establishedStandard | 48.54 | 96.32 |
| Classical | newlyAuthored | 25.15 | 0.08 |
| Classical | existingCover | 26.30 | 3.60 |
| Classical | ownAuthored | 0.00 | 0.00 |
| Classical | existingCover / ArtistOriginal / Classical | 23.72 | 0.00 |
| Classical | existingCover / Classical works | 2.58 | 3.60 |
| Gospel | Inherited | 72.25 | 72.25 |
| Gospel | Pre-1940 | 58.55 | 58.55 |
| Gospel | Screen | 0.00 | 0.00 |
| Gospel | Stage/film | 0.00 | 0.00 |
| Gospel | traditionalLineage | 0.00 | 0.00 |
| Gospel | establishedStandard | 72.25 | 72.25 |
| Gospel | newlyAuthored | 10.60 | 10.60 |
| Gospel | existingCover | 17.15 | 17.15 |
| Gospel | ownAuthored | 0.00 | 0.00 |
| Gospel | existingCover / ArtistOriginal / Gospel | 11.24 | 11.24 |
| Gospel | existingCover / Gospel Standard | 5.91 | 5.91 |
| Folk | Inherited | 50.95 | 50.95 |
| Folk | Pre-1940 | 36.38 | 36.38 |
| Folk | Screen | 0.00 | 0.00 |
| Folk | Stage/film | 0.00 | 0.00 |
| Folk | traditionalLineage | 33.50 | 33.50 |
| Folk | establishedStandard | 17.46 | 17.46 |
| Folk | newlyAuthored | 20.53 | 20.53 |
| Folk | existingCover | 28.52 | 28.52 |
| Folk | ownAuthored | 0.00 | 0.00 |
| Folk | existingCover / ArtistOriginal / Blues | 0.95 | 0.95 |
| Folk | existingCover / ArtistOriginal / Country | 4.73 | 4.73 |
| Folk | existingCover / ArtistOriginal / Folk | 11.92 | 11.92 |
| Folk | existingCover / Contemporary folk | 3.09 | 3.09 |
| Folk | existingCover / Country Standard | 4.76 | 4.76 |
| Folk | existingCover / Recent Country Hit | 3.07 | 3.07 |
| EasyListening | Inherited | 67.47 | 67.74 |
| EasyListening | Pre-1940 | 51.47 | 51.47 |
| EasyListening | Screen | 16.37 | 1.92 |
| EasyListening | Stage/film | 10.69 | 6.05 |
| EasyListening | traditionalLineage | 1.01 | 1.01 |
| EasyListening | establishedStandard | 66.45 | 66.73 |
| EasyListening | newlyAuthored | 2.75 | 2.75 |
| EasyListening | existingCover | 29.79 | 29.51 |
| EasyListening | ownAuthored | 0.00 | 0.00 |
| EasyListening | existingCover / ArtistOriginal / Classical | 0.00 | 0.52 |
| EasyListening | existingCover / ArtistOriginal / EasyListening | 0.37 | 3.84 |
| EasyListening | existingCover / ArtistOriginal / Folk | 0.00 | 0.97 |
| EasyListening | existingCover / ArtistOriginal / Jazz | 0.00 | 0.87 |
| EasyListening | existingCover / ArtistOriginal / TeenPop | 0.00 | 0.20 |
| EasyListening | existingCover / ArtistOriginal / TraditionalPop | 0.10 | 1.37 |
| EasyListening | existingCover / Brazilian songbook | 0.00 | 1.44 |
| EasyListening | existingCover / Christmas Standard | 0.28 | 0.57 |
| EasyListening | existingCover / Contemporary folk | 0.04 | 0.11 |
| EasyListening | existingCover / Country Standard | 0.17 | 0.87 |
| EasyListening | existingCover / Jazz Standard | 0.26 | 0.99 |
| EasyListening | existingCover / ProfessionalOffice / EasyListening | 0.00 | 0.26 |
| EasyListening | existingCover / ProfessionalOffice / TraditionalPop | 0.12 | 0.34 |
| EasyListening | existingCover / Recent Country Hit | 0.00 | 0.61 |
| EasyListening | existingCover / Recent Pop Hit | 0.37 | 2.04 |
| EasyListening | existingCover / Recent Teen Hit | 0.21 | 2.88 |
| EasyListening | existingCover / Screen instrumental | 16.37 | 1.92 |
| EasyListening | existingCover / Stage and film songs | 10.69 | 6.05 |
| EasyListening | existingCover / Tin Pan Alley | 0.80 | 3.67 |
| BossaNova | Inherited | 19.89 | 31.78 |
| BossaNova | Pre-1940 | 11.43 | 21.06 |
| BossaNova | Screen | 25.72 | 0.00 |
| BossaNova | Stage/film | 2.54 | 1.12 |
| BossaNova | traditionalLineage | 0.47 | 0.53 |
| BossaNova | establishedStandard | 19.41 | 31.25 |
| BossaNova | newlyAuthored | 27.08 | 27.08 |
| BossaNova | existingCover | 53.04 | 41.14 |
| BossaNova | ownAuthored | 0.00 | 0.00 |
| BossaNova | existingCover / ArtistOriginal / BossaNova | 2.67 | 4.94 |
| BossaNova | existingCover / ArtistOriginal / EasyListening | 1.30 | 1.20 |
| BossaNova | existingCover / ArtistOriginal / Jazz | 4.55 | 11.12 |
| BossaNova | existingCover / ArtistOriginal / LatinPop | 0.61 | 1.61 |
| BossaNova | existingCover / ArtistOriginal / TeenPop | 0.96 | 1.24 |
| BossaNova | existingCover / ArtistOriginal / TraditionalPop | 3.92 | 5.71 |
| BossaNova | existingCover / Brazilian songbook | 2.74 | 4.14 |
| BossaNova | existingCover / Jazz Standard | 2.07 | 3.18 |
| BossaNova | existingCover / ProfessionalOffice / GirlGroup | 0.00 | 0.24 |
| BossaNova | existingCover / ProfessionalOffice / Jazz | 0.86 | 0.86 |
| BossaNova | existingCover / ProfessionalOffice / TraditionalPop | 0.48 | 0.48 |
| BossaNova | existingCover / Recent Pop Hit | 0.85 | 0.85 |
| BossaNova | existingCover / Recent Teen Hit | 1.24 | 1.90 |
| BossaNova | existingCover / Screen instrumental | 25.72 | 0.00 |
| BossaNova | existingCover / Stage and film songs | 2.54 | 1.12 |
| BossaNova | existingCover / Tin Pan Alley | 2.55 | 2.55 |
| DooWop | Inherited | 6.65 | 6.69 |
| DooWop | Pre-1940 | 6.08 | 6.12 |
| DooWop | Screen | 6.03 | 0.00 |
| DooWop | Stage/film | 0.00 | 2.25 |
| DooWop | traditionalLineage | 0.84 | 0.84 |
| DooWop | establishedStandard | 5.81 | 5.85 |
| DooWop | newlyAuthored | 26.68 | 26.68 |
| DooWop | existingCover | 66.67 | 66.63 |
| DooWop | ownAuthored | 0.00 | 0.00 |
| DooWop | existingCover / ArtistOriginal / Blues | 1.83 | 1.82 |
| DooWop | existingCover / ArtistOriginal / DooWop | 10.96 | 11.42 |
| DooWop | existingCover / ArtistOriginal / EasyListening | 1.24 | 1.24 |
| DooWop | existingCover / ArtistOriginal / Gospel | 0.26 | 0.26 |
| DooWop | existingCover / ArtistOriginal / RnB | 2.33 | 2.40 |
| DooWop | existingCover / ArtistOriginal / Soul | 9.02 | 9.79 |
| DooWop | existingCover / ArtistOriginal / TeenPop | 5.25 | 5.57 |
| DooWop | existingCover / ArtistOriginal / TraditionalPop | 0.33 | 0.33 |
| DooWop | existingCover / Blues Standard | 0.73 | 0.73 |
| DooWop | existingCover / ProfessionalOffice / Bubblegum | 1.60 | 1.04 |
| DooWop | existingCover / ProfessionalOffice / GirlGroup | 0.22 | 0.39 |
| DooWop | existingCover / ProfessionalOffice / Motown | 1.97 | 2.08 |
| DooWop | existingCover / ProfessionalOffice / RnB | 0.23 | 0.23 |
| DooWop | existingCover / ProfessionalOffice / SunshinePop | 0.25 | 0.25 |
| DooWop | existingCover / ProfessionalOffice / TeenPop | 1.40 | 1.40 |
| DooWop | existingCover / R&B Catalog | 1.08 | 1.17 |
| DooWop | existingCover / Recent DooWop Hit | 16.44 | 17.64 |
| DooWop | existingCover / Recent Pop Hit | 1.66 | 2.56 |
| DooWop | existingCover / Recent R&B Hit | 0.30 | 0.52 |
| DooWop | existingCover / Recent Teen Hit | 3.54 | 3.54 |
| DooWop | existingCover / Screen instrumental | 6.03 | 0.00 |
| DooWop | existingCover / Stage and film songs | 0.00 | 2.25 |
| Jazz | Inherited | 35.56 | 43.16 |
| Jazz | Pre-1940 | 25.38 | 30.29 |
| Jazz | Screen | 14.24 | 0.00 |
| Jazz | Stage/film | 2.58 | 1.58 |
| Jazz | traditionalLineage | 0.00 | 0.00 |
| Jazz | establishedStandard | 35.56 | 43.16 |
| Jazz | newlyAuthored | 14.93 | 14.93 |
| Jazz | existingCover | 49.50 | 41.91 |
| Jazz | ownAuthored | 0.00 | 0.00 |
| Jazz | existingCover / ArtistOriginal / Blues | 0.81 | 1.37 |
| Jazz | existingCover / ArtistOriginal / Classical | 1.90 | 2.05 |
| Jazz | existingCover / ArtistOriginal / EasyListening | 1.78 | 2.53 |
| Jazz | existingCover / ArtistOriginal / Jazz | 16.93 | 20.32 |
| Jazz | existingCover / ArtistOriginal / Soul | 0.27 | 0.27 |
| Jazz | existingCover / ArtistOriginal / TeenPop | 0.50 | 0.24 |
| Jazz | existingCover / ArtistOriginal / TraditionalPop | 1.21 | 1.47 |
| Jazz | existingCover / Brazilian songbook | 0.26 | 0.26 |
| Jazz | existingCover / Jazz Standard | 6.47 | 9.23 |
| Jazz | existingCover / ProfessionalOffice / Jazz | 0.59 | 0.85 |
| Jazz | existingCover / Recent Pop Hit | 0.77 | 0.77 |
| Jazz | existingCover / Recent Teen Hit | 0.63 | 0.39 |
| Jazz | existingCover / Screen instrumental | 14.24 | 0.00 |
| Jazz | existingCover / Stage and film songs | 2.58 | 1.58 |
| Jazz | existingCover / Tin Pan Alley | 0.57 | 0.57 |

| Genre | Media reference → after | Pre-1940 reference → after | Existing cover families after (all-slot %) |
|---|---|---|---|
| Country | 14.02 → 1.86 | 7.85 → 11.11 | ArtistOriginal / Country: 35.97; ArtistOriginal / Folk: 6.43; ArtistOriginal / TraditionalPop: 2.25; Country Standard: 2.23; Recent Country Hit: 2.13; ProfessionalOffice / Country: 2.06; Stage and film songs: 1.86; Recent Pop Hit: 1.10; ArtistOriginal / TeenPop: 0.94; ArtistOriginal / EasyListening: 0.55; ArtistOriginal / Gospel: 0.39; ProfessionalOffice / TeenPop: 0.28; ProfessionalOffice / TraditionalPop: 0.28; Recent Teen Hit: 0.26; ProfessionalOffice / Folk: 0.12 |
| TeenPop | 18.55 → 3.90 | 5.92 → 3.69 | ArtistOriginal / TeenPop: 13.62; ArtistOriginal / Soul: 9.02; Recent Teen Hit: 7.91; ArtistOriginal / RockAndRoll: 7.50; ProfessionalOffice / TeenPop: 6.02; ArtistOriginal / DooWop: 4.66; Recent Pop Hit: 4.15; Stage and film songs: 3.90; ArtistOriginal / TraditionalPop: 3.39; ArtistOriginal / EasyListening: 3.22; ArtistOriginal / Jazz: 2.84; Christmas Standard: 0.76; Brazilian songbook: 0.75; Tin Pan Alley: 0.48; Jazz Standard: 0.37; ProfessionalOffice / TraditionalPop: 0.27; ProfessionalOffice / GirlGroup: 0.27 |
| TraditionalPop | 44.73 → 5.01 | 10.28 → 25.00 | ArtistOriginal / TraditionalPop: 10.67; Jazz Standard: 7.29; Stage and film songs: 5.01; ProfessionalOffice / TraditionalPop: 2.75; Recent Pop Hit: 2.75; Tin Pan Alley: 1.81; ArtistOriginal / Jazz: 1.37; ArtistOriginal / TeenPop: 0.99; ArtistOriginal / Classical: 0.65; ArtistOriginal / EasyListening: 0.58; Christmas Standard: 0.58; Recent Teen Hit: 0.33; Brazilian songbook: 0.23; ArtistOriginal / BossaNova: 0.10 |
| Comedy | 0.00 → 0.00 | 7.01 → 0.00 | ArtistOriginal / Classical: 2.24 |
| Childrens | 0.00 → 0.00 | 31.84 → 36.65 |  |
| Classical | 0.00 → 0.00 | 47.79 → 96.32 | Classical works: 3.60 |
| Gospel | 0.00 → 0.00 | 58.55 → 58.55 | ArtistOriginal / Gospel: 11.24; Gospel Standard: 5.91 |
| Folk | 0.00 → 0.00 | 36.38 → 36.38 | ArtistOriginal / Folk: 11.92; Country Standard: 4.76; ArtistOriginal / Country: 4.73; Contemporary folk: 3.09; Recent Country Hit: 3.07; ArtistOriginal / Blues: 0.95 |
| EasyListening | 27.06 → 7.97 | 51.47 → 51.47 | Stage and film songs: 6.05; ArtistOriginal / EasyListening: 3.84; Tin Pan Alley: 3.67; Recent Teen Hit: 2.88; Recent Pop Hit: 2.04; Screen instrumental: 1.92; Brazilian songbook: 1.44; ArtistOriginal / TraditionalPop: 1.37; Jazz Standard: 0.99; ArtistOriginal / Folk: 0.97; ArtistOriginal / Jazz: 0.87; Country Standard: 0.87; Recent Country Hit: 0.61; Christmas Standard: 0.57; ArtistOriginal / Classical: 0.52; ProfessionalOffice / TraditionalPop: 0.34; ProfessionalOffice / EasyListening: 0.26; ArtistOriginal / TeenPop: 0.20; Contemporary folk: 0.11 |
| BossaNova | 28.26 → 1.12 | 11.43 → 21.06 | ArtistOriginal / Jazz: 11.12; ArtistOriginal / TraditionalPop: 5.71; ArtistOriginal / BossaNova: 4.94; Brazilian songbook: 4.14; Jazz Standard: 3.18; Tin Pan Alley: 2.55; Recent Teen Hit: 1.90; ArtistOriginal / LatinPop: 1.61; ArtistOriginal / TeenPop: 1.24; ArtistOriginal / EasyListening: 1.20; Stage and film songs: 1.12; ProfessionalOffice / Jazz: 0.86; Recent Pop Hit: 0.85; ProfessionalOffice / TraditionalPop: 0.48; ProfessionalOffice / GirlGroup: 0.24 |
| DooWop | 6.03 → 2.25 | 6.08 → 6.12 | Recent DooWop Hit: 17.64; ArtistOriginal / DooWop: 11.42; ArtistOriginal / Soul: 9.79; ArtistOriginal / TeenPop: 5.57; Recent Teen Hit: 3.54; Recent Pop Hit: 2.56; ArtistOriginal / RnB: 2.40; Stage and film songs: 2.25; ProfessionalOffice / Motown: 2.08; ArtistOriginal / Blues: 1.82; ProfessionalOffice / TeenPop: 1.40; ArtistOriginal / EasyListening: 1.24; R&B Catalog: 1.17; ProfessionalOffice / Bubblegum: 1.04; Blues Standard: 0.73; Recent R&B Hit: 0.52; ProfessionalOffice / GirlGroup: 0.39; ArtistOriginal / TraditionalPop: 0.33; ArtistOriginal / Gospel: 0.26; ProfessionalOffice / SunshinePop: 0.25; ProfessionalOffice / RnB: 0.23 |
| Jazz | 16.82 → 1.58 | 25.38 → 30.29 | ArtistOriginal / Jazz: 20.32; Jazz Standard: 9.23; ArtistOriginal / EasyListening: 2.53; ArtistOriginal / Classical: 2.05; Stage and film songs: 1.58; ArtistOriginal / TraditionalPop: 1.47; ArtistOriginal / Blues: 1.37; ProfessionalOffice / Jazz: 0.85; Recent Pop Hit: 0.77; Tin Pan Alley: 0.57; Recent Teen Hit: 0.39; ArtistOriginal / Soul: 0.27; Brazilian songbook: 0.26; ArtistOriginal / TeenPop: 0.24 |

### All-genre pre-1940 share

| Genre | Unsigned reference → after | Signed reference → after |
|---|---:|---:|
| Blues | 18.31 → 18.31 | 17.88 → 17.88 |
| BossaNova | 11.43 → 21.06 | 12.03 → 15.54 |
| Childrens | 31.84 → 36.65 | 31.72 → 34.52 |
| Classical | 47.79 → 96.32 | 44.12 → 93.75 |
| Comedy | 7.01 → 0.00 | 7.03 → 0.00 |
| ContemporaryFolk | 36.84 → 36.84 | 39.24 → 39.24 |
| Country | 7.85 → 11.11 | 9.08 → 13.41 |
| DooWop | 6.08 → 6.12 | 3.67 → 3.82 |
| EasyListening | 51.47 → 51.47 | 53.25 → 53.25 |
| Folk | 36.38 → 36.38 | 38.34 → 38.34 |
| GarageRock | 0.00 → 0.00 | 0.00 → 0.00 |
| Gospel | 58.55 → 58.55 | 52.16 → 52.16 |
| Jazz | 25.38 → 30.29 | 21.78 → 25.35 |
| LatinPop | 12.39 → 12.39 | 14.07 → 14.07 |
| RnB | 4.24 → 4.24 | 6.38 → 6.94 |
| RockAndRoll | 0.00 → 0.00 | 0.30 → 0.30 |
| Soul | 5.64 → 6.26 | 8.15 → 8.07 |
| SurfRock | 0.93 → 0.93 | 1.15 → 1.15 |
| TeenPop | 5.92 → 3.69 | 2.84 → 2.46 |
| TexMex | 18.88 → 18.88 | 19.90 → 19.90 |
| TraditionalPop | 10.28 → 25.00 | 9.61 → 23.84 |

### Per-seed unsigned comparison

For the six focal genres, before is the retained baseline; for Gospel/Folk/Easy it is the same-world reference. All entries are before → after percentages. Family details for each seed and signing status remain in the validation JSON.

| Genre | Seed | Lineage | Established | Newly authored | Existing cover | Own catalogued | Screen | Stage/film | Pre-1940 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Childrens | 1001 | 69.51 → 75.32 | 0.60 → 0.00 | 24.29 → 24.68 | 5.60 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 32.78 → 33.22 |
| Childrens | 1002 | 63.34 → 73.22 | 2.20 → 0.00 | 27.02 → 26.78 | 7.44 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 34.24 → 39.81 |
| Classical | 1001 | 0.00 → 0.00 | 54.32 → 96.57 | 25.29 → 0.00 | 20.40 → 3.43 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 54.32 → 96.57 |
| Classical | 1002 | 0.00 → 0.00 | 42.68 → 96.06 | 25.02 → 0.17 | 32.30 → 3.77 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 42.68 → 96.06 |
| Comedy | 1001 | 18.02 → 0.00 | 25.46 → 0.00 | 26.61 → 88.13 | 29.91 → 2.77 | 0.00 → 9.11 | 0.00 → 0.00 | 0.00 → 0.00 | 9.84 → 0.00 |
| Comedy | 1002 | 10.89 → 0.00 | 26.54 → 0.00 | 25.08 → 80.91 | 37.50 → 1.69 | 0.00 → 17.40 | 0.00 → 0.00 | 0.00 → 0.00 | 4.62 → 0.00 |
| Country | 1001 | 3.13 → 2.29 | 7.65 → 8.14 | 29.05 → 29.55 | 60.18 → 60.02 | 0.00 → 0.00 | 11.09 → 0.00 | 0.52 → 1.88 | 7.05 → 6.89 |
| Country | 1002 | 0.77 → 1.26 | 9.03 → 17.10 | 29.66 → 28.08 | 60.54 → 53.56 | 0.00 → 0.00 | 18.77 → 0.00 | 1.09 → 1.85 | 8.78 → 15.49 |
| EasyListening | 1001 | 1.17 → 1.17 | 66.20 → 66.20 | 2.20 → 2.20 | 30.42 → 30.42 | 0.00 → 0.00 | 17.41 → 1.60 | 10.15 → 5.92 | 49.52 → 49.52 |
| EasyListening | 1002 | 0.84 → 0.84 | 66.72 → 67.28 | 3.31 → 3.31 | 29.13 → 28.57 | 0.00 → 0.00 | 15.28 → 2.25 | 11.26 → 6.19 | 53.51 → 53.51 |
| Folk | 1001 | 32.72 → 32.72 | 15.32 → 15.32 | 20.77 → 20.77 | 31.19 → 31.19 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 32.07 → 32.07 |
| Folk | 1002 | 34.35 → 34.35 | 19.83 → 19.83 | 20.26 → 20.26 | 25.56 → 25.56 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 41.16 → 41.16 |
| Gospel | 1001 | 0.00 → 0.00 | 73.90 → 73.90 | 9.19 → 9.19 | 16.91 → 16.91 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 59.18 → 59.18 |
| Gospel | 1002 | 0.00 → 0.00 | 70.67 → 70.67 | 11.95 → 11.95 | 17.38 → 17.38 | 0.00 → 0.00 | 0.00 → 0.00 | 0.00 → 0.00 | 57.96 → 57.96 |
| TeenPop | 1001 | 1.26 → 0.74 | 8.09 → 2.56 | 27.41 → 27.74 | 63.24 → 68.97 | 0.00 → 0.00 | 9.28 → 0.00 | 4.47 → 3.61 | 7.81 → 3.30 |
| TeenPop | 1002 | 0.32 → 0.32 | 5.55 → 3.79 | 27.42 → 26.63 | 66.72 → 69.27 | 0.00 → 0.00 | 17.28 → 0.00 | 5.30 → 4.20 | 4.21 → 4.10 |
| TraditionalPop | 1001 | 0.00 → 0.00 | 7.65 → 30.63 | 31.62 → 31.83 | 60.73 → 37.54 | 0.00 → 0.00 | 47.52 → 0.00 | 2.60 → 5.56 | 6.49 → 23.07 |
| TraditionalPop | 1002 | 0.00 → 0.00 | 14.20 → 37.26 | 31.27 → 29.98 | 54.53 → 32.76 | 0.00 → 0.00 | 38.36 → 0.00 | 5.06 → 4.48 | 12.75 → 26.88 |

## Fit fallback

Only Gospel and TeenPop have an inherited/contemporary affinity draw. TeenPop applies the new global fit band; its fallback includes depletion of fitting inherited candidates. Gospel retains the prior selector, so its recorded fallback means existing inherited-source exhaustion, not a new global-fit rejection. Other genres have no applicable affinity request; all genre/status/seed rows remain in the JSON, with a null requested-source rate when there were no requests.

| Genre | Seed | Status | Weighted inherited requests | Weighted fallbacks | Fallback/request % | Fallback/all slots % |
|---|---:|---|---:|---:|---:|---:|
| Gospel | 1001 | unsigned | 1093.33 | 0.00 | 0.00 | 0.00 |
| Gospel | 1001 | signed | 497.17 | 0.00 | 0.00 | 0.00 |
| TeenPop | 1001 | unsigned | 112.17 | 0.00 | 0.00 | 0.00 |
| TeenPop | 1001 | signed | 62.08 | 13.67 | 22.01 | 0.71 |
| Gospel | 1002 | unsigned | 1093.83 | 0.00 | 0.00 | 0.00 |
| Gospel | 1002 | signed | 473.58 | 0.00 | 0.00 | 0.00 |
| TeenPop | 1002 | unsigned | 134.42 | 0.00 | 0.00 | 0.00 |
| TeenPop | 1002 | signed | 92.83 | 0.00 | 0.00 | 0.00 |

No unsigned TeenPop fallback was observed in either seed. Its 3.69% realized inherited share therefore cannot be attributed to measured unsigned fit rejection. The .06 setting applies only to remaining ordinary cover slots, after original/supplied-new allocation and the media opportunity; act spread and keyed source draws also affect this small matched sample. Signed seed 1001 did exercise the fallback (22.01% of weighted inherited requests). There is no retrospective tuning to force ~4.4%.


## Acceptance and compatibility

| Criterion | Result | Evidence |
|---|---|---|
| Gospel fresh full-window seed 1001 | met | 69.60% inherited (65–70%); 0.00% secular (≤2%); 13822 slots / 21 months |
| Gospel fresh full-window seed 1002 | met | 66.14% inherited (65–70%); 0.00% secular (≤2%); 12765 slots / 21 months |
| Existing Gospel/Folk/Easy numeric calibration | met | numbers, assignments, supply, bands, liveSetMixes and Gospel affinity equal before-edit snapshot |
| Gospel same-world inherited regression | met | Largest signed/unsigned per-seed difference: 0.00 percentage points |
| Folk same-world inherited regression | met | Largest signed/unsigned per-seed difference: 0.00 percentage points |
| EasyListening same-world inherited regression | reported change | Largest signed/unsigned per-seed difference: 0.56 percentage points |
| Country external-media share (provisional) | met | 1.86% vs ~5% |
| TeenPop external-media share (provisional) | met | 3.90% vs ~5% |
| TraditionalPop external-media share (provisional) | met | 5.01% vs ~10% |
| No Comedy routines in Children sets | met | All observed signed/unsigned slots checked |
| No Children songs in Comedy sets | met | All observed signed/unsigned slots checked |
| No non-author Comedy routines | met | All observed catalogued routines require originator/member-author evidence; newly authored placeholders are authored by construction |
| Classical share in Comedy does not exceed baseline | met | 2.39% retained baseline → 2.24% |
| Classical inherited >=85% (provisional) | met | 96.32% |
| Classical newly authored <=10% (provisional) | met | 0.08% |
| Classical highest pre-1940 share (provisional) | met | 96.32% |
| TeenPop realized inherited share (provisional) | reported; no numeric tolerance was specified | 3.69% pooled; seed range 3.30–4.10%; compare provisional ~4.4% |
| Fit fallback rates | reported | Per genre, seed, signed status; requested-source and all-slot denominators separate |

Build succeeds with the existing four warnings. The catalogue-count, screen-family, vocal eligibility, author-only, dedicated-book, fit-fallback, rights, migration-idempotence, Easy mix and global RNG checks pass. --folk-easy-check, --gospel-preference-check and --polar-directive3-check pass on both seeds. Actual 1001/1002 legacy s5 checkpoints load and round-trip after the explicit migration; every resumed checkpoint is verified too. The schema conversion is canonicalized before the round-trip digest, so this proves migrated saves remain lossless, not byte identity with the pre-migration Comedy fields.

Gospel’s pre-existing acceptance report specifies fresh 1960–1961 trajectories at 65–70% inherited and ≤2% secular. Both fresh 91-week trajectories were rerun with the final build, independently of the matched legacy census. Unknown contexts remain explicit. Full-window, quarterly and signed-status evidence, invocation fingerprints and resumed-save verification are in GenreRepertoireRepairGospelRegression.json.

| Seed | Weeks / monthly observations | Slots | Inherited | Secular | Unknown context slots | Acceptance |
|---|---|---:|---:|---:|---:|---|
| 1001 | 91 / 21 | 13822 | 69.60% | 0.00% | 0 | pass |
| 1002 | 91 / 21 | 12765 | 66.14% | 0.00% | 0 | pass |

Gospel working repertoire remains measured separately from its source share.

| Seed | Unique inherited songs | Population top-10 share | Median distinct inherited songs per act | P90 |
|---|---:|---:|---:|---:|
| 1001 | 259 | 12.79% | 5 | 7 |
| 1002 | 226 | 23.95% | 5 | 7 |

Easy Listening’s largest same-world inherited change is 0.56 percentage points (unsigned seed 1002); pooled unsigned share changes 67.47% → 67.74%, while pooled signed share is unchanged at 68.48%. Its calibrated source weights and original allocation are unchanged. The source buckets use a narrower year window than the inherited provenance numerator, so choosing different contemporary material can change the census category even when the selected mix source is preserved.


## Decisions and scope

Country’s inheritedLiveShare remains unset. The six-seed decomposition rules out old classics in the ArtistOriginal/Country bucket; Alice still decides any source-share setting. Provisional misses are reported without further tuning. The screen opportunity reuses existing access probabilities as source probabilities; this is an explicit implementation interpretation, not a measured historical rate. The Comedy Classical musical exception uses the existing .035 familyAccess opportunity while retaining the old secondary candidate path; no Classical family access was added. Classical’s .10 low writing propensity is an implementation choice under the directive’s qualitative allocation requirement. No TraditionalPop inherited setting, Children authored tuning, book expansion, taxonomy change, genreAcceptance, chart formula or economic calibration was added. The named Gospel Directive 4 document was unavailable; the available landed numerical acceptance and preference/Directive 3 checks were rerun instead.

No additional 42001–42004 runs were needed: both development seeds agree on the media comparisons, the Classical result and the structural Comedy/Children restrictions. TeenPop’s inherited result ranges 3.30–4.10%; it is reported against an approximate expectation without inventing a pass tolerance or retuning its approved setting. Additional seeds could improve precision but would not resolve that undefined tolerance.

## Touched source SHA-256

| Source | SHA-256 |
|---|---|
| Systems/LiveRepertoire.cs | 29a1999aec47bac804a4abfa6022ab6a1e7fcf05f4656e77ed36f4ca3559ba79 |
| Systems/SongMaterialSelectionService.cs | 09bd7961e7adb116838a6d60c5f6de77e990b93c201a83fb73822be052a41f7d |
| Systems/RepertoireProvenance.cs | c32d462396edb4cbdaee4d4da2dc4b7c0b1cdc24c7e28356d9f112d114b4aaa5 |
| Systems/PolarRepertoireTable.cs | 26605c3fd94ed50ad5d76080dd7536caa5f3dacc415ea3f637eadfd008837783 |
| Systems/CompositionCatalogService.cs | 54c04aaea878755b82cc0ee5d98884959a51e618acc4b327fb4c16ba9c77939f |
| Systems/PlayerDesk.cs | aee0304c3da189622b8b8ec4709dd89def111b5dbda454b98e638b5e4265a00b |
| Data/PolarRepertoireTable.json | bcae8b0fac36305d0dd8522ccb56729b9088597fe7be66f40fe9183db9feea70 |
| SimTools/ChartAuditRunner.GenreFollowUp.cs | b7fd58193d9a664e208d74ba0a6a6fadb397661e6e8e35274f4d116933d64386 |
| SimTools/ChartAuditRunner.PolarRepertoire.cs | 686b1ce27f915ea5d52d2503bbe57a19af5c14dc6e1ffadf31ee14d67d69b22a |
| SimTools/ChartAuditRunner.PolarResearch.cs | 84fd7d5f760a1bb9da2a88861e9bc7a472f613ad9b79ebbeb7aa63e8e026106d |
| SimTools/SaveLoadRoundTripRunner.cs | 045d7e956111153da38a61df0e8ca483e6c00391ed5386291dd8ccc1cb04fa5a |
| SimTools/GenreRepertoireRepairChecks.cs | a45aa1df5e6e22c7b2690501faeb6db0249378364293f97274c1f92f3d44934a |
| SimTools/analyze-polar-research.mjs | 9ae3e927477e7b85bfb490bf58b567794fecc58332c3454e82962d12c64004d4 |
| SimTools/run-polar-research.ps1 | 00da2f3cfca889084d34db9f0f9c6ff259a274ed670054c270a78b7bb5292532 |
| SimTools/run-genre-repertoire-repair.ps1 | 203185bfa0355533e7d4094f59545518ffffb9ba3097c47ac82f1fc74eb3d1b5 |
| SimTools/analyze-genre-repertoire-repair-phase0.mjs | dbbfe69a3cded29488c263b1fa62af98b685899922c97fa77115336411884b56 |
| SimTools/analyze-genre-repertoire-repair.mjs | 6d7652469fe1800645acd7177fd5a29072ed3b37f4c87f646101768e52472df6 |
| SimTools/analyze-genre-repair-gospel.mjs | 1f6c5f51e6450ba4b2031c59eb657a036c3e4dd71539a033b54947f29e3fa656 |

Reproduce with dotnet build --no-restore -v quiet and the final checks above. Run .\SimTools\run-genre-repertoire-repair.ps1 -RunTag FRESH, .\SimTools\run-polar-directive3-trajectories.ps1 -RunTag genre-repair-gospel-FRESH, node SimTools/analyze-genre-repair-gospel.mjs genre-repair-gospel-FRESH, then node SimTools/analyze-genre-repertoire-repair.mjs FRESH. Every invocation, assembly/table fingerprint, CSV/log/manifest hash and legacy checkpoint input hash is retained in the validation JSON.
