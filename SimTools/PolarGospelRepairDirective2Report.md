# Gospel repair directive 2: implementation and development results

October 4, 2026. Findings first; development seeds 1001 and 1002 only. A and B remain implemented with k=4. No archetype/center constants, bands, quotas, selector weights, preference rules or production tie-breaks were tuned.

**The repair is not ready for hold-out validation.** Fresh Q3 Gospel results are seed 1001, H off: 1432/3303 (43.35%) fails; seed 1001, H on: 1428/3305 (43.21%) fails; seed 1002, H off: 1222/2898 (42.17%) fails; seed 1002, H on: 1345/2889 (46.56%) fails. The bounded cross-genre quarterly panels contain 12 new range-failure cells. These are descriptive cells, including repeated quarters, rather than independent population estimates. No hold-out approval was requested or assumed.

Context metadata and all-song shape variation are implemented and tested. Fixed-world Gospel selection lines are byte-exact against retained evidence for every retained variant. Removing only the six added save fields reproduces each old capture hash exactly. Actors, pools, originals and requested covers are unchanged; each probe also requires its full new capture hash to remain unchanged before/after observation. Original caches remain historical recording facts.

| Frame | Seconds | Retained selection lines | Old capture hash equals projection |
| --- | --- | --- | --- |
| 1001 1/1/1960 | 8.94 | byte-exact | yes |
| 1002 1/1/1960 | 7.95 | byte-exact | yes |
| 1001 3/10/1961 | 59.37 | byte-exact | yes |
| 1002 2/10/1961 | 47.92 | byte-exact | yes |

## Context coverage (Package D)

Composition fields are `contentContext` (unknown/sacred/secular/mixed) and `contextAuthorshipEvidence`. Explicit authoring/import evidence establishes context; musical archetype, title, origin, publishing scene and genre do not establish it. `AuthorContext` requires evidence. Covers and scouting reuse composition IDs. The selector has no context-fit term and does not consume these fields.

| Retained frame | Distinct accessible compositions | unknown | sacred | secular | mixed |
| --- | --- | --- | --- | --- | --- |
| 1001 1/1/1960 | 5038 | 5038 | 0 | 0 | 0 |
| 1002 1/1/1960 | 4939 | 4939 | 0 | 0 | 0 |
| 1001 3/10/1961 | 14031 | 14031 | 0 | 0 | 0 |
| 1002 2/10/1961 | 13171 | 13171 | 0 | 0 | 0 |

Fresh authoring tags exactly 350 Gospel Standard compositions with `seeded-authored-fixture:Gospel Standard`. Each is checked against membership in that authored songbook before tagging. All other seeded material, including R&B Catalog, ArtistOriginal and Folk, stays unknown. Older frames remain unknown; the sacred-only mask on those retained frames has no eligible observations.

| Seed | Authored sacred fixtures | Sacred-only covers | Fixed originals | Inherited / all slots |
| --- | --- | --- | --- | --- |
| 1001 | 350 | 199 | 23 | 157/222 (70.72%) |
| 1002 | 350 | 145 | 18 | 110/163 (67.48%) |

This strict sacred-only sensitivity is a fixture diagnostic on the authored songbook, **not a population estimate or a proposed eligibility rule**. Some authored songs are too young to be established standards, so sacred-only and inherited-only are different measures. Save/load, cover inheritance, older-save unknown defaults and exact fixture-size checks pass. Performance role and repertoire orientation remain out of scope.

## Within-bucket ordering (Package E; report only)

The first bucket uses the production integer bucket calculation with width 0.025. Within it, preference = stable per-act/song base draw [0,1), plus sinusoidal drift ±0.005, genre draw ×0.08, commercial hook ×0.06 and age-specific familiarity ×0.08. Acts changing genre interpolate the genre draw over a year. After the first attempt, absence of a slow song adds 0.05 to ballad preference; refused attempts still advance this balance state. The base draw dominates the named content terms. A stable act/song collision hash breaks equal preference. Family does not enter production preference directly; family composition count and hook/familiarity distributions affect the resulting winners.

| Later frame | Arrangement | First-bucket family candidate instances | Gospel rank median / P10 | Acts with both families | Gospel wins / selected first-bucket slots when both present |
| --- | --- | --- | --- | --- | --- |
| 1001 | packageAB | Gospel Standard: 11196; ArtistOriginal:unpublished: 3092; ArtistOriginal:albumRepertoire+unpublished: 13839; ArtistOriginal:locallyFamiliar+unpublished: 169; other: 258; R&B Catalog: 206; ArtistOriginal:albumRepertoire+locallyFamiliar+unpublished: 66 | 54 / 9 | 225 | 561/797 |
| 1001 | packageABResolved | Gospel Standard: 10754; ArtistOriginal:unpublished: 3480; ArtistOriginal:albumRepertoire+unpublished: 14119; ArtistOriginal:locallyFamiliar+unpublished: 216; other: 331; R&B Catalog: 319; ArtistOriginal:albumRepertoire+locallyFamiliar+unpublished: 89 | 56 / 9 | 224 | 530/796 |
| 1002 | packageAB | Gospel Standard: 9380; ArtistOriginal:albumRepertoire+unpublished: 11411; ArtistOriginal:unpublished: 2577; other: 178; ArtistOriginal:locallyFamiliar+unpublished: 90; ArtistOriginal:albumRepertoire+locallyFamiliar+unpublished: 36; R&B Catalog: 150 | 55 / 9 | 186 | 452/656 |
| 1002 | packageABResolved | Gospel Standard: 8849; ArtistOriginal:albumRepertoire+unpublished: 11286; ArtistOriginal:unpublished: 2783; other: 270; ArtistOriginal:locallyFamiliar+unpublished: 149; ArtistOriginal:albumRepertoire+locallyFamiliar+unpublished: 79; R&B Catalog: 235 | 55 / 9 | 185 | 425/656 |

Family counts and rank distributions are per-act candidate instances. Rank is the first-slot preference order among first-bucket candidates. The shared-window win denominator uses successful attempts in that bucket while Gospel and another family are both still available, traced before each removal. Refused attempts and the dynamic ballad bonus are recorded. Per-act candidates, ranks, attempts and selected families are retained in `PolarWithinBucket-later-1001.json` and `PolarWithinBucket-later-1002.json`. Selected win counts for **every** family are in the analysis JSON.

| Later frame | Diagnostic only | Gospel inherited share | Cross-genre inherited-slot delta, summed panel |
| --- | --- | --- | --- |
| 1001 | packageAB | 488/968 (50.41%) | 0 |
| 1001 | packageABNeutralPreference | 411/968 (42.46%) | -6 |
| 1001 | packageABFamilyPreference | 395/968 (40.81%) | -5 |
| 1001 | packageABResolved | 460/968 (47.52%) | 0 |
| 1001 | packageABResolvedNeutralPreference | 370/968 (38.22%) | -15 |
| 1001 | packageABResolvedFamilyPreference | 335/968 (34.61%) | -4 |
| 1002 | packageAB | 371/793 (46.78%) | 0 |
| 1002 | packageABNeutralPreference | 299/793 (37.70%) | -2 |
| 1002 | packageABFamilyPreference | 318/793 (40.10%) | -13 |
| 1002 | packageABResolved | 343/793 (43.25%) | 0 |
| 1002 | packageABResolvedNeutralPreference | 261/793 (32.91%) | -5 |
| 1002 | packageABResolvedFamilyPreference | 262/793 (33.04%) | -13 |

Neutral preference removes the numeric preference terms while retaining ballad balance and the existing collision hash. Family preference replaces individual preference with that act’s candidate-family mean, retaining ballad balance and collision hashes. For Rock-family panels the weighted-source production adapter remains in use; these preference counterfactuals are not applied there. Full genre effects of each diagnostic are retained in the analysis JSON. **Proposal for review only:** examine whether album-repertoire admission makes an act’s accessible book too broad, and whether the dominant per-song draw expresses the intended preference persistence. Neither admission nor preference was changed. These counterfactuals do not establish a specific accepted repair.

## Cross-genre A/B split (Package F)

Each cell reports inherited slots / all filled slots and percentage. All populated panel genres are retained. The four-act per signing-status cap is not population weighting; 1–3 slot changes on 15–35-slot panels are probable noise, including changes that cross a range edge. A and B can interact, so their combined effect need not equal the sum.

| Frame | Genre | Baseline | A only | B only | A+B |
| --- | --- | --- | --- | --- | --- |
| 1001 start | Classical | 20/30 (66.67%) | 20/30 (66.67%) | 20/30 (66.67%) | 20/30 (66.67%) |
| 1001 start | Comedy | 13/31 (41.94%) | 14/31 (45.16%) | 13/31 (41.94%) | 14/31 (45.16%) |
| 1001 start | LatinPop | 7/16 (43.75%) | 9/16 (56.25%) | 7/16 (43.75%) | 9/16 (56.25%) |
| 1001 start | EasyListening | 17/33 (51.52%) | 18/33 (54.55%) | 17/33 (51.52%) | 18/33 (54.55%) |
| 1001 start | TexMex | 11/23 (47.83%) | 12/23 (52.17%) | 11/23 (47.83%) | 12/23 (52.17%) |
| 1001 start | Childrens | 25/33 (75.76%) | 25/33 (75.76%) | 25/33 (75.76%) | 25/33 (75.76%) |
| 1001 start | TraditionalPop | 10/16 (62.50%) | 9/16 (56.25%) | 10/16 (62.50%) | 9/16 (56.25%) |
| 1001 start | Folk | 13/17 (76.47%) | 13/17 (76.47%) | 13/17 (76.47%) | 13/17 (76.47%) |
| 1001 start | Jazz | 5/18 (27.78%) | 6/18 (33.33%) | 5/18 (27.78%) | 6/18 (33.33%) |
| 1001 start | RockAndRoll | 0/17 (0.00%) | 0/17 (0.00%) | 0/17 (0.00%) | 0/17 (0.00%) |
| 1001 start | Country | 4/14 (28.57%) | 5/14 (35.71%) | 4/14 (28.57%) | 5/14 (35.71%) |
| 1001 start | RnB | 1/15 (6.67%) | 2/15 (13.33%) | 1/15 (6.67%) | 2/15 (13.33%) |
| 1001 start | Soul | 2/16 (12.50%) | 2/16 (12.50%) | 2/16 (12.50%) | 2/16 (12.50%) |
| 1001 start | TeenPop | 1/18 (5.56%) | 4/18 (22.22%) | 1/18 (5.56%) | 4/18 (22.22%) |
| 1001 start | DooWop | 2/15 (13.33%) | 3/15 (20.00%) | 2/15 (13.33%) | 3/15 (20.00%) |
| 1001 start | Blues | 6/17 (35.29%) | 5/17 (29.41%) | 6/17 (35.29%) | 5/17 (29.41%) |
| 1002 start | EasyListening | 21/32 (65.63%) | 21/32 (65.63%) | 21/32 (65.63%) | 21/32 (65.63%) |
| 1002 start | LatinPop | 7/17 (41.18%) | 5/17 (29.41%) | 7/17 (41.18%) | 5/17 (29.41%) |
| 1002 start | Classical | 12/28 (42.86%) | 12/28 (42.86%) | 12/28 (42.86%) | 12/28 (42.86%) |
| 1002 start | Childrens | 18/29 (62.07%) | 18/29 (62.07%) | 18/29 (62.07%) | 18/29 (62.07%) |
| 1002 start | Comedy | 12/33 (36.36%) | 11/33 (33.33%) | 12/33 (36.36%) | 11/33 (33.33%) |
| 1002 start | TexMex | 18/30 (60.00%) | 15/30 (50.00%) | 18/30 (60.00%) | 15/30 (50.00%) |
| 1002 start | Folk | 12/17 (70.59%) | 13/17 (76.47%) | 12/17 (70.59%) | 13/17 (76.47%) |
| 1002 start | Jazz | 9/15 (60.00%) | 9/15 (60.00%) | 9/15 (60.00%) | 9/15 (60.00%) |
| 1002 start | TraditionalPop | 2/14 (14.29%) | 2/14 (14.29%) | 2/14 (14.29%) | 2/14 (14.29%) |
| 1002 start | Country | 8/18 (44.44%) | 9/18 (50.00%) | 8/18 (44.44%) | 9/18 (50.00%) |
| 1002 start | RockAndRoll | 0/17 (0.00%) | 0/17 (0.00%) | 0/17 (0.00%) | 0/17 (0.00%) |
| 1002 start | RnB | 3/17 (17.65%) | 3/17 (17.65%) | 3/17 (17.65%) | 3/17 (17.65%) |
| 1002 start | Soul | 2/16 (12.50%) | 2/16 (12.50%) | 2/16 (12.50%) | 2/16 (12.50%) |
| 1002 start | TeenPop | 1/14 (7.14%) | 3/14 (21.43%) | 1/14 (7.14%) | 3/14 (21.43%) |
| 1002 start | DooWop | 2/17 (11.76%) | 1/17 (5.88%) | 2/17 (11.76%) | 1/17 (5.88%) |
| 1002 start | Blues | 3/15 (20.00%) | 3/15 (20.00%) | 3/15 (20.00%) | 3/15 (20.00%) |
| 1001 later | Comedy | 12/29 (41.38%) | 11/29 (37.93%) | 11/29 (37.93%) | 12/29 (41.38%) |
| 1001 later | LatinPop | 7/34 (20.59%) | 12/34 (35.29%) | 8/34 (23.53%) | 10/34 (29.41%) |
| 1001 later | Classical | 14/32 (43.75%) | 14/32 (43.75%) | 14/32 (43.75%) | 14/32 (43.75%) |
| 1001 later | EasyListening | 15/30 (50.00%) | 14/30 (46.67%) | 13/30 (43.33%) | 13/30 (43.33%) |
| 1001 later | TexMex | 11/32 (34.38%) | 9/32 (28.13%) | 10/32 (31.25%) | 9/32 (28.13%) |
| 1001 later | Childrens | 15/28 (53.57%) | 15/28 (53.57%) | 15/28 (53.57%) | 15/28 (53.57%) |
| 1001 later | Folk | 24/37 (64.86%) | 24/37 (64.86%) | 25/37 (67.57%) | 25/37 (67.57%) |
| 1001 later | Country | 13/34 (38.24%) | 13/34 (38.24%) | 13/34 (38.24%) | 13/34 (38.24%) |
| 1001 later | RnB | 4/30 (13.33%) | 4/30 (13.33%) | 4/30 (13.33%) | 4/30 (13.33%) |
| 1001 later | RockAndRoll | 0/28 (0.00%) | 0/28 (0.00%) | 0/28 (0.00%) | 0/28 (0.00%) |
| 1001 later | Soul | 4/38 (10.53%) | 4/38 (10.53%) | 4/38 (10.53%) | 4/38 (10.53%) |
| 1001 later | DooWop | 3/33 (9.09%) | 2/33 (6.06%) | 3/33 (9.09%) | 3/33 (9.09%) |
| 1001 later | TraditionalPop | 13/32 (40.63%) | 13/32 (40.63%) | 12/32 (37.50%) | 13/32 (40.63%) |
| 1001 later | SurfRock | 1/33 (3.03%) | 1/33 (3.03%) | 1/33 (3.03%) | 1/33 (3.03%) |
| 1001 later | Jazz | 19/36 (52.78%) | 14/36 (38.89%) | 15/36 (41.67%) | 13/36 (36.11%) |
| 1001 later | TeenPop | 1/33 (3.03%) | 7/33 (21.21%) | 1/33 (3.03%) | 6/33 (18.18%) |
| 1001 later | Blues | 12/33 (36.36%) | 9/33 (27.27%) | 12/33 (36.36%) | 9/33 (27.27%) |
| 1001 later | ContemporaryFolk | 7/20 (35.00%) | 7/20 (35.00%) | 7/20 (35.00%) | 7/20 (35.00%) |
| 1002 later | EasyListening | 16/34 (47.06%) | 18/34 (52.94%) | 17/34 (50.00%) | 18/34 (52.94%) |
| 1002 later | Classical | 9/32 (28.13%) | 8/32 (25.00%) | 14/32 (43.75%) | 14/32 (43.75%) |
| 1002 later | Childrens | 20/31 (64.52%) | 20/31 (64.52%) | 20/31 (64.52%) | 20/31 (64.52%) |
| 1002 later | LatinPop | 4/30 (13.33%) | 11/30 (36.67%) | 6/30 (20.00%) | 14/30 (46.67%) |
| 1002 later | Comedy | 13/29 (44.83%) | 14/29 (48.28%) | 13/29 (44.83%) | 14/29 (48.28%) |
| 1002 later | TexMex | 10/32 (31.25%) | 14/32 (43.75%) | 15/32 (46.88%) | 14/32 (43.75%) |
| 1002 later | TraditionalPop | 9/29 (31.03%) | 7/29 (24.14%) | 10/29 (34.48%) | 8/29 (27.59%) |
| 1002 later | RnB | 5/35 (14.29%) | 3/35 (8.57%) | 5/35 (14.29%) | 3/35 (8.57%) |
| 1002 later | Jazz | 17/31 (54.84%) | 13/31 (41.94%) | 17/31 (54.84%) | 18/31 (58.06%) |
| 1002 later | Country | 10/29 (34.48%) | 10/29 (34.48%) | 12/29 (41.38%) | 11/29 (37.93%) |
| 1002 later | DooWop | 1/35 (2.86%) | 1/35 (2.86%) | 1/35 (2.86%) | 2/35 (5.71%) |
| 1002 later | Soul | 2/34 (5.88%) | 3/34 (8.82%) | 2/34 (5.88%) | 3/34 (8.82%) |
| 1002 later | Folk | 19/33 (57.58%) | 15/33 (45.45%) | 22/33 (66.67%) | 22/33 (66.67%) |
| 1002 later | RockAndRoll | 1/32 (3.13%) | 1/32 (3.13%) | 1/32 (3.13%) | 1/32 (3.13%) |
| 1002 later | Blues | 11/36 (30.56%) | 9/36 (25.00%) | 11/36 (30.56%) | 9/36 (25.00%) |
| 1002 later | TeenPop | 3/30 (10.00%) | 8/30 (26.67%) | 3/30 (10.00%) | 7/30 (23.33%) |
| 1002 later | SurfRock | 0/30 (0.00%) | 0/30 (0.00%) | 0/30 (0.00%) | 0/30 (0.00%) |
| 1002 later | ContemporaryFolk | 6/15 (40.00%) | 7/15 (46.67%) | 6/15 (40.00%) | 7/15 (46.67%) |

Repeated-sign flags across seeds or frames (including small changes) are below. A/B entries are inherited-slot deltas from baseline; a sign repeat is a diagnostic flag, not evidence of statistical significance.

| Genre | Frame: A / B / AB slot delta (denominator) | Larger isolated component (absolute slot movement) |
| --- | --- | --- |
| Comedy | 1001 start: 1/0/1 (31); 1002 start: -1/0/-1 (33); 1001 later: -1/-1/0 (29); 1002 later: 1/0/1 (29) | A |
| LatinPop | 1001 start: 2/0/2 (16); 1002 start: -2/0/-2 (17); 1001 later: 5/1/3 (34); 1002 later: 7/2/10 (30) | A |
| EasyListening | 1001 start: 1/0/1 (33); 1002 start: 0/0/0 (32); 1001 later: -1/-2/-2 (30); 1002 later: 2/1/2 (34) | A |
| TexMex | 1001 start: 1/0/1 (23); 1002 start: -3/0/-3 (30); 1001 later: -2/-1/-2 (32); 1002 later: 4/5/4 (32) | A |
| TraditionalPop | 1001 start: -1/0/-1 (16); 1002 start: 0/0/0 (14); 1001 later: 0/-1/0 (32); 1002 later: -2/1/-1 (29) | A |
| Folk | 1001 start: 0/0/0 (17); 1002 start: 1/0/1 (17); 1001 later: 0/1/1 (37); 1002 later: -4/3/3 (33) | A |
| Jazz | 1001 start: 1/0/1 (18); 1002 start: 0/0/0 (15); 1001 later: -5/-4/-6 (36); 1002 later: -4/0/1 (31) | A |
| Country | 1001 start: 1/0/1 (14); 1002 start: 1/0/1 (18); 1001 later: 0/0/0 (34); 1002 later: 0/2/1 (29) | equal / interactions |
| TeenPop | 1001 start: 3/0/3 (18); 1002 start: 2/0/2 (14); 1001 later: 6/0/5 (33); 1002 later: 5/0/4 (30) | A |
| DooWop | 1001 start: 1/0/1 (15); 1002 start: -1/0/-1 (17); 1001 later: -1/0/0 (33); 1002 later: 0/0/1 (35) | A |
| Blues | 1001 start: -1/0/-1 (17); 1002 start: 0/0/0 (15); 1001 later: -3/0/-3 (33); 1002 later: -2/0/-2 (36) | A |

The two previously identified new range failures in seed 1001 later are separately shown.

| Genre / cohort | Baseline | A only | B only | A+B frozen | A+B resolved |
| --- | --- | --- | --- | --- | --- |
| EasyListening / standardsLed | 15/30 (50.00%) | 14/30 (46.67%) | 13/30 (43.33%) | 13/30 (43.33%) | 13/30 (43.33%) |
| Jazz / standardsInterpreter | 17/28 (60.71%) | 13/28 (46.43%) | 13/28 (46.43%) | 12/28 (42.86%) | 10/28 (35.71%) |

## Fresh repaired trajectories (Package G)

Monthly complete Gospel cohorts; other five genres use at most four acts per genre/cohort/signing cell. Retained repertoire-final monthly baseline is reduced to the same sampling rule. Raw slot shares, no population weighting. Census none. Monthly inherited totals are pooled into quarters, with signed and unsigned reported separately. All 21 month observations per trajectory are required by the analyzer. Resumed duplicate month observations are collapsed to their first date. Baseline monthly streams are reduced using the same ID-hash panel rule for the other five genres; the evolved populations can differ.

Weekly comparison scope: Completed polar-census209-on baseline: 91 matching calendar dates. This predates the repertoire repairs; weekly economic differences cannot isolate A+B alone. The interrupted repertoire-final treatment only flushed 52 weekly rows, so it cannot supply the full requested calendar window. No missing weekly values were invented or reconstructed.

| Seed | H version | Slices | Completed weeks | End date | Monthly observations |
| --- | --- | --- | --- | --- | --- |
| 1001 | off | 4 | 91 | 1961-9-29 | 21 |
| 1001 | on | 3 | 91 | 1961-9-29 | 21 |
| 1002 | off | 4 | 91 | 1961-9-29 | 21 |
| 1002 | on | 3 | 91 | 1961-9-29 | 21 |

Every slice uses census none and a 180-second graceful-stop budget; checkpoints and round-trip verification resume the world. Stop checks run at safe boundaries, so a completed week/observation/checkpoint can extend wall time beyond the nominal budget. Global RNG restoration retains the repository’s existing deterministic per-week reseeding behavior; a resumed trajectory is not claimed byte-identical to an uninterrupted simulation. H off/on have different calendar resume boundaries because stopping is time-based. Their longitudinal difference therefore includes resume-boundary effects and is not an isolated causal H estimate; the fixed-world H probe isolates the score/selection effect. The original retained artifacts remain untouched.

| Seed | Genre / quarter | Signed baseline | Signed H off | Signed H on | Unsigned baseline | Unsigned H off | Unsigned H on |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | Gospel 1960-Q1 | 202/647 (31.22%) | 388/647 (59.97%) | 372/647 (57.50%) | 41/104 (39.42%) | 59/104 (56.73%) | 61/104 (58.65%) |
| 1001 | Gospel 1960-Q2 | 163/644 (25.31%) | 365/644 (56.68%) | 349/644 (54.19%) | 117/354 (33.05%) | 196/354 (55.37%) | 207/354 (58.47%) |
| 1001 | Gospel 1960-Q3 | 139/630 (22.06%) | 338/629 (53.74%) | 302/607 (49.75%) | 197/712 (27.67%) | 364/713 (51.05%) | 385/735 (52.38%) |
| 1001 | Gospel 1960-Q4 | 100/615 (16.26%) | 323/666 (48.50%) | 253/612 (41.34%) | 230/1119 (20.55%) | 556/1068 (52.06%) | 550/1122 (49.02%) |
| 1001 | Gospel 1961-Q1 | 71/542 (13.10%) | 333/674 (49.41%) | 222/567 (39.15%) | 226/2247 (10.06%) | 1049/2115 (49.60%) | 886/2222 (39.87%) |
| 1001 | Gospel 1961-Q2 | 54/620 (8.71%) | 337/750 (44.93%) | 275/660 (41.67%) | 171/2543 (6.72%) | 1163/2413 (48.20%) | 1018/2503 (40.67%) |
| 1001 | Gospel 1961-Q3 | 62/671 (9.24%) | 320/788 (40.61%) | 281/658 (42.71%) | 181/2646 (6.84%) | 1112/2515 (44.21%) | 1147/2647 (43.33%) |
| 1001 | Folk 1960-Q1 | 33/51 (64.71%) | 40/51 (78.43%) | 42/51 (82.35%) | 9/30 (30.00%) | 14/30 (46.67%) | 14/30 (46.67%) |
| 1001 | Folk 1960-Q2 | 34/49 (69.39%) | 37/49 (75.51%) | 41/49 (83.67%) | 26/48 (54.17%) | 31/48 (64.58%) | 22/48 (45.83%) |
| 1001 | Folk 1960-Q3 | 37/45 (82.22%) | 36/45 (80.00%) | 37/45 (82.22%) | 25/48 (52.08%) | 21/48 (43.75%) | 23/48 (47.92%) |
| 1001 | Folk 1960-Q4 | 40/49 (81.63%) | 40/49 (81.63%) | 38/49 (77.55%) | 24/48 (50.00%) | 16/48 (33.33%) | 20/48 (41.67%) |
| 1001 | Folk 1961-Q1 | 37/49 (75.51%) | 30/49 (61.22%) | 25/51 (49.02%) | 29/54 (53.70%) | 18/54 (33.33%) | 22/53 (41.51%) |
| 1001 | Folk 1961-Q2 | 26/46 (56.52%) | 21/45 (46.67%) | 20/44 (45.45%) | 21/45 (46.67%) | 21/44 (47.73%) | 21/45 (46.67%) |
| 1001 | Folk 1961-Q3 | 28/53 (52.83%) | 29/52 (55.77%) | 22/53 (41.51%) | 27/47 (57.45%) | 27/47 (57.45%) | 27/47 (57.45%) |
| 1001 | EasyListening 1960-Q1 | 26/50 (52.00%) | 27/50 (54.00%) | 27/50 (54.00%) | 23/46 (50.00%) | 25/46 (54.35%) | 25/46 (54.35%) |
| 1001 | EasyListening 1960-Q2 | 20/47 (42.55%) | 22/47 (46.81%) | 21/47 (44.68%) | 20/48 (41.67%) | 23/48 (47.92%) | 23/48 (47.92%) |
| 1001 | EasyListening 1960-Q3 | 17/44 (38.64%) | 22/44 (50.00%) | 18/42 (42.86%) | 20/45 (44.44%) | 27/44 (61.36%) | 27/46 (58.70%) |
| 1001 | EasyListening 1960-Q4 | 22/47 (46.81%) | 27/46 (58.70%) | 20/46 (43.48%) | 25/47 (53.19%) | 22/45 (48.89%) | 29/47 (61.70%) |
| 1001 | EasyListening 1961-Q1 | 20/43 (46.51%) | 20/44 (45.45%) | 16/43 (37.21%) | 30/52 (57.69%) | 20/52 (38.46%) | 28/52 (53.85%) |
| 1001 | EasyListening 1961-Q2 | 19/42 (45.24%) | 20/42 (47.62%) | 21/48 (43.75%) | 25/47 (53.19%) | 22/47 (46.81%) | 22/46 (47.83%) |
| 1001 | EasyListening 1961-Q3 | 20/46 (43.48%) | 27/48 (56.25%) | 17/45 (37.78%) | 20/46 (43.48%) | 18/47 (38.30%) | 23/46 (50.00%) |
| 1001 | Jazz 1960-Q1 | 33/100 (33.00%) | 36/101 (35.64%) | 39/101 (38.61%) | 21/40 (52.50%) | 21/40 (52.50%) | 22/40 (55.00%) |
| 1001 | Jazz 1960-Q2 | 23/94 (24.47%) | 30/96 (31.25%) | 30/96 (31.25%) | 36/98 (36.73%) | 34/97 (35.05%) | 34/97 (35.05%) |
| 1001 | Jazz 1960-Q3 | 31/101 (30.69%) | 34/100 (34.00%) | 37/100 (37.00%) | 39/92 (42.39%) | 34/93 (36.56%) | 30/93 (32.26%) |
| 1001 | Jazz 1960-Q4 | 37/95 (38.95%) | 30/93 (32.26%) | 36/94 (38.30%) | 29/93 (31.18%) | 31/94 (32.98%) | 39/93 (41.94%) |
| 1001 | Jazz 1961-Q1 | 46/101 (45.54%) | 33/99 (33.33%) | 49/102 (48.04%) | 34/95 (35.79%) | 24/97 (24.74%) | 31/96 (32.29%) |
| 1001 | Jazz 1961-Q2 | 43/98 (43.88%) | 32/95 (33.68%) | 52/96 (54.17%) | 28/97 (28.87%) | 27/100 (27.00%) | 28/99 (28.28%) |
| 1001 | Jazz 1961-Q3 | 39/94 (41.49%) | 30/87 (34.48%) | 47/85 (55.29%) | 29/91 (31.87%) | 27/97 (27.84%) | 26/101 (25.74%) |
| 1001 | Blues 1960-Q1 | 13/45 (28.89%) | 14/45 (31.11%) | 16/45 (35.56%) | 10/25 (40.00%) | 11/25 (44.00%) | 8/25 (32.00%) |
| 1001 | Blues 1960-Q2 | 10/51 (19.61%) | 11/51 (21.57%) | 17/51 (33.33%) | 20/45 (44.44%) | 22/46 (47.83%) | 19/46 (41.30%) |
| 1001 | Blues 1960-Q3 | 10/48 (20.83%) | 9/48 (18.75%) | 14/48 (29.17%) | 20/47 (42.55%) | 21/47 (44.68%) | 18/47 (38.30%) |
| 1001 | Blues 1960-Q4 | 9/44 (20.45%) | 9/44 (20.45%) | 16/44 (36.36%) | 22/47 (46.81%) | 24/47 (51.06%) | 20/48 (41.67%) |
| 1001 | Blues 1961-Q1 | 12/49 (24.49%) | 15/49 (30.61%) | 20/52 (38.46%) | 23/46 (50.00%) | 24/47 (51.06%) | 19/46 (41.30%) |
| 1001 | Blues 1961-Q2 | 10/46 (21.74%) | 16/46 (34.78%) | 20/48 (41.67%) | 27/50 (54.00%) | 25/48 (52.08%) | 20/49 (40.82%) |
| 1001 | Blues 1961-Q3 | 11/47 (23.40%) | 21/52 (40.38%) | 19/45 (42.22%) | 23/43 (53.49%) | 22/43 (51.16%) | 18/43 (41.86%) |
| 1001 | TraditionalPop 1960-Q1 | 31/52 (59.62%) | 28/52 (53.85%) | 23/52 (44.23%) | 12/32 (37.50%) | 11/32 (34.38%) | 9/32 (28.13%) |
| 1001 | TraditionalPop 1960-Q2 | 24/45 (53.33%) | 19/45 (42.22%) | 21/45 (46.67%) | 21/49 (42.86%) | 16/49 (32.65%) | 14/49 (28.57%) |
| 1001 | TraditionalPop 1960-Q3 | 24/48 (50.00%) | 23/48 (47.92%) | 21/48 (43.75%) | 12/45 (26.67%) | 8/45 (17.78%) | 12/45 (26.67%) |
| 1001 | TraditionalPop 1960-Q4 | 20/47 (42.55%) | 16/45 (35.56%) | 23/45 (51.11%) | 16/50 (32.00%) | 13/48 (27.08%) | 14/48 (29.17%) |
| 1001 | TraditionalPop 1961-Q1 | 16/45 (35.56%) | 16/46 (34.78%) | 19/45 (42.22%) | 24/46 (52.17%) | 26/47 (55.32%) | 22/46 (47.83%) |
| 1001 | TraditionalPop 1961-Q2 | 18/45 (40.00%) | 15/47 (31.91%) | 21/46 (45.65%) | 21/47 (44.68%) | 26/47 (55.32%) | 19/47 (40.43%) |
| 1001 | TraditionalPop 1961-Q3 | 22/46 (47.83%) | 10/47 (21.28%) | 20/47 (42.55%) | 21/52 (40.38%) | 30/52 (57.69%) | 20/52 (38.46%) |
| 1002 | Gospel 1960-Q1 | 145/500 (29.00%) | 306/500 (61.20%) | 293/500 (58.60%) | 12/106 (11.32%) | 45/106 (42.45%) | 49/106 (46.23%) |
| 1002 | Gospel 1960-Q2 | 93/525 (17.71%) | 295/521 (56.62%) | 291/525 (55.43%) | 47/360 (13.06%) | 187/364 (51.37%) | 199/360 (55.28%) |
| 1002 | Gospel 1960-Q3 | 98/556 (17.63%) | 281/542 (51.85%) | 283/562 (50.36%) | 81/573 (14.14%) | 257/587 (43.78%) | 275/567 (48.50%) |
| 1002 | Gospel 1960-Q4 | 121/542 (22.32%) | 270/546 (49.45%) | 258/552 (46.74%) | 153/870 (17.59%) | 341/862 (39.56%) | 356/860 (41.40%) |
| 1002 | Gospel 1961-Q1 | 280/585 (47.86%) | 274/621 (44.12%) | 256/626 (40.89%) | 809/1774 (45.60%) | 847/1738 (48.73%) | 715/1733 (41.26%) |
| 1002 | Gospel 1961-Q2 | 103/662 (15.56%) | 275/618 (44.50%) | 310/685 (45.26%) | 348/1977 (17.60%) | 893/2009 (44.45%) | 909/1942 (46.81%) |
| 1002 | Gospel 1961-Q3 | 198/685 (28.91%) | 306/721 (42.44%) | 297/678 (43.81%) | 672/2241 (29.99%) | 916/2177 (42.08%) | 1048/2211 (47.40%) |
| 1002 | Folk 1960-Q1 | 31/49 (63.27%) | 30/49 (61.22%) | 26/49 (53.06%) | 15/32 (46.88%) | 14/32 (43.75%) | 16/32 (50.00%) |
| 1002 | Folk 1960-Q2 | 26/49 (53.06%) | 24/49 (48.98%) | 27/49 (55.10%) | 30/48 (62.50%) | 29/48 (60.42%) | 30/48 (62.50%) |
| 1002 | Folk 1960-Q3 | 30/52 (57.69%) | 26/52 (50.00%) | 31/52 (59.62%) | 32/49 (65.31%) | 27/49 (55.10%) | 32/49 (65.31%) |
| 1002 | Folk 1960-Q4 | 26/47 (55.32%) | 23/47 (48.94%) | 30/47 (63.83%) | 27/44 (61.36%) | 26/44 (59.09%) | 25/44 (56.82%) |
| 1002 | Folk 1961-Q1 | 27/52 (51.92%) | 20/52 (38.46%) | 24/50 (48.00%) | 33/52 (63.46%) | 41/52 (78.85%) | 34/56 (60.71%) |
| 1002 | Folk 1961-Q2 | 30/49 (61.22%) | 24/49 (48.98%) | 30/50 (60.00%) | 35/48 (72.92%) | 35/48 (72.92%) | 29/45 (64.44%) |
| 1002 | Folk 1961-Q3 | 31/49 (63.27%) | 25/49 (51.02%) | 27/50 (54.00%) | 34/49 (69.39%) | 34/49 (69.39%) | 27/50 (54.00%) |
| 1002 | EasyListening 1960-Q1 | 33/50 (66.00%) | 33/50 (66.00%) | 33/50 (66.00%) | 29/47 (61.70%) | 34/47 (72.34%) | 35/47 (74.47%) |
| 1002 | EasyListening 1960-Q2 | 35/46 (76.09%) | 31/46 (67.39%) | 35/46 (76.09%) | 22/45 (48.89%) | 24/45 (53.33%) | 25/43 (58.14%) |
| 1002 | EasyListening 1960-Q3 | 33/50 (66.00%) | 28/50 (56.00%) | 36/50 (72.00%) | 20/44 (45.45%) | 20/44 (45.45%) | 17/43 (39.53%) |
| 1002 | EasyListening 1960-Q4 | 33/52 (63.46%) | 34/52 (65.38%) | 35/52 (67.31%) | 17/53 (32.08%) | 22/53 (41.51%) | 23/53 (43.40%) |
| 1002 | EasyListening 1961-Q1 | 33/51 (64.71%) | 31/51 (60.78%) | 38/52 (73.08%) | 15/52 (28.85%) | 23/52 (44.23%) | 21/52 (40.38%) |
| 1002 | EasyListening 1961-Q2 | 33/53 (62.26%) | 31/53 (58.49%) | 36/56 (64.29%) | 14/53 (26.42%) | 26/53 (49.06%) | 23/53 (43.40%) |
| 1002 | EasyListening 1961-Q3 | 25/43 (58.14%) | 28/43 (65.12%) | 26/44 (59.09%) | 10/45 (22.22%) | 25/45 (55.56%) | 20/45 (44.44%) |
| 1002 | Jazz 1960-Q1 | 48/92 (52.17%) | 43/92 (46.74%) | 45/92 (48.91%) | 26/47 (55.32%) | 25/47 (53.19%) | 22/47 (46.81%) |
| 1002 | Jazz 1960-Q2 | 54/92 (58.70%) | 50/92 (54.35%) | 53/92 (57.61%) | 47/100 (47.00%) | 38/100 (38.00%) | 46/100 (46.00%) |
| 1002 | Jazz 1960-Q3 | 56/100 (56.00%) | 50/100 (50.00%) | 46/100 (46.00%) | 41/96 (42.71%) | 30/97 (30.93%) | 33/96 (34.38%) |
| 1002 | Jazz 1960-Q4 | 52/101 (51.49%) | 53/101 (52.48%) | 47/101 (46.53%) | 40/97 (41.24%) | 31/91 (34.07%) | 31/97 (31.96%) |
| 1002 | Jazz 1961-Q1 | 50/98 (51.02%) | 53/99 (53.54%) | 53/96 (55.21%) | 39/100 (39.00%) | 36/98 (36.73%) | 45/98 (45.92%) |
| 1002 | Jazz 1961-Q2 | 46/93 (49.46%) | 41/93 (44.09%) | 42/93 (45.16%) | 33/98 (33.67%) | 32/101 (31.68%) | 36/101 (35.64%) |
| 1002 | Jazz 1961-Q3 | 44/90 (48.89%) | 47/91 (51.65%) | 42/95 (44.21%) | 38/99 (38.38%) | 36/99 (36.36%) | 38/95 (40.00%) |
| 1002 | Blues 1960-Q1 | 11/47 (23.40%) | 13/47 (27.66%) | 11/47 (23.40%) | 8/30 (26.67%) | 13/30 (43.33%) | 14/30 (46.67%) |
| 1002 | Blues 1960-Q2 | 14/53 (26.42%) | 15/53 (28.30%) | 12/53 (22.64%) | 16/48 (33.33%) | 25/48 (52.08%) | 23/48 (47.92%) |
| 1002 | Blues 1960-Q3 | 11/52 (21.15%) | 13/52 (25.00%) | 10/52 (19.23%) | 12/45 (26.67%) | 20/45 (44.44%) | 15/45 (33.33%) |
| 1002 | Blues 1960-Q4 | 10/53 (18.87%) | 11/53 (20.75%) | 9/53 (16.98%) | 25/49 (51.02%) | 31/49 (63.27%) | 23/49 (46.94%) |
| 1002 | Blues 1961-Q1 | 18/49 (36.73%) | 20/51 (39.22%) | 16/51 (31.37%) | 18/51 (35.29%) | 18/46 (39.13%) | 16/47 (34.04%) |
| 1002 | Blues 1961-Q2 | 12/43 (27.91%) | 22/53 (41.51%) | 17/48 (35.42%) | 18/51 (35.29%) | 16/49 (32.65%) | 15/51 (29.41%) |
| 1002 | Blues 1961-Q3 | 15/49 (30.61%) | 19/47 (40.43%) | 14/52 (26.92%) | 14/47 (29.79%) | 12/46 (26.09%) | 8/46 (17.39%) |
| 1002 | TraditionalPop 1960-Q1 | 5/44 (11.36%) | 5/44 (11.36%) | 6/44 (13.64%) | 13/31 (41.94%) | 13/31 (41.94%) | 13/31 (41.94%) |
| 1002 | TraditionalPop 1960-Q2 | 10/49 (20.41%) | 11/49 (22.45%) | 9/49 (18.37%) | 27/52 (51.92%) | 26/52 (50.00%) | 23/52 (44.23%) |
| 1002 | TraditionalPop 1960-Q3 | 11/50 (22.00%) | 12/49 (24.49%) | 9/49 (18.37%) | 21/45 (46.67%) | 22/45 (48.89%) | 18/45 (40.00%) |
| 1002 | TraditionalPop 1960-Q4 | 12/48 (25.00%) | 12/46 (26.09%) | 8/46 (17.39%) | 15/41 (36.59%) | 18/41 (43.90%) | 15/41 (36.59%) |
| 1002 | TraditionalPop 1961-Q1 | 6/46 (13.04%) | 20/48 (41.67%) | 14/50 (28.00%) | 20/41 (48.78%) | 9/45 (20.00%) | 21/41 (51.22%) |
| 1002 | TraditionalPop 1961-Q2 | 16/49 (32.65%) | 12/48 (25.00%) | 14/49 (28.57%) | 28/50 (56.00%) | 21/50 (42.00%) | 26/51 (50.98%) |
| 1002 | TraditionalPop 1961-Q3 | 16/48 (33.33%) | 13/48 (27.08%) | 13/50 (26.00%) | 30/50 (60.00%) | 28/50 (56.00%) | 25/47 (53.19%) |

| 1961 Q3 Gospel | H | Inherited / all slots | Provisional 50–90 band |
| --- | --- | --- | --- |
| 1001 | off | 1432/3303 (43.35%) | FAIL |
| 1001 | on | 1428/3305 (43.21%) | FAIL |
| 1002 | off | 1222/2898 (42.17%) | FAIL |
| 1002 | on | 1345/2889 (46.56%) | FAIL |

New cross-genre range failures are shown with counts; existing baseline failures are retained separately in the analysis JSON. The non-Gospel panels support descriptive range flags, not full-population acceptance.

| Seed | H | Quarter | Genre / cohort | Baseline | Repaired | Band |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | off | 1961-Q1 | EasyListening / standardsLed | 50/95 (52.63%) | 40/96 (41.67%) | 50–85 |
| 1001 | off | 1961-Q1 | Folk / traditionalRevival | 66/103 (64.08%) | 48/103 (46.60%) | 50–90 |
| 1001 | off | 1961-Q2 | Folk / traditionalRevival | 47/91 (51.65%) | 42/89 (47.19%) | 50–90 |
| 1001 | on | 1961-Q1 | EasyListening / standardsLed | 50/95 (52.63%) | 44/95 (46.32%) | 50–85 |
| 1001 | on | 1961-Q1 | Folk / traditionalRevival | 66/103 (64.08%) | 47/104 (45.19%) | 50–90 |
| 1001 | on | 1961-Q2 | Folk / traditionalRevival | 47/91 (51.65%) | 41/89 (46.07%) | 50–90 |
| 1001 | on | 1961-Q3 | Folk / traditionalRevival | 55/100 (55.00%) | 49/100 (49.00%) | 50–90 |
| 1002 | off | 1961-Q2 | Jazz / standardsInterpreter | 59/86 (68.60%) | 53/89 (59.55%) | 60–90 |
| 1002 | on | 1960-Q3 | Jazz / standardsInterpreter | 74/100 (74.00%) | 59/100 (59.00%) | 60–90 |
| 1002 | on | 1960-Q4 | Jazz / standardsInterpreter | 67/97 (69.07%) | 53/97 (54.64%) | 60–90 |
| 1002 | on | 1961-Q2 | Jazz / standardsInterpreter | 59/86 (68.60%) | 53/89 (59.55%) | 60–90 |
| 1002 | on | 1961-Q3 | Jazz / standardsInterpreter | 63/94 (67.02%) | 54/93 (58.06%) | 60–90 |

Gospel taste evidence at the last available week of each quarter (Toughness, Sophistication, Sincerity, Maturity) is below. n counts distinct chart singles; raw taste remains the existing drifting mean, and adjusted taste uses k=4 without feedback into that mean.

| Seed / H | Quarter | Date | n | Raw mean | Adjusted |
| --- | --- | --- | --- | --- | --- |
| 1001 off | 1960-Q1 | 3/25/1960 | 1 | 0.1516, 0.3492, 0.9579, 0.7611 | 0.3103, 0.3798, 0.9316, 0.7222 |
| 1001 off | 1960-Q2 | 6/24/1960 | 1 | 0.1516, 0.3492, 0.9579, 0.7611 | 0.3103, 0.3798, 0.9316, 0.7222 |
| 1001 off | 1960-Q3 | 9/30/1960 | 3 | 0.2203, 0.3957, 0.9226, 0.7378 | 0.2944, 0.3910, 0.9240, 0.7233 |
| 1001 off | 1960-Q4 | 12/30/1960 | 4 | 0.1788, 0.5458, 0.7617, 0.6860 | 0.2644, 0.4667, 0.8433, 0.6992 |
| 1001 off | 1961-Q1 | 3/31/1961 | 5 | 0.1664, 0.5419, 0.8068, 0.6711 | 0.2480, 0.4733, 0.8593, 0.6895 |
| 1001 off | 1961-Q2 | 6/30/1961 | 6 | 0.3780, 0.3995, 0.7791, 0.6364 | 0.3668, 0.3947, 0.8374, 0.6669 |
| 1001 off | 1961-Q3 | 9/29/1961 | 8 | 0.3659, 0.4325, 0.7940, 0.6607 | 0.3606, 0.4175, 0.8377, 0.6780 |
| 1001 on | 1960-Q1 | 3/25/1960 | 1 | 0.1538, 0.3443, 0.9501, 0.7529 | 0.3108, 0.3789, 0.9300, 0.7206 |
| 1001 on | 1960-Q2 | 6/24/1960 | 2 | 0.1268, 0.3438, 0.9738, 0.7520 | 0.2756, 0.3729, 0.9413, 0.7257 |
| 1001 on | 1960-Q3 | 9/30/1960 | 4 | 0.3897, 0.3531, 0.7941, 0.5841 | 0.3699, 0.3703, 0.8595, 0.6483 |
| 1001 on | 1960-Q4 | 12/30/1960 | 7 | 0.5725, 0.2672, 0.7554, 0.5996 | 0.4916, 0.3110, 0.8171, 0.6406 |
| 1001 on | 1961-Q1 | 3/31/1961 | 9 | 0.5948, 0.2543, 0.7487, 0.5961 | 0.5194, 0.2953, 0.8029, 0.6319 |
| 1001 on | 1961-Q2 | 6/30/1961 | 12 | 0.4072, 0.4116, 0.7270, 0.6227 | 0.3929, 0.4056, 0.7765, 0.6451 |
| 1001 on | 1961-Q3 | 9/29/1961 | 14 | 0.2943, 0.4094, 0.8844, 0.7012 | 0.3067, 0.4045, 0.8934, 0.7037 |
| 1002 off | 1960-Q1 | 3/25/1960 | 4 | 0.3177, 0.4009, 0.7842, 0.5530 | 0.3339, 0.3942, 0.8546, 0.6327 |
| 1002 off | 1960-Q2 | 6/24/1960 | 5 | 0.2903, 0.3210, 0.8580, 0.6894 | 0.3169, 0.3506, 0.8878, 0.6996 |
| 1002 off | 1960-Q3 | 9/30/1960 | 6 | 0.4493, 0.2721, 0.7990, 0.6350 | 0.4096, 0.3183, 0.8494, 0.6660 |
| 1002 off | 1960-Q4 | 12/30/1960 | 7 | 0.2674, 0.3152, 0.9073, 0.7031 | 0.2974, 0.3415, 0.9137, 0.7065 |
| 1002 off | 1961-Q1 | 3/31/1961 | 8 | 0.2303, 0.3240, 0.9258, 0.7148 | 0.2702, 0.3452, 0.9255, 0.7140 |
| 1002 off | 1961-Q2 | 6/30/1961 | 11 | 0.2371, 0.4065, 0.8700, 0.6907 | 0.2672, 0.4015, 0.8847, 0.6965 |
| 1002 off | 1961-Q3 | 9/29/1961 | 13 | 0.4815, 0.3326, 0.9034, 0.7379 | 0.4506, 0.3455, 0.9085, 0.7319 |
| 1002 on | 1960-Q1 | 3/25/1960 | 0 | 0.0000 | 0.0000 |
| 1002 on | 1960-Q2 | 6/24/1960 | 0 | 0.0000 | 0.0000 |
| 1002 on | 1960-Q3 | 9/30/1960 | 2 | 0.3422, 0.4409, 0.8179, 0.5949 | 0.3474, 0.4053, 0.8893, 0.6733 |
| 1002 on | 1960-Q4 | 12/30/1960 | 5 | 0.4146, 0.3810, 0.7945, 0.5770 | 0.3859, 0.3839, 0.8525, 0.6372 |
| 1002 on | 1961-Q1 | 3/31/1961 | 7 | 0.2330, 0.3368, 0.9223, 0.7145 | 0.2756, 0.3553, 0.9233, 0.7138 |
| 1002 on | 1961-Q2 | 6/30/1961 | 8 | 0.1505, 0.3462, 0.9496, 0.7491 | 0.2170, 0.3599, 0.9414, 0.7369 |
| 1002 on | 1961-Q3 | 9/29/1961 | 9 | 0.2390, 0.3284, 0.9093, 0.7206 | 0.2731, 0.3466, 0.9141, 0.7181 |

| Seed / H | Selected Gospel cover slots | Admission family / routes |
| --- | --- | --- |
| 1001 off | 12572 | Gospel Standard: 8364; ArtistOriginal:unpublished;albumRepertoire: 2210; Recent R&B Hit: 304; Recent DooWop Hit: 185; R&B Catalog: 582; ProfessionalOffice: 47; ArtistOriginal:unpublished: 774; ArtistOriginal:unpublished;locallyFamiliar: 78; ArtistOriginal:unpublished;locallyFamiliar;albumRepertoire: 28 |
| 1001 on | 12576 | Gospel Standard: 7599; ArtistOriginal:unpublished;albumRepertoire: 2531; R&B Catalog: 895; Recent R&B Hit: 447; Recent DooWop Hit: 247; ArtistOriginal:unpublished: 782; ProfessionalOffice: 45; ArtistOriginal:unpublished;locallyFamiliar;albumRepertoire: 10; ArtistOriginal:unpublished;locallyFamiliar: 20 |
| 1002 off | 10490 | Gospel Standard: 7190; R&B Catalog: 291; Recent DooWop Hit: 209; Recent R&B Hit: 339; ArtistOriginal:unpublished: 429; ArtistOriginal:unpublished;albumRepertoire: 1940; ArtistOriginal:unpublished;locallyFamiliar: 53; ArtistOriginal:unpublished;locallyFamiliar;albumRepertoire: 27; ArtistOriginal:unpublished;albumRepertoire;locallyFamiliar: 6; ProfessionalOffice: 6 |
| 1002 on | 10486 | Gospel Standard: 7351; Recent DooWop Hit: 175; R&B Catalog: 271; ArtistOriginal:unpublished;albumRepertoire: 1798; ArtistOriginal:unpublished: 475; Recent R&B Hit: 317; ArtistOriginal:unpublished;locallyFamiliar: 58; ArtistOriginal:unpublished;locallyFamiliar;albumRepertoire: 29; ArtistOriginal:unpublished;albumRepertoire;locallyFamiliar: 8; ProfessionalOffice: 4 |

Same-week aggregates (the complete per-week paired rows are in the trajectory analysis JSON):

| Seed / H | Weeks | New singles baseline / repaired | Market units baseline / repaired | Label net baseline / repaired |
| --- | --- | --- | --- | --- |
| 1001 off | 91 | 8397 / 8331 | 360883702 / 359518028 | 324932975 / 317652089 |
| 1001 on | 91 | 8397 / 8220 | 360883702 / 358588510 | 324932975 / 315029757 |
| 1002 off | 91 | 8277 / 8340 | 359870495 / 360466262 | 328830979 / 323674476 |
| 1002 on | 91 | 8277 / 8216 | 359870495 / 357572741 | 328830979 / 315701693 |

## Shape variation and selector effect (Package H)

See `PolarGospelShapeCurrentState.md` for the inspected pre-change authoring paths. Seeded material already had ten-axis jitter; act-authored originals did not. H v1 uses deterministic per-composition uniform demand offsets ±0.012 and identity offsets ±0.008, keyed by world seed and composition ID, instead of stacking the previous songbook jitter. It stores offsets even with H off. Older saves default H off and receive deterministic missing offsets for future resolutions. Existing cached recording shapes are not rewritten. Version and seed persist in composition save data; covers keep the composition offset while resolving archetype, pace, mood and genre. Intrinsically instrumental templates and instrumental taxonomy keep all three vocal/lyric demands exactly zero with H on.

Measures use distinct compositions, grouped by home archetype, tolerance 1e-6 (rounded axis cells) for full-shape collisions, Euclidean distance on all ten axes, and a standard-deviation floor of 1e-4. The maximum offset vector length is 0.03347, versus a minimum between-template distance of 0.16583. Collisions count compositions belonging to a repeated full shape, not extra recordings. Covers are a separate overlapping view of compositions identified by parent-linked masters or explicit cover flags in retained records/album tracks, so rows from source and cover panels must not be added together. Imported/scouted/professional source naming denotes the remaining unseeded composition source group; scouting reuses its composition.

| Seed | Source | H | Compositions | Recordings | Colliding compositions | Nearest-template disagreements | Constraint violations |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 | original | off | 16024 | 11076 | 16024 | 0/16024 | 0 |
| 1001 | original | on | 16024 | 11076 | 0 | 0/16024 | 0 |
| 1001 | seeded | off | 5370 | 9424 | 0 | 0/5370 | 0 |
| 1001 | seeded | on | 5370 | 9424 | 0 | 0/5370 | 0 |
| 1001 | scouted-imported-professional | off | 270 | 9402 | 270 | 0/270 | 0 |
| 1001 | scouted-imported-professional | on | 270 | 9402 | 0 | 0/270 | 0 |
| 1001 | covers | off | 4109 | 15841 | 1809 | 0/4109 | 0 |
| 1001 | covers | on | 4109 | 15841 | 0 | 0/4109 | 0 |
| 1002 | original | off | 14522 | 9793 | 14522 | 0/14522 | 0 |
| 1002 | original | on | 14522 | 9793 | 0 | 0/14522 | 0 |
| 1002 | seeded | off | 5370 | 8493 | 0 | 0/5370 | 0 |
| 1002 | seeded | on | 5370 | 8493 | 0 | 0/5370 | 0 |
| 1002 | scouted-imported-professional | off | 270 | 8516 | 270 | 0/270 | 0 |
| 1002 | scouted-imported-professional | on | 270 | 8516 | 0 | 0/270 | 0 |
| 1002 | covers | off | 3847 | 14271 | 1637 | 0/3847 | 0 |
| 1002 | covers | on | 3847 | 14271 | 0 | 0/3847 | 0 |

The complete per-archetype NN median/P10, nearest-template distance, nearest-other-template separation, all ten per-axis standard deviations and axis-floor counts are in `PolarCompositionShapeMeasures.csv`; these include both seeds and all source views. Original H-on shapes have zero collisions, ten varying axes for the observed original archetypes, and zero nearest-template disagreements. Constraint violations in H-on views must be zero.

| Frame | Gospel frozen H off | Gospel frozen H on | Gospel resolved H off | Gospel resolved H on |
| --- | --- | --- | --- | --- |
| 1001 start | 134/222 (60.36%) | 132/222 (59.46%) | 131/222 (59.01%) | 130/222 (58.56%) |
| 1002 start | 104/163 (63.80%) | 104/163 (63.80%) | 103/163 (63.19%) | 103/163 (63.19%) |
| 1001 later | 488/968 (50.41%) | 492/968 (50.83%) | 460/968 (47.52%) | 459/968 (47.42%) |
| 1002 later | 371/793 (46.78%) | 373/793 (47.04%) | 343/793 (43.25%) | 342/793 (43.13%) |

| Frame | Genre | Frozen H-off → H-on | Resolved H-off → H-on |
| --- | --- | --- | --- |
| 1001 start | Classical | 20/30 (66.67%) → 20/30 (66.67%) | 20/30 (66.67%) → 20/30 (66.67%) |
| 1001 start | Comedy | 14/31 (45.16%) → 14/31 (45.16%) | 13/31 (41.94%) → 13/31 (41.94%) |
| 1001 start | LatinPop | 9/16 (56.25%) → 9/16 (56.25%) | 8/16 (50.00%) → 8/16 (50.00%) |
| 1001 start | EasyListening | 18/33 (54.55%) → 18/33 (54.55%) | 18/33 (54.55%) → 18/33 (54.55%) |
| 1001 start | TexMex | 12/23 (52.17%) → 12/23 (52.17%) | 14/23 (60.87%) → 12/23 (52.17%) |
| 1001 start | Childrens | 25/33 (75.76%) → 25/33 (75.76%) | 25/33 (75.76%) → 25/33 (75.76%) |
| 1001 start | TraditionalPop | 9/16 (56.25%) → 8/16 (50.00%) | 9/16 (56.25%) → 8/16 (50.00%) |
| 1001 start | Folk | 13/17 (76.47%) → 14/17 (82.35%) | 13/17 (76.47%) → 14/17 (82.35%) |
| 1001 start | Jazz | 6/18 (33.33%) → 6/18 (33.33%) | 6/18 (33.33%) → 6/18 (33.33%) |
| 1001 start | RockAndRoll | 0/17 (0.00%) → 0/17 (0.00%) | 0/17 (0.00%) → 0/17 (0.00%) |
| 1001 start | Country | 5/14 (35.71%) → 5/14 (35.71%) | 3/14 (21.43%) → 4/14 (28.57%) |
| 1001 start | RnB | 2/15 (13.33%) → 1/15 (6.67%) | 1/15 (6.67%) → 1/15 (6.67%) |
| 1001 start | Soul | 2/16 (12.50%) → 2/16 (12.50%) | 2/16 (12.50%) → 2/16 (12.50%) |
| 1001 start | TeenPop | 4/18 (22.22%) → 4/18 (22.22%) | 3/18 (16.67%) → 3/18 (16.67%) |
| 1001 start | DooWop | 3/15 (20.00%) → 3/15 (20.00%) | 0/15 (0.00%) → 0/15 (0.00%) |
| 1001 start | Blues | 5/17 (29.41%) → 5/17 (29.41%) | 5/17 (29.41%) → 6/17 (35.29%) |
| 1002 start | EasyListening | 21/32 (65.63%) → 21/32 (65.63%) | 22/32 (68.75%) → 22/32 (68.75%) |
| 1002 start | LatinPop | 5/17 (29.41%) → 5/17 (29.41%) | 6/17 (35.29%) → 5/17 (29.41%) |
| 1002 start | Classical | 12/28 (42.86%) → 13/28 (46.43%) | 12/28 (42.86%) → 13/28 (46.43%) |
| 1002 start | Childrens | 18/29 (62.07%) → 18/29 (62.07%) | 18/29 (62.07%) → 18/29 (62.07%) |
| 1002 start | Comedy | 11/33 (33.33%) → 11/33 (33.33%) | 11/33 (33.33%) → 11/33 (33.33%) |
| 1002 start | TexMex | 15/30 (50.00%) → 16/30 (53.33%) | 15/30 (50.00%) → 16/30 (53.33%) |
| 1002 start | Folk | 13/17 (76.47%) → 11/17 (64.71%) | 13/17 (76.47%) → 11/17 (64.71%) |
| 1002 start | Jazz | 9/15 (60.00%) → 9/15 (60.00%) | 9/15 (60.00%) → 9/15 (60.00%) |
| 1002 start | TraditionalPop | 2/14 (14.29%) → 2/14 (14.29%) | 2/14 (14.29%) → 2/14 (14.29%) |
| 1002 start | Country | 9/18 (50.00%) → 9/18 (50.00%) | 9/18 (50.00%) → 9/18 (50.00%) |
| 1002 start | RockAndRoll | 0/17 (0.00%) → 0/17 (0.00%) | 0/17 (0.00%) → 0/17 (0.00%) |
| 1002 start | RnB | 3/17 (17.65%) → 3/17 (17.65%) | 3/17 (17.65%) → 3/17 (17.65%) |
| 1002 start | Soul | 2/16 (12.50%) → 2/16 (12.50%) | 2/16 (12.50%) → 2/16 (12.50%) |
| 1002 start | TeenPop | 3/14 (21.43%) → 3/14 (21.43%) | 4/14 (28.57%) → 4/14 (28.57%) |
| 1002 start | DooWop | 1/17 (5.88%) → 1/17 (5.88%) | 0/17 (0.00%) → 0/17 (0.00%) |
| 1002 start | Blues | 3/15 (20.00%) → 3/15 (20.00%) | 3/15 (20.00%) → 3/15 (20.00%) |
| 1001 later | Comedy | 12/29 (41.38%) → 12/29 (41.38%) | 13/29 (44.83%) → 13/29 (44.83%) |
| 1001 later | LatinPop | 10/34 (29.41%) → 13/34 (38.24%) | 7/34 (20.59%) → 10/34 (29.41%) |
| 1001 later | Classical | 14/32 (43.75%) → 14/32 (43.75%) | 14/32 (43.75%) → 13/32 (40.63%) |
| 1001 later | EasyListening | 13/30 (43.33%) → 14/30 (46.67%) | 13/30 (43.33%) → 14/30 (46.67%) |
| 1001 later | TexMex | 9/32 (28.13%) → 9/32 (28.13%) | 11/32 (34.38%) → 12/32 (37.50%) |
| 1001 later | Childrens | 15/28 (53.57%) → 15/28 (53.57%) | 15/28 (53.57%) → 15/28 (53.57%) |
| 1001 later | Folk | 25/37 (67.57%) → 24/37 (64.86%) | 25/37 (67.57%) → 24/37 (64.86%) |
| 1001 later | Country | 13/34 (38.24%) → 14/34 (41.18%) | 12/34 (35.29%) → 13/34 (38.24%) |
| 1001 later | RnB | 4/30 (13.33%) → 4/30 (13.33%) | 5/30 (16.67%) → 5/30 (16.67%) |
| 1001 later | RockAndRoll | 0/28 (0.00%) → 0/28 (0.00%) | 0/28 (0.00%) → 0/28 (0.00%) |
| 1001 later | Soul | 4/38 (10.53%) → 4/38 (10.53%) | 4/38 (10.53%) → 4/38 (10.53%) |
| 1001 later | DooWop | 3/33 (9.09%) → 2/33 (6.06%) | 0/33 (0.00%) → 0/33 (0.00%) |
| 1001 later | TraditionalPop | 13/32 (40.63%) → 13/32 (40.63%) | 14/32 (43.75%) → 14/32 (43.75%) |
| 1001 later | SurfRock | 1/33 (3.03%) → 1/33 (3.03%) | 1/33 (3.03%) → 1/33 (3.03%) |
| 1001 later | Jazz | 13/36 (36.11%) → 14/36 (38.89%) | 12/36 (33.33%) → 14/36 (38.89%) |
| 1001 later | TeenPop | 6/33 (18.18%) → 8/33 (24.24%) | 5/33 (15.15%) → 5/33 (15.15%) |
| 1001 later | Blues | 9/33 (27.27%) → 10/33 (30.30%) | 12/33 (36.36%) → 13/33 (39.39%) |
| 1001 later | ContemporaryFolk | 7/20 (35.00%) → 6/20 (30.00%) | 7/20 (35.00%) → 6/20 (30.00%) |
| 1002 later | EasyListening | 18/34 (52.94%) → 13/34 (38.24%) | 17/34 (50.00%) → 15/34 (44.12%) |
| 1002 later | Classical | 14/32 (43.75%) → 14/32 (43.75%) | 14/32 (43.75%) → 14/32 (43.75%) |
| 1002 later | Childrens | 20/31 (64.52%) → 20/31 (64.52%) | 20/31 (64.52%) → 20/31 (64.52%) |
| 1002 later | LatinPop | 14/30 (46.67%) → 10/30 (33.33%) | 11/30 (36.67%) → 11/30 (36.67%) |
| 1002 later | Comedy | 14/29 (48.28%) → 14/29 (48.28%) | 14/29 (48.28%) → 14/29 (48.28%) |
| 1002 later | TexMex | 14/32 (43.75%) → 14/32 (43.75%) | 15/32 (46.88%) → 15/32 (46.88%) |
| 1002 later | TraditionalPop | 8/29 (27.59%) → 6/29 (20.69%) | 9/29 (31.03%) → 8/29 (27.59%) |
| 1002 later | RnB | 3/35 (8.57%) → 3/35 (8.57%) | 3/35 (8.57%) → 3/35 (8.57%) |
| 1002 later | Jazz | 18/31 (58.06%) → 14/31 (45.16%) | 18/31 (58.06%) → 14/31 (45.16%) |
| 1002 later | Country | 11/29 (37.93%) → 11/29 (37.93%) | 12/29 (41.38%) → 11/29 (37.93%) |
| 1002 later | DooWop | 2/35 (5.71%) → 2/35 (5.71%) | 0/35 (0.00%) → 0/35 (0.00%) |
| 1002 later | Soul | 3/34 (8.82%) → 3/34 (8.82%) | 3/34 (8.82%) → 4/34 (11.76%) |
| 1002 later | Folk | 22/33 (66.67%) → 20/33 (60.61%) | 22/33 (66.67%) → 17/33 (51.52%) |
| 1002 later | RockAndRoll | 1/32 (3.13%) → 1/32 (3.13%) | 1/32 (3.13%) → 1/32 (3.13%) |
| 1002 later | Blues | 9/36 (25.00%) → 9/36 (25.00%) | 10/36 (27.78%) → 9/36 (25.00%) |
| 1002 later | TeenPop | 7/30 (23.33%) → 8/30 (26.67%) | 9/30 (30.00%) → 9/30 (30.00%) |
| 1002 later | SurfRock | 0/30 (0.00%) → 0/30 (0.00%) | 0/30 (0.00%) → 0/30 (0.00%) |
| 1002 later | ContemporaryFolk | 7/15 (46.67%) → 7/15 (46.67%) | 7/15 (46.67%) → 7/15 (46.67%) |

Easy Listening review: ReverieMoodPiece is a fallback for act originals and appears in instrumental catalog readings as well; instrumental masking creates a real vocal/instrumental split. SaloonBallad and LushStandard also combine authored densities and lyric/vocal/production modifiers on recording arrangements. Small jitter alone does not explain the old 0.11/0.05 vocal spreads. Home-composition measures below distinguish this from the repeated recording caches in the earlier report.

| Seed | Source / H | Archetype | Compositions / recordings | NN median / P10 | Nearest-template disagreements | VocalPower / VocalNuance SD |
| --- | --- | --- | --- | --- | --- | --- |
| 1001 | original off | LushStandard | 1270/389 | 0.00000 / 0.00000 | 0/1270 | 0.0000 / 0.0000 |
| 1001 | original off | ReverieMoodPiece | 1494/921 | 0.00000 / 0.00000 | 0/1494 | 0.0000 / 0.0000 |
| 1001 | seeded off | LushStandard | 296/11 | 0.02474 / 0.01962 | 0/296 | 0.0146 / 0.0141 |
| 1001 | seeded off | SaloonBallad | 280/11 | 0.02368 / 0.01861 | 0/280 | 0.0143 / 0.0143 |
| 1001 | scouted-imported-professional off | LushStandard | 52/1081 | 0.00000 / 0.00000 | 0/52 | 0.0000 / 0.0000 |
| 1001 | scouted-imported-professional off | ReverieMoodPiece | 18/542 | 0.00000 / 0.00000 | 0/18 | 0.0000 / 0.0000 |
| 1001 | covers off | LushStandard | 11/11 | 0.03897 / 0.03532 | 0/11 | 0.0133 / 0.0127 |
| 1001 | covers off | SaloonBallad | 12/11 | 0.03978 / 0.03581 | 0/12 | 0.0156 / 0.0111 |
| 1001 | covers off | ReverieMoodPiece | 237/847 | 0.00000 / 0.00000 | 0/237 | 0.0000 / 0.0000 |
| 1001 | original on | LushStandard | 1270/389 | 0.01011 / 0.00828 | 0/1270 | 0.0069 / 0.0069 |
| 1001 | original on | ReverieMoodPiece | 1494/921 | 0.00990 / 0.00796 | 0/1494 | 0.0069 / 0.0068 |
| 1001 | seeded on | LushStandard | 296/11 | 0.01114 / 0.00837 | 0/296 | 0.0069 / 0.0066 |
| 1001 | seeded on | SaloonBallad | 280/11 | 0.01071 / 0.00804 | 0/280 | 0.0066 / 0.0073 |
| 1001 | scouted-imported-professional on | LushStandard | 52/1081 | 0.01419 / 0.01203 | 0/52 | 0.0069 / 0.0071 |
| 1001 | scouted-imported-professional on | ReverieMoodPiece | 18/542 | 0.01664 / 0.01221 | 0/18 | 0.0070 / 0.0067 |
| 1001 | covers on | LushStandard | 11/11 | 0.01980 / 0.01677 | 0/11 | 0.0073 / 0.0069 |
| 1001 | covers on | SaloonBallad | 12/11 | 0.01651 / 0.01241 | 0/12 | 0.0076 / 0.0062 |
| 1001 | covers on | ReverieMoodPiece | 237/847 | 0.01250 / 0.01018 | 0/237 | 0.0070 / 0.0066 |
| 1002 | original off | LushStandard | 1072/310 | 0.00000 / 0.00000 | 0/1072 | 0.0000 / 0.0000 |
| 1002 | original off | ReverieMoodPiece | 1339/863 | 0.00000 / 0.00000 | 0/1339 | 0.0000 / 0.0000 |
| 1002 | seeded off | LushStandard | 296/11 | 0.02474 / 0.01962 | 0/296 | 0.0146 / 0.0141 |
| 1002 | seeded off | SaloonBallad | 280/16 | 0.02368 / 0.01861 | 0/280 | 0.0143 / 0.0143 |
| 1002 | scouted-imported-professional off | LushStandard | 47/869 | 0.00000 / 0.00000 | 0/47 | 0.0000 / 0.0000 |
| 1002 | scouted-imported-professional off | ReverieMoodPiece | 21/462 | 0.00000 / 0.00000 | 0/21 | 0.0000 / 0.0000 |
| 1002 | covers off | LushStandard | 12/11 | 0.03511 / 0.02877 | 0/12 | 0.0145 / 0.0108 |
| 1002 | covers off | SaloonBallad | 13/15 | 0.03199 / 0.02765 | 0/13 | 0.0134 / 0.0091 |
| 1002 | covers off | ReverieMoodPiece | 226/618 | 0.00000 / 0.00000 | 0/226 | 0.0000 / 0.0000 |
| 1002 | original on | LushStandard | 1072/310 | 0.01031 / 0.00831 | 0/1072 | 0.0069 / 0.0069 |
| 1002 | original on | ReverieMoodPiece | 1339/863 | 0.01007 / 0.00809 | 0/1339 | 0.0069 / 0.0069 |
| 1002 | seeded on | LushStandard | 296/11 | 0.01155 / 0.00918 | 0/296 | 0.0069 / 0.0069 |
| 1002 | seeded on | SaloonBallad | 280/16 | 0.01076 / 0.00839 | 0/280 | 0.0068 / 0.0070 |
| 1002 | scouted-imported-professional on | LushStandard | 47/869 | 0.01484 / 0.01266 | 0/47 | 0.0069 / 0.0072 |
| 1002 | scouted-imported-professional on | ReverieMoodPiece | 21/462 | 0.01765 / 0.01285 | 0/21 | 0.0069 / 0.0069 |
| 1002 | covers on | LushStandard | 12/11 | 0.01997 / 0.01957 | 0/12 | 0.0071 / 0.0072 |
| 1002 | covers on | SaloonBallad | 13/15 | 0.01588 / 0.01411 | 0/13 | 0.0049 / 0.0061 |
| 1002 | covers on | ReverieMoodPiece | 226/618 | 0.01237 / 0.00957 | 0/226 | 0.0068 / 0.0068 |

The retained seed-1001 Easy Listening range failure is 13/30 with H off and 14/30 with H on in both combined variants; it still fails its band. Thus composition jitter does not account for that new A+B failure. All H-on home-composition views have zero nearest-template disagreements. The wide spreads and disagreements in the previous report concern recording/candidate arrangements, including explicit instrumental readings, rather than excessive H offsets. Instrumental masking is intentional in the resolver, but whether all such readings fit the intended archetype remains a review question. No template was changed.

Retained recording-cache counts with composition denominators (recording genre, not act genre) follow. They expose catalog reuse separately from rigid templates; the earlier 149 Soul recordings / 10 shapes example belongs to its own observation frame and is not substituted for these counts.

| Seed | Recording genre | Recordings | Distinct compositions | Distinct cached shapes |
| --- | --- | --- | --- | --- |
| 1001 | Jazz | 1306 | 845 | 588 |
| 1001 | TraditionalPop | 3631 | 1177 | 1443 |
| 1001 | Folk | 2921 | 1857 | 1119 |
| 1001 | RockAndRoll | 2391 | 1784 | 402 |
| 1001 | Country | 1589 | 709 | 369 |
| 1001 | RnB | 2556 | 1095 | 259 |
| 1001 | Soul | 6988 | 2575 | 600 |
| 1001 | TeenPop | 1600 | 419 | 225 |
| 1001 | DooWop | 510 | 489 | 139 |
| 1001 | Gospel | 649 | 413 | 291 |
| 1001 | Classical | 1147 | 453 | 74 |
| 1001 | EasyListening | 1774 | 722 | 843 |
| 1001 | Blues | 1885 | 1338 | 414 |
| 1001 | Childrens | 241 | 155 | 117 |
| 1001 | Comedy | 433 | 233 | 186 |
| 1001 | LatinPop | 186 | 147 | 80 |
| 1001 | TexMex | 56 | 47 | 24 |
| 1001 | SurfRock | 39 | 39 | 6 |
| 1002 | TraditionalPop | 2924 | 1050 | 1191 |
| 1002 | Jazz | 1520 | 934 | 703 |
| 1002 | Folk | 2554 | 1627 | 1004 |
| 1002 | RockAndRoll | 2398 | 1786 | 386 |
| 1002 | Country | 1398 | 650 | 342 |
| 1002 | RnB | 2247 | 930 | 242 |
| 1002 | Soul | 6166 | 2279 | 573 |
| 1002 | TeenPop | 1704 | 467 | 278 |
| 1002 | DooWop | 698 | 658 | 172 |
| 1002 | Gospel | 624 | 403 | 249 |
| 1002 | EasyListening | 1548 | 674 | 736 |
| 1002 | Classical | 1070 | 451 | 82 |
| 1002 | Blues | 1127 | 890 | 276 |
| 1002 | Childrens | 233 | 135 | 110 |
| 1002 | Comedy | 447 | 250 | 184 |
| 1002 | LatinPop | 108 | 89 | 50 |
| 1002 | TexMex | 30 | 29 | 16 |
| 1002 | SurfRock | 6 | 6 | 3 |

## Derived genre center review (report only)

Axis order is Toughness, Sophistication, Sincerity, Maturity. Ranks use descending value among all 51 genres, tied values share a rank. “Codex mixture” marks every equal-share form mixture without an existing authored songbook. These are provisional playtesting choices; no historical numbers were researched and no center was changed.

| Genre | Source | Authored forms and normalized shares | Derived center | Axis ranks T / So / Si / M |
| --- | --- | --- | --- | --- |
| ProgressiveRock | Codex mixture | SuiteMultiPart 50.00%; LightAndShadeEpic 50.00% | 0.5750, 0.7750, 0.7750, 0.7500 | 13 / 3 / 13 / 5 |
| DooWop | Recent DooWop Hit | SlowBallad 33.33%; SingAlongChant 33.33%; MidTempoPopSong 33.33% | 0.3000, 0.4167, 0.7333, 0.4833 | 38 / 31 / 23 / 38 |
| PsychedelicRock | Codex mixture | RagaModalDrone 33.33%; Collage 33.33%; JamExtendedWorkout 33.33% | 0.4833, 0.6833, 0.6167, 0.6833 | 21 / 8 / 44 / 8 |
| BritishInvasion | Codex mixture | MidTempoRocker 33.33%; BrightPopNumber 33.33%; ShuffleTwelveBar 33.33% | 0.5167, 0.3167, 0.6667, 0.4333 | 17 / 40 / 36 / 43 |
| BritishBeat | Codex mixture | MidTempoRocker 33.33%; BrightPopNumber 33.33%; ShuffleTwelveBar 33.33% | 0.5167, 0.3167, 0.6667, 0.4333 | 17 / 40 / 36 / 43 |
| RootsRock | Codex mixture | CountryTwoBeat 33.33%; ShuffleTwelveBar 33.33%; VerseDrivenSong 33.33% | 0.5167, 0.4167, 0.7667, 0.5833 | 17 / 31 / 16 / 27 |
| Motown | Codex mixture | HornDrivenSoulNumber 33.33%; DeepSoulPleader 33.33%; SlowBallad 33.33% | 0.4833, 0.4167, 0.8833, 0.6500 | 21 / 31 / 2 / 16 |
| Psychedelic | Codex mixture | RagaModalDrone 33.33%; Collage 33.33%; JamExtendedWorkout 33.33% | 0.4833, 0.6833, 0.6167, 0.6833 | 21 / 8 / 44 / 8 |
| Skiffle | Folk Traditional | VerseDrivenSong 33.33%; SlowBallad 33.33%; ProtestMessageSong 33.33% | 0.4167, 0.6500, 0.8833, 0.6833 | 27 / 11 / 2 / 8 |
| Funk | Codex mixture | FunkWorkout 50.00%; GrooveRiffVamp 50.00% | 0.7500, 0.2500, 0.6750, 0.5750 | 1 / 49 / 34 / 28 |
| BossaNova | Brazilian songbook | LatinBolero 33.33%; CharmSong 33.33%; BossaSong 33.33% | 0.2000, 0.6833, 0.6833, 0.6333 | 50 / 8 / 33 / 21 |
| Soul | Codex mixture | HornDrivenSoulNumber 33.33%; DeepSoulPleader 33.33%; SlowBallad 33.33% | 0.4833, 0.4167, 0.8833, 0.6500 | 21 / 31 / 2 / 16 |
| BluesRock | Codex mixture | ShuffleTwelveBar 33.33%; SlowBlues 33.33%; JamExtendedWorkout 33.33% | 0.6333, 0.3167, 0.7833, 0.6667 | 7 / 40 / 11 / 14 |
| BritishBlues | Codex mixture | ShuffleTwelveBar 33.33%; SlowBlues 33.33%; JamExtendedWorkout 33.33% | 0.6333, 0.3167, 0.7833, 0.6667 | 7 / 40 / 11 / 14 |
| AcidRock | Codex mixture | JamExtendedWorkout 33.33%; LightAndShadeEpic 33.33%; GrooveRiffVamp 33.33% | 0.6833, 0.4333, 0.7333, 0.6500 | 5 / 30 / 23 / 16 |
| ProtoMetal | Codex mixture | LightAndShadeEpic 50.00%; GrooveRiffVamp 50.00% | 0.6750, 0.4500, 0.7500, 0.6500 | 6 / 22 / 21 / 16 |
| FolkRock | Codex mixture | VerseDrivenSong 33.33%; ProtestMessageSong 33.33%; MidTempoRocker 33.33% | 0.5833, 0.5667, 0.8333, 0.6167 | 12 / 16 / 9 / 25 |
| CountryRock | Codex mixture | CountryTwoBeat 33.33%; CountryShuffle 33.33%; MidTempoRocker 33.33% | 0.5667, 0.2833, 0.7167, 0.5167 | 15 / 47 / 26 / 32 |
| SkaRocksteady | Codex mixture | LatinDance 50.00%; DanceNumber 50.00% | 0.4250, 0.4500, 0.5750, 0.4000 | 25 / 22 / 47 / 49 |
| Ska | Codex mixture | LatinDance 50.00%; DanceNumber 50.00% | 0.4250, 0.4500, 0.5750, 0.4000 | 25 / 22 / 47 / 49 |
| Reggae | Codex mixture | GrooveRiffVamp 50.00%; DanceNumber 50.00% | 0.5500, 0.3000, 0.5750, 0.4500 | 16 / 44 / 47 / 40 |
| Rocksteady | Codex mixture | DanceNumber 50.00%; SlowBallad 50.00% | 0.3250, 0.4500, 0.6750, 0.5000 | 34 / 22 / 34 / 35 |
| RockAndRoll | Recent RnR Hit | StomperRocker 25.00%; MidTempoRocker 25.00%; ShuffleTwelveBar 25.00%; SlowBallad 25.00% | 0.5750, 0.3250, 0.7250, 0.5125 | 13 / 39 / 25 / 34 |
| TeenPop | Recent Teen Hit | BrightPopNumber 25.00%; MidTempoPopSong 25.00%; DanceNumber 25.00%; SlowBallad 25.00% | 0.3000, 0.4500, 0.6500, 0.4250 | 38 / 22 / 39 / 46 |
| Gospel | Gospel Standard | QuietHymn 25.00%; GospelQuartet 25.00%; GospelChoir 25.00%; SpiritualShout 25.00% | 0.3500, 0.3875, 0.9250, 0.7125 | 32 / 36 / 1 / 6 |
| GirlGroup | Recent Teen Hit | BrightPopNumber 25.00%; MidTempoPopSong 25.00%; DanceNumber 25.00%; SlowBallad 25.00% | 0.3000, 0.4500, 0.6500, 0.4250 | 38 / 22 / 39 / 46 |
| Bubblegum | Recent Teen Hit | BrightPopNumber 25.00%; MidTempoPopSong 25.00%; DanceNumber 25.00%; SlowBallad 25.00% | 0.3000, 0.4500, 0.6500, 0.4250 | 38 / 22 / 39 / 46 |
| PopRock | Codex mixture | MidTempoRocker 33.33%; MidTempoPopSong 33.33%; SlowBallad 33.33% | 0.4000, 0.4500, 0.7500, 0.5167 | 31 / 22 / 21 / 32 |
| Country | Country Standard | CountryTwoBeat 14.29%; CountryShuffle 14.29%; CountryWaltz 14.29%; WesternSwing 14.29%; SlowBallad 14.29%; VerseDrivenSong 14.29%; NashvilleBallad 14.29% | 0.3500, 0.5000, 0.7643, 0.6286 | 32 / 20 / 19 / 23 |
| Blues | Blues Standard | ShuffleTwelveBar 33.33%; SlowBlues 33.33%; GrooveRiffVamp 33.33% | 0.6167, 0.2667, 0.7667, 0.6333 | 11 / 48 / 16 / 21 |
| Childrens | Children songs | SingAlongChant 33.33%; Novelty 33.33%; CharmSong 33.33% | 0.2667, 0.3833, 0.4833, 0.4333 | 43 / 37 / 50 / 43 |
| PsychedelicPop | Codex mixture | ChamberBaroquePiece 33.33%; RagaModalDrone 33.33%; MidTempoPopSong 33.33% | 0.2667, 0.7333, 0.7667, 0.6000 | 43 / 6 / 16 / 26 |
| Boogaloo | Codex mixture | LatinDance 33.33%; DanceNumber 33.33%; HornDrivenSoulNumber 33.33% | 0.5167, 0.4167, 0.6667, 0.4667 | 17 / 31 / 36 / 39 |
| SurfRock | Surf adaptations | SurfInstrumental 50.00%; GrooveRiffVamp 50.00% | 0.6250, 0.3000, 0.6000, 0.4500 | 9 / 44 / 46 / 40 |
| BaroquePop | Codex mixture | ChamberBaroquePiece 50.00%; DramaticBalladBigBuild 50.00% | 0.3250, 0.7250, 0.8750, 0.7000 | 34 / 7 / 8 / 7 |
| TexMex | TexMex songbook | TexMexSong 50.00%; LatinBolero 50.00% | 0.3250, 0.5250, 0.7750, 0.6250 | 34 / 17 / 13 / 24 |
| LatinPop | Latin songbook | LatinBolero 50.00%; LatinDance 50.00% | 0.3250, 0.6000, 0.7000, 0.5500 | 34 / 15 / 29 / 30 |
| TraditionalPop | Tin Pan Alley | LushStandard 20.00%; SaloonBallad 20.00%; CharmSong 20.00%; Swinger 20.00%; SlowBallad 20.00% | 0.2200, 0.7400, 0.7600, 0.7900 | 49 / 5 / 20 / 2 |
| HardRock | Codex mixture | StomperRocker 33.33%; LightAndShadeEpic 33.33%; GrooveRiffVamp 33.33% | 0.7167, 0.3667, 0.7000, 0.5500 | 4 / 38 / 29 / 30 |
| GarageRock | Codex mixture | StomperRocker 50.00%; MidTempoRocker 50.00% | 0.7500, 0.2500, 0.6500, 0.4000 | 1 / 49 / 39 / 49 |
| Jazz | Jazz Standard | Swinger 20.00%; SaloonBallad 20.00%; CharmSong 20.00%; ModernJazzInstrumental 20.00%; LushStandard 20.00% | 0.2600, 0.8000, 0.7100, 0.7800 | 45 / 2 / 28 / 4 |
| Classical | Classical works | ClassicalOrchestral 33.33%; ClassicalChamber 33.33%; ClassicalSolo 33.33% | 0.2333, 0.9167, 0.7167, 0.8000 | 46 / 1 / 26 / 1 |
| RnB | R&B Catalog | ShuffleTwelveBar 20.00%; SlowBlues 20.00%; GrooveRiffVamp 20.00%; HornDrivenSoulNumber 20.00%; DeepSoulPleader 20.00% | 0.6200, 0.3000, 0.8200, 0.6400 | 10 / 44 / 10 / 20 |
| EasyListening | Codex mixture | ReverieMoodPiece 25.00%; LushStandard 25.00%; SaloonBallad 25.00%; CharmSong 25.00% | 0.1750, 0.7750, 0.7750, 0.7875 | 51 / 3 / 13 / 3 |
| ProtoPunk | Codex mixture | StomperRocker 50.00%; GrooveRiffVamp 50.00% | 0.7250, 0.2250, 0.6250, 0.4500 | 3 / 51 / 43 / 40 |
| SunshinePop | Recent Pop Hit | BrightPopNumber 25.00%; MidTempoPopSong 25.00%; SlowBallad 25.00%; CharmSong 25.00% | 0.2250, 0.5250, 0.6875, 0.5000 | 47 / 17 / 31 / 35 |
| BritishPop | Recent Pop Hit | BrightPopNumber 25.00%; MidTempoPopSong 25.00%; SlowBallad 25.00%; CharmSong 25.00% | 0.2250, 0.5250, 0.6875, 0.5000 | 47 / 17 / 31 / 35 |
| Comedy | Comedy routines | Novelty 33.33%; SpokenWord 33.33%; JauntyMusicHallRomp 33.33% | 0.3000, 0.4833, 0.4000, 0.5667 | 38 / 21 / 51 / 29 |
| Folk | Folk Traditional | VerseDrivenSong 33.33%; SlowBallad 33.33%; ProtestMessageSong 33.33% | 0.4167, 0.6500, 0.8833, 0.6833 | 27 / 11 / 2 / 8 |
| ContemporaryFolk | Contemporary folk | VerseDrivenSong 33.33%; ProtestMessageSong 33.33%; SlowBallad 33.33% | 0.4167, 0.6500, 0.8833, 0.6833 | 27 / 11 / 2 / 8 |
| SingerSongwriter | Codex mixture | VerseDrivenSong 33.33%; SlowBallad 33.33%; ProtestMessageSong 33.33% | 0.4167, 0.6500, 0.8833, 0.6833 | 27 / 11 / 2 / 8 |

Ordinal review flags are interpretations of the table, not historical findings: Skiffle inherits Folk’s full sophistication (0.6500), above Country (0.5000) and BritishPop (0.5250), despite its comparatively plain construction; this deserves review. EasyListening and ProgressiveRock tie on sophistication (0.7750), while TraditionalPop is lower (0.7400); that ordering could underrepresent refinement in the standards songbook. Funk has sincerity 0.6750, below RootsRock (0.7667) and RnB (0.8200); the mixture merits review if the intended meaning is expressive conviction. These relationship judgments cannot establish exact replacement numbers.

| Named change | Forms producing the current value |
| --- | --- |
| DooWop: Toughness 0.70 → 0.30 | SlowBallad 33.33%; SingAlongChant 33.33%; MidTempoPopSong 33.33% |
| ProgressiveRock: Sophistication 0.30 → 0.775 | SuiteMultiPart 50.00%; LightAndShadeEpic 50.00% |
| PsychedelicRock: Sophistication +0.3833 | RagaModalDrone 33.33%; Collage 33.33%; JamExtendedWorkout 33.33% |
| Skiffle: Sincerity 0.65 → 0.8833 (Folk Traditional) | VerseDrivenSong 33.33%; SlowBallad 33.33%; ProtestMessageSong 33.33% |
| Funk: Sincerity 0.90 → 0.675 | FunkWorkout 50.00%; GrooveRiffVamp 50.00% |

Folk’s maximum center gap is 0.0167 and is **not comparable** to Gospel’s 0.15 gap. The contradiction in the previous report and its generator has been corrected.

## Failures, limits and verification

Package C’s old failure is preserved; D proceeded under the new explicit authorization. The fixed-world production-resolved Gospel shortfall remains in both retained later frames. Shape variation does not constitute an accepted selector repair. New cross-genre quarterly range flags remain as shown. The completed fresh trajectories determine the Q3 Gospel gate; the smaller non-Gospel panels cannot establish a full-population safety gate. No production within-bucket ordering repair, repertoire orientation, performance role or hold-out work was done.

One initial endpoint-only trajectory was gracefully stopped and retained as superseded evidence when its aggregation was found insufficient for monthly-comparable quarterly totals; it is excluded from final measurements. Initial wrappers could not inspect saves beyond JavaScript’s maximum string length; a streaming metadata reader recovered all completed slices without rerunning them. Final adapter review found A-only Rock reference-fit sampling included shrinkage, and H Rock sampling used the old reference shape. The affected Rock cells were remeasured on all four frames using precomputed per-variant reference fits; baseline ordering, worlds and pools remained exact. Initial Rock A/H cells are superseded. Two initial sandboxed Godot launches crashed before project initialization; reruns using the installed runtime completed. The final build has zero errors and four existing warnings. Composition follow-up checks and existing A/B, fit, behavior and player-perception checks pass. Source packages have separate D/H/F/E/G/center-review checkpoints.

Reproduce with `dotnet build "Label Man.csproj" --no-restore`, the fixed-world helper’s `-Followup`, `-CaptureOnly`, `-RockOnly` and `-ContextFixture` switches using fresh run names, and `run-polar-directive2-trajectories.ps1` using fresh tags. Pass new trajectory tags as arguments to `analyze-polar-directive2-trajectories.mjs` to select those completed runs; update the fixed-world analyzer’s frame/prefix specifications for fresh probe names. The trajectory wrapper runs 91 weeks to September 29, 1961 with census none and persisted H flags. Explicit `--composition-shape-v1=on`/`off` can override a loaded world’s saved flag; absent explicit arguments, its saved choice wins and older saves stay off. The two analysis scripts reject malformed CSVs, changed actor/pool/original/cover invariants, incomplete months/weeks, failed snapshot round trips and any H-on constraint violation. Main outputs are this report, both analysis JSONs, shape CSV, per-act bucket JSONs and checkpoint files. The shape measures use the dedicated composition stream; early `repair-fits.csv` H-frozen rows logged the old arrangement’s axes alongside the correct counterfactual fit and are not used as shape evidence. That output-only column has been corrected for future probes.
