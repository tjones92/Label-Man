# Genre repertoire review and cast/film album compositions

October 5, 2026. The prior work is committed and pushed as 2f492e5 on codex/polar-song-data-model. This follow-up adds album compositions and records a bounded genre review; it does not retune additional genre targets.

## Previously flagged genres

The quoted percentages reproduce the old 1960–63 unsigned legacy census in PolarRepertoireReport.md. They are historical diagnostic artifacts, not current measurements. The new columns use the corrected traditional-lineage plus established-as-of-year standard numerator, weighted by cross-sectional population/sample, over all filled unsigned live slots. Rights status is reported separately in GenreRepertoireReviewValidation.json. Different definitions and observation windows mean these are not exact before/after effect estimates.

| Genre | Old legacy reference | Seed 1001, September 6, 1963 | Seed 1002, October 4, 1963 |
| --- | ---: | ---: | ---: |
| BossaNova | 77.21% | 18.74% (47 slots) | 28.20% (49 slots) |
| ContemporaryFolk | 78.00% | 55.23% (34 slots) | 49.88% (37 slots) |
| Country | 76.60% | 8.72% (31 slots) | 26.69% (39 slots) |
| RnB | 45.14% | 12.06% (37 slots) | 2.44% (33 slots) |
| SurfRock | 2.31% | 0.00% (34 slots) | 0.00% (33 slots) |

Bossa Nova, Country and RnB no longer reproduce their old excessive shares in these completed samples. Contemporary Folk was explicitly included in the Folk pass: 60% traditional + 25% standards in 1960–61, then 40% + 15% from 1962 onward. Its roughly 50–55% late sample is consistent with that existing policy. SurfRock is already rare; zero selections in a small sample does not establish zero exceptions in the population. No blanket elimination of instrumental adaptations was added.

Historical sources support the qualitative distinctions, not exact numeric quotas: the Library of Congress discusses new Jobim compositions in [bossa nova](https://blogs.loc.gov/now-see-hear/2021/08/from-the-recording-registry-the-girl-from-ipanema-1963/), while the Country Music Hall of Fame documents [Harlan Howard's professional songwriting](https://countrymusichalloffame.org/hall-of-fame/harlan-howard) and [Hank Cochran's staff-writing work](https://countrymusichalloffame.org/hall-of-fame/hank-cochran). Songs written for another performer are contemporary material, not automatically standards or artist originals. The Library of Congress also describes [Motown's songwriting](https://blogs.loc.gov/loc/2022/04/motowns-songwriting-stars-and-reach-out-ill-be-there/) and the traditional tune [Misirlou becoming surf repertoire](https://wwws.loc.gov/folklife/sampler/FLaudio.html).

## Other genres and remaining questions

| Unsigned genre | Seed 1001, September 1963 | Seed 1002, October 1963 |
| --- | ---: | ---: |
| Blues | 37.96% (35 slots) | 36.11% (41 slots) |
| BossaNova | 18.74% (47 slots) | 28.20% (49 slots) |
| BritishBeat | 0.00% (12 slots) | 0.00% (9 slots) |
| Childrens | 69.59% (36 slots) | 69.92% (37 slots) |
| Classical | 62.52% (40 slots) | 54.66% (35 slots) |
| Comedy | 40.88% (36 slots) | 19.93% (39 slots) |
| ContemporaryFolk | 55.23% (34 slots) | 49.88% (37 slots) |
| Country | 8.72% (31 slots) | 26.69% (39 slots) |
| DooWop | 14.76% (36 slots) | 12.99% (35 slots) |
| EasyListening | 75.31% (76 slots) | 72.83% (71 slots) |
| Folk | 47.09% (37 slots) | 58.52% (34 slots) |
| GarageRock | 0.00% (35 slots) | 0.00% (38 slots) |
| Gospel | 80.73% (38 slots) | 74.79% (34 slots) |
| Jazz | 54.34% (49 slots) | 55.14% (47 slots) |
| LatinPop | 13.31% (35 slots) | 19.72% (35 slots) |
| RnB | 12.06% (37 slots) | 2.44% (33 slots) |
| RockAndRoll | 2.21% (37 slots) | 4.44% (34 slots) |
| Soul | 14.81% (36 slots) | 5.69% (39 slots) |
| SurfRock | 0.00% (34 slots) | 0.00% (33 slots) |
| TeenPop | 15.69% (34 slots) | 0.00% (37 slots) |
| TexMex | 18.37% (38 slots) | 13.44% (33 slots) |
| TraditionalPop | 27.63% (38 slots) | 39.88% (37 slots) |

The largest inherited shares remain in the intentionally tuned Gospel/Easy Listening scenes and in Classical/Children's material. This scan found no additional convincing genre-wide excess requiring an immediate numerical change. It covers observed early-1960s genres, not later-emerging genres or a decade.

Country has substantial seed variation (approximately 9% versus 27% unsigned); the low seed warrants a larger follow-up before selecting a target. Signed Jazz standards interpreters also fall below the old provisional band in these snapshots. Cohort/status estimates, including Bossa Nova's older-songbook versus composer-led split, are retained in the validation JSON. These are potential underrepresentation issues rather than the excess the user flagged. The table's older provisional bands are not acceptance targets for the subsequently revised Gospel/Folk/Easy Listening policies.

## Album composition addition and singles boundary

Released StageCast, FilmScore and FilmSong albums now get separate AlbumTrack and SongComposition identities. The existing representative theme keeps its ID and keyed shape and becomes the first actual album track. Other songs/cues receive their own stable IDs, external-publisher rights, source album/type and recording masters. FilmScore remains instrumental; cast and film songs remain vocal. Runtime/three minutes supplies 8–16 cuts (12 when runtime is unavailable), an authored procedural sizing rule rather than a historical statistic. Titles are procedural theme/cue/song labels.

Only album tracks are created. No Record singles, lead-single references or album promo projects are created. The promo extractor explicitly returns null for external-media soundtrack/cast albums. Added cuts are removed from non-Easy Listening recording pools before sampling so those catalogue positions and draw inputs stay unchanged. Easy Listening may record separate interpretations through its existing release pipeline. Existing representative themes keep their prior access. Live repertoire can access the new cuts through existing scene rules.

Album pooled appeal, scalar recording quality, licensing fees, production spend, market demand and the weekly origination draw are not recomputed. Authoring consumes keyed hashes rather than global randomness. Existing active external-media albums with empty track lists backfill on the next enabled simulation week using their original release date; populated albums are retained. Repeated admission is idempotent. The same ordinary composition/master/album save data stores the addition, with the new origin enum appended for compatibility.

## Verification and limits

The build passes with four existing warnings. Final FolkEasyListeningChecks pass on seeds 1001 and 1002. They cover all three media types, distinct compositions/masters, instrumental versus vocal context, rights/source metadata, album and composition serialization, repeated release, album economics, no chart-record creation, explicit promo-extraction refusal, unchanged non-Easy recording pool ordering and Easy Listening recording access. The pre-existing Folk/Easy source-selection and role checks also pass.

Completed censuses include startup on both seeds and one retained evolving-world snapshot per seed, with sampling weights, slot/set agreement, category partitions and future-material checks passing. Restored late snapshots pass world digest round trips before simulation resumes. Invocation/CSV hashes and per-status/per-cohort measures are in GenreRepertoireReviewValidation.json. The first 1964 attempt produced no eligible census because the harness ends in 1963; an initial 1963 sample hit its time limit. Neither attempt enters the estimates. Smaller completed samples replace them.

This is a bounded review, not a new 48-month trajectory or decade economic-neutrality test. Original cast/film singles are structurally excluded; no claim of byte-identical future singles charts is made, since intended Easy Listening interpretations can affect charts. The old retained Folk/Easy 48-month results remain documented in FolkEasyListeningLiveReport.md. No hold-out seed was used.

Reproduce the retained complete review with:

```powershell
node SimTools/analyze-genre-repertoire-review.mjs genre-review-current-start-1001 genre-review-final-start-1002 genre-review-final-1963-1001 genre-review-final-1963-1002
dotnet build --no-restore -v quiet
# Run SaveLoadRoundTripRunner.tscn with --folk-easy-check for seeds 1001 and 1002.
```
