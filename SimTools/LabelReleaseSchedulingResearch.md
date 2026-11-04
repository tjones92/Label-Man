# Label release scheduling, 1960-64

October 22, 2026. US commercial release practice; research and model design, not
an implemented calendar or a claim of a representative historical census.

## Decisions from this session

Keep both hit-act cadence and retention as useful mechanisms. The remaining
work is how a label plans its programme and allocates it among acts. A shortfall
in repeat hits is not, by itself, a reason to discard those mechanisms.

The user explicitly identifies the old one-attempt-per-label-per-week limit as
gamification and authorizes changing it. It is **not an acceptance constraint**
for the new scheduler. Capacity and allocation still need separate measurement
so their effects can be understood; that does not mean preserving the cap.
The present cadence/retention screen has studio off, and that stays true for
that screen. **Revised in review (same day):** the scheduler's capacity and
allocation screens run studio **on** as a co-primary arm, with a studio-off arm
kept for attribution. Studio adoption is still a separate decision. See
"Studio labels in this work" below for the reasons.

## Contemporary evidence

These are selected institutional cases, not population estimates. Company
announcements establish intended practice; executives' claims of higher sales
do not identify a causal treatment effect. Printed page numbers are given below.

### RCA: selectivity still meant several singles a week

Billboard's January 13, 1962 Red Seal report (p.4; PDF p.5) says RCA had reduced
its singles output in 1960 to two or three records weekly, supporting each more
intensively, and maintained its sales lead in 1961. It describes a distinct
classical LP merchandising programme. Thus selectivity did not mean a common
one-record weekly ceiling across singles and LPs.
[Original issue](https://www.worldradiohistory.com/Archive-Billboard/60s/1962/Billboard%201962-01-13.pdf).

### RCA and Columbia: coordinated programmes with discretion

Billboard, March 2, 1963, pp.4/12: RCA's A&R, sales and promotion planned
12-14 singles over six-eight weeks, months ahead, coordinating distributor
information and named release sequences. Timing could change for a fading hit
or film opportunity. The executives also described competition with LP promotion
programmes. Columbia considered, but rejected, a two-week release pause because
some records needed to appear. This supports coordinated, revisable programmes,
not a universal rule to wait for every current record to die.
[Original issue](https://www.worldradiohistory.com/Archive-Billboard/60s/1963/Billboard%201963-03-02.pdf).

### RCA in 1964: established sellers plus development

Steve Sholes describes continued support for dependable established artists
alongside new-talent development, and a conservative programme of two or three
singles weekly, with occasional other-category additions. This is a firm-level
policy, not a per-act quota or a measured industry mean. Billboard, January 25,
1964, p.8, continuation of the Sholes interview on p.1.
[Original issue, accessible mirror](https://device.report/m/69d6d4e1978fcfef81bbfd2e253d26d407c05f52e9181b6ba88bcd82ceb6339d.pdf).

### Mercury and United Artists: planned labels and sales programmes

Cash Box, December 8, 1962, p.6: Mercury planned a joint executive/A&R meeting
to review artists, release counts and saleability for 1963, with differentiated
approaches for Mercury, Philips, Smash and Fontana. The same page reports UA's
forthcoming distributor meeting and named initial 1963 album programme, mixing
established acts with new names. These are planning and merchandising decisions;
the article supplies no universal formula for allocating every roster slot.
[Original issue](https://www.worldradiohistory.com/Archive-All-Music/Cash-Box/60s/1962/CB-1962-12-08.pdf).

### Laurie: a focused roster, material and promotional limits

Billboard, November 14, 1960, pp.2/4/14: Laurie reduced its roster to concentrate
promotion on a few continuing sellers. Distributors wanted consistent product;
sustained airplay and good material were scarce. Laurie described long-term
development, differentiation among its own acts and outside-master purchases.
This is evidence for limited commitments and repertoire acquisition, not a
universal one-act-per-genre rule or a numerical release-rate target for indies.
[Original issue](https://retrocdn.net/images/4/4e/Billboard_US_1960-11-14.pdf).

### Columbia: separate LP merchandising policy

Billboard, July 27, 1963, p.6: Columbia replaced two big annual release
programmes with a steadier monthly LP programme, featuring key artists through
the year; the report explicitly leaves singles policy unchanged. It does **not**
say Columbia had previously issued LPs only twice a year. The same page's Fox
fall package illustrates another company's continued batch programme.
[Original issue](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1963/BB-1963-07-27.pdf).

### Motown: supporting first-hand recollection, not a dated rate

In a 2017 interview, Lamont Dozier recalls artist-specific assignments with
deadlines, including preparation for performers going on the road, and formal
group review of prospective records. This supports anticipatory repertoire work
and approval decisions. It does not establish a precise 1960-64 weekly quota;
his later *Jimmy Mack* example is outside this window and is not used to set one.
[Dozier interview](https://www.musicbusinessworldwide.com/lamont-dozier-youve-got-to-work-at-songwriting-seven-days-a-week-theres-no-such-thing-as-writers-block/).

## What the current code actually does

1. `CompetitorManager.ProcessWeeklyReleases` makes one Bernoulli draw per active
   AI label and calls `TryReleaseRecord` at most once on its main path.
2. `CalculateLabelReleaseCapacityChance` divides `releasesPerMonth` by four,
   adjusts for status, availability and seasonality, and clamps the result to one.
   More capacity therefore cannot create multiple ordinary weekly slots.
3. `AILabel.GetArtistForRelease` gives due hot acts first claim when cadence is
   enabled; otherwise it draws from a weighted roster lottery. The small career,
   momentum and waiting bonuses do not constitute an artist release programme.
4. `TryReleaseRecord` decides format after selecting the act. The cadence lane
   can convert an album choice into a single. Delayed LP drops also reset
   `weeksSinceLastRelease`, so that field is not an independent single clock.
5. LP projects already have scheduling and delivery state. The opt-in
   `AlbumReleasePractice` also has a separate-singles opportunity, but it is
   conditional on an LP decision and is **not** a general roster release slate.
   It is absent from `ttx-v1-tax`; do not silently adopt its whole rejected
   parameter bundle under the name of a calendar repair.

6. The weekly probability has further throttles beside the clamp:
   `availabilityMod = eligible acts / 3` (capped at 1), a status multiplier
   (Struggling .5, Rising 1.2, Dying .3), the staff `CapacityFactor` when the
   corporate staff arm applies, and `MarketSeasonality`'s artist-availability
   multiplier. A slate that represents act eligibility and resources explicitly
   would double-count these if they were kept as probability factors. Each one
   needs a stated home (a resource, a feasibility check, or a date preference)
   or a recorded decision to drop it. Seasonality in particular must not vanish
   silently when the Bernoulli draw goes.
7. `AILabel.GetMinimumReleaseCooldown` keeps every act off the release path for
   12 weeks after any release (10 for Star and above, 8 for a Superstar at a
   Major). It does not bind today because hit acts' median gap is about 22
   weeks. Once the slate raises their rate it will. History's stars averaged
   about 4.7 singles a year (`ReleaseJudgementDirective.md` sample), a mean gap
   of about 11 weeks. A 10-week floor allows at most 5.2 a year even with perfect
   timing, so with ordinary dispersion it cannot reach that mean. The floor
   becomes a slate parameter to be checked against the historical gap
   distribution, including quick follow-ups to a breaking hit. It must not stay
   a hidden hard limit.
8. `TryIndependentSinglesProgramme` (under `AlbumReleasePractice`) is the only
   existing path that can issue a second record for a label in the same week.
   It is evidence that the release path survives a same-week re-entry. It is not
   a capacity model.

This is more than a weak choice of lottery weights: the model lacks a forward
single programme, shares its initial opportunity between formats, and turns a
monthly rate into at most one weekly event. There is also no future act-level
reservation to prevent an LP or another act's opportunistic release from
consuming the intended follow-up opportunity.

The control's Big-5 average **35.5-43.1 actual single launches per label per year**
over these three 1960-64 worlds. RCA is 43.1. Strategy decisions are 37.4-45.3;
they are a different count from issued singles or delivered LPs. Reproduction:
`SimLogs/hac-v2/control-major-throughput.csv`. The historical weekly policy
implies a much larger annual singles programme at that firm if sustained, but
is not a nationally measured annual total. Releases, chart debuts, scheduled LPs,
two sides of a single, reissues and duplicate territorial/format issues must not
be interchanged.

## Scale and conversion: what a larger programme must preserve

Added in review. These are the measured constraints that a capacity change runs
into. They come from existing runs and the earlier directive, not new sources.

**Compare shares of national issues, not raw counts.** `ReleaseJudgementDirective.md`
(Calibration notes, item 5) transcribes Billboard's 1966 directory estimate of US
45s issued: 5,797 / 6,036 / 6,700 / 6,543 / 6,504 for 1960-64, and LPs
3,112 / 2,927 / 3,655 / 3,686 / 3,553. Its issue/page still needs verification
before it is used as a hard gate. That directive found the sim at 82-90% of the
singles count on older controls. The `ttx-v1-tax` control is lower: 20,878
single launches over 265 weeks (three-seed mean, `SimLogs/hac-v2/screen.md`),
about 4,100 a year, or roughly two-thirds. It also has 9,194 actual album
launches, about 1,800 a year, or roughly half. Re-measure by calendar year with
the same counting rule before relying on either ratio. On a share basis, RCA's
documented two or three singles a week would be about 1.7-2.6% of national
issues. The sim's RCA (43.1) is about 1.05% of the sim's. The head's volume gap
is therefore roughly 1.6-2.5×, smaller than the 2.4-3.6× gap in raw counts,
because the sim's national total is also short. Calibrate tier programmes as
shares of a national total that is itself checked against the Billboard band,
not by multiplying label rates. Scope the Major comparison to the corporate
family: the Mercury item itself names Philips, Smash and Fontana. A sim "label"
must be matched to an imprint or a family before RCA's rate is used against it.

**National volume is in scope: close it as an outcome, not with a knob.** The
scheduler should aim to bring national singles and LP issues into the Billboard
band, not just reshuffle a fixed total. Conditions:

1. *Measure first.* Count issues by calendar year and tier on the current base,
   using one counting rule: A-sides issued, excluding reissues, duplicate
   issues and the second side of a single. Until the directory's scope is
   verified, the target is a band. Use the unadjusted estimate as its centre,
   and treat `estimate / 0.90` as a sensitivity case only.
2. *Attribute the shortfall before filling it.* Decide which tiers are short
   from evidence. The head is short (RCA, above). Already 70% of sim singles
   come from never-charting acts (`release-judgement-directive`). Whether the
   rest of the shortfall belongs to the Major/MidTier programmes, to too few
   active small labels, or to the regional tail changes what gets fixed. A
   uniform rate multiplier answers none of these questions.
3. *Volume should come from the programme and capacity rules.* It should not
   come from a separate target-seeking servo. If the scheduler with evidence-based
   capacity still falls short, report the gap and its tier. Do not raise
   rates until it closes.
4. *Know what more records do to demand.* Hot 100 slots are fixed. Check whether
   total single and LP units are fixed or scale with the number of eligible
   records. If they are fixed, extra issues split the same demand: units per
   record fall and the loss rate rises, which is the historical direction.
   Then small-label closures and chart breadth are the binding guards. If
   units grow with supply, total market units (`market-units-benchmark-includes-albums`)
   become the guard.
5. *LPs separately.* The sim is at roughly half of Billboard's LP count, and
   the national LP:single ratio was a flat 0.48-0.56. Raise LPs through the
   separate LP clock and judge the ratio by year. Runtime is LP-count-bound:
   a doubling of LPs needs a runtime estimate before a decade run, and 1960 LP
   unit share remains a guard.

Sequence: hold the national total roughly fixed in the allocation screen, then
let the capacity screen move it, so the two effects are not confused.

**More Major issues must mean less per issue.** Genre share tracks eligible
records (`market-share-is-supply-bound`, r = .94). The Big-5's collective Top 10
share drives CR4. The sim already runs it above history: about 21.6% history
for 1961-64, against a control of 26.2% in 1961, 31.7% with the lane and split,
and 36.5% with both (`hac-v2`). RCA's own 1962 account says it cut its programme
to support each record *more intensively*. Promotion per record was a share of a
finite department, not a constant. If the slate doubles Major volume while each
release keeps the label's whole push, Big-5 share will rise with supply. The
capacity step therefore needs a label-level promotion capacity that the slate
divides among concurrent releases. It must be costed and reserved like
production, and distinct from the same-act launch split, which divides only
an act's own fame. Without that, step 3 below cannot be judged.
The historical direction to check: Major share of issues **up**, Major Top 10
conversion per issue **down**, Big-5 Top 10 share flat or toward history.

**Per-act references** (weak samples, not quotas): stars about 4.7 singles and
2.5 LPs a year (sim 2.3 / 1.4); hit acts 2.19 Hot 100 debuts a year with a
13-week median debut gap (sim 1.23 launches, 27 weeks); 9.4% of hit acts'
charting weeks had two of their records on the chart. Albums currently take
0.68 of a hit act's 1.23 rolls a year (`hit-acts-release-too-little`, §17x).
Distinct acts on the Hot 100 run 645 a year against history's 386. A good
programme should lower that count by concentrating issues on proven acts, not
raise it by adding unknowns.

**Expected side effects to read, not targets.** More issues with divided
promotion should push the per-release loss rate upward. It is about 8% of
singles today, against the trade's 80-90% (`ReleaseLossCalibration-2026-10-21.md`).
A rise is the historical direction, but Small-label closures and chart breadth
are the guards on it. A separate LP clock will raise LP titles. LP count drives
runtime and 1960 LP unit share (`runtime-is-album-count-bound`), and the
national LP:single ratio was a flat 0.48-0.56, so watch both by year.

## Studio labels in this work

The studio arm (`--studio-parent-labels`) seeds MGM, ABC-Paramount, Warner
Bros., United Artists and Dot as standard MidTier labels with a parent company.
In spl-v3 they reach 60-80% of history's 1960-62 Top 10 share (history
21/17/17/17/12%). The remaining gap is conversion of their stars, not roster
size. Run the scheduler's calibration with them on:

1. **Capacity is being calibrated per firm type.** In 1960-64 these firms ran
   national programmes; MGM was commonly grouped with the majors in the 1950s.
   They get the MidTier envelope (1-2.5 releases a month). A capacity
   calibration done without them tunes Big-5 and MidTier volume on a world
   missing a fifth of history's Top 10. Studio-on then has to take that share
   back from the tuned tiers. Their programme size needs its own evidence
   (trade reports of their release schedules), not the MidTier default.
2. **Their known gap is what a programme addresses.** Stars who were not given
   follow-ups do not convert. The slate's follow-up reviews act directly on the
   spl-v3 miss, and the screen should show whether it closes it.
3. **The parent's cash rule meets the slate's reservations.** The parent tops cash
   up to the commitment reserve and "funds no new launches". If planned releases
   reserve cash, and those reservations count toward the commitment reserve,
   the parent will start funding launches indirectly. Decide the boundary
   explicitly: either reservations are excluded from the reserve the parent
   honours, or parent-funded programme spend is intended and logged as such.
   Check it with a probe before the screen.

Keep a studio-off arm in each screen so allocation and capacity effects can be
attributed. Studio is still not adopted. It fails the relaxed rank guard on seed
2002, and seeds 4004/5005 are unscreened. The final acceptance read is on the
arm the project intends to ship.

## Proposed replacement: a label programme and dated slate

The following is a model design inferred from the evidence, not a claim that
every historical label followed an identical algorithm.

**Plan work before issuing it.** Give each label a bounded rolling slate with
dated act/format intentions and an explicit state: proposed, in preparation,
ready, scheduled, postponed, issued or cancelled. An initial configurable
eight-week issue horizon is a plausible probe; repertoire/session work can
start earlier. The exact horizon, review frequency and freeze window are model
parameters, not universal historical constants.

**Preserve act programmes.** Cadence supplies a follow-up review and an intended
window; retention keeps successful acts available to fulfil it. Maintain a
single clock distinct from LP commitments and drops. Established sellers get
continuity, while the label deliberately considers development and regional or
specialist business. Use its existing strategy, repertoire and observed results;
do not assign every artist an equal quota or reserve a fixed percentage solely
to force historical hit totals. A failure to find worthwhile material can leave
a planned opportunity unfilled.

**Schedule packages, not lottery winners.** Compare feasible intentions using
contract commitments, follow-up urgency, already observed demand, suitable
material, expected contribution with uncertainty, and available promotion and
production resources. Record why a project wins, waits or is rejected. A ranker
cannot inspect the runtime quality of an unmade record or a future chart result.
At issue time, recheck rights, artist availability and funding. A blocked release
may be postponed or replaced by another ready item, with an explicit reason.

**Separate single and LP timing; share real constraints.** The label can plan
both for one act. They still compete for cash, sessions, preparation and scarce
promotion. An LP must not automatically erase a due single, and separate clocks
must not create free financing or duplicate a master. Reuse existing project,
catalogue, contract and finance services rather than inventing a parallel ledger.

**Replace the artificial weekly ceiling.** Represent label capacity as available
work/issue slots over time, capable of delivering zero, one or several releases
in a week. Production and promotion limits constrain actual commitments and
deliveries. Do not clamp a monthly rate to a Bernoulli probability. Unused
capacity must not accumulate forever and then dump a large batch. Label growth,
resource purchases and losses still have their actual costs and operating rules.

**Calibrate capacity as well as allocation.** The user authorizes changing the
cap and envelope. RCA's evidence supports testing a materially larger Major
singles programme, not multiplying every label's volume by the same factor.
Indie and specialist rates need their own evidence and sensitivity ranges.
First compare allocation effects under a matched envelope; then compare revised
capacity, and their combination. These are causal comparisons, not a requirement
that the adopted design retain the old envelope. Preserve the act-level split
and measure concentration and tenure as throughput changes.

**Make revisions intelligible.** React to the current record's observed progress,
promotional congestion, delivery failures, finance and genuine events. Allow
rush releases and rescheduling. Avoid a universal rule that nobody releases
while anything is charting, or that a ready record always launches immediately.
The existing launch split remains useful, but it is not itself a conserved
weekly promotional budget. A later resource allocator must not claim that it is.

## Build and measurement sequence

The current cadence/retention runs remain the mechanism screen on `ttx-v1-tax`.
Keep both mechanisms for the next scheduling work, as the user directed. A full
calendar is **not built in this change**; no source evidence is a substitute for
implementing and checking its state and finance boundaries.

1. Add calendar telemetry: intended/actual issue date, act, format, source,
   readiness, resources reserved, review evidence, postpone/cancel reason and
   missed follow-up windows. Preserve a bounded history and save/load continuity.
2. Replace roster allocation with a forward slate and independent single clock.
   Keep the old lane as a comparison and integrate its due logic into scheduling;
   do not return to a flat lottery once those obligations are represented.
3. Remove the weekly ceiling and test evidence-based capacity scenarios,
   separately from allocation and from the economic acceptance bar. Existing
   capacity rates themselves need revision; merely removing the clamp while
   leaving almost all rates below four per month would barely change throughput.
   The same step adds the label-level promotion capacity described under "Scale
   and conversion". Without it, extra Major volume is extra Major share by
   construction.
4. Read actual release distributions, never-charting and regional careers,
   contract delivery, delays, idle resources, material/session costs and producer
   load, as well as repeat hits. Add national issues by year against the
   Billboard band, each tier's share of issues, Major Top 10 conversion per issue,
   Big-5 Top 10 share and CR4, distinct Hot 100 acts a year, per-release loss
   rate, LP titles and runtime. Screen allocation and capacity as a 2×2 with
   studio on and off, against the accepted working bundle. Mechanism checks
   (state, save/load, finance boundaries) need both settings but no screen.

**Determinism.** Slate decisions must draw from keyed RNG, keyed by label, act
and slot identity rather than by sequence counters (`keyed-rng-ids-are-sequence-counters`).
With every new switch off, the existing streams must replay byte-identically,
as the `hac-v2` off replay did (95 streams). With the switch on, expect a full
reshuffle. Read the mechanism ledgers, not `sumAbsErr` alone.

The earlier `ReleaseJudgementDirective.md`, Phase 3, already sketches a slate
and independent calendar. It is design context, not an instruction imported
from an attachment. This research supplies period evidence and updates the
capacity question following the user's explicit authorization.

## Evidence and limits

Four newly downloaded issues, with original URLs, file hashes and page counts,
are indexed in `SimLogs/release-scheduling-research/manifest.json`. Relevant
pages and continuations were read; RCA March 1963 pp.4/12, Laurie November 1960
p.4, Mercury December 1962 p.6 and Columbia July 1963 p.6 were also rendered
and visually checked. The older January 1962 issue is retained under
`SimLogs/album-promo-research/` with its own provenance and rendered page.
The January 1964 Sholes continuation and Dozier interview were read online.
This is targeted source review, not cover-to-cover reading of all these issues.

No representative 1960-64 census of every label's full roster, failed releases,
prepared-but-unissued masters, resource commitments or actual promotion hours
has been established here. Exact act shares, universal calendar horizons,
development reserves and tier capacity multipliers remain modelling choices.
The existing 13-week due rule should remain a probe/reference, not be relabelled
a universally verified retail release interval: chart debut gaps, issue dates
and session dates measure different parts of the process.
The 1949 Atlantic/Plaza four-per-month policy found during the search and later
Motown anecdotes are not treated as period rate evidence. Successful-artist
discographies and chart entries alone cannot fill the missing denominators.
