# Easy Listening soundtrack repair

October 5, 2026. Run tag v1; development seeds 1001/1002.

Easy Listening screen themes and stage/film songs now compete with ordinary songs on fit and ranking within the existing selected source. Vocal Easy acts may take screen themes. No calibration, access tiers, source weights or original allocation changed.

## Implementation

- [Systems/LiveRepertoire.cs:17](C:/Project/Label-Man/Systems/LiveRepertoire.cs:17): Easy-only exemption predicate and non-persisted comparator switch; [vocal compatibility at line 40](C:/Project/Label-Man/Systems/LiveRepertoire.cs:40).
- [Systems/SongMaterialSelectionService.cs:649](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:649): Easy media joins the ordinary ranking within each source at lines 654–657; bypasses the media opportunity and family lottery. [Line 620](C:/Project/Label-Man/Systems/SongMaterialSelectionService.cs:620) also bypasses the generic media lottery for Easy without a source mix.
- Census reference disables only the Easy exemption, restoring the artist repertoire state and using the same private census RNG. Other repair phases remain enabled.

## Matched results

| Population | Seed | Screen before → after | Stage/film before → after | External total before → after | Inherited before → after | Pre-1940 before → after |
|---|---|---:|---:|---:|---:|---:|
| Unsigned | 1001 | 1.60% → 23.06% | 5.92% → 6.48% | 7.51% → 29.54% | 67.38% → 67.38% | 49.52% → 49.52% |
| Unsigned | 1002 | 2.25% → 15.11% | 6.19% → 13.36% | 8.44% → 28.47% | 68.12% → 67.56% | 53.51% → 53.51% |
| Unsigned | pooled | 1.92% → 19.16% | 6.05% → 9.85% | 7.97% → 29.01% | 67.74% → 67.47% | 51.47% → 51.47% |
| Signed | 1001 | 0.94% → 19.82% | 3.35% → 5.95% | 4.28% → 25.77% | 69.48% → 69.48% | 51.48% → 51.48% |
| Signed | 1002 | 1.71% → 19.49% | 4.89% → 10.50% | 6.60% → 29.98% | 67.47% → 67.47% | 55.05% → 55.05% |
| Signed | pooled | 1.32% → 19.65% | 4.12% → 8.22% | 5.44% → 27.87% | 68.48% → 68.48% | 53.25% → 53.25% |

The pre-repair soundtrack share of 27.06% is a comparison point, not a cap. All tables use all filled weighted slots as their denominator.

## Full existing-cover family tables

### Unsigned, seed 1001

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / Classical | 0.40% | 0.00% |
| ArtistOriginal / EasyListening | 4.13% | 0.00% |
| ArtistOriginal / Folk | 1.23% | 0.00% |
| ArtistOriginal / Jazz | 0.89% | 0.38% |
| ArtistOriginal / TeenPop | 0.28% | 0.00% |
| ArtistOriginal / TraditionalPop | 1.89% | 0.00% |
| Brazilian songbook | 1.35% | 0.00% |
| Christmas Standard | 0.35% | 0.00% |
| Country Standard | 0.84% | 0.00% |
| Jazz Standard | 1.75% | 0.00% |
| ProfessionalOffice / EasyListening | 0.39% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.12% | 0.00% |
| Recent Country Hit | 0.53% | 0.00% |
| Recent Pop Hit | 2.05% | 0.17% |
| Recent Teen Hit | 3.75% | 0.00% |
| Screen instrumental | 1.60% | 23.06% |
| Stage and film songs | 5.92% | 6.48% |
| Tin Pan Alley | 2.95% | 0.34% |

### Unsigned, seed 1002

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / Classical | 0.66% | 0.00% |
| ArtistOriginal / EasyListening | 3.54% | 0.00% |
| ArtistOriginal / Folk | 0.69% | 0.00% |
| ArtistOriginal / Jazz | 0.85% | 0.00% |
| ArtistOriginal / TeenPop | 0.12% | 0.00% |
| ArtistOriginal / TraditionalPop | 0.83% | 0.00% |
| Brazilian songbook | 1.52% | 0.00% |
| Christmas Standard | 0.80% | 0.23% |
| Contemporary folk | 0.23% | 0.00% |
| Country Standard | 0.89% | 0.10% |
| Jazz Standard | 0.19% | 0.00% |
| ProfessionalOffice / EasyListening | 0.13% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.57% | 0.00% |
| Recent Country Hit | 0.70% | 0.00% |
| Recent Pop Hit | 2.03% | 0.00% |
| Recent Teen Hit | 1.97% | 0.00% |
| Screen instrumental | 2.25% | 15.11% |
| Stage and film songs | 6.19% | 13.36% |
| Tin Pan Alley | 4.42% | 0.33% |

### Unsigned, seed pooled

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / Classical | 0.52% | 0.00% |
| ArtistOriginal / EasyListening | 3.84% | 0.00% |
| ArtistOriginal / Folk | 0.97% | 0.00% |
| ArtistOriginal / Jazz | 0.87% | 0.19% |
| ArtistOriginal / TeenPop | 0.20% | 0.00% |
| ArtistOriginal / TraditionalPop | 1.37% | 0.00% |
| Brazilian songbook | 1.44% | 0.00% |
| Christmas Standard | 0.57% | 0.11% |
| Contemporary folk | 0.11% | 0.00% |
| Country Standard | 0.87% | 0.05% |
| Jazz Standard | 0.99% | 0.00% |
| ProfessionalOffice / EasyListening | 0.26% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.34% | 0.00% |
| Recent Country Hit | 0.61% | 0.00% |
| Recent Pop Hit | 2.04% | 0.09% |
| Recent Teen Hit | 2.88% | 0.00% |
| Screen instrumental | 1.92% | 19.16% |
| Stage and film songs | 6.05% | 9.85% |
| Tin Pan Alley | 3.67% | 0.33% |

### Signed, seed 1001

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / Classical | 0.49% | 0.00% |
| ArtistOriginal / EasyListening | 4.06% | 0.00% |
| ArtistOriginal / TraditionalPop | 2.95% | 0.00% |
| Brazilian songbook | 1.12% | 0.00% |
| Christmas Standard | 0.61% | 0.12% |
| Contemporary folk | 0.41% | 0.00% |
| Country Standard | 1.21% | 0.00% |
| Jazz Standard | 1.85% | 0.61% |
| ProfessionalOffice / EasyListening | 0.30% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.85% | 0.19% |
| Recent Country Hit | 0.69% | 0.00% |
| Recent Pop Hit | 2.92% | 0.00% |
| Recent Teen Hit | 3.49% | 0.57% |
| Screen instrumental | 0.94% | 19.82% |
| Stage and film songs | 3.35% | 5.95% |
| Tin Pan Alley | 2.39% | 0.37% |

### Signed, seed 1002

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / BossaNova | 0.12% | 0.00% |
| ArtistOriginal / Classical | 0.27% | 0.00% |
| ArtistOriginal / Country | 0.13% | 0.00% |
| ArtistOriginal / EasyListening | 5.54% | 0.00% |
| ArtistOriginal / Folk | 0.40% | 0.00% |
| ArtistOriginal / Jazz | 1.04% | 0.00% |
| ArtistOriginal / TeenPop | 0.23% | 0.00% |
| ArtistOriginal / TraditionalPop | 1.24% | 0.00% |
| Brazilian songbook | 0.46% | 0.00% |
| Christmas Standard | 1.77% | 0.13% |
| Contemporary folk | 0.95% | 0.00% |
| Country Standard | 0.46% | 0.00% |
| Jazz Standard | 0.84% | 0.23% |
| ProfessionalOffice / TeenPop | 0.27% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.80% | 0.00% |
| Recent Country Hit | 1.02% | 0.00% |
| Recent Pop Hit | 2.70% | 0.25% |
| Recent Teen Hit | 3.41% | 0.00% |
| Screen instrumental | 1.71% | 19.49% |
| Stage and film songs | 4.89% | 10.50% |
| Tin Pan Alley | 2.98% | 0.63% |

### Signed, seed pooled

| Family | Before | After |
|---|---:|---:|
| ArtistOriginal / BossaNova | 0.06% | 0.00% |
| ArtistOriginal / Classical | 0.38% | 0.00% |
| ArtistOriginal / Country | 0.06% | 0.00% |
| ArtistOriginal / EasyListening | 4.79% | 0.00% |
| ArtistOriginal / Folk | 0.20% | 0.00% |
| ArtistOriginal / Jazz | 0.52% | 0.00% |
| ArtistOriginal / TeenPop | 0.11% | 0.00% |
| ArtistOriginal / TraditionalPop | 2.10% | 0.00% |
| Brazilian songbook | 0.79% | 0.00% |
| Christmas Standard | 1.19% | 0.13% |
| Contemporary folk | 0.68% | 0.00% |
| Country Standard | 0.83% | 0.00% |
| Jazz Standard | 1.35% | 0.42% |
| ProfessionalOffice / EasyListening | 0.15% | 0.00% |
| ProfessionalOffice / TeenPop | 0.14% | 0.00% |
| ProfessionalOffice / TraditionalPop | 0.83% | 0.09% |
| Recent Country Hit | 0.85% | 0.00% |
| Recent Pop Hit | 2.81% | 0.13% |
| Recent Teen Hit | 3.45% | 0.28% |
| Screen instrumental | 1.32% | 19.65% |
| Stage and film songs | 4.12% | 8.22% |
| Tin Pan Alley | 2.68% | 0.50% |

## Regression results

Other genres: PASS for the 0.5 pp external-media and inherited-share limits across per-seed and pooled signed/unsigned results.
Build: dotnet build --no-restore -v quiet passes with four existing warnings and no errors.
Easy inherited alert: No movement above 1 pp from the handoff references.

The repair check includes catalogue-count opportunity invariance for other genres, retained book counts, rights/save migration, Easy source preservation, ranked media competition, vocal eligibility and global-RNG preservation. All four named checks pass on both seeds.
Actual resolver: 286 observed vocal Easy screen-theme slots (945.67 weighted slots) in the matched census. The whole repertoire calibration file has the same SHA-256 as the v3 invocation.
The first Directive 3 invocation incorrectly forced shape v2 on, conflicting with its legacy-save defaults check. The corrected invocation uses default shape flags and passes; the failed log remains retained.

| Fresh Gospel seed | Inherited | Secular | Accepted |
|---|---:|---:|---|
| 1001 | 69.59% | 0.00% | PASS |
| 1002 | 66.14% | 0.00% | PASS |

## Jazz observation

| Population | Seed | Screen | Stage/film | External total | Inherited |
|---|---|---:|---:|---:|---:|
| Unsigned | 1001 | 0.00% | 1.81% | 1.81% | 43.76% |
| Unsigned | 1002 | 0.00% | 1.36% | 1.36% | 42.55% |
| Unsigned | pooled | 0.00% | 1.58% | 1.58% | 43.16% |
| Signed | 1001 | 0.00% | 2.67% | 2.67% | 34.72% |
| Signed | 1002 | 0.00% | 0.09% | 0.09% | 41.26% |
| Signed | pooled | 0.00% | 1.35% | 1.35% | 38.08% |

## Source SHA-256 hashes

| Source | SHA-256 |
|---|---|
| Systems/LiveRepertoire.cs | b98f79060c79e57798ecf6c4e98fd0c3fcc02bb4dcdda067de409ba5f5ecd7d1 |
| Systems/SongMaterialSelectionService.cs | 6ba3feed618758e6dfd918f5b3261a66146885bca9ec61d2a2ccc2ad9abc8fe6 |
| SimTools/ChartAuditRunner.GenreFollowUp.cs | 952c2a191ea53ee4a8f90accdfe722fa7ffd6ab690f1997399c0890cb573e9e2 |
| SimTools/ChartAuditRunner.PolarResearch.cs | e291369c12ae05d59464f06481d5c859ef4778fdc28048d1eb5fbe2bc180d7e5 |
| SimTools/GenreRepertoireRepairChecks.cs | 1b93c0676338872c0c69f0f99ab9cb92eeb202b0b7460cfa7ae792cb4366d61a |
| SimTools/run-polar-research.ps1 | 1f8fdd927b68f0afd54df79a55df78c24980e5f38922a107722d39529a456bda |
| SimTools/run-easy-soundtrack-repair.ps1 | 69b4961f0a94112a797d7868b46fb91bad84fdb9d0a11ce79d5bba2bea2b5f12 |
| SimTools/analyze-genre-repair-gospel.mjs | f14321a9a71cca6e7ab7f081019b52513a1071b6127870d418f6abdc38cca1a9 |
| SimTools/analyze-easy-soundtrack-repair.mjs | 8ee897628ada720ea11e7486d2a4540004bc1f4a8f4761a69da594c03ae45c3e |

Complete per-genre deltas, invocation fingerprints, input hashes, source hashes and Gospel trajectory evidence are in EasySoundtrackRepairValidation.json. Partial census frames are excluded.
