# Polar Gospel repair checkpoint

October 4, 2026. The user requested implementation of the attached repair directive. Packages A and B and their bounded measurement are implemented in the working tree. This is a failed acceptance checkpoint, not an accepted repair. Earlier uncommitted work and retained evidence were preserved.

## Findings

**Package C fails. The repair is not ready for hold-out validation.** Frozen-arrangement A+B reaches 50.41% Gospel inherited share in the March 10, 1961 seed-1001 frame, but only 46.78% in the February 10, 1961 seed-1002 frame. Startup passes at 60.36% and 63.80%. Re-resolving arrangements gives 47.52% and 43.25% later, so the implemented production path has not passed either later frame. No band, quota, moment weight, moment axes, or archetype shape constant was changed.

The bounded cross-genre panel flags 13 inherited-share decreases greater than one percentage point, including seed-1001 Easy Listening moving from 50.00% to 43.33% later. These flags are paired sample effects, not full-population estimates; the standards-led Easy Listening sample fails its existing 50–85% range in that frame. All measured set-fill counts are unchanged. The directive says to stop at a failed gate and before Package D if cross-genre regressions appear. **Package D was not implemented; no sacred fixtures, context selector, or hold-out run was attempted.**

Sparse-taste shrinkage reduces the earlier extreme cross-seed market effect but does not solve inherited-repertoire retention in both worlds. Initial Gospel actors gained 7,327 and 5,306 candidate instances in the prior paired-pool analysis, all ArtistOriginal; the paired probes do not reduce those pools. All explicit context remains unknown. Pool growth remains a documented competing mechanism; missing context remains an unmeasured mechanism, not proof of secular adaptation.

## Genre prior audit and implementation

Act identity is derived by `PolarActProfileDeriver.Derive`, rather than persisted as a prior-seeded identity vector. `polar-repair-v2` computes each genre center once at table load from weighted archetype identity axes. Existing songbooks supply their existing intended shares; genres without a matching authored book have explicit provisional equal-share mixtures in `genreForms`. No exact historical numbers were sourced. These new mixture choices need playtesting and review, particularly the large gaps below. A change to an archetype or songbook share automatically changes its center. Independent numeric prior identities were removed from the active JSON. The old table is retained only as a development comparator fixture.

New and loaded acts derive the current center while keeping performer adjustments, history blending, capabilities, membership and all stored attributes. No act migration is appropriate because there is no persisted act identity vector to selectively recompute. Historical recording taxonomy and cached musical shapes remain intact. `ValidatePriorCenters` runs at parse and in the repair test; a deliberately divergent cached prior is rejected.

Axis order throughout is Toughness, Sophistication, Sincerity, Maturity. Gap is derived minus old; sorting uses the largest absolute per-axis gap. “Comparable” means at least Gospel’s 0.15 maximum gap. Legacy enum aliases are listed too; their old center reproduces the old code’s direct catalogue lookup and Pop fallback.

| Genre | Old prior | Derived center | Gap | Comparable | Authored shares |
| --- | --- | --- | --- | --- | --- |
| ProgressiveRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.5750, 0.7750, 0.7750, 0.7500] | [-0.1750, 0.4750, 0.0750, 0.3000] | yes | SuiteMultiPart:1; LightAndShadeEpic:1 |
| DooWop | [0.7000, 0.3500, 0.9000, 0.6500] | [0.3000, 0.4167, 0.7333, 0.4833] | [-0.4000, 0.0667, -0.1667, -0.1667] | yes | Recent DooWop Hit |
| PsychedelicRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.4833, 0.6833, 0.6167, 0.6833] | [-0.2667, 0.3833, -0.0833, 0.2333] | yes | RagaModalDrone:1; Collage:1; JamExtendedWorkout:1 |
| BritishInvasion | [0.2500, 0.6000, 0.6500, 0.5500] | [0.5167, 0.3167, 0.6667, 0.4333] | [0.2667, -0.2833, 0.0167, -0.1167] | yes | MidTempoRocker:1; BrightPopNumber:1; ShuffleTwelveBar:1 |
| BritishBeat | [0.7500, 0.3000, 0.7000, 0.4500] | [0.5167, 0.3167, 0.6667, 0.4333] | [-0.2333, 0.0167, -0.0333, -0.0167] | yes | MidTempoRocker:1; BrightPopNumber:1; ShuffleTwelveBar:1 |
| RootsRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.5167, 0.4167, 0.7667, 0.5833] | [-0.2333, 0.1167, 0.0667, 0.1333] | yes | CountryTwoBeat:1; ShuffleTwelveBar:1; VerseDrivenSong:1 |
| Motown | [0.2500, 0.6000, 0.6500, 0.5500] | [0.4833, 0.4167, 0.8833, 0.6500] | [0.2333, -0.1833, 0.2333, 0.1000] | yes | HornDrivenSoulNumber:1; DeepSoulPleader:1; SlowBallad:1 |
| Psychedelic | [0.2500, 0.6000, 0.6500, 0.5500] | [0.4833, 0.6833, 0.6167, 0.6833] | [0.2333, 0.0833, -0.0333, 0.1333] | yes | RagaModalDrone:1; Collage:1; JamExtendedWorkout:1 |
| Skiffle | [0.2500, 0.6000, 0.6500, 0.5500] | [0.4167, 0.6500, 0.8833, 0.6833] | [0.1667, 0.0500, 0.2333, 0.1333] | yes | Folk Traditional |
| Funk | [0.7000, 0.3500, 0.9000, 0.6500] | [0.7500, 0.2500, 0.6750, 0.5750] | [0.0500, -0.1000, -0.2250, -0.0750] | yes | FunkWorkout:1; GrooveRiffVamp:1 |
| BossaNova | [0.3500, 0.8500, 0.7000, 0.8500] | [0.2000, 0.6833, 0.6833, 0.6333] | [-0.1500, -0.1667, -0.0167, -0.2167] | yes | Brazilian songbook |
| Soul | [0.7000, 0.3500, 0.9000, 0.6500] | [0.4833, 0.4167, 0.8833, 0.6500] | [-0.2167, 0.0667, -0.0167, -0.0000] | yes | HornDrivenSoulNumber:1; DeepSoulPleader:1; SlowBallad:1 |
| BluesRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.6333, 0.3167, 0.7833, 0.6667] | [-0.1167, 0.0167, 0.0833, 0.2167] | yes | ShuffleTwelveBar:1; SlowBlues:1; JamExtendedWorkout:1 |
| BritishBlues | [0.7500, 0.3000, 0.7000, 0.4500] | [0.6333, 0.3167, 0.7833, 0.6667] | [-0.1167, 0.0167, 0.0833, 0.2167] | yes | ShuffleTwelveBar:1; SlowBlues:1; JamExtendedWorkout:1 |
| AcidRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.6833, 0.4333, 0.7333, 0.6500] | [-0.0667, 0.1333, 0.0333, 0.2000] | yes | JamExtendedWorkout:1; LightAndShadeEpic:1; GrooveRiffVamp:1 |
| ProtoMetal | [0.7500, 0.3000, 0.7000, 0.4500] | [0.6750, 0.4500, 0.7500, 0.6500] | [-0.0750, 0.1500, 0.0500, 0.2000] | yes | LightAndShadeEpic:1; GrooveRiffVamp:1 |
| FolkRock | [0.4000, 0.6500, 0.9000, 0.7000] | [0.5833, 0.5667, 0.8333, 0.6167] | [0.1833, -0.0833, -0.0667, -0.0833] | yes | VerseDrivenSong:1; ProtestMessageSong:1; MidTempoRocker:1 |
| CountryRock | [0.4500, 0.4000, 0.9000, 0.7000] | [0.5667, 0.2833, 0.7167, 0.5167] | [0.1167, -0.1167, -0.1833, -0.1833] | yes | CountryTwoBeat:1; CountryShuffle:1; MidTempoRocker:1 |
| SkaRocksteady | [0.2500, 0.6000, 0.6500, 0.5500] | [0.4250, 0.4500, 0.5750, 0.4000] | [0.1750, -0.1500, -0.0750, -0.1500] | yes | LatinDance:1; DanceNumber:1 |
| Ska | [0.5000, 0.3500, 0.7500, 0.5500] | [0.4250, 0.4500, 0.5750, 0.4000] | [-0.0750, 0.1000, -0.1750, -0.1500] | yes | LatinDance:1; DanceNumber:1 |
| Reggae | [0.5000, 0.3500, 0.7500, 0.5500] | [0.5500, 0.3000, 0.5750, 0.4500] | [0.0500, -0.0500, -0.1750, -0.1000] | yes | GrooveRiffVamp:1; DanceNumber:1 |
| Rocksteady | [0.5000, 0.3500, 0.7500, 0.5500] | [0.3250, 0.4500, 0.6750, 0.5000] | [-0.1750, 0.1000, -0.0750, -0.0500] | yes | DanceNumber:1; SlowBallad:1 |
| RockAndRoll | [0.7500, 0.3000, 0.7000, 0.4500] | [0.5750, 0.3250, 0.7250, 0.5125] | [-0.1750, 0.0250, 0.0250, 0.0625] | yes | Recent RnR Hit |
| TeenPop | [0.2000, 0.4000, 0.5500, 0.2500] | [0.3000, 0.4500, 0.6500, 0.4250] | [0.1000, 0.0500, 0.1000, 0.1750] | yes | Recent Teen Hit |
| Gospel | [0.5000, 0.3500, 0.9500, 0.7000] | [0.3500, 0.3875, 0.9250, 0.7125] | [-0.1500, 0.0375, -0.0250, 0.0125] | yes | Gospel Standard |
| GirlGroup | [0.2500, 0.6000, 0.6500, 0.5500] | [0.3000, 0.4500, 0.6500, 0.4250] | [0.0500, -0.1500, 0.0000, -0.1250] | yes | Recent Teen Hit |
| Bubblegum | [0.2500, 0.6000, 0.6500, 0.5500] | [0.3000, 0.4500, 0.6500, 0.4250] | [0.0500, -0.1500, 0.0000, -0.1250] | yes | Recent Teen Hit |
| PopRock | [0.2500, 0.6000, 0.6500, 0.5500] | [0.4000, 0.4500, 0.7500, 0.5167] | [0.1500, -0.1500, 0.1000, -0.0333] | yes | MidTempoRocker:1; MidTempoPopSong:1; SlowBallad:1 |
| Country | [0.4500, 0.4000, 0.9000, 0.7000] | [0.3500, 0.5000, 0.7643, 0.6286] | [-0.1000, 0.1000, -0.1357, -0.0714] | no | Country Standard |
| Blues | [0.6500, 0.3000, 0.9000, 0.7500] | [0.6167, 0.2667, 0.7667, 0.6333] | [-0.0333, -0.0333, -0.1333, -0.1167] | no | Blues Standard |
| Childrens | [0.3000, 0.4500, 0.3500, 0.5500] | [0.2667, 0.3833, 0.4833, 0.4333] | [-0.0333, -0.0667, 0.1333, -0.1167] | no | Children songs |
| PsychedelicPop | [0.2500, 0.6000, 0.6500, 0.5500] | [0.2667, 0.7333, 0.7667, 0.6000] | [0.0167, 0.1333, 0.1167, 0.0500] | no | ChamberBaroquePiece:1; RagaModalDrone:1; MidTempoPopSong:1 |
| Boogaloo | [0.4500, 0.5000, 0.7000, 0.6000] | [0.5167, 0.4167, 0.6667, 0.4667] | [0.0667, -0.0833, -0.0333, -0.1333] | no | LatinDance:1; DanceNumber:1; HornDrivenSoulNumber:1 |
| SurfRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.6250, 0.3000, 0.6000, 0.4500] | [-0.1250, 0.0000, -0.1000, 0.0000] | no | Surf adaptations |
| BaroquePop | [0.2000, 0.8500, 0.7500, 0.6000] | [0.3250, 0.7250, 0.8750, 0.7000] | [0.1250, -0.1250, 0.1250, 0.1000] | no | ChamberBaroquePiece:1; DramaticBalladBigBuild:1 |
| TexMex | [0.4500, 0.5000, 0.7000, 0.6000] | [0.3250, 0.5250, 0.7750, 0.6250] | [-0.1250, 0.0250, 0.0750, 0.0250] | no | TexMex songbook |
| LatinPop | [0.4500, 0.5000, 0.7000, 0.6000] | [0.3250, 0.6000, 0.7000, 0.5500] | [-0.1250, 0.1000, 0.0000, -0.0500] | no | Latin songbook |
| TraditionalPop | [0.2000, 0.8500, 0.7500, 0.8500] | [0.2200, 0.7400, 0.7600, 0.7900] | [0.0200, -0.1100, 0.0100, -0.0600] | no | Tin Pan Alley |
| HardRock | [0.7500, 0.3000, 0.7000, 0.4500] | [0.7167, 0.3667, 0.7000, 0.5500] | [-0.0333, 0.0667, 0.0000, 0.1000] | no | StomperRocker:1; LightAndShadeEpic:1; GrooveRiffVamp:1 |
| GarageRock | [0.8500, 0.1500, 0.7000, 0.3500] | [0.7500, 0.2500, 0.6500, 0.4000] | [-0.1000, 0.1000, -0.0500, 0.0500] | no | StomperRocker:1; MidTempoRocker:1 |
| Jazz | [0.3500, 0.8500, 0.7000, 0.8500] | [0.2600, 0.8000, 0.7100, 0.7800] | [-0.0900, -0.0500, 0.0100, -0.0700] | no | Jazz Standard |
| Classical | [0.2000, 0.9500, 0.8000, 0.8500] | [0.2333, 0.9167, 0.7167, 0.8000] | [0.0333, -0.0333, -0.0833, -0.0500] | no | Classical works |
| RnB | [0.7000, 0.3500, 0.9000, 0.6500] | [0.6200, 0.3000, 0.8200, 0.6400] | [-0.0800, -0.0500, -0.0800, -0.0100] | no | R&B Catalog |
| EasyListening | [0.1500, 0.8000, 0.7000, 0.8000] | [0.1750, 0.7750, 0.7750, 0.7875] | [0.0250, -0.0250, 0.0750, -0.0125] | no | ReverieMoodPiece:1; LushStandard:1; SaloonBallad:1; CharmSong:1 |
| ProtoPunk | [0.7500, 0.3000, 0.7000, 0.4500] | [0.7250, 0.2250, 0.6250, 0.4500] | [-0.0250, -0.0750, -0.0750, 0.0000] | no | StomperRocker:1; GrooveRiffVamp:1 |
| SunshinePop | [0.2500, 0.6000, 0.6500, 0.5500] | [0.2250, 0.5250, 0.6875, 0.5000] | [-0.0250, -0.0750, 0.0375, -0.0500] | no | Recent Pop Hit |
| BritishPop | [0.2500, 0.6000, 0.6500, 0.5500] | [0.2250, 0.5250, 0.6875, 0.5000] | [-0.0250, -0.0750, 0.0375, -0.0500] | no | Recent Pop Hit |
| Comedy | [0.3000, 0.4500, 0.3500, 0.5500] | [0.3000, 0.4833, 0.4000, 0.5667] | [0.0000, 0.0333, 0.0500, 0.0167] | no | Comedy routines |
| Folk | [0.4000, 0.6500, 0.9000, 0.7000] | [0.4167, 0.6500, 0.8833, 0.6833] | [0.0167, 0.0000, -0.0167, -0.0167] | no | Folk Traditional |
| ContemporaryFolk | [0.4000, 0.6500, 0.9000, 0.7000] | [0.4167, 0.6500, 0.8833, 0.6833] | [0.0167, 0.0000, -0.0167, -0.0167] | no | Contemporary folk |
| SingerSongwriter | [0.4000, 0.6500, 0.9000, 0.7000] | [0.4167, 0.6500, 0.8833, 0.6833] | [0.0167, 0.0000, -0.0167, -0.0167] | no | VerseDrivenSong:1; SlowBallad:1; ProtestMessageSong:1 |

Folk’s maximum gap is 0.0167, including [0.0167, 0.0000, -0.0167, -0.0167]. It is not comparable to Gospel's 0.15 maximum gap. Gospel derives [0.3500, 0.3875, 0.9250, 0.7125] from QuietHymn, GospelQuartet, GospelChoir and SpiritualShout at their existing equal shares.

## Taste shrinkage

The pseudo-count is **k = 4**, chosen as a small, understandable evidence threshold: one charting single contributes 20%, two contribute 33.3%, and eight contribute 66.7% of the adjusted taste. Sensitivity at k=2 and k=8 is reported below; it was not used to retune away the failure. Taste is `(n × raw drifting mean + k × derived center) / (n + k)`. The persisted raw mean and 0.2 drift remain unchanged. The adjustment is applied at both reference-fit and resolved-fit consumption, without feeding adjusted values back into drift. Toughness, Sophistication and Maturity and the 0.20 moment weight are unchanged; the original as-of-week guard is retained. No snapshot still means the original constant neutral startup moment. Package B preserves startup selection order exactly.

New worlds persist a versioned set of distinct chart-single record IDs; repeated chart weeks do not increase the count. Older saves did not persist cumulative IDs. They use their last-week observation count as a conservative floor, combined by `max` with subsequently known distinct IDs to avoid double counting. The retained fixed worlds therefore use the available legacy n=2 and n=1 for Gospel; a fresh repaired trajectory might accumulate more evidence and is not claimed here. Save/load of the new fields, repeated-chart handling, drift preservation and week gating pass the repair test.

## Combined variants

Every frame uses the complete current Gospel cohort and a bounded panel of at most four acts per other genre/signing-status cell. Actors and accessible pools are identical within a frame; originals and requested covers stay fixed. The first four variants freeze the baseline musical arrangements. The fifth re-resolves them. Production currently resolves arrangements, so variant 5 is a practical limitation as well as variant 4’s explicit gate failure. Gospel “outside” means no Gospel Standard candidate enters the first 0.025 suitability window. Capability/identity/moment columns compare best overall versus best Gospel-book candidate and already include their 0.35/0.45/0.20 weights; their sum is the score gap.

| Seed / date | Variant | Inherited / filled slots | Share | Outside / acts | Capability gap | Identity gap | Moment gap |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1001 / 1/1/1960 | baseline | 74/222 | 33.33% | 32/53 | 0.001256 | 0.025448 | 0.000000 |
| 1001 / 1/1/1960 | packageA | 134/222 | 60.36% | 1/53 | 0.000401 | 0.000382 | 0.000000 |
| 1001 / 1/1/1960 | packageB | 74/222 | 33.33% | 32/53 | 0.001256 | 0.025448 | 0.000000 |
| 1001 / 1/1/1960 | packageAB | 134/222 | 60.36% | 1/53 | 0.000401 | 0.000382 | 0.000000 |
| 1001 / 1/1/1960 | packageABResolved | 131/222 | 59.01% | 1/53 | 0.000593 | 0.000299 | 0.000000 |
| 1001 / 1/1/1960 | packageABHalfK | 134/222 | 60.36% | 1/53 | 0.000401 | 0.000382 | 0.000000 |
| 1001 / 1/1/1960 | packageABDoubleK | 134/222 | 60.36% | 1/53 | 0.000401 | 0.000382 | 0.000000 |
| 1002 / 1/1/1960 | baseline | 53/163 | 32.52% | 31/40 | 0.000698 | 0.030693 | 0.000000 |
| 1002 / 1/1/1960 | packageA | 104/163 | 63.80% | 0/40 | 0.000616 | 0.000711 | 0.000000 |
| 1002 / 1/1/1960 | packageB | 53/163 | 32.52% | 31/40 | 0.000698 | 0.030693 | 0.000000 |
| 1002 / 1/1/1960 | packageAB | 104/163 | 63.80% | 0/40 | 0.000616 | 0.000711 | 0.000000 |
| 1002 / 1/1/1960 | packageABResolved | 103/163 | 63.19% | 0/40 | 0.000517 | 0.000845 | 0.000000 |
| 1002 / 1/1/1960 | packageABHalfK | 104/163 | 63.80% | 0/40 | 0.000616 | 0.000711 | 0.000000 |
| 1002 / 1/1/1960 | packageABDoubleK | 104/163 | 63.80% | 0/40 | 0.000616 | 0.000711 | 0.000000 |
| 1001 / 3/10/1961 | baseline | 63/968 | 6.51% | 198/245 | 0.005829 | 0.020377 | 0.024461 |
| 1001 / 3/10/1961 | packageA | 371/968 | 38.33% | 70/245 | 0.004660 | -0.013888 | 0.024336 |
| 1001 / 3/10/1961 | packageB | 187/968 | 19.32% | 165/245 | 0.003528 | 0.021110 | 0.006964 |
| 1001 / 3/10/1961 | packageAB | 488/968 | 50.41% | 17/245 | 0.002390 | -0.002601 | 0.005411 |
| 1001 / 3/10/1961 | packageABResolved | 460/968 | 47.52% | 19/245 | 0.002117 | -0.001311 | 0.004516 |
| 1001 / 3/10/1961 | packageABHalfK | 477/968 | 49.28% | 23/245 | 0.003273 | -0.005011 | 0.009306 |
| 1001 / 3/10/1961 | packageABDoubleK | 494/968 | 51.03% | 17/245 | 0.001775 | -0.000442 | 0.002652 |
| 1002 / 2/10/1961 | baseline | 377/793 | 47.54% | 3/200 | 0.000829 | 0.013237 | -0.009681 |
| 1002 / 2/10/1961 | packageA | 412/793 | 51.95% | 0/200 | 0.000065 | 0.001586 | -0.000833 |
| 1002 / 2/10/1961 | packageB | 294/793 | 37.07% | 31/200 | 0.005314 | 0.019424 | -0.013236 |
| 1002 / 2/10/1961 | packageAB | 371/793 | 46.78% | 9/200 | -0.001373 | 0.002823 | 0.001051 |
| 1002 / 2/10/1961 | packageABResolved | 343/793 | 43.25% | 10/200 | -0.001524 | 0.003089 | 0.001231 |
| 1002 / 2/10/1961 | packageABHalfK | 376/793 | 47.41% | 3/200 | -0.001104 | 0.002618 | 0.000464 |
| 1002 / 2/10/1961 | packageABDoubleK | 374/793 | 47.16% | 13/200 | -0.000598 | 0.002391 | 0.001028 |

| Frame | All measured acts | Candidate instances | Probe seconds |
| --- | ---: | ---: | ---: |
| 1001 / 1/1/1960 | 134 | 13980 | 8.65 |
| 1002 / 1/1/1960 | 123 | 12561 | 7.84 |
| 1001 / 3/10/1961 | 385 | 97507 | 53.73 |
| 1002 / 2/10/1961 | 340 | 82746 | 46.49 |

Every accepted fixed-world probe completes within its 120-second observation budget. Initialization and snapshot load are outside that probe timer. No ecosystem census or optional fresh repair trajectory was launched. Non-Gospel Rock genres retain their existing weighted-source production selector via scoped probe adapters; other genres use the cached production ordering. No replacement universal selector was introduced.

## Cross-genre effects and regressions

These are paired panel ratios, without population weighting or confidence intervals. “Retention” is the fraction of baseline selected covers still selected after A+B on the same frame; it supplements inherited-share retention but is not longitudinal retention. No filled-slot deficit worsens in these panels. All populated genres are included; absent genres have no outcome and remain untested. Signed/unsigned and available cohort partitions are in the analysis JSON.

| Seed / date | Genre | Observed / population | Inherited before → after | Set fill before → after | Baseline covers retained |
| --- | --- | --- | --- | --- | --- |
| 1001 / 1/1/1960 | Classical | 8/75 | 66.67 → 66.67% | 30/30 → 30/30 | 95.65% |
| 1001 / 1/1/1960 | Comedy | 8/21 | 41.94 → 45.16% | 31/31 → 31/31 | 91.30% |
| 1001 / 1/1/1960 | LatinPop | 4/20 | 43.75 → 56.25% | 16/16 → 16/16 | 57.14% |
| 1001 / 1/1/1960 | EasyListening | 8/151 | 51.52 → 54.55% | 33/33 → 33/33 | 93.55% |
| 1001 / 1/1/1960 | TexMex | 5/5 | 47.83 → 52.17% | 23/23 → 23/23 | 83.33% |
| 1001 / 1/1/1960 | Childrens | 8/10 | 75.76 → 75.76% | 33/33 → 33/33 | 84.00% |
| 1001 / 1/1/1960 | TraditionalPop | 4/285 | 62.50 → 56.25% | 16/16 → 16/16 | 69.23% |
| 1001 / 1/1/1960 | Folk | 4/176 | 76.47 → 76.47% | 17/17 → 17/17 | 100.00% |
| 1001 / 1/1/1960 | Jazz | 4/116 | 27.78 → 33.33% | 18/18 → 18/18 | 84.62% |
| 1001 / 1/1/1960 | RockAndRoll | 4/497 | 0.00 → 0.00% | 17/17 → 17/17 | 85.71% |
| 1001 / 1/1/1960 | Country | 4/189 | 28.57 → 35.71% | 14/14 → 14/14 | 75.00% |
| 1001 / 1/1/1960 | RnB | 4/437 | 6.67 → 13.33% | 15/15 → 15/15 | 90.00% |
| 1001 / 1/1/1960 | Soul | 4/427 | 12.50 → 12.50% | 16/16 → 16/16 | 100.00% |
| 1001 / 1/1/1960 | Gospel | 53/53 | 33.33 → 60.36% | 222/222 → 222/222 | 37.69% |
| 1001 / 1/1/1960 | TeenPop | 4/310 | 5.56 → 22.22% | 18/18 → 18/18 | 33.33% |
| 1001 / 1/1/1960 | DooWop | 4/94 | 13.33 → 20.00% | 15/15 → 15/15 | 50.00% |
| 1001 / 1/1/1960 | Blues | 4/134 | 35.29 → 29.41% | 17/17 → 17/17 | 66.67% |
| 1002 / 1/1/1960 | EasyListening | 8/149 | 65.63 → 65.63% | 32/32 → 32/32 | 93.33% |
| 1002 / 1/1/1960 | LatinPop | 4/29 | 41.18 → 29.41% | 17/17 → 17/17 | 72.73% |
| 1002 / 1/1/1960 | Classical | 8/96 | 42.86 → 42.86% | 28/28 → 28/28 | 100.00% |
| 1002 / 1/1/1960 | Childrens | 8/20 | 62.07 → 62.07% | 29/29 → 29/29 | 88.89% |
| 1002 / 1/1/1960 | Comedy | 8/27 | 36.36 → 33.33% | 33/33 → 33/33 | 96.67% |
| 1002 / 1/1/1960 | TexMex | 7/7 | 60.00 → 50.00% | 30/30 → 30/30 | 61.90% |
| 1002 / 1/1/1960 | Folk | 4/158 | 70.59 → 76.47% | 17/17 → 17/17 | 86.67% |
| 1002 / 1/1/1960 | Jazz | 4/147 | 60.00 → 60.00% | 15/15 → 15/15 | 76.92% |
| 1002 / 1/1/1960 | TraditionalPop | 4/266 | 14.29 → 14.29% | 14/14 → 14/14 | 100.00% |
| 1002 / 1/1/1960 | Country | 4/178 | 44.44 → 50.00% | 18/18 → 18/18 | 81.25% |
| 1002 / 1/1/1960 | RockAndRoll | 4/496 | 0.00 → 0.00% | 17/17 → 17/17 | 92.86% |
| 1002 / 1/1/1960 | RnB | 4/427 | 17.65 → 17.65% | 17/17 → 17/17 | 100.00% |
| 1002 / 1/1/1960 | Soul | 4/423 | 12.50 → 12.50% | 16/16 → 16/16 | 100.00% |
| 1002 / 1/1/1960 | Gospel | 40/40 | 32.52 → 63.80% | 163/163 → 163/163 | 41.38% |
| 1002 / 1/1/1960 | TeenPop | 4/328 | 7.14 → 21.43% | 14/14 → 14/14 | 0.00% |
| 1002 / 1/1/1960 | DooWop | 4/122 | 11.76 → 5.88% | 17/17 → 17/17 | 85.71% |
| 1002 / 1/1/1960 | Blues | 4/87 | 20.00 → 20.00% | 15/15 → 15/15 | 100.00% |
| 1001 / 3/10/1961 | Comedy | 8/215 | 41.38 → 41.38% | 29/29 → 29/29 | 80.00% |
| 1001 / 3/10/1961 | LatinPop | 8/147 | 20.59 → 29.41% | 34/34 → 34/34 | 34.78% |
| 1001 / 3/10/1961 | Classical | 8/289 | 43.75 → 43.75% | 32/32 → 32/32 | 100.00% |
| 1001 / 3/10/1961 | EasyListening | 8/526 | 50.00 → 43.33% | 30/30 → 30/30 | 73.33% |
| 1001 / 3/10/1961 | TexMex | 8/68 | 34.38 → 28.13% | 32/32 → 32/32 | 85.00% |
| 1001 / 3/10/1961 | Childrens | 8/137 | 53.57 → 53.57% | 28/28 → 28/28 | 53.33% |
| 1001 / 3/10/1961 | Folk | 8/540 | 64.86 → 67.57% | 37/37 → 37/37 | 59.38% |
| 1001 / 3/10/1961 | Country | 8/796 | 38.24 → 38.24% | 34/34 → 34/34 | 100.00% |
| 1001 / 3/10/1961 | RnB | 8/1123 | 13.33 → 13.33% | 30/30 → 30/30 | 91.67% |
| 1001 / 3/10/1961 | RockAndRoll | 8/1272 | 0.00 → 0.00% | 28/28 → 28/28 | 100.00% |
| 1001 / 3/10/1961 | Soul | 8/1170 | 10.53 → 10.53% | 38/38 → 38/38 | 96.43% |
| 1001 / 3/10/1961 | Gospel | 245/245 | 6.51 → 50.41% | 968/968 → 968/968 | 5.67% |
| 1001 / 3/10/1961 | DooWop | 8/486 | 9.09 → 9.09% | 33/33 → 33/33 | 57.69% |
| 1001 / 3/10/1961 | TraditionalPop | 8/798 | 40.63 → 40.63% | 32/32 → 32/32 | 83.33% |
| 1001 / 3/10/1961 | SurfRock | 8/63 | 3.03 → 3.03% | 33/33 → 33/33 | 100.00% |
| 1001 / 3/10/1961 | Jazz | 8/422 | 52.78 → 36.11% | 36/36 → 36/36 | 50.00% |
| 1001 / 3/10/1961 | TeenPop | 8/889 | 3.03 → 18.18% | 33/33 → 33/33 | 37.04% |
| 1001 / 3/10/1961 | Blues | 8/377 | 36.36 → 27.27% | 33/33 → 33/33 | 81.82% |
| 1001 / 3/10/1961 | ContemporaryFolk | 4/17 | 35.00 → 35.00% | 20/20 → 20/20 | 93.75% |
| 1002 / 2/10/1961 | EasyListening | 8/498 | 47.06 → 52.94% | 34/34 → 34/34 | 35.48% |
| 1002 / 2/10/1961 | Classical | 8/308 | 28.13 → 43.75% | 32/32 → 32/32 | 71.43% |
| 1002 / 2/10/1961 | Childrens | 8/139 | 64.52 → 64.52% | 31/31 → 31/31 | 95.24% |
| 1002 / 2/10/1961 | LatinPop | 8/127 | 13.33 → 46.67% | 30/30 → 30/30 | 22.73% |
| 1002 / 2/10/1961 | Comedy | 8/206 | 44.83 → 48.28% | 29/29 → 29/29 | 96.00% |
| 1002 / 2/10/1961 | TexMex | 8/76 | 31.25 → 43.75% | 32/32 → 32/32 | 60.87% |
| 1002 / 2/10/1961 | TraditionalPop | 8/795 | 31.03 → 27.59% | 29/29 → 29/29 | 70.59% |
| 1002 / 2/10/1961 | RnB | 8/1117 | 14.29 → 8.57% | 35/35 → 35/35 | 80.77% |
| 1002 / 2/10/1961 | Jazz | 8/463 | 54.84 → 58.06% | 31/31 → 31/31 | 77.27% |
| 1002 / 2/10/1961 | Country | 8/782 | 34.48 → 37.93% | 29/29 → 29/29 | 64.00% |
| 1002 / 2/10/1961 | DooWop | 8/498 | 2.86 → 5.71% | 35/35 → 35/35 | 51.85% |
| 1002 / 2/10/1961 | Soul | 8/1126 | 5.88 → 8.82% | 34/34 → 34/34 | 92.31% |
| 1002 / 2/10/1961 | Folk | 8/509 | 57.58 → 66.67% | 33/33 → 33/33 | 53.57% |
| 1002 / 2/10/1961 | RockAndRoll | 8/1258 | 3.13 → 3.13% | 32/32 → 32/32 | 95.65% |
| 1002 / 2/10/1961 | Gospel | 200/200 | 47.54 → 46.78% | 793/793 → 793/793 | 72.44% |
| 1002 / 2/10/1961 | Blues | 8/356 | 30.56 → 25.00% | 36/36 → 36/36 | 66.67% |
| 1002 / 2/10/1961 | TeenPop | 8/896 | 10.00 → 23.33% | 30/30 → 30/30 | 17.39% |
| 1002 / 2/10/1961 | SurfRock | 8/53 | 0.00 → 0.00% | 30/30 → 30/30 | 100.00% |
| 1002 / 2/10/1961 | ContemporaryFolk | 4/4 | 40.00 → 46.67% | 15/15 → 15/15 | 81.82% |

Regression flags (any inherited-share drop greater than one point, or any fill drop) are intentionally conservative: a decrease can be beneficial for a genre above its range, such as RnB, and is still shown for review. They are not all confirmed target violations.

| Frame | Genre | Share change (points) | Fill change (points) |
| --- | --- | ---: | ---: |
| polar-repair-start-1001-v3 | TraditionalPop | -6.25 | 0.00 |
| polar-repair-start-1001-v3 | Blues | -5.88 | 0.00 |
| polar-repair-start-1002-v3 | LatinPop | -11.76 | 0.00 |
| polar-repair-start-1002-v3 | Comedy | -3.03 | 0.00 |
| polar-repair-start-1002-v3 | TexMex | -10.00 | 0.00 |
| polar-repair-start-1002-v3 | DooWop | -5.88 | 0.00 |
| polar-repair-later-1001 | EasyListening | -6.67 | 0.00 |
| polar-repair-later-1001 | TexMex | -6.25 | 0.00 |
| polar-repair-later-1001 | Jazz | -16.67 | 0.00 |
| polar-repair-later-1001 | Blues | -9.09 | 0.00 |
| polar-repair-later-1002 | TraditionalPop | -3.45 | 0.00 |
| polar-repair-later-1002 | RnB | -5.71 | 0.00 |
| polar-repair-later-1002 | Blues | -5.56 | 0.00 |

Genres absent from all four measured panels: ProgressiveRock, PsychedelicRock, BritishInvasion, BritishBeat, RootsRock, Motown, Psychedelic, Skiffle, Funk, BossaNova, BluesRock, BritishBlues, AcidRock, ProtoMetal, FolkRock, CountryRock, SkaRocksteady, Ska, Reggae, Rocksteady, GirlGroup, Bubblegum, PopRock, PsychedelicPop, Boogaloo, BaroquePop, HardRock, GarageRock, ProtoPunk, SunshinePop, BritishPop, SingerSongwriter.

## Acceptance measures

**Inherited share: FAIL overall.** The Gospel combined gate misses seed 1002; several other sampled cohort ranges are missed. The unchanged historical ranges remain provisional tuning targets. **Set-fill non-regression: PASS in the paired panels.** Requested/filled slots and short-set counts are preserved, including any pre-existing deficits; no new numeric fill tolerance was invented. Full-world longitudinal acceptance remains unverified. **Folk/Gospel retention: NOT PASSED longitudinally.** The paired endpoint shares below improve Gospel in seed 1001 and Folk in both samples, but no repaired 1960–1961 trajectory was run after the gate failure. There is no evidence for post-repair longitudinal acceptance.

| Frame | Target cohort | Range | Baseline share / status | A+B share / status | Requested → filled before / after |
| --- | --- | --- | --- | --- | --- |
| 1001 / 1/1/1960 | EasyListening/standardsLed | 50–85% | 51.52% / pass | 54.55% / pass | 33→33 / 33→33 |
| 1001 / 1/1/1960 | RnB/mixedContemporary | 5–25% | 6.67% / pass | 13.33% / pass | 15→15 / 15→15 |
| 1001 / 1/1/1960 | Gospel/inheritedRepertoire | 50–90% | 33.33% / fail | 60.36% / pass | 222→222 / 222→222 |
| 1001 / 1/1/1960 | Country/mixedContemporary | 20–50% | 28.57% / pass | 35.71% / pass | 14→14 / 14→14 |
| 1001 / 1/1/1960 | Folk/traditionalRevival | 50–90% | 76.47% / pass | 76.47% / pass | 17→17 / 17→17 |
| 1001 / 1/1/1960 | Jazz/standardsInterpreter | 60–90% | 30.00% / fail | 40.00% / fail | 10→10 / 10→10 |
| 1001 / 1/1/1960 | Jazz/composerLed | 10–50% | 25.00% / pass | 25.00% / pass | 8→8 / 8→8 |
| 1002 / 1/1/1960 | Gospel/inheritedRepertoire | 50–90% | 32.52% / fail | 63.80% / pass | 163→163 / 163→163 |
| 1002 / 1/1/1960 | Jazz/standardsInterpreter | 60–90% | 60.00% / pass | 60.00% / pass | 15→15 / 15→15 |
| 1002 / 1/1/1960 | Country/mixedContemporary | 20–50% | 44.44% / pass | 50.00% / pass | 18→18 / 18→18 |
| 1002 / 1/1/1960 | EasyListening/standardsLed | 50–85% | 65.63% / pass | 65.63% / pass | 32→32 / 32→32 |
| 1002 / 1/1/1960 | RnB/mixedContemporary | 5–25% | 17.65% / pass | 17.65% / pass | 17→17 / 17→17 |
| 1002 / 1/1/1960 | Folk/traditionalRevival | 50–90% | 70.59% / pass | 76.47% / pass | 17→17 / 17→17 |
| 1001 / 3/10/1961 | EasyListening/standardsLed | 50–85% | 50.00% / pass | 43.33% / fail | 30→30 / 30→30 |
| 1001 / 3/10/1961 | RnB/mixedContemporary | 5–25% | 13.33% / pass | 13.33% / pass | 30→30 / 30→30 |
| 1001 / 3/10/1961 | Gospel/inheritedRepertoire | 50–90% | 6.51% / fail | 50.41% / pass | 968→968 / 968→968 |
| 1001 / 3/10/1961 | Country/mixedContemporary | 20–50% | 38.24% / pass | 38.24% / pass | 34→34 / 34→34 |
| 1001 / 3/10/1961 | Folk/traditionalRevival | 50–90% | 64.86% / pass | 67.57% / pass | 37→37 / 37→37 |
| 1001 / 3/10/1961 | Jazz/standardsInterpreter | 60–90% | 60.71% / pass | 42.86% / fail | 28→28 / 28→28 |
| 1001 / 3/10/1961 | Jazz/composerLed | 10–50% | 25.00% / pass | 12.50% / pass | 8→8 / 8→8 |
| 1001 / 3/10/1961 | SurfRock/vocal | 0–10% | 3.03% / pass | 3.03% / pass | 33→33 / 33→33 |
| 1002 / 2/10/1961 | Gospel/inheritedRepertoire | 50–90% | 47.54% / fail | 46.78% / fail | 793→793 / 793→793 |
| 1002 / 2/10/1961 | Jazz/standardsInterpreter | 60–90% | 73.68% / pass | 78.95% / pass | 19→19 / 19→19 |
| 1002 / 2/10/1961 | Country/mixedContemporary | 20–50% | 34.48% / pass | 37.93% / pass | 29→29 / 29→29 |
| 1002 / 2/10/1961 | RnB/mixedContemporary | 5–25% | 14.29% / pass | 8.57% / pass | 35→35 / 35→35 |
| 1002 / 2/10/1961 | Folk/traditionalRevival | 50–90% | 57.58% / pass | 66.67% / pass | 33→33 / 33→33 |
| 1002 / 2/10/1961 | EasyListening/standardsLed | 50–85% | 47.06% / fail | 52.94% / pass | 34→34 / 34→34 |
| 1002 / 2/10/1961 | Jazz/composerLed | 10–50% | 25.00% / pass | 25.00% / pass | 12→12 / 12→12 |
| 1002 / 2/10/1961 | SurfRock/vocal | 0–10% | 0.00% / pass | 0.00% / pass | 30→30 / 30→30 |

The existing weekly aggregates carry market/economic totals, not per-genre repertoire slots. Retention is therefore calculated from the retained monthly slot aggregates, with signed and unsigned acts separate. The reference Gospel seed-1001 signed decline remains 604/2,536 (23.82%) in 1960 to 62/671 (9.24%) in 1961 Q3. Membership changes and unequal monthly set sizes make these descriptive ratios. The monthly results through each February/March checkpoint, plus all later quarters through September for the reference problem, are in the analysis JSON. No old aggregate was overwritten.

| Seed | Genre | Population | Period | Inherited / slots | Share |
| --- | --- | --- | --- | --- | --- |
| 1001 | Folk | signed | 1960 | 4667/8507 | 54.86% |
| 1001 | Gospel | signed | 1960 | 604/2536 | 23.82% |
| 1001 | Folk | unsigned | 1960 | 1465/2677 | 54.73% |
| 1001 | Gospel | unsigned | 1960 | 585/2289 | 25.56% |
| 1001 | Folk | unsigned | 1961-Q1 | 2583/4318 | 59.82% |
| 1001 | Gospel | unsigned | 1961-Q1 | 226/2247 | 10.06% |
| 1001 | Folk | signed | 1961-Q1 | 1287/2102 | 61.23% |
| 1001 | Gospel | signed | 1961-Q1 | 71/542 | 13.10% |
| 1001 | Folk | unsigned | 1961-Q2 | 2467/4464 | 55.26% |
| 1001 | Gospel | unsigned | 1961-Q2 | 171/2543 | 6.72% |
| 1001 | Folk | signed | 1961-Q2 | 1258/2228 | 56.46% |
| 1001 | Gospel | signed | 1961-Q2 | 54/620 | 8.71% |
| 1001 | Folk | unsigned | 1961-Q3 | 2669/4710 | 56.67% |
| 1001 | Gospel | unsigned | 1961-Q3 | 181/2646 | 6.84% |
| 1001 | Folk | signed | 1961-Q3 | 1341/2295 | 58.43% |
| 1001 | Gospel | signed | 1961-Q3 | 62/671 | 9.24% |
| 1002 | Folk | signed | 1960 | 4711/7718 | 61.04% |
| 1002 | Gospel | signed | 1960 | 457/2123 | 21.53% |
| 1002 | Folk | unsigned | 1960 | 1446/2574 | 56.18% |
| 1002 | Gospel | unsigned | 1960 | 293/1909 | 15.35% |
| 1002 | Folk | unsigned | 1961-Q1 | 2529/4269 | 59.24% |
| 1002 | Gospel | unsigned | 1961-Q1 | 809/1774 | 45.60% |
| 1002 | Folk | signed | 1961-Q1 | 1129/1836 | 61.49% |
| 1002 | Gospel | signed | 1961-Q1 | 280/585 | 47.86% |
| 1002 | Folk | unsigned | 1961-Q2 | 2750/4534 | 60.65% |
| 1002 | Gospel | unsigned | 1961-Q2 | 348/1977 | 17.60% |
| 1002 | Folk | signed | 1961-Q2 | 1190/1904 | 62.50% |
| 1002 | Gospel | signed | 1961-Q2 | 103/662 | 15.56% |
| 1002 | Folk | unsigned | 1961-Q3 | 3010/4823 | 62.41% |
| 1002 | Gospel | unsigned | 1961-Q3 | 672/2241 | 29.99% |
| 1002 | Folk | signed | 1961-Q3 | 1195/1865 | 64.08% |
| 1002 | Gospel | signed | 1961-Q3 | 198/685 | 28.91% |

### Recording shape variation: baseline then compare

Actual recording caches in the retained checkpoints are measured separately from candidate arrangements. Frozen A+B leaves those existing recordings exactly unchanged. The re-resolved candidate comparisons in the JSON show prospective arrangement variation; they are not newly completed recording trajectories. Per-axis population standard deviations and distinct full shapes are recorded for each archetype, including small archetype samples. A near-zero-spread flag is all axes below 0.00001 or fewer than two distinct shapes; singleton samples do not establish excessive rigidity. A nearest-template disagreement uses unweighted Euclidean distance across ten axes, and is a review flag because legitimate lyric/form modifiers can move a shape closer to a different template. It does not establish a violated archetype boundary. Instrumental profiles must have zero VocalPower, VocalNuance and LyricDelivery. No new shape target is imposed.

Seed 1001: 29902 cached recordings, 0 missing profiles, 590 linked covers. 274/590 (46.44%) of evaluable covers differ from the composition’s first committed recording archetype; 0 originals unavailable. Instrumental violations: 0.

Seed 1002: 26802 cached recordings, 0 missing profiles, 533 linked covers. 263/533 (49.34%) of evaluable covers differ from the composition’s first committed recording archetype; 0 originals unavailable. Instrumental violations: 0.

| Seed | Archetype | Recordings / distinct shapes | Axis SD (six demands; four identities) | Nearest-template disagreements | Rigidity review |
| --- | --- | --- | --- | --- | --- |
| 1001 | BossaSong | 892/342 | [0.1210, 0.1462, 0.0106, 0.0099, 0.0936, 0.0101, 0.0457, 0.0080, 0.0260, 0.0140] | 60/892 | spread present |
| 1001 | ReverieMoodPiece | 1572/184 | [0.1362, 0.2019, 0.0055, 0.0143, 0.0704, 0.0055, 0.0218, 0.0046, 0.0138, 0.0073] | 120/1572 | spread present |
| 1001 | LatinBolero | 63/43 | [0.1099, 0.0537, 0.0110, 0.0112, 0.0083, 0.0148, 0.0436, 0.0111, 0.0120, 0.0175] | 0/63 | spread present |
| 1001 | CharmSong | 2833/882 | [0.1116, 0.0597, 0.0110, 0.0118, 0.0194, 0.0110, 0.0561, 0.0084, 0.0260, 0.0164] | 25/2833 | spread present |
| 1001 | VerseDrivenSong | 1033/149 | [0.1089, 0.0501, 0.0056, 0.0056, 0.0887, 0.0062, 0.0306, 0.0071, 0.0178, 0.0114] | 2/1033 | spread present |
| 1001 | SlowBallad | 685/211 | [0.1121, 0.0510, 0.0093, 0.0103, 0.0130, 0.0097, 0.0473, 0.0077, 0.0253, 0.0144] | 0/685 | spread present |
| 1001 | NashvilleBallad | 6/6 | [0.0977, 0.0456, 0.0155, 0.0107, 0.0059, 0.0109, 0.0059, 0.0085, 0.0042, 0.0088] | 0/6 | spread present |
| 1001 | MidTempoPopSong | 1567/613 | [0.0993, 0.0496, 0.0117, 0.0120, 0.0201, 0.0109, 0.0349, 0.0089, 0.0252, 0.0112] | 2/1567 | spread present |
| 1001 | LushStandard | 104/18 | [0.1110, 0.0498, 0.0043, 0.0054, 0.0240, 0.0062, 0.0482, 0.0052, 0.0178, 0.0147] | 0/104 | spread present |
| 1001 | CountryWaltz | 307/203 | [0.1113, 0.0521, 0.0141, 0.0133, 0.0104, 0.0131, 0.0735, 0.0108, 0.0199, 0.0217] | 0/307 | spread present |
| 1001 | WesternSwing | 12/11 | [0.1123, 0.0537, 0.0116, 0.0185, 0.0080, 0.0128, 0.0757, 0.0116, 0.0113, 0.0182] | 0/12 | spread present |
| 1001 | ProtestMessageSong | 400/304 | [0.1115, 0.0510, 0.0142, 0.0148, 0.0796, 0.0145, 0.0245, 0.0262, 0.0092, 0.0263] | 0/400 | spread present |
| 1001 | MidTempoRocker | 1266/192 | [0.1133, 0.0590, 0.0064, 0.0058, 0.0404, 0.0060, 0.0228, 0.0046, 0.0185, 0.0061] | 14/1266 | spread present |
| 1001 | CountryShuffle | 1384/262 | [0.1094, 0.0501, 0.0075, 0.0077, 0.0041, 0.0073, 0.0293, 0.0060, 0.0225, 0.0084] | 8/1384 | spread present |
| 1001 | BrightPopNumber | 1629/225 | [0.1103, 0.0514, 0.0056, 0.0057, 0.0253, 0.0056, 0.0243, 0.0045, 0.0187, 0.0068] | 1/1629 | spread present |
| 1001 | HornDrivenSoulNumber | 149/10 | [0.1059, 0.0480, 0.0028, 0.0041, 0.0057, 0.0034, 0.0125, 0.0029, 0.0122, 0.0021] | 0/149 | spread present |
| 1001 | ShuffleTwelveBar | 10761/954 | [0.1101, 0.0504, 0.0057, 0.0110, 0.0091, 0.0058, 0.0268, 0.0044, 0.0184, 0.0078] | 1/10761 | spread present |
| 1001 | SlowBlues | 1153/84 | [0.1076, 0.0491, 0.0045, 0.0089, 0.0072, 0.0043, 0.0200, 0.0035, 0.0128, 0.0052] | 0/1153 | spread present |
| 1001 | DeepSoulPleader | 94/8 | [0.1039, 0.0482, 0.0039, 0.0042, 0.0130, 0.0026, 0.0098, 0.0012, 0.0102, 0.0031] | 0/94 | spread present |
| 1001 | GrooveRiffVamp | 758/177 | [0.1100, 0.0500, 0.0074, 0.0075, 0.0302, 0.0077, 0.0325, 0.0058, 0.0237, 0.0094] | 18/758 | spread present |
| 1001 | CountryTwoBeat | 808/175 | [0.1100, 0.0497, 0.0075, 0.0075, 0.0041, 0.0069, 0.0382, 0.0056, 0.0225, 0.0116] | 36/808 | spread present |
| 1001 | Swinger | 129/61 | [0.1405, 0.1355, 0.0098, 0.0093, 0.0772, 0.0096, 0.0263, 0.0080, 0.0239, 0.0088] | 4/129 | spread present |
| 1001 | QuietHymn | 385/193 | [0.1104, 0.0523, 0.0134, 0.0150, 0.0076, 0.0133, 0.0210, 0.0112, 0.0150, 0.0122] | 0/385 | spread present |
| 1001 | GospelQuartet | 171/54 | [0.1067, 0.0487, 0.0077, 0.0079, 0.0121, 0.0084, 0.0265, 0.0071, 0.0143, 0.0084] | 0/171 | spread present |
| 1001 | Medley | 16/12 | [0.0681, 0.0335, 0.0127, 0.0067, 0.0312, 0.0127, 0.0260, 0.0073, 0.0257, 0.0101] | 0/16 | spread present |
| 1001 | ClassicalSolo | 473/55 | [0.0000, 0.0000, 0.0146, 0.0127, 0.0000, 0.0146, 0.0115, 0.0112, 0.0100, 0.0130] | 0/473 | spread present |
| 1001 | SpokenWord | 406/78 | [0.1039, 0.0472, 0.0067, 0.0062, 0.1007, 0.0073, 0.0380, 0.0051, 0.0206, 0.0118] | 0/406 | spread present |
| 1001 | JauntyMusicHallRomp | 377/250 | [0.1020, 0.0455, 0.0128, 0.0148, 0.0083, 0.0138, 0.0770, 0.0117, 0.0110, 0.0222] | 0/377 | spread present |
| 1001 | Novelty | 162/100 | [0.0932, 0.0489, 0.0127, 0.0137, 0.0077, 0.0151, 0.0933, 0.0125, 0.0114, 0.0273] | 0/162 | spread present |
| 1001 | TexMexSong | 222/88 | [0.1066, 0.0518, 0.0111, 0.0100, 0.0212, 0.0101, 0.0235, 0.0259, 0.0209, 0.0258] | 0/222 | spread present |
| 1001 | LatinDance | 17/4 | [0.0034, 0.0073, 0.0078, 0.0025, 0.0265, 0.0043, 0.0140, 0.0196, 0.0179, 0.0241] | 2/17 | spread present |
| 1001 | ModernJazzInstrumental | 8/8 | [0.0000, 0.0000, 0.0123, 0.0125, 0.0000, 0.0099, 0.0081, 0.0107, 0.0143, 0.0109] | 0/8 | spread present |
| 1001 | SingAlongChant | 40/25 | [0.0886, 0.0409, 0.0108, 0.0093, 0.0443, 0.0110, 0.0288, 0.0081, 0.0254, 0.0084] | 0/40 | spread present |
| 1001 | DanceNumber | 11/10 | [0.1050, 0.0472, 0.0128, 0.0247, 0.0481, 0.0103, 0.0594, 0.0234, 0.0232, 0.0267] | 0/11 | spread present |
| 1001 | SaloonBallad | 8/8 | [0.1176, 0.4765, 0.0086, 0.0173, 0.2739, 0.0161, 0.0287, 0.0097, 0.0215, 0.0110] | 3/8 | spread present |
| 1001 | SpiritualShout | 1/1 | [0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000] | 0/1 | flag (check sample size) |
| 1002 | ReverieMoodPiece | 1339/196 | [0.1427, 0.2194, 0.0063, 0.0162, 0.0777, 0.0062, 0.0317, 0.0054, 0.0163, 0.0106] | 125/1339 | spread present |
| 1002 | BossaSong | 1127/452 | [0.1178, 0.1474, 0.0107, 0.0103, 0.0935, 0.0109, 0.0533, 0.0082, 0.0258, 0.0165] | 76/1127 | spread present |
| 1002 | Swinger | 61/40 | [0.0974, 0.0454, 0.0121, 0.0114, 0.0130, 0.0115, 0.0275, 0.0094, 0.0248, 0.0091] | 0/61 | spread present |
| 1002 | VerseDrivenSong | 1005/225 | [0.1100, 0.0504, 0.0077, 0.0079, 0.1000, 0.0077, 0.0278, 0.0121, 0.0205, 0.0147] | 3/1005 | spread present |
| 1002 | ProtestMessageSong | 282/216 | [0.1075, 0.0495, 0.0136, 0.0144, 0.0786, 0.0135, 0.0217, 0.0195, 0.0144, 0.0197] | 0/282 | spread present |
| 1002 | SlowBallad | 636/222 | [0.1095, 0.0509, 0.0095, 0.0100, 0.0128, 0.0106, 0.0561, 0.0074, 0.0253, 0.0174] | 0/636 | spread present |
| 1002 | WesternSwing | 25/25 | [0.1093, 0.0532, 0.0132, 0.0127, 0.0080, 0.0142, 0.0114, 0.0117, 0.0095, 0.0094] | 0/25 | spread present |
| 1002 | CountryWaltz | 249/170 | [0.1127, 0.0524, 0.0131, 0.0130, 0.0090, 0.0138, 0.0617, 0.0108, 0.0162, 0.0194] | 0/249 | spread present |
| 1002 | CharmSong | 2445/792 | [0.1129, 0.0747, 0.0106, 0.0112, 0.0318, 0.0104, 0.0467, 0.0083, 0.0261, 0.0140] | 21/2445 | spread present |
| 1002 | MidTempoRocker | 1156/160 | [0.1210, 0.0632, 0.0057, 0.0058, 0.0454, 0.0055, 0.0198, 0.0047, 0.0183, 0.0051] | 16/1156 | spread present |
| 1002 | CountryShuffle | 1222/237 | [0.1068, 0.0487, 0.0073, 0.0075, 0.0041, 0.0073, 0.0305, 0.0060, 0.0220, 0.0086] | 11/1222 | spread present |
| 1002 | LatinBolero | 38/31 | [0.1061, 0.0525, 0.0133, 0.0160, 0.0069, 0.0138, 0.0329, 0.0108, 0.0127, 0.0112] | 0/38 | spread present |
| 1002 | MidTempoPopSong | 1469/549 | [0.0846, 0.0422, 0.0112, 0.0116, 0.0171, 0.0114, 0.0362, 0.0087, 0.0253, 0.0112] | 1/1469 | spread present |
| 1002 | ShuffleTwelveBar | 9504/844 | [0.1100, 0.0501, 0.0055, 0.0107, 0.0085, 0.0056, 0.0240, 0.0046, 0.0181, 0.0067] | 2/9504 | spread present |
| 1002 | HornDrivenSoulNumber | 159/10 | [0.1088, 0.0494, 0.0024, 0.0033, 0.0054, 0.0031, 0.0087, 0.0020, 0.0101, 0.0026] | 0/159 | spread present |
| 1002 | SlowBlues | 676/50 | [0.1046, 0.0479, 0.0041, 0.0131, 0.0067, 0.0046, 0.0229, 0.0033, 0.0145, 0.0069] | 0/676 | spread present |
| 1002 | GrooveRiffVamp | 823/165 | [0.1095, 0.0503, 0.0077, 0.0066, 0.0287, 0.0068, 0.0239, 0.0056, 0.0221, 0.0063] | 5/823 | spread present |
| 1002 | BrightPopNumber | 1715/238 | [0.1108, 0.0521, 0.0056, 0.0054, 0.0267, 0.0054, 0.0226, 0.0046, 0.0195, 0.0060] | 2/1715 | spread present |
| 1002 | GospelQuartet | 141/26 | [0.1099, 0.0506, 0.0054, 0.0058, 0.0097, 0.0062, 0.0123, 0.0049, 0.0124, 0.0053] | 0/141 | spread present |
| 1002 | QuietHymn | 396/203 | [0.1108, 0.0516, 0.0136, 0.0145, 0.0078, 0.0139, 0.0187, 0.0118, 0.0140, 0.0121] | 0/396 | spread present |
| 1002 | CountryTwoBeat | 653/155 | [0.1082, 0.0508, 0.0072, 0.0069, 0.0161, 0.0073, 0.0383, 0.0057, 0.0230, 0.0107] | 27/653 | spread present |
| 1002 | JauntyMusicHallRomp | 413/228 | [0.1067, 0.0507, 0.0133, 0.0146, 0.0080, 0.0138, 0.0594, 0.0119, 0.0114, 0.0192] | 0/413 | spread present |
| 1002 | Medley | 14/8 | [0.0100, 0.0112, 0.0077, 0.0100, 0.0366, 0.0091, 0.0277, 0.0100, 0.0240, 0.0072] | 0/14 | spread present |
| 1002 | DeepSoulPleader | 84/4 | [0.1071, 0.0481, 0.0023, 0.0023, 0.0071, 0.0009, 0.0083, 0.0017, 0.0076, 0.0023] | 0/84 | spread present |
| 1002 | SpokenWord | 360/54 | [0.1058, 0.0486, 0.0066, 0.0058, 0.0944, 0.0063, 0.0184, 0.0050, 0.0181, 0.0049] | 0/360 | spread present |
| 1002 | NashvilleBallad | 11/9 | [0.1013, 0.0477, 0.0115, 0.0097, 0.0056, 0.0139, 0.0701, 0.0085, 0.0072, 0.0191] | 0/11 | spread present |
| 1002 | ClassicalSolo | 424/59 | [0.0000, 0.0000, 0.0136, 0.0129, 0.0000, 0.0148, 0.0115, 0.0111, 0.0100, 0.0128] | 0/424 | spread present |
| 1002 | DanceNumber | 53/25 | [0.1093, 0.0493, 0.0104, 0.0234, 0.0554, 0.0091, 0.0456, 0.0199, 0.0237, 0.0186] | 1/53 | spread present |
| 1002 | SingAlongChant | 44/15 | [0.1048, 0.0494, 0.0078, 0.0084, 0.0423, 0.0080, 0.0240, 0.0064, 0.0232, 0.0065] | 0/44 | spread present |
| 1002 | SpiritualShout | 7/1 | [0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000] | 0/7 | flag (check sample size) |
| 1002 | TexMexSong | 103/51 | [0.1042, 0.0490, 0.0119, 0.0089, 0.0205, 0.0106, 0.0200, 0.0260, 0.0204, 0.0265] | 0/103 | spread present |
| 1002 | SaloonBallad | 10/7 | [0.1865, 0.4732, 0.0104, 0.0129, 0.2853, 0.0128, 0.0297, 0.0105, 0.0214, 0.0098] | 5/10 | spread present |
| 1002 | Novelty | 117/68 | [0.1041, 0.0473, 0.0128, 0.0139, 0.0078, 0.0147, 0.0619, 0.0129, 0.0127, 0.0177] | 0/117 | spread present |
| 1002 | LushStandard | 33/6 | [0.1986, 0.2698, 0.0045, 0.0032, 0.1579, 0.0035, 0.0101, 0.0024, 0.0110, 0.0035] | 3/33 | spread present |
| 1002 | ModernJazzInstrumental | 6/6 | [0.0000, 0.0000, 0.0126, 0.0154, 0.0000, 0.0167, 0.0087, 0.0099, 0.0113, 0.0071] | 0/6 | spread present |
| 1002 | StomperRocker | 1/1 | [0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000] | 0/1 | flag (check sample size) |
| 1002 | ClassicalChamber | 1/1 | [0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000, 0.0000] | 0/1 | flag (check sample size) |

The candidate-arrangement baseline/resolved tables, all axis standard deviations, archetype changes and nearest-template flags are in `shapes` in the analysis JSON. Easy Listening’s standards-led sample is explicitly reported above and its seed-1001 later range failure prevents claiming that the repair preserves its target.

## Infrastructure and validation

Already present before this task: periodic flush of admissions and all open event writers, atomic checkpoint writes before/completed observations and on graceful stop, bounded deterministic sampling with persistent panel, stop-file support, cooperative research time budgets, and fixed-world probes. Missing: census-off as default. Changed the runner and all three census helpers from sample to none; sampling remains opt-in. No large full census was added. Admission telemetry cheaply adds an optional month when the current simulation date agrees with the event year. Retrospective seeded origins retain a blank month rather than inventing an event date. This does not change simulation state or RNG.

The 15-second infrastructure smoke stops safely with a final checkpoint (startup/current boundary may overshoot the cooperative limit). It reports census mode none. The stopped checkpoint passes exact capture/load verification and advances one week in the bounded resume smoke. Existing infrastructure reports document sampled-census and forced-kill recovery; those expensive checks were not repeated. Build: zero errors, four existing warnings, no new warnings. Existing fit and behavior suites pass; the repair suite covers all centers, divergent prior rejection, shrinkage, week lag, performer offsets, distinct-chart counts and persistence.

CSV shape, actor/pool hashes, selected cover uniqueness and provenance counts, fixed originals/cover requests, score-gap decomposition sums, exact cached baseline agreement with production order, unchanged world capture hashes versus the retained frames, and byte-exact retained Gospel candidate/variant-selection lines all pass. Active archetypes, lyric/vocal modifiers, identity weights and every pre-existing tuning number match the frozen legacy fixture. The preserved original probe source was not altered.

## Failed or deferred work

Package C’s failed gate stops Package D. Composition context vocabulary/evidence fields, Gospel Standard sacred fixture, strict sacred fixture diagnostic and its persistence/cover tests are not implemented. All 112,978 Gospel candidate instances across the retained frames still have unknown explicit context. No retrospective tagging, genre quota, suitability-band tuning, single-Gospel fallback restoration, moment shutdown, performance-role or repertoire-orientation change was made. No fresh repaired multi-month trajectory, population-wide cross-genre census, or hold-out validation was run. The four-acts-per-status cross-genre panel is a bounded regression screen and does not fulfill full-population acceptance. Longitudinal before/after repaired retention remains untested.

Source checkpoint manifests for infrastructure, A, B and C are in `SimTools/PolarGospelRepairCheckpoint-*.json`; these capture the final validated package boundaries and hashes, not intermediate Git commits. No commit, push, merge or export was made. Pre-existing user modifications remain in place. Early sandbox Godot startup crashes and the first cross-genre probe attempts that omitted Rock’s distinct selector are excluded from all evidence; corrected v3 starts and the accepted later prefixes are used. The final count-floor correction affects versioned/loaded future observations, not the legacy-count fixed frames, and is covered by the final repair test.

## Reproduction and outputs

Run from `C:\Project\Label-Man`. Godot helpers require the configured installed Mono Godot executable and .NET SDK. The probe uses a frozen legacy world to reproduce prior production output, then compares repaired variants without committing changes to that world. Use fresh run names; helpers refuse to overwrite artifacts.

~~~powershell
dotnet build "Label Man.csproj" --no-restore
.\SimTools\run-polar-gospel-repair.ps1 -Seed 1001 -Run polar-repair-start-1001-v3 -MaxSeconds 120
.\SimTools\run-polar-gospel-repair.ps1 -Seed 1002 -Run polar-repair-start-1002-v3 -MaxSeconds 120
.\SimTools\run-polar-gospel-repair.ps1 -Seed 1001 -Run polar-repair-later-1001 -Snapshot "SimLogs/polar-gospel-routes-1001-polar-checkpoint.json.gz" -MaxSeconds 120
.\SimTools\run-polar-gospel-repair.ps1 -Seed 1002 -Run polar-repair-later-1002 -Snapshot "SimLogs/polar-gospel-routes-1002-polar-checkpoint.json.gz" -MaxSeconds 120
node --max-old-space-size=2048 SimTools/analyze-polar-gospel-repair.mjs
node SimTools/write-polar-gospel-repair-report.mjs
~~~

The listed prefixes are already occupied by retained results. Reanalyze them directly, or substitute fresh prefixes in both the helper invocations and `frameSpecs` in the analyzer for a new reproduction. Repair checks run through `SaveLoadRoundTripRunner.tscn -- --seed=1001 --polar-gospel-repair-check`; existing checks use `--polar-song-fit-check` and `--polar-song-behavior-check`. Infrastructure smoke/resume invocations and elapsed times are retained in their `*-invocation.json` files.

Main outputs: `SimLogs/polar-gospel-repair-analysis.json`, `SimTools/PolarGenrePriorAudit.json`, `SimTools/PolarGospelRepairValidation.json`, this report and the four package checkpoints. Each accepted prefix has `-gospel-actors.csv`, `-gospel-candidates.csv`, `-gospel-selections.csv`, `-repair-fits.csv`, `-gospel-summary.json`, `-invocation.json` and `.log`. Hashes are in the validation JSON. Source checkpoints contain package-specific hashes.

**Not ready for hold-out validation.** The remaining blockers are seed-1002 Gospel’s combined share, both later re-resolved shares, the cross-genre panel regressions and missing longitudinal acceptance. Alice’s hold-out approval has not been requested or assumed; hold-out seeds remain untouched.
