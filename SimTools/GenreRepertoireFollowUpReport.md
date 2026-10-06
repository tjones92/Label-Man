# Genre repertoire follow-up census

October 5, 2026. Diagnostic only: no genre policy, target or saved-data format changed.

## Findings and proposed changes

Under/over-representation judgments below refer to the author’s stated game-design expectations. They do not establish historical numeric quotas, and the reported spread is not a statistical confidence interval.

**TeenPop: concentrated original outlier, plus a persistent live-selection effect.** The original 15.69% is four established Tin Pan Alley slots from three artists, not traditional lineage. Brian and the Teens supplies 55.59% of that inherited weight, and the top three artists supply 100%. Four of 34 unweighted slots is 11.76%; the population weights lift that to 15.69%, amplifying the small sampled middle-writing stratum. This is the correct existing weighting, not evidence of a weighting bug. The larger matched estimate is 7.01%, supported by 64 seed/artist identities and 71 seed/song identities. It is no longer explained by those original three artists alone.

Seed 1002’s original 0% reflects its sampled roster’s fit/preference choices: all 9 sampled unsigned artists have accepted inherited material, with a weighted mean of 40.80 songs per artist and 0 empty inherited pools. The live rules do not forbid inherited material in TeenPop; they simply selected contemporary candidates in those observed sets. None of the four seed-1001 standards is misclassified by the unchanged establishment-year definition.

**Country: a small-sample low result within broader systematic under-selection in the current evolved catalogue.** The original 8.72% is 6.66 points traditional lineage and 2.06 points established standards; seed 1002’s 26.69% is 1.57 and 25.12 points respectively. Seed 1001 selects no Country Standard-family song in its original inherited rows, while seed 1002 draws most inherited weight from that family. Increasing the sample on the same September seed-1001 world raises the estimate to 19.23%; its June world is 16.09%. Thus the original 9% was especially roster/sample sensitive.

The matched evolved-world estimate is 10.08%. Per-date weighted mean accepted inherited pools remain 125.71–131.57 songs per unsigned artist; no sampled Country artist has an empty inherited pool. This rules out an empty inherited catalogue as the explanation. New live originals occupy 28.59% of slots, and external-media album cuts 13.84%. Available inherited songs lose to other material through roster-specific resolved fit and preference ranking, without an explicit inherited source weight. Roster composition and ranking both contribute; this census does not claim a causal decomposition from an artist-swap or policy ablation.

**Proposed TeenPop/Country experiment, for approval:** use the existing live affinity path to choose inherited versus contemporary material before ranking within each source. Illustrative game-design settings are `TeenPop: inheritedLiveShare 0.06, actSpread 0.02` and `Country: inheritedLiveShare 0.35, actSpread 0.10`. These are cover-source probabilities, not shares of all filled slots. With the observed original-slot allocations they would imply roughly 4.44% / 25.00% inherited across all slots before fallback and act effects. They express the author’s judgments, not historical estimates or acceptance targets, and have not been applied or validated. Preserve candidate access, taxonomy, rights and global RNG behavior; verify realized outcomes after any approval.

**Comedy: systematic pool/classification effect, with additional seed variation.** Pooled inherited share is 40.83%. 26.46 points come from established Comedy routines; 14.37 points come from traditional Children’s songs. Routines receive an authored establishment year of origin + 15, so sufficiently old routines count as established standards without parody or observed reuse evidence. Comedy draws Children’s material through shared NonMusic family access. **Proposal:** separate Comedy/Children’s access by genre compatibility, and review whether automatically aging a routine should establish a reusable standard. The latter would be a future classification policy decision; retain the present numerator throughout this diagnostic.

**Classical: systematic generic-original allocation and contemporary-catalogue competition.** Pooled inherited share is 48.27%; newly authored live placeholders take 26.09%, and existing ArtistOriginal-provenance catalogue material takes another 23.22%. The remaining non-inherited families and every song are listed below/in the slot export. The default writing propensity and cover ranking govern this split, rather than a dedicated Classical repertoire policy. **Proposal:** give Classical a dedicated live-original allocation and inherited source balance; agree its numeric design weights before tuning. This is not primarily an empty-pool or one-artist result.

**Children’s: systematic original allocation, with shared-family leakage.** Pooled inherited share is 68.06%; newly authored placeholders take 26.16%. Non-inherited Comedy routines contribute 4.39% of all slots. Its traditional children’s pool remains available; the generic original rule reserves much of the set before cover selection. **Proposal:** use a dedicated traditional-led live mix with a smaller authored share and explicit genre-compatible access, rather than adding more traditional songs to the catalogue. Agree numeric weights separately.

**TraditionalPop: a systematic catalogue-composition effect that the original saved snapshots understate.** Matched inherited share is 16.72%; new live originals occupy 28.61%, and external-media album cuts 38.43%. On seed 1001, album cuts alone fill 45.14% of matched slots, while the immediately loaded June/September legacy snapshots contain no selected cuts of that origin. The album-composition work backfills at the next enabled simulation week, so the matched evolved worlds and immediate old snapshots have different live pools. **Proposal:** choose source families before song ranking for TraditionalPop, so multiplying cast/film cuts cannot multiply their effective source opportunity. Review its generic original allocation alongside that change. An inherited catalogue increase alone is not supported.

These proposals leave Gospel, Folk and Easy Listening calibration outside the tuning scope. No policy change, saved-data change, commit or push is performed by the diagnostic scripts.

## Matched January/February 1963 comparison

Use this calendar-matched comparison for the six-seed diagnosis. Later observations on the original seeds are retained as supplemental per-date measures in the JSON and pool tables.

| Genre | Pooled unsigned | Equal-seed mean | Seed min–max | SD (pp) | Observed unsigned slots | Pooled signed |
| --- | ---: | ---: | --- | ---: | ---: | ---: |
| TeenPop | 7.01% | 6.93% | 2.48–10.62% | 3.36 | 1749 | 7.13% |
| Country | 10.08% | 10.02% | 4.67–18.58% | 4.93 | 1724 | 11.58% |
| Comedy | 40.83% | 40.80% | 37.26–43.48% | 2.76 | 1701 | 37.32% |
| Classical | 48.27% | 48.18% | 42.68–54.32% | 5.29 | 1721 | 41.90% |
| Childrens | 68.06% | 68.18% | 64.28–72.63% | 3.04 | 1708 | 66.04% |
| TraditionalPop | 16.72% | 16.55% | 7.65–24.18% | 5.70 | 1735 | 13.28% |

| Seed | TeenPop inherited | Slots | Country inherited | Slots |
| --- | ---: | ---: | ---: | ---: |
| 1001 | 9.36% | 295 | 10.77% | 286 |
| 1002 | 5.87% | 289 | 9.80% | 292 |
| 42001 | 3.82% | 299 | 4.67% | 282 |
| 42002 | 2.48% | 300 | 18.58% | 290 |
| 42003 | 9.45% | 284 | 5.69% | 289 |
| 42004 | 10.62% | 282 | 10.61% | 285 |

## Method and limits

- **numerator**: traditionalLineage plus establishedStandard as of observation year; public-domain status is separate
- **denominator**: all filled unsigned live slots; population/sample weights unchanged
- **pool**: Accessed pool after year and EligibleLive filter; accepted also applies resolved live refusal, without observer events. Top fit band means distance < suitabilityWindow below best accepted score; approximate diagnostic, not exact ranking buckets.
- **poolUnion**: Distinct inherited IDs accepted by at least one sampled artist; not the population union.
- **concentration**: Artist identity is seed plus artistId. Song identity is seed plus songId; cross-seed catalogue IDs do not prove identical works. Shares are of inherited weighted slots; all-slot contributions also reported.
- **pooled**: Ratio of summed weighted inherited slots to summed weighted filled slots across retained snapshots. Equal-seed mean reported separately. Startup and 1963 remain separate.
- **spread**: Min/max and sample SD across six per-seed pooled percentages; descriptive, not confidence intervals. Repeated acts and dates are correlated.
- **window**: Primary: January 4 and February 1, 1963 on all six seeds. Supplemental legacy saved worlds: June/September on 1001 and July/October on 1002. Primary restored worlds on 1001/1002 evolve from October/November 1962; new seeds evolve from startup. Save/resume does not claim equality with uninterrupted global RNG continuation. No hold-out or chart claims.
- **primaryPeriod**: matched1963
- **catalogueStates**: The primary worlds have evolved with external-media album-composition cuts/backfill. Supplemental legacy saved worlds are censused immediately after load, before their next simulation tick can backfill empty album tracks. Do not interpret all-1963 mixed-state pooling as a policy effect or an equal-state seed comparison.
- **originals**: newlyAuthored slots are the existing census live-original placeholders, without registered composition IDs or assigned rights. ArtistOriginal provenance in an existingCover row means an already catalogued composition, not a new live-original slot.

Seeds: 1001, 1002, 42001, 42002, 42003, 42004. Each stratum selects at most 12 artists by the existing keyed seed/artist hash, versus 3 in the quoted late review. Strata remain genre, cohort, signed status and writing bin. Only complete cross-sectional censuses enter estimates. Baseline replays use 3 and reproduce the original slots exactly; they are excluded from the expanded estimates.

All six January 1, 1960 startup censuses completed, but the unsigned population has not yet initialized at that instant. They supply signed measurements only; unsigned startup results are **absent**, and do not enter unsigned estimates as zeros. All six focal genres currently use the `mixedContemporary` repertoire cohort; writing bins and artist formation years supply the additional roster distinctions in the JSON.

The four added seeds were absent from retained invocation/trajectory manifests when selected. They are additional seeds, not claimed hold-outs. New worlds run 166 ordinary weeks, ending March 8, 1963; only January/February 1963 censuses are retained. This is world preparation for the census, not a 48-month trajectory or decade economic test.

## Live selection rules

None of the six focal genres has a `liveSetMixes` or `genreAffinities` entry. They allocate live originals through the generic skill-scaled `OriginalCount` rule (`writingFloor` 0.15, `exceptionalWriter` 0.85; default `WritingPropensity` 0.65, Country 0.70), then select covers by resolved fit bands and keyed personal preferences. Exact/secondary/family/cross-scene access probabilities are 0.22/0.08/0.035/0.015. The recording-source mixes do not govern these genres’ live selection.

TraditionalPop, TeenPop and Country can access adjacent scenes. Comedy and Children’s share the NonMusic family, admitting each other’s songs at family access. Classical uses its own family. Most nontraditional seeded families establish at origin year + 15; explicit dates such as the Folk Standards supply’s 1960 establishment can override that age. Rights and public-domain flags are not lineage evidence. Comedy has no parody classification in this selection path.

## TeenPop

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 9.36% | 295 | 3416.08 | 1.26% | 8.09% | 5.34% | 291 |
| 1002 | 1/4/1963; 2/1/1963 | 5.87% | 289 | 3132.67 | 0.32% | 5.55% | 5.13% | 292 |
| 42001 | 1/4/1963; 2/1/1963 | 3.82% | 299 | 2872.58 | 0.87% | 2.95% | 10.99% | 281 |
| 42002 | 1/4/1963; 2/1/1963 | 2.48% | 300 | 3071.08 | 0.00% | 2.48% | 10.03% | 288 |
| 42003 | 1/4/1963; 2/1/1963 | 9.45% | 284 | 2857.50 | 0.00% | 9.45% | 4.90% | 292 |
| 42004 | 1/4/1963; 2/1/1963 | 10.62% | 282 | 3233.58 | 0.33% | 10.29% | 7.47% | 293 |

Pooled unsigned: **7.01%**, 1749 observed slots, 18583.50 weighted slots. Equal-seed mean 6.93%; range 2.48–10.62%; sample SD 3.36 percentage points.

Inherited concentration: 64 seed/artist identities and 71 seed/song identities. Top artist 5.40% and top three 12.52% of inherited weight (0.38% / 0.88% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 12 | 15 | 15.80% | 37.46% | absent |
| 1002 | 12 | 12 | 17.32% | 45.53% | absent |
| 42001 | 9 | 9 | 25.21% | 52.70% | absent |
| 42002 | 5 | 5 | 38.47% | 77.60% | absent |
| 42003 | 13 | 11 | 11.44% | 34.33% | absent |
| 42004 | 13 | 19 | 20.49% | 43.07% | absent |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| traditionalLineage | Easy traditional | EasyListening | 8 | 0.48% |
| establishedStandard | Tin Pan Alley | TraditionalPop | 61 | 3.67% |
| establishedStandard | Brazilian songbook | BossaNova | 6 | 0.28% |
| establishedStandard | Christmas Standard | TraditionalPop | 26 | 1.58% |
| establishedStandard | Jazz Standard | Jazz | 13 | 1.00% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | TeenPop | 486 | 26.07% |
| existingCover | ArtistOriginal | Soul | 90 | 5.29% |
| existingCover | ArtistOriginal | TeenPop | 153 | 8.53% |
| existingCover | ProfessionalOffice | TeenPop | 73 | 4.33% |
| existingCover | ArtistOriginal | Jazz | 20 | 1.20% |
| existingCover | ArtistOriginal | DooWop | 37 | 2.45% |
| existingCover | RecentHit / Recent Teen Hit | TeenPop | 128 | 7.11% |
| existingCover | RecentHit / Recent Pop Hit | TraditionalPop | 59 | 3.53% |
| existingCover | ArtistOriginal | TraditionalPop | 30 | 1.85% |
| existingCover | ArtistOriginal | EasyListening | 26 | 1.35% |
| existingCover | ExternalMediaComposition / Screen instrumental | EasyListening | 256 | 15.64% |
| existingCover | ArtistOriginal | RockAndRoll | 113 | 6.34% |
| existingCover | ExternalMediaTheme / Screen instrumental | EasyListening | 33 | 1.82% |
| existingCover | ExternalMediaTheme / Stage and film songs | EasyListening | 6 | 0.28% |
| existingCover | ProfessionalOffice | TraditionalPop | 6 | 0.32% |
| existingCover | ExternalMediaComposition / Stage and film songs | EasyListening | 63 | 3.64% |
| existingCover | ProfessionalOffice | GirlGroup | 7 | 0.51% |
| existingCover | ArtistOriginal | Classical | 3 | 0.18% |
| existingCover | PreGameCatalog / Brazilian songbook | BossaNova | 9 | 0.47% |
| existingCover | PreGameStandard / Christmas Standard | TraditionalPop | 6 | 0.33% |
| existingCover | ProfessionalOffice | Bubblegum | 4 | 0.18% |
| existingCover | PreGameStandard / Jazz Standard | Jazz | 11 | 0.51% |
| existingCover | PreGameStandard / Tin Pan Alley | TraditionalPop | 6 | 0.43% |
| existingCover | ProfessionalOffice | EasyListening | 8 | 0.49% |
| existingCover | ProfessionalOffice | SunshinePop | 2 | 0.17% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

| Seed | Date | Status | Sampled artists | Mean accepted inherited pool | Distinct inherited pool songs | Artists with zero inherited pool | Mean inherited in top fit band |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 6/14/1963 | unsigned | 36 | 39.31 | 858 | 0 | 7.56 |
| 1001 | 6/14/1963 | signed | 36 | 39.84 | 853 | 0 | 9.57 |
| 1001 | 9/6/1963 | unsigned | 36 | 38.17 | 851 | 0 | 8.41 |
| 1001 | 9/6/1963 | signed | 36 | 39.41 | 874 | 0 | 9.11 |
| 1002 | 7/12/1963 | unsigned | 36 | 39.11 | 862 | 0 | 4.49 |
| 1002 | 7/12/1963 | signed | 36 | 36.43 | 812 | 0 | 4.09 |
| 1002 | 10/4/1963 | unsigned | 36 | 38.60 | 863 | 0 | 4.61 |
| 1002 | 10/4/1963 | signed | 36 | 37.50 | 822 | 0 | 4.61 |
| 1001 | 1/4/1963 | unsigned | 36 | 39.41 | 860 | 0 | 5.90 |
| 1001 | 1/4/1963 | signed | 36 | 38.46 | 843 | 0 | 4.09 |
| 1001 | 2/1/1963 | unsigned | 36 | 39.51 | 861 | 0 | 7.39 |
| 1001 | 2/1/1963 | signed | 36 | 38.74 | 840 | 0 | 5.04 |
| 1002 | 1/4/1963 | unsigned | 36 | 37.90 | 840 | 0 | 6.60 |
| 1002 | 1/4/1963 | signed | 36 | 38.31 | 858 | 0 | 5.62 |
| 1002 | 2/1/1963 | unsigned | 36 | 38.04 | 842 | 0 | 6.23 |
| 1002 | 2/1/1963 | signed | 36 | 37.99 | 855 | 0 | 5.56 |
| 42001 | 1/4/1963 | signed | 36 | 38.55 | 846 | 0 | 7.00 |
| 42001 | 1/4/1963 | unsigned | 36 | 41.77 | 862 | 0 | 6.41 |
| 42001 | 2/1/1963 | signed | 36 | 38.19 | 843 | 0 | 6.98 |
| 42001 | 2/1/1963 | unsigned | 36 | 40.61 | 870 | 0 | 5.45 |
| 42002 | 1/4/1963 | unsigned | 36 | 39.56 | 882 | 0 | 5.26 |
| 42002 | 1/4/1963 | signed | 36 | 38.10 | 841 | 0 | 7.15 |
| 42002 | 2/1/1963 | unsigned | 36 | 39.30 | 879 | 0 | 5.75 |
| 42002 | 2/1/1963 | signed | 36 | 37.95 | 834 | 0 | 8.85 |
| 42003 | 1/4/1963 | signed | 36 | 41.01 | 894 | 0 | 5.45 |
| 42003 | 1/4/1963 | unsigned | 36 | 37.87 | 827 | 0 | 3.17 |
| 42003 | 2/1/1963 | signed | 36 | 41.19 | 883 | 0 | 5.11 |
| 42003 | 2/1/1963 | unsigned | 36 | 38.34 | 844 | 0 | 3.32 |
| 42004 | 1/4/1963 | signed | 36 | 38.83 | 866 | 0 | 6.11 |
| 42004 | 1/4/1963 | unsigned | 36 | 40.88 | 889 | 0 | 7.08 |
| 42004 | 2/1/1963 | signed | 36 | 39.42 | 875 | 0 | 4.98 |
| 42004 | 2/1/1963 | unsigned | 36 | 40.87 | 887 | 0 | 5.73 |

Pool makeup by category, seed family and primary/secondary genre, with accessed and accepted counts, is in `poolMeasures`. Global catalogue category counts are retained separately in `catalog`; they are not artist-specific access.

## Country

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 10.77% | 286 | 5217.67 | 3.13% | 7.65% | 14.33% | 274 |
| 1002 | 1/4/1963; 2/1/1963 | 9.80% | 292 | 5164.50 | 0.77% | 9.03% | 11.16% | 283 |
| 42001 | 1/4/1963; 2/1/1963 | 4.67% | 282 | 4999.75 | 0.28% | 4.39% | 9.48% | 276 |
| 42002 | 1/4/1963; 2/1/1963 | 18.58% | 290 | 5102.42 | 1.08% | 17.50% | 9.16% | 288 |
| 42003 | 1/4/1963; 2/1/1963 | 5.69% | 289 | 4828.33 | 0.59% | 5.09% | 14.25% | 297 |
| 42004 | 1/4/1963; 2/1/1963 | 10.61% | 285 | 4935.92 | 0.00% | 10.61% | 11.26% | 289 |

Pooled unsigned: **10.08%**, 1724 observed slots, 30248.58 weighted slots. Equal-seed mean 10.02%; range 4.67–18.58%; sample SD 4.93 percentage points.

Inherited concentration: 83 seed/artist identities and 89 seed/song identities. Top artist 5.26% and top three 12.42% of inherited weight (0.53% / 1.25% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 15 | 16 | 14.73% | 35.13% | absent |
| 1002 | 19 | 16 | 11.50% | 30.99% | absent |
| 42001 | 10 | 10 | 21.42% | 51.41% | absent |
| 42002 | 17 | 27 | 16.90% | 39.44% | absent |
| 42003 | 8 | 9 | 20.89% | 54.46% | absent |
| 42004 | 14 | 11 | 21.27% | 48.85% | absent |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| traditionalLineage | Folk Traditional | Folk | 14 | 0.81% |
| establishedStandard | Country Standard | Country | 135 | 8.04% |
| traditionalLineage | Easy traditional | EasyListening | 2 | 0.18% |
| establishedStandard | Tin Pan Alley | TraditionalPop | 7 | 0.33% |
| establishedStandard | Christmas Standard | TraditionalPop | 6 | 0.41% |
| establishedStandard | Folk Standards | Folk | 5 | 0.31% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | Country | 503 | 28.59% |
| existingCover | ArtistOriginal | Folk | 83 | 4.73% |
| existingCover | ExternalMediaComposition / Screen instrumental | EasyListening | 219 | 13.26% |
| existingCover | ArtistOriginal | TraditionalPop | 21 | 1.30% |
| existingCover | ArtistOriginal | Country | 523 | 29.95% |
| existingCover | ProfessionalOffice | Country | 37 | 2.00% |
| existingCover | RecentHit / Recent Pop Hit | TraditionalPop | 9 | 0.60% |
| existingCover | RecentHit / Recent Country Hit | Country | 38 | 2.27% |
| existingCover | ProfessionalOffice | TeenPop | 6 | 0.35% |
| existingCover | ProfessionalOffice | TraditionalPop | 5 | 0.31% |
| existingCover | ArtistOriginal | TeenPop | 11 | 0.66% |
| existingCover | ProfessionalOffice | EasyListening | 1 | 0.04% |
| existingCover | PreGameStandard / Country Standard | Country | 29 | 1.85% |
| existingCover | ExternalMediaTheme / Screen instrumental | EasyListening | 20 | 1.11% |
| existingCover | ProfessionalOffice | Folk | 1 | 0.04% |
| existingCover | ExternalMediaComposition / Stage and film songs | EasyListening | 10 | 0.58% |
| existingCover | ArtistOriginal | EasyListening | 4 | 0.24% |
| existingCover | ExternalMediaTheme / Stage and film songs | EasyListening | 6 | 0.44% |
| existingCover | RecentHit / Recent Teen Hit | TeenPop | 2 | 0.09% |
| existingCover | ArtistOriginal | Gospel | 9 | 0.61% |
| existingCover | ArtistOriginal | Classical | 3 | 0.14% |
| existingCover | ArtistOriginal | TexMex | 7 | 0.31% |
| existingCover | ProfessionalOffice | SunshinePop | 2 | 0.09% |
| existingCover | ProfessionalOffice | GirlGroup | 1 | 0.04% |
| existingCover | PreGameStandard / Christmas Standard | TraditionalPop | 1 | 0.09% |
| existingCover | PreGameStandard / Tin Pan Alley | TraditionalPop | 4 | 0.24% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

| Seed | Date | Status | Sampled artists | Mean accepted inherited pool | Distinct inherited pool songs | Artists with zero inherited pool | Mean inherited in top fit band |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 6/14/1963 | unsigned | 36 | 130.45 | 1302 | 0 | 4.18 |
| 1001 | 6/14/1963 | signed | 36 | 127.66 | 1304 | 0 | 5.67 |
| 1001 | 9/6/1963 | signed | 36 | 126.57 | 1289 | 0 | 6.18 |
| 1001 | 9/6/1963 | unsigned | 36 | 133.16 | 1321 | 0 | 5.87 |
| 1002 | 7/12/1963 | signed | 36 | 129.35 | 1282 | 0 | 5.45 |
| 1002 | 7/12/1963 | unsigned | 36 | 127.32 | 1279 | 0 | 4.63 |
| 1002 | 10/4/1963 | signed | 36 | 130.34 | 1277 | 0 | 5.65 |
| 1002 | 10/4/1963 | unsigned | 36 | 128.13 | 1273 | 0 | 5.36 |
| 1001 | 1/4/1963 | unsigned | 36 | 128.46 | 1291 | 0 | 4.10 |
| 1001 | 1/4/1963 | signed | 36 | 128.96 | 1312 | 0 | 5.17 |
| 1001 | 2/1/1963 | unsigned | 36 | 127.75 | 1297 | 0 | 3.90 |
| 1001 | 2/1/1963 | signed | 36 | 128.75 | 1299 | 0 | 4.23 |
| 1002 | 1/4/1963 | unsigned | 36 | 129.88 | 1297 | 0 | 4.64 |
| 1002 | 1/4/1963 | signed | 36 | 126.53 | 1287 | 0 | 4.85 |
| 1002 | 2/1/1963 | signed | 36 | 126.73 | 1285 | 0 | 5.58 |
| 1002 | 2/1/1963 | unsigned | 36 | 129.33 | 1289 | 0 | 4.23 |
| 42001 | 1/4/1963 | unsigned | 36 | 127.47 | 1295 | 0 | 4.44 |
| 42001 | 1/4/1963 | signed | 36 | 124.78 | 1296 | 0 | 5.18 |
| 42001 | 2/1/1963 | unsigned | 36 | 125.71 | 1287 | 0 | 5.58 |
| 42001 | 2/1/1963 | signed | 36 | 125.55 | 1299 | 0 | 5.66 |
| 42002 | 1/4/1963 | unsigned | 36 | 130.70 | 1305 | 0 | 10.17 |
| 42002 | 1/4/1963 | signed | 36 | 130.11 | 1300 | 0 | 7.94 |
| 42002 | 2/1/1963 | unsigned | 36 | 130.29 | 1306 | 0 | 9.07 |
| 42002 | 2/1/1963 | signed | 36 | 131.11 | 1307 | 0 | 7.69 |
| 42003 | 1/4/1963 | signed | 36 | 132.92 | 1309 | 0 | 5.31 |
| 42003 | 1/4/1963 | unsigned | 36 | 131.16 | 1286 | 0 | 3.54 |
| 42003 | 2/1/1963 | signed | 36 | 132.34 | 1303 | 0 | 5.21 |
| 42003 | 2/1/1963 | unsigned | 36 | 131.57 | 1282 | 0 | 3.92 |
| 42004 | 1/4/1963 | signed | 36 | 128.72 | 1285 | 0 | 4.31 |
| 42004 | 1/4/1963 | unsigned | 36 | 129.61 | 1300 | 0 | 5.96 |
| 42004 | 2/1/1963 | signed | 36 | 130.01 | 1303 | 0 | 4.56 |
| 42004 | 2/1/1963 | unsigned | 36 | 129.36 | 1299 | 0 | 6.09 |

Pool makeup by category, seed family and primary/secondary genre, with accessed and accepted counts, is in `poolMeasures`. Global catalogue category counts are retained separately in `catalog`; they are not artist-specific access.

## Comedy

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 43.48% | 285 | 1746.58 | 18.02% | 25.46% | 38.67% | 288 |
| 1002 | 1/4/1963; 2/1/1963 | 37.43% | 289 | 1621.75 | 10.89% | 26.54% | 38.11% | 285 |
| 42001 | 1/4/1963; 2/1/1963 | 41.48% | 277 | 1611.50 | 12.93% | 28.55% | 36.13% | 285 |
| 42002 | 1/4/1963; 2/1/1963 | 42.30% | 291 | 1734.75 | 15.73% | 26.56% | 35.10% | 284 |
| 42003 | 1/4/1963; 2/1/1963 | 37.26% | 281 | 1617.42 | 12.03% | 25.23% | 31.23% | 279 |
| 42004 | 1/4/1963; 2/1/1963 | 42.85% | 278 | 1510.17 | 16.34% | 26.50% | 44.97% | 284 |

Pooled unsigned: **40.83%**, 1701 observed slots, 9842.17 weighted slots. Equal-seed mean 40.80%; range 37.26–43.48%; sample SD 2.76 percentage points.

Inherited concentration: 213 seed/artist identities and 207 seed/song identities. Top artist 1.64% and top three 4.52% of inherited weight (0.67% / 1.85% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 36 | 39 | 8.67% | 23.55% | 42.86% (42 slots) |
| 1002 | 35 | 33 | 7.15% | 21.39% | 34.29% (35 slots) |
| 42001 | 33 | 32 | 7.48% | 19.96% | 36.96% (46 slots) |
| 42002 | 39 | 34 | 6.54% | 19.13% | 39.62% (53 slots) |
| 42003 | 32 | 33 | 7.29% | 19.44% | 41.67% (36 slots) |
| 42004 | 38 | 36 | 7.19% | 19.09% | 55.56% (9 slots) |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| establishedStandard | Comedy routines | Comedy | 437 | 26.46% |
| traditionalLineage | Children songs | Childrens | 239 | 14.37% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | Comedy | 454 | 25.40% |
| existingCover | PreGameCatalog / Comedy routines | Comedy | 510 | 30.96% |
| existingCover | ArtistOriginal | Classical | 61 | 2.82% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

## Classical

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 54.32% | 282 | 1436.92 | 0.00% | 54.32% | 43.46% | 271 |
| 1002 | 1/4/1963; 2/1/1963 | 42.68% | 282 | 1415.50 | 0.00% | 42.68% | 42.07% | 254 |
| 42001 | 1/4/1963; 2/1/1963 | 45.07% | 283 | 1322.92 | 0.00% | 45.07% | 38.75% | 282 |
| 42002 | 1/4/1963; 2/1/1963 | 49.81% | 293 | 1631.50 | 0.00% | 49.81% | 47.76% | 286 |
| 42003 | 1/4/1963; 2/1/1963 | 43.16% | 288 | 1388.92 | 0.00% | 43.16% | 33.86% | 265 |
| 42004 | 1/4/1963; 2/1/1963 | 54.03% | 293 | 1398.08 | 0.00% | 54.03% | 46.80% | 264 |

Pooled unsigned: **48.27%**, 1721 observed slots, 8593.83 weighted slots. Equal-seed mean 48.18%; range 42.68–54.32%; sample SD 5.29 percentage points.

Inherited concentration: 191 seed/artist identities and 235 seed/song identities. Top artist 1.80% and top three 4.65% of inherited weight (0.87% / 2.25% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 32 | 37 | 6.09% | 17.08% | 71.21% (119 slots) |
| 1002 | 29 | 36 | 10.32% | 27.12% | 49.86% (130 slots) |
| 42001 | 32 | 38 | 7.06% | 19.73% | 64.75% (100 slots) |
| 42002 | 35 | 42 | 9.19% | 22.97% | 71.13% (149 slots) |
| 42003 | 27 | 35 | 7.72% | 20.98% | 63.76% (126 slots) |
| 42004 | 36 | 47 | 6.38% | 17.30% | 53.41% (84 slots) |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| establishedStandard | Classical works | Classical | 810 | 48.27% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | Classical | 473 | 26.09% |
| existingCover | ArtistOriginal | Classical | 395 | 23.22% |
| existingCover | PreGameCatalog / Classical works | Classical | 43 | 2.41% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

## Childrens

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 70.11% | 283 | 1330.67 | 69.51% | 0.60% | 57.47% | 240 |
| 1002 | 1/4/1963; 2/1/1963 | 65.54% | 283 | 1463.33 | 63.34% | 2.20% | 64.99% | 273 |
| 42001 | 1/4/1963; 2/1/1963 | 67.84% | 289 | 1426.00 | 65.18% | 2.66% | 68.63% | 289 |
| 42002 | 1/4/1963; 2/1/1963 | 72.63% | 285 | 1309.00 | 69.33% | 3.29% | 68.81% | 298 |
| 42003 | 1/4/1963; 2/1/1963 | 64.28% | 283 | 1486.50 | 62.89% | 1.39% | 62.21% | 288 |
| 42004 | 1/4/1963; 2/1/1963 | 68.71% | 285 | 1423.08 | 65.42% | 3.29% | 72.40% | 285 |

Pooled unsigned: **68.06%**, 1708 observed slots, 8438.58 weighted slots. Equal-seed mean 68.18%; range 64.28–72.63%; sample SD 3.04 percentage points.

Inherited concentration: 240 seed/artist identities and 353 seed/song identities. Top artist 1.17% and top three 3.30% of inherited weight (0.79% / 2.25% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 40 | 56 | 5.40% | 15.99% | 73.33% (30 slots) |
| 1002 | 42 | 61 | 6.99% | 17.48% | 57.14% (35 slots) |
| 42001 | 42 | 62 | 4.80% | 14.21% | 84.21% (19 slots) |
| 42002 | 38 | 61 | 5.00% | 13.76% | 77.27% (22 slots) |
| 42003 | 39 | 54 | 6.42% | 17.65% | 61.29% (31 slots) |
| 42004 | 39 | 59 | 5.73% | 15.55% | 78.95% (19 slots) |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| traditionalLineage | Children songs | Childrens | 1099 | 65.82% |
| establishedStandard | Comedy routines | Comedy | 43 | 2.24% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | Childrens | 469 | 26.16% |
| existingCover | PreGameCatalog / Comedy routines | Comedy | 73 | 4.39% |
| existingCover | ArtistOriginal | Classical | 24 | 1.39% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

## TraditionalPop

Matched evolved worlds:

| Seed | Retained 1963 dates | Unsigned inherited | Observed slots | Weighted slots | Lineage | Established | Signed inherited | Signed slots |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1001 | 1/4/1963; 2/1/1963 | 7.65% | 293 | 2636.17 | 0.00% | 7.65% | 12.39% | 295 |
| 1002 | 1/4/1963; 2/1/1963 | 14.20% | 276 | 2661.92 | 0.00% | 14.20% | 9.82% | 282 |
| 42001 | 1/4/1963; 2/1/1963 | 18.57% | 288 | 2655.25 | 0.76% | 17.81% | 16.22% | 294 |
| 42002 | 1/4/1963; 2/1/1963 | 24.18% | 287 | 2959.42 | 1.81% | 22.37% | 15.40% | 292 |
| 42003 | 1/4/1963; 2/1/1963 | 19.99% | 298 | 2794.08 | 0.00% | 19.99% | 11.91% | 286 |
| 42004 | 1/4/1963; 2/1/1963 | 14.71% | 293 | 2776.75 | 0.00% | 14.71% | 14.20% | 298 |

Pooled unsigned: **16.72%**, 1735 observed slots, 16483.58 weighted slots. Equal-seed mean 16.55%; range 7.65–24.18%; sample SD 5.70 percentage points.

Inherited concentration: 123 seed/artist identities and 167 seed/song identities. Top artist 3.20% and top three 9.38% of inherited weight (0.54% / 1.57% of all slots). These are sample concentration estimates.

| Seed | Inherited artists | Inherited songs | Top 1 share of inherited | Top 3 share of inherited | Startup unsigned |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | 15 | 17 | 14.68% | 35.72% | absent |
| 1002 | 17 | 24 | 22.75% | 37.92% | absent |
| 42001 | 23 | 35 | 10.11% | 25.88% | absent |
| 42002 | 30 | 36 | 11.26% | 27.11% | absent |
| 42003 | 18 | 29 | 15.80% | 39.57% | absent |
| 42004 | 20 | 26 | 20.63% | 35.99% | absent |

Inherited slot families:

| Category | Seed family | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| establishedStandard | Jazz Standard | Jazz | 109 | 6.03% |
| establishedStandard | Christmas Standard | TraditionalPop | 37 | 1.96% |
| establishedStandard | Tin Pan Alley | TraditionalPop | 139 | 8.24% |
| traditionalLineage | Easy traditional | EasyListening | 8 | 0.45% |
| establishedStandard | Brazilian songbook | BossaNova | 1 | 0.04% |

Non-inherited slots:

| Category | Seed family / origin | Song genre | Observed slots | Weighted share of all slots |
| --- | --- | --- | ---: | ---: |
| newlyAuthored | ArtistOriginal | TraditionalPop | 465 | 25.68% |
| existingCover | ProfessionalOffice | TraditionalPop | 18 | 1.18% |
| existingCover | ExternalMediaComposition / Screen instrumental | EasyListening | 596 | 35.23% |
| existingCover | ExternalMediaTheme / Screen instrumental | EasyListening | 55 | 3.26% |
| existingCover | PreGameStandard / Jazz Standard | Jazz | 35 | 2.32% |
| existingCover | ExternalMediaComposition / Stage and film songs | EasyListening | 55 | 3.21% |
| existingCover | ArtistOriginal | EasyListening | 2 | 0.09% |
| existingCover | ArtistOriginal | Jazz | 15 | 0.91% |
| newlyAuthored | ProfessionalOffice | TraditionalPop | 50 | 2.94% |
| existingCover | ArtistOriginal | TraditionalPop | 60 | 3.28% |
| existingCover | ArtistOriginal | Classical | 8 | 0.51% |
| existingCover | RecentHit / Recent Pop Hit | TraditionalPop | 36 | 2.20% |
| existingCover | PreGameStandard / Tin Pan Alley | TraditionalPop | 19 | 0.94% |
| existingCover | ArtistOriginal | Folk | 4 | 0.22% |
| existingCover | RecentHit / Recent Teen Hit | TeenPop | 9 | 0.58% |
| existingCover | PreGameCatalog / Brazilian songbook | BossaNova | 4 | 0.25% |
| existingCover | ArtistOriginal | TeenPop | 7 | 0.38% |
| existingCover | ArtistOriginal | Blues | 1 | 0.04% |
| existingCover | ProfessionalOffice | Bubblegum | 1 | 0.04% |
| existingCover | ProfessionalOffice | TeenPop | 1 | 0.04% |

Per-date/status/cohort/writing-bin partitions, rights measures, sampled artist roster, full inherited and non-inherited slot rows, and concentration lists are in the validation JSON. The companion CSV includes each slot’s artist/song name and ID, category, year, rights status and weight.

## Exact original snapshot trace

Baseline enriched replays match the date/artist/slot/song/category/weight values in the six-genre subset of the original census CSVs exactly.

| Seed | Genre | Unsigned inherited | Slots |
| --- | --- | ---: | ---: |
| 1001 | TraditionalPop | 27.63% | 38 |
| 1001 | TeenPop | 15.69% | 34 |
| 1001 | Classical | 62.52% | 40 |
| 1001 | Childrens | 69.59% | 36 |
| 1001 | Comedy | 40.88% | 36 |
| 1001 | Country | 8.72% | 31 |
| 1002 | TeenPop | 0.00% | 37 |
| 1002 | Comedy | 19.93% | 39 |
| 1002 | TraditionalPop | 39.88% | 37 |
| 1002 | Country | 26.69% | 39 |
| 1002 | Childrens | 69.92% | 37 |
| 1002 | Classical | 54.66% | 35 |

Seed 1001 TeenPop inherited rows:

| Artist | Song | Category | Year | Rights | Slot weight |
| --- | --- | --- | ---: | --- | ---: |
| Robin and the Teens (artist_02418) | The Calm Passage (song_0000828) | establishedStandard | 1917 | PublicDomain; public domain flag | 116.00 |
| Brian and the Teens (artist_04599) | In Rome (song_0000319) | establishedStandard | 1905 | ExternalPublisher | 116.00 |
| Brian and the Teens (artist_04599) | Precious Precious (song_0000665) | establishedStandard | 1947 | ExternalPublisher | 116.00 |
| Lloyd and the Wonders (artist_04809) | In the Riviera (song_0000409) | establishedStandard | 1944 | ExternalPublisher | 69.33 |

## Excluded attempts

The initial restricted-runtime attempt crashed before engine initialization and produced no census. All four partial February censuses hit their 900-second time budgets. Their partial rows are excluded; the completed January censuses remain valid, and complete February checkpoint replays replace the partial samples.

- genre-followup-start-1001: No census manifest.
- genre-followup-v1-late-42001 2/1/1963: Incomplete or disabled census.
- genre-followup-v1-late-42002 2/1/1963: Incomplete or disabled census.
- genre-followup-v1-late-42003 2/1/1963: Incomplete or disabled census.
- genre-followup-v1-late-42004 2/1/1963: Incomplete or disabled census.

## Reproduction and validation

```powershell
# Build the existing checkout, including album-composition work.
dotnet build --no-restore -v quiet
# Same startup, baseline, and 1963 collection, using a fresh run tag.
& .\SimTools\run-genre-repertoire-followup.ps1 -Phase startup -RunTag reproduce
& .\SimTools\run-genre-repertoire-followup.ps1 -Seeds @(1001,1002) -Phase baseline -RunTag reproduce
& .\SimTools\run-genre-repertoire-followup.ps1 -Seeds @(1001,1002) -Phase matched -RunTag reproduce
& .\SimTools\run-genre-repertoire-followup.ps1 -Phase late -RunTag reproduce
& .\SimTools\check-genre-repertoire-followup.ps1 -RunTag reproduce
# Re-analyze the exact retained inputs for this report.
node SimTools/analyze-genre-repertoire-review.mjs --follow-up genre-followup-start-1001 genre-followup-start-1001-retry1 genre-followup-v1-start-1002 genre-followup-v1-start-42001 genre-followup-v1-start-42002 genre-followup-v1-start-42003 genre-followup-v1-start-42004 genre-followup-v1-baseline-1001 genre-followup-v1-baseline-1002 genre-followup-v1-late-1001-s8 genre-followup-v1-late-1001-s9 genre-followup-v1-late-1002-s8 genre-followup-v1-late-1002-s9 genre-followup-v1-matched-1001 genre-followup-v1-matched-1002 genre-followup-v1-late-42001 genre-followup-v1-late-42001-resume1 genre-followup-v1-late-42002 genre-followup-v1-late-42002-resume1 genre-followup-v1-late-42003 genre-followup-v1-late-42003-resume1 genre-followup-v1-late-42004 genre-followup-v1-late-42004-resume1
```

For a fresh collection, replace the retained prefixes in the analyzer invocation with the corresponding `genre-followup-reproduce-*` prefixes, including any automatic `-resumeN` slices. The analyzer also requires the saved build evidence manifest and matching assembly hash. It keeps the original review files intact.

The final project build passes, and `--folk-easy-check` passes on 1001 and 1002. The validation JSON records build/check evidence, full invocations, input CSV/manifest/log SHA-256 hashes, loaded checkpoint hashes and source hashes. Completion, sampling weights, set/slot agreement, category partition, no future material, no duplicate observations, detailed-row parity, exact baseline replay, matching build/check assemblies and unchanged genre-policy-table assertions must pass before outputs are written.
