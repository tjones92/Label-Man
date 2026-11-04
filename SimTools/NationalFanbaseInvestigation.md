# National fanbase and career continuity investigation

2026-10-24. Investigation, implementation sequence and experimental prototype.
The user asked to develop an artist
audience that persists between records and gives the game world something to
respond to beyond chart counters.

The user subsequently authorized continued experiments with best judgment until
a promising blend is ready for review. The first optional AI-single demand
prototype is built; its first screen completed under `SimLogs/afb-v1`.
This is not adopted game behavior. See the implementation notes at the end.

## Working reference

Keep the recent-hit national commercial floor **0.6 as the provisional development
reference**, on the complete repair/earlier-planning bundle. This is an experimental
reference, not a new game default or a declaration that career building is solved.
Its completed sources are `hfs-v2-f0p6` (1001) and `hfs-v5-f0p6` (2002, 3003).
Retain support-off sources `hfs-v2-off,hfs-v4-off` and floor-1 sources
`hfs-v2-f1,hfs-v3-f1`. Every world covers 106 weeks with the same assembly.

The three-seed .6 comparison improves field later debuts/act 1.48 -> 1.61,
recognition growth .071 -> .080, and annual field two-Top-10 acts 7.33 -> 9.50
(recovered 1960-61 history: 8.50). New Established counts are 3/3/8 in control
and 7/4/3 at .6: **14 in each arm**, not a replicated promotion gain. Career
progression rates average 19.4% -> 22.0%, with different outcome cohorts. These
mixed results justify retaining a useful world for development while investigating
the underlying mechanism. Full evidence: `SimLogs/hfs-v5/review/`.

The floor is a proxy for continuing interest. A real returning-audience mechanism
must be tested with the floor removed as well as retained; stacking both without
an interaction test could pay for the same audience twice.

## What the current model carries forward

| State/path | Existing behavior | Limit relevant to fanbase continuity |
|---|---|---|
| `ArtistRecognitionService`, `SimulatedArtist` | Durable public familiarity and cultural standing; gains from milestones and completed chart runs, diminishing returns and calendar decay | Mostly a national scalar, not a retained pool of listeners with a propensity to try the next record |
| `CompetitorManager` launch path | Recognition affects awareness, stock and radio, all bounded; momentum/reputation also contribute | Recognition already has economic readers. A fanbase must not be described as the first success feedback or added as duplicate awareness |
| `HitActHype` | Recent Top 10/Top 40 clocks lift the next single's launch awareness | Recency of chart success, not who became attached or who returns |
| `RegionalAudience` / `LocalSceneRoomService.LocalDraw` | Live set attendance and stayed share create a local draw proxy, fading from the last local appearance; hometown/live-following deposits reach single awareness | Separate from record purchasers. Repeated attendance is included more than once and then saturated; it is not an identified unique fan count |
| `MediaFanState` / `MediaAtmosphereService.Fans` | Sparse regional teen attachment, response to sales/TV/press, fan episodes, letters and member spotlight | Media-owned. `MediaAtmosphereMode` currently has only Disabled and Observe; causal mode is explicitly unimplemented |
| Media booking/press | Fan episodes affect TV interest, publicity and the stories the player sees | Real world characterization exists, but this observe-mode layer does not feed the record economy or canonical career state |
| `ChartSimulator.CalculateRegionalSales` | Population, record awareness/discovery, material/genre response, persistent per-record commercial fit, availability and budget determine purchases | No shared artist-audience reader. Exhaustion uses this record's cumulative regional units; the next record starts its own exhaustion history |
| `StationRuntime.artistLoyalty` | A dictionary/getter and candidacy multiplier exist | Repository search finds no production writer to populate it. It is an extension point, not evidence of an earned programmer-memory loop |
| Band life, wealth, contracts and release programmes | Success already affects people, money and career decisions | These are useful systems to connect to audience evidence; they do not establish buyer continuity themselves |

Recognition and hometown audience are enabled in `chart_manager.tscn`. The gap is
therefore a **missing connection between artist audience experience and future
record demand**, not simply an existing fame switch left off. The broad market
also changes by region, genre and year; calling all of it static would overstate
the diagnosis.

The strongest code evidence is the observe-only media boundary and the absence
of an artist audience input in single demand. The claim that adding continuity
will repair careers remains a hypothesis to test.

## Proposed model: national reach assembled from local audiences

Use sparse **artist x region x audience segment** state. A national fanbase is the
population-weighted sum of those regional relationships, with breadth reported
separately. A Southern R&B favorite, a country touring act and a national teen idol
should retain different footprints. Do not grant uniform nationwide fans at the
first Hot 100 entry. Individual simulated consumer agents are unnecessary for the
first implementation.

Start with two distinct stocks:

- **Familiar audience:** people likely to notice the artist's name or hear about
  a new release. More people can recognize an act than follow it.
- **Attached audience:** a subset inclined to try a suitable next release and
  return to a performance. Attachment requires positive encounters; exposure
  alone cannot mint it.

Store bounded cohort fractions or calibrated audience equivalents, not a claim
that cumulative units or fan letters identify unique individuals. Keep attachment
within familiarity and each region/segment's reachable audience. Fan populations
can overlap between artists; they are not an exclusive allocation of the public.
Existing segment routes should be reused where suitable rather than inventing
one national teen demographic for every artist.

### Earn, retain, disappoint

1. Distinguish first contact, trial, satisfaction and return. Actual cleared
   purchases and completed performances provide experience; availability failures
   do not count as a listener rejecting the artist. Listening can create familiarity
   without a purchase. Each event needs provenance and a once-only settlement latch.
2. Convert a bounded share of positive encounters into attachment, with diminishing
   returns. Repeat performances, suitable material and a coherent creative identity
   can deepen it. A hit creates an opportunity to build a relationship, not a
   permanent entitlement to the next hit.
3. Retain attachment across release gaps and modest records, with different decay
   from short-lived hype. Disappointing experiences, repeated poor follow-ups,
   overexposure and a change of musical direction can reduce or split it. Inactivity
   and disappointment need separate causes in the ledger.
4. Preserve real failures and one-hit careers. Attached listeners can try a weak
   single without the broader public adopting it. Material, discovery, stocking,
   distribution and the consumer purse continue to constrain sales.

Do not choose recruitment rates, retention half-lives or genre-change penalties
from the desired repeat-hit total. Those are unresolved design/calibration inputs.
The media system's eight-week teen-attachment half-life describes a publicity
episode; it is not automatically the lifespan of a committed national audience.

### The first economic reader

Test a **returning-audience component of single demand** first. Divide the same
reachable audience into returning and discovery components; apply suitable trial/
purchase probabilities, then the existing supply and market clearing. This must
replace the relevant part of anonymous demand, rather than add another population
or multiply every current term by a fan bonus.

- The same listener cannot be counted in both components for the same purchase.
- Keep per-release cohort ownership/purchase depletion; a fan does not buy the
  same 45 again every week simply because attachment persists.
- Concurrent singles share attention; each cannot claim the entire artist audience.
- Fan demand competes for the existing regional/segment consumer budget. No free
  units, larger purse, guaranteed chart rank or automatic national distribution.
- The demand reader sees last completed week's audience. This week's hit cannot
  create fans that buy the same record earlier in the same settlement.
- Labels can estimate their act's returning audience with uncertainty. They still
  cannot inspect the hidden per-record commercial-response draw.

Measure how the current commercial-fit draw interacts with the returning cohort.
Applying the full cold-market downside to every committed listener could recreate
the present problem inside a new data structure. Completely exempting fans from
material response would create the opposite problem. This choice needs an explicit
bounded response curve and record-level attribution, not another global multiplier.

## Give the world readers of the relationship

After isolating the first demand reader, add and test other readers separately:

| World actor | Evidence to consume | Observable consequence |
|---|---|---|
| Label / A&R | Retained audience, footprint and expected return, with forecast uncertainty | Follow-up commitment, appropriate campaign, realistic renewal offers and release priorities |
| Programmers | Local listener return/requests and remembered prior support | A plausible audition for a known act before chart entry; no automatic playlist entitlement |
| Live bookers | Local draw plus the relevant audience footprint | Suitable rooms, repeat bookings, broader routing, offers constrained by calendars and costs |
| Press / television | Familiarity, attachment, distinct members and recent behavior | Interest that can persist between singles; coverage remains attributable to an outlet and event |
| Artist / player | Why the audience grew, stayed, split or left | Meaningful choices about material, image, performing and release spacing |

Reuse the existing media fan presentation and live-scene evidence. Do not silently
turn observe-mode media state into an economic writer, or treat its letters as
additional independent proof of the sales that generated them. A canonical audience
service should own the new causal state; media can present it and later submit
provenanced inputs under an explicit tested adapter. Existing recognition should
continue to mean familiarity/standing; decide its overlap with the new familiarity
stock before implementation so two competing fame ledgers do not drift.

## Implementation and experiment sequence

1. **Observation ledger:** capture artist/region/segment contacts, existing fame,
   media attachment, local draw, record trial/return proxies and the intended
   recruitment/retention accounting. Distinguish measured events from estimated
   unique audience. Define ownership, initialization and schema before activating
   any reader. Prove no economic/RNG drift.
2. **One demand reader:** implement and validate cohort accounting, depletion,
   concurrency, old-save activation boundaries and real save/load/resumed play.
   Opening legacy audiences need an explicit estimate from pre-1960 evidence;
   loading an old save must not replay past purchases into new credit.
3. **Four-arm short screen:** support off, .6 reference, fanbase without the floor,
   and fanbase plus .6 as an interaction diagnostic. Use the same 1001/2002/3003
   worlds, all other settings fixed, 106 weeks and full 52-week hit cohorts. Prefer
   replacement of the proxy if earned audience does its job. Do not promote the
   combined arm because its hit count is simply largest.
4. **World readers and longer careers:** isolate label/programmer/booker effects
   before combining them, then extend promising settings through the longer
   historical window. Inspect several complete individual careers as well as
   aggregates; a 106-week screen cannot establish a credible decade.

Acceptance needs progress beyond titles: retained audience after the first hit,
returning vs new buyers on each follow-up, repeat Top 40/Top 10s, viable release
chains, departures/silence, recognition, time to decline and meaningful world
opportunities. Keep cold acts, low sellers and failed follow-ups in denominators.
Report label/genre/region differences, market reconciliation, concentration, units,
losses and company survival alongside these outcomes. A chart match alone is not
evidence that the underlying career mechanism is credible.

## Evidence and next unresolved decisions

The completed .6 screen is evidence for keeping that candidate, not an estimate
of fan loyalty. Only one of 56 eligible field Top 10 follow-ups shows any direct
floor binding in its captured regional weeks, while 121 of 202 misses do. This
narrows the immediate explanation but does not causally attribute later hits:
career and label trajectories, competing records and record IDs can diverge.

The prototype decisions below resolve state ownership, overlap with recognition,
event provenance, title depletion and initialization for the first experiment.
Audience scale and numerical loyalty/retention assumptions still need sensitivity
checks and a separate historical research pass. None is presented here as a
recovered historical fact.

Source anchors: `Systems/ChartSimulator.cs` (regional sales and potential audience),
`Systems/ArtistRecognitionService.cs`, `Systems/CompetitorManager.cs` (launch),
`Systems/RegionalAudience.cs`, `Systems/LocalSceneRoomService.Bookings.cs` (LocalDraw),
`Systems/Media/MediaFans.cs`, `Systems/Media/MediaAtmosphere.cs`,
`Systems/Media/MediaAtmosphereService.Fans.cs`, `Systems/Radio/StationRuntime.cs` and
`Systems/Radio/StationNetwork.Playlist.cs`. Existing design context:
`CelebrityRecognitionDirective.md`, `RegionalAudienceDesign.md` and
`MediaAtmosphereDirective.md` (especially the observe-mode ownership boundary).

## First prototype implementation

`ArtistFanbase` is opt-in. `--artist-fanbase=0` observes acquisition and saves
audience stocks while returning the original demand float exactly. A positive
value is the maximum weekly purchase-trial probability before material, awareness,
response, title attention and concurrency terms. The first screen uses .25.
Other initial assumptions are .35 recruitment and a 104-week attachment half-life;
they are exposed as `--artist-fanbase-recruit=` and `--artist-fanbase-half-life=`.
These are model assumptions, not recovered loyalty statistics.

The prototype reuses existing recognition for national familiarity and adds only
the sparse attached audience. Regional/segment recruitment is based on actual
cleared non-returning purchases, multiplied by recruitment and
`clamp((quality-.4)/.4,0,1)`, then saturated against .20 of the segment's buying
population. The estimated audience can overlap between artists; consumers still
share the existing purse. Opening fan estimates use only pre-1960 public history,
with dated decay, not current/future success or a replay of past purchases.

At release, the compatible regional cohorts are snapshotted and saved on the
record. Its own newly recruited fans cannot refresh its launch pool. Every week's
fan attribution depletes this title's pool once, and the entire original pool is
removed from the anonymous component, including after the title's fan window ends.
Returning purchase probability reads material (`quality^1.5`), notice
(`clamp(baselineAwareness+.35,0,1)`), an attenuated response
(`.5+.5*clamp(unprotectedCommercialFit,0,1)`), attention shared between concurrent
titles and an eight-week title-interest half-life. This softens the cold-response
downside for an earned cohort; it does not apply a universal floor to the public.
Direct returning intent ends after 26 record weeks; attachment itself persists.

All intent still passes through ordinary inventory, shelf capacity, physical
fulfilment and consumer clearing. Unfulfilled intent creates neither purchase
experience nor fan depletion. Disappointment removes a bounded amount of the
actual returning cohort when material is weak. The acquired, departing and
returning ledgers reconcile, and settlement is credited once per week.

This first writer covers AI singles. Player daily trunk purchases, live and media
recruitment, label forecasts, programmer loyalty and new booking income remain
outside it. Those limitations matter when assessing a promising blend: this is
a test of career continuity through returning buyers, not a completed fanbase
feature for the player.

Initial verification: clean build; 222 audience assertions and 47 existing
stocking assertions; full-world JSON, actual gzip and resumed-play checks with
live stocks and launch cohorts. The zero-trial observation replay matches the
existing .6 world in 65 week-keyed streams through week 105. Economic identity
and audience accounting are distinct checks. Per-artist weekly audience and
per-record regional returning/anonymous attribution are exported for review.

The completed three-seed screen and review candidate are documented in
[NationalFanbaseScreenResults.md](NationalFanbaseScreenResults.md). The blend
improves entry and continuing careers but retains a major/studio imbalance. The
latest check bundle has 231 audience/renewal assertions (including nine finance
gate checks), 47 stocking assertions and real gzip/resumed play. A separate
optional renewal probe investigates the reserve-horizon asymmetry between funded
release slates and recent-hit contract renewals; it does not change fan demand.
