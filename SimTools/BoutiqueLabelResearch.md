# Boutique labels: 1960s business model, survival and founder compensation

October 22, 2026. Research and experimental implementation following
`DemandAttentionBoutiqueClosures.md`. The user defines a Boutique as a small
label that deliberately concentrates on one or two genres and generally chooses
to remain small. Growth is possible; smallness is not evidence of failure.

## Recommendation and confidence

Use **15–30% five-year operating closures**, with **20–25% as a central planning
range**, for established, independently operated specialist Boutiques. Review
bankruptcy separately, provisionally expecting about 10–20%; voluntary exits may
account for another roughly 5–10 points. These intervals describe an educated
design prior, **not an estimated historical population rate**, and are not quotas.
Do not tune each seed, each genre, or each five-year birth cohort into them.

Confidence in that numeric range is **low**. Confidence in the underlying business
model is **moderate to high**: the sources repeatedly describe lean, curated,
catalog-led businesses; some persisted for decades, some expanded or sold, and
some failed. They do not supply a representative census of active 1960 labels
with independently verified 1965 operating status. Famous survivors are easier
to find than obscure failures. A case-history tally would disguise that bias.

The range deliberately allows substantial risk while rejecting an assumption
that specialist labels must regularly produce national pop hits or become larger
companies to survive. It is a judgment about the intended game population,
informed by the histories below; the exact endpoints are not found in a source.
New, undercapitalized ventures need separate birth-cohort reporting rather than
automatic application of an established-firm survival expectation.

The metric counts the end of active label operations, retaining the existing
Bankruptcy/Dormancy distinction. **An acquisition that continues production is
not an operating closure.** A catalog sale after production has stopped does
not undo that closure. A quiet catalog business still collecting sustainable
income is not automatically dormant. Later reissue activity under a revived
name does not prove continuous operation of the original firm.

## What the histories establish

| Business | Evidence from the source | Implication for Label Man |
|---|---|---|
| Folkways | Incorporated in 1948; a tiny staff issued 2,168 albums through Asch's death in 1986. Its commitment was documentary breadth and keeping its catalog available. [Smithsonian history](https://folkways.si.edu/mission-and-history) | Incorporation does not imply a corporate office or a salaried executive team. Catalog depth and a small current roster can coexist. |
| Folkways' finances | Asch economized on production/presentation, kept low-selling titles in print, and charged for a carefully documented product. His earlier Asch and Disc businesses had failed. [Smithsonian archive](https://folklife.si.edu/archives-and-resources/moses-and-frances-asch-collection/smithsonian) | Founder commitment is not immortality; costs and catalog economics matter. Do not infer zero failure from Folkways' longevity. |
| Delmark | Koester combined record retailing and a label; the label's 1960s work centered on Chicago jazz and blues. [University of Chicago archive](https://www.lib.uchicago.edu/e/scrc/findingaids/view.php?eadid=ICU.SPCL.KOESTER) | A label may be one part of an owner's musical business, sharing premises and work rather than supporting a separate executive livelihood. |
| Delmark, founder account | Koester describes selling records from his dorm, operating a small shop and moving into recording. [Koester interview](https://www.jazzweekly.com/interviews/delmark.htm) | Retail and collecting are actual economic activities; they should not become an unexplained recurring cash grant in the label ledger. |
| Arhoolie | Smithsonian describes a 1960 roots-music business and later publishing income that helped finance premises and a record store. [Smithsonian Arhoolie history](https://folkways.si.edu/arhoolie) | Publishing and related operations can support a niche label. The game already has publishing; do not add a second generic subsidy for the same revenue. |
| Arhoolie, founder income | A source-based biographical history describes Strachwitz leaving teaching in 1962 and supporting himself through record resale and the new label. [Immigrant Entrepreneurship](https://www.immigrantentrepreneurship.org/entries/chris-strachwitz/) | Outside work and modest owner income are plausible, but this is not a nationwide salary schedule. This source is scholarly secondary evidence, distinct from a founder's payroll record. |
| Folk-Legacy | Founded in 1961 to preserve and circulate traditional music; recordings were made in homes, at gatherings and concerts, as well as in its studio. [Smithsonian collection](https://folkways.si.edu/folk-legacy) | A small-scale specialist need not run a regular commercial studio/office operation. It is a new 1960s venture, not a starting-1960 survivor. |
| Nonesuch/Elektra | Nonesuch began in 1964 with licensed European classical recordings rather than newly commissioned sessions. [Label's history](https://www.nonesuch.com/about) | Specialization has distinct catalog and cost strategies; every release need not have the same new-session economics. Nonesuch is a related imprint, not a second independent founder for survival counts. |
| Elektra, growth | Holzman recounts Nonesuch helping finance Elektra's rock recording. [Founder interview, Fresh Air](https://freshairarchive.org/segments/elektra-records-founder-jac-holzman) | A specialist can choose a commercial expansion. Neither roster capacity nor genre identity should impose permanent smallness. |
| Prestige | The owner/catalog steward describes jazz development across the 1950s–60s and a sale to Fantasy in the early 1970s. [Concord history](https://concord.com/news/craft-recordings-commemorates-70-years-of-prestige-records-with-new-titles-deluxe-reissues-and-exclusive-digital-content/) | Sale and survival differ; continuing identity can outlast independent ownership. |
| Blue Note | The present owner's history describes Liberty's purchase in May 1966, with Lion and Wolff contracted to continue running it. [UMG/uDiscover history](https://www.udiscovermusic.com/in-depth-features/blue-note-finest-jazz-since-1939/) | An acquisition is not necessarily failure. Other retrospectives give 1965; do not put an uncertain sale date into a precise five-year mortality count. |
| Riverside | The NEA describes Grauer's death at the end of 1963 and bankruptcy several months later. [NEA Keepnews biography](https://www.arts.gov/honors/jazz/orrin-keepnews) | Specialist labels could fail. Founder/key-person risk and finances can matter despite an important artistic catalog. This is not evidence that Riverside failed because of the game's simulated tax event. |

These are business-model cases, not a matched sample for a closure fraction.
The useful distinction is a curated independent production business, a growing
commercial house, a parent-funded imprint, and an acquired-but-operating label.
Counting all four as either surviving or failing “independents” produces a
different statistic from operating closures.

## Definition, specialization and growth

The user's interpretation is a sound **design definition**, not a claim that
1960s trade directories contained a uniform legal category called Boutique.
Implement it as a chosen operating strategy: selective A&R, a focused musical
identity, modest commitments, slow/optional roster growth and a catalog that can
earn without new Top 40 singles. A small pop entrepreneur chasing expansion is
not automatically the same business merely because current revenue is low.

Label Man already has separate `LabelTier` and `LabelArchetype` concepts, but
they only partly express that distinction. Its Boutique tier contains everything
from FolkBoutique/JazzPrestige to RockRebel, SoulFactory and CorporateGiant
archetypes. The 35.7% aggregate is consequently not a pure specialist-label rate.
Current promotion logic blocks the five niche archetypes from Boutique promotion
entirely while allowing other archetypes to pivot on commercial evidence. The
preference to stay small is appropriate; an absolute lifetime ban is stronger
than the historical evidence. A later strategy choice should permit an earned
expansion, rather than promoting every successful specialist or scripting named
firms' real careers. That change is kept separate from the compensation screen.

For present measurement, the analyzer exposes an explicit **specialist proxy**:
starting Boutique plus FolkBoutique, JazzPrestige, BluesRoots, GospelPowerhouse
or CountrySpecialist archetype. This is a reproducible approximation to intent,
not a permanent classification by genre. Compare both this group and the entire
starting Boutique tier; retain each seed and bankruptcy/dormancy/acquisition counts.

The pre-change founder worlds have 12/46, 16/68 and 23/53 closures in that proxy
group: 26.1%, 23.5% and 43.4%. The pooled rate is 51/167 (30.5%), with a three-seed
mean near 31.0%. FolkBoutique and JazzPrestige alone are 14/83 (16.9%). Those
differences rule out reading 35.7% as universal specialist mortality or promising
that one compensation change will correct every niche's economics.

## Is an 80% salary share intrinsically wrong?

No. A tiny operation with cheap premises and little equipment can have labor
dominate its **fixed** costs. That denominator excludes sessions, campaigns,
pressing and other variable costs. The earlier 80% finding concerned cumulative
fixed spending of **bankrupt simulated Boutiques**, not all Boutique spending,
not salary's share of total expenses, and not an observed historical industry mean.

The problem is making the founder's wage a mandatory cash bill based mainly on
incorporation. In the game, no-employee incorporated founders can pay about
$650–750 a month even while receipts are negligible. For broad context only,
the Census reports median US **family income** of $5,600 in 1960; that is not
an individual executive wage or an employer-cost comparison. It cannot set a
label founder's salary, but it underscores why a managerial wage proxy is not
a harmless default for a marginal hobby/part-time venture. [Census 1960 income](https://www.census.gov/library/publications/1962/demo/p60-037.html)

The sources support contingent owner income and mixed work arrangements. They
do not establish that founders worked free forever, that all Boutiques were
unincorporated, or that any particular monthly salary or salary-to-rent ratio
was typical. No actual employment wage already owed is forgiven by the probe.

The founder testimony is particularly helpful here. Koester explicitly says he
did not treat the records themselves as his principal way to make a living;
his discussion of later licensing and property income should not be projected
backward as a typical 1960 label budget. Holzman describes starting with the
aim of making enough for another recording and only considering larger ambitions
after ten years. These accounts support a flexible owner-income model without
supplying a numerical owner-wage calibration. [Koester](https://www.jazzweekly.com/interviews/delmark.htm),
[Holzman](https://freshairarchive.org/segments/elektra-records-founder-jac-holzman)

## Implemented experimental compensation policy

`--founder-surplus-compensation` requires the existing corporate staff/premises
model and `--home-operated-micro-labels`. It changes **AI-only, no-paid-employee,
independently owned incorporated founders** in Small/Boutique/Independent tiers.
Sole operators retain their existing earnings-draw policy; subsidiaries, players,
paid-staff businesses and larger firms retain their ordinary wage treatment.

- Preserve the existing founder work-equivalent and nominal wage quote as a
  **payment ceiling**. Founder time is still supplied once across functions;
  no extra organizational capacity is created.
- Remove that founder quote from mandatory monthly overhead and its reserve
  calculations while the operation is eligible. Keep real premises/lease costs,
  the home allowance, employee wages, plant obligations and tax obligations.
- At monthly overhead payment, pay at most **half the collected operating cash
  surplus**, capped at the nominal work quote and cash above the existing
  protected reserve. Half is an explicit scenario coefficient, not measured
  history; it retains cash for reinvestment rather than extracting every dollar.
- Exclude loans and incoming owner dividends from earned surplus. Uncollected
  sales cannot fund pay. Capital purchases still reduce available cash. Tax,
  plant, commercial and ordinary project/runway reserves remain protected.
- Book actual payment once as `OwnerSalary`, with the existing expense/tax
  treatment and company cash reconciliation. Later dividends still come from
  retained after-tax earnings under the ordinary distribution rules; salary is
  not counted as retained earnings a second time.
- Foregone discretionary compensation creates no wage arrears. The probe does
  **not** reinterpret a promised employee salary as optional, inject outside
  cash, or simulate the founder's household. It shares the existing sole-operator
  boundary: label survival does not prove that the founder earns a viable living.
  Actual owner salary and dividends must remain visible in the assessment.

There are no new random draws or save fields. Existing nominal quotes, cash
journals, monthly close guards and statements persist. Resume this experimental
world with the switch enabled. Its operating exits still follow the existing
bankruptcy and separately observed dormancy rules. The receivables/working-capital
problem is deliberately not changed in the same experiment.

This is a bounded correction to the founder-income assumption, not a historical
tax/payroll implementation or final adoption of the whole demand bundle.

## Screen and evidence

`RunBoutiqueCompensationAudit.ps1` runs native mechanism/accounting/lifecycle
checks, a 13-week save/load round trip, a 265-week switch-off replay on seed 1001,
then the compensation probe on 1001/2002/3003. Its matched controls are the
existing `dfs-v1-founder` worlds. All other bundle flags are fixed. Output and
assembly hashes are recorded under `SimLogs/bcs-v1/` and group `runs.json` files.

The switch-off replay must match the existing control byte-for-byte on finance,
records and release-strategy streams; compare all common CSVs as well. The
five-year assessment must report total and specialist-proxy Boutique closures,
causes, other starting tiers, actual founder income, unit shares, total single
units, LP share, genre/slot balance and annual chart breadth/dynamics. Release
quality strata must remain visible; no closure quota is introduced.

No historical closure-rate claim follows from a successful screen.

## Screen results (`bcs-v1`, 2026-10-22)

All native checks, the 13-week round trip and the switch-off replay passed; the
replay is byte-identical to `dfs-v1-founder` in all 95 CSV streams. The three
compensation worlds completed 265 weeks with exit 0 and empty error logs, and
every monthly cash bridge reconciles. Tables come from
`analyze_boutique_closures.py` (into `SimLogs/bcs-v1/closures/`) and
`analyze_boutique_compensation.py` (`SimLogs/bcs-v1/screen.json`), the latter
run from the repository root. Paired differences are pay − control, ± SE across
three seeds.

| Starting Boutiques, closed in 5 years | 1001 | 2002 | 3003 | Pooled |
|---|---|---|---|---|
| Control, whole tier | 48/128 (37.5%) | 48/146 (32.9%) | 46/125 (36.8%) | 142/399 (35.6%) |
| Compensation, whole tier | 25/128 (19.5%) | 30/146 (20.5%) | 27/125 (21.6%) | 82/399 (20.6%) |
| Control, specialist proxy | 12/46 (26.1%) | 16/68 (23.5%) | 23/53 (43.4%) | 51/167 (30.5%) |
| Compensation, specialist proxy | 9/46 (19.6%) | 9/68 (13.2%) | 10/53 (18.9%) | 28/167 (16.8%) |

Tier closures fall −15.2 ± 1.6 points; bankruptcies −14.6 ± 0.8. Dormancy is
6 → 4 labels pooled. Seed 3003's specialist outlier (43%) falls to 19%, so the
three-seed spread narrows sharply. Both populations now sit inside the 15–30%
design prior and near its 20–25% centre. That is consistency with a judged prior,
not validation against a historical rate.

**Other tiers.** Small closures are unchanged (−0.4 ± 1.5 points). Independent
closures fall 3.2 ± 0.9 points, as expected, since no-staff Independent founders
are eligible. MidTier (about 20 firms a seed, ineligible) closes two more firms
in 2002 and 3003 and one fewer in 1001. That is within noise for a group this
small, but it is the direction to watch alongside the Major share below.

**Founder income.** The change moves pay away from the weakest months; it does
not turn founder pay off. Survivors rise from 257 to 317 pooled, and none
of them pays zero salary over the run.

| Boutique owner income (3-seed mean) | Control | Compensation | Paired |
|---|---|---|---|
| Survivors' monthly founder salary, p10 / p25 / median / p75 (pooled) | 702 / 752 / 796 / 824 | 387 / 539 / 688 / 788 | |
| Survivors' median monthly founder salary | $793 | $692 | −101 ± 8 |
| Tier total founder salary (5 yr) | $4.70M | $4.53M | −3.7% |
| Tier net owner distributions out (5 yr) | $12.29M | $13.20M | +0.90M ± 1.08M |
| Survivors' median cash, final | $43.5k | $38.7k | −4.8k ± 2.9k |

The median survivor takes about 13% less salary, and the weakest decile about
45% less. Total founder salary barely falls, because more firms survive to pay
it. Total owner take (salary plus distributions) is slightly higher, but the
distribution difference is within its SE. Survival therefore does not prove a
viable living for the bottom quartile of founders. A founder drawing $390–540 a
month from the label is plausibly part-time, as the Delmark and Arhoolie
accounts describe, and outside income is not simulated.

**Market and chart guards.** Live single units fall 5.4M ± 1.9M (−0.7%), with
the same sign in all three seeds. Boutique unit share is flat (11.6% both arms).
Major singles share including off-chart rises 30.6 → 33.0% (+2.4 ± 1.6; per seed
−0.2, +5.3, +1.9). MidTier unit share falls 2.0 ± 1.5. Neither difference is
significant at three seeds, but both point the same way, so do not read seed
1001's flat Major share as settled. Mean uncensored chart life (9.7 weeks), 1964
LP unit share and annual Hot 100 act counts are unchanged. Annual genre-share
and year-end slot L1 shifts (10–24 share points, 16–40 slots) are reshuffle-sized:
the switch-only `dfs-v1-home` → `dfs-v1-founder` pair shows 0–22 points and 0–40
slots on seed 1001, and two different worlds show 17–26 points. Annual #1 counts
move with the same reshuffle (e.g. 3003 1960: 23 → 16; 1962: 21 → 29). The
early-launch quality strata remain in `closures/results.json`.

**What still bankrupts Boutiques: working capital.** The salary signature is
gone from the remaining failures. Bankrupt firms that would have had
non-negative six-month operating cash with founder salary added back fall from
51/136 to 17/78. Firms that never released an album fall from 84 to 18.
The remaining failures are active firms (median 13.5 launches vs 5) that die later
(median week 122 vs 81). Every bankruptcy is a negative month-end cash position
(`EconomicLabelLifecycle.ReviewMonth`: two consecutive negative month-ends, with
receivables explicitly not spendable). At exit, the median remaining bankrupt
Boutique is $3.5k overdrawn while holding **$54k of outstanding wholesale
receivables** and owing $1.5k to plants. Cash plus receivables covers plant
payables and recorded tax in 56 of 78 (72%) cases, against 46 of 136 (34%) in
the control. (`positiveCashAtEndDespiteBankruptcy` reads a post-exit finance row
and is not evidence; every exit row is negative.)

These are balance-sheet-solvent firms dying on collection timing. Whether
that is a modelling defect or a realistic indie hazard is the next question.
Slow distributor payment did hurt independents historically, but that needs
sources before the model gets credit against receivables, factoring or
distributor advances. Assess it as its own experiment rather than in this
switch.

**Status.** Promising and consistent across three seeds on its target, with a
small, non-significant shift of unit share toward Majors and a −0.7% unit cost.
Not adopted as a default. Next steps: (1) research and screen receivables
financing/collection terms (done: [WorkingCapitalResearch.md](WorkingCapitalResearch.md)
finds most remaining failures are one-hit squeezes and proposes a distress
master sale rather than credit); (2) a decade A/B with the bundle at audit end, per
the usual practice; (3) watch Major/MidTier share in that A/B.
