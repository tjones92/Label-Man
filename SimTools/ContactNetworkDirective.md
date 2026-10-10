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

## 4. Phase 2 — next

- **House bands.** A solo act on a room bill is backed by local players (pooled first, then members of acts not
  booked that night), paid per player, linked as contacts. This also fixes the solo door-share high end in the live
  calibration (`LocalSceneLiveCalibrationDirective.md` §5).
- **Session hours feed growth.** Studio work for others is real practice at `KindSession`. It moves growth, so it
  needs the reference comparison.
- **Sessions feed the record.** A strong crew lifts execution, as the player's Band Room session hire already does.
  This moves the economy, so it lands last, behind the decade comparison.
- **Player surfaces.** Notebook entries for "who knows whom", leads where a contact tells the player where a former
  member landed (Rolodex rule: every sentence backed by an event and IDs), and a session-crew line on a label card.
- **Wider contacts.** Bookers (already familiar), DJs (Rolodex), record shops, distributors and rival A&R as contact
  nodes, so a tip can come through the people the player actually knows.

## 5. Phase 3 — the rest of the handoff item

Autonomous AI investment in scenes (funded programs exist; AI labels deciding to fund them does not), consented
migration driven by work and contacts, and repertoire influence: standards and arrangements travelling with people
and performances.
