# Fresh material access: follow-up and historical calibration

This follow-up reuses the four frozen-assembly opportunity runs and their existing
analysis; it does not add an observer, run a simulation, or change gameplay. It
compares issued single intentions, then proposes a historical calibration range.
The supplied investigation's restrictions remain hypotheses: the traces cannot
rank their causal importance or identify non-writers.

## What the existing runs show

These are plans proposed early enough to have 26 complete weeks of follow-up,
issued within that window. Source is the mutually exclusive simulation launch
classification, not songwriter credit. Counts pool two seeds per arm.

| Workflow | Issued plans | Artist-written | Fresh professional | Standard cover | Recent-hit cover | Traditional/public domain | Covers : professional |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Control | 9,939 | 2,060 | 762 | 2,484 | 3,642 | 991 | 8.04 : 1 |
| Combined | 9,956 | 1,960 | 948 | 2,553 | 3,567 | 928 | 6.46 : 1 |

Here covers are standard plus recent-hit, excluding the separate traditional
category. In the combined arm those are 61.5% of issued plans and fresh
professional 9.5%. For new-signing plans the ratio is 5,025 : 496 = 10.13 : 1;
for established plans it is 372 : 160 = 2.33 : 1. Career labels describe the
plan at observation, not stable cohorts. The comparison is descriptive; the two
arms can select different plans and songs. These are not all records, albums,
all signed acts, or a release-weighted historical market sample. Adding
traditional to covers would give 7.43 : 1 in the combined arm, but that is a
different definition.

The ratio varies considerably by launch genre in the combined arm: Traditional
Pop 912:82 (11.12:1), Soul 268:87 (3.08:1), Rock and Roll 806:63 (12.79:1),
Country 757:84 (9.01:1), Doo Wop 239:69 (3.46:1), and Easy Listening 363:75
(4.84:1). These are not matched comparisons; differing act, label, career and
selection mixes may matter. They nevertheless show that the overall result is
not solely caused by mixing genres. High cover shares and selected professional
hooks do not prove a specific missed profitable opportunity.

## Early-1960s historical working assumption

For US-oriented, commercial single releases in approximately 1960–63, use a
**working central mix of 20% artist-written, 30% fresh external professional,
40% covers (20% standards and 20% recent recordings), and 10% traditional/public
domain**. This implies **4:3, or 1.33 covers per fresh professional song**.
Treat roughly **1:1 to 2:1** as a deliberately broad sensitivity range, not a
confidence interval or measured historical distribution. For example, 30%
covers and 30% professional gives 1:1; 45% covers and 22.5% professional gives
2:1. The remaining categories must be rebalanced to total 100%. This central
assumption is close to the pre-existing owner calibration in
`PublishingCoverSongDirective.md` (about 30% professional, 20% standards and
15–20% recent covers), not an independent historical estimate derived from a
census. The central split is a judgment chosen for testing, not a researched
statistic; do not mechanically force each genre, act, year, or run to hit it.

The evidence establishes plausible mechanisms, not the percentages. The
Library of Congress describes Goffin and King writing for the Shirelles through
Aldon Music, illustrating professional material introduced by performers:
https://www.loc.gov/static/programs/national-recording-preservation-board/documents/Tapestry.pdf.
The Country Music Hall of Fame describes Harlan Howard and Hank Cochran's
Nashville publishing work and both Cline's 1961 "I Fall to Pieces" and Guy
Mitchell's pop cover of Howard's "Heartaches by the Number":
https://countrymusichalloffame.org/hall-of-fame/harlan-howard. These examples
support fresh outside songs and covers, but cannot estimate their frequency.
The Library of Congress also describes frequent Motown cover filler on albums:
https://www.loc.gov/static/programs/national-recording-preservation-board/documents/Tonights-the-Night_Betts.pdf.
That is evidence that albums differ from singles, not a singles calibration.

A historical cover can itself be written by a professional songwriter; the game
classification instead asks whether the *recording* is a standard/recent cover
or an external professional introduction. Songwriter credit alone cannot
reconstruct the game ratio. A prior demo does not necessarily make the first
commercial issue a cover. Collaborations between performer and outside writer,
traditional adaptations, reissues and simultaneous competing versions need
explicit rules in any future historical sample. Geography, genre, date, chart
selection, singles versus album tracks and A-side versus all releases also change
the denominator. The proposed historical range is not directly validated for the
simulation's plan and genre mix.

## Mechanism and next test

Code inspection supports several distinct plausible restrictions, not their
relative magnitude. In `AiRepertoireService.Create`, an empty professional pick
can trigger a commission only when fewer than two active candidates exist; that
count includes covers and originals. The professional arrival rate is driven by
source shares and label access, while craft directly scales artist-written
arrivals. Programme search samples at most four entries per source with fixed
intention-keyed draws, filters for access, and evaluates incremental perceived
benefit net of fee. A declined trace joins multiple failure modes. Owned delivery
can be included separately from the sampled professional pool. These routes can
substitute for each other, so neither arrival nor search counts alone measure
professional access. The commission trigger also only runs after a professional
arrival fails to pick a song, so altering that threshold alone cannot address
cases where no professional arrival occurs.

The most informative next step is the observer specified in
`FreshProfessionalMaterialAccessInvestigation.md`: save effective craft and the
candidate/filter and commission-decision funnel without additional random draws;
replay against matched runs and verify outcome noninterference. Compare eligible
low-, middle- and high-craft acts within genre and career, including plans that
never issue. Then test one restriction at a time against matched controls,
including paid-unused songs, cost, release timing and chart outcomes. Use the
historical mix as an external plausibility check, not a quota or proof of a
particular causal fix. The current traces do not justify choosing a winner among
commission demand, discovery, rights, price, fit and programme valuation.

## Reproduction

Source counts above are sums of `careers.all`, `careers.NewSigning`,
`careers.Established`, and `issuedGenres` in
`SimLogs/writing-calibration/fresh-material-access.json`, produced by
`SimTools/analyze_fresh_material_access.py` for the four opportunity runs. The
original investigation documents the frozen assembly, endpoint and provenance
checks. Results are conditional on that build and those run flags, not a claim
about later changes in the working tree.
