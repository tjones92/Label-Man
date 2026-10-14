# Local Scene Ecosystem Directive — Cities With a Memory

**Status: researched design directive; no implementation authorized by this document alone.**

Prepared October 8, 2026. Branch: `codex/local-scene-ecosystem`, isolated from `band-member-simulation` at commit `3203a66df703325b22f206d674d9da96cff04f50`. The source branch had ongoing uncommitted work; this design uses its committed state and does not modify those edits. Re-audit integration seams before implementation.

Companion: [Historical scene atlas and research ledger](LocalSceneHistoricalAtlas.md). It covers all 31 playable cities and distinguishes researched anchors from provisional content directions. Historical evidence establishes constraints and mechanisms; formulas, counts, thresholds, and UI behavior proposed here are design choices, not historical measurements.

## 1. The experience we are building

The player should remember the Thursday band, the room that gave its singer a chance, the rival who signed them, and the drummer who later turned up across town. Returning after six months should reveal a changed scene whose history remains recognizable. Moving from New Orleans to Miami should change musicians, venues, repertoire, audiences, industry contacts, and practical opportunities—not merely scenery or prices.

Build a persistent, geographically grounded music society underneath the record business. It must support local careers that never become national recording careers, visiting acts whose hometowns remain elsewhere, and labels whose rosters reflect people they can actually discover and persuade.

Success means:

1. Every act has a stable hometown/formation place, a current operating base, and a traceable history of moves.
2. Scouting observes scheduled activity by existing people. Reopening a panel never creates talent, destroys an act, changes a bill, or rerolls its underlying quality.
3. Every playable city has a researched character, multiple musical communities, persistent venues and contacts, and change through the decade.
4. Small labels recruit mainly through local and connected circuits. A distant signing has a discoverable cause. Majors recruit nationally through real reach and finite attention.
5. Signing, relocation, disbandment, and national breakthrough are different events with different local consequences.
6. Scenes generate stories from simulated events, with imperfect information and believable delays.
7. Geography does not accidentally destroy calibrated artist supply, the release economy, genre chronology, or band-member identity.

**A scene is a network of opportunities and relationships, not a bag of artists sharing a genre.**

## 2. What to keep and correct in the sketch

Keep persistent residents, named rooms, recurring casts, locality in A&R, slow-changing institutions, short-lived buzz, relationship-based tips, and historical variation. Make these corrections binding:

| Sketch assumption | Directive decision |
|---|---|
| Every generated act disappears on the next scout | The code spares notebook acts and refuses removal of signed artists. Other generated discoveries remain disposable; that is enough to break continuity. |
| Delete `PurgeGeneratedProspects()` first; low risk | Replace slate-time creation and cleanup together. Removing cleanup alone creates unlimited births through repeated scouting and risks ownership/cache inconsistencies. |
| A scene resident exists until signing, disbanding, or leaving | Signing changes contract status, not existence or automatically residence. Moving changes membership; terminal careers retain archival identity. |
| Roughly 80 acts represent a city's entire population | Simulation population is a selected cohort, not a census. Commercial rosters, amateur activity, and performance capacity have different denominators. |
| Vitality drives talent quality | Institutions and practice affect development and opportunity. Geography must not confer innate ability or hidden ceilings. |
| One signature genre per city | Multiple subscenes coexist. Sound is also repertoire, arrangement, personnel, production practice, and audience expectation. |
| San Francisco is dead before 1965 | False as a description of musical life. Folk and jazz precede its psychedelic export boom. See atlas. |
| Detroit pivots from soul to proto-punk; Philadelphia collapses after 1963 | New scenes coexist with continuing soul activity. National attention is not total local vitality. |
| Ballrooms and armories unlock in 1966 | These existed earlier. Change programming, promoters, amplification, audience, and scale over time. |
| A Memphis indie never signs Seattle talent | Rare, causally grounded exceptions are desirable. Unexplained national shopping is the defect. |
| Old acts can all be assigned to regional hubs | This manufactures hometowns. Use deterministic, confidence-marked migration and preserve unknown origin when evidence is absent. |
| Foreign scenes require foreign US sales regions | Separate place identity and foreign talent sourcing from US distribution. British musical life exists before 1964. |

The sketch's 25–30% annual circuit turnover does not replace almost everyone within five years: independent annual retention of 70–75% leaves roughly 17–24% of the original cast. Long-lived survivors are valuable. Do not force turnover to meet a narrative slogan.

## 3. Code-verified starting point

These are observations of the branch base, not claims about later band-member edits.

| Seam | Verified behavior | Required intervention |
|---|---|---|
| `Systems/PlayerDesk.cs`: `ScoutVenue`, `GenerateLocalProspect`, `PurgeGeneratedProspects` | Four venue categories; regional unsigned sampling; short bills topped up with generated artists; unkept generated acts removed from the registry on another scout. | Replace with booking observations and persistent discovery records. Remove obsolete slate ownership, not legitimate terminal lifecycle management. |
| `PlayerDesk.BuildRegionAffinity`, `PlausibleGenres`, `IsLocal` | Regional consumer affinities govern supply/admission; locality is normalized region-name equality. | Stop treating what a region buys as what every city produces. Participation and actual presence determine a live encounter. |
| `ArtistManager.GenerateArtist` | Fourth argument is a **region string**, not a city ID. Default is a uniform draw among seven named regions. | Introduce a typed geographic assignment seam; do not pass `cityId` into the existing parameter. |
| `ArtistManager.MaterializeRuntimeFormation` | Responsive formation, band-life formation debt, dissolution refill credit, genre selection, and recombination already exist. | Scene placement consumes births from this owner; no competing birth clock. |
| `Data/SimulatedArtist.cs`; `ArtistPublicProfile.cs` | Artist has `homeRegion`; public `homeCity` aliases it. | Real place fields, independent public display, explicit compatibility mapping. |
| `Data/AILabel.cs`; `DistanceModel` | Labels already have `homeCityId` and assignment provenance. There are 31 domestic cities, several town-to-hub aliases, and six UK fallback names. | Distinguish actual places from distribution proxies and foreign fallbacks. |
| `DistanceModel.AddCity` | Population is a parameter used to seed store counts; `MarketCity` does not retain it. | Add a documented datum if capacity needs population. Do not infer population from stores. |
| `RosterManager.FindArtistForLabel` / `ScoreArtistForLabel` | Initial rosters use genre/quality with a whole-pool fallback and no city-distance term in the inspected score. | Fix initialization as well as runtime, or opening rosters remain wrong. |
| `RosterManager.GetEnabledSupplyCandidates`, daily talent market | Regional selection and `NationalFreshRecovery` exist; offers resolve through a shared market. | Apply geographic eligibility to discovery and recovery, not only final scoring. |
| `Data/CityProfile.cs` | Authored costs/studio values and prose are explicitly player-side. | Do not turn player balance constants into historical facts or silently apply them to AI. Revise prose after content exists. |
| `MusicInfrastructure` | Regional studio, talent, club, theatre, and circuit fields. | Reuse as documented fallback with city/subscene overrides; no automatic Nashville identity for every southern city. |
| `WorldSaveData`, `SaveGameService` | World save plus separately restored player artists; v4 at this base; band-life pool, seed, debt, credit persist. | Add scene state; relink after both artist sets exist. Never duplicate artists inside scenes. |

Audit `JournalisticDescriptor` too: current phrasing describes a public `homeCity` as a region. Find every caller, not just the scouting card.

## 4. Geography: origin, residence, presence, and connections

### 4.1 Places are not market regions

Keep the seven-region sales/distribution model as a compatibility projection initially. Introduce canonical places with stable IDs, names, country, coordinates, optional US market mapping, and optional playable-city catchment.

A **place** may be a playable city, satellite town, foreign city, or recording destination. A **scene** is its local social/music system or an explicitly defined metro catchment. A **market region** is a sales aggregation. No string field should represent all three.

Connect Seattle/Tacoma, Boston/Cambridge, Minneapolis/St. Paul, and San Francisco/Oakland/Berkeley without relabeling everyone as a hub resident. Muscle Shoals, Gary, Bakersfield, Clovis, and Macon illustrate why smaller places need identity before becoming playable start cities. Satellites need not all become full markets.

Existing Oakland→San Francisco, Milwaukee→Chicago, Jackson→Memphis, and Indianapolis→Cincinnati aliases may remain legacy distribution lookups. They must not establish actual hometowns.

### 4.2 Authoritative fields

Recommended conceptual contract; adapt names to code conventions:

| Entity | Geography |
|---|---|
| Musician | `originPlaceId` when known, `basePlaceId`, dated moves; preserve `personId` through lineups. |
| Act | Immutable `formationPlaceId` (act hometown), mutable `basePlaceId`, current itinerary/presence, former bases and dates. Soloists can separately display personal origin. |
| Label | True HQ place, branch/A&R offices, scouts/agents, referral and licensing connections, recruitment budget. |
| Venue / studio / radio outlet | Exact place, catchment, effective dates, community/circuit affiliations. |
| Recording | Actual recording location; do not infer hometown from its studio. |

If compatibility requires `homeCityId`, define it as the playable base-city projection and name true origin separately. Document `homeRegion` for each consumer. Preserve display-region strings where expected; add a region-ID adapter. Artists currently use values like `Deep South`, while labels use IDs like `deepsouth`. Blindly assigning `parentRegionId` everywhere is unsafe.

A touring band in Chicago is not a Chicago band. A Memphis contract does not make its Atlanta act Memphis-born. A migrant can retain an Oakland hometown while joining Los Angeles's working scene.

### 4.3 Four related geographic graphs

- **Travel:** route time, money, overnight needs, feasible dates, seasonal friction.
- **Booking:** promoters, circuits, college dates, support slots, package tours and introductions.
- **Information:** DJs, shops, jukebox operators, musicians, managers, scouts, trade press.
- **Recording/business:** studios, session personnel, publishers, labels, distribution and licensing.

A record can reach a label through a distributor without the artist traveling there. A touring artist can be heard locally while living elsewhere. Both explain distant signings in different ways.

Start with existing approximate domestic road travel and scheduled circuit edges. The road-mile approximation is not an international travel model. Unknown routes must not resolve to zero distance.

## 5. Scene structure and historical dynamics

### 5.1 Static profiles and mutable state

Keep geographic registry, authored history, runtime state, and player knowledge separate.

- `SceneProfile`: place/catchment, dated subscene profiles, population source/year/boundary, opportunity baselines, institutions, circuit connections, sources and confidence.
- `SceneState`: resident IDs, participation, opportunities, venues, bounded heat, local reputations, significant history, processed tick.
- `SubsceneProfile/State`: overlapping community with activity, audiences, institutions, repertoire/style tendencies, access and connections.
- `SceneParticipation`: resident, regular visitor, alumnus, touring guest, or session worker relationship, with dates and strengths.

Do not encode race, ethnicity, or class as musical ability. Community context influences learned repertoire, institutions, opportunities, access, and connections. Individuals cross scenes and diverge from typical paths.

### 5.2 Five quantities that remain separate

1. **Activity:** people making music and frequency of work.
2. **Opportunity:** gigs, rehearsal space, equipment, tuition, studios, publishers, record-business access.
3. **Audience composition:** overlapping groups and what they respond to.
4. **Export visibility:** national press, airplay, releases and touring reach.
5. **Heat:** transient attention to particular events or subscenes.

These separate New Orleans jazz continuity from pop-chart visibility and allow a prosperous country circuit outside a national country recording center.

Use dated, overlapping curves for subscene opportunity and style prevalence. They are priors with bounded changes from openings, migration, records, mentorship and investment. Never force a hit, predetermined star, or exact chart share. Historical institutions/external events may have authored dates; simulated businesses and careers respond to play. Distinguish the two.

Do not close a fictional venue because its approximate historical analogue closed. Historical venues cannot operate before documented openings. Represent uncertain dates as ranges, not invented precision.

### 5.3 Sound travels through people and work

Map local styles onto existing canonical genres plus extensible descriptors. Keep the genre enum stable initially. Traditional New Orleans jazz and modern jazz need different scene descriptors even if both map to `Jazz`; conjunto and West Side R&B hybrids should not become identical because both touch `TexMex`.

Influence enters through exposure, repertoire, mentors, co-writing, arrangements, producer/session choices, and repeated work. Use existing musical identity/Polar/member axes where appropriate. Never stack an invisible city quality bonus on every record.

A session player carries practice to a new studio; a visitor spreads a riff; musicians imitate a successful arrangement. Persist influence provenance. Players build a sound by developing people and institutions, without changing an entire city's taste with one hit.

## 6. Population and carrying capacity

### 6.1 One birth ledger, several capacities

`ArtistManager` owns births and career population. `BandLifeService` owns people, lineup events, and its debt/credit arrangements. Scenes request placement and report pressure; they do not independently spawn replacements after signings or venue vacancies.

Distinguish background community activity represented statistically, persistent materialized acts, active local performers including signed residents, contract-seeking acts, recurring acts visible to an observer, and booked performance slots per week. Do not call all six capacity.

The sketch's population formula can be an opportunity prior after documenting units and inputs. It is neither a historical census nor a safe cap on the existing national registry.

### 6.2 Placement before recalibration

First allocate existing initial/runtime cohorts geographically, preserving total counts and canonical genre mix as far as assignment permits. Audit reserve generation, runtime labels, solo spin-outs, recombination and dissolution refills.

For an already budgeted formation of genre `g`:

```text
placementWeight(c,g,t) = opportunity(c,t)
                       × formationFit(c,g,t)
                       × availableConnections(c,g,t)
                       × softHeadroom(c,g,t)
```

Normalize over eligible places. Use sublinear population influence so metros matter without excluding specialized smaller centers. High recording opportunity need not imply large residential population.

Keep headroom nonnegative, with explicit overflow behavior. Saturation can reduce regular work, increase underemployment, or encourage later voluntary moves. It must not delete residents, cancel remaining national formations, or force a soul act into an implausible town solely for an empty slot. If all weights collapse, log it and use a documented genre-compatible overflow rule. Values require calibration.

Restricting local discovery must not trick the vacancy servo into extra national births. Diagnose geographic mismatch separately from insufficient total supply. Observe unmet demand by city/genre before changing formation targets.

### 6.3 Recurring casts without a tiny universe

Use latent/occasional/working/circuit/headliner as **performance participation**, independent of career and seeking status. A local headliner may not want a deal; a signed act may remain a room's regular attraction.

In particular, the existing `ProspectMarketStatus` latent/seeking rotation is a contract-search mechanism, not the new performance ladder. A latent contract prospect can still work publicly. Hearing them does not silently make them eligible for signing: an invitation to discuss a deal must pass the existing activation, cooldown and ownership rules through their authoritative service. Conversely, a temporary end to a contract-search spell must not remove the act from its venue calendar or erase its local relationships.

As a UX hypothesis, aim for roughly 12–25 recurring recognizable acts across the rooms a player frequents in a substantial scene, fewer in a small catchment. This is an attention budget, not a census. Big cities have many more recurring acts across subscenes.

A notebook is knowledge, not immortality or a permanent market hold. Preserve intentional short negotiation holds through their existing owner and expiry. Writing down a name cannot prevent rival signing, relocation, or breakup.

The first named, actionable encounter must resolve a persistent person/act ID from an independently budgeted cohort. Never materialize extra high-quality prospects because a player clicks. Background aggregate texture must not pretend to have been a named signable act all along.

### 6.4 Local legends are careers, not traps

Long tenure, remembered performances, community ties, and sustained draw confer local standing. Such people need not have low ceilings, be old, unreliable, or bad investments. Some choose family, steady income, independence or community; others face exclusion or lack business connections.

A local legend can anchor a venue, mentor musicians, lead a house band, record a regional seller, or achieve late national success. Reluctance to travel is character information, not a hidden punishment.

## 7. Persistent venues, institutions, and actual bills

### 7.1 More than nightlife

Use a small physical taxonomy plus programming capabilities/events: bars/clubs, roadhouses/honky-tonks, theatres/supper clubs/hotels, dance halls/ballrooms/armories, coffeehouses, and community/campus/religious spaces. Churches, schools, house gatherings and rehearsal spaces can begin as lightweight institutions.

Trade meetings and auditions are **events**, not necessarily buildings. Church programs and coffeehouse hoots cannot depend on alcohol-spending interactions. Small acoustic rooms can have high prestige; capacity and prestige are separate.

Venues need stable ID, place/neighborhood, name, historical or fictional provenance, dates, capacity, acoustics/equipment, audiences, programming, booker/promoter ID, calendar, operating health, prestige by scene, and recurring engagements.

Relationships belong on `(observer/contact)` edges, not one `bookerRelationship` field on a room. Staff may change while venue identity survives.

### 7.2 Booking and performance

Create dated bookings before resolving visits: event/venue ID, time, act IDs, bill roles, fee/guarantee or door terms, expected audience, cancellation state. A residency is a recurring agreement, not one global artist slot.

Booking considers availability, travel, fee, repertoire, observed reliability, fit, contacts, draw, and support opportunities. Visitors, signed artists, amateurs and non-signing professionals can all be heard. Explain deal availability in UI instead of erasing the rest of musical life.

Generate deterministic rolling schedules; visits cannot change them. Resolve each performance once. Attendance, set, receipts, reputation and practice exposure are shared facts. Additional observations improve understanding, not the number of gigs or practice hours.

Feed real work to band-member development/fatigue through its existing owner. Reconcile synthetic work budgets so a night is not counted twice. Preserve documented baseline approximations for background activity.

Give rooms a weekly rhythm and supported seasonal patterns: recurring dance nights, amateur/hootenanny opportunities, matinees, campus terms, religious/community calendars and tourist seasons. Author named observances only where appropriate to the community and year. Repetition makes a room learnable; finite slots, cancellations and visiting bills keep it alive. The player can plan ahead instead of repeatedly pressing a discovery button.

### 7.3 Contextual audiences

Local draw is separate from national recognition and musicianship. Dancers, a coffeehouse, a church program and hotel supper guests value different things. Repertoire, arrangement, volume, instrumentation, professionalism and social connection affect response.

Crowd response is evidence, not an oracle for potential. Excellent musicians can fail in the wrong room; ordinary acts can serve an audience brilliantly.

## 8. Labels, rosters, and geographic recruitment

### 8.1 Reach is earned and explainable

Order: **access/discovery → perception → interest/offer → artist choice → atomic signing**. Geography belongs in access and practical deal terms, not just a small final score bonus.

A local label knows its catchment and nearby circuits. A regional indie maintains some agents/routes. A specialist may have dispersed contacts in its field. A major has offices, scouts, publishers, agents and national leads, but finite attention and imperfect judgment. Independent ownership is not synonymous with parochial reach; tier and network are separate axes.

Nonlocal opportunities carry provenance: tour appearance, referral, submitted demo, trade event, scout trip, relocation, office or licensed master. Cold inbound demos can be rare and costly to process; they cannot disguise a full-registry query.

Within an accessible candidate set, a possible smooth weight is:

```text
accessWeight = exp(-effectiveTravelHours / labelTravelScale)
             + documentedReferralWeight
             + officeOrCircuitWeight
```

Normalize/cap influence before sampling. Terms describe discovery probability, not skill or ownership overrides. Exclude nonexistent routes. A real distant referral need not be vetoed by region boundaries.

### 8.2 Opening world and recovery paths

Assign geography before new-game rosters; recruit through reach and specialty. Deduplicate candidates accumulated through genre queries. Use deterministic fair allocation or rounds so first-loaded labels cannot vacuum up hometown talent.

Replace unrestricted `NationalFreshRecovery` with explicit effort: connected circuits, referrals, demos, a paid trip, or a truly national network. Without a match, an empty slot is truthful. Log its cause; do not teleport talent to rescue a metric.

Regional labels need viable access, not identical signing rates. A Rockies label can sign a Midwestern act with a real connection. It should not routinely prefer disconnected national talent over suitable neighbors. A Deep South label is not equally local to Nashville, Miami, Memphis, and New Orleans.

### 8.3 Artist agency and contractual geography

Artists weigh money, trust, fit, treatment, recording access, distribution, travel, livelihood, and relocation support. Local attention and relationships can beat a major; major reach and money can win too. No hidden automatic victory.

Show hometown, current base, studio destination and label HQ independently. Group rosters by base with hometown visible and explain unusual connections. Exclusive artist contracts, distribution and licensed masters are different relationships. Distributing a regional record does not move its act or grant an exclusive contract.

Apply common access and ownership rules to player and AI. A player can deliberately travel farther than a local AI's normal budget; the trip explains the opportunity.

## 9. People, mobility, and remembered relationships

Extend band-member simulation, never shadow it. Recombination prefers compatible available people with geography, prior work, and credible relocation. A Miami bassist cannot instantly fill a Seattle rehearsal through a national pool.

Persist former bandmates, mentors, writing partners, support bills, house-band service, repeated sessions, booker trust and introductions. Allocate sparse edges when events occur, not a dense all-person graph.

Movement is a dated decision with reason, commitment, cost and cooldown: job, recording invitation, contact, inadequate work, family or artistic community. Heat cannot make everyone oscillate between cities. Migration changes base and participation, not origin; tours change presence only.

A city remembers alumni. Origin, development, recording and label cities have different claims on a success. Split a bounded influence budget rather than award four full hit bonuses.

## 10. Information, buzz, and player decisions

### 10.1 Rumors carry evidence and uncertainty

Record subject, source/contact, underlying event or belief, observation/receipt times, confidence, expiry, audience and verification. A speaker may misread a real situation. Do not invent nonexistent offers for pressure; model a mistaken belief separately from truth.

Bookers know draw/reliability; musicians know lineups; shops/jukebox operators see demand; DJs know requests/playlists; managers know intentions; trade papers give wider delayed coverage. Relationships affect candor, speed and access, not omniscience.

Offer visits, introductions, demo mail, phone calls, auditions, sponsored support slots, studio referrals and follow-up listening. Time, money, trust and opportunity compete. Drinks are one social action, not the ecosystem's universal currency.

Dossiers hold dated observations. A departure can invalidate last month's singing/arrangement assessment. Familiarity sharpens estimates of current performance, not hidden ceilings.

### 10.2 Bounded, temporary heat

Maintain subscene/genre heat and selected act/venue heat; city summary is a presentation aggregate. Distinguish local from national attention and signing from success.

```text
nextHeat = clamp(currentHeat × exp(-ln(2) × elapsedWeeks / halfLife)
                 + deduplicatedEventImpulses, 0, maximum)
```

Different channels may have different half-lives. Shows, studio openings, signings, arrivals and hits have evidence-scaled impulses. One modest contract is not a citywide price shock.

Use diminishing returns, delays and bounds for prices, competition, imitation, investment and migration. Never let one success multiply formation, talent, prices and chart appeal without independent constraints. Measure feedback strength.

### 10.3 Player surfaces

- Tonight/this week: named rooms, bills, known costs, travel feasibility, leads and appointments.
- People: recurring acts, contacts, former members, known contract status, hometown/base.
- Neighborhoods/circuits: recognizable communities and places; learn unknown details through play.
- Local news: consequential changes linked to an act, venue, contract or record.
- Notebook: dated observations, leads, commitments and remembered relationships.
- Label geography: reachable circuits, offices/referrals, roster places and unusual connections.

Returning to town produces a short account of change: a room closed, singer left, band earned a residency, rival signed an old lead. Keep significant history behind the recap.

All dialogue follows the band directive's Rolodex rule: reveal a real fact or belief, express a modeled motive, or execute an action. Example fictional chain: a drummer leaves, another group hires them, a booker changes the bill, a trusted contact tells the player where to hear them. Every sentence has events and IDs behind it.

## 11. Historical realism beyond genre weights

Represent date/place-specific venue access, discriminatory booking/lodging barriers, language communities, school/church networks, media, migration and changing audiences. The [Library of Congress Green Book collection](https://www.loc.gov/item/2016298176/) documents constraints that make frictionless universal travel untenable. They are not South-only and do not disappear instantly in 1964.

Keep barriers external to talent. Identity cannot determine ability, reliability, criminality or musical preference. Show agency, mutual aid, institutions and collaboration as well as exclusion. Avoid random tragedy as a substitute for depth. Existing band-life ownership governs draft, illness, death and lineups.

Localize shocks and their causes: redevelopment removes rooms/housing, campus terms change bookings, promoters open routes, policing affects events, stations change format. Do not turn civil unrest into a generic citywide genre debuff. Source specific assertions and dates before shipping content.

Stage features, but admit them in the data model from the start. A historically grounded release cannot represent every community with a generic bar and regional preference vector.

## 12. International origin without another market simulation

Make places country-aware now. Foreign-origin artists need real places, not American fallback hometowns. Initial UK source nodes can be the existing six names: London, Liverpool, Manchester, Birmingham, Glasgow and Bristol. They may begin as coarse source scenes without playable offices or foreign consumer markets.

Domestic British careers predate US exposure. Introductions happen through licensing, imports, agents, promoters and tours. The [Cavern Club's history](https://www.cavernclub.com/blog-post/cavernhistorymyths/) dates its jazz-club opening to 1957; a 1964 American-market gate is not its cultural birth date.

Keep foreign origin separate from compatible US commercial representation, with provenance. Overseas nodes do not join domestic road routes. Distribution tier cannot stand for immigration, licensing or popularity. Later source scenes can cover Jamaica, Canada and origins required by existing repertoire; reggae artists cannot all become Miami locals as a shortcut.

## 13. Ownership, update order, saves, determinism

### 13.1 Owners

| Owner | Authority |
|---|---|
| Geographic registry | Places, catchments, countries, coordinates, market mapping. |
| Authored catalog | Versioned priors, institutions, dates, provenance. |
| Artist/population and band-life | IDs, births, careers, lineups, terminal events, debt/credit. |
| Scene service | Participation, bookings, venues, contacts, local reputation, heat, event projection. |
| Roster/contract | Offers, holds, artist choice, signing/dropping, exclusive ownership. |
| Player knowledge | Observations, dossiers, rumors heard, read state. |

### 13.2 Update contract

Adapt to the scheduler, explicitly ordering: dated institutional changes → committed lifecycle/lineup changes and arrivals → budgeted formation/placement → valid bookings and performances → information/discovery → offers, choice and atomic signing → news/heat effects for subsequent opportunities.

Use stable snapshots where phases share candidates. Recheck location, availability and ownership at commit. Signing cannot retroactively change a played bill. Later lineup changes can amend/cancel future bookings with a logged cause.

Use keyed scene RNG namespaces and sorted stable IDs. No new `GD.Rand*` or population-stream draws for placement, bookers, rumors, visits or observation. Persist advancing sequence state if used, event ordinals and processed ticks. Never double-resolve a gig or heat impulse after loading.

### 13.3 Migration

Version scene content and saves. Choose the next version at implementation, since the ongoing branch may advance beyond v4.

Save mutable venues, participation, bookings, relocations, rumors/beliefs, observations, significant events, processed markers and RNG state/ordinals. Reference authoritative people/acts by ID. Rebuild indices after both world and player restoration. Preserve terminal tombstones needed by records, alumni and dossiers.

Audit the old purge's musician and unsigned-pool consequences explicitly: the inspected removal method deletes the artist-registry entry, so the conversion cannot assume it also reconciles every person or cache. Repair surviving references through existing ownership rules; do not indiscriminately delete pooled musicians who may now belong to another act.

1. Preserve credible explicit places. A prospect's observed `CityId` proves encounter location, not hometown.
2. Normalize legacy region names/IDs through one adapter.
3. Deterministically assign missing bases within legacy regions, conditioned on genre/cohort/institutions; stratify to avoid hub concentration.
4. Mark provenance/confidence. Unknown origin can remain unknown; an inferred base is not archival fact.
5. Preserve contracts, rosters, discography, people, holds and careers. Never rewrite origin to make a label look local.
6. Adopt surviving generated prospects by existing IDs; retire their cleanup ownership after conversion. Never resurrect deleted acts by matching invented names.
7. Seed venues/relationships once, record content version, and make migration idempotent.

Old roster anomalies remain legacy history; new games and recruitment obey new rules. Feature disabling mid-save cannot delete population: freeze dynamics or require a compatible mode explicitly. Never silently rerun initialization.

## 14. Performance and inspectability

All scenes advance independently of visits. Use weekly opportunities/heat, dated events and sparse changes. Lazy schedules must produce the same result for visited/unvisited scenes at the same seed/time.

Index by city, subscene and eligibility. Avoid all-artists-per-venue-per-day scans. Bound future calendars, retain significant history, compact routine bookings without breaking replay/dossier evidence. Keep edges sparse.

Expose developer explanations for placement, booking, nonlocal access, roster origin, rumors and population changes. Inspect surprising outcomes before adding multipliers.

## 15. Delivery sequence and risk boundaries

**Proposed future work only. Stop the current task after this directive and atlas.**

| Phase | Deliverable | Acceptance |
|---|---|---|
| 0 — Baseline/content contract | Re-audit band branch; enumerate creation/removal/signing paths; flag matrix, hashes, geographic census; source first-slice content. | No hidden factory/fallback omitted; evidence separated from hypotheses. |
| 1 — Identity, observe only | Places, origin/base, assignment, adapters, migration/save probes. | No economic readers switched; economic control unchanged. |
| 2 — Persistence | Membership, lifecycle hooks, replace factory/purge, adopt prospects, existing birth owner. | Visits never change births; signed/touring/terminal/notebook cases valid. |
| 3 — Rooms/recurring lives | Venues, bills, engagements, draw, contacts, observations; minimum differentiated profiles for all 31 cities. | Shared persistent world; no duplicate gigs/work. |
| 4 — Geographic rosters | Initialization, discovery/recovery, artist choice, relocation explanations. | Locality improves without hidden bailouts; economic gates pass. |
| 5 — Dynamic ecosystems | Pressure, moves, institutions, curves, bounded diffusion/feedback. | Observe first; conservation, chronology, concentration and economy pass. |
| 6 — Information/authorship | Rumors, recaps, rivalry, investments, deeper stories. | Causal state behind actions; no omniscience or reward farming. |
| 7 — Extended world | Detailed foreign source scenes, specialist institutions/circuits. | Truthful origins; explicit domestic sales/travel assumptions. |

International-compatible IDs and institution types begin in Phase 1. All playable cities get basic differentiated content by Phase 3; a few rich famous hubs are insufficient.

Suggested independent gates: observation, persistent world, geographic recruitment, dynamic feedback. Use repository naming conventions; reject contradictory flags and specify dependencies. All-off stays clean; observation cannot consume RNG or mutate candidates. Shared-registry changes need AI validation even when their UI is player-facing.

## 16. Verification and acceptance

### 16.1 Zero-tolerance invariants

- Refresh, notebook and travel UI never create/delete acts or reroll schedules.
- Artist, person-pool, roster, membership, alumni and record references reconcile by ID.
- One birth owner; debt/credits consumed once; migration conserves world headcount.
- Exclusive contracts have one owner; simultaneous bids/stale tips respect commit checks.
- Hometown stays fixed absent explicit correction; visits/recording trips do not change it.
- Distant small-label discoveries have access provenance; no unlogged national fallback.
- Bookings obey dates, feasible presence, venue availability and shared-musician conflicts.
- Historical venues/styles respect valid date ranges; uncertainties are marked.
- Save/replay reproduces scenes, bookings, rumors, population and outcomes.
- All-off/observe-only preserve the accepted control under the chosen baseline flags.

### 16.2 Diagnostics, not historical laws

Pre-register thresholds against the baseline before tuning. Provisional experience targets:

| Measure | Definition/use |
|---|---|
| Recurring cast | Share of unique acts seen this month also seen last month, at comparable city/subscene/effort. Evaluate 50–70%; separate cold starts/tours. |
| Discovery runway | Unique worthwhile leads under a fixed itinerary. Fifteen visits is a playability probe, not a promise of fresh talent per click. |
| Roster locality | HQ-to-base/origin travel distributions by reach/tier at start and annually: catchment, connected, distant explained, unexplained. Last category must be zero. |
| Opportunity | Reachable eligible talent, vacant-slot weeks, failures, advances and viable openings by city/genre. |
| Distinction | Blind comparisons of calendars, mixes, repertoire, institutions and news between cities in the same year. |
| Continuity | New Orleans jazz, early SF folk/jazz, Nashville non-country and Miami non-country survive regional consumer filters. |
| Era response | Changing opportunity without erasing traditions; separate origin/base/studio/HQ in chart attribution. |
| Concentration | City shares, diversity, migration, nationally successful acts; historical ranges where supported, not predetermined winners. |
| Stability | Heat, asks, bids, migration and entry after a controlled success shock; decay/stabilization without runaway prices/population. |
| Story integrity | Valid causal news, distinct chains, explained stale tips, suppression of repetition, visible consequences. |
| Cost | Registry, edges, calendar size, saves, tick time and memory across a decade. |

National Top 40 city shares cannot be the sole test. A studio city exports migrants' recordings; a vigorous jazz community may barely touch the pop chart. Validate cultural activity and industry roles too.

### 16.3 Test ladder

Fixed probes first: migration, ownership, dates, locality, replay. Matched 52/104-week runs diagnose early failures. Changes to shared artists, AI recruitment, development or releases then owe the band branch's established two-seed decade comparison. Re-establish accepted control/noise floor at implementation; do not copy obsolete scalar guardrails from old directives.

Report active acts, formations/exits, seeking/latent pools, reused musicians, label survival/vacancies/signings, singles/Albums, genres, finance and geography. Diagnose mechanism before compensating with cheaper advances, births, or universal access.

### 16.4 Required scenarios

1. Miami 1961/1968: distinct soul/R&B, hotel, youth and Cuban-community pathways; country possible but not defining.
2. New Orleans early/late decade: persistent discoverable jazz plus separate R&B/soul activity.
3. Nashville: country opportunity and Jefferson Street R&B; nearby cities do not automatically inherit them.
4. San Francisco 1961/1967: active earlier folk/jazz alongside later psychedelic opportunity.
5. Memphis indie: local/connected recruitment; distant touring/referral exception explained.
6. Billings/Albuquerque: working cast, feasible connections, viable opening, no infinite free pool.
7. Scout twice/save/travel/return: stable IDs, remembered rivals/lineups, no double ticks.
8. Breakup/recombination: preserved people, existing formation ledger, no teleporting members.
9. New versus legacy rosters: geographic initialization; old contracts preserved with honest provenance.
10. Foreign act recording in US: distinct origin, base, studio, rights holder and market projection.

## 17. Ambitious extensions worth designing for

- **Local record economies:** self-financed singles, consignments, local airplay, jukebox tests, leased masters. Integrate existing cash/distribution accounting.
- **House bands/session communities:** musicians bridge jobs; trust and repeated collaboration build recognizable records.
- **Institution building:** fund rehearsal space, sessions, showcases, songwriter nights or equipment loans; real capacity and costs.
- **Neighborhood geography:** different audiences, travel, rent and institutions; redevelopment/migration changes routes.
- **Tour exchanges:** reciprocal bills, agency packages, college networks, military-base/social-club dates, festivals with valid dates/access.
- **Repertoire transmission:** standards, local favorites and arrangements travel with performances/people; catalog retains publishing ownership.
- **Entrepreneurship:** booker becomes manager, DJ introduces producer, session players found studio, regional seller earns a license. Require resources/relationships.
- **Community memory:** alumni, former venues, homecomings, benefits, rivalries and mentoring lines across the decade.
- **Editorial disagreement:** contacts assess different rooms, lineups and repertoires; learn whose judgment helps which decision.
- **Influence history:** inspect how a sound emerged from actual people, records and institutions, including the player's contribution.

Prioritize stable identity, trustworthy geography, actual opportunities, remembered relationships and one population. The world should reward knowing a place and its people.
