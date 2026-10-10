# Contact Network and Session Employment

Branch: `band-member-simulation`. This picks up two handoff items from `LocalScenePhase7Handoff.md`: "full
employment/opportunity modeling beyond named-room slots" and "wider DJ, record-shop, distributor, artist and rival
contact networks". It follows `LocalSceneEcosystemDirective.md` §9 ("persist former bandmates, mentors, writing
partners, support bills, house-band service, repeated sessions … allocate sparse edges when events occur, not a dense
all-person graph") and §17 ("house bands/session communities: musicians bridge jobs; trust and repeated
collaboration build recognizable records").

Testing follows the standing rule: treatment arms only, against a stored reference at the base commit. The decade
comparison runs once, at the end, together with the live-calibration work.

## 1. Starting point

- No person-to-person relationship exists. Pair strain is computed on the fly inside a lineup; romance and alumni are
  events and per-act lists. Nobody can be hired because someone knows them.
- Pool hiring (`FindPoolReplacement`) scores role, region, scene/genre, age and skill.
- AI records have no session players. A solo singer's record is made by nobody but the singer.
- Pooled people have no work: no income, no growth, and they leave music after four years.
- 1960, seed 1001: ~5,200 release decisions a year; the pool peaks near 80 people (213 entries, 155 hired back out).

## 2. Phase 1 — built

### 2.1 Contact edges (`ContactNetworkService`, `--disable-contact-network`)

An undirected edge `{A, B, kinds, firstYear, lastYear, jobs, fallout}` is written only by real events:

- **Former bandmate.** A member leaving an act (`RemoveFromAct`: departures and dissolutions alike) is linked to each
  remaining active member; `jobs` is the years they overlapped. Acrimony, firing and a walkout mark a fallout.
- **Session.** Each session crew is linked pairwise and to the act's active members.

Edges where either person has died or retired are pruned at the year end. Saved in `WorldSaveData.ContactNetwork`;
an older save starts with no edges.

Readers:

- Pool replacement: +1 for knowing a current member without a fallout (the weight of a region match).
- Recombination: a contact counts as local.
- The player's audition list (Band Room) ranks the same way.

### 2.2 Session employment (`SessionEmploymentService`, `--disable-session-employment`)

- **Who hires a crew:** an AI record by an act with fewer than two active rhythm players (guitar, bass, drums,
  piano, organ, multi-instrumentalist): solo singers, vocal groups, a singer with a horn player.
- **Size:** 1 session for a single, 3 for an album; a crew of 4, or 2 for folk.
- **Where:** the label's home city (`label.geography.basePlaceId`). A label with no mapped place hires nobody.
- **Who is eligible:** the city's session community, rebuilt yearly. That is members of acts based there
  (moonlighting) and pooled players based there, who are strong (`technicalSkill` ≥ 0.68, band life's session-work
  bar), read (`sightReading` ≥ 0.55, the Band Room session-hire bar) and play a rhythm instrument.
- **Who is chosen:** ranked by reading + skill + a regular's bonus (up to +0.5 at ten dates for that label) + a small
  keyed jitter. A player can do at most 10 sessions a week: a physical limit, not a quota. Standing crews (the
  Funk Brothers, the Nashville A-Team) emerge from the regulars bonus rather than being authored.
- **Pay:** $50 a session in 1960 dollars, rising with CPI. Derived: the 2019 AFM three-hour sideman scale ($434.21)
  deflated by CPI-U. No 1960s scale sheet was found; the Phonograph Record Labor Agreement was national, so there is
  no town adjustment. A quarter reaches wealth, like live pay. Members bank it in the act pass; pooled players in a new
  `MemberWealthService.OnPooledYear`, which also gives pooled people the ordinary spending sink.
- **Staying in music:** a pooled player who worked a session this year does not expire from the pool.
- **Not touched:** record quality and label costs. Label recording budgets already stand for what a session cost.

Diagnostics (`--contact-network-audit`): `<run>-contact-edges.csv`, `<run>-session-work.csv`,
`<run>-session-regulars.csv`, and a `CONTACT_NETWORK_CENSUS` line (edges by kind, fallouts, hires through a contact,
crewed and uncrewed records, distinct session players).

## 3. Results — Phase 1

`net-v2` (104 weeks, seeds 1001/2002, treatment only) against `ref-b3cc1ce-{1001,2002}-104`, canonical flags plus
`--aggregate-only --scene-room-audit --log-band-life-members --contact-network-audit`. Analysis:
`py SimTools/analyze_contact_network.py net-v2 live-inc-v1-live`.

`net-v1` found a wiring bug: most AI release paths call `ReleaseRecord(record)` with no label, so no record was ever
crewed. The service now resolves the label from `record.labelId`.

| | seed 1001 | seed 2002 |
|---|---:|---:|
| Records crewed / uncrewed (no mapped label city), 2 years | 8,622 / 325 | 7,822 / 181 |
| Session players, 1960 | 289 | 312 |
| Player-sessions, 1960 | 26,232 | 23,818 |
| Sessions per player, median / p90 / max | 43 / 269 / 530 | 27 / 225 / 528 |
| Top-10 players' share of all sessions | 18.3% | 20.9% |
| Session pay, 1960 | $1.31M | $1.19M |
| Labels with a core crew (a player on 10+ dates), core size median | 364, 4 | 351, 4 |
| Session players by city, 1960: New York / Nashville / Los Angeles / Chicago / Detroit | 65 / 17 / 30 / 21 / 14 | 78 / 19 / 18 / 26 / 19 |
| Contact edges at the end: session / former bandmate (fallouts) | 28,336 / 551 (103) | 26,758 / 603 (148) |
| Pool hires through a contact | 4 | 4 |
| Band-life outcomes, 1960 pass | identical to reference | identical to reference |
| Member wealth p99, 1960 | $2,080 → $2,242 | $2,214 → $2,375 |
| Singles / album units, 2 years | 0.00% / 0.00% | +0.06% / −0.26% |

The shape is right without being authored. The session trade concentrates in New York, Nashville, Los Angeles,
Chicago and Detroit. Nashville carries a core of 17-19 players (the A-Team was about that size), and the busiest
players do 500+ dates a year, two a day. Most session players are moonlighting members of local acts; only 1-2 are
pooled, because the pool is small and turns over fast.

Contact hires are few for now: former-bandmate edges first appear at the 1960 pass, the same pass that hires.
Session edges link players to singers, and they matter once a singer forms or joins a band. Both compound over a
decade, and the decade comparison measures that.

## 4. Phase 2 — built

- **House bands** (`SceneHouseBandService`, `--disable-scene-house-bands`). An act that can't back itself (the
  session rule: fewer than two active rhythm players) is backed by three local players on a club, listening-room or
  roadhouse bill. Coffeehouses and community halls are left as they are.
  - **Who:** working players (`technicalSkill` ≥ 0.55, below the session bar; a house band reads less than a studio).
    They are members of acts present in the town, or pooled players based there. They are ranked by a room-regular
    bonus (up to +1 at 20 shows), a contact bonus (+0.5 for knowing the singer) and skill.
  - **Booking:** backers are reserved like performers: one room a day, no double-booked hour. The room census's
    conflict check covers them.
  - **Pay:** they are paid per player. A door is split across act and band; a wage or flat fee is paid to each
    player. They are linked as `HouseBand` contacts.
  - **Ledger:** their hours and pay sit in the room ledger as `Backing` work rows. A pooled backer's house work also
    keeps them in music.
  - **Solo door share:** this fixes the high end flagged in the live calibration. The expected-night pay for a backed
    act also counts the band.
- **Employment counts as practice** (`--disable-employment-growth`). An act member's session hours (3 per session) at
  `KindSession` and house-band hours at `KindResidency` are added on top of the act's own allowance.
- **Player surfaces.**
  - The Band Room audition list says how a candidate knows the act: "Played with Joe in The Hawks.", "Worked
    sessions with Joe for Decca.", or "Fell out with Joe in The Hawks.".
  - A new desk action, **Ask your musicians who they know** (1 hour, scene-rooms board), turns the roster's contact
    edges into up to three dated tips: who is between bands, and where a former bandmate or session partner plays
    now. For example: "Spencer Harris: Enzo Mitchell now plays in The Jackson Strings, on Weathered Records; they cut
    sessions together for Hurting Records."
  - Tips never repeat a person in the same state, fallouts are not passed on, and a stale tip says so. Tips are
    saved with the player (`SceneInformationKnowledge.Tips`).
- **Crews are hired when the record is made** (`GenerateRecordFromArtist`), not at release. Singles pulled from an
  album, compilations and reissues no longer hire a second crew, so crewed records fell from ~4,300 to ~3,000 a year.
- **The record lift** (`--disable-session-record-lift`). Production moves 0.30 per point of crew score (mean skill and
  reading) off the typical crew. Typical is 0.876 / 0.881, measured on `made-v1` seeds 1001 / 2002. It is zero-centred
  because existing record calibration already stands for the session players every vocal record used. A top crew adds
  ~0.015; a thin small-town crew costs ~0.04.
- **Checks:** `--contact-network-check` (21): flags, edge invariants, link semantics, ledgers, the audition sentence,
  the player's tips across a player save, and a byte-identical world round-trip of the network and both ledgers.

### Results — Phase 2

`house-v1` (house bands, employment growth, tips) against `ref-45c5219`, 104 weeks:

| | seed 1001 | seed 2002 |
|---|---:|---:|
| Backers / rooms / backer-sets (2 years) | 310 / 92 / 100,476 | 295 / 92 / 105,595 |
| Player-room pairs with 20+ shows (standing house bands) | 382 | 387 |
| Live income p50 / p99 / max, 1960 | $1,012 / $7,776 / $9,609 → $864 / $3,038 / $6,219 | $1,024 / $7,776 / $9,624 → $902 / $3,038 / $5,744 |
| Room census person conflicts / remote days | 0 / 0 | 0 / 0 |
| Band-life outcomes, 1960 pass | identical | identical |
| Singles / album units, 2 years | −0.05% / +0.19% | +0.03% / −0.11% |

Record lift: `lift-v1` against `ref-dc20d52` (`made-v1`: crews at recording, lift inert), 104 weeks. This is the
first change in this directive that moves the economy, so band-life numbers move too, because charts feed success.
The 1960 band-life deltas below run in opposite directions on the two seeds: that is the reshuffled stream, not
an effect.

| | seed 1001 | seed 2002 |
|---|---:|---:|
| Mean crew score | 0.875 | 0.881 |
| Singles / album units, 2 years | −0.62% / +1.75% | −0.82% / +2.04% |
| First-chart events, all labels | 314 → 328 | 316 → 320 |
| First-chart events by New York labels | 86 → 94 | 103 → 107 |
| Strain departures, 1960 | 32 → 43 | 50 → 39 |
| Pool hires, 1960 | 153 → 173 | 170 → 165 |

New York labels gain a few first-chart entries on both seeds and the thinner session towns lose one or two; that is
the intended direction, but it is inside two-seed noise. The one consistent signal is album units +1.8-2.0% with
singles −0.6-0.8%, on both seeds. The lift is zero-mean on production, so this is most likely convexity: sales
respond more to a good album than they lose on a weak one. It is small, and the decade comparison decides whether
it stands; `--disable-session-record-lift` isolates it.

## 5. Phase 3 — the rest of the handoff item

Autonomous AI investment in scenes (funded programs exist; AI labels deciding to fund them does not), consented
migration driven by work and contacts, and repertoire influence: standards and arrangements travelling with people
and performances.
