# Local Scene Historical Atlas — Research and Authoring Brief

Companion to [Local Scene Ecosystem Directive](LocalSceneEcosystemDirective.md). Researched October 8, 2026, against the 31-city registry in `Systems/DistanceModel.cs` at `3203a66df703325b22f206d674d9da96cff04f50`.

This is a first-pass historical design atlas, not a finished numerical content database. It supplies evidence-backed anchors, corrections, and a research/authoring contract for every playable city. It does not claim that all proposed subscenes, venue counts, dates, or local genre shares are already verified.

**Reading the tables:** “Evidence” identifies what the linked source supports. “Design direction” is our interpretation or proposal. “Still to establish” names gaps before production content ships. A source proving one club or tradition does not establish a city's entire genre distribution. No table is a zero-sum ranking of cities.

## 1. Historical corrections that change the architecture

### Miami is several musical worlds, not generic Deep South country

HistoryMiami documents local 1960s garage bands and R&B performers. Its research on Cuban popular music describes early-exile communities recreating musical and broadcasting institutions. A first-person account of a July 1968 visit describes soul radio, local performers, clubs, producers and distribution contacts. Together these support separate community, commercial and youth networks—not a single southern affinity inherited from Nashville. [HistoryMiami: Teen Miami](https://temp.historymiami.org/exhibition/teen-miami/), [HistoryMiami: Cuban Popular Music](https://historymiami.org/wp-content/uploads/sfh-1990-3.pdf), [Soul Music Odyssey: Miami](https://pressbooks.library.yorku.ca/soulmusicodyssey/chapter/miami/).

**Design implication:** differentiate local soul/R&B, Cuban/Spanish-language community music, hotel/supper-club work and youth rock. Country can exist as a minority or visiting pathway. Do not import the later disco identity into 1960. A 1968 rock festival demonstrates visiting-event opportunity, not that all its performers were Miami residents. [HistoryMiami: Miami Rocks](https://temp.historymiami.org/exhibition/miamirocks/).

### New Orleans jazz is a living community throughout the decade

Preservation Hall's history and dated photographs establish continuing traditional jazz performance in the 1960s. The city's jazz museum opened in 1961, reflecting an organized preservation community. Neither fact implies that jazz controlled national pop sales. [Preservation Hall: Our Story](https://www.preservationhall.com/about/), [New Orleans Jazz Museum: 1961 opening](https://www.neworleansjazzmuseum.org/november-12-1961-the-worlds-first-jazz-museum-opens).

**Design implication:** retain traditional jazz, brass/community and other jazz pathways alongside R&B/soul. Track audiences, working livelihoods and heritage separately from pop exports. A late-decade change in chart share cannot delete older local musicians.

### San Francisco did not begin with psychedelia

The hungry i and the San Francisco Folk Music Club contributed to an earlier folk community; the hungry i also presented jazz and comedy. [FoundSF: Folk institutions](https://www.foundsf.org/index.php?title=1960%E2%80%99s_Folk_Music_at_the_hungry_i_and_SF_Folk_Music_Club).

**Design implication:** early folk/jazz/cabaret institutions provide people, relationships and audience pathways into later developments. The later psychedelic surge is a change in opportunities and export attention. Oakland and Berkeley retain identities within the wider Bay network.

### Soul cities do not switch off when rock becomes conspicuous

Philadelphia's small labels and writers developed soul during the 1960s, with later institutional consolidation; it was not simply a dead teen-pop city after the Invasion. Detroit's late-decade rock is documented alongside Motown's continuing recruitment and releases. Chicago's Record Row participated in gospel-influenced soul while its blues and folk worlds continued changing. [Philadelphia Encyclopedia: Soul Music](https://philadelphiaencyclopedia.org/essays/soul-music/), [Motown Museum: Jackson 5](https://www.motownmuseum.org/artist/the-jackson-5/), [Rock Hall: MC5](https://rockhall.com/inductees/mc5/), [Encyclopedia of Chicago: R&B](https://www.encyclopedia.chicagohistory.org/pages/1070.html).

**Design implication:** independently varying subscenes, with overlap and personnel exchange. No whole-city “soul→proto-punk” replacement curve. Do not move Philadelphia International Records into the 1960s; the institutional culmination described by the source belongs to the following decade.

### Nashville and Memphis are not single-genre caricatures

The Country Music Hall of Fame's Nashville R&B exhibition explicitly covers 1945–1970. Stax's institutional history starts with Satellite in 1957 and the Stax name in 1961, before the sketch's suggested Memphis rise window. [Night Train to Nashville](https://countrymusichalloffame.org/night-train-to-nashville), [Stax Museum: history](https://staxmuseum.org/stax-museums-electronic-press-kit/).

**Design implication:** strong Nashville country institutions coexist with Black R&B networks. Memphis soul opportunity grows out of prior musical/institutional life; it is not generated from nothing in 1964.

### Dance circuits precede psychedelic ballrooms

The Spanish Castle's history includes teen rock bookings from 1959 and country programming. Tacoma's Sonics progressed through smaller rooms and larger halls. These show how promoter networks, audience age and venue ladders create a scene before 1966. [HistoryLink: Spanish Castle](https://www.historylink.org/File/3826), [HistoryLink: Sonics](https://historylink.org/File/8844).

**Design implication:** physical venue, programming and promoter are separate. Existing halls can host new movements without being newly invented venue types.

### Geographic exceptions are part of realism

Motown's account of the Jackson 5 describes a Gary act reaching the company through Bobby Taylor. FAME's history documents recording activity and a 1961 breakthrough in Muscle Shoals. These are evidence for referral networks and specialized recording destinations, not reasons to make all recruitment local or to equate recording city with birthplace. [Motown Museum](https://www.motownmuseum.org/artist/the-jackson-5/), [FAME: Our History](https://famestudios.com/our-history/).

## 2. All 31 playable cities

The ordering follows the existing game regions, which remain commercial abstractions. These groups are not claims that their member cities share a culture. Qualitative mixes below are authoring directions, not measured market shares.

### East Coast registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `new_york` | Documented Village clubs/coffeehouses and folk revival. Build overlapping folk, jazz, publishing/pop, R&B vocal and Latin-community worlds; a trade center with multiple entry points. [Museum of the City of New York](https://www.mcny.org/exhibition/folk-city). | Preserve established worlds while electric/experimental opportunities grow. Source the Latin/boogaloo, publishing, jazz and neighborhood profiles independently; the folk exhibit does not prove them all. Proto-punk is a minority pathway, never the whole city's final signature. |
| `boston` | Club 47 began as a jazz coffeehouse in 1958 and became a close musician community drawing Boston and Cambridge. Build a Cambridge-aware folk/college network and separate professional rooms. [History Cambridge](https://historycambridge.org/Folk%20Music/Folk%20Tour%20Club%2047.html). | Venue/community continuity with changes in repertoire. Establish rock, jazz and R&B institutions separately. Preserve Cambridge hometowns; do not call every participant Boston-born. Club 47's documented closure is 1968. [Walking-tour guide](https://historycambridge.org/images_graphics/Folk%20Music%20tour/Folk%20Music%20Tour%20final-2.pdf). |
| `philadelphia` | Small soul labels, arrangers, singers and session communities are documented. Build teen-pop/television roots alongside gospel/vocal/soul development. [Philadelphia Encyclopedia](https://philadelphiaencyclopedia.org/essays/soul-music/). | Continue local soul development after early teen-pop dominance changes; no post-1963 collapse. Date label/studio openings and separate late-1960s antecedents from the mature 1970s sound. |
| `baltimore` | Royal Theatre/Pennsylvania Avenue heritage establishes a major Black entertainment anchor. Build connected theatre, club, gospel and youth pathways. [Baltimore Heritage](https://baltimoreheritage.org/event/27746/). | Establish actual 1960s bills, venue health and neighborhood changes before scheduling historical closures. DC/Philadelphia connections are proposed network candidates, not automatic shared rosters. |
| `washington` | Local bluegrass musicians and working groups are documented in the middle/late decade; separate R&B/jazz/community circuits require content. [DC Bluegrass Union: Tom Gray](https://dcbu.org/tomgray/). | Folk/bluegrass is a real opportunity, not an incidental country import. Do not bring in mature go-go: Smithsonian dates its foundation to the early 1970s. [National Museum of American History](https://www.americanhistory.si.edu/explore/stories/go-go-funky-percussive-music-invented-washington-dc). Source specific 1960s rooms/radio schedules. |
| `pittsburgh` | Hill District/Crawford Grill history establishes deep jazz networks and cross-racial musical contact. Build jazz and working-club continuity, with separately researched vocal/R&B and youth routes. [Heinz History Center](https://www.heinzhistorycenter.org/blog/western-pennsylvania-history-the-crawford-grill/). | Be precise about which Crawford Grill location and dates apply. Investigate urban redevelopment's venue effects rather than treating “mill town” as the whole identity. The source's strongest description concerns earlier decades; 1960s roster/capacity needs corroboration. |

### Great Lakes registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `chicago` | Record Row R&B/soul, blues and folk institutions support several concurrent worlds. [Encyclopedia of Chicago: R&B](https://www.encyclopedia.chicagohistory.org/pages/1070.html), [Folk Music](https://www.encyclopedia.chicagohistory.org/pages/466.html), [Music Clubs](https://www.encyclopedia.chicagohistory.org/pages/861.html). | Blues audiences and markets change without ending local activity. Separate neighborhood rooms, student/folk audiences and production networks. Do not tie the city's fate to one label. Source each venue's dates and avoid later institutions in early years. |
| `detroit` | Motown's recruitment account and the documented MC5 scene support concurrent soul/pop and harder experimental rock networks. [Motown Museum](https://www.motownmuseum.org/artist/the-jackson-5/), [Rock Hall](https://rockhall.com/inductees/mc5/). | Grande Ballroom rock programming is anchored by a contemporary October 1966 announcement. [Fifth Estate archive](https://www.fifthestate.org/archive/15-october-1-15-1966/participatory-zoo-dance-rescheduled-for-oct-7-8/). Model a new programming network, not the invention of ballrooms or termination of soul. Extend to jazz/gospel with separate evidence. |
| `cleveland` | La Cave opened in 1962 as a coffeehouse folk club and evolved with rock. [Case Western Reserve: La Cave](https://case.edu/ech/articles/l/la-cave). | A strong venue-transition case: retain identity while bills/audiences change. Establish local R&B, radio influence, and youth-band pathways separately; radio importance alone does not imply a vast hometown artist pool. |
| `cincinnati` | King joined recording, manufacturing and promotion and worked across country/R&B traditions. [Urban Appalachian Community Coalition, article by library historian Brian Powers](https://uacvoice.org/2017/08/king-records-appalachian-migration/), [King Records history project](https://kingrecords.org/tam). | Model studio/label/circuit links and Appalachian migration context. King's reach demonstrates that an independent can have strong nonlocal networks. Do not infer every recorded act was Cincinnati-born. Date changes in company operation before coding them. |

### Great Plains registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `minneapolis` | The city's commissioned music history describes a substantial mid-1960s ballroom-band ecology. Build Twin Cities teen dance/garage work plus folk and separately researched Black music networks. [Minneapolis Music History, 1850–2000](https://www.minneapolismn.gov/media/-www-content-assets/documents/Minneapolis-Music-History-1850-2000.pdf). | Distinguish St. Paul and catchment towns; touring halls connect beyond the metro. This also warns against treating 80 simulated acts as an actual census. Do not retroject the later Prince-era Minneapolis Sound. Obtain venue and label year ranges. |
| `st_louis` | Gaslight Square flourished in the 1950s–1960s; library collection anchors an entertainment district. Proposed layers: jazz/cabaret/folk rooms and distinct blues/R&B/rock working circuits. [St. Louis Public Library](https://www.slpl.org/archival_post/gaslight-square-photo-collection/). | Source actual 1960s programs and South/East St. Louis connections. Collection photographs are from 1971–72; their date is not permission to transplant every photographed business into 1960. Other genre layers remain provisional. |
| `kansas_city` | Established jazz legacy and musician mobility are supported by museum collections; the cited museum overview is not a 1960s scene census. [American Jazz Museum exhibits](https://americanjazzmuseum.org/exhibits/). | Provisional profile: jazz/blues working traditions, dance bands, regional routes and younger R&B/rock activity. Research 1960–69 advertisements/radio charts before fixing weights or famous rooms. Never freeze the city in its 1930s swing heyday. |
| `omaha` | Local radio entrepreneurship and Top 40 history are documented. [Nebraska State Historical Society: Todd Storz](https://history.nebraska.gov/todd-storz-radio-for-a-new-era/). | Provisional performance profile: Black jazz/R&B community, youth dances and regional working bands. These require local club/artist evidence beyond the radio source. Represent reach through media independently of recording infrastructure; avoid “small, quiet, culturally empty” defaults. |

### Deep South registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `nashville` | Country institutions coexist with the documented 1945–1970 R&B world. [Country Music Hall of Fame: Night Train](https://countrymusichalloffame.org/night-train-to-nashville). | Strong country publishing/studio opportunities throughout; separate Jefferson Street/community routes and access. Source room-level calendars and cross-scene session work. A country business hub does not make every resident a country performer. |
| `memphis` | Satellite/Stax chronology predates 1964; studio and neighborhood relationships provide an institutional soul anchor. [Stax Museum](https://staxmuseum.org/stax-museums-electronic-press-kit/). | Build blues/gospel/R&B roots, soul development and independently evidenced country/rock remnants. Model recording visitors separately. Do not start the entire scene in the middle of the decade or script every historical label outcome onto fictional firms. |
| `atlanta` | Georgia's R&B/soul history establishes an important regional context, but statewide artists cannot all be assigned to Atlanta. [New Georgia Encyclopedia: Blues, R&B & Soul](https://www.georgiaencyclopedia.org/topics/blues-rb-soul/). | Proposed Atlanta institutions include Black entertainment, gospel, radio and touring connections. Verify individual venues/broadcasters and dates. Retain Macon/Augusta origins; do not import the city's much later national industry dominance. |
| `new_orleans` | Living traditional jazz and preservation institutions explicitly continue into the decade. [Preservation Hall](https://www.preservationhall.com/about/), [Jazz Museum's 1961 opening](https://www.neworleansjazzmuseum.org/november-12-1961-the-worlds-first-jazz-museum-opens). | Distinct jazz, community/brass, R&B/soul and tourist circuits. Jazz persists even if pop attention changes. Source specific R&B/studio profiles and community calendars; no generic “slow fade” scalar across everything. |
| `miami` | Museum and first-person evidence supports 1960s garage/R&B, Cuban community music, and soul business links. [Teen Miami](https://temp.historymiami.org/exhibition/teen-miami/), [Cuban Popular Music](https://historymiami.org/wp-content/uploads/sfh-1990-3.pdf), [1968 field account](https://pressbooks.library.yorku.ca/soulmusicodyssey/chapter/miami/). | Parallel soul, Spanish-language, tourism/professional and youth networks. Later decade opportunities grow without deleting earlier communities. Verify each named institution's dates; do not turn the interviewer's opinions into objective talent scores or use a 1968 snapshot as a 1960 census. |

### Southwest registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `dallas` | Texas history documents Dallas/Fort Worth recording/performer activity and a wider rock tradition. [Texas State Historical Association: Rock-and-Roll](https://www.tshaonline.org/handbook/entries/rock-and-roll). | Proposed country, R&B and mid-decade garage/dance networks; preserve Fort Worth/Denton origins and connections. Date rooms and studio networks separately; a 1969 festival cannot supply a permanent year-round resident roster. |
| `houston` | Duke/Peacock and associated booking activity establish blues/R&B and label-network infrastructure. [TSHA: Blues](https://www.tshaonline.org/handbook/entries/blues). | Distinguish blues, gospel/soul, country and younger rock opportunities with further institution-level work. Booking ties explain distant artists; do not reduce Houston to oil money and generic available talent. |
| `san_antonio` | Conjunto and orquesta Tejana have distinct histories and overlapping audiences; the West Side sound deserves its own treatment. [TSHA: Conjunto](https://www.tshaonline.org/handbook/entries/texas-mexican-conjunto), [Orquestas Tejanas](https://www.tshaonline.org/handbook/entries/orquestas-tejanas), [Texas State: West Side Sound study](https://docs.gato.txst.edu/56030/Talk_to_me_The_History_Of_San_Antonio-s_West_Side_Sound.pdf). | Model language, repertoire, dance venues and hybrid influences. Do not flatten all Latin music into one TexMex probability. Source garage-band institutions independently and respect style continuity across the decade. |
| `phoenix` | Arizona PBS's historian interview describes Phoenix recording, guitar/twang experimentation and artists of the 1950s/1960s. [Arizona PBS: The Phoenix Sound](https://azpbs.org/horizon/2015/12/book-the-phoenix-sound/). | Small-metro studio specialization can exceed a population-based expectation. Proposed country/rockabilly, instrumental and youth-rock pathways need dated artist/venue detail. “Few studios” cannot imply no consequential recording practice. |
| `albuquerque` | The local tourism history identifies a 1960s garage-rock scene and later Spanish-language activity; those time periods must not be conflated. [Visit Albuquerque: Sound of 66](https://www.visitalbuquerque.org/route-66-centennial/learn/stories/the-sound-of-66/). | Research the specifically 1960s Spanish-language, country and recording networks before assigning weights. Preserve Clovis as a separate recording destination where appropriate. A modest city can connect through road circuits without being a miniature Los Angeles. |

### Rockies registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `denver` | Harry Tuft's archive documents the Denver Folklore Center from 1962 and its role in Colorado's folk revival. [University of Colorado American Music Research Center](https://www.colorado.edu/amrc/2025/12/02/american-music-research-center-acquires-harry-tuft-collection). | Begin with folk institutions and regional connections; add later rock-promoter development only with dated evidence. Jazz, country and neighborhood networks require separate sourcing. Distance is a strategic cost, not cultural absence. |
| `salt_lake_city` | Oral-history finding aids identify participants in a 1960s Utah folk scene. [Utah Historical Society oral-history index](https://history.utah.gov/wp-content/uploads/2024/05/Womens-Oral-History-Database-Sheet1.pdf). | Folk/community and youth performance are provisional directions; read the underlying interviews and establish city-specific rooms, audience/access rules and touring routes. A religious/community context must not become a stereotype of musical uniformity or no nightlife. |
| `billings` | Participant accounts of the Wanderers describe school/teen dates and a Montana/Wyoming circuit, including a claimed 1966 battle-of-bands win. [Garage Hangover: participant account](https://garagehangover.com/wanderersmt/). | This is a valuable primary recollection on a specialist site, not a verified city census. Build a small youth/working circuit; corroborate ads, dates and institutions. Country/community opportunities remain to source. Do not make Billings empty or force its labels to shop the entire Midwest. |

### West Coast registry

| City ID | Evidence and proposed scene character | Decade behavior and remaining research |
|---|---|---|
| `los_angeles` | Sunset Strip documentation and Eastside community history support geographically distinct networks. [Library of Congress HABS: Sunset Strip](https://tile.loc.gov/storage-services/master/pnp/habshaer/ca/ca4300/ca4391/data/ca4391data.pdf), [East LA music-history project](https://eastlarevue.com/historic/). | Proposed overlapping studio/pop, surf, Eastside, R&B, folk/rock and later experimental pathways. Source each, including coastal/suburban catchments; “surf→canyon” is too narrow. Studio labor and neighborhood stages should not be the same candidate pool. |
| `san_francisco` | Earlier folk, jazz/cabaret institutions and community gatherings are documented. [FoundSF](https://www.foundsf.org/index.php?title=1960%E2%80%99s_Folk_Music_at_the_hungry_i_and_SF_Folk_Music_Club). | Preserve active early communities while later psychedelic opportunities expand. Individually source later ballrooms/promoters. Oakland/Berkeley need distinct place IDs and community profiles; never infer all Bay Area musicians lived in San Francisco. |
| `seattle` | Seattle/Tacoma dance and R&B-derived rock networks preceded the mid-1960s; the Spanish Castle served a wider corridor. [HistoryLink: Richard Berry's 1957 visit and repertoire transmission](https://www.historylink.org/File/9173), [Spanish Castle](https://www.historylink.org/File/3826). | Tacoma retains origin identity; shared standards and circuits spread influence. Do not equate all Northwest music with garage or pretend it starts in 1963. Source separate jazz/folk/community continuity. |
| `portland` | Northwest histories link Portland acts to a broader dance/rock network; Portland's historic-resource study provides evidence for Black community institutions. [HistoryLink](https://www.historylink.org/File/9173), [City of Portland historic-resource documentation](https://www.portland.gov/bps/planning/historic-resources/documents/african-american-resources-portland-oregon-1851-1973/download). | Separate garage/dance, jazz/R&B and folk paths. Verify individual club operation ranges before use. Model connections with Seattle/Tacoma without merging all three into a generic West Coast pool. |

## 3. Institutions and the sound they make possible

The following are **mechanisms to implement**, inferred from the research, rather than claims that every example institution had identical rules.

| Institution/network | Opportunity it creates | Story it can produce |
|---|---|---|
| Teen dance promoter / school hall | Regular bills, local competition, rehearsed dance repertoire, regional routing. | The opening act starts drawing more than the headliner; another promoter offers a better date. |
| Church/community music | Ensemble practice, audiences, mentorship, interpersonal trust, distinctive repertoire. | A singer considers secular work while balancing existing commitments. |
| Coffeehouse / folk club | Listening audiences, original material, shared standards, introductions and jams. | An old trio's songwriter finds an electric band through people they already know. |
| Working hotel/supper room | Reliable paid work, presentation, demanding audiences, professional networks. | A technically accomplished act declines touring because its residency pays steadily. |
| Label/studio/session community | Recording access, producer judgment, recurring ensembles, transfer of musical practice. | A new engineer and familiar rhythm section make a record that attracts outside clients. |
| Local radio / shops / jukebox route | Observable requests, sales and repertoire diffusion. | A local single breaks in nearby towns before a national company notices. |
| College / visiting circuit | Seasonal audiences, hosts, introductions, fees and travel obligations. | An out-of-town act earns a return booking and later a label introduction. |
| Specialized booking/referral network | Access across distance and sometimes across institutional barriers. | A small specialist signs a distant act for a reason the player can understand. |

The data model should support a venue hosting several communities on different nights. A generic “house genre” is useful as a tendency but insufficient as the sole admission rule.

## 4. Historical content contract

Every production-ready city profile needs:

1. **Identity and boundary:** city, metro/catchment and meaningful satellites; map coordinates; explanation of any market projection.
2. **Period baseline:** population year/source and geographic definition. Do not mix city-proper counts with metro counts or silently treat current numbers as 1960 numbers.
3. **At least two meaningful musical communities where evidence supports them**, with small places allowed modest but nonempty opportunity. Do not invent a second scene simply to satisfy a quota; document uncertainty and use conservative generalized opportunities.
4. **Early/middle/late-decade snapshots:** proposed 1960–63, 1964–66, 1967–69 authoring windows, refined to exact dates only when evidence justifies them. No automatic national change on each boundary.
5. **Performance and non-performance institutions:** named historical anchors where sourced and period-plausible fictional supporting institutions explicitly marked as fictional.
6. **Local industry and connections:** labels, studios, publishers, agents, radio and record distribution. Distinguish actual act origins from where records were made or released.
7. **Access and communities:** language, neighborhood, audience, youth/adult programming, institutions and historically supported barriers. Never derive innate traits from demographics.
8. **Dated changes:** openings, closures, programming changes, migration, transport or redevelopment effects. Mark hard evidence versus broad inference.
9. **Local life outside national charts:** regular work, community prestige, standards, musicians who remain local and acts not seeking contracts.
10. **Citations and uncertainty per authored claim:** source, location/page where applicable, date consulted, relevant years, confidence, contradictory evidence, and the exact fields affected.

Minimum first-slice content should include several persistent places to hear music, a recurring cast, a credible local contact network, at least one feasible path to recording/distribution, and a way to hear visiting talent. Counts are balance choices. Do not claim that four playable rooms means the real city had four venues.

## 5. Evidence standards and safeguards against false precision

Use period advertisements, city directories, radio surveys, union records, trade papers, recordings, photographs, oral histories, institutional archives and scholarly local histories together. National chart histories reveal commercial visibility, not total scene activity. Survivor memoirs may exaggerate importance or misremember dates; one account needs corroboration before it dictates precise venue capacities or economic weights.

Several sources here are institutional histories or scholarly secondary syntheses. The Miami travel account and Billings participant recollections are firsthand accounts presented later, not error-free contemporary census data. The Grande announcement is contemporaneous evidence of programming. A finding aid identifies research material; it does not mean all underlying interviews were reviewed.

Research coverage is strongest for the named architectural corrections and major institutional examples. Kansas City and Omaha's specifically 1960s performance mix, Salt Lake City's room-level detail, some smaller-city subscenes, and numerical mixes/capacities remain provisional. That uncertainty is a content-authoring backlog, not permission to populate them with national stereotypes. This directive is complete as a first-pass design; final historical datasets are a later implementation deliverable.

### Rules for translating evidence into weights

- A source's list of famous artists is not a statistically representative genre sample.
- Use bounded qualitative tiers first: absent in evidence, limited, established, prominent, exceptional. Absence of documentation is not proof of absence.
- Convert tiers to testable priors only after comparing opportunity, supply, activity and export visibility independently.
- Existing game genre gates remain authoritative for commercial supply unless a separate reviewed change is necessary. Earlier roots can use repertoire/style descriptors without creating an anachronistic commercial genre.
- Do not hard-ban plausible minority music because a city is famous for something else.
- Do not guarantee each city a star, a major office, or equal national success.
- Expose which fields are historical anchors and which are simulation parameters.

## 6. Example authored profiles: behavior, not percentages

**New Orleans, early decade.** A traditional-jazz community supports recurring players and rooms; R&B activity has different booking/recording connections; a touring act is labeled a visitor. Preservation Hall becomes available only from its supported opening period. Players can discover performers whose local draw exceeds their pop-market prospects without being told they are inherently inferior musicians.

**Miami, middle/late decade.** Different contacts lead to soul clubs, Spanish-language community performances, professional tourist work, and teen bands. Radio/distributor knowledge can reveal a regional record before a headline. One city's populations and institutions overlap without sharing every venue or language network. The exact timetable of each fictional booking is simulated; the historical anchors constrain plausibility.

**Billings, middle decade.** Several persistent working/teen opportunities connect to a regional touring network. A local act can become a familiar favorite, hear a visitor, change personnel, or move for a credible opportunity. The player must develop local trust or travel to expand options. A shortage of fresh leads does not summon four new bands on demand.

**Detroit, late decade.** A soul/pop production world coexists with harder rock activity in a separate but connected circuit. A referral can bring in a Gary act without rewriting its origin. Local success may change who visits and which rooms matter, while the game remains free to produce different careers from history.

These are intended outcomes for simulated worlds, not scripts that assign real historical careers to generated acts.
