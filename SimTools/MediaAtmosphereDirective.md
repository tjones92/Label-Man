# Media Atmosphere Directive — Television, Press, and Radio

**Design directive, October 15, 2026.** This specifies new work; it does not claim that the systems below have been implemented or calibrated. It includes the requested addition of peak television viewing times.

The label should live in a world where a Cleveland television producer needs a presentable act for Saturday, a fan editor wants an exclusive photograph, a trade reporter asks whether orders really justify the publicity, and a rock critic wants to know who actually made the record. Rival labels should inhabit that same world. Some of their acts should acquire screaming fans, recognizable faces, and stories that outlive a chart run.

Media gives existing success a public shape and gives the player decisions about access, presentation, relationships, and risk. It must fit inside the economy already tuned. Millions of viewers are exposure, not millions of record buyers; a publicity campaign does not manufacture purchasing power, pressing capacity, distribution, artistic merit, or another release.

## 0. Scope and governing decisions

1. Build named television programs, press outlets, editors/reporters, appearances, stories, and follow-through. Include local papers, specialist publications, and community radio alongside the national names.
2. Separate **exposure**, **audience response**, **industry confidence**, and **critical judgment**. Each has different readers. A hostile national story can make an act familiar while hurting its access to family television.
3. Television has two independent axes: **performance method** and **broadcast method**. Live music, live vocals over a track, and complete miming are different from live transmission, videotaping, editing, delayed feeds, and reruns.
4. Audience depends on the actual local broadcast time, date, clearance, competition, program identity, and the act's position in the running order. A good teen slot can be more useful for a 45 than a larger adult audience.
5. The label, manager, artist, booking agent, and outlet have distinct authority. Access negotiations never grant ownership of the story or blanket control over a person's private life.
6. Use the existing radio, trade, artist, song, recognition, calendar, and money systems. Do not implement a second promotion economy.
7. Ship atmosphere and AI characterization before enabling new market effects. Those effects require their own bounded adapter, paired runs, and explicit acceptance gates in this directive.
8. All numbers described as **initial design values** are tunable simulation priors, not measured historical probabilities or ratings.

## 1. Ground truth in this checkout

Verified against the current source, including the distinction between an older directive and what is now built:

| Existing seam | What it means for this work |
|---|---|
| `Data/MediaInfrastructure.cs`: `tvMarketRank`, `hasLocalMusicShow`, `bandstandReach` | Useful geographic priors. There is no demonstrated program schedule or booking engine behind these fields. Derive candidate local coverage from them; do not treat `bandstandReach` as a national television bonus. |
| `Systems/TradePress.cs`, `Systems/PlayerDesk.TradePress.cs` | Trade submissions, delayed review outcomes, and advertisements already exist. Their bounded benefits are aimed at business access, not direct consumer units. Extend their identity and reporting; preserve current benefits until a replacement is validated. |
| `Systems/PlayerDesk.TradePaper.cs` | Existing observational chart, breakout, and rival-signing news is the foundation for the morning media digest. Reading it must remain free of economic writes and global RNG draws. |
| `Systems/Radio/StationNetwork*`, `Deejay`, `StationAdvocacy`, `PayolaLedger`, `RecordServicing`, Rolodex scenes | Stations, programming decisions, advocacy, servicing, personal access, and payola already have causal machinery. An interview is not permission to bypass that machinery. |
| `AudienceSegment` and `SegmentCapacityModel` | Youth, Mainstream AM, Adult MOR, Urban R&B, Country, College/Folk, Underground FM, and other audiences already exist. Do not introduce a parallel teen purchasing population. |
| `SimulatedArtist.publicRecognition`, `culturalStanding`, `momentum`; `ArtistRecognitionService` | Recognition is implemented, including diminishing returns, decay, member attribution, and a launch profile. Inspect active switches at implementation time. Never assume the older celebrity directive still describes unbuilt work. |
| `Musician` | Stage presence, vocal/instrumental abilities, reliability, fatigue, substance load, personal recognition, creative reputation, and identity axes already exist. Read these for performances and interviews rather than rolling a new all-purpose charisma stat. |
| `SongComposition.contentTagIds`, `PolarSongTags.content`, master metadata | Storage for content exists. Authoring, aliases, evidence, and a consuming censorship policy still need auditing. The legacy classifier is not proof that content has been populated correctly. |
| `ManagerProfile` and `SimulatedArtist.artistCreativeControl` | Managers currently use static archetype modifiers, not autonomous weekly agents. Keep that discipline. Use archetypes as policy inputs; respect existing creative-control rights. |
| `ChartManager`: regional serviceable intents, format clearing, bounded spillover, completed-week settlement | The market already reconciles physical availability and regional purchasing capacity. New media effects must be attached to an audited seam here, not bolted onto final sales or chart positions. |
| `ExternalMediaService` / `ExternalMediaProfile` | These names already belong to film and stage soundtrack origination. Call the new subsystem `MediaAtmosphere`, not `ExternalMediaService`. |

Existing plans to cross-reference: `PromoMechanicDirective.md`, `RolodexDJDirective.md`, `CelebrityRecognitionDirective.md`, `BandMemberSimulationDirective.md`, `ContactNetworkDirective.md`, and `LocalSceneEcosystemDirective.md`. These supply context; the source and current calibrated baseline resolve stale implementation claims.

Implementation of this directive must preserve unrelated concurrent work and establish its own baseline from the actual starting tree.

## 2. The small amount of persistent state that earns its keep

Do not store a score merely because its name sounds useful. Every proposed stock below has a writer, reader, and expiry rule.

| State | Written by | Read by | Lifetime and boundary |
|---|---|---|---|
| Outlet/editor relationship | Kept appointments, useful accurate information, corrections, breached exclusives, broken production agreements | Connection, willingness to consider a pitch, terms offered | Slow decay; belongs to the contact and role. It cannot purchase a favorable review. |
| Coverage/exposure ledger | An actual airing, published issue, or radio segment | Recognition presentation, subsequent editorial interest, audience response | Event history with compact aggregates. A booking or unprinted interview is not exposure. |
| `teenAttachment` by act and serviced region | Relevant positive coverage plus independently observed listener response | Fan requests, teen-show interest, fan frenzy progression; bounded demand adapter only when enabled | `[0,1]`, decays without fresh response. Distinct from musical quality, genre, and canonical recognition. |
| `mediaCredibility` by audience family | Visible compromises, convincing performances, observed candor or deception | Critics' framing, artistic-audience reception, media choices | A small signed attitude, initially `0`, recovers toward neutral. No automatic write to `culturalStanding`, record quality, or contract price. |
| Public controversy episode | Published allegation, witnessed on-air incident, or public event supplied by an existing system | Outlet interest, network review, fan/parent reactions, crisis decisions | One attributed episode with severity, geography, verification, and expiry. Not a permanent bad-person stat. |
| Network/program reliability | Arrival, rehearsals, compliance with an agreed plan, technical preparation | Future booking and conditions at that outlet | Program first; a network connection matters only where the registry says it does. No automatic industry-wide blacklist. |
| Fan frenzy episode | A conjunction of independent response, repeated exposure, and momentum | Crowd scenes, cover competition, member spotlight, finite bookings | Has growth, saturation, plateau, and cooldown. No permanent sales multiplier. |

Initial decay values: teen attachment has an eight-week half-life without new response; audience credibility attitudes move 10% toward neutral each week; outlet relationships decay on a much slower calendar. Episode-specific memories can remain in history after their active effects end. These are starting values to test, not historical findings.

Store region-specific attachment sparsely. Individual fan agents, individual television households, and a daily simulation of every reporter are unnecessary.

**Observation mode boundary:** media-owned state may change, but it must have no indirect economic readers. In particular, do not copy it into canonical recognition, momentum, member strain, fatigue, reliability, critical acclaim, career state, artist evolution, signing evaluation, or live-booking demand. Those systems are already causal. The media UI can report a publicity disagreement without secretly changing band cohesion.

## 3. Television ecology and historical availability

The historical calendar controls institutions; simulated acts supply the guests. Do not script famous real artists into the player's world or force their controversies to recur.

### 3.1 Local teen dance and music shows

Generate inexpensive, comparatively approachable opportunities in cities that support them. A local producer wants a cleanly presented record, a reliable arrival, a short usable segment, and some evidence that local teenagers care. National chart status is helpful, not required.

The player loop is: discover the host or producer through a DJ, local paper, venue, or cold inquiry; send a suitable disc and short press sheet; offer an available date; settle performance requirements; attend rehearsal and the recording/broadcast. Booking should be moderately easy for a plausible act after those requirements are met. It should not require the label to become nationally successful first.

Low reach means the cleared local footprint, not a generic national percentage. A first appearance can produce a little mail, a photograph in the local entertainment pages, interested shops, or another booking inquiry. The important early-game result may be that the producer now knows the label can deliver.

Use fictional local programs where research is incomplete. Seed verified historical examples only inside verified dates and cities. Cleveland's **The Big 5 Show / Upbeat** is a particularly useful reference, but it should not remain trapped in the low-reach category: it began locally and developed syndication. [WEWS's own retrospective](https://www.news5cleveland.com/entertainment/rock-hall/wews-tvs-upbeat-brought-music-acts-to-cleveland-to-gain-popularity-across-country) describes a Saturday 5 p.m. local slot and tapes moving between markets over several weeks. Model this as staggered airings of one recorded appearance. Local dance-party programs and multi-act music showcases should also have different running orders.

### 3.2 National teen-oriented shows

| Program | In-decade availability | Editorial/booking character | Performance policy to represent |
|---|---|---|---|
| **American Bandstand** | Present throughout 1960–1969 | Danceable current singles, concise interviews, recognizability, teen taste; national booking is much harder than local television | Complete lip-syncing is a normal program requirement/default. Show it before acceptance. |
| **Shindig!** | 1964–early 1966 | Energetic rock/pop/R&B showcase with a strong ensemble-production identity | Support live vocals and prepared accompaniment, plus episode-specific arrangements. Do not mark every booking as compulsory full miming. |
| **Hullabaloo** | 1965–1966 | Highly produced youth-oriented network variety, hosts, dancers, medleys | Specify live/track/mime conditions per production. Camera and arrangement preparation matter. |

Registry date ranges must be precise enough to exclude canceled shows; a year-only `1966` flag is insufficient. Repeats do not create new guest bookings. The registry also needs **American Bandstand's** weekday-to-Saturday change and Philadelphia-to-Los Angeles production move, rather than using one schedule for the decade. [Paley documents the series and its early ABC broadcasts](https://www.paleycenter.org/collection/item?item=T86%3A0317); the detailed transition dates need period-listing verification during catalog implementation.

These appearances strongly favor teen attachment and 45-oriented responses. That is an audience emphasis, not a guaranteed lift to every teenager's opinion. A skilled serious act can gain younger listeners without losing its identity; a visibly uncomfortable act forced into a pin-up presentation can acquire an artistic-credibility objection. Avoid a universal penalty for appearing on popular television.

For production research, use the [Television Academy's Shindig! oral-history index](https://interviews.televisionacademy.com/shows/shindig) and [Hullabaloo production interviews](https://interviews.televisionacademy.com/shows/hullabaloo). The registry must attach evidence to its performance defaults rather than assuming all three programs used the same method.

### 3.3 Primetime variety

| Program | In-decade availability | Distinctive opportunity and tension |
|---|---|---|
| **The Ed Sullivan Show** | Throughout the decade | A major family audience, event appearances, broad household familiarity, strong production and standards authority. A program relationship can last for several bookings. |
| **The Hollywood Palace** | 1964–1969, continuing beyond the game's end | Broad variety bill, rotating hosts, polished presentation, substantial production requirements. |
| **The Smothers Brothers Comedy Hour** | February 1967–April 1969 | Large network exposure with a younger topical identity and conflict between creative producers and network standards. The producer's support does not guarantee the network clears the segment. |

The [Paley Hollywood Palace catalog](https://www.paleycenter.org/collection/item?item=T80%3A0233&p=1&q=petula) dates that series to 1964–1970. The [Television Academy's Smothers Brothers interviews](https://interviews.televisionacademy.com/shows/smothers-brothers-comedy-hour-the) establish its February 1967 premiere and provide production/censorship research leads.

Do not treat primetime variety as uniformly live transmission. The [Television Academy describes the Smothers Brothers' taped program and pre-broadcast censorship](https://www.televisionacademy.com/news/smothers-brothers). A taped segment can contain a real live musical performance, while still being subject to retakes, edits, or removal before viewers see it. The booking needs both facts.

Successful appearances can be career landmarks in the media history: the parents recognize the name, the interviewer no longer needs to explain the act, and major bookers take calls. Tens of millions are plausible for exceptional network audiences, but are not a flat default. The [Sullivan archive's estimate of 73 million for the Beatles' February 9, 1964 appearance](https://www.edsullivan.com/the-beatles-on-the-ed-sullivan-show-on-february-9-1964/) is an exceptional event reference, not the audience assigned to an unknown act on a routine Sunday.

### 3.4 Other useful television surfaces

Local variety and talk programs, regional country shows, gospel programs, public-affairs interviews, late-night talk, and festival/news footage provide routes for acts outside the teen-pop lane. Add named historical examples only after checking date and production policy. Do not give every genre equal access to every show; preserve plausible specialist paths rather than making national teen television the compulsory road to success.

## 4. Peak viewership, program time slots, and actual exposure

**The scheduled hour is part of the offer.** A program's reputation alone does not determine reach. Separate the audience available at that time from the program's ability to win it, and separate both from the audience that stays for the artist.

### 4.1 Schedule records

An effective-dated `BroadcastScheduleVersion` specifies:

- Program, season, first/last valid dates, original episodes and repeats, day of week, local start and duration.
- Origin production city and production time, with separate rehearsal, recording, feed, and local airing times.
- Network/affiliate or syndication carriage, local exceptions, delay rules, preemptions, and a market footprint.
- The performance's slot inside the episode: opening number, first featured segment, middle, closing number, brief interview, or multiple segments.
- Expected ratings range, demographic composition, evidence source, and confidence. Unsupported estimates stay visibly approximate.

The player usually negotiates an existing slot or waits for another date; they cannot move a network show to a more convenient hour. A headliner may negotiate running-order prominence. A newcomer may accept being last or risk losing the date.

### 4.2 Available audience and competition

Use 15-minute local-time buckets for exposure calculation. Integrate only the buckets overlapping the act's segments. The scheduling UI can still show ordinary clock times; there is no need for a minute-by-minute television management game.

For each market `m`, audience `s`, airing `a`, and bucket `t`:

```text
available[m,s,t] = population[m,s] * tvAccess[m,year] * viewingLikelihood[s,day,t,season]
viewers[a,m,s,t] = available[m,s,t] * clearance[a,m] * programShare[a,m,s,t]
attentiveExposure = integral(viewers during the act's segments * segmentRetention)
```

`programShare` is a share of the available viewers, not a household rating multiplied again by television-in-use. If source data supplies household ratings, convert through one explicit, documented path; never mix households, people, shares, and ratings as if they were interchangeable.

Keep **peak simultaneous viewers**, **average viewers during the act**, **viewer-minutes of attentive exposure**, and **estimated unique reach** as separate measures. The integral above produces viewer-minutes, not a count of different people. Estimate unique reach with bounded overlap between segments and airings; never report the sum of minute-bucket audiences as unique viewers. The peak is the maximum simultaneous audience in the relevant window, so a whole-episode peak reached before the act appears is useful context, not the act's credited audience.

Allocate shares over the small set of modeled competing programs plus a residual **other viewing** option. Shares sum to at most one per audience/time bucket. Include a **not watching** outcome in viewing likelihood. Most competing entertainment can be represented by a background competition profile; there is no requirement to simulate every western or sitcom.

Program share depends on its established attraction, audience fit, lead-in retention, rival strength, and modest preview publicity. Guest drawing power is a bounded substitution between programs, not permission to exceed the available audience. In the initial implementation, limit the guest-related share change to 10% of the program's baseline share. Do not use the same episode's eventual record sales to predict its own ratings.

### 4.3 Distinct viewing peaks

These are initial design curves to calibrate, not claimed historical ratings:

| Local viewing window | Expected audience emphasis | Booking implication |
|---|---|---|
| School-day afternoon after dismissal | High teen availability relative to adults, sensitive to local school hours | A low-total-audience appearance can be unusually efficient for a teen single. |
| Saturday afternoon / early evening | Youth leisure and dance-show viewing, with outdoor/sport competition | Strong local teen route; season and affiliate carriage matter. |
| Early family evening | Mixed households and younger viewers | Good broad introduction; parental approval and presentation matter. |
| Sunday primetime | Broad family co-viewing and established variety audiences | High potential exposure and strong network scrutiny. Sullivan's historic Sunday 8 p.m. slot is the reference. |
| Later evening / late night | More adult viewers; younger children less available | Interview/cultural recognition can exceed direct teen attachment. |

Do not hard-code a modern primetime definition onto 1960s schedules. Author each program's actual time slot and schedule changes. A show starting at 7:30 p.m. must remain possible; [Paley lists Hullabaloo at that hour on April 11, 1966](https://www.paleycenter.org/collection/item?item=T80%3A0076). School holidays change teen availability; summer can reduce evening television use and increase other leisure competition. Seasonal effects belong to viewing, not to an additional record-sales seasonality multiplier.

Major holiday specials, competing sports events, breaking news, and affiliate preemptions can change an episode's audience. Begin with authored sparse calendar events and a small seasonal curve. Ratings/sweeps pressure can make a producer seek a marquee guest or a safer act, but must not invent a booking on a week the show is off air.

### 4.4 Local times and delayed distribution

Eastern/Central/Mountain/Pacific markets require explicit carriage rules. A live East Coast feed does not imply an 8 p.m. local broadcast everywhere. A taped West Coast feed can be shown in a more favorable local slot; a live simultaneous feed can land earlier. Use historical schedule metadata, not the execution machine's current timezone or modern daylight-saving rules. The game's `GameDate` needs a media clock adapter, not a replacement calendar.

For syndication, the same tape may arrive in different cities on different dates and at different local hours. That produces a rolling pattern of mail and local attention. It does not multiply the national population by the number of airings. Unreceived or preempted markets receive no exposure. Show the player a regional window when a precise schedule is unknown.

### 4.5 Segment peaks, repetition, and uncertainty

An opening number benefits from the lead-in; a closing number can lose viewers to bedtime or switching. A mid-show act can inherit a peak created by an earlier attraction. Model retention and guest overlap, not a universal rule that the first or last slot is best. Strong advertised guests can attract viewers for a later segment, but the increase must come from available viewers or competing programs.

Two songs on the same episode do not mean twice as many unique people. Maintain bounded estimated unique reach plus repeat impressions. Repeats deliver reduced novelty and potentially new viewers. They do not replay the full recognition event, recreate a censorship incident, pay an original appearance fee again, or consume a new booking slot.

Before accepting, display **expected reach range**, main audiences, local-time coverage, performance method, and risk. After airing, provide an estimated audience and delayed response. The label should not know exact ratings, fan conversion, and press sentiment before the broadcast. Booked, recorded, cleared, transmitted, and reported are different states.

## 5. Booking, preparation, and costs

### 5.1 One opportunity with real stages

```text
lead → inquiry/pitch → producer consideration → tentative hold → terms/clearance
     → confirmed → rehearsal/recording → aired → response/follow-up → archived
```

Branches include declined, lost to another act, canceled, preempted, cut from tape, no-show, and rescheduled. A tentative hold is not a confirmed invitation. Store who canceled and why; a network preemption should not damage the act's reliability.

Every opportunity identifies the act, record/version, program, producer/booker, city, dates, required materials, backing method, duration, standards issues, rights, costs, deadline, and exclusivity. Multiple dates are reserved against the same finite calendar. Physical travel and rehearsal must be possible. A band cannot record television in Los Angeles while playing in Cleveland that afternoon.

### 5.2 What bookers consider

Use separate eligibility checks and a weighted consideration score. Eligibility includes active program/date, usable material, achievable travel, any age/presentation restrictions, availability, rights, and minimum technical readiness. Consideration reads:

- Local listener evidence and regional fit for local shows; broader response for national shows.
- Camera presence, usable arrangement, current story, and performance readiness.
- Outlet relationship and a track record of reliable delivery.
- Bill balance: genre, energy, audience, established headliner versus discovery slot, and repeated appearances.
- Actual campaign timing, advertiser/network requirements, and competition for limited slots.

Keep a discovery allocation in program policy so newcomers can get considered. It is an editorial route for eligible unknowns, not a guaranteed outcome or a reserved slot for the player. Large labels obtain access and package deals more easily, but cannot turn one show into an unlimited roster advertisement.

### 5.3 Rehearsal decisions

The player can approve a shortened arrangement, schedule a band rehearsal, hire an arranger or necessary backing musicians, practice camera marks, run the approved lyric, or decline conditions. Use existing people/skill/availability and travel systems. Studio efficiency can inform preparation, but it must not substitute for live vocal ability.

Extra rehearsal has diminishing returns. It reduces avoidable errors, not all nerves, illness, equipment trouble, or deliberate defiance. Offer a manager's recommended preparation with an itemized cost and a reason. Routine dates can be delegated under a standing policy.

### 5.4 Money and authority

The label may pay travel, hotel, promotional photographs, preparation, arranger costs, or production support it has actually agreed to fund. A guest appearance is not ordinarily purchased as advertising inventory. Treat a paid commercial or sponsored segment as its own disclosed product, never as an editorial booking bought with a hidden fee.

Performance fees belong to the contracted payee, commonly the performers. Route performer income through the existing musician payment/wealth seam; do not put all television receipts into label cash or call them record sales. Rights to a master, a backing track, publishing, performance, photography, and future reuse are separate. An edit for television does not rewrite the commercial master or publishing ownership. Use contract-specific statements where known; otherwise mark terms as simplified simulation assumptions.

In the initial causal phase, newly introduced actual appearance-fee payments are player-side only. AI fee terms remain estimates without a new receipt or wealth write; activating additional AI performer income requires a separate accepted accounting experiment. This keeps the fee design consistent with §12.4 rather than quietly adding money to the musician economy.

For AI, all label-funded media costs are allocations of its existing promotion envelope (§12). No new recurring publicist salary or automatic travel bill may be added to every AI label. Optional publicist services for the player need a concrete work package and existing cash/time accounting.

## 6. Performance results: a great record is not a great television act

Resolve the musical result and the production result separately.

| Method | Musical risk | Presentation/production risk |
|---|---|---|
| Full live band and vocal | Vocal control/power, instrumental ability, arrangement difficulty, ensemble readiness | Nerves, balance, timing, camera marks, equipment, crowd noise |
| Live vocal over backing track | Exposed lead vocal, cues, track/vocal coordination | Timing, wrong playback version, camera work, inability to extend the arrangement |
| House orchestra / augmented band | Rehearsal, chart readability, unfamiliar accompaniment, vocal confidence | Tight timings, production cues, limited rehearsal |
| Full mime/lip-sync | The prerecorded master supplies the sound | Obvious failed mime, wrong track speed, camera embarrassment, refusal to cooperate |

Prepared, skilled acts usually deliver competent shows. Sloppy or under-rehearsed acts have a meaningful bad-show tail. Avoid a coin flip where a professional is as likely to fail as an amateur. Use a bounded production draw keyed to the booking, performance skills and preparation, with equipment accidents separate from artist reliability.

Read the members who perform the parts. A strong guitarist does not rescue a weak live singer by raising the band average. A studio-built arrangement may need adaptation; a simple strong song can work beautifully with a house band. Do not change the underlying master quality when the television performance is poor.

Results range from flat but adequate to engaging, electrifying, audibly weak, or production breakdown. Different audiences respond differently. Screaming fans can overwhelm the sound while the visual event succeeds. A mime error can be amusing, embarrassing, or reputation-confirming depending on expectations and the act's response.

For taped shows, retakes require available time and producer cooperation. The producer/network can cut a segment. A live transmitted mistake is public; a rehearsal mistake is not automatically a national scandal. Reputation effects identify whether the problem was the act, the label's preparation, or the production team.

## 7. Censorship, content, and public controversy

### 7.1 The distinction that makes the tags usable

Separate **what a song contains**, **how a censor interprets it**, **what someone did**, and **what the public knows**.

A drug-reference tag can prompt a lyric query; it does not prove the singer takes drugs. A musician's private `substanceLoad` is not available to a reporter, network, or fan editor without an observable event. Conversely, an innocent lyric can be challenged because of innuendo or an anxious standards department.

Audit and normalize content identifiers before adding consumers. Support explicit language, sexual suggestion, drug reference, religious provocation, political/protest content, anti-war content, and other already-authored categories without inferring them from genre, hair, name, or identity. Unknown stays unknown. A master/performance variant can omit an objectionable line while the composition and original recording retain their content metadata.

### 7.2 Standards reviews are outlet-specific

A `StandardsQuery` records the exact issue, interpreting authority, requested change, deadline, and consequence. Policy depends on program, network, sponsor, year, region/affiliate, and broadcast context. The same material can be acceptable on a specialist radio program and refused on a family variety show.

Historical examples establish the possible choices, not a mandatory repeating script. The [Sullivan archive records the Rolling Stones changing the lyric on January 15, 1967](https://www.edsullivan.com/timeline/the-rolling-stones-are-censored/). [The Doors' official account describes the instruction not to sing “higher”](https://thedoors.com/news/the-doors-on-the-ed-sullivan-show). Use these to support an agreed-edit versus on-air-defiance distinction. Do not make every ambiguous word trigger a ban.

### 7.3 Player choices and differentiated consequences

| Response | Immediate consequence | Relationship / credibility consequence |
|---|---|---|
| Agree to a lyric or presentation change | Keeps the proposed booking if other terms pass | Improves program/network confidence in delivery; visible compromises may disappoint artistic audiences and the artist |
| Offer another song or a negotiated edit | May satisfy standards while preserving the original work elsewhere | Can demonstrate professionalism without the full artistic cost of a conspicuous surrender |
| Ask the producer to defend the original | Time, uncertainty, possible higher-level review | Producer support and network clearance can diverge |
| Refuse before confirming / withdraw honestly | Loses that opportunity and sunk preparation where applicable | Smaller relationship cost than breaking an agreement; may preserve artistic credibility |
| Promise compliance, then defy on air | Only works if the method actually permits it; a live mic can, a fixed mime track cannot | Significant breach at that program, possible network review and future exclusion; artistic response varies |
| Accept removal or cancellation | Little or no exposure from the affected segment | Story interest depends on whether the dispute becomes public |

Complying should normally help future access with the authority that demanded the change. Artistic credibility costs require a relevant audience to see or learn about the compromise; unreported rehearsal negotiations are not public events. Principled refusal can earn respect, but an incoherent or unconvincing stunt does not guarantee it. Neither compliance nor defiance is the universal optimal strategy.

Artists have agency. With artist creative control, the label can recommend and negotiate, not force a changed song. Otherwise, label control still does not guarantee a particular person will obey on a live microphone. Existing disposition, identity, temperament, and stated boundaries inform consent, with unknown preferences revealed through conversation. In the first live media phase, the consequence is a media decision and a remembered disagreement; broader band-life writes need separate validation.

### 7.4 Scandals, boycotts, and recovery

Existing drug busts, public statements, witnessed disruptions, and other public events can seed one controversy episode. Journalists may discover a real inconsistency through sourced work. Never invent a drug bust because an act needs a colorful press cycle.

Model stages: local report → wider pickup → institutional reaction → response → correction/aftermath. Mainstream amplification requires prominence, a striking verifiable story, or a wider social issue. A small local allegation does not immediately reach every household.

A boycott is a collection of actual actor decisions: an affiliate declines a segment, a station refuses a title, a retailer declines to stock it, an organization calls for action. Distinguish a call for a boycott from observed compliance. Scope it by market, title/act, affected institution, and end/review date. Some listeners may become more interested; that response does not cancel the logistical loss by definition.

Start with media access and bounded audience effects. Any new station or retailer refusal that changes serviceable supply is an additional causal adapter behind its own gate; it cannot silently bypass §12's limits. “National boycott” can describe a widely reported campaign while its measured economic effect remains explicitly scoped. Catastrophic permanent losses are outside the initial economy-preserving release, though the incident and the difficult label decision belong in the world.

Responses include a factual correction, interview, apology, defense, clarification, refusal to discuss private matters, changing upcoming promotional material, or waiting. They affect trust, story framing, and future access according to the evidence. Publicity does not erase a genuine event or guarantee forgiveness.

## 8. Press ecology: different readers, different consequences

Each outlet has a finite issue/page budget, lead times, geographic footprint, audience mix, editorial interests, advertising office, and contacts. A mailing is a submission; an interview is reporting; a feature is the editor's publication decision.

| Family | Examples | What earns interest | Principal consequence |
|---|---|---|---|
| Trades | **Billboard, Cash Box, Record World** | Release plans, orders, regional action, distribution arrangements, credible industry developments | Business confidence and contacts: jukebox operators, retail buyers, distributors, and program directors |
| Teen fan magazines | **16, Tiger Beat** | Reader requests, attractive personalities, access, photographs, a new idol or group story | Teen familiarity, attachment, member recognition, cover competition, manufactured availability pressure |
| Mainstream news/culture | **Life, Look, Time, Newsweek** | A cultural phenomenon, youth movement, extraordinary success, scandal, striking images or broader issue | Broad household recognition, parental reaction, legitimacy or moral alarm |
| Emerging rock criticism | **Crawdaddy!, Rolling Stone**, relevant music coverage in **The Village Voice** | Compelling records, scenes, craft, interviews, artistic change, cultural arguments | Critical framing and access to College/Folk/Underground audiences; initially limited mass reach |
| Local general press | City dailies, entertainment columns, local photo desks | Hometown achievement, a nearby show, a local recording business, a useful human story | Affordable local visibility and recognizable people; often the first earned clipping |
| Specialist/community press | Black newspapers, R&B/soul, country, gospel/church, jazz, college, regional and language-specific publications | Community relevance, craft, touring and local connection | Strong audience-specific influence and credible specialist routes |

Do not treat specialist/community media as a lesser universal publicity tier. An outlet can be highly influential inside its audience without being read by the whole country. Historical exclusion belongs in specific institutional policies and the existing integration model, not a new blanket penalty repeatedly applied to minority acts.

### 8.1 Trades: business evidence, editorial desks, and advertising

Give the existing submission and ad systems named desks. Keep review, advertisement, chart/reporting function, and business reporting separate even inside one publication.

Pitch a review copy; provide a verifiable regional breakout sheet; announce a signing or distribution deal; answer a reporter's questions about returns; place a paid ad. A trade pick can help obtain a meeting or consideration. It does not create consumer awareness directly. A chart entry must use the existing chart/reporting machinery; a friendly reporter cannot assign a fabricated position.

Billboard can emphasize breadth of industry/radio reporting, Cash Box operator/retail-facing context, and Record World competing reviews and trade coverage. These are initial editorial emphases, not exclusive readership claims. Verify named columns and review labels before reproducing them as exact historical branding. Use **Music Vendor** before its 1964 change to **Record World**; [a contemporary September 1964 issue still prints the former name on its masthead](https://www.worldradiohistory.com/Archive-All-Music/Record-World/60s/64/RW-1964-09-26.pdf).

Migration must preserve the value and expiry of old anonymous trade submissions. Adding three named desks must not turn one existing submission into three independent full-strength bonuses. Keep a shared B2B ceiling per title/window and deduplicate the same underlying order evidence. Advertising may signal commitment and make a salesperson receptive; it cannot buy an editorial verdict.

### 8.2 Fan magazines: access, intimacy, and ownership of image

Build recurring editors who know which faces generate mail. Features can be a pin-up, short Q&A, favorite-things column, cover, backstage visit, exclusive photo session, reader contest, or carefully framed personal story. A group can become familiar member by member. The lead singer is not automatically the only person fans notice.

The negotiation is shared work. The label's publicity office coordinates records, campaign timing, press assets, and access it can legitimately arrange. The manager/agent coordinates the artist's diary, personal presentation, and permissions. The artist controls participation in the proposed personal story; photography/reuse terms identify their actual rights holder. The editor decides what to commission and publish. Small labels may do the publicity work themselves. There is no historically universal rule that either label or manager alone owns this domain. As one limited first-person example, a [Lorin Rowan oral history](https://ppolinks.com/mvpl39241/2016_097_001_RowanLorin_OralHistoryTranscript.pdf) describes management seeking fan-magazine promotion; it is evidence of a route, not a universal contract arrangement.

Initial negotiable terms: date/location; exclusive versus supplied photograph; image reuse rights; embargo; a limited access window; how many members attend; topics the artist agrees to discuss; and whether the editor is offering a feature, merely considering one, or guaranteeing paid placement. An exclusive has a named subject, outlet, territory, and expiry. It is not perpetual control of all photographs or interviews.

“Romantic availability” is a publicity choice. Options include keeping private life private, a dreamy noncommittal interview, acknowledging an existing relationship, or actively planting a fictional romance rumor. A planted story is stored as a publicity claim with its origin and consent, never as a real romance in band-life data. A later contradiction can harm trust and upset participants. Do not assume every reader wants the same fantasy or that every artist accepts the policy. Covers can succeed on humor, style, musicianship, or group chemistry too.

`16` is available at the start; `Tiger Beat` opens in September 1965, supported by [Northwestern's catalog of its first issue](https://findingaids.library.northwestern.edu/repositories/6/archival_objects/109423). No Tiger Beat pitch in 1960. Print lead times mean a campaign can peak before the feature reaches the stands.

### 8.3 Mainstream magazines: a reason to cover popular music

These editors do not accept an ordinary release announcement as a national cover story. Coverage needs a threshold of cultural salience, a distinctive documented event, or a larger story in which the act is an example. Many features concern youth, religion, race, commerce, or changing manners rather than a review of the single.

A Life/Look-style photo essay may require access across several days; a Time/Newsweek-style piece may require industry interviews and independent sources. The same event can produce admiring reporting, curiosity, or alarm without a simple good/bad coverage roll. A skeptical journalist may quote the label's claim and then test it against buyers, promoters, or musicians.

An act can become a household name while parents dislike it. Keep familiarity, affection, and institutional acceptance separate. Editors can return months later to examine whether the sensation lasted. Positive coverage is not a subsidy; negative coverage is not automatically a national sales collapse.

### 8.4 Emerging rock journalism

Enable **Crawdaddy!** in 1966 and **Rolling Stone** in November 1967. Early readership is small and influential; neither begins with its later national authority. [The Rock Hall's media guide dates Crawdaddy!'s beginnings to 1966](https://library.rockhall.com/c.php?g=916705&p=6606422), and [PBS reproduces the November 9, 1967 Rolling Stone first-issue reference](https://www.pbs.org/opb/thesixties/topics/culture/newsmakers_4.html).

**The Village Voice** already exists before this wave: [its own history dates its founding to 1955](https://www.villagevoice.com/about/). Evolve its music coverage; do not spawn the publication in 1966. Include college papers and local underground publications as the surrounding ecology grows.

Critics respond to the record, performance, ideas, scene context, and evidence of craft. They can be wrong, partisan, surprised, or divided. Do not equate artistic credibility with a single genre or guarantee that a technically elaborate album receives praise. A critic may admire a supposedly manufactured act's record and dislike a self-consciously serious act's work.

Separate critical judgments from launch awareness and from the established cultural-merit pipeline. Initially, publish attributable opinions and change media attitudes. Any later write into `ArtistCriticalAcclaimService`, cultural memory, or evolution pressure must replace an identified input, avoid counting the same review twice, and undergo a separate causal test. A paid advertisement cannot mint a landmark.

## 9. How journalists interact with the label

### 9.1 People with beats and memories

Contacts need name, outlet, role/beat, city, preferred communication, interests, deadline cadence, relationship history, and a short voice profile. Distinguish an assignment editor, reporter, reviewer, photographer, trade advertising representative, and television booker. One person may hold multiple roles at a small outlet, but authority follows the role.

Use the existing contact-network substrate where its semantics fit. Do not stretch a DJ-only card into a universal contact that inherits station influence or payola. Introductions can reveal the right person; they do not guarantee coverage. Maintain outlet-level institutional memory separately from the personal relationship, so a reporter changing jobs does not take the network's blacklist or every editor's goodwill with them.

### 9.2 Concrete label-facing verbs

- Send a press sheet and review copy, with real inventory/postage/time where applicable.
- Pitch a specific angle to the appropriate desk, with source evidence and an embargo if needed.
- Respond to an incoming inquiry, confirm facts, provide records, decline a question, or arrange an interview.
- Offer a studio visit, rehearsal access, backstage time, a photo session, or a specialist introduction.
- Negotiate an exclusive, protect an artist's agreed boundary, or decline conditions.
- Correct a factual error, challenge an unsupported allegation, or supply a statement before deadline.
- Buy labeled advertising through the advertising office, using the existing trade-ad seam where appropriate.

“Tell them we're the next Beatles” is a claim, not evidence. An honest modest story can be useful; inflated orders, fake crowd figures, and invented exclusives can later be checked. Do not require a fictional criminal investigation to make ordinary publicity difficult.

### 9.3 Reporting is a process

```text
assignment/lead → sources contacted → material gathered → fact query
                → editor decision → issue closed → published → response/correction
```

A reporter may request the act, manager, producer, songwriter, a shop buyer, an operator, a DJ, or a rival's comment where those people have relevant knowledge. The label can facilitate access, offer attributable information, or refuse. It cannot know all other sources or rewrite the final piece.

Use a compact evidence record: source actor, observable fact/claim, date, firsthand versus hearsay, confidence, public/private status, and who may know it. A confidential band-room exchange is not available to the press merely because it exists in the save. A direct quote must come from an actual dialogue outcome or authored scene; generated prose cannot invent an admission.

On/off-record and embargo terms must be mutually agreed before information is supplied, not retroactively toggled after an answer. A respected boundary can build trust; a publication may decline conditions. Fact-checking gives a chance to correct facts, not approval of the opinion or story. Refusing an interview may yield a shorter story or a “declined to comment” line rather than automatically suppressing it.

### 9.4 Publication and feedback

Decide whether the story has enough evidence and interest, then compete for finite page space. Delay or kill stories that are weak or displaced by bigger events. Publish structured claims first, compose the headline/body second. The text must reflect the outcome; it must not become a second simulation engine.

After publication, a correction can reduce an inaccurate claim's active spread without erasing the original event from history. Syndicated copies and follow-up articles retain a common story root. Ten papers repeating one quote are ten exposures to one story, not ten independent scandals or ten recognition awards.

Coverage can include the label itself: the tiny shop that found a regional record, a reliable specialist house, the owner exaggerating orders, a rushed delivery, a publishing dispute, or a respected producer's working methods. A useful label reputation is audience/role-specific. A trade reporter and a teen editor are asking different questions about the same company.

## 10. Teen appeal and emergent fan frenzies

Give rival acts their own escalating public presence, using facts from their careers. Avoid a random weekly “Beatlemania” event that awards a preselected act free demand.

### 10.1 Attachment is earned in more than one way

Teen attachment reads youthful audience fit, memorable songs, visual/personality exposure, member distinctiveness, repeat positive encounters, and evidence that listeners are responding. Use current member identity/stage presence and audience evidence; do not invent a universal beauty score or impose a single gender template. A girl group, mixed group, solo singer, or instrumental band can each acquire passionate fans through different stories.

One appearance creates curiosity. Repeated appearances without a compelling record can create familiarity without attachment. A new hit can create demand before the public knows the members' names. Existing chart sales, fan mail, radio requests, and local audience response have different origins; record provenance so coverage of a hit is not treated as independent proof of another hit.

For an event's positive deposit, use diminishing returns:

```text
attachment += eligibleGain * (1 - attachment)
```

Scale `eligibleGain` by audience fit, actual novel exposure, outcome, and already-observed listener response. Weekly saturation and the common economic envelope still apply. Output cannot depend on how many times a UI panel is opened.

### 10.2 Episode states

| State | Required evidence | Visible world response |
|---|---|---|
| Local excitement | Positive local response plus a relevant airing, show, or feature | Requests to a named station, fans at a local appearance, first unsolicited letters |
| Regional teen favorite | Sustained response in more than one independent market/source | Local television invitations, fan-column repetition, photo requests |
| National teen sensation | Multi-region response plus national exposure and repeat interest | Competing fan-mag covers, recognizable members, more difficult access logistics |
| Cultural phenomenon | Sustained extraordinary response and at least two independent evidence families, including one audience-behavior source | Mainstream assignment, crowd footage, household discussion, institutional anxiety |
| Plateau / backlash / cooldown | Novelty exhaustion, weaker new response, overexposure, or contradictory image | Editors seek a new angle; fans divide or move on; some core attachment remains |

Transitions require persistence, not one threshold crossing. Initial values: evaluate over four completed weeks; require two completed qualifying weeks to enter national sensation; require four to enter cultural phenomenon; use a minimum eight-week episode cooldown after exit. Tune the actual evidence thresholds from observed distributions. There is no quota that must be filled and no guarantee every decade contains a phenomenon.

An act can remain a beloved regional favorite without reaching the national state. A phenomenon can coexist with strong adult or artistic appeal; teen success does not make an act permanently shallow. Fame can shift among group members. Store which faces received the exposure so a drummer's surprising pin-up popularity can appear in the next interview.

### 10.3 Rare stories without an economic explosion

The episode determines how existing results are reported and which finite media opportunities become available. Rival crowds, fan-club mail, imitators' hairstyles, and a reporter asking whether adults understand the new music add color without awarding cash or records. Descriptions must be proportional to the underlying evidence: do not call an act a national sales phenomenon when it is a cult favorite.

Positive feedback is allowed inside media: coverage attracts attention, attention makes an editor interested, another appearance grows recognition of the faces. Its market reader remains capped. Never let fan frenzy directly change release counts, the AI signing population, quality, market size, pressing, campaign scale, or every subsequent album's penetration.

In causal mode, a frenzy can help a suitable existing 45 win a small amount of demand from competing titles. The rare-event ceiling is larger than the routine ceiling but shares the same total pool (§12). This can influence a close race and make a successful rival feel distinctive. It cannot turn poor material with no distribution into a guaranteed national hit.

## 11. Radio additions: fill gaps around the real stations

Audit each item against the existing implementation before adding it. These are extensions or new presentation, not authorization to replace playlist scoring.

| Surface | Concrete decision | Existing authority / constraint |
|---|---|---|
| Artist interview or station visit | Send the artist, arrange a usable interview, honor an arrival time | Separate from pitching the disc; a host invitation does not force the music director to add it |
| Record premiere / short exclusivity | Offer a finite first-play window and a correctly serviced master/version | Bind to a real station and airing; no indefinite suppression of every competing station |
| Listener requests and dedications | Observe genuine local response; decide whether to support a promotion | Signals sourced from audience response. Manufactured requests are claims with detection risk, not free airplay |
| Station-sponsored hop / remote / contest | Coordinate artist, venue, host, prizes, and actual available time | Reuse existing hop, station advocacy, inventory, and live-event seams; do not pay twice for the same audience |
| Aircheck and follow-up | Hear whether a visit or pitch produced a spin, playlist consideration, or nothing | Real station log/playlists remain authoritative; distinguish a single spin from rotation |
| Program/music director handoff | Ask the person who can actually make the decision | Reuse `RadioEra` and local station policy for changing DJ autonomy; no decade-wide instantaneous switch |
| Specialist sessions and interviews | Offer material that fits R&B, country, gospel, jazz, college, or emerging FM programs | Real format, regional eligibility, and era viability; small reach can mean high credibility |
| Length / lyric / sound edits | Supply an approved shorter or clean radio version | Version metadata and artist rights; do not change composition/master truth or silently replace the commercial release |
| Press-to-radio inquiry | Service a title a programmer has read about and asks to hear | Access/consideration only. Editorial coverage is not a second guaranteed playlist add |
| Station refusal / controversy | Negotiate an acceptable version, answer concerns, or accept a refusal | Only through the gated station adapter; never a blanket national `radioHeat = 0` |

Do not create another payola ledger. Cash, gifts, quid-pro-quo promotion, and associated scandal exposure must route through the existing system where modeled. Legitimate advertising, an editorial interview, an appearance fee, and covert payment are different transactions.

Time of day matters here too, at a smaller initial scope. An overnight specialist interview and afternoon teen-oriented segment have different audience profiles. Use station/daypart metadata for the segment's exposure; leave the calibrated spin/rotation aggregation alone until a replacement is tested. No online streaming, social-media virality, instant audience analytics, or anachronistic later-decade FM dominance.

## 12. Economic contract: color first, bounded causality second

### 12.1 Three explicit modes

| Mode | New behavior | Economic guarantee |
|---|---|---|
| Disabled | No media work or reads except compatibility loading | Existing behavior and RNG sequence preserved |
| Observe / atmosphere | Schedule, assignments, performances, stories, relationships, AI media choices, fan episodes | All economic inputs and outcomes equal the control; no spending, canonical trait writes, or availability changes |
| Causal | Player actions have their stated costs; AI allocation and selected bounded adapters enabled | Small measured differences within the preregistered limits below; no claim of identity |

Player choices in causal mode can consume real time and cash. That is part of playing the label. Observe mode must clearly identify simulated costs as estimates and must not consume the player's diary or wallet. Do not expose a free, effective action under a diagnostic switch by accident.

### 12.2 AI spends the promotion money it already had

Before adding any new debit, establish an `AiMediaAllocation` view of the existing record/campaign promotion envelope. The existing charge stays the accounting authority. Television preparation, press servicing, photos, travel support, and retained reserve are allocations **within** that charged/planned amount.

```text
radio allocation + TV allocation + press allocation + other promotion + reserve
    = existing authorized promotion envelope
```

Each allocation reserves resources once; settlement cannot debit them again. An AI label too poor for the travel/preparation cannot book that plan simply because the top-level campaign charge was already posted. It selects cheaper feasible activity. No new free credit, grant, signing, or duplicate campaign is allowed.

Initial AI media dates must fit spare promotional availability. They cannot block or reschedule an already-planned recording, release, or paid live engagement. A conflicting offer is declined or moved. Reservations affect competing media plans only until broader calendar effects have their own accepted test; the player can still choose to rearrange their own diary in causal mode.

The initial adapter leaves `ApplyReleasePromotion`, album promotion snapshots, and their existing campaign outputs unchanged. It does not add paid TV/press output on top of them. Concrete media activities explain the promotion already being performed; only the small reallocation below can change its audience distribution. If a later phase decomposes the launch formula into channels, that is a separately validated replacement with a frozen total, not an additive feature in this directive.

### 12.3 One consumer-demand adapter with conserved totals

All television exposure, fan-mag attachment, mainstream reactions, routine radio-interview response, and controversy sentiment submit requests to **one** weekly ledger. They cannot each write awareness, radio heat, sales, or recognition separately.

The verified initial seam is `ChartManager`'s singles `MarketIntent` list **after** serviceable demand and physical limits are known, **before** format budgets and proportional local clearing are calculated. Keep the control intent for every title. Do not claim that the current code exposes a separate segment-level market-clearing API; it does not follow merely from having an `AudienceSegment` enum.

For region `r` and title `i`, let `b[i]` be the existing nonnegative serviceable singles intent and `bPrime[i]` the media-adjusted intent. Initial hard limits:

```text
sum(bPrime) = sum(b)                       // same regional singles intent total
0 <= bPrime[i] <= physicalLimit[i]         // existing inventory/capacity remain authoritative
abs(bPrime[i] - b[i]) <= 0.05 * b[i]       // routine title/week ceiling
abs(bPrime[i] - b[i]) <= 0.10 * b[i]       // rare qualified frenzy/major controversy ceiling
0.5 * sum(abs(bPrime - b)) <= 0.01 * sum(b) // at most 1% of that region's intent is transferred
```

The 10% ceiling **replaces** the 5% ceiling for a qualifying title; it does not stack with it. These are maximum requests, not promised sales lifts. A title with zero baseline serviceable demand stays at zero in this first adapter. Awareness and inquiries can still appear narratively; real servicing/distribution must establish a viable title before the adapter can help it. A stock-bound title cannot receive demand it cannot serve.

Implement transfers as a deterministic constrained allocation between recipient requests and donor capacity, with a proportional neutral balancing pool over eligible competing singles. Each title submits one net request. Net-positive requests can draw from neutral donors; net-negative requests can transfer to neutral recipients with physical headroom, all within the same limits. A title is a donor or recipient for that pass, never both. Cancel any unmatched portion. Use stable rounding, preserve the integer total exactly, and recheck caps **after** rounding. Never normalize and assume the per-title ratios survived. Log requested, granted, clipped, and unmatched transfers. Preserve the control intent in a separate telemetry field and keep the authoritative adjusted serviceable-intent field consistent with the actual clearing input.

Use segment fit and segment population weights to compute requests, but retain the existing regional clearing unit. No Youth/Mainstream double spending and no new segment purchasing capacity. A future segment-level adapter can replace this one if the underlying market actually exposes conserved segment allocations.

Preserving serviceable intent totals and physical limits protects local format budgets; it does not prove every downstream outcome is identical. Regional spillover, restocking, survival, chart rank, and later decisions can still diverge. Measure those effects. Never overwrite settled units to force a passing result.

Initial live scope is **singles only**. Album purchases, LP penetration, soundtrack origination, album/single format choice, project cadence, and catalog tails receive no new media multiplier. A profile can discuss an album without changing its economic inputs. Enabling album effects requires its own accepted experiment.

### 12.4 Close the indirect routes

- No direct media writes to canonical recognition, cultural standing, critical acclaim, momentum, record quality, genre acceptance, launch stock, chart points, market capacity, royalty rates, or mechanical accounting.
- Canonical recognition can still grow from actual chart outcomes through its existing milestone latches. That is the existing earned path, not another exposure deposit.
- Media-only member spotlight is stored separately from canonical personal recognition until a validated attribution replacement exists.
- All correlated follow-ups share a root event and remaining effect budget. A TV performance, its trade item, and a syndicated retelling do not provide three full causal grants.
- Existing player trade benefits remain under their existing cap. New named desks can replace presentation and attribution, but cannot multiply those benefits. New AI B2B benefits are observational until a substitution is proven.
- No new AI roster growth, member departure, death/bust hazard, touring yield, performance income, contract valuation, retirement protection, or automatic distribution expansion from media-owned state.
- New station refusals, retailer boycotts, and future recognition writers remain off unless their own adapter accounts for all effects within an accepted combined envelope. A cap on consumer preference alone does not bound a supply ban.

### 12.5 Acceptance limits are broader than market totals

Freeze the control build, flags, starting tree, seeds, and baseline artifact family before activation. Use the current economy's acceptance metrics and targets; do not recalibrate the control to make media pass.

Initial additional ceilings for paired causal/control runs, evaluated for each seed and year as well as pooled totals:

| Measure | Provisional acceptance limit |
|---|---|
| Annual singles units, album units, and combined wholesale revenue | Each within ±1% of the paired control |
| AI production/release decisions at the same frozen inputs; promotion debits | Exact match in decision fixtures; exact one-charge accounting |
| Annual active-label and active-artist populations; releases by format | Within ±1%; no fresh violations of existing lifecycle gates |
| Median AI annual operating surplus | Absolute shift at most 1% of control median annual revenue; use this denominator because surplus can cross zero |
| Chart concentration, top-label share, and genre/format shares | No new failure of existing bands; top-label share and HHI movement also reported explicitly |
| Genre share movement attributable to media | At most 1 percentage point annually in any existing genre reporting bucket |
| Routine and exceptional media demand effects | Every region/title/week respects §12.3; no averaged-away cap breach |

These limits are design acceptance requirements, not claims of measured results. Short fixtures cannot establish decade stability. If the effect is imperceptible at these caps, improve scenes, feedback, and attribution before widening the economy. If the limits fail, reduce or disable the causal adapter and retain the atmosphere layer; do not quietly retune unrelated supply, acceptance, quality, or population systems.

## 13. AI behavior: selective publicity with a recognizable policy

AI labels use the same opportunity records, eligibility, finite slots, standards decisions, evidence, publication decisions, and caps as the player. Their cognition can be simpler; their outcomes cannot bypass the rules.

Derive a compact policy from existing label priorities, artist disposition, manager archetype, evidence, current promotion budget, and upcoming availability. Do not simulate managers as new independent agents. Do not give every label a fresh personality roll every week.

| Policy tendency | Observable choices |
|---|---|
| Teen-pop cultivation | Favor relevant dance/music shows, accessible photographs, recurring fan-mag contact; seek a distinctive face or group story |
| Commercial pragmatism | Take a broad family appearance, accept a workable edit, concentrate on a title that can be stocked |
| Artistic protection | Negotiate another song, a live arrangement, or a craft interview; sometimes reject an image demand |
| Specialist/community focus | Work the outlets and programs that serve the actual audience; do not chase a national teen slot by default |
| Opportunistic publicity | Take a topical interview or a scarce late opening; weigh damaged trust from exaggeration |
| Cautious crisis handling | Clarify facts, preserve existing appointments where appropriate, reduce speculative access |

Use a small bounded planner at a weekly boundary: select a few campaign-relevant acts per label, examine a capped shortlist of feasible opportunities, rank by audience fit, access, cost, timing, and artist consent, then submit pitches. Reevaluate an existing plan only on meaningful changes: an offer, deadline, cancellation, release shift, or public episode. Everyone cannot pitch every editor every day.

Outlets choose from the combined pool of human and AI pitches using frozen evidence and their editorial policy. Major-label advantages appear through resources, relationships, and credible acts, with diminishing returns and bill diversity. Maintain eligible newcomer consideration so a colorful regional act is not permanently crowded out.

AI can make mistakes consistent with its policy: an overprotective manager loses a useful feature; a commercial house overuses the same pin-up angle; an artistic act surprises a family audience with a strong performance. Do not grant the AI exact hidden audience forecasts or reliable foreknowledge of its next production draw.

Fan frenzies arise from the same state transitions for rivals and the player. The planner can respond by prioritizing scarce photo and television dates, but the episode does not increase its money, releases, roster slots, or effect allowance outside the rare-event limits.

## 14. What the player sees and does

Extend the office, morning paper, Rolodex, calendar, and artist page. Avoid a detached dashboard that requires the player to farm every outlet daily.

**Office inbox:** an incoming producer inquiry, a reporter's deadline, a standards query, a feature proof limited to fact questions, a photo request, or a clipped story. Show who is asking, what they want, what is due, and the real decision.

**Media contact card:** role, outlet, location, interests, last dealings, known reliability, and relevant ways to approach them. Replace a mysterious universal friendship percentage with concrete memory: “Used your regional figures last month”; “Still waiting for the photograph”; “You missed rehearsal.”

**Appearance offer:** date/city, rehearsal and transmission details, expected audience range and peak viewing context, demographic fit, local clearance, song/version, live/track/mime rules, required edits, costs, rights, exclusivity, and whether the date is held or confirmed. Explain the marginal choice: a Saturday afternoon youth slot versus a later family-variety opportunity.

**Scenes:** producer asks for a shorter number; editor wants the quieter member in the photograph; musician objects to an invented romance; reporter asks who wrote the hit; a familiar DJ offers a short interview between records. Use existing scene grammar and member voices. Routine logistics may be delegated, but a new intrusive image demand or on-air compliance decision deserves attention.

**Follow-through:** actual airing/publication, estimated viewers, audience reactions, specific inbound inquiries, and the occasional disappointment. “The program cleared six of your serviced markets” is useful. “+17 fame” is insufficient. Observational interest must not be worded as a firm distributor order or a booked paying gig unless the relevant system actually created one.

**Artist history:** a bounded scrapbook of meaningful appearances, photographs, interviews, criticism, and public episodes. Group member attention is visible, with media spotlight clearly distinct from earned canonical recognition. Rival history is public evidence, not their private diary.

**Pacing:** combine low-stakes requests into a weekly summary. Surface a few consequential decisions rather than interrupting every day with the same interview. Standing policies can accept routine clean edits, decline romance fabrication, protect specified topics, or delegate small local dates within a budget. The player can revise policy without rewriting already-completed events.

## 15. Implementation contract, ordering, persistence, and telemetry

Suggested names are design names, not claims that these types exist today:

| Proposed component | Responsibility |
|---|---|
| `MediaAtmosphere` | Feature switches, mode, configuration version, isolated deterministic namespace |
| `MediaOutletCatalog` / `BroadcastScheduleVersion` | Historical availability, roles, footprints, editorial/production policies, schedules, source confidence |
| `MediaOpportunityService` | Bounded opportunity generation, shared slots, holds, deadlines, feasibility |
| `MediaAppearanceResolver` / `BroadcastExposureService` | Rehearsal/performance, airings, actual time-slot exposure, deduplication |
| `MediaPressService` | Assignments, sources, interviews, publication, corrections, exclusivity |
| `MediaResponseService` | Media-owned attachment, attitudes, episode progression, parent/teen/critic splits |
| `MediaEconomicAdapter` | The only new market writer; constrained requests and transfers under §12 |
| `AiMediaPlanner` | Existing-budget allocation, policy choices, pitches and responses |
| `MediaHistoryComposer` | Grounded text from settled events; zero simulation writes |

### 15.1 Deterministic processing

Use a saved world seed and keyed draws incorporating feature namespace, schema/config version, stable act/contact/outlet/booking/story IDs, occurrence date, and draw purpose. Never use runtime `GetHashCode`, collection iteration order, display text, or the global `GD.Rand*` stream. Reopening an offer must not reroll it. A seed must distinguish otherwise identical worlds.

Daily/time-bound processing handles deadlines, appointments, recordings, publications, and airings in chronological order. Fast-forward processes every due transition in the same order as ordinary play. A late broadcast after the week's market cutoff affects the next eligible settlement, not units already sold.

The weekly boundary first settles due media transitions and freezes eligible media response from information known by the cutoff; the market adapter consumes that frozen request snapshot; the existing market and completed-week settlement run; only afterward can the new sales/chart evidence inform future press and bookings. Prove the exact insertion order against `ChartManager` and `TimeManager` at implementation. Do not add an unordered event subscriber that sometimes sees the new chart and sometimes the old one.

### 15.2 Save/load and migration

Persist stable IDs, outlet/schedule versions, contacts, active relationships, offers/holds/commitments, performance variants, standards terms, consumed resources, actual airings, pending syndication dates, assignments, evidence/claims, exclusives, active episodes, decay clocks, applied-effect receipts, allocation reservations, and seed/config. Keep compact milestone history after archiving closed transactions.

Each transition and each debit/effect has an idempotency receipt. Save after rehearsal and reload: no repeat travel charge. Save after transmission and before the weekly market: the airing still counts once. Save after the weekly market: no second demand transfer. Old trade picks retain their original economic expiry and migrate once. Missing historical metadata stays unknown; do not backfill a decade of high-visibility coverage or random fame into old saves.

Mode switches cannot replay old history for fresh causal credit. Disabled/observe state may retain stories for display, but only newly eligible events in a live causal window can request effects. Explicitly store the activation boundary and invalidate stale pending grants while retaining their history.

Deleting a label, retiring a title, moving an act between labels, or a contact changing employment must not duplicate bookings, leak private evidence, or leave infinite holds. Scope commitments to the responsible party and the contract; preserve the same act/person IDs. Do not transfer one label's confidential correspondence to another as public knowledge.

### 15.3 Useful telemetry

Emit separate compact opportunity, airing, story, response, budget, and effect tables. Include:

- World seed/mode/config; dates and local times; stable IDs; program/issue; original/repeat; story root.
- Eligibility, consideration reasons, lost slots, cancellations, holds, standards outcomes, and consent decisions.
- Performance method versus broadcast method; rehearsal; outcome attribution; technical versus artist fault.
- Available audience, clearance, competition share, segment retention, gross impressions, estimated novel reach, uncertainty.
- Story family, angle, sources/verification, positive/negative/mixed response by audience, correction trail.
- Attachment/episode changes, prior evidence window, member spotlight, and decay.
- Existing promotion envelope, allocation, payer/payee, reservation/debit receipts, and rejected unaffordable plans.
- Per-title baseline intent, request, transfer, binding cap, physical limit, root-event deduplication, downstream settled units.

No single “media score” can substitute for this provenance. The audit must be able to answer whether TV helped a particular existing title, whether a newspaper merely described success, and which competing titles supplied any transferred demand.

## 16. Delivery phases and evidence required

**Phase 0 — recon and baseline.** Audit existing consumers, trade bonuses, content tags/aliases, station refusals, rights, and diary/money seams. Freeze current control hashes and acceptance metrics. Research the program registry, exact opening/cancellation dates, original versus repeat schedules, performance methods, and named press identities. Keep source confidence. This document supplies a design and research leads, not a fully verified episode database.

**Phase 1 — historical institutions and peak audiences, observe-only.** Implement catalogs, local shows, effective-dated schedules, clearance, viewing curves, finite slots, delayed syndication, and exposure accounting. Produce read-only paper items and a clear offer/airing distinction. All economic columns must match control exactly.

**Phase 2 — journalist and producer interactions, observe-only.** Contacts, pitches, interviews, source evidence, deadlines, issue budgets, standards queries, photo/exclusivity terms, performance results, corrections, and artist consent. Exercise scenes through the actual office UI. Test that private musician information cannot leak through omniscient reporting.

**Phase 3 — AI characterization and fan episodes, observe-only.** Bounded AI planner, existing-budget allocation estimates, distinct editorial decisions, fan-mag member attention, and emergent frenzies. Prove rivals use the same slots and dates. No extra spending, production, release, canonical traits, or economic readers.

**Phase 4 — player costs and the singles demand adapter.** Activate actual authorized costs and rights/accounting, preserve old trade behavior, then introduce §12's single constrained writer. Validate each writer separately before the combined adapter. Recognition, albums, supply bans, and AI fixed costs remain outside this activation. *Amendment (2026-10-31, author): a media → act recognition writer is approved as its own bounded adapter. It is shadow-validated first and kept off the §12.3 ledger; see `MediaRecognitionAdapterDesign.md`.*

**Phase 5 — long-run acceptance and presentation.** Run paired controls and causal worlds through the complete decade, inspect tails and indirect effects, then polish summaries, scene variation, contact history, and scrapbook pacing. Record measured results in a separate implementation/acceptance handoff; do not rewrite provisional targets as if they were measurements.

### 16.1 Required behavior fixtures

1. A plausible uncharted local act can book a small dance/music show after servicing and feasible preparation; a national show can still decline it.
2. No Shindig!/Hullabaloo bookings after their original production ends; no Tiger Beat before September 1965; no Rolling Stone before November 1967; Voice exists earlier.
3. The same appearance at a teen viewing peak and a weak adult slot produces different audience composition. Seasonal and competing-program changes affect exposure without changing record-sales seasonality constants.
4. Delayed West Coast carriage uses its own local hour. A preempted affiliate has zero exposure. A closing segment uses its own retention; it does not inherit a whole-episode peak by default.
5. One syndicated tape generates dated regional airings; repeated songs and reruns never exceed population for unique reach or repeat the original fee/incident.
6. A strong live act usually delivers; a weak unprepared singer has a worse tail; a failed mime affects presentation without changing the prerecorded sound.
7. A lyric edit, alternate song, honest refusal, and promised-then-broken agreement produce distinct access and credibility consequences. A fixed mime track cannot spontaneously sing a different lyric.
8. A private substance-load field never creates a published accusation. A sourced public incident can create a story with verification and a bounded pickup path.
9. Buying a trade ad never guarantees a review, an interview never guarantees publication, and a review never assigns a chart place. Named desks do not multiply old bonuses.
10. A photo exclusive has a scope and expiry; a planted romance stays a claim; an artist can refuse participation; fabricated publicity can later be contradicted.
11. A local favorite can remain regional; multi-source evidence can produce a rival national frenzy; repeated descriptions of one event cannot meet independent-evidence requirements.
12. A high-frenzy act with no serviceable baseline demand receives no units from the initial adapter; a viable stocked title can receive a clipped, conserved transfer.
13. Per-region totals, per-title ratios, physical limits, and integer rounding invariants hold with tiny totals, saturated recipients, all-positive requests, all-negative requests, and no eligible donors.
14. AI allocation sums to its existing promotion envelope and is charged once. Confirm performer fees do not become label record income.
15. Save/reload, multi-day skips, mode changes, cancellations, label transfer, and title retirement preserve chronology and once-only effects. Repeated UI reads do not draw RNG or mutate outcomes.

### 16.2 Run ladder and stopping rules

Use the repository's current build/headless instructions and accepted flags in `HeadlessGodotExecutionNote.md`. Do not invent a passing run from a build alone. Begin with decision/accounting fixtures, then matched 52-week worlds, a mid-decade window covering new outlets and censorship, and complete 1960–1969 paired runs. Use existing accepted seeds 1001 and 1002 plus at least two preregistered holdouts, and representative player starts where costs are live. Do not move the holdouts after seeing an attractive result.

Disabled/control identity and observe/control economic identity are hard gates. Causal comparisons use §12.5 plus the existing economy's stricter gates where applicable. Report all per-seed failures, distributions, top-label/artist tails, genre outcomes, appointment volume, and performance overhead. A pooled mean cannot excuse a monopoly seed or an out-of-cap week.

Keep work bounded by active campaigns, scheduled dates, live assignments, and sparse local relationships. Cache stable schedules and demographic curves. Initial performance budget: no more than 5% median or 10% p95 added weekly-tick time relative to a matched warmed control, with memory/history growth measured over the decade. Recompute text when requested, not every simulation tick.

Completion requires an atmosphere the player can encounter and trace: local television access, consequential live/track choices, a time slot that changes who watches, a journalist with a reason to call, a fan editor who remembers a face, and rivals whose public lives differ. Economic activation additionally requires the stated receipts, conserved transfers, and accepted paired-run evidence. If causality fails, keep the useful world and its decisions in observation while repairing the one bounded adapter.
