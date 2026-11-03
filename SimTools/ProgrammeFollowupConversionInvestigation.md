# First-hit follow-up conversion and songwriting supply

2026-11-02. Read-only investigation of all eight completed `followup-v4-fresh` worlds.
No simulation settings, demand rates, LP rules or candidate defaults changed.

## Where the funnel loses acts

The cohort matches the concentration investigation: each act's first observed new Top 40 title,
launched NewSigning/Rising, with first entry in weeks 5 through end-minus-65. Every act has
52 weeks for subsequent launches, and every such launch has 13 further weeks for chart outcomes.
These are first observed hits, not verified first-ever career hits. All release routes are included.

Across eight worlds there are 2,511 act-world observations; 275 have no subsequent launch within
52 weeks. Of the 2,236 first follow-ups, 1,552 miss the Top 100 within 13 weeks, 434 reach only
#41–100, and 250 reach the Top 40. These pooled observations are not independent historical acts.

Equal-weight world means:

| First follow-up scope | Top 100 within 13 weeks | Top 40 within 13 weeks |
|---|---:|---:|
| All first follow-ups | 30.6% | 11.2% |
| First follow-ups launched within 13 weeks after the hit | 29.8% | 11.8% |

First-release median gaps are 6–8 weeks in every world. Scheduling alone cannot explain this loss.
These scope comparisons are descriptive, not a causal estimate of releasing earlier.

## Support does not broadly disappear

Closed follower books exist for 2,232 of 2,236 first follow-ups. Stocked advance copies sum to
approximately 100% of the booked intent in every world. Among first follow-ups missing the Top 100,
the summed follower grant / asked ratio is 96.4–97.9% across worlds. Grants are **before final consumer
budget clearing**, not proof of completed purchases or adequate ordinary replenishment.

The pooled median follower book for first follow-up misses is 4,180 copies; for Top 40 successes
it is 5,205. Books are larger than those of the origin hit where both exist, but this paired comparison
has only 899 observations: many origin records have no closed book. Missing books are not zero.

Programme issue rows cover 2,219 first follow-ups. Within each outcome group, the median ratio of
follow-up to hit reserved marketing is 1.0. Reservation is a budget proxy, not realized exposure or
continuity of push. Major follow-ups also convert poorly (world-mean 30.8% Top 100, 13.0% Top 40),
so losing Major access is not a sufficient general explanation.

The programme books campaigns for four weeks; `ProgrammePushFactor` then becomes zero. Ownership
transfer replaces the booking with zero-spend/zero-share support. These remain specific continuity
mechanisms to inspect with weekly exposure data, not demonstrated causes in this dataset.

## The measured quality loss is mainly hook strength

Pooled first follow-ups, change from their own origin hit:

| Outcome within 13 weeks | Hook change | Production change | Runtime quality change |
|---|---:|---:|---:|
| No Top 100 | -0.163 | -0.012 | -0.107 |
| #41–100 only | -0.127 | -0.011 | -0.087 |
| Top 40 | -0.040 | -0.009 | -0.024 |

The quality-loss ordering holds in all eight worlds. Runtime quality is not a synonym for melody;
the hook and production launch fields are observed separately here.

Among first follow-ups with runtime quality <= .65, 8/740 become Top 40 entries; above .75,
154/539 do. This strong association does not identify causality: first hits select unusually
successful material and market draws, and follow-ups can regress toward an act's normal level.

Only 51 first follow-ups use the forced own-session fallback (2 become Top 40 entries), versus
2,168 reviewed-material issues (245 Top 40 entries), with 17 issues outside this audit join.
The late/early own-session fallback is therefore not the dominant source of this cohort's failures.

## Songwriting generation and discernment: code findings

Artist originals are not uniform draws across the quality scale. `ArtistWriterCraft` is .6 times
the act's songwriting ability plus .4 times its most creative member. Original composition craft
is .8 times that craft plus .1 times an era term plus uniform noise of only +/- .05. The default
legacy hook formula then reads .75 times composition craft plus .15 times performance hook.
The optional `--artist-written-hook-scale` changes this scale but is absent from the screened
default profile. Strong self-writers can consistently generate stronger hooks; their hook ceiling
and genre/era term are a separate existing calibration question.

The raw recording starts with uniform hook noise from -.12 to +.18, plus artist-performance
variance. However normal Polar realization uses 90% composition hook and 10% performance hook,
then a small composition ceiling adjustment and a capability factor. The broad raw session draw
is substantially attenuated; it is not the whole shipped hook distribution.

Professional office songs expose a more direct weakness. `GenerateInitialProfessionalCatalog`
draws composition and melody independently from N(.62,.15), and hook from N(.68,.15), clipped to
[0,1]. It assigns a staff writer ID afterward. `WriteOfficeSong` repeats those generic distributions.
The credited writer's melody/hook craft does not set these baseline distributions. Existing optional
professional-team processing applies a small team-versus-lead craft delta, not a writer-specific
baseline. A high-craft credited writer therefore does not by itself guarantee a consistently stronger
professional catalogue. Roughly 95% of the unclipped generic hook distribution spans .39–.97.

There is discernment: dated songs enter a bounded shortlist; the label chooses the best perceived
recordable song, with persistent perception error. Heard-read half-width ranges from .08 to .20
depending on scouting, with 25% shared label/act bias. Project judgement compares material against
the act's expected own session, allowing uncertainty. This is an act-relative acceptability bar,
not a requirement to match the previous hit's hook. Professional arrival rates depend on source mix
and label scouting access. Material searches can sample existing professional songs and pay for
delivery, but these paths do not establish a hit act's recurring relationship with the successful
writer/team. A popular teen idol can receive professional material, but this is not equivalent to
a dedicated team consistently supplying its follow-ups.

## Commercial-fit protection is narrower than the first-hit cohort

`HitActHype` gives Top 40 recency half the Top 10 awareness lift. `HitActFitSupport`, in contrast,
requires the saved Top 10 clock: its .6 national floor decays toward the .12 base with a 26-week
half-life and ends after 52 weeks. It is captured at launch. Regional fit and the .88 scale still
apply afterward; .6 is not a floor on final regional demand.

Only about 35% of first follow-ups have an observed prior Top 10 in the preceding 52 weeks.
Those releases have higher entry conversion (world means 43.5% versus 23.7% Top 100; 16.7% versus
8.3% Top 40). Differences remain within broad quality bands, but prior Top 10 status also selects
different acts and audiences. This is not an isolated test of the floor. Observed prior charts do
not reconstruct pre-1960 or saved-clock eligibility exactly.

## Next attribution

Prioritize professional writer-conditioned song supply, continuity of writer/team access, and
shortlist selection error before changing demand. Trace song source, credited writer crafts,
composition hook, expected recorded hook, perceived read, best available alternative, and shipped
hook on the origin hit and follow-up. Distinguish an actually weaker available song from a bad
selection or execution loss. Evaluate the emerging repeat-career middle and leading-act concentration
together: concentrating all strong material at successful incumbents could worsen concentration.

Weekly offchart demand stages and actual campaign shares are absent from these completed CSVs.
The existing `--followup-chart-trace` starts at Top 10, so it excludes the crucial Top40-only cohort;
using it unchanged would bias the next investigation. Any future trace should cover Top40-only acts
and retain offchart follow-ups, and establish replay identity before interpreting diagnostic additions.
No blanket hook boost, writer-floor change or new scheduling experiment is established by this readout.

## Reproduction and validation

Run `python SimTools/analyze_programme_followup_conversion.py` for all four seeds and both arms.
Completion manifests and complete 100-position weekly charts are checked. Evidence is under
`SimLogs/followup-v4-fresh/followup-conversion/`: `actors.csv`, `releases.csv`, `summary.csv`, `method.json`.
Two synthetic tests verify follow-up windows, prior-only Top 10 evidence, predecessor timing and
retention of actors without follow-up releases. No game build or simulation rerun is needed.
