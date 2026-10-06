Right call, and it solves a problem we left hanging: Production as a 4-value scalar was too blunt to distinguish _Pet Sounds_ from _Sgt. Pepper_. Tags fix that. But your sketch has three things in one bucket that behave completely differently in a business sim, so I'd split by **mechanical consequence**, not by vibe.

## The classes

|Class|Costs|Gated by|Drives|
|---|---|---|---|
|**Instrumentation**|Money (players, hire)|Era, access, budget|Session fees, recording days|
|**Technique**|Time (studio hours)|Era, studio capability, engineer skill|Budget burn, innovation credit|
|**Content**|Risk|Nothing — anyone can|Censorship, press, market split|
|**Texture**|Free|—|Critic language only; **derive, don't author**|
|**Market/scene**|—|Scene membership|What's left of your `GenreTag`|

Content tags are the odd one out and need their own table: they don't cost money, they don't need a studio, and they can _increase_ value through controversy. Everything else is a spend.

## Fixing your examples

**Drone** — already an archetype (Raga/modal drone). Keep both, but define: archetype = drone is the _form_ (_Tomorrow Never Knows_); tag = drone is a _texture_ inside a conventional song (_See My Friends_, _Ticket to Ride_). Tooltip it or designers will double-tag.

**Male/Female Vocalist** — not a tag. Derive from the performer record. You already need `VocalPerformers`, and gender is an artist attribute with real 60s market consequences (girl-group radio, teen-idol demographics, the duet format). Hand-tagging it invites desync. What _is_ worth a tag: **Duet** and **Mixed-gender group**, because those are configuration choices, not facts about a person.

**Chinese / Japanese / African instrumentation** — honestly, don't model these as peers of Indian. Indian instrumentation was a genuine 1965–68 commercial movement with a supply chain (London's Asian Music Circle literally supplied the players for _Within You Without You_). The others were near-nonexistent in charting 60s pop outside Martin Denny-style exotica. I'd do:

- `Indian instrumentation` — its own tag, full adoption curve
- `Exotica / non-Western percussion` — one tag with a free-text flavour field, covering everything else

Otherwise you've got three tags that fire twice a decade.

**Chamber section** — don't split by _type_, split by **size tier**, since that's what the money tracks:

|Tag|Players|Rough cost|
|---|---|---|
|Strings (quartet)|4|low|
|Strings (section)|8–16|mid|
|Full orchestra|30–60|high + conductor + arranger|

Same for brass: `Horn section (2–4)` → `Big band brass` → `Brass band`. One tag, one size scalar. That also lets _A Day in the Life_ be "Full orchestra + aleatoric technique" rather than needing a bespoke tag.

## The tag set

**Instrumentation** — gate = year available, cost = money

|Tag|From|Notes|
|---|---|---|
|Horn section|—|Stax/Motown staple|
|Strings / orchestra (3 tiers)|—|Needs an arranger; arranger is a hireable with a stat|
|Harpsichord|—|Fashionable 1965–67|
|Tack / honky-tonk piano|—|Cheap|
|Pedal steel|—|Country; Nashville session pool|
|Gospel choir|—|Access via church connections|
|Hand percussion / claps|—|Free, Motown signature|
|Fuzz guitar|1962|Maestro FZ-1; explodes 1965|
|Sitar|1965|_Norwegian Wood_, Oct 1965|
|Mellotron|1963|Rare until 1966|
|Tabla / tamboura|1966|Needs Indian-player access|
|Electro-theremin|1966|One guy in LA (Paul Tanner) — great access gate|
|Wah-wah|1967|Vox Clyde McCoy|
|Piccolo trumpet|1967|_Penny Lane_; needs a classical ringer|
|Electric sitar|1967|Coral — the cheap sitar substitute, which is itself a story|
|Moog|1968|_Switched-On Bach_; absurdly expensive + needs a specialist|

**Technique** — gate = studio capability + engineer stat, cost = hours

|Tag|From|Hours|
|---|---|---|
|Echo chamber / plate|—|Low; studio-specific character|
|Slapback|—|Low|
|Compression-as-sound|—|Low; Meek, Motown|
|Stereo ping-pong|1958|Low; reads gimmicky after 1964|
|Feedback|1964|Low|
|ADT / double-tracking|1966|Low — Abbey Road exclusive at first|
|Varispeed|—|Mid|
|Leslie on vocals|1966|Mid|
|Backwards audio|1966|Mid|
|Tape loops|1966|**High** — needs multiple machines + staff holding pencils|
|Phasing / flanging|1967|Mid|
|Edit assembly (splice)|—|**High** — _Good Vibrations_, _Strawberry Fields_|
|Aleatoric / orchestral chaos|1967|High + orchestra|
|Direct injection|1967|Low|
|Sound effects / found audio|—|Mid; clearance risk if sourced|

**Content** — gate = none, cost = risk

|Tag|Effect|
|---|---|
|Drug reference|BBC ban risk, US station-level bans (_Eight Miles High_, 1966), press cycle|
|Sexual content|Ban risk scales hard after 1967 (_Je t'aime_, 1969)|
|Profanity|Near-fatal pre-1968; retailer refusal (_Kick Out the Jams_, 1969)|
|Religious / blasphemy|Regional — devastating in US South|
|Political / protest|Splits market; _Eve of Destruction_ was banned **and** hit #1|
|Racial consciousness|Format-dependent; R&B radio ≠ Top 40|
|Anti-war|Rises sharply in value 1966→69|
|Death / tragedy|BBC-sensitive (_Leader of the Pack_)|
|Named-person satire|Defamation/legal event risk|
|Interpolation / quotation|**Publishing clearance** — a cost, not a vibe|

Two things I'd add here because they're very 60s:

**Perceived content.** A song can acquire a content tag it doesn't have. The FBI investigated _Louie Louie_ for two years over lyrics nobody could make out; _Lucy in the Sky_ was an acronym the writer denied forever. Model as a low-probability event that attaches a `Rumoured` content tag post-release — unanswerable, drives both bans and sales. It's free drama and it's historically accurate.

**Sanitised cover.** Content tags live on the **composition**, but a cover can strip them. That's the whole early-60s white-cover-version economy, and it slots into the cover system from round two.

**Texture** — derive these from the other tags plus Production level, don't let designers type them. Jangly, fuzzy, lush, sparse, raw, punchy, murky, cavernous, dry. They're for generating critic prose and scouting blurbs.

## The mechanic that makes tags worth building

Give every Instrumentation and Technique tag the same **lifecycle you already have on Genre**:

`Unheard → Novel → Fashionable → Ubiquitous → Dated → Retro`

- **First commercial release** using an Unheard tag: permanent innovation credit on the recording, big artist prestige, tag advances to Novel globally. Players can _race for firsts_.
- **Novel/Fashionable**: critic bonus, press attention
- **Ubiquitous**: no bonus
- **Dated**: critic _penalty_

Sitar is the perfect test case: +huge in late 1965, neutral by 1967, actively embarrassing by 1969. Wall of Sound: definitive 1963, exhausted by 1966. This gives you era-feel enforcement for free and it rewards A&R staff who can read the curve.

Two constraints to stop tag soup:

**Tags consume studio hours, and hours are the budget.** This is where your Pepper-vs-Zeppelin contrast actually comes from: ~700 hours vs ~36. Let tag count and tag cost be the primary driver of that number and the budget system mostly builds itself.

**Overcooking penalty.** Beyond ~4–5 technique tags, cohesion drops unless the Producer stat is high. A mediocre producer with tape loops, backwards audio, phasing and an orchestra should make a mess, because in 1967 most of them did.

## Attachment points

- **Composition**: content tags, word density, form
- **Recording**: instrumentation, technique, texture, production level, performers
- **Release**: mono/stereo, mix, edit length

**Mono/stereo is a release decision, not a tag**, and it's a good one — dual inventory, stereo at a ~$1 premium, mono deleted industry-wide through 1968. A player still pressing mono in 1969 looks cheap; one who went stereo-only in 1964 cut off half the market.

## Pet Sounds, with tags

This is the test that motivated the system:

|Track|Instrumentation|Technique|
|---|---|---|
|Wouldn't It Be Nice|Accordion ×2, strings, horns, mandolin-guitar|Edit assembly, echo chamber|
|God Only Knows|French horn, strings, sleigh bells, accordion|Round/canon vocal stack, echo chamber|
|Don't Talk|Strings (section), timpani|ADT-era vocal layering, echo chamber|
|Pet Sounds|Horn section, electric guitar, percussion stack|Echo chamber|
|Caroline, No|Tack piano (varispeeded), flutes, found audio (dogs, train)|**Varispeed**, found audio|

Archetypes stay conventional — bright pop numbers, mid-tempo pop songs, a slow ballad closer. The tag column is where the album lives. Which was exactly your point, and it means the hidden-information idea from last round now has something concrete to hide: **tags are the thing only high-Ear characters can see.**

Compare _Within You Without You_ — Indian instrumentation (tabla, dilruba, tamboura, swarmandal) + strings, requiring _access to Indian session players_, a gate most 1967 labels fail. And _Communication Breakdown_ — zero tags, 36-hour album, sells fine. Tags should be optional, not a tax on every record.

## Enum cleanup

Your current `GenreTag` is three systems wearing one coat. Suggested migration, preserving ordinals:

|Current|Goes to|
|---|---|
|WallOfSound, HornSection, Orchestral, LoFi|**ProductionTag**|
|Instrumental, Novelty, Topical, Protest|**LyricMode** (already decided)|
|Motown, GirlGroup, British, Skiffle, Jamaican, Rockabilly|**SceneTag** (keep — these are market facts)|
|Seasonal, Christmas, Halloween, Summer|**ReleaseTag** — timing mechanic, not a sound|
|Romantic|Delete — Lyric mode covers it|

Wall of Sound is worth a special note: it wasn't a technique anyone could apply, it was **Gold Star Studio's echo chambers plus Spector's arranger plus five guitarists**. Model it as a composite tag requiring a specific studio + producer, not a checkbox. Players who want it have to book the room.