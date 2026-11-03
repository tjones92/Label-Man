# Fresh professional material access — 2026-11-02

The evidence supports investigating a demand/access bottleneck before increasing
writer supply or imposing a professional-song quota. The system has no general
rule that a weak writer with a worthwhile release opportunity should seek a
competitive new song. Several existing safeguards can instead let covers satisfy
the act's material need. This is a mechanism diagnosis, not a measured causal
effect or a completed census of non-writing performers.

No gameplay settings or decisions changed in this pass.

## Observations

The analyzer uses the four frozen-assembly, 104-week `opportunity` observer runs
(control/combined, seeds 1001/2002), whose outcome reproduction is documented in
ProfessionalWritingOpportunityAudit.md. Every single intention is counted once,
with 26 complete weeks after proposal; later events are excluded. Career is the
plan's recorded evidence, not a permanent artist cohort. Genre tables contain only
issued plans and use launch genre. Counts below pool the two combined runs.

| Plan career | Mature plans | Search observed | No worthwhile offer / declined | Professional search started | Cover search started |
| --- | ---: | ---: | ---: | ---: | ---: |
| NewSigning | 10,264 | 9,808 | 5,143 | 142 | 4,523 |
| Established | 842 | 744 | 257 | 63 | 424 |

Thus 52.4% of observed new-signing searches end at the declined observation,
versus 34.5% for established plans. Only 1.45% of new-signing searches start a
professional search, versus 8.47% for established plans. These are existing-song
searches, including paid deliveries already owned; they are NOT new commission
orders or the complete route to professional releases. Shortlist selection and
commissions are separate paths. Search events are deduplicated by plan and state.

In the controls the corresponding new-signing counts are 9,699 searches, 4,839
declined, 475 professional starts and 4,385 cover starts. Combined policies reduce
professional search starts but improve actual professional single use (496 versus
424 new-signing issued plans). This illustrates why search volume alone cannot
measure total access; delivery routing substitutes for some searches.

| Issued-plan genre | Issued plans | Professional songs | Share | Professional mean record hook | Recent-cover mean record hook | Artist-written mean record hook |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| TraditionalPop | 1,082 | 82 | 7.6% | .747 | .698 | .498 |
| Soul | 642 | 87 | 13.6% | .733 | .523 | .437 |
| RockAndRoll | 1,680 | 63 | 3.8% | .692 | .531 | .448 |
| Country | 1,035 | 84 | 8.1% | .773 | .648 | .500 |
| DooWop | 506 | 69 | 13.6% | .722 | .600 | .432 |
| EasyListening | 572 | 75 | 13.1% | .681 | .618 | .422 |

Professional material is uncommon within these genres, so the aggregate result
cannot be explained entirely by mixing pop with traditions favoring inherited
repertoire. Released professional songs have competitive hooks in these samples.
However, this is selected released material: it does not show that an unreleased
demo would outperform a particular act's cover, nor establish net profit or chart
conversion. Artist mix and careers differ even within genre. These groups are NOT
non-writer groups. No historical target percentages are inferred here.

## Where the code can restrict access

1. **Commission demand uses shortlist quantity.** In `AiRepertoireService.Create`,
   an unsuccessful professional arrival can request a commission only with fewer
   than two active candidates. Covers or weak originals can satisfy this count;
   it does not compare a new commission's expected value against those songs.
   Repeat business is an exception through `RunReturningClients`, but requires
   a prior hit in the writer/act partnership. This is a route to persistence for
   successful relationships rather than a general weak-writer service.

2. **Ability does not directly redirect professional arrivals.** `ArrivalRate`
   uses genre source shares and label scouting for professional opportunities.
   Artist writing craft affects the original-arrival rate, but there is no direct
   corresponding boost to professional arrivals for a weak writer. The optional
   self-writing craft gate is absent from these run manifests. Its existence is
   not evidence that it ran. The legacy builder's `artistNeedsSong` term also does
   not establish such a boost: polar selection replaces its candidate score with
   source mix multiplied by fit.

3. **Commission cash requirements are deliberately stricter than the invoice.**
   `TryAiCommission` requires uncommitted cash covering eight commission fees;
   only one fee is charged. An act can have a planned single and enough cash for
   the invoice yet fail this guard. The trace does not count these refusals, so
   their frequency remains unknown. This guard was introduced to reduce waste;
   removing it wholesale is not supported by this investigation.

4. **Primary-genre pools and bounded sampling restrict discovery.** Professional
   repertoire uses an exact primary-genre pool. Standards/recent-hit repertoire
   in `AiRepertoireService.PoolFor` has a family fallback when its genre pool is
   empty; professional repertoire does not. Programme searches sample at most
   four professional pool entries before checking access. A fixed intention uses
   the same draws on later reviews; rejected draws are not replaced with other
   accessible songs. Consequently world stock is not effective act-level supply.

5. **The programme search charges for incremental benefit, not absolute quality.**
   It compares perceived expected recording quality with the default session
   expectation, converts improvement to money, subtracts the license fee, and
   chooses the largest positive net proposal. A free cover can win rationally.
   Rights, cash, delivery timing and pending-search capacity can then prevent a
   start. A declined observation does not identify which of these mechanisms,
   sampling, fit or lack of incremental quality was responsible.

6. **Paying and fit assessment occur at different steps.** Repertoire arrivals
   can license a professional song before `MakeRecordable` performs its fit
   refusal. Paid songs can therefore still be refused or passed over. The
   combined delivery policy fixes one ingestion gap but preserves those judgments.

Relevant code: Systems/ReleaseJudgement/AiRepertoireService.cs,
Systems/ProfessionalWritingMarket.cs, Systems/CompetitorManager.MaterialSearch.cs,
Systems/CompositionCatalogService.cs, Systems/SongMaterialSelectionService.cs.

## What is needed to answer the non-writer question directly

Existing traces do not save artist songwriting ability/member creativity or every
rejected offer. Do not infer inability from zero artist-written releases: that
would define the cohort using the selection outcome being investigated. The
effective writer craft is `.6 * songwritingAbility + .4 * best member creativity`;
both should be observed at the decision date. A low-ability band can still have a
creative member. Team/cowriting opportunities also require separate attribution.

The next useful observer should record, per impending single and act:

- Raw writing ability, effective craft, performance capability, genre, career and
  label; compare low/middle/high craft with sensitivity to the chosen thresholds.
- Professional attempts and pool counts before genre, rights, priority, cash,
  shortlist-duplicate and fit filters; count failed sampling separately.
- Every sampled offer's fee, perceived read, expected recording quality and best
  competing cover/original; retain deterministic reads without extra RNG draws.
- Commission eligibility reasons: candidate count, existing delivery, pending or
  recent order, cash reserve, supplier capacity and feasible linked plan.
- Actual preparation, issuance, unused paid work and complete 13-week chart
  outcomes. Compare within genre/career/tier and include eligible acts without
  proposals, rather than measuring only issued records.

Verify that observer output leaves existing outcomes byte-identical before using
it. Then isolate the dominant restriction in a controlled screen. The leading
candidate is replacing the quantity-only commission trigger with an imperfect
read of the value of better material for a real single opportunity. Do not change
prices, writer population, ability distributions or several access guards at once.

## Reproduction and checks

Run `analyze_fresh_material_access.py opportunity-control-market-1001
opportunity-combined-market-1001 opportunity-control-market-2002
opportunity-combined-market-2002 --output
SimLogs/writing-calibration/fresh-material-access.json` with Python.

The script rejects mixed assembly hashes, incomplete chart endpoints and missing
issued provenance. Every career group's mature-plan count, issued count and full
source distribution was checked against the existing opportunity-audit JSON and
matches exactly across all four runs. No new simulation or intervention was run.
