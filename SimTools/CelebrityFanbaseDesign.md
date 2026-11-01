# Celebrity, fanbase and media fame: design note

Status: draft 2026-10-31, branch `media-atmosphere`. Phase 0 is built (`CelebrityFanbasePhase0Notes.md`), Phase 1
is built (`CelebrityFanbasePhase1Notes.md`), and Phase 2 is built and being screened (`CelebrityFanbasePhase2Notes.md`). The note answers six points the author raised after the
media-fame review. The terms used throughout are recognition, attachment, member spotlight and the career ladder,
as they stand today:

| Store | Today |
|---|---|
| `publicRecognition` (act) and `personalRecognition` (member) | Familiarity, built from chart milestones, landmark LPs and live draw. TV adds a little behind `--media-recognition`, capped at 0.06 a year. |
| Teen attachment by region (`MediaFanState.Attachment`) | Weekly heat with an 8-week half-life. Read only by media. |
| Member spotlight (`MediaFanState.Spotlight`) | Which faces the camera and photographers found. Media-only, never canonical. |
| `careerState` ladder | Chart trophies only: three hits and a #1 for Star, four #1s for Superstar. It drives AI budgets, royalty leverage, contract terms, drop decisions and the album prior. |

This note adds one new stock, the **following**, and reworks four writers and one classifier around it.

## 0. The governing decisions this note asks to change

`MediaAtmosphereDirective.md` was written to keep media cosmetic until proven safe. This design needs four explicit
amendments. They aren't loopholes, and each needs the author's sign-off before code:

1. **§10.3** says fan frenzy must never change "every subsequent album's penetration". A following exists to do exactly
   that: its members buy the next record.
2. **§12.3** routes all media demand through one ledger capped at 5-10% per title-week. Follower purchases are audience
   behavior, like chart demand, not media requests. They enter the same regional singles intent as ordinary demand,
   under the fixed purse, outside the media ledger.
3. **§12.4** forbids direct media writes to canonical recognition and personal recognition. TV already breaks this
   behind `--media-recognition`. This note extends it to press, fan magazines and member spotlight.
4. **§12.5** asks every genre to stay within 1 pt of control. A fanbase model will move genre and concentration on
   purpose (acts with followings repeat), so §12.5 needs historical targets in place of "no change" for the measures
   in §9.

The safeguards (§8) replace "media can't matter" with "attention and money are finite, and fans leave".

## 1. History: what the model has to reproduce

**Die-hard fans bought the next record before they heard it.**
- *Can't Buy Me Love* had about 1.7M US advance orders, 2.7M worldwide, before its 21 March 1964 release
  ([Guinness](https://www.guinnessworldrecords.com/world-records/70165-greatest-advance-sales-for-a-single)). That is
  the phenomenon ceiling.
- Lesser followings sustained runs:
  - the Dave Clark Five had 15 straight US Top 20 singles and 18 Sullivan dates
    ([Wikipedia](https://en.wikipedia.org/wiki/The_Dave_Clark_Five));
  - Ricky Nelson had roughly 30 Top 40 hits in 1957-62.
- **Fan clubs were large but faded within years.**
  - The first Elvis fan club reportedly reached about 60,000 members (secondary source,
    [ritchieyorke.com](https://ritchieyorke.com/?p=461)).
  - The Beatles' club secretary put membership at about 80,000 around 1965 and 11,000 by its close
    ([paulmccartney-project](https://www.the-paulmccartney-project.com/1972/03/the-beatles-fan-club-closes/amp/)).
    That is roughly a quarter lost each year once the records stopped feeding it.

**Television made stars, and the strongest route was weekly presence, not one night.**
- **Weekly series:**
  - Ricky Nelson's early records "benefited greatly from repeated exposure" on *Ozzie and Harriet*
    ([Wikipedia](https://en.wikipedia.org/wiki/Ricky_Nelson)).
  - Paul Revere & the Raiders became national stars on *Where the Action Is* (1965-67). "Just Like Me" reached #11 on
    that exposure and was followed by two Top 10s; Revere said the albums "were turning gold as fast as we could record
    them" ([Wikipedia](https://en.wikipedia.org/wiki/Where_the_Action_Is),
    [encyclopedia.com](https://www.encyclopedia.com/education/news-wires-white-papers-and-books/paul-revere-raiders)).
  - The Monkees' series (September 1966) produced four #1 albums in a row. The "outsold the Beatles and Stones" line
    is Nesmith's own admitted invention, so it is not a target
    ([todayifoundout](https://www.todayifoundout.com/index.php/2014/10/beatles-vs-monkees/)).
  - Bobby Sherman's *Here Come the Brides* (1968-70) put him on the teen-magazine covers before his 1969 hits
    ([Wikipedia](https://en.wikipedia.org/wiki/Bobby_Sherman)).
- **Dance shows:** Chancellor built Frankie Avalon and Fabian on *American Bandstand* exposure
  ([Wikipedia](https://en.wikipedia.org/wiki/Chancellor_Records)).
- **Variety breakouts:**
  - Sullivan's Beatles debut drew about 73M viewers, and the booking was agreed before the US #1
    ([Wikipedia](https://en.wikipedia.org/wiki/The_Beatles_on_The_Ed_Sullivan_Show),
    [edsullivan.com](https://www.edsullivan.com/artists/the-beatles/)).
  - The Smothers Brothers hour is credited, mostly in retrospectives, with boosting the Who, Buffalo Springfield and
    the Doors in 1967-69 ([bestclassicbands](https://bestclassicbands.com/smothers-brothers-comedy-hour-3-10-20/)). This is
    anecdotal, so use it for shape, not size.
- **Most one-off spots did little.** The few that broke an act combined a huge fresh audience, a fitting record already
  in the shops, and a performance people talked about.

**Fan magazines made individual faces famous.**
- *16* had about 250,000 readers in 1958 under Gloria Stavers
  ([Austin Chronicle](https://www.austinchronicle.com/music/dreamsville-11738969/)). Later figures of 1M+ copies a
  month are recollections, not audited circulation.
- *Tiger Beat* opens in September 1965 (directive §8.2).
- Group members became famous one by one: Davy Jones and Mark Lindsay are the textbook cases.

**Was the ladder historically media-blind? No. Labels paid for stature, and stature included TV fame.**
- Decca signed Ricky Nelson, a TV star whose chart run was already slowing, to a 20-year deal guaranteeing over $1M
  in January 1963 ([Billboard via mcrfb](https://mcrfb.com/archives/29775)).
- The Raiders renegotiated to become the stars of their show after their first Top 50 hit.
- Motown rationed promotion down a visible stature order
  ([substack account](https://rocknrollwithme.substack.com/p/behind-the-glamor-of-the-sixties), anecdotal).
- No source gives budget formulas. The pattern is still clear: contract value, promotion and LP programmes followed
  observed draw (advance orders, TV pull, crowds), not a count of #1s.
- The adult side shows the same: Mitch Miller's *Sing Along* series sold LPs on TV presence with no singles career.

## 2. The following: a durable fanbase

### 2.1 What it is

A following is the people who will buy an act's next record on sight, come to its shows and write in. It is counted in
**people**, by act, by market region, and by two audiences:
- **Young**: teen and young-adult viewers/buyers (`ViewerGroup.Teen`, `YoungAdult`; the Youth segment).
- **Adult**: `ViewerGroup.Adult`; the AdultMOR, Country, Gospel, Jazz and specialist segments.

Counting people gives every number a historical check (advance orders, fan-club sizes, crowds) and makes saturation
automatic: a region's following can't exceed the people in it who fit the act.

It sits beside the existing stores without replacing them:

| | Recognition | Attachment (heat) | Following |
|---|---|---|---|
| Question | Have they heard of the act? | Are they excited this month? | Will they buy the next one unheard? |
| Clock | Years (5.3-year half-life) | Weeks (8-week half-life) | Years (§2.3) |
| Built by | Hits, exposure | Sales near the top, TV, press | Buyers, viewers and readers converting while hot |
| Read by | Launch awareness, stock, radio | Media; becomes a converter (§2.2) | Launch demand, live draw, bookers, labels |

### 2.2 How a following grows

Each week, per act, region and audience, from things that actually happened:

```text
joiners = buyers    × cBuy  × heatFactor
        + registered viewers (§3) × cView × fit
        + magazine readers reached × cRead × fit       (§4)
        + live attendance × cLive
heatFactor = 0.3 + 0.7 × attachment           (adult audience: 0.3 + 0.7 × adult regard, §4)
following += joiners × (1 − following / fitPopulation)
```

Initial values, to be calibrated in Phase 0 against §1:

| Constant | Start | Why |
|---|---|---|
| `cBuy` | 0.10 | At full heat, one buyer in ten becomes a committed fan. Three Top 10s (~1.5M units) → about 50-150k followers, the Elvis-club scale. |
| `cView` | 0.02 | A weekly series reaches the same viewers repeatedly. Registration (§3) already removes repeats, so a series grows a following steadily rather than all at once. |
| `cRead` | 0.03 | Teen-magazine features about the act itself |
| `cLive` | 0.15 | The room is where die-hards are made |

- Heat is the **converter**, not the stock. That keeps today's attachment model and gives it a lasting consequence.
- A hit that sells cold (adult novelty, a one-off) leaves few followers.
- The same units sold during a frenzy leave many.

### 2.3 How a following shrinks

| Loss | Rate | Why |
|---|---|---|
| Drift, young | 15% a year, plus ageing (below) | Beatles club 80k → 11k over 1965-72, about 25% a year once ageing is counted. Teens grow up and move on. |
| Drift, adult | 10% a year | Adult audiences stay put |
| Silence | +15% a year after 12 months with no release | Fans wander when nothing comes out |
| Disappointment | 5-20% of the following at a release whose quality falls clearly below the act's recent average | The 60s pop-group cliff: fans carry a few weak records, then leave |
| Switching | Young followers lost in proportion to the attention a new sensation takes in their region (§7) | Fans move to the new thing |
| Controversy | Per §4.4 | |

**Young fans grow up.**
- A young following carries a cohort: the year its members joined, assumed to be about 16 then.
- Each year, members past about 22 leave the young pool. Half stay as adult followers and half lapse.
- Adult followers buy LPs rather than singles (§2.4). So 1964's teen fans become 1969's adults who still buy the act's
  albums, which is how a 1964 beat group keeps an LP audience after its singles stop.
- Ageing replaces part of the young drift. The total young loss should still come to about 25% a year, to match the
  fan-club figures.

Disappointment is what keeps a following from being a permanent franchise. It reads the record's quality, which the
sim already has, against what the followers came for.

### 2.4 What a following does

**1. Release-on-sight demand (the economic reader).**
- When the act releases, each region's followers add launch intent spread over the first three weeks:

  | Release | Young | Adult |
  |---|---|---|
  | Single | 0.35 per follower | 0.10 |
  | LP | 0.12 | 0.30 |

- That intent joins the record's regional singles or LP intent before clearing. It competes inside the existing fixed
  purse ([[singles-volume-is-a-fixed-purse]]), so fans move units toward their act rather than creating units.
- Dealers see it as **advance orders**. The launch-stock arm reads it, so a fanbase act ships with stock on the shelves.
  That stock is the observable a label, dealer or rival can see.

**2. Live draw.** Followers in a region raise room and tour attendance. Live draw already writes a little recognition.

**3. Bookers.** Following replaces "fans already asking" (today a 1.1-1.4x weight from the episode state) with a
continuous term. A big following gets a standing booking with no current record. That is the Dave Clark Five's 18
Sullivan dates.

**4. Labels.** Following, advance orders and recognition become the evidence for stature (§6).

**5. The player sees it the way a 1960s label did.**
- **The manager's fan-club report.** The act's manager tells the label how the fanbase is looking: club membership
  (about 3% of the following), whether mail is rising or falling, and the strongest regions.
  - It is reported, not exact. It comes monthly, carries a little noise, and an act whose manager is a booster may
    round it up.
- **Advance orders.** Dealers' orders for a new release arrive before it ships. They are the hard number.
- Rivals' followings stay hidden except through public evidence: crowds, covers, press.

## 3. TV breakouts: replace the yearly cap with registration and attention

The 0.06 yearly cap stops any single night from mattering. Its job, keeping one act from owning the chart, can be done
by three limits that are true to life instead.

### 3.1 Registration: who remembers the act

An airing's recognition gain becomes the share of the population who saw the act fresh **and registered it**:

```text
registered = freshReach × r0 × fit × performance × novelty × attention   (per region and audience)
gain       = registered / population       (then AddPublicRecognition's diminishing returns)
r0 = 0.25; performance: Breakdown 0.6 … Engaging 1.15, Electrifying 1.6
novelty = 1.0 for the act's first national exposure, falling toward 0.4 with national dates already seen
attention = §7, below 1 when other acts are drawing the country's attention that week
```

- **A routine Sullivan spot:** 14% fresh × 0.25 → about 0.035. Close to today's 0.022, so ordinary bookings stay upkeep.
- **The Beatles night:** about 38% of the population, nearly all fresh, × 0.25 × 1.6 × 1.0 × fit ≈ **0.15 before
  diminishing returns**. One night makes a household name.
- **A weekly series:** registration falls as the same viewers return, but the series keeps converting viewers into
  followers (§2.2) every week. That is the Nelson, Raiders and Monkees route.

### 3.2 The three limits that replace the cap

1. **People, not appearances.** Fresh reach is already tracked (`RecognitionReach`). An act can't register more people
   than have seen it, and returns to the same audience add almost nothing.
2. **Fame isn't sales.** Recognition reaches launch only through the existing bounded lifts (+0.115 awareness, ×1.30
   stock, +0.075 radio at full recognition). A household name with a weak record still has a weak record. TV converts
   to sales only through followers (§2.2) and the demand ledger's short impulse.
3. **Attention is finite (§7).** Two acts breaking the same month share the country's attention, so breakouts can't
   stack without bound.

### 3.3 Series residencies

The catalog leaves out programs with a resident cast (*Sing Along with Mitch*, Lawrence Welk, the King Family), and no
act can hold a series. Add **residencies**: an act on a program every week for a season.
- For an existing program (*Where the Action Is*), it books the act as a regular.
- For a new series (*The Monkees*), it creates a program the act headlines.

A residency is rare and offered on recognition and fit, not pitched like a guest spot. Its weekly reach drives
following growth through `cView`. This is the single biggest historical route from TV to stardom.

It is open to the player from the start, as a negotiation. A producer approaches an act with a following or a fitting
image, or the label pitches its act for a regular slot. The terms to settle:
- the season's length;
- scale fees to the performers (through member wealth, as now);
- whether the act must play the show's material;
- how much of the act's diary it takes. A weekly taping limits touring, which trades live draw for TV reach.

### 3.4 Breakouts: event audiences and registration fit (Phase 2c)

> **Outcome:** screened and not adopted. Breakouts are scoped to phenomena (§3.4.6). §3.4.1-§3.4.5 record what was built
> behind `--event-hype`.

Phase 2's screen (`CelebrityFanbasePhase2Notes.md`, *What keeps the biggest airings small*) found no breakout in eight
world-years. The causes:
- no airing's fresh audience passed 0.17 of the country;
- registration fit ranks a teen act lowest on a family show (0.35 on Sullivan).

Residencies (§3.3) change neither, so they come first, as their own phase.

**Two limits, two fixes:**
- **The night's audience is fixed.** The exposure model already enlarges an event night: hype → `eventLift` (share
  taken from rival programs, up to 0.90) and `tuneIn` (people who weren't watching, up to 0.35 of them). But hype
  never passes 0.25, the point where lift begins. Across 51 headline events in 1963-66 the maximum was 0.25. Hype is
  `Precursor × novelty × (0.3 + 0.7 × advance coverage)`, and the precursor needs six weeks at No. 1, twice an
  ordinary No. 1's sales and other hits on the chart. An ordinary No. 1 gives about 0.2 × 0.85 × 1 ≈ 0.19 at most. Fan
  episodes only cap hype (`MediaFans.HypeCap`) and never add to it, although the code marks them as meant to join it
  here. The one act that became a National Sensation, three times in 1964-65 on seed 1001, had 2-4 week spells and was
  never on Sullivan during one.
- **Registration fit is following fit.** Registration reuses `MediaFollowing.AudienceFit`, the share of an audience
  who could *become fans*. But recognition is familiarity, not esteem (`MediaRecognition`'s own rule: a disaster makes a
  face known too). Whether a family audience remembers a teen act is not whether it would join its fan club.

#### 3.4.1 Event audiences: the public's frenzy draws the audience

The exposure model, its ceilings (0.90 lift, 0.35 tune-in) and the hype cap by episode state all stay. Three changes
feed them:

1. **Fan episodes raise hype.** Precursor′ = max(Precursor, EpisodeHype), where EpisodeHype reads the strongest state in
   the 8 weeks before the air date:
   - Regional Favorite 0.35;
   - National Sensation 0.70;
   - Cultural Phenomenon 1.0.

   Taking the strongest state over 8 weeks means a booking made in the frenzy still counts after a short spell cools.
   Novelty and the coverage term apply as now, and `HypeCap` still bounds the result. A sensation can't reach the
   phenomenon ceiling.
2. **Bookers chase a sensation.** On entering National Sensation (or Phenomenon), the act is offered a headline hold
   (`PlaceHolds`) on the largest primetime variety program with a free hold in the next 3-6 weeks. History: the Beatles'
   Sullivan date was agreed before their US No. 1 (§1). The hold's announcement is what lets the press build advance
   coverage, so it also raises the coverage term. This applies to rival acts. The player's act gets the same approach
   as an offer.
3. **Novelty stays strict.** A return to the same program (0.4) or to primetime at all (0.85) still damps hype, so an
   act gets about one event night per program.

**Expected sizes** (Sullivan's base fresh audience 0.166; estimates to be confirmed by the screen):

| Act at air date | Hype (coverage 0.6) | Lift / tune-in | Fresh audience |
|---|---:|---:|---:|
| Ordinary No. 1 | ≤ 0.19 | 0 / 0 | 0.166, as now |
| National Sensation, first primetime date | 0.50 | 0.30 / 0 | about 0.22 |
| Cultural Phenomenon, full coverage | 1.0 → capped 1.0 | 0.90 / 0.34 | about 0.35-0.40, the Beatles' 38% |

#### 3.4.2 Registration fit: familiarity ignores taste

Registration fit becomes a constant, **`RegistrationFit` = 0.6** for every genre and audience. Taste stays where it
belongs, in who joins the following (`AudienceFit`, §2.2). Performance and novelty still vary by night.

- A routine first Sullivan spot registers 0.166 × 0.25 × 0.6 ≈ **0.025**, back to the design's original upkeep size
  (§3.1, "close to today's 0.022"). Today it is 0.012-0.020.
- TV's yearly total rises by about 0.6 ÷ 0.46 ≈ 1.3×, to roughly 3-4 a year. That is still below the old capped writer's
  4.9-6.4.
- The biggest change by genre is Soul and TeenPop acts on family shows, which roughly gain 60-70%. Watch genre share:
  Phase 2 already showed Soul +1.3 on one seed.

#### 3.4.3 What makes a breakout

Breakout bar 0.045 raw (a Top 10's worth), r0 0.25, fit 0.6, novelty 1 for a first national date:

| Night | Fresh | Routine (1.0) | Engaging (1.15) | Electrifying (1.6) |
|---|---:|---:|---:|---:|
| Any routine Sullivan date | 0.166 | 0.025 | 0.029 | 0.040, **no** |
| Sensation's first event night | 0.22 | 0.033 | 0.038 | **0.053** |
| Phenomenon's event night | 0.35-0.40 | **0.053-0.060** | **0.060-0.069** | **0.084-0.096** |

- No routine booking breaks out, however well it goes. The cap's job is kept without the cap.
- A sensation breaks out only on an electrifying first event night. That puts the rate in the §9 band ("a handful a
  decade") if electrifying nights among event headliners run at a few tens of percent. The screen measures that rate
  first. The calibration knob is the sensation's EpisodeHype (0.70), not r0, so routine spots stay put.
- A phenomenon breaks out on any decent night: the Beatles case. The design's 0.15 for that night (§3.1) assumed fit 1.
  At fit 0.6 it is 0.08-0.10 before diminishing returns, still a household name in one night.
- The feedback loop (event → recognition and followers → a longer sensation → the next event) is bounded by:
  - novelty: one event night per program;
  - `HypeCap` by state;
  - attention (§7.3), which binds once two acts are hot at once.

#### 3.4.4 Switches, telemetry and screens

- **`--event-hype`**, off unless named. It changes audiences, so it changes the demand ledger and followings in causal
  mode, and it must be its own switch, A/B'd separately. Shadow (`--observe-media-atmosphere`) stays byte-identical to
  the ref.
- **`RegistrationFit`** applies under `--fame-registration`. Phase 2 is still uncommitted, so it replaces the reused
  `AudienceFit` there rather than adding another switch.
- **Telemetry:**
  - `MEDIA_EVENT` gains precursor, EpisodeHype, the state read, novelty and coverage. That settles whether coverage
    is ever nonzero for a hot act.
  - New `MEDIA_EVENT_HOLD` per chased booking.
  - `MEDIA_TV_REG_DECOMP` stays, for fresh-audience and fit distributions.
  - **TV-made stars** (§9): an act's first Top 40 within a year of a breakout.
- **Screens:**
  1. Fixtures: the three table rows above; a cooled sensation still lifts within 8 weeks; a second event night on the
     same program is damped.
  2. Shadow inertness, 104 weeks against `ref-f50b9a4` (with `--lean-probe`).
  3. Causal 1963-66 resumes, both seeds, against `fr2-t`. Measure breakouts, sensation spells and how many got an event
     night, electrifying rate among event headliners, TV share of recognition, and the Phase 2 economy table. Kill rule
     as before (top-10% slot-weeks +3 pt).

#### 3.4.5 Book the rising act (added after the first screen)

Sensations had always played Sullivan as chart guests before they were chased, so return-visit novelty kept every
chased night routine. A second route books an act the public is warming to (a Regional Favorite or more in the last
8 weeks) that has never played a primetime variety show:
- its first network date is announced as the event, at hype 0.75;
- it plays no other primetime variety show until then (Sullivan's exclusivity).

#### 3.4.6 Outcome: breakouts are scoped to phenomena (author, 2026-11-01)

Both routes were built and screened (`CelebrityFanbasePhase2cNotes.md`):
- **The sensation chase** never lifted an audience.
- **The rising-act route** produced real event nights, including two 41.5M Sullivan nights against the usual 20-27M.
  Neither route produced a breakout:
  - no rising night was Engaging;
  - daytime network shows had already shown the headliner to much of the audience;
  - the lift goes to the whole bill.
- **Economy:**
  - Soul gained on both seeds from the constant fit;
  - 1966 albums fell on both seeds (−3.7%, −4.8%).

**Decision:** single-night TV breakouts are a phenomenon's event, 0-1 a decade and none on most seeds, like
phenomena themselves (§11, decision 1). TV-made stars come through followings and residencies (§3.3, Phase 3), not
single nights.
- The Phase 2c code stays behind `--event-hype`, off, as a screened experiment, and the constant registration fit
  moves behind the same switch.
- Without it, registration reads `AudienceFit` exactly as Phase 2 was screened.

## 4. Press, fan magazines and controversy as fame writers

Every item writes through one **root-event budget**, which already exists in the press model. A story and its
syndicated retellings count once, at the strength of the widest outlet that ran it.

| Source | Writes | Size |
|---|---|---|
| Teen fan magazine feature, pin-up or cover | Young attachment and following in its circulation regions; **member** spotlight → `personalRecognition` (§5) | Circulation × reading share × fit × `cRead`; a cover is 3× a feature |
| Mainstream magazine (Life, Look, Time, Newsweek) | Act recognition among adults; adult regard (new) | Circulation × 0.2 registration. Only for national sensations, phenomena, calamities and big stories (existing gate). |
| Local daily | Act recognition and young or adult attachment in that region | Small; the first clipping |
| Specialist and community press | Recognition and following in that audience only (Soul in the Black press, Country, Gospel, College) | Circulation-scaled |
| Trades | Nothing to the public. They stay business evidence (directive §8.1). | — |
| Critics (1966+) | `culturalStanding`, not recognition | Small, replacing no other writer (directive §8.4) |

### 4.1 Adult regard

Attachment is teen-only by design. Adult audiences need their own heat, **regard**, built from adult TV viewing, adult
press and LP buying. It plays the converter role for adult followings, so Sinatra, Mathis and Mitch Miller build
followings the same way teen idols do, through different channels.

### 4.2 Controversy

Today `controversy` is a random record field and only UI reads it. A controversy becomes an event with a root, and
directive §7.4 already defines its stages. Its fame effects:
- **Recognition always rises.** Notoriety is familiarity.
- **Followings split by audience.** The adult following and adult regard fall. The young following loses its marginal
  members, while the act's core may harden (its disappointment churn drops for a year).
- **Booking access falls.** Program floors rise for the act until a review date (the Doors after Sullivan).

Supply effects such as station bans and retailer refusals stay out until their own adapter exists (directive §12.4).

### 4.3 Critics: reviews become the channel acclaim runs through (Phase 2b)

Author's decision, 2026-11-01: critics should raise standing, but through the existing acclaim pipeline rather than as a
new writer.

**Today.**
- `culturalStanding` has three writers:
  - landmark albums (`AlbumLegitimacyService`, 0.12 × strength);
  - influence: another act converting on the record (`CulturalMemoryService`, 0.008 × strength);
  - the 1960 opening seed.
- `criticalAcclaim` is the sim's internal critics. Each completed chart run moves it by the record's craft
  (`ArtistCriticalAcclaimService`), more for a high-craft record that didn't sell. The landmark test reads it, so acclaim
  already reaches standing.
- The media critics are separate and write nothing:
  - they review only records entering the chart, in critic-fit genres;
  - their verdict is 0.65 × craft + 0.35 × the critic's own taste;
  - their "craft" is hook and production, not the merit craft that acclaim and landmarks read.

**History.** Critical regard was real but specialist and late:
- jazz reputations were made in *Down Beat*;
- folk reputations in *Sing Out!* and a few newspaper critics (Shelton on Dylan, 1961);
- pop and rock had no critics of their own until *Crawdaddy!* (1966) and *Rolling Stone* (late 1967). Their readerships
  were tiny, but the readers were musicians, FM DJs and A&R staff;
- the trades reviewed nearly everything, briefly. Billboard and Cash Box picks were the baseline every release met.

Much of the 1960s canon was built after the decade. A model that lets critics mint big reputations inside it overshoots.

**Design.** The same craft, the same pipeline; reviews decide how much of it is heard.

1. **Reviews read the merit craft.** A critic's verdict is `ArtisticMeritService.GetCraft` for the record + τ × (taste −
   0.5). Centering taste on zero keeps the mean verdict on craft, so critics add disagreement, not drift.
2. **Reviews reach non-charting records.** At release, LPs in critic-fit and specialist genres become review leads for
   critics and specialist papers whether or not they chart. Without this the underrated case can't be reviewed.
3. **Acclaim is earned only as far as the record was heard.** At a completed run, the positive acclaim delta is scaled
   by coverage:
   - coverage = 1 − ∏(1 − authority) over the outlets that reviewed the record;
   - the trades' picks give every release a floor;
   - the craft term reads the authority-weighted verdict in place of raw craft.

   Erosion (the negative delta for anonymous product) is not scaled: nobody has to review a record for it to be forgotten.
4. **Authority, not circulation.** Each critical outlet carries an authority for its own audience, rising through the
   decade. Initial values to calibrate:

   | Outlet | Authority | Audience |
   |---|---|---|
   | Trades' picks (floor) | 0.5 | All |
   | *Down Beat* | 0.6 | Jazz |
   | *Sing Out!*, *Broadside* | 0.5, 0.2 | Folk |
   | Black press (*Jet*, *Ebony*, *Defender*) | 0.3 | Soul and R&B |
   | *Village Voice* | 0.15 → 0.35 by 1967 | Critic-fit genres |
   | *Crawdaddy!* | 0.3 from 1966 | Critic-fit genres |
   | *Rolling Stone* | 0.3 → 0.6 by 1969 | Critic-fit genres |

5. **Standing still has its three writers.** Critics reach it only through acclaim → landmarks and influence. Nothing is
   counted twice, and §12.4 needs no new amendment for standing. Acclaim's own write changes source, so it gets its own
   switch and causal test (directive §8.4).

**Readers to watch.** Acclaim feeds:
- the landmark test (standing, plus a small recognition deposit);
- `ArtistEvolutionPressureService`, so artists' choices.

Landmarks feed launch through `EffectiveRecognition` (standing × 0.20). So this is causal and economic.

**Measures.**
- Landmarks: 25-40 a decade, unchanged in total ([[landmark-album-is-body-of-work]]). The genre mix may move toward jazz
  and folk before 1966 and toward critic-fit rock after.
- A few landmarks a decade from records that never reached the Top 40.
- Acclaim by genre and year: pop acclaim falls before 1966.
- The causal pair's units, Major share and genre.

**Author's answers (2026-11-01).**
- The trades' picks (Billboard, Cash Box) count as reviewers and give every release the coverage floor.
- Erosion stays unscaled: a forgotten record loses acclaim whether or not anyone reviewed it.

## 5. Member fame: spotlight writes personal recognition

A face on a teen-magazine cover is remembered.
- Each camera share (TV) and each photo session or cover (press) deposits into the member's `personalRecognition`,
  sized by registered viewers or readers × that member's share, with the same diminishing returns as the act.
- The act still gets its own deposit. The member's is **extra**: it represents fans who know the drummer's name.
- `MediaFanState.Spotlight` stays as the weekly "who's hot" signal for press angles.

Two consequences to watch, both intended:
1. **Solo spinouts get easier for camera-loved members** (the solo bar is 0.05 of personal recognition). Measure solo
   spinouts by spotlight share in Phase 1.
2. **Spotlight strain in band life rises.** It already reads personal-recognition share. This is the Davy Jones story,
   but check it doesn't break up groups early.

This phase also fixes audit finding 10: `SpotlightThem` and `SoloSingleYes` currently mint fame in the office. They
should promise the camera share instead, and the fame arrives when an audience sees it.

## 6. Stature in place of the trophy ladder

History says label investment followed draw, and draw included TV fame (§1). The ladder's readers are some of the
economy's most sensitive inputs:
- `AILabel` royalty leverage, contract term, and drop protection (Superstars are never dropped);
- `GetMarketingBudget` and the album prior;
- release cadence.

So this is the **last** phase and gets its own A/B.

- Keep `careerState` as the act's operating label and for UI.
- Add an observed **stature** in [0,1] that AI readers use in place of the rung:

```text
stature = max(chart form (last two years' peak units, as now),
              advance orders / follower size relative to the national top,
              recognition × 0.7)
```

  `max` lets each route to stature count on its own: the chart act, the TV-made idol with a following, and the adult
  LP star.
- Map stature back to the existing budget and leverage tables so the control's money is unchanged at the same rung.
- **Link to the Phase 5 decade problem:** stature is how Majors historically bought stars from smaller labels (Decca
  and Nelson). Bidding on stature, not on trophies, is a plausible route to the 1968-69 consolidation goal. It is also
  the riskiest piece here economically ([[big5-entry-edge-is-campaign-impact]]).

## 7. Attention, sensations and phenomena

### 7.1 One continuous measure replaces the gate stack

Today a phenomenon needs four consecutive weeks of extraordinary records, five hot regions, national exposure and two
evidence families all at once. It never happens, and the reason is the coincidence of conditions, not rarity.

Replace the stack with **salience**: each act's share of the nation's attention, smoothed.

```text
weekly share_a = w_sales × (a's share of national singles + LP units, young and adult weighted by audience size)
               + w_tv    × (a's share of registered TV viewers this week)
               + w_press × (a's share of circulation-weighted coverage, root-deduplicated)
               + w_live  × (a's share of national attendance)
salience_a = salience_a × 0.89 + share_a × 0.11      (≈ six-week half-life, a leaky integrator)
breadth_a  = number of the 4 families where a's share > 3 × an average charting act's
             + 1 if a is above that bar in both audiences (young and adult)
```

### 7.2 The states it defines

A phenomenon means Beatlemania: total cultural dominance, about once a decade at most. The Monkees, the Dave Clark
Five and Herman's Hermits were national sensations, not phenomena. So the phenomenon test is **dominance**, an act far
ahead of everyone else, not just a high share:

| State | Rule (enter / leave) | Initial values |
|---|---|---|
| Regional favorite | Hot attachment in one region (existing) | unchanged |
| National sensation | measured on **young** attention: young salience ≥ θs, young breadth ≥ 2 (young sales, young TV, press), young share ≥ adult share / leave below 0.6 θs | θs = 3% of young attention (Phase 0 calibration) |
| Cultural phenomenon | salience ≥ θp, **and** ≥ D × the next act's salience, **and** breadth 5 (all four families plus both audiences), held for 8+ weeks / leave when any test fails for 8 weeks | θp = 8%, D = 3 |

- **Persistence comes from the integrator, not from consecutive weeks.** One huge week counts, a run of strong weeks
  counts, and a single dip doesn't reset it.
- **The fixed 56-day cooldown is replaced by hysteresis.** A new record can relaunch an act straight away.
- **Crossover to parents is required.** Beatles coverage in Life and adult viewers on Sullivan are part of the
  history. A pure teen craze tops out at sensation, however big it gets.
- **A sensation leads in the young audience** (author's decision, 2026-11-01). The 1960s sensation is a youth craze, not
  a Sinatra-style adult following. An adult act with broad sales and television is a star, not a sensation; it reaches
  the top only as a phenomenon, through crossover to the young.
- **Dominance makes it rare by construction.** Only one act can be 3× everyone else at a time, and a busy field (the
  1965-66 British wave) produces several sensations and no phenomenon.
- **Calibrate on dominance, not on a count.** A 0-1 target can't be tuned by counting occurrences. Tune D and θp on
  the distribution of the top act's lead over the second act in observe-mode decades, so a typical seed's best year
  falls short of the bar and only an outlier career clears it.
- **Calibration targets:**
  - phenomena: 0-1 per decade, with most seeds having none;
  - national sensations: 10-25 per decade;
  - none forced.

### 7.3 What salience feeds

- `attention` in §3.1: registration falls in a week when other acts hold the country's attention.
- Switching churn in §2.3.
- Press appetite: mainstream editors' existing gates.
- The demand ledger's 10% frenzy ceiling: sensation or phenomenon, as today.

## 8. Safeguards, in one place

| Risk | Guard |
|---|---|
| One act owns the chart | The fixed singles purse: fans redistribute units, they don't create them. The kill rule (top-10% acts' slot-weeks ≤ +3 pt). |
| A franchise that never ends | Drift, silence and disappointment churn (§2.3). Young followings age out. |
| TV fame snowballs | Registration counts people, novelty decays, and the attention share is finite (§3, §7) |
| The same event counted three times | Root-event budget across TV, press and syndication (§4) |
| Fame without demand inflating sales | Recognition reaches launch only through the existing bounded lifts. Follower intent clears against physical stock. |
| Phenomena everywhere | Salience is a share of national attention, and a phenomenon must dominate the next act by D. Calibrated to 0-1 a decade with no quota. |
| Economic drift hidden under fame | Each causal phase has its own paired A/B, with the §9 targets replacing "no change" |

## 9. Measures and historical targets

| Measure | Target | Source in the sim |
|---|---|---|
| Repeat-hit share: Top 40 entries by acts with a Top 40 in the prior two years | From the recovered 1960-64 Hot 100 ([[historical-hot100-1960-64-recovered]]) | records + lifecycles |
| Consecutive-hit runs: acts with 5+ straight Top 40 singles | History, same source | records |
| Advance orders | Top act 1-2M in a phenomenon year; a solid fanbase act 100-300k | New: launch follower intent |
| Following size | Top teen act 1-3M; fan clubs (a slice of it) 50-80k | New |
| Following half-life after the last hit | 2-3 years young, 5-7 adult | New |
| Phenomena / sensations per decade | 0-1 (most seeds none) / 10-25 | Salience states |
| TV single-night breakouts (an airing worth a Top 10 on its own) | 0-1 a decade, a phenomenon's event; most seeds none (§3.4.6) | `MEDIA_TV_BREAKOUT` |
| TV-made stars (first Top 40 within a year of a residency's start) | A handful a decade | Media telemetry |
| Solo spinouts from camera-loved members | Rises; keep within band-life bounds | Band life |
| Top-10% slot-weeks, CR4, Major share, genre | Kill and report as now. Genre moves are allowed where followings explain them. | Existing |

## 10. Phases

Each phase is short-screened, then decade-checked at the end of the branch ([[decade-ab-is-one-paired-seed]]).

| Phase | Builds | Mode | Evidence |
|---|---|---|---|
| 0 | Following stock (inflows from today's sales, attachment and TV only), adult regard, salience; telemetry | Observe-only, byte-identical economy | Following sizes, half-lives and salience states against §9 targets; calibrate `c*` and θ |
| 1 | Spotlight → `personalRecognition`; office fame-minting fix | Causal (small) | Solo spinouts and band strain by spotlight share |
| 2 | Press, fan-magazine and controversy writers; registration model replacing the 0.06 cap | Causal | Recognition distribution, TV share of recognition, breakouts per decade |
| 2b | Critics: reviews gate and season acclaim (§4.3) | Causal | Landmarks a decade and their genre mix, acclaim by genre, units and Major share |
| 2c | Breakouts: fan episodes raise hype, bookers chase sensations and rising acts, constant registration fit (§3.4) | Screened, not adopted; kept behind `--event-hype` | Breakouts scoped to phenomena instead (§3.4.6) |
| 3 | Residencies | Causal | TV-made stars; following growth on series |
| 4 | **Follower demand at launch, advance orders, live draw** | Causal; the big one | Repeat-hit share, runs, concentration, genre, album units |
| 5 | Stature for AI readers | Causal | Budgets, contract terms, Major share, closures |

Phase 0 is cheap and answers the main calibration question first: given today's sales, how big would followings be,
and who would have them?

## 11. Decisions (author, 2026-10-31)

1. **Phenomena mean Beatlemania.** 0-1 a decade, decided by dominance (§7.2). The Monkees-scale acts are sensations.
2. **The player sees a fan-club figure,** reported by the act's manager, plus dealers' advance orders (§2.4).
3. **Residencies are open to the player** as a negotiation from the start (§3.3).
4. **Young fans age into adult fans** (§2.3).
5. **Stature is in scope.** This is the media and celebrity branch (§6).
6. **No standalone attribution decade before this work.** The Phase 4 demand adapter and Recovery survive this design
   unchanged, but the recognition writer is replaced. The open question, how targeted demand cuts Major share, gets a
   log diagnosis on the existing 1963 resume arms. The paired decade A/B runs once, at the end of the branch, on the
   final writers.
7. **Critics count, through acclaim** (2026-11-01). Reviews become the channel through which a record's craft earns
   acclaim, and standing follows through landmarks and influence as before (§4.3, Phase 2b). No direct standing write.
8. **TV breakouts are a phenomenon's event** (2026-11-01). Single-night breakouts are 0-1 a decade. TV-made stars
   come through residencies and followings (Phase 3). Phase 2c's routes stay off (§3.4.6).
